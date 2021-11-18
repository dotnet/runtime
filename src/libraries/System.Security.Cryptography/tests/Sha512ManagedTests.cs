// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Sha512Managed has a copy of the same implementation as SHA512
    /// </summary>
    public class Sha512ManagedTests : Sha512Tests
    {
        protected override HashAlgorithm Create()
        {
            return new SHA512Managed();
        }
    }
}
