// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
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

            //for class EventsTests
            {
                module.AddChild(new EventsTests() { Attribute = new TestCaseAttribute() { Name = "Events", Desc = "XLinq Events Tests" } });
            }
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }
        public partial class EventsTests : XLinqTestCase
        {
            // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests
            // Test Case
            public override void AddChildren()
            {
                this.AddChild(new EventsAddBeforeSelf() { Attribute = new TestCaseAttribute() { Name = "XNode.AddBeforeSelf()" } });
                this.AddChild(new EventsAddAfterSelf() { Attribute = new TestCaseAttribute() { Name = "XNode.AddAfterSelf()" } });
                this.AddChild(new EventsAddFirst() { Attribute = new TestCaseAttribute() { Name = "XContainer.AddFirst()" } });
                this.AddChild(new EventsAdd() { Attribute = new TestCaseAttribute() { Name = "XContainer.Add()" } });
                this.AddChild(new EventsXElementName() { Attribute = new TestCaseAttribute() { Name = "XObject.Name" } });
                this.AddChild(new EventsSpecialCases() { Attribute = new TestCaseAttribute() { Name = "SpecialCases" } });
                this.AddChild(new EventsRemove() { Attribute = new TestCaseAttribute() { Name = "XNode.Remove()" } });
                this.AddChild(new EventsRemoveNodes() { Attribute = new TestCaseAttribute() { Name = "XContainer.RemoveNodes()" } });
                this.AddChild(new EventsRemoveAll() { Attribute = new TestCaseAttribute() { Name = "XElement.RemoveAll()" } });
                this.AddChild(new EventsReplaceWith() { Attribute = new TestCaseAttribute() { Name = "XNode.ReplaceWith()" } });
                this.AddChild(new EventsReplaceNodes() { Attribute = new TestCaseAttribute() { Name = "XContainer.ReplaceNodes()" } });
                this.AddChild(new EvensReplaceAttributes() { Attribute = new TestCaseAttribute() { Name = "XElement.ReplaceAttributes()" } });
                this.AddChild(new EventsReplaceAll() { Attribute = new TestCaseAttribute() { Name = "XElement.ReplaceAll()" } });
                this.AddChild(new EventsXObjectValue() { Attribute = new TestCaseAttribute() { Name = "XObjects.Value" } });
                this.AddChild(new EventsXElementValue() { Attribute = new TestCaseAttribute() { Name = "XElement.Value" } });
                this.AddChild(new EventsXAttributeValue() { Attribute = new TestCaseAttribute() { Name = "XAttribute.Value" } });
                this.AddChild(new EventsXAttributeSetValue() { Attribute = new TestCaseAttribute() { Name = "XAttribute.SetValue" } });
                this.AddChild(new EventsXElementSetValue() { Attribute = new TestCaseAttribute() { Name = "XElement.SetValue" } });
                this.AddChild(new EventsXElementSetAttributeValue() { Attribute = new TestCaseAttribute() { Name = "XElement.SetAttributeValue" } });
                this.AddChild(new EventsXElementSetElementValue() { Attribute = new TestCaseAttribute() { Name = "XElement.SetElementValue" } });
            }
            public partial class EventsBase : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsBase
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsAddBeforeSelf : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsAddBeforeSelf
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(AddNull) { Attribute = new VariationAttribute("XElement - Add null") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes1) { Attribute = new VariationAttribute("XElement - Working on the text nodes 1.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes2) { Attribute = new VariationAttribute("XElement - Working on the text nodes 2.") { Priority = 1 } });
                }
            }
            public partial class EventsAddAfterSelf : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsAddAfterSelf
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(AddNull) { Attribute = new VariationAttribute("XElement-Add null") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes1) { Attribute = new VariationAttribute("XElement - Working on the text nodes 1.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes2) { Attribute = new VariationAttribute("XElement - Working on the text nodes 2.") { Priority = 1 } });
                }
            }
            public partial class EventsAddFirst : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsAddFirst
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(AddNull) { Attribute = new VariationAttribute("XElement - Add null") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes1) { Attribute = new VariationAttribute("XElement - Working on the text nodes 1.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes2) { Attribute = new VariationAttribute("XElement - Working on the text nodes 2.") { Priority = 1 } });
                    this.AddChild(new TestVariation(StringContent) { Attribute = new VariationAttribute("XElement - Change content in the pre-event handler") { Priority = 1 } });
                    this.AddChild(new TestVariation(ParentedXNode) { Attribute = new VariationAttribute("XElement - Change xnode's parent in the pre-event handler") { Priority = 1 } });
                }
            }
            public partial class EventsAdd : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsAdd
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(XAttributeAddAtDeepLevel) { Attribute = new VariationAttribute("XAttribute - Add at each level, nested elements") { Priority = 1 } });
                    this.AddChild(new TestVariation(XElementAddAtDeepLevel) { Attribute = new VariationAttribute("XElement - Add at each level, nested elements") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkTextNodes) { Attribute = new VariationAttribute("XElement - Text node incarnation.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes1) { Attribute = new VariationAttribute("XElement - Working on the text nodes 1.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes2) { Attribute = new VariationAttribute("XElement - Working on the text nodes 2.") { Priority = 1 } });
                    this.AddChild(new TestVariation(StringContent) { Attribute = new VariationAttribute("XElement - Change content in the pre-event handler") { Priority = 1 } });
                    this.AddChild(new TestVariation(ParentedXNode) { Attribute = new VariationAttribute("XElement - Change xnode's parent in the pre-event handler") { Priority = 1 } });
                    this.AddChild(new TestVariation(ParentedAttribute) { Attribute = new VariationAttribute("XElement - Change attribute's parent in the pre-event handler") { Priority = 1 } });
                }
            }
            public partial class EventsXElementName : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXElementName
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(PIVariation) { Attribute = new VariationAttribute("XProcessingInstruction - Name") { Priority = 0 } });
                    this.AddChild(new TestVariation(DocTypeVariation) { Attribute = new VariationAttribute("XDocumentType - Name") { Priority = 0 } });
                }
            }
            public partial class EventsSpecialCases : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsSpecialCases
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(XElementRemoveNullEventListner) { Attribute = new VariationAttribute("Adding, Removing null listeners") { Priority = 1 } });
                    this.AddChild(new TestVariation(XElementRemoveBothEventListners) { Attribute = new VariationAttribute("Remove both event listeners") { Priority = 1 } });
                    this.AddChild(new TestVariation(AddListnerInPreEvent) { Attribute = new VariationAttribute("Add Changed listner in pre-event") { Priority = 1 } });
                    this.AddChild(new TestVariation(XElementAddRemoveEventListners) { Attribute = new VariationAttribute("Add and remove event listners") { Priority = 1 } });
                    this.AddChild(new TestVariation(XElementAttachAtEachLevel) { Attribute = new VariationAttribute("Attach listners at each level, nested elements") { Priority = 1 } });
                    this.AddChild(new TestVariation(XElementPreException) { Attribute = new VariationAttribute("Exception in PRE Event Handler") { Priority = 2 } });
                    this.AddChild(new TestVariation(XElementPostException) { Attribute = new VariationAttribute("Exception in POST Event Handler") { Priority = 2 } });
                }
            }
            public partial class EventsRemove : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsRemove
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(WorkOnTextNodes1) { Attribute = new VariationAttribute("XElement - Working on the text nodes 1.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes2) { Attribute = new VariationAttribute("XElement - Working on the text nodes 2.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes3) { Attribute = new VariationAttribute("XElement - Working on the text nodes 3.") { Priority = 1 } });
                    this.AddChild(new TestVariation(WorkOnTextNodes4) { Attribute = new VariationAttribute("XElement - Working on the text nodes 4.") { Priority = 1 } });
                    this.AddChild(new TestVariation(XAttributeRemoveOneByOne) { Attribute = new VariationAttribute("XAttribute - Remove one attribute at a time") { Priority = 1 } });
                    this.AddChild(new TestVariation(XElementRemoveOneByOne) { Attribute = new VariationAttribute("XElement - Remove one element (with children) at a time") { Priority = 1 } });
                    this.AddChild(new TestVariation(XAttributeRemoveSeq) { Attribute = new VariationAttribute("Remove Sequence - IEnumerable<XAttribute>") { Priority = 1 } });
                    this.AddChild(new TestVariation(XElementRemoveSeq) { Attribute = new VariationAttribute("Remove Sequence - IEnumerable<XElement>") { Priority = 1 } });
                    this.AddChild(new TestVariation(ParentedXNode) { Attribute = new VariationAttribute("XElement - Change xnode's parent in the pre-event handler") { Priority = 1 } });
                    this.AddChild(new TestVariation(ParentedAttribute) { Attribute = new VariationAttribute("XElement - Change attribute's parent in the pre-event handler") { Priority = 1 } });
                }
            }
            public partial class EventsRemoveNodes : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsRemoveNodes
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(RemoveNodesBug) { Attribute = new VariationAttribute("XElement - empty element special case <A></A>") { Priority = 1 } });
                    this.AddChild(new TestVariation(ChangeContentBeforeRemove) { Attribute = new VariationAttribute("XElement - Change node value in the pre-event handler") { Priority = 1 } });
                    this.AddChild(new TestVariation(RemoveNodeInPreEvent) { Attribute = new VariationAttribute("XElement - Change nodes in the pre-event handler") { Priority = 1 } });
                    this.AddChild(new TestVariation(RemoveAttributeInPreEvent) { Attribute = new VariationAttribute("XElement - Change attributes in the pre-event handler") { Priority = 1 } });
                }
            }
            public partial class EventsRemoveAll : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsRemoveAll
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsReplaceWith : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsReplaceWith
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsReplaceNodes : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsReplaceNodes
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ReplaceNodes) { Attribute = new VariationAttribute("XElement - Replace Nodes") { Priority = 1 } });
                    this.AddChild(new TestVariation(ReplaceWithIEnum) { Attribute = new VariationAttribute("XElement - Replace with IEnumerable") { Priority = 1 } });
                }
            }
            public partial class EvensReplaceAttributes : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EvensReplaceAttributes
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsReplaceAll : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsReplaceAll
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(ElementWithAttributes) { Attribute = new VariationAttribute("Element with attributes, with Element with attributes") { Priority = 1 } });
                }
            }
            public partial class EventsXObjectValue : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXObjectValue
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(XTextValue) { Attribute = new VariationAttribute("XText - change value") { Priority = 0 } });
                    this.AddChild(new TestVariation(XCDataValue) { Attribute = new VariationAttribute("XCData - change value") { Priority = 0 } });
                    this.AddChild(new TestVariation(XCommentValue) { Attribute = new VariationAttribute("XComment - change value") { Priority = 0 } });
                    this.AddChild(new TestVariation(XPIValue) { Attribute = new VariationAttribute("XProcessingInstruction - change value") { Priority = 0 } });
                    this.AddChild(new TestVariation(XDocTypePublicID) { Attribute = new VariationAttribute("XDocumentType - change public Id") { Priority = 0 } });
                    this.AddChild(new TestVariation(XDocTypeSystemID) { Attribute = new VariationAttribute("XDocumentType - change system Id") { Priority = 0 } });
                    this.AddChild(new TestVariation(XDocTypeInternalSubset) { Attribute = new VariationAttribute("XDocumentType - change internal subset") { Priority = 0 } });
                }
            }
            public partial class EventsXElementValue : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXElementValue
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsXAttributeValue : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXAttributeValue
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsXAttributeSetValue : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXAttributeSetValue
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsXElementSetValue : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXElementSetValue
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsXElementSetAttributeValue : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXElementSetAttributeValue
                // Test Case
                public override void AddChildren()
                {
                }
            }
            public partial class EventsXElementSetElementValue : EventsBase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+EventsTests+EventsXElementSetElementValue
                // Test Case
                public override void AddChildren()
                {
                }
            }
        }
    }
}
