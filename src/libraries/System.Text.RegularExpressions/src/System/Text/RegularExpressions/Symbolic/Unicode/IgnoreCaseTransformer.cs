// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic.Unicode
{
    internal sealed class IgnoreCaseTransformer
    {
        private const char Turkish_I_WithDot = '\u0130';
        private const char Turkish_i_WithoutDot = '\u0131';
        private const char KelvinSign = '\u212A';

        private readonly CharSetSolver _solver;
        private readonly BDD _i_Invariant;
        private readonly BDD _i_Default;
        private readonly BDD _i_Turkish;
        private readonly BDD _I_Turkish;

        private volatile IgnoreCaseRelation? _relationDefault;
        private volatile IgnoreCaseRelation? _relationInvariant;
        private volatile IgnoreCaseRelation? _relationTurkish;

        /// <summary>Maps each char c to the case-insensitive set of c that is culture-independent (for non-null entries).</summary>
        private readonly BDD?[] _cultureIndependentChars = new BDD[char.MaxValue + 1];

        private sealed class IgnoreCaseRelation
        {
            public IgnoreCaseRelation(BDD instance, BDD instanceDomain)
            {
                Instance = instance;
                InstanceDomain = instanceDomain;
            }

            public BDD Instance { get; }
            public BDD InstanceDomain { get; }
        }

        public IgnoreCaseTransformer(CharSetSolver solver)
        {
            _solver = solver;
            _i_Invariant = solver.Or(_solver.CharConstraint('i'), solver.CharConstraint('I'));
            _i_Default = solver.Or(_i_Invariant, solver.CharConstraint(Turkish_I_WithDot));
            _i_Turkish = solver.Or(solver.CharConstraint('i'), solver.CharConstraint(Turkish_I_WithDot));
            _I_Turkish = solver.Or(solver.CharConstraint('I'), solver.CharConstraint(Turkish_i_WithoutDot));
        }

        /// <summary>
        /// Get the set of CI-equivalent characters to c.
        /// This operation depends on culture for i, I, '\u0130', and '\u0131';
        /// culture="" means InvariantCulture while culture=null means to use the current culture.
        /// </summary>
        public BDD Apply(char c, string? culture = null)
        {
            if (Volatile.Read(ref _cultureIndependentChars[c]) is BDD bdd)
            {
                return bdd;
            }

            culture ??= CultureInfo.CurrentCulture.Name;
            switch (c)
            {
                // Do not cache in _cultureIndependentChars values that are culture-dependent

                case 'i':
                    return
                        culture == string.Empty ? _i_Invariant :
                        IsTurkishAlphabet(culture) ? _i_Turkish :
                        _i_Default; // for all other cultures, case-sensitivity is the same as for en-US

                case 'I':
                    return
                        culture == string.Empty ? _i_Invariant :
                        IsTurkishAlphabet(culture) ? _I_Turkish : // different from 'i' above
                        _i_Default;

                case Turkish_I_WithDot:
                    return
                        culture == string.Empty ? _solver.CharConstraint(Turkish_I_WithDot) :
                        IsTurkishAlphabet(culture) ? _i_Turkish :
                        _i_Default;

                case Turkish_i_WithoutDot:
                    return
                        IsTurkishAlphabet(culture) ? _I_Turkish :
                        _solver.CharConstraint(Turkish_i_WithoutDot);

                case 'k':
                case 'K':
                case KelvinSign:
                    Volatile.Write(ref _cultureIndependentChars[c], _solver.Or(_solver.Or(_solver.CharConstraint('k'), _solver.CharConstraint('K')), _solver.CharConstraint(KelvinSign)));
                    return _cultureIndependentChars[c]!;

                // Cache in _cultureIndependentChars entries that are culture-independent.
                // BDDs are idempotent, so while we use volatile to ensure proper adherence
                // to ECMA's memory model, we don't need Interlocked.CompareExchange.

                case <= '\x7F':
                    // For ASCII range other than letters i, I, k, and K, the case-conversion is independent of culture and does
                    // not include case-insensitive-equivalent non-ASCII.
                    Volatile.Write(ref _cultureIndependentChars[c], _solver.Or(_solver.CharConstraint(char.ToLower(c)), _solver.CharConstraint(char.ToUpper(c))));
                    return _cultureIndependentChars[c]!;

                default:
                    // Bring in the full transfomation relation, but here it does not actually depend on culture
                    // so it is safe to store the result for c.
                    Volatile.Write(ref _cultureIndependentChars[c], Apply(_solver.CharConstraint(c)));
                    return _cultureIndependentChars[c]!;
            }
        }

        /// <summary>
        /// For all letters in the bdd add their lower and upper case equivalents.
        /// This operation depends on culture for i, I, '\u0130', and '\u0131';
        /// culture="" means InvariantCulture while culture=null means to use the current culture.
        /// </summary>
        public BDD Apply(BDD bdd, string? culture = null)
        {
            // First get the culture specific relation
            IgnoreCaseRelation relation = GetIgnoreCaseRelation(culture);

            if (_solver.And(relation.InstanceDomain, bdd).IsEmpty)
            {
                // No elements need to be added
                return bdd;
            }

            // Compute the set of all characters that are equivalent to some element in bdd.
            // restr is the relation restricted to the relevant characters in bdd.
            // This conjunction works because bdd is unspecified for bits > 15.
            BDD restr = _solver.And(bdd, relation.Instance);

            // ShiftRight essentially produces the LHS of the relation (char X char) that restr represents.
            BDD ignorecase = _solver.ShiftRight(restr, 16);

            // The final set is the union of all the characters.
            return _solver.Or(ignorecase, bdd);
        }

        /// <summary>Gets the transformation relation based on the current culture.</summary>
        /// <remarks>culture == "" means InvariantCulture. culture == null means to use the current culture.</remarks>
        private IgnoreCaseRelation GetIgnoreCaseRelation(string? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture.Name;

            if (culture == string.Empty)
            {
                return _relationInvariant ?? CreateIgnoreCaseRelationInvariant();
            }

            if (IsTurkishAlphabet(culture))
            {
                return _relationTurkish ?? CreateIgnoreCaseRelationTurkish();
            }

            // All other cultures are equivalent to the default culture wrt case-sensitivity.
            return _relationDefault ?? EnsureDefault();
        }

        [MemberNotNull(nameof(_relationDefault))]
        private IgnoreCaseRelation EnsureDefault()
        {
            // Deserialize the table for the default culture.
            if (_relationDefault is null)
            {
                BDD instance = BDD.Deserialize(Unicode.IgnoreCaseRelation.IgnoreCaseEnUsSerializedBDD, _solver);
                BDD instanceDomain = _solver.ShiftRight(instance, 16); // represents the set of all case-sensitive characters in the default culture.
                _relationDefault = new IgnoreCaseRelation(instance, instanceDomain);
            }

            return _relationDefault;
        }

        private IgnoreCaseRelation CreateIgnoreCaseRelationInvariant()
        {
            EnsureDefault();

            // Compute the invariant table based off of default.
            // In the default (en-US) culture: Turkish_I_withDot = i = I
            // In the invariant culture: i = I, while Turkish_I_withDot is case-insensitive
            BDD tr_I_withdot_BDD = _solver.CharConstraint(Turkish_I_WithDot);

            // Since Turkish_I_withDot is case-insensitive in invariant culture, remove it from the default (en-US culture) table.
            BDD inv_table = _solver.And(_relationDefault.Instance, _solver.Not(tr_I_withdot_BDD));

            // Next, remove Turkish_I_withDot from the RHS of the relation.
            // This also effectively removes Turkish_I_withDot from the equivalence sets of 'i' and 'I'.
            BDD instance = _solver.And(inv_table, _solver.Not(_solver.ShiftLeft(tr_I_withdot_BDD, 16)));

            // Remove Turkish_I_withDot from the domain of casesensitive characters in the default case
            BDD instanceDomain = _solver.And(instance, _solver.Not(tr_I_withdot_BDD));

            _relationInvariant = new IgnoreCaseRelation(instance, instanceDomain);
            return _relationInvariant;
        }

        private IgnoreCaseRelation CreateIgnoreCaseRelationTurkish()
        {
            EnsureDefault();

            // Compute the tr table based off of default.
            // In the default (en-US) culture: Turkish_I_withDot = i = I
            // In the tr culture: i = Turkish_I_withDot, I = Turkish_i_withoutDot
            BDD tr_I_withdot_BDD = _solver.CharConstraint(Turkish_I_WithDot);
            BDD tr_i_withoutdot_BDD = _solver.CharConstraint(Turkish_i_WithoutDot);
            BDD i_BDD = _solver.CharConstraint('i');
            BDD I_BDD = _solver.CharConstraint('I');

            // First remove all i's from the default table from the LHS and from the RHS.
            // Note that Turkish_i_withoutDot is not in the default table because it is case-insensitive in the en-US culture.
            BDD iDefault = _solver.Or(i_BDD, _solver.Or(I_BDD, tr_I_withdot_BDD));
            BDD tr_table = _solver.And(_relationDefault.Instance, _solver.Not(iDefault));
            tr_table = _solver.And(tr_table, _solver.Not(_solver.ShiftLeft(iDefault, 16)));

            BDD i_tr = _solver.Or(i_BDD, tr_I_withdot_BDD);
            BDD I_tr = _solver.Or(I_BDD, tr_i_withoutdot_BDD);

            // The Cartesian product i_tr X i_tr.
            BDD i_trXi_tr = _solver.And(_solver.ShiftLeft(i_tr, 16), i_tr);

            // The Cartesian product I_tr X I_tr.
            BDD I_trXI_tr = _solver.And(_solver.ShiftLeft(I_tr, 16), I_tr);

            // Update the table with the new entries, and add Turkish_i_withoutDot also into the domain of case-sensitive characters.
            BDD instance = _solver.Or(tr_table, _solver.Or(i_trXi_tr, I_trXI_tr));
            BDD instanceDomain = _solver.Or(_relationDefault.InstanceDomain, tr_i_withoutdot_BDD);

            _relationTurkish = new IgnoreCaseRelation(instance, instanceDomain);
            return _relationTurkish;
        }

        private static bool IsTurkishAlphabet(string culture) =>
            culture is "az" or "az-Cyrl" or "az-Cyrl-AZ" or "az-Latn" or "az-Latn-AZ" or "tr" or "tr-CY" or "tr-TR";
    }
}
