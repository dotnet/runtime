// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestModule(Name = "XmlCharCheckingReader Test", Desc = "XmlCharCheckingReader Test")]
    public partial class CharCheckingReaderTest : CGenericTestModule
    {
        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);
            string strFile = String.Empty;

            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.GENERIC);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.XSLT_COPY);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.XMLSCHEMA);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.INVALID_DTD);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.INVALID_NAMESPACE);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.INVALID_SCHEMA);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.INVWELLFORMED_DTD);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.NONWELLFORMED_DTD);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.VALID_DTD);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.WELLFORMED_DTD);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.WHITESPACE_TEST);

            ReaderFactory = new XmlCharCheckingReaderFactory();
            return ret;
        }

        public override int Terminate(object objParam)
        {
            return base.Terminate(objParam);
        }
    }

    ////////////////////////////////////////////////////////////////
    // CharCheckingReader factory
    //
    ////////////////////////////////////////////////////////////////
    internal class XmlCharCheckingReaderFactory : ReaderFactory
    {
        public override XmlReader Create(MyDict<string, object> options)
        {
            string tcDesc = (string)options[ReaderFactory.HT_CURDESC];
            string tcVar = (string)options[ReaderFactory.HT_CURVAR];

            CError.Compare(tcDesc == "charcheckingreader", "Invalid testcase");

            XmlReaderSettings rs = (XmlReaderSettings)options[ReaderFactory.HT_READERSETTINGS];
            Stream stream = (Stream)options[ReaderFactory.HT_STREAM];
            string filename = (string)options[ReaderFactory.HT_FILENAME];
            object readerType = options[ReaderFactory.HT_READERTYPE];
            object vt = options[ReaderFactory.HT_VALIDATIONTYPE];
            string fragment = (string)options[ReaderFactory.HT_FRAGMENT];
            StringReader sr = (StringReader)options[ReaderFactory.HT_STRINGREADER];

            if (rs == null)
                rs = new XmlReaderSettings();

            rs.DtdProcessing = DtdProcessing.Ignore;
            rs.CheckCharacters = false;

            XmlReaderSettings wrs = new XmlReaderSettings();
            wrs.DtdProcessing = DtdProcessing.Ignore;
            wrs.CheckCharacters = true;
            wrs.ConformanceLevel = ConformanceLevel.Auto;

            if (sr != null)
            {
                CError.WriteLine("CharCheckingReader String");
                XmlReader r = ReaderHelper.Create(sr, rs, string.Empty);
                XmlReader wr = ReaderHelper.Create(r, wrs);
                return wr;
            }

            if (stream != null)
            {
                CError.WriteLine("CharCheckingReader Stream");
                XmlReader r = ReaderHelper.Create(stream, rs, filename);
                XmlReader wr = ReaderHelper.Create(r, wrs);
                return wr;
            }

            if (fragment != null)
            {
                CError.WriteLine("CharCheckingReader Fragment");
                rs.ConformanceLevel = ConformanceLevel.Fragment;
                StringReader tr = new StringReader(fragment);
                XmlReader r = ReaderHelper.Create(tr, rs, (string)null);
                XmlReader wr = ReaderHelper.Create(r, wrs);
                return wr;
            }

            if (filename != null)
            {
                CError.WriteLine("CharCheckingReader Filename");
                Stream fs = FilePathUtil.getStream(filename);
                XmlReader r = ReaderHelper.Create(fs, rs, filename);
                XmlReader wr = ReaderHelper.Create(r, wrs);
                return wr;
            }
            return null;
        }
    }

    [TestCase(Name = "ErrorCondition", Desc = "CharCheckingReader")]
    public class TCErrorConditionReader : TCErrorCondition
    {
    }

    [TestCase(Name = "Depth", Desc = "CharCheckingReader")]
    public class TCDepthReader : TCDepth
    {
    }

    [TestCase(Name = "Namespace", Desc = "CharCheckingReader")]
    public class TCNamespaceReader : TCNamespace
    {
    }

    [TestCase(Name = "LookupNamespace", Desc = "CharCheckingReader")]
    public class TCLookupNamespaceReader : TCLookupNamespace
    {
    }

    [TestCase(Name = "IsEmptyElement", Desc = "CharCheckingReader")]
    public class TCIsEmptyElementReader : TCIsEmptyElement
    {
    }

    [TestCase(Name = "XmlSpace", Desc = "CharCheckingReader")]
    public class TCXmlSpaceReader : TCXmlSpace
    {
    }

    [TestCase(Name = "XmlLang", Desc = "CharCheckingReader")]
    public class TCXmlLangReader : TCXmlLang
    {
    }

    [TestCase(Name = "Skip", Desc = "CharCheckingReader")]
    public class TCSkipReader : TCSkip
    {
    }

    [TestCase(Name = "InvalidXML", Desc = "CharCheckingReader")]
    public class TCInvalidXMLReader : TCInvalidXML
    {
    }

    [TestCase(Name = "ReadOuterXml", Desc = "CharCheckingReader")]
    public class TCReadOuterXmlReader : TCReadOuterXml
    {
    }

    [TestCase(Name = "AttributeAccess", Desc = "CharCheckingReader")]
    public class TCAttributeAccessReader : TCAttributeAccess
    {
    }

    [TestCase(Name = "This(Name) and This(Name, Namespace)", Desc = "CharCheckingReader")]
    public class TCThisNameReader : TCThisName
    {
    }

    [TestCase(Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "CharCheckingReader")]
    public class TCMoveToAttributeReader : TCMoveToAttribute
    {
    }

    [TestCase(Name = "GetAttribute (Ordinal)", Desc = "CharCheckingReader")]
    public class TCGetAttributeOrdinalReader : TCGetAttributeOrdinal
    {
    }

    [TestCase(Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "CharCheckingReader")]
    public class TCGetAttributeNameReader : TCGetAttributeName
    {
    }

    [TestCase(Name = "This [Ordinal]", Desc = "CharCheckingReader")]
    public class TCThisOrdinalReader : TCThisOrdinal
    {
    }

    [TestCase(Name = "MoveToAttribute(Ordinal)", Desc = "CharCheckingReader")]
    public class TCMoveToAttributeOrdinalReader : TCMoveToAttributeOrdinal
    {
    }

    [TestCase(Name = "MoveToFirstAttribute()", Desc = "CharCheckingReader")]
    public class TCMoveToFirstAttributeReader : TCMoveToFirstAttribute
    {
    }

    [TestCase(Name = "MoveToNextAttribute()", Desc = "CharCheckingReader")]
    public class TCMoveToNextAttributeReader : TCMoveToNextAttribute
    {
    }

    [TestCase(Name = "Attribute Test when NodeType != Attributes", Desc = "CharCheckingReader")]
    public class TCAttributeTestReader : TCAttributeTest
    {
    }

    [TestCase(Name = "Attributes test on XmlDeclaration DCR52258", Desc = "CharCheckingReader")]
    public class TCAttributeXmlDeclarationReader : TCAttributeXmlDeclaration
    {
    }

    [TestCase(Name = "xmlns as local name DCR50345", Desc = "CharCheckingReader")]
    public class TCXmlnsReader : TCXmlns
    {
    }

    [TestCase(Name = "bounded namespace to xmlns prefix DCR50881", Desc = "CharCheckingReader")]
    public class TCXmlnsPrefixReader : TCXmlnsPrefix
    {
    }

    [TestCase(Name = "ReadInnerXml", Desc = "CharCheckingReader")]
    public class TCReadInnerXmlReader : TCReadInnerXml
    {
    }

    [TestCase(Name = "MoveToContent", Desc = "CharCheckingReader")]
    public class TCMoveToContentReader : TCMoveToContent
    {
    }

    [TestCase(Name = "IsStartElement", Desc = "CharCheckingReader")]
    public class TCIsStartElementReader : TCIsStartElement
    {
    }

    [TestCase(Name = "ReadStartElement", Desc = "CharCheckingReader")]
    public class TCReadStartElementReader : TCReadStartElement
    {
    }

    [TestCase(Name = "ReadEndElement", Desc = "CharCheckingReader")]
    public class TCReadEndElementReader : TCReadEndElement
    {
    }

    [TestCase(Name = "ResolveEntity and ReadAttributeValue", Desc = "CharCheckingReader")]
    public class TCResolveEntityReader : TCResolveEntity
    {
    }

    [TestCase(Name = "HasValue", Desc = "CharCheckingReader")]
    public class TCHasValueReader : TCHasValue
    {
    }

    [TestCase(Name = "ReadAttributeValue", Desc = "CharCheckingReader")]
    public class TCReadAttributeValueReader : TCReadAttributeValue
    {
    }

    [TestCase(Name = "Read", Desc = "CharCheckingReader")]
    public class TCReadReader : TCRead2
    {
    }

    [TestCase(Name = "MoveToElement", Desc = "CharCheckingReader")]
    public class TCMoveToElementReader : TCMoveToElement
    {
    }

    [TestCase(Name = "Dispose", Desc = "CharCheckingReader")]
    public class TCDisposeReader : TCDispose
    {
    }

    [TestCase(Name = "Buffer Boundaries", Desc = "CharCheckingReader")]
    public class TCBufferBoundariesReader : TCBufferBoundaries
    {
    }

    //[TestCase(Name = "BeforeRead", Desc = "BeforeRead")]
    //[TestCase(Name = "AfterReadIsFalse", Desc = "AfterReadIsFalse")]
    //[TestCase(Name = "AfterCloseInTheMiddle", Desc = "AfterCloseInTheMiddle")]
    //[TestCase(Name = "AfterClose", Desc = "AfterClose")]
    public class TCXmlNodeIntegrityTestFile : TCXMLIntegrityBase
    {
    }

    [TestCase(Name = "Read Subtree", Desc = "CharCheckingReader")]
    public class TCReadSubtreeReader : TCReadSubtree
    {
    }

    [TestCase(Name = "ReadToDescendant", Desc = "CharCheckingReader")]
    public class TCReadToDescendantReader : TCReadToDescendant
    {
    }

    [TestCase(Name = "ReadToNextSibling", Desc = "CharCheckingReader")]
    public class TCReadToNextSiblingReader : TCReadToNextSibling
    {
    }

    [TestCase(Name = "ReadValue", Desc = "CharCheckingReader")]
    public class TCReadValueReader : TCReadValue
    {
    }

    [TestCase(Name = "ReadContentAsBase64", Desc = "CharCheckingReader")]
    public class TCReadContentAsBase64Reader : TCReadContentAsBase64
    {
    }

    [TestCase(Name = "ReadElementContentAsBase64", Desc = "CharCheckingReader")]
    public class TCReadElementContentAsBase64Reader : TCReadElementContentAsBase64
    {
    }

    [TestCase(Name = "ReadContentAsBinHex", Desc = "CharCheckingReader")]
    public class TCReadContentAsBinHexReader : TCReadContentAsBinHex
    {
    }

    [TestCase(Name = "ReadElementContentAsBinHex", Desc = "CharCheckingReader")]
    public class TCReadElementContentAsBinHexReader : TCReadElementContentAsBinHex
    {
    }

    [TestCase(Name = "ReadToFollowing", Desc = "CharCheckingReader")]
    public class TCReadToFollowingReader : TCReadToFollowing
    {
    }
}
