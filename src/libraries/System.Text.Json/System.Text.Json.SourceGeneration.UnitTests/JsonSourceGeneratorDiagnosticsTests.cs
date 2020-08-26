// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class JsonSourceGeneratorDiagnosticsTests
    {
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
            string source = @"
            using System.Collections.Generic;
            using System.Text.Json.Serialization;
            using ReferencedAssembly;

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
                "Generated type class ActiveOrUpcomingEvent for root type IndexViewModel",
                "Generated type class CampaignSummaryViewModel for root type IndexViewModel",
                "Generated type class IndexViewModel for root type IndexViewModel",
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
            Compilation eventCompilation = CompilationHelper.CreateActiveOrUpcomingEventCompilation();

            // Emit the image of the referenced assembly.
            byte[] campaignImage = CompilationHelper.CreateAssemblyImage(campaignCompilation);
            byte[] eventImage = CompilationHelper.CreateAssemblyImage(eventCompilation);

            // Main source for current compilation.
            string source = @"
            using System.Text.Json.Serialization;
            using System.Collections.Generic;
            using ReferencedAssembly;

              namespace JsonSourceGenerator
              {
                [JsonSerializable]
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

            // Expected warning logs.
            string[] expectedWarningDiagnostics = new string[] { "Failed in sourcegenerating nested type ISet`1 for root type IndexViewModel" };

            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, new string[] { });
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, expectedWarningDiagnostics);
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, new string[] { });
        }

        [Fact]
        public void NameClashSourceGeneration()
        {
            Compilation compilation = CompilationHelper.CreateRepeatedLocationsCompilation();

            JsonSourceGenerator generator = new JsonSourceGenerator();

            CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Expected info logs.
            string[] expectedInfoDiagnostics = new string[] {
                "Generated type class Location for root type Location",
                "Generated type class TestAssemblyLocation for root type Location",
            };
            // Expected warning logs.
            string[] expectedWarningDiagnostics = new string[] { "Changed name type Location to TestAssemblyLocation. To use please call `JsonContext.Instance.TestAssemblyLocation`" };

            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Info, expectedInfoDiagnostics);
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Warning, expectedWarningDiagnostics);
            CheckDiagnosticMessages(generatorDiags, DiagnosticSeverity.Error, new string[] { });
        }

        private void CheckDiagnosticMessages(ImmutableArray<Diagnostic> diagnostics, DiagnosticSeverity level, string[] expectedMessages)
        {
            Assert.Equal(expectedMessages, diagnostics.Where(diagnostic => diagnostic.Severity == level ).Select(diagnostic => diagnostic.GetMessage()).ToArray());
        }
    }
}
