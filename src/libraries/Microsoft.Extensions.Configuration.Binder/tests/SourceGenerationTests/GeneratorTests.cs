// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBindingGeneratorTests : ConfigurationBinderTestsBase
    {
        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.CSharp10)]
        public async Task LangVersionMustBeCharp12OrHigher(LanguageVersion langVersion)
        {
            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(BindCallSampleCode, langVersion: langVersion);
            Assert.False(result.GeneratedSource.HasValue);

            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.True(diagnostic.Id == "SYSLIB1102");
            Assert.Contains("C# 12", diagnostic.Descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));
            Assert.Contains("C# 12", diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task ValueTypesAreInvalidAsBindInputs()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                	public static void Main()
                	{
                		ConfigurationBuilder configurationBuilder = new();
                		IConfigurationRoot config = configurationBuilder.Build();

                        int myInt = 1
                		config.Bind(myInt);
                        int? myNInt = 2;
                        config.Bind(myNInt)

                        var myStruct = new MyStruct()
                        config.Bind(myStruct, options => { })
                        MyStruct? myNStruct = new();
                        config.Bind(myNStruct, options => { });

                        var myRecordStruct = new MyRecordStruct();
                        config.Bind("key", myRecordStruct);
                        MyRecordStruct? myNRecordStruct = new();
                        config.Bind("key", myNRecordStruct);

                        Memory<int> memory = new(new int[] {1, 2, 3});
                        config.Bind(memory);
                	}

                    public struct MyStruct { }
                    public record struct MyRecordStruct { }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.False(result.GeneratedSource.HasValue);
            Assert.Equal(7, result.Diagnostics.Count());

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                Assert.True(diagnostic.Id == Diagnostics.ValueTypesInvalidForBind.Id);
                Assert.Contains(Diagnostics.ValueTypesInvalidForBind.Title, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.NotNull(diagnostic.Location);
            }
        }

        [Fact]
        public async Task InvalidRootMethodInputTypes()
        {
            string source = """
                using System.Collections.Generic;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        config.GetValue(typeof(int*), "");
                        config.Get<Dictionary<string, T>>();
                    }

                    public struct MyStruct { }
                    public record struct MyRecordStruct { }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.False(result.GeneratedSource.HasValue);
            Assert.Equal(2, result.Diagnostics.Count());

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                Assert.True(diagnostic.Id == Diagnostics.CouldNotDetermineTypeInfo.Id);
                Assert.Contains(Diagnostics.CouldNotDetermineTypeInfo.Title, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.NotNull(diagnostic.Location);
            }
        }

        [Fact]
        public async Task CannotDetermineTypeInfo()
        {
            string source = """
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                public class Program
                {
                	public static void Main()
                	{
                		ConfigurationBuilder configurationBuilder = new();
                		IConfiguration config = configurationBuilder.Build();

                		PerformGenericBinderCalls<MyClass>(config);
                	}

                    public static void PerformGenericBinderCalls<T>(IConfiguration config) where T : class
                    {
                        config.Get<T>();
                        config.Get<T>(binderOptions => { });
                        config.GetValue<T>("key");
                        config.GetValue<T>("key", default(T));

                        IConfigurationSection section = config.GetSection("MySection");
                		ServiceCollection services = new();
                        services.Configure<T>(section);
                    }

                    private void BindOptions(IConfiguration config, object? instance)
                    {
                        config.Bind(instance);
                    }

                    public class MyClass { }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.False(result.GeneratedSource.HasValue);
            Assert.Equal(6, result.Diagnostics.Count());

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                Assert.True(diagnostic.Id == Diagnostics.CouldNotDetermineTypeInfo.Id);
                Assert.Contains(Diagnostics.CouldNotDetermineTypeInfo.Title, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.NotNull(diagnostic.Location);
            }
        }

        [Fact]
        public async Task SucceedWhenGivenConflictingTypeNames()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/93498

            string source = """
                using Microsoft.Extensions.Configuration;

                var c = new ConfigurationBuilder().Build();
                c.Get<Foo.Bar.BType>();

                namespace Microsoft.Foo
                {
                    internal class AType {}
                }

                namespace Foo.Bar
                {
                    internal class BType {}
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public async Task IgnorePropertiesWithUnresolvableMetadataTypes()
        {
            CSharpCompilationOptions compilationOptions = new(OutputKind.DynamicallyLinkedLibrary);
            MetadataReference[] commonReferences = s_compilationAssemblyRefs
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToArray();

            CSharpCompilation transitiveDependencyCompilation = CSharpCompilation.Create(
                assemblyName: $"TransitiveDependency_{Guid.NewGuid():N}",
                syntaxTrees:
                [
                    CSharpSyntaxTree.ParseText("""
                        namespace MissingTypes;

                        public struct ValueTypeMessage {}
                        public sealed class HttpRequestMessage {}
                        public sealed class CredentialDescription {}
                        """)
                ],
                references: commonReferences,
                options: compilationOptions);

            byte[] transitiveDependencyImage = CreateAssemblyImage(transitiveDependencyCompilation);
            MetadataReference transitiveDependencyReference = MetadataReference.CreateFromImage(transitiveDependencyImage);

            CSharpCompilation modelCompilation = CSharpCompilation.Create(
                assemblyName: $"UnresolvableModel_{Guid.NewGuid():N}",
                syntaxTrees:
                [
                    CSharpSyntaxTree.ParseText("""
                        namespace UnresolvableModel;

                        public sealed class Wrapper<T>
                        {
                            public int Count { get; set; }

                            public sealed class Inner
                            {
                                public int Value { get; set; }
                            }
                        }

                        public class DstsOptionsBase
                        {
                            public virtual MissingTypes.HttpRequestMessage? OverriddenMessage { get; set; }
                            public MissingTypes.HttpRequestMessage? ShadowedMessage { get; set; }
                        }

                        public sealed class DstsOptions : DstsOptionsBase
                        {
                            public MissingTypes.ValueTypeMessage? ValueTypeMessage { get; set; }
                            public MissingTypes.HttpRequestMessage? HttpRequestMessage { get; set; }
                            public MissingTypes.CredentialDescription? CredentialDescription { get; set; }
                            public Wrapper<MissingTypes.HttpRequestMessage>? WrappedMessage { get; set; }
                            public Wrapper<MissingTypes.HttpRequestMessage>.Inner? NestedInnerMessage { get; set; }
                            public System.Tuple<int, MissingTypes.CredentialDescription>? TupleMessage { get; set; }
                            public override MissingTypes.HttpRequestMessage? OverriddenMessage { get; set; }
                            public new MissingTypes.HttpRequestMessage? ShadowedMessage { get; set; }
                            public int Value { get; set; }
                        }
                        """)
                ],
                references: commonReferences.Concat([transitiveDependencyReference]),
                options: compilationOptions);

            // Reference the model as an in-memory metadata reference and omit the transitive dependency, so the
            // generator sees the affected member types as unresolved error symbols. Using a metadata reference
            // (rather than writing the assemblies to disk and using Assembly.LoadFrom) avoids locking files and
            // leaking a temp directory on every run.
            MetadataReference modelReference = MetadataReference.CreateFromImage(CreateAssemblyImage(modelCompilation));

            string source = """
                using Microsoft.Extensions.Configuration;
                using UnresolvableModel;

                public static class Program
                {
                    public static void Main()
                    {
                        var configuration = new ConfigurationBuilder().Build();
                        _ = configuration.Get<DstsOptions>();
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, metadataReferences: [modelReference]);

            result.ValidateDiagnostics(ExpectedDiagnostics.None);
            Assert.NotNull(result.GeneratedSource);
            Assert.Contains("instance.Value = ", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("ValueTypeMessage", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("HttpRequestMessage", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("CredentialDescription", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("WrappedMessage", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("NestedInnerMessage", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("TupleMessage", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("OverriddenMessage", result.GeneratedSource.Value.SourceText.ToString());
            Assert.DoesNotContain("ShadowedMessage", result.GeneratedSource.Value.SourceText.ToString());

            // Each skipped member surfaces a SYSLIB1101 warning so the incomplete binding is not silent.
            foreach (string skippedProperty in new[] { "ValueTypeMessage", "HttpRequestMessage", "CredentialDescription", "WrappedMessage", "NestedInnerMessage", "TupleMessage" })
            {
                Assert.Contains(result.Diagnostics, diagnostic =>
                    diagnostic.Id == "SYSLIB1101" &&
                    diagnostic.Severity == DiagnosticSeverity.Warning &&
                    diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains($"'{skippedProperty}'"));
            }

            // An error-typed property that is overridden or `new`-shadowed must report SYSLIB1101 exactly once,
            // not once per occurrence while walking the inheritance chain.
            foreach (string shadowedProperty in new[] { "OverriddenMessage", "ShadowedMessage" })
            {
                Assert.Equal(1, result.Diagnostics.Count(diagnostic =>
                    diagnostic.Id == "SYSLIB1101" &&
                    diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains($"'{shadowedProperty}'")));
            }

            // The bindable member must still bind without a diagnostic.
            Assert.DoesNotContain(result.Diagnostics, diagnostic =>
                diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("'Value'"));
        }

        [Fact]
        public async Task SucceedWhenGivenMinimumRequiredReferences()
        {
            string source = """
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        config.Bind(new MyClass0());
                    }

                    public class MyClass0 { }
                }
                """;

            HashSet<Type> exclusions = new()
            {
                typeof(CultureInfo),
                typeof(IServiceCollection),
                typeof(IDictionary),
                typeof(ServiceCollection),
                typeof(OptionsBuilder<>),
                typeof(OptionsConfigurationServiceCollectionExtensions),
                typeof(Uri)
            };

            await Test(expectOutput: true);

            exclusions.Add(typeof(ConfigurationBinder));
            await Test(expectOutput: false);

            exclusions.Remove(typeof(ConfigurationBinder));
            exclusions.Add(typeof(IConfiguration));
            await Test(expectOutput: false);

            async Task Test(bool expectOutput)
            {
                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetFilteredAssemblyRefs(exclusions));
                Assert.Empty(result.Diagnostics);
                Action ValidateSourceResult = expectOutput ? () => Assert.NotNull(result.GeneratedSource) : () => Assert.False(result.GeneratedSource.HasValue);
                ValidateSourceResult();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task ListOfTupleTest()
        {
            string source = """
                using Microsoft.Extensions.Configuration;
                using System;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        var settingsSection = config.GetSection("Settings");

                        Settings options = settingsSection.Get<Settings>()!;
                    }
                }

                public class Settings
                {
                    public List<(string Item1, string? Item2)>? Items { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        // Required, settable property whose value flows through a constructor marked [SetsRequiredMembers]
        // whose parameter name differs only in casing from the property.
        [InlineData("""
            [method: SetsRequiredMembers]
            public class GreetSettings(string name)
            {
                public string Greeting { get; set; } = "Hello";
                public required string Name { get; set; } = name;
            }
            """)]
        // Same as above, but the constructor parameter name matches the property name exactly.
        [InlineData("""
            [method: SetsRequiredMembers]
            public class GreetSettings(string Name)
            {
                public string Greeting { get; set; } = "Hello";
                public required string Name { get; set; } = Name;
            }
            """)]
        // Required property set via a constructor parameter, but the constructor does not set required
        // members, so the property must still be assigned through the object initializer.
        [InlineData("""
            public class GreetSettings(string name)
            {
                public string Greeting { get; set; } = "Hello";
                public required string Name { get; set; } = name;
            }
            """)]
        public async Task RequiredPropertyWithMatchingConstructorParameter(string greetSettingsType)
        {
            string source = $$"""
                using System.Diagnostics.CodeAnalysis;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        GreetSettings settings = config.GetSection("Settings").Get<GreetSettings>()!;
                    }
                }

                {{greetSettingsType}}
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task ListOfTupleWithComplexElementInInternalPropertyTest()
        {
            string source = """
                using Microsoft.Extensions.Configuration;
                using System;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        ExampleOptions options = new();
                        config.Bind(options);
                    }
                }

                public class ExampleOptions
                {
                    public List<string> ExampleCollection { get; set; } = new();

                    internal List<(string, ICollection<string>?)> UsesCollection =>
                        [
                            ("Label-1", ExampleCollection)
                        ];
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData("IReadOnlyList")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlySet")]
        [InlineData("IEnumerable")]
        public async Task ReadOnlyCollectionConstructorParameterIsBindable(string collectionType)
        {
            string source = $$"""
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        Options options = config.Get<Options>();
                    }
                }

                public record Options(string Name, {{collectionType}}<string> Values);
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            // The collection type is only reachable through a read-only property, so its BindCore
            // helper must still be generated for the constructor parameter.
            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData("IReadOnlyList")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlySet")]
        [InlineData("IEnumerable")]
        public async Task SoleReadOnlyCollectionConstructorParameterIsBindable(string collectionType)
        {
            // Regression test: a type whose only member is a non-bindable, read-only collection
            // constructor parameter (no other bindable property) used to make the generator emit a
            // call to an Initialize method that was never generated, producing CS0103 at compile time.
            //
            // This only covers the top-level GetCore path; NestedSoleReadOnlyCollectionConstructorParameterIsBindable
            // covers the same shape reached as a nested member.
            string source = $$"""
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;

                public class Program
                {
                    public static object? Result;

                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Values:0"] = "a",
                            ["Values:1"] = "b",
                        });
                        IConfiguration config = configurationBuilder.Build();
                        Options options = config.Get<Options>();
                        Result = options.Values;
                    }
                }

                public record Options({{collectionType}}<string> Values);
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            // Compiling only proves the Initialize method the fix registers is emitted; loading and
            // running the assembly proves it also binds the right values, not just compilable code.
            var boundValues = (IEnumerable<string>)LoadAndInvokeMain(result.OutputCompilation, "Result")!;
            Assert.Equal(new[] { "a", "b" }, boundValues);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData("IReadOnlyList")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlySet")]
        [InlineData("IEnumerable")]
        public async Task NestedSoleReadOnlyCollectionConstructorParameterIsBindable(string collectionType)
        {
            // Regression test: the same shape as SoleReadOnlyCollectionConstructorParameterIsBindable, but
            // reached as a nested member rather than as the top-level bound type. The member was silently
            // left at null, since the emitter skipped every complex member without bindable members - even
            // one whose constructor parameters its Initialize method binds. Covers every way such a member is
            // reached: a constructor parameter (bound in Initialize), a settable property (bound in BindCore),
            // and a settable property with a matching constructor parameter (bound in Initialize when the
            // instance is created, and in BindCore when binding an existing one).
            string source = $$"""
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;

                public class Program
                {
                    public static object? Result;

                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Nested:Values:0"] = "a",
                            ["Nested:Values:1"] = "b",
                        });
                        IConfiguration config = configurationBuilder.Build();

                        Outer outer = config.Get<Outer>();
                        Holder holder = config.Get<Holder>();
                        Rebindable rebindable = config.Get<Rebindable>();

                        Rebindable existing = new(null!);
                        config.Bind(existing);

                        Result = new object?[]
                        {
                            outer.Nested?.Values,
                            holder.Nested?.Values,
                            rebindable.Nested?.Values,
                            existing.Nested?.Values,
                        };
                    }
                }

                public record Inner({{collectionType}}<string> Values);

                public record Outer(Inner Nested);

                public class Holder
                {
                    public Inner Nested { get; set; }
                }

                public class Rebindable
                {
                    public Rebindable(Inner nested) => Nested = nested;

                    public Inner Nested { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            var boundValues = (object?[])LoadAndInvokeMain(result.OutputCompilation, "Result")!;
            Assert.All(boundValues, boundValue => Assert.Equal(new[] { "a", "b" }, (IEnumerable<string>?)boundValue));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData("IReadOnlyList")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlySet")]
        [InlineData("IEnumerable")]
        public async Task NestedSoleReadOnlyCollectionConstructorParameterOfStructIsBindable(string collectionType)
        {
            // The value-type counterpart of NestedSoleReadOnlyCollectionConstructorParameterIsBindable. A struct
            // member is bound through a temporary (binding one in place would only mutate the copy its getter
            // returns), and that path never instantiated a type without bindable members - so a struct whose only
            // member is a read-only collection constructor parameter was left at its default. Covers a settable
            // property, a nullable one, a constructor parameter, a nullable constructor parameter, and a settable
            // property with a matching constructor parameter.
            string source = $$"""
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;

                public class Program
                {
                    public static object? Result;

                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Nested:Values:0"] = "a",
                            ["Nested:Values:1"] = "b",
                        });
                        IConfiguration config = configurationBuilder.Build();

                        Holder holder = config.Get<Holder>();
                        NullableHolder nullableHolder = config.Get<NullableHolder>();
                        Outer outer = config.Get<Outer>();
                        NullableOuter nullableOuter = config.Get<NullableOuter>();
                        Rebindable rebindable = config.Get<Rebindable>();

                        Rebindable existing = new(default);
                        config.Bind(existing);

                        Result = new object?[]
                        {
                            holder.Nested.Values,
                            nullableHolder.Nested?.Values,
                            outer.Nested.Values,
                            nullableOuter.Nested?.Values,
                            rebindable.Nested.Values,
                            existing.Nested.Values,
                        };
                    }
                }

                public readonly record struct Inner({{collectionType}}<string> Values);

                public class Holder
                {
                    public Inner Nested { get; set; }
                }

                public class NullableHolder
                {
                    public Inner? Nested { get; set; }
                }

                public record Outer(Inner Nested);

                public record NullableOuter(Inner? Nested);

                public class Rebindable
                {
                    public Rebindable(Inner nested) => Nested = nested;

                    public Inner Nested { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            var boundValues = (object?[])LoadAndInvokeMain(result.OutputCompilation, "Result")!;
            Assert.All(boundValues, boundValue => Assert.Equal(new[] { "a", "b" }, (IEnumerable<string>?)boundValue));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task ConstructorBoundSetOnlyMemberIsNotBoundTwice()
        {
            // The member predicate also determines whether a property already populated through a matching
            // constructor parameter is deferred behind boundThroughConstructor. A set-only property would otherwise
            // assign a second newly-initialized instance after construction instead of taking the usual ??= path.
            string source = """
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;

                public class Program
                {
                    public static object? Result;

                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Nested:Values:0"] = "a",
                        });
                        IConfiguration config = configurationBuilder.Build();

                        Parent parent = config.Get<Parent>();
                        Result = new object?[] { parent.SetterCalls, parent.NestedValue.Values };
                    }
                }

                public record Inner(IReadOnlyList<string> Values);

                public class Parent
                {
                    public Parent(Inner nested) => NestedValue = nested;

                    public Inner Nested
                    {
                        set
                        {
                            NestedValue = value;
                            SetterCalls++;
                        }
                    }

                    public Inner NestedValue { get; private set; }

                    public int SetterCalls { get; private set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            var values = (object?[])LoadAndInvokeMain(result.OutputCompilation, "Result")!;
            Assert.Equal(0, values[0]);
            Assert.Equal(new[] { "a" }, (IEnumerable<string>?)values[1]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task SoleReadOnlyCollectionConstructorParameterOfComplexElementIsBindable()
        {
            // Same regression as SoleReadOnlyCollectionConstructorParameterIsBindable, but the element
            // type is itself a bindable object rather than a string. ComplexReadOnlyListConstructorParameterIsBindable
            // below covers the complex-element case, but always pairs it with a second, ordinarily-bindable
            // property, so it never exercises the fixed code path (the type has bindable members either way).
            string source = """
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;
                using System.Linq;

                public class Program
                {
                    public static object? Result;

                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Values:0:Value"] = "a",
                            ["Values:1:Value"] = "b",
                        });
                        IConfiguration config = configurationBuilder.Build();
                        Options options = config.Get<Options>();
                        Result = options.Values.Select(v => v.Value).ToArray();
                    }
                }

                public class Child
                {
                    public string Value { get; set; }
                }

                public record Options(IReadOnlyList<Child> Values);
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            var boundValues = (string[])LoadAndInvokeMain(result.OutputCompilation, "Result")!;
            Assert.Equal(new[] { "a", "b" }, boundValues);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task ComplexReadOnlyListConstructorParameterIsBindable()
        {
            string source = """
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        Options options = config.Get<Options>();
                    }
                }

                public class Child
                {
                    public string Value { get; set; }
                }

                public record Options(string Name, IReadOnlyList<Child> Items);
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task TypeReachableOnlyThroughNonBindablePropertyIsNotEmitted()
        {
            string source = """
                using Microsoft.Extensions.Configuration;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        ExampleOptions options = new();
                        config.Bind(options);
                    }
                }

                public class ExampleOptions
                {
                    public List<string> ExampleCollection { get; set; } = new();

                    // Non-bindable internal property. UnreachableChild is only reachable through it,
                    // so the generator must not emit any binding code that references it.
                    internal List<UnreachableChild> UsesCollection => new();
                }

                public class UnreachableChild
                {
                    public string Value { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            // The type is reachable only through a non-bindable property, so the generator must
            // not register or emit any binding code that references it.
            Assert.DoesNotContain("UnreachableChild", result.GeneratedSource.Value.SourceText.ToString());

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData("List<AbstractElement>")]
        [InlineData("AbstractElement[]")]
        [InlineData("HashSet<AbstractElement>")]
        [InlineData("IReadOnlyList<AbstractElement>")]
        [InlineData("List<List<AbstractElement>>")]
        [InlineData("Dictionary<string, List<AbstractElement>>")]
        public async Task CollectionOfNonInstantiableElementsDoesNotEmitEmptyBindCore(string collectionType)
        {
            string source = $$"""
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        ExampleOptions options = new();
                        config.Bind(options);
                        _ = config.Get<{{collectionType}}>();

                        ServiceCollection services = new();
                        services.Configure<{{collectionType}}>(config);
                        services.Configure<ExampleOptions>(config);
                    }
                }

                public class ExampleOptions
                {
                    public {{collectionType}} Elements { get; set; }

                    public int Value { get; set; }
                }

                public abstract class AbstractElement
                {
                    public int Value { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                source,
                assemblyReferences: GetAssemblyRefsWithAdditional(
                    typeof(ConfigurationBuilder),
                    typeof(OptionsConfigurationServiceCollectionExtensions),
                    typeof(ServiceCollection),
                    typeof(IOptions<>),
                    typeof(List<>)));
            result.ValidateDiagnostics(ExpectedDiagnostics.FromGeneratorOnly);
            Assert.NotNull(result.GeneratedSource);

            string generated = result.GeneratedSource.Value.SourceText.ToString();
            SyntaxNode root = await CSharpSyntaxTree.ParseText(result.GeneratedSource.Value.SourceText).GetRootAsync();

            // Elements of the collection can never be created, so there is nothing to bind: the generator
            // must not emit a BindCore method whose only content is an enumeration of the config children.
            Assert.DoesNotContain(
                root.DescendantNodes().OfType<ForEachStatementSyntax>(),
                loop => loop.Statement is BlockSyntax { Statements.Count: 0 });

            // The element type itself can never be created, so it needs no binding logic. Nested cases keep a
            // BindCore for the outer collection because its elements are empty inner collections, which can be
            // created; matching on the end of the name excludes those without pinning the exact emitted name.
            MethodDeclarationSyntax[] bindCoreMethods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.ValueText == "BindCore")
                .ToArray();

            Assert.NotEmpty(bindCoreMethods);
            Assert.DoesNotContain(
                bindCoreMethods,
                method => method.ParameterList.Parameters.Any(
                    parameter => parameter.Type!.ToString().EndsWith("AbstractElement", StringComparison.Ordinal)));

            // The member is still recognized as bindable; it is assigned an empty collection.
            Assert.Contains("instance.Elements", generated);

            // Interception is preserved, and intercepted calls keep validating their arguments.
            Assert.Equal(4, Regex.Matches(generated, @"\[InterceptsLocation\(").Count);
            Assert.Contains("ArgumentNullException.ThrowIfNull(configuration);", generated);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData("TypeWithNoMembers")]
        [InlineData("System.Collections.Generic.List<AbstractElement>")]
        public async Task ConfigureOfTypeWithNothingToBindGeneratesNoOpBinding(string type)
        {
            string source = $$"""
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        ServiceCollection services = new();
                        services.Configure<{{type}}>(config);
                    }
                }

                public class TypeWithNoMembers
                {
                }

                public abstract class AbstractElement
                {
                    public int Value { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                source,
                assemblyReferences: GetAssemblyRefsWithAdditional(
                    typeof(ConfigurationBuilder),
                    typeof(OptionsConfigurationServiceCollectionExtensions),
                    typeof(ServiceCollection),
                    typeof(IOptions<>)));
            Assert.NotNull(result.GeneratedSource);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [Theory]
        [InlineData("private UnbindableType Lazy => UnbindableType.Create();")]
        [InlineData("internal UnbindableType Lazy { get; set; }")]
        [InlineData("protected UnbindableType Lazy { get; set; }")]
        [InlineData("[ConfigurationIgnore] public UnbindableType Lazy { get; set; }")]
        public async Task PropertyExcludedFromBindingDoesNotReportItsType(string propertyDeclaration)
        {
            string source = $$"""
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        MySettings settings = new();
                        config.Bind(settings);
                    }
                }

                public sealed class UnbindableType
                {
                    private UnbindableType() { }
                    public static UnbindableType Create() => new UnbindableType();
                    public int Value { get; set; }
                }

                public class MySettings
                {
                    public int Supported { get; set; }
                    {{propertyDeclaration}}
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);

            Assert.Empty(result.Diagnostics);
            Assert.NotNull(result.GeneratedSource);

            string generated = result.GeneratedSource.Value.SourceText.ToString();
            Assert.Contains("instance.Supported = ", generated);
            Assert.DoesNotContain("UnbindableType", generated);
        }

        [Fact]
        public async Task UnbindableTypeIsStillReportedWhenAlsoReachedThroughABindableProperty()
        {
            // The excluded property is declared first, so it would be the one to pull the type into the graph.
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        MySettings settings = new();
                        config.Bind(settings);
                    }
                }

                public sealed class UnbindableType
                {
                    private UnbindableType() { }
                    public static UnbindableType Create() => new UnbindableType();
                    public int Value { get; set; }
                }

                public class MySettings
                {
                    [ConfigurationIgnore]
                    public UnbindableType Excluded { get; set; }

                    public UnbindableType Bindable { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);

            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Id == Diagnostics.PropertyNotSupported.Id &&
                diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("'Bindable'"));

            Assert.DoesNotContain(result.Diagnostics, diagnostic =>
                diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("'Excluded'"));

            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == Diagnostics.TypeNotSupported.Id);
        }

        [Fact]
        public async Task NonPublicPropertyBackingConstructorParameterKeepsItsTypeRegistered()
        {
            // The binder cannot reach the property, but it does bind the constructor parameter it backs, so the
            // type must stay registered and an unbindable one must still be reported.
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        MySettings settings = config.Get<MySettings>()!;
                    }
                }

                public sealed class UnbindableType
                {
                    private UnbindableType() { }
                    public static UnbindableType Create() => new UnbindableType();
                    public int Value { get; set; }
                }

                public class MySettings
                {
                    public MySettings(UnbindableType inner) => Inner = inner;

                    private UnbindableType Inner { get; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);

            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Id == Diagnostics.PropertyNotSupported.Id &&
                diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("'Inner'"));
        }

        [Fact]
        public async Task BindingToCollectionOnlyTest()
        {
            string source = """
                using Microsoft.Extensions.Configuration;
                using System;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        var settingsSection = config.GetSection("Settings");

                        IDictionary<string, string> options = settingsSection.Get<IDictionary<string, string>>()!;
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        /// <summary>
        /// We binding the type "SslClientAuthenticationOptions" which has a property "CipherSuitesPolicy" of type "CipherSuitesPolicy". We can't bind this type.
        /// This test is to ensure not including the property "CipherSuitesPolicy" in the generated code caused a build break.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task IgnoredUnBindablePropertiesTest()
        {
            string source = """
                 using System;
                 using System.Net.Security;
                 using Microsoft.Extensions.Configuration;
                 using System.Collections.Immutable;
                 using System.Text;
                 using System.Text.Json;

                 public class Program
                 {
                     public static void Main()
                     {
                         ConfigurationBuilder configurationBuilder = new();
                         IConfiguration config = configurationBuilder.Build();

                         var obj = config.Get<SslClientAuthenticationOptions>();
                      }
                 }
                 """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ImmutableArray<>), typeof(Encoding), typeof(JsonSerializer), typeof(System.Net.Security.AuthenticatedStream)));
            Assert.NotNull(result.GeneratedSource);

            Assert.DoesNotContain("CipherSuitesPolicy = ", result.GeneratedSource.Value.SourceText.ToString());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [ActiveIssue("Work out why we aren't getting all the expected diagnostics.")]
        public async Task IssueDiagnosticsForAllOffendingCallsites()
        {
            string source = """
                using System.Collections.Immutable;
                using System.Text;
                using System.Text.Json;
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                public class Program
                {
                	public static void Main()
                	{
                		ConfigurationBuilder configurationBuilder = new();
                		IConfiguration configuration = configurationBuilder.Build();

                        var obj = new TypeGraphWithUnsupportedMember();
                        configuration.Bind(obj);

                        var obj2 = new AnotherGraphWithUnsupportedMembers();
                        var obj4 = Encoding.UTF8;

                        // Must require separate suppression.
                        configuration.Bind(obj2);
                        configuration.Bind(obj2, _ => { });
                        configuration.Bind("", obj2);
                        configuration.Get<TypeGraphWithUnsupportedMember>();
                        configuration.Get<AnotherGraphWithUnsupportedMembers>(_ => { });
                        configuration.Get(typeof(TypeGraphWithUnsupportedMember));
                        configuration.Get(typeof(AnotherGraphWithUnsupportedMembers), _ => { });
                        configuration.Bind(obj4);
                        configuration.Get<Encoding>();
                	}

                    public class TypeGraphWithUnsupportedMember
                    {
                        public JsonWriterOptions WriterOptions { get; set; }
                    }

                    public class AnotherGraphWithUnsupportedMembers
                    {
                        public JsonWriterOptions WriterOptions { get; set; }
                        public ImmutableArray<int> UnsupportedArray { get; set; }
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ImmutableArray<>), typeof(Encoding), typeof(JsonSerializer)));
            Assert.NotNull(result.GeneratedSource);
            Assert.True(result.Diagnostics.Any(diag => diag.Id == Diagnostics.TypeNotSupported.Id));
            Assert.True(result.Diagnostics.Any(diag => diag.Id == Diagnostics.PropertyNotSupported.Id));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_HasPragmaSuppressibleLocation()
        {
            // SYSLIB1103: ValueTypesInvalidForBind (Warning, configurable).
            string source = """
                #pragma warning disable SYSLIB1103
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        int myInt = 1;
                        config.Bind(myInt);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1103");
            Assert.True(diagnostic.IsSuppressed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_NoPragma_IsNotSuppressed()
        {
            string source = """
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        int myInt = 1;
                        config.Bind(myInt);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1103");
            Assert.False(diagnostic.IsSuppressed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_MultipleDiagnostics_OnlySomeSuppressed()
        {
            string source = """
                using System;
                using System.Collections.Immutable;
                using System.Text;
                using System.Text.Json;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        // SYSLIB1103 suppressed for this call only.
                        #pragma warning disable SYSLIB1103
                        int myInt = 1;
                        config.Bind(myInt);
                        #pragma warning restore SYSLIB1103

                        // SYSLIB1103 NOT suppressed for this call.
                        long myLong = 1;
                        config.Bind(myLong);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation)
                .Where(d => d.Id == "SYSLIB1103")
                .ToList();

            Assert.Equal(2, effective.Count);
            Assert.Single(effective, d => d.IsSuppressed);
            Assert.Single(effective, d => !d.IsSuppressed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_PragmaRestoreOutsideSpan_IsNotSuppressed()
        {
            string source = """
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        // Suppress and restore BEFORE the diagnostic site.
                        #pragma warning disable SYSLIB1103
                        #pragma warning restore SYSLIB1103

                        int myInt = 1;
                        config.Bind(myInt);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1103");
            Assert.False(diagnostic.IsSuppressed);
        }

        /// <summary>
        /// Verifies that the suppressor suppresses IL2026/IL3050 when a ConfigurationBinder call
        /// is passed directly as a method argument (e.g. Some.Method(config.Get&lt;T&gt;())).
        /// Regression test for https://github.com/dotnet/runtime/issues/94544.
        /// </summary>
        [Fact]
        public async Task Suppressor_SuppressesWarnings_WhenBindingCallIsMethodArgument()
        {
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        IConfigurationSection c = new ConfigurationBuilder().Build().GetSection("Options");
                        Some.Method(c.Get<MyOptions>());
                    }
                }

                internal static class Some
                {
                    public static void Method(MyOptions? options) { }
                }

                public class MyOptions
                {
                    public int MaxRetries { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);

            await VerifySuppressedCallsMatchInterceptedCalls(result);
        }

        /// <summary>
        /// Verifies that the suppressor also works for the straightforward assignment case,
        /// ensuring no regression in existing behavior.
        /// </summary>
        [Fact]
        public async Task Suppressor_SuppressesWarnings_ForSimpleBindingCall()
        {
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        IConfigurationSection c = new ConfigurationBuilder().Build().GetSection("Options");
                        var options = c.Get<MyOptions>();
                    }
                }

                public class MyOptions
                {
                    public int MaxRetries { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);

            await VerifySuppressedCallsMatchInterceptedCalls(result);
        }

        [Fact]
        public async Task Suppressor_SuppressesWarnings_WithLineDirective()
        {
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        IConfigurationSection c = new ConfigurationBuilder().Build().GetSection("Options");
                #line 100 "Remapped.cs"
                        var options = c.Get<MyOptions>();
                #line default
                    }
                }

                public class MyOptions
                {
                    public int MaxRetries { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);

            await VerifySuppressedCallsMatchInterceptedCalls(result);
        }

        /// <summary>
        /// Verifies that the set of IL2026/IL3050 diagnostics suppressed by the suppressor
        /// matches exactly the set of calls intercepted by the source generator.
        /// Catches both under-suppression (https://github.com/dotnet/runtime/issues/94544)
        /// and over-suppression (https://github.com/dotnet/runtime/issues/96643).
        /// </summary>
        private static async Task VerifySuppressedCallsMatchInterceptedCalls(ConfigBindingGenRunResult result)
        {
            Assert.NotNull(result.GenerationSpec);

            // Collect all intercepted (line, column) locations from the generator spec.
            // The interceptor targets MemberAccessExpression.Name (e.g. "Get" in "c.Get<T>()").
            HashSet<(int Line, int Column)> interceptedLocations = GetInterceptedLocations(result.GenerationSpec);
            Assert.NotEmpty(interceptedLocations);

            // Run the ILLink analyzer + suppressor on the output compilation (which includes generated InterceptsLocation attributes).
            ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsWithSuppressor(result.OutputCompilation);

            // The ILLink analyzer must have produced at least one IL2026 or IL3050 that was suppressed.
            // Without this, the assertions below would pass vacuously if the analyzer didn't fire.
            Assert.Contains(diagnostics, d => (d.Id is "IL2026" or "IL3050") && d.IsSuppressed);

            // Every suppressed IL2026/IL3050 diagnostic should be at an intercepted location.
            foreach (Diagnostic d in diagnostics.Where(d => (d.Id is "IL2026" or "IL3050") && d.IsSuppressed))
            {
                (int line, int column) = GetMethodNameLocation(d);
                Assert.True(interceptedLocations.Contains((line, column)),
                    $"Suppressed {d.Id} at ({line},{column}) but no interceptor was generated for that call site.");
            }

            // Every intercepted location should have its IL2026/IL3050 diagnostics suppressed.
            foreach (Diagnostic d in diagnostics.Where(d => (d.Id is "IL2026" or "IL3050") && !d.IsSuppressed))
            {
                (int line, int column) = GetMethodNameLocation(d);
                Assert.False(interceptedLocations.Contains((line, column)),
                    $"Unsuppressed {d.Id} at ({line},{column}) but an interceptor was generated for that call site.");
            }
        }

        /// <summary>
        /// Resolves a diagnostic's location to the method name position that the interceptor targets.
        /// The ILLink analyzer reports on the MemberAccessExpression (e.g. "c.Get&lt;T&gt;"),
        /// but the interceptor targets just the Name part (e.g. "Get"). This method walks from
        /// the diagnostic location to the InvocationExpression's MemberAccessExpression.Name
        /// to get the matching (line, column).
        /// </summary>
        private static (int Line, int Column) GetMethodNameLocation(Diagnostic diagnostic)
        {
            Location location = diagnostic.AdditionalLocations.Count > 0
                ? diagnostic.AdditionalLocations[0]
                : diagnostic.Location;
            SyntaxTree sourceTree = location.SourceTree!;
            SyntaxNode node = sourceTree.GetRoot().FindNode(location.SourceSpan, getInnermostNodeForTie: true);

            InvocationExpressionSyntax invocation = (node as InvocationExpressionSyntax
                ?? node.Parent as InvocationExpressionSyntax)!;

            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
            FileLinePositionSpan nameSpan = sourceTree.GetLineSpan(memberAccess.Name.Span);

            return (nameSpan.StartLinePosition.Line + 1, nameSpan.StartLinePosition.Character + 1);
        }

        private static HashSet<(int Line, int Column)> GetInterceptedLocations(SourceGenerationSpec spec)
        {
            var locations = new HashSet<(int, int)>();
            InterceptorInfo info = spec.InterceptorInfo;

            AddLocations(info.ConfigBinder);
            AddLocations(info.OptionsBuilderExt);
            AddLocations(info.ServiceCollectionExt);
            AddTypedLocations(info.ConfigBinder_Bind_instance);
            AddTypedLocations(info.ConfigBinder_Bind_instance_BinderOptions);
            AddTypedLocations(info.ConfigBinder_Bind_key_instance);

            return locations;

            void AddLocations(IEnumerable<InvocationLocationInfo>? locationInfos)
            {
                if (locationInfos is null)
                    return;

                foreach (InvocationLocationInfo loc in locationInfos)
                {
                    locations.Add(GetLocation(loc));
                }
            }

            void AddTypedLocations(IEnumerable<TypedInterceptorInvocationInfo>? typedInfos)
            {
                if (typedInfos is null)
                    return;

                foreach (TypedInterceptorInvocationInfo typed in typedInfos)
                {
                    AddLocations(typed.Locations);
                }
            }
        }

        private static (int Line, int Column) GetLocation(InvocationLocationInfo loc)
        {
            if (loc.LineNumber != 0)
            {
                return (loc.LineNumber, loc.CharacterNumber);
            }

            // v1 interceptor: parse from display location, e.g. "path(line,col)"
            string display = loc.InterceptableLocationGetDisplayLocation();
            Match match = Regex.Match(display, @"\((\d+),(\d+)\)$");
            Assert.True(match.Success, $"Could not parse display location: {display}");

            return (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        }

        private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsWithSuppressor(Compilation compilation)
        {
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new DynamicallyAccessedMembersAnalyzer(),
                new ConfigurationBindingGenerator.Suppressor());

            var trimAotAnalyzerOptions = new DictionaryAnalyzerConfigOptions(
                ImmutableDictionary.CreateRange<string, string>(
                    StringComparer.OrdinalIgnoreCase,
                    [
                        new("build_property.EnableTrimAnalyzer", "true"),
                        new("build_property.EnableAotAnalyzer", "true"),
                    ]));
            var analyzerOptions = new AnalyzerOptions(
                ImmutableArray<AdditionalText>.Empty,
                new GlobalOptionsOnlyProvider(trimAotAnalyzerOptions));
            var options = new CompilationWithAnalyzersOptions(
                analyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: true);

            return await new CompilationWithAnalyzers(compilation, analyzers, options)
                .GetAllDiagnosticsAsync();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        // Keyword-named constructor parameters bound to locals.
        [InlineData("""
            class MyConfiguration(string @base, string @event)
            {
                public string Base { get; } = @base;
                public string Event { get; } = @event;
            }
            """)]
        // Keyword-named settable properties bound through member access on the instance.
        [InlineData("""
            class MyConfiguration
            {
                public string @base { get; set; }
                public string @event { get; set; }
            }
            """)]
        // Positional record properties keep the keyword names of their matching constructor parameters,
        // so both sides of the emitted object initializer need escaping.
        [InlineData("record MyConfiguration(string @base, string @event);")]
        public async Task KeywordNamedMembers(string configurationType)
        {
            string source = $$"""
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        MyConfiguration options = config.GetSection("My").Get<MyConfiguration>()!;
                    }
                }

                {{configurationType}}
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData("quoted\"key")]
        [InlineData(@"path\key")]
        [InlineData("line\nbreak")]
        public async Task ConfigurationKeyNamesRequiringEscaping(string configurationKeyName)
        {
            string source = $$"""
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        MyConfiguration options = config.GetSection("My").Get<MyConfiguration>()!;
                    }
                }

                class MyConfiguration
                {
                    [ConfigurationKeyName({{SymbolDisplay.FormatLiteral(configurationKeyName, quote: true)}})]
                    public string Value { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            AssertCanCreateAssemblyImage(result.OutputCompilation);
        }
    }
}
