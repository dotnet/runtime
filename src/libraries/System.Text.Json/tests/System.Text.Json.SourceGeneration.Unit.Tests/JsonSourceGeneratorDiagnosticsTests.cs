// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/92467")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotX86Process))] // https://github.com/dotnet/runtime/issues/71962
    public class JsonSourceGeneratorDiagnosticsTests
    {
        /// <summary>
        /// https://github.com/dotnet/runtime/issues/61379
        /// </summary>
        [Fact]
        public void EmitsDocumentationOnPublicMembersAndDoesNotCauseCS1591()
        {
            // Compile the referenced assembly first.
            Compilation documentedCompilation = CompilationHelper.CreateReferencedModelWithFullyDocumentedProperties();

            // Emit the image of the referenced assembly.
            byte[] documentedImage = CompilationHelper.CreateAssemblyImage(documentedCompilation);

            // Main source for current compilation.
            string source = """
                using System.Collections.Generic;
                using System.Text.Json.Serialization;
                using ReferencedAssembly;

                namespace JsonSourceGenerator
                {
                    /// <summary>
                    /// Documentation
                    /// </summary>
                    [JsonSerializable(typeof(DocumentedModel))]
                    [JsonSerializable(typeof(DocumentedModel2<string>))]
                    public partial class JsonContext : JsonSerializerContext
                    {
                    }

                    /// <summary>
                    /// Documentation
                    /// </summary>
                    public class DocumentedModel2<T>
                    {
                        /// <summary>
                        /// Documentation
                        /// </summary>
                        public List<Model> Models { get; set; }
                        /// documentation
                        public T Prop { get; set; }
                    }

                    /// <summary>
                    /// Documentation
                    /// </summary>
                    public class DocumentedModel
                    {
                        /// <summary>
                        /// Documentation
                        /// </summary>
                        public List<Model> Models { get; set; }
                    }
                }
                """;

            MetadataReference[] additionalReferences = {
                MetadataReference.CreateFromImage(documentedImage),
            };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences, parseOptions: CompilationHelper.CreateParseOptions(documentationMode: DocumentationMode.Diagnose));
            JsonSourceGeneratorResult sourceGenResult = CompilationHelper.RunJsonSourceGenerator(compilation);

            using var emitStream = new MemoryStream();
            using var xmlStream = new MemoryStream();
            var result = sourceGenResult.NewCompilation.Emit(emitStream, xmlDocumentationStream: xmlStream);
        }

        [Fact]
        public void SuccessfulSourceGeneration()
        {
            // Compile the referenced assembly first.
            Compilation campaignCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();
            Compilation eventCompilation = CompilationHelper.CreateActiveOrUpcomingEventCompilation();

            // Emit the image of the referenced assembly.
            byte[] campaignImage = CompilationHelper.CreateAssemblyImage(campaignCompilation);
            byte[] eventImage = CompilationHelper.CreateAssemblyImage(eventCompilation);

            // Main source for current compilation.
            string source = """
                using System.Collections.Generic;
                using System.Text.Json.Serialization;
                using ReferencedAssembly;

                namespace JsonSourceGenerator
                {
                    [JsonSerializable(typeof(JsonSourceGenerator.IndexViewModel))]
                    public partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class IndexViewModel
                    {
                        public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
                        public CampaignSummaryViewModel FeaturedCampaign { get; set; }
                        public bool IsNewAccount { get; set; }
                        public bool HasFeaturedCampaign => FeaturedCampaign != null;
                    }
                }
                """;

            MetadataReference[] additionalReferences = {
                MetadataReference.CreateFromImage(campaignImage),
                MetadataReference.CreateFromImage(eventImage),
            };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultiDimensionArrayDoesNotProduceWarnings(bool explicitRef)
        {
            // Compile the referenced assembly first.
            Compilation campaignCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();
            Compilation eventCompilation = CompilationHelper.CreateActiveOrUpcomingEventCompilation();

            // Emit the image of the referenced assembly.
            byte[] campaignImage = CompilationHelper.CreateAssemblyImage(campaignCompilation);
            byte[] eventImage = CompilationHelper.CreateAssemblyImage(eventCompilation);

            string optionalAttribute = explicitRef ? "[JsonSerializable(typeof(ActiveOrUpcomingEvent[,]))]" : null;

            // Main source for current compilation.
            string source = $$"""
                using System.Text.Json.Serialization;
                using ReferencedAssembly;

                namespace JsonSourceGenerator
                {
                    {{optionalAttribute}}
                    [JsonSerializable(typeof(JsonSourceGenerator.IndexViewModel))]
                    public partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class IndexViewModel
                    {
                        public ActiveOrUpcomingEvent[,] ActiveOrUpcomingEvents { get; set; }
                        public CampaignSummaryViewModel FeaturedCampaign { get; set; }
                        public bool IsNewAccount { get; set; }
                        public bool HasFeaturedCampaign => FeaturedCampaign != null;
                    }
                }
                """;

            MetadataReference[] additionalReferences = {
                MetadataReference.CreateFromImage(campaignImage),
                MetadataReference.CreateFromImage(eventImage),
            };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void NameClashSourceGeneration()
        {
            // Without resolution.
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location location = compilation.GetSymbolsWithName("JsonContext").FirstOrDefault().GetAttributes()[1].GetLocation();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, location, "There are multiple types named 'Location'. Source was generated for the first one detected. Use 'JsonSerializableAttribute.TypeInfoPropertyName' to resolve this collision.")
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void ProgramsThatDontUseGeneratorCompile()
        {
            // No STJ usage.
            string source = """
                using System;

                public class Program
                {
                    public static void Main()
                    {
                        Console.WriteLine("Hello World");
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation);

            // With STJ usage.
            source = """
                using System.Text.Json;

                public class Program
                {
                    public static void Main()
                    {
                        JsonSerializer.Serialize("Hello World");
                    }
                }
                """;

            compilation = CompilationHelper.CreateCompilation(source);
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void DoNotWarnOnClassesWithInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInitOnlyProperties();
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void DoNotWarnOnClassesWithConstructorInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithConstructorInitOnlyProperties();
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void DoNotWarnOnClassesWithMixedInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithMixedInitOnlyProperties();
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void DoNotWarnOnRecordsWithInitOnlyPositionalParameters()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithRecordPositionalParameters();
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

#if ROSLYN4_4_OR_GREATER
        [Fact]
        public void DoNotWarnOnClassesWithRequiredProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithRequiredProperties();
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }
#endif

        [Fact]
        public void DoNotWarnOnClassesWithInaccessibleJsonIncludeProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInaccessibleJsonIncludeProperties();
            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Fact]
        public void PolymorphicClassWarnsOnFastPath()
        {
            Compilation compilation = CompilationHelper.CreatePolymorphicClassOnFastPathContext();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location myBaseClassLocation = compilation.GetSymbolsWithName("MyBaseClass").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, myBaseClassLocation, "Type 'HelloWorld.MyBaseClass' is annotated with 'JsonDerivedTypeAttribute' which is not supported in 'JsonSourceGenerationMode.Serialization'."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void JsonStringEnumConverterWarns()
        {
            Compilation compilation = CompilationHelper.CreateTypesAnnotatedWithJsonStringEnumConverter();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location enum2PropLocation = compilation.GetSymbolsWithName("Enum2Prop").First().GetAttributes()[0].GetLocation();
            Location enum1TypeLocation = compilation.GetSymbolsWithName("Enum1").First().GetAttributes()[0].GetLocation();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, enum1TypeLocation, "The member 'HelloWorld.Enum1' has been annotated with 'JsonStringEnumConverter' which is not supported in native AOT. Consider using the generic 'JsonStringEnumConverter<TEnum>' instead."),
                new(DiagnosticSeverity.Warning, enum2PropLocation, "The member 'HelloWorld.MyClass.Enum2Prop' has been annotated with 'JsonStringEnumConverter' which is not supported in native AOT. Consider using the generic 'JsonStringEnumConverter<TEnum>' instead."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void InvalidJsonConverterAttributeTypeWarns()
        {
            Compilation compilation = CompilationHelper.CreateTypesWithInvalidJsonConverterAttributeType();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location value1PropLocation = compilation.GetSymbolsWithName("Value1").First().GetAttributes()[0].GetLocation();
            Location value2PropLocation = compilation.GetSymbolsWithName("Value2").First().GetAttributes()[0].GetLocation();
            Location value3PropLocation = compilation.GetSymbolsWithName("Value3").First().GetAttributes()[0].GetLocation();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, value1PropLocation, "The 'JsonConverterAttribute' type 'null' specified on member 'HelloWorld.MyClass.Value1' is not a converter type or does not contain an accessible parameterless constructor."),
                new(DiagnosticSeverity.Warning, value2PropLocation, "The 'JsonConverterAttribute' type 'int' specified on member 'HelloWorld.MyClass.Value2' is not a converter type or does not contain an accessible parameterless constructor."),
                new(DiagnosticSeverity.Warning, value3PropLocation, "The 'JsonConverterAttribute' type 'HelloWorld.InacessibleConverter' specified on member 'HelloWorld.MyClass.Value3' is not a converter type or does not contain an accessible parameterless constructor."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void DerivedJsonConverterAttributeTypeWarns()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithDerivedJsonConverterAttributeAnnotations();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location typeAttrLocation = compilation.GetSymbolsWithName("ClassWithConverterDeclaration").First().GetAttributes()[0].GetLocation();
            Location jsonSerializableAttrLocation = compilation.GetSymbolsWithName("JsonContext").First().GetAttributes()[0].GetLocation();
            Location propAttrLocation = compilation.GetSymbolsWithName("Value").First().GetAttributes()[0].GetLocation();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, typeAttrLocation, "The custom attribute 'HelloWorld.MyJsonConverterAttribute' deriving from JsonConverterAttribute is not supported by the source generator."),
                new(DiagnosticSeverity.Warning, jsonSerializableAttrLocation, "Did not generate serialization metadata for type 'HelloWorld.ClassWithConverterDeclaration'."),
                new(DiagnosticSeverity.Warning, propAttrLocation, "The custom attribute 'HelloWorld.MyJsonConverterAttribute' deriving from JsonConverterAttribute is not supported by the source generator."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void UnboundGenericTypeDeclarationWarns()
        {
            Compilation compilation = CompilationHelper.CreateContextWithUnboundGenericTypeDeclarations();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            INamedTypeSymbol symbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("JsonContext").FirstOrDefault();
            Collections.Immutable.ImmutableArray<AttributeData> attributes = symbol.GetAttributes();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, attributes[0].GetLocation(), "Did not generate serialization metadata for type 'System.Collections.Generic.List<>'."),
                new(DiagnosticSeverity.Warning, attributes[1].GetLocation(), "Did not generate serialization metadata for type 'System.Collections.Generic.Dictionary<,>'."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void ErrorTypeDeclarationWarns()
        {
            Compilation compilation = CompilationHelper.CreateContextWithErrorTypeDeclarations();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            INamedTypeSymbol symbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("JsonContext").FirstOrDefault();
            Collections.Immutable.ImmutableArray<AttributeData> attributes = symbol.GetAttributes();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, attributes[0].GetLocation(), "Did not generate serialization metadata for type 'BogusType'."),
                new(DiagnosticSeverity.Warning, attributes[1].GetLocation(), "Did not generate serialization metadata for type 'BogusType<int>'."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Theory]
        [InlineData(LanguageVersion.Default)]
        [InlineData(LanguageVersion.Preview)]
        [InlineData(LanguageVersion.Latest)]
        [InlineData(LanguageVersion.LatestMajor)]
        [InlineData(LanguageVersion.CSharp9)]
#if ROSLYN4_4_OR_GREATER
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
#endif
        public void SupportedLanguageVersions_SucceedCompilation(LanguageVersion langVersion)
        {
            string source = """
                    using System.Text.Json.Serialization;

                    namespace HelloWorld
                    {
                        public class MyClass
                        {
                            public MyClass(int value)
                            {
                                Value = value;
                            }

                            public int Value { get; set; }
                        }

                        [JsonSerializable(typeof(MyClass))]
                        public partial class MyJsonContext : JsonSerializerContext
                        {
                        }
                    }
                """;

            CSharpParseOptions parseOptions = CompilationHelper.CreateParseOptions(langVersion);
            Compilation compilation = CompilationHelper.CreateCompilation(source, parseOptions: parseOptions);

            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Theory]
        [InlineData(LanguageVersion.Default)]
        [InlineData(LanguageVersion.Preview)]
        [InlineData(LanguageVersion.Latest)]
        [InlineData(LanguageVersion.LatestMajor)]
        [InlineData(LanguageVersion.CSharp9)]
#if ROSLYN4_4_OR_GREATER
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
#endif
        public void SupportedLanguageVersions_Memory_SucceedCompilation(LanguageVersion langVersion)
        {
            string source = """
                    using System;
                    using System.Text.Json.Serialization;

                    namespace HelloWorld
                    {
                        public class MyClass<T>
                        {
                            public MyClass(
                                Memory<T> memoryOfT,
                                Memory<byte> memoryByte,
                                ReadOnlyMemory<T> readOnlyMemoryOfT,
                                ReadOnlyMemory<byte> readOnlyMemoryByte)
                            {
                                MemoryOfT = memoryOfT;
                                MemoryByte = memoryByte;
                                ReadOnlyMemoryOfT = readOnlyMemoryOfT;
                                ReadOnlyMemoryByte = readOnlyMemoryByte;
                            }

                            public Memory<T> MemoryOfT { get; set; }
                            public Memory<byte> MemoryByte { get; set; }
                            public ReadOnlyMemory<T> ReadOnlyMemoryOfT { get; set; }
                            public ReadOnlyMemory<byte> ReadOnlyMemoryByte { get; set; }
                        }

                        [JsonSerializable(typeof(MyClass<int>))]
                        public partial class MyJsonContext : JsonSerializerContext
                        {
                        }
                    }
                """;

            CSharpParseOptions parseOptions = CompilationHelper.CreateParseOptions(langVersion);
            Compilation compilation = CompilationHelper.CreateCompilation(source, parseOptions: parseOptions);

            CompilationHelper.RunJsonSourceGenerator(compilation);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp1)]
        [InlineData(LanguageVersion.CSharp2)]
        [InlineData(LanguageVersion.CSharp3)]
        [InlineData(LanguageVersion.CSharp7)]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        public void UnsupportedLanguageVersions_FailCompilation(LanguageVersion langVersion)
        {
            string source = """
                    using System.Text.Json.Serialization;

                    namespace HelloWorld
                    {
                        public class MyClass
                        {
                            public MyClass(int value)
                            {
                                Value = value;
                            }

                            public int Value { get; set; }
                        }

                        [JsonSerializable(typeof(MyClass))]
                        public partial class MyJsonContext : JsonSerializerContext
                        {
                        }
                    }
                """;

            CSharpParseOptions parseOptions = CompilationHelper.CreateParseOptions(langVersion);
            Compilation compilation = CompilationHelper.CreateCompilation(source, parseOptions: parseOptions);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location contextLocation = compilation.GetSymbolsWithName("MyJsonContext").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Error, contextLocation, $"The System.Text.Json source generator is not available in C# {langVersion.ToDisplayString()}. Please use language version 9.0 or greater.")
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void TypesWithJsonConstructorAnnotations_WarnAsExpected()
        {
            // Inaccessible [JsonConstructor] constructors are now supported via UnsafeAccessor or reflection fallback.
            // No diagnostics should be emitted.
            Compilation compilation = CompilationHelper.CreateCompilationWithJsonConstructorAttributeAnnotations();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            CompilationHelper.AssertEqualDiagnosticMessages([], result.Diagnostics);
        }

        [Fact]
        public void DiagnosticOnMemberFromReferencedAssembly_LocationDefaultsToContextClass()
        {
            Compilation referencedCompilation = CompilationHelper.CreateCompilation("""
                using System.Text.Json.Serialization;

                namespace Library
                {
                    public class MyPoco
                    {
                        [JsonConverter(typeof(int))]
                        public int Value { get; set; }
                    }
                }
                """);

            // Emit the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);
            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            Compilation compilation = CompilationHelper.CreateCompilation("""
                using Library;
                using System.Text.Json.Serialization;

                namespace Application
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class MyContext : JsonSerializerContext
                    { }
                }
                """, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location contextLocation = compilation.GetSymbolsWithName("MyContext").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, contextLocation, "The 'JsonConverterAttribute' type 'int' specified on member 'Library.MyPoco.Value' is not a converter type or does not contain an accessible parameterless constructor."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void JsonSerializableAttributeOnNonContextClass()
        {
            Compilation compilation = CompilationHelper.CreateCompilation("""
                using System.Text.Json.Serialization;

                namespace Application
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class MyContext : IDisposable
                    {
                        public void Dispose() { }
                    }

                    public class MyPoco { }
                }
                """);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location contextLocation = compilation.GetSymbolsWithName("MyContext").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, contextLocation, "The type 'Application.MyContext' has been annotated with JsonSerializableAttribute but does not derive from JsonSerializerContext. No source code will be generated."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void RefStructPropertyWithoutJsonIgnore_CompilesWithWarning()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/98590

            string source = """
                using System;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    public ReadOnlySpan<char> Values => "abc".AsSpan();
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location propertyLocation = ((INamedTypeSymbol)compilation.GetSymbolsWithName("MyPoco").First()).GetMembers("Values").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, propertyLocation, "The type 'MyPoco' includes the ref like property, field or constructor parameter 'Values'. No source code will be generated for the property, field or constructor."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void RefStructCtorParam_CompilesWithWarning()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/98590

            string source = """
                using System;
                using System.Text.Json.Serialization;

                public class MyPoco
                {
                    public MyPoco(ReadOnlySpan<char> value)
                    {
                        Value = value.ToString();
                    }

                    public string Value { get; }
                }

                [JsonSerializable(typeof(MyPoco))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            ITypeSymbol type = (INamedTypeSymbol)compilation.GetSymbolsWithName("MyPoco").First();
            IMethodSymbol ctor = (IMethodSymbol)type.GetMembers(".ctor").First();
            IParameterSymbol param = ctor.Parameters.First();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, param.Locations.First(), "The type 'MyPoco' includes the ref like property, field or constructor parameter 'value'. No source code will be generated for the property, field or constructor."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void UnionWithAmbiguousCaseTypes_CompilesWithWarning()
        {
            // Two case types serialize as the same JSON value type (Number).
            string source = """
                using System.Runtime.CompilerServices;
                using System.Text.Json.Serialization;
                using System.Text.Json.Serialization.Metadata;

                namespace TestApp
                {
                    [JsonSerializable(typeof(IntOrLongUnion))]
                    internal partial class MyContext : JsonSerializerContext { }

                    [Union]
                    public readonly struct IntOrLongUnion : IUnion
                    {
                        public IntOrLongUnion(int value) { Value = value; }
                        public IntOrLongUnion(long value) { Value = value; }
                        public object? Value { get; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location unionLocation = compilation.GetSymbolsWithName("IntOrLongUnion").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, unionLocation, "Union type 'IntOrLongUnion': case types 'int', 'long' all serialize as JSON value type 'Number'. Set JsonUnionAttribute.TypeClassifier to provide a custom classifier that can disambiguate."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void UnionWithListAndDictionaryCases_CompilesWithoutWarning()
        {
            string source = """
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                using System.Text.Json.Serialization;

                namespace TestApp
                {
                    [JsonSerializable(typeof(ListOrDictionaryUnion))]
                    internal partial class MyContext : JsonSerializerContext { }

                    [Union]
                    public readonly struct ListOrDictionaryUnion : IUnion
                    {
                        public ListOrDictionaryUnion(List<int> value) { Value = value; }
                        public ListOrDictionaryUnion(Dictionary<string, int> value) { Value = value; }
                        public object? Value { get; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "SYSLIB1227");
        }

        [Fact]
        public void UnionWithAmbiguousCaseTypesAndAttributeClassifier_CompilesWithoutWarning()
        {
            string source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Text.Json;
                using System.Text.Json.Serialization;
                using System.Text.Json.Serialization.Metadata;

                namespace TestApp
                {
                    [JsonSerializable(typeof(IntOrLongUnion))]
                    internal partial class MyContext : JsonSerializerContext { }

                    [JsonUnion(TypeClassifier = typeof(IntOrLongClassifier))]
                    [Union]
                    public readonly struct IntOrLongUnion : IUnion
                    {
                        public IntOrLongUnion(int value) { Value = value; }
                        public IntOrLongUnion(long value) { Value = value; }
                        public object? Value { get; }
                    }

                    public sealed class IntOrLongClassifier : JsonTypeClassifierFactory
                    {
                        public override bool CanClassify(JsonTypeClassifierContext context) => true;
                        public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
                            (ref Utf8JsonReader reader) => typeof(int);
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "SYSLIB1227");
        }

        [Fact]
        public void UnionWithAmbiguousCaseTypesAndOptionsClassifier_CompilesWithWarning()
        {
            string source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Text.Json;
                using System.Text.Json.Serialization;
                using System.Text.Json.Serialization.Metadata;

                namespace TestApp
                {
                    [JsonSourceGenerationOptions(TypeClassifiers = new[] { typeof(IntOrLongClassifier) })]
                    [JsonSerializable(typeof(IntOrLongUnion))]
                    internal partial class MyContext : JsonSerializerContext { }

                    [Union]
                    public readonly struct IntOrLongUnion : IUnion
                    {
                        public IntOrLongUnion(int value) { Value = value; }
                        public IntOrLongUnion(long value) { Value = value; }
                        public object? Value { get; }
                    }

                    public sealed class IntOrLongClassifier : JsonTypeClassifierFactory
                    {
                        public override bool CanClassify(JsonTypeClassifierContext context) => true;
                        public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
                            (ref Utf8JsonReader reader) => typeof(int);
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            Location unionLocation = compilation.GetSymbolsWithName("IntOrLongUnion").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, unionLocation, "Union type 'IntOrLongUnion': case types 'int', 'long' all serialize as JSON value type 'Number'. Set JsonUnionAttribute.TypeClassifier to provide a custom classifier that can disambiguate."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void UnionWithCustomConverterCaseType_CompilesWithWarning()
        {
            // A case type annotated with [JsonConverter] cannot be classified at compile time
            // because user-defined converters can serialize as any JSON value type.
            string source = """
                using System.Runtime.CompilerServices;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                namespace TestApp
                {
                    [JsonSerializable(typeof(MyUnion))]
                    internal partial class MyContext : JsonSerializerContext { }

                    [JsonConverter(typeof(CustomConverter))]
                    public class CustomCase
                    {
                    }

                    public class CustomConverter : JsonConverter<CustomCase>
                    {
                        public override CustomCase? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) => null;
                        public override void Write(Utf8JsonWriter writer, CustomCase value, JsonSerializerOptions options) { }
                    }

                    public class OtherCase
                    {
                    }

                    [Union]
                    public readonly struct MyUnion : IUnion
                    {
                        public MyUnion(CustomCase value) { Value = value; }
                        public MyUnion(OtherCase value) { Value = value; }
                        public object? Value { get; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location unionLocation = compilation.GetSymbolsWithName("MyUnion").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, unionLocation, "Union type 'MyUnion': case type 'CustomCase' is annotated with [JsonConverter] and may serialize as any JSON value type. Set JsonUnionAttribute.TypeClassifier to provide a custom classifier that can disambiguate."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void UnionAttributeWithoutValueProperty_GeneratesDiagnosticWarning()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Text.Json.Serialization;
                using System.Text.Json.Serialization.Metadata;

                namespace TestApp
                {
                    [JsonSerializable(typeof(MissingShapeUnion))]
                    internal partial class MyContext : JsonSerializerContext { }

                    [Union]
                    public readonly struct MissingShapeUnion : IUnion
                    {
                        public MissingShapeUnion(int value) { }
                        object IUnion.Value => throw new System.NotSupportedException();
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location unionLocation = compilation.GetSymbolsWithName("MissingShapeUnion").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, unionLocation, "Union type 'MissingShapeUnion' does not match the C# union shape convention: it must declare at least one public single-parameter case constructor and a public instance property named 'Value' with type 'object'."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void UnionWithValuePropertyButNoCaseConstructor_GeneratesDiagnosticWarning()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Text.Json.Serialization;
                using System.Text.Json.Serialization.Metadata;

                namespace TestApp
                {
                    [JsonSerializable(typeof(MissingShapeUnion))]
                    internal partial class MyContext : JsonSerializerContext { }

                    [Union]
                    public readonly struct MissingShapeUnion : IUnion
                    {
                        public object? Value { get; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location unionLocation = compilation.GetSymbolsWithName("MissingShapeUnion").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, unionLocation, "Union type 'MissingShapeUnion' does not match the C# union shape convention: it must declare at least one public single-parameter case constructor and a public instance property named 'Value' with type 'object'."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

#if NET
        [Fact]
        public void CollectionWithRefStructElement_CompilesWithWarning()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/98590

            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                public class CollectionWithRefStructElement : IEnumerable<ReadOnlySpan<char>>
                {
                    private List<string> _values = new();
                    public void Add(ReadOnlySpan<char> value) => _values.Add(value.ToString());
                    IEnumerator<ReadOnlySpan<char>> IEnumerable<ReadOnlySpan<char>>.GetEnumerator() => new SpanEnumerator(_values.GetEnumerator());
                    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

                    private sealed class SpanEnumerator(IEnumerator<string> inner) : IEnumerator<ReadOnlySpan<char>>
                    {
                        public ReadOnlySpan<char> Current => inner.Current.AsSpan();
                        object IEnumerator.Current => throw new NotSupportedException();
                        public void Dispose() => inner.Dispose();
                        public bool MoveNext() => inner.MoveNext();
                        public void Reset() => inner.Reset();
                    }
                }

                [JsonSerializable(typeof(CollectionWithRefStructElement))]
                public partial class MyContext : JsonSerializerContext
                {
                }
                """;

            CSharpParseOptions parseOptions = CompilationHelper.CreateParseOptions((LanguageVersion)1300); // C# 13 required for ref struct collection elements.
            Compilation compilation = CompilationHelper.CreateCompilation(source, parseOptions: parseOptions);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            ISymbol contextSymbol = compilation.GetSymbolsWithName("MyContext").First();
            Collections.Immutable.ImmutableArray<AttributeData> attributes = contextSymbol.GetAttributes();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, attributes[0].GetLocation(), "Did not generate serialization metadata for type 'CollectionWithRefStructElement'."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }
#endif

        [Fact]
        public void JsonIgnoreConditionAlwaysOnTypeWarns()
        {
            Compilation compilation = CompilationHelper.CreateTypeAnnotatedWithJsonIgnoreAlways();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location myClassLocation = compilation.GetSymbolsWithName("MyClass").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, myClassLocation, "The type 'HelloWorld.MyClass' has been annotated with 'JsonIgnoreAttribute' using 'JsonIgnoreCondition.Always' which is not valid on type declarations. The attribute will be ignored."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void Diagnostic_HasPragmaSuppressibleLocation()
        {
            // SYSLIB1039: Polymorphism not supported for fast-path serialization (Warning, configurable).
            string source = """
                #pragma warning disable SYSLIB1039
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBaseClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerivedClass), "derived")]
                    public class MyBaseClass
                    {
                    }

                    public class MyDerivedClass : MyBaseClass
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, compilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1039");
            Assert.True(diagnostic.IsSuppressed);
        }

        [Fact]
        public void GenericConverterArityMismatch_WarnsAsExpected()
        {
            Compilation compilation = CompilationHelper.CreateTypesWithGenericConverterArityMismatch();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location converterAttrLocation = compilation.GetSymbolsWithName("TypeWithArityMismatch").First().GetAttributes()[0].GetLocation();
            INamedTypeSymbol contextSymbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("JsonContext").First();
            Location jsonSerializableAttrLocation = contextSymbol.GetAttributes()[0].GetLocation();

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, jsonSerializableAttrLocation, "Did not generate serialization metadata for type 'HelloWorld.TypeWithArityMismatch<int>'."),
                new(DiagnosticSeverity.Warning, converterAttrLocation, "The 'JsonConverterAttribute' type 'HelloWorld.ConverterWithTwoParams<,>' specified on member 'HelloWorld.TypeWithArityMismatch<int>' is not a converter type or does not contain an accessible parameterless constructor."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
        }

        [Fact]
        public void GenericConverterTypeMismatch_NoSourceGeneratorWarning_FailsAtRuntime()
        {
            // Note: The source generator cannot detect at compile time that the converter
            // converts the wrong type (DifferentType<T> vs TypeWithConverterMismatch<T>).
            // The DifferentTypeConverter<int> is a valid JsonConverter with a parameterless constructor,
            // so the source generator accepts it. The mismatch will cause a runtime error.
            Compilation compilation = CompilationHelper.CreateTypesWithGenericConverterTypeMismatch();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Should compile without diagnostics - the converter is technically valid
            Assert.Empty(result.Diagnostics);
            result.AssertContainsType("global::HelloWorld.TypeWithConverterMismatch<int>");
        }

        [Fact]
        public void NestedGenericConverter_CompileSuccessfully()
        {
            Compilation compilation = CompilationHelper.CreateTypesWithNestedGenericConverter();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Should compile without diagnostics
            Assert.Empty(result.Diagnostics);
            result.AssertContainsType("global::HelloWorld.TypeWithNestedConverter<int, string>");
        }

        [Fact]
        public void ConstrainedGenericConverter_WithSatisfiedConstraint_CompileSuccessfully()
        {
            Compilation compilation = CompilationHelper.CreateTypesWithConstrainedGenericConverter();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            result.AssertContainsType("global::HelloWorld.TypeWithSatisfiedConstraint<string>");
        }

        [Fact]
        public void DeeplyNestedGenericConverter_CompileSuccessfully()
        {
            Compilation compilation = CompilationHelper.CreateTypesWithDeeplyNestedGenericConverter();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            result.AssertContainsType("global::HelloWorld.TypeWithDeeplyNestedConverter<int, string>");
        }

        [Fact]
        public void NonGenericOuterGenericConverter_CompileSuccessfully()
        {
            Compilation compilation = CompilationHelper.CreateTypesWithNonGenericOuterGenericConverter();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            result.AssertContainsType("global::HelloWorld.TypeWithNonGenericOuterConverter<int>");
        }

        [Fact]
        public void ManyParamsAsymmetricNestedConverter_CompileSuccessfully()
        {
            Compilation compilation = CompilationHelper.CreateTypesWithManyParamsAsymmetricNestedConverter();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            result.AssertContainsType("global::HelloWorld.TypeWithManyParams<int, string, bool, double, long>");
        }

        [Fact]
        public void OpenGenericDerivedType_SupportedPattern_CompileSuccessfully()
        {
            string source = """
                #nullable enable
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                        public T? Value { get; set; }
                    }

                    public class MyDerived<T> : MyBase<T>
                    {
                        public T? Extra { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            result.AssertContainsType("global::HelloWorld.MyBase<int>");
        }

        [Fact]
        public void OpenGenericDerivedType_WrappedTypeArgs_WarnsWithSYSLIB1229()
        {
            string source = """
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                        public T? Value { get; set; }
                    }

                    public class MyDerived<T> : MyBase<List<T>>
                    {
                        public T? Extra { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SYSLIB1229");
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("MyDerived<>", diagnostic.GetMessage());
            Assert.Contains("MyBase<int>", diagnostic.GetMessage());
        }

        [Fact]
        public void OpenGenericDerivedType_GroundMismatchAgainstClosedBase_WarnsWithSYSLIB1229()
        {
            // Derived<T> : Base<T, int> registered on Base<int, string>:
            // position 0 (T) unifies with int, but position 1 (the concrete `int` in the
            // derived's base spec) contradicts `string` in the closed base. The derived is
            // well-formed in isolation -- it just does not apply to this particular closed
            // base. The resolver surfaces a SYSLIB1229 diagnostic.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int, string>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T1, T2>
                    {
                        public T1 Value1 { get; set; }
                        public T2 Value2 { get; set; }
                    }

                    public class MyDerived<T> : MyBase<T, int>
                    {
                        public T Extra { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SYSLIB1229");
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        }

        [Fact]
        public void OpenGenericDerivedType_PartiallyConcrete_Resolves()
        {
            // Derived<T> : Base<T, int> registered on Base<string, int>:
            // position 0 (T) unifies with string, position 1 (concrete int) matches.
            // Expected: closed type is MyDerived<string>, no diagnostic.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<string, int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T1, T2>
                    {
                        public T1 Value1 { get; set; }
                        public T2 Value2 { get; set; }
                    }

                    public class MyDerived<T> : MyBase<T, int>
                    {
                        public T Extra { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Assert.Empty(result.Diagnostics.Where(d => d.Id == "SYSLIB1229"));
        }

        [Fact]
        public void OpenGenericDerivedType_NonGenericBase_WarnsWithSYSLIB1229()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase
                    {
                        public int Value { get; set; }
                    }

                    public class MyDerived<T> : MyBase
                    {
                        public T? Extra { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SYSLIB1229");
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        }

        [Fact]
        public void OpenGenericDerivedType_SYSLIB1229_IsPragmaSuppressible()
        {
            string source = """
                #pragma warning disable SYSLIB1229
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                        public T? Value { get; set; }
                    }

                    public class MyDerived<T> : MyBase<List<T>>
                    {
                        public T? Extra { get; set; }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, compilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1229");
            Assert.True(diagnostic.IsSuppressed);
        }

        [Fact]
        public void OpenGenericDerivedType_WrappedArgWithMatchingBase_CompilesSuccessfully()
        {
            // Derived<T> : Base<List<T>> registered on Base<List<int>> unifies to Derived<int>.
            string source = """
                #nullable enable
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<List<int>>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                        public T? Value { get; set; }
                    }

                    public class MyDerived<T> : MyBase<List<T>>
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_ReorderedParameters_CompilesSuccessfully()
        {
            // Derived<T1, T2> : Base<T2, T1> registered on Base<int, string> unifies to Derived<string, int>.
            string source = """
                #nullable enable
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int, string>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<,>), "derived")]
                    public class MyBase<T1, T2>
                    {
                    }

                    public class MyDerived<T1, T2> : MyBase<T2, T1>
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_PartialConcretization_CompilesSuccessfully()
        {
            // Derived<T> : Base<T, int> registered on Base<string, int> unifies to Derived<string>.
            string source = """
                #nullable enable
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<string, int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T1, T2>
                    {
                    }

                    public class MyDerived<T> : MyBase<T, int>
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_ArrayTypeArg_CompilesSuccessfully()
        {
            // Derived<T> : Base<T[]> registered on Base<int[]> unifies to Derived<int>.
            // Exercises the IArrayTypeSymbol branch of structural unification.
            string source = """
                #nullable enable
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int[]>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                    }

                    public class MyDerived<T> : MyBase<T[]>
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_UnboundParameter_WarnsWithSYSLIB1229()
        {
            // Derived<T1, T2> : Base<T1> — T2 is not bound by the base type's arguments.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<,>), "derived")]
                    public class MyBase<T>
                    {
                    }

                    public class MyDerived<T1, T2> : MyBase<T1>
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SYSLIB1229");
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        }

        [Fact]
        public void OpenGenericDerivedType_AmbiguousMatch_WarnsWithSYSLIB1229()
        {
            // Impl<T> : IBase<T>, IBase<List<T>> registered on IBase<List<int>>.
            // Both interface ancestors unify, which is ambiguous.
            string source = """
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(IBase<List<int>>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(Impl<>), "impl")]
                    public interface IBase<T> { }

                    public class Impl<T> : IBase<T>, IBase<List<T>> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SYSLIB1229");
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        }

        [Fact]
        public void OpenGenericDerivedType_ConstraintViolation_WarnsWithSYSLIB1229()
        {
            // Derived<T> : Base<T> where T : struct, registered on Base<string>.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<string>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                    }

                    public class MyDerived<T> : MyBase<T> where T : struct
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SYSLIB1229");
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        }

        [Fact]
        public void OpenGenericDerivedType_ReferenceTypeConstraintSatisfied_CompilesSuccessfully()
        {
            // Derived<T> : Base<T> where T : class, registered on Base<string>.
            string source = """
                #nullable enable
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<string>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                    }

                    public class MyDerived<T> : MyBase<T> where T : class
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_InterfaceConstraintSatisfied_CompilesSuccessfully()
        {
            // Derived<T> : Base<T> where T : System.IComparable<T>, registered on Base<int>.
            string source = """
                #nullable enable
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<>), "derived")]
                    public class MyBase<T>
                    {
                    }

                    public class MyDerived<T> : MyBase<T> where T : System.IComparable<T>
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_ArrayInsideGenericConstraint_CompilesSuccessfully()
        {
            // where T : IEnumerable<U[]> with T=List<int[]>, U=int.
            // Exercises array substitution into a generic constraint type.
            string source = """
                #nullable enable
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBase<List<int[]>, int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerived<,>), "derived")]
                    public class MyBase<T1, T2>
                    {
                    }

                    public class MyDerived<T, U> : MyBase<T, U> where T : IEnumerable<U[]>
                    {
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_NestedInGenericOuter_CompilesSuccessfully()
        {
            // Outer<T>.Middle.Leaf<U> : Base<(T, U)> registered on Base<(int, string)>.
            // Exercises ConstructEnclosing rebinding a non-generic intermediate type
            // through a constructed generic outer.
            string source = """
                #nullable enable
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(Base<(int, string)>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(Outer<>.Middle.Leaf<>), "leaf")]
                    public class Base<T>
                    {
                    }

                    public class Outer<T>
                    {
                        public class Middle
                        {
                            public class Leaf<U> : Base<(T, U)>
                            {
                            }
                        }
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_MultipleGenericInterfaceBases_EachResolvesIndependently()
        {
            // Impl<T> implements two unrelated generic interfaces, each carrying its own
            // open-generic [JsonDerivedType(typeof(Impl<>))]. Each base interface's
            // polymorphism metadata must resolve Impl<> independently against its closed form.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(IBaseA<int>))]
                    [JsonSerializable(typeof(IBaseB<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(Impl<>), "impl")]
                    public interface IBaseA<T> { }

                    [JsonDerivedType(typeof(Impl<>), "impl")]
                    public interface IBaseB<T> { }

                    public class Impl<T> : IBaseA<T>, IBaseB<T> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_GenericInterfaceDiamond_CompilesSuccessfully()
        {
            // Diamond inheritance via two generic interface legs. Impl<T> reaches IBaseA<T>
            // and IBaseB<T> through the intermediate IDerived<T>. Resolution through each
            // diamond leg must independently succeed.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(IBaseA<int>))]
                    [JsonSerializable(typeof(IBaseB<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(Impl<>), "impl")]
                    public interface IBaseA<T> { }

                    [JsonDerivedType(typeof(Impl<>), "impl")]
                    public interface IBaseB<T> { }

                    public interface IDerived<T> : IBaseA<T>, IBaseB<T> { }

                    public class Impl<T> : IDerived<T> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_MultipleInterfaceConstructions_NonAmbiguousResolution_CompilesSuccessfully()
        {
            // Impl<T> reaches IBase<> twice: once via its own type-parameterized base (IBase<T>)
            // and once via inheritance from the non-generic IntBase (IBase<int>). When resolved
            // against the closed base IBase<string>, only the IBase<T> leg unifies (T=string);
            // the IBase<int> leg is incompatible. Resolution must succeed and produce
            // Impl<string>. The both-legs-match scenario (which IS ambiguous) is covered by
            // OpenGenericDerivedType_AmbiguousMatch. Indirecting the IBase<int> leg through a
            // non-generic base class avoids C# CS0695 -- a class cannot directly declare two
            // constructions of the same generic interface that could unify under any
            // substitution.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(IBase<string>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(Impl<>), "impl")]
                    public interface IBase<T> { }

                    public class IntBase : IBase<int> { }

                    public class Impl<T> : IntBase, IBase<T> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_CovariantInterface_CompilesSuccessfully()
        {
            // Open generic VarCovImpl<T> registered on a covariant interface base.
            // Unification is purely structural and ignores 'out'; the resolver closes T
            // to whatever the closed base specifies. No diagnostic expected.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(IVarCovBase<Animal>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class Animal { }

                    [JsonDerivedType(typeof(VarCovImpl<>), "covImpl")]
                    public interface IVarCovBase<out T> { }

                    public class VarCovImpl<T> : IVarCovBase<T> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_ContravariantInterface_CompilesSuccessfully()
        {
            // Open generic VarContraImpl<T> registered on a contravariant interface base.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(IVarContraBase<Animal>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class Animal { }

                    [JsonDerivedType(typeof(VarContraImpl<>), "contraImpl")]
                    public interface IVarContraBase<in T> { }

                    public class VarContraImpl<T> : IVarContraBase<T> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_BivariantInterface_CompilesSuccessfully()
        {
            // Mixed-variance interface with both 'in' and 'out' parameters.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(IVarBivariantBase<Dog, Animal>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class Animal { }
                    public class Dog : Animal { }

                    [JsonDerivedType(typeof(VarBivariantImpl<,>), "bvImpl")]
                    public interface IVarBivariantBase<in TIn, out TOut> { }

                    public class VarBivariantImpl<TIn, TOut> : IVarBivariantBase<TIn, TOut> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_NestedGenericEnclosingMismatch_WarnsWithSYSLIB1229()
        {
            // Pattern: NestedDerived<T> : NestedBase<NestedOuter<int>.NestedBox<T>>.
            // Closed base: NestedBase<NestedOuter<string>.NestedBox<int>>.
            // The enclosing argument differs (int vs string); unification MUST fail.
            // Pre-fix source-gen ignored ContainingType arguments in TryUnifyWith and would
            // have false-accepted T=int. With the ContainingType walk in place, the
            // mismatch is detected and SYSLIB1229 is reported, matching the reflection
            // resolver's InvalidOperationException at runtime.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(NestedBase<NestedOuter<string>.NestedBox<int>>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(NestedDerivedMismatch<>), "nested")]
                    public class NestedBase<T> { }

                    public class NestedOuter<TOuter>
                    {
                        public class NestedBox<TInner> { }
                    }

                    public class NestedDerivedMismatch<T> : NestedBase<NestedOuter<int>.NestedBox<T>> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("SYSLIB1229", diagnostic.Id);
        }

        [Fact]
        public void OpenGenericDerivedType_NestedGenericTypeParameterInEnclosing_CompilesSuccessfully()
        {
            // Pattern: NestedDerived<T> : NestedBase<NestedOuter<T>.NestedBox<int>>.
            // Closed base: NestedBase<NestedOuter<string>.NestedBox<int>>.
            // T appears only in the ENCLOSING type's argument list. Pre-fix source-gen
            // ignored ContainingType and reported SYSLIB1229 because T was never bound by
            // the leaf-only TryUnifyWith walk. With the ContainingType walk in place,
            // unification succeeds with T=string and the resolver closes NestedDerived to
            // NestedDerived<string>. No diagnostic expected.
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(NestedBaseB<NestedOuterB<string>.NestedBoxB<int>>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(NestedDerivedParamInEnclosing<>), "nestedB")]
                    public class NestedBaseB<T> { }

                    public class NestedOuterB<TOuter>
                    {
                        public class NestedBoxB<TInner> { }
                    }

                    public class NestedDerivedParamInEnclosing<T> : NestedBaseB<NestedOuterB<T>.NestedBoxB<int>> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void OpenGenericDerivedType_CovariantInterfaceConstraintSatisfied_CompilesSuccessfully()
        {
            // where T : IEnumerable<object>. Closing T to List<string> satisfies the
            // constraint ONLY via IEnumerable<out T> covariance (IEnumerable<string> is
            // assignable to IEnumerable<object> only by virtue of 'out T'). Pre-fix
            // source-gen used identity-based interface containment for the constraint
            // check and would have reported SYSLIB1229. With Compilation.HasImplicitConversion
            // in place, source-gen matches reflection's behavior and accepts.
            string source = """
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(ConstraintBase<List<string>>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(ConstraintImpl<>), "impl")]
                    public class ConstraintBase<T> { }

                    public class ConstraintImpl<T> : ConstraintBase<T> where T : IEnumerable<object> { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);
            Assert.Empty(result.Diagnostics);
        }
    }
}
