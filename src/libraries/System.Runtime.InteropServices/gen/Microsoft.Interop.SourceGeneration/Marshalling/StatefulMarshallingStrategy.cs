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
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly MarshallerShape _shape;

        public StatefulValueMarshalling(TypeSyntax marshallerTypeSyntax, TypeSyntax nativeTypeSyntax, MarshallerShape shape)
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
            yield return MarshallerHelpers.Declare(
                _marshallerTypeSyntax,
                context.GetAdditionalIdentifier(info, MarshallerIdentifier),
                ImplicitObjectCreationExpression(ArgumentList(), initializer: null));
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

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (CanUseCallerAllocatedBuffer(info, context))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return _innerMarshaller.GenerateMarshalStatements(info, context);

            IEnumerable<StatementSyntax> GenerateCallerAllocatedBufferMarshalStatements()
            {
                // TODO: Update once we can consume the scoped keword. We should be able to simplify this once we get that API.
                string stackPtrIdentifier = context.GetAdditionalIdentifier(info, "stackptr");
                // <bufferElementType>* <managedIdentifier>__stackptr = stackalloc <bufferElementType>[<_bufferSize>];
                yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PointerType(_bufferElementType),
                    SingletonSeparatedList(
                        VariableDeclarator(stackPtrIdentifier)
                            .WithInitializer(EqualsValueClause(
                                StackAllocArrayCreationExpression(
                                        ArrayType(
                                            _bufferElementType,
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize))
                                            ))))))))));


                (string managedIdentifier, _) = context.GetIdentifiers(info);

                // <marshaller>.FromManaged(<managedIdentifier>, new Span<bufferElementType>(<stackPtrIdentifier>, <marshallerType>.BufferSize));
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetAdditionalIdentifier(info, StatefulValueMarshalling.MarshallerIdentifier)),
                            IdentifierName(ShapeMemberNames.Value.Stateful.FromManaged)),
                        ArgumentList(SeparatedList(
                            new[]
                            {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(
                                    ObjectCreationExpression(
                                        GenericName(Identifier(TypeNames.System_Span),
                                            TypeArgumentList(SingletonSeparatedList(
                                                _bufferElementType))))
                                    .WithArgumentList(
                                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                                        {
                                            Argument(IdentifierName(stackPtrIdentifier)),
                                            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    _marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize)))
                                        }))))
                            }))));
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        }

        private static bool CanUseCallerAllocatedBuffer(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && (!info.IsByRef || info.RefKind == RefKind.In);
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

    /// <summary>
    /// Marshaller that enables support for marshalling blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatefulLinearCollectionBlittableElementsMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly MarshallerShape _shape;
        private readonly TypeSyntax _managedElementType;
        private readonly TypeSyntax _unmanagedElementType;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatefulLinearCollectionBlittableElementsMarshalling(
            ICustomTypeMarshallingStrategy innerMarshaller, MarshallerShape shape, TypeSyntax managedElementType, TypeSyntax unmanagedElementType, ExpressionSyntax numElementsExpression)
        {
            _innerMarshaller = innerMarshaller;
            _shape = shape;
            _managedElementType = managedElementType;
            _unmanagedElementType = unmanagedElementType;
            _numElementsExpression = numElementsExpression;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);
        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateCleanupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged) && !_shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context))
            {
                yield return statement;
            }

            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);

            // <marshaller>.GetUnmanagedValuesDestination()
            ExpressionSyntax destination =
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(marshaller),
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesDestination)),
                    ArgumentList());

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                // <marshaller>.GetUnmanagedValuesDestination().Clear();
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

            // <marshaller>.GetManagedValuesSource()
            ExpressionSyntax source = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(StatefulValueMarshalling.GetMarshallerIdentifier(info, context)),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesSource)),
                ArgumentList());

            // <source>.CopyTo(<destination>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source,
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(destination)));
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);

            ExpressionSyntax copySource;
            ExpressionSyntax copyDestination;
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // <marshaller>.GetUnmanagedValuesDestination()
                copySource =
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(marshaller),
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesDestination)),
                        ArgumentList());

                // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<marshaller>.GetManagedValuesSource()), <marshallerType>.GetManagedValuesSource().Length)
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
                                                    IdentifierName(marshaller),
                                                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesSource)),
                                                ArgumentList()))))))
                                .WithRefKindKeyword(
                                    Token(SyntaxKind.RefKeyword)),
                            Argument(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(marshaller),
                                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                                        ArgumentList()),
                                    IdentifierName("Length")))
                        })));
            }
            else
            {
                // int <numElements> = <numElementExpression>
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(numElementsIdentifier)
                                .WithInitializer(EqualsValueClause(_numElementsExpression)))));

                // <marshaller>.GetUnmanagedValuesSource(<numElements>)
                copySource = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(marshaller),
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesSource)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(numElementsIdentifier)))));

                // <marshaller>.GetManagedValuesDestination(<numElements>)
                copyDestination = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(marshaller),
                        IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesDestination)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(numElementsIdentifier)))));
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

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);

        private static string GetNumElementsIdentifier(TypePositionInfo info, StubCodeContext context)
            => context.GetAdditionalIdentifier(info, "numElements");
    }

    /// <summary>
    /// Marshaller that enables support for marshalling non-blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatefulLinearCollectionNonBlittableElementsMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;
        private readonly MarshallerShape _shape;
        private readonly TypeSyntax _unmanagedElementType;
        private readonly IMarshallingGenerator _elementMarshaller;
        private readonly TypePositionInfo _elementInfo;
        private readonly ExpressionSyntax _numElementsExpression;

        public StatefulLinearCollectionNonBlittableElementsMarshalling(
            ICustomTypeMarshallingStrategy innerMarshaller,
            MarshallerShape shape,
            TypeSyntax unmanagedElementType,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo,
            ExpressionSyntax numElementsExpression)
        {
            _innerMarshaller = innerMarshaller;
            _shape = shape;
            _unmanagedElementType = unmanagedElementType;
            _elementMarshaller = elementMarshaller;
            _elementInfo = elementInfo;
            _numElementsExpression = numElementsExpression;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);
        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateCleanupStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context))
            {
                yield return statement;
            }

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                // <marshaller>.GetUnmanagedValuesDestination().Clear();
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

            // ReadOnlySpan<T> <managedSpan> = <marshaller>.GetManagedValuesSource()
            // Span<TUnmanagedElement> <nativeSpan> = <marshaller>.GetUnmanagedValuesDestination()
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
                MarshallerHelpers.LinearCollection.NonBlittableContentsMarshallingStatement(
                    info,
                    context,
                    _elementInfo,
                    _elementMarshaller,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(MarshallerHelpers.GetManagedSpanIdentifier(info, context)),
                        IdentifierName("Length")),
                    StubCodeContext.Stage.Marshal));
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield return GenerateByValueUnmarshalStatement(info, context);
            }
            else
            {
                // int <numElements> = <numElementsExpression>;
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(numElementsIdentifier)
                                .WithInitializer(EqualsValueClause(_numElementsExpression)))));

                string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
                string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);

                // ReadOnlySpan<TUnmanagedElement> <nativeSpan> = <marshaller>.GetUnmanagedValuesSource(<nativeIdentifier>, <numElements>)
                // Span<T> <managedSpan> = <marshaller>.GetManagedValuesDestination(<managedIdentifier>)
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
                    MarshallerHelpers.LinearCollection.NonBlittableContentsMarshallingStatement(
                        info,
                        context,
                        _elementInfo,
                        _elementMarshaller,
                        IdentifierName(numElementsIdentifier),
                        StubCodeContext.Stage.UnmarshalCapture,
                        StubCodeContext.Stage.Unmarshal));
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        private static string GetNumElementsIdentifier(TypePositionInfo info, StubCodeContext context)
            => context.GetAdditionalIdentifier(info, "numElements");

        private static InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);

            // <marshallerType>.GetUnmanagedValuesDestination()
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination)),
                ArgumentList());
        }

        private static InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context)
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

        private static InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            // <marshaller>.GetUnmanagedValuesSource(<numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesSource)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(numElementsIdentifier)))));
        }

        private static InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = GetNumElementsIdentifier(info, context);

            // <marshaller>.GetManagedValuesDestination(<numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(numElementsIdentifier)))));
        }

        private StatementSyntax GenerateByValueUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = context.GetAdditionalIdentifier(info, "numElements");

            // <numElements> = <marshaller>.GetManagedValuesSource().Length;
            ExpressionStatementSyntax numElementsStatement = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        GetManagedValuesSource(info, context),
                        IdentifierName("Length"))));

            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);

            // Span<TElement> <managedSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <marshaller>.GetManagedValuesSource().GetPinnableReference(), <numElements>));
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

            // Span<TUnmanagedElement> <nativeSpan> = <marshaller>.GetUnmanagedValuesDestination()
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
                MarshallerHelpers.LinearCollection.NonBlittableContentsMarshallingStatement(
                    info,
                    context,
                    _elementInfo,
                    _elementMarshaller,
                    IdentifierName(numElementsIdentifier),
                    StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }
    }
}
