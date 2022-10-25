// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Interop.UnitTests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace LibraryImportGenerator.UnitTests
{
    public class Compiles
    {
        private static string ID(
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string? filePath = null)
            => TestUtils.GetFileLineName(lineNumber, filePath);

        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            yield return new[] { ID(), CodeSnippets.TrivialClassDeclarations };
            yield return new[] { ID(), CodeSnippets.TrivialStructDeclarations };
            yield return new[] { ID(), CodeSnippets.MultipleAttributes };
            yield return new[] { ID(), CodeSnippets.NestedNamespace };
            yield return new[] { ID(), CodeSnippets.NestedTypes };
            yield return new[] { ID(), CodeSnippets.UnsafeContext };
            yield return new[] { ID(), CodeSnippets.UserDefinedEntryPoint };
            yield return new[] { ID(), CodeSnippets.AllLibraryImportNamedArguments };
            yield return new[] { ID(), CodeSnippets.DefaultParameters };
            yield return new[] { ID(), CodeSnippets.UseCSharpFeaturesForConstants };

            // Parameter / return types
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<byte>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<short>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<uint>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<long>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<float>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<double>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<UIntPtr>() };

            // Arrays
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<byte>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<short>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<uint>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<long>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<float>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<double>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("byte*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("sbyte*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("short*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("ushort*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("int*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("uint*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("long*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("ulong*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("float*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("double*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("System.IntPtr*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("System.UIntPtr*", "unsafe") };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<byte>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<sbyte>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<short>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<ushort>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<int>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<uint>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<long>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<ulong>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<IntPtr>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<UIntPtr>(isByRef: false) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<byte>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<sbyte>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<short>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<ushort>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<int>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<uint>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<long>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<ulong>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<IntPtr>(isByRef: true) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<UIntPtr>(isByRef: true) };

            // StringMarshalling
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<char>(StringMarshalling.Utf16) };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<string>(StringMarshalling.Utf16) };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<string>(StringMarshalling.Utf8) };

            // StringMarshallingCustomType
            yield return new[] { ID(), CodeSnippets.CustomStringMarshallingParametersAndModifiers<string>() };

            // MarshalAs
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.Bool) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.VariantBool) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.I1) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.I2) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.U2) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<IntPtr>(UnmanagedType.SysInt) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<UIntPtr>(UnmanagedType.SysUInt) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPWStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPTStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPUTF8Str) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.LPStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.BStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPWStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPUTF8Str) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.BStr) };

            // [In, Out] attributes
            // By value non-blittable array
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithModifier("S[]", "Out")
                + CustomStructMarshallingCodeSnippets<CodeSnippets>.NonBlittableUserDefinedType()
                + CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.Default };
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithModifier("S[]", "In, Out")
                + CustomStructMarshallingCodeSnippets<CodeSnippets>.NonBlittableUserDefinedType()
                + CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.Default };

            // Enums
            yield return new[] { ID(), CodeSnippets.EnumParameters };

            // Pointers
            yield return new[] { ID(), CodeSnippets.PointerParameters<byte>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<sbyte>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<short>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<ushort>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<int>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<uint>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<long>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<ulong>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<float>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<double>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<bool>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.PointerParameters<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersUnsafe("void*") };

            // Delegates
            yield return new[] { ID(), CodeSnippets.DelegateParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.DelegateMarshalAsParametersAndModifiers };

            // Function pointers
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* <void>") };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* unmanaged<void>") };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* unmanaged<int, int>") };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersUnsafe("delegate* unmanaged[Stdcall]<int, int>") };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiersUnsafe("delegate* <int>", UnmanagedType.FunctionPtr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiersUnsafe("delegate* unmanaged<int>", UnmanagedType.FunctionPtr) };

            // Structs
            yield return new[] { ID(), CodeSnippets.BlittableStructParametersAndModifiers(string.Empty) };
            yield return new[] { ID(), CodeSnippets.BlittableStructParametersAndModifiers(CodeSnippets.DisableRuntimeMarshalling) };
            yield return new[] { ID(), CodeSnippets.ValidateDisableRuntimeMarshalling.TypeUsage(string.Empty)
                + CodeSnippets.ValidateDisableRuntimeMarshalling.NonBlittableUserDefinedTypeWithNativeType };
            yield return new[] { ID(), CodeSnippets.ValidateDisableRuntimeMarshalling.TypeUsage(CodeSnippets.DisableRuntimeMarshalling)
                + CodeSnippets.ValidateDisableRuntimeMarshalling.NonBlittableUserDefinedTypeWithNativeType };

            // SafeHandle
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle") };
            yield return new[] { ID(), CodeSnippets.BasicParameterByValue("System.Runtime.InteropServices.SafeHandle") };
            yield return new[] { ID(), CodeSnippets.SafeHandleWithCustomDefaultConstructorAccessibility(privateCtor: false) };
            yield return new[] { ID(), CodeSnippets.SafeHandleWithCustomDefaultConstructorAccessibility(privateCtor: true) };

            // Custom type marshalling
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.StructMarshallerEntryPoint };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.PinByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.StackallocByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.StackallocByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.PinByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.MarshallerPinByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultModeReturnValue };

            // Escaped C# keyword identifiers
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithName("Method", "@event") };
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithName("Method", "@var") };
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithName("@params", "i") };

            //Generics
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<byte>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<short>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<uint>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<long>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<float>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<double>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.MaybeBlittableGenericTypeParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.GenericsStress };
        }

        public static IEnumerable<object[]> CustomCollections()
        {
            // Custom collection marshalling
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValue<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValue<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.ByValueWithStaticPinning<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<byte[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<sbyte[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<short[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<ushort[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<int[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<uint[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<long[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<ulong[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<float[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<double[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<IntPtr[]>() };
            yield return new[] { ID(), CodeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<UIntPtr[]>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedFinallyOnlyOutParameter<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedFinallyOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NestedMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementByValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementNativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementNativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.CustomElementMarshalling };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedFinallyOnlyOutParameter<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedFinallyOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementByValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementNativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementNativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.DefaultModeReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomElementMarshalling };
            yield return new[] { ID(), CodeSnippets.CollectionsOfCollectionsStress };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        [MemberData(nameof(CustomCollections))]
        public async Task ValidateSnippets(string id, string source)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
        }

        public static IEnumerable<object[]> CodeSnippetsToCompileWithPreprocessorSymbols()
        {
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAroundFullFunctionDefinition("Foo"), new string[] { "Foo" } };
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAroundFullFunctionDefinition("Foo"), Array.Empty<string>() };
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAroundFullFunctionDefinitionWithFollowingFunction("Foo"), new string[] { "Foo" } };
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAroundFullFunctionDefinitionWithFollowingFunction("Foo"), Array.Empty<string>() };
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAfterAttributeAroundFunction("Foo"), new string[] { "Foo" } };
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAfterAttributeAroundFunction("Foo"), Array.Empty<string>() };
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAfterAttributeAroundFunctionAdditionalFunctionAfter("Foo"), new string[] { "Foo" } };
            yield return new object[] { ID(), CodeSnippets.PreprocessorIfAfterAttributeAroundFunctionAdditionalFunctionAfter("Foo"), Array.Empty<string>() };
        }
        [Theory]
        [MemberData(nameof(CodeSnippetsToCompileWithPreprocessorSymbols))]
        public async Task ValidateSnippetsWithPreprocessorDefinitions(string id, string source, IEnumerable<string> preprocessorSymbols)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source, preprocessorSymbols: preprocessorSymbols);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
        }

        public static IEnumerable<object[]> CodeSnippetsToValidateFallbackForwarder()
        {
            yield return new object[] { ID(), CodeSnippets.UserDefinedEntryPoint, TestTargetFramework.Net, true };

            // Confirm that all unsupported target frameworks can be generated.
            {
                string code = CodeSnippets.BasicParametersAndModifiers<byte>(CodeSnippets.LibraryImportAttributeDeclaration);
                yield return new object[] { ID(), code, TestTargetFramework.Net5, false };
                yield return new object[] { ID(), code, TestTargetFramework.Core, false };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, false };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, false };
            }

            // Confirm that all unsupported target frameworks fall back to a forwarder.
            {
                string code = CodeSnippets.BasicParametersAndModifiers<byte[]>(CodeSnippets.LibraryImportAttributeDeclaration);
                yield return new object[] { ID(), code, TestTargetFramework.Net5, true };
                yield return new object[] { ID(), code, TestTargetFramework.Core, true };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, true };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, true };
            }

            // Confirm that all unsupported target frameworks fall back to a forwarder.
            {
                string code = CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<string>(StringMarshalling.Utf16, CodeSnippets.LibraryImportAttributeDeclaration);
                yield return new object[] { ID(), code, TestTargetFramework.Net5, true };
                yield return new object[] { ID(), code, TestTargetFramework.Core, true };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, true };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, true };
            }

            // Confirm that if support is missing for any type (like arrays), we fall back to a forwarder even if other types are supported.
            {
                string code = CodeSnippets.BasicReturnAndParameterByValue("System.Runtime.InteropServices.SafeHandle", "int[]", CodeSnippets.LibraryImportAttributeDeclaration);
                yield return new object[] { ID(), code, TestTargetFramework.Net5, true };
                yield return new object[] { ID(), code, TestTargetFramework.Core, true };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, true };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, true };
            }
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToValidateFallbackForwarder))]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task ValidateSnippetsFallbackForwarder(string id, string source, TestTargetFramework targetFramework, bool expectFallbackForwarder)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source, targetFramework);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(
                comp,
                out var generatorDiags,
                new Microsoft.Interop.LibraryImportGenerator());

            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);

            // Verify that the forwarder generates the method as a DllImport.
            SyntaxTree generatedCode = newComp.SyntaxTrees.Last();
            SemanticModel model = newComp.GetSemanticModel(generatedCode);
            var methods = generatedCode.GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .ToList();
            MethodDeclarationSyntax generatedMethod = Assert.Single(methods);

            IMethodSymbol method = model.GetDeclaredSymbol(generatedMethod)!;

            // If we expect fallback forwarder, then the DllImportData will not be null.
            Assert.Equal(expectFallbackForwarder, method.GetDllImportData() is not null);
        }

        public static IEnumerable<object[]> FullyBlittableSnippetsToCompile()
        {
            yield return new[] { ID(), CodeSnippets.UserDefinedEntryPoint };
            yield return new[] { ID(), CodeSnippets.BasicParameterByValue("int") };
        }

        [Theory]
        [MemberData(nameof(FullyBlittableSnippetsToCompile))]
        public async Task ValidateSnippetsWithBlittableAutoForwarding(string id, string source)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(
                comp,
                out var generatorDiags,
                new Microsoft.Interop.LibraryImportGenerator());

            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);

            // Verify that the forwarder generates the method as a DllImport.
            SyntaxTree generatedCode = newComp.SyntaxTrees.Last();
            SemanticModel model = newComp.GetSemanticModel(generatedCode);
            var methods = generatedCode.GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .ToList();

            Assert.All(methods, method => Assert.NotNull(model.GetDeclaredSymbol(method)!.GetDllImportData()));
        }

        public static IEnumerable<object[]> SnippetsWithBlittableTypesButNonBlittableDataToCompile()
        {
            yield return new[] { ID(), CodeSnippets.AllLibraryImportNamedArguments };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.SetLastErrorTrue<int>() };
        }

        [Theory]
        [MemberData(nameof(SnippetsWithBlittableTypesButNonBlittableDataToCompile))]
        public async Task ValidateSnippetsWithBlittableTypesButNonBlittableMetadataDoNotAutoForward(string id, string source)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(
                comp,
                out var generatorDiags,
                new Microsoft.Interop.LibraryImportGenerator());

            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);

            // Verify that the generator generates stubs with inner DllImports for all methods.
            SyntaxTree generatedCode = newComp.SyntaxTrees.Last();
            SemanticModel model = newComp.GetSemanticModel(generatedCode);
            int numStubMethods = generatedCode.GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Count();
            int numInnerDllImports = generatedCode.GetRoot()
                .DescendantNodes().OfType<LocalFunctionStatementSyntax>()
                .Count();

            Assert.Equal(numStubMethods, numInnerDllImports);
        }

        public static IEnumerable<object[]> CodeSnippetsToCompileWithMarshalType()
        {
            yield break;
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped.
                                  // If we have any new experimental APIs that we are implementing that have not been approved,
                                  // we will add new scenarios for this test.
        [Theory(Skip = "No current scenarios to test.")]
#pragma warning restore xUnit1004
        [MemberData(nameof(CodeSnippetsToCompileWithMarshalType))]
        public async Task ValidateSnippetsWithMarshalType(string id, string source)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(
                comp,
                new LibraryImportGeneratorOptionsProvider(useMarshalType: true, generateForwarders: false),
                out var generatorDiags,
                new Microsoft.Interop.LibraryImportGenerator());

            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp, "CS0117");
        }

        public static IEnumerable<object[]> CodeSnippetsToCompileMultipleSources()
        {
            yield return new object[] { ID(), new[] { CodeSnippets.BasicParametersAndModifiers<int>(), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.Bool) } };
            yield return new object[] { ID(), new[] { CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<int>(StringMarshalling.Utf16), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.Bool) } };
            yield return new object[] { ID(), new[] { CodeSnippets.BasicParameterByValue("int[]", CodeSnippets.DisableRuntimeMarshalling), CodeSnippets.BasicParameterWithByRefModifier("ref", "int") } };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompileMultipleSources))]
        public async Task ValidateSnippetsWithMultipleSources(string id, string[] sources)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(sources);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
        }

        public static IEnumerable<object[]> CodeSnippetsToVerifyNoTreesProduced()
        {
            string source = @"
using System.Runtime.InteropServices;
public class Basic { }
";
            yield return new object[] { ID(), source, TestTargetFramework.Standard };
            yield return new object[] { ID(), source, TestTargetFramework.Framework };
            yield return new object[] { ID(), source, TestTargetFramework.Net };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToVerifyNoTreesProduced))]
        public async Task ValidateNoGeneratedOuptutForNoImport(string id, string source, TestTargetFramework framework)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source, framework, allowUnsafe: false);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            Assert.Empty(generatorDiags);

            // Assert we didn't generate any syntax trees, even empty ones
            Assert.Same(comp, newComp);
        }
    }
}
