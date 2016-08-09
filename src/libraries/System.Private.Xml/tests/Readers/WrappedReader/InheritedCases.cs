// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestModule(Name = "XmlWrappedReader Test", Desc = "XmlWrappedReader Test")]
    public partial class WrappedReaderTest : CGenericTestModule
    {
        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);
            string strFile = String.Empty;

            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.GENERIC);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.XSLT_COPY);

            ReaderFactory = new XmlWrappedReaderFactory();
            return ret;
        }

        public override int Terminate(object objParam)
        {
            return base.Terminate(objParam);
        }
    }

    ////////////////////////////////////////////////////////////////
    // WrappedReader factory
    //
    ////////////////////////////////////////////////////////////////
    internal class XmlWrappedReaderFactory : ReaderFactory
    {
        public override XmlReader Create(MyDict<string, object> options)
        {
            string tcDesc = (string)options[ReaderFactory.HT_CURDESC];
            string tcVar = (string)options[ReaderFactory.HT_CURVAR];

            CError.Compare(tcDesc == "wrappedreader", "Invalid testcase");

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
            if (sr != null)
            {
                CError.WriteLine("WrappedReader String");

                XmlReader r = ReaderHelper.Create(sr, rs, string.Empty);
                XmlReader wr = ReaderHelper.Create(r, rs);
                return wr;
            }

            if (stream != null)
            {
                CError.WriteLine("WrappedReader Stream");

                XmlReader r = ReaderHelper.Create(stream, rs, filename);
                XmlReader wr = ReaderHelper.Create(r, rs);
                return wr;
            }

            if (fragment != null)
            {
                CError.WriteLine("WrappedReader Fragment");
                rs.ConformanceLevel = ConformanceLevel.Fragment;
                StringReader tr = new StringReader(fragment);

                XmlReader r = ReaderHelper.Create(tr, rs, (string)null);
                XmlReader wr = ReaderHelper.Create(r, rs);
                return wr;
            }

            if (filename != null)
            {
                CError.WriteLine("WrappedReader Filename");

                Stream fs = FilePathUtil.getStream(filename);
                XmlReader r = ReaderHelper.Create(fs, rs, filename);
                XmlReader wr = ReaderHelper.Create(r, rs);
                return wr;
            }
            throw new CTestFailedException("WrappedReader not created");
        }
    }

    [TestCase(Name = "ErrorCondition", Desc = "WrappedReader")]
    internal class TCErrorConditionReader : TCErrorCondition
    {
    }

    [TestCase(Name = "XMLException", Desc = "WrappedReader")]
    public class TCXMLExceptionReader : TCXMLException
    {
    }

    [TestCase(Name = "LinePos", Desc = "WrappedReader")]
    public class TCLinePosReader : TCLinePos
    {
    }

    [TestCase(Name = "Depth", Desc = "WrappedReader")]
    internal class TCDepthReader : TCDepth
    {
    }

    [TestCase(Name = "Namespace", Desc = "WrappedReader")]
    internal class TCNamespaceReader : TCNamespace
    {
    }

    [TestCase(Name = "LookupNamespace", Desc = "WrappedReader")]
    internal class TCLookupNamespaceReader : TCLookupNamespace
    {
    }

    [TestCase(Name = "HasValue", Desc = "WrappedReader")]
    internal class TCHasValueReader : TCHasValue
    {
    }

    [TestCase(Name = "IsEmptyElement", Desc = "WrappedReader")]
    internal class TCIsEmptyElementReader : TCIsEmptyElement
    {
    }

    [TestCase(Name = "XmlSpace", Desc = "WrappedReader")]
    internal class TCXmlSpaceReader : TCXmlSpace
    {
    }

    [TestCase(Name = "XmlLang", Desc = "WrappedReader")]
    internal class TCXmlLangReader : TCXmlLang
    {
    }

    [TestCase(Name = "Skip", Desc = "WrappedReader")]
    internal class TCSkipReader : TCSkip
    {
    }

    [TestCase(Name = "InvalidXML", Desc = "WrappedReader")]
    internal class TCInvalidXMLReader : TCInvalidXML
    {
    }

    [TestCase(Name = "AttributeAccess", Desc = "WrappedReader")]
    internal class TCAttributeAccessReader : TCAttributeAccess
    {
    }

    [TestCase(Name = "This(Name) and This(Name, Namespace)", Desc = "WrappedReader")]
    internal class TCThisNameReader : TCThisName
    {
    }

    [TestCase(Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "WrappedReader")]
    internal class TCMoveToAttributeReader : TCMoveToAttribute
    {
    }

    [TestCase(Name = "GetAttribute (Ordinal)", Desc = "WrappedReader")]
    internal class TCGetAttributeOrdinalReader : TCGetAttributeOrdinal
    {
    }

    [TestCase(Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "WrappedReader")]
    internal class TCGetAttributeNameReader : TCGetAttributeName
    {
    }

    [TestCase(Name = "This [Ordinal]", Desc = "WrappedReader")]
    internal class TCThisOrdinalReader : TCThisOrdinal
    {
    }

    [TestCase(Name = "MoveToAttribute(Ordinal)", Desc = "WrappedReader")]
    internal class TCMoveToAttributeOrdinalReader : TCMoveToAttributeOrdinal
    {
    }

    [TestCase(Name = "MoveToFirstAttribute()", Desc = "WrappedReader")]
    internal class TCMoveToFirstAttributeReader : TCMoveToFirstAttribute
    {
    }

    [TestCase(Name = "MoveToNextAttribute()", Desc = "WrappedReader")]
    internal class TCMoveToNextAttributeReader : TCMoveToNextAttribute
    {
    }

    [TestCase(Name = "Attribute Test when NodeType != Attributes", Desc = "WrappedReader")]
    internal class TCAttributeTestReader : TCAttributeTest
    {
    }

    [TestCase(Name = "Attributes test on XmlDeclaration DCR52258", Desc = "WrappedReader")]
    internal class TCAttributeXmlDeclarationReader : TCAttributeXmlDeclaration
    {
    }

    [TestCase(Name = "xmlns as local name DCR50345", Desc = "WrappedReader")]
    internal class TCXmlnsReader : TCXmlns
    {
    }

    [TestCase(Name = "bounded namespace to xmlns prefix DCR50881", Desc = "WrappedReader")]
    internal class TCXmlnsPrefixReader : TCXmlnsPrefix
    {
    }

    [TestCase(Name = "ReadInnerXml", Desc = "WrappedReader")]
    internal class TCReadInnerXmlReader : TCReadInnerXml
    {
    }

    [TestCase(Name = "MoveToContent", Desc = "WrappedReader")]
    internal class TCMoveToContentReader : TCMoveToContent
    {
    }

    [TestCase(Name = "IsStartElement", Desc = "WrappedReader")]
    internal class TCIsStartElementReader : TCIsStartElement
    {
    }

    [TestCase(Name = "ReadStartElement", Desc = "WrappedReader")]
    internal class TCReadStartElementReader : TCReadStartElement
    {
    }

    [TestCase(Name = "ReadEndElement", Desc = "WrappedReader")]
    internal class TCReadEndElementReader : TCReadEndElement
    {
    }

    [TestCase(Name = "ResolveEntity and ReadAttributeValue", Desc = "WrappedReader")]
    internal class TCResolveEntityReader : TCResolveEntity
    {
    }

    [TestCase(Name = "ReadAttributeValue", Desc = "WrappedReader")]
    internal class TCReadAttributeValueReader : TCReadAttributeValue
    {
    }

    [TestCase(Name = "Read", Desc = "WrappedReader")]
    internal class TCReadReader : TCRead2
    {
    }

    [TestCase(Name = "MoveToElement", Desc = "WrappedReader")]
    internal class TCMoveToElementReader : TCMoveToElement
    {
    }

    [TestCase(Name = "Dispose", Desc = "WrappedReader")]
    internal class TCDisposeReader : TCDispose
    {
    }

    [TestCase(Name = "Buffer Boundaries", Desc = "WrappedReader")]
    internal class TCBufferBoundariesReader : TCBufferBoundaries
    {
    }

    //[TestCase(Name = "BeforeRead", Desc = "BeforeRead")]
    //[TestCase(Name = "AfterReadIsFalse", Desc = "AfterReadIsFalse")]
    //[TestCase(Name = "AfterCloseInTheMiddle", Desc = "AfterCloseInTheMiddle")]
    //[TestCase(Name = "AfterClose", Desc = "AfterClose")]
    internal class TCXmlNodeIntegrityTestFile : TCXMLIntegrityBase
    {
    }

    [TestCase(Name = "Read Subtree", Desc = "WrappedReader")]
    internal class TCReadSubtreeReader : TCReadSubtree
    {
    }

    [TestCase(Name = "ReadToDescendant", Desc = "WrappedReader")]
    internal class TCReadToDescendantReader : TCReadToDescendant
    {
    }

    [TestCase(Name = "ReadToNextSibling", Desc = "WrappedReader")]
    internal class TCReadToNextSiblingReader : TCReadToNextSibling
    {
    }

    [TestCase(Name = "ReadValue", Desc = "WrappedReader")]
    internal class TCReadValueReader : TCReadValue
    {
    }

    [TestCase(Name = "ReadContentAsBase64", Desc = "WrappedReader")]
    internal class TCReadContentAsBase64Reader : TCReadContentAsBase64
    {
    }

    [TestCase(Name = "ReadElementContentAsBase64", Desc = "WrappedReader")]
    internal class TCReadElementContentAsBase64Reader : TCReadElementContentAsBase64
    {
    }

    [TestCase(Name = "ReadContentAsBinHex", Desc = "WrappedReader")]
    internal class TCReadContentAsBinHexReader : TCReadContentAsBinHex
    {
    }

    [TestCase(Name = "ReadElementContentAsBinHex", Desc = "WrappedReader")]
    internal class TCReadElementContentAsBinHexReader : TCReadElementContentAsBinHex
    {
    }

    [TestCase(Name = "ReadToFollowing", Desc = "WrappedReader")]
    internal class TCReadToFollowingReader : TCReadToFollowing
    {
    }
}
