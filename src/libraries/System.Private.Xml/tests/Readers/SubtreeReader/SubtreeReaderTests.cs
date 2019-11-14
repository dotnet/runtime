// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class SubtreeReaderTest : CGenericTestModule
    {
        [Theory]
        [XmlTests(nameof(Create))]
        public void RunTests(XunitTestCase testCase)
        {
            testCase.Run();
        }

        public static CTestModule Create()
        {
            var module = new SubtreeReaderTest();

            module.Init(null);
            module.AddChild(new TCInvalidXMLReader() { Attribute = new TestCase() { Name = "InvalidXML", Desc = "SubtreeReader" } });
            module.AddChild(new TCErrorConditionReader() { Attribute = new TestCase() { Name = "ErrorCondition", Desc = "SubtreeReader" } });
            module.AddChild(new TCDepthReader() { Attribute = new TestCase() { Name = "Depth", Desc = "SubtreeReader" } });
            module.AddChild(new TCNamespaceReader() { Attribute = new TestCase() { Name = "Namespace", Desc = "SubtreeReader" } });
            module.AddChild(new TCLookupNamespaceReader() { Attribute = new TestCase() { Name = "LookupNamespace", Desc = "SubtreeReader" } });
            module.AddChild(new TCIsEmptyElementReader() { Attribute = new TestCase() { Name = "IsEmptyElement", Desc = "SubtreeReader" } });
            module.AddChild(new TCXmlSpaceReader() { Attribute = new TestCase() { Name = "XmlSpace", Desc = "SubtreeReader" } });
            module.AddChild(new TCXmlLangReader() { Attribute = new TestCase() { Name = "XmlLang", Desc = "SubtreeReader" } });
            module.AddChild(new TCSkipReader() { Attribute = new TestCase() { Name = "Skip", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadOuterXmlReader() { Attribute = new TestCase() { Name = "ReadOuterXml", Desc = "SubtreeReader" } });
            module.AddChild(new TCAttributeAccessReader() { Attribute = new TestCase() { Name = "AttributeAccess", Desc = "SubtreeReader" } });
            module.AddChild(new TCThisNameReader() { Attribute = new TestCase() { Name = "This(Name) and This(Name, Namespace)", Desc = "SubtreeReader" } });
            module.AddChild(new TCMoveToAttributeReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "SubtreeReader" } });
            module.AddChild(new TCGetAttributeOrdinalReader() { Attribute = new TestCase() { Name = "GetAttribute (Ordinal)", Desc = "SubtreeReader" } });
            module.AddChild(new TCGetAttributeNameReader() { Attribute = new TestCase() { Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "SubtreeReader" } });
            module.AddChild(new TCThisOrdinalReader() { Attribute = new TestCase() { Name = "This [Ordinal]", Desc = "SubtreeReader" } });
            module.AddChild(new TCMoveToAttributeOrdinalReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Ordinal)", Desc = "SubtreeReader" } });
            module.AddChild(new TCMoveToFirstAttributeReader() { Attribute = new TestCase() { Name = "MoveToFirstAttribute()", Desc = "SubtreeReader" } });
            module.AddChild(new TCMoveToNextAttributeReader() { Attribute = new TestCase() { Name = "MoveToNextAttribute()", Desc = "SubtreeReader" } });
            module.AddChild(new TCAttributeTestReader() { Attribute = new TestCase() { Name = "Attribute Test when NodeType != Attributes", Desc = "SubtreeReader" } });
            module.AddChild(new TCXmlnsReader() { Attribute = new TestCase() { Name = "xmlns as local name DCR50345", Desc = "SubtreeReader" } });
            module.AddChild(new TCXmlnsPrefixReader() { Attribute = new TestCase() { Name = "bounded namespace to xmlns prefix DCR50881", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadInnerXmlReader() { Attribute = new TestCase() { Name = "ReadInnerXml", Desc = "SubtreeReader" } });
            module.AddChild(new TCMoveToContentReader() { Attribute = new TestCase() { Name = "MoveToContent", Desc = "SubtreeReader" } });
            module.AddChild(new TCIsStartElementReader() { Attribute = new TestCase() { Name = "IsStartElement", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadStartElementReader() { Attribute = new TestCase() { Name = "ReadStartElement", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadEndElementReader() { Attribute = new TestCase() { Name = "ReadEndElement", Desc = "SubtreeReader" } });
            module.AddChild(new TCResolveEntityReader() { Attribute = new TestCase() { Name = "ResolveEntity and ReadAttributeValue", Desc = "SubtreeReader" } });
            module.AddChild(new TCHasValueReader() { Attribute = new TestCase() { Name = "HasValue", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadAttributeValueReader() { Attribute = new TestCase() { Name = "ReadAttributeValue", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadReader() { Attribute = new TestCase() { Name = "Read", Desc = "SubtreeReader" } });
            module.AddChild(new TCMoveToElementReader() { Attribute = new TestCase() { Name = "MoveToElement", Desc = "SubtreeReader" } });
            module.AddChild(new TCDisposeReader() { Attribute = new TestCase() { Name = "Dispose", Desc = "SubtreeReader" } });
            module.AddChild(new TCBufferBoundariesReader() { Attribute = new TestCase() { Name = "Buffer Boundaries", Desc = "SubtreeReader" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "BeforeRead", Desc = "BeforeRead" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterCloseInTheMiddle", Desc = "AfterCloseInTheMiddle" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterClose", Desc = "AfterClose" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterReadIsFalse", Desc = "AfterReadIsFalse" } });
            module.AddChild(new TCReadSubtreeReader() { Attribute = new TestCase() { Name = "Read Subtree", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadToDescendantReader() { Attribute = new TestCase() { Name = "ReadToDescendant", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadToNextSiblingReader() { Attribute = new TestCase() { Name = "ReadToNextSibling", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadValueReader() { Attribute = new TestCase() { Name = "ReadValue", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadContentAsBase64", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadElementContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadElementContentAsBase64", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadContentAsBinHex", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadElementContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadElementContentAsBinHex", Desc = "SubtreeReader" } });
            module.AddChild(new TCReadToFollowingReader() { Attribute = new TestCase() { Name = "ReadToFollowing", Desc = "SubtreeReader" } });

            return module;
        }


    }
}
