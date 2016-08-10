// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCMoveToContent : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCMoveToContent
        // Test Case
        public override void AddChildren()
        {
            // for function TestMoveToContent1
            {
                this.AddChild(new CVariation(TestMoveToContent1) { Attribute = new Variation("MoveToContent on Skip XmlDeclaration") { Pri = 0 } });
            }


            // for function TestMoveToContent3
            {
                this.AddChild(new CVariation(TestMoveToContent3) { Attribute = new Variation("MoveToContent on Read through All invalid Content Node(PI, Comment and whitespace)") { Pri = 0 } });
            }


            // for function TestMoveToContent5
            {
                this.AddChild(new CVariation(TestMoveToContent5) { Attribute = new Variation("MoveToContent on Attribute") { Pri = 0 } });
            }
        }
    }
}
