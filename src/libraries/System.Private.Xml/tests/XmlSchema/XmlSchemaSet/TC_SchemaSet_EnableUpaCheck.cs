// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_EnableUpaCheck", Desc = "")]
    public class TC_SchemaSet_EnableUpaCheck
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_EnableUpaCheck(ITestOutputHelper output)
        {
            _output = output;
        }


        //todo: use rootpath
        public bool bWarningCallback;

        public bool bErrorCallback;
        public int errorCount;
        public int[] errorLineNumbers;

        public string testData = null;

        public void Initialize()
        {
            this.testData = Path.Combine(TestData._Root, "EnableUpaCheck");
            bWarningCallback = bErrorCallback = false;
            errorCount = 0;
            errorLineNumbers = new Int32[10];
        }

        //Hook up validaton callback
        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                _output.WriteLine("ERROR: ");
                bErrorCallback = true;
                XmlSchemaException se = args.Exception as XmlSchemaException;
                errorLineNumbers[errorCount] = se.LineNumber;
                errorCount++;

                _output.WriteLine("Exception Message:" + se.Message + "\n");

                if (se.InnerException != null)
                {
                    _output.WriteLine("InnerException Message:" + se.InnerException.Message + "\n");
                }
                else

                    _output.WriteLine("Inner Exception is NULL\n");
            }
        }

        public XmlReader CreateReader(string xmlFile, XmlSchemaSet ss, bool UpaCheck)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            settings.Schemas = new XmlSchemaSet();
            settings.Schemas.CompilationSettings.EnableUpaCheck = UpaCheck;
            settings.Schemas.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            settings.Schemas.Add(ss);
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation |
                                        XmlSchemaValidationFlags.ProcessInlineSchema |
                                        XmlSchemaValidationFlags.ReportValidationWarnings;

            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlReader vr = XmlReader.Create(xmlFile, settings);
            return vr;
        }

        /* a choice containing a wildcard and element declaraions */

        //[Variation(Desc = "v6-2- a choice containing a wildcard and element declaraions(2)", Priority = 1, id = 25, Params = new object[] { "v6-2.xml", "v6-2.xsd", 0 })]
        [InlineData("v6-2.xml", "v6-2.xsd", 0, new int[] { })]
        //[Variation(Desc = "v6-1- a choice containing a wildcard and element declaraions(1)", Priority = 1, id = 24, Params = new object[] { "v6-1.xml", "v6-1.xsd", 0 })]
        [InlineData("v6-1.xml", "v6-1.xsd", 0, new int[] { })]
        /* Sequence having 3 sequences each having an optional wildcard and two elements of same name */
        //[Variation(Desc = "v5-2- Sequence having 3 sequences each having an optional wildcard and two elements of same name(2)", Priority = 1, id = 23, Params = new object[] { "v5-2.xml", "v5-2.xsd", 1, 15 })]
        [InlineData("v5-2.xml", "v5-2.xsd", 1, new int[] { 15 })]
        //[Variation(Desc = "v5-1- Sequence having 3 sequences each having an optional wildcard and two elements of same name(1)", Priority = 1, id = 22, Params = new object[] { "v5-1.xml", "v5-1.xsd", 1, 23 })]
        [InlineData("v5-1.xml", "v5-1.xsd", 1, new int[] { 23 })]
        /* Optional wildcards before two element declarations*/
        //[Variation(Desc = "v4.5- Optional wildcards before two element declarations(5)", Priority = 1, id = 21, Params = new object[] { "v4-5.xml", "v4-2.xsd", 1, 16 })]
        [InlineData("v4-5.xml", "v4-2.xsd", 1, new int[] { 16 })]
        //[Variation(Desc = "v4.4- Optional wildcards before two element declarations(4)", Priority = 1, id = 20, Params = new object[] { "v4-4.xml", "v4-2.xsd", 1, 11 })]
        [InlineData("v4-4.xml", "v4-2.xsd", 1, new int[] { 11 })]
        //[Variation(Desc = "v4.3- Optional wildcards before two element declarations(3)", Priority = 1, id = 19, Params = new object[] { "v4-3.xml", "v4-1.xsd", 1, 11 })]
        [InlineData("v4-3.xml", "v4-1.xsd", 1, new int[] { 11 })]
        //[Variation(Desc = "v4.2- Optional wildcards before two element declarations(2)", Priority = 1, id = 18, Params = new object[] { "v4-2.xml", "v4-1.xsd", 1, 11 })]
        [InlineData("v4-2.xml", "v4-1.xsd", 1, new int[] { 11 })]
        //[Variation(Desc = "v4.1- Optional wildcards before two element declarations(1)", Priority = 1, id = 17, Params = new object[] { "v4-1.xml", "v4-1.xsd", 0 })]
        [InlineData("v4-1.xml", "v4-1.xsd", 0, new int[] { })]
        /* Optional wildcards between two element declarations*/
        //[Variation(Desc = "v3.6- Optional wildcards between two element declarations(6)", Priority = 1, id = 16, Params = new object[] { "v3-6.xml", "v3.xsd", 1, 76 })]
        [InlineData("v3-6.xml", "v3.xsd", 1, new int[] { 76 })]
        //[Variation(Desc = "v3.5- Optional wildcards between two element declarations(5)", Priority = 1, id = 15, Params = new object[] { "v3-5.xml", "v3.xsd", 2, 13, 62 })]
        [InlineData("v3-5.xml", "v3.xsd", 2, new int[] { 13, 62 })]
        //[Variation(Desc = "v3.4- Optional wildcards between two element declarations(4)", Priority = 1, id = 14, Params = new object[] { "v3-4.xml", "v3.xsd", 2, 13, 16 })]
        [InlineData("v3-4.xml", "v3.xsd", 2, new int[] { 13, 16 })]
        //[Variation(Desc = "v3.3- Optional wildcards between two element declarations(3)", Priority = 1, id = 13, Params = new object[] { "v3-3.xml", "v3.xsd", 1, 11 })]
        [InlineData("v3-3.xml", "v3.xsd", 1, new int[] { 11 })]
        //[Variation(Desc = "v3.2- Optional wildcards between two element declarations(2)", Priority = 1, id = 12, Params = new object[] { "v3-2.xml", "v3.xsd", 1, 15 })]
        [InlineData("v3-2.xml", "v3.xsd", 1, new int[] { 15 })]
        //[Variation(Desc = "v3.1- Optional wildcards between two element declarations(1)", Priority = 1, id = 11, Params = new object[] { "v3-1.xml", "v3.xsd", 1, 11 })]
        [InlineData("v3-1.xml", "v3.xsd", 1, new int[] { 11 })]
        /* Sequence of choices having same element name and same type */
        //[Variation(Desc = "v2.7- Sequence of choices with same element name,one which doesnt match fixed value in schema(7)", Priority = 1, id = 10, Params = new object[] { "v2-7.xml", "v2-7.xsd", 0 })]
        [InlineData("v2-7.xml", "v2-7.xsd", 0, new int[] { })]
        //[Variation(Desc = "v2.6- Sequence of choices with same element name,one which doesnt match fixed value in schema(6)", Priority = 1, id = 9, Params = new object[] { "v2-6.xml", "v2-6.xsd", 3, 2, 5, 10 })]
        [InlineData("v2-6.xml", "v2-6.xsd", 3, new int[] { 2, 5, 10 })]
        //[Variation(Desc = "v2.5- Sequence of choices with same element name,one which doesnt match fixed value in schema(5)", Priority = 1, id = 8, Params = new object[] { "v2-5.xml", "v2-5.xsd", 2, 5, 10 })]
        [InlineData("v2-5.xml", "v2-5.xsd", 2, new int[] { 5, 10 })]
        //[Variation(Desc = "v2.4- Sequence of choices with same element name,one which doesnt match fixed value in schema(4)", Priority = 1, id = 7, Params = new object[] { "v2-4.xml", "v2-4.xsd", 2, 5, 7 })]
        [InlineData("v2-4.xml", "v2-4.xsd", 2, new int[] { 5, 7 })]
        //[Variation(Desc = "v2.3- Sequence of choices with same element name,one which doesnt match fixed value in schema(3)", Priority = 1, id = 6, Params = new object[] { "v2-3.xml", "v2-3.xsd", 1, 3 })]
        [InlineData("v2-3.xml", "v2-3.xsd", 1, new int[] { 3 })]
        //[Variation(Desc = "v2.2- Sequence of choices with same element name,one which doesnt match fixed value in schema(2)", Priority = 1, id = 5, Params = new object[] { "v2-2.xml", "v2-2.xsd", 1, 3 })]
        [InlineData("v2-2.xml", "v2-2.xsd", 1, new int[] { 3 })]
        //[Variation(Desc = "v2.1- Sequence of choices with same element name, one which doesnt match fixed value in schema(1)", Priority = 1, id = 4, Params = new object[] { "v2-1.xml", "v2-1.xsd", 3, 3, 4, 5 })]
        [InlineData("v2-1.xml", "v2-1.xsd", 3, new int[] { 3, 4, 5 })]
        /* Sequence with same element name and same type */

        //[Variation(Desc = "v1.3- Sequence on element with same name and type, one has fixed value, instance has violation of fixed", Priority = 1, id = 3, Params = new object[] { "v1-3.xml", "v1-3.xsd", 1, 2 })]
        [InlineData("v1-3.xml", "v1-3.xsd", 1, new int[] { 2 })]
        //[Variation(Desc = "v1.2- Sequence on element with same name and type, one has default value", Priority = 1, id = 2, Params = new object[] { "v1-2.xml", "v1-2.xsd", 0 })]
        [InlineData("v1-2.xml", "v1-2.xsd", 0, new int[] { })]
        //[Variation(Desc = "v1.1- Sequence on element with same name and type", Priority = 1, id = 1, Params = new object[] { "v1-1.xml", "v1.xsd", 0 })]
        [InlineData("v1-1.xml", "v1.xsd", 0, new int[] { })]
        [Theory]
        public void v1(object param0, object param1, object param2, int[] expectedErrorLineNumbers)
        {
            string xmlFile = param0.ToString();
            string xsdFile = param1.ToString();
            int expectedErrorCount = (int)param2;

            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            xss.Add(null, Path.Combine(testData, xsdFile));

            XmlReader vr = CreateReader(Path.Combine(testData, xmlFile), xss, false);
            while (vr.Read()) ;

            CError.Compare(errorCount, expectedErrorCount, "Error Count mismatch");

            if (errorCount > 0) //compare only if there is an error
            {
                for (int i = 0; i < errorCount; i++)
                {
                    CError.Compare(errorLineNumbers[i], expectedErrorLineNumbers[i], "Error Line Number is different");
                }
            }

            return;
        }
    }
}
