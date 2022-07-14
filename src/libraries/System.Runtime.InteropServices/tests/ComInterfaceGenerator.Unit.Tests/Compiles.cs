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

            // SafeHandles
            yield return new[] { ID(), CodeSnippets.BasicParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle") };
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
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NestedMarshallerParametersAndModifiers<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementByValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateless.NonBlittableElementNativeToManagedOnlyReturnValue };
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
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NativeToManagedOnlyReturnValue<int>() };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementByValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementParametersAndModifiers };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementNativeToManagedOnlyOutParameter };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.NonBlittableElementNativeToManagedOnlyReturnValue };
            yield return new[] { ID(), CodeSnippets.CustomCollectionMarshalling.Stateful.CustomElementMarshalling };
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
