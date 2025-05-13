namespace dotnet_lox;

internal interface IExprVisitor<R>
{
    R Visit(Binary binary);
    R Visit(Grouping grouping);
    R Visit(Unary unary);
    R Visit(Literal literal);
    R Visit(Variable variable);
    R Visit(Assign assign);
    R Visit(Logical logical);
}