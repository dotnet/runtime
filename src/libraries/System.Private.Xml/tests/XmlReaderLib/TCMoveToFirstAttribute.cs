// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCMoveToFirstAttribute : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCMoveToFirstAttribute
        // Test Case
        public override void AddChildren()
        {
            // for function MoveToFirstAttribute1
            {
                this.AddChild(new CVariation(MoveToFirstAttribute1) { Attribute = new Variation("MoveToFirstAttribute() When AttributeCount=0, <EMPTY1/> ") { Pri = 0 } });
            }


            // for function MoveToFirstAttribute2
            {
                this.AddChild(new CVariation(MoveToFirstAttribute2) { Attribute = new Variation("MoveToFirstAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ") });
            }


            // for function MoveToFirstAttribute3
            {
                this.AddChild(new CVariation(MoveToFirstAttribute3) { Attribute = new Variation("MoveToFirstAttribute() When iOrdinal=0, with namespace") });
            }


            // for function MoveToFirstAttribute4
            {
                this.AddChild(new CVariation(MoveToFirstAttribute4) { Attribute = new Variation("MoveToFirstAttribute() When iOrdinal=0, without namespace") });
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
        }
    }
}
