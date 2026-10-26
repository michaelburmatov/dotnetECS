namespace AbstractGoF.Behavioral.Stratagy
{
    public interface IStrategy<T>
    {
        void DoStratagy(T context);
    }
}
