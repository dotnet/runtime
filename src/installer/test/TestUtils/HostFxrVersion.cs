namespace TestUtils
{
    public class HostFxrVersion
    {
        public int Major { get; }
        public int Minor { get; }
        public int? Patch { get; }

        public bool HasCustomErrorWriter { get; }

        public HostFxrVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            HasCustomErrorWriter = (Major < 3) ? false : true;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }
}
