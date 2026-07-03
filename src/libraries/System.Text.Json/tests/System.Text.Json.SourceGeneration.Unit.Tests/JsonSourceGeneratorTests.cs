// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/92467")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotX86Process))] // https://github.com/dotnet/runtime/issues/71962
    public class GeneratorTests(ITestOutputHelper logger)
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
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            // Should find the generated type.
            Assert.Equal(2, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::HelloWorld.MyType");
            result.AssertContainsType("int");
        }

        [Fact]
        public void NoWarningsDueToObsoleteMembers()
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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void NoErrorsWhenUsingReservedCSharpKeywords()
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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            // Should find the generated type.
            Assert.Equal(3, result.AllGeneratedTypes.Count());
            result.AssertContainsType("(string, string, int)");
            result.AssertContainsType("string");
            result.AssertContainsType("int");
        }

        [Fact]
        public void NoErrorsWhenUsingTypesWithMultipleEqualsOperators()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/103515
            string source = """
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    public class Foo
                    {
                        public override bool Equals(object obj) => false;
                
                        public static bool operator ==(Foo left, Foo right) => false;
                        public static bool operator !=(Foo left, Foo right) => false;
                    
                        public static bool operator ==(Foo left, string right) => false;
                        public static bool operator !=(Foo left, string right) => false;
                    
                        public override int GetHashCode() => 1;
                    }

                    [JsonSourceGenerationOptions(WriteIndented = true)]
                    [JsonSerializable(typeof(Foo))]
                    internal partial class JsonSourceGenerationContext : JsonSerializerContext
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void NoErrorsWhenUsingIgnoredReservedCSharpKeywords()
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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

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
#if ROSLYN4_0_OR_GREATER && NET
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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

#if ROSLYN4_4_OR_GREATER && NET
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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
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
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void InitOnlyPropertyWithReservedKeywordName_CompilesSuccessfully()
        {
            // Verbatim identifiers like @else should be correctly handled in property initializers.

            string source = """
                using System.Text.Json.Serialization;

                public class MyClass
                {
                    public string @else { get; init; }
                }

                [JsonSerializable(typeof(MyClass))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void RefStructPropertyWithJsonIgnore_CompilesSuccessfully()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/98590

            string source = """
                using System;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    [JsonIgnore]
                    public ReadOnlySpan<char> Values => "abc".AsSpan();
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

#if ROSLYN4_4_OR_GREATER && NET
        [Fact]
        public void PropertyWithExperimentalType_JsonIgnore_CompilesSuccessfully()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP001")]
                public class ExperimentalType
                {
                    public int Value { get; set; }
                }

                public class MyPoco
                {
                    [JsonIgnore]
                #pragma warning disable EXP001
                    public ExperimentalType ExpType { get; set; }
                #pragma warning restore EXP001
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void PocoWithExperimentalProperty_JsonIgnore_CompilesSuccessfully()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    [Experimental("EXP001"), JsonIgnore]
                    public int ExperimentalProperty { get; set; }
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Theory]
        [MemberData(nameof(ExperimentalSuppressionSources))]
        public void ExperimentalMemberUsage_IsSuppressedByDefault(string scenario, string source)
        {
            _ = scenario;
            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void ExperimentalConverterTypeArgument_IsSuppressed()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                [Experimental("EXP_TEST")]
                public class MyExperimentalType { }

                public class MyConverter<T> : JsonConverter<object>
                {
                    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;
                    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) { }
                }

                public class MyPoco
                {
                    #pragma warning disable EXP_TEST
                    [JsonConverter(typeof(MyConverter<MyExperimentalType>))]
                    #pragma warning restore EXP_TEST
                    public object Prop { get; set; }
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            // Default validation asserts the post-generator compilation is warning/error-free, which is the
            // real proof of suppression; the explicit check documents that EXP_TEST specifically is gone.
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            Assert.Empty(result.NewCompilation.GetDiagnostics().Where(d => d.Id == "EXP_TEST"));
        }

        [Fact]
        public void ExperimentalConverterConstructor_IsSuppressed()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                public class WidgetConverter : JsonConverter<ConvertedWidget>
                {
                    [Experimental("EXP_CONV_CTOR")]
                    public WidgetConverter() { }

                    public override ConvertedWidget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new ConvertedWidget();
                    public override void Write(Utf8JsonWriter writer, ConvertedWidget value, JsonSerializerOptions options) { }
                }

                [JsonConverter(typeof(WidgetConverter))]
                public class ConvertedWidget
                {
                    public int X { get; set; }
                }

                [JsonSerializable(typeof(ConvertedWidget))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            TypeGenerationSpec convertedWidget = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "ConvertedWidget");
            Assert.Equal(new[] { "EXP_CONV_CTOR" }, convertedWidget.ExperimentalDiagnosticIds);
        }

        [Fact]
        public void ExperimentalTypeClassifierFactoryConstructor_IsSuppressed()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json;
                using System.Text.Json.Serialization;
                using System.Text.Json.Serialization.Metadata;

                [JsonPolymorphic(TypeClassifier = typeof(AnimalClassifierFactory))]
                [JsonDerivedType(typeof(Dog), "dog")]
                public class Animal
                {
                    public int Age { get; set; }
                }

                public class Dog : Animal
                {
                    public int Bark { get; set; }
                }

                public sealed class AnimalClassifierFactory : JsonTypeClassifierFactory
                {
                    [Experimental("EXP_CLS_CTOR")]
                    public AnimalClassifierFactory() { }

                    public override bool CanClassify(JsonTypeClassifierContext context) => true;
                    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
                        (ref Utf8JsonReader reader) => typeof(Dog);
                }

                [JsonSerializable(typeof(Animal))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            TypeGenerationSpec animal = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "Animal");
            Assert.Equal(new[] { "EXP_CLS_CTOR" }, animal.ExperimentalDiagnosticIds);
        }

        [Fact]
        public void NestedGenericOuterTypeArgument_IsSuppressed()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP_NESTED")]
                public class MyExperimentalType
                {
                    public int Value { get; set; }
                }

                public class Outer<T>
                {
                    public class Inner
                    {
                        public int Value { get; set; }
                    }
                }

                public class MyPoco
                {
                    #pragma warning disable EXP_NESTED
                    public Outer<MyExperimentalType>.Inner Value { get; set; }
                    #pragma warning restore EXP_NESTED
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            TypeGenerationSpec myPoco = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "MyPoco");
            Assert.Equal(new[] { "EXP_NESTED" }, myPoco.ExperimentalDiagnosticIds);
        }

        [Fact]
        public void ExperimentalPropertyAccessor_IsSuppressed()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                public class WithExpAccessors
                {
                    public int GetterValue { [Experimental("EXP_GET_ACCESSOR")] get; set; }
                    public int SetterValue { get; [Experimental("EXP_SET_ACCESSOR")] set; }
                    public int UnusedSetterValue { get; [Experimental("EXP_UNUSED_SETTER")] private set; }
                }

                [JsonSerializable(typeof(WithExpAccessors))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            TypeGenerationSpec type = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "WithExpAccessors");
            Assert.Equal(new[] { "EXP_GET_ACCESSOR", "EXP_SET_ACCESSOR" }, type.ExperimentalDiagnosticIds);
        }

        [Fact]
        public void OptionsLevelExperimentalConverterTypeArgument_IsSuppressedInContextSources()
        {
            string source = """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                [Experimental("EXP_OPTIONS")]
                public class MyExperimentalType { }

                public class MyConverter<T> : JsonConverter<object>
                {
                    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;
                    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) { }
                }

                public class MyPoco
                {
                    public object Prop { get; set; }
                }

                #pragma warning disable EXP_OPTIONS
                [JsonSourceGenerationOptions(Converters = new[] { typeof(MyConverter<MyExperimentalType>) })]
                #pragma warning restore EXP_OPTIONS
                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            // EXP_OPTIONS originates from an options-level converter type argument, so it lands on the options
            // spec (not any per-type spec) and the emitter unions it into the aggregate context sources.
            Assert.Equal(new[] { "EXP_OPTIONS" }, result.ContextGenerationSpecs.Single().GeneratedOptionsSpec!.ExperimentalDiagnosticIds);
        }

        [Fact]
        public void MalformedExperimentalDiagnosticIds_AreNotEmitted()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP_VALID")]
                public class ValidExperimentalType { public int Value { get; set; } }

                [Experimental("EXP_BAD\n#error INJECTED_FROM_EXPERIMENTAL_ID")]
                public class MalformedExperimentalType { public int Value { get; set; } }

                public class MyPoco
                {
                    #pragma warning disable EXP_VALID
                    public ValidExperimentalType Valid { get; set; }
                    #pragma warning restore EXP_VALID
                    public MalformedExperimentalType Malformed { get; set; }
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger, disableDiagnosticValidation: true);

            TypeGenerationSpec myPoco = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "MyPoco");
            Assert.Equal(new[] { "EXP_VALID" }, myPoco.ExperimentalDiagnosticIds);
            Assert.Empty(result.NewCompilation.GetDiagnostics()
                .Where(d => d.GetMessage().Contains("INJECTED_FROM_EXPERIMENTAL_ID", StringComparison.Ordinal)));
        }

        public static IEnumerable<object[]> ExperimentalSuppressionSources()
        {
            // Experimental property member: the generated getter/setter is the sole usage.
            yield return new object[]
            {
                "ExperimentalProperty",
                """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    [Experimental("EXP001")]
                    public int Value { get; set; }
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Experimental field member (fields included): the generated field access is the sole usage.
            yield return new object[]
            {
                "ExperimentalField",
                """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    [Experimental("EXP002")]
                    public int Value;
                }

                [JsonSourceGenerationOptions(IncludeFields = true)]
                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Experimental property type: the user's own declaration is suppressed, leaving generated code.
            yield return new object[]
            {
                "ExperimentalPropertyType",
                """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP003")]
                public class Widget { public int X { get; set; } }

                public class MyPoco
                {
                #pragma warning disable EXP003
                    public Widget W { get; set; }
                #pragma warning restore EXP003
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Experimental serializable type itself: the user's [JsonSerializable] usage is suppressed.
            yield return new object[]
            {
                "ExperimentalSerializableType",
                """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP004")]
                public class Widget { public int X { get; set; } }

                #pragma warning disable EXP004
                [JsonSerializable(typeof(Widget))]
                #pragma warning restore EXP004
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Experimental constructor: the generated 'new Widget()' is the sole usage.
            yield return new object[]
            {
                "ExperimentalConstructor",
                """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                public class Widget
                {
                    [Experimental("EXP005")]
                    public Widget() { }
                    public int X { get; set; }
                }

                [JsonSerializable(typeof(Widget))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Polymorphic experimental derived type (dotnet/runtime#119451): the [JsonDerivedType] usage is suppressed.
            yield return new object[]
            {
                "ExperimentalDerivedType",
                """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP006")]
                public class Derived : Base { public int Y { get; set; } }

                #pragma warning disable EXP006
                [JsonDerivedType(typeof(Derived), "derived")]
                #pragma warning restore EXP006
                public class Base { public int X { get; set; } }

                [JsonSerializable(typeof(Base))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Experimental converter: the generated 'new WidgetConverter()' is suppressed.
            yield return new object[]
            {
                "ExperimentalConverter",
                """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                [Experimental("EXP007")]
                public class WidgetConverter : JsonConverter<ConvertedWidget>
                {
                    public override ConvertedWidget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new ConvertedWidget();
                    public override void Write(Utf8JsonWriter writer, ConvertedWidget value, JsonSerializerOptions options) { }
                }

                #pragma warning disable EXP007
                [JsonConverter(typeof(WidgetConverter))]
                #pragma warning restore EXP007
                public class ConvertedWidget { public int X { get; set; } }

                [JsonSerializable(typeof(ConvertedWidget))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Mixed experimental and obsolete members on the same type; both are suppressed.
            yield return new object[]
            {
                "MixedExperimentalAndObsolete",
                """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    [Experimental("EXP008")]
                    public int Experimental { get; set; }

                    [Obsolete("old")]
                    public int Obsolete { get; set; }
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };

            // Multiple distinct experimental IDs on a single type; each ID is suppressed.
            yield return new object[]
            {
                "MultipleDistinctIds",
                """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    [Experimental("EXP009")]
                    public int A { get; set; }

                    [Experimental("EXP010")]
                    public int B { get; set; }
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """
            };
        }

        [Fact]
        public void ExperimentalDiagnosticIds_AreScopedPerType()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP100")]
                public class TypeA { public int X { get; set; } }

                [Experimental("EXP200")]
                public class TypeB { public int Y { get; set; } }

                #pragma warning disable EXP100, EXP200
                [JsonSerializable(typeof(TypeA))]
                [JsonSerializable(typeof(TypeB))]
                #pragma warning restore EXP100, EXP200
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            TypeGenerationSpec typeA = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "TypeA");
            TypeGenerationSpec typeB = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "TypeB");

            // Each type's generated file suppresses only its own experimental ID.
            Assert.Equal(new[] { "EXP100" }, typeA.ExperimentalDiagnosticIds);
            Assert.Equal(new[] { "EXP200" }, typeB.ExperimentalDiagnosticIds);

            // The aggregate source files suppress the union of all per-type IDs, reconstituted by the emitter.
            // Default diagnostic validation is enabled above, so an unsuppressed EXP100/EXP200 in those files
            // (both registered types are [Experimental]) would surface as a generation error and fail this test.
        }

        [Fact]
        public void UserDefinedExperimentalAttribute_IsRecognized()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace System.Diagnostics.CodeAnalysis
                {
                    [System.AttributeUsage(System.AttributeTargets.All)]
                    internal sealed class ExperimentalAttribute : System.Attribute
                    {
                        public ExperimentalAttribute(string diagnosticId) { DiagnosticId = diagnosticId; }
                        public string DiagnosticId { get; }
                        public string UrlFormat { get; set; } = "";
                    }
                }

                [System.Diagnostics.CodeAnalysis.Experimental("EXP_USERDEF")]
                public class Widget { public int X { get; set; } }

                #pragma warning disable EXP_USERDEF
                [JsonSerializable(typeof(Widget))]
                #pragma warning restore EXP_USERDEF
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger, disableDiagnosticValidation: true);

            TypeGenerationSpec widget = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "Widget");
            Assert.Equal(new[] { "EXP_USERDEF" }, widget.ExperimentalDiagnosticIds);
        }

        [Fact]
        public void ReferencedExperimentalAttributeSymbolMismatch_IsSuppressed()
        {
            Compilation referencedCompilation = CompilationHelper.CreateReferencedExperimentalPocoWithPolyfillCompilation();
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);
            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            string source = """
                using ReferencedAssembly;
                using System.Text.Json.Serialization;

                namespace System.Diagnostics.CodeAnalysis
                {
                    [System.AttributeUsage(System.AttributeTargets.All)]
                    public sealed class ExperimentalAttribute : System.Attribute
                    {
                        public ExperimentalAttribute(string diagnosticId) { DiagnosticId = diagnosticId; }
                        public string DiagnosticId { get; }
                        public string UrlFormat { get; set; } = "";
                    }
                }

                #pragma warning disable EXP_TEST
                [JsonSerializable(typeof(ExperimentalPocoFromLib))]
                #pragma warning restore EXP_TEST
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger, disableDiagnosticValidation: true);

            TypeGenerationSpec poco = result.AllGeneratedTypes.Single(s => s.TypeRef.Name == "ExperimentalPocoFromLib");
            Assert.Equal(new[] { "EXP_TEST" }, poco.ExperimentalDiagnosticIds);
            Assert.Empty(result.NewCompilation.GetDiagnostics().Where(d => d.Id == "EXP_TEST"));
        }

        [Fact]
        public void ExperimentalSuppression_DoesNotLeakToUserCode()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                using System.Text.Json.Serialization;

                [Experimental("EXP300")]
                public class Widget { public int X { get; set; } }

                public class MyPoco
                {
                #pragma warning disable EXP300
                    public Widget W { get; set; }
                #pragma warning restore EXP300
                }

                public class Consumer
                {
                    public int Use() => new Widget().X;
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext { }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger, disableDiagnosticValidation: true);

            // The generated code suppresses its own references, but the unguarded user usage in Consumer.Use
            // must still report the diagnostic (suppression is scoped to generated files). Exactly one match
            // also confirms the generated files contributed none.
            Diagnostic diagnostic = Assert.Single(result.NewCompilation.GetDiagnostics().Where(d => d.Id == "EXP300"));
            Assert.False(diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) ?? false);
        }
#endif

        [Fact]
        public void NegativeJsonPropertyOrderGeneratesValidCode()
        {
            // Test for https://github.com/dotnet/runtime/issues/121277
            // Verify that negative JsonPropertyOrder values generate compilable code
            // even on locales that use non-ASCII minus signs (e.g., fi_FI uses U+2212)
            string source = """
                using System.Text.Json.Serialization;

                namespace Test
                {
                    public class MyClass
                    {
                        [JsonPropertyOrder(-1)]
                        public int FirstProperty { get; set; }

                        [JsonPropertyOrder(0)]
                        public int SecondProperty { get; set; }

                        [JsonPropertyOrder(-100)]
                        public int ThirdProperty { get; set; }
                    }

                    [JsonSerializable(typeof(MyClass))]
                    public partial class MyContext : JsonSerializerContext
                    {
                    }
                }
                """;

            // Test with fi_FI culture which uses U+2212 minus sign for negative numbers
            using (new ThreadCultureChange("fi-FI"))
            {
                Compilation compilation = CompilationHelper.CreateCompilation(source);
                JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

                // The generated code should compile without errors
                // If the bug exists, we'd see CS1525, CS1002, CS1056, or CS0201 errors
                var errors = result.NewCompilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToList();

                Assert.Empty(errors);
            }
        }

        [Fact]
        public void PartialContextClassWithAttributesOnMultipleDeclarations()
        {
            // Test for https://github.com/dotnet/runtime/issues/99669
            // When a JsonSerializerContext is defined across multiple partial class declarations
            // with [JsonSerializable] attributes on different declarations, the generator should
            // successfully generate code without duplicate hintName errors.

            string source1 = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    public class MyClass1
                    {
                        public int Value { get; set; }
                    }

                    public class MyClass2
                    {
                        public string Name { get; set; }
                    }

                    [JsonSerializable(typeof(MyClass1))]
                    internal partial class SerializerContext : JsonSerializerContext
                    {
                    }
                }
                """;

            string source2 = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyClass2))]
                    internal partial class SerializerContext
                    {
                    }
                }
                """;

            // Create a base compilation to get proper references (including netfx polyfill attributes).
            // File paths are explicitly set to verify the canonical partial selection (first alphabetically).
            // "File1.cs" comes before "File2.cs" alphabetically, so source1 declares the canonical partial.
            Compilation baseCompilation = CompilationHelper.CreateCompilation("");

            // Add our syntax trees with explicit file paths. Keep any existing polyfill trees from base compilation.
            var polyfillTrees = baseCompilation.SyntaxTrees.Where(t => string.IsNullOrEmpty(t.FilePath) == false || !t.ToString().Contains("namespace HelloWorld"));
            Compilation compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: baseCompilation.SyntaxTrees.Concat(new[]
                {
                    CSharpSyntaxTree.ParseText(source1, CompilationHelper.CreateParseOptions()).WithFilePath("File1.cs"),
                    CSharpSyntaxTree.ParseText(source2, CompilationHelper.CreateParseOptions()).WithFilePath("File2.cs")
                }),
                references: baseCompilation.References,
                options: (CSharpCompilationOptions)baseCompilation.Options);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            // Verify no errors from the source generator
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.Empty(errors);

            // Verify a single combined context was generated containing both types
            // (not two separate contexts, which would cause duplicate hintName errors)
            Assert.Equal(1, result.ContextGenerationSpecs.Length);
            result.AssertContainsType("global::HelloWorld.MyClass1");
            result.AssertContainsType("global::HelloWorld.MyClass2");

            // Verify the generated code compiles without errors
            var compilationErrors = result.NewCompilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.Empty(compilationErrors);
        }

        [Fact]
        public void GenericTypeWithConstrainedTypeParameters_InitOnlyProperties()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace TestApp
                {
                    public class MyBase
                    {
                        public int Id { get; set; }
                    }

                    public class ConstrainedGenericType<T> where T : notnull, MyBase
                    {
                        public T Response { get; init; }
                        public string Name { get; init; }
                    }

                    public class DerivedType : MyBase
                    {
                        public string Extra { get; set; }
                    }

                    [JsonSerializable(typeof(ConstrainedGenericType<DerivedType>))]
                    internal partial class MyContext : JsonSerializerContext { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Theory]
        [InlineData("notnull", "int")]
        [InlineData("class", "string")]
        [InlineData("class, new()", "object")]
        [InlineData("struct", "int")]
        [InlineData("unmanaged", "int")]
        [InlineData("notnull, System.IDisposable", "System.IO.MemoryStream")]
        public void GenericTypeWithConstrainedTypeParameters_VariousConstraints(string constraint, string typeArg)
        {
            string source = $$"""
                using System;
                using System.Text.Json.Serialization;

                namespace TestApp
                {
                    public class GenericWithConstraint<T> where T : {{constraint}}
                    {
                        public string Label { get; init; }
                    }

                    [JsonSerializable(typeof(GenericWithConstraint<{{typeArg}}>))]
                    internal partial class MyContext : JsonSerializerContext { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void GenericTypeWithConstrainedTypeParameters_MultipleTypeParameters()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace TestApp
                {
                    public class MultiConstraint<TKey, TValue>
                        where TKey : notnull
                        where TValue : class, new()
                    {
                        public TKey Key { get; init; }
                        public TValue Value { get; init; }
                    }

                    [JsonSerializable(typeof(MultiConstraint<int, object>))]
                    internal partial class MyContext : JsonSerializerContext { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }

        [Fact]
        public void GenericTypeWithConstrainedTypeParameters_BaseClassAndInterface()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace TestApp
                {
                    public class MyBase
                    {
                        public int Id { get; set; }
                    }

                    public class Derived : MyBase, IDisposable
                    {
                        public void Dispose() { }
                    }

                    public class GenericWithMultiConstraint<T> where T : MyBase, IDisposable
                    {
                        public T Item { get; init; }
                        public string Label { get; init; }
                    }

                    [JsonSerializable(typeof(GenericWithMultiConstraint<Derived>))]
                    internal partial class MyContext : JsonSerializerContext { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);
        }
    }
}
