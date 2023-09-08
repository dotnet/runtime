// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

#pragma warning disable CA1510

namespace System.Security.Cryptography
{
    /// <summary>
    ///   NIST SP 800-108 HMAC CTR Key-Based Key Derivation (KBKDF)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This implements NIST SP 800-108 HMAC in counter mode. The implemented KDF assumes the form of
    ///     <c>PRF (KI, [i]2 || Label || 0x00 || Context || [L]2)</c> where <c>[i]2</c> and <c>[L]2</c> are encoded as
    ///     unsigned 32-bit integers, big endian.
    ///   </para>
    ///   <para>
    ///     All members of this class are thread safe. If the instance is disposed of while other threads are using
    ///     the instance, those threads will either receive an <see cref="ObjectDisposedException" /> or produce a valid
    ///     derived key.
    ///   </para>
    /// </remarks>
    public sealed partial class SP800108HmacCounterKdf : IDisposable
    {
        // The maximum amount of data that we can produce with the PRF is 0x1FFFFFFF.
        // This is because of L[2]. From SP 800-108 r1:
        // L – An integer specifying the requested length (in bits) of the derived keying material KOUT.
        // As an unsigned 32-bit interger (see r), L needs to become L[2] by multiplying by 8 (bytes to bits).
        // We can't encode more than 0x1FFFFFFF as bits without overflowing.
        // Windows' BCryptKeyDerivation cannot fullfill a request larger than 0x1FFFFFFF, either.
        private const int MaxPrfOutputSize = (int)(uint.MaxValue / 8);

        private readonly SP800108HmacCounterKdfImplementationBase _implementation;

        private static partial SP800108HmacCounterKdfImplementationBase CreateImplementation(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm);

        /// <summary>
        ///   Initializes a new instance of <see cref="SP800108HmacCounterKdf" /> using a specified key and HMAC algorithm.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        public SP800108HmacCounterKdf(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
        {
            CheckHashAlgorithm(hashAlgorithm);
            _implementation = CreateImplementation(key, hashAlgorithm);
        }

        /// <summary>
        ///   Initializes a new instance of <see cref="SP800108HmacCounterKdf" /> using a specified key and HMAC algorithm.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///     <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="key" /> is <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        public SP800108HmacCounterKdf(byte[] key, HashAlgorithmName hashAlgorithm)
        {
            // This constructor doesn't defer to the span constructor because SP800108HmacCounterKdfImplementationCng
            // has a constructor for byte[] key to avoid a byte[]->span->byte[] conversion.

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            CheckHashAlgorithm(hashAlgorithm);
            _implementation = CreateImplementation(key, hashAlgorithm);
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///     <paramref name="key" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="label" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="context" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///     that can be derived.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        public static byte[] DeriveBytes(byte[] key, HashAlgorithmName hashAlgorithm, byte[] label, byte[] context, int derivedKeyLengthInBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (label is null)
                throw new ArgumentNullException(nameof(label));

            if (context is null)
                throw new ArgumentNullException(nameof(context));

            CheckPrfOutputLength(derivedKeyLengthInBytes, nameof(derivedKeyLengthInBytes));
            CheckHashAlgorithm(hashAlgorithm);

            // Don't call to the Span overload so that we don't go from array->span->array for the key in the .NET Standard
            // build, which prefers to use arrays for the key.
            return DeriveBytesCore(key, hashAlgorithm, label, context, derivedKeyLengthInBytes);
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///     <paramref name="key" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="label" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="context" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///     that can be derived.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        ///   <paramref name="label" /> or <paramref name="context" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <remarks>
        ///   <paramref name="label" /> and <paramref name="context" /> will be converted to bytes using the UTF-8 encoding.
        ///   for other encodings, perform the conversion using the desired encoding and use an overload which accepts the
        ///   label and context as a sequence of bytes.
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        public static byte[] DeriveBytes(byte[] key, HashAlgorithmName hashAlgorithm, string label, string context, int derivedKeyLengthInBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (label is null)
                throw new ArgumentNullException(nameof(label));

            if (context is null)
                throw new ArgumentNullException(nameof(context));

            CheckPrfOutputLength(derivedKeyLengthInBytes, nameof(derivedKeyLengthInBytes));
            CheckHashAlgorithm(hashAlgorithm);

            byte[] result = new byte[derivedKeyLengthInBytes];
            DeriveBytesCore(key, hashAlgorithm, label.AsSpan(), context.AsSpan(), result);
            return result;
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///     that can be derived.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        public static byte[] DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, int derivedKeyLengthInBytes)
        {
            CheckPrfOutputLength(derivedKeyLengthInBytes, nameof(derivedKeyLengthInBytes));

            byte[] result = new byte[derivedKeyLengthInBytes];
            DeriveBytes(key, hashAlgorithm, label, context, result);
            return result;
        }

        /// <summary>
        ///   Fills a buffer with a derived key.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="destination">The buffer which will receive the derived key.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="destination" /> is larger than the maximum number of bytes that can be derived.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        public static void DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            CheckHashAlgorithm(hashAlgorithm);
            CheckPrfOutputLength(destination.Length, nameof(destination));
            DeriveBytesCore(key, hashAlgorithm, label, context, destination);
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///   that can be derived.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        ///   <paramref name="label" /> or <paramref name="context" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        /// <remarks>
        ///   <paramref name="label" /> and <paramref name="context" /> will be converted to bytes using the UTF-8 encoding.
        ///   for other encodings, perform the conversion using the desired encoding and use an overload which accepts the
        ///   label and context as a sequence of bytes.
        /// </remarks>
        public static byte[] DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> label, ReadOnlySpan<char> context, int derivedKeyLengthInBytes)
        {
            CheckPrfOutputLength(derivedKeyLengthInBytes, nameof(derivedKeyLengthInBytes));

            byte[] result = new byte[derivedKeyLengthInBytes];
            DeriveBytes(key, hashAlgorithm, label, context, result);
            return result;
        }

        /// <summary>
        ///   Fills a buffer with a derived key.
        /// </summary>
        /// <param name="key">The key-derivation key.</param>
        /// <param name="hashAlgorithm">The HMAC algorithm.</param>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="destination">The buffer which will receive the derived key.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" /> which is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="destination" /> is larger than the maximum number of bytes that can be derived.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> is not a known or supported hash algorithm.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        ///   <paramref name="label" /> or <paramref name="context" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not have a supported implementation of HMAC.
        /// </exception>
        /// <remarks>
        ///   <paramref name="label" /> and <paramref name="context" /> will be converted to bytes using the UTF-8 encoding.
        ///   for other encodings, perform the conversion using the desired encoding and use an overload which accepts the
        ///   label and context as a sequence of bytes.
        /// </remarks>
        public static void DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
        {
            CheckHashAlgorithm(hashAlgorithm);
            CheckPrfOutputLength(destination.Length, nameof(destination));
            DeriveBytesCore(key, hashAlgorithm, label, context, destination);
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///     <paramref name="label" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="context" /> is <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///   that can be derived.
        /// </exception>
        public byte[] DeriveKey(byte[] label, byte[] context, int derivedKeyLengthInBytes)
        {
            if (label is null)
                throw new ArgumentNullException(nameof(label));

            if (context is null)
                throw new ArgumentNullException(nameof(context));

            CheckPrfOutputLength(derivedKeyLengthInBytes, nameof(derivedKeyLengthInBytes));

            byte[] result = new byte[derivedKeyLengthInBytes];
            DeriveKeyCore(label, context, result);
            return result;
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///   that can be derived.
        /// </exception>
        public byte[] DeriveKey(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, int derivedKeyLengthInBytes)
        {
            CheckPrfOutputLength(derivedKeyLengthInBytes, nameof(derivedKeyLengthInBytes));

            byte[] result = new byte[derivedKeyLengthInBytes];
            DeriveKey(label, context, result);
            return result;
        }

        /// <summary>
        ///   Fills a buffer with a derived key.
        /// </summary>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="destination">The buffer which will receive the derived key.</param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///     <paramref name="label" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="context" /> is <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="destination" /> is larger than the maximum number of bytes that can be derived.
        /// </exception>
        public void DeriveKey(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            CheckPrfOutputLength(destination.Length, nameof(destination));
            DeriveKeyCore(label, context, destination);
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///   that can be derived.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        ///   <paramref name="label" /> or <paramref name="context" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <remarks>
        ///   <paramref name="label" /> and <paramref name="context" /> will be converted to bytes using the UTF-8 encoding.
        ///   for other encodings, perform the conversion using the desired encoding and use an overload which accepts the
        ///   label and context as a sequence of bytes.
        /// </remarks>
        public byte[] DeriveKey(ReadOnlySpan<char> label, ReadOnlySpan<char> context, int derivedKeyLengthInBytes)
        {
            CheckPrfOutputLength(derivedKeyLengthInBytes, nameof(derivedKeyLengthInBytes));

            byte[] result = new byte[derivedKeyLengthInBytes];
            DeriveKeyCore(label, context, result);
            return result;
        }

        /// <summary>
        ///   Fills a buffer with a derived key.
        /// </summary>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="destination">The buffer which will receive the derived key.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="destination" /> is larger than the maximum number of bytes that can be derived.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        ///   <paramref name="label" /> or <paramref name="context" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <remarks>
        ///   <paramref name="label" /> and <paramref name="context" /> will be converted to bytes using the UTF-8 encoding.
        ///   for other encodings, perform the conversion using the desired encoding and use an overload which accepts the
        ///   label and context as a sequence of bytes.
        /// </remarks>
        public void DeriveKey(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
        {
            CheckPrfOutputLength(destination.Length, nameof(destination));
            DeriveKeyCore(label, context, destination);
        }

        /// <summary>
        ///   Derives a key of a specified length.
        /// </summary>
        /// <param name="label">The label that identifies the purpose for the derived key.</param>
        /// <param name="context">The context containing information related to the derived key.</param>
        /// <param name="derivedKeyLengthInBytes">The length of the derived key, in bytes.</param>
        /// <returns>An array containing the derived key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///     <paramref name="label" /> is <see langword="null" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="context" /> is <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="derivedKeyLengthInBytes" /> is negative or larger than the maximum number of bytes
        ///   that can be derived.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        ///   <paramref name="label" /> or <paramref name="context" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <remarks>
        ///   <paramref name="label" /> and <paramref name="context" /> will be converted to bytes using the UTF-8 encoding.
        ///   for other encodings, perform the conversion using the desired encoding and use an overload which accepts the
        ///   label and context as a sequence of bytes.
        /// </remarks>
        public byte[] DeriveKey(string label, string context, int derivedKeyLengthInBytes)
        {
            if (label is null)
                throw new ArgumentNullException(nameof(label));

            if (context is null)
                throw new ArgumentNullException(nameof(context));

            return DeriveKey(label.AsSpan(), context.AsSpan(), derivedKeyLengthInBytes);
        }

        /// <summary>
        ///   Releases all resources used by the current instance of <see cref="SP800108HmacCounterKdf"/>.
        /// </summary>
        public void Dispose()
        {
            _implementation.Dispose();
        }

        private static void CheckHashAlgorithm(HashAlgorithmName hashAlgorithm)
        {
            string? hashAlgorithmName = hashAlgorithm.Name;

            switch (hashAlgorithmName)
            {
                case null:
                    throw new ArgumentNullException(nameof(hashAlgorithm));
                case "":
                    throw new ArgumentException(SR.Argument_EmptyString, nameof(hashAlgorithm));
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    break;
#if NET8_0_OR_GREATER
                case HashAlgorithmNames.SHA3_256:
                    if (!HMACSHA3_256.IsSupported)
                    {
                        throw new PlatformNotSupportedException();
                    }
                    break;
                case HashAlgorithmNames.SHA3_384:
                    if (!HMACSHA3_384.IsSupported)
                    {
                        throw new PlatformNotSupportedException();
                    }
                    break;
                case HashAlgorithmNames.SHA3_512:
                    if (!HMACSHA3_512.IsSupported)
                    {
                        throw new PlatformNotSupportedException();
                    }
                    break;
#endif
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName));
            }
        }

        private static partial byte[] DeriveBytesCore(
            byte[] key,
            HashAlgorithmName hashAlgorithm,
            byte[] label,
            byte[] context,
            int derivedKeyLengthInBytes);

        private static partial void DeriveBytesCore(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination);

        private static partial void DeriveBytesCore(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<char> label,
            ReadOnlySpan<char> context,
            Span<byte> destination);

        private void DeriveKeyCore(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            _implementation.DeriveBytes(label, context, destination);
        }

        private void DeriveKeyCore(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
        {
            _implementation.DeriveBytes(label, context, destination);
        }

        private static void CheckPrfOutputLength(int length, string paramName)
        {
            if (length > MaxPrfOutputSize)
            {
                throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_KOut_Too_Large);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
        }
    }
}
