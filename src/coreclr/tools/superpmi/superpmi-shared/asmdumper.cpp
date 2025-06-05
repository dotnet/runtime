// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "asmdumper.h"

void ASMDumper::DumpToFile(HANDLE hFile, MethodContext* mc, CompileResult* cr)
{
    CORINFO_METHOD_INFO info;
    unsigned            flags = 0;
    CORINFO_OS          os    = CORINFO_WINNT;
    mc->repCompileMethod(&info, &flags, &os);

#define bufflen 4096
    DWORD bytesWritten;
    char  buff[bufflen];

    int buff_offset = 0;
    ZeroMemory(buff, bufflen * sizeof(char));
    buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset,
                             ";;Generated from SuperPMI on original input '%s'", cr->repProcessName());

    buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset, "\r\n Method Name \"%s\"",
                             getMethodName(mc, info.ftn).c_str());
    WriteFile(hFile, buff, buff_offset * sizeof(char), &bytesWritten, nullptr);

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

#ifdef USE_MSVCDIS

#ifdef TARGET_AMD64
    DIS* disasm = DIS::PdisNew(DIS::distX8664);
#elif TARGET_X86
    DIS* disasm = DIS::PdisNew(DIS::distX86);
#endif
    size_t offset = 0;
    while (offset < hotCodeSize)
    {
        buff_offset = 0;
        ZeroMemory(buff, bufflen * sizeof(char));

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
        buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset, "\r\n%p %s",
                                 (void*)((size_t)orig_hotCodeBlock + offset), instrMnemonicUtf8.c_str());
        buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset, "   ; ");
        for (unsigned int i = 0; i < instrSize; i++)
            buff_offset +=
                sprintf_s(&buff[buff_offset], bufflen - buff_offset, "%02x ", *((BYTE*)(hotCodeBlock + offset + i)));
        WriteFile(hFile, buff, buff_offset * sizeof(char), &bytesWritten, nullptr);
        offset += instrSize;
    }

    delete disasm;

#else // !USE_MSVCDIS

    buff_offset = 0;
    ZeroMemory(buff, bufflen * sizeof(char));
    buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset, ";; No disassembler available");
    WriteFile(hFile, buff, buff_offset * sizeof(char), &bytesWritten, nullptr);

#endif // !USE_MSVCDIS

    FlushFileBuffers(hFile);
}
