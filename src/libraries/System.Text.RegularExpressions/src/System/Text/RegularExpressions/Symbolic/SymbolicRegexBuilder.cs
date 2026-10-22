// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Builder of symbolic regexes over TSet.
    /// TSet is the type of the set of elements.
    /// Used to convert .NET regexes to symbolic regexes.
    /// </summary>
    internal sealed class SymbolicRegexBuilder<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        private const int InitialNodeCapacity = 16;

        internal readonly CharSetSolver _charSetSolver;
        internal readonly ISolver<TSet> _solver;

        private readonly int _nothingId;
        private readonly int _anyCharId;
        private readonly int _anyStarId;
        private readonly int _anyStarLazyId;

        internal SymbolicRegexNode<TSet> _nothing => GetNode(_nothingId);
        internal SymbolicRegexNode<TSet> _anyChar => GetNode(_anyCharId);
        internal SymbolicRegexNode<TSet> _anyStar => GetNode(_anyStarId);
        internal SymbolicRegexNode<TSet> _anyStarLazy => GetNode(_anyStarLazyId);

        private int _epsilonId;
        private int _beginningAnchorId;
        private int _endAnchorId;
        private int _endAnchorZId;
        private int _endAnchorZReverseId;
        private int _bolAnchorId;
        private int _eolAnchorId;
        private int _boundaryAnchorId;
        private int _nonBoundaryAnchorId;

        internal SymbolicRegexNode<TSet> Epsilon => GetOrCreateNode(ref _epsilonId, SymbolicRegexNodeKind.Epsilon);
        internal SymbolicRegexNode<TSet> BeginningAnchor => GetOrCreateNode(ref _beginningAnchorId, SymbolicRegexNodeKind.BeginningAnchor);
        internal SymbolicRegexNode<TSet> EndAnchor => GetOrCreateNode(ref _endAnchorId, SymbolicRegexNodeKind.EndAnchor);
        internal SymbolicRegexNode<TSet> EndAnchorZ => GetOrCreateNode(ref _endAnchorZId, SymbolicRegexNodeKind.EndAnchorZ);
        internal SymbolicRegexNode<TSet> EndAnchorZReverse => GetOrCreateNode(ref _endAnchorZReverseId, SymbolicRegexNodeKind.EndAnchorZReverse);
        internal SymbolicRegexNode<TSet> BolAnchor => GetOrCreateNode(ref _bolAnchorId, SymbolicRegexNodeKind.BOLAnchor);
        internal SymbolicRegexNode<TSet> EolAnchor => GetOrCreateNode(ref _eolAnchorId, SymbolicRegexNodeKind.EOLAnchor);
        internal SymbolicRegexNode<TSet> BoundaryAnchor => GetOrCreateNode(ref _boundaryAnchorId, SymbolicRegexNodeKind.BoundaryAnchor);
        internal SymbolicRegexNode<TSet> NonBoundaryAnchor => GetOrCreateNode(ref _nonBoundaryAnchorId, SymbolicRegexNodeKind.NonBoundaryAnchor);

        internal TSet _wordLetterForBoundariesSet;
        internal TSet _newLineSet;

        private readonly Dictionary<TSet, int> _singletonCache = new();

        private NodeData[] _nodes = new NodeData[InitialNodeCapacity];
        private int _nodeCount;

        internal readonly struct NodeData(
            SymbolicRegexNodeKind kind, int leftId, int rightId,
            int lower, int upper, TSet set, SymbolicRegexInfo info)
        {
            internal readonly SymbolicRegexNodeKind Kind = kind;
            internal readonly int LeftId = leftId;
            internal readonly int RightId = rightId;
            internal readonly int Lower = lower;
            internal readonly int Upper = upper;
            internal readonly TSet Set = set;
            internal readonly SymbolicRegexInfo Info = info;
            internal readonly byte[]? NullabilityCache =
                info.StartsWithSomeAnchor && info.CanBeNullable ? new byte[CharKind.ContextLimit] : null;
        }

        /// <summary>
        /// This cache is used when creating nodes to keep all nodes associated with this builder
        /// unique. This ensures that handle equality can be used for syntactic equality and that all shared subexpressions
        /// are maximally shared.
        /// </summary>
        internal readonly Dictionary<NodeCacheKey, int> _nodeCache = new();

        /// <summary>Key for the <see cref="_nodeCache"/>.</summary>
        /// <remarks>
        /// Used instead of a ValueTuple`7 to avoid rooting that rarely used type that also
        /// includes much more code for its interface implementations.
        /// </remarks>
        internal readonly struct NodeCacheKey(
            SymbolicRegexNodeKind kind, int leftId, int rightId,
            int lower, int upper,
            TSet set, SymbolicRegexInfo info) : IEquatable<NodeCacheKey>
        {
            public readonly SymbolicRegexNodeKind Kind = kind;
            public readonly int LeftId = leftId;
            public readonly int RightId = rightId;
            public readonly int Lower = lower;
            public readonly int Upper = upper;
            public readonly TSet Set = set;
            public readonly SymbolicRegexInfo Info = info;

            public override int GetHashCode() =>
                HashCode.Combine((int)Kind, LeftId, RightId, Lower, Upper, Set, Info);

            public override bool Equals([NotNullWhen(true)] object? obj) =>
                obj is NodeCacheKey other && Equals(other);

            public bool Equals(SymbolicRegexBuilder<TSet>.NodeCacheKey other) =>
                Kind == other.Kind &&
                LeftId == other.LeftId &&
                RightId == other.RightId &&
                Lower == other.Lower &&
                Upper == other.Upper &&
                EqualityComparer<TSet>.Default.Equals(Set, other.Set) &&
                EqualityComparer<SymbolicRegexInfo>.Default.Equals(Info, other.Info);
        }

        // The following dictionaries are used as caches for operations that recurse over the structure of SymbolicRegexNode.
        // These operations are called potentially on every step of the matching process, and they may do linear work in the
        // of the pattern in each call. Thus, caching is necessary to avoid a quadratic worst-case over multiple steps of
        // matching when simplification rules fail to eliminate the portions being walked over.

        /// <summary>
        /// Cache for <see cref="SymbolicRegexNode{TSet}.CreateDerivative(SymbolicRegexBuilder{TSet}, TSet, uint)"/> keyed by:
        ///  -The node to derivate
        ///  -The character or minterm to take the derivative with
        ///  -The surrounding character context
        /// The value is the derivative.
        /// </summary>
        internal readonly Dictionary<(int nodeId, TSet elem, uint context), int> _derivativeCache = new();

        /// <summary>
        /// Cache for <see cref="SymbolicRegexNode{TSet}.PruneLowerPriorityThanNullability(SymbolicRegexBuilder{TSet}, uint)"/> keyed by:
        ///  -The node to prune
        ///  -The surrounding character context
        /// The value is the pruned node.
        /// </summary>
        internal readonly Dictionary<(int nodeId, uint context), int> _pruneLowerPriorityThanNullabilityCache = new();

        /// <summary>
        /// Cache for <see cref="SymbolicRegexNode{TSet}.StripEffects(SymbolicRegexBuilder{TSet})"/> keyed by the node
        /// to strip. The value is the stripped node. This cache is essential for avoiding exponential blowup when
        /// stripping effects from derivatives of deeply nested loop patterns (e.g. ((a)*)*), where the derivative
        /// tree is a DAG with shared sub-trees that would otherwise be traversed repeatedly.
        /// </summary>
        internal readonly Dictionary<int, int> _stripEffectsCache = new();

        /// <summary>
        /// Cache for <see cref="SymbolicRegexNode{TSet}.Subsumes(SymbolicRegexBuilder{TSet}, SymbolicRegexNode{TSet}, int)"/> keyed by:
        ///  -The node R potentially subsuming S
        ///  -The node S potentially being subsumed by R
        /// The value indicates if subsumption is known to hold.
        /// </summary>
        internal readonly Dictionary<(int leftId, int rightId), bool> _subsumptionCache = new();

        internal int NodeCount => Volatile.Read(ref _nodeCount);

        internal NodeData[] AcquireNodeDataArray(int id)
        {
            int nodeCount = Volatile.Read(ref _nodeCount);
            Debug.Assert(id > 0 && id <= nodeCount);
            return _nodes;
        }

        internal SymbolicRegexNode<TSet> GetNode(int id)
        {
            Debug.Assert(id > 0 && id <= Volatile.Read(ref _nodeCount));
            return new(id);
        }

        internal SymbolicRegexNodeContext<TSet> GetNodeContext(SymbolicRegexNode<TSet> node)
        {
            if (node.IsNull)
            {
                return default;
            }

            // Child nodes were published before their parent, so contexts created from this acquired snapshot can
            // traverse the complete immutable subtree without another acquire for each field.
            NodeData[] nodes = AcquireNodeDataArray(node.Id);
            return new SymbolicRegexNodeContext<TSet>(this, nodes, node.Id);
        }

        internal int AddNode(SymbolicRegexNodeKind kind, int leftId, int rightId, int lower, int upper, TSet set, SymbolicRegexInfo info)
        {
            int id = _nodeCount + 1;
            var data = new NodeData(kind, leftId, rightId, lower, upper, set, info);
            NodeData[] nodes = _nodes;

            if (id > nodes.Length)
            {
                var newNodes = new NodeData[checked(nodes.Length * 2)];
                Array.Copy(nodes, newNodes, nodes.Length);
                newNodes[id - 1] = data;
                Volatile.Write(ref _nodes, newNodes);
            }
            else
            {
                nodes[id - 1] = data;
            }

            Volatile.Write(ref _nodeCount, id);
            return id;
        }

        private SymbolicRegexNode<TSet> GetOrCreateNode(ref int id, SymbolicRegexNodeKind kind)
        {
            int existingId = id;
            if (existingId != 0)
            {
                return GetNode(existingId);
            }

            SymbolicRegexNode<TSet> node = kind == SymbolicRegexNodeKind.Epsilon ?
                SymbolicRegexNode<TSet>.CreateEpsilon(this) :
                SymbolicRegexNode<TSet>.CreateAnchor(this, kind);
            id = node.Id;
            return node;
        }

        /// <summary>Create a new symbolic regex builder.</summary>
        internal SymbolicRegexBuilder(ISolver<TSet> solver, CharSetSolver charSetSolver)
        {
            // Solver must be set first, else it will cause null reference exception in the following
            _charSetSolver = charSetSolver;
            _solver = solver;

            // initialized to False but updated later to the actual condition ony if \b or \B occurs anywhere in the regex
            // this implies that if a regex never uses \b or \B then the character context will never
            // update the previous character context to distinguish word and nonword letters
            _wordLetterForBoundariesSet = solver.Empty;

            // initialized to False but updated later to the actual condition of \n only if a line anchor occurs anywhere in the regex
            // this implies that if a regex never uses a line anchor then the character context will never
            // update the previous character context to mark that the previous character was \n
            _newLineSet = solver.Empty;
            _nothingId = SymbolicRegexNode<TSet>.CreateFalse(this).Id;
            _anyCharId = SymbolicRegexNode<TSet>.CreateTrue(this).Id;
            _anyStarId = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: false).Id;
            _anyStarLazyId = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: true).Id;

            // --- initialize singletonCache ---
            _singletonCache[_solver.Empty] = _nothing.Id;
            _singletonCache[_solver.Full] = _anyChar.Id;
        }

        /// <summary>
        /// Make an alternation of given nodes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TSet> Alternate(List<SymbolicRegexNode<TSet>> nodes)
        {
            HashSet<SymbolicRegexNode<TSet>> seenElems = new();

            // Keep track of any elements from the right side that need to be eliminated.
            for (int i = 0; i < nodes.Count; i++)
            {
                if (!seenElems.Add(nodes[i]))
                {
                    // Nothing will be eliminated in the next step
                    nodes[i] = _nothing;
                }
            }

            // Iterate backwards to avoid quadratic rebuilding of the Alternate nodes, which are always simplified to
            // right associative form. Concretely:
            // In (a|(b|c)) | d -> (a|(b|(c|d)) the first argument is not a subtree of the result.
            // In a | (b|(c|d)) -> (a|(b|(c|d)) the second argument is a subtree of the result.
            // The first case performs linear work for each element, leading to a quadratic blowup.
            SymbolicRegexNode<TSet> or = _nothing;
            for (int i = nodes.Count - 1; i >= 0; --i)
            {
                or = SymbolicRegexNode<TSet>.CreateAlternate(this, nodes[i], or, deduplicated: true);
            }

            return or;
        }

        /// <summary>Create a concatenation of given nodes already given in reverse order.</summary>
        /// <remarks>
        /// If any regex is nothing, then return nothing.
        /// Eliminate intermediate epsilons.
        /// </remarks>
        internal SymbolicRegexNode<TSet> CreateConcatAlreadyReversed(IEnumerable<SymbolicRegexNode<TSet>> nodes)
        {
            SymbolicRegexNode<TSet> result = Epsilon;

            // Iterate through all the nodes concatenating them together in reverse order.
            // Here the nodes enumeration is already reversed, so reversing it back to the original concatenation order.
            foreach (SymbolicRegexNode<TSet> node in nodes)
            {
                // If there's a nothing in the list, the whole concatenation can't match, so just return nothing.
                if (node == _nothing)
                {
                    return _nothing;
                }

                result = SymbolicRegexNode<TSet>.CreateConcat(this, node, result);
            }

            return result;
        }

        internal SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right) => SymbolicRegexNode<TSet>.CreateConcat(this, left, right);

        /// <summary>
        /// Make loop regex
        /// </summary>
        internal SymbolicRegexNode<TSet> CreateLoop(SymbolicRegexNode<TSet> node, bool isLazy, int lower = 0, int upper = int.MaxValue)
        {
            // If the lower and upper bound are both 1, then the node would be processed once and only once, so we can just return that node.
            if (lower == 1 && upper == 1)
            {
                return node;
            }

            // If the lower and upper bound are both 0, this is actually empty.
            if (lower == 0 && upper == 0)
            {
                return Epsilon;
            }

            // If this is equivalent to any*, return that.
            SymbolicRegexNodeContext<TSet> nodeContext = GetNodeContext(node);

            if (!isLazy && lower == 0 && upper == int.MaxValue && nodeContext._kind == SymbolicRegexNodeKind.Singleton)
            {
                Debug.Assert(nodeContext._set is not null);
                if (_solver.IsFull(nodeContext._set))
                {
                    return _anyStar;
                }
            }

            // Flip X? into X?? or X?? into X?
            if (nodeContext.Kind == SymbolicRegexNodeKind.Loop && nodeContext._lower == 0 && nodeContext._upper == 1 && lower == 0 && upper == 1)
            {
                Debug.Assert(!nodeContext._left.IsNull);
                if (nodeContext.IsLazy != isLazy)
                {
                    // flip lazyness
                    return SymbolicRegexNode<TSet>.CreateLoop(this, nodeContext._left, 0, 1, isLazy);
                }
                // otherwise there is no change (X??)?? = X?? and (X?)? = X?
                return node;
            }

            // Otherwise, create the loop.
            return SymbolicRegexNode<TSet>.CreateLoop(this, node, lower, upper, isLazy);
        }

        /// <summary>Creates a "singleton", which matches a single character.</summary>
        internal SymbolicRegexNode<TSet> CreateSingleton(TSet set)
        {
            // We maintain a cache of singletons, under the assumption that it's likely the same one/notone/set appears
            // multiple times in the same pattern.  First consult the cache, and then create a new singleton if one didn't exist.
            if (!_singletonCache.TryGetValue(set, out int id))
            {
                SymbolicRegexNode<TSet> node = SymbolicRegexNode<TSet>.CreateSingleton(this, set);
                _singletonCache.Add(set, node.Id);
                return node;
            }

            return GetNode(id);
        }

        /// <summary>Creates a fixed length marker for the end of a sequence.</summary>
        internal SymbolicRegexNode<TSet> CreateFixedLengthMarker(int length) => SymbolicRegexNode<TSet>.CreateFixedLengthMarker(this, length);

        internal SymbolicRegexNode<TSet> CreateEffect(SymbolicRegexNode<TSet> node, SymbolicRegexNode<TSet> effectNode) => SymbolicRegexNode<TSet>.CreateEffect(this, node, effectNode);

        internal SymbolicRegexNode<TSet> CreateCapture(SymbolicRegexNode<TSet> child, int captureNum) => CreateConcat(CreateCaptureStart(captureNum), CreateConcat(child, CreateCaptureEnd(captureNum)));

        internal SymbolicRegexNode<TSet> CreateCaptureStart(int captureNum) => SymbolicRegexNode<TSet>.CreateCaptureStart(this, captureNum);

        internal SymbolicRegexNode<TSet> CreateCaptureEnd(int captureNum) => SymbolicRegexNode<TSet>.CreateCaptureEnd(this, captureNum);

        internal SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexNode<TSet> child)
        {
            return child == _nothing ? _nothing : SymbolicRegexNode<TSet>.CreateDisableBacktrackingSimulation(this, child);
        }

        internal SymbolicRegexNode<TNewSet> Transform<TNewSet>(SymbolicRegexNode<TSet> node, SymbolicRegexBuilder<TNewSet> builder, Func<SymbolicRegexBuilder<TNewSet>, TSet, TNewSet> setTransformer)
            where TNewSet : IComparable<TNewSet>, IEquatable<TNewSet>
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Transform, node, builder, setTransformer);
            }

            SymbolicRegexNodeContext<TSet> nodeContext = GetNodeContext(node);
            switch (nodeContext._kind)
            {
                case SymbolicRegexNodeKind.BeginningAnchor:
                    return builder.BeginningAnchor;

                case SymbolicRegexNodeKind.EndAnchor:
                    return builder.EndAnchor;

                case SymbolicRegexNodeKind.EndAnchorZ:
                    return builder.EndAnchorZ;

                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    return builder.EndAnchorZReverse;

                case SymbolicRegexNodeKind.BOLAnchor:
                    return builder.BolAnchor;

                case SymbolicRegexNodeKind.EOLAnchor:
                    return builder.EolAnchor;

                case SymbolicRegexNodeKind.BoundaryAnchor:
                    return builder.BoundaryAnchor;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                    return builder.NonBoundaryAnchor;

                case SymbolicRegexNodeKind.FixedLengthMarker:
                    return builder.CreateFixedLengthMarker(nodeContext._lower);

                case SymbolicRegexNodeKind.Epsilon:
                    return builder.Epsilon;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(nodeContext._set is not null);
                    return builder.CreateSingleton(setTransformer(builder, nodeContext._set));

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(!nodeContext._left.IsNull);
                    return builder.CreateLoop(Transform(nodeContext._left, builder, setTransformer), nodeContext.IsLazy, nodeContext._lower, nodeContext._upper);

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!nodeContext._left.IsNull && !nodeContext._right.IsNull);
                    return SymbolicRegexNode<TNewSet>.CreateAlternate(builder,
                        Transform(nodeContext._left, builder, setTransformer),
                        Transform(nodeContext._right, builder, setTransformer),
                        deduplicated: true);

                case SymbolicRegexNodeKind.CaptureStart:
                    return builder.CreateCaptureStart(nodeContext._lower);

                case SymbolicRegexNodeKind.CaptureEnd:
                    return builder.CreateCaptureEnd(nodeContext._lower);

                case SymbolicRegexNodeKind.Concat:
                    {
                        List<SymbolicRegexNode<TSet>> concatElements = nodeContext.ToList();
                        SymbolicRegexNode<TNewSet>[] reverseTransformed = new SymbolicRegexNode<TNewSet>[concatElements.Count];
                        for (int i = 0; i < reverseTransformed.Length; i++)
                        {
                            reverseTransformed[i] = Transform(concatElements[^(i + 1)], builder, setTransformer);
                        }
                        return builder.CreateConcatAlreadyReversed(reverseTransformed);
                    }

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(!nodeContext._left.IsNull);
                    return builder.CreateDisableBacktrackingSimulation(Transform(nodeContext._left, builder, setTransformer));

                default:
                    Debug.Fail($"{nameof(Transform)}:{nodeContext._kind}");
                    return default;
            }
        }
    }
}
