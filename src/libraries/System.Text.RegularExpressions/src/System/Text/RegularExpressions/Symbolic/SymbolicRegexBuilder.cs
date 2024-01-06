// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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
        internal readonly CharSetSolver _charSetSolver;
        internal readonly ISolver<TSet> _solver;

        internal readonly SymbolicRegexNode<TSet> _nothing;
        internal readonly SymbolicRegexNode<TSet> _anyChar;
        internal readonly SymbolicRegexNode<TSet> _anyStar;
        internal readonly SymbolicRegexNode<TSet> _anyStarLazy;

        private SymbolicRegexNode<TSet>? _epsilon;
        internal SymbolicRegexNode<TSet> Epsilon => _epsilon ??= SymbolicRegexNode<TSet>.CreateEpsilon(this);

        private SymbolicRegexNode<TSet>? _beginningAnchor;
        internal SymbolicRegexNode<TSet> BeginningAnchor => _beginningAnchor ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.BeginningAnchor);

        private SymbolicRegexNode<TSet>? _endAnchor;
        internal SymbolicRegexNode<TSet> EndAnchor => _endAnchor ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EndAnchor);

        private SymbolicRegexNode<TSet>? _endAnchorZ;
        internal SymbolicRegexNode<TSet> EndAnchorZ => _endAnchorZ ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EndAnchorZ);

        private SymbolicRegexNode<TSet>? _endAnchorZReverse;
        internal SymbolicRegexNode<TSet> EndAnchorZReverse => _endAnchorZReverse ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EndAnchorZReverse);

        private SymbolicRegexNode<TSet>? _bolAnchor;
        internal SymbolicRegexNode<TSet> BolAnchor => _bolAnchor ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.BOLAnchor);

        private SymbolicRegexNode<TSet>? _eolAnchor;
        internal SymbolicRegexNode<TSet> EolAnchor => _eolAnchor ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EOLAnchor);

        private SymbolicRegexNode<TSet>? _wbAnchor;
        internal SymbolicRegexNode<TSet> BoundaryAnchor => _wbAnchor ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.BoundaryAnchor);

        private SymbolicRegexNode<TSet>? _nwbAnchor;
        internal SymbolicRegexNode<TSet> NonBoundaryAnchor => _nwbAnchor ??= SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.NonBoundaryAnchor);

        internal TSet _wordLetterForBoundariesSet;
        internal TSet _newLineSet;

        private readonly Dictionary<TSet, SymbolicRegexNode<TSet>> _singletonCache = new();

        /// <summary>
        /// This cache is used in <see cref="SymbolicRegexNode{TSet}.Create"/> to keep all nodes associated with this builder
        /// unique. This ensures that reference equality can be used for syntactic equality and that all shared subexpressions
        /// are maximally shared.
        /// </summary>
        internal readonly Dictionary<NodeCacheKey, SymbolicRegexNode<TSet>> _nodeCache = new();

        /// <summary>Key for the <see cref="_nodeCache"/>.</summary>
        /// <remarks>
        /// Used instead of a ValueTuple`7 to avoid rooting that rarely used type that also
        /// includes much more code an interface implementation.
        /// </remarks>
        internal readonly struct NodeCacheKey(
            SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet>? left, SymbolicRegexNode<TSet>? right,
            int lower, int upper,
            TSet set, SymbolicRegexInfo info) : IEquatable<NodeCacheKey>
        {
            public readonly SymbolicRegexNodeKind Kind = kind;
            public readonly SymbolicRegexNode<TSet>? Left = left;
            public readonly SymbolicRegexNode<TSet>? Right = right;
            public readonly int Lower = lower;
            public readonly int Upper = upper;
            public readonly TSet Set = set;
            public readonly SymbolicRegexInfo Info = info;

            public override int GetHashCode() =>
                HashCode.Combine((int)Kind, Left, Right, Lower, Upper, Set, Info);

            public override bool Equals([NotNullWhen(true)] object? obj) =>
                obj is NodeCacheKey other && Equals(other);

            public bool Equals(SymbolicRegexBuilder<TSet>.NodeCacheKey other) =>
                Kind == other.Kind &&
                Left == other.Left &&
                Right == other.Right &&
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
        internal readonly Dictionary<(SymbolicRegexNode<TSet>, TSet elem, uint context), SymbolicRegexNode<TSet>> _derivativeCache = new();

        /// <summary>
        /// Cache for <see cref="SymbolicRegexNode{TSet}.PruneLowerPriorityThanNullability(SymbolicRegexBuilder{TSet}, uint)"/> keyed by:
        ///  -The node to prune
        ///  -The surrounding character context
        /// The value is the pruned node.
        /// </summary>
        internal readonly Dictionary<(SymbolicRegexNode<TSet>, uint), SymbolicRegexNode<TSet>> _pruneLowerPriorityThanNullabilityCache = new();

        /// <summary>
        /// Cache for <see cref="SymbolicRegexNode{TSet}.Subsumes(SymbolicRegexBuilder{TSet}, SymbolicRegexNode{TSet}, int)"/> keyed by:
        ///  -The node R potentially subsuming S
        ///  -The node S potentially being subsumed by R
        /// The value indicates if subsumption is known to hold.
        /// </summary>
        internal readonly Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>), bool> _subsumptionCache = new();

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
            // update the previous character context to mark that the previous caharcter was \n
            _newLineSet = solver.Empty;
            _nothing = SymbolicRegexNode<TSet>.CreateFalse(this);
            _anyChar = SymbolicRegexNode<TSet>.CreateTrue(this);
            _anyStar = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: false);
            _anyStarLazy = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: true);

            // --- initialize singletonCache ---
            _singletonCache[_solver.Empty] = _nothing;
            _singletonCache[_solver.Full] = _anyChar;
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
            if (!isLazy && lower == 0 && upper == int.MaxValue && node._kind == SymbolicRegexNodeKind.Singleton)
            {
                Debug.Assert(node._set is not null);
                if (_solver.IsFull(node._set))
                {
                    return _anyStar;
                }
            }

            // Flip X? into X?? or X?? into X?
            if (node.Kind == SymbolicRegexNodeKind.Loop && node._lower == 0 && node._upper == 1 && lower == 0 && upper == 1)
            {
                Debug.Assert(node._left is not null);
                if (node.IsLazy != isLazy)
                {
                    // flip lazyness
                    return SymbolicRegexNode<TSet>.CreateLoop(this, node._left, 0, 1, isLazy);
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
            ref SymbolicRegexNode<TSet>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(_singletonCache, set, out _);
            return result ??= SymbolicRegexNode<TSet>.CreateSingleton(this, set);
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

            switch (node._kind)
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
                    return builder.CreateFixedLengthMarker(node._lower);

                case SymbolicRegexNodeKind.Epsilon:
                    return builder.Epsilon;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(node._set is not null);
                    return builder.CreateSingleton(setTransformer(builder, node._set));

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(node._left is not null);
                    return builder.CreateLoop(Transform(node._left, builder, setTransformer), node.IsLazy, node._lower, node._upper);

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(node._left is not null && node._right is not null);
                    return SymbolicRegexNode<TNewSet>.CreateAlternate(builder,
                        Transform(node._left, builder, setTransformer),
                        Transform(node._right, builder, setTransformer),
                        deduplicated: true);

                case SymbolicRegexNodeKind.CaptureStart:
                    return builder.CreateCaptureStart(node._lower);

                case SymbolicRegexNodeKind.CaptureEnd:
                    return builder.CreateCaptureEnd(node._lower);

                case SymbolicRegexNodeKind.Concat:
                    {
                        List<SymbolicRegexNode<TSet>> concatElements = node.ToList();
                        SymbolicRegexNode<TNewSet>[] reverseTransformed = new SymbolicRegexNode<TNewSet>[concatElements.Count];
                        for (int i = 0; i < reverseTransformed.Length; i++)
                        {
                            reverseTransformed[i] = Transform(concatElements[^(i + 1)], builder, setTransformer);
                        }
                        return builder.CreateConcatAlreadyReversed(reverseTransformed);
                    }

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(node._left is not null);
                    return builder.CreateDisableBacktrackingSimulation(Transform(node._left, builder, setTransformer));

                default:
                    Debug.Fail($"{nameof(Transform)}:{node._kind}");
                    return null;
            }
        }
    }
}
