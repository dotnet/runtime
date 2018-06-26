// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Contains_ns", Desc = "")]
    public class TC_SchemaSet_Contains_ns : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Contains_ns(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v1 - Contains with null")]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            Assert.Equal(sc.Contains((string)null), false);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v2 - Contains with non existing ns", Priority = 0)]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add("xsdauthor", TestData._XsdAuthor);
            Assert.Equal(sc.Contains("test"), false);
            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v3 - Contains with existing schema, Remove it, Contains again")]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = sc.Add("xsdauthor", TestData._XsdAuthor);
            Assert.Equal(sc.Contains("xsdauthor"), true);

            sc.Remove(Schema);

            Assert.Equal(sc.Contains("xsdauthor"), false);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v4 - Contains for 2 existing schemas, Remove one, Contains again", Priority = 0)]
        public void v4()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add("xsdauthor", TestData._XsdNoNs);

            Assert.Equal(sc.Contains("xsdauthor"), true);

            sc.Remove(Schema1);

            Assert.Equal(sc.Contains("xsdauthor"), true);

            return;
        }
    }
}