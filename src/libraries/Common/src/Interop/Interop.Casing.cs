// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCase", CharSet = CharSet.Unicode)]
        internal static unsafe partial void ChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCaseInvariant", CharSet = CharSet.Unicode)]
        internal static unsafe partial void ChangeCaseInvariant(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCaseTurkish", CharSet = CharSet.Unicode)]
        internal static unsafe partial void ChangeCaseTurkish(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_InitOrdinalCasingPage", CharSet = CharSet.Unicode)]
        internal static unsafe partial void InitOrdinalCasingPage(int pageNumber, char* pTarget);
    }
}
