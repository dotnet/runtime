// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCNewLineHandling : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCNewLineHandling
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function NewLineHandling_1
            {
                this.AddChild(new CVariation(NewLineHandling_1) { Attribute = new Variation("Test for CR (xD) inside element when NewLineHandling = Replace") { id = 1, Pri = 0 } });
            }


            // for function NewLineHandling_2
            {
                this.AddChild(new CVariation(NewLineHandling_2) { Attribute = new Variation("Test for LF (xA) inside element when NewLineHandling = Replace") { id = 2, Pri = 0 } });
            }


            // for function NewLineHandling_3
            {
                this.AddChild(new CVariation(NewLineHandling_3) { Attribute = new Variation("Test for CR LF (xD xA) inside element when NewLineHandling = Replace") { id = 3, Pri = 0 } });
            }


            // for function NewLineHandling_4
            {
                this.AddChild(new CVariation(NewLineHandling_4) { Attribute = new Variation("Test for CR (xD) inside element when NewLineHandling = Entitize") { id = 4, Pri = 0 } });
            }


            // for function NewLineHandling_5
            {
                this.AddChild(new CVariation(NewLineHandling_5) { Attribute = new Variation("Test for LF (xA) inside element when NewLineHandling = Entitize") { id = 5, Pri = 0 } });
            }


            // for function NewLineHandling_6
            {
                this.AddChild(new CVariation(NewLineHandling_6) { Attribute = new Variation("Test for CR LF (xD xA) inside element when NewLineHandling = Entitize") { id = 6, Pri = 0 } });
            }


            // for function NewLineHandling_7
            {
                this.AddChild(new CVariation(NewLineHandling_7) { Attribute = new Variation("Test for CR (xD) inside attr when NewLineHandling = Replace") { id = 7, Pri = 0 } });
            }


            // for function NewLineHandling_8
            {
                this.AddChild(new CVariation(NewLineHandling_8) { Attribute = new Variation("Test for LF (xA) inside attr when NewLineHandling = Replace") { id = 8, Pri = 0 } });
            }


            // for function NewLineHandling_9
            {
                this.AddChild(new CVariation(NewLineHandling_9) { Attribute = new Variation("Test for CR LF (xD xA) inside attr when NewLineHandling = Replace") { id = 9, Pri = 0 } });
            }


            // for function NewLineHandling_10
            {
                this.AddChild(new CVariation(NewLineHandling_10) { Attribute = new Variation("Test for CR (xD) inside attr when NewLineHandling = Entitize") { id = 10, Pri = 0 } });
            }


            // for function NewLineHandling_11
            {
                this.AddChild(new CVariation(NewLineHandling_11) { Attribute = new Variation("Test for LF (xA) inside attr when NewLineHandling = Entitize") { id = 11, Pri = 0 } });
            }


            // for function NewLineHandling_12
            {
                this.AddChild(new CVariation(NewLineHandling_12) { Attribute = new Variation("Test for CR LF (xD xA) inside attr when NewLineHandling = Entitize") { id = 12, Pri = 0 } });
            }


            // for function NewLineHandling_13
            {
                this.AddChild(new CVariation(NewLineHandling_13) { Attribute = new Variation("Bug 110270: Factory-created writers do not entitize 0xD character in text content when NewLineHandling=Entitize") { id = 13 } });
            }
        }
    }
}
