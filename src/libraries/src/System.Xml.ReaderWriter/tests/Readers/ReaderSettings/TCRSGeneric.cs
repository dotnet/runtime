// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;
using XmlReaderTest.Common;

namespace XmlReaderTest.ReaderSettingsTest
{
    public partial class TCRSGeneric : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.ReaderSettingsTest.TCRSGeneric
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
