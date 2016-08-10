// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCErrorState : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCErrorState
        // Test Case
        public override void AddChildren()
        {
            // for function state_1
            {
                this.AddChild(new CVariation(state_1) { Attribute = new Variation("EntityRef after Document should error - PROLOG") { id = 1, Pri = 1 } });
            }


            // for function state_2
            {
                this.AddChild(new CVariation(state_2) { Attribute = new Variation("EntityRef after Document should error - EPILOG") { id = 2, Pri = 1 } });
            }


            // for function state_3
            {
                this.AddChild(new CVariation(state_3) { Attribute = new Variation("CharEntity after Document should error - PROLOG") { id = 3, Pri = 1 } });
            }


            // for function state_4
            {
                this.AddChild(new CVariation(state_4) { Attribute = new Variation("CharEntity after Document should error - EPILOG") { id = 4, Pri = 1 } });
            }


            // for function state_5
            {
                this.AddChild(new CVariation(state_5) { Attribute = new Variation("SurrogateCharEntity after Document should error - PROLOG") { id = 5, Pri = 1 } });
            }


            // for function state_6
            {
                this.AddChild(new CVariation(state_6) { Attribute = new Variation("SurrogateCharEntity after Document should error - EPILOG") { id = 6, Pri = 1 } });
            }


            // for function state_7
            {
                this.AddChild(new CVariation(state_7) { Attribute = new Variation("Attribute after Document should error - PROLOG") { id = 7, Pri = 1 } });
            }


            // for function state_8
            {
                this.AddChild(new CVariation(state_8) { Attribute = new Variation("Attribute after Document should error - EPILOG") { id = 8, Pri = 1 } });
            }


            // for function state_9
            {
                this.AddChild(new CVariation(state_9) { Attribute = new Variation("CDATA after Document should error - PROLOG") { id = 9, Pri = 1 } });
            }


            // for function state_10
            {
                this.AddChild(new CVariation(state_10) { Attribute = new Variation("CDATA after Document should error - EPILOG") { id = 10, Pri = 1 } });
            }


            // for function state_11
            {
                this.AddChild(new CVariation(state_11) { Attribute = new Variation("Element followed by Document should error") { id = 11, Pri = 1 } });
            }


            // for function state_12
            {
                this.AddChild(new CVariation(state_12) { Attribute = new Variation("Element followed by DocType should error") { id = 12, Pri = 1 } });
            }
        }
    }
}
