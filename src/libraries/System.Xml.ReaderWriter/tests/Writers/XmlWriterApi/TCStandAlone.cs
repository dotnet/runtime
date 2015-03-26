// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace XmlWriterAPI.Test
{
    public partial class TCStandAlone : XmlFactoryWriterTestCaseBase
    {
        // Type is XmlWriterAPI.Test.TCStandAlone
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function standalone_1
            {
                this.AddChild(new CVariation(standalone_1) { Attribute = new Variation("StartDocument(bool standalone = true)") { id = 1, Pri = 0 } });
            }


            // for function standalone_2
            {
                this.AddChild(new CVariation(standalone_2) { Attribute = new Variation("StartDocument(bool standalone = false)") { id = 2, Pri = 0 } });
            }
        }
    }
}
