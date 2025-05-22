using System;
using System.Collections.Generic;
using System.Linq;

namespace dotnet_lox;

internal class Desugarer : IExprVisitor<Expr>, IStmtVisitor<Stmt>
{
    private class PlaceholderSubstituter : IExprVisitor<Expr>
    {
        private readonly Expr _replacementExpr;
        public bool PlaceholderFound { get; private set; }

        public PlaceholderSubstituter(Expr replacementExpr)
        {
            _replacementExpr = replacementExpr;
            PlaceholderFound = false;
        }

        public Expr Visit(Assign assign)
        {
            // Assignment target (name) is not an expression to be substituted.
            // Value is an expression.
            return new Assign(assign.Name, assign.Value.Accept(this));
        }

        public Expr Visit(Binary binary)
        {
            return new Binary(binary.Left.Accept(this), binary.Op, binary.Right.Accept(this));
        }

        public Expr Visit(Call call)
        {
            var callee = call.Callee.Accept(this);
            var arguments = call.Arguments.Select(arg => arg.Accept(this)).ToList();
            return new Call(callee, call.Paren, arguments);
        }

        public Expr Visit(Grouping grouping)
        {
            return new Grouping(grouping.Expression.Accept(this));
        }

        public Expr Visit(Literal literal)
        {
            return literal; // Literals don't contain placeholders
        }

        public Expr Visit(Logical logical)
        {
            return new Logical(logical.Left.Accept(this), logical.Operator, logical.Right.Accept(this));
        }

        public Expr Visit(Unary unary)
        {
            return new Unary(unary.Op, unary.Right.Accept(this));
        }

        public Expr Visit(Variable variable)
        {
            if (variable.Name.Type == TokenType.Dollar)
            {
                PlaceholderFound = true;
                return _replacementExpr;
            }
            return variable;
        }

        public Expr Visit(Pipe pipe)
        {
            // As per prompt: "when the PlaceholderSubstituter for outer_lhs visits this nested Pipe node, 
            // it should only substitute into inner_lhs if it contains $. 
            // It must *not* descend into inner_rhs_with_$"
            // This means we only apply the current substitution to the left side of a nested pipe.
            // The right side remains untouched by *this* substituter.
            // It will be handled by its own desugaring step when the outer Desugarer visits it.
            Expr newLeft = pipe.Left.Accept(this);
            return new Pipe(newLeft, pipe.OperatorToken, pipe.Right);
        }
    }

    public List<Stmt> Desugar(List<Stmt> statements)
    {
        var desugaredStatements = new List<Stmt>();
        foreach (var statement in statements)
        {
            if (statement != null) // Though current parser doesn't produce null Stmts
            {
                desugaredStatements.Add(statement.Accept(this));
            }
        }
        return desugaredStatements;
    }

    // --- IExprVisitor<Expr> Methods for Desugarer ---

    public Expr Visit(Assign assign)
    {
        // The left side of an assignment (assign.Name) is a Token, not an Expr.
        // We desugar the right-hand side value.
        return new Assign(assign.Name, assign.Value.Accept(this));
    }

    public Expr Visit(Binary binary)
    {
        return new Binary(binary.Left.Accept(this), binary.Op, binary.Right.Accept(this));
    }

    public Expr Visit(Call call)
    {
        var callee = call.Callee.Accept(this);
        var arguments = call.Arguments.Select(arg => arg.Accept(this)).ToList();
        return new Call(callee, call.Paren, arguments);
    }

    public Expr Visit(Grouping grouping)
    {
        return new Grouping(grouping.Expression.Accept(this));
    }

    public Expr Visit(Literal literal)
    {
        return literal; // Literals are atomic, no desugaring needed for their content
    }

    public Expr Visit(Logical logical)
    {
        return new Logical(logical.Left.Accept(this), logical.Operator, logical.Right.Accept(this));
    }

    public Expr Visit(Unary unary)
    {
        return new Unary(unary.Op, unary.Right.Accept(this));
    }

    public Expr Visit(Variable variable)
    {
        // Variables (including $) are atomic in terms of structure from Desugarer's view.
        // The $ is handled by PlaceholderSubstituter.
        return variable;
    }

    public Expr Visit(Pipe pipe)
    {
        // 1. Desugar the left-hand side first. This LHS might itself be a pipe.
        Expr desugaredLhs = pipe.Left.Accept(this);

        // 2. Create a substituter with the (potentially already complex) desugared LHS.
        PlaceholderSubstituter substituter = new PlaceholderSubstituter(desugaredLhs);

        // 3. Use the substituter on the *original* (non-desugared by this Desugarer pass yet) RHS.
        // The substituter will replace $ in the RHS.
        Expr rhsWithSubstitutions = pipe.Right.Accept(substituter);

        // 4. Check if any placeholder was actually found and replaced.
        if (!substituter.PlaceholderFound)
        {
            Reporter.Error(pipe.OperatorToken, "The right-hand side of a pipe operator ('|>') must contain at least one '$' placeholder.");
            // Return the original RHS or the partially substituted one to allow further error collection.
            // Or, to be safe and avoid potential execution of invalid AST, could return desugaredLhs or a specific error node if we had one.
            // For now, returning the result of substitution attempt. If no '$', it's the original RHS.
            return rhsWithSubstitutions; 
        }

        // 5. The result of the substitution might itself be a pipe expression or contain pipes
        // (e.g. if $ was replaced by a pipe, or if RHS was `foo($) |> bar($)`).
        // So, we need to recursively desugar this new structure.
        return rhsWithSubstitutions.Accept(this);
    }

    // --- IStmtVisitor<Stmt> Methods for Desugarer ---

    public Stmt Visit(Block block)
    {
        List<Stmt> desugaredStmts = new List<Stmt>();
        foreach (Stmt stmtInBlock in block.Statements)
        {
            desugaredStmts.Add(stmtInBlock.Accept(this));
        }
        return new Block(desugaredStmts);
    }

    public Stmt Visit(Expression expression)
    {
        return new Expression(expression.Expr.Accept(this));
    }

    public Stmt Visit(Function function)
    {
        // Function parameters and name are not desugared in this context.
        // The body is a list of statements that needs desugaring.
        List<Stmt> desugaredBody = new List<Stmt>();
        foreach (Stmt stmtInFunc in function.Body)
        {
            desugaredBody.Add(stmtInFunc.Accept(this));
        }
        return new Function(function.Name, function.Parameters, desugaredBody);
    }

    public Stmt Visit(If ifStmt)
    {
        Expr condition = ifStmt.Condition.Accept(this);
        Stmt thenBranch = ifStmt.ThenBranch.Accept(this);
        Stmt? elseBranch = ifStmt.ElseBranch?.Accept(this);
        return new If(condition, thenBranch, elseBranch);
    }

    public Stmt Visit(Print print)
    {
        return new Print(print.Expression.Accept(this));
    }

    public Stmt Visit(Return returnStmt)
    {
        Expr? value = returnStmt.Value?.Accept(this);
        return new Return(returnStmt.Keyword, value);
    }

    public Stmt Visit(Var varStmt)
    {
        Expr? initializer = varStmt.Initializer?.Accept(this);
        return new Var(varStmt.Name, initializer);
    }

    public Stmt Visit(While whileStmt)
    {
        Expr condition = whileStmt.Condition.Accept(this);
        Stmt body = whileStmt.Body.Accept(this);
        return new While(condition, body);
    }
}
