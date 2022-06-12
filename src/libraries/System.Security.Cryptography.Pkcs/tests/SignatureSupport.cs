// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public class SignatureSupport
    {
        public static bool SupportsRsaSha1Signatures { get; } =
            System.Security.Cryptography.Tests.SignatureSupport.CanProduceSha1Signature(RSA.Create());
    }
}
