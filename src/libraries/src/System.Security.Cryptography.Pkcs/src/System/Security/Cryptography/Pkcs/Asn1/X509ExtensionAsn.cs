// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct X509ExtensionAsn
    {
        [ObjectIdentifier]
        internal string ExtnId;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [DefaultValue(0x01, 0x01, 0x00)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
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
    }
}
