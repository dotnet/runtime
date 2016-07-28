// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestModule(Name = "XmlSubtreeReader Test", Desc = "XmlSubtreeReader Test")]
    public partial class SubtreeReaderTest : CGenericTestModule
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

            ReaderFactory = new XmlSubtreeReaderFactory();
            return ret;
        }

        public override int Terminate(object objParam)
        {
            return base.Terminate(objParam);
        }
    }

    ////////////////////////////////////////////////////////////////
    // SubtreeReader factory
    //
    ////////////////////////////////////////////////////////////////
    internal class XmlSubtreeReaderFactory : ReaderFactory
    {
        public override XmlReader Create(MyDict<string, object> options)
        {
            string tcDesc = (string)options[ReaderFactory.HT_CURDESC];
            string tcVar = (string)options[ReaderFactory.HT_CURVAR];

            CError.Compare(tcDesc == "subtreereader", "Invalid testcase");

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
                CError.WriteLine("SubtreeReader String");

                XmlReader r = ReaderHelper.Create(sr, rs, string.Empty);
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element)
                        break;
                }
                XmlReader wr = r.ReadSubtree();
                return wr;
            }

            if (stream != null)
            {
                CError.WriteLine("SubtreeReader Stream");

                XmlReader r = ReaderHelper.Create(stream, rs, filename);
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element)
                        break;
                }
                XmlReader wr = r.ReadSubtree();
                return wr;
            }

            if (fragment != null)
            {
                CError.WriteLine("SubtreeReader Fragment");
                rs.ConformanceLevel = ConformanceLevel.Fragment;
                StringReader tr = new StringReader(fragment);

                XmlReader r = ReaderHelper.Create(tr, rs, (string)null);
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element)
                        break;
                }
                XmlReader wr = r.ReadSubtree();
                return wr;
            }

            if (filename != null)
            {
                CError.WriteLine("SubtreeReader Filename");

                Stream fs = FilePathUtil.getStream(filename);
                XmlReader r = ReaderHelper.Create(fs, rs, filename);
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element)
                        break;
                }
                XmlReader wr = r.ReadSubtree();
                return wr;
            }

            throw new CTestFailedException("SubtreeReader not created");
        }
    }

    [TestCase(Name = "InvalidXML", Desc = "SubtreeReader")]
    public class TCInvalidXMLReader : TCInvalidXML
    {
    }

    [TestCase(Name = "ErrorCondition", Desc = "SubtreeReader")]
    public class TCErrorConditionReader : TCErrorCondition
    {
    }

    [TestCase(Name = "Depth", Desc = "SubtreeReader")]
    public class TCDepthReader : TCDepth
    {
    }

    [TestCase(Name = "Namespace", Desc = "SubtreeReader")]
    public class TCNamespaceReader : TCNamespace
    {
    }

    [TestCase(Name = "LookupNamespace", Desc = "SubtreeReader")]
    public class TCLookupNamespaceReader : TCLookupNamespace
    {
    }

    [TestCase(Name = "IsEmptyElement", Desc = "SubtreeReader")]
    public class TCIsEmptyElementReader : TCIsEmptyElement
    {
    }

    [TestCase(Name = "XmlSpace", Desc = "SubtreeReader")]
    public class TCXmlSpaceReader : TCXmlSpace
    {
    }

    [TestCase(Name = "XmlLang", Desc = "SubtreeReader")]
    public class TCXmlLangReader : TCXmlLang
    {
    }

    [TestCase(Name = "Skip", Desc = "SubtreeReader")]
    public class TCSkipReader : TCSkip
    {
    }

    [TestCase(Name = "ReadOuterXml", Desc = "SubtreeReader")]
    public class TCReadOuterXmlReader : TCReadOuterXml
    {
    }

    [TestCase(Name = "AttributeAccess", Desc = "SubtreeReader")]
    public class TCAttributeAccessReader : TCAttributeAccess
    {
    }

    [TestCase(Name = "This(Name) and This(Name, Namespace)", Desc = "SubtreeReader")]
    public class TCThisNameReader : TCThisName
    {
    }

    [TestCase(Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "SubtreeReader")]
    public class TCMoveToAttributeReader : TCMoveToAttribute
    {
    }

    [TestCase(Name = "GetAttribute (Ordinal)", Desc = "SubtreeReader")]
    public class TCGetAttributeOrdinalReader : TCGetAttributeOrdinal
    {
    }

    [TestCase(Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "SubtreeReader")]
    public class TCGetAttributeNameReader : TCGetAttributeName
    {
    }

    [TestCase(Name = "This [Ordinal]", Desc = "SubtreeReader")]
    public class TCThisOrdinalReader : TCThisOrdinal
    {
    }

    [TestCase(Name = "MoveToAttribute(Ordinal)", Desc = "SubtreeReader")]
    public class TCMoveToAttributeOrdinalReader : TCMoveToAttributeOrdinal
    {
    }

    [TestCase(Name = "MoveToFirstAttribute()", Desc = "SubtreeReader")]
    public class TCMoveToFirstAttributeReader : TCMoveToFirstAttribute
    {
    }

    [TestCase(Name = "MoveToNextAttribute()", Desc = "SubtreeReader")]
    public class TCMoveToNextAttributeReader : TCMoveToNextAttribute
    {
    }

    [TestCase(Name = "Attribute Test when NodeType != Attributes", Desc = "SubtreeReader")]
    public class TCAttributeTestReader : TCAttributeTest
    {
    }

    [TestCase(Name = "xmlns as local name DCR50345", Desc = "SubtreeReader")]
    public class TCXmlnsReader : TCXmlns
    {
    }

    [TestCase(Name = "bounded namespace to xmlns prefix DCR50881", Desc = "SubtreeReader")]
    public class TCXmlnsPrefixReader : TCXmlnsPrefix
    {
    }

    [TestCase(Name = "ReadInnerXml", Desc = "SubtreeReader")]
    public class TCReadInnerXmlReader : TCReadInnerXml
    {
    }

    [TestCase(Name = "MoveToContent", Desc = "SubtreeReader")]
    public class TCMoveToContentReader : TCMoveToContent
    {
    }

    [TestCase(Name = "IsStartElement", Desc = "SubtreeReader")]
    public class TCIsStartElementReader : TCIsStartElement
    {
    }

    [TestCase(Name = "ReadStartElement", Desc = "SubtreeReader")]
    public class TCReadStartElementReader : TCReadStartElement
    {
    }

    [TestCase(Name = "ReadEndElement", Desc = "SubtreeReader")]
    public class TCReadEndElementReader : TCReadEndElement
    {
    }

    [TestCase(Name = "ResolveEntity and ReadAttributeValue", Desc = "SubtreeReader")]
    public class TCResolveEntityReader : TCResolveEntity
    {
    }

    [TestCase(Name = "HasValue", Desc = "SubtreeReader")]
    public class TCHasValueReader : TCHasValue
    {
    }

    [TestCase(Name = "ReadAttributeValue", Desc = "SubtreeReader")]
    public class TCReadAttributeValueReader : TCReadAttributeValue
    {
    }

    [TestCase(Name = "Read", Desc = "SubtreeReader")]
    public class TCReadReader : TCRead2
    {
    }

    [TestCase(Name = "MoveToElement", Desc = "SubtreeReader")]
    public class TCMoveToElementReader : TCMoveToElement
    {
    }

    [TestCase(Name = "Dispose", Desc = "SubtreeReader")]
    public class TCDisposeReader : TCDispose
    {
    }

    [TestCase(Name = "Buffer Boundaries", Desc = "SubtreeReader")]
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

    [TestCase(Name = "Read Subtree", Desc = "SubtreeReader")]
    public class TCReadSubtreeReader : TCReadSubtree
    {
    }

    [TestCase(Name = "ReadToDescendant", Desc = "SubtreeReader")]
    public class TCReadToDescendantReader : TCReadToDescendant
    {
    }

    [TestCase(Name = "ReadToNextSibling", Desc = "SubtreeReader")]
    public class TCReadToNextSiblingReader : TCReadToNextSibling
    {
    }

    [TestCase(Name = "ReadValue", Desc = "SubtreeReader")]
    public class TCReadValueReader : TCReadValue
    {
    }

    [TestCase(Name = "ReadContentAsBase64", Desc = "SubtreeReader")]
    public class TCReadContentAsBase64Reader : TCReadContentAsBase64
    {
    }

    [TestCase(Name = "ReadElementContentAsBase64", Desc = "SubtreeReader")]
    public class TCReadElementContentAsBase64Reader : TCReadElementContentAsBase64
    {
    }

    [TestCase(Name = "ReadContentAsBinHex", Desc = "SubtreeReader")]
    public class TCReadContentAsBinHexReader : TCReadContentAsBinHex
    {
    }

    [TestCase(Name = "ReadElementContentAsBinHex", Desc = "SubtreeReader")]
    public class TCReadElementContentAsBinHexReader : TCReadElementContentAsBinHex
    {
    }

    [TestCase(Name = "ReadToFollowing", Desc = "SubtreeReader")]
    public class TCReadToFollowingReader : TCReadToFollowing
    {
    }
}
