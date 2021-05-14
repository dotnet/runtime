// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    /// <summary>
    /// Sha384Managed has a copy of the same implementation as SHA384
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsPlatformCryptoSupported))]
    public class Sha384ManagedTests : Sha384Tests
    {
        protected override HashAlgorithm Create()
        {
            return new SHA384Managed();
        }
    }
}
