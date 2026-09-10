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
    public sealed class BoundGenerators
    {
        private BoundGenerators() { }

        public static BoundGenerators Create(ImmutableArray<TypePositionInfo> elementTypeInfo, IMarshallingGeneratorResolver generatorResolver, StubCodeContext context, IUnboundMarshallingGenerator fallbackGenerator, out ImmutableArray<GeneratorDiagnostic> generatorBindingDiagnostics)
        {
            IBoundMarshallingGenerator defaultBoundGenerator = fallbackGenerator.Bind(new TypePositionInfo(SpecialTypeInfo.Void, NoMarshallingInfo.Instance), context);
            BoundGenerators result = new();

            ImmutableArray<IBoundMarshallingGenerator>.Builder signatureMarshallers = ImmutableArray.CreateBuilder<IBoundMarshallingGenerator>();
            ImmutableArray<IBoundMarshallingGenerator>.Builder nativeParamMarshallers = ImmutableArray.CreateBuilder<IBoundMarshallingGenerator>();
            ImmutableArray<IBoundMarshallingGenerator>.Builder managedParamMarshallers = ImmutableArray.CreateBuilder<IBoundMarshallingGenerator>();
            ImmutableArray<GeneratorDiagnostic>.Builder generatorDiagnostics = ImmutableArray.CreateBuilder<GeneratorDiagnostic>();
            IBoundMarshallingGenerator managedReturnMarshaller = defaultBoundGenerator;
            IBoundMarshallingGenerator nativeReturnMarshaller = defaultBoundGenerator;
            IBoundMarshallingGenerator managedExceptionMarshaller = defaultBoundGenerator;
            TypePositionInfo? errorHandlingInfo = null;

            foreach (TypePositionInfo argType in elementTypeInfo)
            {
                if (argType.IsErrorHandlingPosition)
                {
                    Debug.Assert(errorHandlingInfo is null);
                    errorHandlingInfo = argType;
                    // Delay binding until all ordinary signature marshallers have been processed.
                    continue;
                }

                IBoundMarshallingGenerator generator = CreateGenerator(argType, generatorResolver);

                signatureMarshallers.Add(generator);
                if (argType.IsManagedReturnPosition)
                {
                    Debug.Assert(managedReturnMarshaller == defaultBoundGenerator);
                    managedReturnMarshaller = generator;
                }
                if (argType.IsNativeReturnPosition)
                {
                    Debug.Assert(nativeReturnMarshaller == defaultBoundGenerator);
                    nativeReturnMarshaller = generator;
                }
                if (!TypePositionInfo.IsSpecialIndex(argType.ManagedIndex))
                {
                    managedParamMarshallers.Add(generator);
                }
                if (!TypePositionInfo.IsSpecialIndex(argType.NativeIndex))
                {
                    nativeParamMarshallers.Add(generator);
                }
            }

            // Now that we've processed all of the signature marshallers, handle the error marshaller,
            // which may depend on or overlap with another marshaller in the native position.
            // Some cases may require an overlap, such as when using COM exception marshalling.
            if (errorHandlingInfo is not null && context.Direction == MarshalDirection.ManagedToUnmanaged)
            {
                IBoundMarshallingGenerator? overlappedMarshaller = FindOverlappedMarshaller(errorHandlingInfo);
                IMarshallingGeneratorResolver errorHandlerFactory = generatorResolver;
                if (overlappedMarshaller is not null)
                {
                    errorHandlerFactory = new MatchingNativeTypeValidator(overlappedMarshaller.NativeType, errorHandlerFactory);
                }

                IBoundMarshallingGenerator errorHandlingMarshaller = CreateGenerator(errorHandlingInfo, errorHandlerFactory);
                signatureMarshallers.Add(errorHandlingMarshaller);

                if (overlappedMarshaller is null)
                {
                    if (errorHandlingInfo.IsNativeReturnPosition)
                    {
                        Debug.Assert(!nativeReturnMarshaller.TypeInfo.IsNativeReturnPosition);
                        nativeReturnMarshaller = errorHandlingMarshaller;
                    }
                    else if (!TypePositionInfo.IsSpecialIndex(errorHandlingInfo.NativeIndex))
                    {
                        nativeParamMarshallers.Add(errorHandlingMarshaller);
                    }
                }
            }

            if (errorHandlingInfo is not null && context.Direction == MarshalDirection.UnmanagedToManaged)
            {
                IBoundMarshallingGenerator? overlappedMarshaller = null;
                if (errorHandlingInfo.IsNativeReturnPosition)
                {
                    overlappedMarshaller = nativeReturnMarshaller;
                }
                else if (errorHandlingInfo.NativeIndex is not (TypePositionInfo.UnsetIndex or TypePositionInfo.ExceptionIndex))
                {
                    overlappedMarshaller = nativeParamMarshallers.FirstOrDefault(e => e.TypeInfo.NativeIndex == errorHandlingInfo.NativeIndex);
                }

                if (errorHandlingInfo.MarshallingAttributeInfo is ComExceptionMarshalling)
                {
                    if (overlappedMarshaller is null)
                    {
                        generatorDiagnostics.Add(new GeneratorDiagnostic.NotSupported(errorHandlingInfo));
                    }
                    else
                    {
                        errorHandlingInfo = errorHandlingInfo with
                        {
                            MarshallingAttributeInfo = ComExceptionMarshalling.CreateSpecificMarshallingInfo(overlappedMarshaller.NativeType)
                        };
                    }
                }

                IMarshallingGeneratorResolver exceptionHandlerFactory = generatorResolver;

                if (overlappedMarshaller is not null)
                {
                    exceptionHandlerFactory = new MatchingNativeTypeValidator(overlappedMarshaller.NativeType, exceptionHandlerFactory);
                }

                managedExceptionMarshaller = CreateGenerator(errorHandlingInfo, exceptionHandlerFactory);

                if (overlappedMarshaller is null && !TypePositionInfo.IsSpecialIndex(errorHandlingInfo.NativeIndex))
                {
                    // If the exception marshaller doesn't overlap with another marshaller but has a native index,
                    // we need to add it to the list of native parameter marshallers.
                    nativeParamMarshallers.Add(managedExceptionMarshaller);
                }
            }

            // Sort the parameter marshallers by index to ensure that we handle them in order when producing signatures.
            managedParamMarshallers.Sort(static (m1, m2) => m1.TypeInfo.ManagedIndex.CompareTo(m2.TypeInfo.ManagedIndex));
            nativeParamMarshallers.Sort(static (m1, m2) => m1.TypeInfo.NativeIndex.CompareTo(m2.TypeInfo.NativeIndex));

            generatorBindingDiagnostics = generatorDiagnostics.ToImmutable();

            return new BoundGenerators()
            {
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
                SignatureMarshallers = MarshallerHelpers.GetTopologicallySortedElements(
                    signatureMarshallers,
                    static m => GetInfoIndex(m.TypeInfo),
                    static m => GetInfoDependencies(m.TypeInfo))
                    .ToImmutableArray(),
                NativeParameterMarshallers = nativeParamMarshallers.ToImmutable(),
                ManagedParameterMarshallers = managedParamMarshallers.ToImmutable(),
                ManagedReturnMarshaller = managedReturnMarshaller,
                NativeReturnMarshaller = nativeReturnMarshaller,
                ManagedExceptionMarshaller = managedExceptionMarshaller,
            };

            static IEnumerable<(bool IsManagedIndex, int Index)> GetInfoDependencies(TypePositionInfo info)
            {
                // A parameter without a managed index cannot have any dependencies.
                if (info.ManagedIndex == TypePositionInfo.UnsetIndex)
                {
                    return Array.Empty<(bool, int)>();
                }
                return info.MarshallingAttributeInfo.ElementDependencies
                    .Select(static info => GetInfoIndex(info)).ToImmutableArray();
            }

            static (bool IsManagedIndex, int Index) GetInfoIndex(TypePositionInfo info)
            {
                if (info.IsErrorHandlingPosition)
                {
                    return (false, info.NativeIndex);
                }

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

            IBoundMarshallingGenerator? FindOverlappedMarshaller(TypePositionInfo info)
            {
                if (info.IsNativeReturnPosition)
                {
                    return nativeReturnMarshaller.TypeInfo.IsNativeReturnPosition
                        ? nativeReturnMarshaller
                        : null;
                }

                return TypePositionInfo.IsSpecialIndex(info.NativeIndex)
                    ? null
                    : nativeParamMarshallers.FirstOrDefault(
                        marshaller => marshaller.TypeInfo.NativeIndex == info.NativeIndex);
            }

            IBoundMarshallingGenerator CreateGenerator(TypePositionInfo p, IMarshallingGeneratorResolver factory)
            {
                ResolvedGenerator generator = factory.Create(p, context);
                generatorDiagnostics.AddRange(generator.Diagnostics);
                return generator.IsResolvedWithoutErrors ? generator.Generator : fallbackGenerator.Bind(p, context);
            }
        }

        public IBoundMarshallingGenerator ManagedReturnMarshaller { get; private init; }

        public IBoundMarshallingGenerator NativeReturnMarshaller { get; private init; }

        public IBoundMarshallingGenerator ManagedExceptionMarshaller { get; private init; }

        public ImmutableArray<IBoundMarshallingGenerator> SignatureMarshallers { get; private init; }

        public ImmutableArray<IBoundMarshallingGenerator> ManagedParameterMarshallers { get; private init; }

        public ImmutableArray<IBoundMarshallingGenerator> NativeParameterMarshallers { get; private init; }

        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData(StubIdentifierContext context)
        {
            return (
                ParameterList(
                    SeparatedList(
                        NativeParameterMarshallers.Select(marshaler => marshaler.AsParameter(context)))),
                NativeReturnMarshaller.AsReturnType(),
                NativeReturnMarshaller.GenerateAttributesForReturnType()
            );
        }

        public bool ManagedNativeSameReturn => ManagedReturnMarshaller.TypeInfo.IsNativeReturnPosition;

        public bool IsManagedVoidReturn => ManagedReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void;

        public bool IsUnmanagedVoidReturn => NativeReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void;

        public bool HasManagedExceptionMarshaller => !ManagedExceptionMarshaller.IsForwarder();

        /// <summary>
        /// Validate that the resolved generator resolves to the same native type.
        /// </summary>
        private sealed class MatchingNativeTypeValidator(ManagedTypeInfo requiredNativeType, IMarshallingGeneratorResolver inner) : IMarshallingGeneratorResolver
        {
            public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
            {
                ResolvedGenerator generator = inner.Create(info, context);
                if (!generator.IsResolvedWithoutErrors)
                {
                    return generator;
                }
                // Marshallers that share a native position must have the same native type.
                if (generator.Generator.NativeType != requiredNativeType)
                {
                    return ResolvedGenerator.NotSupported(info, context, new(info)
                    {
                        NotSupportedDetails = SR.MarshallerInOverlappingNativePositionMustMatchNativeType
                    });
                }
                return generator;
            }
        }
    }
}
