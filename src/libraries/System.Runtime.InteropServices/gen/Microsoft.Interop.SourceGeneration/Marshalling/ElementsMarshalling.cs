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
    internal interface IElementsMarshallingCollectionSource
    {
        InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context);
        InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context);
        InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context);
        InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context);
    }

    internal interface IElementsMarshalling
    {
        StatementSyntax GenerateManagedToUnmanagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context);
        StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context);
        StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context);
        StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context);
        StatementSyntax GenerateUnmanagedToManagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context);
        StatementSyntax GenerateUnmarshalStatement(TypePositionInfo info, StubCodeContext context);
        StatementSyntax GenerateElementCleanupStatement(TypePositionInfo info, StubCodeContext context);
    }

#pragma warning disable SA1400 // Access modifier should be declared. https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3659
    static file class ElementsMarshallingCollectionSourceExtensions
#pragma warning restore SA1400 // Access modifier should be declared
    {
        public static StatementSyntax GetNumElementsAssignmentFromManagedValuesSource(this IElementsMarshallingCollectionSource source, TypePositionInfo info, StubCodeContext context)
        {
            var numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            // <numElements> = <GetManagedValuesSource>.Length;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source.GetManagedValuesSource(info, context),
                        IdentifierName("Length"))));
        }

        public static StatementSyntax GetNumElementsAssignmentFromManagedValuesDestination(this IElementsMarshallingCollectionSource source, TypePositionInfo info, StubCodeContext context)
        {
            var numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            // <numElements> = <GetManagedValuesDestination>.Length;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source.GetManagedValuesDestination(info, context),
                        IdentifierName("Length"))));
        }
    }

    /// <summary>
    /// Support for marshalling blittable elements
    /// </summary>
    internal sealed class BlittableElementsMarshalling : IElementsMarshalling
    {
        private readonly TypeSyntax _managedElementType;
        private readonly TypeSyntax _unmanagedElementType;
        private readonly IElementsMarshallingCollectionSource _collectionSource;

        public BlittableElementsMarshalling(TypeSyntax managedElementType, TypeSyntax unmanagedElementType, IElementsMarshallingCollectionSource collectionSource)
        {
            _managedElementType = managedElementType;
            _unmanagedElementType = unmanagedElementType;
            _collectionSource = collectionSource;
        }

        public StatementSyntax GenerateManagedToUnmanagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
            // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
            // <GetUnmanagedValuesDestination>.Clear();
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _collectionSource.GetUnmanagedValuesDestination(info, context),
                        IdentifierName("Clear"))));
        }

        public StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax destination = CastToManagedIfNecessary(_collectionSource.GetUnmanagedValuesSource(info, context));

            // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<GetManagedValuesSource>), <GetManagedValuesSource>.Length)
            ExpressionSyntax source = InvocationExpression(
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
                                    Argument(_collectionSource.GetManagedValuesDestination(info, context))))))
                            .WithRefKindKeyword(
                                Token(SyntaxKind.RefKeyword)),
                        Argument(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                _collectionSource.GetManagedValuesDestination(info, context),
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

        public StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax destination = CastToManagedIfNecessary(_collectionSource.GetUnmanagedValuesDestination(info, context));

            // <GetManagedValuesSource>.CopyTo(<destination>);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _collectionSource.GetManagedValuesSource(info, context),
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(destination)));
        }

        public StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(_collectionSource.GetUnmanagedValuesDestination(info, context));

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
                                    Argument(_collectionSource.GetManagedValuesSource(info, context))))))
                            .WithRefKindKeyword(
                                Token(SyntaxKind.RefKeyword)),
                        Argument(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                _collectionSource.GetManagedValuesSource(info, context),
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

        public StatementSyntax GenerateUnmanagedToManagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
            // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
            // <GetManagedValuesDestination>.Clear();
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _collectionSource.GetManagedValuesDestination(info, context),
                        IdentifierName("Clear"))));
        }

        public StatementSyntax GenerateUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(_collectionSource.GetUnmanagedValuesSource(info, context));

            // <source>.CopyTo(<GetManagedValuesDestination>);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source,
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(_collectionSource.GetManagedValuesDestination(info, context))));
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

        public StatementSyntax GenerateElementCleanupStatement(TypePositionInfo info, StubCodeContext context) => EmptyStatement();
    }

    /// <summary>
    /// Support for marshalling non-blittable elements
    /// </summary>
    internal sealed class NonBlittableElementsMarshalling : IElementsMarshalling
    {
        private readonly TypeSyntax _unmanagedElementType;
        private readonly IMarshallingGenerator _elementMarshaller;
        private readonly TypePositionInfo _elementInfo;
        private readonly IElementsMarshallingCollectionSource _collectionSource;

        public NonBlittableElementsMarshalling(
            TypeSyntax unmanagedElementType,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo,
            IElementsMarshallingCollectionSource collectionSource)
        {
            _unmanagedElementType = unmanagedElementType;
            _elementMarshaller = elementMarshaller;
            _elementInfo = elementInfo;
            _collectionSource = collectionSource;
        }

        public StatementSyntax GenerateManagedToUnmanagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
            // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
            // <GetUnmanagedValuesDestination>.Clear();
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _collectionSource.GetUnmanagedValuesDestination(info, context),
                        IdentifierName("Clear"))));
        }

        public StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context)
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
                            _collectionSource.GetManagedValuesSource(info, context)))))),
                LocalDeclarationStatement(VariableDeclaration(
                    GenericName(
                        Identifier(TypeNames.System_Span),
                        TypeArgumentList(SingletonSeparatedList(_unmanagedElementType))),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(nativeSpanIdentifier))
                        .WithInitializer(EqualsValueClause(
                            _collectionSource.GetUnmanagedValuesDestination(info, context)))))),
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
                            _collectionSource.GetUnmanagedValuesSource(info, context)))))),
                LocalDeclarationStatement(VariableDeclaration(
                    GenericName(
                        Identifier(TypeNames.System_Span),
                        TypeArgumentList(SingletonSeparatedList(_elementInfo.ManagedType.Syntax))),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(managedSpanIdentifier))
                        .WithInitializer(EqualsValueClause(
                            _collectionSource.GetManagedValuesDestination(info, context)))))),
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }

        public StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);

            var setNumElements = _collectionSource.GetNumElementsAssignmentFromManagedValuesSource(info, context);

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
                                                            _collectionSource.GetManagedValuesSource(info, context),
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
                        _collectionSource.GetUnmanagedValuesDestination(info, context))))));

            return Block(
                setNumElements,
                managedValuesDeclaration,
                unmanagedValuesDeclaration,
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }

        public StatementSyntax GenerateElementCleanupStatement(TypePositionInfo info, StubCodeContext context)
        {
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            StatementSyntax contentsCleanupStatements = GenerateContentsMarshallingStatement(info, context,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(MarshallerHelpers.GetNativeSpanIdentifier(info, context)),
                        IdentifierName("Length")),
                        StubCodeContext.Stage.Cleanup);

            if (contentsCleanupStatements.IsKind(SyntaxKind.EmptyStatement))
            {
                return EmptyStatement();
            }

            return Block(
                LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(SingletonSeparatedList(_unmanagedElementType))),
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(nativeSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                            context.Direction == MarshalDirection.ManagedToUnmanaged
                                ? _collectionSource.GetUnmanagedValuesDestination(info, context)
                                : _collectionSource.GetUnmanagedValuesSource(info, context)))))),
                contentsCleanupStatements);
        }

        private StatementSyntax GenerateContentsMarshallingStatement(
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

            if (elementStatements.Count != 0)
            {
                StatementSyntax marshallingStatement = Block(
                    List(_elementMarshaller.Generate(localElementInfo, elementSetupSubContext)
                        .Concat(elementStatements)));

                if (_elementMarshaller.AsNativeType(_elementInfo).Syntax is PointerTypeSyntax elementNativeType)
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

        public StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);

            var setNumElements = _collectionSource.GetNumElementsAssignmentFromManagedValuesDestination(info, context);

            // Span<TUnmanagedElement> <nativeSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <GetUnmanagedValuesSource>.GetPinnableReference(), <numElements>));
            LocalDeclarationStatementSyntax unmanagedValuesSource = LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(
                        SingletonSeparatedList(_unmanagedElementType))
                ),
                SingletonSeparatedList(VariableDeclarator(nativeSpanIdentifier).WithInitializer(EqualsValueClause(
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
                                                            _collectionSource.GetUnmanagedValuesSource(info, context),
                                                            IdentifierName("GetPinnableReference")),
                                                            ArgumentList()))
                                                .WithRefKindKeyword(
                                                    Token(SyntaxKind.InKeyword))))))
                                    .WithRefKindKeyword(
                                        Token(SyntaxKind.RefKeyword)),
                                    Argument(
                                        IdentifierName(numElementsIdentifier))
                                }))))))));

            // Span<TElement> <managedSpan> = <GetManagedValuesDestination>
            LocalDeclarationStatementSyntax managedValuesDestination = LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(SingletonSeparatedList(_elementInfo.ManagedType.Syntax))),
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(managedSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                        _collectionSource.GetManagedValuesDestination(info, context))))));

            // TODO: Clear elements in the loop as we see them. We can't use "original value" tracking here since we aren't replacing the collection,
            // we're updating it in-place.
            return Block(
                setNumElements,
                unmanagedValuesSource,
                managedValuesDestination,
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    StubCodeContext.Stage.Marshal,
                    StubCodeContext.Stage.PinnedMarshal));
        }

        public StatementSyntax GenerateUnmanagedToManagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
            // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
            // <GetManagedValuesDestination>.Clear();
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        _collectionSource.GetManagedValuesDestination(info, context),
                        IdentifierName("Clear"))));
        }
    }
}
