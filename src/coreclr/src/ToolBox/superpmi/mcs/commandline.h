//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
            , actionILDump(false)
            , actionInteg(false)
            , actionMerge(false)
            , actionRemoveDup(false)
            , actionStat(false)
            , actionStrip(false)
            , actionTOC(false)
            , legacyCompare(false)
            , recursive(false)
            , stripCR(false)
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
        bool  actionILDump;
        bool  actionInteg;
        bool  actionMerge;
        bool  actionRemoveDup;
        bool  actionStat;
        bool  actionStrip;
        bool  actionTOC;
        bool  legacyCompare;
        bool  recursive;
        bool  stripCR;
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
