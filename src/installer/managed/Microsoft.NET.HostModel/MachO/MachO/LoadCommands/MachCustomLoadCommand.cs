// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.HostModel.MachO.BinaryFormat;

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed class MachCustomLoadCommand : MachLoadCommand
    {
        public MachCustomLoadCommand(MachLoadCommandType type, byte[] data)
        {
            this.Type = type;
            this.Data = data;
        }

        public MachLoadCommandType Type { get; set; }

        public byte[] Data { get; set; }
    }
}
