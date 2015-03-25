// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCXmlnsPrefix : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCXmlnsPrefix
        // Test Case
        public override void AddChildren()
        {
            // for function TXmlnsPrefix1
            {
                this.AddChild(new CVariation(TXmlnsPrefix1) { Attribute = new Variation("NamespaceURI of xmlns:a attribute") { Pri = 0 } });
            }


            // for function TXmlnsPrefix2
            {
                this.AddChild(new CVariation(TXmlnsPrefix2) { Attribute = new Variation("NamespaceURI of element/attribute with xmlns attribute") { Pri = 0 } });
            }


            // for function TXmlnsPrefix3
            {
                this.AddChild(new CVariation(TXmlnsPrefix3) { Attribute = new Variation("LookupNamespace with xmlns prefix") });
            }


            // for function TXmlnsPrefix4
            {
                this.AddChild(new CVariation(TXmlnsPrefix4) { Attribute = new Variation("Define prefix for 'www.w3.org/2000/xmlns'") { Pri = 0 } });
            }


            // for function TXmlnsPrefix5
            {
                this.AddChild(new CVariation(TXmlnsPrefix5) { Attribute = new Variation("Redefine namespace attached to xmlns prefix") });
            }


            // for function TXmlnsPrefix6
            {
                this.AddChild(new CVariation(TXmlnsPrefix6) { Attribute = new Variation("False duplicates and non-duplicates possible in the XmlReader during attribute normalization") { Param = false } });
                this.AddChild(new CVariation(TXmlnsPrefix6) { Attribute = new Variation("False duplicates and non-duplicates possible in the XmlReader during attribute normalization") { Param = true } });
            }
        }
    }
}
