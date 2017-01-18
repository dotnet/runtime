// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Imports", Desc = "")]
    public class TC_SchemaSet_Imports : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Imports(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1.3 - Import: A(ns-a) which improts B (no ns)", Priority = 0, Params = new object[] { "import_v4_a.xsd", "import_v4_b.xsd", 2, null })]
        [InlineData("import_v4_a.xsd", "import_v4_b.xsd", 2, null)]
        //[Variation(Desc = "v1.2 - Import: A(ns-a) improts B (ns-b)", Priority = 0, Params = new object[] { "import_v2_a.xsd", "import_v2_b.xsd", 2, "ns-b" })]
        [InlineData("import_v2_a.xsd", "import_v2_b.xsd", 2, "ns-b")]
        //[Variation(Desc = "v1.1 - Import: A with NS imports B with no NS", Priority = 0, Params = new object[] { "import_v1_a.xsd", "include_v1_b.xsd", 2, null })]
        [InlineData("import_v1_a.xsd", "include_v1_b.xsd", 2, null)]
        [Theory]
        public void v1(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            CError.Compare(sc.Count, param2, "Count");

            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, param2, "Count");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals(param1.ToString()) && imp.Schema.TargetNamespace == (string)param3)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2.2 - Import: Add B(no ns) with ns-b , then A(ns-a) which imports B (no ns)", Priority = 1, Params = new object[] { "import_v5_a.xsd", "import_v4_b.xsd", 3, "ns-b", null })]
        [InlineData("import_v5_a.xsd", "import_v4_b.xsd", 3, "ns-b", null)]
        //[Variation(Desc = "v2.1 - Import: Add B(ns-b) , then A(ns-a) which improts B (ns-b)", Priority = 1, Params = new object[] { "import_v2_a.xsd", "import_v2_b.xsd", 2, null, "ns-b" })]
        [InlineData("import_v2_a.xsd", "import_v2_b.xsd", 2, null, "ns-b")]
        [Theory]
        public void v3(object param0, object param1, object param2, object param3, object param4)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            sc.Add((string)param3, Path.Combine(TestData._Root, param1.ToString()));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));

            CError.Compare(sc.Count, param2, "Add2Count");
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals(param1.ToString()) && imp.Schema.TargetNamespace == (string)param4)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - Import: Add A(ns-a) which imports B (no ns), then Add B(no ns) again", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            XmlSchema orig = sc.Add(null, Path.Combine(TestData._Root, "import_v4_b.xsd")); // should be already present in the set

            CError.Compare(sc.IsCompiled, true, "Add2IsCompiled");
            CError.Compare(sc.Count, 2, "Add2Count");
            CError.Compare(orig.SourceUri.Contains("import_v4_b.xsd"), true, "Compare the schema object");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v4_b.xsd") && imp.Schema.TargetNamespace == null)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4 - Import: Add A(ns-a) which improts B (no ns), then Add B to ns-b", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v7()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Add("ns-b", Path.Combine(TestData._Root, "import_v4_b.xsd")); // should be already present in the set

            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v4_b.xsd") && imp.Schema.TargetNamespace == null)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5 - Import: Add A(ns-a) which improts B (ns-b), then Add B(ns-b) again", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v8()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v2_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Add("ns-b", Path.Combine(TestData._Root, "import_v2_b.xsd")); // should be already present in the set
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v2_b.xsd") && imp.Schema.TargetNamespace.Equals("ns-b"))
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - Import: A(ns-a) imports B(ns-b) imports C (ns-c)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v9()
        {
            bool found = false;
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v9_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            XmlSchema sch_B = sc.Add(null, Path.Combine(TestData._Root, "import_v9_b.xsd")); // should be already present in the set
            sc.Add(null, Path.Combine(TestData._Root, "import_v9_c.xsd"));				   // should be already present in the set

            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v9_b.xsd") && imp.Schema.TargetNamespace.Equals("ns-b"))
                    found = true;
            if (!found) Assert.True(false);

            // check that schema C in sch_b.Includes and its NS correct.
            foreach (XmlSchemaImport imp in sch_B.Includes)
                if (imp.SchemaLocation.Equals("import_v9_c.xsd") && imp.Schema.TargetNamespace.Equals("ns-c"))
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v7 - Import: A(ns-a) imports B(NO NS) imports C (ns-c)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v10()
        {
            bool found = false;
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v10_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            XmlSchema sch_B = sc.Add(null, Path.Combine(TestData._Root, "import_v10_b.xsd")); // should be already present in the set
            sc.Add(null, Path.Combine(TestData._Root, "import_v10_c.xsd"));				   // should be already present in the set

            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v10_b.xsd") && imp.Schema.TargetNamespace == null)
                    found = true;

            if (!found) Assert.True(false);

            // check that schema C in sch_b.Includes and its NS correct.
            foreach (XmlSchemaImport imp in sch_B.Includes)
                if (imp.SchemaLocation.Equals("import_v10_c.xsd") && imp.Schema.TargetNamespace.Equals("ns-c"))
                    found = true;

            if (!found) Assert.True(false);

            // try adding no ns schema with an ns
            sc.Add("ns-b", Path.Combine(TestData._Root, "import_v10_b.xsd"));
            CError.Compare(sc.Count, 4, "Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v8 - Import: A(ns-a) imports B(ns-b) imports C (ns-a)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v11()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v11_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 2, "count for ns-a");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v9 - Import: A(ns-a) imports B(ns-b) and C (ns-b)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v12()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v12_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-b").Count, 2, "count for ns-b");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v10 - Import: A imports B and B and C, B imports C and D, C imports D and A", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v13()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v13_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 4, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 4, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v11 - Import: A(ns-a) imports B(ns-b) and C (ns-b), B and C include each other", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v14()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v14_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-b").Count, 2, "count for ns-b");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v12 - Import: A(ns-a) imports B(BOGUS) and C (ns-c)", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v15()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v15_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v13 - Import: B(ns-b) added, A(ns-a) imports B with bogus url", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v16()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add(null, Path.Combine(TestData._Root, "import_v16_b.xsd"));
            CError.Compare(sc.IsCompiled, false, "Add1IsCompiled");
            CError.Compare(sc.Count, 1, "Add1Count");

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v16_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");
            CError.Compare(sc.Count, 2, "Add2Count");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v14 - Import: A(ns-a) includes B(ns-a) which imports C(ns-c)", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v17()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v17_a.xsd"));

            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");
            CError.Compare(sc.Count, 2, "CompileCount");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v15 - Import: A(ns-a) includes A(ns-a) of v17", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v18()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v18_a.xsd"));

            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");
            CError.Compare(sc.Count, 2, "CompileCount");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v16 - Import: A(ns-b) imports A(ns-a) of v17", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v19()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v19_a.xsd"));
            CError.Compare(sc.Count, 3, "Count");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 2, "count for ns-c");
            CError.Compare(sc.Count, 3, "CompileCount");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v17 - Import: A,B,C,D all import and reference each other for types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v20()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v20_a.xsd"));
            CError.Compare(sc.Count, 4, "Count");

            sc.Compile();
            // try to add each individually
            XmlSchema b = sc.Add(null, Path.Combine(TestData._Root, "import_v20_b.xsd"));
            XmlSchema c = sc.Add(null, Path.Combine(TestData._Root, "import_v20_c.xsd"));
            XmlSchema d = sc.Add(null, Path.Combine(TestData._Root, "import_v20_d.xsd"));

            CError.Compare(sc.Count, 4, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(b.SourceUri.Contains("import_v20_b.xsd"), true, "Compare B");
            CError.Compare(c.SourceUri.Contains("import_v20_c.xsd"), true, "Compare C");
            CError.Compare(d.SourceUri.Contains("import_v20_d.xsd"), true, "Compare D");

            return;
        }

        //[Variation(Desc = "v21- Import: Bug 114549 , A imports only B but refers to C and D both", Priority = 1, Params = new object[] { "import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v21_a.xsd" })]
        [InlineData("import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v21_a.xsd")]
        //[Variation(Desc = "v22- Import: Bug 114549 , A imports only B's NS, but refers to B,C and D both", Priority = 1, Params = new object[] { "import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        //[Variation(Desc = "v23- Import: Bug 114549 , A imports only B's NS, and B also improts A's NS AND refers to A's types", Priority = 1, Params = new object[] { "import_v24_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v24_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        [Theory]
        public void v21(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            ss.Add(null, Path.Combine(TestData._Root, param1.ToString()));
            ss.Add(null, Path.Combine(TestData._Root, param2.ToString()));
            ss.Add(null, Path.Combine(TestData._Root, param3.ToString()));
            CError.Compare(ss.Count, 4, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 4, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");
            return;
        }

        //[Variation(Desc = "v24- Import: Bug 114549 , A imports only B's NS, and B also refers to A's types (WARNING)", Priority = 1, Params = new object[] { "import_v23_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v23_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        //[Variation(Desc = "v25- Import: Bug 114549 , A imports only B's NS, and B also improts A's NS AND refers to A's type, D refers to A's type (WARNING)", Priority = 1, Params = new object[] { "import_v24_b.xsd", "import_v21_c.xsd", "import_v25_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v24_b.xsd", "import_v21_c.xsd", "import_v25_d.xsd", "import_v22_a.xsd")]
        [Theory]
        public void v24(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet ss = new XmlSchemaSet();

            ss.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            ss.Add(null, Path.Combine(TestData._Root, param1.ToString()));
            ss.Add(null, Path.Combine(TestData._Root, param2.ToString()));
            ss.Add(null, Path.Combine(TestData._Root, param3.ToString()));
            CError.Compare(ss.Count, 4, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 4, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            return;
        }

        //[Variation(Desc = "v100 - Import: Bug 105897", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v100()
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "105897.xsd"));
            ss.Add(null, Path.Combine(TestData._Root, "105897_a.xsd"));
            CError.Compare(ss.Count, 3, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 3, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings |
                                       XmlSchemaValidationFlags.ProcessSchemaLocation |
                                       XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.Schemas = new XmlSchemaSet();
            settings.Schemas.Add(ss);

            using (XmlReader vr = XmlReader.Create(Path.Combine(TestData._Root, "105897.xml"), settings))
            {
                while (vr.Read()) ;
            }
            return;
        }

        /********* reprocess compile import**************/

        //[Variation(Desc = "v101.3 - Import: A(ns-a) which improts B (no ns)", Priority = 0, Params = new object[] { "import_v4_a.xsd", "import_v4_b.xsd", 2, null })]
        [InlineData("import_v4_a.xsd", "import_v4_b.xsd", 2, null)]
        //[Variation(Desc = "v101.2 - Import: A(ns-a) improts B (ns-b)", Priority = 0, Params = new object[] { "import_v2_a.xsd", "import_v2_b.xsd", 2, "ns-b" })]
        [InlineData("import_v2_a.xsd", "import_v2_b.xsd", 2, "ns-b")]
        //[Variation(Desc = "v101.1 - Improt: A with NS imports B with no NS", Priority = 0, Params = new object[] { "import_v1_a.xsd", "include_v1_b.xsd", 2, null })]
        [InlineData("import_v1_a.xsd", "include_v1_b.xsd", 2, null)]
        [Theory]
        public void v101(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            CError.Compare(sc.Count, param2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, param2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, param2, "Count");
            // check that schema is present in parent.Includes and its NS correct.

            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals(param1.ToString()) && imp.Schema.TargetNamespace == (string)param3)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------

        //[Variation(Desc = "v102.1 - Import: Add B(ns-b) , then A(ns-a) which improts B (ns-b)", Priority = 1, Params = new object[] { "import_v2_a.xsd", "import_v2_b.xsd", 2, null, "ns-b" })]
        [InlineData("import_v2_a.xsd", "import_v2_b.xsd", 2, null, "ns-b")]
        [Theory]
        public void v102(object param0, object param1, object param2, object param3, object param4)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema sch = sc.Add((string)param3, Path.Combine(TestData._Root, param1.ToString()));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Reprocess(sch);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            CError.Compare(sc.Count, param2, "Add2Count");
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 2, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals(param1.ToString()) && imp.Schema.TargetNamespace == (string)param4)
                    return;

            Assert.True(false);
        }

        //[Variation(Desc = "v102.2 - Import: Add B(no ns) with ns-b , then A(ns-a) which imports B (no ns)", Priority = 1, Params = new object[] { "import_v5_a.xsd", "import_v4_b.xsd", 3, "ns-b", null })]
        [InlineData("import_v5_a.xsd", "import_v4_b.xsd", 3, "ns-b", null)]
        [Theory]
        public void v102a(object param0, object param1, object param2, object param3, object param4)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema sch = sc.Add((string)param3, Path.Combine(TestData._Root, param1.ToString()));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Reprocess(sch);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            try
            {
                XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                Assert.True(false);
            }
            catch (XmlSchemaException) { }

            CError.Compare(sc.Count, 1, "Add2Count");
            CError.Compare(sc.IsCompiled, true, "Add2IsCompiled");

            sc.Reprocess(sch);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v103 - Import: Add A(ns-a) which imports B (no ns), then Add B(no ns) again", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v103()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            XmlSchema orig = sc.Add(null, Path.Combine(TestData._Root, "import_v4_b.xsd")); // should be already present in the set
            CError.Compare(sc.IsCompiled, true, "Add2IsCompiled");
            CError.Compare(sc.Count, 2, "Add2Count");
            CError.Compare(orig.SourceUri.Contains("import_v4_b.xsd"), true, "Compare the schema object");

            sc.Reprocess(orig);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v4_b.xsd") && imp.Schema.TargetNamespace == null)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v104 - Import: Add A(ns-a) which improts B (no ns), then Add B to ns-b", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v104()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            XmlSchema sch_B = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v4_b.xsd")); // should be already present in the set
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 3, "CompileCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v4_b.xsd") && imp.Schema.TargetNamespace == null)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v105 - Import: Add A(ns-a) which improts B (ns-b), then Add B(ns-b) again", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v105()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v2_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            XmlSchema sch_B = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v2_b.xsd")); // should be already present in the set
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v2_b.xsd") && imp.Schema.TargetNamespace.Equals("ns-b"))
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v106 - Import: A(ns-a) imports B(ns-b) imports C (ns-c)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v106()
        {
            bool found = false;
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v9_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            XmlSchema sch_B = sc.Add(null, Path.Combine(TestData._Root, "import_v9_b.xsd")); // should be already present in the set
            sc.Add(null, Path.Combine(TestData._Root, "import_v9_c.xsd"));				   // should be already present in the set
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v9_b.xsd") && imp.Schema.TargetNamespace.Equals("ns-b"))
                    found = true;
            if (!found) Assert.True(false);
            // check that schema C in sch_b.Includes and its NS correct.
            foreach (XmlSchemaImport imp in sch_B.Includes)
                if (imp.SchemaLocation.Equals("import_v9_c.xsd") && imp.Schema.TargetNamespace.Equals("ns-c"))
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v107 - Import: A(ns-a) imports B(NO NS) imports C (ns-c)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v107()
        {
            bool found = false;
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v10_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            XmlSchema sch_B = sc.Add(null, Path.Combine(TestData._Root, "import_v10_b.xsd")); // should be already present in the set
            sc.Add(null, Path.Combine(TestData._Root, "import_v10_c.xsd"));				   // should be already present in the set
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v10_b.xsd") && imp.Schema.TargetNamespace == null)
                    found = true;

            if (!found) Assert.True(false);

            // check that schema C in sch_b.Includes and its NS correct.
            foreach (XmlSchemaImport imp in sch_B.Includes)
                if (imp.SchemaLocation.Equals("import_v10_c.xsd") && imp.Schema.TargetNamespace.Equals("ns-c"))
                    found = true;

            if (!found) Assert.True(false);

            // try adding no ns schema with an ns
            sch_B = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v10_b.xsd"));
            CError.Compare(sc.Count, 4, "Count");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 4, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v108 - Import: A(ns-a) imports B(ns-b) imports C (ns-a)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v108()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v11_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 2, "count for ns-a");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v109 - Import: A(ns-a) imports B(ns-b) and C (ns-b)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v109()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v12_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-b").Count, 2, "count for ns-b");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v110 - Import: A imports B and B and C, B imports C and D, C imports D and A", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v110()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v13_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 4, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 4, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v111 - Import: A(ns-a) imports B(ns-b) and C (ns-b), B and C include each other", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v111()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v14_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-b").Count, 2, "count for ns-b");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v112 - Import: A(ns-a) imports B(BOGUS) and C (ns-c)", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v112()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v15_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v113 - Import: B(ns-b) added, A(ns-a) imports B with bogus url", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v113()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add(null, Path.Combine(TestData._Root, "import_v16_b.xsd"));
            CError.Compare(sc.IsCompiled, false, "Add1IsCompiled");
            CError.Compare(sc.Count, 1, "Add1Count");

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v16_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");
            CError.Compare(sc.Count, 2, "Add2Count");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v114 - Import: A(ns-a) includes B(ns-a) which imports C(ns-c)", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v114()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v17_a.xsd"));

            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");
            CError.Compare(sc.Count, 2, "CompileCount");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v115 - Import: A(ns-a) includes A(ns-a) of v17", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v115()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v18_a.xsd"));

            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");
            CError.Compare(sc.Count, 2, "CompileCount");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v116 - Import: A(ns-b) imports A(ns-a) of v17", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v116()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v19_a.xsd"));
            CError.Compare(sc.Count, 3, "Count");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 2, "count for ns-c");
            CError.Compare(sc.Count, 3, "CompileCount");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v117 - Import: A,B,C,D all import and reference each other for types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v117()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v20_a.xsd"));
            CError.Compare(sc.Count, 4, "Count");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 4, "CompileCount");

            XmlSchema b = sc.Add(null, Path.Combine(TestData._Root, "import_v20_b.xsd"));
            XmlSchema c = sc.Add(null, Path.Combine(TestData._Root, "import_v20_c.xsd"));
            XmlSchema d = sc.Add(null, Path.Combine(TestData._Root, "import_v20_d.xsd"));

            CError.Compare(sc.Count, 4, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(b.SourceUri.Contains("import_v20_b.xsd"), true, "Compare B");
            CError.Compare(c.SourceUri.Contains("import_v20_c.xsd"), true, "Compare C");
            CError.Compare(d.SourceUri.Contains("import_v20_d.xsd"), true, "Compare D");

            sc.Reprocess(b);
            CError.Compare(sc.Count, 4, "b ReprocessCount");
            sc.Reprocess(c);
            CError.Compare(sc.Count, 4, "c ReprocessCount");
            sc.Reprocess(d);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "d ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 4, "CompileCount");
            return;
        }

        //[Variation(Desc = "v121- Import: Bug 114549 , A imports only B but refers to C and D both", Priority = 1, Params = new object[] { "import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v21_a.xsd" })]
        [InlineData("import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v21_a.xsd")]
        //[Variation(Desc = "v122- Import: Bug 114549 , A imports only B's NS, but refers to B,C and D both", Priority = 1, Params = new object[] { "import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        //[Variation(Desc = "v123- Import: Bug 114549 , A imports only B's NS, and B also improts A's NS AND refers to A's types", Priority = 1, Params = new object[] { "import_v24_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v24_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        [Theory]
        public void v118(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema1 = ss.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            XmlSchema Schema2 = ss.Add(null, Path.Combine(TestData._Root, param1.ToString()));
            XmlSchema Schema3 = ss.Add(null, Path.Combine(TestData._Root, param2.ToString()));
            XmlSchema Schema4 = ss.Add(null, Path.Combine(TestData._Root, param3.ToString()));
            CError.Compare(ss.Count, 4, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Reprocess(Schema1);
            CError.Compare(ss.Count, 4, "ReprocessCount1");
            ss.Reprocess(Schema2);
            CError.Compare(ss.Count, 4, "ReprocessCount2");
            ss.Reprocess(Schema3);
            CError.Compare(ss.Count, 4, "ReprocessCount3");
            ss.Reprocess(Schema4);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 4, "ReprocessCount4");

            ss.Compile();
            CError.Compare(ss.Count, 4, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");
            return;
        }

        //[Variation(Desc = "v124- Import: Bug 114549 , A imports only B's NS, and B also refers to A's types (WARNING)", Priority = 1, Params = new object[] { "import_v23_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v23_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        //[Variation(Desc = "v125- Import: Bug 114549 , A imports only B's NS, and B also improts A's NS AND refers to A's type, D refers to A's type (WARNING)", Priority = 1, Params = new object[] { "import_v24_b.xsd", "import_v21_c.xsd", "import_v25_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v24_b.xsd", "import_v21_c.xsd", "import_v25_d.xsd", "import_v22_a.xsd")]
        [Theory]
        public void v119(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet ss = new XmlSchemaSet();

            XmlSchema Schema1 = ss.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            XmlSchema Schema2 = ss.Add(null, Path.Combine(TestData._Root, param1.ToString()));
            XmlSchema Schema3 = ss.Add(null, Path.Combine(TestData._Root, param2.ToString()));
            XmlSchema Schema4 = ss.Add(null, Path.Combine(TestData._Root, param3.ToString()));
            CError.Compare(ss.Count, 4, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Reprocess(Schema1);
            CError.Compare(ss.Count, 4, "ReprocessCount1");
            ss.Reprocess(Schema2);
            CError.Compare(ss.Count, 4, "ReprocessCount2");
            ss.Reprocess(Schema3);
            CError.Compare(ss.Count, 4, "ReprocessCount3");
            ss.Reprocess(Schema4);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 4, "ReprocessCount4");

            ss.Compile();
            CError.Compare(ss.Count, 4, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            return;
        }

        //[Variation(Desc = "v120 - Import: Bug 105897", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v120()
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema1 = ss.Add(null, Path.Combine(TestData._Root, "105897.xsd"));
            XmlSchema Schema2 = ss.Add(null, Path.Combine(TestData._Root, "105897_a.xsd"));
            CError.Compare(ss.Count, 3, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Reprocess(Schema1);
            CError.Compare(ss.Count, 3, "ReprocessCount1");
            ss.Reprocess(Schema2);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 3, "ReprocessCount2");

            ss.Compile();
            CError.Compare(ss.Count, 3, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings |
                                       XmlSchemaValidationFlags.ProcessSchemaLocation |
                                       XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.Schemas = new XmlSchemaSet();
            settings.Schemas.Add(ss);

            using (XmlReader vr = XmlReader.Create(Path.Combine(TestData._Root, "105897.xml"), settings))
            {
                while (vr.Read()) ;
            }
            return;
        }

        /*********compile reprocess import**************/

        //[Variation(Desc = "v201.3 - Import: A(ns-a) which improts B (no ns)", Priority = 0, Params = new object[] { "import_v4_a.xsd", "import_v4_b.xsd", 2, null })]
        [InlineData("import_v4_a.xsd", "import_v4_b.xsd", 2, null)]
        //[Variation(Desc = "v201.2 - Import: A(ns-a) improts B (ns-b)", Priority = 0, Params = new object[] { "import_v2_a.xsd", "import_v2_b.xsd", 2, "ns-b" })]
        [InlineData("import_v2_a.xsd", "import_v2_b.xsd", 2, "ns-b")]
        //[Variation(Desc = "v201.1 - Improt: A with NS imports B with no NS", Priority = 0, Params = new object[] { "import_v1_a.xsd", "include_v1_b.xsd", 2, null })]
        [InlineData("import_v1_a.xsd", "include_v1_b.xsd", 2, null)]
        [Theory]
        public void v201(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            CError.Compare(sc.Count, param2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, param2, "Count");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, param2, "ReprocessCount");
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals(param1.ToString()) && imp.Schema.TargetNamespace == (string)param3)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------

        //[Variation(Desc = "v202.1 - Import: Add B(ns-b) , then A(ns-a) which improts B (ns-b)", Priority = 1, Params = new object[] { "import_v2_a.xsd", "import_v2_b.xsd", 2, null, "ns-b" })]
        [InlineData("import_v2_a.xsd", "import_v2_b.xsd", 2, null, "ns-b")]
        [Theory]
        public void v202(object param0, object param1, object param2, object param3, object param4)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema sch = sc.Add((string)param3, Path.Combine(TestData._Root, param1.ToString()));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(sch);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            CError.Compare(sc.Count, param2, "Add2Count");
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 2, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals(param1.ToString()) && imp.Schema.TargetNamespace == (string)param4)
                    return;

            Assert.True(false);
        }

        //[Variation(Desc = "v202.2 - Import: Add B(no ns) with ns-b , then A(ns-a) which imports B (no ns)", Priority = 1, Params = new object[] { "import_v5_a.xsd", "import_v4_b.xsd", 3, "ns-b", null })]
        [InlineData("import_v5_a.xsd", "import_v4_b.xsd", 3, "ns-b", null)]
        [Theory]
        public void v202a(object param0, object param1, object param2, object param3, object param4)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema sch = sc.Add((string)param3, Path.Combine(TestData._Root, param1.ToString()));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(sch);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");
            try
            {
                XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                Assert.True(false);
            }
            catch (XmlSchemaException) { }
            CError.Compare(sc.Count, 1, "Add2Count");
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");

            try
            {
                sc.Compile();
                Assert.True(false);
            }
            catch (XmlSchemaException) { }
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.IsCompiled, false, "CompileIsCompiled");

            sc.Reprocess(sch);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v203 - Import: Add A(ns-a) which imports B (no ns), then Add B(no ns) again", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v203()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            XmlSchema orig = sc.Add(null, Path.Combine(TestData._Root, "import_v4_b.xsd")); // should be already present in the set
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");
            CError.Compare(sc.Count, 2, "Add2Count");
            CError.Compare(orig.SourceUri.Contains("import_v4_b.xsd"), true, "Compare the schema object");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Reprocess(orig);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v4_b.xsd") && imp.Schema.TargetNamespace == null)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v204 - Import: Add A(ns-a) which improts B (no ns), then Add B to ns-b", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v204()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            XmlSchema sch_B = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v4_b.xsd")); // should be already present in the set
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 3, "CompileCount");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v4_b.xsd") && imp.Schema.TargetNamespace == null)
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v205 - Import: Add A(ns-a) which improts B (ns-b), then Add B(ns-b) again", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v205()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v2_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            XmlSchema sch_B = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v2_b.xsd")); // should be already present in the set
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v2_b.xsd") && imp.Schema.TargetNamespace.Equals("ns-b"))
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v206 - Import: A(ns-a) imports B(ns-b) imports C (ns-c)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v206()
        {
            bool found = false;
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v9_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            XmlSchema sch_B = sc.Add(null, Path.Combine(TestData._Root, "import_v9_b.xsd")); // should be already present in the set
            sc.Add(null, Path.Combine(TestData._Root, "import_v9_c.xsd"));				   // should be already present in the set
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v9_b.xsd") && imp.Schema.TargetNamespace.Equals("ns-b"))
                    found = true;
            if (!found) Assert.True(false);

            // check that schema C in sch_b.Includes and its NS correct.
            foreach (XmlSchemaImport imp in sch_B.Includes)
                if (imp.SchemaLocation.Equals("import_v9_c.xsd") && imp.Schema.TargetNamespace.Equals("ns-c"))
                    return;

            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v207 - Import: A(ns-a) imports B(NO NS) imports C (ns-c)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v207()
        {
            bool found = false;
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v10_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            XmlSchema sch_B = sc.Add(null, Path.Combine(TestData._Root, "import_v10_b.xsd")); // should be already present in the set
            sc.Add(null, Path.Combine(TestData._Root, "import_v10_c.xsd"));				   // should be already present in the set
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 3, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");

            // check that schema is present in parent.Includes and its NS correct.
            foreach (XmlSchemaImport imp in parent.Includes)
                if (imp.SchemaLocation.Equals("import_v10_b.xsd") && imp.Schema.TargetNamespace == null)
                    found = true;

            if (!found) Assert.True(false);

            // check that schema C in sch_b.Includes and its NS correct.
            foreach (XmlSchemaImport imp in sch_B.Includes)
                if (imp.SchemaLocation.Equals("import_v10_c.xsd") && imp.Schema.TargetNamespace.Equals("ns-c"))
                    found = true;

            if (!found) Assert.True(false);

            // try adding no ns schema with an ns
            sch_B = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v10_b.xsd"));
            CError.Compare(sc.Count, 4, "Count");

            sc.Compile();
            CError.Compare(sc.Count, 4, "CompileCount");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");

            sc.Reprocess(sch_B);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v208 - Import: A(ns-a) imports B(ns-b) imports C (ns-a)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v208()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v11_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 2, "count for ns-a");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v209 - Import: A(ns-a) imports B(ns-b) and C (ns-b)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v209()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v12_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-b").Count, 2, "count for ns-b");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v210 - Import: A imports B and B and C, B imports C and D, C imports D and A", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v210()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v13_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 4, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 4, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v211 - Import: A(ns-a) imports B(ns-b) and C (ns-b), B and C include each other", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v211()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v14_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 3, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-b").Count, 2, "count for ns-b");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v212 - Import: A(ns-a) imports B(BOGUS) and C (ns-c)", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v212()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v15_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
            CError.Compare(sc.Count, 2, "AddCount");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v213 - Import: B(ns-b) added, A(ns-a) imports B with bogus url", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v213()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add(null, Path.Combine(TestData._Root, "import_v16_b.xsd"));
            CError.Compare(sc.IsCompiled, false, "Add1IsCompiled");
            CError.Compare(sc.Count, 1, "Add1Count");

            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v16_a.xsd"));
            CError.Compare(sc.IsCompiled, false, "Add2IsCompiled");
            CError.Compare(sc.Count, 2, "Add2Count");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v214 - Import: A(ns-a) includes B(ns-a) which imports C(ns-c)", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v214()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v17_a.xsd"));

            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v215 - Import: A(ns-a) includes A(ns-a) of v17", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v215()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v18_a.xsd"));

            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 1, "count for ns-c");
            CError.Compare(sc.Count, 2, "CompileCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 2, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v216 - Import: A(ns-b) imports A(ns-a) of v17", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v216()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v19_a.xsd"));
            CError.Compare(sc.Count, 3, "Count");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Schemas("ns-a").Count, 1, "count for ns-a");
            CError.Compare(sc.Schemas("ns-c").Count, 2, "count for ns-c");
            CError.Compare(sc.Count, 3, "CompileCount");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 3, "ReprocessCount");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v217 - Import: A,B,C,D all import and reference each other for types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v217()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v20_a.xsd"));
            CError.Compare(sc.Count, 4, "Count");

            sc.Reprocess(parent);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 4, "CompileCount");

            XmlSchema b = sc.Add(null, Path.Combine(TestData._Root, "import_v20_b.xsd"));
            XmlSchema c = sc.Add(null, Path.Combine(TestData._Root, "import_v20_c.xsd"));
            XmlSchema d = sc.Add(null, Path.Combine(TestData._Root, "import_v20_d.xsd"));

            CError.Compare(sc.Count, 4, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(b.SourceUri.Contains("import_v20_b.xsd"), true, "Compare B");
            CError.Compare(c.SourceUri.Contains("import_v20_c.xsd"), true, "Compare C");
            CError.Compare(d.SourceUri.Contains("import_v20_d.xsd"), true, "Compare D");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 4, "CompileCount");

            sc.Reprocess(b);
            CError.Compare(sc.Count, 4, "b ReprocessCount");
            sc.Reprocess(c);
            CError.Compare(sc.Count, 4, "c ReprocessCount");
            sc.Reprocess(d);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 4, "d ReprocessCount");
            return;
        }

        //[Variation(Desc = "v221- Import: Bug 114549 , A imports only B but refers to C and D both", Priority = 1, Params = new object[] { "import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v21_a.xsd" })]
        [InlineData("import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v21_a.xsd")]
        //[Variation(Desc = "v222- Import: Bug 114549 , A imports only B's NS, but refers to B,C and D both", Priority = 1, Params = new object[] { "import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v21_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        //[Variation(Desc = "v223- Import: Bug 114549 , A imports only B's NS, and B also improts A's NS AND refers to A's types", Priority = 1, Params = new object[] { "import_v24_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v24_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        [Theory]
        public void v218(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema1 = ss.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            XmlSchema Schema2 = ss.Add(null, Path.Combine(TestData._Root, param1.ToString()));
            XmlSchema Schema3 = ss.Add(null, Path.Combine(TestData._Root, param2.ToString()));
            XmlSchema Schema4 = ss.Add(null, Path.Combine(TestData._Root, param3.ToString()));
            CError.Compare(ss.Count, 4, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 4, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            ss.Reprocess(Schema1);
            CError.Compare(ss.Count, 4, "ReprocessCount1");
            ss.Reprocess(Schema2);
            CError.Compare(ss.Count, 4, "ReprocessCount2");
            ss.Reprocess(Schema3);
            CError.Compare(ss.Count, 4, "ReprocessCount3");
            ss.Reprocess(Schema4);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 4, "ReprocessCount4");
            return;
        }

        //[Variation(Desc = "v224- Import: Bug 114549 , A imports only B's NS, and B also refers to A's types (WARNING)", Priority = 1, Params = new object[] { "import_v23_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v23_b.xsd", "import_v21_c.xsd", "import_v21_d.xsd", "import_v22_a.xsd")]
        //[Variation(Desc = "v225- Import: Bug 114549 , A imports only B's NS, and B also improts A's NS AND refers to A's type, D refers to A's type (WARNING)", Priority = 1, Params = new object[] { "import_v24_b.xsd", "import_v21_c.xsd", "import_v25_d.xsd", "import_v22_a.xsd" })]
        [InlineData("import_v24_b.xsd", "import_v21_c.xsd", "import_v25_d.xsd", "import_v22_a.xsd")]
        [Theory]
        public void v219(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet ss = new XmlSchemaSet();

            XmlSchema Schema1 = ss.Add(null, Path.Combine(TestData._Root, param0.ToString()));
            XmlSchema Schema2 = ss.Add(null, Path.Combine(TestData._Root, param1.ToString()));
            XmlSchema Schema3 = ss.Add(null, Path.Combine(TestData._Root, param2.ToString()));
            XmlSchema Schema4 = ss.Add(null, Path.Combine(TestData._Root, param3.ToString()));
            CError.Compare(ss.Count, 4, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 4, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            ss.Reprocess(Schema1);
            CError.Compare(ss.Count, 4, "ReprocessCount1");
            ss.Reprocess(Schema2);
            CError.Compare(ss.Count, 4, "ReprocessCount2");
            ss.Reprocess(Schema3);
            CError.Compare(ss.Count, 4, "ReprocessCount3");
            ss.Reprocess(Schema4);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 4, "ReprocessCount4");

            return;
        }

        //[Variation(Desc = "v220 - Import: Bug 105897", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v220()
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema1 = ss.Add(null, Path.Combine(TestData._Root, "105897.xsd"));
            XmlSchema Schema2 = ss.Add(null, Path.Combine(TestData._Root, "105897_a.xsd"));
            CError.Compare(ss.Count, 3, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 3, "Count mismatch!");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            ss.Reprocess(Schema1);
            CError.Compare(ss.Count, 3, "ReprocessCount1");
            ss.Reprocess(Schema2);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 3, "ReprocessCount2");

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings |
                                       XmlSchemaValidationFlags.ProcessSchemaLocation |
                                       XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.Schemas = new XmlSchemaSet();
            settings.Schemas.Add(ss);

            using (XmlReader vr = XmlReader.Create(Path.Combine(TestData._Root, "105897.xml"), settings))
            {
                while (vr.Read()) ;
            }
            return;
        }
    }
}
