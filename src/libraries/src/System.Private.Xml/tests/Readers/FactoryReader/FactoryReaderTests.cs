// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class FactoryReaderTest : CGenericTestModule
    {
        [Theory]
        [XmlTests(nameof(Create))]
        public void RunTests(XunitTestCase testCase)
        {
            testCase.Run();
        }

        public static CTestModule Create()
        {
            var module = new FactoryReaderTest();

            module.Init(null);
            module.AddChild(new TCErrorConditionReader() { Attribute = new TestCase() { Name = "ErrorCondition", Desc = "FactoryReader" } });
            module.AddChild(new TCXMLExceptionReader() { Attribute = new TestCase() { Name = "XMLException", Desc = "FactoryReader" } });
            module.AddChild(new TCLinePosReader() { Attribute = new TestCase() { Name = "LinePos", Desc = "FactoryReader" } });
            module.AddChild(new TCDepthReader() { Attribute = new TestCase() { Name = "Depth", Desc = "FactoryReader" } });
            module.AddChild(new TCNamespaceReader() { Attribute = new TestCase() { Name = "Namespace", Desc = "FactoryReader" } });
            module.AddChild(new TCLookupNamespaceReader() { Attribute = new TestCase() { Name = "LookupNamespace", Desc = "FactoryReader" } });
            module.AddChild(new TCHasValueReader() { Attribute = new TestCase() { Name = "HasValue", Desc = "FactoryReader" } });
            module.AddChild(new TCIsEmptyElementReader() { Attribute = new TestCase() { Name = "IsEmptyElement", Desc = "FactoryReader" } });
            module.AddChild(new TCXmlSpaceReader() { Attribute = new TestCase() { Name = "XmlSpace", Desc = "FactoryReader" } });
            module.AddChild(new TCXmlLangReader() { Attribute = new TestCase() { Name = "XmlLang", Desc = "FactoryReader" } });
            module.AddChild(new TCSkipReader() { Attribute = new TestCase() { Name = "Skip", Desc = "FactoryReader" } });
            module.AddChild(new TCBaseURIReader() { Attribute = new TestCase() { Name = "BaseURI", Desc = "FactoryReader" } });
            module.AddChild(new TCInvalidXMLReader() { Attribute = new TestCase() { Name = "InvalidXML", Desc = "FactoryReader" } });
            module.AddChild(new TCReadOuterXmlReader() { Attribute = new TestCase() { Name = "ReadOuterXml", Desc = "FactoryReader" } });
            module.AddChild(new TCAttributeAccessReader() { Attribute = new TestCase() { Name = "AttributeAccess", Desc = "FactoryReader" } });
            module.AddChild(new TCThisNameReader() { Attribute = new TestCase() { Name = "This(Name) and This(Name, Namespace)", Desc = "FactoryReader" } });
            module.AddChild(new TCMoveToAttributeReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "FactoryReader" } });
            module.AddChild(new TCGetAttributeOrdinalReader() { Attribute = new TestCase() { Name = "GetAttribute (Ordinal)", Desc = "FactoryReader" } });
            module.AddChild(new TCGetAttributeNameReader() { Attribute = new TestCase() { Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "FactoryReader" } });
            module.AddChild(new TCThisOrdinalReader() { Attribute = new TestCase() { Name = "This [Ordinal]", Desc = "FactoryReader" } });
            module.AddChild(new TCMoveToAttributeOrdinalReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Ordinal)", Desc = "FactoryReader" } });
            module.AddChild(new TCMoveToFirstAttributeReader() { Attribute = new TestCase() { Name = "MoveToFirstAttribute()", Desc = "FactoryReader" } });
            module.AddChild(new TCMoveToNextAttributeReader() { Attribute = new TestCase() { Name = "MoveToNextAttribute()", Desc = "FactoryReader" } });
            module.AddChild(new TCAttributeTestReader() { Attribute = new TestCase() { Name = "Attribute Test when NodeType != Attributes", Desc = "FactoryReader" } });
            module.AddChild(new TCAttributeXmlDeclarationReader() { Attribute = new TestCase() { Name = "Attributes test on XmlDeclaration", Desc = "FactoryReader" } });
            module.AddChild(new TCXmlnsReader() { Attribute = new TestCase() { Name = "xmlns as local name", Desc = "FactoryReader" } });
            module.AddChild(new TCXmlnsPrefixReader() { Attribute = new TestCase() { Name = "bounded namespace to xmlns prefix", Desc = "FactoryReader" } });
            module.AddChild(new TCReadStateReader() { Attribute = new TestCase() { Name = "ReadState", Desc = "FactoryReader" } });
            module.AddChild(new TCReadInnerXmlReader() { Attribute = new TestCase() { Name = "ReadInnerXml", Desc = "FactoryReader" } });
            module.AddChild(new TCMoveToContentReader() { Attribute = new TestCase() { Name = "MoveToContent", Desc = "FactoryReader" } });
            module.AddChild(new TCIsStartElementReader() { Attribute = new TestCase() { Name = "IsStartElement", Desc = "FactoryReader" } });
            module.AddChild(new TCReadStartElementReader() { Attribute = new TestCase() { Name = "ReadStartElement", Desc = "FactoryReader" } });
            module.AddChild(new TCReadEndElementReader() { Attribute = new TestCase() { Name = "ReadEndElement", Desc = "FactoryReader" } });
            module.AddChild(new TCResolveEntityReader() { Attribute = new TestCase() { Name = "ResolveEntity and ReadAttributeValue", Desc = "FactoryReader" } });
            module.AddChild(new TCReadAttributeValueReader() { Attribute = new TestCase() { Name = "ReadAttributeValue", Desc = "FactoryReader" } });
            module.AddChild(new TCReadReader() { Attribute = new TestCase() { Name = "Read", Desc = "FactoryReader" } });
            module.AddChild(new TCMoveToElementReader() { Attribute = new TestCase() { Name = "MoveToElement", Desc = "FactoryReader" } });
            module.AddChild(new TCDisposeReader() { Attribute = new TestCase() { Name = "Dispose", Desc = "FactoryReader" } });
            module.AddChild(new TCBufferBoundariesReader() { Attribute = new TestCase() { Name = "Buffer Boundaries", Desc = "FactoryReader" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "BeforeRead", Desc = "BeforeRead" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterCloseInTheMiddle", Desc = "AfterCloseInTheMiddle" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterClose", Desc = "AfterClose" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterReadIsFalse", Desc = "AfterReadIsFalse" } });
            module.AddChild(new TCReadSubtreeReader() { Attribute = new TestCase() { Name = "Read Subtree", Desc = "FactoryReader" } });
            module.AddChild(new TCReadToDescendantReader() { Attribute = new TestCase() { Name = "ReadToDescendant", Desc = "FactoryReader" } });
            module.AddChild(new TCReadToNextSiblingReader() { Attribute = new TestCase() { Name = "ReadToNextSibling", Desc = "FactoryReader" } });
            module.AddChild(new TCReadValueReader() { Attribute = new TestCase() { Name = "ReadValue", Desc = "FactoryReader" } });
            module.AddChild(new TCReadContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadContentAsBase64", Desc = "FactoryReader" } });
            module.AddChild(new TCReadElementContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadElementContentAsBase64", Desc = "FactoryReader" } });
            module.AddChild(new TCReadContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadContentAsBinHex", Desc = "FactoryReader" } });
            module.AddChild(new TCReadElementContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadElementContentAsBinHex", Desc = "FactoryReader" } });
            module.AddChild(new TCReadToFollowingReader() { Attribute = new TestCase() { Name = "ReadToFollowing", Desc = "FactoryReader" } });
            module.AddChild(new TCNormalization() { Attribute = new TestCase() { Name = "FactoryReader Normalization", Desc = "FactoryReader" } });

            return module;
        }
    }
}
