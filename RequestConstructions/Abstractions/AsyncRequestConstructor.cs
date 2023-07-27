namespace SquidORM.RequestConstructions.Abstractions
{
    public abstract class AsyncRequestConstructor : BaseRequestConstructor
    {
        public abstract ValueTask<string> ConstructAsync();
    }
}
