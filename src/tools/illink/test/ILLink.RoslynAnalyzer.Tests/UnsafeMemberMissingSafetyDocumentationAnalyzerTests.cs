// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies <c>IL5005</c> coverage across caller-unsafe member kinds and related documentation locations.
    /// The cases also confirm that declarations which cannot expose an unsafe contract are ignored.
    /// </summary>
    public class UnsafeMemberMissingSafetyDocumentationAnalyzerTests
    {
        [Fact]
        public void IsDisabledByDefault()
        {
            var analyzer = new UnsafeMemberMissingSafetyDocumentationAnalyzer();

            Assert.False(analyzer.SupportedDiagnostics[0].IsEnabledByDefault);
        }

        [Fact]
        public async Task ReportsAllUnsafeContractMemberKinds()
        {
            var source = """
                using System;

                class C
                {
                    public {|IL5005:unsafe|} C(int value) { }
                    public {|IL5005:unsafe|} void Method() { }
                    public static {|IL5005:unsafe|} C operator +(C left, C right) => left;
                    public static {|IL5005:unsafe|} explicit operator int(C value) => 0;
                    public {|IL5005:unsafe|} int Property { get; set; }
                    public {|IL5005:unsafe|} int this[int index] => index;

                    public int Getter
                    {
                        {|IL5005:unsafe|} get => 0;
                        set { }
                    }

                    public int Initializer
                    {
                        get => 0;
                        {|IL5005:unsafe|} init { }
                    }

                    public int Setter
                    {
                        get => 0;
                        {|IL5005:unsafe|} set { }
                    }

                    public {|IL5005:unsafe|} event Action Event
                    {
                        add { }
                        remove { }
                    }

                    public {|IL5005:unsafe|} event Action EventField;
                    public {|IL5005:unsafe|} int Field1, Field2;

                    public void Outer()
                    {
                        {|IL5005:unsafe|} void Local() { }
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeMemberMissingSafetyDocumentationAnalyzer>(source);
            await test.RunAsync();
        }

        [Fact]
        public async Task SafetyDocumentationSuppressesDiagnosticsAcrossRelatedDeclarations()
        {
            var source = """
                partial class C
                {
                    /// <safety>Callers must satisfy the documented preconditions.</safety>
                    public unsafe partial void Partial();

                    public unsafe partial void Partial() { }

                    /// <safety />
                    public unsafe int Property { get; set; }

                    /// <safety>The getter validates its state.</safety>
                    public int Accessor
                    {
                        unsafe get => 0;
                    }

                    public void Outer()
                    {
                        /// <safety>The local function is only called after validation.</safety>
                        unsafe void Local() { }
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeMemberMissingSafetyDocumentationAnalyzer>(source);
            await test.RunAsync();
        }

        [Fact]
        public async Task IgnoresDeclarationsThatCannotExposeUnsafeContracts()
        {
            var source = """
                class C
                {
                    static unsafe C() { }
                    unsafe ~C() { }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeMemberMissingSafetyDocumentationAnalyzer>(source);
            await test.RunAsync();
        }
    }
}
#endif
