// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Microsoft.Test.ModuleCore;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests
        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            TestInput.CommandLine = "";
            FunctionalTests module = new FunctionalTests();
            module.Init();

            //for class SDMSamplesTests
            {
                module.AddChild(new SDMSamplesTests() { Attribute = new TestCaseAttribute() { Name = "SDM Samples", Desc = "XLinq SDM Samples Tests" } });
            }
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }

        #region Class
        public partial class SDMSamplesTests : XLinqTestCase
        {
            // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests
            // Test Case
            public override void AddChildren()
            {
                this.AddChild(new SDM_Attribute() { Attribute = new TestCaseAttribute() { Name = "XAttribute" } });
                this.AddChild(new SDM_CDATA() { Attribute = new TestCaseAttribute() { Name = "XCDATA" } });
                this.AddChild(new SDM_Comment() { Attribute = new TestCaseAttribute() { Name = "XComment" } });
                this.AddChild(new SDM_Container() { Attribute = new TestCaseAttribute() { Name = "XContainer" } });
                this.AddChild(new SDM_Document() { Attribute = new TestCaseAttribute() { Name = "XDocument" } });
                this.AddChild(new SDM_Element() { Attribute = new TestCaseAttribute() { Name = "XElement" } });
                this.AddChild(new SDM_LoadSave() { Attribute = new TestCaseAttribute() { Name = "LoadSave" } });
                this.AddChild(new SDM_Misc() { Attribute = new TestCaseAttribute() { Name = "Misc" } });
                this.AddChild(new SDM_Node() { Attribute = new TestCaseAttribute() { Name = "XNode" } });
                this.AddChild(new SDM__PI() { Attribute = new TestCaseAttribute() { Name = "XPI" } });
                this.AddChild(new SDM_XName() { Attribute = new TestCaseAttribute() { Name = "XName" } });
            }
            public partial class SDM_Attribute : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_Attribute
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(AttributeConstructor) { Attribute = new VariationAttribute("AttributeConstructor") });
                    this.AddChild(new TestVariation(AttributeEmptySequence) { Attribute = new VariationAttribute("AttributeEmptySequence") });
                    this.AddChild(new TestVariation(AttributeIsNamespaceDeclaration) { Attribute = new VariationAttribute("AttributeIsNamespaceDeclaration") });
                    this.AddChild(new TestVariation(AttributeParent) { Attribute = new VariationAttribute("AttributeParent") });
                    this.AddChild(new TestVariation(AttributeValue) { Attribute = new VariationAttribute("AttributeValue") });
                    this.AddChild(new TestVariation(AttributeRemove) { Attribute = new VariationAttribute("AttributeRemove") });
                    this.AddChild(new TestVariation(AttributeExplicitToString) { Attribute = new VariationAttribute("AttributeExplicitToString") });
                    this.AddChild(new TestVariation(AttributeExplicitToBoolean) { Attribute = new VariationAttribute("AttributeExplicitToBoolean") });
                    this.AddChild(new TestVariation(AttributeExplicitToInt32) { Attribute = new VariationAttribute("AttributeExplicitToInt32") });
                    this.AddChild(new TestVariation(AttributeExplicitToUInt32) { Attribute = new VariationAttribute("AttributeExplicitToUInt32") });
                    this.AddChild(new TestVariation(AttributeExplicitToInt64) { Attribute = new VariationAttribute("AttributeExplicitToInt64") });
                    this.AddChild(new TestVariation(AttributeExplicitToUInt64) { Attribute = new VariationAttribute("AttributeExplicitToUInt64") });
                    this.AddChild(new TestVariation(AttributeExplicitToFloat) { Attribute = new VariationAttribute("AttributeExplicitToFloat") });
                    this.AddChild(new TestVariation(AttributeExplicitToDouble) { Attribute = new VariationAttribute("AttributeExplicitToDouble") });
                    this.AddChild(new TestVariation(AttributeExplicitToDecimal) { Attribute = new VariationAttribute("AttributeExplicitToDecimal") });
                    this.AddChild(new TestVariation(AttributeExplicitToDateTime) { Attribute = new VariationAttribute("AttributeExplicitToDateTime") });
                    this.AddChild(new TestVariation(AttributeExplicitToTimeSpan) { Attribute = new VariationAttribute("AttributeExplicitToTimeSpan") });
                    this.AddChild(new TestVariation(AttributeExplicitToGuid) { Attribute = new VariationAttribute("AttributeExplicitToGuId") });
                    this.AddChild(new TestVariation(AttributeExplicitToNullables) { Attribute = new VariationAttribute("AttributeExplicitToNullables") });
                }
            }
            public partial class SDM_CDATA : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_CDATA
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(CreateTextSimple) { Attribute = new VariationAttribute("CreateTextSimple") });
                    this.AddChild(new TestVariation(CreateTextFromReader) { Attribute = new VariationAttribute("CreateTextFromReader") });
                    this.AddChild(new TestVariation(TextEquals) { Attribute = new VariationAttribute("TextEquals") });
                    this.AddChild(new TestVariation(DeepEquals) { Attribute = new VariationAttribute("DeepEquals") });
                    this.AddChild(new TestVariation(TextValue) { Attribute = new VariationAttribute("TextValue") });
                    this.AddChild(new TestVariation(TextWriteTo) { Attribute = new VariationAttribute("TextWriteTo") });
                }
            }
            public partial class SDM_Comment : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_Comment
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(CreateCommentSimple) { Attribute = new VariationAttribute("CreateCommentSimple") });
                    this.AddChild(new TestVariation(CreateCommentFromReader) { Attribute = new VariationAttribute("CreateCommentFromReader") });
                    this.AddChild(new TestVariation(CommentEquals) { Attribute = new VariationAttribute("CommentEquals") });
                    this.AddChild(new TestVariation(CommentDeepEquals) { Attribute = new VariationAttribute("Comment DeepEquals") });
                    this.AddChild(new TestVariation(CommentValue) { Attribute = new VariationAttribute("CommentValue") });
                    this.AddChild(new TestVariation(CommentWriteTo) { Attribute = new VariationAttribute("CommentWriteTo") });
                }
            }
            public partial class SDM_Container : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_Container
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ContainerAdd) { Attribute = new VariationAttribute("ContainerAdd") });
                    this.AddChild(new TestVariation(ContainerAddAttributes) { Attribute = new VariationAttribute("ContainerAddAttributes") });
                    this.AddChild(new TestVariation(ContainerAddFirst) { Attribute = new VariationAttribute("ContainerAddFirst") });
                    this.AddChild(new TestVariation(ContainerContent) { Attribute = new VariationAttribute("ContainerContent") });
                    this.AddChild(new TestVariation(ContainerDescendents) { Attribute = new VariationAttribute("ContainerDescendents") });
                    this.AddChild(new TestVariation(ContainerElements) { Attribute = new VariationAttribute("ContainerElements") });
                    this.AddChild(new TestVariation(ContainerReplaceNodes) { Attribute = new VariationAttribute("ContainerReplaceNodes") });
                    this.AddChild(new TestVariation(ContainerAnnotations) { Attribute = new VariationAttribute("ContainerAnnotations") });
                    this.AddChild(new TestVariation(ContainerRemoveTextual) { Attribute = new VariationAttribute("ContainerRemoveTextual") });
                }
            }
            public partial class SDM_Document : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_Document
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(CreateEmptyDocument) { Attribute = new VariationAttribute("CreateEmptyDocument") });
                    this.AddChild(new TestVariation(CreateDocumentWithContent) { Attribute = new VariationAttribute("CreateDocumentWithContent") });
                    this.AddChild(new TestVariation(CreateDocumentCopy) { Attribute = new VariationAttribute("CreateDocumentCopy") });
                    this.AddChild(new TestVariation(DocumentXmlDeclaration) { Attribute = new VariationAttribute("DocumentXmlDeclaration") });
                    this.AddChild(new TestVariation(DocumentRoot) { Attribute = new VariationAttribute("DocumentRoot") });
                    this.AddChild(new TestVariation(DocumentAddString) { Attribute = new VariationAttribute("DocumentAddString") });
                }
            }
            public partial class SDM_Element : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_Element
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(CreateElementSimple) { Attribute = new VariationAttribute("CreateElementSimple") });
                    this.AddChild(new TestVariation(CreateElementWithContent) { Attribute = new VariationAttribute("CreateElementWithContent") });
                    this.AddChild(new TestVariation(CreateElementCopy) { Attribute = new VariationAttribute("CreateElementCopy") });
                    this.AddChild(new TestVariation(CreateElementFromReader) { Attribute = new VariationAttribute("CreateElementFromReader") });
                    this.AddChild(new TestVariation(ElementEmptyElementSequence) { Attribute = new VariationAttribute("ElementEmptyElementSequence") });
                    this.AddChild(new TestVariation(ElementHasAttributesAndElements) { Attribute = new VariationAttribute("ElementHasAttributesAndElements") });
                    this.AddChild(new TestVariation(ElementIsEmpty) { Attribute = new VariationAttribute("ElementIsEmpty") });
                    this.AddChild(new TestVariation(ElementValue) { Attribute = new VariationAttribute("ElementValue") });
                    this.AddChild(new TestVariation(ElementExplicitToString) { Attribute = new VariationAttribute("ElementExplicitToString") });
                    this.AddChild(new TestVariation(ElementExplicitToBoolean) { Attribute = new VariationAttribute("ElementExplicitToBoolean") });
                    this.AddChild(new TestVariation(ElementExplicitToInt32) { Attribute = new VariationAttribute("ElementExplicitToInt32") });
                    this.AddChild(new TestVariation(ElementExplicitToUInt32) { Attribute = new VariationAttribute("ElementExplicitToUInt32") });
                    this.AddChild(new TestVariation(ElementExplicitToInt64) { Attribute = new VariationAttribute("ElementExplicitToInt64") });
                    this.AddChild(new TestVariation(ElementExplicitToUInt64) { Attribute = new VariationAttribute("ElementExplicitToUInt64") });
                    this.AddChild(new TestVariation(ElementExplicitToFloat) { Attribute = new VariationAttribute("ElementExplicitToFloat") });
                    this.AddChild(new TestVariation(ElementExplicitToDouble) { Attribute = new VariationAttribute("ElementExplicitToDouble") });
                    this.AddChild(new TestVariation(ElementExplicitToDecimal) { Attribute = new VariationAttribute("ElementExplicitToDecimal") });
                    this.AddChild(new TestVariation(ElementExplicitToDateTime) { Attribute = new VariationAttribute("ElementExplicitToDateTime") });
                    this.AddChild(new TestVariation(ElementExplicitToTimeSpan) { Attribute = new VariationAttribute("ElementExplicitToTimeSpan") });
                    this.AddChild(new TestVariation(ElementExplicitToGuid) { Attribute = new VariationAttribute("ElementExplicitToGuId") });
                    this.AddChild(new TestVariation(ElementExplicitToNullables) { Attribute = new VariationAttribute("ElementExplicitToNullables") });
                    this.AddChild(new TestVariation(ElementAncestors) { Attribute = new VariationAttribute("ElementAncestors") });
                    this.AddChild(new TestVariation(ElementDescendents) { Attribute = new VariationAttribute("ElementDescendents") });
                    this.AddChild(new TestVariation(ElementAttributes) { Attribute = new VariationAttribute("ElementAttributes") });
                    this.AddChild(new TestVariation(ElementRemove) { Attribute = new VariationAttribute("ElementRemove") });
                    this.AddChild(new TestVariation(ElementSetElementValue) { Attribute = new VariationAttribute("ElementSetElementValue") });
                    this.AddChild(new TestVariation(ElementGetDefaultNamespace) { Attribute = new VariationAttribute("ElementGetDefaultNamespace") });
                    this.AddChild(new TestVariation(ElementGetNamespaceOfPrefix) { Attribute = new VariationAttribute("ElementGetNamespaceOfPrefix") });
                    this.AddChild(new TestVariation(ElementGetPrefixOfNamespace) { Attribute = new VariationAttribute("ElementGetPrefixOfNamespace") });
                    this.AddChild(new TestVariation(ElementWithXmlnsAttribute) { Attribute = new VariationAttribute("ElementWithXmlnsAttribute") });
                    this.AddChild(new TestVariation(ElementEquality) { Attribute = new VariationAttribute("ElementEquality") });
                    this.AddChild(new TestVariation(ElementAppendedChildIsIterated) { Attribute = new VariationAttribute("ElementAppendedChildIsIterated") });
                }
            }
            public partial class SDM_LoadSave : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_LoadSave
                // Test Case
                public override void AddChildren()
                {

                    this.AddChild(new TestVariation(DocumentLoadFromXmlReader) { Attribute = new VariationAttribute("DocumentLoadFromXmlReader") });
                    this.AddChild(new TestVariation(DocumentSaveToXmlWriter) { Attribute = new VariationAttribute("DocumentSaveToXmlWriter") });
                    this.AddChild(new TestVariation(DocumentWriteTo) { Attribute = new VariationAttribute("DocumentWriteTo") });
                    this.AddChild(new TestVariation(ElementLoadFromXmlReader) { Attribute = new VariationAttribute("ElementLoadFromXmlReader") });
                }
            }
            public partial class SDM_Misc : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_Misc
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(NodeTypes) { Attribute = new VariationAttribute("NodeTypes") });
                }
            }
            public partial class SDM_Node : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_Node
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(NodeParent) { Attribute = new VariationAttribute("NodeParent") });
                    this.AddChild(new TestVariation(NodeReadFrom) { Attribute = new VariationAttribute("NodeReadFrom") });
                    this.AddChild(new TestVariation(NodeNoParentAddRemove) { Attribute = new VariationAttribute("NodeNoParentAddRemove") });
                    this.AddChild(new TestVariation(NodeAddAfterSelf) { Attribute = new VariationAttribute("NodeAddAfterSelf") });
                    this.AddChild(new TestVariation(NodeAddBeforeSelf) { Attribute = new VariationAttribute("NodeAddBeforeSelf") });
                    this.AddChild(new TestVariation(NodeRemove) { Attribute = new VariationAttribute("NodeRemove") });
                    this.AddChild(new TestVariation(NodeAllContentBeforeSelf) { Attribute = new VariationAttribute("NodeAllContentBeforeSelf") });
                    this.AddChild(new TestVariation(NodeAllContentAfterSelf) { Attribute = new VariationAttribute("NodeAllContentAfterSelf") });
                    this.AddChild(new TestVariation(NodeContentBeforeSelf) { Attribute = new VariationAttribute("NodeContentBeforeSelf") });
                    this.AddChild(new TestVariation(NodeContentAfterSelf) { Attribute = new VariationAttribute("NodeContentAfterSelf") });
                    this.AddChild(new TestVariation(NodeElementsBeforeSelf) { Attribute = new VariationAttribute("NodeElementsBeforeSelf") });
                    this.AddChild(new TestVariation(NodeElementsAfterSelf) { Attribute = new VariationAttribute("NodeElementsAfterSelf") });
                    this.AddChild(new TestVariation(NodeDocument) { Attribute = new VariationAttribute("NodeDocument") });
                }
            }
            public partial class SDM__PI : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM__PI
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(CreateProcessingInstructionSimple) { Attribute = new VariationAttribute("CreateProcessingInstructionSimple") });
                    this.AddChild(new TestVariation(CreateProcessingInstructionFromReader) { Attribute = new VariationAttribute("CreateProcessingInstructionFromReader") });
                    this.AddChild(new TestVariation(ProcessingInstructionEquals) { Attribute = new VariationAttribute("ProcessingInstructionEquals") });
                    this.AddChild(new TestVariation(ProcessingInstructionValues) { Attribute = new VariationAttribute("ProcessingInstructionValues") });
                    this.AddChild(new TestVariation(ProcessingInstructionWriteTo) { Attribute = new VariationAttribute("ProcessingInstructionWriteTo") });
                }
            }
            public partial class SDM_XName : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+SDMSamplesTests+SDM_XName
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(NameGetInvalid) { Attribute = new VariationAttribute("NameGetInvalId") });
                    this.AddChild(new TestVariation(NameOperators) { Attribute = new VariationAttribute("NameOperators") });
                    this.AddChild(new TestVariation(NamespaceGetNull) { Attribute = new VariationAttribute("NamespaceGetNull") });
                    this.AddChild(new TestVariation(NamespaceOperators) { Attribute = new VariationAttribute("NamespaceOperators") });
                }
            }
        }
        #endregion
    }
}
