// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class KeyInfoEncryptedKey : KeyInfoClause
    {
        private EncryptedKey _encryptedKey;

        public KeyInfoEncryptedKey() { }

        public KeyInfoEncryptedKey(EncryptedKey encryptedKey)
        {
            _encryptedKey = encryptedKey;
        }

        public EncryptedKey EncryptedKey
        {
            get { return _encryptedKey; }
            set { _encryptedKey = value; }
        }

        public override XmlElement GetXml()
        {
            if (_encryptedKey == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "KeyInfoEncryptedKey");
            return _encryptedKey.GetXml();
        }

        internal override XmlElement GetXml(XmlDocument xmlDocument)
        {
            if (_encryptedKey == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "KeyInfoEncryptedKey");
            return _encryptedKey.GetXml(xmlDocument);
        }

        public override void LoadXml(XmlElement value)
        {
            _encryptedKey = new EncryptedKey();
            _encryptedKey.LoadXml(value);
        }
    }
}
