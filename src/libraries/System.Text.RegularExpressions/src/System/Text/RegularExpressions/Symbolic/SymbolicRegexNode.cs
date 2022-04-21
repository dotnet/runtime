// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents an abstract syntax tree node of a symbolic regex.</summary>
    internal sealed class SymbolicRegexNode<TSet> where TSet : IComparable<TSet>
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
            Create(builder, SymbolicRegexNodeKind.FixedLengthMarker, null, null, length, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        internal static SymbolicRegexNode<TSet> CreateEpsilon(SymbolicRegexBuilder<TSet> builder) =>
            Create(builder, SymbolicRegexNodeKind.Epsilon, null, null, -1, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        internal static SymbolicRegexNode<TSet> CreateEagerEmptyLoop(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> body) =>
            Create(builder, SymbolicRegexNodeKind.Loop, body, null, 0, 0, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true, isLazy: false));

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

        internal static SymbolicRegexNode<TSet> Or(SymbolicRegexBuilder<TSet> builder, params SymbolicRegexNode<TSet>[] disjuncts) =>
            CreateCollection(builder, SymbolicRegexNodeKind.Or, SymbolicRegexSet<TSet>.CreateMulti(builder, disjuncts, SymbolicRegexNodeKind.Or), SymbolicRegexInfo.Or(GetInfos(disjuncts)));

        internal static SymbolicRegexNode<TSet> Or(SymbolicRegexBuilder<TSet> builder, SymbolicRegexSet<TSet> disjuncts)
        {
            Debug.Assert(disjuncts._kind == SymbolicRegexNodeKind.Or || disjuncts.IsEverything);
            return CreateCollection(builder, SymbolicRegexNodeKind.Or, disjuncts, SymbolicRegexInfo.Or(GetInfos(disjuncts)));
        }

        internal static SymbolicRegexNode<TSet> And(SymbolicRegexBuilder<TSet> builder, params SymbolicRegexNode<TSet>[] conjuncts) =>
            CreateCollection(builder, SymbolicRegexNodeKind.And, SymbolicRegexSet<TSet>.CreateMulti(builder, conjuncts, SymbolicRegexNodeKind.And), SymbolicRegexInfo.And(GetInfos(conjuncts)));

        internal static SymbolicRegexNode<TSet> And(SymbolicRegexBuilder<TSet> builder, SymbolicRegexSet<TSet> conjuncts)
        {
            Debug.Assert(conjuncts.IsNothing || conjuncts._kind == SymbolicRegexNodeKind.And);
            return CreateCollection(builder, SymbolicRegexNodeKind.And, conjuncts, SymbolicRegexInfo.And(GetInfos(conjuncts)));
        }

        internal static SymbolicRegexNode<TSet> CreateCaptureStart(SymbolicRegexBuilder<TSet> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureStart, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        internal static SymbolicRegexNode<TSet> CreateCaptureEnd(SymbolicRegexBuilder<TSet> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureEnd, null, null, captureNum, -1, default, null, SymbolicRegexInfo.Create(isAlwaysNullable: true));

        internal static SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> child) =>
            Create(builder, SymbolicRegexNodeKind.DisableBacktrackingSimulation, child, null, -1, -1, default, null, child._info);

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
        internal static SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right)
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
            SymbolicRegexNode<TSet> concat = right;
            List<SymbolicRegexNode<TSet>> leftNodes = left.ToList();
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
        internal static SymbolicRegexNode<TSet> OrderedOr(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, bool deduplicated = false)
        {
            if (left.IsAnyStar || right == builder._nothing || left == right)
                return left;
            if (left == builder._nothing)
                return right;

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
            return Create(builder, SymbolicRegexNodeKind.OrderedOr, left, right, -1, -1, default, null, SymbolicRegexInfo.Or(left._info, right._info));
        }

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
        internal SymbolicRegexNode<TSet> CreateDerivative(TSet elem, uint context)
        {
            List<(SymbolicRegexNode<TSet> Node, DerivativeEffect[])> transitions = new();
            if (_kind == SymbolicRegexNodeKind.DisableBacktrackingSimulation)
            {
                // Since this node disables backtracking simulation, unwrap the node and pass the corresponding flag as
                // false to AddTransitions.
                Debug.Assert(_left is not null);
                _left.AddTransitions(elem, context, transitions, new List<SymbolicRegexNode<TSet>>(), null, simulateBacktracking: false);
            }
            else
            {
                AddTransitions(elem, context, transitions, new List<SymbolicRegexNode<TSet>>(), null, simulateBacktracking: true);
            }
            SymbolicRegexNode<TSet> derivative = _builder._nothing;
            // Iterate backwards to avoid quadratic rebuilding of the Or nodes, which are always simplified to
            // right associative form. Concretely:
            // In (a|(b|c)) | d -> (a|(b|(c|d)) the first argument is not a subtree of the result.
            // In a | (b|(c|d)) -> (a|(b|(c|d)) the second argument is a subtree of the result.
            // The first case performs linear work for each element, leading to a quadratic blowup.
            for (int i = transitions.Count - 1; i >= 0; --i)
            {
                SymbolicRegexNode<TSet> node = transitions[i].Node;
                Debug.Assert(node._kind != SymbolicRegexNodeKind.DisableBacktrackingSimulation);
                derivative = _builder.OrderedOr(node, derivative);
            }
            if (_kind == SymbolicRegexNodeKind.DisableBacktrackingSimulation)
                // Make future derivatives disable backtracking simulation too
                derivative = _builder.CreateDisableBacktrackingSimulation(derivative);
            return derivative;
        }

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
            if (_kind == SymbolicRegexNodeKind.DisableBacktrackingSimulation)
            {
                // Since this node disables backtracking simulation, unwrap the node and pass the corresponding flag as
                // false to AddTransitions.
                Debug.Assert(_left is not null);
                _left.AddTransitions(elem, context, transitions, new List<SymbolicRegexNode<TSet>>(), new Stack<DerivativeEffect>(), simulateBacktracking: false);
                // Make future derivatives disable backtracking simulation too
                for (int i = 0; i < transitions.Count; ++i)
                {
                    var (node, effects) = transitions[i];
                    Debug.Assert(node._kind != SymbolicRegexNodeKind.DisableBacktrackingSimulation);
                    transitions[i] = (_builder.CreateDisableBacktrackingSimulation(node), effects);
                }
            }
            else
            {
                AddTransitions(elem, context, transitions, new List<SymbolicRegexNode<TSet>>(), new Stack<DerivativeEffect>(), simulateBacktracking: true);
            }
            return transitions;
        }

        /// <summary>
        /// Base function used to implement derivative functions. Given an element and a context this will add all patterns
        /// whose union makes up the derivative to the given <paramref name="transitions"/> list. If the <paramref name="effects"/>
        /// stack is null, then effects are not tracked and all effects arrays in the result will be null. Transitions are added
        /// in an order that is consistent with backtracking.
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <param name="transitions">a list to add transitions to</param>
        /// <param name="continuation">a list used in recursive calls to track nodes to concatenate, should be an empty list at the root call</param>
        /// <param name="effects">a stack used in recursive calls to track effects, should be an empty stack at the root call</param>
        /// <param name="simulateBacktracking">whether the derivative should only consider paths that backtracking would take, true by default</param>
        private void AddTransitions(TSet elem, uint context, List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> transitions,
            List<SymbolicRegexNode<TSet>> continuation, Stack<DerivativeEffect>? effects, bool simulateBacktracking)
        {
            Debug.Assert(!_builder._solver.IsEmpty(elem), "False element or minterm should not make it into derivative construction.");

            // Helper function for concatenating a head node and a list of continuation nodes. The continuation nodes
            // are added in reverse order and the function below uses the list as a stack, so the nodes added to the
            // stack first end up at the tail of the concatenation.
            static SymbolicRegexNode<TSet> BuildLeaf(SymbolicRegexNode<TSet> head, List<SymbolicRegexNode<TSet>> continuation)
            {
                SymbolicRegexNode<TSet> leaf = head._builder.Epsilon;
                for (int i = 0; i < continuation.Count; ++i)
                {
                    leaf = head._builder.CreateConcat(continuation[i], leaf);
                }
                return head._builder.CreateConcat(head, leaf);
            }

            // Helper function for detecting when an unconditionally nullable pattern is in the transitions and no
            // more transitions need to be considered. If the derivative simulates backtracking, then in the next
            // step nothing after an unconditionally nullable state would be considered.
            // For example, d_a( a|ab ) under backtracking simulation transitions to just epsilon, while without
            // backtracking simulation it would transition to epsilon|b.
            // All parts of the function below should consider IsDone and return early when it is true.
            static bool IsDone(List<(SymbolicRegexNode<TSet> Node, DerivativeEffect[])> transitions, bool simulateBacktracking) =>
                simulateBacktracking && transitions.Count > 0 && transitions[transitions.Count - 1].Node.IsNullable;

            // Nothing and epsilon can't consume a character so they generate no transition
            if (IsNothing || IsEpsilon)
            {
                return;
            }

            // For both .* and .+ the derivative is .* for any character
            if (IsAnyStar || IsAnyPlus)
            {
                Debug.Assert(!IsDone(transitions, simulateBacktracking));
                SymbolicRegexNode<TSet> leaf = BuildLeaf(_builder._anyStar, continuation);
                transitions.Add((leaf, effects?.ToArray() ?? Array.Empty<DerivativeEffect>()));
                // Signal early exit if the leaf is unconditionally nullable
                return;
            }

            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(AddTransitions, elem, context, transitions, continuation, effects, simulateBacktracking);
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(!IsDone(transitions, simulateBacktracking));
                    Debug.Assert(_set is not null);
                    // The following check assumes that either (1) the element and set are minterms, in which case
                    // the element is exactly the set if the intersection is non-empty (satisfiable), or (2) the element is a singleton
                    // set in which case it is fully contained in the set if the intersection is non-empty.
                    if (!_builder._solver.IsEmpty(_builder._solver.And(elem, _set)))
                    {
                        SymbolicRegexNode<TSet> leaf = BuildLeaf(_builder.Epsilon, continuation);
                        transitions.Add((leaf, effects?.ToArray() ?? Array.Empty<DerivativeEffect>()));
                        // Signal early exit if the leaf is unconditionally nullable
                        return;
                    }
                    break;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(_left is not null && _right is not null);

                    if (!_left.IsNullableFor(context))
                    {
                        // If the left side can't be nullable then the character must be consumed there.
                        // For example, d(ab) = d(a)b.
                        continuation.Add(_right);
                        _left.AddTransitions(elem, context, transitions, continuation, effects, simulateBacktracking);
                        continuation.RemoveAt(continuation.Count - 1);
                    }
                    else if (_left._kind == SymbolicRegexNodeKind.OrderedOr)
                    {
                        Debug.Assert(_left._left is not null && _left._right is not null);
                        // The case of a concatenation with an alternation on the left side, i.e. (R|T)S, is handled
                        // separately by applying the rewrite to push the concatenation in, i.e. (R|T)S -> RS|TS.
                        // This is done to support the rule for concatenations with a lazy loop on the left side,
                        // i.e. R{m,n}?T, which have to be handled as part of the concatenation case to properly order
                        // the paths in a way that matches the backtracking engines preference. By pushing the
                        // concatenation into the alternation any loops inside the alternation are guaranteed to show up
                        // directly on the left side of a concatenation.

                        // This pattern is for the path where the backtracking matcher would find the match from the first alternative
                        SymbolicRegexNode<TSet> leftLeftPath = _builder.CreateConcat(_left._left, _right);

                        // The backtracking-simulating derivative of the left path will be used when the pattern is nullable,
                        // i.e. the backtracking matcher would end the match rather than go onto paths in the second
                        // alternative. When the path through the first alternative is not nullable or the derivative is not
                        // backtracking-simulating, then all paths through it are considered.
                        bool leftLeftSimulateBacktracking = simulateBacktracking && leftLeftPath.IsNullableFor(context);
                        leftLeftPath.AddTransitions(elem, context, transitions, continuation, effects, leftLeftSimulateBacktracking);
                        // Include the path through the right side only if the left side is not nullable or the derivative
                        // is not simulating backtracking.
                        // For example, d( (a|b)c* ) = d(ac) | d(bc), while on the other hand d( (a?|b)c* ) = d(a?c*).
                        // In the latter case the second alternative is omitted because the backtracking matcher would rather
                        // accept an empty string for a?c* than anything for bc*.
                        if (!IsDone(transitions, simulateBacktracking) && !leftLeftSimulateBacktracking)
                            _builder.CreateConcat(_left._right, _right).AddTransitions(elem, context, transitions, continuation, effects, simulateBacktracking);
                    }
                    else
                    {
                        // Helper function for the case where the right side consumes the character
                        static void RightTransition(SymbolicRegexNode<TSet> node, TSet elem, uint context, List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> transitions,
                            List<SymbolicRegexNode<TSet>> continuation, Stack<DerivativeEffect>? effects, bool simulateBacktracking)
                        {
                            Debug.Assert(node._left is not null && node._right is not null);
                            // Remember current number of effects so that we know how many to pop
                            int oldEffectsCount = effects?.Count ?? 0;
                            if (effects is not null)
                            {
                                // Push all effects onto the effects stack
                                node._left.ApplyEffects((effect, stack) => stack.Push(effect), context, effects);
                            }
                            node._right.AddTransitions(elem, context, transitions, continuation, effects, simulateBacktracking);
                            if (effects is not null)
                            {
                                // Pop all effects that were added here
                                while (effects.Count > oldEffectsCount)
                                    effects.Pop();
                            }
                        }

                        // Helper function for the case where the left side consumes the character
                        static void LeftTransition(SymbolicRegexNode<TSet> node, TSet elem, uint context, List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> transitions,
                            List<SymbolicRegexNode<TSet>> continuation, Stack<DerivativeEffect>? effects, bool simulateBacktracking)
                        {
                            Debug.Assert(node._left is not null && node._right is not null);
                            continuation.Add(node._right);
                            // Disable backtracking simulation for the left side if the right side is not nullable here.
                            // The intuition is that if the right side is not nullable, then backtracking would not accept the empty
                            // string here even when it hits a nullable path for the left side, so all paths through the left side
                            // need to be considered.
                            bool leftSimulateBacktracking = simulateBacktracking && node._right.IsNullableFor(context);
                            node._left.AddTransitions(elem, context, transitions, continuation, effects, leftSimulateBacktracking);
                            continuation.RemoveAt(continuation.Count - 1);
                        }

                        // Order the transitions. If the left side is a lazy loop that is nullable due to its lower bound then prefer the right side.
                        // This is done to match the order that backtracking engines would explore different alternatives, where for a lazy loop
                        // matches that consume as few characters into the loop as possible are preferred.
                        // For example, d(a*?b) = d(b) | d(a*?)b while without the lazy loop d(a*b) = d(a*)b | d(b).
                        if (_left._kind == SymbolicRegexNodeKind.Loop && _left.IsLazy && _left._lower == 0)
                        {
                            RightTransition(this, elem, context, transitions, continuation, effects, simulateBacktracking);
                            if (!IsDone(transitions, simulateBacktracking))
                                LeftTransition(this, elem, context, transitions, continuation, effects, simulateBacktracking);
                        }
                        else
                        {
                            LeftTransition(this, elem, context, transitions, continuation, effects, simulateBacktracking);
                            if (!IsDone(transitions, simulateBacktracking))
                                RightTransition(this, elem, context, transitions, continuation, effects, simulateBacktracking);
                        }
                    }
                    break;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(_left is not null);
                    Debug.Assert(_upper > 0);

                    // Add transitions only when the backtracking engines would prefer to enter the loop
                    if (!simulateBacktracking || !IsLazy || _lower != 0)
                    {
                        // The loop derivative peels out one iteration and concatenates the body's derivative with the decremented loop,
                        // so d(R{m,n}) = d(R)R{max(0,m-1),n-1}. Note that n is guaranteed to be greater than zero, since otherwise the
                        // loop would have been simplified to nothing, and int.MaxValue is treated as infinity.
                        int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
                        int newlower = _lower == 0 ? 0 : _lower - 1;
                        SymbolicRegexNode<TSet> rest = _builder.CreateLoop(_left, IsLazy, newlower, newupper);

                        continuation.Add(rest);
                        _left.AddTransitions(elem, context, transitions, continuation, effects, simulateBacktracking);
                        continuation.RemoveAt(continuation.Count - 1);
                    }
                    break;

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(_left is not null && _right is not null);

                    // The backtracking derivative for the first alternative will be used when it is nullable, i.e. the
                    // backtracking matcher would end the match rather than go onto paths in the right side. When
                    // the path through the left side is not nullable or the derivative doesn't simulate backtracking,
                    // then all paths through it are considered.
                    bool leftSimulateBacktracking = simulateBacktracking && _left.IsNullableFor(context);
                    _left.AddTransitions(elem, context, transitions, continuation, effects, simulateBacktracking && _left.IsNullableFor(context));
                    // Include the path through the right side only if the left side is not nullable or the derivative
                    // is not simulating backtracking.
                    // For example, d(a|b) = d(a) | d(b) while d(a*|b) = d(a*).
                    if (!IsDone(transitions, simulateBacktracking) && !leftSimulateBacktracking)
                        _right.AddTransitions(elem, context, transitions, continuation, effects, simulateBacktracking);
                    break;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Fail($"{nameof(AddTransitions)}: DisableBacktrackingSimulation should have been handled outside this function.");
                    break;

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(_alts is not null);
                    foreach (SymbolicRegexNode<TSet> alt in _alts)
                    {
                        alt.AddTransitions(elem, context, transitions, continuation, effects, simulateBacktracking);
                    }
                    break;

                case SymbolicRegexNodeKind.And:
                case SymbolicRegexNodeKind.Not:
                    Debug.Fail($"{nameof(AddTransitions)}:{_kind}");
                    break;
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
                    return _builder.OrderedOr(_left.ExtractNullabilityTest(), _right.ExtractNullabilityTest());
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
                    _left.ToString(sb);
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
    }
}
