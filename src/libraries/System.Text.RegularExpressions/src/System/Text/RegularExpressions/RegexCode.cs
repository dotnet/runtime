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
        public const int Onerep = 0;              // lef,back char,min,max    a {n}
        public const int Notonerep = 1;           // lef,back char,min,max    .{n}
        public const int Setrep = 2;              // lef,back set,min,max     [\d]{n}

        public const int Oneloop = 3;             // lef,back char,min,max    a {,n}
        public const int Notoneloop = 4;          // lef,back char,min,max    .{,n}
        public const int Setloop = 5;             // lef,back set,min,max     [\d]{,n}

        public const int Onelazy = 6;             // lef,back char,min,max    a {,n}?
        public const int Notonelazy = 7;          // lef,back char,min,max    .{,n}?
        public const int Setlazy = 8;             // lef,back set,min,max     [\d]{,n}?

        public const int One = 9;                 // lef      char            a
        public const int Notone = 10;             // lef      char            [^a]
        public const int Set = 11;                // lef      set             [a-z\s]  \w \s \d

        public const int Multi = 12;              // lef      string          abcd
        public const int Ref = 13;                // lef      group           \#

        public const int Bol = 14;                //                          ^
        public const int Eol = 15;                //                          $
        public const int Boundary = 16;           //                          \b
        public const int Nonboundary = 17;        //                          \B
        public const int Beginning = 18;          //                          \A
        public const int Start = 19;              //                          \G
        public const int EndZ = 20;               //                          \Z
        public const int End = 21;                //                          \Z

        public const int Nothing = 22;            //                          Reject!

        // Primitive control structures

        public const int Lazybranch = 23;         // back     jump            straight first
        public const int Branchmark = 24;         // back     jump            branch first for loop
        public const int Lazybranchmark = 25;     // back     jump            straight first for loop
        public const int Nullcount = 26;          // back     val             set counter, null mark
        public const int Setcount = 27;           // back     val             set counter, make mark
        public const int Branchcount = 28;        // back     jump,limit      branch++ if zero<=c<limit
        public const int Lazybranchcount = 29;    // back     jump,limit      same, but straight first
        public const int Nullmark = 30;           // back                     save position
        public const int Setmark = 31;            // back                     save position
        public const int Capturemark = 32;        // back     group           define group
        public const int Getmark = 33;            // back                     recall position
        public const int Setjump = 34;            // back                     save backtrack state
        public const int Backjump = 35;           //                          zap back to saved state
        public const int Forejump = 36;           //                          zap backtracking state
        public const int Testref = 37;            //                          backtrack if ref undefined
        public const int Goto = 38;               //          jump            just go

        public const int Prune = 39;              //                          prune it baby
        public const int Stop = 40;               //                          done!

        public const int ECMABoundary = 41;       //                          \b
        public const int NonECMABoundary = 42;    //                          \B

        // Modifiers for alternate modes
        public const int Mask = 63;   // Mask to get unmodified ordinary operator
        public const int Rtl = 64;    // bit to indicate that we're reverse scanning.
        public const int Back = 128;  // bit to indicate that we're backtracking.
        public const int Back2 = 256; // bit to indicate that we're backtracking on a second branch.
        public const int Ci = 512;    // bit to indicate that we're case-insensitive.

        public readonly int[] Codes;                     // the code
        public readonly string[] Strings;                // the string/set table
        public readonly int TrackCount;                  // how many instructions use backtracking
        public readonly Hashtable Caps;                  // mapping of user group numbers -> impl group slots
        public readonly int CapSize;                     // number of impl group slots
        public readonly RegexPrefix? FCPrefix;           // the set of candidate first characters (may be null)
        public readonly RegexBoyerMoore BMPrefix;        // the fixed prefix string as a Boyer-Moore machine (may be null)
        public readonly int Anchors;                     // the set of zero-length start anchors (RegexFCD.Bol, etc)
        public readonly bool RightToLeft;                // true if right to left

        public RegexCode(int[] codes, List<string> stringlist, int trackcount,
                           Hashtable caps, int capsize,
                           RegexBoyerMoore bmPrefix, RegexPrefix? fcPrefix,
                           int anchors, bool rightToLeft)
        {
            Debug.Assert(codes != null, "codes cannot be null.");
            Debug.Assert(stringlist != null, "stringlist cannot be null.");

            Codes = codes;
            Strings = stringlist.ToArray();
            TrackCount = trackcount;
            Caps = caps;
            CapSize = capsize;
            BMPrefix = bmPrefix;
            FCPrefix = fcPrefix;
            Anchors = anchors;
            RightToLeft = rightToLeft;
        }

        public static bool OpcodeBacktracks(int Op)
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

        public static int OpcodeSize(int opcode)
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
                    throw new ArgumentException(SR.Format(SR.UnexpectedOpcode, opcode.ToString()));
            }
        }

#if DEBUG
        private static readonly string[] s_codeStr = new string[]
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

        private static string OperatorDescription(int Opcode)
        {
            bool isCi = ((Opcode & Ci) != 0);
            bool isRtl = ((Opcode & Rtl) != 0);
            bool isBack = ((Opcode & Back) != 0);
            bool isBack2 = ((Opcode & Back2) != 0);

            return s_codeStr[Opcode & Mask] +
            (isCi ? "-Ci" : "") + (isRtl ? "-Rtl" : "") + (isBack ? "-Back" : "") + (isBack2 ? "-Back2" : "");
        }

        public string OpcodeDescription(int offset)
        {
            StringBuilder sb = new StringBuilder();
            int opcode = Codes[offset];

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
                    sb.Append(RegexCharClass.CharDescription((char)Codes[offset + 1]));
                    break;

                case Set:
                case Setrep:
                case Setloop:
                case Setlazy:
                    sb.Append("Set = ");
                    sb.Append(RegexCharClass.SetDescription(Strings[Codes[offset + 1]]));
                    break;

                case Multi:
                    sb.Append("String = ");
                    sb.Append(Strings[Codes[offset + 1]]);
                    break;

                case Ref:
                case Testref:
                    sb.Append("Index = ");
                    sb.Append(Codes[offset + 1]);
                    break;

                case Capturemark:
                    sb.Append("Index = ");
                    sb.Append(Codes[offset + 1]);
                    if (Codes[offset + 2] != -1)
                    {
                        sb.Append(", Unindex = ");
                        sb.Append(Codes[offset + 2]);
                    }
                    break;

                case Nullcount:
                case Setcount:
                    sb.Append("Value = ");
                    sb.Append(Codes[offset + 1]);
                    break;

                case Goto:
                case Lazybranch:
                case Branchmark:
                case Lazybranchmark:
                case Branchcount:
                case Lazybranchcount:
                    sb.Append("Addr = ");
                    sb.Append(Codes[offset + 1]);
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
                    if (Codes[offset + 2] == int.MaxValue)
                        sb.Append("inf");
                    else
                        sb.Append(Codes[offset + 2]);
                    break;

                case Branchcount:
                case Lazybranchcount:
                    sb.Append(", Limit = ");
                    if (Codes[offset + 2] == int.MaxValue)
                        sb.Append("inf");
                    else
                        sb.Append(Codes[offset + 2]);
                    break;
            }

            sb.Append(')');

            return sb.ToString();
        }

        public void Dump()
        {
            int i;

            Debug.WriteLine("Direction:  " + (RightToLeft ? "right-to-left" : "left-to-right"));
            Debug.WriteLine("Firstchars: " + (FCPrefix == null ? "n/a" : RegexCharClass.SetDescription(FCPrefix.GetValueOrDefault().Prefix)));
            Debug.WriteLine("Prefix:     " + (BMPrefix == null ? "n/a" : Regex.Escape(BMPrefix.ToString())));
            Debug.WriteLine("Anchors:    " + RegexFCD.AnchorDescription(Anchors));
            Debug.WriteLine("");
            if (BMPrefix != null)
            {
                Debug.WriteLine("BoyerMoore:");
                Debug.WriteLine(BMPrefix.Dump("    "));
            }
            for (i = 0; i < Codes.Length;)
            {
                Debug.WriteLine(OpcodeDescription(i));
                i += OpcodeSize(Codes[i]);
            }

            Debug.WriteLine("");
        }
#endif
    }
}
