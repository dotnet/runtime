// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCXmlns : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCXmlns
        // Test Case
        public override void AddChildren()
        {
            // for function TXmlns1
            {
                this.AddChild(new CVariation(TXmlns1) { Attribute = new Variation("Name, LocalName, Prefix and Value with xmlns=ns attribute") { Pri = 0 } });
            }


            // for function TXmlns2
            {
                this.AddChild(new CVariation(TXmlns2) { Attribute = new Variation("Name, LocalName, Prefix and Value with xmlns:p=ns attribute") });
            }


            // for function TXmlns3
            {
                this.AddChild(new CVariation(TXmlns3) { Attribute = new Variation("LookupNamespace with xmlns=ns attribute") });
            }


            // for function TXmlns4
            {
                this.AddChild(new CVariation(TXmlns4) { Attribute = new Variation("MoveToAttribute access on xmlns attribute") });
            }


            // for function TXmlns5
            {
                this.AddChild(new CVariation(TXmlns5) { Attribute = new Variation("GetAttribute access on xmlns attribute") });
            }


            // for function TXmlns6
            {
                this.AddChild(new CVariation(TXmlns6) { Attribute = new Variation("this[xmlns] attribute access") });
            }
        }
    }
}
