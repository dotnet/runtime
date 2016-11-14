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
    //[TestCase(Name = "TC_SchemaSet_Reprocess", Desc = "", Priority = 1)]
    public class TC_SchemaSet_Reprocess
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Reprocess(ITestOutputHelper output)
        {
            _output = output;
        }


        public bool bWarningCallback = false;
        public bool bErrorCallback = false;

        public void ValidateSchemaSet(XmlSchemaSet ss, int schCount, bool isCompiled, int countGT, int countGE, int countGA, string str)
        {
            _output.WriteLine(str);
            Assert.Equal(ss.Count, schCount);
            Assert.Equal(ss.IsCompiled, isCompiled);
            Assert.Equal(ss.GlobalTypes.Count, countGT);
            Assert.Equal(ss.GlobalElements.Count, countGE);
            Assert.Equal(ss.GlobalAttributes.Count, countGA);
        }

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
        //[Variation(Desc = "v1 - Reprocess: Reprocess with null", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            try
            {
                sc.Reprocess(null);
            }
            catch (ArgumentNullException e)
            {
                _output.WriteLine(e.ToString());
                CError.Compare(sc.Count, 0, "AddCount");
                CError.Compare(sc.IsCompiled, false, "AddIsCompiled");
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - Reprocess: Schema not present in set", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            try
            {
                XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
                CError.Compare(sc.Count, 1, "AddCount");
                CError.Compare(sc.Contains(Schema1), true, "AddContains");
                CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

                XmlSchema Schema2 = XmlSchema.Read(new StreamReader(new FileStream(TestData._XsdNoNs, FileMode.Open, FileAccess.Read)), null);

                sc.Compile();
                CError.Compare(sc.Count, 1, "Compile");
                CError.Compare(sc.Contains(Schema1), true, "Contains");

                sc.Reprocess(Schema2);
                CError.Compare(sc.Count, 1, "Reprocess");
                CError.Compare(sc.Contains(Schema2), true, "Contains");
            }
            catch (ArgumentException e)
            {
                _output.WriteLine(e.ToString());
                CError.Compare(sc.Count, 1, "AE");
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - Reprocess: Without changing any content of the schema", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.Contains(Schema1), true, "AddContains");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Compile");
            CError.Compare(sc.Contains(Schema1), true, "Compile Contains");

            CError.Compare(sc.IsCompiled, true, "Compile IsCompiled");
            sc.Reprocess(Schema1);
            CError.Compare(sc.Count, 1, "IsCompiled on set");
            CError.Compare(sc.IsCompiled, false, "Reprocess IsCompiled");
            CError.Compare(sc.Contains(Schema1), true, "Reprocess Contains");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4 - Reprocess: Insert a valid element", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v4()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.Contains(Schema1), true, "AddContains");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Compile Count");
            CError.Compare(sc.Contains(Schema1), true, "CompileContains");
            //edit
            XmlSchemaElement elementDog = new XmlSchemaElement();
            Schema1.Items.Add(elementDog);
            elementDog.Name = "dog";
            elementDog.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

            sc.Reprocess(Schema1);
            CError.Compare(sc.Count, 1, "ReprocessCount");
            CError.Compare(sc.Contains(Schema1), true, "ReprocessContains");
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Compile");
            CError.Compare(sc.IsCompiled, true, "CompileIsCompiled");
            CError.Compare(Schema1.IsCompiled, true, "CompileIsCompiled on SOM");
            CError.Compare(sc.Contains(Schema1), true, "CompileContains");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5 - Reprocess: Insert a valid attribute", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v5()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            CError.Compare(sc.Count, 1, "AddCount");
            CError.Compare(sc.Contains(Schema1), true, "AddContains");
            CError.Compare(sc.IsCompiled, false, "AddIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "CompileCount");
            CError.Compare(sc.Contains(Schema1), true, "CompileContains");
            //edit
            XmlSchemaAttribute attributeDog = new XmlSchemaAttribute();

            Schema1.Items.Add(attributeDog);
            attributeDog.Name = "dog";
            attributeDog.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

            sc.Reprocess(Schema1);
            CError.Compare(sc.Count, 1, "ReprocessCount");
            CError.Compare(sc.Contains(Schema1), true, "ReprocessContains");
            CError.Compare(sc.IsCompiled, false, "ReprocessIsCompiled");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count Compile");
            CError.Compare(sc.IsCompiled, true, "IsCompiled Compile");
            CError.Compare(Schema1.IsCompiled, true, "IsCompiled on SOM Compile");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - Reprocess: Insert an element with an invalid type", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v6()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
                CError.Compare(sc.Count, 1, "Count after add");
                CError.Compare(sc.Contains(Schema1), true, "Contains after add");

                sc.Compile();
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains(Schema1), true, "Contains");

                XmlSchemaElement elementDog = new XmlSchemaElement();

                Schema1.Items.Add(elementDog);
                elementDog.Name = "dog";
                elementDog.SchemaTypeName = new XmlQualifiedName("sstring", "http://www.w3.org/2001/XMLSchema");

                sc.Reprocess(Schema1);
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains(Schema1), true, "Contains");
                CError.Compare(sc.IsCompiled, false, "IsCompiled");
                sc.Compile();
                CError.Compare(sc.IsCompiled, true, "IsCompiled");
                CError.Compare(Schema1.IsCompiled, true, "IsCompiled on SOM");
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.ToString());
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v7 - Reprocess: Insert an attribute with an invalid type", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v7()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
                CError.Compare(sc.Count, 1, "Count after add");
                CError.Compare(sc.Contains(Schema1), true, "Contains after add");

                sc.Compile();
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains(Schema1), true, "Contains");

                //edit
                XmlSchemaAttribute attributeDog = new XmlSchemaAttribute();
                Schema1.Items.Add(attributeDog);
                attributeDog.Name = "dog";
                attributeDog.SchemaTypeName = new XmlQualifiedName("blah", "http://www.w3.org/2001/XMLSchema");
                sc.Reprocess(Schema1);
                CError.Compare(sc.Count, 1, "Count");
                CError.Compare(sc.Contains(Schema1), true, "Contains");
                CError.Compare(sc.IsCompiled, false, "IsCompiled");
                sc.Compile();
                CError.Compare(sc.IsCompiled, true, "IsCompiled");
                CError.Compare(Schema1.IsCompiled, true, "IsCompiled on SOM");
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.ToString());
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v8 - Reprocess: Change an import statement to unresolvable", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v8()
        {
            bWarningCallback = false;
            bErrorCallback = false;

            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

            XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, "import_v1_a.xsd"));
            CError.Compare(sc.Count, 2, "Count after add");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.Contains(Schema1), true, "Contains");

            //edit
            foreach (XmlSchemaImport imp in Schema1.Includes)
            {
                imp.SchemaLocation = "bogus";
                imp.Schema = null;
            }

            sc.Reprocess(Schema1);

            CError.Compare(bWarningCallback, true, "Warning");
            CError.Compare(bErrorCallback, false, "Error");

            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.Contains(Schema1), true, "Contains");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");

            sc.Compile();

            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(Schema1.IsCompiled, true, "IsCompiled on SOM");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v9 - Reprocess: Change an import statement added to import another schema", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v9()
        {
            bWarningCallback = false;
            bErrorCallback = false;

            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

            XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, "reprocess_v9_a.xsd"));
            CError.Compare(sc.Count, 2, "Count after add");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count");
            CError.Compare(sc.Contains(Schema1), true, "Contains");

            //edit
            XmlSchemaImport imp = new XmlSchemaImport();
            imp.Namespace = "ns-c";
            imp.SchemaLocation = "reprocess_v9_c.xsd";
            Schema1.Includes.Add(imp);

            sc.Reprocess(Schema1);

            CError.Compare(bWarningCallback, false, "Warning");
            CError.Compare(bErrorCallback, false, "Error");

            CError.Compare(sc.Count, 3, "Count");
            CError.Compare(sc.Contains(Schema1), true, "Contains");
            CError.Compare(sc.IsCompiled, false, "IsCompiled");
            sc.Compile();
            CError.Compare(sc.IsCompiled, true, "IsCompiled");
            CError.Compare(Schema1.IsCompiled, true, "IsCompiled on SOM");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v10 - Reprocess: Add an invalid import statement, Import self", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v10()
        {
            bWarningCallback = false;
            bErrorCallback = false;

            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

            XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, "reprocess_v9_a.xsd"));
            CError.Compare(sc.Count, 2, "Count after add");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add");

            sc.Compile();
            CError.Compare(sc.Count, 2, "Count after add/comp");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add/comp");
            //edit
            XmlSchemaImport imp = new XmlSchemaImport();
            imp.Namespace = "ns-a";
            imp.SchemaLocation = "reprocess_v9_a.xsd";
            Schema1.Includes.Add(imp);

            sc.Reprocess(Schema1);
            ValidateSchemaSet(sc, 2, false, 2, 1, 0, "Validation after edit/reprocess");
            CError.Compare(bWarningCallback, false, "Warning repr");
            CError.Compare(bErrorCallback, true, "Error repr");

            sc.Compile();
            ValidateSchemaSet(sc, 2, false, 2, 1, 0, "Validation after comp/reprocess");
            CError.Compare(bWarningCallback, false, "Warning comp");
            CError.Compare(bErrorCallback, true, "Error comp");
            CError.Compare(Schema1.IsCompiled, false, "IsCompiled on SOM");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v11 - Reprocess: Change Include statement to unresolvable", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v11()
        {
            bWarningCallback = false;
            bErrorCallback = false;

            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

            XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, "include_v1_a.xsd"));
            CError.Compare(sc.Count, 1, "Count after add");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count after add/comp");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add/comp");
            //edit
            foreach (XmlSchemaInclude inc in Schema1.Includes)
            {
                inc.SchemaLocation = "bogus";
                inc.Schema = null;
            }

            sc.Reprocess(Schema1);
            ValidateSchemaSet(sc, 1, false, 1, 0, 0, "Validation after edit/reprocess");
            CError.Compare(bWarningCallback, true, "Warning repr");
            CError.Compare(bErrorCallback, false, "Error repr");

            sc.Compile();
            ValidateSchemaSet(sc, 1, true, 2, 2, 0, "Validation after comp/reprocess");
            CError.Compare(bWarningCallback, true, "Warning comp");
            CError.Compare(bErrorCallback, false, "Error comp");
            CError.Compare(Schema1.IsCompiled, true, "IsCompiled on SOM");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v12 - Reprocess: Add invalid include statement", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v12()
        {
            bWarningCallback = false;
            bErrorCallback = false;

            XmlSchemaSet sc = new XmlSchemaSet();
            sc.XmlResolver = new XmlUrlResolver();

            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema Schema1 = sc.Add(null, Path.Combine(TestData._Root, "include_v1_a.xsd"));
            CError.Compare(sc.Count, 1, "Count after add");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add");

            sc.Compile();
            CError.Compare(sc.Count, 1, "Count after add/comp");
            CError.Compare(sc.Contains(Schema1), true, "Contains after add/comp");
            ///edit
            XmlSchemaInclude inc = new XmlSchemaInclude();
            inc.SchemaLocation = "include_v2.xsd";
            Schema1.Includes.Add(inc);

            sc.Reprocess(Schema1);
            ValidateSchemaSet(sc, 1, false, 1, 0, 0, "Validation after edit/reprocess");
            CError.Compare(bWarningCallback, false, "Warning repr");
            CError.Compare(bErrorCallback, true, "Error repr");

            sc.Compile();
            ValidateSchemaSet(sc, 1, false, 1, 0, 0, "Validation after comp/reprocess");
            CError.Compare(bWarningCallback, false, "Warning comp");
            CError.Compare(bErrorCallback, true, "Error comp");
            CError.Compare(Schema1.IsCompiled, false, "IsCompiled on SOM");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "Regression bug 346902 - Types should be removed of a schema when SchemaSet.Reprocess is called", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v50()
        {
            bWarningCallback = false;
            bErrorCallback = false;
            XmlSchemaSet schemaSet = new XmlSchemaSet();

            XmlSchema schema = new XmlSchema();
            schemaSet.Add(schema);
            schema.TargetNamespace = "http://myns/";

            //type
            XmlSchemaType schemaType = new XmlSchemaComplexType();
            schemaType.Name = "MySimpleType";
            schema.Items.Add(schemaType);
            schemaSet.Reprocess(schema);
            schemaSet.Compile();

            //element
            XmlSchemaElement schemaElement = new XmlSchemaElement();
            schemaElement.Name = "MyElement";
            schema.Items.Add(schemaElement);
            schemaSet.Reprocess(schema);
            schemaSet.Compile();

            //attribute
            XmlSchemaAttribute schemaAttribute = new XmlSchemaAttribute();
            schemaAttribute.Name = "MyAttribute";
            schema.Items.Add(schemaAttribute);
            schemaSet.Reprocess(schema);
            schemaSet.Compile();

            schemaSet.Reprocess(schema);
            schemaSet.Compile();

            schema.Items.Remove(schemaType);//what is the best way to remove it?
            schema.Items.Remove(schemaElement);
            schema.Items.Remove(schemaAttribute);
            schemaSet.Reprocess(schema);
            schemaSet.Compile();

            schemaType = new XmlSchemaComplexType();
            schemaType.Name = "MySimpleType";
            schema.Items.Add(schemaType);
            schema.Items.Add(schemaElement);
            schema.Items.Add(schemaAttribute);
            schemaSet.Reprocess(schema);

            schemaSet.Compile();
            CError.Compare(schemaSet.GlobalElements.Count, 1, "Element count mismatch!");
            CError.Compare(schemaSet.GlobalAttributes.Count, 1, "Attributes count mismatch!");
            CError.Compare(schemaSet.GlobalTypes.Count, 2, "Types count mismatch!");

            return;
        }

        //[Variation(Desc = "v53 Regression bug 382119 - XmlEditor:  Changes within an include is not getting picked up as expected - include", Priority = 1, Params = new object[] { "bug382119_a.xsd", "bug382119_inc.xsd", "bug382119_inc1.xsd", false })]
        [InlineData("bug382119_a.xsd", "bug382119_inc.xsd", "bug382119_inc1.xsd", false)]
        //[Variation(Desc = "v52 Regression bug 382119 - XmlEditor:  Changes within an include is not getting picked up as expected - import", Priority = 1, Params = new object[] { "bug382119_b.xsd", "bug382119_imp.xsd", "bug382119_imp1.xsd", true })]
        [InlineData("bug382119_b.xsd", "bug382119_imp.xsd", "bug382119_imp1.xsd", true)]
        //[Variation(Desc = "v51 Regression bug 382119 - XmlEditor:  Changes within an include is not getting picked up as expected - redefine", Priority = 1, Params = new object[] { "bug382119_c.xsd", "bug382119_red.xsd", "bug382119_red1.xsd", false })]
        [InlineData("bug382119_c.xsd", "bug382119_red.xsd", "bug382119_red1.xsd", false)]
        [Theory]
        public void v51(object param0, object param1, object param2, object param3)
        {
            bWarningCallback = false;
            bErrorCallback = false;

            string mainFile = Path.Combine(TestData._Root, param0.ToString());
            string include1 = Path.Combine(TestData._Root, param1.ToString());
            string include2 = Path.Combine(TestData._Root, param2.ToString());
            string xmlFile = Path.Combine(TestData._Root, "bug382119.xml");
            bool IsImport = (bool)param3;

            XmlSchemaSet set1 = new XmlSchemaSet();
            set1.XmlResolver = new XmlUrlResolver();
            set1.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema includedSchema = set1.Add(null, include1);
            set1.Compile();

            XmlSchemaSet set = new XmlSchemaSet();
            set.XmlResolver = new XmlUrlResolver();
            set.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema mainSchema = set.Add(null, mainFile);
            set.Compile();

            _output.WriteLine("First validation ***************");
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = set;
            XmlReader reader = XmlReader.Create(xmlFile, settings);
            while (reader.Read()) { }

            CError.Compare(bWarningCallback, false, "Warning count mismatch");
            CError.Compare(bErrorCallback, true, "Error count mismatch");

            if (IsImport == true)
                set.Remove(((XmlSchemaExternal)mainSchema.Includes[0]).Schema);
            _output.WriteLine("re-setting include");
            XmlSchema reParsedInclude = LoadSchema(include2, include1);
            ((XmlSchemaExternal)mainSchema.Includes[0]).Schema = reParsedInclude;

            _output.WriteLine("Calling reprocess");

            set.Reprocess(mainSchema);
            set.Compile();

            bWarningCallback = false;
            bErrorCallback = false;
            _output.WriteLine("Second validation ***************");
            settings.Schemas = set;
            reader = XmlReader.Create(xmlFile, settings);
            while (reader.Read()) { }

            CError.Compare(bWarningCallback, false, "Warning count mismatch");
            CError.Compare(bErrorCallback, false, "Error count mismatch");

            //Editing the include again
            _output.WriteLine("Re-adding include to set1");
            XmlSchema reParsedInclude2 = LoadSchema(include1, include1);
            set1.Remove(includedSchema);
            set1.Add(reParsedInclude2);
            set1.Compile();

            if (IsImport == true)
                set.Remove(((XmlSchemaExternal)mainSchema.Includes[0]).Schema);

            ((XmlSchemaExternal)mainSchema.Includes[0]).Schema = reParsedInclude2;
            set.Reprocess(mainSchema);
            set.Compile();

            bWarningCallback = false;
            bErrorCallback = false;

            _output.WriteLine("Third validation, Expecting errors ***************");
            settings.Schemas = set;
            reader = XmlReader.Create(xmlFile, settings);
            while (reader.Read()) { }

            CError.Compare(bWarningCallback, false, "Warning count mismatch");
            CError.Compare(bErrorCallback, true, "Error count mismatch");

            return;
        }

        public XmlSchema LoadSchema(string path, string baseuri)
        {
            string includeUri = Path.GetFullPath(baseuri);
            string correctUri = Path.GetFullPath(path);
            _output.WriteLine("Include uri: " + includeUri);
            _output.WriteLine("Correct uri: " + correctUri);
            Stream s = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read, 1);
            XmlReader r = XmlReader.Create(s, new XmlReaderSettings(), includeUri);
            _output.WriteLine("Reader uri: " + r.BaseURI);
            XmlSchema som = null;
            using (r)
            {
                som = XmlSchema.Read(r, new ValidationEventHandler(ValidationCallback));
            }
            return som;
        }
    }
}
