// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe class Runtime_58874
{
    private static int Main(string[] args)
    {
        using EndOfPage endOfPage = EndOfPage.Create();
        if (endOfPage != null)
        {
            Foo(endOfPage.Pointer);
        }
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Test Foo(Test* t)
    {
        // This read was too wide.
        return *t;
    }

    private class EndOfPage : IDisposable
    {
        private void* _addr;
        private EndOfPage()
        {
        }

        public Test* Pointer => (Test*)((byte*)_addr + 0x1000 - sizeof(Test));
        public void Dispose()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const int MEM_RELEASE = 0x8000;
                VirtualFree(_addr, 0, MEM_RELEASE);
            }
            else
            {
                NativeMemory.Free(_addr);
            }
        }

        public static EndOfPage Create()
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
                    return null;
                }
                // Commit first page
                mem = VirtualAlloc(pages, 0x1000, MEM_COMMIT, PAGE_READWRITE);
                if (mem != pages)
                {
                    return null;
                }
            }
            else
            {
                try
                {
                    mem = NativeMemory.Alloc(0x1000);
                }
                catch (OutOfMemoryException)
                {
                    return null;
                }
            }

            return new EndOfPage { _addr = mem };
        }

        [DllImport("kernel32")]
        private static extern void* VirtualAlloc(void* lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualFree(void* lpAddress, nuint dwSize, uint dwFreeType);
    }
}

struct Test
{
    public byte A, B;
}
