// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Xml.XmlReaderTests
{
    public static class AttributeBufferInvalidationTests
    {
        // The reader keeps attribute values as lazy references into its internal character buffer and only
        // materializes ("invalidates") them into strings when that buffer has to be grown or shifted while a
        // start tag is still being parsed. Parsing an element whose start tag is much larger than the internal
        // buffer (4 KB for the synchronous reader, 32 KB for the asynchronous one) forces ReadData/ReadDataAsync
        // to reload the buffer many times, so the invalidation loop runs multiple passes over the attribute nodes.
        //
        // These tests exercise that path and verify that every attribute value survives the reloads intact.
        // Multiple elements ensure the per-element invalidation bookkeeping is reset between elements, and one
        // attribute per element is deliberately written with a huge run of whitespace between its name and the
        // '=' sign: because that run is larger than the internal buffer, a buffer reload is guaranteed to happen
        // after the attribute has been counted but before its buffer-backed value has been assigned. That is
        // exactly the window in which the bookkeeping must not mark the still-pending attribute as already
        // materialized; a regression there leaves a dangling reference into the buffer and corrupts the value.
        private const int ElementCount = 3;
        private const int AttributesPerElement = 3000;
        private const int ValuePadding = 40;

        // Larger than the asynchronous reader's 32 KB internal buffer so a reload is guaranteed while the
        // attribute carrying it is counted but not yet valued.
        private const int PendingAttributeWhitespace = 40 * 1024;

        // The attribute (per element) that is preceded by the huge whitespace run.
        private const int PendingAttributeIndex = 1;

        private static string MakeAttributeValue(int element, int attribute) =>
            $"E{element}_A{attribute}_" + new string((char)('a' + (attribute % 26)), ValuePadding);

        private static string BuildXml()
        {
            var sb = new StringBuilder(ElementCount * (PendingAttributeWhitespace + AttributesPerElement * (ValuePadding + 24)));
            sb.Append("<root>");
            for (int e = 0; e < ElementCount; e++)
            {
                sb.Append("<e").Append(e);
                for (int a = 0; a < AttributesPerElement; a++)
                {
                    sb.Append(" a").Append(a);
                    if (a == PendingAttributeIndex)
                    {
                        sb.Append(' ', PendingAttributeWhitespace);
                    }
                    sb.Append("=\"").Append(MakeAttributeValue(e, a)).Append('"');
                }
                sb.Append("/>");
            }
            sb.Append("</root>");
            return sb.ToString();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task AttributeValuesRemainValidAcrossBufferReloads(bool async)
        {
            string xml = BuildXml();
            var settings = new XmlReaderSettings { Async = async };

            using XmlReader reader = XmlReader.Create(new StringReader(xml), settings);

            int elementsChecked = 0;
            while (async ? await reader.ReadAsync() : reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element || reader.Depth == 0)
                {
                    continue;
                }

                Assert.Equal($"e{elementsChecked}", reader.Name);
                Assert.Equal(AttributesPerElement, reader.AttributeCount);

                for (int a = 0; a < AttributesPerElement; a++)
                {
                    Assert.Equal(MakeAttributeValue(elementsChecked, a), reader.GetAttribute(a));
                }

                elementsChecked++;
            }

            Assert.Equal(ElementCount, elementsChecked);
        }
    }
}
