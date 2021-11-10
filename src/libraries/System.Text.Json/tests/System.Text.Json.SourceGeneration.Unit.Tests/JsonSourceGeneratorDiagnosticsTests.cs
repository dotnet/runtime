// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
        public void UnsuccessfulSourceGeneration()
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
                    public ActiveOrUpcomingEvent[,] ActiveOrUpcomingEvents { get; set; }
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

            // Expected warning logs.
            ValueTuple<TextSpan, string>[] expectedWarningDiagnostics = new ValueTuple<TextSpan, string>[]
            {
                (new TextSpan(315, 11), "Did not generate serialization metadata for type 'global::ReferencedAssembly.ActiveOrUpcomingEvent[]'.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
        }

        [Fact]
        public void NameClashSourceGeneration()
        {
            // Without resolution.
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            ValueTuple<TextSpan, string>[] expectedWarningDiagnostics = new ValueTuple<TextSpan, string>[]
            {
                (new TextSpan(303, 45), "There are multiple types named Location. Source was generated for the first one detected. Use 'JsonSerializableAttribute.TypeInfoPropertyName' to resolve this collision.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());

            // With resolution.
            compilation = CompilationHelper.CreateRepeatedLocationsWithResolutionCompilation();
            generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
        }

        [Fact]
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

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());

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

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
        }

        [Fact]
        public void WarnOnClassesWithInitOnlyProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInitOnlyProperties();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            ValueTuple<TextSpan, string>[] expectedWarningDiagnostics = new ValueTuple<TextSpan, string>[]
            {
                (new TextSpan(236, 2), "The type 'Location' defines init-only properties, deserialization of which is currently not supported in source generation mode.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
        }

        [Fact]
        public void WarnOnClassesWithInaccessibleJsonIncludeProperties()
        {
            Compilation compilation = CompilationHelper.CreateCompilationWithInaccessibleJsonIncludeProperties();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            ValueTuple<TextSpan, string>[] expectedWarningDiagnostics = new ValueTuple<TextSpan, string>[]
            {
                (new TextSpan(271, 2), "The member 'Location.Id' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                (new TextSpan(469, 8), "The member 'Location.Address2' has been annotated with the JsonIncludeAttribute but is not visible to the source generator."),
                (new TextSpan(667, 7), "The member 'Location.Country' has been annotated with the JsonIncludeAttribute but is not visible to the source generator.")
            };

            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Info, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Warning, generatorDiags, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(DiagnosticSeverity.Error, generatorDiags, Array.Empty<ValueTuple<TextSpan, string>>());
        }
    }
}
