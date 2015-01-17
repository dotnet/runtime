// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.MemoryMappedFiles
{
    internal partial class MemoryMappedView
    {
        [SecurityCritical]
        public unsafe static MemoryMappedView CreateView(SafeMemoryMappedFileHandle memMappedFileHandle,
                                            MemoryMappedFileAccess access, Int64 offset, Int64 size)
        {
            // MapViewOfFile can only create views that start at a multiple of the system memory allocation 
            // granularity. We decided to hide this restriction form the user by creating larger views than the
            // user requested and hiding the parts that the user did not request.  extraMemNeeded is the amount of
            // extra memory we allocate before the start of the requested view. MapViewOfFile will also round the 
            // capacity of the view to the nearest multiple of the system page size.  Once again, we hide this 
            // from the user by preventing them from writing to any memory that they did not request.
            ulong extraMemNeeded = (ulong)offset % (ulong)GetSystemPageAllocationGranularity();

            // newOffset takes into account the fact that we have some extra memory allocated before the requested view
            ulong newOffset = (ulong)offset - extraMemNeeded;
            Debug.Assert(newOffset >= 0, "newOffset = (offset - extraMemNeeded) < 0");

            // determine size to pass to MapViewOfFile
            ulong nativeSize = (size != MemoryMappedFile.DefaultSize) ?
                (ulong)size + (ulong)extraMemNeeded :
                0;

            if (IntPtr.Size == 4 && nativeSize > UInt32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("size", SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }

            // if request is >= than total virtual, then MapViewOfFile will fail with meaningless error message 
            // "the parameter is incorrect"; this provides better error message in advance
            Interop.MEMORYSTATUSEX memStatus;
            memStatus.dwLength = (uint)sizeof(Interop.MEMORYSTATUSEX);
            Interop.mincore.GlobalMemoryStatusEx(out memStatus);
            ulong totalVirtual = memStatus.ullTotalVirtual;
            if (nativeSize >= totalVirtual)
            {
                throw new IOException(SR.IO_NotEnoughMemory);
            }

            // split the Int64 into two ints
            int offsetLow = unchecked((int)(newOffset & 0x00000000FFFFFFFFL));
            int offsetHigh = unchecked((int)(newOffset >> 32));

            // create the view
            SafeMemoryMappedViewHandle viewHandle = Interop.mincore.MapViewOfFile(memMappedFileHandle,
                    (int)MemoryMappedFile.GetFileMapAccess(access), offsetHigh, offsetLow, new UIntPtr(nativeSize));
            if (viewHandle.IsInvalid)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            // Query the view for its size and allocation type
            Interop.MEMORY_BASIC_INFORMATION viewInfo = new Interop.MEMORY_BASIC_INFORMATION();
            Interop.mincore.VirtualQuery(viewHandle, ref viewInfo, (UIntPtr)Marshal.SizeOf(viewInfo));
            ulong viewSize = (ulong)viewInfo.RegionSize;

            // Allocate the pages if we were using the MemoryMappedFileOptions.DelayAllocatePages option
            // OR check if the allocated view size is smaller than the expected native size
            // If multiple overlapping views are created over the file mapping object, the pages in a given region
            // could have different attributes(MEM_RESERVE OR MEM_COMMIT) as MapViewOfFile preserves coherence between 
            // views created on a mapping object backed by same file.
            // In which case, the viewSize will be smaller than nativeSize required and viewState could be MEM_COMMIT 
            // but more pages may need to be committed in the region.
            // This is because, VirtualQuery function(that internally invokes VirtualQueryEx function) returns the attributes 
            // and size of the region of pages with matching attributes starting from base address.
            // VirtualQueryEx: http://msdn.microsoft.com/en-us/library/windows/desktop/aa366907(v=vs.85).aspx
            if (((viewInfo.State & Interop.MEM_RESERVE) != 0) || (viewSize < nativeSize))
            {
                IntPtr tempHandle = Interop.mincore.VirtualAlloc(viewHandle, (UIntPtr)nativeSize, Interop.MEM_COMMIT,
                                                        MemoryMappedFile.GetPageAccess(access));
                int lastError = Marshal.GetLastWin32Error();
                if (viewHandle.IsInvalid)
                {
                    throw Win32Marshal.GetExceptionForWin32Error(lastError);
                }
                // again query the view for its new size
                viewInfo = new Interop.MEMORY_BASIC_INFORMATION();
                Interop.mincore.VirtualQuery(viewHandle, ref viewInfo, (UIntPtr)Marshal.SizeOf(viewInfo));
                viewSize = (ulong)viewInfo.RegionSize;
            }

            // if the user specified DefaultSize as the size, we need to get the actual size
            if (size == MemoryMappedFile.DefaultSize)
            {
                size = (Int64)(viewSize - extraMemNeeded);
            }
            else
            {
                Debug.Assert(viewSize >= (ulong)size, "viewSize < size");
            }

            viewHandle.Initialize((ulong)size + extraMemNeeded);
            return new MemoryMappedView(viewHandle, (long)extraMemNeeded, size, access);
        }

        // Flushes the changes such that they are in sync with the FileStream bits (ones obtained
        // with the win32 ReadFile and WriteFile functions).  Need to call FileStream's Flush to 
        // flush to the disk.
        // NOTE: This will flush all bytes before and after the view up until an offset that is a multiple
        //       of SystemPageSize.
        [SecurityCritical]
        public void Flush(UIntPtr capacity)
        {
            if (_viewHandle != null)
            {
                unsafe
                {
                    byte* firstPagePtr = null;
                    try
                    {
                        _viewHandle.AcquirePointer(ref firstPagePtr);

                        bool success = Interop.mincore.FlushViewOfFile((IntPtr)firstPagePtr, capacity) != 0;
                        if (success)
                            return; // This will visit the finally block.

                        // It is a known issue within the NTFS transaction log system that
                        // causes FlushViewOfFile to intermittently fail with ERROR_LOCK_VIOLATION
                        // As a workaround, we catch this particular error and retry the flush operation 
                        // a few milliseconds later. If it does not work, we give it a few more tries with
                        // increasing intervals. Eventually, however, we need to give up. In ad-hoc tests
                        // this strategy successfully flushed the view after no more than 3 retries.

                        Int32 error = Marshal.GetLastWin32Error();
                        bool canRetry = (!success && error == Interop.ERROR_LOCK_VIOLATION);

                        SpinWait spinWait = new SpinWait();
                        for (Int32 w = 0; canRetry && w < MaxFlushWaits; w++)
                        {
                            Int32 pause = (1 << w);  // MaxFlushRetries should never be over 30
                            Task.Delay(pause).Wait();

                            for (Int32 r = 0; canRetry && r < MaxFlushRetriesPerWait; r++)
                            {
                                success = Interop.mincore.FlushViewOfFile((IntPtr)firstPagePtr, capacity) != 0;
                                if (success)
                                    return; // This will visit the finally block.

                                spinWait.SpinOnce();

                                error = Marshal.GetLastWin32Error();
                                canRetry = (error == Interop.ERROR_LOCK_VIOLATION);
                            }
                        }

                        // We got to here, so there was no success:
                        throw Win32Marshal.GetExceptionForWin32Error(error);
                    }
                    finally
                    {
                        if (firstPagePtr != null)
                        {
                            _viewHandle.ReleasePointer();
                        }
                    }
                }
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        [SecurityCritical]
        private static Int32 GetSystemPageAllocationGranularity()
        {
            Interop.SYSTEM_INFO info;
            Interop.mincore.GetSystemInfo(out info);

            return (Int32)info.dwAllocationGranularity;
        }
    }
}
