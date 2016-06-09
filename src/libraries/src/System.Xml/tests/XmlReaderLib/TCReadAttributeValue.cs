// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadAttributeValue : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadAttributeValue
        // Test Case
        public override void AddChildren()
        {
            // for function TestReadAttributeValue1
            {
                this.AddChild(new CVariation(TestReadAttributeValue1) { Attribute = new Variation("ReadAttributeValue where Attribute count = 0") });
            }


            // for function TestReadAttributeValue2
            {
                this.AddChild(new CVariation(TestReadAttributeValue2) { Attribute = new Variation("ReadAttributeValue where Attribute count = 1") });
            }


            // for function TestReadAttributeValue3
            {
                this.AddChild(new CVariation(TestReadAttributeValue3) { Attribute = new Variation("ReadAttributeValue where Attribute count = 3") { Pri = 0 } });
            }


            // for function TestReadAttributeValue4
            {
                this.AddChild(new CVariation(TestReadAttributeValue4) { Attribute = new Variation("ReadAttributeValue where Attribute count = 1 and value is empty String") { Pri = 0 } });
            }


            // for function TestReadAttributeValue5
            {
                this.AddChild(new CVariation(TestReadAttributeValue5) { Attribute = new Variation("ReadAttributeValue successive calls") });
            }
        }
    }
}
