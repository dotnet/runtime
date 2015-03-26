// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCMoveToContent : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCMoveToContent
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
