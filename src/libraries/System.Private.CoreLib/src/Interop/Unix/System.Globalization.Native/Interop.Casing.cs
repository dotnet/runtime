// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ChangeCase")]
        internal static extern unsafe void ChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ChangeCaseInvariant")]
        internal static extern unsafe void ChangeCaseInvariant(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ChangeCaseTurkish")]
        internal static extern unsafe void ChangeCaseTurkish(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);
    }
}
