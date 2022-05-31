// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        private const int LoadWithAlteredSearchPathFlag = 0x8; /* LOAD_WITH_ALTERED_SEARCH_PATH */

        private static IntPtr LoadLibraryHelper(string libraryName, int flags, ref LoadLibErrorTracker errorTracker)
        {
            IntPtr hmod;

            if (((uint)flags & 0xFFFFFF00) != 0)
            {
                hmod = Interop.Kernel32.LoadLibraryEx(libraryName, IntPtr.Zero, (int)((uint)flags & 0xFFFFFF00));
                if (hmod != IntPtr.Zero)
                {
                    return hmod;
                }

                int lastError = Marshal.GetLastWin32Error();
                if (lastError != Interop.Errors.ERROR_INVALID_PARAMETER)
                {
                    errorTracker.TrackErrorCode(lastError);
                    return hmod;
                }
            }

            hmod = Interop.Kernel32.LoadLibraryEx(libraryName, IntPtr.Zero, flags & 0xFF);
            if (hmod == IntPtr.Zero)
            {
                errorTracker.TrackErrorCode(Marshal.GetLastWin32Error());
            }

            return hmod;
        }

        private static void FreeLib(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            bool result = Interop.Kernel32.FreeLibrary(handle);
            if (!result)
                throw new InvalidOperationException();
        }

        private static unsafe IntPtr GetSymbolOrNull(IntPtr handle, string symbolName)
        {
            return Interop.Kernel32.GetProcAddress(handle, symbolName);
        }

        // Preserving good error info from DllImport-driven LoadLibrary is tricky because we keep loading from different places
        // if earlier loads fail and those later loads obliterate error codes.
        //
        // This tracker object will keep track of the error code in accordance to priority:
        //
        //   low-priority:      unknown error code (should never happen)
        //   medium-priority:   dll not found
        //   high-priority:     dll found but error during loading
        //
        // We will overwrite the previous load's error code only if the new error code is higher priority.
        internal struct LoadLibErrorTracker
        {
            private int _errorCode;
            private int _priority;

            private const int PriorityNotFound = 10;
            private const int PriorityAccessDenied = 20;
            private const int PriorityCouldNotLoad = 99999;

            public void Throw(string libraryName)
            {
                if (_errorCode == Interop.Errors.ERROR_BAD_EXE_FORMAT)
                {
                    throw new BadImageFormatException();
                }

                string message = Interop.Kernel32.GetMessage(_errorCode);
                throw new DllNotFoundException(SR.Format(SR.DllNotFound_Windows, libraryName, message));
            }

            public void TrackErrorCode(int errorCode)
            {
                int priority = errorCode switch
                {
                    Interop.Errors.ERROR_FILE_NOT_FOUND or
                    Interop.Errors.ERROR_PATH_NOT_FOUND or
                    Interop.Errors.ERROR_MOD_NOT_FOUND or
                    Interop.Errors.ERROR_DLL_NOT_FOUND => PriorityNotFound,

                    // If we can't access a location, we can't know if the dll's there or if it's good.
                    // Still, this is probably more unusual (and thus of more interest) than a dll-not-found
                    // so give it an intermediate priority.
                    Interop.Errors.ERROR_ACCESS_DENIED => PriorityAccessDenied,

                    // Assume all others are "dll found but couldn't load."
                    _ => PriorityCouldNotLoad,
                };

                if (priority > _priority)
                {
                    _errorCode = errorCode;
                    _priority = priority;
                }
            }
        }
    }
}
