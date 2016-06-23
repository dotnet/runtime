// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates.Tests
{
    /// <summary>
    /// A type to extend the Dispose on X509Chain to also dispose all of the X509Certificate objects
    /// in the ChainElements structure.
    /// </summary>
    internal sealed class ChainHolder : IDisposable
    {
        public X509Chain Chain { get; } = new X509Chain();

        public void Dispose()
        {
            DisposeChainElements();

            Chain.Dispose();
        }

        public void DisposeChainElements()
        {
            int count = Chain.ChainElements.Count;

            for (int i = 0; i < count; i++)
            {
                Chain.ChainElements[i].Certificate.Dispose();
            }
        }
    }
}
