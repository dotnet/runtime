// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
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
