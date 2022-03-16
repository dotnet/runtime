// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Xunit;

namespace LibraryImportGenerator.UnitTests
{
    public class CompileFails
    {
        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            // Not LibraryImportAttribute
            yield return new object[] { CodeSnippets.UserDefinedPrefixedAttributes, 0, 3 };

            // No explicit marshalling for char or string
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<char>(), 5, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<string>(), 5, 0 };
            yield return new object[] { CodeSnippets.MarshalAsArrayParametersAndModifiers<char>(), 5, 0 };
            yield return new object[] { CodeSnippets.MarshalAsArrayParametersAndModifiers<string>(), 5, 0 };

            // No explicit marshaling for bool
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<bool>(), 5, 0 };
            yield return new object[] { CodeSnippets.MarshalAsArrayParametersAndModifiers<bool>(), 5, 0 };

            // Unsupported StringMarshalling configuration
            yield return new object[] { CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<char>(StringMarshalling.Utf8), 6, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<char>(StringMarshalling.Custom), 7, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<string>(StringMarshalling.Custom), 7, 0 };

            // Unsupported UnmanagedType
            yield return new object[] { CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.I1), 5, 0 };
            yield return new object[] { CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.U1), 5, 0 };
            yield return new object[] { CodeSnippets.MarshalAsParametersAndModifiers<int[]>(UnmanagedType.SafeArray), 10, 0 };

            // Unsupported MarshalAsAttribute usage
            //  * UnmanagedType.CustomMarshaler, MarshalTypeRef, MarshalType, MarshalCookie
            yield return new object[] { CodeSnippets.MarshalAsCustomMarshalerOnTypes, 16, 0 };

            // Unsupported [In, Out] attributes usage
            // Blittable array
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier<int[]>("Out"), 1, 0 };
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier<int[]>("In, Out"), 1, 0 };

            // By ref with [In, Out] attributes
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier("in int", "In"), 1, 0 };
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier("ref int", "In"), 1, 0 };
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier("ref int", "In, Out"), 1, 0 };
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier("out int", "Out"), 1, 0 };

            // By value non-array with [In, Out] attributes
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier<byte>("In"), 1, 0 };
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier<byte>("Out"), 1, 0 };
            yield return new object[] { CodeSnippets.ByValueParameterWithModifier<byte>("In, Out"), 1, 0 };

            // LCIDConversion
            yield return new object[] { CodeSnippets.LCIDConversionAttribute, 1, 0 };

            // No size information for array marshalling from unmanaged to managed
            //   * return, out, ref
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<byte[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<sbyte[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<short[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<ushort[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<char[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<string[]>(CodeSnippets.DisableRuntimeMarshalling), 5, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<int[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<uint[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<long[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<ulong[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<float[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<double[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<bool[]>(CodeSnippets.DisableRuntimeMarshalling), 5, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<IntPtr[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiers<UIntPtr[]>(CodeSnippets.DisableRuntimeMarshalling), 3, 0 };

            // Collection with non-integer size param
            yield return new object[] { CodeSnippets.MarshalAsArrayParameterWithSizeParam<float>(isByRef: false), 1, 0 };
            yield return new object[] { CodeSnippets.MarshalAsArrayParameterWithSizeParam<double>(isByRef: false), 1, 0 };
            yield return new object[] { CodeSnippets.MarshalAsArrayParameterWithSizeParam<bool>(isByRef: false), 2, 0 };
            yield return new object[] { CodeSnippets.MarshalUsingArrayParameterWithSizeParam<float>(isByRef: false), 1, 0 };
            yield return new object[] { CodeSnippets.MarshalUsingArrayParameterWithSizeParam<double>(isByRef: false), 1, 0 };
            yield return new object[] { CodeSnippets.MarshalUsingArrayParameterWithSizeParam<bool>(isByRef: false), 2, 0 };


            // Custom type marshalling with invalid members
            yield return new object[] { CodeSnippets.CustomStructMarshallingByRefValueProperty, 3, 0 };
            yield return new object[] { CodeSnippets.CustomStructMarshallingManagedToNativeOnlyOutParameter, 1, 0 };
            yield return new object[] { CodeSnippets.CustomStructMarshallingManagedToNativeOnlyReturnValue, 1, 0 };
            yield return new object[] { CodeSnippets.CustomStructMarshallingNativeToManagedOnlyInParameter, 1, 0 };
            yield return new object[] { CodeSnippets.CustomStructMarshallingStackallocOnlyRefParameter, 1, 0 };

            // Abstract SafeHandle type by reference
            yield return new object[] { CodeSnippets.BasicParameterWithByRefModifier("ref", "System.Runtime.InteropServices.SafeHandle"), 1, 0 };

            // Collection with constant and element size parameter
            yield return new object[] { CodeSnippets.MarshalUsingCollectionWithConstantAndElementCount, 2, 0 };

            // Collection with null element size parameter name
            yield return new object[] { CodeSnippets.MarshalUsingCollectionWithNullElementName, 2, 0 };

            // Generic collection marshaller has different arity than collection.
            yield return new object[] { CodeSnippets.GenericCollectionMarshallingArityMismatch, 2, 0 };

            yield return new object[] { CodeSnippets.MarshalAsAndMarshalUsingOnReturnValue, 2, 0 };
            yield return new object[] { CodeSnippets.GenericCollectionWithCustomElementMarshallingDuplicateElementIndirectionLevel, 2, 0 };
            yield return new object[] { CodeSnippets.GenericCollectionWithCustomElementMarshallingUnusedElementIndirectionLevel, 1, 0 };
            yield return new object[] { CodeSnippets.RecursiveCountElementNameOnReturnValue, 2, 0 };
            yield return new object[] { CodeSnippets.RecursiveCountElementNameOnParameter, 2, 0 };
            yield return new object[] { CodeSnippets.MutuallyRecursiveCountElementNameOnParameter, 4, 0 };
            yield return new object[] { CodeSnippets.MutuallyRecursiveSizeParamIndexOnParameter, 4, 0 };

            // Ref returns
            yield return new object[] { CodeSnippets.RefReturn("int"), 2, 2 };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        public async Task ValidateSnippets(string source, int expectedGeneratorErrors, int expectedCompilerErrors)
        {
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
            yield return new object[] { CodeSnippets.RecursiveImplicitlyBlittableStruct, 0, 1 };
            yield return new object[] { CodeSnippets.MutuallyRecursiveImplicitlyBlittableStruct, 0, 2 };
            yield return new object[] { CodeSnippets.PartialPropertyName, 1, 2 };
            yield return new object[] { CodeSnippets.InvalidConstantForModuleName, 1, 1 };
            yield return new object[] { CodeSnippets.IncorrectAttributeFieldType, 1, 1 };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile_InvalidCode))]
        public async Task ValidateSnippets_InvalidCodeGracefulFailure(string source, int expectedGeneratorErrors, int expectedCompilerErrors)
        {
            // Do not validate that the compilation has no errors that the generator will not fix.
            Compilation comp = await TestUtils.CreateCompilation(source);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());

            // Verify the compilation failed with errors.
            int generatorErrors = generatorDiags.Count(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(expectedGeneratorErrors, generatorErrors);

            int compilerErrors = newComp.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(expectedCompilerErrors, compilerErrors);
        }
    }
}
