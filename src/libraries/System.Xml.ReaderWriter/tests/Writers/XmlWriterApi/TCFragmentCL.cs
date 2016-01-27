// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCFragmentCL : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCFragmentCL
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function frag_1
            {
                this.AddChild(new CVariation(frag_1) { Attribute = new Variation("Multiple root elements should be allowed") { id = 1, Pri = 1 } });
            }


            // for function frag_2
            {
                this.AddChild(new CVariation(frag_2) { Attribute = new Variation("Top level text should be allowed - PROLOG") { id = 2, Pri = 1 } });
            }


            // for function frag_3
            {
                this.AddChild(new CVariation(frag_3) { Attribute = new Variation("Top level text should be allowed - EPILOG") { id = 3, Pri = 1 } });
            }


            // for function frag_4
            {
                this.AddChild(new CVariation(frag_4) { Attribute = new Variation("Top level atomic value should be allowed - PROLOG") { id = 4, Pri = 1 } });
            }


            // for function frag_5
            {
                this.AddChild(new CVariation(frag_5) { Attribute = new Variation("Top level atomic value should be allowed - EPILOG") { id = 5, Pri = 1 } });
            }


            // for function frag_6
            {
                this.AddChild(new CVariation(frag_6) { Attribute = new Variation("Multiple top level atomic values") { id = 6, Pri = 1 } });
            }


            // for function frag_7
            {
                this.AddChild(new CVariation(frag_7) { Attribute = new Variation("WriteDocType should error when CL=fragment") { id = 7, Pri = 1 } });
            }


            // for function frag_8
            {
                this.AddChild(new CVariation(frag_8) { Attribute = new Variation("WriteStartDocument() should error when CL=fragment") { id = 8, Pri = 1 } });
            }
        }
    }
}
