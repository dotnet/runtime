// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCSkip : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCSkip
        // Test Case
        public override void AddChildren()
        {
            // for function TestSkip1
            {
                this.AddChild(new CVariation(TestSkip1) { Attribute = new Variation("Call Skip on empty element") { Pri = 0 } });
            }


            // for function TestSkip2
            {
                this.AddChild(new CVariation(TestSkip2) { Attribute = new Variation("Call Skip on element") { Pri = 0 } });
            }


            // for function TestSkip3
            {
                this.AddChild(new CVariation(TestSkip3) { Attribute = new Variation("Call Skip on element with content") { Pri = 0 } });
            }


            // for function TestSkip4
            {
                this.AddChild(new CVariation(TestSkip4) { Attribute = new Variation("Call Skip on text node (leave node)") { Pri = 0 } });
            }


            // for function skip307543
            {
                this.AddChild(new CVariation(skip307543) { Attribute = new Variation("Call Skip in while read loop") { Pri = 0 } });
            }


            // for function TestSkip5
            {
                this.AddChild(new CVariation(TestSkip5) { Attribute = new Variation("Call Skip on text node with another element: <elem2>text<elem3></elem3></elem2>") });
            }


            // for function TestSkip6
            {
                this.AddChild(new CVariation(TestSkip6) { Attribute = new Variation("Call Skip on attribute") { Pri = 0 } });
            }


            // for function TestSkip7
            {
                this.AddChild(new CVariation(TestSkip7) { Attribute = new Variation("Call Skip on text node of attribute") });
            }


            // for function TestSkip8
            {
                this.AddChild(new CVariation(TestSkip8) { Attribute = new Variation("Call Skip on CDATA") { Pri = 0 } });
            }


            // for function TestSkip9
            {
                this.AddChild(new CVariation(TestSkip9) { Attribute = new Variation("Call Skip on Processing Instruction") { Pri = 0 } });
            }


            // for function TestSkip10
            {
                this.AddChild(new CVariation(TestSkip10) { Attribute = new Variation("Call Skip on Comment") { Pri = 0 } });
            }


            // for function TestSkip12
            {
                this.AddChild(new CVariation(TestSkip12) { Attribute = new Variation("Call Skip on Whitespace") { Pri = 0 } });
            }


            // for function TestSkip13
            {
                this.AddChild(new CVariation(TestSkip13) { Attribute = new Variation("Call Skip on EndElement") { Pri = 0 } });
            }


            // for function TestSkip14
            {
                this.AddChild(new CVariation(TestSkip14) { Attribute = new Variation("Call Skip on root Element") });
            }


            // for function TestSkip15
            {
                this.AddChild(new CVariation(TestSkip15) { Attribute = new Variation("Call Skip on Entity Reference") { Pri = 0 } });
            }


            // for function TestTextSkip1
            {
                this.AddChild(new CVariation(TestTextSkip1) { Attribute = new Variation("Call Skip on general entity ref node of attribute") });
            }


            // for function XmlTextReaderDoesHandleAmpersands
            {
                this.AddChild(new CVariation(XmlTextReaderDoesHandleAmpersands) { Attribute = new Variation("320154 : XmlTextReader ArgumentOutOfRangeException when handling ampersands") });
            }
        }
    }
}
