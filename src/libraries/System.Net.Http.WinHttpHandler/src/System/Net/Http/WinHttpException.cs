// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace System.Net.Http
{
    internal class WinHttpException : Win32Exception
    {
        public WinHttpException(int error, string message) : base(error, message)
        {
            this.HResult = ConvertErrorCodeToHR(error);
        }

        public WinHttpException(int error, string message, Exception innerException) : base(message, innerException)
        {
            this.HResult = ConvertErrorCodeToHR(error);
        }

        public static int ConvertErrorCodeToHR(int error)
        {
            // This method allows common error detection code to be used by consumers
            // of HttpClient. This method converts the ErrorCode returned by WinHTTP
            // to the same HRESULT value as is provided in the .NET Native implementation
            // of HttpClient under the same error conditions. Clients would access
            // HttpRequestException.InnerException.HRESULT to discover what caused
            // the exception.
            switch (unchecked((uint)error))
            {
                case Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR:
                    return unchecked((int)Interop.WinHttp.WININET_E_CONNECTION_RESET);
                default:
                    // Marshal.GetHRForLastWin32Error can't be used as not all error codes originate from native
                    // code.
                    return Interop.HRESULT_FROM_WIN32(error);
            }
        }

        public static void ThrowExceptionUsingLastError(string nameOfCalledFunction)
        {
            throw CreateExceptionUsingLastError(nameOfCalledFunction);
        }

        public static WinHttpException CreateExceptionUsingLastError(string nameOfCalledFunction)
        {
            int lastError = Marshal.GetLastWin32Error();
            return CreateExceptionUsingError(lastError, nameOfCalledFunction);
        }

        public static WinHttpException CreateExceptionUsingError(int error, string nameOfCalledFunction)
        {
            var e = new WinHttpException(error, GetErrorMessage(error, nameOfCalledFunction));
            ExceptionStackTrace.AddCurrentStack(e);
            return e;
        }

        public static WinHttpException CreateExceptionUsingError(int error, string nameOfCalledFunction, Exception innerException)
        {
            var e = new WinHttpException(error, GetErrorMessage(error, nameOfCalledFunction), innerException);
            ExceptionStackTrace.AddCurrentStack(e);
            return e;
        }

        public static string GetErrorMessage(int error, string nameOfCalledFunction)
        {
            Debug.Assert(!string.IsNullOrEmpty(nameOfCalledFunction));

            // Look up specific error message in WINHTTP.DLL since it is not listed in default system resources
            // and thus can't be found by default .NET interop.
            IntPtr moduleHandle = Interop.Kernel32.GetModuleHandle(Interop.Libraries.WinHttp);
            string httpError = Interop.Kernel32.GetMessage(error, moduleHandle);

            return SR.Format(SR.net_http_winhttp_error, error, nameOfCalledFunction, httpError);
        }
    }
}
