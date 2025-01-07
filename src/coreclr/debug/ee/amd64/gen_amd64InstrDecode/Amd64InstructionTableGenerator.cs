// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Amd64InstructionTableGenerator
{
    public class Debug
    {
        public static readonly bool debug;
        // public static readonly bool debug = true;
    }

    [Flags]
    public enum EncodingFlags : int
    {
        None = 0x0,

        P = 0x1, // OpSize (P)refix
        F2 = 0x2,
        F3 = 0x4,
        Rex = 0x8,

        W = 0x10, // VEX.W / EVEX.W
        L = 0x20, // VEX.L (for EVEX, see LL bits below)
        b = 0x40, // EVEX.b (broadcast/RC/SAE Context)

        // EVEX L'L bits. Mask off using Util.LLmask then direct compare (don't bit mask these value).
        // We want EVEX L'L=00b to have a non-zero representation, so we add 1 to the L'L value to store it.
        // Note that there is no support for EVEX L'L=11b, so we still only need 2 bits.
        // Every EVEX encoded instruction will have one of these set, even for the EVEX.LLIG case (LL ignored).
        LL00 = 0x100,
        LL01 = 0x200,
        LL10 = 0x300
    }

    public class Util
    {
        // The number of bits to shift into the `LL` positions in `EncodingFlags`.
        public static readonly int LLshift = 8;

        // A mask for the `LL` bits in `EncodingFlags`.
        public static readonly int LLmask = 0x3 << Util.LLshift;

        public static EncodingFlags ConvertEvexLLToEncodingFlags(int LL)
        {
            if ((LL < 0) || (LL > 2))
                throw new ArgumentException($"Illegal LL value: {LL}");

            return (EncodingFlags)((LL + 1) << LLshift);
        }

        public static bool EncodingFlagsHasEvexLL(EncodingFlags e)
        {
            return ((int)e & LLmask) != 0;
        }

        // There must be non-zero LL values in EncodingFlags. Return the EVEX L'L value. So, LL00 => 00b, LL01 => 01b, LL10 => 10b.
        public static int ConvertEncodingFlagsToEvexLL(EncodingFlags e)
        {
            if (!EncodingFlagsHasEvexLL(e))
                throw new ArgumentException($"EncodingFlags doesn't have EVEX L'L bits: {e}");

            int LL = (((int)e & LLmask) >> LLshift) - 1;
            if ((LL < 0) || (LL > 2))
                throw new Exception($"Unexpected LL value: {LL}");
            return LL;
        }
    }

    [Flags]
    internal enum SuffixFlags : int
    {
        None = 0x0, // No flags set
        MOp = 0x1, // Instruction supports modrm RIP memory operations
        M1st = 0x3, // Memory op is first operand normally src/dst
        MOnly = 0x7, // Memory op is only operand.  May not be a write...
        MUnknown = 0x8, // Memory op size is unknown.  Size not included in disassembly
        MAddr = 0x10, // Memory op is address load effective address
        M1B = 0x20, // Memory op is 1  byte
        M2B = 0x40, // Memory op is 2  bytes
        M4B = 0x80, // Memory op is 4  bytes
        M8B = 0x100, // Memory op is 8  bytes
        M16B = 0x200, // Memory op is 16 bytes
        M32B = 0x400, // Memory op is 32 bytes
        M64B = 0x800, // Memory op is 64 bytes
        M6B = 0x1000, // Memory op is 6  bytes
        M10B = 0x2000, // Memory op is 10 bytes
        I1B = 0x4000, // Instruction includes 1  byte  of immediates
        I2B = 0x8000, // Instruction includes 2  bytes of immediates
        I3B = 0x10000, // Instruction includes 3  bytes of immediates
        I4B = 0x20000, // Instruction includes 4  bytes of immediates
        I8B = 0x40000, // Instruction includes 8  bytes of immediates
        Unknown = 0x80000, // Instruction sample did not include a modrm configured to produce RIP addressing
    }

    internal enum Map
    {
        // Map
        None,
        Primary,
        Secondary,
        F38,
        F3A,
        Vex1, // mmmmm = 00001 (0F)
        Vex2, // mmmmm = 00010 (0F 38)
        Vex3, // mmmmm = 00011 (0F 3A)
        Evex_0F, // mmm = 001
        Evex_0F38, // mmm = 010
        Evex_0F3A, // mmm = 011
    }

    internal sealed partial class Amd64InstructionSample
    {
        [GeneratedRegex(@"^\s*(?<address>0x[a-f0-9]+)\s[^:]*:\s*(?<encoding>[0-9a-f ]*)\t(?<prefixes>(((rex[.WRXB]*)|(rep[nez]*)|(data16)|(addr32)|(lock)|(bnd)|(\{vex\})|([cdefgs]s)) +)*)(?<mnemonic>\S+) *(?<operands>(\S[^#]*?)?)\s*(?<comment>#.*)?$",
            RegexOptions.ExplicitCapture)]
        private static partial Regex EncDisassemblySplit();

        [GeneratedRegex(@"^\s*,?\s*(?<op>[^\(,]*(\([^\)]*\))?)?(?<rest>.+$)?", RegexOptions.ExplicitCapture)]
        private static partial Regex EncOperandSplit();

        [GeneratedRegex(@"\[.*\]({1to[0-9]+})?$")]
        private static partial Regex EncOperandIsMemOp();

        [GeneratedRegex(@"\[rip.*\]({1to[0-9]+})?$")]
        private static partial Regex EncOperandIsMOp();

        private static readonly HashSet<string> allOperands = new HashSet<string>();

        private static readonly Dictionary<string, SuffixFlags> memOpSize = new Dictionary<string, SuffixFlags>()
        {
            ["[rip+0x53525150]"] = SuffixFlags.MUnknown,
            ["BYTE PTR [rip+0x53525150]"] = SuffixFlags.M1B,
            ["WORD PTR [rip+0x53525150]"] = SuffixFlags.M2B,
            ["WORD PTR [rip+0x53525150]{1to8}"] = SuffixFlags.M2B,
            ["WORD PTR [rip+0x53525150]{1to16}"] = SuffixFlags.M2B,
            ["WORD PTR [rip+0x53525150]{1to32}"] = SuffixFlags.M2B,
            ["DWORD PTR [rip+0x53525150]"] = SuffixFlags.M4B,
            ["DWORD PTR [rip+0x53525150]{1to2}"] = SuffixFlags.M4B,
            ["DWORD PTR [rip+0x53525150]{1to4}"] = SuffixFlags.M4B,
            ["DWORD PTR [rip+0x53525150]{1to8}"] = SuffixFlags.M4B,
            ["DWORD PTR [rip+0x53525150]{1to16}"] = SuffixFlags.M4B,
            ["QWORD PTR [rip+0x53525150]"] = SuffixFlags.M8B,
            ["QWORD PTR [rip+0x53525150]{1to2}"] = SuffixFlags.M8B,
            ["QWORD PTR [rip+0x53525150]{1to4}"] = SuffixFlags.M8B,
            ["QWORD PTR [rip+0x53525150]{1to8}"] = SuffixFlags.M8B,
            ["OWORD PTR [rip+0x53525150]"] = SuffixFlags.M16B,
            ["XMMWORD PTR [rip+0x53525150]"] = SuffixFlags.M16B,
            ["YMMWORD PTR [rip+0x53525150]"] = SuffixFlags.M32B,
            ["ZMMWORD PTR [rip+0x53525150]"] = SuffixFlags.M64B,
            ["FWORD PTR [rip+0x53525150]"] = SuffixFlags.M6B,
            ["TBYTE PTR [rip+0x53525150]"] = SuffixFlags.M10B,
        };

        private static readonly Dictionary<string, Func<EncodingFlags, SuffixFlags>> unknownMemOps = new Dictionary<string, Func<EncodingFlags, SuffixFlags>>()
        {
            ["lddqu"] = _ => SuffixFlags.M16B,
            ["lea"] = _ => SuffixFlags.MAddr,
            ["lgdt"] = _ => SuffixFlags.M10B,
            ["lidt"] = _ => SuffixFlags.M10B,
            ["sgdt"] = _ => SuffixFlags.M10B,
            ["sidt"] = _ => SuffixFlags.M10B,
            ["vlddqu"] = e => Amd64InstructionTableGenerator.Amd64L(SuffixFlags.M32B, SuffixFlags.M16B, e),
        };

        public readonly string disassembly;
        private readonly string address;
        private readonly List<byte> encoding;
        public readonly string mnemonic;
        private readonly List<string> operands;

        public readonly Map map;
        public readonly EncodingFlags encodingFlags;
        private readonly byte opIndex; // offset into `encoding` of the opcode byte: encoding[opIndex] is the opcode byte.

        public int opCodeExt
        {
            get
            {
                const byte BytePP = 0x3;
                byte opcode = encoding[opIndex];
                byte pp = 0;

                switch (map)
                {
                    case Map.Primary:
                        break; // no pp (use zero)
                    case Map.Secondary:
                    case Map.F38:
                    case Map.F3A:
                        if (encodingFlags.HasFlag(EncodingFlags.F2))
                            pp = 0x3;
                        else if (encodingFlags.HasFlag(EncodingFlags.P))
                            pp = 0x1;
                        else if (encodingFlags.HasFlag(EncodingFlags.F3))
                            pp = 0x2;
                        break;
                    case Map.Vex1:
                    case Map.Vex2:
                    case Map.Vex3:
                        // `pp` is the low 2 bits of the last byte of the VEX prefix (either 3-byte or 2-byte form).
                        pp = (byte)(encoding[opIndex - 1] & BytePP);
                        break;
                    case Map.Evex_0F:
                    case Map.Evex_0F38:
                    case Map.Evex_0F3A:
                    {
                        var evex_p1 = encoding[opIndex - 2];
                        pp = (byte)(evex_p1 & BytePP);
                        break;
                    }
                    default:
                        return 0;
                }

                return (((int)opcode) << 4) + pp;
            }
        }

        private int suffixBytes { get { return encoding.Count - opIndex - 1; } }
        public int modrm { get { return (suffixBytes > 0) ? encoding[opIndex + 1] : 0; } }
        public int modrm_reg { get { return (modrm >> 3) & 0x7; } }

        public static HashSet<string> AllOperands { get { return allOperands; } }

        public Amd64InstructionSample(string disassembly_)
        {
            disassembly = disassembly_;

            if (Debug.debug) Console.WriteLine($"new sample: {disassembly}");

            var match = EncDisassemblySplit().Match(disassembly);

            if (Debug.debug)
            {
                foreach (Group g in match.Groups)
                {
                    Console.WriteLine($"{g.Name}:'{g}'");
                }
            }

            address = match.Groups["address"].ToString();
            encoding = parseEncoding(match.Groups["encoding"].ToString());

            mnemonic = match.Groups["mnemonic"].ToString();

            if (mnemonic.Length == 0)
                throw new ArgumentException($"Missing mnemonic: {disassembly}");

            operands = parseOperands(match.Groups["operands"].ToString());

            (map, opIndex, encodingFlags) = parsePrefix(encoding);

            if (Debug.debug) Console.WriteLine($"  opCodeExt:{opCodeExt:x3}");
        }

        private static List<byte> parseEncoding(string encodingDisassembly)
        {
            var encoding = new List<byte>();
            foreach (var b in encodingDisassembly.Split(' '))
            {
                if (Debug.debug) Console.WriteLine($"  {b}");

                encoding.Add(byte.Parse(b, NumberStyles.HexNumber));
            }
            return encoding;
        }

        private static List<string> parseOperands(string operandDisassembly)
        {
            var operands = new List<string>();
            string rest = operandDisassembly;

            while (rest?.Length != 0)
            {
                var opMatch = EncOperandSplit().Match(rest);

                string op = opMatch.Groups["op"].ToString();
                operands.Add(op);

                if (Debug.debug) Console.WriteLine($"  op:'{op}'");

                allOperands.Add(op);
                rest = opMatch.Groups["rest"].ToString();
            }
            return operands;
        }

        internal enum Prefixes : byte
        {
            Secondary = 0xf,
            ES = 0x26,
            CS = 0x2E,
            SS = 0x36,
            F38 = 0x38,
            F3A = 0x3A,
            DS = 0x3E,
            Rex = 0x40,
            Evex = 0x62,
            FS = 0x64,
            GS = 0x65,
            OpSize = 0x66,
            AddSize = 0x67,
            Vex = 0xc4,
            VexShort = 0xc5,
            Lock = 0xf0,
            Rep = 0xf2,
            Repne = 0xf3
        };

        private static (Map, byte, EncodingFlags) parsePrefix(List<byte> encoding)
        {
            Map map;
            byte operandIndex = 0;
            EncodingFlags flags = 0;
            bool done = false;

            const byte RexMask = 0xf0;
            const byte RexW = 0x8;
            const byte Vex_ByteW = 0x80;
            const byte Vex_ByteL = 0x04;

            while (!done)
            {
                switch ((Prefixes)encoding[operandIndex])
                {
                    case Prefixes.OpSize:
                        flags |= EncodingFlags.P;
                        if (Debug.debug) Console.WriteLine($"  P:66");
                        operandIndex++;
                        break;
                    case Prefixes.Rep:
                        flags |= EncodingFlags.F2;
                        if (Debug.debug) Console.WriteLine($"  P:F2");
                        operandIndex++;
                        break;
                    case Prefixes.Repne:
                        flags |= EncodingFlags.F3;
                        if (Debug.debug) Console.WriteLine($"  P:F3");
                        operandIndex++;
                        break;
                    case Prefixes.ES:
                    case Prefixes.CS:
                    case Prefixes.SS:
                    case Prefixes.DS:
                    case Prefixes.FS:
                    case Prefixes.GS:
                    case Prefixes.AddSize:
                    case Prefixes.Lock:
                        if (Debug.debug) Console.WriteLine($"  P:misc");
                        operandIndex++;
                        break;
                    default:
                        done = true;
                        break;
                }
            }

            // Handle Rex prefix. Note: Rex prefixes are all values 0x40-0x4f.
            if ((encoding[operandIndex] & RexMask) == (byte)Prefixes.Rex)
            {
                byte rex = encoding[operandIndex++];

                flags |= EncodingFlags.Rex;
                if (Debug.debug) Console.WriteLine($"  P:REX");

                if ((rex & RexW) != 0)
                {
                    flags |= EncodingFlags.W;
                    if (Debug.debug) Console.WriteLine($"  P:REX.W");
                }
            }

            switch ((Prefixes)encoding[operandIndex])
            {
                case Prefixes.Secondary:
                    if (Debug.debug) Console.WriteLine($"  P:0F");
                    switch ((Prefixes)encoding[operandIndex + 1])
                    {
                        case Prefixes.F38:
                            if (Debug.debug) Console.WriteLine($"  P:38");
                            map = Map.F38;
                            if (Debug.debug) Console.WriteLine($"  map: 0F 38");
                            operandIndex += 2;
                            break;
                        case Prefixes.F3A:
                            if (Debug.debug) Console.WriteLine($"  P:3A");
                            map = Map.F3A;
                            if (Debug.debug) Console.WriteLine($"  map: 0F 3A");
                            operandIndex += 2;
                            break;
                        default:
                            map = Map.Secondary;
                            if (Debug.debug) Console.WriteLine($"  map: 0F");
                            operandIndex += 1;
                            break;
                    }
                    break;
                case Prefixes.Vex:
                    {
                        if (Debug.debug) Console.WriteLine($"  P:VEX3");
                        switch (encoding[operandIndex + 1] & 0x1f)
                        {
                            case 0x1:
                                map = Map.Vex1;
                                if (Debug.debug) Console.WriteLine($"  map: Vex1");
                                break;
                            case 0x2:
                                map = Map.Vex2;
                                if (Debug.debug) Console.WriteLine($"  map: Vex2");
                                break;
                            case 0x3:
                                map = Map.Vex3;
                                if (Debug.debug) Console.WriteLine($"  map: Vex3");
                                break;
                            default:
                                throw new Exception($"Unexpected VEX map {encoding}");
                        }

                        var byte2 = encoding[operandIndex + 2];
                        if ((byte2 & Vex_ByteW) != 0)
                        {
                            flags |= EncodingFlags.W;
                            if (Debug.debug) Console.WriteLine($"  VEX.W");
                        }
                        if ((byte2 & Vex_ByteL) != 0)
                        {
                            flags |= EncodingFlags.L;
                            if (Debug.debug) Console.WriteLine($"  VEX.L");
                        }

                        operandIndex += 3;
                        break;
                    }
                case Prefixes.VexShort:
                    {
                        if (Debug.debug) Console.WriteLine($"  P:VEX2");
                        var byte1 = encoding[operandIndex + 1];
                        map = Map.Vex1;
                        if (Debug.debug) Console.WriteLine($"  map: Vex1");

                        if ((byte1 & Vex_ByteL) != 0)
                        {
                            flags |= EncodingFlags.L;
                            if (Debug.debug) Console.WriteLine($"  VEX.L");
                        }

                        operandIndex += 3;
                        break;
                    }
                case Prefixes.Evex:
                    {
                        const byte Evex_ByteW = 0x80; // in byte P1
                        const byte Evex_ByteLprimeLmask = 0x60; // in byte P2
                        const byte Evex_ByteLprimeLshift = 5;

                        if (Debug.debug) Console.WriteLine($"  P:EVEX");
                        var evex_p0 = encoding[operandIndex + 1];
                        var evex_p1 = encoding[operandIndex + 2];
                        var evex_p2 = encoding[operandIndex + 3];
                        var evex_mmm = evex_p0 & 0x7;
                        switch (evex_mmm)
                        {
                            case 0x1:
                                map = Map.Evex_0F;
                                if (Debug.debug) Console.WriteLine($"  map: Evex_0F");
                                break;
                            case 0x2:
                                map = Map.Evex_0F38;
                                if (Debug.debug) Console.WriteLine($"  map: Evex_0F38");
                                break;
                            case 0x3:
                                map = Map.Evex_0F3A;
                                if (Debug.debug) Console.WriteLine($"  map: Evex_0F3A");
                                break;
                            default:
                                throw new Exception($"Unexpected EVEX map {encoding}");
                        }

                        if ((evex_p1 & Evex_ByteW) != 0)
                        {
                            flags |= EncodingFlags.W;
                            if (Debug.debug) Console.WriteLine($"  EVEX.W");
                        }

                        byte evex_LprimeL = (byte)((evex_p2 & Evex_ByteLprimeLmask) >> Evex_ByteLprimeLshift);
                        flags |= Util.ConvertEvexLLToEncodingFlags(evex_LprimeL);
                        if (Debug.debug)
                        {
                            Console.WriteLine($"  EVEX.L'L={evex_LprimeL:x1}");
                        }

                        var evex_b = evex_p2 & 0x10;
                        if (evex_b != 0)
                        {
                            flags |= EncodingFlags.b;
                            if (Debug.debug) Console.WriteLine($"  EVEX.b");
                        }

                        operandIndex += 4;
                        break;
                    }
                default:
                    map = Map.Primary;
                    if (Debug.debug) Console.WriteLine($"  map: primary");
                    break;
            }

            return (map, operandIndex, flags);
        }

        public SuffixFlags parseSuffix()
        {
            if (suffixBytes == 0)
                return SuffixFlags.None;

            byte modrm = encoding[opIndex + 1];

            int mod = modrm >> 6;
            int rm = modrm & 0x7;

            SuffixFlags flags = 0;

            if (mod == 0x3)
                return SuffixFlags.Unknown;

            int accounted = 0;
            for (int i = 0; i < operands.Count; i++)
            {
                string operand = operands[i];
                bool memop = EncOperandIsMemOp().IsMatch(operand);

                if (memop)
                {
                    // Note: we don't handle VSIB since instructions using it don't have interesting memory operands.

                    bool hasSIB = (rm == 0x4);

                    accounted += hasSIB ? 6 : 5;

                    if (EncOperandIsMOp().IsMatch(operand))
                    {
                        if (i == 0)
                        {
                            flags |= SuffixFlags.M1st;
                            if (operands.Count == 1)
                                flags |= SuffixFlags.MOnly;
                        }
                        flags |= SuffixFlags.MOp;

                        flags |= memOpSize?[operand] ?? SuffixFlags.MUnknown;

                        if (flags.HasFlag(SuffixFlags.MUnknown))
                        {
                            if (unknownMemOps.TryGetValue(mnemonic, out var value))
                            {
                                flags |= value(encodingFlags);
                                flags ^= SuffixFlags.MUnknown;
                            }
                        }
                    }
                    break;
                }
            }

            switch (suffixBytes - accounted)
            {
                case 8: if (accounted > 0) goto default; flags |= SuffixFlags.I8B; break;
                case 4: flags |= SuffixFlags.I4B; break;
                case 3: flags |= SuffixFlags.I3B; break;
                case 2: flags |= SuffixFlags.I2B; break;
                case 1: flags |= SuffixFlags.I1B; break;
                case 0: break;
                default:
                    if (suffixBytes < accounted)
                    {
                        throw new Exception($"Encoding too short m:{map} o:{opIndex} s:{suffixBytes} a:{accounted}? : {disassembly}");
                    }
                    else if (suffixBytes - accounted > 8)
                    {
                        throw new Exception($"Encoding too long m:{map} o:{opIndex} s:{suffixBytes} a:{accounted}? : {disassembly}");
                    }
                    else
                    {
                        throw new Exception($"Encoding Immediate too long m:{map} o:{opIndex} s:{suffixBytes} a:{accounted}? : {disassembly}");
                    }
            }

            return flags;
        }
    } // end class Amd64InstructionSample


    internal sealed partial class Amd64InstructionTableGenerator
    {
        private List<Amd64InstructionSample> samples = new List<Amd64InstructionSample>();

        private const string assemblyPrefix = "   0x000000000";
        private const string preTerminator = "58\t";
        private const string groupTerminator = "59\tpop";

        [GeneratedRegex(@"((\{vex\})|(\{bad\})|(\(bad\))|(\srex(\.[WRXB]*)?\s*(#.*)?$))")]
        private static partial Regex BadDisassembly();

        private List<(Map, int)> regExpandOpcodes;

        // C++ Code generation
        private HashSet<string> rules;
        private Dictionary<Map, Dictionary<int, string>> opcodes;
        private int currentExtension = -8;

        private Amd64InstructionTableGenerator()
        {
            // This is a set of (map, opcode) where the ModRM.reg contributes to the opcode for determining
            // whether two opcodes are unique.
            regExpandOpcodes = new List<(Map, int)>()
            {
                // Code assumes ordered list
                (Map.Primary, 0xd9),
                (Map.Primary, 0xdb),
                (Map.Primary, 0xdd),
                (Map.Primary, 0xdf),
                (Map.Primary, 0xf6),
                (Map.Primary, 0xf7),
                (Map.Primary, 0xff),
                (Map.Secondary, 0x01),
                (Map.Secondary, 0x18), // prefetch / Reserved-NOP
                (Map.Secondary, 0x1c), // cldemote / Reserved-NOP
                (Map.Secondary, 0xae),
                (Map.Secondary, 0xc7),
            };

            rules = new HashSet<string>();
            opcodes = new Dictionary<Map, Dictionary<int, string>>()
            {
                { Map.None,      new Dictionary<int, string>() },
                { Map.Primary,   new Dictionary<int, string>() },
                { Map.Secondary, new Dictionary<int, string>() },
                { Map.F38,       new Dictionary<int, string>() },
                { Map.F3A,       new Dictionary<int, string>() },
                { Map.Vex1,      new Dictionary<int, string>() },
                { Map.Vex2,      new Dictionary<int, string>() },
                { Map.Vex3,      new Dictionary<int, string>() },
                { Map.Evex_0F,   new Dictionary<int, string>() },
                { Map.Evex_0F38, new Dictionary<int, string>() },
                { Map.Evex_0F3A, new Dictionary<int, string>() },
            };

            ParseSamples();
            WriteCode();
        }

        private void ParseSamples()
        {
            string line;
            string sample = null;
            bool saw58 = false;
            while ((line = Console.In.ReadLine()) != null)
            {
                //if (Debug.debug) Console.WriteLine($"line: {line}");

                if (sample == null)
                {
                    // Ignore non-assembly lines
                    if (line.StartsWith(assemblyPrefix))
                        sample = line.Trim();
                    continue;
                }

                //if (Debug.debug) Console.WriteLine($"sample: {sample}");

                // Each sample may contain multiple instructions
                // We are only interested in the first of each group
                // Each group is terminated by 0x58 then 0x59 which is a pop instruction
                if (!saw58)
                {
                    saw58 = line.Contains(preTerminator);
                    continue;
                }
                else if (!line.Contains(groupTerminator))
                {
                    saw58 = false;
                    continue;
                }

                if (!BadDisassembly().IsMatch(sample))
                {
                    try
                    {
                        // We expect samples to be disassembled instruction in intel disassembly syntax
                        // Roughly like this:
                        //    0x0000000000713cd0 <+1125488>:    c4 01 02 7f 05 50 51 52 53  vmovdqu XMMWORD PTR [rip+0x53525150],xmm8        # 0x53c38e29
                        var s = new Amd64InstructionSample(sample);

                        SuffixFlags suffix = s.parseSuffix();
                        AddSample(s);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception:{e.Message}");
                    }
                }
                else
                {
                    if (Debug.debug)
                    {
                        Console.WriteLine($"{sample} - bad disassembly");
                    }
                }

                saw58 = false;
                sample = null;
            }
        }

        private void AddSample(Amd64InstructionSample sample)
        {
            if (samples.Count > 0)
            {
                bool regEnc = (regExpandOpcodes.Count > 0) && ((samples[0].map, samples[0].opCodeExt >> 4) == regExpandOpcodes[0]);

                if ((sample.opCodeExt != samples[0].opCodeExt) || (regEnc && (sample.modrm_reg != samples[0].modrm_reg)))
                {
                    SummarizeSamples(regEnc);
                    if (regEnc && ((sample.opCodeExt >> 4) != (samples[0].opCodeExt >> 4)))
                    {
                        if (Debug.debug) Console.WriteLine($"Removing {regExpandOpcodes[0]}");

                        regExpandOpcodes.RemoveAt(0);
                    }
                    samples.Clear();
                }
            }
            samples.Add(sample);
        }

        private void SummarizeSamples(bool reg)
        {
            if (Debug.debug) Console.WriteLine($"SummarizeSamples opcodeExt={samples[0].opCodeExt:x3}, useModRmReg={reg}");

            var sample = samples[0];
            SuffixFlags intersectionSuffix = (SuffixFlags)~0;
            SuffixFlags unionSuffix = 0;
            var map = new Dictionary<SuffixFlags, List<Amd64InstructionSample>>();
            HashSet<string> mnemonics = new HashSet<string>();
            foreach (var s in samples)
            {
                SuffixFlags suffix = s.parseSuffix();

                if (Debug.debug) Console.WriteLine($"{s.disassembly} => {suffix} ({s.encodingFlags})");

                map.TryAdd(suffix, new List<Amd64InstructionSample>() { });
                map[suffix].Add(s);

                mnemonics.Add(s.mnemonic);

                intersectionSuffix &= suffix;
                unionSuffix |= suffix;
            }

            if (Debug.debug)
            {
                Console.WriteLine($"SuffixFlags => samples map:");
                foreach ((SuffixFlags e, List<Amd64InstructionSample> l) in map)
                {
                    Console.WriteLine($"  {e}");
                    foreach (var s in l)
                    {
                        Console.WriteLine($"    {s.disassembly} ({s.encodingFlags})");
                    }
                }
            }

            string rules = Enum.Format(typeof(SuffixFlags), intersectionSuffix, "F").Replace(", ", "_");
            rules = rules.Replace("None", "^");

            if (Debug.debug) Console.WriteLine($"  rules-intersection:{rules}");

            SuffixFlags sometimesSuffix = unionSuffix & ~intersectionSuffix;
            if (Debug.debug) Console.WriteLine($"  rules-sometimes:{sometimesSuffix}");

            switch (sometimesSuffix)
            {
                case SuffixFlags.None:
                    break;
                case SuffixFlags.M32B | SuffixFlags.M16B:
                    if (TestHypothesis((e) => Amd64L(SuffixFlags.M32B, SuffixFlags.M16B, e), sometimesSuffix, map))
                        rules += "_L_M32B_or_M16B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M32B | SuffixFlags.M8B:
                    if (TestHypothesis((e) => Amd64L(SuffixFlags.M32B, SuffixFlags.M8B, e), sometimesSuffix, map))
                        rules += "_L_M32B_or_M8B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M16B | SuffixFlags.M8B:
                    if (TestHypothesis((e) => Amd64L(SuffixFlags.M16B, SuffixFlags.M8B, e), sometimesSuffix, map))
                        rules += "_L_M16B_or_M8B";
                    else if (TestHypothesis((e) => Amd64W(SuffixFlags.M16B, SuffixFlags.M8B, e), sometimesSuffix, map))
                        rules += "_W_M16B_or_M8B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M16B | SuffixFlags.MOp:
                    if (TestHypothesis((e) => Amd64W(SuffixFlags.None, SuffixFlags.M16B | SuffixFlags.MOp, e), sometimesSuffix, map))
                        rules += "_W_None_or_MOp_M16B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M8B | SuffixFlags.M4B:
                    if (TestHypothesis((e) => Amd64W(SuffixFlags.M8B, SuffixFlags.M4B, e), sometimesSuffix, map))
                        rules += "_W_M8B_or_M4B";
                    else if (TestHypothesis((e) => Amd64L(SuffixFlags.M8B, SuffixFlags.M4B, e), sometimesSuffix, map))
                        rules += "_L_M8B_or_M4B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M8B | SuffixFlags.M4B | SuffixFlags.M2B:
                    if (TestHypothesis((e) => Amd64WP(SuffixFlags.M8B, SuffixFlags.M4B, SuffixFlags.M2B, e), sometimesSuffix, map))
                        rules += "_WP_M8B_or_M4B_or_M2B";
                    else if (TestHypothesis((e) => TestLL(SuffixFlags.M2B, SuffixFlags.M4B, SuffixFlags.M8B, e), sometimesSuffix, map))
                        rules += "_LL_M2B_M4B_M8B"; // e.g., vpmovusqb
                    else
                        goto default;
                    break;
                case SuffixFlags.M8B | SuffixFlags.M2B:
                    if (TestHypothesis((e) => Amd64W(SuffixFlags.M8B, SuffixFlags.M2B, e), sometimesSuffix, map))
                        rules += "_W_M8B_or_M2B";
                    else if (TestHypothesis((e) => Amd64WP(SuffixFlags.M8B, SuffixFlags.M8B, SuffixFlags.M2B, e), sometimesSuffix, map))
                        rules += "_WP_M8B_or_M8B_or_M2B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M6B | SuffixFlags.M4B:
                    if (TestHypothesis((e) => Amd64P(SuffixFlags.M6B, SuffixFlags.M4B, e), sometimesSuffix, map))
                        rules += "_P_M6B_or_M4B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M4B | SuffixFlags.M2B:
                    if (TestHypothesis((e) => Amd64L(SuffixFlags.M4B, SuffixFlags.M2B, e), sometimesSuffix, map))
                        rules += "_L_M4B_or_M2B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M4B | SuffixFlags.M1B:
                    if (TestHypothesis((e) => Amd64W(SuffixFlags.M4B, SuffixFlags.M1B, e), sometimesSuffix, map))
                        rules += "_W_M4B_or_M1B";
                    else
                        goto default;
                    break;
                case SuffixFlags.I8B | SuffixFlags.I4B | SuffixFlags.I2B:
                    if (TestHypothesis((e) => Amd64WP(SuffixFlags.I8B, SuffixFlags.I4B, SuffixFlags.I2B, e), sometimesSuffix, map))
                        rules += "_WP_I8B_or_I4B_or_I2B";
                    else
                        goto default;
                    break;
                case SuffixFlags.I4B | SuffixFlags.I2B:
                    if (TestHypothesis((e) => Amd64WP(SuffixFlags.I4B, SuffixFlags.I4B, SuffixFlags.I2B, e), sometimesSuffix, map))
                        rules += "_WP_I4B_or_I4B_or_I2B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M8B | SuffixFlags.M4B | SuffixFlags.M2B | SuffixFlags.I4B | SuffixFlags.I2B:
                    if (TestHypothesis((e) => Amd64WP(SuffixFlags.M8B | SuffixFlags.I4B, SuffixFlags.M4B | SuffixFlags.I4B, SuffixFlags.M2B | SuffixFlags.I2B, e), sometimesSuffix, map))
                        rules += "_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B";
                    else
                        goto default;
                    break;
                case SuffixFlags.M16B | SuffixFlags.M32B | SuffixFlags.M64B:
                    if (TestHypothesis((e) => TestLL(SuffixFlags.M16B, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_LL_M16B_M32B_M64B"; // e.g., vmovups, vmovupd, vmovsldup
                    else
                        goto default;
                    break;
                case SuffixFlags.M8B | SuffixFlags.M32B | SuffixFlags.M64B:
                    if (TestHypothesis((e) => TestLL(SuffixFlags.M8B, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_LL_M8B_M32B_M64B"; // e.g., vmovddup, vcvtps2pd
                    else if (TestHypothesis((e) => Amd64bLL(SuffixFlags.M8B, SuffixFlags.None, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_bLL_M8B_None_M32B_M64B"; // vpermq
                    else
                        goto default;
                    break;
                case SuffixFlags.M8B | SuffixFlags.M16B | SuffixFlags.M32B:
                    if (TestHypothesis((e) => TestLL(SuffixFlags.M8B, SuffixFlags.M16B, SuffixFlags.M32B, e), sometimesSuffix, map))
                        rules += "_LL_M8B_M16B_M32B"; // e.g., vcvtps2pd, vpmovuswb
                    else
                        goto default;
                    break;
                case SuffixFlags.M4B | SuffixFlags.M8B | SuffixFlags.M16B:
                    if (TestHypothesis((e) => TestLL(SuffixFlags.M4B, SuffixFlags.M8B, SuffixFlags.M16B, e), sometimesSuffix, map))
                        rules += "_LL_M4B_M8B_M16B"; // e.g., vpmovusdb
                    else
                        goto default;
                    break;
                case SuffixFlags.M32B | SuffixFlags.M64B:
                    if (TestHypothesis((e) => TestLL(SuffixFlags.None, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_LL_None_M32B_M64B"; // e.g., vpermpd, vpermps (no EVEX.128 (LL00) form)
                    else
                        goto default;
                    break;
                case SuffixFlags.M8B | SuffixFlags.M16B | SuffixFlags.M32B | SuffixFlags.M64B:
                    if (TestHypothesis((e) => Amd64WLL(SuffixFlags.M16B, SuffixFlags.M32B, SuffixFlags.M64B, SuffixFlags.M8B, SuffixFlags.M16B, SuffixFlags.M32B, e), sometimesSuffix, map))
                        rules += "_WLL_M16B_M32B_M64B_or_M8B_M16B_M32B"; // e.g., vcvttps2uqq, vcvttpd2uqq
                    else if (TestHypothesis((e) => Amd64bLL(SuffixFlags.M8B, SuffixFlags.M16B, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_bLL_M8B_M16B_M32B_M64B"; // e.g., vunpcklpd, vunpckhpd, vmovapd
                    else
                        goto default;
                    break;
                case SuffixFlags.M2B | SuffixFlags.M16B | SuffixFlags.M32B | SuffixFlags.M64B:
                    if (TestHypothesis((e) => Amd64bLL(SuffixFlags.M2B, SuffixFlags.M16B, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_bLL_M2B_M16B_M32B_M64B"; // e.g., vrndscaleph
                    else
                        goto default;
                    break;
                case SuffixFlags.M4B | SuffixFlags.M16B | SuffixFlags.M32B | SuffixFlags.M64B:
                    if (TestHypothesis((e) => Amd64bLL(SuffixFlags.M4B, SuffixFlags.M16B, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_bLL_M4B_M16B_M32B_M64B"; // e.g., vunpcklps, vunpckhps, vmovaps
                    else
                        goto default;
                    break;
                case SuffixFlags.M4B | SuffixFlags.M8B | SuffixFlags.M16B | SuffixFlags.M32B:
                    if (TestHypothesis((e) => Amd64bLL(SuffixFlags.M4B, SuffixFlags.M8B, SuffixFlags.M16B, SuffixFlags.M32B, e), sometimesSuffix, map))
                        rules += "_bLL_M4B_M8B_M16B_M32B"; // e.g., vcvtps2pd
                    else
                        goto default;
                    break;
                case SuffixFlags.M4B | SuffixFlags.M8B | SuffixFlags.M32B | SuffixFlags.M64B:
                    if (TestHypothesis((e) => Amd64bWLL(SuffixFlags.M4B, SuffixFlags.M8B, SuffixFlags.None, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_bWLL_M4B_M8B_None_M32B_M64B"; // e.g., vpermpd, vpermps (no EVEX.128 (LL00) form)
                    else
                        goto default;
                    break;
                case SuffixFlags.M4B | SuffixFlags.M8B | SuffixFlags.M16B | SuffixFlags.M32B | SuffixFlags.M64B:
                    // Does EVEX.b only affect broadcast size, with EVEX.W, but L'L vector size for non-broadcast is all the same?
                    if (TestHypothesis((e) => Amd64bWLL(SuffixFlags.M4B, SuffixFlags.M8B, SuffixFlags.M16B, SuffixFlags.M32B, SuffixFlags.M64B, e), sometimesSuffix, map))
                        rules += "_bWLL_M4B_M8B_M16B_M32B_M64B"; // e.g., vcvtdq2ps, vcvtqq2ps
                    // Does EVEX.W affect all L'L sizes, including broadcast size?
                    else if (TestHypothesis((e) => Amd64WbLL(SuffixFlags.M8B, SuffixFlags.M16B, SuffixFlags.M32B, SuffixFlags.M64B, SuffixFlags.M4B, SuffixFlags.M8B, SuffixFlags.M16B, SuffixFlags.M32B, e), sometimesSuffix, map))
                        rules += "_WbLL_M8B_M16B_M32B_M64B_or_M4B_M8B_M16B_M32B"; // e.g., vcvttps2uqq, vcvttpd2uqq
                    else
                        goto default;
                    break;
                default:
                    if (Debug.debug)
                    {
                        Console.WriteLine($"Unhandled rule...{sometimesSuffix}");
                    }
                    Console.Error.WriteLine($"Unhandled rule...{sometimesSuffix}");
                    return;
            }

            rules = rules.Replace("^_", "").Replace("^", "None");

            if (Debug.debug) Console.WriteLine($"  rules:{rules}");

            AddOpCode(sample.map, sample.opCodeExt, reg, sample.modrm_reg, rules, mnemonics);

            if (Debug.debug)
            {
                string op = reg ? $"OpReg(0x{sample.opCodeExt:x3}, 0x{sample.modrm_reg})" : $"Op(0x{sample.opCodeExt:x3})";
                Console.WriteLine($"Amd64Op({sample.map}, {op}, {sample.mnemonic}, {rules})");
            }
        }

        private static bool TestHypothesis(Func<EncodingFlags, SuffixFlags> hypothesis, SuffixFlags sometimesSuffixFlags, Dictionary<SuffixFlags, List<Amd64InstructionSample>> suffixMap)
        {
            foreach ((SuffixFlags suffixFlags, List<Amd64InstructionSample> samples) in suffixMap)
            {
                foreach (var sample in samples)
                {
                    if (hypothesis(sample.encodingFlags) != (suffixFlags & sometimesSuffixFlags))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static SuffixFlags Test(EncodingFlags e, SuffixFlags t, SuffixFlags f, EncodingFlags g) => g.HasFlag(e) ? t : f;
        public static SuffixFlags TestLL(SuffixFlags LL00, SuffixFlags LL01, SuffixFlags LL10, EncodingFlags g)
        {
            if (!Util.EncodingFlagsHasEvexLL(g))
                return SuffixFlags.None;

            int LL = Util.ConvertEncodingFlagsToEvexLL(g);
            if (LL == 0)
                return LL00;
            else if (LL == 1)
                return LL01;
            else
                return LL10;
        }

        // Tests for a single flag

        public static SuffixFlags Amd64L(SuffixFlags t, SuffixFlags f, EncodingFlags g) => Test(EncodingFlags.L, t, f, g);
        public static SuffixFlags Amd64W(SuffixFlags W1, SuffixFlags W0, EncodingFlags g) => Test(EncodingFlags.W, W1, W0, g);
        public static SuffixFlags Amd64P(SuffixFlags t, SuffixFlags f, EncodingFlags g) => Test(EncodingFlags.P, f, t, g);
        public static SuffixFlags Amd64b(SuffixFlags b1, SuffixFlags b0, EncodingFlags g) => Test(EncodingFlags.b, b1, b0, g);

        // Tests for multiple flags

        public static SuffixFlags Amd64WP(SuffixFlags tx, SuffixFlags ft, SuffixFlags ff, EncodingFlags g) => Amd64W(tx, Amd64P(ft, ff, g), g);
        public static SuffixFlags Amd64WLL(SuffixFlags W1LL00, SuffixFlags W1LL01, SuffixFlags W1LL10, SuffixFlags W0LL00, SuffixFlags W0LL01, SuffixFlags W0LL10, EncodingFlags g) =>
            Amd64W(TestLL(W1LL00, W1LL01, W1LL10, g), TestLL(W0LL00, W0LL01, W0LL10, g), g);
        public static SuffixFlags Amd64bLL(SuffixFlags b1, SuffixFlags b0LL00, SuffixFlags b0LL01, SuffixFlags b0LL10, EncodingFlags g) =>
            Amd64b(b1, TestLL(b0LL00, b0LL01, b0LL10, g), g);
        public static SuffixFlags Amd64bWLL(SuffixFlags b1W0, SuffixFlags b1W1, SuffixFlags b0LL00, SuffixFlags b0LL01, SuffixFlags b0LL10, EncodingFlags g) =>
            Amd64b(Amd64W(b1W1, b1W0, g), TestLL(b0LL00, b0LL01, b0LL10, g), g);
        public static SuffixFlags Amd64WbLL(SuffixFlags W1b1, SuffixFlags W1b0LL00, SuffixFlags W1b0LL01, SuffixFlags W1b0LL10, SuffixFlags W0b1, SuffixFlags W0b0LL00, SuffixFlags W0b0LL01, SuffixFlags W0b0LL10, EncodingFlags g) =>
            Amd64W(Amd64b(W1b1, TestLL(W1b0LL00, W1b0LL01, W1b0LL10, g), g), Amd64b(W0b1, TestLL(W0b0LL00, W0b0LL01, W0b0LL10, g), g), g);

        private void AddOpCode(Map map, int opCodeExt, bool reg, int modrmReg, string rule, HashSet<string> mnemonics)
        {
            rules.Add(rule);

            if (reg)
            {
                if (!opcodes[map].ContainsKey(opCodeExt))
                {
                    currentExtension += 8;
                    string ext = $"InstrForm(int(Extension)|0x{(currentExtension >> 3):x2})";
                    opcodes[map][opCodeExt] = $"        {ext + ",",-40} // 0x{opCodeExt:x3}";
                }
                opcodes[Map.None][currentExtension | modrmReg] = $"        {rule + ",",-40} // {map}:0x{opCodeExt:x3}/{modrmReg} {string.Join(",", mnemonics.OrderBy(s => s))}";
            }
            else
            {
                string oldstring = null;
                if (Debug.debug)
                {
                    if (opcodes[map].TryGetValue(opCodeExt, out oldstring))
                    {
                        Console.WriteLine($"WARNING: REPLACING opcodes[{map}][{opCodeExt:x3}] = {oldstring}");
                    }
                }
                opcodes[map][opCodeExt] = $"        {rule + ",",-40} // 0x{opCodeExt:x3} {string.Join(",", mnemonics.OrderBy(s => s))}";
                if (Debug.debug)
                {
                    Console.WriteLine($"add opcodes[{map}][{opCodeExt:x3}] = {opcodes[map][opCodeExt]}");
                    if ((oldstring != null) && (oldstring != opcodes[map][opCodeExt]))
                    {
                        Console.WriteLine($"WARNING: REPLACEMENT WAS DIFFERENT");
                    }
                }
            }
        }

        private void WriteCode()
        {
            string none = "None";

            Console.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
            Console.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("// File machine generated. See gen_amd64InstrDecode/README.md");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("namespace Amd64InstrDecode");
            Console.WriteLine("{");
            Console.WriteLine("    // The enumeration below encodes the various amd64 instruction forms");
            Console.WriteLine("    // Each enumeration is an '_' separated set of flags");
            Console.WriteLine("    //      None     // No flags set");
            Console.WriteLine("    //      MOp      // Instruction supports modrm RIP memory operations");
            Console.WriteLine("    //      M1st     // Memory op is first operand normally src/dst");
            Console.WriteLine("    //      MOnly    // Memory op is only operand.  May not be a write...");
            Console.WriteLine("    //      MUnknown // Memory op size is unknown.  Size not included in disassembly");
            Console.WriteLine("    //      MAddr    // Memory op is address load effective address");
            Console.WriteLine("    //      M1B      // Memory op is 1  byte");
            Console.WriteLine("    //      M2B      // Memory op is 2  bytes");
            Console.WriteLine("    //      M4B      // Memory op is 4  bytes");
            Console.WriteLine("    //      M8B      // Memory op is 8  bytes");
            Console.WriteLine("    //      M16B     // Memory op is 16 bytes");
            Console.WriteLine("    //      M32B     // Memory op is 32 bytes");
            Console.WriteLine("    //      M64B     // Memory op is 64 bytes");
            Console.WriteLine("    //      M6B      // Memory op is 6  bytes");
            Console.WriteLine("    //      M10B     // Memory op is 10 bytes");
            Console.WriteLine("    //      I1B      // Instruction includes 1  byte  of immediates");
            Console.WriteLine("    //      I2B      // Instruction includes 2  bytes of immediates");
            Console.WriteLine("    //      I3B      // Instruction includes 3  bytes of immediates");
            Console.WriteLine("    //      I4B      // Instruction includes 4  bytes of immediates");
            Console.WriteLine("    //      I8B      // Instruction includes 8  bytes of immediates");
            Console.WriteLine("    //      Unknown  // Instruction samples did not include a modrm configured to produce RIP addressing");
            Console.WriteLine("    //      L        // Flags depend on L bit in encoding.  L_<flagsLTrue>_or_<flagsLFalse>");
            Console.WriteLine("    //      LL       // Flags depend on L'L bits in EVEX encoding.  LL_<flagsLL00>_<flagsLL01>_<flagsLL10>");
            Console.WriteLine("    //                  LL00 = 128-bit vector; LL01 = 256-bit vector; LL10 = 512-bit vector");
            Console.WriteLine("    //      W        // Flags depend on W bit in encoding.  W_<flagsWTrue>_or_<flagsWFalse>");
            Console.WriteLine("    //      P        // Flags depend on OpSize prefix for encoding.  P_<flagsNoOpSizePrefix>_or_<flagsOpSizePrefix>");
            Console.WriteLine("    //      WP       // Flags depend on W bit in encoding and OpSize prefix.  WP_<flagsWTrue>_or_<flagsNoOpSizePrefix>_or_<flagsOpSizePrefix>");
            Console.WriteLine("    //      WLL      // Flags depend on W and L'L bits.");
            Console.WriteLine("    //               //     WLL_<W=1 LL=00>_<W=1 LL=01>_<W=1 LL=10>_or_<W=0 LL=00>_<W=0 LL=01>_<W=0 LL=10>");
            Console.WriteLine("    //      bLL      // Flags depend on EVEX.b and L'L bits.");
            Console.WriteLine("    //               //     bLL_<b=1>_<b=0 LL=00>_<b=0 LL=01>_<b=0 LL=10>");
            Console.WriteLine("    //      bWLL     // Flags depend on EVEX.b, EVEX.W, and L'L bits, but EVEX.W only affects the EVEX.b case, not the L'L case.");
            Console.WriteLine("    //               //     bWLL_<b=1 W=0>_<b=1 W=1>_<b=0 LL=00>_<b=0 LL=01>_<b=0 LL=10>");
            Console.WriteLine("    //      WbLL     // Flags depend on EVEX.W, EVEX.b, and L'L bits.");
            Console.WriteLine("    //               //     WbLL_<W=1 b=1>_<W=1 b=0 LL=00>_<W=1 b=0 LL=01>_<W=1 b=0 LL=10>_or_<W=0 b=1>_<W=0 b=0 LL=00>_<W=0 b=0 LL=01>_<W=0 b=0 LL=10>");
            Console.WriteLine("    //      or       // Flag option separator to help readability in some cases");
            Console.WriteLine("    enum InstrForm : uint8_t");
            Console.WriteLine("    {");
            Console.WriteLine($"       None,");
            foreach (string rule in rules.OrderBy(s => s))
            {
                if (rule == "None")
                    continue;
                Console.WriteLine($"       {rule},");
            }
            Console.WriteLine($"       Extension = 0x80, // The instruction encoding form depends on the modrm.reg field. Extension table location in encoded in lower bits");
            Console.WriteLine("    };");

            Console.WriteLine();
            Console.WriteLine("    // The following instrForm maps correspond to the amd64 instr maps");
            Console.WriteLine("    // The comments are for debugging convenience.  The comments use a packed opcode followed by a list of observed mnemonics");
            Console.WriteLine("    // The opcode is packed to be human readable.  PackedOpcode = opcode << 4 + pp");
            Console.WriteLine("    //   - For Vex* the pp is directly included in the encoding");
            Console.WriteLine("    //   - For the Secondary, F38, and F3A pages the pp is not defined in the encoding, but affects instr form.");
            Console.WriteLine("    //          - pp = 0 implies no prefix.");
            Console.WriteLine("    //          - pp = 1 implies 0x66 OpSize prefix only.");
            Console.WriteLine("    //          - pp = 2 implies 0xF3 prefix.");
            Console.WriteLine("    //          - pp = 3 implies 0xF2 prefix.");
            Console.WriteLine("    //   - For the primary map, pp is not used and is always 0 in the comments.");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("    // Instruction which change forms based on modrm.reg are encoded in this extension table.");
            Console.WriteLine("    // Since there are 8 modrm.reg values, they occur is groups of 8.");
            Console.WriteLine("    // Each group is referenced from the other tables below using Extension|(index >> 3).");
            currentExtension += 8;
            Console.WriteLine($"    static const InstrForm instrFormExtension[{currentExtension + 1}]");
            Console.WriteLine("    {");
            for (int i = 0; i < currentExtension; i++)
            {
                if (opcodes[Map.None].TryGetValue(i, out string value))
                    Console.WriteLine(value);
                else
                    Console.WriteLine($"        {none},");
            }
            Console.WriteLine("    };");

            Console.WriteLine();
            Console.WriteLine($"    static const InstrForm instrFormPrimary[256]");
            Console.WriteLine("    {");
            for (int i = 0; i < 4096; i += 16)
            {
                if (opcodes[Map.Primary].TryGetValue(i, out string value))
                    Console.WriteLine(value);
                else
                    Console.WriteLine($"        {none + ",",-40} // 0x{i:x3}");
            }
            Console.WriteLine("    };");

            var mapTuples = new List<(string, Map)>()
                {
                    ("Secondary", Map.Secondary),
                    ("F38", Map.F38),
                    ("F3A", Map.F3A),
                    ("Vex1", Map.Vex1),
                    ("Vex2", Map.Vex2),
                    ("Vex3", Map.Vex3),
                    ("Evex_0F", Map.Evex_0F),
                    ("Evex_0F38", Map.Evex_0F38),
                    ("Evex_0F3A", Map.Evex_0F3A)
                };

            foreach ((string name, Map map) in mapTuples)
            {
                Console.WriteLine();
                Console.WriteLine($"    static const InstrForm instrForm{name}[1024]");
                Console.WriteLine("    {");
                for (int i = 0; i < 4096; i += 16)
                {
                    for (int pp = 0; pp < 4; pp++)
                    {
                        if (opcodes[map].TryGetValue(i + pp, out string value))
                        {
                            Console.WriteLine(value);
                        }
                        else
                        {
                            Console.WriteLine($"        {none + ",",-40} // 0x{i + pp:x3}");
                        }
                    }
                }
                Console.WriteLine("    };");
            }

            Console.WriteLine("}");
        }

        private static void Main()
        {
            new Amd64InstructionTableGenerator();
        }
    }
}
