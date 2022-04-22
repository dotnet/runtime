// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public readonly record struct AttributedMarshallingModelOptions(bool RuntimeMarshallingDisabled);

    public class AttributedMarshallingModelGeneratorFactory : IMarshallingGeneratorFactory
    {
        private static readonly ImmutableDictionary<string, string> AddDisableRuntimeMarshallingAttributeProperties =
            ImmutableDictionary<string, string>.Empty.Add(GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute, GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute);

        private static readonly BlittableMarshaller s_blittable = new BlittableMarshaller();
        private static readonly Forwarder s_forwarder = new Forwarder();

        private readonly IMarshallingGeneratorFactory _innerMarshallingGenerator;
        private readonly IMarshallingGeneratorFactory _elementMarshallingGenerator;

        public AttributedMarshallingModelGeneratorFactory(
            IMarshallingGeneratorFactory innerMarshallingGenerator,
            AttributedMarshallingModelOptions options)
        {
            Options = options;
            _innerMarshallingGenerator = innerMarshallingGenerator;
            // Unless overridden, default to using this generator factory for creating generators for collection elements.
            _elementMarshallingGenerator = this;
        }

        public AttributedMarshallingModelGeneratorFactory(
            IMarshallingGeneratorFactory innerMarshallingGenerator,
            IMarshallingGeneratorFactory elementMarshallingGenerator,
            AttributedMarshallingModelOptions options)
        {
            Options = options;
            _innerMarshallingGenerator = innerMarshallingGenerator;

            _elementMarshallingGenerator = elementMarshallingGenerator;
        }

        private AttributedMarshallingModelOptions Options { get; }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            return info.MarshallingAttributeInfo switch
            {
                NativeMarshallingAttributeInfo marshalInfo when Options.RuntimeMarshallingDisabled => CreateCustomNativeTypeMarshaller(info, context, marshalInfo),
                NativeMarshallingAttributeInfo { NativeValueType: SpecialTypeInfo specialType } marshalInfo when specialType.SpecialType.IsAlwaysBlittable() => CreateCustomNativeTypeMarshaller(info, context, marshalInfo),
                NativeMarshallingAttributeInfo { NativeValueType: PointerTypeInfo } marshalInfo => CreateCustomNativeTypeMarshaller(info, context, marshalInfo),
                UnmanagedBlittableMarshallingInfo when Options.RuntimeMarshallingDisabled => s_blittable,
                UnmanagedBlittableMarshallingInfo or NativeMarshallingAttributeInfo when !Options.RuntimeMarshallingDisabled =>
                    throw new MarshallingNotSupportedException(info, context)
                    {
                        NotSupportedDetails = SR.RuntimeMarshallingMustBeDisabled,
                        DiagnosticProperties = AddDisableRuntimeMarshallingAttributeProperties
                    },
                GeneratedNativeMarshallingAttributeInfo => s_forwarder,
                MissingSupportMarshallingInfo => s_forwarder,
                _ => _innerMarshallingGenerator.Create(info, context)
            };
        }

        private static ExpressionSyntax GetNumElementsExpressionFromMarshallingInfo(TypePositionInfo info, CountInfo count, StubCodeContext context)
        {
            return count switch
            {
                SizeAndParamIndexInfo(int size, SizeAndParamIndexInfo.UnspecifiedParam) => GetConstSizeExpression(size),
                ConstSizeCountInfo(int size) => GetConstSizeExpression(size),
                SizeAndParamIndexInfo(SizeAndParamIndexInfo.UnspecifiedConstSize, TypePositionInfo param) => CheckedExpression(SyntaxKind.CheckedExpression, GetExpressionForParam(param)),
                SizeAndParamIndexInfo(int size, TypePositionInfo param) => CheckedExpression(SyntaxKind.CheckedExpression, BinaryExpression(SyntaxKind.AddExpression, GetConstSizeExpression(size), GetExpressionForParam(param))),
                CountElementCountInfo(TypePositionInfo elementInfo) => CheckedExpression(SyntaxKind.CheckedExpression, GetExpressionForParam(elementInfo)),
                _ => throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = SR.ArraySizeMustBeSpecified
                },
            };

            static LiteralExpressionSyntax GetConstSizeExpression(int size)
            {
                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(size));
            }

            ExpressionSyntax GetExpressionForParam(TypePositionInfo paramInfo)
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
                        type = collectionInfo.ElementType;
                        marshallingInfo = collectionInfo.ElementMarshallingInfo;
                    }
                    else
                    {
                        throw new MarshallingNotSupportedException(info, context)
                        {
                            NotSupportedDetails = SR.CollectionSizeParamTypeMustBeIntegral
                        };
                    }
                }

                if (type is not SpecialTypeInfo specialType || !specialType.SpecialType.IsIntegralType())
                {
                    throw new MarshallingNotSupportedException(info, context)
                    {
                        NotSupportedDetails = SR.CollectionSizeParamTypeMustBeIntegral
                    };
                }

                return CastExpression(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        ParenthesizedExpression(numElementsExpression));
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

        private IMarshallingGenerator CreateCustomNativeTypeMarshaller(TypePositionInfo info, StubCodeContext context, NativeMarshallingAttributeInfo marshalInfo)
        {
            ValidateCustomNativeTypeMarshallingSupported(info, context, marshalInfo);

            ICustomNativeTypeMarshallingStrategy marshallingStrategy = new SimpleCustomNativeTypeMarshalling(marshalInfo.NativeMarshallingType.Syntax);

            if ((marshalInfo.MarshallingFeatures & CustomTypeMarshallerFeatures.CallerAllocatedBuffer) != 0)
            {
                if (marshalInfo.BufferSize is null)
                {
                    throw new MarshallingNotSupportedException(info, context);
                }
                marshallingStrategy = new StackallocOptimizationMarshalling(marshallingStrategy, marshalInfo.BufferElementType.Syntax, marshalInfo.BufferSize.Value);
            }

            if ((marshalInfo.MarshallingFeatures & CustomTypeMarshallerFeatures.UnmanagedResources) != 0)
            {
                marshallingStrategy = new FreeNativeCleanupStrategy(marshallingStrategy);
            }

            // Collections have extra configuration, so handle them here.
            if (marshalInfo is NativeLinearCollectionMarshallingInfo collectionMarshallingInfo)
            {
                return CreateNativeCollectionMarshaller(info, context, collectionMarshallingInfo, marshallingStrategy);
            }

            if (marshalInfo.NativeValueType is not null)
            {
                marshallingStrategy = DecorateWithTwoStageMarshallingStrategy(marshalInfo, marshallingStrategy);
            }

            IMarshallingGenerator marshallingGenerator = new CustomNativeTypeMarshallingGenerator(marshallingStrategy, enableByValueContentsMarshalling: false);

            if (marshalInfo.PinningFeatures.HasFlag(CustomTypeMarshallerPinning.ManagedType))
            {
                return new PinnableManagedValueMarshaller(marshallingGenerator);
            }

            return marshallingGenerator;
        }

        private static void ValidateCustomNativeTypeMarshallingSupported(TypePositionInfo info, StubCodeContext context, NativeMarshallingAttributeInfo marshalInfo)
        {
            // The marshalling method for this type doesn't support marshalling from native to managed,
            // but our scenario requires marshalling from native to managed.
            if ((info.RefKind == RefKind.Ref || info.RefKind == RefKind.Out || info.IsManagedReturnPosition)
                && !marshalInfo.Direction.HasFlag(CustomTypeMarshallerDirection.Out))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(SR.CustomTypeMarshallingNativeToManagedUnsupported, marshalInfo.NativeMarshallingType.FullTypeName)
                };
            }
            // The marshalling method for this type doesn't support marshalling from managed to native by value,
            // but our scenario requires marshalling from managed to native by value.
            else if (!info.IsByRef
                && context.SingleFrameSpansNativeContext
                && !(marshalInfo.PinningFeatures.HasFlag(CustomTypeMarshallerPinning.ManagedType)
                    || marshalInfo.MarshallingFeatures.HasFlag(CustomTypeMarshallerFeatures.CallerAllocatedBuffer)
                    || marshalInfo.Direction.HasFlag(CustomTypeMarshallerDirection.In)))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(SR.CustomTypeMarshallingManagedToNativeUnsupported, marshalInfo.NativeMarshallingType.FullTypeName)
                };
            }
            // The marshalling method for this type doesn't support marshalling from managed to native by reference,
            // but our scenario requires marshalling from managed to native by reference.
            // "in" byref supports stack marshalling.
            else if (info.RefKind == RefKind.In
                && !(context.SingleFrameSpansNativeContext && marshalInfo.MarshallingFeatures.HasFlag(CustomTypeMarshallerFeatures.CallerAllocatedBuffer))
                && !marshalInfo.Direction.HasFlag(CustomTypeMarshallerDirection.In))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(SR.CustomTypeMarshallingManagedToNativeUnsupported, marshalInfo.NativeMarshallingType.FullTypeName)
                };
            }
            // The marshalling method for this type doesn't support marshalling from managed to native by reference,
            // but our scenario requires marshalling from managed to native by reference.
            // "ref" byref marshalling doesn't support stack marshalling
            // The "Out" direction for "ref" was checked above
            else if (info.RefKind == RefKind.Ref
                && !marshalInfo.Direction.HasFlag(CustomTypeMarshallerDirection.In))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(SR.CustomTypeMarshallingManagedToNativeUnsupported, marshalInfo.NativeMarshallingType.FullTypeName)
                };
            }
        }

        private static ICustomNativeTypeMarshallingStrategy DecorateWithTwoStageMarshallingStrategy(NativeMarshallingAttributeInfo marshalInfo, ICustomNativeTypeMarshallingStrategy nativeTypeMarshaller)
        {
            TypeSyntax valuePropertyTypeSyntax = marshalInfo.NativeValueType!.Syntax;

            if (marshalInfo.PinningFeatures.HasFlag(CustomTypeMarshallerPinning.NativeType) && marshalInfo.MarshallingFeatures.HasFlag(CustomTypeMarshallerFeatures.TwoStageMarshalling))
            {
                return new PinnableMarshallerTypeMarshalling(nativeTypeMarshaller, valuePropertyTypeSyntax);
            }

            return new CustomNativeTypeWithToFromNativeValueMarshalling(nativeTypeMarshaller, valuePropertyTypeSyntax);
        }

        private IMarshallingGenerator CreateNativeCollectionMarshaller(
            TypePositionInfo info,
            StubCodeContext context,
            NativeLinearCollectionMarshallingInfo collectionInfo,
            ICustomNativeTypeMarshallingStrategy marshallingStrategy)
        {
            var elementInfo = new TypePositionInfo(collectionInfo.ElementType, collectionInfo.ElementMarshallingInfo) { ManagedIndex = info.ManagedIndex };
            IMarshallingGenerator elementMarshaller = _elementMarshallingGenerator.Create(
                elementInfo,
                new LinearCollectionElementMarshallingCodeContext(StubCodeContext.Stage.Setup, string.Empty, string.Empty, context));
            TypeSyntax elementType = elementMarshaller.AsNativeType(elementInfo);


            ExpressionSyntax numElementsExpression = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0));
            if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
            {
                // In this case, we need a numElementsExpression supplied from metadata, so we'll calculate it here.
                numElementsExpression = GetNumElementsExpressionFromMarshallingInfo(info, collectionInfo.ElementCountInfo, context);
            }

            bool isBlittable = elementMarshaller is BlittableMarshaller;

            if (isBlittable)
            {
                marshallingStrategy = new LinearCollectionWithBlittableElementsMarshalling(marshallingStrategy, collectionInfo.ElementType.Syntax, numElementsExpression);
            }
            else
            {
                marshallingStrategy = new LinearCollectionWithNonBlittableElementsMarshalling(marshallingStrategy, elementMarshaller, elementInfo, numElementsExpression);
            }

            // Explicitly insert the Value property handling here (before numElements handling) so that the numElements handling will be emitted before the Value property handling in unmarshalling.
            if (collectionInfo.NativeValueType is not null)
            {
                marshallingStrategy = DecorateWithTwoStageMarshallingStrategy(collectionInfo, marshallingStrategy);
            }

            marshallingStrategy = new SizeOfElementMarshalling(
                marshallingStrategy,
                SizeOfExpression(elementType));

            if (collectionInfo.UseDefaultMarshalling && info.ManagedType is SzArrayType)
            {
                return new ArrayMarshaller(
                    new CustomNativeTypeMarshallingGenerator(marshallingStrategy, enableByValueContentsMarshalling: true),
                    elementType,
                    isBlittable);
            }

            IMarshallingGenerator marshallingGenerator = new CustomNativeTypeMarshallingGenerator(marshallingStrategy, enableByValueContentsMarshalling: false);

            if (collectionInfo.PinningFeatures.HasFlag(CustomTypeMarshallerPinning.ManagedType))
            {
                return new PinnableManagedValueMarshaller(marshallingGenerator);
            }

            return marshallingGenerator;
        }
    }
}
