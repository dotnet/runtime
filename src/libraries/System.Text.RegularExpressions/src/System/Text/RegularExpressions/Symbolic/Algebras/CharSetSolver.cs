// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Provides functionality to build character sets, to perform boolean operations over character sets,
    /// and to construct an SFA over character sets from a regex.
    /// Character sets are represented by bitvector sets.
    /// </summary>
    internal sealed class CharSetSolver : BDDAlgebra, ICharAlgebra<BDD>
    {
        /// <summary>BDDs for all ASCII characters for fast lookup.</summary>
        private readonly BDD[] _charPredTable = new BDD[128];
        private readonly Unicode.IgnoreCaseTransformer _ignoreCase;
        internal readonly BDD _nonAscii;

        /// <summary>
        /// Construct the solver.
        /// </summary>
        public CharSetSolver()
        {
            _nonAscii = CreateCharSetFromRange('\x80', '\uFFFF');
            _ignoreCase = new Unicode.IgnoreCaseTransformer(this);
        }

        public BDD ApplyIgnoreCase(BDD set, string? culture = null) => _ignoreCase.Apply(set, culture);

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
                //individual character BDDs are always fixed
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
        /// Make a character set that is the union of the character sets of the given ranges.
        /// </summary>
        public BDD CreateCharSetFromRanges(IEnumerable<(uint, uint)> ranges)
        {
            BDD res = False;
            foreach ((uint, uint) range in ranges)
            {
                res = Or(res, CreateSetFromRange(range.Item1, range.Item2, 15));
            }

            return res;
        }

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

        /// <summary>
        /// Make a BDD encoding of k least significant bits of all the integers in the ranges
        /// </summary>
        internal BDD CreateBddForIntRanges(IEnumerable<int[]> ranges)
        {
            BDD bdd = False;
            foreach (int[] range in ranges)
            {
                bdd = Or(bdd, CreateSetFromRange((uint)range[0], (uint)range[1], 15));
            }

            return bdd;
        }

        /// <summary>
        /// Identity function, returns s.
        /// </summary>
        public BDD ConvertFromCharSet(BDDAlgebra _, BDD s) => s;

        /// <summary>
        /// Returns this character set solver.
        /// </summary>
        public CharSetSolver CharSetProvider => this;

        public IEnumerable<char> GenerateAllCharacters(BDD bvSet, bool inReverseOrder = false)
        {
            foreach (uint c in GenerateAllElements(bvSet, inReverseOrder))
            {
                yield return (char)c;
            }
        }

        public IEnumerable<char> GenerateAllCharacters(BDD set) => GenerateAllCharacters(set, false);

        /// <summary>Calculate the number of elements in the set.</summary>
        /// <param name="set">the given set</param>
        /// <returns>the cardinality of the set</returns>
        public ulong ComputeDomainSize(BDD set) => ComputeDomainSize(set, 15);

        /// <summary>Calculate the number of elements in multiple sets.</summary>
        /// <param name="sets">The sets</param>
        /// <returns>An array of the cardinality of the sets.</returns>
        public ulong[] ComputeDomainSizes(BDD[] sets)
        {
            var results = new ulong[sets.Length];
            for (int i = 0; i < sets.Length; i++)
            {
                results[i] = ComputeDomainSize(sets[i]);
            }
            return results;
        }

        /// <summary>
        /// Returns true iff the set contains exactly one element.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <returns>true iff the set is a singleton</returns>
        public bool IsSingleton(BDD set) => ComputeDomainSize(set, 15) == 1;

        /// <summary>
        /// Convert the set into an equivalent array of ranges. The ranges are nonoverlapping and ordered.
        /// </summary>
        public (uint, uint)[] ToRanges(BDD set) => ToRanges(set, 15);

        private IEnumerable<uint> GenerateAllCharactersInOrder(BDD set)
        {
            foreach ((uint, uint) range in ToRanges(set))
            {
                for (uint i = range.Item1; i <= range.Item2; i++)
                {
                    yield return i;
                }
            }
        }

        private IEnumerable<uint> GenerateAllCharactersInReverseOrder(BDD set)
        {
            (uint, uint)[] ranges = ToRanges(set);
            for (int j = ranges.Length - 1; j >= 0; j--)
            {
                for (uint i = ranges[j].Item2; i >= ranges[j].Item1; i--)
                {
                    yield return (char)i;
                }
            }
        }

        /// <summary>
        /// Generate all characters that are members of the set in alphabetical order, smallest first, provided that inReverseOrder is false.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <param name="inReverseOrder">if true the members are generated in reverse alphabetical order with the largest first, otherwise in alphabetical order</param>
        /// <returns>enumeration of all characters in the set, the enumeration is empty if the set is empty</returns>
        private IEnumerable<uint> GenerateAllElements(BDD set, bool inReverseOrder) =>
            set == False ? Array.Empty<uint>() :
            inReverseOrder ? GenerateAllCharactersInReverseOrder(set) :
            GenerateAllCharactersInOrder(set);

        public BDD ConvertToCharSet(ICharAlgebra<BDD> _, BDD pred) => pred;

        public BDD[]? GetMinterms() => null;

        public string PrettyPrint(BDD pred)
        {
            if (pred.IsEmpty)
                return "[]";

            //check if pred is full, show this case with a dot
            if (pred.IsFull)
                return ".";

            // try to optimize representation involving common direct use of \d \w and \s to avoid blowup of ranges
            BDD digit = SymbolicRegexRunnerFactory.s_unicode.CategoryCondition(8);
            if (pred == SymbolicRegexRunnerFactory.s_unicode.WordLetterCondition)
                return @"\w";
            if (pred == SymbolicRegexRunnerFactory.s_unicode.WhiteSpaceCondition)
                return @"\s";
            if (pred == digit)
                return @"\d";
            if (pred == Not(SymbolicRegexRunnerFactory.s_unicode.WordLetterCondition))
                return @"\W";
            if (pred == Not(SymbolicRegexRunnerFactory.s_unicode.WhiteSpaceCondition))
                return @"\S";
            if (pred == Not(digit))
                return @"\D";

            (uint, uint)[] ranges = ToRanges(pred);

            if (IsSingletonRange(ranges))
                return Escape((char)ranges[0].Item1);

            #region if too many ranges try to optimize the representation using \d \w etc.
            if (SymbolicRegexRunnerFactory.s_unicode != null && ranges.Length > 10)
            {
                BDD w = SymbolicRegexRunnerFactory.s_unicode.WordLetterCondition;
                BDD W = Not(w);
                BDD d = SymbolicRegexRunnerFactory.s_unicode.CategoryCondition(8);
                BDD D = Not(d);
                BDD asciiDigit = CreateCharSetFromRange('0', '9');
                BDD nonasciiDigit = And(d, Not(asciiDigit));
                BDD s = SymbolicRegexRunnerFactory.s_unicode.WhiteSpaceCondition;
                BDD S = Not(s);
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

        private string RepresentSet(BDD set) =>
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

        public override int CombineTerminals(BoolOp op, int terminal1, int terminal2) => throw new NotSupportedException();
    }
}
