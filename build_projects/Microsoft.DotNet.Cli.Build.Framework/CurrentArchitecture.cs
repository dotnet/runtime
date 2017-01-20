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

        public static bool Isarm
        {
            get
            {
                var archName = Environment.GetEnvironmentVariable("TARGETPLATFORM");
                return string.Equals(archName, "arm", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool Isarmel
        {
            get
            {
                var archName = Environment.GetEnvironmentVariable("TARGETPLATFORM");
                return string.Equals(archName, "armel", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool Isarm64
        {
            get
            {
                var archName = Environment.GetEnvironmentVariable("TARGETPLATFORM");
                return string.Equals(archName, "arm64", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static BuildArchitecture DetermineCurrentArchitecture()
        {
            if (Isarm)
            {
                return BuildArchitecture.arm;
            }
            else if (Isarmel)
            {
                return BuildArchitecture.armel;
            }
            else if (Isarm64)
            {
                return BuildArchitecture.arm64;
            }
            else if (Isx86)
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
