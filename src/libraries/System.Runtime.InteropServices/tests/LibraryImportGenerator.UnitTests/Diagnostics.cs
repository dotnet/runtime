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
using Microsoft.Interop.UnitTests;
using Xunit;

using StringMarshalling = Microsoft.Interop.StringMarshalling;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

namespace LibraryImportGenerator.UnitTests
{
    public class Diagnostics
    {
        [Fact]
        public async Task ParameterTypeNotSupported_ReportsDiagnostic()
        {
            string source = """

                using System.Collections.Generic;
                using System.Runtime.InteropServices;
                namespace NS
                {
                    class MyClass { }
                }
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void Method1(NS.MyClass {|#0:c|});

                    [LibraryImport("DoesNotExist")]
                    public static partial void Method2(int i, List<int> {|#1:list|});
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported)
                    .WithLocation(0)
                    .WithArguments("NS.MyClass", "c"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported)
                    .WithLocation(1)
                    .WithArguments("System.Collections.Generic.List<int>", "list"));
        }

        [Fact]
        public async Task ReturnTypeNotSupported_ReportsDiagnostic()
        {
            string source = """

                using System.Collections.Generic;
                using System.Runtime.InteropServices;
                namespace NS
                {
                    class MyClass { }
                }
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial NS.MyClass {|#0:Method1|}();

                    [LibraryImport("DoesNotExist")]
                    public static partial List<int> {|#1:Method2|}();
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupported)
                    .WithLocation(0)
                    .WithArguments("NS.MyClass", "Method1"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupported)
                    .WithLocation(1)
                    .WithArguments("System.Collections.Generic.List<int>", "Method2"));
        }

        [Fact]
        public async Task ParameterTypeNotSupportedWithDetails_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void Method(char {|#0:c|}, string {|#1:s|});
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "c"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "s"));
        }

        [Fact]
        public async Task ReturnTypeNotSupportedWithDetails_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial char {|#0:Method1|}();

                    [LibraryImport("DoesNotExist")]
                    public static partial string {|#1:Method2|}();
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "Method1"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "Method2"));
        }

        [Fact]
        public async Task ParameterConfigurationNotSupported_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void Method1([MarshalAs(UnmanagedType.BStr)] int {|#0:i1|}, int i2);

                    [LibraryImport("DoesNotExist")]
                    public static partial void Method2(int i1, [MarshalAs(UnmanagedType.FunctionPtr)] bool {|#1:b2|});
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(nameof(MarshalAsAttribute), "i1"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments(nameof(MarshalAsAttribute), "b2"));
        }

        [Fact]
        public async Task ReturnConfigurationNotSupported_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.BStr)]
                    public static partial int {|#0:Method1|}(int i);

                    [LibraryImport("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.FunctionPtr)]
                    public static partial bool {|#1:Method2|}(int i);
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(nameof(MarshalAsAttribute), "Method1"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments(nameof(MarshalAsAttribute), "Method2"));
        }

        [Fact]
        public async Task MarshalAsUnmanagedTypeNotSupported_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    [return: {|#0:MarshalAs(1)|}]
                    public static partial int {|#1:Method1|}(int i);

                    [LibraryImport("DoesNotExist")]
                    public static partial int Method2([{|#2:MarshalAs((short)0)|}] bool {|#3:b|});
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(0)
                    .WithArguments(1, nameof(UnmanagedType)),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments(nameof(MarshalAsAttribute), "Method1"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(2)
                    .WithArguments(0, nameof(UnmanagedType)),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(3)
                    .WithArguments(nameof(MarshalAsAttribute), "b"));
        }

        [Fact]
        public async Task MarshalAsFieldNotSupported_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    [return: {|#0:MarshalAs(UnmanagedType.I4, SafeArraySubType=VarEnum.VT_I4)|}]
                    public static partial int Method1(int i);

                    [LibraryImport("DoesNotExist")]
                    public static partial int Method2([{|#1:MarshalAs(UnmanagedType.I1, IidParameterIndex = 1)|}] bool b);
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments($"{nameof(MarshalAsAttribute)}{Type.Delimiter}{nameof(MarshalAsAttribute.SafeArraySubType)}"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments($"{nameof(MarshalAsAttribute)}{Type.Delimiter}{nameof(MarshalAsAttribute.IidParameterIndex)}"));
        }

        [Fact]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task StringMarshallingForwardingNotSupported_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8)]
                    public static partial void {|#0:Method1|}(string s);

                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Native))]
                    public static partial void Method2(string {|#1:s|});

                    struct Native
                    {
                        public Native(string s) { }
                        public string ToManaged() => default;
                    }
                }
                """ + CodeSnippets.LibraryImportAttributeDeclaration;
            DiagnosticResult[] expectedDiags = new DiagnosticResult[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.CannotForwardToDllImport)
                    .WithLocation(0)
                    .WithArguments($"{nameof(TypeNames.LibraryImportAttribute)}{Type.Delimiter}{nameof(StringMarshalling)}={nameof(StringMarshalling)}{Type.Delimiter}{nameof(StringMarshalling.Utf8)}"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "s")
            };

            var test = new VerifyCS.Test(TestTargetFramework.Standard)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };
            test.ExpectedDiagnostics.AddRange(expectedDiags);
            await test.RunAsync();
        }

        [Fact]
        public async Task InvalidStringMarshallingConfiguration_ReportsDiagnostic()
        {
            string source = $$"""

                using System.Runtime.InteropServices;
                {{CodeSnippets.DisableRuntimeMarshalling}}
                partial class Test
                {
                    [{|#0:LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Custom)|}]
                    public static partial void Method1(out int i);
                
                    [{|#1:LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf8, StringMarshallingCustomType = typeof(Native))|}]
                    public static partial void Method2(out int i);
                
                    struct Native
                    {
                        public Native(string s) { }
                        public string ToManaged() => default;
                    }
                }
                """;


            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidStringMarshallingConfiguration)
                    .WithLocation(0)
                    .WithArguments("Method1", "'StringMarshallingCustomType' must be specified when 'StringMarshalling' is set to 'StringMarshalling.Custom'."),
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidStringMarshallingConfiguration)
                    .WithLocation(1)
                    .WithArguments("Method2", "'StringMarshalling' should be set to 'StringMarshalling.Custom' when 'StringMarshallingCustomType' is specified."));
        }

        [Fact]
        public async Task NonPartialMethod_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static void {|#0:Method|}() { }

                    [LibraryImport("DoesNotExist")]
                    public static extern void {|#1:ExternMethod|}();
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidAttributedMethodSignature)
                    .WithLocation(0)
                    .WithArguments("Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidAttributedMethodSignature)
                    .WithLocation(1)
                    .WithArguments("ExternMethod"));
        }

        [Fact]
        public async Task NonStaticMethod_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public partial void {|#0:Method|}();
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidAttributedMethodSignature)
                    .WithLocation(0)
                    .WithArguments("Method"),
                // Generator ignores the method
                DiagnosticResult.CompilerError("CS8795")
                    .WithLocation(0));
        }

        [Fact]
        public async Task GenericMethod_ReportsDiagnostic()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|#0:Method1|}<T>();

                    [LibraryImport("DoesNotExist")]
                    public static partial void {|#1:Method2|}<T, U>();
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidAttributedMethodSignature)
                    .WithLocation(0)
                    .WithArguments("Method1"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidAttributedMethodSignature)
                    .WithLocation(1)
                    .WithArguments("Method2"),
                // Generator ignores the method
                DiagnosticResult.CompilerError("CS8795")
                    .WithLocation(0),
                DiagnosticResult.CompilerError("CS8795")
                    .WithLocation(1));
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task NonPartialParentType_Diagnostic(string typeKind)
        {
            string source = $$"""

                using System.Runtime.InteropServices;
                {{typeKind}} Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|#0:Method|}();
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers)
                    .WithLocation(0)
                    .WithArguments("Method", "Test"),
                // Generator ignores the method
                DiagnosticResult.CompilerError("CS8795")
                    .WithLocation(0),
                // Also expect CS0751: A partial method must be declared within a partial type
                DiagnosticResult.CompilerError("CS0751")
                    .WithLocation(0));
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task NonPartialGrandparentType_Diagnostic(string typeKind)
        {
            string source = $$"""

                using System.Runtime.InteropServices;
                {{typeKind}} Test
                {
                    partial class TestInner
                    {
                        [LibraryImport("DoesNotExist")]
                        static partial void {|#0:Method|}();
                    }
                }
                """;

            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers)
                    .WithLocation(0)
                    .WithArguments("Method", "Test"));
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
