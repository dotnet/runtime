// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCMoveToAttribute : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCMoveToAttribute
        // Test Case
        public override void AddChildren()
        {
            // for function MoveToAttributeWithName1
            {
                this.AddChild(new CVariation(MoveToAttributeWithName1) { Attribute = new Variation("MoveToAttribute(String.Empty)") });
            }


            // for function MoveToAttributeWithName2
            {
                this.AddChild(new CVariation(MoveToAttributeWithName2) { Attribute = new Variation("MoveToAttribute(String.Empty,String.Empty)") });
            }
        }
    }
}
