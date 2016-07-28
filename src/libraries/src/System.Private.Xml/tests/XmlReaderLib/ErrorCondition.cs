// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [InheritRequired()]
    public abstract partial class TCErrorCondition : TCXMLReaderBaseGeneral
    {
        public static string xmlStr = "<a></a>";

        //[Variation("XmlReader.Create((string)null)",  Param = 1)]
        //[Variation("XmlReader.Create((TextReader)null)",  Param = 2)]
        //[Variation("XmlReader.Create((Stream)null)",  Param = 3)]
        //[Variation("XmlReader.Create((string)null, null)",  Param = 4)]
        //[Variation("XmlReader.Create((TextReader)null, null)",  Param = 5)]
        //[Variation("XmlReader.Create((Stream)null, null)",  Param = 6)]
        //[Variation("XmlReader.Create((XmlReader)null, null)",  Param = 7)]
        //[Variation("XmlReader.Create((Stream)null, null, (string) null)",  Param = 8)]
        //[Variation("XmlReader.Create((Stream)null, null, (XmlParserContext)null)",  Param = 9)]
        //[Variation("XmlReader.Create((string)null, null, null)",  Param = 10)]
        //[Variation("XmlReader.Create((TextReader)null, null, (string)null)",  Param = 11)]
        //[Variation("XmlReader.Create((TextReader)null, null, (XmlParserContext)null)",  Param = 12)]
        //[Variation("new XmlNamespaceManager(null)",  Param = 13)]
        //[Variation("XmlReader.IsName(null)",  Param = 14)]
        //[Variation("XmlReader.IsNameToken(null)",  Param = 15)]
        //[Variation("new XmlValidatingReader(null)", Param = 16)]
        //[Variation("new XmlTextReader(null)", Param = 17)]
        //[Variation("new XmlNodeReader(null)", Param = 18)]
        public int v1()
        {
            int param = (int)CurVariation.Param;
            try
            {
                switch (param)
                {
                    case 1: ReaderHelper.Create((string)null); break;
                    case 2: ReaderHelper.Create((TextReader)null); break;
                    case 3: ReaderHelper.Create((Stream)null); break;
                    case 4: ReaderHelper.Create((string)null, null); break;
                    case 5: ReaderHelper.Create((TextReader)null, null); break;
                    case 6: ReaderHelper.Create((Stream)null, null); break;
                    case 7: ReaderHelper.Create((XmlReader)null, null); break;
                    case 8: ReaderHelper.Create((Stream)null, null, (string)null); break;
                    case 9: ReaderHelper.Create((Stream)null, null, (XmlParserContext)null); break;
                    case 10: ReaderHelper.Create((string)null, null, null); break;
                    case 11: ReaderHelper.Create((TextReader)null, null, (string)null); break;
                    case 12: ReaderHelper.Create((TextReader)null, null, (XmlParserContext)null); break;
                    case 13: new XmlNamespaceManager(null); break;
                    case 14: XmlReader.IsName(null); break;
                    case 15: XmlReader.IsNameToken(null); break;
                    default:
                        return TEST_PASS;
                }
            }
            catch (ArgumentNullException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: ReaderHelper.Create((string)null); break;
                        case 2: ReaderHelper.Create((TextReader)null); break;
                        case 3: ReaderHelper.Create((Stream)null); break;
                        case 4: ReaderHelper.Create((string)null, null); break;
                        case 5: ReaderHelper.Create((TextReader)null, null); break;
                        case 6: ReaderHelper.Create((Stream)null, null); break;
                        case 7: ReaderHelper.Create((XmlReader)null, null); break;
                        case 8: ReaderHelper.Create((Stream)null, null, (string)null); break;
                        case 9: ReaderHelper.Create((Stream)null, null, (XmlParserContext)null); break;
                        case 10: ReaderHelper.Create((string)null, null, null); break;
                        case 11: ReaderHelper.Create((TextReader)null, null, (string)null); break;
                        case 12: ReaderHelper.Create((TextReader)null, null, (XmlParserContext)null); break;
                        default:
                            return TEST_PASS;
                    }
                }
                catch (ArgumentNullException) { return TEST_PASS; }
            }
            catch (NullReferenceException)
            {
                try
                {
                    switch (param)
                    {
                        case 13: new XmlNamespaceManager(null); break;
                        case 14: XmlReader.IsName(null); break;
                        case 15: XmlReader.IsNameToken(null); break;
                        default:
                            return TEST_PASS;
                    }
                }
                catch (NullReferenceException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation("XmlReader.Create(String.Empty)",  Param = 1)]
        //[Variation("XmlReader.Create(String.Empty, null)",  Param = 2)]      
        //[Variation("XmlReader.Create(String.Empty, null, (XmlParserContext)null)",  Param = 3)]       
        //[Variation("XmlReader.Create(new Stream(), null, (string)null)",  Param = 4)]
        //[Variation("XmlReader.Create(new Stream(), null, (XmlParserContext)null)",  Param = 5)]
        public int v1a()
        {
            int param = (int)CurVariation.Param;
            try
            {
                switch (param)
                {
                    case 1: ReaderHelper.Create(String.Empty); break;
                    case 2: ReaderHelper.Create(String.Empty, null); break;
                    case 3: ReaderHelper.Create(String.Empty, null, (XmlParserContext)null); break;
                }
            }
            catch (ArgumentException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: ReaderHelper.Create(String.Empty); break;
                        case 2: ReaderHelper.Create(String.Empty, null); break;
                        case 3: ReaderHelper.Create(String.Empty, null, (XmlParserContext)null); break;
                    }
                }
                catch (ArgumentException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation("XmlReader.Create(Stream)",  Param = 1)]
        //[Variation("XmlReader.Create(String)",  Param = 2)]      
        //[Variation("XmlReader.Create(TextReader)",  Param = 3)]
        //[Variation("XmlReader.Create(Stream, XmlReaderSettings)",  Param = 4)]
        //[Variation("XmlReader.Create(String, XmlReaderSettings)",  Param = 5)]
        //[Variation("XmlReader.Create(TextReader, XmlReaderSettings)",  Param = 6)]
        //[Variation("XmlReader.Create(XmlReader, XmlReaderSettings)",  Param = 7)]
        //[Variation("XmlReader.Create(Stream, XmlReaderSettings, string)",  Param = 8)]
        //[Variation("XmlReader.Create(Stream, XmlReaderSettings, XmlParserContext)",  Param = 9)]
        //[Variation("XmlReader.Create(String, XmlReaderSettings, XmlParserContext)",  Param = 10)]
        //[Variation("XmlReader.Create(TextReader, XmlReaderSettings, string)",  Param = 11)]
        //[Variation("XmlReader.Create(TextReader, XmlReaderSettings, XmlParserContext)",  Param = 12)]
        public int v1b()
        {
            int param = (int)CurVariation.Param;
            XmlReaderSettings rs = new XmlReaderSettings();
            XmlReader r = ReaderHelper.Create(new StringReader("<a/>"));
            string uri = Path.Combine(TestData, "Common", "file_23.xml");
            try
            {
                switch (param)
                {
                    case 2: ReaderHelper.Create(uri); break;
                    case 3: ReaderHelper.Create(new StringReader("<a/>")); break;
                    case 5: ReaderHelper.Create(uri, rs); break;
                    case 6: ReaderHelper.Create(new StringReader("<a/>"), rs); break;
                    case 7: ReaderHelper.Create(r, rs); break;
                    case 10: ReaderHelper.Create(uri, rs, (XmlParserContext)null); break;
                    case 11: ReaderHelper.Create(new StringReader("<a/>"), rs, uri); break;
                    case 12: ReaderHelper.Create(new StringReader("<a/>"), rs, (XmlParserContext)null); break;
                }
            }
            catch (FileNotFoundException) { return (IsXsltReader()) ? TEST_PASS : TEST_FAIL; }
            return TEST_PASS;
        }

        //[Variation("XmlReader[null])",  Param = 1)]
        //[Variation("XmlReader[null, null]",  Param = 2)]
        //[Variation("XmlReader.GetAttribute(null)",  Param = 3)]
        //[Variation("XmlReader.GetAttribute(null, null)",  Param = 4)]
        //[Variation("XmlReader.MoveToAttribute(null)",  Param = 5)]
        //[Variation("XmlReader.MoveToAttribute(null, null)",  Param = 6)]
        //[Variation("XmlReader.ReadContentAsBase64(null, 0, 0)",  Param = 7)]
        //[Variation("XmlReader.ReadContentAsBinHex(null, 0, 0)",  Param = 8)]
        //[Variation("XmlReader.ReadElementContentAs(null, null, null, null)",  Param = 9)]
        //[Variation("XmlReader.ReadElementContentAs(null, null, 'a', null)",  Param = 10)]
        //[Variation("XmlReader.ReadElementContentAsBase64(null, 0, 0)",  Param = 11)]
        //[Variation("XmlReader.ReadElementContentAsBinHex(null, 0, 0)",  Param = 12)]
        //[Variation("XmlReader.ReadElementContentAsBoolean(null, null)",  Param = 13)]
        //[Variation("XmlReader.ReadElementContentAsBoolean('a', null)",  Param = 14)]
        //[Variation("XmlReader.ReadElementContentAsDateTime(null, null)",  Param = 15)]
        //[Variation("XmlReader.ReadElementContentAsDateTime('a', null)",  Param = 16)]
        //[Variation("XmlReader.ReadElementContentAsDecimal(null, null)",  Param = 17)]
        //[Variation("XmlReader.ReadElementContentAsDecimal('a', null)",  Param = 18)]
        //[Variation("XmlReader.ReadElementContentAsDouble(null, null)",  Param = 19)]
        //[Variation("XmlReader.ReadElementContentAsDouble('a', null)",  Param = 20)]
        //[Variation("XmlReader.ReadElementContentAsFloat(null, null)",  Param = 21)]
        //[Variation("XmlReader.ReadElementContentAsFloat('a', null)",  Param = 22)]
        //[Variation("XmlReader.ReadElementContentAsInt(null, null)",  Param = 23)]
        //[Variation("XmlReader.ReadElementContentAsInt('a', null)",  Param = 24)]
        //[Variation("XmlReader.ReadElementContentAsLong(null, null)",  Param = 25)]
        //[Variation("XmlReader.ReadElementContentAsLong('a', null)",  Param = 26)]
        //[Variation("XmlReader.ReadElementContentAsObject(null, null)",  Param = 27)]
        //[Variation("XmlReader.ReadElementContentAsObject('a', null)",  Param = 28)]
        //[Variation("XmlReader.ReadElementContentAsString(null, null)",  Param = 29)]
        //[Variation("XmlReader.ReadElementContentAsString('a', null)",  Param = 30)]
        //[Variation("XmlReader.ReadToDescendant(null)",  Param = 31)]
        //[Variation("XmlReader.ReadToDescendant(null, null)",  Param = 32)]
        //[Variation("XmlReader.ReadToDescendant('a', null)",  Param = 33)]
        //[Variation("XmlReader.ReadToFollowing(null)",  Param = 34)]
        //[Variation("XmlReader.ReadToFollowing(null, null)",  Param = 35)]
        //[Variation("XmlReader.ReadToFollowing('a', null)",  Param = 36)]
        //[Variation("XmlReader.ReadToNextSibling(null)",  Param = 37)]
        //[Variation("XmlReader.ReadToNextSibling(null, null)",  Param = 38)]
        //[Variation("XmlReader.ReadToNextSibling('a', null)",  Param = 39)]
        //[Variation("XmlReader.ReadValueChunk(null, 0, 0)",  Param = 40)]
        //[Variation("XmlReader.ReadContentAs(null, null)",  Param = 41)]
        public int v2()
        {
            int param = (int)CurVariation.Param;
            ReloadSource(new StringReader(@"<a xmlns:f='urn:foobar' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
                       "<b><c xsi:type='f:mytype'>some content</c></b></a>"));

            string s = "";
            try
            {
                switch (param)
                {
                    case 1: s = DataReader[null]; break;
                    case 2: s = DataReader[null, null]; break;
                    case 3: s = DataReader.GetAttribute(null); break;
                    case 4: s = DataReader.GetAttribute(null, null); break;
                    case 5: DataReader.MoveToAttribute(null); break;
                    case 6: DataReader.MoveToAttribute(null, null); break;
                    case 7: DataReader.ReadContentAsBase64(null, 0, 0); break;
                    case 8: DataReader.ReadContentAsBinHex(null, 0, 0); break;
                    case 9: DataReader.ReadElementContentAs(null, null, null, null); break;
                    case 10: DataReader.ReadElementContentAs(null, null, "a", null); break;
                    case 11: DataReader.ReadElementContentAsBase64(null, 0, 0); break;
                    case 12: DataReader.ReadElementContentAsBinHex(null, 0, 0); break;
                    case 13: DataReader.ReadElementContentAsBoolean(null, null); break;
                    case 14: DataReader.ReadElementContentAsBoolean("a", null); break;
                    case 17: DataReader.ReadElementContentAsDecimal(null, null); break;
                    case 18: DataReader.ReadElementContentAsDecimal("a", null); break;
                    case 19: DataReader.ReadElementContentAsDouble(null, null); break;
                    case 20: DataReader.ReadElementContentAsDouble("a", null); break;
                    case 21: DataReader.ReadElementContentAsFloat(null, null); break;
                    case 22: DataReader.ReadElementContentAsFloat("a", null); break;
                    case 23: DataReader.ReadElementContentAsInt(null, null); break;
                    case 24: DataReader.ReadElementContentAsInt("a", null); break;
                    case 25: DataReader.ReadElementContentAsLong(null, null); break;
                    case 26: DataReader.ReadElementContentAsLong("a", null); break;
                    case 27: DataReader.ReadElementContentAsObject(null, null); break;
                    case 28: DataReader.ReadElementContentAsObject("a", null); break;
                    case 29: DataReader.Read(); DataReader.ReadElementContentAsString(null, null); break;
                    case 30: DataReader.Read(); DataReader.ReadElementContentAsString("a", null); break;
                    case 31: DataReader.ReadToDescendant(null); break;
                    case 32: DataReader.ReadToDescendant(null, null); break;
                    case 33: DataReader.ReadToDescendant("a", null); break;
                    case 34: DataReader.ReadToFollowing(null); break;
                    case 35: DataReader.ReadToFollowing(null, null); break;
                    case 36: DataReader.ReadToFollowing("a", null); break;
                    case 37: DataReader.ReadToNextSibling(null); break;
                    case 38: DataReader.ReadToNextSibling(null, null); break;
                    case 39: DataReader.ReadToNextSibling("a", null); break;
                    case 40: DataReader.Read(); DataReader.MoveToFirstAttribute(); DataReader.ReadValueChunk(null, 0, 0); break;
                    case 41: DataReader.Read(); DataReader.MoveToFirstAttribute(); DataReader.ReadContentAs(null, null); break;
                }
            }
            catch (ArgumentNullException)
            {
                try
                {
                    switch (param)
                    {
                        case 2: s = DataReader[null, null]; break;
                        case 4: s = DataReader.GetAttribute(null, null); break;
                        case 6: DataReader.MoveToAttribute(null, null); break;
                        case 7: DataReader.ReadContentAsBase64(null, 0, 0); break;
                        case 8: DataReader.ReadContentAsBinHex(null, 0, 0); break;
                        case 9: DataReader.ReadElementContentAs(null, null, null, null); break;
                        case 10: DataReader.ReadElementContentAs(null, null, "a", null); break;
                        case 11: DataReader.ReadElementContentAsBase64(null, 0, 0); break;
                        case 12: DataReader.ReadElementContentAsBinHex(null, 0, 0); break;
                        case 13: DataReader.ReadElementContentAsBoolean(null, null); break;
                        case 14: DataReader.ReadElementContentAsBoolean("a", null); break;
                        case 17: DataReader.ReadElementContentAsDecimal(null, null); break;
                        case 18: DataReader.ReadElementContentAsDecimal("a", null); break;
                        case 19: DataReader.ReadElementContentAsDouble(null, null); break;
                        case 20: DataReader.ReadElementContentAsDouble("a", null); break;
                        case 21: DataReader.ReadElementContentAsFloat(null, null); break;
                        case 22: DataReader.ReadElementContentAsFloat("a", null); break;
                        case 23: DataReader.ReadElementContentAsInt(null, null); break;
                        case 24: DataReader.ReadElementContentAsInt("a", null); break;
                        case 25: DataReader.ReadElementContentAsLong(null, null); break;
                        case 26: DataReader.ReadElementContentAsLong("a", null); break;
                        case 27: DataReader.ReadElementContentAsObject(null, null); break;
                        case 28: DataReader.ReadElementContentAsObject("a", null); break;
                        case 29: DataReader.ReadElementContentAsString(null, null); break;
                        case 30: DataReader.ReadElementContentAsString("a", null); break;
                        case 31: DataReader.ReadToDescendant(null); break;
                        case 32: DataReader.ReadToDescendant(null, null); break;
                        case 33: DataReader.ReadToDescendant("a", null); break;
                        case 34: DataReader.ReadToFollowing(null); break;
                        case 35: DataReader.ReadToFollowing(null, null); break;
                        case 36: DataReader.ReadToFollowing("a", null); break;
                        case 37: DataReader.ReadToNextSibling(null); break;
                        case 38: DataReader.ReadToNextSibling(null, null); break;
                        case 39: DataReader.ReadToNextSibling("a", null); break;
                        case 40: DataReader.ReadValueChunk(null, 0, 0); break;
                        case 41: DataReader.ReadContentAs(null, null); break;
                    }
                }
                catch (ArgumentNullException) { return TEST_PASS; }
                catch (InvalidOperationException)
                {
                    if (IsSubtreeReader())
                        return TEST_PASS;
                }
            }
            catch (NotSupportedException)
            {
                if (IsCustomReader() && (param == 7 || param == 8 || param == 11 || param == 12 || param == 40))
                    return TEST_PASS;
                if ((IsCharCheckingReader() || IsXmlTextReader()) && param == 40)
                    return TEST_PASS;
                return TEST_FAIL;
            }
            catch (NullReferenceException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: s = DataReader[null]; break;
                        case 3: s = DataReader.GetAttribute(null); break;
                        case 5: DataReader.MoveToAttribute(null); break;
                    }
                }
                catch (NullReferenceException) { return TEST_PASS; }
            }
            finally
            {
                DataReader.Close();
            }
            return (IsCharCheckingReader() && (param == 7 || param == 8) || IsSubtreeReader()) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation("XmlReader[-1]",  Param = 1)]
        //[Variation("XmlReader[0]",  Param = 2)]
        //[Variation("XmlReader.GetAttribute(-1)",  Param = 3)]
        //[Variation("XmlReader.GetAttribute(0)",  Param = 4)]
        //[Variation("XmlReader.MoveToAttribute(-1)",  Param = 5)]
        //[Variation("XmlReader.MoveToAttribute(0)",  Param = 6)]
        public int v3()
        {
            int param = (int)CurVariation.Param;
            ReloadSource(new StringReader(xmlStr));
            string s = "";
            try
            {
                switch (param)
                {
                    case 1: s = DataReader[-1]; break;
                    case 2: s = DataReader[0]; break;
                    case 3: s = DataReader.GetAttribute(-1); break;
                    case 4: s = DataReader.GetAttribute(0); break;
                    case 5: DataReader.MoveToAttribute(-1); break;
                    case 6: DataReader.MoveToAttribute(0); break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: s = DataReader[-1]; break;
                        case 2: s = DataReader[0]; break;
                        case 3: s = DataReader.GetAttribute(-1); break;
                        case 4: s = DataReader.GetAttribute(0); break;
                        case 5: DataReader.MoveToAttribute(-1); break;
                        case 6: DataReader.MoveToAttribute(0); break;
                    }
                }
                catch (ArgumentOutOfRangeException) { return TEST_PASS; }
            }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        //[Variation("nsm.AddNamespace('p', null)", Param = 1)]
        //[Variation("nsm.RemoveNamespace('p', null)", Param = 2)]
        public int v4()
        {
            int param = (int)CurVariation.Param;
            string xml = @"<a>p:foo</a>";
            ReloadSource(new StringReader(xml));
            DataReader.Read(); DataReader.Read();
            XmlNamespaceManager nsm = new XmlNamespaceManager(DataReader.NameTable);
            try
            {
                if (param == 1)
                    nsm.AddNamespace("p", null);
                else
                    nsm.RemoveNamespace("p", null);
            }
            catch (ArgumentNullException)
            {
                try
                {
                    if (param == 1)
                        nsm.AddNamespace("p", null);
                    else
                        nsm.RemoveNamespace("p", null);
                }
                catch (ArgumentNullException) { return TEST_PASS; }
            }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        //[Variation("nsm.AddNamespace(null, 'ns1')", Param = 1)]
        //[Variation("nsm.RemoveNamespace(null, 'ns1')", Param = 2)]
        public int v5()
        {
            int param = (int)CurVariation.Param;
            string xml = @"<a>p:foo</a>";
            ReloadSource(new StringReader(xml));
            DataReader.Read(); DataReader.Read();
            XmlNamespaceManager nsm = new XmlNamespaceManager(DataReader.NameTable);
            try
            {
                if (param == 1)
                    nsm.AddNamespace(null, "ns1");
                else
                    nsm.RemoveNamespace(null, "ns1");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    if (param == 1)
                        nsm.AddNamespace(null, "ns1");
                    else
                        nsm.RemoveNamespace(null, "ns1");
                }
                catch (ArgumentNullException) { return TEST_PASS; }
            }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        //[Variation("DataReader.ReadContentAs(null, nsm)")]
        public int v6()
        {
            string xml = @"<a>p:foo</a>";
            ReloadSource(new StringReader(xml));
            DataReader.Read(); DataReader.Read();
            if (IsBinaryReader()) DataReader.Read();
            XmlNamespaceManager nsm = new XmlNamespaceManager(DataReader.NameTable);
            nsm.AddNamespace("p", "ns1");
            try
            {
                XmlQualifiedName qname = (XmlQualifiedName)DataReader.ReadContentAs(null, nsm);
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XmlQualifiedName qname = (XmlQualifiedName)DataReader.ReadContentAs(null, nsm);
                }
                catch (ArgumentNullException) { return TEST_PASS; }
                catch (InvalidOperationException)
                {
                    if (IsSubtreeReader())
                        return TEST_PASS;
                }
            }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        //[Variation("nsm.AddNamespace('xml', 'ns1')")]
        public int v7()
        {
            string xml = @"<a>p:foo</a>";
            ReloadSource(new StringReader(xml));
            DataReader.Read(); DataReader.Read();
            XmlNamespaceManager nsm = new XmlNamespaceManager(DataReader.NameTable);
            try
            {
                nsm.AddNamespace("xml", "ns1");
            }
            catch (ArgumentException)
            {
                try
                {
                    nsm.AddNamespace("xml", "ns1");
                }
                catch (ArgumentException) { return TEST_PASS; }
            }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        //[Variation("Test Integrity of all values after Error")]
        public int V8()
        {
            ReloadSourceStr(@"<a xmlns:f='urn:foobar' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
                       "<b><c xsi:type='f:mytype'>some content</c></b></a>");
            try
            {
                DataReader.Read();
                DataReader.MoveToFirstAttribute();
                DataReader.ReadValueChunk(null, 0, 0);
            }
            catch (Exception)
            {
                CError.Compare(DataReader.AttributeCount, 2, "AttributeCount");
                CError.Compare(DataReader.BaseURI, string.Empty, "BaseURI");
                CError.Compare(DataReader.CanReadBinaryContent, IsCustomReader() ? false : true, "CanReadBinaryContent");
                CError.Compare(DataReader.CanReadValueChunk, (IsCharCheckingReader() || IsCustomReader() || IsXmlTextReader()) ? false : true, "CanReadValueChunk");
                CError.Compare(DataReader.Depth, 1, "Depth");
                CError.Compare(DataReader.EOF, false, "EOF");
                CError.Compare(DataReader.HasAttributes, true, "HasAttributes");
                CError.Compare(DataReader.IsDefault, false, "IsDefault");
                CError.Compare(DataReader.IsEmptyElement, false, "IsEmptyElement");
                if (!(IsCustomReader()))
                {
                    CError.Compare(DataReader.LineNumber, 1, "LN");
                    CError.Compare(DataReader.LinePosition, 4, "LP");
                }
                CError.Compare(DataReader.LocalName, "f", "LocalName");
                CError.Compare(DataReader.Name, "xmlns:f", "Name");
                CError.Compare(DataReader.NamespaceURI, "http://www.w3.org/2000/xmlns/", "NamespaceURI");
                CError.Compare(DataReader.NodeType, XmlNodeType.Attribute, "NodeType");
                CError.Compare(DataReader.Prefix, "xmlns", "Prefix");
                CError.Compare(DataReader.Read(), true, "Read");
                CError.Compare(DataReader.ReadInnerXml(), "<c xsi:type=\"f:mytype\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">some content</c>", "ReadInnerXml");
                CError.Compare(DataReader.ReadOuterXml(), string.Empty, "ReadOuterXml");
                CError.Compare(DataReader.ReadState, ReadState.EndOfFile, "ReadState");
                CError.Compare(DataReader.ReadToDescendant("b"), false, "ReadToDescendant");
                CError.Compare(DataReader.ReadToFollowing("b"), false, "ReadToFollowing");
                CError.Compare(DataReader.ReadToNextSibling("b"), false, "ReadToNextSibling");
                if ((DataReader.Settings != null))
                {
                    CError.Compare(DataReader.Settings.CheckCharacters, true, "CheckCharacters");
                    CError.Compare(DataReader.Settings.CloseInput, false, "CloseInput");
                    CError.Compare(DataReader.Settings.ConformanceLevel, ConformanceLevel.Fragment, "ConformanceLevel");
                    CError.Compare(DataReader.Settings.IgnoreComments, false, "IgnoreComments");
                    CError.Compare(DataReader.Settings.IgnoreProcessingInstructions, false, "IgnoreProcessingInstructions");
                    CError.Compare(DataReader.Settings.IgnoreWhitespace, false, "IgnoreWhitespace");
                    CError.Compare(DataReader.Settings.LineNumberOffset, 0, "LineNumberOffset");
                    CError.Compare(DataReader.Settings.LinePositionOffset, 0, "LinePositionOffset");
                    CError.Compare(DataReader.Settings.MaxCharactersInDocument, 0L, "MaxCharactersInDocument");
                    CError.Compare(DataReader.Settings.NameTable, null, "Settings.NameTable");
                }
                CError.Compare(DataReader.Value, string.Empty, "Value");
                CError.Compare(DataReader.ValueType, typeof(System.String), "ValueType");
                CError.Compare(DataReader.XmlLang, string.Empty, "XmlLang");
                CError.Compare(DataReader.XmlSpace, XmlSpace.None, "XmlSpace");
                CError.Compare(DataReader.GetAttribute("b"), null, "GetAttribute('b')");
                CError.Compare(DataReader.Equals(null), false, "Equals(null)");
                CError.Compare(DataReader.IsStartElement(), false, "IsStartElement()");
                CError.Compare(DataReader.LookupNamespace("b"), null, "LookupNamespace('b')");
                CError.Compare(DataReader.MoveToAttribute("b"), false, "MoveToAttribute('b')");
                CError.Compare(DataReader.MoveToContent(), XmlNodeType.None, "MoveToContent()");
                CError.Compare(DataReader.MoveToElement(), false, "MoveToElement()");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "MoveToFirstAttribute()");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "MoveToNextAttribute()");

                CError.Equals(DataReader.HasValue, false, "HasValue");
                CError.Equals(DataReader.CanResolveEntity, true, "CanResolveEntity");
                CError.Equals(DataReader.ReadAttributeValue(), false, "ReadAttributeValue()");
                try
                {
                    DataReader.ResolveEntity();
                    CError.Compare(false, "failed0");
                }
                catch (InvalidOperationException) { }
                try
                {
                    DataReader.ReadContentAsObject();
                    CError.Compare(false, "failed1");
                }
                catch (InvalidOperationException) { }
                try
                {
                    DataReader.ReadContentAsString();
                    CError.Compare(false, "failed2");
                }
                catch (InvalidOperationException) { }
            }
            return TEST_PASS;
        }

        //[Variation("2.Test Integrity of all values after Dispose")]
        public int V9a()
        {
            ReloadSource();
            DataReader.Dispose();
            DataReader.Dispose();
            DataReader.Dispose();

            CError.Compare(DataReader.AttributeCount, 0, "AttributeCount");
            CError.Compare(DataReader.BaseURI, string.Empty, "BaseURI");
            CError.Compare(DataReader.CanReadBinaryContent, IsCustomReader() ? false : true, "CanReadBinaryContent");
            CError.Compare(DataReader.CanReadValueChunk, (IsCharCheckingReader() || IsCustomReader() || IsXmlTextReader()) ? false : true, "CanReadValueChunk");
            CError.Compare(DataReader.Depth, 0, "Depth");
            CError.Compare(DataReader.EOF, IsSubtreeReader() ? true : false, "EOF");
            CError.Compare(DataReader.HasAttributes, false, "HasAttributes");
            CError.Compare(DataReader.IsDefault, false, "IsDefault");
            CError.Compare(DataReader.IsEmptyElement, false, "IsEmptyElement");
            if (!IsCustomReader())
            {
                CError.Compare(DataReader.LineNumber, 0, "LN");
                CError.Compare(DataReader.LinePosition, 0, "LP");
            }
            CError.Compare(DataReader.LocalName, String.Empty, "LocalName");
            CError.Compare(DataReader.Name, String.Empty, "Name");
            CError.Compare(DataReader.NamespaceURI, String.Empty, "NamespaceURI");
            CError.Compare(DataReader.NodeType, XmlNodeType.None, "NodeType");
            CError.Compare(DataReader.Prefix, String.Empty, "Prefix");
            CError.Compare(DataReader.Read(), IsCharCheckingReader(), "Read");
            CError.Compare(DataReader.ReadInnerXml(), String.Empty, "ReadInnerXml");
            CError.Compare(DataReader.ReadOuterXml(), String.Empty, "ReadOuterXml");
            CError.Compare(DataReader.ReadState, ReadState.Closed, "ReadState");
            CError.Compare(DataReader.ReadToDescendant("b"), false, "ReadToDescendant");
            CError.Compare(DataReader.ReadToFollowing("b"), false, "ReadToFollowing");
            CError.Compare(DataReader.ReadToNextSibling("b"), false, "ReadToNextSibling");
            if ((DataReader.Settings != null))
            {
                CError.Compare(DataReader.Settings.CheckCharacters, true, "CheckCharacters");
                CError.Compare(DataReader.Settings.CloseInput, false, "CloseInput");
                CError.Compare(DataReader.Settings.ConformanceLevel, ConformanceLevel.Document, "ConformanceLevel");
                CError.Compare(DataReader.Settings.IgnoreComments, false, "IgnoreComments");
                CError.Compare(DataReader.Settings.IgnoreProcessingInstructions, false, "IgnoreProcessingInstructions");
                CError.Compare(DataReader.Settings.IgnoreWhitespace, false, "IgnoreWhitespace");
                CError.Compare(DataReader.Settings.LineNumberOffset, 0, "LineNumberOffset");
                CError.Compare(DataReader.Settings.LinePositionOffset, 0, "LinePositionOffset");
                CError.Compare(DataReader.Settings.MaxCharactersInDocument, 0L, "MaxCharactersInDocument");
                CError.Compare(DataReader.Settings.NameTable, null, "Settings.NameTable");
            }
            CError.Compare(DataReader.Value, string.Empty, "Value");
            CError.Compare(DataReader.ValueType, typeof(System.String), "ValueType");
            CError.Compare(DataReader.XmlLang, string.Empty, "XmlLang");
            CError.Compare(DataReader.XmlSpace, XmlSpace.None, "XmlSpace");
            CError.Compare(DataReader.GetAttribute("b"), null, "GetAttribute('b')");
            CError.Compare(DataReader.Equals(null), false, "Equals(null)");
            CError.Compare(DataReader.IsStartElement(), false, "IsStartElement()");
            CError.Compare(DataReader.LookupNamespace("b"), null, "LookupNamespace('b')");
            CError.Compare(DataReader.MoveToAttribute("b"), false, "MoveToAttribute('b')");
            CError.Compare(DataReader.MoveToContent(), XmlNodeType.None, "MoveToContent()");
            CError.Compare(DataReader.MoveToElement(), false, "MoveToElement()");
            CError.Compare(DataReader.MoveToFirstAttribute(), false, "MoveToFirstAttribute()");
            CError.Compare(DataReader.MoveToNextAttribute(), false, "MoveToNextAttribute()");

            CError.Equals(DataReader.HasValue, false, "HasValue");
            CError.Equals(DataReader.CanResolveEntity, true, "CanResolveEntity");
            CError.Equals(DataReader.ReadAttributeValue(), false, "ReadAttributeValue()");
            try
            {
                DataReader.ResolveEntity();
                CError.Compare(false, "failed0");
            }
            catch (InvalidOperationException) { }
            try
            {
                DataReader.ReadContentAsObject();
                CError.Compare(false, "failed1");
            }
            catch (InvalidOperationException) { }
            try
            {
                DataReader.ReadContentAsString();
                CError.Compare(false, "failed2");
            }
            catch (InvalidOperationException) { }

            return TEST_PASS;
        }

        //[Variation("XmlCharType::IsName, IsNmToken")]
        public int v10()
        {
            CError.Compare(XmlReader.IsName("a"), "Error1");
            CError.Compare(!XmlReader.IsName("@a"), "Error2");
            CError.Compare(!XmlReader.IsName("b@a"), "Error3");
            CError.Compare(XmlReader.IsNameToken("a"), "Error4");
            CError.Compare(!XmlReader.IsNameToken("@a"), "Error5");
            CError.Compare(!XmlReader.IsNameToken("b@a"), "Error6");
            return TEST_PASS;
        }

        //[Variation("XmlReader[String.Empty])",  Param = 1)]
        //[Variation("XmlReader[String.Empty, String.Empty]",  Param = 2)]
        //[Variation("XmlReader.GetAttribute(String.Empty)",  Param = 3)]
        //[Variation("XmlReader.GetAttribute(String.Empty, String.Empty)",  Param = 4)]
        //[Variation("XmlReader.MoveToAttribute(String.Empty)",  Param = 5)]
        //[Variation("XmlReader.MoveToAttribute(String.Empty, String.Empty)",  Param = 6)]
        //[Variation("XmlReader.ReadElementContentAs(String.Empty, String.Empty, String.Empty, String.Empty)",  Param = 9)]
        //[Variation("XmlReader.ReadElementContentAs(String.Empty, String.Empty, 'a', String.Empty)",  Param = 10)]        
        //[Variation("XmlReader.ReadElementContentAsBoolean(String.Empty, String.Empty)",  Param = 13)]
        //[Variation("XmlReader.ReadElementContentAsBoolean('a', String.Empty)",  Param = 14)]
        //[Variation("XmlReader.ReadElementContentAsDateTime(String.Empty, String.Empty)",  Param = 15)]
        //[Variation("XmlReader.ReadElementContentAsDateTime('a', String.Empty)",  Param = 16)]
        //[Variation("XmlReader.ReadElementContentAsDecimal(String.Empty, String.Empty)",  Param = 17)]
        //[Variation("XmlReader.ReadElementContentAsDecimal('a', String.Empty)",  Param = 18)]
        //[Variation("XmlReader.ReadElementContentAsDouble(String.Empty, String.Empty)",  Param = 19)]
        //[Variation("XmlReader.ReadElementContentAsDouble('a', String.Empty)",  Param = 20)]
        //[Variation("XmlReader.ReadElementContentAsFloat(String.Empty, String.Empty)",  Param = 21)]
        //[Variation("XmlReader.ReadElementContentAsFloat('a', String.Empty)",  Param = 22)]
        //[Variation("XmlReader.ReadElementContentAsInt(String.Empty, String.Empty)",  Param = 23)]
        //[Variation("XmlReader.ReadElementContentAsInt('a', String.Empty)",  Param = 24)]
        //[Variation("XmlReader.ReadElementContentAsLong(String.Empty, String.Empty)",  Param = 25)]
        //[Variation("XmlReader.ReadElementContentAsLong('a', String.Empty)",  Param = 26)]
        //[Variation("XmlReader.ReadElementContentAsObject(String.Empty, String.Empty)",  Param = 27)]
        //[Variation("XmlReader.ReadElementContentAsObject('a', String.Empty)",  Param = 28)]
        //[Variation("XmlReader.ReadElementContentAsString(String.Empty, String.Empty)",  Param = 29)]
        //[Variation("XmlReader.ReadElementContentAsString('a', String.Empty)",  Param = 30)]
        //[Variation("XmlReader.ReadToDescendant(String.Empty)",  Param = 31)]
        //[Variation("XmlReader.ReadToDescendant(String.Empty, String.Empty)",  Param = 32)]
        //[Variation("XmlReader.ReadToDescendant('a', String.Empty)",  Param = 33)]
        //[Variation("XmlReader.ReadToFollowing(String.Empty)",  Param = 34)]
        //[Variation("XmlReader.ReadToFollowing(String.Empty, String.Empty)",  Param = 35)]
        //[Variation("XmlReader.ReadToFollowing('a', String.Empty)",  Param = 36)]
        //[Variation("XmlReader.ReadToNextSibling(String.Empty)",  Param = 37)]
        //[Variation("XmlReader.ReadToNextSibling(String.Empty, String.Empty)",  Param = 38)]
        //[Variation("XmlReader.ReadToNextSibling('a', String.Empty)",  Param = 39)]
        public int v11()
        {
            int param = (int)CurVariation.Param;
            ReloadSource(new StringReader(xmlStr));
            DataReader.Read();
            if (IsBinaryReader()) DataReader.Read();
            string s = "";
            switch (param)
            {
                case 1: s = DataReader[String.Empty]; return TEST_PASS;
                case 2: s = DataReader[String.Empty, String.Empty]; return TEST_PASS;
                case 3: s = DataReader.GetAttribute(String.Empty); return TEST_PASS;
                case 4: s = DataReader.GetAttribute(String.Empty, String.Empty); return TEST_PASS;
                case 5: DataReader.MoveToAttribute(String.Empty); return TEST_PASS;
                case 6: DataReader.MoveToAttribute(String.Empty, String.Empty); return TEST_PASS;
                case 10: DataReader.ReadElementContentAs(typeof(String), null, "a", String.Empty); return TEST_PASS;
                case 28: DataReader.ReadElementContentAsObject("a", String.Empty); return TEST_PASS;
                case 30: DataReader.ReadElementContentAsString("a", String.Empty); return TEST_PASS;
                case 33: DataReader.ReadToDescendant("a", String.Empty); return TEST_PASS;
                case 36: DataReader.ReadToFollowing("a", String.Empty); return TEST_PASS;
                case 39: DataReader.ReadToNextSibling("a", String.Empty); return TEST_PASS;
            }
            try
            {
                switch (param)
                {
                    case 9: DataReader.ReadElementContentAs(typeof(String), null, String.Empty, String.Empty); break;
                    case 13: DataReader.ReadElementContentAsBoolean(String.Empty, String.Empty); break;
                    case 14: DataReader.ReadElementContentAsBoolean("a", String.Empty); break;
                    case 17: DataReader.ReadElementContentAsDecimal(String.Empty, String.Empty); break;
                    case 18: DataReader.ReadElementContentAsDecimal("a", String.Empty); break;
                    case 19: DataReader.ReadElementContentAsDouble(String.Empty, String.Empty); break;
                    case 20: DataReader.ReadElementContentAsDouble("a", String.Empty); break;
                    case 21: DataReader.ReadElementContentAsFloat(String.Empty, String.Empty); break;
                    case 22: DataReader.ReadElementContentAsFloat("a", String.Empty); break;
                    case 23: DataReader.ReadElementContentAsInt(String.Empty, String.Empty); break;
                    case 24: DataReader.ReadElementContentAsInt("a", String.Empty); break;
                    case 25: DataReader.ReadElementContentAsLong(String.Empty, String.Empty); break;
                    case 26: DataReader.ReadElementContentAsLong("a", String.Empty); break;
                    case 27: DataReader.ReadElementContentAsObject(String.Empty, String.Empty); break;
                    case 29: DataReader.ReadElementContentAsString(String.Empty, String.Empty); break;
                    case 31: DataReader.ReadToDescendant(String.Empty); break;
                    case 32: DataReader.ReadToDescendant(String.Empty, String.Empty); break;
                    case 34: DataReader.ReadToFollowing(String.Empty); break;
                    case 35: DataReader.ReadToFollowing(String.Empty, String.Empty); break;
                    case 37: DataReader.ReadToNextSibling(String.Empty); break;
                    case 38: DataReader.ReadToNextSibling(String.Empty, String.Empty); break;
                }
            }
            catch (ArgumentException)
            {
                try
                {
                    switch (param)
                    {
                        case 9: DataReader.ReadElementContentAs(typeof(String), null, String.Empty, String.Empty); break;
                        case 13: DataReader.ReadElementContentAsBoolean(String.Empty, String.Empty); break;
                        case 14: DataReader.ReadElementContentAsBoolean("a", String.Empty); break;
                        case 17: DataReader.ReadElementContentAsDecimal(String.Empty, String.Empty); break;
                        case 18: DataReader.ReadElementContentAsDecimal("a", String.Empty); break;
                        case 19: DataReader.ReadElementContentAsDouble(String.Empty, String.Empty); break;
                        case 20: DataReader.ReadElementContentAsDouble("a", String.Empty); break;
                        case 21: DataReader.ReadElementContentAsFloat(String.Empty, String.Empty); break;
                        case 22: DataReader.ReadElementContentAsFloat("a", String.Empty); break;
                        case 23: DataReader.ReadElementContentAsInt(String.Empty, String.Empty); break;
                        case 24: DataReader.ReadElementContentAsInt("a", String.Empty); break;
                        case 25: DataReader.ReadElementContentAsLong(String.Empty, String.Empty); break;
                        case 26: DataReader.ReadElementContentAsLong("a", String.Empty); break;
                        case 27: DataReader.ReadElementContentAsObject(String.Empty, String.Empty); break;
                        case 29: DataReader.ReadElementContentAsString(String.Empty, String.Empty); break;
                        case 31: DataReader.ReadToDescendant(String.Empty); break;
                        case 32: DataReader.ReadToDescendant(String.Empty, String.Empty); break;
                        case 34: DataReader.ReadToFollowing(String.Empty); break;
                        case 35: DataReader.ReadToFollowing(String.Empty, String.Empty); break;
                        case 37: DataReader.ReadToNextSibling(String.Empty); break;
                        case 38: DataReader.ReadToNextSibling(String.Empty, String.Empty); break;
                    }
                }
                catch (ArgumentException) { return TEST_PASS; }
            }
            catch (NotSupportedException)
            {
                if (IsCustomReader() && (param == 7 || param == 8 || param == 11 || param == 12))
                    return TEST_PASS;
                return TEST_FAIL;
            }
            catch (FormatException) { return TEST_PASS; }
            finally
            {
                DataReader.Close();
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "XmlReaderSettings.ConformanceLevel - invalid values", Priority = 2)]
        public int var12()
        {
            return TEST_SKIPPED;
        }

        //[Variation(Desc = "XmlReaderSettings.LineNumberOffset - invalid values",  Param = 1)]
        //[Variation(Desc = "XmlReaderSettings.LinePositionOffset - invalid values",  Param = 2)]
        public int var13()
        {
            int param = (int)CurVariation.Param;
            XmlReaderSettings rs = new XmlReaderSettings();
            if (param == 1)
                rs.LineNumberOffset = -10;
            else
                rs.LinePositionOffset = -10;
            return TEST_PASS;
        }

        //[Variation("XmlReader.ReadContentAsBase64(b, -1, 0)",  Param = 1)]
        //[Variation("XmlReader.ReadContentAsBinHex(b, -1, 0)",  Param = 2)]     
        //[Variation("XmlReader.ReadContentAsBase64(b, 0, -1)",  Param = 3)]
        //[Variation("XmlReader.ReadContentAsBinHex(b, 0, -1)",  Param = 4)]
        //[Variation("XmlReader.ReadContentAsBase64(b, 0, 2)",  Param = 5)]
        //[Variation("XmlReader.ReadContentAsBinHex(b, 0, 2)",  Param = 6)]

        //[Variation("XmlReader.ReadElementContentAsBase64(b, -1, 0)",  Param = 7)]
        //[Variation("XmlReader.ReadElementContentAsBinHex(b, -1, 0)",  Param = 8)] 
        //[Variation("XmlReader.ReadValueChunk(c, 0, -1)",  Param = 9)]        
        //[Variation("XmlReader.ReadElementContentAsBase64(b, 0, -1)",  Param = 10)]
        //[Variation("XmlReader.ReadElementContentAsBinHex(b, 0, -1)",  Param = 11)]
        //[Variation("XmlReader.ReadValueChunk(c, 0, -1)",  Param = 12)]        
        //[Variation("XmlReader.ReadElementContentAsBase64(b, 0, 2)",  Param = 13)]
        //[Variation("XmlReader.ReadElementContentAsBinHex(b, 0, 2)",  Param = 14)]
        //[Variation("XmlReader.ReadValueChunk(c, 0, 2)",  Param = 15)]
        //[Variation("XmlReader.ReadValueChunk(c, -1, 1)", Param = 16)]
        public int v14()
        {
            int param = (int)CurVariation.Param;
            ReloadSource(new StringReader(@"<a xmlns:f='urn:foobar' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
                       "<b><c xsi:type='f:mytype'>some content</c></b></a>"));
            char[] c = new char[1];
            byte[] b = new byte[1];

            try
            {
                switch (param)
                {
                    case 1: DataReader.ReadContentAsBase64(b, -1, 0); break;
                    case 2: DataReader.ReadContentAsBinHex(b, -1, 0); break;
                    case 3: DataReader.ReadContentAsBase64(b, 0, -1); break;
                    case 4: DataReader.ReadContentAsBinHex(b, 0, -1); break;
                    case 5: DataReader.ReadContentAsBase64(b, 0, 2); break;
                    case 6: DataReader.ReadContentAsBinHex(b, 0, 2); break;
                    case 7: DataReader.ReadElementContentAsBase64(b, -1, 0); break;
                    case 8: DataReader.ReadElementContentAsBinHex(b, -1, 0); break;
                    case 9: DataReader.Read(); DataReader.MoveToFirstAttribute(); DataReader.ReadValueChunk(c, 0, -1); break;
                    case 10: DataReader.ReadElementContentAsBase64(b, 0, -10); break;
                    case 11: DataReader.ReadElementContentAsBinHex(b, 0, -1); break;
                    case 12: DataReader.Read(); DataReader.MoveToFirstAttribute(); DataReader.ReadValueChunk(c, 0, -1); break;
                    case 13: DataReader.ReadElementContentAsBase64(b, 0, 2); break;
                    case 14: DataReader.ReadElementContentAsBinHex(b, 0, 2); break;
                    case 15: DataReader.Read(); DataReader.MoveToFirstAttribute(); DataReader.ReadValueChunk(c, 0, 2); break;
                    case 16: DataReader.Read(); DataReader.MoveToFirstAttribute(); DataReader.ReadValueChunk(c, -1, 2); break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: DataReader.ReadContentAsBase64(b, -1, 0); break;
                        case 2: DataReader.ReadContentAsBinHex(b, -1, 0); break;
                        case 3: DataReader.ReadContentAsBase64(b, 0, -1); break;
                        case 4: DataReader.ReadContentAsBinHex(b, 0, -1); break;
                        case 5: DataReader.ReadContentAsBase64(b, 0, 2); break;
                        case 6: DataReader.ReadContentAsBinHex(b, 0, 2); break;
                        case 7: DataReader.ReadElementContentAsBase64(b, -1, 0); break;
                        case 8: DataReader.ReadElementContentAsBinHex(b, -1, 0); break;
                        case 9: DataReader.ReadValueChunk(c, 0, -1); break;
                        case 10: DataReader.ReadElementContentAsBase64(b, 0, -10); break;
                        case 11: DataReader.ReadElementContentAsBinHex(b, 0, -1); break;
                        case 12: DataReader.ReadValueChunk(c, 0, -1); break;
                        case 13: DataReader.ReadElementContentAsBase64(b, 0, 2); break;
                        case 14: DataReader.ReadElementContentAsBinHex(b, 0, 2); break;
                        case 15: DataReader.ReadValueChunk(c, 0, 2); break;
                        case 16: DataReader.ReadValueChunk(c, -1, 2); break;
                    }
                }
                catch (ArgumentOutOfRangeException) { return TEST_PASS; }
            }
            catch (NotSupportedException) { if (IsCustomReader() || IsCharCheckingReader() || IsXmlTextReader()) return TEST_PASS; }
            finally
            {
                DataReader.Close();
            }
            return ((IsCharCheckingReader() && param >= 1 && param <= 6) || IsSubtreeReader()) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "DataReader.Settings.LineNumberOffset - readonly",  Param = 1)]
        //[Variation(Desc = "DataReader.Settings.LinePositionOffset - readonly",  Param = 2)]
        //[Variation(Desc = "DataReader.Settings.CheckCharacters - readonly",  Param = 3)]
        //[Variation(Desc = "DataReader.Settings.CloseInput - readonly",  Param = 4)]
        //[Variation(Desc = "DataReader.Settings.ConformanceLevel - readonly",  Param = 5)]
        //[Variation(Desc = "DataReader.Settings.IgnoreComments - readonly",  Param = 6)]
        //[Variation(Desc = "DataReader.Settings.IgnoreProcessingInstructions - readonly",  Param = 7)]
        //[Variation(Desc = "DataReader.Settings.IgnoreWhitespace - readonly",  Param = 8)]
        //[Variation(Desc = "DataReader.Settings.MaxCharactersInDocument - readonly",  Param = 9)]
        //[Variation(Desc = "DataReader.Settings.ProhibitDtd - readonly",  Param = 10)]
        //[Variation(Desc = "DataReader.Settings.XmlResolver - readonly",  Param = 11)]
        //[Variation(Desc = "DataReader.Settings.MaxCharactersFromEntities - readonly",  Param = 12)]
        //[Variation(Desc = "DataReader.Settings.DtdProcessing - readonly", Param = 13)]
        public int var15()
        {
            if (IsCustomReader() || IsXmlTextReader()) return TEST_SKIPPED;
            int param = (int)CurVariation.Param;
            ReloadSource(new StringReader(@"<a xmlns:f='urn:foobar' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
                       "<b><c xsi:type='f:mytype'>some content</c></b></a>"));
            try
            {
                switch (param)
                {
                    case 1: DataReader.Settings.LineNumberOffset = -10; break;
                    case 2: DataReader.Settings.LinePositionOffset = -10; break;
                    case 3: DataReader.Settings.CheckCharacters = false; break;
                    case 4: DataReader.Settings.CloseInput = false; break;
                    case 5: DataReader.Settings.ConformanceLevel = ConformanceLevel.Fragment; break;
                    case 6: DataReader.Settings.IgnoreComments = false; break;
                    case 7: DataReader.Settings.IgnoreProcessingInstructions = false; break;
                    case 8: DataReader.Settings.IgnoreWhitespace = false; break;
                    case 9: DataReader.Settings.MaxCharactersInDocument = -10; break;
                    case 12: DataReader.Settings.MaxCharactersFromEntities = -10; break;
                    case 13: DataReader.Settings.DtdProcessing = (DtdProcessing)(-10); break;
                }
            }
            catch (XmlException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: DataReader.Settings.LineNumberOffset = -10; break;
                        case 2: DataReader.Settings.LinePositionOffset = -10; break;
                        case 3: DataReader.Settings.CheckCharacters = false; break;
                        case 4: DataReader.Settings.CloseInput = false; break;
                        case 5: DataReader.Settings.ConformanceLevel = ConformanceLevel.Fragment; break;
                        case 6: DataReader.Settings.IgnoreComments = false; break;
                        case 7: DataReader.Settings.IgnoreProcessingInstructions = false; break;
                        case 8: DataReader.Settings.IgnoreWhitespace = false; break;
                        case 9: DataReader.Settings.MaxCharactersInDocument = -10; break;
                        case 12: DataReader.Settings.MaxCharactersFromEntities = -10; break;
                        case 13: DataReader.Settings.DtdProcessing = (DtdProcessing)(-10); break;
                    }
                }
                catch (XmlException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation("Readcontentas in close state and call ReadContentAsBase64", Param = 1)]
        //[Variation("Readcontentas in close state and call ReadContentAsBinHex", Param = 2)]
        //[Variation("Readcontentas in close state and call ReadElementContentAsBase64", Param = 3)]
        //[Variation("Readcontentas in close state and call ReadElementContentAsBinHex", Param = 4)]
        //[Variation("Readcontentas in close state and call ReadValueChunk", Param = 5)]

        //[Variation("XmlReader[a])",  Param = 6)]
        //[Variation("XmlReader[a, b]",  Param = 7)]
        //[Variation("XmlReader.GetAttribute(a)",  Param = 8)]
        //[Variation("XmlReader.GetAttribute(a, b)",  Param = 9)]
        //[Variation("XmlReader.MoveToAttribute(a)",  Param = 10)]
        //[Variation("XmlReader.MoveToAttribute(a, b)",  Param = 11)]
        //[Variation("XmlReader.ReadElementContentAs(typeof(String), null)",  Param = 12)]
        //[Variation("XmlReader.ReadElementContentAsObject()",  Param = 13)]
        //[Variation("XmlReader.ReadElementContentAsString()",  Param = 14)]
        //[Variation("XmlReader.ReadToDescendant(a, b)",  Param = 15)]
        //[Variation("XmlReader.ReadToFollowing(a, b)",  Param = 16)]
        //[Variation("XmlReader.ReadToNextSibling(a, b)",  Param = 17)]
        //[Variation("XmlReader.ReadElementContentAs(typeof(String), null)",  Param = 18)]
        //[Variation("XmlReader.ReadElementContentAsBoolean(a,b)",  Param = 19)]
        //[Variation("XmlReader.ReadElementContentAsBoolean()",  Param = 20)]
        //[Variation("XmlReader.ReadElementContentAsDateTime(a,b)",  Param = 21)]
        //[Variation("XmlReader.ReadElementContentAsDateTime()",  Param = 22)]
        //[Variation("XmlReader.ReadElementContentAsDecimal(a,b)",  Param = 23)]
        //[Variation("XmlReader.ReadElementContentAsDecimal()",  Param = 24)]
        //[Variation("XmlReader.ReadElementContentAsDouble(a,b)",  Param = 25)]
        //[Variation("XmlReader.ReadElementContentAsDouble()",  Param = 26)]
        //[Variation("XmlReader.ReadElementContentAsFloat(a,b)",  Param = 27)]
        //[Variation("XmlReader.ReadElementContentAsFloat()",  Param = 28)]
        //[Variation("XmlReader.ReadElementContentAsInt(a,b)",  Param = 29)]
        //[Variation("XmlReader.ReadElementContentAsInt()",  Param = 30)]
        //[Variation("XmlReader.ReadElementContentAsLong(a,b)",  Param = 31)]
        //[Variation("XmlReader.ReadElementContentAsLong()",  Param = 32)]
        //[Variation("XmlReader.ReadElementContentAsObject(a,b)",  Param = 33)]
        //[Variation("XmlReader.ReadElementContentAsString(a,b)",  Param = 34)]
        //[Variation("XmlReader.ReadToDescendant(a)",  Param = 35)]
        //[Variation("XmlReader.ReadToFollowing(a)",  Param = 36)]
        //[Variation("XmlReader.ReadToNextSibling(a)",  Param = 37)]
        //[Variation("XmlReader.ReadAttributeValue()",  Param = 38)]
        //[Variation("XmlReader.ResolveEntity()",  Param = 39)]
        public int V16()
        {
            int param = (int)CurVariation.Param;
            byte[] buffer = new byte[3];
            int[] skipParams = new int[] { 1, 2, 3, 4, 5, 35, 36, 37, 38 };
            char[] chars = new char[3];
            string s = "";
            ReloadSource(new StringReader("<elem0>123 $%^ 56789 abcdefg hij klmn opqrst  12345 uvw xy ^ z</elem0>"));
            DataReader.Read();
            DataReader.Close();
            try
            {
                DataReader.ReadContentAs(typeof(string), null);
            }
            catch (InvalidOperationException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: CError.Compare(DataReader.ReadContentAsBase64(buffer, 0, 3), 0, "size"); break;
                        case 2: CError.Compare(DataReader.ReadContentAsBinHex(buffer, 0, 3), 0, "size"); break;
                        case 3: CError.Compare(DataReader.ReadElementContentAsBase64(buffer, 0, 3), 0, "size"); break;
                        case 4: CError.Compare(DataReader.ReadElementContentAsBinHex(buffer, 0, 3), 0, "size"); break;
                        case 5: CError.Compare(DataReader.ReadValueChunk(chars, 0, 3), 0, "size"); break;
                        case 6: s = DataReader["a"]; return TEST_PASS;
                        case 7: s = DataReader["a", "b"]; return TEST_PASS;
                        case 8: s = DataReader.GetAttribute("a"); return TEST_PASS;
                        case 9: s = DataReader.GetAttribute("a", "b"); return TEST_PASS;
                        case 10: DataReader.MoveToAttribute("a"); return TEST_PASS;
                        case 11: DataReader.MoveToAttribute("a", "b"); return TEST_PASS;
                        case 12: DataReader.ReadElementContentAs(typeof(String), null, "a", "b"); return TEST_PASS;
                        case 13: DataReader.ReadElementContentAsObject(); return TEST_PASS;
                        case 14: DataReader.ReadElementContentAsString(); return TEST_PASS;
                        case 15: DataReader.ReadToDescendant("a", "b"); return TEST_PASS;
                        case 16: DataReader.ReadToFollowing("a", "b"); return TEST_PASS;
                        case 17: DataReader.ReadToNextSibling("a", "b"); return TEST_PASS;
                        case 18: DataReader.ReadElementContentAs(typeof(String), null); break;
                        case 19: DataReader.ReadElementContentAsBoolean("a", "b"); break;
                        case 20: DataReader.ReadElementContentAsBoolean(); break;
                        case 23: DataReader.ReadElementContentAsDecimal("a", "b"); break;
                        case 24: DataReader.ReadElementContentAsDecimal(); break;
                        case 25: DataReader.ReadElementContentAsDouble("a", "b"); break;
                        case 26: DataReader.ReadElementContentAsDouble(); break;
                        case 27: DataReader.ReadElementContentAsFloat("a", "b"); break;
                        case 28: DataReader.ReadElementContentAsFloat(); break;
                        case 29: DataReader.ReadElementContentAsInt("a", "b"); break;
                        case 30: DataReader.ReadElementContentAsInt(); break;
                        case 31: DataReader.ReadElementContentAsLong(); break;
                        case 32: DataReader.ReadElementContentAsLong("a", "b"); break;
                        case 33: DataReader.ReadElementContentAsObject("a", "b"); break;
                        case 34: DataReader.ReadElementContentAsString("a", "b"); break;
                        case 35: DataReader.ReadToDescendant("a"); break;
                        case 36: DataReader.ReadToFollowing("a"); break;
                        case 37: DataReader.ReadToNextSibling("a"); break;
                        case 38: DataReader.ReadAttributeValue(); break;
                        case 39: DataReader.ResolveEntity(); break;
                    }
                }
                catch (NotSupportedException) { return TEST_PASS; }
                catch (InvalidOperationException) { return TEST_PASS; }
                catch (XmlException) { return TEST_PASS; }
            }
            foreach (int p in skipParams)
            {
                if (param == p) return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation("Assertion when creating validating reader from a XmlReader.Create reader")]
        public int Dev10_67883()
        {
            return TEST_SKIPPED;
        }
    }
}
