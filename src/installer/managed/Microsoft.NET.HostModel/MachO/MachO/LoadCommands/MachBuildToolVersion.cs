// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed class MachBuildToolVersion
    {
        public MachBuildTool BuildTool { get; set; }

        public Version Version { get; set; } = MachBuildVersionBase.EmptyVersion;
    }
}
