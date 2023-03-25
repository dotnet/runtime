// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed partial class Shake128 : IDisposable
    {
        private static partial LiteXof CreateHashProvider()
        {
            // This should have been checked before.
            Debug.Fail("Unsupported algorithm SHAKE128");
            throw new UnreachableException();
        }

        private static partial bool GetIsSupported() => false;
    }
}
