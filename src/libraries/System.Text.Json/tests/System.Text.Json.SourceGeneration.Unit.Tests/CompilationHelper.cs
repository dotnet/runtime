// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class CompilationHelper
    {
        private static readonly CSharpParseOptions s_parseOptions =
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse)
            // workaround https://github.com/dotnet/roslyn/pull/55866. We can remove "LangVersion=Preview" when we get a Roslyn build with that change.
            .WithLanguageVersion(LanguageVersion.Preview);

        public static Compilation CreateCompilation(
            string source,
            MetadataReference[] additionalReferences = null,
            string assemblyName = "TestAssembly",
            bool includeSTJ = true)
        {
            // Bypass System.Runtime error.
            Assembly systemRuntimeAssembly = Assembly.Load("System.Runtime, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            Assembly systemCollectionsAssembly = Assembly.Load("System.Collections, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            string systemRuntimeAssemblyPath = systemRuntimeAssembly.Location;
            string systemCollectionsAssemblyPath = systemCollectionsAssembly.Location;

            List<MetadataReference> references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Type).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(KeyValuePair).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ContractNamespaceAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JavaScriptEncoder).Assembly.Location),
                MetadataReference.CreateFromFile(systemRuntimeAssemblyPath),
                MetadataReference.CreateFromFile(systemCollectionsAssemblyPath),
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

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, s_parseOptions) },
                references: references.ToArray(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        public static Compilation RunGenerators(
            Compilation compilation,
            out ImmutableArray<Diagnostic> diagnostics,
#if ROSLYN4_0_OR_GREATER
            params IIncrementalGenerator[] generators)
        {
            CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: generators.Select(g => g.AsSourceGenerator()),
                parseOptions: s_parseOptions);
#else
            params ISourceGenerator[] generators)
        {
            CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: generators,
                parseOptions: s_parseOptions);
#endif

            driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outCompilation, out diagnostics);
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

        public static Compilation CreateRepeatedLocationsCompilation()
        {
            string source = @"
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
            }";

            return CreateCompilation(source);
        }

        public static Compilation CreateRepeatedLocationsWithResolutionCompilation()
        {
            string source = @"
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
            }";

            return CreateCompilation(source);
        }
        
        public static Compilation CreateCompilationWithInitOnlyProperties()
        {
            string source = @"
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
            }";

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithConstructorInitOnlyProperties()
        {
            string source = @"
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
            }";

            return CreateCompilation(source);
        }
        
        public static Compilation CreateCompilationWithMixedInitOnlyProperties()
        {
            string source = @"
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
            }";

            return CreateCompilation(source);
        }
        
        public static Compilation CreateCompilationWithRecordPositionalParameters()
        {
            string source = @"
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
            }";

            return CreateCompilation(source);
        }
        
        public static Compilation CreateCompilationWithInaccessibleJsonIncludeProperties()
        {
            string source = @"
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
            }";

            return CreateCompilation(source);
        }

        public static Compilation CreateReferencedLibRecordCompilation()
        {
            string source = @"
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
";

            return CreateCompilation(source);
        }

            public static Compilation CreateReferencedSimpleLibRecordCompilation()
            {
                string source = @"
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
";

                return CreateCompilation(source);
        }

        internal static void CheckDiagnosticMessages(
            DiagnosticSeverity level,
            ImmutableArray<Diagnostic> diagnostics,
            (Location Location, string Message)[] expectedDiags,
            bool sort = true)
        {
            (Location Location, string Message)[] actualDiags = diagnostics
                .Where(diagnostic => diagnostic.Severity == level)
                .Select(diagnostic => (diagnostic.Location, diagnostic.GetMessage()))
                .ToArray();

            if (sort)
            {
                // Can't depend on reflection order when generating type metadata.
                Array.Sort(actualDiags);
                Array.Sort(expectedDiags);
            }

            if (CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(expectedDiags, actualDiags);
            }
            else
            {
                // for non-English runs, just compare the number of messages are the same
                Assert.Equal(expectedDiags.Length, actualDiags.Length);
            }
        }
    }
}
