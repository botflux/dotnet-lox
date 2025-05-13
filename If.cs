record If(Expr Condition, Stmt ThenBranch, Stmt? ElseBranch) : Stmt
{
    public override T Accept<T>(IStmtVisitor<T> visitor) => visitor.Visit(this);
};