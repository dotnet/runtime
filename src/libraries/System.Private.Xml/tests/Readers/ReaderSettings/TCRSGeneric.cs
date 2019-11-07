// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCRSGeneric : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCRSGeneric
        // Test Case
        public override void AddChildren()
        {
            // for function v1
            {
                this.AddChild(new CVariation(v1) { Attribute = new Variation("ReaderSettings not null") { Priority = 0 } });
            }


            // for function WrappingScenario
            {
                this.AddChild(new CVariation(WrappingScenario) { Attribute = new Variation("Wrapping scenario") });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("Reset") { Priority = 0 } });
            }


            // for function v4
            {
                this.AddChild(new CVariation(v4) { Attribute = new Variation("Clone") { Priority = 0 } });
            }


            // for function v5
            {
                this.AddChild(new CVariation(v5) { Attribute = new Variation("NameTable") { Priority = 0 } });
            }
        }
    }
}
