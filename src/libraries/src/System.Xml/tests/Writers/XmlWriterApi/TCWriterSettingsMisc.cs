// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCWriterSettingsMisc : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCWriterSettingsMisc
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
