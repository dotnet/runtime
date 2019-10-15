// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Xml.Tests
{
    public static class Utils
    {
        public static XmlReader CreateFragmentReader(string fragment)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                CheckCharacters = false,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            var stream = new StringReader(fragment);

            return XmlReader.Create(stream, settings);
        }

        public static void PositionOnElement(this XmlReader reader, string elementName)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName)
                return;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName)
                    break;
            }

            if (reader.EOF)
                throw new InvalidOperationException("Couldn't find element '" + elementName + "'");
        }

        public static void PositionOnElementNonEmptyNoDoctype(this XmlReader reader, string elementName)
        {
            while (reader.Read())
            {
                if (reader.Name == elementName && !reader.IsEmptyElement && reader.NodeType != XmlNodeType.DocumentType)
                    break;
            }
        }

        public static void PositionOnElementNoDoctype(this XmlReader reader, string elementName)
        {
            while (reader.Read())
            {
                if (reader.Name == elementName && reader.NodeType != XmlNodeType.DocumentType)
                    break;
            }
        }
    }
}
