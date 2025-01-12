// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.PortableExecutable
{
    public readonly struct DirectoryEntry
    {
        public readonly int RelativeVirtualAddress;
        public readonly int Size;

        public DirectoryEntry(int relativeVirtualAddress, int size)
        {
            RelativeVirtualAddress = relativeVirtualAddress;
            Size = size;
        }

        internal static DirectoryEntry Create<TReader>(ref TReader reader) where TReader : IBinaryReader
        {
            return new DirectoryEntry(reader.ReadInt32(), reader.ReadInt32());
        }
    }
}
