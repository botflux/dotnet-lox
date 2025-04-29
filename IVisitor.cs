interface IVisitor<R>
{
    R Visit(Binary binary);
    R Visit(Grouping grouping);
    R Visit(Unary unary);
    R Visit(Literal literal);
}