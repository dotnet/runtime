// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        public void WarnOnClassesWithInaccessibleJsonIncludeProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInaccessibleJsonIncludeProperties();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location idLocation = compilation.GetSymbolsWithName("Id").First().Locations[0];
            Location address2Location = compilation.GetSymbolsWithName("Address2").First().Locations[0];
            Location countryLocation = compilation.GetSymbolsWithName("Country").First().Locations[0];
            Location privateFieldLocation = compilation.GetSymbolsWithName("privateField").First().Locations[0];
            Location protectedFieldLocation = compilation.GetSymbolsWithName("protectedField").First().Locations[0];
            Location protectedPropertyLocation = compilation.GetSymbolsWithName("ProtectedProperty").First().Locations[0];
            Location internalPropertyWithPrivateGetterLocation = compilation.GetSymbolsWithName("InternalPropertyWithPrivateGetter").First().Locations[0];
            Location internalPropertyWithPrivateSetterLocation = compilation.GetSymbolsWithName("InternalPropertyWithPrivateSetter").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, idLocation, "The member 'Location.Id' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                new(DiagnosticSeverity.Warning, address2Location, "The member 'Location.Address2' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                new(DiagnosticSeverity.Warning, countryLocation, "The member 'Location.Country' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                new(DiagnosticSeverity.Warning, privateFieldLocation, "The member 'Location.privateField' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                new(DiagnosticSeverity.Warning, protectedFieldLocation, "The member 'Location.protectedField' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                new(DiagnosticSeverity.Warning, protectedPropertyLocation, "The member 'Location.ProtectedProperty' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                new(DiagnosticSeverity.Warning, internalPropertyWithPrivateGetterLocation, "The member 'Location.InternalPropertyWithPrivateGetter' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                new(DiagnosticSeverity.Warning, internalPropertyWithPrivateSetterLocation, "The member 'Location.InternalPropertyWithPrivateSetter' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
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
            Compilation compilation = CompilationHelper.CreateCompilationWithJsonConstructorAttributeAnnotations();

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, disableDiagnosticValidation: true);

            Location protectedCtorLocation = compilation.GetSymbolsWithName("ClassWithProtectedCtor").First().Locations[0];
            Location privateCtorLocation = compilation.GetSymbolsWithName("ClassWithPrivateCtor").First().Locations[0];

            var expectedDiagnostics = new DiagnosticData[]
            {
                new(DiagnosticSeverity.Warning, protectedCtorLocation, "The constructor on type 'HelloWorld.ClassWithProtectedCtor' has been annotated with JsonConstructorAttribute but is not accessible by the source generator."),
                new(DiagnosticSeverity.Warning, privateCtorLocation, "The constructor on type 'HelloWorld.ClassWithPrivateCtor' has been annotated with JsonConstructorAttribute but is not accessible by the source generator."),
            };

            CompilationHelper.AssertEqualDiagnosticMessages(expectedDiagnostics, result.Diagnostics);
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
    }
}
