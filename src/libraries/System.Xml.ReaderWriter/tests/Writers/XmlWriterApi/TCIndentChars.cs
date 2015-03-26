// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace XmlWriterAPI.Test
{
    public partial class TCIndentChars : XmlFactoryWriterTestCaseBase
    {
        // Type is XmlWriterAPI.Test.TCIndentChars
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function IndentChars_1
            {
                this.AddChild(new CVariation(IndentChars_1) { Attribute = new Variation("Set to tab char") { id = 1, Pri = 0 } });
            }


            // for function IndentChars_2
            {
                this.AddChild(new CVariation(IndentChars_2) { Attribute = new Variation("Set to multiple whitespace chars") { id = 2, Pri = 0 } });
            }


            // for function IndentChars_3
            {
                this.AddChild(new CVariation(IndentChars_3) { Attribute = new Variation("Set to 0xA") { id = 3, Pri = 0 } });
            }


            // for function IndentChars_4
            {
                this.AddChild(new CVariation(IndentChars_4) { Attribute = new Variation("Set to 0xD") { id = 4, Pri = 0 } });
            }


            // for function IndentChars_5
            {
                this.AddChild(new CVariation(IndentChars_5) { Attribute = new Variation("Set to 0x20") { id = 5, Pri = 0 } });
            }


            // for function IndentChars_6
            {
                this.AddChild(new CVariation(IndentChars_6) { Attribute = new Variation("Set to &") { Param = "&", id = 7, Pri = 1 } });
                this.AddChild(new CVariation(IndentChars_6) { Attribute = new Variation("Set to element start tag") { Param = "<", id = 6, Pri = 1 } });
                this.AddChild(new CVariation(IndentChars_6) { Attribute = new Variation("Set to comment start tag") { Param = "<!--", id = 8, Pri = 1 } });
            }
        }
    }
}
