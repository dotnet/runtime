// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Net;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_XmlResolver", Desc = "")]
    public class TC_SchemaSet_XmlResolver
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_XmlResolver(ITestOutputHelper output)
        {
            _output = output;
        }


        //todo: use rootpath
        public bool bWarningCallback;

        public bool bErrorCallback;

        public void Initialize()
        {
            bWarningCallback = bErrorCallback = false;
        }

        //hook up validaton callback
        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                _output.WriteLine("WARNING: ");
                bWarningCallback = true;
            }
            else if (args.Severity == XmlSeverityType.Error)
            {
                _output.WriteLine("ERROR: ");
                bErrorCallback = true;
            }

            _output.WriteLine(args.Message); // Print the error to the screen.
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - Resolver=NULL, add with URL", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
                sc.XmlResolver = null;
                XmlSchema Schema = sc.Add(null, Path.Combine(TestData._Root, "XmlResolver", "File", "simpledtd.xml"));
            }
            catch (Exception)
            {
                return;
            }

            Assert.True(false);
        }

        //[Variation(Desc = "v2 - Resolver=NULL, add schema which imports schema on internet", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            sc.XmlResolver = null;
            sc.Add(null, Path.Combine(TestData._Root, "xmlresolver_v2.xsd"));
            CError.Compare(sc.Count, 1, "SchemaSet count");
            return;
        }

        //[Variation(Desc = "v3 - Resolver=Default, add schema which imports schema on internet", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            sc.Add(null, Path.Combine(TestData._Root, "xmlresolver_v2.xsd"));
            CError.Compare(sc.Count, 1, "SchemaSet count");
            return;
        }

        // TODO: Fix
        ////[Variation(Desc = "v4 - schema(Internet)->schema(Local)", Priority = 1)]
        //[InlineData()]
        //public int v4()
        //{
        //    Initialize();
        //    XmlSchemaSet sc = new XmlSchemaSet();
        //    sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
        //    sc.Add(null, FilePathUtil.GetHttpTestDataPath() + @"/XmlSchemaCollection/xmlresolver_v4.xsd");
        //    CError.Compare(sc.Count, 2, "SchemaSet count");
        //    CError.Compare(bWarningCallback, false, "Warning thrown");
        //    return;
        //}
        //todo: add these tests for local
        ////[Variation(Desc = "v5 - schema(Intranet)->schema(Internet)->schema(Intranet)", Priority = 1)]
        //[InlineData()]
        //public int v5()
        //{
        //    Initialize();
        //    XmlSchemaSet sc = new XmlSchemaSet();
        //    sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
        //    sc.Add(null, TestData._Root + "xmlresolver_v5.xsd");
        //    CError.Compare(sc.Count, 3, "SchemaSet count");
        //    CError.Compare(bWarningCallback, false, "Warning not thrown");
        //    return;
        //}
        ////[Variation(Desc = "v6 - schema(Intranet)->schema(Internet)->schema(Internet)", Priority = 1)]
        //[InlineData()]
        //public int v6()
        //{
        //    Initialize();
        //    XmlSchemaSet sc = new XmlSchemaSet();
        //    sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
        //    sc.Add(null, TestData._Root + "xmlresolver_v6_a.xsd");
        //    CError.Compare(sc.Count, 3, "SchemaSet count");
        //    CError.Compare(bWarningCallback, false, "Warning not thrown");
        //    return;
        //}
        ////[Variation(Desc = "v7 - schema(Local)->schema(Internet)[->schema(Internet)][schema(Local)]", Priority = 1)]
        //[InlineData()]
        //public int v7()
        //{
        //    Initialize();
        //    XmlSchemaSet sc = new XmlSchemaSet();
        //    sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
        //    sc.Add(null, TestData._Root + "xmlresolver_v7_a.xsd");
        //    CError.Compare(sc.Count, 3, "SchemaSet count");
        //    CError.Compare(bWarningCallback, false, "Warning not thrown");
        //    return;
        //}
    }
}

//todo: add tests for include
//add tests for intranet
//add tests for custom resolver
