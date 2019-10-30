// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using HeapAllocFlags = Interop.Kernel32.HeapAllocFlags;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeHeapAllocHandle : SafeBuffer, IDisposable
    {
        private SafeHeapAllocHandle() : base(true) { }

        internal static SafeHeapAllocHandle Alloc(int size)
        {
            SafeHeapAllocHandle result = Interop.Kernel32.HeapAlloc(s_hHeap, HeapAllocFlags.None, size);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }
#if DEBUG
            result._size = size;
            unsafe
            {
                byte* p = (byte*)(result.DangerousGetHandle());
                for (int i = 0; i < size; i++)
                {
                    p[i] = 0xcc;
                }
            }
#endif
            return result;
        }

        protected sealed override bool ReleaseHandle()
        {
#if DEBUG
            unsafe
            {
                byte* p = (byte*)handle;
                for (int i = 0; i < _size; i++)
                {
                    p[i] = 0xcc;
                }
            }
#endif
            bool success = Interop.Kernel32.HeapFree(s_hHeap, HeapAllocFlags.None, handle);
            return success;
        }

#if DEBUG
        private int _size;
#endif
        private static readonly IntPtr s_hHeap = Interop.Kernel32.GetProcessHeap();
    }
}
