using System;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class CurrentArchitecture
    {
        public static BuildArchitecture Current
        {
            get
            {
                return DetermineCurrentArchitecture();
            }
        }

        public static bool Isx86
        {
            get
            {
                var archName = RuntimeEnvironment.RuntimeArchitecture;
                return string.Equals(archName, "x86", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool Isx64
        {
            get
            {
                var archName = RuntimeEnvironment.RuntimeArchitecture;
                return string.Equals(archName, "x64", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static BuildArchitecture DetermineCurrentArchitecture()
        {
            if (Isx86)
            {
                return BuildArchitecture.x86;
            }
            else if (Isx64)
            {
                return BuildArchitecture.x64;
            }
            else
            {
                return default(BuildArchitecture);
            }
        }
    }
}