// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Text;
using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class CharCheckingReaderTest : CGenericTestModule
    {
        [Theory]
        [XmlTests(nameof(Create))]
        public void RunTests(XunitTestCase testCase)
        {
            testCase.Run();
        }

        public static CTestModule Create()
        {
            var module = new CharCheckingReaderTest();

            module.Init(null);
            module.AddChild(new TCErrorConditionReader() { Attribute = new TestCase() { Name = "ErrorCondition", Desc = "CharCheckingReader" } });
            module.AddChild(new TCDepthReader() { Attribute = new TestCase() { Name = "Depth", Desc = "CharCheckingReader" } });
            module.AddChild(new TCNamespaceReader() { Attribute = new TestCase() { Name = "Namespace", Desc = "CharCheckingReader" } });
            module.AddChild(new TCIsEmptyElementReader() { Attribute = new TestCase() { Name = "IsEmptyElement", Desc = "CharCheckingReader" } });
            module.AddChild(new TCXmlSpaceReader() { Attribute = new TestCase() { Name = "XmlSpace", Desc = "CharCheckingReader" } });
            module.AddChild(new TCXmlLangReader() { Attribute = new TestCase() { Name = "XmlLang", Desc = "CharCheckingReader" } });
            module.AddChild(new TCSkipReader() { Attribute = new TestCase() { Name = "Skip", Desc = "CharCheckingReader" } });
            module.AddChild(new TCInvalidXMLReader() { Attribute = new TestCase() { Name = "InvalidXML", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadOuterXmlReader() { Attribute = new TestCase() { Name = "ReadOuterXml", Desc = "CharCheckingReader" } });
            module.AddChild(new TCAttributeAccessReader() { Attribute = new TestCase() { Name = "AttributeAccess", Desc = "CharCheckingReader" } });
            module.AddChild(new TCThisNameReader() { Attribute = new TestCase() { Name = "This(Name) and This(Name, Namespace)", Desc = "CharCheckingReader" } });
            module.AddChild(new TCMoveToAttributeReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)", Desc = "CharCheckingReader" } });
            module.AddChild(new TCGetAttributeOrdinalReader() { Attribute = new TestCase() { Name = "GetAttribute (Ordinal)", Desc = "CharCheckingReader" } });
            module.AddChild(new TCGetAttributeNameReader() { Attribute = new TestCase() { Name = "GetAttribute(Name) and GetAttribute(Name, Namespace)", Desc = "CharCheckingReader" } });
            module.AddChild(new TCThisOrdinalReader() { Attribute = new TestCase() { Name = "This [Ordinal]", Desc = "CharCheckingReader" } });
            module.AddChild(new TCMoveToAttributeOrdinalReader() { Attribute = new TestCase() { Name = "MoveToAttribute(Ordinal)", Desc = "CharCheckingReader" } });
            module.AddChild(new TCMoveToFirstAttributeReader() { Attribute = new TestCase() { Name = "MoveToFirstAttribute()", Desc = "CharCheckingReader" } });
            module.AddChild(new TCMoveToNextAttributeReader() { Attribute = new TestCase() { Name = "MoveToNextAttribute()", Desc = "CharCheckingReader" } });
            module.AddChild(new TCAttributeTestReader() { Attribute = new TestCase() { Name = "Attribute Test when NodeType != Attributes", Desc = "CharCheckingReader" } });
            module.AddChild(new TCAttributeXmlDeclarationReader() { Attribute = new TestCase() { Name = "Attributes test on XmlDeclaration", Desc = "CharCheckingReader" } });
            module.AddChild(new TCXmlnsReader() { Attribute = new TestCase() { Name = "xmlns as local name", Desc = "CharCheckingReader" } });
            module.AddChild(new TCXmlnsPrefixReader() { Attribute = new TestCase() { Name = "bounded namespace to xmlns prefix", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadInnerXmlReader() { Attribute = new TestCase() { Name = "ReadInnerXml", Desc = "CharCheckingReader" } });
            module.AddChild(new TCMoveToContentReader() { Attribute = new TestCase() { Name = "MoveToContent", Desc = "CharCheckingReader" } });
            module.AddChild(new TCIsStartElementReader() { Attribute = new TestCase() { Name = "IsStartElement", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadStartElementReader() { Attribute = new TestCase() { Name = "ReadStartElement", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadEndElementReader() { Attribute = new TestCase() { Name = "ReadEndElement", Desc = "CharCheckingReader" } });
            module.AddChild(new TCResolveEntityReader() { Attribute = new TestCase() { Name = "ResolveEntity and ReadAttributeValue", Desc = "CharCheckingReader" } });
            module.AddChild(new TCHasValueReader() { Attribute = new TestCase() { Name = "HasValue", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadAttributeValueReader() { Attribute = new TestCase() { Name = "ReadAttributeValue", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadReader() { Attribute = new TestCase() { Name = "Read", Desc = "CharCheckingReader" } });
            module.AddChild(new TCMoveToElementReader() { Attribute = new TestCase() { Name = "MoveToElement", Desc = "CharCheckingReader" } });
            module.AddChild(new TCDisposeReader() { Attribute = new TestCase() { Name = "Dispose", Desc = "CharCheckingReader" } });
            module.AddChild(new TCBufferBoundariesReader() { Attribute = new TestCase() { Name = "Buffer Boundaries", Desc = "CharCheckingReader" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "BeforeRead", Desc = "BeforeRead" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterCloseInTheMiddle", Desc = "AfterCloseInTheMiddle" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterClose", Desc = "AfterClose" } });
            module.AddChild(new TCXmlNodeIntegrityTestFile() { Attribute = new TestCase() { Name = "AfterReadIsFalse", Desc = "AfterReadIsFalse" } });
            module.AddChild(new TCReadSubtreeReader() { Attribute = new TestCase() { Name = "Read Subtree", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadToDescendantReader() { Attribute = new TestCase() { Name = "ReadToDescendant", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadToNextSiblingReader() { Attribute = new TestCase() { Name = "ReadToNextSibling", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadValueReader() { Attribute = new TestCase() { Name = "ReadValue", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadContentAsBase64", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadElementContentAsBase64Reader() { Attribute = new TestCase() { Name = "ReadElementContentAsBase64", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadContentAsBinHex", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadElementContentAsBinHexReader() { Attribute = new TestCase() { Name = "ReadElementContentAsBinHex", Desc = "CharCheckingReader" } });
            module.AddChild(new TCReadToFollowingReader() { Attribute = new TestCase() { Name = "ReadToFollowing", Desc = "CharCheckingReader" } });

            return module;
        }
    }
}
