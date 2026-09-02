// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Provides a Cryptography Next Generation (CNG) implementation of the Composite Module-Lattice-Based Key-Encapsulation
    ///   Mechanism (Composite ML-KEM) algorithm.
    /// </summary>
    /// <remarks>
    ///   Developers are encouraged to program against the <see cref="CompositeMLKem" /> base class,
    ///   rather than any specific derived class. The derived classes are intended for interop with the underlying
    ///   system cryptographic libraries.
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed partial class CompositeMLKemCng : CompositeMLKem
    {
        private CngKey _key;

        /// <summary>
        ///   Initializes a new instance of the <see cref="CompositeMLKemCng"/> class by using the specified
        ///   <see cref="CngKey"/>.
        /// </summary>
        /// <param name="key">
        ///   The key that will be used as input to the cryptographic operations performed by the current object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="key"/> does not specify a Composite ML-KEM algorithm group.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Cryptography Next Generation (CNG) classes are not supported on this system.
        /// </exception>
        [SupportedOSPlatform("windows")]
        public CompositeMLKemCng(CngKey key)
            : base(AlgorithmFromHandleWithPlatformCheck(key, out CngKey duplicateKey))
        {
            _key = duplicateKey;
        }

        private static CompositeMLKemAlgorithm AlgorithmFromHandleWithPlatformCheck(
            CngKey key,
            out CngKey duplicateKey)
        {
            if (!Helpers.IsOSPlatformWindows)
            {
                throw new PlatformNotSupportedException();
            }

            return AlgorithmFromHandle(key, out duplicateKey);
        }

        private static partial CompositeMLKemAlgorithm AlgorithmFromHandle(CngKey key, out CngKey duplicateKey);

        /// <summary>
        ///   Gets a new <see cref="CngKey" /> representing the key used by the current instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <remarks>
        ///   This <see cref="CngKey"/> object is not the same as the one passed to <see cref="CompositeMLKemCng(CngKey)"/>,
        ///   if that constructor was used. However, it will point to the same CNG key.
        /// </remarks>
        public partial CngKey GetKey();
    }
}
