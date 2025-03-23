// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        TypePositionInfo TypeInfo { get; }
        StubCodeContext CodeContext { get; }

        InvocationExpressionSyntax GetUnmanagedValuesDestination(StubIdentifierContext context);
        InvocationExpressionSyntax GetManagedValuesSource(StubIdentifierContext context);
        InvocationExpressionSyntax GetUnmanagedValuesSource(StubIdentifierContext context);
        InvocationExpressionSyntax GetManagedValuesDestination(StubIdentifierContext context);
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
        public StatementSyntax GenerateClearUnmanagedDestination(StubIdentifierContext context)

        {
            // <GetUnmanagedValuesDestination>.Clear();
            return MethodInvocationStatement(
                        CollectionSource.GetUnmanagedValuesDestination(context),
                        IdentifierName("Clear"));
        }
        /// <summary>
        /// <code>
        /// &lt; GetManagedValuesDestination &gt;.Clear();
        /// </code>
        /// </summary>
        public StatementSyntax GenerateClearManagedValuesDestination(StubIdentifierContext context)
        {
            // <GetManagedValuedDestination>.Clear();
            return MethodInvocationStatement(
                        CollectionSource.GetManagedValuesDestination(context),
                        IdentifierName("Clear"));
        }

        public static ExpressionSyntax GenerateNumElementsExpression(CountInfo count, bool countInfoRequiresCast, StubCodeContext codeContext, StubIdentifierContext context)
        {
            ExpressionSyntax numElementsExpression = count switch
            {
                SizeAndParamIndexInfo(int size, SizeAndParamIndexInfo.UnspecifiedParam) => GetConstSizeExpression(size),
                ConstSizeCountInfo(int size) => GetConstSizeExpression(size),
                SizeAndParamIndexInfo(SizeAndParamIndexInfo.UnspecifiedConstSize, TypePositionInfo param) => GetExpressionForParam(param),
                SizeAndParamIndexInfo(int size, TypePositionInfo param) => CheckedExpression(SyntaxKind.CheckedExpression,
                    BinaryExpression(SyntaxKind.AddExpression,
                        GetConstSizeExpression(size),
                        GetExpressionForParam(param))),
                CountElementCountInfo(TypePositionInfo elementInfo) => GetExpressionForParam(elementInfo),
                _ => throw new UnreachableException("Count info should have been verified in generator resolution")
            };

            if (countInfoRequiresCast)
            {
                if (numElementsExpression.IsKind(SyntaxKind.CheckedExpression))
                {
                    numElementsExpression = ((CheckedExpressionSyntax)numElementsExpression).Expression;
                }
                numElementsExpression = CheckedExpression(SyntaxKind.CheckedExpression,
                    CastExpression(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        ParenthesizedExpression(numElementsExpression)));
            }

            return numElementsExpression;

            static LiteralExpressionSyntax GetConstSizeExpression(int size)
            {
                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(size));
            }

            ExpressionSyntax GetExpressionForParam(TypePositionInfo paramInfo)
            {
                return MarshallerHelpers.GetIndexedManagedElementExpression(paramInfo, codeContext, context);
            }
        }

        public abstract StatementSyntax GenerateSetupStatement(StubIdentifierContext context);
        public abstract StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(StubIdentifierContext context);
        public abstract StatementSyntax GenerateMarshalStatement(StubIdentifierContext context);
        public abstract StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(StubIdentifierContext context);

        public abstract StatementSyntax GenerateUnmarshalStatement(StubIdentifierContext context);
        public abstract StatementSyntax GenerateElementCleanupStatement(StubIdentifierContext context);
    }

    file static class ElementsMarshallingCollectionSourceExtensions
    {
        public static StatementSyntax GetNumElementsAssignmentFromManagedValuesSource(this IElementsMarshallingCollectionSource source, TypePositionInfo info, StubIdentifierContext context)
        {
            var numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            // <numElements> = <GetManagedValuesSource>.Length;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source.GetManagedValuesSource(context),
                        IdentifierName("Length"))));
        }

        public static StatementSyntax GetNumElementsAssignmentFromManagedValuesDestination(this IElementsMarshallingCollectionSource source, TypePositionInfo info, StubIdentifierContext context)
        {
            var numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            // <numElements> = <GetManagedValuesDestination>.Length;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        source.GetManagedValuesDestination(context),
                        IdentifierName("Length"))));
        }
    }

    /// <summary>
    /// Support for marshalling blittable elements
    /// </summary>
    internal sealed class BlittableElementsMarshalling(
        TypeSyntax managedElementType,
        TypeSyntax unmanagedElementType,
        IElementsMarshallingCollectionSource collectionSource) : ElementsMarshalling(collectionSource)
    {
        public override StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(StubIdentifierContext context)
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
                            Argument(CollectionSource.GetUnmanagedValuesSource(context)))),
                    Argument(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            CollectionSource.GetUnmanagedValuesSource(context),
                            IdentifierName("Length")))));

            // <GetManagedValuesDestination>.CopyTo(<source>);
            return MethodInvocationStatement(
                    CollectionSource.GetManagedValuesDestination(context),
                    IdentifierName("CopyTo"),
                    Argument(destination));
        }

        public override StatementSyntax GenerateMarshalStatement(StubIdentifierContext context)
        {
            ExpressionSyntax destination = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesDestination(context));

            // <GetManagedValuesSource>.CopyTo(<destination>);
            return MethodInvocationStatement(
                    CollectionSource.GetManagedValuesSource(context),
                    IdentifierName("CopyTo"),
                    Argument(destination));
        }

        public override StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(StubIdentifierContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesDestination(context));

            // MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(<GetManagedValuesSource>), <GetManagedValuesSource>.Length)
            ExpressionSyntax destination = MethodInvocation(
                    TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                    IdentifierName("CreateSpan"),
                        RefArgument(
                            MethodInvocation(
                                TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                                IdentifierName("GetReference"),
                                Argument(CollectionSource.GetManagedValuesSource(context)))),
                        Argument(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                CollectionSource.GetManagedValuesSource(context),
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

        public override StatementSyntax GenerateUnmarshalStatement(StubIdentifierContext context)
        {
            ExpressionSyntax source = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesSource(context));

            // <source>.CopyTo(<GetManagedValuesDestination>);
            return MethodInvocationStatement(
                    source,
                    IdentifierName("CopyTo"),
                    Argument(CollectionSource.GetManagedValuesDestination(context)));
        }

        private ExpressionSyntax CastToManagedIfNecessary(ExpressionSyntax expression)
        {
            // Skip the cast if the managed and unmanaged element types are the same
            if (unmanagedElementType.IsEquivalentTo(managedElementType))
                return expression;

            // MemoryMarshal.Cast<<unmanagedElementType>, <elementType>>(<expression>)
            return MethodInvocation(
                    TypeSyntaxes.System_Runtime_InteropServices_MemoryMarshal,
                    GenericName(
                        Identifier("Cast"),
                        TypeArgumentList(SeparatedList(new[]
                            {
                                unmanagedElementType,
                                managedElementType
                            }))),
                    Argument(expression));
        }

        public override StatementSyntax GenerateElementCleanupStatement(StubIdentifierContext context) => EmptyStatement();
        public override StatementSyntax GenerateSetupStatement(StubIdentifierContext context) => EmptyStatement();
    }

    /// <summary>
    /// Support for marshalling non-blittable elements
    /// </summary>
    internal sealed class NonBlittableElementsMarshalling(
        TypeSyntax unmanagedElementType,
        IBoundMarshallingGenerator elementMarshaller,
        IElementsMarshallingCollectionSource collectionSource) : ElementsMarshalling(collectionSource)
    {
        public override StatementSyntax GenerateMarshalStatement(StubIdentifierContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);

            // ReadOnlySpan<T> <managedSpan> = <GetManagedValuesSource>
            // Span<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesDestination>
            // <if multidimensional collection> <nativeSpan>.Clear()
            // << marshal contents >>
            var statements = new List<StatementSyntax>()
            {
                Declare(
                    ReadOnlySpanOf(elementMarshaller.TypeInfo.ManagedType.Syntax),
                    managedSpanIdentifier,
                    CollectionSource.GetManagedValuesSource(context)),
                Declare(
                    SpanOf(unmanagedElementType),
                    nativeSpanIdentifier,
                    CollectionSource.GetUnmanagedValuesDestination(context))
            };
            // If it is a multidimensional array, we will just clear each allocated span.
            if (ShouldCleanUpAllElements(CollectionSource.TypeInfo, CollectionSource.CodeContext))
            {
                // <nativeSpanIdentifier>.Clear()
                statements.Add(MethodInvocationStatement(
                            IdentifierName(nativeSpanIdentifier),
                            IdentifierName("Clear")));
            }
            statements.Add(GenerateContentsMarshallingStatement(
                    context,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context)),
                        IdentifierName("Length")),
                    elementMarshaller,
                    StubIdentifierContext.Stage.Marshal));
            return Block(statements);
        }

        public override StatementSyntax GenerateUnmarshalStatement(StubIdentifierContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(CollectionSource.TypeInfo, context);

            // ReadOnlySpan<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesSource>
            // Span<T> <managedSpan> = <GetManagedValuesDestination>
            // << unmarshal contents >>
            return Block(
                Declare(
                    ReadOnlySpanOf(unmanagedElementType),
                    nativeSpanIdentifier,
                    CollectionSource.GetUnmanagedValuesSource(context)),
                Declare(
                    SpanOf(elementMarshaller.TypeInfo.ManagedType.Syntax),
                    managedSpanIdentifier,
                    CollectionSource.GetManagedValuesDestination(context)),
                GenerateContentsMarshallingStatement(
                    context,
                    IdentifierName(numElementsIdentifier),
                    elementMarshaller,
                    StubIdentifierContext.Stage.UnmarshalCapture, StubIdentifierContext.Stage.Unmarshal));
        }

        public override StatementSyntax GenerateManagedToUnmanagedByValueOutUnmarshalStatement(StubIdentifierContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(CollectionSource.TypeInfo, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);

            var setNumElements = CollectionSource.GetNumElementsAssignmentFromManagedValuesSource(CollectionSource.TypeInfo, context);

            // Span<TElement> <managedSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <GetManagedValuesSource>.GetPinnableReference(), <numElements>));
            LocalDeclarationStatementSyntax managedValuesDeclaration = Declare(SpanOf(elementMarshaller.TypeInfo.ManagedType.Syntax),
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
                                CollectionSource.GetManagedValuesSource(context),
                                IdentifierName("GetPinnableReference"))))),
                Argument(IdentifierName(numElementsIdentifier))));

            // Span<TUnmanagedElement> <nativeSpan> = <GetUnmanagedValuesDestination>
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);
            LocalDeclarationStatementSyntax unmanagedValuesDeclaration = Declare(
                SpanOf(unmanagedElementType),
                nativeSpanIdentifier,
                CollectionSource.GetUnmanagedValuesDestination(context));

            return Block(
                setNumElements,
                managedValuesDeclaration,
                unmanagedValuesDeclaration,
                GenerateContentsMarshallingStatement(
                    context,
                    IdentifierName(numElementsIdentifier),
                    elementMarshaller,
                    StubIdentifierContext.Stage.UnmarshalCapture, StubIdentifierContext.Stage.Unmarshal));
        }

        public override StatementSyntax GenerateElementCleanupStatement(StubIdentifierContext context)
        {
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);
            ExpressionSyntax indexConstraintName;
            if (!UsesLastIndexMarshalled(CollectionSource.TypeInfo, CollectionSource.CodeContext))
            {
                indexConstraintName = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(nativeSpanIdentifier),
                        IdentifierName("Length"));
            }
            else
            {
                indexConstraintName = IdentifierName(MarshallerHelpers.GetLastIndexMarshalledIdentifier(CollectionSource.TypeInfo, context));
            }
            StatementSyntax contentsCleanupStatements = GenerateContentsMarshallingStatement(
                context,
                indexConstraintName,
                elementMarshaller,
                context.CurrentStage);

            if (contentsCleanupStatements.IsKind(SyntaxKind.EmptyStatement))
            {
                if (UsesLastIndexMarshalled(CollectionSource.TypeInfo, CollectionSource.CodeContext))
                {
                    return AssignmentStatement(
                            IdentifierName("_"),
                            IdentifierName(MarshallerHelpers.GetLastIndexMarshalledIdentifier(CollectionSource.TypeInfo, context)));
                }
                return EmptyStatement();
            }

            return Block(
                Declare(
                    ReadOnlySpanOf(unmanagedElementType),
                    nativeSpanIdentifier,
                    MarshallerHelpers.GetMarshalDirection(CollectionSource.TypeInfo, CollectionSource.CodeContext) == MarshalDirection.ManagedToUnmanaged
                        ? CollectionSource.GetUnmanagedValuesDestination(context)
                        : CollectionSource.GetUnmanagedValuesSource(context)),
                contentsCleanupStatements);
        }

        public override StatementSyntax GenerateUnmanagedToManagedByValueOutMarshalStatement(StubIdentifierContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(CollectionSource.TypeInfo, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);

            var setNumElements = CollectionSource.GetNumElementsAssignmentFromManagedValuesDestination(CollectionSource.TypeInfo, context);

            // Span<TUnmanagedElement> <nativeSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in <GetUnmanagedValuesSource>.GetPinnableReference()), <numElements>);
            LocalDeclarationStatementSyntax unmanagedValuesSource = Declare(
                SpanOf(unmanagedElementType),
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
                                    CollectionSource.GetUnmanagedValuesSource(context),
                                    IdentifierName("GetPinnableReference"))))),
                    Argument(IdentifierName(numElementsIdentifier))));

            // Span<TElement> <managedSpan> = <GetManagedValuesDestination>
            LocalDeclarationStatementSyntax managedValuesDestination = LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(SingletonSeparatedList(elementMarshaller.TypeInfo.ManagedType.Syntax))),
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(managedSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                        CollectionSource.GetManagedValuesDestination(context))))));

            StubIdentifierContext.Stage[] stagesToGenerate;

            // Until we separate CalleeAllocated cleanup and CallerAllocated cleanup in unmanaged to managed, we'll need this hack
            if (CollectionSource.CodeContext.Direction is MarshalDirection.UnmanagedToManaged && CollectionSource.TypeInfo.ByValueContentsMarshalKind is ByValueContentsMarshalKind.Out)
            {
                stagesToGenerate = [StubIdentifierContext.Stage.Marshal, StubIdentifierContext.Stage.PinnedMarshal];
            }
            else
            {
                stagesToGenerate = [StubIdentifierContext.Stage.Marshal, StubIdentifierContext.Stage.PinnedMarshal, StubIdentifierContext.Stage.CleanupCallerAllocated, StubIdentifierContext.Stage.CleanupCalleeAllocated];
            }

            return Block(
                setNumElements,
                unmanagedValuesSource,
                managedValuesDestination,
                GenerateContentsMarshallingStatement(
                    context,
                    IdentifierName(numElementsIdentifier),
                    new FreeAlwaysOwnedOriginalValueGenerator(elementMarshaller),
                    stagesToGenerate));
        }

        private List<StatementSyntax> GenerateElementStages(
            StubIdentifierContext context,
            IBoundMarshallingGenerator elementMarshaller,
            out string indexer,
            params StubIdentifierContext.Stage[] stagesToGeneratePerElement)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);
            StubCodeContext elementCodeContext = StubCodeContext.CreateElementMarshallingContext(CollectionSource.CodeContext);
            LinearCollectionElementIdentifierContext elementSetupSubContext = new(
                context,
                elementMarshaller.TypeInfo,
                managedSpanIdentifier,
                nativeSpanIdentifier,
                elementCodeContext.ElementIndirectionLevel)
            {
                CurrentStage = StubIdentifierContext.Stage.Setup,
                CodeEmitOptions = context.CodeEmitOptions
            };

            indexer = elementSetupSubContext.IndexerIdentifier;

            StubIdentifierContext identifierContext = elementSetupSubContext;

            if (elementMarshaller.NativeType is PointerTypeInfo)
            {
                identifierContext = new GenericFriendlyPointerIdentifierContext(elementSetupSubContext, elementMarshaller.TypeInfo, $"{nativeSpanIdentifier}__{indexer}")
                {
                    CodeEmitOptions = elementSetupSubContext.CodeEmitOptions,
                };
            }

            List<StatementSyntax> elementStatements = [];
            foreach (StubIdentifierContext.Stage stage in stagesToGeneratePerElement)
            {
                var elementIdentifierContext = identifierContext with { CurrentStage = stage };
                elementStatements.AddRange(elementMarshaller.Generate(elementIdentifierContext));
            }

            if (elementStatements.Count == 0)
            {
                return [];
            }

            // Only add the setup stage if we generated code for other stages.
            elementStatements.InsertRange(0, elementMarshaller.Generate(identifierContext with { CurrentStage = StubIdentifierContext.Stage.Setup }));

            if (identifierContext is not GenericFriendlyPointerIdentifierContext)
            {
                // If we didn't need to account for pointer types, we have the statements we need.
                return elementStatements;
            }

            // If we have the generic friendly pointer context, we need to declare the special identifier and assign to/from it.

            // <native_type> <native_exactType> = (<native_type>)<native_collection>[i];
            StatementSyntax exactTypeDeclaration =
                LocalDeclarationStatement(
                    VariableDeclaration(
                        elementMarshaller.NativeType.Syntax,
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(identifierContext.GetIdentifiers(elementMarshaller.TypeInfo).native))
                            .WithInitializer(
                                EqualsValueClause(
                                    CastExpression(elementMarshaller.NativeType.Syntax,
                                        ParseExpression(elementSetupSubContext.GetIdentifiers(elementMarshaller.TypeInfo).native)))))));

            if (stagesToGeneratePerElement.Any(stage => stage is StubIdentifierContext.Stage.Marshal or StubIdentifierContext.Stage.PinnedMarshal))
            {
                // <native_collection>[i] = (<generic_compatible_native_type>)<native_exactType>;
                StatementSyntax propagateResult = AssignmentStatement(
                    ParseExpression(elementSetupSubContext.GetIdentifiers(elementMarshaller.TypeInfo).native),
                    CastExpression(TypeSyntaxes.System_IntPtr,
                        IdentifierName(identifierContext.GetIdentifiers(elementMarshaller.TypeInfo).native)));

                return
                    [
                        exactTypeDeclaration,
                            ..elementStatements,
                            propagateResult
                    ];
            }

            return [
                exactTypeDeclaration,
                    ..elementStatements
                ];
        }

        private StatementSyntax GenerateContentsMarshallingStatement(
            StubIdentifierContext context,
            ExpressionSyntax lengthExpression,
            IBoundMarshallingGenerator elementMarshaller,
            params StubIdentifierContext.Stage[] stagesToGeneratePerElement)
        {
            var elementStatements = GenerateElementStages(context, elementMarshaller, out string indexer, stagesToGeneratePerElement);

            if (elementStatements.Count != 0)
            {
                StatementSyntax marshallingStatement = Block(elementStatements);

                // Iterate through the elements of the native collection to marshal them
                var forLoop = ForLoop(indexer, lengthExpression)
                    .WithStatement(marshallingStatement);
                // If we're tracking LastIndexMarshalled, increment that each iteration as well.
                if (UsesLastIndexMarshalled(CollectionSource.TypeInfo, CollectionSource.CodeContext) && stagesToGeneratePerElement.Contains(StubIdentifierContext.Stage.Marshal))
                {
                    forLoop = forLoop.AddIncrementors(
                        PrefixUnaryExpression(SyntaxKind.PreIncrementExpression,
                            IdentifierName(MarshallerHelpers.GetLastIndexMarshalledIdentifier(CollectionSource.TypeInfo, context))));
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
            // ElementIndirectionLevel != 0 means that we are in a collection
            // Out parameters means that the contents are created by the P/Invoke and assumed to have successfully created all elements
            return context.ElementIndirectionLevel != 0 || info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out || info.RefKind == RefKind.Out || info.IsNativeReturnPosition;
        }

        public override StatementSyntax GenerateSetupStatement(StubIdentifierContext context)
            => UsesLastIndexMarshalled(CollectionSource.TypeInfo, CollectionSource.CodeContext)
                ? LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(MarshallerHelpers.GetLastIndexMarshalledIdentifier(CollectionSource.TypeInfo, context)),
                            null,
                            EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))))
                : EmptyStatement();
    }
}
