// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X500DistinguishedName : AsnEncodedData
    {
        public X500DistinguishedName(byte[] encodedDistinguishedName)
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

        public X500DistinguishedName(String distinguishedName)
            : this(distinguishedName, X500DistinguishedNameFlags.Reversed)
        {
        }

        public X500DistinguishedName(String distinguishedName, X500DistinguishedNameFlags flag)
            : base(new Oid(null, null), Encode(distinguishedName, flag))
        {
            _lazyDistinguishedName = distinguishedName;
        }

        public String Name
        {
            get
            {
                String name = _lazyDistinguishedName;
                if (name == null)
                {
                    name = _lazyDistinguishedName = Decode(X500DistinguishedNameFlags.Reversed);
                }
                return name;
            }
        }

        public String Decode(X500DistinguishedNameFlags flag)
        {
            ThrowIfInvalid(flag);
            return X509Pal.Instance.X500DistinguishedNameDecode(RawData, flag);
        }

        public override String Format(bool multiLine)
        {
            return X509Pal.Instance.X500DistinguishedNameFormat(RawData, multiLine);
        }

        private static byte[] Encode(String distinguishedName, X500DistinguishedNameFlags flags)
        {
            if (distinguishedName == null)
                throw new ArgumentNullException("distinguishedName");
            ThrowIfInvalid(flags);

            return X509Pal.Instance.X500DistinguishedNameEncode(distinguishedName, flags);
        }

        private static void ThrowIfInvalid(X500DistinguishedNameFlags flags)
        {
            // All values or'ed together. Change this if you add values to the enumeration.
            uint allFlags = 0x71F1;
            uint dwFlags = (uint)flags;
            if ((dwFlags & ~allFlags) != 0)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.Arg_EnumIllegalVal, "flag"));
            return;
        }

        private volatile String _lazyDistinguishedName;
    }
}

