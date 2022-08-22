// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;

using StringMarshalling = Microsoft.Interop.StringMarshalling;

namespace LibraryImportGenerator.UnitTests
{
    public class Diagnostics
    {
        [Fact]
        public async Task ParameterTypeNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace NS
{
    class MyClass { }
}
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public static partial void Method1(NS.MyClass c);

    [LibraryImport(""DoesNotExist"")]
    public static partial void Method2(int i, List<int> list);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupported))
                    .WithSpan(11, 51, 11, 52)
                    .WithArguments("NS.MyClass", "c"),
                (new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupported))
                    .WithSpan(14, 57, 14, 61)
                    .WithArguments("System.Collections.Generic.List<int>", "list"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task ReturnTypeNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace NS
{
    class MyClass { }
}
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public static partial NS.MyClass Method1();

    [LibraryImport(""DoesNotExist"")]
    public static partial List<int> Method2();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ReturnTypeNotSupported))
                    .WithSpan(11, 38, 11, 45)
                    .WithArguments("NS.MyClass", "Method1"),
                (new DiagnosticResult(GeneratorDiagnostics.ReturnTypeNotSupported))
                    .WithSpan(14, 37, 14, 44)
                    .WithArguments("System.Collections.Generic.List<int>", "Method2"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task ParameterTypeNotSupportedWithDetails_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public static partial void Method(char c, string s);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails))
                    .WithSpan(6, 44, 6, 45),
                (new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails))
                    .WithSpan(6, 54, 6, 55),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task ReturnTypeNotSupportedWithDetails_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public static partial char Method1();

    [LibraryImport(""DoesNotExist"")]
    public static partial string Method2();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails))
                    .WithSpan(6, 32, 6, 39),
                (new DiagnosticResult(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails))
                    .WithSpan(9, 34, 9, 41),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task ParameterConfigurationNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public static partial void Method1([MarshalAs(UnmanagedType.BStr)] int i1, int i2);

    [LibraryImport(""DoesNotExist"")]
    public static partial void Method2(int i1, [MarshalAs(UnmanagedType.FunctionPtr)] bool b2);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ParameterConfigurationNotSupported))
                    .WithSpan(6, 76, 6, 78)
                    .WithArguments(nameof(MarshalAsAttribute), "i1"),
                (new DiagnosticResult(GeneratorDiagnostics.ParameterConfigurationNotSupported))
                    .WithSpan(9, 92, 9, 94)
                    .WithArguments(nameof(MarshalAsAttribute), "b2"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task ReturnConfigurationNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.BStr)]
    public static partial int Method1(int i);

    [LibraryImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static partial bool Method2(int i);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ReturnConfigurationNotSupported))
                    .WithSpan(7, 31, 7, 38)
                    .WithArguments(nameof(MarshalAsAttribute), "Method1"),
                (new DiagnosticResult(GeneratorDiagnostics.ReturnConfigurationNotSupported))
                    .WithSpan(11, 32, 11, 39)
                    .WithArguments(nameof(MarshalAsAttribute), "Method2"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task MarshalAsUnmanagedTypeNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    [return: MarshalAs(1)]
    public static partial int Method1(int i);

    [LibraryImport(""DoesNotExist"")]
    public static partial int Method2([MarshalAs((short)0)] bool b);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationValueNotSupported))
                    .WithSpan(6, 14, 6, 26)
                    .WithArguments(1, nameof(UnmanagedType)),
                (new DiagnosticResult(GeneratorDiagnostics.ReturnConfigurationNotSupported))
                    .WithSpan(7, 31, 7, 38)
                    .WithArguments(nameof(MarshalAsAttribute), "Method1"),
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationValueNotSupported))
                    .WithSpan(10, 40, 10, 59)
                    .WithArguments(0, nameof(UnmanagedType)),
                (new DiagnosticResult(GeneratorDiagnostics.ParameterConfigurationNotSupported))
                    .WithSpan(10, 66, 10, 67)
                    .WithArguments(nameof(MarshalAsAttribute), "b"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task MarshalAsFieldNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.I4, SafeArraySubType=VarEnum.VT_I4)]
    public static partial int Method1(int i);

    [LibraryImport(""DoesNotExist"")]
    public static partial int Method2([MarshalAs(UnmanagedType.I1, IidParameterIndex = 1)] bool b);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationNotSupported))
                    .WithSpan(6, 14, 6, 73)
                    .WithArguments($"{nameof(MarshalAsAttribute)}{Type.Delimiter}{nameof(MarshalAsAttribute.SafeArraySubType)}"),
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationNotSupported))
                    .WithSpan(10, 40, 10, 90)
                    .WithArguments($"{nameof(MarshalAsAttribute)}{Type.Delimiter}{nameof(MarshalAsAttribute.IidParameterIndex)}"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));
            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task StringMarshallingForwardingNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void Method1(string s);

    [LibraryImport(""DoesNotExist"", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Native))]
    public static partial void Method2(string s);

    struct Native
    {
        public Native(string s) { }
        public string ToManaged() => default;
    }
}
" + CodeSnippets.LibraryImportAttributeDeclaration;

            // Compile against Standard so that we generate forwarders
            Compilation comp = await TestUtils.CreateCompilation(source, TestTargetFramework.Standard);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.CannotForwardToDllImport))
                    .WithSpan(6, 32, 6, 39)
                    .WithArguments($"{nameof(TypeNames.LibraryImportAttribute)}{Type.Delimiter}{nameof(StringMarshalling)}={nameof(StringMarshalling)}{Type.Delimiter}{nameof(StringMarshalling.Utf8)}"),
                (new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails))
                    .WithSpan(9, 47, 9, 48)
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));
            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task InvalidStringMarshallingConfiguration_ReportsDiagnostic()
        {
            string source = @$"
using System.Runtime.InteropServices;
{CodeSnippets.DisableRuntimeMarshalling}
partial class Test
{{
    [LibraryImport(""DoesNotExist"", StringMarshalling = StringMarshalling.Custom)]
    public static partial void Method1(out int i);

    [LibraryImport(""DoesNotExist"", StringMarshalling = StringMarshalling.Utf8, StringMarshallingCustomType = typeof(Native))]
    public static partial void Method2(out int i);

    struct Native
    {{
        public Native(string s) {{ }}
        public string ToManaged() => default;
    }}
}}
";

            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.InvalidStringMarshallingConfiguration))
                    .WithSpan(6, 6, 6, 81),
                (new DiagnosticResult(GeneratorDiagnostics.InvalidStringMarshallingConfiguration))
                    .WithSpan(9, 6, 9, 125)
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));
            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task NonPartialMethod_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public static void Method() { }

    [LibraryImport(""DoesNotExist"")]
    public static extern void ExternMethod();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedMethodSignature))
                    .WithSpan(6, 24, 6, 30)
                    .WithArguments("Method"),
                (new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedMethodSignature))
                    .WithSpan(9, 31, 9, 43)
                    .WithArguments("ExternMethod"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));
            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [Fact]
        public async Task NonStaticMethod_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public partial void Method();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedMethodSignature))
                    .WithSpan(6, 25, 6, 31)
                    .WithArguments("Method")
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            // Generator ignores the method
            TestUtils.AssertPreSourceGeneratorCompilation(newComp);
        }

        [Fact]
        public async Task GenericMethod_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LibraryImport(""DoesNotExist"")]
    public static partial void Method1<T>();

    [LibraryImport(""DoesNotExist"")]
    public static partial void Method2<T, U>();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedMethodSignature))
                    .WithSpan(6, 32, 6, 39)
                    .WithArguments("Method1"),
                (new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedMethodSignature))
                    .WithSpan(9, 32, 9, 39)
                    .WithArguments("Method2"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            // Generator ignores the method
            TestUtils.AssertPreSourceGeneratorCompilation(newComp);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task NonPartialParentType_Diagnostic(string typeKind)
        {
            string source = $@"
using System.Runtime.InteropServices;
{typeKind} Test
{{
    [LibraryImport(""DoesNotExist"")]
    public static partial void Method();
}}
";
            Compilation comp = await TestUtils.CreateCompilation(source);

            // Also expect CS0751: A partial method must be declared within a partial type
            string additionalDiag = "CS0751";
            TestUtils.AssertPreSourceGeneratorCompilation(comp, additionalDiag);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers))
                    .WithSpan(6, 32, 6, 38)
                    .WithArguments("Method", "Test"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            // Generator ignores the method
            TestUtils.AssertPreSourceGeneratorCompilation(newComp, additionalDiag);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task NonPartialGrandparentType_Diagnostic(string typeKind)
        {
            string source = $@"
using System.Runtime.InteropServices;
{typeKind} Test
{{
    partial class TestInner
    {{
        [LibraryImport(""DoesNotExist"")]
        static partial void Method();
    }}
}}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers))
                    .WithSpan(8, 29, 8, 35)
                    .WithArguments("Method", "Test"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            // Generator ignores the method
            TestUtils.AssertPreSourceGeneratorCompilation(newComp);
        }

        private static void VerifyDiagnostics(DiagnosticResult[] expectedDiagnostics, Diagnostic[] actualDiagnostics)
        {
            Assert.True(expectedDiagnostics.Length == actualDiagnostics.Length,
                $"Expected {expectedDiagnostics.Length} diagnostics, but encountered {actualDiagnostics.Length}. Actual diagnostics:{Environment.NewLine}{string.Join(Environment.NewLine, actualDiagnostics.Select(d => d.ToString()))}");
            for (var i = 0; i < expectedDiagnostics.Length; i++)
            {
                DiagnosticResult expected = expectedDiagnostics[i];
                Diagnostic actual = actualDiagnostics[i];

                Assert.Equal(expected.Id, actual.Id);
                Assert.Equal(expected.Severity, actual.Severity);
                if (expected.HasLocation)
                {
                    FileLinePositionSpan expectedSpan = expected.Spans[0].Span;
                    FileLinePositionSpan actualSpan = actual.Location.GetLineSpan();
                    Assert.Equal(expectedSpan, actualSpan);
                }

                if (expected.MessageArguments is null)
                {
                    Assert.Equal(expected.MessageFormat, actual.Descriptor.MessageFormat);
                }
                else
                {
                    Assert.Equal(expected.Message, actual.GetMessage());
                }
            }
        }

        private static Diagnostic[] GetSortedDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics
                .OrderBy(d => d.Location.GetLineSpan().Path, StringComparer.Ordinal)
                .ThenBy(d => d.Location.SourceSpan.Start)
                .ThenBy(d => d.Location.SourceSpan.End)
                .ThenBy(d => d.Id)
                .ToArray();
        }
    }
}
