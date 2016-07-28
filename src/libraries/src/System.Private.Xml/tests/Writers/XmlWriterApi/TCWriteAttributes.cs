// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCWriteAttributes : ReaderParamTestCase
    {
        // Type is System.Xml.Tests.TCWriteAttributes
        // Test Case
        public override void AddChildren()
        {
            // for function writeAttributes_3
            {
                this.AddChild(new CVariation(writeAttributes_3) { Attribute = new Variation("Call WriteAttributes with XmlReader = null") { id = 3 } });
            }


            // for function writeAttributes_4
            {
                this.AddChild(new CVariation(writeAttributes_4) { Attribute = new Variation("Call WriteAttributes when reader is located on element") { id = 4, Pri = 1 } });
            }


            // for function writeAttributes_5
            {
                this.AddChild(new CVariation(writeAttributes_5) { Attribute = new Variation("Call WriteAttributes when reader is located in the middle attribute") { id = 5, Pri = 1 } });
            }


            // for function writeAttributes_6
            {
                this.AddChild(new CVariation(writeAttributes_6) { Attribute = new Variation("Call WriteAttributes when reader is located in the last attribute") { id = 6, Pri = 1 } });
            }


            // for function writeAttributes_8
            {
                this.AddChild(new CVariation(writeAttributes_8) { Attribute = new Variation("Call WriteAttributes with reader on XmlDeclaration") { id = 8, Pri = 1 } });
            }


            // for function writeAttributes_9
            {
                this.AddChild(new CVariation(writeAttributes_9) { Attribute = new Variation("Call WriteAttributes with reader on Text") { Param = "Text", id = 11, Pri = 1 } });
                this.AddChild(new CVariation(writeAttributes_9) { Attribute = new Variation("Call WriteAttributes with reader on Comment") { Param = "Comment", id = 13, Pri = 1 } });
                this.AddChild(new CVariation(writeAttributes_9) { Attribute = new Variation("Call WriteAttributes with reader on CDATA") { Param = "CDATA", id = 10, Pri = 1 } });
                this.AddChild(new CVariation(writeAttributes_9) { Attribute = new Variation("Call WriteAttributes with reader on PI") { Param = "ProcessingInstruction", id = 12, Pri = 1 } });
                this.AddChild(new CVariation(writeAttributes_9) { Attribute = new Variation("Call WriteAttributes with reader on EntityRef") { Param = "EntityReference", id = 14, Pri = 1 } });
                this.AddChild(new CVariation(writeAttributes_9) { Attribute = new Variation("Call WriteAttributes with reader on Whitespace") { Param = "Whitespace", id = 15, Pri = 1 } });
                this.AddChild(new CVariation(writeAttributes_9) { Attribute = new Variation("Call WriteAttributes with reader on SignificantWhitespace") { Param = "SignificantWhitespace", id = 16, Pri = 1 } });
            }


            // for function writeAttributes_10
            {
                this.AddChild(new CVariation(writeAttributes_10) { Attribute = new Variation("Call WriteAttribute with double quote char in the value") { id = 17, Pri = 1 } });
            }


            // for function writeAttributes_11
            {
                this.AddChild(new CVariation(writeAttributes_11) { Attribute = new Variation("Call WriteAttribute with single quote char in the value") { id = 18, Pri = 1 } });
            }


            // for function writeAttributes_12
            {
                this.AddChild(new CVariation(writeAttributes_12) { Attribute = new Variation("Call WriteAttributes with 100 attributes") { id = 19, Pri = 1 } });
            }


            // for function writeAttributes_13
            {
                this.AddChild(new CVariation(writeAttributes_13) { Attribute = new Variation("WriteAttributes with different builtin entities in attribute value") { id = 20, Pri = 1 } });
            }


            // for function writeAttributes_14
            {
                this.AddChild(new CVariation(writeAttributes_14) { Attribute = new Variation("WriteAttributes tries to duplicate attribute") { id = 21, Pri = 1 } });
            }
        }
    }
}
