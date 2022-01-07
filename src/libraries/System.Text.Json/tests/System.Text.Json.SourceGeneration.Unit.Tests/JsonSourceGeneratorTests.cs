// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    public class GeneratorTests
    {
        [Fact]
        public void TypeDiscoveryPrimitivePOCO()
        {
            string source = @"
            using System.Text.Json.Serialization;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(HelloWorld.MyType))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public class MyType
                {
                    public int PublicPropertyInt { get; set; }
                    public string PublicPropertyString { get; set; }
                    private int PrivatePropertyInt { get; set; }
                    private string PrivatePropertyString { get; set; }

                    public double PublicDouble;
                    public char PublicChar;
                    private double PrivateDouble;
                    private char PrivateChar;

                    public void MyMethod() { }
                    public void MySecondMethod() { }

                    public void UsePrivates()
                    {
                        PrivateDouble = 0;
                        PrivateChar = ' ';
                        double d = PrivateDouble;
                        char c = PrivateChar;
                    }
                }
            }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());

            Dictionary<string, Type> types = generator.GetSerializableTypes();

            // Check base functionality of found types.
            Assert.Equal(1, types.Count);
            Type myType = types["HelloWorld.MyType"];
            Assert.Equal("HelloWorld.MyType", myType.FullName);

            // Check for received fields, properties and methods in created type.
            string[] expectedPropertyNames = { "PublicPropertyInt", "PublicPropertyString", };
            string[] expectedFieldNames = { "PublicChar", "PublicDouble" };
            string[] expectedMethodNames = { "get_PrivatePropertyInt", "get_PrivatePropertyString", "get_PublicPropertyInt", "get_PublicPropertyString", "MyMethod", "MySecondMethod", "set_PrivatePropertyInt", "set_PrivatePropertyString", "set_PublicPropertyInt", "set_PublicPropertyString", "UsePrivates" };
            CheckFieldsPropertiesMethods(myType, expectedFieldNames, expectedPropertyNames, expectedMethodNames);
        }

        [Fact]
        public void TypeDiscoveryPrimitiveExternalPOCO()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLocationCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = @"
            using System.Text.Json.Serialization;
            using ReferencedAssembly;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(HelloWorld.MyType))]
                [JsonSerializable(typeof(ReferencedAssembly.Location))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public class MyType
                {
                    public int PublicPropertyInt { get; set; }
                    public string PublicPropertyString { get; set; }
                    private int PrivatePropertyInt { get; set; }
                    private string PrivatePropertyString { get; set; }

                    public double PublicDouble;
                    public char PublicChar;
                    private double PrivateDouble;
                    private char PrivateChar;

                    public void MyMethod() { }
                    public void MySecondMethod() { }
                    public void UsePrivates()
                    {
                        PrivateDouble = 0;
                        PrivateChar = ' ';
                        double x = PrivateDouble;
                        string s = PrivateChar.ToString();
                    }
                }
            }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());

            Dictionary<string, Type> types = generator.GetSerializableTypes();

            // Check base functionality of found types.
            Assert.Equal(2, types.Count);
            Type myType = types["HelloWorld.MyType"];
            Type notMyType = types["ReferencedAssembly.Location"];

            // Check for MyType.
            Assert.Equal("HelloWorld.MyType", myType.FullName);

            // Check for received fields, properties and methods for MyType.
            string[] expectedFieldNamesMyType = { "PublicChar", "PublicDouble" };
            string[] expectedPropertyNamesMyType = { "PublicPropertyInt", "PublicPropertyString" };
            string[] expectedMethodNamesMyType = { "get_PrivatePropertyInt", "get_PrivatePropertyString", "get_PublicPropertyInt", "get_PublicPropertyString", "MyMethod", "MySecondMethod", "set_PrivatePropertyInt", "set_PrivatePropertyString", "set_PublicPropertyInt", "set_PublicPropertyString", "UsePrivates" };
            CheckFieldsPropertiesMethods(myType, expectedFieldNamesMyType, expectedPropertyNamesMyType, expectedMethodNamesMyType);

            // Check for NotMyType.
            Assert.Equal("ReferencedAssembly.Location", notMyType.FullName);

            // Check for received fields, properties and methods for NotMyType.
            string[] expectedFieldNamesNotMyType = { };
            string[] expectedPropertyNamesNotMyType = { "Address1", "Address2", "City", "Country", "Id", "Name", "PhoneNumber", "PostalCode", "State" };
            string[] expectedMethodNamesNotMyType = { "get_Address1", "get_Address2", "get_City", "get_Country", "get_Id", "get_Name", "get_PhoneNumber", "get_PostalCode", "get_State",
                                                      "set_Address1", "set_Address2", "set_City", "set_Country", "set_Id", "set_Name", "set_PhoneNumber", "set_PostalCode", "set_State" };
            CheckFieldsPropertiesMethods(notMyType, expectedFieldNamesNotMyType, expectedPropertyNamesNotMyType, expectedMethodNamesNotMyType);
        }

        [Fact]
        public void TypeDiscoveryWithRenamedAttribute()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLocationCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);
            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            string source = @"
            using System.Text.Json.Serialization;
            using ReferencedAssembly;

            using @JsonSerializable = System.Runtime.Serialization.CollectionDataContractAttribute ;
            using AliasedAttribute = System.Text.Json.Serialization.JsonSerializableAttribute;

            namespace HelloWorld
            {

                [AliasedAttribute(typeof(HelloWorld.MyType))]
                [AliasedAttribute(typeof(ReferencedAssembly.Location))]
                [@JsonSerializable]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public class MyType
                {
                    public int PublicPropertyInt { get; set; }
                    public string PublicPropertyString { get; set; }
                    private int PrivatePropertyInt { get; set; }
                    private string PrivatePropertyString { get; set; }

                    public double PublicDouble;
                    public char PublicChar;
                    private double PrivateDouble;
                    private char PrivateChar;

                    public void MyMethod() { }
                    public void MySecondMethod() { }
                    public void UsePrivates()
                    {
                        PrivateDouble = 0;
                        PrivateChar = ' ';
                        double d = PrivateDouble;
                        char c = PrivateChar;
                    }
                }
            }";

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());

            Dictionary<string, Type> types = generator.GetSerializableTypes();

            // Check base functionality of found types.
            Assert.Equal(2, types.Count);

            // Check for MyType.
            Type myType = types["HelloWorld.MyType"];
            Assert.Equal("HelloWorld.MyType", myType.FullName);

            // Check for received fields, properties and methods for MyType.
            string[] expectedFieldNamesMyType = { "PublicChar", "PublicDouble" };
            string[] expectedPropertyNamesMyType = { "PublicPropertyInt", "PublicPropertyString" };
            string[] expectedMethodNamesMyType = { "get_PrivatePropertyInt", "get_PrivatePropertyString", "get_PublicPropertyInt", "get_PublicPropertyString", "MyMethod", "MySecondMethod", "set_PrivatePropertyInt", "set_PrivatePropertyString", "set_PublicPropertyInt", "set_PublicPropertyString", "UsePrivates" };
            CheckFieldsPropertiesMethods(myType, expectedFieldNamesMyType, expectedPropertyNamesMyType, expectedMethodNamesMyType);

            // Check for NotMyType.
            Type notMyType = types["ReferencedAssembly.Location"];
            Assert.Equal("ReferencedAssembly.Location", notMyType.FullName);

            // Check for received fields, properties and methods for NotMyType.
            string[] expectedFieldNamesNotMyType = { };
            string[] expectedPropertyNamesNotMyType = { "Address1", "Address2", "City", "Country", "Id", "Name", "PhoneNumber", "PostalCode", "State" };
            string[] expectedMethodNamesNotMyType = { "get_Address1", "get_Address2", "get_City", "get_Country", "get_Id", "get_Name", "get_PhoneNumber", "get_PostalCode", "get_State",
                                                      "set_Address1", "set_Address2", "set_City", "set_Country", "set_Id", "set_Name", "set_PhoneNumber", "set_PostalCode", "set_State" };
            CheckFieldsPropertiesMethods(notMyType, expectedFieldNamesNotMyType, expectedPropertyNamesNotMyType, expectedMethodNamesNotMyType);
        }

        [Theory]
        [InlineData("System.Text.Json", true)]
        [InlineData("System.Text.Json.Not", true)]
        [InlineData("System.Text.Json", false)]
        [InlineData("System.Text.Json.Not", false)]
        public static void LocalJsonSerializableAttributeExpectedShape(string assemblyName, bool includeSTJ)
        {
            string source = @"using System;
using System.Text.Json.Serialization;

namespace System.Text.Json.Serialization
{
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(string), TypeInfoPropertyName = ""Str"")]
    internal partial class JsonContext : JsonSerializerContext
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class JsonSerializableAttribute : JsonAttribute
    {
        public string TypeInfoPropertyName { get; set; }

        public JsonSerializableAttribute(Type type) { }
    }
}";

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: null, assemblyName, includeSTJ);
            JsonSourceGenerator generator = new JsonSourceGenerator();

            CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            Dictionary<string, Type> types = generator.GetSerializableTypes();
            if (includeSTJ)
            {
                Assert.Equal("System.Int32", types["System.Int32"].FullName);
                Assert.Equal("System.String", types["System.String"].FullName);
            }
            else
            {
                Assert.Null(types);
            }

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Theory]
        [InlineData("System.Text.Json", true)]
        [InlineData("System.Text.Json.Not", true)]
        [InlineData("System.Text.Json", false)]
        [InlineData("System.Text.Json.Not", false)]
        public static void LocalJsonSerializableAttributeUnexpectedShape(string assemblyName, bool includeSTJ)
        {
            string source = @"using System;
using System.Text.Json.Serialization;

[assembly: JsonSerializable(typeof(int))]

namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class JsonSerializableAttribute : JsonAttribute
    {
        public JsonSerializableAttribute(string typeInfoPropertyName, Type type) { }
    }
}";

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: null, assemblyName, includeSTJ);
            JsonSourceGenerator generator = new JsonSourceGenerator();

            CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);
            Assert.Null(generator.GetSerializableTypes());

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags,  Array.Empty<(Location, string)>());
        }

        [Fact]
        public void NameClashCompilation()
        {
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());
        }

        [Fact]
        public void CollectionDictionarySourceGeneration()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedHighLowTempsCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = @"
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;
            using ReferencedAssembly;
    
            namespace HelloWorld
            {
                [JsonSerializable(typeof(HelloWorld.WeatherForecastWithPOCOs))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public class WeatherForecastWithPOCOs
                {
                    public DateTimeOffset Date { get; set; }
                    public int TemperatureCelsius { get; set; }
                    public string Summary { get; set; }
                    public string SummaryField;
                    public List<DateTimeOffset> DatesAvailable { get; set; }
                    public Dictionary<string, HighLowTemps> TemperatureRanges { get; set; }
                    public string[] SummaryWords { get; set; }
                }
            }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Make sure compilation was successful.

            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());
        }

        [Fact]
        public void ContextTypeNotInNamespace()
        {
            string source = @"
            using System.Text.Json.Serialization;

            [JsonSerializable(typeof(MyType))]
            internal partial class JsonContext : JsonSerializerContext
            {
            }

            public class MyType
            {
                public int PublicPropertyInt { get; set; }
                public string PublicPropertyString { get; set; }
                private int PrivatePropertyInt { get; set; }
                private string PrivatePropertyString { get; set; }

                public double PublicDouble;
                public char PublicChar;
                private double PrivateDouble;
                private char PrivateChar;

                public void MyMethod() { }
                public void MySecondMethod() { }

                public void UsePrivates()
                {
                    PrivateDouble = 0;
                    PrivateChar = ' ';
                    double d = PrivateDouble;
                    char c = PrivateChar;
                }
            }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());

            Dictionary<string, Type> types = generator.GetSerializableTypes();

            // Check base functionality of found types.
            Assert.Equal(1, types.Count);
            Type myType = types["MyType"];
            Assert.Equal("MyType", myType.FullName);

            // Check for received fields, properties and methods in created type.
            string[] expectedPropertyNames = { "PublicPropertyInt", "PublicPropertyString", };
            string[] expectedFieldNames = { "PublicChar", "PublicDouble" };
            string[] expectedMethodNames = { "get_PrivatePropertyInt", "get_PrivatePropertyString", "get_PublicPropertyInt", "get_PublicPropertyString", "MyMethod", "MySecondMethod", "set_PrivatePropertyInt", "set_PrivatePropertyString", "set_PublicPropertyInt", "set_PublicPropertyString", "UsePrivates" };
            CheckFieldsPropertiesMethods(myType, expectedFieldNames, expectedPropertyNames, expectedMethodNames);
        }

        [Fact]
        public void Record()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLibRecordCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = @"
            using System.Text.Json.Serialization;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(AppRecord))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public record AppRecord(int Id)
                {
                    public string Address1 { get; set; }
                    public string Address2 { get; set; }
                    public string City { get; set; }
                    public string State { get; set; }
                    public string PostalCode { get; set; }
                    public string Name { get; set; }
                    [JsonInclude]
                    public string PhoneNumber;
                    [JsonInclude]
                    public string Country;
                }
            }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());

            Dictionary<string, Type> types = generator.GetSerializableTypes();

            // Check base functionality of found types.
            Assert.Equal(1, types.Count);
            Type recordType = types["HelloWorld.AppRecord"];
            Assert.Equal("HelloWorld.AppRecord", recordType.FullName);

            // Check for received fields, properties and methods for NotMyType.
            string[] expectedFieldsNames = { "Country", "PhoneNumber" };
            string[] expectedPropertyNames = { "Address1", "Address2", "City", "Id", "Name", "PostalCode", "State" };
            CheckFieldsPropertiesMethods(recordType, expectedFieldsNames, expectedPropertyNames);

            Assert.Equal(1, recordType.GetConstructors().Length);
        }

        [Fact]
        public void RecordInExternalAssembly()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLibRecordCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = @"
            using System.Text.Json.Serialization;
            using ReferencedAssembly;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(LibRecord))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }
            }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());

            Dictionary<string, Type> types = generator.GetSerializableTypes();

            Assert.Equal(1, types.Count);
            Type recordType = types["ReferencedAssembly.LibRecord"];
            Assert.Equal("ReferencedAssembly.LibRecord", recordType.FullName);

            string[] expectedFieldsNames = { "Country", "PhoneNumber" };
            string[] expectedPropertyNames = { "Address1", "Address2", "City", "Id", "Name", "PostalCode", "State" };
            CheckFieldsPropertiesMethods(recordType, expectedFieldsNames, expectedPropertyNames);

            Assert.Equal(1, recordType.GetConstructors().Length);
        }

        [Fact]
        public void RecordDerivedFromRecordInExternalAssembly()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedSimpleLibRecordCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = @"
            using System.Text.Json.Serialization;
            using ReferencedAssembly;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(AppRecord))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                internal record AppRecord : LibRecord
                {
                    public string ExtraData { get; set; }
                }
            }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(generatorDiags);
            CheckCompilationDiagnosticsErrors(newCompilation.GetDiagnostics());

            Dictionary<string, Type> types = generator.GetSerializableTypes();

            Assert.Equal(1, types.Count);
            Type recordType = types["HelloWorld.AppRecord"];
            Assert.Equal("HelloWorld.AppRecord", recordType.FullName);

            string[] expectedFieldsNames = { "Country", "PhoneNumber" };
            string[] expectedPropertyNames = { "Address1", "Address2", "City", "ExtraData", "Id", "Name", "PostalCode", "State" };
            CheckFieldsPropertiesMethods(recordType, expectedFieldsNames, expectedPropertyNames, inspectBaseTypes: true);

            Assert.Equal(1, recordType.GetConstructors().Length);
        }

        private void CheckCompilationDiagnosticsErrors(ImmutableArray<Diagnostic> diagnostics)
        {
            Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }

        private void CheckFieldsPropertiesMethods(
            Type type,
            string[] expectedFields,
            string[] expectedProperties,
            string[] expectedMethods = null,
            bool inspectBaseTypes = false)
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            string[] receivedFields;
            string[] receivedProperties;

            if (!inspectBaseTypes)
            {
                receivedFields = type.GetFields(bindingFlags).Select(field => field.Name).OrderBy(s => s).ToArray();
                receivedProperties = type.GetProperties(bindingFlags).Select(property => property.Name).OrderBy(s => s).ToArray();
            }
            else
            {
                List<string> fields = new List<string>();
                List<string> props = new List<string>();

                Type currentType = type;
                while (currentType != null)
                {
                    fields.AddRange(currentType.GetFields(bindingFlags).Select(property => property.Name).OrderBy(s => s).ToArray());
                    props.AddRange(currentType.GetProperties(bindingFlags).Select(property => property.Name).OrderBy(s => s).ToArray());
                    currentType = currentType.BaseType;
                }

                receivedFields = fields.ToArray();
                receivedProperties = props.ToArray();
            }
                
            string[] receivedMethods = type.GetMethods().Select(method => method.Name).OrderBy(s => s).ToArray();

            Array.Sort(receivedFields);
            Array.Sort(receivedProperties);
            Array.Sort(receivedMethods);

            Assert.Equal(expectedFields, receivedFields);
            Assert.Equal(expectedProperties, receivedProperties);

            if (expectedMethods != null)
            {
                Assert.Equal(expectedMethods, receivedMethods);
            }
        }

        // TODO: add test guarding against (de)serializing static classes.

        [Fact]
        public void TestMultipleDefinitions()
        {
            // Adding a dependency to an assembly that has internal definitions of public types
            // should not result in a collision and break generation.
            // Verify usage of the extension GetBestTypeByMetadataName(this Compilation) instead of Compilation.GetTypeByMetadataName().
            var referencedSource = @"
                namespace System.Text.Json.Serialization
                {
                    internal class JsonSerializerContext { }
                    internal class JsonSerializableAttribute { }
                    internal class JsonSourceGenerationOptionsAttribute { }
                }";

            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

            // Obtain the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            // Generate the code
            string source = @"
                using System.Text.Json.Serialization;
                namespace HelloWorld
                {
                    [JsonSerializable(typeof(HelloWorld.MyType))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class MyType
                    {
                        public int MyInt { get; set; }
                    }
                }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };
            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(
                compilation,
                out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags.Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));
            Assert.Empty(newCompilation.GetDiagnostics().Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));

            // Should find the generated type.
            Dictionary<string, Type> types = generator.GetSerializableTypes();
            Assert.Equal(1, types.Count);
            Assert.Equal("HelloWorld.MyType", types.Keys.First());
        }

        [Fact]
        public static void NoWarningsDueToObsoleteMembers()
        {
                string source = @"using System;
using System.Text.Json.Serialization;

namespace Test
{
    [JsonSerializable(typeof(ClassWithObsolete))]
    public partial class JsonContext : JsonSerializerContext { }

    public class ClassWithObsolete
    {
        [Obsolete(""This is a test"")]
        public bool Test { get; set; }
    }
}
";

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out _, generator);
            ImmutableArray<Diagnostic> generatorDiags = newCompilation.GetDiagnostics();

            // No diagnostics expected.
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }
    }
}
