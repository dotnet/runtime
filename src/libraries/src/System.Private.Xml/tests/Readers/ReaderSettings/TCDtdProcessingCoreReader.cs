// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCDtdProcessingCoreReader : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCDtdProcessingCoreReader
        // Test Case
        public override void AddChildren()
        {
            // for function v0
            {
                this.AddChild(new CVariation(v0) { Attribute = new Variation("Read xml without DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(v0) { Attribute = new Variation("Read xml without DTD.Ignore") { Param = 1 } });
            }


            // for function v1a
            {
                this.AddChild(new CVariation(v1a) { Attribute = new Variation("Wrap with Prohibit, xml w/o DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(v1a) { Attribute = new Variation("Wrap with Prohibit, xml w/o DTD.Ignore") { Param = 1 } });
            }


            // for function v1b
            {
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("Wrap with Ignore, xml w/o DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("Wrap with Ignore, xml w/o DTD.Ignore") { Param = 1 } });
            }


            // for function v1d
            {
                this.AddChild(new CVariation(v1d) { Attribute = new Variation("Wrap with Prohibit, change RS, xml w/o DTD.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(v1d) { Attribute = new Variation("Wrap with Prohibit, change RS, xml w/o DTD.Prohibit") { Param = 0 } });
            }


            // for function v1e
            {
                this.AddChild(new CVariation(v1e) { Attribute = new Variation("Wrap with Ignore, change RS, xml w/o DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(v1e) { Attribute = new Variation("Wrap with Ignore, change RS, xml w/o DTD.Ignore") { Param = 1 } });
            }


            // for function v2a
            {
                this.AddChild(new CVariation(v2a) { Attribute = new Variation("Wrap with Prohibit, xml with DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(v2a) { Attribute = new Variation("Wrap with Prohibit, xml with DTD.Ignore") { Param = 1 } });
            }


            // for function v2b
            {
                this.AddChild(new CVariation(v2b) { Attribute = new Variation("Wrap with Ignore, xml with DTD.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(v2b) { Attribute = new Variation("Wrap with Ignore, xml with DTD.Prohibit") { Param = 0 } });
            }


            // for function V3
            {
                this.AddChild(new CVariation(V3) { Attribute = new Variation("Testing default values.") });
            }


            // for function V4
            {
                this.AddChild(new CVariation(V4) { Attribute = new Variation("Parse a file with inline DTD.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(V4) { Attribute = new Variation("Parse a file with inline DTD.Prohibit") { Param = 0 } });
            }


            // for function V4c
            {
                this.AddChild(new CVariation(V4c) { Attribute = new Variation("Parse a xml with inline inv.DTD.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(V4c) { Attribute = new Variation("Parse a xml with inline inv.DTD.Prohibit") { Param = 0 } });
            }


            // for function V4i
            {
                this.AddChild(new CVariation(V4i) { Attribute = new Variation("Read xml with invalid content.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(V4i) { Attribute = new Variation("Read xml with invalid content.Ignore") { Param = 1 } });
            }


            // for function V7a
            {
                this.AddChild(new CVariation(V7a) { Attribute = new Variation("Changing DtdProcessing to Prohibit,Ignore.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(V7a) { Attribute = new Variation("Changing DtdProcessing to Prohibit,Ignore.Ignore") { Param = 1 } });
            }


            // for function V8
            {
                this.AddChild(new CVariation(V8) { Attribute = new Variation("Parse a file with external DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(V8) { Attribute = new Variation("Parse a file with external DTD.Ignore") { Param = 1 } });
            }


            // for function V9
            {
                this.AddChild(new CVariation(V9) { Attribute = new Variation("Parse a file with invalid inline DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(V9) { Attribute = new Variation("Parse a file with invalid inline DTD.Ignore") { Param = 1 } });
            }


            // for function V11
            {
                this.AddChild(new CVariation(V11) { Attribute = new Variation("Parse a valid xml with predefined entities with no DTD.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(V11) { Attribute = new Variation("Parse a valid xml with predefined entities with no DTD.Ignore") { Param = 1 } });
            }


            // for function V11a
            {
                this.AddChild(new CVariation(V11a) { Attribute = new Variation("Parse a valid xml with entity and DTD.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(V11a) { Attribute = new Variation("Parse a valid xml with entity and DTD.Prohibit") { Param = 0 } });
            }


            // for function V11b
            {
                this.AddChild(new CVariation(V11b) { Attribute = new Variation("Parse a valid xml with entity in attribute and DTD.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(V11b) { Attribute = new Variation("Parse a valid xml with entity in attribute and DTD.Prohibit") { Param = 0 } });
            }


            // for function V11c
            {
                this.AddChild(new CVariation(V11c) { Attribute = new Variation("Parse a invalid xml with entity in attribute and DTD.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(V11c) { Attribute = new Variation("Parse a invalid xml with entity in attribute and DTD.Prohibit") { Param = 0 } });
            }


            // for function v12
            {
                this.AddChild(new CVariation(v12) { Attribute = new Variation("Set value to Reader.Settings.DtdProcessing.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(v12) { Attribute = new Variation("Set value to Reader.Settings.DtdProcessing.Ignore") { Param = 1 } });
            }


            // for function V14
            {
                this.AddChild(new CVariation(V14) { Attribute = new Variation("DtdProcessing - ArgumentOutOfRangeException") });
            }


            // for function V15
            {
                this.AddChild(new CVariation(V15) { Attribute = new Variation("DtdProcessing - ArgumentOutOfRangeException.Ignore") { Param = 1 } });
                //this.AddChild(new CVariation(V15){ Attribute =  new Variation("DtdProcessing - ArgumentOutOfRangeException.Parse"){ Param = 2}});
                this.AddChild(new CVariation(V15) { Attribute = new Variation("DtdProcessing - ArgumentOutOfRangeException.Prohibit") { Param = 0 } });
            }


            // for function V16
            {
                this.AddChild(new CVariation(V16) { Attribute = new Variation("Parse a valid xml DTD and check NodeType.Prohibit") { Param = 0 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("Parse a valid xml DTD and check NodeType.Ignore") { Param = 1 } });
                //this.AddChild(new CVariation(V16){ Attribute =  new Variation("Parse a valid xml DTD and check NodeType.Parse"){ Param = 2}});
            }


            // for function V18
            {
                this.AddChild(new CVariation(V18) { Attribute = new Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Ignore") { Param = 1 } });
                this.AddChild(new CVariation(V18) { Attribute = new Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Prohibit") { Param = 0 } });
                //this.AddChild(new CVariation(V18){ Attribute =  new Variation("Parse a invalid xml DTD SYSTEM PUBLIC.Parse"){ Param = 2}});
            }


            // for function V19
            {
                this.AddChild(new CVariation(V19) { Attribute = new Variation("6.Parsing invalid DOCTYPE.Ignore") { Params = new object[] { DtdProcessing.Ignore, 6 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("8.PParsing invalid xml version.Ignore") { Params = new object[] { DtdProcessing.Ignore, 8 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("9.Parsing invalid xml version.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 9 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("9.Parsing invalid xml version.Ignore") { Params = new object[] { DtdProcessing.Ignore, 9 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("10.Parsing invalid xml version.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 10 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("10.Parsing invalid xml version.Ignore") { Params = new object[] { DtdProcessing.Ignore, 10 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("11.Parsing invalid xml version.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 11 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("11.Parsing invalid xml version.Ignore") { Params = new object[] { DtdProcessing.Ignore, 11 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("12.Parsing invalid xml version.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 12 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("12.Parsing invalid xml version.Ignore") { Params = new object[] { DtdProcessing.Ignore, 12 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("1.Parsing invalid DOCTYPE.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 1 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("1.Parsing invalid DOCTYPE.Ignore") { Params = new object[] { DtdProcessing.Ignore, 1 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("2.Parsing invalid DOCTYPE.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 2 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("7.Parsing invalid DOCTYPE.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 7 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("7.Parsing invalid DOCTYPE.Ignore") { Params = new object[] { DtdProcessing.Ignore, 7 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("8.Parsing invalid xml version.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 8 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("2.Parsing invalid DOCTYPE.Ignore") { Params = new object[] { DtdProcessing.Ignore, 2 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("3.Parsing invalid DOCTYPE.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 3 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("3.Parsing invalid DOCTYPE.Ignore") { Params = new object[] { DtdProcessing.Ignore, 3 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("4.Parsing invalid DOCTYPE.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 4 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("4.Parsing invalid DOCTYPE.Ignore") { Params = new object[] { DtdProcessing.Ignore, 4 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("5.Parsing invalid DOCTYPE.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 5 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("5.Parsing invalid DOCTYPE.Ignore") { Params = new object[] { DtdProcessing.Ignore, 5 } } });
                this.AddChild(new CVariation(V19) { Attribute = new Variation("6.Parsing invalid DOCTYPE.Prohibit") { Params = new object[] { DtdProcessing.Prohibit, 6 } } });
            }
        }
    }
}
