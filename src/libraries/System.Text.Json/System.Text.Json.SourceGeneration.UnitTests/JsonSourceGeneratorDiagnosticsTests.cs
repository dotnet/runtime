// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class JsonSourceGeneratorDiagnosticsTests
    {
        [Fact]
        public void SuccessfulSourceGeneration()
        {
            // Compile the referenced assembly first.
            Compilation campaignCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();
            Compilation eventCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();

            // Emit the image of the referenced assembly.
            byte[] campaignImage = CompilationHelper.CreateAssemblyImage(campaignCompilation);
            byte[] eventImage = CompilationHelper.CreateAssemblyImage(eventCompilation);

            // Main source for current compilation.
            string source = @"
            using System.Text.Json.Serialization;

              namespace JsonSourceGenerator
              {
                [JsonSerializable]
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
                "Generated type class TestAssemblyActiveOrUpcomingEvent for root type IndexViewModel",
                "Generated type class TestAssemblyCampaignSummaryViewModel for root type IndexViewModel",
                "Generated type class TestAssemblyIndexViewModel for root type IndexViewModel",
            };

            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, expectedInfoDiagnostics);
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, new string[] { });
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, new string[] { });
        }

        [Fact]
        public void UnSuccessfulSourceGeneration()
        {
            // Compile the referenced assembly first.
            Compilation campaignCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();
            Compilation eventCompilation = CompilationHelper.CreateCampaignSummaryViewModelCompilation();

            // Emit the image of the referenced assembly.
            byte[] campaignImage = CompilationHelper.CreateAssemblyImage(campaignCompilation);
            byte[] eventImage = CompilationHelper.CreateAssemblyImage(eventCompilation);

            // Main source for current compilation.
            string source = @"
            using System.Text.Json.Serialization;
            using System.Collections.Generic;

              namespace JsonSourceGenerator
              {
                [JsonSerializable]
                public class IndexViewModel
                {
                    public Dictionary<string, ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
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
            string[] expectedWarningDiagnostics = new string[] { "Failed in sourcegenerating nested type Dictionary`2 for root type IndexViewModel" };

            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, new string[] { });
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, expectedWarningDiagnostics);
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, new string[] { });
        }

        private void CheckDiagnosticMessages(ImmutableArray<Diagnostic> diagnostics, DiagnosticSeverity level, string[] expectedMessages)
        {
            Assert.Equal(expectedMessages, diagnostics.Where(diagnostic => diagnostic.Severity == level ).Select(diagnostic => diagnostic.GetMessage()).ToArray());
        }
    }
}
