// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents an abstract syntax tree node of a symbolic regex.</summary>
    internal sealed class SymbolicRegexNode<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        internal const string EmptyCharClass = "[]";
        /// <summary>Some byte other than 0 to represent true</summary>
        internal const byte TrueByte = 1;
        /// <summary>Some byte other than 0 to represent false</summary>
        internal const byte FalseByte = 2;
        /// <summary>The undefined value is the default value 0</summary>
        internal const byte UndefinedByte = 0;

        internal readonly SymbolicRegexBuilder<TSet> _builder;
        internal readonly SymbolicRegexNodeKind _kind;
        internal readonly int _lower;
        internal readonly int _upper;
        internal readonly TSet? _set;
        internal readonly SymbolicRegexNode<TSet>? _left;
        internal readonly SymbolicRegexNode<TSet>? _right;
        //depricated
        internal readonly SymbolicRegexSet<TSet>? _alts;

        /// <summary>
        /// Caches nullability of this node for any given context (0 &lt;= context &lt; ContextLimit)
        /// when _info.StartsWithSomeAnchor and _info.CanBeNullable are true. Otherwise the cache is null.
        /// </summary>
        private byte[]? _nullabilityCache;

        private TSet _startSet;

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
        private SymbolicRegexNode(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet>? left, SymbolicRegexNode<TSet>? right, int lower, int upper, TSet? set, SymbolicRegexSet<TSet>? alts, SymbolicRegexInfo info)
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

        //depricated
        private bool _isInternalizedUnion;

        /// <summary> Create a new node or retrieve one from the builder _nodeCache</summary>
        private static SymbolicRegexNode<TSet> Create(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet>? left, SymbolicRegexNode<TSet>? right, int lower, int upper, TSet? set, SymbolicRegexSet<TSet>? alts, SymbolicRegexInfo info)
        {
            SymbolicRegexNode<TSet>? node;
            var key = (kind, left, right, lower, upper, set, alts, info);
            if (!builder._nodeCache.TryGetValue(key, out node))
            {
                // Do not internalize top level Or-nodes or else NFA mode will become ineffective
                if (kind == SymbolicRegexNodeKind.Or)
                {
                    node = new SymbolicRegexNode<TSet>(builder, kind, left, right, lower, upper, set, alts, info);
                    return node;
                }

                left = left == null || left._kind != SymbolicRegexNodeKind.Or || left._isInternalizedUnion ? left : Internalize(left);
                right = right == null || right._kind != SymbolicRegexNodeKind.Or || right._isInternalizedUnion ? right : Internalize(right);

                node = new SymbolicRegexNode<TSet>(builder, kind, left, right, lower, upper, set, alts, info);
                builder._nodeCache[key] = node;
            }

            Debug.Assert(node is not null);
            return node;
        }

        /// <summary> Internalize an Or-node that is not yet internalized</summary>
        private static SymbolicRegexNode<TSet> Internalize(SymbolicRegexNode<TSet> node)
        {
            Debug.Assert(node._kind == SymbolicRegexNodeKind.Or && !node._isInternalizedUnion);

            (SymbolicRegexNodeKind, SymbolicRegexNode<TSet>?, SymbolicRegexNode<TSet>?, int, int, TSet?, SymbolicRegexSet<TSet>?, SymbolicRegexInfo) node_key =
                (SymbolicRegexNodeKind.Or, null, null, -1, -1, default(TSet), node._alts, node._info);
            SymbolicRegexNode<TSet>? node1;
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

        /// <summary>True if this node is lazy</summary>
        internal bool IsLazy => _info.IsLazyLoop;

        /// <summary>True if this node is high priority nullable</summary>
        internal bool IsHighPriorityNullable => _info.IsHighPriorityNullable;

        /// <summary>True if this node is high priority nullable for the given context</summary>
        internal bool IsHighPriorityNullableFor(uint context) => _info.CanBeNullable && IsHighPriorityNullableForLeftmostBranch(this, context);

        /// <summary>Lightweigth nullability test that determines if the leftmost branch of the node is high-priority-nullable for the given context</summary>
        private static bool IsHighPriorityNullableForLeftmostBranch(SymbolicRegexNode<TSet> node, uint context)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(IsHighPriorityNullableForLeftmostBranch, node, context);
            }

            //deep recursion can only occur for deep left-associative concatenations that is uncommon
            //in all common cases deep recursion is avoided by using a while loop
            while (true)
            {
                Debug.Assert(node.CanBeNullable);

                if (node._info.IsHighPriorityNullable)
                {
                    return true;
                }

                switch (node._kind)
                {
                    case SymbolicRegexNodeKind.Loop:
                        //only a lazy loop with lower bound 0 is high-priority-nullable
                        return node._info.IsLazyLoop && node._lower == 0;

                    case SymbolicRegexNodeKind.Concat:
                        Debug.Assert(node._left is not null && node._right is not null);
                        //both left and right must be high-priority-nullable
                        //observe that node.CanBeNullable implies that both node._left and node._right can be nullable
                        if (!IsHighPriorityNullableForLeftmostBranch(node._left, context))
                        {
                            return false;
                        }
                        node = node._right;
                        continue;

                    case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    case SymbolicRegexNodeKind.Effect:
                    case SymbolicRegexNodeKind.OrderedOr:
                        Debug.Assert(node._left is not null);
                        //the left alternative must be high-priority-nullable
                        //nullability of the right alternative does not matter
                        if (!node._left._info.CanBeNullable)
                        {
                            return false;
                        }
                        node = node._left;
                        continue;

                    default:
                        // any remaining case must be an anchor or else next._info.IsHighPriorityNullable would have been true
                        Debug.Assert(node._kind is SymbolicRegexNodeKind.BeginningAnchor or
                            SymbolicRegexNodeKind.EndAnchor or
                            SymbolicRegexNodeKind.BOLAnchor or
                            SymbolicRegexNodeKind.EOLAnchor or
                            SymbolicRegexNodeKind.BoundaryAnchor or
                            SymbolicRegexNodeKind.NonBoundaryAnchor or
                            SymbolicRegexNodeKind.EndAnchorZ or
                            SymbolicRegexNodeKind.EndAnchorZReverse);
                        return node.IsNullableFor(context);
                }
            }
        }

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
        public List<SymbolicRegexNode<TSet>> ToList(List<SymbolicRegexNode<TSet>>? list = null, SymbolicRegexNodeKind listKind = SymbolicRegexNodeKind.Concat)
        {
            Debug.Assert(listKind == SymbolicRegexNodeKind.Concat || listKind == SymbolicRegexNodeKind.OrderedOr);
            list ??= new List<SymbolicRegexNode<TSet>>();
            AppendToList(this, list, listKind);
            return list;

            static void AppendToList(SymbolicRegexNode<TSet> concat, List<SymbolicRegexNode<TSet>> list, SymbolicRegexNodeKind listKind)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(AppendToList, concat, list, listKind);
                    return;
                }

                SymbolicRegexNode<TSet> node = concat;
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

                    case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    case SymbolicRegexNodeKind.Effect:
                        Debug.Assert(_left is not null);
                        is_nullable = _left.IsNullableFor(context);
                        break;

                    default:
                        // SymbolicRegexNodeKind.EndAnchorZReverse:
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
                        return !IsLazy && _builder._solver.Full.Equals(_left._set);
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
                        return !IsLazy && _builder._solver.Full.Equals(_left._set);
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
                    return _builder._solver.IsFull(_set);
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
                    return _builder._solver.IsEmpty(_set);
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

        internal static SymbolicRegexNode<TSet> CreateFalse(SymbolicRegexBuilder<TSet> builder) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, builder._solver.Empty, null, SymbolicRegexInfo.Create());

        internal static SymbolicRegexNode<TSet> CreateTrue(SymbolicRegexBuilder<TSet> builder) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, builder._solver.Full, null, SymbolicRegexInfo.Create());

        internal static SymbolicRegexNode<TSet> CreateFixedLengthMarker(SymbolicRegexBuilder<TSet> builder, int length) =>
            Create(builder, SymbolicRegexNodeKind.FixedLengthMarker, null, null, length, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true, isHighPriorityNullable: true));

        internal static SymbolicRegexNode<TSet> CreateEpsilon(SymbolicRegexBuilder<TSet> builder) =>
            Create(builder, SymbolicRegexNodeKind.Epsilon, null, null, -1, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true, isHighPriorityNullable: true));

        internal static SymbolicRegexNode<TSet> CreateBeginEndAnchor(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind)
        {
            Debug.Assert(kind is
                SymbolicRegexNodeKind.BeginningAnchor or SymbolicRegexNodeKind.EndAnchor or
                SymbolicRegexNodeKind.EndAnchorZ or SymbolicRegexNodeKind.EndAnchorZReverse or
                SymbolicRegexNodeKind.EOLAnchor or SymbolicRegexNodeKind.BOLAnchor);
            return Create(builder, kind, null, null, -1, -1, default, null, SymbolicRegexInfo.Create(startsWithSomeAnchor: true, canBeNullable: true,
                startsWithLineAnchor: kind is
                    SymbolicRegexNodeKind.EndAnchorZ or SymbolicRegexNodeKind.EndAnchorZReverse or
                    SymbolicRegexNodeKind.EOLAnchor or SymbolicRegexNodeKind.BOLAnchor));
        }

        internal static SymbolicRegexNode<TSet> CreateBoundaryAnchor(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind)
        {
            Debug.Assert(kind is SymbolicRegexNodeKind.BoundaryAnchor or SymbolicRegexNodeKind.NonBoundaryAnchor);
            return Create(builder, kind, null, null, -1, -1, default, null, SymbolicRegexInfo.Create(startsWithSomeAnchor: true, canBeNullable: true));
        }

        #endregion

        internal static SymbolicRegexNode<TSet> CreateSingleton(SymbolicRegexBuilder<TSet> builder, TSet set) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, set, null, SymbolicRegexInfo.Create());

        internal static SymbolicRegexNode<TSet> CreateLoop(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> body, int lower, int upper, bool isLazy)
        {
            Debug.Assert(lower >= 0 && lower <= upper);
            return Create(builder, SymbolicRegexNodeKind.Loop, body, null, lower, upper, default, null, SymbolicRegexInfo.Loop(body._info, lower, isLazy));
        }

        /// <summary>
        /// depricated
        /// </summary>
        internal static SymbolicRegexNode<TSet> Or(SymbolicRegexBuilder<TSet> builder, params SymbolicRegexNode<TSet>[] disjuncts) =>
            CreateCollection(builder, SymbolicRegexNodeKind.Or, SymbolicRegexSet<TSet>.CreateMulti(builder, disjuncts, SymbolicRegexNodeKind.Or), SymbolicRegexInfo.Or(GetInfos(disjuncts)));

        /// <summary>
        /// depricated
        /// </summary>
        internal static SymbolicRegexNode<TSet> Or(SymbolicRegexBuilder<TSet> builder, SymbolicRegexSet<TSet> disjuncts)
        {
            Debug.Assert(disjuncts._kind == SymbolicRegexNodeKind.Or || disjuncts.IsEverything);
            return CreateCollection(builder, SymbolicRegexNodeKind.Or, disjuncts, SymbolicRegexInfo.Or(GetInfos(disjuncts)));
        }

        /// <summary>
        /// depricated
        /// </summary>
        internal static SymbolicRegexNode<TSet> And(SymbolicRegexBuilder<TSet> builder, params SymbolicRegexNode<TSet>[] conjuncts) =>
            CreateCollection(builder, SymbolicRegexNodeKind.And, SymbolicRegexSet<TSet>.CreateMulti(builder, conjuncts, SymbolicRegexNodeKind.And), SymbolicRegexInfo.And(GetInfos(conjuncts)));

        /// <summary>
        /// depricated
        /// </summary>
        internal static SymbolicRegexNode<TSet> And(SymbolicRegexBuilder<TSet> builder, SymbolicRegexSet<TSet> conjuncts)
        {
            Debug.Assert(conjuncts.IsNothing || conjuncts._kind == SymbolicRegexNodeKind.And);
            return CreateCollection(builder, SymbolicRegexNodeKind.And, conjuncts, SymbolicRegexInfo.And(GetInfos(conjuncts)));
        }

        internal static SymbolicRegexNode<TSet> CreateEffect(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> node, SymbolicRegexNode<TSet> effectNode)
        {
            if (effectNode == builder.Epsilon)
                return node;
            if (node == builder._nothing)
                return builder._nothing;
            if (node._kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(node._left is not null && node._right is not null);
                return CreateEffect(builder, node._left, CreateConcat(builder, effectNode, node._right));
            }
            return Create(builder, SymbolicRegexNodeKind.Effect, node, effectNode, -1, -1, default, null, SymbolicRegexInfo.Effect(node._info));
        }

        internal static SymbolicRegexNode<TSet> CreateCaptureStart(SymbolicRegexBuilder<TSet> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureStart, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true, isHighPriorityNullable: true));

        internal static SymbolicRegexNode<TSet> CreateCaptureEnd(SymbolicRegexBuilder<TSet> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureEnd, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true, isHighPriorityNullable: true));

        internal static SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> child) =>
            Create(builder, SymbolicRegexNodeKind.DisableBacktrackingSimulation, child, null, -1, -1, default, null, child._info);

        /// <summary>
        /// depricated
        /// </summary>
        private static SymbolicRegexNode<TSet> CreateCollection(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexSet<TSet> alts, SymbolicRegexInfo info) =>
            alts.IsNothing ? builder._nothing :
            alts.IsEverything ? builder._anyStar :
            alts.IsSingleton ? alts.GetSingletonElement() :
            Create(builder, kind, null, null, -1, -1, default, alts, info);

        private static SymbolicRegexInfo[] GetInfos(SymbolicRegexNode<TSet>[] nodes)
        {
            var infos = new SymbolicRegexInfo[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                infos[i] = nodes[i]._info;
            }
            return infos;
        }

        /// <summary>
        /// depricated
        /// </summary>
        private static SymbolicRegexInfo[] GetInfos(SymbolicRegexSet<TSet> nodes)
        {
            var infos = new SymbolicRegexInfo[nodes.Count];
            int i = 0;
            foreach (SymbolicRegexNode<TSet> node in nodes)
            {
                Debug.Assert(i < nodes.Count);
                infos[i++] = node._info;
            }
            Debug.Assert(i == nodes.Count);
            return infos;
        }

        /// <summary>Make a concatenation of the supplied regex nodes.</summary>
        internal static SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, bool keepLeftConcat = true)
        {
            // Concatenating anything with a nothing means the entire concatenation can't match
            if (left == builder._nothing || right == builder._nothing)
                return builder._nothing;

            // If the left or right is empty, just return the other.
            if (left.IsEpsilon)
                return right;
            if (right.IsEpsilon)
                return left;

            // push concatenation inside Effect nodes
            Debug.Assert(right._kind is not SymbolicRegexNodeKind.Effect);
            if (left._kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(left._left is not null && left._right is not null);
                return CreateEffect(builder, CreateConcat(builder, left._left, right), left._right);
            }

            SymbolicRegexNode<TSet> rl = right._kind == SymbolicRegexNodeKind.Concat ? right._left! : right;
            SymbolicRegexNode<TSet> rr = right._kind == SymbolicRegexNodeKind.Concat ? right._right! : builder.Epsilon;

            // join concatenation of two loops with the same laziness status and bodies into a single loop
            if (left._kind == SymbolicRegexNodeKind.Loop && rl._kind == SymbolicRegexNodeKind.Loop && left._left == rl._left &&
               left._info.IsLazyLoop == rl._info.IsLazyLoop)
            {
                Debug.Assert(left._left is not null);
                //either both loops are eager or both are lazy
                //compute the sum of the bounds as the new bounds of the joined loop
                int lower = left._lower + right._lower;
                long upperInt64 = left._upper + right._upper;  //avoid overflow in case some loop has no upper bound
                int upper = (upperInt64 >= int.MaxValue) ? int.MaxValue : (int)upperInt64;
                SymbolicRegexNode<TSet> loop = CreateLoop(builder, left._left, lower, upper, left._info.IsLazyLoop);
                return MkConcat(loop, rr);
            }

            //try to create loop structure: XX --> X{2}
            if (left == rl)
            {
                int lower = left._info.IsHighPriorityNullable ? 0 : 2;
                SymbolicRegexNode<TSet> loop = CreateLoop(builder, left, lower, 2, left._info.IsHighPriorityNullable);
                return MkConcat(loop, rr);
            }

            //increment existing loop X{m,n}X --> X{m+1,n+1}
            if (left._kind == SymbolicRegexNodeKind.Loop && left._left == rl)
            {
                int lower = rl._info.IsHighPriorityNullable ? 0 : left._lower + 1;
                SymbolicRegexNode<TSet> loop = CreateLoop(builder, rl, lower, left._upper + 1, rl._info.IsHighPriorityNullable);
                return MkConcat(loop, rr);
            }
            //increment existing loop XX{m,n} --> X{m+1,n+1}
            if (rl._kind == SymbolicRegexNodeKind.Loop && rl._left == left)
            {
                //if the body of the loop is high-priority-nullable, e.g. a?
                //make the lower bound 0 irrespective of the laziness of rl
                int lower = left._info.IsHighPriorityNullable ? 0 : rl._lower + 1;
                SymbolicRegexNode<TSet> loop = CreateLoop(builder, left, lower, rl._upper + 1, rl._info.IsLazyLoop);
                return MkConcat(loop, rr);
            }

            // If left isn't a concatenation, then concatenate left with right.
            if (left._kind != SymbolicRegexNodeKind.Concat || keepLeftConcat)
            {
                return Create(builder, SymbolicRegexNodeKind.Concat, left, right, -1, -1, default, null, SymbolicRegexInfo.Concat(left._info, right._info));
            }

            // The left is a concatenation.  We want to flatten it out and maintain a right-associative form.
            SymbolicRegexNode<TSet> concat = right;
            List<SymbolicRegexNode<TSet>> leftNodes = left.ToList();
            for (int i = leftNodes.Count - 1; i >= 0; i--)
            {
                concat = Create(builder, SymbolicRegexNodeKind.Concat, leftNodes[i], concat, -1, -1, default, null, SymbolicRegexInfo.Concat(leftNodes[i]._info, concat._info));
            }
            return concat;

            //private helper that handles the case when right is epsilon else makes a concatenation
            SymbolicRegexNode<TSet> MkConcat(SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right) =>
                right.IsEpsilon ? left : Create(builder, SymbolicRegexNodeKind.Concat, left, right, -1, -1, default, null, SymbolicRegexInfo.Concat(left._info, right._info));
        }

        private bool IsWithEffects(SymbolicRegexNodeKind kind, [NotNullWhen(true)] out SymbolicRegexNode<TSet>? match)
        {
            if (_kind == kind)
            {
                match = this;
                return true;
            }
            else if (_kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(_left is not null);
                return _left.IsWithEffects(kind, out match);
            }
            match = null;
            return false;
        }

        /// <summary>
        /// Make an ordered or of given regexes, eliminate nothing regexes and treat .* as consuming element.
        /// Keep the or flat, assuming both right and left are flat.
        /// Apply a counber subsumption/combining optimization, such that e.g. a{2,5}|a{3,10} will be combined to a{2,10}.
        /// </summary>
        internal static SymbolicRegexNode<TSet> OrderedOr(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, bool deduplicated = false)
        {
            if (left.IsAnyStar || right == builder._nothing || left == right || (left.IsNullable && right.IsEpsilon))
                return left;
            if (left == builder._nothing)
                return right;

            SymbolicRegexNode<TSet> rl = right._kind == SymbolicRegexNodeKind.OrderedOr ? right._left! : right;
            SymbolicRegexNode<TSet> rr = right._kind == SymbolicRegexNodeKind.OrderedOr ? right._right! : builder._nothing;

            if (builder.Subsumes(left, rl))
                return OrderedOr(builder, left, rr);

            ////try to simplify left|(rl|rr) by attempting to detect left as a suffix of rl
            ////this subsumption simplies a common case of taking derivatives of concatenations by keeping them more compact
            //if (TryToSimplifyAlternation(builder, left, rl, out SymbolicRegexNode<TSet>? left_or_rl))
            //{
            //    Debug.Assert(left_or_rl is not null);
            //    return MkAlternation(left_or_rl, rr);
            //}

            ////check if both left and rl start with loops with the same body and continuation
            ////try to match the case (X{l1,u1}Y|X{l2,u2}Y)
            //SymbolicRegexNode<TSet> X1 = left._kind == SymbolicRegexNodeKind.Concat ? left._left! : left;
            //SymbolicRegexNode<TSet> Y = left._kind == SymbolicRegexNodeKind.Concat ? left._right! : builder.Epsilon;
            //SymbolicRegexNode<TSet> X2 = rl._kind == SymbolicRegexNodeKind.Concat ? rl._left! : rl;
            //SymbolicRegexNode<TSet> Y_ = rl._kind == SymbolicRegexNodeKind.Concat ? rl._right! : builder.Epsilon;
            //if (X1._kind == SymbolicRegexNodeKind.Loop && X2._kind == SymbolicRegexNodeKind.Loop &&
            //    X1._left == X2._left && Y == Y_)
            //{
            //    Debug.Assert(X1._left is not null);
            //    SymbolicRegexNode<TSet> X = X1._left;
            //    if ((X1._lower == X1._upper || X1._info.IsLazyLoop) && X2._lower == X1._upper + 1 && X2._lower == X2._upper)
            //    {
            //        // (X{l,u}?Y|X{u+1}Y) --> X{l,u+1}?Y
            //        //    (X{u}Y|X{u+1}Y) --> X{u,u+1}?Y
            //        SymbolicRegexNode<TSet> XY = builder.CreateConcat(builder.CreateLoop(X, true, X1._lower, X2._upper), Y);
            //        return MkAlternation(XY, rr);
            //    }
            //}

            // If left is not an Or, try to avoid allocation by checking if deduplication is necessary
            if (!deduplicated && left._kind != SymbolicRegexNodeKind.OrderedOr)
            {
                SymbolicRegexNode<TSet> current = right;
                // Initially assume there are no duplicates
                deduplicated = true;
                while (current._kind == SymbolicRegexNodeKind.OrderedOr)
                {
                    Debug.Assert(current._left is not null && current._right is not null);
                    // All Ors are supposed to be in a right associative normal form
                    Debug.Assert(current._left._kind != SymbolicRegexNodeKind.OrderedOr);
                    if (current._left == left)
                    {
                        // Duplicate found, mark that and exit early
                        deduplicated = false;
                        break;
                    }
                    current = current._right;
                }
                // If the loop above got to the end, current is the last element. Check that too
                if (deduplicated)
                    deduplicated = (current != left);
            }

            if (!deduplicated || left._kind == SymbolicRegexNodeKind.OrderedOr)
            {
                // If the left side was an or, then it has to be flattened, gather the elements from both sides
                List<SymbolicRegexNode<TSet>> elems = left.ToList(listKind: SymbolicRegexNodeKind.OrderedOr);
                int firstRightElem = elems.Count;
                right.ToList(elems, listKind: SymbolicRegexNodeKind.OrderedOr);

                // Eliminate any duplicate elements, keeping the leftmost element
                HashSet<SymbolicRegexNode<TSet>> seenElems = new();
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
                    SymbolicRegexNode<TSet> or = builder._nothing;
                    for (int i = elems.Count - 1; i >= 0; i--)
                    {
                        or = OrderedOr(builder, elems[i], or, deduplicated: true);
                    }
                    return or;
                }
                else
                {
                    SymbolicRegexNode<TSet> or = right;
                    for (int i = firstRightElem - 1; i >= 0; i--)
                    {
                        or = OrderedOr(builder, elems[i], or, deduplicated: true);
                    }
                    return or;
                }
            }

            Debug.Assert(left._kind != SymbolicRegexNodeKind.OrderedOr);
            Debug.Assert(deduplicated);

            // Counter optimization did not apply, just build the or
            return Create(builder, SymbolicRegexNodeKind.OrderedOr, left, right, -1, -1, default, null, SymbolicRegexInfo.Alternate(left._info, right._info));

            //SymbolicRegexNode<TSet> MkAlternation(SymbolicRegexNode<TSet> arg1, SymbolicRegexNode<TSet> arg2) =>
            //    arg2.IsNothing ? arg1 : Create(builder, SymbolicRegexNodeKind.OrderedOr, arg1, arg2, -1, -1, default, null, SymbolicRegexInfo.Alternate(arg1._info, arg2._info));

            //bool TryToOptimizeOrderedOrOfLeftWithRightLeft(out SymbolicRegexNode<TSet> combined)
            //{
            //    SymbolicRegexNode<TSet> rightL = right.Left;

            //    //try to match the case were left = YR and rightL = (Z)??R where Z = XY and Y is a nullable
            //    //simplify the alternation YR|(Z)??R into (X)??YR
            //    //this pattern will keep repeating where R is growing
            //    //here Z is assumed to be in left-associative form that has been created by earlier simplifications
            //    if (rightL.TrySplitConcatWithLazy01LoopLeft(out SymbolicRegexNode<TSet> Z, out SymbolicRegexNode<TSet> R) &&
            //        left.TrySplitConcatWithSuffix(R, out SymbolicRegexNode<TSet> Y) &&
            //        Z.TrySplitLeftAssocConcatWithSuffix(Y, out SymbolicRegexNode<TSet> X))
            //    {
            //        SymbolicRegexNode<TSet> X1 = builder.CreateLoop(X, true, 0, 1);
            //        combined = builder.CreateConcat(X1, left);
            //        return true;
            //    }

            //    if (rightL._kind != SymbolicRegexNodeKind.Concat || !rightL.Left.IsNullable)
            //    {
            //        combined = builder._nothing;
            //        return false;
            //    }

            //    //this case kicks in if right.Left does not start with a lazy 0-1-loop
            //    //try to create prefix as a concatenation of nullable elements in left-associative form
            //    //such that right.Left == prefix ++ left
            //    SymbolicRegexNode<TSet> prefix = rightL.Left;
            //    rightL = rightL.Right;
            //    while (rightL != left)
            //    {
            //        if (rightL._kind != SymbolicRegexNodeKind.Concat || !rightL.Left.IsNullable)
            //        {
            //            combined = builder._nothing;
            //            return false;
            //        }

            //        prefix = CreateConcat(builder, prefix, rightL.Left, true);
            //        rightL = rightL.Right;
            //    }

            //    combined = CreateConcat(builder, builder.CreateLoop(prefix, isLazy: true, lower: 0, upper: 1), left);
            //    return true;
            //}
        }

        /// <summary>If left = X{0,n}Y and right = X^nXY then simplify left|right to X{0,n+1}Y</summary>
        private static bool TryToSimplifyAlternation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, out SymbolicRegexNode<TSet>? left_or_right)
        {
            // right subsumes left
            // Y|XY  --> X??Y
            if (right._kind == SymbolicRegexNodeKind.Concat)
            {
                Debug.Assert(right._left is not null && right._right is not null);
                if (right._right == left)
                {
                    left_or_right = CreateConcat(left._builder, right._left.MakeOptional(), left);
                    return true;
                }
            }

            // right subsumes left
            // Y|X??(ZY) --> (X??Z??)Y
            if (right._kind == SymbolicRegexNodeKind.Concat)
            {
                Debug.Assert(right._left is not null && right._right is not null);
                if (right._right._kind == SymbolicRegexNodeKind.Concat && right._left!.IsHighPriorityNullable & right._right!._right == left)
                {
                    Debug.Assert(builder.Subsumes(right, left)); // TODO: just sanity checking
                    Debug.Assert(right._right._left is not null && right._right._right is not null);
                    //here rl = (X,(Z,left)) where X is high-priority nullable
                    //then left|(X,(Z,left))|rr is simplied into ((X,Z?),left)|rr
                    SymbolicRegexNode<TSet> X = right._left;
                    SymbolicRegexNode<TSet> Z = right._right._left;
                    //this simplifaction maintains the same pattern that can then be detected by this rule again
                    //which is why rl1 is created in left-assaciative form as ((X,Z?),left) instead of (X,(Z?,left))
                    //observe that (X,Z?) remains to be high-priority nullable because both X and Y? are so
                    //e.g. if X=a{0,k}? and Y=a then CreateConcat(X,Z?) = a{0,k+1}?
                    left_or_right = builder.CreateConcat(builder.CreateConcat(X, Z.MakeOptional()), left);
                    return true;
                }
            }

            // right subsumes left
            // Y|X{0,k}?XY --> X{0,k+1}?Y
            if (right._kind == SymbolicRegexNodeKind.Concat && right._left!.Kind == SymbolicRegexNodeKind.Loop &&
                right._left._lower == 0 && right._left._info.IsLazyLoop &&
                right._right!._kind == SymbolicRegexNodeKind.Concat &&
                right._left._left == right._right._left && right._right._right == left)
            {
                Debug.Assert(builder.Subsumes(right, left)); // TODO: just sanity checking
                Debug.Assert(right._left._left is not null);
                left_or_right = CreateConcat(left._builder, CreateLoop(left._builder, right._left._left, 0, right._left._upper + 1, true), left);
                return true;
            }

            // right subsumes left, but not detected currently
            // X{0,k}?Y|X{k+1}Y --> X{0,k+1}?Y
            if (left._kind == SymbolicRegexNodeKind.Concat && left._left!._kind == SymbolicRegexNodeKind.Loop &&
                left._left._lower == 0 && left._left._info.IsLazyLoop &&
                right._kind == SymbolicRegexNodeKind.Concat &&
                right._left!.Kind == SymbolicRegexNodeKind.Loop &&
                right._left._upper == left._left._upper + 1)
            {
                //Debug.Assert(builder.Subsumes(right, left)); // TODO: just sanity checking
                left_or_right = CreateConcat(left._builder, CreateLoop(left._builder, left._left._left!, 0, right._left._upper, true), left._right!);
                return true;
            }

            // TODO: remove, probably obsolete due to above rule
            // X{0,k}?Y|X^kXY --> X{0,k+1}?Y
            if (left._kind == SymbolicRegexNodeKind.Concat && left._left!._kind == SymbolicRegexNodeKind.Loop &&
            left._left._lower == 0 && left._left._info.IsLazyLoop)
            {
                SymbolicRegexNode<TSet> X = left._left._left!;
                SymbolicRegexNode<TSet> Y = left._right!;
                int n = left._left._upper;
                SymbolicRegexNode<TSet> nthright = right;
                //try to extract the n'th right while checking that the first element is X
                while (n > 0 && nthright._kind == SymbolicRegexNodeKind.Concat && X == nthright._left!)
                {
                    nthright = nthright._right!;
                    n--;
                }
                if (n > 0)
                {
                    //the loop did not complete
                    left_or_right = null;
                    return false;
                }
                if (Y == nthright)
                {
                    //left subsumes right because right = X^nY and X{0,n}Y|X^nY is equivalent to X{0,n}Y
                    left_or_right = left;
                    return true;
                }
                if (nthright._kind == SymbolicRegexNodeKind.Concat && X == nthright._left! && Y == nthright._right!)
                {
                    //X{0,k}Y|X^kXY is simplified to X{0,k+1}Y
                    left_or_right = CreateConcat(left._builder, CreateLoop(left._builder, X, 0, left._left._upper + 1, true), Y);
                    return true;
                }
            }

            left_or_right = null;
            return false;
        }


        private SymbolicRegexNode<TSet> Left { get { Debug.Assert(_left is not null); return _left; } }
        private SymbolicRegexNode<TSet> Right { get { Debug.Assert(_right is not null); return _right; } }

        private SymbolicRegexNode<TSet> SkipCaptureMarkers()
        {
            SymbolicRegexNode<TSet> node = this;
            while (node.Kind == SymbolicRegexNodeKind.Concat &&
                   (node.Left._kind == SymbolicRegexNodeKind.CaptureStart ||
                    node.Left._kind == SymbolicRegexNodeKind.CaptureEnd))
            {
                node = node.Right;
            }

            return node;
        }

        //private bool IfSubsumesCheck2(SymbolicRegexNode<TSet> node, out SymbolicRegexNode<TSet> result)
        //{
        //    List<SymbolicRegexNode<TSet>>? markers = null;
        //    SymbolicRegexNode<TSet> current = this;
        //    while (current.Kind == SymbolicRegexNodeKind.Concat && current.Left._kind == SymbolicRegexNodeKind.CaptureStart)
        //    {
        //        markers ??= new();
        //        markers.Add(current.Left);
        //        current = current.Right;
        //    }

        //    if (current.Kind == SymbolicRegexNodeKind.Concat && current.Left == node)
        //    {
        //        result = _builder.CreateConcat(_builder.CreateLoop(node, true, 0, 1), current.Right);
        //        if (markers is not null)
        //        {
        //            for (int i= markers.Count - 1; i >= 0; i--)
        //            {
        //                // add back the markers
        //                result = _builder.CreateConcat(markers[i], result);
        //            }
        //        }
        //        return true;
        //    }

        //    result = _builder._nothing;
        //    return false;
        //}

        ///// <summary>Try to split this into prefix ++ suffix for the given suffix</summary>
        ///// <param name="suffix">given suffix</param>
        ///// <param name="prefix">computed prefix in left-associative form</param>
        ///// <returns>true iff the split succeeds</returns>
        //private bool TrySplitLeftAssocConcatWithSuffix(SymbolicRegexNode<TSet> suffix, out SymbolicRegexNode<TSet> prefix)
        //{
        //    if (this == suffix)
        //    {
        //        //the prefix is empty
        //        prefix = _builder.Epsilon;
        //        return true;
        //    }

        //    //try to split the concatenation into a prefix followed by the given suffix
        //    //here the concatenation as well as the given suffix
        //    //are given in left associative form that result from prior steps
        //    prefix = this;
        //    while (prefix._kind == SymbolicRegexNodeKind.Concat)
        //    {
        //        if (suffix.Kind == SymbolicRegexNodeKind.Concat)
        //        {
        //            if (suffix.Right != prefix.Right)
        //            {
        //                //the current last elements do not match
        //                return false;
        //            }
        //            //check the prior elements backwards
        //            suffix = suffix.Left;
        //            prefix = prefix.Left;
        //        }
        //        else
        //        {
        //            //at the beginning of the suffix
        //            //check that the remaining element is the last element of prefix
        //            if (suffix == prefix.Right)
        //            {
        //                prefix = prefix.Left;
        //                return true;
        //            }
        //            return false;
        //        }
        //    }

        //    //at this point the suffix did not match
        //    //the case that prefix == suffix is not possible at this point
        //    //because else the initial test (this == suffix) must have succeeded

        //    Debug.Assert(prefix != suffix);

        //    return false;
        //}

        ///// <summary>Try to split a concat into prefix (in left associative form) ++ suffix</summary>
        ///// <param name="suffix">given suffix in right associative form</param>
        ///// <param name="prefix">output prefix in left associative form</param>
        ///// <returns>true iff the split succeeds</returns>
        //private bool TrySplitConcatWithSuffix(SymbolicRegexNode<TSet> suffix, out SymbolicRegexNode<TSet> prefix)
        //{
        //    //try to split the concatenation into a nullable prefix followed by the given suffix
        //    prefix = _builder.Epsilon;
        //    SymbolicRegexNode<TSet> rest = this;
        //    while (rest._kind == SymbolicRegexNodeKind.Concat && rest.Left.IsNullable)
        //    {
        //        //keep the prefix in left associative form
        //        prefix = CreateConcat(_builder, prefix, rest.Left, true);
        //        rest = rest.Right;
        //        if (rest == suffix)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}


        ///// <summary>Try to split concat into (leftLoopBody)?? ++ right</summary>
        ///// <param name="leftLoopBody">output loop body of a lazy 0-1-loop</param>
        ///// <param name="right">output right element of the concatenation</param>
        ///// <returns>true iff the split succeeds</returns>
        //private bool TrySplitConcatWithLazy01LoopLeft(out SymbolicRegexNode<TSet> leftLoopBody, out SymbolicRegexNode<TSet> right)
        //{
        //    if (_kind == SymbolicRegexNodeKind.Concat &&
        //        Left.Kind == SymbolicRegexNodeKind.Loop &&
        //        Left.IsLazy && Left._lower == 0 && Left._upper == 1)
        //    {
        //        leftLoopBody = Left.Left;
        //        right = Right;
        //        return true;
        //    }
        //    leftLoopBody = _builder._nothing;
        //    right = _builder._nothing;
        //    return false;
        //}

        /// <summary>
        /// Extract a counter as a loop followed by its continuation. For example, a*b returns (a*,b).
        /// Also look into the first element of an or, so a+|xyz returns (a+,()).
        /// If no counter is found returns ([],[]).
        /// </summary>
        /// <returns>a tuple of the loop and its continuation</returns>
        private (SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>) FirstCounterInfo()
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

        internal static SymbolicRegexNode<TSet> Not(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> root)
        {
            // Instead of just creating a negated root node
            // Convert ~root to Negation Normal Form (NNF) by using deMorgan's laws and push ~ to the leaves
            // This may avoid rather large overhead (such case was discovered with unit test PasswordSearchDual)
            // Do this transformation in-line without recursion, to avoid any chance of deep recursion
            // OBSERVE: NNF[node] represents the Negation Normal Form of ~node
            Dictionary<SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>> NNF = new();
            Stack<(SymbolicRegexNode<TSet>, bool)> todo = new();
            todo.Push((root, false));
            while (todo.Count > 0)
            {
                (SymbolicRegexNode<TSet>, bool) top = todo.Pop();
                bool secondTimePushed = top.Item2;
                SymbolicRegexNode<TSet> node = top.Item1;
                if (secondTimePushed)
                {
                    Debug.Assert((node._kind == SymbolicRegexNodeKind.Or || node._kind == SymbolicRegexNodeKind.And) && node._alts is not null);
                    // Here all members of _alts have been processed
                    List<SymbolicRegexNode<TSet>> alts_nnf = new();
                    foreach (SymbolicRegexNode<TSet> elem in node._alts)
                    {
                        alts_nnf.Add(NNF[elem]);
                    }
                    // Using deMorgan's laws, flip the kind: Or becomes And, And becomes Or
                    SymbolicRegexNode<TSet> node_nnf = node._kind == SymbolicRegexNodeKind.Or ? And(builder, alts_nnf.ToArray()) : Or(builder, alts_nnf.ToArray());
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
                            foreach (SymbolicRegexNode<TSet> elem in node._alts)
                            {
                                todo.Push((elem, false));
                            }
                            break;

                        case SymbolicRegexNodeKind.Epsilon:
                            //  ~() = .+
                            NNF[node] = SymbolicRegexNode<TSet>.CreateLoop(builder, builder._anyChar, 1, int.MaxValue, isLazy: false);
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

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(_left is not null);
                    return _left.GetFixedLength();
            }

            return -1;
        }

#if DEBUG
        private TransitionRegex<TSet>? _transitionRegex;
        /// <summary>
        /// Computes the symbolic derivative as a transition regex.
        /// Transitions are in the tree left to right in the order the backtracking engine would explore them.
        /// </summary>
        internal TransitionRegex<TSet> CreateDerivative()
        {
            if (_transitionRegex is not null)
            {
                return _transitionRegex;
            }

            if (IsNothing || IsEpsilon)
            {
                _transitionRegex = TransitionRegex<TSet>.Leaf(_builder._nothing);
                return _transitionRegex;
            }

            if (IsAnyStar || IsAnyPlus)
            {
                _transitionRegex = TransitionRegex<TSet>.Leaf(_builder._anyStar);
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
                    _transitionRegex = TransitionRegex<TSet>.Conditional(_set, TransitionRegex<TSet>.Leaf(_builder.Epsilon), TransitionRegex<TSet>.Leaf(_builder._nothing));
                    break;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    TransitionRegex<TSet> mainTransition = _left.CreateDerivative().Concat(_right);

                    if (!_left.CanBeNullable)
                    {
                        // If _left is never nullable
                        _transitionRegex = mainTransition;
                    }
                    else if (_left.IsNullable)
                    {
                        // If _left is unconditionally nullable
                        _transitionRegex = TransitionRegex<TSet>.Union(mainTransition, _right.CreateDerivative());
                    }
                    else
                    {
                        // The left side contains anchors and can be nullable in some context
                        // Extract the nullability as the lookaround condition
                        SymbolicRegexNode<TSet> leftNullabilityTest = _left.ExtractNullabilityTest();
                        _transitionRegex = TransitionRegex<TSet>.Lookaround(leftNullabilityTest, TransitionRegex<TSet>.Union(mainTransition, _right.CreateDerivative()), mainTransition);
                    }
                    break;

                case SymbolicRegexNodeKind.Loop:
                    // d(R*) = d(R+) = d(R)R*
                    Debug.Assert(_left is not null);
                    Debug.Assert(_upper > 0);
                    TransitionRegex<TSet> step = _left.CreateDerivative();

                    if (IsStar || IsPlus)
                    {
                        _transitionRegex = step.Concat(_builder.CreateLoop(_left, IsLazy));
                    }
                    else
                    {
                        int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                        int newlower = _lower == 0 ? 0 : _lower - 1;
                        SymbolicRegexNode<TSet> rest = _builder.CreateLoop(_left, IsLazy, newlower, newupper);
                        _transitionRegex = step.Concat(rest);
                    }
                    break;

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    _transitionRegex = TransitionRegex<TSet>.Leaf(_builder._nothing);
                    foreach (SymbolicRegexNode<TSet> elem in _alts)
                    {
                        _transitionRegex = TransitionRegex<TSet>.Union(_transitionRegex, elem.CreateDerivative());
                    }
                    break;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _transitionRegex = TransitionRegex<TSet>.Union(_left.CreateDerivative(), _right.CreateDerivative());
                    break;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    // The derivative to TransitionRegex does not support backtracking simulation, so ignore this node
                    _transitionRegex = _left.CreateDerivative();
                    break;

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    _transitionRegex = TransitionRegex<TSet>.Leaf(_builder._anyStar);
                    foreach (SymbolicRegexNode<TSet> elem in _alts)
                    {
                        _transitionRegex = TransitionRegex<TSet>.Intersect(_transitionRegex, elem.CreateDerivative());
                    }
                    break;

                case SymbolicRegexNodeKind.Not:
                    Debug.Assert(_left is not null);
                    _transitionRegex = _left.CreateDerivative().Complement();
                    break;

                default:
                    _transitionRegex = TransitionRegex<TSet>.Leaf(_builder._nothing);
                    break;
            }
            return _transitionRegex;
        }
#endif

        /// <summary>
        /// Takes the derivative of the symbolic regex for the given element, which must be either
        /// a minterm (i.e. a class of characters that have identical behavior for all sets in the pattern)
        /// or a singleton set. This derivative simulates backtracking, i.e. it only considers paths that backtracking would
        /// take before accepting the empty string for this pattern and returns the pattern ordered in the order backtracking
        /// would explore paths. For example the derivative of a*ab for a is a*ab|b, while for a*?ab it is b|a*?ab.
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <returns>the derivative</returns>
        internal SymbolicRegexNode<TSet> CreateDerivative(TSet elem, uint context) => CreateDerivativeGeneric(elem, context).StripEffects();

        /// <summary>
        /// Takes the derivative of the symbolic regex for the given element, which must be either
        /// a minterm (i.e. a class of characters that have identical behavior for all sets in the pattern)
        /// or a singleton set. This derivative simulates backtracking, i.e. it only considers paths that backtracking would
        /// take before accepting the empty string for this pattern and returns the pattern ordered in the order backtracking
        /// would explore paths. For example the derivative of a*ab places a*ab before b, while for a*?ab the order is reversed.
        /// </summary>
        /// <remarks>
        /// The differences of this to <see cref="CreateDerivative(TSet,uint)"/> are that (1) effects (e.g. capture starts and ends)
        /// are considered and (2) the different elements that would form a top level union are instead returned as separate
        /// nodes (paired with their associated effects). This function is meant to be used for NFA simulation, where top level
        /// unions would be broken up into separate states anyway, so nodes are not combined even if they have the same effects.
        /// </remarks>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <returns>the derivative</returns>
        internal List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> CreateNfaDerivativeWithEffects(TSet elem, uint context)
        {
            List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> transitions = new();
            CreateDerivativeGeneric(elem, context).StripAndMapEffects(context, transitions);
            return transitions;
        }

        internal SymbolicRegexNode<TSet> CreateDerivativeGeneric(TSet elem, uint context)
        {
            if (this._kind == SymbolicRegexNodeKind.DisableBacktrackingSimulation)
            {
                //the kind can only occur at the top level and indicates that backtracking simulation is turned off
                Debug.Assert(_left is not null);
                SymbolicRegexNode<TSet> derivative = _left.CreateDerivativeRec(elem, context);
                //reinsert the marker that maintains the non-backtracking semantics
                return _builder.CreateDisableBacktrackingSimulation(derivative);
            }
            else
            {
                SymbolicRegexNode<TSet>? derivative;
                //key for backtracking derivative
                (SymbolicRegexNode<TSet>, TSet, uint, bool) key = (this, elem, context, true);
                if (_builder._derivativeCache.TryGetValue(key, out derivative))
                {
                    return derivative;
                }

                //if this node is nullable for the given context
                //then prune the node in order to maintain backtracking semantics
                SymbolicRegexNode<TSet> node = this;
                if (IsNullableFor(context))
                {
                    node = Prune(context);
                }
                derivative = node.CreateDerivativeRec(elem, context);
                _builder._derivativeCache[key] = derivative;
                return derivative;
            }
        }

        private SymbolicRegexNode<TSet> Prune(uint context)
        {
            switch (_kind)
            {
                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    if (_left.IsNullableFor(context))
                    {
                        return _left.Prune(context);
                    }
                    return OrderedOr(_builder, _left, _right.Prune(context), deduplicated: true);

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    if (_left._kind == SymbolicRegexNodeKind.OrderedOr)
                    {
                        Debug.Assert(_left._left is not null && _left._right is not null);
                        if (_left._left.IsNullableFor(context))
                        {
                            return CreateConcat(_builder, _left._left, _right).Prune(context);
                        }
                        return OrderedOr(_builder, CreateConcat(_builder, _left._left, _right), CreateConcat(_builder, _left._right, _right).Prune(context));
                    }
                    return CreateConcat(_builder, _left.Prune(context), _right.Prune(context));

                case SymbolicRegexNodeKind.Loop when _info.IsLazyLoop && _lower == 0:
                    return _builder.Epsilon;

                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(_left is not null && _right is not null);
                    return CreateEffect(_builder, _left.Prune(context), _right);

                default:
                    return this;
            }
        }


        /// <summary>Wraps this node into a lazy 0-1-loop, unless this node is already high priority nullable and therefore optional</summary>
        internal SymbolicRegexNode<TSet> MakeOptional() => _info.IsHighPriorityNullable ? this : CreateLoop(_builder, this, 0, 1, true);

        /// <summary>
        /// Helper function for CreateDerivative
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <returns>the derivative</returns>
        private SymbolicRegexNode<TSet> CreateDerivativeRec(TSet elem, uint context)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(CreateDerivativeRec, elem, context);
            }

            SymbolicRegexNode<TSet>? derivative;
            (SymbolicRegexNode<TSet>, TSet, uint, bool) key = (this, elem, context, false);
            if (_builder._derivativeCache.TryGetValue(key, out derivative))
            {
                return derivative;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Singleton:
                    {
                        Debug.Assert(_set is not null);
                        // The following check assumes that either (1) the element and set are minterms, in which case
                        // the element is exactly the set if the intersection is non-empty (satisfiable), or (2) the element is a singleton
                        // set in which case it is fully contained in the set if the intersection is non-empty.
                        if (!_builder._solver.IsEmpty(_builder._solver.And(elem, _set)))
                        {
                            // the sigleton is consumed so the derivative is epsilon
                            derivative = _builder.Epsilon;
                        }
                        else
                        {
                            derivative = _builder._nothing;
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);

                        if (!_left.IsNullableFor(context))
                        {
                            // If the left side is not nullable then the character must be consumed there.
                            // For example, Da(ab) = Da(a)b = b.
                            derivative = _builder.CreateConcat(_left.CreateDerivativeRec(elem, context), _right);
                        }
                        else
                        {
                            // Make sure the alternatives are ordered in the correct way
                            SymbolicRegexNode<TSet> lderiv = _left.CreateDerivativeRec(elem, context);
                            SymbolicRegexNode<TSet> lderivR = _builder.CreateConcat(lderiv, _right);
                            SymbolicRegexNode<TSet> rderiv = _right.CreateDerivativeRec(elem, context);
                            SymbolicRegexNode<TSet> rderivE = _builder.CreateEffect(rderiv, _left);
                            // if the left alternative is high-priority-nullable then
                            // the priority is to skip left and prioritize rderiv over lderivR
                            // Two examples: suppose elem = a
                            // 1) if _left = (((ab)*)?|ac) and _right = ab then _left is high-priority-nullable
                            //    then derivative = rderiv|lderivR = b|b(ab)*|c
                            // 2) if _left = (ac|((ab)*)?) and _right = ab then _left is not high-priority-nullable
                            //    then derivative = lderivR|rderiv = b(ab)*|c|b
                            // Suppose the next input is b
                            // In the first case backtracking would stop after reading b
                            // In the second case backtracking would try to continue to follow (ab)* after reading b
                            // This backtracking semantics is effectively being recorded into the order of the alternatives
                            derivative = _left.IsHighPriorityNullableFor(context) ? OrderedOr(_builder, rderivE, lderivR) : OrderedOr(_builder, lderivR, rderivE);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Loop:
                    {
                        Debug.Assert(_left is not null);
                        Debug.Assert(_upper > 0);

                        SymbolicRegexNode<TSet> bodyDerivative = _left.CreateDerivativeRec(elem, context);
                        if (bodyDerivative.IsNothing)
                        {
                            derivative = _builder._nothing;
                        }
                        else
                        {
                            // The loop derivative peels out one iteration and concatenates the body's derivative with the decremented loop,
                            // so d(R{m,n}) = d(R)R{max(0,m-1),n-1}. Note that n is guaranteed to be greater than zero, since otherwise the
                            // loop would have been simplified to nothing, and int.MaxValue is treated as infinity.
                            int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                            int newlower = _lower == 0 ? 0 : _lower - 1;
                            // the continued loop becomes epsilon when newlower == newupper == 0
                            // in which case the returned concatenation will be just bodyDerivative
                            derivative = _builder.CreateConcat(bodyDerivative, _builder.CreateLoop(_left, IsLazy, newlower, newupper));
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);

                        derivative = OrderedOr(_builder, _left.CreateDerivativeRec(elem, context), _right.CreateDerivativeRec(elem, context));
                        break;
                    }

                case SymbolicRegexNodeKind.Effect:
                    Debug.Fail($"{nameof(CreateDerivativeRec)}:{_kind}");
                    break;

                default:
                    // The derivative of any other case is nothing
                    derivative = _builder._nothing;
                    break;
            }

            _builder._derivativeCache[key] = derivative;
            return derivative;
        }

        internal SymbolicRegexNode<TSet> StripEffects()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(StripEffects);
            }

            if (!_info.ContainsEffect)
            {
                return this;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(_left is not null);
                    return _left.StripEffects();

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    Debug.Assert(_left._info.ContainsEffect && !_right._info.ContainsEffect);
                    return _builder.CreateConcat(_left.StripEffects(), _right);

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    List<SymbolicRegexNode<TSet>> elems = ToList(listKind: SymbolicRegexNodeKind.OrderedOr);
                    for (int i = 0; i < elems.Count; i++)
                        elems[i] = elems[i].StripEffects();
                    return _builder.OrderedOr(elems);

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    return _builder.CreateDisableBacktrackingSimulation(_left.StripEffects());

                default:
                    Debug.Fail($"{nameof(StripEffects)}:{_kind}");
                    return null;
            }
        }

        internal void StripAndMapEffects(uint context, List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> alternativesAndEffects, List<DerivativeEffect>? currentEffects = null)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(StripAndMapEffects, context, alternativesAndEffects, currentEffects);
                return;
            }

            currentEffects ??= new List<DerivativeEffect>();

            if (!_info.ContainsEffect)
            {
                alternativesAndEffects.Add((this, currentEffects.Count > 0 ? currentEffects.ToArray() : Array.Empty<DerivativeEffect>()));
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(_left is not null && _right is not null);
                    int oldEffectCount = currentEffects.Count;
                    _right.ApplyEffects((e, s) => s.Add(e), context, currentEffects);
                    _left.StripAndMapEffects(context, alternativesAndEffects, currentEffects);
                    currentEffects.RemoveRange(oldEffectCount, currentEffects.Count - oldEffectCount);
                    return;

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        Debug.Assert(_left._info.ContainsEffect && !_right._info.ContainsEffect);
                        int oldAlternativesCount = alternativesAndEffects.Count;
                        _left.StripAndMapEffects(context, alternativesAndEffects, currentEffects);
                        for (int i = oldAlternativesCount; i < alternativesAndEffects.Count; i++)
                        {
                            var (node, effects) = alternativesAndEffects[i];
                            alternativesAndEffects[i] = (_builder.CreateConcat(node, _right), effects);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.StripAndMapEffects(context, alternativesAndEffects, currentEffects);
                    _right.StripAndMapEffects(context, alternativesAndEffects, currentEffects);
                    break;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    {
                        Debug.Assert(_left is not null);
                        int oldAlternativesCount = alternativesAndEffects.Count;
                        _left.StripAndMapEffects(context, alternativesAndEffects, currentEffects);
                        for (int i = oldAlternativesCount; i < alternativesAndEffects.Count; i++)
                        {
                            var (node, effects) = alternativesAndEffects[i];
                            alternativesAndEffects[i] = (_builder.CreateDisableBacktrackingSimulation(node), effects);
                        }
                        break;
                    }

                default:
                    Debug.Fail($"{nameof(StripAndMapEffects)}:{_kind}");
                    return;
            }
        }

        /// <summary>
        /// Find all effects under this node and supply them to the callback.
        /// </summary>
        /// <remarks>
        /// The construction follows the paths that the backtracking matcher would take. For example in ()|() only the
        /// effects for the first alternative will be included.
        /// </remarks>
        /// <param name="apply">action called for each effect</param>
        /// <param name="context">the current context to determine nullability</param>
        /// <param name="arg">an additional argument passed through to all callbacks</param>
        internal void ApplyEffects<TArg>(Action<DerivativeEffect, TArg> apply, uint context, TArg arg)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(ApplyEffects, apply, context, arg);
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    Debug.Assert(_left.IsNullableFor(context) && _right.IsNullableFor(context));
                    _left.ApplyEffects(apply, context, arg);
                    _right.ApplyEffects(apply, context, arg);
                    break;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    // Apply effect when backtracking engine would enter loop
                    if (_lower != 0 || (_upper != 0 && !IsLazy && _left.IsNullableFor(context)))
                    {
                        Debug.Assert(_left.IsNullableFor(context));
                        _left.ApplyEffects(apply, context, arg);
                    }
                    break;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    if (_left.IsNullableFor(context))
                    {
                        // Prefer the left side
                        _left.ApplyEffects(apply, context, arg);
                    }
                    else
                    {
                        // Otherwise right side must be nullable
                        Debug.Assert(_right.IsNullableFor(context));
                        _right.ApplyEffects(apply, context, arg);
                    }
                    break;

                case SymbolicRegexNodeKind.CaptureStart:
                    apply(new DerivativeEffect(DerivativeEffectKind.CaptureStart, _lower), arg);
                    break;

                case SymbolicRegexNodeKind.CaptureEnd:
                    apply(new DerivativeEffect(DerivativeEffectKind.CaptureEnd, _lower), arg);
                    break;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    _left.ApplyEffects(apply, context, arg);
                    break;

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<TSet> elem in _alts)
                    {
                        if (elem.IsNullableFor(context))
                            elem.ApplyEffects(apply, context, arg);
                    }
                    break;

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<TSet> elem in _alts)
                    {
                        Debug.Assert(elem.IsNullableFor(context));
                        elem.ApplyEffects(apply, context, arg);
                    }
                    break;
            }
        }

#if DEBUG
        /// <summary>
        /// Computes the closure of CreateDerivative, by exploring all the leaves
        /// of the transition regex until no more new leaves are found.
        /// Converts the resulting transition system into a symbolic NFA.
        /// If the exploration remains incomplete due to the given state bound
        /// being reached then the InComplete property of the constructed NFA is true.
        /// </summary>
        internal SymbolicNFA<TSet> Explore(int bound) => SymbolicNFA<TSet>.Explore(this, bound);

        /// <summary>Extracts the nullability test as a Boolean combination of anchors</summary>
        public SymbolicRegexNode<TSet> ExtractNullabilityTest()
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
                    SymbolicRegexNode<TSet> disjunction = _builder._nothing;
                    foreach (SymbolicRegexNode<TSet> elem in _alts)
                    {
                        disjunction = _builder.Or(disjunction, elem.ExtractNullabilityTest());
                    }
                    return disjunction;
                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    return OrderedOr(_builder, _left.ExtractNullabilityTest(), _right.ExtractNullabilityTest());
                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    SymbolicRegexNode<TSet> conjunction = _builder._anyStar;
                    foreach (SymbolicRegexNode<TSet> elem in _alts)
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
#endif

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
                case SymbolicRegexNodeKind.Effect:
                    return HashCode.Combine(_left, _right, _info);

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    return HashCode.Combine(_left, _info);

                case SymbolicRegexNodeKind.Singleton:
                    return HashCode.Combine(_kind, _set);

                default:
                    Debug.Assert(_kind == SymbolicRegexNodeKind.Not);
                    return HashCode.Combine(_kind, _left, _info);
            };
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not SymbolicRegexNode<TSet> that)
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

#if DEBUG
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
                    sb.Append('(');
                    _left.ToString(sb);
                    sb.Append('|');
                    _right.ToString(sb);
                    sb.Append(')');
                    return;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);
                    //mark left associative case with parenthesis
                    if (_left.Kind == SymbolicRegexNodeKind.Concat)
                    {
                        sb.Append('(');
                    }
                    _left.ToString(sb);
                    if (_left.Kind == SymbolicRegexNodeKind.Concat)
                    {
                        sb.Append(')');
                    }
                    _right.ToString(sb);
                    return;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    sb.Append(_builder._solver.PrettyPrint(_set, _builder._charSetSolver));
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
                        if (IsLazy)
                        {
                            // lazy loop R{0,1}? is printed by R??
                            sb.Append('?');
                        }
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


                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(_left is not null && _right is not null);
                    sb.Append('(');
                    _left.ToString(sb);
                    sb.Append(")\u03BE(");
                    _right.ToString(sb);
                    sb.Append(')');
                    break;

                case SymbolicRegexNodeKind.CaptureStart:
                    sb.Append('\u230A'); // Left floor
                    // Include group number as a subscript
                    Debug.Assert(_lower >= 0);
                    foreach (char c in _lower.ToString())
                    {
                        sb.Append((char)('\u2080' + (c - '0')));
                    }
                    return;

                case SymbolicRegexNodeKind.CaptureEnd:
                    // Include group number as a superscript
                    Debug.Assert(_lower >= 0);
                    foreach (char c in _lower.ToString())
                    {
                        switch (c)
                        {
                            case '1':
                                sb.Append('\u00B9');
                                break;
                            case '2':
                                sb.Append('\u00B2');
                                break;
                            case '3':
                                sb.Append('\u00B3');
                                break;
                            default:
                                sb.Append((char)('\u2070' + (c - '0')));
                                break;
                        }
                    }
                    sb.Append('\u2309'); // Right ceiling
                    return;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    _left.ToString(sb);
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
#endif

        /// <summary>
        /// Returns all sets that occur in the regex or the full set if there are no sets in the regex (e.g. the regex is "^").
        /// </summary>
        public HashSet<TSet> GetSets()
        {
            var sets = new HashSet<TSet>();
            CollectSets(sets);
            return sets;
        }

        /// <summary>Collects all sets that occur in the regex into the specified collection.</summary>
        private void CollectSets(HashSet<TSet> sets)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(CollectSets, sets);
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.BOLAnchor:
                case SymbolicRegexNodeKind.EOLAnchor:
                case SymbolicRegexNodeKind.EndAnchorZ:
                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    sets.Add(_builder._newLineSet);
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
                    sets.Add(_set);
                    return;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    _left.CollectSets(sets);
                    return;

                case SymbolicRegexNodeKind.Or:
                case SymbolicRegexNodeKind.And:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<TSet> sr in _alts)
                    {
                        sr.CollectSets(sets);
                    }
                    return;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    _left.CollectSets(sets);
                    _right.CollectSets(sets);
                    return;

                case SymbolicRegexNodeKind.Concat:
                    // avoid deep nested recursion over long concat nodes
                    SymbolicRegexNode<TSet> conc = this;
                    while (conc._kind == SymbolicRegexNodeKind.Concat)
                    {
                        Debug.Assert(conc._left is not null && conc._right is not null);
                        conc._left.CollectSets(sets);
                        conc = conc._right;
                    }
                    conc.CollectSets(sets);
                    return;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    _left.CollectSets(sets);
                    return;

                case SymbolicRegexNodeKind.Not:
                    Debug.Assert(_left is not null);
                    _left.CollectSets(sets);
                    return;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                    sets.Add(_builder._wordLetterForBoundariesSet);
                    return;

                default:
                    Debug.Fail($"{nameof(CollectSets)}:{_kind}");
                    break;
            }
        }

        /// <summary>Compute and sort all the minterms from the sets in this regex.</summary>
        public TSet[] ComputeMinterms()
        {
            HashSet<TSet> sets = GetSets();
            List<TSet> minterms = _builder._solver.GenerateMinterms(sets);
            minterms.Sort();
            return minterms.ToArray();
        }

        /// <summary>
        /// Create the reverse of this regex
        /// </summary>
        public SymbolicRegexNode<TSet> Reverse()
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
                        SymbolicRegexNode<TSet> rev = _left.Reverse();
                        SymbolicRegexNode<TSet> rest = _right;
                        while (rest._kind == SymbolicRegexNodeKind.Concat)
                        {
                            Debug.Assert(rest._left is not null && rest._right is not null);
                            SymbolicRegexNode<TSet> rev1 = rest._left.Reverse();
                            rev = _builder.CreateConcat(rev1, rev);
                            rest = rest._right;
                        }
                        SymbolicRegexNode<TSet> restr = rest.Reverse();
                        rev = _builder.CreateConcat(restr, rev);
                        return rev;
                    }

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    return _builder.Or(_alts.Reverse());

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);
                    return OrderedOr(_builder, _left.Reverse(), _right.Reverse());

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

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    return _builder.CreateDisableBacktrackingSimulation(_left.Reverse());

                // Remaining cases map to themselves:
                case SymbolicRegexNodeKind.Epsilon:
                case SymbolicRegexNodeKind.Singleton:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                case SymbolicRegexNodeKind.NonBoundaryAnchor:
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

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    return _left.StartsWithLoop(upperBoundLowestValue);

                default:
                    return false;
            };
        }


        /// <summary>Gets the set that includes all elements that can start a match.</summary>
        internal TSet GetStartSet() => _startSet;

        /// <summary>Computes the set that includes all elements that can start a match.</summary>
        private TSet ComputeStartSet()
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
                    return _builder._solver.Empty;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    return _set;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    return _left._startSet;

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        TSet startSet = _left.CanBeNullable ? _builder._solver.Or(_left._startSet, _right._startSet) : _left._startSet;
                        return startSet;
                    }

                case SymbolicRegexNodeKind.Or:
                    {
                        Debug.Assert(_alts is not null);
                        TSet startSet = _builder._solver.Empty;
                        foreach (SymbolicRegexNode<TSet> alt in _alts)
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
                        TSet startSet = _builder._solver.Full;
                        foreach (SymbolicRegexNode<TSet> alt in _alts)
                        {
                            startSet = _builder._solver.And(startSet, alt._startSet);
                        }
                        return startSet;
                    }

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(_left is not null);
                    return _left._startSet;

                default:
                    Debug.Assert(_kind == SymbolicRegexNodeKind.Not);
                    return _builder._solver.Full;
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
        internal SymbolicRegexNode<TSet> PruneAnchors(uint prevKind, bool contWithWL, bool contWithNWL)
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
                    SymbolicRegexNode<TSet> body = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                    return body == _left ?
                        this :
                        CreateLoop(_builder, body, _lower, _upper, IsLazy);

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<TSet> left1 = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<TSet> right1 = _left.IsNullable ? _right.PruneAnchors(prevKind, contWithWL, contWithNWL) : _right;

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            CreateConcat(_builder, left1, right1);
                    }

                case SymbolicRegexNodeKind.Or:
                    {
                        Debug.Assert(_alts != null);
                        var elements = new SymbolicRegexNode<TSet>[_alts.Count];
                        int i = 0;
                        foreach (SymbolicRegexNode<TSet> alt in _alts)
                        {
                            elements[i++] = alt.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        }
                        Debug.Assert(i == elements.Length);
                        return Or(_builder, elements);
                    }

                case SymbolicRegexNodeKind.OrderedOr:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<TSet> left1 = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<TSet> right1 = _right.PruneAnchors(prevKind, contWithWL, contWithNWL);

                        Debug.Assert(left1 is not null && right1 is not null);
                        return left1 == _left && right1 == _right ?
                            this :
                            OrderedOr(_builder, left1, right1);
                    }

                case SymbolicRegexNodeKind.Effect:
                    {
                        Debug.Assert(_left is not null && _right is not null);
                        SymbolicRegexNode<TSet> left1 = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                        return left1 == _left ?
                            this :
                            CreateEffect(_builder, left1, _right);
                    }

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(_left is not null);
                    SymbolicRegexNode<TSet> child = _left.PruneAnchors(prevKind, contWithWL, contWithNWL);
                    return child == _left ?
                        this :
                        _builder.CreateDisableBacktrackingSimulation(child);

                default:
                    return this;
            }
        }

        /// <summary>
        /// Represents a list of transitions without duplicate effect arrays.
        /// Keeps the list of transitions unique, expects higher priority transitions
        /// to be added first and ignores equivalent lower priority transitions.
        /// </summary>
        private sealed class TransitionList
        {
            private SymbolicRegexBuilder<TSet> _builder;
            internal List<(SymbolicRegexNode<TSet> Node, DerivativeEffect[] Effects)> _transitions = new();
            private readonly HashSet<SymbolicRegexNode<TSet>> _nodes = new();

            public TransitionList(SymbolicRegexBuilder<TSet> builder) { _builder = builder; }

            public int Count => _transitions.Count;

            public (SymbolicRegexNode<TSet> Node, DerivativeEffect[]) this[int i] => _transitions[i];

            public (SymbolicRegexNode<TSet> Node, DerivativeEffect[]) LastTransition => _transitions[_transitions.Count - 1];

            internal void Add(SymbolicRegexNode<TSet> node, DerivativeEffect[] newEffects)
            {
                if (_nodes.Add(node))
                {
                    _transitions.Add((node, newEffects));
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new();
                for (int i = 0; i < _transitions.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",\n");
                    sb.Append(_transitions[i].Node);
                    sb.Append("-->(");
                    for (int j = 0; j < _transitions[i].Effects.Length; j++)
                    {
                        if (j > 0)
                            sb.Append(',');
                        sb.Append(_transitions[i].Effects[j]);
                    }
                    sb.Append(')');
                }

                return sb.ToString();
            }

            public SymbolicRegexNode<TSet> Union
            {
                get
                {
                    SymbolicRegexNode<TSet> union = _builder._nothing;
                    for (int i = _transitions.Count - 1; i >= 0; --i)
                    {
                        Debug.Assert(_transitions[i].Node._kind != SymbolicRegexNodeKind.DisableBacktrackingSimulation);
                        union = OrderedOr(_builder, _transitions[i].Node, union, true);
                    }
                    return union;
                }
            }

            public TransitionList DisableBacktrackingSimulation()
            {
                TransitionList transitions = new(_builder);
                for (int i = 0; i < _transitions.Count; ++i)
                {
                    Debug.Assert(_transitions[i].Node._kind != SymbolicRegexNodeKind.DisableBacktrackingSimulation);
                    transitions.Add(_builder.CreateDisableBacktrackingSimulation(_transitions[i].Node), _transitions[i].Effects);
                }
                return transitions;
            }
        }
    }
}
