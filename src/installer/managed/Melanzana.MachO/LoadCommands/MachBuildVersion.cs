namespace Melanzana.MachO
{
    public class MachBuildVersion : MachBuildVersionBase
    {
        public override MachPlatform Platform => TargetPlatform;

        public MachPlatform TargetPlatform { get; set; }

        public IList<MachBuildToolVersion> ToolVersions { get; set; } = new List<MachBuildToolVersion>();
    }
}