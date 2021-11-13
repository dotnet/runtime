// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CertNameToStrW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial int CertNameToStr(
            int dwCertEncodingType,
            void* pName,
            int dwStrType,
            char* psz,
            int csz);
    }
}
