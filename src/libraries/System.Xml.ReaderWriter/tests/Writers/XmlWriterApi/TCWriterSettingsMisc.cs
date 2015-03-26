// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace XmlWriterAPI.Test
{
    public partial class TCWriterSettingsMisc : XmlFactoryWriterTestCaseBase
    {
        // Type is XmlWriterAPI.Test.TCWriterSettingsMisc
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function Reset_1
            {
                this.AddChild(new CVariation(Reset_1) { Attribute = new Variation("Test for Reset()") { id = 1, Pri = 0 } });
            }


            // for function Clone_1
            {
                this.AddChild(new CVariation(Clone_1) { Attribute = new Variation("Test for Clone()") { id = 2, Pri = 0 } });
            }
        }
    }
}
