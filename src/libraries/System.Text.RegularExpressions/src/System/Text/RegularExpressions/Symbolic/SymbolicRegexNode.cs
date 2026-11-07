// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Identifies an abstract syntax tree node in a <see cref="SymbolicRegexBuilder{TSet}"/> arena.</summary>
    internal readonly struct SymbolicRegexNode<TSet> : IEquatable<SymbolicRegexNode<TSet>>
        where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        private readonly int _id;

        internal SymbolicRegexNode(int id)
        {
            Debug.Assert(id > 0);
            _id = id;
        }

        internal bool IsNull => _id == 0;
        internal int Id => _id;

        public bool Equals(SymbolicRegexNode<TSet> other) => _id == other._id;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is SymbolicRegexNode<TSet> other && Equals(other);

        public override int GetHashCode() => _id;

        public static bool operator ==(SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right) => left.Equals(right);

        public static bool operator !=(SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right) => !left.Equals(right);

        internal SymbolicRegexNodeKind Kind(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).Kind;
        internal bool IsLazy(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsLazy;
        internal bool IsHighPriorityNullable(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsHighPriorityNullable;
        internal bool IsHighPriorityNullableFor(SymbolicRegexBuilder<TSet> builder, uint context) => builder.GetNodeContext(this).IsHighPriorityNullableFor(context);
        internal bool IsNullable(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsNullable;
        internal bool CanBeNullable(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).CanBeNullable;
        internal bool IsNullableFor(SymbolicRegexBuilder<TSet> builder, uint context) => builder.GetNodeContext(this).IsNullableFor(context);
        internal bool IsAnyStar(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsAnyStar(builder._solver);
        internal bool IsNothing(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsNothing(builder._solver);
        internal bool IsStar(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsStar;
        internal bool IsEpsilon(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsEpsilon;
        internal bool IsPlus(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).IsPlus;
        internal bool Subsumes(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> other, int depth = 0) => builder.GetNodeContext(this).Subsumes(builder, other, depth);

        internal List<SymbolicRegexNode<TSet>> ToList(SymbolicRegexBuilder<TSet> builder, List<SymbolicRegexNode<TSet>>? list = null, SymbolicRegexNodeKind listKind = SymbolicRegexNodeKind.Concat) =>
            builder.GetNodeContext(this).ToList(list, listKind);

        internal int GetFixedLength(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).GetFixedLength();
        internal SymbolicRegexNode<TSet> AddFixedLengthMarkers(SymbolicRegexBuilder<TSet> builder, int lengthSoFar = 0) => builder.GetNodeContext(this).AddFixedLengthMarkers(builder, lengthSoFar);
        internal SymbolicRegexNode<TSet> CreateDerivativeWithoutEffects(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context) => builder.GetNodeContext(this).CreateDerivativeWithoutEffects(builder, elem, context);
        internal SymbolicRegexNode<TSet> CreateDerivative(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context) => builder.GetNodeContext(this).CreateDerivative(builder, elem, context);
        internal SymbolicRegexNode<TSet> PruneLowerPriorityThanNullability(SymbolicRegexBuilder<TSet> builder, uint context) => builder.GetNodeContext(this).PruneLowerPriorityThanNullability(builder, context);
        internal SymbolicRegexNode<TSet> CreateLoopContinuation(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).CreateLoopContinuation(builder);
        internal List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> CreateNfaDerivativeWithEffects(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context) => builder.GetNodeContext(this).CreateNfaDerivativeWithEffects(builder, elem, context);
        internal SymbolicRegexNode<TSet> StripEffects(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).StripEffects(builder);
        internal void StripAndMapEffects(SymbolicRegexBuilder<TSet> builder, uint context, List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> alternativesAndEffects, List<DerivativeEffect>? currentEffects = null) =>
            builder.GetNodeContext(this).StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
        internal void ApplyEffects<TArg>(SymbolicRegexBuilder<TSet> builder, Action<DerivativeEffect, TArg> apply, uint context, TArg arg) =>
            builder.GetNodeContext(this).ApplyEffects(apply, context, arg);
        internal HashSet<TSet> GetSets(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).GetSets(builder);
        internal TSet[] ComputeMinterms(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).ComputeMinterms(builder);
        internal SymbolicRegexNode<TSet> Reverse(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).Reverse(builder);
        internal bool StartsWithLoop(SymbolicRegexBuilder<TSet> builder, int upperBoundLowestValue = 1) => builder.GetNodeContext(this).StartsWithLoop(upperBoundLowestValue);
        internal SymbolicRegexNode<TSet> PruneAnchors(SymbolicRegexBuilder<TSet> builder, uint prevKind) => builder.GetNodeContext(this).PruneAnchors(builder, prevKind);
        internal int ResolveFixedLength(SymbolicRegexBuilder<TSet> builder, uint context) => builder.GetNodeContext(this).ResolveFixedLength(context);

        /// <summary>Enumerates the ID-only branches of a top-level alternation.</summary>
        internal IEnumerable<SymbolicRegexNode<TSet>> EnumerateAlternationBranches(SymbolicRegexBuilder<TSet> builder)
        {
            SymbolicRegexBuilder<TSet>.NodeData[] nodes = builder.AcquireNodeDataArray(_id);
            switch (nodes[_id - 1].Kind)
            {
                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    int childId = nodes[_id - 1].LeftId;
                    Debug.Assert(childId != 0);
                    Debug.Assert(nodes[childId - 1].Kind is not SymbolicRegexNodeKind.DisableBacktrackingSimulation);
                    foreach (SymbolicRegexNode<TSet> element in new SymbolicRegexNode<TSet>(childId).EnumerateAlternationBranches(builder))
                    {
                        yield return builder.CreateDisableBacktrackingSimulation(element);
                    }
                    break;

                case SymbolicRegexNodeKind.Alternate:
                    int currentId = _id;
                    while (nodes[currentId - 1].Kind is SymbolicRegexNodeKind.Alternate)
                    {
                        int leftId = nodes[currentId - 1].LeftId;
                        int rightId = nodes[currentId - 1].RightId;
                        Debug.Assert(leftId != 0 && rightId != 0);
                        Debug.Assert(nodes[leftId - 1].Kind is not SymbolicRegexNodeKind.Alternate);
                        yield return new SymbolicRegexNode<TSet>(leftId);
                        currentId = rightId;
                    }
                    yield return new SymbolicRegexNode<TSet>(currentId);
                    break;

                default:
                    yield return this;
                    break;
            }
        }
        internal int EstimateNfaSize(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).EstimateNfaSize();
        internal int CountSingletons(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).CountSingletons();

#if DEBUG
        internal string ToString(SymbolicRegexBuilder<TSet> builder) => builder.GetNodeContext(this).ToString();
#endif

        internal static SymbolicRegexNode<TSet> CreateFalse(SymbolicRegexBuilder<TSet> builder) => SymbolicRegexNodeContext<TSet>.CreateFalse(builder);
        internal static SymbolicRegexNode<TSet> CreateTrue(SymbolicRegexBuilder<TSet> builder) => SymbolicRegexNodeContext<TSet>.CreateTrue(builder);
        internal static SymbolicRegexNode<TSet> CreateFixedLengthMarker(SymbolicRegexBuilder<TSet> builder, int length) => SymbolicRegexNodeContext<TSet>.CreateFixedLengthMarker(builder, length);
        internal static SymbolicRegexNode<TSet> CreateEpsilon(SymbolicRegexBuilder<TSet> builder) => SymbolicRegexNodeContext<TSet>.CreateEpsilon(builder);
        internal static SymbolicRegexNode<TSet> CreateAnchor(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind) => SymbolicRegexNodeContext<TSet>.CreateAnchor(builder, kind);
        internal static SymbolicRegexNode<TSet> CreateSingleton(SymbolicRegexBuilder<TSet> builder, TSet set) => SymbolicRegexNodeContext<TSet>.CreateSingleton(builder, set);
        internal static SymbolicRegexNode<TSet> CreateLoop(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> body, int lower, int upper, bool isLazy) => SymbolicRegexNodeContext<TSet>.CreateLoop(builder, body, lower, upper, isLazy);
        internal static SymbolicRegexNode<TSet> CreateEffect(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> node, SymbolicRegexNode<TSet> effectNode) => SymbolicRegexNodeContext<TSet>.CreateEffect(builder, node, effectNode);
        internal static SymbolicRegexNode<TSet> CreateCaptureStart(SymbolicRegexBuilder<TSet> builder, int captureNum) => SymbolicRegexNodeContext<TSet>.CreateCaptureStart(builder, captureNum);
        internal static SymbolicRegexNode<TSet> CreateCaptureEnd(SymbolicRegexBuilder<TSet> builder, int captureNum) => SymbolicRegexNodeContext<TSet>.CreateCaptureEnd(builder, captureNum);
        internal static SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> child) => SymbolicRegexNodeContext<TSet>.CreateDisableBacktrackingSimulation(builder, child);
        internal static SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right) => SymbolicRegexNodeContext<TSet>.CreateConcat(builder, left, right);
        internal static SymbolicRegexNode<TSet> CreateAlternate(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, bool deduplicated = false, bool hintRightLikelySubsumes = false) =>
            SymbolicRegexNodeContext<TSet>.CreateAlternate(builder, left, right, deduplicated, hintRightLikelySubsumes);
    }

    /// <summary>Provides stack-local access to a node and its owning builder.</summary>
    internal readonly struct SymbolicRegexNodeContext<TSet> : IEquatable<SymbolicRegexNodeContext<TSet>>
        where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        internal const string EmptyCharClass = "[]";
        /// <summary>Some byte other than 0 to represent true</summary>
        internal const byte TrueByte = 1;
        /// <summary>Some byte other than 0 to represent false</summary>
        internal const byte FalseByte = 2;
        /// <summary>The undefined value is the default value 0</summary>
        internal const byte UndefinedByte = 0;

        /// <summary>
        /// Maximum recursion depth for subsumption rules before giving up.
        /// </summary>
        /// <remarks>
        /// A regex R subsumes another regex S if all strings matching S are also matched by R.
        /// The subsumption check may do unproductive linear walks when subsumption doesn't hold. This depth limit helps
        /// curb the cost of these walks at the cost of sometimes not detecting subsumption. This limit should be set
        /// high enough that subsumption is detected in all typical cases. This limit of 50 still gives good performance
        /// in the stress tests.
        /// </remarks>
        internal const int SubsumptionCheckDepthLimit = 50;

        private readonly SymbolicRegexBuilder<TSet>? _builder;
        private readonly SymbolicRegexBuilder<TSet>.NodeData[]? _nodes;
        private readonly int _id;

        internal SymbolicRegexNodeContext(SymbolicRegexBuilder<TSet> builder, SymbolicRegexBuilder<TSet>.NodeData[] nodes, int id)
        {
            Debug.Assert(id > 0);
            _builder = builder;
            _nodes = nodes;
            _id = id;
        }

        internal ref readonly SymbolicRegexBuilder<TSet>.NodeData Data
        {
            get
            {
                Debug.Assert(_builder is not null && _nodes is not null && _id > 0);
                return ref _nodes[_id - 1];
            }
        }

        internal SymbolicRegexNodeKind _kind => Data.Kind;
        internal int _lower => Data.Lower;
        internal int _upper => Data.Upper;
        internal TSet _set => Data.Set;
        internal SymbolicRegexNodeContext<TSet> _left
        {
            get
            {
                return GetContext(Data.LeftId);
            }
        }

        internal SymbolicRegexNodeContext<TSet> _right
        {
            get
            {
                return GetContext(Data.RightId);
            }
        }

        internal SymbolicRegexInfo _info => Data.Info;

        /// <summary>
        /// Caches nullability of this node for any given context (0 &lt;= context &lt; ContextLimit)
        /// when _info.StartsWithSomeAnchor and _info.CanBeNullable are true. Otherwise the cache is null.
        /// </summary>
        private byte[]? _nullabilityCache => Data.NullabilityCache;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SymbolicRegexNodeContext<TSet> GetContext(int id) =>
            id == 0 ? default : new SymbolicRegexNodeContext<TSet>(_builder!, _nodes!, id);

        internal bool IsNull => _id == 0;
        internal int Id => _id;

        public static implicit operator SymbolicRegexNode<TSet>(SymbolicRegexNodeContext<TSet> context) =>
            context.IsNull ? default : new SymbolicRegexNode<TSet>(context._id);

        public static bool operator ==(SymbolicRegexNodeContext<TSet> left, SymbolicRegexNodeContext<TSet> right) => left._id == right._id;
        public static bool operator !=(SymbolicRegexNodeContext<TSet> left, SymbolicRegexNodeContext<TSet> right) => left._id != right._id;
        public static bool operator ==(SymbolicRegexNodeContext<TSet> left, SymbolicRegexNode<TSet> right) => left._id == right.Id;
        public static bool operator !=(SymbolicRegexNodeContext<TSet> left, SymbolicRegexNode<TSet> right) => left._id != right.Id;
        public static bool operator ==(SymbolicRegexNode<TSet> left, SymbolicRegexNodeContext<TSet> right) => left.Id == right._id;
        public static bool operator !=(SymbolicRegexNode<TSet> left, SymbolicRegexNodeContext<TSet> right) => left.Id != right._id;

        public bool Equals(SymbolicRegexNodeContext<TSet> other) => _id == other._id;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is SymbolicRegexNodeContext<TSet> other && Equals(other);

        public override int GetHashCode() => _id;

        /// <summary>Create a new node or retrieve one from the builder _nodeCache.</summary>
        private static SymbolicRegexNode<TSet> Create(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, int lower, int upper, TSet? set, SymbolicRegexInfo info)
        {
            Debug.Assert(kind != SymbolicRegexNodeKind.Singleton || set is not null);
            Debug.Assert(left.IsNull || left.Id <= builder.NodeCount);
            Debug.Assert(right.IsNull || right.Id <= builder.NodeCount);
            TSet setOrStartSet = kind == SymbolicRegexNodeKind.Singleton ? set! : ComputeStartSet(builder, kind, builder.GetNodeContext(left), builder.GetNodeContext(right));
            var key = new SymbolicRegexBuilder<TSet>.NodeCacheKey(kind, left.Id, right.Id, lower, upper, setOrStartSet, info);
            if (!builder._nodeCache.TryGetValue(key, out int id))
            {
                id = builder.AddNode(kind, left.Id, right.Id, lower, upper, setOrStartSet, info);
                builder._nodeCache[key] = id;
            }
            return builder.GetNode(id);
        }

        /// <summary>True if this node is lazy</summary>
        internal bool IsLazy => _info.IsLazyLoop;

        /// <summary>True if this node is high priority nullable</summary>
        internal bool IsHighPriorityNullable => _info.IsHighPriorityNullable;

        /// <summary>True if this node is high priority nullable for the given context</summary>
        internal bool IsHighPriorityNullableFor(uint context) => _info.CanBeNullable && IsHighPriorityNullableFor(this, context);

        /// <summary>Nullability test that determines if the node is high-priority-nullable for the given context</summary>
        private static bool IsHighPriorityNullableFor(SymbolicRegexNodeContext<TSet> node, uint context)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(IsHighPriorityNullableFor, node, context);
            }

            //Observe that this test is lightweight when node does not include any anchors,
            //because in that case if node._info.IsHighPriorityNullable=false this cannot change for any context
            //deep recursion can only occur for deep left-associative concatenations that is uncommon
            //in all common cases deep recursion is avoided by using a while loop
            while (true)
            {
                ref readonly SymbolicRegexBuilder<TSet>.NodeData data = ref node.Data;
                Debug.Assert(data.Info.CanBeNullable);

                if (data.Info.IsHighPriorityNullable || !data.Info.ContainsSomeAnchor)
                {
                    return data.Info.IsHighPriorityNullable;
                }

                switch (data.Kind)
                {
                    case SymbolicRegexNodeKind.Loop:
                        //only a lazy loop with lower bound 0 is high-priority-nullable
                        return data.Info.IsLazyLoop && data.Lower == 0;

                    case SymbolicRegexNodeKind.Concat:
                        {
                            SymbolicRegexNodeContext<TSet> left = node.GetContext(data.LeftId);
                            Debug.Assert(!left.IsNull && data.RightId != 0);
                            //both left and right must be high-priority-nullable
                            //observe that node.CanBeNullable implies that both node._left and node._right can be nullable
                            if (!IsHighPriorityNullableFor(left, context))
                            {
                                return false;
                            }
                            node = node.GetContext(data.RightId);
                            continue;
                        }

                    case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    case SymbolicRegexNodeKind.Effect:
                    case SymbolicRegexNodeKind.Alternate:
                        {
                            SymbolicRegexNodeContext<TSet> left = node.GetContext(data.LeftId);
                            Debug.Assert(!left.IsNull);
                            //the left alternative must be high-priority-nullable
                            //nullability of the right alternative does not matter
                            if (!left.Data.Info.CanBeNullable)
                            {
                                return false;
                            }
                            node = left;
                            continue;
                        }

                    default:
                        // any remaining case must be an anchor or else next._info.IsHighPriorityNullable would have been true
                        Debug.Assert(data.Kind is SymbolicRegexNodeKind.BeginningAnchor or
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

        /// <summary>
        /// Converts a list of a given kind, e.g. Concat or Alternate, into an array,
        /// returns anything else in a singleton array.
        /// </summary>
        /// <param name="list">a list to insert the elements into, or null to return results in a new list</param>
        /// <param name="listKind">kind of node to consider as the list builder</param>
        public List<SymbolicRegexNode<TSet>> ToList(List<SymbolicRegexNode<TSet>>? list = null, SymbolicRegexNodeKind listKind = SymbolicRegexNodeKind.Concat)
        {
            Debug.Assert(listKind is SymbolicRegexNodeKind.Concat or SymbolicRegexNodeKind.Alternate);
            list ??= [];
            AppendToList(this, list, listKind);
            return list;

            static void AppendToList(SymbolicRegexNodeContext<TSet> concat, List<SymbolicRegexNode<TSet>> list, SymbolicRegexNodeKind listKind)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(AppendToList, concat, list, listKind);
                    return;
                }

                SymbolicRegexNodeContext<TSet> node = concat;
                while (true)
                {
                    ref readonly SymbolicRegexBuilder<TSet>.NodeData data = ref node.Data;
                    if (data.Kind != listKind)
                    {
                        break;
                    }

                    SymbolicRegexNodeContext<TSet> left = node.GetContext(data.LeftId);
                    Debug.Assert(!left.IsNull && data.RightId != 0);
                    if (left.Data.Kind == listKind)
                    {
                        AppendToList(left, list, listKind);
                    }
                    else
                    {
                        list.Add(left);
                    }
                    node = node.GetContext(data.RightId);
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

            SymbolicRegexNodeContext<TSet> node = this;
            ref readonly SymbolicRegexBuilder<TSet>.NodeData data = ref node.Data;
            return data.NullabilityCache is null ?
                data.Info.IsNullable :
                WithCache(node, context);

            // Separated out to enable the common case (no nullability cache) to be inlined
            // and to avoid zero-init costs for generally unused state.
            static bool WithCache(SymbolicRegexNodeContext<TSet> node, uint context)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    return StackHelper.CallOnEmptyStack(node.IsNullableFor, context);
                }

                Debug.Assert(context < CharKind.ContextLimit);

                // If nullability has been computed for the given context then return it
                ref readonly SymbolicRegexBuilder<TSet>.NodeData data = ref node.Data;
                byte[] nullabilityCache = data.NullabilityCache!;
                byte b = Volatile.Read(ref nullabilityCache[context]);
                if (b != UndefinedByte)
                {
                    return b == TrueByte;
                }

                // Otherwise compute the nullability recursively for the given context
                bool is_nullable;
                switch (data.Kind)
                {
                    case SymbolicRegexNodeKind.Loop:
                        Debug.Assert(data.LeftId != 0);
                        is_nullable = data.Lower == 0 || node.GetContext(data.LeftId).IsNullableFor(context);
                        break;

                    case SymbolicRegexNodeKind.Concat:
                        Debug.Assert(data.LeftId != 0 && data.RightId != 0);
                        is_nullable = node.GetContext(data.LeftId).IsNullableFor(context) && node.GetContext(data.RightId).IsNullableFor(context);
                        break;

                    case SymbolicRegexNodeKind.Alternate:
                        Debug.Assert(data.LeftId != 0 && data.RightId != 0);
                        is_nullable = node.GetContext(data.LeftId).IsNullableFor(context) || node.GetContext(data.RightId).IsNullableFor(context);
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
                        Debug.Assert(data.LeftId != 0);
                        is_nullable = node.GetContext(data.LeftId).IsNullableFor(context);
                        break;

                    default:
                        // SymbolicRegexNodeKind.EndAnchorZReverse:
                        // EndAnchorZRev (rev(\Z)) anchor is nullable when the prev character is either the first Newline or Start
                        // note: CharKind.NewLineS == CharKind.Newline|CharKind.StartStop
                        Debug.Assert(data.Kind == SymbolicRegexNodeKind.EndAnchorZReverse);
                        is_nullable = (CharKind.Prev(context) & CharKind.BeginningEnd) != 0;
                        break;
                }

                Volatile.Write(ref nullabilityCache[context], is_nullable ? TrueByte : FalseByte);

                return is_nullable;
            }
        }

        /// <summary>Returns true if this is equivalent to .* (the node must be eager also)</summary>
        public bool IsAnyStar(ISolver<TSet> solver)
        {
            if (IsStar)
            {
                Debug.Assert(!_left.IsNull);
                if (_left._kind == SymbolicRegexNodeKind.Singleton)
                {
                    Debug.Assert(_left._set is not null);
                    return !IsLazy && solver.Full.Equals(_left._set);
                }
            }

            return false;
        }

        /// <summary>Returns true if this is equivalent to [0-[0]]</summary>
        public bool IsNothing(ISolver<TSet> solver)
        {
            if (_kind == SymbolicRegexNodeKind.Singleton)
            {
                Debug.Assert(_set is not null);
                return solver.IsEmpty(_set);
            }

            return false;
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
            Create(builder, SymbolicRegexNodeKind.Singleton, default, default, -1, -1, builder._solver.Empty, default);

        internal static SymbolicRegexNode<TSet> CreateTrue(SymbolicRegexBuilder<TSet> builder) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, default, default, -1, -1, builder._solver.Full, default);

        internal static SymbolicRegexNode<TSet> CreateFixedLengthMarker(SymbolicRegexBuilder<TSet> builder, int length) =>
            Create(builder, SymbolicRegexNodeKind.FixedLengthMarker, default, default, length, -1, default, SymbolicRegexInfo.Epsilon());

        internal static SymbolicRegexNode<TSet> CreateEpsilon(SymbolicRegexBuilder<TSet> builder) =>
            Create(builder, SymbolicRegexNodeKind.Epsilon, default, default, -1, -1, default, SymbolicRegexInfo.Epsilon());

        internal static SymbolicRegexNode<TSet> CreateAnchor(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind)
        {
            Debug.Assert(kind is
                SymbolicRegexNodeKind.BoundaryAnchor or SymbolicRegexNodeKind.NonBoundaryAnchor or
                SymbolicRegexNodeKind.BeginningAnchor or SymbolicRegexNodeKind.EndAnchor or
                SymbolicRegexNodeKind.EndAnchorZ or SymbolicRegexNodeKind.EndAnchorZReverse or
                SymbolicRegexNodeKind.EOLAnchor or SymbolicRegexNodeKind.BOLAnchor);
            return Create(
                builder, kind, default, default, -1, -1, default,
                SymbolicRegexInfo.Anchor(
                    isLineAnchor: kind is SymbolicRegexNodeKind.EndAnchorZ or SymbolicRegexNodeKind.EndAnchorZReverse or SymbolicRegexNodeKind.EOLAnchor or SymbolicRegexNodeKind.BOLAnchor,
                    isEndZAnchor: kind is SymbolicRegexNodeKind.EndAnchorZ));
        }

        #endregion

        internal static SymbolicRegexNode<TSet> CreateSingleton(SymbolicRegexBuilder<TSet> builder, TSet set) =>
            Create(builder, SymbolicRegexNodeKind.Singleton, default, default, -1, -1, set, default);

        internal static SymbolicRegexNode<TSet> CreateLoop(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> body, int lower, int upper, bool isLazy)
        {
            Debug.Assert(lower >= 0 && lower <= upper);
            SymbolicRegexNodeContext<TSet> bodyContext = builder.GetNodeContext(body);
            // Avoid wrapping X? inside (X?)?, or X?? inside (X??)??
            // This simplification in particular avoids creating unnecessary loops from the rule for concatenation in TryFoldAlternation
            if (lower == 0 && upper == 1 && bodyContext._kind == SymbolicRegexNodeKind.Loop && bodyContext._lower == 0 && bodyContext._upper == 1)
            {
                Debug.Assert(!bodyContext._left.IsNull);
                return CreateLoop(builder, bodyContext._left, 0, 1, isLazy || bodyContext.IsLazy);
            }
            return Create(builder, SymbolicRegexNodeKind.Loop, body, default, lower, upper, default, SymbolicRegexInfo.Loop(bodyContext._info, lower, isLazy));
        }

        internal static SymbolicRegexNode<TSet> CreateEffect(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> node, SymbolicRegexNode<TSet> effectNode)
        {
            if (effectNode == builder.Epsilon)
                return node;

            if (node == builder._nothing)
                return builder._nothing;

            SymbolicRegexNodeContext<TSet> nodeContext = builder.GetNodeContext(node);
            if (nodeContext._kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(!nodeContext._left.IsNull && !nodeContext._right.IsNull);
                return CreateEffect(builder, nodeContext._left, CreateConcat(builder, effectNode, nodeContext._right));
            }

            return Create(builder, SymbolicRegexNodeKind.Effect, node, effectNode, -1, -1, default, SymbolicRegexInfo.Effect(nodeContext._info));
        }

        internal static SymbolicRegexNode<TSet> CreateCaptureStart(SymbolicRegexBuilder<TSet> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureStart, default, default, captureNum, -1, default, SymbolicRegexInfo.Epsilon());

        internal static SymbolicRegexNode<TSet> CreateCaptureEnd(SymbolicRegexBuilder<TSet> builder, int captureNum) =>
            Create(builder, SymbolicRegexNodeKind.CaptureEnd, default, default, captureNum, -1, default, SymbolicRegexInfo.Epsilon());

        internal static SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> child) =>
            Create(builder, SymbolicRegexNodeKind.DisableBacktrackingSimulation, child, default, -1, -1, default, builder.GetNodeContext(child)._info);

        /// <summary>Make a concatenation of the supplied regex nodes.</summary>
        internal static SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right)
        {
            // Concatenating anything with a nothing means the entire concatenation can't match
            if (left == builder._nothing || right == builder._nothing)
                return builder._nothing;

            // If the left or right is empty, just return the other.
            SymbolicRegexNodeContext<TSet> leftContext = builder.GetNodeContext(left);
            SymbolicRegexNodeContext<TSet> rightContext = builder.GetNodeContext(right);

            if (leftContext.IsEpsilon)
                return right;
            if (rightContext.IsEpsilon)
                return left;

            // Push concatenation inside Effect nodes
            Debug.Assert(rightContext._kind is not SymbolicRegexNodeKind.Effect);
            if (leftContext._kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(!leftContext._left.IsNull && !leftContext._right.IsNull);
                return CreateEffect(builder, CreateConcat(builder, leftContext._left, right), leftContext._right);
            }

            return Create(builder, SymbolicRegexNodeKind.Concat, left, right, -1, -1, default, SymbolicRegexInfo.Concat(leftContext._info, rightContext._info));
        }

        /// <summary>
        /// Make an alternation of given regexes, eliminate nothing regexes and treat .* as consuming element.
        /// Keep the alternation flat, assuming both right and left are flat.
        /// Apply subsumption/combining optimizations, such that e.g. a?b|b will be simplified to a?b and b|a?b will be combined to a??b
        /// </summary>
        /// <remarks>
        /// The <paramref name="deduplicated"/> argument allows skipping deduplication when it is known to be not necessary. This is
        /// commonly the case when some transformation f is applied to an existing alternation A|B|C|... such that
        /// for all regexes R,S it is the case that f(R)==f(S) iff R==S.
        /// </remarks>
        /// <param name="builder">the builder for the nodes</param>
        /// <param name="left">the left hand side, higher priority alternative</param>
        /// <param name="right">the right hand side, lower priority alternative</param>
        /// <param name="deduplicated">whether to skip deduplication</param>
        /// <param name="hintRightLikelySubsumes">if true then simplification rules succeeding when the right hand side subsumes the left hand side are tried first</param>
        /// <returns></returns>
        internal static SymbolicRegexNode<TSet> CreateAlternate(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, bool deduplicated = false, bool hintRightLikelySubsumes = false)
        {
            SymbolicRegexNodeContext<TSet> leftContext = builder.GetNodeContext(left);
            SymbolicRegexNodeContext<TSet> rightContext = builder.GetNodeContext(right);
            if (leftContext.IsAnyStar(builder._solver) || rightContext.IsNothing(builder._solver) || left == right || (leftContext.IsNullable && rightContext.IsEpsilon))
                return left;
            if (left == builder._nothing)
                return right;

            // Handle cases where right is an alternation or not uniformly. If right is R|S then the head is R and the
            // tail is S. If right is not an alternation then the head is right and the tail is nothing.
            SymbolicRegexNode<TSet> head = rightContext._kind == SymbolicRegexNodeKind.Alternate ? rightContext._left : right;
            SymbolicRegexNode<TSet> tail = rightContext._kind == SymbolicRegexNodeKind.Alternate ? rightContext._right : builder._nothing;

            // Simplify away right side if left side subsumes it. For example X?Y|Y|Z would simplify to just X?Y|Z.
            if (!hintRightLikelySubsumes && left.Subsumes(builder, head))
                return CreateAlternate(builder, left, tail);

            // Simplify by folding right side into left side if right side subsumes the left side. For example Y|X?Y|Z
            // would simplify to X??Y|Z.
            if (head.Subsumes(builder, left) && TryFoldAlternation(builder, left, head, out SymbolicRegexNode<TSet> result))
                return CreateAlternate(builder, result, tail);

            // This is a repeat of a rule above, but for the case when the hint tells us to try reverse subsumption first.
            if (hintRightLikelySubsumes && left.Subsumes(builder, head))
                return CreateAlternate(builder, left, tail);

            // If left is not an Alternate, try to avoid allocation by checking if deduplication is necessary
            if (!deduplicated && leftContext._kind != SymbolicRegexNodeKind.Alternate)
            {
                SymbolicRegexNodeContext<TSet> current = rightContext;
                // Initially assume there are no duplicates
                deduplicated = true;
                while (current._kind == SymbolicRegexNodeKind.Alternate)
                {
                    Debug.Assert(!current._left.IsNull && !current._right.IsNull);
                    // All Alternates are supposed to be in a right associative normal form
                    Debug.Assert(current._left._kind != SymbolicRegexNodeKind.Alternate);
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

            if (!deduplicated || leftContext._kind == SymbolicRegexNodeKind.Alternate)
            {
                // If the left side was an or, then it has to be flattened, gather the elements from both sides
                List<SymbolicRegexNode<TSet>> elems = leftContext.ToList(listKind: SymbolicRegexNodeKind.Alternate);
                int firstRightElem = elems.Count;
                rightContext.ToList(elems, listKind: SymbolicRegexNodeKind.Alternate);

                // Eliminate any duplicate elements, keeping the leftmost element
                // Keep track of if any elements from the right side need to be eliminated
                HashSet<SymbolicRegexNode<TSet>> seenElems = [];
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
                        or = CreateAlternate(builder, elems[i], or, deduplicated: true);
                    }
                    return or;
                }
                else
                {
                    SymbolicRegexNode<TSet> or = right;
                    for (int i = firstRightElem - 1; i >= 0; i--)
                    {
                        or = CreateAlternate(builder, elems[i], or, deduplicated: true);
                    }
                    return or;
                }
            }

            Debug.Assert(leftContext._kind != SymbolicRegexNodeKind.Alternate);
            Debug.Assert(deduplicated);

            return Create(builder, SymbolicRegexNodeKind.Alternate, left, right, -1, -1, default, SymbolicRegexInfo.Alternate(leftContext._info, rightContext._info));
        }

        /// <summary>
        /// Tries to detect whether or not the language of another node is fully contained within the language of this
        /// node. It does this by applying a set of rules, such as "RS subsumes T if R is nullable and S subsumes T",
        /// which peels off one nullable element from a concatenation and recurses into another subsumption check.
        /// Note that differences in Effect nodes are not considered for subsumption, which is an important feature since
        /// this allows simplifications relying on subsumption to apply in the presence of effects.
        /// </summary>
        /// <remarks>
        /// Subsumption checks for regular expressions are in general difficult (equivalence and thus subsumption are
        /// known to be PSPACE-complete). Thus this fast rule based check will never be complete. Adding rules into this
        /// function should be directed by concrete use cases.
        /// Some rules may be unproductive (e.g. the rule for R?S subsuming T when T is not a suffix of S), which would
        /// result in deep recursions. Rule application depth is limited by <see cref="SubsumptionCheckDepthLimit"/> to
        /// avoid these cases.
        /// The function uses a caching approach where the boolean returned is only cached if it is the result of a
        /// recursive subsumption check. The rationale is that if the answer could be produced locally then recomputing
        /// it is better than caching.
        /// </remarks>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="other">the node to check for being subsumed</param>
        /// <param name="depth">the current recursion depth</param>
        /// <returns></returns>
        internal bool Subsumes(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> other, int depth = 0)
        {
            // A node subsumes itself
            if (this == other)
                return true;

            // Nothing has an empty language, which is subsumed by anything
            if (other.IsNothing(builder))
                return true;

            // Early exit if we've gone too deep
            if (depth >= SubsumptionCheckDepthLimit)
                return false;

            if (builder._subsumptionCache.TryGetValue((_id, other.Id), out bool cached))
            {
                return cached;
            }

            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Subsumes, builder, other, depth);
            }

            // Try to apply all subsumption rules
            bool? subsumes = ApplySubsumptionRules(builder, this, other, depth + 1);

            // Cache and return the result if any rule applied
            if (subsumes.HasValue)
            {
                return (builder._subsumptionCache[(_id, other.Id)] = subsumes.Value);
            }

            // Assume false if no rule applied
            return false;

            static bool? ApplySubsumptionRules(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeContext<TSet> left, SymbolicRegexNode<TSet> rightNode, int depth)
            {
                SymbolicRegexNodeContext<TSet> right = builder.GetNodeContext(rightNode);
                // Rule: Effect(X,E) subsumes Y iff X subsumes Y
                // Effectively this ignores any effects
                if (left._kind == SymbolicRegexNodeKind.Effect)
                {
                    Debug.Assert(!left._left.IsNull && !left._right.IsNull);
                    return left._left.Subsumes(builder, right, depth);
                }

                // Rule: X subsumes Effect(Y,E) iff X subsumes Y
                // Effectively this ignores any effects
                if (right._kind == SymbolicRegexNodeKind.Effect)
                {
                    Debug.Assert(!right._left.IsNull && !right._right.IsNull);
                    return left.Subsumes(builder, right._left, depth);
                }

                // Rule: XY subsumes (X')??Y' if X equals X' and Y subsumes Y'
                // This structure arises from a folding rule in TryFold
                if (left._kind == SymbolicRegexNodeKind.Concat && right._kind == SymbolicRegexNodeKind.Concat)
                {
                    Debug.Assert(!left._left.IsNull && !right._left.IsNull && !right._right.IsNull);
                    SymbolicRegexNodeContext<TSet> rl = right._left;
                    if (left._left.IsNullable && rl._kind == SymbolicRegexNodeKind.Loop && rl._lower == 0 && rl._upper == 1 && rl.IsLazy)
                    {
                        Debug.Assert(!rl._left.IsNull);
                        if (TrySkipPrefix(left, rl._left, out SymbolicRegexNode<TSet> tail))
                            return tail.Subsumes(builder, right._right, depth);
                    }
                }

                // Rule: (X)??Y subsumes X'Y' if X equals X' and Y subsumes Y'
                // This structure arises from a folding rule in TryFold
                if (left._kind == SymbolicRegexNodeKind.Concat && right._kind == SymbolicRegexNodeKind.Concat)
                {
                    Debug.Assert(!left._left.IsNull && !left._right.IsNull);
                    SymbolicRegexNodeContext<TSet> ll = left._left;
                    if (ll._kind == SymbolicRegexNodeKind.Loop && ll._lower == 0 && ll._upper == 1 && ll.IsLazy)
                    {
                        Debug.Assert(!ll._left.IsNull);
                        if (TrySkipPrefix(right, ll._left, out SymbolicRegexNode<TSet> tail))
                            return left._right.Subsumes(builder, tail, depth);
                    }
                }

                // Rule: XY subsumes Y' if X is nullable and Y subsumes Y'
                if (left._kind == SymbolicRegexNodeKind.Concat)
                {
                    Debug.Assert(!left._left.IsNull && !left._right.IsNull);
                    if (left._left.IsNullable)
                    {
                        return left._right.Subsumes(builder, right, depth);
                    }
                }

                return null;

                // Given a candidate prefix P and another regex R, tries to skip over P in R to find a split R=PT.
                // If this succeeds T is returned in tail.
                // Note that this method currently only succeeds when both node and prefix are in right associative
                // form (if they are concats).
                static bool TrySkipPrefix(SymbolicRegexNodeContext<TSet> node, SymbolicRegexNodeContext<TSet> prefix, out SymbolicRegexNode<TSet> tail)
                {
                    tail = default;
                    // Walk over the prefix and the node in lockstep
                    while (prefix._kind == SymbolicRegexNodeKind.Concat)
                    {
                        Debug.Assert(!prefix._left.IsNull && !prefix._right.IsNull);
                        if (node._kind != SymbolicRegexNodeKind.Concat)
                            return false;

                        Debug.Assert(!node._left.IsNull && !node._right.IsNull);
                        if (node._left != prefix._left)
                            return false;

                        node = node._right;
                        prefix = prefix._right;
                    }

                    // Handle the final element
                    if (node._kind != SymbolicRegexNodeKind.Concat)
                        return false;

                    Debug.Assert(!node._left.IsNull && !node._right.IsNull);
                    if (node._left == prefix)
                    {
                        tail = node._right;
                        return true;
                    }

                    return false;
                }
            }
        }

        /// <summary>
        /// Unwraps Effect nodes by walking into their left children until a non-Effect node is found.
        /// </summary>
        /// <returns>the first non-Effect node</returns>
        private SymbolicRegexNode<TSet> UnwrapEffects()
        {
            SymbolicRegexNodeContext<TSet> current = this;
            while (current._kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(!current._left.IsNull);
                current = current._left;
            }
            return current;
        }

        /// <summary>
        /// Tries to combine the lower priority right hand side with the higher priority left hand side in an alternation
        /// when the right hand side is known to subsume the left hand side.
        /// For example in abc|(xyz){0,3}abc the right hand side subsumes the left hand side. This function can be used to
        /// eliminate the alternation by simplifying to (xyz){0,3}?abc. Note that the transformation preserves the priority
        /// of the shorter "abc" match by making the prefix lazy.
        /// </summary>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="left">the lower priority alternative</param>
        /// <param name="right">the higher priority alternative</param>
        /// <param name="result">the folded regex that eliminates alternation, or null if the operation fails</param>
        /// <param name="rightEffects">accumulated effects from the right side</param>
        /// <returns>whether folding was successful</returns>
        private static bool TryFoldAlternation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, out SymbolicRegexNode<TSet> result,
            SymbolicRegexNode<TSet> rightEffects = default)
        {
            SymbolicRegexNodeContext<TSet> leftContext = builder.GetNodeContext(left);
            SymbolicRegexNodeContext<TSet> rightContext = builder.GetNodeContext(right);

            // The rules below assume that the right side subsumes the left side
            Debug.Assert(right.Subsumes(builder, left));

            if (rightEffects.IsNull)
            {
                rightEffects = builder.Epsilon;
            }

            // If the sides are equal (ignoring effects) then just return the higher priority left side
            if (leftContext.UnwrapEffects() == rightContext.UnwrapEffects())
            {
                result = left;
                return true;
            }

            // If the left side has an effect, then the folding proceeds into the actual child and the effect
            // is kept on the top level.
            // For example, Effect(Y,E) | X?Y folds into Effect(X??Y,E)
            if (leftContext._kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(!leftContext._left.IsNull && !leftContext._right.IsNull);
                Debug.Assert(right.Subsumes(builder, leftContext._left));

                // If there are any accumulated effects we don't know how to handle them here.
                // This shouldn't normally happen because this rule has priority over the rule
                // for effects on the right side.
                if (rightEffects != builder.Epsilon)
                {
                    result = default;
                    return false;
                }

                if (TryFoldAlternation(builder, leftContext._left, right, out SymbolicRegexNode<TSet> innerResult, rightEffects))
                {
                    result = CreateEffect(builder, innerResult, leftContext._right);
                    return true;
                }
            }

            // If the right side has an effect, then it has to be pushed into the optional prefix.
            // For example, Y | Effect(X?Y,E) folds into Effect(X?,E)??Y
            if (rightContext._kind == SymbolicRegexNodeKind.Effect)
            {
                Debug.Assert(!rightContext._left.IsNull && !rightContext._right.IsNull);
                Debug.Assert(rightContext._left.Subsumes(builder, left));
                rightEffects = CreateConcat(builder, rightContext._right, rightEffects);
                return TryFoldAlternation(builder, left, rightContext._left, out result, rightEffects);
            }

            // If we have Y | XY then this rule will find X and fold to X??Y.
            if (rightContext._kind == SymbolicRegexNodeKind.Concat)
            {
                Debug.Assert(!rightContext._left.IsNull && !rightContext._right.IsNull);
                if (rightContext._left.IsNullable && TrySplitConcatSubsumption(builder, left, rightContext, out SymbolicRegexNode<TSet> prefix))
                {
                    prefix = CreateEffect(builder, prefix, rightEffects);
                    result = builder.CreateConcat(CreateLoop(builder, prefix, 0, 1, true), left);
                    return true;
                }
            }

            // If no rule matched then return false for failure
            result = default;
            return false;

            // This rule tries to find a prefix P that the right side has such that right is PR and left is equivalent to R
            static bool TrySplitConcatSubsumption(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNodeContext<TSet> right,
                out SymbolicRegexNode<TSet> prefix)
            {
                List<SymbolicRegexNode<TSet>> prefixElements = [];
                SymbolicRegexNodeContext<TSet> suffix = right;
                while (suffix._kind == SymbolicRegexNodeKind.Concat)
                {
                    Debug.Assert(!suffix._left.IsNull && !suffix._right.IsNull);
                    // We maintain a loop invariant that the suffix subsumes the left hand side
                    Debug.Assert(suffix.Subsumes(builder, left));
                    if (suffix == left)
                    {
                        // We found a split, so store the prefix and return success
                        prefixElements.Reverse();
                        prefix = builder.CreateConcatAlreadyReversed(prefixElements);
                        return true;
                    }
                    else if (suffix._right.Subsumes(builder, left))
                    {
                        // The tail of the suffix still subsumes left, so we can extend the prefix
                        prefixElements.Add(suffix._left);
                        suffix = suffix._right;
                    }
                    else if (left.Subsumes(builder, suffix))
                    {
                        // If left subsumes the suffix, then due to the loop invariant we have equivalence
                        prefixElements.Reverse();
                        prefix = builder.CreateConcatAlreadyReversed(prefixElements);
                        return true;
                    }
                    else
                    {
                        break;
                    }
                }

                // Return false if we failed to find a split
                prefix = default;
                return false;
            }
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
                        Debug.Assert(!_left.IsNull);
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
                        Debug.Assert(!_left.IsNull && !_right.IsNull);
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

                case SymbolicRegexNodeKind.Alternate:
                    {
                        Debug.Assert(!_left.IsNull && !_right.IsNull);
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
                    Debug.Assert(!_left.IsNull);
                    return _left.GetFixedLength();
            }

            return -1;
        }

        /// <summary>
        /// Insert <see cref="SymbolicRegexNodeKind.FixedLengthMarker"/> nodes to mark paths in the regex that correspond
        /// to matches of fixed length. For example, for abar|bar two markers would be added abar(4)|bar(3).
        /// </summary>
        /// <remarks>
        /// This function will rebuild concatenations because it pushes the FixedLengthMarker into the rightmost element.
        /// Due to this function should not be called on every character.
        /// </remarks>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="lengthSoFar">accumulator used in the recursion for lengths of paths</param>
        /// <returns>the node with fixed length markers added</returns>
        public SymbolicRegexNode<TSet> AddFixedLengthMarkers(SymbolicRegexBuilder<TSet> builder, int lengthSoFar = 0)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return this;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    // For an Alternate attempt to add markers separately for each element
                    return CreateAlternate(builder,
                        _left.AddFixedLengthMarkers(builder, lengthSoFar),
                        _right.AddFixedLengthMarkers(builder, lengthSoFar), deduplicated: true);

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    // For a concat if the left side has a fixed length then accumulate that to the right side
                    int leftLength = _left.GetFixedLength();
                    if (leftLength >= 0)
                    {
                        return CreateConcat(builder, _left, _right.AddFixedLengthMarkers(builder, lengthSoFar + leftLength));
                    }
                    // If the right side is always zero length, then just recurse to the left side
                    int rightLength = _right.GetFixedLength();
                    if (rightLength == 0)
                    {
                        return CreateConcat(builder, _left.AddFixedLengthMarkers(builder, lengthSoFar), _right);
                    }
                    break;

                case SymbolicRegexNodeKind.FixedLengthMarker:
                    Debug.Assert(_lower == lengthSoFar);
                    return this;
            }

            // For all other nodes defer to GetFixedLength to figure out if there is a fixed length and add the marker
            // if there is one.
            int thisLength = GetFixedLength();
            return thisLength < 0 ? this :
                CreateConcat(builder, this, CreateFixedLengthMarker(builder, lengthSoFar + thisLength));
        }

        /// <summary>
        /// Create a derivative (<see cref="CreateDerivative(SymbolicRegexBuilder{TSet}, TSet, uint)"/> and <see cref="CreateDerivativeWrapper"/>) and then strip
        /// effects with <see cref="StripEffects"/>.
        /// This derivative simulates backtracking, i.e. it only considers paths that backtracking would
        /// take before accepting the empty string for this pattern and returns the pattern ordered in the order backtracking
        /// would explore paths. For example the derivative of a*ab places a*ab before b, while for a*?ab the order is reversed.
        /// </summary>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <returns>the derivative</returns>
        internal SymbolicRegexNode<TSet> CreateDerivativeWithoutEffects(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context) => CreateDerivativeWrapper(builder, elem, context).StripEffects(builder);

        /// <summary>
        /// Create a derivative (<see cref="CreateDerivative(SymbolicRegexBuilder{TSet}, TSet, uint)"/> and <see cref="CreateDerivativeWrapper"/>) and then strip
        /// and map effects for use in NFA simulation with <see cref="StripAndMapEffects"/>.
        /// This derivative simulates backtracking, i.e. it only considers paths that backtracking would
        /// take before accepting the empty string for this pattern and returns the pattern ordered in the order backtracking
        /// would explore paths. For example the derivative of a*ab places a*ab before b, while for a*?ab the order is reversed.
        /// </summary>
        /// <remarks>
        /// The differences of this to <see cref="CreateDerivativeWithoutEffects(SymbolicRegexBuilder{TSet}, TSet, uint)"/> are that (1) effects (e.g. capture starts and ends)
        /// are considered and (2) the different elements that would form a top level union are instead returned as separate
        /// nodes (paired with their associated effects). This function is meant to be used for NFA simulation, where top level
        /// unions would be broken up into separate states.
        /// </remarks>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <returns>the derivative</returns>
        internal List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> CreateNfaDerivativeWithEffects(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context)
        {
            List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> transitions = [];
            CreateDerivativeWrapper(builder, elem, context).StripAndMapEffects(builder, context, transitions);
            return transitions;
        }

        // This wrapper handles the shared top-level concerns of constructing derivatives. Namely:
        // -Unwrapping and rewrapping nodes in DisableBacktrackingSimulation
        // -When backtracking is being simulated calling into PruneLowerPriorityThanNullability
        private SymbolicRegexNode<TSet> CreateDerivativeWrapper(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context)
        {
            if (_kind == SymbolicRegexNodeKind.DisableBacktrackingSimulation)
            {
                // This node kind can only occur at the top level and indicates that backtracking simulation is turned off
                Debug.Assert(!_left.IsNull);
                SymbolicRegexNode<TSet> derivative = _left.CreateDerivative(builder, elem, context);
                // Reinsert the marker that maintains the non-backtracking semantics
                return builder.CreateDisableBacktrackingSimulation(derivative);
            }
            else
            {
                // To maintain backtracking semantics, prune any branches that are less preferred than just the empty match
                SymbolicRegexNode<TSet> node = PruneLowerPriorityThanNullability(builder, context);
                return node.CreateDerivative(builder, elem, context);
            }
        }

        /// <summary>Prune this node wrt the given context in order to maintain backtracking semantics. Mimics how backtracking chooses a path.</summary>
        internal SymbolicRegexNode<TSet> PruneLowerPriorityThanNullability(SymbolicRegexBuilder<TSet> builder, uint context)
        {
            if (!IsNullableFor(context))
                return this;

            // Cache result to avoid otherwise potential quadratic worst case behavior
            (int, uint) key = (_id, context);
            if (builder._pruneLowerPriorityThanNullabilityCache.TryGetValue(key, out int prunedNodeId))
            {
                return builder.GetNode(prunedNodeId);
            }

            SymbolicRegexNode<TSet> prunedNode;
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(PruneLowerPriorityThanNullability, builder, context);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    // The left alternative, when nullable, has priority over the right alternative
                    // Otherwise the left alternative is still active and the right alternative is pruned
                    // In a alternation (X|Y) where X is nullable (in the given context), Y must be eliminated.
                    // Thus, taking the higher-priority branch in backtracking that is known to lead to a match
                    // at which point the other branches become irrelevant and must no longer be used.
                    prunedNode = _left.IsNullableFor(context) ? _left.PruneLowerPriorityThanNullability(builder, context) :
                        CreateAlternate(builder, _left, _right.PruneLowerPriorityThanNullability(builder, context), deduplicated: true);
                    break;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    prunedNode = _left._kind switch
                    {
                        // If the left side is a concatenation, then we bring it to right associative form to expose
                        // the first element of the concatenation for the cases below.
                        SymbolicRegexNodeKind.Concat => CreateConcat(builder, _left._left!, CreateConcat(builder, _left._right!, _right))
                            .PruneLowerPriorityThanNullability(builder, context),
                        // In a concatenation (X|Y)Z priority is given to XZ when X is nullable.
                        // Observe that (X|Y)Z is equivalent to (XZ|YZ) and the branch YZ must be ignored
                        // when X is nullable (observe that XZ is nullable because this=(X|Y)Z is nullable).
                        // If X is not nullable then XZ is maintaned as is, and YZ is pruned. For example,
                        // (a{0,5}?|abc|b+)c* reduces to c*.
                        SymbolicRegexNodeKind.Alternate => (_left._left!.IsNullableFor(context) ?
                            CreateConcat(builder, _left._left, _right).PruneLowerPriorityThanNullability(builder, context) :
                            CreateAlternate(builder, CreateConcat(builder, _left._left, _right),
                                CreateConcat(builder, _left._right!, _right).PruneLowerPriorityThanNullability(builder, context), deduplicated: true)),
                        // Loops have various cases that are handled uniformly for concatenations and standalone loops.
                        SymbolicRegexNodeKind.Loop => PruneLoop(builder, context, _left, _right),
                        // The previous cases handle all the ways that the left side of the concatenation could
                        // contain branching. Thus if we get here it is safe to only prune the right side.
                        _ => CreateConcat(builder, _left, _right.PruneLowerPriorityThanNullability(builder, context)),
                    };
                    break;

                case SymbolicRegexNodeKind.Loop:
                    // Loops have various cases that are handled uniformly for standalone loops and concatenations.
                    prunedNode = PruneLoop(builder, context, this, builder.Epsilon);
                    break;

                case SymbolicRegexNodeKind.Effect:
                    // Effects are maintained and the pruning is propagated to the body of the effect
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    prunedNode = CreateEffect(builder, _left.PruneLowerPriorityThanNullability(builder, context), _right);
                    break;

                default:
                    // In all other remaining cases no pruning takes place
                    prunedNode = this;
                    break;
            }

            builder._pruneLowerPriorityThanNullabilityCache[key] = prunedNode.Id;
            return prunedNode;

            static SymbolicRegexNode<TSet> PruneLoop(SymbolicRegexBuilder<TSet> builder, uint context, SymbolicRegexNodeContext<TSet> loop, SymbolicRegexNode<TSet> tail)
            {
                Debug.Assert(loop.Kind == SymbolicRegexNodeKind.Loop && !loop._left.IsNull);

                if (loop._lower == 0)
                {
                    // Loop is nullable at least due to a zero lower bound and the cases below handle the different
                    // priorities of checking that lower bound.
                    if (loop.IsLazy)
                    {
                        // In a lazy loop nullability from the zero lower bound has highest priority, so the loop is skipped completely
                        return tail.PruneLowerPriorityThanNullability(builder, context);
                    }
                    else if (!loop._left.IsNullableFor(context))
                    {
                        // For an eager loop with a non-nullable body, the nullability from the zero lower bound has lowest priority.
                        // Handle by case splitting into (1) doing at least one iteration in the loop and (2) skipping the loop and
                        // continuing directly into the tail, which then must be pruned in the current context.
                        return CreateAlternate(builder,
                            CreateConcat(builder, CreateLoop(builder, loop._left, 1, loop._upper, loop.IsLazy), tail),
                            tail.PruneLowerPriorityThanNullability(builder, context));
                    }
                    else if (loop._upper == int.MaxValue)
                    {
                        // The special case of a R*S loop with a nullable body is handled separately, as the general case handler could cause
                        // infinite recursion. The paths that backtracking would explore before stopping here are (1) consuming a character in the
                        // first iteration of the loop and (2) skipping the loop and continuing with anything higher priority than nullability in S.
                        // For example, a pattern (a|b??)*c? would prune into essentially a?(a|b??)*c?|c?. Note that pruning only one peeled-out
                        // iteration of the loop is necessary, since anchors could change the priority of nullability in other locations in the input.
                        // Additionally, a high-priority nullable body will always be skipped, in which case only option (2) is included. Finally, in
                        // all cases the loop body must not be dropped, as doing so could affect whether some capture groups match zero characters or
                        // don't match at all.
                        SymbolicRegexNode<TSet> skipLoopCase = CreateConcat(builder, loop._left.PruneLowerPriorityThanNullability(builder, context),
                                tail.PruneLowerPriorityThanNullability(builder, context));
                        return loop._left.IsHighPriorityNullableFor(context) ? skipLoopCase : CreateAlternate(builder,
                            CreateConcat(builder, loop._left.PruneLowerPriorityThanNullability(builder, context), CreateConcat(builder, loop, tail)),
                            skipLoopCase);
                    }
                    // For an eager loop with finite upper bound and nullable body fall back to the general case handler
                }

                Debug.Assert(loop._left.IsNullableFor(context));
                // The general case handler peels one iteration out of the loop and prunes the resulting concatenation
                return CreateConcat(builder, loop._left, CreateConcat(builder, loop.CreateLoopContinuation(builder), tail))
                    .PruneLowerPriorityThanNullability(builder, context);
            }
        }

        /// <summary>
        /// Creates the remainder of a loop when one iteration is peeled out of it. In other words, for a loop R with
        /// a body B returns a new regex S such that R is equivalent to BS.
        /// </summary>
        /// <param name="builder">the builder that owns this node</param>
        /// <returns>the loop continuation regex</returns>
        internal SymbolicRegexNode<TSet> CreateLoopContinuation(SymbolicRegexBuilder<TSet> builder)
        {
            Debug.Assert(_kind == SymbolicRegexNodeKind.Loop && !_left.IsNull);

            // Note that the upper bound is guaranteed to be greater than zero, since otherwise the loop would have
            // been simplified to nothing, and int.MaxValue is treated as infinity.
            int newupper = _upper == int.MaxValue ? int.MaxValue : _upper - 1;
            // Do not decrement the lower bound if it equals int.MaxValue
            int newlower = _lower is 0 or int.MaxValue ? _lower : _lower - 1;

            // The continued loop becomes epsilon when newlower == newupper == 0
            return builder.CreateLoop(_left, IsLazy, newlower, newupper);
        }

        /// <summary>
        /// Takes the derivative of the symbolic regex for the given element, which must be either
        /// a minterm (i.e. a class of characters that have identical behavior for all sets in the pattern)
        /// or a singleton set, and a context, which encodes the relevant information about the surrounding
        /// characters for deciding the nullability of anchors.
        /// </summary>
        /// <remarks>
        /// This derivative differs from ones familiar from literature in several ways:
        /// -Ordered alternations are properly handled and any choices represented as alternations are ordered in a way
        ///  that matches the backtracking engines.
        /// -Some nodes, namely <see cref="SymbolicRegexNodeKind.CaptureStart"/> and <see cref="SymbolicRegexNodeKind.CaptureEnd"/>,
        ///  will produce <see cref="SymbolicRegexNodeKind.Effect"/> nodes, which indicate effects to be applied on
        ///  transitions using this derivative. The derivative must be further post-processed with a function that strips
        ///  and possibly interprets these effects into a conveniently executable form. See <see cref="StripAndMapEffects"/>
        ///  for an example of this interpretation.
        ///  Ultimately, effects are translated into <see cref="DerivativeEffect"/> instances which may be applied at a
        ///  specific input position to <see cref="SymbolicRegexMatcher{TSet}.Registers"/> instances, which track concrete
        ///  positions for capture starts and ends. For example, given a DerivativeEffect for CaptureStart of capture number 0
        ///  and an input position 5, applying it to a Registers instance is simply assigning the relevant value to 5.
        /// </remarks>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <returns>the derivative</returns>
        internal SymbolicRegexNode<TSet> CreateDerivative(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(CreateDerivative, builder, elem, context);
            }

            (int, TSet, uint) key = (_id, elem, context);
            if (builder._derivativeCache.TryGetValue(key, out int derivativeId))
            {
                return builder.GetNode(derivativeId);
            }

            ref readonly SymbolicRegexBuilder<TSet>.NodeData data = ref Data;
            SymbolicRegexNode<TSet> derivative = default;
            switch (data.Kind)
            {
                case SymbolicRegexNodeKind.Singleton:
                    {
                        Debug.Assert(data.Set is not null);
                        // The following check assumes that either (1) the element and set are minterms, in which case
                        // the element is exactly the set if the intersection is non-empty (satisfiable), or (2) the element is a singleton
                        // set in which case it is fully contained in the set if the intersection is non-empty.
                        if (!builder._solver.IsEmpty(builder._solver.And(elem, data.Set)))
                        {
                            // the singleton is consumed so the derivative is epsilon
                            derivative = builder.Epsilon;
                        }
                        else
                        {
                            derivative = builder._nothing;
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Concat:
                    {
                        SymbolicRegexNodeContext<TSet> left = GetContext(data.LeftId);
                        SymbolicRegexNodeContext<TSet> right = GetContext(data.RightId);
                        Debug.Assert(!left.IsNull && !right.IsNull);

                        if (!left.IsNullableFor(context))
                        {
                            // If the left side is not nullable then the character must be consumed there.
                            // For example, Da(ab) = Da(a)b = b.
                            derivative = builder.CreateConcat(left.CreateDerivative(builder, elem, context), right);
                        }
                        else
                        {
                            SymbolicRegexNode<TSet> leftDerivative = builder.CreateConcat(left.CreateDerivative(builder, elem, context), right);
                            SymbolicRegexNode<TSet> rightDerivative = builder.CreateEffect(right.CreateDerivative(builder, elem, context), left);
                            // If the left alternative is high-priority-nullable then
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
                            derivative = left.IsHighPriorityNullableFor(context) ?
                                CreateAlternate(builder, rightDerivative, leftDerivative, hintRightLikelySubsumes: true) :
                                CreateAlternate(builder, leftDerivative, rightDerivative);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Loop:
                    {
                        SymbolicRegexNodeContext<TSet> body = GetContext(data.LeftId);
                        Debug.Assert(!body.IsNull);
                        Debug.Assert(data.Upper > 0);

                        if (data.Lower == 0 || body.IsNullable || !body.IsNullableFor(context))
                        {
                            // In these special cases the derivative concatenates the body's derivative with the decremented loop, so
                            // d(R{m,n}) = d(R)R{max(0,m-1),n-1}.
                            derivative = builder.CreateConcat(body.CreateDerivative(builder, elem, context), CreateLoopContinuation(builder));
                        }
                        else
                        {
                            // In the general case the concatenation must be created first and the derivative taken on that. For example,
                            // the first derivative for (a|\b){2} with an input "ac" is (a|\b)|epsilon, but the rule above would produce
                            // just (a|\b).
                            derivative = builder.CreateConcat(body, CreateLoopContinuation(builder)).CreateDerivative(builder, elem, context);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Alternate:
                    {
                        SymbolicRegexNodeContext<TSet> left = GetContext(data.LeftId);
                        SymbolicRegexNodeContext<TSet> right = GetContext(data.RightId);
                        Debug.Assert(!left.IsNull && !right.IsNull);
                        derivative = CreateAlternate(builder, left.CreateDerivative(builder, elem, context), right.CreateDerivative(builder, elem, context));
                        break;
                    }

                case SymbolicRegexNodeKind.Effect:
                    //Effect nodes do not have derivatives (effects have been stripped): It is an error to reach an Effect node here
                    Debug.Fail($"{nameof(CreateDerivative)}:{data.Kind}");
                    break;

                default:
                    // The derivative of any other case is nothing
                    // e.g. taking the derivative of () (epsilon) is [] (nothing)
                    derivative = builder._nothing;
                    break;
            }

            builder._derivativeCache[key] = derivative.Id;
            return derivative;
        }

        /// <summary>
        /// Remove any Effect nodes from the tree, keeping just the actual pattern.
        /// So Effect(R,E) would be simplified to just R.
        /// </summary>
        /// <returns>the node with all Effect nodes stripped away</returns>
        internal SymbolicRegexNode<TSet> StripEffects(SymbolicRegexBuilder<TSet> builder)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(StripEffects, builder);
            }

            // If the node doesn't contain any Effect nodes under it we are done
            if (!_info.ContainsEffect)
                return this;

            // Check the cache to avoid redundant work. The derivative of deeply nested loop patterns
            // like ((a)*)* produces a DAG with shared sub-trees that differ only in their Effect
            // wrappers. Without caching, each shared sub-tree is traversed once per reference rather
            // than once per unique node, leading to exponential time complexity in the nesting depth.
            if (builder._stripEffectsCache.TryGetValue(_id, out int cachedId))
                return builder.GetNode(cachedId);

            SymbolicRegexNode<TSet> result;

            // Recurse over the structure of the node to strip effects
            switch (_kind)
            {
                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    // This is the place where the effect (the right child) is getting ignored
                    result = _left.StripEffects(builder);
                    break;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    Debug.Assert(_left._info.ContainsEffect && !_right._info.ContainsEffect);
                    result = builder.CreateConcat(_left.StripEffects(builder), _right);
                    break;

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    // Strip effects from each child first, leveraging the cache for shared sub-trees,
                    // then flatten and deduplicate the results. Stripping before flattening is important:
                    // the pre-stripped tree may have exponentially many paths that collapse after effects
                    // are removed, but the cache ensures each unique sub-tree is only processed once.
                    {
                        SymbolicRegexNode<TSet> strippedLeft = _left.StripEffects(builder);
                        SymbolicRegexNode<TSet> strippedRight = _right.StripEffects(builder);
                        List<SymbolicRegexNode<TSet>> elems = strippedLeft.ToList(builder, listKind: SymbolicRegexNodeKind.Alternate);
                        strippedRight.ToList(builder, elems, listKind: SymbolicRegexNodeKind.Alternate);
                        result = builder.Alternate(elems);
                    }
                    break;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(!_left.IsNull);
                    result = builder.CreateDisableBacktrackingSimulation(_left.StripEffects(builder));
                    break;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(!_left.IsNull);
                    result = builder.CreateLoop(_left.StripEffects(builder), IsLazy, _lower, _upper);
                    break;

                default:
                    Debug.Fail($"{nameof(StripEffects)}:{_kind}");
                    return default;
            }

            builder._stripEffectsCache[_id] = result.Id;
            return result;
        }

        /// <summary>
        /// This function maps <see cref="SymbolicRegexNodeKind.Effect"/> nodes present in the tree and maps them into
        /// arrays of <see cref="DerivativeEffect"/>. The node is split into a list of pairs (node, effects), where the
        /// nodes represent elements of a top level alternation and the effects are what should be applied for all
        /// matches using that node. In effect, this function interprets effects produced during derivation into a form
        /// suitable for use in NFA simulation. This function is similar to <see cref="StripEffects"/> in that it removes
        /// Effect nodes as it maps them.
        /// For example, Effect(Effect(R,CaptureStart_1)|S,CaptureStart_0) would be turned into two pairs:
        /// (R,[CaptureStart_0,CaptureStart_1]) and (S,[CaptureStart_0]).
        /// Here both include the CaptureStart_0 effect, since both are nested inside the outer Effect node,
        /// while only R includes the CaptureStart_1 effect.
        /// </summary>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="context">immediately surrounding character context that affects nullability of anchors</param>
        /// <param name="alternativesAndEffects">the list to insert the pairs of nodes and their effects into in priority order</param>
        /// <param name="currentEffects">a helper list this function uses to accumulate effects in recursive calls</param>
        internal void StripAndMapEffects(SymbolicRegexBuilder<TSet> builder, uint context, List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> alternativesAndEffects,
            List<DerivativeEffect>? currentEffects = null)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(StripAndMapEffects, builder, context, alternativesAndEffects, currentEffects);
                return;
            }

            currentEffects ??= [];

            // If we've reached a node with no effects, then output that with the effects that have been accumulated
            if (!_info.ContainsEffect)
            {
                alternativesAndEffects.Add((this, currentEffects.Count > 0 ?
                    currentEffects.ToArray() :
                    []));
                return;
            }

            // Recurse over the structure of the node to strip and map effects
            switch (_kind)
            {
                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    // Push any effects that should be applied in any nodes under this node
                    int oldEffectCount = currentEffects.Count;
                    _right.ApplyEffects((e, s) => s.Add(e), context, currentEffects);
                    // Recurse into the main child
                    _left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
                    // Pop all the effects that were pushed above
                    currentEffects.RemoveRange(oldEffectCount, currentEffects.Count - oldEffectCount);
                    return;

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(!_left.IsNull && !_right.IsNull);
                        Debug.Assert(_left._info.ContainsEffect && !_right._info.ContainsEffect);
                        // For concat the nodes for the left hand side are added first and then fixed up by concatenating
                        // the right side to each of them.
                        int oldAlternativesCount = alternativesAndEffects.Count;
                        _left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
                        for (int i = oldAlternativesCount; i < alternativesAndEffects.Count; i++)
                        {
                            (SymbolicRegexNode<TSet> node, DerivativeEffect[] effects) = alternativesAndEffects[i];
                            alternativesAndEffects[i] = (builder.CreateConcat(node, _right), effects);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    _left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
                    _right.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
                    break;

                case SymbolicRegexNodeKind.Loop when _lower == 0 && _upper == 1:
                    // Currently due to the way Effect nodes are constructed and handled in simplification rules they
                    // should only appear inside loops that are used to make nodes "optional", i.e., ones with {0,1}
                    // bounds. The branch that skips the loop is represented by outputting a pair (epsilon,effects).
                    Debug.Assert(!_left.IsNull);
                    // For lazy loops skipping is preferred, so output the epsilon first
                    if (IsLazy)
                        alternativesAndEffects.Add((builder.Epsilon, currentEffects.Count > 0 ?
                            currentEffects.ToArray() :
                            []));
                    // Recurse into the body
                    _left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
                    // For eager loops the body is preferred, so output the epsilon last
                    if (!IsLazy)
                        alternativesAndEffects.Add((builder.Epsilon, currentEffects.Count > 0 ?
                            currentEffects.ToArray() :
                            []));
                    break;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    {
                        Debug.Assert(!_left.IsNull);
                        int oldAlternativesCount = alternativesAndEffects.Count;
                        _left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
                        for (int i = oldAlternativesCount; i < alternativesAndEffects.Count; i++)
                        {
                            (SymbolicRegexNode<TSet> node, DerivativeEffect[] effects) = alternativesAndEffects[i];
                            alternativesAndEffects[i] = (builder.CreateDisableBacktrackingSimulation(node), effects);
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

            ref readonly SymbolicRegexBuilder<TSet>.NodeData data = ref Data;
            switch (data.Kind)
            {
                case SymbolicRegexNodeKind.Concat:
                    {
                        SymbolicRegexNodeContext<TSet> left = GetContext(data.LeftId);
                        SymbolicRegexNodeContext<TSet> right = GetContext(data.RightId);
                        Debug.Assert(!left.IsNull && !right.IsNull);
                        Debug.Assert(left.IsNullableFor(context) && right.IsNullableFor(context));
                        left.ApplyEffects(apply, context, arg);
                        right.ApplyEffects(apply, context, arg);
                        break;
                    }

                case SymbolicRegexNodeKind.Loop:
                    {
                        SymbolicRegexNodeContext<TSet> body = GetContext(data.LeftId);
                        Debug.Assert(!body.IsNull);
                        // Apply effect when backtracking engine would enter loop
                        if (data.Lower != 0 || (data.Upper != 0 && !data.Info.IsLazyLoop && body.IsNullableFor(context)))
                        {
                            Debug.Assert(body.IsNullableFor(context));
                            body.ApplyEffects(apply, context, arg);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.Alternate:
                    {
                        SymbolicRegexNodeContext<TSet> left = GetContext(data.LeftId);
                        SymbolicRegexNodeContext<TSet> right = GetContext(data.RightId);
                        Debug.Assert(!left.IsNull && !right.IsNull);
                        if (left.IsNullableFor(context))
                        {
                            // Prefer the left side
                            left.ApplyEffects(apply, context, arg);
                        }
                        else
                        {
                            // Otherwise right side must be nullable
                            Debug.Assert(right.IsNullableFor(context));
                            right.ApplyEffects(apply, context, arg);
                        }
                        break;
                    }

                case SymbolicRegexNodeKind.CaptureStart:
                    apply(new DerivativeEffect(DerivativeEffectKind.CaptureStart, data.Lower), arg);
                    break;

                case SymbolicRegexNodeKind.CaptureEnd:
                    apply(new DerivativeEffect(DerivativeEffectKind.CaptureEnd, data.Lower), arg);
                    break;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(data.LeftId != 0);
                    GetContext(data.LeftId).ApplyEffects(apply, context, arg);
                    break;
            }
        }

#if DEBUG
        public override string ToString()
        {
            StringBuilder sb = new();
            ToStringHelper(sb);
            return sb.ToString();
        }

        internal void ToStringHelper(StringBuilder sb)
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(ToStringHelper, sb);
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
                    sb.Append('\u03B5');
                    return;

                case SymbolicRegexNodeKind.FixedLengthMarker:
                    sb.Append('\u02FF');
                    AppendNumberSubscript(sb, _lower);
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

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    sb.Append('(');
                    _left.ToStringHelper(sb);
                    sb.Append('|');
                    _right.ToStringHelper(sb);
                    sb.Append(')');
                    return;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    //mark left associative case with parenthesis
                    if (_left.Kind == SymbolicRegexNodeKind.Concat)
                        sb.Append('(');
                    _left.ToStringHelper(sb);
                    if (_left.Kind == SymbolicRegexNodeKind.Concat)
                        sb.Append(')');
                    _right.ToStringHelper(sb);
                    return;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(_set is not null);
                    sb.Append(_builder!._solver.PrettyPrint(_set, _builder._charSetSolver));
                    return;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(!_left.IsNull);
                    if (IsAnyStar(_builder!._solver))
                    {
                        sb.Append(".*");
                    }
                    else if (_lower == 0 && _upper == 1)
                    {
                        ToStringGrouped(_left, sb);
                        sb.Append('?');
                        if (IsLazy)
                        {
                            // lazy loop R{0,1}? is printed by R??
                            sb.Append('?');
                        }
                    }
                    else if (IsStar)
                    {
                        ToStringGrouped(_left, sb);
                        sb.Append('*');
                        if (IsLazy)
                        {
                            sb.Append('?');
                        }
                    }
                    else if (IsPlus)
                    {
                        ToStringGrouped(_left, sb);
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
                        ToStringGrouped(_left, sb);
                        sb.Append('{');
                        sb.Append(_lower);
                        if (_upper == int.MaxValue)
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
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    // Note that the order of printing here is flipped, because for notation it is prettier for the
                    // effects to appear first, but for uniformity of representation with other nodes the "main" child
                    // should be the left one.
                    sb.Append('(');
                    _right.ToStringHelper(sb);
                    sb.Append('\u03BE');
                    _left.ToStringHelper(sb);
                    sb.Append(')');
                    break;

                case SymbolicRegexNodeKind.CaptureStart:
                    sb.Append('\u230A'); // Left floor
                    // Include group number as a subscript
                    Debug.Assert(_lower >= 0);
                    AppendNumberSubscript(sb, _lower);
                    return;

                case SymbolicRegexNodeKind.CaptureEnd:
                    // Include group number as a superscript
                    Debug.Assert(_lower >= 0);
                    AppendNumberSuperscript(sb, _lower);
                    sb.Append('\u2309'); // Right ceiling
                    return;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(!_left.IsNull);
                    _left.ToStringHelper(sb);
                    return;

                default:
                    Debug.Fail($"{nameof(ToStringHelper)}:{_kind}");
                    return;
            }

            static void ToStringGrouped(SymbolicRegexNodeContext<TSet> node, StringBuilder sb)
            {
                switch (node._kind)
                {
                    case SymbolicRegexNodeKind.Singleton:
                        node.ToStringHelper(sb);
                        break;

                    default:
                        sb.Append('(');
                        node.ToStringHelper(sb);
                        sb.Append(')');
                        break;

                }
            }

            static void AppendNumberSubscript(StringBuilder sb, int value)
            {
                foreach (char c in value.ToString(CultureInfo.InvariantCulture))
                {
                    sb.Append((char)('\u2080' + (c - '0')));
                }
            }

            static void AppendNumberSuperscript(StringBuilder sb, int value)
            {
                foreach (char c in value.ToString(CultureInfo.InvariantCulture))
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
            }
        }
#endif

        /// <summary>
        /// Returns all sets that occur in the regex or the full set if there are no sets in the regex (e.g. the regex is "^").
        /// </summary>
        public HashSet<TSet> GetSets(SymbolicRegexBuilder<TSet> builder)
        {
            var sets = new HashSet<TSet>();
            CollectSets(builder, sets);
            return sets;
        }

        /// <summary>Collects all sets that occur in the regex into the specified collection.</summary>
        private void CollectSets(SymbolicRegexBuilder<TSet> builder, HashSet<TSet> sets)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                StackHelper.CallOnEmptyStack(CollectSets, builder, sets);
                return;
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.BOLAnchor:
                case SymbolicRegexNodeKind.EOLAnchor:
                case SymbolicRegexNodeKind.EndAnchorZ:
                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    sets.Add(builder._newLineSet);
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
                    Debug.Assert(!_left.IsNull);
                    _left.CollectSets(builder, sets);
                    return;

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    _left.CollectSets(builder, sets);
                    _right.CollectSets(builder, sets);
                    return;

                case SymbolicRegexNodeKind.Concat:
                    // avoid deep nested recursion over long concat nodes
                    SymbolicRegexNodeContext<TSet> conc = this;
                    while (conc._kind == SymbolicRegexNodeKind.Concat)
                    {
                        Debug.Assert(!conc._left.IsNull && !conc._right.IsNull);
                        conc._left.CollectSets(builder, sets);
                        conc = conc._right;
                    }
                    conc.CollectSets(builder, sets);
                    return;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(!_left.IsNull);
                    _left.CollectSets(builder, sets);
                    return;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                case SymbolicRegexNodeKind.BoundaryAnchor:
                    sets.Add(builder._wordLetterForBoundariesSet);
                    return;

                default:
                    Debug.Fail($"{nameof(CollectSets)}:{_kind}");
                    break;
            }
        }

        /// <summary>Compute and sort all the minterms from the sets in this regex.</summary>
        public TSet[] ComputeMinterms(SymbolicRegexBuilder<TSet> builder) =>
            MintermGenerator<TSet>.GenerateMinterms(builder._solver, GetSets(builder)).ToArray();

        /// <summary>
        /// Create the reverse of this regex
        /// </summary>
        public SymbolicRegexNode<TSet> Reverse(SymbolicRegexBuilder<TSet> builder)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Reverse, builder);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(!_left.IsNull);
                    return builder.CreateLoop(_left.Reverse(builder), IsLazy, _lower, _upper);

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(!_left.IsNull && !_right.IsNull);
                        SymbolicRegexNode<TSet> rev = _left.Reverse(builder);
                        SymbolicRegexNodeContext<TSet> rest = _right;
                        while (rest._kind == SymbolicRegexNodeKind.Concat)
                        {
                            Debug.Assert(!rest._left.IsNull && !rest._right.IsNull);
                            SymbolicRegexNode<TSet> rev1 = rest._left.Reverse(builder);
                            rev = builder.CreateConcat(rev1, rev);
                            rest = rest._right;
                        }
                        SymbolicRegexNode<TSet> restr = rest.Reverse(builder);
                        rev = builder.CreateConcat(restr, rev);
                        return rev;
                    }

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    return CreateAlternate(builder, _left.Reverse(builder), _right.Reverse(builder));

                case SymbolicRegexNodeKind.FixedLengthMarker:
                    // Fixed length markers are omitted in reverse
                    return builder.Epsilon;

                case SymbolicRegexNodeKind.BeginningAnchor:
                    // The reverse of BeginningAnchor is EndAnchor
                    return builder.EndAnchor;

                case SymbolicRegexNodeKind.EndAnchor:
                    return builder.BeginningAnchor;

                case SymbolicRegexNodeKind.BOLAnchor:
                    // The reverse of BOLanchor is EOLanchor
                    return builder.EolAnchor;

                case SymbolicRegexNodeKind.EOLAnchor:
                    return builder.BolAnchor;

                case SymbolicRegexNodeKind.EndAnchorZ:
                    // The reversal of the \Z anchor
                    return builder.EndAnchorZReverse;

                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    Debug.Fail("Should only happen if a reversed regex is reversed again, which isn't expected");
                    return builder.EndAnchorZ;

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(!_left.IsNull);
                    return builder.CreateDisableBacktrackingSimulation(_left.Reverse(builder));

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
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    return _left.StartsWithLoop(upperBoundLowestValue) || (_left.IsNullable && _right.StartsWithLoop(upperBoundLowestValue));

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    return _left.StartsWithLoop(upperBoundLowestValue) || _right.StartsWithLoop(upperBoundLowestValue);

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(!_left.IsNull);
                    return _left.StartsWithLoop(upperBoundLowestValue);

                default:
                    return false;
            };
        }

        /// <summary>Computes the set that includes all elements that can start a match.</summary>
        private static TSet ComputeStartSet(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexNodeContext<TSet> left, SymbolicRegexNodeContext<TSet> right)
        {
            // For singletons the start set is the set the singleton is constructed with,
            // so calling this function would not make sense.
            Debug.Assert(kind != SymbolicRegexNodeKind.Singleton);

            switch (kind)
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
                    return builder._solver.Empty;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(!left.IsNull);
                    return left._set;

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(!left.IsNull && !right.IsNull);
                    return left.CanBeNullable ? builder._solver.Or(left._set, right._set) : left._set;

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!left.IsNull && !right.IsNull);
                    return builder._solver.Or(left._set, right._set);

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                case SymbolicRegexNodeKind.Effect:
                    Debug.Assert(!left.IsNull);
                    return left._set;

                default:
                    Debug.Fail($"{nameof(ComputeStartSet)}:{kind}");
                    return builder._solver.Full;
            }
        }

        /// <summary>
        /// Replace anchors that are infeasible by [] wrt the given previous character kind and what continuation is possible.
        /// </summary>
        /// <remarks>
        /// This helps the matcher detect deadend states that have no viable matches in situations where the pattern's
        /// language is empty due to interactions between anchors and the rest of the pattern. For example, a*\ba would
        /// be simplified to [] when prevKind is a word letter. This allows the matcher to avoid spurious work and return
        /// early.
        /// </remarks>
        /// <param name="builder">the builder that owns this node</param>
        /// <param name="prevKind">previous character kind</param>
        internal SymbolicRegexNode<TSet> PruneAnchors(SymbolicRegexBuilder<TSet> builder, uint prevKind)
        {
            //first prune the anchors in the node
            TSet wlbSet = builder._wordLetterForBoundariesSet;

            //true if the startset of the node overlaps with some wordletter or the node can be nullable
            bool contWithWL = CanBeNullable || !builder._solver.IsEmpty(builder._solver.And(wlbSet, _set));

            //true if the startset of the node overlaps with some nonwordletter or the node can be nullable
            bool contWithNWL = CanBeNullable || !builder._solver.IsEmpty(builder._solver.And(builder._solver.Not(wlbSet), _set));

            return PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
        }

        private SymbolicRegexNode<TSet> PruneAnchorsImpl(SymbolicRegexBuilder<TSet> builder, uint prevKind, bool contWithWL, bool contWithNWL)
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(PruneAnchorsImpl, builder, prevKind, contWithWL, contWithNWL);
            }

            if (!_info.StartsWithSomeAnchor)
                return this;

            switch (_kind)
            {
                case SymbolicRegexNodeKind.BeginningAnchor:
                    return prevKind == CharKind.BeginningEnd ?
                        this :
                        builder._nothing; //start anchor is only nullable if the previous character is Start

                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    return ((prevKind & CharKind.BeginningEnd) != 0) ?
                        this :
                        builder._nothing; //rev(\Z) is only nullable if the previous characters is Start or the very first \n

                case SymbolicRegexNodeKind.BoundaryAnchor:
                    return (prevKind == CharKind.WordLetter ? contWithNWL : contWithWL) ?
                        this :
                        // \b is impossible when the previous character is \w but no continuation matches \W
                        // or the previous character is \W but no continuation matches \w
                        builder._nothing;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                    return (prevKind == CharKind.WordLetter ? contWithWL : contWithNWL) ?
                        this :
                        // \B is impossible when the previous character is \w but no continuation matches \w
                        // or the previous character is \W but no continuation matches \W
                        builder._nothing;

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(!_left.IsNull);
                    SymbolicRegexNode<TSet> body = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
                    return body == _left ?
                        this :
                        CreateLoop(builder, body, _lower, _upper, IsLazy);

                case SymbolicRegexNodeKind.Concat:
                    {
                        Debug.Assert(!_left.IsNull && !_right.IsNull);
                        SymbolicRegexNode<TSet> left1 = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<TSet> right1 = _left.IsNullable ? _right.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL) : _right;

                        Debug.Assert(!left1.IsNull && !right1.IsNull);
                        return left1 == _left && right1 == _right ?
                            this :
                            CreateConcat(builder, left1, right1);
                    }

                case SymbolicRegexNodeKind.Alternate:
                    {
                        Debug.Assert(!_left.IsNull && !_right.IsNull);
                        SymbolicRegexNode<TSet> left1 = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
                        SymbolicRegexNode<TSet> right1 = _right.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);

                        Debug.Assert(!left1.IsNull && !right1.IsNull);
                        return left1 == _left && right1 == _right ?
                            this :
                            CreateAlternate(builder, left1, right1);
                    }

                case SymbolicRegexNodeKind.Effect:
                    {
                        Debug.Assert(!_left.IsNull && !_right.IsNull);
                        SymbolicRegexNode<TSet> left1 = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
                        return left1 == _left ?
                            this :
                            CreateEffect(builder, left1, _right);
                    }

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(!_left.IsNull);
                    SymbolicRegexNode<TSet> child = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
                    return child == _left ?
                        this :
                        builder.CreateDisableBacktrackingSimulation(child);

                default:
                    return this;
            }
        }

        /// <summary>
        /// Resolve the preferred fixed length when accepting a match for this node. For example, a pattern .*?(dada$(4)|ada(3))
        /// after "dada" would be in a state $(4)|(3)|... and this function would return 4 if the match is at the end of input
        /// 3 otherwise.
        /// </summary>
        /// <param name="context">the context for deciding nullability</param>
        /// <returns>the fixed length of any match ending in this state, if any, or -1 otherwise</returns>
        internal int ResolveFixedLength(uint context)
        {
            Debug.Assert(IsNullableFor(context));

            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(ResolveFixedLength, context);
            }

            switch (_kind)
            {
                case SymbolicRegexNodeKind.FixedLengthMarker:
                    return _lower;

                case SymbolicRegexNodeKind.Alternate:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    if (_left.IsNullableFor(context))
                    {
                        // Left is nullable, so the match is from the left
                        return _left.ResolveFixedLength(context);
                    }
                    else
                    {
                        // Otherwise right must be nullable and thus the relevant match
                        Debug.Assert(_right.IsNullableFor(context));
                        return _right.ResolveFixedLength(context);
                    }

                case SymbolicRegexNodeKind.Concat:
                    Debug.Assert(!_left.IsNull && !_right.IsNull);
                    int leftLength = _left.ResolveFixedLength(context);
                    return leftLength >= 0 ? leftLength : _right.ResolveFixedLength(context);
            }
            return -1;
        }

        /// <summary>
        /// Let #(this) denote the number of singletons in this node.
        /// Then the NFA size estimation in terms of state count
        /// is #(this) if there are no anchors else <see cref="CharKind.CharKindCount"/>x#(this).
        /// Add 1 for the initial state also.
        /// </summary>
        internal int EstimateNfaSize() => Times(_info.ContainsSomeAnchor ? CharKind.CharKindCount : 1, Sum(1, CountSingletons()));

        /// <summary>
        /// Count the number of Regex Singletons, if all loops with explicit counters
        /// were eliminated from the node, i.e., as if the repetitions were explicitly unfolded.
        /// </summary>
        /// <remarks>
        /// Let node.CountSingletons() be abbreviated by #(node).
        /// Ex: #(a{6}) = 6*#(a) = 6
        /// Ex: #(a+|()) = #(aa*) = 2
        /// Ex: #(a{3,6}) = 6
        /// Ex: #(a{6,}) = #(a{6}a*)= 7
        /// </remarks>
        internal int CountSingletons()
        {
            using var pending = new ValueListBuilder<(int NodeId, int Multiplier)>(stackalloc (int, int)[32]);
            SymbolicRegexNodeContext<TSet> current = this;
            int multiplier = 1;
            int total = 0;

            // Saturating multiplication distributes over the nonnegative counts,
            // so loop multipliers can be carried to the leaves.
            while (true)
            {
                switch (current._kind)
                {
                    case SymbolicRegexNodeKind.Singleton:
                        total = Sum(total, multiplier);
                        break;

                    case SymbolicRegexNodeKind.Concat:
                    case SymbolicRegexNodeKind.Alternate:
                        Debug.Assert(!current._left.IsNull && !current._right.IsNull);
                        pending.Append((current._right.Id, multiplier));
                        current = current._left;
                        continue;

                    case SymbolicRegexNodeKind.Loop:
                        Debug.Assert(!current._left.IsNull && current._right.IsNull);
                        Debug.Assert(current._lower >= 0 && current._upper > 0 && current._upper >= current._lower);

                        int loopMultiplier;
                        if (current._upper == int.MaxValue)
                        {
                            if (current._lower is 0 or int.MaxValue)
                            {
                                // When an unbounded loop's body contains another unbounded loop, the
                                // derivative computation creates a 2-way Alternate at each nesting level
                                // (because the Concat derivative branches when its left side is nullable).
                                // This means the per-character derivative cost doubles with each such
                                // nesting level. Account for this so that the NFA size estimate catches
                                // deeply nested patterns that would cause exponential blowup.
                                // Note: RegexNode.ReduceLoops already collapses same-laziness non-capturing
                                // nested loops before the symbolic engine sees them, so this multiplier
                                // primarily fires for patterns with captures or mixed laziness.
                                // For capturing patterns like ((a)*)*, the inner loop is wrapped in
                                // Concat(CaptureStart, Concat(Loop, CaptureEnd)), so we search through
                                // structural wrappers to find nested unbounded loops.
                                loopMultiplier = ContainsNestedUnboundedLoop(current._builder!, current._left) ? 2 : 1;
                            }
                            else
                            {
                                // The case is R{m,} with R = _left and m = _lower.
                                // #(this) = (m+1) x #(R)
                                // Ex: #((ab){4,}) = #((ab)(ab)(ab)(ab)(ab)*) = 5x2 = 10
                                loopMultiplier = current._lower + 1;
                            }
                        }
                        else
                        {
                            // The general case with both upper and lower bounds is R{m,n} with m =_lower and n = _upper
                            // #(this) = n x #(R)
                            // Ex: #((ab){4,6}) = #((ab)(ab)(ab)(ab)(ab)?(ab)?) = 6x2 = 12
                            loopMultiplier = current._upper;
                        }

                        multiplier = Times(multiplier, loopMultiplier);
                        current = current._left;
                        continue;

                    case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    case SymbolicRegexNodeKind.Effect:
                        Debug.Assert(!current._left.IsNull);
                        current = current._left;
                        continue;

                    default:
                        Debug.Assert(current._left.IsNull && current._right.IsNull);
                        // All the other nodes contribute 0 to the overall count
                        // because they contain no children and therefore no singletons
                        break;
                }

                if (pending.Length == 0)
                {
                    return total;
                }

                (int NodeId, int Multiplier) next = pending.Pop();
                current = current.GetContext(next.NodeId);
                multiplier = next.Multiplier;
            }
        }

        /// <summary>
        /// Checks whether a node contains an unbounded loop (Loop with _upper == int.MaxValue)
        /// reachable through structural wrappers (Concat, Effect, DisableBacktrackingSimulation).
        /// Used by CountSingletons to detect nested unbounded loops even when captures or other
        /// structural nodes are interposed between the loops.
        /// </summary>
        private static bool ContainsNestedUnboundedLoop(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> node)
        {
            Stack<SymbolicRegexNode<TSet>> stack = new();
            stack.Push(node);

            while (stack.Count > 0)
            {
                SymbolicRegexNodeContext<TSet> current = builder.GetNodeContext(stack.Pop());

                switch (current._kind)
                {
                    case SymbolicRegexNodeKind.Loop:
                        if (current._upper == int.MaxValue)
                        {
                            return true;
                        }
                        break;

                    case SymbolicRegexNodeKind.Concat:
                        Debug.Assert(!current._left.IsNull && !current._right.IsNull);
                        stack.Push(current._right);
                        stack.Push(current._left);
                        break;

                    case SymbolicRegexNodeKind.Effect:
                    case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                        Debug.Assert(!current._left.IsNull);
                        stack.Push(current._left);
                        break;
                }
            }

            return false;
        }

        // In case of overflow in m+n, return int.MaxValue
        private static int Sum(int m, int n)
        {
            Debug.Assert(m >= 0 && n >= 0);
            return (int)Math.Min((long)m + n, int.MaxValue);
        }

        // In case of overflow in m*n return int.MaxValue
        private static int Times(int m, int n)
        {
            Debug.Assert(m >= 0 && n >= 0);
            return (int)Math.Min((long)m * n, int.MaxValue);
        }
    }
}
