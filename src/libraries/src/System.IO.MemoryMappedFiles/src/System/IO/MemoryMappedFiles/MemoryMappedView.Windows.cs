// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        // These control the retry behaviour when lock violation errors occur during Flush:
        private const int MaxFlushWaits = 15;  // must be <=30
        private const int MaxFlushRetriesPerWait = 20;

        public static unsafe MemoryMappedView CreateView(SafeMemoryMappedFileHandle memMappedFileHandle,
                                            MemoryMappedFileAccess access, long offset, long size)
        {
            // MapViewOfFile can only create views that start at a multiple of the system memory allocation 
            // granularity. We decided to hide this restriction from the user by creating larger views than the
            // user requested and hiding the parts that the user did not request.  extraMemNeeded is the amount of
            // extra memory we allocate before the start of the requested view. MapViewOfFile will also round the 
            // capacity of the view to the nearest multiple of the system page size.  Once again, we hide this 
            // from the user by preventing them from writing to any memory that they did not request.
            ulong nativeSize;
            long extraMemNeeded, newOffset;
            ValidateSizeAndOffset(
                size, offset, GetSystemPageAllocationGranularity(),
                out nativeSize, out extraMemNeeded, out newOffset);

            // if request is >= than total virtual, then MapViewOfFile will fail with meaningless error message 
            // "the parameter is incorrect"; this provides better error message in advance
            Interop.CheckForAvailableVirtualMemory(nativeSize);

            // create the view
            SafeMemoryMappedViewHandle viewHandle = Interop.MapViewOfFile(memMappedFileHandle,
                    (int)MemoryMappedFile.GetFileMapAccess(access), newOffset, new UIntPtr(nativeSize));
            if (viewHandle.IsInvalid)
            {
                viewHandle.Dispose();
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            // Query the view for its size and allocation type
            Interop.Kernel32.MEMORY_BASIC_INFORMATION viewInfo = new Interop.Kernel32.MEMORY_BASIC_INFORMATION();
            Interop.Kernel32.VirtualQuery(viewHandle, ref viewInfo, (UIntPtr)Marshal.SizeOf(viewInfo));
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
            if (((viewInfo.State & Interop.Kernel32.MemOptions.MEM_RESERVE) != 0) || ((ulong)viewSize < (ulong)nativeSize))
            {
                IntPtr tempHandle = Interop.VirtualAlloc(
                    viewHandle, (UIntPtr)(nativeSize != MemoryMappedFile.DefaultSize ? nativeSize : viewSize), 
                    Interop.Kernel32.MemOptions.MEM_COMMIT, MemoryMappedFile.GetPageAccess(access));
                int lastError = Marshal.GetLastWin32Error();
                if (viewHandle.IsInvalid)
                {
                    viewHandle.Dispose();
                    throw Win32Marshal.GetExceptionForWin32Error(lastError);
                }
                // again query the view for its new size
                viewInfo = new Interop.Kernel32.MEMORY_BASIC_INFORMATION();
                Interop.Kernel32.VirtualQuery(viewHandle, ref viewInfo, (UIntPtr)Marshal.SizeOf(viewInfo));
                viewSize = (ulong)viewInfo.RegionSize;
            }

            // if the user specified DefaultSize as the size, we need to get the actual size
            if (size == MemoryMappedFile.DefaultSize)
            {
                size = (long)(viewSize - (ulong)extraMemNeeded);
            }
            else
            {
                Debug.Assert(viewSize >= (ulong)size, "viewSize < size");
            }

            viewHandle.Initialize((ulong)size + (ulong)extraMemNeeded);
            return new MemoryMappedView(viewHandle, extraMemNeeded, size, access);
        }

        // Flushes the changes such that they are in sync with the FileStream bits (ones obtained
        // with the win32 ReadFile and WriteFile functions).  Need to call FileStream's Flush to 
        // flush to the disk.
        // NOTE: This will flush all bytes before and after the view up until an offset that is a multiple
        //       of SystemPageSize.
        public void Flush(UIntPtr capacity)
        {
            unsafe
            {
                byte* firstPagePtr = null;
                try
                {
                    _viewHandle.AcquirePointer(ref firstPagePtr);

                    if (Interop.Kernel32.FlushViewOfFile((IntPtr)firstPagePtr, capacity))
                        return;

                    // It is a known issue within the NTFS transaction log system that
                    // causes FlushViewOfFile to intermittently fail with ERROR_LOCK_VIOLATION
                    // As a workaround, we catch this particular error and retry the flush operation 
                    // a few milliseconds later. If it does not work, we give it a few more tries with
                    // increasing intervals. Eventually, however, we need to give up. In ad-hoc tests
                    // this strategy successfully flushed the view after no more than 3 retries.

                    int error = Marshal.GetLastWin32Error();
                    if (error != Interop.Errors.ERROR_LOCK_VIOLATION)
                        throw Win32Marshal.GetExceptionForWin32Error(error);

                    SpinWait spinWait = new SpinWait();
                    for (int w = 0; w < MaxFlushWaits; w++)
                    {
                        int pause = (1 << w);  // MaxFlushRetries should never be over 30
                        Thread.Sleep(pause);

                        for (int r = 0; r < MaxFlushRetriesPerWait; r++)
                        {
                            if (Interop.Kernel32.FlushViewOfFile((IntPtr)firstPagePtr, capacity))
                                return;

                            error = Marshal.GetLastWin32Error();
                            if (error != Interop.Errors.ERROR_LOCK_VIOLATION)
                                throw Win32Marshal.GetExceptionForWin32Error(error);

                            spinWait.SpinOnce();
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

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static int GetSystemPageAllocationGranularity()
        {
            Interop.Kernel32.SYSTEM_INFO info;
            Interop.Kernel32.GetSystemInfo(out info);

            return (int)info.dwAllocationGranularity;
        }
    }
}
