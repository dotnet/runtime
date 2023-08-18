// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    internal interface IElementsMarshallingCollectionSource
    {
        InvocationExpressionSyntax GetUnmanagedValuesDestination(TypePositionInfo info, StubCodeContext context);
        InvocationExpressionSyntax GetManagedValuesSource(TypePositionInfo info, StubCodeContext context);
        InvocationExpressionSyntax GetUnmanagedValuesSource(TypePositionInfo info, StubCodeContext context);
        InvocationExpressionSyntax GetManagedValuesDestination(TypePositionInfo info, StubCodeContext context);
    }

    internal abstract class ElementsMarshalling
    {
        protected IElementsMarshallingCollectionSource CollectionSource { get; }

        protected ElementsMarshalling(IElementsMarshallingCollectionSource collectionSource)
        {
            CollectionSource = collectionSource;
        }

        /// <summary>
        /// <code>
        /// &lt; GetUnmanagedValuesDestination &gt;.Clear();
        /// </code>
        /// </summary>
        public StatementSyntax GenerateClearUnmanagedDestination(TypePositionInfo info, StubCodeContext context)

        {
            // <GetUnmanagedValuesDestination>.Clear();
            return MethodInvocationStatement(
                        CollectionSource.GetUnmanagedValuesDestination(info, context),
                        IdentifierName("Clear"));
        }
        /// <summary>
        /// <code>
        /// &lt; GetManagedValuesDestination &gt;.Clear();
        /// </code>
        /// </summary>
        public StatementSyntax GenerateClearManagedValuesDestination(TypePositionInfo info, StubCodeContext context)
        {
            // <GetManagedValuedDestination>.Clear();
            return MethodInvocationStatement(
                        CollectionSource.GetManagedValuesDestination(info, context),
                        IdentifierName("Clear"));
        }

        public abstract StatementSyntax GenerateSetupStatement(TypePositionInfo info, StubCodeContext context);
        public abstract StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context);
        public abstract StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context);
        public abstract StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context);

        public abstract StatementSyntax GenerateUnmarshalStatement(TypePositionInfo info, StubCodeContext context);
        public abstract StatementSyntax GenerateElementCleanupStatement(TypePositionInfo info, StubCodeContext context);
    }

#pragma warning disable SA1400 // Access modifier should be declared https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3659
    file static class ElementsMarshallingCollectionSourceExtensions
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
    internal sealed class BlittableElementsMarshalling : ElementsMarshalling
    {
        private readonly TypeSyntax _managedElementType;
        private readonly TypeSyntax _unmanagedElementType;

        public BlittableElementsMarshalling(TypeSyntax managedElementType, TypeSyntax unmanagedElementType, IElementsMarshallingCollectionSource collectionSource)
            : base(collectionSource)
        {
            _managedElementType = managedElementType;
            _unmanagedElementType = unmanagedElementType;
        }

        public override StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<GetUnmanagedValuesSource>), <GetUnmanagedValuesSource>.Length)
            ExpressionSyntax destination = CastToManagedIfNecessary(
                MethodInvocation(
                    TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                    IdentifierName("CreateSpan"),
                    RefArgument(
                        MethodInvocation(
                            TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                            IdentifierName("GetReference"),
                            Argument(CollectionSource.GetUnmanagedValuesSource(info, context)))),
                    Argument(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            CollectionSource.GetUnmanagedValuesSource(info, context),
                            IdentifierName("Length")))));

            // <GetManagedValuesDestination>.CopyTo(<source>);
            return MethodInvocationStatement(
                    CollectionSource.GetManagedValuesDestination(info, context),
                    IdentifierName("CopyTo"),
                    Argument(destination));
        }

        public override StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax destination = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesDestination(info, context));

            // <GetManagedValuesSource>.CopyTo(<destination>);
            return MethodInvocationStatement(
                    CollectionSource.GetManagedValuesSource(info, context),
                    IdentifierName("CopyTo"),
                    Argument(destination));
        }

        public override StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesDestination(info, context));

            // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<GetManagedValuesSource>), <GetManagedValuesSource>.Length)
            ExpressionSyntax destination = MethodInvocation(
                    TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                    IdentifierName("CreateSpan"),
                        RefArgument(
                            MethodInvocation(
                                TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                                IdentifierName("GetReference"),
                                Argument(CollectionSource.GetManagedValuesSource(info, context)))),
                        Argument(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                CollectionSource.GetManagedValuesSource(info, context),
                                IdentifierName("Length"))));

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

        public override StatementSyntax GenerateUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesSource(info, context));

            // <source>.CopyTo(<GetManagedValuesDestination>);
            return MethodInvocationStatement(
                    source,
                    IdentifierName("CopyTo"),
                    Argument(CollectionSource.GetManagedValuesDestination(info, context)));
        }

        private ExpressionSyntax CastToManagedIfNecessary(ExpressionSyntax expression)
        {
            // Skip the cast if the managed and unmanaged element types are the same
            if (_unmanagedElementType.IsEquivalentTo(_managedElementType))
                return expression;

            // MemoryMarshal.Cast<<unmanagedElementType>, <elementType>>(<expression>)
            return MethodInvocation(
                    TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                    GenericName(
                        Identifier("Cast"),
                        TypeArgumentList(SeparatedList(new[]
                            {
                                _unmanagedElementType,
                                _managedElementType
                            }))),
                    Argument(expression));
        }

        public override StatementSyntax GenerateElementCleanupStatement(TypePositionInfo info, StubCodeContext context) => EmptyStatement();
        public override StatementSyntax GenerateSetupStatement(TypePositionInfo info, StubCodeContext context) => EmptyStatement();
    }

    /// <summary>
    /// Support for marshalling non-blittable elements
    /// </summary>
    internal sealed class NonBlittableElementsMarshalling : ElementsMarshalling
    {
        private readonly TypeSyntax _unmanagedElementType;
        private readonly IMarshallingGenerator _elementMarshaller;
        private readonly TypePositionInfo _elementInfo;

        public NonBlittableElementsMarshalling(
            TypeSyntax unmanagedElementType,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo,
            IElementsMarshallingCollectionSource collectionSource)
            : base(collectionSource)
        {
            _unmanagedElementType = unmanagedElementType;
            _elementMarshaller = elementMarshaller;
            _elementInfo = elementInfo;
        }

        public override StatementSyntax GenerateMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);

            // ReadOnlySpan<T> <managedSpan> = <GetManagedValuesSource>
            // Span<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesDestination>
            // <if multidimensional collection> <nativeSpan>.Clear()
            // << marshal contents >>
            var statements = new List<StatementSyntax>()
            {
                Declare(
                    ReadOnlySpanOf(_elementInfo.ManagedType.Syntax),
                    managedSpanIdentifier,
                    CollectionSource.GetManagedValuesSource(info, context)),
                Declare(
                    SpanOf(_unmanagedElementType),
                    nativeSpanIdentifier,
                    CollectionSource.GetUnmanagedValuesDestination(info, context))
            };
            // If it is a multidimensional array, we will just clear each allocated span.
            if (ShouldCleanUpAllElements(info, context))
            {
                // <nativeSpanIdentifier>.Clear()
                statements.Add(MethodInvocationStatement(
                            IdentifierName(nativeSpanIdentifier),
                            IdentifierName("Clear")));
            }
            statements.Add(GenerateContentsMarshallingStatement(
                    info,
                    context,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(MarshallerHelpers.GetManagedSpanIdentifier(info, context)),
                        IdentifierName("Length")),
                     _elementInfo, _elementMarshaller, StubCodeContext.Stage.Marshal));
            return Block(statements);
        }

        public override StatementSyntax GenerateUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            // ReadOnlySpan<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesSource>
            // Span<T> <managedSpan> = <GetManagedValuesDestination>
            // << unmarshal contents >>
            return Block(
                Declare(
                    ReadOnlySpanOf(_unmanagedElementType),
                    nativeSpanIdentifier,
                    CollectionSource.GetUnmanagedValuesSource(info, context)),
                Declare(
                    SpanOf(_elementInfo.ManagedType.Syntax),
                    managedSpanIdentifier,
                    CollectionSource.GetManagedValuesDestination(info, context)),
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    _elementInfo, _elementMarshaller, StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }

        public override StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);

            var setNumElements = CollectionSource.GetNumElementsAssignmentFromManagedValuesSource(info, context);

            // Span<TElement> <managedSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <GetManagedValuesSource>.GetPinnableReference(), <numElements>));
            LocalDeclarationStatementSyntax managedValuesDeclaration = Declare(SpanOf(_elementInfo.ManagedType.Syntax),
            managedSpanIdentifier,
            MethodInvocation(
                TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                IdentifierName("CreateSpan"),
                RefArgument(
                    MethodInvocation(
                        TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                        IdentifierName("AsRef"),
                        InArgument(
                            MethodInvocation(
                                CollectionSource.GetManagedValuesSource(info, context),
                                IdentifierName("GetPinnableReference"))))),
                Argument(IdentifierName(numElementsIdentifier))));

            // Span<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesDestination>
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            LocalDeclarationStatementSyntax unmanagedValuesDeclaration = Declare(
                SpanOf(_unmanagedElementType),
                nativeSpanIdentifier,
                CollectionSource.GetUnmanagedValuesDestination(info, context));

            return Block(
                setNumElements,
                managedValuesDeclaration,
                unmanagedValuesDeclaration,
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    _elementInfo, _elementMarshaller, StubCodeContext.Stage.UnmarshalCapture,
                    StubCodeContext.Stage.Unmarshal));
        }

        public override StatementSyntax GenerateElementCleanupStatement(TypePositionInfo info, StubCodeContext context)
        {
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            ExpressionSyntax indexConstraintName;
            if (!UsesLastIndexMarshalled(info, context))
            {
                indexConstraintName = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(nativeSpanIdentifier),
                        IdentifierName("Length"));
            }
            else
            {
                indexConstraintName = IdentifierName(MarshallerHelpers.GetLastIndexMarshalledIdentifier(info, context));
            }
            StatementSyntax contentsCleanupStatements = GenerateContentsMarshallingStatement(
                info,
                context,
                indexConstraintName,
                _elementInfo,
                _elementMarshaller,
                context.CurrentStage);

            if (contentsCleanupStatements.IsKind(SyntaxKind.EmptyStatement))
            {
                if (UsesLastIndexMarshalled(info, context))
                {
                    return AssignmentStatement(
                            IdentifierName("_"),
                            IdentifierName(MarshallerHelpers.GetLastIndexMarshalledIdentifier(info, context)));
                }
                return EmptyStatement();
            }

            return Block(
                Declare(
                    ReadOnlySpanOf(_unmanagedElementType),
                    nativeSpanIdentifier,
                    MarshallerHelpers.GetMarshalDirection(info, context) == MarshalDirection.ManagedToUnmanaged
                        ? CollectionSource.GetUnmanagedValuesDestination(info, context)
                        : CollectionSource.GetUnmanagedValuesSource(info, context)),
                contentsCleanupStatements);
        }

        public override StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);

            var setNumElements = CollectionSource.GetNumElementsAssignmentFromManagedValuesDestination(info, context);

            // Span<TUnmanagedElement> <nativeSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <GetUnmanagedValuesSource>.GetPinnableReference()), <numElements>);
            LocalDeclarationStatementSyntax unmanagedValuesSource = Declare(
                SpanOf(_unmanagedElementType),
                nativeSpanIdentifier,
                MethodInvocation(
                    TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                    IdentifierName("CreateSpan"),
                    RefArgument(
                        MethodInvocation(
                            TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                            IdentifierName("AsRef"),
                            InArgument(
                                MethodInvocation(
                                    CollectionSource.GetUnmanagedValuesSource(info, context),
                                    IdentifierName("GetPinnableReference"))))),
                    Argument(IdentifierName(numElementsIdentifier))));

            // Span<TElement> <managedSpan> = <GetManagedValuesDestination>
            LocalDeclarationStatementSyntax managedValuesDestination = LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(SingletonSeparatedList(_elementInfo.ManagedType.Syntax))),
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(managedSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                        CollectionSource.GetManagedValuesDestination(info, context))))));

            StubCodeContext.Stage[] stagesToGenerate;

            // Until we separate CalleeAllocated cleanup and CallerAllocated cleanup in unmanaged to managed, we'll need this hack
            if (context.Direction is MarshalDirection.UnmanagedToManaged && info.ByValueContentsMarshalKind is ByValueContentsMarshalKind.Out)
            {
                stagesToGenerate = new[] { StubCodeContext.Stage.Marshal, StubCodeContext.Stage.PinnedMarshal };
            }
            else
            {
                stagesToGenerate = new[] { StubCodeContext.Stage.Marshal, StubCodeContext.Stage.PinnedMarshal, StubCodeContext.Stage.CleanupCallerAllocated, StubCodeContext.Stage.CleanupCalleeAllocated };
            }

            return Block(
                setNumElements,
                unmanagedValuesSource,
                managedValuesDestination,
                GenerateContentsMarshallingStatement(
                    info,
                    context,
                    IdentifierName(numElementsIdentifier),
                    _elementInfo,
                    new FreeAlwaysOwnedOriginalValueGenerator(_elementMarshaller),
                    stagesToGenerate));
        }

        private static List<StatementSyntax> GenerateElementStages(
            TypePositionInfo info,
            StubCodeContext context,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo,
            out LinearCollectionElementMarshallingCodeContext elementSetupSubContext,
            out TypePositionInfo localElementInfo,
            params StubCodeContext.Stage[] stagesToGeneratePerElement)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            elementSetupSubContext = new LinearCollectionElementMarshallingCodeContext(
                StubCodeContext.Stage.Setup,
                managedSpanIdentifier,
                nativeSpanIdentifier,
                context);

            localElementInfo = elementInfo with
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
                elementStatements.AddRange(elementMarshaller.Generate(localElementInfo, elementSubContext));
            }
            return elementStatements;
        }

        private static StatementSyntax GenerateContentsMarshallingStatement(
            TypePositionInfo info,
            StubCodeContext context,
            ExpressionSyntax lengthExpression,
            TypePositionInfo elementInfo,
            IMarshallingGenerator elementMarshaller,
            params StubCodeContext.Stage[] stagesToGeneratePerElement)
        {
            var elementStatements = GenerateElementStages(info, context, elementMarshaller, elementInfo, out var elementSetupSubContext, out var localElementInfo, stagesToGeneratePerElement);

            if (elementStatements.Count != 0)
            {
                StatementSyntax marshallingStatement = Block(
                    List(elementMarshaller.Generate(localElementInfo, elementSetupSubContext)
                        .Concat(elementStatements)));

                if (elementMarshaller.AsNativeType(elementInfo).Syntax is PointerTypeSyntax elementNativeType)
                {
                    PointerNativeTypeAssignmentRewriter rewriter = new(elementSetupSubContext.GetIdentifiers(localElementInfo).native, elementNativeType);
                    marshallingStatement = (StatementSyntax)rewriter.Visit(marshallingStatement);
                }

                // Iterate through the elements of the native collection to marshal them
                var forLoop = ForLoop(elementSetupSubContext.IndexerIdentifier, lengthExpression)
                    .WithStatement(marshallingStatement);
                // If we're tracking LastIndexMarshalled, increment that each iteration as well.
                if (UsesLastIndexMarshalled(info, context) && stagesToGeneratePerElement.Contains(StubCodeContext.Stage.Marshal))
                {
                    forLoop = forLoop.AddIncrementors(
                        PrefixUnaryExpression(SyntaxKind.PreIncrementExpression,
                            IdentifierName(MarshallerHelpers.GetLastIndexMarshalledIdentifier(info, context))));
                }
                return forLoop;
            }

            return EmptyStatement();
        }

        private static bool UsesLastIndexMarshalled(TypePositionInfo info, StubCodeContext context)
        {
            bool shouldCleanupAllElements = ShouldCleanUpAllElements(info, context);
            if (shouldCleanupAllElements)
            {
                return false;
            }
            bool onlyUnmarshals = MarshallerHelpers.GetMarshalDirection(info, context) == MarshalDirection.UnmanagedToManaged;
            if (onlyUnmarshals)
            {
                return false;
            }
            return true;
        }

        private static bool ShouldCleanUpAllElements(TypePositionInfo info, StubCodeContext context)
        {
            _ = info;
            _ = context;
            // AdditionalTemporaryStateLivesAcrossStages implies that it is an outer collection
            // Out parameters means that the contents are created by the P/Invoke and assumed to have successfully created all elements
            return !context.AdditionalTemporaryStateLivesAcrossStages || info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out || info.RefKind == RefKind.Out || info.IsNativeReturnPosition;
        }

        public override StatementSyntax GenerateSetupStatement(TypePositionInfo info, StubCodeContext context)
            => UsesLastIndexMarshalled(info, context)
                ? LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(MarshallerHelpers.GetLastIndexMarshalledIdentifier(info, context)),
                            null,
                            EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))))
                : EmptyStatement();
    }
}
