namespace POPS.Objectives
{
    public abstract class Objective<TContext>
    {
        public abstract void TryComplite(TContext context);
    }
}
