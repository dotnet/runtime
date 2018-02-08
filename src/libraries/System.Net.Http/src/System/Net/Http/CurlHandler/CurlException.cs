// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net.Http
{
    internal sealed class CurlException : Exception
    {
        internal CurlException(int error, string message) : base(message)
        {
            HResult = error;
        }

        internal CurlException(int error, Exception innerException) : base(GetCurlErrorString(error, isMulti:false), innerException)
        {
            HResult = error;
        }

        internal CurlException(int error, bool isMulti) : this(error, GetCurlErrorString(error, isMulti))
        {
        }

        internal static string GetCurlErrorString(int code, bool isMulti)
        {
            IntPtr ptr = isMulti ? Interop.Http.MultiGetErrorString(code) : Interop.Http.EasyGetErrorString(code);
            return Marshal.PtrToStringAnsi(ptr);
        }
    }
}
