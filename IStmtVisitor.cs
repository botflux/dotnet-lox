interface IStmtVisitor<R>
{
    R Visit(Print print);
    R Visit(Expression expression);
    R Visit(Var var);
    R Visit(Block block);
    R Visit(If @if);
}