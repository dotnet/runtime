// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Collections;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Schemas_ns", Desc = "")]
    public class TC_SchemaSet_Schemas_ns : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Schemas_ns(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - Schemas with null on empty collection", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            ICollection Col = sc.Schemas("");

            CError.Compare(Col.Count, 0, "Count");
            CError.Compare(Col.IsSynchronized, false, "IsSynchronized");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - Schemas with null on non empty collection without schemas without ns", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor1", TestData._XsdNoNs);

            ICollection Col = sc.Schemas(null);

            CError.Compare(Col.Count, 0, "Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - Schemas with null on non empty collection with schemas without ns", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdNoNs);

            ICollection Col = sc.Schemas(null);

            CError.Compare(Col.Count, 1, "Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4 - Schemas on non empty collection with existing ns, all members of ICollection")]
        [InlineData()]
        [Theory]
        public void v4()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdNoNs);
            XmlSchema Schema2 = sc.Add(null, TestData._XsdAuthor);

            ICollection Col = sc.Schemas("xsdauthor");

            CError.Compare(Col.Count, 2, "Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5 - Schemas on non empty collection with existing ns, use in foreach")]
        [InlineData()]
        [Theory]
        public void v5()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdNoNs);
            XmlSchema Schema2 = sc.Add(null, TestData._XsdAuthor);

            ICollection Col = sc.Schemas("xsdauthor");

            CError.Compare(Col.Count, 2, "Count");
            XmlSchema[] Schemas = new XmlSchema[2];
            sc.CopyTo(Schemas, 0);

            int i = 0;
            foreach (XmlSchema Schema in Col)
            {
                CError.Compare(Schema, Schemas[i], "Count");
                i++;
            }
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - Schemas on non empty collection with null ns,call Schemas,Edit check all members of ICollection")]
        [InlineData()]
        [Theory]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema2 = sc.Add(null, TestData._XsdAuthor);

            sc.Remove(Schema2);
            ICollection Col = sc.Schemas("xsdauthor");

            CError.Compare(Col.Count, 0, "Count");

            foreach (XmlSchema Schema in Col)
            {
                XmlSchema a = Schema;
                _output.WriteLine("should never enter this loop");
                Assert.True(false);
            }
            return;
        }
    }
}