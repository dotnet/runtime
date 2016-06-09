// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCAutoComplete : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCAutoComplete
        // Test Case
        public override void AddChildren()
        {
            // for function var_1
            {
                this.AddChild(new CVariation(var_1) { Attribute = new Variation("Missing EndAttr, followed by element") { id = 1, Pri = 1 } });
            }


            // for function var_2
            {
                this.AddChild(new CVariation(var_2) { Attribute = new Variation("Missing EndAttr, followed by comment") { id = 2, Pri = 1 } });
            }


            // for function var_3
            {
                this.AddChild(new CVariation(var_3) { Attribute = new Variation("Write EndDocument with unclosed element tag") { id = 3, Pri = 1 } });
            }


            // for function var_4
            {
                this.AddChild(new CVariation(var_4) { Attribute = new Variation("WriteStartDocument - WriteEndDocument") { id = 4, Pri = 1 } });
            }


            // for function var_5
            {
                this.AddChild(new CVariation(var_5) { Attribute = new Variation("WriteEndElement without WriteStartElement") { id = 5, Pri = 1 } });
            }


            // for function var_6
            {
                this.AddChild(new CVariation(var_6) { Attribute = new Variation("WriteFullEndElement without WriteStartElement") { id = 6, Pri = 1 } });
            }
        }
    }
}
