// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public enum EINTEGRITY
    {
        //DataReader
        BEFORE_READ,
        AFTER_READ_FALSE,
        AFTER_RESETSTATE,

        //DataWriter
        BEFORE_WRITE,
        AFTER_WRITE_FALSE,
        AFTER_CLEAR,
        AFTER_FLUSH,

        // Both DataWriter and DataReader
        AFTER_CLOSE,
        CLOSE_IN_THE_MIDDLE,
    };

    ////////////////////////////////////////////////////////////////
    // TestCase TCXMLIntegrity
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public partial class TCXMLIntegrityBase : CDataReaderTestCase
    {
        private EINTEGRITY _eEIntegrity;

        public EINTEGRITY IntegrityVer
        {
            get { return _eEIntegrity; }
            set { _eEIntegrity = value; }
        }

        private static string s_ATTR = "Attr1";
        private static string s_NS = "Foo";
        private static string s_NAME = "PLAY0";

        public virtual void ReloadSource()
        {
            // load the reader
            string strFile = GetTestFileName(EREADER_TYPE.GENERIC);
            DataReader.Internal = XmlReader.Create(FilePathUtil.getStream(strFile));

            // position the reader at the expected place
            InitReaderPointer();
        }

        public int InitReaderPointer()
        {
            int iRetVal = TEST_PASS;

            CError.WriteLine("InitReaderPointer:{0}", GetDescription());
            if (GetDescription() == "BeforeRead")
            {
                IntegrityVer = EINTEGRITY.BEFORE_READ;
                CError.Compare(DataReader.ReadState, ReadState.Initial, "ReadState=Initial");
                CError.Compare(DataReader.EOF, false, "EOF==false");
            }

            else if (GetDescription() == "AfterReadIsFalse")
            {
                IntegrityVer = EINTEGRITY.AFTER_READ_FALSE;
                while (DataReader.Read()) ;
                CError.Compare(DataReader.ReadState, ReadState.EndOfFile, "ReadState=EOF");
                CError.Compare(DataReader.EOF, true, "EOF==true");
            }

            else if (GetDescription() == "AfterClose")
            {
                IntegrityVer = EINTEGRITY.AFTER_CLOSE;
                while (DataReader.Read()) ;
                DataReader.Close();
                CError.Compare(DataReader.ReadState, ReadState.Closed, "ReadState=Closed");
                CError.Compare(DataReader.EOF, false, "EOF==true");
            }

            else if (GetDescription() == "AfterCloseInTheMiddle")
            {
                IntegrityVer = EINTEGRITY.CLOSE_IN_THE_MIDDLE;
                for (int i = 0; i < 1; i++)
                {
                    if (false == DataReader.Read())
                        iRetVal = TEST_FAIL;
                    CError.Compare(DataReader.ReadState, ReadState.Interactive, "ReadState=Interactive");
                }
                DataReader.Close();
                CError.Compare(DataReader.ReadState, ReadState.Closed, "ReadState=Closed");
                CError.Compare(DataReader.EOF, false, "EOF==true");
                CError.WriteLine("EOF = " + DataReader.EOF);
            }
            else if (GetDescription() == "AfterResetState")
            {
                IntegrityVer = EINTEGRITY.AFTER_RESETSTATE;

                // position the reader somewhere in the middle of the file
                DataReader.PositionOnElement("elem1");
                DataReader.ResetState();

                CError.Compare(DataReader.ReadState, ReadState.Initial, "ReadState=Initial");
            }


            CError.WriteLine("ReadState = " + (DataReader.ReadState).ToString());
            return iRetVal;
        }

        [Variation("NodeType")]
        public int GetXmlReaderNodeType()
        {
            ReloadSource();
            CError.Compare(DataReader.NodeType, XmlNodeType.None, CurVariation.Desc);
            CError.Compare(DataReader.NodeType, XmlNodeType.None, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("Name")]
        public int GetXmlReaderName()
        {
            CError.Compare(DataReader.Name, String.Empty, CurVariation.Desc);
            CError.Compare(DataReader.Name, String.Empty, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("LocalName")]
        public int GetXmlReaderLocalName()
        {
            CError.Compare(DataReader.LocalName, String.Empty, CurVariation.Desc);
            CError.Compare(DataReader.LocalName, String.Empty, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("NamespaceURI")]
        public int Namespace()
        {
            CError.Compare(DataReader.NamespaceURI, String.Empty, CurVariation.Desc);
            CError.Compare(DataReader.NamespaceURI, String.Empty, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("Prefix")]
        public int Prefix()
        {
            CError.Compare(DataReader.Prefix, String.Empty, CurVariation.Desc);
            CError.Compare(DataReader.Prefix, String.Empty, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("HasValue")]
        public int HasValue()
        {
            CError.Compare(DataReader.HasValue, false, CurVariation.Desc);
            CError.Compare(DataReader.HasValue, false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("Value")]
        public int GetXmlReaderValue()
        {
            CError.Compare(DataReader.Value, String.Empty, CurVariation.Desc);
            CError.Compare(DataReader.Value, String.Empty, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("Depth")]
        public int GetDepth()
        {
            CError.Compare(DataReader.Depth, 0, CurVariation.Desc);
            CError.Compare(DataReader.Depth, 0, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("BaseURI")]
        public virtual int GetBaseURI()
        {
            return TEST_SKIPPED;
        }

        [Variation("IsEmptyElement")]
        public int IsEmptyElement()
        {
            CError.Compare(DataReader.IsEmptyElement, false, CurVariation.Desc);
            CError.Compare(DataReader.IsEmptyElement, false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("IsDefault")]
        public int IsDefault()
        {
            CError.Compare(DataReader.IsDefault, false, CurVariation.Desc);
            CError.Compare(DataReader.IsDefault, false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("XmlSpace")]
        public int GetXmlSpace()
        {
            CError.Compare(DataReader.XmlSpace, XmlSpace.None, CurVariation.Desc);
            CError.Compare(DataReader.XmlSpace, XmlSpace.None, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("XmlLang")]
        public int GetXmlLang()
        {
            CError.Compare(DataReader.XmlLang, String.Empty, CurVariation.Desc);
            CError.Compare(DataReader.XmlLang, String.Empty, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("AttributeCount")]
        public int AttributeCount()
        {
            CError.Compare(DataReader.AttributeCount, 0, CurVariation.Desc);
            CError.Compare(DataReader.AttributeCount, 0, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("HasAttributes")]
        public int HasAttribute()
        {
            CError.Compare(DataReader.HasAttributes, false, CurVariation.Desc);
            CError.Compare(DataReader.HasAttributes, false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("GetAttributes(name)")]
        public int GetAttributeName()
        {
            CError.Compare(DataReader.GetAttribute(s_ATTR), null, "Compare the GetAttribute");
            return TEST_PASS;
        }

        [Variation("GetAttribute(String.Empty)")]
        public int GetAttributeEmptyName()
        {
            CError.Compare(DataReader.GetAttribute(String.Empty), null, "Compare the GetAttribute");
            return TEST_PASS;
        }

        [Variation("GetAttribute(name,ns)")]
        public int GetAttributeNameNamespace()
        {
            CError.Compare(DataReader.GetAttribute(s_ATTR, s_NS), null, "Compare the GetAttribute");
            return TEST_PASS;
        }

        [Variation("GetAttribute(String.Empty, String.Empty)")]
        public int GetAttributeEmptyNameNamespace()
        {
            CError.Compare(DataReader.GetAttribute(String.Empty, String.Empty), null, "Compare the GetAttribute");
            return TEST_PASS;
        }

        [Variation("GetAttribute(i)")]
        public int GetAttributeOrdinal()
        {
            try
            {
                DataReader.GetAttribute(0);
            }
            catch (ArgumentOutOfRangeException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("this[i]")]
        public int HelperThisOrdinal()
        {
            ReloadSource();
            try
            {
                string str = DataReader[0];
            }
            catch (ArgumentOutOfRangeException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("this[name]")]
        public int HelperThisName()
        {
            ReloadSource();
            CError.Compare(DataReader[s_ATTR], null, "Compare the GetAttribute");
            return TEST_PASS;
        }

        [Variation("this[name,namespace]")]
        public int HelperThisNameNamespace()
        {
            ReloadSource();
            string str = DataReader[s_ATTR, s_NS];
            CError.Compare(DataReader[s_ATTR, s_NS], null, "Compare the GetAttribute");
            return TEST_PASS;
        }

        [Variation("MoveToAttribute(name)")]
        public int MoveToAttributeName()
        {
            ReloadSource();
            CError.Compare(DataReader.MoveToAttribute(s_ATTR), false, CurVariation.Desc);
            CError.Compare(DataReader.MoveToAttribute(s_ATTR), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToAttributeNameNamespace(name,ns)")]
        public int MoveToAttributeNameNamespace()
        {
            ReloadSource();
            CError.Compare(DataReader.MoveToAttribute(s_ATTR, s_NS), false, CurVariation.Desc);
            CError.Compare(DataReader.MoveToAttribute(s_ATTR, s_NS), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToAttribute(i)")]
        public int MoveToAttributeOrdinal()
        {
            ReloadSource();
            try
            {
                DataReader.MoveToAttribute(0);
            }
            catch (ArgumentOutOfRangeException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("MoveToFirstAttribute()")]
        public int MoveToFirstAttribute()
        {
            ReloadSource();
            CError.Compare(DataReader.MoveToFirstAttribute(), false, CurVariation.Desc);
            CError.Compare(DataReader.MoveToFirstAttribute(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToNextAttribute()")]
        public int MoveToNextAttribute()
        {
            ReloadSource();
            CError.Compare(DataReader.MoveToNextAttribute(), false, CurVariation.Desc);
            CError.Compare(DataReader.MoveToNextAttribute(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToElement()")]
        public int MoveToElement()
        {
            ReloadSource();
            CError.Compare(DataReader.MoveToElement(), false, CurVariation.Desc);
            CError.Compare(DataReader.MoveToElement(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("Read")]
        public int ReadTestAfterClose()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                    IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interesting for test
                return TEST_SKIPPED;
            }
            else
            {
                CError.Compare(DataReader.Read(), false, CurVariation.Desc);
                CError.Compare(DataReader.Read(), false, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("GetEOF")]
        public int GetEOF()
        {
            ReloadSource();
            if ((IntegrityVer == EINTEGRITY.AFTER_READ_FALSE))
            {
                CError.Compare(DataReader.EOF, true, CurVariation.Desc);
                CError.Compare(DataReader.EOF, true, CurVariation.Desc);
            }
            else
            {
                CError.Compare(DataReader.EOF, false, CurVariation.Desc);
                CError.Compare(DataReader.EOF, false, CurVariation.Desc);
            }

            return TEST_PASS;
        }

        [Variation("GetReadState")]
        public int GetReadState()
        {
            ReloadSource();
            ReadState iState = ReadState.Initial;

            // EndOfFile State
            if ((IntegrityVer == EINTEGRITY.AFTER_READ_FALSE))
            {
                iState = ReadState.EndOfFile;
            }

            // Closed State 
            if ((IntegrityVer == EINTEGRITY.AFTER_CLOSE) || (IntegrityVer == EINTEGRITY.CLOSE_IN_THE_MIDDLE))
            {
                iState = ReadState.Closed;
            }
            CError.Compare(DataReader.ReadState, iState, CurVariation.Desc);
            CError.Compare(DataReader.ReadState, iState, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("Skip")]
        public int XMLSkip()
        {
            ReloadSource();
            DataReader.Skip();
            DataReader.Skip();
            return TEST_PASS;
        }

        [Variation("NameTable")]
        public int TestNameTable()
        {
            ReloadSource();
            CError.Compare(DataReader.NameTable != null, "nt");
            return TEST_PASS;
        }

        [Variation("ReadInnerXml")]
        public int ReadInnerXmlTestAfterClose()
        {
            ReloadSource();
            XmlNodeType nt = DataReader.NodeType;
            string name = DataReader.Name;
            string value = DataReader.Value;

            CError.Compare(DataReader.ReadInnerXml(), String.Empty, CurVariation.Desc);

            CError.Compare(DataReader.VerifyNode(nt, name, value), "vn");
            return TEST_PASS;
        }

        [Variation("ReadOuterXml")]
        public int TestReadOuterXml()
        {
            ReloadSource();
            XmlNodeType nt = DataReader.NodeType;
            string name = DataReader.Name;
            string value = DataReader.Value;
            CError.Compare(DataReader.ReadOuterXml(), String.Empty, CurVariation.Desc);

            CError.Compare(DataReader.VerifyNode(nt, name, value), "vn");

            return TEST_PASS;
        }

        [Variation("MoveToContent")]
        public int TestMoveToContent()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                CError.WriteLine("NodeType  " + DataReader.NodeType);
                CError.Compare(DataReader.MoveToContent(), XmlNodeType.None, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("IsStartElement")]
        public int TestIsStartElement()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                CError.Compare(DataReader.IsStartElement(), false, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("IsStartElement(name)")]
        public int TestIsStartElementName()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                CError.Compare(DataReader.IsStartElement(s_NAME), false, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("IsStartElement(String.Empty)")]
        public int TestIsStartElementName2()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                CError.Compare(DataReader.IsStartElement(String.Empty), false, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("IsStartElement(name, ns)")]
        public int TestIsStartElementNameNs()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                CError.Compare(DataReader.IsStartElement(s_NAME, s_NS), false, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("IsStartElement(String.Empty,String.Empty)")]
        public int TestIsStartElementNameNs2()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                CError.Compare(DataReader.IsStartElement(String.Empty, String.Empty), false, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("ReadStartElement")]
        public int TestReadStartElement()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                try
                {
                    DataReader.ReadStartElement();
                }
                catch (XmlException)
                {
                    return TEST_PASS;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
        }

        [Variation("ReadStartElement(name)")]
        public int TestReadStartElementName()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                try
                {
                    DataReader.ReadStartElement(s_NAME);
                }
                catch (XmlException)
                {
                    return TEST_PASS;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
        }

        [Variation("ReadStartElement(String.Empty)")]
        public int TestReadStartElementName2()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                try
                {
                    DataReader.ReadStartElement(String.Empty);
                }
                catch (XmlException)
                {
                    return TEST_PASS;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
        }

        [Variation("ReadStartElement(name, ns)")]
        public int TestReadStartElementNameNs()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                try
                {
                    DataReader.ReadStartElement(s_NAME, s_NS);
                }
                catch (XmlException)
                {
                    return TEST_PASS;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
        }

        [Variation("ReadStartElement(String.Empty,String.Empty)")]
        public int TestReadStartElementNameNs2()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                try
                {
                    DataReader.ReadStartElement(String.Empty, String.Empty);
                }
                catch (XmlException)
                {
                    return TEST_PASS;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
        }

        [Variation("ReadEndElement")]
        public int TestReadEndElement()
        {
            if (IntegrityVer == EINTEGRITY.BEFORE_READ ||
                IntegrityVer == EINTEGRITY.AFTER_RESETSTATE)
            {
                // not interseting for test
                return TEST_SKIPPED;
            }
            else
            {
                try
                {
                    DataReader.ReadEndElement();
                }
                catch (XmlException)
                {
                    return TEST_PASS;
                }
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
        }

        [Variation("LookupNamespace")]
        public int LookupNamespace()
        {
            ReloadSource();
            string[] astr = { "a", "Foo", /*String.Empty,*/ "Foo1", "Foo_S" };  // for String.Empty XmlTextReader returns null, while xmlreader returns ""

            for (int i = 0; i < astr.Length; i++)
            {
                if (DataReader.LookupNamespace(astr[i]) != null)
                {
                    CError.WriteLine("Not NULL " + i + " LookupNameSpace " + DataReader.LookupNamespace(astr[i]) + "," + DataReader.NodeType);
                }
                CError.Compare(DataReader.LookupNamespace(astr[i]), null, CurVariation.Desc);
            }

            return TEST_PASS;
        }

        [Variation("ResolveEntity")]
        public int ResolveEntity()
        {
            ReloadSource();
            try
            {
                DataReader.ResolveEntity();
            }
            catch (InvalidOperationException)
            {
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ReadAttributeValue")]
        public int ReadAttributeValue()
        {
            ReloadSource();
            CError.Compare(DataReader.ReadAttributeValue(), false, CurVariation.Desc);
            CError.Compare(DataReader.ReadAttributeValue(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("Close")]
        public int CloseTest()
        {
            DataReader.Close();
            DataReader.Close();
            DataReader.Close();
            return TEST_PASS;
        }
    }
}
