// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Xml
{
    internal static class CryptoHelpers
    {
        private static readonly char[] _invalidChars = new char[] { ',', '`', '[', '*', '&' };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 needed for compat.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "HMACMD5 needed for compat.")]
        public static object CreateFromKnownName(string name) =>
            name switch
            {
                "http://www.w3.org/TR/2001/REC-xml-c14n-20010315" => new XmlDsigC14NTransform(),
                "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments" => new XmlDsigC14NWithCommentsTransform(),
                "http://www.w3.org/2001/10/xml-exc-c14n#" => new XmlDsigExcC14NTransform(),
                "http://www.w3.org/2001/10/xml-exc-c14n#WithComments" => new XmlDsigExcC14NWithCommentsTransform(),
                "http://www.w3.org/2000/09/xmldsig#base64" => new XmlDsigBase64Transform(),
                "http://www.w3.org/TR/1999/REC-xpath-19991116" => new XmlDsigXPathTransform(),
                "http://www.w3.org/TR/1999/REC-xslt-19991116" => new XmlDsigXsltTransform(),
                "http://www.w3.org/2000/09/xmldsig#enveloped-signature" => new XmlDsigEnvelopedSignatureTransform(),
                "http://www.w3.org/2002/07/decrypt#XML" => new XmlDecryptionTransform(),
                "urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform" => new XmlLicenseTransform(),
                "http://www.w3.org/2000/09/xmldsig# X509Data" => new KeyInfoX509Data(),
                "http://www.w3.org/2000/09/xmldsig# KeyName" => new KeyInfoName(),
                "http://www.w3.org/2000/09/xmldsig# KeyValue/DSAKeyValue" => new DSAKeyValue(),
                "http://www.w3.org/2000/09/xmldsig# KeyValue/RSAKeyValue" => new RSAKeyValue(),
                "http://www.w3.org/2000/09/xmldsig# RetrievalMethod" => new KeyInfoRetrievalMethod(),
                "http://www.w3.org/2001/04/xmlenc# EncryptedKey" => new KeyInfoEncryptedKey(),
                "http://www.w3.org/2000/09/xmldsig#dsa-sha1" => new DSASignatureDescription(),
                "System.Security.Cryptography.DSASignatureDescription" => new DSASignatureDescription(),
                "http://www.w3.org/2000/09/xmldsig#rsa-sha1" => new RSAPKCS1SHA1SignatureDescription(),
                "System.Security.Cryptography.RSASignatureDescription" => new RSAPKCS1SHA1SignatureDescription(),
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256" => new RSAPKCS1SHA256SignatureDescription(),
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384" => new RSAPKCS1SHA384SignatureDescription(),
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512" => new RSAPKCS1SHA512SignatureDescription(),

                // workarounds for issue https://github.com/dotnet/corefx/issues/16563
                // remove attribute from this method when removing them
                "http://www.w3.org/2000/09/xmldsig#sha1" => SHA1.Create(),
                "MD5" => MD5.Create(),
                "http://www.w3.org/2001/04/xmldsig-more#hmac-md5" => new HMACMD5(),
                "http://www.w3.org/2001/04/xmlenc#tripledes-cbc" => TripleDES.Create(),

                _ => null,
            };

        public static T CreateFromName<T>(string name) where T : class
        {
            if (name == null || name.IndexOfAny(_invalidChars) >= 0)
            {
                return null;
            }
            try
            {
                return (CreateFromKnownName(name) ?? CryptoConfig.CreateFromName(name)) as T;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
