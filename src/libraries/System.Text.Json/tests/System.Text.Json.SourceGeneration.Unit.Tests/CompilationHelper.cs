﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public record JsonSourceGeneratorResult
    {
        public Compilation NewCompilation { get; set; }
        public SourceGenerationSpec? SourceGenModel { get; set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; set; }

        public IEnumerable<TypeGenerationSpec> AllGeneratedTypes
            => SourceGenModel is { } model
                ? model.ContextGenerationSpecs.SelectMany(ctx => ctx.GeneratedTypes)
                : Array.Empty<TypeGenerationSpec>();

        public void AssertContainsType(string fullyQualifiedName)
            => Assert.Contains(
                    AllGeneratedTypes,
                    spec => spec.TypeRef.FullyQualifiedName == fullyQualifiedName);
    }

    public class CompilationHelper
    {
        private static readonly CSharpParseOptions s_parseOptions =
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);

#if ROSLYN4_0_OR_GREATER
        private static readonly GeneratorDriverOptions s_generatorDriverOptions = new GeneratorDriverOptions(disabledOutputs: IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);
#endif

#if NETCOREAPP
        private static readonly Assembly systemRuntimeAssembly = Assembly.Load(new AssemblyName("System.Runtime"));
#endif

        public static Compilation CreateCompilation(
            string source,
            MetadataReference[] additionalReferences = null,
            string assemblyName = "TestAssembly",
            bool includeSTJ = true,
            Func<CSharpParseOptions, CSharpParseOptions> configureParseOptions = null)
        {

            List<MetadataReference> references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Type).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(KeyValuePair<,>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ContractNamespaceAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JavaScriptEncoder).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ReadOnlySpan<>).Assembly.Location),
#if NETCOREAPP
                MetadataReference.CreateFromFile(typeof(LinkedList<>).Assembly.Location),
                MetadataReference.CreateFromFile(systemRuntimeAssembly.Location),
#endif
            };

            if (includeSTJ)
            {
                references.Add(MetadataReference.CreateFromFile(typeof(JsonSerializerOptions).Assembly.Location));
            }

            // Add additional references as needed.
            if (additionalReferences != null)
            {
                foreach (MetadataReference reference in additionalReferences)
                {
                    references.Add(reference);
                }
            }

            var parseOptions = configureParseOptions?.Invoke(s_parseOptions) ?? s_parseOptions;
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
                references: references.ToArray(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        public static SyntaxTree ParseSource(string source)
            => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

        public static CSharpGeneratorDriver CreateJsonSourceGeneratorDriver(JsonSourceGenerator? generator = null)
        {
            generator ??= new();
            return
#if ROSLYN4_0_OR_GREATER
                CSharpGeneratorDriver.Create(
                    generators: new ISourceGenerator[] { generator.AsSourceGenerator() },
                    parseOptions: s_parseOptions,
                    driverOptions: new GeneratorDriverOptions(
                        disabledOutputs: IncrementalGeneratorOutputKind.None,
                        trackIncrementalGeneratorSteps: true));
#else
                CSharpGeneratorDriver.Create(
                    generators: new ISourceGenerator[] { generator },
                    parseOptions: s_parseOptions);
#endif
        }

        public static JsonSourceGeneratorResult RunJsonSourceGenerator(Compilation compilation)
        {
            SourceGenerationSpec? generatedSpec = null;
            var generator = new JsonSourceGenerator
            {
                OnSourceEmitting = spec => generatedSpec = spec
            };

            CSharpGeneratorDriver driver = CreateJsonSourceGeneratorDriver(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outCompilation, out ImmutableArray<Diagnostic> diagnostics);
            return new()
            {
                NewCompilation = outCompilation,
                Diagnostics = diagnostics,
                SourceGenModel = generatedSpec,
            };
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
            string source = """
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
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCampaignSummaryViewModelCompilation()
        {
            string source = """
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
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateActiveOrUpcomingEventCompilation()
        {
            string source = """
                using System;
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
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateReferencedHighLowTempsCompilation()
        {
            string source = """
                namespace ReferencedAssembly
                {
                    public class HighLowTemps
                    {
                        public int High { get; set; }
                        public int Low { get; set; }
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateRepeatedLocationsCompilation()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace JsonSourceGeneration
                {
                    [JsonSerializable(typeof(Fake.Location))]
                    [JsonSerializable(typeof(HelloWorld.Location))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }
                }

                namespace Fake
                {
                    public class Location
                    {
                        public int FakeId { get; set; }
                        public string FakeAddress1 { get; set; }
                        public string FakeAddress2 { get; set; }
                        public string FakeCity { get; set; }
                        public string FakeState { get; set; }
                        public string FakePostalCode { get; set; }
                        public string FakeName { get; set; }
                        public string FakePhoneNumber { get; set; }
                        public string FakeCountry { get; set; }
                    }
                }

                namespace HelloWorld
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
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateRepeatedLocationsWithResolutionCompilation()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                [assembly: JsonSerializable(typeof(Fake.Location))]
                [assembly: JsonSerializable(typeof(HelloWorld.Location), TypeInfoPropertyName = ""RepeatedLocation"")]

                namespace Fake
                {
                    public class Location
                    {
                        public int FakeId { get; set; }
                        public string FakeAddress1 { get; set; }
                        public string FakeAddress2 { get; set; }
                        public string FakeCity { get; set; }
                        public string FakeState { get; set; }
                        public string FakePostalCode { get; set; }
                        public string FakeName { get; set; }
                        public string FakePhoneNumber { get; set; }
                        public string FakeCountry { get; set; }
                    }
                }

                namespace HelloWorld
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
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithInitOnlyProperties()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {                
                    public class Location
                    {
                        public int Id { get; init; }
                        public string Address1 { get; init; }
                        public string Address2 { get; init; }
                        public string City { get; init; }
                        public string State { get; init; }
                        public string PostalCode { get; init; }
                        public string Name { get; init; }
                        public string PhoneNumber { get; init; }
                        public string Country { get; init; }
                    }

                    [JsonSerializable(typeof(Location))]
                    public partial class MyJsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithConstructorInitOnlyProperties()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                { 
                    public class MyClass
                    {
                        public MyClass(int value)
                        {
                            Value = value;
                        }

                        public int Value { get; init; }
                    }

                    [JsonSerializable(typeof(MyClass))]
                    public partial class MyJsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithMixedInitOnlyProperties()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    public class MyClass
                    {
                        public MyClass(int value)
                        {
                            Value = value;
                        }

                        public int Value { get; init; }
                        public string Orphaned { get; init; }
                    }

                    [JsonSerializable(typeof(MyClass))]
                    public partial class MyJsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithRequiredProperties()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    public class MyClass
                    {
                        public required string Required1 { get; set; }
                        public required string Required2 { get; set; }

                        public MyClass(string required1)
                        {
                            Required1 = required1;
                        }
                    }

                    [JsonSerializable(typeof(MyClass))]
                    public partial class MyJsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithRecordPositionalParameters()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {                
                    public record Location
                    (
                        int Id,
                        string Address1,
                        string Address2,
                        string City,
                        string State,
                        string PostalCode,
                        string Name,
                        string PhoneNumber,
                        string Country
                    );

                    [JsonSerializable(typeof(Location))]
                    public partial class MyJsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithInaccessibleJsonIncludeProperties()
        {
            string source = """
                using System;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {                
                    public class Location
                    {
                        [JsonInclude]
                        public int Id { get; private set; }
                        [JsonInclude]
                        public string Address1 { get; internal set; }
                        [JsonInclude]
                        private string Address2 { get; set; }
                        [JsonInclude]
                        public string PhoneNumber { internal get; set; }
                        [JsonInclude]
                        public string Country { private get; set; }
                    }

                    [JsonSerializable(typeof(Location))]
                    public partial class MyJsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateReferencedLibRecordCompilation()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace ReferencedAssembly
                {
                    public record LibRecord(int Id)
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

            return CreateCompilation(source);
        }

        public static Compilation CreateReferencedSimpleLibRecordCompilation()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace ReferencedAssembly
                {
                    public record LibRecord
                    {
                        public int Id { get; set; }
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

            return CreateCompilation(source);
        }

        public static Compilation CreateReferencedModelWithFullyDocumentedProperties()
        {
            string source = """
                namespace ReferencedAssembly
                {
                    /// <summary>
                    /// Documentation
                    /// </summary>
                    public class Model
                    {
                        /// <summary>
                        /// Documentation
                        /// </summary>
                        public int Property1 { get; set; }

                        /// <summary>
                        /// Documentation
                        /// </summary>
                        public int Property2 { get; set; }
                    }
                }
                """;

            return CreateCompilation(source);
        }

        internal static void CheckDiagnosticMessages(
            DiagnosticSeverity level,
            ImmutableArray<Diagnostic> diagnostics,
            (Location Location, string Message)[] expectedDiags,
            bool sort = true)
        {
            ((string FileName, TextSpan, LinePositionSpan), string)[] actualDiags = diagnostics
                .Where(diagnostic => diagnostic.Severity == level)
                .Select(diagnostic => (GetLocationNormalForm(diagnostic.Location), diagnostic.GetMessage()))
                .ToArray();

            if (CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                ((string FileName, TextSpan, LinePositionSpan), string Message)[] expectedDiagsNormalized = expectedDiags
                    .Select(diag => (GetLocationNormalForm(diag.Location), diag.Message))
                    .ToArray();

                if (sort)
                {
                    // Can't depend on reflection order when generating type metadata.
                    Array.Sort(actualDiags);
                    Array.Sort(expectedDiagsNormalized);
                }

                Assert.Equal(expectedDiagsNormalized, actualDiags);
            }
            else
            {
                // for non-English runs, just compare the number of messages are the same
                Assert.Equal(expectedDiags.Length, actualDiags.Length);
            }

            static (string FileName, TextSpan, LinePositionSpan) GetLocationNormalForm(Location location)
                => (location.SourceTree?.FilePath ?? "", location.SourceSpan, location.GetLineSpan().Span);
        }
    }
}
