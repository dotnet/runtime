// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal static string CertNameToStr([In] ref DATA_BLOB pName, CertNameStrTypeAndFlags dwStrType)
        {
            int nc = CertNameToStr(MsgEncodingType.All, ref pName, dwStrType, null, 0);
            if (nc <= 1) // The API actually return 1 when it fails; which is not what the documentation says.
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            StringBuilder name = new StringBuilder(nc);
            nc = CertNameToStr(MsgEncodingType.All, ref pName, dwStrType, name, nc);
            if (nc <= 1) // The API actually return 1 when it fails; which is not what the documentation says.
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return name.ToString();
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertNameToStrW")]
        internal static extern int CertNameToStr(MsgEncodingType dwCertEncodingType, [In] ref DATA_BLOB pName, CertNameStrTypeAndFlags dwStrType, StringBuilder psz, int csz);
    }
}

