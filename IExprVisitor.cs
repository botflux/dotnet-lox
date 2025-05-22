namespace dotnet_lox;

internal interface IExprVisitor<T>
{
    T Visit(Binary binary);
    T Visit(Grouping grouping);
    T Visit(Unary unary);
    T Visit(Literal literal);
    T Visit(Variable variable);
    T Visit(Assign assign);
    T Visit(Logical logical);
    T Visit(Call call);
    T Visit(Pipe pipe);
}