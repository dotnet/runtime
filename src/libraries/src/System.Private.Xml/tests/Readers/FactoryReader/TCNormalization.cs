// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCNormalization : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.FactoryReaderTest.TCNormalization
        // Test Case
        public override void AddChildren()
        {
            // for function TestNormalization1
            {
                this.AddChild(new CVariation(TestNormalization1) { Attribute = new Variation("XmlTextReader Normalization - CRLF in Attribute value") { Pri = 0 } });
            }


            // for function TestNormalization2
            {
                this.AddChild(new CVariation(TestNormalization2) { Attribute = new Variation("XmlTextReader Normalization - CR in Attribute value") });
            }


            // for function TestNormalization3
            {
                this.AddChild(new CVariation(TestNormalization3) { Attribute = new Variation("XmlTextReader Normalization - LF in Attribute value") });
            }


            // for function TestNormalization4
            {
                this.AddChild(new CVariation(TestNormalization4) { Attribute = new Variation("XmlTextReader Normalization - multiple spaces in Attribute value") { Pri = 0 } });
            }


            // for function TestNormalization5
            {
                this.AddChild(new CVariation(TestNormalization5) { Attribute = new Variation("XmlTextReader Normalization - tab in Attribute value") { Pri = 0 } });
            }


            // for function TestNormalization6
            {
                this.AddChild(new CVariation(TestNormalization6) { Attribute = new Variation("XmlTextReader Normalization - CRLF in text node") { Pri = 0 } });
            }


            // for function TestNormalization7
            {
                this.AddChild(new CVariation(TestNormalization7) { Attribute = new Variation("XmlTextReader Normalization - CR in text node") });
            }


            // for function TestNormalization8
            {
                this.AddChild(new CVariation(TestNormalization8) { Attribute = new Variation("XmlTextReader Normalization - LF in text node") });
            }


            // for function TestNormalization9
            {
                this.AddChild(new CVariation(TestNormalization9) { Attribute = new Variation("XmlTextReader Normalization = true with invalid chars") { Pri = 0 } });
            }


            // for function TestNormalization11
            {
                this.AddChild(new CVariation(TestNormalization11) { Attribute = new Variation("Line breaks normalization in document entity") });
            }


            // for function TestNormalization14
            {
                this.AddChild(new CVariation(TestNormalization14) { Attribute = new Variation("XmlTextReader Normalization = true with invalid chars") });
            }


            // for function TestNormalization16
            {
                this.AddChild(new CVariation(TestNormalization16) { Attribute = new Variation("Character entities with Normalization=true") });
            }


            // for function TestNormalization17
            {
                this.AddChild(new CVariation(TestNormalization17) { Attribute = new Variation("Character entities with in text nodes") });
            }
        }
    }
}
