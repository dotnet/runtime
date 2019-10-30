// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    public enum KeyNumber
    {
        //These are identifiers for the private keys from the key container
        Exchange = 1,
        Signature = 2
    }
}
