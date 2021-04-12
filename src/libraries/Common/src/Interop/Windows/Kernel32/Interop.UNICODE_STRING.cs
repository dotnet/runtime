// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa380518.aspx
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff564879.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            internal char* Buffer;

            internal unsafe UNICODE_STRING(char* buffer, int lengthInChars)
            {
                Length = checked((ushort)(lengthInChars * sizeof(char)));
                MaximumLength = Length;
                Buffer = buffer;
            }
        }
    }
}
