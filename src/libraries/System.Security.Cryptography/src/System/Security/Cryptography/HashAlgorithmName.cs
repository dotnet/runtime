// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    // Strongly typed string representing the name of a hash algorithm.
    // Open ended to allow extensibility while giving the discoverable feel of an enum for common values.

    /// <summary>
    /// Specifies the name of a cryptographic hash algorithm.
    /// </summary>
    /// <remarks>
    /// Asymmetric Algorithms implemented using Microsoft's CNG (Cryptography Next Generation) API
    /// will interpret the underlying string value as a CNG algorithm identifier:
    ///   * https://msdn.microsoft.com/en-us/library/windows/desktop/aa375534(v=vs.85).aspx
    ///
    /// As with CNG, the names are case-sensitive.
    ///
    /// Asymmetric Algorithms implemented using other technologies:
    ///    * Must recognize at least "MD5", "SHA1", "SHA256", "SHA384", and "SHA512".
    ///    * Should recognize additional CNG IDs for any other hash algorithms that they also support.
    /// </remarks>
    public readonly struct HashAlgorithmName : IEquatable<HashAlgorithmName>
    {
        // Returning a new instance every time is free here since HashAlgorithmName is a struct with
        // a single string field. The optimized codegen should be equivalent to return "MD5".

        /// <summary>
        /// Gets a <see cref="HashAlgorithmName" /> representing "MD5"
        /// </summary>
        public static HashAlgorithmName MD5 { get { return new HashAlgorithmName("MD5"); } }

        /// <summary>
        /// Gets a <see cref="HashAlgorithmName" /> representing "SHA1"
        /// </summary>
        public static HashAlgorithmName SHA1 { get { return new HashAlgorithmName("SHA1"); } }

        /// <summary>
        /// Gets a <see cref="HashAlgorithmName" /> representing "SHA256"
        /// </summary>
        public static HashAlgorithmName SHA256 { get { return new HashAlgorithmName("SHA256"); } }

        /// <summary>
        /// Gets a <see cref="HashAlgorithmName" /> representing "SHA384"
        /// </summary>
        public static HashAlgorithmName SHA384 { get { return new HashAlgorithmName("SHA384"); } }

        /// <summary>
        /// Gets a <see cref="HashAlgorithmName" /> representing "SHA512"
        /// </summary>
        public static HashAlgorithmName SHA512 { get { return new HashAlgorithmName("SHA512"); } }

        private readonly string? _name;

        /// <summary>
        /// Gets a <see cref="HashAlgorithmName" /> representing a custom name.
        /// </summary>
        /// <param name="name">The custom hash algorithm name.</param>
        public HashAlgorithmName(string? name)
        {
            // Note: No validation because we have to deal with default(HashAlgorithmName) regardless.
            _name = name;
        }

        /// <summary>
        /// Gets the underlying string representation of the algorithm name.
        /// </summary>
        /// <remarks>
        /// May be null or empty to indicate that no hash algorithm is applicable.
        /// </remarks>
        public string? Name
        {
            get { return _name; }
        }

        public override string ToString()
        {
            return _name ?? string.Empty;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is HashAlgorithmName && Equals((HashAlgorithmName)obj);
        }

        public bool Equals(HashAlgorithmName other)
        {
            // NOTE: intentionally ordinal and case sensitive, matches CNG.
            return _name == other._name;
        }

        public override int GetHashCode()
        {
            return _name == null ? 0 : _name.GetHashCode();
        }

        public static bool operator ==(HashAlgorithmName left, HashAlgorithmName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HashAlgorithmName left, HashAlgorithmName right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Tries to convert the specified OID to a hash algorithm name.
        /// </summary>
        /// <param name="oidValue">The OID of the hash algorithm.</param>
        /// <param name="value">
        /// When this method returns <c>true</c>, the hash algorithm. When this
        /// method returns <c>false</c>, contains <c>default</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the OID was successfully mapped to a hash
        /// algorithm; otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oidValue" /> is null.
        /// </exception>
        public static bool TryFromOid(string oidValue, out HashAlgorithmName value)
        {
            ArgumentNullException.ThrowIfNull(oidValue);

            switch (oidValue)
            {
                case Oids.Md5:
                    value = MD5;
                    return true;
                case Oids.Sha1:
                    value = SHA1;
                    return true;
                case Oids.Sha256:
                    value = SHA256;
                    return true;
                case Oids.Sha384:
                    value = SHA384;
                    return true;
                case Oids.Sha512:
                    value = SHA512;
                    return true;
                default:
                    value = default;
                    return false;
            }
        }

        /// <summary>
        /// Converts the specified OID to a hash algorithm name.
        /// </summary>
        /// <param name="oidValue">The OID of the hash algorithm.</param>
        /// <returns>
        /// The hash algorithm name identified by the OID.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oidValue" /> is null.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <paramref name="oidValue" /> does not represent a known hash algorithm.
        /// </exception>
        public static HashAlgorithmName FromOid(string oidValue)
        {
            if (TryFromOid(oidValue, out HashAlgorithmName value))
            {
                return value;
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_InvalidHashAlgorithmOid, oidValue));
        }
    }
}
