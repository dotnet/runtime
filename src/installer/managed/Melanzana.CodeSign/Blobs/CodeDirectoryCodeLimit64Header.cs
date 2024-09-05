// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryCodeLimit64Header
    {
        public uint Reserved;
        public ulong CodeLimit64;
    }
}