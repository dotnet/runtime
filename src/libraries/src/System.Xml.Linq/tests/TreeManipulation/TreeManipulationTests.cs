// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class TreeManipulationTests : TestModule
    {
        [Fact]
        [OuterLoop]
        public static void ConstructorXDocument()
        {
            RunTestCase(new ParamsObjectsCreation { Attribute = new TestCaseAttribute { Name = "Constructors with params - XDocument" } });
        }

        [Fact]
        [OuterLoop]
        public static void ConstructorXElementArray()
        {
            RunTestCase(new ParamsObjectsCreationElem { Attribute = new TestCaseAttribute { Name = "Constructors with params - XElement - array", Param = 0 } }); //Param = InputParamStyle.Array
        }

        [Fact]
        [OuterLoop]
        public static void ConstructorXElementIEnumerable()
        {
            RunTestCase(new ParamsObjectsCreationElem { Attribute = new TestCaseAttribute { Name = "Constructors with params - XElement - IEnumerable", Param = 2 } }); //InputParamStyle.IEnumerable
        }

        [Fact]
        [OuterLoop]
        public static void ConstructorXElementNodeArray()
        {
            RunTestCase(new ParamsObjectsCreationElem { Attribute = new TestCaseAttribute { Name = "Constructors with params - XElement - node + array", Param = 1 } }); //InputParamStyle.SingleAndArray
        }

        [Fact]
        [OuterLoop]
        public static void IEnumerableOfXAttributeRemove()
        {
            RunTestCase(new XAttributeEnumRemove { Attribute = new TestCaseAttribute { Name = "IEnumerable<XAttribute>.Remove()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void IEnumerableOfXAttributeRemoveWithRemove()
        {
            RunTestCase(new XAttributeEnumRemove { Attribute = new TestCaseAttribute { Name = "IEnumerable<XAttribute>.Remove() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void IEnumerableOfXNodeRemove()
        {
            RunTestCase(new XNodeSequenceRemove { Attribute = new TestCaseAttribute { Name = "IEnumerable<XNode>.Remove()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void IEnumerableOfXNodeRemoveWithEvents()
        {
            RunTestCase(new XNodeSequenceRemove { Attribute = new TestCaseAttribute { Name = "IEnumerable<XNode>.Remove() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void LoadFromReader()
        {
            RunTestCase(new LoadFromReader { Attribute = new TestCaseAttribute { Name = "Load from Reader" } });
        }

        [Fact]
        [OuterLoop]
        public static void LoadFromStreamSanity()
        {
            RunTestCase(new LoadFromStream { Attribute = new TestCaseAttribute { Name = "Load from Stream - sanity" } });
        }

        [Fact]
        [OuterLoop]
        public static void SaveWithWriter()
        {
            RunTestCase(new SaveWithWriter { Attribute = new TestCaseAttribute { Name = "Save with Writer" } });
        }

        [Fact]
        [OuterLoop]
        public static void SimpleConstructors()
        {
            RunTestCase(new SimpleObjectsCreation { Attribute = new TestCaseAttribute { Name = "Simple constructors" } });
        }

        [Fact]
        [OuterLoop]
        public static void XAttributeRemove()
        {
            RunTestCase(new XAttributeRemove { Attribute = new TestCaseAttribute { Name = "XAttribute.Remove", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XAttributeRemoveWithEvents()
        {
            RunTestCase(new XAttributeRemove { Attribute = new TestCaseAttribute { Name = "XAttribute.Remove with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerAddIntoElement()
        {
            RunTestCase(new XContainerAddIntoElement { Attribute = new TestCaseAttribute { Name = "XContainer.Add1", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerAddIntoDocument()
        {
            RunTestCase(new XContainerAddIntoDocument { Attribute = new TestCaseAttribute { Name = "XContainer.Add2", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerAddFirstInvalidIntoXDocument()
        {
            RunTestCase(new AddFirstInvalidIntoXDocument { Attribute = new TestCaseAttribute { Name = "XContainer.AddFirst", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerAddFirstInvalidIntoXDocumentWithEvents()
        {
            RunTestCase(new AddFirstInvalidIntoXDocument { Attribute = new TestCaseAttribute { Name = "XContainer.AddFirst with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void AddFirstSingeNodeAddIntoElement()
        {
            RunTestCase(new AddFirstSingeNodeAddIntoElement { Attribute = new TestCaseAttribute { Name = "XContainer.AddFirstAddFirstSingeNodeAddIntoElement", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void AddFirstSingeNodeAddIntoElementWithEvents()
        {
            RunTestCase(new AddFirstSingeNodeAddIntoElement { Attribute = new TestCaseAttribute { Name = "XContainer.AddFirstAddFirstSingeNodeAddIntoElement with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void AddFirstAddFirstIntoDocument()
        {
            RunTestCase(new AddFirstAddFirstIntoDocument { Attribute = new TestCaseAttribute { Name = "XContainer.AddFirstAddFirstAddFirstIntoDocument", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void AddFirstAddFirstIntoDocumentWithEvents()
        {
            RunTestCase(new AddFirstAddFirstIntoDocument { Attribute = new TestCaseAttribute { Name = "XContainer.AddFirstAddFirstAddFirstIntoDocument with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerAddIntoElementWithEvents()
        {
            RunTestCase(new XContainerAddIntoElement { Attribute = new TestCaseAttribute { Name = "XContainer.Add with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerAddIntoDocumentWithEvents()
        {
            RunTestCase(new XContainerAddIntoDocument { Attribute = new TestCaseAttribute { Name = "XContainer.Add with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerFirstNode()
        {
            RunTestCase(new FirstNode { Attribute = new TestCaseAttribute { Name = "XContainer.FirstNode", Param = true } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerLastNode()
        {
            RunTestCase(new FirstNode { Attribute = new TestCaseAttribute { Name = "XContainer.LastNode", Param = false } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerNextPreviousNode()
        {
            RunTestCase(new NextNode { Attribute = new TestCaseAttribute { Name = "XContainer.Next/PreviousNode" } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerRemoveNodesOnXElement()
        {
            RunTestCase(new XContainerRemoveNodesOnXElement { Attribute = new TestCaseAttribute { Name = "XContainer.RemoveNodes()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerRemoveNodesOnXElementWithEvents()
        {
            RunTestCase(new XContainerRemoveNodesOnXElement { Attribute = new TestCaseAttribute { Name = "XContainer.RemoveNodes() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerRemoveNodesOnXDocument()
        {
            RunTestCase(new XContainerRemoveNodesOnXDocument { Attribute = new TestCaseAttribute { Name = "XContainer.RemoveNodes()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerRemoveNodesOnXDocumentWithEvents()
        {
            RunTestCase(new XContainerRemoveNodesOnXDocument { Attribute = new TestCaseAttribute { Name = "XContainer.RemoveNodes() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerReplaceNodesOnDocument()
        {
            RunTestCase(new XContainerReplaceNodesOnDocument { Attribute = new TestCaseAttribute { Name = "XContainer.ReplaceNodesOnXDocument()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerReplaceNodesOnDocumentWithEvents()
        {
            RunTestCase(new XContainerReplaceNodesOnDocument { Attribute = new TestCaseAttribute { Name = "XContainer.ReplaceNodesOnXDocument() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerReplaceNodesOnXElement()
        {
            RunTestCase(new XContainerReplaceNodesOnXElement { Attribute = new TestCaseAttribute { Name = "XContainer.ReplaceNodesOnXElement()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XContainerReplaceNodesOnXElementWithEvents()
        {
            RunTestCase(new XContainerReplaceNodesOnXElement { Attribute = new TestCaseAttribute { Name = "XContainer.ReplaceNodesOnXElement() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XElementRemoveAttributes()
        {
            RunTestCase(new RemoveAttributes { Attribute = new TestCaseAttribute { Name = "XElement.RemoveAttributes", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XElementRemoveAttributesWithEvents()
        {
            RunTestCase(new RemoveAttributes { Attribute = new TestCaseAttribute { Name = "XElement.RemoveAttributes with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XElementSetAttributeValue()
        {
            RunTestCase(new XElement_SetAttributeValue { Attribute = new TestCaseAttribute { Name = "XElement.SetAttributeValue()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XElementSetAttributeValueWithEvents()
        {
            RunTestCase(new XElement_SetAttributeValue { Attribute = new TestCaseAttribute { Name = "XElement.SetAttributeValue() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XElementSetElementValue()
        {
            RunTestCase(new XElement_SetElementValue { Attribute = new TestCaseAttribute { Name = "XElement.SetElementValue()", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XElementSetElementValueWithEvents()
        {
            RunTestCase(new XElement_SetElementValue { Attribute = new TestCaseAttribute { Name = "XElement.SetElementValue() with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeAddAfter()
        {
            RunTestCase(new AddNodeAfter { Attribute = new TestCaseAttribute { Name = "XNode.AddAfter", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeAddAfterWithEvents()
        {
            RunTestCase(new AddNodeAfter { Attribute = new TestCaseAttribute { Name = "XNode.AddAfter with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeAddBefore()
        {
            RunTestCase(new AddNodeBefore { Attribute = new TestCaseAttribute { Name = "XNode.AddBefore", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeAddBeforeWithEvents()
        {
            RunTestCase(new AddNodeBefore { Attribute = new TestCaseAttribute { Name = "XNode.AddBefore with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeRemoveNodeMisc()
        {
            RunTestCase(new XNodeRemoveNodeMisc { Attribute = new TestCaseAttribute { Name = "XNode.Remove", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeRemoveNodeMiscWithEvents()
        {
            RunTestCase(new XNodeRemoveNodeMisc { Attribute = new TestCaseAttribute { Name = "XNode.Remove with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeRemoveOnDocument()
        {
            RunTestCase(new XNodeRemoveOnDocument { Attribute = new TestCaseAttribute { Name = "XNode.Remove", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeRemoveOnDocumentWithEvents()
        {
            RunTestCase(new XNodeRemoveOnDocument { Attribute = new TestCaseAttribute { Name = "XNode.Remove with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeRemoveOnElement()
        {
            RunTestCase(new XNodeRemoveOnElement { Attribute = new TestCaseAttribute { Name = "XNode.Remove", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeRemoveOnElementWithEvents()
        {
            RunTestCase(new XNodeRemoveOnElement { Attribute = new TestCaseAttribute { Name = "XNode.Remove with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnDocument1()
        {
            RunTestCase(new XNodeReplaceOnDocument1 { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnDocument1WithEvents()
        {
            RunTestCase(new XNodeReplaceOnDocument1 { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnDocument2()
        {
            RunTestCase(new XNodeReplaceOnDocument2 { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnDocument2WithEvents()
        {
            RunTestCase(new XNodeReplaceOnDocument2 { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnDocument3()
        {
            RunTestCase(new XNodeReplaceOnDocument3 { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnDocument3WithEvents()
        {
            RunTestCase(new XNodeReplaceOnDocument3 { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith with Events", Params = new object[] { true } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnElement()
        {
            RunTestCase(new XNodeReplaceOnElement { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith", Params = new object[] { false } } });
        }

        [Fact]
        [OuterLoop]
        public static void XNodeReplaceOnElementWithEvents()
        {
            RunTestCase(new XNodeReplaceOnElement { Attribute = new TestCaseAttribute { Name = "XNode.ReplaceWith with Events", Params = new object[] { true } } });
        }

        private static void RunTestCase(TestItem testCase)
        {
            var module = new TreeManipulationTests();

            module.Init();
            module.AddChild(testCase);
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }
    }
}
