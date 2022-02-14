// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// CommandLine.cpp - tiny very specific command line parser
//----------------------------------------------------------

#include "standardpch.h"
#include "commandline.h"
#include "logging.h"
#include "mclist.h"

void CommandLine::DumpHelp(const char* program)
{
    printf("MCS is a utility for examining and manipulating SuperPMI MC files.\n");
    printf("\n");
    printf("Usage: %s [options] {verb} {verb options}", program);
    printf("\n");
    printf("Options:\n");
    printf("\n");
    printf(" -v[erbosity] messagetypes\n");
    printf("     Controls which types of messages MCS logs. Specify a string of\n");
    printf("     characters representing message categories to enable, where:\n");
    printf("         e - errors (internal fatal errors that are non-recoverable)\n");
    printf("         w - warnings (internal conditions that are unusual, but not serious)\n");
    printf("         m - missing (failures due to missing JIT-EE interface details)\n");
    printf("         n - information (notifications/summaries, e.g. 'Loaded 42, Saved 23')\n");
    printf("         v - verbose (status messages, e.g. 'Jit startup took 151.12ms')\n");
    printf("         d - debug (lots of detailed output)\n");
    printf("         a - all (enable all message types; overrides other enable message types)\n");
    printf("         q - quiet (disable all output; overrides all others)\n");
    printf("     e.g. '-v ew' only writes error and warning messages to the console.\n");
    printf("     'q' takes precedence over any other message type specified.\n");
    printf("     Default set of messages enabled is 'ewmnv'.\n");
    printf("\n");
    printf(" -writeLogFile logfile\n");
    printf("     Write log messages to the specified file.\n");
    printf("\n");
    printf("Verbs:\n");
    printf("\n");
    printf(" -ASMDump {optional range} inputfile outputfile\n");
    printf("     Dump out the asm file for each input methodContext.\n");
    printf("     inputfile is read and output is written to outputfile.\n");
    printf("     e.g. -ASMDump a.mc a.asm\n");
    printf("\n");
    printf(" -concat file1 file2\n");
    printf("     Concatenate two files without regard to internal formatting.\n");
    printf("     file2 is appended to file1.\n");
    printf("     e.g. -concat a.mch b.mch\n");
    printf("\n");
    printf(" -copy range file1 file2\n");
    printf("     Copy methodContext numbers in range from file1 to file2.\n");
    printf("     file1 is read and file2 is written\n");
    printf("     e.g. -copy a.mch b.mch\n");
    printf("\n");
    printf(" -dump {optional range} inputfile [-simple]\n");
    printf("     Dump details for each methodContext.\n");
    printf("     With -simple, don't display the function name/arguments in the header (useful for debugging mcs itself).\n");
    printf("     e.g. -dump a.mc\n");
    printf("\n");
    printf(" -dumpMap inputfile\n");
    printf("     Dump a map from MC index to function name to the console, in CSV format\n");
    printf("     e.g. -dumpMap a.mc\n");
    printf("\n");
    printf(" -dumpToc inputfile\n");
    printf("     Dump a TOC file\n");
    printf("     e.g. -dumpToc a.mct\n");
    printf("\n");
    printf(" -fracture range inputfile outputfile\n");
    printf("     Break the input file into chunks sized by range.\n");
    printf("     If '-thin' is also passed, CompileResults are stripped from the input file when written.\n");
    printf("     e.g. '-fracture 3 a.mch b-' leads to b-0.mch, b-1.mch, etc., with 3 mc's in each file.\n");
    printf("\n");
    printf(" -ildump {optional range} inputfile\n");
    printf("     Dump raw IL for each methodContext\n");
    printf("     e.g. -ildump a.mc\n");
    printf("\n");
    printf(" -integ inputfile\n");
    printf("     Check the integrity of each methodContext\n");
    printf("     e.g. -integ a.mc\n");
    printf("\n");
    printf(" -merge outputfile pattern [-dedup [-thin]]\n");
    printf("     Merge all the input files matching the pattern.\n");
    printf("     With -dedup, skip duplicates when copying (like using -removeDup). With -thin, also strip CompileResults.\n");
    printf("     e.g. -merge a.mch *.mc\n");
    printf("     e.g. -merge a.mch c:\\foo\\bar\\*.mc\n");
    printf("     e.g. -merge a.mch relpath\\*.mc\n");
    printf("     e.g. -merge a.mch .\n");
    printf("     e.g. -merge a.mch onedir\n");
    printf("     e.g. -merge a.mch *.mc -dedup -thin\n");
    printf("\n");
    printf(" -merge outputfile pattern -recursive [-dedup [-thin]]\n");
    printf("     Merge all the input files matching the pattern, in the specified and all child directories.\n");
    printf("     With -dedup, skip duplicates when copying (like using -removeDup). With -thin, also strip CompileResults.\n");
    printf("     e.g. -merge a.mch *.mc -recursive\n");
    printf("     e.g. -merge a.mch *.mc -recursive -dedup -thin\n");
    printf("\n");
    printf(" -printJITEEVersion\n");
    printf("     Print the JITEEVersion GUID with which this was built, in the form: a5eec3a4-4176-43a7-8c2b-a05b551d4f49\n");
    printf("\n");
    printf(" -removeDup inputfile outputfile\n");
    printf("     Copy methodContexts from inputfile to outputfile, skipping duplicates.\n");
    printf("     e.g. -removeDup a.mc b.mc\n");
    printf("\n");
    printf(" -removeDup -legacy inputfile outputfile\n");
    printf("     Copy methodContexts from inputfile to outputfile, skipping duplicates.\n");
    printf("     Comparisons are performed using the legacy method and may take much longer\n");
    printf("     e.g. -removeDup -legacy a.mc b.mc\n");
    printf("\n");
    printf(" -removeDup -thin inputfile outputfile\n");
    printf("     Copy methodContexts from inputfile to outputfile, skipping duplicates.\n");
    printf("     CompileResults are stripped from the input file when written.\n");
    printf("     e.g. -removeDup -thin a.mc b.mc\n");
    printf("     e.g. -removeDup -legacy -thin a.mc b.mc\n");
    printf("\n");
    printf(" -stat {optional range} inputfile outputfile\n");
    printf("     Report various statistics per method context.\n");
    printf("     inputfile is read and statistics are written into outputfile\n");
    printf("     e.g. -stat a.mc a.csv\n");
    printf("\n");
    printf(" -strip range inputfile outputfile\n");
    printf("     Copy method contexts from one file to another, skipping ranged items.\n");
    printf("     inputfile is read and records not in range are written to outputfile.\n");
    printf("     e.g. -strip 2 a.mc b.mc\n");
    printf("\n");
    printf(" -toc inputfile\n");
    printf("     Create a Table of Contents file for inputfile to allow better random access\n");
    printf("     to the mch file.\n");
    printf("     e.g. '-toc a.mch' creates a.mch.mct\n");
    printf("\n");
    printf(" -jitflags inputfile\n");
    printf("     Summarize interesting jitflags for the method contexts\n");
    printf("     e.g. '-jitflags a.mch'\n");
    printf("\n");
    printf("Range descriptions are either a single number, or a text file with .mcl extension\n");
    printf("containing a sorted list of line delimited numbers.\n");
    printf("     e.g. -strip 2 a.mc b.mc\n");
    printf("     e.g. -strip list.mcl a.mc b.mc\n");
    printf("\n");
    printf("Note: Inputs are case insensitive.\n");
}

// Assumption: All inputs are initialized to default or real value.  we'll just set the stuff in what we see on the
// command line. Assumption: Single byte names are passed in.. mb stuff doesnt cause an obvious problem... but it might
// have issues... Assumption: Values larger than 2^31 aren't expressible from the commandline.... (atoi) Unless you pass
// in negatives.. :-|
bool CommandLine::Parse(int argc, char* argv[], /* OUT */ Options* o)
{
    size_t argLen  = 0;
    size_t tempLen = 0;

    bool foundVerb  = false;
    bool foundFile1 = false;
    bool foundFile2 = false;

    if (argc == 1) // Print help when no args are passed
    {
        DumpHelp(argv[0]);
        return false;
    }

    for (int i = 1; i < argc; i++)
    {
        bool isASwitch = (argv[i][0] == '-');
#ifndef TARGET_UNIX
        if (argv[i][0] == '/') // Also accept "/" on Windows
        {
            isASwitch = true;
        }
#endif // !TARGET_UNIX

        // Process a switch
        if (isASwitch)
        {
            argLen = strlen(argv[i]);

            if (argLen > 1)
                argLen--; // adjust for leading switch
            else
            {
                DumpHelp(argv[0]);
                return false;
            }

            if ((_strnicmp(&argv[i][1], "help", argLen) == 0) || (_strnicmp(&argv[i][1], "?", argLen) == 0))
            {
                DumpHelp(argv[0]);
                return false;
            }
            else if ((_strnicmp(&argv[i][1], "ASMDump", argLen) == 0))
            {
                tempLen          = strlen(argv[i]);
                foundVerb        = true;
                o->actionASMDump = true;
                if (i + 1 < argc) // Peek to see if we have an mcl file or an integer next
                    goto processMCL;
            }
            else if ((_strnicmp(&argv[i][1], "concat", argLen) == 0))
            {
                tempLen         = strlen(argv[i]);
                foundVerb       = true;
                o->actionConcat = true;
            }
            else if ((_strnicmp(&argv[i][1], "copy", argLen) == 0))
            {
                tempLen       = strlen(argv[i]);
                foundVerb     = true;
                o->actionCopy = true;
                if (i + 1 < argc) // Peek to see if we have an mcl file or an integer next
                    goto processMCL;
            }
            else if ((_strnicmp(&argv[i][1], "dump", argLen) == 0))
            {
                tempLen       = strlen(argv[i]);
                foundVerb     = true;
                o->actionDump = true;
                if (i + 1 < argc) // Peek to see if we have an mcl file or an integer next
                    goto processMCL;
            }
            else if ((_strnicmp(&argv[i][1], "fracture", argLen) == 0))
            {
                tempLen           = strlen(argv[i]);
                foundVerb         = true;
                o->actionFracture = true;
                if (i + 1 < argc) // Peek to see if we have an mcl file or an integer next
                    goto processMCL;
            }
            else if ((_strnicmp(&argv[i][1], "dumpmap", argLen) == 0))
            {
                tempLen          = strlen(argv[i]);
                foundVerb        = true;
                o->actionDumpMap = true;
            }
            else if ((_strnicmp(&argv[i][1], "dumptoc", argLen) == 0))
            {
                tempLen          = strlen(argv[i]);
                foundVerb        = true;
                o->actionDumpToc = true;
            }
            else if ((_strnicmp(&argv[i][1], "jitflags", argLen) == 0))
            {
                tempLen           = strlen(argv[i]);
                foundVerb         = true;
                o->actionJitFlags = true;
            }
            else if ((_strnicmp(&argv[i][1], "ildump", argLen) == 0))
            {
                tempLen         = strlen(argv[i]);
                foundVerb       = true;
                o->actionILDump = true;
                if (i + 1 < argc) // Peek to see if we have an mcl file or an integer next
                    goto processMCL;
            }
            else if ((_strnicmp(&argv[i][1], "merge", argLen) == 0))
            {
                tempLen        = strlen(argv[i]);
                foundVerb      = true;
                o->actionMerge = true;
            }
            else if ((_strnicmp(&argv[i][1], "printjiteeversion", argLen) == 0))
            {
                tempLen        = strlen(argv[i]);
                foundVerb      = true;
                o->actionPrintJITEEVersion = true;
            }
            else if ((_strnicmp(&argv[i][1], "recursive", argLen) == 0))
            {
                tempLen      = strlen(argv[i]);
                o->recursive = true;
            }
            else if ((_strnicmp(&argv[i][1], "dedup", argLen) == 0))
            {
                tempLen  = strlen(argv[i]);
                o->dedup = true;
            }
            else if ((_strnicmp(&argv[i][1], "toc", argLen) == 0))
            {
                tempLen      = strlen(argv[i]);
                foundVerb    = true;
                o->actionTOC = true;
            }
            else if ((_strnicmp(&argv[i][1], "input", argLen) == 0))
            {
                if (++i >= argc)
                {
                    DumpHelp(argv[0]);
                    return false;
                }

            processInput:

                tempLen = strlen(argv[i]);
                if (tempLen == 0)
                {
                    printf("ERROR: CommandLine::Parse() Arg '%s' is invalid, name of file missing.\n", argv[i]);
                    DumpHelp(argv[0]);
                    return false;
                }
                if (foundFile1 == false)
                {
                    o->nameOfFile1 = new char[tempLen + 1];
                    strcpy_s(o->nameOfFile1, tempLen + 1, argv[i]);
                    foundFile1 = true;
                }
                else if (foundFile2 == false)
                {
                    o->nameOfFile2 = new char[tempLen + 1];
                    strcpy_s(o->nameOfFile2, tempLen + 1, argv[i]);
                    foundFile2 = true;
                }
                else
                {
                    printf("ERROR: CommandLine::Parse() Arg '%s' is invalid, too many files given.\n", argv[i]);
                    DumpHelp(argv[0]);
                    return false;
                }
            }
            else if ((_strnicmp(&argv[i][1], "integ", argLen) == 0))
            {
                tempLen        = strlen(argv[i]);
                foundVerb      = true;
                o->actionInteg = true;
            }
            else if ((_strnicmp(&argv[i][1], "mcl", argLen) == 0))
            {
                if (i + 1 >= argc)
                {
                    DumpHelp(argv[0]);
                    return false;
                }

            processMCL:
                i++;
            processMCL2:

                bool isValidList = MCList::processArgAsMCL(argv[i], &o->indexCount, &o->indexes);
                if (!isValidList)
                    i--;
            }
            else if ((_strnicmp(&argv[i][1], "removeDup", argLen) == 0))
            {
                tempLen            = strlen(argv[i]);
                foundVerb          = true;
                o->actionRemoveDup = true;
            }
            else if ((_strnicmp(&argv[i][1], "stat", argLen) == 0))
            {
                tempLen       = strlen(argv[i]);
                foundVerb     = true;
                o->actionStat = true;
                if (i + 1 < argc) // Peek to see if we have an mcl file or an integer next
                    goto processMCL;
            }
            else if ((_strnicmp(&argv[i][1], "strip", argLen) == 0))
            {
                tempLen        = strlen(argv[i]);
                foundVerb      = true;
                o->actionStrip = true;
                if (i + 1 < argc) // Peek to see if we have an mcl file or an integer next
                    goto processMCL;
            }
            else if ((_strnicmp(&argv[i][1], "thin", argLen) == 0))
            {
                o->stripCR = true;
            }
            else if ((_strnicmp(&argv[i][1], "legacy", argLen) == 0))
            {
                o->legacyCompare = true;
            }
            else if ((_strnicmp(&argv[i][1], "verbosity", argLen) == 0))
            {
                if (++i >= argc)
                {
                    DumpHelp(argv[0]);
                    return false;
                }

                Logger::SetLogLevel(Logger::ParseLogLevelString(argv[i]));
            }
            else if ((_strnicmp(&argv[i][1], "writeLogFile", argLen) == 0))
            {
                if (++i >= argc)
                {
                    DumpHelp(argv[0]);
                    return false;
                }

                Logger::OpenLogFile(argv[i]);
            }
            else if ((_strnicmp(&argv[i][1], "simple", argLen) == 0))
            {
                o->simple = true;
            }
            else
            {
                LogError("CommandLine::Parse() - Unknown verb '%s'", argv[i]);
                DumpHelp(argv[0]);
                return false;
            }
        }
        // Process an input filename
        else
        {
            char* lastdot = strrchr(argv[i], '.');
            if (lastdot != nullptr)
            {
                if (_stricmp(lastdot, ".mcl") == 0)
                    goto processMCL2;
            }
            goto processInput;
        }
    }

    if (o->simple)
    {
        if (!o->actionDump)
        {
            LogError("CommandLine::Parse() '-simple' requires -dump.");
            DumpHelp(argv[0]);
            return false;
        }
    }

    if (o->recursive)
    {
        if (!o->actionMerge)
        {
            LogError("CommandLine::Parse() '-recursive' requires -merge.");
            DumpHelp(argv[0]);
            return false;
        }
    }

    if (o->dedup)
    {
        if (!o->actionMerge)
        {
            LogError("CommandLine::Parse() '-dedup' requires -merge.");
            DumpHelp(argv[0]);
            return false;
        }
    }

    if (o->stripCR)
    {
        if (o->actionMerge)
        {
            if (!o->dedup)
            {
                LogError("CommandLine::Parse() '-thin' in '-merge' requires -dedup.");
                DumpHelp(argv[0]);
                return false;
            }
        }
        else if (o->actionRemoveDup || o->actionStrip || o->actionFracture || o->actionCopy)
        {
        }
        else
        {
            LogError("CommandLine::Parse() '-thin' requires -merge, -removeDup, -strip, -fracture, or -copy.");
            DumpHelp(argv[0]);
            return false;
        }
    }

    if (o->actionASMDump)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() -ASMDump needs one input file and one output file.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionConcat)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() '-concat' needs two input files (second will be used as output).");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionMerge)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() '-merge' needs an output file (the first) and a file pattern (the second).");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionCopy)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() '-copy' needs one input and one output.");
            DumpHelp(argv[0]);
            return false;
        }
        if (o->indexCount == 0)
        {
            LogError("CommandLine::Parse() -copy requires a range.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionDump)
    {
        if (!foundFile1)
        {
            LogError("CommandLine::Parse() '-dump' needs one input file, but didn't see one.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionFracture)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() '-fracture' needs one input and one output.");
            DumpHelp(argv[0]);
            return false;
        }
        if (o->indexCount == 0)
        {
            LogError("CommandLine::Parse() -fracture requires a range.");
            DumpHelp(argv[0]);
            return false;
        }
        if (o->indexCount > 1)
        {
            LogWarning("CommandLine::Parse() -fracture found multiple ranges, we'll use the first one.");
        }
        return true;
    }
    if (o->actionDumpMap)
    {
        if (!foundFile1)
        {
            LogError("CommandLine::Parse() '-dumpMap' needs one input.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionDumpToc)
    {
        if (!foundFile1)
        {
            LogError("CommandLine::Parse() '-dumpToc' needs one input.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionJitFlags)
    {
        if (!foundFile1)
        {
            LogError("CommandLine::Parse() '-jitFlags' needs one input.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionILDump)
    {
        if (!foundFile1)
        {
            LogError("CommandLine::Parse() '-ildump' needs one input.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionTOC)
    {
        if (!foundFile1)
        {
            LogError("CommandLine::Parse() '-toc' needs one input.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionInteg)
    {
        if (!foundFile1)
        {
            LogError("CommandLine::Parse() '-integ' needs one input file, but didn't see one.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionRemoveDup)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() -removeDup needs one input file and one output file.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionStat)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() '-stat' needs one input file and one output file.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionStrip)
    {
        if ((!foundFile1) || (!foundFile2))
        {
            LogError("CommandLine::Parse() -strip needs one input file and one output file.");
            DumpHelp(argv[0]);
            return false;
        }
        if (o->indexCount == 0)
        {
            LogError("CommandLine::Parse() -strip requires a range.");
            DumpHelp(argv[0]);
            return false;
        }
        return true;
    }
    if (o->actionPrintJITEEVersion)
    {
        // No arguments to check
        return true;
    }

    DumpHelp(argv[0]);
    return false;
}
