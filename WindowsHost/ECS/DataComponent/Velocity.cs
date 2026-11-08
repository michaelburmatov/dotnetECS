namespace WindowsHost.ECS.DataComponent
{
    internal struct Velocity
    {
        public float VX;
        public float VY;
        public float VZ;

        public override string ToString() => $"Vel({VX:F2}, {VY:F2}, {VZ:F2})";
    }
}