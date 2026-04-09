// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.XmlSchemaTests
{
    //[TestCase(Name = "TC_SchemaSet_Constructors", Desc = "", Priority = 0)]
    public class TC_SchemaSet_Constructors : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Constructors(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v1 - Default constructor", Priority = 1)]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v2 - XmlSchemaSet(XmlNameTable = 0)")]
        public void v2()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet(null);
            }
            catch (ArgumentNullException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.Fail();
        }

        [Fact]
        //[Variation(Desc = "v3 - XmlDataSourceResolver(XmlNameTable = valid) check back")]
        public void v3()
        {
            NameTable NT = new NameTable();
            XmlSchemaSet sc = new XmlSchemaSet(NT);
            Assert.Equal(sc.NameTable, NT);
            return;
        }
    }
}
