// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal const string BCRYPT_CHAIN_MODE_CBC = "ChainingModeCBC";
        internal const string BCRYPT_CHAIN_MODE_ECB = "ChainingModeECB";
        internal const string BCRYPT_CHAIN_MODE_CFB = "ChainingModeCFB";
    }
}
