// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    /// <summary>
    /// Sha1Managed has a copy of the same implementation as SHA1
    /// </summary>
    public class Sha1ManagedTests : Sha1Tests
    {
        protected override HashAlgorithm Create()
        {
            return new SHA1Managed();
        }
    }
}
