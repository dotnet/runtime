// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public partial class ConfigurationBindingGeneratorTests : ConfigurationBinderTestsBase
    {
        [ActiveIssue("")]
        [Fact]
        public async Task RunWithNoDiags_Then_NoEdit()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
            result.AssertHasSourceAndNoDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation();
            result.AssertEmpty();
        }

        [ActiveIssue("")]
        [Fact]
        public async Task RunWithNoDiags_Then_NoOpEdit()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
            result.AssertHasSourceAndNoDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_ReorderedInvocations);
            result.AssertEmpty();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_ReorderedConfigTypeMembers);
            result.AssertEmpty();
        }

        [ActiveIssue("")]
        [Fact]
        public async Task RunWithNoDiags_Then_EditWithNoDiags()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
            result.AssertHasSourceAndNoDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithDifferentConfigTypeName);
            result.AssertEmpty();
        }

        [ActiveIssue("")]
        [Fact]
        public async Task RunWithNoDiags_Then_EditWithDiags()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
            result.AssertHasSourceAndNoDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
            result.AssertEmpty();
        }

        [ActiveIssue("")]
        [Fact]
        public async Task RunWithDiags_Then_NoEdit()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
            result.AssertHasSourceAndDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation();
            result.AssertEmpty();
        }

        [ActiveIssue("")]
        [Fact]
        public async Task RunWithDiags_Then_NoOpEdit()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
            result.AssertHasSourceAndDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember_ReorderedInvocations);
            result.AssertEmpty();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember_ReorderedConfigTypeMembers);
            result.AssertEmpty();
        }

        [ActiveIssue("")]
        [Fact]
        public async Task RunWithDiags_Then_EditWithNoDiags()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
            result.AssertHasSourceAndDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCode);
            result.AssertEmpty();
        }

        [ActiveIssue("")]
        [Fact]
        public async Task RunWithDiags_Then_EditWithDiags()
        {
            ConfigBindingGenDriver driver = new ConfigBindingGenDriver();

            ConfigBindingGenResult result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember);
            result.AssertHasSourceAndDiagnostics();

            result = await driver.RunGeneratorAndUpdateCompilation(BindCallSampleCodeVariant_WithUnsupportedMember_WithDiffMemberName);
            result.AssertHasSourceAndDiagnostics();
        }

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
        		    config.Bind(configObj);
                    config.Bind(configObj, options => { });
                    config.Bind("key", configObj);
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
    }
}
