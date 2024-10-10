// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed class MachBuildVersion : MachBuildVersionBase
    {
        public override MachPlatform Platform => TargetPlatform;

        public MachPlatform TargetPlatform { get; set; }

        public IList<MachBuildToolVersion> ToolVersions { get; set; } = new List<MachBuildToolVersion>();
    }
}
