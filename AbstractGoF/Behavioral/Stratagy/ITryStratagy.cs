namespace AbstractGoF.Behavioral.Stratagy
{
    public interface ITryStratagy<TResult, TContext>
    {
        bool TryDoStratagy(out TResult result, TContext context);
    }
}
