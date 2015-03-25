// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCXmlLang : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCXmlLang
        // Test Case
        public override void AddChildren()
        {
            // for function TestXmlLang1
            {
                this.AddChild(new CVariation(TestXmlLang1) { Attribute = new Variation("XmlLang test within EmptyTag") });
            }


            // for function TestXmlLang2
            {
                this.AddChild(new CVariation(TestXmlLang2) { Attribute = new Variation("XmlLang test within a scope (no nested element)") { Pri = 0 } });
            }


            // for function TestXmlLang3
            {
                this.AddChild(new CVariation(TestXmlLang3) { Attribute = new Variation("XmlLang test within a scope (with nested element)") { Pri = 0 } });
            }


            // for function TestXmlLang4
            {
                this.AddChild(new CVariation(TestXmlLang4) { Attribute = new Variation("XmlLang test immediately outside the XmlLang scope") });
            }


            // for function TestXmlLang5
            {
                this.AddChild(new CVariation(TestXmlLang5) { Attribute = new Variation("XmlLang test with multiple XmlLang declaration") });
            }


            // for function TestXmlLang6
            {
                this.AddChild(new CVariation(TestXmlLang6) { Attribute = new Variation("XmlLang valid values") { Pri = 0 } });
            }


            // for function TestXmlTextReaderLang1
            {
                this.AddChild(new CVariation(TestXmlTextReaderLang1) { Attribute = new Variation("More XmlLang valid values") });
            }
        }
    }
}
