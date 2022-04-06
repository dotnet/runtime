// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides functionality to convert <see cref="RegexNode"/>s to corresponding <see cref="SymbolicRegexNode{S}"/>s.</summary>
    internal sealed class RegexNodeConverter
    {
        /// <summary>The culture to use for IgnoreCase comparisons.</summary>
        private readonly CultureInfo _culture;
        /// <summary>Capture information.</summary>
        private readonly Hashtable? _captureSparseMapping;
        /// <summary>The builder to use to create the <see cref="SymbolicRegexNode{S}"/> nodes.</summary>
        internal readonly SymbolicRegexBuilder<BDD> _builder;

        /// <summary>Cache of BDDs created to represent <see cref="RegexCharClass"/> set strings.</summary>
        /// <remarks>This cache is useful iff the same character class is used multiple times in the same regex, but that's fairly common.</remarks>
        private Dictionary<(bool IgnoreCase, string Set), BDD>? _setBddCache;

        /// <summary>Constructs a regex to symbolic finite automata converter</summary>
        public RegexNodeConverter(SymbolicRegexBuilder<BDD> builder, CultureInfo culture, Hashtable? captureSparseMapping)
        {
            _builder = builder;
            _culture = culture;
            _captureSparseMapping = captureSparseMapping;
        }

        /// <summary>Converts a <see cref="RegexNode"/> into its corresponding <see cref="SymbolicRegexNode{S}"/>.</summary>
        /// <param name="node">The node to convert.</param>
        /// <param name="tryCreateFixedLengthMarker">Whether we should attempt to create a fixed length marker after this node.</param>
        /// <returns>The generated <see cref="SymbolicRegexNode{S}"/> that corresponds to the supplied <paramref name="node"/>.</returns>
        public SymbolicRegexNode<BDD> ConvertToSymbolicRegexNode(RegexNode node, bool tryCreateFixedLengthMarker)
        {
            // We're processing the node tree recursively and need to avoid stack overflows for really deep trees.
            // To achieve this, if we detect we're too deep on the stack, we fork off the handling of this node
            // to another thread and block this thread until it completes.
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(ConvertToSymbolicRegexNode, node, tryCreateFixedLengthMarker);
            }

            // Handle each node kind as-is appropriate.
            switch (node.Kind)
            {
                // Singletons and multis

                case RegexNodeKind.One:
                    return _builder.CreateSingleton(CharSetSolver.Instance.CharConstraint(node.Ch));

                case RegexNodeKind.Notone:
                    return _builder.CreateSingleton(CharSetSolver.Instance.Not(CharSetSolver.Instance.CharConstraint(node.Ch)));

                case RegexNodeKind.Set:
                    return ConvertSet(node);

                case RegexNodeKind.Multi:
                    {
                        // Create a BDD for each character in the string and concatenate them.
                        string? str = node.Str;
                        Debug.Assert(str is not null);
                        bool ignoreCase = (node.Options & RegexOptions.IgnoreCase) != 0;
                        var nodes = new SymbolicRegexNode<BDD>[str.Length];
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            nodes[i] = _builder.CreateSingleton(CharSetSolver.Instance.CharConstraint(str[i]));
                        }
                        return _builder.CreateConcat(nodes, tryCreateFixedLengthMarker);
                    }

                // Joins

                case RegexNodeKind.Concatenate:
                    {
                        var children = new SymbolicRegexNode<BDD>[node.ChildCount()];
                        for (int i = 0; i < children.Length; ++i)
                        {
                            children[i] = ConvertToSymbolicRegexNode(node.Child(i), tryCreateFixedLengthMarker: false);
                        }
                        return _builder.CreateConcat(children, tryCreateFixedLengthMarker);
                    }

                case RegexNodeKind.Alternate:
                    {
                        // Alternations are created by creating an Or of all of its children.
                        // This Or needs to be "ordered" to achieve the same semantics as the backtracking engines.
                        var branches = new SymbolicRegexNode<BDD>[node.ChildCount()];
                        for (int i = 0; i < branches.Length; i++)
                        {
                            branches[i] = ConvertToSymbolicRegexNode(node.Child(i), tryCreateFixedLengthMarker);
                        }
                        return _builder.OrderedOr(branches);
                    }

                // Loops

                case RegexNodeKind.Oneloop:
                case RegexNodeKind.Onelazy:
                case RegexNodeKind.Notoneloop:
                case RegexNodeKind.Notonelazy:
                    {
                        // Create a BDD that represents the character, then create a loop around it.
                        bool ignoreCase = (node.Options & RegexOptions.IgnoreCase) != 0;
                        BDD bdd = CharSetSolver.Instance.CharConstraint(node.Ch);
                        if (node.IsNotoneFamily)
                        {
                            bdd = CharSetSolver.Instance.Not(bdd);
                        }
                        return _builder.CreateLoop(_builder.CreateSingleton(bdd), node.Kind is RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy, node.M, node.N);
                    }

                case RegexNodeKind.Setloop:
                case RegexNodeKind.Setlazy:
                    {
                        // Create a BDD that represents the set string, then create a loop around it.
                        string? set = node.Str;
                        Debug.Assert(set is not null);
                        BDD setBdd = CreateBDDFromSetString((node.Options & RegexOptions.IgnoreCase) != 0, set);
                        return _builder.CreateLoop(_builder.CreateSingleton(setBdd), node.Kind == RegexNodeKind.Setlazy, node.M, node.N);
                    }

                case RegexNodeKind.Loop:
                case RegexNodeKind.Lazyloop:
                    return _builder.CreateLoop(ConvertToSymbolicRegexNode(node.Child(0), tryCreateFixedLengthMarker: false), node.Kind == RegexNodeKind.Lazyloop, node.M, node.N);

                // Other constructs

                case RegexNodeKind.Capture when node.N == -1: // N == -1 because balancing groups aren't supported
                    int captureNum = RegexParser.MapCaptureNumber(node.M, _captureSparseMapping);
                    return _builder.CreateCapture(ConvertToSymbolicRegexNode(node.Child(0), tryCreateFixedLengthMarker), captureNum);

                case RegexNodeKind.Empty:
                case RegexNodeKind.UpdateBumpalong: // UpdateBumpalong is a directive relevant only to backtracking and can be ignored just like Empty
                    return _builder.Epsilon;

                case RegexNodeKind.Nothing:
                    return _builder._nothing;

                // Anchors

                case RegexNodeKind.Beginning:
                    return _builder.BeginningAnchor;

                case RegexNodeKind.Bol:
                    EnsureNewlinePredicateInitialized();
                    return _builder.BolAnchor;

                case RegexNodeKind.End:  // \z anchor
                    return _builder.EndAnchor;

                case RegexNodeKind.EndZ: // \Z anchor
                    EnsureNewlinePredicateInitialized();
                    return _builder.EndAnchorZ;

                case RegexNodeKind.Eol:
                    EnsureNewlinePredicateInitialized();
                    return _builder.EolAnchor;

                case RegexNodeKind.Boundary:
                    EnsureWordLetterPredicateInitialized();
                    return _builder.BoundaryAnchor;

                case RegexNodeKind.NonBoundary:
                    EnsureWordLetterPredicateInitialized();
                    return _builder.NonBoundaryAnchor;

                // Experimental / unsupported

#if DEBUG
                case RegexNodeKind.ExpressionConditional:
                    // Try to extract the special case representing complement or intersection
                    if (IsComplementedNode(node))
                    {
                        return _builder.Not(ConvertToSymbolicRegexNode(node.Child(0), tryCreateFixedLengthMarker: false));
                    }

                    if (TryGetIntersection(node, out List<RegexNode>? conjuncts))
                    {
                        var nested = new SymbolicRegexNode<BDD>[conjuncts.Count];
                        for (int i = 0; i < nested.Length; i++)
                        {
                            nested[i] = ConvertToSymbolicRegexNode(conjuncts[i], tryCreateFixedLengthMarker: false);
                        }
                        return _builder.And(nested);
                    }

                    goto default;
#endif

                default:
                    throw new NotSupportedException(SR.Format(SR.NotSupported_NonBacktrackingConflictingExpression, node.Kind switch
                    {
                        RegexNodeKind.Atomic or RegexNodeKind.Setloopatomic or RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloopatomic => SR.ExpressionDescription_AtomicSubexpressions,
                        RegexNodeKind.Backreference => SR.ExpressionDescription_Backreference,
                        RegexNodeKind.BackreferenceConditional => SR.ExpressionDescription_Conditional,
                        RegexNodeKind.Capture => SR.ExpressionDescription_BalancingGroup,
                        RegexNodeKind.ExpressionConditional => SR.ExpressionDescription_IfThenElse,
                        RegexNodeKind.NegativeLookaround => SR.ExpressionDescription_NegativeLookaround,
                        RegexNodeKind.PositiveLookaround => SR.ExpressionDescription_PositiveLookaround,
                        RegexNodeKind.Start => SR.ExpressionDescription_ContiguousMatches,
                        _ => UnexpectedNodeType(node)
                    }));

                    static string UnexpectedNodeType(RegexNode node)
                    {
                        // The default should never arise, since other node types are either supported
                        // or have been removed (e.g. Group) from the final parse tree.
                        string description = $"Unexpected ({nameof(RegexNodeKind)}: {node.Kind})";
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
                    _builder._wordLetterPredicateForAnchors = UnicodeCategoryConditions.WordLetterForAnchors;
                }
            }

            SymbolicRegexNode<BDD> ConvertSet(RegexNode node)
            {
                Debug.Assert(node.Kind == RegexNodeKind.Set);

                string? set = node.Str;
                Debug.Assert(set is not null);

                return _builder.CreateSingleton(CreateBDDFromSetString((node.Options & RegexOptions.IgnoreCase) != 0, set));
            }

#if DEBUG
            // TODO-NONBACKTRACKING: recognizing strictly only [] (RegexNode.Nothing), for example [0-[0]] would not be recognized
            bool IsNothing(RegexNode node) => node.Kind == RegexNodeKind.Nothing || (node.Kind == RegexNodeKind.Set && ConvertSet(node).IsNothing);

            bool IsDotStar(RegexNode node) => node.Kind == RegexNodeKind.Setloop && ConvertToSymbolicRegexNode(node, tryCreateFixedLengthMarker: false).IsAnyStar;

            bool IsIntersect(RegexNode node) => node.Kind == RegexNodeKind.ExpressionConditional && IsNothing(node.Child(2));

            bool TryGetIntersection(RegexNode node, [Diagnostics.CodeAnalysis.NotNullWhen(true)] out List<RegexNode>? conjuncts)
            {
                if (!IsIntersect(node))
                {
                    conjuncts = null;
                    return false;
                }

                conjuncts = new List<RegexNode>();
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

        /// <summary>Creates a BDD from the <see cref="RegexCharClass"/> set string to determine whether a char is in the set.</summary>
        /// <param name="ignoreCase">true if the RegexOptions.IgnoreCase option is set; otherwise, false.</param>
        /// <param name="set">The RegexCharClass set string.</param>
        /// <returns>A BDD that, when queried with a char, answers whether that char is in the specified set.</returns>
        private BDD CreateBDDFromSetString(bool ignoreCase, string set)
        {
            // If we're too deep on the stack, continue any recursion on another thread.
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(CreateBDDFromSetString, ignoreCase, set);
            }

            // Lazily-initialize the set cache on first use, since some expressions may not have character classes in them.
            _setBddCache ??= new Dictionary<(bool IgnoreCase, string Set), BDD>();

            // Try to get the cached BDD for the combined ignoreCase+set key.
            // If one doesn't yet exist, compute and populate it.
            ref BDD? result = ref CollectionsMarshal.GetValueRefOrAddDefault(_setBddCache, (ignoreCase, set), out _);
            return result ??= Compute(ignoreCase, set);

            // <summary>Parses the RegexCharClass set string and creates a BDD that represents the same condition.</summary>
            BDD Compute(bool ignoreCase, string set)
            {
                List<BDD> conditions = new();

                // The set string is composed of four parts: flags (which today are just for negation), ranges (a list
                // of pairs of values representing the ranges a character that matches the set could fall in (or if it's
                // negated, fall out of), categories (a list of codes based on UnicodeCategory values), and then optionally
                // another entire set string that's subtracted from the outer set string.  We parse each of those pieces
                // to build up a BDD that will return true if a char matches the set string, and otherwise false. This
                // BDD then is functionally equivalent to RegexCharClass.CharInClass.

                bool negate = RegexCharClass.IsNegated(set);

                // Handle ranges
                // A BDD is created for each range, and is then negated if the set is negated.  All of the BDDs for
                // all of the ranges are stored in a set of these "conditions", which will later have all of the BDDs
                // and'd (conjunction) together if the set is negated, or or'd (disjunction) together if not negated.
                List<(char First, char Last)>? ranges = RegexCharClass.ComputeRanges(set);
                if (ranges is not null)
                {
                    foreach ((char first, char last) in ranges)
                    {
                        BDD bdd = CharSetSolver.Instance.RangeConstraint(first, last);
                        if (negate)
                        {
                            bdd = CharSetSolver.Instance.Not(bdd);
                        }
                        conditions.Add(bdd);
                    }
                }

                // Handle categories
                int setLength = set[RegexCharClass.SetLengthIndex];
                int catLength = set[RegexCharClass.CategoryLengthIndex];
                int catStart = setLength + RegexCharClass.SetStartIndex;
                int i = catStart;
                while (i < catStart + catLength)
                {
                    // TODO: This logic should really be part of RegexCharClass, as it's parsing a set
                    // string and ideally such logic would be internal to RegexCharClass.  It could expose
                    // a helper that returns the set of specified UnicodeCategory's and whether they're negated.

                    // Singleton categories are stored as values whose code is 1 + the UnicodeCategory value.
                    // Thus -1 is applied to extract the actual code of the category. The category itself may be
                    // negated, e.g. \D instead of \d.
                    short categoryCode = (short)set[i++];
                    if (categoryCode != 0)
                    {
                        // Create a BDD for the UnicodeCategory.  If the category code is negative, the category
                        // is negated, but the whole set string may also be negated, so we need to negate the condition
                        // if one and only one of these negations occurs.
                        BDD cond = MapCategoryCodeToCondition((UnicodeCategory)(Math.Abs(categoryCode) - 1));
                        if ((categoryCode < 0) ^ negate)
                        {
                            cond = CharSetSolver.Instance.Not(cond);
                        }
                        conditions.Add(cond);
                        continue;
                    }

                    // Special case for a whole group G of categories surrounded by 0's.
                    // Essentially 0 C1 C2 ... Cn 0 ==> G = (C1 | C2 | ... | Cn)
                    categoryCode = (short)set[i++];
                    if (categoryCode == 0)
                    {
                        continue; // empty set of categories
                    }

                    // If the first catCode is negated, the group as a whole is negated
                    bool negatedGroup = categoryCode < 0;

                    // Collect individual category codes
                    var categoryCodes = new HashSet<UnicodeCategory>();
                    while (categoryCode != 0)
                    {
                        categoryCodes.Add((UnicodeCategory)Math.Abs((int)categoryCode) - 1);
                        categoryCode = (short)set[i++];
                    }

                    // Create a BDD that represents all of the categories or'd (disjunction) together (C1 | C2 | ... | Cn),
                    // then negate the result if necessary (noting that two negations cancel each other out... if the set
                    // is negated but then the group itself is also negated).  And add the resulting BDD to our set of conditions.
                    BDD bdd = MapCategoryCodeSetToCondition(categoryCodes);
                    if (negate ^ negatedGroup)
                    {
                        bdd = CharSetSolver.Instance.Not(bdd);
                    }
                    conditions.Add(bdd);
                }

                // Handle subtraction
                BDD? subtractorCond = null;
                if (set.Length > i)
                {
                    // The set has a subtractor-set at the end.
                    // All characters in the subtractor-set are excluded from the set.
                    // Note that the subtractor sets may be nested, e.g. in r=[a-z-[b-g-[cd]]]
                    // the subtractor set [b-g-[cd]] has itself a subtractor set [cd].
                    // Thus r is the set of characters between a..z except b,e,f,g
                    subtractorCond = CreateBDDFromSetString(ignoreCase, set.Substring(i));
                }

                // If there are no ranges and no groups then there are no conditions.
                // This situation arises in particular for RegexOptions.SingleLine with a . (dot),
                // which translates into a set string that accepts everything.
                BDD result = conditions.Count == 0 ?
                    (negate ? CharSetSolver.Instance.False : CharSetSolver.Instance.True) :
                    (negate ? CharSetSolver.Instance.And(CollectionsMarshal.AsSpan(conditions)) : CharSetSolver.Instance.Or(CollectionsMarshal.AsSpan(conditions)));

                // Now apply the subtracted condition if there is one.  As a subtly of Regex semantics,
                // the subtractor is not within the scope of the negation (if there is any negation).
                // Thus we subtract after applying any negation above rather than before.  Subtraction
                // is achieved by negating the subtraction (such that the result of the negation represents
                // things still to be accepted after subtraction) and then and'ing it with the result, effectively
                // masking off anything matched by the subtraction set.
                if (subtractorCond is not null)
                {
                    result = CharSetSolver.Instance.And(result, CharSetSolver.Instance.Not(subtractorCond));
                }

                return result;

                // <summary>Creates a BDD that matches when a character is part of any of the specified UnicodeCategory values.</summary>
                BDD MapCategoryCodeSetToCondition(HashSet<UnicodeCategory> catCodes)
                {
                    Debug.Assert(catCodes.Count > 0);

                    // \w is so common, to help speed up construction we special-case it by using
                    // the combined \w predicate rather than an or (disjunction) of the component categories.
                    // This is done by validating that all of the categories for \w are there, and then removing
                    // them all if they are and instead starting our BDD off as \w.
                    BDD? result = null;
                    if (catCodes.Contains(UnicodeCategory.UppercaseLetter) &&
                        catCodes.Contains(UnicodeCategory.LowercaseLetter) &&
                        catCodes.Contains(UnicodeCategory.TitlecaseLetter) &&
                        catCodes.Contains(UnicodeCategory.ModifierLetter) &&
                        catCodes.Contains(UnicodeCategory.OtherLetter) &&
                        catCodes.Contains(UnicodeCategory.NonSpacingMark) &&
                        catCodes.Contains(UnicodeCategory.DecimalDigitNumber) &&
                        catCodes.Contains(UnicodeCategory.ConnectorPunctuation))
                    {
                        catCodes.Remove(UnicodeCategory.UppercaseLetter);
                        catCodes.Remove(UnicodeCategory.LowercaseLetter);
                        catCodes.Remove(UnicodeCategory.TitlecaseLetter);
                        catCodes.Remove(UnicodeCategory.ModifierLetter);
                        catCodes.Remove(UnicodeCategory.OtherLetter);
                        catCodes.Remove(UnicodeCategory.NonSpacingMark);
                        catCodes.Remove(UnicodeCategory.DecimalDigitNumber);
                        catCodes.Remove(UnicodeCategory.ConnectorPunctuation);

                        result = UnicodeCategoryConditions.WordLetter;
                    }

                    // For any remaining categories, create a condition for each and
                    // or that into the resulting BDD.
                    foreach (UnicodeCategory cat in catCodes)
                    {
                        BDD cond = MapCategoryCodeToCondition(cat);
                        result = result is null ? cond : CharSetSolver.Instance.Or(result, cond);
                    }

                    Debug.Assert(result is not null);
                    return result;
                }

                // Gets the BDD for evaluating whether a character is part of the specified category.
                BDD MapCategoryCodeToCondition(UnicodeCategory code)
                {
                    Debug.Assert(Enum.IsDefined(code) || code == (UnicodeCategory)(RegexCharClass.SpaceConst - 1), $"Unknown category: {code}");
                    return code == (UnicodeCategory)(RegexCharClass.SpaceConst - 1) ?
                        UnicodeCategoryConditions.WhiteSpace :
                        UnicodeCategoryConditions.GetCategory(code);
                }
            }
        }
    }
}
