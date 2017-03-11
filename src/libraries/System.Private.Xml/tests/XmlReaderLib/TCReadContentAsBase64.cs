// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadContentAsBase64 : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadContentAsBase64
        // Test Case
        public override void AddChildren()
        {
            // for function TestReadBase64_1
            {
                this.AddChild(new CVariation(TestReadBase64_1) { Attribute = new Variation("ReadBase64 Element with all valid value") });
            }


            // for function TestReadBase64_2
            {
                this.AddChild(new CVariation(TestReadBase64_2) { Attribute = new Variation("ReadBase64 Element with all valid Num value") { Pri = 0 } });
            }


            // for function TestReadBase64_3
            {
                this.AddChild(new CVariation(TestReadBase64_3) { Attribute = new Variation("ReadBase64 Element with all valid Text value") });
            }


            // for function TestReadBase64_5
            {
                this.AddChild(new CVariation(TestReadBase64_5) { Attribute = new Variation("ReadBase64 Element with all valid value (from concatenation), Pri=0") });
            }


            // for function TestReadBase64_6
            {
                this.AddChild(new CVariation(TestReadBase64_6) { Attribute = new Variation("ReadBase64 Element with Long valid value (from concatenation), Pri=0") });
            }


            // for function ReadBase64_7
            {
                this.AddChild(new CVariation(ReadBase64_7) { Attribute = new Variation("ReadBase64 with count > buffer size") });
            }


            // for function ReadBase64_8
            {
                this.AddChild(new CVariation(ReadBase64_8) { Attribute = new Variation("ReadBase64 with count < 0") });
            }


            // for function ReadBase64_9
            {
                this.AddChild(new CVariation(ReadBase64_9) { Attribute = new Variation("ReadBase64 with index > buffer size") });
            }


            // for function ReadBase64_10
            {
                this.AddChild(new CVariation(ReadBase64_10) { Attribute = new Variation("ReadBase64 with index < 0") });
            }


            // for function ReadBase64_11
            {
                this.AddChild(new CVariation(ReadBase64_11) { Attribute = new Variation("ReadBase64 with index + count exceeds buffer") });
            }


            // for function ReadBase64_12
            {
                this.AddChild(new CVariation(ReadBase64_12) { Attribute = new Variation("ReadBase64 index & count =0") });
            }


            // for function TestReadBase64_13
            {
                this.AddChild(new CVariation(TestReadBase64_13) { Attribute = new Variation("ReadBase64 Element multiple into same buffer (using offset), Pri=0") });
            }


            // for function TestReadBase64_14
            {
                this.AddChild(new CVariation(TestReadBase64_14) { Attribute = new Variation("ReadBase64 with buffer == null") });
            }


            // for function TestReadBase64_15
            {
                this.AddChild(new CVariation(TestReadBase64_15) { Attribute = new Variation("ReadBase64 after failure") });
            }


            // for function TestReadBase64_16
            {
                this.AddChild(new CVariation(TestReadBase64_16) { Attribute = new Variation("Read after partial ReadBase64") { Pri = 0 } });
            }


            // for function TestReadBase64_17
            {
                this.AddChild(new CVariation(TestReadBase64_17) { Attribute = new Variation("Current node on multiple calls") });
            }


            // for function TestTextReadBase64_23
            {
                this.AddChild(new CVariation(TestTextReadBase64_23) { Attribute = new Variation("ReadBase64 with incomplete sequence") });
            }


            // for function TestTextReadBase64_24
            {
                this.AddChild(new CVariation(TestTextReadBase64_24) { Attribute = new Variation("ReadBase64 when end tag doesn't exist") });
            }


            // for function TestTextReadBase64_26
            {
                this.AddChild(new CVariation(TestTextReadBase64_26) { Attribute = new Variation("ReadBase64 with whitespace in the middle") });
            }


            // for function TestTextReadBase64_27
            {
                this.AddChild(new CVariation(TestTextReadBase64_27) { Attribute = new Variation("ReadBase64 with = in the middle") });
            }


            // for function ReadBase64BufferOverflowWorksProperly
            {
                this.AddChild(new CVariation(ReadBase64BufferOverflowWorksProperly) { Attribute = new Variation("105376: ReadBase64 runs into an Overflow") { Params = new object[] { "10000" } } });
                this.AddChild(new CVariation(ReadBase64BufferOverflowWorksProperly) { Attribute = new Variation("105376: ReadBase64 runs into an Overflow") { Params = new object[] { "10000000" } } });
                this.AddChild(new CVariation(ReadBase64BufferOverflowWorksProperly) { Attribute = new Variation("105376: ReadBase64 runs into an Overflow") { Params = new object[] { "1000000" } } });
            }
        }
    }
}
