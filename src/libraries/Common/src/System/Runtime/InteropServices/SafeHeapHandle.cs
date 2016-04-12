// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Handle for heap memory that allows tracking of capacity and reallocating.
    /// </summary>
    internal sealed class SafeHeapHandle : SafeBuffer
    {
        /// <summary>
        /// Allocate a buffer of the given size if requested.
        /// </summary>
        /// <param name="byteLength">Required size in bytes. Must be less than UInt32.MaxValue for 32 bit or UInt64.MaxValue for 64 bit.</param>
        /// <exception cref="OutOfMemoryException">Thrown if the requested memory size cannot be allocated.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if size is greater than the maximum memory size.</exception>
        public SafeHeapHandle(ulong byteLength) : base(ownsHandle: true)
        {
            Resize(byteLength);
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        /// <summary>
        /// Resize the buffer to the given size if requested.
        /// </summary>
        /// <param name="byteLength">Required size in bytes. Must be less than UInt32.MaxValue for 32 bit or UInt64.MaxValue for 64 bit.</param>
        /// <exception cref="OutOfMemoryException">Thrown if the requested memory size cannot be allocated.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if size is greater than the maximum memory size.</exception>
        public void Resize(ulong byteLength)
        {
            if (IsClosed) throw new ObjectDisposedException(nameof(SafeHeapHandle));

            ulong originalLength = 0;
            if (handle == IntPtr.Zero)
            {
                handle = Marshal.AllocHGlobal((IntPtr)byteLength);
            }
            else
            {
                originalLength = ByteLength;

                // This may or may not be the same handle, may realloc in place. If the
                // handle changes Windows will deal with the old handle, trying to free it will
                // cause an error.
                handle = Marshal.ReAllocHGlobal(pv: handle, cb: (IntPtr)byteLength);
            }

            if (handle == IntPtr.Zero)
            {
                // Only real plausible answer
                throw new OutOfMemoryException();
            }

            if (byteLength > originalLength)
            {
                // Add pressure
                ulong addedBytes = byteLength - originalLength;
                if (addedBytes > long.MaxValue)
                {
                    GC.AddMemoryPressure(long.MaxValue);
                    GC.AddMemoryPressure((long)(addedBytes - long.MaxValue));
                }
                else
                {
                    GC.AddMemoryPressure((long)addedBytes);
                }
            }
            else
            {
                // Shrank or did nothing, release pressure if needed
                RemoveMemoryPressure(originalLength - byteLength);
            }

            Initialize(byteLength);
        }

        private void RemoveMemoryPressure(ulong removedBytes)
        {
            if (removedBytes == 0) return;

            if (removedBytes > long.MaxValue)
            {
                GC.RemoveMemoryPressure(long.MaxValue);
                GC.RemoveMemoryPressure((long)(removedBytes - long.MaxValue));
            }
            else
            {
                GC.RemoveMemoryPressure((long)removedBytes);
            }
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                RemoveMemoryPressure(ByteLength);
                Marshal.FreeHGlobal(handle);
            }

            handle = IntPtr.Zero;
            return true;
        }
    }
}
