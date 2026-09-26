// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBindingGeneratorTests : ConfigurationBinderTestsBase
    {
        [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public sealed class IncrementalTests
        {
            [Fact]
            public async Task CompilingTheSameSourceResultsInEqualModels()
            {
                SourceGenerationSpec spec1 = (await new ConfigBindingGenTestDriver().RunGeneratorAndUpdateCompilation(BindCallSampleCode)).GenerationSpec;
                SourceGenerationSpec spec2 = (await new ConfigBindingGenTestDriver().RunGeneratorAndUpdateCompilation(BindCallSampleCode)).GenerationSpec;

                Assert.NotSame(spec1, spec2);
#pragma warning disable IL2026 // https://github.com/dotnet/runtime/issues/126862
                GeneratorTestHelpers.AssertStructurallyEqual(spec1, spec2);
#pragma warning restore IL2026

                Assert.Equal(spec1, spec2);
                Assert.Equal(spec1.GetHashCode(), spec2.GetHashCode());
            }

            [Fact]
            public async Task RunWithNoDiags_Then_NoEdit()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                result = await driver.RunGeneratorAndUpdateCompilation();
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Unchanged);
            }

            [Fact]
            public async Task RunWithNoDiags_Then_ChangeInputOrder()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                // We expect different spec because diag locations are different.
                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_ReorderedInvocations);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);

                // We expect different spec because members are reordered.
                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_ReorderedConfigTypeMembers);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);
            }

            [Fact]
            public async Task RunWithNoDiags_Then_EditWithNoDiags()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithDifferentConfigTypeName);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);
            }

            [Fact]
            public async Task RunWithNoDiags_Then_EditWithDiags()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);
            }

            [Fact]
            public async Task RunWithDiags_Then_NoEdit()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                result = await driver.RunGeneratorAndUpdateCompilation();
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Unchanged);
            }

            [Fact]
            public async Task RunWithDiags_Then_ChangeInputOrder()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                // We expect different spec because diag locations are different.
                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember_ReorderedInvocations);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);

                // We expect different spec because members are reordered.
                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember_ReorderedConfigTypeMembers);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);
            }

            [Fact]
            public async Task RunWithDiags_Then_EditWithNoDiags()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);
            }

            [Fact]
            public async Task RunWithDiags_Then_EditWithDiags()
            {
                ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver();

                ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
                result.ValidateIncrementalResult(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

                result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember_WithDiffMemberName);
                result.ValidateIncrementalResult(IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified);
            }

            [Theory]
            [InlineData(null)]
            [InlineData(false)]
            public async Task PropertyTypeConverterOptOut_PreservesLegacyBehavior(bool? enableTypeConverters)
            {
                ConfigBindingGenRunResult baseline = await RunGeneratorAndUpdateCompilation(
                    PropertyTypeConverterSampleCode,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)));

                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                    PropertyTypeConverterSampleCode,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)),
                    enableTypeConverters: enableTypeConverters);

                result.ValidateDiagnostics(ExpectedDiagnostics.None);
                Assert.Empty(result.Diagnostics);
                Assert.NotNull(result.GeneratedSource);
                Assert.NotNull(baseline.GeneratedSource);
                string generated = result.GeneratedSource.Value.SourceText.ToString();
                Assert.DoesNotContain("TryConvert_", generated);
                Assert.DoesNotContain("TypeDescriptor", generated);
                Assert.Equal(baseline.GeneratedSource.Value.SourceText.ToString(), generated);
            }

            [Fact]
            public async Task PropertyTypeConverterOptIn_EmitsDirectConversion()
            {
                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                    PropertyTypeConverterSampleCode,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)),
                    enableTypeConverters: true);

                result.ValidateDiagnostics(ExpectedDiagnostics.None);
                Assert.Empty(result.Diagnostics);
                Assert.NotNull(result.GeneratedSource);
                string generated = result.GeneratedSource.Value.SourceText.ToString();
                Assert.Contains("TryConvert_", generated);
                Assert.DoesNotContain("TypeDescriptor", generated);

                // Toggling the flag for otherwise identical source must change the generated output.
                ConfigBindingGenRunResult legacyResult = await RunGeneratorAndUpdateCompilation(
                    PropertyTypeConverterSampleCode,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)),
                    enableTypeConverters: false);
                Assert.NotNull(legacyResult.GeneratedSource);
                Assert.NotEqual(legacyResult.GeneratedSource.Value.SourceText.ToString(), generated);
            }

            [Theory]
            [InlineData(false, false, DiagnosticSeverity.Warning)]
            [InlineData(true, false, DiagnosticSeverity.Error)]
            [InlineData(false, true, DiagnosticSeverity.Error)]
            public async Task PropertyTypeConverterInaccessible_SeverityFollowsPublishFlags(bool publishAot, bool publishTrimmed, DiagnosticSeverity expectedSeverity)
            {
                // A private nested converter type is not statically usable; with the opt-in enabled this
                // falls back to SYSLIB1105, escalated to an error under PublishAot/PublishTrimmed.
                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                    PropertyTypeConverterInaccessibleSampleCode,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)),
                    enableTypeConverters: true,
                    publishAot: publishAot,
                    publishTrimmed: publishTrimmed);

                result.ValidateDiagnostics(ExpectedDiagnostics.FromGeneratorOnly);
                Diagnostic diagnostic = Assert.Single(result.Diagnostics);
                Assert.Equal(Diagnostics.PropertyTypeConverterRequiresReflection.Id, diagnostic.Id);
                Assert.Contains("TypeConverterAttribute", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
                Assert.Equal(expectedSeverity, diagnostic.Severity);
            }

            [Fact]
            public async Task PropertyTypeConverterWithoutUsableConstructor_ReportsWarning()
            {
                // A converter without a public parameterless constructor or a public constructor accepting a
                // single System.Type argument can't be constructed statically, so it also falls back to SYSLIB1105.
                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                    PropertyTypeConverterMalformedSampleCode,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)),
                    enableTypeConverters: true);

                result.ValidateDiagnostics(ExpectedDiagnostics.FromGeneratorOnly);
                Diagnostic diagnostic = Assert.Single(result.Diagnostics);
                Assert.Equal(Diagnostics.PropertyTypeConverterRequiresReflection.Id, diagnostic.Id);
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            }

            [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
            [InlineData(false)]
            [InlineData(true)]
            public async Task PropertyTypeConverterWithRequiredMembers_RequiresConstructorGuarantee(bool setsRequiredMembers)
            {
                string constructor = """
                    public required string Label { get; init; }
                    public MalformedConverter() { Label = "preset"; }
                    """;
                if (setsRequiredMembers)
                {
                    constructor = constructor.Replace("public MalformedConverter()",
                        "[System.Diagnostics.CodeAnalysis.SetsRequiredMembers] public MalformedConverter()");
                }
                string source = PropertyTypeConverterMalformedSampleCode.Replace("public MalformedConverter(string label) { }", constructor);
                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                    source,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)),
                    enableTypeConverters: true);

                if (setsRequiredMembers)
                {
                    result.ValidateDiagnostics(ExpectedDiagnostics.None);
                    Assert.Empty(result.Diagnostics);
                    Assert.NotNull(result.GeneratedSource);
                }
                else
                {
                    result.ValidateDiagnostics(ExpectedDiagnostics.FromGeneratorOnly);
                    Assert.Equal(Diagnostics.PropertyTypeConverterRequiresReflection.Id, Assert.Single(result.Diagnostics).Id);
                    Assert.False(result.GeneratedSource.HasValue);
                }
            }

            [Fact]
            public async Task PublishFlags_DoNotAffectOutputWithoutPropertyConverters()
            {
                // PublishAot/PublishTrimmed only matter once a property converter can't be resolved statically;
                // for source with no [TypeConverter] usage at all, they must be a no-op.
                ConfigBindingGenRunResult baseline = await RunGeneratorAndUpdateCompilation(BindCallSampleCode);
                ConfigBindingGenRunResult withPublishFlags = await RunGeneratorAndUpdateCompilation(
                    BindCallSampleCode,
                    publishAot: true,
                    publishTrimmed: true);

                Assert.Empty(baseline.Diagnostics);
                Assert.Empty(withPublishFlags.Diagnostics);
                Assert.NotNull(baseline.GeneratedSource);
                Assert.NotNull(withPublishFlags.GeneratedSource);
                Assert.Equal(baseline.GeneratedSource.Value.SourceText.ToString(), withPublishFlags.GeneratedSource.Value.SourceText.ToString());
            }

            [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
            [InlineData(null, false, false, 41)]
            [InlineData(false, false, false, 41)]
            [InlineData(null, true, false, 41)]
            [InlineData(null, false, true, 41)]
            [InlineData(false, true, true, 41)]
            [InlineData(true, false, false, 42)]
            [InlineData(true, true, false, 42)]
            [InlineData(true, false, true, 42)]
            public async Task PropertyTypeConverterOptIn_PreservesUpgradeResults(
                bool? enableTypeConverters, bool publishAot, bool publishTrimmed, int expected)
            {
                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(
                    PropertyTypeConverterSampleCode,
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)),
                    enableTypeConverters: enableTypeConverters,
                    publishAot: publishAot,
                    publishTrimmed: publishTrimmed);

                result.ValidateDiagnostics(ExpectedDiagnostics.None);
                Assert.Equal(expected, LoadAndInvokeMain(result.OutputCompilation, "Result"));
            }

            [Fact]
            public async Task PropertyTypeConverterOptIn_UpdatesIncrementally()
            {
                ConfigBindingGenTestDriver driver = new(
                    assemblyReferences: GetAssemblyRefsWithAdditional(typeof(TypeConverter), typeof(TypeConverterAttribute)));
                ConfigBindingGenRunResult legacy = await driver.RunGeneratorAndUpdateCompilation(PropertyTypeConverterSampleCode);

                driver.UpdateOptions(enableTypeConverters: true);
                ConfigBindingGenRunResult enabled = await driver.RunGeneratorAndUpdateCompilation();
                enabled.ValidateDiagnostics(ExpectedDiagnostics.None);
                Assert.NotEqual(legacy.GeneratedSource.Value.SourceText.ToString(), enabled.GeneratedSource.Value.SourceText.ToString());

                driver.UpdateOptions(enableTypeConverters: true, publishAot: true);
                ConfigBindingGenRunResult aot = await driver.RunGeneratorAndUpdateCompilation();
                Assert.Equal(enabled.GenerationSpec, aot.GenerationSpec);
                Assert.Equal(enabled.GeneratedSource.Value.SourceText.ToString(), aot.GeneratedSource.Value.SourceText.ToString());

                driver.UpdateOptions(enableTypeConverters: false);
                ConfigBindingGenRunResult disabled = await driver.RunGeneratorAndUpdateCompilation();
                Assert.Equal(legacy.GeneratedSource.Value.SourceText.ToString(), disabled.GeneratedSource.Value.SourceText.ToString());
            }
        }

        #region Incremental test sources.
        /// <summary>
        /// Keep in sync with <see cref="BindCallSampleCode"/>.
        /// </summary>
        private const string BindCallSampleCodeVariant_ReorderedInvocations = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass configObj = new();
                    config.Bind(configObj, options => { });
                    config.Bind("key", configObj);
        		    config.Bind(configObj);
        	    }

        	    public class MyClass
        	    {
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
        		    public List<int> MyList { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        /// <summary>
        /// Keep in sync with <see cref="BindCallSampleCode"/>.
        /// </summary>
        private const string BindCallSampleCodeVariant_ReorderedConfigTypeMembers = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass configObj = new();
                    config.Bind(configObj, options => { });
                    config.Bind("key", configObj);
        		    config.Bind(configObj);
        	    }

        	    public class MyClass
        	    {
                    public List<int> MyList { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        /// <summary>
        /// Keep in sync with <see cref="BindCallSampleCode"/>.
        /// </summary>
        private const string BindCallSampleCodeVariant_WithDifferentConfigTypeName = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass0 configObj = new();
                    config.Bind(configObj, options => { });
                    config.Bind("key", configObj);
        		    config.Bind(configObj);
        	    }

        	    public class MyClass0
        	    {
                    public List<int> MyList { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        private const string BindCallSampleCodeVariant_WithUnsupportedMember = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass configObj = new();
        		    config.Bind(configObj);
                    config.Bind(configObj, options => { });
                    config.Bind("key", configObj);
        	    }

        	    public class MyClass
        	    {
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
        		    public List<int> MyList { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
                    public int[,] UnsupportedMember { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        private const string BindCallSampleCodeVariant_WithUnsupportedMember_ReorderedInvocations = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass configObj = new();
                    config.Bind("key", configObj);
                    config.Bind(configObj);
                    config.Bind(configObj, options => { });
        	    }

        	    public class MyClass
        	    {
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
        		    public List<int> MyList { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
                    public int[,] UnsupportedMember { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        private const string BindCallSampleCodeVariant_WithUnsupportedMember_ReorderedConfigTypeMembers = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass configObj = new();
                    config.Bind("key", configObj);
                    config.Bind(configObj);
                    config.Bind(configObj, options => { });
        	    }

        	    public class MyClass
        	    {
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
                    public int[,] UnsupportedMember { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
                    public List<int> MyList { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        private const string BindCallSampleCodeVariant_WithUnsupportedMember_WithDiffMemberName = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass configObj = new();
        		    config.Bind(configObj);
                    config.Bind(configObj, options => { });
                    config.Bind("key", configObj);
        	    }

        	    public class MyClass
        	    {
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
        		    public List<int> MyList { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
                    public int[,] UnsupportedMember_DiffMemberName { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        /// <summary>
        /// A publicly accessible property converter with a public parameterless constructor: statically usable
        /// once EnableConfigurationBindingGeneratorTypeConverters is enabled.
        /// </summary>
        private const string PropertyTypeConverterSampleCode = """
            using System;
            using System.Collections.Generic;
            using System.ComponentModel;
            using System.Globalization;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
                public static int Result;

                public static void Main()
                {
                    ConfigurationBuilder configurationBuilder = new();
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> { ["Value"] = "41" });
                    IConfigurationRoot config = configurationBuilder.Build();

                    MyClass configObj = new();
                    config.Bind(configObj);
                    Result = configObj.Value;
                }

                public class MyClass
                {
                    [TypeConverter(typeof(AddOneConverter))]
                    public int Value { get; set; }
                }

                public sealed class AddOneConverter : TypeConverter
                {
                    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
                        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

                    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
                        value is string text
                            ? int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture) + 1
                            : base.ConvertFrom(context, culture, value);
                }
            }
        """;

        /// <summary>
        /// The converter is only reachable from a private nested type, so it can't be resolved statically even
        /// though it has a usable public constructor. This is expected to fall back to SYSLIB1105.
        /// </summary>
        private const string PropertyTypeConverterInaccessibleSampleCode = """
            using System;
            using System.ComponentModel;
            using System.Globalization;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
                public static void Main()
                {
                    ConfigurationBuilder configurationBuilder = new();
                    IConfigurationRoot config = configurationBuilder.Build();

                    MyClass configObj = new();
                    config.Bind(configObj);
                }

                public class MyClass
                {
                    [TypeConverter(typeof(PrivateConverter))]
                    public int Value { get; set; }

                    private sealed class PrivateConverter : TypeConverter
                    {
                        public PrivateConverter() { }

                        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
                            sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

                        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
                            value is string text
                                ? int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture) + 1
                                : base.ConvertFrom(context, culture, value);
                    }
                }
            }
        """;

        /// <summary>
        /// The converter is public and accessible, but has neither a public parameterless constructor nor a
        /// public constructor accepting a single <see cref="Type"/>, so it can't be constructed statically.
        /// </summary>
        private const string PropertyTypeConverterMalformedSampleCode = """
            using System;
            using System.ComponentModel;
            using System.Globalization;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
                public static void Main()
                {
                    ConfigurationBuilder configurationBuilder = new();
                    IConfigurationRoot config = configurationBuilder.Build();

                    MyClass configObj = new();
                    config.Bind(configObj);
                }

                public class MyClass
                {
                    [TypeConverter(typeof(MalformedConverter))]
                    public int Value { get; set; }
                }

                public sealed class MalformedConverter : TypeConverter
                {
                    public MalformedConverter(string label) { }

                    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
                        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

                    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
                        value is string text
                            ? int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture) + 1
                            : base.ConvertFrom(context, culture, value);
                }
            }
        """;
        #endregion Incremental test sources.
    }
}
