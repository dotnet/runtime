// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/92467")]
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

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: null, assemblyName, includeSTJ: includeSTJ);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            if (includeSTJ)
            {
                result.AssertContainsType("int");
                result.AssertContainsType("string");
            }
            else
            {
                Assert.Empty(result.AllGeneratedTypes);
            }

            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void NameClashCompilation()
        {
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            // Make sure compilation was successful.
            result.Diagnostics.AssertMaxSeverity(DiagnosticSeverity.Warning);
            result.NewCompilation.GetDiagnostics().AssertMaxSeverity(DiagnosticSeverity.Warning);
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
            CompilationHelper.RunJsonSourceGenerator(compilation);
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

            Assert.Equal(5, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::MyType");
            result.AssertContainsType("int");
            result.AssertContainsType("string");
            result.AssertContainsType("double");
            result.AssertContainsType("char");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Netfx lacks IsExternalInit class needed for records
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

            Assert.Equal(3, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.AppRecord");
            result.AssertContainsType("string");
            result.AssertContainsType("int");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Netfx lacks IsExternalInit class needed for records
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

            Assert.Equal(3, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::ReferencedAssembly.LibRecord");
            result.AssertContainsType("string");
            result.AssertContainsType("int");
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

            Assert.Equal(3, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.AppRecord");
            result.AssertContainsType("string");
            result.AssertContainsType("int");
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

            // Should find the generated type.
            Assert.Equal(2, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.MyType");
            result.AssertContainsType("int");
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
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public static void NoErrorsWhenUsingReservedCSharpKeywords()
        {
            string source = """
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
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void EquivalentTupleDeclarations_DoNotConflict()
        {
            string source = """
                using System.Text.Json.Serialization;

                #nullable enable

                namespace HelloWorld
                {
                    [JsonSerializable(typeof((string? Label1, string Label2, int Integer)))]
                    [JsonSerializable(typeof((string, string, int)))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Should find the generated type.
            Assert.Equal(3, result.AllGeneratedTypes.Count());
            result.AssertContainsType("(string, string, int)");
            result.AssertContainsType("string");
            result.AssertContainsType("int");
        }

        [Fact]
        public static void NoErrorsWhenUsingIgnoredReservedCSharpKeywords()
        {
            string source = """
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
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void UseUnderlyingTypeConverterForNullableType()
        {
            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateReferencedLocationCompilation();

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            string source = """
                using System;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                namespace Test
                {
                    [JsonSourceGenerationOptions]
                    [JsonSerializable(typeof(Sample))]
                    public partial class SourceGenerationContext : JsonSerializerContext
                    {
                    }
                    public class Sample
                    {
                        [JsonConverter(typeof(DateTimeOffsetToTimestampJsonConverter))]
                        public DateTimeOffset Start { get; set; }
                        [JsonConverter(typeof(DateTimeOffsetToTimestampJsonConverter))]
                        public DateTimeOffset? End { get; set; } // Without this property, this is fine
                    }
                    public class DateTimeOffsetToTimestampJsonConverter : JsonConverter<DateTimeOffset>
                    {
                        internal const long TicksPerMicroseconds = 10;
                        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                        {
                            var value = reader.GetInt64();
                            return new DateTimeOffset(value * TicksPerMicroseconds, TimeSpan.Zero);
                        }
                        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
                        {
                            writer.WriteNumberValue(value.Ticks / TicksPerMicroseconds);
                        }
                    }
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Equal(3, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::Test.Sample");
            result.AssertContainsType("global::System.DateTimeOffset");
            result.AssertContainsType("global::System.DateTimeOffset?");
        }

        [Fact]
        public void VariousGenericSerializableTypesAreSupported()
        {
            string source = """
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(Dictionary<string, string>))]
                    [JsonSerializable(typeof(HelloWorld.MyClass.NestedGenericClass<string>))]
                    [JsonSerializable(typeof(HelloWorld.MyGenericClass<string>.NestedClass))]
                    [JsonSerializable(typeof(HelloWorld.MyGenericClass<string>.NestedGenericClass<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class MyClass
                    {
                        public class NestedGenericClass<T>
                        {
                        }
                    }

                    public class MyGenericClass<T1>
                    {
                        public class NestedClass
                        {
                        }
                        public class NestedGenericClass<T2>
                        {
                        }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Equal(5, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::System.Collections.Generic.Dictionary<string, string>");
            result.AssertContainsType("global::HelloWorld.MyClass.NestedGenericClass<string>");
            result.AssertContainsType("global::HelloWorld.MyGenericClass<string>.NestedClass");
            result.AssertContainsType("global::HelloWorld.MyGenericClass<string>.NestedGenericClass<int>");
            result.AssertContainsType("string");
        }

        [Theory]
        [InlineData("public sealed partial class MySealedClass")]
        [InlineData("public partial class MyGenericClass<T>")]
        [InlineData("public partial interface IMyInterface")]
        [InlineData("public partial interface IMyGenericInterface<T, U>")]
        [InlineData("public partial struct MyStruct")]
        [InlineData("public partial struct MyGenericStruct<T>")]
        [InlineData("public ref partial struct MyRefStruct")]
        [InlineData("public ref partial struct MyGenericRefStruct<T>")]
        [InlineData("public readonly partial struct MyReadOnlyStruct")]
        [InlineData("public readonly ref partial struct MyReadOnlyRefStruct")]
#if ROSLYN4_0_OR_GREATER && NETCOREAPP
        [InlineData("public partial record MyRecord(int x)", LanguageVersion.CSharp10)]
        [InlineData("public partial record struct MyRecordStruct(int x)", LanguageVersion.CSharp10)]
#endif
        public void NestedContextsAreSupported(string containingTypeDeclarationHeader, LanguageVersion? languageVersion = null)
        {
            string source = $$"""
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    {{containingTypeDeclarationHeader}}
                    {
                        [JsonSerializable(typeof(MyClass))]
                        internal partial class JsonContext : JsonSerializerContext
                        {
                        }
                    }

                    public class MyClass
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source, parseOptions: CompilationHelper.CreateParseOptions(languageVersion));
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void DoesNotWarnOnNullabilityMismatch()
        {
            string source = $$"""
                using System.Collections.Generic;
                using System.Text.Json;
                using System.Text.Json.Serialization;
                #nullable enable

                namespace HelloWorld
                {
                    public static class MyClass
                    {
                        public static string Test()
                        {
                            Dictionary<int, string?> values = new();
                            return JsonSerializer.Serialize(values, JsonContext.Default.DictionaryInt32String);
                        }
                    }

                    [JsonSerializable(typeof(Dictionary<int, string>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

#if ROSLYN4_4_OR_GREATER && NETCOREAPP
        [Fact]
        public void ShadowedMemberInitializers()
        {
            string source = """
                using System.Text.Json.Serialization;

                public record Base
                {
                    public string Value { get; init; }
                }
                public record Derived : Base
                {
                    public new string Value { get; init; }
                }

                [JsonSerializable(typeof(Derived))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source, parseOptions: CompilationHelper.CreateParseOptions(LanguageVersion.CSharp11));
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }
#endif

        [Fact]
        public void FastPathWithReservedKeywordPropertyNames_CompilesSuccessfully()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/98050

            string source = """
                using System.Text.Json.Serialization;

                public class Model
                {
                    public string type { get; set; }
                    public string alias { get; set; }
                    public string @class { get; set; }
                    public string @struct { get; set; }
                }

                [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
                [JsonSerializable(typeof(Model))]
                internal partial class ModelContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }
    }
}
