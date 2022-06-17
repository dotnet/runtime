// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions.Symbolic
{
    internal sealed partial class SymbolicRegexMatcher<TSet>
    {
        /// <summary>
        /// The probability of stopping match sampling when a candidate is found. This influences the expected length
        /// of the sampled matches. For a pattern .* that accepts anything the expected length is:
        /// 1/p - 1, where the -1 comes from the fact that the first coin is tossed with the empty input.
        /// </summary>
        private const double SampleMatchesStoppingProbability = 0.2;

        /// <summary>
        /// Maximum length to try to sample input to.
        /// </summary>
        /// <remarks>
        /// This is required for cases where the state space has a loop that is not detected as a deadend,
        /// which would otherwise cause the sampling to hang.
        /// </remarks>
        private const int SampleMatchesMaxInputLength = 100;

        /// <inheritdoc cref="Regex.SampleMatches(int, int)"/>
        [ExcludeFromCodeCoverage(Justification = "Currently only used for testing")]
        public override IEnumerable<string> SampleMatches(int k, int randomseed)
        {
            // Zero is treated as no seed, instead using a system provided one
            Random random = randomseed != 0 ? new Random(randomseed) : new Random();

            ISolver<TSet> solver = _builder._solver;
            CharSetSolver charSetSolver = _builder._charSetSolver;

            // Create helper BDDs for handling anchors and preferentially generating ASCII inputs
            BDD asciiWordCharacters = charSetSolver.Or(new BDD[] {
                charSetSolver.CreateBDDFromRange('A', 'Z'),
                charSetSolver.CreateBDDFromRange('a', 'z'),
                charSetSolver.CreateBDDFromChar('_'),
                charSetSolver.CreateBDDFromRange('0', '9')});
            // Visible ASCII range for input character generation
            BDD ascii = charSetSolver.CreateBDDFromRange('\x20', '\x7E');
            BDD asciiNonWordCharacters = charSetSolver.And(ascii, charSetSolver.Not(asciiWordCharacters));

            // Set up two sets of minterms, one with the additional special minterm for the last end-of-line
            Debug.Assert(_builder._minterms is not null);
            int[] mintermIdsWithoutZ = new int[_builder._minterms.Length];
            int[] mintermIdsWithZ = new int[_builder._minterms.Length + 1];
            for (int i = 0; i < _builder._minterms.Length; ++i)
            {
                mintermIdsWithoutZ[i] = i;
                mintermIdsWithZ[i] = i;
            }
            mintermIdsWithZ[_builder._minterms.Length] = _builder._minterms.Length;

            for (int i = 0; i < k; i++)
            {
                // Holds the generated input so far
                StringBuilder inputSoFar = new();
                StringBuilder? latestCandidate = null;

                // Current set of states reached initially contains just the root
                NfaMatchingState states = new(_builder);
                // Here one could also consider previous characters for example for \b, \B, and ^ anchors
                // and initialize inputSoFar accordingly
                states.InitializeFrom(_initialStates[GetCharKind(ReadOnlySpan<char>.Empty, -1)]);
                CurrentState statesWrapper = new(states);

                // Used for end suffixes
                List<string> possibleEndings = new();

                while (true)
                {
                    Debug.Assert(states.NfaStateSet.Count > 0);

                    // Gather the possible endings for satisfying nullability
                    possibleEndings.Clear();
                    if (NfaStateHandler.CanBeNullable(ref statesWrapper))
                    {
                        // Unconditionally final state or end of the input due to \Z anchor for example
                        if (NfaStateHandler.IsNullable(ref statesWrapper) ||
                            NfaStateHandler.IsNullableFor(_builder, ref statesWrapper, CharKind.BeginningEnd))
                        {
                            possibleEndings.Add("");
                        }

                        // End of line due to end-of-line anchor
                        if (NfaStateHandler.IsNullableFor(_builder, ref statesWrapper, CharKind.Newline))
                        {
                            possibleEndings.Add("\n");
                        }

                        // Related to wordborder due to \b or \B
                        if (NfaStateHandler.IsNullableFor(_builder, ref statesWrapper, CharKind.WordLetter))
                        {
                            possibleEndings.Add(ChooseChar(random, asciiWordCharacters, ascii, charSetSolver).ToString());
                        }

                        // Related to wordborder due to \b or \B
                        if (NfaStateHandler.IsNullableFor(_builder, ref statesWrapper, CharKind.General))
                        {
                            possibleEndings.Add(ChooseChar(random, asciiNonWordCharacters, ascii, charSetSolver).ToString());
                        }
                    }

                    // If we have a possible ending, then store a candidate input
                    if (possibleEndings.Count > 0)
                    {
                        latestCandidate ??= new();
                        latestCandidate.Clear();
                        latestCandidate.Append(inputSoFar);
                        //Choose some suffix that allows some anchor (if any) to be nullable
                        latestCandidate.Append(Choose(random, possibleEndings));

                        // Choose to stop here based on a coin-toss
                        if (FlipBiasedCoin(random, SampleMatchesStoppingProbability))
                        {
                            yield return latestCandidate.ToString();
                            break;
                        }
                    }

                    // Shuffle the minterms, including the last end-of-line marker if appropriate
                    int[] mintermIds = NfaStateHandler.StartsWithLineAnchor(_builder, ref statesWrapper) ?
                        Shuffle(random, mintermIdsWithZ) :
                        Shuffle(random, mintermIdsWithoutZ);
                    foreach (int mintermId in mintermIds)
                    {
                        bool success = NfaStateHandler.TakeTransition(_builder, ref statesWrapper, mintermId);
                        Debug.Assert(success);
                        if (states.NfaStateSet.Count > 0)
                        {
                            TSet minterm = _builder.GetMinterm(mintermId);
                            // Append a random member of the minterm
                            inputSoFar.Append(ChooseChar(random, ToBDD(minterm, solver, charSetSolver), ascii, charSetSolver));
                            break;
                        }
                        else
                        {
                            // The transition was a dead end, undo and continue to try another minterm
                            NfaStateHandler.UndoTransition(ref statesWrapper);
                        }
                    }

                    // In the case that there are no next states or input has become too large: stop here
                    if (states.NfaStateSet.Count == 0 || inputSoFar.Length > SampleMatchesMaxInputLength)
                    {
                        // Ending up here without an ending is unlikely but possible for example for infeasible patterns
                        // such as @"no\bway" or due to poor choice of c -- no anchor is enabled -- so this is a deadend.
                        if (latestCandidate != null)
                        {
                            yield return latestCandidate.ToString();
                        }
                        break;
                    }
                }
            }

            static BDD ToBDD(TSet set, ISolver<TSet> solver, CharSetSolver charSetSolver) => solver.ConvertToBDD(set, charSetSolver);

            static T Choose<T>(Random random, IList<T> elems) => elems[random.Next(elems.Count)];

            static char ChooseChar(Random random, BDD bdd, BDD ascii, CharSetSolver charSetSolver)
            {
                Debug.Assert(!bdd.IsEmpty);
                // Select characters from the visible ASCII range whenever possible
                BDD bdd1 = charSetSolver.And(bdd, ascii);
                (uint, uint) range = Choose(random, BDDRangeConverter.ToRanges(bdd1.IsEmpty ? bdd : bdd1));
                return (char)random.Next((int)range.Item1, (int)range.Item2 + 1);
            }

            static bool FlipBiasedCoin(Random random, double probTrue) => random.NextDouble() < probTrue;

            static T[] Shuffle<T>(Random random, T[] array)
            {
                // In-place Fisher-Yates shuffle
                for (int i = 0; i < array.Length - 1; ++i)
                {
                    int j = random.Next(i, array.Length);
                    var tmp = array[i];
                    array[i] = array[j];
                    array[j] = tmp;
                }
                return array;
            }
        }
    }
}
#endif
