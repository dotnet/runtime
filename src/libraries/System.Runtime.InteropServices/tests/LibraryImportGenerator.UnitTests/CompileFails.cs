// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;

using StringMarshalling = System.Runtime.InteropServices.StringMarshalling;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

namespace LibraryImportGenerator.UnitTests
{
    public class CompileFails
    {
        private static string ID(
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string? filePath = null)
            => TestUtils.GetFileLineName(lineNumber, filePath);

        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            // Not LibraryImportAttribute
            yield return new object[] { ID(), CodeSnippets.UserDefinedPrefixedAttributes, 0, 3 };

            // No explicit marshalling for char or string
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<char>(), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<string>(), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<char>(), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<string>(), 5, 0 };

            // No explicit marshaling for bool
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<bool>(), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<bool>(), 5, 0 };

            // Unsupported StringMarshalling configuration
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<char>(StringMarshalling.Utf8), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<char>(StringMarshalling.Custom), 6, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<string>(StringMarshalling.Custom), 6, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStringMarshallingParametersAndModifiers<char>(), 5, 0 };

            // Unsupported UnmanagedType
            yield return new object[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.I1), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.U1), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<int[]>(UnmanagedType.SafeArray), 10, 0 };

            // Unsupported MarshalAsAttribute usage
            //  * UnmanagedType.CustomMarshaler, MarshalTypeRef, MarshalType, MarshalCookie
            yield return new object[] { ID(), CodeSnippets.MarshalAsCustomMarshalerOnTypes, 16, 0 };

            // Unsupported [In, Out] attributes usage
            // Blittable array
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<int[]>("Out"), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<int[]>("In, Out"), 1, 0 };

            // By ref with [In, Out] attributes
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("in int", "In"), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int", "In"), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int", "In, Out"), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("out int", "Out"), 1, 0 };

            // By value non-array with [In, Out] attributes
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<byte>("In"), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<byte>("Out"), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<byte>("In, Out"), 1, 0 };

            // LCIDConversion
            yield return new object[] { ID(), CodeSnippets.LCIDConversionAttribute, 1, 0 };

            // No size information for array marshalling from unmanaged to managed
            //   * return, out, ref
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<byte[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<sbyte[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<short[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<ushort[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<char[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<string[]>(CodeSnippets.DisableRuntimeMarshalling), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<int[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<uint[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<long[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<ulong[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<float[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<double[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<bool[]>(CodeSnippets.DisableRuntimeMarshalling), 5, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<IntPtr[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<UIntPtr[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };

            // Collection with non-integer size param
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<float>(isByRef: false), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<double>(isByRef: false), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<bool>(isByRef: false), 2, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalUsingArrayParameterWithSizeParam<float>(isByRef: false), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalUsingArrayParameterWithSizeParam<double>(isByRef: false), 1, 0 };
            yield return new object[] { ID(), CodeSnippets.MarshalUsingArrayParameterWithSizeParam<bool>(isByRef: false), 2, 0 };

            // Custom type marshalling with invalid members
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippets = new(new CodeSnippets());
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.NonStaticMarshallerEntryPoint, 2, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.ManagedToNativeOnlyOutParameter, 1, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.ManagedToNativeOnlyReturnValue, 1, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyInParameter, 1, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.StackallocOnlyRefParameter, 1, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.ManagedToNativeOnlyOutParameter, 1, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.ManagedToNativeOnlyReturnValue, 1, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyInParameter, 1, 0 };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.StackallocOnlyRefParameter, 1, 0 };

            // Abstract SafeHandle type by reference
            yield return new object[] { ID(), CodeSnippets.BasicParameterWithByRefModifier("ref", "System.Runtime.InteropServices.SafeHandle"), 1, 0 };

            // Collection with constant and element size parameter
            yield return new object[] { ID(), CodeSnippets.MarshalUsingCollectionWithConstantAndElementCount, 2, 0 };

            // Collection with null element size parameter name
            yield return new object[] { ID(), CodeSnippets.MarshalUsingCollectionWithNullElementName, 2, 0 };

            // Generic collection marshaller has different arity than collection.
            CustomCollectionMarshallingCodeSnippets customCollectionMarshallingCodeSnippets = new(new CodeSnippets());
            yield return new object[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.GenericCollectionMarshallingArityMismatch, 2, 0 };

            yield return new object[] { ID(), CodeSnippets.MarshalAsAndMarshalUsingOnReturnValue, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomElementMarshallingDuplicateElementIndirectionDepth, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomElementMarshallingUnusedElementIndirectionDepth, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.RecursiveCountElementNameOnReturnValue, 2, 0 };
            yield return new object[] { ID(), CodeSnippets.RecursiveCountElementNameOnParameter, 2, 0 };
            yield return new object[] { ID(), CodeSnippets.MutuallyRecursiveCountElementNameOnParameter, 4, 0 };
            yield return new object[] { ID(), CodeSnippets.MutuallyRecursiveSizeParamIndexOnParameter, 4, 0 };

            // Ref returns
            yield return new object[] { ID(), CodeSnippets.RefReturn("int"), 2, 2 };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        public async Task ValidateSnippets(string id, string source, int expectedGeneratorErrors, int expectedCompilerErrors)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());

            // Verify the compilation failed with errors.
            IEnumerable<Diagnostic> generatorErrors = generatorDiags.Where(d => d.Severity == DiagnosticSeverity.Error);
            int generatorErrorCount = generatorErrors.Count();
            Assert.True(
                expectedGeneratorErrors == generatorErrorCount,
                $"Expected {expectedGeneratorErrors} errors, but encountered {generatorErrorCount}. Errors: {string.Join(Environment.NewLine, generatorErrors.Select(d => d.ToString()))}");

            IEnumerable<Diagnostic> compilerErrors = newComp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
            int compilerErrorCount = compilerErrors.Count();
            Assert.True(
                expectedCompilerErrors == compilerErrorCount,
                $"Expected {expectedCompilerErrors} errors, but encountered {compilerErrorCount}. Errors: {string.Join(Environment.NewLine, compilerErrors.Select(d => d.ToString()))}");
        }

        public static IEnumerable<object[]> CodeSnippetsToCompile_InvalidCode()
        {
            yield return new[] { ID(), CodeSnippets.RecursiveImplicitlyBlittableStruct };
            yield return new[] { ID(), CodeSnippets.MutuallyRecursiveImplicitlyBlittableStruct };
            yield return new[] { ID(), CodeSnippets.PartialPropertyName };
            yield return new[] { ID(), CodeSnippets.InvalidConstantForModuleName };
            yield return new[] { ID(), CodeSnippets.IncorrectAttributeFieldType };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile_InvalidCode))]
        public async Task ValidateSnippets_InvalidCodeGracefulFailure(string id, string source)
        {
            TestUtils.Use(id);
            // Each snippet will contain the expected diagnostic codes in their expected locations for the compile errors.
            // We expect there to be no generator diagnostics or failures.
            await VerifyCS.VerifySourceGeneratorAsync(source);
        }

        [Fact]
        public async Task ValidateDisableRuntimeMarshallingForBlittabilityCheckFromAssemblyReference()
        {
            // Emit the referenced assembly to a stream so we reference it through a metadata reference.
            // Our check for strict blittability doesn't work correctly when using source compilation references.
            // (There are sometimes false-positives.)
            // This causes any diagnostics that depend on strict blittability being correctly calculated to
            // not show up in the IDE experience. However, since they correctly show up when doing builds,
            // either by running the Build command in the IDE or a command line build, we aren't allowing invalid code.
            // This test validates the Build-like experience. In the future, we should update this test to validate the
            // IDE-like experience once we fix that case
            // (If the IDE experience works, then the command-line experience will also work.)
            string assemblySource = $$"""
                using System.Runtime.InteropServices.Marshalling;
                {{CodeSnippets.ValidateDisableRuntimeMarshalling.NonBlittableUserDefinedTypeWithNativeType}}
                """;
            Compilation assemblyComp = await TestUtils.CreateCompilation(assemblySource);
            Assert.Empty(assemblyComp.GetDiagnostics());

            var ms = new MemoryStream();
            Assert.True(assemblyComp.Emit(ms).Success);

            string testSource = CodeSnippets.ValidateDisableRuntimeMarshalling.TypeUsage(string.Empty);

            VerifyCS.Test test = new(referenceAncillaryInterop: false)
            {
                TestCode = testSource,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(ms.ToArray()));

            // The errors should indicate the DisableRuntimeMarshalling is required.
            test.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "Method"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(1)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "p"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(2)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pIn"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(3)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pRef"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(4)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pOut"));

            await test.RunAsync();
        }

        [Fact]
        public async Task ValidateRequireAllowUnsafeBlocksDiagnostic()
        {
            var test = new AllowUnsafeBlocksTest()
            {
                TestCode = CodeSnippets.TrivialClassDeclarations,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic("SYSLIB1062"));
            test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0227").WithLocation(0));

            await test.RunAsync();
        }

        class AllowUnsafeBlocksTest : VerifyCS.Test
        {
            public AllowUnsafeBlocksTest()
                    :base(referenceAncillaryInterop: false)
            {
            }

            protected override CompilationOptions CreateCompilationOptions() => ((CSharpCompilationOptions)base.CreateCompilationOptions()).WithAllowUnsafe(false);
        }
    }
}
