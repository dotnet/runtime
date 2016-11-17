// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Collections;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_RemoveRecursive", Desc = "")]
    public class TC_SchemaSet_RemoveRecursive
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_RemoveRecursive(ITestOutputHelper output)
        {
            _output = output;
        }


        public bool bWarningCallback = false;
        public bool bErrorCallback = false;

        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                _output.WriteLine("WARNING: ");
                bWarningCallback = true;
            }
            else if (args.Severity == XmlSeverityType.Error)
            {
                _output.WriteLine("ERROR: ");
                bErrorCallback = true;
            }
            _output.WriteLine(args.Message); // Print the error to the screen.
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - RemoveRecursive with null", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.RemoveRecursive(null);
            }
            catch (ArgumentNullException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - RemoveRecursive with just added schema", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            //remove after compile
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            sc.Compile();
            sc.RemoveRecursive(Schema1);
            CError.Compare(sc.Count, 0, "Count");
            ICollection Col = sc.Schemas();
            CError.Compare(Col.Count, 0, "ICollection.Count");

            //remove before compile
            Schema1 = sc.Add(null, TestData._XsdAuthor);
            sc.RemoveRecursive(Schema1);
            CError.Compare(sc.Count, 0, "Count");
            Col = sc.Schemas();
            CError.Compare(Col.Count, 0, "ICollection.Count"); return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - RemoveRecursive first added schema, check the rest")]
        [InlineData()]
        [Theory]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add("test", TestData._XsdNoNs);
            sc.Compile();
            sc.RemoveRecursive(Schema1);
            CError.Compare(sc.Count, 1, "Count");
            ICollection Col = sc.Schemas();
            CError.Compare(Col.Count, 1, "ICollection.Count");
            CError.Compare(sc.Contains("test"), true, "Contains");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4.6 - RemoveRecursive  A(ns-a) include B(ns-a) which includes C(ns-a) ", Priority = 1, Params = new object[] { "include_v7_a.xsd" })]
        [InlineData("include_v7_a.xsd")]
        //[Variation(Desc = "v4.5 - RemoveRecursive: A with NS includes B and C with no NS", Priority = 1, Params = new object[] { "include_v6_a.xsd" })]
        [InlineData("include_v6_a.xsd")]
        //[Variation(Desc = "v4.4 - RemoveRecursive: A with NS includes B and C with no NS, B also includes C", Priority = 1, Params = new object[] { "include_v5_a.xsd" })]
        [InlineData("include_v5_a.xsd")]
        //[Variation(Desc = "v4.3 - RemoveRecursive: A with NS includes B with no NS, which includes C with no NS", Priority = 1, Params = new object[] { "include_v4_a.xsd" })]
        [InlineData("include_v4_a.xsd")]
        //[Variation(Desc = "v4.2 - RemoveRecursive: A with no NS includes B with no NS", Params = new object[] { "include_v3_a.xsd" })]
        [InlineData("include_v3_a.xsd")]
        //[Variation(Desc = "v4.1 - RemoveRecursive: A with NS includes B with no NS", Params = new object[] { "include_v1_a.xsd", })]
        [InlineData("include_v1_a.xsd")]
        [Theory]
        public void v4(string fileName)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                // remove after compile
                XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, fileName)); // param as filename
                sc.Compile();
                sc.RemoveRecursive(Schema2);
                CError.Compare(sc.Count, 1, "Count");
                ICollection Col = sc.Schemas();
                CError.Compare(Col.Count, 1, "ICollection.Count");

                //remove before compile
                Schema2 = sc.Add(null, Path.Combine(TestData._Root, fileName)); // param as filename
                CError.Compare(sc.Count, 2, "Count");
                sc.RemoveRecursive(Schema2);
                CError.Compare(sc.Count, 1, "Count");
                Col = sc.Schemas();
                CError.Compare(Col.Count, 1, "ICollection.Count");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5.2 - Remove: A(ns-a) improts B (ns-b)", Priority = 1, Params = new object[] { "import_v2_a.xsd", 1 })]
        [InlineData("import_v2_a.xsd", 1)]
        //[Variation(Desc = "v5.1 - Remove: A with NS imports B with no NS", Priority = 1, Params = new object[] { "import_v1_a.xsd", 1 })]
        [InlineData("import_v1_a.xsd", 1)]
        [Theory]
        public void v5(object param0, object param1)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);

                //remove after compile
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename
                sc.Compile();
                sc.RemoveRecursive(Schema2);
                CError.Compare(sc.Count, param1, "Count");
                ICollection Col = sc.Schemas();
                CError.Compare(Col.Count, param1, "ICollection.Count");

                //remove before compile
                Schema2 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename
                sc.RemoveRecursive(Schema2);
                CError.Compare(sc.Count, param1, "Count");
                Col = sc.Schemas();
                CError.Compare(Col.Count, param1, "ICollection.Count");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - Remove: Add B(NONS) to a namespace, Add A(ns-a) which imports B, Remove B(ns-b) then A(ns-a)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                //after compile
                XmlSchema Schema1 = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v4_b.xsd"));
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd")); // param as filename
                sc.Compile();
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 2, "Count");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains(String.Empty), true, "Contains");
                CError.Compare(sc.Contains("ns-a"), true, "Contains");

                sc.RemoveRecursive(Schema2);
                ICollection Col = sc.Schemas();
                CError.Compare(Col.Count, 0, "ICollection.Count");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains(String.Empty), false, "Contains");
                CError.Compare(sc.Contains("ns-a"), false, "Contains");

                //before compile
                Schema1 = sc.Add("ns-b", Path.Combine(TestData._Root, "import_v4_b.xsd"));
                Schema2 = sc.Add(null, Path.Combine(TestData._Root, "import_v5_a.xsd")); // param as filename
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 2, "Count");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains(String.Empty), true, "Contains");
                CError.Compare(sc.Contains("ns-a"), true, "Contains");
                sc.RemoveRecursive(Schema2);
                Col = sc.Schemas();
                CError.Compare(Col.Count, 0, "ICollection.Count");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains(String.Empty), false, "Contains");
                CError.Compare(sc.Contains("ns-a"), false, "Contains");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v7.2 - Remove: A(ns-a) imports B(NO NS) imports C (ns-c)", Priority = 1, Params = new object[] { "import_v10_a.xsd", "ns-a", "", "ns-c" })]
        [InlineData("import_v10_a.xsd", "ns-a", "", "ns-c")]
        //[Variation(Desc = "v7.1 - Remove: A(ns-a) imports B(ns-b) imports C (ns-c)", Priority = 1, Params = new object[] { "import_v9_a.xsd", "ns-a", "ns-b", "ns-c" })]
        [InlineData("import_v9_a.xsd", "ns-a", "ns-b", "ns-c")]
        [Theory]
        public void v7(object param0, object param1, object param2, object param3)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                //after compile
                XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                sc.Compile();
                CError.Compare(sc.Count, 3, "Count");
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 0, "Count");
                CError.Compare(sc.Contains(param1.ToString()), false, "Contains");
                CError.Compare(sc.Contains(param2.ToString()), false, "Contains");
                CError.Compare(sc.Contains(param3.ToString()), false, "Contains");

                //before compile
                Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                CError.Compare(sc.Count, 3, "Count");
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 0, "Count");
                CError.Compare(sc.Contains(param1.ToString()), false, "Contains");
                CError.Compare(sc.Contains(param2.ToString()), false, "Contains");
                CError.Compare(sc.Contains(param3.ToString()), false, "Contains");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v8 - Remove: A imports B and B and C, B imports C and D, C imports D and A", Priority = 1, Params = new object[] { "import_v13_a.xsd" })]
        [InlineData("import_v13_a.xsd")]
        [Theory]
        public void v8(object param0)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            try
            {
                //after compile
                XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                sc.Compile();
                CError.Compare(sc.Count, 4, "Count");
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 0, "Count");
                CError.Compare(sc.Contains("ns-d"), false, "Contains");
                CError.Compare(sc.Contains("ns-c"), false, "Contains");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains("ns-a"), false, "Contains");

                //before compile
                Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                CError.Compare(sc.Count, 4, "Count");
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 0, "Count");
                CError.Compare(sc.Contains("ns-d"), false, "Contains");
                CError.Compare(sc.Contains("ns-c"), false, "Contains");
                CError.Compare(sc.Contains("ns-b"), false, "Contains");
                CError.Compare(sc.Contains("ns-a"), false, "Contains");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v9 - Import: B(ns-b) added, A(ns-a) imports B's NS", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v9()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Add(null, Path.Combine(TestData._Root, "import_v16_b.xsd"));

                //before compile
                XmlSchema parent = sc.Add(null, Path.Combine(TestData._Root, "import_v16_a.xsd"));
                sc.Compile();
                sc.RemoveRecursive(parent);
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains("ns-b"), true, "Contains");

                //after compile
                parent = sc.Add(null, Path.Combine(TestData._Root, "import_v16_a.xsd"));
                sc.RemoveRecursive(parent);
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

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v10.8 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports A's NS, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e8.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e8.xsd")]
        //[Variation(Desc = "v10.7 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports C's NS, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e7.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e7.xsd")]
        //[Variation(Desc = "v10.6 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports D's NS, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e6.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e6.xsd")]
        //[Variation(Desc = "v10.5 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports B's NS, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e5.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e5.xsd")]
        //[Variation(Desc = "v10.4 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports A, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e4.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e4.xsd")]
        //[Variation(Desc = "v10.3 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports C, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e3.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e3.xsd")]
        //[Variation(Desc = "v10.2 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports D, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e2.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e2.xsd")]
        //[Variation(Desc = "v10.1 - Remove: A imports B and B and C, B imports C and D, C imports D and A, E imports B, Remove A", Priority = 1, Params = new object[] { "import_v13_a.xsd", "remove_v10_e1.xsd" })]
        [InlineData("import_v13_a.xsd", "remove_v10_e1.xsd")]
        [Theory]
        public void v10(object param0, object param1)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            bWarningCallback = false;
            bErrorCallback = false;

            try
            {
                //after compile
                XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                XmlSchema Schema2 = sc.Add(null, Path.Combine(TestData._Root, param1.ToString()));
                sc.Compile();
                CError.Compare(sc.Count, 5, "Count");
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 5, "Count");
                CError.Compare(bWarningCallback, true, "Warning Callback");
                CError.Compare(bErrorCallback, false, "Error Callback");

                //reinit
                bWarningCallback = false;
                bErrorCallback = false;
                sc.Remove(Schema2);
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 0, "Count");
                CError.Compare(bWarningCallback, false, "Warning Callback");
                CError.Compare(bErrorCallback, false, "Error Callback");

                //before compile
                Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                Schema2 = sc.Add(null, Path.Combine(TestData._Root, param1.ToString()));
                CError.Compare(sc.Count, 5, "Count");
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 5, "Count");
                CError.Compare(bWarningCallback, true, "Warning Callback");
                CError.Compare(bErrorCallback, false, "Error Callback");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //[Variation(Desc = "v11 - Remove: A imports B and C, B imports C, C imports B and D, Remove A", Priority = 1, Params = new object[] { "remove_v11_a.xsd" })]
        [InlineData("remove_v11_a.xsd")]
        [Theory]
        public void v11(object param0)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            bWarningCallback = false;
            bErrorCallback = false;
            try
            {
                //after compile
                XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                sc.Compile();
                CError.Compare(sc.Count, 4, "Count");
                sc.RemoveRecursive(Schema1);
                sc.Compile();
                CError.Compare(sc.Count, 0, "Count");
                CError.Compare(sc.GlobalElements.Count, 0, "Global Elements Count");
                CError.Compare(sc.GlobalTypes.Count, 0, "Global Types Count");//should contain xs:anyType

                //before compile
                Schema1 = sc.Add(null, Path.Combine(TestData._Root, param0.ToString()));
                CError.Compare(sc.Count, 4, "Count");
                sc.RemoveRecursive(Schema1);
                CError.Compare(sc.Count, 0, "Count");
                CError.Compare(sc.GlobalElements.Count, 0, "Global Elements Count");
                CError.Compare(sc.GlobalTypes.Count, 0, "Global Types Count"); //should contain xs:anyType
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }
    }
}
