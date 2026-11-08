namespace WindowsHost.ECS.DataComponent
{
    internal struct Health
    {
        public int Current;
        public int Max;

        public override string ToString() => $"HP({Current}/{Max})";
    }
}