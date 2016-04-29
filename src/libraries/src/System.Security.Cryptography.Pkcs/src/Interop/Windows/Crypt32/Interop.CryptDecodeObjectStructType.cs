// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CryptDecodeObjectStructType
        {
            X509_EXTENSIONS = 5,
            X509_NAME_VALUE = 6,
            X509_NAME = 7,
            X509_AUTHORITY_KEY_ID = 9,
            X509_KEY_USAGE_RESTRICTION = 11,
            X509_BASIC_CONSTRAINTS = 13,
            X509_KEY_USAGE = 14,
            X509_BASIC_CONSTRAINTS2 = 15,
            X509_CERT_POLICIES = 16,
            PKCS_UTC_TIME = 17,
            PKCS_ATTRIBUTE = 22,
            X509_UNICODE_NAME_VALUE = 24,
            X509_OCTET_STRING = 25,
            X509_BITS = 26,
            X509_ANY_STRING = X509_NAME_VALUE,
            X509_UNICODE_ANY_STRING = X509_UNICODE_NAME_VALUE,
            X509_ENHANCED_KEY_USAGE = 36,
            PKCS_RC2_CBC_PARAMETERS = 41,
            X509_CERTIFICATE_TEMPLATE = 64,
            X509_OBJECT_IDENTIFIER = 73,
            PKCS7_SIGNER_INFO = 500,
            CMS_SIGNER_INFO = 501,
        }
    }
}

