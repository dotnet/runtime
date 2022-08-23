// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using LibraryImportGenerator.UnitTests;
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
                + CodeSnippets.CustomStructMarshalling.NonBlittableUserDefinedType()
                + CodeSnippets.CustomStructMarshalling.Stateless.Default };
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithModifier("S[]", "In, Out")
                + CodeSnippets.CustomStructMarshalling.NonBlittableUserDefinedType()
                + CodeSnippets.CustomStructMarshalling.Stateless.Default };

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
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.StructMarshallerEntryPoint };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.ByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.PinByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.StackallocByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.RefParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.ByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.StackallocByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.PinByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.MarshallerPinByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.RefParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomStructMarshalling.Stateful.DefaultModeReturnValue };

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
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValue<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValue<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.ByValueWithStaticPinning<UIntPtr>() };
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
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NativeToManagedFinallyOnlyOutParameter<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NativeToManagedFinallyOnlyReturnValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NestedMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementByValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementNativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementNativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.CustomElementMarshalling };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NativeToManagedFinallyOnlyOutParameter<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NativeToManagedFinallyOnlyReturnValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementByValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementNativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementNativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.DefaultModeReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomElementMarshalling };
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

        public static IEnumerable<object[]> CodeSnippetsToCompileToValidateAllowUnsafeBlocks()
        {
            yield return new object[] { ID(), CodeSnippets.TrivialClassDeclarations, TestTargetFramework.Net, true };

            {
                string source = @"
using System.Runtime.InteropServices;
public class Basic { }
";
                yield return new object[] { ID(), source, TestTargetFramework.Standard, false };
                yield return new object[] { ID(), source, TestTargetFramework.Framework, false };
                yield return new object[] { ID(), source, TestTargetFramework.Net, false };
            }
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompileToValidateAllowUnsafeBlocks))]
        public async Task ValidateRequireAllowUnsafeBlocksDiagnosticNoTrigger(string id, string source, TestTargetFramework framework, bool allowUnsafe)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source, framework, allowUnsafe: allowUnsafe);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.LibraryImportGenerator());
            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
        }
    }
}
