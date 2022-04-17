// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents a set of symbolic regexes that is either a disjunction or a conjunction</summary>
    internal sealed class SymbolicRegexSet<TSet> : IEnumerable<SymbolicRegexNode<TSet>> where TSet : IComparable<TSet>
    {
        internal readonly SymbolicRegexBuilder<TSet> _builder;

        private readonly HashSet<SymbolicRegexNode<TSet>> _set;

        /// <remarks>
        /// Symbolic regex A{0,k}?B is stored as (A,B,true) -> k  -- lazy
        /// Symbolic regex A{0,k}? is stored as (A,(),true) -> k  -- lazy
        /// Symbolic regex A{0,k}B is stored as (A,B,false) -> k  -- eager
        /// Symbolic regex A{0,k} is stored as (A,(),false) -> k  -- eager
        /// </remarks>
        private readonly Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int> _loops;

        /// <summary> the union (or intersection) of all singletons in the collection if any or null if none</summary>
        private readonly SymbolicRegexNode<TSet>? _singleton;

        internal readonly SymbolicRegexNodeKind _kind;

        private int _hashCode;

        /// <summary>If >= 0 then the maximal length of a fixed length markers in the set</summary>
        internal int _maximumLength = -1;

        private SymbolicRegexSet(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, HashSet<SymbolicRegexNode<TSet>>? set, Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int>? loops, SymbolicRegexNode<TSet>? singleton)
        {
            Debug.Assert(kind is SymbolicRegexNodeKind.And or SymbolicRegexNodeKind.Or);
            Debug.Assert((set is null) == (loops is null));

            _builder = builder;
            _kind = kind;
            _set = set ?? new HashSet<SymbolicRegexNode<TSet>>();
            _loops = loops ?? new Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int>();
            _singleton = singleton;
        }

        /// <summary>Denotes the empty conjunction</summary>
        public bool IsEverything => _kind == SymbolicRegexNodeKind.And && _set.Count == 0 && _loops.Count == 0 && _singleton == null;

        /// <summary>Denotes the empty disjunction</summary>
        public bool IsNothing => _kind == SymbolicRegexNodeKind.Or && _set.Count == 0 && _loops.Count == 0 && _singleton == null;

        /// <summary>How many elements are there in this set</summary>
        public int Count => _set.Count + _loops.Count + (_singleton == null ? 0 : 1);

        /// <summary>True iff the set is a singleton</summary>
        public bool IsSingleton => Count == 1;

        internal static SymbolicRegexSet<TSet> CreateFull(SymbolicRegexBuilder<TSet> builder) => new SymbolicRegexSet<TSet>(builder, SymbolicRegexNodeKind.And, null, null, null);

        internal static SymbolicRegexSet<TSet> CreateEmpty(SymbolicRegexBuilder<TSet> builder) => new SymbolicRegexSet<TSet>(builder, SymbolicRegexNodeKind.Or, null, null, null);

        internal static SymbolicRegexSet<TSet> CreateMulti(SymbolicRegexBuilder<TSet> builder, IEnumerable<SymbolicRegexNode<TSet>> elems, SymbolicRegexNodeKind kind)
        {
            // Loops contains the actual multi-set part of the collection
            var loops = new Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int>();

            // Other represents a normal set
            var other = new HashSet<SymbolicRegexNode<TSet>>();

            // Combination of singletons (when not null)
            SymbolicRegexNode<TSet>? singleton = null;

            int fixedLength = -1;

            foreach (SymbolicRegexNode<TSet> elem in elems)
            {
                // Keep track of the maximal fixed length if this is a disjunction
                // this means for example if the regex is abc(3)|bc(2) and
                // the input is xxxabcyyy then two fixed length markers will occur (3) and (2)
                // after reading c and the maximal one is taken
                // in a conjuctive setting this is undefined and the fixed length remains -1
                if (kind == SymbolicRegexNodeKind.Or &&
                    elem._kind == SymbolicRegexNodeKind.FixedLengthMarker && elem._lower > fixedLength)
                {
                    fixedLength = elem._lower;
                }

                #region start foreach
                if (elem == builder._anyStar)
                {
                    // .* is the absorbing element for disjunction
                    if (kind == SymbolicRegexNodeKind.Or)
                    {
                        return builder.FullSet;
                    }
                }
                else if (elem == builder._nothing)
                {
                    // [] is the absorbing element for conjunction
                    if (kind == SymbolicRegexNodeKind.And)
                    {
                        return builder.EmptySet;
                    }
                }
                else
                {
                    switch (elem._kind)
                    {
                        case SymbolicRegexNodeKind.And:
                        case SymbolicRegexNodeKind.Or:
                            Debug.Assert(elem._alts is not null);
                            if (kind == elem._kind)
                            {
                                // Flatten the inner set
                                foreach (SymbolicRegexNode<TSet> alt in elem._alts)
                                {
                                    if (alt._kind == SymbolicRegexNodeKind.Loop && alt._lower == 0)
                                    {
                                        AddLoopElement(builder, loops, other, alt, builder.Epsilon, kind);
                                    }
                                    else
                                    {
                                        if (alt._kind == SymbolicRegexNodeKind.Concat && alt._left!._kind == SymbolicRegexNodeKind.Loop && alt._left._lower == 0)
                                        {
                                            Debug.Assert(alt._right is not null);
                                            AddLoopElement(builder, loops, other, alt._left, alt._right, kind);
                                        }
                                        else
                                        {
                                            if (alt._kind == SymbolicRegexNodeKind.Singleton)
                                            {
                                                Debug.Assert(alt._set is not null);
                                                if (singleton is null)
                                                {
                                                    singleton = alt;
                                                }
                                                else
                                                {
                                                    Debug.Assert(singleton._kind == SymbolicRegexNodeKind.Singleton && singleton._set is not null);
                                                    // Join the sets either by Intersecting or Unioning
                                                    // which at the character set level translates to conjunction or disjunction in the underlying character solver
                                                    TSet set = kind == SymbolicRegexNodeKind.Or ? builder._solver.Or(singleton._set, alt._set) : builder._solver.And(singleton._set, alt._set);
                                                    singleton = SymbolicRegexNode<TSet>.CreateSingleton(builder, set);
                                                }
                                            }
                                            else
                                            {
                                                other.Add(alt);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                other.Add(elem);
                            }
                            break;

                        case SymbolicRegexNodeKind.Loop:
                            if (elem._lower == 0)
                            {
                                AddLoopElement(builder, loops, other, elem, builder.Epsilon, kind);
                            }
                            else
                            {
                                other.Add(elem);
                            }
                            break;

                        case SymbolicRegexNodeKind.Concat:
                            Debug.Assert(elem._left is not null && elem._right is not null);
                            if (elem._kind == SymbolicRegexNodeKind.Concat && elem._left._kind == SymbolicRegexNodeKind.Loop && elem._left._lower == 0)
                            {
                                AddLoopElement(builder, loops, other, elem._left, elem._right, kind);
                            }
                            else
                            {
                                other.Add(elem);
                            }
                            break;

                        case SymbolicRegexNodeKind.Singleton:
                            Debug.Assert(elem._set is not null);
                            if (singleton is null)
                            {
                                singleton = elem;
                            }
                            else
                            {
                                Debug.Assert(singleton._kind == SymbolicRegexNodeKind.Singleton && singleton._set is not null);
                                // Join the sets either by Intersecting or Unioning
                                // which at the character set level translates to conjunction or disjunction in the underlying character solver
                                TSet set = kind == SymbolicRegexNodeKind.Or ? builder._solver.Or(singleton._set, elem._set) : builder._solver.And(singleton._set, elem._set);
                                singleton = SymbolicRegexNode<TSet>.CreateSingleton(builder, set);
                            }
                            break;

                        default:
                            other.Add(elem);
                            break;
                    }
                }
                #endregion
            }

            // This optimization is only valid for a conjunction/intersection
            if (kind == SymbolicRegexNodeKind.And && singleton is not null && singleton.Equals(builder._solver.Empty))
            {
                return builder.EmptySet;
            }

            // The following is only valid for a disjunction/union
            if (kind == SymbolicRegexNodeKind.Or)
            {
                // If any element of other is covered in loops then omit it
                var others1 = new HashSet<SymbolicRegexNode<TSet>>();
                foreach (SymbolicRegexNode<TSet> sr in other)
                {
                    // If there is an element A{0,m} then A is not needed because
                    // it is included by the loop due to the upper bound m > 0
                    if (loops.ContainsKey((sr, builder.Epsilon, false)))
                    {
                        others1.Add(sr);
                    }
                }

                foreach (KeyValuePair<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int> pair in loops)
                {
                    // If there is an element A{0,m}B then B is not needed because
                    // it is included by the concatenation due to the lower bound 0
                    if (other.Contains(pair.Key.Item2))
                    {
                        others1.Add(pair.Key.Item2);
                    }
                }

                other.ExceptWith(others1);
            }

            return
                other.Count != 0 || loops.Count != 0 || singleton is not null ? new SymbolicRegexSet<TSet>(builder, kind, other, loops, singleton) { _maximumLength = fixedLength } :
                kind == SymbolicRegexNodeKind.Or ? builder.EmptySet :
                builder.FullSet;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void AddLoopElement(
                SymbolicRegexBuilder<TSet> builder,
                Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int> loops,
                HashSet<SymbolicRegexNode<TSet>> other, SymbolicRegexNode<TSet> loop,
                SymbolicRegexNode<TSet> rest,
                SymbolicRegexNodeKind kind)
            {
                if (loop._upper == 0 && rest.IsEpsilon)
                {
                    // In a set treat a loop with upper=lower=0 and no rest (no continuation after the loop)
                    // as () independent of whether it is lazy or eager
                    other.Add(builder.Epsilon);
                }
                else
                {
                    Debug.Assert(loop._left is not null);
                    (SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool) key = (loop._left, rest, loop.IsLazy);
                    if (!loops.TryGetValue(key, out int count) ||
                        (kind == SymbolicRegexNodeKind.Or ? count < loop._upper : count > loop._upper)) // If disjunction then map to the maximum of the upper bounds else to the minimum
                    {
                        loops[key] = loop._upper;
                    }
                }
            }
        }

        internal bool IsNullableFor(uint context)
        {
            Enumerator e = GetEnumerator();

            if (_kind == SymbolicRegexNodeKind.Or)
            {
                // Some element must be nullable
                while (e.MoveNext())
                {
                    if (e.Current.IsNullableFor(context))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                Debug.Assert(_kind == SymbolicRegexNodeKind.And);

                // All elements must be nullable
                while (e.MoveNext())
                {
                    if (!e.Current.IsNullableFor(context))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                int hashCode = _kind.GetHashCode();

                if (_singleton is not null)
                {
                    hashCode ^= _singleton.GetHashCode();
                }

                foreach (SymbolicRegexNode<TSet> n in _set)
                {
                    hashCode ^= n.GetHashCode();
                }

                foreach (KeyValuePair<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int> entry in _loops)
                {
                    hashCode ^= entry.Key.GetHashCode() + entry.Value.GetHashCode();
                }

                _hashCode = hashCode;
            }

            return _hashCode;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            if (obj is not SymbolicRegexSet<TSet> that ||
                _kind != that._kind ||
                _singleton is null && that._singleton is not null ||
                _singleton is not null && !_singleton.Equals(that._singleton) ||
                _set.Count != that._set.Count ||
                _loops.Count != that._loops.Count ||
                (_set.Count > 0 && !_set.SetEquals(that._set)))
            {
                return false;
            }

            foreach (KeyValuePair<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int> c in _loops)
            {
                if (!that._loops.TryGetValue(c.Key, out int count) || !count.Equals(c.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public void ToString(StringBuilder sb)
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            if (IsNothing)
            {
                sb.Append(SymbolicRegexNode<TSet>.EmptyCharClass);
            }
            else if (IsEverything)
            {
                sb.Append(".*");
            }
            else
            {
                Enumerator enumerator = GetEnumerator();
                bool nonempty = enumerator.MoveNext();
                Debug.Assert(nonempty, "Collection must be nonempty because IsNothing is false and IsEverything is false");
                SymbolicRegexNode<TSet> node = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    // The collection only has one element
                    node.ToString(sb);
                }
                else
                {
                    // Union of two or more elements
                    sb.Append('(');
                    // Append the first two elements
                    node.ToString(sb);
                    // Using the operator & for intersection
                    char op = _kind == SymbolicRegexNodeKind.Or ? '|' : '&';
                    sb.Append(op);
                    enumerator.Current.ToString(sb);
                    while (enumerator.MoveNext())
                    {
                        // Append all the remaining elements
                        sb.Append(op);
                        enumerator.Current.ToString(sb);
                    }
                    sb.Append(')');
                }
            }
        }

        internal SymbolicRegexSet<TNewSet> Transform<TNewSet>(SymbolicRegexBuilder<TNewSet> builderT, Func<SymbolicRegexBuilder<TNewSet>, TSet, TNewSet> setTransformer)
            where TNewSet : IComparable<TNewSet>
        {
            // This function is mutually recursive with the one in SymbolicRegexBuilder, which has stack overflow avoidance
            return SymbolicRegexSet<TNewSet>.CreateMulti(builderT, TransformElements(builderT, setTransformer), _kind);

            IEnumerable<SymbolicRegexNode<TNewSet>> TransformElements(SymbolicRegexBuilder<TNewSet> builderT, Func<SymbolicRegexBuilder<TNewSet>, TSet, TNewSet> setTransformer)
            {
                foreach (SymbolicRegexNode<TSet> sr in this)
                {
                    yield return _builder.Transform(sr, builderT, setTransformer);
                }
            }
        }

        internal SymbolicRegexNode<TSet> GetSingletonElement()
        {
            Debug.Assert(IsSingleton);

            Enumerator e = GetEnumerator();
            bool success = e.MoveNext();
            Debug.Assert(success);
            return e.Current;
        }

        internal SymbolicRegexSet<TSet> Reverse()
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            return CreateMulti(_builder, ReverseElements(), _kind);

            IEnumerable<SymbolicRegexNode<TSet>> ReverseElements()
            {
                foreach (SymbolicRegexNode<TSet> n in this)
                {
                    yield return n.Reverse();
                }
            }
        }

        internal bool StartsWithLoop(int upperBoundLowestValue)
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            foreach (SymbolicRegexNode<TSet> n in this)
            {
                if (n.StartsWithLoop(upperBoundLowestValue))
                {
                    return true;
                }
            }

            return false;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<SymbolicRegexNode<TSet>> IEnumerable<SymbolicRegexNode<TSet>>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        internal int GetFixedLength()
        {
            // This function is mutually recursive with the one in SymbolicRegexNode, which has stack overflow avoidance
            if (_loops.Count > 0)
            {
                return -1;
            }

            int length = -1;
            foreach (SymbolicRegexNode<TSet> node in _set)
            {
                int nodeLength = node.GetFixedLength();

                if (nodeLength == -1)
                {
                    return -1;
                }
                else if (length == -1)
                {
                    length = nodeLength;
                }
                else if (length != nodeLength)
                {
                    return -1;
                }
            }

            if (_singleton is not null && length != 1)
            {
                if (length == -1)
                {
                    length = 1;
                }
                else
                {
                    length = -1;
                }
            }

            return length;
        }

        /// <summary>Enumerates all symbolic regexes in the set</summary>
        internal struct Enumerator : IEnumerator<SymbolicRegexNode<TSet>>
        {
            private readonly SymbolicRegexSet<TSet> _set;
            private int _state; // 0 = return singleton, 1 == iterate set, 2 == iterate loops, 3 == done
            private SymbolicRegexNode<TSet>? _current;
            private HashSet<SymbolicRegexNode<TSet>>.Enumerator _setEnumerator;
            private Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>, bool), int>.Enumerator _loopsEnumerator;

            internal Enumerator(SymbolicRegexSet<TSet> symbolicRegexSet)
            {
                _state = symbolicRegexSet._singleton is null ? 1 : 0;
                _set = symbolicRegexSet;
                _setEnumerator = symbolicRegexSet._set.GetEnumerator();
                _loopsEnumerator = symbolicRegexSet._loops.GetEnumerator();
                _current = null;
            }

            public SymbolicRegexNode<TSet> Current => _current!;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _state = 3;
                _setEnumerator.Dispose();
                _loopsEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 0:
                        Debug.Assert(_set._singleton is not null);
                        _current = _set._singleton;
                        _state = 1;
                        return true;

                    case 1:
                        if (_setEnumerator.MoveNext())
                        {
                            _current = _setEnumerator.Current;
                            return true;
                        }
                        _state = 2;
                        goto case 2;

                    case 2:
                        if (_loopsEnumerator.MoveNext())
                        {
                            // Recreate the symbolic regex from (body,rest)->k to body{0,k}rest
                            (SymbolicRegexNode<TSet> body, SymbolicRegexNode<TSet> rest, bool isLazy) = _loopsEnumerator.Current.Key;
                            int upper = _loopsEnumerator.Current.Value;
                            _current = _set._builder.CreateConcat(_set._builder.CreateLoop(body, isLazy, 0, upper), rest);
                            return true;
                        }
                        _state = 3;
                        goto default;

                    default:
                        _current = null!;
                        return false;
                }
            }

            public void Reset() => throw new NotSupportedException();
        }
    }
}
