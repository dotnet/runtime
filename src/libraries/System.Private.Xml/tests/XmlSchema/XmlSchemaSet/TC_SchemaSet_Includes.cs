// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Includes", Desc = "")]
    public class TC_SchemaSet_Includes
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Includes(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------

        //[Variation(Desc = "v1.6 - Include: A(ns-a) include B(ns-a) which includes C(ns-a) ", Priority = 2, Params = new object[] { "include_v7_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v7_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v1.5 - Include: A with NS includes B and C with no NS", Priority = 2, Params = new object[] { "include_v6_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v6_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v1.4 - Include: A with NS includes B and C with no NS, B also includes C", Priority = 2, Params = new object[] { "include_v5_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v5_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v1.3 - Include: A with NS includes B with no NS, which includes C with no NS", Priority = 2, Params = new object[] { "include_v4_a.xsd", 1, "ns-a:c-e2" })]
        [InlineData("include_v4_a.xsd", 1, "ns-a:c-e2")]
        //[Variation(Desc = "v1.2 - Include: A with no NS includes B with no NS", Priority = 0, Params = new object[] { "include_v3_a.xsd", 1, "e2" })]
        [InlineData("include_v3_a.xsd", 1, "e2")]
        //[Variation(Desc = "v1.1 - Include: A with NS includes B with no NS", Priority = 0, Params = new object[] { "include_v1_a.xsd", 1, "ns-a:e2" })]
        [InlineData("include_v1_a.xsd", 1, "ns-a:e2")]
        [Theory]
        public void v1(object param0, object param1, object param2)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename
            CError.Compare(sc.Count, param1, "AddCount"); //compare the count
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, param1, "Count");

            // Check that B's data is present in the NS for A
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals(param2.ToString()))
                    return;
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - Include: A with NS includes B with a diff NS (INVALID)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema schema = new XmlSchema();
            sc.Add(null, TestData._XsdNoNs);
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            try
            {
                schema = sc.Add(null, Path.Combine(TestData._Root, "include_v2.xsd"));
            }
            catch (XmlSchemaException)
            {
                // no schema should be addded to the set.
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.IsCompiled, true, "IsCompiled");
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - Include: A(ns-a) which includes B(ns-a) twice", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v8()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v8_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;
            CError.Compare(elem_count, 1, "ns-a:e2");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4 - Include: A(ns-a) which includes B(No NS) twice", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v9()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v9_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e2");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5 - Include: A,B,C all include each other, all with no ns and refer each others' types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v10()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v10_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("c-e2"))
                    elem_count++;
            CError.Compare(elem_count, 1, "c-e2");
            elem_count = 0;
            // Check that B's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("b-e1"))
                    elem_count++;
            CError.Compare(elem_count, 1, "b-e1");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - Include: A,B,C all include each other, all with same ns and refer each others' types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v11()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v11_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            // Check that A's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e1"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e1");
            elem_count = 0;

            // Check B's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e2");
            elem_count = 0;

            // Check C's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e3"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e3");

            return;
        }

        //[Variation(Desc = "v12 - 20008213 SOM: SourceUri property on a chameleon include is not set", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v12()
        {
            bool succeeded = false;

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema a = ss.Add(null, Path.Combine(TestData._Root, "include_v12_a.xsd"));
            CError.Compare(ss.Count, 1, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 1, "Count");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            foreach (XmlSchemaExternal s in a.Includes)
            {
                if (String.IsNullOrEmpty(s.Schema.SourceUri))
                {
                    CError.Compare(false, "Unexpected null uri");
                }
                else if (s.Schema.SourceUri.EndsWith("include_v12_b.xsd"))
                {
                    succeeded = true;
                }
            }
            Assert.True(succeeded);
        }

        /******** reprocess compile include **********/

        //[Variation(Desc = "v101.6 - Include: A(ns-a) include B(ns-a) which includes C(ns-a) ", Priority = 2, Params = new object[] { "include_v7_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v7_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v101.5 - Include: A with NS includes B and C with no NS", Priority = 2, Params = new object[] { "include_v6_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v6_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v101.4 - Include: A with NS includes B and C with no NS, B also includes C", Priority = 2, Params = new object[] { "include_v5_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v5_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v101.3 - Include: A with NS includes B with no NS, which includes C with no NS", Priority = 2, Params = new object[] { "include_v4_a.xsd", 1, "ns-a:c-e2" })]
        [InlineData("include_v4_a.xsd", 1, "ns-a:c-e2")]
        //[Variation(Desc = "v101.2 - Include: A with no NS includes B with no NS", Priority = 0, Params = new object[] { "include_v3_a.xsd", 1, "e2" })]
        [InlineData("include_v3_a.xsd", 1, "e2")]
        //[Variation(Desc = "v101.1 - Include: A with NS includes B with no NS", Priority = 0, Params = new object[] { "include_v1_a.xsd", 1, "ns-a:e2" })]
        [InlineData("include_v1_a.xsd", 1, "ns-a:e2")]
        [Theory]
        public void v101(object param0, object param1, object param2)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename
            CError.Compare(sc.Count, param1, "AddCount"); //compare the count
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, param1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, param1, "Count");

            // Check that B's data is present in the NS for A
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals(param2.ToString()))
                    return;
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v102 - Include: A with NS includes B with a diff NS (INVALID)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v102()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema schema = new XmlSchema();
            sc.Add(null, TestData._XsdNoNs);
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            try
            {
                sc.Reprocess(schema);
                Assert.True(false);
            }
            catch (ArgumentException) { }
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            try
            {
                schema = sc.Add(null, Path.Combine(TestData._Root, "include_v2.xsd"));
            }
            catch (XmlSchemaException)
            {
                // no schema should be addded to the set.
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.IsCompiled, true, "IsCompiled");

                try
                {
                    sc.Reprocess(schema);
                    Assert.True(false);
                }
                catch (ArgumentException) { }
                CError.Compare(sc.IsCompiled, true, "ReprocessIsCompiled");
                CError.Compare(sc.Count, 1, "ReprocessCount");
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v103 - Include: A(ns-a) which includes B(ns-a) twice", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v103()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v8_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;
            CError.Compare(elem_count, 1, "ns-a:e2");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v104 - Include: A(ns-a) which includes B(No NS) twice", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v104()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v9_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e2");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v105 - Include: A,B,C all include each other, all with no ns and refer each others' types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v105()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v10_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("c-e2"))
                    elem_count++;
            CError.Compare(elem_count, 1, "c-e2");
            elem_count = 0;
            // Check that B's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("b-e1"))
                    elem_count++;
            CError.Compare(elem_count, 1, "b-e1");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v106 - Include: A,B,C all include each other, all with same ns and refer each others' types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v106()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v11_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            // Check that A's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e1"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e1");
            elem_count = 0;

            // Check B's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e2");
            elem_count = 0;

            // Check C's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e3"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e3");

            return;
        }

        //[Variation(Desc = "v112 - 20008213 SOM: SourceUri property on a chameleon include is not set", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v107()
        {
            bool succeeded = false;

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema a = ss.Add(null, Path.Combine(TestData._Root, "include_v12_a.xsd"));
            CError.Compare(ss.Count, 1, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Reprocess(a);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 1, "ReprocessCount");

            ss.Compile();
            CError.Compare(ss.Count, 1, "Count");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            foreach (XmlSchemaExternal s in a.Includes)
            {
                if (String.IsNullOrEmpty(s.Schema.SourceUri))
                {
                    CError.Compare(false, "Unexpected null uri");
                }
                else if (s.Schema.SourceUri.EndsWith("include_v12_b.xsd"))
                {
                    succeeded = true;
                }
            }
            Assert.True(succeeded);
        }

        /********  compile reprocess include **********/

        //[Variation(Desc = "v201.6 - Include: A(ns-a) include B(ns-a) which includes C(ns-a) ", Priority = 2, Params = new object[] { "include_v7_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v7_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v201.5 - Include: A with NS includes B and C with no NS", Priority = 2, Params = new object[] { "include_v6_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v6_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v201.4 - Include: A with NS includes B and C with no NS, B also includes C", Priority = 2, Params = new object[] { "include_v5_a.xsd", 1, "ns-a:e3" })]
        [InlineData("include_v5_a.xsd", 1, "ns-a:e3")]
        //[Variation(Desc = "v201.3 - Include: A with NS includes B with no NS, which includes C with no NS", Priority = 2, Params = new object[] { "include_v4_a.xsd", 1, "ns-a:c-e2" })]
        [InlineData("include_v4_a.xsd", 1, "ns-a:c-e2")]
        //[Variation(Desc = "v201.2 - Include: A with no NS includes B with no NS", Priority = 0, Params = new object[] { "include_v3_a.xsd", 1, "e2" })]
        [InlineData("include_v3_a.xsd", 1, "e2")]
        //[Variation(Desc = "v201.1 - Include: A with NS includes B with no NS", Priority = 0, Params = new object[] { "include_v1_a.xsd", 1, "ns-a:e2" })]
        [InlineData("include_v1_a.xsd", 1, "ns-a:e2")]
        [Theory]
        public void v201(object param0, object param1, object param2)
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, param0.ToString())); // param as filename
            CError.Compare(sc.Count, param1, "AddCount"); //compare the count
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, param1, "Count");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, param1, "ReprocessCount");

            // Check that B's data is present in the NS for A
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals(param2.ToString()))
                    return;
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v202 - Include: A with NS includes B with a diff NS (INVALID)", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v202()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            XmlSchema schema = new XmlSchema();
            sc.Add(null, TestData._XsdNoNs);
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            try
            {
                sc.Reprocess(schema);
                Assert.True(false);
            }
            catch (ArgumentException) { }
            CError.Compare(sc.IsCompiled, true, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            try
            {
                schema = sc.Add(null, Path.Combine(TestData._Root, "include_v2.xsd"));
            }
            catch (XmlSchemaException)
            {
                // no schema should be addded to the set.
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.IsCompiled, true, "IsCompiled");

                try
                {
                    sc.Reprocess(schema);
                    Assert.True(false);
                }
                catch (ArgumentException) { }
                CError.Compare(sc.IsCompiled, true, "ReprocessIsCompiled");
                CError.Compare(sc.Count, 1, "ReprocessCount");
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v203 - Include: A(ns-a) which includes B(ns-a) twice", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v203()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v8_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;
            CError.Compare(elem_count, 1, "ns-a:e2");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v204 - Include: A(ns-a) which includes B(No NS) twice", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v204()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v9_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e2");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v205 - Include: A,B,C all include each other, all with no ns and refer each others' types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v205()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v10_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(sc.Count, 1, "Count");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            // Check that C's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("c-e2"))
                    elem_count++;
            CError.Compare(elem_count, 1, "c-e2");
            elem_count = 0;
            // Check that B's data is present in A's NS
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("b-e1"))
                    elem_count++;
            CError.Compare(elem_count, 1, "b-e1");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v206 - Include: A,B,C all include each other, all with same ns and refer each others' types", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v206()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            int elem_count = 0;

            XmlSchema schema = sc.Add(null, Path.Combine(TestData._Root, "include_v11_a.xsd"));
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count");
            CError.Compare(sc.IsCompiled, true, "IsCompiled");

            sc.Reprocess(schema);
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(sc.Count, 1, "ReprocessCount");

            // Check that A's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e1"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e1");
            elem_count = 0;

            // Check B's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e2"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e2");
            elem_count = 0;

            // Check C's data
            foreach (object obj in schema.Elements.Names)
                if ((obj.ToString()).Equals("ns-a:e3"))
                    elem_count++;

            CError.Compare(elem_count, 1, "ns-a:e3");

            return;
        }

        //[Variation(Desc = "v212 - 20008213 SOM: SourceUri property on a chameleon include is not set", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v207()
        {
            bool succeeded = false;

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema a = ss.Add(null, Path.Combine(TestData._Root, "include_v12_a.xsd"));
            CError.Compare(ss.Count, 1, "AddCount");
            CError.Compare(ss.IsCompiled, false, "AddIsCompiled");

            ss.Compile();
            CError.Compare(ss.Count, 1, "Count");
            CError.Compare(ss.IsCompiled, true, "IsCompiled");

            ss.Reprocess(a);
            CError.Compare(ss.IsCompiled, false, "ReprocessIsCompiled");
            CError.Compare(ss.Count, 1, "ReprocessCount");

            foreach (XmlSchemaExternal s in a.Includes)
            {
                if (String.IsNullOrEmpty(s.Schema.SourceUri))
                {
                    CError.Compare(false, "Unexpected null uri");
                }
                else if (s.Schema.SourceUri.EndsWith("include_v12_b.xsd"))
                {
                    succeeded = true;
                }
            }
            Assert.True(succeeded);
        }
    }
}
