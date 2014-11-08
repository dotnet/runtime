// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Reflection.PortableExecutable
{
    public struct DirectoryEntry
    {
        public readonly int RelativeVirtualAddress;
        public readonly int Size;

        internal DirectoryEntry(ref PEBinaryReader reader)
        {
            RelativeVirtualAddress = reader.ReadInt32();
            Size = reader.ReadInt32();
        }
    }
}