// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadSubtree : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadSubtree
        // Test Case
        public override void AddChildren()
        {
            // for function ReadSubtreeWorksOnlyOnElementNode
            {
                this.AddChild(new CVariation(ReadSubtreeWorksOnlyOnElementNode) { Attribute = new Variation("ReadSubtree only works on Element Node") });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test depth=1") { Params = new object[] { "elem1", "", "ELEMENT", "elem5", "", "ELEMENT" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test empty element before root") { Params = new object[] { "elem6", "", "ELEMENT", "root", "", "ENDELEMENT" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test PI after element") { Params = new object[] { "elempi", "", "ELEMENT", "pi", "target", "PROCESSINGINSTRUCTION" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test on Root") { Params = new object[] { "root", "", "ELEMENT", "", "", "NONE" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test depth=3") { Params = new object[] { "x:elem3", "", "ELEMENT", "elem2", "", "ENDELEMENT" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test depth=4") { Params = new object[] { "elem4", "", "ELEMENT", "x:elem3", "", "ENDELEMENT" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test empty element") { Params = new object[] { "elem5", "", "ELEMENT", "elem6", "", "ELEMENT" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test depth=2") { Params = new object[] { "elem2", "", "ELEMENT", "elem1", "", "ENDELEMENT" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("ReadSubtree Test Comment after element") { Params = new object[] { "elem", "", "ELEMENT", "", "Comment", "COMMENT" }, Pri = 0 } });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read with entities") { Pri = 1 } });
            }


            // for function v4
            {
                this.AddChild(new CVariation(v4) { Attribute = new Variation("Inner XML on Subtree reader") { Pri = 1 } });
            }


            // for function v5
            {
                this.AddChild(new CVariation(v5) { Attribute = new Variation("Outer XML on Subtree reader") { Pri = 1 } });
            }


            // for function v7
            {
                this.AddChild(new CVariation(v7) { Attribute = new Variation("Close on inner reader with CloseInput should not close the outer reader") { Params = new object[] { "false" }, Pri = 1 } });
                this.AddChild(new CVariation(v7) { Attribute = new Variation("Close on inner reader with CloseInput should not close the outer reader") { Params = new object[] { "true" }, Pri = 1 } });
            }


            // for function v8
            {
                this.AddChild(new CVariation(v8) { Attribute = new Variation("Nested Subtree reader calls") { Pri = 2 } });
            }


            // for function v100
            {
                this.AddChild(new CVariation(v100) { Attribute = new Variation("ReadSubtree for element depth more than 4K chars") { Pri = 2 } });
            }


            // for function SubtreeReaderCanDealWithMultipleNamespaces
            {
                this.AddChild(new CVariation(SubtreeReaderCanDealWithMultipleNamespaces) { Attribute = new Variation("Multiple Namespaces on Subtree reader") { Pri = 1 } });
            }


            // for function SubtreeReaderReadsProperlyNodeTypeOfAttributes
            {
                this.AddChild(new CVariation(SubtreeReaderReadsProperlyNodeTypeOfAttributes) { Attribute = new Variation("Subtree Reader caches the NodeType and reports node type of Attribute on subsequent reads.") { Pri = 1 } });
            }


            // for function XmlSubtreeReaderDoesntDuplicateLocalNames
            {
                this.AddChild(new CVariation(XmlSubtreeReaderDoesntDuplicateLocalNames) { Attribute = new Variation("XmlSubtreeReader add duplicate namespace declaration") });
            }


            // for function XmlSubtreeReaderDoesntAddMultipleNamespaceDeclarations
            {
                this.AddChild(new CVariation(XmlSubtreeReaderDoesntAddMultipleNamespaceDeclarations) { Attribute = new Variation("XmlSubtreeReader adds duplicate namespace declaration") });
            }


            // for function XmlReaderDisposeDoesntDisposeMainReader
            {
                this.AddChild(new CVariation(XmlReaderDisposeDoesntDisposeMainReader) { Attribute = new Variation("XmlSubtreeReader.Dispose disposes the main reader") });
            }


            // for function XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute
            {
                this.AddChild(new CVariation(XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute) { Attribute = new Variation("0. XmlReader.Name inconsistent when reading namespace node attribute") { Param = 0 } });
                this.AddChild(new CVariation(XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute) { Attribute = new Variation("2. XmlReader.Name inconsistent when reading namespace node attribute") { Param = 2 } });
                this.AddChild(new CVariation(XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute) { Attribute = new Variation("1. XmlReader.Name inconsistent when reading namespace node attribute") { Param = 1 } });
                this.AddChild(new CVariation(XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute) { Attribute = new Variation("3. XmlReader.Name inconsistent when reading namespace node attribute") { Param = 3 } });
                this.AddChild(new CVariation(XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute) { Attribute = new Variation("4. XmlReader.Name inconsistent when reading namespace node attribute") { Param = 4 } });
                this.AddChild(new CVariation(XmlReaderNameIsConsistentWhenReadingNamespaceNodeAttribute) { Attribute = new Variation("5. XmlReader.Name inconsistent when reading namespace node attribute") { Param = 5 } });
            }


            // for function IndexingMethodsWorksProperly
            {
                this.AddChild(new CVariation(IndexingMethodsWorksProperly) { Attribute = new Variation("Indexing methods cause infinite recursion & stack overflow") });
            }


            // for function DisposingSubtreeReaderThatIsInErrorStateWorksProperly
            {
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("2. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 2 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("13. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 13 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("16. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 16 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("17. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 17 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("18. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 18 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("19. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 19 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("20. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 20 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("21. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 21 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("22. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 22 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("23. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 23 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("24. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 24 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("6. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 6 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("12. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 12 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("15. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 15 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("3. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 3 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("5. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 5 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("1. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 1 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("7. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 7 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("8. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 8 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("9. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 9 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("10. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 10 } });
                this.AddChild(new CVariation(DisposingSubtreeReaderThatIsInErrorStateWorksProperly) { Attribute = new Variation("11. Close on a subtree reader that is in error state doesn't get it into in infinite loop") { Param = 11 } });
            }


            // for function v101
            {
                this.AddChild(new CVariation(v101) { Attribute = new Variation("SubtreeReader has empty namespace") });
            }


            // for function v102
            {
                this.AddChild(new CVariation(v102) { Attribute = new Variation("ReadValueChunk on an xmlns attribute that has been added by the subtree reader") });
            }
        }
    }
}
