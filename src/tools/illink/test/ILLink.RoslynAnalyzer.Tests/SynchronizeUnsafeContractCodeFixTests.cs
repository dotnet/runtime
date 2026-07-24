// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies one-way unsafe propagation for compiler-reported contract mismatches.
    /// </summary>
    public class SynchronizeUnsafeContractCodeFixTests
    {
        [Fact]
        public async Task CompilerOverrideFixPropagatesUpAndDown()
        {
            var source = """
                class Base
                {
                    public virtual void Method() { }
                }

                class First : Base
                {
                    public override unsafe void {|CS9364:Method|}() { }
                }

                class Second : Base
                {
                    public override void Method() { }
                }
                """;
            var fixedSource = """
                class Base
                {
                    /// <safety>TODO: Audit</safety>
                    public virtual unsafe void Method() { }
                }

                class First : Base
                {
                    public override unsafe void Method() { }
                }

                class Second : Base
                {
                    public override void Method() { }
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task CompilerInterfaceFixPropagatesToEveryImplementation()
        {
            var source = """
                interface I
                {
                    void Method();
                }

                class Implicit : I
                {
                    public unsafe void {|CS9365:Method|}() { }
                }

                class Explicit : I
                {
                    unsafe void I.{|CS9366:Method|}() { }
                }

                class Other : I
                {
                    public void Method() { }
                }
                """;
            var fixedSource = """
                interface I
                {
                    /// <safety>TODO: Audit</safety>
                    unsafe void Method();
                }

                class Implicit : I
                {
                    public unsafe void Method() { }
                }

                class Explicit : I
                {
                    unsafe void I.Method() { }
                }

                class Other : I
                {
                    public void Method() { }
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task CompilerPropertyFixPropagatesPropertyContract()
        {
            var source = """
                interface I
                {
                    int Property { get; }
                }

                class UnsafeImplementation : I
                {
                    public unsafe int Property => {|CS9365:0|};
                }

                class OtherImplementation : I
                {
                    public int Property => 0;
                }
                """;
            var fixedSource = """
                interface I
                {
                    /// <safety>TODO: Audit</safety>
                    unsafe int Property { get; }
                }

                class UnsafeImplementation : I
                {
                    public unsafe int Property => 0;
                }

                class OtherImplementation : I
                {
                    public int Property => 0;
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task PropagatesToInterfaceEvenWhenOtherImplementationIsSynthesized()
        {
            var source = """
                interface I
                {
                    int Property { get; }
                }

                class UnsafeImplementation : I
                {
                    public unsafe int Property => {|CS9365:0|};
                }

                record OtherImplementation(int Property) : I;
                """;
            var fixedSource = """
                interface I
                {
                    /// <safety>TODO: Audit</safety>
                    unsafe int Property { get; }
                }

                class UnsafeImplementation : I
                {
                    public unsafe int Property => 0;
                }

                record OtherImplementation(int Property) : I;
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task CompilerDefaultInterfaceFixPropagatesToBaseContract()
        {
            var source = """
                interface IBase
                {
                    void Method();
                }

                interface IDerived : IBase
                {
                    unsafe void IBase.{|CS9366:Method|}() { }
                }
                """;
            var fixedSource = """
                interface IBase
                {
                    /// <safety>TODO: Audit</safety>
                    unsafe void Method();
                }

                interface IDerived : IBase
                {
                    unsafe void IBase.Method() { }
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task NoFixForInheritedImplementationDiagnosticOnType()
        {
            var source = """
                interface I
                {
                    void Method();
                }

                class Base
                {
                    public unsafe void Method() { }
                }

                class Derived : Base, {|CS9365:I|}
                {
                }
                """;

            await CreateCompilerTest(source).RunAsync();
        }

        [Fact]
        public async Task NoFixForUneditableMetadataContract()
        {
            var source = """
                class UnsafeImplementation : I
                {
                    public unsafe void {|CS9365:Method|}() { }
                }

                class SafeImplementation : I
                {
                    public void Method() { }
                }
                """;

            var test = CreateCompilerTest(source);
            test.TestState.AdditionalReferences.Add(CreateReference(
                """
                public interface I
                {
                    void Method();
                }
                """));
            await test.RunAsync();
        }

        [Fact]
        public async Task PartialUnsafeMismatchAddsUnsafeToEveryPart()
        {
            var source = """
                partial class C
                {
                    public partial void Method();
                    public unsafe partial void {|CS0764:Method|}() { }
                }
                """;
            var fixedSource = """
                partial class C
                {
                    /// <safety>TODO: Audit</safety>
                    public unsafe partial void Method();
                    public unsafe partial void Method() { }
                }
                """;

            var test = CreateCompilerTest(source, fixedSource);
            test.CodeActionEquivalenceKey = "Propagate unsafe contract";
            await test.RunAsync();
        }

        [Fact]
        public async Task UnsafeWinsOverSafePartialContract()
        {
            var source = """
                partial class C
                {
                    public unsafe partial void Method();
                    public safe extern partial void {|CS0764:{|CS9390:Method|}|}();
                }
                """;
            var fixedSource = """
                partial class C
                {
                    public unsafe partial void Method();
                    /// <safety>TODO: Audit</safety>
                    public unsafe extern partial void Method();
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task PureSafePartialMismatchDefaultsToUnsafe()
        {
            var source = """
                partial class C
                {
                    public partial void {|CS9389:Method|}();
                    public safe extern partial void {|CS9390:Method|}();
                }
                """;
            var fixedSource = """
                partial class C
                {
                    /// <safety>TODO: Audit</safety>
                    public unsafe partial void Method();
                    /// <safety>TODO: Audit</safety>
                    public unsafe extern partial void Method();
                }
                """;

            var test = CreateCompilerTest(source, fixedSource);
            // Replacing a deliberate audit is offered under a title that says so.
            test.CodeActionEquivalenceKey = "Propagate unsafe contract (discards explicit 'safe')";
            await test.RunAsync();
        }

        [Fact]
        public async Task AccessorContractIsPropagatedIndependently()
        {
            var source = """
                class Base
                {
                    public virtual int Property { get; set; }
                }

                class Derived : Base
                {
                    public override int Property
                    {
                        unsafe {|CS9364:get|} => 0;
                        set { }
                    }
                }
                """;
            var fixedSource = """
                class Base
                {
                    /// <safety>TODO: Audit</safety>
                    public virtual int Property { unsafe get; set; }
                }

                class Derived : Base
                {
                    public override int Property
                    {
                        unsafe get => 0;
                        set { }
                    }
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ExpressionBodiedBaseGetterBecomesUnsafeAccessor()
        {
            var source = """
                class Base
                {
                    public virtual int Property => 0;
                }

                class Derived : Base
                {
                    public override int Property
                    {
                        unsafe {|CS9364:get|} => 1;
                    }
                }
                """;
            var fixedSource = """
                class Base
                {
                    /// <safety>TODO: Audit</safety>
                    public virtual int Property { unsafe get => 0; }
                }

                class Derived : Base
                {
                    public override int Property
                    {
                        unsafe get => 1;
                    }
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task PropagatesAcrossDocuments()
        {
            var test = new CSharpCodeFixVerifier<
                DynamicallyAccessedMembersAnalyzer,
                SynchronizeUnsafeContractCodeFixProvider>.Test();
            test.TestState.Sources.Add((
                "Base.cs",
                """
                class Base
                {
                    public virtual void Method() { }
                }
                """));
            test.TestState.Sources.Add((
                "First.cs",
                """
                class First : Base
                {
                    public override unsafe void {|CS9364:Method|}() { }
                }
                """));
            test.TestState.Sources.Add((
                "Second.cs",
                """
                class Second : Base
                {
                    public override void Method() { }
                }
                """));
            test.FixedState.Sources.Add((
                "Base.cs",
                """
                class Base
                {
                    /// <safety>TODO: Audit</safety>
                    public virtual unsafe void Method() { }
                }
                """));
            test.FixedState.Sources.Add((
                "First.cs",
                """
                class First : Base
                {
                    public override unsafe void Method() { }
                }
                """));
            test.FixedState.Sources.Add((
                "Second.cs",
                """
                class Second : Base
                {
                    public override void Method() { }
                }
                """));
            test.SolutionTransforms.Add(UnsafeMigrationTestHelpers.SetOptions);

            await test.RunAsync();
        }

        [Fact]
        public async Task MixedPartialPropertyFormsUseAccessorModifiers()
        {
            var source = """
                partial class C
                {
                    public partial int Property { unsafe get; }
                    public partial int Property => {|CS0764:0|};
                }
                """;
            var fixedSource = """
                partial class C
                {
                    public partial int Property { unsafe get; }
                    /// <safety>TODO: Audit</safety>
                    public partial int Property { unsafe get => 0; }
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task CompilerEventFixSplitsSharedInterfaceDeclaration()
        {
            var source = """
                #pragma warning disable CS0067

                interface I
                {
                    event System.Action First, Safe;
                }

                class C : I
                {
                    public unsafe event System.Action {|CS9365:First|};
                    public event System.Action Safe;
                }
                """;
            var fixedSource = """
                #pragma warning disable CS0067

                interface I
                {
                    /// <safety>TODO: Audit</safety>
                    unsafe event System.Action First;
                    event System.Action Safe;
                }

                class C : I
                {
                    public unsafe event System.Action First;
                    public event System.Action Safe;
                }
                """;

            await CreateCompilerTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task FixAllAggregatesMultipleEventsFromOneInterfaceDeclaration()
        {
            var source = """
                #pragma warning disable CS0067

                interface I
                {
                    event System.Action First, Second, Safe;
                }

                class C : I
                {
                    public unsafe event System.Action {|CS9365:First|};
                    public unsafe event System.Action {|CS9365:Second|};
                    public event System.Action Safe;
                }
                """;
            var fixedSource = """
                #pragma warning disable CS0067

                interface I
                {
                    /// <safety>TODO: Audit</safety>
                    unsafe event System.Action First;
                    /// <safety>TODO: Audit</safety>
                    unsafe event System.Action Second;
                    event System.Action Safe;
                }

                class C : I
                {
                    public unsafe event System.Action First;
                    public unsafe event System.Action Second;
                    public event System.Action Safe;
                }
                """;
            // Fix all computes both fixes against the original shared declaration, so the merged text keeps a blank
            // line between the two declarations it split apart.
            var batchFixedSource = """
                #pragma warning disable CS0067

                interface I
                {
                    /// <safety>TODO: Audit</safety>
                    unsafe event System.Action First;

                    /// <safety>TODO: Audit</safety>
                    unsafe event System.Action Second;
                    event System.Action Safe;
                }

                class C : I
                {
                    public unsafe event System.Action First;
                    public unsafe event System.Action Second;
                    public event System.Action Safe;
                }
                """;

            var test = CreateCompilerTest(source, fixedSource);
            test.BatchFixedCode = batchFixedSource;
            await test.RunAsync();
        }

        private static CSharpCodeFixVerifier<
            DynamicallyAccessedMembersAnalyzer,
            SynchronizeUnsafeContractCodeFixProvider>.Test CreateCompilerTest(
                string source,
                string fixedSource) =>
            UnsafeMigrationTestHelpers
                .CreateCodeFixTest<
                    DynamicallyAccessedMembersAnalyzer,
                    SynchronizeUnsafeContractCodeFixProvider>(
                    source,
                    fixedSource);

        private static CSharpCodeFixVerifier<
            DynamicallyAccessedMembersAnalyzer,
            SynchronizeUnsafeContractCodeFixProvider>.Test CreateCompilerTest(
                string source) =>
            UnsafeMigrationTestHelpers
                .CreateCodeFixTest<
                    DynamicallyAccessedMembersAnalyzer,
                    SynchronizeUnsafeContractCodeFixProvider>(
                    source);

        private static MetadataReference CreateReference(string source)
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(
                source,
                new CSharpParseOptions(LanguageVersion.Preview));
            CSharpCompilation compilation = CSharpCompilation.Create(
                "ContractReference",
                [syntaxTree],
                SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences(),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: true));
            return compilation.EmitToImageReference();
        }
    }
}
#endif
