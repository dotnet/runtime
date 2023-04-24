// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// This PInvokeMarshal class should provide full public Marshal
    /// implementation for all things related to P/Invoke marshalling
    /// </summary>
    public partial class PInvokeMarshal
    {
        public static void SaveLastError()
        {
            t_lastError = Interop.Sys.GetErrNo();
        }

        public static void ClearLastError()
        {
            Interop.Sys.SetErrNo(0);
        }

        #region String marshalling

        public static unsafe int ConvertMultiByteToWideChar(byte* multiByteStr,
                                                            int multiByteLen,
                                                            char* wideCharStr,
                                                            int wideCharLen)
        {
            return System.Text.Encoding.UTF8.GetChars(multiByteStr, multiByteLen, wideCharStr, wideCharLen);
        }

        public static unsafe int ConvertWideCharToMultiByte(char* wideCharStr,
                                                            int wideCharLen,
                                                            byte* multiByteStr,
                                                            int multiByteLen,
                                                            bool bestFit,
                                                            bool throwOnUnmappableChar)
        {
            return System.Text.Encoding.UTF8.GetBytes(wideCharStr, wideCharLen, multiByteStr, multiByteLen);
        }

        public static unsafe int ConvertWideCharToMultiByte(char* wideCharStr,
                                                            int wideCharLen,
                                                            byte* multiByteStr,
                                                            int multiByteLen)
        {
            return System.Text.Encoding.UTF8.GetBytes(wideCharStr, wideCharLen, multiByteStr, multiByteLen);
        }

        public static unsafe int GetByteCount(char* wideCharStr, int wideCharLen)
        {
            return System.Text.Encoding.UTF8.GetByteCount(wideCharStr, wideCharLen);
        }

        public static unsafe int GetCharCount(byte* multiByteStr, int multiByteLen)
        {
            return System.Text.Encoding.UTF8.GetCharCount(multiByteStr, multiByteLen);
        }
        #endregion
    }
}
