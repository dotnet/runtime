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
    [ExcludeFromCodeCoverage(Justification = "Currently only used for testing")]
    internal static class DgmlWriter<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        /// <summary>Write the DFA or NFA in DGML format into the TextWriter.</summary>
        /// <param name="matcher">The <see cref="SymbolicRegexMatcher"/> for the regular expression.</param>
        /// <param name="writer">Writer to which the DGML is written.</param>
        /// <param name="nfa">True to create an NFA instead of a DFA.</param>
        /// <param name="addDotStar">True to prepend .*? onto the pattern (outside of the implicit root capture).</param>
        /// <param name="reverse">If true, then unwind the regex backwards (and <paramref name="addDotStar"/> is ignored).</param>
        /// <param name="maxStates">The approximate maximum number of states to include; less than or equal to 0 for no maximum.</param>
        /// <param name="maxLabelLength">maximum length of labels in nodes anything over that length is indicated with .. </param>
        public static void Write(
            TextWriter writer, SymbolicRegexMatcher<TSet> matcher,
            bool nfa = false, bool addDotStar = true, bool reverse = false, int maxStates = -1, int maxLabelLength = -1)
        {
            var charSetSolver = new CharSetSolver();
            var explorer = new DfaExplorer(matcher, nfa, addDotStar, reverse, maxStates);
            var nonEpsilonTransitions = new Dictionary<(int SourceState, int TargetState), List<(SymbolicRegexNode<TSet>?, TSet)>>();
            var epsilonTransitions = new List<Transition>();

            foreach (Transition transition in explorer.GetTransitions())
            {
                if (transition.IsEpsilon)
                {
                    epsilonTransitions.Add(transition);
                }
                else
                {
                    (int SourceState, int TargetState) p = (transition.SourceState, transition.TargetState);
                    if (!nonEpsilonTransitions.TryGetValue(p, out List<(SymbolicRegexNode<TSet>?, TSet)>? rules))
                    {
                        nonEpsilonTransitions[p] = rules = new List<(SymbolicRegexNode<TSet>?, TSet)>();
                    }

                    rules.Add(transition.Label);
                }
            }

            writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            writer.WriteLine("<DirectedGraph xmlns=\"http://schemas.microsoft.com/vs/2009/dgml\" ZoomLevel=\"1.5\" GraphDirection=\"TopToBottom\" >");
            writer.WriteLine("    <Nodes>");
            writer.WriteLine("        <Node Id=\"dfa\" Label=\" \" Group=\"Collapsed\" Category=\"DFA\" DFAInfo=\"{0}\" />", GetDFAInfo(explorer, charSetSolver));
            writer.WriteLine("        <Node Id=\"dfainfo\" Category=\"DFAInfo\" Label=\"{0}\"/>", GetDFAInfo(explorer, charSetSolver));
            foreach (int state in explorer.GetStates())
            {
                writer.WriteLine("        <Node Id=\"{0}\" Label=\"{0}\" Category=\"State\" Group=\"Collapsed\" StateInfo=\"{1}\">", state, explorer.DescribeState(state));
                if (state == explorer.InitialState)
                {
                    writer.WriteLine("            <Category Ref=\"InitialState\" />");
                }
                if (explorer.IsFinalState(state))
                {
                    writer.WriteLine("            <Category Ref=\"FinalState\" />");
                }
                writer.WriteLine("        </Node>");
                writer.WriteLine("        <Node Id=\"{0}info\" Label=\"{1}\" Category=\"StateInfo\"/>", state, explorer.DescribeState(state));
            }
            writer.WriteLine("    </Nodes>");
            writer.WriteLine("    <Links>");
            writer.WriteLine("        <Link Source=\"dfa\" Target=\"{0}\" Label=\"\" Category=\"StartTransition\" />", explorer.InitialState);
            writer.WriteLine("        <Link Source=\"dfa\" Target=\"dfainfo\" Label=\"\" Category=\"Contains\" />");

            foreach (Transition transition in epsilonTransitions)
            {
                writer.WriteLine("        <Link Source=\"{0}\" Target=\"{1}\" Category=\"EpsilonTransition\" />", transition.SourceState, transition.TargetState);
            }

            foreach (KeyValuePair<(int, int), List<(SymbolicRegexNode<TSet>?, TSet)>> transition in nonEpsilonTransitions)
            {
                string label = string.Join($",{Environment.NewLine} ", DescribeLabels(explorer, transition.Value, charSetSolver));
                string info = "";
                if (label.Length > (uint)maxLabelLength)
                {
                    info = $"FullLabel = \"{label}\" ";
                    label = string.Concat(label.AsSpan(0, maxLabelLength), "..");
                }

                writer.WriteLine($"        <Link Source=\"{transition.Key.Item1}\" Target=\"{transition.Key.Item2}\" Label=\"{label}\" Category=\"NonEpsilonTransition\" {info}/>");
            }

            foreach (int state in explorer.GetStates())
            {
                writer.WriteLine("        <Link Source=\"{0}\" Target=\"{0}info\" Category=\"Contains\" />", state);
            }

            writer.WriteLine("    </Links>");
            writer.WriteLine("    <Categories>");
            writer.WriteLine("        <Category Id=\"DFA\" Label=\"DFA\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"EpsilonTransition\" Label=\"Epsilon transition\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"StartTransition\" Label=\"Initial transition\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"FinalLabel\" Label=\"Final transition\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"FinalState\" Label=\"Final\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"SinkState\" Label=\"Sink state\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"EpsilonState\" Label=\"Epsilon state\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"InitialState\" Label=\"Initial\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"NonEpsilonTransition\" Label=\"Nonepsilon transition\" IsTag=\"True\" />");
            writer.WriteLine("        <Category Id=\"State\" Label=\"State\" IsTag=\"True\" />");
            writer.WriteLine("    </Categories>");
            writer.WriteLine("    <Styles>");
            writer.WriteLine("        <Style TargetType=\"Node\" GroupLabel=\"InitialState\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('InitialState')\" />");
            writer.WriteLine("            <Setter Property=\"Background\" Value=\"lightgray\" />");
            writer.WriteLine("            <Setter Property=\"MinWidth\" Value=\"0\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Node\" GroupLabel=\"FinalState\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('FinalState')\" />");
            writer.WriteLine("            <Setter Property=\"Background\" Value=\"lightgreen\" />");
            writer.WriteLine("            <Setter Property=\"StrokeThickness\" Value=\"4\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Node\" GroupLabel=\"State\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('State')\" />");
            writer.WriteLine("            <Setter Property=\"Stroke\" Value=\"black\" />");
            writer.WriteLine("            <Setter Property=\"Background\" Value=\"white\" />");
            writer.WriteLine("            <Setter Property=\"MinWidth\" Value=\"0\" />");
            writer.WriteLine("            <Setter Property=\"FontSize\" Value=\"12\" />");
            writer.WriteLine("            <Setter Property=\"FontFamily\" Value=\"Arial\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Link\" GroupLabel=\"NonEpsilonTransition\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('NonEpsilonTransition')\" />");
            writer.WriteLine("            <Setter Property=\"Stroke\" Value=\"black\" />");
            writer.WriteLine("            <Setter Property=\"FontSize\" Value=\"18\" />");
            writer.WriteLine("            <Setter Property=\"FontFamily\" Value=\"Arial\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Link\" GroupLabel=\"StartTransition\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('StartTransition')\" />");
            writer.WriteLine("            <Setter Property=\"Stroke\" Value=\"black\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Link\" GroupLabel=\"EpsilonTransition\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('EpsilonTransition')\" />");
            writer.WriteLine("            <Setter Property=\"Stroke\" Value=\"black\" />");
            writer.WriteLine("            <Setter Property=\"StrokeDashArray\" Value=\"8 8\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Link\" GroupLabel=\"FinalLabel\" ValueLabel=\"False\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('FinalLabel')\" />");
            writer.WriteLine("            <Setter Property=\"Stroke\" Value=\"black\" />");
            writer.WriteLine("            <Setter Property=\"StrokeDashArray\" Value=\"8 8\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Node\" GroupLabel=\"StateInfo\" ValueLabel=\"True\">");
            writer.WriteLine("            <Setter Property=\"Stroke\" Value=\"white\" />");
            writer.WriteLine("            <Setter Property=\"FontSize\" Value=\"18\" />");
            writer.WriteLine("            <Setter Property=\"FontFamily\" Value=\"Arial\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Node\" GroupLabel=\"DFAInfo\" ValueLabel=\"True\">");
            writer.WriteLine("            <Setter Property=\"Stroke\" Value=\"white\" />");
            writer.WriteLine("            <Setter Property=\"FontSize\" Value=\"18\" />");
            writer.WriteLine("            <Setter Property=\"FontFamily\" Value=\"Arial\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("    </Styles>");
            writer.WriteLine("</DirectedGraph>");
        }

        private static string GetDFAInfo(DfaExplorer explorer, CharSetSolver solver)
        {
            StringBuilder sb = new();
            sb.Append($"States = {explorer.StateCount}&#13;");
            sb.Append($"Transitions = {explorer.TransitionCount}&#13;");
            sb.Append($"Min Terms ({explorer._builder._solver.GetMinterms()!.Length}) = ").AppendJoin(',', DescribeLabels(explorer, explorer.Alphabet, solver));
            return sb.ToString();
        }

        private static IEnumerable<string> DescribeLabels(DfaExplorer explorer, IList<(SymbolicRegexNode<TSet>?, TSet)> items, CharSetSolver solver)
        {
            for (int i = 0; i < items.Count; i++)
            {
                yield return explorer.DescribeLabel(items[i], solver);
            }
        }

        /// <summary>Used to unwind a regex into a DFA up to a bound that limits the number of states</summary>
        private sealed class DfaExplorer
        {
            private readonly DfaMatchingState<TSet> _initialState;
            private readonly List<int> _states = new();
            private readonly List<Transition> _transitions = new();
            private readonly SymbolicNFA<TSet>? _nfa;
            internal readonly SymbolicRegexBuilder<TSet> _builder;

            internal DfaExplorer(SymbolicRegexMatcher<TSet> srm, bool nfa, bool addDotStar, bool reverse, int maxStates)
            {
                _builder = srm._builder;
                uint startId = reverse ?
                    (srm._reversePattern._info.StartsWithSomeAnchor ? CharKind.BeginningEnd : 0) :
                    (srm._pattern._info.StartsWithSomeAnchor ? CharKind.BeginningEnd : 0);

                // Create the initial state
                _initialState = _builder.CreateState(
                    reverse ? srm._reversePattern :
                    addDotStar ? srm._dotStarredPattern :
                    srm._pattern, startId);

                if (nfa)
                {
                    _nfa = _initialState.Node.Explore(maxStates);
                    for (int q = 0; q < _nfa.StateCount; q++)
                    {
                        _states.Add(q);
                        foreach ((TSet, SymbolicRegexNode<TSet>?, int) branch in _nfa.EnumeratePaths(q))
                        {
                            _transitions.Add(new Transition(q, branch.Item3, (branch.Item2, branch.Item1)));
                        }
                    }
                }
                else
                {
                    Dictionary<(int, int), TSet> normalizedMoves = new();
                    Stack<DfaMatchingState<TSet>> stack = new();
                    stack.Push(_initialState);
                    _states.Add(_initialState.Id);

                    HashSet<int> stateSet = new();
                    stateSet.Add(_initialState.Id);

                    TSet[]? minterms = _builder._solver.GetMinterms();
                    Debug.Assert(minterms is not null);

                    // Unwind until the stack is empty or the bound has been reached
                    while (stack.Count > 0 && (maxStates <= 0 || _states.Count < maxStates))
                    {
                        DfaMatchingState<TSet> q = stack.Pop();
                        foreach (TSet c in minterms)
                        {
                            DfaMatchingState<TSet> p = q.Next(c);

                            // check that p is not a dead-end
                            if (!p.IsNothing)
                            {
                                if (stateSet.Add(p.Id))
                                {
                                    stack.Push(p);
                                    _states.Add(p.Id);
                                }

                                (int, int) qp = (q.Id, p.Id);
                                normalizedMoves[qp] = normalizedMoves.ContainsKey(qp) ?
                                    _builder._solver.Or(normalizedMoves[qp], c) :
                                    c;
                            }
                        }
                    }

                    foreach (KeyValuePair<(int, int), TSet> entry in normalizedMoves)
                    {
                        _transitions.Add(new Transition(entry.Key.Item1, entry.Key.Item2, (null, entry.Value)));
                    }
                }
            }

            public (SymbolicRegexNode<TSet>?, TSet)[] Alphabet
            {
                get
                {
                    TSet[]? alphabet = _builder._solver.GetMinterms();
                    Debug.Assert(alphabet is not null);
                    var results = new (SymbolicRegexNode<TSet>?, TSet)[alphabet.Length];
                    for (int i = 0; i < alphabet.Length; i++)
                    {
                        results[i] = (null, alphabet[i]);
                    }
                    return results;
                }
            }

            public int InitialState => _nfa is not null ? 0 : _initialState.Id;

            public int StateCount => _states.Count;

            public int TransitionCount => _transitions.Count;

            public string DescribeLabel((SymbolicRegexNode<TSet>?, TSet) lab, CharSetSolver solver) =>
                WebUtility.HtmlEncode(lab.Item1 is null ? // Conditional nullability based on anchors
                    _builder._solver.PrettyPrint(lab.Item2, solver) :
                    $"{lab.Item1}/{_builder._solver.PrettyPrint(lab.Item2, solver)}");

            public string DescribeState(int state)
            {
                if (_nfa is not null)
                {
                    Debug.Assert(state < _nfa.StateCount);
                    string? str = WebUtility.HtmlEncode(_nfa.GetNode(state).ToString());
                    return _nfa.IsUnexplored(state) ? $"Unexplored:{str}" : str;
                }

                Debug.Assert(_builder._stateArray is not null);
                return _builder._stateArray[state].DgmlView;
            }

            public IEnumerable<int> GetStates() => _states;

            public bool IsFinalState(int state)
            {
                if (_nfa is not null)
                {
                    Debug.Assert(state < _nfa.StateCount);
                    return _nfa.CanBeNullable(state);
                }

                Debug.Assert(_builder._stateArray is not null && state < _builder._stateArray.Length);
                return _builder._stateArray[state].Node.CanBeNullable;
            }

            public List<Transition> GetTransitions() => _transitions;
        }

        private sealed record Transition(int SourceState, int TargetState, (SymbolicRegexNode<TSet>?, TSet) Label)
        {
            public bool IsEpsilon => Label.Equals(default);
        }
    }
}
#endif
