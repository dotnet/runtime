// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Http
{
    internal class WinHttpException : Win32Exception
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int HRESULT_FROM_WIN32(int errorCode)
        {
            if ((errorCode & 0x80000000) == 0x80000000)
            {
                return errorCode;
            }
            else
            {
                return (errorCode & 0x0000FFFF) | unchecked((int)0x80070000);
            }
        }
    
        public WinHttpException(int error, string message) : base(error, message)
        {
            HResult = ConvertErrorCodeToHR(error);
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
                    // Marshal.GetHRForLastWin32Error can't be used as not all error codes 
                    // passed into this method originate from calling Win32 native functions.
                    return HRESULT_FROM_WIN32(error);
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
            // and thus can't be found by default .NET interop.
            IntPtr moduleHandle = Interop.mincore.GetModuleHandle(Interop.Libraries.WinHttp);
            
            return Interop.mincore.GetMessage(moduleHandle, error);
        }
    }
}
