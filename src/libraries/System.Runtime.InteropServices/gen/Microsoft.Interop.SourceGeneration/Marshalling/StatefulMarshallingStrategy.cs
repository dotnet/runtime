// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class StatefulValueMarshalling : ICustomTypeMarshallingStrategy
    {
        internal const string MarshallerIdentifier = "marshaller";
        private readonly ManagedTypeInfo _marshallerType;
        private readonly ManagedTypeInfo _unmanagedType;
        private readonly MarshallerShape _shape;

        public StatefulValueMarshalling(ManagedTypeInfo marshallerType, ManagedTypeInfo unmanagedType, MarshallerShape shape)
        {
            _marshallerType = marshallerType;
            _unmanagedType = unmanagedType;
            _shape = shape;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _unmanagedType;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.Free))
                yield break;

            // <marshaller>.Free();
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Free)),
                    ArgumentList()));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshaller>.ToManagedFinally();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.ToManagedFinally)),
                        ArgumentList())));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <marshaller>.FromManaged(<managedIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.FromManaged)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(managedIdentifier))))));
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            (_, string nativeIdentifier) = context.GetIdentifiers(info);

            // <nativeIdentifier> = <marshaller>.ToUnmanaged();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.ToUnmanaged)),
                        ArgumentList())));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshaller>.ToManaged();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.ToManaged)),
                        ArgumentList())));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged) && !_shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (_, string nativeIdentifier) = context.GetIdentifiers(info);

            // <marshaller>.FromUnmanaged(<nativeIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.FromUnmanaged)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(nativeIdentifier))))));
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            // <marshaller> = new();
            LocalDeclarationStatementSyntax declaration = MarshallerHelpers.Declare(
                _marshallerType.Syntax,
                context.GetAdditionalIdentifier(info, MarshallerIdentifier),
                ImplicitObjectCreationExpression(ArgumentList(), initializer: null));

            // For byref-like marshaller types, we'll mark them as scoped.
            // Byref-like types can capture references, so by default the compiler has to worry that
            // they could enable those references to escape the current stack frame.
            // In particular, this can interact poorly with the caller-allocated-buffer marshalling
            // support and make the simple `marshaller.FromManaged(managed, stackalloc X[i])` expression
            // illegal. Mark the marshaller type as scoped so the compiler knows that it won't escape.
            if (_marshallerType is ValueTypeInfo { IsByRefLike: true })
            {
                declaration = declaration.AddModifiers(Token(SyntaxKind.ScopedKeyword));
            }

            yield return declaration;
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.StatefulPinnableReference))
                yield break;

            string unusedIdentifier = context.GetAdditionalIdentifier(info, "unused");
            yield return FixedStatement(
                VariableDeclaration(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    SingletonSeparatedList(
                        VariableDeclarator(unusedIdentifier)
                            .WithInitializer(EqualsValueClause(IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)))))),
                EmptyStatement());
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.OnInvoked))
                yield break;

            // <marshaller>.OnInvoked();
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.OnInvoked)),
                    ArgumentList()));
        }

        public static string GetMarshallerIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, MarshallerIdentifier);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StatefulCallerAllocatedBufferMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _marshallerType;
        private readonly TypeSyntax _bufferElementType;

        public StatefulCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType, TypeSyntax bufferElementType)
        {
            _innerMarshaller = innerMarshaller;
            _marshallerType = marshallerType;
            _bufferElementType = bufferElementType;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (MarshallerHelpers.CanUseCallerAllocatedBuffer(info, context))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return _innerMarshaller.GenerateMarshalStatements(info, context);

            IEnumerable<StatementSyntax> GenerateCallerAllocatedBufferMarshalStatements()
            {
                (string managedIdentifier, _) = context.GetIdentifiers(info);

                // <marshaller>.FromManaged(<managedIdentifier>, stackalloc <bufferElementType>[<marshallerType>.BufferSize]);
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, StatefulValueMarshalling.MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.FromManaged)),
                        ArgumentList(SeparatedList(
                            new[]
                            {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(StackAllocArrayCreationExpression(
                                        ArrayType(
                                            _bufferElementType,
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize))))))))
                            }))));
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateUnmarshalStatements(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
    }

    internal sealed class StatefulLinearCollectionSource : IElementsMarshallingCollectionSource
    {
        public InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);

            // <marshaller>.GetUnmanagedValuesDestination()
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesDestination)),
                ArgumentList());
        }

        public InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);

            // <marshaller>.GetManagedValuesSource()
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesSource)),
                ArgumentList());
        }

        public InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            // <marshaller>.GetUnmanagedValuesSource(<numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesSource)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(numElementsIdentifier)))));
        }

        public InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            // <marshaller>.GetManagedValuesDestination(<numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesDestination)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(numElementsIdentifier)))));
        }
    }

    /// <summary>
    /// Marshaller that enables support for marshalling elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatefulLinearCollectionMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly MarshallerShape _shape;
        private readonly ExpressionSyntax _numElementsExpression;
        private readonly ElementsMarshalling _elementsMarshalling;
        private readonly bool _cleanupElements;

        public StatefulLinearCollectionMarshalling(
            ICustomTypeMarshallingStrategy innerMarshaller,
            MarshallerShape shape,
            ExpressionSyntax numElementsExpression,
            ElementsMarshalling elementsMarshalling,
            bool cleanupElements)
        {
            _innerMarshaller = innerMarshaller;
            _shape = shape;
            _numElementsExpression = numElementsExpression;
            _elementsMarshalling = elementsMarshalling;
            _cleanupElements = cleanupElements;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);
        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_cleanupElements)
            {
                yield break;
            }

            StatementSyntax elementCleanup = _elementsMarshalling.GenerateElementCleanupStatement(info, context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                yield return elementCleanup;
            }
        }
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context))
            {
                yield return statement;
            }

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

            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            yield return _elementsMarshalling.GenerateMarshalStatement(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (StatementSyntax statement in _innerMarshaller.GenerateSetupStatements(info, context))
            {
                yield return statement;
            }

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
                yield return _elementsMarshalling.GenerateClearUnmanagedValuesSource(info, context);
                yield break;
            }

            if (!_shape.HasFlag(MarshallerShape.ToManaged))
            {
                yield break;
            }

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            // <numElements> = <numElementsExpression>;
            yield return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    _numElementsExpression));

            yield return _elementsMarshalling.GenerateUnmarshalStatement(info, context);

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
    }

    /// <summary>
    /// Marshaller that enables calling the Free method on a stateful marshaller.
    /// </summary>
    internal sealed class StatefulFreeMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;

        public StatefulFreeMarshalling(ICustomTypeMarshallingStrategy innerMarshaller)
        {
            _innerMarshaller = innerMarshaller;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (var statement in _innerMarshaller.GenerateCleanupStatements(info, context))
            {
                yield return statement;
            }

            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            // <marshaller>.Free();
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(marshaller),
                        IdentifierName(ShapeMemberNames.Free)),
                    ArgumentList()));
        }
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateMarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);
    }
}
