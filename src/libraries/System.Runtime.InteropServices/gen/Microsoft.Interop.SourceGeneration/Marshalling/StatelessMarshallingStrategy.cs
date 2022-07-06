// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Stateless marshalling support for a type that has a custom unmanaged type.
    /// </summary>
    internal sealed class StatelessValueMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly MarshallerShape _shape;

        public StatelessValueMarshalling(TypeSyntax marshallerTypeSyntax, TypeSyntax nativeTypeSyntax, MarshallerShape shape)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _nativeTypeSyntax = nativeTypeSyntax;
            _shape = shape;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshallerType>.ConvertToManagedFinally(<nativeIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToManagedFinally)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nativeIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <nativeIdentifier> = <marshallerType>.ConvertToUnmanaged(<managedIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshallerType>.ConvertToManaged(<nativeIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToManaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nativeIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<ArgumentSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StatelessCallerAllocatedBufferMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _marshallerType;
        private readonly TypeSyntax _bufferElementType;

        public StatelessCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType, TypeSyntax bufferElementType)
        {
            _innerMarshaller = innerMarshaller;
            _marshallerType = marshallerType;
            _bufferElementType = bufferElementType;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);
        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateCleanupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (CanUseCallerAllocatedBuffer(info, context))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);

            IEnumerable<StatementSyntax> GenerateCallerAllocatedBufferMarshalStatements()
            {
                string bufferIdentifier = context.GetAdditionalIdentifier(info, "buffer");

                // Span<bufferElementType> <bufferIdentifier> = stackalloc <bufferElementType>[<marshallerType>.BufferSize];
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        GenericName(
                            Identifier(TypeNames.System_Span),
                            TypeArgumentList(
                                SingletonSeparatedList(_bufferElementType))),
                        SingletonSeparatedList(
                            VariableDeclarator(bufferIdentifier)
                                .WithInitializer(EqualsValueClause(
                                    StackAllocArrayCreationExpression(
                                        ArrayType(
                                            _bufferElementType,
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize))
                                            ))))))))));

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

                // <nativeIdentifier> = <marshallerType>.ConvertToUnmanaged(<managedIdentifier>, <nativeIdentifier>__buffer);
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(nativeIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                _marshallerType,
                                IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(IdentifierName(bufferIdentifier))
                            })))));
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);

        private static bool CanUseCallerAllocatedBuffer(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && (!info.IsByRef || info.RefKind == RefKind.In);
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
    }

    internal sealed class StatelessFreeMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _marshallerType;

        public StatelessFreeMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType)
        {
            _innerMarshaller = innerMarshaller;
            _marshallerType = marshallerType;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            // <marshallerType>.Free(<nativeIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerType,
                        IdentifierName(ShapeMemberNames.Free)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(context.GetIdentifiers(info).native))))));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments) => _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);
        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);
    }

    /// <summary>
    /// Marshaller that enables support for marshalling blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionBlittableElementsMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly MarshallerShape _shape;
        private readonly TypeSyntax _managedElementType;
        private readonly TypeSyntax _unmanagedElementType;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatelessLinearCollectionBlittableElementsMarshalling(TypeSyntax marshallerTypeSyntax, TypeSyntax nativeTypeSyntax, MarshallerShape shape, TypeSyntax managedElementType, TypeSyntax unmanagedElementType, ExpressionSyntax numElementsExpression)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _nativeTypeSyntax = nativeTypeSyntax;
            _shape = shape;
            _managedElementType = managedElementType;
            _unmanagedElementType = unmanagedElementType;
            _numElementsExpression = numElementsExpression;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            // <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, out <numElements>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(managedIdentifier)),
                            Argument(IdentifierName(numElementsIdentifier))
                                .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
                        })))));

            // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
            ExpressionSyntax destination =
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination)),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier)),
                    })));

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>).Clear();
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            destination,
                            IdentifierName("Clear"))));
                yield break;
            }

            // Skip the cast if the managed and unmanaged element types are the same
            if (!_unmanagedElementType.IsEquivalentTo(_managedElementType))
            {
                // MemoryMarshal.Cast<<unmanagedElementType>, <managedElementType>>(<destination>)
                destination = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                        GenericName(
                            Identifier("Cast"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SeparatedList(
                                    new[]
                                    {
                                        _unmanagedElementType,
                                        _managedElementType
                                    })))),
                    ArgumentList(SingletonSeparatedList(
                        Argument(destination))));
            }

            // <marshallerType>.GetManagedValuesSource(<managedIdentifer>).CopyTo(<destination>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                _marshallerTypeSyntax,
                                IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifier))))),
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(destination)));
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(GetNumElementsIdentifier(info, context)))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            ExpressionSyntax copySource;
            ExpressionSyntax copyDestination;
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
                copySource =
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier)),
                        })));

                // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<marshallerType>.GetManagedValuesSource(<managedIdentifer>)), <marshallerType>.GetManagedValuesSource(<managedIdentifer>).Length)
                copyDestination = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                        IdentifierName("CreateSpan")),
                    ArgumentList(
                        SeparatedList(new[]
                        {
                            Argument(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ParseName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                                        IdentifierName("GetReference")),
                                    ArgumentList(SingletonSeparatedList(
                                        Argument(
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerTypeSyntax,
                                                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                                                ArgumentList(SingletonSeparatedList(
                                                    Argument(IdentifierName(managedIdentifier))))))))))
                                .WithRefKindKeyword(
                                    Token(SyntaxKind.RefKeyword)),
                            Argument(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            _marshallerTypeSyntax,
                                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName(managedIdentifier))))),
                                    IdentifierName("Length")))
                        })));

            }
            else
            {
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(numElementsIdentifier),
                        _numElementsExpression));

                // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElements(<nativeIdentifier>, <numElements>);
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(managedIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                _marshallerTypeSyntax,
                                IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElements)),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName(nativeIdentifier)),
                                Argument(IdentifierName(numElementsIdentifier))
                            })))));

                // <marshallerType>.GetUnmanagedValuesSource(<nativeIdentifier>, <numElements>)
                copySource = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesSource)),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier))
                    })));

                // <marshallerType>.GetManagedValuesDestination(<managedIdentifier>)
                copyDestination = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination)),
                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(managedIdentifier)))));
            }

            // Skip the cast if the managed and unmanaged element types are the same
            if (!_unmanagedElementType.IsEquivalentTo(_managedElementType))
            {
                // MemoryMarshal.Cast<<unmanagedElementType>, <elementType>>(<copySource>)
                copySource = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                        GenericName(
                            Identifier("Cast"),
                            TypeArgumentList(SeparatedList(
                                new[]
                                {
                                    _unmanagedElementType,
                                    _managedElementType
                                })))),
                    ArgumentList(SingletonSeparatedList(
                        Argument(copySource))));
            }

            // <copySource>.CopyTo(<copyDestination>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        copySource,
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(copyDestination)));
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context) => Array.Empty<ArgumentSyntax>();

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        private static string GetNumElementsIdentifier(TypePositionInfo info, StubCodeContext context)
            => context.GetAdditionalIdentifier(info, "numElements");
    }

    /// <summary>
    /// Marshaller that enables support for marshalling non-blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionNonBlittableElementsMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly MarshallerShape _shape;
        private readonly TypeSyntax _unmanagedElementType;
        private readonly IMarshallingGenerator _elementMarshaller;
        private readonly TypePositionInfo _elementInfo;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatelessLinearCollectionNonBlittableElementsMarshalling(
            TypeSyntax marshallerTypeSyntax,
            TypeSyntax nativeTypeSyntax,
            MarshallerShape shape,
            TypeSyntax unmanagedElementType,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo,
            ExpressionSyntax numElementsExpression)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _nativeTypeSyntax = nativeTypeSyntax;
            _shape = shape;
            _unmanagedElementType = unmanagedElementType;
            _elementMarshaller = elementMarshaller;
            _elementInfo = elementInfo;
            _numElementsExpression = numElementsExpression;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info) => _nativeTypeSyntax;

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            // <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, out <numElements>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(managedIdentifier)),
                            Argument(IdentifierName(numElementsIdentifier))
                                .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
                        })))));

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>).Clear();
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GetUnmanagedValuesDestination(info, context),
                            IdentifierName("Clear"))));
                yield break;
            }

            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);

            // ReadOnlySpan<T> <managedSpan> = <marshallerType>.GetManagedValuesSource(<managedIdentifer>)
            // Span<TUnmanagedElement> <nativeSpan> = <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
            // << marshal contents >>
            yield return Block(
                LocalDeclarationStatement(VariableDeclaration(
                    GenericName(
                        Identifier(TypeNames.System_ReadOnlySpan),
                        TypeArgumentList(SingletonSeparatedList(_elementInfo.ManagedType.Syntax))),
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier(managedSpanIdentifier))
                        .WithInitializer(EqualsValueClause(
                            GetManagedValuesSource(info, context)))))),
                LocalDeclarationStatement(VariableDeclaration(
                    GenericName(
                        Identifier(TypeNames.System_Span),
                        TypeArgumentList(SingletonSeparatedList(_unmanagedElementType))),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(nativeSpanIdentifier))
                        .WithInitializer(EqualsValueClause(
                            GetUnmanagedValuesDestination(info, context)))))),
                GenerateContentsMarshallingStatement(info, context,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(MarshallerHelpers.GetManagedSpanIdentifier(info, context)),
                        IdentifierName("Length")),
                    StubCodeContext.Stage.Marshal));
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(GetNumElementsIdentifier(info, context)))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield return GenerateByValueUnmarshalStatement(info, context);
            }
            else
            {
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(numElementsIdentifier),
                        _numElementsExpression));

                // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElements(<nativeIdentifier>, <numElements>);
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(managedIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                _marshallerTypeSyntax,
                                IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElements)),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName(nativeIdentifier)),
                                Argument(IdentifierName(numElementsIdentifier))
                            })))));

                string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
                string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);

                // ReadOnlySpan<TUnmanagedElement> <nativeSpan> = <marshallerType>.GetUnmanagedValuesSource(<nativeIdentifier>, <numElements>)
                // Span<T> <managedSpan> = <marshallerType>.GetManagedValuesDestination(<managedIdentifier>)
                // << unmarshal contents >>
                yield return Block(
                    LocalDeclarationStatement(VariableDeclaration(
                        GenericName(
                            Identifier(TypeNames.System_ReadOnlySpan),
                            TypeArgumentList(SingletonSeparatedList(_unmanagedElementType))),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(nativeSpanIdentifier))
                            .WithInitializer(EqualsValueClause(
                                GetUnmanagedValuesSource(info, context)))))),
                    LocalDeclarationStatement(VariableDeclaration(
                        GenericName(
                            Identifier(TypeNames.System_Span),
                            TypeArgumentList(SingletonSeparatedList(_elementInfo.ManagedType.Syntax))),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(managedSpanIdentifier))
                            .WithInitializer(EqualsValueClause(
                                GetManagedValuesDestination(info, context)))))),
                    GenerateContentsMarshallingStatement(info, context,
                        IdentifierName(numElementsIdentifier),
                        StubCodeContext.Stage.UnmarshalCapture,
                        StubCodeContext.Stage.Unmarshal));
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context) => Array.Empty<ArgumentSyntax>();

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        private static string GetNumElementsIdentifier(TypePositionInfo info, StubCodeContext context)
            => context.GetAdditionalIdentifier(info, "numElements");

        private InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);
            string nativeIdentifier = context.GetIdentifiers(info).native;

            // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    _marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination)),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(IdentifierName(nativeIdentifier)),
                    Argument(IdentifierName(numElementsIdentifier)),
                })));
        }

        private InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            // <marshallerType>.GetManagedValuesSource(<managedIdentifer>)
            string managedIdentifier = context.GetIdentifiers(info).managed;
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    _marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(managedIdentifier)))));
        }

        private InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);
            string nativeIdentifier = context.GetIdentifiers(info).native;

            // <marshallerType>.GetUnmanagedValuesSource(<nativeIdentifier>, <numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    _marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesSource)),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier))
                })));
        }

        private InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string managedIdentifier = context.GetIdentifiers(info).managed;

            // <marshallerType>.GetManagedValuesDestination(<managedIdentifier>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    _marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination)),
                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(managedIdentifier)))));
        }

        private StatementSyntax GenerateContentsMarshallingStatement(TypePositionInfo info, StubCodeContext context, ExpressionSyntax lengthExpression, params StubCodeContext.Stage[] stagesToGeneratePerElement)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            var elementSetupSubContext = new LinearCollectionElementMarshallingCodeContext(
                StubCodeContext.Stage.Setup,
                managedSpanIdentifier,
                nativeSpanIdentifier,
                context);

            TypePositionInfo localElementInfo = _elementInfo with
            {
                InstanceIdentifier = info.InstanceIdentifier,
                RefKind = info.IsByRef ? info.RefKind : info.ByValueContentsMarshalKind.GetRefKindForByValueContentsKind(),
                ManagedIndex = info.ManagedIndex,
                NativeIndex = info.NativeIndex
            };

            List<StatementSyntax> elementStatements = new();
            foreach (var stage in stagesToGeneratePerElement)
            {
                var elementSubContext = elementSetupSubContext with { CurrentStage = stage };
                elementStatements.AddRange(_elementMarshaller.Generate(localElementInfo, elementSubContext));
            }

            if (elementStatements.Any())
            {
                StatementSyntax marshallingStatement = Block(
                    List(_elementMarshaller.Generate(localElementInfo, elementSetupSubContext)
                        .Concat(elementStatements)));

                if (_elementMarshaller.AsNativeType(_elementInfo) is PointerTypeSyntax elementNativeType)
                {
                    PointerNativeTypeAssignmentRewriter rewriter = new(elementSetupSubContext.GetIdentifiers(localElementInfo).native, elementNativeType);
                    marshallingStatement = (StatementSyntax)rewriter.Visit(marshallingStatement);
                }

                // Iterate through the elements of the native collection to marshal them
                return MarshallerHelpers.GetForLoop(lengthExpression, elementSetupSubContext.IndexerIdentifier)
                    .WithStatement(marshallingStatement);
            }

            return EmptyStatement();
        }

        private StatementSyntax GenerateByValueUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = context.GetAdditionalIdentifier(info, "numElements");

            // <numElements> = <marshallerType>.GetManagedValuesSource(<managedIdentifier>).Length;
            ExpressionStatementSyntax numElementsStatement = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        GetManagedValuesSource(info, context),
                        IdentifierName("Length"))));

            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);

            // Span<TElement> <managedSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <marshallerType>.GetManagedValuesSource(<managedIdentifier>).GetPinnableReference(), <numElements>));
            LocalDeclarationStatementSyntax managedValuesDeclaration = LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(
                        SingletonSeparatedList(_elementInfo.ManagedType.Syntax))
                ),
                SingletonSeparatedList(VariableDeclarator(managedSpanIdentifier).WithInitializer(EqualsValueClause(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ParseName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                            IdentifierName("CreateSpan")))
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    Argument(
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                ParseName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                                IdentifierName("AsRef")),
                                            ArgumentList(SingletonSeparatedList(
                                                Argument(
                                                    InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            GetManagedValuesSource(info, context),
                                                            IdentifierName("GetPinnableReference")),
                                                            ArgumentList()))
                                                .WithRefKindKeyword(
                                                    Token(SyntaxKind.InKeyword))))))
                                    .WithRefKindKeyword(
                                        Token(SyntaxKind.RefKeyword)),
                                    Argument(
                                        IdentifierName(numElementsIdentifier))
                                }))))))));

            // Span<TUnmanagedElement> <nativeSpan> = <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            LocalDeclarationStatementSyntax unmanagedValuesDeclaration = LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(SingletonSeparatedList(_unmanagedElementType))),
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(nativeSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                        GetUnmanagedValuesDestination(info, context))))));

            return Block(
                numElementsStatement,
                managedValuesDeclaration,
                unmanagedValuesDeclaration,
                GenerateContentsMarshallingStatement(info, context,
                    IdentifierName(numElementsIdentifier),
                    StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }

        /// <summary>
        /// Rewrite assignment expressions to the native identifier to cast to IntPtr.
        /// This handles the case where the native type of a non-blittable managed type is a pointer,
        /// which are unsupported in generic type parameters.
        /// </summary>
        private sealed class PointerNativeTypeAssignmentRewriter : CSharpSyntaxRewriter
        {
            private readonly string _nativeIdentifier;
            private readonly PointerTypeSyntax _nativeType;

            public PointerNativeTypeAssignmentRewriter(string nativeIdentifier, PointerTypeSyntax nativeType)
            {
                _nativeIdentifier = nativeIdentifier;
                _nativeType = nativeType;
            }

            public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (node.Left.ToString() == _nativeIdentifier)
                {
                    return node.WithRight(
                        CastExpression(MarshallerHelpers.SystemIntPtrType, node.Right));
                }
                if (node.Right.ToString() == _nativeIdentifier)
                {
                    return node.WithRight(CastExpression(_nativeType, node.Right));
                }

                return node;
            }

            public override SyntaxNode? VisitArgument(ArgumentSyntax node)
            {
                if (node.Expression.ToString() == _nativeIdentifier)
                {
                    return node.WithExpression(
                        CastExpression(_nativeType, node.Expression));
                }
                return node;
            }
        }
    }
}
