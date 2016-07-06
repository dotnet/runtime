// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                this.AddChild(new PrefixImprovements() { Attribute = new TestCaseAttribute() { Name = "Prefix improvements :: Find correct namespace prefix. in case of redefinition conflict." } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - expanded name", Param = GetNameType.ExpandedName } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - expanded name (From string)", Param = GetNameType.FromString } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - two param Get", Param = GetNameType.TwoParamGet } });
                this.AddChild(new XNameAPI() { Attribute = new TestCaseAttribute() { Name = "XName API - XNamespace + string", Param = GetNameType.XNamespacePlusOperator } });
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
