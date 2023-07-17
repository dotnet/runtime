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

        public IEnumerable<StatementSyntax> GenerateAssignParameterIn(TypePositionInfo info, StubCodeContext context)
        {
            var ids = context.GetAssignInOutIdentifiers(info);
            var assignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(ids.local), IdentifierName(ids.parameter));
            if (_unmanagedType is PointerTypeInfo pointer)
            {
                var rewriter = new PointerNativeTypeAssignmentRewriter(assignment.Right.ToString(), (PointerTypeSyntax)pointer.Syntax);
                assignment = (AssignmentExpressionSyntax)rewriter.Visit(assignment);
            }
            yield return ExpressionStatement(assignment);
        }
        public IEnumerable<StatementSyntax> GenerateAssignParameterOut(TypePositionInfo info, StubCodeContext context)
        {
            var ids = context.GetAssignInOutIdentifiers(info);
            var assignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(ids.parameter), IdentifierName(ids.local));
            if (_unmanagedType is PointerTypeInfo pointer)
            {
                var rewriter = new PointerNativeTypeAssignmentRewriter(assignment.Right.ToString(), (PointerTypeSyntax)pointer.Syntax);
                assignment = (AssignmentExpressionSyntax)rewriter.Visit(assignment);
            }
            yield return ExpressionStatement(assignment);
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
        public IEnumerable<StatementSyntax> GenerateAssignParameterIn(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateAssignParameterIn(info, context);
        public IEnumerable<StatementSyntax> GenerateAssignParameterOut(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateAssignParameterOut(info, context);
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

        public IEnumerable<StatementSyntax> GenerateAssignParameterIn(TypePositionInfo info, StubCodeContext context)
            => _innerMarshaller.GenerateAssignParameterIn(info, context);

        public IEnumerable<StatementSyntax> GenerateAssignParameterOut(TypePositionInfo info, StubCodeContext context)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            // In unmanaged to managed, we take ownership of the parameter and should clean up
            if (context.Direction == MarshalDirection.UnmanagedToManaged)
            {
                statements.AddRange(GenerateCleanupStatements(info, new NativeIdIsParameterContext(context)));
            }
            statements.AddRange(_innerMarshaller.GenerateAssignParameterOut(info, context));
            return statements;
        }

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


    internal interface ILinearCollectionSpaceAllocator : ICustomTypeMarshallingStrategy
    {

    }

    /// <summary>
    /// Marshaller type that enables allocating space for marshalling a linear collection using a marshaller that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionSpaceAllocator : ILinearCollectionSpaceAllocator
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatelessLinearCollectionSpaceAllocator(TypeSyntax marshallerTypeSyntax, ManagedTypeInfo unmanagedType, MarshallerShape shape, ExpressionSyntax numElementsExpression)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _unmanagedType = unmanagedType;
            _shape = shape;
            _numElementsExpression = numElementsExpression;
        }

        /// <summary>
        /// <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, out <numElements>);
        /// </summary>
        private ExpressionStatementSyntax GetAllocateContainerForUnmanagedElements(string nativeIdentifier, string managedIdentifier, string numElementsIdentifier)
        {
            // <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, out <numElements>);
            return ExpressionStatement(
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
        /// <summary>
        /// <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElements(<nativeIdentifier>, <numElements>);
        /// </summary>
        private ExpressionStatementSyntax GetAllocateContainerForManagedElements(string managedIdentifier, string nativeIdentifier, string numElementsIdentifier)
        {
            // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElements(<nativeIdentifier>, <numElements>);
            return ExpressionStatement(
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

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _unmanagedType;
        }

        public IEnumerable<StatementSyntax> GenerateAssignParameterIn(TypePositionInfo info, StubCodeContext context)
        {
            // For ByValue arrays, we need to allocate a container to copy the array contents
            var ids = context.GetIdentifiers(info);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            switch (context.Direction)
            {
                case MarshalDirection.UnmanagedToManaged:
                    var allocateStatement = GetAllocateContainerForUnmanagedElements(ids.native, ids.managed, numElementsIdentifier);
                    yield return allocateStatement;
                    yield break;
                case MarshalDirection.ManagedToUnmanaged:
                    var allocatestatement = GetAllocateContainerForManagedElements(ids.managed, ids.native, numElementsIdentifier);
                    yield return allocatestatement;
                    yield break;
                default:
                    throw new NotImplementedException();
            }
        }

        public IEnumerable<StatementSyntax> GenerateAssignParameterOut(TypePositionInfo info, StubCodeContext context)
        {
            // We shouldn't need to allocate for assign out, the marshal / unmarshal has already allocated
            yield break;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (MarshallerHelpers.GetMarshalDirection(info, context) == MarshalDirection.ManagedToUnmanaged)
            {
                yield return EmptyStatement();
                yield break;
            }

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    _numElementsExpression));
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
            if (context.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
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

        public InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context)
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

        public InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context)
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

        public InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context)
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

        public InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context)
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
        private readonly ILinearCollectionSpaceAllocator _spaceMarshallingStrategy;
        private readonly ElementsMarshalling _elementsMarshalling;
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;
        private readonly ExpressionSyntax _numElementsExpression;
        private readonly bool _cleanupElementsAndSpace;

        public StatelessLinearCollectionMarshalling(
            ILinearCollectionSpaceAllocator spaceMarshallingStrategy,
            ElementsMarshalling elementsMarshalling,
            ManagedTypeInfo unmanagedType,
            MarshallerShape shape,
            ExpressionSyntax numElementsExpression,
            bool cleanupElementsAndSpace)
        {
            _spaceMarshallingStrategy = spaceMarshallingStrategy;
            _elementsMarshalling = elementsMarshalling;
            _unmanagedType = unmanagedType;
            _shape = shape;
            _numElementsExpression = numElementsExpression;
            _cleanupElementsAndSpace = cleanupElementsAndSpace;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _unmanagedType;
        public IEnumerable<StatementSyntax> GenerateAssignParameterIn(TypePositionInfo info, StubCodeContext context)
        {
            // If we need to marshal the contents back out, we should make a copy of the elements in a new array. Otherwise they won't be modified.
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                _spaceMarshallingStrategy.GenerateAssignParameterIn(info, context);
                if (context.Direction == MarshalDirection.ManagedToUnmanaged)
                {
                    //TODO: copy the contents
                    yield break;
                }
                else if (context.Direction == MarshalDirection.UnmanagedToManaged)
                {
                    //TODO: copy the contents
                    yield break;
                }
                throw new UnreachableException();
            }

            // Otherwise, we can just assign the native identifier to be the parameter
            var ids = context.GetAssignInOutIdentifiers(info);
            yield return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(ids.local), IdentifierName(ids.parameter)));
        }
        public IEnumerable<StatementSyntax> GenerateAssignParameterOut(TypePositionInfo info, StubCodeContext context)
        {
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                if (context.Direction == MarshalDirection.ManagedToUnmanaged)
                {
                    //TODO: copy the contents
                    yield break;
                }
                else if (context.Direction == MarshalDirection.UnmanagedToManaged)
                {
                    //TODO: copy the contents
                    yield break;
                }
                throw new UnreachableException();
            }

            // Otherwise, we can just assign the native identifier to be the parameter
            var ids = context.GetAssignInOutIdentifiers(info);
            yield return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(ids.local), IdentifierName(ids.parameter)));
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_cleanupElementsAndSpace)
            {
                yield break;
            }
            StatementSyntax elementCleanup = _elementsMarshalling.GenerateElementCleanupStatement(info, context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                // If we don't have the numElements variable still available from unmarshal or marshal stage, we need to reassign that again
                if (!context.AdditionalTemporaryStateLivesAcrossStages)
                {
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(numElementsIdentifier),
                            _numElementsExpression));
                }
                yield return elementCleanup;
            }

            foreach (var statement in _spaceMarshallingStrategy.GenerateCleanupStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _spaceMarshallingStrategy.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (context.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                yield return _elementsMarshalling.GenerateClearManagedSource(info, context);
                yield break;
            }

            if (context.Direction == MarshalDirection.UnmanagedToManaged && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
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

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _spaceMarshallingStrategy.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _spaceMarshallingStrategy.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _spaceMarshallingStrategy.GeneratePinStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (var s in _spaceMarshallingStrategy.GenerateSetupStatements(info, context))
                yield return s;
            var elementsSetup = _elementsMarshalling.GenerateSetupStatement(info, context);
            if (elementsSetup is not EmptyStatementSyntax)
            {
                yield return elementsSetup;
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (context.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // If the parameter is marshalled by-value [Out] or [In, Out], then we need to unmarshal the contents of the collection
                // into the passed-in collection value.
                yield return _elementsMarshalling.GenerateManagedToUnmanagedByValueOutUnmarshalStatement(info, context);
                yield break;
            }

            if (context.Direction == MarshalDirection.UnmanagedToManaged && !info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                yield return _elementsMarshalling.GenerateClearManagedValuesDestination(info, context);
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

    /// <summary>
    /// Context that treats the parameter identifier as the "native" identifier. Used to make GenerateCleanupStatements clean the value passed in.
    /// </summary>
    /// <param name="inner"></param>
    internal sealed record NativeIdIsParameterContext(StubCodeContext inner) : StubCodeContext
    {
        public override bool SingleFrameSpansNativeContext => inner.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => inner.AdditionalTemporaryStateLivesAcrossStages;

        public override (TargetFramework framework, Version version) GetTargetFramework() => inner.GetTargetFramework();

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info) => (inner.GetIdentifiers(info).managed, inner.GetAssignInOutIdentifiers(info).parameter);
    }

    internal sealed record ManagedIdIsParameterContext(StubCodeContext inner) : StubCodeContext
    {
        public override bool SingleFrameSpansNativeContext => inner.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => inner.AdditionalTemporaryStateLivesAcrossStages;

        public override (TargetFramework framework, Version version) GetTargetFramework() => inner.GetTargetFramework();

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info) => (inner.GetAssignInOutIdentifiers(info).parameter, inner.GetIdentifiers(info).native);
    }
}
