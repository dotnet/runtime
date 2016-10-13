// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) 2002 Ville Palo
// (C) 2003 Martin Willemoes Hansen

// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.XPath;

using Xunit;

namespace System.Data.Tests.Xml
{
    public class XmlDataDocumentTest : DataSetAssertion, IDisposable
    {
        private static string s_EOL = "\n";
        private const string RegionXsd =
@"<?xml version=""1.0"" standalone=""yes""?>
<!-- Note that the msdata namespace is incorrect (even mofifying any URI), however Root is regarded as dataset element under MS.NET -->
<xsd:schema id=""Root"" xmlns=""""
xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""INCORRECT!!!-urn:schemas-microsoft-
com:xml-msdata"">
    <xsd:element name=""Root"" msdata:IsDataSet=""true"">
    <xsd:complexType>
        <xsd:choice maxOccurs=""unbounded"">
        <xsd:element name=""Region"">
            <xsd:complexType>
            <xsd:sequence>
                <xsd:element name=""RegionID"" type=""xsd:string""
                minOccurs=""0"" />
                <xsd:element name=""RegionDescription"" type=""xsd:string""
                minOccurs=""0"" />
            </xsd:sequence>
            </xsd:complexType>
        </xsd:element>
        </xsd:choice>
    </xsd:complexType>
    </xsd:element>
</xsd:schema>";
        private const string RegionXml =
@"<?xml version=""1.0"" standalone=""yes""?>
<Root>
 <Region>
   <RegionID>1</RegionID>
   <RegionDescription>Eastern
   </RegionDescription>
 </Region>
 <Region>
   <RegionID>2</RegionID>
   <RegionDescription>Western
   </RegionDescription>
 </Region>
 <Region>
   <RegionID>3</RegionID>
   <RegionDescription>Northern
   </RegionDescription>
 </Region>
 <Region>
   <RegionID>4</RegionID>
   <RegionDescription>Southern
   </RegionDescription>
 </Region>
 <MoreData>
   <Column1>12</Column1>
   <Column2>Hi There</Column2>
 </MoreData>
 <MoreData>
   <Column1>12</Column1>
   <Column2>Hi There</Column2>
 </MoreData>
</Root>";

        private CultureInfo _originalCulture;

        public XmlDataDocumentTest()
        {
            _originalCulture = CultureInfo.CurrentCulture; ;
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
        }

        [Fact]
        public void NewInstance()
        {
            XmlDataDocument doc = new XmlDataDocument();
            AssertDataSet("#1", doc.DataSet, "NewDataSet", 0, 0);
            Assert.False(doc.DataSet.EnforceConstraints);
            XmlElement el = doc.CreateElement("TEST");
            AssertDataSet("#2", doc.DataSet, "NewDataSet", 0, 0);
            Assert.Null(doc.GetRowFromElement(el));
            doc.AppendChild(el);
            AssertDataSet("#3", doc.DataSet, "NewDataSet", 0, 0);

            DataSet ds = new DataSet();
            doc = new XmlDataDocument(ds);
            Assert.True(doc.DataSet.EnforceConstraints);
        }

        [Fact]
        public void SimpleLoad()
        {
            string xml001 = "<root/>";
            XmlDataDocument doc = new XmlDataDocument();
            DataSet ds = new DataSet();
            ds.InferXmlSchema(new StringReader(xml001), null);
            doc.LoadXml(xml001);

            string xml002 = "<root><child/></root>";
            doc = new XmlDataDocument();
            ds = new DataSet();
            ds.InferXmlSchema(new StringReader(xml002), null);
            doc.LoadXml(xml002);

            string xml003 = "<root><col1>test</col1><col1></col1></root>";
            doc = new XmlDataDocument();
            ds = new DataSet();
            ds.InferXmlSchema(new StringReader(xml003), null);
            doc.LoadXml(xml003);

            string xml004 = "<set><tab1><col1>test</col1><col1>test2</col1></tab1><tab2><col2>test3</col2><col2>test4</col2></tab2></set>";
            doc = new XmlDataDocument();
            ds = new DataSet();
            ds.InferXmlSchema(new StringReader(xml004), null);
            doc.LoadXml(xml004);
        }

        [Fact]
        public void CloneNode()
        {
            XmlDataDocument doc = new XmlDataDocument();

            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));

            XmlDataDocument doc2 = (XmlDataDocument)doc.CloneNode(false);

            Assert.Equal(0, doc2.ChildNodes.Count);
            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?>", doc2.DataSet.GetXmlSchema().Substring(0, 39));

            doc2 = (XmlDataDocument)doc.CloneNode(true);

            Assert.Equal(2, doc2.ChildNodes.Count);
            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?>", doc2.DataSet.GetXmlSchema().Substring(0, 39));

            doc.DataSet.Tables[0].Rows[0][0] = "64";

            Assert.Equal("1", doc2.DataSet.Tables[0].Rows[0][0].ToString());
        }

        [Fact]
        public void EditingXmlTree()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));

            XmlElement Element = doc.GetElementFromRow(doc.DataSet.Tables[0].Rows[1]);
            Element.FirstChild.InnerText = "64";
            Assert.Equal("64", doc.DataSet.Tables[0].Rows[1][0]);

            DataSet Set = new DataSet();
            Set.ReadXml(new StringReader(RegionXml));
            doc = new XmlDataDocument(Set);

            Element = doc.GetElementFromRow(doc.DataSet.Tables[0].Rows[1]);
            Assert.NotNull(Element);

            try
            {
                Element.FirstChild.InnerText = "64";
                Assert.False(true);
            }
            catch (InvalidOperationException)
            {
            }

            Assert.Equal("2", doc.DataSet.Tables[0].Rows[1][0]);

            Set.EnforceConstraints = false;
            Element.FirstChild.InnerText = "64";
            Assert.Equal("64", doc.DataSet.Tables[0].Rows[1][0]);
        }

        [Fact]
        public void EditingDataSet()
        {
            string xml = "<Root><Region><RegionID>1</RegionID><RegionDescription>Eastern" + Environment.NewLine + "   </RegionDescription></Region><Region><RegionID>2</RegionID><RegionDescription>Western" + Environment.NewLine + "   </RegionDescription></Region><Region><RegionID>3</RegionID><RegionDescription>Northern" + Environment.NewLine + "   </RegionDescription></Region><Region><RegionID>4</RegionID><RegionDescription>Southern" + Environment.NewLine + "   </RegionDescription></Region><MoreData><Column1>12</Column1><Column2>Hi There</Column2></MoreData><MoreData><Column1>12</Column1><Column2>Hi There</Column2></MoreData></Root>";

            XmlReader Reader = new XmlTextReader(new StringReader(RegionXml));
            XmlDataDocument Doc = new XmlDataDocument();
            Doc.DataSet.ReadXml(Reader);
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            Doc.DataSet.WriteXml(xw);
            string s = sw.ToString();
            Assert.Equal(xml, s);
            Assert.Equal(xml, Doc.InnerXml);
            Assert.Equal("EndOfFile", Reader.ReadState.ToString());

            DataSet Set = Doc.DataSet;
            Assert.Equal("2", Set.Tables[0].Rows[1][0]);
            Set.Tables[0].Rows[1][0] = "64";
            Assert.Equal("64", Doc.FirstChild.FirstChild.NextSibling.FirstChild.InnerText);
        }

        [Fact]
        public void CreateElement1()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));

            XmlElement Element = doc.CreateElement("prefix", "localname", "namespaceURI");
            Assert.Equal("prefix", Element.Prefix);
            Assert.Equal("localname", Element.LocalName);
            Assert.Equal("namespaceURI", Element.NamespaceURI);
            doc.ImportNode(Element, false);

            TextWriter text = new StringWriter();
            doc.Save(text);

            string substring = string.Empty;
            string TextString = text.ToString();

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("<Root>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("    <RegionID>1</RegionID>") != -1);

            for (int i = 0; i < 26; i++)
            {
                substring = TextString.Substring(0, TextString.IndexOf("\n"));
                TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            }

            substring = TextString.Substring(0, TextString.Length);
            Assert.True(substring.IndexOf("</Root>") != -1);
        }

        [Fact]
        public void CreateElement2()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));

            XmlElement Element = doc.CreateElement("ElementName");
            Assert.Equal(string.Empty, Element.Prefix);
            Assert.Equal("ElementName", Element.LocalName);
            Assert.Equal(string.Empty, Element.NamespaceURI);

            Element = doc.CreateElement("prefix:ElementName");
            Assert.Equal("prefix", Element.Prefix);
            Assert.Equal("ElementName", Element.LocalName);
            Assert.Equal(string.Empty, Element.NamespaceURI);
        }

        [Fact]
        public void CreateElement3()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));

            XmlElement Element = doc.CreateElement("ElementName", "namespace");
            Assert.Equal(string.Empty, Element.Prefix);
            Assert.Equal("ElementName", Element.LocalName);
            Assert.Equal("namespace", Element.NamespaceURI);

            Element = doc.CreateElement("prefix:ElementName", "namespace");
            Assert.Equal("prefix", Element.Prefix);
            Assert.Equal("ElementName", Element.LocalName);
            Assert.Equal("namespace", Element.NamespaceURI);
        }

        [Fact]
        public void Navigator()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));

            XPathNavigator Nav = doc.CreateNavigator();

            Nav.MoveToRoot();
            Nav.MoveToFirstChild();

            Assert.Equal("Root", Nav.Name.ToString());
            Assert.Equal(string.Empty, Nav.NamespaceURI.ToString());
            Assert.Equal("False", Nav.IsEmptyElement.ToString());
            Assert.Equal("Element", Nav.NodeType.ToString());
            Assert.Equal(string.Empty, Nav.Prefix);

            Nav.MoveToFirstChild();
            Nav.MoveToNext();
            Assert.Equal("Region", Nav.Name.ToString());

            Assert.Equal("2Western", Nav.Value.Substring(0, Nav.Value.IndexOfAny(new[] { '\r', '\n' })));
            Nav.MoveToFirstChild();
            Assert.Equal("2", Nav.Value);
            Nav.MoveToRoot();
            Assert.Equal("Root", Nav.NodeType.ToString());
        }

        // Test constructor
        [Fact]
        public void Test1()
        {
            //Create an XmlDataDocument.
            XmlDataDocument doc = new XmlDataDocument();

            //Load the schema file.
            doc.DataSet.ReadXmlSchema(new StringReader(DataProvider.store));
            //Load the XML data.
            doc.Load(new StringReader(
@"<!--sample XML fragment-->
<bookstore>
  <book genre='novel' ISBN='10-861003-324'>
    <title>The Handmaid's Tale</title>
    <price>19.95</price>
  </book>
  <book genre='novel' ISBN='1-861001-57-5'>
    <title>Pride And Prejudice</title>
    <price>24.95</price>
  </book>
</bookstore>"));

            //Update the price on the first book using the DataSet methods.
            DataTable books = doc.DataSet.Tables["book"];
            books.Rows[0]["price"] = "12.95";

            //string outstring = "";
            TextWriter text = new StringWriter();
            text.NewLine = "\n";
            doc.Save(text);

            //str.Read (bytes, 0, (int)str.Length);
            //String OutString = new String (bytes);

            string TextString = text.ToString();
            string substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("<?xml version=\"1.0\" encoding=\"utf-16\"?>") == 0);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("<!--sample XML fragment-->") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("<bookstore>") != -1);
            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <book genre=\"novel\" ISBN=\"10-861003-324\">") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <title>The Handmaid's Tale</title>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("    <price>12.95</price>", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </book>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <book genre=\"novel\" ISBN=\"1-861001-57-5\">") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <title>Pride And Prejudice</title>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <price>24.95</price>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </book>") != -1);

            substring = TextString;
            Assert.True(substring.IndexOf("</bookstore>") != -1);
        }

        // Test public fields
        [Fact]
        public void Test2()
        {
            DataSet RegionDS = new DataSet();
            DataRow RegionRow;
            RegionDS.ReadXmlSchema(new StringReader(RegionXsd));
            Assert.Equal(1, RegionDS.Tables.Count);
            XmlDataDocument DataDoc = new XmlDataDocument(RegionDS);
            DataDoc.Load(new StringReader(RegionXml));

            RegionRow = RegionDS.Tables[0].Rows[0];

            RegionDS.AcceptChanges();
            RegionRow["RegionDescription"] = "Reeeeeaalllly Far East!";
            RegionDS.AcceptChanges();

            TextWriter text = new StringWriter();
            text.NewLine = "\n";
            DataDoc.Save(text);
            string TextString = text.ToString();
            string substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);

            //Assert.Equal ("<?xml version=\"1.0\" encoding=\"utf-16\" standalone=\"yes\"?>", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("<Root>", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>1</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("    <RegionDescription>Reeeeeaalllly Far East!</RegionDescription>", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>2</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionDescription>Western") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>3</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionDescription>Northern") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>4</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionDescription>Southern") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <MoreData>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column1>12</Column1>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column2>Hi There</Column2>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </MoreData>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <MoreData>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column1>12</Column1>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column2>Hi There</Column2>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </MoreData>") != -1);
        }

        [Fact]
        public void Test3()
        {
            XmlDataDocument DataDoc = new XmlDataDocument();
            DataSet dataset = DataDoc.DataSet;
            dataset.ReadXmlSchema(new StringReader(RegionXsd));
            DataDoc.Load(new StringReader(RegionXml));

            DataDoc.GetElementsByTagName("Region")[0].RemoveAll();

            TextWriter text = new StringWriter();
            text.NewLine = "\n";
            dataset.WriteXml(text);
            //DataDoc.Save (text);
            string TextString = text.ToString();
            string substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);

            Assert.True(substring.IndexOf("<Root>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("  <Region />", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("  <Region>", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("    <RegionID>2</RegionID>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            // Regardless of NewLine value, original xml contains CR
            // (but in the context of XML spec, it should be normalized)
            Assert.Equal("    <RegionDescription>Western", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("  </Region>", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>3</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            // Regardless of NewLine value, original xml contains CR
            // (but in the context of XML spec, it should be normalized)
            Assert.Equal("    <RegionDescription>Northern", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>4</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionDescription>Southern") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.Length);
            Assert.True(substring.IndexOf("</Root>") != -1);
        }

        [Fact]
        public void Test4()
        {
            DataSet RegionDS = new DataSet();

            RegionDS.ReadXmlSchema(new StringReader(RegionXsd));
            XmlDataDocument DataDoc = new XmlDataDocument(RegionDS);
            DataDoc.Load(new StringReader(RegionXml));
            Assert.True(RegionDS.EnforceConstraints);
            DataTable table = DataDoc.DataSet.Tables["Region"];
            DataRow newRow = table.NewRow();
            newRow[0] = "new row";
            newRow[1] = "new description";

            table.Rows.Add(newRow);

            TextWriter text = new StringWriter();
            text.NewLine = "\n";
            DataDoc.Save(text);
            string TextString = text.ToString();
            string substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("<Root>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>1</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            // Regardless of NewLine value, original xml contains CR
            // (but in the context of XML spec, it should be normalized)
            Assert.Equal("    <RegionDescription>Eastern", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.Equal("   </RegionDescription>", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>2</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionDescription>Western") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>3</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionDescription>Northern") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>4</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            // Regardless of NewLine value, original xml contains CR
            // (but in the context of XML spec, it should be normalized)
            Assert.Equal("    <RegionDescription>Southern", substring);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("   </RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <MoreData>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column1>12</Column1>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column2>Hi There</Column2>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </MoreData>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <MoreData>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column1>12</Column1>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <Column2>Hi There</Column2>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </MoreData>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionID>new row</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("    <RegionDescription>new description</RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf(s_EOL));
            TextString = TextString.Substring(TextString.IndexOf(s_EOL) + s_EOL.Length);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.Length);
            Assert.True(substring.IndexOf("</Root>") != -1);
        }

        [Fact]
        public void Test5()
        {
            DataSet RegionDS = new DataSet();

            RegionDS.ReadXmlSchema(new StringReader(RegionXsd));
            XmlDataDocument DataDoc = new XmlDataDocument(RegionDS);
            DataDoc.Load(new StringReader(RegionXml));
            try
            {
                DataDoc.DocumentElement.AppendChild(DataDoc.DocumentElement.FirstChild);
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
                Assert.Equal(typeof(InvalidOperationException), e.GetType());
                Assert.Equal("Please set DataSet.EnforceConstraints == false before trying to edit XmlDataDocument using XML operations.", e.Message);
                DataDoc.DataSet.EnforceConstraints = false;
            }
            XmlElement newNode = DataDoc.CreateElement("Region");
            XmlElement newChildNode = DataDoc.CreateElement("RegionID");
            newChildNode.InnerText = "64";
            XmlElement newChildNode2 = DataDoc.CreateElement("RegionDescription");
            newChildNode2.InnerText = "test node";
            newNode.AppendChild(newChildNode);
            newNode.AppendChild(newChildNode2);
            DataDoc.DocumentElement.AppendChild(newNode);
            TextWriter text = new StringWriter();

            //DataDoc.Save (text);
            DataDoc.DataSet.WriteXml(text);
            string TextString = text.ToString();
            string substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);

            for (int i = 0; i < 21; i++)
            {
                substring = TextString.Substring(0, TextString.IndexOf("\n"));
                TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            }
            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("    <RegionID>64</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("    <RegionDescription>test node</RegionDescription>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.Length);
            Assert.True(substring.IndexOf("</Root>") != -1);
        }

        [Fact]
        public void Test6()
        {
            DataSet RegionDS = new DataSet();

            RegionDS.ReadXmlSchema(new StringReader(RegionXsd));
            XmlDataDocument DataDoc = new XmlDataDocument(RegionDS);
            DataDoc.Load(new StringReader(RegionXml));
            DataDoc.DataSet.EnforceConstraints = false;

            XmlElement newNode = DataDoc.CreateElement("Region");
            XmlElement newChildNode = DataDoc.CreateElement("RegionID");

            newChildNode.InnerText = "64";
            XmlElement newChildNode2 = null;
            try
            {
                newChildNode2 = DataDoc.CreateElement("something else");
                Assert.False(true);
            }
            catch (XmlException)
            {
            }
            newChildNode2 = DataDoc.CreateElement("something_else");

            newChildNode2.InnerText = "test node";

            newNode.AppendChild(newChildNode);
            newNode.AppendChild(newChildNode2);
            DataDoc.DocumentElement.AppendChild(newNode);

            TextWriter text = new StringWriter();

            //DataDoc.Save (text);
            DataDoc.DataSet.WriteXml(text);
            string TextString = text.ToString();
            string substring = TextString.Substring(0, TextString.IndexOf("\n") - 1);
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);

            for (int i = 0; i < 21; i++)
            {
                substring = TextString.Substring(0, TextString.IndexOf("\n"));
                TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            }

            Assert.True(substring.IndexOf("  <Region>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("    <RegionID>64</RegionID>") != -1);

            substring = TextString.Substring(0, TextString.IndexOf("\n"));
            TextString = TextString.Substring(TextString.IndexOf("\n") + 1);
            Assert.True(substring.IndexOf("  </Region>") != -1);

            substring = TextString.Substring(0, TextString.Length);
            Assert.True(substring.IndexOf("</Root>") != -1);
        }

        [Fact]
        public void GetElementFromRow()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));
            DataTable table = doc.DataSet.Tables["Region"];

            XmlElement element = doc.GetElementFromRow(table.Rows[2]);
            Assert.Equal("Region", element.Name);
            Assert.Equal("3", element["RegionID"].InnerText);

            try
            {
                element = doc.GetElementFromRow(table.Rows[4]);
                Assert.False(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
                Assert.Equal("There is no row at position 4.", e.Message);
            }
        }

        [Fact]
        public void GetRowFromElement()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(RegionXsd));
            doc.Load(new StringReader(RegionXml));
            XmlElement root = doc.DocumentElement;

            DataRow row = doc.GetRowFromElement((XmlElement)root.FirstChild);

            Assert.Equal("1", row[0]);

            row = doc.GetRowFromElement((XmlElement)root.ChildNodes[2]);
            Assert.Equal("3", row[0]);
        }
    }
}
