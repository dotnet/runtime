// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ConversionResult = System.Text.RegularExpressions.Symbolic.DoublyLinkedList<System.Text.RegularExpressions.Symbolic.SymbolicRegexNode<System.Text.RegularExpressions.Symbolic.BDD>>;

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

        /// <summary>Converts the root <see cref="RegexNode"/> into its corresponding <see cref="SymbolicRegexNode{S}"/>.</summary>
        /// <param name="root">The root node to convert.</param>
        /// <returns>The generated <see cref="SymbolicRegexNode{S}"/> that corresponds to the supplied <paramref name="root"/>.</returns>
        internal SymbolicRegexNode<BDD> ConvertToSymbolicRegexNode(RegexNode root)
        {
            Debug.Assert(_builder is not null);

            // cache stores the intermediate results to avoid use of recursion
            // a singleton list in ConversionResult represents a non-concatenation
            // any non-singleton list represents a concatenation of those nodes
            // ConversionResult is a non-empty list
            Dictionary<RegexNode, ConversionResult> cache = new();

            //(node, mark, second) represents a work item such that if second=true then
            //the node has children that have been processed and the results are stored in the cache
            Stack<(RegexNode Node, bool TryToMarkFixedLength, bool IsSecondPush)> work = new();
            // for the root node the length marker check is set to true
            work.Push((root, true, false));

            while (work.Count > 0)
            {
                (RegexNode Node, bool TryToMarkFixedLength, bool IsSecondPush) top = work.Pop();
                RegexNode node = top.Node;

                // No pushed node may have been converted already
                Debug.Assert(!cache.ContainsKey(node));

                if (top.IsSecondPush)
                {
                    // only nodes with children are pushed two times
                    // at this point all the child nodes must have been converted
                    Debug.Assert(node.ChildCount() > 0);
                    Debug.Assert(CheckThatAllChildrenHaveBeenCached(node));

                    switch (node.Kind)
                    {
                        case RegexNodeKind.Concatenate:
                            {
                                ConversionResult nodes = cache[node.Child(0)];
                                //flatten all the remaining converted child nodes into the single nodes list
                                for (int i = 1; i < node.ChildCount(); ++i)
                                {
                                    nodes.Append(cache[node.Child(i)]);
                                }
                                //reuse the resulting list for the node itself
                                cache[node] = nodes;
                                break;
                            }

                        case RegexNodeKind.Alternate:
                            {
                                // Alternations are created by creating an Or of all of its children.
                                // This Or needs to be "ordered" to achieve the same semantics as the backtracking engines.
                                var branches = new SymbolicRegexNode<BDD>[node.ChildCount()];
                                for (int i = 0; i < branches.Length; i++)
                                {
                                    ConversionResult res = cache[node.Child(i)];
                                    //if the converted node of child i is a non-singleton list then convert it into
                                    //a corresponding concatenation node, while propagating the length marker flag
                                    branches[i] = res._size == 1 ? res._first._elem : _builder.CreateConcat(res, top.TryToMarkFixedLength);
                                }
                                cache[node] = new(_builder.OrderedOr(branches));
                                break;
                            }

                        case RegexNodeKind.Loop:
                        case RegexNodeKind.Lazyloop:
                            {
                                ConversionResult res = cache[node.Child(0)];
                                //convert a list of nodes into a concatenation, do not propagate the length marker flag inside the loop body
                                SymbolicRegexNode<BDD> body = res._size == 1 ? res._first._elem : _builder.CreateConcat(res, false);
                                cache[node] = new(_builder.CreateLoop(body, node.Kind == RegexNodeKind.Lazyloop, node.M, node.N));
                                break;
                            }

                        default:
                            {
                                //no other nodes besides captures can have been pushed twice at this point
                                Debug.Assert(node.Kind == RegexNodeKind.Capture && node.N == -1);

                                int captureNum = RegexParser.MapCaptureNumber(node.M, _captureSparseMapping);
                                ConversionResult res = cache[node.Child(0)];
                                // add capture start/end markers
                                res.InsertAtStart(_builder.CreateCaptureStart(captureNum));
                                res.InsertAtEnd(_builder.CreateCaptureEnd(captureNum));
                                cache[node] = res;
                                break;
                            }
                    }
                }
                else
                {
                    // at this point no child node may have been converted yet
                    CheckThatNoChildHasBeenCached(node);

                    // Handle each node kind as-is appropriate.
                    switch (node.Kind)
                    {
                        // Singletons and multis

                        case RegexNodeKind.One:
                            cache[node] = new(_builder.CreateSingleton(_builder._solver.CreateFromChar(node.Ch)));
                            break;

                        case RegexNodeKind.Notone:
                            cache[node] = new(_builder.CreateSingleton(_builder._solver.Not(_builder._solver.CreateFromChar(node.Ch))));
                            break;

                        case RegexNodeKind.Set:
                            cache[node] = new(ConvertSet(node));
                            break;

                        case RegexNodeKind.Multi:
                            {
                                // Create a BDD for each character in the string and concatenate them.
                                string? str = node.Str;
                                Debug.Assert(str is not null);
                                bool ignoreCase = (node.Options & RegexOptions.IgnoreCase) != 0;
                                if (str.Length == 0)
                                {
                                    cache[node] = new(_builder.Epsilon);
                                }
                                else
                                {
                                    ConversionResult nodes = new(_builder.CreateSingleton(_builder._solver.CreateFromChar(str[0])));
                                    for (int i = 1; i < str.Length; i++)
                                    {
                                        nodes.InsertAtEnd(_builder.CreateSingleton(_builder._solver.CreateFromChar(str[i])));
                                    }
                                    cache[node] = nodes;
                                }
                                break;
                            }


                        // The following five cases are the only node kinds that are pushed twice

                        // Joins

                        case RegexNodeKind.Concatenate:
                        case RegexNodeKind.Alternate:
                            {
                                //push the node for the second time
                                work.Push((node, top.TryToMarkFixedLength, true));
                                //propagate the length mark check only in case of alternation
                                bool mark = node.Kind == RegexNodeKind.Alternate && top.TryToMarkFixedLength;
                                for (int i = 0; i < node.ChildCount(); ++i)
                                {
                                    //push all the children to be converted
                                    work.Push((node.Child(i), mark, false));
                                }
                                break;
                            }

                        // General loops and supported captures

                        case RegexNodeKind.Loop:
                        case RegexNodeKind.Lazyloop:
                        case RegexNodeKind.Capture when node.N == -1: // N == -1 because balancing groups aren't supported
                            {
                                //push the node for the second time
                                work.Push((node, top.TryToMarkFixedLength, true));
                                //push the child node, but omit the length mark check
                                work.Push((node.Child(0), false, false));
                                break;
                            }

                        // Specialized loops

                        case RegexNodeKind.Oneloop:
                        case RegexNodeKind.Onelazy:
                        case RegexNodeKind.Notoneloop:
                        case RegexNodeKind.Notonelazy:
                            {
                                // Create a BDD that represents the character, then create a loop around it.
                                bool ignoreCase = (node.Options & RegexOptions.IgnoreCase) != 0;
                                BDD bdd = _builder._solver.CreateFromChar(node.Ch);
                                if (node.IsNotoneFamily)
                                {
                                    bdd = _builder._solver.Not(bdd);
                                }
                                cache[node] = new(_builder.CreateLoop(_builder.CreateSingleton(bdd), node.Kind is RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy, node.M, node.N));
                                break;
                            }

                        case RegexNodeKind.Setloop:
                        case RegexNodeKind.Setlazy:
                            {
                                // Create a BDD that represents the set string, then create a loop around it.
                                string? set = node.Str;
                                Debug.Assert(set is not null);
                                BDD setBdd = CreateBDDFromSetString((node.Options & RegexOptions.IgnoreCase) != 0, set);
                                cache[node] = new(_builder.CreateLoop(_builder.CreateSingleton(setBdd), node.Kind == RegexNodeKind.Setlazy, node.M, node.N));
                                break;
                            }

                        case RegexNodeKind.Empty:
                        case RegexNodeKind.UpdateBumpalong: // UpdateBumpalong is a directive relevant only to backtracking and can be ignored just like Empty
                            cache[node] = new(_builder.Epsilon);
                            break;

                        case RegexNodeKind.Nothing:
                            cache[node] = new(_builder._nothing);
                            break;

                        // Anchors

                        case RegexNodeKind.Beginning:
                            cache[node] = new(_builder.BeginningAnchor);
                            break;

                        case RegexNodeKind.Bol:
                            EnsureNewlinePredicateInitialized();
                            cache[node] = new(_builder.BolAnchor);
                            break;

                        case RegexNodeKind.End:  // \z anchor
                            cache[node] = new(_builder.EndAnchor);
                            break;

                        case RegexNodeKind.EndZ: // \Z anchor
                            EnsureNewlinePredicateInitialized();
                            cache[node] = new(_builder.EndAnchorZ);
                            break;

                        case RegexNodeKind.Eol:
                            EnsureNewlinePredicateInitialized();
                            cache[node] = new(_builder.EolAnchor);
                            break;

                        case RegexNodeKind.Boundary:
                            EnsureWordLetterPredicateInitialized();
                            cache[node] = new(_builder.BoundaryAnchor);
                            break;

                        case RegexNodeKind.NonBoundary:
                            EnsureWordLetterPredicateInitialized();
                            cache[node] = new(_builder.NonBoundaryAnchor);
                            break;

                        // unsupported

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
                }
            }

            Debug.Assert(cache.ContainsKey(root));
            ConversionResult rootres = cache[root];

            // only a top-level concatenation or capture node can result in a non-singleton list
            Debug.Assert(rootres._size == 1 || root.Kind == RegexNodeKind.Concatenate || root.Kind == RegexNodeKind.Capture);

            // if the root node is a concatenation then the converted concatenation is built with length marker check being true
            SymbolicRegexNode<BDD> result = rootres._size == 1 ? rootres._first._elem : _builder.CreateConcat(rootres, true);
            return result;


            void EnsureNewlinePredicateInitialized()
            {
                // Initialize the \n set in the builder if it has not been updated already
                if (_builder._newLineSet.Equals(_builder._solver.Empty))
                {
                    _builder._newLineSet = _builder._solver.CreateFromChar('\n');
                }
            }

            void EnsureWordLetterPredicateInitialized()
            {
                // Initialize the word letter set based on the Unicode definition of it if it was not updated already
                if (_builder._wordLetterForBoundariesSet.Equals(_builder._solver.Empty))
                {
                    // Use the set including joiner and non-joiner
                    _builder._wordLetterForBoundariesSet = UnicodeCategoryConditions.WordLetterForAnchors((CharSetSolver)_builder._solver);
                }
            }

            bool CheckThatAllChildrenHaveBeenCached(RegexNode node)
            {
                for (int i=0; i < node.ChildCount(); ++i)
                {
                    if (!cache.ContainsKey(node.Child(i)))
                        return false;
                }
                return true;
            }

            bool CheckThatNoChildHasBeenCached(RegexNode node)
            {
                for (int i = 0; i < node.ChildCount(); ++i)
                {
                    if (cache.ContainsKey(node.Child(i)))
                        return false;
                }
                return true;
            }

            SymbolicRegexNode<BDD> ConvertSet(RegexNode node)
            {
                Debug.Assert(node.Kind == RegexNodeKind.Set);

                string? set = node.Str;
                Debug.Assert(set is not null);

                return _builder.CreateSingleton(CreateBDDFromSetString((node.Options & RegexOptions.IgnoreCase) != 0, set));
            }

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
                var charSetSolver = (CharSetSolver)_builder._solver;

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
                        BDD bdd = charSetSolver.CreateSetFromRange(first, last);
                        if (negate)
                        {
                            bdd = charSetSolver.Not(bdd);
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
                            cond = charSetSolver.Not(cond);
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
                        bdd = charSetSolver.Not(bdd);
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
                    (negate ? charSetSolver.Empty : charSetSolver.Full) :
                    (negate ? charSetSolver.And(CollectionsMarshal.AsSpan(conditions)) : charSetSolver.Or(CollectionsMarshal.AsSpan(conditions)));

                // Now apply the subtracted condition if there is one.  As a subtly of Regex semantics,
                // the subtractor is not within the scope of the negation (if there is any negation).
                // Thus we subtract after applying any negation above rather than before.  Subtraction
                // is achieved by negating the subtraction (such that the result of the negation represents
                // things still to be accepted after subtraction) and then and'ing it with the result, effectively
                // masking off anything matched by the subtraction set.
                if (subtractorCond is not null)
                {
                    result = charSetSolver.And(result, charSetSolver.Not(subtractorCond));
                }

                return result;

                // <summary>Creates a BDD that matches when a character is part of any of the specified UnicodeCategory values.</summary>
                BDD MapCategoryCodeSetToCondition(HashSet<UnicodeCategory> catCodes)
                {
                    Debug.Assert(catCodes.Count > 0);

                    // \w is so common, to help speed up construction we special-case it by using
                    // the combined \w set rather than an or (disjunction) of the component categories.
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

                        result = UnicodeCategoryConditions.WordLetter(charSetSolver);
                    }

                    // For any remaining categories, create a condition for each and
                    // or that into the resulting BDD.
                    foreach (UnicodeCategory cat in catCodes)
                    {
                        BDD cond = MapCategoryCodeToCondition(cat);
                        result = result is null ? cond : charSetSolver.Or(result, cond);
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
