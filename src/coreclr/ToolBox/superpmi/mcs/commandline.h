// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// CommandLine.h - tiny very specific command line parser
//----------------------------------------------------------
#ifndef _CommandLine
#define _CommandLine

class CommandLine
{
public:
    class Options
    {
    public:
        Options()
            : actionASMDump(false)
            , actionConcat(false)
            , actionCopy(false)
            , actionDump(false)
            , actionDumpMap(false)
            , actionDumpToc(false)
            , actionFracture(false)
            , actionJitFlags(false)
            , actionILDump(false)
            , actionInteg(false)
            , actionMerge(false)
            , actionPrintJITEEVersion(false)
            , actionRemoveDup(false)
            , actionStat(false)
            , actionStrip(false)
            , actionTOC(false)
            , legacyCompare(false)
            , recursive(false)
            , dedup(false)
            , stripCR(false)
            , simple(false)
            , nameOfFile1(nullptr)
            , nameOfFile2(nullptr)
            , nameOfFile3(nullptr)
            , indexCount(-1)
            , indexes(nullptr)
        {
        }

        bool  actionASMDump;
        bool  actionConcat;
        bool  actionCopy;
        bool  actionDump;
        bool  actionDumpMap;
        bool  actionDumpToc;
        bool  actionFracture;
        bool  actionJitFlags;
        bool  actionILDump;
        bool  actionInteg;
        bool  actionMerge;
        bool  actionPrintJITEEVersion;
        bool  actionRemoveDup;
        bool  actionStat;
        bool  actionStrip;
        bool  actionTOC;
        bool  legacyCompare;
        bool  recursive;
        bool  dedup;
        bool  stripCR;
        bool  simple;
        char* nameOfFile1;
        char* nameOfFile2;
        char* nameOfFile3;
        int   indexCount;
        int*  indexes;
    };

    static bool Parse(int argc, char* argv[], /* OUT */ Options* o);

private:
    static void DumpHelp(const char* program);
};
#endif
