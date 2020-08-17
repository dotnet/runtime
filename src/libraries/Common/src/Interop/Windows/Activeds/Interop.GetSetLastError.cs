// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Activeds
    {
        internal enum AdsLastError
        {
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
            ERROR_MORE_DATA = 234,
            ERROR_SUCCESS = 0
        }

        [DllImport(Libraries.Activeds, CharSet = CharSet.Unicode)]
        public static extern unsafe int ADsGetLastError(out int error, char* errorBuffer, int errorBufferLength, char* nameBuffer, int nameBufferLength);

        [DllImport(Libraries.Activeds, CharSet = CharSet.Unicode)]
        public static extern int ADsSetLastError(int error, string errorString, string provider);
    }
}
