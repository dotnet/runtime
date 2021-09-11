// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class JsonSourceGeneratorDiagnosticsTests
    {
        [Fact]
        [ActiveIssue("Figure out issue with CampaignSummaryViewModel namespace.")]
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

            [assembly: JsonSerializable(typeof(JsonSourceGenerator.IndexViewModel)]

            namespace JsonSourceGenerator
            {
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

            // Expected info logs.
            string[] expectedInfoDiagnostics = new string[] {
                "Generated serialization metadata for type System.Collections.Generic.List<ReferencedAssembly.ActiveOrUpcomingEvent>",
                "Generated serialization metadata for type System.Int32",
                "Generated serialization metadata for type System.String",
                "Generated serialization metadata for type System.DateTimeOffset",
                "Generated serialization metadata for type System.Boolean",
                "Generated serialization metadata for type ReferencedAssembly.ActiveOrUpcomingEvent",
                "Generated serialization metadata for type ReferencedAssembly.CampaignSummaryViewModel",
                "Generated serialization metadata for type JsonSourceGenerator.IndexViewModel",
            };

            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, expectedInfoDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, new string[] { });
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, new string[] { });
        }

        [Fact]
        [ActiveIssue("Figure out issue with CampaignSummaryViewModel namespace.")]
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

            [assembly: JsonSerializable(typeof(JsonSourceGenerator.IndexViewModel)]

            namespace JsonSourceGenerator
            {
                public class IndexViewModel
                {
                    public ISet<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
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

            // Expected success info logs.
            string[] expectedInfoDiagnostics = new string[] {
                "Generated serialization metadata for type JsonSourceGeneration.IndexViewModel",
                "Generated serialization metadata for type System.Boolean",
                "Generated serialization metadata for type ReferencedAssembly.CampaignSummaryViewModel"
            };

            // Expected warning logs.
            string[] expectedWarningDiagnostics = new string[] { "Did not generate serialization metadata for type System.Collections.Generic.ISet<ReferencedAssembly.ActiveOrUpcomingEvent>" };

            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, expectedInfoDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, new string[] { });
        }

        [Fact]
        public void NameClashSourceGeneration()
        {
            // Without resolution.
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();
            JsonSourceGenerator generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            string[] expectedWarningDiagnostics = new string[] { "There are multiple types named Location. Source was generated for the first one detected. Use 'JsonSerializableAttribute.TypeInfoPropertyName' to resolve this collision." };

            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, Array.Empty<string>());
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, expectedWarningDiagnostics);
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, Array.Empty<string>());

            // With resolution.
            compilation = CompilationHelper.CreateRepeatedLocationsWithResolutionCompilation();
            generator = new JsonSourceGenerator();
            CompilationHelper.RunGenerators(compilation, out generatorDiags, generator);

            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, Array.Empty<string>());
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, Array.Empty<string>());
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, Array.Empty<string>());
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

            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, Array.Empty<string>());
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, Array.Empty<string>());
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, Array.Empty<string>());

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

            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, Array.Empty<string>());
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, Array.Empty<string>());
            CompilationHelper.CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, Array.Empty<string>());
        }
    }
}
