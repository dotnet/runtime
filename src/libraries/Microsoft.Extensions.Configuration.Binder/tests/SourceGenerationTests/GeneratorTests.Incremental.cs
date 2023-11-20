// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBindingGeneratorTests : ConfigurationBinderTestsBase
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
        public sealed class IncrementalTests
        {
            [Fact]
            public async Task CompilingTheSameSourceResultsInEqualModels()
            {
                SourceGenerationSpec spec1 = (await new ConfigBindingGenTestDriver().RunGeneratorAndUpdateCompilation(BindCallSampleCode)).GenerationSpec;
                SourceGenerationSpec spec2 = (await new ConfigBindingGenTestDriver().RunGeneratorAndUpdateCompilation(BindCallSampleCode)).GenerationSpec;

                Assert.NotSame(spec1, spec2);
                GeneratorTestHelpers.AssertStructurallyEqual(spec1, spec2);

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
        #endregion Incremental test sources.
    }
}
