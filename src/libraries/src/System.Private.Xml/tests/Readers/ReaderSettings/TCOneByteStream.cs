// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCOneByteStream : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCOneByteStream
        // Test Case
        public override void AddChildren()
        {
            // for function v0
            {
                this.AddChild(new CVariation(v0) { Attribute = new Variation("445370: Parsing this 'some]' as fragment fails with 'Unexpected EOF' error") });
            }


            // for function v0a
            {
                this.AddChild(new CVariation(v0a) { Attribute = new Variation("445370a: Parsing this 'some]' as fragment fails with 'Unexpected EOF' error") });
            }


            // for function v1
            {
                this.AddChild(new CVariation(v1) { Attribute = new Variation("Read as one byte stream xml with surrogate char") });
            }


            // for function v1a
            {
                this.AddChild(new CVariation(v1a) { Attribute = new Variation("Read as TextReader xml with surrogate char") });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlWriter.WriteNode: read as one byte stream xml with surrogate char") });
            }
        }
    }
}
