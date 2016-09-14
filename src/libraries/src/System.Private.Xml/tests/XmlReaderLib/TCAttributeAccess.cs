// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCAttributeAccess : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCAttributeAccess
        // Test Case
        public override void AddChildren()
        {
            // for function TestAttributeAccess1
            {
                this.AddChild(new CVariation(TestAttributeAccess1) { Attribute = new Variation("Attribute Access test using ordinal (Ascending Order)") { Pri = 0 } });
            }


            // for function TestAttributeAccess2
            {
                this.AddChild(new CVariation(TestAttributeAccess2) { Attribute = new Variation("Attribute Access test using ordinal (Descending Order)") });
            }


            // for function TestAttributeAccess3
            {
                this.AddChild(new CVariation(TestAttributeAccess3) { Attribute = new Variation("Attribute Access test using ordinal (Odd number)") { Pri = 0 } });
            }


            // for function TestAttributeAccess4
            {
                this.AddChild(new CVariation(TestAttributeAccess4) { Attribute = new Variation("Attribute Access test using ordinal (Even number)") });
            }


            // for function TestAttributeAccess5
            {
                this.AddChild(new CVariation(TestAttributeAccess5) { Attribute = new Variation("Attribute Access with namespace=null") });
            }
        }
    }
}
