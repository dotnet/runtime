// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context)
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

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubIdentifierContext context)
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

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context)
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

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubIdentifierContext context)
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
        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (MarshallerHelpers.CanUseCallerAllocatedBuffer(info, context.CodeContext))
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

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
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

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, context.CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
                yield break;

            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(info, context))
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

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, context.CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
                yield break;

            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(info, context))
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

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);
    }

    /// <summary>
    /// Marshaller type that enables allocating space for marshalling a linear collection using a marshaller that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionSpaceAllocator : ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;
        private readonly CountInfo _countInfo;
        private readonly bool _countInfoRequiresCast;

        public StatelessLinearCollectionSpaceAllocator(TypeSyntax marshallerTypeSyntax, ManagedTypeInfo unmanagedType, MarshallerShape shape, CountInfo countInfo, bool countInfoRequiresCast)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _unmanagedType = unmanagedType;
            _shape = shape;
            _countInfo = countInfo;
            _countInfoRequiresCast = countInfoRequiresCast;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _unmanagedType;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, context.CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
                yield break;

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            // <numElements> = <numElementsExpression>;
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    ElementsMarshalling.GenerateNumElementsExpression(_countInfo, _countInfoRequiresCast, context)));
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, context.CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
                yield break;

            if (MarshallerHelpers.GetMarshalDirection(info, context.CodeContext) == MarshalDirection.ManagedToUnmanaged)
            {
                yield return EmptyStatement();
                yield break;
            }

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            // <numElements> = <numElementsExpression>;
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    ElementsMarshalling.GenerateNumElementsExpression(_countInfo, _countInfoRequiresCast, context)));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    ElementsMarshalling.GenerateNumElementsExpression(_countInfo, _countInfoRequiresCast, context)));

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

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            if (_shape.HasFlag(MarshallerShape.ToUnmanaged)
                && !(_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer)
                    && MarshallerHelpers.CanUseCallerAllocatedBuffer(info, context.CodeContext)))
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
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubIdentifierContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubIdentifierContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubIdentifierContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            // int <numElements>;
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(numElementsIdentifier))));
            // Use the numElements local to ensure the compiler doesn't give errors for using an uninitialized variable.
            // The value may be used in cleanup before it has been initialized, so this is unsafe
            yield return MarshallerHelpers.SkipInitOrDefaultInit(
                new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
                {
                    InstanceIdentifier = numElementsIdentifier
                }, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (context.CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
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
                    ElementsMarshalling.GenerateNumElementsExpression(_countInfo, _countInfoRequiresCast, context)));

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
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
    }

    internal sealed class StatelessLinearCollectionSource : IElementsMarshallingCollectionSource
    {
        private readonly TypeSyntax _marshallerTypeSyntax;

        public StatelessLinearCollectionSource(TypeSyntax marshallerTypeSyntax)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
        }

        public InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubIdentifierContext context)
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

        public InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubIdentifierContext context)
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

        public InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubIdentifierContext context)
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

        public InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubIdentifierContext context)
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
    /// Marshaller that enables support for marshalling elements of a collection via a marshaller type that implements the contiguous collection marshalling shape.
    /// </summary>
    internal sealed class StatelessLinearCollectionMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _spaceMarshallingStrategy;
        private readonly ElementsMarshalling _elementsMarshalling;
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;
        private readonly CountInfo _countInfo;
        private readonly bool _castCountInfo;
        private readonly bool _cleanupElementsAndSpace;

        public StatelessLinearCollectionMarshalling(
            ICustomTypeMarshallingStrategy spaceMarshallingStrategy,
            ElementsMarshalling elementsMarshalling,
            ManagedTypeInfo unmanagedType,
            MarshallerShape shape,
            CountInfo countInfo,
            bool castCountInfo,
            bool cleanupElementsAndSpace)
        {
            _spaceMarshallingStrategy = spaceMarshallingStrategy;
            _elementsMarshalling = elementsMarshalling;
            _unmanagedType = unmanagedType;
            _shape = shape;
            _countInfo = countInfo;
            _castCountInfo = castCountInfo;
            _cleanupElementsAndSpace = cleanupElementsAndSpace;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _unmanagedType;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (!_cleanupElementsAndSpace)
            {
                yield break;
            }

            StatementSyntax elementCleanup = _elementsMarshalling.GenerateElementCleanupStatement(info, context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                // If we don't have the numElements variable still available from unmarshal or marshal stage, we need to reassign that again
                if (!context.CodeContext.AdditionalTemporaryStateLivesAcrossStages)
                {
                    // <numElements> = <numElementsExpression>;
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(numElementsIdentifier),
                            ElementsMarshalling.GenerateNumElementsExpression(_countInfo, _castCountInfo, context)));
                }
                yield return elementCleanup;
            }

            if (MarshallerHelpers.GetCleanupStage(info, context.CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                foreach (var statement in _spaceMarshallingStrategy.GenerateCleanupCallerAllocatedResourcesStatements(info, context))
                {
                    yield return statement;
                }
            }
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (!_cleanupElementsAndSpace)
            {
                yield break;
            }
            StatementSyntax elementCleanup = _elementsMarshalling.GenerateElementCleanupStatement(info, context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                // If we don't have the numElements variable still available from unmarshal or marshal stage, we need to reassign that again
                if (!context.CodeContext.AdditionalTemporaryStateLivesAcrossStages)
                {
                    // <numElements> = <numElementsExpression>;
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(numElementsIdentifier),
                            ElementsMarshalling.GenerateNumElementsExpression(_countInfo, _castCountInfo, context)));
                }
                yield return elementCleanup;
            }

            if (MarshallerHelpers.GetCleanupStage(info, context.CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                foreach (var statement in _spaceMarshallingStrategy.GenerateCleanupCalleeAllocatedResourcesStatements(info, context))
                {
                    yield return statement;
                }
            }
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _spaceMarshallingStrategy.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (context.CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                yield return _elementsMarshalling.GenerateClearUnmanagedDestination(info, context);

                yield break;
            }

            if (context.CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // If the parameter is marshalled by-value [Out] or [In, Out], then we need to unmarshal the contents of the collection
                // into the passed-in collection value.
                yield return _elementsMarshalling.GenerateUnmanagedToManagedByValueOutMarshalStatement(info, context);
                yield break;
            }

            foreach (var statement in _spaceMarshallingStrategy.GenerateMarshalStatements(info, context))
            {
                yield return statement;
            }

            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            yield return _elementsMarshalling.GenerateMarshalStatement(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubIdentifierContext context) => _spaceMarshallingStrategy.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubIdentifierContext context) => _spaceMarshallingStrategy.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubIdentifierContext context) => _spaceMarshallingStrategy.GeneratePinStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            foreach (var s in _spaceMarshallingStrategy.GenerateSetupStatements(info, context))
                yield return s;
            var elementsSetup = _elementsMarshalling.GenerateSetupStatement(info, context);
            if (elementsSetup is not EmptyStatementSyntax)
            {
                yield return elementsSetup;
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context)
        {
            if (context.CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // If the parameter is marshalled by-value [Out] or [In, Out], then we need to unmarshal the contents of the collection
                // into the passed-in collection value.
                yield return _elementsMarshalling.GenerateManagedToUnmanagedByValueOutUnmarshalStatement(info, context);
                yield break;
            }

            if (context.CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                yield return _elementsMarshalling.GenerateClearManagedValuesDestination(info, context);
                foreach (var statement in _spaceMarshallingStrategy.GenerateUnmarshalStatements(info, context))
                {
                    yield return statement;
                }
                yield break;
            }

            if (!_shape.HasFlag(MarshallerShape.ToManaged))
            {
                yield break;
            }
            else
            {
                foreach (var statement in _spaceMarshallingStrategy.GenerateUnmarshalStatements(info, context))
                {
                    yield return statement;
                }

                yield return _elementsMarshalling.GenerateUnmarshalStatement(info, context);
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
    }
}
