// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if SRM
namespace System.Reflection.PortableExecutable
#else
namespace Roslyn.Reflection.PortableExecutable
#endif
{
#if SRM
    public
#endif
    struct PESectionLocation
    {
        public int RelativeVirtualAddress { get; }
        public int PointerToRawData { get; }

        public PESectionLocation(int relativeVirtualAddress, int pointerToRawData)
        {
            RelativeVirtualAddress = relativeVirtualAddress;
            PointerToRawData = pointerToRawData;
        }
    }
}
