// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.Xml;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class SubjectIdentifierOrKey
    {
        internal SubjectIdentifierOrKey(SubjectIdentifierOrKeyType type, object value)
        {
            Debug.Assert(value != null);
#if DEBUG
            switch (type)
            {
                case SubjectIdentifierOrKeyType.IssuerAndSerialNumber:
                    Debug.Assert(value is X509IssuerSerial);
                    break;

                case SubjectIdentifierOrKeyType.SubjectKeyIdentifier:
                    Debug.Assert(value is string);
                    break;

                case SubjectIdentifierOrKeyType.PublicKeyInfo:
                    Debug.Assert(value is PublicKeyInfo);
                    break;

                default:
                    Debug.Fail($"Illegal type passed to SubjectIdentifierOrKey: {type}");
                    break;
            }
#endif

            Type = type;
            Value = value;
        }

        public SubjectIdentifierOrKeyType Type { get; }

        public object Value { get; }
    }
}


