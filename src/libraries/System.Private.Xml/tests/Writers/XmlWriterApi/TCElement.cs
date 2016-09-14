// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCElement : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCElement
        // Test Case
        public override void AddChildren()
        {
            // for function element_1
            {
                this.AddChild(new CVariation(element_1) { Attribute = new Variation("StartElement-EndElement Sanity Test") { id = 1, Pri = 0 } });
            }


            // for function element_2
            {
                this.AddChild(new CVariation(element_2) { Attribute = new Variation("Sanity test for overload WriteStartElement(string prefix, string name, string ns)") { id = 2, Pri = 0 } });
            }


            // for function element_3
            {
                this.AddChild(new CVariation(element_3) { Attribute = new Variation("Sanity test for overload WriteStartElement(string name, string ns)") { id = 3, Pri = 0 } });
            }


            // for function element_4
            {
                this.AddChild(new CVariation(element_4) { Attribute = new Variation("Element name = String.Empty should error") { id = 4, Pri = 1 } });
            }


            // for function element_5
            {
                this.AddChild(new CVariation(element_5) { Attribute = new Variation("Element name = null should error") { id = 5, Pri = 1 } });
            }


            // for function element_6
            {
                this.AddChild(new CVariation(element_6) { Attribute = new Variation("Element NS = String.Empty") { id = 6, Pri = 1 } });
            }


            // for function element_7
            {
                this.AddChild(new CVariation(element_7) { Attribute = new Variation("Element NS = null") { id = 7, Pri = 1 } });
            }


            // for function element_8
            {
                this.AddChild(new CVariation(element_8) { Attribute = new Variation("Write 100 nested elements") });
            }


            // for function element_9
            {
                this.AddChild(new CVariation(element_9) { Attribute = new Variation("WriteDecl with start element with prefix and namespace") { id = 9 } });
            }


            // for function element_10
            {
                this.AddChild(new CVariation(element_10) { Attribute = new Variation("Write many attributes with same names and diff.namespaces") { Param = false } });
                this.AddChild(new CVariation(element_10) { Attribute = new Variation("Write many attributes with same names and diff.namespaces") { Param = true } });
            }


            // for function element_10a
            {
                this.AddChild(new CVariation(element_10a) { Attribute = new Variation("Write many attributes and dup namespace") });
            }


            // for function element_10b
            {
                this.AddChild(new CVariation(element_10b) { Attribute = new Variation("Write many attributes and dup name") });
            }


            // for function element_10c
            {
                this.AddChild(new CVariation(element_10c) { Attribute = new Variation("Write many attributes and dup prefix") });
            }


            // for function element_10d
            {
                this.AddChild(new CVariation(element_10d) { Attribute = new Variation("Write invalid DOCTYPE with many attributes with prefix") });
            }


            // for function element_11
            {
                this.AddChild(new CVariation(element_11) { Attribute = new Variation("WriteEntityRef with XmlWellformedWriter for 'lt'") { Param = 2 } });
                this.AddChild(new CVariation(element_11) { Attribute = new Variation("WriteEntityRef with XmlWellformedWriter for 'quot'") { Param = 3 } });
                this.AddChild(new CVariation(element_11) { Attribute = new Variation("WriteEntityRef with XmlWellformedWriter for 'apos'") { Param = 1 } });
            }


            // for function element_12
            {
                this.AddChild(new CVariation(element_12) { Attribute = new Variation("WriteValue & WriteWhitespace on a special attribute value \u2013 xmlns") { Param = 4 } });
                this.AddChild(new CVariation(element_12) { Attribute = new Variation("WriteValue & WriteWhitespace on a special attribute value \u2013 xml:xmlns") { Param = 1 } });
                this.AddChild(new CVariation(element_12) { Attribute = new Variation("WriteValue & WriteWhitespace on a special attribute value \u2013 space") { Param = 5 } });
                this.AddChild(new CVariation(element_12) { Attribute = new Variation("WriteValue & WriteWhitespace on a special attribute value \u2013 lang") { Param = 6 } });
                this.AddChild(new CVariation(element_12) { Attribute = new Variation("WriteValue & WriteWhitespace on a special attribute value \u2013 xml:space") { Param = 2 } });
                this.AddChild(new CVariation(element_12) { Attribute = new Variation("WriteValue & WriteWhitespace on a special attribute value \u2013 xml:lang") { Param = 3 } });
            }


            // for function element_13
            {
                this.AddChild(new CVariation(element_13) { Attribute = new Variation("WriteValue element double value") { Params = new object[] { false, "<Root>-0</Root>" } } });
                this.AddChild(new CVariation(element_13) { Attribute = new Variation("WriteValue attribute double value") { Params = new object[] { true, "<Root b=\"-0\" />" } } });
            }
        }
    }
}
