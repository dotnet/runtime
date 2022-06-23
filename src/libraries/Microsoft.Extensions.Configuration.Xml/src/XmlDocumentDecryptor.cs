// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// Class responsible for encrypting and decrypting XML.
    /// </summary>
    public class XmlDocumentDecryptor
    {
        /// <summary>
        /// Accesses the singleton decryptor instance.
        /// </summary>
        public static readonly XmlDocumentDecryptor Instance = new XmlDocumentDecryptor();

        private readonly Func<XmlDocument, EncryptedXml>? _encryptedXmlFactory;

        /// <summary>
        /// Initializes a XmlDocumentDecryptor.
        /// </summary>
        // don't create an instance of this directly
        protected XmlDocumentDecryptor()
        {
            // _encryptedXmlFactory stays null by default
        }

        // for testing only
        internal XmlDocumentDecryptor(Func<XmlDocument, EncryptedXml> encryptedXmlFactory)
        {
            _encryptedXmlFactory = encryptedXmlFactory;
        }

        private static bool ContainsEncryptedData(XmlDocument document)
        {
            // EncryptedXml will simply decrypt the document in-place without telling
            // us that it did so, so we need to perform a check to see if EncryptedXml
            // will actually do anything. The below check for an encrypted data blob
            // is the same one that EncryptedXml would have performed.
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("enc", "http://www.w3.org/2001/04/xmlenc#");
            return (document.SelectSingleNode("//enc:EncryptedData", namespaceManager) != null);
        }

        /// <summary>
        /// Returns an XmlReader that decrypts data transparently.
        /// </summary>
        public XmlReader CreateDecryptingXmlReader(Stream input, XmlReaderSettings? settings)
        {
            // XML-based configurations aren't really all that big, so we can buffer
            // the whole thing in memory while we determine decryption operations.
            var memStream = new MemoryStream();
            input.CopyTo(memStream);
            memStream.Position = 0;

            // First, consume the entire XmlReader as an XmlDocument.
            var document = new XmlDocument();
            using (var reader = XmlReader.Create(memStream, settings))
            {
                document.Load(reader);
            }
            memStream.Position = 0;

            if (ContainsEncryptedData(document))
            {
                // DecryptDocumentAndCreateXmlReader is not supported on 'browser',
                // but we only call it depending on the input XML document. If the document
                // is encrypted and this is running on 'browser', just let the PNSE throw.
#pragma warning disable CA1416
                return DecryptDocumentAndCreateXmlReader(document);
#pragma warning restore CA1416
            }
            else
            {
                // If no decryption would have taken place, return a new fresh reader
                // based on the memory stream (which doesn't need to be disposed).
                return XmlReader.Create(memStream, settings);
            }
        }

        /// <summary>
        /// Creates a reader that can decrypt an encrypted XML document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>An XmlReader which can read the document.</returns>
        [UnsupportedOSPlatform("browser")]
        protected virtual XmlReader DecryptDocumentAndCreateXmlReader(XmlDocument document)
        {
            // Perform the actual decryption step, updating the XmlDocument in-place.
            EncryptedXml encryptedXml = _encryptedXmlFactory?.Invoke(document) ?? new EncryptedXml(document);
            encryptedXml.DecryptDocument();

            // Finally, return the new XmlReader from the updated XmlDocument.
            // Error messages based on this XmlReader won't show line numbers,
            // but that's fine since we transformed the document anyway.
            return document.CreateNavigator()!.ReadSubtree();
        }
    }
}
