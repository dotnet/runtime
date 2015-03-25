// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace NameTableTest
{
    public partial class TCUserNameTable : CTestCase
    {
        // Type is NameTableTest.TCUserNameTable
        // Test Case
        public override void AddChildren()
        {
            // for function v1
            {
                this.AddChild(new CVariation(v1) { Attribute = new Variation("Read xml file using custom name table") });
            }
        }
    }
}
