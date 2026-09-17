// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1822 // Mark members as static

namespace System.Runtime
{
    internal class ThunksHeap
    {
        public static unsafe ThunksHeap CreateThunksHeap(IntPtr commonStubAddress)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_DynamicEntrypoint);
        }

        public unsafe IntPtr AllocateThunk()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_DynamicEntrypoint);
        }

        public unsafe void FreeThunk(IntPtr thunkAddress)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_DynamicEntrypoint);
        }

        public unsafe bool TryGetThunkData(IntPtr thunkAddress, out IntPtr context, out IntPtr target)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_DynamicEntrypoint);
        }

        public unsafe void SetThunkData(IntPtr thunkAddress, IntPtr context, IntPtr target)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_DynamicEntrypoint);
        }
    }
}
