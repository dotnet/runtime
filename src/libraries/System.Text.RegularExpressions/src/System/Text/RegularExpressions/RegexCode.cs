// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    /// <summary>Representation of a regular expression, written by <see cref="RegexWriter"/> and containing the code evaluated by <see cref="RegexInterpreter"/>.</summary>
    /// <remarks>It currently stores some data used by engines other than the interpreter; that can be refactored out in the future.</remarks>
    internal sealed class RegexCode
    {
        /// <summary>The optimized parse tree.</summary>
        public readonly RegexTree Tree;
        /// <summary>RegexOpcodes and arguments written by <see cref="RegexWriter"/>.</summary>
        public readonly int[] Codes;
        /// <summary>The string / set table. <see cref="Codes"/> includes offsets into this table, for string and set arguments.</summary>
        public readonly string[] Strings;
        /// <summary>ASCII lookup table optimization for sets in <see cref="Strings"/>.</summary>
        public readonly uint[]?[] StringsAsciiLookup;
        /// <summary>How many instructions in <see cref="Codes"/> use backtracking.</summary>
        public readonly int TrackCount;
        /// <summary>Mapping of user group numbers to impl group slots.</summary>
        public readonly Hashtable? Caps;
        /// <summary>Number of impl group slots.</summary>
        public readonly int CapSize;
        /// <summary>True if right to left.</summary>
        public readonly bool RightToLeft;
        /// <summary>Optimization mode and supporting data to enable quickly finding the next possible match location.</summary>
        public readonly RegexFindOptimizations FindOptimizations;

        public RegexCode(RegexTree tree, CultureInfo culture, int[] codes, string[] strings, int trackcount, Hashtable? caps, int capsize)
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

        /// <summary>Gets whether the specified opcode may incur backtracking.</summary>
        public static bool OpcodeBacktracks(RegexOpcode opcode)
        {
            opcode &= RegexOpcode.OperatorMask;

            switch (opcode)
            {
                case RegexOpcode.Oneloop:
                case RegexOpcode.Onelazy:
                case RegexOpcode.Notoneloop:
                case RegexOpcode.Notonelazy:
                case RegexOpcode.Setloop:
                case RegexOpcode.Setlazy:
                case RegexOpcode.Lazybranch:
                case RegexOpcode.Branchmark:
                case RegexOpcode.Lazybranchmark:
                case RegexOpcode.Nullcount:
                case RegexOpcode.Setcount:
                case RegexOpcode.Branchcount:
                case RegexOpcode.Lazybranchcount:
                case RegexOpcode.Setmark:
                case RegexOpcode.Capturemark:
                case RegexOpcode.Getmark:
                case RegexOpcode.Setjump:
                case RegexOpcode.Backjump:
                case RegexOpcode.Forejump:
                case RegexOpcode.Goto:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Gets the number of integers required to store an operation represented by the specified opcode (including the opcode).</summary>
        /// <returns>Values range from 1 (just the opcode) to 3 (the opcode plus up to two operands).</returns>
        public static int OpcodeSize(RegexOpcode opcode)
        {
            opcode &= RegexOpcode.OperatorMask;
            switch (opcode)
            {
                case RegexOpcode.Nothing:
                case RegexOpcode.Bol:
                case RegexOpcode.Eol:
                case RegexOpcode.Boundary:
                case RegexOpcode.NonBoundary:
                case RegexOpcode.ECMABoundary:
                case RegexOpcode.NonECMABoundary:
                case RegexOpcode.Beginning:
                case RegexOpcode.Start:
                case RegexOpcode.EndZ:
                case RegexOpcode.End:
                case RegexOpcode.Nullmark:
                case RegexOpcode.Setmark:
                case RegexOpcode.Getmark:
                case RegexOpcode.Setjump:
                case RegexOpcode.Backjump:
                case RegexOpcode.Forejump:
                case RegexOpcode.Stop:
                case RegexOpcode.UpdateBumpalong:
                    // The opcode has no operands.
                    return 1;

                case RegexOpcode.One:
                case RegexOpcode.Notone:
                case RegexOpcode.Multi:
                case RegexOpcode.Backreference:
                case RegexOpcode.TestBackreference:
                case RegexOpcode.Goto:
                case RegexOpcode.Nullcount:
                case RegexOpcode.Setcount:
                case RegexOpcode.Lazybranch:
                case RegexOpcode.Branchmark:
                case RegexOpcode.Lazybranchmark:
                case RegexOpcode.Set:
                    // The opcode has one operand.
                    return 2;

                case RegexOpcode.Capturemark:
                case RegexOpcode.Branchcount:
                case RegexOpcode.Lazybranchcount:
                case RegexOpcode.Onerep:
                case RegexOpcode.Notonerep:
                case RegexOpcode.Oneloop:
                case RegexOpcode.Oneloopatomic:
                case RegexOpcode.Notoneloop:
                case RegexOpcode.Notoneloopatomic:
                case RegexOpcode.Onelazy:
                case RegexOpcode.Notonelazy:
                case RegexOpcode.Setlazy:
                case RegexOpcode.Setrep:
                case RegexOpcode.Setloop:
                case RegexOpcode.Setloopatomic:
                    // The opcode has two operands.
                    return 3;

                default:
                    Debug.Fail($"Unknown opcode: {opcode}");
                    goto case RegexOpcode.Stop;
            }
        }

#if DEBUG
        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Direction: {(RightToLeft ? "right-to-left" : "left-to-right")}");
            sb.AppendLine($"Anchor:    {FindOptimizations.LeadingAnchor}");
            sb.AppendLine();
            for (int i = 0; i < Codes.Length; i += OpcodeSize((RegexOpcode)Codes[i]))
            {
                sb.AppendLine(DescribeInstruction(i));
            }

            return sb.ToString();
        }

        [ExcludeFromCodeCoverage]
        internal string DescribeInstruction(int opcodeOffset)
        {
            RegexOpcode opcode = (RegexOpcode)Codes[opcodeOffset];

            var sb = new StringBuilder();
            sb.Append($"{opcodeOffset:D6} ");
            sb.Append(OpcodeBacktracks(opcode & RegexOpcode.OperatorMask) ? '~' : ' ');
            sb.Append(opcode & RegexOpcode.OperatorMask);
            if ((opcode & RegexOpcode.CaseInsensitive) != 0) sb.Append("-Ci");
            if ((opcode & RegexOpcode.RightToLeft) != 0) sb.Append("-Rtl");
            if ((opcode & RegexOpcode.Backtracking) != 0) sb.Append("-Back");
            if ((opcode & RegexOpcode.BacktrackingSecond) != 0) sb.Append("-Back2");

            opcode &= RegexOpcode.OperatorMask;

            switch (opcode)
            {
                case RegexOpcode.One:
                case RegexOpcode.Onerep:
                case RegexOpcode.Oneloop:
                case RegexOpcode.Oneloopatomic:
                case RegexOpcode.Onelazy:
                case RegexOpcode.Notone:
                case RegexOpcode.Notonerep:
                case RegexOpcode.Notoneloop:
                case RegexOpcode.Notoneloopatomic:
                case RegexOpcode.Notonelazy:
                    sb.Append(Indent()).Append('\'').Append(RegexCharClass.DescribeChar((char)Codes[opcodeOffset + 1])).Append('\'');
                    break;

                case RegexOpcode.Set:
                case RegexOpcode.Setrep:
                case RegexOpcode.Setloop:
                case RegexOpcode.Setloopatomic:
                case RegexOpcode.Setlazy:
                    sb.Append(Indent()).Append(RegexCharClass.DescribeSet(Strings[Codes[opcodeOffset + 1]]));
                    break;

                case RegexOpcode.Multi:
                    sb.Append(Indent()).Append('"').Append(Strings[Codes[opcodeOffset + 1]]).Append('"');
                    break;

                case RegexOpcode.Backreference:
                case RegexOpcode.TestBackreference:
                    sb.Append(Indent()).Append("index = ").Append(Codes[opcodeOffset + 1]);
                    break;

                case RegexOpcode.Capturemark:
                    sb.Append(Indent()).Append("index = ").Append(Codes[opcodeOffset + 1]);
                    if (Codes[opcodeOffset + 2] != -1)
                    {
                        sb.Append(", unindex = ").Append(Codes[opcodeOffset + 2]);
                    }
                    break;

                case RegexOpcode.Nullcount:
                case RegexOpcode.Setcount:
                    sb.Append(Indent()).Append("value = ").Append(Codes[opcodeOffset + 1]);
                    break;

                case RegexOpcode.Goto:
                case RegexOpcode.Lazybranch:
                case RegexOpcode.Branchmark:
                case RegexOpcode.Lazybranchmark:
                case RegexOpcode.Branchcount:
                case RegexOpcode.Lazybranchcount:
                    sb.Append(Indent()).Append("addr = ").Append(Codes[opcodeOffset + 1]);
                    break;
            }

            switch (opcode)
            {
                case RegexOpcode.Onerep:
                case RegexOpcode.Oneloop:
                case RegexOpcode.Oneloopatomic:
                case RegexOpcode.Onelazy:
                case RegexOpcode.Notonerep:
                case RegexOpcode.Notoneloop:
                case RegexOpcode.Notoneloopatomic:
                case RegexOpcode.Notonelazy:
                case RegexOpcode.Setrep:
                case RegexOpcode.Setloop:
                case RegexOpcode.Setloopatomic:
                case RegexOpcode.Setlazy:
                    sb.Append(", rep = ").Append(Codes[opcodeOffset + 2] == int.MaxValue ? "inf" : Codes[opcodeOffset + 2]);
                    break;

                case RegexOpcode.Branchcount:
                case RegexOpcode.Lazybranchcount:
                    sb.Append(", limit = ").Append(Codes[opcodeOffset + 2] == int.MaxValue ? "inf" : Codes[opcodeOffset + 2]);
                    break;
            }

            return sb.ToString();

            string Indent() => new string(' ', Math.Max(1, 25 - sb.Length));
        }
#endif
    }
}
