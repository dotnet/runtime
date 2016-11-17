// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Xml;
using System.Xml.XPath;
using System.Text;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Utility class to convert ECC keys into XML and back using a format similar to the one described
    ///     in RFC 4050 (http://www.ietf.org/rfc/rfc4050.txt).
    /// 
    ///     #RFC4050ECKeyFormat
    /// 
    ///     The format looks similar to the following:
    /// 
    ///         <ECDSAKeyValue xmlns="http://www.w3.org/2001/04/xmldsig-more#">
    ///             <DomainParameters>
    ///                 <NamedCurve URN="urn:oid:1.3.132.0.35" />
    ///             </DomainParameters>
    ///             <PublicKey>
    ///                 <X Value="0123456789..." xsi:type="PrimeFieldElemType" />
    ///                 <Y Value="0123456789..." xsi:type="PrimeFieldElemType" />
    ///             </PublicKey>
    ///         </ECDSAKeyValue>
    /// </summary>
    internal static class Rfc4050KeyFormatter
    {
        private const string DomainParametersRoot = "DomainParameters";
        private const string ECDHRoot = "ECDHKeyValue";
        private const string ECDsaRoot = "ECDSAKeyValue";
        private const string NamedCurveElement = "NamedCurve";
        private const string Namespace = "http://www.w3.org/2001/04/xmldsig-more#";
        private const string OidUrnPrefix = "urn:oid:";
        private const string PublicKeyRoot = "PublicKey";
        private const string UrnAttribute = "URN";
        private const string ValueAttribute = "Value";
        private const string XElement = "X";
        private const string YElement = "Y";

        private const string XsiTypeAttribute = "type";
        private const string XsiTypeAttributeValue = "PrimeFieldElemType";
        private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private const string XsiNamespacePrefix = "xsi";

        private const string ECDSA_P256_OID_VALUE = "1.2.840.10045.3.1.7"; // nistP256 or secP256r1
        private const string ECDSA_P384_OID_VALUE = "1.3.132.0.34"; // nistP384 or secP384r1
        private const string ECDSA_P521_OID_VALUE = "1.3.132.0.35"; // nistP521 or secP521r1

        private const string BCRYPT_ECC_CURVE_NISTP256 = "nistP256";
        private const string BCRYPT_ECC_CURVE_NISTP384 = "nistP384";
        private const string BCRYPT_ECC_CURVE_NISTP521 = "nistP521";

        /// <summary>
        ///     Restore a key from XML
        /// </summary>
        internal static ECParameters FromXml(string xml, out bool isEcdh)
        {
            Debug.Assert(xml != null);

            ECParameters parameters = new ECParameters();

            // Load the XML into an XPathNavigator to access sub elements
            using (TextReader textReader = new StringReader(xml))
            using (XmlTextReader xmlReader = new XmlTextReader(textReader))
            {
                XPathDocument document = new XPathDocument(xmlReader);
                XPathNavigator navigator = document.CreateNavigator();

                // Move into the root element - we don't do a specific namespace check here for compatibility
                // with XML that Windows generates.
                if (!navigator.MoveToFirstChild())
                {
                    throw new ArgumentException(SR.Cryptography_MissingDomainParameters);
                }

                // First figure out which algorithm this key belongs to
                parameters.Curve = ReadCurve(navigator, out isEcdh);

                // Then read out the public key value
                if (!navigator.MoveToNext(XPathNodeType.Element))
                {
                    throw new ArgumentException(SR.Cryptography_MissingPublicKey);
                }

                ReadPublicKey(navigator, ref parameters);
                return parameters;
            }
        }

        /// <summary>
        ///     Determine which ECC curve the key refers to
        /// </summary>
        private static ECCurve ReadCurve(XPathNavigator navigator, out bool isEcdh)
        {
            Debug.Assert(navigator != null);

            if (navigator.NamespaceURI != Namespace)
            {
                throw new ArgumentException(string.Format(SR.Cryptography_UnexpectedXmlNamespace, navigator.NamespaceURI, Namespace));
            }

            //
            // The name of the root element determines which algorithm to use, while the DomainParameters
            // element specifies which curve we should be using.
            //

            bool isDHKey = navigator.Name == ECDHRoot;
            bool isDsaKey = navigator.Name == ECDsaRoot;

            if (!isDHKey && !isDsaKey)
            {
                throw new ArgumentException(SR.Cryptography_UnknownEllipticCurveAlgorithm);
            }

            // Move into the DomainParameters element
            if (!navigator.MoveToFirstChild() || navigator.Name != DomainParametersRoot)
            {
                throw new ArgumentException(SR.Cryptography_MissingDomainParameters);
            }

            // Now move into the NamedCurve element
            if (!navigator.MoveToFirstChild() || navigator.Name != NamedCurveElement)
            {
                throw new ArgumentException(SR.Cryptography_MissingDomainParameters);
            }

            // And read its URN value
            if (!navigator.MoveToFirstAttribute() || navigator.Name != UrnAttribute || String.IsNullOrEmpty(navigator.Value))
            {
                throw new ArgumentException(SR.Cryptography_MissingDomainParameters);
            }

            string oidUrn = navigator.Value;

            if (!oidUrn.StartsWith(OidUrnPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.Cryptography_UnknownEllipticCurve);
            }

            // position the navigator at the end of the domain parameters
            navigator.MoveToParent();   // NamedCurve
            navigator.MoveToParent();   // DomainParameters

            // The out-bool only works because we have either/or.  If a third type of data is handled
            // then a more complex signal is required.
            Debug.Assert(isDHKey || isDsaKey);
            isEcdh = isDHKey;
            return ECCurve.CreateFromValue(oidUrn.Substring(OidUrnPrefix.Length));
        }

        /// <summary>
        ///     Read the x and y components of the public key
        /// </summary>
        private static void ReadPublicKey(XPathNavigator navigator, ref ECParameters parameters)
        {
            Debug.Assert(navigator != null);

            if (navigator.NamespaceURI != Namespace)
            {
                throw new ArgumentException(string.Format(SR.Cryptography_UnexpectedXmlNamespace, navigator.NamespaceURI, Namespace));
            }

            if (navigator.Name != PublicKeyRoot)
            {
                throw new ArgumentException(SR.Cryptography_MissingPublicKey);
            }

            // First get the x parameter
            if (!navigator.MoveToFirstChild() || navigator.Name != XElement)
            {
                throw new ArgumentException(SR.Cryptography_MissingPublicKey);
            }
            if (!navigator.MoveToFirstAttribute() || navigator.Name != ValueAttribute || String.IsNullOrEmpty(navigator.Value))
            {
                throw new ArgumentException(SR.Cryptography_MissingPublicKey);
            }

            BigInteger x = BigInteger.Parse(navigator.Value, CultureInfo.InvariantCulture);
            navigator.MoveToParent();

            // Then the y parameter
            if (!navigator.MoveToNext(XPathNodeType.Element) || navigator.Name != YElement)
            {
                throw new ArgumentException(SR.Cryptography_MissingPublicKey);
            }
            if (!navigator.MoveToFirstAttribute() || navigator.Name != ValueAttribute || String.IsNullOrEmpty(navigator.Value))
            {
                throw new ArgumentException(SR.Cryptography_MissingPublicKey);
            }

            BigInteger y = BigInteger.Parse(navigator.Value, CultureInfo.InvariantCulture);

            byte[] xBytes = x.ToByteArray();
            byte[] yBytes = y.ToByteArray();

            int xLen = xBytes.Length;
            int yLen = yBytes.Length;

            // If the last byte of X is 0x00 that's a padding byte by BigInteger to indicate X is
            // a positive number with the highest bit in the most significant byte set. We can't count
            // that in the length of the number.
            if (xLen > 0 && xBytes[xLen - 1] == 0)
            {
                xLen--;
            }

            // Ditto for Y.
            if (yLen > 0 && yBytes[yLen - 1] == 0)
            {
                yLen--;
            }

            // Q.X and Q.Y have to be the same length.  They ultimately have to be the right length for the curve,
            // but that requires more knowledge than we have. So we'll ask the system. If it doesn't know, just make
            // them match each other.
            int requiredLength = Math.Max(xLen, yLen);

            try
            {
                using (ECDsa ecdsa = ECDsa.Create(parameters.Curve))
                {
                    // Convert the bit value of keysize to a byte value.
                    // EC curves can have non-mod-8 keysizes (e.g. 521), so the +7 is really necessary.
                    int curveLength = (ecdsa.KeySize + 7) / 8;

                    // We could just use this answer, but if the user has formatted the input to be
                    // too long, maybe they know something we don't.
                    requiredLength = Math.Max(requiredLength, curveLength);
                }
            }
            catch (ArgumentException) { /* Curve had invalid data, like an empty OID */ }
            catch (CryptographicException) { /* The system failed to generate a key for the curve */ }
            catch (NotSupportedException) { /* An unknown curve type was requested */ }

            // There is a chance that the curve is known to Windows but only allowed for ECDH
            // (curve25519 is known to be in this state). Since RFC4050 is officially only
            // concerned with ECDSA, and the only known example of this problem does not have
            // an OID, it is not worth trying to generate the curve under ECDH as a fallback.

            // Since BigInteger does Little Endian and Array.Resize maintains indexes when growing,
            // just Array.Resize, then Array.Reverse. We could optimize this to be 1N instead of 2N,
            // but this isn't a very hot codepath, so use tried-and-true methods.
            Array.Resize(ref xBytes, requiredLength);
            Array.Resize(ref yBytes, requiredLength);
            Array.Reverse(xBytes);
            Array.Reverse(yBytes);

            parameters.Q.X = xBytes;
            parameters.Q.Y = yBytes;
        }

        /// <summary>
        ///     Serialize out information about the elliptic curve
        /// </summary>
        private static void WriteDomainParameters(XmlWriter writer, ref ECParameters parameters)
        {
            Debug.Assert(writer != null);

            Oid curveOid = parameters.Curve.Oid;

            if (!parameters.Curve.IsNamed || curveOid == null)
                throw new ArgumentException(SR.Cryptography_UnknownEllipticCurve);

            string oidValue = curveOid.Value;

            // If the OID didn't specify a value, use the mutable FriendlyName behavior of
            // resolving the value without throwing an exception.
            if (string.IsNullOrEmpty(oidValue))
            {
                // The name strings for the 3 NIST curves from Win7 changed in Win10, but the Win10
                // names are what we use. This fallback supports Win7-Win8.1 resolution
                switch (curveOid.FriendlyName)
                {
                    case BCRYPT_ECC_CURVE_NISTP256:
                        oidValue = ECDSA_P256_OID_VALUE;
                        break;

                    case BCRYPT_ECC_CURVE_NISTP384:
                        oidValue = ECDSA_P384_OID_VALUE;
                        break;

                    case BCRYPT_ECC_CURVE_NISTP521:
                        oidValue = ECDSA_P521_OID_VALUE;
                        break;

                    default:
                        Oid resolver = new Oid();
                        resolver.FriendlyName = curveOid.FriendlyName;
                        oidValue = resolver.Value;
                        break;
                }
            }

            if (string.IsNullOrEmpty(oidValue))
                throw new ArgumentException(SR.Cryptography_UnknownEllipticCurve);

            writer.WriteStartElement(DomainParametersRoot);

            // We always use OIDs for the named prime curves
            writer.WriteStartElement(NamedCurveElement);
            writer.WriteAttributeString(UrnAttribute, OidUrnPrefix + oidValue);
            writer.WriteEndElement();   // </NamedCurve>

            writer.WriteEndElement();   // </DomainParameters>
        }

        private static void WritePublicKeyValue(XmlWriter writer, ref ECParameters parameters)
        {
            Debug.Assert(writer != null);

            writer.WriteStartElement(PublicKeyRoot);

            byte[] providedX = parameters.Q.X;
            byte[] providedY = parameters.Q.Y;

            int xSize = providedX.Length;
            int ySize = providedY.Length;
            const byte SignBit = 0x80;

            // BigInteger will interpret a byte[] number as negative if the most significant bit is set.
            // Since we're still in Big Endian at this point that means checking val[0].
            // If the high bit is set, we need to extract into a byte[] with a padding zero to keep the
            // sign bit cleared.

            if ((providedX[0] & SignBit) == SignBit)
            {
                xSize++;
            }

            if ((providedY[0] & SignBit) == SignBit)
            {
                ySize++;
            }

            // We can't just use the arrays that are passed in even when the number wasn't negative,
            // because we need to reverse the bytes to load into BigInteger.
            byte[] xBytes = new byte[xSize];
            byte[] yBytes = new byte[ySize];

            // If the size grew then the offset will be 1, otherwise 0.
            Buffer.BlockCopy(providedX, 0, xBytes, xSize - providedX.Length, providedX.Length);
            Buffer.BlockCopy(providedY, 0, yBytes, ySize - providedY.Length, providedY.Length);

            Array.Reverse(xBytes);
            Array.Reverse(yBytes);

            BigInteger x = new BigInteger(xBytes);
            BigInteger y = new BigInteger(yBytes);

            writer.WriteStartElement(XElement);
            writer.WriteAttributeString(ValueAttribute, x.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteAttributeString(XsiNamespacePrefix, XsiTypeAttribute, XsiNamespace, XsiTypeAttributeValue);
            writer.WriteEndElement();   // </X>

            writer.WriteStartElement(YElement);
            writer.WriteAttributeString(ValueAttribute, y.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteAttributeString(XsiNamespacePrefix, XsiTypeAttribute, XsiNamespace, XsiTypeAttributeValue);
            writer.WriteEndElement();   // </Y>

            writer.WriteEndElement();   // </PublicKey>
        }

        /// <summary>
        ///     Convert a key to XML
        /// </summary>
        internal static string ToXml(ECParameters parameters, bool isEcdh)
        {
            parameters.Validate();

            StringBuilder keyXml = new StringBuilder();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.OmitXmlDeclaration = true;

            using (XmlWriter writer = XmlWriter.Create(keyXml, settings))
            {
                // The root element depends upon the type of key
                string rootElement = isEcdh ? ECDHRoot : ECDsaRoot;
                writer.WriteStartElement(rootElement, Namespace);

                WriteDomainParameters(writer, ref parameters);
                WritePublicKeyValue(writer, ref parameters);

                writer.WriteEndElement();   // root element
            }

            return keyXml.ToString();
        }
    }
}
