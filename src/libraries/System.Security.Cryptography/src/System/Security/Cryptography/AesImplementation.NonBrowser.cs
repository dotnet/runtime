// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation
    {
        internal static readonly KeySizes[] s_legalKeySizes = { new KeySizes(128, 256, 64) };
    }
}
