// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCThisOrdinal : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCThisOrdinal
        // Test Case
        public override void AddChildren()
        {
            // for function OrdinalWithGetAttrDoubleQ
            {
                this.AddChild(new CVariation(OrdinalWithGetAttrDoubleQ) { Attribute = new Variation("This[i] Verify with GetAttribute[i] - Double Quote") { Pri = 0 } });
            }


            // for function OrdinalWithGetAttrSingleQ
            {
                this.AddChild(new CVariation(OrdinalWithGetAttrSingleQ) { Attribute = new Variation("This[i] Verify with GetAttribute[i] - Single Quote") });
            }


            // for function OrdinalWithMoveAttrDoubleQ
            {
                this.AddChild(new CVariation(OrdinalWithMoveAttrDoubleQ) { Attribute = new Variation("This[i] Verify with MoveToAttribute[i] - Double Quote") { Pri = 0 } });
            }


            // for function OrdinalWithMoveAttrSingleQ
            {
                this.AddChild(new CVariation(OrdinalWithMoveAttrSingleQ) { Attribute = new Variation("This[i] Verify with MoveToAttribute[i] - Single Quote") });
            }


            // for function NegativeOneOrdinal
            {
                this.AddChild(new CVariation(NegativeOneOrdinal) { Attribute = new Variation("ThisOrdinal NegativeOneOrdinal") { Pri = 0 } });
            }


            // for function FieldCountOrdinal
            {
                this.AddChild(new CVariation(FieldCountOrdinal) { Attribute = new Variation("ThisOrdinal FieldCountOrdinal") });
            }


            // for function OrdinalPlusOne
            {
                this.AddChild(new CVariation(OrdinalPlusOne) { Attribute = new Variation("ThisOrdinal OrdinalPlusOne") { Pri = 0 } });
            }


            // for function OrdinalMinusOne
            {
                this.AddChild(new CVariation(OrdinalMinusOne) { Attribute = new Variation("ThisOrdinal OrdinalMinusOne") });
            }
        }
    }
}
