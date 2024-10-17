// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    internal sealed partial class MainCommandHeader
    {
        public ulong FileOffset { get; set; }
        public ulong StackSize { get; set; }
    }
}
