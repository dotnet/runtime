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
        // Test Module
        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            TestInput.CommandLine = "";
            FunctionalTests module = new FunctionalTests();
            module.Init();

            module.AddChild(new MiscTests() { Attribute = new TestCaseAttribute() { Name = "Misc", Desc = "XLinq Misc. Tests" } });
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }
        public partial class MiscTests : XLinqTestCase
        {
            // Type is CoreXml.Test.XLinq.FunctionalTests+MiscTests
            // Test Case
            public override void AddChildren()
            {
                this.AddChild(new Annotations() { Attribute = new TestCaseAttribute() { Name = "Annotations" } });
                this.AddChild(new RegressionTests() { Attribute = new TestCaseAttribute() { Name = "RegressionTests" } });
                this.AddChild(new PrefixImprovements() { Attribute = new TestCaseAttribute() { Name = "Prefix improvements :: Find correct namespace prefix. in case of redefinition conflict." } });
                this.AddChild(new XHashtableAPI() { Attribute = new TestCaseAttribute() { Name = "XHashtable API" } });
                this.AddChild(new XmlErrata4() { Attribute = new TestCaseAttribute() { Name = "Xml Errata 4" } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - expanded name", Param = GetNameType.ExpandedName } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - expanded name (From string)", Param = GetNameType.FromString } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - two param Get", Param = GetNameType.TwoParamGet } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - XNamespace + string", Param = GetNameType.XNamespacePlusOperator } });
            }
            public partial class Annotations : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+MiscTests+Annotations
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(Annotations_1) { Attribute = new VariationAttribute("Add one string annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_2) { Attribute = new VariationAttribute("Add int annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_3) { Attribute = new VariationAttribute("Add int and string annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_4) { Attribute = new VariationAttribute("Add, remove and get annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_5) { Attribute = new VariationAttribute("Remove annotation without adding") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_6) { Attribute = new VariationAttribute("Add int annotation, remove string annotation, get int annotation") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_7) { Attribute = new VariationAttribute("Add 2 string annotations, get both of them") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_8) { Attribute = new VariationAttribute("Remove annotation twice") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_9) { Attribute = new VariationAttribute("Add generic annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_10) { Attribute = new VariationAttribute("Remove generic annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_11) { Attribute = new VariationAttribute("Add inherited annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_12) { Attribute = new VariationAttribute("Remove inherited annotation") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_13) { Attribute = new VariationAttribute("Null parameters throw") { Priority = 1 } });
                    this.AddChild(new TestVariation(Annotations_14) { Attribute = new VariationAttribute("Typed string null parameters throw") { Priority = 1 } });
                    this.AddChild(new TestVariation(Annotations_15) { Attribute = new VariationAttribute("Add annotation with same class name but different namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_16) { Attribute = new VariationAttribute("Remove annotation with same class name but different namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_17) { Attribute = new VariationAttribute("Remove annotations of different types and different XObjects") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_18) { Attribute = new VariationAttribute("Remove twice annotations of different types and different XObjects") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_19) { Attribute = new VariationAttribute("Add twice, remove once annotations of different types and different XObjects") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_20) { Attribute = new VariationAttribute("Add annotation to XElement, and clone this element to another subtree, get null annotation") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_21) { Attribute = new VariationAttribute("Add annotation to XElement, and remove this element, get annotation") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_22) { Attribute = new VariationAttribute("Add annotation to parent and child, valIdate annotations for each XObjects") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_23) { Attribute = new VariationAttribute("Add annotations, remove using type object") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_24) { Attribute = new VariationAttribute("Enumerate annotations without adding") { Priority = 2 } });
                    this.AddChild(new TestVariation(Annotations_25) { Attribute = new VariationAttribute("Remove annotations using type object") { Priority = 0 } });
                    this.AddChild(new TestVariation(Annotations_26) { Attribute = new VariationAttribute("Remove twice annotations without adding using type object") { Priority = 0 } });
                }
            }
            public partial class RegressionTests : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+MiscTests+RegressionTests
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(XPIEmptyStringShouldNotBeAllowed) { Attribute = new VariationAttribute("pi.Target = '' should not be allowed") });
                    this.AddChild(new TestVariation(RemovingMixedContent) { Attribute = new VariationAttribute("Removing mixed content") });
                    this.AddChild(new TestVariation(CannotParseDTD) { Attribute = new VariationAttribute("Cannot parse DTD") });
                    this.AddChild(new TestVariation(ReplaceContent) { Attribute = new VariationAttribute("Replace content") });
                    this.AddChild(new TestVariation(DuplicateNamespaceDeclarationIsAllowed) { Attribute = new VariationAttribute("It is possible to add duplicate namespace declaration.") });
                    this.AddChild(new TestVariation(ManuallyDeclaredPrefixNamespacePairIsNotReflectedInTheXElementSerialization) { Attribute = new VariationAttribute("Manually declared prefix-namespace pair is not reflected in the XElement serialization") });
                    this.AddChild(new TestVariation(XNameGetDoesThrowWhenPassingNulls1) { Attribute = new VariationAttribute("XName.Get(string, string) - for NULL, NULL :: wrong exception param") });
                    this.AddChild(new TestVariation(XNameGetDoesThrowWhenPassingNulls2) { Attribute = new VariationAttribute("XName.Get(string, string) - for NULL, string :: wrong exception param") });
                    this.AddChild(new TestVariation(HashingNamePartsShouldBeSameAsHashingExpandedNameWhenUsingNamespaces) { Attribute = new VariationAttribute("'Hashing name parts should be same as hashing expanded name' when using namespaces") });
                    this.AddChild(new TestVariation(CreatingNewXElementsPassingNullReaderAndOrNullXNameShouldThrow) { Attribute = new VariationAttribute("Creating new XElements passing null reader and/or null XName should throw ArgumentNullException") });
                    this.AddChild(new TestVariation(XNodeAddBeforeSelfPrependingTextNodeToTextNodeDoesDisconnectTheOriginalNode) { Attribute = new VariationAttribute("XNode.AddBeforeSelf('text') - prepending the text node to the text node does disconnect the original node.") });
                    this.AddChild(new TestVariation(ReadSubtreeOnXReaderThrows) { Attribute = new VariationAttribute("ReadSubtree () on the XReader throws NullRefException") });
                    this.AddChild(new TestVariation(StackOverflowForDeepNesting) { Attribute = new VariationAttribute("stack overflow for deep nesting") });
                    this.AddChild(new TestVariation(EmptyCDataTextNodeIsNotPreservedInTheTree) { Attribute = new VariationAttribute("The Empty CData text node is not preserved in the tree.") });
                    this.AddChild(new TestVariation(XDocumentToStringThrowsForXDocumentContainingOnlyWhitespaceNodes) { Attribute = new VariationAttribute("XDocument.ToString() throw exception for the XDocument containing whitespace node only") });
                    this.AddChild(new TestVariation(NametableReturnsIncorrectXNamespace) { Attribute = new VariationAttribute("Nametable returns an incorrect XNamespace") });
                    this.AddChild(new TestVariation(XmlNamespaceSerialization) { Attribute = new VariationAttribute("Xml Namespace serialization") });
                    this.AddChild(new TestVariation(CreatingXElementsFromNewDev10Types) { Attribute = new VariationAttribute("Dictionary - old type") { Param = 4 } });
                    this.AddChild(new TestVariation(CreatingXElementsFromNewDev10Types) { Attribute = new VariationAttribute("Tuple.651957 - New Dev10 Types") { Param = 1 } });
                    this.AddChild(new TestVariation(CreatingXElementsFromNewDev10Types) { Attribute = new VariationAttribute("Guid - old type") { Param = 3 } });
                }
            }
            public partial class PrefixImprovements : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+MiscTests+PrefixImprovements
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(var_1) { Attribute = new VariationAttribute("Smoke test") { Priority = 0 } });
                    this.AddChild(new TestVariation(var_1a) { Attribute = new VariationAttribute("Smoke test with attributes.") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_2) { Attribute = new VariationAttribute("Default namespace I.") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_2a) { Attribute = new VariationAttribute("Default namespace I. (attributes)") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_3) { Attribute = new VariationAttribute("Default namespace II.") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_3a) { Attribute = new VariationAttribute("Default namespace II. (attributes)") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_4) { Attribute = new VariationAttribute("Extended tree") { Priority = 2 } });
                    this.AddChild(new TestVariation(var_5) { Attribute = new VariationAttribute("Attribute and element in default NS.") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_5a) { Attribute = new VariationAttribute("Attribute and element in default NS II.") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_5b) { Attribute = new VariationAttribute("Attribute and element in default NS (mix of all)") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_6) { Attribute = new VariationAttribute("In depth ++") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_6b) { Attribute = new VariationAttribute("In depth --") { Priority = 1 } });
                    this.AddChild(new TestVariation(var_7) { Attribute = new VariationAttribute("XmlWriter interference") { Priority = 1 } });
                }
            }

            public partial class XHashtableAPI : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+MiscTests+XHashtableAPI
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(NoneNamespaceSameNameElements) { Attribute = new VariationAttribute("None Namespace, Same Name: Single XNamespace and XName Objects") { Priority = 0 } });
                    this.AddChild(new TestVariation(XmlNamespaceDifferentNameElements) { Attribute = new VariationAttribute("Xml Namespace, Different Name: Single XNamespace, Multiple XName Objects") { Priority = 0 } });
                    this.AddChild(new TestVariation(XmlnsNamespaceSameNameElements) { Attribute = new VariationAttribute("Xmlns Namespace, Same Name: Single XNamespace and XName Objects") { Priority = 0 } });
                    this.AddChild(new TestVariation(DefaultNamespaceDifferentNameElements) { Attribute = new VariationAttribute("Default Namespace, Different Name: Single XNamespace, Multiple XName Objects") { Priority = 0 } });
                    this.AddChild(new TestVariation(SameNamespaceSameNameAttributes) { Attribute = new VariationAttribute("Same Namespace, Same Name: Single XName and XNamespace Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(SameNamespaceDifferentNamesElements) { Attribute = new VariationAttribute("Same Namespaces, Different Names: Single XNamespace, Multiple XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(DifferentNamespacesSameNameAttributes) { Attribute = new VariationAttribute("Different Namespaces, Same Name: Multiple XNamespace and XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(DifferentNamespacesAndNamesElements) { Attribute = new VariationAttribute("Different Namespaces and Names: Multiple XNamespaces and XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(SameDocumentDefaultNamespaceSameNameElements) { Attribute = new VariationAttribute("Same Document, Default Namespace, Same Name: Single XNamespace and XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(DifferentDocumentSameNamespaceSameNameElements) { Attribute = new VariationAttribute("Different Document, Same Namespace, Same Name: Single XNamespace and XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(ImplicitSameName) { Attribute = new VariationAttribute("Implicit Conversion, Same XName: Single XNamespace and XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(ImplicitDifferentName) { Attribute = new VariationAttribute("Implicit Conversion, Different XName: Single XNamespace and Multiple XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(ExplicitSameName) { Attribute = new VariationAttribute("Explicit Conversion, Same XName: Single XNamespace and XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(ExplicitDifferentName) { Attribute = new VariationAttribute("Explicit Conversion, Different XName: Single XNamespace and Multiple XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(ExplicitSameHashcodeElements) { Attribute = new VariationAttribute("Explicit Conversion, Same Hashcode: Multiple XNamespace and XName Objects") { Priority = 1 } });
                    this.AddChild(new TestVariation(DifferentNameSameHashcodeElements) { Attribute = new VariationAttribute("Different Names, Default Namespace, Same Hashcode: Single XNamespace and Multiple XName Objects") { Priority = 0 } });
                    this.AddChild(new TestVariation(DifferentNameNoneNamespaceSameHashcodeElements) { Attribute = new VariationAttribute("Different Names, None Namespace, Same Hashcode: Single XNamespace and Multiple XName Objects") { Priority = 0 } });
                    this.AddChild(new TestVariation(DifferentNamespaceAndNameSameHashcodeElements) { Attribute = new VariationAttribute("Different Names, Different Namespace, Same Hashcode: Multiple XNamespace and XName Objects") { Priority = 0 } });
                }
            }
            public partial class XmlErrata4 : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+MiscTests+XmlErrata4
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with InValid NCName Characters") { Params = new object[] { "InValid", "NCNameChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with InValid NameStart Surrogate High Characters") { Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with InValid Name Surrogate Low Characters") { Params = new object[] { "InValid", "NameSurrogateLowChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with InValid NCName Characters") { Params = new object[] { "InValid", "NCNameChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with InValid NCName Characters") { Params = new object[] { "InValid", "NCNameChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with InValid NCName Start Characters") { Params = new object[] { "InValid", "NCNameStartChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with InValid NCName Start Characters") { Params = new object[] { "InValid", "NCNameStartChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with InValid NCName Start Characters") { Params = new object[] { "InValid", "NCNameStartChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with Valid NCName Characters") { Params = new object[] { "Valid", "NCNameChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with Valid NCName Characters") { Params = new object[] { "Valid", "NCNameChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with Valid NCName Characters") { Params = new object[] { "Valid", "NCNameChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with Valid NCName Start Characters") { Params = new object[] { "Valid", "NCNameStartChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with Valid NCName Start Characters") { Params = new object[] { "Valid", "NCNameStartChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with Valid NCName Start Characters") { Params = new object[] { "Valid", "NCNameStartChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with InValid Name Surrogate Low Characters") { Params = new object[] { "InValid", "NameSurrogateLowChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with InValid Name Surrogate Low Characters") { Params = new object[] { "InValid", "NameSurrogateLowChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with InValid Name Surrogate High Characters") { Params = new object[] { "InValid", "NameSurrogateHighChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with InValid Name Surrogate High Characters") { Params = new object[] { "InValid", "NameSurrogateHighChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with InValid Name Surrogate High Characters") { Params = new object[] { "InValid", "NameSurrogateHighChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with InValid NameStart Surrogate Low Characters") { Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with InValid NameStart Surrogate Low Characters") { Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XElement with InValid NameStart Surrogate Low Characters") { Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XName with InValid NameStart Surrogate High Characters") { Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XName" } } });
                    this.AddChild(new TestVariation(varation1) { Attribute = new VariationAttribute("XAttribute with InValid NameStart Surrogate High Characters") { Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XElement with Valid Name Surrogate High Characters") { Params = new object[] { "InValid", "NameSurrogateHighChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XElement with Valid NameStart Surrogate Low Characters") { Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XAttribute with Valid NameStart Surrogate High Characters") { Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XAttribute with Valid NameStart Surrogate Low Characters") { Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XElement with Valid Name Surrogate Low Characters") { Params = new object[] { "InValid", "NameSurrogateLowChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XName with Valid Name Surrogate High Characters") { Params = new object[] { "InValid", "NameSurrogateHighChar", "XName" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XName with Valid NameStart Surrogate High Characters") { Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XName" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XAttribute with Valid Name Surrogate Low Characters") { Params = new object[] { "InValid", "NameSurrogateLowChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XElement with Valid NameStart Surrogate High Characters") { Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XElement" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XAttribute with Valid Name Surrogate High Characters") { Params = new object[] { "InValid", "NameSurrogateHighChar", "XAttribute" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XName with Valid Name Surrogate Low Characters") { Params = new object[] { "InValid", "NameSurrogateLowChar", "XName" } } });
                    this.AddChild(new TestVariation(varation2) { Attribute = new VariationAttribute("XName with Valid NameStart Surrogate Low Characters") { Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XName" } } });
                    this.AddChild(new TestVariation(varation3) { Attribute = new VariationAttribute("Xml Version Number Change Test") });
                }
            }

            public partial class XNameAPI : XLinqTestCase
            {
                // Type is CoreXml.Test.XLinq.FunctionalTests+MiscTests+XNameAPI
                // Test Case
                public override void AddChildren()
                {
                    this.AddChild(new TestVariation(Variation1) { Attribute = new VariationAttribute("XName.Get: No Namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(Variation2) { Attribute = new VariationAttribute("XName.Get: Valid Namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(Variation3) { Attribute = new VariationAttribute("XName.Get: Xmlns Namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(Variation4) { Attribute = new VariationAttribute("XName.Get: Xml Namespace") { Priority = 0 } });
                    this.AddChild(new TestVariation(Variation5) { Attribute = new VariationAttribute("XName.Get: Invalid name (empty string)") { Priority = 1 } });
                    this.AddChild(new TestVariation(Variation6) { Attribute = new VariationAttribute("XName.Get: Invalid name (null)") { Priority = 1 } });
                    this.AddChild(new TestVariation(Variation12) { Attribute = new VariationAttribute("IEquatable: same names") { Priority = 0 } });
                    this.AddChild(new TestVariation(Variation13) { Attribute = new VariationAttribute("IEquatable: different names (NS)") { Priority = 0 } });
                }
            }
        }
    }
}
