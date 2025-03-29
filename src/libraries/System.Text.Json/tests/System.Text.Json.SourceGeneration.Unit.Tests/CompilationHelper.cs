// Licensed to the .NET Foundation under one or more agreements.
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
        public ImmutableArray<ContextGenerationSpec> ContextGenerationSpecs { get; set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; set; }

        public IEnumerable<TypeGenerationSpec> AllGeneratedTypes
            => ContextGenerationSpecs.SelectMany(ctx => ctx.GeneratedTypes);

        public void AssertContainsType(string fullyQualifiedName)
            => Assert.Contains(
                    AllGeneratedTypes,
                    spec => spec.TypeRef.FullyQualifiedName == fullyQualifiedName);
    }

    public static class CompilationHelper
    {
        private readonly static CSharpParseOptions s_defaultParseOptions = CreateParseOptions();

        public static CSharpParseOptions CreateParseOptions(
            LanguageVersion? version = null,
            DocumentationMode? documentationMode = null)
        {
            return new CSharpParseOptions(
                kind: SourceCodeKind.Regular,
                languageVersion: version ?? LanguageVersion.CSharp9, // C# 9 is the minimum supported lang version by the source generator.
                documentationMode: documentationMode ?? DocumentationMode.Parse);
        }

#if NET
        private static readonly Assembly systemRuntimeAssembly = Assembly.Load(new AssemblyName("System.Runtime"));
#endif

        public static Compilation CreateCompilation(
            string source,
            MetadataReference[] additionalReferences = null,
            string assemblyName = "TestAssembly",
            bool includeSTJ = true,
            CSharpParseOptions? parseOptions = null)
        {

            List<MetadataReference> references = new() {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Type).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(KeyValuePair<,>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ContractNamespaceAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JavaScriptEncoder).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ReadOnlySpan<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
#if NET
                MetadataReference.CreateFromFile(typeof(LinkedList<>).Assembly.Location),
                MetadataReference.CreateFromFile(systemRuntimeAssembly.Location),
#else
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.Unsafe).Assembly.Location),
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

            parseOptions ??= s_defaultParseOptions;
            SyntaxTree[] syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(source, parseOptions),
#if !NET
                CSharpSyntaxTree.ParseText(NetfxPolyfillAttributes, parseOptions),
#endif
            };

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: syntaxTrees,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        public static SyntaxTree ParseSource(string source, CSharpParseOptions? options = null)
            => CSharpSyntaxTree.ParseText(source, options ?? s_defaultParseOptions);

        public static CSharpGeneratorDriver CreateJsonSourceGeneratorDriver(Compilation compilation, JsonSourceGenerator? generator = null)
        {
            generator ??= new();
            CSharpParseOptions parseOptions = compilation.SyntaxTrees
                .OfType<CSharpSyntaxTree>()
                .Select(tree => tree.Options)
                .FirstOrDefault() ?? s_defaultParseOptions;

            return
#if ROSLYN4_0_OR_GREATER
                CSharpGeneratorDriver.Create(
                    generators: new ISourceGenerator[] { generator.AsSourceGenerator() },
                    parseOptions: parseOptions,
                    driverOptions: new GeneratorDriverOptions(
                        disabledOutputs: IncrementalGeneratorOutputKind.None,
                        trackIncrementalGeneratorSteps: true));
#else
                CSharpGeneratorDriver.Create(
                    generators: new ISourceGenerator[] { generator },
                    parseOptions: parseOptions);
#endif
        }

        public static JsonSourceGeneratorResult RunJsonSourceGenerator(Compilation compilation, bool disableDiagnosticValidation = false)
        {
            var generatedSpecs = ImmutableArray<ContextGenerationSpec>.Empty;
            var generator = new JsonSourceGenerator
            {
                OnSourceEmitting = specs => generatedSpecs = specs
            };

            CSharpGeneratorDriver driver = CreateJsonSourceGeneratorDriver(compilation, generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outCompilation, out ImmutableArray<Diagnostic> diagnostics);

            if (!disableDiagnosticValidation)
            {
                outCompilation.GetDiagnostics().AssertMaxSeverity(DiagnosticSeverity.Info);
                diagnostics.AssertMaxSeverity(DiagnosticSeverity.Info);
            }

            return new()
            {
                NewCompilation = outCompilation,
                Diagnostics = diagnostics,
                ContextGenerationSpecs = generatedSpecs,
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

#if !NET
        private const string NetfxPolyfillAttributes = """
            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit { }

                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
                internal sealed class RequiredMemberAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
                internal sealed class CompilerFeatureRequiredAttribute : Attribute
                {
                    public CompilerFeatureRequiredAttribute(string featureName) { }
                }
            }
            """;
#endif

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

            return CreateCompilation(source, assemblyName: "CampaignSummaryAssembly");
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

        public static Compilation CreateCompilationWithInitOnlyProperties()
        {
            string source = """
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

#if ROSLYN4_4_OR_GREATER
        public static Compilation CreateCompilationWithRequiredProperties()
        {
            string source = """
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

            CSharpParseOptions parseOptions = CreateParseOptions(LanguageVersion.CSharp11);
            return CreateCompilation(source, parseOptions: parseOptions);
        }
#endif

        public static Compilation CreateCompilationWithRecordPositionalParameters()
        {
            string source = """
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
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {                
                    public class Location
                    {
                        [JsonInclude]
                        public int publicField = 1;
                        [JsonInclude]
                        internal int internalField = 2;
                        [JsonInclude]
                        private int privateField = 4;
                        [JsonInclude]
                        protected int protectedField = 8;

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
                        [JsonInclude]
                        internal string InternalProperty { get; set; }
                        [JsonInclude]
                        protected string ProtectedProperty { get; set; }
                        [JsonInclude]
                        internal string InternalPropertyWithPrivateGetter { private get; set; }
                        [JsonInclude]
                        internal string InternalPropertyWithPrivateSetter { get; private set; }

                        public int GetPrivateField() => privateField;
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

        public static Compilation CreatePolymorphicClassOnFastPathContext()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyBaseClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [JsonDerivedType(typeof(MyDerivedClass), "derived")]
                    public class MyBaseClass
                    {
                    }

                    public class MyDerivedClass : MyBaseClass
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateTypesAnnotatedWithJsonStringEnumConverter()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyClass))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class MyClass
                    {
                        public Enum1 Enum1Prop { get; set; }

                        [JsonConverter(typeof(JsonStringEnumConverter))]
                        public Enum2 Enum2Prop { get; set; }
                    }

                    [JsonConverter(typeof(JsonStringEnumConverter))]
                    public enum Enum1 { A, B, C };
                    
                    public enum Enum2 { A, B, C };
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateTypesWithInvalidJsonConverterAttributeType()
        {
            string source = """
                using System;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(MyClass))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    public class MyClass
                    {
                        [JsonConverter(null)]
                        public int Value1 { get; set; }

                        [JsonConverter(typeof(int))]
                        public int Value2 { get; set; }

                        [JsonConverter(typeof(InacessibleConverter))]
                        public int Value3 { get; set; }
                    }

                    public class InacessibleConverter : JsonConverter<int>
                    {
                        private InacessibleConverter()
                        { }

                        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                        {
                            throw new NotImplementedException();
                        }

                        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithDerivedJsonConverterAttributeAnnotations()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(ClassWithConverterDeclaration))]
                    [JsonSerializable(typeof(ClassWithPropertyConverterDeclaration))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }

                    [MyJsonConverter]
                    public class ClassWithConverterDeclaration
                    {
                    }

                    public partial class ClassWithPropertyConverterDeclaration : JsonSerializerContext
                    {
                        [MyJsonConverter]
                        public int Value { get; set; }
                    }

                    public class MyJsonConverterAttribute : JsonConverterAttribute
                    {
                        public override JsonConverter? CreateConverter(Type typeToConvert) => null;
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateContextWithUnboundGenericTypeDeclarations()
        {
            string source = """
                using System.Collections.Generic;
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(List<>))]
                    [JsonSerializable(typeof(Dictionary<,>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateContextWithErrorTypeDeclarations()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {
                    [JsonSerializable(typeof(BogusType))]
                    [JsonSerializable(typeof(BogusType<int>))]
                    internal partial class JsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        public static Compilation CreateCompilationWithJsonConstructorAttributeAnnotations()
        {
            string source = """
                using System.Text.Json.Serialization;

                namespace HelloWorld
                {                
                    public class ClassWithPublicCtor
                    {
                        [JsonConstructor]
                        public ClassWithPublicCtor() { }
                    }

                    public class ClassWithInternalCtor
                    {
                        [JsonConstructor]
                        internal ClassWithInternalCtor() { }
                    }

                    public class ClassWithProtectedCtor
                    {
                        [JsonConstructor]
                        protected ClassWithProtectedCtor() { }
                    }

                    public class ClassWithPrivateCtor
                    {
                        [JsonConstructor]
                        private ClassWithPrivateCtor() { }
                    }

                    [JsonSerializable(typeof(ClassWithPublicCtor))]
                    [JsonSerializable(typeof(ClassWithInternalCtor))]
                    [JsonSerializable(typeof(ClassWithProtectedCtor))]
                    [JsonSerializable(typeof(ClassWithPrivateCtor))]
                    public partial class MyJsonContext : JsonSerializerContext
                    {
                    }
                }
                """;

            return CreateCompilation(source);
        }

        internal static void AssertEqualDiagnosticMessages(
            IEnumerable<DiagnosticData> expectedDiags,
            IEnumerable<Diagnostic> actualDiags)
        {
            HashSet<DiagnosticData> expectedSet = new(expectedDiags);
            HashSet<DiagnosticData> actualSet = new(actualDiags.Select(d => new DiagnosticData(d.Severity, d.Location, d.GetMessage())));
            AssertExtensions.Equal(expectedSet, actualSet);
        }

        internal static Location? GetLocation(this AttributeData attributeData)
        {
            SyntaxReference? reference = attributeData.ApplicationSyntaxReference;
            return reference?.SyntaxTree.GetLocation(reference.Span);
        }

        internal static void AssertMaxSeverity(this IEnumerable<Diagnostic> diagnostics, DiagnosticSeverity maxSeverity)
        {
            Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Severity > maxSeverity));
        }
    }

    public record struct DiagnosticData(
        DiagnosticSeverity Severity,
        string FilePath,
        LinePositionSpan LinePositionSpan,
        string Message)
    {
        public DiagnosticData(DiagnosticSeverity severity, Location location, string message)
            : this(severity, location.SourceTree?.FilePath ?? "", location.GetLineSpan().Span, TrimCultureSensitiveMessage(message))
        {
        }

        // for non-English runs, trim the message content since it might be translated.
        private static string TrimCultureSensitiveMessage(string message) => s_IsEnglishCulture ? message : "";
        private readonly static bool s_IsEnglishCulture = CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        public override string ToString() => $"{Severity}, {Message}, {FilePath}@{LinePositionSpan}";
    }
}
