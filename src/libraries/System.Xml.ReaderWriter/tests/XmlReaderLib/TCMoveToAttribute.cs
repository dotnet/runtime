// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlReaderTest.Common
{
    public partial class TCMoveToAttribute : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCMoveToAttribute
        // Test Case
        public override void AddChildren()
        {
            // for function MoveToAttributeWithName1
            {
                this.AddChild(new CVariation(MoveToAttributeWithName1) { Attribute = new Variation("MoveToAttribute(String.Empty)") });
            }


            // for function MoveToAttributeWithName2
            {
                this.AddChild(new CVariation(MoveToAttributeWithName2) { Attribute = new Variation("MoveToAttribute(String.Empty,String.Empty)") });
            }
        }
    }
}
