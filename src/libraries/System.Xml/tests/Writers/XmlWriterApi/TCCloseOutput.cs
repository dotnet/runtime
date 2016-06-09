// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCCloseOutput : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCCloseOutput
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function CloseOutput_1
            {
                this.AddChild(new CVariation(CloseOutput_1) { Attribute = new Variation("Check that underlying stream is NOT CLOSED when CloseOutput = FALSE and Create(TextWriter)") { Param = "Textwriter", id = 2, Pri = 0 } });
                this.AddChild(new CVariation(CloseOutput_1) { Attribute = new Variation("Check that underlying stream is NOT CLOSED when CloseOutput = FALSE and Create(Stream)") { Param = "Stream", id = 1, Pri = 0 } });
            }


            // for function CloseOutput_2
            {
                this.AddChild(new CVariation(CloseOutput_2) { Attribute = new Variation("Check that underlying stream is CLOSED when CloseOutput = FALSE and Create(Uri)") { Param = "false", id = 3, Pri = 0 } });
                this.AddChild(new CVariation(CloseOutput_2) { Attribute = new Variation("Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Uri)") { Param = "true", id = 4, Pri = 0 } });
            }


            // for function CloseOutput_3
            {
                this.AddChild(new CVariation(CloseOutput_3) { Attribute = new Variation("Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Textwriter)") { Param = "Textwriter", id = 6, Pri = 0 } });
                this.AddChild(new CVariation(CloseOutput_3) { Attribute = new Variation("Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Stream)") { Param = "Stream", id = 5, Pri = 0 } });
            }


            // for function CloseOutput_4
            {
                this.AddChild(new CVariation(CloseOutput_4) { Attribute = new Variation("Writer should not close underlying stream when an exception is thrown before Close (Textwriter)") { Param = "Textwriter", id = 8, Pri = 1 } });
                this.AddChild(new CVariation(CloseOutput_4) { Attribute = new Variation("Writer should not close underlying stream when an exception is thrown before Close (Stream)") { Param = "Stream", id = 7, Pri = 1 } });
            }
        }
    }
}
