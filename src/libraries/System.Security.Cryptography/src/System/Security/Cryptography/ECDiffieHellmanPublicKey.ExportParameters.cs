// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Wrapper for public key material passed between parties during Diffie-Hellman key material generation
    /// </summary>
    public abstract partial class ECDiffieHellmanPublicKey : IDisposable
    {
        /// <summary>
        /// When overridden in a derived class, exports the named or explicit ECParameters for an ECCurve.
        /// If the curve has a name, the Curve property will contain named curve parameters, otherwise it
        /// will contain explicit parameters.
        /// </summary>
        /// <returns>The ECParameters representing the point on the curve for this key.</returns>
        public virtual ECParameters ExportParameters()
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, exports the explicit ECParameters for an ECCurve.
        /// </summary>
        /// <returns>The ECParameters representing the point on the curve for this key, using the explicit curve format.</returns>
        public virtual ECParameters ExportExplicitParameters()
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// Attempts to export the current key in the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <param name="destination">The byte span to receive the X.509 SubjectPublicKeyInfo data.</param>
        /// <param name="bytesWritten">
        /// When this method returns, contains a value that indicates the number of bytes written to <paramref name="destination" />.
        /// This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is big enough to receive the output;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// The member <see cref="ExportParameters" /> has not been overridden in a derived class.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">The key is invalid and could not be exported.</exception>
        public virtual bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters();
            AsnWriter writer = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
            return writer.TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        /// Exports the current key in the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        /// A byte array containing the X.509 SubjectPublicKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// The member <see cref="ExportParameters" /> has not been overridden in a derived class.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">The key is invalid and could not be exported.</exception>
        public virtual byte[] ExportSubjectPublicKeyInfo()
        {
            ECParameters ecParameters = ExportParameters();
            AsnWriter writer = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
            return writer.Encode();
        }
    }
}
