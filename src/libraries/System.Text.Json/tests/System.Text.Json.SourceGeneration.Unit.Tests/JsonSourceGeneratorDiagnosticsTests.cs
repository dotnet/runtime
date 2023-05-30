// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
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

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences, configureParseOptions: options => options.WithDocumentationMode(DocumentationMode.Diagnose));
            JsonSourceGeneratorResult sourceGenResult = CompilationHelper.RunJsonSourceGenerator(compilation);

            using var emitStream = new MemoryStream();
            using var xmlStream = new MemoryStream();
            var result = sourceGenResult.NewCompilation.Emit(emitStream, xmlDocumentationStream: xmlStream);
            var diagnostics = result.Diagnostics;

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, diagnostics, Array.Empty<(Location, string)>());
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
                    [JsonSerializable(typeof(JsonSourceGenerator.IndexViewModel)]
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

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void MultiDimensionArrayDoesNotProduceWarnings()
        {
            static void RunTest(bool explicitRef)
            {
                // Compile the referenced assembly first.
                Compilation campaignCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();
                Compilation eventCompilation = CompilationHelper.CreateActiveOrUpcomingEventCompilation();

                // Emit the image of the referenced assembly.
                byte[] campaignImage = CompilationHelper.CreateAssemblyImage(campaignCompilation);
                byte[] eventImage = CompilationHelper.CreateAssemblyImage(eventCompilation);

                string optionalAttribute = explicitRef ? "[JsonSerializable(typeof(ActiveOrUpcomingEvent[,])]" : null;

                // Main source for current compilation.
                string source = $$"""
                    using System.Collections.Generic;
                    using System.Text.Json.Serialization;
                    using ReferencedAssembly;

                    namespace JsonSourceGenerator
                    {
                        {{optionalAttribute}}
                        [JsonSerializable(typeof(JsonSourceGenerator.IndexViewModel)]
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

                JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

                Location location;
                if (explicitRef)
                {
                    // Unsupported type is not in compiling assembly, but is indicated directly with [JsonSerializable], so location points to attribute application.
                    INamedTypeSymbol symbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("JsonContext").FirstOrDefault();
                    SyntaxReference syntaxReference = symbol.GetAttributes().First().ApplicationSyntaxReference;
                    TextSpan textSpan = syntaxReference.Span;
                    location = syntaxReference.SyntaxTree.GetLocation(textSpan)!;
                }
                else
                {
                    // Unsupported type is not in compiling assembly, and isn't indicated directly with [JsonSerializable], so location points to context type.
                    location = compilation.GetSymbolsWithName("JsonContext").First().Locations[0];
                }

                CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
                CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
                CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
            }

            RunTest(explicitRef: true);
            RunTest(false);
        }

        [Fact]
        public void NameClashSourceGeneration()
        {
            // Without resolution.
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            INamedTypeSymbol symbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("JsonContext").FirstOrDefault();
            SyntaxReference syntaxReference = new List<AttributeData>(symbol.GetAttributes())[1].ApplicationSyntaxReference;
            TextSpan textSpan = syntaxReference.Span;
            Location location = syntaxReference.SyntaxTree.GetLocation(textSpan)!;

            (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
            {
                (location, "There are multiple types named Location. Source was generated for the first one detected. Use 'JsonSerializableAttribute.TypeInfoPropertyName' to resolve this collision.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());

            // With resolution.
            compilation = CompilationHelper.CreateRepeatedLocationsWithResolutionCompilation();
            result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
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
                        Console.WriteLine(""Hello World"");
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());

            // With STJ usage.
            source = """
                using System.Text.Json;

                public class Program
                {
                    public static void Main()
                    {
                        JsonSerializer.Serialize(""Hello World"");
                    }
                }
                """;

            compilation = CompilationHelper.CreateCompilation(source);
            result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void DoNotWarnOnClassesWithInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInitOnlyProperties();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void DoNotWarnOnClassesWithConstructorInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithConstructorInitOnlyProperties();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }
        
        [Fact]
        public void DoNotWarnOnClassesWithMixedInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithMixedInitOnlyProperties();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void DoNotWarnOnRecordsWithInitOnlyPositionalParameters()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithRecordPositionalParameters();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void DoNotWarnOnClassesWithRequiredProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithRequiredProperties();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void WarnOnClassesWithInaccessibleJsonIncludeProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInaccessibleJsonIncludeProperties();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Location idLocation = compilation.GetSymbolsWithName("Id").First().Locations[0];
            Location address2Location = compilation.GetSymbolsWithName("Address2").First().Locations[0];
            Location countryLocation = compilation.GetSymbolsWithName("Country").First().Locations[0];

            (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
            {
                (idLocation, "The member 'Location.Id' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                (address2Location, "The member 'Location.Address2' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                (countryLocation, "The member 'Location.Country' has been annotated with the JsonIncludeAttribute but is not visible to the source generator.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, expectedWarningDiagnostics, sort: false);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }

        [Fact]
        public void PolymorphicClassWarnsOnFastPath()
        {
            Compilation compilation = CompilationHelper.CreatePolymorphicClassOnFastPathContext();
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            Location myBaseClassLocation = compilation.GetSymbolsWithName("MyBaseClass").First().Locations[0];

            (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
            {
                (myBaseClassLocation, "Type 'HelloWorld.MyBaseClass' is annotated with 'JsonDerivedTypeAttribute' which is not supported in 'JsonSourceGenerationMode.Serialization'."),
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, result.Diagnostics, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, result.Diagnostics, expectedWarningDiagnostics, sort: false);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, result.Diagnostics, Array.Empty<(Location, string)>());
        }
    }
}
