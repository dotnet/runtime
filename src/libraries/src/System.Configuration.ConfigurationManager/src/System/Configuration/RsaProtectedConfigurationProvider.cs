// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

namespace System.Configuration
{
    // Need the System.Security.Cryptography.Xml package published before this can be enabled,
    // for now, make it throw PlatformNotSupported.
    //
    // https://github.com/dotnet/corefx/issues/14950

    public sealed class RsaProtectedConfigurationProvider : ProtectedConfigurationProvider
    {
        public override XmlNode Decrypt(XmlNode encryptedNode)
        {
            throw new PlatformNotSupportedException();
        }

        public override XmlNode Encrypt(XmlNode node)
        {
            throw new PlatformNotSupportedException();
        }

        public void AddKey(int keySize, bool exportable)
        {
            throw new PlatformNotSupportedException();
        }

        public void DeleteKey()
        {
            throw new PlatformNotSupportedException();
        }

        public void ImportKey(string xmlFileName, bool exportable)
        {
            throw new PlatformNotSupportedException();
        }

        public void ExportKey(string xmlFileName, bool includePrivateParameters)
        {
            throw new PlatformNotSupportedException();
        }

        public string KeyContainerName { get { throw new PlatformNotSupportedException(); } }
        public string CspProviderName { get { throw new PlatformNotSupportedException(); } }
        public bool UseMachineContainer { get { throw new PlatformNotSupportedException(); } }
        public bool UseOAEP { get { throw new PlatformNotSupportedException(); } }
        public bool UseFIPS { get { throw new PlatformNotSupportedException(); } }
        public RSAParameters RsaPublicKey { get { throw new PlatformNotSupportedException(); } }
    }


#if FALSE
    public sealed class RsaProtectedConfigurationProvider : ProtectedConfigurationProvider
    {
        // Note: this name has to match the name used in RegiisUtility 
        const string DefaultRsaKeyContainerName = "NetFrameworkConfigurationKey";
        const uint PROV_Rsa_FULL = 1;
        const uint CRYPT_MACHINE_KEYSET = 0x00000020;

        private string _keyName;
        private string _keyContainerName;
        private string _cspProviderName;
        private bool _useMachineContainer;
        private bool _useOAEP;
        private bool _useFIPS;

        public override XmlNode Decrypt(XmlNode encryptedNode)
        {
            XmlDocument xmlDocument = new XmlDocument();
            EncryptedXml exml = null;
            RSACryptoServiceProvider rsa = GetCryptoServiceProvider(false, true);

            xmlDocument.PreserveWhitespace = true;
            xmlDocument.LoadXml(encryptedNode.OuterXml);
            exml = new FipsAwareEncryptedXml(xmlDocument);
            exml.AddKeyNameMapping(_keyName, rsa);
            exml.DecryptDocument();
            rsa.Clear();
            return xmlDocument.DocumentElement;
        }

        public override XmlNode Encrypt(XmlNode node)
        {
            XmlDocument xmlDocument;
            EncryptedXml exml;
            byte[] rgbOutput;
            EncryptedData ed;
            KeyInfoName kin;
            EncryptedKey ek;
            KeyInfoEncryptedKey kek;
            XmlElement inputElement;
            RSACryptoServiceProvider rsa = GetCryptoServiceProvider(false, false);

            // Encrypt the node with the new key
            xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            xmlDocument.LoadXml("<foo>" + node.OuterXml + "</foo>");
            exml = new EncryptedXml(xmlDocument);
            inputElement = xmlDocument.DocumentElement;

            using (SymmetricAlgorithm symAlg = GetSymAlgorithmProvider())
            {
                rgbOutput = exml.EncryptData(inputElement, symAlg, true);
                ed = new EncryptedData();
                ed.Type = EncryptedXml.XmlEncElementUrl;
                ed.EncryptionMethod = GetSymEncryptionMethod();
                ed.KeyInfo = new KeyInfo();

                ek = new EncryptedKey();
                ek.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSA15Url);
                ek.KeyInfo = new KeyInfo();
                ek.CipherData = new CipherData();
                ek.CipherData.CipherValue = EncryptedXml.EncryptKey(symAlg.Key, rsa, UseOAEP);
            }

            kin = new KeyInfoName();
            kin.Value = _keyName;
            ek.KeyInfo.AddClause(kin);
            kek = new KeyInfoEncryptedKey(ek);
            ed.KeyInfo.AddClause(kek);
            ed.CipherData = new CipherData();
            ed.CipherData.CipherValue = rgbOutput;
            EncryptedXml.ReplaceElement(inputElement, ed, true);

            rsa.Clear();

            // Get node from the document
            foreach (XmlNode node2 in xmlDocument.ChildNodes)
                if (node2.NodeType == XmlNodeType.Element)
                    foreach (XmlNode node3 in node2.ChildNodes) // node2 is the "foo" node
                        if (node3.NodeType == XmlNodeType.Element)
                            return node3; // node3 is the "EncryptedData" node
            return null;
        }

        public void AddKey(int keySize, bool exportable)
        {
            RSACryptoServiceProvider rsa = GetCryptoServiceProvider(exportable, false);
            rsa.KeySize = keySize;
            rsa.PersistKeyInCsp = true;
            rsa.Clear();
        }

        public void DeleteKey()
        {
            RSACryptoServiceProvider rsa = GetCryptoServiceProvider(false, true);
            rsa.PersistKeyInCsp = false;
            rsa.Clear();
        }

        public void ImportKey(string xmlFileName, bool exportable)
        {
            RSACryptoServiceProvider rsa = GetCryptoServiceProvider(exportable, false);
            rsa.FromXmlString(File.ReadAllText(xmlFileName));
            rsa.PersistKeyInCsp = true;
            rsa.Clear();
        }

        public void ExportKey(string xmlFileName, bool includePrivateParameters)
        {
            RSACryptoServiceProvider rsa = GetCryptoServiceProvider(false, false);
            string xmlString = rsa.ToXmlString(includePrivateParameters);
            File.WriteAllText(xmlFileName, xmlString);
            rsa.Clear();
        }

        public string KeyContainerName { get { return _keyContainerName; } }
        public string CspProviderName { get { return _cspProviderName; } }
        public bool UseMachineContainer { get { return _useMachineContainer; } }
        public bool UseOAEP { get { return _useOAEP; } }
        public bool UseFIPS { get { return _useFIPS; } }

        public override void Initialize(string name, NameValueCollection configurationValues)
        {
            base.Initialize(name, configurationValues);

            _keyName = "Rsa Key";
            _keyContainerName = configurationValues["keyContainerName"];
            configurationValues.Remove("keyContainerName");
            if (_keyContainerName == null || _keyContainerName.Length < 1)
                _keyContainerName = DefaultRsaKeyContainerName;

            _cspProviderName = configurationValues["cspProviderName"];
            configurationValues.Remove("cspProviderName");
            _useMachineContainer = GetBooleanValue(configurationValues, "useMachineContainer", true);
            _useOAEP = GetBooleanValue(configurationValues, "useOAEP", false);
            _useFIPS = GetBooleanValue(configurationValues, "useFIPS", false);
            if (configurationValues.Count > 0)
                throw new ConfigurationErrorsException(string.Format(SR.Unrecognized_initialization_value, configurationValues.GetKey(0)));
        }

        public RSAParameters RsaPublicKey { get { return GetCryptoServiceProvider(false, false).ExportParameters(false); } }

        private RSACryptoServiceProvider GetCryptoServiceProvider(bool exportable, bool keyMustExist)
        {
            try
            {
                CspParameters csp = new CspParameters();
                csp.KeyContainerName = KeyContainerName;
                csp.KeyNumber = 1;
                csp.ProviderType = 1; // Dev10 Bug #548719: Explicitly require "RSA Full (Signature and Key Exchange)"

                if (CspProviderName != null && CspProviderName.Length > 0)
                    csp.ProviderName = CspProviderName;

                if (UseMachineContainer)
                    csp.Flags |= CspProviderFlags.UseMachineKeyStore;
                if (!exportable && !keyMustExist)
                    csp.Flags |= CspProviderFlags.UseNonExportableKey;
                if (keyMustExist)
                    csp.Flags |= CspProviderFlags.UseExistingKey;

                return new RSACryptoServiceProvider(csp);

            }
            catch
            {
                // On NetFX (Desktop) we try to P/Invoke directly to Windows to get a "better" exception
                // ThrowBetterException(keyMustExist);
                throw;
            }
        }

        private byte[] GetRandomKey()
        {
            byte[] buf = new byte[24];
            (new RNGCryptoServiceProvider()).GetBytes(buf);
            return buf;
        }

        private static bool GetBooleanValue(NameValueCollection configurationValues, string valueName, bool defaultValue)
        {
            string s = configurationValues[valueName];
            if (s == null)
                return defaultValue;
            configurationValues.Remove(valueName);
            if (s == "true")
                return true;
            if (s == "false")
                return false;
            throw new ConfigurationErrorsException(string.Format(SR.Config_invalid_boolean_attribute, valueName));
        }

        private SymmetricAlgorithm GetSymAlgorithmProvider()
        {
            SymmetricAlgorithm symAlg;

            if (UseFIPS)
            {
                // AesCryptoServiceProvider implementation is FIPS certified
                symAlg = new AesCryptoServiceProvider();
            }
            else
            {
                // Use the 3DES. FIPS obsolated 3DES
                symAlg = new TripleDESCryptoServiceProvider();

                byte[] rgbKey1 = GetRandomKey();
                symAlg.Key = rgbKey1;
                symAlg.Mode = CipherMode.ECB;
                symAlg.Padding = PaddingMode.PKCS7;
            }

            return symAlg;
        }

        private EncryptionMethod GetSymEncryptionMethod()
        {
            return UseFIPS ? new EncryptionMethod(EncryptedXml.XmlEncAES256Url) :
                             new EncryptionMethod(EncryptedXml.XmlEncTripleDESUrl);
        }
    }
#endif
}
