// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Sha256Managed has a copy of the same implementation as SHA256
    /// </summary>
    public class Sha256ManagedTests : Sha256Tests
    {
        protected override HashAlgorithm Create()
        {
            return new SHA256Managed();
        }
    }
}
