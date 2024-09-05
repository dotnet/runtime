// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryExecSegmentHeader
    {
        public ulong Base;
        public ulong Limit;
        public ExecutableSegmentFlags Flags;
    }
}