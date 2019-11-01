// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCGetAttributeName : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCGetAttributeName
        // Test Case
        public override void AddChildren()
        {
            // for function GetAttributeWithName1
            {
                this.AddChild(new CVariation(GetAttributeWithName1) { Attribute = new Variation("GetAttribute(Name) Verify with This[Name]") { Pri = 0 } });
            }


            // for function GetAttributeWithName2
            {
                this.AddChild(new CVariation(GetAttributeWithName2) { Attribute = new Variation("GetAttribute(Name, null) Verify with This[Name]") });
            }


            // for function GetAttributeWithName3
            {
                this.AddChild(new CVariation(GetAttributeWithName3) { Attribute = new Variation("GetAttribute(Name) Verify with This[Name,null]") });
            }


            // for function GetAttributeWithName4
            {
                this.AddChild(new CVariation(GetAttributeWithName4) { Attribute = new Variation("GetAttribute(Name, NamespaceURI) Verify with This[Name, NamespaceURI]") { Pri = 0 } });
            }


            // for function GetAttributeWithName5
            {
                this.AddChild(new CVariation(GetAttributeWithName5) { Attribute = new Variation("GetAttribute(Name, null) Verify not the same as This[Name, NamespaceURI]") });
            }


            // for function GetAttributeWithName6
            {
                this.AddChild(new CVariation(GetAttributeWithName6) { Attribute = new Variation("GetAttribute(Name, NamespaceURI) Verify not the same as This[Name, null]") });
            }


            // for function GetAttributeWithName7
            {
                this.AddChild(new CVariation(GetAttributeWithName7) { Attribute = new Variation("GetAttribute(Name) Verify with MoveToAttribute(Name)") });
            }


            // for function GetAttributeWithName8
            {
                this.AddChild(new CVariation(GetAttributeWithName8) { Attribute = new Variation("GetAttribute(Name,null) Verify with MoveToAttribute(Name)") { Pri = 1 } });
            }


            // for function GetAttributeWithName9
            {
                this.AddChild(new CVariation(GetAttributeWithName9) { Attribute = new Variation("GetAttribute(Name) Verify with MoveToAttribute(Name,null)") { Pri = 1 } });
            }


            // for function GetAttributeWithName10
            {
                this.AddChild(new CVariation(GetAttributeWithName10) { Attribute = new Variation("GetAttribute(Name, NamespaceURI) Verify not the same as MoveToAttribute(Name, null)") });
            }


            // for function GetAttributeWithName11
            {
                this.AddChild(new CVariation(GetAttributeWithName11) { Attribute = new Variation("GetAttribute(Name, null) Verify not the same as MoveToAttribute(Name, NamespaceURI)") });
            }


            // for function GetAttributeWithName12
            {
                this.AddChild(new CVariation(GetAttributeWithName12) { Attribute = new Variation("GetAttribute(Name, namespace) Verify not the same as MoveToAttribute(Name, namespace)") });
            }


            // for function GetAttributeWithName13
            {
                this.AddChild(new CVariation(GetAttributeWithName13) { Attribute = new Variation("GetAttribute(String.Empty)") });
            }


            // for function GetAttributeWithName14
            {
                this.AddChild(new CVariation(GetAttributeWithName14) { Attribute = new Variation("GetAttribute(String.Empty,String.Empty)") });
            }
        }
    }
}
