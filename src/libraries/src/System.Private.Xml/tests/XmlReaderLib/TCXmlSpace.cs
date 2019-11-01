// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCXmlSpace : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCXmlSpace
        // Test Case
        public override void AddChildren()
        {
            // for function TestXmlSpace1
            {
                this.AddChild(new CVariation(TestXmlSpace1) { Attribute = new Variation("XmlSpace test within EmptyTag") });
            }


            // for function TestXmlSpace2
            {
                this.AddChild(new CVariation(TestXmlSpace2) { Attribute = new Variation("Xmlspace test within a scope (no nested element)") { Pri = 0 } });
            }


            // for function TestXmlSpace3
            {
                this.AddChild(new CVariation(TestXmlSpace3) { Attribute = new Variation("Xmlspace test within a scope (with nested element)") { Pri = 0 } });
            }


            // for function TestXmlSpace4
            {
                this.AddChild(new CVariation(TestXmlSpace4) { Attribute = new Variation("Xmlspace test immediately outside the XmlSpace scope") });
            }


            // for function TestXmlSpace5
            {
                this.AddChild(new CVariation(TestXmlSpace5) { Attribute = new Variation("XmlSpace test with multiple XmlSpace declaration") });
            }
        }
    }
}
