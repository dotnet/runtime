// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    internal static class Utils
    {
        // The maximum number of characters in an XML document (0 means no limit).
        internal const int MaxCharactersInDocument = 0;

        // The entity expansion limit. This is used to prevent entity expansion denial of service attacks.
        internal const long MaxCharactersFromEntities = (long)1e7;

        // The default XML Dsig recursion limit.
        // This should be within limits of real world scenarios.
        // Keeping this number low will preserve some stack space
        internal const int XmlDsigSearchDepth = 20;

        private static bool HasNamespace(XmlElement element, string prefix, string value)
        {
            if (IsCommittedNamespace(element, prefix, value)) return true;
            if (element.Prefix == prefix && element.NamespaceURI == value) return true;
            return false;
        }

        // A helper function that determines if a namespace node is a committed attribute
        internal static bool IsCommittedNamespace(XmlElement element, string prefix, string value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            string name = ((prefix.Length > 0) ? "xmlns:" + prefix : "xmlns");
            if (element.HasAttribute(name) && element.GetAttribute(name) == value) return true;
            return false;
        }

        internal static bool IsRedundantNamespace(XmlElement element, string prefix, string value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            XmlNode ancestorNode = ((XmlNode)element).ParentNode;
            while (ancestorNode != null)
            {
                XmlElement ancestorElement = ancestorNode as XmlElement;
                if (ancestorElement != null)
                    if (HasNamespace(ancestorElement, prefix, value)) return true;
                ancestorNode = ancestorNode.ParentNode;
            }

            return false;
        }

        internal static string GetAttribute(XmlElement element, string localName, string namespaceURI)
        {
            string s = (element.HasAttribute(localName) ? element.GetAttribute(localName) : null);
            if (s == null && element.HasAttribute(localName, namespaceURI))
                s = element.GetAttribute(localName, namespaceURI);
            return s;
        }

        internal static bool HasAttribute(XmlElement element, string localName, string namespaceURI)
        {
            return element.HasAttribute(localName) || element.HasAttribute(localName, namespaceURI);
        }

        internal static bool VerifyAttributes(XmlElement element, string expectedAttrName)
        {
            return VerifyAttributes(element, expectedAttrName == null ? null : new string[] { expectedAttrName });
        }

        internal static bool VerifyAttributes(XmlElement element, string[] expectedAttrNames)
        {
            foreach (XmlAttribute attr in element.Attributes)
            {
                // There are a few Xml Special Attributes that are always allowed on any node. Make sure we allow those here.
                bool attrIsAllowed = attr.Name == "xmlns" || attr.Name.StartsWith("xmlns:") || attr.Name == "xml:space" || attr.Name == "xml:lang" || attr.Name == "xml:base";
                int expectedInd = 0;
                while (!attrIsAllowed && expectedAttrNames != null && expectedInd < expectedAttrNames.Length)
                {
                    attrIsAllowed = attr.Name == expectedAttrNames[expectedInd];
                    expectedInd++;
                }
                if (!attrIsAllowed)
                    return false;
            }
            return true;
        }

        internal static bool IsNamespaceNode(XmlNode n)
        {
            return n.NodeType == XmlNodeType.Attribute && (n.Prefix.Equals("xmlns") || (n.Prefix.Length == 0 && n.LocalName.Equals("xmlns")));
        }

        internal static bool IsXmlNamespaceNode(XmlNode n)
        {
            return n.NodeType == XmlNodeType.Attribute && n.Prefix.Equals("xml");
        }

        // We consider xml:space style attributes as default namespace nodes since they obey the same propagation rules
        internal static bool IsDefaultNamespaceNode(XmlNode n)
        {
            bool b1 = n.NodeType == XmlNodeType.Attribute && n.Prefix.Length == 0 && n.LocalName.Equals("xmlns");
            bool b2 = IsXmlNamespaceNode(n);
            return b1 || b2;
        }

        internal static bool IsEmptyDefaultNamespaceNode(XmlNode n)
        {
            return IsDefaultNamespaceNode(n) && n.Value.Length == 0;
        }

        internal static string GetNamespacePrefix(XmlAttribute a)
        {
            Debug.Assert(IsNamespaceNode(a) || IsXmlNamespaceNode(a));
            return a.Prefix.Length == 0 ? string.Empty : a.LocalName;
        }

        internal static bool HasNamespacePrefix(XmlAttribute a, string nsPrefix)
        {
            return GetNamespacePrefix(a).Equals(nsPrefix);
        }

        internal static bool IsNonRedundantNamespaceDecl(XmlAttribute a, XmlAttribute nearestAncestorWithSamePrefix)
        {
            if (nearestAncestorWithSamePrefix == null)
                return !IsEmptyDefaultNamespaceNode(a);
            else
                return !nearestAncestorWithSamePrefix.Value.Equals(a.Value);
        }

        internal static bool IsXmlPrefixDefinitionNode(XmlAttribute a)
        {
            return false;
            //            return a.Prefix.Equals("xmlns") && a.LocalName.Equals("xml") && a.Value.Equals(NamespaceUrlForXmlPrefix);
        }

        internal static string DiscardWhiteSpaces(string inputBuffer)
        {
            return DiscardWhiteSpaces(inputBuffer, 0, inputBuffer.Length);
        }


        internal static string DiscardWhiteSpaces(string inputBuffer, int inputOffset, int inputCount)
        {
            int i, iCount = 0;
            for (i = 0; i < inputCount; i++)
                if (char.IsWhiteSpace(inputBuffer[inputOffset + i])) iCount++;
            char[] rgbOut = new char[inputCount - iCount];
            iCount = 0;
            for (i = 0; i < inputCount; i++)
                if (!char.IsWhiteSpace(inputBuffer[inputOffset + i]))
                {
                    rgbOut[iCount++] = inputBuffer[inputOffset + i];
                }
            return new string(rgbOut);
        }

        internal static void SBReplaceCharWithString(StringBuilder sb, char oldChar, string newString)
        {
            int i = 0;
            int newStringLength = newString.Length;
            while (i < sb.Length)
            {
                if (sb[i] == oldChar)
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, newString);
                    i += newStringLength;
                }
                else i++;
            }
        }

        internal static XmlReader PreProcessStreamInput(Stream inputStream, XmlResolver xmlResolver, string baseUri)
        {
            XmlReaderSettings settings = GetSecureXmlReaderSettings(xmlResolver);
            XmlReader reader = XmlReader.Create(inputStream, settings, baseUri);
            return reader;
        }

        internal static XmlReaderSettings GetSecureXmlReaderSettings(XmlResolver xmlResolver)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = xmlResolver;
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.MaxCharactersFromEntities = MaxCharactersFromEntities;
            settings.MaxCharactersInDocument = MaxCharactersInDocument;
            return settings;
        }

        internal static XmlDocument PreProcessDocumentInput(XmlDocument document, XmlResolver xmlResolver, string baseUri)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            MyXmlDocument doc = new MyXmlDocument();
            doc.PreserveWhitespace = document.PreserveWhitespace;

            // Normalize the document
            using (TextReader stringReader = new StringReader(document.OuterXml))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.XmlResolver = xmlResolver;
                settings.DtdProcessing = DtdProcessing.Parse;
                settings.MaxCharactersFromEntities = MaxCharactersFromEntities;
                settings.MaxCharactersInDocument = MaxCharactersInDocument;
                XmlReader reader = XmlReader.Create(stringReader, settings, baseUri);
                doc.Load(reader);
            }
            return doc;
        }

        internal static XmlDocument PreProcessElementInput(XmlElement elem, XmlResolver xmlResolver, string baseUri)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            MyXmlDocument doc = new MyXmlDocument();
            doc.PreserveWhitespace = true;
            // Normalize the document
            using (TextReader stringReader = new StringReader(elem.OuterXml))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.XmlResolver = xmlResolver;
                settings.DtdProcessing = DtdProcessing.Parse;
                settings.MaxCharactersFromEntities = MaxCharactersFromEntities;
                settings.MaxCharactersInDocument = MaxCharactersInDocument;
                XmlReader reader = XmlReader.Create(stringReader, settings, baseUri);
                doc.Load(reader);
            }
            return doc;
        }

        internal static XmlDocument DiscardComments(XmlDocument document)
        {
            XmlNodeList nodeList = document.SelectNodes("//comment()");
            if (nodeList != null)
            {
                foreach (XmlNode node1 in nodeList)
                {
                    node1.ParentNode.RemoveChild(node1);
                }
            }
            return document;
        }

        internal static XmlNodeList AllDescendantNodes(XmlNode node, bool includeComments)
        {
            CanonicalXmlNodeList nodeList = new CanonicalXmlNodeList();
            CanonicalXmlNodeList elementList = new CanonicalXmlNodeList();
            CanonicalXmlNodeList attribList = new CanonicalXmlNodeList();
            CanonicalXmlNodeList namespaceList = new CanonicalXmlNodeList();

            int index = 0;
            elementList.Add(node);

            do
            {
                XmlNode rootNode = (XmlNode)elementList[index];
                // Add the children nodes
                XmlNodeList childNodes = rootNode.ChildNodes;
                if (childNodes != null)
                {
                    foreach (XmlNode node1 in childNodes)
                    {
                        if (includeComments || (!(node1 is XmlComment)))
                        {
                            elementList.Add(node1);
                        }
                    }
                }
                // Add the attribute nodes
                XmlAttributeCollection attribNodes = rootNode.Attributes;
                if (attribNodes != null)
                {
                    foreach (XmlNode attribNode in rootNode.Attributes)
                    {
                        if (attribNode.LocalName == "xmlns" || attribNode.Prefix == "xmlns")
                            namespaceList.Add(attribNode);
                        else
                            attribList.Add(attribNode);
                    }
                }
                index++;
            } while (index < elementList.Count);
            foreach (XmlNode elementNode in elementList)
            {
                nodeList.Add(elementNode);
            }
            foreach (XmlNode attribNode in attribList)
            {
                nodeList.Add(attribNode);
            }
            foreach (XmlNode namespaceNode in namespaceList)
            {
                nodeList.Add(namespaceNode);
            }

            return nodeList;
        }

        internal static bool NodeInList(XmlNode node, XmlNodeList nodeList)
        {
            foreach (XmlNode nodeElem in nodeList)
            {
                if (nodeElem == node) return true;
            }
            return false;
        }

        internal static string GetIdFromLocalUri(string uri, out bool discardComments)
        {
            string idref = uri.Substring(1);
            // initialize the return value
            discardComments = true;

            // Deal with XPointer of type #xpointer(id("ID")). Other XPointer support isn't handled here and is anyway optional
            if (idref.StartsWith("xpointer(id(", StringComparison.Ordinal))
            {
                int startId = idref.IndexOf("id(", StringComparison.Ordinal);
                int endId = idref.IndexOf(')');
                if (endId < 0 || endId < startId + 3)
                    throw new CryptographicException(SR.Cryptography_Xml_InvalidReference);
                idref = idref.Substring(startId + 3, endId - startId - 3);
                idref = idref.Replace("\'", "");
                idref = idref.Replace("\"", "");
                discardComments = false;
            }
            return idref;
        }

        internal static string ExtractIdFromLocalUri(string uri)
        {
            string idref = uri.Substring(1);

            // Deal with XPointer of type #xpointer(id("ID")). Other XPointer support isn't handled here and is anyway optional
            if (idref.StartsWith("xpointer(id(", StringComparison.Ordinal))
            {
                int startId = idref.IndexOf("id(", StringComparison.Ordinal);
                int endId = idref.IndexOf(')');
                if (endId < 0 || endId < startId + 3)
                    throw new CryptographicException(SR.Cryptography_Xml_InvalidReference);
                idref = idref.Substring(startId + 3, endId - startId - 3);
                idref = idref.Replace("\'", "");
                idref = idref.Replace("\"", "");
            }
            return idref;
        }

        // This removes all children of an element.
        internal static void RemoveAllChildren(XmlElement inputElement)
        {
            XmlNode child = inputElement.FirstChild;
            XmlNode sibling = null;

            while (child != null)
            {
                sibling = child.NextSibling;
                inputElement.RemoveChild(child);
                child = sibling;
            }
        }

        // Writes one stream (starting from the current position) into
        // an output stream, connecting them up and reading until
        // hitting the end of the input stream.
        // returns the number of bytes copied
        internal static long Pump(Stream input, Stream output)
        {
            // Use MemoryStream's WriteTo(Stream) method if possible
            MemoryStream inputMS = input as MemoryStream;
            if (inputMS != null && inputMS.Position == 0)
            {
                inputMS.WriteTo(output);
                return inputMS.Length;
            }

            const int count = 4096;
            byte[] bytes = new byte[count];
            int numBytes;
            long totalBytes = 0;

            while ((numBytes = input.Read(bytes, 0, count)) > 0)
            {
                output.Write(bytes, 0, numBytes);
                totalBytes += numBytes;
            }

            return totalBytes;
        }

        internal static Hashtable TokenizePrefixListString(string s)
        {
            Hashtable set = new Hashtable();
            if (s != null)
            {
                string[] prefixes = s.Split(null);
                foreach (string prefix in prefixes)
                {
                    if (prefix.Equals("#default"))
                    {
                        set.Add(string.Empty, true);
                    }
                    else if (prefix.Length > 0)
                    {
                        set.Add(prefix, true);
                    }
                }
            }
            return set;
        }

        internal static string EscapeWhitespaceData(string data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(data);
            Utils.SBReplaceCharWithString(sb, (char)13, "&#xD;");
            return sb.ToString();
        }

        internal static string EscapeTextData(string data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(data);
            sb.Replace("&", "&amp;");
            sb.Replace("<", "&lt;");
            sb.Replace(">", "&gt;");
            SBReplaceCharWithString(sb, (char)13, "&#xD;");
            return sb.ToString();
        }

        internal static string EscapeCData(string data)
        {
            return EscapeTextData(data);
        }

        internal static string EscapeAttributeValue(string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(value);
            sb.Replace("&", "&amp;");
            sb.Replace("<", "&lt;");
            sb.Replace("\"", "&quot;");
            SBReplaceCharWithString(sb, (char)9, "&#x9;");
            SBReplaceCharWithString(sb, (char)10, "&#xA;");
            SBReplaceCharWithString(sb, (char)13, "&#xD;");
            return sb.ToString();
        }

        internal static XmlDocument GetOwnerDocument(XmlNodeList nodeList)
        {
            foreach (XmlNode node in nodeList)
            {
                if (node.OwnerDocument != null)
                    return node.OwnerDocument;
            }
            return null;
        }

        internal static void AddNamespaces(XmlElement elem, CanonicalXmlNodeList namespaces)
        {
            if (namespaces != null)
            {
                foreach (XmlNode attrib in namespaces)
                {
                    string name = ((attrib.Prefix.Length > 0) ? attrib.Prefix + ":" + attrib.LocalName : attrib.LocalName);
                    // Skip the attribute if one with the same qualified name already exists
                    if (elem.HasAttribute(name) || (name.Equals("xmlns") && elem.Prefix.Length == 0)) continue;
                    XmlAttribute nsattrib = (XmlAttribute)elem.OwnerDocument.CreateAttribute(name);
                    nsattrib.Value = attrib.Value;
                    elem.SetAttributeNode(nsattrib);
                }
            }
        }

        internal static void AddNamespaces(XmlElement elem, Hashtable namespaces)
        {
            if (namespaces != null)
            {
                foreach (string key in namespaces.Keys)
                {
                    if (elem.HasAttribute(key)) continue;
                    XmlAttribute nsattrib = (XmlAttribute)elem.OwnerDocument.CreateAttribute(key);
                    nsattrib.Value = namespaces[key] as string;
                    elem.SetAttributeNode(nsattrib);
                }
            }
        }

        // This method gets the attributes that should be propagated
        internal static CanonicalXmlNodeList GetPropagatedAttributes(XmlElement elem)
        {
            if (elem == null)
                return null;

            CanonicalXmlNodeList namespaces = new CanonicalXmlNodeList();
            XmlNode ancestorNode = elem;
            bool bDefNamespaceToAdd = true;

            while (ancestorNode != null)
            {
                XmlElement ancestorElement = ancestorNode as XmlElement;
                if (ancestorElement == null)
                {
                    ancestorNode = ancestorNode.ParentNode;
                    continue;
                }
                if (!Utils.IsCommittedNamespace(ancestorElement, ancestorElement.Prefix, ancestorElement.NamespaceURI))
                {
                    // Add the namespace attribute to the collection if needed
                    if (!Utils.IsRedundantNamespace(ancestorElement, ancestorElement.Prefix, ancestorElement.NamespaceURI))
                    {
                        string name = ((ancestorElement.Prefix.Length > 0) ? "xmlns:" + ancestorElement.Prefix : "xmlns");
                        XmlAttribute nsattrib = elem.OwnerDocument.CreateAttribute(name);
                        nsattrib.Value = ancestorElement.NamespaceURI;
                        namespaces.Add(nsattrib);
                    }
                }
                if (ancestorElement.HasAttributes)
                {
                    XmlAttributeCollection attribs = ancestorElement.Attributes;
                    foreach (XmlAttribute attrib in attribs)
                    {
                        // Add a default namespace if necessary
                        if (bDefNamespaceToAdd && attrib.LocalName == "xmlns")
                        {
                            XmlAttribute nsattrib = elem.OwnerDocument.CreateAttribute("xmlns");
                            nsattrib.Value = attrib.Value;
                            namespaces.Add(nsattrib);
                            bDefNamespaceToAdd = false;
                            continue;
                        }
                        // retain the declarations of type 'xml:*' as well
                        if (attrib.Prefix == "xmlns" || attrib.Prefix == "xml")
                        {
                            namespaces.Add(attrib);
                            continue;
                        }
                        if (attrib.NamespaceURI.Length > 0)
                        {
                            if (!Utils.IsCommittedNamespace(ancestorElement, attrib.Prefix, attrib.NamespaceURI))
                            {
                                // Add the namespace attribute to the collection if needed
                                if (!Utils.IsRedundantNamespace(ancestorElement, attrib.Prefix, attrib.NamespaceURI))
                                {
                                    string name = ((attrib.Prefix.Length > 0) ? "xmlns:" + attrib.Prefix : "xmlns");
                                    XmlAttribute nsattrib = elem.OwnerDocument.CreateAttribute(name);
                                    nsattrib.Value = attrib.NamespaceURI;
                                    namespaces.Add(nsattrib);
                                }
                            }
                        }
                    }
                }
                ancestorNode = ancestorNode.ParentNode;
            }

            return namespaces;
        }

        // output of this routine is always big endian
        internal static byte[] ConvertIntToByteArray(int dwInput)
        {
            byte[] rgbTemp = new byte[8]; // int can never be greater than Int64
            int t1;  // t1 is remaining value to account for
            int t2;  // t2 is t1 % 256
            int i = 0;

            if (dwInput == 0) return new byte[1];
            t1 = dwInput;
            while (t1 > 0)
            {
                t2 = t1 % 256;
                rgbTemp[i] = (byte)t2;
                t1 = (t1 - t2) / 256;
                i++;
            }
            // Now, copy only the non-zero part of rgbTemp and reverse
            byte[] rgbOutput = new byte[i];
            // copy and reverse in one pass
            for (int j = 0; j < i; j++)
            {
                rgbOutput[j] = rgbTemp[i - j - 1];
            }
            return rgbOutput;
        }

        internal static int ConvertByteArrayToInt(byte[] input)
        {
            // Input to this routine is always big endian
            int dwOutput = 0;
            for (int i = 0; i < input.Length; i++)
            {
                dwOutput *= 256;
                dwOutput += input[i];
            }
            return (dwOutput);
        }

        internal static int GetHexArraySize(byte[] hex)
        {
            int index = hex.Length;
            while (index-- > 0)
            {
                if (hex[index] != 0)
                    break;
            }
            return index + 1;
        }

        // Mimic the behavior of the X509IssuerSerial constructor with null and empty checks
        internal static X509IssuerSerial CreateX509IssuerSerial(string issuerName, string serialNumber)
        {
            if (issuerName == null || issuerName.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullString, nameof(issuerName));
            if (serialNumber == null || serialNumber.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullString, nameof(serialNumber));

            return new X509IssuerSerial()
            {
                IssuerName = issuerName,
                SerialNumber = serialNumber
            };
        }

        internal static X509Certificate2Collection BuildBagOfCerts(KeyInfoX509Data keyInfoX509Data, CertUsageType certUsageType)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            ArrayList decryptionIssuerSerials = (certUsageType == CertUsageType.Decryption ? new ArrayList() : null);
            if (keyInfoX509Data.Certificates != null)
            {
                foreach (X509Certificate2 certificate in keyInfoX509Data.Certificates)
                {
                    switch (certUsageType)
                    {
                        case CertUsageType.Verification:
                            collection.Add(certificate);
                            break;
                        case CertUsageType.Decryption:
                            decryptionIssuerSerials.Add(CreateX509IssuerSerial(certificate.IssuerName.Name, certificate.SerialNumber));
                            break;
                    }
                }
            }

            if (keyInfoX509Data.SubjectNames == null && keyInfoX509Data.IssuerSerials == null &&
                keyInfoX509Data.SubjectKeyIds == null && decryptionIssuerSerials == null)
                return collection;

            // Open LocalMachine and CurrentUser "Other People"/"My" stores.

            X509Store[] stores = new X509Store[2];
            string storeName = (certUsageType == CertUsageType.Verification ? "AddressBook" : "My");
            stores[0] = new X509Store(storeName, StoreLocation.CurrentUser);
            stores[1] = new X509Store(storeName, StoreLocation.LocalMachine);

            for (int index = 0; index < stores.Length; index++)
            {
                if (stores[index] != null)
                {
                    X509Certificate2Collection filters = null;
                    // We don't care if we can't open the store.
                    try
                    {
                        stores[index].Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                        filters = stores[index].Certificates;
                        stores[index].Close();
                        if (keyInfoX509Data.SubjectNames != null)
                        {
                            foreach (string subjectName in keyInfoX509Data.SubjectNames)
                            {
                                filters = filters.Find(X509FindType.FindBySubjectDistinguishedName, subjectName, false);
                            }
                        }
                        if (keyInfoX509Data.IssuerSerials != null)
                        {
                            foreach (X509IssuerSerial issuerSerial in keyInfoX509Data.IssuerSerials)
                            {
                                filters = filters.Find(X509FindType.FindByIssuerDistinguishedName, issuerSerial.IssuerName, false);
                                filters = filters.Find(X509FindType.FindBySerialNumber, issuerSerial.SerialNumber, false);
                            }
                        }
                        if (keyInfoX509Data.SubjectKeyIds != null)
                        {
                            foreach (byte[] ski in keyInfoX509Data.SubjectKeyIds)
                            {
                                string hex = EncodeHexString(ski);
                                filters = filters.Find(X509FindType.FindBySubjectKeyIdentifier, hex, false);
                            }
                        }
                        if (decryptionIssuerSerials != null)
                        {
                            foreach (X509IssuerSerial issuerSerial in decryptionIssuerSerials)
                            {
                                filters = filters.Find(X509FindType.FindByIssuerDistinguishedName, issuerSerial.IssuerName, false);
                                filters = filters.Find(X509FindType.FindBySerialNumber, issuerSerial.SerialNumber, false);
                            }
                        }
                    }
                    // Store doesn't exist, no read permissions, other system error
                    catch (CryptographicException) { }
                    // Opening LocalMachine stores (other than Root or CertificateAuthority) on Linux
                    catch (PlatformNotSupportedException) { }

                    if (filters != null)
                        collection.AddRange(filters);
                }
            }

            return collection;
        }

        internal static string EncodeHexString(byte[] sArray)
        {
            return HexConverter.ToString(sArray);
        }

        internal static byte[] DecodeHexString(string s)
        {
            string hexString = Utils.DiscardWhiteSpaces(s);
            uint cbHex = (uint)hexString.Length / 2;
            byte[] hex = new byte[cbHex];
            int i = 0;
            for (int index = 0; index < cbHex; index++)
            {
                hex[index] = (byte)((HexConverter.FromChar(hexString[i]) << 4) | HexConverter.FromChar(hexString[i + 1]));
                i += 2;
            }
            return hex;
        }

        internal static bool IsSelfSigned(X509Chain chain)
        {
            X509ChainElementCollection elements = chain.ChainElements;
            if (elements.Count != 1)
                return false;
            X509Certificate2 certificate = elements[0].Certificate;
            if (string.Equals(certificate.SubjectName.Name, certificate.IssuerName.Name, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        internal static AsymmetricAlgorithm GetAnyPublicKey(X509Certificate2 certificate)
        {
            AsymmetricAlgorithm algorithm = (AsymmetricAlgorithm)certificate.GetRSAPublicKey() ?? certificate.GetECDsaPublicKey();

#if NETCOREAPP
            if (algorithm is null && !OperatingSystem.IsTvOS() && !OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst())
            {
                algorithm = certificate.GetDSAPublicKey();
            }
#endif

            return algorithm;
        }

        internal const int MaxTransformsPerReference = 10;
        internal const int MaxReferencesPerSignedInfo = 100;
    }
}
