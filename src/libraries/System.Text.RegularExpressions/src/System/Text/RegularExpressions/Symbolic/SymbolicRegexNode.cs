// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents an AST node of a symbolic regex.</summary>
    internal sealed class SymbolicRegexNode<S> where S : notnull
    {
        internal const string EmptyCharClass = "[]";
        /// <summary>Some byte other than 0 to represent true</summary>
        internal const byte TrueByte = 1;
        /// <summary>Some byte other than 0 to represent false</summary>
        internal const byte FalseByte = 2;
        /// <summary>The undefined value is the default value 0</summary>
        internal const byte UndefinedByte = 0;

        internal readonly SymbolicRegexBuilder<S> _builder;
        internal readonly SymbolicRegexNodeKind _kind;
        internal readonly int _lower;
        internal readonly int _upper;
        internal readonly S? _set;
        internal readonly SymbolicRegexNode<S>? _left;
        internal readonly SymbolicRegexNode<S>? _right;
        internal readonly SymbolicRegexSet<S>? _alts;

        /// <summary>
        /// Caches nullability of this node for any given context (0 &lt;= context &lt; ContextLimit)
        /// when _info.StartsWithSomeAnchor and _info.CanBeNullable are true. Otherwise the cache is null.
        /// </summary>
        private byte[]? _nullabilityCache;

        private S _startSet;

        /// <summary>AST node of a symbolic regex</summary>
        /// <param name="builder">the builder</param>
        /// <param name="kind">what kind of node</param>
        /// <param name="left">left child</param>
        /// <param name="right">right child</param>
        /// <param name="lower">lower bound of a loop</param>
        /// <param name="upper">upper boubd of a loop</param>
        /// <param name="set">singelton set</param>
        /// <param name="alts">alternatives set of a disjunction or conjunction</param>
        /// <param name="info">misc flags including laziness</param>
        private SymbolicRegexNode(SymbolicRegexBuilder<S> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<S>? left, SymbolicRegexNode<S>? right, int lower, int upper, S? set, SymbolicRegexSet<S>? alts, SymbolicRegexInfo info)
        {
            _builder = builder;
            _kind = kind;
            _left = left;
            _right = right;
            _lower = lower;
            _upper = upper;
            _set = set;
            _alts = alts;
            _info = info;
            _hashcode = ComputeHashCode();
            _startSet = ComputeStartSet();
            _nullabilityCache = info.StartsWithSomeAnchor && info.CanBeNullable ? new byte[CharKind.ContextLimit] : null;
        }

        private bool _isInternalizedUnion;

        /// <summary> Create a new node or retrieve one from the builder _nodeCache</summary>
        private static SymbolicRegexNode<S> Create(SymbolicRegexBuilder<S> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<S>? left, SymbolicRegexNode<S>? right, int lower, int upper, S? set, SymbolicRegexSet<S>? alts, SymbolicRegexInfo info)
        {
            SymbolicRegexNode<S>? node;
            var key = (kind, left, right, lower, upper, set, alts, info);
            if (!builder._nodeCache.TryGetValue(key, out node))
            {
                // Do not internalize top level Or-nodes or else NFA mode will become ineffective
                if (kind == SymbolicRegexNodeKind.Or)
                {
                    node = new(builder, kind, left, right, lower, upper, set, alts, info);
                    return node;
                }

                left = left == null || left._kind != SymbolicRegexNodeKind.Or || left._isInternalizedUnion ? left : Internalize(left);
                right = right == null || right._kind != SymbolicRegexNodeKind.Or || right._isInternalizedUnion ? right : Internalize(right);

                node = new(builder, kind, left, right, lower, upper, set, alts, info);
                builder._nodeCache[key] = node;
            }

            Debug.Assert(node is not null);
            return node;
        }

        /// <summary> Internalize an Or-node that is not yet internalized</summary>
        private static SymbolicRegexNode<S> Internalize(SymbolicRegexNode<S> node)
        {
            Debug.Assert(node._kind == SymbolicRegexNodeKind.Or && !node._isInternalizedUnion);

            (SymbolicRegexNodeKind, SymbolicRegexNode<S>?, SymbolicRegexNode<S>?, int, int, S?, SymbolicRegexSet<S>?, SymbolicRegexInfo) node_key =
                (SymbolicRegexNodeKind.Or, null, null, -1, -1, default(S), node._alts, node._info);
            SymbolicRegexNode<S>? node1;
            if (node._builder._nodeCache.TryGetValue(node_key, out node1))
            {
                Debug.Assert(node1 is not null && node1._isInternalizedUnion);
                return node1;
            }
            else
            {
                node._isInternalizedUnion = true;
                node._builder._nodeCache[node_key] = node;
                return node;
            }
        }

        /// <summary>True if this node only involves lazy loops</summary>
        internal bool IsLazy => _info.IsLazy;

        /// <summary>True if this node accepts the empty string unconditionally.</summary>
        internal bool IsNullable => _info.IsNullable;

        /// <summary>True if this node can potentially accept the empty string depending on anchors and immediate context.</summary>
        internal bool CanBeNullable
        {
            get
            {
                Debug.Assert(_info.CanBeNullable || !_info.IsNullable);
                return _info.CanBeNullable;
            }
        }

        internal SymbolicRegexInfo _info;

        private readonly int _hashcode;


        /// <summary>Converts a Concat or OrderdOr into an array, returns anything else in a singleton array.</summary>
        /// <param name="list">a list to insert the elements into, or null to return results in a new list</param>
        /// <param name="listKind">kind of node to consider as the list builder</param>
        public List<SymbolicRegexNode<S>> ToList(List<SymbolicRegexNode<S>>? list = null, SymbolicRegexNodeKind listKind = SymbolicRegexNodeKind.Concat)
        {
            Debug.Assert(listKind == SymbolicRegexNodeKind.Concat || listKind == SymbolicRegexNodeKind.OrderedOr);
            list ??= new List<SymbolicRegexNode<S>>();
            AppendToList(this, list, listKind);
            return list;

            static void AppendToList(SymbolicRegexNode<S> concat, List<SymbolicRegexNode<S>> list, SymbolicRegexNodeKind listKind)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(AppendToList, concat, list, listKind);
                    return;
                }

                SymbolicRegexNode<S> node = concat;
                while (node._kind == listKind)
                {
                    Debug.Assert(node._left is not null && node._right is not null);
                    if (node._left._kind == listKind)
                    {
                        AppendToList(node._left, list, listKind);
                    }
                    else
                    {
                        list.Add(node._left);
                    }
                    node = node._right;
                }

                list.Add(node);
            }
        }

        /// <summary>
        /// Relative nullability that takes into account the immediate character context
        /// in order to resolve nullability of anchors
        /// </summary>
        /// <param name="context">kind info for previous and next characters</param>
        internal bool IsNullableFor(uint context)
        {
            // if _nullabilityCache is null then IsNullable==CanBeNullable
            // Observe that if IsNullable==true then CanBeNullable==true.
            // but when the node does not start with an anchor
            // and IsNullable==false then CanBeNullable==false.

            return _nullabilityCache is null ?
                _info.IsNullable :
                WithCache(context);

            // Separated out to enable the common case (no nullability cache) to be inlined
            // and to avoid zero-init costs for generally unused state.
            bool WithCache(uint context)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    return StackHelper.CallOnEmptyStack(IsNullableFor, context);
                }

                Debug.Assert(context < CharKind.ContextLimit);

                // If nullablity has been computed for the given context then return it
                byte b = Volatile.Read(ref _nullabilityCache[context]);
                if (b != UndefinedByte)
                {
                    return b == TrueByte;
                }

                // Otherwise compute the nullability recursively for the given context
                bool is_nullable;
                switch (_kind)
                {
                    case SymbolicRegexNodeKind.Loop:
                        Debug.Assert(_left is not null);
                        is_nullable = _lower == 0 || _left.IsNullableFor(context);
                        break;

                    case SymbolicRegexNodeKind.Concat:
                        Debug.Assert(_left is not null && _right is not null);
                        is_nullable = _left.IsNullableFor(context) && _right.IsNullableFor(context);
                        break;

                    case SymbolicRegexNodeKind.Or:
                    case SymbolicRegexNodeKind.And:
                        Debug.Assert(_alts is not null);
                        is_nullable = _alts.IsNullableFor(context);
                        break;

                    case SymbolicRegexNodeKind.OrderedOr:
                        Debug.Assert(_left is not null && _right is not null);
                        is_nullable = _left.IsNullableFor(context) || _right.IsNullableFor(context);
                        break;

                    case SymbolicRegexNodeKind.Not:
                        Debug.Assert(_left is not null);
                        is_nullable = !_left.IsNullableFor(context);
                        break;

                    case SymbolicRegexNodeKind.BeginningAnchor:
                        is_nullable = CharKind.Prev(context) == CharKind.BeginningEnd;
                        break;

                    case SymbolicRegexNodeKind.EndAnchor:
                        is_nullable = CharKind.Next(context) == CharKind.BeginningEnd;
                        break;

                    case SymbolicRegexNodeKind.BOLAnchor:
                        // Beg-Of-Line anchor is nullable when the previous character is Newline or Start
                        // note: at least one of the bits must be 1, but both could also be 1 in case of very first newline
                        is_nullable = (CharKind.Prev(context) & CharKind.NewLineS) != 0;
                        break;

                    case SymbolicRegexNodeKind.EOLAnchor:
                        // End-Of-Line anchor is nullable when the next character is Newline or Stop
                        // note: at least one of the bits must be 1, but both could also be 1 in case of \Z
                        is_nullable = (CharKind.Next(context) & CharKind.NewLineS) != 0;
                        break;

                    case SymbolicRegexNodeKind.BoundaryAnchor:
                        // test that prev char is word letter iff next is not not word letter
                        is_nullable = ((CharKind.Prev(context) & CharKind.WordLetter) ^ (CharKind.Next(context) & CharKind.WordLetter)) != 0;
                        break;

                    case SymbolicRegexNodeKind.NonBoundaryAnchor:
                        // test that prev char is word letter iff next is word letter
                        is_nullable = ((CharKind.Prev(context) & CharKind.WordLetter) ^ (CharKind.Next(context) & CharKind.WordLetter)) == 0;
                        break;

                    case SymbolicRegexNodeKind.EndAnchorZ:
                        // \Z anchor is nullable when the next character is either the last Newline or Stop
                        // note: CharKind.NewLineS == CharKind.Newline|CharKind.StartStop
                        is_nullable = (CharKind.Next(context) & CharKind.BeginningEnd) != 0;
                        break;

                    case SymbolicRegexNodeKind.CaptureStart:
                    case SymbolicRegexNodeKind.CaptureEnd:
                        is_nullable = true;
                        break;

                    default:
                        // SymbolicRegexKind.EndAnchorZReverse:
                        // EndAnchorZRev (rev(\Z)) anchor is nullable when the prev character is either the first Newline or Start
                        // note: CharKind.NewLineS == CharKind.Newline|CharKind.StartStop
                        Debug.Assert(_kind == SymbolicRegexNodeKind.EndAnchorZReverse);
                        is_nullable = (CharKind.Prev(context) & CharKind.BeginningEnd) != 0;
                        break;
                }

                Volatile.Write(ref _nullabilityCache[context], is_nullable ? TrueByte : FalseByte);

                return is_nullable;
            }
        }

        /// <summary>Returns true if this is equivalent to .* (the node must be eager also)</summary>
        public bool IsAnyStar
        {
            get
            {
                if (IsStar)
                {
                    Debug.Assert(_left is not null);
                    if (_left._kind == SymbolicRegexNodeKind.Singleton)
                    {
                        Debug.Assert(_left._set is not null);
                        return !IsLazy && _builder._solver.True.Equals(_left._set);
                    }
                }

                return false;
            }
        }

        /// <summary>Returns true if this is equivalent to .+ (the node must be eager also)</summary>
        public bool IsAnyPlus
        {
            get
            {
                if (IsPlus)
                {
                    Debug.Assert(_left is not null);
                    if (_left._kind == SymbolicRegexNodeKind.Singleton)
                    {
                        Debug.Assert(_left._set is not null);
                        return !IsLazy && _builder._solver.True.Equals(_left._set);
                    }
                }

                return false;
            }
        }

        /// <summary>Returns true if this is equivalent to [\0-\xFFFF] </summary>
        public bool IsAnyChar
        {
            get
            {
                if (_kind == SymbolicRegexNodeKind.Singleton)
                {
                    Debug.Assert(_set is not null);
                    return _builder._solver.AreEquivalent(_builder._solver.True, _set);
                }

                return false;
            }
        }

        /// <summary>Returns true if this is equivalent to [0-[0]]</summary>
        public bool IsNothing
        {
            get
            {
                if (_kind == SymbolicRegexNodeKind.Singleton)
                {
                    Debug.Assert(_set is not null);
                    return !_builder._solver.IsSatisfiable(_set);
                }

                return false;
            }
        }

        /// <summary>Returns true iff this is a loop whose lower bound is 0 and upper bound is max</summary>
        public bool IsStar => _lower == 0 && _upper == int.MaxValue;

        /// <summary>Returns true if this is Epsilon</summary>
        public bool IsEpsilon => _kind == SymbolicRegexNodeKind.Epsilon;

        /// <summary>Gets the kind of the regex</summary>
        internal SymbolicRegexNodeKind Kind => _kind;

        /// <summary>
        /// Returns true iff this is a loop whose lower bound is 1 and upper bound is max
        /// </summary>
        public bool IsPlus => _lower == 1 && _upper == int.MaxValue;

        #region called only once, in the constructor of SymbolicRegexBuilder

        internal static SymbolicRegexNode<S> CreateFalse(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, builder._solver.False, null, SymbolicRegexInfo.Create());

        internal static SymbolicRegexNode<S> CreateTrue(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, builder._solver.True, null, SymbolicRegexInfo.Create(containsSomeCharacter: true));

        internal static SymbolicRegexNode<S> CreateFixedLengthMarker(SymbolicRegexBuilder<S> builder, int length) =>
            Create(builder, SymbolicRegexNodeKind.FixedLengthMarker, null, null, length, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        internal static SymbolicRegexNode<S> CreateEpsilon(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexNodeKind.Epsilon, null, null, -1, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        internal static SymbolicRegexNode<S> CreateEagerEmptyLoop(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body) =>
            Create(builder, SymbolicRegexNodeKind.Loop, body, null, 0, 0, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true, isLazy: false));

        internal static SymbolicRegexNode<S> CreateBeginEndAnchor(SymbolicRegexBuilder<S> builder, SymbolicRegexNodeKind kind)
        {
            Debug.Assert(kind is
                SymbolicRegexNodeKind.BeginningAnchor or SymbolicRegexNodeKind.EndAnchor or
                SymbolicRegexNodeKind.EndAnchorZ or SymbolicRegexNodeKind.EndAnchorZReverse or
                SymbolicRegexNodeKind.EOLAnchor or SymbolicRegexNodeKind.BOLAnchor);
            return Create(builder, kind, null, null, -1, -1, default, null, SymbolicRegexInfo.Create(startsWithLineAnchor: true, canBeNullable: true));
        }

        internal static SymbolicRegexNode<S> CreateBoundaryAnchor(SymbolicRegexBuilder<S> builder, SymbolicRegexNodeKind kind)
        {
            Debug.Assert(kind is SymbolicRegexNodeKind.BoundaryAnchor or SymbolicRegexNodeKind.NonBoundaryAnchor);
            return Create(builder, kind, null, null, -1, -1, default, null, SymbolicRegexInfo.Create(startsWithBoundaryAnchor: true, canBeNullable: true));
        }

        #endregion

        internal static SymbolicRegexNode<S> CreateSingleton(SymbolicRegexBuilder<S> builder, S set) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, set, null, SymbolicRegexInfo.Create(containsSomeCharacter: !set.Equals(builder._solver.False)));

        internal static SymbolicRegexNode<S> CreateLoop(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body, int lower, int upper, bool isLazy)
        {
            Debug.Assert(lower >= 0 && lower <= upper);
            return Create(builder, SymbolicRegexNodeKind.Loop, body, null, lower, upper, default, null, SymbolicRegexInfo.Loop(body._info, lower, isLazy));
        }

        internal static SymbolicRegexNode<S> Or(SymbolicRegexBuilder<S> builder, params SymbolicRegexNode<S>[] disjuncts) =>
            CreateCollection(builder, SymbolicRegexNodeKind.Or, SymbolicRegexSet<S>.CreateMulti(builder, disjuncts, SymbolicRegexNodeKind.Or), SymbolicRegexInfo.Or(GetInfos(disjuncts)));

        internal static SymbolicRegexNode<S> Or(SymbolicRegexBuilder<S> builder, SymbolicRegexSet<S> disjuncts)
        {
            Debug.Assert(disjuncts._kind == SymbolicRegexNodeKind.Or || disjuncts.IsEverything);
            return CreateCollection(builder, SymbolicRegexNodeKind.Or, disjuncts, SymbolicRegexInfo.Or(GetInfos(disjuncts)));
        }

        internal static SymbolicRegexNode<S> And(SymbolicRegexBuilder<S> builder, params SymbolicRegexNode<S>[] conjuncts) =>
            CreateCollection(builder, SymbolicRegexNodeKind.And, SymbolicRegexSet<S>.CreateMulti(builder, conjuncts, SymbolicRegexNodeKind.And), SymbolicRegexInfo.And(GetInfos(conjuncts)));

        internal static SymbolicRegexNode<S> And(SymbolicRegexBuilder<S> builder, SymbolicRegexSet<S> conjuncts)
        {
            Debug.Assert(conjuncts.IsNothing || conjuncts._kind == SymbolicRegexNodeKind.And);
            return CreateCollection(builder, SymbolicRegexNodeKind.And, conjuncts, SymbolicRegexInfo.And(GetInfos(conjuncts)));
        }

        internal static SymbolicRegexNode<S> CreateCaptureStart(SymbolicRegexBuilder<S> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureStart, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        internal static SymbolicRegexNode<S> CreateCaptureEnd(SymbolicRegexBuilder<S> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureEnd, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        private static SymbolicRegexNode<S> CreateCollection(SymbolicRegexBuilder<S> builder, SymbolicRegexNodeKind kind, SymbolicRegexSet<S> alts, SymbolicRegexInfo info) =>
            alts.IsNothing ? builder._nothing :
            alts.IsEverything ? builder._anyStar :
            alts.IsSingleton ? alts.GetSingletonElement() :
            Create(builder, kind, null, null, -1, -1, default, alts, info);

        private static SymbolicRegexInfo[] GetInfos(SymbolicRegexNode<S>[] nodes)
        {
            var infos = new SymbolicRegexInfo[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                infos[i] = nodes[i]._info;
            }
            return infos;
        }

        private static SymbolicRegexInfo[] GetInfos(SymbolicRegexSet<S> nodes)
        {
            var infos = new SymbolicRegexInfo[nodes.Count];
            int i = 0;
            foreach (SymbolicRegexNode<S> node in nodes)
            {
                Debug.Assert(i < nodes.Count);
                infos[i++] = node._info;
            }
            Debug.Assert(i == nodes.Count);
            return infos;
        }

        /// <summary>Make a concatenation of the supplied regex nodes.</summary>
        internal static SymbolicRegexNode<S> CreateConcat(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            // Concatenating anything with a nothing means the entire concatenation can't match
            if (left == builder._nothing || right == builder._nothing)
                return builder._nothing;

            // If the left or right is empty, just return the other.
            if (left.IsEpsilon)
                return right;
            if (right.IsEpsilon)
                return left;

            // If the left isn't a concatenation, then proceed to concatenation the left with the right.
            if (left._kind != SymbolicRegexNodeKind.Concat)
            {
                return Create(builder, SymbolicRegexNodeKind.Concat, left, right, -1, -1, default, null, SymbolicRegexInfo.Concat(left._info, right._info));
            }

            // The left is a concatenation.  We want to flatten it out and maintain a right-associative form.
            SymbolicRegexNode<S> concat = right;
            List<SymbolicRegexNode<S>> leftNodes = left.ToList();
            for (int i = leftNodes.Count - 1; i >= 0; i--)
            {
                concat = Create(builder, SymbolicRegexNodeKind.Concat, leftNodes[i], concat, -1, -1, default, null, SymbolicRegexInfo.Concat(leftNodes[i]._info, concat._info));
            }
            return concat;
        }

        /// <summary>
        /// Make an ordered or of given regexes, eliminate nothing regexes and treat .* as consuming element.
        /// Keep the or flat, assuming both right and left are flat.
        /// Apply a counber subsumption/combining optimization, such that e.g. a{2,5}|a{3,10} will be combined to a{2,10}.
        /// </summary>
        internal static SymbolicRegexNode<S> OrderedOr(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            if (left.IsAnyStar || right == builder._nothing || left == right)
                return left;
            if (left == builder._nothing)
                return right;

            if (left._kind != SymbolicRegexNodeKind.OrderedOr)
            {
                // Apply the counter subsumption/combining optimization if possible
                (SymbolicRegexNode<S> loop, SymbolicRegexNode<S> rest) = left.FirstCounterInfo();
                if (loop != builder._nothing)
                {
                    Debug.Assert(loop._kind == SymbolicRegexNodeKind.Loop && loop._left is not null);
                    (SymbolicRegexNode<S> otherLoop, SymbolicRegexNode<S> otherRest) = right.FirstCounterInfo();
                    if (otherLoop != builder._nothing && rest == otherRest)
                    {
                        // Found two adjacent counters with the same continuation, check that the loops are equivalent apart from bounds
                        // and that the bounds form a contiguous interval. Two integer intervals [x1,x2] and [y1,y2] overlap when
                        // x1 <= y2 and y1 <= x2. The union of intervals that just touch is still contiguous, e.g. [2,5] and [6,10] make
                        // [2,10], so the lower bounds are decremented by 1 in the check.
                        Debug.Assert(otherLoop._kind == SymbolicRegexNodeKind.Loop && otherLoop._left is not null);
                        if (loop._left == otherLoop._left && loop.IsLazy == otherLoop.IsLazy &&
                            loop._lower - 1 <= otherLoop._upper && otherLoop._lower - 1 <= loop._upper)
                        {
                            // Loops are equivalent apart from bounds, and the union of the bounds is a contiguous interval
                            // Build a new counter for the union of the ranges
                            SymbolicRegexNode<S> newCounter = CreateConcat(builder, CreateLoop(builder, loop._left,
                                Math.Min(loop._lower, otherLoop._lower), Math.Max(loop._upper, otherLoop._upper), loop.IsLazy), rest);
                            if (right._kind == SymbolicRegexNodeKind.OrderedOr)
                            {
                                // The right counter came from an or, so include the rest of that or
                                Debug.Assert(right._right is not null);
                                return OrderedOr(builder, newCounter, right._right);
                            }
                            else
                            {
                                return newCounter;
                            }
                        }
                    }
                }
                // No need for flattening and counter optimization did not apply, just build the or
                return Create(builder, SymbolicRegexNodeKind.OrderedOr, left, right, -1, -1, default, null, SymbolicRegexInfo.Or(left._info, right._info));
            }

            // If the left side was an or, then it has to be flattened, gather the elements from both sides
            List<SymbolicRegexNode<S>> elems = left.ToList(listKind: SymbolicRegexNodeKind.OrderedOr);
            int firstRightElem = elems.Count;
            right.ToList(elems, listKind: SymbolicRegexNodeKind.OrderedOr);

            // Eliminate any duplicate elements, keeping the leftmost element
            HashSet<SymbolicRegexNode<S>> seenElems = new();
            // Keep track of if any elements from the right side need to be eliminated
            bool rightChanged = false;
            for (int i = 0; i < elems.Count; i++)
            {
                if (!seenElems.Contains(elems[i]))
                {
                    seenElems.Add(elems[i]);
                }
                else
                {
                    // Nothing will be eliminated in the next step
                    elems[i] = builder._nothing;
                    rightChanged |= i >= firstRightElem;
                }
            }

            // Build the flattened or, avoiding rebuilding the right side if possible
            if (rightChanged)
            {
                SymbolicRegexNode<S> or = builder._nothing;
                for (int i = elems.Count - 1; i >= 0; i--)
                {
                    or = OrderedOr(builder, elems[i], or);
                }
                return or;
            }
            else
            {
                SymbolicRegexNode<S> or = right;
                for (int i = firstRightElem - 1; i >= 0; i--)
                {
                    or = OrderedOr(builder, elems[i], or);
                }
                return or;
            }
        }

        /// <summary>
        /// Extract a counter as a loop followed by its continuation. For example, a*b returns (a*,b).
        /// Also look into the first element of an or, so a+|xyz returns (a+,()).
        /// If no counter is found returns ([],[]).
        /// </summary>
        /// <returns>a tuple of the loop and its continuation</returns>
        private (SymbolicRegexNode<S>, SymbolicRegexNode<S>) FirstCounterInfo()
        {
            if (_kind == SymbolicRegexNodeKind.Loop)
                return (this, _builder.Epsilon);
            if (_kind == SymbolicRegexNodeKind.Concat)
            {
                Debug.Assert(_left is not null && _right is not null);
                if (_left.Kind == SymbolicRegexNodeKind.Loop)
                    return (_left, _right);
            }
            if (_kind == SymbolicRegexNodeKind.OrderedOr)
            {
                Debug.Assert(_left is not null);
                return _left.FirstCounterInfo();
            }
            return (_builder._nothing, _builder._nothing);
        }

        internal static SymbolicRegexNode<S> Not(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> root)
        {
            // Instead of just creating a negated root node
            // Convert ~root to Negation Normal Form (NNF) by using deMorgan's laws and push ~ to the leaves
            // This may avoid rather large overhead (such case was discovered with unit test PasswordSearchDual)
            // Do this transformation in-line without recursion, to avoid any chance of deep recursion
            // OBSERVE: NNF[node] represents the Negation Normal Form of ~node
            Dictionary<SymbolicRegexNode<S>, SymbolicRegexNode<S>> NNF = new();
            Stack<(SymbolicRegexNode<S>, bool)> todo = new();
            todo.Push((root, false));
            while (todo.Count > 0)
            {
                (SymbolicRegexNode<S>, bool) top = todo.Pop();
                bool secondTimePushed = top.Item2;
                SymbolicRegexNode<S> node = top.Item1;
                if (secondTimePushed)
                {
                    Debug.Assert((node._kind == SymbolicRegexNodeKind.Or || node._kind == SymbolicRegexNodeKind.And) && node._alts is not null);
                    // Here all members of _alts have been processed
                    List<SymbolicRegexNode<S>> alts_nnf = new();
                    foreach (SymbolicRegexNode<S> elem in node._alts)
                    {
                        alts_nnf.Add(NNF[elem]);
                    }
                    // Using deMorgan's laws, flip the kind: Or becomes And, And becomes Or
                    SymbolicRegexNode<S> node_nnf = node._kind == SymbolicRegexNodeKind.Or ? And(builder, alts_nnf.ToArray()) : Or(builder, alts_nnf.ToArray());
                    NNF[node] = node_nnf;
                }
                else
                {
                    switch (node._kind)
                    {
                        case SymbolicRegexNodeKind.Not:
                            Debug.Assert(node._left is not null);
                            // Here we assume that top._left is already in NNF, double negation is cancelled out
                            NNF[node] = node._left;
                            break;

                        case SymbolicRegexNodeKind.Or or SymbolicRegexNodeKind.And:
                            Debug.Assert(node._alts is not null);
                            // Push the node for the second time
                            todo.Push((node, true));
                            // Compute the negation normal form of all the members
                            // Their computation is actually the same independent from being inside an 'Or' or 'And' node
                            foreach (SymbolicRegexNode<S> elem in node._alts)
                            {
                                todo.Push((elem, false));
                            }
                            break;

                        case SymbolicRegexNodeKind.Epsilon:
                            //  ~() = .+
                            NNF[node] = SymbolicRegexNode<S>.CreateLoop(builder, builder._anyChar, 1, int.MaxValue, isLazy: false);
                            break;

                        case SymbolicRegexNodeKind.Singleton:
                            Debug.Assert(node._set is not null);
                            // ~[] = .*
                            if (node.IsNothing)
                            {
                                NNF[node] = builder._anyStar;
                                break;
                            }
                            goto default;

                        case SymbolicRegexNodeKind.Loop:
                            Debug.Assert(node._left is not null);
                            // ~(.*) = [] and ~(.+) = ()
                            if (node.IsAnyStar)
                            {
                                NNF[node] = builder._nothing;
                                break;
                            }
                            else if (node.IsPlus && node._left.IsAnyChar)
                            {
                                NNF[node] = builder.Epsilon;
                                break;
                            }
                            goto default;

                        default:
                            // In all other cases construct the complement
                            NNF[node] = Create(builder, SymbolicRegexNodeKind.Not, node, null, -1, -1, default, null, SymbolicRegexInfo.Not(node._info));
                            break;
                    }
                }
            }
            return NNF[root];
        }

        /// <summary>
        /// Returns the fixed matching length of the regex or -1 if the regex does not have a fixed matching length.
        /// </summary>
        public int GetFixedLength()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, assume no fixed length.
                return -1;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.FixedLengthMarker:
                case SymbolicRegexNodeKind.Epsilon:
                case SymbolicRegexNodeKind.BOLAnchor:
                case SymbolicRegexNodeKind.EOLAnchor:
                case SymbolicRegexNodeKind.EndAnchor:
                case SymbolicRegexNodeKind.BeginningAnchor:
                case SymbolicRegexNodeKind.EndAnchorZ:
                case SymbolicRegexNodeKind.EndAnchorZReverse:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                case SymbolicRegexNodeKind.CaptureStart:
                case SymbolicRegexNodeKind.CaptureEnd:
                    return 0;

                case SymbolicRegexNodeKind.Singleton:
                    return 1;

                case SymbolicRegexNodeKind.Loop:
                    {
                        Debug.Assert(_left is not null);
                        if (_lower == _upper)
                        {
                            long length = _left.GetFixedLength();
                            if (length >= 0)
                            {
                                length *= _lower;
                                if (length <= int.MaxValue)
                                {
                                    return (int)length;
                                }
                            }
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        int leftLength = _left.GetFixedLength();
                        if (leftLength >= 0)
                        {
                            int rightLength = _right.GetFixedLength();
                            if (rightLength >= 0)
                            {
                                long length = (long)leftLength + rightLength;
                                if (length <= int.MaxValue)
                                {
                                    return (int)length;
                                }
                            }
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    return _alts.GetFixedLength();

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        int length = _left.GetFixedLength();
                        if (length >= 0)
                        {
                            if (_right.GetFixedLength() == length)
                            {
                                return length;
                            }
                        }
                        break;
                    }
            }

            return -1;
        }

        private Dictionary<(S, uint), SymbolicRegexNode<S>>? _MkDerivative_Cache;

        /// <summary>
        /// Takes the derivative of the symbolic regex wrt elem.
        /// Assumes that elem is either a minterm wrt the predicates of the whole regex or a singleton set.
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <returns></returns>
        internal SymbolicRegexNode<S> CreateDerivative(S elem, uint context)
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(CreateDerivative, elem, context);
            }

            if (this == _builder._anyStar || this == _builder._nothing)
            {
                return this;
            }

            if (Kind == SymbolicRegexNodeKind.Or && !_isInternalizedUnion)
            {
                // Internalize the node before proceeding
                // this node could end up being internalized or replaced by
                // an already previously created object (!= this)
                SymbolicRegexNode<S> this_internalized = Internalize(this);
                Debug.Assert(this_internalized._isInternalizedUnion);
                if (this_internalized != this)
                {
                    return this_internalized.CreateDerivative(elem, context);
                }
            }

            _MkDerivative_Cache ??= new();
            (S, uint) key = (elem, context);
            SymbolicRegexNode<S>? deriv;
            if (_MkDerivative_Cache.TryGetValue(key, out deriv))
            {
                Debug.Assert(deriv != null);
                return deriv;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    deriv = _builder._solver.IsSatisfiable(_builder._solver.And(elem, _set)) ?
                        _builder.Epsilon :
                        _builder._nothing;
                    break;

                case SymbolicRegexNodeKind.Loop:
                    {
                        #region d(a, R*) = d(a,R)R*
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> step = _left.CreateDerivative(elem, context);

                        if (step == _builder._nothing || _upper == 0)
                        {
                            deriv = _builder._nothing;
                            break;
                        }

                        if (IsStar)
                        {
                            deriv = _builder.CreateConcat(step, this);
                            break;
                        }

                        if (IsPlus)
                        {
                            SymbolicRegexNode<S> star = _builder.CreateLoop(_left, IsLazy);
                            deriv = _builder.CreateConcat(step, star);
                            break;
                        }

                        int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                        int newlower = _lower == 0 ? 0 : _lower - 1;
                        SymbolicRegexNode<S> rest = _builder.CreateLoop(_left, IsLazy, newlower, newupper);
                        deriv = _builder.CreateConcat(step, rest);
                        break;
                        #endregion
                    }

                case SymbolicRegexNodeKind.Concat:
                    {
                        #region d(a, AB) = d(a,A)B | (if A nullable then d(a,B))
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> leftd = _left.CreateDerivative(elem, context);
                        SymbolicRegexNode<S> first = _builder.CreateConcat(leftd, _right);

                        if (_left.IsNullableFor(context))
                        {
                            SymbolicRegexNode<S> second = _right.CreateDerivative(elem, context);
                            deriv = _builder.Or(first, second);
                            break;
                        }

                        deriv = first;
                        break;
                        #endregion
                    }

                case SymbolicRegexNodeKind.Or:
                    {
                        #region d(a,A|B) = d(a,A)|d(a,B)
                        Debug.Assert(_alts is not null && _alts._kind == SymbolicRegexNodeKind.Or);
                        SymbolicRegexSet<S> alts_deriv = _alts.CreateDerivative(elem, context);
                        // At this point alts_deriv can be the empty conjunction denoting .*
                        deriv = _builder.Or(alts_deriv);
                        break;
                        #endregion
                    }

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        #region d(a,A|B) = d(a,A)|d(a,B)
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> leftd = _left.CreateDerivative(elem, context);
                        SymbolicRegexNode<S> rightd = _right.CreateDerivative(elem, context);
                        deriv = _builder.OrderedOr(leftd, rightd);
                        break;
                        #endregion
                    }

                case SymbolicRegexNodeKind.And:
                    {
                        #region d(a,A & B) = d(a,A) & d(a,B)
                        Debug.Assert(_alts is not null && _alts._kind == SymbolicRegexNodeKind.And);
                        SymbolicRegexSet<S> alts_deriv = _alts.CreateDerivative(elem, context);
                        // At this point alts_deriv can be the empty disjunction denoting nothing
                        deriv = _builder.And(alts_deriv);
                        break;
                        #endregion
                    }

                case SymbolicRegexNodeKind.Not:
                    {
                        #region d(a,~(A)) = ~(d(a,A))
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> leftD = _left.CreateDerivative(elem, context);
                        deriv = _builder.Not(leftD);
                        break;
                        #endregion
                    }

                default:
                    deriv = _builder._nothing;
                    break;
            }

            _MkDerivative_Cache[key] = deriv;
            return deriv;
        }

        private TransitionRegex<S>? _transitionRegex;
        /// <summary>
        /// Computes the symbolic derivative as a transition regex.
        /// Transitions are in the tree left to right in the order the backtracking engine would explore them.
        /// </summary>
        internal TransitionRegex<S> CreateDerivative()
        {
            if (_transitionRegex is not null)
            {
                return _transitionRegex;
            }

            if (IsNothing || IsEpsilon)
            {
                _transitionRegex = TransitionRegex<S>.Leaf(_builder._nothing);
                return _transitionRegex;
            }

            if (IsAnyStar || IsAnyPlus)
            {
                _transitionRegex = TransitionRegex<S>.Leaf(_builder._anyStar);
                return _transitionRegex;
            }

            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(CreateDerivative);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    _transitionRegex = TransitionRegex<S>.Conditional(_set, TransitionRegex<S>.Leaf(_builder.Epsilon), TransitionRegex<S>.Leaf(_builder._nothing));
                    break;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    TransitionRegex<S> mainTransition = _left.CreateDerivative().Concat(_right);

                    if (!_left.CanBeNullable)
                    {
                        // If _left is never nullable
                        _transitionRegex = mainTransition;
                    }
                    else if (_left.IsNullable)
                    {
                        // If _left is unconditionally nullable
                        _transitionRegex = TransitionRegex<S>.Union(mainTransition, _right.CreateDerivative());
                    }
                    else
                    {
                        // The left side contains anchors and can be nullable in some context
                        // Extract the nullability as the lookaround condition
                        SymbolicRegexNode<S> leftNullabilityTest = _left.ExtractNullabilityTest();
                        _transitionRegex = TransitionRegex<S>.Lookaround(leftNullabilityTest, TransitionRegex<S>.Union(mainTransition, _right.CreateDerivative()), mainTransition);
                    }
                    break;

                case SymbolicRegexNodeKind.Loop:
                    // d(R*) = d(R+) = d(R)R*
                    Debug.Assert(_left is not null);
                    Debug.Assert(_upper > 0);
                    TransitionRegex<S> step = _left.CreateDerivative();

                    if (IsStar || IsPlus)
                    {
                        _transitionRegex = step.Concat(_builder.CreateLoop(_left, IsLazy));
                    }
                    else
                    {
                        int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                        int newlower = _lower == 0 ? 0 : _lower - 1;
                        SymbolicRegexNode<S> rest = _builder.CreateLoop(_left, IsLazy, newlower, newupper);
                        _transitionRegex = step.Concat(rest);
                    }
                    break;

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    _transitionRegex = TransitionRegex<S>.Leaf(_builder._nothing);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        _transitionRegex = TransitionRegex<S>.Union(_transitionRegex, elem.CreateDerivative());
                    }
                    break;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _transitionRegex = TransitionRegex<S>.Union(_left.CreateDerivative(), _right.CreateDerivative());
                    break;

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    _transitionRegex = TransitionRegex<S>.Leaf(_builder._anyStar);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        _transitionRegex = TransitionRegex<S>.Intersect(_transitionRegex, elem.CreateDerivative());
                    }
                    break;

                case SymbolicRegexNodeKind.Not:
                    Debug.Assert(_left is not null);
                    _transitionRegex = _left.CreateDerivative().Complement();
                    break;

                default:
                    _transitionRegex = TransitionRegex<S>.Leaf(_builder._nothing);
                    break;
            }
            return _transitionRegex;
        }

        // These are the cache for transition regexes with effects
        private TransitionRegex<S>? _transitionRegexWithEffectsEager;
        private TransitionRegex<S>? _transitionRegexWithEffectsAny;
        private ref TransitionRegex<S>? TransitionRegexWithEffects(bool eager) => ref eager ? ref _transitionRegexWithEffectsEager : ref _transitionRegexWithEffectsAny;

        /// <summary>
        /// Computes the symbolic derivative as a transition regex.
        /// Transitions are in the tree left to right in the order the backtracking engine would explore them.
        /// The transitions also include the effects (e.g. capture starts and ends) along their paths.
        /// </summary>
        /// <param name="eager">whether to only include paths before the first time the backtracking matchers would hit nullability</param>
        /// <returns>the derivative as a TransitionRegex/></returns>
        internal TransitionRegex<S> CreateDerivativeWithEffects(bool eager)
        {
            // Get a reference to the correct variable to cache on based on eagerness
            ref TransitionRegex<S>? transition = ref TransitionRegexWithEffects(eager);
            if (transition is not null)
            {
                return transition;
            }

            if (IsNothing || IsEpsilon)
            {
                transition = TransitionRegex<S>.Leaf(_builder._nothing);
                return transition;
            }

            if (IsAnyStar || IsAnyPlus)
            {
                transition = TransitionRegex<S>.Leaf(_builder._anyStar);
                return transition;
            }

            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(CreateDerivativeWithEffects, eager);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    transition = TransitionRegex<S>.Conditional(_set, TransitionRegex<S>.Leaf(_builder.Epsilon), TransitionRegex<S>.Leaf(_builder._nothing));
                    break;

                case SymbolicRegexNodeKind.Concat:
                    {
                        // The Concat case below explicitly handles cases where the left side is an alternation or a loop.
                        // This is required for properly maintaining transition ordering and handling eagerness.

                        Debug.Assert(_left is not null && _right is not null);

                        TransitionRegex<S> leftTransition = _left.CreateDerivativeWithEffects(eager).Concat(_right);
                        if (!_left.CanBeNullable)
                        {
                            // If the left side can't be nullable then the character must be consumed there
                            transition = leftTransition;
                        }
                        else if (_left._kind == SymbolicRegexNodeKind.OrderedOr)
                        {
                            Debug.Assert(_left._left is not null && _left._right is not null);

                            // This pattern is for the path where the backtracking matcher would find the match from the first alternative
                            SymbolicRegexNode<S> leftLeftPath = _builder.CreateConcat(_left._left, _right);
                            // The eager derivative of that path will be used when the pattern is nullable, i.e., the backtracking matcher
                            // would end the match rather than go onto paths in the second alternative.
                            TransitionRegex<S> leftLeftTransition = leftLeftPath.CreateDerivativeWithEffects(eager);
                            // When the path through the first alternative is not nullable, this transition that includes all derivatives from
                            // it is used.
                            TransitionRegex<S> orTransition = TransitionRegex<S>.Union(leftLeftPath.CreateDerivativeWithEffects(false),
                                _builder.CreateConcat(_left._right, _right).CreateDerivativeWithEffects(eager), ordered: true);

                            // Based on the nullability of the path thorugh the first alternative, select or construct the transition for when
                            // the left side of the concatenation is nullable.
                            TransitionRegex<S> leftNullableTransition = eager && leftLeftPath.CanBeNullable ?
                                (leftLeftPath.IsNullable ?
                                    leftLeftTransition :
                                    TransitionRegex<S>.Lookaround(leftLeftPath.ExtractNullabilityTest(),
                                        leftLeftTransition,
                                        orTransition)) :
                                orTransition;
                            // Select or construct the transition based on whether the left side is nullable
                            transition = _left.IsNullable ?
                                leftNullableTransition :
                                TransitionRegex<S>.Lookaround(_left.ExtractNullabilityTest(),
                                    leftNullableTransition,
                                    leftTransition);
                        }
                        else
                        {
                            // The full derivative through the left side is used when the right side is not nullable or the derivative is not eager
                            TransitionRegex<S> leftAnyTransition = _left.CreateDerivativeWithEffects(false).Concat(_right);
                            // Select or construct the case where the left side consumes the character
                            TransitionRegex<S> mainTransition = eager ?
                                (_right.CanBeNullable ?
                                    (_right.IsNullable ?
                                        leftTransition :
                                        TransitionRegex<S>.Lookaround(_right.ExtractNullabilityTest(),
                                            leftTransition,
                                            leftAnyTransition)) :
                                    leftAnyTransition) :
                                leftAnyTransition;
                            // Construct the case where the right side consumes the character. Any effects from the left side are applied
                            TransitionRegex<S> nullTransition = _left.WrapEffects(_right.CreateDerivativeWithEffects(eager));
                            // Order the transitions. If the left side is a lazy loop that is nullable due to its lower bound then prefer the right side
                            TransitionRegex<S> orTransition = _left._kind == SymbolicRegexNodeKind.Loop && _left.IsLazy && _left._lower == 0 ?
                                TransitionRegex<S>.Union(nullTransition, mainTransition, ordered: true) :
                                TransitionRegex<S>.Union(mainTransition, nullTransition, ordered: true);

                            // Select or construct the transition based on whether the left side is nullable
                            transition = _left.IsNullable ?
                                orTransition :
                                TransitionRegex<S>.Lookaround(_left.ExtractNullabilityTest(),
                                    orTransition,
                                    leftTransition);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Loop:
                    // d(R*) = d(R+) = d(R)R*
                    Debug.Assert(_left is not null);
                    Debug.Assert(_upper > 0);

                    if (eager && IsLazy && _lower == 0)
                    {
                        // This is nothing because the backtracking matcher would prefer to exit the loop
                        transition = TransitionRegex<S>.Leaf(_builder._nothing);
                    }
                    else
                    {
                        TransitionRegex<S> step = _left.CreateDerivativeWithEffects(eager);
                        if (IsStar || IsPlus)
                        {
                            transition = step.Concat(_builder.CreateLoop(_left, IsLazy));
                        }
                        else
                        {
                            int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                            int newlower = _lower == 0 ? 0 : _lower - 1;
                            SymbolicRegexNode<S> rest = _builder.CreateLoop(_left, IsLazy, newlower, newupper);
                            transition = step.Concat(rest);
                        }
                    }
                    break;

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        // The transition when the first alternative is nullable
                        TransitionRegex<S> leftTransition = _left.CreateDerivativeWithEffects(eager);
                        // The transition when the backtracking engines could continue to the second alternative
                        TransitionRegex<S> orTransition = TransitionRegex<S>.Union(_left.CreateDerivativeWithEffects(false),
                            _right.CreateDerivativeWithEffects(eager), ordered: true);
                        // Select or construct the transition based on whether the first alternative is nullable
                        transition = eager && _left.CanBeNullable ?
                            (_left.IsNullable ?
                                leftTransition :
                                TransitionRegex<S>.Lookaround(_left.ExtractNullabilityTest(),
                                    leftTransition,
                                    orTransition)) :
                            orTransition;
                        break;
                    }

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    transition = TransitionRegex<S>.Leaf(_builder._nothing);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        transition = TransitionRegex<S>.Union(transition, elem.CreateDerivativeWithEffects(eager));
                    }
                    break;

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    transition = TransitionRegex<S>.Leaf(_builder._anyStar);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        transition = TransitionRegex<S>.Intersect(transition, elem.CreateDerivativeWithEffects(eager));
                    }
                    break;

                case SymbolicRegexNodeKind.Not:
                    Debug.Assert(_left is not null);
                    transition = _left.CreateDerivativeWithEffects(eager).Complement();
                    break;

                default:
                    transition = TransitionRegex<S>.Leaf(_builder._nothing);
                    break;
            }
            return transition;
        }

        /// <summary>
        /// Wrap a TransitionRegex with the effects under this node. The effects are valid when this node is nullable.
        /// </summary>
        /// <remarks>
        /// The construction follows the paths that the backtracking matcher would take. For example in ()|() only the effects for the first
        /// alternative will be included.
        /// </remarks>
        internal TransitionRegex<S> WrapEffects(TransitionRegex<S> transition)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(WrapEffects, transition);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    Debug.Assert(_left.CanBeNullable && _right.CanBeNullable);
                    transition = _left.WrapEffects(transition);
                    transition = _right.WrapEffects(transition);
                    break;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    // Apply effect when backtracking engine would enter loop
                    if (_lower != 0)
                    {
                        Debug.Assert(_left.CanBeNullable);
                        transition = _left.WrapEffects(transition);
                    }
                    else if (_upper != 0 && !IsLazy && _left.CanBeNullable)
                    {
                        TransitionRegex<S> bodyTransition = _left.WrapEffects(transition);
                        transition = _left.IsNullable ?
                            bodyTransition :
                            TransitionRegex<S>.Lookaround(_left.ExtractNullabilityTest(),
                                bodyTransition,
                                transition);
                    }
                    break;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    if (!_left.CanBeNullable)
                    {
                        // Left can't be nullable so right must be visited
                        Debug.Assert(_right.CanBeNullable);
                        transition = _right.WrapEffects(transition);
                    }
                    else if (!_right.CanBeNullable)
                    {
                        // Right can't be nullable so left must be visited
                        Debug.Assert(_left.CanBeNullable);
                        transition = _left.WrapEffects(transition);
                    }
                    else
                    {
                        // Prefer left side if it is nullable, otherwise right side
                        TransitionRegex<S> leftTransition = _left.WrapEffects(transition);
                        transition = _left.IsNullable ?
                            leftTransition :
                            TransitionRegex<S>.Lookaround(_left.ExtractNullabilityTest(),
                                leftTransition,
                                _right.WrapEffects(transition));
                    }
                    break;

                case SymbolicRegexNodeKind.CaptureStart:
                    // Add the effect to record the capture start
                    transition = TransitionRegex<S>.Effect(transition,
                        new DerivativeEffect(DerivativeEffectKind.CaptureStart, _lower));
                    break;

                case SymbolicRegexNodeKind.CaptureEnd:
                    // Add the effect to record the capture start
                    transition = TransitionRegex<S>.Effect(transition,
                        new DerivativeEffect(DerivativeEffectKind.CaptureEnd, _lower));
                    break;

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        if (elem.CanBeNullable)
                            transition = elem.IsNullable ?
                                elem.WrapEffects(transition) :
                                TransitionRegex<S>.Lookaround(elem.ExtractNullabilityTest(),
                                    elem.WrapEffects(transition),
                                    transition);
                    }
                    break;

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        Debug.Assert(elem.CanBeNullable);
                        transition = elem.WrapEffects(transition);
                    }
                    break;
            }
            return transition;
        }

        /// <summary>
        /// Find all effects under this node and supply them to the callback.
        /// </summary>
        /// <remarks>
        /// The construction is similar to WrapEffects.
        /// </remarks>
        /// <param name="apply">action called for each effect</param>
        /// <param name="context">the current context to determine nullability</param>
        internal void ApplyEffects(Action<DerivativeEffect> apply, uint context)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(ApplyEffects, apply, context);
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    Debug.Assert(_left.IsNullableFor(context) && _right.IsNullableFor(context));
                    _left.ApplyEffects(apply, context);
                    _right.ApplyEffects(apply, context);
                    break;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    // Apply effect when backtracking engine would enter loop
                    if (_lower != 0 || (_upper != 0 && !IsLazy && _left.IsNullableFor(context)))
                    {
                        Debug.Assert(_left.IsNullableFor(context));
                        _left.ApplyEffects(apply, context);
                    }
                    break;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    if (_left.IsNullableFor(context))
                    {
                        // Prefer the left side
                        _left.ApplyEffects(apply, context);
                    }
                    else
                    {
                        // Otherwise right side must be nullable
                        Debug.Assert(_right.IsNullableFor(context));
                        _right.ApplyEffects(apply, context);
                    }
                    break;

                case SymbolicRegexNodeKind.CaptureStart:
                    apply(new DerivativeEffect(DerivativeEffectKind.CaptureStart, _lower));
                    break;

                case SymbolicRegexNodeKind.CaptureEnd:
                    apply(new DerivativeEffect(DerivativeEffectKind.CaptureEnd, _lower));
                    break;

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        if (elem.IsNullableFor(context))
                            elem.ApplyEffects(apply, context);
                    }
                    break;

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        Debug.Assert(elem.IsNullableFor(context));
                        elem.ApplyEffects(apply, context);
                    }
                    break;
            }
        }

        /// <summary>
        /// Computes the closure of CreateDerivative, by exploring all the leaves
        /// of the transition regex until no more new leaves are found.
        /// Converts the resulting transition system into a symbolic NFA.
        /// If the exploration remains incomplete due to the given state bound
        /// being reached then the InComplete property of the constructed NFA is true.
        /// </summary>
        internal SymbolicNFA<S> Explore(int bound) => SymbolicNFA<S>.Explore(this, bound);

        /// <summary>Extracts the nullability test as a Boolean combination of anchors</summary>
        public SymbolicRegexNode<S> ExtractNullabilityTest()
        {
            if (IsNullable)
            {
                return _builder._anyStar;
            }

            if (!CanBeNullable)
            {
                return _builder._nothing;
            }

            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(ExtractNullabilityTest);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.BeginningAnchor:
                case SymbolicRegexNodeKind.EndAnchor:
                case SymbolicRegexNodeKind.BOLAnchor:
                case SymbolicRegexNodeKind.EOLAnchor:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                case SymbolicRegexNodeKind.EndAnchorZ:
                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    return this;
                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    return _builder.And(_left.ExtractNullabilityTest(), _right.ExtractNullabilityTest());
                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    SymbolicRegexNode<S> disjunction = _builder._nothing;
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        disjunction = _builder.Or(disjunction, elem.ExtractNullabilityTest());
                    }
                    return disjunction;
                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    return _builder.OrderedOr(_left.ExtractNullabilityTest(), _right.ExtractNullabilityTest());
                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    SymbolicRegexNode<S> conjunction = _builder._anyStar;
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        conjunction = _builder.And(conjunction, elem.ExtractNullabilityTest());
                    }
                    return conjunction;
                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    return _left.ExtractNullabilityTest();
                default:
                    // All remaining cases could not be nullable or were trivially nullable
                    // Singleton cannot be nullable and Epsilon and FixedLengthMarker are trivially nullable
                    Debug.Assert(_kind == SymbolicRegexNodeKind.Not && _left is not null);
                    return _builder.Not(_left.ExtractNullabilityTest());
            }
        }

        public override int GetHashCode()
        {
            return _hashcode;
        }

        private int ComputeHashCode()
        {
            switch (_kind)
            {
                case SymbolicRegexNodeKind.EndAnchor:
                case SymbolicRegexNodeKind.BeginningAnchor:
                case SymbolicRegexNodeKind.BOLAnchor:
                case SymbolicRegexNodeKind.EOLAnchor:
                case SymbolicRegexNodeKind.Epsilon:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                case SymbolicRegexNodeKind.EndAnchorZ:
                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    return HashCode.Combine(_kind, _info);

                case SymbolicRegexNodeKind.FixedLengthMarker:
                case SymbolicRegexNodeKind.CaptureStart:
                case SymbolicRegexNodeKind.CaptureEnd:
                    return HashCode.Combine(_kind, _lower);

                case SymbolicRegexNodeKind.Loop:
                    return HashCode.Combine(_kind, _left, _lower, _upper, _info);

                case SymbolicRegexNodeKind.Or or SymbolicRegexNodeKind.And:
                    return HashCode.Combine(_kind, _alts, _info);

                case SymbolicRegexNodeKind.Concat:
                case SymbolicRegexNodeKind.OrderedOr:
                    return HashCode.Combine(_left, _right, _info);

                case SymbolicRegexNodeKind.Singleton:
                    return HashCode.Combine(_kind, _set);

                default:
                    Debug.Assert(_kind == SymbolicRegexNodeKind.Not);
                    return HashCode.Combine(_kind, _left, _info);
            };
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not SymbolicRegexNode<S> that)
            {
                return false;
            }

            if (this == that)
            {
                return true;
            }

            if (_kind != that._kind)
            {
                return false;
            }

            if (_kind == SymbolicRegexNodeKind.Or)
            {
                if (_isInternalizedUnion && that._isInternalizedUnion)
                {
                    // Internalized nodes that are not identical are not equal
                    return false;
                }

                // Check equality of the sets of regexes
                Debug.Assert(_alts is not null && that._alts is not null);
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    return StackHelper.CallOnEmptyStack(_alts.Equals, that._alts);
                }
                return _alts.Equals(that._alts);
            }

            return false;
        }

        private void ToStringForLoop(StringBuilder sb)
        {
            if (_kind == SymbolicRegexNodeKind.Singleton)
            {
                ToString(sb);
            }
            else
            {
                sb.Append('(');
                ToString(sb);
                sb.Append(')');
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            ToString(sb);
            return sb.ToString();
        }

        internal void ToString(StringBuilder sb)
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(ToString, sb);
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.EndAnchor:
                    sb.Append("\\z");
                    return;

                case SymbolicRegexNodeKind.BeginningAnchor:
                    sb.Append("\\A");
                    return;

                case SymbolicRegexNodeKind.BOLAnchor:
                    sb.Append('^');
                    return;

                case SymbolicRegexNodeKind.EOLAnchor:
                    sb.Append('$');
                    return;

                case SymbolicRegexNodeKind.Epsilon:
                case SymbolicRegexNodeKind.FixedLengthMarker:
                    return;

                case SymbolicRegexNodeKind.BoundaryAnchor:
                    sb.Append("\\b");
                    return;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                    sb.Append("\\B");
                    return;

                case SymbolicRegexNodeKind.EndAnchorZ:
                    sb.Append("\\Z");
                    return;

                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    sb.Append("\\a");
                    return;

                case SymbolicRegexNodeKind.Or:
                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    _alts.ToString(sb);
                    return;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.ToString(sb);
                    sb.Append('|');
                    _right.ToString(sb);
                    return;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.ToString(sb);
                    _right.ToString(sb);
                    return;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    sb.Append(_builder._solver.PrettyPrint(_set));
                    return;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    if (IsAnyStar)
                    {
                        sb.Append(".*");
                    }
                    else if (_lower == 0 && _upper == 1)
                    {
                        _left.ToStringForLoop(sb);
                        sb.Append('?');
                    }
                    else if (IsStar)
                    {
                        _left.ToStringForLoop(sb);
                        sb.Append('*');
                        if (IsLazy)
                        {
                            sb.Append('?');
                        }
                    }
                    else if (IsPlus)
                    {
                        _left.ToStringForLoop(sb);
                        sb.Append('+');
                        if (IsLazy)
                        {
                            sb.Append('?');
                        }
                    }
                    else if (_lower == 0 && _upper == 0)
                    {
                        sb.Append("()");
                    }
                    else
                    {
                        _left.ToStringForLoop(sb);
                        sb.Append('{');
                        sb.Append(_lower);
                        if (!IsBoundedLoop)
                        {
                            sb.Append(',');
                        }
                        else if (_lower != _upper)
                        {
                            sb.Append(',');
                            sb.Append(_upper);
                        }
                        sb.Append('}');
                        if (IsLazy)
                            sb.Append('?');
                    }
                    return;

                case SymbolicRegexNodeKind.CaptureStart:
                    sb.Append('('); // The group number may be wrong
                    return;

                case SymbolicRegexNodeKind.CaptureEnd:
                    sb.Append(')');
                    return;

                default:
                    // Using the operator ~ for complement
                    Debug.Assert(_kind == SymbolicRegexNodeKind.Not);
                    Debug.Assert(_left is not null);
                    sb.Append("~(");
                    _left.ToString(sb);
                    sb.Append(')');
                    return;
            }
        }

        /// <summary>
        /// Returns the set of all predicates that occur in the regex or
        /// the set containing True if there are no precidates in the regex, e.g., if the regex is "^"
        /// </summary>
        public HashSet<S> GetPredicates()
        {
            var predicates = new HashSet<S>();
            CollectPredicates_helper(predicates);
            return predicates;
        }

        /// <summary>
        /// Collects all predicates that occur in the regex into the given set predicates
        /// </summary>
        private void CollectPredicates_helper(HashSet<S> predicates)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(CollectPredicates_helper, predicates);
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.BOLAnchor:
                case SymbolicRegexNodeKind.EOLAnchor:
                case SymbolicRegexNodeKind.EndAnchorZ:
                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    predicates.Add(_builder._newLinePredicate);
                    return;

                case SymbolicRegexNodeKind.BeginningAnchor:
                case SymbolicRegexNodeKind.EndAnchor:
                case SymbolicRegexNodeKind.Epsilon:
                case SymbolicRegexNodeKind.FixedLengthMarker:
                case SymbolicRegexNodeKind.CaptureStart:
                case SymbolicRegexNodeKind.CaptureEnd:
                    return;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    predicates.Add(_set);
                    return;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    _left.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexNodeKind.Or:
                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<S> sr in _alts)
                    {
                        sr.CollectPredicates_helper(predicates);
                    }
                    return;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.CollectPredicates_helper(predicates);
                    _right.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexNodeKind.Concat:
                    // avoid deep nested recursion over long concat nodes
                    SymbolicRegexNode<S> conc = this;
                    while (conc._kind == SymbolicRegexNodeKind.Concat)
                    {
                        Debug.Assert(conc._left is not null && conc._right is not null);
                        conc._left.CollectPredicates_helper(predicates);
                        conc = conc._right;
                    }
                    conc.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexNodeKind.Not:
                    Debug.Assert(_left is not null);
                    _left.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                    predicates.Add(_builder._wordLetterPredicateForAnchors);
                    return;

                default:
                    Debug.Fail($"{nameof(CollectPredicates_helper)}:{_kind}");
                    break;
            }
        }

        /// <summary>
        /// Compute all the minterms from the predicates in this regex.
        /// If S implements IComparable then sort the result in increasing order.
        /// </summary>
        public S[] ComputeMinterms()
        {
            Debug.Assert(typeof(S).IsAssignableTo(typeof(IComparable<S>)));

            HashSet<S> predicates = GetPredicates();
            List<S> mt = _builder._solver.GenerateMinterms(predicates);
            mt.Sort();
            return mt.ToArray();
        }

        /// <summary>
        /// Create the reverse of this regex
        /// </summary>
        public SymbolicRegexNode<S> Reverse()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Reverse);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    return _builder.CreateLoop(_left.Reverse(), IsLazy, _lower, _upper);

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> rev = _left.Reverse();
                        SymbolicRegexNode<S> rest = _right;
                        while (rest._kind == SymbolicRegexNodeKind.Concat)
                        {
                            Debug.Assert(rest._left is not null && rest._right is not null);
                            SymbolicRegexNode<S> rev1 = rest._left.Reverse();
                            rev = _builder.CreateConcat(rev1, rev);
                            rest = rest._right;
                        }
                        SymbolicRegexNode<S> restr = rest.Reverse();
                        rev = _builder.CreateConcat(restr, rev);
                        return rev;
                    }

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    return _builder.Or(_alts.Reverse());

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    return _builder.OrderedOr(_left.Reverse(), _right.Reverse());

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    return _builder.And(_alts.Reverse());

                case SymbolicRegexNodeKind.Not:
                    Debug.Assert(_left is not null);
                    return _builder.Not(_left.Reverse());

                case SymbolicRegexNodeKind.FixedLengthMarker:
                    // Fixed length markers are omitted in reverse
                    return _builder.Epsilon;

                case SymbolicRegexNodeKind.BeginningAnchor:
                    // The reverse of BeginningAnchor is EndAnchor
                    return _builder.EndAnchor;

                case SymbolicRegexNodeKind.EndAnchor:
                    return _builder.BeginningAnchor;

                case SymbolicRegexNodeKind.BOLAnchor:
                    // The reverse of BOLanchor is EOLanchor
                    return _builder.EolAnchor;

                case SymbolicRegexNodeKind.EOLAnchor:
                    return _builder.BolAnchor;

                case SymbolicRegexNodeKind.EndAnchorZ:
                    // The reversal of the \Z anchor
                    return _builder.EndAnchorZReverse;

                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    // This can potentially only happen if a reversed regex is reversed again.
                    // Thus, this case is unreachable here, but included for completeness.
                    return _builder.EndAnchorZ;

                case SymbolicRegexNodeKind.CaptureStart:
                    return CreateCaptureEnd(_builder, _lower);

                case SymbolicRegexNodeKind.CaptureEnd:
                    return CreateCaptureStart(_builder, _lower);

                // Remaining cases map to themselves:
                case SymbolicRegexNodeKind.Epsilon:
                case SymbolicRegexNodeKind.Singleton:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                default:
                    return this;
            }
        }

        /// <summary>
        /// Transform OrderdOr to Or nodes and any lazy loops to eager ones.
        /// </summary>
        /// <remarks>
        /// This transformation allows the second reverse pass to use the same derivative function as the third pass,
        /// which must respect the backtracking engines alternation and lazyness semantics.
        /// </remarks>
        public SymbolicRegexNode<S> IgnoreOrOrderAndLazyness()
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(IgnoreOrOrderAndLazyness);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Loop:
                    {
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> body = _left.IgnoreOrOrderAndLazyness();
                        return body == _left && !IsLazy ?
                            this :
                            CreateLoop(_builder, body, _lower, _upper, isLazy: false);
                    }

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> left1 = _left.IgnoreOrOrderAndLazyness();
                        SymbolicRegexNode<S> right1 = _right.IgnoreOrOrderAndLazyness();

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            CreateConcat(_builder, left1, right1);
                    }

                case SymbolicRegexNodeKind.Or:
                case SymbolicRegexNodeKind.And:
                    {
                        Debug.Assert(_alts != null);
                        var elements = new SymbolicRegexNode<S>[_alts.Count];
                        int i = 0;
                        bool someChanged = false;
                        foreach (SymbolicRegexNode<S> alt in _alts)
                        {
                            elements[i] = alt.IgnoreOrOrderAndLazyness();
                            someChanged |= alt != elements[i];
                            i += 1;
                        }
                        Debug.Assert(i == elements.Length);
                        return !someChanged ? this :
                            _kind == SymbolicRegexNodeKind.Or ? Or(_builder, elements) :
                            And(_builder, elements);
                    }

                case SymbolicRegexNodeKind.Not:
                    {
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> body = _left.IgnoreOrOrderAndLazyness();
                        return body == _left ?
                            this :
                            Not(_builder, body);
                    }

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        return Or(_builder, _left.IgnoreOrOrderAndLazyness(), _right.IgnoreOrOrderAndLazyness());
                    }

                default:
                    return this;
            }
        }

        internal bool StartsWithLoop(int upperBoundLowestValue = 1)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(StartsWithLoop, upperBoundLowestValue);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Loop:
                    return (_upper < int.MaxValue) && (_upper > upperBoundLowestValue);

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    return _left.StartsWithLoop(upperBoundLowestValue) || (_left.IsNullable && _right.StartsWithLoop(upperBoundLowestValue));

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    return _alts.StartsWithLoop(upperBoundLowestValue);

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    return _left.StartsWithLoop(upperBoundLowestValue) || _right.StartsWithLoop(upperBoundLowestValue);

                default:
                    return false;
            };
        }


        /// <summary>Get the predicate that covers all elements that make some progress.</summary>
        internal S GetStartSet() => _startSet;

        /// <summary>Compute the predicate that covers all elements that make some progress.</summary>
        private S ComputeStartSet()
        {
            switch (_kind)
            {
                // Anchors and () do not contribute to the startset
                case SymbolicRegexNodeKind.Epsilon:
                case SymbolicRegexNodeKind.FixedLengthMarker:
                case SymbolicRegexNodeKind.EndAnchor:
                case SymbolicRegexNodeKind.BeginningAnchor:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                case SymbolicRegexNodeKind.EOLAnchor:
                case SymbolicRegexNodeKind.EndAnchorZ:
                case SymbolicRegexNodeKind.EndAnchorZReverse:
                case SymbolicRegexNodeKind.BOLAnchor:
                case SymbolicRegexNodeKind.CaptureStart:
                case SymbolicRegexNodeKind.CaptureEnd:
                    return _builder._solver.False;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    return _set;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    return _left._startSet;

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        S startSet = _left.CanBeNullable ? _builder._solver.Or(_left._startSet, _right._startSet) : _left._startSet;
                        return startSet;
                    }

                case SymbolicRegexNodeKind.Or:
                    {
                        Debug.Assert(_alts is not null);
                        S startSet = _builder._solver.False;
                        foreach (SymbolicRegexNode<S> alt in _alts)
                        {
                            startSet = _builder._solver.Or(startSet, alt._startSet);
                        }
                        return startSet;
                    }

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        return _builder._solver.Or(_left._startSet, _right._startSet);
                    }

                case SymbolicRegexNodeKind.And:
                    {
                        Debug.Assert(_alts is not null);
                        S startSet = _builder._solver.True;
                        foreach (SymbolicRegexNode<S> alt in _alts)
                        {
                            startSet = _builder._solver.And(startSet, alt._startSet);
                        }
                        return startSet;
                    }

                default:
                    Debug.Assert(_kind == SymbolicRegexNodeKind.Not);
                    return _builder._solver.True;
            }
        }

        /// <summary>
        /// Returns true if this is a loop with an upper bound
        /// </summary>
        public bool IsBoundedLoop => _kind == SymbolicRegexNodeKind.Loop && _upper < int.MaxValue;

        /// <summary>
        /// Replace anchors that are infeasible by [] wrt the given previous character kind and what continuation is possible.
        /// </summary>
        /// <param name="prevKind">previous character kind</param>
        /// <param name="contWithWL">if true the continuation can start with wordletter or stop</param>
        /// <param name="contWithNWL">if true the continuation can start with nonwordletter or stop</param>
        internal SymbolicRegexNode<S> PruneAnchors(uint prevKind, bool contWithWL, bool contWithNWL)
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(PruneAnchors, prevKind, contWithWL, contWithNWL);
            }

            if (!_info.StartsWithSomeAnchor)
                return this;

            switch (_kind)
            {
                case SymbolicRegexNodeKind.BeginningAnchor:
                    return prevKind == CharKind.BeginningEnd ?
                        this :
                        _builder._nothing; //start anchor is only nullable if the previous character is Start

                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    return ((prevKind & CharKind.BeginningEnd) != 0) ?
                        this :
                        _builder._nothing; //rev(\Z) is only nullable if the previous characters is Start or the very first \n

                case SymbolicRegexNodeKind.BoundaryAnchor:
                    return (prevKind == CharKind.WordLetter ? contWithNWL : contWithWL) ?
                        this :
                        // \b is impossible when the previous character is \w but no continuation matches \W
                        // or the previous character is \W but no continuation matches \w
                        _builder._nothing;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                    return (prevKind == CharKind.WordLetter ? contWithWL : contWithNWL) ?
                        this :
                        // \B is impossible when the previous character is \w but no continuation matches \w
                        // or the previous character is \W but no continuation matches \W
                        _builder._nothing;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    SymbolicRegexNode<S> body = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                    return body == _left ?
                        this :
                        CreateLoop(_builder, body, _lower, _upper, IsLazy);

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> left1 = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<S> right1 = _left.IsNullable ? _right.PruneAnchors(prevKind, contWithWL, contWithNWL) : _right;

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            CreateConcat(_builder, left1, right1);
                    }

                case SymbolicRegexNodeKind.Or:
                    {
                        Debug.Assert(_alts != null);
                        var elements = new SymbolicRegexNode<S>[_alts.Count];
                        int i = 0;
                        foreach (SymbolicRegexNode<S> alt in _alts)
                        {
                            elements[i++] = alt.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        }
                        Debug.Assert(i == elements.Length);
                        return Or(_builder, elements);
                    }

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> left1 = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<S> right1 = _right.PruneAnchors(prevKind, contWithWL, contWithNWL);

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            OrderedOr(_builder, left1, right1);
                    }

                default:
                    return this;
            }
        }
    }
}
