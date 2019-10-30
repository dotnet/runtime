// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadElementContentAsBinHex : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadElementContentAsBinHex
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
                this.AddChild(new CVariation(TestReadBinHex_4) { Attribute = new Variation("ReadBinHex Element with Comments and PIs") { Pri = 0 } });
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


            // for function TestReadBinHex_18
            {
                this.AddChild(new CVariation(TestReadBinHex_18) { Attribute = new Variation("No op node types") });
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
                this.AddChild(new CVariation(TestTextReadBinHex_24) { Attribute = new Variation("WS:WireCompat:hex binary fails to send/return data after 1787 bytes") });
            }


            // for function TestReadBinHex_430329
            {
                this.AddChild(new CVariation(TestReadBinHex_430329) { Attribute = new Variation("SubtreeReader inserted attributes don't work with ReadContentAsBinHex") });
            }


            // for function TestReadBinHex_27
            {
                this.AddChild(new CVariation(TestReadBinHex_27) { Attribute = new Variation("ReadBinHex with = in the middle") });
            }


            // for function TestReadBinHex_105376
            {
                this.AddChild(new CVariation(TestReadBinHex_105376) { Attribute = new Variation("ReadBinHex runs into an Overflow") { Params = new object[] { "1000000" } } });
                this.AddChild(new CVariation(TestReadBinHex_105376) { Attribute = new Variation("ReadBinHex runs into an Overflow") { Params = new object[] { "10000000" } } });
            }


            // for function TestReadBinHex_28
            {
                this.AddChild(new CVariation(TestReadBinHex_28) { Attribute = new Variation("call ReadContentAsBinHex on two or more nodes") });
            }


            // for function TestReadBinHex_29
            {
                this.AddChild(new CVariation(TestReadBinHex_29) { Attribute = new Variation("read BinHex over invalid text node") });
            }


            // for function TestReadBinHex_30
            {
                this.AddChild(new CVariation(TestReadBinHex_30) { Attribute = new Variation("goto to text node, ask got.Value, readcontentasBinHex") });
            }


            // for function TestReadBinHex_31
            {
                this.AddChild(new CVariation(TestReadBinHex_31) { Attribute = new Variation("goto to text node, readcontentasBinHex, ask got.Value") });
            }


            // for function TestReadBinHex_32
            {
                this.AddChild(new CVariation(TestReadBinHex_32) { Attribute = new Variation("goto to huge text node, read several chars with ReadContentAsBinHex and Move forward with .Read()") });
            }


            // for function TestReadBinHex_33
            {
                this.AddChild(new CVariation(TestReadBinHex_33) { Attribute = new Variation("goto to huge text node with invalid chars, read several chars with ReadContentAsBinHex and Move forward with .Read()") });
            }


            // for function TestBinHex_34
            {
                this.AddChild(new CVariation(TestBinHex_34) { Attribute = new Variation("ReadContentAsBinHex on an xmlns attribute") { Param = "<foo xmlns='default'> <bar > id='1'/> </foo>" } });
                this.AddChild(new CVariation(TestBinHex_34) { Attribute = new Variation("ReadContentAsBinHex on an xmlns:k attribute") { Param = "<k:foo xmlns:k='default'> <k:bar id='1'/> </k:foo>" } });
                this.AddChild(new CVariation(TestBinHex_34) { Attribute = new Variation("ReadContentAsBinHex on an xml:space attribute") { Param = "<foo xml:space='default'> <bar > id='1'/> </foo>" } });
                this.AddChild(new CVariation(TestBinHex_34) { Attribute = new Variation("ReadContentAsBinHex on an xml:lang attribute") { Param = "<foo xml:lang='default'> <bar > id='1'/> </foo>" } });
            }


            // for function TestReadBinHex_35
            {
                this.AddChild(new CVariation(TestReadBinHex_35) { Attribute = new Variation("call ReadContentAsBinHex on two or more nodes and whitespace") });
            }


            // for function TestReadBinHex_36
            {
                this.AddChild(new CVariation(TestReadBinHex_36) { Attribute = new Variation("call ReadContentAsBinHex on two or more nodes and whitespace after call Value") });
            }
        }
    }
}
