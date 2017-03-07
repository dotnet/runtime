// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;

namespace System.Security.Cryptography.Xml.Tests
{
    internal static class TestHelpers
    {
        /// <summary>
        /// Convert a <see cref="Stream"/> to a <see cref="string"/> using the given <see cref="Encoding"/>.
        /// </summary>
        /// <param name="stream">
        /// The <see cref="Stream"/> to read from. This cannot be null.
        /// </param>
        /// <param name="encoding">
        /// The <see cref="Encoding"/> to use. This cannot be null.
        /// </param>
        /// <returns>
        /// The stream as a string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// No argument can be null.
        /// </exception>
        public static string StreamToString(Stream stream, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (StreamReader streamReader = new StreamReader(stream, encoding))
            {
                return streamReader.ReadToEnd();
            }
        }

        /// <summary>
        /// Perform
        /// </summary>
        /// <param name="inputXml">
        /// The XML to transform. This cannot be null, empty or whitespace.
        /// </param>
        /// <param name="transform">
        /// The <see cref="Transform"/> to perform on 
        /// <paramref name="inputXml"/>. This cannot be null.
        /// </param>
        /// <param name="encoding">
        /// An optional <see cref="Encoding"/> to use when serializing or 
        /// deserializing <paramref name="inputXml"/>. This should match the 
        /// encoding specified in <paramref name="inputXml"/>. If omitted or 
        /// null, <see cref="UTF8Encoding"/> is used.
        /// </param>
        /// <param name="resolver">
        /// An optional <see cref="XmlResolver"/> to use. If omitted or null, 
        /// no resolver is used.
        /// </param>
        /// <returns>
        /// The transformed <paramref name="inputXml"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="transform"/> cannot be null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputXml"/> cannot be null, empty or whitespace.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="inputXml"/> is not valid XML.
        /// </exception>
        public static string ExecuteTransform(string inputXml, Transform transform, Encoding encoding = null, XmlResolver resolver = null)
        {
            if (string.IsNullOrWhiteSpace(inputXml))
            {
                throw new ArgumentException("Cannot be null, empty or whitespace", nameof(inputXml));
            }
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(Transform));
            }

            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = resolver;
            doc.PreserveWhitespace = true;
            doc.LoadXml(inputXml);

            Encoding actualEncoding = encoding ?? Encoding.UTF8;
            byte[] data = actualEncoding.GetBytes(inputXml);
            using (Stream stream = new MemoryStream(data))
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { ValidationType = ValidationType.None, DtdProcessing = DtdProcessing.Parse, XmlResolver = resolver }))
            {
                doc.Load(reader);
                transform.LoadInput(doc);
                return StreamToString((Stream)transform.GetOutput(), actualEncoding);
            }
        }

        /// <summary>
        /// Convert <paramref name="fileName"/> to a full URI for referencing 
        /// in an <see cref="XmlPreloadedResolver"/>.
        /// </summary>
        /// <param name="fileName">
        /// The file name. This cannot be null, empty or whitespace.
        /// </param>
        /// <returns>
        /// The created <see cref="Uri"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="fileName"/> cannot be null, empty or whitespace.
        /// </exception>
        public static Uri ToUri(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Cannot be null, empty or whitespace", nameof(fileName));
            }

            string path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            return new Uri("file://" + (path[0] == '/' ? path : '/' + path));
        }
    }
}
