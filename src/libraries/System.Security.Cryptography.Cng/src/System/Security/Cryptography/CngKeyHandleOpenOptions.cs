// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Parameter to CngKey.Open(SafeNCryptKeyHandle,...)
    /// </summary>

    //
    // Note: This is not a mapping of a native NCrypt value.
    //
    [Flags]
    public enum CngKeyHandleOpenOptions
    {
        None = 0,
        EphemeralKey = 1,
    }
}
