// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_STRING
        {
            internal LSA_STRING(IntPtr pBuffer, ushort length)
            {
                Length = length;
                MaximumLength = length;
                Buffer = pBuffer;
            }

            /// <summary>
            /// Specifies the length, in bytes, of the string in Buffer. This value does not include the terminating null character, if any.
            /// </summary>
            internal ushort Length;

            /// <summary>
            /// Specifies the total size, in bytes, of Buffer. Up to MaximumLength bytes may be written into the buffer without trampling memory.
            /// </summary>
            internal ushort MaximumLength;

            /// <summary>
            /// Pointer to an array of characters. Note that strings returned by the LSA may not be null-terminated.
            /// </summary>
            internal IntPtr Buffer;
        }
    }
}
