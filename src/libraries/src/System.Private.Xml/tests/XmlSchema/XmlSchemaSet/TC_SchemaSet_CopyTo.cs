// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Collections;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_CopyTo", Desc = "")]
    public class TC_SchemaSet_CopyTo : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_CopyTo(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v1 - CopyTo with array = null")]
        public void v1()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Add("xsdauthor", TestData._XsdAuthor);
                sc.CopyTo(null, 0);
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
        //[Variation(Desc = "v2 - ICollection.CopyTo with array = null")]
        public void v2()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Add("xsdauthor", TestData._XsdAuthor);
                ICollection Col = sc.Schemas();
                Col.CopyTo(null, 0);
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
        //[Variation(Desc = "v3 - ICollection.CopyTo with array smaller than source", Priority = 0)]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            try
            {
                sc.Add("xsdauthor", TestData._XsdAuthor);
                sc.Add(null, TestData._XsdNoNs);
                ICollection Col = sc.Schemas();
                XmlSchema[] array = new XmlSchema[1];
                Col.CopyTo(array, 0);
            }
            catch (ArgumentException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v4 - CopyTo with index < 0")]
        public void v4()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            try
            {
                sc.Add("xsdauthor", TestData._XsdAuthor);
                sc.Add(null, TestData._XsdNoNs);
                XmlSchema[] array = new XmlSchema[1];
                sc.CopyTo(array, -1);
            }
            catch (ArgumentException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v5 - ICollection.CopyTo with index < 0")]
        public void v5()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            try
            {
                sc.Add("xsdauthor", TestData._XsdAuthor);
                sc.Add(null, TestData._XsdNoNs);
                ICollection Col = sc.Schemas();
                XmlSchema[] array = new XmlSchema[1];
                Col.CopyTo(array, -1);
            }
            catch (ArgumentException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v6 - Filling last two positions of array", Priority = 0)]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add(null, TestData._XsdNoNs);

            XmlSchema[] array = new XmlSchema[10];
            sc.CopyTo(array, 8);

            Assert.Equal(array[8], Schema1);
            Assert.Equal(array[9], Schema2);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v7 - Copy all to array of the same size", Priority = 0)]
        public void v7()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add(null, TestData._XsdNoNs);

            XmlSchema[] array = new XmlSchema[2];
            sc.CopyTo(array, 0);

            Assert.Equal(array[0], Schema1);
            Assert.Equal(array[1], Schema2);

            return;
        }

        [Fact]
        //[Variation(Desc = "v8 - 378346: CopyTo throws correct exception for index < 0 but incorrect exception for index > maxLength of array.", Priority = 0)]
        public void v8()
        {
            try
            {
                XmlSchemaSet ss = new XmlSchemaSet();
                ss.CopyTo(new XmlSchema[2], 3);
            }
            catch (ArgumentOutOfRangeException e)
            {
                _output.WriteLine(e.Message);
                return;
            }

            Assert.True(false);
        }
    }
}