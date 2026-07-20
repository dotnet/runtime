// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies <c>IL5006</c> for pointer and function-pointer signatures across supported member kinds.
    /// The suppression cases cover explicit modifiers, safety documentation, partials, constraints, and fixed buffers.
    /// </summary>
    public class PointerSignatureRequiresUnsafeAnalyzerTests
    {
        [Fact]
        public void IsDisabledByDefault()
        {
            var analyzer = new PointerSignatureRequiresUnsafeAnalyzer();

            Assert.False(analyzer.SupportedDiagnostics[0].IsEnabledByDefault);
        }

        [Fact]
        public async Task ReportsPointerSignaturesOnAllSupportedMemberKinds()
        {
            var source = """
                class Outer<T>
                {
                    public delegate void D();
                }

                class C
                {
                    public int* {|IL5006:Method|}(int*[] values) => values[0];
                    public {|IL5006:C|}(delegate*<void> callback) { }
                    public int* {|IL5006:Property|} { get; set; }
                    public int* {|IL5006:this|}[int* index] => index;
                    public delegate*<void> {|IL5006:Field|};
                    public event Outer<int*[]>.D {|IL5006:Event|};
                    public event Outer<int*[]>.D {|IL5006:ManualEvent|}
                    {
                        add { }
                        remove { }
                    }
                    public static int* operator {|IL5006:+|}(C value, int offset) => null;
                    public static explicit operator {|IL5006:int*|}(C value) => null;

                    public void OuterMethod()
                    {
                        int* {|IL5006:Local|}(int* value) => value;
                        _ = Local(null);
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<PointerSignatureRequiresUnsafeAnalyzer>(source);
            await test.RunAsync();
        }

        [Fact]
        public async Task SafetyDocumentationAndExplicitModifiersSuppressDiagnostics()
        {
            var source = """
                interface I<T> { }

                class C
                {
                    /// <safety>The pointer is never dereferenced.</safety>
                    public void Documented(void* value) { }

                    public unsafe void Unsafe(void* value) { }
                    public safe extern void Extern(void* value);

                    public void ConstraintOnly<T>() where T : I<int*[]> { }
                    public T TypeParameterOnly<T>(T value) => value;
                }

                struct S
                {
                    public fixed int Buffer[4];
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<PointerSignatureRequiresUnsafeAnalyzer>(source);
            await test.RunAsync();
        }

        [Fact]
        public async Task DocumentationOnOnePartialDeclarationSuppressesBothParts()
        {
            var source = """
                partial class C
                {
                    /// <safety>The returned pointer remains valid for the documented lifetime.</safety>
                    public partial int* M();

                    public partial int* M() => null;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<PointerSignatureRequiresUnsafeAnalyzer>(source);
            await test.RunAsync();
        }
    }
}
#endif
