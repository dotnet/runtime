// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // Module: use CXMLTypeCoercionTestModule
    //
    ////////////////////////////////////////////////////////////////
    [TestModule(Name = "XmlFactoryReader Test", Desc = "XmlFactoryReader Test")]
    public partial class FactoryReaderTest : CGenericTestModule
    {
        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);
            // Create global usage test files
            string strFile = String.Empty;

            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.GENERIC);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.XSLT_COPY);

            // Create reader factory
            ReaderFactory = new XmlFactoryReaderFactory();
            return ret;
        }

        public override int Terminate(object objParam)
        {
            return base.Terminate(objParam);
        }
    }



    ////////////////////////////////////////////////////////////////
    // FactoryReader factory
    //
    ////////////////////////////////////////////////////////////////
    internal class XmlFactoryReaderFactory : ReaderFactory
    {
        public override XmlReader Create(MyDict<string, object> options)
        {
            string tcDesc = (string)options[ReaderFactory.HT_CURDESC];
            string tcVar = (string)options[ReaderFactory.HT_CURVAR];

            CError.Compare(tcDesc == "factoryreader", "Invalid testcase");

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
                CError.WriteLine("StringReader");
                XmlReader reader = ReaderHelper.Create(sr, rs, string.Empty);
                return reader;
            }

            if (stream != null)
            {
                CError.WriteLine("Stream");
                XmlReader reader = ReaderHelper.Create(stream, rs, filename);
                return reader;
            }

            if (fragment != null)
            {
                CError.WriteLine("Fragment");
                rs.ConformanceLevel = ConformanceLevel.Fragment;
                StringReader tr = new StringReader(fragment);
                XmlReader reader = ReaderHelper.Create(tr, rs, (string)null);

                return reader;
            }

            if (filename != null)
            {
                CError.WriteLine("Filename");
                XmlReader reader = ReaderHelper.Create(filename, rs);
                return reader;
            }

            throw new
                CTestFailedException("Factory Reader not created");
        }
    }

    [TestCase(Name = "ErrorCondition", Desc = "FactoryReader")]
    internal class TCErrorConditionReader : TCErrorCondition
    {
    }

    [TestCase(Name = "XMLException", Desc = "FactoryReader")]
    public class TCXMLExceptionReader : TCXMLException
    {
    }

    [TestCase(Name = "LinePos", Desc = "FactoryReader")]
    public class TCLinePosReader : TCLinePos
    {
    }

    [TestCase(Name = "Depth", Desc = "FactoryReader")]
    internal class TCDepthReader : TCDepth
    {
    }

    [TestCase(Name = "Namespace", Desc = "FactoryReader")]
    internal class TCNamespaceReader : TCNamespace
    {
    }

    [TestCase(Name = "LookupNamespace", Desc = "FactoryReader")]
    internal class TCLookupNamespaceReader : TCLookupNamespace
    {
    }

    [TestCase(Name = "HasValue", Desc = "FactoryReader")]
    internal class TCHasValueReader : TCHasValue
    {
    }

    [TestCase(Name = "IsEmptyElement", Desc = "FactoryReader")]
    internal class TCIsEmptyElementReader : TCIsEmptyElement
    {
    }

    [TestCase(Name = "XmlSpace", Desc = "FactoryReader")]
    internal class TCXmlSpaceReader : TCXmlSpace
    {
    }

    [TestCase(Name = "XmlLang", Desc = "FactoryReader")]
    internal class TCXmlLangReader : TCXmlLang
    {
    }

    [TestCase(Name = "Skip", Desc = "FactoryReader")]
    internal class TCSkipReader : TCSkip
    {
    }

    [TestCase(Name = "BaseURI", Desc = "FactoryReader")]
    internal class TCBaseURIReader : TCBaseURI
    {
    }

    [TestCase(Name = "InvalidXML", Desc = "FactoryReader")]
    internal class TCInvalidXMLReader : TCInvalidXML
    {
    }

    [TestCase(Name = "ReadOuterXml", Desc = "FactoryReader")]
    internal class TCReadOuterXmlReader : TCReadOuterXml
    {
    }

    [TestCase(Name = "AttributeAccess", Desc = "FactoryReader")]
    internal class TCAttributeAccessReader : TCAttributeAccess
    {
    }

    [TestCase(Name = "This(Name) and This(Name, Namespace)", Desc = "FactoryReader")]
    internal class TCThisNameReader : TCThisName
    {
    }

    [TestCase(Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "FactoryReader")]
    internal class TCMoveToAttributeReader : TCMoveToAttribute
    {
    }

    [TestCase(Name = "GetAttribute (Ordinal)", Desc = "FactoryReader")]
    internal class TCGetAttributeOrdinalReader : TCGetAttributeOrdinal
    {
    }

    [TestCase(Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "FactoryReader")]
    internal class TCGetAttributeNameReader : TCGetAttributeName
    {
    }

    [TestCase(Name = "This [Ordinal]", Desc = "FactoryReader")]
    internal class TCThisOrdinalReader : TCThisOrdinal
    {
    }

    [TestCase(Name = "MoveToAttribute(Ordinal)", Desc = "FactoryReader")]
    internal class TCMoveToAttributeOrdinalReader : TCMoveToAttributeOrdinal
    {
    }

    [TestCase(Name = "MoveToFirstAttribute()", Desc = "FactoryReader")]
    internal class TCMoveToFirstAttributeReader : TCMoveToFirstAttribute
    {
    }

    [TestCase(Name = "MoveToNextAttribute()", Desc = "FactoryReader")]
    internal class TCMoveToNextAttributeReader : TCMoveToNextAttribute
    {
    }

    [TestCase(Name = "Attribute Test when NodeType != Attributes", Desc = "FactoryReader")]
    internal class TCAttributeTestReader : TCAttributeTest
    {
    }

    [TestCase(Name = "Attributes test on XmlDeclaration DCR52258", Desc = "FactoryReader")]
    internal class TCAttributeXmlDeclarationReader : TCAttributeXmlDeclaration
    {
    }

    [TestCase(Name = "xmlns as local name DCR50345", Desc = "FactoryReader")]
    internal class TCXmlnsReader : TCXmlns
    {
    }

    [TestCase(Name = "bounded namespace to xmlns prefix DCR50881", Desc = "FactoryReader")]
    internal class TCXmlnsPrefixReader : TCXmlnsPrefix
    {
    }

    [TestCase(Name = "ReadState", Desc = "FactoryReader")]
    internal class TCReadStateReader : TCReadState
    {
    }

    [TestCase(Name = "ReadInnerXml", Desc = "FactoryReader")]
    internal class TCReadInnerXmlReader : TCReadInnerXml
    {
    }

    [TestCase(Name = "MoveToContent", Desc = "FactoryReader")]
    internal class TCMoveToContentReader : TCMoveToContent
    {
    }

    [TestCase(Name = "IsStartElement", Desc = "FactoryReader")]
    internal class TCIsStartElementReader : TCIsStartElement
    {
    }

    [TestCase(Name = "ReadStartElement", Desc = "FactoryReader")]
    internal class TCReadStartElementReader : TCReadStartElement
    {
    }

    [TestCase(Name = "ReadEndElement", Desc = "FactoryReader")]
    internal class TCReadEndElementReader : TCReadEndElement
    {
    }

    [TestCase(Name = "ResolveEntity and ReadAttributeValue", Desc = "FactoryReader")]
    internal class TCResolveEntityReader : TCResolveEntity
    {
    }

    [TestCase(Name = "ReadAttributeValue", Desc = "FactoryReader")]
    internal class TCReadAttributeValueReader : TCReadAttributeValue
    {
    }

    [TestCase(Name = "Read", Desc = "FactoryReader")]
    internal class TCReadReader : TCRead2
    {
    }

    [TestCase(Name = "MoveToElement", Desc = "FactoryReader")]
    internal class TCMoveToElementReader : TCMoveToElement
    {
    }

    [TestCase(Name = "Dispose", Desc = "FactoryReader")]
    internal class TCDisposeReader : TCDispose
    {
    }

    [TestCase(Name = "Buffer Boundaries", Desc = "FactoryReader")]
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

    [TestCase(Name = "Read Subtree", Desc = "FactoryReader")]
    internal class TCReadSubtreeReader : TCReadSubtree
    {
    }

    [TestCase(Name = "ReadToDescendant", Desc = "FactoryReader")]
    internal class TCReadToDescendantReader : TCReadToDescendant
    {
    }

    [TestCase(Name = "ReadToNextSibling", Desc = "FactoryReader")]
    internal class TCReadToNextSiblingReader : TCReadToNextSibling
    {
    }

    [TestCase(Name = "ReadValue", Desc = "FactoryReader")]
    internal class TCReadValueReader : TCReadValue
    {
    }

    [TestCase(Name = "ReadContentAsBase64", Desc = "FactoryReader")]
    internal class TCReadContentAsBase64Reader : TCReadContentAsBase64
    {
    }

    [TestCase(Name = "ReadElementContentAsBase64", Desc = "FactoryReader")]
    internal class TCReadElementContentAsBase64Reader : TCReadElementContentAsBase64
    {
    }

    [TestCase(Name = "ReadContentAsBinHex", Desc = "FactoryReader")]
    internal class TCReadContentAsBinHexReader : TCReadContentAsBinHex
    {
    }

    [TestCase(Name = "ReadElementContentAsBinHex", Desc = "FactoryReader")]
    internal class TCReadElementContentAsBinHexReader : TCReadElementContentAsBinHex
    {
    }

    [TestCase(Name = "ReadToFollowing", Desc = "FactoryReader")]
    internal class TCReadToFollowingReader : TCReadToFollowing
    {
    }
}
