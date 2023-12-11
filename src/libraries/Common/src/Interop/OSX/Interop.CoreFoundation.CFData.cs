// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

// Declared as signed long, which has sizeof(void*) on OSX.
using CFIndex = System.IntPtr;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        [LibraryImport(Libraries.CoreFoundationLibrary)]
        private static unsafe partial byte* CFDataGetBytePtr(SafeCFDataHandle cfData);

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        private static partial CFIndex CFDataGetLength(SafeCFDataHandle cfData);

        internal static unsafe Span<byte> CFDataDangerousGetSpan(SafeCFDataHandle cfData)
        {
            long length = CFDataGetLength(cfData).ToInt64();
            byte* dataBytes = CFDataGetBytePtr(cfData);
            return new Span<byte>(dataBytes, checked((int)length));
        }

        internal static byte[] CFGetData(SafeCFDataHandle cfData)
        {
            bool addedRef = false;

            try
            {
                cfData.DangerousAddRef(ref addedRef);
                return CFDataDangerousGetSpan(cfData).ToArray();
            }
            finally
            {
                if (addedRef)
                {
                    cfData.DangerousRelease();
                }
            }
        }

        internal static unsafe bool TryCFWriteData(SafeCFDataHandle cfData, Span<byte> destination, out int bytesWritten)
        {
            bool addedRef = false;
            try
            {
                cfData.DangerousAddRef(ref addedRef);

                long length = CFDataGetLength(cfData).ToInt64();

                if (length > 0)
                {
                    if (destination.Length < length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    byte* dataBytes = CFDataGetBytePtr(cfData);
                    fixed (byte* destinationPtr = &MemoryMarshal.GetReference(destination))
                    {
                        Buffer.MemoryCopy(dataBytes, destinationPtr, destination.Length, length);
                    }
                }

                bytesWritten = (int)length;
                return true;
            }
            finally
            {
                if (addedRef)
                {
                    cfData.DangerousRelease();
                }
            }
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeCFDataHandle : SafeHandle
    {
        public SafeCFDataHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal SafeCFDataHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
