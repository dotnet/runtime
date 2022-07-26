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
    /// <summary>
    /// Support for marshalling blittable elements
    /// </summary>
    internal abstract class BlittableElementsMarshalling
    {
        private readonly TypeSyntax _managedElementType;
        private readonly TypeSyntax _unmanagedElementType;

        public BlittableElementsMarshalling(TypeSyntax managedElementType, TypeSyntax unmanagedElementType)
        {
            _managedElementType = managedElementType;
            _unmanagedElementType = unmanagedElementType;
        }

        protected abstract InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context);
        protected abstract InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context);
        protected abstract InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context);
        protected abstract InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context);

        protected StatementSyntax GenerateByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
            // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
            // <GetUnmanagedValuesDestination>.Clear();
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        GetUnmanagedValuesDestination(info, context),
                        IdentifierName("Clear"))));
        }

        protected StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax destination = CastToManagedIfNecessary(GetUnmanagedValuesDestination(info, context));

            // <GetManagedValuesSource>.CopyTo(<destination>);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        GetManagedValuesSource(info, context),
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(destination)));
        }

        protected StatementSyntax GenerateByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(GetUnmanagedValuesDestination(info, context));

            // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<GetManagedValuesSource>), <GetManagedValuesSource>.Length)
            ExpressionSyntax destination = InvocationExpression(
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
                                    Argument(GetManagedValuesSource(info, context))))))
                            .WithRefKindKeyword(
                                Token(SyntaxKind.RefKeyword)),
                        Argument(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                GetManagedValuesSource(info, context),
                                IdentifierName("Length")))
                    })));

            // <source>.CopyTo(<destination>);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source,
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(destination)));
        }

        public StatementSyntax GenerateUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(GetUnmanagedValuesSource(info, context));

            // <source>.CopyTo(<GetManagedValuesDestination>);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source,
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(GetManagedValuesDestination(info, context))));
        }

        private ExpressionSyntax CastToManagedIfNecessary(ExpressionSyntax expression)
        {
            // Skip the cast if the managed and unmanaged element types are the same
            if (_unmanagedElementType.IsEquivalentTo(_managedElementType))
                return expression;

            // MemoryMarshal.Cast<<unmanagedElementType>, <elementType>>(<expression>)
            return InvocationExpression(
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
                    Argument(expression))));
        }
    }

    /// <summary>
    /// Support for marshalling non-blittable elements
    /// </summary>
    internal abstract class NonBlittableElementsMarshalling
    {
        private readonly TypeSyntax _unmanagedElementType;
        private readonly IMarshallingGenerator _elementMarshaller;
        private readonly TypePositionInfo _elementInfo;

        public NonBlittableElementsMarshalling(
            TypeSyntax unmanagedElementType,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo)
        {
            _unmanagedElementType = unmanagedElementType;
            _elementMarshaller = elementMarshaller;
            _elementInfo = elementInfo;
        }

        protected abstract InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context);
        protected abstract InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context);
        protected abstract InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context);
        protected abstract InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context);

        protected StatementSyntax GenerateByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
            // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
            // <GetUnmanagedValuesDestination>.Clear();
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        GetUnmanagedValuesDestination(info, context),
                        IdentifierName("Clear"))));
        }

        protected StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);

            // ReadOnlySpan<T> <managedSpan> = <GetManagedValuesSource>
            // Span<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesDestination>
            // << marshal contents >>
            return Block(
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
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(MarshallerHelpers.GetManagedSpanIdentifier(info, context)),
                        IdentifierName("Length")),
                    StubCodeContext.Stage.Marshal));
        }

        public StatementSyntax GenerateUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            // ReadOnlySpan<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesSource>
            // Span<T> <managedSpan> = <GetManagedValuesDestination>
            // << unmarshal contents >>
            return Block(
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
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }

        protected StatementSyntax GenerateByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);

            // Span<TElement> <managedSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <GetManagedValuesSource>.GetPinnableReference(), <numElements>));
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

            // Span<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesDestination>
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
                managedValuesDeclaration,
                unmanagedValuesDeclaration,
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }

        protected StatementSyntax GenerateContentsMarshallingStatement(
            TypePositionInfo info,
            StubCodeContext context,
            ExpressionSyntax lengthExpression,
            params StubCodeContext.Stage[] stagesToGeneratePerElement)
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
            foreach (StubCodeContext.Stage stage in stagesToGeneratePerElement)
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

                return base.VisitAssignmentExpression(node);
            }

            public override SyntaxNode? VisitArgument(ArgumentSyntax node)
            {
                if (node.Expression.ToString() == _nativeIdentifier)
                {
                    return node.WithExpression(
                        CastExpression(_nativeType, node.Expression));
                }
                return base.VisitArgument(node);
            }
        }
    }
}
