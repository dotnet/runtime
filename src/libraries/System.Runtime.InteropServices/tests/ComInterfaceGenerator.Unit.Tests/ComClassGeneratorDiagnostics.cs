// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComClassGenerator>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class ComClassGeneratorDiagnostics
    {
        [Fact]
        public async Task NonPartialClassWarns()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                partial interface INativeAPI
                {
                }

                [GeneratedComClass]
                internal class {|#0:C|} : INativeAPI {}

                """;

            await VerifyCS.VerifySourceGeneratorAsync(
                source,
                new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedClassMissingPartialModifier)
                    .WithLocation(0)
                    .WithArguments("C"));
        }

        [Fact]
        public async Task NonPartialContainingTypeWarns()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                public class Test
                {
                    [GeneratedComInterface]
                    partial interface INativeAPI
                    {
                    }

                    [GeneratedComClass]
                    internal partial class {|#0:C|} : INativeAPI {}
                }

                """;

            await VerifyCS.VerifySourceGeneratorAsync(
                source,
                new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedClassMissingPartialModifier)
                    .WithLocation(0)
                    .WithArguments("Test.C"));
        }

        internal class UnsafeBlocksNotAllowedTest : VerifyCS.Test
        {
            internal UnsafeBlocksNotAllowedTest(bool referenceAncillaryInterop) : base(referenceAncillaryInterop) { }
            protected override CompilationOptions CreateCompilationOptions()
                => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: false);
        }

        [Fact]
        public async Task UnsafeCodeNotEnabledWarns()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                public partial class Test
                {
                    [GeneratedComInterface]
                    internal partial interface INativeAPI
                    {
                    }

                    [GeneratedComClass]
                    internal partial class {|#0:C|} : INativeAPI {}
                }

                """;

            var test = new UnsafeBlocksNotAllowedTest(false);
            test.TestState.Sources.Add(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(GeneratorDiagnostics.RequiresAllowUnsafeBlocks)
                    .WithLocation(0)
                    .WithArguments("Test.C"));

            await test.RunAsync();
        }

        [Fact]
        public async Task ClassThatDoesNotInheritFromGeneratedInterfaceWarns()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                public partial class Test{
                    internal interface INativeAPI
                    {
                    }

                    [GeneratedComClass]
                    internal partial class {|#0:C|} : INativeAPI {}
                }

                """;

            await VerifyCS.VerifySourceGeneratorAsync(
                source,
                new DiagnosticResult(GeneratorDiagnostics.ClassDoesNotImplementAnyGeneratedComInterface)
                    .WithLocation(0)
                    .WithArguments("Test.C"));
        }
    }
}
