// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public abstract class MachBuildVersionBase : MachLoadCommand
    {
        internal static readonly Version EmptyVersion = new Version(0, 0, 0);

        public abstract MachPlatform Platform { get; }

        public Version MinimumPlatformVersion { get; set; } = EmptyVersion;

        public Version SdkVersion { get; set; } = EmptyVersion;
    }
}
