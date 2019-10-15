// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCNamespace : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCNamespace
        // Test Case
        public override void AddChildren()
        {
            // for function TestNamespace1
            {
                this.AddChild(new CVariation(TestNamespace1) { Attribute = new Variation("Namespace test within a scope (no nested element)") { Pri = 0 } });
            }


            // for function TestNamespace2
            {
                this.AddChild(new CVariation(TestNamespace2) { Attribute = new Variation("Namespace test within a scope (with nested element)") { Pri = 0 } });
            }


            // for function TestNamespace3
            {
                this.AddChild(new CVariation(TestNamespace3) { Attribute = new Variation("Namespace test immediately outside the Namespace scope") });
            }


            // for function TestNamespace4
            {
                this.AddChild(new CVariation(TestNamespace4) { Attribute = new Variation("Namespace test Attribute should has no default namespace") { Pri = 0 } });
            }


            // for function TestNamespace5
            {
                this.AddChild(new CVariation(TestNamespace5) { Attribute = new Variation("Namespace test with multiple Namespace declaration") { Pri = 0 } });
            }


            // for function TestNamespace6
            {
                this.AddChild(new CVariation(TestNamespace6) { Attribute = new Variation("Namespace test with multiple Namespace declaration, including default namespace") });
            }


            // for function TestNamespace7
            {
                this.AddChild(new CVariation(TestNamespace7) { Attribute = new Variation("Namespace URI for xml prefix") { Pri = 0 } });
            }
        }
    }
}
