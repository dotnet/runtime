// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

