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
using Xunit;

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
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.NonStaticMarshallerEntryPoint, 2, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.ManagedToNativeOnlyOutParameter, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.ManagedToNativeOnlyReturnValue, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.NativeToManagedOnlyInParameter, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.StackallocOnlyRefParameter, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.ManagedToNativeOnlyOutParameter, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.ManagedToNativeOnlyReturnValue, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.NativeToManagedOnlyInParameter, 1, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.StackallocOnlyRefParameter, 1, 0 };

            // Abstract SafeHandle type by reference
            yield return new object[] { ID(), CodeSnippets.BasicParameterWithByRefModifier("ref", "System.Runtime.InteropServices.SafeHandle"), 1, 0 };

            // Collection with constant and element size parameter
            yield return new object[] { ID(), CodeSnippets.MarshalUsingCollectionWithConstantAndElementCount, 2, 0 };

            // Collection with null element size parameter name
            yield return new object[] { ID(), CodeSnippets.MarshalUsingCollectionWithNullElementName, 2, 0 };

            // Generic collection marshaller has different arity than collection.
            yield return new object[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.GenericCollectionMarshallingArityMismatch, 2, 0 };

            yield return new object[] { ID(), CodeSnippets.MarshalAsAndMarshalUsingOnReturnValue, 2, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomElementMarshallingDuplicateElementIndirectionDepth, 2, 0 };
            yield return new object[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomElementMarshallingUnusedElementIndirectionDepth, 1, 0 };
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
            yield return new object[] { ID(), CodeSnippets.RecursiveImplicitlyBlittableStruct, 0, 1 };
            yield return new object[] { ID(), CodeSnippets.MutuallyRecursiveImplicitlyBlittableStruct, 0, 2 };
            yield return new object[] { ID(), CodeSnippets.PartialPropertyName, 0, 2 };
            yield return new object[] { ID(), CodeSnippets.InvalidConstantForModuleName, 0, 1 };
            yield return new object[] { ID(), CodeSnippets.IncorrectAttributeFieldType, 0, 1 };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile_InvalidCode))]
        public async Task ValidateSnippets_InvalidCodeGracefulFailure(string id, string source, int expectedGeneratorErrors, int expectedCompilerErrors)
        {
            TestUtils.Use(id);
            // Do not validate that the compilation has no errors that the generator will not fix.
            Compilation comp = await TestUtils.CreateCompilation(source);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());

            // Verify the compilation failed with errors.
            int generatorErrors = generatorDiags.Count(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(expectedGeneratorErrors, generatorErrors);

            int compilerErrors = newComp.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(expectedCompilerErrors, compilerErrors);
        }

        [Fact]
        public async Task ValidateDisableRuntimeMarshallingForBlittabilityCheckFromAssemblyReference()
        {
            string assemblySource = $@"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{CodeSnippets.ValidateDisableRuntimeMarshalling.NonBlittableUserDefinedTypeWithNativeType}
";
            Compilation assemblyComp = await TestUtils.CreateCompilation(assemblySource);
            TestUtils.AssertPreSourceGeneratorCompilation(assemblyComp);

            var ms = new MemoryStream();
            Assert.True(assemblyComp.Emit(ms).Success);

            string testSource = CodeSnippets.ValidateDisableRuntimeMarshalling.TypeUsage(string.Empty);

            Compilation testComp = await TestUtils.CreateCompilation(testSource, refs: new[] { MetadataReference.CreateFromImage(ms.ToArray()) });
            TestUtils.AssertPreSourceGeneratorCompilation(testComp);

            var newComp = TestUtils.RunGenerators(testComp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());

            // The errors should indicate the DisableRuntimeMarshalling is required.
            Assert.True(generatorDiags.All(d => d.Id == "SYSLIB1051"));

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
        }

        [Fact]
        public async Task ValidateRequireAllowUnsafeBlocksDiagnostic()
        {
            string source = CodeSnippets.TrivialClassDeclarations;
            Compilation comp = await TestUtils.CreateCompilation(new[] { source }, allowUnsafe: false);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());

            // The errors should indicate the AllowUnsafeBlocks is required.
            Assert.True(generatorDiags.All(d => d.Id == "SYSLIB1062"));

            // There should only be one SYSLIB1062, even if there are multiple LibraryImportAttribute uses.
            Assert.Equal(1, generatorDiags.Count());
        }
    }
}
