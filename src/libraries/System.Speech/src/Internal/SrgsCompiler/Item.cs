// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    internal sealed class Item : ParseElementCollection, IItem
    {
        #region Constructors

        internal Item(Backend backend, Rule rule, int minRepeat, int maxRepeat, float repeatProbability, float weigth)
            : base(backend, rule)
        {
            // Validated by the caller
            _minRepeat = minRepeat;
            _maxRepeat = maxRepeat;
            _repeatProbability = repeatProbability;
        }

        #endregion

        #region Internal Method

        /// <summary>
        ///  Process the '/item' element.
        /// </summary>
        void IElement.PostParse(IElement parentElement)
        {
            // Special case of no words but only tags. Returns an error as the result is ambiguous
            // <tag>var res= 1;</tag>
            // <item repeat="2-2">
            //    <tag>res= res * 2;</tag>
            // </item>
            // Should the result be 2 or 4
            if (_maxRepeat != _minRepeat && _startArc != null && _startArc == _endArc && _endArc.IsEpsilonTransition && !_endArc.IsPropertylessTransition)
            {
                XmlParser.ThrowSrgsException((SRID.InvalidTagInAnEmptyItem));
            }

            // empty <item> or repeat count == 0
            if (_startArc == null || _maxRepeat == 0)
            {
                // Special Case: _maxRepeat = 0 => Epsilon transition.
                if (_maxRepeat == 0 && _startArc != null && _startArc.End != null)
                {
                    // Delete contents of Item.  Otherwise, we will end up with states disconnected to the rest of the rule.
                    State endState = _startArc.End;
                    _startArc.End = null;
                    _backend.DeleteSubGraph(endState);
                }
                // empty item, just add an epsilon transition.
                _startArc = _endArc = _backend.EpsilonTransition(_repeatProbability);
            }
            else
            {
                // Hard case if repeat count is not one
                if (_minRepeat != 1 || _maxRepeat != 1)
                {
                    // Duplicate the states/transitions graph as many times as repeat count

                    //Add a state before the start to be able to duplicate the graph
                    _startArc = InsertState(_startArc, _repeatProbability, Position.Before);
                    State startState = _startArc.End;

                    // If _maxRepeat = Infinite, add epsilon transition loop back to the start of this
                    if (_maxRepeat == int.MaxValue && _minRepeat == 1)
                    {
                        _endArc = InsertState(_endArc, 1.0f, Position.After);

                        AddEpsilonTransition(_endArc.Start, startState, 1 - _repeatProbability);
                    }
                    else
                    {
                        State currentStartState = startState;

                        // For each additional repeat count, clone a new subgraph and connect with appropriate transitions.
                        for (uint cnt = 1; cnt < _maxRepeat && cnt < 255; cnt++)
                        {
                            // Prepare to clone a new subgraph matching the <item> content.
                            State newStartState = _backend.CreateNewState(_endArc.Start.Rule);

                            // Clone subgraphs and update CurrentEndState.
                            State newEndState = _backend.CloneSubGraph(currentStartState, _endArc.Start, newStartState);

                            // Connect the last state with the first state
                            //_endArc.Start.OutArcs.Add (_endArc);
                            _endArc.End = newStartState;

                            // reset the _endArc
                            System.Diagnostics.Debug.Assert(newEndState.OutArcs.CountIsOne && Arc.CompareContent(_endArc, newEndState.OutArcs.First) == 0);
                            _endArc = newEndState.OutArcs.First;

                            if (_maxRepeat == int.MaxValue)
                            {
                                // If we are beyond _minRepeat, add epsilon transition from startState with (1-_repeatProbability).
                                if (cnt == _minRepeat - 1)
                                {
                                    // Create a new state and attach the last Arc to add
                                    _endArc = InsertState(_endArc, 1.0f, Position.After);

                                    AddEpsilonTransition(_endArc.Start, newStartState, 1 - _repeatProbability);
                                    break;
                                }
                            }
                            else if (cnt <= _maxRepeat - _minRepeat)
                            {
                                // If we are beyond _minRepeat, add epsilon transition from startState with (1-_repeatProbability).
                                AddEpsilonTransition(startState, newStartState, 1 - _repeatProbability);
                            }

                            // reset the current start state
                            currentStartState = newStartState;
                        }
                    }
                    // If _minRepeat == 0, add epsilon transition from currentEndState to FinalState with (1-_repeatProbability).
                    // but do not do it if the only transition is an epsilon
                    if (_minRepeat == 0 && (_startArc != _endArc || !_startArc.IsEpsilonTransition))
                    {
                        if (!_endArc.IsEpsilonTransition || _endArc.SemanticTagCount > 0)
                        {
                            _endArc = InsertState(_endArc, 1.0f, Position.After);
                        }
                        AddEpsilonTransition(startState, _endArc.Start, 1 - _repeatProbability);
                    }

                    // Remove the added startState if possible
                    _startArc = TrimStart(_startArc, _backend);
                }
            }

            // Add this item to the parent list
            base.PostParse((ParseElementCollection)parentElement);
        }

        #endregion

        #region Private Methods

        private void AddEpsilonTransition(State start, State end, float weight)
        {
            Arc epsilon = _backend.EpsilonTransition(weight);
            epsilon.Start = start;
            epsilon.End = end;
        }

        #endregion

        #region Private Fields

        private float _repeatProbability = 0.5f;

        private int _minRepeat = NotSet;

        private int _maxRepeat = NotSet;

        private const int NotSet = -1;

        #endregion
    }
}
