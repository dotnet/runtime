// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.Schema;
using System.Xml.Tests;
using Xunit;

namespace System.Xml.XmlSchemaTests
{
    //[TestCase(Name = "TC_SchemaSet_XmlResolver", Desc = "")]
    public class TC_SchemaSet_XmlResolver : TC_SchemaSetBase
    {
        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - Resolver=NULL, add with URL", Priority = 1)]
        [Fact]
        public void v1()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.ValidationEventHandler += new ValidationEventHandler((s, args) => { });
                sc.XmlResolver = null;
                XmlSchema Schema = sc.Add(null, Path.Combine(TestData._Root, "XmlResolver", "File", "simpledtd.xml"));
            }
            catch (Exception)
            {
                return;
            }

            Assert.Fail();
        }

        //[Variation(Desc = "v2 - Resolver=NULL, add schema which imports schema on internet", Priority = 1)]
        [Fact]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler((s, args) => { });
            sc.XmlResolver = null;
            sc.Add(null, Path.Combine(TestData._Root, "xmlresolver_v2.xsd"));
            CError.Compare(sc.Count, 1, "SchemaSet count");
        }

        //[Variation(Desc = "v3 - Resolver=Default, add schema which imports schema on internet", Priority = 1)]
        [Fact]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler((s, args) => { });
            sc.Add(null, Path.Combine(TestData._Root, "xmlresolver_v2.xsd"));
            CError.Compare(sc.Count, 1, "SchemaSet count");
        }

        //[Variation(Desc = "v4 - schema(Local)->schema(Local)", Priority = 1)]
        [Fact]
        public void v4()
        {
            using (new AllowDefaultResolverContext())
            {
                bool warningCallback = false;
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.ValidationEventHandler += new ValidationEventHandler((s, args) => warningCallback |= args.Severity == XmlSeverityType.Warning);
                sc.Add(null, Path.Combine(TestData._Root, "xmlresolver_v4.xsd"));
                CError.Compare(sc.Count, 2, "SchemaSet count");
                CError.Compare(warningCallback, false, "Warning thrown");
            }
        }

        //[Variation(Desc = "v5 - schema(Local)->schema(Local)->schema(Local)", Priority = 1)]
        [Fact]
        public void v5()
        {
            using (new AllowDefaultResolverContext())
            {
                bool warningCallback = false;
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.ValidationEventHandler += new ValidationEventHandler((s, args) => warningCallback |= args.Severity == XmlSeverityType.Warning);
                sc.Add(null, Path.Combine(TestData._Root, "xmlresolver_v5.xsd"));
                CError.Compare(sc.Count, 3, "SchemaSet count");
                CError.Compare(warningCallback, false, "Warning not thrown");
            }
        }

        //[Variation(Desc = "v6 - schema(Local)->schema(Local), but resolving external URI is not allowed", Priority = 1)]
        [Fact]
        public void v6()
        {
            bool warningCallback = false;
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler((s, args) => warningCallback |= args.Severity == XmlSeverityType.Warning);
            sc.Add(null, Path.Combine(TestData._Root, "xmlresolver_v4.xsd"));
            CError.Compare(sc.Count, 1, "SchemaSet count");
            CError.Compare(warningCallback, false, "Warning thrown");
        }
    }
}
