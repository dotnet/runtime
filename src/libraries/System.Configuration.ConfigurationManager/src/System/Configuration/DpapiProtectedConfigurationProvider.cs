// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Runtime.Versioning;

namespace System.Configuration
{
    [MinimumOSPlatform("windows7.0")]
    public sealed class DpapiProtectedConfigurationProvider : ProtectedConfigurationProvider
    {
        private bool _useMachineProtection = true;
        private string _keyEntropy;

        public override XmlNode Decrypt(XmlNode encryptedNode)
        {
            if (encryptedNode.NodeType != XmlNodeType.Element ||
                encryptedNode.Name != "EncryptedData")
            {
                throw new ConfigurationErrorsException(SR.DPAPI_bad_data);
            }

            XmlNode cipherNode = TraverseToChild(encryptedNode, "CipherData", false);
            if (cipherNode == null)
                throw new ConfigurationErrorsException(SR.DPAPI_bad_data);

            XmlNode cipherValue = TraverseToChild(cipherNode, "CipherValue", true);
            if (cipherValue == null)
                throw new ConfigurationErrorsException(SR.DPAPI_bad_data);

            string encText = cipherValue.InnerText;
            if (encText == null)
                throw new ConfigurationErrorsException(SR.DPAPI_bad_data);

            string decText = DecryptText(encText);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            xmlDocument.LoadXml(decText);
            return xmlDocument.DocumentElement;
        }

        public override XmlNode Encrypt(XmlNode node)
        {
            string text = node.OuterXml;
            string encText = EncryptText(text);
            string pre = @"<EncryptedData><CipherData><CipherValue>";
            string post = @"</CipherValue></CipherData></EncryptedData>";
            string xmlText = pre + encText + post;

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            xmlDocument.LoadXml(xmlText);
            return xmlDocument.DocumentElement;
        }

        private string EncryptText(string clearText)
        {
            if (clearText == null || clearText.Length < 1)
                return clearText;

            byte[] inputData = PrepareDataBlob(clearText);
            byte[] entropyData = PrepareDataBlob(_keyEntropy);

            byte[] encryptedData = ProtectedData.Protect(
                userData: inputData,
                optionalEntropy: entropyData,
                scope: UseMachineProtection ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(encryptedData);
        }

        private string DecryptText(string encText)
        {
            if (encText == null || encText.Length < 1)
                return encText;

            byte[] inputData = Convert.FromBase64String(encText);
            byte[] entropyData = PrepareDataBlob(_keyEntropy);

            byte[] decryptedData = ProtectedData.Unprotect(
                encryptedData: inputData,
                optionalEntropy: entropyData,
                scope: UseMachineProtection ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser);

            return Encoding.Unicode.GetString(decryptedData);
        }

        public bool UseMachineProtection { get { return _useMachineProtection; } }

        public override void Initialize(string name, NameValueCollection configurationValues)
        {
            base.Initialize(name, configurationValues);
            _useMachineProtection = GetBooleanValue(configurationValues, "useMachineProtection", true);
            _keyEntropy = configurationValues["keyEntropy"];
            configurationValues.Remove("keyEntropy");
            if (configurationValues.Count > 0)
                throw new ConfigurationErrorsException(SR.Format(SR.Unrecognized_initialization_value, configurationValues.GetKey(0)));
        }

        private static XmlNode TraverseToChild(XmlNode node, string name, bool onlyChild)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;
                if (child.Name == name)
                    return child; // found it!
                if (onlyChild)
                    return null;
            }

            return null;
        }

        private static byte[] PrepareDataBlob(string s)
        {
            return (s != null) ? Encoding.Unicode.GetBytes(s) : Array.Empty<byte>();
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
            throw new ConfigurationErrorsException(SR.Format(SR.Config_invalid_boolean_attribute, valueName));
        }
    }
}
