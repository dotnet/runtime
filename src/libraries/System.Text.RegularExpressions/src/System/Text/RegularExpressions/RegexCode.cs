// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This RegexCode class is internal to the regular expression package.

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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        public const int NonBoundary = 17;        //                          \B
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

        public const int Stop = 40;               //                          done!

        public const int ECMABoundary = 41;       //                          \b
        public const int NonECMABoundary = 42;    //                          \B

        // Manufactured primitive operations, derived from the tree that comes from the parser.
        // These exist to reduce backtracking (both actually performing it and spitting code for it).

        public const int Oneloopatomic = 43;      // lef,back char,min,max    (?> a {,n} )
        public const int Notoneloopatomic = 44;   // lef,back set,min,max     (?> . {,n} )
        public const int Setloopatomic = 45;      // lef,back set,min,max     (?> [\d]{,n} )
        public const int UpdateBumpalong = 46;    // updates the bumpalong position to the current position

        // Modifiers for alternate modes
        public const int Mask = 63;   // Mask to get unmodified ordinary operator
        public const int Rtl = 64;    // bit to indicate that we're reverse scanning.
        public const int Back = 128;  // bit to indicate that we're backtracking.
        public const int Back2 = 256; // bit to indicate that we're backtracking on a second branch.
        public const int Ci = 512;    // bit to indicate that we're case-insensitive.

        public readonly RegexTree Tree;                                                 // the optimized parse tree
        public readonly int[] Codes;                                                    // the code
        public readonly string[] Strings;                                               // the string/set table
        public readonly uint[]?[] StringsAsciiLookup;                                   // the ASCII lookup table optimization for the sets in Strings
        public readonly int TrackCount;                                                 // how many instructions use backtracking
        public readonly Hashtable? Caps;                                                // mapping of user group numbers -> impl group slots
        public readonly int CapSize;                                                    // number of impl group slots
        public readonly bool RightToLeft;                                               // true if right to left
        public readonly RegexFindOptimizations FindOptimizations;

        public RegexCode(RegexTree tree, CultureInfo culture, int[] codes, string[] strings, int trackcount,
                         Hashtable? caps, int capsize)
        {
            Tree = tree;
            Codes = codes;
            Strings = strings;
            StringsAsciiLookup = new uint[strings.Length][];
            TrackCount = trackcount;
            Caps = caps;
            CapSize = capsize;
            RightToLeft = (tree.Options & RegexOptions.RightToLeft) != 0;
            FindOptimizations = new RegexFindOptimizations(tree, culture);
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
                case NonBoundary:
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
                case UpdateBumpalong:
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
                case Set:
                    return 2;

                case Capturemark:
                case Branchcount:
                case Lazybranchcount:
                case Onerep:
                case Notonerep:
                case Oneloop:
                case Oneloopatomic:
                case Notoneloop:
                case Notoneloopatomic:
                case Onelazy:
                case Notonelazy:
                case Setlazy:
                case Setrep:
                case Setloop:
                case Setloopatomic:
                    return 3;

                default:
                    throw new ArgumentException(SR.Format(SR.UnexpectedOpcode, opcode.ToString()));
            }
        }

        [ExcludeFromCodeCoverage]
        private static string OperatorDescription(int Opcode)
        {
            string codeStr = (Opcode & Mask) switch
            {
                Onerep => nameof(Onerep),
                Notonerep => nameof(Notonerep),
                Setrep => nameof(Setrep),
                Oneloop => nameof(Oneloop),
                Notoneloop => nameof(Notoneloop),
                Setloop => nameof(Setloop),
                Onelazy => nameof(Onelazy),
                Notonelazy => nameof(Notonelazy),
                Setlazy => nameof(Setlazy),
                One => nameof(One),
                Notone => nameof(Notone),
                Set => nameof(Set),
                Multi => nameof(Multi),
                Ref => nameof(Ref),
                Bol => nameof(Bol),
                Eol => nameof(Eol),
                Boundary => nameof(Boundary),
                NonBoundary => nameof(NonBoundary),
                Beginning => nameof(Beginning),
                Start => nameof(Start),
                EndZ => nameof(EndZ),
                End => nameof(End),
                Nothing => nameof(Nothing),
                Lazybranch => nameof(Lazybranch),
                Branchmark => nameof(Branchmark),
                Lazybranchmark => nameof(Lazybranchmark),
                Nullcount => nameof(Nullcount),
                Setcount => nameof(Setcount),
                Branchcount => nameof(Branchcount),
                Lazybranchcount => nameof(Lazybranchcount),
                Nullmark => nameof(Nullmark),
                Setmark => nameof(Setmark),
                Capturemark => nameof(Capturemark),
                Getmark => nameof(Getmark),
                Setjump => nameof(Setjump),
                Backjump => nameof(Backjump),
                Forejump => nameof(Forejump),
                Testref => nameof(Testref),
                Goto => nameof(Goto),
                Stop => nameof(Stop),
                ECMABoundary => nameof(ECMABoundary),
                NonECMABoundary => nameof(NonECMABoundary),
                Oneloopatomic => nameof(Oneloopatomic),
                Notoneloopatomic => nameof(Notoneloopatomic),
                Setloopatomic => nameof(Setloopatomic),
                UpdateBumpalong => nameof(UpdateBumpalong),
                _ => "(unknown)"
            };

            return
                codeStr +
                ((Opcode & Ci) != 0 ? "-Ci" : "") +
                ((Opcode & Rtl) != 0 ? "-Rtl" : "") +
                ((Opcode & Back) != 0 ? "-Back" : "") +
                ((Opcode & Back2) != 0 ? "-Back2" : "");
        }

        [ExcludeFromCodeCoverage]
        internal string OpcodeDescription(int offset) => OpcodeDescription(offset, Codes, Strings);

        [ExcludeFromCodeCoverage]
        internal static string OpcodeDescription(int offset, int[] codes, string[] strings)
        {
            var sb = new StringBuilder();
            int opcode = codes[offset];

            sb.Append($"{offset:D6} ");
            sb.Append(OpcodeBacktracks(opcode & Mask) ? '*' : ' ');
            sb.Append(OperatorDescription(opcode));

            opcode &= Mask;

            switch (opcode)
            {
                case One:
                case Notone:
                case Onerep:
                case Notonerep:
                case Oneloop:
                case Oneloopatomic:
                case Notoneloop:
                case Notoneloopatomic:
                case Onelazy:
                case Notonelazy:
                    sb.Append(Indent()).Append('\'').Append(RegexCharClass.CharDescription((char)codes[offset + 1])).Append('\'');
                    break;

                case Set:
                case Setrep:
                case Setloop:
                case Setloopatomic:
                case Setlazy:
                    sb.Append(Indent()).Append(RegexCharClass.SetDescription(strings[codes[offset + 1]]));
                    break;

                case Multi:
                    sb.Append(Indent()).Append('"').Append(strings[codes[offset + 1]]).Append('"');
                    break;

                case Ref:
                case Testref:
                    sb.Append(Indent()).Append("index = ").Append(codes[offset + 1]);
                    break;

                case Capturemark:
                    sb.Append(Indent()).Append("index = ").Append(codes[offset + 1]);
                    if (codes[offset + 2] != -1)
                    {
                        sb.Append(", unindex = ").Append(codes[offset + 2]);
                    }
                    break;

                case Nullcount:
                case Setcount:
                    sb.Append(Indent()).Append("value = ").Append(codes[offset + 1]);
                    break;

                case Goto:
                case Lazybranch:
                case Branchmark:
                case Lazybranchmark:
                case Branchcount:
                case Lazybranchcount:
                    sb.Append(Indent()).Append("addr = ").Append(codes[offset + 1]);
                    break;
            }

            switch (opcode)
            {
                case Onerep:
                case Notonerep:
                case Oneloop:
                case Oneloopatomic:
                case Notoneloop:
                case Notoneloopatomic:
                case Onelazy:
                case Notonelazy:
                case Setrep:
                case Setloop:
                case Setloopatomic:
                case Setlazy:
                    sb.Append(", rep = ");
                    if (codes[offset + 2] == int.MaxValue)
                    {
                        sb.Append("inf");
                    }
                    else
                    {
                        sb.Append(codes[offset + 2]);
                    }
                    break;

                case Branchcount:
                case Lazybranchcount:
                    sb.Append(", limit = ");
                    if (codes[offset + 2] == int.MaxValue)
                    {
                        sb.Append("inf");
                    }
                    else
                    {
                        sb.Append(codes[offset + 2]);
                    }
                    break;
            }

            string Indent() => new string(' ', Math.Max(1, 25 - sb.Length));

            return sb.ToString();
        }

#if DEBUG
        [ExcludeFromCodeCoverage]
        public void Dump() => Debug.WriteLine(ToString());

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Direction:  {(RightToLeft ? "right-to-left" : "left-to-right")}");
            sb.AppendLine($"Anchor:     {RegexPrefixAnalyzer.AnchorDescription(FindOptimizations.LeadingAnchor)}");
            sb.AppendLine();
            for (int i = 0; i < Codes.Length; i += OpcodeSize(Codes[i]))
            {
                sb.AppendLine(OpcodeDescription(i));
            }
            sb.AppendLine();

            return sb.ToString();
        }
#endif
    }
}
