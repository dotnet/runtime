// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadContentAsBinHex : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadContentAsBinHex
        // Test Case
        public override void AddChildren()
        {
            // for function TestReadBinHex_1
            {
                this.AddChild(new CVariation(TestReadBinHex_1) { Attribute = new Variation("ReadBinHex Element with all valid value") });
            }


            // for function TestReadBinHex_2
            {
                this.AddChild(new CVariation(TestReadBinHex_2) { Attribute = new Variation("ReadBinHex Element with all valid Num value") { Pri = 0 } });
            }


            // for function TestReadBinHex_3
            {
                this.AddChild(new CVariation(TestReadBinHex_3) { Attribute = new Variation("ReadBinHex Element with all valid Text value") });
            }


            // for function TestReadBinHex_4
            {
                this.AddChild(new CVariation(TestReadBinHex_4) { Attribute = new Variation("ReadBinHex Element on CDATA") { Pri = 0 } });
            }


            // for function TestReadBinHex_5
            {
                this.AddChild(new CVariation(TestReadBinHex_5) { Attribute = new Variation("ReadBinHex Element with all valid value (from concatenation), Pri=0") });
            }


            // for function TestReadBinHex_6
            {
                this.AddChild(new CVariation(TestReadBinHex_6) { Attribute = new Variation("ReadBinHex Element with all long valid value (from concatenation)") });
            }


            // for function TestReadBinHex_7
            {
                this.AddChild(new CVariation(TestReadBinHex_7) { Attribute = new Variation("ReadBinHex with count > buffer size") });
            }


            // for function TestReadBinHex_8
            {
                this.AddChild(new CVariation(TestReadBinHex_8) { Attribute = new Variation("ReadBinHex with count < 0") });
            }


            // for function vReadBinHex_9
            {
                this.AddChild(new CVariation(vReadBinHex_9) { Attribute = new Variation("ReadBinHex with index > buffer size") });
            }


            // for function TestReadBinHex_10
            {
                this.AddChild(new CVariation(TestReadBinHex_10) { Attribute = new Variation("ReadBinHex with index < 0") });
            }


            // for function TestReadBinHex_11
            {
                this.AddChild(new CVariation(TestReadBinHex_11) { Attribute = new Variation("ReadBinHex with index + count exceeds buffer") });
            }


            // for function TestReadBinHex_12
            {
                this.AddChild(new CVariation(TestReadBinHex_12) { Attribute = new Variation("ReadBinHex index & count =0") });
            }


            // for function TestReadBinHex_13
            {
                this.AddChild(new CVariation(TestReadBinHex_13) { Attribute = new Variation("ReadBinHex Element multiple into same buffer (using offset), Pri=0") });
            }


            // for function TestReadBinHex_14
            {
                this.AddChild(new CVariation(TestReadBinHex_14) { Attribute = new Variation("ReadBinHex with buffer == null") });
            }


            // for function TestReadBinHex_16
            {
                this.AddChild(new CVariation(TestReadBinHex_16) { Attribute = new Variation("Read after partial ReadBinHex") });
            }


            // for function TestReadBinHex_17
            {
                this.AddChild(new CVariation(TestReadBinHex_17) { Attribute = new Variation("Current node on multiple calls") });
            }


            // for function TestTextReadBinHex_21
            {
                this.AddChild(new CVariation(TestTextReadBinHex_21) { Attribute = new Variation("ReadBinHex with whitespace") });
            }


            // for function TestTextReadBinHex_22
            {
                this.AddChild(new CVariation(TestTextReadBinHex_22) { Attribute = new Variation("ReadBinHex with odd number of chars") });
            }


            // for function TestTextReadBinHex_23
            {
                this.AddChild(new CVariation(TestTextReadBinHex_23) { Attribute = new Variation("ReadBinHex when end tag doesn't exist") });
            }


            // for function TestTextReadBinHex_24
            {
                this.AddChild(new CVariation(TestTextReadBinHex_24) { Attribute = new Variation("Bug99148 - WS:WireCompat:hex binary fails to send/return data after 1787 bytes") });
            }
        }
    }
}
