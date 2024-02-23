// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public readonly record struct AttributedMarshallingModelOptions(
        bool RuntimeMarshallingDisabled,
        MarshalMode ManagedToUnmanagedMode,
        MarshalMode BidirectionalMode,
        MarshalMode UnmanagedToManagedMode,
        bool ResolveElementsFromSelf);

    public class AttributedMarshallingModelGeneratorResolver : IMarshallingGeneratorResolver
    {
        private static readonly ImmutableDictionary<string, string> AddDisableRuntimeMarshallingAttributeProperties =
            ImmutableDictionary<string, string>.Empty.Add(GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute, GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute);

        private static readonly BlittableMarshaller s_blittable = new BlittableMarshaller();
        private static readonly Forwarder s_forwarder = new Forwarder();

        private readonly IMarshallingGeneratorResolver _elementGeneratorResolver;

        public AttributedMarshallingModelGeneratorResolver(
            IMarshallingGeneratorResolver elementGeneratorResolver,
            AttributedMarshallingModelOptions options)
        {
            Options = options;
            _elementGeneratorResolver = elementGeneratorResolver;
            if (options.ResolveElementsFromSelf)
            {
                _elementGeneratorResolver = new CompositeMarshallingGeneratorResolver(this, _elementGeneratorResolver);
            }
        }

        private AttributedMarshallingModelOptions Options { get; }

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is NativeMarshallingAttributeInfo marshalInfo)
            {
                return CreateCustomNativeTypeMarshaller(info, context, marshalInfo);
            }

            if (info.MarshallingAttributeInfo is UnmanagedBlittableMarshallingInfo blittableInfo)
            {
                if (Options.RuntimeMarshallingDisabled || blittableInfo.IsStrictlyBlittable)
                {
                    return ResolvedGenerator.Resolved(s_blittable);
                }

                return ResolvedGenerator.NotSupported(
                    new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.RuntimeMarshallingMustBeDisabled,
                        DiagnosticProperties = AddDisableRuntimeMarshallingAttributeProperties
                    });
            }

            if (info.MarshallingAttributeInfo is MissingSupportMarshallingInfo)
            {
                return ResolvedGenerator.Resolved(s_forwarder);
            }

            return ResolvedGenerator.UnresolvedGenerator;
        }

        private record struct ExpressionOrNotSupported(ExpressionSyntax? Expression, GeneratorDiagnostic.NotSupported? NotSupported)
        {
            public ExpressionOrNotSupported(ExpressionSyntax expression)
                : this(expression, null)
            {
            }
            public ExpressionOrNotSupported(GeneratorDiagnostic.NotSupported notSupportedDiagnostic)
                : this(null, notSupportedDiagnostic)
            {
            }
        }

        private ExpressionOrNotSupported GetNumElementsExpressionFromMarshallingInfo(TypePositionInfo info, CountInfo count, StubCodeContext context)
        {
            switch (count)
            {
                case SizeAndParamIndexInfo(int size, SizeAndParamIndexInfo.UnspecifiedParam):
                    return new(GetConstSizeExpression(size));
                case ConstSizeCountInfo(int size):
                    return new(GetConstSizeExpression(size));
                case SizeAndParamIndexInfo(SizeAndParamIndexInfo.UnspecifiedConstSize, TypePositionInfo param):
                    {
                        return GetExpressionForParam(param, out bool isIntType) switch
                        {
                            (ExpressionSyntax expr, null) => new(isIntType ? expr : CheckedExpression(SyntaxKind.CheckedExpression, expr)),
                            (null, GeneratorDiagnostic.NotSupported notSupported) => new(notSupported),
                            (not null, not null) => throw new UnreachableException()
                        };
                    }
                case SizeAndParamIndexInfo(int size, TypePositionInfo param):
                    return GetExpressionForParam(param, out bool _) switch
                    {
                        (ExpressionSyntax expr, null) => new(
                            CheckedExpression(SyntaxKind.CheckedExpression,
                                BinaryExpression(SyntaxKind.AddExpression,
                                    GetConstSizeExpression(size),
                                    expr))),
                        (null, GeneratorDiagnostic.NotSupported notSupported) => new(notSupported),
                        (not null, not null) => throw new UnreachableException()
                    };
                case CountElementCountInfo(TypePositionInfo elementInfo):
                    {
                        return GetExpressionForParam(elementInfo, out bool isIntType) switch
                        {
                            (ExpressionSyntax expr, null) => new(isIntType ? expr : CheckedExpression(SyntaxKind.CheckedExpression, expr)),
                            (null, GeneratorDiagnostic.NotSupported notSupported) => new(notSupported),
                            (not null, not null) => throw new UnreachableException()
                        };
                    }
                default:
                    return new(new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.ArraySizeMustBeSpecified
                    });
            }

            static LiteralExpressionSyntax GetConstSizeExpression(int size)
            {
                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(size));
            }

            ExpressionOrNotSupported GetExpressionForParam(TypePositionInfo paramInfo, out bool isIntType)
            {
                ExpressionSyntax numElementsExpression = GetIndexedNumElementsExpression(
                           context,
                           paramInfo,
                           out int numIndirectionLevels);

                ManagedTypeInfo type = paramInfo.ManagedType;
                MarshallingInfo marshallingInfo = paramInfo.MarshallingAttributeInfo;

                for (int i = 0; i < numIndirectionLevels; i++)
                {
                    if (marshallingInfo is NativeLinearCollectionMarshallingInfo collectionInfo)
                    {
                        CustomTypeMarshallerData marshallerData = GetMarshallerDataForTypePositionInfo(collectionInfo.Marshallers, info, context);
                        type = marshallerData.CollectionElementType;
                        marshallingInfo = marshallerData.CollectionElementMarshallingInfo;
                    }
                    else
                    {
                        isIntType = false;
                        return new(new GeneratorDiagnostic.NotSupported(info, context)
                        {
                            NotSupportedDetails = SR.CollectionSizeParamTypeMustBeIntegral
                        });
                    }
                }

                if (type is not SpecialTypeInfo specialType || !specialType.SpecialType.IsIntegralType())
                {
                    isIntType = false;
                    return new(new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.CollectionSizeParamTypeMustBeIntegral
                    });
                }

                isIntType = specialType.SpecialType == SpecialType.System_Int32;
                return new(isIntType
                    ? numElementsExpression
                    : CastExpression(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        ParenthesizedExpression(numElementsExpression)));
            }

            static ExpressionSyntax GetIndexedNumElementsExpression(StubCodeContext context, TypePositionInfo numElementsInfo, out int numIndirectionLevels)
            {
                Stack<string> indexerStack = new();

                StubCodeContext? currentContext = context;
                StubCodeContext lastContext = null!;

                while (currentContext is not null)
                {
                    if (currentContext is LinearCollectionElementMarshallingCodeContext collectionContext)
                    {
                        indexerStack.Push(collectionContext.IndexerIdentifier);
                    }
                    lastContext = currentContext;
                    currentContext = currentContext.ParentContext;
                }

                numIndirectionLevels = indexerStack.Count;

                ExpressionSyntax indexedNumElements = IdentifierName(lastContext.GetIdentifiers(numElementsInfo).managed);
                while (indexerStack.Count > 0)
                {
                    NameSyntax indexer = IdentifierName(indexerStack.Pop());
                    indexedNumElements = ElementAccessExpression(indexedNumElements)
                        .AddArgumentListArguments(Argument(indexer));
                }

                return indexedNumElements;
            }
        }

        private bool ValidateRuntimeMarshallingOptions(CustomTypeMarshallerData marshallerData)
        {
            if (Options.RuntimeMarshallingDisabled || marshallerData.IsStrictlyBlittable)
                return true;

            if (marshallerData.NativeType is SpecialTypeInfo specialType && specialType.SpecialType.IsAlwaysBlittable())
                return true;

            if (marshallerData.NativeType is PointerTypeInfo)
                return true;

            return false;
        }

        private CustomTypeMarshallerData GetMarshallerDataForTypePositionInfo(CustomTypeMarshallers marshallers, TypePositionInfo info, StubCodeContext context)
        {
            MarshalDirection elementDirection = MarshallerHelpers.GetMarshalDirection(info, context);

            return elementDirection switch
            {
                MarshalDirection.ManagedToUnmanaged => marshallers.GetModeOrDefault(Options.ManagedToUnmanagedMode),
                MarshalDirection.Bidirectional => marshallers.GetModeOrDefault(Options.BidirectionalMode),
                MarshalDirection.UnmanagedToManaged => marshallers.GetModeOrDefault(Options.UnmanagedToManagedMode),
                _ => throw new UnreachableException()
            };
        }

        private ResolvedGenerator CreateCustomNativeTypeMarshaller(TypePositionInfo info, StubCodeContext context, NativeMarshallingAttributeInfo marshalInfo)
        {
            if (ValidateCustomNativeTypeMarshallingSupported(info, context, marshalInfo) is GeneratorDiagnostic.NotSupported diagnostic)
            {
                return ResolvedGenerator.NotSupported(diagnostic);
            }

            CustomTypeMarshallerData marshallerData = GetMarshallerDataForTypePositionInfo(marshalInfo.Marshallers, info, context);
            if (!ValidateRuntimeMarshallingOptions(marshallerData))
            {
                return ResolvedGenerator.NotSupported(new(info, context)
                {
                    NotSupportedDetails = SR.RuntimeMarshallingMustBeDisabled,
                    DiagnosticProperties = AddDisableRuntimeMarshallingAttributeProperties
                });
            }

            // Collections have extra configuration, so handle them separately.
            if (marshalInfo is NativeLinearCollectionMarshallingInfo collectionMarshallingInfo)
                return CreateNativeCollectionMarshaller(info, context, marshallerData, collectionMarshallingInfo);

            ICustomTypeMarshallingStrategy marshallingStrategy;
            if (marshallerData.HasState)
            {
                marshallingStrategy = new StatefulValueMarshalling(marshallerData.MarshallerType, marshallerData.NativeType, marshallerData.Shape);
                if (marshallerData.Shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                    marshallingStrategy = new StatefulCallerAllocatedBufferMarshalling(marshallingStrategy, marshallerData.MarshallerType.Syntax, marshallerData.BufferElementType.Syntax);
            }
            else
            {
                marshallingStrategy = new StatelessValueMarshalling(marshallerData.MarshallerType.Syntax, marshallerData.NativeType, marshallerData.Shape);
                if (marshallerData.Shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                    marshallingStrategy = new StatelessCallerAllocatedBufferMarshalling(marshallingStrategy, marshallerData.MarshallerType.Syntax, marshallerData.BufferElementType.Syntax, isLinearCollectionMarshalling: false);

                FreeStrategy freeStrategy = GetFreeStrategy(info, context);

                if (freeStrategy == FreeStrategy.FreeOriginal)
                {
                    marshallingStrategy = new UnmanagedToManagedOwnershipTrackingStrategy(marshallingStrategy);
                }

                if (freeStrategy != FreeStrategy.NoFree && marshallerData.Shape.HasFlag(MarshallerShape.Free))
                {
                    marshallingStrategy = new StatelessFreeMarshalling(marshallingStrategy, marshallerData.MarshallerType.Syntax);
                }

                if (freeStrategy == FreeStrategy.FreeOriginal)
                {
                    marshallingStrategy = new CleanupOwnedOriginalValueMarshalling(marshallingStrategy);
                }
            }

            IMarshallingGenerator marshallingGenerator = new CustomTypeMarshallingGenerator(marshallingStrategy, ByValueMarshalKindSupportDescriptor.Default, marshallerData.Shape.HasFlag(MarshallerShape.StatelessPinnableReference));

            if (marshallerData.Shape.HasFlag(MarshallerShape.StatelessPinnableReference))
            {
                marshallingGenerator = new StaticPinnableManagedValueMarshaller(marshallingGenerator, marshallerData.MarshallerType.Syntax);
            }

            return ResolvedGenerator.Resolved(marshallingGenerator);
        }

        private ResolvedGenerator CreateNativeCollectionMarshaller(
            TypePositionInfo info,
            StubCodeContext context,
            CustomTypeMarshallerData marshallerData,
            NativeLinearCollectionMarshallingInfo marshalInfo)
        {
            var elementInfo = new TypePositionInfo(marshallerData.CollectionElementType, marshallerData.CollectionElementMarshallingInfo)
            {
                InstanceIdentifier = info.InstanceIdentifier,
                ManagedIndex = info.ManagedIndex,
                RefKind = CreateElementRefKind(info.RefKind, info.ByValueContentsMarshalKind)
            };
            ResolvedGenerator resolvedElementMarshaller = _elementGeneratorResolver.Create(
                elementInfo,
                new LinearCollectionElementMarshallingCodeContext(StubCodeContext.Stage.Setup, string.Empty, string.Empty, context));

            if (!resolvedElementMarshaller.IsResolvedWithoutErrors)
            {
                return resolvedElementMarshaller;
            }
            IMarshallingGenerator elementMarshaller = resolvedElementMarshaller.Generator;

            ExpressionSyntax numElementsExpression = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0));
            if (MarshallerHelpers.GetMarshalDirection(info, context) != MarshalDirection.ManagedToUnmanaged)
            {
                // In this case, we need a numElementsExpression supplied from metadata, so we'll calculate it here.
                ExpressionOrNotSupported numElementsExpressionResult = GetNumElementsExpressionFromMarshallingInfo(info, marshalInfo.ElementCountInfo, context);
                if (numElementsExpressionResult is (_, GeneratorDiagnostic.NotSupported notSupportedDiagnostic))
                {
                    return ResolvedGenerator.NotSupported(notSupportedDiagnostic);
                }
                numElementsExpression = numElementsExpressionResult.Expression;
            }

            // Insert the unmanaged element type into the marshaller type
            TypeSyntax unmanagedElementType = elementMarshaller.AsNativeType(elementInfo).Syntax.GetCompatibleGenericTypeParameterSyntax();
            ManagedTypeInfo marshallerType = marshallerData.MarshallerType;
            TypeSyntax marshallerTypeSyntax = ReplacePlaceholderSyntaxWithUnmanagedTypeSyntax(marshallerType.Syntax, marshalInfo, unmanagedElementType);
            marshallerType = marshallerType with
            {
                FullTypeName = marshallerTypeSyntax.ToString(),
                DiagnosticFormattedName = marshallerTypeSyntax.ToString(),
            };
            string newNativeTypeName = ReplacePlaceholderSyntaxWithUnmanagedTypeSyntax(marshallerData.NativeType.Syntax, marshalInfo, unmanagedElementType).ToFullString();
            ManagedTypeInfo nativeType = marshallerData.NativeType with
            {
                FullTypeName = newNativeTypeName,
                DiagnosticFormattedName = newNativeTypeName
            };

            ICustomTypeMarshallingStrategy marshallingStrategy;
            bool elementIsBlittable = elementMarshaller is BlittableMarshaller;

            if (marshallerData.HasState)
            {
                marshallingStrategy = new StatefulValueMarshalling(marshallerType, nativeType, marshallerData.Shape);
                if (marshallerData.Shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                {
                    // Check if the buffer element type is actually the unmanaged element type
                    TypeSyntax bufferElementTypeSyntax = marshallerData.BufferElementType.Syntax.IsEquivalentTo(marshalInfo.PlaceholderTypeParameter.Syntax)
                        ? unmanagedElementType
                        : marshallerData.BufferElementType.Syntax;
                    marshallingStrategy = new StatefulCallerAllocatedBufferMarshalling(marshallingStrategy, marshallerTypeSyntax, bufferElementTypeSyntax);
                }

                var freeStrategy = GetFreeStrategy(info, context);
                IElementsMarshallingCollectionSource collectionSource = new StatefulLinearCollectionSource();
                ElementsMarshalling elementsMarshalling = CreateElementsMarshalling(marshallerData, elementInfo, elementMarshaller, unmanagedElementType, collectionSource);

                if (freeStrategy == FreeStrategy.FreeOriginal)
                {
                    marshallingStrategy = new UnmanagedToManagedOwnershipTrackingStrategy(marshallingStrategy);
                }

                marshallingStrategy = new StatefulLinearCollectionMarshalling(marshallingStrategy, marshallerData.Shape, numElementsExpression, elementsMarshalling, freeStrategy != FreeStrategy.NoFree);

                if (freeStrategy == FreeStrategy.FreeOriginal)
                {
                    marshallingStrategy = new CleanupOwnedOriginalValueMarshalling(marshallingStrategy);
                }

                if (marshallerData.Shape.HasFlag(MarshallerShape.Free))
                {
                    marshallingStrategy = new StatefulFreeMarshalling(marshallingStrategy);
                }
            }
            else
            {
                marshallingStrategy = new StatelessLinearCollectionSpaceAllocator(marshallerTypeSyntax, nativeType, marshallerData.Shape, numElementsExpression);

                var freeStrategy = GetFreeStrategy(info, context);

                IElementsMarshallingCollectionSource collectionSource = new StatelessLinearCollectionSource(marshallerTypeSyntax);
                if (freeStrategy == FreeStrategy.FreeOriginal)
                {
                    marshallingStrategy = new UnmanagedToManagedOwnershipTrackingStrategy(marshallingStrategy);
                }

                ElementsMarshalling elementsMarshalling = CreateElementsMarshalling(marshallerData, elementInfo, elementMarshaller, unmanagedElementType, collectionSource);

                marshallingStrategy = new StatelessLinearCollectionMarshalling(marshallingStrategy, elementsMarshalling, nativeType, marshallerData.Shape, numElementsExpression, freeStrategy != FreeStrategy.NoFree);

                if (marshallerData.Shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                {
                    // Check if the buffer element type is actually the unmanaged element type
                    TypeSyntax bufferElementTypeSyntax = marshallerData.BufferElementType.Syntax.IsEquivalentTo(marshalInfo.PlaceholderTypeParameter.Syntax)
                        ? unmanagedElementType
                        : marshallerData.BufferElementType.Syntax;
                    marshallingStrategy = new StatelessCallerAllocatedBufferMarshalling(marshallingStrategy, marshallerTypeSyntax, bufferElementTypeSyntax, isLinearCollectionMarshalling: true);
                }

                if (freeStrategy != FreeStrategy.NoFree && marshallerData.Shape.HasFlag(MarshallerShape.Free))
                {
                    marshallingStrategy = new StatelessFreeMarshalling(marshallingStrategy, marshallerTypeSyntax);
                }

                if (freeStrategy == FreeStrategy.FreeOriginal)
                {
                    marshallingStrategy = new CleanupOwnedOriginalValueMarshalling(marshallingStrategy);
                }
            }

            ByValueMarshalKindSupportDescriptor byValueMarshalKindSupport;
            if (info.ManagedType is not SzArrayType)
            {
                byValueMarshalKindSupport = ByValueMarshalKindSupportDescriptor.Default;
            }
            else
            {
                // If we have an array, we will use the Array [In, Out] support descriptor
                byValueMarshalKindSupport = ByValueMarshalKindSupportDescriptor.ArrayParameter;
            }

            // Elements in the collection must be blittable to use the pinnable marshaller.
            bool isPinned = marshallerData.Shape.HasFlag(MarshallerShape.StatelessPinnableReference) && elementIsBlittable;
            IMarshallingGenerator marshallingGenerator = new CustomTypeMarshallingGenerator(marshallingStrategy, byValueMarshalKindSupport, isPinned);
            if (isPinned)
            {
                marshallingGenerator = new StaticPinnableManagedValueMarshaller(marshallingGenerator, marshallerTypeSyntax);
            }
            return ResolvedGenerator.Resolved(marshallingGenerator);
        }

        private enum FreeStrategy
        {
            /// <summary>
            /// Free the unmanaged value stored in the native identifier.
            /// </summary>
            FreeNative,
            /// <summary>
            /// Free the unmanaged value originally passed into the stub.
            /// </summary>
            FreeOriginal,
            /// <summary>
            /// Do not free the unmanaged value, we don't own it.
            /// </summary>
            NoFree
        }

        private static FreeStrategy GetFreeStrategy(TypePositionInfo info, StubCodeContext context)
        {
            // When marshalling from managed to unmanaged, we always own the value in the native identifier.
            if (context.Direction == MarshalDirection.ManagedToUnmanaged)
            {
                return FreeStrategy.FreeNative;
            }

            // When we're in a case where we don't have state across stages, the parent stub context that can track the state
            // will only call our Cleanup stage when we own the value in the native identifier.
            if (!context.AdditionalTemporaryStateLivesAcrossStages)
            {
                return FreeStrategy.FreeNative;
            }

            // In an unmanaged-to-managed stub where a value is passed by 'ref',
            // we own the original value once we replace it with the new value we're passing out to the caller.
            if (info.RefKind == RefKind.Ref)
            {
                return FreeStrategy.FreeOriginal;
            }

            // In an unmanaged-to-managed stub, we don't take ownership of the value when it isn't passed by 'ref'.
            return FreeStrategy.NoFree;
        }

        private static ElementsMarshalling CreateElementsMarshalling(CustomTypeMarshallerData marshallerData, TypePositionInfo elementInfo, IMarshallingGenerator elementMarshaller, TypeSyntax unmanagedElementType, IElementsMarshallingCollectionSource collectionSource)
        {
            ElementsMarshalling elementsMarshalling;

            bool elementIsBlittable = elementMarshaller is BlittableMarshaller;
            if (elementIsBlittable)
            {
                elementsMarshalling = new BlittableElementsMarshalling(marshallerData.CollectionElementType.Syntax, unmanagedElementType, collectionSource);
            }
            else
            {
                elementsMarshalling = new NonBlittableElementsMarshalling(unmanagedElementType, elementMarshaller, elementInfo, collectionSource);
            }

            return elementsMarshalling;
        }

        private static bool ElementTypeIsSometimesNonBlittable(TypePositionInfo elementInfo)
        {
            if (elementInfo.MarshallingAttributeInfo is NoMarshallingInfo
                || elementInfo.MarshallingAttributeInfo is UnmanagedBlittableMarshallingInfo { IsStrictlyBlittable: true })
            {
                return false;
            }
            return true;
        }

        private static TypeSyntax ReplacePlaceholderSyntaxWithUnmanagedTypeSyntax(
            TypeSyntax originalTypeSyntax,
            NativeLinearCollectionMarshallingInfo marshalInfo,
            TypeSyntax unmanagedElementType)
            => originalTypeSyntax.ReplaceNodes(
                    originalTypeSyntax.DescendantNodesAndSelf().OfType<TypeSyntax>().Where(t => t.IsEquivalentTo(marshalInfo.PlaceholderTypeParameter.Syntax)),
                    (_, _) => unmanagedElementType);

        private GeneratorDiagnostic.NotSupported? ValidateCustomNativeTypeMarshallingSupported(TypePositionInfo info, StubCodeContext context, NativeMarshallingAttributeInfo marshalInfo)
        {
            MarshalDirection elementDirection = MarshallerHelpers.GetMarshalDirection(info, context);
            // Marshalling out or return parameter, but no out marshaller is specified
            if (elementDirection == MarshalDirection.UnmanagedToManaged
                && !marshalInfo.Marshallers.IsDefinedOrDefault(Options.UnmanagedToManagedMode))
            {
                return new(info, context)
                {
                    NotSupportedDetails = SR.Format(SR.UnmanagedToManagedMissingRequiredMarshaller, marshalInfo.EntryPointType.FullTypeName)
                };
            }

            // Marshalling ref parameter, but no ref marshaller is specified
            if (elementDirection == MarshalDirection.Bidirectional
                && !marshalInfo.Marshallers.IsDefinedOrDefault(Options.BidirectionalMode))
            {
                return new(info, context)
                {
                    NotSupportedDetails = SR.Format(SR.BidirectionalMissingRequiredMarshaller, marshalInfo.EntryPointType.FullTypeName)
                };
            }

            // Marshalling in parameter, but no in marshaller is specified
            if (elementDirection == MarshalDirection.ManagedToUnmanaged
                && !marshalInfo.Marshallers.IsDefinedOrDefault(Options.ManagedToUnmanagedMode))
            {
                return new(info, context)
                {
                    NotSupportedDetails = SR.Format(SR.ManagedToUnmanagedMissingRequiredMarshaller, marshalInfo.EntryPointType.FullTypeName)
                };
            }

            return null;
        }

        private static RefKind CreateElementRefKind(RefKind refKind, ByValueContentsMarshalKind byValueContentsMarshalKind)
        {
            if (refKind == RefKind.None)
            {
                return byValueContentsMarshalKind.GetRefKindForByValueContentsKind();
            }
            return refKind;
        }
    }
}
