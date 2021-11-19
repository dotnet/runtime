// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Credui
    {
        // These contants were taken from the wincred.h file
        internal const int CRED_MAX_USERNAME_LENGTH = 514;
        internal const int CRED_MAX_DOMAIN_TARGET_LENGTH = 338;

        [GeneratedDllImport(Libraries.Credui, EntryPoint = "CredUIParseUserNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static unsafe partial int CredUIParseUserName(
            string pszUserName,
            char* pszUser,
            uint ulUserMaxChars,
            char* pszDomain,
            uint ulDomainMaxChars);
    }
}
