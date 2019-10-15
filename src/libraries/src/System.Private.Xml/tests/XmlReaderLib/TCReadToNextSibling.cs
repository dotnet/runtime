// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadToNextSibling : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadToNextSibling
        // Test Case
        public override void AddChildren()
        {
            // for function v
            {
                this.AddChild(new CVariation(v) { Attribute = new Variation("Simple positive test 1") { Params = new object[] { "NNS" }, Pri = 0 } });
                this.AddChild(new CVariation(v) { Attribute = new Variation("Simple positive test 2") { Params = new object[] { "DNS" }, Pri = 0 } });
                this.AddChild(new CVariation(v) { Attribute = new Variation("Simple positive test 3") { Params = new object[] { "NS" }, Pri = 0 } });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("Read on a deep tree at least more than 4K boundary") { Pri = 2 } });
            }


            // for function v2_1
            {
                this.AddChild(new CVariation(v2_1) { Attribute = new Variation("Read on a deep tree with more than 65536 elems") { Pri = 2 } });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read on next sibling with same names 3") { Params = new object[] { "NS", "<root xmlns:a='a'><a:a att='1'/><a:a att='2'/><a:a att='3'/></root>" }, Pri = 1 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read on next sibling with same names 2") { Params = new object[] { "DNS", "<root xmlns='a'><a att='1'/><a att='2'/><a att='3'/></root>" }, Pri = 1 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Read to next sibling with same names 1") { Params = new object[] { "NNS", "<root><a att='1'/><a att='2'/><a att='3'/></root>" }, Pri = 1 } });
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
                this.AddChild(new CVariation(v6) { Attribute = new Variation("Read to next sibling on one level and again to level below it") { Pri = 1 } });
            }


            // for function v12
            {
                this.AddChild(new CVariation(v12) { Attribute = new Variation("Call from different nodetypes") { Pri = 1 } });
            }


            // for function v16
            {
                this.AddChild(new CVariation(v16) { Attribute = new Variation("Pass null to both arguments throws ArgumentException") { Pri = 2 } });
            }


            // for function v17
            {
                this.AddChild(new CVariation(v17) { Attribute = new Variation("Different names, same uri works correctly") { Pri = 2 } });
            }
        }
    }
}
