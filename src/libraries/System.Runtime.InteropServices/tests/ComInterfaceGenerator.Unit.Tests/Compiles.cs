// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private static IComInterfaceAttributeProvider GetAttributeProvider(GeneratorKind generator)
            => generator switch
            {
                GeneratorKind.VTableIndexStubGenerator => new VirtualMethodIndexAttributeProvider(),
                GeneratorKind.ComInterfaceGenerator => new GeneratedComInterfaceAttributeProvider(),
                _ => throw new UnreachableException(),
            };

        public static IEnumerable<object[]> CodeSnippetsToCompile(GeneratorKind generator)
        {
            CodeSnippets codeSnippets = new(GetAttributeProvider(generator));
            yield return new[] { ID(), codeSnippets.SpecifiedMethodIndexNoExplicitParameters };
            yield return new[] { ID(), codeSnippets.SpecifiedMethodIndexNoExplicitParametersNoImplicitThis };
            yield return new[] { ID(), codeSnippets.SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions };

            // Use different method modifiers
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers("int", "public") };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers("int", "internal") };

            // Basic marshalling validation
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<byte>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<short>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<ushort>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<int>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<uint>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<long>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<ulong>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<float>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<double>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<byte>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<sbyte>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<short>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<ushort>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<int>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<uint>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<long>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<ulong>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<float>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<double>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<IntPtr>() };
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersNoImplicitThis<UIntPtr>() };

            // Custom type marshalling managed-to-unmanaged
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippetsManagedToUnmanaged = new(new CodeSnippets.ManagedToUnmanaged(GetAttributeProvider(generator)));
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.PinByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.StackallocByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.DefaultModeByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateless.DefaultModeReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NativeToManagedFinallyOnlyOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NativeToManagedOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NativeToManagedFinallyOnlyReturnValue };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.StackallocByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.PinByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.MarshallerPinByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.StackallocParametersAndModifiersNoRef };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.DefaultModeByValueInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsManagedToUnmanaged.Stateful.DefaultModeReturnValue };

            // Custom type marshalling unmanaged-to-managed
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippetsUnmanagedToManaged = new(new CodeSnippets.UnmanagedToManaged(GetAttributeProvider(generator)));
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateless.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateless.NativeToManagedFinallyOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateless.ByValueOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateless.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.NativeToManagedFinallyOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.ByValueOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsUnmanagedToManaged.Stateful.OptionalStackallocParametersAndModifiers };

            // Custom type marshalling bidirectional
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippetsBidirectional = new(new CodeSnippets.Bidirectional(GetAttributeProvider(generator)));
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateless.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateless.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateless.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateless.OptionalStackallocParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateful.ParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateful.ParametersAndModifiersWithFree };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateful.ParametersAndModifiersWithOnInvoked };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateful.MarshalUsingParametersAndModifiers };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateful.RefParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippetsBidirectional.Stateful.OptionalStackallocParametersAndModifiers };

            // SafeHandles
            yield return new[] { ID(), codeSnippets.BasicParametersAndModifiersManagedToUnmanaged("Microsoft.Win32.SafeHandles.SafeFileHandle") };

            // Exception Handling
            // HResult
            yield return new[] { ID(), codeSnippets.BasicReturnTypeComExceptionHandling("int") };
            yield return new[] { ID(), codeSnippets.BasicReturnTypeComExceptionHandling("uint") };
            // NaN
            yield return new[] { ID(), codeSnippets.BasicReturnTypeComExceptionHandling("float") };
            yield return new[] { ID(), codeSnippets.BasicReturnTypeComExceptionHandling("double") };
            // Default Value
            yield return new[] { ID(), codeSnippets.BasicReturnTypeComExceptionHandling("nint") };
            // Void
            yield return new[] { ID(), codeSnippets.BasicReturnTypeComExceptionHandling("void") };
        }

        public static IEnumerable<object[]> CustomCollections(GeneratorKind generator)
        {
            // Custom collection marshalling
            CustomCollectionMarshallingCodeSnippets customCollectionMarshallingCodeSnippetsManagedToUnmanaged = new(new CodeSnippets.ManagedToUnmanaged(GetAttributeProvider(generator)));
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValue<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValue<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueCallerAllocatedBuffer<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithPinning<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.ByValueWithStaticPinning<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NonBlittableElementByValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateless.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NativeToManagedOnlyOutParameter<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NonBlittableElementByValue };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsManagedToUnmanaged.Stateful.NonBlittableElementNativeToManagedOnlyReturnValue };

            CodeSnippets codeSnippets = new(GetAttributeProvider(generator));
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<byte[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<sbyte[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<short[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<ushort[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<int[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<uint[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<long[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<ulong[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<float[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<double[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<IntPtr[]>() };
            yield return new[] { ID(), codeSnippets.MarshalUsingCollectionCountInfoParametersAndModifiers<UIntPtr[]>() };

            CustomCollectionMarshallingCodeSnippets customCollectionMarshallingCodeSnippetsBidirectional = new(new CodeSnippets.Bidirectional(GetAttributeProvider(generator)));
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.NestedMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateless.CustomElementMarshalling };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.DefaultMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<byte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<sbyte>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<short>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<ushort>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<uint>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<long>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<ulong>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<float>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<double>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<IntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerParametersAndModifiers<UIntPtr>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomMarshallerReturnValueLength<int>() };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), customCollectionMarshallingCodeSnippetsBidirectional.Stateful.CustomElementMarshalling };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile), GeneratorKind.VTableIndexStubGenerator)]
        [MemberData(nameof(CustomCollections), GeneratorKind.VTableIndexStubGenerator)]
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

        public static IEnumerable<object[]> ComInterfaceSnippetsToCompile()
        {
            CodeSnippets codeSnippets = new(new GeneratedComInterfaceAttributeProvider());
            yield return new object[] { ID(), codeSnippets.DerivedComInterfaceType };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile), GeneratorKind.ComInterfaceGenerator)]
        [MemberData(nameof(CustomCollections), GeneratorKind.ComInterfaceGenerator)]
        [MemberData(nameof(ComInterfaceSnippetsToCompile))]
        public async Task ValidateComInterfaceSnippets(string id, string source)
        {
            _ = id;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            // We allow duplicate usings here since some of the shared snippets add a using for System.Runtime.InteropServices.Marshalling when we already have one in our base snippets.
            TestUtils.AssertPreSourceGeneratorCompilation(comp, "CS0426", "CS0105");

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.ComInterfaceGenerator());
            Assert.Empty(generatorDiags.Where(IsValidGeneratorDiagnostic));

            List<string> allowedDiagnostics = new()
            {
                // Duplicate 'using'
                "CS0105",
                // Variable assigned to but never read
                "CS0219"
            };
            // There are valid warnings from the generator -- 
            if (generatorDiags.Length != 0)
            {
                List<string> additionalDiags = new() {
                    // No overload for 'ABI_Method' matches function pointer 'delegate* unmanaged<...>'
                    "CS8757",
                    // Cannot use 'parameterType' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                    "CS8894",
                    // The out parameter 'paramName' must be assigned to before control leaves the current method
                    "CS0177",
                    // Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                    "CS8977",
                    // The type 'SafeFileHandle' must be a non-nullable value type, along with all fields at any level of nesting,
                    // in order to use it as parameter 'T' in the generic type or method 'ExceptionAsDefaultMarshaller<T>'
                    "CS8377",
                    // Argument N may not be passed with the 'in' keyword
                    "CS1615"
                };
                allowedDiagnostics.AddRange(additionalDiags);
            }

            TestUtils.AssertPostSourceGeneratorCompilation(newComp, allowedDiagnostics.ToArray());
        }

        private bool IsValidGeneratorDiagnostic(Diagnostic diag)
            => diag.Id != "SYSLIB1051"
                && diag.GetMessage().Contains("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type '")
                && diag.GetMessage().Contains("' does not support it. The generated source will not handle marshalling of parameter");
    }
}
