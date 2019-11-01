// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCRecordNameTableAdd : TCBase
    {
        // Type is NameTableTest.TCRecordNameTableAdd
        // Test Case
        public override void AddChildren()
        {
            // for function Variation_1
            {
                this.AddChild(new CVariation(Variation_1) { Attribute = new Variation("Add a new atomized string (padded with chars at the end), valid offset and length = str_length") { Pri = 0 } });
            }


            // for function Variation_2
            {
                this.AddChild(new CVariation(Variation_2) { Attribute = new Variation("Add a new atomized string (padded with chars at both front and end), valid offset and length = str_length") { Pri = 0 } });
            }


            // for function Variation_3
            {
                this.AddChild(new CVariation(Variation_3) { Attribute = new Variation("Add a new atomized string (padded with chars at the front), valid offset and length = str_length") { Pri = 0 } });
            }


            // for function Variation_4
            {
                this.AddChild(new CVariation(Variation_4) { Attribute = new Variation("Add a new atomized string (padded a char at the end), valid offset and length = str_length") { Pri = 0 } });
            }


            // for function Variation_5
            {
                this.AddChild(new CVariation(Variation_5) { Attribute = new Variation("Add a new atomized string (padded with a char at both front and end), valid offset and length = str_length") { Pri = 0 } });
            }


            // for function Variation_6
            {
                this.AddChild(new CVariation(Variation_6) { Attribute = new Variation("Add a new atomized string (padded with a char at the front), valid offset and length = str_length") { Pri = 0 } });
            }


            // for function Variation_7
            {
                this.AddChild(new CVariation(Variation_7) { Attribute = new Variation("Add new string between 1M - 2M in size, valid offset and length") });
            }


            // for function Variation_8
            {
                this.AddChild(new CVariation(Variation_8) { Attribute = new Variation("Add an existing atomized string (with Max string for test: 1-2M), valid offset and valid length") });
            }


            // for function Variation_9
            {
                this.AddChild(new CVariation(Variation_9) { Attribute = new Variation("Add new string, and do Get with a combination of the same string in different order") { Pri = 0 } });
            }


            // for function Variation_10
            {
                this.AddChild(new CVariation(Variation_10) { Attribute = new Variation("Add new string, and Add a combination of the same string in different case, all are different objects") { Pri = 0 } });
            }


            // for function Variation_11
            {
                this.AddChild(new CVariation(Variation_11) { Attribute = new Variation("Add 1M new string, and do Get with the last char different than the original string") { Pri = 0 } });
            }


            // for function Variation_12
            {
                this.AddChild(new CVariation(Variation_12) { Attribute = new Variation("Add new alpha numeric, valid offset, valid length") { Pri = 0 } });
            }


            // for function Variation_13
            {
                this.AddChild(new CVariation(Variation_13) { Attribute = new Variation("Add new alpha numeric, valid offset, length= 0") { Pri = 0 } });
            }


            // for function Variation_14
            {
                this.AddChild(new CVariation(Variation_14) { Attribute = new Variation("Add new with whitespace, valid offset, valid length") { Pri = 0 } });
            }


            // for function Variation_15
            {
                this.AddChild(new CVariation(Variation_15) { Attribute = new Variation("Add new with sign characters, valid offset, valid length") { Pri = 0 } });
            }


            // for function Variation_16
            {
                this.AddChild(new CVariation(Variation_16) { Attribute = new Variation("Add new string between 1M - 2M in size, valid offset and length") { Pri = 0 } });
            }


            // for function Variation_17
            {
                this.AddChild(new CVariation(Variation_17) { Attribute = new Variation("Add new string, get object using permutations of upper & lowercase, should be null") { Pri = 0 } });
            }


            // for function Variation_18
            {
                this.AddChild(new CVariation(Variation_18) { Attribute = new Variation("Add an empty atomized string, valid offset and length = 0") { Pri = 0 } });
            }


            // for function Variation_19
            {
                this.AddChild(new CVariation(Variation_19) { Attribute = new Variation("Add an empty atomized string (array char only), valid offset and length = 1") { Pri = 0 } });
            }


            // for function Variation_20
            {
                this.AddChild(new CVariation(Variation_20) { Attribute = new Variation("Add a NULL atomized string, valid offset and length = 0") { Pri = 0 } });
            }


            // for function Variation_21
            {
                this.AddChild(new CVariation(Variation_21) { Attribute = new Variation("Add a NULL atomized string, valid offset and length = 1") { Pri = 0 } });
            }


            // for function Variation_22
            {
                this.AddChild(new CVariation(Variation_22) { Attribute = new Variation("Add a valid atomized string, valid offset and length = 0") { Pri = 0 } });
            }


            // for function Variation_23
            {
                this.AddChild(new CVariation(Variation_23) { Attribute = new Variation("Add a valid atomized string, valid offset and length > valid_length") { Pri = 0 } });
            }


            // for function Variation_24
            {
                this.AddChild(new CVariation(Variation_24) { Attribute = new Variation("Add a valid atomized string, valid offset and length = max_int") { Pri = 0 } });
            }


            // for function Variation_25
            {
                this.AddChild(new CVariation(Variation_25) { Attribute = new Variation("Add a valid atomized string, valid offset and length = - 1") { Pri = 0 } });
            }


            // for function Variation_26
            {
                this.AddChild(new CVariation(Variation_26) { Attribute = new Variation("Add a valid atomized string, valid length and offset > str_length") { Pri = 0 } });
            }


            // for function Variation_27
            {
                this.AddChild(new CVariation(Variation_27) { Attribute = new Variation("Add a valid atomized string, valid length and offset = max_int") { Pri = 0 } });
            }


            // for function Variation_28
            {
                this.AddChild(new CVariation(Variation_28) { Attribute = new Variation("Add a valid atomized string, valid length and offset = str_length") { Pri = 0 } });
            }


            // for function Variation_29
            {
                this.AddChild(new CVariation(Variation_29) { Attribute = new Variation("Add a valid atomized string, valid length and offset = - 1") { Pri = 0 } });
            }


            // for function Variation_30
            {
                this.AddChild(new CVariation(Variation_30) { Attribute = new Variation("Add a valid atomized string, with both invalid offset and length") { Pri = 0 } });
            }
        }
    }
}
