// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public class SignatureDescription
    {
        public string KeyAlgorithm { get; set; }
        public string DigestAlgorithm { get; set; }
        public string FormatterAlgorithm { get; set; }
        public string DeformatterAlgorithm { get; set; }

        public SignatureDescription()
        {
        }

        public SignatureDescription(SecurityElement el)
        {
            if (el == null)
                throw new ArgumentNullException(nameof(el));
            KeyAlgorithm = el.SearchForTextOfTag("Key");
            DigestAlgorithm = el.SearchForTextOfTag("Digest");
            FormatterAlgorithm = el.SearchForTextOfTag("Formatter");
            DeformatterAlgorithm = el.SearchForTextOfTag("Deformatter");
        }

        public virtual AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            AsymmetricSignatureDeformatter item = (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(DeformatterAlgorithm);
            item.SetKey(key);
            return item;
        }

        public virtual AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            AsymmetricSignatureFormatter item = (AsymmetricSignatureFormatter)CryptoConfig.CreateFromName(FormatterAlgorithm);
            item.SetKey(key);
            return item;
        }

        public virtual HashAlgorithm CreateDigest()
        {
            return (HashAlgorithm)CryptoConfig.CreateFromName(DigestAlgorithm);
        }
    }
}