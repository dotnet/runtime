// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCRecordNameTableGet : TCBase
    {
        // Type is NameTableTest.TCRecordNameTableGet
        // Test Case
        public override void AddChildren()
        {
            // for function Variation_1
            {
                this.AddChild(new CVariation(Variation_1) { Attribute = new Variation("GetUnAutomized") { Pri = 0 } });
            }


            // for function Variation_2
            {
                this.AddChild(new CVariation(Variation_2) { Attribute = new Variation("Get Atomized String") { Pri = 0 } });
            }


            // for function Variation_3
            {
                this.AddChild(new CVariation(Variation_3) { Attribute = new Variation("Get Atomized String with end padded") { Pri = 0 } });
            }


            // for function Variation_4
            {
                this.AddChild(new CVariation(Variation_4) { Attribute = new Variation("Get Atomized String with front and end padded") { Pri = 0 } });
            }


            // for function Variation_5
            {
                this.AddChild(new CVariation(Variation_5) { Attribute = new Variation("Get Atomized String with front padded") { Pri = 0 } });
            }


            // for function Variation_6
            {
                this.AddChild(new CVariation(Variation_6) { Attribute = new Variation("Get Atomized String with end multi-padded") { Pri = 0 } });
            }


            // for function Variation_7
            {
                this.AddChild(new CVariation(Variation_7) { Attribute = new Variation("Get Atomized String with front and end multi-padded") { Pri = 0 } });
            }


            // for function Variation_8
            {
                this.AddChild(new CVariation(Variation_8) { Attribute = new Variation("Get Atomized String with front multi-padded") { Pri = 0 } });
            }


            // for function Variation_9
            {
                this.AddChild(new CVariation(Variation_9) { Attribute = new Variation("Get Invalid permutation of valid string") { Pri = 0 } });
            }


            // for function Variation_10
            {
                this.AddChild(new CVariation(Variation_10) { Attribute = new Variation("Get Valid Super String") });
            }


            // for function Variation_11
            {
                this.AddChild(new CVariation(Variation_11) { Attribute = new Variation("Get invalid Super String") });
            }


            // for function Variation_12
            {
                this.AddChild(new CVariation(Variation_12) { Attribute = new Variation("Get empty string, valid offset and length = 0") { Pri = 0 } });
            }


            // for function Variation_13
            {
                this.AddChild(new CVariation(Variation_13) { Attribute = new Variation("Get empty string, valid offset and length = 1") { Pri = 0 } });
            }


            // for function Variation_14
            {
                this.AddChild(new CVariation(Variation_14) { Attribute = new Variation("Get null char[], valid offset and length = 0") { Pri = 0 } });
            }


            // for function Variation_15
            {
                this.AddChild(new CVariation(Variation_15) { Attribute = new Variation("Get null string") { Pri = 0 } });
            }


            // for function Variation_16
            {
                this.AddChild(new CVariation(Variation_16) { Attribute = new Variation("Get null char[], valid offset and length = 1") { Pri = 0 } });
            }


            // for function Variation_17
            {
                this.AddChild(new CVariation(Variation_17) { Attribute = new Variation("Get valid string, invalid length, length = 0") { Pri = 0 } });
            }


            // for function Variation_18
            {
                this.AddChild(new CVariation(Variation_18) { Attribute = new Variation("Get valid string, invalid length, length = Length+1") { Pri = 0 } });
            }


            // for function Variation_19
            {
                this.AddChild(new CVariation(Variation_19) { Attribute = new Variation("Get valid string, invalid length, length = max_int") { Pri = 0 } });
            }


            // for function Variation_20
            {
                this.AddChild(new CVariation(Variation_20) { Attribute = new Variation("Get valid string, invalid length, length = -1") { Pri = 0 } });
            }


            // for function Variation_21
            {
                this.AddChild(new CVariation(Variation_21) { Attribute = new Variation("Get valid string, invalid offset > Length") { Pri = 0 } });
            }


            // for function Variation_22
            {
                this.AddChild(new CVariation(Variation_22) { Attribute = new Variation("Get valid string, invalid offset = max_int") { Pri = 0 } });
            }


            // for function Variation_23
            {
                this.AddChild(new CVariation(Variation_23) { Attribute = new Variation("Get valid string, invalid offset = Length") { Pri = 0 } });
            }


            // for function Variation_24
            {
                this.AddChild(new CVariation(Variation_24) { Attribute = new Variation("Get valid string, invalid offset -1") { Pri = 0 } });
            }


            // for function Variation_25
            {
                this.AddChild(new CVariation(Variation_25) { Attribute = new Variation("Get valid string, invalid offset and length") { Pri = 0 } });
            }
        }
    }
}
