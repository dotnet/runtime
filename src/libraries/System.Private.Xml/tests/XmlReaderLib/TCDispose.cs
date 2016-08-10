// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCDispose : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCDispose
        // Test Case
        public override void AddChildren()
        {
            // for function Variation1
            {
                this.AddChild(new CVariation(Variation1) { Attribute = new Variation("Test Integrity of all values after Dispose") });
            }


            // for function Variation2
            {
                this.AddChild(new CVariation(Variation2) { Attribute = new Variation("Call Dispose Multiple(3) Times") });
            }


            // for function Variation3
            {
                this.AddChild(new CVariation(Variation3) { Attribute = new Variation("Check Dispose with Exclusive Stream") });
            }
        }
    }
}
