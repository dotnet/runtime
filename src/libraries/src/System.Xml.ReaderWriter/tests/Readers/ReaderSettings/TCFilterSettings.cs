// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCFilterSettings : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCFilterSettings
        // Test Case
        public override void AddChildren()
        {
            // for function pi00
            {
                this.AddChild(new CVariation(pi00) { Attribute = new Variation("FilterSettings Default Values") { Pri = 0 } });
            }


            // for function pi01
            {
                this.AddChild(new CVariation(pi01) { Attribute = new Variation("IgnorePI with setting true and false") { Pri = 0 } });
            }


            // for function pi02
            {
                this.AddChild(new CVariation(pi02) { Attribute = new Variation("IgnorePI and invalid PI in XML") { Pri = 0 } });
            }


            // for function pi03
            {
                this.AddChild(new CVariation(pi03) { Attribute = new Variation("IgnorePI and XmlDecl") { Pri = 2 } });
            }


            // for function c03
            {
                this.AddChild(new CVariation(c03) { Attribute = new Variation("IgnoreComments and escaped end comment") { Pri = 2 } });
            }


            // for function c01
            {
                this.AddChild(new CVariation(c01) { Attribute = new Variation("IgnoreComments with setting true and false") { Pri = 0 } });
            }


            // for function c02
            {
                this.AddChild(new CVariation(c02) { Attribute = new Variation("IgnoreComments and invalid comment in XML") { Pri = 0 } });
            }


            // for function w01
            {
                this.AddChild(new CVariation(w01) { Attribute = new Variation("IgnoreWhitespace with setting true and false") { Pri = 0 } });
            }


            // for function w02
            {
                this.AddChild(new CVariation(w02) { Attribute = new Variation("IgnoreWhitespace and Xml:Space=preserve") { Pri = 0 } });
            }


            // for function w03
            {
                this.AddChild(new CVariation(w03) { Attribute = new Variation("IgnoreWhitespace and invalid whitespace in XML") { Pri = 0 } });
            }
        }
    }
}
