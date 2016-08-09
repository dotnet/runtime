// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadToDescendant : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadToDescendant
        // Test Case
        public override void AddChildren()
        {
            // for function v
            {
                this.AddChild(new CVariation(v) { Attribute = new Variation("Simple positive test") { Params = new object[] { "NNS" }, Pri = 0 } });
                this.AddChild(new CVariation(v) { Attribute = new Variation("Simple positive test") { Params = new object[] { "NS" }, Pri = 0 } });
                this.AddChild(new CVariation(v) { Attribute = new Variation("Simple positive test") { Params = new object[] { "DNS" }, Pri = 0 } });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("Read on a deep tree at least more than 4K boundary") { Pri = 2 } });
            }


            // for function v2_1
            {
                this.AddChild(new CVariation(v2_1) { Attribute = new Variation("Read on a deep tree at least more than 65535 boundary") { Pri = 2 } });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read on descendant with same names") { Params = new object[] { "DNS" }, Pri = 1 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read on descendant with same names") { Params = new object[] { "NS" }, Pri = 1 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read on descendant with same names") { Params = new object[] { "NNS" }, Pri = 1 } });
            }


            // for function v4
            {
                this.AddChild(new CVariation(v4) { Attribute = new Variation("If name not found, stop at end element of the subtree") { Pri = 1 } });
            }


            // for function v5
            {
                this.AddChild(new CVariation(v5) { Attribute = new Variation("Positioning on a level and try to find the name which is on a level higher") { Pri = 1 } });
            }


            // for function v6
            {
                this.AddChild(new CVariation(v6) { Attribute = new Variation("Read to Descendant on one level and again to level below it") { Pri = 1 } });
            }


            // for function v7
            {
                this.AddChild(new CVariation(v7) { Attribute = new Variation("Read to Descendant on one level and again to level below it, with namespace") { Pri = 1 } });
            }


            // for function v8
            {
                this.AddChild(new CVariation(v8) { Attribute = new Variation("Read to Descendant on one level and again to level below it, with prefix") { Pri = 1 } });
            }


            // for function v9
            {
                this.AddChild(new CVariation(v9) { Attribute = new Variation("Multiple Reads to children and then next siblings, NNS") { Pri = 2 } });
            }


            // for function v10
            {
                this.AddChild(new CVariation(v10) { Attribute = new Variation("Multiple Reads to children and then next siblings, DNS") { Pri = 2 } });
            }


            // for function v11
            {
                this.AddChild(new CVariation(v11) { Attribute = new Variation("Multiple Reads to children and then next siblings, NS") { Pri = 2 } });
            }


            // for function v12
            {
                this.AddChild(new CVariation(v12) { Attribute = new Variation("Call from different nodetypes") { Pri = 1 } });
            }


            // for function v13
            {
                this.AddChild(new CVariation(v13) { Attribute = new Variation("Interaction with MoveToContent") { Pri = 2 } });
            }


            // for function v14
            {
                this.AddChild(new CVariation(v14) { Attribute = new Variation("Only child has namespaces and read to it") { Pri = 2 } });
            }


            // for function v15
            {
                this.AddChild(new CVariation(v15) { Attribute = new Variation("Pass null to both arguments throws ArgumentException") { Pri = 2 } });
            }


            // for function v17
            {
                this.AddChild(new CVariation(v17) { Attribute = new Variation("Different names, same uri works correctly") { Pri = 2 } });
            }


            // for function v18
            {
                this.AddChild(new CVariation(v18) { Attribute = new Variation("On Root Node") { Params = new object[] { "DNS" }, Pri = 0 } });
                this.AddChild(new CVariation(v18) { Attribute = new Variation("On Root Node") { Params = new object[] { "NNS" }, Pri = 0 } });
                this.AddChild(new CVariation(v18) { Attribute = new Variation("On Root Node") { Params = new object[] { "NS" }, Pri = 0 } });
            }


            // for function v19
            {
                this.AddChild(new CVariation(v19) { Attribute = new Variation("427176	Assertion failed when call XmlReader.ReadToDescendant() for non-existing node") { Pri = 1 } });
            }
        }
    }
}
