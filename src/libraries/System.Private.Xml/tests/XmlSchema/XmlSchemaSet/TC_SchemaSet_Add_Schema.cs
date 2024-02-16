// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.XmlSchemaTests
{
    public class TC_SchemaSet_Add_Schema : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Add_Schema(ITestOutputHelper output)
        {
            _output = output;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - sc = null", Priority = 0)]
        [Fact]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            try
            {
                sc.Add((XmlSchema)null);
            }
            catch (ArgumentNullException)
            {
                try
                {
                    Assert.Equal(0, sc.Count);
                    Assert.False(sc.Contains((XmlSchema)null));
                }
                catch (ArgumentNullException)
                {
                    Assert.False(sc.Contains((string)null));
                    Assert.False(sc.IsCompiled);
                    return;
                }
            }
            Assert.Fail();
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - sc = valid schema")]
        [Fact]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(new StreamReader(new FileStream(TestData._FileXSD1, FileMode.Open, FileAccess.Read)), null);
            XmlSchema SchemaNew = sc.Add(Schema);

            Assert.Equal(1, sc.Count);
            Assert.True(sc.Contains(SchemaNew));
            Assert.False(sc.IsCompiled);
            Assert.True(Schema == SchemaNew);

        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - sc = valid schema, add twice, second add should be ignored.", Priority = 0)]
        [Fact]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(new StreamReader(new FileStream(TestData._FileXSD1, FileMode.Open, FileAccess.Read)), null);
            XmlSchema SchemaNew1 = sc.Add(Schema);
            XmlSchema SchemaNew2 = sc.Add(Schema);

            Assert.Equal(1, sc.Count);
            Assert.True(sc.Contains(SchemaNew1));
            Assert.True(sc.Contains(SchemaNew2));
            Assert.False(sc.IsCompiled);
            Assert.True(Schema == SchemaNew1);
            Assert.True(Schema == SchemaNew2);

            sc.Compile();
            Assert.Equal(1, sc.Count);
            Assert.True(sc.Contains(SchemaNew1));
            Assert.True(sc.Contains(SchemaNew2));
            Assert.True(sc.IsCompiled);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4 - sc = valid schema, add url for same schema, call compile", Priority = 0)]
        [Fact]
        public void v4()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(new StreamReader(new FileStream(TestData._FileXSD1, FileMode.Open, FileAccess.Read)), null);
            XmlSchema SchemaNew1 = sc.Add(Schema);
            XmlSchema SchemaNew2 = sc.Add("schema1.xsd", TestData._FileXSD1);

            // both schemas are added but they are dup
            Assert.Equal(2, sc.Count);
            Assert.True(sc.Contains(SchemaNew1));
            Assert.True(sc.Contains(SchemaNew2));
            Assert.False(sc.IsCompiled);

            // check its not the same schema as first
            Assert.True(Schema == SchemaNew1);
            Assert.False(Schema == SchemaNew2);

            try
            {
                sc.Compile();
            }
            catch (XmlSchemaException)
            {
                Assert.Equal(2, sc.Count);
                Assert.True(sc.Contains(SchemaNew1));
                Assert.True(sc.Contains(SchemaNew2));
                Assert.False(sc.IsCompiled);
                return;
            }

            Assert.Fail();
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5 - sc = schema without ns, add url for same schema with ns, call compile")]
        [Fact]
        public void v5()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(new StreamReader(new FileStream(TestData._XsdAuthor, FileMode.Open, FileAccess.Read)), null);
            XmlSchema SchemaNew1 = sc.Add(Schema);
            XmlSchema SchemaNew2 = sc.Add(null, TestData._XsdAuthorNoNs);

            // both schemas are added but they are dup
            Assert.Equal(2, sc.Count);
            Assert.True(sc.Contains(SchemaNew1));
            Assert.True(sc.Contains(SchemaNew2));
            Assert.False(sc.IsCompiled);

            // check its not the same schema as first
            Assert.True(Schema == SchemaNew1);
            Assert.False(Schema == SchemaNew2);

            sc.Compile();
            Assert.Equal(2, sc.Count);
            Assert.True(sc.Contains(SchemaNew1));
            Assert.True(sc.Contains(SchemaNew2));
            Assert.True(sc.IsCompiled);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - sc = schema without ns, add url for same schema, call compile")]
        [Fact]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(new StreamReader(new FileStream(TestData._XsdNoNs, FileMode.Open, FileAccess.Read)), null);
            XmlSchema SchemaNew1 = sc.Add(Schema);
            XmlSchema SchemaNew2 = sc.Add(null, TestData._XsdNoNs);

            // both schemas are added but they are dup
            Assert.Equal(2, sc.Count);
            Assert.True(sc.Contains(SchemaNew1));
            Assert.True(sc.Contains(SchemaNew2));
            Assert.False(sc.IsCompiled);

            // check its not the same schema as first
            Assert.True(Schema == SchemaNew1);
            Assert.False(Schema == SchemaNew2);
            try
            {
                sc.Compile();
            }
            catch (XmlSchemaException)
            {
                Assert.Equal(2, sc.Count);
                Assert.True(sc.Contains(SchemaNew1));
                Assert.True(sc.Contains(SchemaNew2));
                Assert.False(sc.IsCompiled);
                return;
            }

            Assert.Fail();
        }

        //[Variation(Desc = "v7 - 430164_import Add(XmlSchema) does not check if location already exists")]
        [Fact]
        public void v7()
        {
            Assert.Equal(0, AddSchema(Path.Combine(TestData._Root, "Bug430164_c_import.xsd"), Path.Combine(TestData._Root, "Bug430164.xsd"), 2));
        }

        //[Variation(Desc = "v8 - 430164_include Add(XmlSchema)")]
        [Fact]
        public void v8()
        {
            Assert.Equal(0, AddSchema(Path.Combine(TestData._Root, "Bug430164_b_include.xsd"), Path.Combine(TestData._Root, "Bug430164.xsd"), 1));
        }

        //[Variation(Desc = "v9 - 430164_redefine Add(XmlSchema)")]
        [Fact]
        public void v9()
        {
            Assert.Equal(0, AddSchema(Path.Combine(TestData._Root, "Bug430164_a_redefine.xsd"), Path.Combine(TestData._Root, "Bug430164.xsd"), 1));
        }

        private int AddSchema(string path1, string path2, int expCount)
        {
            XmlSchemaSet s = new XmlSchemaSet();
            s.XmlResolver = new XmlUrlResolver();

            XmlSchema aSchema = XmlSchema.Read(new XmlTextReader(path1), null);
            XmlSchema bSchema = XmlSchema.Read(new XmlTextReader(path2), null);
            Assert.Equal(0, s.Count);
            Assert.False(s.Contains(aSchema));
            Assert.False(s.Contains(bSchema));
            Assert.False(s.IsCompiled);

            s.Add(aSchema);
            Assert.Equal(s.Count, expCount);
            Assert.True(s.Contains(aSchema));
            Assert.False(s.Contains(bSchema));
            Assert.False(s.IsCompiled);

            s.Add(bSchema);
            Assert.Equal(s.Count, expCount + 1);
            Assert.True(s.Contains(aSchema));
            Assert.True(s.Contains(bSchema));
            Assert.False(s.IsCompiled);
            try
            {
                s.Compile();
                _output.WriteLine("No exception thrown");
                Assert.Fail();
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.ToString());
                Assert.Equal(s.Count, expCount + 1);
                Assert.True(s.Contains(aSchema));
                Assert.True(s.Contains(bSchema));
                Assert.False(s.IsCompiled);
            }

            return 0;
        }

        //[Variation(Desc = "525477a: XSD Redefine doesn't work")]
        [Fact]
        public void v10()
        {
            XmlCachedSchemaSetResolver resolver = new XmlCachedSchemaSetResolver();
            XmlTextReader r = new XmlTextReader(Path.Combine(TestData._Root, @"RedefineEmployee.xsd"));
            XmlSchema s = XmlSchema.Read(r, null);
            resolver.Add(new Uri(s.SourceUri), s);

            XmlTextReader r2 = new XmlTextReader(Path.Combine(TestData._Root, @"BaseEmployee2.xsd"));
            XmlSchema s2 = XmlSchema.Read(r2, null);
            resolver.Add(new Uri(s2.SourceUri), s2);

            XmlSchemaSet set = new XmlSchemaSet();
            set.ValidationEventHandler += new ValidationEventHandler(callback);
            set.XmlResolver = resolver;

            set.Add(s2);
            Assert.Equal(1, set.Count);
            Assert.True(set.Contains(s2));
            Assert.False(set.IsCompiled);

            set.Add(s);
            Assert.Equal(2, set.Count);
            Assert.True(set.Contains(s));
            Assert.False(set.IsCompiled);

            set.Compile();
            Assert.Equal(2, set.Count);
            Assert.True(set.Contains(s2));
            Assert.True(set.Contains(s));
            Assert.True(set.IsCompiled);

            XmlTextReader r3 = new XmlTextReader(Path.Combine(TestData._Root, @"BaseEmployee2.xsd"));
            XmlSchema s3 = XmlSchema.Read(r3, null);
            resolver.Add(new Uri(s3.SourceUri), s3);

            //Clear includes in S
            foreach (XmlSchemaExternal ext in s.Includes)
            {
                ext.Schema = null;
            }
            XmlSchemaSet set2 = new XmlSchemaSet();
            set2.ValidationEventHandler += new ValidationEventHandler(callback);
            set2.XmlResolver = resolver;
            set2.Add(s3);
            Assert.Equal(1, set2.Count);
            Assert.False(set2.Contains(s2));
            Assert.False(set2.Contains(s));
            Assert.True(set2.Contains(s3));
            Assert.False(set2.IsCompiled);

            set2.Add(s);
            Assert.Equal(2, set2.Count);
            Assert.False(set2.Contains(s2));
            Assert.True(set2.Contains(s));
            Assert.True(set2.Contains(s3));
            Assert.False(set2.IsCompiled);

            set2.Compile();
            Assert.Equal(2, set2.Count);
            Assert.False(set2.Contains(s2));
            Assert.True(set2.Contains(s));
            Assert.True(set2.Contains(s3));
            Assert.True(set2.IsCompiled);

            Assert.Equal(0, errorCount);
        }

        //[Variation(Desc = "525477b: XSD Redefine doesn't work")]
        [Fact]
        public void v11()
        {
            XmlCachedSchemaSetResolver resolver = new XmlCachedSchemaSetResolver();

            XmlTextReader r = new XmlTextReader(Path.Combine(TestData._Root, @"RedefineEmployee.xsd"));
            XmlSchema s = XmlSchema.Read(r, null);
            resolver.Add(new Uri(s.SourceUri), s);

            XmlTextReader r2 = new XmlTextReader(Path.Combine(TestData._Root, @"BaseEmployee2.xsd"));
            XmlSchema s2 = XmlSchema.Read(r2, null);
            resolver.Add(new Uri(s2.SourceUri), s2);

            XmlSchemaSet set = new XmlSchemaSet();
            set.XmlResolver = resolver;
            set.Add(s2);
            Assert.Equal(1, set.Count);
            Assert.True(set.Contains(s2));
            Assert.False(set.IsCompiled);

            set.Add(s);
            Assert.Equal(2, set.Count);
            Assert.True(set.Contains(s));
            Assert.False(set.IsCompiled);

            set.Compile();
            Assert.Equal(2, set.Count);
            Assert.True(set.Contains(s2));
            Assert.True(set.Contains(s));
            Assert.True(set.IsCompiled);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = set;

            using (XmlReader reader = XmlReader.Create(Path.Combine(TestData._Root, "EmployeesDefaultPrefix.xml"), settings))
            {
                while (reader.Read()) ;
            }
            XmlTextReader r3 = new XmlTextReader(Path.Combine(TestData._Root, @"BaseEmployee2.xsd"));
            XmlSchema s3 = XmlSchema.Read(r3, null);
            resolver.Add(new Uri(s3.SourceUri), s3);

            XmlSchemaSet set2 = new XmlSchemaSet();
            set2.XmlResolver = resolver;
            set2.Add(s3);
            Assert.Equal(1, set2.Count);
            Assert.False(set2.Contains(s2));
            Assert.False(set2.Contains(s));
            Assert.True(set2.Contains(s3));
            Assert.False(set2.IsCompiled);

            foreach (XmlSchemaRedefine redefine in s.Includes)
            {
                redefine.Schema = null;
            }

            set2.Add(s);
            Assert.Equal(2, set2.Count);
            Assert.False(set2.Contains(s2));
            Assert.True(set2.Contains(s));
            Assert.True(set2.Contains(s3));
            Assert.False(set2.IsCompiled);

            set2.Compile();
            Assert.Equal(2, set2.Count);
            Assert.False(set2.Contains(s2));
            Assert.True(set2.Contains(s));
            Assert.True(set2.Contains(s3));
            Assert.True(set2.IsCompiled);

            settings.Schemas = set2;

            using (XmlReader reader = XmlReader.Create(Path.Combine(TestData._Root, "EmployeesDefaultPrefix.xml"), settings))
            {
                while (reader.Read()) ;
            }
            Assert.Equal(0, errorCount);
        }

        //[Variation(Desc = "649967a.XmlSchemaSet.Reprocess() fix is changing a collection where schemas are stored")]
        [Fact]
        public void v12a()
        {
            using (XmlReader r = XmlReader.Create(Path.Combine(TestData._Root, @"bug264908_v1.xsd")))
            {
                XmlSchema s = XmlSchema.Read(r, null);
                using (XmlReader r2 = XmlReader.Create(Path.Combine(TestData._Root, @"bug264908_v1a.xsd")))
                {
                    XmlSchema s2 = XmlSchema.Read(r2, null);
                    XmlSchemaSet set = new XmlSchemaSet();

                    set.XmlResolver = null;
                    set.Add(s);
                    set.Add(s2);
                    set.Compile();

                    foreach (XmlSchema schema in set.Schemas())
                        set.Reprocess(schema);
                }
            }
        }

        public int errorCount;

        private void callback(object sender, ValidationEventArgs args)
        {
            errorCount++;
            _output.WriteLine(args.Message);
        }

        public class XmlCachedSchemaSetResolver : XmlResolver
        {
            private Dictionary<Uri, XmlSchema> schemas = new Dictionary<Uri, XmlSchema>();

            public void Add(Uri uri, XmlSchema schema)
            {
                schemas[uri] = schema;
            }

            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                if (ofObjectToReturn == null || ofObjectToReturn == typeof(XmlSchema))
                {
                    XmlSchema schema;
                    if (this.schemas.TryGetValue(absoluteUri, out schema))
                        return schema;
                }
                return null;
            }
        }
    }
}
