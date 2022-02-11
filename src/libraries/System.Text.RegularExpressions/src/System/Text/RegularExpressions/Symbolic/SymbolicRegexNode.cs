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
        internal readonly SymbolicRegexKind _kind;
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
        private SymbolicRegexNode(SymbolicRegexBuilder<S> builder, SymbolicRegexKind kind, SymbolicRegexNode<S>? left, SymbolicRegexNode<S>? right, int lower, int upper, S? set, SymbolicRegexSet<S>? alts, SymbolicRegexInfo info)
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
        private static SymbolicRegexNode<S> Create(SymbolicRegexBuilder<S> builder, SymbolicRegexKind kind, SymbolicRegexNode<S>? left, SymbolicRegexNode<S>? right, int lower, int upper, S? set, SymbolicRegexSet<S>? alts, SymbolicRegexInfo info)
        {
            SymbolicRegexNode<S>? node;
            var key = (kind, left, right, lower, upper, set, alts, info);
            if (!builder._nodeCache.TryGetValue(key, out node))
            {
                // Do not internalize top level Or-nodes or else Antimirov mode will become ineffective
                if (kind == SymbolicRegexKind.Or)
                {
                    node = new(builder, kind, left, right, lower, upper, set, alts, info);
                    return node;
                }

                left = left == null || left._kind != SymbolicRegexKind.Or || left._isInternalizedUnion ? left : Internalize(left);
                right = right == null || right._kind != SymbolicRegexKind.Or || right._isInternalizedUnion ? right : Internalize(right);

                node = new(builder, kind, left, right, lower, upper, set, alts, info);
                builder._nodeCache[key] = node;
            }

            Debug.Assert(node is not null);
            return node;
        }

        /// <summary> Internalize an Or-node that is not yet internalized</summary>
        private static SymbolicRegexNode<S> Internalize(SymbolicRegexNode<S> node)
        {
            Debug.Assert(node._kind == SymbolicRegexKind.Or && !node._isInternalizedUnion);

            (SymbolicRegexKind, SymbolicRegexNode<S>?, SymbolicRegexNode<S>?, int, int, S?, SymbolicRegexSet<S>?, SymbolicRegexInfo) node_key =
                (SymbolicRegexKind.Or, null, null, -1, -1, default(S), node._alts, node._info);
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
        public List<SymbolicRegexNode<S>> ToList(List<SymbolicRegexNode<S>>? list = null, SymbolicRegexKind listKind = SymbolicRegexKind.Concat)
        {
            Debug.Assert(listKind == SymbolicRegexKind.Concat || listKind == SymbolicRegexKind.OrderedOr);
            if (list is null)
                list = new List<SymbolicRegexNode<S>>();
            AppendToList(this, list, listKind);
            return list;

            static void AppendToList(SymbolicRegexNode<S> concat, List<SymbolicRegexNode<S>> list, SymbolicRegexKind listKind)
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
                    case SymbolicRegexKind.Loop:
                        Debug.Assert(_left is not null);
                        is_nullable = _lower == 0 || _left.IsNullableFor(context);
                        break;

                    case SymbolicRegexKind.Concat:
                        Debug.Assert(_left is not null && _right is not null);
                        is_nullable = _left.IsNullableFor(context) && _right.IsNullableFor(context);
                        break;

                    case SymbolicRegexKind.Or:
                    case SymbolicRegexKind.And:
                        Debug.Assert(_alts is not null);
                        is_nullable = _alts.IsNullableFor(context);
                        break;

                    case SymbolicRegexKind.OrderedOr:
                        Debug.Assert(_left is not null && _right is not null);
                        is_nullable = _left.IsNullableFor(context) || _right.IsNullableFor(context);
                        break;

                    case SymbolicRegexKind.Not:
                        Debug.Assert(_left is not null);
                        is_nullable = !_left.IsNullableFor(context);
                        break;

                    case SymbolicRegexKind.StartAnchor:
                        is_nullable = CharKind.Prev(context) == CharKind.StartStop;
                        break;

                    case SymbolicRegexKind.EndAnchor:
                        is_nullable = CharKind.Next(context) == CharKind.StartStop;
                        break;

                    case SymbolicRegexKind.BOLAnchor:
                        // Beg-Of-Line anchor is nullable when the previous character is Newline or Start
                        // note: at least one of the bits must be 1, but both could also be 1 in case of very first newline
                        is_nullable = (CharKind.Prev(context) & CharKind.NewLineS) != 0;
                        break;

                    case SymbolicRegexKind.EOLAnchor:
                        // End-Of-Line anchor is nullable when the next character is Newline or Stop
                        // note: at least one of the bits must be 1, but both could also be 1 in case of \Z
                        is_nullable = (CharKind.Next(context) & CharKind.NewLineS) != 0;
                        break;

                    case SymbolicRegexKind.WBAnchor:
                        // test that prev char is word letter iff next is not not word letter
                        is_nullable = ((CharKind.Prev(context) & CharKind.WordLetter) ^ (CharKind.Next(context) & CharKind.WordLetter)) != 0;
                        break;

                    case SymbolicRegexKind.NWBAnchor:
                        // test that prev char is word letter iff next is word letter
                        is_nullable = ((CharKind.Prev(context) & CharKind.WordLetter) ^ (CharKind.Next(context) & CharKind.WordLetter)) == 0;
                        break;

                    case SymbolicRegexKind.EndAnchorZ:
                        // \Z anchor is nullable when the next character is either the last Newline or Stop
                        // note: CharKind.NewLineS == CharKind.Newline|CharKind.StartStop
                        is_nullable = (CharKind.Next(context) & CharKind.StartStop) != 0;
                        break;

                    case SymbolicRegexKind.CaptureStart:
                    case SymbolicRegexKind.CaptureEnd:
                        is_nullable = true;
                        break;

                    default: // SymbolicRegexKind.EndAnchorZRev:
                             // EndAnchorZRev (rev(\Z)) anchor is nullable when the prev character is either the first Newline or Start
                             // note: CharKind.NewLineS == CharKind.Newline|CharKind.StartStop
                        Debug.Assert(_kind == SymbolicRegexKind.EndAnchorZRev);
                        is_nullable = (CharKind.Prev(context) & CharKind.StartStop) != 0;
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
                    if (_left._kind == SymbolicRegexKind.Singleton)
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
                    if (_left._kind == SymbolicRegexKind.Singleton)
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
                if (_kind == SymbolicRegexKind.Singleton)
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
                if (_kind == SymbolicRegexKind.Singleton)
                {
                    Debug.Assert(_set is not null);
                    return !_builder._solver.IsSatisfiable(_set);
                }

                return false;
            }
        }

        /// <summary>Returns true iff this is a loop whose lower bound is 0 and upper bound is max</summary>
        public bool IsStar => _lower == 0 && _upper == int.MaxValue;

        /// <summary>Returns true iff this is a loop whose lower bound is 0 and upper bound is 1</summary>
        public bool IsMaybe => _lower == 0 && _upper == 1;

        /// <summary>Returns true if this is Epsilon</summary>
        public bool IsEpsilon => _kind == SymbolicRegexKind.Epsilon;

        /// <summary>Gets the kind of the regex</summary>
        internal SymbolicRegexKind Kind => _kind;

        /// <summary>
        /// Returns true iff this is a loop whose lower bound is 1 and upper bound is max
        /// </summary>
        public bool IsPlus => _lower == 1 && _upper == int.MaxValue;

        #region called only once, in the constructor of SymbolicRegexBuilder

        internal static SymbolicRegexNode<S> MkFalse(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, builder._solver.False, null, SymbolicRegexInfo.Mk());

        internal static SymbolicRegexNode<S> MkTrue(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, builder._solver.True, null, SymbolicRegexInfo.Mk(containsSomeCharacter: true));

        internal static SymbolicRegexNode<S> MkWatchDog(SymbolicRegexBuilder<S> builder, int length) =>
            Create(builder, SymbolicRegexKind.WatchDog, null, null, length, -1, default, null, SymbolicRegexInfo.Mk(isAlwaysNullable: true));

        internal static SymbolicRegexNode<S> MkEpsilon(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.Epsilon, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(isAlwaysNullable: true));

        internal static SymbolicRegexNode<S> MkEagerEmptyLoop(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body) =>
            Create(builder, SymbolicRegexKind.Loop, body, null, 0, 0, default, null, SymbolicRegexInfo.Mk(isAlwaysNullable: true, isLazy: false));

        internal static SymbolicRegexNode<S> MkStartAnchor(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.StartAnchor, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithLineAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkEndAnchor(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.EndAnchor, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithLineAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkEndAnchorZ(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.EndAnchorZ, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithLineAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkEndAnchorZRev(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.EndAnchorZRev, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithLineAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkEolAnchor(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.EOLAnchor, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithLineAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkBolAnchor(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.BOLAnchor, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithLineAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkWBAnchor(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.WBAnchor, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithBoundaryAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkNWBAnchor(SymbolicRegexBuilder<S> builder) =>
            Create(builder, SymbolicRegexKind.NWBAnchor, null, null, -1, -1, default, null, SymbolicRegexInfo.Mk(startsWithBoundaryAnchor: true, canBeNullable: true));

        internal static SymbolicRegexNode<S> MkStar(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body) =>
            Create(builder, SymbolicRegexKind.Loop, body, null, 0, int.MaxValue, default, null, SymbolicRegexInfo.Loop(body._info, 0, false));

        internal static SymbolicRegexNode<S> MkPlus(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body) =>
           Create(builder, SymbolicRegexKind.Loop, body, null, 1, int.MaxValue, default, null, SymbolicRegexInfo.Loop(body._info, 1, false));

        #endregion

        internal static SymbolicRegexNode<S> MkSingleton(SymbolicRegexBuilder<S> builder, S set) =>
            Create(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, set, null, SymbolicRegexInfo.Mk(containsSomeCharacter: !set.Equals(builder._solver.False)));

        internal static SymbolicRegexNode<S> MkLoop(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body, int lower, int upper, bool isLazy)
        {
            Debug.Assert(lower >= 0 && lower <= upper);
            return Create(builder, SymbolicRegexKind.Loop, body, null, lower, upper, default, null, SymbolicRegexInfo.Loop(body._info, lower, isLazy));
        }

        internal static SymbolicRegexNode<S> MkOr(SymbolicRegexBuilder<S> builder, params SymbolicRegexNode<S>[] disjuncts) =>
            MkCollection(builder, SymbolicRegexKind.Or, SymbolicRegexSet<S>.CreateMulti(builder, disjuncts, SymbolicRegexKind.Or), SymbolicRegexInfo.Or(GetInfos(disjuncts)));

        internal static SymbolicRegexNode<S> MkOr(SymbolicRegexBuilder<S> builder, SymbolicRegexSet<S> disjuncts)
        {
            Debug.Assert(disjuncts._kind == SymbolicRegexKind.Or || disjuncts.IsEverything);
            return MkCollection(builder, SymbolicRegexKind.Or, disjuncts, SymbolicRegexInfo.Or(GetInfos(disjuncts)));
        }

        internal static SymbolicRegexNode<S> MkAnd(SymbolicRegexBuilder<S> builder, params SymbolicRegexNode<S>[] conjuncts) =>
            MkCollection(builder, SymbolicRegexKind.And, SymbolicRegexSet<S>.CreateMulti(builder, conjuncts, SymbolicRegexKind.And), SymbolicRegexInfo.And(GetInfos(conjuncts)));

        internal static SymbolicRegexNode<S> MkAnd(SymbolicRegexBuilder<S> builder, SymbolicRegexSet<S> conjuncts)
        {
            Debug.Assert(conjuncts.IsNothing || conjuncts._kind == SymbolicRegexKind.And);
            return MkCollection(builder, SymbolicRegexKind.And, conjuncts, SymbolicRegexInfo.And(GetInfos(conjuncts)));
        }

        internal static SymbolicRegexNode<S> MkCaptureStart(SymbolicRegexBuilder<S> builder, int captureNum) =>
            Create(builder, SymbolicRegexKind.CaptureStart, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Mk(isAlwaysNullable: true));

        internal static SymbolicRegexNode<S> MkCaptureEnd(SymbolicRegexBuilder<S> builder, int captureNum) =>
            Create(builder, SymbolicRegexKind.CaptureEnd, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Mk(isAlwaysNullable: true));

        private static SymbolicRegexNode<S> MkCollection(SymbolicRegexBuilder<S> builder, SymbolicRegexKind kind, SymbolicRegexSet<S> alts, SymbolicRegexInfo info) =>
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

        /// <summary>
        /// Make a concatenation of given regexes, if any regex is nothing then return nothing, eliminate
        /// intermediate epsilons. Keep the concatenation flat, assuming both right and left are flat.
        /// </summary>
        internal static SymbolicRegexNode<S> MkConcat(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            if (left == builder._nothing || right == builder._nothing)
                return builder._nothing;

            if (left.IsEpsilon)
                return right;

            if (right.IsEpsilon)
                return left;

            if (left._kind != SymbolicRegexKind.Concat)
            {
                return Create(builder, SymbolicRegexKind.Concat, left, right, -1, -1, default, null, SymbolicRegexInfo.Concat(left._info, right._info));
            }

            SymbolicRegexNode<S> concat = right;
            List<SymbolicRegexNode<S>> left_elems = left.ToList();
            for (int i = left_elems.Count - 1; i >= 0; i--)
            {
                concat = Create(builder, SymbolicRegexKind.Concat, left_elems[i], concat, -1, -1, default, null, SymbolicRegexInfo.Concat(left_elems[i]._info, concat._info));
            }
            return concat;
        }

        /// <summary>
        /// Make an ordered or of given regexes, eliminate nothing regexes and treat .* as consuming element.
        /// Keep the or flat, assuming both right and left are flat.
        /// Apply a counber subsumption/combining optimization, such that e.g. a{2,5}|a{3,10} will be combined to a{2,10}.
        /// </summary>
        internal static SymbolicRegexNode<S> MkOrderedOr(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            if (left.IsAnyStar || right == builder._nothing || left == right)
                return left;
            if (left == builder._nothing)
                return right;

            if (left._kind != SymbolicRegexKind.OrderedOr)
            {
                // Apply the counter subsumption/combining optimization if possible
                var (loop, rest) = left.FirstCounterInfo();
                if (loop != builder._nothing)
                {
                    Debug.Assert(loop._kind == SymbolicRegexKind.Loop && loop._left is not null);
                    var (otherLoop, otherRest) = right.FirstCounterInfo();
                    if (otherLoop != builder._nothing && rest == otherRest)
                    {
                        // Found two adjacent counters with the same continuation, check that the loops are equivalent apart from bounds
                        // and that the bounds form a contiguous interval. Two integer intervals [x1,x2] and [y1,y2] overlap when
                        // x1 <= y2 and y1 <= x2. The union of intervals that just touch is still contiguous, e.g. [2,5] and [6,10] make
                        // [2,10], so the lower bounds are decremented by 1 in the check.
                        Debug.Assert(otherLoop._kind == SymbolicRegexKind.Loop && otherLoop._left is not null);
                        if (loop._left == otherLoop._left && loop.IsLazy == otherLoop.IsLazy &&
                            loop._lower - 1 <= otherLoop._upper && otherLoop._lower - 1 <= loop._upper)
                        {
                            // Loops are equivalent apart from bounds, and the union of the bounds is a contiguous interval
                            // Build a new counter for the union of the ranges
                            SymbolicRegexNode<S> newCounter = MkConcat(builder, MkLoop(builder, loop._left,
                                Math.Min(loop._lower, otherLoop._lower), Math.Max(loop._upper, otherLoop._upper), loop.IsLazy), rest);
                            if (right._kind == SymbolicRegexKind.OrderedOr)
                            {
                                // The right counter came from an or, so include the rest of that or
                                Debug.Assert(right._right is not null);
                                return MkOrderedOr(builder, newCounter, right._right);
                            }
                            else
                            {
                                return newCounter;
                            }
                        }
                    }
                }
                // No need for flattening and counter optimization did not apply, just build the or
                return Create(builder, SymbolicRegexKind.OrderedOr, left, right, -1, -1, default, null, SymbolicRegexInfo.Or(left._info, right._info));
            }

            // If the left side was an or, then it has to be flattened, gather the elements from both sides
            List<SymbolicRegexNode<S>> elems = left.ToList(listKind: SymbolicRegexKind.OrderedOr);
            int firstRightElem = elems.Count;
            right.ToList(elems, listKind: SymbolicRegexKind.OrderedOr);

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
                    or = MkOrderedOr(builder, elems[i], or);
                }
                return or;
            }
            else
            {
                SymbolicRegexNode<S> or = right;
                for (int i = firstRightElem - 1; i >= 0; i--)
                {
                    or = MkOrderedOr(builder, elems[i], or);
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
            if (_kind == SymbolicRegexKind.Loop)
                return (this, _builder._epsilon);
            if (_kind == SymbolicRegexKind.Concat)
            {
                Debug.Assert(_left is not null && _right is not null);
                if (_left.Kind == SymbolicRegexKind.Loop)
                    return (_left, _right);
            }
            if (_kind == SymbolicRegexKind.OrderedOr)
            {
                Debug.Assert(_left is not null);
                return _left.FirstCounterInfo();
            }
            return (_builder._nothing, _builder._nothing);
        }

        internal static SymbolicRegexNode<S> MkNot(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> root)
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
                    Debug.Assert((node._kind == SymbolicRegexKind.Or || node._kind == SymbolicRegexKind.And) && node._alts is not null);
                    // Here all members of _alts have been processed
                    List<SymbolicRegexNode<S>> alts_nnf = new();
                    foreach (SymbolicRegexNode<S> elem in node._alts)
                    {
                        alts_nnf.Add(NNF[elem]);
                    }
                    // Using deMorgan's laws, flip the kind: Or becomes And, And becomes Or
                    SymbolicRegexNode<S> node_nnf = node._kind == SymbolicRegexKind.Or ? MkAnd(builder, alts_nnf.ToArray()) : MkOr(builder, alts_nnf.ToArray());
                    NNF[node] = node_nnf;
                }
                else
                {
                    switch (node._kind)
                    {
                        case SymbolicRegexKind.Not:
                            Debug.Assert(node._left is not null);
                            // Here we assume that top._left is already in NNF, double negation is cancelled out
                            NNF[node] = node._left;
                            break;

                        case SymbolicRegexKind.Or or SymbolicRegexKind.And:
                            Debug.Assert(node._alts is not null);
                            // Push the node for the second time
                            todo.Push((node, true));
                            // Compute the negation normal form of all the members
                            // Their computation is actually the same independent from being inside an 'Or' or 'And' node
                            foreach (var elem in node._alts)
                            {
                                todo.Push((elem, false));
                            }
                            break;

                        case SymbolicRegexKind.Epsilon:
                            //  ~() = .+
                            NNF[node] = SymbolicRegexNode<S>.MkPlus(builder, builder._anyChar);
                            break;

                        case SymbolicRegexKind.Singleton:
                            Debug.Assert(node._set is not null);
                            // ~[] = .*
                            if (node.IsNothing)
                            {
                                NNF[node] = builder._anyStar;
                                break;
                            }
                            goto default;

                        case SymbolicRegexKind.Loop:
                            Debug.Assert(node._left is not null);
                            // ~(.*) = [] and ~(.+) = ()
                            if (node.IsAnyStar)
                            {
                                NNF[node] = builder._nothing;
                                break;
                            }
                            else if (node.IsPlus && node._left.IsAnyChar)
                            {
                                NNF[node] = builder._epsilon;
                                break;
                            }
                            goto default;

                        default:
                            // In all other cases construct the complement
                            NNF[node] = Create(builder, SymbolicRegexKind.Not, node, null, -1, -1, default, null, SymbolicRegexInfo.Not(node._info));
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
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.BOLAnchor:
                case SymbolicRegexKind.EOLAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchorZ:
                case SymbolicRegexKind.EndAnchorZRev:
                case SymbolicRegexKind.WBAnchor:
                case SymbolicRegexKind.NWBAnchor:
                case SymbolicRegexKind.CaptureStart:
                case SymbolicRegexKind.CaptureEnd:
                    return 0;

                case SymbolicRegexKind.Singleton:
                    return 1;

                case SymbolicRegexKind.Loop:
                    {
                        Debug.Assert(_left is not null);
                        if (_lower == _upper)
                        {
                            int body_length = _left.GetFixedLength();
                            if (body_length >= 0)
                            {
                                return _lower * body_length;
                            }
                        }
                        break;
                    }

                case SymbolicRegexKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        int left_length = _left.GetFixedLength();
                        if (left_length >= 0)
                        {
                            int right_length = _right.GetFixedLength();
                            if (right_length >= 0)
                            {
                                return left_length + right_length;
                            }
                        }
                        break;
                    }

                case SymbolicRegexKind.Or:
                    Debug.Assert(_alts is not null);
                    return _alts.GetFixedLength();

                case SymbolicRegexKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        int left_length = _left.GetFixedLength();
                        if (left_length >= 0)
                        {
                            int right_length = _right.GetFixedLength();
                            if (right_length == left_length)
                            {
                                return right_length;
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
        internal SymbolicRegexNode<S> MkDerivative(S elem, uint context)
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(MkDerivative, elem, context);
            }

            if (this == _builder._anyStar || this == _builder._nothing)
            {
                return this;
            }

            if (Kind == SymbolicRegexKind.Or && !_isInternalizedUnion && !_builder._antimirov)
            {
                // Internalize the node before proceeding
                // this node could end up being internalized or replaced by
                // an already previously created object (!= this)
                SymbolicRegexNode<S> this_internalized = Internalize(this);
                Debug.Assert(this_internalized._isInternalizedUnion);
                if (this_internalized != this)
                {
                    return this_internalized.MkDerivative(elem, context);
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
                case SymbolicRegexKind.Singleton:
                    Debug.Assert(_set is not null);
                    deriv = _builder._solver.IsSatisfiable(_builder._solver.And(elem, _set)) ?
                        _builder._epsilon :
                        _builder._nothing;
                    break;

                case SymbolicRegexKind.Loop:
                    {
                        #region d(a, R*) = d(a,R)R*
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> step = _left.MkDerivative(elem, context);

                        if (step == _builder._nothing || _upper == 0)
                        {
                            deriv = _builder._nothing;
                            break;
                        }

                        if (IsStar)
                        {
                            deriv = _builder.MkConcat(step, this);
                            break;
                        }

                        if (IsPlus)
                        {
                            SymbolicRegexNode<S> star = _builder.MkLoop(_left, IsLazy);
                            deriv = _builder.MkConcat(step, star);
                            break;
                        }

                        int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                        int newlower = _lower == 0 ? 0 : _lower - 1;
                        SymbolicRegexNode<S> rest = _builder.MkLoop(_left, IsLazy, newlower, newupper);
                        deriv = _builder.MkConcat(step, rest);
                        break;
                        #endregion
                    }

                case SymbolicRegexKind.Concat:
                    {
                        #region d(a, AB) = d(a,A)B | (if A nullable then d(a,B))
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> leftd = _left.MkDerivative(elem, context);
                        SymbolicRegexNode<S> first = _builder._nothing;
                        if (_builder._antimirov && leftd._kind == SymbolicRegexKind.Or)
                        {
                            // push concatenations into the union
                            Debug.Assert(leftd._alts is not null);
                            foreach (SymbolicRegexNode<S> d in leftd._alts)
                            {
                                first = _builder.MkOr(first, _builder.MkConcat(d, _right));
                            }
                        }
                        else
                        {
                            first = _builder.MkConcat(leftd, _right);
                        }

                        if (_left.IsNullableFor(context))
                        {
                            SymbolicRegexNode<S> second = _right.MkDerivative(elem, context);
                            deriv = _builder.MkOr2(first, second);
                            break;
                        }

                        deriv = first;
                        break;
                        #endregion
                    }

                case SymbolicRegexKind.Or:
                    {
                        #region d(a,A|B) = d(a,A)|d(a,B)
                        Debug.Assert(_alts is not null && _alts._kind == SymbolicRegexKind.Or);
                        SymbolicRegexSet<S> alts_deriv = _alts.CreateDerivative(elem, context);
                        // At this point alts_deriv can be the empty conjunction denoting .*
                        deriv = _builder.MkOr(alts_deriv);
                        break;
                        #endregion
                    }

                case SymbolicRegexKind.OrderedOr:
                    {
                        #region d(a,A|B) = d(a,A)|d(a,B)
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> leftd = _left.MkDerivative(elem, context);
                        SymbolicRegexNode<S> rightd = _right.MkDerivative(elem, context);
                        deriv = _builder.MkOrderedOr(leftd, rightd);
                        break;
                        #endregion
                    }

                case SymbolicRegexKind.And:
                    {
                        #region d(a,A & B) = d(a,A) & d(a,B)
                        Debug.Assert(_alts is not null && _alts._kind == SymbolicRegexKind.And);
                        SymbolicRegexSet<S> alts_deriv = _alts.CreateDerivative(elem, context);
                        // At this point alts_deriv can be the empty disjunction denoting nothing
                        deriv = _builder.MkAnd(alts_deriv);
                        break;
                        #endregion
                    }

                case SymbolicRegexKind.Not:
                    {
                        #region d(a,~(A)) = ~(d(a,A))
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> leftD = _left.MkDerivative(elem, context);
                        deriv = _builder.MkNot(leftD);
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
        internal TransitionRegex<S> MkDerivative()
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
                return StackHelper.CallOnEmptyStack(MkDerivative);
            }

            switch (_kind)
            {
                case SymbolicRegexKind.Singleton:
                    Debug.Assert(_set is not null);
                    _transitionRegex = TransitionRegex<S>.Conditional(_set, TransitionRegex<S>.Leaf(_builder._epsilon), TransitionRegex<S>.Leaf(_builder._nothing));
                    break;

                case SymbolicRegexKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    TransitionRegex<S> mainTransition = _left.MkDerivative().Concat(_right);

                    if (!_left.CanBeNullable)
                    {
                        // If _left is never nullable
                        _transitionRegex = mainTransition;
                    }
                    else if (_left.IsNullable)
                    {
                        // If _left is unconditionally nullable
                        _transitionRegex = mainTransition | _right.MkDerivative();
                    }
                    else
                    {
                        // The left side contains anchors and can be nullable in some context
                        // Extract the nullability as the lookaround condition
                        SymbolicRegexNode<S> leftNullabilityTest = _left.ExtractNullabilityTest();
                        _transitionRegex = TransitionRegex<S>.Lookaround(leftNullabilityTest, mainTransition | _right.MkDerivative(), mainTransition);
                    }
                    break;

                case SymbolicRegexKind.Loop:
                    // d(R*) = d(R+) = d(R)R*
                    Debug.Assert(_left is not null);
                    Debug.Assert(_upper > 0);
                    TransitionRegex<S> step = _left.MkDerivative();

                    if (IsStar || IsPlus)
                    {
                        _transitionRegex = step.Concat(_builder.MkLoop(_left, IsLazy));
                    }
                    else
                    {
                        int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                        int newlower = _lower == 0 ? 0 : _lower - 1;
                        SymbolicRegexNode<S> rest = _builder.MkLoop(_left, IsLazy, newlower, newupper);
                        _transitionRegex = step.Concat(rest);
                    }
                    break;

                case SymbolicRegexKind.Or:
                    Debug.Assert(_alts is not null);
                    _transitionRegex = TransitionRegex<S>.Leaf(_builder._nothing);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        _transitionRegex = _transitionRegex | elem.MkDerivative();
                    }
                    break;

                case SymbolicRegexKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _transitionRegex = _left.MkDerivative() | _right.MkDerivative();
                    break;

                case SymbolicRegexKind.And:
                    Debug.Assert(_alts is not null);
                    _transitionRegex = TransitionRegex<S>.Leaf(_builder._anyStar);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        _transitionRegex = _transitionRegex & elem.MkDerivative();
                    }
                    break;

                case SymbolicRegexKind.Not:
                    Debug.Assert(_left is not null);
                    _transitionRegex = ~_left.MkDerivative();
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
        internal TransitionRegex<S> MkDerivativeWithEffects(bool eager)
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
                return StackHelper.CallOnEmptyStack(MkDerivativeWithEffects, eager);
            }

            switch (_kind)
            {
                case SymbolicRegexKind.Singleton:
                    Debug.Assert(_set is not null);
                    transition = TransitionRegex<S>.Conditional(_set, TransitionRegex<S>.Leaf(_builder._epsilon), TransitionRegex<S>.Leaf(_builder._nothing));
                    break;

                case SymbolicRegexKind.Concat:
                    {
                        // The Concat case below explicitly handles cases where the left side is an alternation or a loop.
                        // This is required for properly maintaining transition ordering and handling eagerness.

                        Debug.Assert(_left is not null && _right is not null);

                        TransitionRegex<S> leftTransition = _left.MkDerivativeWithEffects(eager).Concat(_right);
                        if (!_left.CanBeNullable)
                        {
                            // If the left side can't be nullable then the character must be consumed there
                            transition = leftTransition;
                        }
                        else if (_left._kind == SymbolicRegexKind.OrderedOr)
                        {
                            Debug.Assert(_left._left is not null && _left._right is not null);

                            // This pattern is for the path where the backtracking matcher would find the match from the first alternative
                            SymbolicRegexNode<S> leftLeftPath = _builder.MkConcat(_left._left, _right);
                            // The eager derivative of that path will be used when the pattern is nullable, i.e., the backtracking matcher
                            // would end the match rather than go onto paths in the second alternative.
                            TransitionRegex<S> leftLeftTransition = leftLeftPath.MkDerivativeWithEffects(eager);
                            // When the path through the first alternative is not nullable, this transition that includes all derivatives from
                            // it is used.
                            TransitionRegex<S> orTransition = TransitionRegex<S>.Union(leftLeftPath.MkDerivativeWithEffects(false),
                                _builder.MkConcat(_left._right, _right).MkDerivativeWithEffects(eager), ordered: true);

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
                            TransitionRegex<S> leftAnyTransition = _left.MkDerivativeWithEffects(false).Concat(_right);
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
                            TransitionRegex<S> nullTransition = _left.WrapEffects(_right.MkDerivativeWithEffects(eager));
                            // Order the transitions. If the left side is a lazy loop that is nullable due to its lower bound then prefer the right side
                            TransitionRegex<S> orTransition = _left._kind == SymbolicRegexKind.Loop && _left.IsLazy && _left._lower == 0 ?
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

                case SymbolicRegexKind.Loop:
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
                        TransitionRegex<S> step = _left.MkDerivativeWithEffects(eager);
                        if (IsStar || IsPlus)
                        {
                            transition = step.Concat(_builder.MkLoop(_left, IsLazy));
                        }
                        else
                        {
                            int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                            int newlower = _lower == 0 ? 0 : _lower - 1;
                            SymbolicRegexNode<S> rest = _builder.MkLoop(_left, IsLazy, newlower, newupper);
                            transition = step.Concat(rest);
                        }
                    }
                    break;

                case SymbolicRegexKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        // The transition when the first alternative is nullable
                        TransitionRegex<S> leftTransition = _left.MkDerivativeWithEffects(eager);
                        // The transition when the backtracking engines could continue to the second alternative
                        TransitionRegex<S> orTransition = TransitionRegex<S>.Union(_left.MkDerivativeWithEffects(false),
                            _right.MkDerivativeWithEffects(eager), ordered: true);
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

                case SymbolicRegexKind.Or:
                    Debug.Assert(_alts is not null);
                    transition = TransitionRegex<S>.Leaf(_builder._nothing);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        transition = transition | elem.MkDerivativeWithEffects(eager);
                    }
                    break;

                case SymbolicRegexKind.And:
                    Debug.Assert(_alts is not null);
                    transition = TransitionRegex<S>.Leaf(_builder._anyStar);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        transition = transition & elem.MkDerivativeWithEffects(eager);
                    }
                    break;

                case SymbolicRegexKind.Not:
                    Debug.Assert(_left is not null);
                    transition = ~_left.MkDerivativeWithEffects(eager);
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
                case SymbolicRegexKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    Debug.Assert(_left.CanBeNullable && _right.CanBeNullable);
                    transition = _left.WrapEffects(transition);
                    transition = _right.WrapEffects(transition);
                    break;

                case SymbolicRegexKind.Loop:
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

                case SymbolicRegexKind.OrderedOr:
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

                case SymbolicRegexKind.CaptureStart:
                    // Add the effect to record the capture start
                    transition = TransitionRegex<S>.Effect(transition,
                        new DerivativeEffect(DerivativeEffect.EffectKind.CaptureStart, _lower));
                    break;

                case SymbolicRegexKind.CaptureEnd:
                    // Add the effect to record the capture start
                    transition = TransitionRegex<S>.Effect(transition,
                        new DerivativeEffect(DerivativeEffect.EffectKind.CaptureEnd, _lower));
                    break;

                case SymbolicRegexKind.Or:
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

                case SymbolicRegexKind.And:
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
                case SymbolicRegexKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    Debug.Assert(_left.IsNullableFor(context) && _right.IsNullableFor(context));
                    _left.ApplyEffects(apply, context);
                    _right.ApplyEffects(apply, context);
                    break;

                case SymbolicRegexKind.Loop:
                    Debug.Assert(_left is not null);
                    // Apply effect when backtracking engine would enter loop
                    if (_lower != 0 || (_upper != 0 && !IsLazy && _left.IsNullableFor(context)))
                    {
                        Debug.Assert(_left.IsNullableFor(context));
                        _left.ApplyEffects(apply, context);
                    }
                    break;

                case SymbolicRegexKind.OrderedOr:
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

                case SymbolicRegexKind.CaptureStart:
                    apply(new DerivativeEffect(DerivativeEffect.EffectKind.CaptureStart, _lower));
                    break;

                case SymbolicRegexKind.CaptureEnd:
                    apply(new DerivativeEffect(DerivativeEffect.EffectKind.CaptureEnd, _lower));
                    break;

                case SymbolicRegexKind.Or:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        if (elem.IsNullableFor(context))
                            elem.ApplyEffects(apply, context);
                    }
                    break;

                case SymbolicRegexKind.And:
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
        /// Computes the closure of MkDerivative, by exploring all the leaves
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
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.BOLAnchor:
                case SymbolicRegexKind.EOLAnchor:
                case SymbolicRegexKind.WBAnchor:
                case SymbolicRegexKind.NWBAnchor:
                case SymbolicRegexKind.EndAnchorZ:
                case SymbolicRegexKind.EndAnchorZRev:
                    return this;
                case SymbolicRegexKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    return _builder.MkAnd(_left.ExtractNullabilityTest(), _right.ExtractNullabilityTest());
                case SymbolicRegexKind.Or:
                    Debug.Assert(_alts is not null);
                    SymbolicRegexNode<S> disjunction = _builder._nothing;
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        disjunction = _builder.MkOr(disjunction, elem.ExtractNullabilityTest());
                    }
                    return disjunction;
                case SymbolicRegexKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    return _builder.MkOrderedOr(_left.ExtractNullabilityTest(), _right.ExtractNullabilityTest());
                case SymbolicRegexKind.And:
                    Debug.Assert(_alts is not null);
                    SymbolicRegexNode<S> conjunction = _builder._anyStar;
                    foreach (SymbolicRegexNode<S> elem in _alts)
                    {
                        conjunction = _builder.MkAnd(conjunction, elem.ExtractNullabilityTest());
                    }
                    return conjunction;
                case SymbolicRegexKind.Loop:
                    Debug.Assert(_left is not null);
                    return _left.ExtractNullabilityTest();
                default:
                    // All remaining cases could not be nullable or were trivially nullable
                    // Sigleton cannot be nullable and Epsilon and Watchdog are trivially nullable
                    Debug.Assert(_kind == SymbolicRegexKind.Not && _left is not null);
                    return _builder.MkNot(_left.ExtractNullabilityTest());
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
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.BOLAnchor:
                case SymbolicRegexKind.EOLAnchor:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.WBAnchor:
                case SymbolicRegexKind.NWBAnchor:
                case SymbolicRegexKind.EndAnchorZ:
                case SymbolicRegexKind.EndAnchorZRev:
                    return HashCode.Combine(_kind, _info);

                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.CaptureStart:
                case SymbolicRegexKind.CaptureEnd:
                    return HashCode.Combine(_kind, _lower);

                case SymbolicRegexKind.Loop:
                    return HashCode.Combine(_kind, _left, _lower, _upper, _info);

                case SymbolicRegexKind.Or or SymbolicRegexKind.And:
                    return HashCode.Combine(_kind, _alts, _info);

                case SymbolicRegexKind.Concat:
                case SymbolicRegexKind.OrderedOr:
                    return HashCode.Combine(_left, _right, _info);

                case SymbolicRegexKind.Singleton:
                    return HashCode.Combine(_kind, _set);

                default:
                    Debug.Assert(_kind == SymbolicRegexKind.Not);
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

            if (_kind == SymbolicRegexKind.Or)
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
            if (_kind == SymbolicRegexKind.Singleton)
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
                case SymbolicRegexKind.EndAnchor:
                    sb.Append("\\z");
                    return;

                case SymbolicRegexKind.StartAnchor:
                    sb.Append("\\A");
                    return;

                case SymbolicRegexKind.BOLAnchor:
                    sb.Append('^');
                    return;

                case SymbolicRegexKind.EOLAnchor:
                    sb.Append('$');
                    return;

                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.WatchDog:
                    return;

                case SymbolicRegexKind.WBAnchor:
                    sb.Append("\\b");
                    return;

                case SymbolicRegexKind.NWBAnchor:
                    sb.Append("\\B");
                    return;

                case SymbolicRegexKind.EndAnchorZ:
                    sb.Append("\\Z");
                    return;

                case SymbolicRegexKind.EndAnchorZRev:
                    sb.Append("\\a");
                    return;

                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    Debug.Assert(_alts is not null);
                    _alts.ToString(sb);
                    return;

                case SymbolicRegexKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.ToString(sb);
                    sb.Append('|');
                    _right.ToString(sb);
                    return;

                case SymbolicRegexKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.ToString(sb);
                    _right.ToString(sb);
                    return;

                case SymbolicRegexKind.Singleton:
                    Debug.Assert(_set is not null);
                    sb.Append(_builder._solver.PrettyPrint(_set));
                    return;

                case SymbolicRegexKind.Loop:
                    Debug.Assert(_left is not null);
                    if (IsAnyStar)
                    {
                        sb.Append(".*");
                    }
                    else if (IsMaybe)
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
                    else if (!IsBoundedLoop)
                    {
                        _left.ToStringForLoop(sb);
                        sb.Append('{');
                        sb.Append(_lower);
                        sb.Append(",}");
                        if (IsLazy)
                            sb.Append('?');
                    }
                    else if (_lower == _upper)
                    {
                        _left.ToStringForLoop(sb);
                        sb.Append('{');
                        sb.Append(_lower);
                        sb.Append('}');
                        if (IsLazy)
                            sb.Append('?');
                    }
                    else
                    {
                        _left.ToStringForLoop(sb);
                        sb.Append('{');
                        sb.Append(_lower);
                        sb.Append(',');
                        sb.Append(_upper);
                        sb.Append('}');
                        if (IsLazy)
                            sb.Append('?');
                    }
                    return;

                case SymbolicRegexKind.CaptureStart:
                    sb.Append('('); // The group number may be wrong
                    return;

                case SymbolicRegexKind.CaptureEnd:
                    sb.Append(')');
                    return;

                default:
                    // Using the operator ~ for complement
                    Debug.Assert(_kind == SymbolicRegexKind.Not);
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
                case SymbolicRegexKind.BOLAnchor:
                case SymbolicRegexKind.EOLAnchor:
                case SymbolicRegexKind.EndAnchorZ:
                case SymbolicRegexKind.EndAnchorZRev:
                    predicates.Add(_builder._newLinePredicate);
                    return;

                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.CaptureStart:
                case SymbolicRegexKind.CaptureEnd:
                    return;

                case SymbolicRegexKind.Singleton:
                    Debug.Assert(_set is not null);
                    predicates.Add(_set);
                    return;

                case SymbolicRegexKind.Loop:
                    Debug.Assert(_left is not null);
                    _left.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<S> sr in _alts)
                    {
                        sr.CollectPredicates_helper(predicates);
                    }
                    return;

                case SymbolicRegexKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.CollectPredicates_helper(predicates);
                    _right.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexKind.Concat:
                    // avoid deep nested recursion over long concat nodes
                    SymbolicRegexNode<S> conc = this;
                    while (conc._kind == SymbolicRegexKind.Concat)
                    {
                        Debug.Assert(conc._left is not null && conc._right is not null);
                        conc._left.CollectPredicates_helper(predicates);
                        conc = conc._right;
                    }
                    conc.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexKind.Not:
                    Debug.Assert(_left is not null);
                    _left.CollectPredicates_helper(predicates);
                    return;

                case SymbolicRegexKind.NWBAnchor:
                case SymbolicRegexKind.WBAnchor:
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
            Debug.Assert(typeof(S).IsAssignableTo(typeof(IComparable)));

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
                case SymbolicRegexKind.Loop:
                    Debug.Assert(_left is not null);
                    return _builder.MkLoop(_left.Reverse(), IsLazy, _lower, _upper);

                case SymbolicRegexKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> rev = _left.Reverse();
                        SymbolicRegexNode<S> rest = _right;
                        while (rest._kind == SymbolicRegexKind.Concat)
                        {
                            Debug.Assert(rest._left is not null && rest._right is not null);
                            SymbolicRegexNode<S> rev1 = rest._left.Reverse();
                            rev = _builder.MkConcat(rev1, rev);
                            rest = rest._right;
                        }
                        SymbolicRegexNode<S> restr = rest.Reverse();
                        rev = _builder.MkConcat(restr, rev);
                        return rev;
                    }

                case SymbolicRegexKind.Or:
                    Debug.Assert(_alts is not null);
                    return _builder.MkOr(_alts.Reverse());

                case SymbolicRegexKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    return _builder.MkOrderedOr(_left.Reverse(), _right.Reverse());

                case SymbolicRegexKind.And:
                    Debug.Assert(_alts is not null);
                    return _builder.MkAnd(_alts.Reverse());

                case SymbolicRegexKind.Not:
                    Debug.Assert(_left is not null);
                    return _builder.MkNot(_left.Reverse());

                case SymbolicRegexKind.WatchDog:
                    // Watchdogs are omitted in reverse
                    return _builder._epsilon;

                case SymbolicRegexKind.StartAnchor:
                    // The reverse of StartAnchor is EndAnchor
                    return _builder._endAnchor;

                case SymbolicRegexKind.EndAnchor:
                    return _builder._startAnchor;

                case SymbolicRegexKind.BOLAnchor:
                    // The reverse of BOLanchor is EOLanchor
                    return _builder._eolAnchor;

                case SymbolicRegexKind.EOLAnchor:
                    return _builder._bolAnchor;

                case SymbolicRegexKind.EndAnchorZ:
                    // The reversal of the \Z anchor
                    return _builder._endAnchorZRev;

                case SymbolicRegexKind.EndAnchorZRev:
                    // This can potentially only happen if a reversed regex is reversed again.
                    // Thus, this case is unreachable here, but included for completeness.
                    return _builder._endAnchorZ;

                case SymbolicRegexKind.CaptureStart:
                    return MkCaptureEnd(_builder, _lower);

                case SymbolicRegexKind.CaptureEnd:
                    return MkCaptureStart(_builder, _lower);

                default:
                    // Remaining cases map to themselves:
                    // SymbolicRegexKind.Epsilon
                    // SymbolicRegexKind.Singleton
                    // SymbolicRegexKind.WBAnchor
                    // SymbolicRegexKind.NWBAnchor
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
                case SymbolicRegexKind.Loop:
                    {
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> body = _left.IgnoreOrOrderAndLazyness();
                        return body == _left && !IsLazy?
                            this :
                            MkLoop(_builder, body, _lower, _upper, isLazy: false);
                    }

                case SymbolicRegexKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> left1 = _left.IgnoreOrOrderAndLazyness();
                        SymbolicRegexNode<S> right1 = _right.IgnoreOrOrderAndLazyness();

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            MkConcat(_builder, left1, right1);
                    }

                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
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
                            _kind == SymbolicRegexKind.Or ? MkOr(_builder, elements) :
                            MkAnd(_builder, elements);
                    }

                case SymbolicRegexKind.Not:
                    {
                        Debug.Assert(_left is not null);
                        SymbolicRegexNode<S> body = _left.IgnoreOrOrderAndLazyness();
                        return body == _left ?
                            this :
                            MkNot(_builder, body);
                    }

                case SymbolicRegexKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        return MkOr(_builder, _left.IgnoreOrOrderAndLazyness(), _right.IgnoreOrOrderAndLazyness());
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
                case SymbolicRegexKind.Loop:
                    return (_upper < int.MaxValue) && (_upper > upperBoundLowestValue);

                case SymbolicRegexKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    return _left.StartsWithLoop(upperBoundLowestValue) || (_left.IsNullable && _right.StartsWithLoop(upperBoundLowestValue));

                case SymbolicRegexKind.Or:
                    Debug.Assert(_alts is not null);
                    return _alts.StartsWithLoop(upperBoundLowestValue);

                case SymbolicRegexKind.OrderedOr:
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
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.WBAnchor:
                case SymbolicRegexKind.NWBAnchor:
                case SymbolicRegexKind.EOLAnchor:
                case SymbolicRegexKind.EndAnchorZ:
                case SymbolicRegexKind.EndAnchorZRev:
                case SymbolicRegexKind.BOLAnchor:
                case SymbolicRegexKind.CaptureStart:
                case SymbolicRegexKind.CaptureEnd:
                    return _builder._solver.False;

                case SymbolicRegexKind.Singleton:
                    Debug.Assert(_set is not null);
                    return _set;

                case SymbolicRegexKind.Loop:
                    Debug.Assert(_left is not null);
                    return _left._startSet;

                case SymbolicRegexKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        S startSet = _left.CanBeNullable ? _builder._solver.Or(_left._startSet, _right._startSet) : _left._startSet;
                        return startSet;
                    }

                case SymbolicRegexKind.Or:
                    {
                        Debug.Assert(_alts is not null);
                        S startSet = _builder._solver.False;
                        foreach (SymbolicRegexNode<S> alt in _alts)
                        {
                            startSet = _builder._solver.Or(startSet, alt._startSet);
                        }
                        return startSet;
                    }

                case SymbolicRegexKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        return _builder._solver.Or(_left._startSet, _right._startSet);
                    }

                case SymbolicRegexKind.And:
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
                    Debug.Assert(_kind == SymbolicRegexKind.Not);
                    return _builder._solver.True;
            }
        }

        /// <summary>
        /// Returns true if this is a loop with an upper bound
        /// </summary>
        public bool IsBoundedLoop => _kind == SymbolicRegexKind.Loop && _upper < int.MaxValue;

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
                case SymbolicRegexKind.StartAnchor:
                    return prevKind == CharKind.StartStop ?
                        this :
                        _builder._nothing; //start anchor is only nullable if the previous character is Start

                case SymbolicRegexKind.EndAnchorZRev:
                    return ((prevKind & CharKind.StartStop) != 0) ?
                        this :
                        _builder._nothing; //rev(\Z) is only nullable if the previous characters is Start or the very first \n

                case SymbolicRegexKind.WBAnchor:
                    return (prevKind == CharKind.WordLetter ? contWithNWL : contWithWL) ?
                        this :
                        // \b is impossible when the previous character is \w but no continuation matches \W
                        // or the previous character is \W but no continuation matches \w
                        _builder._nothing;

                case SymbolicRegexKind.NWBAnchor:
                    return (prevKind == CharKind.WordLetter ? contWithWL : contWithNWL) ?
                        this :
                        // \B is impossible when the previous character is \w but no continuation matches \w
                        // or the previous character is \W but no continuation matches \W
                        _builder._nothing;

                case SymbolicRegexKind.Loop:
                    Debug.Assert(_left is not null);
                    SymbolicRegexNode<S> body = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                    return body == _left ?
                        this :
                        MkLoop(_builder, body, _lower, _upper, IsLazy);

                case SymbolicRegexKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> left1 = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<S> right1 = _left.IsNullable ? _right.PruneAnchors(prevKind, contWithWL, contWithNWL) : _right;

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            MkConcat(_builder, left1, right1);
                    }

                case SymbolicRegexKind.Or:
                    {
                        Debug.Assert(_alts != null);
                        var elements = new SymbolicRegexNode<S>[_alts.Count];
                        int i = 0;
                        foreach (SymbolicRegexNode<S> alt in _alts)
                        {
                            elements[i++] = alt.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        }
                        Debug.Assert(i == elements.Length);
                        return MkOr(_builder, elements);
                    }

                case SymbolicRegexKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<S> left1 = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<S> right1 = _right.PruneAnchors(prevKind, contWithWL, contWithNWL);

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            MkOrderedOr(_builder, left1, right1);
                    }

                default:
                    return this;
            }
        }
    }
}
