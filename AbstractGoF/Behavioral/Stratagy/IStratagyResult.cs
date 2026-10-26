namespace AbstractGoF.Behavioral.Stratagy
{
    public interface IStratagyResult<TResult, TContext>
    {
        TResult DoStratagy(TContext context);
    }
}
