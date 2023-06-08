// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*
** This program was translated to C# and adapted for xunit-performance.
** New variants of several tests were added to compare class versus
** struct and to compare jagged arrays vs multi-dimensional arrays.
*/

/*
** BYTEmark (tm)
** BYTE Magazine's Native Mode benchmarks
** Rick Grehan, BYTE Magazine
**
** Create:
** Revision: 3/95
**
** DISCLAIMER
** The source, executable, and documentation files that comprise
** the BYTEmark benchmarks are made available on an "as is" basis.
** This means that we at BYTE Magazine have made every reasonable
** effort to verify that the there are no errors in the source and
** executable code.  We cannot, however, guarantee that the programs
** are error-free.  Consequently, McGraw-HIll and BYTE Magazine make
** no claims in regard to the fitness of the source code, executable
** code, and documentation of the BYTEmark.
**
** Furthermore, BYTE Magazine, McGraw-Hill, and all employees
** of McGraw-Hill cannot be held responsible for any damages resulting
** from the use of this code or the results obtained from using
** this code.
*/

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

internal class global
{
    // Following should be modified accordingly per each compilation.
    public const String SysName = "Generic 486/Pentium";
    public const String CompilerName = "CoreCLR";
    public const String CompilerVersion = "ver 0.0";

    public static long min_ticks;
    public static int min_secs;
    public static bool allstats;
    public static String ofile_name;    // Output file name
    public static StreamWriter ofile;   // Output file
    public static bool custrun;         // Custom run flag
    public static bool write_to_file;   // Write output to file
    public static int align;            // Memory alignment

    /*
    ** Following are global structures, one built for
    ** each of the tests.
    */
    public static SortStruct numsortstruct_jagged;    // For numeric sort
    public static SortStruct numsortstruct_rect;      // For numeric sort
    public static StringSort strsortstruct;           // For string sort
    public static BitOpStruct bitopstruct;            // For bitfield ops
    public static EmFloatStruct emfloatstruct_struct; // For emul. float. pt.
    public static EmFloatStruct emfloatstruct_class;  // For emul. float. pt.
    public static FourierStruct fourierstruct;        // For fourier test
    public static AssignStruct assignstruct_jagged;   // For assignment algs
    public static AssignStruct assignstruct_rect;     // For assignment algs
    public static IDEAStruct ideastruct;              // For IDEA encryption
    public static HuffStruct huffstruct;              // For Huffman compression
    public static NNetStruct nnetstruct_jagged;       // For Neural Net
    public static NNetStruct nnetstruct_rect;         // For Neural Net
    public static LUStruct lustruct;                  // For LU decomposition

    public const long TICKS_PER_SEC = 1000;
    public const long MINIMUM_TICKS = 60; // 60 msecs

#if DEBUG
    public const int MINIMUM_SECONDS = 1;
#else
    public const int MINIMUM_SECONDS = 1;
#endif

    public const int NUMNUMARRAYS = 1000;
    public const int NUMARRAYSIZE = 8111;
    public const int STRINGARRAYSIZE = 8111;
    // This is the upper limit of number of string arrays to sort in one
    // iteration. If we can sort more than this number of arrays in less
    // than MINIMUM_TICKS an exception is thrown.
    public const int NUMSTRARRAYS = 100;
    public const int HUFFARRAYSIZE = 5000;
    public const int MAXHUFFLOOPS = 50000;

    // Assignment constants
    public const int ASSIGNROWS = 101;
    public const int ASSIGNCOLS = 101;
    public const int MAXPOSLONG = 0x7FFFFFFF;

    // BitOps constants
#if LONG64
        public const int BITFARRAYSIZE = 16384;
#else
    public const int BITFARRAYSIZE = 32768;
#endif

    // IDEA constants
    public const int MAXIDEALOOPS = 5000;
    public const int IDEAARRAYSIZE = 4000;
    public const int IDEAKEYSIZE = 16;
    public const int IDEABLOCKSIZE = 8;
    public const int ROUNDS = 8;
    public const int KEYLEN = (6 * ROUNDS + 4);

    // LUComp constants
    public const int LUARRAYROWS = 101;
    public const int LUARRAYCOLS = 101;

    // EMFLOAT constants
    public const int CPUEMFLOATLOOPMAX = 50000;
    public const int EMFARRAYSIZE = 3000;

    // FOURIER constants
    public const int FOURIERARRAYSIZE = 100;
}

/*
** TYPEDEFS
*/

public abstract class HarnessTest
{
    public bool bRunTest = true;
    public double score;
    public int adjust;        /* Set adjust code */
    public int request_secs;  /* # of seconds requested */

    public abstract string Name();
    public abstract void ShowStats();
    public abstract double Run();
}

public abstract class SortStruct : HarnessTest
{
    public short numarrays = global.NUMNUMARRAYS;   /* # of arrays */
    public int arraysize = global.NUMARRAYSIZE;     /* # of elements in array */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
    }
}

public abstract class StringSortStruct : HarnessTest
{
    public short numarrays = global.NUMNUMARRAYS;   /* # of arrays */
    public int arraysize = global.STRINGARRAYSIZE;     /* # of elements in array */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
    }
}

public abstract class HuffStruct : HarnessTest
{
    public int arraysize = global.HUFFARRAYSIZE;
    public int loops = 0;
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
    }
}

public abstract class FourierStruct : HarnessTest
{
    public int arraysize = global.FOURIERARRAYSIZE;
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of coefficients: {0}", arraysize));
    }
}

public abstract class AssignStruct : HarnessTest
{
    public short numarrays = global.NUMNUMARRAYS;   /* # of elements in array */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
    }
}

public abstract class BitOpStruct : HarnessTest
{
    public int bitoparraysize;                      /* Total # of bitfield ops */
    public int bitfieldarraysize = global.BITFARRAYSIZE; /* Bit field array size */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Operations array size: {0}", bitoparraysize));
        ByteMark.OutputString(
            string.Format("  Bitfield array size: {0}", bitfieldarraysize));
    }
}

public abstract class IDEAStruct : HarnessTest
{
    public int arraysize = global.IDEAARRAYSIZE;    /* Size of array */
    public int loops;                               /* # of times to convert */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
    }
}

public abstract class LUStruct : HarnessTest
{
    public int numarrays;
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
    }
}

public abstract class NNetStruct : HarnessTest
{
    public int loops;            /* # of times to learn */
    public double iterspersec;     /* Results */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
    }
}

public abstract class EmFloatStruct : HarnessTest
{
    public int arraysize = global.EMFARRAYSIZE;     /* Size of array */
    public int loops;                               /* Loops per iterations */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
    }
}

public class ByteMark
{
    private static int[] s_randw;
    private static double[] s_bindex;
    private static HarnessTest[] s_tests;

    [Fact]
    public static int TestEntryPoint()
    {
        ByteMark app = new ByteMark();
        int result = app.ExecuteCore(Array.Empty<string>());
        return result;
    }

    public int ExecuteCore(string[] argv)
    {
        s_randw = new int[2] { 13, 117 };

        global.min_ticks = global.MINIMUM_TICKS;
        global.min_secs = global.MINIMUM_SECONDS;
        global.allstats = false;
        global.custrun = false;
        global.align = 8;
        global.write_to_file = false;

        /*
        ** Indexes -- Baseline is DELL Pentium XP90
        ** 11/28/94
        */
        // JTR: Should make member of HarnessTest, but left
        // this way to keep similar to original test.
        s_bindex = new double[14] {
            38.993,                     /* Numeric sort */
            38.993,                     /* Numeric sort */
            2.238,                      /* String sort */
            5829704,                    /* Bitfield */
            2.084,                       /* FP Emulation */
            2.084,                       /* FP Emulation */
            879.278,                     /* Fourier */
            .2628,                     /* Assignment */
            .2628,                     /* Assignment */
            65.382,                      /* IDEA */
            36.062,                     /* Huffman */
            .6225,                       /* Neural Net */
            .6225,                       /* Neural Net */
            19.3031                     /* LU Decomposition */
            };

        s_tests = new HarnessTest[14]
        {
            global.numsortstruct_jagged = new NumericSortJagged(),
            global.numsortstruct_rect = new NumericSortRect(),
            global.strsortstruct = new StringSort(),
            global.bitopstruct = new BitOps(),
            global.emfloatstruct_struct = new EMFloat(),
            global.emfloatstruct_class = new EMFloatClass(),
            global.fourierstruct = new Fourier(),
            global.assignstruct_jagged = new AssignJagged(),
            global.assignstruct_rect = new AssignRect(),
            global.ideastruct = new IDEAEncryption(),
            global.huffstruct = new Huffman(),
            global.nnetstruct_jagged = new NeuralJagged(),
            global.nnetstruct_rect = new Neural(),
            global.lustruct = new LUDecomp(),
        };

        SetRequestSecs();

        /*
        ** Handle any command-line arguments.
        */
        int argc = argv.Length;
        if (argc > 0)
        {
            for (int i = 0; i < argc; i++)
            {
                if (parse_arg(argv[i]) == -1)
                {
                    display_help("Bytemark");
                    return -1;
                }
            }
        }

        /*
        ** Output header
        */
        OutputString("BBBBBB   YYY   Y  TTTTTTT  EEEEEEE");
        OutputString("BBB   B  YYY   Y    TTT    EEE");
        OutputString("BBB   B  YYY   Y    TTT    EEE");
        OutputString("BBBBBB    YYY Y     TTT    EEEEEEE");
        OutputString("BBB   B    YYY      TTT    EEE");
        OutputString("BBB   B    YYY      TTT    EEE");
        OutputString("BBBBBB     YYY      TTT    EEEEEEE");
        OutputString("");
        OutputString("BYTEmark (tm) C# Mode Benchmark ver. 2 (06/99)");

        if (global.allstats)
        {
            OutputString("========== ALL STATISTICS ==========");
            DateTime time_and_date = DateTime.Now;
            OutputString("**" +
                time_and_date.ToString("ddd MMM dd HH:mm:ss yyyy"));
            OutputString("**" + global.SysName);
            OutputString("**" + global.CompilerName);
            OutputString("**" + global.CompilerVersion);
            OutputString("**Sizeof: int:4 short:2 long:8");
            OutputString("====================================");
            OutputString("");
        }

        try
        {
            /*
            ** Execute the tests.
            */
            int fpcount = 0;
            int intcount = 0;
            double intindex = 1.0;       /* Integer index */
            double fpindex = 1.0;        /* Floating-point index */
            for (int i = 0; i < s_tests.Length; i++)
            {
                if (s_tests[i].bRunTest)
                {
                    double bmean;     /* Benchmark mean */
                    double bstdev;    /* Benchmark stdev */
                    int bnumrun;   /* # of runs */
                    OutputStringPart(
                        string.Format("{0}:", s_tests[i].Name()));
                    bench_with_confidence(i,
                            out bmean,
                            out bstdev,
                            out bnumrun);
                    OutputString(
                        string.Format("  Iterations/sec: {0:F5}  Index: {1:F5}",
                            bmean,
                            bmean / s_bindex[i]));

                    /*
                    ** Gather integer or FP indexes
                    */
                    // JTR: indexes all have 1 added to them to compensate
                    // for splitting int sort into 2 tests
                    if ((i == 6) || (i == 12) || (i == 13) || (i == 11))
                    {
                        /* FP index */
                        fpindex = fpindex * (bmean / s_bindex[i]);
                        fpcount++;
                    }
                    else
                    {
                        /* Integer index */
                        intindex = intindex * (bmean / s_bindex[i]);
                        intcount++;
                    }

                    if (global.allstats)
                    {
                        OutputString(
                        string.Format("  Standard Deviation: {0}", bstdev));
                        OutputString(
                        string.Format("  Number of runs: {0}", bnumrun));
                        s_tests[i].ShowStats();
                    }
                }
            }
            /*
            ** Output the total indexes
            */
            if (!global.custrun)
            {
                OutputString("===========OVERALL============");
                OutputString(
                    string.Format("INTEGER INDEX: {0:F5}", Math.Pow(intindex, (double)1.0 / (double)intcount)));
                OutputString(
                    string.Format("FLOATING-POINT INDEX: {0:F5}", Math.Pow(fpindex, (double)1.0 / (double)fpcount)));
                OutputString(" (90 MHz Dell Pentium = 1.00)");
                OutputString("==============================");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("ExecuteCore - Exception {0}", e.ToString());
            if (global.ofile != null)
            {
                global.ofile.Flush();
            }
            Console.WriteLine("Exception: {0}", e.ToString());
            return -1;
        }

        return 100;
    }

    /**************
    ** parse_arg **
    ***************
    ** Given a pointer to a string, we assume that's an argument.
    ** Parse that argument and act accordingly.
    ** Return 0 if ok, else return -1.
    */
    private int parse_arg(String arg)
    {
        int i = 0;          /* Index */
        StreamReader cfile = null;    /* Command file identifier */

        /*
        ** First character has got to be a hyphen.
        */
        if (arg[i++] != '-') return (-1);

        /*
        ** Convert the rest of the argument to upper case
        ** so there's little chance of confusion.
        */
        arg = arg.ToUpper();

        /*
        ** Next character picks the action.
        */
        switch (arg[i++])
        {
            case '?': return (-1);     /* Will display help */

            case 'C':                       /* Command file name */
                                            /*
                                            ** First try to open the file for reading.
                                            */
                String inputFileName = arg.Substring(i);

                try
                {
                    cfile = File.OpenText(inputFileName);
                }
                catch (IOException)
                {
                    Console.WriteLine("**Error opening file: {0}", inputFileName);
                    return (-1);
                }

                read_comfile(cfile);    /* Read commands */
                break;
            default:
                return (-1);
        }
        return (0);
    }

    /*******************
    ** display_help() **
    ********************
    ** Display a help message showing argument requirements and such.
    ** Exit when you're done...I mean, REALLY exit.
    */
    private static void display_help(String progname)
    {
        Console.WriteLine("Usage: {0} [-c<FILE>]", progname);
        Console.WriteLine(" -c = Input parameters thru command file <FILE>");
    }

    private enum PF
    { // parameter flags
        GMTICKS = 0,            /* GLOBALMINTICKS */
        MINSECONDS = 1,         /* MINSECONDS */
        ALLSTATS = 2,           /* ALLSTATS */
        OUTFILE = 3,            /* OUTFILE */
        CUSTOMRUN = 4,          /* CUSTOMRUN */
        DONUM = 5,              /* DONUMSORT */
        NUMNUMA = 6,            /* NUMNUMARRAYS */
        NUMASIZE = 7,           /* NUMARRAYSIZE */
        NUMMINS = 8,            /* NUMMINSECONDS */
        DOSTR = 9,              /* DOSTRINGSORT */
        STRASIZE = 10,          /* STRARRAYSIZE */
        NUMSTRA = 11,           /* NUMSTRARRAYS */
        STRMINS = 12,           /* STRMINSECONDS */
        DOBITF = 13,            /* DOBITFIELD */
        NUMBITOPS = 14,         /* NUMBITOPS */
        BITFSIZE = 15,          /* BITFIELDSIZE */
        BITMINS = 16,           /* BITMINSECONDS */
        DOEMF = 17,             /* DOEMF */
        EMFASIZE = 18,          /* EMFARRAYSIZE */
        EMFLOOPS = 19,          /* EMFLOOPS */
        EMFMINS = 20,           /* EMFMINSECOND */
        DOFOUR = 21,            /* DOFOUR */
        FOURASIZE = 22,         /* FOURASIZE */
        FOURMINS = 23,          /* FOURMINSECONDS */
        DOASSIGN = 24,          /* DOASSIGN */
        AARRAYS = 25,           /* ASSIGNARRAYS */
        ASSIGNMINS = 26,        /* ASSIGNMINSECONDS */
        DOIDEA = 27,            /* DOIDEA */
        IDEAASIZE = 28,         /* IDEAARRAYSIZE */
        IDEALOOPS = 29,         /* IDEALOOPS */
        IDEAMINS = 30,          /* IDEAMINSECONDS */
        DOHUFF = 31,            /* DOHUFF */
        HUFFASIZE = 32,         /* HUFFARRAYSIZE */
        HUFFLOOPS = 33,         /* HUFFLOOPS */
        HUFFMINS = 34,          /* HUFFMINSECONDS */
        DONNET = 35,            /* DONNET */
        NNETLOOPS = 36,         /* NNETLOOPS */
        NNETMINS = 37,          /* NNETMINSECONDS */
        DOLU = 38,              /* DOLU */
        LUNARRAYS = 39,         /* LUNUMARRAYS */
        LUMINS = 40,            /* LUMINSECONDS */
        ALIGN = 41,         /* ALIGN */

        // Added for control of new C# rect/jagged struct/class tests
        DONUMJAGGED = 42,        /* DONUMSORTJAGGED */
        DONUMRECT = 43,          /* DONUMSORTRECT */
        DOEMFSTRUCT = 44,        /* DOEMFSTRUCT */
        DOEMFCLASS = 45,         /* DOEMFCLASS */
        DOASSIGNJAGGED = 46,     /* DOASSIGNJAGGED */
        DOASSIGNRECT = 47,       /* DOASSIGNRECT */
        DONNETJAGGED = 48,       /* DONNETJAGGED */
        DONNETRECT = 49,         /* DONNETRECT */

        MAXPARAM = 49
    }

    /* Parameter names */
    private static String[] s_paramnames = {
        "GLOBALMINTICKS",
        "MINSECONDS",
        "ALLSTATS",
        "OUTFILE",
        "CUSTOMRUN",
        "DONUMSORT",
        "NUMNUMARRAYS",
        "NUMARRAYSIZE",
        "NUMMINSECONDS",
        "DOSTRINGSORT",
        "STRARRAYSIZE",
        "NUMSTRARRAYS",
        "STRMINSECONDS",
        "DOBITFIELD",
        "NUMBITOPS",
        "BITFIELDSIZE",
        "BITMINSECONDS",
        "DOEMF",
        "EMFARRAYSIZE",
        "EMFLOOPS",
        "EMFMINSECONDS",
        "DOFOUR",
        "FOURSIZE",
        "FOURMINSECONDS",
        "DOASSIGN",
        "ASSIGNARRAYS",
        "ASSIGNMINSECONDS",
        "DOIDEA",
        "IDEARRAYSIZE",
        "IDEALOOPS",
        "IDEAMINSECONDS",
        "DOHUFF",
        "HUFARRAYSIZE",
        "HUFFLOOPS",
        "HUFFMINSECONDS",
        "DONNET",
        "NNETLOOPS",
        "NNETMINSECONDS",
        "DOLU",
        "LUNUMARRAYS",
        "LUMINSECONDS",
        "ALIGN",

        // Added for control of new C# rect/jagged struct/class tests
        "DONUMSORTJAGGED",
        "DONUMSORTRECT",
        "DOEMFSTRUCT",
        "DOEMFCLASS",
        "DOASSIGNJAGGED",
        "DOASSIGNRECT",
        "DONNETJAGGED",
        "DONNETRECT"
    };

    /*****************
    ** read_comfile **
    ******************
    ** Read the command file.  Set global parameters as
    ** specified.  This routine assumes that the command file
    ** is already open.
    */
    private static void read_comfile(StreamReader cfile)
    {
        String inbuf;

        String eptr;             /* Offset to "=" sign */
        /* markples: now the value half of the key=value pair */

        int eIndex;      /* markples: now this is the "=" offset */

        PF i;                 /* Index */

        /*
        ** Sit in a big loop, reading a line from the file at each
        ** pass.  Terminate on EOF.
        */
        while ((inbuf = cfile.ReadLine()) != null)
        {
            /*
            ** Parse up to the "=" sign.  If we don't find an
            ** "=", then flag an error.
            */
            if ((eIndex = inbuf.IndexOf('=')) == -1)
            {
                Console.WriteLine("**COMMAND FILE ERROR at LINE:");
                Console.WriteLine(" " + inbuf);
                goto skipswitch;        /* A GOTO!!!! */
            }

            /*
            ** Insert a null where the "=" was, then convert
            ** the substring to uppercase.  That will enable
            ** us to perform the match.
            */
            String name = inbuf.Substring(0, eIndex);
            eptr = inbuf.Substring(eIndex + 1);
            name = name.ToUpper();
            i = PF.MAXPARAM;
            do
            {
                if (name == s_paramnames[(int)i])
                {
                    break;
                }
            } while (--i >= 0);

            if (i < 0)
            {
                Console.WriteLine("**COMMAND FILE ERROR -- UNKNOWN PARAM: "
                    + name);
                goto skipswitch;
            }

            /*
            ** Advance eptr to the next field...which should be
            ** the value assigned to the parameter.
            */
            switch (i)
            {
                case PF.GMTICKS:        /* GLOBALMINTICKS */
                    global.min_ticks = Int64.Parse(eptr);
                    break;

                case PF.MINSECONDS:     /* MINSECONDS */
                    global.min_secs = Int32.Parse(eptr);
                    SetRequestSecs();
                    break;

                case PF.ALLSTATS:       /* ALLSTATS */
                    global.allstats = getflag(eptr);
                    break;

                case PF.OUTFILE:        /* OUTFILE */
                    global.ofile_name = eptr;
                    try
                    {
                        global.ofile = File.AppendText(global.ofile_name);
                        global.write_to_file = true;
                        /*
                        ** Open the output file.
                        */
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("**Error opening output file: {0}", global.ofile_name);
                        global.write_to_file = false;
                    }
                    break;

                case PF.CUSTOMRUN:      /* CUSTOMRUN */
                    global.custrun = getflag(eptr);
                    for (i = 0; (int)i < s_tests.Length; i++)
                    {
                        s_tests[(int)i].bRunTest = !global.custrun;
                    }
                    break;

                case PF.DONUM:          /* DONUMSORT */
                    global.numsortstruct_jagged.bRunTest =
                        global.numsortstruct_rect.bRunTest = getflag(eptr);
                    break;

                case PF.NUMNUMA:        /* NUMNUMARRAYS */
                    global.numsortstruct_rect.numarrays = Int16.Parse(eptr);
                    global.numsortstruct_jagged.numarrays = global.numsortstruct_rect.numarrays;
                    global.numsortstruct_jagged.adjust =
                        global.numsortstruct_rect.adjust = 1;
                    break;

                case PF.NUMASIZE:       /* NUMARRAYSIZE */
                    global.numsortstruct_rect.arraysize = Int32.Parse(eptr);
                    global.numsortstruct_jagged.arraysize = global.numsortstruct_rect.arraysize;
                    break;

                case PF.NUMMINS:        /* NUMMINSECONDS */
                    global.numsortstruct_rect.request_secs = Int32.Parse(eptr);
                    global.numsortstruct_jagged.request_secs = global.numsortstruct_rect.request_secs;
                    break;

                case PF.DOSTR:          /* DOSTRINGSORT */
                    global.strsortstruct.bRunTest = getflag(eptr);
                    break;

                case PF.STRASIZE:       /* STRARRAYSIZE */
                    global.strsortstruct.arraysize = Int32.Parse(eptr);
                    break;

                case PF.NUMSTRA:        /* NUMSTRARRAYS */
                    global.strsortstruct.numarrays = Int16.Parse(eptr);
                    global.strsortstruct.adjust = 1;
                    break;

                case PF.STRMINS:        /* STRMINSECONDS */
                    global.strsortstruct.request_secs = Int32.Parse(eptr);
                    break;

                case PF.DOBITF: /* DOBITFIELD */
                    global.bitopstruct.bRunTest = getflag(eptr);
                    break;

                case PF.NUMBITOPS:      /* NUMBITOPS */
                    global.bitopstruct.bitoparraysize = Int32.Parse(eptr);
                    global.bitopstruct.adjust = 1;
                    break;

                case PF.BITFSIZE:       /* BITFIELDSIZE */
                    global.bitopstruct.bitfieldarraysize = Int32.Parse(eptr);
                    break;

                case PF.BITMINS:        /* BITMINSECONDS */
                    global.bitopstruct.request_secs = Int32.Parse(eptr);
                    break;

                case PF.DOEMF:          /* DOEMF */
                    global.emfloatstruct_struct.bRunTest =
                    global.emfloatstruct_class.bRunTest = getflag(eptr);
                    break;

                case PF.EMFASIZE:       /* EMFARRAYSIZE */
                    global.emfloatstruct_class.arraysize = Int32.Parse(eptr);
                    global.emfloatstruct_struct.arraysize = global.emfloatstruct_class.arraysize;
                    break;

                case PF.EMFLOOPS:       /* EMFLOOPS */
                    global.emfloatstruct_class.loops = Int32.Parse(eptr);
                    break;

                case PF.EMFMINS:        /* EMFMINSECOND */
                    global.emfloatstruct_class.request_secs = Int32.Parse(eptr);
                    global.emfloatstruct_struct.request_secs = global.emfloatstruct_class.request_secs;
                    break;

                case PF.DOFOUR: /* DOFOUR */
                    global.fourierstruct.bRunTest = getflag(eptr);
                    break;

                case PF.FOURASIZE:      /* FOURASIZE */
                    global.fourierstruct.arraysize = Int32.Parse(eptr);
                    global.fourierstruct.adjust = 1;
                    break;

                case PF.FOURMINS:       /* FOURMINSECONDS */
                    global.fourierstruct.request_secs = Int32.Parse(eptr);
                    break;

                case PF.DOASSIGN:       /* DOASSIGN */
                    global.assignstruct_jagged.bRunTest =
                    global.assignstruct_rect.bRunTest = getflag(eptr);
                    break;

                case PF.AARRAYS:        /* ASSIGNARRAYS */
                    global.assignstruct_rect.numarrays = Int16.Parse(eptr);
                    global.assignstruct_jagged.numarrays = global.assignstruct_rect.numarrays;
                    break;

                case PF.ASSIGNMINS:     /* ASSIGNMINSECONDS */
                    global.assignstruct_rect.request_secs = Int32.Parse(eptr);
                    global.assignstruct_jagged.request_secs = global.assignstruct_rect.request_secs;
                    break;

                case PF.DOIDEA: /* DOIDEA */
                    global.ideastruct.bRunTest = getflag(eptr);
                    break;

                case PF.IDEAASIZE:      /* IDEAARRAYSIZE */
                    global.ideastruct.arraysize = Int32.Parse(eptr);
                    break;

                case PF.IDEALOOPS:      /* IDEALOOPS */
                    global.ideastruct.loops = Int32.Parse(eptr);
                    break;

                case PF.IDEAMINS:       /* IDEAMINSECONDS */
                    global.ideastruct.request_secs = Int32.Parse(eptr);
                    break;

                case PF.DOHUFF: /* DOHUFF */
                    global.huffstruct.bRunTest = getflag(eptr);
                    break;

                case PF.HUFFASIZE:      /* HUFFARRAYSIZE */
                    global.huffstruct.arraysize = Int32.Parse(eptr);
                    break;

                case PF.HUFFLOOPS:      /* HUFFLOOPS */
                    global.huffstruct.loops = Int32.Parse(eptr);
                    global.huffstruct.adjust = 1;
                    break;

                case PF.HUFFMINS:       /* HUFFMINSECONDS */
                    global.huffstruct.request_secs = Int32.Parse(eptr);
                    break;

                case PF.DONNET: /* DONNET */
                    global.nnetstruct_jagged.bRunTest =
                        global.nnetstruct_rect.bRunTest = getflag(eptr);
                    break;

                case PF.NNETLOOPS:      /* NNETLOOPS */
                    global.nnetstruct_rect.loops = Int32.Parse(eptr);
                    global.nnetstruct_jagged.loops = global.nnetstruct_rect.loops;
                    global.nnetstruct_jagged.adjust =
                        global.nnetstruct_rect.adjust = 1;
                    break;

                case PF.NNETMINS:       /* NNETMINSECONDS */
                    global.nnetstruct_rect.request_secs = Int32.Parse(eptr);
                    global.nnetstruct_jagged.request_secs = global.nnetstruct_rect.request_secs;
                    break;

                case PF.DOLU:           /* DOLU */
                    global.lustruct.bRunTest = getflag(eptr);
                    break;

                case PF.LUNARRAYS:      /* LUNUMARRAYS */
                    global.lustruct.numarrays = Int32.Parse(eptr);
                    global.lustruct.adjust = 1;
                    break;

                case PF.LUMINS: /* LUMINSECONDS */
                    global.lustruct.request_secs = Int32.Parse(eptr);
                    break;

                case PF.ALIGN:          /* ALIGN */
                    global.align = Int32.Parse(eptr);
                    break;

                case PF.DONUMJAGGED:          /* DONUMSORTJAGGED */
                    global.numsortstruct_jagged.bRunTest = getflag(eptr);
                    break;

                case PF.DONUMRECT:          /* DONUMSORTRECT */
                    global.numsortstruct_rect.bRunTest = getflag(eptr);
                    break;

                case PF.DOEMFSTRUCT:          /* DOEMFSTRUCT */
                    global.emfloatstruct_struct.bRunTest = getflag(eptr);
                    break;

                case PF.DOEMFCLASS:          /* DOEMFCLASS */
                    global.emfloatstruct_class.bRunTest = getflag(eptr);
                    break;

                case PF.DOASSIGNJAGGED:       /* DOASSIGNJAGGED */
                    global.assignstruct_jagged.bRunTest = getflag(eptr);
                    break;

                case PF.DOASSIGNRECT:       /* DOASSIGNRECT */
                    global.assignstruct_rect.bRunTest = getflag(eptr);
                    break;

                case PF.DONNETJAGGED: /* DONNETJAGGED */
                    global.nnetstruct_jagged.bRunTest = getflag(eptr);
                    break;

                case PF.DONNETRECT: /* DONNETRECT */
                    global.nnetstruct_rect.bRunTest = getflag(eptr);
                    break;
            }
        skipswitch:
            continue;
        }       /* End while */

        return;
    }

    /************
    ** getflag **
    *************
    ** Return 1 if cptr points to "T"; 0 otherwise.
    */
    private static bool getflag(String cptr)
    {
        return cptr[0] == 'T' || cptr[0] == 't';
    }

    /*********************
    ** set_request_secs **
    **********************
    ** Set everyone's "request_secs" entry to whatever
    ** value is in global.min_secs.  This is done
    ** at the beginning, and possibly later if the
    ** user redefines global.min_secs in the command file.
    */
    private static void SetRequestSecs()
    {
        foreach (HarnessTest ht in s_tests)
        {
            ht.request_secs = global.min_secs;
        }
        return;
    }

    /**************************
    ** bench_with_confidence **
    ***************************
    ** Given a benchmark id that indicates a function, this
    ** routine repeatedly calls that benchmark, seeking
    ** to collect enough scores to get 5 that meet the confidence
    ** criteria.  Return 0 if ok, -1 if failure.
    ** Returns mean ans std. deviation of results if successful.
    */
    private static
    int bench_with_confidence(int fid,       /* Function id */
            out double mean,                   /* Mean of scores */
            out double stdev,                  /* Standard deviation */
            out int numtries)                /* # of attempts */
    {
        double[] myscores = new double[5]; /* Need at least 5 scores */
        double c_half_interval;         /* Confidence half interval */
        int i;                          /* Index */
        double newscore;                /* For improving confidence interval */

        /*
        ** Get first 5 scores.  Then begin confidence testing.
        */
        for (i = 0; i < 5; i++)
        {
            myscores[i] = s_tests[fid].Run();
        }
        numtries = 5;            /* Show 5 attempts */

        /*
        ** The system allows a maximum of 10 tries before it gives
        ** up.  Since we've done 5 already, we'll allow 5 more.
        */

        /*
        ** Enter loop to test for confidence criteria.
        */
        while (true)
        {
            /*
            ** Calculate confidence.
            */
            calc_confidence(myscores,
                    out c_half_interval,
                    out mean,
                    out stdev);

            /*
            ** Is half interval 5% or less of mean?
            ** If so, we can go home.  Otherwise,
            ** we have to continue.
            */
            if (c_half_interval / mean <= (double)0.05)
                break;

            /*
            ** Go get a new score and see if it
            ** improves existing scores.
            */
            do
            {
                if (numtries == 10)
                    return (-1);
                newscore = s_tests[fid].Run();
                numtries += 1;
            } while (seek_confidence(myscores, ref newscore,
                    out c_half_interval, out mean, out stdev) == 0);
        }

        return (0);
    }

    /********************
    ** seek_confidence **
    *********************
    ** Pass this routine an array of 5 scores PLUS a new score.
    ** This routine tries the new score in place of each of
    ** the other five scores to determine if the new score,
    ** when replacing one of the others, improves the confidence
    ** half-interval.
    ** Return 0 if failure.  Original 5 scores unchanged.
    ** Return -1 if success.  Also returns new half-interval,
    ** mean, and stand. dev.
    */
    private static int seek_confidence(double[] scores,
                    ref double newscore,
                    out double c_half_interval,
                    out double smean,
                    out double sdev)
    {
        double sdev_to_beat;    /* Original sdev to be beaten */
        double temp;            /* For doing a swap */
        int is_beaten;          /* Indicates original was beaten */
        int i;                  /* Index */

        /*
        ** First calculate original standard deviation
        */
        calc_confidence(scores, out c_half_interval, out smean, out sdev);
        sdev_to_beat = sdev;
        is_beaten = -1;

        /*
        ** Try to beat original score.  We'll come out of this
        ** loop with a flag.
        */
        for (i = 0; i < 5; i++)
        {
            temp = scores[i];
            scores[i] = newscore;
            calc_confidence(scores, out c_half_interval, out smean, out sdev);
            scores[i] = temp;
            if (sdev_to_beat > sdev)
            {
                is_beaten = i;
                sdev_to_beat = sdev;
            }
        }

        if (is_beaten != -1)
        {
            scores[is_beaten] = newscore;
            return (-1);
        }
        return (0);
    }

    /********************
    ** calc_confidence **
    *********************
    ** Given a set of 5 scores, calculate the confidence
    ** half-interval.  We'l also return the sample mean and sample
    ** standard deviation.
    ** NOTE: This routines presumes a confidence of 95% and
    ** a confidence coefficient of .95
    */
    private static void calc_confidence(double[] scores,    /* Array of scores */
                    out double c_half_interval, /* Confidence half-int */
                    out double smean,           /* Standard mean */
                    out double sdev)            /* Sample stand dev */
    {
        int i;          /* Index */
        /*
        ** First calculate mean.
        */
        smean = (scores[0] + scores[1] + scores[2] + scores[3] + scores[4]) /
                (double)5.0;

        /*
        ** Get standard deviation - first get variance
        */
        sdev = (double)0.0;
        for (i = 0; i < 5; i++)
        {
            sdev += (scores[i] - smean) * (scores[i] - smean);
        }
        sdev /= (double)4.0;
        sdev = Math.Sqrt(sdev) / Math.Sqrt(5.0);

        /*
        ** Now calculate the confidence half-interval.
        ** For a confidence level of 95% our confidence coefficient
        ** gives us a multiplying factor of 2.776
        ** (The upper .025 quartile of a t distribution with 4 degrees
        ** of freedom.)
        */
        c_half_interval = (double)2.776 * sdev;
        return;
    }

    public static void OutputStringPart(String s)
    {
        Console.Write(s);
        if (global.write_to_file)
        {
            global.ofile.Write(s);
        }
    }

    public static void OutputString(String s)
    {
        Console.WriteLine(s);
        if (global.write_to_file)
        {
            global.ofile.WriteLine(s);
            global.ofile.Flush();
        }
    }

    /****************************
    ** TicksToSecs
    ** Converts ticks to seconds.  Converts ticks to integer
    ** seconds, discarding any fractional amount.
    */
    public static int TicksToSecs(long tickamount)
    {
        return ((int)(tickamount / global.TICKS_PER_SEC));
    }

    /****************************
    ** TicksToFracSecs
    ** Converts ticks to fractional seconds.  In other words,
    ** this returns the exact conversion from ticks to
    ** seconds.
    */
    public static double TicksToFracSecs(long tickamount)
    {
        return ((double)tickamount / (double)global.TICKS_PER_SEC);
    }

    public static long StartStopwatch()
    {
        //DateTime t = DateTime.Now;
        //return(t.Ticks);
        return Environment.TickCount;
    }

    public static long StopStopwatch(long start)
    {
        //DateTime t = DateTime.Now;
        //Console.WriteLine(t.Ticks - start);
        //return(t.Ticks-start);
        long x = Environment.TickCount - start;
        //Console.WriteLine(x);
        return x;
    }

    /****************************
    *         randwc()          *
    *****************************
    ** Returns int random modulo num.
    */
    public static int randwc(int num)
    {
        return (randnum(0) % num);
    }

    /***************************
    **      abs_randwc()      **
    ****************************
    ** Same as randwc(), only this routine returns only
    ** positive numbers.
    */
    public static int abs_randwc(int num)
    {
        int temp;       /* Temporary storage */

        temp = randwc(num);
        if (temp < 0) temp = 0 - temp;

        return temp;
    }

    /****************************
    *        randnum()          *
    *****************************
    ** Second order linear congruential generator.
    ** Constants suggested by J. G. Skellam.
    ** If val==0, returns next member of sequence.
    **    val!=0, restart generator.
    */
    public static int randnum(int lngval)
    {
        int interm;

        if (lngval != 0L)
        { s_randw[0] = 13; s_randw[1] = 117; }

        unchecked
        {
            interm = (s_randw[0] * 254754 + s_randw[1] * 529562) % 999563;
        }
        s_randw[1] = s_randw[0];
        s_randw[0] = interm;
        return (interm);
    }

    static void Setup()
    {
        s_randw = new int[2] { 13, 117 };
        global.min_ticks = global.MINIMUM_TICKS;
        global.min_secs = global.MINIMUM_SECONDS;
        global.allstats = false;
        global.custrun = false;
        global.align = 8;
        global.write_to_file = false;
    }
}
