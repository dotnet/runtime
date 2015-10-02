// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCNewLineOnAttributes : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCNewLineOnAttributes
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function NewLineOnAttributes_1
            {
                this.AddChild(new CVariation(NewLineOnAttributes_1) { Attribute = new Variation("Make sure the setting has no effect when Indent is false") { id = 1, Pri = 0 } });
            }


            // for function NewLineOnAttributes_2
            {
                this.AddChild(new CVariation(NewLineOnAttributes_2) { Attribute = new Variation("Sanity test when Indent is true") { id = 2, Pri = 1 } });
            }


            // for function NewLineOnAttributes_3
            {
                this.AddChild(new CVariation(NewLineOnAttributes_3) { Attribute = new Variation("Attributes of nested elements") { id = 3, Pri = 1 } });
            }
        }
    }
}
