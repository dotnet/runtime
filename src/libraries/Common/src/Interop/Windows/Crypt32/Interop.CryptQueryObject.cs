// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial bool CryptQueryObject(
            CertQueryObjectType dwObjectType,
            void* pvObject,
            ExpectedContentTypeFlags dwExpectedContentTypeFlags,
            ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
            int dwFlags, // reserved - always pass 0
            out CertEncodingType pdwMsgAndCertEncodingType,
            out ContentType pdwContentType,
            out FormatType pdwFormatType,
            out SafeCertStoreHandle phCertStore,
            out SafeCryptMsgHandle phMsg,
            out SafeCertContextHandle ppvContext
            );

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial bool CryptQueryObject(
            CertQueryObjectType dwObjectType,
            void* pvObject,
            ExpectedContentTypeFlags dwExpectedContentTypeFlags,
            ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
            int dwFlags, // reserved - always pass 0
            IntPtr pdwMsgAndCertEncodingType,
            out ContentType pdwContentType,
            IntPtr pdwFormatType,
            IntPtr phCertStore,
            IntPtr phMsg,
            IntPtr ppvContext
            );

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial bool CryptQueryObject(
            CertQueryObjectType dwObjectType,
            void* pvObject,
            ExpectedContentTypeFlags dwExpectedContentTypeFlags,
            ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
            int dwFlags, // reserved - always pass 0
            IntPtr pdwMsgAndCertEncodingType,
            out ContentType pdwContentType,
            IntPtr pdwFormatType,
            out SafeCertStoreHandle phCertStore,
            IntPtr phMsg,
            IntPtr ppvContext
            );
    }
}
