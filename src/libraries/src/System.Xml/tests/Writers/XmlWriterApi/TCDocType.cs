// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCDocType : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCDocType
        // Test Case
        public override void AddChildren()
        {
            // for function docType_1
            {
                this.AddChild(new CVariation(docType_1) { Attribute = new Variation("Sanity test") { id = 1, Pri = 1 } });
            }


            // for function docType_2
            {
                this.AddChild(new CVariation(docType_2) { Attribute = new Variation("WriteDocType pubid = null and sysid = null") { id = 2, Pri = 1 } });
            }


            // for function docType_3
            {
                this.AddChild(new CVariation(docType_3) { Attribute = new Variation("Call WriteDocType twice") { id = 3, Pri = 1 } });
            }


            // for function docType_4
            {
                this.AddChild(new CVariation(docType_4) { Attribute = new Variation("WriteDocType with name value = String.Empty") { Param = "String.Empty", id = 4, Pri = 1 } });
                this.AddChild(new CVariation(docType_4) { Attribute = new Variation("WriteDocType with name value = null") { Param = "null", id = 5, Pri = 1 } });
            }


            // for function docType_5
            {
                this.AddChild(new CVariation(docType_5) { Attribute = new Variation("Bug 53726: WriteDocType with DocType end tag in the value") { id = 6, Pri = 1 } });
            }


            // for function docType_6
            {
                this.AddChild(new CVariation(docType_6) { Attribute = new Variation("Call WriteDocType in the root element") { id = 7, Pri = 1 } });
            }


            // for function docType_7
            {
                this.AddChild(new CVariation(docType_7) { Attribute = new Variation("Call WriteDocType following root element") { id = 8, Pri = 1 } });
            }
        }
    }
}
