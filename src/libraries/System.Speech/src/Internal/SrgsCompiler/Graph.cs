// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace System.Speech.Internal.SrgsCompiler
{
    // Doubled chained linked list for fast removal of states.
    // Checks are made to ensure that the State pointers are never reused.

#if DEBUG
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GraphDebugDisplay))]
#endif
    internal class Graph : IEnumerable<State>
    {
        #region Internal Methods

        internal void Add(State state)
        {
            state.Init();
            if (_startState == null)
            {
                _curState = _startState = state;
            }
            else
            {
                _curState = _curState.Add(state);
            }
        }

        internal void Remove(State state)
        {
            if (state == _startState)
            {
                _startState = state.Next;
            }
            if (state == _curState)
            {
                _curState = state.Prev;
            }

            state.Remove();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (State item = _startState; item != null; item = item.Next)
            {
                yield return item;
            }
        }

        IEnumerator<State> IEnumerable<State>.GetEnumerator()
        {
            for (State item = _startState; item != null; item = item.Next)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Creates a new state handle in a given rule
        /// </summary>
        internal State CreateNewState(Rule rule)
        {
            uint hNewState = CfgGrammar.NextHandle;

            State newState = new(rule, hNewState);
            Add(newState);
#if DEBUG
            rule._cStates++;
#endif
            return newState;
        }

        /// <summary>
        /// Delete a state
        /// </summary>
        internal void DeleteState(State state)
        {
#if DEBUG
            state.Rule._cStates--;
#endif
            Remove(state);
        }

        /// <summary>
        /// Optimizes the grammar network by removing the epsilon states and merging
        /// duplicate transitions.
        /// </summary>
        internal void Optimize()
        {
            foreach (State state in this)
            {
                NormalizeTransitionWeights(state);
            }

#if DEBUG
            // Remove redundant epsilon transitions.
            int cStates = Count;
            RemoveEpsilonStates();
            if (Count != cStates)
            {
                System.Diagnostics.Trace.WriteLine("Grammar compiler, additional Epsilons could have been removed :" + (cStates - Count).ToString(CultureInfo.InvariantCulture));
                //System.Diagnostics.Debug.Assert (_states.Count == cStates);
            }
            // Remove duplicate transitions.
#endif
            MergeDuplicateTransitions();

#if DEBUG
            // Remove redundant epsilon transitions again now that identical epsilon transitions have been removed.
            cStates = Count;
            RemoveEpsilonStates();
            //System.Diagnostics.Debug.Assert (_states.Count == cStates);
            if (Count != cStates)
            {
                System.Diagnostics.Trace.WriteLine("Grammar compiler, additional Epsilons could have been removed post merge transition :" + (cStates - Count).ToString(CultureInfo.InvariantCulture));
            }

            // Verify the transition weights are normalized.
            foreach (State state in this)
            {
                double flSumWeights = 0.0f;                        // Compute the sum of the weights.
                int cArcs = 0;

                foreach (Arc arc in state.OutArcs)
                {
                    flSumWeights += arc.Weight;
                    cArcs++;
                }

                float maxWeightError = 0.00001f * cArcs;
                if (flSumWeights != 0.0f && maxWeightError - Math.Abs(flSumWeights - 1.0f) < 0)
                {
                    System.Diagnostics.Debug.Assert(true);
                }
            }
#endif
        }

        /// <summary>
        /// Description:
        ///     Change all transitions ending at SourceState to end at DestState, instead.
        ///     Replace references to SourceState with references to DestState before deleting SourceState.
        ///     - There may be additional duplicate input transitions at DestState after the move.
        ///
        /// Assumptions:
        /// - SourceState == !null, RuleInitialState, !DestState,   ...
        /// - DestState   ==  null, RuleInitialState, !SourceState, ...
        /// - SourceState.OutputArc.IsEmpty
        /// - !(SourceState == RuleInitialState AND DestState == null)
        ///
        /// Algorithm:
        /// - For each input transition into SourceState
        ///   - Transition.EndState = DestState
        ///   - If DestState != null, DestState.InputArcs += Transition
        ///   - SourceState.InputArcs -= Transition
        /// - SourceState.InputArcs.Clear()
        /// - If SourceState == RuleInitialState, RuleInitialState = DestState
        /// - Delete SourceState
        /// </summary>
        internal void MoveInputTransitionsAndDeleteState(State srcState, State destState)
        {
            System.Diagnostics.Debug.Assert(srcState != null);
            System.Diagnostics.Debug.Assert(srcState != destState);

            // For each input transition into SourceState, change EndState to DestState.
            List<Arc> arcs = srcState.InArcs.ToList();
            foreach (Arc arc in arcs)
            {
                // Change EndState to DestState
                arc.End = destState;
            }

            // Replace references to SourceState with references to DestState before deleting SourceState
            if (srcState.Rule._firstState == srcState) // Update RuleInitialState reference, if necessary
            {
                System.Diagnostics.Debug.Assert(destState != null);
                srcState.Rule._firstState = destState;
            }

            // Delete SourceState
            System.Diagnostics.Debug.Assert(srcState != null);
            //System.Diagnostics.Debug.Assert (srcState.InArcs.IsEmpty);
            System.Diagnostics.Debug.Assert(srcState.OutArcs.IsEmpty);
            DeleteState(srcState);  // Delete state from handle table
        }

        /// <summary>
        /// Description:
        ///     Change all transitions starting at SourceState to start at DestState, instead.
        ///     Deleting SourceState.
        ///     - The weights on the transitions have been properly adjusted.
        ///         The weights are not changed when moving transitions.
        ///     - There may be additional duplicate input transitions at DestState after the move.
        ///
        /// Assumptions:
        /// - SourceState == !null, !RuleInitialState, !DestState,   ...
        /// - DestState   == !null,  RuleInitialState, !SourceState, ...
        /// - SourceState.InputArc.IsEmpty
        ///
        /// Algorithm:
        /// - For each output transition from SourceState
        ///   - Transition.StartState = DestState
        ///   - DestState.OutputArcs += Transition
        /// - Delete SourceState
        /// </summary>
        internal void MoveOutputTransitionsAndDeleteState(State srcState, State destState)
        {
            System.Diagnostics.Debug.Assert(srcState != null);
            System.Diagnostics.Debug.Assert((destState != null) && (destState != srcState));
            System.Diagnostics.Debug.Assert(srcState.InArcs.IsEmpty);

            // For each output transition from SourceState, change StartState to DestState.
            List<Arc> arcs = srcState.OutArcs.ToList();
            foreach (Arc arc in arcs)
            {
                // Change StartState to DestState
                arc.Start = destState;
            }

            // Delete SourceState
            System.Diagnostics.Debug.Assert(srcState != null);
            System.Diagnostics.Debug.Assert(srcState.InArcs.IsEmpty);
            //System.Diagnostics.Debug.Assert (srcState.OutArcs.IsEmpty);
            DeleteState(srcState);  // Delete state from handle table
        }

        #endregion

        #region Internal Property

#if DEBUG
        internal State First
        {
            get
            {
                return _startState;
            }
        }

        internal int Count
        {
            get
            {
                int c = 0;
                for (State se = _startState; se != null; se = se.Next)
                {
                    c++;
                }
                return c;
            }
        }

#endif
        #endregion

        #region Private Methods

#if DEBUG
        /// <summary>
        ///   Description:
        ///        Removing epsilon states from the grammar network.
        ///        - There may be additional duplicate transitions after removing epsilon transitions.
        ///
        ///   Algorithm:
        ///   - For each State in the graph,
        ///     - If the state has a single input epsilon transition and is not the rule initial state,
        ///     - Move properties to the right, if necessary.
        ///     - If EpsilonTransition does not have properties and is not referenced by other properties,
        ///   - Delete EpsilonTransition.
        ///   - Multiply weight of all transitions from State by EpsilonTransition.Weight.
        ///   - MoveOutputTransitionsAndDeleteState(State, EpsilonTransition.StartState)
        ///    - If the state has a single output epsilon transition,
        ///    - Move properties to the left, if necessary.
        ///   - If EpsilonTransition does not have properties and is not referenced by other properties,
        ///   - Delete EpsilonTransition.
        ///   - MoveInputTransitionsAndDeleteState(State, EpsilonTransition.EndState)
        ///
        ///    Moving SemanticTag:
        ///    - InputEpsilonTransitions  can move its semantic tag ownerships/references to the right.
        ///    - OutputEpsilonTransitions can move its semantic tag ownerships/references to the left.
        /// </summary>
        private void RemoveEpsilonStates()
        {
            // For each state in the grammar graph, remove excess input/output epsilon transitions.
            for (State state = First, nextState = null; state != null; state = nextState)
            {
                nextState = state.Next;
                if (state.InArcs.CountIsOne && state.InArcs.First.IsEpsilonTransition && (state != state.Rule._firstState))
                {
                    // State has a single input epsilon transition and is not the rule initial state.
                    Arc epsilonArc = state.InArcs.First;

                    // Attempt to move properties referencing EpsilonArc to the right.
                    // Optimization can only be applied when the epsilon arc is not referenced by any properties.
                    if (MoveSemanticTagRight(epsilonArc))
                    {
                        // Delete the input epsilon transition
                        State pEpsilonStartState = epsilonArc.Start;
                        float flEpsilonWeight = epsilonArc.Weight;

                        DeleteTransition(epsilonArc);

                        // Multiply weight of all transitions from state by EpsilonWeight.
                        foreach (Arc arc in state.OutArcs)
                        {
                            arc.Weight *= flEpsilonWeight;
                        }

                        // Move all output transitions from state to pEpsilonStartState and delete state if appropriate.
                        if (state != pEpsilonStartState)
                        {
                            MoveOutputTransitionsAndDeleteState(state, pEpsilonStartState);
                        }
                    }
                }
                // Optimize output epsilon transition, if possible
                else if ((state.OutArcs.CountIsOne) && state.OutArcs.First.IsEpsilonTransition && (state != state.Rule._firstState))
                {
                    // State has a single output epsilon transition
                    Arc epsilonArc = state.OutArcs.First;

                    // Attempt to move properties referencing EpsilonArc to the left.
                    // Optimization can only be applied when the epsilon arc is not referenced by any properties
                    // and when the arc does not connect RuleInitialState to null.
                    if (!((state == state.Rule._firstState) && (epsilonArc.End == null)) && MoveSemanticTagLeft(epsilonArc))
                    {
                        // Delete the output epsilon transition
                        State pEpsilonEndState = epsilonArc.End;

                        DeleteTransition(epsilonArc);

                        // Move all input transitions from state to pEpsilonEndState and delete state if appropriate.
                        if (state != pEpsilonEndState)
                        {
                            MoveInputTransitionsAndDeleteState(state, pEpsilonEndState);
                        }
                    }
                }
            }
        }
#endif
        /// <summary>
        /// Description:
        ///     Remove duplicate transitions starting from the same state, or ending at the same state.
        ///
        /// Algorithm:
        /// - Add all states to ToDoList
        /// - For each state left in the ToDoList,
        ///   - Merge any duplicate output transitions.
        /// - Add all states to ToDoList in reverse order.
        /// - Remove duplicate transitions to null (special case since there is no state for FinalState)
        /// - For each state left in the ToDoList,
        ///   - Merge any duplicate input transitions.
        ///
        /// Notes:
        /// - For best optimization, we need to move semantic properties referencing the transitions.
        /// </summary>
        private void MergeDuplicateTransitions()
        {
            List<Arc> tempList = new();

            // Build collection of states with potential identical transition.
            foreach (State state in this)
            {
                if (state.OutArcs.ContainsMoreThanOneItem)
                {
                    // Merge identical transitions in arcs
                    MergeIdenticalTransitions(state.OutArcs, tempList);
                }
            }

            // Collection of states with potential transitions to merge
            Stack<State> mergeStates = new();

            RecursiveMergeDuplicatedOutputTransition(mergeStates);
            RecursiveMergeDuplicatedInputTransition(mergeStates);
        }

        private void RecursiveMergeDuplicatedInputTransition(Stack<State> mergeStates)
        {
            // Build collection of states with potential duplicate input transitions to merge.
            foreach (State state in this)
            {
                if (state.InArcs.ContainsMoreThanOneItem)
                {
                    MergeDuplicateInputTransitions(state.InArcs, mergeStates);
                }
            }

            // For each state in the collection, merge any duplicate input transitions.
            List<Arc> tempList = new();
            while (mergeStates.Count > 0)
            {
                State state = mergeStates.Pop();
                if (state.InArcs.ContainsMoreThanOneItem)
                {
                    // Merge identical transitions in arcs that may have been created
                    MergeIdenticalTransitions(state.InArcs, tempList);
                    MergeDuplicateInputTransitions(state.InArcs, mergeStates);
                }
            }
        }

        private void RecursiveMergeDuplicatedOutputTransition(Stack<State> mergeStates)
        {
            // Build collection of states with potential duplicate output transitions to merge.
            foreach (State state in this)
            {
                if (state.OutArcs.ContainsMoreThanOneItem)
                {
                    MergeDuplicateOutputTransitions(state.OutArcs, mergeStates);
                }
            }

            // For each state in the collection, merge any duplicate output transitions.
            List<Arc> tempList = new();
            while (mergeStates.Count > 0)
            {
                State state = mergeStates.Pop();
                if (state.OutArcs.ContainsMoreThanOneItem)
                {
                    // Merge identical transitions in arcs that may have been created
                    MergeIdenticalTransitions(state.OutArcs, tempList);
                    MergeDuplicateOutputTransitions(state.OutArcs, mergeStates);
                }
            }
        }

        /// <summary>
        /// Description:
        ///        Sort and iterate through the input arcs and remove duplicate input transitions.
        ///
        /// Algorithm:
        ///   - MergeIdenticalTransitions(Arcs)
        ///   - Sort the input transitions from the state (by content and # output arcs from start state)
        ///   - For each set of transitions with identical content and StartState.OutputArcs.Count() == 1
        ///            - Move semantic properties to the left, if necessary.
        ///            - Label the first property-less transition as CommonArc
        ///            - For each successive property-less transition (DuplicateArc)
        ///            - Delete DuplicateArc
        ///            - MoveInputTransitionsAndDeleteState(DuplicateArc.StartState, CommonArc.StartState)
        ///            - Add CommonArc.StartState to ToDoList if not there already.
        ///
        ///  Moving SemanticTag:
        ///  - Duplicate input transitions can move its semantic tag ownerships/references to the left.
        /// </summary>
        /// <param name="arcs">Collection of input transitions to collapse</param>
        /// <param name="mergeStates">Collection of states with potential transitions to merge</param>
        private void MergeDuplicateInputTransitions(ArcList arcs, Stack<State> mergeStates)
        {
            List<Arc> arcsToMerge = null;

            // Reference Arc
            Arc refArc = null;
            bool refSet = false;

            // Build a list of possible arcs to Merge
            foreach (Arc arc in arcs)
            {
                // Skip transitions whose end state has other incoming transitions or if the end state has more than one incoming transition
                bool skipTransition = arc.Start == null || !arc.Start.OutArcs.CountIsOne;
                // Find next set of duplicate output transitions (potentially with properties).
                if (refArc != null && Arc.CompareContent(arc, refArc) == 0)
                {
                    if (!skipTransition)
                    {
                        // Lazy init as entering this loop is a rare event
                        arcsToMerge ??= new List<Arc>();
                        // Add the first element
                        if (!refSet)
                        {
                            arcsToMerge.Add(refArc);
                            refSet = true;
                        }
                        arcsToMerge.Add(arc);
                    }
                }
                else
                {
                    // New word, reset everything
                    refArc = skipTransition ? null : arc;
                    refSet = false;
                }
            }

            // Combine the arcs if possible
            if (arcsToMerge != null)
            {
                // Sort the arc per content and output transition
                arcsToMerge.Sort(Arc.CompareForDuplicateInputTransitions);

                refArc = null;
                Arc commonArc = null;                   // Common property-less transition to merge into
                State commonStartState = null;
                bool fCommonStartStateChanged = false;      // Did CommonStartState change and need re-optimization?

                foreach (Arc arc in arcsToMerge)
                {
                    if (refArc == null || Arc.CompareContent(arc, refArc) != 0)
                    {
                        // Purge the last operations and reset all the local
                        refArc = arc;

                        // If CommonStartState changed, renormalize weights and add it to MergeStates for reoptimization.
                        if (fCommonStartStateChanged)
                        {
                            AddToMergeStateList(mergeStates, commonStartState);
                        }

                        // Reset the arcs
                        commonArc = null;
                        commonStartState = null;
                        fCommonStartStateChanged = false;
                    }

                    // For each property-less duplicate transition
                    Arc duplicatedArc = arc;
                    State duplicatedStartState = duplicatedArc.Start;

                    // Attempt to move properties referencing duplicate arc to the right.
                    // Optimization can only be applied when the duplicate arc is not referenced by any properties
                    // and the duplicate end state is not the RuleOutitalState.
                    if (MoveSemanticTagLeft(duplicatedArc))
                    {
                        // duplicatedArc != commonArc
                        if (commonArc != null)
                        {
                            if (!fCommonStartStateChanged)
                            {
                                // Processing first duplicate arc.
                                // Multiply the weights of transitions from CommonStartState by CommonArc.Weight.
                                foreach (Arc arcOut in commonStartState.OutArcs)
                                {
                                    arcOut.Weight *= commonArc.Weight;
                                }

                                fCommonStartStateChanged = true;  // Output transitions of CommonStartState changed.
                            }

                            // Multiply the weights of transitions from DuplicateStartState by DuplicateArc.Weight.
                            foreach (Arc arcOut in duplicatedStartState.OutArcs)
                            {
                                arcOut.Weight *= duplicatedArc.Weight;
                            }

                            duplicatedArc.Weight += commonArc.Weight;// Merge duplicate arc weight with common arc
                            Arc.CopyTags(commonArc, duplicatedArc, Direction.Left);
                            DeleteTransition(commonArc);    // Delete successive duplicate transitions

                            // Move outputs of duplicate state to common state; Delete duplicate state
                            MoveInputTransitionsAndDeleteState(commonStartState, duplicatedStartState);
                        }

                        // Label first property-less transition as CommonArc
                        commonArc = duplicatedArc;
                        commonStartState = duplicatedStartState;
                    }
                }
                // If CommonStartState changed, renormalize weights and add it to MergeStates for reoptimization.
                if (fCommonStartStateChanged)
                {
                    AddToMergeStateList(mergeStates, commonStartState);
                }
            }
        }

        /// <summary>
        /// Description:
        ///     Sort and iterate through the output arcs and remove duplicate output transitions.
        ///
        /// Algorithm:
        ///   - MergeIdenticalTransitions(Arcs)
        ///   - Sort the output transitions from the state (by content and # input arcs from end state)
        ///   - For each set of transitions with identical content, EndState != null, and EndState.InputArcs.Count() == 1
        ///     - Move semantic properties to the right, if necessary.
        ///     - Label the first property-less transition as CommonArc
        ///     - For each property-less transition (DuplicateArc) including CommonArc
        ///       - Multiply the weights of output transitions from DuplicateArc.EndState by DuplicateArc.Weight.
        ///       - If DuplicateArc != CommonArc
        ///       - CommonArc.Weight += DuplicateArc.Weight
        ///       - Delete DuplicateArc
        ///       - MoveOutputTransitionsAndDeleteState(DuplicateArc.EndState, CommonArc.EndState)
        ///     - Normalize weights of output transitions from CommonArc.EndState.
        ///     - Add CommonArc.EndtState to ToDoList if not there already.
        ///
        /// Moving SemanticTag:
        /// - Duplicate output transitions can move its semantic tag ownerships/references to the right.
        /// </summary>
        /// <param name="arcs">Collection of output transitions to collapse</param>
        /// <param name="mergeStates">Collection of states with potential transitions to merge</param>
        private void MergeDuplicateOutputTransitions(ArcList arcs, Stack<State> mergeStates)
        {
            List<Arc> arcsToMerge = null;

            // Reference Arc
            Arc refArc = null;
            bool refSet = false;

            // Build a list of possible arcs to Merge
            foreach (Arc arc in arcs)
            {
                // Skip transitions whose end state has other incoming transitions or if the end state has more than one incoming transition
                bool skipTransition = arc.End == null || !arc.End.InArcs.CountIsOne;
                // Find next set of duplicate output transitions (potentially with properties).
                if (refArc != null && Arc.CompareContent(arc, refArc) == 0)
                {
                    if (!skipTransition)
                    {
                        // Lazy init as entering this loop is a rare event
                        arcsToMerge ??= new List<Arc>();
                        // Add the first element
                        if (!refSet)
                        {
                            arcsToMerge.Add(refArc);
                            refSet = true;
                        }
                        arcsToMerge.Add(arc);
                    }
                }
                else
                {
                    // New word, reset everything
                    refArc = skipTransition ? null : arc;
                    refSet = false;
                }
            }

            // Combine the arcs if possible
            if (arcsToMerge != null)
            {
                // Sort the arc per content and output transition
                arcsToMerge.Sort(Arc.CompareForDuplicateOutputTransitions);

                refArc = null;
                Arc commonArc = null;                   // Common property-less transition to merge into
                State commonEndState = null;
                bool fCommonEndStateChanged = false;      // Did CommonEndState change and need re-optimization?

                foreach (Arc arc in arcsToMerge)
                {
                    if (refArc == null || Arc.CompareContent(arc, refArc) != 0)
                    {
                        // Purge the last operations and reset all the local
                        refArc = arc;

                        // If CommonEndState changed, renormalize weights and add it to MergeStates for reoptimization.
                        if (fCommonEndStateChanged)
                        {
                            AddToMergeStateList(mergeStates, commonEndState);
                        }

                        // Reset the arcs
                        commonArc = null;
                        commonEndState = null;
                        fCommonEndStateChanged = false;
                    }

                    // For each property-less duplicate transition
                    Arc duplicatedArc = arc;
                    State duplicatedEndState = duplicatedArc.End;

                    // Attempt to move properties referencing duplicate arc to the right.
                    // Optimization can only be applied when the duplicate arc is not referenced by any properties
                    // and the duplicate end state is not the RuleInitalState.
                    if ((duplicatedEndState != duplicatedEndState.Rule._firstState) && MoveSemanticTagRight(duplicatedArc))
                    {
                        // duplicatedArc != commonArc
                        if (commonArc != null)
                        {
                            if (!fCommonEndStateChanged)
                            {
                                // Processing first duplicate arc.
                                // Multiply the weights of transitions from CommonEndState by CommonArc.Weight.
                                foreach (Arc arcOut in commonEndState.OutArcs)
                                {
                                    arcOut.Weight *= commonArc.Weight;
                                }

                                fCommonEndStateChanged = true;  // Output transitions of CommonEndState changed.
                            }

                            // Multiply the weights of transitions from DuplicateEndState by DuplicateArc.Weight.
                            foreach (Arc arcOut in duplicatedEndState.OutArcs)
                            {
                                arcOut.Weight *= duplicatedArc.Weight;
                            }

                            duplicatedArc.Weight += commonArc.Weight;// Merge duplicate arc weight with common arc
                            Arc.CopyTags(commonArc, duplicatedArc, Direction.Right);
                            DeleteTransition(commonArc);    // Delete successive duplicate transitions

                            // Move outputs of duplicate state to common state; Delete duplicate state
                            MoveOutputTransitionsAndDeleteState(commonEndState, duplicatedEndState);
                        }

                        // Label first property-less transition as CommonArc
                        commonArc = duplicatedArc;
                        commonEndState = duplicatedEndState;
                    }
                }
                // If CommonEndState changed, renormalize weights and add it to MergeStates for reoptimization.
                if (fCommonEndStateChanged)
                {
                    AddToMergeStateList(mergeStates, commonEndState);
                }
            }
        }

        private static void AddToMergeStateList(Stack<State> mergeStates, State commonEndState)
        {
            NormalizeTransitionWeights(commonEndState);
            if (!mergeStates.Contains(commonEndState))
            {
                mergeStates.Push(commonEndState);
            }
        }

        /// <summary>
        /// Move any semantic tag ownership and optionally references to a unique
        /// previous arc, if possible.
        ///
        /// MoveReferences = true:  Return if arc is propertyless after the move.
        /// MoveReferences = false: Return if arc does not own semantic tag after the move.
        ///                         The arc can still be referenced by other semantic tags.
        /// </summary>
        internal static bool MoveSemanticTagLeft(Arc arc)
        {
            //       This changes the range of words spanned by the tag, which is a bug for SAPI grammars.
            State startState = arc.Start;

            // Can only move ownership/references if there is an unique input and output arc from the start state.
            // Cannot concatenate semantic tags.  (SemanticInterpretation script can arguably be concatenated.)
            // Cannot move ownership across RuleRef (to maintain semantics of $$ in SemanticTag JScript).
            // Cannot move semantic tag to special transition.  (SREngine may return multiple result arcs for the transition.)
            Arc previousArc = startState.InArcs.First;
            if ((startState.InArcs.CountIsOne) && (startState.OutArcs.CountIsOne) && CanTagsBeMoved(previousArc, arc))
            {
                // Move semantic tag ownership to the previous arc.
                Arc.CopyTags(arc, previousArc, Direction.Left);

                // Semantic tag and optionally references have been moved successfully.
                return true;
            }

            return arc.IsPropertylessTransition;
        }

        /// <summary>
        /// Move any semantic tag ownership and optionally references to a unique
        /// next arc, if possible.
        ///
        /// MoveReferences = true:  Return if arc is propertyless after the move.
        /// MoveReferences = false: Return if arc does not own semantic tag after the move.
        ///                         The arc can still be referenced by other semantic tags.
        ///
        /// Force semantic tag references to always move with tag.
        ///      This changes the range of words spanned by the tag, which is a bug for SAPI grammars.
        /// </summary>
        internal static bool MoveSemanticTagRight(Arc arc)
        {
            System.Diagnostics.Debug.Assert(arc.End != null);

            State endState = arc.End;

            // Can only move ownership/references if there is an unique input and output arc from the end state.
            // Cannot concatenate semantic tags.  (SemanticInterpretation script can arguably be concatenated.)
            // Cannot move ownership across RuleRef (to maintain semantics of $$ in SemanticTag JScript).
            // Cannot move semantic tag to special transition.  (SREngine may return multiple result arcs for the transition.)
            Arc pNextArc = endState.OutArcs.First;
            if ((endState.InArcs.CountIsOne) && (endState.OutArcs.CountIsOne) && CanTagsBeMoved(arc, pNextArc))
            {
                // Move semantic tag ownership to the next arc.
                Arc.CopyTags(arc, pNextArc, Direction.Right);

                // Semantic tag and optionally references have been moved successfully.
                return true;
            }

            return arc.IsPropertylessTransition;
        }

        /// <summary>
        /// Check if tags can be moved from a source arc to a destination
        ///     - Semantic interpretation. Tags cannot be moved if they would end up over a rule ref.
        ///     - Sapi properties. Tag can be put anywhere.
        /// </summary>
        internal static bool CanTagsBeMoved(Arc start, Arc end)
        {
            return (start.RuleRef == null) && (end.RuleRef == null) && (end.SpecialTransitionIndex == 0);
        }

        /// <summary>
        /// Description:
        ///        Detach and delete the specified transition from the graph.
        ///        Relocate or delete referencing semantic tags before deleting the transition.
        ///
        /// Special Case:
        ///        Arc.EndState == null
        ///        Arc.Optional == true
        /// </summary>
        private static void DeleteTransition(Arc arc)
        {
            // Arc cannot own SemanticTag
            System.Diagnostics.Debug.Assert(arc.SemanticTagCount == 0);

            // Arc cannot be referenced by SemanticTags
            System.Diagnostics.Debug.Assert(arc.IsPropertylessTransition);

            // Detach arc from start and end state
            arc.Start = arc.End = null;
        }

        /// <summary>
        /// Description:
        ///    Merge identical transitions with identical content, StartState, and EndState.
        ///
        /// </summary>
        private static void MergeIdenticalTransitions(ArcList arcs, List<Arc> identicalWords)
        {
            // Need at least two transitions to merge.
            System.Diagnostics.Debug.Assert(arcs.ContainsMoreThanOneItem);

            // Need at least two transitions to merge.
            List<List<Arc>> segmentsToDelete = null;
            Arc refArc = arcs.First;

            // Accumulate a set of transition to delete
            foreach (Arc arc in arcs)
            {
                if (Arc.CompareContent(refArc, arc) != 0)
                {
                    // Identical transition
                    if (identicalWords.Count >= 2)
                    {
                        identicalWords.Sort(Arc.CompareIdenticalTransitions);
                        segmentsToDelete ??= new List<List<Arc>>();

                        // Add the list of same words into a list for further processing.
                        // The expectation of having an identical transition is very low so the code
                        // may be a bit slow.
                        segmentsToDelete.Add(new List<Arc>(identicalWords));
                    }
                    identicalWords.Clear();
                }
                refArc = arc;
                identicalWords.Add(arc);
            }

            // Did the last word was replicated several times?
            if (identicalWords.Count >= 2)
            {
                MergeIdenticalTransitions(identicalWords);
            }
            identicalWords.Clear();

            // Process the accumulated words
            if (segmentsToDelete != null)
            {
                foreach (List<Arc> segmentToDelete in segmentsToDelete)
                {
                    MergeIdenticalTransitions(segmentToDelete);
                }
            }
        }

        /// <summary>
        /// Description:
        ///    Merge identical transitions with identical content, StartState, and EndState.
        ///
        /// Algorithm:
        /// - LastArc = Arcs[0]
        /// - For each Arc in Arcs[1-],
        ///   - If Arc is identical to LastArc,
        ///   - LastArc.Weight += Arc.Weight
        ///   - Delete Arc
        ///   - Else LastArc = Arc
        ///
        /// Moving SemanticTag:
        /// - Identical transitions have identical semantic tags.  Currently impossible to have identical
        /// non-null tags.
        /// - MoveSemanticTagReferences(DuplicateArc, CommonArc)
        /// </summary>
        private static void MergeIdenticalTransitions(List<Arc> identicalWords)
        {
            Collection<Arc> arcsToDelete = null;
            Arc refArc = null;
            foreach (Arc arc in identicalWords)
            {
                if (refArc != null && Arc.CompareIdenticalTransitions(refArc, arc) == 0)
                {
                    // Identical transition
                    arc.Weight += refArc.Weight;
                    refArc.ClearTags();
                    // delay the creation of the collection as this operation in infrequent.
                    arcsToDelete ??= new Collection<Arc>();
                    arcsToDelete.Add(refArc);
                }
                refArc = arc;
            }
            if (arcsToDelete != null)
            {
                foreach (Arc arc in arcsToDelete)
                {
                    // arc will become an orphan
                    DeleteTransition(arc);
                }
            }
        }

        /// <summary>
        /// Normalize the weights of output transitions from this state.
        /// </summary>
        private static void NormalizeTransitionWeights(State state)
        {
            float flSumWeights = 0.0f;

            // Compute the sum of the weights.
            foreach (Arc arc in state.OutArcs)
            {
                flSumWeights += arc.Weight;
            }

            // If Sum != 0 or 1, normalize transition weights by 1/Sum.
            if (!flSumWeights.Equals(0.0f) && !flSumWeights.Equals(1.0f))
            {
                float flNormalizationFactor = 1.0f / flSumWeights;

                foreach (Arc arc in state.OutArcs)
                {
                    arc.Weight *= flNormalizationFactor;
                }
            }
        }

        #endregion

        #region Private Types

#if DEBUG
        // Used by the debugger display attribute
        internal class GraphDebugDisplay
        {
            public GraphDebugDisplay(Graph states)
            {
                _states = states;
            }
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public State[] AKeys
            {
                get
                {
                    State[] states = new State[_states.Count];
                    int i = 0;
                    foreach (State state in _states)
                    {
                        states[i++] = state;
                    }
                    return states;
                }
            }

            private Graph _states;
        }
#endif

        #endregion

        #region Private Fields

        private State _startState;
        private State _curState;

        #endregion
    }
}
