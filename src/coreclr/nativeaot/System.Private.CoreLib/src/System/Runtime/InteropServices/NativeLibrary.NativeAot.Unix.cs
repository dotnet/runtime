// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        private const int LoadWithAlteredSearchPathFlag = 0;

        private static IntPtr LoadLibraryHelper(string libraryName, int flags, ref LoadLibErrorTracker errorTracker)
        {
            // do the Dos/Unix conversion
            libraryName = libraryName.Replace('\\', '/');

            IntPtr ret = Interop.Sys.LoadLibrary(libraryName);
            if (ret == IntPtr.Zero)
            {
                string? message = Marshal.PtrToStringAnsi(Interop.Sys.GetLoadLibraryError());
                errorTracker.TrackErrorMessage(message);
            }

            return ret;
        }

        private static void FreeLib(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            Interop.Sys.FreeLibrary(handle);
        }

        private static unsafe IntPtr GetSymbolOrNull(IntPtr handle, string symbolName)
        {
            return Interop.Sys.GetProcAddress(handle, symbolName);
        }

        internal struct LoadLibErrorTracker
        {
            private string? _errorMessage;

            public void Throw(string libraryName)
            {
#if TARGET_OSX
                throw new DllNotFoundException(SR.Format(SR.DllNotFound_Mac, libraryName, _errorMessage));
#else
                throw new DllNotFoundException(SR.Format(SR.DllNotFound_Linux, libraryName, _errorMessage));
#endif
            }

            public void TrackErrorMessage(string? message)
            {
                _errorMessage = message;
            }
        }
    }
}
