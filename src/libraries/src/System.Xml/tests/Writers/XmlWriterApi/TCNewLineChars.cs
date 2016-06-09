// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCNewLineChars : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCNewLineChars
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function NewLineChars_1
            {
                this.AddChild(new CVariation(NewLineChars_1) { Attribute = new Variation("Set to tab char") { id = 1, Pri = 0 } });
            }


            // for function NewLineChars_2
            {
                this.AddChild(new CVariation(NewLineChars_2) { Attribute = new Variation("Set to multiple whitespace chars") { id = 2, Pri = 0 } });
            }


            // for function NewLineChars_3
            {
                this.AddChild(new CVariation(NewLineChars_3) { Attribute = new Variation("Set to 0xA") { id = 3, Pri = 0 } });
            }


            // for function NewLineChars_4
            {
                this.AddChild(new CVariation(NewLineChars_4) { Attribute = new Variation("Set to 0xD") { id = 4, Pri = 0 } });
            }


            // for function NewLineChars_5
            {
                this.AddChild(new CVariation(NewLineChars_5) { Attribute = new Variation("Set to 0x20") { id = 5, Pri = 0 } });
            }


            // for function NewLineChars_6
            {
                this.AddChild(new CVariation(NewLineChars_6) { Attribute = new Variation("Set to comment start tag") { Param = "<!--", id = 8, Pri = 1 } });
                this.AddChild(new CVariation(NewLineChars_6) { Attribute = new Variation("Set to <") { Param = "<", id = 6, Pri = 1 } });
                this.AddChild(new CVariation(NewLineChars_6) { Attribute = new Variation("Set to &") { Param = "&", id = 7, Pri = 1 } });
            }
        }
    }
}
