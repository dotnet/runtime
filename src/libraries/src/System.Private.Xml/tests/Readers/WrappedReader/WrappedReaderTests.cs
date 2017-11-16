// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class WrappedReaderTest : CGenericTestModule
    {
        [Theory]
        [XmlTests(nameof(Create))]
        public void RunTests(XunitTestCase testCase)
        {
            testCase.Run();
        }

        public static CTestModule Create()
        {
            var module = new WrappedReaderTest();

            module.Init(null);
            module.AddChild(new TCErrorConditionReader() { Attribute = new TestCase() { Name = "ErrorCondition", Desc = "WrappedReader" } });
            module.AddChild(new TCXMLExceptionReader() { Attribute = new TestCase() { Name = "XMLException", Desc = "WrappedReader" } });
            module.AddChild(new TCLinePosReader() { Attribute = new TestCase() { Name = "LinePos", Desc = "WrappedReader" } });
            module.AddChild(new TCDepthReader() { Attribute = new TestCase() { Name = "Depth", Desc = "WrappedReader" } });
            module.AddChild(new TCNamespaceReader() { Attribute = new TestCase() { Name = "Namespace", Desc = "WrappedReader" } });
            module.AddChild(new TCLookupNamespaceReader() { Attribute = new TestCase() { Name = "LookupNamespace", Desc = "WrappedReader" } });
            module.AddChild(new TCHasValueReader() { Attribute = new TestCase() { Name = "HasValue", Desc = "WrappedReader" } });
            module.AddChild(new TCIsEmptyElementReader() { Attribute = new TestCase() { Name = "IsEmptyElement", Desc = "WrappedReader" } });
            module.AddChild(new TCXmlSpaceReader() { Attribute = new TestCase() { Name = "XmlSpace", Desc = "WrappedReader" } });
            module.AddChild(new TCXmlLangReader() { Attribute = new TestCase() { Name = "XmlLang", Desc = "WrappedReader" } });
            module.AddChild(new TCSkipReader() { Attribute = new TestCase() { Name = "Skip", Desc = "WrappedReader" } });
            module.AddChild(new TCInvalidXMLReader() { Attribute = new TestCase() { Name = "InvalidXML", Desc = "WrappedReader" } });
            module.AddChild(new TCAttributeAccessReader() { Attribute = new TestCase() { Name = "AttributeAccess", Desc = "WrappedReader" } });
            module.AddChild(new TCThisNameReader() { Attribute = new TestCase() { Name = "This(Name) and This(Name, Namespace)", Desc = "WrappedReader" } });
            module.AddChild(new TCMoveToAttributeReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "WrappedReader" } });
            module.AddChild(new TCGetAttributeOrdinalReader() { Attribute = new TestCase() { Name = "GetAttribute (Ordinal)", Desc = "WrappedReader" } });
            module.AddChild(new TCGetAttributeNameReader() { Attribute = new TestCase() { Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "WrappedReader" } });
            module.AddChild(new TCThisOrdinalReader() { Attribute = new TestCase() { Name = "This [Ordinal]", Desc = "WrappedReader" } });
            module.AddChild(new TCMoveToAttributeOrdinalReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Ordinal)", Desc = "WrappedReader" } });
            module.AddChild(new TCMoveToFirstAttributeReader() { Attribute = new TestCase() { Name = "MoveToFirstAttribute()", Desc = "WrappedReader" } });
            module.AddChild(new TCMoveToNextAttributeReader() { Attribute = new TestCase() { Name = "MoveToNextAttribute()", Desc = "WrappedReader" } });
            module.AddChild(new TCAttributeTestReader() { Attribute = new TestCase() { Name = "Attribute Test when NodeType != Attributes", Desc = "WrappedReader" } });
            module.AddChild(new TCAttributeXmlDeclarationReader() { Attribute = new TestCase() { Name = "Attributes test on XmlDeclaration DCR52258", Desc = "WrappedReader" } });
            module.AddChild(new TCXmlnsReader() { Attribute = new TestCase() { Name = "xmlns as local name DCR50345", Desc = "WrappedReader" } });
            module.AddChild(new TCXmlnsPrefixReader() { Attribute = new TestCase() { Name = "bounded namespace to xmlns prefix DCR50881", Desc = "WrappedReader" } });
            module.AddChild(new TCReadInnerXmlReader() { Attribute = new TestCase() { Name = "ReadInnerXml", Desc = "WrappedReader" } });
            module.AddChild(new TCMoveToContentReader() { Attribute = new TestCase() { Name = "MoveToContent", Desc = "WrappedReader" } });
            module.AddChild(new TCIsStartElementReader() { Attribute = new TestCase() { Name = "IsStartElement", Desc = "WrappedReader" } });
            module.AddChild(new TCReadStartElementReader() { Attribute = new TestCase() { Name = "ReadStartElement", Desc = "WrappedReader" } });
            module.AddChild(new TCReadEndElementReader() { Attribute = new TestCase() { Name = "ReadEndElement", Desc = "WrappedReader" } });
            module.AddChild(new TCResolveEntityReader() { Attribute = new TestCase() { Name = "ResolveEntity and ReadAttributeValue", Desc = "WrappedReader" } });
            module.AddChild(new TCReadAttributeValueReader() { Attribute = new TestCase() { Name = "ReadAttributeValue", Desc = "WrappedReader" } });
            module.AddChild(new TCReadReader() { Attribute = new TestCase() { Name = "Read", Desc = "WrappedReader" } });
            module.AddChild(new TCMoveToElementReader() { Attribute = new TestCase() { Name = "MoveToElement", Desc = "WrappedReader" } });
            module.AddChild(new TCDisposeReader() { Attribute = new TestCase() { Name = "Dispose", Desc = "WrappedReader" } });
            module.AddChild(new TCBufferBoundariesReader() { Attribute = new TestCase() { Name = "Buffer Boundaries", Desc = "WrappedReader" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterClose", Desc = "AfterClose" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterCloseInTheMiddle", Desc = "AfterCloseInTheMiddle" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "BeforeRead", Desc = "BeforeRead" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterReadIsFalse", Desc = "AfterReadIsFalse" } });
            module.AddChild(new TCReadSubtreeReader() { Attribute = new TestCase() { Name = "Read Subtree", Desc = "WrappedReader" } });
            module.AddChild(new TCReadToDescendantReader() { Attribute = new TestCase() { Name = "ReadToDescendant", Desc = "WrappedReader" } });
            module.AddChild(new TCReadToNextSiblingReader() { Attribute = new TestCase() { Name = "ReadToNextSibling", Desc = "WrappedReader" } });
            module.AddChild(new TCReadValueReader() { Attribute = new TestCase() { Name = "ReadValue", Desc = "WrappedReader" } });
            module.AddChild(new TCReadContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadContentAsBase64", Desc = "WrappedReader" } });
            module.AddChild(new TCReadElementContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadElementContentAsBase64", Desc = "WrappedReader" } });
            module.AddChild(new TCReadContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadContentAsBinHex", Desc = "WrappedReader" } });
            module.AddChild(new TCReadElementContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadElementContentAsBinHex", Desc = "WrappedReader" } });
            module.AddChild(new TCReadToFollowingReader() { Attribute = new TestCase() { Name = "ReadToFollowing", Desc = "WrappedReader" } });

            return module;
        }
    }
}
