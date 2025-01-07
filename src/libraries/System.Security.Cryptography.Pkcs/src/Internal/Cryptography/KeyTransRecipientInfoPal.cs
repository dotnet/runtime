// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography.Pkcs;
using System.Text;

namespace Internal.Cryptography
{
    internal abstract class KeyTransRecipientInfoPal : RecipientInfoPal
    {
        internal KeyTransRecipientInfoPal()
            : base()
        {
        }
    }
}
