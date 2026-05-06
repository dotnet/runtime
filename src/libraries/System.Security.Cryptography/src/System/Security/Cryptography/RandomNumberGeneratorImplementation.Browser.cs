// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class RandomNumberGeneratorImplementation
    {
        private static unsafe void GetBytes(byte* pbBuffer, int count)
        {
            Debug.Assert(count > 0);

            Interop.GetCryptographicallySecureRandomBytes(pbBuffer, count);
        }
    }
}
