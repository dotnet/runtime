// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public static class MarshallerHelpers
    {
        public static readonly TypeSyntax SystemIntPtrType = ParseTypeName(TypeNames.System_IntPtr);

        public static ForStatementSyntax GetForLoop(ExpressionSyntax lengthExpression, string indexerIdentifier)
        {
            // for(int <indexerIdentifier> = 0; <indexerIdentifier> < <lengthIdentifier>; ++<indexerIdentifier>)
            //      ;
            return ForStatement(EmptyStatement())
            .WithDeclaration(
                VariableDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(indexerIdentifier))
                        .WithInitializer(
                            EqualsValueClause(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))))))
            .WithCondition(
                BinaryExpression(
                    SyntaxKind.LessThanExpression,
                    IdentifierName(indexerIdentifier),
                    lengthExpression))
            .WithIncrementors(
                SingletonSeparatedList<ExpressionSyntax>(
                    PrefixUnaryExpression(
                        SyntaxKind.PreIncrementExpression,
                        IdentifierName(indexerIdentifier))));
        }

        public static LocalDeclarationStatementSyntax Declare(TypeSyntax typeSyntax, string identifier, bool initializeToDefault)
        {
            return Declare(typeSyntax, identifier, initializeToDefault ? LiteralExpression(SyntaxKind.DefaultLiteralExpression) : null);
        }

        public static LocalDeclarationStatementSyntax Declare(TypeSyntax typeSyntax, string identifier, ExpressionSyntax? initializer)
        {
            VariableDeclaratorSyntax decl = VariableDeclarator(identifier);
            if (initializer is not null)
            {
                decl = decl.WithInitializer(
                    EqualsValueClause(
                        initializer));
            }

            // <type> <identifier>;
            // or
            // <type> <identifier> = <initializer>;
            return LocalDeclarationStatement(
                VariableDeclaration(
                    typeSyntax,
                    SingletonSeparatedList(decl)));
        }

        public static RefKind GetRefKindForByValueContentsKind(this ByValueContentsMarshalKind byValue)
        {
            return byValue switch
            {
                ByValueContentsMarshalKind.Default => RefKind.None,
                ByValueContentsMarshalKind.In => RefKind.In,
                ByValueContentsMarshalKind.InOut => RefKind.Ref,
                ByValueContentsMarshalKind.Out => RefKind.Out,
                _ => throw new System.ArgumentOutOfRangeException(nameof(byValue))
            };
        }

        public static TypeSyntax GetCompatibleGenericTypeParameterSyntax(this TypeSyntax type)
        {
            TypeSyntax spanElementTypeSyntax = type;
            if (spanElementTypeSyntax is PointerTypeSyntax)
            {
                // Pointers cannot be passed to generics, so use IntPtr for this case.
                spanElementTypeSyntax = SystemIntPtrType;
            }
            return spanElementTypeSyntax;
        }


        // Marshal.SetLastSystemError(<errorCode>);
        public static StatementSyntax CreateClearLastSystemErrorStatement(int errorCode) =>
            ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseName(TypeNames.System_Runtime_InteropServices_Marshal),
                        IdentifierName("SetLastSystemError")),
                    ArgumentList(SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(errorCode)))))));

        // <lastError> = Marshal.GetLastSystemError();
        public static StatementSyntax CreateGetLastSystemErrorStatement(string lastErrorIdentifier) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(lastErrorIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseName(TypeNames.System_Runtime_InteropServices_Marshal),
                        IdentifierName("GetLastSystemError")))));

        // Marshal.SetLastPInvokeError(<lastError>);
        public static StatementSyntax CreateSetLastPInvokeErrorStatement(string lastErrorIdentifier) =>
            ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseName(TypeNames.System_Runtime_InteropServices_Marshal),
                        IdentifierName("SetLastPInvokeError")),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(lastErrorIdentifier))))));

        public static string GetMarshallerIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, "marshaller");
        }

        public static string GetManagedSpanIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, "managedSpan");
        }

        public static string GetNativeSpanIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, "nativeSpan");
        }

        /// <summary>
        /// Generate a topologically sorted collection of elements.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="elements">The initial collection of elements.</param>
        /// <param name="keyFn">A function to create a key for the element.</param>
        /// <param name="getDependentIndicesFn">A function to resolve the dependencies of a given item in the <paramref name="elements"/> collection as index values that would be returned by <paramref name="keyFn"/>.</param>
        /// <returns>A topologically sorted collection of the elemens of the <paramref name="elements"/> collection.</returns>
        /// <exception cref="InvalidOperationException">The graph of <paramref name="elements"/> nodes and the edges produced by <paramref name="getDependentIndicesFn"/> has cycles.</exception>
        public static IEnumerable<T> GetTopologicallySortedElements<T, U>(
            ICollection<T> elements,
            Func<T, U> keyFn,
            Func<T, IEnumerable<U>> getDependentIndicesFn)
        {
            Dictionary<U, int> elementIndexToEdgeMapNodeId = new(elements.Count);
            List<T> nodeIdToElement = new(elements.Count);
            EdgeMap edgeMap = new(elements.Count);

            int nextEdgeMapIndex = 0;
            foreach (T element in elements)
            {
                elementIndexToEdgeMapNodeId.Add(keyFn(element), nextEdgeMapIndex++);
                nodeIdToElement.Add(element);
            }

            foreach (T element in elements)
            {
                U elementIndex = keyFn(element);
                foreach (U dependentElementIndex in getDependentIndicesFn(element))
                {
                    // Add an edge from the node for dependentElementIndex-> the node for elementIndex
                    // This way, elements that have no dependencies have no edges pointing to them.
                    edgeMap[elementIndexToEdgeMapNodeId[elementIndex], elementIndexToEdgeMapNodeId[dependentElementIndex]] = true;
                }
            }

            // Now that we have initialized our map of edges and we have our list of nodes,
            // we'll use Khan's algorithm to calculate a topological sort of the elements.
            // Algorithm adapted from A. B. Kahn. 1962. Topological sorting of large networks. Commun. ACM 5, 11 (Nov. 1962), 558-562. DOI:https://doi.org/10.1145/368996.369025

            // L is the sorted list
            List<T> L = new(elements.Count);
            // S is the set of elements with no incoming edges (no dependencies on it)
            List<T> S = new(elements.Count);

            // Initialize S
            for (int node = 0; node < nodeIdToElement.Count; node++)
            {
                if (!edgeMap.AnyIncomingEdge(node))
                {
                    S.Add(nodeIdToElement[node]);
                }
            }

            while (S.Count != 0)
            {
                // Remove element from S
                T element = S[S.Count - 1];
                S.RemoveAt(S.Count - 1);
                // Add element to L
                L.Add(element);
                int n = elementIndexToEdgeMapNodeId[keyFn(element)];
                // For each node m that n points to
                for (int m = 0; m < edgeMap.NodeCount; m++)
                {
                    if (!edgeMap[m, n])
                    {
                        continue;
                    }
                    // Remove the edge from n to m
                    edgeMap[m, n] = false;
                    // If m does not have any incoming edges, add to S
                    if (!edgeMap.AnyIncomingEdge(m))
                    {
                        S.Add(nodeIdToElement[m]);
                    }
                }
            }

            // If we have edges left, then we have a cycle.
            if (edgeMap.AnyEdges)
            {
                throw new InvalidOperationException(SR.GraphHasCycles);
            }

            // If we make it here, we have a topologically sorted list.
            return L;
        }

        private struct EdgeMap
        {
            private readonly bool[] _edgeMap;

            public EdgeMap(int numNodes)
            {
                NodeCount = numNodes;
                _edgeMap = new bool[NodeCount * NodeCount];
            }

            /// <summary>
            /// EdgeMap contains a map of boolean values denoting if an edge exists
            /// If edgeMap[X][Y] is true, that means that there exists an edge Y -> X
            /// </summary>
            /// <param name="to">The node the edge points to.</param>
            /// <param name="from">The node the edge start at.</param>
            /// <returns>If there exists an edge that starts at <paramref name="from"/> and ends at <paramref name="to"/></returns>
            public bool this[int to, int from]
            {
                get => _edgeMap[to * NodeCount + from];
                set => _edgeMap[to * NodeCount + from] = value;
            }

            public bool AnyEdges => Array.IndexOf(_edgeMap, true) != -1;

            public int NodeCount { get; }

            public bool AnyIncomingEdge(int to)
            {
                return Array.IndexOf(_edgeMap, true, to * NodeCount, NodeCount) != -1;
            }
        }

        public static IEnumerable<TypePositionInfo> GetDependentElementsOfMarshallingInfo(
            MarshallingInfo elementMarshallingInfo)
        {
            if (elementMarshallingInfo is NativeLinearCollectionMarshallingInfo_V1 nestedCollection)
            {
                if (nestedCollection.ElementCountInfo is CountElementCountInfo { ElementInfo: TypePositionInfo nestedCountElement })
                {
                    yield return nestedCountElement;
                }
                foreach (TypePositionInfo nestedElements in GetDependentElementsOfMarshallingInfo(nestedCollection.ElementMarshallingInfo))
                {
                    yield return nestedElements;
                }
            }
        }

        public static class LinearCollection
        {
            public static StatementSyntax NonBlittableContentsMarshallingStatement(
                TypePositionInfo info,
                StubCodeContext context,
                TypePositionInfo elementInfo,
                IMarshallingGenerator elementMarshaller,
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

                TypePositionInfo localElementInfo = elementInfo with
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

                if (elementStatements.Any())
                {
                    StatementSyntax marshallingStatement = Block(
                        List(elementMarshaller.Generate(localElementInfo, elementSetupSubContext)
                            .Concat(elementStatements)));

                    if (elementMarshaller.AsNativeType(elementInfo) is PointerTypeSyntax elementNativeType)
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
}
