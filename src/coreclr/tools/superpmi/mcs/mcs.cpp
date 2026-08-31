// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "mcs.h"
#include "commandline.h"
#include "verbasmdump.h"
#include "verbinteg.h"
#include "verbdump.h"
#include "verbfracture.h"
#include "verbdumpmap.h"
#include "verbdumptoc.h"
#include "verbjitflags.h"
#include "verbildump.h"
#include "verbtoc.h"
#include "verbremovedup.h"
#include "verbstat.h"
#include "verbconcat.h"
#include "verbmerge.h"
#include "verbstrip.h"
#include "verbprintjiteeversion.h"
#include "logging.h"

int __cdecl main(int argc, char* argv[])
{
#ifdef TARGET_UNIX
    if (0 != PAL_Initialize(argc, argv))
    {
        fprintf(stderr, "Error: Fail to PAL_Initialize\n");
        exit(1);
    }
#endif // TARGET_UNIX

    Logger::Initialize();

    CommandLine::Options o;
    if (!CommandLine::Parse(argc, argv, &o))
    {
        return -1;
    }

    // execute the chosen command.
    int exitCode = 0;
    if (o.actionASMDump)
    {
        exitCode = verbASMDump::DoWork(o.nameOfFile1, o.nameOfFile2, o.indexCount, o.indexes);
    }
    if (o.actionConcat)
    {
        exitCode = verbConcat::DoWork(o.nameOfFile1, o.nameOfFile2);
    }
    if (o.actionMerge)
    {
        exitCode = verbMerge::DoWork(o.nameOfFile1, o.nameOfFile2, o.recursive, o.dedup, o.stripCR);
    }
    if (o.actionCopy)
    {
        exitCode = verbStrip::DoWork(o.nameOfFile1, o.nameOfFile2, o.indexCount, o.indexes, false, o.stripCR);
    }
    if (o.actionDump)
    {
        exitCode = verbDump::DoWork(o.nameOfFile1, o.indexCount, o.indexes, o.simple);
    }
    if (o.actionFracture)
    {
        exitCode = verbFracture::DoWork(o.nameOfFile1, o.nameOfFile2, o.indexCount, o.indexes, o.stripCR);
    }
    if (o.actionDumpMap)
    {
        exitCode = verbDumpMap::DoWork(o.nameOfFile1);
    }
    if (o.actionDumpToc)
    {
        exitCode = verbDumpToc::DoWork(o.nameOfFile1);
    }
    if (o.actionILDump)
    {
        exitCode = verbILDump::DoWork(o.nameOfFile1, o.indexCount, o.indexes);
    }
    if (o.actionInteg)
    {
        exitCode = verbInteg::DoWork(o.nameOfFile1);
    }
    if (o.actionRemoveDup)
    {
        exitCode = verbRemoveDup::DoWork(o.nameOfFile1, o.nameOfFile2, o.stripCR, o.legacyCompare);
    }
    if (o.actionStat)
    {
        exitCode = verbStat::DoWork(o.nameOfFile1, o.nameOfFile2, o.indexCount, o.indexes);
    }
    if (o.actionStrip)
    {
        exitCode = verbStrip::DoWork(o.nameOfFile1, o.nameOfFile2, o.indexCount, o.indexes, true, o.stripCR);
    }
    if (o.actionTOC)
    {
        exitCode = verbTOC::DoWork(o.nameOfFile1);
    }
    if (o.actionPrintJITEEVersion)
    {
        exitCode = verbPrintJITEEVersion::DoWork();
    }
    if (o.actionJitFlags)
    {
        exitCode = verbJitFlags::DoWork(o.nameOfFile1);
    }

    Logger::Shutdown();
    return exitCode;
}
