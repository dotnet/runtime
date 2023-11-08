// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBindingGeneratorTests
    {
        [Fact]
        public async Task Bind() =>
            await VerifyAgainstBaselineUsingFile("Bind.generated.txt", BindCallSampleCode, extType: ExtensionClassType.ConfigurationBinder);

        [Theory]
        [InlineData("ConfigurationBinder.Bind(instance: configObj, configuration: config);")]
        [InlineData("""ConfigurationBinder.Bind(key: "", instance: configObj, configuration: config);""")]
        [InlineData("""ConfigurationBinder.Bind(instance: configObj, key: "", configuration: config);""")]
        [InlineData("ConfigurationBinder.Bind(configureOptions: _ => { }, configuration: config, instance: configObj);")]
        [InlineData("ConfigurationBinder.Bind(configuration: config, configureOptions: _ => { }, instance: configObj);")]
        public async Task Bind_NamedParameters_OutOfOrder(string row)
        {
            string source = $$"""
                        using System.Collections.Generic;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                MyClass configObj = new();
                                {{row}}
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }
                        }
                    """;

            await VerifyThatSourceIsGenerated(source);
        }

        [Theory]
        [InlineData("var obj = ConfigurationBinder.Get(type: typeof(MyClass), configuration: config);")]
        [InlineData("var obj = ConfigurationBinder.Get<MyClass>(configureOptions: _ => { }, configuration: config);")]
        [InlineData("var obj = ConfigurationBinder.Get(configureOptions: _ => { }, type: typeof(MyClass), configuration: config);")]
        [InlineData("var obj =  ConfigurationBinder.Get(type: typeof(MyClass), configureOptions: _ => { }, configuration: config);")]
        public async Task Get_TypeOf_NamedParametersOutOfOrder(string row)
        {
            string source = $$"""
                        using System.Collections.Generic;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                MyClass configObj = new();
                                {{row}}
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }
                        }
                    """;

            await VerifyThatSourceIsGenerated(source);
        }

        [Theory]
        [InlineData("""var str = ConfigurationBinder.GetValue(key: "key", configuration: config, type: typeof(string));""")]
        [InlineData("""var str = ConfigurationBinder.GetValue<string>(key: "key", configuration: config);""")]
        [InlineData("""var str = ConfigurationBinder.GetValue<string>(key: "key", defaultValue: "default", configuration: config);""")]
        [InlineData("""var str = ConfigurationBinder.GetValue<string>(configuration: config, key: "key", defaultValue: "default");""")]
        [InlineData("""var str = ConfigurationBinder.GetValue(defaultValue: "default", key: "key", configuration: config, type: typeof(string));""")]
        [InlineData("""var str = ConfigurationBinder.GetValue(defaultValue: "default", type: typeof(string), key: "key", configuration: config);""")]
        public async Task GetValue_NamedParametersOutOfOrder(string row)
        {
            string source = $$"""
                        using System.Collections.Generic;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();
                                {{row}}
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }
                        }
                    """;

            await VerifyThatSourceIsGenerated(source);
        }

        [Fact]
        public async Task Bind_Instance()
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

                                MyClass configObj = new();
                                config.Bind(configObj);
                            }

                            public class MyClass
                            {
                                public string? MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                                public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
                            }

                            public class MyClass2 { }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Bind_Instance.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Bind_Instance_BinderOptions()
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

                                MyClass configObj = new();
                                config.Bind(configObj, options => { });
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

            await VerifyAgainstBaselineUsingFile("Bind_Instance_BinderOptions.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Bind_Key_Instance()
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

                                MyClass configObj = new();
                                config.Bind("key", configObj);
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

            await VerifyAgainstBaselineUsingFile("Bind_Key_Instance.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Bind_CanParseTargetConfigType_FromMethodParam()
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

                        BindOptions(config, new MyClass0());
                        BindOptions(config, new MyClass1(), _ => { });
                        BindOptions(config, "", new MyClass2());
                    }

                    private static void BindOptions(IConfiguration config, MyClass0 instance)
                    {
                        config.Bind(instance);
                    }

                    private static void BindOptions(IConfiguration config, MyClass1 instance, Action<BinderOptions>? configureOptions)
                    {
                        config.Bind(instance, configureOptions);
                    }

                    private static void BindOptions(IConfiguration config, string path, MyClass2 instance)
                    {
                        config.Bind(path, instance);
                    }

                    public class MyClass0 { }
                    public class MyClass1 { }
                    public class MyClass2 { }
                }
                """;

            await VerifyAgainstBaselineUsingFile("Bind_ParseTypeFromMethodParam.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get()
        {
            string source = @"
        using System.Collections.Generic;
        using Microsoft.Extensions.Configuration;

        public class Program
        {
        	public static void Main()
        	{
        		ConfigurationBuilder configurationBuilder = new();
        		IConfigurationRoot config = configurationBuilder.Build();

        		MyClass configObj = config.Get<MyClass>();
                MyClass2 configObj2 = (MyClass2)config.Get(typeof(MyClass2));
                configObj = config.Get<MyClass>(binderOptions => { });
                configObj2 = (MyClass2)config.Get(typeof(MyClass2), binderOptions => { });
        	}

        	public class MyClass
        	{
        		public string MyString { get; set; }
        		public int MyInt { get; set; }
        		public List<int> MyList { get; set; }
                public int[] MyArray { get; set; }
        		public Dictionary<string, string> MyDictionary { get; set; }
        	}

            public class MyClass2
            {
                public int MyInt { get; set; }
            }

            public class MyClass3
            {
                public int MyInt { get; set; }
            }

            public class MyClass4
            {
                public int MyInt { get; set; }
            }
        }";

            await VerifyAgainstBaselineUsingFile("Get.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_PrimitivesOnly()
        {
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        config.Get<int>();
                        config.Get(typeof(string));
                        config.Get<float>(binderOptions => { });
                        config.Get(typeof(double), binderOptions => { });
                    }
                }
                """;

            await VerifyAgainstBaselineUsingFile("Get_PrimitivesOnly.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_T()
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

                                MyClass configObj = config.Get<MyClass>();
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_T.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_T_BinderOptions()
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

                                MyClass configObj = config.Get<MyClass>(binderOptions => { });
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_T_BinderOptions.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_TypeOf()
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

                                MyClass2 configObj = (MyClass2)config.Get(typeof(MyClass2));
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_TypeOf.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_TypeOf_BinderOptions()
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

                                config.Get(typeof(MyClass2), binderOptions => { });
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_TypeOf_BinderOptions.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue()
        {
            string source = @"
        using System.Collections.Generic;
        using System.Globalization;
        using Microsoft.Extensions.Configuration;

        public class Program
        {
        	public static void Main()
        	{
        		ConfigurationBuilder configurationBuilder = new();
        		IConfigurationRoot config = configurationBuilder.Build();

        		config.GetValue<int>(""key"");
                config.GetValue(typeof(bool?), ""key"");
                config.GetValue<MyClass>(""key"", new MyClass());
                config.GetValue<byte[]>(""key"", new byte[] { });
                config.GetValue(typeof(CultureInfo), ""key"", CultureInfo.InvariantCulture);
        	}

        	public class MyClass
        	{
        		public string MyString { get; set; }
        		public int MyInt { get; set; }
        		public List<int> MyList { get; set; }
                public int[] MyArray { get; set; }
        		public Dictionary<string, string> MyDictionary { get; set; }
        	}
        }";

            await VerifyAgainstBaselineUsingFile("GetValue.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_T_Key()
        {
            string source = """
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue<int>("key");
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_T_Key.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_T_Key_DefaultValue()
        {
            string source = """
                        using System.Collections.Generic;
                        using System.Globalization;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue<int>("key", 5);
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_T_Key_DefaultValue.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_TypeOf_Key()
        {
            string source = """
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue(typeof(bool?), "key");
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_TypeOf_Key.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_TypeOf_Key_DefaultValue()
        {
            string source = """
                        using System.Globalization;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue(typeof(CultureInfo), "key", CultureInfo.InvariantCulture);
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_TypeOf_Key_DefaultValue.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task None()
        {
            string source = @"
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;

        public class Program
        {
        	public static void Main()
        	{
                ConfigurationBuilder configurationBuilder = new();
        		IConfiguration config = configurationBuilder.Build();
                IConfigurationSection section = config.GetSection(""MySection"");
        	}

        	public class MyClass
        	{
        		public string MyString { get; set; }
        		public int MyInt { get; set; }
        		public List<int> MyList { get; set; }
                public List<MyClass2> MyList2 { get; set; }
        		public Dictionary<string, string> MyDictionary { get; set; }
        	}

            public class MyClass2
            {
                public int MyInt { get; set; }
            }
        }"
            ;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.False(result.GeneratedSource.HasValue);
            Assert.Empty(result.Diagnostics);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Primitives()
        {
            string source = """
                        using System;
                        using System.Globalization;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                MyClass obj = new();
                                config.Bind(obj);
                            }

                            public class MyClass
                            {
                                public bool Prop0 { get; set; }
                                public byte Prop1 { get; set; }
                                public sbyte Prop2 { get; set; }
                                public char Prop3 { get; set; }
                                public double Prop4 { get; set; }
                                public string Prop5 { get; set; }
                                public int Prop6 { get; set; }
                                public short Prop8 { get; set; }
                                public long Prop9 { get; set; }
                                public float Prop10 { get; set; }
                                public ushort Prop13 { get; set; }
                                public uint Prop14 { get; set; }
                                public ulong Prop15 { get; set; }
                                public object Prop16 { get; set; }
                                public CultureInfo Prop17 { get; set; }
                                public DateTime Prop19 { get; set; }
                                public DateTimeOffset Prop20 { get; set; }
                                public decimal Prop21 { get; set; }
                                public TimeSpan Prop23 { get; set; }
                                public Guid Prop24 { get; set; }
                                public Uri Prop25 { get; set; }
                                public Version Prop26 { get; set; }
                                public DayOfWeek Prop27 { get; set; }
                                public Int128 Prop7 { get; set; }
                                public Half Prop11 { get; set; }
                                public UInt128 Prop12 { get; set; }
                                public DateOnly Prop18 { get; set; }
                                public TimeOnly Prop22 { get; set; }
                                public byte[] Prop28 { get; set; }
                                public int Prop29 { get; set; }
                                public DateTime Prop30 { get; set; }
                            }
                        }
                        """;
            await VerifyAgainstBaselineUsingFile("Primitives.generated.txt", source);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetFramework))]
        public async Task PrimitivesNetFwk()
        {
            string source = """
                        using System;
                        using System.Globalization;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                MyClass obj = new();
                                config.Bind(obj);
                            }

                            public class MyClass
                            {
                                public bool Prop0 { get; set; }
                                public byte Prop1 { get; set; }
                                public sbyte Prop2 { get; set; }
                                public char Prop3 { get; set; }
                                public double Prop4 { get; set; }
                                public string Prop5 { get; set; }
                                public int Prop6 { get; set; }
                                public short Prop8 { get; set; }
                                public long Prop9 { get; set; }
                                public float Prop10 { get; set; }
                                public ushort Prop13 { get; set; }
                                public uint Prop14 { get; set; }
                                public ulong Prop15 { get; set; }
                                public object Prop16 { get; set; }
                                public CultureInfo Prop17 { get; set; }
                                public DateTime Prop19 { get; set; }
                                public DateTimeOffset Prop20 { get; set; }
                                public decimal Prop21 { get; set; }
                                public TimeSpan Prop23 { get; set; }
                                public Guid Prop24 { get; set; }
                                public Uri Prop25 { get; set; }
                                public Version Prop26 { get; set; }
                                public DayOfWeek Prop27 { get; set; }
                                public byte[] Prop28 { get; set; }
                                public int Prop29 { get; set; }
                                public DateTime Prop30 { get; set; }
                            }
                        }
                        """;

            await VerifyAgainstBaselineUsingFile("Primitives.generated.txt", source);
        }

        [Fact]
        public async Task Collections()
        {
            string source = """
                using System.Collections.Generic;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        IConfigurationSection section = config.GetSection("MySection");

                        section.Get<MyClassWithCustomCollections>();
                    }

                    // Diagnostic warning because we don't know how to instantiate two properties on this type.
                    public class MyClassWithCustomCollections
                    {
                        public CustomDictionary<string, int> CustomDictionary { get; set; }
                        public CustomList CustomList { get; set; }
                        public ICustomDictionary<string> ICustomDictionary { get; set; }
                        public ICustomSet<MyClassWithCustomCollections> ICustomCollection { get; set; }
                        public IReadOnlyList<int> IReadOnlyList { get; set; }
                        // Diagnostic warning because we don't know how to instantiate the property type.
                        public IReadOnlyDictionary<MyClassWithCustomCollections, int> UnsupportedIReadOnlyDictionaryUnsupported { get; set; }
                        public IReadOnlyDictionary<string, int> IReadOnlyDictionary { get; set; }
                    }

                    public class CustomDictionary<TKey, TValue> : Dictionary<TKey, TValue>
                    {
                    }

                    public class CustomList : List<string>
                    {
                    }

                    // Diagnostic warning because we don't know how to instantiate this type.
                    public interface ICustomDictionary<T> : IDictionary<T, string>
                    {
                    }

                    // Diagnostic warning because we don't know how to instantiate this type.
                    public interface ICustomSet<T> : ISet<T>
                    {
                    }
                }
                """;

            ConfigBindingGenRunResult result = await VerifyAgainstBaselineUsingFile(
                "Collections.generated.txt",
                source,
                expectedDiags: ExpectedDiagnostics.FromGeneratorOnly);

            ImmutableArray<Diagnostic> diagnostics = result.Diagnostics;
            Assert.Equal(3, diagnostics.Where(diag => diag.Id == Diagnostics.TypeNotSupported.Id).Count());
            Assert.Equal(3, diagnostics.Where(diag => diag.Id == Diagnostics.PropertyNotSupported.Id).Count());
        }

        [Fact]
        public async Task MinimalGenerationIfNoBindableMembers()
        {
            string source = """
                using System.Collections.Generic;
                using Microsoft.Extensions.Configuration;
                
                public class Program
                {
                	public static void Main()
                	{
                		ConfigurationBuilder configurationBuilder = new();
                		IConfiguration configuration = configurationBuilder.Build();

                        TypeWithNoMembers obj = new();
                        configuration.Bind(obj);

                        TypeWithNoMembers_Wrapper obj2 = new();
                        configuration.Bind(obj2);

                        List<AbstractType_CannotInit> obj3 = new();
                        configuration.Bind(obj3);
                    }
                }

                public class TypeWithNoMembers
                {
                }

                public class TypeWithNoMembers_Wrapper
                {
                    public TypeWithNoMembers Member { get; set; }
                }

                public abstract class AbstractType_CannotInit
                {
                }
                """;

            ConfigBindingGenRunResult result = await VerifyAgainstBaselineUsingFile(
                "EmptyConfigType.generated.txt",
                source,
                expectedDiags: ExpectedDiagnostics.FromGeneratorOnly);

            Assert.Equal(2, result.Diagnostics.Where(diag => diag.Id == Diagnostics.TypeNotSupported.Id).Count());
        }
    }
}
