// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
