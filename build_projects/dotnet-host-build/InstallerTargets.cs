using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Host.Build
{
    public class InstallerTargets
    {
        [Target(nameof(MsiTargets.GenerateMsis),
        nameof(MsiTargets.GenerateBundles),
        nameof(PkgTargets.GeneratePkgs),
        nameof(DebTargets.GenerateDebs))]
        public static BuildTargetResult GenerateInstaller(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target(nameof(DebTargets.TestDebInstaller))]
        public static BuildTargetResult TestInstaller(BuildTargetContext c)

        {
            return c.Success();
        }
    }
}
