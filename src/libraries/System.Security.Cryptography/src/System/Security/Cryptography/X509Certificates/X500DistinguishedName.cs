// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X500DistinguishedName : AsnEncodedData
    {
        private volatile string? _lazyDistinguishedName;
        private List<X500RelativeDistinguishedName>? _parsedAttributes;

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

        public string Name => _lazyDistinguishedName ??= Decode(X500DistinguishedNameFlags.Reversed);

        public string Decode(X500DistinguishedNameFlags flag)
        {
            ThrowIfInvalid(flag);
            return X509Pal.Instance.X500DistinguishedNameDecode(RawData, flag);
        }

        public override string Format(bool multiLine)
        {
            return X509Pal.Instance.X500DistinguishedNameFormat(RawData, multiLine);
        }

        /// <summary>
        ///   Iterates over the RelativeDistinguishedName values within this distinguished name value.
        /// </summary>
        /// <param name="reversed">
        ///   <see langword="true" /> to enumerate in the order used by <see cref="Name"/>;
        ///   <see langword="false" /> to enumerate in the declared order.
        /// </param>
        /// <returns>
        ///   An enumerator that iterates over the relative distinguished names in the X.500 Dinstinguished Name.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The X.500 Name is not a proper DER-encoded X.500 Name value.
        /// </exception>
        public IEnumerable<X500RelativeDistinguishedName> EnumerateRelativeDistinguishedNames(bool reversed = true)
        {
            List<X500RelativeDistinguishedName> parsedAttributes = _parsedAttributes ??= ParseAttributes(RawData);

            return EnumerateRelativeDistinguishedNames(parsedAttributes, reversed);
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

        private static IEnumerable<X500RelativeDistinguishedName> EnumerateRelativeDistinguishedNames(
            List<X500RelativeDistinguishedName> parsedAttributes,
            bool reversed)
        {
            if (reversed)
            {
                for (int i = parsedAttributes.Count - 1; i >= 0; i--)
                {
                    yield return parsedAttributes[i];
                }
            }
            else
            {
                for (int i = 0; i < parsedAttributes.Count; i++)
                {
                    yield return parsedAttributes[i];
                }
            }
        }

        private static List<X500RelativeDistinguishedName> ParseAttributes(byte[] rawData)
        {
            List<X500RelativeDistinguishedName>? parsedAttributes = null;
            ReadOnlyMemory<byte> rawDataMemory = rawData;
            ReadOnlySpan<byte> rawDataSpan = rawData;

            try
            {
                AsnValueReader outer = new AsnValueReader(rawDataSpan, AsnEncodingRules.DER);
                AsnValueReader sequence = outer.ReadSequence();
                outer.ThrowIfNotEmpty();

                while (sequence.HasData)
                {
                    ReadOnlySpan<byte> encodedValue = sequence.PeekEncodedValue();

                    if (!rawDataSpan.Overlaps(encodedValue, out int offset))
                    {
                        Debug.Fail("AsnValueReader produced a span outside of the original bounds");
                        throw new UnreachableException();
                    }

                    var rdn = new X500RelativeDistinguishedName(rawDataMemory.Slice(offset, encodedValue.Length));
                    sequence.ReadEncodedValue();
                    (parsedAttributes ??= new List<X500RelativeDistinguishedName>()).Add(rdn);
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            return parsedAttributes ?? new List<X500RelativeDistinguishedName>();
        }
    }
}
