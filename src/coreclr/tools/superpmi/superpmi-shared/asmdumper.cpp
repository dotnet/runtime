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

    fprintf(fp, ";;Generated from SuperPMI on original input '%s'", cr->repProcessName());

    fprintf(fp, "\n Method Name \"%s\"", getMethodName(mc, info.ftn).c_str());

    ULONG              hotCodeSize;
    ULONG              coldCodeSize;
    ULONG              roDataSize;
    ULONG              xcptnsCount;
    unsigned char*     hotCodeBlock;
    unsigned char*     coldCodeBlock;
    unsigned char*     roDataBlock;
    void*              orig_hotCodeBlock;
    void*              orig_coldCodeBlock;
    void*              orig_roDataBlock;

    cr->repAllocMem(&hotCodeSize, &coldCodeSize, &roDataSize, &xcptnsCount, &hotCodeBlock, &coldCodeBlock,
                    &roDataBlock, &orig_hotCodeBlock, &orig_coldCodeBlock, &orig_roDataBlock);

    RelocContext rc;

    rc.mc                      = mc;
    rc.hotCodeAddress          = (size_t)hotCodeBlock;
    rc.hotCodeSize             = hotCodeSize;
    rc.coldCodeAddress         = (size_t)coldCodeBlock;
    rc.coldCodeSize            = coldCodeSize;
    rc.roDataAddress           = (size_t)roDataBlock;
    rc.roDataSize1             = roDataSize;
    rc.roDataSize2             = 0;
    rc.originalHotCodeAddress  = (size_t)orig_hotCodeBlock;
    rc.originalColdCodeAddress = (size_t)orig_coldCodeBlock;
    rc.originalRoDataAddress1  = (size_t)orig_roDataBlock;
    rc.originalRoDataAddress2  = 0;

    cr->applyRelocs(&rc, hotCodeBlock, hotCodeSize, orig_hotCodeBlock);
    cr->applyRelocs(&rc, coldCodeBlock, coldCodeSize, orig_coldCodeBlock);
    cr->applyRelocs(&rc, roDataBlock, roDataSize, orig_roDataBlock);

#ifdef USE_MSVCDIS

#ifdef TARGET_AMD64
    DIS* disasm = DIS::PdisNew(DIS::distX8664);
#elif TARGET_X86
    DIS* disasm = DIS::PdisNew(DIS::distX86);
#endif
    size_t offset = 0;
    while (offset < hotCodeSize)
    {
        DIS::INSTRUCTION instr;
        DIS::OPERAND     ops[3];

        size_t instrSize = disasm->CbDisassemble(0, (void*)(hotCodeBlock + offset), 15);
        if (instrSize == 0)
        {
            LogWarning("Zero sized instruction");
            break;
        }
        disasm->FDecode(&instr, ops, 3);

        WCHAR instrMnemonic[64]; // I never know how much to allocate...
        disasm->CchFormatInstr(instrMnemonic, 64);
        std::string instrMnemonicUtf8 = ConvertToUtf8(instrMnemonic);
        fprintf(fp, "\r\n%p %s   ; ", (void*)((size_t)orig_hotCodeBlock + offset), instrMnemonicUtf8.c_str());
        for (unsigned int i = 0; i < instrSize; i++)
            fprintf(fp, "%02x ", *((BYTE*)(hotCodeBlock + offset + i)));
        offset += instrSize;
    }

    delete disasm;

#else // !USE_MSVCDIS

    fprintf(fp, ";; No disassembler available");

#endif // !USE_MSVCDIS

    fflush(fp);
}
