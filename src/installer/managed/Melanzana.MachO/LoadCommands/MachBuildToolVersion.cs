namespace Melanzana.MachO
{
    public class MachBuildToolVersion
    {
        public MachBuildTool BuildTool { get; set; }

        public Version Version { get; set; } = MachBuildVersionBase.EmptyVersion;
    }
}