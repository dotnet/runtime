// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions.Symbolic.Unicode;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Provides functionality to build character sets, to perform boolean operations over character sets,
    /// and to construct a symbolic finite automata (SFA) over character sets from a regex.
    /// Character sets are represented by bitvector sets.
    /// </summary>
    internal sealed class CharSetSolver : BDDAlgebra, ICharAlgebra<BDD>
    {
        /// <summary>BDDs for all ASCII characters for fast lookup.</summary>
        private readonly BDD[] _charPredTable = new BDD[128];
        private readonly IgnoreCaseTransformer _ignoreCase;
        internal readonly BDD _nonAscii;

        /// <summary>Initialize the solver.</summary>
        /// <remarks>Consumers should use the singleton <see cref="Instance"/>.</remarks>
        private CharSetSolver()
        {
            _nonAscii = CreateCharSetFromRange('\x80', '\uFFFF');
            _ignoreCase = new IgnoreCaseTransformer(this); // do this last in ctor, as IgnoreCaseTransform's ctor uses `this`
        }

        /// <summary>Singleton instance of <see cref="CharSetSolver"/>.</summary>
        public static CharSetSolver Instance { get; } = new CharSetSolver();

        /// <summary>
        /// Make a character predicate for the given character c.
        /// </summary>
        public BDD CharConstraint(char c, bool ignoreCase = false, string? culture = null)
        {
            if (ignoreCase)
            {
                return _ignoreCase.Apply(c, culture);
            }
            else
            {
                // individual character BDDs are always fixed
                BDD[] charPredTable = _charPredTable;
                return c < charPredTable.Length ?
                    charPredTable[c] ??= CreateBDDFromChar(c) :
                    CreateBDDFromChar(c);
            }
        }

        private BDD CreateBDDFromChar(ushort c)
        {
            BDD bdd = BDD.True;
            for (int k = 0; k < 16; k++)
            {
                bdd = (c & (1 << k)) == 0 ? GetOrCreateBDD(k, BDD.False, bdd) : GetOrCreateBDD(k, bdd, BDD.False);
            }
            return bdd;
        }

        /// <summary>
        /// Make a CharSet from all the characters in the range from m to n.
        /// Returns the empty set if n is less than m
        /// </summary>
        public BDD CreateCharSetFromRange(char m, char n) =>
            m == n ? CharConstraint(m) :
            CreateSetFromRange(m, n, 15);

        /// <summary>
        /// Make a character set of all the characters in the interval from c to d.
        /// If ignoreCase is true ignore cases for upper and lower case characters by including both versions.
        /// </summary>
        public BDD RangeConstraint(char c, char d, bool ignoreCase = false, string? culture = null)
        {
            if (c == d)
            {
                return CharConstraint(c, ignoreCase, culture);
            }

            BDD res = CreateSetFromRange(c, d, 15);
            if (ignoreCase)
            {
                res = _ignoreCase.Apply(res, culture);
            }

            return res;
        }

#if DEBUG
        /// <summary>
        /// Make a BDD encoding of k least significant bits of all the integers in the ranges
        /// </summary>
        internal BDD CreateBddForIntRanges(List<int[]> ranges)
        {
            BDD bdd = False;
            foreach (int[] range in ranges)
            {
                bdd = Or(bdd, CreateSetFromRange((uint)range[0], (uint)range[1], 15));
            }

            return bdd;
        }
#endif

        /// <summary>
        /// Identity function, returns s.
        /// </summary>
        public BDD ConvertFromCharSet(BDDAlgebra _, BDD s) => s;

        /// <summary>
        /// Convert the set into an equivalent array of ranges. The ranges are nonoverlapping and ordered.
        /// </summary>
        public static (uint, uint)[] ToRanges(BDD set) => BDDRangeConverter.ToRanges(set, 15);

        public BDD ConvertToCharSet(BDD pred) => pred;

        public BDD[]? GetMinterms() => null;

        public string PrettyPrint(BDD pred)
        {
            if (pred.IsEmpty)
                return "[]";

            //check if pred is full, show this case with a dot
            if (pred.IsFull)
                return ".";

            // try to optimize representation involving common direct use of \w, \s, and \d to avoid blowup of ranges
            BDD w = UnicodeCategoryConditions.WordLetter;
            if (pred == w)
                return @"\w";

            BDD W = Not(w);
            if (pred == W)
                return @"\W";

            BDD s = UnicodeCategoryConditions.WhiteSpace;
            if (pred == s)
                return @"\s";

            BDD S = Not(s);
            if (pred == S)
                return @"\S";

            BDD d = UnicodeCategoryConditions.GetCategory(UnicodeCategory.DecimalDigitNumber);
            if (pred == d)
                return @"\d";

            BDD D = Not(d);
            if (pred == D)
                return @"\D";

            (uint, uint)[] ranges = ToRanges(pred);

            if (IsSingletonRange(ranges))
                return Escape((char)ranges[0].Item1);

            #region if too many ranges try to optimize the representation using \d \w etc.
            if (ranges.Length > 10)
            {
                BDD asciiDigit = CreateCharSetFromRange('0', '9');
                BDD nonasciiDigit = And(d, Not(asciiDigit));
                BDD wD = And(w, D);
                BDD SW = And(S, W);
                //s, d, wD, SW are the 4 main large minterms
                //note: s|SW = W, d|wD = w
                //
                // Venn Diagram: s and w do not overlap, and d is contained in w
                // ------------------------------------------------
                // |                                              |
                // |              SW     ------------(w)--------  |
                // |   --------          |                     |  |
                // |   |      |          |        ----------   |  |
                // |   |  s   |          |  wD    |        |   |  |
                // |   |      |          |        |   d    |   |  |
                // |   --------          |        |        |   |  |
                // |                     |        ----------   |  |
                // |                     -----------------------  |
                // ------------------------------------------------
                //
                //-------------------------------------------------------------------
                // singletons
                //---
                if (Or(s, pred) == s)
                    return $"[^\\S{RepresentSet(And(s, Not(pred)))}]";
                //---
                if (Or(d, pred) == d)
                    return $"[^\\D{RepresentSet(And(d, Not(pred)))}]";
                //---
                if (Or(wD, pred) == wD)
                    return $"[\\w-[\\d{RepresentSet(And(wD, Not(pred)))}]]";
                //---
                if (Or(SW, pred) == SW)
                    return $"[^\\s\\w{RepresentSet(And(SW, Not(pred)))}]";
                //-------------------------------------------------------------------
                // unions of two
                // s|SW
                if (Or(W, pred) == W)
                {
                    string? repr1 = null;
                    if (And(s, pred) == s)
                    {
                        //pred contains all of \s and is contained in \W
                        repr1 = $"[\\s{RepresentSet(And(S, pred))}]";
                    }

                    //the more common case is that pred is not \w and not some specific non-word character such as ':'
                    string repr2 = $"[^\\w{RepresentSet(And(W, Not(pred)))}]";
                    return repr1 != null && repr1.Length < repr2.Length ? repr1 : repr2;
                }
                //---
                // s|d
                BDD s_or_d = Or(s, d);
                if (pred == s_or_d)
                    return "[\\s\\d]";

                if (Or(s_or_d, pred) == s_or_d)
                {
                    //check first if this is purely ascii range for digits
                    return And(pred, s).Equals(s) && And(pred, nonasciiDigit).IsEmpty ?
                        $"[\\s{RepresentRanges(ToRanges(And(pred, asciiDigit)), checkSingletonComlement: false)}]" :
                        $"[\\s\\d-[{RepresentSet(And(s_or_d, Not(pred)))}]]";
                }
                //---
                // s|wD
                BDD s_or_wD = Or(s, wD);
                if (Or(s_or_wD, pred) == s_or_wD)
                    return $"[\\s\\w-[\\d{RepresentSet(And(s_or_wD, Not(pred)))}]]";
                //---
                // d|wD
                if (Or(w, pred) == w)
                    return $"[\\w-[{RepresentSet(And(w, Not(pred)))}]]";
                //---
                // d|SW
                BDD d_or_SW = Or(d, SW);
                if (pred == d_or_SW)
                    return "\\d|[^\\s\\w]";
                if (Or(d_or_SW, pred) == d_or_SW)
                    return $"[\\d-[{RepresentSet(And(d, Not(pred)))}]]|[^\\s\\w{RepresentSet(And(SW, Not(pred)))}]";
                // wD|SW = S&D
                BDD SD = Or(wD, SW);
                if (Or(SD, pred) == SD)
                    return $"[^\\s\\d{RepresentSet(And(SD, Not(pred)))}]";
                //-------------------------------------------------------------------
                //unions of three
                // s|SW|wD = D
                if (Or(D, pred) == D)
                    return $"[^\\d{RepresentSet(And(D, Not(pred)))}]";
                // SW|wD|d = S
                if (Or(S, pred) == S)
                    return $"[^\\s{RepresentSet(And(S, Not(pred)))}]";
                // s|SW|d = complement of wD = W|d
                BDD W_or_d = Not(wD);
                if (Or(W_or_d, pred) == W_or_d)
                    return $"[\\W\\d-[{RepresentSet(And(W_or_d, Not(pred)))}]]";
                // s|wD|d = s|w
                BDD s_or_w = Or(s, w);
                if (Or(s_or_w, pred) == s_or_w)
                    return $"[\\s\\w-[{RepresentSet(And(s_or_w, Not(pred)))}]]";
                //-------------------------------------------------------------------
                //touches all four minterms, typically happens as the fallback arc in .* extension
            }
            #endregion

            // Represent either the ranges or its complement, if the complement representation is more compact.
            string ranges_repr = $"[{RepresentRanges(ranges, checkSingletonComlement: false)}]";
            string ranges_compl_repr = $"[^{RepresentRanges(ToRanges(Not(pred)), checkSingletonComlement: false)}]";
            return ranges_repr.Length <= ranges_compl_repr.Length ? ranges_repr : ranges_compl_repr;
        }

        private static string RepresentSet(BDD set) =>
            set.IsEmpty ? "" : RepresentRanges(ToRanges(set));

        private static string RepresentRanges((uint, uint)[] ranges, bool checkSingletonComlement = true)
        {
            //check if ranges represents a complement of a singleton
            if (checkSingletonComlement && ranges.Length == 2 &&
                ranges[0].Item1 == 0 && ranges[1].Item2 == 0xFFFF &&
                ranges[0].Item2 + 2 == ranges[1].Item1)
            {
                return "^" + Escape((char)(ranges[0].Item2 + 1));
            }

            StringBuilder sb = new();
            for (int i = 0; i < ranges.Length; i++)
            {
                if (ranges[i].Item1 == ranges[i].Item2)
                {
                    sb.Append(Escape((char)ranges[i].Item1));
                }
                else if (ranges[i].Item2 == ranges[i].Item1 + 1)
                {
                    sb.Append(Escape((char)ranges[i].Item1));
                    sb.Append(Escape((char)ranges[i].Item2));
                }
                else
                {
                    sb.Append(Escape((char)ranges[i].Item1));
                    sb.Append('-');
                    sb.Append(Escape((char)ranges[i].Item2));
                }
            }
            return sb.ToString();
        }

        /// <summary>Make an escaped string from a character.</summary>
        /// <param name="c">The character to escape.</param>
        private static string Escape(char c)
        {
            uint code = c;
            return c switch
            {
                '.' => @"\.",
                '[' => @"\[",
                ']' => @"\]",
                '(' => @"\(",
                ')' => @"\)",
                '{' => @"\{",
                '}' => @"\}",
                '?' => @"\?",
                '+' => @"\+",
                '*' => @"\*",
                '|' => @"\|",
                '\\' => @"\\",
                '^' => @"\^",
                '$' => @"\$",
                '-' => @"\-",
                ':' => @"\:",
                '\"' => "\\\"",
                '\0' => @"\0",
                '\t' => @"\t",
                '\r' => @"\r",
                '\v' => @"\v",
                '\f' => @"\f",
                '\n' => @"\n",
                _ when code is >= 0x20 and <= 0x7E => c.ToString(),
                _ when code <= 0xFF => $"\\x{code:X2}",
                _ => $"\\u{code:X4}",
            };
        }

        private static bool IsSingletonRange((uint, uint)[] ranges) => ranges.Length == 1 && ranges[0].Item1 == ranges[0].Item2;
    }
}
