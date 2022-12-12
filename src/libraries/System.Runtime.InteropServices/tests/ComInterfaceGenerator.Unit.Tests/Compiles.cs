// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Interop.UnitTests;
using Xunit;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class Compiles
    {
        private static string ID(
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string? filePath = null)
            => TestUtils.GetFileLineName(lineNumber, filePath);

        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            yield return new[] { ID(), CodeSnippets.SpecifiedMethodIndexNoExplicitParameters };
            yield return new[] { ID(), CodeSnippets.SpecifiedMethodIndexNoExplicitParametersNoImplicitThis };
            yield return new[] { ID(), CodeSnippets.SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions };

            // Basic marshalling validation
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
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<byte>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<sbyte>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<short>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<ushort>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<int>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<uint>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<long>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<ulong>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<float>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<double>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<IntPtr>() };
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersNoImplicitThis<UIntPtr>() };

            // Custom type marshalling managed-to-unmanaged
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.PinByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.StackallocByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.StackallocByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.PinByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.MarshallerPinByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.DefaultModeReturnValue };

            // Custom type marshalling unmanaged-to-managed
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateless.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateless.NativeToManagedFinallyOnlyInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateless.ByValueOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateless.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.NativeToManagedFinallyOnlyInParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.ByValueOutParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.UnmanagedToManaged>.Stateful.OptionalStackallocParametersAndModifiers };

            // Custom type marshalling bidirectional

            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.RefParameter };
            yield return new[] { ID(), CustomStructMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.OptionalStackallocParametersAndModifiers };

            // SafeHandles
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiersManagedToUnmanaged("Microsoft.Win32.SafeHandles.SafeFileHandle") };

            // Exception Handling
            // HResult
            yield return new[] { ID(), CodeSnippets.BasicReturnTypeComExceptionHandling("int") };
            yield return new[] { ID(), CodeSnippets.BasicReturnTypeComExceptionHandling("uint") };
            // NaN
            yield return new[] { ID(), CodeSnippets.BasicReturnTypeComExceptionHandling("float") };
            yield return new[] { ID(), CodeSnippets.BasicReturnTypeComExceptionHandling("double") };
            // Default Value
            yield return new[] { ID(), CodeSnippets.BasicReturnTypeComExceptionHandling("nint") };
            // Void
            yield return new[] { ID(), CodeSnippets.BasicReturnTypeComExceptionHandling("void") }; 
        }

        public static IEnumerable<object[]> CustomCollections()
        {
            // Custom collection marshalling
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValue<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValue<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.ByValueWithStaticPinning<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NonBlittableElementByValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateless.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NonBlittableElementByValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.ManagedToUnmanaged>.Stateful.NonBlittableElementNativeToManagedOnlyReturnValue };
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
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.NestedMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateless.CustomElementMarshalling };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets.Bidirectional>.Stateful.CustomElementMarshalling };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        [MemberData(nameof(CustomCollections))]
        public async Task ValidateVTableIndexSnippets(string id, string source)
        {
            _ = id;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            // We allow duplicate usings here since some of the shared snippets add a using for System.Runtime.InteropServices.Marshalling when we already have one in our base snippets.
            TestUtils.AssertPreSourceGeneratorCompilation(comp, "CS0426", "CS0105");

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.VtableIndexStubGenerator());
            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp, "CS0105");
        }
    }
}
