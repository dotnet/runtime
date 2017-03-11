// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Xml
{
    internal class DSASignatureDescription : SignatureDescription
    {
        const string HashAlgorithm = "SHA1";

        public DSASignatureDescription()
        {
            KeyAlgorithm = typeof(System.Security.Cryptography.DSA).AssemblyQualifiedName;
            FormatterAlgorithm = typeof(System.Security.Cryptography.DSASignatureFormatter).AssemblyQualifiedName;
            DeformatterAlgorithm = typeof(System.Security.Cryptography.DSASignatureDeformatter).AssemblyQualifiedName;
            DigestAlgorithm = "SHA1";
        }

        public sealed override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureDeformatter)CryptoHelpers.CreateFromName(DeformatterAlgorithm);
            item.SetKey(key);
            item.SetHashAlgorithm(HashAlgorithm);
            return item;
        }

        public sealed override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureFormatter)CryptoHelpers.CreateFromName(FormatterAlgorithm);
            item.SetKey(key);
            item.SetHashAlgorithm(HashAlgorithm);
            return item;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 needed for compat.")]
        public sealed override HashAlgorithm CreateDigest()
        {
            return SHA1.Create();
        }
    }
}
