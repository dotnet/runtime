// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_GlobalTypes", Desc = "")]
    public class TC_SchemaSet_GlobalTypes : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_GlobalTypes(ITestOutputHelper output)
        {
            _output = output;
        }


        public XmlSchema GetSchema(string ns, string type1, string type2)
        {
            string xsd = String.Empty;
            if (ns.Equals(String.Empty))
                xsd = "<schema xmlns='http://www.w3.org/2001/XMLSchema'><complexType name='" + type1 + "'><sequence><element name='local'/></sequence></complexType><simpleType name='" + type2 + "'><restriction base='int'/></simpleType></schema>";
            else
                xsd = "<schema xmlns='http://www.w3.org/2001/XMLSchema' targetNamespace='" + ns + "'><complexType name='" + type1 + "'><sequence><element name='local'/></sequence></complexType><simpleType name='" + type2 + "'><restriction base='int'/></simpleType></schema>";

            XmlSchema schema = XmlSchema.Read(new StringReader(xsd), null);
            return schema;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - GlobalTypes on empty collection")]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchemaObjectTable table = sc.GlobalTypes;

            CError.Compare(table == null, false, "Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        // params is a pair of the following info: (namaespace, type1 type2) two schemas are made from this info
        //[Variation(Desc = "v2.1 - GlobalTypes with set with two schemas, both without NS", Params = new object[] { "", "t1", "t2", "", "t3", "t4" })]
        [InlineData("", "t1", "t2", "", "t3", "t4")]
        //[Variation(Desc = "v2.2 - GlobalTypes with set with two schemas, one without NS one with NS", Params = new object[] { "a", "t1", "t2", "", "t3", "t4" })]
        [InlineData("a", "t1", "t2", "", "t3", "t4")]
        //[Variation(Desc = "v2.2 - GlobalTypes with set with two schemas, both with NS", Params = new object[] { "a", "t1", "t2", "b", "t3", "t4" })]
        [InlineData("a", "t1", "t2", "b", "t3", "t4")]
        [Theory]
        public void v2(object param0, object param1, object param2, object param3, object param4, object param5)
        {
            string ns1 = param0.ToString();
            string ns2 = param3.ToString();

            string type1 = param1.ToString();
            string type2 = param2.ToString();
            string type3 = param4.ToString();
            string type4 = param5.ToString();

            XmlSchema s1 = GetSchema(ns1, type1, type2);
            XmlSchema s2 = GetSchema(ns2, type3, type4);

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(s1);
            CError.Compare(ss.GlobalTypes.Count, 0, "Types Count after add");
            ss.Compile();
            ss.Add(s2);
            CError.Compare(ss.GlobalTypes.Count, 3, "Types Count after add/compile"); //+1 for anyType
            ss.Compile();

            //Verify
            CError.Compare(ss.GlobalTypes.Count, 5, "Types Count after add/compile/add/compile"); //+1 for anyType
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type2, ns1)), true, "Contains2");
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type3, ns2)), true, "Contains3");
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type4, ns2)), true, "Contains4");

            //Now reprocess one schema and check
            ss.Reprocess(s1);
            CError.Compare(ss.GlobalTypes.Count, 3, "Types Count after repr"); //+1 for anyType
            ss.Compile();
            CError.Compare(ss.GlobalTypes.Count, 5, "Types Count after repr/comp"); //+1 for anyType

            //Now Remove one schema and check
            ss.Remove(s1);
            CError.Compare(ss.GlobalTypes.Count, 3, "Types Count after remove");
            ss.Compile();
            CError.Compare(ss.GlobalTypes.Count, 3, "Types Count after remove/comp");

            return;
        }

        // params is a pair of the following info: (namaespace, type1 type2)*, doCompile?
        //[Variation(Desc = "v3.1 - GlobalTypes with a set having schema (nons) to another set with schema(nons)", Params = new object[] { "", "t1", "t2", "", "t3", "t4", true })]
        [InlineData("", "t1", "t2", "", "t3", "t4", true)]
        //[Variation(Desc = "v3.2 - GlobalTypes with a set having schema (ns) to another set with schema(nons)", Params = new object[] { "a", "t1", "t2", "", "t3", "t4", true })]
        [InlineData("a", "t1", "t2", "", "t3", "t4", true)]
        //[Variation(Desc = "v3.3 - GlobalTypes with a set having schema (nons) to another set with schema(ns)", Params = new object[] { "", "t1", "t2", "a", "t3", "t4", true })]
        [InlineData("", "t1", "t2", "a", "t3", "t4", true)]
        //[Variation(Desc = "v3.4 - GlobalTypes with a set having schema (ns) to another set with schema(ns)", Params = new object[] { "a", "t1", "t2", "b", "t3", "t4", true })]
        [InlineData("a", "t1", "t2", "b", "t3", "t4", true)]
        //[Variation(Desc = "v3.5 - GlobalTypes with a set having schema (nons) to another set with schema(nons), no compile", Params = new object[] { "", "t1", "t2", "", "t3", "t4", false })]
        [InlineData("", "t1", "t2", "", "t3", "t4", false)]
        //[Variation(Desc = "v3.6 - GlobalTypes with a set having schema (ns) to another set with schema(nons), no compile", Params = new object[] { "a", "t1", "t2", "", "t3", "t4", false })]
        [InlineData("a", "t1", "t2", "", "t3", "t4", false)]
        //[Variation(Desc = "v3.7 - GlobalTypes with a set having schema (nons) to another set with schema(ns), no compile", Params = new object[] { "", "t1", "t2", "a", "t3", "t4", false })]
        [InlineData("", "t1", "t2", "a", "t3", "t4", false)]
        //[Variation(Desc = "v3.8 - GlobalTypes with a set having schema (ns) to another set with schema(ns), no compile", Params = new object[] { "a", "t1", "t2", "b", "t3", "t4", false })]
        [InlineData("a", "t1", "t2", "b", "t3", "t4", false)]
        [Theory]
        public void v3(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
        {
            string ns1 = param0.ToString();
            string ns2 = param3.ToString();
            string type1 = param1.ToString();
            string type2 = param2.ToString();
            string type3 = param4.ToString();
            string type4 = param5.ToString();
            bool doCompile = (bool)param6;

            XmlSchema s1 = GetSchema(ns1, type1, type2);
            XmlSchema s2 = GetSchema(ns2, type3, type4);

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
            CError.Compare(ss1.GlobalTypes.Count, 5, "Types Count after add/comp"); //+1 for anyType
            CError.Compare(ss1.GlobalTypes.Contains(new XmlQualifiedName(type1, ns1)), true, "Contains1");
            CError.Compare(ss1.GlobalTypes.Contains(new XmlQualifiedName(type2, ns1)), true, "Contains2");
            CError.Compare(ss1.GlobalTypes.Contains(new XmlQualifiedName(type3, ns2)), true, "Contains3");
            CError.Compare(ss1.GlobalTypes.Contains(new XmlQualifiedName(type4, ns2)), true, "Contains4");

            //Now reprocess one schema and check
            ss1.Reprocess(s1);
            CError.Compare(ss1.GlobalTypes.Count, 3, "Types Count repr"); //+1 for anyType
            ss1.Compile();
            CError.Compare(ss1.GlobalTypes.Count, 5, "Types Count repr/comp"); //+1 for anyType

            //Now Remove one schema and check
            ss1.Remove(s1);
            CError.Compare(ss1.GlobalTypes.Count, 3, "Types Count after remove");
            ss1.Compile();
            CError.Compare(ss1.GlobalTypes.Count, 3, "Types Count after rem/comp");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4.1 - GlobalTypes with set having one which imports another, remove one", Priority = 1, Params = new object[] { "import_v1_a.xsd", "ns-a", "ct-A", "", "ct-B" })]
        [InlineData("import_v1_a.xsd", "ns-a", "ct-A", "", "ct-B")]
        //[Variation(Desc = "v4.2 - GlobalTypes with set having one which imports another, remove one", Priority = 1, Params = new object[] { "import_v2_a.xsd", "ns-a", "ct-A", "ns-b", "ct-B" })]
        [InlineData("import_v2_a.xsd", "ns-a", "ct-A", "ns-b", "ct-B")]
        [Theory]
        public void v4(object param0, object param1, object param2, object param3, object param4)
        {
            string uri1 = param0.ToString();

            string ns1 = param1.ToString();
            string type1 = param2.ToString();
            string ns2 = param3.ToString();
            string type2 = param4.ToString();

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema schema1 = ss.Add(null, Path.Combine(TestData._Root, uri1));
            ss.Compile();
            CError.Compare(ss.GlobalTypes.Count, 3, "Types Count"); //+1 for anyType
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type2, ns2)), true, "Contains2");

            //get the SOM for the imported schema
            foreach (XmlSchema s in ss.Schemas(ns2))
            {
                ss.Remove(s);
            }

            ss.Compile();
            CError.Compare(ss.GlobalTypes.Count, 2, "Types Count after Remove"); //+1 for anyType
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type2, ns2)), false, "Contains2");

            return;
        }

        //[Variation(Desc = "v5.1 - GlobalTypes with set having one which imports another, then removerecursive", Priority = 1, Params = new object[] { "import_v1_a.xsd", "ns-a", "ct-A", "", "ct-B" })]
        [InlineData("import_v1_a.xsd", "ns-a", "ct-A", "", "ct-B")]
        //[Variation(Desc = "v5.2 - GlobalTypes with set having one which imports another, then removerecursive", Priority = 1, Params = new object[] { "import_v2_a.xsd", "ns-a", "ct-A", "ns-b", "ct-B" })]
        [InlineData("import_v2_a.xsd", "ns-a", "ct-A", "ns-b", "ct-B")]
        [Theory]
        public void v5(object param0, object param1, object param2, object param3, object param4)
        {
            string uri1 = param0.ToString();

            string ns1 = param1.ToString();
            string type1 = param2.ToString();
            string ns2 = param3.ToString();
            string type2 = param4.ToString();

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "xsdauthor.xsd"));
            XmlSchema schema1 = ss.Add(null, Path.Combine(TestData._Root, uri1));
            ss.Compile();
            CError.Compare(ss.GlobalTypes.Count, 3, "Types Count"); //+1 for anyType
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type2, ns2)), true, "Contains2");

            ss.RemoveRecursive(schema1); // should not need to compile for RemoveRecursive to take effect
            CError.Compare(ss.GlobalTypes.Count, 1, "Types Count"); //+1 for anyType
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type1, ns1)), false, "Contains1");
            CError.Compare(ss.GlobalTypes.Contains(new XmlQualifiedName(type2, ns2)), false, "Contains2");

            return;
        }

        //-----------------------------------------------------------------------------------
        //REGRESSIONS
        //[Variation(Desc = "v100 - XmlSchemaSet: Components are not added to the global tabels if it is already compiled", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v100()
        {
            try
            {
                // anytype t1 t2
                XmlSchema schema1 = XmlSchema.Read(new StringReader("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='a'><xs:element name='e1' type='xs:anyType'/><xs:complexType name='t1'/><xs:complexType name='t2'/></xs:schema>"), null);
                // anytype t3 t4
                XmlSchema schema2 = XmlSchema.Read(new StringReader("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' ><xs:element name='e1' type='xs:anyType'/><xs:complexType name='t3'/><xs:complexType name='t4'/></xs:schema>"), null);
                XmlSchemaSet ss1 = new XmlSchemaSet();
                XmlSchemaSet ss2 = new XmlSchemaSet();

                ss1.Add(schema1);
                ss2.Add(schema2);
                ss2.Compile();
                ss1.Add(ss2);
                ss1.Compile();
                CError.Compare(ss1.GlobalTypes.Count, 5, "Count");
                CError.Compare(ss1.GlobalTypes.Contains(new XmlQualifiedName("t1", "a")), true, "Contains");
                CError.Compare(ss1.GlobalTypes.Contains(new XmlQualifiedName("t2", "a")), true, "Contains");
            }
            catch (Exception e)
            {
                _output.WriteLine(e.Message);
                Assert.True(false);
            }

            return;
        }
    }
}
