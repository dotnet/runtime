// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Amd64InstructionTableGenerator
{
    [Flags]
    public enum EncodingFlags : int
    {
        None = 0x0,
        P = 0x1, // OpSize (P)refix
        F2 = 0x2,
        F3 = 0x4,
        Rex = 0x8,

        W = 0x10,
        L = 0x100,
    }

    [Flags]
    internal enum SuffixFlags : int
    {
        None = 0x0, // No flags set
        MOp = 0x1, // Instruction supports modrm RIP memory operations
        M1st = 0x3, // Memory op is first operand normally src/dst
        MOnly = 0x7, // Memory op is only operand.  May not be a write...
        MUnknown = 0x8, // Memory op size is unknown.  Size not included in disassemby
        MAddr = 0x10, // Memory op is address load effective address
        M1B = 0x20, // Memory op is 1  byte
        M2B = 0x40, // Memory op is 2  bytes
        M4B = 0x80, // Memory op is 4  bytes
        M8B = 0x100, // Memory op is 8  bytes
        M16B = 0x200, // Memory op is 16 bytes
        M32B = 0x400, // Memory op is 32 bytes
        M6B = 0x800, // Memory op is 6  bytes
        M10B = 0x1000, // Memory op is 10 bytes
        I1B = 0x2000, // Instruction includes 1  byte  of immediates
        I2B = 0x4000, // Instruction includes 2  bytes of immediates
        I3B = 0x8000, // Instruction includes 3  bytes of immediates
        I4B = 0x10000, // Instruction includes 4  bytes of immediates
        I8B = 0x20000, // Instruction includes 8  bytes of immediates
        Unknown = 0x40000, // Instruction sample did not include a modrm configured to produce RIP addressing
    }

    internal enum Map
    {
        // Map
        None,
        Primary,
        Secondary,
        F38,
        F3A,
        NOW3D,
        Vex1,
        Vex2,
        Vex3,
        XOP8,
        XOP9,
        XOPA,
    }

    internal sealed class Amd64InstructionSample
    {
        private static readonly Regex encDisassemblySplit = new Regex(@"^\s*(?<address>0x[a-f0-9]+)\s[^:]*:\s*(?<encoding>[0-9a-f ]*)\t(?<prefixes>(((rex[.WRXB]*)|(rep[nez]*)|(data16)|(addr32)|(lock)|(bnd)|([cdefgs]s)) +)*)(?<mnemonic>\S+) *(?<operands>(\S[^#]*?)?)\s*(?<comment>#.*)?$",
               RegexOptions.ExplicitCapture);
        private static readonly Regex encOperandSplit = new Regex(@"^\s*,?\s*(?<op>[^\(,]*(\([^\)]*\))?)?(?<rest>.+$)?", RegexOptions.ExplicitCapture);
        private static readonly Regex encOperandIsMemOp = new Regex(@"\[.*\]$");
        private static readonly Regex encOperandIsMOp = new Regex(@"\[rip.*\]$");
        private static readonly HashSet<string> allOperands = new HashSet<string>();
        private static readonly Dictionary<string, SuffixFlags> memOpSize = new Dictionary<string, SuffixFlags>()
        {
            ["[rip+0x53525150]"] = SuffixFlags.MUnknown,
            ["BYTE PTR [rip+0x53525150]"] = SuffixFlags.M1B,
            ["WORD PTR [rip+0x53525150]"] = SuffixFlags.M2B,
            ["DWORD PTR [rip+0x53525150]"] = SuffixFlags.M4B,
            ["QWORD PTR [rip+0x53525150]"] = SuffixFlags.M8B,
            ["OWORD PTR [rip+0x53525150]"] = SuffixFlags.M16B,
            ["XMMWORD PTR [rip+0x53525150]"] = SuffixFlags.M16B,
            ["YMMWORD PTR [rip+0x53525150]"] = SuffixFlags.M32B,
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
            ["vprotb"] = _ => SuffixFlags.M16B,
            ["vprotd"] = _ => SuffixFlags.M16B,
            ["vprotq"] = _ => SuffixFlags.M16B,
            ["vprotw"] = _ => SuffixFlags.M16B,
            ["vpshab"] = _ => SuffixFlags.M16B,
            ["vpshad"] = _ => SuffixFlags.M16B,
            ["vpshaq"] = _ => SuffixFlags.M16B,
            ["vpshaw"] = _ => SuffixFlags.M16B,
            ["vpshlb"] = _ => SuffixFlags.M16B,
            ["vpshld"] = _ => SuffixFlags.M16B,
            ["vpshlq"] = _ => SuffixFlags.M16B,
            ["vpshlw"] = _ => SuffixFlags.M16B,
        };

        public readonly string disassembly;
        private readonly string address;
        private readonly List<byte> encoding;
        public readonly string mnemonic;
        private readonly List<string> operands;

        public readonly Map map;
        public readonly EncodingFlags encodingFlags;
        private readonly byte opIndex;

        public int opCodeExt
        {
            get
            {
                const byte BytePP = 0x3;
                switch (map)
                {
                    case Map.Primary:
                        return encoding[opIndex] << 4;
                    case Map.Secondary:
                    case Map.F38:
                    case Map.F3A:
                        return (((int)encoding[opIndex]) << 4) +
                                (encodingFlags.HasFlag(EncodingFlags.F2) ? 0x3 :
                                    (encodingFlags.HasFlag(EncodingFlags.P) ? 0x1 :
                                        (encodingFlags.HasFlag(EncodingFlags.F3) ? 0x2 : 0)));
                    case Map.NOW3D:
                        return encoding[opIndex + 6] << 4;
                    case Map.Vex1:
                    case Map.Vex2:
                    case Map.Vex3:
                    case Map.XOP8:
                    case Map.XOP9:
                    case Map.XOPA:
                        return (((int)encoding[opIndex]) << 4) + (encoding[opIndex - 1] & BytePP);
                    default:
                        return 0;
                };
            }
        }

        private int suffixBytes { get { return encoding.Count - opIndex - 1; } }
        public int modrm { get { return (suffixBytes > 0) ? encoding[opIndex + 1] : 0; } }
        public int modrm_reg { get { return (modrm >> 3) & 0x7; } }

        public static HashSet<string> AllOperands { get { return allOperands; } }

        public Amd64InstructionSample(string disassembly_)
        {
            disassembly = disassembly_;

            var match = encDisassemblySplit.Match(disassembly);

            if (match == null)
                throw new ArgumentException($"Unable to parse disassembly: {disassembly}");

            // foreach (Group g in match.Groups)
            // {
            //     Console.WriteLine($"{g.Name}:'{g.ToString()}");
            // }

            address = match.Groups["address"].ToString();
            encoding = parseEncoding(match.Groups["encoding"].ToString());

            mnemonic = match.Groups["mnemonic"].ToString();

            if (mnemonic.Length == 0)
                throw new ArgumentException($"Missing mnemonic: {disassembly}");

            operands = parseOperands(match.Groups["operands"].ToString());

            (map, opIndex, encodingFlags) = parsePrefix(encoding);
        }

        private static List<byte> parseEncoding(string encodingDisassembly)
        {
            var encoding = new List<byte>();
            foreach (var b in encodingDisassembly.Split(' '))
            {
                // Console.WriteLine(b);
                encoding.Add(byte.Parse(b, NumberStyles.HexNumber));
            }
            return encoding;
        }

        private static List<string> parseOperands(string operandDisassemby)
        {
            var operands = new List<string>();
            string rest = operandDisassemby;

            while (rest?.Length != 0)
            {
                var opMatch = encOperandSplit.Match(rest);

                if (opMatch != null)
                {
                    string op = opMatch.Groups["op"].ToString();
                    operands.Add(op);
                    allOperands.Add(op);
                    rest = opMatch.Groups["rest"].ToString();
                }
                else
                {
                    throw new Exception($"Op parsing failed {operandDisassemby}");
                }
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
            FS = 0x64,
            GS = 0x65,
            OpSize = 0x66,
            AddSize = 0x67,
            Xop = 0x8f,
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
            const byte ByteL = 0x04;
            const byte ByteW = 0x80;

            while (!done)
            {
                switch ((Prefixes)encoding[operandIndex])
                {
                    case Prefixes.OpSize:
                        flags |= EncodingFlags.P;
                        operandIndex++;
                        break;
                    case Prefixes.Rep:
                        flags |= EncodingFlags.F2;
                        operandIndex++;
                        break;
                    case Prefixes.Repne:
                        flags |= EncodingFlags.F3;
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
                        operandIndex++;
                        break;
                    default:
                        done = true;
                        break;
                }
            }

            // Handle Rex prefix
            if ((encoding[operandIndex] & RexMask) == (byte)Prefixes.Rex)
            {
                byte rex = encoding[operandIndex++];

                flags |= EncodingFlags.Rex;

                if ((rex & RexW) != 0)
                    flags |= EncodingFlags.W;
            }

            switch ((Prefixes)encoding[operandIndex])
            {
                case Prefixes.Secondary:
                    switch ((Prefixes)encoding[operandIndex + 1])
                    {
                        case Prefixes.Secondary:
                            map = Map.NOW3D;
                            operandIndex += 1;
                            break;
                        case Prefixes.F38:
                            map = Map.F38;
                            operandIndex += 2;
                            break;
                        case Prefixes.F3A:
                            map = Map.F3A;
                            operandIndex += 2;
                            break;
                        default:
                            map = Map.Secondary;
                            operandIndex += 1;
                            break;
                    }
                    break;
                case Prefixes.Vex:
                case Prefixes.Xop:
                    {
                        var byte2 = encoding[operandIndex + 2];
                        if ((Prefixes)encoding[operandIndex] == Prefixes.Vex)
                        {
                            switch (encoding[operandIndex + 1] & 0x1f)
                            {
                                case 0x1:
                                    map = Map.Vex1;
                                    break;
                                case 0x2:
                                    map = Map.Vex2;
                                    break;
                                case 0x3:
                                    map = Map.Vex3;
                                    break;
                                default:
                                    throw new Exception($"Unexpected VEX map {encoding.ToString()}");
                            }
                        }
                        else
                        {
                            switch (encoding[operandIndex + 1] & 0x1f)
                            {
                                case 0x0:
                                case 0x1:
                                case 0x2:
                                case 0x3:
                                case 0x4:
                                case 0x5:
                                case 0x6:
                                case 0x7:
                                    map = Map.Primary;
                                    break;
                                case 0x8:
                                    map = Map.XOP8;
                                    break;
                                case 0x9:
                                    map = Map.XOP9;
                                    break;
                                case 0xA:
                                    map = Map.XOPA;
                                    break;
                                default:
                                    {
                                        string encodingString = new string("");

                                        foreach (var b in encoding)
                                        {
                                            encodingString += $"{b:x} ";
                                        }

                                        throw new Exception($"Unexpected XOP map \noperandIndex:{operandIndex}\nflags:{flags}\nencoding:{encodingString}");
                                    }
                            }
                            if (map == Map.Primary)
                                goto default;
                        }

                        if ((byte2 & ByteW) != 0)
                            flags |= EncodingFlags.W;
                        if ((byte2 & ByteL) != 0)
                            flags |= EncodingFlags.L;

                        operandIndex += 3;
                        break;
                    }
                case Prefixes.VexShort:
                    {
                        var byte1 = encoding[operandIndex + 1];
                        map = Map.Vex1;

                        if ((byte1 & ByteL) != 0)
                            flags |= EncodingFlags.L;

                        operandIndex += 3;
                        break;
                    }
                default:
                    map = Map.Primary;
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
                bool memop = encOperandIsMemOp.IsMatch(operand);

                if (encOperandIsMemOp.IsMatch(operand))
                {
                    bool hasSIB = (rm == 0x4);

                    accounted += hasSIB ? 6 : 5;

                    if (encOperandIsMOp.IsMatch(operand))
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
                            if (unknownMemOps.ContainsKey(mnemonic))
                            {
                                flags |= unknownMemOps[mnemonic](encodingFlags);
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
                        throw new Exception($"Encoding too short m:{map} o:{opIndex} s:{suffixBytes} a:{accounted}?? : {disassembly} ");
                    }
                    else if (suffixBytes - accounted > 8)
                    {
                        throw new Exception($"Encoding too long m:{map} o:{opIndex} s:{suffixBytes} a:{accounted}???? : {disassembly}");
                    }
                    else
                    {
                        throw new Exception($"Encoding Immediate too long m:{map} o:{opIndex} s:{suffixBytes} a:{accounted}???? : {disassembly}");
                    }
            }

            return flags;
        }
    }


    internal sealed class Amd64InstructionTableGenerator
    {
        private List<Amd64InstructionSample> samples = new List<Amd64InstructionSample>();

        private const string assemblyPrefix = "   0x000000000";
        private const string preTerminator = "58\t";
        private const string groupTerminator = "59\tpop";
        private static readonly Regex badDisassembly = new Regex(@"((\(bad\))|(\srex(\.[WRXB]*)?\s*(#.*)?$))");
        private List<(Map, int)> regExpandOpcodes;

        // C++ Code generation
        private HashSet<string> rules;
        private Dictionary<Map, Dictionary<int, string>> opcodes;
        private int currentExtension = -8;

        private Amd64InstructionTableGenerator()
        {
            regExpandOpcodes = new List<(Map, int)>()
            {
                // Code assunes ordered list
                (Map.Primary, 0xd9),
                (Map.Primary, 0xdb),
                (Map.Primary, 0xdd),
                (Map.Primary, 0xdf),
                (Map.Primary, 0xf6),
                (Map.Primary, 0xf7),
                (Map.Primary, 0xff),
                (Map.Secondary, 0x01),
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
                { Map.NOW3D,     new Dictionary<int, string>() },
                { Map.Vex1,      new Dictionary<int, string>() },
                { Map.Vex2,      new Dictionary<int, string>() },
                { Map.Vex3,      new Dictionary<int, string>() },
                { Map.XOP8,      new Dictionary<int, string>() },
                { Map.XOP9,      new Dictionary<int, string>() },
                { Map.XOPA,      new Dictionary<int, string>() },
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
                if (sample == null)
                {
                    // Ignore non-assembly lines
                    if (line.StartsWith(assemblyPrefix))
                        sample = line.Trim();
                    continue;
                }

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

                if (!badDisassembly.IsMatch(sample))
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

                saw58 = false;
                sample = null;

                continue;
            };
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
                        // Console.WriteLine($"Removing {regExpandOpcodes[0]}");
                        regExpandOpcodes.RemoveAt(0);
                    }
                    samples.Clear();
                }
            }
            samples.Add(sample);
        }

        private void SummarizeSamples(bool reg)
        {
            var sample = samples[0];
            SuffixFlags intersectionSuffix = (SuffixFlags)~0;
            SuffixFlags unionSuffix = 0;
            var map = new Dictionary<SuffixFlags, List<Amd64InstructionSample>>();
            HashSet<string> mnemonics = new HashSet<string>();
            foreach (var s in samples)
            {
                SuffixFlags suffix = s.parseSuffix();

                if (!map.ContainsKey(suffix))
                {
                    map[suffix] = new List<Amd64InstructionSample>() { };
                }
                map[suffix].Add(s);

                mnemonics.Add(s.mnemonic);

                intersectionSuffix &= suffix;
                unionSuffix |= suffix;
            }
            string rules = Enum.Format(typeof(SuffixFlags), intersectionSuffix, "F").Replace(", ", "_");

            rules = rules.Replace("None", "^");

            SuffixFlags sometimesSuffix = unionSuffix & ~intersectionSuffix;
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
                default:
                    throw new Exception($"Unhandled rule...{sometimesSuffix}");
            }
            rules = rules.Replace("^_", "").Replace("^", "None");

            AddOpCode(sample.map, sample.opCodeExt, reg, sample.modrm_reg, rules, mnemonics);
            // string op = reg ? $"OpReg(0x{sample.opCode:x3}, 0x{sample.modrm_reg})" : $"Op(0x{sample.opCode:x3})";
            // Console.WriteLine($"Amd64Op({sample.map}, {op}, {sample.mnemonic}, {rules})");
        }

        private static bool TestHypothesis(Func<EncodingFlags, SuffixFlags> hypothesis, SuffixFlags sometimes, Dictionary<SuffixFlags, List<Amd64InstructionSample>> samples)
        {
            foreach ((SuffixFlags e, List<Amd64InstructionSample> l) in samples)
            {
                foreach (var sample in l)
                {
                    if (hypothesis(sample.encodingFlags) != (e & sometimes))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static SuffixFlags Test(EncodingFlags e, SuffixFlags t, SuffixFlags f, EncodingFlags g) => g.HasFlag(e) ? t : f;

        public static SuffixFlags Amd64L(SuffixFlags t, SuffixFlags f, EncodingFlags g) => Test(EncodingFlags.L, t, f, g);
        public static SuffixFlags Amd64W(SuffixFlags t, SuffixFlags f, EncodingFlags g) => Test(EncodingFlags.W, t, f, g);
        public static SuffixFlags Amd64P(SuffixFlags t, SuffixFlags f, EncodingFlags g) => Test(EncodingFlags.P, f, t, g);
        public static SuffixFlags Amd64WP(SuffixFlags tx, SuffixFlags ft, SuffixFlags ff, EncodingFlags g) => Amd64W(tx, Amd64P(ft, ff, g), g);

        private void AddOpCode(Map map, int opCode, bool reg, int modrmReg, string rule, HashSet<string> mnemonics)
        {
            rules.Add(rule);

            if (reg)
            {
                if (!opcodes[map].ContainsKey(opCode))
                {
                    currentExtension += 8;
                    string ext = $"InstrForm(int(Extension)|0x{(currentExtension >> 3):x2})";
                    opcodes[map][opCode] = $"        {ext + ",",-40} // 0x{opCode:x3}";
                }
                opcodes[Map.None][currentExtension | modrmReg] = $"        {rule + ",",-40} // {map}:0x{opCode:x3}/{modrmReg} {string.Join(",", mnemonics.OrderBy(s => s))}";
            }
            else
            {
                opcodes[map][opCode] = $"        {rule + ",",-40} // 0x{opCode:x3} {string.Join(",", mnemonics.OrderBy(s => s))}";
            }
        }

        private void WriteCode()
        {
            string none = "None";
            string none3dnow = "MOp_M8B_I1B"; // All 3DNow instructions include a memOp.  All current 3DNow instructions encode the operand as I1B
            rules.Add(none3dnow);

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
            Console.WriteLine("    //      MUnknown // Memory op size is unknown.  Size not included in disassemby");
            Console.WriteLine("    //      MAddr    // Memory op is address load effective address");
            Console.WriteLine("    //      M1B      // Memory op is 1  byte");
            Console.WriteLine("    //      M2B      // Memory op is 2  bytes");
            Console.WriteLine("    //      M4B      // Memory op is 4  bytes");
            Console.WriteLine("    //      M8B      // Memory op is 8  bytes");
            Console.WriteLine("    //      M16B     // Memory op is 16 bytes");
            Console.WriteLine("    //      M32B     // Memory op is 32 bytes");
            Console.WriteLine("    //      M6B      // Memory op is 6  bytes");
            Console.WriteLine("    //      M10B     // Memory op is 10 bytes");
            Console.WriteLine("    //      I1B      // Instruction includes 1  byte  of immediates");
            Console.WriteLine("    //      I2B      // Instruction includes 2  bytes of immediates");
            Console.WriteLine("    //      I3B      // Instruction includes 3  bytes of immediates");
            Console.WriteLine("    //      I4B      // Instruction includes 4  bytes of immediates");
            Console.WriteLine("    //      I8B      // Instruction includes 8  bytes of immediates");
            Console.WriteLine("    //      Unknown  // Instruction samples did not include a modrm configured to produce RIP addressing");
            Console.WriteLine("    //      L        // Flags depend on L bit in encoding.  L_<flagsLTrue>_or_<flagsLFalse>");
            Console.WriteLine("    //      W        // Flags depend on W bit in encoding.  W_<flagsWTrue>_or_<flagsWFalse>");
            Console.WriteLine("    //      P        // Flags depend on OpSize prefix for encoding.  P_<flagsNoOpSizePrefix>_or_<flagsOpSizePrefix>");
            Console.WriteLine("    //      WP       // Flags depend on W bit in encoding and OpSize prefix.  WP_<flagsWTrue>_or__<flagsNoOpSizePrefix>_or_<flagsOpSizePrefix>");
            Console.WriteLine("    //      or       // Flag option separator used in W, L, P, and WP above");
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
            Console.WriteLine("    //   - For Vex* and Xop* the pp is directly included in the encoding");
            Console.WriteLine("    //   - For the Secondary, F38, and F3A pages the pp is not defined in the encoding, but affects instr form.");
            Console.WriteLine("    //          - pp = 0 implies no prefix.");
            Console.WriteLine("    //          - pp = 1 implies 0x66 OpSize prefix only.");
            Console.WriteLine("    //          - pp = 2 implies 0xF3 prefix.");
            Console.WriteLine("    //          - pp = 3 implies 0xF2 prefix.");
            Console.WriteLine("    //   - For the primary and 3DNow pp is not used. And is always 0 in the comments");
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
                if (opcodes[Map.None].ContainsKey(i))
                    Console.WriteLine(opcodes[Map.None][i]);
                else
                    Console.WriteLine($"        {none},");
            }
            Console.WriteLine("    };");

            Console.WriteLine();
            Console.WriteLine($"    static const InstrForm instrFormPrimary[256]");
            Console.WriteLine("    {");
            for (int i = 0; i < 4096; i += 16)
            {
                if (opcodes[Map.Primary].ContainsKey(i))
                    Console.WriteLine(opcodes[Map.Primary][i]);
                else
                    Console.WriteLine($"        {none + ",",-40} // 0x{i:x3}");
            }
            Console.WriteLine("    };");

            Console.WriteLine();
            Console.WriteLine($"    static const InstrForm instrForm3DNow[256]");
            Console.WriteLine("    {");
            for (int i = 0; i < 4096; i += 16)
            {
                if (opcodes[Map.NOW3D].ContainsKey(i))
                    Console.WriteLine(opcodes[Map.NOW3D][i]);
                else
                    Console.WriteLine($"        {none3dnow + ",",-40} // 0x{i:x3}");
            }
            Console.WriteLine("    };");

            var mapTuples = new List<(string, Map)>()
            {("Secondary", Map.Secondary), ("F38", Map.F38), ("F3A", Map.F3A), ("Vex1", Map.Vex1), ("Vex2", Map.Vex2), ("Vex3", Map.Vex3), ("XOP8", Map.XOP8), ("XOP9", Map.XOP9), ("XOPA", Map.XOPA)};

            foreach ((string name, Map map) in mapTuples)
            {
                Console.WriteLine();
                Console.WriteLine($"    static const InstrForm instrForm{name}[1024]");
                Console.WriteLine("    {");
                for (int i = 0; i < 4096; i += 16)
                {
                    for (int pp = 0; pp < 4; pp++)
                    {
                        if (opcodes[map].ContainsKey(i + pp))
                            Console.WriteLine(opcodes[map][i + pp]);
                        else
                            Console.WriteLine($"        {none + ",",-40} // 0x{i + pp:x3}");
                    }
                }
                Console.WriteLine("    };");
            }

            Console.WriteLine("}");
        }

        private static void Main(string[] args)
        {
            new Amd64InstructionTableGenerator();
        }
    }
}
