// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Net.Http
{
    internal class WinHttpException : Win32Exception
    {
        public WinHttpException(int error, string message) : base(error, message)
        {
            this.HResult = ConvertErrorCodeToHR(error);
        }

        public static int ConvertErrorCodeToHR(int error)
        {
            // This method allows common error detection code to be used by consumers
            // of HttpClient. This method converts the ErrorCode returned by WinHTTP
            // to the same HRESULT value as is provided in the .Net Native implementation
            // of HttpClient under the same error conditions. Clients would access
            // HttpRequestException.InnerException.HRESULT to discover what caused
            // the exception.
            switch ((uint)error)
            {
                case Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR:
                    return unchecked((int)Interop.WinHttp.WININET_E_CONNECTION_RESET);
                default:
                    // Marshal.GetHRForLastWin32Error can't be used as not all error codes originate from native
                    // code.
                    return Interop.HRESULT_FROM_WIN32(error);
            }
        }

        public static void ThrowExceptionUsingLastError()
        {
            throw CreateExceptionUsingLastError();
        }

        public static WinHttpException CreateExceptionUsingLastError()
        {
            int lastError = Marshal.GetLastWin32Error();
            return CreateExceptionUsingError(lastError);
        }

        public static WinHttpException CreateExceptionUsingError(int error)
        {
            return new WinHttpException(error, GetErrorMessage(error));
        }

        public static string GetErrorMessage(int error)
        {
            // Look up specific error message in WINHTTP.DLL since it is not listed in default system resources
            // and thus can't be found by default .Net interop.
            IntPtr moduleHandle = Interop.Kernel32.GetModuleHandle(Interop.Libraries.WinHttp);
            return Interop.Kernel32.GetMessage(moduleHandle, error);
        }
    }
}
