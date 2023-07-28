// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public abstract class EncryptedType
    {
        private string? _id;
        private string? _type;
        private string? _mimeType;
        private string? _encoding;
        private EncryptionMethod? _encryptionMethod;
        private CipherData? _cipherData;
        private EncryptionPropertyCollection? _props;
        private KeyInfo? _keyInfo;
        internal XmlElement? _cachedXml;

        [MemberNotNullWhen(true, nameof(_cachedXml))]
        internal bool CacheValid
        {
            get
            {
                return (_cachedXml != null);
            }
        }

        public virtual string? Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _cachedXml = null;
            }
        }

        public virtual string? Type
        {
            get { return _type; }
            set
            {
                _type = value;
                _cachedXml = null;
            }
        }

        public virtual string? MimeType
        {
            get { return _mimeType; }
            set
            {
                _mimeType = value;
                _cachedXml = null;
            }
        }

        public virtual string? Encoding
        {
            get { return _encoding; }
            set
            {
                _encoding = value;
                _cachedXml = null;
            }
        }

        [AllowNull]
        public KeyInfo KeyInfo
        {
            get => _keyInfo ??= new KeyInfo();
            set => _keyInfo = value;
        }

        public virtual EncryptionMethod? EncryptionMethod
        {
            get { return _encryptionMethod; }
            set
            {
                _encryptionMethod = value;
                _cachedXml = null;
            }
        }

        public virtual EncryptionPropertyCollection EncryptionProperties => _props ??= new EncryptionPropertyCollection();

        public void AddProperty(EncryptionProperty ep)
        {
            EncryptionProperties.Add(ep);
        }

        public virtual CipherData CipherData
        {
            get => _cipherData ??= new CipherData();
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _cipherData = value;
                _cachedXml = null;
            }
        }

        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        public abstract void LoadXml(XmlElement value);
        public abstract XmlElement GetXml();
    }
}
