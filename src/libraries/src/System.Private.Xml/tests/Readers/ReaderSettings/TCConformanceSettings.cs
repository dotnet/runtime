// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCConformanceSettings : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCConformanceSettings
        // Test Case
        public override void AddChildren()
        {
            // for function wrappingTests
            {
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Fragment", "Fragment", "<root/><root/>", "true" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Document", "Auto", "<root/>", "true" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Auto", "Fragment", "<root/>", "false" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Fragment", "Fragment", "<root/>", "true" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Auto", "Auto", "<root/>", "true" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Document", "Fragment", "<root/>", "false" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Auto", "Document", "<root/>", "false" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Fragment", "Document", "<root/><root/>", "false" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Document", "Document", "<root/>", "true" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Auto", "Auto", "<root/><root/>", "true" }, Pri = 2 } });
                this.AddChild(new CVariation(wrappingTests) { Attribute = new Variation("Wrapping Tests: CR with CR") { Params = new object[] { "Fragment", "Auto", "<root/><root/>", "true" }, Pri = 2 } });
            }


            // for function v1
            {
                this.AddChild(new CVariation(v1) { Attribute = new Variation("Default Values") { Pri = 0 } });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("Default Reader, Check Characters On and pass invalid characters") { Params = new object[] { "CoreValidatingReader" }, Pri = 0 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("Default Reader, Check Characters On and pass invalid characters") { Params = new object[] { "CoreReader" }, Pri = 0 } });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Default Reader, Check Characters Off and pass invalid characters in text") { Params = new object[] { "CoreReader" }, Pri = 0 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Default Reader, Check Characters Off and pass invalid characters in text") { Params = new object[] { "CoreValidatingReader" }, Pri = 0 } });
            }


            // for function v4
            {
                this.AddChild(new CVariation(v4) { Attribute = new Variation("Default Reader, Check Characters Off and pass invalid characters in element") { Params = new object[] { "CoreReader" }, Pri = 0 } });
                this.AddChild(new CVariation(v4) { Attribute = new Variation("Default Reader, Check Characters Off and pass invalid characters in element") { Params = new object[] { "CoreValidatingReader" }, Pri = 0 } });
            }


            // for function v5
            {
                this.AddChild(new CVariation(v5) { Attribute = new Variation("Default Reader, Check Characters On and pass invalid characters in text") { Params = new object[] { "CoreReader" }, Pri = 0 } });
                this.AddChild(new CVariation(v5) { Attribute = new Variation("Default Reader, Check Characters On and pass invalid characters in text") { Params = new object[] { "CoreValidatingReader" }, Pri = 0 } });
            }


            // for function CAuto
            {
                this.AddChild(new CVariation(CAuto) { Attribute = new Variation("Conformance Level to Auto and test various scenarios from test plan") { Pri = 0 } });
            }


            // for function CFragment
            {
                this.AddChild(new CVariation(CFragment) { Attribute = new Variation("Conformance Level to Fragment and test various scenarios from test plan") { Pri = 0 } });
            }


            // for function CDocument
            {
                this.AddChild(new CVariation(CDocument) { Attribute = new Variation("Conformance Level to Document and test various scenarios from test plan") { Pri = 0 } });
            }


            // for function InvalidValueRange
            {
                this.AddChild(new CVariation(InvalidValueRange) { Attribute = new Variation("Test Invalid Value Range for enum properties") { Pri = 1 } });
            }
        }
    }
}
