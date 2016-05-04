// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using X509IssuerSerial = System.Security.Cryptography.Xml.X509IssuerSerial;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class SubjectIdentifier
    {
        internal SubjectIdentifier(SubjectIdentifierType type, object value)
        {
            Debug.Assert(value != null);
#if DEBUG
            switch (type)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    Debug.Assert(value is X509IssuerSerial);
                    break;

                case SubjectIdentifierType.SubjectKeyIdentifier:
                    Debug.Assert(value is string);
                    break;

                default:
                    Debug.Fail($"Illegal type passed to SubjectIdentifier: {type}");
                    break;
            }
#endif //DEBUG
            Type = type;
            Value = value;
        }

        public SubjectIdentifierType Type { get; }
        public object Value { get; }
    }
}


