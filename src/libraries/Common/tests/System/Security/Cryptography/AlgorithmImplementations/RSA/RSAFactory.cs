// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Rsa.Tests
{
    public interface IRSAProvider
    {
        RSA Create();
        RSA Create(int keySize);
        bool Supports384PrivateKey { get; }
        bool SupportsLargeExponent { get; }
        bool SupportsSha2Oaep { get; }
        bool SupportsPss { get; }
        bool SupportsSha1Signatures { get; }
        bool SupportsMd5Signatures { get; }
        bool SupportsSha3 { get; }
    }
}
