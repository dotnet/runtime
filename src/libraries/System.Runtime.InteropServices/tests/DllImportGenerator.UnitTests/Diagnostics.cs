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

namespace DllImportGenerator.UnitTests
{
    public class Diagnostics
    {
        [ConditionalTheory]
        [InlineData(TestTargetFramework.Framework)]
        [InlineData(TestTargetFramework.Core)]
        [InlineData(TestTargetFramework.Standard)]
        [InlineData(TestTargetFramework.Net5)]
        public async Task TargetFrameworkNotSupported_NoDiagnostic(TestTargetFramework targetFramework)
        {
            string source = $@"
using System.Runtime.InteropServices;
{CodeSnippets.GeneratedDllImportAttributeDeclaration}
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method();
}}
";
            Compilation comp = await TestUtils.CreateCompilation(source, targetFramework);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(generatorDiags);

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [ConditionalTheory]
        [InlineData(TestTargetFramework.Framework)]
        [InlineData(TestTargetFramework.Core)]
        [InlineData(TestTargetFramework.Standard)]
        [InlineData(TestTargetFramework.Net5)]
        public async Task TargetFrameworkNotSupported_NoGeneratedDllImport_NoDiagnostic(TestTargetFramework targetFramework)
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [DllImport(""DoesNotExist"")]
    public static extern void Method();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source, targetFramework);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(generatorDiags);

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [ConditionalFact]
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
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method1(NS.MyClass c);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2(int i, List<int> list);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
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

        [ConditionalFact]
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
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial NS.MyClass Method1();

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial List<int> Method2();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
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

        [ConditionalFact]
        public async Task ParameterTypeNotSupportedWithDetails_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(char c, string s);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
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

        [ConditionalFact]
        public async Task ReturnTypeNotSupportedWithDetails_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial char Method1();

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial string Method2();
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
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

        [ConditionalFact]
        public async Task ParameterConfigurationNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method1([MarshalAs(UnmanagedType.BStr)] int i1, int i2);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2(bool b1, [MarshalAs(UnmanagedType.FunctionPtr)] bool b2);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ParameterConfigurationNotSupported))
                    .WithSpan(6, 76, 6, 78)
                    .WithArguments(nameof(MarshalAsAttribute), "i1"),
                (new DiagnosticResult(GeneratorDiagnostics.ParameterConfigurationNotSupported))
                    .WithSpan(9, 93, 9, 95)
                    .WithArguments(nameof(MarshalAsAttribute), "b2"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [ConditionalFact]
        public async Task ReturnConfigurationNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.BStr)]
    public static partial int Method1(int i);

    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static partial bool Method2(bool b);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
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

        [ConditionalFact]
        public async Task MarshalAsUnmanagedTypeNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(1)]
    public static partial int Method1(int i);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial bool Method2([MarshalAs((short)0)] bool b);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationValueNotSupported))
                    .WithSpan(6, 14, 6, 26)
                    .WithArguments(1, nameof(UnmanagedType)),
                (new DiagnosticResult(GeneratorDiagnostics.ReturnConfigurationNotSupported))
                    .WithSpan(7, 31, 7, 38)
                    .WithArguments(nameof(MarshalAsAttribute), "Method1"),
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationValueNotSupported))
                    .WithSpan(10, 41, 10, 60)
                    .WithArguments(0, nameof(UnmanagedType)),
                (new DiagnosticResult(GeneratorDiagnostics.ParameterConfigurationNotSupported))
                    .WithSpan(10, 67, 10, 68)
                    .WithArguments(nameof(MarshalAsAttribute), "b"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        [ConditionalFact]
        public async Task MarshalAsFieldNotSupported_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.I4, SafeArraySubType=VarEnum.VT_I4)]
    public static partial int Method1(int i);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial bool Method2([MarshalAs(UnmanagedType.I1, IidParameterIndex = 1)] bool b);
}
";
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationNotSupported))
                    .WithSpan(6, 14, 6, 73)
                    .WithArguments($"{nameof(MarshalAsAttribute)}{Type.Delimiter}{nameof(MarshalAsAttribute.SafeArraySubType)}"),
                (new DiagnosticResult(GeneratorDiagnostics.ConfigurationNotSupported))
                    .WithSpan(10, 41, 10, 91)
                    .WithArguments($"{nameof(MarshalAsAttribute)}{Type.Delimiter}{nameof(MarshalAsAttribute.IidParameterIndex)}"),
            };
            VerifyDiagnostics(expectedDiags, GetSortedDiagnostics(generatorDiags));
            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        private static void VerifyDiagnostics(DiagnosticResult[] expectedDiagnostics, Diagnostic[] actualDiagnostics)
        {
            Assert.Equal(expectedDiagnostics.Length, actualDiagnostics.Length);
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
