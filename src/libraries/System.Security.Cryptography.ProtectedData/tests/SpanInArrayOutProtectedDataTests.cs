// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.ProtectedDataTests
{
    public class SpanInArrayOutProtectedDataTests : ProtectedDataTests
    {
        protected override byte[] Protect(byte[] plain, byte[]? entropy, DataProtectionScope scope)
        {
            ReadOnlySpan<byte> inputSpan = plain;
            ReadOnlySpan<byte> entropySpan = entropy;
            return ProtectedData.Protect(inputSpan, scope, entropySpan);
        }

        protected override byte[] Unprotect(byte[] encrypted, byte[]? entropy, DataProtectionScope scope)
        {
            ReadOnlySpan<byte> inputSpan = encrypted;
            ReadOnlySpan<byte> entropySpan = entropy;
            return ProtectedData.Unprotect(inputSpan, scope, entropySpan);
        }
    }
}
