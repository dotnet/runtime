// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.NET.HostModel.MachO.BinaryFormat;

namespace Microsoft.NET.HostModel.MachO
{
    internal abstract class MachDylibCommand : MachLoadCommand
    {
        internal string Name { get; set; } = string.Empty;

        internal uint Timestamp { get; set; }

        internal uint CurrentVersion { get; set; }

        internal uint CompatibilityVersion { get; set; }
    }
}
