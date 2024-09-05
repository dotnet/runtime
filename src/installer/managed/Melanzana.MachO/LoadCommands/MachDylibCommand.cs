// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Melanzana.MachO.BinaryFormat;

namespace Melanzana.MachO
{
    public abstract class MachDylibCommand : MachLoadCommand
    {
        public string Name { get; set; } = string.Empty;

        public uint Timestamp { get; set; }

        public uint CurrentVersion { get; set; }

        public uint CompatibilityVersion { get; set; }
    }
}
