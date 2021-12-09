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
            t_lastError = Interop.Kernel32.GetLastError();
        }

        public static void ClearLastError()
        {
            Interop.Kernel32.SetLastError(0);
        }

        #region String marshalling

        public static unsafe int ConvertMultiByteToWideChar(byte* buffer, int ansiLength, char* pWChar, int uniLength)
        {
            return Interop.Kernel32.MultiByteToWideChar(Interop.Kernel32.CP_ACP, 0, buffer, ansiLength, pWChar, uniLength);
        }

        // Convert a UTF16 string to ANSI byte array
        public static unsafe int ConvertWideCharToMultiByte(char* wideCharStr, int wideCharLen, byte* multiByteStr, int multiByteLen)
        {
            return Interop.Kernel32.WideCharToMultiByte(Interop.Kernel32.CP_ACP,
                                                        0,
                                                        wideCharStr,
                                                        wideCharLen,
                                                        multiByteStr,
                                                        multiByteLen,
                                                        default(IntPtr),
                                                        default(IntPtr)
                                                        );
        }

        // Convert a UTF16 string to ANSI byte array using flags
        public static unsafe int ConvertWideCharToMultiByte(char* wideCharStr,
                                                            int wideCharLen,
                                                            byte* multiByteStr,
                                                            int multiByteLen,
                                                            bool bestFit,
                                                            bool throwOnUnmappableChar)
        {
            uint flags = (bestFit ? 0 : Interop.Kernel32.WC_NO_BEST_FIT_CHARS);
            int defaultCharUsed = 0;
            int ret = Interop.Kernel32.WideCharToMultiByte(Interop.Kernel32.CP_ACP,
                                                        flags,
                                                        wideCharStr,
                                                        wideCharLen,
                                                        multiByteStr,
                                                        multiByteLen,
                                                        default(IntPtr),
                                                        throwOnUnmappableChar ? new System.IntPtr(&defaultCharUsed) : default(IntPtr)
                                                        );
            if (defaultCharUsed != 0)
            {
                throw new ArgumentException(SR.Arg_InteropMarshalUnmappableChar);
            }

            return ret;
        }

        // Return size in bytes required to convert a UTF16 string to byte array.
        public static unsafe int GetByteCount(char* wStr, int wideStrLen)
        {
            return Interop.Kernel32.WideCharToMultiByte(Interop.Kernel32.CP_ACP,
                                                        0,
                                                        wStr,
                                                        wideStrLen,
                                                        default(byte*),
                                                        0,
                                                        default(IntPtr),
                                                        default(IntPtr)
                                                        );
        }

        // Return number of charaters encoded in native byte array lpMultiByteStr
        public static unsafe int GetCharCount(byte* multiByteStr, int multiByteLen)
        {
            return Interop.Kernel32.MultiByteToWideChar(Interop.Kernel32.CP_ACP, 0, multiByteStr, multiByteLen, default(char*), 0);
        }
        #endregion
    }
}
