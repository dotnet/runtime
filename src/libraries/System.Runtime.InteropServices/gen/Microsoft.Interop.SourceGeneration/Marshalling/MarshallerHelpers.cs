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
    public static class MarshallerHelpers
    {
        public static readonly TypeSyntax SystemIntPtrType = TypeSyntaxes.System_IntPtr;

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

        /// <summary>
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/> = default;</code>
        /// or
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/>;</code>
        /// </summary>
        public static LocalDeclarationStatementSyntax Declare(TypeSyntax typeSyntax, string identifier, bool initializeToDefault)
        {
            return Declare(typeSyntax, identifier, initializeToDefault ? LiteralExpression(SyntaxKind.DefaultLiteralExpression) : null);
        }

        /// <summary>
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/> = <paramref name="identifier"/>;</code>
        /// or
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/>;</code>
        /// </summary>
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
                spanElementTypeSyntax = TypeSyntaxes.System_IntPtr;
            }
            return spanElementTypeSyntax;
        }


        // Marshal.SetLastSystemError(<errorCode>);
        public static StatementSyntax CreateClearLastSystemErrorStatement(int errorCode) =>
            ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        TypeSyntaxes.System_Runtime_InteropServices_Marshal,
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
                        TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                        IdentifierName("GetLastSystemError")))));

        // Marshal.SetLastPInvokeError(<lastError>);
        public static StatementSyntax CreateSetLastPInvokeErrorStatement(string lastErrorIdentifier) =>
            ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        TypeSyntaxes.System_Runtime_InteropServices_Marshal,
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

        public static string GetNumElementsIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, "numElements");
        }

        public static string GetLastIndexMarshalledIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, "lastIndexMarshalled");
        }

        internal static bool CanUseCallerAllocatedBuffer(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && (!info.IsByRef || info.RefKind == RefKind.In);
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
            if (elementMarshallingInfo is NativeLinearCollectionMarshallingInfo nestedCollection)
            {
                if (nestedCollection.ElementCountInfo is CountElementCountInfo { ElementInfo: TypePositionInfo nestedCountElement })
                {
                    // Do not include dependent elements with no managed or native index.
                    // These values are dummy values that are inserted earlier to avoid emitting extra diagnostics.
                    if (nestedCountElement.ManagedIndex != TypePositionInfo.UnsetIndex || nestedCountElement.NativeIndex != TypePositionInfo.UnsetIndex)
                    {
                        yield return nestedCountElement;
                    }
                }
                foreach (KeyValuePair<MarshalMode, CustomTypeMarshallerData> mode in nestedCollection.Marshallers.Modes)
                {
                    foreach (TypePositionInfo nestedElement in GetDependentElementsOfMarshallingInfo(mode.Value.CollectionElementMarshallingInfo))
                    {
                        if (nestedElement.ManagedIndex != TypePositionInfo.UnsetIndex || nestedElement.NativeIndex != TypePositionInfo.UnsetIndex)
                        {
                            yield return nestedElement;
                        }
                    }
                }
            }
        }

        public static StatementSyntax SkipInitOrDefaultInit(TypePositionInfo info, StubCodeContext context)
        {
            (TargetFramework fmk, _) = context.GetTargetFramework();
            if (info.ManagedType is not PointerTypeInfo
                && info.ManagedType is not ValueTypeInfo { IsByRefLike: true }
                && fmk is TargetFramework.Net)
            {
                // Use the Unsafe.SkipInit<T> API when available and
                // managed type is usable as a generic parameter.
                return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                            IdentifierName("SkipInit")))
                    .WithArgumentList(
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(info.InstanceIdentifier))
                            .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))));
            }
            else
            {
                // Assign out params to default
                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(info.InstanceIdentifier),
                        LiteralExpression(
                            SyntaxKind.DefaultLiteralExpression,
                            Token(SyntaxKind.DefaultKeyword))));
            }
        }

        /// <summary>
        /// Get the marshalling direction for a given <see cref="TypePositionInfo"/> in a given <see cref="StubCodeContext"/>.
        /// For example, an out parameter is marshalled in the <see cref="MarshalDirection.UnmanagedToManaged"/> direction in a <see cref="MarshalDirection.ManagedToUnmanaged"/> stub,
        /// but from <see cref="MarshalDirection.ManagedToUnmanaged"/> in a <see cref="MarshalDirection.UnmanagedToManaged"/> stub.
        /// </summary>
        /// <param name="info">The info for an element.</param>
        /// <param name="context">The context for the stub.</param>
        /// <returns>The direction the element is marshalled.</returns>
        public static MarshalDirection GetMarshalDirection(TypePositionInfo info, StubCodeContext context)
        {
            if (context.Direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.UnmanagedToManaged))
            {
                throw new ArgumentException("Stub context direction must not be bidirectional.");
            }

            if (context.Direction == MarshalDirection.ManagedToUnmanaged)
            {
                if (info.IsManagedReturnPosition)
                {
                    return MarshalDirection.UnmanagedToManaged;
                }
                if (!info.IsByRef)
                {
                    return MarshalDirection.ManagedToUnmanaged;
                }
                switch (info.RefKind)
                {
                    case RefKind.In:
                        return MarshalDirection.ManagedToUnmanaged;
                    case RefKind.Ref:
                        return MarshalDirection.Bidirectional;
                    case RefKind.Out:
                        return MarshalDirection.UnmanagedToManaged;
                }
                throw new UnreachableException("An element is either a return value or passed by value or by ref.");
            }


            if (info.IsNativeReturnPosition)
            {
                return MarshalDirection.ManagedToUnmanaged;
            }
            if (!info.IsByRef)
            {
                return MarshalDirection.UnmanagedToManaged;
            }
            switch (info.RefKind)
            {
                case RefKind.In:
                    return MarshalDirection.UnmanagedToManaged;
                case RefKind.Ref:
                    return MarshalDirection.Bidirectional;
                case RefKind.Out:
                    return MarshalDirection.ManagedToUnmanaged;
            }
            throw new UnreachableException("An element is either a return value or passed by value or by ref.");
        }

        /// <summary>
        /// Returns which stage cleanup should be performed for the parameter.
        /// </summary>
        public static StubCodeContext.Stage GetCleanupStage(TypePositionInfo info, StubCodeContext context)
        {
            // Unmanaged to managed doesn't properly handle lifetimes right now and will default to the original behavior.
            // Failures will only occur when marshalling fails, and would only cause leaks, not double frees.
            // See https://github.com/dotnet/runtime/issues/89483 for more details
            if (context.Direction is MarshalDirection.UnmanagedToManaged)
                return StubCodeContext.Stage.CleanupCallerAllocated;

            return GetMarshalDirection(info, context) switch
            {
                MarshalDirection.UnmanagedToManaged => StubCodeContext.Stage.CleanupCalleeAllocated,
                MarshalDirection.ManagedToUnmanaged => StubCodeContext.Stage.CleanupCallerAllocated,
                MarshalDirection.Bidirectional => StubCodeContext.Stage.CleanupCallerAllocated,
                _ => throw new UnreachableException()
            };
        }

        /// <summary>
        /// Ensure that the count of a collection is available at call time if the parameter is not an out parameter.
        /// It only looks at an indirection level of 0 (the size of the outer array), so there are some holes in
        /// analysis if the parameter is a multidimensional array, but that case seems very unlikely to be hit.
        /// </summary>
        public static void ValidateCountInfoAvailableAtCall(MarshalDirection stubDirection, TypePositionInfo info, GeneratorDiagnosticsBag generatorDiagnostics, IMethodSymbol symbol, DiagnosticDescriptor outParamDescriptor, DiagnosticDescriptor returnValueDescriptor)
        {
            // In managed to unmanaged stubs, we can always just get the length of managed object
            // We only really need to be concerned about unmanaged to managed stubs
            if (stubDirection is MarshalDirection.ManagedToUnmanaged)
                return;

            if (!(info.RefKind is RefKind.Out
                    || info.ManagedIndex is TypePositionInfo.ReturnIndex)
                && info.MarshallingAttributeInfo is NativeLinearCollectionMarshallingInfo collectionMarshallingInfo
                && collectionMarshallingInfo.ElementCountInfo is CountElementCountInfo countInfo)
            {
                if (countInfo.ElementInfo.IsByRef && countInfo.ElementInfo.RefKind is RefKind.Out)
                {
                    Location location = TypePositionInfo.GetLocation(info, symbol);
                    generatorDiagnostics.ReportDiagnostic(
                        DiagnosticInfo.Create(
                            outParamDescriptor,
                            location,
                            info.InstanceIdentifier,
                            countInfo.ElementInfo.InstanceIdentifier));
                }
                else if (countInfo.ElementInfo.ManagedIndex is TypePositionInfo.ReturnIndex)
                {
                    Location location = TypePositionInfo.GetLocation(info, symbol);
                    generatorDiagnostics.ReportDiagnostic(
                        DiagnosticInfo.Create(
                            returnValueDescriptor,
                            location,
                            info.InstanceIdentifier));
                }
                // If the parameter is multidimensional and a higher indirection level parameter is ByValue [Out], then we should warn.
            }
        }
    }
}
