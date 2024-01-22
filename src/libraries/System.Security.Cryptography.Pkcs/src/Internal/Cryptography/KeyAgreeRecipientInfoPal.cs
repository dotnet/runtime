// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;

namespace Internal.Cryptography
{
    internal abstract class KeyAgreeRecipientInfoPal : RecipientInfoPal
    {
        internal KeyAgreeRecipientInfoPal()
            : base()
        {
        }

        public abstract DateTime Date { get; }
        public abstract SubjectIdentifierOrKey OriginatorIdentifierOrKey { get; }
        public abstract CryptographicAttributeObject? OtherKeyAttribute { get; }
    }
}
