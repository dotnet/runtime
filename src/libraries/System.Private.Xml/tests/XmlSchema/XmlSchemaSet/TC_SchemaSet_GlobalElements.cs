// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_GlobalElements", Desc = "")]
    public class TC_SchemaSet_GlobalElements : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_GlobalElements(ITestOutputHelper output)
        {
            _output = output;
        }


        public XmlSchema GetSchema(string ns, string e1, string e2)
        {
            string xsd = string.Empty;
            if (ns.Equals(string.Empty))
                xsd = "<schema xmlns='http://www.w3.org/2001/XMLSchema'><element name='" + e1 + "'/><element name='" + e2 + "'/></schema>";
            else
                xsd = "<schema xmlns='http://www.w3.org/2001/XMLSchema' targetNamespace='" + ns + "'><element name='" + e1 + "'/><element name='" + e2 + "'/></schema>";

            XmlSchema schema = XmlSchema.Read(new StringReader(xsd), null);
            return schema;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - GlobalElements on empty collection", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchemaObjectTable table = sc.GlobalElements;

            CError.Compare(table == null, false, "Count");

            return;
        }

        // params is a pair of the following info: (namaespace, e2 e2) two schemas are made from this info
        //[Variation(Desc = "v2.1 - GlobalElements with set with two schemas, both without NS", Params = new object[] { "", "e1", "e2", "", "e3", "e4" })]
        [InlineData("", "e1", "e2", "", "e3", "e4")]
        //[Variation(Desc = "v2.2 - GlobalElements with set with two schemas, one without NS one with NS", Params = new object[] { "a", "e1", "e2", "", "e3", "e4" })]
        [InlineData("a", "e1", "e2", "", "e3", "e4")]
        //[Variation(Desc = "v2.2 - GlobalElements with set with two schemas, both with NS", Params = new object[] { "a", "e1", "e2", "b", "e3", "e4" })]
        [InlineData("a", "e1", "e2", "b", "e3", "e4")]
        [Theory]
        public void v2(object param0, object param1, object param2, object param3, object param4, object param5)
        {
            string ns1 = param0.ToString();
            string ns2 = param3.ToString();

            string e1 = param1.ToString();
            string e2 = param2.ToString();
            string e3 = param4.ToString();
            string e4 = param5.ToString();

            XmlSchema s1 = GetSchema(ns1, e1, e2);
            XmlSchema s2 = GetSchema(ns2, e3, e4);

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(s1);
            ss.Compile();
            ss.Add(s2);
            CError.Compare(ss.GlobalElements.Count, 2, "Elements Count after add"); //+1 for anyType
            ss.Compile();

            //Verify
            CError.Compare(ss.GlobalElements.Count, 4, "Elements Count after add/compile");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e2, ns1)), true, "Contains2");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e3, ns2)), true, "Contains3");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e4, ns2)), true, "Contains4");

            //Now reprocess one schema and check
            ss.Reprocess(s1);
            ss.Compile();
            CError.Compare(ss.GlobalElements.Count, 4, "Elements Count after reprocess/compile");

            //Now Remove one schema and check
            ss.Remove(s1);
            CError.Compare(ss.GlobalElements.Count, 2, "Elements Count after remove no compile");
            ss.Compile();
            CError.Compare(ss.GlobalElements.Count, 2, "Elements Count adter remove and compile");

            return;
        }

        // params is a pair of the following info: (namaespace, e1 e2)*, doCompile?
        //[Variation(Desc = "v3.1 - GlobalElements with a set having schema (nons) to another set with schema(nons)", Params = new object[] { "", "e1", "e2", "", "e3", "e4", true })]
        [InlineData("", "e1", "e2", "", "e3", "e4", true)]
        //[Variation(Desc = "v3.2 - GlobalElements with a set having schema (ns) to another set with schema(nons)", Params = new object[] { "a", "e1", "e2", "", "e3", "e4", true })]
        [InlineData("a", "e1", "e2", "", "e3", "e4", true)]
        //[Variation(Desc = "v3.3 - GlobalElements with a set having schema (nons) to another set with schema(ns)", Params = new object[] { "", "e1", "e2", "a", "e3", "e4", true })]
        [InlineData("", "e1", "e2", "a", "e3", "e4", true)]
        //[Variation(Desc = "v3.4 - GlobalElements with a set having schema (ns) to another set with schema(ns)", Params = new object[] { "a", "e1", "e2", "b", "e3", "e4", true })]
        [InlineData("a", "e1", "e2", "b", "e3", "e4", true)]
        //[Variation(Desc = "v3.5 - GlobalElements with a set having schema (nons) to another set with schema(nons), no compile", Params = new object[] { "", "e1", "e2", "", "e3", "e4", false })]
        [InlineData("", "e1", "e2", "", "e3", "e4", false)]
        //[Variation(Desc = "v3.6 - GlobalElements with a set having schema (ns) to another set with schema(nons), no compile", Params = new object[] { "a", "e1", "e2", "", "e3", "e4", false })]
        [InlineData("a", "e1", "e2", "", "e3", "e4", false)]
        //[Variation(Desc = "v3.7 - GlobalElements with a set having schema (nons) to another set with schema(ns), no compile", Params = new object[] { "", "e1", "e2", "a", "e3", "e4", false })]
        [InlineData("", "e1", "e2", "a", "e3", "e4", false)]
        //[Variation(Desc = "v3.8 - GlobalElements with a set having schema (ns) to another set with schema(ns), no compile", Params = new object[] { "a", "e1", "e2", "b", "e3", "e4", false })]
        [InlineData("a", "e1", "e2", "b", "e3", "e4", false)]
        [Theory]
        public void v3(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
        {
            string ns1 = param0.ToString();
            string ns2 = param3.ToString();
            string e1 = param1.ToString();
            string e2 = param2.ToString();
            string e3 = param4.ToString();
            string e4 = param5.ToString();
            bool doCompile = (bool)param6;

            XmlSchema s1 = GetSchema(ns1, e1, e2);
            XmlSchema s2 = GetSchema(ns2, e3, e4);

            XmlSchemaSet ss1 = new XmlSchemaSet();
            XmlSchemaSet ss2 = new XmlSchemaSet();
            ss1.Add(s1);
            ss1.Compile();

            ss2.Add(s2);

            if (doCompile)
                ss2.Compile();

            // add one schemaset to another
            ss1.Add(ss2);

            if (!doCompile)
                ss1.Compile();
            //Verify
            CError.Compare(ss1.GlobalElements.Count, 4, "Types Count after add, compile,add");
            CError.Compare(ss1.GlobalElements.Contains(new XmlQualifiedName(e1, ns1)), true, "Contains1");
            CError.Compare(ss1.GlobalElements.Contains(new XmlQualifiedName(e2, ns1)), true, "Contains2");
            CError.Compare(ss1.GlobalElements.Contains(new XmlQualifiedName(e3, ns2)), true, "Contains3");
            CError.Compare(ss1.GlobalElements.Contains(new XmlQualifiedName(e4, ns2)), true, "Contains4");

            //Now reprocess one schema and check
            ss1.Reprocess(s1);
            ss1.Compile();
            CError.Compare(ss1.GlobalElements.Count, 4, "Types Count after reprocess/compile");

            //Now Remove one schema and check
            ss1.Remove(s1);
            CError.Compare(ss1.GlobalElements.Count, 2, "Types Count after remove"); // count should still be 4
            ss1.Compile();
            CError.Compare(ss1.GlobalElements.Count, 2, "Types Count after remove/comp"); // count should NOW still be 2

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4.1 - GlobalElements with set having one which imports another, remove one", Priority = 1, Params = new object[] { "import_v1_a.xsd", "ns-a", "e1", "", "e2" })]
        [InlineData("import_v1_a.xsd", "ns-a", "e1", "", "e2")]
        //[Variation(Desc = "v4.2 - GlobalElements with set having one which imports another, remove one", Priority = 1, Params = new object[] { "import_v2_a.xsd", "ns-a", "e1", "ns-b", "e2" })]
        [InlineData("import_v2_a.xsd", "ns-a", "e1", "ns-b", "e2")]
        [Theory]
        public void v4(object param0, object param1, object param2, object param3, object param4)
        {
            string uri1 = param0.ToString();

            string ns1 = param1.ToString();
            string e1 = param2.ToString();
            string ns2 = param3.ToString();
            string e2 = param4.ToString();

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema schema1 = ss.Add(null, Path.Combine(TestData._Root, uri1));
            ss.Compile();
            CError.Compare(ss.GlobalElements.Count, 3, "Types Count after add"); // +1 for root in ns-a
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e2, ns2)), true, "Contains2");

            //get the SOM for the imported schema
            foreach (XmlSchema s in ss.Schemas(ns2))
            {
                ss.Remove(s);
            }

            ss.Compile();
            CError.Compare(ss.GlobalElements.Count, 2, "Types Count after Remove");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e2, ns2)), false, "Contains2");

            return;
        }

        //[Variation(Desc = "v5.1 - GlobalElements with set having one which imports another, then removerecursive", Priority = 1, Params = new object[] { "import_v1_a.xsd", "ns-a", "e1", "", "e2" })]
        [InlineData("import_v1_a.xsd", "ns-a", "e1", "", "e2")]
        //[Variation(Desc = "v5.2 - GlobalElements with set having one which imports another, then removerecursive", Priority = 1, Params = new object[] { "import_v2_a.xsd", "ns-a", "e1", "ns-b", "e2" })]
        [InlineData("import_v2_a.xsd", "ns-a", "e1", "ns-b", "e2")]
        [Theory]
        public void v5(object param0, object param1, object param2, object param3, object param4)
        {
            string uri1 = param0.ToString();

            string ns1 = param1.ToString();
            string e1 = param2.ToString();
            string ns2 = param3.ToString();
            string e2 = param4.ToString();

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "xsdauthor.xsd"));
            XmlSchema schema1 = ss.Add(null, Path.Combine(TestData._Root, uri1));
            ss.Compile();
            CError.Compare(ss.GlobalElements.Count, 4, "Types Count");  // +1 for root in ns-a and xsdauthor
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e2, ns2)), true, "Contains2");

            ss.RemoveRecursive(schema1); // should not need to compile for RemoveRecursive to take effect
            CError.Compare(ss.GlobalElements.Count, 1, "Types Count");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e1, ns1)), false, "Contains1");
            CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e2, ns2)), false, "Contains2");

            return;
        }

        //[Variation(Desc = "v6 - GlobalElements with set with two schemas, second schema will fail to compile, no elements from it should be added", Params = new object[] { "", "e1", "e2" })]
        [InlineData("", "e1", "e2")]
        [Theory]
        public void v6(object param0, object param1, object param2)
        {
            string ns1 = param0.ToString();
            string e1 = param1.ToString();
            string e2 = param2.ToString();
            XmlSchema s1 = GetSchema(ns1, e1, e2);
            XmlSchema s2 = XmlSchema.Read(new StreamReader(new FileStream(Path.Combine(TestData._Root, "invalid.xsd"), FileMode.Open, FileAccess.Read)), null);

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(s1);
            ss.Compile();
            ss.Add(s2);
            CError.Compare(ss.GlobalElements.Count, 2, "Elements Count"); //+1 for anyType

            try
            {
                ss.Compile();
            }
            catch (XmlSchemaException)
            {
                //Verify
                CError.Compare(ss.GlobalElements.Count, 2, "Elements Count after compile");
                CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e1, ns1)), true, "Contains1");
                CError.Compare(ss.GlobalElements.Contains(new XmlQualifiedName(e2, ns1)), true, "Contains2");
                return;
            }
            Assert.True(false);
        }
    }
}
