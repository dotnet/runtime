// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Claims
{
    /// <summary>
    /// Defines the claim value types of the framework.
    /// </summary>
    public static class ClaimValueTypes
    {
        private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

        public const string Base64Binary = XmlSchemaNamespace + "#base64Binary";
        public const string Base64Octet = XmlSchemaNamespace + "#base64Octet";
        public const string Boolean = XmlSchemaNamespace + "#boolean";
        public const string Date = XmlSchemaNamespace + "#date";
        public const string DateTime = XmlSchemaNamespace + "#dateTime";
        public const string Double = XmlSchemaNamespace + "#double";
        public const string Fqbn = XmlSchemaNamespace + "#fqbn";
        public const string HexBinary = XmlSchemaNamespace + "#hexBinary";
        public const string Integer = XmlSchemaNamespace + "#integer";
        public const string Integer32 = XmlSchemaNamespace + "#integer32";
        public const string Integer64 = XmlSchemaNamespace + "#integer64";
        public const string Sid = XmlSchemaNamespace + "#sid";
        public const string String = XmlSchemaNamespace + "#string";
        public const string Time = XmlSchemaNamespace + "#time";
        public const string UInteger32 = XmlSchemaNamespace + "#uinteger32";
        public const string UInteger64 = XmlSchemaNamespace + "#uinteger64";

        private const string SoapSchemaNamespace = "http://schemas.xmlsoap.org/";

        public const string DnsName = SoapSchemaNamespace + "claims/dns";
        public const string Email = SoapSchemaNamespace + "ws/2005/05/identity/claims/emailaddress";
        public const string Rsa = SoapSchemaNamespace + "ws/2005/05/identity/claims/rsa";
        public const string UpnName = SoapSchemaNamespace + "claims/UPN";

        private const string XmlSignatureConstantsNamespace = "http://www.w3.org/2000/09/xmldsig#";

        public const string DsaKeyValue = XmlSignatureConstantsNamespace + "DSAKeyValue";
        public const string KeyInfo = XmlSignatureConstantsNamespace + "KeyInfo";
        public const string RsaKeyValue = XmlSignatureConstantsNamespace + "RSAKeyValue";

        private const string XQueryOperatorsNameSpace = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816";

        public const string DaytimeDuration = XQueryOperatorsNameSpace + "#dayTimeDuration";
        public const string YearMonthDuration = XQueryOperatorsNameSpace + "#yearMonthDuration";

        private const string Xacml10Namespace = "urn:oasis:names:tc:xacml:1.0";

        public const string Rfc822Name = Xacml10Namespace + ":data-type:rfc822Name";
        public const string X500Name = Xacml10Namespace + ":data-type:x500Name";
    }
}
