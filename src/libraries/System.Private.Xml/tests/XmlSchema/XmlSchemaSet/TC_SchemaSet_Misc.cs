// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Misc", Desc = "")]
    public class TC_SchemaSet_Misc : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Misc(ITestOutputHelper output)
        {
            _output = output;
        }
        
        public bool bWarningCallback;

        public bool bErrorCallback;
        public int errorCount;
        public int warningCount;
        public bool WarningInnerExceptionSet = false;
        public bool ErrorInnerExceptionSet = false;

        public void Initialize()
        {
            bWarningCallback = bErrorCallback = false;
            errorCount = warningCount = 0;
            WarningInnerExceptionSet = ErrorInnerExceptionSet = false;
        }

        //hook up validaton callback
        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                _output.WriteLine("WARNING: ");
                bWarningCallback = true;
                warningCount++;
                WarningInnerExceptionSet = (args.Exception.InnerException != null);
                _output.WriteLine("\nInnerExceptionSet : " + WarningInnerExceptionSet + "\n");
            }
            else if (args.Severity == XmlSeverityType.Error)
            {
                _output.WriteLine("ERROR: ");
                bErrorCallback = true;
                errorCount++;
                ErrorInnerExceptionSet = (args.Exception.InnerException != null);
                _output.WriteLine("\nInnerExceptionSet : " + ErrorInnerExceptionSet + "\n");
            }

            _output.WriteLine(args.Message); // Print the error to the screen.
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - Bug110823 - SchemaSet.Add is holding onto some of the schema files after adding", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.XmlResolver = new XmlUrlResolver();
            using (XmlTextReader xtr = new XmlTextReader(Path.Combine(TestData._Root, "bug110823.xsd")))
            {
                xss.Add(XmlSchema.Read(xtr, null));
            }
        }

        //[Variation(Desc = "v2 - Bug115049 - XSD: content model validation for an invalid root element should be abandoned", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            Initialize();
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss.Add(null, Path.Combine(TestData._Root, "bug115049.xsd"));
            ss.Compile();

            //create reader
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings |
                                       XmlSchemaValidationFlags.ProcessSchemaLocation |
                                       XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            settings.Schemas.Add(ss);
            XmlReader vr = XmlReader.Create(Path.Combine(TestData._Root, "bug115049.xml"), settings);
            while (vr.Read()) ;
            CError.Compare(errorCount, 1, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "v4 - 243300 - We are not correctly handling xs:anyType as xsi:type in the instance", Priority = 2)]
        [InlineData()]
        [Theory]
        public void v4()
        {
            string xml = @"<a xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xsi:type='xsd:anyType'>1242<b/></a>";
            Initialize();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = new XmlUrlResolver();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings |
                                       XmlSchemaValidationFlags.ProcessSchemaLocation |
                                       XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlReader vr = XmlReader.Create(new StringReader(xml), settings, (string)null);
            while (vr.Read()) ;
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            CError.Compare(warningCount, 1, "Warning Count mismatch!");
            return;
        }

        /* Parameters = file name , is custom xml namespace System.Xml.Tests */

        //[Variation(Desc = "v20 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v10.xsd", 2, false })]
        [InlineData("bug264908_v10.xsd", 2, false)]
        //[Variation(Desc = "v19 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v9.xsd", 5, true })]
        [InlineData("bug264908_v9.xsd", 5, true)]
        //[Variation(Desc = "v18 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v8.xsd", 5, false })]
        [InlineData("bug264908_v8.xsd", 5, false)]
        //[Variation(Desc = "v17 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v7.xsd", 4, false })]
        [InlineData("bug264908_v7.xsd", 4, false)]
        //[Variation(Desc = "v16 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v6.xsd", 4, true })]
        [InlineData("bug264908_v6.xsd", 4, true)]
        //[Variation(Desc = "v15 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v5.xsd", 4, false })]
        [InlineData("bug264908_v5.xsd", 4, false)]
        //[Variation(Desc = "v14 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v4.xsd", 4, true })]
        [InlineData("bug264908_v4.xsd", 4, true)]
        //[Variation(Desc = "v13 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v3.xsd", 1, true })]
        [InlineData("bug264908_v3.xsd", 1, true)]
        //[Variation(Desc = "v12 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v2.xsd", 1, true })]
        [InlineData("bug264908_v2.xsd", 1, true)]
        //[Variation(Desc = "v11 - DCR 264908 - XSD: Support user specified schema for http://www.w3.org/XML/1998/namespace System.Xml.Tests", Priority = 1, Params = new object[] { "bug264908_v1.xsd", 3, true })]
        [InlineData("bug264908_v1.xsd", 3, true)]
        [Theory]
        public void v10(object param0, object param1, object param2)
        {
            string xmlFile = param0.ToString();
            int count = (int)param1;
            bool custom = (bool)param2;
            string attName = "blah";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss.Add(null, Path.Combine(TestData._Root, xmlFile));
            ss.Compile();

            //test the count
            CError.Compare(ss.Count, count, "Count of SchemaSet not matched!");

            //make sure the correct schema is in the set
            if (custom)
            {
                foreach (XmlSchemaAttribute a in ss.GlobalAttributes.Values)
                {
                    if (a.QualifiedName.Name == attName)
                        return;
                }
                Assert.True(false);
            }
            return;
        }

        //[Variation(Desc = "v21 - Bug 319346 - Chameleon add of a schema into the xml namespace", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v20()
        {
            string xmlns = @"http://www.w3.org/XML/1998/namespace";
            string attName = "blah";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss.Add(xmlns, Path.Combine(TestData._Root, "bug264908_v11.xsd"));
            ss.Compile();

            //test the count
            CError.Compare(ss.Count, 3, "Count of SchemaSet not matched!");

            //make sure the correct schema is in the set

            foreach (XmlSchemaAttribute a in ss.GlobalAttributes.Values)
            {
                if (a.QualifiedName.Name == attName)
                    return;
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v22 - Bug 338038 - Component should be additive into the Xml namespace", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v21()
        {
            string xmlns = @"http://www.w3.org/XML/1998/namespace";
            string attName = "blah1";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss.Add(xmlns, Path.Combine(TestData._Root, "bug338038_v1.xsd"));
            ss.Compile();

            //test the count
            CError.Compare(ss.Count, 4, "Count of SchemaSet not matched!");

            //make sure the correct schema is in the set

            foreach (XmlSchemaAttribute a in ss.GlobalAttributes.Values)
            {
                if (a.QualifiedName.Name == attName)
                    return;
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v23 - Bug 338038 - Conflicting components in custome xml namespace System.Xml.Tests be caught", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v22()
        {
            string xmlns = @"http://www.w3.org/XML/1998/namespace";
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(xmlns, Path.Combine(TestData._Root, "bug338038_v2.xsd"));

            try
            {
                ss.Compile();
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
                CError.Compare(ss.Count, 4, "Count of SchemaSet not matched!");
                return;
            }

            Assert.True(false);
        }

        //[Variation(Desc = "v24 - Bug 338038 - Change type of xml:lang to decimal in custome xml namespace System.Xml.Tests", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v24()
        {
            string attName = "lang";
            string newtype = "decimal";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3.xsd"));
            ss.Compile();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3a.xsd"));
            ss.Compile();

            CError.Compare(ss.Count, 4, "Count of SchemaSet not matched!");

            foreach (XmlSchemaAttribute a in ss.GlobalAttributes.Values)
            {
                if (a.QualifiedName.Name == attName)
                {
                    CError.Compare(a.AttributeSchemaType.QualifiedName.Name, newtype, "Incorrect type for xml:lang");
                    return;
                }
            }

            Assert.True(false);
        }

        //[Variation(Desc = "v25 - Bug 338038 - Conflicting definitions for xml attributes in two schemas", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v25()
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3.xsd"));
            ss.Compile();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3a.xsd"));
            ss.Compile();
            try
            {
                ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3b.xsd"));
                ss.Compile();
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
                CError.Compare(ss.Count, 6, "Count of SchemaSet not matched!");
                return;
            }

            Assert.True(false);
        }

        //[Variation(Desc = "v26 - Bug 338038 - Change type of xml:lang to decimal and xml:base to short in two steps", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v26()
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3.xsd"));
            ss.Compile();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v4a.xsd"));
            ss.Compile();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v4b.xsd"));
            ss.Compile();

            foreach (XmlSchemaAttribute a in ss.GlobalAttributes.Values)
            {
                if (a.QualifiedName.Name == "lang")
                {
                    CError.Compare(a.AttributeSchemaType.QualifiedName.Name, "decimal", "Incorrect type for xml:lang");
                }
                if (a.QualifiedName.Name == "base")
                {
                    CError.Compare(a.AttributeSchemaType.QualifiedName.Name, "short", "Incorrect type for xml:base");
                }
            }

            CError.Compare(ss.Count, 6, "Count of SchemaSet not matched!");
            return;
        }

        //[Variation(Desc = "v27 - Bug 338038 - Add new attributes to the already present xml namespace System.Xml.Tests", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v27()
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3.xsd"));
            ss.Compile();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v4a.xsd"));
            ss.Compile();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v5b.xsd"));
            ss.Compile();

            foreach (XmlSchemaAttribute a in ss.GlobalAttributes.Values)
            {
                if (a.QualifiedName.Name == "blah")
                {
                    CError.Compare(a.AttributeSchemaType.QualifiedName.Name, "int", "Incorrect type for xml:lang");
                }
            }
            CError.Compare(ss.Count, 6, "Count of SchemaSet not matched!");
            return;
        }

        //[Variation(Desc = "v28 - Bug 338038 - Add new attributes to the already present xml namespace System.Xml.Tests, remove default ns schema", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v28()
        {
            string xmlns = @"http://www.w3.org/XML/1998/namespace";
            XmlSchema schema = null;

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v3.xsd"));
            ss.Compile();

            foreach (XmlSchema s in ss.Schemas(xmlns))
            {
                schema = s;
            }

            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v4a.xsd"));
            ss.Compile();
            ss.Add(null, Path.Combine(TestData._Root, "bug338038_v5b.xsd"));
            ss.Compile();

            ss.Remove(schema);
            ss.Compile();

            foreach (XmlSchemaAttribute a in ss.GlobalAttributes.Values)
            {
                if (a.QualifiedName.Name == "blah")
                {
                    CError.Compare(a.AttributeSchemaType.QualifiedName.Name, "int", "Incorrect type for xml:lang");
                }
            }
            CError.Compare(ss.Count, 5, "Count of SchemaSet not matched!");
            return;
        }

        //Regressions - Bug Fixes
        public void Callback1(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                _output.WriteLine("WARNING Recieved");
                bWarningCallback = true;
                warningCount++;
                CError.Compare(args.Exception.InnerException == null, false, "Inner Exception not set");
            }
        }

        //[Variation(Desc = "v100 - Bug 320502 - XmlSchemaSet: while throwing a warning for invalid externals we do not set the inner exception", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v100()
        {
            string xsd = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:include schemaLocation='bogus'/></xs:schema>";
            Initialize();
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(Callback1);
            ss.Add(null, new XmlTextReader(new StringReader(xsd)));
            ss.Compile();
            CError.Compare(warningCount, 1, "Warning Count mismatch!");
            return;
        }

        //[Variation(Desc = "v101 - Bug 339706 - XmlSchemaSet: Compile on the set fails when a compiled schema containing notation is already present", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v101()
        {
            string xsd1 = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:notation name='a' public='a'/></xs:schema>";
            string xsd2 = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='root'/></xs:schema>";

            Initialize();
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(Callback1);
            ss.Add(null, new XmlTextReader(new StringReader(xsd1)));
            ss.Compile();
            ss.Add(null, new XmlTextReader(new StringReader(xsd2)));
            ss.Compile();
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "v102 - Bug 337850 - XmlSchemaSet: Type already declared error when redefined schema is added to the set before the redefining schema.", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v102()
        {
            Initialize();
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss.Add(null, Path.Combine(TestData._Root, "schZ013c.xsd"));
            ss.Add(null, Path.Combine(TestData._Root, "schZ013a.xsd"));
            ss.Compile();
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "v104 - CodeCoverage- XmlSchemaSet: add precompiled subs groups, global elements, attributes and types to another compiled SOM.", Priority = 1, Params = new object[] { false })]
        [InlineData(false)]
        //[Variation(Desc = "v103 - CodeCoverage- XmlSchemaSet: add precompiled subs groups, global elements, attributes and types to another compiled set.", Priority = 1, Params = new object[] { true })]
        [InlineData(true)]
        [Theory]
        public void v103(object param0)
        {
            bool addset = (bool)param0;

            Initialize();
            XmlSchemaSet ss1 = new XmlSchemaSet();
            ss1.XmlResolver = new XmlUrlResolver();
            ss1.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss1.Add(null, Path.Combine(TestData._Root, "Misc103_x.xsd"));
            ss1.Compile();

            CError.Compare(ss1.Count, 1, "Schema Set 1 Count mismatch!");

            XmlSchemaSet ss2 = new XmlSchemaSet();
            ss2.XmlResolver = new XmlUrlResolver();
            ss2.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema s = ss2.Add(null, Path.Combine(TestData._Root, "Misc103_a.xsd"));
            ss2.Compile();

            CError.Compare(ss1.Count, 1, "Schema Set 1 Count mismatch!");

            if (addset)
            {
                ss1.Add(ss2);

                CError.Compare(ss1.GlobalElements.Count, 7, "Schema Set 1 GlobalElements Count mismatch!");
                CError.Compare(ss1.GlobalAttributes.Count, 2, "Schema Set 1 GlobalAttributes Count mismatch!");
                CError.Compare(ss1.GlobalTypes.Count, 6, "Schema Set 1 GlobalTypes Count mismatch!");
            }
            else
            {
                ss1.Add(s);

                CError.Compare(ss1.GlobalElements.Count, 2, "Schema Set 1 GlobalElements Count mismatch!");
                CError.Compare(ss1.GlobalAttributes.Count, 0, "Schema Set 1 GlobalAttributes Count mismatch!");
                CError.Compare(ss1.GlobalTypes.Count, 2, "Schema Set 1 GlobalTypes Count mismatch!");
            }

            /***********************************************/

            XmlSchemaSet ss3 = new XmlSchemaSet();
            ss3.XmlResolver = new XmlUrlResolver();
            ss3.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss3.Add(null, Path.Combine(TestData._Root, "Misc103_c.xsd"));
            ss3.Compile();
            ss1.Add(ss3);

            CError.Compare(ss1.GlobalElements.Count, 8, "Schema Set 1 GlobalElements Count mismatch!");

            return;
        }

        //[Variation(Desc = "v103 - Reference to a component from no namespace System.Xml.Tests an explicit import of no namespace System.Xml.Tests throw a validation warning", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v105()
        {
            Initialize();
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.XmlResolver = new XmlUrlResolver();
            schemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            schemaSet.Add(null, Path.Combine(TestData._Root, "Misc105.xsd"));
            CError.Compare(warningCount, 1, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "v106 - Adding a compiled SoS(schema for schema) to a set causes type collision error", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v106()
        {
            Initialize();

            XmlSchemaSet ss1 = new XmlSchemaSet();
            ss1.XmlResolver = new XmlUrlResolver();
            ss1.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlReaderSettings settings = new XmlReaderSettings();
#pragma warning disable 0618
            settings.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader r = XmlReader.Create(Path.Combine(TestData._Root, "XMLSchema.xsd"), settings);
            ss1.Add(null, r);
            ss1.Compile();

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

            foreach (XmlSchema s in ss1.Schemas())
            {
                ss.Add(s);
            }

            ss.Add(null, Path.Combine(TestData._Root, "xsdauthor.xsd"));
            ss.Compile();
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "v107 - XsdValidatingReader: InnerException not set on validation warning of a schemaLocation not loaded.", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v107()
        {
            string strXml = @"<root xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='a bug356711_a.xsd' xmlns:a='a'></root>";
            Initialize();
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.XmlResolver = new XmlUrlResolver();
            schemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            schemaSet.Add(null, Path.Combine(TestData._Root, "bug356711_root.xsd"));

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = new XmlUrlResolver();
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.Schemas.Add(schemaSet);
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            settings.ValidationType = ValidationType.Schema;
            XmlReader vr = XmlReader.Create(new StringReader(strXml), settings);

            while (vr.Read()) ;

            CError.Compare(warningCount, 1, "Warning Count mismatch!");
            CError.Compare(WarningInnerExceptionSet, true, "Inner Exception not set!");
            return;
        }

        //[Variation(Desc = "v108 - XmlSchemaSet.Add() should not trust compiled state of the schema being added", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v108()
        {
            string strSchema1 = @"
<xs:schema targetNamespace='http://bar'
           xmlns='http://bar' xmlns:x='http://foo'
           elementFormDefault='qualified'
           attributeFormDefault='unqualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:import namespace='http://foo'/>
  <xs:element name='bar'>
    <xs:complexType>
      <xs:sequence>
        <xs:element ref='x:foo'/>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>
";
            string strSchema2 = @"<xs:schema targetNamespace='http://foo'
           xmlns='http://foo' xmlns:x='http://bar'
           elementFormDefault='qualified'
           attributeFormDefault='unqualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:import namespace='http://bar'/>
  <xs:element name='foo'>
    <xs:complexType>
      <xs:sequence>
        <xs:element ref='x:bar'/>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";

            Initialize();
            XmlSchemaSet set = new XmlSchemaSet();
            set.XmlResolver = new XmlUrlResolver();
            ValidationEventHandler handler = new ValidationEventHandler(ValidationCallback);
            set.ValidationEventHandler += handler;
            XmlSchema s1 = null;
            using (XmlReader r = XmlReader.Create(new StringReader(strSchema1)))
            {
                s1 = XmlSchema.Read(r, handler);
                set.Add(s1);
            }
            set.Compile();

            // Now load set 2
            set = new XmlSchemaSet();
            set.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema s2 = null;
            using (XmlReader r = XmlReader.Create(new StringReader(strSchema2)))
            {
                s2 = XmlSchema.Read(r, handler);
            }
            XmlSchemaImport import = (XmlSchemaImport)s2.Includes[0];
            import.Schema = s1;
            import = (XmlSchemaImport)s1.Includes[0];
            import.Schema = s2;
            set.Add(s1);
            set.Reprocess(s1);
            set.Add(s2);
            set.Reprocess(s2);
            set.Compile();

            s2 = null;
            using (XmlReader r = XmlReader.Create(new StringReader(strSchema2)))
            {
                s2 = XmlSchema.Read(r, handler);
            }
            set = new XmlSchemaSet();
            set.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            import = (XmlSchemaImport)s2.Includes[0];
            import.Schema = s1;
            import = (XmlSchemaImport)s1.Includes[0];
            import.Schema = s2;
            set.Add(s1);
            set.Reprocess(s1);
            set.Add(s2);
            set.Reprocess(s2);
            set.Compile();
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 1, "Error Count mismatch");
            return;
        }

        //[Variation(Desc = "v109 - 386243, Adding a chameleon schema against to no namespace throws unexpected warnings", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v109()
        {
            Initialize();
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss.Add("http://EmployeeTest.org", Path.Combine(TestData._Root, "EmployeeTypes.xsd"));
            ss.Add(null, Path.Combine(TestData._Root, "EmployeeTypes.xsd"));

            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");

            return;
        }

        //[Variation(Desc = "v110 - 386246,  ArgumentException 'item arleady added' error on a chameleon add done twice", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v110()
        {
            Initialize();
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema s1 = ss.Add("http://EmployeeTest.org", Path.Combine(TestData._Root, "EmployeeTypes.xsd"));
            XmlSchema s2 = ss.Add("http://EmployeeTest.org", Path.Combine(TestData._Root, "EmployeeTypes.xsd"));

            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");

            return;
        }

        //[Variation(Desc = "v111 - 380805,  Chameleon include compiled in one set added to another", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v111()
        {
            Initialize();

            XmlSchemaSet newSet = new XmlSchemaSet();
            newSet.XmlResolver = new XmlUrlResolver();
            newSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema chameleon = newSet.Add(null, Path.Combine(TestData._Root, "EmployeeTypes.xsd"));
            newSet.Compile();

            CError.Compare(newSet.GlobalTypes.Count, 10, "GlobalTypes count mismatch!");

            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            sc.Add(chameleon);
            sc.Add(null, Path.Combine(TestData._Root, "baseEmployee.xsd"));
            sc.Compile();

            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");

            return;
        }

        //[Variation(Desc = "v112 - 382035,  schema set tables not cleared as expected on reprocess", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v112()
        {
            Initialize();

            XmlSchemaSet set2 = new XmlSchemaSet();
            set2.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema includedSchema = set2.Add(null, Path.Combine(TestData._Root, "bug382035a1.xsd"));
            set2.Compile();

            XmlSchemaSet set = new XmlSchemaSet();
            set.XmlResolver = new XmlUrlResolver();
            set.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema mainSchema = set.Add(null, Path.Combine(TestData._Root, "bug382035a.xsd"));
            set.Compile();

            XmlReader r = XmlReader.Create(Path.Combine(TestData._Root, "bug382035a1.xsd"));
            XmlSchema reParsedInclude = XmlSchema.Read(r, new ValidationEventHandler(ValidationCallback));

            ((XmlSchemaExternal)mainSchema.Includes[0]).Schema = reParsedInclude;
            set.Reprocess(mainSchema);
            set.Compile();

            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");

            return;
        }

        //[Variation(Desc = "v113 - Set InnerException on XmlSchemaValidationException while parsing typed values", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v113()
        {
            string strXml = @"<root xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xsi:type='xs:int'>a</root>";
            Initialize();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            settings.ValidationType = ValidationType.Schema;
            XmlReader vr = XmlReader.Create(new StringReader(strXml), settings);

            while (vr.Read()) ;

            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 1, "Error Count mismatch!");
            CError.Compare(ErrorInnerExceptionSet, true, "Inner Exception not set!");
            return;
        }

        //[Variation(Desc = "v114 - XmlSchemaSet: InnerException not set on parse errors during schema compilation", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v114()
        {
            string strXsd = @"<xs:schema elementFormDefault='qualified' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
 <xs:element name='date' type='date'/>
 <xs:simpleType name='date'>
  <xs:restriction base='xs:int'>
   <xs:enumeration value='a'/>
  </xs:restriction>
 </xs:simpleType>
</xs:schema>";

            Initialize();
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            ss.Add(XmlSchema.Read(new StringReader(strXsd), new ValidationEventHandler(ValidationCallback)));

            ss.Compile();

            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 1, "Error Count mismatch!");
            CError.Compare(ErrorInnerExceptionSet, true, "Inner Exception not set!");
            return;
        }

        //[Variation(Desc = "v116 - 405327 NullReferenceExceptions while accessing obsolete properties in the SOM", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v116()
        {
#pragma warning disable 0618
            XmlSchemaAttribute attribute = new XmlSchemaAttribute();
            object attributeType = attribute.AttributeType;
            XmlSchemaElement element = new XmlSchemaElement();
            object elementType = element.ElementType;
            XmlSchemaType schemaType = new XmlSchemaType();
            object BaseSchemaType = schemaType.BaseSchemaType;
#pragma warning restore 0618
        }

        //[Variation(Desc = "v117 - 398474 InnerException not set on XmlSchemaException, when xs:pattern has an invalid regular expression", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v117()
        {
            string strXsdv117 =
            @"<?xml version='1.0' encoding='utf-8' ?>
                  <xs:schema  xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <xs:element name='doc'>
                      <xs:complexType>
                         <xs:sequence>
                            <xs:element name='value' maxOccurs='unbounded'>
                              <xs:simpleType>
                                 <xs:restriction base='xs:string'>
                                    <xs:pattern value='(?r:foo)'/>
                                 </xs:restriction>
                              </xs:simpleType>
                            </xs:element>
                         </xs:sequence>
                      </xs:complexType>
                    </xs:element>
                  </xs:schema>";

            Initialize();

            using (StringReader reader = new StringReader(strXsdv117))
            {
                XmlSchemaSet ss = new XmlSchemaSet();
                ss.XmlResolver = new XmlUrlResolver();
                ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
                ss.Add(XmlSchema.Read(reader, ValidationCallback));
                ss.Compile();
                CError.Compare(ErrorInnerExceptionSet, true, "\nInner Exception not set\n");
            }
            return;
        }

        //[Variation(Desc = "v118 - 424904 Not getting unhandled attributes on particle", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v118()
        {
            using (XmlReader r = new XmlTextReader(Path.Combine(TestData._Root, "Bug424904.xsd")))
            {
                XmlSchema s = XmlSchema.Read(r, null);
                XmlSchemaSet set = new XmlSchemaSet();
                set.XmlResolver = new XmlUrlResolver();
                set.Add(s);
                set.Compile();

                XmlQualifiedName name = new XmlQualifiedName("test2", "http://foo");
                XmlSchemaComplexType test2type = s.SchemaTypes[name] as XmlSchemaComplexType;
                XmlSchemaParticle p = test2type.ContentTypeParticle;
                XmlAttribute[] att = p.UnhandledAttributes;

                Assert.False(att == null || att.Length < 1);
            }
        }

        //[Variation(Desc = "v120 - 397633 line number and position not set on the validation error for an invalid xsi:type value", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v120()
        {
            using (XmlReader schemaReader = XmlReader.Create(Path.Combine(TestData._Root, "Bug397633.xsd")))
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.XmlResolver = new XmlUrlResolver();
                sc.Add("", schemaReader);
                sc.Compile();

                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.ValidationType = ValidationType.Schema;
                readerSettings.Schemas = sc;

                using (XmlReader docValidatingReader = XmlReader.Create(Path.Combine(TestData._Root, "Bug397633.xml"), readerSettings))
                {
                    XmlDocument doc = new XmlDocument();
                    try
                    {
                        doc.Load(docValidatingReader);
                        doc.Validate(null);
                    }
                    catch (XmlSchemaValidationException ex)
                    {
                        if (ex.LineNumber == 1 && ex.LinePosition == 2 && !string.IsNullOrEmpty(ex.SourceUri))
                        {
                            return;
                        }
                    }
                }
            }
            Assert.True(false);
        }

        //[Variation(Desc = "v120a.XmlDocument.Load non-validating reader.Expect IOE.")]
        [InlineData()]
        [Theory]
        public void v120a()
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ValidationType = ValidationType.Schema;
            using (XmlReader reader = XmlReader.Create(Path.Combine(TestData._Root, "Bug397633.xml"), readerSettings))
            {
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(reader);
                    doc.Validate(null);
                }
                catch (XmlSchemaValidationException ex)
                {
                    _output.WriteLine(ex.Message);
                    return;
                }
            }
            Assert.True(false);
        }

        //[Variation(Desc = "444196: XmlReader.MoveToNextAttribute returns incorrect results")]
        [InlineData()]
        [Theory]
        public void v124()
        {
            Initialize();
            string XamlPresentationNamespace =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            string XamlToParse =
        "<pfx0:DrawingBrush TileMode=\"Tile\" Viewbox=\"foobar\" />";

            string xml =
        "	<xs:schema " +
        "		xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" +
        "		xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"" +
        "		targetNamespace=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
        "		elementFormDefault=\"qualified\" " +
        "		attributeFormDefault=\"unqualified\"" +
        "	>" +
        "" +
        "		<xs:element name=\"DrawingBrush\" type=\"DrawingBrushType\" />" +
        "" +
        "		<xs:complexType name=\"DrawingBrushType\">" +
        "			<xs:attribute name=\"Viewbox\" type=\"xs:string\" />" +
        "			<xs:attribute name=\"TileMode\" type=\"xs:string\" />" +
        "		</xs:complexType>" +
        "	</xs:schema>";

            XmlSchema schema = XmlSchema.Read(new StringReader(xml), null);
            schema.TargetNamespace = XamlPresentationNamespace;
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.XmlResolver = new XmlUrlResolver();
            schemaSet.Add(schema);
            schemaSet.Compile();

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            readerSettings.ValidationType = ValidationType.Schema;
            readerSettings.Schemas = schemaSet;

            NameTable nameTable = new NameTable();
            XmlNamespaceManager namespaces = new XmlNamespaceManager(nameTable);
            namespaces.AddNamespace("pfx0", XamlPresentationNamespace);
            namespaces.AddNamespace(string.Empty, XamlPresentationNamespace);
            XmlParserContext parserContext = new XmlParserContext(nameTable, namespaces, null, null, null, null, null, null, XmlSpace.None);

            using (XmlReader xmlReader = XmlReader.Create(new StringReader(XamlToParse), readerSettings, parserContext))
            {
                xmlReader.Read();
                xmlReader.MoveToAttribute(0);
                xmlReader.MoveToNextAttribute();
                xmlReader.MoveToNextAttribute();
                xmlReader.MoveToNextAttribute();

                xmlReader.MoveToAttribute(0);
                if (xmlReader.MoveToNextAttribute())
                    return;
            }
            Assert.True(false);
        }

        //[Variation(Desc = "615444 XmlSchema.Write ((XmlWriter)null) throws InvalidOperationException instead of ArgumentNullException")]
        [Fact]
        public void v125()
        {
            XmlSchema xs = new XmlSchema();
            try
            {
                xs.Write((XmlWriter)null);
            }
            catch (InvalidOperationException) { return; }
            Assert.True(false);
        }

        //[Variation(Desc = "Dev10_40561 Redefine Chameleon: Unexpected qualified name on local particle")]
        [InlineData()]
        [Theory]
        public void Dev10_40561()
        {
            Initialize();
            string xml = @"<?xml version='1.0' encoding='utf-8'?><e1 xmlns='ns-a'>  <c23 xmlns='ns-b'/></e1>";
            XmlSchemaSet set = new XmlSchemaSet();
            set.XmlResolver = new XmlUrlResolver();
            string path = Path.Combine(TestData.StandardPath, "xsd10", "SCHEMA", "schN11_a.xsd");
            set.Add(null, path);
            set.Compile();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = set;

            using (XmlReader reader = XmlReader.Create(new StringReader(xml), settings))
            {
                try
                {
                    while (reader.Read()) ;
                    _output.WriteLine("XmlSchemaValidationException was not thrown");
                    Assert.True(false);
                }
                catch (XmlSchemaValidationException e) { _output.WriteLine(e.Message); }
            }
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        // Test failure on ILC: Test depends on Xml Serialization and requires reflection on a LOT of types under System.Xml.Schema namespace.
        // Rd.xml with "<Namespace Name="System.Xml.Schema" Dynamic="Required Public" />" lets this test pass but we should probably be
        // fixing up XmlSerializer's own rd.xml rather than the test here.
        [Fact]
        public void GetBuiltinSimpleTypeWorksAsEcpected()
        {
            Initialize();
            string xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" + Environment.NewLine +
 "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
 "  <xs:simpleType>" + Environment.NewLine +
 "    <xs:restriction base=\"xs:anySimpleType\" />" + Environment.NewLine +
 "  </xs:simpleType>" + Environment.NewLine +
 "</xs:schema>";
            XmlSchema schema = new XmlSchema();
            XmlSchemaSimpleType stringType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String);
            schema.Items.Add(stringType);
            StringWriter sw = new StringWriter();
            schema.Write(sw);
            CError.Compare(sw.ToString(), xml, "Mismatch");
            return;
        }

        //[Variation(Desc = "Dev10_40509 Assert and NRE when validate the XML against the XSD")]
        [InlineData()]
        [Theory]
        public void Dev10_40509()
        {
            Initialize();
            string xml = Path.Combine(TestData._Root, "bug511217.xml");
            string xsd = Path.Combine(TestData._Root, "bug511217.xsd");
            XmlSchemaSet s = new XmlSchemaSet();
            s.XmlResolver = new XmlUrlResolver();
            XmlReader r = XmlReader.Create(xsd);
            s.Add(null, r);
            s.Compile();
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            using (XmlReader docValidatingReader = XmlReader.Create(xml, rs))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(docValidatingReader);
                doc.Schemas = s;
                doc.Validate(null);
            }
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "Dev10_40511 XmlSchemaSet::Compile throws XmlSchemaException for valid schema")]
        [InlineData()]
        [Theory]
        public void Dev10_40511()
        {
            Initialize();
            string xsd = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
<xs:simpleType name='textType'>
    <xs:restriction base='xs:string'>
      <xs:minLength value='1' />
    </xs:restriction>
  </xs:simpleType>
  <xs:simpleType name='statusCodeType'>
    <xs:restriction base='textType'>
      <xs:length value='6' />
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            sc.Add("xs", XmlReader.Create(new StringReader(xsd)));
            sc.Compile();
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "Dev10_40495 Undefined ComplexType error when loading schemas from in memory strings")]
        [InlineData()]
        [Theory]
        public void Dev10_40495()
        {
            Initialize();
            const string schema1Str = @"<xs:schema xmlns:tns=""http://BizTalk_Server_Project2.Schema1"" xmlns:b=""http://schemas.microsoft.com/BizTalk/2003"" attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" targetNamespace=""http://BizTalk_Server_Project2.Schema1"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:include schemaLocation=""S3"" />
  <xs:include schemaLocation=""S2"" />
  <xs:element name=""Root"">
    <xs:complexType>
      <xs:sequence>
        <xs:element name=""FxTypeElement"">
          <xs:complexType>
            <xs:complexContent mixed=""false"">
              <xs:extension base=""tns:FxType"">
                <xs:attribute name=""Field"" type=""xs:string"" />
              </xs:extension>
            </xs:complexContent>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";

            const string schema2Str = @"<xs:schema xmlns:b=""http://schemas.microsoft.com/BizTalk/2003"" attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:complexType name=""FxType"">
    <xs:attribute name=""Fx2"" type=""xs:string"" />
  </xs:complexType>
</xs:schema>";

            const string schema3Str = @"<xs:schema xmlns:b=""http://schemas.microsoft.com/BizTalk/2003"" attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:complexType name=""TestType"">
    <xs:attribute name=""Fx2"" type=""xs:string"" />
  </xs:complexType>
</xs:schema>";
            XmlSchema schema1 = XmlSchema.Read(new StringReader(schema1Str), null);
            XmlSchema schema2 = XmlSchema.Read(new StringReader(schema2Str), null);
            XmlSchema schema3 = XmlSchema.Read(new StringReader(schema3Str), null);

            //schema1 has some xs:includes in it. Since all schemas are string based, XmlSchema on its own cannot load automatically
            //load these included schemas. We will resolve these schema locations schema1 and make them point to the correct
            //in memory XmlSchema objects
            ((XmlSchemaExternal)schema1.Includes[0]).Schema = schema3;
            ((XmlSchemaExternal)schema1.Includes[1]).Schema = schema2;

            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.XmlResolver = new XmlUrlResolver();
            schemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

            if (schemaSet.Add(schema1) != null)
            {
                //This compile will complain about Undefined complex Type tns:FxType and schemaSet_ValidationEventHandler will be
                //called with this error.
                schemaSet.Compile();
                schemaSet.Reprocess(schema1);
            }
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "Dev10_64765 XmlSchemaValidationException.SourceObject is always null when using XPathNavigator.CheckValidity method")]
        [InlineData()]
        [Theory]
        public void Dev10_64765()
        {
            Initialize();
            string xsd =
                "<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>" +
                    "<xsd:element name='some'>" +
                    "</xsd:element>" +
                "</xsd:schema>";
            string xml = "<root/>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            ValidateXPathNavigator(xml, CompileSchemaSet(xsd));

            return;
        }

        private void ValidateXPathNavigator(string xml, XmlSchemaSet schemaSet)
        {
            XPathDocument doc = new XPathDocument(new StringReader(xml));
            XPathNavigator nav = doc.CreateNavigator();
            ValidateXPathNavigator(nav, schemaSet);
        }

        private void ValidateXPathNavigator(XPathNavigator nav, XmlSchemaSet schemaSet)
        {
            _output.WriteLine(nav.CheckValidity(schemaSet, OnValidationEvent) ? "Validation succeeded." : "Validation failed.");
        }

        private XmlSchemaSet CompileSchemaSet(string xsd)
        {
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.XmlResolver = new XmlUrlResolver();
            schemaSet.Add(XmlSchema.Read(new StringReader(xsd), OnValidationEvent));
            schemaSet.ValidationEventHandler += OnValidationEvent;
            schemaSet.Compile();
            return schemaSet;
        }

        private void OnValidationEvent(object sender, ValidationEventArgs e)
        {
            XmlSchemaValidationException exception = e.Exception as XmlSchemaValidationException;

            if (exception == null || exception.SourceObject == null)
            {
                CError.Compare(exception != null, "exception == null");
                CError.Compare(exception.SourceObject != null, "SourceObject == null");
                return;
            }
            if (!PlatformDetection.IsNetNative) // Cannot get names of internal framework types
            {
                CError.Compare(exception.SourceObject.GetType().ToString(), "MS.Internal.Xml.Cache.XPathDocumentNavigator", "SourceObject.GetType");
            }
            _output.WriteLine("Exc: " + exception);
        }

        //[Variation(Desc = "Dev10_40563 XmlSchemaSet: Assert Failure with Chk Build.")]
        [InlineData()]
        [Theory]
        public void Dev10_40563()
        {
            Initialize();
            string xsd =
                "<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>" +
                    "<xsd:element name='some'>" +
                    "</xsd:element>" +
                "</xsd:schema>";
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            ss.Add("http://www.w3.org/2001/XMLSchema", XmlReader.Create(new StringReader(xsd)));
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas = ss;
            string input = "<root xml:space='default'/>";
            using (XmlReader r1 = XmlReader.Create(new StringReader(input), rs))
            {
                using (XmlReader r2 = XmlReader.Create(new StringReader(input), rs))
                {
                    while (r1.Read()) ;
                    while (r2.Read()) ;
                }
            }
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 0, "Error Count mismatch!");
            return;
        }

        //[Variation(Desc = "TFS_470020 Schema with substitution groups does not throw when content model is ambiguous")]
        [InlineData()]
        [Theory]
        public void TFS_470020()
        {
            Initialize();
            string xml = @"<?xml version='1.0' encoding='utf-8' ?>
            <e3>
            <e2>1</e2>
            <e2>1</e2>
            </e3>";

            string xsd = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' elementFormDefault='qualified'>
              <xs:element name='e1' type='xs:int'/>
              <xs:element name='e2' type='xs:int' substitutionGroup='e1'/>
              <xs:complexType name='t3'>
                <xs:sequence>
                  <xs:element ref='e1' minOccurs='0' maxOccurs='1'/>
                  <xs:element name='e2' type='xs:int' minOccurs='0' maxOccurs='1'/>
                </xs:sequence>
              </xs:complexType>
              <xs:element name='e3' type='t3'/>
            </xs:schema>";

            XmlSchemaSet set = new XmlSchemaSet();
            set.XmlResolver = new XmlUrlResolver();
            set.Add(null, XmlReader.Create(new StringReader(xsd)));
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            doc.Schemas = set;
            doc.Validate(ValidationCallback);
            CError.Compare(warningCount, 0, "Warning Count mismatch!");
            CError.Compare(errorCount, 1, "Error Count mismatch!");
            return;
        }
    }
}
