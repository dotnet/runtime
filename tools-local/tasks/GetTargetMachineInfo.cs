// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GetTargetMachineInfo : BuildTask
    {
        [Output]
        public string TargetOS { get; set; }

        [Output]
        public string TargetArch { get; set; }

        [Output]
        public string RuntimeIdentifier { get; set; }

        public override bool Execute()
        {
            switch (RuntimeInformation.OSArchitecture)
            {
                case Architecture.X64:
                    TargetArch = "x64";
                    break;
                case Architecture.X86:
                    TargetArch = "x86";
                    break;
                case Architecture.Arm:
                    TargetArch = "arm";
                    break;
                case Architecture.Arm64:
                    TargetArch = "arm64";
                    break;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                TargetOS = "Windows_NT";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                TargetOS = "Linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                TargetOS = "OSX";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
                TargetOS = "FreeBSD";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")))
                TargetOS = "NetBSD";

            RuntimeIdentifier = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();

            if (TargetArch == null)
            {
                Log.LogError("{0} is null", nameof(TargetArch));
                return false;
            }

            if (TargetOS == null)
            {
                Log.LogError("{0} is null", nameof(TargetOS));
                return false;
            }

            return true;
        }
    }
}
