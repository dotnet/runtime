// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        /// <summary>
        /// Used by the 2 Create factory method groups.  A -1 fileHandle specifies that the 
        /// memory mapped file should not be associated with an exsiting file on disk (ie start
        /// out empty).
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle CreateCore(
            SafeFileHandle fileHandle, String mapName, HandleInheritability inheritability,
            MemoryMappedFileAccess access, MemoryMappedFileOptions options, Int64 capacity)
        {
            SafeMemoryMappedFileHandle handle = null;
            Interop.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(inheritability);

            // split the long into two ints
            int capacityLow = unchecked((int)(capacity & 0x00000000FFFFFFFFL));
            int capacityHigh = unchecked((int)(capacity >> 32));

            handle = Interop.mincore.CreateFileMapping(fileHandle, ref secAttrs, GetPageAccess(access) | (int)options,
                capacityHigh, capacityLow, mapName);

            Int32 errorCode = Marshal.GetLastWin32Error();
            if (!handle.IsInvalid && errorCode == Interop.ERROR_ALREADY_EXISTS)
            {
                handle.Dispose();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
            else if (handle.IsInvalid)
            {
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            return handle;
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        private static SafeMemoryMappedFileHandle OpenCore(
            String mapName, HandleInheritability inheritability, MemoryMappedFileAccess access, bool createOrOpen)
        {
            return OpenCore(mapName, inheritability, GetFileMapAccess(access), createOrOpen);
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        private static SafeMemoryMappedFileHandle OpenCore(
            String mapName, HandleInheritability inheritability, MemoryMappedFileRights rights, bool createOrOpen)
        {
            return OpenCore(mapName, inheritability, GetFileMapAccess(rights), createOrOpen);
        }

        /// <summary>
        /// Used by the CreateOrOpen factory method groups.  A -1 fileHandle specifies that the 
        /// memory mapped file should not be associated with an existing file on disk (ie start
        /// out empty).
        ///
        /// Try to open the file if it exists -- this requires a bit more work. Loop until we can
        /// either create or open a memory mapped file up to a timeout. CreateFileMapping may fail
        /// if the file exists and we have non-null security attributes, in which case we need to
        /// use OpenFileMapping.  But, there exists a race condition because the memory mapped file
        /// may have closed inbetween the two calls -- hence the loop. 
        /// 
        /// This uses similar retry/timeout logic as in performance counter. It increases the wait
        /// time each pass through the loop and times out in approximately 1.4 minutes. If after 
        /// retrying, a MMF handle still hasn't been opened, throw an InvalidOperationException.
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle CreateOrOpenCore(SafeFileHandle fileHandle, String mapName,
                                                                HandleInheritability inheritability,
                                                                MemoryMappedFileAccess access, MemoryMappedFileOptions options,
                                                                Int64 capacity)
        {
            Debug.Assert(access != MemoryMappedFileAccess.Write, "Callers requesting write access shouldn't try to create a mmf");

            SafeMemoryMappedFileHandle handle = null;
            Interop.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(inheritability);

            // split the long into two ints
            Int32 capacityLow = unchecked((Int32)(capacity & 0x00000000FFFFFFFFL));
            Int32 capacityHigh = unchecked((Int32)(capacity >> 32));

            int waitRetries = 14;   //((2^13)-1)*10ms == approximately 1.4mins
            int waitSleep = 0;

            // keep looping until we've exhausted retries or break as soon we we get valid handle
            while (waitRetries > 0)
            {
                // try to create
                handle = Interop.mincore.CreateFileMapping(fileHandle, ref secAttrs,
                    GetPageAccess(access) | (int)options, capacityHigh, capacityLow, mapName);

                Int32 createErrorCode = Marshal.GetLastWin32Error();
                if (!handle.IsInvalid)
                {
                    break;
                }
                else
                {
                    if (createErrorCode != Interop.ERROR_ACCESS_DENIED)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(createErrorCode);
                    }

                    // the mapname exists but our ACL is preventing us from opening it with CreateFileMapping.  
                    // Let's try to open it with OpenFileMapping.
                    handle.SetHandleAsInvalid();
                }

                // try to open
                handle = Interop.mincore.OpenFileMapping(GetFileMapAccess(access), (inheritability &
                        HandleInheritability.Inheritable) != 0, mapName);

                Int32 openErrorCode = Marshal.GetLastWin32Error();

                // valid handle
                if (!handle.IsInvalid)
                {
                    break;
                }
                // didn't get valid handle; have to retry
                else
                {
                    if (openErrorCode != Interop.ERROR_FILE_NOT_FOUND)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(openErrorCode);
                    }

                    // increase wait time
                    --waitRetries;
                    if (waitSleep == 0)
                    {
                        waitSleep = 10;
                    }
                    else
                    {
                        Task.Delay(waitSleep).Wait();
                        waitSleep *= 2;
                    }
                }
            }

            // finished retrying but couldn't create or open
            if (handle == null || handle.IsInvalid)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CantCreateFileMapping);
            }

            return handle;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>
        /// This converts a MemoryMappedFileRights to its corresponding native FILE_MAP_XXX value to be used when 
        /// creating new views.  
        /// </summary>
        private static Int32 GetFileMapAccess(MemoryMappedFileRights rights)
        {
            return (int)rights;
        }

        /// <summary>
        /// This converts a MemoryMappedFileAccess to its corresponding native FILE_MAP_XXX value to be used when 
        /// creating new views.  
        /// </summary>
        internal static Int32 GetFileMapAccess(MemoryMappedFileAccess access)
        {
            switch (access)
            {
                case MemoryMappedFileAccess.Read: return Interop.FILE_MAP_READ;
                case MemoryMappedFileAccess.Write: return Interop.FILE_MAP_WRITE;
                case MemoryMappedFileAccess.ReadWrite: return Interop.FILE_MAP_READ | Interop.FILE_MAP_WRITE;
                case MemoryMappedFileAccess.CopyOnWrite: return Interop.FILE_MAP_COPY;
                case MemoryMappedFileAccess.ReadExecute: return Interop.FILE_MAP_EXECUTE | Interop.FILE_MAP_READ;
                case MemoryMappedFileAccess.ReadWriteExecute: return Interop.FILE_MAP_EXECUTE | Interop.FILE_MAP_READ | Interop.FILE_MAP_WRITE;
                default: throw new ArgumentOutOfRangeException("access");
            }
        }

        /// <summary>
        /// This converts a MemoryMappedFileAccess to it's corresponding native PAGE_XXX value to be used by the 
        /// factory methods that construct a new memory mapped file object. MemoryMappedFileAccess.Write is not 
        /// valid here since there is no corresponding PAGE_XXX value.
        /// </summary>
        internal static Int32 GetPageAccess(MemoryMappedFileAccess access)
        {
            switch (access)
            {
                case MemoryMappedFileAccess.Read: return Interop.PAGE_READONLY;
                case MemoryMappedFileAccess.ReadWrite: return Interop.PAGE_READWRITE;
                case MemoryMappedFileAccess.CopyOnWrite: return Interop.PAGE_WRITECOPY;
                case MemoryMappedFileAccess.ReadExecute: return Interop.PAGE_EXECUTE_READ;
                case MemoryMappedFileAccess.ReadWriteExecute: return Interop.PAGE_EXECUTE_READWRITE;
                default: throw new ArgumentOutOfRangeException("access");
            }
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle OpenCore(
            String mapName, HandleInheritability inheritability, int desiredAccessRights, bool createOrOpen)
        {
            SafeMemoryMappedFileHandle handle = Interop.mincore.OpenFileMapping(
                desiredAccessRights, (inheritability & HandleInheritability.Inheritable) != 0, mapName);
            Int32 lastError = Marshal.GetLastWin32Error();

            if (handle.IsInvalid)
            {
                if (createOrOpen && (lastError == Interop.ERROR_FILE_NOT_FOUND))
                {
                    throw new ArgumentException(SR.Argument_NewMMFWriteAccessNotAllowed, "access");
                }
                else
                {
                    throw Win32Marshal.GetExceptionForWin32Error(lastError);
                }
            }
            return handle;
        }

        /// <summary>
        /// Helper method used to extract the native binary security descriptor from the MemoryMappedFileSecurity
        /// type. If pinningHandle is not null, caller must free it AFTER the call to CreateFile has returned.
        /// </summary>
        [SecurityCritical]
        private unsafe static Interop.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
        {
            Interop.SECURITY_ATTRIBUTES secAttrs = default(Interop.SECURITY_ATTRIBUTES);
            if ((inheritability & HandleInheritability.Inheritable) != 0)
            {
                secAttrs = new Interop.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (uint)Marshal.SizeOf(secAttrs);
                secAttrs.bInheritHandle = true;
            }
            return secAttrs;
        }
    }
}
