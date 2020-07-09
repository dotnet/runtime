// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.PortableExecutable
{
    public readonly struct SectionLocation
    {
        public int RelativeVirtualAddress { get; }
        public int PointerToRawData { get; }

        public SectionLocation(int relativeVirtualAddress, int pointerToRawData)
        {
            RelativeVirtualAddress = relativeVirtualAddress;
            PointerToRawData = pointerToRawData;
        }
    }
}
