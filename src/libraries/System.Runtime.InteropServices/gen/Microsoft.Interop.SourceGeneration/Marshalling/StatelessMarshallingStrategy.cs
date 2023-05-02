﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;

        public StatelessValueMarshalling(TypeSyntax marshallerTypeSyntax, ManagedTypeInfo unmanagedType, MarshallerShape shape)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _unmanagedType = unmanagedType;
            _shape = shape;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _unmanagedType;
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

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <marshallerType>.ConvertToUnmanaged(<managedIdentifier>)
            ExpressionSyntax convertToUnmanaged = InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifier)))));

            if (_unmanagedType == SpecialTypeInfo.Void)
            {
                // To support our exception marshalling scenarios, we allow a "marshal to void" marshaller.
                // In this case, we don't assign the result to the native identifier because there is no native identifier.
                // <convertToUnmanaged>
                yield return ExpressionStatement(convertToUnmanaged);
                yield break;
            }

            // <nativeIdentifier> = <convertToUnmanaged>;
            var assignment = AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    convertToUnmanaged);


            if (_unmanagedType is PointerTypeInfo pointer)
            {
                var rewriter = new PointerNativeTypeAssignmentRewriter(assignment.Right.ToString(), (PointerTypeSyntax)pointer.Syntax);
                assignment = (AssignmentExpressionSyntax)rewriter.Visit(assignment);
            }
            yield return ExpressionStatement(assignment);
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
        private readonly bool _isLinearCollectionMarshalling;

        public StatelessCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType, TypeSyntax bufferElementType, bool isLinearCollectionMarshalling)
        {
            _innerMarshaller = innerMarshaller;
            _marshallerType = marshallerType;
            _bufferElementType = bufferElementType;
            _isLinearCollectionMarshalling = isLinearCollectionMarshalling;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);
        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateCleanupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (MarshallerHelpers.CanUseCallerAllocatedBuffer(info, context))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return _innerMarshaller.GenerateMarshalStatements(info, context);

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

                if (_isLinearCollectionMarshalling)
                {
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

                    // <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, <bufferIdentifier>, out <numElements>);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(nativeIdentifier),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    _marshallerType,
                                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements)),
                                ArgumentList(SeparatedList(new ArgumentSyntax[]
                                {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(IdentifierName(bufferIdentifier)),
                                Argument(IdentifierName(numElementsIdentifier))
                                    .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
                                })))));

                    // Linear collections have additional marshalling required using the inner marshaller
                    foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context))
                    {
                        yield return statement;
                    }
                }
                else
                {
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
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);

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

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, context))
            {
                yield return statement;
            }
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
        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);
    }

    /// <summary>
    /// Marshaller that enables support for marshalling blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionBlittableElementsMarshalling : BlittableElementsMarshalling, ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatelessLinearCollectionBlittableElementsMarshalling(TypeSyntax marshallerTypeSyntax, ManagedTypeInfo unmanagedType, MarshallerShape shape, TypeSyntax managedElementType, TypeSyntax unmanagedElementType, ExpressionSyntax numElementsExpression)
            : base(managedElementType, unmanagedElementType)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _unmanagedType = unmanagedType;
            _shape = shape;
            _numElementsExpression = numElementsExpression;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _unmanagedType;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    _numElementsExpression));

            // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElementsFinally(<nativeIdentifier>, <numElements>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElementsFinally)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(nativeIdentifier)),
                            Argument(IdentifierName(numElementsIdentifier))
                        })))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            if (_shape.HasFlag(MarshallerShape.ToUnmanaged)
                && !(_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer)
                    && MarshallerHelpers.CanUseCallerAllocatedBuffer(info, context)))
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

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
            }

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                yield return GenerateByValueOutMarshalStatement(info, context);
            }
            else
            {
                yield return GenerateMarshalStatement(info, context);
            }
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(numElementsIdentifier))));
            // Use the numElements local to ensure the compiler doesn't give errors for using an uninitialized variable.
            // The value will never be used unless it has been initialized, so this is safe.
            yield return MarshallerHelpers.SkipInitOrDefaultInit(
                new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
                {
                    InstanceIdentifier = numElementsIdentifier
                }, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield return GenerateByValueOutUnmarshalStatement(info, context);
                yield break;
            }

            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

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

            yield return GenerateUnmarshalStatement(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        protected override InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
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

        protected override InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            // <marshallerType>.GetManagedValuesSource(<managedIdentifier>)
            string managedIdentifier = context.GetIdentifiers(info).managed;
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    _marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(managedIdentifier)))));
        }

        protected override InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
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

        protected override InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context)
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
    }

    /// <summary>
    /// Marshaller that enables support for marshalling non-blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionNonBlittableElementsMarshalling : NonBlittableElementsMarshalling, ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatelessLinearCollectionNonBlittableElementsMarshalling(
            TypeSyntax marshallerTypeSyntax,
            ManagedTypeInfo unmanagedType,
            MarshallerShape shape,
            TypeSyntax unmanagedElementType,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo,
            ExpressionSyntax numElementsExpression)
            : base(unmanagedElementType, elementMarshaller, elementInfo)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _unmanagedType = unmanagedType;
            _shape = shape;
            _numElementsExpression = numElementsExpression;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _unmanagedType;

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            StatementSyntax elementCleanup = GenerateElementCleanupStatement(info, context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                yield return elementCleanup;
            }
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    _numElementsExpression));

            // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElementsFinally(<nativeIdentifier>, <numElements>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElementsFinally)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(nativeIdentifier)),
                            Argument(IdentifierName(numElementsIdentifier))
                        })))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            if (_shape.HasFlag(MarshallerShape.ToUnmanaged)
                && !(_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer) && MarshallerHelpers.CanUseCallerAllocatedBuffer(info, context)))
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

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
            }

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                yield return GenerateByValueOutMarshalStatement(info, context);
            }
            else
            {
                yield return GenerateMarshalStatement(info, context);
            }
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(numElementsIdentifier))));
            // Use the numElements local to ensure the compiler doesn't give errors for using an uninitialized variable.
            // The value will never be used unless it has been initialized, so this is safe.
            yield return MarshallerHelpers.SkipInitOrDefaultInit(
                new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
                {
                    InstanceIdentifier = numElementsIdentifier
                }, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // <numElements> = <GetManagedValuesSource.Length;
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(numElementsIdentifier),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GetManagedValuesSource(info, context),
                            IdentifierName("Length"))));
                yield return GenerateByValueOutUnmarshalStatement(info, context);
            }

            if (!_shape.HasFlag(MarshallerShape.ToManaged))
            {
                yield break;
            }
            else
            {
                // <numElements> = <numElementsExpression>;
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

                // ReadOnlySpan<TUnmanagedElement> <nativeSpan> = <marshallerType>.GetUnmanagedValuesSource(<nativeIdentifier>, <numElements>)
                // Span<T> <managedSpan> = <marshallerType>.GetManagedValuesDestination(<managedIdentifier>)
                // << unmarshal contents >>
                yield return GenerateUnmarshalStatement(info, context);
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        protected override InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
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

        protected override InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            // <marshallerType>.GetManagedValuesSource(<managedIdentifier>)
            string managedIdentifier = context.GetIdentifiers(info).managed;
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    _marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(managedIdentifier)))));
        }

        protected override InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
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

        protected override InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context)
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
    }
}
