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
        public BoundGenerators(ImmutableArray<TypePositionInfo> elementTypeInfo, Func<TypePositionInfo, IMarshallingGenerator> generatorFactoryCallback)
        {
            ImmutableArray<BoundGenerator>.Builder allMarshallers = ImmutableArray.CreateBuilder<BoundGenerator>();
            ImmutableArray<BoundGenerator>.Builder paramMarshallers = ImmutableArray.CreateBuilder<BoundGenerator>();
            bool foundNativeRetMarshaller = false;
            bool foundManagedRetMarshaller = false;
            NativeReturnMarshaller = new(new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance), new Forwarder());
            ManagedReturnMarshaller = new(new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance), new Forwarder());

            foreach (TypePositionInfo argType in elementTypeInfo)
            {
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
                if (!argType.IsManagedReturnPosition && !argType.IsNativeReturnPosition)
                {
                    paramMarshallers.Add(generator);
                }
            }

            bool stubReturnsVoid = ManagedReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void;

            if (!ManagedReturnMarshaller.TypeInfo.IsNativeReturnPosition && !stubReturnsVoid)
            {
                // If the managed ret marshaller isn't the native ret marshaller, then the managed ret marshaller
                // is a parameter.
                paramMarshallers.Add(ManagedReturnMarshaller);
            }
            NativeParameterMarshallers = paramMarshallers.ToImmutable();

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

            static IEnumerable<int> GetInfoDependencies(TypePositionInfo info)
            {
                // A parameter without a managed index cannot have any dependencies.
                if (info.ManagedIndex == TypePositionInfo.UnsetIndex)
                {
                    return Array.Empty<int>();
                }
                return MarshallerHelpers.GetDependentElementsOfMarshallingInfo(info.MarshallingAttributeInfo)
                    .Select(static info => GetInfoIndex(info)).ToImmutableArray();
            }

            static int GetInfoIndex(TypePositionInfo info)
            {
                if (info.ManagedIndex == TypePositionInfo.UnsetIndex)
                {
                    // A TypePositionInfo needs to have either a managed or native index.
                    // We use negative values of the native index to distinguish them from the managed index.
                    return -info.NativeIndex;
                }
                return info.ManagedIndex;
            }
        }

        public BoundGenerator ManagedReturnMarshaller { get; }

        public BoundGenerator NativeReturnMarshaller { get; }

        public ImmutableArray<BoundGenerator> AllMarshallers { get; }

        public ImmutableArray<BoundGenerator> NativeParameterMarshallers { get; }
        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData()
        {
            return (
                ParameterList(
                    SeparatedList(
                        NativeParameterMarshallers.Select(marshaler => marshaler.Generator.AsParameter(marshaler.TypeInfo)))),
                NativeReturnMarshaller.Generator.AsReturnType(NativeReturnMarshaller.TypeInfo),
                NativeReturnMarshaller.Generator.GenerateAttributesForReturnType(NativeReturnMarshaller.TypeInfo)
            );
        }

        public bool ManagedNativeSameReturn => ManagedReturnMarshaller.TypeInfo.IsNativeReturnPosition;

        public bool IsManagedVoidReturn => ManagedReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void;
    }
}
