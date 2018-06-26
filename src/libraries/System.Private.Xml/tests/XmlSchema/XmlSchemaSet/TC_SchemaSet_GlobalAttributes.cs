// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_GlobalAttributes", Desc = "")]
    public class TC_SchemaSet_GlobalAttributes : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_GlobalAttributes(ITestOutputHelper output)
        {
            _output = output;
        }


        public XmlSchema GetSchema(string ns, string a1, string a2)
        {
            string xsd = string.Empty;
            if (ns.Equals(string.Empty))
                xsd = "<schema xmlns='http://www.w3.org/2001/XMLSchema'><attribute name='" + a1 + "'/><attribute name='" + a2 + "'/></schema>";
            else
                xsd = "<schema xmlns='http://www.w3.org/2001/XMLSchema' targetNamespace='" + ns + "'><attribute name='" + a1 + "'/><attribute name='" + a2 + "'/></schema>";

            XmlSchema schema = XmlSchema.Read(new StringReader(xsd), null);
            return schema;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - GlobalAttributes on empty collection", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchemaObjectTable table = sc.GlobalAttributes;

            CError.Compare(table == null, false, "Count");

            return;
        }

        // params is a pair of the following info: (namaespace, a2 a2) two schemas are made from this info
        //[Variation(Desc = "v2.1 - GlobalAttributes with set with two schemas, both without NS", Params = new object[] { "", "a1", "a2", "", "a3", "a4" })]
        [InlineData("", "a1", "a2", "", "a3", "a4")]
        //[Variation(Desc = "v2.2 - GlobalAttributes with set with two schemas, one without NS one with NS", Params = new object[] { "a", "a1", "a2", "", "a3", "a4" })]
        [InlineData("a", "a1", "a2", "", "a3", "a4")]
        //[Variation(Desc = "v2.2 - GlobalAttributes with set with two schemas, both with NS", Params = new object[] { "a", "a1", "a2", "b", "a3", "a4" })]
        [InlineData("a", "a1", "a2", "b", "a3", "a4")]
        [Theory]
        public void v2(object param0, object param1, object param2, object param3, object param4, object param5)
        {
            string ns1 = param0.ToString();
            string ns2 = param3.ToString();

            string a1 = param1.ToString();
            string a2 = param2.ToString();
            string a3 = param4.ToString();
            string a4 = param5.ToString();

            XmlSchema s1 = GetSchema(ns1, a1, a2);
            XmlSchema s2 = GetSchema(ns2, a3, a4);

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(s1);
            ss.Compile();
            ss.Add(s2);
            CError.Compare(ss.GlobalAttributes.Count, 2, "Elements Countafter add"); //+1 for anyType
            ss.Compile();

            //Verify
            CError.Compare(ss.GlobalAttributes.Count, 4, "Elements Count after add/compile");
            CError.Compare(ss.GlobalAttributes.Contains(new XmlQualifiedName(a1, ns1)), true, "Contains1");
            CError.Compare(ss.GlobalAttributes.Contains(new XmlQualifiedName(a2, ns1)), true, "Contains2");
            CError.Compare(ss.GlobalAttributes.Contains(new XmlQualifiedName(a3, ns2)), true, "Contains3");
            CError.Compare(ss.GlobalAttributes.Contains(new XmlQualifiedName(a4, ns2)), true, "Contains4");

            //Now reprocess one schema and check
            ss.Reprocess(s1);
            ss.Compile();
            CError.Compare(ss.GlobalAttributes.Count, 4, "Elements Count after reprocess");

            //Now Remove one schema and check
            ss.Remove(s1);
            CError.Compare(ss.GlobalAttributes.Count, 2, "Elements Count after remove no compile");
            ss.Compile();
            CError.Compare(ss.GlobalAttributes.Count, 2, "Elements Count adter remove and compile");

            return;
        }

        // params is a pair of the following info: (namaespace, a1 a2)*, doCompile?
        //[Variation(Desc = "v3.1 - GlobalAttributes with a set having schema (nons) to another set with schema(nons)", Params = new object[] { "", "a1", "a2", "", "a3", "a4", true })]
        [InlineData("", "a1", "a2", "", "a3", "a4", true)]
        //[Variation(Desc = "v3.2 - GlobalAttributes with a set having schema (ns) to another set with schema(nons)", Params = new object[] { "a", "a1", "a2", "", "a3", "a4", true })]
        [InlineData("a", "a1", "a2", "", "a3", "a4", true)]
        //[Variation(Desc = "v3.3 - GlobalAttributes with a set having schema (nons) to another set with schema(ns)", Params = new object[] { "", "a1", "a2", "a", "a3", "a4", true })]
        [InlineData("", "a1", "a2", "a", "a3", "a4", true)]
        //[Variation(Desc = "v3.4 - GlobalAttributes with a set having schema (ns) to another set with schema(ns)", Params = new object[] { "a", "a1", "a2", "b", "a3", "a4", true })]
        [InlineData("a", "a1", "a2", "b", "a3", "a4", true)]
        //[Variation(Desc = "v3.5 - GlobalAttributes with a set having schema (nons) to another set with schema(nons), no compile", Params = new object[] { "", "a1", "a2", "", "a3", "a4", false })]
        [InlineData("", "a1", "a2", "", "a3", "a4", false)]
        //[Variation(Desc = "v3.6 - GlobalAttributes with a set having schema (ns) to another set with schema(nons), no compile", Params = new object[] { "a", "a1", "a2", "", "a3", "a4", false })]
        [InlineData("a", "a1", "a2", "", "a3", "a4", false)]
        //[Variation(Desc = "v3.7 - GlobalAttributes with a set having schema (nons) to another set with schema(ns), no compile", Params = new object[] { "", "a1", "a2", "a", "a3", "a4", false })]
        [InlineData("", "a1", "a2", "a", "a3", "a4", false)]
        //[Variation(Desc = "v3.8 - GlobalAttributes with a set having schema (ns) to another set with schema(ns), no compile", Params = new object[] { "a", "a1", "a2", "b", "a3", "a4", false })]
        [InlineData("a", "a1", "a2", "b", "a3", "a4", false)]
        [Theory]
        public void v3(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
        {
            string ns1 = param0.ToString();
            string ns2 = param3.ToString();
            string a1 = param1.ToString();
            string a2 = param2.ToString();
            string a3 = param4.ToString();
            string a4 = param5.ToString();
            bool doCompile = (bool)param6;

            XmlSchema s1 = GetSchema(ns1, a1, a2);
            XmlSchema s2 = GetSchema(ns2, a3, a4);

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
            CError.Compare(ss1.GlobalAttributes.Count, 4, "Types Count after add");
            CError.Compare(ss1.GlobalAttributes.Contains(new XmlQualifiedName(a1, ns1)), true, "Contains1");
            CError.Compare(ss1.GlobalAttributes.Contains(new XmlQualifiedName(a2, ns1)), true, "Contains2");
            CError.Compare(ss1.GlobalAttributes.Contains(new XmlQualifiedName(a3, ns2)), true, "Contains3");
            CError.Compare(ss1.GlobalAttributes.Contains(new XmlQualifiedName(a4, ns2)), true, "Contains4");

            //Now reprocess one schema and check
            ss1.Reprocess(s1);
            ss1.Compile();
            CError.Compare(ss1.GlobalAttributes.Count, 4, "Types Count after reprocess");

            //Now Remove one schema and check
            ss1.Remove(s1);
            CError.Compare(ss1.GlobalAttributes.Count, 2, "Types Count after repr/remove"); // count should still be 4
            ss1.Compile();
            CError.Compare(ss1.GlobalAttributes.Count, 2, "Types Count after repr/remove/comp"); // count should NOW still be 2

            return;
        }
    }
}