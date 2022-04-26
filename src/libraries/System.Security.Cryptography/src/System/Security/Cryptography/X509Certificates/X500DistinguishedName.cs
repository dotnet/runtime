// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X500DistinguishedName : AsnEncodedData
    {
        public X500DistinguishedName(byte[] encodedDistinguishedName)
            : base(new Oid(null, null), encodedDistinguishedName)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X500DistinguishedName"/>
        ///   class using information from the provided data.
        /// </summary>
        /// <param name="encodedDistinguishedName">
        ///   The encoded distinguished name.
        /// </param>
        /// <seealso cref="Encode"/>
        public X500DistinguishedName(ReadOnlySpan<byte> encodedDistinguishedName)
            : base(new Oid(null, null), encodedDistinguishedName)
        {
        }

        public X500DistinguishedName(AsnEncodedData encodedDistinguishedName)
            : base(encodedDistinguishedName)
        {
        }

        public X500DistinguishedName(X500DistinguishedName distinguishedName)
            : base(distinguishedName)
        {
            _lazyDistinguishedName = distinguishedName.Name;
        }

        public X500DistinguishedName(string distinguishedName)
            : this(distinguishedName, X500DistinguishedNameFlags.Reversed)
        {
        }

        public X500DistinguishedName(string distinguishedName, X500DistinguishedNameFlags flag)
            : base(new Oid(null, null), Encode(distinguishedName, flag))
        {
            _lazyDistinguishedName = distinguishedName;
        }

        public string Name
        {
            get
            {
                string? name = _lazyDistinguishedName;
                if (name == null)
                {
                    name = _lazyDistinguishedName = Decode(X500DistinguishedNameFlags.Reversed);
                }
                return name;
            }
        }

        public string Decode(X500DistinguishedNameFlags flag)
        {
            ThrowIfInvalid(flag);
            return X509Pal.Instance.X500DistinguishedNameDecode(RawData, flag);
        }

        public override string Format(bool multiLine)
        {
            return X509Pal.Instance.X500DistinguishedNameFormat(RawData, multiLine);
        }

        private static byte[] Encode(string distinguishedName, X500DistinguishedNameFlags flags)
        {
            ArgumentNullException.ThrowIfNull(distinguishedName);

            ThrowIfInvalid(flags);

            return X509Pal.Instance.X500DistinguishedNameEncode(distinguishedName, flags);
        }

        private static void ThrowIfInvalid(X500DistinguishedNameFlags flags)
        {
            // All values or'ed together. Change this if you add values to the enumeration.
            uint allFlags = 0x71F1;
            uint dwFlags = (uint)flags;
            if ((dwFlags & ~allFlags) != 0)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, "flag"));
        }

        private volatile string? _lazyDistinguishedName;
    }
}
