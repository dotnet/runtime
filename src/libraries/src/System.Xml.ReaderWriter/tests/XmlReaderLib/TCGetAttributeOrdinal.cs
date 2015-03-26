// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCGetAttributeOrdinal : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCGetAttributeOrdinal
        // Test Case
        public override void AddChildren()
        {
            // for function GetAttributeWithGetAttrDoubleQ
            {
                this.AddChild(new CVariation(GetAttributeWithGetAttrDoubleQ) { Attribute = new Variation("GetAttribute(i) Verify with This[i] - Double Quote") { Pri = 0 } });
            }


            // for function OrdinalWithGetAttrSingleQ
            {
                this.AddChild(new CVariation(OrdinalWithGetAttrSingleQ) { Attribute = new Variation("GetAttribute[i] Verify with This[i] - Single Quote") });
            }


            // for function GetAttributeWithMoveAttrDoubleQ
            {
                this.AddChild(new CVariation(GetAttributeWithMoveAttrDoubleQ) { Attribute = new Variation("GetAttribute(i) Verify with MoveToAttribute[i] - Double Quote") { Pri = 0 } });
            }


            // for function GetAttributeWithMoveAttrSingleQ
            {
                this.AddChild(new CVariation(GetAttributeWithMoveAttrSingleQ) { Attribute = new Variation("GetAttribute(i) Verify with MoveToAttribute[i] - Single Quote") });
            }


            // for function NegativeOneOrdinal
            {
                this.AddChild(new CVariation(NegativeOneOrdinal) { Attribute = new Variation("GetAttribute(i) NegativeOneOrdinal") { Pri = 0 } });
            }


            // for function FieldCountOrdinal
            {
                this.AddChild(new CVariation(FieldCountOrdinal) { Attribute = new Variation("GetAttribute(i) FieldCountOrdinal") });
            }


            // for function OrdinalPlusOne
            {
                this.AddChild(new CVariation(OrdinalPlusOne) { Attribute = new Variation("GetAttribute(i) OrdinalPlusOne") { Pri = 0 } });
            }


            // for function OrdinalMinusOne
            {
                this.AddChild(new CVariation(OrdinalMinusOne) { Attribute = new Variation("GetAttribute(i) OrdinalMinusOne") });
            }
        }
    }
}
