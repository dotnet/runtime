// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Collections;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Remove", Desc = "")]
    public class TC_SchemaSet_Remove : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Remove(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - Remove with null", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Remove(null);
            }
            catch (ArgumentNullException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - Remove with just added schema", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            sc.Compile();
            sc.Remove(Schema1);

            CError.Compare(sc.Count, 0, "Count");
            ICollection Col = sc.Schemas();
            CError.Compare(Col.Count, 0, "ICollection.Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - Remove first added schema, check the rest", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add("test", TestData._XsdNoNs);
            sc.Compile();
            sc.Remove(Schema1);

            CError.Compare(sc.Count, 1, "Count");
            ICollection Col = sc.Schemas();
            CError.Compare(Col.Count, 1, "ICollection.Count");
            CError.Compare(sc.Contains("test"), true, "Contains");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4.6 - Remove  A(ns-a) include B(ns-a) which includes C(ns-a) ", Priority = 1, Params = new object[] { "include_v7_a.xsd" })]
        [InlineData("include_v7_a.xsd")]
        //[Variation(Desc = "v4.5 - Remove: A with NS includes B and C with no NS", Priority = 1, Params = new object[] { "include_v6_a.xsd" })]
        [InlineData("include_v6_a.xsd")]
        //[Variation(Desc = "v4.4 - Remove: A with NS includes B and C with no NS, B also includes C", Priority = 1, Params = new object[] { "include_v5_a.xsd" })]
        [InlineData("include_v5_a.xsd")]
        //[Variation(Desc = "v4.3 - Remove: A with NS includes B with no NS, which includes C with no NS", Priority = 1, Params = new object[] { "include_v4_a.xsd" })]
        [InlineData("include_v4_a.xsd")]
        //[Variation(Desc = "v4.2 - Remove: A with no NS includes B with no NS", Params = new object[] { "include_v3_a.xsd" })]
        [InlineData("include_v3_a.xsd")]
        //[Variation(Desc = "v4.1 - Remove: A with NS includes B with no NS", Params = new object[] { "include_v1_a.xsd", })]
        [InlineData("include_v1_a.xsd")]
        [Theory]
        public void v4(object param0)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename

                sc.Compile();
                sc.Remove(Schema2);
                CError.Compare(sc.Count, 1, "Count");
                ICollection Col = sc.Schemas();
                CError.Compare(Col.Count, 1, "ICollection.Count");
            }
            catch (Exception)
            {
                Assert.True(false);
            }

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5.3 - Remove: A(ns-a) which imports B (no ns)", Priority = 1, Params = new object[] { "import_v4_a.xsd", 2 })]
        [InlineData("import_v4_a.xsd", 2)]
        //[Variation(Desc = "v5.2 - Remove: A(ns-a) improts B (ns-b)", Priority = 1, Params = new object[] { "import_v2_a.xsd", 2 })]
        [InlineData("import_v2_a.xsd", 2)]
        //[Variation(Desc = "v5.1 - Remove: A with NS imports B with no NS", Priority = 1, Params = new object[] { "import_v1_a.xsd", 2 })]
        [InlineData("import_v1_a.xsd", 2)]
        [Theory]
        public void v5(object param0, object param1)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename

                sc.Compile();
                sc.Remove(Schema2);
                CError.Compare(sc.Count, param1, "Count");
                ICollection Col = sc.Schemas();
                CError.Compare(Col.Count, param1, "ICollection.Count");
            }
            catch (Exception)
            {
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - Remove: Add B(NONS) to a namespace, Add A(ns-a) which imports B, Remove B(nons)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v4_b.xsd"));
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd")); // param as filename
                sc.Compile();
                sc.Remove(Schema1);
                CError.Compare(sc.Count, 2, "Count");
                ICollection Col = sc.Schemas();
                CError.Compare(Col.Count, 2, "ICollection.Count");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains(String.Empty), true, "Contains");
                CError.Compare(sc.Contains("ns-a"), true, "Contains");
            }
            catch (Exception)
            {
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v7 - Remove: Add B(NONS) to a namespace, Add A(ns-a) which improts B, Remove B(ns-b)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v7()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v4_b.xsd"));
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd")); // param as filename
                sc.Compile();
                ICollection col = sc.Schemas(String.Empty);
                foreach (XmlSchema schema in col)
                {
                    sc.Remove(schema); //should remove just one
                }

                CError.Compare(sc.Count, 2, "Count");
                ICollection Col = sc.Schemas();
                CError.Compare(Col.Count, 2, "ICollection.Count");
                CError.Compare(sc.Contains("ns-b"), true, "Contains");
                CError.Compare(sc.Contains(String.Empty), false, "Contains");
                CError.Compare(sc.Contains("ns-a"), true, "Contains");
            }
            catch (Exception)
            {
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v8.2 - Remove: A(ns-a) imports B(NO NS) imports C (ns-c)", Priority = 1, Params = new object[] { "import_v10_a.xsd", "ns-a", "", "ns-c" })]
        [InlineData("import_v10_a.xsd", "ns-a", "", "ns-c")]
        //[Variation(Desc = "v8.1 - Remove: A(ns-a) imports B(ns-b) imports C (ns-c)", Priority = 1, Params = new object[] { "import_v9_a.xsd", "ns-a", "ns-b", "ns-c" })]
        [InlineData("import_v9_a.xsd", "ns-a", "ns-b", "ns-c")]
        [Theory]
        public void v8(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                sc.Compile();
                CError.Compare(sc.Count, 3, "Count");

                ICollection col = sc.Schemas(param2.ToString());

                foreach (XmlSchema schema in col)
                {
                    sc.Remove(schema); //should remove just one
                }

                CError.Compare(sc.Count, 2, "Count");
                CError.Compare(sc.Contains(param2.ToString()), false, "Contains");
                col = sc.Schemas(param3.ToString());
                foreach (XmlSchema schema in col)
                {
                    sc.Remove(schema); //should remove just one
                }

                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains(param3.ToString()), false, "Contains");
                CError.Compare(sc.Contains(param1.ToString()), true, "Contains");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v9 - Remove: A imports B and B and C, B imports C and D, C imports D and A", Priority = 1, Params = new object[] { "import_v13_a.xsd" })]
        [InlineData("import_v13_a.xsd")]
        [Theory]
        public void v9(object param0)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                sc.Compile();
                CError.Compare(sc.Count, 4, "Count");

                ICollection col = sc.Schemas("ns-d");
                foreach (XmlSchema schema in col)
                {
                    sc.Remove(schema); //should remove just one
                }

                CError.Compare(sc.Count, 3, "Count");
                CError.Compare(sc.Contains("ns-d"), false, "Contains");

                col = sc.Schemas("ns-c");
                foreach (XmlSchema schema in col)
                {
                    sc.Remove(schema); //should remove just one
                }

                CError.Compare(sc.Count, 2, "Count");
                CError.Compare(sc.Contains("ns-c"), false, "Contains");

                col = sc.Schemas("ns-b");
                foreach (XmlSchema schema in col)
                {
                    sc.Remove(schema); //should remove just one
                }

                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains("ns-a"), true, "Contains");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v10 - Import: B(ns-b) added, A(ns-a) imports B's NS", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v10()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.XmlResolver = new XmlUrlResolver();
                sc.Add(null, Path.Combine(TestData._Root, "import_v16_b.xsd"));
                XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v16_a.xsd"));
                sc.Compile();
                sc.Remove(parent);
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains("ns-b"), true, "Contains");
                return;
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine("Exception : " + e.Message);
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v11.2 - Remove: A(ns-a) improts B (ns-b), Remove imported schema", Priority = 2, Params = new object[] { "import_v2_a.xsd", "import_v2_b.xsd" })]
        [InlineData("import_v2_a.xsd", "import_v2_b.xsd")]
        //[Variation(Desc = "v11.1 - Remove: A with NS imports B with no NS, Remove imported schema", Priority = 2, Params = new object[] { "import_v1_a.xsd", "include_v1_b.xsd" })]
        [InlineData("import_v1_a.xsd", "include_v1_b.xsd")]
        [Theory]
        public void v11(object param0, object param1)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, param1.ToString())); // param as filename
                sc.Compile();
                CError.Compare(sc.Count, 2, "Count");
                sc.Remove(Schema2);
                sc.Compile();
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.GlobalElements.Count, 2, "GlobalElems Count");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //[Variation(Desc = "v20 - 358206 : Removing the last schema from the set should clear the global tables", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v20()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema = sc.Add(null, TestData._XsdAuthor); // param as filename
            sc.Compile();
            CError.Compare(sc.Count, 1, "Count before remove");
            sc.Remove(Schema);
            sc.Compile();
            CError.Compare(sc.Count, 0, "Count after remove");
            CError.Compare(sc.GlobalElements.Count, 0, "GlobalElems Count");
            return;
        }
    }
}
