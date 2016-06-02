using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build
{
    public class BuildVersion : Version
    {
        public string SimpleVersion => $"{Major}.{Minor}.{Patch}.{CommitCountString}";
        public string VersionSuffix => $"{ReleaseSuffix}-{CommitCountString}";
        public string NuGetVersion => $"{Major}.{Minor}.{Patch}-{VersionSuffix}";
        public string NetCoreAppVersion => $"{Major}.{Minor}.{Patch}-{ReleaseSuffix}-{CommitCountString}";
        public string ProductionVersion => $"{Major}.{Minor}.{Patch}";
    }
}
