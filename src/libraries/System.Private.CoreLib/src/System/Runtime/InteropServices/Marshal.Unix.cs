// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        private static int GetSystemMaxDBCSCharSize() => 3;

        private static bool IsWin32Atom(IntPtr ptr) => false;

        internal static unsafe int StringToAnsiString(string s, byte* buffer, int bufferLength, bool bestFit = false, bool throwOnUnmappableChar = false)
        {
            Debug.Assert(bufferLength >= (s.Length + 1) * SystemMaxDBCSCharSize, "Insufficient buffer length passed to StringToAnsiString");

            int convertedBytes;

            fixed (char* pChar = s)
            {
                convertedBytes = Encoding.UTF8.GetBytes(pChar, s.Length, buffer, bufferLength);
            }

            buffer[convertedBytes] = 0;

            return convertedBytes;
        }
    }
}