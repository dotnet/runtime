// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCDepth : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCDepth
        // Test Case
        public override void AddChildren()
        {
            // for function TestDepth1
            {
                this.AddChild(new CVariation(TestDepth1) { Attribute = new Variation("XmlReader Depth at the Root") { Pri = 0 } });
            }


            // for function TestDepth2
            {
                this.AddChild(new CVariation(TestDepth2) { Attribute = new Variation("XmlReader Depth at Empty Tag") });
            }


            // for function TestDepth3
            {
                this.AddChild(new CVariation(TestDepth3) { Attribute = new Variation("XmlReader Depth at Empty Tag with Attributes") });
            }


            // for function TestDepth4
            {
                this.AddChild(new CVariation(TestDepth4) { Attribute = new Variation("XmlReader Depth at Non Empty Tag with Text") });
            }


            // for function TestDepth5
            {
                this.AddChild(new CVariation(TestDepth5) { Attribute = new Variation("Depth on node from expanded entity") });
            }


            // for function TestDepth6
            {
                this.AddChild(new CVariation(TestDepth6) { Attribute = new Variation("Depth on node from expanded entity EntityHandling = ExpandEntities") });
            }
        }
    }
}
