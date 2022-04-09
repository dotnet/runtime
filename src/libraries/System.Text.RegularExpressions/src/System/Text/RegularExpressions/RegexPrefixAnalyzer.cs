// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>Detects various forms of prefixes in the regular expression that can help FindFirstChars optimize its search.</summary>
    internal static class RegexPrefixAnalyzer
    {
        /// <summary>Computes the leading substring in <paramref name="node"/>; may be empty.</summary>
        public static string FindPrefix(RegexNode node)
        {
            var vsb = new ValueStringBuilder(stackalloc char[64]);
            Process(node, ref vsb);
            return vsb.ToString();

            // Processes the node, adding any prefix text to the builder.
            // Returns whether processing should continue with subsequent nodes.
            static bool Process(RegexNode node, ref ValueStringBuilder vsb)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    // If we're too deep on the stack, just give up finding any more prefix.
                    return false;
                }

                // We don't bother to handle reversed input, so process at most one node
                // when handling RightToLeft.
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;

                switch (node.Kind)
                {
                    // Concatenation
                    case RegexNodeKind.Concatenate:
                        {
                            int childCount = node.ChildCount();
                            for (int i = 0; i < childCount; i++)
                            {
                                if (!Process(node.Child(i), ref vsb))
                                {
                                    return false;
                                }
                            }
                            return !rtl;
                        }

                    // Alternation: find a string that's a shared prefix of all branches
                    case RegexNodeKind.Alternate:
                        {
                            int childCount = node.ChildCount();

                            // Store the initial branch into the target builder, keeping track
                            // of how much was appended. Any of this contents that doesn't overlap
                            // will every other branch will be removed before returning.
                            int initialLength = vsb.Length;
                            Process(node.Child(0), ref vsb);
                            int addedLength = vsb.Length - initialLength;

                            // Then explore the rest of the branches, finding the length
                            // of prefix they all share in common with the initial branch.
                            if (addedLength != 0)
                            {
                                var alternateSb = new ValueStringBuilder(64);

                                // Process each branch.  If we reach a point where we've proven there's
                                // no overlap, we can bail early.
                                for (int i = 1; i < childCount && addedLength != 0; i++)
                                {
                                    alternateSb.Length = 0;

                                    // Process the branch into a temporary builder.
                                    Process(node.Child(i), ref alternateSb);

                                    // Find how much overlap there is between this branch's prefix
                                    // and the smallest amount of prefix that overlapped with all
                                    // the previously seen branches.
                                    addedLength = Math.Min(addedLength, alternateSb.Length);
                                    for (int j = 0; j < addedLength; j++)
                                    {
                                        if (vsb[initialLength + j] != alternateSb[j])
                                        {
                                            addedLength = j;
                                            break;
                                        }
                                    }
                                }

                                alternateSb.Dispose();

                                // Then cull back on what was added based on the other branches.
                                vsb.Length = initialLength + addedLength;
                            }

                            // Don't explore anything after the alternation.  We could make this work if desirable,
                            // but it's currently not worth the extra complication.  The entire contents of every
                            // branch would need to be identical other than zero-width anchors/assertions.
                            return false;
                        }

                    // One character
                    case RegexNodeKind.One:
                        vsb.Append(node.Ch);
                        return !rtl;

                    // Multiple characters
                    case RegexNodeKind.Multi:
                        vsb.Append(node.Str);
                        return !rtl;

                    // Loop of one character
                    case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy when node.M > 0:
                        const int SingleCharIterationLimit = 32; // arbitrary cut-off to avoid creating super long strings unnecessarily
                        int count = Math.Min(node.M, SingleCharIterationLimit);
                        vsb.Append(node.Ch, count);
                        return count == node.N && !rtl;

                    // Loop of a node
                    case RegexNodeKind.Loop or RegexNodeKind.Lazyloop when node.M > 0:
                        {
                            const int NodeIterationLimit = 4; // arbitrary cut-off to avoid creating super long strings unnecessarily
                            int limit = Math.Min(node.M, NodeIterationLimit);
                            for (int i = 0; i < limit; i++)
                            {
                                if (!Process(node.Child(0), ref vsb))
                                {
                                    return false;
                                }
                            }
                            return limit == node.N && !rtl;
                        }

                    // Grouping nodes for which we only care about their single child
                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.Capture:
                        return Process(node.Child(0), ref vsb);

                    // Zero-width anchors and assertions
                    case RegexNodeKind.Bol:
                    case RegexNodeKind.Eol:
                    case RegexNodeKind.Boundary:
                    case RegexNodeKind.ECMABoundary:
                    case RegexNodeKind.NonBoundary:
                    case RegexNodeKind.NonECMABoundary:
                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Start:
                    case RegexNodeKind.EndZ:
                    case RegexNodeKind.End:
                    case RegexNodeKind.Empty:
                    case RegexNodeKind.UpdateBumpalong:
                    case RegexNodeKind.PositiveLookaround:
                    case RegexNodeKind.NegativeLookaround:
                        return true;

                    // Give up for anything else
                    default:
                        return false;
                }
            }
        }

        /// <summary>Finds sets at fixed-offsets from the beginning of the pattern/</summary>
        /// <param name="root">The RegexNode tree root.</param>
        /// <param name="thorough">true to spend more time finding sets (e.g. through alternations); false to do a faster analysis that's potentially more incomplete.</param>
        /// <returns>The array of found sets, or null if there aren't any.</returns>
        public static List<(char[]? Chars, string Set, int Distance)>? FindFixedDistanceSets(RegexNode root, bool thorough)
        {
            const int MaxLoopExpansion = 20; // arbitrary cut-off to avoid loops adding significant overhead to processing
            const int MaxFixedResults = 50; // arbitrary cut-off to avoid generating lots of sets unnecessarily

            // Find all fixed-distance sets.
            var results = new List<(char[]? Chars, string Set, int Distance)>();
            int distance = 0;
            TryFindFixedSets(root, results, ref distance, thorough);

            // Remove any sets that match everything; they're not helpful.  (This check exists primarily to weed
            // out use of . in Singleline mode.)
            bool hasAny = false;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Set == RegexCharClass.AnyClass)
                {
                    hasAny = true;
                    break;
                }
            }
            if (hasAny)
            {
                results.RemoveAll(s => s.Set == RegexCharClass.AnyClass);
            }

            // If we don't have any results, try harder to compute one for the starting character.
            // This is a more involved computation that can find things the fixed-distance investigation
            // doesn't.
            if (results.Count == 0)
            {
                string? charClass = FindFirstCharClass(root);
                if (charClass is not null)
                {
                    results.Add((null, charClass, 0));
                }

                if (results.Count == 0)
                {
                    return null;
                }
            }

            // For every entry, try to get the chars that make up the set, if there are few enough.
            Span<char> scratch = stackalloc char[5]; // max optimized by IndexOfAny today
            for (int i = 0; i < results.Count; i++)
            {
                (char[]? Chars, string Set, int Distance) result = results[i];
                if (!RegexCharClass.IsNegated(result.Set))
                {
                    int count = RegexCharClass.GetSetChars(result.Set, scratch);
                    if (count != 0)
                    {
                        result.Chars = scratch.Slice(0, count).ToArray();
                        results[i] = result;
                    }
                }
            }

            // Finally, try to move the "best" results to be earlier.  "best" here are ones we're able to search
            // for the fastest and that have the best chance of matching as few false positives as possible.
            results.Sort((s1, s2) =>
            {
                if (s1.Chars is not null && s2.Chars is not null)
                {
                    // Then of the ones that are the same length, prefer those with less frequent values.  The frequency is
                    // only an approximation, used as a tie-breaker when we'd otherwise effectively be picking randomly.  True
                    // frequencies will vary widely based on the actual data being searched, the language of the data, etc.
                    int c = SumFrequencies(s1.Chars).CompareTo(SumFrequencies(s2.Chars));
                    if (c != 0)
                    {
                        return c;
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static float SumFrequencies(char[] chars)
                    {
                        float sum = 0;
                        foreach (char c in chars)
                        {
                            // Lookup each character in the table.  For values > 255, this will end up truncating
                            // and thus we'll get skew in the data.  It's already a gross approximation, though,
                            // and it is primarily meant for disambiguation of ASCII letters.
                            sum += s_frequency[(byte)c];
                        }
                        return sum;
                    }
                }
                else if (s1.Chars is not null)
                {
                    // If s1 has chars and s2 doesn't, then s1 has fewer chars.
                    return -1;
                }
                else if (s2.Chars is not null)
                {
                    // If s2 has chars and s1 doesn't, then s2 has fewer chars.
                    return 1;
                }

                return s1.Distance.CompareTo(s2.Distance);
            });

            return results;

            // Starting from the specified root node, populates results with any characters at a fixed distance
            // from the node's starting position.  The function returns true if the entire contents of the node
            // is at a fixed distance, in which case distance will have been updated to include the full length
            // of the node.  If it returns false, the node isn't entirely fixed, in which case subsequent nodes
            // shouldn't be examined and distance should no longer be trusted.  However, regardless of whether it
            // returns true or false, it may have populated results, and all populated results are valid.
            static bool TryFindFixedSets(RegexNode node, List<(char[]? Chars, string Set, int Distance)> results, ref int distance, bool thorough)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    return false;
                }

                if ((node.Options & RegexOptions.RightToLeft) != 0)
                {
                    return false;
                }

                switch (node.Kind)
                {
                    case RegexNodeKind.One:
                        if (results.Count < MaxFixedResults)
                        {
                            string setString = RegexCharClass.OneToStringClass(node.Ch);
                            results.Add((null, setString, distance++));
                            return true;
                        }
                        return false;

                    case RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic when node.M > 0:
                        {
                            string setString = RegexCharClass.OneToStringClass(node.Ch);
                            int minIterations = Math.Min(node.M, MaxLoopExpansion);
                            int i = 0;
                            for (; i < minIterations && results.Count < MaxFixedResults; i++)
                            {
                                results.Add((null, setString, distance++));
                            }
                            return i == node.M && i == node.N;
                        }

                    case RegexNodeKind.Multi:
                        {
                            string s = node.Str!;
                            int i = 0;
                            for (; i < s.Length && results.Count < MaxFixedResults; i++)
                            {
                                string setString = RegexCharClass.OneToStringClass(s[i]);
                                results.Add((null, setString, distance++));
                            }
                            return i == s.Length;
                        }

                    case RegexNodeKind.Set:
                        if (results.Count < MaxFixedResults)
                        {
                            results.Add((null, node.Str!, distance++));
                            return true;
                        }
                        return false;

                    case RegexNodeKind.Setlazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic when node.M > 0:
                        {
                            int minIterations = Math.Min(node.M, MaxLoopExpansion);
                            int i = 0;
                            for (; i < minIterations && results.Count < MaxFixedResults; i++)
                            {
                                results.Add((null, node.Str!, distance++));
                            }
                            return i == node.M && i == node.N;
                        }

                    case RegexNodeKind.Notone:
                        // We could create a set out of Notone, but it will be of little value in helping to improve
                        // the speed of finding the first place to match, as almost every character will match it.
                        distance++;
                        return true;

                    case RegexNodeKind.Notonelazy or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic when node.M == node.N:
                        distance += node.M;
                        return true;

                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Bol:
                    case RegexNodeKind.Boundary:
                    case RegexNodeKind.ECMABoundary:
                    case RegexNodeKind.Empty:
                    case RegexNodeKind.End:
                    case RegexNodeKind.EndZ:
                    case RegexNodeKind.Eol:
                    case RegexNodeKind.NonBoundary:
                    case RegexNodeKind.NonECMABoundary:
                    case RegexNodeKind.UpdateBumpalong:
                    case RegexNodeKind.Start:
                    case RegexNodeKind.NegativeLookaround:
                    case RegexNodeKind.PositiveLookaround:
                        // Zero-width anchors and assertions.  In theory, for PositiveLookaround and NegativeLookaround we could also
                        // investigate them and use the learned knowledge to impact the generated sets, at least for lookaheads.
                        // For now, we don't bother.
                        return true;

                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.Group:
                    case RegexNodeKind.Capture:
                        return TryFindFixedSets(node.Child(0), results, ref distance, thorough);

                    case RegexNodeKind.Lazyloop or RegexNodeKind.Loop when node.M > 0:
                        // This effectively only iterates the loop once.  If deemed valuable,
                        // it could be updated in the future to duplicate the found results
                        // (updated to incorporate distance from previous iterations) and
                        // summed distance for all node.M iterations.  If node.M == node.N,
                        // this would then also allow continued evaluation of the rest of the
                        // expression after the loop.
                        TryFindFixedSets(node.Child(0), results, ref distance, thorough);
                        return false;

                    case RegexNodeKind.Concatenate:
                        {
                            int childCount = node.ChildCount();
                            for (int i = 0; i < childCount; i++)
                            {
                                if (!TryFindFixedSets(node.Child(i), results, ref distance, thorough))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }

                    case RegexNodeKind.Alternate when thorough:
                        {
                            int childCount = node.ChildCount();
                            bool allSameSize = true;
                            int? sameDistance = null;
                            var combined = new Dictionary<int, (RegexCharClass Set, int Count)>();

                            var localResults = new List<(char[]? Chars, string Set, int Distance)>();
                            for (int i = 0; i < childCount; i++)
                            {
                                localResults.Clear();
                                int localDistance = 0;
                                allSameSize &= TryFindFixedSets(node.Child(i), localResults, ref localDistance, thorough);

                                if (localResults.Count == 0)
                                {
                                    return false;
                                }

                                if (allSameSize)
                                {
                                    if (sameDistance is null)
                                    {
                                        sameDistance = localDistance;
                                    }
                                    else if (sameDistance.Value != localDistance)
                                    {
                                        allSameSize = false;
                                    }
                                }

                                foreach ((char[]? Chars, string Set, int Distance) fixedSet in localResults)
                                {
                                    if (combined.TryGetValue(fixedSet.Distance, out (RegexCharClass Set, int Count) value))
                                    {
                                        if (value.Set.TryAddCharClass(RegexCharClass.Parse(fixedSet.Set)))
                                        {
                                            value.Count++;
                                            combined[fixedSet.Distance] = value;
                                        }
                                    }
                                    else
                                    {
                                        combined[fixedSet.Distance] = (RegexCharClass.Parse(fixedSet.Set), 1);
                                    }
                                }
                            }

                            foreach (KeyValuePair<int, (RegexCharClass Set, int Count)> pair in combined)
                            {
                                if (results.Count >= MaxFixedResults)
                                {
                                    allSameSize = false;
                                    break;
                                }

                                if (pair.Value.Count == childCount)
                                {
                                    results.Add((null, pair.Value.Set.ToStringClass(), pair.Key + distance));
                                }
                            }

                            if (allSameSize)
                            {
                                Debug.Assert(sameDistance.HasValue);
                                distance += sameDistance.Value;
                                return true;
                            }

                            return false;
                        }

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Computes a character class for the first character in tree.  This uses a more robust algorithm
        /// than is used by TryFindFixedLiterals and thus can find starting sets it couldn't.  For example,
        /// fixed literals won't find the starting set for a*b, as the a isn't guaranteed and the b is at a
        /// variable position, but this will find [ab] as it's instead looking for anything that under any
        /// circumstance could possibly start a match.
        /// </summary>
        public static string? FindFirstCharClass(RegexNode root)
        {
            // Explore the graph, adding found chars into a result set, which is lazily initialized so that
            // we can initialize it to a parsed set if we discover one first (this is helpful not just for allocation
            // but because it enables supporting starting negated sets, which wouldn't work if they had to be merged
            // into a non-negated default set). If the operation returns true, we successfully explore all relevant nodes
            // in the graph.  If it returns false, we were unable to successfully explore all relevant nodes, typically
            // due to conflicts when trying to add characters into the result set, e.g. we may have read a negated set
            // and were then unable to merge into that a subsequent non-negated set.  If it returns null, it means the
            // whole pattern was nullable such that it could match an empty string, in which case we
            // can't make any statements about what begins a match.
            RegexCharClass? cc = null;
            return TryFindFirstCharClass(root, ref cc) == true ?
                cc!.ToStringClass() :
                null;

            // Walks the nodes of the expression looking for any node that could possibly match the first
            // character of a match, e.g. in `a*b*c+d`, we'd find [abc], or in `(abc|d*e)?[fgh]`, we'd find
            // [adefgh].  The function is called for each node, recurring into children where appropriate,
            // and returns:
            // - true if the child was successfully processed and represents a stopping point, e.g. a single
            //   char loop with a minimum bound greater than 0 such that nothing after that node in a
            //   concatenation could possibly match the first character.
            // - false if the child failed to be processed but needed to be, such that the results can't
            //   be trusted.  If any node returns false, the whole operation fails.
            // - null if the child was successfully processed but doesn't represent a stopping point, i.e.
            //   it's zero-width (e.g. empty, a lookaround, an anchor, etc.) or it could be zero-width
            //   (e.g. a loop with a min bound of 0).  A concatenation processing a child that returns
            //   null needs to keep processing the next child.
            static bool? TryFindFirstCharClass(RegexNode node, ref RegexCharClass? cc)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    // If we're too deep on the stack, give up.
                    return false;
                }

                switch (node.Kind)
                {
                    // Base cases where we have results to add to the result set. Add the values into the result set, if possible.
                    // If this is a loop and it has a lower bound of 0, then it's zero-width, so return null.
                    case RegexNodeKind.One or RegexNodeKind.Oneloop or RegexNodeKind.Onelazy or RegexNodeKind.Oneloopatomic:
                        if (cc is null || cc.CanMerge)
                        {
                            cc ??= new RegexCharClass();
                            cc.AddChar(node.Ch);
                            return node.Kind is RegexNodeKind.One || node.M > 0 ? true : null;
                        }
                        return false;

                    case RegexNodeKind.Notone or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Notonelazy:
                        if (cc is null || cc.CanMerge)
                        {
                            cc ??= new RegexCharClass();
                            if (node.Ch > 0)
                            {
                                // Add the range before the excluded char.
                                cc.AddRange((char)0, (char)(node.Ch - 1));
                            }
                            if (node.Ch < char.MaxValue)
                            {
                                // Add the range after the excluded char.
                                cc.AddRange((char)(node.Ch + 1), char.MaxValue);
                            }
                            return node.Kind is RegexNodeKind.Notone || node.M > 0 ? true : null;
                        }
                        return false;

                    case RegexNodeKind.Set or RegexNodeKind.Setloop or RegexNodeKind.Setlazy or RegexNodeKind.Setloopatomic:
                        {
                            bool setSuccess = false;
                            if (cc is null)
                            {
                                cc = RegexCharClass.Parse(node.Str!);
                                setSuccess = true;
                            }
                            else if (cc.CanMerge && RegexCharClass.Parse(node.Str!) is { CanMerge: true } setCc)
                            {
                                cc.AddCharClass(setCc);
                                setSuccess = true;
                            }
                            return
                                !setSuccess ? false :
                                node.Kind is RegexNodeKind.Set || node.M > 0 ? true :
                                null;
                        }

                    case RegexNodeKind.Multi:
                        if (cc is null || cc.CanMerge)
                        {
                            cc ??= new RegexCharClass();
                            cc.AddChar(node.Str![(node.Options & RegexOptions.RightToLeft) != 0 ? node.Str.Length - 1 : 0]);
                            return true;
                        }
                        return false;

                    // Zero-width elements.  These don't contribute to the starting set, so return null to indicate a caller
                    // should keep looking past them.
                    case RegexNodeKind.Empty:
                    case RegexNodeKind.Nothing:
                    case RegexNodeKind.Bol:
                    case RegexNodeKind.Eol:
                    case RegexNodeKind.Boundary:
                    case RegexNodeKind.NonBoundary:
                    case RegexNodeKind.ECMABoundary:
                    case RegexNodeKind.NonECMABoundary:
                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Start:
                    case RegexNodeKind.EndZ:
                    case RegexNodeKind.End:
                    case RegexNodeKind.UpdateBumpalong:
                    case RegexNodeKind.PositiveLookaround:
                    case RegexNodeKind.NegativeLookaround:
                        return null;

                    // Groups.  These don't contribute anything of their own, and are just pass-throughs to their children.
                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.Capture:
                        return TryFindFirstCharClass(node.Child(0), ref cc);

                    // Loops.  Like groups, these are mostly pass-through: if the child fails, then the whole operation needs
                    // to fail, and if the child is nullable, then the loop is as well.  However, if the child succeeds but
                    // the loop has a lower bound of 0, then the loop is still nullable.
                    case RegexNodeKind.Loop:
                    case RegexNodeKind.Lazyloop:
                        return TryFindFirstCharClass(node.Child(0), ref cc) switch
                        {
                            false => false,
                            null => null,
                            _ => node.M == 0 ? null : true,
                        };

                    // Concatenation.  Loop through the children as long as they're nullable.  The moment a child returns true,
                    // we don't need or want to look further, as that child represents non-zero-width and nothing beyond it can
                    // contribute to the starting character set.  The moment a child returns false, we need to fail the whole thing.
                    // If every child is nullable, then the concatenation is also nullable.
                    case RegexNodeKind.Concatenate:
                        {
                            int childCount = node.ChildCount();
                            for (int i = 0; i < childCount; i++)
                            {
                                bool? childResult = TryFindFirstCharClass(node.Child(i), ref cc);
                                if (childResult != null)
                                {
                                    return childResult;
                                }
                            }
                            return null;
                        }

                    // Alternation. Every child is its own fork/branch and contributes to the starting set.  As with concatenation,
                    // the moment any child fails, fail.  And if any child is nullable, the alternation is also nullable (since that
                    // zero-width path could be taken).  Otherwise, if every branch returns true, so too does the alternation.
                    case RegexNodeKind.Alternate:
                        {
                            int childCount = node.ChildCount();
                            bool anyChildWasNull = false;
                            for (int i = 0; i < childCount; i++)
                            {
                                bool? childResult = TryFindFirstCharClass(node.Child(i), ref cc);
                                if (childResult is null)
                                {
                                    anyChildWasNull = true;
                                }
                                else if (childResult == false)
                                {
                                    return false;
                                }
                            }
                            return anyChildWasNull ? null : true;
                        }

                    // Conditionals.  Just like alternation for their "yes"/"no" child branches.  If either returns false, return false.
                    // If either is nullable, this is nullable. If both return true, return true.
                    case RegexNodeKind.BackreferenceConditional:
                    case RegexNodeKind.ExpressionConditional:
                        int branchStart = node.Kind is RegexNodeKind.BackreferenceConditional ? 0 : 1;
                        return (TryFindFirstCharClass(node.Child(branchStart), ref cc), TryFindFirstCharClass(node.Child(branchStart + 1), ref cc)) switch
                        {
                            (false, _) or (_, false) => false,
                            (null, _) or (_, null) => null,
                            _ => true,
                        };

                    // Backreferences.  We can't easily make any claims about what content they might match, so just give up.
                    case RegexNodeKind.Backreference:
                        return false;
                }

                // Unknown node.
                Debug.Fail($"Unexpected node {node.Kind}");
                return false;
            }
        }

        /// <summary>
        /// Analyzes the pattern for a leading set loop followed by a non-overlapping literal. If such a pattern is found, an implementation
        /// can search for the literal and then walk backward through all matches for the loop until the beginning is found.
        /// </summary>
        public static (RegexNode LoopNode, (char Char, string? String, char[]? Chars) Literal)? FindLiteralFollowingLeadingLoop(RegexNode node)
        {
            if ((node.Options & RegexOptions.RightToLeft) != 0)
            {
                // As a simplification, ignore RightToLeft.
                return null;
            }

            // Find the first concatenation.  We traverse through atomic and capture nodes as they don't effect flow control.  (We don't
            // want to explore loops, even if they have a guaranteed iteration, because we may use information about the node to then
            // skip the node's execution in the matching algorithm, and we would need to special-case only skipping the first iteration.)
            while (node.Kind is RegexNodeKind.Atomic or RegexNodeKind.Capture)
            {
                node = node.Child(0);
            }
            if (node.Kind != RegexNodeKind.Concatenate)
            {
                return null;
            }

            // Bail if the first node isn't a set loop.  We treat any kind of set loop (Setloop, Setloopatomic, and Setlazy)
            // the same because of two important constraints: the loop must not have an upper bound, and the literal we look
            // for immediately following it must not overlap.  With those constraints, all three of these kinds of loops will
            // end up having the same semantics; in fact, if atomic optimizations are used, we will have converted Setloop
            // into a Setloopatomic (but those optimizations are disabled for NonBacktracking in general). This
            // could also be made to support Oneloopatomic and Notoneloopatomic, but the scenarios for that are rare.
            Debug.Assert(node.ChildCount() >= 2);
            RegexNode firstChild = node.Child(0);
            if (firstChild.Kind is not (RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy) || firstChild.N != int.MaxValue)
            {
                return null;
            }

            // Get the subsequent node.  An UpdateBumpalong may have been added as an optimization, but it doesn't have an
            // impact on semantics and we can skip it.
            RegexNode nextChild = node.Child(1);
            if (nextChild.Kind == RegexNodeKind.UpdateBumpalong)
            {
                if (node.ChildCount() == 2)
                {
                    return null;
                }
                nextChild = node.Child(2);
            }

            // If the subsequent node is a literal, we need to ensure it doesn't overlap with the prior set.
            // If there's no overlap, we have a winner.
            switch (nextChild.Kind)
            {
                case RegexNodeKind.One when !RegexCharClass.CharInClass(nextChild.Ch, firstChild.Str!):
                    return (firstChild, (nextChild.Ch, null, null));

                case RegexNodeKind.Multi when !RegexCharClass.CharInClass(nextChild.Str![0], firstChild.Str!):
                    return (firstChild, ('\0', nextChild.Str, null));

                case RegexNodeKind.Set when !RegexCharClass.IsNegated(nextChild.Str!):
                    Span<char> chars = stackalloc char[5]; // maximum number of chars optimized by IndexOfAny
                    chars = chars.Slice(0, RegexCharClass.GetSetChars(nextChild.Str!, chars));
                    if (!chars.IsEmpty)
                    {
                        foreach (char c in chars)
                        {
                            if (RegexCharClass.CharInClass(c, firstChild.Str!))
                            {
                                return null;
                            }
                        }

                        return (firstChild, ('\0', null, chars.ToArray()));
                    }
                    break;
            }

            // Otherwise, we couldn't find the pattern of an atomic set loop followed by a literal.
            return null;
        }

        /// <summary>Computes the leading anchor of a node.</summary>
        public static RegexNodeKind FindLeadingAnchor(RegexNode node) =>
            FindLeadingOrTrailingAnchor(node, leading: true);

        /// <summary>Computes the leading anchor of a node.</summary>
        public static RegexNodeKind FindTrailingAnchor(RegexNode node) =>
            FindLeadingOrTrailingAnchor(node, leading: false);

        /// <summary>Computes the leading or trailing anchor of a node.</summary>
        private static RegexNodeKind FindLeadingOrTrailingAnchor(RegexNode node, bool leading)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // We only recur for alternations, but with a really deep nesting of alternations we could potentially overflow.
                // In such a case, simply stop searching for an anchor.
                return RegexNodeKind.Unknown;
            }

            while (true)
            {
                switch (node.Kind)
                {
                    case RegexNodeKind.Bol:
                    case RegexNodeKind.Eol:
                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Start:
                    case RegexNodeKind.EndZ:
                    case RegexNodeKind.End:
                    case RegexNodeKind.Boundary:
                    case RegexNodeKind.ECMABoundary:
                        // Return any anchor found.
                        return node.Kind;

                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.Capture:
                        // For groups, continue exploring the sole child.
                        node = node.Child(0);
                        continue;

                    case RegexNodeKind.Concatenate:
                        // For concatenations, we expect primarily to explore its first (for leading) or last (for trailing) child,
                        // but we can also skip over certain kinds of nodes (e.g. Empty), and thus iterate through its children backward
                        // looking for the last we shouldn't skip.
                        {
                            int childCount = node.ChildCount();
                            RegexNode? child = null;
                            if (leading)
                            {
                                for (int i = 0; i < childCount; i++)
                                {
                                    if (node.Child(i).Kind is not (RegexNodeKind.Empty or RegexNodeKind.PositiveLookaround or RegexNodeKind.NegativeLookaround))
                                    {
                                        child = node.Child(i);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for (int i = childCount - 1; i >= 0; i--)
                                {
                                    if (node.Child(i).Kind is not (RegexNodeKind.Empty or RegexNodeKind.PositiveLookaround or RegexNodeKind.NegativeLookaround))
                                    {
                                        child = node.Child(i);
                                        break;
                                    }
                                }
                            }

                            if (child is not null)
                            {
                                node = child;
                                continue;
                            }

                            goto default;
                        }

                    case RegexNodeKind.Alternate:
                        // For alternations, every branch needs to lead or trail with the same anchor.
                        {
                            // Get the leading/trailing anchor of the first branch.  If there isn't one, bail.
                            RegexNodeKind anchor = FindLeadingOrTrailingAnchor(node.Child(0), leading);
                            if (anchor == RegexNodeKind.Unknown)
                            {
                                return RegexNodeKind.Unknown;
                            }

                            // Look at each subsequent branch and validate it has the same leading or trailing
                            // anchor.  If any doesn't, bail.
                            int childCount = node.ChildCount();
                            for (int i = 1; i < childCount; i++)
                            {
                                if (FindLeadingOrTrailingAnchor(node.Child(i), leading) != anchor)
                                {
                                    return RegexNodeKind.Unknown;
                                }
                            }

                            // All branches have the same leading/trailing anchor.  Return it.
                            return anchor;
                        }

                    default:
                        // For everything else, we couldn't find an anchor.
                        return RegexNodeKind.Unknown;
                }
            }
        }

        /// <summary>Percent occurrences in source text (100 * char count / total count).</summary>
        private static readonly float[] s_frequency = new float[]
        {
            0.000f /* '\x00' */, 0.000f /* '\x01' */, 0.000f /* '\x02' */, 0.000f /* '\x03' */, 0.000f /* '\x04' */, 0.000f /* '\x05' */, 0.000f /* '\x06' */, 0.000f /* '\x07' */,
            0.000f /* '\x08' */, 0.001f /* '\x09' */, 0.000f /* '\x0A' */, 0.000f /* '\x0B' */, 0.000f /* '\x0C' */, 0.000f /* '\x0D' */, 0.000f /* '\x0E' */, 0.000f /* '\x0F' */,
            0.000f /* '\x10' */, 0.000f /* '\x11' */, 0.000f /* '\x12' */, 0.000f /* '\x13' */, 0.003f /* '\x14' */, 0.000f /* '\x15' */, 0.000f /* '\x16' */, 0.000f /* '\x17' */,
            0.000f /* '\x18' */, 0.004f /* '\x19' */, 0.000f /* '\x1A' */, 0.000f /* '\x1B' */, 0.006f /* '\x1C' */, 0.006f /* '\x1D' */, 0.000f /* '\x1E' */, 0.000f /* '\x1F' */,
            8.952f /* '    ' */, 0.065f /* '   !' */, 0.420f /* '   "' */, 0.010f /* '   #' */, 0.011f /* '   $' */, 0.005f /* '   %' */, 0.070f /* '   &' */, 0.050f /* '   '' */,
            3.911f /* '   (' */, 3.910f /* '   )' */, 0.356f /* '   *' */, 2.775f /* '   +' */, 1.411f /* '   ,' */, 0.173f /* '   -' */, 2.054f /* '   .' */, 0.677f /* '   /' */,
            1.199f /* '   0' */, 0.870f /* '   1' */, 0.729f /* '   2' */, 0.491f /* '   3' */, 0.335f /* '   4' */, 0.269f /* '   5' */, 0.435f /* '   6' */, 0.240f /* '   7' */,
            0.234f /* '   8' */, 0.196f /* '   9' */, 0.144f /* '   :' */, 0.983f /* '   ;' */, 0.357f /* '   <' */, 0.661f /* '   =' */, 0.371f /* '   >' */, 0.088f /* '   ?' */,
            0.007f /* '   @' */, 0.763f /* '   A' */, 0.229f /* '   B' */, 0.551f /* '   C' */, 0.306f /* '   D' */, 0.449f /* '   E' */, 0.337f /* '   F' */, 0.162f /* '   G' */,
            0.131f /* '   H' */, 0.489f /* '   I' */, 0.031f /* '   J' */, 0.035f /* '   K' */, 0.301f /* '   L' */, 0.205f /* '   M' */, 0.253f /* '   N' */, 0.228f /* '   O' */,
            0.288f /* '   P' */, 0.034f /* '   Q' */, 0.380f /* '   R' */, 0.730f /* '   S' */, 0.675f /* '   T' */, 0.265f /* '   U' */, 0.309f /* '   V' */, 0.137f /* '   W' */,
            0.084f /* '   X' */, 0.023f /* '   Y' */, 0.023f /* '   Z' */, 0.591f /* '   [' */, 0.085f /* '   \' */, 0.590f /* '   ]' */, 0.013f /* '   ^' */, 0.797f /* '   _' */,
            0.001f /* '   `' */, 4.596f /* '   a' */, 1.296f /* '   b' */, 2.081f /* '   c' */, 2.005f /* '   d' */, 6.903f /* '   e' */, 1.494f /* '   f' */, 1.019f /* '   g' */,
            1.024f /* '   h' */, 3.750f /* '   i' */, 0.286f /* '   j' */, 0.439f /* '   k' */, 2.913f /* '   l' */, 1.459f /* '   m' */, 3.908f /* '   n' */, 3.230f /* '   o' */,
            1.444f /* '   p' */, 0.231f /* '   q' */, 4.220f /* '   r' */, 3.924f /* '   s' */, 5.312f /* '   t' */, 2.112f /* '   u' */, 0.737f /* '   v' */, 0.573f /* '   w' */,
            0.992f /* '   x' */, 1.067f /* '   y' */, 0.181f /* '   z' */, 0.391f /* '   {' */, 0.056f /* '   |' */, 0.391f /* '   }' */, 0.002f /* '   ~' */, 0.000f /* '\x7F' */,
            0.000f /* '\x80' */, 0.000f /* '\x81' */, 0.000f /* '\x82' */, 0.000f /* '\x83' */, 0.000f /* '\x84' */, 0.000f /* '\x85' */, 0.000f /* '\x86' */, 0.000f /* '\x87' */,
            0.000f /* '\x88' */, 0.000f /* '\x89' */, 0.000f /* '\x8A' */, 0.000f /* '\x8B' */, 0.000f /* '\x8C' */, 0.000f /* '\x8D' */, 0.000f /* '\x8E' */, 0.000f /* '\x8F' */,
            0.000f /* '\x90' */, 0.000f /* '\x91' */, 0.000f /* '\x92' */, 0.000f /* '\x93' */, 0.000f /* '\x94' */, 0.000f /* '\x95' */, 0.000f /* '\x96' */, 0.000f /* '\x97' */,
            0.000f /* '\x98' */, 0.000f /* '\x99' */, 0.000f /* '\x9A' */, 0.000f /* '\x9B' */, 0.000f /* '\x9C' */, 0.000f /* '\x9D' */, 0.000f /* '\x9E' */, 0.000f /* '\x9F' */,
            0.000f /* '\xA0' */, 0.000f /* '\xA1' */, 0.000f /* '\xA2' */, 0.000f /* '\xA3' */, 0.000f /* '\xA4' */, 0.000f /* '\xA5' */, 0.000f /* '\xA6' */, 0.000f /* '\xA7' */,
            0.000f /* '\xA8' */, 0.000f /* '\xA9' */, 0.000f /* '\xAA' */, 0.000f /* '\xAB' */, 0.000f /* '\xAC' */, 0.000f /* '\xAD' */, 0.000f /* '\xAE' */, 0.000f /* '\xAF' */,
            0.000f /* '\xB0' */, 0.000f /* '\xB1' */, 0.000f /* '\xB2' */, 0.000f /* '\xB3' */, 0.000f /* '\xB4' */, 0.000f /* '\xB5' */, 0.000f /* '\xB6' */, 0.000f /* '\xB7' */,
            0.000f /* '\xB8' */, 0.000f /* '\xB9' */, 0.000f /* '\xBA' */, 0.000f /* '\xBB' */, 0.000f /* '\xBC' */, 0.000f /* '\xBD' */, 0.000f /* '\xBE' */, 0.000f /* '\xBF' */,
            0.000f /* '\xC0' */, 0.000f /* '\xC1' */, 0.000f /* '\xC2' */, 0.000f /* '\xC3' */, 0.000f /* '\xC4' */, 0.000f /* '\xC5' */, 0.000f /* '\xC6' */, 0.000f /* '\xC7' */,
            0.000f /* '\xC8' */, 0.000f /* '\xC9' */, 0.000f /* '\xCA' */, 0.000f /* '\xCB' */, 0.000f /* '\xCC' */, 0.000f /* '\xCD' */, 0.000f /* '\xCE' */, 0.000f /* '\xCF' */,
            0.000f /* '\xD0' */, 0.000f /* '\xD1' */, 0.000f /* '\xD2' */, 0.000f /* '\xD3' */, 0.000f /* '\xD4' */, 0.000f /* '\xD5' */, 0.000f /* '\xD6' */, 0.000f /* '\xD7' */,
            0.000f /* '\xD8' */, 0.000f /* '\xD9' */, 0.000f /* '\xDA' */, 0.000f /* '\xDB' */, 0.000f /* '\xDC' */, 0.000f /* '\xDD' */, 0.000f /* '\xDE' */, 0.000f /* '\xDF' */,
            0.000f /* '\xE0' */, 0.000f /* '\xE1' */, 0.000f /* '\xE2' */, 0.000f /* '\xE3' */, 0.000f /* '\xE4' */, 0.000f /* '\xE5' */, 0.000f /* '\xE6' */, 0.000f /* '\xE7' */,
            0.000f /* '\xE8' */, 0.000f /* '\xE9' */, 0.000f /* '\xEA' */, 0.000f /* '\xEB' */, 0.000f /* '\xEC' */, 0.000f /* '\xED' */, 0.000f /* '\xEE' */, 0.000f /* '\xEF' */,
            0.000f /* '\xF0' */, 0.000f /* '\xF1' */, 0.000f /* '\xF2' */, 0.000f /* '\xF3' */, 0.000f /* '\xF4' */, 0.000f /* '\xF5' */, 0.000f /* '\xF6' */, 0.000f /* '\xF7' */,
            0.000f /* '\xF8' */, 0.000f /* '\xF9' */, 0.000f /* '\xFA' */, 0.000f /* '\xFB' */, 0.000f /* '\xFC' */, 0.000f /* '\xFD' */, 0.000f /* '\xFE' */, 0.000f /* '\xFF' */,
        };

        // The above table was generated programmatically with the following.  This can be augmented to incorporate additional data sources,
        // though it is only intended to be a rough approximation use when tie-breaking and we'd otherwise be picking randomly, so, it's something.
        // The frequencies may be wildly inaccurate when used with data sources different in nature than the training set, in which case we shouldn't
        // be much worse off than just picking randomly:
        //
        // using System.Runtime.InteropServices;
        //
        // var counts = new Dictionary<byte, long>();
        //
        // (string, string)[] rootsAndExtensions = new[]
        // {
        //     (@"d:\repos\runtime\src\", "*.cs"),   // C# files in dotnet/runtime
        //     (@"d:\Top25GutenbergBooks", "*.txt"), // Top 25 most popular books on Project Gutenberg
        // };
        //
        // foreach ((string root, string ext) in rootsAndExtensions)
        //     foreach (string path in Directory.EnumerateFiles(root, ext, SearchOption.AllDirectories))
        //         foreach (string line in File.ReadLines(path))
        //             foreach (char c in line.AsSpan().Trim())
        //                 CollectionsMarshal.GetValueRefOrAddDefault(counts, (byte)c, out _)++;
        //
        // long total = counts.Sum(i => i.Value);
        //
        // Console.WriteLine("/// <summary>Percent occurrences in source text (100 * char count / total count).</summary>");
        // Console.WriteLine("private static readonly float[] s_frequency = new float[]");
        // Console.WriteLine("{");
        // int i = 0;
        // for (int row = 0; row < 32; row++)
        // {
        //     Console.Write("   ");
        //     for (int col = 0; col < 8; col++)
        //     {
        //         counts.TryGetValue((byte)i, out long charCount);
        //         float frequency = (float)(charCount / (double)total) * 100;
        //         Console.Write($" {frequency:N3}f /* '{(i >= 32 && i < 127 ? $"   {(char)i}" : $"\\x{i:X2}")}' */,");
        //         i++;
        //     }
        //     Console.WriteLine();
        // }
        // Console.WriteLine("};");
    }
}
