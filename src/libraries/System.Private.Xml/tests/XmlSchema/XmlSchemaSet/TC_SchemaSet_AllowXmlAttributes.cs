// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_AllowXmlAttributes", Desc = "")]
    public class TC_SchemaSet_AllowXmlAttributes
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_AllowXmlAttributes(ITestOutputHelper output)
        {
            _output = output;
        }

        //todo: use rootpath
        public bool bWarningCallback;

        public bool bErrorCallback;
        public int errorCount;
        public int warningCount;
        public string testData = null;

        public void Initialize()
        {
            this.testData = Path.Combine(TestData._Root, "AllowXmlAttributes");
            bWarningCallback = bErrorCallback = false;
            errorCount = warningCount = 0;
        }

        //hook up validaton callback
        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                _output.WriteLine("WARNING: ");
                bWarningCallback = true;
                warningCount++;
            }
            else if (args.Severity == XmlSeverityType.Error)
            {
                _output.WriteLine("ERROR: ");
                bErrorCallback = true;
                errorCount++;
            }

            XmlSchemaException se = args.Exception as XmlSchemaException;
            _output.WriteLine("Exception Message:" + se.Message + "\n");
            if (se.InnerException != null)
            {
                _output.WriteLine("InnerException Message:" + se.InnerException.Message + "\n");
            }
            else

                _output.WriteLine("Inner Exception is NULL\n");
        }

        private XmlReader CreateReader(string xmlFile, XmlSchemaSet ss, bool allowXml)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = new XmlUrlResolver();

            settings.Schemas = new XmlSchemaSet();
            settings.Schemas.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            settings.Schemas.Add(ss);
            settings.ValidationType = ValidationType.Schema;
            if (allowXml == true)
                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation |
                                            XmlSchemaValidationFlags.ProcessInlineSchema |
                                            XmlSchemaValidationFlags.ReportValidationWarnings;
            else
                settings.ValidationFlags = XmlSchemaValidationFlags.ProcessSchemaLocation |
                                        XmlSchemaValidationFlags.ProcessInlineSchema |
                                        XmlSchemaValidationFlags.ReportValidationWarnings |
                                        XmlSchemaValidationFlags.ProcessIdentityConstraints;

            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlReader vr = XmlReader.Create(xmlFile, settings);
            return vr;
        }

        /*
         *	Attribute Wildcards
         */

        [Theory]
        //[Variation(Desc = "v8.4.4- Attributes Wildcards(11), allowXmlAttribute=true", Priority = 1, id = 81, Params = new object[] { "v9-4.xml", "v9-4.xsd", true, 0, 1, 2 })]
        [InlineData("v9-4.xml", "v9-4.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v8.4.3- Attributes Wildcards(11), allowXmlAttribute=false", Priority = 1, id = 80, Params = new object[] { "v9-4.xml", "v9-4.xsd", false, 0, 1, 2 })]
        [InlineData("v9-4.xml", "v9-4.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v8.4.2- Attributes Wildcards(10), allowXmlAttribute=true", Priority = 1, id = 79, Params = new object[] { "v9-3.xml", "v9-4.xsd", true, 1, 0, 2 })]
        [InlineData("v9-3.xml", "v9-4.xsd", true, 1, 0, 2)]
        //[Variation(Desc = "v8.4.1- Attributes Wildcards(10), allowXmlAttribute=false", Priority = 1, id = 78, Params = new object[] { "v9-3.xml", "v9-4.xsd", false, 1, 0, 2 })]
        [InlineData("v9-3.xml", "v9-4.xsd", false, 1, 0, 2)]
        //[Variation(Desc = "v8.3.6- Attributes Wildcards(9), allowXmlAttribute=true", Priority = 1, id = 76, Params = new object[] { "v9-3.xml", "v9-3.xsd", true, 0, 1, 2 })]
        [InlineData("v9-3.xml", "v9-3.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v8.3.5- Attributes Wildcards(9), allowXmlAttribute=false", Priority = 1, id = 75, Params = new object[] { "v9-3.xml", "v9-3.xsd", false, 0, 1, 2 })]
        [InlineData("v9-3.xml", "v9-3.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v8.3.4- Attributes Wildcards(8), allowXmlAttribute=true", Priority = 1, id = 74, Params = new object[] { "v9-2.xml", "v9-3.xsd", true, 0, 0, 2 })]
        [InlineData("v9-2.xml", "v9-3.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v8.3.3- Attributes Wildcards(8), allowXmlAttribute=false", Priority = 1, id = 73, Params = new object[] { "v9-2.xml", "v9-3.xsd", false, 0, 0, 2 })]
        [InlineData("v9-2.xml", "v9-3.xsd", false, 0, 0, 2)]
        //[Variation(Desc = "v8.3.2- Attributes Wildcards(7), allowXmlAttribute=true", Priority = 1, id = 72, Params = new object[] { "v9-1.xml", "v9-3.xsd", true, 0, 0, 2 })]
        [InlineData("v9-1.xml", "v9-3.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v8.3.1- Attributes Wildcards(7), allowXmlAttribute=false", Priority = 1, id = 71, Params = new object[] { "v9-1.xml", "v9-3.xsd", false, 0, 0, 2 })]
        [InlineData("v9-1.xml", "v9-3.xsd", false, 0, 0, 2)]
        //[Variation(Desc = "v8.2.6- Attributes Wildcards(6), allowXmlAttribute=true", Priority = 1, id = 70, Params = new object[] { "v9-3.xml", "v9-2.xsd", true, 0, 1, 2 })]
        [InlineData("v9-3.xml", "v9-2.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v8.2.5- Attributes Wildcards(6), allowXmlAttribute=false", Priority = 1, id = 69, Params = new object[] { "v9-3.xml", "v9-2.xsd", false, 0, 1, 2 })]
        [InlineData("v9-3.xml", "v9-2.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v8.2.4- Attributes Wildcards(5), allowXmlAttribute=true", Priority = 1, id = 68, Params = new object[] { "v9-2.xml", "v9-2.xsd", true, 0, 0, 2 })]
        [InlineData("v9-2.xml", "v9-2.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v8.2.3- Attributes Wildcards(5), allowXmlAttribute=false", Priority = 1, id = 67, Params = new object[] { "v9-2.xml", "v9-2.xsd", false, 0, 1, 2 })]
        [InlineData("v9-2.xml", "v9-2.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v8.2.2- Attributes Wildcards(4), allowXmlAttribute=true", Priority = 1, id = 66, Params = new object[] { "v9-1.xml", "v9-2.xsd", true, 0, 0, 2 })]
        [InlineData("v9-1.xml", "v9-2.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v8.2.1- Attributes Wildcards(4), allowXmlAttribute=false", Priority = 1, id = 65, Params = new object[] { "v9-1.xml", "v9-2.xsd", false, 0, 1, 2 })]
        [InlineData("v9-1.xml", "v9-2.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v8.1.6- Attributes Wildcards(4), allowXmlAttribute=true", Priority = 1, id = 64, Params = new object[] { "v9-3.xml", "v9-1.xsd", true, 0, 1, 2 })]
        [InlineData("v9-3.xml", "v9-1.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v8.1.5- Attributes Wildcards(3), allowXmlAttribute=false", Priority = 1, id = 63, Params = new object[] { "v9-3.xml", "v9-1.xsd", false, 0, 1, 2 })]
        [InlineData("v9-3.xml", "v9-1.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v8.1.4- Attributes Wildcards(2), allowXmlAttribute=true", Priority = 1, id = 62, Params = new object[] { "v9-2.xml", "v9-1.xsd", true, 0, 0, 2 })]
        [InlineData("v9-2.xml", "v9-1.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v8.1.3- Attributes Wildcards(2), allowXmlAttribute=false", Priority = 1, id = 61, Params = new object[] { "v9-2.xml", "v9-1.xsd", false, 0, 0, 2 })]
        [InlineData("v9-2.xml", "v9-1.xsd", false, 0, 0, 2)]
        //[Variation(Desc = "v8.1.2- Attributes Wildcards(1), allowXmlAttribute=true", Priority = 1, id = 60, Params = new object[] { "v9-1.xml", "v9-1.xsd", true, 0, 0, 2 })]
        [InlineData("v9-1.xml", "v9-1.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v8.1.1- Attributes Wildcards(1), allowXmlAttribute=false", Priority = 1, id = 59, Params = new object[] { "v9-1.xml", "v9-1.xsd", false, 0, 0, 2 })]
        [InlineData("v9-1.xml", "v9-1.xsd", false, 0, 0, 2)]
        /*
         *	Required and Prohibited Attributes
         */
        //[Variation(Desc = "v7.1.8- Required and Prohibited Attributes(4), allowXmlAttribute=true", Priority = 1, id = 58, Params = new object[] { "v8-4.xml", "v8-3.xsd", true, 0, 1, 2 })]
        [InlineData("v8-4.xml", "v8-3.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v7.1.7- Required and Prohibited Attributes(4), allowXmlAttribute=false", Priority = 1, id = 57, Params = new object[] { "v8-4.xml", "v8-3.xsd", false, 0, 2, 2 })]
        [InlineData("v8-4.xml", "v8-3.xsd", false, 0, 2, 2)]
        //[Variation(Desc = "v7.1.6- Required and Prohibited Attributes(3), allowXmlAttribute=true", Priority = 1, id = 56, Params = new object[] { "v8-3.xml", "v8-2.xsd", true, 0, 1, 2 })]
        [InlineData("v8-3.xml", "v8-2.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v7.1.5- Required and Prohibited Attributes(3), allowXmlAttribute=false", Priority = 1, id = 55, Params = new object[] { "v8-3.xml", "v8-2.xsd", false, 0, 1, 2 })]
        [InlineData("v8-3.xml", "v8-2.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v7.1.4- Required and Prohibited Attributes(2), allowXmlAttribute=true", Priority = 1, id = 54, Params = new object[] { "v8-2.xml", "v8-2.xsd", true, 0, 0, 2 })]
        [InlineData("v8-2.xml", "v8-2.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v7.1.3- Required and Prohibited Attributes(2), allowXmlAttribute=false", Priority = 1, id = 53, Params = new object[] { "v8-2.xml", "v8-2.xsd", false, 0, 1, 2 })]
        [InlineData("v8-2.xml", "v8-2.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v7.1.2- Required and Prohibited Attributes(1), allowXmlAttribute=true", Priority = 1, id = 52, Params = new object[] { "v8-1.xml", "v8-1.xsd", true, 0, 1, 2 })]
        [InlineData("v8-1.xml", "v8-1.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v7.1.1- Required and Prohibited Attributes(1), allowXmlAttribute=false", Priority = 1, id = 51, Params = new object[] { "v8-1.xml", "v8-1.xsd", false, 0, 2, 1 })]
        [InlineData("v8-1.xml", "v8-1.xsd", false, 0, 2, 1)]
        /*
         * * Undeclared root
         */
        //[Variation(Desc = "v6.2.2- Undeclared root, Particle has no reference to XML attributes, instance has attributes from default XML namespace, allowXmlAttribute=true", Priority = 1, id = 50, Params = new object[] { "v7-1.xml", "custom2.xsd", true, 1, 0, 3 })]
        [InlineData("v7-1.xml", "custom2.xsd", true, 1, 0, 3)]
        //[Variation(Desc = "v6.2.1- Undeclared root, Particle has no reference to XML attributes,  instance has attributes from default XML namespace, allowXmlAttribute=false", Priority = 1, id = 49, Params = new object[] { "v7-1.xml", "custom2.xsd", false, 1, 1, 3 })]
        [InlineData("v7-1.xml", "custom2.xsd", false, 1, 1, 3)]
        //[Variation(Desc = "v6.1.2- Undeclared root, Particle has no reference to XML attributes, instance has attributes from default XML namespace, allowXmlAttribute=true", Priority = 1, id = 48, Params = new object[] { "v7-1.xml", "v3.xsd", true, 1, 0, 2 })]
        [InlineData("v7-1.xml", "v3.xsd", true, 1, 0, 2)]
        //[Variation(Desc = "v6.1.1- Undeclared root, Particle has no reference to XML attributes,  instance has attributes from default XML namespace, allowXmlAttribute=false", Priority = 1, id = 47, Params = new object[] { "v7-1.xml", "v3.xsd", false, 1, 1, 1 })]
        [InlineData("v7-1.xml", "v3.xsd", false, 1, 1, 1)]
        /*
         * * No XML namespace System.Xml.Tests in the SchemaSet
         */
        //[Variation(Desc = "v5.1.8- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes, instance has base and foo attributes from XML namespace, allowXmlAttribute=true", Priority = 1, id = 46, Params = new object[] { "v5-3.xml", "v3.xsd", true, 0, 1, 2 })]
        [InlineData("v5-3.xml", "v3.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v5.1.7- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes,  instance has base, and foo attributes from XML namespace, allowXmlAttribute=false", Priority = 1, id = 45, Params = new object[] { "v5-3.xml", "v3.xsd", false, 0, 2, 1 })]
        [InlineData("v5-3.xml", "v3.xsd", false, 0, 2, 1)]
        //[Variation(Desc = "v5.1.6- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes, instance has base and lang attributes from XML namespace, allowXmlAttribute=true", Priority = 1, id = 44, Params = new object[] { "v4-1.xml", "v3.xsd", true, 0, 0, 2 })]
        [InlineData("v4-1.xml", "v3.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v5.1.5- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes,  instance has base, and lang attributes from XML namespace, allowXmlAttribute=false", Priority = 1, id = 43, Params = new object[] { "v4-1.xml", "v3.xsd", false, 0, 2, 1 })]
        [InlineData("v4-1.xml", "v3.xsd", false, 0, 2, 1)]
        //[Variation(Desc = "v5.1.4- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes, instance has foo attribute from XML namespace, allowXmlAttribute=true", Priority = 1, id = 42, Params = new object[] { "v2-1.xml", "v3.xsd", true, 0, 1, 2 })]
        [InlineData("v2-1.xml", "v3.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v5.1.3- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes,  instance has foo attribute from XML namespace, allowXmlAttribute=false", Priority = 1, id = 41, Params = new object[] { "v2-1.xml", "v3.xsd", false, 0, 1, 1 })]
        [InlineData("v2-1.xml", "v3.xsd", false, 0, 1, 1)]
        //[Variation(Desc = "v5.1.2- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes, instance has attributes from default XML namespace, allowXmlAttribute=true", Priority = 1, id = 40, Params = new object[] { "v1-1.xml", "v3.xsd", true, 0, 0, 2 })]
        [InlineData("v1-1.xml", "v3.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v5.1.1- No XML namespace System.Xml.Tests in the set, particle has no reference to XML attributes,  instance has attributes from default XML namespace, allowXmlAttribute=false", Priority = 1, id = 39, Params = new object[] { "v1-1.xml", "v3.xsd", false, 0, 1, 1 })]
        [InlineData("v1-1.xml", "v3.xsd", false, 0, 1, 1)]
        /*
         * * Default XML namespace System.Xml.Tests in the SchemaSet
         */
        //[Variation(Desc = "v4.4.2- Default XML namespace System.Xml.Tests in the set which imports another, particle has reference to XML attributes, instance has invalid attributes, allowXmlAttribute=true", Priority = 1, id = 38, Params = new object[] { "v5-3.xml", "custom2.xsd", true, 0, 0, 3 })]
        [InlineData("v5-3.xml", "custom2.xsd", true, 0, 0, 3)]
        //[Variation(Desc = "v4.4.1- Default XML namespace System.Xml.Tests in the set which imports another, particle has reference to XML attributes, instance has invalid attributes, allowXmlAttribute=false", Priority = 1, id = 37, Params = new object[] { "v5-3.xml", "custom2.xsd", false, 0, 1, 3 })]
        [InlineData("v5-3.xml", "custom2.xsd", false, 0, 1, 3)]
        //[Variation(Desc = "v4.3.4- Default XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to XML attributes, instance has invalid attributes, allowXmlAttribute=true", Priority = 1, id = 36, Params = new object[] { "v5-4.xml", null, true, 0, 1, 2 })]
        [InlineData("v5-4.xml", null, true, 0, 1, 2)]
        //[Variation(Desc = "v4.3.3- Default XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to XML attributes, instance has invalid attributes, allowXmlAttribute=false", Priority = 1, id = 35, Params = new object[] { "v5-4.xml", null, false, 0, 2, 2 })]
        [InlineData("v5-4.xml", null, false, 0, 2, 2)]
        //[Variation(Desc = "v4.3.2- Default XML namespace System.Xml.Tests in the set, particle has reference to XML attributes, instance has invalid attributes, allowXmlAttribute=true", Priority = 1, id = 34, Params = new object[] { "v5-3.xml", "v4-1.xsd", true, 0, 1, 2 })]
        [InlineData("v5-3.xml", "v4-1.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v4.3.1- Default XML namespace System.Xml.Tests in the set, particle has reference to XML attributes, instance has invalid attributes, allowXmlAttribute=false", Priority = 1, id = 33, Params = new object[] { "v5-3.xml", "v4-1.xsd", false, 0, 1, 2 })]
        [InlineData("v5-3.xml", "v4-1.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v4.2.4- Default XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to XML attributes, instance has same attributes, allowXmlAttribute=true", Priority = 1, id = 32, Params = new object[] { "v5-2.xml", null, true, 0, 0, 2 })]
        [InlineData("v5-2.xml", null, true, 0, 0, 2)]
        //[Variation(Desc = "v4.2.3- Default XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to XML attributes, instance has same attributes, allowXmlAttribute=false", Priority = 1, id = 31, Params = new object[] { "v5-2.xml", null, false, 0, 1, 2 })]
        [InlineData("v5-2.xml", null, false, 0, 1, 2)]
        //[Variation(Desc = "v4.2.2- Default XML namespace System.Xml.Tests in the set, particle has reference to XML attributes, instance has same attributes, allowXmlAttribute=true", Priority = 1, id = 30, Params = new object[] { "v5-1.xml", "v4-1.xsd", true, 0, 0, 2 })]
        [InlineData("v5-1.xml", "v4-1.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v4.2.1- Default XML namespace System.Xml.Tests in the set, particle has reference to XML attributes, instance has same attributes, allowXmlAttribute=false", Priority = 1, id = 29, Params = new object[] { "v5-1.xml", "v4-1.xsd", false, 0, 1, 2 })]
        [InlineData("v5-1.xml", "v4-1.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v4.1.4- Default XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to custom attribute, allowXmlAttribute=true", Priority = 1, id = 28, Params = new object[] { "v4-2.xml", null, true, 0, 0, 2 })]
        [InlineData("v4-2.xml", null, true, 0, 0, 2)]
        //[Variation(Desc = "v4.1.3- Default XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to custom attribute, allowXmlAttribute=false", Priority = 1, id = 27, Params = new object[] { "v4-2.xml", null, false, 0, 0, 2 })]
        [InlineData("v4-2.xml", null, false, 0, 0, 2)]
        //[Variation(Desc = "v4.1.2- Default XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, allowXmlAttribute=true", Priority = 1, id = 26, Params = new object[] { "v4-1.xml", "v4-1.xsd", true, 0, 0, 2 })]
        [InlineData("v4-1.xml", "v4-1.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v4.1.1- Default XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, allowXmlAttribute=false", Priority = 1, id = 25, Params = new object[] { "v4-1.xml", "v4-1.xsd", false, 0, 0, 2 })]
        [InlineData("v4-1.xml", "v4-1.xsd", false, 0, 0, 2)]
        /*
         * * Custom XML namespace System.Xml.Tests in the SchemaSet
         */

        //[Variation(Desc = "v3.2.4- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has that attribute invalid custom attribute with schemalocation, allowXmlAttribute=true", Priority = 1, id = 24, Params = new object[] { "v3-3.xml", null, true, 0, 1, 3 })]
        [InlineData("v3-3.xml", null, true, 0, 1, 3)]
        //[Variation(Desc = "v3.2.3- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has that attribute invalid custom attribute with schemalocation, allowXmlAttribute=false", Priority = 1, id = 23, Params = new object[] { "v3-3.xml", null, false, 0, 1, 3 })]
        [InlineData("v3-3.xml", null, false, 0, 1, 3)]
        //[Variation(Desc = "v3.2.2- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has invalid custom attribute, allowXmlAttribute=true", Priority = 1, id = 22, Params = new object[] { "v3-1.xml", "custom2.xsd", true, 0, 1, 3 })]
        [InlineData("v3-1.xml", "custom2.xsd", true, 0, 1, 3)]
        //[Variation(Desc = "v3.2.1- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has invalid custom attribute, allowXmlAttribute=false", Priority = 1, id = 21, Params = new object[] { "v3-1.xml", "custom2.xsd", false, 0, 1, 3 })]
        [InlineData("v3-1.xml", "custom2.xsd", false, 0, 1, 3)]
        //[Variation(Desc = "v3.1.4- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has that attribute invalid custom attribute with schemalocation, allowXmlAttribute=true", Priority = 1, id = 20, Params = new object[] { "v3-2.xml", null, true, 0, 1, 2 })]
        [InlineData("v3-2.xml", null, true, 0, 1, 2)]
        //[Variation(Desc = "v3.1.3- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has that attribute invalid custom attribute with schemalocation, allowXmlAttribute=false", Priority = 1, id = 19, Params = new object[] { "v3-2.xml", null, false, 0, 1, 2 })]
        [InlineData("v3-2.xml", null, false, 0, 1, 2)]
        //[Variation(Desc = "v3.1.2- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has invalid custom attribute, allowXmlAttribute=true", Priority = 1, id = 18, Params = new object[] { "v3-1.xml", "v1.xsd", true, 0, 1, 2 })]
        [InlineData("v3-1.xml", "v1.xsd", true, 0, 1, 2)]
        //[Variation(Desc = "v3.1.1- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has invalid custom attribute, allowXmlAttribute=false", Priority = 1, id = 17, Params = new object[] { "v3-1.xml", "v1.xsd", false, 0, 1, 2 })]
        [InlineData("v3-1.xml", "v1.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v2.2.4- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has that attribute with schemalocation, allowXmlAttribute=true", Priority = 1, id = 16, Params = new object[] { "v2-3.xml", null, true, 0, 0, 3 })]
        [InlineData("v2-3.xml", null, true, 0, 0, 3)]
        //[Variation(Desc = "v2.2.3- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has that attribute with schemalocation, allowXmlAttribute=false", Priority = 1, id = 15, Params = new object[] { "v2-3.xml", null, false, 0, 0, 3 })]
        [InlineData("v2-3.xml", null, false, 0, 0, 3)]
        //[Variation(Desc = "v2.2.2- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has that attribute, allowXmlAttribute=true", Priority = 1, id = 14, Params = new object[] { "v2-1.xml", "custom2.xsd", true, 0, 0, 3 })]
        [InlineData("v2-1.xml", "custom2.xsd", true, 0, 0, 3)]
        //[Variation(Desc = "v2.2.1- Custom XML namespace System.Xml.Tests in the set which imports another, particle has reference to custom attribute, instance has that attribute, allowXmlAttribute=false", Priority = 1, id = 13, Params = new object[] { "v2-1.xml", "custom2.xsd", false, 0, 0, 3 })]
        [InlineData("v2-1.xml", "custom2.xsd", false, 0, 0, 3)]
        //[Variation(Desc = "v2.1.4- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has that attribute with schemalocation, allowXmlAttribute=true", Priority = 1, id = 12, Params = new object[] { "v2-2.xml", null, true, 0, 0, 2 })]
        [InlineData("v2-2.xml", null, true, 0, 0, 2)]
        //[Variation(Desc = "v2.1.3- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has that attribute with schemalocation, allowXmlAttribute=false", Priority = 1, id = 11, Params = new object[] { "v2-2.xml", null, false, 0, 0, 2 })]
        [InlineData("v2-2.xml", null, false, 0, 0, 2)]
        //[Variation(Desc = "v2.1.2- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has that attribute, allowXmlAttribute=true", Priority = 1, id = 10, Params = new object[] { "v2-1.xml", "v1.xsd", true, 0, 0, 2 })]
        [InlineData("v2-1.xml", "v1.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v2.1.1- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, instance has that attribute, allowXmlAttribute=false", Priority = 1, id = 9, Params = new object[] { "v2-1.xml", "v1.xsd", false, 0, 0, 2 })]
        [InlineData("v2-1.xml", "v1.xsd", false, 0, 0, 2)]
        //[Variation(Desc = "v1.2.4- Custom XML namespace System.Xml.Tests in the set importing another schema through schemaLocation, particle has reference to custom attribute, allowXmlAttribute=true", id = 8, Priority = 1, Params = new object[] { "v1-3.xml", null, true, 0, 0, 3 })]
        [InlineData("v1-3.xml", null, true, 0, 0, 3)]
        //[Variation(Desc = "v1.2.3- Custom XML namespace System.Xml.Tests in the set importing another schema through schemaLocation, particle has reference to custom attribute, allowXmlAttribute=false", id = 7, Priority = 1, Params = new object[] { "v1-3.xml", null, false, 0, 1, 3 })]
        [InlineData("v1-3.xml", null, false, 0, 1, 3)]
        //[Variation(Desc = "v1.2.2- Custom XML namespace System.Xml.Tests in the set importing another schema, particle has reference to custom attribute, allowXmlAttribute=true", Priority = 1, id = 6, Params = new object[] { "v1-1.xml", "custom2.xsd", true, 0, 0, 3 })]
        [InlineData("v1-1.xml", "custom2.xsd", true, 0, 0, 3)]
        //[Variation(Desc = "v1.2.1- Custom XML namespace System.Xml.Tests in the set importing another schema, particle has reference to custom attribute, allowXmlAttribute=false", Priority = 1, id = 5, Params = new object[] { "v1-1.xml", "custom2.xsd", false, 0, 1, 3 })]
        [InlineData("v1-1.xml", "custom2.xsd", false, 0, 1, 3)]
        //[Variation(Desc = "v1.1.4- Custom XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to custom attribute, allowXmlAttribute=true", Priority = 1, id = 4, Params = new object[] { "v1-2.xml", "v1.xsd", true, 0, 0, 2 })]
        [InlineData("v1-2.xml", "v1.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v1.1.3- Custom XML namespace System.Xml.Tests in the set via schemaLocation, particle has reference to custom attribute, allowXmlAttribute=false", Priority = 1, id = 3, Params = new object[] { "v1-2.xml", "v1.xsd", false, 0, 1, 2 })]
        [InlineData("v1-2.xml", "v1.xsd", false, 0, 1, 2)]
        //[Variation(Desc = "v1.1.2- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, allowXmlAttribute=true", Priority = 1, id = 2, Params = new object[] { "v1-1.xml", "v1.xsd", true, 0, 0, 2 })]
        [InlineData("v1-1.xml", "v1.xsd", true, 0, 0, 2)]
        //[Variation(Desc = "v1.1.1- Custom XML namespace System.Xml.Tests in the set, particle has reference to custom attribute, allowXmlAttribute=false", Priority = 1, id = 1, Params = new object[] { "v1-1.xml", "v1.xsd", false, 0, 1, 2 })]
        [InlineData("v1-1.xml", "v1.xsd", false, 0, 1, 2)]
        public void v1(string xmlFile, string xsdFile, bool allowXmlAttributes, int expectedWarningCount, int expectedErrorCount, int expectedSchemaSetCount)
        {
            Initialize();
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            xss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            if (xsdFile != null)
                xss.Add(null, Path.Combine(testData, xsdFile));

            XmlReader vr = CreateReader(Path.Combine(testData, xmlFile), xss, allowXmlAttributes);
            while (vr.Read()) ;

            Assert.Equal(warningCount, expectedWarningCount);
            Assert.Equal(errorCount, expectedErrorCount);
            Assert.Equal(vr.Settings.Schemas.Count, expectedSchemaSetCount);
        }
    }
}
