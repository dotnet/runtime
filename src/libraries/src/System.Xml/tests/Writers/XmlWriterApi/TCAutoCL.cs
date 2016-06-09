// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCAutoCL : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCAutoCL
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function auto_1
            {
                this.AddChild(new CVariation(auto_1) { Attribute = new Variation("Change to CL Document after WriteStartDocument()") { id = 1, Pri = 0 } });
            }


            // for function auto_2
            {
                this.AddChild(new CVariation(auto_2) { Attribute = new Variation("Change to CL Document after WriteStartDocument(standalone = true)") { Param = "true", id = 2, Pri = 0 } });
                this.AddChild(new CVariation(auto_2) { Attribute = new Variation("Change to CL Document after WriteStartDocument(standalone = false)") { Param = "false", id = 3, Pri = 0 } });
            }


            // for function auto_3
            {
                this.AddChild(new CVariation(auto_3) { Attribute = new Variation("Change to CL Document when you write DocType decl") { id = 4, Pri = 0 } });
            }


            // for function auto_4
            {
                this.AddChild(new CVariation(auto_4) { Attribute = new Variation("Change to CL Fragment when you write a root element") { id = 5, Pri = 1 } });
            }


            // for function auto_5
            {
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteChars at top level") { Param = "Chars", id = 11, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteRaw at top level") { Param = "Raw", id = 12, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteString at top level") { Param = "String", id = 6, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteCData at top level") { Param = "CData", id = 7, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteEntityRef at top level") { Param = "EntityRef", id = 8, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteCharEntity at top level") { Param = "CharEntity", id = 9, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteSurrogateCharEntity at top level") { Param = "SurrogateCharEntity", id = 10, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteBase64 at top level") { Param = "Base64", id = 13, Pri = 1 } });
                this.AddChild(new CVariation(auto_5) { Attribute = new Variation("Change to CL Fragment for WriteBinHex at top level") { Param = "BinHex", id = 14, Pri = 1 } });
            }


            // for function auto_6
            {
                this.AddChild(new CVariation(auto_6) { Attribute = new Variation("WriteWhitespace at top level, followed by DTD, expected CL = Document") { Param = "WS", id = 17, Pri = 2 } });
                this.AddChild(new CVariation(auto_6) { Attribute = new Variation("WritePI at top level, followed by DTD, expected CL = Document") { Param = "PI", id = 15, Pri = 2 } });
                this.AddChild(new CVariation(auto_6) { Attribute = new Variation("WriteComment at top level, followed by DTD, expected CL = Document") { Param = "Comment", id = 16, Pri = 2 } });
            }


            // for function auto_7
            {
                this.AddChild(new CVariation(auto_7) { Attribute = new Variation("WriteWhitespace at top level, followed by text, expected CL = Fragment") { Param = "WS", id = 20, Pri = 2 } });
                this.AddChild(new CVariation(auto_7) { Attribute = new Variation("WritePI at top level, followed by text, expected CL = Fragment") { Param = "PI", id = 18, Pri = 2 } });
                this.AddChild(new CVariation(auto_7) { Attribute = new Variation("WriteComment at top level, followed by text, expected CL = Fragment") { Param = "Comment", id = 19, Pri = 2 } });
            }


            // for function auto_10
            {
                this.AddChild(new CVariation(auto_10) { Attribute = new Variation("WriteNode(XmlReader) when reader positioned on text node, expected CL = Fragment") { id = 22, Pri = 2 } });
            }
        }
    }
}
