// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;

namespace System.Xml.Tests
{
    [TestModule(Name = "CustomReader Test", Desc = "CustomReader Test")]
    public partial class CReaderTestModule : CGenericTestModule
    {
        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);
            string strFile = String.Empty;

            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.GENERIC);
            TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.XSLT_COPY);

            ReaderFactory = new CustomReaderFactory();
            return ret;
        }

        public override int Terminate(object objParam)
        {
            return base.Terminate(objParam);
        }
    }

    ////////////////////////////////////////////////////////////////
    // CustomInheritedReader factory
    //
    ////////////////////////////////////////////////////////////////
    internal class CustomReaderFactory : ReaderFactory
    {
        public override XmlReader Create(MyDict<string, object> options)
        {
            string tcDesc = (string)options[ReaderFactory.HT_CURDESC];
            string tcVar = (string)options[ReaderFactory.HT_CURVAR];

            CError.Compare(tcDesc == "custominheritedreader", "Invalid testcase");

            Stream stream = (Stream)options[ReaderFactory.HT_STREAM];
            string filename = (string)options[ReaderFactory.HT_FILENAME];
            string fragment = (string)options[ReaderFactory.HT_FRAGMENT];
            StringReader sr = (StringReader)options[ReaderFactory.HT_STRINGREADER];

            if (sr != null)
            {
                return new CustomReader(sr, false);
            }

            if (stream != null)
            {
                return new CustomReader(stream);
            }

            if (fragment != null)
            {
                return new CustomReader(new StringReader(fragment), true);
            }

            if (filename != null)
            {
                return new CustomReader(filename);
            }

            throw new
                CTestFailedException("CustomReader not created");
        }
    }

    [TestCase(Name = "InvalidXML", Desc = "CustomInheritedReader")]
    internal class TCInvalidXMLReader : TCInvalidXML
    {
    }

    [TestCase(Name = "ErrorCondition", Desc = "CustomInheritedReader")]
    internal class TCErrorConditionReader : TCErrorCondition
    {
    }

    [TestCase(Name = "XMLException", Desc = "CustomInheritedReader")]
    public class TCXMLExceptionReader : TCXMLException
    {
    }

    [TestCase(Name = "LinePos", Desc = "CustomInheritedReader")]
    public class TCLinePosReader : TCLinePos
    {
    }

    [TestCase(Name = "ReadOuterXml", Desc = "CustomInheritedReader")]
    internal class TCReadOuterXmlReader : TCReadOuterXml
    {
    }

    [TestCase(Name = "AttributeAccess", Desc = "CustomInheritedReader")]
    internal class TCAttributeAccessReader : TCAttributeAccess
    {
    }

    [TestCase(Name = "This(Name) and This(Name, Namespace)", Desc = "CustomInheritedReader")]
    internal class TCThisNameReader : TCThisName
    {
    }

    [TestCase(Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "CustomInheritedReader")]
    internal class TCMoveToAttributeReader : TCMoveToAttribute
    {
    }

    [TestCase(Name = "GetAttribute (Ordinal)", Desc = "CustomInheritedReader")]
    internal class TCGetAttributeOrdinalReader : TCGetAttributeOrdinal
    {
    }

    [TestCase(Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "CustomInheritedReader")]
    internal class TCGetAttributeNameReader : TCGetAttributeName
    {
    }

    [TestCase(Name = "This [Ordinal]", Desc = "CustomInheritedReader")]
    internal class TCThisOrdinalReader : TCThisOrdinal
    {
    }

    [TestCase(Name = "MoveToAttribute(Ordinal)", Desc = "CustomInheritedReader")]
    internal class TCMoveToAttributeOrdinalReader : TCMoveToAttributeOrdinal
    {
    }

    [TestCase(Name = "MoveToFirstAttribute()", Desc = "CustomInheritedReader")]
    internal class TCMoveToFirstAttributeReader : TCMoveToFirstAttribute
    {
    }

    [TestCase(Name = "MoveToNextAttribute()", Desc = "CustomInheritedReader")]
    internal class TCMoveToNextAttributeReader : TCMoveToNextAttribute
    {
    }

    [TestCase(Name = "Attribute Test when NodeType != Attributes", Desc = "CustomInheritedReader")]
    internal class TCAttributeTestReader : TCAttributeTest
    {
    }

    [TestCase(Name = "Attributes test on XmlDeclaration DCR52258", Desc = "CustomInheritedReader")]
    internal class TCAttributeXmlDeclarationReader : TCAttributeXmlDeclaration
    {
    }

    [TestCase(Name = "xmlns as local name DCR50345", Desc = "CustomInheritedReader")]
    internal class TCXmlnsReader : TCXmlns
    {
    }

    [TestCase(Name = "bounded namespace to xmlns prefix DCR50881", Desc = "CustomInheritedReader")]
    internal class TCXmlnsPrefixReader : TCXmlnsPrefix
    {
    }

    [TestCase(Name = "ReadState", Desc = "CustomInheritedReader")]
    internal class TCReadStateReader : TCReadState
    {
    }

    [TestCase(Name = "ReadInnerXml", Desc = "CustomInheritedReader")]
    internal class TCReadInnerXmlReader : TCReadInnerXml
    {
    }

    [TestCase(Name = "MoveToContent", Desc = "CustomInheritedReader")]
    internal class TCMoveToContentReader : TCMoveToContent
    {
    }

    [TestCase(Name = "IsStartElement", Desc = "CustomInheritedReader")]
    internal class TCIsStartElementReader : TCIsStartElement
    {
    }

    [TestCase(Name = "ReadStartElement", Desc = "CustomInheritedReader")]
    internal class TCReadStartElementReader : TCReadStartElement
    {
    }

    [TestCase(Name = "ReadEndElement", Desc = "CustomInheritedReader")]
    internal class TCReadEndElementReader : TCReadEndElement
    {
    }

    [TestCase(Name = "ResolveEntity and ReadAttributeValue", Desc = "CustomInheritedReader")]
    internal class TCResolveEntityReader : TCResolveEntity
    {
    }

    [TestCase(Name = "ReadAttributeValue", Desc = "CustomInheritedReader")]
    internal class TCReadAttributeValueReader : TCReadAttributeValue
    {
    }

    [TestCase(Name = "Read", Desc = "CustomInheritedReader")]
    internal class TCReadReader : TCRead2
    {
    }

    [TestCase(Name = "Read Subtree", Desc = "CustomInheritedReader")]
    internal class TCReadSubtreeReader : TCReadSubtree
    {
    }
    [TestCase(Name = "ReadToDescendant", Desc = "CustomInheritedReader")]
    internal class TCReadToDescendantReader : TCReadToDescendant
    {
    }

    [TestCase(Name = "ReadToNextSibling", Desc = "CustomInheritedReader")]
    internal class TCReadToNextSiblingReader : TCReadToNextSibling
    {
    }

    [TestCase(Name = "ReadToFollowing", Desc = "CustomInheritedReader")]
    internal class TCReadToFollowingReader : TCReadToFollowing
    {
    }

    [TestCase(Name = "MoveToElement", Desc = "CustomInheritedReader")]
    internal class TCMoveToElementReader : TCMoveToElement
    {
    }

    [TestCase(Name = "Buffer Boundary", Desc = "CustomInheritedReader")]
    internal class TCBufferBoundariesReader : TCBufferBoundaries
    {
    }

    [TestCase(Name = "Dispose", Desc = "CustomInheritedReader")]
    internal class TCDisposeReader : TCDispose
    {
    }

    [TestCase(Name = "TCXmlNodeIntegrityTestFile", Desc = "TCXmlNodeIntegrityTestFile")]
    internal class TCXmlNodeIntegrityTestFile : TCXMLIntegrityBase
    {
    }

    [TestCase(Name = "ReadValue", Desc = "CustomInheritedReader")]
    internal class TCReadValueReader : TCReadValue
    {
    }

    [TestCase(Name = "ReadContentAsBase64", Desc = "CustomInheritedReader")]
    internal class TCReadContentAsBase64Reader : TCReadContentAsBase64
    {
    }

    [TestCase(Name = "ReadElementContentAsBase64", Desc = "CustomInheritedReader")]
    internal class TCReadElementContentAsBase64Reader : TCReadElementContentAsBase64
    {
    }

    [TestCase(Name = "ReadContentAsBinHex", Desc = "CustomInheritedReader")]
    internal class TCReadContentAsBinHexReader : TCReadContentAsBinHex
    {
    }

    [TestCase(Name = "ReadElementContentAsBinHex", Desc = "CustomInheritedReader")]
    internal class TCReadElementContentAsBinHexReader : TCReadElementContentAsBinHex
    {
    }
}
