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

            // Custom type marshalling
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

            // SafeHandles
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle") };
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
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NestedMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementByValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateless.NonBlittableElementNativeToManagedOnlyReturnValue };
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
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementByValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CustomCollectionMarshallingCodeSnippets<CodeSnippets>.Stateful.CustomElementMarshalling };
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
