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
            Debug.Assert(_builder._minterms is not null);

            // Track seen states to avoid exploring twice
            HashSet<DfaMatchingState<TSet>> seen = new();
            // Use a queue for unexplored states
            // This results in a breadth-first exploration
            Queue<DfaMatchingState<TSet>> toExplore = new();

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
                    DfaMatchingState<TSet> state = toExplore.Peek();
                    // Include the special minterm for the last end-of-line if the state is sensitive to it
                    int maxMinterm = state.StartsWithLineAnchor ? _builder._minterms.Length : _builder._minterms.Length - 1;
                    // Explore successor states for each minterm
                    for (int mintermId = 0; mintermId <= maxMinterm; ++mintermId)
                    {
                        int offset = (state.Id << _builder._mintermsLog) | mintermId;
                        if (!_builder.TryCreateNewTransition(state, mintermId, offset, true, out DfaMatchingState<TSet>? nextState))
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
                DfaMatchingState<TSet>[] toBreakUp = toExplore.ToArray();
                toExplore.Clear();
                foreach (DfaMatchingState<TSet> dfaState in toBreakUp)
                {
                    // Remove state from seen so that it can be added back in if necessary
                    seen.Remove(dfaState);
                    // Enqueue all elements of a top level alternation or the state itself
                    foreach (var element in dfaState.Node.EnumerateAlternationBranches())
                    {
                        int nfaState = _builder.CreateNfaState(element, dfaState.PrevCharKind);
                        EnqueueIfUnseen(_builder.GetCoreState(nfaState), seen, toExplore);
                    }
                }

                while (toExplore.Count > 0)
                {
                    // NFA transitions can't fail, so its safe to dequeue here
                    DfaMatchingState<TSet> state = toExplore.Dequeue();
                    // Include the special minterm for the last end-of-line if the state is sensitive to it
                    int maxMinterm = state.StartsWithLineAnchor ? _builder._minterms.Length : _builder._minterms.Length - 1;
                    // Explore successor states for each minterm
                    for (int mintermId = 0; mintermId <= maxMinterm; ++mintermId)
                    {
                        int nfaOffset = (_builder._nfaStateArrayInverse[state.Id] << _builder._mintermsLog) | mintermId;
                        int[] nextNfaStates = _builder.CreateNewNfaTransition(_builder._nfaStateArrayInverse[state.Id], mintermId, nfaOffset);
                        foreach (int nextNfaState in nextNfaStates)
                        {
                            EnqueueIfUnseen(_builder.GetCoreState(nextNfaState), seen, toExplore);
                        }
                    }
                }
            }

            static void EnqueueAll(DfaMatchingState<TSet>[] states, HashSet<DfaMatchingState<TSet>> seen, Queue<DfaMatchingState<TSet>> toExplore)
            {
                foreach (DfaMatchingState<TSet> state in states)
                {
                    EnqueueIfUnseen(state, seen, toExplore);
                }
            }

            static void EnqueueIfUnseen(DfaMatchingState<TSet> state, HashSet<DfaMatchingState<TSet>> seen, Queue<DfaMatchingState<TSet>> queue)
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
