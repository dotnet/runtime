// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {

        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class CReaderTestModule : BridgeHelpers
            {
                //[Variation("Reader Property empty doc", Priority = 0)]
                public void v1()
                {
                    XDocument doc = new XDocument();
                    XmlReader r = doc.CreateReader();

                    TestLog.Compare(r.AttributeCount, 0, "Error");
                    TestLog.Compare(r.BaseURI, "", "Error");
                    TestLog.Compare(r.CanReadBinaryContent, false, "Error");
                    TestLog.Compare(r.CanReadValueChunk, false, "Error");
                    TestLog.Compare(r.CanResolveEntity, false, "Error");
                    TestLog.Compare(r.Depth, 0, "Error");
                    TestLog.Compare(r.EOF, false, "Error");
                    TestLog.Compare(r.HasAttributes, false, "Error");
                    TestLog.Compare(r.HasValue, false, "Error");
                    TestLog.Compare(r.IsDefault, false, "Error");
                    TestLog.Compare(r.IsEmptyElement, false, "Error");
                    TestLog.Compare(r.LocalName, "", "Error");
                    TestLog.Compare(r.Name, "", "Error");
                    TestLog.Compare(r.NamespaceURI, "", "Error");
                    TestLog.Compare(r.Prefix, "", "Error");
                    TestLog.Compare(r.ReadState, ReadState.Initial, "Error");
                    TestLog.Compare(r.Value, "", "Error");
                    TestLog.Compare(r.XmlLang, "", "Error");
                    TestLog.Compare(r.XmlSpace, XmlSpace.None, "Error");
                }

                //[Variation("Reader Property after Read", Priority = 0)]
                public void v2()
                {
                    XDocument doc = XDocument.Parse("<a b='c'><d>xxx<e/></d></a>");
                    XmlReader r = doc.CreateReader();
                    r.Read();

                    TestLog.Compare(r.AttributeCount, 1, "Error");
                    TestLog.Compare(r.BaseURI, "", "Error");
                    TestLog.Compare(r.CanReadBinaryContent, false, "Error");
                    TestLog.Compare(r.CanReadValueChunk, false, "Error");
                    TestLog.Compare(r.CanResolveEntity, false, "Error");
                    TestLog.Compare(r.Depth, 0, "Error");
                    TestLog.Compare(r.EOF, false, "Error");
                    TestLog.Compare(r.HasAttributes, true, "Error");
                    TestLog.Compare(r.HasValue, false, "Error");
                    TestLog.Compare(r.IsDefault, false, "Error");
                    TestLog.Compare(r.IsEmptyElement, false, "Error");
                    TestLog.Compare(r.LocalName, "a", "Error");
                    TestLog.Compare(r.Name, "a", "Error");
                    TestLog.Compare(r.NamespaceURI, "", "Error");
                    TestLog.Compare(r.Prefix, "", "Error");
                    TestLog.Compare(r.ReadState, ReadState.Interactive, "Error");
                    TestLog.Compare(r.Value, "", "Error");
                    TestLog.Compare(r.XmlLang, "", "Error");
                    TestLog.Compare(r.XmlSpace, XmlSpace.None, "Error");
                }

                //[Variation("Reader Property after EOF", Priority = 0)]
                public void v3()
                {
                    XDocument doc = XDocument.Parse("<a b='c'><d>xxx<e/></d></a>");
                    XmlReader r = doc.CreateReader();
                    while (r.Read()) ;

                    TestLog.Compare(r.AttributeCount, 0, "Error");
                    TestLog.Compare(r.BaseURI, "", "Error");
                    TestLog.Compare(r.CanReadBinaryContent, false, "Error");
                    TestLog.Compare(r.CanReadValueChunk, false, "Error");
                    TestLog.Compare(r.CanResolveEntity, false, "Error");
                    TestLog.Compare(r.Depth, 0, "Error");
                    TestLog.Compare(r.EOF, true, "Error");
                    TestLog.Compare(r.HasAttributes, false, "Error");
                    TestLog.Compare(r.HasValue, false, "Error");
                    TestLog.Compare(r.IsDefault, false, "Error");
                    TestLog.Compare(r.IsEmptyElement, false, "Error");
                    TestLog.Compare(r.LocalName, "", "Error");
                    TestLog.Compare(r.Name, "", "Error");
                    TestLog.Compare(r.NamespaceURI, "", "Error");
                    TestLog.Compare(r.Prefix, "", "Error");
                    TestLog.Compare(r.ReadState, ReadState.EndOfFile, "Error");
                    TestLog.Compare(r.Value, "", "Error");
                    TestLog.Compare(r.XmlLang, "", "Error");
                    TestLog.Compare(r.XmlSpace, XmlSpace.None, "Error");
                }

                //[Variation("Default Reader Settings", Priority = 0)]
                public void v4()
                {
                    XDocument doc = XDocument.Parse("<a/>");
                    XmlReader r = doc.CreateReader();
                    XmlReaderSettings rs = r.Settings;

                    TestLog.Compare(rs.CheckCharacters, false, "Error");
                    TestLog.Compare(rs.CloseInput, false, "Error");
                    TestLog.Compare(rs.ConformanceLevel, ConformanceLevel.Document, "Error");
                    TestLog.Compare(rs.IgnoreComments, false, "Error");
                    TestLog.Compare(rs.IgnoreProcessingInstructions, false, "Error");
                    TestLog.Compare(rs.IgnoreWhitespace, false, "Error");
                    TestLog.Compare(rs.LineNumberOffset, 0, "Error");
                    TestLog.Compare(rs.LinePositionOffset, 0, "Error");
                    TestLog.Compare(rs.NameTable, null, "Error");
                }
            }
        }
    }
}
