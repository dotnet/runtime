// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>Detects various forms of prefixes in the regular expression that can help FindFirstChars optimize its search.</summary>
    internal static class RegexPrefixAnalyzer
    {
        /// <summary>Cache of ToString() strings for the ASCII chars.</summary>
        /// <remarks>The strings are lazily created on first use.</remarks>
        private static string[]? s_asciiCharStrings;

        /// <summary>Gets the ToString() string for the specified char.</summary>
        private static string GetCharString(char ch)
        {
            // If the character isn't ASCII, just ToString it.
            if (ch >= 128)
            {
                return ch.ToString();
            }

            // Use a lazily-initialized cache of strings for ASCII characters. The overall cache is initialized
            // with Interlocked.CompareExchange simply to avoid accidentally throwing out a lot of strings accidentally.

            string[] asciiCharString =
                s_asciiCharStrings ??
                Interlocked.CompareExchange(ref s_asciiCharStrings, new string[128], null) ??
                s_asciiCharStrings;

            return asciiCharString[ch] ??= ch.ToString();
        }

        /// <summary>Finds an array of multiple prefixes that a node can begin with.</summary>
        /// <param name="node">The node to search.</param>
        /// <param name="ignoreCase">true to find ordinal ignore-case prefixes; false for case-sensitive.</param>
        /// <returns>
        /// If a fixed set of prefixes is found, such that a match for this node is guaranteed to begin
        /// with one of those prefixes, an array of those prefixes is returned.  Otherwise, null.
        /// </returns>
        public static string[]? FindPrefixes(RegexNode node, bool ignoreCase)
        {
            // Minimum string length for prefixes to be useful. If any prefix has length 1,
            // then we're generally better off just using IndexOfAny with chars.
            const int MinPrefixLength = 2;

            // Arbitrary string length limit (with some wiggle room) to avoid creating strings that are longer than is useful and consuming too much memory.
            const int MaxPrefixLength = 8;

            // Arbitrary limit on the number of prefixes to find. If we find more than this, we're likely to be spending too much time finding prefixes that won't be useful.
            const int MaxPrefixes = 16;

            // Analyze the node to find prefixes.
            List<StringBuilder> results = [new StringBuilder()];
            FindPrefixesCore(node, results, ignoreCase);

            // If we found too many prefixes or if any found is too short, fail.
            if (results.Count > MaxPrefixes || !results.TrueForAll(sb => sb.Length >= MinPrefixLength))
            {
                return null;
            }

            // Return the prefixes.
            string[] resultStrings = new string[results.Count];
            for (int i = 0; i < results.Count; i++)
            {
                resultStrings[i] = results[i].ToString();
            }
            return resultStrings;

            // <summary>
            // Updates the results list with found prefixes. All existing strings in the list are treated as existing
            // discovered prefixes prior to the node being processed. The method returns true if subsequent nodes after
            // this one should be examined, or returns false if they shouldn't be because the node wasn't guaranteed
            // to be fully processed.
            // </summary>
            static bool FindPrefixesCore(RegexNode node, List<StringBuilder> results, bool ignoreCase)
            {
                // If we're too deep to analyze further, we can't trust what we've already computed, so stop iterating.
                // Also bail if any of our results is already hitting the threshold
                if (!StackHelper.TryEnsureSufficientExecutionStack() ||
                    !results.TrueForAll(sb => sb.Length < MaxPrefixLength))
                {
                    return false;
                }

                // These limits are approximations. We'll stop trying to make strings longer once we exceed the max length,
                // and if we exceed the max number of prefixes by a non-trivial amount, we'll fail the operation.
                Span<char> setChars = stackalloc char[MaxPrefixes]; // limit how many chars we get from a set based on the max prefixes we care about

                // Loop down the left side of the tree, looking for a starting node we can handle. We only loop through
                // atomic and capture nodes, as the child is guaranteed to execute once, as well as loops with a positive
                // minimum and thus at least one guaranteed iteration.
                while (true)
                {
                    switch (node.Kind)
                    {
                        // These nodes are all guaranteed to execute at least once, so we can just
                        // skip through them to their child.
                        case RegexNodeKind.Atomic:
                        case RegexNodeKind.Capture:
                        case RegexNodeKind.Loop or RegexNodeKind.Lazyloop when node.M > 0:
                            node = node.Child(0);
                            continue;

                        // Zero-width anchors and assertions don't impact a prefix and may be skipped over.
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

                        // If we hit a single character, we can just return that character.
                        // This is only relevant for case-sensitive searches, as for case-insensitive we'd have sets for anything
                        // that produces a different result when case-folded, or for strings composed entirely of characters that
                        // don't participate in case conversion. Single character loops are handled the same as single characters
                        // up to the min iteration limit. We can continue processing after them as well if they're repeaters such
                        // that their min and max are the same.
                        case RegexNodeKind.One or RegexNodeKind.Oneloop or RegexNodeKind.Onelazy or RegexNodeKind.Oneloopatomic when !ignoreCase || !RegexCharClass.ParticipatesInCaseConversion(node.Ch):
                            {
                                int reps = node.Kind is RegexNodeKind.One ? 1 : node.M;
                                foreach (StringBuilder sb in results)
                                {
                                    sb.Append(node.Ch, reps);
                                }
                            }
                            return node.Kind is RegexNodeKind.One || node.M == node.N;

                        // If we hit a string, we can just return that string.
                        // As with One above, this is only relevant for case-sensitive searches.
                        case RegexNodeKind.Multi:
                            if (!ignoreCase)
                            {
                                foreach (StringBuilder sb in results)
                                {
                                    sb.Append(node.Str);
                                }
                            }
                            else
                            {
                                // If we're ignoring case, then only append up through characters that don't participate in case conversion.
                                // If there are any beyond that, we can't go further and need to stop with what we have.
                                foreach (char c in node.Str!)
                                {
                                    if (RegexCharClass.ParticipatesInCaseConversion(c))
                                    {
                                        return false;
                                    }

                                    foreach (StringBuilder sb in results)
                                    {
                                        sb.Append(c);
                                    }
                                }
                            }
                            return true;

                        // For case-sensitive,  try to extract the characters that comprise it, and if there are
                        // any and there aren't more than the max number of prefixes, we can return
                        // them each as a prefix. Effectively, this is an alternation of the characters
                        // that comprise the set. For case-insensitive, we need the set to be two ASCII letters that case fold to the same thing.
                        // As with One and loops, set loops are handled the same as sets up to the min iteration limit.
                        case RegexNodeKind.Set or RegexNodeKind.Setloop or RegexNodeKind.Setlazy or RegexNodeKind.Setloopatomic when !RegexCharClass.IsNegated(node.Str!): // negated sets are too complex to analyze
                            {
                                int charCount = RegexCharClass.GetSetChars(node.Str!, setChars);
                                if (charCount == 0)
                                {
                                    return false;
                                }

                                int reps = node.Kind is RegexNodeKind.Set ? 1 : node.M;
                                if (!ignoreCase)
                                {
                                    int existingCount = results.Count;

                                    // Duplicate all of the existing strings for all of the new suffixes, other than the first.
                                    foreach (char suffix in setChars.Slice(1, charCount - 1))
                                    {
                                        for (int existing = 0; existing < existingCount; existing++)
                                        {
                                            StringBuilder newSb = new StringBuilder().Append(results[existing]);
                                            newSb.Append(suffix, reps);
                                            results.Add(newSb);
                                        }
                                    }

                                    // Then append the first suffix to all of the existing strings.
                                    for (int existing = 0; existing < existingCount; existing++)
                                    {
                                        results[existing].Append(setChars[0], reps);
                                    }
                                }
                                else
                                {
                                    // For ignore-case, we currently only handle the simple (but common) case of a single
                                    // ASCII character that case folds to the same char.
                                    if (!RegexCharClass.SetContainsAsciiOrdinalIgnoreCaseCharacter(node.Str!, setChars))
                                    {
                                        return false;
                                    }

                                    // Append it to each.
                                    foreach (StringBuilder sb in results)
                                    {
                                        sb.Append(setChars[1], reps);
                                    }
                                }
                            }
                            return node.Kind is RegexNodeKind.Set || node.N == node.M;

                        case RegexNodeKind.Concatenate:
                            {
                                int childCount = node.ChildCount();
                                for (int i = 0; i < childCount; i++)
                                {
                                    // Atomic and Capture nodes don't impact prefixes, so skip through them.
                                    // Unlike earlier, however, we can't skip through loops, as a loop with
                                    // more than one iteration impacts the matched sequence for the concatenation,
                                    // and since we need a minimum of one, we'd only be able to skip a loop with
                                    // both a min and max of 1, which in general is removed as superfluous during
                                    // tree optimization. We could keep track of having traversed a loop and then
                                    // stop processing the continuation after that, but that complexity isn't
                                    // currently worthwhile.
                                    if (!FindPrefixesCore(SkipThroughAtomicAndCapture(node.Child(i)), results, ignoreCase))
                                    {
                                        return false;
                                    }
                                }
                            }
                            return true;

                        // For alternations, we need to find a prefix for every branch; if we can't compute a
                        // prefix for any one branch, we can't trust the results and need to give up, since we don't
                        // know if our set of prefixes is complete.
                        case RegexNodeKind.Alternate:
                            {
                                // If there are more children than our maximum, just give up immediately, as we
                                // won't be able to get a prefix for every branch and have it be within our max.
                                int childCount = node.ChildCount();
                                Debug.Assert(childCount >= 2); // otherwise it would have been optimized out
                                if (childCount > MaxPrefixes)
                                {
                                    return false;
                                }

                                // Build up the list of all prefixes across all branches.
                                List<StringBuilder>? allBranchResults = null;
                                List<StringBuilder>? alternateBranchResults = [new StringBuilder()];
                                for (int i = 0; i < childCount; i++)
                                {
                                    _ = FindPrefixesCore(node.Child(i), alternateBranchResults, ignoreCase);

                                    Debug.Assert(alternateBranchResults.Count > 0);
                                    foreach (StringBuilder sb in alternateBranchResults)
                                    {
                                        if (sb.Length == 0)
                                        {
                                            return false;
                                        }
                                    }

                                    if (allBranchResults is null)
                                    {
                                        allBranchResults = alternateBranchResults;
                                        alternateBranchResults = [new StringBuilder()];
                                    }
                                    else
                                    {
                                        allBranchResults.AddRange(alternateBranchResults);
                                        alternateBranchResults.Clear();
                                        alternateBranchResults.Add(new StringBuilder());
                                    }
                                }

                                // At this point, we know we can successfully incorporate the alternation's results
                                // into the main results.

                                // Duplicate all of the existing strings for all of the new suffixes, other than the first.
                                int existingCount = results.Count;
                                for (int i = 1; i < allBranchResults!.Count; i++)
                                {
                                    StringBuilder suffix = allBranchResults[i];
                                    for (int existing = 0; existing < existingCount; existing++)
                                    {
                                        StringBuilder newSb = new StringBuilder().Append(results[existing]);
                                        newSb.Append(suffix);
                                        results.Add(newSb);
                                    }
                                }

                                // Then append the first suffix to all of the existing strings.
                                for (int existing = 0; existing < existingCount; existing++)
                                {
                                    results[existing].Append(allBranchResults[0]);
                                }
                            }

                            // We don't know that we fully processed every branch, so we can't iterate through what comes after this node.
                            // The results were successfully updated, but return false to indicate that nothing after this node should be examined.
                            return false;

                        // Something else we don't recognize, so stop iterating.
                        default:
                            return false;
                    }
                }
            }
        }

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
                                    addedLength = vsb.AsSpan(initialLength, addedLength).CommonPrefixLength(alternateSb.AsSpan());
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

        /// <summary>Computes the leading ordinal case-insensitive substring in <paramref name="node"/>.</summary>
        public static string? FindPrefixOrdinalCaseInsensitive(RegexNode node)
        {
            while (true)
            {
                // Search down the left side of the tree looking for a concatenation.  If we find one,
                // ask it for any ordinal case-insensitive prefix it has.
                switch (node.Kind)
                {
                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.Capture:
                    case RegexNodeKind.Loop or RegexNodeKind.Lazyloop when node.M > 0:
                        node = node.Child(0);
                        continue;

                    case RegexNodeKind.Concatenate:
                        node.TryGetOrdinalCaseInsensitiveString(0, node.ChildCount(), out _, out string? caseInsensitiveString, consumeZeroWidthNodes: true);
                        return caseInsensitiveString;

                    default:
                        return null;
                }
            }
        }

        /// <summary>Finds sets at fixed-offsets from the beginning of the pattern/</summary>
        /// <param name="root">The RegexNode tree root.</param>
        /// <param name="thorough">true to spend more time finding sets (e.g. through alternations); false to do a faster analysis that's potentially more incomplete.</param>
        /// <returns>The array of found sets, or null if there aren't any.</returns>
        public static List<RegexFindOptimizations.FixedDistanceSet>? FindFixedDistanceSets(RegexNode root, bool thorough)
        {
            const int MaxLoopExpansion = 20; // arbitrary cut-off to avoid loops adding significant overhead to processing
            const int MaxFixedResults = 50; // arbitrary cut-off to avoid generating lots of sets unnecessarily

            // Find all fixed-distance sets.
            var results = new List<RegexFindOptimizations.FixedDistanceSet>();
            int distance = 0;
            TryFindRawFixedSets(root, results, ref distance, thorough);
#if DEBUG
            results.ForEach(r => Debug.Assert(
                !r.Negated && r.Chars is null && r.Range is null,
                $"{nameof(TryFindRawFixedSets)} should have only populated {nameof(r.Set)} and {nameof(r.Distance)}"));
#endif

            // Remove any sets that match everything; they're not helpful.  (This check exists primarily to weed
            // out use of . in Singleline mode, but also filters out explicit sets like [\s\S].)
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Set == RegexCharClass.AnyClass)
                {
                    results.RemoveAll(s => s.Set == RegexCharClass.AnyClass);
                    break;
                }
            }

            // If we don't have any results, try harder to compute one for the starting character.
            // This is a more involved computation that can find things the fixed-distance investigation
            // doesn't.
            if (results.Count == 0)
            {
                if (FindFirstCharClass(root) is not string charClass ||
                    charClass == RegexCharClass.AnyClass) // weed out match-all, same as above
                {
                    return null;
                }

                results.Add(new RegexFindOptimizations.FixedDistanceSet(null, charClass, 0));
            }

            // For every entry, try to get the chars that make up the set, if there are few enough.
            // For any for which we couldn't get the small chars list, see if we can get other useful info.
            Span<char> scratch = stackalloc char[128]; // limit based on what's currently efficiently handled by SearchValues
            for (int i = 0; i < results.Count; i++)
            {
                RegexFindOptimizations.FixedDistanceSet result = results[i];
                result.Negated = RegexCharClass.IsNegated(result.Set);

                int count = RegexCharClass.GetSetChars(result.Set, scratch);
                if (count > 0)
                {
                    result.Chars = scratch.Slice(0, count).ToArray();
                }

                // Prefer IndexOfAnyInRange over IndexOfAny for sets of 3-5 values that fit in a single range.
                if (thorough &&
                    (result.Chars is null || result.Chars.Length > 2) &&
                    RegexCharClass.TryGetSingleRange(result.Set, out char lowInclusive, out char highInclusive))
                {
                    result.Chars = null;
                    result.Range = (lowInclusive, highInclusive);
                }

                results[i] = result;
            }

            return results;

            // Starting from the specified root node, populates results with any characters at a fixed distance
            // from the node's starting position.  The function returns true if the entire contents of the node
            // is at a fixed distance, in which case distance will have been updated to include the full length
            // of the node.  If it returns false, the node isn't entirely fixed, in which case subsequent nodes
            // shouldn't be examined and distance should no longer be trusted.  However, regardless of whether it
            // returns true or false, it may have populated results, and all populated results are valid. All
            // FixedDistanceSet result will only have its Set string and Distance populated; the rest is left
            // to be populated by FindFixedDistanceSets after this returns.
            static bool TryFindRawFixedSets(RegexNode node, List<RegexFindOptimizations.FixedDistanceSet> results, ref int distance, bool thorough)
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
                            results.Add(new RegexFindOptimizations.FixedDistanceSet(null, setString, distance++));
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
                                results.Add(new RegexFindOptimizations.FixedDistanceSet(null, setString, distance++));
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
                                results.Add(new RegexFindOptimizations.FixedDistanceSet(null, setString, distance++));
                            }
                            return i == s.Length;
                        }

                    case RegexNodeKind.Set:
                        if (results.Count < MaxFixedResults)
                        {
                            results.Add(new RegexFindOptimizations.FixedDistanceSet(null, node.Str!, distance++));
                            return true;
                        }
                        return false;

                    case RegexNodeKind.Setlazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic when node.M > 0:
                        {
                            int minIterations = Math.Min(node.M, MaxLoopExpansion);
                            int i = 0;
                            for (; i < minIterations && results.Count < MaxFixedResults; i++)
                            {
                                results.Add(new RegexFindOptimizations.FixedDistanceSet(null, node.Str!, distance++));
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
                        return TryFindRawFixedSets(node.Child(0), results, ref distance, thorough);

                    case RegexNodeKind.Lazyloop or RegexNodeKind.Loop when node.M > 0:
                        // This effectively only iterates the loop once.  If deemed valuable,
                        // it could be updated in the future to duplicate the found results
                        // (updated to incorporate distance from previous iterations) and
                        // summed distance for all node.M iterations.  If node.M == node.N,
                        // this would then also allow continued evaluation of the rest of the
                        // expression after the loop.
                        TryFindRawFixedSets(node.Child(0), results, ref distance, thorough);
                        return false;

                    case RegexNodeKind.Concatenate:
                        {
                            int childCount = node.ChildCount();
                            for (int i = 0; i < childCount; i++)
                            {
                                if (!TryFindRawFixedSets(node.Child(i), results, ref distance, thorough))
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

                            var localResults = new List<RegexFindOptimizations.FixedDistanceSet>();
                            for (int i = 0; i < childCount; i++)
                            {
                                localResults.Clear();
                                int localDistance = 0;
                                allSameSize &= TryFindRawFixedSets(node.Child(i), localResults, ref localDistance, thorough);

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

                                foreach (RegexFindOptimizations.FixedDistanceSet fixedSet in localResults)
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
                                    results.Add(new RegexFindOptimizations.FixedDistanceSet(null, pair.Value.Set.ToStringClass(), pair.Key + distance));
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

        /// <summary>Sorts a set of fixed-distance set results from best to worst quality.</summary>
        public static void SortFixedDistanceSetsByQuality(List<RegexFindOptimizations.FixedDistanceSet> results) =>
            // Finally, try to move the "best" results to be earlier.  "best" here are ones we're able to search
            // for the fastest and that have the best chance of matching as few false positives as possible.
            results.Sort(static (s1, s2) =>
            {
                char[]? s1Chars = s1.Chars;
                char[]? s2Chars = s2.Chars;
                int s1CharsLength = s1Chars?.Length ?? 0;
                int s2CharsLength = s2Chars?.Length ?? 0;
                bool s1Negated = s1.Negated;
                bool s2Negated = s2.Negated;
                int s1RangeLength = s1.Range is not null ? GetRangeLength(s1.Range.Value, s1Negated) : 0;
                int s2RangeLength = s2.Range is not null ? GetRangeLength(s2.Range.Value, s2Negated) : 0;

                // If one set is negated and the other isn't, prefer the non-negated set. In general, negated
                // sets are large and thus likely to match more frequently, making them slower to search for.
                if (s1Negated != s2Negated)
                {
                    return s2Negated ? -1 : 1;
                }

                // If we extracted only a few chars and the sets are negated, they both represent very large
                // sets that are difficult to compare for quality.
                if (!s1Negated)
                {
                    Debug.Assert(!s2Negated);

                    // If both have chars, prioritize the one with the smaller frequency for those chars.
                    if (s1Chars is not null && s2Chars is not null)
                    {
                        // Prefer sets with less frequent values.  The frequency is only an approximation,
                        // used as a tie-breaker when we'd otherwise effectively be picking randomly.
                        // True frequencies will vary widely based on the actual data being searched, the language of the data, etc.
                        float s1Frequency = SumFrequencies(s1Chars);
                        float s2Frequency = SumFrequencies(s2Chars);

                        if (s1Frequency != s2Frequency)
                        {
                            return s1Frequency.CompareTo(s2Frequency);
                        }

                        if (!RegexCharClass.IsAscii(s1Chars) && !RegexCharClass.IsAscii(s2Chars))
                        {
                            // Prefer the set with fewer values.
                            return s1CharsLength.CompareTo(s2CharsLength);
                        }

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static float SumFrequencies(char[] chars)
                        {
                            float sum = 0;
                            foreach (char c in chars)
                            {
                                // Lookup each character in the table.  Values >= 128 are ignored
                                // and thus we'll get skew in the data.  It's already a gross approximation, though,
                                // and it is primarily meant for disambiguation of ASCII letters.
                                if (c < 128)
                                {
                                    sum += Frequency[c];
                                }
                            }
                            return sum;
                        }
                    }

                    // If one has chars and the other has a range, prefer the shorter set.
                    if ((s1CharsLength > 0 && s2RangeLength > 0) || (s1RangeLength > 0 && s2CharsLength > 0))
                    {
                        int c = Math.Max(s1CharsLength, s1RangeLength).CompareTo(Math.Max(s2CharsLength, s2RangeLength));
                        if (c != 0)
                        {
                            return c;
                        }

                        // If lengths are the same, prefer the chars.
                        return s1CharsLength > 0 ? -1 : 1;
                    }

                    // If one has chars and the other doesn't, prioritize the one with chars.
                    if ((s1CharsLength > 0) != (s2CharsLength > 0))
                    {
                        return s1CharsLength > 0 ? -1 : 1;
                    }
                }

                // If one has a range and the other doesn't, prioritize the one with a range.
                if ((s1RangeLength > 0) != (s2RangeLength > 0))
                {
                    return s1RangeLength > 0 ? -1 : 1;
                }

                // If both have ranges, prefer the one that includes fewer characters.
                if (s1RangeLength > 0)
                {
                    return s1RangeLength.CompareTo(s2RangeLength);
                }

                // As a tiebreaker, prioritize the earlier one.
                return s1.Distance.CompareTo(s2.Distance);

                static int GetRangeLength((char LowInclusive, char HighInclusive) range, bool negated)
                {
                    int length = range.HighInclusive - range.LowInclusive + 1;
                    return negated ?
                        char.MaxValue + 1 - length :
                        length;
                }
            });

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
        public static (RegexNode LoopNode, (char Char, string? String, StringComparison StringComparison, char[]? Chars) Literal)? FindLiteralFollowingLeadingLoop(RegexNode node)
        {
            if ((node.Options & RegexOptions.RightToLeft) != 0)
            {
                // As a simplification, ignore RightToLeft.
                return null;
            }

            // Find the first concatenation.  We traverse through atomic and capture nodes as they don't effect flow control.  (We don't
            // want to explore loops, even if they have a guaranteed iteration, because we may use information about the node to then
            // skip the node's execution in the matching algorithm, and we would need to special-case only skipping the first iteration.)
            node = SkipThroughAtomicAndCapture(node);
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
            while (firstChild.Kind is RegexNodeKind.Atomic or RegexNodeKind.Capture)
            {
                firstChild = firstChild.Child(0);
            }
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
                    // If the UpdateBumpalong is the last node, nothing meaningful follows the set loop.
                    return null;
                }
                nextChild = node.Child(2);
            }

            // Is the set loop followed by a case-sensitive string we can search for?
            if (FindPrefix(nextChild) is { Length: >= 1 } prefix)
            {
                // The literal can be searched for as either a single char or as a string.
                // But we need to make sure that its starting character isn't part of the preceding
                // set, as then we can't know for certain where the set loop ends.
                return
                    RegexCharClass.CharInClass(prefix[0], firstChild.Str!) ? null :
                    prefix.Length == 1 ? (firstChild, (prefix[0], null, StringComparison.Ordinal, null)) :
                    (firstChild, ('\0', prefix, StringComparison.Ordinal, null));
            }

            // Is the set loop followed by an ordinal case-insensitive string we can search for? We could
            // search for a string with at least one char, but if it has only one, we're better off just
            // searching as a set, so we look for strings with at least two chars.
            if (FindPrefixOrdinalCaseInsensitive(nextChild) is { Length: >= 2 } ordinalCaseInsensitivePrefix)
            {
                // The literal can be searched for as a case-insensitive string. As with ordinal above,
                // though, we need to make sure its starting character isn't part of the previous set.
                // If that starting character participates in case conversion, then we need to test out
                // both casings (FindPrefixOrdinalCaseInsensitive will only return strings composed of
                // characters that either are ASCII or that don't participate in case conversion).
                Debug.Assert(
                    !RegexCharClass.ParticipatesInCaseConversion(ordinalCaseInsensitivePrefix[0]) ||
                    ordinalCaseInsensitivePrefix[0] < 128);

                if (RegexCharClass.ParticipatesInCaseConversion(ordinalCaseInsensitivePrefix[0]))
                {
                    if (RegexCharClass.CharInClass((char)(ordinalCaseInsensitivePrefix[0] | 0x20), firstChild.Str!) ||
                        RegexCharClass.CharInClass((char)(ordinalCaseInsensitivePrefix[0] & ~0x20), firstChild.Str!))
                    {
                        return null;
                    }
                }
                else if (RegexCharClass.CharInClass(ordinalCaseInsensitivePrefix[0], firstChild.Str!))
                {
                    return null;
                }

                return (firstChild, ('\0', ordinalCaseInsensitivePrefix, StringComparison.OrdinalIgnoreCase, null));
            }

            // Is the set loop followed by a set we can search for? Whereas the above helpers will drill down into
            // children as is appropriate, to examine a set here, we need to drill in ourselves. We can drill through
            // atomic and capture nodes, as they don't affect flow control, and into the left-most node of a concatenate,
            // as the first child is guaranteed next. We can also drill into a loop or lazy loop that has a guaranteed
            // iteration, for the same reason as with concatenate.
            while ((nextChild.Kind is RegexNodeKind.Atomic or RegexNodeKind.Capture or RegexNodeKind.Concatenate) ||
                   (nextChild.Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop && nextChild.M >= 1))
            {
                nextChild = nextChild.Child(0);
            }

            // If the resulting node is a set with at least one iteration, we can search for it.
            if (nextChild.IsSetFamily &&
                !RegexCharClass.IsNegated(nextChild.Str!) &&
                (nextChild.Kind is RegexNodeKind.Set || nextChild.M >= 1))
            {
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

                    return (firstChild, ('\0', null, StringComparison.Ordinal, chars.ToArray()));
                }
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

        /// <summary>Walk through a node's children as long as the nodes are atomic or capture.</summary>
        private static RegexNode SkipThroughAtomicAndCapture(RegexNode node)
        {
            while (node.Kind is RegexNodeKind.Atomic or RegexNodeKind.Capture)
            {
                node = node.Child(0);
            }
            return node;
        }

        /// <summary>Percent occurrences in source text (100 * char count / total count).</summary>
        private static ReadOnlySpan<float> Frequency =>
        [
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
        ];

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
        // Console.WriteLine("private static ReadOnlySpan<float> Frequency =>");
        // Console.WriteLine("[");
        // int i = 0;
        // for (int row = 0; row < 16; row++)
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
        // Console.WriteLine("];");
    }
}
