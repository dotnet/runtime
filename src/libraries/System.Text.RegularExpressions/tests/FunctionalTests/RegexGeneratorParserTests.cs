// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    // Tests don't actually use reflection emit, but they do generate assembly via Roslyn in-memory at run time and expect it to be JIT'd.
    // The tests also use typeof(object).Assembly.Location, which returns an empty string on wasm.
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsNotMobile), nameof(PlatformDetection.IsNotBrowser))]
    public class RegexGeneratorParserTests
    {
        [Fact]
        public async Task Diagnostic_MultipleAttributes()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    [GeneratedRegex(""abc"")]
                    private static partial Regex MultipleAttributes();
                }
            ");

            Assert.Equal("SYSLIB1041", Assert.Single(diagnostics).Id);
        }

        public static IEnumerable<object[]> Diagnostic_MalformedCtor_MemberData()
        {
            const string Pre = "[GeneratedRegex";
            const string Post = "]";
            const string Middle = "\"abc\", RegexOptions.None, -1, \"extra\"";

            foreach (bool withParens in new[] { false, true })
            {
                string preParen = withParens ? "(" : "";
                string postParen = withParens ? ")" : "";
                for (int i = 0; i < Middle.Length; i++)
                {
                    yield return new object[] { Pre + preParen + Middle.Substring(0, i) + postParen };
                    yield return new object[] { Pre + preParen + Middle.Substring(0, i) + postParen + Post };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Diagnostic_MalformedCtor_MemberData))]
        public async Task Diagnostic_MalformedCtor(string attribute)
        {
            // Validate the generator doesn't crash with an incomplete attribute

            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    {attribute}
                    private static partial Regex MultipleAttributes();
                }}
            ");

            if (diagnostics.Count != 0)
            {
                Assert.Contains(Assert.Single(diagnostics).Id, new[] { "SYSLIB1040", "SYSLIB1042" });
            }
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"ab[]\"")]
        public async Task Diagnostic_InvalidRegexPattern(string pattern)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex({pattern})]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
        }

        [Theory]
        [InlineData(0x800)]
        public async Task Diagnostic_InvalidRegexOptions(int options)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@$"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(""ab"", (RegexOptions){options})]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(0)]
        public async Task Diagnostic_InvalidRegexTimeout(int matchTimeout)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@$"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(""ab"", RegexOptions.None, {matchTimeout.ToString(CultureInfo.InvariantCulture)})]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"xxxxxxxxxxxxxxxxxxxx-ThisIsNotAValidCultureName-xxxxxxxxxxxxxxxxxxxx\"")]
        public async Task Diagnostic_InvalidCultureName(string cultureName)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@$"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(""(?i)ab"", RegexOptions.None, {cultureName})]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
        }

        [Theory]
        [InlineData("(?i)abc", "RegexOptions.CultureInvariant")]
        [InlineData("abc", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase")]
        public async Task Diagnostic_InvalidOptionsForCaseInsensitive(string pattern, string options)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@$"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(""{pattern}"", {options}, ""en-US"")]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustReturnRegex()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial int MethodMustReturnRegex();
                }
            ");

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustNotBeGeneric()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex MethodMustNotBeGeneric<T>();
                }
            ");

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustBeParameterless()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex MethodMustBeParameterless(int i);
                }
            ");

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustBePartial()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static Regex MethodMustBePartial() => null;
                }
            ");

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustBeNonAbstract()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;

                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    public abstract partial Regex MethodMustBeNonAbstract();
                }

                partial interface I
                {
                    [GeneratedRegex(""ab"")]
                    public static abstract partial Regex MethodMustBeNonAbstract();
                }
            ");

            Assert.Equal(2, diagnostics.Count);
            Assert.All(diagnostics, d => Assert.Equal("SYSLIB1043", d.Id));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.CSharp10)]
        public async Task Diagnostic_InvalidLangVersion_LimitedSupport(LanguageVersion version)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex InvalidLangVersion();
                }
            ", langVersion: version, compile: true);

            Assert.Equal("SYSLIB1044", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_NonBacktracking_LimitedSupport()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"", RegexOptions.NonBacktracking)]
                    private static partial Regex RightToLeftNotSupported();
                }
            ");

            Assert.Equal("SYSLIB1044", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_CaseInsensitiveBackreference_LimitedSupport()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(@""(?i)(ab)\1"")]
                    private static partial Regex CaseInsensitiveBackreferenceNotSupported();
                }
            ");

            Assert.Equal("SYSLIB1044", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_TooMuchNesting_LimitedSupport()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""" + new string('(', 100) + "a" + new string(')', 100) + @""")]
                    private static partial Regex TooMuchNestingNotSupported();
                }
            ");

            Assert.Equal("SYSLIB1044", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_CustomGeneratedRegexAttribute_ZeroArgCtor()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex]
                    private static partial Regex InvalidCtor();
                }

                namespace System.Text.RegularExpressions
                {
                    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
                    public sealed class GeneratedRegexAttribute : Attribute
                    {
                    }
                }
            ");

            Assert.Equal("SYSLIB1040", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_CustomGeneratedRegexAttribute_FiveArgCtor()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""a"", RegexOptions.None, -1, ""en-Us"", ""b""]
                    private static partial Regex InvalidCtor();
                }

                namespace System.Text.RegularExpressions
                {
                    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
                    public sealed class GeneratedRegexAttribute : Attribute
                    {
                        public GeneratedRegexAttribute(string pattern, RegexOptions options, int timeout, string somethingElse) { }
                    }
                }
            ");

            Assert.Equal("SYSLIB1040", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Valid_ClassWithoutNamespace()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Theory]
        [InlineData("RegexOptions.None")]
        [InlineData("RegexOptions.Compiled")]
        [InlineData("RegexOptions.IgnoreCase | RegexOptions.CultureInvariant")]
        public async Task Valid_PatternOptions(string options)
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(""ab"", {options})]
                    private static partial Regex Valid();
                }}
            ", compile: true));
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("1")]
        [InlineData("1_000")]
        public async Task Valid_PatternOptionsTimeout(string timeout)
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(""ab"", RegexOptions.None, {timeout})]
                    private static partial Regex Valid();
                }}
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NamedArguments()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(pattern: ""ab"", options: RegexOptions.None, matchTimeoutMilliseconds: -1)]
                    private static partial Regex Valid();
                }}
            ", compile: true));
        }

        [Fact]
        public async Task Valid_ReorderedNamedArguments()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [GeneratedRegex(options: RegexOptions.None, matchTimeoutMilliseconds: -1, pattern: ""ab"")]
                    private static partial Regex Valid1();

                    [GeneratedRegex(matchTimeoutMilliseconds: -1, pattern: ""ab"", options: RegexOptions.None)]
                    private static partial Regex Valid2();
                }}
            ", compile: true));
        }

        [Fact]
        public async Task Valid_AdditionalAttributes()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                using System.Diagnostics.CodeAnalysis;
                partial class C
                {{
                    [SuppressMessage(""CATEGORY1"", ""SOMEID1"")]
                    [GeneratedRegex(""abc"")]
                    [SuppressMessage(""CATEGORY2"", ""SOMEID2"")]
                    private static partial Regex AdditionalAttributes();
                }}
            ", compile: true));
        }

        public static IEnumerable<object[]> Valid_ClassWithNamespace_ConfigurationOptions_MemberData() =>
            from pattern in new[] { "", "ab", "ab*c|de*?f|(ghi){1,3}", "\\\\w\\\\W\\\\b\\\\B\\\\d\\\\D\\\\s\\\\S" }
            from options in new[] { RegexOptions.None, RegexOptions.IgnoreCase, RegexOptions.ECMAScript, RegexOptions.RightToLeft }
            from allowUnsafe in new[] { false, true }
            from checkOverflow in new[] { false, true }
            select new object[] { pattern, options, allowUnsafe, checkOverflow };

        [Theory]
        [MemberData(nameof(Valid_ClassWithNamespace_ConfigurationOptions_MemberData))]
        public async Task Valid_ClassWithNamespace_ConfigurationOptions(string pattern, RegexOptions options, bool allowUnsafe, bool checkOverflow)
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator($@"
                using System.Text.RegularExpressions;
                namespace A
                {{
                    partial class C
                    {{
                        [GeneratedRegex(""{pattern}"", RegexOptions.{options})]
                        private static partial Regex Valid();
                    }}
                }}
            ", compile: true, allowUnsafe: allowUnsafe, checkOverflow: checkOverflow));
        }

        [Fact]
        public async Task Valid_ClassWithFileScopedNamespace()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_ClassWithNestedNamespaces()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    namespace B
                    {
                        partial class C
                        {
                            [GeneratedRegex(""ab"")]
                            private static partial Regex Valid();
                        }
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassWithoutNamespace()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class B
                {
                    partial class C
                    {
                        [GeneratedRegex(""ab"")]
                        private static partial Regex Valid();
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassWithNamespace()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    partial class B
                    {
                        partial class C
                        {
                            [GeneratedRegex(""ab"")]
                            private static partial Regex Valid();
                        }
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassWithFileScopedNamespace()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A;
                partial class B
                {
                    partial class C
                    {
                        [GeneratedRegex(""ab"")]
                        private static partial Regex Valid();
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassesWithNamespace()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    public partial class B
                    {
                        internal partial class C
                        {
                            protected internal partial class D
                            {
                                private protected partial class E
                                {
                                    private partial class F
                                    {
                                        [GeneratedRegex(""ab"")]
                                        private static partial Regex Valid();
                                    }
                                }
                            }
                        }
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NullableRegex()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                #nullable enable
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex? Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_ClassWithGenericConstraints()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using D;
                using System.Text.RegularExpressions;
                namespace A
                {
                    public partial class B<U>
                    {
                        private partial class C<T> where T : IBlah
                        {
                            [GeneratedRegex(""ab"")]
                            private static partial Regex Valid();
                        }
                    }
                }
                namespace D
                {
                    internal interface IBlah { }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_InterfaceStatics()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;

                partial interface INonGeneric
                {
                    [GeneratedRegex("".+?"")]
                    public static partial Regex Test();
                }

                partial interface IGeneric<T>
                {
                    [GeneratedRegex("".+?"")]
                    public static partial Regex Test();
                }

                partial interface ICovariantGeneric<out T>
                {
                    [GeneratedRegex("".+?"")]
                    public static partial Regex Test();
                }

                partial interface IContravariantGeneric<in T>
                {
                    [GeneratedRegex("".+?"")]
                    public static partial Regex Test();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_VirtualBaseImplementations()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;

                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    public virtual partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_SameMethodNameInMultipleTypes()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    public partial class B<U>
                    {
                        private partial class C<T>
                        {
                            [GeneratedRegex(""1"")]
                            public partial Regex Valid();
                        }

                        private partial class C<T1,T2>
                        {
                            [GeneratedRegex(""2"")]
                            private static partial Regex Valid();

                            private partial class D
                            {
                                [GeneratedRegex(""3"")]
                                internal partial Regex Valid();
                            }
                        }

                        private partial class E
                        {
                            [GeneratedRegex(""4"")]
                            private static partial Regex Valid();
                        }
                    }
                }

                partial class F
                {
                    [GeneratedRegex(""5"")]
                    public partial Regex Valid();

                    [GeneratedRegex(""6"")]
                    public partial Regex Valid2();
                }
            ", compile: true));
        }

        public static IEnumerable<object[]> Valid_Modifiers_MemberData()
        {
            foreach (string type in new[] { "class", "struct", "record", "record struct", "record class", "interface" })
            {
                string[] typeModifiers = type switch
                {
                    "class" => new[] { "", "public", "public sealed", "internal abstract", "internal static" },
                    _ => ["", "public", "internal"]
                };

                foreach (string typeModifier in typeModifiers)
                {
                    foreach (bool instance in typeModifier.Contains("static") ? new[] { false } : [false, true])
                    {
                        string[] methodVisibilities = type switch
                        {
                            "class" when !typeModifier.Contains("sealed") && !typeModifier.Contains("static") => new[] { "public", "internal", "private protected", "protected internal", "private" },
                            _ => ["public", "internal", "private"]
                        };

                        foreach (string methodVisibility in methodVisibilities)
                        {
                            yield return new object[] { type, typeModifier, instance, methodVisibility };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Valid_Modifiers_MemberData))]
        public async Task Valid_Modifiers(string type, string typeModifier, bool instance, string methodVisibility)
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@$"
                using System.Text.RegularExpressions;
                {typeModifier} partial {type} C
                {{
                    [GeneratedRegex(""ab"")]
                    {methodVisibility} {(instance ? "" : "static")} partial Regex Valid();
                }}
            ", compile: true));
        }

        [Fact]
        public async Task Valid_MultiplRegexMethodsPerClass()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C1
                {
                    [GeneratedRegex(""a"")]
                    private static partial Regex A();

                    [GeneratedRegex(""b"")]
                    public static partial Regex B();

                    [GeneratedRegex(""b"")]
                    public static partial Regex C();
                }
                partial class C2
                {
                    [GeneratedRegex(""d"")]
                    public static partial Regex D();

                    [GeneratedRegex(""d"")]
                    public static partial Regex E();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedVaryingTypes()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                public partial class A
                {
                    public partial record class B
                    {
                        public partial record struct C
                        {
                            public partial record D
                            {
                                public partial struct E
                                {
                                    [GeneratedRegex(""ab"")]
                                    public static partial Regex Valid();
                                }
                            }
                        }
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task MultipleTypeDefinitions_DoesntBreakGeneration()
        {
            byte[] referencedAssembly = RegexGeneratorHelper.CreateAssemblyImage(@"
                namespace System.Text.RegularExpressions;

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
                internal sealed class GeneratedRegexAttribute : Attribute
                {
                    public GeneratedRegexAttribute(string pattern){}
                }", "TestAssembly");

            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""abc"")]
                    private static partial Regex Valid();
                }", compile: true, additionalRefs: new[] { MetadataReference.CreateFromImage(referencedAssembly) }));
        }

        [Fact]
        public async Task Valid_ConcatenatedLiteralsArgument()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;

                partial class C
                {
                    [GeneratedRegex(""ab"" + ""[cd]"")]
                    public static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_InterpolatedLiteralsArgument()
        {
            Assert.Empty(await RegexGeneratorHelper.RunGenerator(@"
                using System.Text.RegularExpressions;

                partial class C
                {
                    [GeneratedRegex($""{""ab""}{""cd""}"")]
                    public static partial Regex Valid();
                }", compile: true));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [OuterLoop("Takes several seconds")]
        public void Deterministic_SameRegexProducesSameSource()
        {
            string first = Generate();
            for (int trials = 0; trials < 3; trials++)
            {
                Assert.Equal(first, Generate());
            }

            static string Generate()
            {
                const string Code =
                    @"using System.Text.RegularExpressions;
                    partial class C
                    {
                        [GeneratedRegex(""(?<Name>\w+) (?<Street>\w+), (?<City>\w+) (?<State>[A-Z]{2}) (?<Zip>[0-9]{5})"")]
                        public static partial Regex Valid();
                    }";

                // Generate the source in a new process so that any process-specific randomization is different between runs,
                // e.g. hash code randomization for strings.

                using RemoteInvokeHandle handle = RemoteExecutor.Invoke(
                    async () => Console.WriteLine(await RegexGeneratorHelper.GenerateSourceText(Code)),
                    new RemoteInvokeOptions { StartInfo = new ProcessStartInfo { RedirectStandardOutput = true } });

                return handle.Process.StandardOutput.ReadToEnd();
            }
        }
    }
}
