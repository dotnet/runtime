// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCFlushClose : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCFlushClose
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function flush_1
            {
                this.AddChild(new CVariation(flush_1) { Attribute = new Variation("Verify Flush() flushes underlying stream when CloseOutput = false") { Param = "false", id = 2, Pri = 1 } });
            }


            // for function close_1
            {
                this.AddChild(new CVariation(close_1) { Attribute = new Variation("Verify Close() flushes underlying stream when CloseOutput = false") { Param = "false", id = 4, Pri = 1 } });
            }


            // for function close_2
            {
                this.AddChild(new CVariation(close_2) { Attribute = new Variation("Verify WriterSettings after Close()") { id = 5, Pri = 1 } });
            }
        }
    }
}
