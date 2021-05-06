using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.UnitTests
{
    public class Compiles
    {
        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            yield return new[] { CodeSnippets.TrivialClassDeclarations };
            yield return new[] { CodeSnippets.TrivialStructDeclarations };
            yield return new[] { CodeSnippets.MultipleAttributes };
            yield return new[] { CodeSnippets.NestedNamespace };
            yield return new[] { CodeSnippets.NestedTypes };
            yield return new[] { CodeSnippets.UserDefinedEntryPoint };
            yield return new[] { CodeSnippets.AllSupportedDllImportNamedArguments };
            yield return new[] { CodeSnippets.DefaultParameters };
            yield return new[] { CodeSnippets.UseCSharpFeaturesForConstants };

            // Parameter / return types
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<byte>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<sbyte>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<short>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<ushort>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<int>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<uint>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<long>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<ulong>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<float>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<double>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<bool>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<IntPtr>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<UIntPtr>() };

            // Arrays
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<byte>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<sbyte>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<short>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<ushort>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<int>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<uint>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<long>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<ulong>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<float>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<double>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<bool>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<IntPtr>() };
            yield return new[] { CodeSnippets.ArrayParametersAndModifiers<UIntPtr>() };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<byte>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<sbyte>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<short>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<ushort>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<int>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<uint>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<long>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<ulong>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<IntPtr>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<UIntPtr>(isByRef: false) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<byte>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<sbyte>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<short>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<ushort>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<int>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<uint>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<long>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<ulong>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<IntPtr>(isByRef: true) };
            yield return new[] { CodeSnippets.ArrayParameterWithSizeParam<UIntPtr>(isByRef: true) };

            // CharSet
            yield return new[] { CodeSnippets.BasicParametersAndModifiersWithCharSet<char>(CharSet.Unicode) };
            yield return new[] { CodeSnippets.BasicParametersAndModifiersWithCharSet<string>(CharSet.Unicode) };
            yield return new[] { CodeSnippets.BasicParametersAndModifiersWithCharSet<string>(CharSet.Ansi) };
            yield return new[] { CodeSnippets.BasicParametersAndModifiersWithCharSet<string>(CharSet.Auto) };

            // MarshalAs
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.Bool) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.VariantBool) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.I1) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.I2) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.U2) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<IntPtr>(UnmanagedType.SysInt) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<UIntPtr>(UnmanagedType.SysUInt) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPWStr) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPTStr) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPUTF8Str) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPStr) };
            yield return new[] { CodeSnippets.ArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPWStr) };
            yield return new[] { CodeSnippets.ArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPUTF8Str) };
            yield return new[] { CodeSnippets.ArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPStr) };

            // [In, Out] attributes
            // By value non-blittable array
            yield return new[] { CodeSnippets.ByValueParameterWithModifier<bool[]>("Out") };
            yield return new[] { CodeSnippets.ByValueParameterWithModifier<bool[]>("In, Out") };

            // Enums
            yield return new[] { CodeSnippets.EnumParameters };

            // Pointers
            yield return new[] { CodeSnippets.PointerParameters<byte>() };
            yield return new[] { CodeSnippets.PointerParameters<sbyte>() };
            yield return new[] { CodeSnippets.PointerParameters<short>() };
            yield return new[] { CodeSnippets.PointerParameters<ushort>() };
            yield return new[] { CodeSnippets.PointerParameters<int>() };
            yield return new[] { CodeSnippets.PointerParameters<uint>() };
            yield return new[] { CodeSnippets.PointerParameters<long>() };
            yield return new[] { CodeSnippets.PointerParameters<ulong>() };
            yield return new[] { CodeSnippets.PointerParameters<float>() };
            yield return new[] { CodeSnippets.PointerParameters<double>() };
            yield return new[] { CodeSnippets.PointerParameters<bool>() };
            yield return new[] { CodeSnippets.PointerParameters<IntPtr>() };
            yield return new[] { CodeSnippets.PointerParameters<UIntPtr>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiersUnsafe("void*") };

            // Delegates
            yield return new[] { CodeSnippets.DelegateParametersAndModifiers };
            yield return new[] { CodeSnippets.DelegateMarshalAsParametersAndModifiers };

            // Function pointers
            yield return new[] { CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* <void>") };
            yield return new[] { CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* unmanaged<void>") };
            yield return new[] { CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* unmanaged<int, int>") };
            yield return new[] { CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* unmanaged[Stdcall]<int, int>") };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiersUnsafe("delegate* <int>", UnmanagedType.FunctionPtr) };
            yield return new[] { CodeSnippets.MarshalAsParametersAndModifiersUnsafe("delegate* unmanaged<int>", UnmanagedType.FunctionPtr) };

            // Structs
            yield return new[] { CodeSnippets.BlittableStructParametersAndModifiers };
            yield return new[] { CodeSnippets.GenericBlittableStructParametersAndModifiers };

            // SafeHandle
            yield return new[] { CodeSnippets.BasicParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle") };
            yield return new[] { CodeSnippets.BasicParameterByValue("System.Runtime.InteropServices.SafeHandle") };
            yield return new[] { CodeSnippets.SafeHandleWithCustomDefaultConstructorAccessibility(privateCtor: false) };
            yield return new[] { CodeSnippets.SafeHandleWithCustomDefaultConstructorAccessibility(privateCtor: true) };

            // PreserveSig
            yield return new[] { CodeSnippets.PreserveSigFalseVoidReturn };
            yield return new[] { CodeSnippets.PreserveSigFalse<byte>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<sbyte>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<short>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<ushort>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<int>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<uint>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<long>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<ulong>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<float>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<double>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<bool>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<IntPtr>() };
            yield return new[] { CodeSnippets.PreserveSigFalse<UIntPtr>() };
            yield return new[] { CodeSnippets.PreserveSigFalse("Microsoft.Win32.SafeHandles.SafeFileHandle") };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<byte>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<sbyte>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<short>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<ushort>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<int>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<uint>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<long>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<ulong>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<float>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<double>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<bool>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<IntPtr>() };
            yield return new[] { CodeSnippets.ArrayPreserveSigFalse<UIntPtr>() };

            // Custom type marshalling
            yield return new[] { CodeSnippets.CustomStructMarshallingParametersAndModifiers };
            yield return new[] { CodeSnippets.CustomStructMarshallingStackallocParametersAndModifiersNoRef };
            yield return new[] { CodeSnippets.CustomStructMarshallingStackallocValuePropertyParametersAndModifiersNoRef };
            yield return new[] { CodeSnippets.CustomStructMarshallingOptionalStackallocParametersAndModifiers };
            yield return new[] { CodeSnippets.CustomStructMarshallingValuePropertyParametersAndModifiers };
            yield return new[] { CodeSnippets.CustomStructMarshallingPinnableParametersAndModifiers };
            yield return new[] { CodeSnippets.CustomStructMarshallingNativeTypePinnable };
            yield return new[] { CodeSnippets.CustomStructMarshallingMarshalUsingParametersAndModifiers };
            yield return new[] { CodeSnippets.ArrayMarshallingWithCustomStructElement };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        public async Task ValidateSnippets(string source)
        {
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(generatorDiags);

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        public static IEnumerable<object[]> CodeSnippetsToCompileWithPreprocessorSymbols()
        {
            yield return new object[] { CodeSnippets.PreprocessorIfAroundFullFunctionDefinition("Foo"), new string[] { "Foo" } };
            yield return new object[] { CodeSnippets.PreprocessorIfAroundFullFunctionDefinition("Foo"), Array.Empty<string>() };
            yield return new object[] { CodeSnippets.PreprocessorIfAroundFullFunctionDefinitionWithFollowingFunction("Foo"), new string[] { "Foo" } };
            yield return new object[] { CodeSnippets.PreprocessorIfAroundFullFunctionDefinitionWithFollowingFunction("Foo"), Array.Empty<string>() };
            yield return new object[] { CodeSnippets.PreprocessorIfAfterAttributeAroundFunction("Foo"), new string[] { "Foo" } };
            yield return new object[] { CodeSnippets.PreprocessorIfAfterAttributeAroundFunction("Foo"), Array.Empty<string>() };
            yield return new object[] { CodeSnippets.PreprocessorIfAfterAttributeAroundFunctionAdditionalFunctionAfter("Foo"), new string[] { "Foo" } };
            yield return new object[] { CodeSnippets.PreprocessorIfAfterAttributeAroundFunctionAdditionalFunctionAfter("Foo"), Array.Empty<string>() };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompileWithPreprocessorSymbols))]
        public async Task ValidateSnippetsWithPreprocessorDefintions(string source, IEnumerable<string> preprocessorSymbols)
        {
            Compilation comp = await TestUtils.CreateCompilation(source, preprocessorSymbols: preprocessorSymbols);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(generatorDiags);

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        public static IEnumerable<object[]> CodeSnippetsToCompileWithForwarder()
        {
            yield return new[] { CodeSnippets.UserDefinedEntryPoint };
            yield return new[] { CodeSnippets.AllSupportedDllImportNamedArguments };

            // Parameter / return types (supported in DllImportGenerator)
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<byte>() };
            // Parameter / return types (not supported in DllImportGenerator)
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<string>() };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompileWithForwarder))]
        public async Task ValidateSnippetsWithForwarder(string source)
        {
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(
                comp,
                new DllImportGeneratorOptionsProvider(useMarshalType: false, generateForwarders: true),
                out var generatorDiags,
                new Microsoft.Interop.DllImportGenerator());

            Assert.Empty(generatorDiags);

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        public static IEnumerable<object[]> CodeSnippetsToCompileWithMarshalType()
        {
            yield break;
        }

        [Theory(Skip = "No current scenarios to test.")]
        [MemberData(nameof(CodeSnippetsToCompileWithMarshalType))]
        public async Task ValidateSnippetsWithMarshalType(string source)
        {
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(
                comp,
                new DllImportGeneratorOptionsProvider(useMarshalType: true, generateForwarders: false),
                out var generatorDiags,
                new Microsoft.Interop.DllImportGenerator());

            Assert.Empty(generatorDiags);

            var newCompDiags = newComp.GetDiagnostics();

            Assert.All(newCompDiags, diag =>
            {
                Assert.Equal("CS0117", diag.Id);
                Assert.StartsWith("'Marshal' does not contain a definition for ", diag.GetMessage());
            });
        }
    }
}
