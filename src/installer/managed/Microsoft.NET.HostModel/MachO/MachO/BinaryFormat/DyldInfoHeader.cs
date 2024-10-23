// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    internal sealed partial class DyldInfoHeader
    {
        public uint RebaseOffset { get; set; }
        public uint RebaseSize { get; set; }
        public uint BindOffset { get; set; }
        public uint BindSize { get; set; }
        public uint WeakBindOffset { get; set; }
        public uint WeakBindSize { get; set; }
        public uint LazyBindOffset { get; set; }
        public uint LazyBindSize { get; set; }
        public uint ExportOffset { get; set; }
        public uint ExportSize { get; set; }
    }
}
