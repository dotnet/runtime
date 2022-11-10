// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public readonly record struct BoundGenerator(TypePositionInfo TypeInfo, IMarshallingGenerator Generator);

    public class BoundGenerators
    {
        public BoundGenerators(ImmutableArray<TypePositionInfo> elementTypeInfo, Func<TypePositionInfo, IMarshallingGenerator> generatorFactoryCallback, Action<TypePositionInfo, string> extendedInvariantViolationCallback)
        {
            ImmutableArray<BoundGenerator>.Builder allMarshallers = ImmutableArray.CreateBuilder<BoundGenerator>();
            ImmutableArray<BoundGenerator>.Builder nativeParamMarshallers = ImmutableArray.CreateBuilder<BoundGenerator>();
            ImmutableArray<BoundGenerator>.Builder managedParamMarshallers = ImmutableArray.CreateBuilder<BoundGenerator>();
            bool foundNativeRetMarshaller = false;
            bool foundManagedRetMarshaller = false;
            TypePositionInfo? nativeExceptionInfo = null;
            NativeReturnMarshaller = new(new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance), new Forwarder());
            ManagedReturnMarshaller = new(new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance), new Forwarder());
            ManagedExceptionMarshaller = new(new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance), new Forwarder());

            foreach (TypePositionInfo argType in elementTypeInfo)
            {
                if (argType.IsManagedExceptionPosition)
                {
                    Debug.Assert(nativeExceptionInfo is null);
                    nativeExceptionInfo = argType;
                    // The exception marshaller's selection might depend on the unmanaged type of the native return marshaller.
                    // Delay binding the generator until we've processed the native return marshaller.
                    continue;
                }

                BoundGenerator generator = new BoundGenerator(argType, generatorFactoryCallback(argType));

                allMarshallers.Add(generator);
                if (argType.IsManagedReturnPosition)
                {
                    Debug.Assert(!foundManagedRetMarshaller);
                    ManagedReturnMarshaller = generator;
                    foundManagedRetMarshaller = true;
                }
                if (argType.IsNativeReturnPosition)
                {
                    Debug.Assert(!foundNativeRetMarshaller);
                    NativeReturnMarshaller = generator;
                    foundNativeRetMarshaller = true;
                }
                if (argType.ManagedIndex is not (TypePositionInfo.UnsetIndex or TypePositionInfo.ReturnIndex or TypePositionInfo.ExceptionIndex))
                {
                    managedParamMarshallers.Add(generator);
                }
                if (!argType.IsManagedReturnPosition && !argType.IsNativeReturnPosition && !argType.IsManagedExceptionPosition)
                {
                    nativeParamMarshallers.Add(generator);
                }
            }

            bool stubReturnsVoid = ManagedReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void;

            if (!ManagedReturnMarshaller.TypeInfo.IsNativeReturnPosition && !stubReturnsVoid)
            {
                // If the managed ret marshaller isn't the native ret marshaller, then the managed ret marshaller
                // is a parameter.
                nativeParamMarshallers.Add(ManagedReturnMarshaller);
            }
            NativeParameterMarshallers = nativeParamMarshallers.ToImmutable();
            ManagedParameterMarshallers = managedParamMarshallers.ToImmutable();

            // Now that we've processed all of the signature marshallers,
            // we'll handle the special ones that might depend on them, like the exception marshaller.
            if (nativeExceptionInfo is not null)
            {
                if (nativeExceptionInfo.MarshallingAttributeInfo is ComExceptionMarshalling)
                {
                    nativeExceptionInfo = nativeExceptionInfo with
                    {
                        MarshallingAttributeInfo = ComExceptionMarshalling.CreateSpecificMarshallingInfo(NativeReturnMarshaller.Generator.AsNativeType(NativeReturnMarshaller.TypeInfo))
                    };
                }

                // We explicitly don't include exceptionMarshaller in the allMarshallers collection
                // as it needs to be specially emitted.
                BoundGenerator exceptionMarshaller = new(nativeExceptionInfo, generatorFactoryCallback(nativeExceptionInfo));

                // The unmanaged type of the exception marshaller must match the unmanaged type for the native return marshaller, as the exception marshaller is expected to produce
                // a return value to use in the exceptional case.
                if (exceptionMarshaller.Generator.AsNativeType(exceptionMarshaller.TypeInfo) != NativeReturnMarshaller.Generator.AsNativeType(NativeReturnMarshaller.TypeInfo))
                {
                    extendedInvariantViolationCallback(nativeExceptionInfo, "The unmanaged type for the exception marshaller must match the unmanaged type for the native return marshaller.");
                }
                else
                {
                    ManagedExceptionMarshaller = exceptionMarshaller;
                }
            }

            // We are doing a topological sort of our marshallers to ensure that each parameter/return value's
            // dependencies are unmarshalled before their dependents. This comes up in the case of contiguous
            // collections, where the number of elements in a collection are provided via another parameter/return value.
            // When using nested collections, the parameter that represents the number of elements of each element of the
            // outer collection is another collection. As a result, there are two options on how to retrieve the size.
            // Either we partially unmarshal the collection of counts while unmarshalling the collection of elements,
            // or we unmarshal our parameters and return value in an order such that we can use the managed identifiers
            // for our lengths.
            // Here's an example signature where the dependency shows up:
            //
            // [LibraryImport(NativeExportsNE_Binary, EntryPoint = "transpose_matrix")]
            // [return: MarshalUsing(CountElementName = "numColumns")]
            // [return: MarshalUsing(CountElementName = "numRows", ElementIndirectionDepth = 1)]
            // public static partial int[][] TransposeMatrix(
            //  int[][] matrix,
            //  [MarshalUsing(CountElementName="numColumns")] ref int[] numRows,
            //  int numColumns);
            //
            // In this scenario, we'd traditionally unmarshal the return value and then each parameter. However, since
            // the return value has dependencies on numRows and numColumns and numRows has a dependency on numColumns,
            // we want to unmarshal numColumns, then numRows, then the return value.
            // A topological sort ensures we get this order correct.
            AllMarshallers = MarshallerHelpers.GetTopologicallySortedElements(
                allMarshallers,
                static m => GetInfoIndex(m.TypeInfo),
                static m => GetInfoDependencies(m.TypeInfo))
                .ToImmutableArray();

            static IEnumerable<(bool, int)> GetInfoDependencies(TypePositionInfo info)
            {
                // A parameter without a managed index cannot have any dependencies.
                if (info.ManagedIndex == TypePositionInfo.UnsetIndex)
                {
                    return Array.Empty<(bool, int)>();
                }
                return MarshallerHelpers.GetDependentElementsOfMarshallingInfo(info.MarshallingAttributeInfo)
                    .Select(static info => GetInfoIndex(info)).ToImmutableArray();
            }

            static (bool IsManagedIndex, int Index) GetInfoIndex(TypePositionInfo info)
            {
                // A TypePositionInfo needs to have either a managed or native index.
                // We'll prioritize representing the managed index if possible
                // as our dependency logic depends on the managed index since the native
                // index can be modified by injected parameters.
                if (info.ManagedIndex != TypePositionInfo.UnsetIndex)
                {
                    return (true, info.ManagedIndex);
                }
                return (false, info.NativeIndex);
            }
        }

        public BoundGenerator ManagedReturnMarshaller { get; }

        public BoundGenerator NativeReturnMarshaller { get; }

        public BoundGenerator ManagedExceptionMarshaller { get; }

        public ImmutableArray<BoundGenerator> AllMarshallers { get; }

        public ImmutableArray<BoundGenerator> ManagedParameterMarshallers { get; }

        public ImmutableArray<BoundGenerator> NativeParameterMarshallers { get; }
        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData(StubCodeContext context)
        {
            return (
                ParameterList(
                    SeparatedList(
                        NativeParameterMarshallers.Select(marshaler => marshaler.Generator.AsParameter(marshaler.TypeInfo, context)))),
                NativeReturnMarshaller.Generator.AsReturnType(NativeReturnMarshaller.TypeInfo),
                NativeReturnMarshaller.Generator.GenerateAttributesForReturnType(NativeReturnMarshaller.TypeInfo)
            );
        }

        public bool ManagedNativeSameReturn => ManagedReturnMarshaller.TypeInfo.IsNativeReturnPosition;

        public bool IsManagedVoidReturn => ManagedReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void;

        public bool IsUnmanagedVoidReturn => NativeReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void;

        public bool HasManagedExceptionMarshaller => ManagedExceptionMarshaller.Generator is not Forwarder;
    }
}
