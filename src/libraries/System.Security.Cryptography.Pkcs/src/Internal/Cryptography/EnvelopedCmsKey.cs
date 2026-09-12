// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal union EnvelopedCmsKey(
        AsymmetricAlgorithm,
#if NET11_0_OR_GREATER
        MLKem,
        CompositeMLKem,
#endif
        EnvelopedCmsKey.None)
    {
        internal sealed record None
        {
            internal static None Instance { get; } = new None();

            private None()
            {
            }
        }
    }
}
