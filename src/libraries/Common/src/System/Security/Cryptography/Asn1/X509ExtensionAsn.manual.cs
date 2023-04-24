// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Asn1
{
    internal partial struct X509ExtensionAsn
    {
        public X509ExtensionAsn(X509Extension extension)
        {
#if NET5_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(extension);
#else
            if (extension is null)
            {
                throw new ArgumentNullException(nameof(extension));
            }
#endif

            ExtnId = extension.Oid!.Value!;
            Critical = extension.Critical;
            ExtnValue = extension.RawData;
        }
    }
}
