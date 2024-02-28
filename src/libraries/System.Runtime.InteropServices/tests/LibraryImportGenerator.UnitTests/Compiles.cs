// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.XUnitExtensions.Attributes;
using Microsoft.Interop.UnitTests;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

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
            yield return new[] { ID(), CodeSnippets.LibraryImportInRefStruct };

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

            // Parameter / return types for specially considered "strictly blittable" types.
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<CLong>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<CULong>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<NFloat>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<Guid>() };

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
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<object>(UnmanagedType.Interface) };
            // TODO: Do we want to limit support of UnmanagedType.Interface to a subset of types?
            // TODO: Should we block delegate types as they use to have special COM interface marshalling that we have since
            // blocked? Blocking it would help .NET Framework->.NET migration as there wouldn't be a silent behavior change.
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<string>(UnmanagedType.Interface) };
            yield return new[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<Action>(UnmanagedType.Interface) };

            // MarshalAs with array element UnmanagedType
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPWStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPUTF8Str) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.LPStr) };
            yield return new[] { ID(), CodeSnippets.MarshalAsArrayParameterWithNestedMarshalInfo<string>(UnmanagedType.BStr) };


            // [In, Out] attributes
            // By value non-blittable array
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithModifier("S[]", "Out")
                + CustomStructMarshallingCodeSnippets.NonBlittableUserDefinedType()
                + CustomStructMarshallingCodeSnippets.StatelessSnippets.Default };
            yield return new[] { ID(), CodeSnippets.ByValueParameterWithModifier("S[]", "In, Out")
                + CustomStructMarshallingCodeSnippets.NonBlittableUserDefinedType()
                + CustomStructMarshallingCodeSnippets.StatelessSnippets.Default };

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

            // Custom type marshalling
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippets = new(new CodeSnippets());
            yield return new[] { ID(), customStructMarshallingCodeSnippets.StructMarshallerEntryPoint };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.ByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.PinByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.StackallocByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.ByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.StackallocByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.PinByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.MarshallerPinByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.DefaultModeReturnValue };

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

            // Type-level interop generator trigger attributes
            yield return new[] { ID(), CodeSnippets.GeneratedComInterface };

            // Parameter modifiers
            yield return new[] { ID(), CodeSnippets.SingleParameterWithModifier("int", "scoped ref") };
            yield return new[] { ID(), CodeSnippets.SingleParameterWithModifier("int", "ref readonly") };
        }

        public static IEnumerable<object[]> CustomCollections()
        {
            // Custom collection marshalling
            CustomCollectionMarshallingCodeSnippets customCollectionMarshallingCodeSnippets = new(new CodeSnippets());
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValue<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValue<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.ByValueWithStaticPinning<UIntPtr>() };
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
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NativeToManagedFinallyOnlyOutParameter<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NativeToManagedFinallyOnlyReturnValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NestedMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NonBlittableElementByValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NonBlittableElementNativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.NonBlittableElementNativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.CustomElementMarshalling };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NativeToManagedFinallyOnlyOutParameter<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NativeToManagedFinallyOnlyReturnValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NonBlittableElementByValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NonBlittableElementNativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.NonBlittableElementNativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.DefaultModeReturnValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippets.Stateful.CustomElementMarshalling };
            yield return new[] { ID(), CodeSnippets.CollectionsOfCollectionsStress };
        }

        [ParallelTheory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        [MemberData(nameof(CustomCollections))]
        public async Task ValidateSnippets(string id, string source)
        {
            TestUtils.Use(id);

            await VerifyCS.VerifySourceGeneratorAsync(source);
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
        [ParallelTheory]
        [MemberData(nameof(CodeSnippetsToCompileWithPreprocessorSymbols))]
        public async Task ValidateSnippetsWithPreprocessorDefinitions(string id, string source, IEnumerable<string> preprocessorSymbols)
        {
            TestUtils.Use(id);
            var test = new PreprocessorTest(preprocessorSymbols)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        private class PreprocessorTest : VerifyCS.Test
        {
            private readonly IEnumerable<string> _preprocessorSymbols;

            public PreprocessorTest(IEnumerable<string> preprocessorSymbols)
                : base(referenceAncillaryInterop: false)
            {
                _preprocessorSymbols = preprocessorSymbols;
            }

            protected override ParseOptions CreateParseOptions()
                => ((CSharpParseOptions)base.CreateParseOptions()).WithPreprocessorSymbols(_preprocessorSymbols);
        }

        public static IEnumerable<object[]> CodeSnippetsToValidateFallbackForwarder()
        {
            //yield return new object[] { ID(), CodeSnippets.UserDefinedEntryPoint, TestTargetFramework.Net, true };

            // Confirm that all unsupported target frameworks can be generated.
            {
                string code = CodeSnippets.BasicParametersAndModifiers<byte>(CodeSnippets.LibraryImportAttributeDeclaration);
                //yield return new object[] { ID(), code, TestTargetFramework.Net6, false };
                yield return new object[] { ID(), code, TestTargetFramework.Core, false };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, false };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, false };
            }

            // Confirm that all unsupported target frameworks fall back to a forwarder.
            {
                string code = CodeSnippets.BasicParametersAndModifiers<byte[]>(CodeSnippets.LibraryImportAttributeDeclaration);
                yield return new object[] { ID(), code, TestTargetFramework.Net6, true };
                yield return new object[] { ID(), code, TestTargetFramework.Core, true };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, true };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, true };
            }

            // Confirm that all unsupported target frameworks fall back to a forwarder.
            {
                string code = CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<string>(StringMarshalling.Utf16, CodeSnippets.LibraryImportAttributeDeclaration);
                yield return new object[] { ID(), code, TestTargetFramework.Net6, true };
                yield return new object[] { ID(), code, TestTargetFramework.Core, true };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, true };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, true };
            }

            // Confirm that if support is missing for any type (like arrays), we fall back to a forwarder even if other types are supported.
            {
                string code = CodeSnippets.BasicReturnAndParameterByValue("System.Runtime.InteropServices.SafeHandle", "int[]", CodeSnippets.LibraryImportAttributeDeclaration);
                yield return new object[] { ID(), code, TestTargetFramework.Net6, true };
                yield return new object[] { ID(), code, TestTargetFramework.Core, true };
                yield return new object[] { ID(), code, TestTargetFramework.Standard, true };
                yield return new object[] { ID(), code, TestTargetFramework.Framework, true };
            }
        }

        [ParallelTheory]
        [MemberData(nameof(CodeSnippetsToValidateFallbackForwarder))]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task ValidateSnippetsFallbackForwarder(string id, string source, TestTargetFramework targetFramework, bool expectFallbackForwarder)
        {
            TestUtils.Use(id);
            var test = new FallbackForwarderTest(targetFramework, expectFallbackForwarder)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        class FallbackForwarderTest : VerifyCS.Test
        {
            private readonly bool _expectFallbackForwarder;

            public FallbackForwarderTest(TestTargetFramework targetFramework, bool expectFallbackForwarder)
                : base(targetFramework)
            {
                _expectFallbackForwarder = expectFallbackForwarder;
            }
            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                SyntaxTree generatedCode = compilation.SyntaxTrees.Last();
                SemanticModel model = compilation.GetSemanticModel(generatedCode);
                var methods = generatedCode.GetRoot()
                    .DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .ToList();
                MethodDeclarationSyntax generatedMethod = Assert.Single(methods);

                IMethodSymbol method = model.GetDeclaredSymbol(generatedMethod)!;

                // If we expect fallback forwarder, then the DllImportData will not be null.
                Assert.Equal(_expectFallbackForwarder, method.GetDllImportData() is not null);
            }
        }

        public static IEnumerable<object[]> FullyBlittableSnippetsToCompile()
        {
            yield return new[] { ID(), CodeSnippets.UserDefinedEntryPoint };
            yield return new[] { ID(), CodeSnippets.BasicParameterByValue("int") };
        }

        [ParallelTheory]
        [MemberData(nameof(FullyBlittableSnippetsToCompile))]
        public async Task ValidateSnippetsWithBlittableAutoForwarding(string id, string source)
        {
            TestUtils.Use(id);
            var test = new BlittableAutoForwarderTest()
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        class BlittableAutoForwarderTest : VerifyCS.Test
        {
            public BlittableAutoForwarderTest()
                : base(referenceAncillaryInterop: false)
            {
            }

            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                SyntaxTree generatedCode = compilation.SyntaxTrees.Last();
                SemanticModel model = compilation.GetSemanticModel(generatedCode);
                var methods = generatedCode.GetRoot()
                    .DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .ToList();

                Assert.All(methods, method => Assert.NotNull(model.GetDeclaredSymbol(method)!.GetDllImportData()));
            }
        }

        public static IEnumerable<object[]> SnippetsWithBlittableTypesButNonBlittableDataToCompile()
        {
            yield return new[] { ID(), CodeSnippets.AllLibraryImportNamedArguments };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.SetLastErrorTrue<int>() };
        }

        [ParallelTheory]
        [MemberData(nameof(SnippetsWithBlittableTypesButNonBlittableDataToCompile))]
        public async Task ValidateSnippetsWithBlittableTypesButNonBlittableMetadataDoNotAutoForward(string id, string source)
        {
            TestUtils.Use(id);
            var test = new NonBlittableNoAutoForwardTest()
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        class NonBlittableNoAutoForwardTest : VerifyCS.Test
        {
            public NonBlittableNoAutoForwardTest()
                : base(referenceAncillaryInterop: false)
            {
            }

            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                SyntaxTree generatedCode = compilation.SyntaxTrees.Last();
                SemanticModel model = compilation.GetSemanticModel(generatedCode);
                int numStubMethods = generatedCode.GetRoot()
                    .DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .Count();
                int numInnerDllImports = generatedCode.GetRoot()
                    .DescendantNodes().OfType<LocalFunctionStatementSyntax>()
                    .Count();
                Assert.Equal(numStubMethods, numInnerDllImports);
            }
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
            var test = new VerifyCS.Test(referenceAncillaryInterop: true)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };
            test.SolutionTransforms.Add((solution, projectId) =>
                solution.AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId),
                    "UseMarshalType.editorconfig",
                    SourceText.From("""
                        is_global = true
                        build_property.LibraryImportGenerator_UseMarshalType = true
                        """,
                        Encoding.UTF8),
                    filePath: "/UseMarshalType.editorconfig"));
            await test.RunAsync();
        }

        public static IEnumerable<object[]> CodeSnippetsToCompileMultipleSources()
        {
            yield return new object[] { ID(), new[] { CodeSnippets.BasicParametersAndModifiers<int>(), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.Bool) } };
            yield return new object[] { ID(), new[] { CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<int>(StringMarshalling.Utf16), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.Bool) } };
            yield return new object[] { ID(), new[] { CodeSnippets.BasicParameterByValue("int[]", CodeSnippets.DisableRuntimeMarshalling), CodeSnippets.BasicParameterWithByRefModifier("ref", "int") } };
        }

        [ParallelTheory]
        [MemberData(nameof(CodeSnippetsToCompileMultipleSources))]
        public async Task ValidateSnippetsWithMultipleSources(string id, string[] sources)
        {
            TestUtils.Use(id);
            // To enable us to reuse snippets that have markup locations in our multiple-sources test, we'll strip out the markup locations.
            // We need to do this as each snippet expects to be able to define all expected markup locations (starting from 0), so including multiple snippets
            // results in multiple definitions for the same location (which doesn't work). Since we expect no diagnostics, we can strip out the locations.
            await VerifyCS.VerifySourceGeneratorAsync(sources.Select(RemoveTestMarkup).ToArray());
        }

        private static string RemoveTestMarkup(string sourceWithMarkup)
        {
            TestFileMarkupParser.GetSpans(sourceWithMarkup, out string sourceWithoutMarkup, out ImmutableArray<TextSpan> _);
            return sourceWithoutMarkup;
        }

        public static IEnumerable<object[]> CodeSnippetsToVerifyNoTreesProduced()
        {
            string source = """
                using System.Runtime.InteropServices;
                public class Basic { }
                """;
            yield return new object[] { ID(), source, TestTargetFramework.Standard };
            yield return new object[] { ID(), source, TestTargetFramework.Framework };
            yield return new object[] { ID(), source, TestTargetFramework.Net };
        }

        [ParallelTheory]
        [MemberData(nameof(CodeSnippetsToVerifyNoTreesProduced))]
        public async Task ValidateNoGeneratedOuptutForNoImport(string id, string source, TestTargetFramework framework)
        {
            TestUtils.Use(id);
            var test = new NoChangeTest(framework)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        class NoChangeTest : VerifyCS.Test
        {
            public NoChangeTest(TestTargetFramework framework)
                : base(framework)
            {
            }

            protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
            {
                var originalCompilation = await project.GetCompilationAsync(cancellationToken);
                var (newCompilation, diagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
                Assert.Same(originalCompilation, newCompilation);
                return (newCompilation, diagnostics);
            }
        }
    }
}
