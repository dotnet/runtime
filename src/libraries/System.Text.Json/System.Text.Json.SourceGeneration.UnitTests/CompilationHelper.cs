// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class CompilationHelper
    {
        public static Compilation CreateCompilation(string source, MetadataReference[] additionalReferences = null)
        {
            // Bypass System.Runtime error.
            Assembly systemRuntimeAssembly = Assembly.Load("System.Runtime, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            Assembly systemCollectionsAssembly = Assembly.Load("System.Collections, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            string systemRuntimeAssemblyPath = systemRuntimeAssembly.Location;
            string systemCollecitonsAssemblyPath = systemCollectionsAssembly.Location;

            List<MetadataReference> references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonSerializableAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonSerializerOptions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Type).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(KeyValuePair).Assembly.Location),
                MetadataReference.CreateFromFile(systemRuntimeAssemblyPath),
                MetadataReference.CreateFromFile(systemCollecitonsAssemblyPath),
            };

            // Add additional references as needed.
            if (additionalReferences != null)
            {
                foreach (MetadataReference reference in additionalReferences)
                {
                    references.Add(reference);
                }
            }

            return CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
                references: references.ToArray(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        private static GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators)
            => new CSharpGeneratorDriver(
                new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse),
                ImmutableArray.Create(generators),
                ImmutableArray<AdditionalText>.Empty);

        public static Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators).RunFullGeneration(compilation, out Compilation outCompilation, out diagnostics);
            return outCompilation;
        }

        public static byte[] CreateAssemblyImage(Compilation compilation)
        {
            MemoryStream ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                throw new InvalidOperationException();
            }
            return ms.ToArray();
        }

        public static Compilation CreateReferencedLocationCompilation()
        {
            string source = @"
            namespace ReferencedAssembly
            {
                public class Location
                {
                    public int Id { get; set; }
                    public string Address1 { get; set; }
                    public string Address2 { get; set; }
                    public string City { get; set; }
                    public string State { get; set; }
                    public string PostalCode { get; set; }
                    public string Name { get; set; }
                    public string PhoneNumber { get; set; }
                    public string Country { get; set; }
                }
            }";

            return CreateCompilation(source);
        }

        public static Compilation CreateCampaignSummaryViewModelCompilation()
        {
            string source = @"
            namespace ReferencedAssembly
            {
                public class CampaignSummaryViewModel
                {
                    public int Id { get; set; }
                    public string Title { get; set; }
                    public string Description { get; set; }
                    public string ImageUrl { get; set; }
                    public string OrganizationName { get; set; }
                    public string Headline { get; set; }
                }
            }";

            return CreateCompilation(source);
        }

        public static Compilation CreateActiveOrUpcomingEventCompilation()
        {
            string source = @"
            namespace ReferencedAssembly
            {
                public class ActiveOrUpcomingEvent
                {
                    public int Id { get; set; }
                    public string ImageUrl { get; set; }
                    public string Name { get; set; }
                    public string CampaignName { get; set; }
                    public string CampaignManagedOrganizerName { get; set; }
                    public string Description { get; set; }
                    public DateTimeOffset StartDate { get; set; }
                    public DateTimeOffset EndDate { get; set; }
                }
            }";

            return CreateCompilation(source);
        }

        public static Compilation CreateReferencedHighLowTempsCompilation()
        {
            string source = @"
            namespace ReferencedAssembly
            {
                public class HighLowTemps
                {
                    public int High { get; set; }
                    public int Low { get; set; }
                }
            }";

            return CreateCompilation(source);
        }
    }
}
