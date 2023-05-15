// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    public class GeneratorTests
    {
        [Fact]
        public void TypeDiscoveryPrimitivePOCO()
        {
            string source = """
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
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());

            Assert.Equal(5, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.MyType");
        }

        [Fact]
        public void TypeDiscoveryPrimitiveExternalPOCO()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLocationCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = """
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
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());

            Assert.Equal(6, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.MyType");
            result.AssertContainsType("global::ReferencedAssembly.Location");
        }

        [Fact]
        public void TypeDiscoveryWithRenamedAttribute()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLocationCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);
            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            string source = """
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
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());

            Assert.Equal(6, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.MyType");
            result.AssertContainsType("global::ReferencedAssembly.Location");
        }

        [Theory]
        [InlineData("System.Text.Json", true)]
        [InlineData("System.Text.Json.Not", true)]
        [InlineData("System.Text.Json", false)]
        [InlineData("System.Text.Json.Not", false)]
        public static void LocalJsonSerializableAttributeExpectedShape(string assemblyName, bool includeSTJ)
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace System.Text.Json.Serialization
                {
                    [JsonSerializable(typeof(int))]
                    [JsonSerializable(typeof(string), TypeInfoPropertyName = "Str")]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                    public sealed class JsonSerializableAttribute : JsonAttribute
                    {
                        public string TypeInfoPropertyName { get; set; }

                        public JsonSerializableAttribute(Type type) { }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: null, assemblyName, includeSTJ);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            if (includeSTJ)
            {
                result.AssertContainsType("global::System.Int32");
                result.AssertContainsType("global::System.String");
            }
            else
            {
                Assert.Empty(result.AllGeneratedTypes);
            }

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Theory]
        [InlineData("System.Text.Json", true)]
        [InlineData("System.Text.Json.Not", true)]
        [InlineData("System.Text.Json", false)]
        [InlineData("System.Text.Json.Not", false)]
        public static void LocalJsonSerializableAttributeUnexpectedShape(string assemblyName, bool includeSTJ)
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                [assembly: JsonSerializable(typeof(int))]

                namespace System.Text.Json.Serialization
                {
                    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                    public sealed class JsonSerializableAttribute : JsonAttribute
                    {
                        public JsonSerializableAttribute(string typeInfoPropertyName, Type type) { }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: null, assemblyName, includeSTJ);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Empty(result.AllGeneratedTypes);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void NameClashCompilation()
        {
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());
        }

        [Fact]
        public void CollectionDictionarySourceGeneration()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedHighLowTempsCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = """
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
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.

            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());
        }

        [Fact]
        public void ContextTypeNotInNamespace()
        {
            string source = """
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
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());

            Assert.Equal(5, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::MyType");
            result.AssertContainsType("global::System.Int32");
            result.AssertContainsType("global::System.String");
            result.AssertContainsType("global::System.Double");
            result.AssertContainsType("global::System.Char");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63802", TargetFrameworkMonikers.NetFramework)]
        public void Record()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLibRecordCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = """
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
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());

            Assert.Equal(4, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.AppRecord");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63802", TargetFrameworkMonikers.NetFramework)]
        public void RecordInExternalAssembly()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLibRecordCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = """
                using System.Text.Json.Serialization;
                using ReferencedAssembly;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(LibRecord))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());

            Assert.Equal(4, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::ReferencedAssembly.LibRecord");
        }

        [Fact]
        public void RecordDerivedFromRecordInExternalAssembly()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedSimpleLibRecordCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = """
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
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            CheckCompilationDiagnosticsErrors(result.Diagnostics);
            CheckCompilationDiagnosticsErrors(result.NewCompilation.GetDiagnostics());

            Assert.Equal(4, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.AppRecord");
        }

        private void CheckCompilationDiagnosticsErrors(ImmutableArray<Diagnostic> diagnostics)
        {
            Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }

        // TODO: add test guarding against (de)serializing static classes.

        [Fact]
        public void TestMultipleDefinitions()
        {
            // Adding a dependency to an assembly that has internal definitions of public types
            // should not result in a collision and break generation.
            // Verify usage of the extension GetBestTypeByMetadataName(this Compilation) instead of Compilation.GetTypeByMetadataName().
            var referencedSource = """
                namespace System.Text.Json.Serialization
                {
                    internal class JsonSerializerContext { }
                    internal class JsonSerializableAttribute { }
                    internal class JsonSourceGenerationOptionsAttribute { }
                }
                """;

            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

            // Obtain the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            // Generate the code
            string source = """
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
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };
            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            Assert.Empty(result.Diagnostics.Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));
            Assert.Empty(result.NewCompilation.GetDiagnostics().Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));

            // Should find the generated type.
            Assert.Equal(2, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.MyType");
            result.AssertContainsType("global::System.Int32");
        }

        [Fact]
        public static void NoWarningsDueToObsoleteMembers()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace Test
                {
                    [JsonSerializable(typeof(ClassWithObsolete))]
                    public partial class JsonContext : JsonSerializerContext { }

                    public class ClassWithObsolete
                    {
                        [Obsolete("This is a test")]
                        public bool Test { get; set; }

                        [Obsolete]
                        public bool Test2 { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            ImmutableArray<Diagnostic> generatorDiags = result.NewCompilation.GetDiagnostics();

            // No diagnostics expected.
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        public static void NoErrorsWhenUsingReservedCSharpKeywords()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace Test
                {
                    [JsonSerializable(typeof(ClassWithPropertiesAndFieldsThatAreReservedKeywords))]
                    public partial class JsonContext : JsonSerializerContext { }

                    public class ClassWithPropertiesAndFieldsThatAreReservedKeywords
                    {
                        public int @class;
                        public string @event { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            ImmutableArray<Diagnostic> generatorDiags = result.NewCompilation.GetDiagnostics();

            // No diagnostics expected.
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        public static void NoErrorsWhenUsingIgnoredReservedCSharpKeywords()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace Test
                {
                    [JsonSerializable(typeof(ClassWithPropertyNameThatIsAReservedKeyword))]
                    public partial class JsonContext : JsonSerializerContext { }

                    public class ClassWithPropertyNameThatIsAReservedKeyword
                    {
                        [JsonIgnore]
                        public string @event { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            ImmutableArray<Diagnostic> generatorDiags = result.NewCompilation.GetDiagnostics();

            // No diagnostics expected.
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }
    }
}
