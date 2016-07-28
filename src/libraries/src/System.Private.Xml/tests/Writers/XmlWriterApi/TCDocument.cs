// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCDocument : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCDocument
        // Test Case
        public override void AddChildren()
        {
            // for function document_1
            {
                this.AddChild(new CVariation(document_1) { Attribute = new Variation("StartDocument-EndDocument Sanity Test") { id = 1, Pri = 0 } });
            }


            // for function document_2
            {
                this.AddChild(new CVariation(document_2) { Attribute = new Variation("Multiple StartDocument should error") { id = 2, Pri = 1 } });
            }


            // for function document_3
            {
                this.AddChild(new CVariation(document_3) { Attribute = new Variation("Missing StartDocument should be fixed") { id = 3, Pri = 1 } });
            }


            // for function document_4
            {
                this.AddChild(new CVariation(document_4) { Attribute = new Variation("Multiple EndDocument should error") { id = 4, Pri = 1 } });
            }


            // for function document_5
            {
                this.AddChild(new CVariation(document_5) { Attribute = new Variation("Missing EndDocument should be fixed") { id = 5, Pri = 1 } });
            }


            // for function document_6
            {
                this.AddChild(new CVariation(document_6) { Attribute = new Variation("Call Start-EndDocument multiple times, should error") { id = 6, Pri = 2 } });
            }


            // for function document_7
            {
                this.AddChild(new CVariation(document_7) { Attribute = new Variation("Multiple root elements should error") { id = 7, Pri = 1 } });
            }


            // for function document_8
            {
                this.AddChild(new CVariation(document_8) { Attribute = new Variation("Start-EndDocument without any element should error") { id = 8, Pri = 2 } });
            }


            // for function document_9
            {
                this.AddChild(new CVariation(document_9) { Attribute = new Variation("Top level text should error - PROLOG") { id = 9, Pri = 1 } });
            }


            // for function document_10
            {
                this.AddChild(new CVariation(document_10) { Attribute = new Variation("Top level text should error - EPILOG") { id = 10, Pri = 1 } });
            }


            // for function document_11
            {
                this.AddChild(new CVariation(document_11) { Attribute = new Variation("Top level atomic value should error - PROLOG") { id = 11, Pri = 1 } });
            }


            // for function document_12
            {
                this.AddChild(new CVariation(document_12) { Attribute = new Variation("Top level atomic value should error - EPILOG") { id = 12, Pri = 1 } });
            }
        }
    }
}
