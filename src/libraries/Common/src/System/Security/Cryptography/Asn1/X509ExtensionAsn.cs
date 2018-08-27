// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct X509ExtensionAsn
    {
        [ObjectIdentifier]
        internal string ExtnId;

        [DefaultValue(0x01, 0x01, 0x00)]
        internal bool Critical;

        [OctetString]
        internal ReadOnlyMemory<byte> ExtnValue;

        public X509ExtensionAsn(X509Extension extension, bool copyValue=true)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            ExtnId = extension.Oid.Value;
            Critical = extension.Critical;
            ExtnValue = copyValue ? extension.RawData.CloneByteArray() : extension.RawData;
        }

        internal void Encode(AsnWriter writer)
        {
            AsnSerializer.Serialize(this, writer);
        }

        internal static void Decode(AsnReader reader, out X509ExtensionAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            ReadOnlyMemory<byte> value = reader.GetEncodedValue();
            decoded = AsnSerializer.Deserialize<X509ExtensionAsn>(value, reader.RuleSet);
        }
    }
}
