// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;

namespace System.Text.RegularExpressions.Symbolic
{
    internal sealed partial class SymbolicRegexMatcher<TSet>
    {
        /// <inheritdoc cref="Regex.Explore(bool, bool, bool, bool, bool)"/>
        [ExcludeFromCodeCoverage(Justification = "Currently only used for testing")]
        public override void Explore(bool includeDotStarred, bool includeReverse, bool includeOriginal, bool exploreDfa, bool exploreNfa)
        {
            lock (this)
            {
                // Track seen states to avoid exploring twice
                HashSet<MatchingState<TSet>> seen = new();
                // Use a queue for unexplored states
                // This results in a breadth-first exploration
                Queue<MatchingState<TSet>> toExplore = new();

                // Explore all initial states as requested
                if (includeDotStarred)
                    EnqueueAll(_dotstarredInitialStates, seen, toExplore);
                if (includeReverse)
                    EnqueueAll(_reverseInitialStates, seen, toExplore);
                if (includeOriginal)
                    EnqueueAll(_initialStates, seen, toExplore);

                if (exploreDfa)
                {
                    while (toExplore.Count > 0)
                    {
                        // Don't dequeue yet, because a transition might fail
                        MatchingState<TSet> state = toExplore.Peek();
                        // Include the special minterm for the last end-of-line if the state is sensitive to it
                        int maxMinterm = state.StartsWithLineAnchor ? _minterms!.Length : _minterms!.Length - 1;
                        // Explore successor states for each minterm
                        for (int mintermId = 0; mintermId <= maxMinterm; ++mintermId)
                        {
                            int offset = DeltaOffset(state.Id, mintermId);
                            if (!TryCreateNewTransition(state, mintermId, offset, true, out MatchingState<TSet>? nextState))
                                goto DfaLimitReached;
                            EnqueueIfUnseen(nextState, seen, toExplore);
                        }
                        // Safe to dequeue now that the state has been completely handled
                        toExplore.Dequeue();
                    }
                }

            DfaLimitReached:
                if (exploreNfa && toExplore.Count > 0)
                {
                    // DFA states are broken up into NFA states when they are alternations
                    MatchingState<TSet>[] toBreakUp = toExplore.ToArray();
                    toExplore.Clear();
                    foreach (MatchingState<TSet> dfaState in toBreakUp)
                    {
                        // Remove state from seen so that it can be added back in if necessary
                        seen.Remove(dfaState);
                        // Enqueue all elements of a top level alternation or the state itself
                        ForEachNfaState(dfaState.Node, dfaState.PrevCharKind, (this, seen, toExplore),
                            static (int nfaId, (SymbolicRegexMatcher<TSet> Matcher, HashSet<MatchingState<TSet>> Seen, Queue<MatchingState<TSet>> ToExplore) args) =>
                            {
                                MatchingState<TSet>? coreState = args.Matcher.GetState(args.Matcher.GetCoreStateId(nfaId));
                                EnqueueIfUnseen(coreState, args.Seen, args.ToExplore);
                            });
                    }

                    while (toExplore.Count > 0)
                    {
                        // NFA transitions can't fail, so its safe to dequeue here
                        MatchingState<TSet> state = toExplore.Dequeue();
                        // Include the special minterm for the last end-of-line if the state is sensitive to it
                        int maxMinterm = state.StartsWithLineAnchor ? _minterms.Length : _minterms.Length - 1;
                        // Explore successor states for each minterm
                        for (int mintermId = 0; mintermId <= maxMinterm; ++mintermId)
                        {
                            int nfaOffset = DeltaOffset(_nfaIdByCoreId[state.Id], mintermId);
                            int[] nextNfaStates = CreateNewNfaTransition(_nfaIdByCoreId[state.Id], mintermId, nfaOffset);
                            foreach (int nextNfaState in nextNfaStates)
                            {
                                EnqueueIfUnseen(GetState(GetCoreStateId(nextNfaState)), seen, toExplore);
                            }
                        }
                    }
                }
            }

            static void EnqueueAll(MatchingState<TSet>[] states, HashSet<MatchingState<TSet>> seen, Queue<MatchingState<TSet>> toExplore)
            {
                foreach (MatchingState<TSet> state in states)
                {
                    EnqueueIfUnseen(state, seen, toExplore);
                }
            }

            static void EnqueueIfUnseen(MatchingState<TSet> state, HashSet<MatchingState<TSet>> seen, Queue<MatchingState<TSet>> queue)
            {
                if (seen.Add(state))
                {
                    queue.Enqueue(state);
                }
            }
        }
    }
}
#endif
