// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCDefaultWriterSettings : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCDefaultWriterSettings
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function default_1
            {
                this.AddChild(new CVariation(default_1) { Attribute = new Variation("Default value of Encoding") { id = 1 } });
            }


            // for function default_2
            {
                this.AddChild(new CVariation(default_2) { Attribute = new Variation("Default value of OmitXmlDeclaration") { id = 2 } });
            }


            // for function default_3
            {
                this.AddChild(new CVariation(default_3) { Attribute = new Variation("Default value of NewLineHandling") { id = 3 } });
            }


            // for function default_4
            {
                this.AddChild(new CVariation(default_4) { Attribute = new Variation("Default value of NewLineChars") { id = 4 } });
            }


            // for function default_5
            {
                this.AddChild(new CVariation(default_5) { Attribute = new Variation("Default value of Indent") { id = 5 } });
            }


            // for function default_6
            {
                this.AddChild(new CVariation(default_6) { Attribute = new Variation("Default value of IndentChars") { id = 6 } });
            }


            // for function default_7
            {
                this.AddChild(new CVariation(default_7) { Attribute = new Variation("Default value of NewLineOnAttributes") { id = 7 } });
            }


            // for function default_8
            {
                this.AddChild(new CVariation(default_8) { Attribute = new Variation("Default value of CloseOutput") { id = 8 } });
            }


            // for function default_10
            {
                this.AddChild(new CVariation(default_10) { Attribute = new Variation("Default value of CheckCharacters") { id = 10 } });
            }


            // for function default_11
            {
                this.AddChild(new CVariation(default_11) { Attribute = new Variation("Default value of ConformanceLevel") { id = 11 } });
            }


            // for function default_13
            {
                this.AddChild(new CVariation(default_13) { Attribute = new Variation("Default value of WriteEndDocumentOnClose") { id = 13 } });
            }
        }
    }
}
