﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides functionality to convert <see cref="RegexNode"/>s to corresponding <see cref="SymbolicRegexNode{S}"/>s.</summary>
    internal sealed class RegexNodeToSymbolicConverter
    {
        internal readonly Unicode.UnicodeCategoryTheory<BDD> _categorizer;
        internal readonly SymbolicRegexBuilder<BDD> _builder;
        private readonly CultureInfo _culture;
        private readonly Dictionary<(bool, string), BDD> _createConditionFromSet_Cache = new();
        private readonly Hashtable? _caps;

        /// <summary>Constructs a regex to symbolic finite automata converter</summary>
        public RegexNodeToSymbolicConverter(Unicode.UnicodeCategoryTheory<BDD> categorizer, CultureInfo culture, Hashtable? caps)
        {
            _categorizer = categorizer;
            _culture = culture;
            Solver = categorizer._solver;
            _builder = new SymbolicRegexBuilder<BDD>(Solver);
            _caps = caps;
        }

        /// <summary>The character solver associated with the regex converter</summary>
        public ICharAlgebra<BDD> Solver { get; }

        private BDD CreateConditionFromSet(bool ignoreCase, string set)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(CreateConditionFromSet, ignoreCase, set);
            }

            (bool ignoreCase, string set) key = (ignoreCase, set);
            if (!_createConditionFromSet_Cache.TryGetValue(key, out BDD? result))
            {
                _createConditionFromSet_Cache[key] = result = Compute(ignoreCase, set);
            }

            return result;

            BDD Compute(bool ignoreCase, string set)
            {
                // Char at position 0 is 1 iff the set is negated
                bool negate = RegexCharClass.IsNegated(set);

                // The set is divided into three pieces: ranges, conditions, subtraction

                // Handle ranges:
                // Following are conditions over characters in the set.
                // These will become disjuncts of a single disjunction
                // or conjuncts of a conjunction in case negate is true.
                // Negation is pushed in when the conditions are created.
                List<BDD> conditions = new List<BDD>();
                foreach ((char first, char last) in ComputeRanges(set))
                {
                    BDD cond = Solver.RangeConstraint(first, last, ignoreCase, _culture.Name);
                    conditions.Add(negate ? Solver.Not(cond) : cond);
                }

                // Handle categories:
                int setLength = set[RegexCharClass.SetLengthIndex];
                int catLength = set[RegexCharClass.CategoryLengthIndex];
                int catStart = setLength + RegexCharClass.SetStartIndex;
                int j = catStart;
                while (j < catStart + catLength)
                {
                    // Singleton categories are stored as unicode characters whose code is 1 + the
                    // unicode category code as a short. Thus -1 is applied to extract the actual
                    // code of the category. The category itself may be negated, e.g. \D instead of \d.
                    short catCode = (short)set[j++];
                    if (catCode != 0)
                    {
                        // Note that double negation cancels out the negation of the category.
                        BDD cond = MapCategoryCodeToCondition(Math.Abs(catCode) - 1);
                        conditions.Add(catCode < 0 ^ negate ? Solver.Not(cond) : cond);
                    }
                    else
                    {
                        // Special case for a whole group G of categories surrounded by 0's.
                        // Essentially 0 C1 C2 ... Cn 0 ==> G = (C1 | C2 | ... | Cn)
                        catCode = (short)set[j++];
                        if (catCode == 0)
                        {
                            continue; //empty set of categories
                        }

                        // Collect individual category codes into this set
                        var catCodes = new HashSet<int>();

                        // If the first catCode is negated, the group as a whole is negated
                        bool negGroup = catCode < 0;

                        while (catCode != 0)
                        {
                            catCodes.Add(Math.Abs(catCode) - 1);
                            catCode = (short)set[j++];
                        }

                        // C1 | C2 | ... | Cn
                        BDD catCondDisj = MapCategoryCodeSetToCondition(catCodes);

                        BDD catGroupCond = negate ^ negGroup ? Solver.Not(catCondDisj) : catCondDisj;
                        conditions.Add(catGroupCond);
                    }
                }

                // Handle subtraction
                BDD? subtractorCond = null;
                if (set.Length > j)
                {
                    // The set has a subtractor-set at the end.
                    // All characters in the subtractor-set are excluded from the set.
                    // Note that the subtractor sets may be nested, e.g. in r=[a-z-[b-g-[cd]]]
                    // the subtractor set [b-g-[cd]] has itself a subtractor set [cd].
                    // Thus r is the set of characters between a..z except b,e,f,g
                    subtractorCond = CreateConditionFromSet(ignoreCase, set.Substring(j));
                }

                // If there are no ranges and no groups then there are no conditions.
                // This situation arises for SingleLine regegex option and .
                // and means that all characters are accepted.
                BDD moveCond = conditions.Count == 0 ?
                    (negate ? Solver.False : Solver.True) :
                    (negate ? Solver.And(conditions) : Solver.Or(conditions));

                // Subtlety of regex sematics:
                // The subtractor is not within the scope of the negation (if there is a negation).
                // Thus the negated subtractor is conjuncted with moveCond after the negation has been
                // performed above.
                if (subtractorCond is not null)
                {
                    moveCond = Solver.And(moveCond, Solver.Not(subtractorCond));
                }

                return moveCond;

                static List<(char First, char Last)> ComputeRanges(string set)
                {
                    int setLength = set[RegexCharClass.SetLengthIndex];

                    var ranges = new List<(char, char)>(setLength);
                    int i = RegexCharClass.SetStartIndex;
                    int end = i + setLength;
                    while (i < end)
                    {
                        char first = set[i];
                        i++;

                        char last = i < end ?
                            (char)(set[i] - 1) :
                            RegexCharClass.LastChar;
                        i++;

                        ranges.Add((first, last));
                    }

                    return ranges;
                }

                BDD MapCategoryCodeSetToCondition(HashSet<int> catCodes)
                {
                    // TBD: perhaps other common cases should be specialized similarly
                    // check first if all word character category combinations are covered
                    // which is the most common case, then use the combined predicate \w
                    // rather than a disjunction of the component category predicates
                    // the word character class \w covers categories 0,1,2,3,4,8,18
                    BDD? catCond = null;
                    if (catCodes.Contains(0) && catCodes.Contains(1) && catCodes.Contains(2) && catCodes.Contains(3) &&
                        catCodes.Contains(4) && catCodes.Contains(8) && catCodes.Contains(18))
                    {
                        catCodes.Remove(0);
                        catCodes.Remove(1);
                        catCodes.Remove(2);
                        catCodes.Remove(3);
                        catCodes.Remove(4);
                        catCodes.Remove(8);
                        catCodes.Remove(18);
                        catCond = _categorizer.WordLetterCondition;
                    }

                    foreach (int cat in catCodes)
                    {
                        BDD cond = MapCategoryCodeToCondition(cat);
                        catCond = catCond is null ? cond : Solver.Or(catCond, cond);
                    }

                    Debug.Assert(catCodes.Count != 0);
                    return catCond!;
                }

                BDD MapCategoryCodeToCondition(int code) =>
                    code switch
                    {
                        99 => _categorizer.WhiteSpaceCondition, // whitespace has special code 99
                        < 0 or > 29 => throw new ArgumentOutOfRangeException(nameof(code), "Must be in the range 0..29 or equal to 99"), // TODO-NONBACKTRACKING: Remove message or put it into the .resx
                        _ => _categorizer.CategoryCondition(code)
                    };
            }
        }

        public SymbolicRegexNode<BDD> Convert(RegexNode node, bool topLevel)
        {
            // Guard against stack overflow due to deep recursion
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Convert, node, topLevel);
            }

            switch (node.Kind)
            {
                case RegexNodeKind.Alternate:
                    {
                        var nested = new SymbolicRegexNode<BDD>[node.ChildCount()];
                        for (int i = 0; i < nested.Length; i++)
                        {
                            nested[i] = Convert(node.Child(i), topLevel);
                        }
                        return _builder.MkOrderedOr(nested);
                    }

                case RegexNodeKind.Beginning:
                    return _builder._startAnchor;

                case RegexNodeKind.Bol:
                    EnsureNewlinePredicateInitialized();
                    return _builder._bolAnchor;

                case RegexNodeKind.Capture when node.N == -1:
                    int captureNum;
                    if (_caps == null || !_caps.TryGetValue(node.M, out captureNum))
                        captureNum = node.M;
                    return _builder.MkCapture(Convert(node.Child(0), topLevel: false), captureNum);

                case RegexNodeKind.Concatenate:
                    {
                        var converted = new SymbolicRegexNode<BDD>[node.ChildCount()];
                        for (int i = 0; i < node.ChildCount(); ++i)
                        {
                            converted[i] = Convert(node.Child(i), topLevel: false);
                        }
                        return _builder.MkConcat(converted, topLevel);
                    }

                case RegexNodeKind.Empty:
                case RegexNodeKind.UpdateBumpalong: // optional directive that behaves the same as Empty
                    return _builder._epsilon;

                case RegexNodeKind.End:  // \z anchor
                    return _builder._endAnchor;

                case RegexNodeKind.EndZ: // \Z anchor
                    EnsureNewlinePredicateInitialized();
                    return _builder._endAnchorZ;

                case RegexNodeKind.Eol:
                    EnsureNewlinePredicateInitialized();
                    return _builder._eolAnchor;

                case RegexNodeKind.Loop:
                    return _builder.MkLoop(Convert(node.Child(0), topLevel: false), isLazy: false, node.M, node.N);

                case RegexNodeKind.Lazyloop:
                    return _builder.MkLoop(Convert(node.Child(0), topLevel: false), isLazy: true, node.M, node.N);

                case RegexNodeKind.Multi:
                    return ConvertMulti(node, topLevel);

                case RegexNodeKind.Notone:
                    return _builder.MkSingleton(Solver.Not(Solver.CharConstraint(node.Ch, (node.Options & RegexOptions.IgnoreCase) != 0, _culture.Name)));

                case RegexNodeKind.Notoneloop:
                case RegexNodeKind.Notonelazy:
                    return ConvertNotoneloop(node, node.Kind == RegexNodeKind.Notonelazy);

                case RegexNodeKind.One:
                    return _builder.MkSingleton(Solver.CharConstraint(node.Ch, (node.Options & RegexOptions.IgnoreCase) != 0, _culture.Name));

                case RegexNodeKind.Oneloop:
                case RegexNodeKind.Onelazy:
                    return ConvertOneloop(node, node.Kind == RegexNodeKind.Onelazy);

                case RegexNodeKind.Set:
                    return ConvertSet(node);

                case RegexNodeKind.Setloop:
                case RegexNodeKind.Setlazy:
                    return ConvertSetloop(node, node.Kind == RegexNodeKind.Setlazy);

                // TBD: ECMA case intersect predicate with ascii range ?
                case RegexNodeKind.Boundary:
                case RegexNodeKind.ECMABoundary:
                    EnsureWordLetterPredicateInitialized();
                    return _builder._wbAnchor;

                // TBD: ECMA case intersect predicate with ascii range ?
                case RegexNodeKind.NonBoundary:
                case RegexNodeKind.NonECMABoundary:
                    EnsureWordLetterPredicateInitialized();
                    return _builder._nwbAnchor;

                case RegexNodeKind.Nothing:
                    return _builder._nothing;

#if DEBUG
                case RegexNodeKind.ExpressionConditional:
                    // Try to extract the special case representing complement or intersection
                    if (IsComplementedNode(node))
                    {
                        return _builder.MkNot(Convert(node.Child(0), topLevel: false));
                    }

                    if (TryGetIntersection(node, out List<RegexNode>? conjuncts))
                    {
                        var nested = new SymbolicRegexNode<BDD>[conjuncts.Count];
                        for (int i = 0; i < nested.Length; i++)
                        {
                            nested[i] = Convert(conjuncts[i], topLevel: false);
                        }
                        return _builder.MkAnd(nested);
                    }

                    goto default;
#endif

                default:
                    throw new NotSupportedException(SR.Format(SR.NotSupported_NonBacktrackingConflictingExpression, node.Kind switch
                    {
                        RegexNodeKind.Capture => SR.ExpressionDescription_BalancingGroup,
                        RegexNodeKind.ExpressionConditional => SR.ExpressionDescription_IfThenElse,
                        RegexNodeKind.Backreference => SR.ExpressionDescription_Backreference,
                        RegexNodeKind.BackreferenceConditional => SR.ExpressionDescription_Conditional,
                        RegexNodeKind.PositiveLookaround => SR.ExpressionDescription_PositiveLookaround,
                        RegexNodeKind.NegativeLookaround => SR.ExpressionDescription_NegativeLookaround,
                        RegexNodeKind.Start => SR.ExpressionDescription_ContiguousMatches,
                        RegexNodeKind.Atomic or
                        RegexNodeKind.Setloopatomic or
                        RegexNodeKind.Oneloopatomic or
                        RegexNodeKind.Notoneloopatomic => SR.ExpressionDescription_AtomicSubexpressions,
                        _ => UnexpectedNodeType(node)
                    }));

                    static string UnexpectedNodeType(RegexNode node)
                    {
                        // The default should never arise, since other node types are either supported
                        // or have been removed (e.g. Group) from the final parse tree.
                        string description = $"Unexpected node type ({nameof(RegexNode)}:{node.Kind})";
                        Debug.Fail(description);
                        return description;
                    }
            }

            void EnsureNewlinePredicateInitialized()
            {
                // Update the \n predicate in the builder if it has not been updated already
                if (_builder._newLinePredicate.Equals(_builder._solver.False))
                {
                    _builder._newLinePredicate = _builder._solver.CharConstraint('\n');
                }
            }

            void EnsureWordLetterPredicateInitialized()
            {
                // Update the word letter predicate based on the Unicode definition of it if it was not updated already
                if (_builder._wordLetterPredicateForAnchors.Equals(_builder._solver.False))
                {
                    // Use the predicate including joiner and non joiner
                    _builder._wordLetterPredicateForAnchors = _categorizer.WordLetterConditionForAnchors;
                }
            }

            SymbolicRegexNode<BDD> ConvertMulti(RegexNode node, bool topLevel)
            {
                Debug.Assert(node.Kind == RegexNodeKind.Multi);

                string? sequence = node.Str;
                Debug.Assert(sequence is not null);

                bool ignoreCase = (node.Options & RegexOptions.IgnoreCase) != 0;

                var conds = new BDD[sequence.Length];
                for (int i = 0; i < conds.Length; i++)
                {
                    conds[i] = Solver.CharConstraint(sequence[i], ignoreCase, _culture.Name);
                }

                return _builder.MkSequence(conds, topLevel);
            }

            SymbolicRegexNode<BDD> ConvertOneloop(RegexNode node, bool isLazy)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Onelazy);

                bool ignoreCase = (node.Options & RegexOptions.IgnoreCase) != 0;
                BDD cond = Solver.CharConstraint(node.Ch, ignoreCase, _culture.Name);

                SymbolicRegexNode<BDD> body = _builder.MkSingleton(cond);
                SymbolicRegexNode<BDD> loop = _builder.MkLoop(body, isLazy, node.M, node.N);
                return loop;
            }

            SymbolicRegexNode<BDD> ConvertNotoneloop(RegexNode node, bool isLazy)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Notoneloop or RegexNodeKind.Notonelazy);

                bool ignoreCase = (node.Options & RegexOptions.IgnoreCase) != 0;
                BDD cond = Solver.Not(Solver.CharConstraint(node.Ch, ignoreCase, _culture.Name));

                SymbolicRegexNode<BDD> body = _builder.MkSingleton(cond);
                SymbolicRegexNode<BDD> loop = _builder.MkLoop(body, isLazy, node.M, node.N);
                return loop;
            }

            SymbolicRegexNode<BDD> ConvertSet(RegexNode node)
            {
                Debug.Assert(node.Kind == RegexNodeKind.Set);

                string? set = node.Str;
                Debug.Assert(set is not null);

                BDD moveCond = CreateConditionFromSet((node.Options & RegexOptions.IgnoreCase) != 0, set);

                return _builder.MkSingleton(moveCond);
            }

            SymbolicRegexNode<BDD> ConvertSetloop(RegexNode node, bool isLazy)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Setloop or RegexNodeKind.Setlazy);

                string? set = node.Str;
                Debug.Assert(set is not null);

                BDD moveCond = CreateConditionFromSet((node.Options & RegexOptions.IgnoreCase) != 0, set);

                SymbolicRegexNode<BDD> body = _builder.MkSingleton(moveCond);
                return _builder.MkLoop(body, isLazy, node.M, node.N);
            }

#if DEBUG
            // TODO-NONBACKTRACKING: recognizing strictly only [] (RegexNode.Nothing), for example [0-[0]] would not be recognized
            bool IsNothing(RegexNode node) => node.Kind == RegexNodeKind.Nothing || (node.Kind == RegexNodeKind.Set && ConvertSet(node).IsNothing);

            bool IsDotStar(RegexNode node) => node.Kind == RegexNodeKind.Setloop && Convert(node, topLevel: false).IsAnyStar;

            bool IsIntersect(RegexNode node) => node.Kind == RegexNodeKind.ExpressionConditional && IsNothing(node.Child(2));

            bool TryGetIntersection(RegexNode node, [Diagnostics.CodeAnalysis.NotNullWhen(true)] out List<RegexNode>? conjuncts)
            {
                if (!IsIntersect(node))
                {
                    conjuncts = null;
                    return false;
                }

                conjuncts = new();
                conjuncts.Add(node.Child(0));
                node = node.Child(1);
                while (IsIntersect(node))
                {
                    conjuncts.Add(node.Child(0));
                    node = node.Child(1);
                }

                conjuncts.Add(node);
                return true;
            }

            bool IsComplementedNode(RegexNode node) => IsNothing(node.Child(1)) && IsDotStar(node.Child(2));
#endif
        }
    }
}
