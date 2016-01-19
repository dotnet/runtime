// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if SRM
namespace System.Reflection.PortableExecutable
{
    public struct DirectoryEntry
    {
        public readonly int RelativeVirtualAddress;
        public readonly int Size;

        public DirectoryEntry(int relativeVirtualAddress, int size)
        {
            RelativeVirtualAddress = relativeVirtualAddress;
            Size = size;
        }

        internal DirectoryEntry(ref PEBinaryReader reader)
        {
            RelativeVirtualAddress = reader.ReadInt32();
            Size = reader.ReadInt32();
        }
    }
}
#else
namespace Microsoft.Cci
{
    // TODO: merge with System.Reflection.PortableExecutable.DirectoryEntry
    internal struct DirectoryEntry
    {
        public readonly int RelativeVirtualAddress;
        public readonly int Size;

        public DirectoryEntry(
            int relativeVirtualAddress,
            int size)
        {
            RelativeVirtualAddress = relativeVirtualAddress;
            Size = size;
        }
    }
}
#endif
