// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCMoveToNextAttribute : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCMoveToNextAttribute
        // Test Case
        public override void AddChildren()
        {
            // for function MoveToNextAttribute1
            {
                this.AddChild(new CVariation(MoveToNextAttribute1) { Attribute = new Variation("MoveToNextAttribute() When AttributeCount=0, <EMPTY1/> ") { Pri = 0 } });
            }


            // for function MoveToNextAttribute2
            {
                this.AddChild(new CVariation(MoveToNextAttribute2) { Attribute = new Variation("MoveToNextAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ") });
            }


            // for function MoveToNextAttribute3
            {
                this.AddChild(new CVariation(MoveToNextAttribute3) { Attribute = new Variation("MoveToNextAttribute() When iOrdinal=0, with namespace") });
            }


            // for function MoveToNextAttribute4
            {
                this.AddChild(new CVariation(MoveToNextAttribute4) { Attribute = new Variation("MoveToNextAttribute() When iOrdinal=0, without namespace") });
            }


            // for function MoveToFirstAttribute5
            {
                this.AddChild(new CVariation(MoveToFirstAttribute5) { Attribute = new Variation("MoveToFirstAttribute() When iOrdinal=middle, with namespace") });
            }


            // for function MoveToFirstAttribute6
            {
                this.AddChild(new CVariation(MoveToFirstAttribute6) { Attribute = new Variation("MoveToFirstAttribute() When iOrdinal=middle, without namespace") });
            }


            // for function MoveToFirstAttribute7
            {
                this.AddChild(new CVariation(MoveToFirstAttribute7) { Attribute = new Variation("MoveToFirstAttribute() When iOrdinal=end, with namespace") });
            }


            // for function MoveToFirstAttribute8
            {
                this.AddChild(new CVariation(MoveToFirstAttribute8) { Attribute = new Variation("MoveToFirstAttribute() When iOrdinal=end, without namespace") });
            }


            // for function MoveToNextAttribute9
            {
                this.AddChild(new CVariation(MoveToNextAttribute9) { Attribute = new Variation("424573 - XmlReader: Does not count depth for attributes of xml decl. and Doctype") });
            }
        }
    }
}
