namespace WindowsHost.ECS.DataComponent
{
    internal struct Position
    {
        public float X;
        public float Y;
        public float Z;

        public override string ToString() => $"Pos({X:F2}, {Y:F2}, {Z:F2})";
    }
}