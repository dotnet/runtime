// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCLinePos : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCLinePos
        // Test Case
        public override void AddChildren()
        {
            // for function TestLinePos1
            {
                this.AddChild(new CVariation(TestLinePos1) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = Element") { Priority = 0 } });
            }


            // for function TestLinePos2
            {
                this.AddChild(new CVariation(TestLinePos2) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = CDATA") { Priority = 0 } });
            }


            // for function TestLinePos4
            {
                this.AddChild(new CVariation(TestLinePos4) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = Comment") { Priority = 0 } });
            }


            // for function TestLinePos6
            {
                this.AddChild(new CVariation(TestLinePos6) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = EndElement") { Priority = 0 } });
            }


            // for function TestLinePos7
            {
                this.AddChild(new CVariation(TestLinePos7) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = EntityReference, not expanded") { Priority = 0 } });
            }


            // for function TestLinePos9
            {
                this.AddChild(new CVariation(TestLinePos9) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = ProcessingInstruction") { Priority = 0 } });
            }


            // for function TestLinePos10
            {
                this.AddChild(new CVariation(TestLinePos10) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = SignificantWhitespace") { Priority = 0 } });
            }


            // for function TestLinePos11
            {
                this.AddChild(new CVariation(TestLinePos11) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = Text") { Priority = 0 } });
            }


            // for function TestLinePos12
            {
                this.AddChild(new CVariation(TestLinePos12) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = Whitespace") { Priority = 0 } });
            }


            // for function TestLinePos13
            {
                this.AddChild(new CVariation(TestLinePos13) { Attribute = new Variation("LineNumber/LinePos after Read and NodeType = XmlDeclaration") { Priority = 0 } });
            }


            // for function TestLinePos14
            {
                this.AddChild(new CVariation(TestLinePos14) { Attribute = new Variation("LineNumber/LinePos after MoveToElement") });
            }


            // for function TestLinePos15
            {
                this.AddChild(new CVariation(TestLinePos15) { Attribute = new Variation("LineNumber/LinePos after MoveToFirstAttribute/MoveToNextAttribute") });
            }


            // for function TestLinePos16
            {
                this.AddChild(new CVariation(TestLinePos16) { Attribute = new Variation("LineNumber/LinePos after MoveToAttribute") });
            }


            // for function TestLinePos18
            {
                this.AddChild(new CVariation(TestLinePos18) { Attribute = new Variation("LineNumber/LinePos after Skip") });
            }


            // for function TestLinePos19
            {
                this.AddChild(new CVariation(TestLinePos19) { Attribute = new Variation("LineNumber/LinePos after ReadInnerXml") });
            }


            // for function TestLinePos20
            {
                this.AddChild(new CVariation(TestLinePos20) { Attribute = new Variation("LineNumber/LinePos after MoveToContent") });
            }


            // for function TestLinePos21
            {
                this.AddChild(new CVariation(TestLinePos21) { Attribute = new Variation("LineNumber/LinePos after ReadBase64 successive calls") });
            }


            // for function TestLinePos22
            {
                this.AddChild(new CVariation(TestLinePos22) { Attribute = new Variation("LineNumber/LinePos after ReadBinHex successive calls") });
            }


            // for function TestLinePos26
            {
                this.AddChild(new CVariation(TestLinePos26) { Attribute = new Variation("LineNumber/LinePos after ReadEndElement") });
            }


            // for function TestLinePos27
            {
                this.AddChild(new CVariation(TestLinePos27) { Attribute = new Variation("LineNumber/LinePos after ReadString") });
            }


            // for function TestLinePos39
            {
                this.AddChild(new CVariation(TestLinePos39) { Attribute = new Variation("LineNumber/LinePos after element containing entities in attribute values") });
            }


            // for function TestLinePos40
            {
                this.AddChild(new CVariation(TestLinePos40) { Attribute = new Variation("LineNumber/LinePos when Read = false") });
            }


            // for function TestLinePos41
            {
                this.AddChild(new CVariation(TestLinePos41) { Attribute = new Variation("XmlTextReader:LineNumber and LinePos don't return the right position after ReadInnerXml is called") });
            }


            // for function TestLinePos42
            {
                this.AddChild(new CVariation(TestLinePos42) { Attribute = new Variation("XmlTextReader: LineNum and LinePosition incorrect for EndTag token and text element") });
            }


            // for function TestLinePos43
            {
                this.AddChild(new CVariation(TestLinePos43) { Attribute = new Variation("Bogus LineNumber value when reading attribute over XmlTextReader") });
            }


            // for function TestLinePos44
            {
                this.AddChild(new CVariation(TestLinePos44) { Attribute = new Variation("LineNumber and LinePosition on attribute with columns") });
            }


            // for function TestLinePos45
            {
                this.AddChild(new CVariation(TestLinePos45) { Attribute = new Variation("HasLineInfo") });
            }


            // for function TestLinePos99
            {
                this.AddChild(new CVariation(TestLinePos99) { Attribute = new Variation("XmlException LineNumber and LinePosition") });
            }


            // for function ReadingNonWellFormedXmlThrows
            {
                this.AddChild(new CVariation(ReadingNonWellFormedXmlThrows) { Attribute = new Variation("Check error message on a non-wellformed XML") });
            }


            // for function XmlExceptionAndXmlTextReaderLineNumberShouldBeSameAfterExceptionIsThrown
            {
                this.AddChild(new CVariation(XmlExceptionAndXmlTextReaderLineNumberShouldBeSameAfterExceptionIsThrown) { Attribute = new Variation("When an XmlException is thrown both XmlException.LineNumber and XmlTextReader.LineNumber should be same") });
            }


            // for function XmlReaderShouldIncreaseLineNumberAfterNewLineInElementTag
            {
                this.AddChild(new CVariation(XmlReaderShouldIncreaseLineNumberAfterNewLineInElementTag) { Attribute = new Variation("Xml(Text)Reader does not increase line number for a new line in element end tag") });
            }


            // for function LineNumberAndLinePositionAreCorrect
            {
                this.AddChild(new CVariation(LineNumberAndLinePositionAreCorrect) { Attribute = new Variation("LineNumber and LinePosition are not correct") });
            }
        }
    }
}
