// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class JsonSourceGeneratorDiagnosticsTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
        public void SuccessfulSourceGeneration()
        {
            // Compile the referenced assembly first.
            Compilation campaignCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();
            Compilation eventCompilation = CompilationHelper.CreateActiveOrUpcomingEventCompilation();

            // Emit the image of the referenced assembly.
            byte[] campaignImage = CompilationHelper.CreateAssemblyImage(campaignCompilation);
            byte[] eventImage = CompilationHelper.CreateAssemblyImage(eventCompilation);

            // Main source for current compilation.
            string source = @"
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
            }";

            MetadataReference[] additionalReferences = {
                MetadataReference.CreateFromImage(campaignImage),
                MetadataReference.CreateFromImage(eventImage),
            };

            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
        public void UnsuccessfulSourceGeneration()
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
                string source = @$"
            using System.Collections.Generic;
            using System.Text.Json.Serialization;
            using ReferencedAssembly;

            namespace JsonSourceGenerator
            {{
                {optionalAttribute}
                [JsonSerializable(typeof(JsonSourceGenerator.IndexViewModel)]
                public partial class JsonContext : JsonSerializerContext
                {{
                }}

                public class IndexViewModel
                {{
                    public ActiveOrUpcomingEvent[,] ActiveOrUpcomingEvents {{ get; set; }}
                    public CampaignSummaryViewModel FeaturedCampaign {{ get; set; }}
                    public bool IsNewAccount {{ get; set; }}
                    public bool HasFeaturedCampaign => FeaturedCampaign != null;
                }}
            }}";

                MetadataReference[] additionalReferences = {
                MetadataReference.CreateFromImage(campaignImage),
                MetadataReference.CreateFromImage(eventImage),
            };

                Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

                JsonSourceGenerator generator = new JsonSourceGenerator();
                CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

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

                // Expected warning logs.
                (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
                {
                    (location, "Did not generate serialization metadata for type 'global::ReferencedAssembly.ActiveOrUpcomingEvent[]'.")
                };

                CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
                CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
                CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
            }

            RunTest(explicitRef: true);
            RunTest(false);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
        public void NameClashSourceGeneration()
        {
            // Without resolution.
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            INamedTypeSymbol symbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("JsonContext").FirstOrDefault();
            SyntaxReference syntaxReference = new List<AttributeData>(symbol.GetAttributes())[1].ApplicationSyntaxReference;
            TextSpan textSpan = syntaxReference.Span;
            Location location = syntaxReference.SyntaxTree.GetLocation(textSpan)!;

            (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
            {
                (location, "There are multiple types named Location. Source was generated for the first one detected. Use 'JsonSerializableAttribute.TypeInfoPropertyName' to resolve this collision.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());

            // With resolution.
            compilation = CompilationHelper.CreateRepeatedLocationsWithResolutionCompilation();
            generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
        public void ProgramsThatDontUseGeneratorCompile()
        {
            // No STJ usage.
            string source = @"using System;

        public class Program
        {
            public static void Main()
            {
                Console.WriteLine(""Hello World"");
            }
        }
        ";
            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());

            // With STJ usage.
            source = @"using System.Text.Json;

        public class Program
        {
            public static void Main()
            {
                JsonSerializer.Serialize(""Hello World"");
            }
        }
        ";
            compilation = CompilationHelper.CreateCompilation(source);
            generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
        public void WarnOnClassesWithInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInitOnlyProperties();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            Location location = compilation.GetSymbolsWithName("Id").First().Locations[0];

            (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
            {
                (location, "The type 'Location' defines init-only properties, deserialization of which is currently not supported in source generation mode.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58770", TestPlatforms.Browser)]
        public void DoNotWarnOnClassesWithConstructorInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithConstructorInitOnlyProperties();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }
        
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58770", TestPlatforms.Browser)]
        public void WarnOnClassesWithMixedInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithMixedInitOnlyProperties();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            Location location = compilation.GetSymbolsWithName("Orphaned").First().Locations[0];

            (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
            {
                (location, "The type 'MyClass' defines init-only properties, deserialization of which is currently not supported in source generation mode.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58770", TestPlatforms.Browser)]
        public void DoNotWarnOnRecordsWithInitOnlyPositionalParameters()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithRecordPositionalParameters();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
        public void WarnOnClassesWithInaccessibleJsonIncludeProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInaccessibleJsonIncludeProperties();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            Location idLocation = compilation.GetSymbolsWithName("Id").First().Locations[0];
            Location address2Location = compilation.GetSymbolsWithName("Address2").First().Locations[0];
            Location countryLocation = compilation.GetSymbolsWithName("Country").First().Locations[0];

            (Location, string)[] expectedWarningDiagnostics = new (Location, string)[]
            {
                (idLocation, "The member 'Location.Id' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                (address2Location, "The member 'Location.Address2' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                (countryLocation, "The member 'Location.Country' has been annotated with the JsonIncludeAttribute but is not visible to the source generator.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<(Location, string)>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics, sort: false);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<(Location, string)>());
        }
    }
}
