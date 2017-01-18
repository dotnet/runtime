// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Contains_Schema", Desc = "")]
    public class TC_SchemaSet_Contains_Schema : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Contains_Schema(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v1 - Contains with null")]
        public void v1()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Contains((XmlSchema)null);
            }
            catch (ArgumentNullException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v2 - Contains with not added schema")]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
#pragma warning disable 0618
            XmlSchemaCollection scl = new XmlSchemaCollection();
#pragma warning restore 0618

            XmlSchema Schema = scl.Add(null, TestData._XsdAuthor);

            Assert.Equal(sc.Contains(Schema), false);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v3 - Contains with existing schema, Remove it, Contains again", Priority = 0)]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
#pragma warning disable 0618
            XmlSchemaCollection scl = new XmlSchemaCollection();
#pragma warning restore 0618

            XmlSchema Schema = scl.Add(null, TestData._XsdAuthor);
            sc.Add(Schema);

            Assert.Equal(sc.Contains(Schema), true);

            sc.Remove(Schema);

            Assert.Equal(sc.Contains(Schema), false);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v4 - Contains for added with URL", Priority = 0)]
        public void v4()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = sc.Add(null, TestData._XsdAuthor);

            Assert.Equal(sc.Contains(Schema), true);

            return;
        }
    }
}