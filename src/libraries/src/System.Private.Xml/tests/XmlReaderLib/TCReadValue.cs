// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCReadValue : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCReadValue
        // Test Case
        public override void AddChildren()
        {
            // for function TestReadValuePri0
            {
                this.AddChild(new CVariation(TestReadValuePri0) { Attribute = new Variation("ReadValue") { Pri = 0 } });
            }


            // for function TestReadValuePri0onElement
            {
                this.AddChild(new CVariation(TestReadValuePri0onElement) { Attribute = new Variation("ReadValue on Element") { Pri = 0 } });
            }


            // for function TestReadValueOnAttribute0
            {
                this.AddChild(new CVariation(TestReadValueOnAttribute0) { Attribute = new Variation("ReadValue on Attribute") { Pri = 0 } });
            }


            // for function TestReadValueOnAttribute1
            {
                this.AddChild(new CVariation(TestReadValueOnAttribute1) { Attribute = new Variation("ReadValue on Attribute after ReadAttributeValue") { Pri = 2 } });
            }


            // for function TestReadValue2Pri0
            {
                this.AddChild(new CVariation(TestReadValue2Pri0) { Attribute = new Variation("ReadValue on empty buffer") { Pri = 0 } });
            }


            // for function TestReadValue3Pri0
            {
                this.AddChild(new CVariation(TestReadValue3Pri0) { Attribute = new Variation("ReadValue on negative count") { Pri = 0 } });
            }


            // for function TestReadValue4Pri0
            {
                this.AddChild(new CVariation(TestReadValue4Pri0) { Attribute = new Variation("ReadValue on negative offset") { Pri = 0 } });
            }


            // for function TestReadValue1
            {
                this.AddChild(new CVariation(TestReadValue1) { Attribute = new Variation("ReadValue with buffer = element content / 2") { Pri = 0 } });
            }


            // for function TestReadValue2
            {
                this.AddChild(new CVariation(TestReadValue2) { Attribute = new Variation("ReadValue entire value in one call") { Pri = 0 } });
            }


            // for function TestReadValue3
            {
                this.AddChild(new CVariation(TestReadValue3) { Attribute = new Variation("ReadValue bit by bit") { Pri = 0 } });
            }


            // for function TestReadValue4
            {
                this.AddChild(new CVariation(TestReadValue4) { Attribute = new Variation("ReadValue for value more than 4K") { Pri = 0 } });
            }


            // for function TestReadValue5
            {
                this.AddChild(new CVariation(TestReadValue5) { Attribute = new Variation("ReadValue for value more than 4K and invalid element") { Pri = 1 } });
            }


            // for function TestReadValue7
            {
                this.AddChild(new CVariation(TestReadValue7) { Attribute = new Variation("ReadValue with count > buffer size") });
            }


            // for function TestReadValue8
            {
                this.AddChild(new CVariation(TestReadValue8) { Attribute = new Variation("ReadValue with index > buffer size") });
            }


            // for function TestReadValue10
            {
                this.AddChild(new CVariation(TestReadValue10) { Attribute = new Variation("ReadValue with index + count exceeds buffer") });
            }


            // for function TestReadChar11
            {
                this.AddChild(new CVariation(TestReadChar11) { Attribute = new Variation("ReadValue with combination Text, CDATA and Whitespace") });
            }


            // for function TestReadChar12
            {
                this.AddChild(new CVariation(TestReadChar12) { Attribute = new Variation("ReadValue with combination Text, CDATA and SignificantWhitespace") });
            }


            // for function TestReadChar13
            {
                this.AddChild(new CVariation(TestReadChar13) { Attribute = new Variation("ReadValue with buffer == null") });
            }


            // for function TestReadChar14
            {
                this.AddChild(new CVariation(TestReadChar14) { Attribute = new Variation("ReadValue with multiple different inner nodes") });
            }


            // for function TestReadChar15
            {
                this.AddChild(new CVariation(TestReadChar15) { Attribute = new Variation("ReadValue after failed ReadValue") });
            }


            // for function TestReadChar16
            {
                this.AddChild(new CVariation(TestReadChar16) { Attribute = new Variation("Read after partial ReadValue") });
            }


            // for function TestReadChar19
            {
                this.AddChild(new CVariation(TestReadChar19) { Attribute = new Variation("Test error after successful ReadValue") });
            }


            // for function TestReadChar21
            {
                this.AddChild(new CVariation(TestReadChar21) { Attribute = new Variation("Call on invalid element content after 4k boundary") { Params = new object[] { false, 4096 }, Pri = 1 } });
                this.AddChild(new CVariation(TestReadChar21) { Attribute = new Variation("Call on invalid element content after 64k boundary for Async") { Params = new object[] { true, 65536 }, Pri = 1 } });
            }


            // for function TestTextReadValue25
            {
                this.AddChild(new CVariation(TestTextReadValue25) { Attribute = new Variation("ReadValue with whitespace") });
            }


            // for function TestTextReadValue26
            {
                this.AddChild(new CVariation(TestTextReadValue26) { Attribute = new Variation("ReadValue when end tag doesn't exist") });
            }


            // for function TestCharEntities0
            {
                this.AddChild(new CVariation(TestCharEntities0) { Attribute = new Variation("Testing with character entities") });
            }


            // for function TestCharEntities1
            {
                this.AddChild(new CVariation(TestCharEntities1) { Attribute = new Variation("Testing with character entities when value more than 4k") });
            }


            // for function TestCharEntities2
            {
                this.AddChild(new CVariation(TestCharEntities2) { Attribute = new Variation("Testing with character entities with another pattern") });
            }


            // for function TestReadValueOnBig
            {
                this.AddChild(new CVariation(TestReadValueOnBig) { Attribute = new Variation("Testing a use case pattern with large file") });
            }


            // for function TestReadValueOnComments0
            {
                this.AddChild(new CVariation(TestReadValueOnComments0) { Attribute = new Variation("ReadValue on Comments with IgnoreComments") });
            }


            // for function TestReadValueOnPIs0
            {
                this.AddChild(new CVariation(TestReadValueOnPIs0) { Attribute = new Variation("ReadValue on PI with IgnorePI") });
            }


            // for function SkipAfterReadAttributeValueAndReadValueChunkDoesNotThrow
            {
                this.AddChild(new CVariation(SkipAfterReadAttributeValueAndReadValueChunkDoesNotThrow) { Attribute = new Variation("SQLBU 340158: Skip after ReadAttributeValue/ReadValueChunk") });
            }


            // for function ReadValueChunkDoesWorkProperlyOnAttributes
            {
                this.AddChild(new CVariation(ReadValueChunkDoesWorkProperlyOnAttributes) { Attribute = new Variation("TFS 107348:  ReadValueChunk - doesn't work correctly on attributes") });
            }


            // for function ReadValueChunkDoesWorkProperlyOnSpecialAttributes
            {
                this.AddChild(new CVariation(ReadValueChunkDoesWorkProperlyOnSpecialAttributes) { Attribute = new Variation("TFS 111470:  ReadValueChunk - doesn't work correctly on special attributes") });
            }


            // for function ReadValueChunkWorksProperlyWithSubtreeReaderInsertedAttributes
            {
                this.AddChild(new CVariation(ReadValueChunkWorksProperlyWithSubtreeReaderInsertedAttributes) { Attribute = new Variation("430329: SubtreeReader inserted attributes don't work with ReadValueChunk") });
            }


            // for function TestReadValue_1
            {
                this.AddChild(new CVariation(TestReadValue_1) { Attribute = new Variation("goto to text node, ask got.Value, ReadValueChunk") });
            }


            // for function TestReadValue_2
            {
                this.AddChild(new CVariation(TestReadValue_2) { Attribute = new Variation("goto to text node, ReadValueChunk, ask got.Value") });
            }


            // for function TestReadValue_3
            {
                this.AddChild(new CVariation(TestReadValue_3) { Attribute = new Variation("goto to huge text node, read several chars with ReadValueChank and Move forward with .Read()") });
            }


            // for function TestReadValue_4
            {
                this.AddChild(new CVariation(TestReadValue_4) { Attribute = new Variation("goto to huge text node with invalid chars, read several chars with ReadValueChank and Move forward with .Read()") });
            }


            // for function TestReadValue_5
            {
                this.AddChild(new CVariation(TestReadValue_5) { Attribute = new Variation("Call ReadValueChunk on two or more nodes") });
            }


            // for function TestReadValue_6
            {
                this.AddChild(new CVariation(TestReadValue_6) { Attribute = new Variation("ReadValueChunk on an xmlns:k attribute") { Param = "<k:foo xmlns:k='default'> <k:bar id='1'/> </k:foo>" } });
                this.AddChild(new CVariation(TestReadValue_6) { Attribute = new Variation("ReadValueChunk on an xmlns attribute") { Param = "<foo xmlns='default'> <bar > id='1'/> </foo>" } });
                this.AddChild(new CVariation(TestReadValue_6) { Attribute = new Variation("ReadValueChunk on an xml:lang attribute") { Param = "<foo xml:lang='default'> <bar > id='1'/> </foo>" } });
                this.AddChild(new CVariation(TestReadValue_6) { Attribute = new Variation("ReadValueChunk on an xml:space attribute") { Param = "<foo xml:space='default'> <bar > id='1'/> </foo>" } });
            }


            // for function TestReadValue_7
            {
                this.AddChild(new CVariation(TestReadValue_7) { Attribute = new Variation("Call ReadValueChunk on two or more nodes and whitespace") });
            }


            // for function TestReadValue_8
            {
                this.AddChild(new CVariation(TestReadValue_8) { Attribute = new Variation("Call ReadValueChunk on two or more nodes and whitespace after call Value") });
            }
        }
    }
}
