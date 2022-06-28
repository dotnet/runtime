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
        /// <inheritdoc cref="Regex.SaveDGML(TextWriter, int)"/>
        [ExcludeFromCodeCoverage(Justification = "Currently only used for testing")]
        public override void SaveDGML(TextWriter writer, int maxLabelLength)
        {
            if (maxLabelLength < 0)
                maxLabelLength = int.MaxValue;

            Dictionary<(int Source, int Target), (TSet Rule, List<int> NfaTargets)> transitions = GatherTransitions(_builder);

            writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            writer.WriteLine("<DirectedGraph xmlns=\"http://schemas.microsoft.com/vs/2009/dgml\" ZoomLevel=\"1.5\" GraphDirection=\"TopToBottom\" >");
            writer.WriteLine("    <Nodes>");
            writer.WriteLine("        <Node Id=\"dfa\" Label=\" \" Group=\"Collapsed\" Category=\"DFA\" DFAInfo=\"{0}\" />", FormatInfo(_builder, transitions.Count));
            writer.WriteLine("        <Node Id=\"dfainfo\" Category=\"DFAInfo\" Label=\"{0}\"/>", FormatInfo(_builder, transitions.Count));
            foreach (DfaMatchingState<TSet> state in _builder._stateCache)
            {
                string info = CharKind.DescribePrev(state.PrevCharKind);
                string deriv = WebUtility.HtmlEncode(state.Node.ToString());
                string nodeDgmlView = $"{(info == string.Empty ? info : $"Previous: {info}&#13;")}{(deriv == string.Empty ? "()" : deriv)}";

                writer.WriteLine("        <Node Id=\"{0}\" Label=\"{0}\" Category=\"State\" Group=\"Collapsed\" StateInfo=\"{1}\">", state.Id, nodeDgmlView);
                if (_builder.GetStateInfo(state.Id).IsInitial)
                {
                    writer.WriteLine("            <Category Ref=\"InitialState\" />");
                }
                if (state.Node.CanBeNullable)
                {
                    writer.WriteLine("            <Category Ref=\"FinalState\" />");
                }
                writer.WriteLine("        </Node>");
                writer.WriteLine("        <Node Id=\"{0}info\" Label=\"{1}\" Category=\"StateInfo\"/>", state.Id, nodeDgmlView);
            }
            writer.WriteLine("    </Nodes>");
            writer.WriteLine("    <Links>");
            foreach (DfaMatchingState<TSet> initialState in GetInitialStates(this))
            {
                Debug.Assert(_builder._stateCache.Contains(initialState));
                writer.WriteLine("        <Link Source=\"dfa\" Target=\"{0}\" Label=\"\" Category=\"StartTransition\" />", initialState.Id);
            }
            writer.WriteLine("        <Link Source=\"dfa\" Target=\"dfainfo\" Label=\"\" Category=\"Contains\" />");

            foreach (KeyValuePair<(int Source, int Target), (TSet Rule, List<int> NfaTargets)> transition in transitions)
            {
                string label = DescribeLabel(transition.Value.Rule, _builder);
                string info = "";
                if (label.Length > maxLabelLength)
                {
                    info = $"FullLabel = \"{label}\" ";
                    label = string.Concat(label.AsSpan(0, maxLabelLength), "..");
                }

                writer.WriteLine($"        <Link Source=\"{transition.Key.Source}\" Target=\"{transition.Key.Target}\" Label=\"{label}\" Category=\"NonEpsilonTransition\" {info}/>");
                // Render NFA transitions as labelless "epsilon" transitions (i.e. ones that don't consume a character)
                // from the target of the DFA transition.
                foreach (int nfaTarget in transition.Value.NfaTargets)
                {
                    writer.WriteLine($"        <Link Source=\"{transition.Key.Target}\" Target=\"{nfaTarget}\" Category=\"EpsilonTransition\"/>");
                }
            }

            foreach (DfaMatchingState<TSet> state in _builder._stateCache)
            {
                writer.WriteLine("        <Link Source=\"{0}\" Target=\"{0}info\" Category=\"Contains\" />", state.Id);
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
            writer.WriteLine("            <Setter Property=\"Background\" Value=\"lightblue\" />");
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
            writer.WriteLine("            <Setter Property=\"FontSize\" Value=\"18\" />");
            writer.WriteLine("            <Setter Property=\"FontFamily\" Value=\"Arial\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Link\" GroupLabel=\"StartTransition\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('StartTransition')\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Link\" GroupLabel=\"EpsilonTransition\" ValueLabel=\"True\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('EpsilonTransition')\" />");
            writer.WriteLine("            <Setter Property=\"StrokeDashArray\" Value=\"8 8\" />");
            writer.WriteLine("        </Style>");
            writer.WriteLine("        <Style TargetType=\"Link\" GroupLabel=\"FinalLabel\" ValueLabel=\"False\">");
            writer.WriteLine("            <Condition Expression=\"HasCategory('FinalLabel')\" />");
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

            // This function gathers all transitions in the given builder and groups them by (source,destination) state ID
            static Dictionary<(int Source, int Target), (TSet Rule, List<int> NfaTargets)> GatherTransitions(SymbolicRegexBuilder<TSet> builder)
            {
                Debug.Assert(builder._delta is not null);
                Debug.Assert(builder._minterms is not null);
                Dictionary<(int Source, int Target), (TSet Rule, List<int> NfaTargets)> result = new();
                foreach (DfaMatchingState<TSet> source in builder._stateCache)
                {
                    // Get the span of entries in delta that gives the transitions for the different minterms
                    Span<int> deltas = builder.GetDeltasFor(source);
                    Span<int[]?> nfaDeltas = builder.GetNfaDeltasFor(source);
                    Debug.Assert(deltas.Length == builder._minterms.Length);
                    for (int i = 0; i < deltas.Length; ++i)
                    {
                        // negative entries are transitions not explored yet, so skip them
                        int targetId = deltas[i];
                        if (targetId >= 0)
                        {
                            // Get or create the data for this (source,destination) state ID pair
                            (int Source, int Target) key = (source.Id, targetId);
                            if (!result.TryGetValue(key, out (TSet Rule, List<int> NfaTargets) entry))
                            {
                                entry = (builder._solver.Empty, new List<int>());
                            }
                            // If this state has an NFA transition for the same minterm, then associate
                            // those with the transition.
                            if (nfaDeltas.Length > 0 && nfaDeltas[i] is int[] nfaTargets)
                            {
                                foreach (int nfaTarget in nfaTargets)
                                {
                                    entry.NfaTargets.Add(builder._nfaStateArray[nfaTarget]);
                                }
                            }
                            // Expand the rule for this minterm
                            result[key] = (builder._solver.Or(entry.Rule, builder._minterms[i]), entry.NfaTargets);
                        }
                    }
                }
                return result;
            }

            static string FormatInfo(SymbolicRegexBuilder<TSet> builder, int transitionCount)
            {
                StringBuilder sb = new();
                sb.Append($"States = {builder._stateCache.Count}&#13;");
                sb.Append($"Transitions = {transitionCount}&#13;");
                sb.Append($"Min Terms ({builder._solver.GetMinterms()!.Length}) = ").AppendJoin(',',
                    DescribeLabels(builder._solver.GetMinterms()!, builder));
                return sb.ToString();
            }

            static IEnumerable<string> DescribeLabels(IEnumerable<TSet> labels, SymbolicRegexBuilder<TSet> builder)
            {
                foreach (TSet label in labels)
                {
                    yield return DescribeLabel(label, builder);
                }
            }

            static string DescribeLabel(TSet label, SymbolicRegexBuilder<TSet> builder) =>
                WebUtility.HtmlEncode(builder._solver.PrettyPrint(label, builder._charSetSolver));

            static IEnumerable<DfaMatchingState<TSet>> GetInitialStates(SymbolicRegexMatcher<TSet> matcher)
            {
                foreach (DfaMatchingState<TSet> state in matcher._dotstarredInitialStates)
                    yield return state;
                foreach (DfaMatchingState<TSet> state in matcher._initialStates)
                    yield return state;
                foreach (DfaMatchingState<TSet> state in matcher._reverseInitialStates)
                    yield return state;
            }
        }
    }
}
#endif
