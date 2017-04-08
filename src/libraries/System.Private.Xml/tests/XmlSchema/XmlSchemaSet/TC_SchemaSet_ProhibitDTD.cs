// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_ProhibitDTD", Desc = "")]
    public class TC_SchemaSet_ProhibitDTD : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_ProhibitDTD(ITestOutputHelper output)
        {
            _output = output;
        }

        public bool bWarningCallback;

        public bool bErrorCallback;
        public int errorCount;
        public int warningCount;

        public void Initialize()
        {
            bWarningCallback = bErrorCallback = false;
            errorCount = warningCount = 0;
        }

        //hook up validaton callback
        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            switch (args.Severity)
            {
                case XmlSeverityType.Warning:
                    _output.WriteLine("WARNING: ");
                    bWarningCallback = true;
                    warningCount++;
                    break;

                case XmlSeverityType.Error:
                    _output.WriteLine("ERROR: ");
                    bErrorCallback = true;
                    errorCount++;
                    break;
            }

            _output.WriteLine("Exception Message:" + args.Exception.Message + "\n");

            if (args.Exception.InnerException != null)
            {
                _output.WriteLine("InnerException Message:" + args.Exception.InnerException.Message + "\n");
            }
            else
            {
                _output.WriteLine("Inner Exception is NULL\n");
            }
        }

        private XmlReaderSettings GetSettings(bool prohibitDtd)
        {
            return new XmlReaderSettings
            {
#pragma warning disable 0618
                ProhibitDtd = prohibitDtd,
#pragma warning restore 0618
                XmlResolver = new XmlUrlResolver()
            };
        }

        private XmlReader CreateReader(string xmlFile, bool prohibitDtd)
        {
            return XmlReader.Create(xmlFile, GetSettings(prohibitDtd));
        }

        private XmlReader CreateReader(string xmlFile)
        {
            return XmlReader.Create(xmlFile);
        }

        private XmlReader CreateReader(XmlReader reader, bool prohibitDtd)
        {
            return XmlReader.Create(reader, GetSettings(prohibitDtd));
        }

        private XmlReader CreateReader(string xmlFile, XmlSchemaSet ss, bool prohibitDTD)
        {
            var settings = GetSettings(prohibitDTD);

            settings.Schemas = new XmlSchemaSet();
            settings.Schemas.XmlResolver = new XmlUrlResolver();
            settings.Schemas.ValidationEventHandler += ValidationCallback;
            settings.Schemas.Add(ss);
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                               XmlSchemaValidationFlags.ProcessSchemaLocation |
                               XmlSchemaValidationFlags.ProcessIdentityConstraints |
                               XmlSchemaValidationFlags.ProcessInlineSchema;

            settings.ValidationEventHandler += ValidationCallback;

            return XmlReader.Create(xmlFile, settings);
        }

        private XmlReader CreateReader(XmlReader reader, XmlSchemaSet ss, bool prohibitDTD)
        {
            var settings = GetSettings(prohibitDTD);

            settings.Schemas = new XmlSchemaSet();
            settings.Schemas.XmlResolver = new XmlUrlResolver();
            settings.Schemas.ValidationEventHandler += ValidationCallback;
            settings.Schemas.Add(ss);
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                               XmlSchemaValidationFlags.ProcessSchemaLocation |
                               XmlSchemaValidationFlags.ProcessIdentityConstraints |
                               XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationEventHandler += ValidationCallback;
            return XmlReader.Create(reader, settings);
        }

        //TEST DEFAULT VALUE FOR SCHEMA COMPILATION
        //[Variation(Desc = "v1- Test Default value of ProhibitDTD for Add(URL) of schema with DTD", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.ValidationEventHandler += ValidationCallback;
            try
            {
                xss.Add(null, Path.Combine(TestData._Root, "bug356711_a.xsd"));
            }
            catch (XmlException e)
            {
                CError.Compare(e.Message.Contains("DTD"), true, "Some other error thrown");
                return;
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v2- Test Default value of ProhibitDTD for Add(XmlReader) of schema with DTD", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.ValidationEventHandler += ValidationCallback;
            XmlReader r = CreateReader(Path.Combine(TestData._Root, "bug356711_a.xsd"));
            try
            {
                xss.Add(null, r);
            }
            catch (XmlException e)
            {
                CError.Compare(e.Message.Contains("DTD"), true, "Some other error thrown");
                return;
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v3- Test Default value of ProhibitDTD for Add(URL) containing xs:import for schema with DTD", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v3()
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;
            try
            {
                xss.Add(null, Path.Combine(TestData._Root, "bug356711.xsd"));
            }
            catch (XmlException)
            {
                Assert.True(false); //expect a validation warning for unresolvable schema location
            }
            CError.Compare(warningCount, 1, "Warning Count mismatch");
            return;
        }

        //[Variation(Desc = "v4- Test Default value of ProhibitDTD for Add(XmlReader) containing xs:import for scehma with DTD", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v4()
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;
            XmlReader r = CreateReader(Path.Combine(TestData._Root, "bug356711.xsd"));
            try
            {
                xss.Add(null, r);
            }
            catch (XmlException)
            {
                Assert.True(false); //expect a validation warning for unresolvable schema location
            }
            CError.Compare(warningCount, 1, "Warning Count mismatch");
            return;
        }

        //[Variation(Desc = "v5.2- Test Default value of ProhibitDTD for Add(TextReader) for schema with DTD", Priority = 1, Params = new object[] { "bug356711_a.xsd", 0 })]
        [InlineData("bug356711_a.xsd", 0)]
        //[Variation(Desc = "v5.1- Test Default value of ProhibitDTD for Add(TextReader) with an xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711.xsd", 0 })]
        [InlineData("bug356711.xsd", 0)]
        public void v5(object param0, object param1)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;
            XmlSchema schema = XmlSchema.Read(new StreamReader(new FileStream(Path.Combine(TestData._Root, param0.ToString()), FileMode.Open, FileAccess.Read)), ValidationCallback);
#pragma warning disable 0618
            schema.Compile(ValidationCallback, new XmlUrlResolver());
#pragma warning restore 0618
            try
            {
                xss.Add(schema);
            }
            catch (XmlException)
            {
                Assert.True(false); //expect a validation warning for unresolvable schema location
            }
            CError.Compare(warningCount, (int)param1, "Warning Count mismatch");
            CError.Compare(errorCount, 0, "Error Count mismatch");
            return;
        }

        //[Variation(Desc = "v6.2- Test Default value of ProhibitDTD for Add(XmlTextReader) for schema with DTD", Priority = 1, Params = new object[] { "bug356711_a.xsd" })]
        [InlineData("bug356711_a.xsd")]
        //[Variation(Desc = "v6.1- Test Default value of ProhibitDTD for Add(XmlTextReader) with an xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711.xsd" })]
        [InlineData("bug356711.xsd")]
        public void v6(object param0)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;
            var reader = new XmlTextReader(Path.Combine(TestData._Root, param0.ToString()));
            reader.XmlResolver = new XmlUrlResolver();
            XmlSchema schema = XmlSchema.Read(reader, ValidationCallback);
#pragma warning disable 0618
            schema.Compile(ValidationCallback);
#pragma warning restore 0618

            xss.Add(schema);

            // expect a validation warning for unresolvable schema location
            CError.Compare(warningCount, 0, "Warning Count mismatch");
            CError.Compare(errorCount, 0, "Error Count mismatch");
            return;
        }

        //[Variation(Desc = "v7- Test Default value of ProhibitDTD for Add(XmlReader) for schema with DTD", Priority = 1, Params = new object[] { "bug356711_a.xsd" })]
        [InlineData("bug356711_a.xsd")]
        [Theory]
        public void v7(object param0)
        {
            Initialize();

            try
            {
                XmlSchema schema = XmlSchema.Read(CreateReader(Path.Combine(TestData._Root, param0.ToString())), ValidationCallback);
#pragma warning disable 0618
                schema.Compile(ValidationCallback);
#pragma warning restore 0618
            }
            catch (XmlException e)
            {
                CError.Compare(e.Message.Contains("DTD"), true, "Some other error thrown");
                return;
            }

            Assert.True(false);
        }

        //[Variation(Desc = "v8- Test Default value of ProhibitDTD for Add(XmlReader) with xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711.xsd" })]
        [InlineData("bug356711.xsd")]
        [Theory]
        public void v8(object param0)
        {
            Initialize();

            try
            {
                XmlSchema schema = XmlSchema.Read(CreateReader(Path.Combine(TestData._Root, param0.ToString())), ValidationCallback);
#pragma warning disable 0618
                schema.Compile(ValidationCallback);
#pragma warning restore 0618
            }
            catch (XmlException)
            {
                Assert.True(false); //expect a validation warning for unresolvable schema location
            }

            CError.Compare(warningCount, 0, "Warning Count mismatch");
            return;
        }

        //TEST CUSTOM VALUE FOR SCHEMA COMPILATION
        //[Variation(Desc = "v10.1- Test Custom value of ProhibitDTD for SchemaSet.Add(XmlReader) with xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711.xsd" })]
        [InlineData("bug356711.xsd")]
        //[Variation(Desc = "v10.2- Test Custom value of ProhibitDTD for SchemaSet.Add(XmlReader) for schema with DTD", Priority = 1, Params = new object[] { "bug356711_a.xsd" })]
        [InlineData("bug356711_a.xsd")]
        [Theory]
        public void v10(object param0)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;

            XmlReader r = CreateReader(Path.Combine(TestData._Root, param0.ToString()), false);
            try
            {
                xss.Add(null, r);
            }
            catch (XmlException)
            {
                Assert.True(false);
            }
            CError.Compare(warningCount, 0, "Warning Count mismatch");
            CError.Compare(errorCount, 0, "Warning Count mismatch");
            return;
        }

        //[Variation(Desc = "v11.2- Test Custom value of ProhibitDTD for XmlSchema.Add(XmlReader) for schema with DTD", Priority = 1, Params = new object[] { "bug356711_a.xsd" })]
        [InlineData("bug356711_a.xsd")]
        //[Variation(Desc = "v11.1- Test Custom value of ProhibitDTD for XmlSchema.Add(XmlReader) with an xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711.xsd" })]
        [InlineData("bug356711.xsd")]
        [Theory]
        public void v11(object param0)
        {
            Initialize();

            try
            {
                XmlSchema schema = XmlSchema.Read(CreateReader(Path.Combine(TestData._Root, param0.ToString()), false), ValidationCallback);
#pragma warning disable 0618
                schema.Compile(ValidationCallback);
#pragma warning restore 0618
            }
            catch (XmlException)
            {
                Assert.True(false);
            }

            CError.Compare(warningCount, 0, "Warning Count mismatch");
            CError.Compare(errorCount, 0, "Warning Count mismatch");
            return;
        }

        //[Variation(Desc = "v12- Test with underlying reader with ProhibitDTD=true, and new Setting with True for schema with DTD", Priority = 1, Params = new object[] { "bug356711_a.xsd" })]
        [InlineData("bug356711_a.xsd")]
        [Theory]
        public void v12(object param0)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.ValidationEventHandler += ValidationCallback;

            XmlReader r = CreateReader(Path.Combine(TestData._Root, param0.ToString()), false);
            XmlReader r2 = CreateReader(r, true);
            try
            {
                xss.Add(null, r2);
            }
            catch (XmlException e)
            {
                CError.Compare(e.Message.Contains("DTD"), true, "Some other error thrown");
                return;
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v13- Test with underlying reader with ProhibitDTD=true, and new Setting with True for a schema with xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711.xsd" })]
        [InlineData("bug356711.xsd")]
        [Theory]
        public void v13(object param0)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;

            XmlReader r = CreateReader(Path.Combine(TestData._Root, param0.ToString()), false);
            XmlReader r2 = CreateReader(r, true);

            try
            {
                xss.Add(null, r2);
            }
            catch (XmlException)
            {
                Assert.True(false); //expect a validation warning for unresolvable schema location
            }

            _output.WriteLine("Count: " + xss.Count);
            CError.Compare(warningCount, 1, "Warning Count mismatch");
            return;
        }

        //[Variation(Desc = "v14 - SchemaSet.Add(XmlReader) with pDTD False ,then a SchemaSet.Add(URL) for schema with DTD", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v14()
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;

            XmlReader r = CreateReader(Path.Combine(TestData._Root, "bug356711.xsd"), false);

            try
            {
                xss.Add(null, r);
                CError.Compare(xss.Count, 2, "SchemaSet count mismatch!");
                xss.Add(null, Path.Combine(TestData._Root, "bug356711_b.xsd"));
            }
            catch (XmlException e)
            {
                CError.Compare(e.Message.Contains("DTD"), true, "Some other error thrown");
                return;
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v15 - SchemaSet.Add(XmlReader) with pDTD True ,then a SchemaSet.Add(XmlReader) with pDTD False with DTD", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v15()
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.ValidationEventHandler += ValidationCallback;

            XmlReader r1 = CreateReader(Path.Combine(TestData._Root, "bug356711_a.xsd"));
            XmlReader r2 = CreateReader(Path.Combine(TestData._Root, "bug356711_b.xsd"), false);

            try
            {
                xss.Add(null, r1);
            }
            catch (XmlException e)
            {
                CError.Compare(e.Message.Contains("DTD"), true, "Some other error thrown");
                CError.Compare(xss.Count, 0, "SchemaSet count mismatch!");
            }

            try
            {
                xss.Add(null, r2);
            }
            catch (Exception)
            {
                Assert.True(false);
            }
            CError.Compare(xss.Count, 1, "SchemaSet count mismatch!");
            return;
        }

        //TEST DEFAULT VALUE FOR INSTANCE VALIDATION
        //[Variation(Desc = "v20.1- Test Default value of ProhibitDTD for XML containing noNamespaceSchemaLocation for schema which contains xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711_1.xml" })]
        [InlineData("bug356711_1.xml")]
        //[Variation(Desc = "v20.2- Test Default value of ProhibitDTD for XML containing schemaLocation for schema with DTD", Priority = 1, Params = new object[] { "bug356711_2.xml" })]
        [InlineData("bug356711_2.xml")]
        //[Variation(Desc = "v20.3- Test Default value of ProhibitDTD for XML containing Inline schema containing xs:import of a schema with DTD", Priority = 1, Params = new object[] { "bug356711_3.xml" })]
        [InlineData("bug356711_3.xml")]
        //[Variation(Desc = "v20.4- Test Default value of ProhibitDTD for XML containing Inline schema containing xs:import of a schema which has a xs:import of schema with DTD", Priority = 1, Params = new object[] { "bug356711_4.xml" })]
        [InlineData("bug356711_4.xml")]
        [Theory]
        public void v20(object param0)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;
            xss.Add(null, Path.Combine(TestData._Root, "bug356711_root.xsd"));

            try
            {
                XmlReader reader = CreateReader(Path.Combine(TestData._Root, param0.ToString()), xss, true);
                while (reader.Read()) ;
            }
            catch (XmlException)
            {
                Assert.True(false);
            }
            CError.Compare(warningCount, 2, "ProhibitDTD did not work with schemaLocation");
            return;
        }

        //[Variation(Desc = "v21- Underlying XmlReader with ProhibitDTD=False and Create new Reader with ProhibitDTD=True", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v21()
        {
            Initialize();
            var xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;
            xss.Add(null, Path.Combine(TestData._Root, "bug356711_root.xsd"));

            try
            {
                using (var r1 = CreateReader(Path.Combine(TestData._Root, "bug356711_1.xml"), false))
                using (var r2 = CreateReader(r1, xss, true))
                {
                    while (r2.Read()) { }
                }
            }
            catch (XmlException)
            {
                Assert.True(false);
            }
            CError.Compare(warningCount, 2, "ProhibitDTD did not work with schemaLocation");
            return;
        }

        //TEST CUSTOM VALUE FOR INSTANCE VALIDATION
        //[Variation(Desc = "v22.1- Test Default value of ProhibitDTD for XML containing noNamespaceSchemaLocation for schema which contains xs:import for schema with DTD", Priority = 1, Params = new object[] { "bug356711_1.xml" })]
        [InlineData("bug356711_1.xml")]
        //[Variation(Desc = "v22.2- Test Default value of ProhibitDTD for XML containing schemaLocation for schema with DTD", Priority = 1, Params = new object[] { "bug356711_2.xml" })]
        [InlineData("bug356711_2.xml")]
        //[Variation(Desc = "v22.3- Test Default value of ProhibitDTD for XML containing Inline schema containing xs:import of a schema with DTD", Priority = 1, Params = new object[] { "bug356711_3.xml" })]
        [InlineData("bug356711_3.xml")]
        //[Variation(Desc = "v22.4- Test Default value of ProhibitDTD for XML containing Inline schema containing xs:import of a schema which has a xs:import of schema with DTD", Priority = 1, Params = new object[] { "bug356711_4.xml" })]
        [InlineData("bug356711_4.xml")]
        [Theory]
        public void v22(object param0)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += ValidationCallback;
            xss.Add(null, Path.Combine(TestData._Root, "bug356711_root.xsd"));

            try
            {
                XmlReader reader = CreateReader(Path.Combine(TestData._Root, param0.ToString()), xss, false);
                while (reader.Read()) ;
            }
            catch (XmlException)
            {
                Assert.True(false);
            }
            CError.Compare(errorCount, 0, "ProhibitDTD did not work with schemaLocation");
            return;
        }

        //[Variation(Desc = "v23- Underlying XmlReader with ProhibitDTD=True and Create new Reader with ProhibitDTD=False", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v23()
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.ValidationEventHandler += ValidationCallback;
            xss.Add(null, Path.Combine(TestData._Root, "bug356711_root.xsd"));

            try
            {
                XmlReader r1 = CreateReader(Path.Combine(TestData._Root, "bug356711_1.xml"), true);
                XmlReader r2 = CreateReader(r1, xss, false);
                while (r2.Read()) ;
            }
            catch (XmlException)
            {
                Assert.True(false);
            }
            CError.Compare(errorCount, 0, "ProhibitDTD did not work with schemaLocation");
            return;
        }
    }
}
