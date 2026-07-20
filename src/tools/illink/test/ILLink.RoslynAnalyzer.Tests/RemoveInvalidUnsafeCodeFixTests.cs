// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies removal of invalid <c>unsafe</c> modifiers reported by <c>CS9377</c> and <c>CS0106</c>.
    /// The cases cover meaningless type-level scopes and declaration kinds where unsafe is syntactically invalid.
    /// </summary>
    public class RemoveInvalidUnsafeCodeFixTests
    {
        public static TheoryData<string, string> InvalidDeclarations => new()
        {
            {
                "unsafe class {|CS9377:C|} { }",
                "class C { }"
            },
            {
                "unsafe struct {|CS9377:S|} { }",
                "struct S { }"
            },
            {
                "unsafe interface {|CS9377:I|} { }",
                "interface I { }"
            },
            {
                "unsafe record {|CS9377:R|};",
                "record R;"
            },
            {
                "unsafe delegate void {|CS9377:D|}();",
                "delegate void D();"
            },
            {
                "unsafe enum {|CS0106:E|} { A }",
                "enum E { A }"
            },
            {
                "class C { unsafe const int {|CS0106:F|} = 0; }",
                "class C { const int F = 0; }"
            },
        };

        [Theory]
        [MemberData(nameof(InvalidDeclarations))]
        public async Task RemovesInvalidUnsafeModifier(string source, string fixedSource)
        {
            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, RemoveInvalidUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }
    }
}
#endif
