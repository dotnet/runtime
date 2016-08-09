// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCAttribute : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCAttribute
        // Test Case
        public override void AddChildren()
        {
            // for function attribute_1
            {
                this.AddChild(new CVariation(attribute_1) { Attribute = new Variation("Sanity test for WriteAttribute") { id = 1, Pri = 0 } });
            }


            // for function attribute_2
            {
                this.AddChild(new CVariation(attribute_2) { Attribute = new Variation("Missing EndAttribute should be fixed") { id = 2, Pri = 0 } });
            }


            // for function attribute_3
            {
                this.AddChild(new CVariation(attribute_3) { Attribute = new Variation("WriteStartAttribute followed by WriteStartAttribute") { id = 3, Pri = 0 } });
            }


            // for function attribute_4
            {
                this.AddChild(new CVariation(attribute_4) { Attribute = new Variation("Multiple WritetAttributeString") { id = 4, Pri = 0 } });
            }


            // for function attribute_5
            {
                this.AddChild(new CVariation(attribute_5) { Attribute = new Variation("WriteStartAttribute followed by WriteString") { id = 5, Pri = 0 } });
            }


            // for function attribute_6
            {
                this.AddChild(new CVariation(attribute_6) { Attribute = new Variation("Sanity test for overload WriteStartAttribute(name, ns)") { id = 6, Pri = 1 } });
            }


            // for function attribute_7
            {
                this.AddChild(new CVariation(attribute_7) { Attribute = new Variation("Sanity test for overload WriteStartAttribute(prefix, name, ns)") { id = 7, Pri = 0 } });
            }


            // for function attribute_8
            {
                this.AddChild(new CVariation(attribute_8) { Attribute = new Variation("DCR 64183: Duplicate attribute 'attr1'") { id = 8, Pri = 1 } });
            }


            // for function attribute_9
            {
                this.AddChild(new CVariation(attribute_9) { Attribute = new Variation("DCR 64183: Duplicate attribute 'ns1:attr1'") { id = 9, Pri = 1 } });
            }


            // for function attribute_10
            {
                this.AddChild(new CVariation(attribute_10) { Attribute = new Variation("Attribute name = String.Empty should error") { id = 10, Pri = 1 } });
            }


            // for function attribute_11
            {
                this.AddChild(new CVariation(attribute_11) { Attribute = new Variation("Attribute name = null") { id = 11, Pri = 1 } });
            }


            // for function attribute_12
            {
                this.AddChild(new CVariation(attribute_12) { Attribute = new Variation("DCR 64183: WriteAttribute with names Foo, fOo, foO, FOO") { id = 12, Pri = 1 } });
            }


            // for function attribute_13
            {
                this.AddChild(new CVariation(attribute_13) { Attribute = new Variation("Invalid value of xml:space") { id = 13, Pri = 1 } });
            }


            // for function attribute_14
            {
                this.AddChild(new CVariation(attribute_14) { Attribute = new Variation("SingleQuote in attribute value should be allowed") { id = 14 } });
            }


            // for function attribute_15
            {
                this.AddChild(new CVariation(attribute_15) { Attribute = new Variation("DoubleQuote in attribute value should be escaped") { id = 15 } });
            }


            // for function attribute_16
            {
                this.AddChild(new CVariation(attribute_16) { Attribute = new Variation("WriteAttribute with value = &, #65, #x20") { id = 16, Pri = 1 } });
            }


            // for function attribute_17
            {
                this.AddChild(new CVariation(attribute_17) { Attribute = new Variation("WriteAttributeString followed by WriteString") { id = 17, Pri = 1 } });
            }


            // for function attribute_18
            {
                this.AddChild(new CVariation(attribute_18) { Attribute = new Variation("WriteAttribute followed by WriteString") { id = 18, Pri = 1 } });
            }


            // for function attribute_19
            {
                this.AddChild(new CVariation(attribute_19) { Attribute = new Variation("WriteAttribute with all whitespace characters") { id = 19, Pri = 1 } });
            }


            // for function attribute_20
            {
                this.AddChild(new CVariation(attribute_20) { Attribute = new Variation("< > & chars should be escaped in attribute value") { id = 20, Pri = 1 } });
            }


            // for function attribute_21
            {
                this.AddChild(new CVariation(attribute_21) { Attribute = new Variation("Bug 73919 testcase: Redefine auto generated prefix n1") { id = 21 } });
            }


            // for function attribute_22
            {
                this.AddChild(new CVariation(attribute_22) { Attribute = new Variation("Bug 74758 testcase: Reuse and redefine existing prefix") { id = 22 } });
            }


            // for function attribute_23
            {
                this.AddChild(new CVariation(attribute_23) { Attribute = new Variation("DCR 100451: WriteStartAttribute(attr) sanity test") { id = 23 } });
            }


            // for function attribute_24
            {
                this.AddChild(new CVariation(attribute_24) { Attribute = new Variation("DCR 100451: WriteStartAttribute(attr) inside an element with changed default namespace") { id = 24 } });
            }


            // for function attribute_25
            {
                this.AddChild(new CVariation(attribute_25) { Attribute = new Variation("DCR 100451: WriteStartAttribute(attr) and duplicate attrs") { id = 25 } });
            }


            // for function attribute_26
            {
                this.AddChild(new CVariation(attribute_26) { Attribute = new Variation("DCR 100451: WriteStartAttribute(attr) when element has ns:attr") { id = 26 } });
            }


            // for function attribute_27
            {
                this.AddChild(new CVariation(attribute_27) { Attribute = new Variation("BUG 397795: XmlCharCheckingWriter should not normalize newLines in attribute values when NewLinesHandling = Replace") { id = 27 } });
            }


            // for function attribute_28
            {
                this.AddChild(new CVariation(attribute_28) { Attribute = new Variation("BUG 396978: Wrapped XmlTextWriter: Invalid replacement of newline characters in text values") { id = 28 } });
            }


            // for function attribute_29
            {
                this.AddChild(new CVariation(attribute_29) { Attribute = new Variation("442897: WriteAttributeString doesn't fail on invalid surrogate pair sequences") { id = 29 } });
            }
        }
    }
}
