// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class Runtime_58874
{
    private static int Main(string[] args)
    {
        void* mem;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const int MEM_COMMIT = 0x1000;
            const int MEM_RESERVE = 0x2000;
            const int PAGE_READWRITE = 0x04;

            // Reserve 2 pages
            void* pages = VirtualAlloc(null, 0x2000, MEM_RESERVE, PAGE_READWRITE);
            if (pages == null)
            {
                return 100;
            }
            // Commit first page
            mem = VirtualAlloc(pages, 0x1000, MEM_COMMIT, PAGE_READWRITE);
            if (mem != pages)
            {
                return 100;
            }
        }
        else
        {
            mem = NativeMemory.Alloc(0x1000);
        }

        ref Test validRef = ref *(Test*)((byte*)mem + 0x1000 - sizeof(Test));
        validRef = default;
        Foo(ref validRef);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Test Foo(ref Test t)
    {
        // This read was too wide.
        return t;
    }

    [DllImport("kernel32")]
    private static extern void* VirtualAlloc(void* lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);
}

struct Test
{
    public byte A, B;
}