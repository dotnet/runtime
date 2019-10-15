// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCThisName : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCThisName
        // Test Case
        public override void AddChildren()
        {
            // for function ThisWithName1
            {
                this.AddChild(new CVariation(ThisWithName1) { Attribute = new Variation("This[Name] Verify with GetAttribute(Name)") { Pri = 0 } });
            }


            // for function ThisWithName2
            {
                this.AddChild(new CVariation(ThisWithName2) { Attribute = new Variation("This[Name, null] Verify with GetAttribute(Name)") });
            }


            // for function ThisWithName3
            {
                this.AddChild(new CVariation(ThisWithName3) { Attribute = new Variation("This[Name] Verify with GetAttribute(Name,null)") });
            }


            // for function ThisWithName4
            {
                this.AddChild(new CVariation(ThisWithName4) { Attribute = new Variation("This[Name, NamespaceURI] Verify with GetAttribute(Name, NamespaceURI)") { Pri = 0 } });
            }


            // for function ThisWithName5
            {
                this.AddChild(new CVariation(ThisWithName5) { Attribute = new Variation("This[Name, null] Verify not the same as GetAttribute(Name, NamespaceURI)") });
            }


            // for function ThisWithName6
            {
                this.AddChild(new CVariation(ThisWithName6) { Attribute = new Variation("This[Name, NamespaceURI] Verify not the same as GetAttribute(Name, null)") });
            }


            // for function ThisWithName7
            {
                this.AddChild(new CVariation(ThisWithName7) { Attribute = new Variation("This[Name] Verify with MoveToAttribute(Name)") { Pri = 0 } });
            }


            // for function ThisWithName8
            {
                this.AddChild(new CVariation(ThisWithName8) { Attribute = new Variation("This[Name, null] Verify with MoveToAttribute(Name)") });
            }


            // for function ThisWithName9
            {
                this.AddChild(new CVariation(ThisWithName9) { Attribute = new Variation("This[Name] Verify with MoveToAttribute(Name,null)") });
            }


            // for function ThisWithName10
            {
                this.AddChild(new CVariation(ThisWithName10) { Attribute = new Variation("This[Name, NamespaceURI] Verify not the same as MoveToAttribute(Name, null)") { Pri = 0 } });
            }


            // for function ThisWithName11
            {
                this.AddChild(new CVariation(ThisWithName11) { Attribute = new Variation("This[Name, null] Verify not the same as MoveToAttribute(Name, NamespaceURI)") });
            }


            // for function ThisWithName12
            {
                this.AddChild(new CVariation(ThisWithName12) { Attribute = new Variation("This[Name, namespace] Verify not the same as MoveToAttribute(Name, namespace)") });
            }


            // for function ThisWithName13
            {
                this.AddChild(new CVariation(ThisWithName13) { Attribute = new Variation("This(String.Empty)") });
            }


            // for function ThisWithName14
            {
                this.AddChild(new CVariation(ThisWithName14) { Attribute = new Variation("This[String.Empty,String.Empty]") });
            }


            // for function ThisWithName15
            {
                this.AddChild(new CVariation(ThisWithName15) { Attribute = new Variation("This[QName] Verify with GetAttribute(Name, NamespaceURI)") { Pri = 0 } });
            }


            // for function ThisWithName16
            {
                this.AddChild(new CVariation(ThisWithName16) { Attribute = new Variation("This[QName] invalid Qname") });
            }
        }
    }
}
