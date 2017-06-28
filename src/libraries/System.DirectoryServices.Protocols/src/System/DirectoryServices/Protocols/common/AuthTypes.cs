// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.DirectoryServices.Protocols
{
    public enum AuthType
    {
        Anonymous = 0,
        Basic = 1,
        Negotiate = 2,
        Ntlm = 3,
        Digest = 4,
        Sicily = 5,
        Dpa = 6,
        Msn = 7,
        External = 8,
        Kerberos = 9
    }

    public enum PartialResultProcessing
    {
        NoPartialResultSupport,
        ReturnPartialResults,
        ReturnPartialResultsAndNotifyCallback
    }
}
