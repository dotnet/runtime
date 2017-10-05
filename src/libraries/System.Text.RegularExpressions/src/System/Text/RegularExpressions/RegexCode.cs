// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexCode class is internal to the regular expression package.
// It provides operator constants for use by the Builder and the Machine.

// Implementation notes:
//
// Regexps are built into RegexCodes, which contain an operation array,
// a string table, and some constants.
//
// Each operation is one of the codes below, followed by the integer
// operands specified for each op.
//
// Strings and sets are indices into a string table.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexCode
    {
        // The following primitive operations come directly from the parser

                                                    // lef/back operands        description
        internal const int Onerep = 0;              // lef,back char,min,max    a {n}
        internal const int Notonerep = 1;           // lef,back char,min,max    .{n}
        internal const int Setrep = 2;              // lef,back set,min,max     [\d]{n}

        internal const int Oneloop = 3;             // lef,back char,min,max    a {,n}
        internal const int Notoneloop = 4;          // lef,back char,min,max    .{,n}
        internal const int Setloop = 5;             // lef,back set,min,max     [\d]{,n}

        internal const int Onelazy = 6;             // lef,back char,min,max    a {,n}?
        internal const int Notonelazy = 7;          // lef,back char,min,max    .{,n}?
        internal const int Setlazy = 8;             // lef,back set,min,max     [\d]{,n}?

        internal const int One = 9;                 // lef      char            a
        internal const int Notone = 10;             // lef      char            [^a]
        internal const int Set = 11;                // lef      set             [a-z\s]  \w \s \d

        internal const int Multi = 12;              // lef      string          abcd
        internal const int Ref = 13;                // lef      group           \#

        internal const int Bol = 14;                //                          ^
        internal const int Eol = 15;                //                          $
        internal const int Boundary = 16;           //                          \b
        internal const int Nonboundary = 17;        //                          \B
        internal const int Beginning = 18;          //                          \A
        internal const int Start = 19;              //                          \G
        internal const int EndZ = 20;               //                          \Z
        internal const int End = 21;                //                          \Z

        internal const int Nothing = 22;            //                          Reject!

        // Primitive control structures

        internal const int Lazybranch = 23;         // back     jump            straight first
        internal const int Branchmark = 24;         // back     jump            branch first for loop
        internal const int Lazybranchmark = 25;     // back     jump            straight first for loop
        internal const int Nullcount = 26;          // back     val             set counter, null mark
        internal const int Setcount = 27;           // back     val             set counter, make mark
        internal const int Branchcount = 28;        // back     jump,limit      branch++ if zero<=c<limit
        internal const int Lazybranchcount = 29;    // back     jump,limit      same, but straight first
        internal const int Nullmark = 30;           // back                     save position
        internal const int Setmark = 31;            // back                     save position
        internal const int Capturemark = 32;        // back     group           define group
        internal const int Getmark = 33;            // back                     recall position
        internal const int Setjump = 34;            // back                     save backtrack state
        internal const int Backjump = 35;           //                          zap back to saved state
        internal const int Forejump = 36;           //                          zap backtracking state
        internal const int Testref = 37;            //                          backtrack if ref undefined
        internal const int Goto = 38;               //          jump            just go

        internal const int Prune = 39;              //                          prune it baby
        internal const int Stop = 40;               //                          done!

        internal const int ECMABoundary = 41;       //                          \b
        internal const int NonECMABoundary = 42;    //                          \B

        // Modifiers for alternate modes
        internal const int Mask = 63;   // Mask to get unmodified ordinary operator
        internal const int Rtl = 64;    // bit to indicate that we're reverse scanning.
        internal const int Back = 128;  // bit to indicate that we're backtracking.
        internal const int Back2 = 256; // bit to indicate that we're backtracking on a second branch.
        internal const int Ci = 512;    // bit to indicate that we're case-insensitive.

        internal readonly int[] _codes;                     // the code
        internal readonly string[] _strings;                // the string/set table
        internal readonly int _trackcount;                  // how many instructions use backtracking
        internal readonly Hashtable _caps;                  // mapping of user group numbers -> impl group slots
        internal readonly int _capsize;                     // number of impl group slots
        internal readonly RegexPrefix _fcPrefix;            // the set of candidate first characters (may be null)
        internal readonly RegexBoyerMoore _bmPrefix;        // the fixed prefix string as a Boyer-Moore machine (may be null)
        internal readonly int _anchors;                     // the set of zero-length start anchors (RegexFCD.Bol, etc)
        internal readonly bool _rightToLeft;                // true if right to left

        internal RegexCode(int[] codes, List<string> stringlist, int trackcount,
                           Hashtable caps, int capsize,
                           RegexBoyerMoore bmPrefix, RegexPrefix fcPrefix,
                           int anchors, bool rightToLeft)
        {
            Debug.Assert(codes != null, "codes cannot be null.");
            Debug.Assert(stringlist != null, "stringlist cannot be null.");

            _codes = codes;
            _strings = stringlist.ToArray();
            _trackcount = trackcount;
            _caps = caps;
            _capsize = capsize;
            _bmPrefix = bmPrefix;
            _fcPrefix = fcPrefix;
            _anchors = anchors;
            _rightToLeft = rightToLeft;
        }

        internal static bool OpcodeBacktracks(int Op)
        {
            Op &= Mask;

            switch (Op)
            {
                case Oneloop:
                case Notoneloop:
                case Setloop:
                case Onelazy:
                case Notonelazy:
                case Setlazy:
                case Lazybranch:
                case Branchmark:
                case Lazybranchmark:
                case Nullcount:
                case Setcount:
                case Branchcount:
                case Lazybranchcount:
                case Setmark:
                case Capturemark:
                case Getmark:
                case Setjump:
                case Backjump:
                case Forejump:
                case Goto:
                    return true;

                default:
                    return false;
            }
        }

        internal static int OpcodeSize(int opcode)
        {
            opcode &= Mask;

            switch (opcode)
            {
                case Nothing:
                case Bol:
                case Eol:
                case Boundary:
                case Nonboundary:
                case ECMABoundary:
                case NonECMABoundary:
                case Beginning:
                case Start:
                case EndZ:
                case End:
                case Nullmark:
                case Setmark:
                case Getmark:
                case Setjump:
                case Backjump:
                case Forejump:
                case Stop:
                    return 1;

                case One:
                case Notone:
                case Multi:
                case Ref:
                case Testref:
                case Goto:
                case Nullcount:
                case Setcount:
                case Lazybranch:
                case Branchmark:
                case Lazybranchmark:
                case Prune:
                case Set:
                    return 2;

                case Capturemark:
                case Branchcount:
                case Lazybranchcount:
                case Onerep:
                case Notonerep:
                case Oneloop:
                case Notoneloop:
                case Onelazy:
                case Notonelazy:
                case Setlazy:
                case Setrep:
                case Setloop:
                    return 3;

                default:
                    throw new ArgumentException(SR.Format(SR.UnexpectedOpcode, opcode.ToString(CultureInfo.CurrentCulture)));
            }
        }

#if DEBUG
        private static readonly string[] CodeStr = new string[]
        {
            "Onerep", "Notonerep", "Setrep",
            "Oneloop", "Notoneloop", "Setloop",
            "Onelazy", "Notonelazy", "Setlazy",
            "One", "Notone", "Set",
            "Multi", "Ref",
            "Bol", "Eol", "Boundary", "Nonboundary", "Beginning", "Start", "EndZ", "End",
            "Nothing",
            "Lazybranch", "Branchmark", "Lazybranchmark",
            "Nullcount", "Setcount", "Branchcount", "Lazybranchcount",
            "Nullmark", "Setmark", "Capturemark", "Getmark",
            "Setjump", "Backjump", "Forejump", "Testref", "Goto",
            "Prune", "Stop",
#if ECMA
            "ECMABoundary", "NonECMABoundary",
#endif
        };

        internal static string OperatorDescription(int Opcode)
        {
            bool isCi = ((Opcode & Ci) != 0);
            bool isRtl = ((Opcode & Rtl) != 0);
            bool isBack = ((Opcode & Back) != 0);
            bool isBack2 = ((Opcode & Back2) != 0);

            return CodeStr[Opcode & Mask] +
            (isCi ? "-Ci" : "") + (isRtl ? "-Rtl" : "") + (isBack ? "-Back" : "") + (isBack2 ? "-Back2" : "");
        }

        internal string OpcodeDescription(int offset)
        {
            StringBuilder sb = new StringBuilder();
            int opcode = _codes[offset];

            sb.AppendFormat("{0:D6} ", offset);
            sb.Append(OpcodeBacktracks(opcode & Mask) ? '*' : ' ');
            sb.Append(OperatorDescription(opcode));
            sb.Append('(');

            opcode &= Mask;

            switch (opcode)
            {
                case One:
                case Notone:
                case Onerep:
                case Notonerep:
                case Oneloop:
                case Notoneloop:
                case Onelazy:
                case Notonelazy:
                    sb.Append("Ch = ");
                    sb.Append(RegexCharClass.CharDescription((char)_codes[offset + 1]));
                    break;

                case Set:
                case Setrep:
                case Setloop:
                case Setlazy:
                    sb.Append("Set = ");
                    sb.Append(RegexCharClass.SetDescription(_strings[_codes[offset + 1]]));
                    break;

                case Multi:
                    sb.Append("String = ");
                    sb.Append(_strings[_codes[offset + 1]]);
                    break;

                case Ref:
                case Testref:
                    sb.Append("Index = ");
                    sb.Append(_codes[offset + 1]);
                    break;

                case Capturemark:
                    sb.Append("Index = ");
                    sb.Append(_codes[offset + 1]);
                    if (_codes[offset + 2] != -1)
                    {
                        sb.Append(", Unindex = ");
                        sb.Append(_codes[offset + 2]);
                    }
                    break;

                case Nullcount:
                case Setcount:
                    sb.Append("Value = ");
                    sb.Append(_codes[offset + 1]);
                    break;

                case Goto:
                case Lazybranch:
                case Branchmark:
                case Lazybranchmark:
                case Branchcount:
                case Lazybranchcount:
                    sb.Append("Addr = ");
                    sb.Append(_codes[offset + 1]);
                    break;
            }

            switch (opcode)
            {
                case Onerep:
                case Notonerep:
                case Oneloop:
                case Notoneloop:
                case Onelazy:
                case Notonelazy:
                case Setrep:
                case Setloop:
                case Setlazy:
                    sb.Append(", Rep = ");
                    if (_codes[offset + 2] == int.MaxValue)
                        sb.Append("inf");
                    else
                        sb.Append(_codes[offset + 2]);
                    break;

                case Branchcount:
                case Lazybranchcount:
                    sb.Append(", Limit = ");
                    if (_codes[offset + 2] == int.MaxValue)
                        sb.Append("inf");
                    else
                        sb.Append(_codes[offset + 2]);
                    break;
            }

            sb.Append(')');

            return sb.ToString();
        }

        internal void Dump()
        {
            int i;

            Debug.WriteLine("Direction:  " + (_rightToLeft ? "right-to-left" : "left-to-right"));
            Debug.WriteLine("Firstchars: " + (_fcPrefix == null ? "n/a" : RegexCharClass.SetDescription(_fcPrefix.Prefix)));
            Debug.WriteLine("Prefix:     " + (_bmPrefix == null ? "n/a" : Regex.Escape(_bmPrefix.ToString())));
            Debug.WriteLine("Anchors:    " + RegexFCD.AnchorDescription(_anchors));
            Debug.WriteLine("");
            if (_bmPrefix != null)
            {
                Debug.WriteLine("BoyerMoore:");
                Debug.WriteLine(_bmPrefix.Dump("    "));
            }
            for (i = 0; i < _codes.Length;)
            {
                Debug.WriteLine(OpcodeDescription(i));
                i += OpcodeSize(_codes[i]);
            }

            Debug.WriteLine("");
        }
#endif
    }
}
