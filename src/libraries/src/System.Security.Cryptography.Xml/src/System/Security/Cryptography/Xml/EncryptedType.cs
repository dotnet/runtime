// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public abstract class EncryptedType
    {
        private string _id;
        private string _type;
        private string _mimeType;
        private string _encoding;
        private EncryptionMethod _encryptionMethod;
        private CipherData _cipherData;
        private EncryptionPropertyCollection _props;
        private KeyInfo _keyInfo;
        internal XmlElement _cachedXml = null;

        internal bool CacheValid
        {
            get
            {
                return (_cachedXml != null);
            }
        }

        public virtual string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _cachedXml = null;
            }
        }

        public virtual string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                _cachedXml = null;
            }
        }

        public virtual string MimeType
        {
            get { return _mimeType; }
            set
            {
                _mimeType = value;
                _cachedXml = null;
            }
        }

        public virtual string Encoding
        {
            get { return _encoding; }
            set
            {
                _encoding = value;
                _cachedXml = null;
            }
        }

        public KeyInfo KeyInfo
        {
            get
            {
                if (_keyInfo == null)
                    _keyInfo = new KeyInfo();
                return _keyInfo;
            }
            set { _keyInfo = value; }
        }

        public virtual EncryptionMethod EncryptionMethod
        {
            get { return _encryptionMethod; }
            set
            {
                _encryptionMethod = value;
                _cachedXml = null;
            }
        }

        public virtual EncryptionPropertyCollection EncryptionProperties
        {
            get
            {
                if (_props == null)
                    _props = new EncryptionPropertyCollection();
                return _props;
            }
        }

        public void AddProperty(EncryptionProperty ep)
        {
            EncryptionProperties.Add(ep);
        }

        public virtual CipherData CipherData
        {
            get
            {
                if (_cipherData == null)
                    _cipherData = new CipherData();

                return _cipherData;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _cipherData = value;
                _cachedXml = null;
            }
        }

        public abstract void LoadXml(XmlElement value);
        public abstract XmlElement GetXml();
    }
}
