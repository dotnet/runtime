// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase TCXML BaseGeneral
    //
    ////////////////////////////////////////////////////////////////
    public abstract class TCXMLReaderBaseGeneral : CDataReaderTestCase
    {
        public static string strAttr = "Attr";
        public static string strNamespace = "http://www.foo.com";

        public const int ASCII_ZERO = 48;

        public const String ST_ENTTEST_NAME = "ENTITY1";

        public const String ST_TEXT_VALUE = "xxx";

        public const String ST_DECIMAL = "#decimal";
        public const String ST_HEXIDECIMAL = "#hexidecimal";
        public const String ST_GT = "gt";
        public const String ST_LT = "lt";

        public const String ST_IGNORE_ENTITIES = "xxx&gt;xxx&#66;xxx&#x44;xxx&e1;xxx";
        public const String ST_EXPAND_ENTITIES = "xxx>xxxBxxxDxxxe1fooxxx";
        public const String ST_EXPAND_ENTITIES2 = "xxx&gt;xxxBxxxDxxxe1fooxxx";
        public const String ST_EXPAND_ENTITIES3 = "xxx&gt;xxxBxxxDxxx&e1;xxx";
        public const String ST_EXPAND_CHAR_ENTITIES = "xxx>xxxBxxxDxxx";
        public const String ST_ENT1_ATT_EXPAND_ENTITIES = "xxx<xxxAxxxCxxxNO_REFERENCEe1;xxx";
        public const String ST_ENT1_ATT_EXPAND_CHAR_ENTITIES = "xxx<xxxAxxxCxxx&e1;xxx";

        public const String ST_ENT1_ATT_IGNORE_ENTITIES = "xxx&lt;xxx&#65;xxx&#x43;xxx&e1;xxx";
        public const String ST_ENT1_ATT_EXPAND_CHAR_ENTITIES2 = "xxx&lt;xxxAxxxCxxx&e1;xxx";
        public const String ST_ENT1_ATT_EXPAND_CHAR_ENTITIES3 = "xxx<xxxAxxxCxxx";
        public const String ST_ENT1_ATT_EXPAND_CHAR_ENTITIES4 = "xxx&lt;xxxAxxxCxxxe1fooxxx";

        private const string _NOVALIDATION = "None";
        private const string _VALIDATION_ONE = "Validation_One";
        private const string _VALIDATION_TWO = "Validation_Two";

        protected XmlReader MainReader = null;

        public virtual void PostReloadSource()
        {
        }

        public virtual void CloseReader()
        {
            if (DataReader.Internal != null && !IsSubtreeReader())
            {
                DataReader.Close();
            }
        }

        public virtual void ReloadSource(MyDict<string, object> options)
        {
            CloseReader();
            options[ReaderFactory.HT_CURDESC] = GetDescription().ToLowerInvariant();
            options[ReaderFactory.HT_CURVAR] = CurVariation.Desc.ToLowerInvariant();
            DataReader.Internal = TestModule.ReaderFactory.Create(options);
            PostReloadSource();
        }

        public virtual void ReloadSource()
        {
            EREADER_TYPE eReaderType = EREADER_TYPE.GENERIC;

            if (GetDescription().ToUpperInvariant() == "XSLTREADER")
                eReaderType = EREADER_TYPE.XSLT_COPY;

            ReloadSource(eReaderType);
        }

        public virtual void ReloadSource(EREADER_TYPE eReaderType)
        {
            string filename = TestFiles.GetTestFileName(eReaderType);

            ReloadSource(filename);
        }

        public virtual void ReloadSource(string filename)
        {
            MyDict<string, object> ht = new MyDict<string, object>();
            ht[ReaderFactory.HT_FILENAME] = filename;

            ReloadSource(ht);
        }

        public virtual void ReloadSource(StringReader strRdr)
        {
            MyDict<string, object> ht = new MyDict<string, object>();
            ht[ReaderFactory.HT_STRINGREADER] = strRdr;
            ReloadSource(ht);
        }

        public virtual void ReloadSource(Stream stream, string filename)
        {
            MyDict<string, object> ht = new MyDict<string, object>();
            ht[ReaderFactory.HT_FILENAME] = filename;
            ht[ReaderFactory.HT_STREAM] = stream;
            ReloadSource(ht);
        }

        public virtual void ReloadSourceStr(string strxml)
        {
            MyDict<string, object> ht = new MyDict<string, object>();
            ht[ReaderFactory.HT_FRAGMENT] = strxml;
            ReloadSource(ht);
        }

        public override void PostExecuteVariation(int index, object param)
        {
            if (DataReader.Internal != null && !IsSubtreeReader())
            {
            }

            if (MainReader != null)
            {
            }
        }

        //////////////////////////////////////////
        // PositionOnNodeType
        //////////////////////////////////////////
        protected void PositionOnNodeType(XmlNodeType nodeType)
        {
            DataReader.PositionOnNodeType(nodeType);
        }

        protected int FindNodeType(XmlNodeType nodeType)
        {
            return DataReader.FindNodeType(nodeType);
        }

        protected bool IsXsltReader()
        {
            return GetDescription().ToUpperInvariant() == "XSLTREADER";
        }

        protected bool IsXmlTextReader()
        {
            return (GetDescription().ToUpperInvariant() == "XMLREADER" ||
                            GetDescription().ToUpperInvariant() == "XMLTEXTREADER");
        }

        protected bool IsXmlValidatingReader()
        {
            return GetDescription().ToUpperInvariant() == "XMLVALIDATINGREADER";
        }

        protected bool IsXmlNodeReader()
        {
            return GetDescription().ToUpperInvariant() == "XMLNODEREADER";
        }

        protected bool IsXmlNodeReaderDataDoc()
        {
            return GetDescription().ToUpperInvariant() == "XMLNODEREADER(DATADOC)";
        }

        protected bool IsCharCheckingReader()
        {
            return GetDescription().ToUpperInvariant() == "CHARCHECKINGREADER";
        }

        protected bool IsWrappedReader()
        {
            return (GetDescription().ToUpperInvariant() == "WRAPPEDREADER");
        }

        protected bool IsCoreReader()
        {
            return (GetDescription().ToUpperInvariant() == "FACTORYREADER" ||
                GetDescription().ToUpperInvariant() == "COREVALIDATINGREADER" ||
                GetDescription().ToUpperInvariant() == "XSDVALIDATINGREADER" ||
                GetDescription().ToUpperInvariant() == "BINARYREADER" ||
                GetDescription().ToUpperInvariant() == "SUBTREEREADER" ||
                GetDescription().ToUpperInvariant() == "CUSTOMINHERITEDREADER" ||
                GetDescription().ToUpperInvariant() == "CHARCHECKINGREADER" ||
                GetDescription().ToUpperInvariant() == "WRAPPEDREADER");
        }

        protected bool IsFactoryReader()
        {
            return (GetDescription().ToUpperInvariant() == "FACTORYREADER" ||
                GetDescription().ToUpperInvariant() == "COREVALIDATINGREADER" ||
                GetDescription().ToUpperInvariant() == "XSDVALIDATINGREADER");
        }

        protected bool IsFactoryTextReader()
        {
            return (GetDescription().ToUpperInvariant() == "FACTORYREADER" ||
                GetDescription().ToUpperInvariant() == "CUSTOMINHERITEDREADER");
        }

        protected bool IsCustomReader()
        {
            return GetDescription().ToUpperInvariant().StartsWith("CUSTOMINHERITEDREADER");
        }

        protected bool IsFactoryValidatingReader()
        {
            return (GetDescription().ToUpperInvariant() == "COREVALIDATINGREADER" || GetDescription().ToUpperInvariant() == "XSDVALIDATINGREADER");
        }

        protected bool IsBinaryReader()
        {
            return (GetDescription().ToUpperInvariant() == "BINARYREADER");
        }

        protected bool IsSubtreeReader()
        {
            return (GetDescription().ToUpperInvariant() == "SUBTREEREADER");
        }

        protected bool IsXPathNavigatorReader()
        {
            return (GetDescription().ToUpperInvariant() == "NAVIGATORREADER");
        }

        protected bool IsRoundTrippedReader()
        {
            return IsBinaryReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader() || IsXsltReader();
        }

        public bool CheckCanReadBinaryContent()
        {
            byte[] buffer = new byte[1];

            if (!DataReader.CanReadBinaryContent)
            {
                try
                {
                    int nBytes = 0;
                    switch ((int)new Random().Next(4))
                    {
                        case 0:
                            CError.WriteLineIgnore("Selecting RCABH");
                            nBytes = DataReader.ReadContentAsBinHex(buffer, 0, 1);
                            break;
                        case 1:
                            CError.WriteLineIgnore("Selecting RECABH");
                            nBytes = DataReader.ReadElementContentAsBinHex(buffer, 0, 1);
                            break;
                        case 2:
                            CError.WriteLineIgnore("Selecting RCAB64");
                            nBytes = DataReader.ReadContentAsBase64(buffer, 0, 1);
                            break;
                        case 3:
                            CError.WriteLineIgnore("Selecting RECAB64");
                            nBytes = DataReader.ReadElementContentAsBase64(buffer, 0, 1);
                            break;
                    }
                    throw new CTestFailedException("ReadContentAsBinHex doesn't throw NotSupportedException");
                }
                catch (NotSupportedException)
                {
                    return true;
                }
            }
            return false;
        }

        private static string s_strValidation = _NOVALIDATION;

        public string StrValidation
        {
            get { return s_strValidation; }
            set { s_strValidation = value; }
        }

        public void DumpStat()
        {
            if (DataReader == null)
            {
                CError.WriteLine("Reader not initialized");
                return;
            }

            CError.WriteLine("Dumping DataReader Status ... ");
            CError.WriteLine("Status : " + DataReader.ReadState);
            CError.WriteLine("Name = " + DataReader.Name);
            CError.WriteLine("Value = " + DataReader.Value);
            CError.WriteLine("ValueType = " + DataReader.ValueType);
            CError.WriteLine("NodeType = " + DataReader.NodeType);
            CError.WriteLine("-----------------");
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML Depth
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCDepth : TCXMLReaderBaseGeneral
    {
        [Variation("XmlReader Depth at the Root", Pri = 0)]
        public int TestDepth1()
        {
            ReloadSource();
            int iDepth = 0;

            DataReader.PositionOnElement("PLAY");
            CError.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
            DataReader.Close();
            return TEST_PASS;
        }

        [Variation("XmlReader Depth at Empty Tag")]
        public int TestDepth2()
        {
            ReloadSource();
            int iDepth = 2;

            DataReader.PositionOnElement("EMPTY1");
            CError.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
            return TEST_PASS;
        }

        [Variation("XmlReader Depth at Empty Tag with Attributes")]
        public int TestDepth3()
        {
            ReloadSource();
            int iDepth = 2;

            DataReader.PositionOnElement("ACT1");
            CError.Compare(DataReader.Depth, iDepth, "Element Depth should be " + (iDepth).ToString());

            while (DataReader.MoveToNextAttribute() == true)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.Depth, iDepth + 1, "Attr Depth should be " + (iDepth + 1).ToString());
            }
            return TEST_PASS;
        }

        [Variation("XmlReader Depth at Non Empty Tag with Text")]
        public int TestDepth4()
        {
            ReloadSource();
            int iDepth = 2;

            DataReader.PositionOnElement("NONEMPTY1");

            CError.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
            while (true == DataReader.Read())
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                if (DataReader.NodeType == XmlNodeType.Text)
                    CError.Compare(DataReader.Depth, iDepth + 1, "Depth should be " + (iDepth + 1).ToString());

                if (DataReader.Name == "NONEMPTY1" && (DataReader.NodeType == XmlNodeType.EndElement)) break;
            }
            CError.Compare(DataReader.Depth, iDepth, "Depth should be " + iDepth);
            return TEST_PASS;
        }

        [Variation("Depth on node from expanded entity")]
        public int TestDepth5()
        {
            string strxml = "<!DOCTYPE root[<!ELEMENT root ANY><!ENTITY MyEntity \"<E a='1'/>\nT2<E/>\">]><root>&MyEntity;</root>";

            if (IsXmlTextReader() || IsCoreReader())
                return TEST_SKIPPED;

            ReloadSource(new StringReader(strxml));
            while (DataReader.Read())
            {
                if (DataReader.NodeType == XmlNodeType.EntityReference)
                    DataReader.ResolveEntity();

                if (DataReader.Name == "E")
                    break;
            }

            int nDepth = 1;
            if (IsXmlNodeReader() || IsXmlValidatingReader())
                nDepth++; // they also see the entity reference node

            CError.Compare(DataReader.Depth, nDepth, "E");

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Text, String.Empty, "\nT2"), "nT2");
            CError.Compare(DataReader.Depth, nDepth, "T2");

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, "E", String.Empty), "nE2");
            CError.Compare(DataReader.Depth, nDepth, "E2");

            if (IsXmlNodeReader() || IsXmlValidatingReader())
            {
                DataReader.Read();
                CError.Compare(DataReader.VerifyNode(XmlNodeType.EndEntity, "MyEntity", String.Empty), "ee");
                CError.Compare(DataReader.Depth, 1, "eed");
            }

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.EndElement, "root", String.Empty), "nr");
            CError.Compare(DataReader.Depth, 0, "er");

            return TEST_PASS;
        }

        [Variation("Depth on node from expanded entity EntityHandling = ExpandEntities")]
        public int TestDepth6()
        {
            string strxml = "<!DOCTYPE root[<!ELEMENT root ANY><!ENTITY MyEntity \"<E a='1'/>\nT2<E/>\">]><root>&MyEntity;</root>";

            if (IsXmlTextReader() || IsCoreReader())
                return TEST_SKIPPED;

            ReloadSource(new StringReader(strxml));

            DataReader.PositionOnElement("E");

            CError.Compare(DataReader.Depth, 1, "E");

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Text, String.Empty, "\nT2"), "nT2");
            CError.Compare(DataReader.Depth, 1, "T2");

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Element, "E", String.Empty), "nE2");
            CError.Compare(DataReader.Depth, 1, "E2");

            DataReader.Read();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.EndElement, "root", String.Empty), "nr");
            CError.Compare(DataReader.Depth, 0, "er");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML Namespace
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCNamespace : TCXMLReaderBaseGeneral
    {
        private static string s_NONAMESPACE = "NONAMESPACE";

        [Variation("Namespace test within a scope (no nested element)", Pri = 0)]
        public int TestNamespace1()
        {
            ReloadSource();
            int i = 0;

            DataReader.PositionOnElement("NAMESPACE0");

            while (true == DataReader.Read())
            {
                if (DataReader.Name == "NAMESPACE1") break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString() + " Loop " + i); i++;

                if (DataReader.NodeType == XmlNodeType.Element)
                {
                    CError.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                    CError.Compare(DataReader.Name, "bar:check", "Compare Name");
                    CError.Compare(DataReader.LocalName, "check", "Compare LocalName");
                    CError.Compare(DataReader.Prefix, "bar", "Compare Prefix");
                }
            }
            return TEST_PASS;
        }

        [Variation("Namespace test within a scope (with nested element)", Pri = 0)]
        public int TestNamespace2()
        {
            ReloadSource();

            DataReader.PositionOnElement("NAMESPACE1");
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "NONAMESPACE") break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                if ((DataReader.NodeType == XmlNodeType.Element) && (DataReader.LocalName == "check"))
                {
                    CError.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                    CError.Compare(DataReader.Name, "bar:check", "Compare Name");
                    CError.Compare(DataReader.LocalName, "check", "Compare LocalName");
                    CError.Compare(DataReader.Prefix, "bar", "Compare Prefix");
                }
            }
            CError.Compare(DataReader.NamespaceURI, String.Empty, "Compare Namespace with String.Empty");

            return TEST_PASS;
        }

        [Variation("Namespace test immediately outside the Namespace scope")]
        public int TestNamespace3()
        {
            ReloadSource();

            DataReader.PositionOnElement(s_NONAMESPACE);
            CError.Compare(DataReader.NamespaceURI, String.Empty, "Compare Namespace with EmptyString");
            CError.Compare(DataReader.Name, s_NONAMESPACE, "Compare Name");
            CError.Compare(DataReader.LocalName, s_NONAMESPACE, "Compare LocalName");
            CError.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");

            return TEST_PASS;
        }

        [Variation("Namespace test Attribute should has no default namespace", Pri = 0)]
        public int TestNamespace4()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();

            DataReader.PositionOnElement("NONAMESPACE1");
            CError.Compare(DataReader.NamespaceURI, "1000", "Compare Namespace for Element");
            if (DataReader.MoveToFirstAttribute())
            {
                CError.Compare(DataReader.NamespaceURI, String.Empty, "Compare Namespace for Attr");
            }
            return TEST_PASS;
        }

        [Variation("Namespace test with multiple Namespace declaration", Pri = 0)]
        public int TestNamespace5()
        {
            ReloadSource();

            DataReader.PositionOnElement("NAMESPACE2");
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "NAMESPACE3") break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                if ((DataReader.NodeType == XmlNodeType.Element) && (DataReader.LocalName == "check"))
                {
                    CError.Compare(DataReader.NamespaceURI, "2", "Compare Namespace");
                    CError.Compare(DataReader.Name, "bar:check", "Compare Name");
                    CError.Compare(DataReader.LocalName, "check", "Compare LocalName");
                    CError.Compare(DataReader.Prefix, "bar", "Compare Prefix");
                }
            }
            return TEST_PASS;
        }

        [Variation("Namespace test with multiple Namespace declaration, including default namespace")]
        public int TestNamespace6()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();

            DataReader.PositionOnElement("NAMESPACE3");
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "NONAMESPACE") break;
                CError.WriteLine("N=" + DataReader.Name + " NS=" + DataReader.NamespaceURI + " V=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());

                if (DataReader.NodeType == XmlNodeType.Element)
                {
                    if (DataReader.LocalName == "check")
                    {
                        CError.WriteLine("Here1");
                        CError.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                        CError.Compare(DataReader.Name, "check", "Compare Name");
                        CError.Compare(DataReader.LocalName, "check", "Compare LocalName");
                        CError.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                    }
                    else if (DataReader.LocalName == "check1")
                    {
                        CError.WriteLine("Here2");
                        CError.Compare(DataReader.NamespaceURI, "1", "Compare Namespace");
                        CError.Compare(DataReader.Name, "check1", "Compare Name");
                        CError.Compare(DataReader.LocalName, "check1", "Compare LocalName");
                        CError.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                    }
                    else if (DataReader.LocalName == "check8")
                    {
                        CError.WriteLine("Here3");
                        CError.Compare(DataReader.NamespaceURI, "8", "Compare Namespace");
                        CError.Compare(DataReader.Name, "d:check8", "Compare Name");
                        CError.Compare(DataReader.LocalName, "check8", "Compare LocalName");
                        CError.Compare(DataReader.Prefix, "d", "Compare Prefix");
                    }
                    else if (DataReader.LocalName == "check100")
                    {
                        CError.WriteLine("Here4");
                        CError.Compare(DataReader.NamespaceURI, "100", "Compare Namespace");
                        CError.Compare(DataReader.Name, "check100", "Compare Name");
                        CError.Compare(DataReader.LocalName, "check100", "Compare LocalName");
                        CError.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                    }
                    else if (DataReader.LocalName == "check5")
                    {
                        CError.WriteLine("Here5");
                        CError.Compare(DataReader.NamespaceURI, "5", "Compare Namespace");
                        CError.Compare(DataReader.Name, "d:check5", "Compare Name");
                        CError.Compare(DataReader.LocalName, "check5", "Compare LocalName");
                        CError.Compare(DataReader.Prefix, "d", "Compare Prefix");
                    }
                    else if (DataReader.LocalName == "check14")
                    {
                        CError.WriteLine("Here6");
                        CError.Compare(DataReader.NamespaceURI, "14", "Compare Namespace");
                        CError.Compare(DataReader.Name, "check14", "Compare Name");
                        CError.Compare(DataReader.LocalName, "check14", "Compare LocalName");
                        CError.Compare(DataReader.Prefix, String.Empty, "Compare Prefix");
                    }
                    else if (DataReader.LocalName == "a13")
                    {
                        CError.WriteLine("Here7");
                        CError.Compare(DataReader.NamespaceURI, "1", "Compare Namespace1");
                        CError.Compare(DataReader.Name, "a13", "Compare Name1");
                        CError.Compare(DataReader.LocalName, "a13", "Compare LocalName1");
                        CError.Compare(DataReader.Prefix, String.Empty, "Compare Prefix1");
                        DataReader.MoveToFirstAttribute();
                        CError.Compare(DataReader.NamespaceURI, "13", "Compare Namespace2");
                        CError.Compare(DataReader.Name, "a:check", "Compare Name2");
                        CError.Compare(DataReader.LocalName, "check", "Compare LocalName2");
                        CError.Compare(DataReader.Prefix, "a", "Compare Prefix2");
                        CError.Compare(DataReader.Value, "Namespace=13", "Compare Name2");
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("Namespace URI for xml prefix", Pri = 0)]
        public int TestNamespace7()
        {
            string strxml = "<ROOT xml:space='preserve'/>";
            ReloadSource(new StringReader(strxml));

            DataReader.PositionOnElement("ROOT");
            DataReader.MoveToFirstAttribute();
            CError.Compare(DataReader.NamespaceURI, "http://www.w3.org/XML/1998/namespace", "xml");

            return TEST_PASS;
        }

        [Variation("XmlReader.ReadContentAs does not use the provided IXmlNamespaceResolver")]
        public int sqlbu435761()
        {
            string xml = @"<a>p:foo</a>";
            ReloadSource(new StringReader(xml));

            while (DataReader.Read())
            {
                if (DataReader.NodeType == XmlNodeType.Text)
                {
                    break;
                }
            }
            XmlNamespaceManager nsm = new XmlNamespaceManager(DataReader.NameTable);
            nsm.AddNamespace("p", "ns1");
            XmlQualifiedName qname = (XmlQualifiedName)DataReader.ReadContentAs(typeof(XmlQualifiedName), nsm);
            CError.Compare("ns1:foo", qname.ToString(), "Wrong namespace returned");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML LookupNamespace
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract class TCLookupNamespace : TCXMLReaderBaseGeneral
    {
        [Variation("LookupNamespace test within EmptyTag")]
        public int LookupNamespace1()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY_NAMESPACE");
            do
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.LookupNamespace("bar"), "1", "Compare LookupNamespace");
            } while (DataReader.MoveToNextAttribute() == true);
            return TEST_PASS;
        }

        [Variation("LookupNamespace test with Default namespace within EmptyTag", Pri = 0)]
        public int LookupNamespace2()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY_NAMESPACE1");
            do
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.LookupNamespace(String.Empty), "14", "Compare LookupNamespace");
            } while (DataReader.MoveToNextAttribute() == true);
            return TEST_PASS;
        }

        [Variation("LookupNamespace test within a scope (no nested element)", Pri = 0)]
        public int LookupNamespace3()
        {
            ReloadSource();
            int i = 0;
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "NAMESPACE0") break;
                CError.Compare(DataReader.LookupNamespace("bar"), null, "Compare LookupNamespace");
            }

            while (true == DataReader.Read())
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString() + " Loop " + i); i++;
                CError.Compare(DataReader.LookupNamespace("bar"), "1", "Compare LookupNamespace");
                if (DataReader.Name == "NAMESPACE0" && DataReader.NodeType == XmlNodeType.EndElement) break;
            }
            return TEST_PASS;
        }

        [Variation("LookupNamespace test within a scope (with nested element)", Pri = 0)]
        public int LookupNamespace4()
        {
            ReloadSource();
            DataReader.PositionOnElement("NAMESPACE1");
            while (true == DataReader.Read())
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.LookupNamespace("bar"), "1", "Compare LookupNamespace");
                if (DataReader.Name == "NAMESPACE1" && DataReader.NodeType == XmlNodeType.EndElement)
                {
                    DataReader.Read();
                    break;
                }
            }
            CError.Compare(DataReader.LookupNamespace("bar"), null, "Compare LookupNamespace with String.Empty");

            return TEST_PASS;
        }

        [Variation("LookupNamespace test immediately outside the Namespace scope")]
        public int LookupNamespace5()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONAMESPACE");
            CError.Compare(DataReader.LookupNamespace("bar"), null, "Compare LookupNamespace with null");

            return TEST_PASS;
        }

        [Variation("LookupNamespace test with multiple Namespace declaration", Pri = 0)]
        public int LookupNamespace6()
        {
            ReloadSource();
            DataReader.PositionOnElement("NAMESPACE2");

            string strValue = "1";
            while (true == DataReader.Read())
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());

                if (DataReader.Name == "c")
                {
                    strValue = "2";
                    CError.Compare(DataReader.LookupNamespace("bar"), strValue, "Compare LookupNamespace-a");
                    if (DataReader.NodeType == XmlNodeType.EndElement)
                        strValue = "1";
                }
                else
                    CError.Compare(DataReader.LookupNamespace("bar"), strValue, "Compare LookupNamespace-a");

                if (DataReader.Name == "NAMESPACE2" && DataReader.NodeType == XmlNodeType.EndElement)
                {
                    CError.Compare(DataReader.LookupNamespace("bar"), strValue, "Compare LookupNamespace-a");
                    DataReader.Read();
                    break;
                }
            }
            return TEST_PASS;
        }

        private void CompareAllNS(string strDef, string strA, string strB, string strC, string strD, string strE, string strF, string strG, string strH)
        {
            CError.Compare(DataReader.LookupNamespace(String.Empty), strDef, "Compare LookupNamespace-default");
            CError.Compare(DataReader.LookupNamespace("a"), strA, "Compare LookupNamespace-a");
            CError.Compare(DataReader.LookupNamespace("b"), strB, "Compare LookupNamespace-b");
            CError.Compare(DataReader.LookupNamespace("c"), strC, "Compare LookupNamespace-c");
            CError.Compare(DataReader.LookupNamespace("d"), strD, "Compare LookupNamespace-d");
            CError.Compare(DataReader.LookupNamespace("e"), strE, "Compare LookupNamespace-e");
            CError.Compare(DataReader.LookupNamespace("f"), strF, "Compare LookupNamespace-f");
            CError.Compare(DataReader.LookupNamespace("g"), strG, "Compare LookupNamespace-g");
            CError.Compare(DataReader.LookupNamespace("h"), strH, "Compare LookupNamespace-h");
        }

        [Variation("Namespace test with multiple Namespace declaration, including default namespace")]
        public int LookupNamespace7()
        {
            ReloadSource();
            DataReader.PositionOnElement("NAMESPACE3");

            string strDef = "1";
            string strA = null;
            string strB = null;
            string strC = null;
            string strD = null;
            string strE = null;
            string strF = null;
            string strG = null;
            string strH = null;
            while (true == DataReader.Read())
            {
                CError.WriteLine("N=" + DataReader.Name + " NS=" + DataReader.NamespaceURI + " V=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                if (DataReader.Name == "a")
                {
                    strA = "2";
                    strB = "3";
                    strC = "4";
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    if (DataReader.NodeType == XmlNodeType.EndElement)
                    {
                        strA = null;
                        strB = null;
                        strC = null;
                    }
                }
                else if (DataReader.Name == "b")
                {
                    strD = "5";
                    strE = "6";
                    strF = "7";
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    if (DataReader.NodeType == XmlNodeType.EndElement)
                    {
                        strD = null;
                        strE = null;
                        strF = null;
                    }
                }
                else if (DataReader.Name == "c")
                {
                    strD = "8";
                    strE = "9";
                    strF = "10";
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    if (DataReader.NodeType == XmlNodeType.EndElement)
                    {
                        strD = "5";
                        strE = "6";
                        strF = "7";
                    }
                }
                else if (DataReader.Name == "d")
                {
                    strG = "11";
                    strH = "12";
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    if (DataReader.NodeType == XmlNodeType.EndElement)
                    {
                        strG = null;
                        strH = null;
                    }
                }
                else if (DataReader.Name == "testns")
                {
                    strDef = "100";
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    if (DataReader.NodeType == XmlNodeType.EndElement)
                    {
                        strDef = "1";
                    }
                }
                else if (DataReader.Name == "a13")
                {
                    strA = "13";
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    do
                    {
                        CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                        CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    } while (DataReader.MoveToNextAttribute() == true);
                    strA = null;
                }
                else if (DataReader.Name == "check14")
                {
                    strDef = "14";
                    CError.WriteLine(strA + " XXX N=" + DataReader.Name + " NS=" + DataReader.NamespaceURI + " V=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    if (DataReader.NodeType == XmlNodeType.EndElement)
                    {
                        strDef = "1";
                    }
                }
                else
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);

                if (DataReader.Name == "NAMESPACE3" && DataReader.NodeType == XmlNodeType.EndElement)
                {
                    CompareAllNS(strDef, strA, strB, strC, strD, strE, strF, strG, strH);
                    DataReader.Read();
                    break;
                }
            }
            return TEST_PASS;
        }

        [Variation("LookupNamespace on whitespace node PreserveWhitespaces = true", Pri = 0)]
        public int LookupNamespace8()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;
            string strxml = "<ROOT xmlns:p='1'>\n<E1/></ROOT>";
            ReloadSource(new StringReader(strxml));
            PositionOnNodeType(XmlNodeType.Whitespace);

            string ns = DataReader.LookupNamespace("p");
            CError.Compare(ns, "1", "ln");

            return TEST_PASS;
        }

        [Variation("Different prefix on inner element for the same namespace", Pri = 0)]
        public int LookupNamespace9()
        {
            string ns = "http://www.w3.org/1999/XMLSchema";
            string filename = Path.Combine(TestData, "Common", "bug_57723.xml");

            ReloadSource(filename);

            DataReader.PositionOnElement("element");
            CError.Compare(DataReader.LookupNamespace("q1"), ns, "q11");
            CError.Compare(DataReader.LookupNamespace("q2"), null, "q21");

            DataReader.Read();
            DataReader.PositionOnElement("element");
            CError.Compare(DataReader.LookupNamespace("q1"), ns, "q12");
            CError.Compare(DataReader.LookupNamespace("q2"), ns, "q22");

            return TEST_PASS;
        }

        [Variation("LookupNamespace when Namespaces = false", Pri = 0)]
        public int LookupNamespace10()
        {
            if (!(IsXmlTextReader() || IsXmlValidatingReader()))
            {
                return TEST_SKIPPED;
            }

            string strxml = "<ROOT xmlns:p='1'>\n<E1/></ROOT>";
            ReloadSourceStr(strxml);
            DataReader.PositionOnElement("ROOT");
            CError.Compare(DataReader.LookupNamespace("p"), null, "ln ROOT");
            DataReader.PositionOnElement("E1");
            CError.Compare(DataReader.LookupNamespace("p"), null, "ln E1");
            DataReader.Read();
            CError.Compare(DataReader.LookupNamespace("p"), null, "ln /ROOT");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML HasValue
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCHasValue : TCXMLReaderBaseGeneral
    {
        [Variation("HasValue On None")]
        public int TestHasValueNodeType_None()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.None) == TEST_PASS)
            {
                bool b = DataReader.HasValue;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue returns True");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On Element", Pri = 0)]
        public int TestHasValueNodeType_Element()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.Element) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.HasValue;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue returns True");
            }
            return TEST_FAIL;
        }

        [Variation("Get node with a scalar value, verify the value with valid ReadString")]
        public int TestHasValue1()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONEMPTY1");

            DataReader.Read();
            CError.Compare(DataReader.HasValue, true, "HasValue test");
            return TEST_PASS;
        }

        [Variation("HasValue On Attribute", Pri = 0)]
        public int TestHasValueNodeType_Attribute()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.Attribute) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.HasValue;
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue for Attribute returns false");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On Text", Pri = 0)]
        public int TestHasValueNodeType_Text()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.Text) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.HasValue;
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue for Text returns false");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On CDATA", Pri = 0)]
        public int TestHasValueNodeType_CDATA()
        {
            ReloadSource();

            // No CDATA for Xslt
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                while (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
                    return TEST_FAIL;
                return TEST_PASS;
            }

            while (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.HasValue;
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue for CDATA returns false");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On ProcessingInstruction", Pri = 0)]
        public int TestHasValueNodeType_ProcessingInstruction()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.ProcessingInstruction) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.HasValue;
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue for PI returns false");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On Comment", Pri = 0)]
        public int TestHasValueNodeType_Comment()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.Comment) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.HasValue;
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue for Comment returns false");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On Whitespace PreserveWhitespaces = true", Pri = 0)]
        public int TestHasValueNodeType_Whitespace()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;
            ReloadSource();
            while (FindNodeType(XmlNodeType.Whitespace) == TEST_PASS)
            {
                bool b = DataReader.HasValue;
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue returns False");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On EndElement")]
        public int TestHasValueNodeType_EndElement()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.EndElement) == TEST_PASS)
            {
                bool b = DataReader.HasValue;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue returns True");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On XmlDeclaration", Pri = 0)]
        public int TestHasValueNodeType_XmlDeclaration()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) == TEST_PASS)
            {
                bool b = DataReader.HasValue;
                if (b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue returns False");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On EntityReference")]
        public int TestHasValueNodeType_EntityReference()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.EntityReference) == TEST_PASS)
            {
                bool b = DataReader.HasValue;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue returns True");
            }
            return TEST_FAIL;
        }

        [Variation("HasValue On EndEntity")]
        public int TestHasValueNodeType_EndEntity()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            ReloadSource();
            while (FindNodeType(XmlNodeType.EndEntity) == TEST_PASS)
            {
                bool b = DataReader.HasValue;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "HasValue returns True");
            }
            return TEST_FAIL;
        }

        [Variation("PI Value containing surrogates", Pri = 0)]
        public int v13()
        {
            if (IsBinaryReader())
                return TEST_SKIPPED;

            string strxml = "<root><?target \uD800\uDC00\uDBFF\uDFFF?></root>"; //Fail
            ReloadSourceStr(strxml);

            DataReader.Read();
            DataReader.Read();
            CError.Compare(DataReader.NodeType, XmlNodeType.ProcessingInstruction, "nt");
            CError.Compare(DataReader.Value, "\uD800\uDC00\uDBFF\uDFFF", "piv");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML IsEmptyElement
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCIsEmptyElement : TCXMLReaderBaseGeneral
    {
        [Variation("Set and Get an element that ends with />", Pri = 0)]
        public int TestEmpty1()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY1");

            bool b = DataReader.IsEmptyElement;
            if (b)
                return TEST_PASS;
            else
                throw new CTestException(CTestBase.TEST_FAIL, "DataReader is NOT_EMPTY, supposed to be EMPTY");
        }

        [Variation("Set and Get an element with an attribute that ends with />", Pri = 0)]
        public int TestEmpty2()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY2");

            bool b = DataReader.IsEmptyElement;
            if (b)
                return TEST_PASS;
            else
                throw new CTestException(CTestBase.TEST_FAIL, "DataReader is NOT_EMPTY, supposed to be EMPTY");
        }

        [Variation("Set and Get an element that ends without />", Pri = 0)]
        public int TestEmpty3()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONEMPTY1");

            bool b = DataReader.IsEmptyElement;
            if (!b)
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Set and Get an element with an attribute that ends with />", Pri = 0)]
        public int TestEmpty4()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONEMPTY2");
            bool b = DataReader.IsEmptyElement;
            if (!b)
                return TEST_PASS;
            else
                throw new CTestException(CTestBase.TEST_FAIL, "DataReader is EMPTY, supposed to be NOT_EMPTY");
        }

        [Variation("IsEmptyElement On Element", Pri = 0)]
        public int TestEmptyNodeType_Element()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.Element) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On None")]
        public int TestEmptyNodeType_None()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.None) == TEST_PASS)
            {
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On Text")]
        public int TestEmptyNodeType_Text()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.Text) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On CDATA")]
        public int TestEmptyNodeType_CDATA()
        {
            ReloadSource();

            // No CDATA for Xslt
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                while (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
                    return TEST_FAIL;
                return TEST_PASS;
            }

            while (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On ProcessingInstruction")]
        public int TestEmptyNodeType_ProcessingInstruction()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.ProcessingInstruction) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On Comment")]
        public int TestEmptyNodeType_Comment()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.Comment) == TEST_PASS)
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value);
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On DocumentType")]
        public int TestEmptyNodeType_DocumentType()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.DocumentType) == TEST_PASS)
            {
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On Whitespace PreserveWhitespaces = true")]
        public int TestEmptyNodeType_Whitespace()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;
            ReloadSource();
            while (FindNodeType(XmlNodeType.Whitespace) == TEST_PASS)
            {
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On EndElement")]
        public int TestEmptyNodeType_EndElement()
        {
            ReloadSource();
            while (FindNodeType(XmlNodeType.EndElement) == TEST_PASS)
            {
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On XmlDeclaration")]
        public int TestEmptyNodeType_XmlDeclaration()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) == TEST_PASS)
            {
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }

        [Variation("IsEmptyElement On EntityReference")]
        public int TestEmptyNodeType_EntityReference()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            ReloadSource();
            while (FindNodeType(XmlNodeType.EntityReference) == TEST_PASS)
            {
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            throw new CTestException(CTestBase.TEST_FAIL, "Entity Reference XMLNodeType Not Found");
        }

        [Variation("IsEmptyElement On EndEntity")]
        public int TestEmptyNodeType_EndEntity()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            ReloadSource();
            while (FindNodeType(XmlNodeType.EndEntity) == TEST_PASS)
            {
                bool b = DataReader.IsEmptyElement;
                if (!b)
                    return TEST_PASS;
                else
                    throw new CTestException(CTestBase.TEST_FAIL, "IsEmptyElement returns True");
            }
            return TEST_FAIL;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML XmlSpace
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCXmlSpace : TCXMLReaderBaseGeneral
    {
        [Variation("XmlSpace test within EmptyTag")]
        public int TestXmlSpace1()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY_XMLSPACE");

            do
            {
                CError.WriteLine("N=" + DataReader.Name + " V=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
            } while (DataReader.MoveToNextAttribute() == true);
            return TEST_PASS;
        }

        [Variation("Xmlspace test within a scope (no nested element)", Pri = 0)]
        public int TestXmlSpace2()
        {
            ReloadSource();
            int i = 0;
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLSPACE1") break;
                CError.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace with None");
            }

            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLSPACE1" && (DataReader.NodeType == XmlNodeType.EndElement)) break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString() + " Loop " + i); i++;
                CError.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
            }

            return TEST_PASS;
        }

        [Variation("Xmlspace test within a scope (with nested element)", Pri = 0)]
        public int TestXmlSpace3()
        {
            ReloadSource();
            DataReader.PositionOnElement("XMLSPACE2");
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "NOSPACE") break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlSpace, XmlSpace.Preserve, "Compare XmlSpace with Preserve");
            }
            CError.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace outside scope");

            return TEST_PASS;
        }

        [Variation("Xmlspace test immediately outside the XmlSpace scope")]
        public int TestXmlSpace4()
        {
            ReloadSource();
            DataReader.PositionOnElement("NOSPACE");
            CError.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace with None");

            return TEST_PASS;
        }

        [Variation("XmlSpace test with multiple XmlSpace declaration")]
        public int TestXmlSpace5()
        {
            ReloadSource();
            DataReader.PositionOnElement("XMLSPACE2A");

            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLSPACE3") break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
            }

            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLSPACE4")
                {
                    while (true == DataReader.Read())
                    {
                        CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                        CError.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
                        if (DataReader.Name == "XMLSPACE4" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            DataReader.Read();
                            break;
                        }
                    }
                }

                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlSpace, XmlSpace.Preserve, "Compare XmlSpace with Preserve");

                if (DataReader.Name == "XMLSPACE3" && DataReader.NodeType == XmlNodeType.EndElement)
                {
                    DataReader.Read();
                    break;
                }
            }

            do
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlSpace, XmlSpace.Default, "Compare XmlSpace with Default");
                if (DataReader.Name == "XMLSPACE2A" && DataReader.NodeType == XmlNodeType.EndElement)
                {
                    DataReader.Read();
                    break;
                }
            } while (true == DataReader.Read());

            CError.Compare(DataReader.XmlSpace, XmlSpace.None, "Compare XmlSpace outside scope");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML XmlLang
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCXmlLang : TCXMLReaderBaseGeneral
    {
        [Variation("XmlLang test within EmptyTag")]
        public int TestXmlLang1()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY_XMLLANG");
            do
            {
                CError.WriteLine("N=" + DataReader.Name + " V=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with en-US");
            } while (DataReader.MoveToNextAttribute() == true);
            return TEST_PASS;
        }

        [Variation("XmlLang test within a scope (no nested element)", Pri = 0)]
        public int TestXmlLang2()
        {
            ReloadSource();
            int i = 0;
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLLANG0") break;
                CError.Compare(DataReader.XmlLang, String.Empty, "Compare XmlLang with String.Empty");
            }

            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLLANG0" && (DataReader.NodeType == XmlNodeType.EndElement)) break;

                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString() + " Loop " + i); i++;

                if (DataReader.NodeType == XmlNodeType.EntityReference)
                {
                    CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with EntityRef");

                    if (DataReader.CanResolveEntity)
                    {
                        DataReader.ResolveEntity();
                        CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang after ResolveEntity");
                        while (DataReader.Read() && DataReader.NodeType != XmlNodeType.EndEntity)
                        {
                            CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang While Read ");
                        }
                        if (DataReader.NodeType == XmlNodeType.EndEntity)
                        {
                            CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang at EndEntity ");
                        }
                    }
                }
                else
                    CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with Preserve");
            }
            return TEST_PASS;
        }

        [Variation("XmlLang test within a scope (with nested element)", Pri = 0)]
        public int TestXmlLang3()
        {
            ReloadSource();
            DataReader.PositionOnElement("XMLLANG1");

            while (true == DataReader.Read())
            {
                if (DataReader.Name == "NOXMLLANG") break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlLang, "en-GB", "Compare XmlLang with en-GB");
            }

            return TEST_PASS;
        }

        [Variation("XmlLang test immediately outside the XmlLang scope")]
        public int TestXmlLang4()
        {
            ReloadSource();
            DataReader.PositionOnElement("NOXMLLANG");
            CError.Compare(DataReader.XmlLang, String.Empty, "Compare XmlLang with EmptyString");

            return TEST_PASS;
        }

        [Variation("XmlLang test with multiple XmlLang declaration")]
        public int TestXmlLang5()
        {
            ReloadSource();
            DataReader.PositionOnElement("XMLLANG2");
            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLLANG1") break;
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with Preserve");
            }

            while (true == DataReader.Read())
            {
                if (DataReader.Name == "XMLLANG0")
                {
                    while (true == DataReader.Read())
                    {
                        CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                        CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with en-US");
                        if (DataReader.Name == "XMLLANG0" && DataReader.NodeType == XmlNodeType.EndElement)
                        {
                            DataReader.Read();
                            break;
                        }
                    }
                }

                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlLang, "en-GB", "Compare XmlLang with en_GB");

                if (DataReader.Name == "XMLLANG1" && DataReader.NodeType == XmlNodeType.EndElement)
                {
                    DataReader.Read();
                    break;
                }
            }

            do
            {
                CError.WriteLine("Name=" + DataReader.Name + " Value=" + DataReader.Value + " NodeType=" + (DataReader.NodeType).ToString());
                CError.Compare(DataReader.XmlLang, "en-US", "Compare XmlLang with en-US");
                if (DataReader.Name == "XMLLANG2" && DataReader.NodeType == XmlNodeType.EndElement)
                {
                    DataReader.Read();
                    break;
                }
            } while (true == DataReader.Read());

            return TEST_PASS;
        }

        // XML 1.0 SE
        [Variation("XmlLang valid values", Pri = 0)]
        public int TestXmlLang6()
        {
            const string ST_VALIDXMLLANG = "VALIDXMLLANG";
            string[] aValidLang = { "a", "", "ab-cd-", "a b-cd" };

            ReloadSource();

            for (int i = 0; i < aValidLang.Length; i++)
            {
                string strelem = ST_VALIDXMLLANG + i;
                DataReader.PositionOnElement(strelem);
                CError.Compare(DataReader.XmlLang, aValidLang[i], "XmlLang");
            }

            return TEST_PASS;
        }

        // XML 1.0 SE
        [Variation("More XmlLang valid values")]
        public int TestXmlTextReaderLang1()
        {
            string[] aValidLang = { "", "ab-cd-", "abcdefghi", "ab-cdefghijk", "a b-cd", "ab-c d" };

            for (int i = 0; i < aValidLang.Length; i++)
            {
                string strxml = String.Format("<ROOT xml:lang='{0}'/>", aValidLang[i]);

                ReloadSourceStr(strxml);

                while (DataReader.Read()) ;
            }

            return TEST_PASS;
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // TestCase TCXML Skip
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCSkip : TCXMLReaderBaseGeneral
    {
        public bool VerifySkipOnNodeType(XmlNodeType testNodeType)
        {
            bool bPassed = false;
            XmlNodeType actNodeType;
            String strActName;
            String strActValue;

            ReloadSource();
            PositionOnNodeType(testNodeType);
            DataReader.Read();
            actNodeType = DataReader.NodeType;
            strActName = DataReader.Name;
            strActValue = DataReader.Value;

            ReloadSource();
            PositionOnNodeType(testNodeType);
            DataReader.Skip();
            bPassed = DataReader.VerifyNode(actNodeType, strActName, strActValue);

            return bPassed;
        }

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////
        [Variation("Call Skip on empty element", Pri = 0)]
        public int TestSkip1()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("SKIP1");

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.Element, "AFTERSKIP1", String.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip on element", Pri = 0)]
        public int TestSkip2()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("SKIP2");

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.Element, "AFTERSKIP2", String.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip on element with content", Pri = 0)]
        public int TestSkip3()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("SKIP3");

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.Element, "AFTERSKIP3", String.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip on text node (leave node)", Pri = 0)]
        public int TestSkip4()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("SKIP3");
            DataReader.PositionOnElement("ELEM2");
            DataReader.Read();
            bPassed = (DataReader.NodeType == XmlNodeType.Text);

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.EndElement, "ELEM2", String.Empty) && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip in while read loop", Pri = 0)]
        public int skip307543()
        {
            string fileName = Path.Combine(TestData, "Common", "skip307543.xml");
            ReloadSource(fileName);
            while (DataReader.Read())
                DataReader.Skip();

            return TEST_PASS;
        }

        [Variation("Call Skip on text node with another element: <elem2>text<elem3></elem3></elem2>")]
        public int TestSkip5()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement("SKIP4");
            DataReader.PositionOnElement("ELEM2");
            DataReader.Read();
            bPassed = (DataReader.NodeType == XmlNodeType.Text);

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.Element, "ELEM3", String.Empty) && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip on attribute", Pri = 0)]
        public int TestSkip6()
        {
            bool bPassed = false;

            ReloadSource();
            DataReader.PositionOnElement(ST_ENTTEST_NAME);
            bPassed = DataReader.MoveToFirstAttribute();

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.Element, "ENTITY2", String.Empty) && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip on text node of attribute")]
        public int TestSkip7()
        {
            bool bPassed = false;

            ReloadSource();

            DataReader.PositionOnElement(ST_ENTTEST_NAME);
            bPassed = DataReader.MoveToFirstAttribute();
            bPassed = DataReader.ReadAttributeValue() && bPassed;
            bPassed = (DataReader.NodeType == XmlNodeType.Text) && bPassed;

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.Element, "ENTITY2", String.Empty) && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip on CDATA", Pri = 0)]
        public int TestSkip8()
        {
            ReloadSource();

            // No CDATA for Xslt
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                if (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
                    return TEST_FAIL;
                return TEST_PASS;
            }

            return BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.CDATA));
        }

        [Variation("Call Skip on Processing Instruction", Pri = 0)]
        public int TestSkip9()
        {
            return BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.ProcessingInstruction));
        }

        [Variation("Call Skip on Comment", Pri = 0)]
        public int TestSkip10()
        {
            return BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.Comment));
        }

        [Variation("Call Skip on Document Type")]
        public int TestSkip11()
        {
            if (IsXsltReader() || IsXPathNavigatorReader() || IsSubtreeReader())
                return TEST_SKIPPED;

            return BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.DocumentType));
        }

        [Variation("Call Skip on Whitespace", Pri = 0)]
        public int TestSkip12()
        {
            ReloadSource();

            // Do not want to run this test for the Xslt Reader
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                if (FindNodeType(XmlNodeType.Whitespace) == TEST_PASS)
                    return TEST_FAIL;
                return TEST_PASS;
            }

            return BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.Whitespace));
        }

        [Variation("Call Skip on EndElement", Pri = 0)]
        public int TestSkip13()
        {
            return BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.EndElement));
        }

        [Variation("Call Skip on root Element")]
        public int TestSkip14()
        {
            bool bPassed;

            ReloadSource();
            DataReader.PositionOnNodeType(XmlNodeType.Element);

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.None, String.Empty, String.Empty);

            return BoolToLTMResult(bPassed);
        }

        [Variation("Call Skip on Entity Reference", Pri = 0)]
        public int TestSkip15()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            return BoolToLTMResult(VerifySkipOnNodeType(XmlNodeType.EntityReference));
        }

        [Variation("Call Skip on general entity ref node of attribute")]
        public int TestTextSkip1()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;
            bool bPassed = false;

            ReloadSource();

            DataReader.PositionOnElement(ST_ENTTEST_NAME);
            bPassed = DataReader.MoveToFirstAttribute();
            while (DataReader.ReadAttributeValue() && DataReader.NodeType != XmlNodeType.EntityReference)
            {
                CError.WriteLine(DataReader.Value);
            }
            bPassed = DataReader.VerifyNode(XmlNodeType.EntityReference, "e1", String.Empty) && bPassed;

            DataReader.Skip();

            bPassed = DataReader.VerifyNode(XmlNodeType.Element, "ENTITY2", String.Empty) && bPassed;

            return BoolToLTMResult(bPassed);
        }

        [Variation("XmlTextReader ArgumentOutOfRangeException when handling ampersands")]
        public int XmlTextReaderDoesHandleAmpersands()
        {
            string xmlStr = @"<a>
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
&gt; 
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
    fffffffffffffffffffffffffffffffffffffff
&amp;
</a>
";
            ReloadSource(new StringReader(xmlStr));
            DataReader.PositionOnElement("a");
            DataReader.Skip();
            return TEST_PASS;
        }
    }

    /////////////////////////////////////////////////////////////////////////
    // TestCase BaseURI
    //
    /////////////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCBaseURI : TCXMLReaderBaseGeneral
    {
        public const String ST_TEST_ENT = "AllNodeTypes.ent";

        public const String ST_GEN_ENT_NAME = "ext3";
        public const String ST_GEN_ENT_VALUE = "blah";

        ////////////////////////////////////////////////////////////////
        // Variations
        ////////////////////////////////////////////////////////////////
        [Variation("BaseURI for element node", Pri = 0)]
        public int TestBaseURI1()
        {
            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.Element);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";
            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);

            if (IsXsltReader())
                bPassed = CError.Equals(DataReader.BaseURI, String.Empty, CurVariation.Desc);
            else
                bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for attribute node", Pri = 0)]
        public int TestBaseURI2()
        {
            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.Attribute);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);

            if (IsXsltReader())
                bPassed = CError.Equals(DataReader.BaseURI, String.Empty, CurVariation.Desc);
            else
                bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for text node", Pri = 0)]
        public int TestBaseURI3()
        {
            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.Text);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);

            if (IsXsltReader())
                bPassed = CError.Equals(DataReader.BaseURI, String.Empty, CurVariation.Desc);
            else
                bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for CDATA node")]
        public int TestBaseURI4()
        {
            ReloadSource();

            // No CDATA for Xslt
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                while (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
                    return TEST_FAIL;
                return TEST_PASS;
            }

            bool bPassed = false;
            string strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            PositionOnNodeType(XmlNodeType.CDATA);
            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);
            bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for PI node")]
        public int TestBaseURI6()
        {
            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.ProcessingInstruction);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);

            if (IsXsltReader())
                bPassed = CError.Equals(DataReader.BaseURI, String.Empty, CurVariation.Desc);
            else
                bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for Comment node")]
        public int TestBaseURI7()
        {
            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.Comment);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);

            if (IsXsltReader())
                bPassed = CError.Equals(DataReader.BaseURI, String.Empty, CurVariation.Desc);
            else
                bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for DTD node")]
        public int TestBaseURI8()
        {
            bool bPassed = false;
            String strExpBaseURI;

            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.DocumentType);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);

            if (IsXsltReader() || IsXPathNavigatorReader())
                bPassed = CError.Equals(DataReader.BaseURI, String.Empty, CurVariation.Desc);
            else
                bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for Whitespace node PreserveWhitespaces = true")]
        public int TestBaseURI9()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            bool bPassed = false;
            string strExpBaseURI = String.Empty;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.Whitespace);

            string tmp = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                tmp = tmp + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + tmp);
            strExpBaseURI = ExpBaseURI.ToString();

            bPassed = CError.Equals(DataReader.BaseURI, strExpBaseURI, CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for EndElement node")]
        public int TestBaseURI10()
        {
            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EndElement);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);

            if (IsXsltReader())
                bPassed = CError.Equals(DataReader.BaseURI, String.Empty, CurVariation.Desc);
            else
                bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for EntityReference node", Pri = 0)]
        public int TestTextBaseURI1()
        {
            if (IsXmlNodeReaderDataDoc() || IsXsltReader() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EntityReference);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);
            bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for EndEntity node")]
        public int TestTextReaderBaseURI2()
        {
            if (IsXmlTextReader() || IsXmlNodeReaderDataDoc() || IsXsltReader() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            bool bPassed = false;
            String strExpBaseURI;

            ReloadSource();

            PositionOnNodeType(XmlNodeType.EndEntity);

            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);
            CError.WriteLine("EndEntity " + DataReader.BaseURI);
            bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), CurVariation.Desc);

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for external General Entity")]
        public int TestTextReaderBaseURI4()
        {
            bool bPassed = false;
            String strExpBaseURI;

            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            DataReader.PositionOnElement("ENTITY5");

            DataReader.Read();
            strExpBaseURI = "Environment.CurrentDirectory" + "\\" + GetTestFileName(EREADER_TYPE.GENERIC);
            if (IsBinaryReader())
                strExpBaseURI = strExpBaseURI + ".bin";

            Uri ExpBaseURI = new Uri("file:///" + strExpBaseURI);
            bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), "Before ResolveEntity");

            if (IsXmlTextReader() || IsXmlNodeReader() || IsXmlValidatingReader())
            {
                bPassed = DataReader.VerifyNode(XmlNodeType.EntityReference, ST_GEN_ENT_NAME, String.Empty);

                if (DataReader.CanResolveEntity)
                {
                    DataReader.ResolveEntity();
                    DataReader.Read();
                    CError.WriteLine("HeRE" + DataReader.Value);
                    bPassed = DataReader.VerifyNode(XmlNodeType.Text, String.Empty, ST_GEN_ENT_VALUE) && bPassed;
                }
            }
            else
                bPassed = DataReader.VerifyNode(XmlNodeType.Text, String.Empty, ST_GEN_ENT_VALUE) && bPassed;

            bPassed = CError.Equals(DataReader.BaseURI, ExpBaseURI.ToString(), "After ResolveEntity");

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for entity defined in nested parameter entity")]
        public int TestTextReaderBaseURI5()
        {
            bool bPassed = false;

            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            string filename = Path.Combine(TestData, "Common", "bug_62426.xml");
            if (IsBinaryReader())
                filename = Path.GetFileName(filename) + ".bin";

            Uri uri = new Uri("file:" + filename);

            ReloadSource(filename);

            DataReader.PositionOnElement("root");

            DataReader.Read();
            bPassed = CError.Equals(DataReader.BaseURI.Contains("Common/bug_62426.xml"), true, "Before ResolveEntity");

            if (IsXmlTextReader() || IsXmlNodeReader() || IsXmlValidatingReader())
            {
                bPassed = DataReader.VerifyNode(XmlNodeType.EntityReference, "bug62426", String.Empty);

                if (DataReader.CanResolveEntity)
                {
                    DataReader.ResolveEntity();
                    DataReader.Read();
                    CError.WriteLine("HeRE" + DataReader.Value);
                    bPassed = DataReader.VerifyNode(XmlNodeType.Text, String.Empty, "bug62426") && bPassed;
                }
            }
            else
                bPassed = DataReader.VerifyNode(XmlNodeType.Text, String.Empty, "bug62426") && bPassed;

            bPassed = CError.Compare(DataReader.BaseURI.Contains("Common/bug_62426.xml"), "After ResolveEntity");

            return BoolToLTMResult(bPassed);
        }

        [Variation("BaseURI for filename containing # and %23")]
        public int TestTextReaderBaseURI6()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            string filename = Path.Combine(TestData, "Common", "file#%23.xml");
            if (IsBinaryReader())
                filename = Path.GetFileName(filename) + ".bin";

            ReloadSource(filename);
            DataReader.Read();
            Uri uri = new Uri(filename);

            string expectedUri = uri.ToString();
            if (IsBinaryReader())
                expectedUri = expectedUri + ".bin";

            expectedUri = expectedUri.Replace(@"\", @"/");
            CError.WriteLine("Expected URI = " + expectedUri);
            CError.WriteLine("Actual URI =   " + DataReader.BaseURI);
            CError.Compare(DataReader.BaseURI.Contains("Common/file%23%2523.xml"), "baseUri");
            return TEST_PASS;
        }

        [Variation("BaseURI for external entity in external DTD")]
        public int TestTextReaderBaseURI7()
        {
            if (IsXsltReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            string filepath = Path.Combine(TestData, "Common");
            string filename = Path.Combine(filepath, "bug_60677.xml");
            if (IsBinaryReader())
                filename = Path.GetFileName(filename) + ".bin";

            string fileent = Path.Combine(filepath, "A", "B", "bug60677.ent");

            Uri uriFile = new Uri("file:" + filename);
            Uri uriEnt = new Uri("file:" + fileent);

            ReloadSource(filename);

            while (DataReader.Read())
            {
                if (DataReader.NodeType == XmlNodeType.EntityReference && DataReader.CanResolveEntity)
                {
                    DataReader.ResolveEntity();
                    DataReader.Read(); // Text
                    CError.Compare(DataReader.BaseURI, uriEnt.ToString(), "ent1 baseuri");
                    DataReader.Read(); // EndEntity
                    CError.Compare(DataReader.BaseURI, uriFile.ToString(), "ent2 baseuri");
                }
            }
            return TEST_PASS;
        }

        [Variation("BaseURI for external entity in external DTD with default entity handling")]
        public int TestTextReaderBaseURI107315()
        {
            if (!IsXmlValidatingReader())
                return TEST_SKIPPED;

            string filepath = Path.Combine(TestData, "Common");
            string filename = Path.Combine(filepath, "bug_60677.xml");
            if (IsBinaryReader())
                filename = Path.GetFileName(filename) + ".bin";

            string fileent = Path.Combine(filepath, "bug_60677.xml");

            Uri uriFile = new Uri("file:" + filename);
            Uri uriEnt = new Uri("file:" + fileent);

            ReloadSource(filename);

            while (DataReader.Read())
            {
                if (DataReader.NodeType == XmlNodeType.Text && DataReader.Value == "ABC")
                {
                    CError.Compare(DataReader.BaseURI, uriEnt.ToString(), "ent1 baseuri");
                }
            }

            return TEST_PASS;
        }

        [Variation("BaseUri for element expanded from nested parameter entities")]
        public int TestTextReaderBaseURI8()
        {
            if (IsXsltReader() || IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsCoreReader() || IsXPathNavigatorReader())
                return TEST_SKIPPED;

            string filename = Path.Combine(TestData, "Common", "Bug94358.xml");
            if (IsBinaryReader())
                filename = Path.GetFileName(filename) + ".bin";

            Uri uriFile = new Uri("file:" + filename);

            ReloadSource(filename);

            while (DataReader.Read())
            {
                if (DataReader.ReadState == ReadState.Interactive && DataReader.LocalName == "x")
                {
                    break;
                }
            }

            CError.WriteLine("Base URI of element <x> is " + DataReader.BaseURI.ToString());

            if (CError.Compare(DataReader.BaseURI.ToString(), uriFile.ToString(), "Expected BaseURI : " + filename))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }
    }
}
