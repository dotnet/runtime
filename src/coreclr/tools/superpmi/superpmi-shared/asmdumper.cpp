// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "asmdumper.h"

void ASMDumper::DumpToFile(FILE* fp, MethodContext* mc, CompileResult* cr)
{
    CORINFO_METHOD_INFO info;
    unsigned            flags = 0;
    CORINFO_OS          os    = CORINFO_WINNT;
    mc->repCompileMethod(&info, &flags, &os);

    fprintf_s(fp, ";;Generated from SuperPMI on original input '%s'", cr->repProcessName());

    fprintf_s(fp, "\n Method Name \"%s\"", getMethodName(mc, info.ftn).c_str());

    ULONG              hotCodeSize;
    ULONG              coldCodeSize;
    ULONG              roDataSize;
    ULONG              xcptnsCount;
    CorJitAllocMemFlag flag;
    unsigned char*     hotCodeBlock;
    unsigned char*     coldCodeBlock;
    unsigned char*     roDataBlock;
    void*              orig_hotCodeBlock;
    void*              orig_coldCodeBlock;
    void*              orig_roDataBlock;

    cr->repAllocMem(&hotCodeSize, &coldCodeSize, &roDataSize, &xcptnsCount, &flag, &hotCodeBlock, &coldCodeBlock,
                    &roDataBlock, &orig_hotCodeBlock, &orig_coldCodeBlock, &orig_roDataBlock);

    RelocContext rc;

    rc.mc                      = mc;
    rc.hotCodeAddress          = (size_t)hotCodeBlock;
    rc.hotCodeSize             = hotCodeSize;
    rc.coldCodeAddress         = (size_t)coldCodeBlock;
    rc.coldCodeSize            = coldCodeSize;
    rc.roDataAddress           = (size_t)roDataBlock;
    rc.roDataSize              = roDataSize;
    rc.originalHotCodeAddress  = (size_t)orig_hotCodeBlock;
    rc.originalColdCodeAddress = (size_t)orig_coldCodeBlock;
    rc.originalRoDataAddress   = (size_t)orig_roDataBlock;

    cr->applyRelocs(&rc, hotCodeBlock, hotCodeSize, orig_hotCodeBlock);
    cr->applyRelocs(&rc, coldCodeBlock, coldCodeSize, orig_coldCodeBlock);
    cr->applyRelocs(&rc, roDataBlock, roDataSize, orig_roDataBlock);

    fprintf_s(fp, ";; No disassembler available");

    fflush(fp);
}
