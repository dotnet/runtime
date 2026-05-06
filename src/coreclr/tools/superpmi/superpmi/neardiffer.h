// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// nearDiffer.h - differ that handles code that is very similar
//----------------------------------------------------------
#ifndef _nearDiffer
#define _nearDiffer

#include "methodcontext.h"
#include "compileresult.h"

class NearDiffer
{
public:
    NearDiffer(const char* targetArch, bool useCorDisTools)
        : TargetArchitecture(targetArch)
        , UseCoreDisTools(useCorDisTools)
#ifdef USE_COREDISTOOLS
        , corAsmDiff(nullptr)
#endif // USE_COREDISTOOLS
    {
    }

    ~NearDiffer();

    bool InitAsmDiff();

    bool compare(MethodContext* mc, CompileResult* cr1, CompileResult* cr2);

    const char* TargetArchitecture;
    const bool  UseCoreDisTools;

private:
    void DumpCodeBlock(unsigned char* block, ULONG blocksize, void* originalAddr);

    bool compareCodeSection(MethodContext* mc,
                            CompileResult* cr1,
                            CompileResult* cr2,
                            unsigned char* block1,
                            ULONG          blocksize1,
                            unsigned char* datablock1,
                            ULONG          datablockSize1,
                            void*          originalBlock1,
                            void*          originalDataBlock1,
                            void*          otherCodeBlock1,
                            ULONG          otherCodeBlockSize1,
                            unsigned char* block2,
                            ULONG          blocksize2,
                            unsigned char* datablock2,
                            ULONG          datablockSize2,
                            void*          originalBlock2,
                            void*          originalDataBlock2,
                            void*          otherCodeBlock2,
                            ULONG          otherCodeBlockSize2);

    bool compareReadOnlyDataBlock(MethodContext* mc,
                                  CompileResult* cr1,
                                  CompileResult* cr2,
                                  unsigned char* block1,
                                  ULONG          blocksize1,
                                  void*          originalDataBlock1,
                                  unsigned char* block2,
                                  ULONG          blocksize2,
                                  void*          originalDataBlock2);
    bool compareEHInfo(MethodContext* mc, CompileResult* cr1, CompileResult* cr2);
    bool compareGCInfo(MethodContext* mc, CompileResult* cr1, CompileResult* cr2);
    bool compareVars(MethodContext* mc, CompileResult* cr1, CompileResult* cr2);
    bool compareBoundaries(MethodContext* mc, CompileResult* cr1, CompileResult* cr2);

    static bool compareOffsets(
        const void* payload, size_t blockOffset, size_t instrLen, uint64_t offset1, uint64_t offset2);

    static bool mungeOffsets(
        const void* payload, size_t blockOffset, size_t instrLen, uint64_t* offset1, uint64_t* offset2, uint32_t* skip1, uint32_t* skip2);

#ifdef USE_COREDISTOOLS

    static bool __cdecl CoreDisCompareOffsetsCallback(
        const void* payload, size_t blockOffset, size_t instrLen, uint64_t offset1, uint64_t offset2);

    static bool __cdecl CoreDisMungeOffsetsCallback(
        const void* payload, size_t blockOffset, size_t instrLen, uint64_t* offset1, uint64_t* offset2, uint32_t* skip1, uint32_t* skip2);

    CorAsmDiff* corAsmDiff;

#endif // USE_COREDISTOOLS

#ifdef USE_MSVCDIS
    DIS* GetMsVcDis();
#endif // USE_MSVCDIS
};

#endif // _nearDiffer
