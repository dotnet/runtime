// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "asmdumper.h"

void ASMDumper::DumpToFile(HANDLE hFile, MethodContext* mc, CompileResult* cr)
{
    CORINFO_METHOD_INFO info;
    unsigned            flags = 0;
    mc->repCompileMethod(&info, &flags);

#define bufflen 4096
    DWORD bytesWritten;
    char  buff[bufflen];

    int buff_offset = 0;
    ZeroMemory(buff, bufflen * sizeof(char));
    buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset,
                             ";;Generated from SuperPMI on original input '%s'", cr->repProcessName());
    buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset, "\r\n Method Name \"%s\"",
                             mc->repGetMethodName(info.ftn, nullptr));
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
    cr->applyRelocs(hotCodeBlock, hotCodeSize, orig_hotCodeBlock);
    cr->applyRelocs(coldCodeBlock, coldCodeSize, orig_coldCodeBlock);
    cr->applyRelocs(roDataBlock, roDataSize, orig_roDataBlock);

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
        buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset, "\r\n%p %S",
                                 (void*)((size_t)orig_hotCodeBlock + offset), instrMnemonic);
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
