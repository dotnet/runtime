// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// nearDiffer.cpp - differ that handles code that is very similar
//----------------------------------------------------------

#include "standardpch.h"

#ifdef USE_COREDISTOOLS
#include "coredistools.h"
#endif // USE_COREDISTOOLS

#include "logging.h"
#include "neardiffer.h"
#include "spmiutil.h"

#ifdef USE_COREDISTOOLS

//
// Helper functions to print messages from CoreDisTools Library
// The file/linenumber information is from this helper itself,
// since we are only linking with the CoreDisTools library.
//
static void LogFromCoreDisToolsHelper(LogLevel level, const char* msg, va_list argList)
{
    Logger::LogVprintf(__func__, __FILE__, __LINE__, level, argList, msg);
}

#define LOGGER(L)                                                                                                      \
    \
static void __cdecl CorDisToolsLog##L(const char* msg, ...)                                                            \
    \
{                                                                                                               \
        va_list argList;                                                                                               \
        va_start(argList, msg);                                                                                        \
        LogFromCoreDisToolsHelper(LOGLEVEL_##L, msg, argList);                                                         \
        va_end(argList);                                                                                               \
    \
}

LOGGER(VERBOSE)
LOGGER(ERROR)
LOGGER(WARNING)

const PrintControl CorPrinter = {CorDisToolsLogERROR, CorDisToolsLogWARNING, CorDisToolsLogVERBOSE,
                                 CorDisToolsLogVERBOSE};

#endif // USE_COREDISTOOLS

#ifdef USE_COREDISTOOLS
NewDiffer_t*          g_PtrNewDiffer          = nullptr;
FinishDiff_t*         g_PtrFinishDiff         = nullptr;
NearDiffCodeBlocks_t* g_PtrNearDiffCodeBlocks = nullptr;
DumpDiffBlocks_t*     g_PtrDumpDiffBlocks     = nullptr;
#endif // USE_COREDISTOOLS

//
// The NearDiff Disassembler initialization.
//
// Returns true on success, false on failure.
//
bool NearDiffer::InitAsmDiff()
{
#ifdef USE_COREDISTOOLS

    if (UseCoreDisTools)
    {
        const WCHAR* coreDisToolsLibrary = MAKEDLLNAME_W("coredistools");
#ifdef TARGET_UNIX
        // Unix will require the full path to coredistools. Assume that the
        // location is next to the full path to the superpmi.so.

        WCHAR coreCLRLoadedPath[MAX_LONGPATH];
        HMODULE result = 0;
        int returnVal = ::GetModuleFileNameW(result, coreCLRLoadedPath, MAX_LONGPATH);

        if (returnVal == 0)
        {
            LogError("GetModuleFileNameW failed (0x%08x)", ::GetLastError());
            return false;
        }

        WCHAR* ptr = ::wcsrchr(coreCLRLoadedPath, '/');

        // Move past the / character.
        ptr = ptr + 1;

        const WCHAR* coreDisToolsLibraryName = MAKEDLLNAME_W("coredistools");
        ::wcscpy_s(ptr, &coreCLRLoadedPath[MAX_LONGPATH] - ptr, coreDisToolsLibraryName);
        coreDisToolsLibrary = coreCLRLoadedPath;
#endif // TARGET_UNIX

        HMODULE hCoreDisToolsLib = ::LoadLibraryW(coreDisToolsLibrary);
        if (hCoreDisToolsLib == 0)
        {
            LogError("LoadLibrary(%s) failed (0x%08x)", MAKEDLLNAME_A("coredistools"), ::GetLastError());
            return false;
        }

        g_PtrNewDiffer = (NewDiffer_t*)::GetProcAddress(hCoreDisToolsLib, "NewDiffer");
        if (g_PtrNewDiffer == nullptr)
        {
            LogError("GetProcAddress 'NewDiffer' failed (0x%08x)", ::GetLastError());
            return false;
        }
        g_PtrFinishDiff = (FinishDiff_t*)::GetProcAddress(hCoreDisToolsLib, "FinishDiff");
        if (g_PtrFinishDiff == nullptr)
        {
            LogError("GetProcAddress 'FinishDiff' failed (0x%08x)", ::GetLastError());
            return false;
        }
        g_PtrNearDiffCodeBlocks = (NearDiffCodeBlocks_t*)::GetProcAddress(hCoreDisToolsLib, "NearDiffCodeBlocks");
        if (g_PtrNearDiffCodeBlocks == nullptr)
        {
            LogError("GetProcAddress 'NearDiffCodeBlocks' failed (0x%08x)", ::GetLastError());
            return false;
        }
        g_PtrDumpDiffBlocks = (DumpDiffBlocks_t*)::GetProcAddress(hCoreDisToolsLib, "DumpDiffBlocks");
        if (g_PtrDumpDiffBlocks == nullptr)
        {
            LogError("GetProcAddress 'DumpDiffBlocks' failed (0x%08x)", ::GetLastError());
            return false;
        }

        TargetArch coreDisTargetArchitecture = Target_Host;

        if (TargetArchitecture != nullptr)
        {
            if ((0 == _stricmp(TargetArchitecture, "x64")) || (0 == _stricmp(TargetArchitecture, "amd64")))
            {
                coreDisTargetArchitecture = Target_X64;
            }
            else if (0 == _stricmp(TargetArchitecture, "x86"))
            {
                coreDisTargetArchitecture = Target_X86;
            }
            else if ((0 == _stricmp(TargetArchitecture, "arm")) || (0 == _stricmp(TargetArchitecture, "arm32")))
            {
                coreDisTargetArchitecture = Target_Thumb;
            }
            else if (0 == _stricmp(TargetArchitecture, "arm64"))
            {
                coreDisTargetArchitecture = Target_Arm64;
            }
            else
            {
                LogError("Illegal target architecture '%s'", TargetArchitecture);
            }
        }
        corAsmDiff = (*g_PtrNewDiffer)(coreDisTargetArchitecture, &CorPrinter, NearDiffer::CoreDisCompareOffsetsCallback);
    }
#endif // USE_COREDISTOOLS

    return true;
}

#ifdef USE_COREDISTOOLS
// static
bool __cdecl NearDiffer::CoreDisCompareOffsetsCallback(
    const void* payload, size_t blockOffset, size_t instrLen, uint64_t offset1, uint64_t offset2)
{
    return compareOffsets(payload, blockOffset, instrLen, offset1, offset2);
}
#endif // USE_COREDISTOOLS

//
// The NearDiff destructor
//
NearDiffer::~NearDiffer()
{
#ifdef USE_COREDISTOOLS
    if (corAsmDiff != nullptr)
    {
        (*g_PtrFinishDiff)(corAsmDiff);
    }
#endif // USE_COREDISTOOLS
}

// At a high level, the near differ takes in a method context and two compile results, performs
// some simple fixups, and then compares the main artifacts of the compile result (i.e. generated
// code, GC info, EH info, debug info, etc.) for equality. In order to be fast, the fixups and
// definitions of "equality" are minimal; for example, the GC info check just does a simple memcmp.
//
// The entrypoint into the near differ is nearDiffer::compare; its doc comments will have more
// details on what it does. That function in turn fans out to various other components. For asm
// diffing, the main function of interest will be nearDiffer::compareCodeSection.
//
// Most of the diffing logic is architecture-independent, with the following exceptions:
//
//   - The MSDIS instance must be created with knowledge of the architecture it is working with.
//   - The heuristics to compare different literal operand values has some architecture-specific
//     assumptions.
//   - The code stream is fixed up using relocations recorded during compilation time. The logic
//     for applying these should, in theory, be architecture independent, but depending on how
//     the runtime implements this from platform to platform, there might be subtle differences here.
//

#ifdef USE_MSVCDIS

DIS* NearDiffer::GetMsVcDis()
{
    DIS* disasm;

#ifdef TARGET_AMD64
    if ((TargetArchitecture != nullptr) && (0 == _stricmp(TargetArchitecture, "arm64")))
    {
        disasm = DIS::PdisNew(DIS::distArm64);
    }
    else
    {
        disasm = DIS::PdisNew(DIS::distX8664);
    }
#elif defined(TARGET_X86)
    disasm = DIS::PdisNew(DIS::distX86);
#endif

    return disasm;
}

#endif // USE_MSVCDIS

//
// Simple, quick-and-dirty disassembler. If NearDiffer::compareCodeSection finds that two code
// streams differ, it will call this to dump the two differing code blocks to the log. The dump
// is logged under the verbose logging level.
//
// The output format is in MSDIS's disassembly format.
//
// Arguments:
//    block        - A pointer to the code block to disassemble.
//    blocksize    - The size of the code block to disassemble.
//    originalAddr - The original base address of the code block.
//
void NearDiffer::DumpCodeBlock(unsigned char* block, ULONG blocksize, void* originalAddr)
{
#ifdef USE_MSVCDIS
    DIS*        disasm = GetMsVcDis();
    size_t      offset = 0;
    std::string codeBlock;

    while (offset < blocksize)
    {
        DIS::INSTRUCTION instr;
        DIS::OPERAND     ops[3];

        size_t instrSize = disasm->CbDisassemble((DIS::ADDR)originalAddr + offset, (void*)(block + offset), 15);
        if (instrSize == 0)
        {
            LogWarning("Zero sized instruction");
            break;
        }
        disasm->FDecode(&instr, ops, 3);

        WCHAR instrMnemonicWide[64]; // I never know how much to allocate...
        disasm->CchFormatInstr(instrMnemonicWide, 64);
        char   instrMnemonic[128];
        size_t count;
        wcstombs_s(&count, instrMnemonic, 128, instrMnemonicWide, 64);

        const size_t minInstrBytes = 7;
        size_t       instrBytes    = max(instrSize, minInstrBytes);
        size_t       buffSize      = sizeof("%p %s\n") + 10 + count + 3 * instrBytes + 1;
        char*        buff          = new char[buffSize];
        int          written       = 0;
        written += sprintf_s(buff, buffSize, "%p ", (void*)((size_t)originalAddr + offset));
        for (size_t i = 0; i < instrBytes; i++)
        {
            if (i < instrSize)
            {
                written +=
                    sprintf_s(buff + written, buffSize - written, "%02X ", *(const uint8_t*)(block + offset + i));
            }
            else
            {
                written += sprintf_s(buff + written, buffSize - written, "   ");
            }
        }
        written += sprintf_s(buff + written, buffSize - written, "%s\n", instrMnemonic);
        codeBlock += buff;
        delete[] buff;
        offset += instrSize;
    }
    LogVerbose("Code dump:\n%s", codeBlock.c_str());
    delete disasm;
#else  // !USE_MSVCDIS
    LogVerbose("No disassembler");
#endif // !USE_MSVCDIS
}

//
// Struct to capture the information required by offset comparator.
//
struct DiffData
{
    // Common Data
    CompileResult* cr1;
    CompileResult* cr2;

    // Details of the first block
    unsigned char* block1;
    size_t         blocksize1;
    unsigned char* datablock1;
    size_t         datablockSize1;
    size_t         originalBlock1;
    size_t         originalDataBlock1;
    size_t         otherCodeBlock1;
    size_t         otherCodeBlockSize1;

    // Details of the second block
    unsigned char* block2;
    size_t         blocksize2;
    unsigned char* datablock2;
    size_t         datablockSize2;
    size_t         originalBlock2;
    size_t         originalDataBlock2;
    size_t         otherCodeBlock2;
    size_t         otherCodeBlockSize2;
};

//
// NearDiff Offset Comparator.
// Determine whether two syntactically different constants are
// semantically equivalent, using certain heuristics.
//
bool NearDiffer::compareOffsets(
    const void* payload, size_t blockOffset, size_t instrLen, uint64_t offset1, uint64_t offset2)
{
    // The trivial case
    if (offset1 == offset2)
    {
        return true;
    }

    const SPMI_TARGET_ARCHITECTURE targetArch = GetSpmiTargetArchitecture();
    const DiffData* data         = (const DiffData*)payload;
    size_t          ip1          = data->originalBlock1 + blockOffset;
    size_t          ip2          = data->originalBlock2 + blockOffset;
    size_t          ipRelOffset1 = ip1 + instrLen + (size_t)offset1;
    size_t          ipRelOffset2 = ip2 + instrLen + (size_t)offset2;

    // Case where we have a call into flat address -- the most common case.
    size_t gOffset1 = ipRelOffset1;
    size_t gOffset2 = ipRelOffset2;
    if ((DWORD)gOffset1 ==
        (DWORD)gOffset2) // make sure the lower 32bits match (best we can do in the current replay form)
        return true;

    // Case where we have an offset into the read only section (e.g. loading a float value)
    size_t roOffset1a = (size_t)offset1 - data->originalDataBlock1;
    size_t roOffset2a = (size_t)offset2 - data->originalDataBlock2;
    if ((roOffset1a == roOffset2a) &&
        (roOffset1a < data->datablockSize1)) // Confirm its an offset that fits inside our RoRegion
        return true;

    // This case is written to catch IP-relative offsets to the RO data-section
    // For example:
    //
    size_t roOffset1b = ipRelOffset1 - data->originalDataBlock1;
    size_t roOffset2b = ipRelOffset2 - data->originalDataBlock2;
    if ((roOffset1b == roOffset2b) &&
        (roOffset1b < data->datablockSize1)) // Confirm its an offset that fits inside our RoRegion
        return true;

    // Case where we push an address to our own code section.
    size_t gOffset1a = (size_t)offset1 - data->originalBlock1;
    size_t gOffset2a = (size_t)offset2 - data->originalBlock2;
    if ((gOffset1a == gOffset2a) && (gOffset1a < data->blocksize1)) // Confirm its in our code region
        return true;

    // Case where we push an address in the other codeblock.
    size_t gOffset1b = (size_t)offset1 - data->otherCodeBlock1;
    size_t gOffset2b = (size_t)offset2 - data->otherCodeBlock2;
    if ((gOffset1b == gOffset2b) && (gOffset1b < data->otherCodeBlockSize1)) // Confirm it's in the other code region
        return true;

    // Case where we have an offset into the hot codeblock from the cold code block (why?)
    size_t ocOffset1 = ipRelOffset1 - data->otherCodeBlock1;
    size_t ocOffset2 = ipRelOffset2 - data->otherCodeBlock2;
    if (ocOffset1 == ocOffset2) // Would be nice to check to see if it fits in the other code block
        return true;

    // In the below, it seems rather odd to pass artifacts from cr1 into queries for cr2.
    //
    // One would generally expect to ask if cr1->map(artifact1) == cr2->map(artifact2).
    // Leaving things this way for now as it seems to work.

    // VSD calling case.
    size_t Offset1 = (ipRelOffset1 - 8);
    if (data->cr2->CallTargetTypes->GetIndex((DWORDLONG)Offset1) != -1)
    {
        // This logging is too noisy, so disable it.
        // LogVerbose("Found VSD callsite, did softer compare than ideal");
        return true;
    }

    // x86 VSD calling cases.
    size_t Offset1b = (size_t)offset1 - 4;
    size_t Offset2b = (size_t)offset2;
    if (data->cr2->CallTargetTypes->GetIndex((DWORDLONG)Offset1b) != -1)
    {
        // This logging is too noisy, so disable it.
        // LogVerbose("Found VSD callsite, did softer compare than ideal");
        return true;
    }
    if (data->cr2->CallTargetTypes->GetIndex((DWORDLONG)Offset2b) != -1)
    {
        // This logging is too noisy, so disable it.
        // LogVerbose("Found VSD callsite, did softer compare than ideal");
        return true;
    }

    // Case might be a field address that we handed out to handle inlined values being loaded into
    // a register as an immediate value (and where the address is encoded as an indirect immediate load)
    size_t realTargetAddr = (size_t)data->cr2->searchAddressMap((void*)gOffset2);
    if (realTargetAddr == gOffset1)
        return true;

    // Case might be a field address that we handed out to handle inlined values being loaded into
    // a register as an immediate value (and where the address is encoded and loaded by immediate into a register)
    realTargetAddr = (size_t)data->cr2->searchAddressMap((void*)offset2);
    if (realTargetAddr == offset1)
        return true;
    if (realTargetAddr == 0x424242) // this offset matches what we got back from a getTailCallCopyArgsThunk
        return true;

    realTargetAddr = (size_t)data->cr2->searchAddressMap((void*)(gOffset2));
    if (realTargetAddr != (size_t)-1) // we know this was passed out as a bbloc
        return true;

    // A new clause that tries to handle the case where neither cr1 or cr2 is the
    // recorded CR (diff case with two jits, neither of which is the one used for
    // collection)
    //
    size_t mapped1 = (size_t)data->cr1->searchAddressMap((void*)offset1);
    size_t mapped2 = (size_t)data->cr2->searchAddressMap((void*)offset2);

    if ((mapped1 == mapped2) && (mapped1 != (size_t)-1))
        return true;

    // There are some cases on arm64 where we generate multiple instruction register construction of addresses
    // but we don't have a relocation for them (so they aren't handled by `applyRelocs`). One case is
    // allocPgoInstrumentationBySchema(), which returns an address the JIT writes into the code stream
    // (used to store dynamic PGO probe data).
    //
    // The instruction sequence is something like this:
    //     mov     x0, #63408
    //     movk    x0, #23602, lsl #16
    //     movk    x0, #606, lsl #32
    //
    // Here, we try to match this sequence and look it up in the address map.
    //
    // Since the mov/movk sequence is specific to the replay address constant, we don't assume the baseline
    // and diff have the same number of instructions (e.g., it's possible to skip a `movk` if it is zero).
    // 
    // Some version of this logic might apply to ARM as well.
    //
    if (targetArch == SPMI_TARGET_ARCHITECTURE_ARM64)
    {
        bool movk2_1 = false, movk3_1 = false;
        bool movk2_2 = false, movk3_2 = false;

        unsigned reg1_1 = 0, reg2_1, reg3_1, reg4_1;
        unsigned reg1_2 = 0, reg2_2, reg3_2, reg4_2;
        unsigned con1_1, con2_1, con3_1, con4_1;
        unsigned con1_2, con2_2, con3_2, con4_2;
        unsigned shift2_1, shift3_1, shift4_1;
        unsigned shift2_2, shift3_2, shift4_2;

        UINT32* iaddr1    = (UINT32*)(data->block1 + blockOffset);
        UINT32* iaddr2    = (UINT32*)(data->block2 + blockOffset);
        UINT32* iaddr1end = (UINT32*)(data->block1 + data->blocksize1);
        UINT32* iaddr2end = (UINT32*)(data->block2 + data->blocksize2);

        DWORDLONG addr1 = 0;
        DWORDLONG addr2 = 0;

        // Look for a mov/movk address pattern in code stream 1.

        if ((iaddr1 < iaddr1end) &&
            GetArm64MovConstant(iaddr1, &reg1_1, &con1_1))
        {
            // We assume the address requires at least 1 'movk' instruction.
            if ((iaddr1 + 1 < iaddr1end) &&
                GetArm64MovkConstant(iaddr1 + 1, &reg2_1, &con2_1, &shift2_1) &&
                (reg1_1 == reg2_1))
            {
                addr1 = (DWORDLONG)con1_1 + ((DWORDLONG)con2_1 << shift2_1);

                if ((iaddr1 + 2 < iaddr1end) &&
                    GetArm64MovkConstant(iaddr1 + 2, &reg3_1, &con3_1, &shift3_1) &&
                    (reg1_1 == reg3_1))
                {
                    movk2_1 = true;
                    addr1 += (DWORDLONG)con3_1 << shift3_1;

                    if ((iaddr1 + 3 < iaddr1end) &&
                        GetArm64MovkConstant(iaddr1 + 3, &reg4_1, &con4_1, &shift4_1) &&
                        (reg1_1 == reg4_1))
                    {
                        movk3_1 = true;
                        addr1 += (DWORDLONG)con4_1 << shift4_1;
                    }
                }
            }
        }

        // Look for a mov/movk address pattern in code stream 2.

        if ((iaddr2 < iaddr2end) &&
            GetArm64MovConstant(iaddr2, &reg1_2, &con1_2))
        {
            // We assume the address requires at least 1 'movk' instruction.
            if ((iaddr2 + 1 < iaddr2end) &&
                GetArm64MovkConstant(iaddr2 + 1, &reg2_2, &con2_2, &shift2_2) &&
                (reg1_2 == reg2_2))
            {
                addr2 = (DWORDLONG)con1_2 + ((DWORDLONG)con2_2 << shift2_2);

                if ((iaddr2 + 2 < iaddr2end) &&
                    GetArm64MovkConstant(iaddr2 + 2, &reg3_2, &con3_2, &shift3_2) &&
                    (reg1_2 == reg3_2))
                {
                    movk2_2 = true;
                    addr2 += (DWORDLONG)con3_2 << shift3_2;

                    if ((iaddr2 + 3 < iaddr2end) &&
                        GetArm64MovkConstant(iaddr2 + 3, &reg4_2, &con4_2, &shift4_2) &&
                        (reg1_2 == reg4_2))
                    {
                        movk3_2 = true;
                        addr2 += (DWORDLONG)con4_2 << shift4_2;
                    }
                }
            }
        }

        // Check the constants. We don't need to check 'addr1 == addr2' because if that were
        // true we wouldn't have gotten here.
        //
        // Note: when replaying on a 32-bit platform, we must have
        // movk2_1 == movk2_2 == movk3_1 == movk3_2 == false

        if ((addr1 != 0) && (addr2 != 0) && (reg1_1 == reg1_2))
        {
            DWORDLONG mapped1 = (DWORDLONG)data->cr1->searchAddressMap((void*)addr1);
            DWORDLONG mapped2 = (DWORDLONG)data->cr2->searchAddressMap((void*)addr2);
            if ((mapped1 == mapped2) && (mapped1 != (DWORDLONG)-1))
            {
                // Now, zero out the constants in the `movk` instructions so when the disassembler
                // gets to them, they compare equal.
                PutArm64MovkConstant(iaddr1 + 1, 0);
                PutArm64MovkConstant(iaddr2 + 1, 0);
                if (movk2_1)
                {
                    PutArm64MovkConstant(iaddr1 + 2, 0);
                }
                if (movk2_2)
                {
                    PutArm64MovkConstant(iaddr2 + 2, 0);
                }
                if (movk3_1)
                {
                    PutArm64MovkConstant(iaddr1 + 3, 0);
                }
                if (movk3_2)
                {
                    PutArm64MovkConstant(iaddr2 + 3, 0);
                }
                return true;
            }
        }
    }

    return false;
}

//
// Compares two code sections for syntactic equality. This is the core of the asm diffing logic.
//
// This mostly relies on MSDIS's decoded representation of an instruction to compare for equality.
// That is, using MSDIS's internal IR, this goes through the code stream and compares, instruction
// by instruction, op code and operand values for equality.
//
// Obviously, just blindly comparing operand values will raise a lot of false alarms. In order to
// compensate for phenomena like literal pointer addresses in the code stream changing, this applies
// some heuristics on mismatching operand values to try to normalize them a little bit. Essentially,
// if operand values don't match, they are re-interpreted as various relative deltas from known base
// addresses. For example, a common case is a pointer into the read-only data section. One of the
// heuristics subtracts both operand values from the base address of the read-only data section and
// checks to see if they are the same distance away from their respective read-only base addresses.
//
// Notes:
//    - The core syntactic comparison is platform agnostic; we compare op codes and operand values
//      using MSDIS's architecture-independent IR (i.e. the data structures defined in msvcdis.h).
//      Only the disassembler instance itself is initialized differently based on the target arch-
//      itecture.
//    - That being said, the heuristics themselves are not guaranteed to be platform agnostic. For
//      instance, there is a case that applies only to x86 VSD calls. When porting the near differ
//      to new platforms, these special cases should be examined and ported with care.
//
// Arguments:
//    mc                 - The method context of the method to diff. Unused.
//    cr1                - The first compile result to compare. Unused.
//    cr2                - The second compile result to compare. Unused.
//    block1             - A pointer to the first code block to disassemble.
//    blocksize1         - The size of the first code block to compare.
//    datablock1         - A pointer to the first read-only data block to compare. Unused.
//    datablockSize1     - The size of the first read-only data block to compare.
//    originalBlock1     - The original base address of the first code block.
//    originalDataBlock1 - The original base address of the first read-only data block.
//    otherCodeBlock1    - The original base address of the first cold code block. Note that this is
//                         just an address; we don't need the cold code buffer.
//    otherCodeBlockSize1- The size of the first cold code block.
//    block2             - A pointer to the second code block to disassemble.
//    blocksize2         - The size of the second code block to compare.
//    datablock2         - A pointer to the second read-only data block to compare.
//    datablockSize2     - The size of the second read-only data block to compare.
//    originalBlock2     - The original base address of the second code block.
//    originalDataBlock2 - The original base address of the second read-only data block.
//    otherCodeBlock2    - The original base address of the second cold code block. Note that this is
//                         just an address; we don't need the cold code buffer.
//    otherCodeBlockSize2- The size of the second cold code block.
//
// Return Value:
//    True if the code sections are syntactically identical; false otherwise.
//

bool NearDiffer::compareCodeSection(MethodContext* mc,
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
                                    ULONG          otherCodeBlockSize2)
{
    DiffData data = {cr1,
                     cr2,

                     // Details of the first block
                     block1, (size_t)blocksize1, datablock1, (size_t)datablockSize1, (size_t)originalBlock1,
                     (size_t)originalDataBlock1, (size_t)otherCodeBlock1, (size_t)otherCodeBlockSize1,

                     // Details of the second block
                     block2, (size_t)blocksize2, datablock2, (size_t)datablockSize2, (size_t)originalBlock2,
                     (size_t)originalDataBlock2, (size_t)otherCodeBlock2, (size_t)otherCodeBlockSize2};

#ifdef USE_COREDISTOOLS
    if (UseCoreDisTools)
    {
        bool areSame = (*g_PtrNearDiffCodeBlocks)(corAsmDiff, &data, (const uint8_t*)originalBlock1, block1, blocksize1,
                                                  (const uint8_t*)originalBlock2, block2, blocksize2);

        if (!areSame)
        {
            (*g_PtrDumpDiffBlocks)(corAsmDiff, (const uint8_t*)originalBlock1, block1, blocksize1,
                                   (const uint8_t*)originalBlock2, block2, blocksize2);
        }

        return areSame;
    }
#endif // USE_COREDISTOOLS

#ifdef USE_MSVCDIS
    bool haveSeenRet = false;
    DIS* disasm_1    = GetMsVcDis();
    DIS* disasm_2    = GetMsVcDis();

    size_t offset = 0;

    if (blocksize1 != blocksize2)
    {
        LogVerbose("Code sizes don't match %u != %u", blocksize1, blocksize2);
        goto DumpDetails;
    }

    while (offset < blocksize1)
    {
        DIS::INSTRUCTION instr_1;
        DIS::INSTRUCTION instr_2;
        const int        MaxOperandCount = 5;
        DIS::OPERAND     ops_1[MaxOperandCount];
        DIS::OPERAND     ops_2[MaxOperandCount];

        // Zero out the locals, just in case.
        memset(&instr_1, 0, sizeof(instr_1));
        memset(&instr_2, 0, sizeof(instr_2));
        memset(&ops_1, 0, sizeof(ops_1));
        memset(&ops_2, 0, sizeof(ops_2));

        size_t instrSize_1 = disasm_1->CbDisassemble((DIS::ADDR)originalBlock1 + offset, (void*)(block1 + offset), 15);
        size_t instrSize_2 = disasm_2->CbDisassemble((DIS::ADDR)originalBlock2 + offset, (void*)(block2 + offset), 15);

        if (instrSize_1 != instrSize_2)
        {
            LogVerbose("Different instruction sizes %llu %llu", instrSize_1, instrSize_2);
            goto DumpDetails;
        }
        if (instrSize_1 == 0)
        {
            if (haveSeenRet)
            {
                // This logging is pretty noisy, so disable it.
                // LogVerbose("instruction size of zero after seeing a ret (soft issue?).");
                break;
            }
            LogWarning("instruction size of zero.");
            goto DumpDetails;
        }

        bool FDecodeError = false;
        if (!disasm_1->FDecode(&instr_1, ops_1, MaxOperandCount))
        {
            LogWarning("FDecode of instr_1 returned false.");
            FDecodeError = true;
        }
        if (!disasm_2->FDecode(&instr_2, ops_2, MaxOperandCount))
        {
            LogWarning("FDecode of instr_2 returned false.");
            FDecodeError = true;
        }

        WCHAR instrMnemonic_1[64]; // I never know how much to allocate...
        disasm_1->CchFormatInstr(instrMnemonic_1, 64);
        WCHAR instrMnemonic_2[64]; // I never know how much to allocate...
        disasm_2->CchFormatInstr(instrMnemonic_2, 64);
        if (wcscmp(instrMnemonic_1, L"ret") == 0)
            haveSeenRet = true;
        if (wcscmp(instrMnemonic_1, L"rep ret") == 0)
            haveSeenRet = true;

        // First, check to see if these instructions are actually identical.
        // This is done 1) to avoid the detailed comparison of the fields of instr_1
        // and instr_2 if they are identical, and 2) because in the event that
        // there are bugs or unimplemented instructions in FDecode, we don't want
        // to count them as diffs if they are bitwise identical.

        if (memcmp((block1 + offset), (block2 + offset), instrSize_1) != 0)
        {
            if (FDecodeError)
            {
                LogWarning("FDecode returned false.");
                goto DumpDetails;
            }

            if (instr_1.opa != instr_2.opa)
            {
                LogVerbose("different opa %d %d", instr_1.opa, instr_2.opa);
                goto DumpDetails;
            }
            if (instr_1.coperand != instr_2.coperand)
            {
                LogVerbose("different coperand %u %u", (unsigned int)instr_1.coperand, (unsigned int)instr_2.coperand);
                goto DumpDetails;
            }
            if (instr_1.dwModifiers != instr_2.dwModifiers)
            {
                LogVerbose("different dwModifiers %u %u", instr_1.dwModifiers, instr_2.dwModifiers);
                goto DumpDetails;
            }

            for (size_t i = 0; i < instr_1.coperand; i++)
            {
                if (ops_1[i].cb != ops_2[i].cb)
                {
                    LogVerbose("different cb  %llu %llu", ops_1[i].cb, ops_2[i].cb);
                    goto DumpDetails;
                }
                if (ops_1[i].imcls != ops_2[i].imcls)
                {
                    LogVerbose("different imcls %d %d", ops_1[i].imcls, ops_2[i].imcls);
                    goto DumpDetails;
                }
                if (ops_1[i].opcls != ops_2[i].opcls)
                {
                    LogVerbose("different opcls %d %d", ops_1[i].opcls, ops_2[i].opcls);
                    goto DumpDetails;
                }
                if (ops_1[i].rega1 != ops_2[i].rega1)
                {
                    LogVerbose("different rega1 %d %d", ops_1[i].rega1, ops_2[i].rega1);
                    goto DumpDetails;
                }
                if (ops_1[i].rega2 != ops_2[i].rega2)
                {
                    LogVerbose("different rega2 %d %d", ops_1[i].rega2, ops_2[i].rega2);
                    goto DumpDetails;
                }
                if (ops_1[i].rega3 != ops_2[i].rega3)
                {
                    LogVerbose("different rega3 %d %d", ops_1[i].rega3, ops_2[i].rega3);
                    goto DumpDetails;
                }
                if (ops_1[i].wScale != ops_2[i].wScale)
                {
                    LogVerbose("different wScale %u %u", ops_1[i].wScale, ops_2[i].wScale);
                    goto DumpDetails;
                }

                //
                // These are special.. we can often reason out exactly why these values
                // are different using heuristics.
                //
                // Why is Instruction size passed as zero?
                // Ans: Because the implementation of areOffsetsEquivalent() uses
                // the instruction size to compute absolute offsets in the case of
                // PC-relative addressing, and MSVCDis already reports the
                // absolute offsets! For example:
                // 0F 2E 05 67 00 9A FD ucomiss xmm0, dword ptr[FFFFFFFFFD9A006Eh]
                //

                if (compareOffsets(&data, offset, 0, ops_1[i].dwl, ops_2[i].dwl))
                {
                    continue;
                }
                else
                {
                    size_t gOffset1 = (size_t)originalBlock1 + offset + (size_t)ops_1[i].dwl;
                    size_t gOffset2 = (size_t)originalBlock2 + offset + (size_t)ops_2[i].dwl;

                    LogVerbose("operand %d dwl is different", i);
#ifdef TARGET_AMD64
                    LogVerbose("gOffset1 %016llX", gOffset1);
                    LogVerbose("gOffset2 %016llX", gOffset2);
                    LogVerbose("gOffset1 - gOffset2 %016llX", gOffset1 - gOffset2);
#elif defined(TARGET_X86)
                    LogVerbose("gOffset1 %08X", gOffset1);
                    LogVerbose("gOffset2 %08X", gOffset2);
                    LogVerbose("gOffset1 - gOffset2 %08X", gOffset1 - gOffset2);
#endif
                    LogVerbose("dwl1 %016llX", ops_1[i].dwl);
                    LogVerbose("dwl2 %016llX", ops_2[i].dwl);
                    goto DumpDetails;
                }
            }
        }
        offset += instrSize_1;
    }
    delete disasm_1;
    delete disasm_2;
    return true;

DumpDetails:
    LogVerbose("block1 %p", block1);
    LogVerbose("block2 %p", block2);
    LogVerbose("originalBlock1 [%p,%p)", originalBlock1, (const uint8_t*)originalBlock1 + blocksize1);
    LogVerbose("originalBlock2 [%p,%p)", originalBlock2, (const uint8_t*)originalBlock2 + blocksize2);
    LogVerbose("blocksize1 %08X", blocksize1);
    LogVerbose("blocksize2 %08X", blocksize2);
    LogVerbose("dataBlock1 [%p,%p)", originalDataBlock1, (const uint8_t*)originalDataBlock1 + datablockSize1);
    LogVerbose("dataBlock2 [%p,%p)", originalDataBlock2, (const uint8_t*)originalDataBlock2 + datablockSize2);
    LogVerbose("datablockSize1 %08X", datablockSize1);
    LogVerbose("datablockSize2 %08X", datablockSize2);
    LogVerbose("otherCodeBlock1 [%p,%p)", otherCodeBlock1, (const uint8_t*)otherCodeBlock1 + otherCodeBlockSize1);
    LogVerbose("otherCodeBlock2 [%p,%p)", otherCodeBlock2, (const uint8_t*)otherCodeBlock2 + otherCodeBlockSize2);
    LogVerbose("otherCodeBlockSize1 %08X", otherCodeBlockSize1);
    LogVerbose("otherCodeBlockSize2 %08X", otherCodeBlockSize2);

#ifdef TARGET_AMD64
    LogVerbose("offset %016llX", offset);
    LogVerbose("addr1 %016llX", (size_t)originalBlock1 + offset);
    LogVerbose("addr2 %016llX", (size_t)originalBlock2 + offset);
#elif defined(TARGET_X86)
    LogVerbose("offset %08X", offset);
    LogVerbose("addr1 %08X", (size_t)originalBlock1 + offset);
    LogVerbose("addr2 %08X", (size_t)originalBlock2 + offset);
#endif

    LogVerbose("Block1:");
    DumpCodeBlock(block1, blocksize1, originalBlock1);
    LogVerbose("Block2:");
    DumpCodeBlock(block2, blocksize2, originalBlock2);

    if (disasm_1 != nullptr)
        delete disasm_1;
    if (disasm_2 != nullptr)
        delete disasm_2;
    return false;
#else  // !USE_MSVCDIS
    return false; // No disassembler; assume there are differences
#endif // !USE_MSVCDIS
}

//
// Compares two read-only data sections for equality.
//
// Arguments:
//    mc                 - The method context of the method to diff.
//    cr1                - The first compile result to compare.
//    cr2                - The second compile result to compare.
//    block1             - A pointer to the first code block to disassemble.
//    blocksize1         - The size of the first code block to compare.
//    originalDataBlock1 - The original base address of the first read-only data block.
//    block2             - A pointer to the second code block to disassemble.
//    blocksize2         - The size of the second code block to compare.
//    originalDataBlock2 - The original base address of the second read-only data block.
//
// Return Value:
//    True if the read-only data sections are identical; false otherwise.
//
bool NearDiffer::compareReadOnlyDataBlock(MethodContext* mc,
                                          CompileResult* cr1,
                                          CompileResult* cr2,
                                          unsigned char* block1,
                                          ULONG          blocksize1,
                                          void*          originalDataBlock1,
                                          unsigned char* block2,
                                          ULONG          blocksize2,
                                          void*          originalDataBlock2)
{
    // no rodata
    if (blocksize1 == 0 && blocksize2 == 0)
        return true;

    if (blocksize1 != blocksize2)
    {
        LogVerbose("compareReadOnlyDataBlock found non-matching sizes %u %u", blocksize1, blocksize2);
        return false;
    }

    // TODO-Cleanup: The values on the datablock seem to wobble. Need further investigation to evaluate a good near
    // comparison for these
    return true;
}

//
// Compares two EH info blocks for equality.
//
// Arguments:
//    mc  - The method context of the method to diff.
//    cr1 - The first compile result to compare.
//    cr2 - The second compile result to compare.
//
// Return Value:
//    True if the EH info blocks are identical; false otherwise.
//
bool NearDiffer::compareEHInfo(MethodContext* mc, CompileResult* cr1, CompileResult* cr2)
{
    ULONG cEHSize_1;
    ULONG ehFlags_1;
    ULONG tryOffset_1;
    ULONG tryLength_1;
    ULONG handlerOffset_1;
    ULONG handlerLength_1;
    ULONG classToken_1;

    ULONG cEHSize_2;
    ULONG ehFlags_2;
    ULONG tryOffset_2;
    ULONG tryLength_2;
    ULONG handlerOffset_2;
    ULONG handlerLength_2;
    ULONG classToken_2;

    cEHSize_1 = cr1->repSetEHcount();
    cEHSize_2 = cr2->repSetEHcount();

    // no exception
    if (cEHSize_1 == 0 && cEHSize_2 == 0)
        return true;

    if (cEHSize_1 != cEHSize_2)
    {
        LogVerbose("compareEHInfo found non-matching sizes %u %u", cEHSize_1, cEHSize_2);
        return false;
    }

    for (unsigned int i = 0; i < cEHSize_1; i++)
    {
        cr1->repSetEHinfo(i, &ehFlags_1, &tryOffset_1, &tryLength_1, &handlerOffset_1, &handlerLength_1, &classToken_1);
        cr2->repSetEHinfo(i, &ehFlags_2, &tryOffset_2, &tryLength_2, &handlerOffset_2, &handlerLength_2, &classToken_2);
        if (ehFlags_1 != ehFlags_2)
        {
            LogVerbose("EH flags don't match %u != %u", ehFlags_1, ehFlags_2);
            return false;
        }
        if ((tryOffset_1 != tryOffset_2) || (tryLength_1 != tryLength_2))
        {
            LogVerbose("EH try information don't match, offset: %u %u, length: %u %u", tryOffset_1, tryOffset_2,
                       tryLength_1, tryLength_2);
            return false;
        }
        if ((handlerOffset_1 != handlerOffset_2) || (handlerLength_1 != handlerLength_2))
        {
            LogVerbose("EH handler information don't match, offset: %u %u, length: %u %u", handlerOffset_1,
                       handlerOffset_2, handlerLength_1, handlerLength_2);
            return false;
        }
        if (classToken_1 != classToken_2)
        {
            LogVerbose("EH class tokens don't match %u!=%u", classToken_1, classToken_2);
            return false;
        }
    }

    return true;
}

//
// Compares two GC info blocks for equality.
//
// Arguments:
//    mc  - The method context of the method to diff.
//    cr1 - The first compile result to compare.
//    cr2 - The second compile result to compare.
//
// Return Value:
//    True if the GC info blocks are identical; false otherwise.
//
bool NearDiffer::compareGCInfo(MethodContext* mc, CompileResult* cr1, CompileResult* cr2)
{
    void*  gcInfo1;
    size_t gcInfo1Size;
    void*  gcInfo2;
    size_t gcInfo2Size;

    cr1->repAllocGCInfo(&gcInfo1Size, &gcInfo1);
    cr2->repAllocGCInfo(&gcInfo2Size, &gcInfo2);

    if (gcInfo1Size != gcInfo2Size)
    {
        LogVerbose("Reported GCInfo sizes don't match: %u != %u", (unsigned int)gcInfo1Size, (unsigned int)gcInfo2Size);
        return false;
    }

    if (memcmp(gcInfo1, gcInfo2, gcInfo1Size) != 0)
    {
        LogVerbose("GCInfo doesn't match.");
        return false;
    }

    return true;
}

//
// Compares two sets of native var info for equality.
//
// Arguments:
//    mc  - The method context of the method to diff.
//    cr1 - The first compile result to compare.
//    cr2 - The second compile result to compare.
//
// Return Value:
//    True if the native var info is identical; false otherwise.
//
bool NearDiffer::compareVars(MethodContext* mc, CompileResult* cr1, CompileResult* cr2)
{
    CORINFO_METHOD_HANDLE         ftn_1;
    ULONG32                       cVars_1;
    ICorDebugInfo::NativeVarInfo* vars_1;

    CORINFO_METHOD_HANDLE         ftn_2;
    ULONG32                       cVars_2;
    ICorDebugInfo::NativeVarInfo* vars_2;

    CORINFO_METHOD_INFO info;
    unsigned            flags = 0;
    CORINFO_OS          os    = CORINFO_WINNT;
    mc->repCompileMethod(&info, &flags, &os);

    bool set1 = cr1->repSetVars(&ftn_1, &cVars_1, &vars_1);
    bool set2 = cr2->repSetVars(&ftn_2, &cVars_2, &vars_2);
    if ((set1 == false) && (set2 == false))
        return true; // we don't have boundaries for either of these.
    if (((set1 == true) && (set2 == false)) || ((set1 == false) && (set2 == true)))
    {
        LogVerbose("missing matching vars sets");
        return false;
    }

    // no vars
    if (cVars_1 == 0 && cVars_2 == 0)
    {
        return true;
    }

    if (ftn_1 != ftn_2)
    {
        // We would like to find out this situation
        __debugbreak();
        LogVerbose("compareVars found non-matching CORINFO_METHOD_HANDLE %p %p", ftn_1, ftn_2);
        return false;
    }
    if (ftn_1 != info.ftn)
    {
        LogVerbose("compareVars found issues with the CORINFO_METHOD_HANDLE %p %p", ftn_1, info.ftn);
        return false;
    }

    if (cVars_1 != cVars_2)
    {
        LogVerbose("compareVars found non-matching var count %u %u", cVars_1, cVars_2);
        return false;
    }

    // TODO-Cleanup: The values on the NativeVarInfo array seem to wobble. Need further investigation to evaluate a good
    // near comparison for these for(unsigned int i=0;i<cVars_1;i++)
    //{
    //    if(vars_1[i].startOffset!=vars_2[i].startOffset)
    //    {
    //        LogVerbose("compareVars found non-matching startOffsets %u %u for var: %u", vars_1[i].startOffset,
    //        vars_2[i].startOffset, i); return false;
    //    }
    //}

    return true;
}

//
// Compares two sets of native offset mappings for equality.
//
// Arguments:
//    mc  - The method context of the method to diff.
//    cr1 - The first compile result to compare.
//    cr2 - The second compile result to compare.
//
// Return Value:
//    True if the native offset mappings are identical; false otherwise.
//
bool NearDiffer::compareBoundaries(MethodContext* mc, CompileResult* cr1, CompileResult* cr2)
{
    CORINFO_METHOD_HANDLE         ftn_1;
    ULONG32                       cMap_1;
    ICorDebugInfo::OffsetMapping* map_1;

    CORINFO_METHOD_HANDLE         ftn_2;
    ULONG32                       cMap_2;
    ICorDebugInfo::OffsetMapping* map_2;

    CORINFO_METHOD_INFO info;
    unsigned            flags = 0;
    CORINFO_OS          os    = CORINFO_WINNT;
    mc->repCompileMethod(&info, &flags, &os);

    bool set1 = cr1->repSetBoundaries(&ftn_1, &cMap_1, &map_1);
    bool set2 = cr2->repSetBoundaries(&ftn_2, &cMap_2, &map_2);
    if ((set1 == false) && (set2 == false))
        return true; // we don't have boundaries for either of these.
    if (((set1 == true) && (set2 == false)) || ((set1 == false) && (set2 == true)))
    {
        LogVerbose("missing matching boundary sets");
        return false;
    }

    if (ftn_1 != ftn_2)
    {
        LogVerbose("compareBoundaries found non-matching CORINFO_METHOD_HANDLE %p %p", ftn_1, ftn_2);
        return false;
    }

    // no maps
    if (cMap_1 == 0 && cMap_2 == 0)
        return true;

    if (cMap_1 != cMap_2)
    {
        LogVerbose("compareBoundaries found non-matching var count %u %u", cMap_1, cMap_2);
        return false;
    }

    for (unsigned int i = 0; i < cMap_1; i++)
    {
        if (map_1[i].ilOffset != map_2[i].ilOffset)
        {
            LogVerbose("compareBoundaries found non-matching ilOffset %u %u for map: %u", map_1[i].ilOffset,
                       map_2[i].ilOffset, i);
            return false;
        }
        if (map_1[i].nativeOffset != map_2[i].nativeOffset)
        {
            LogVerbose("compareBoundaries found non-matching nativeOffset %u %u for map: %u", map_1[i].nativeOffset,
                       map_2[i].nativeOffset, i);
            return false;
        }
        if (map_1[i].source != map_2[i].source)
        {
            LogVerbose("compareBoundaries found non-matching source %u %u for map: %u", (unsigned int)map_1[i].source,
                       (unsigned int)map_2[i].source, i);
            return false;
        }
    }

    return true;
}

//
// Compares two compiled versions of a method for equality. This is the main driver for the various
// components of near diffing.
//
// Before starting the diffing process, this applies some fixups to the code stream based on relocations
// recorded during compilation, using the original base address that was used when compiling the method.
//
// Arguments:
//    mc  - The method context of the method to diff.
//    cr1 - The first compile result to compare.
//    cr2 - The second compile result to compare.
//
// Return Value:
//    True if the compile results are identical; false otherwise.
//
bool NearDiffer::compare(MethodContext* mc, CompileResult* cr1, CompileResult* cr2)
{
    ULONG              hotCodeSize_1;
    ULONG              coldCodeSize_1;
    ULONG              roDataSize_1;
    ULONG              xcptnsCount_1;
    CorJitAllocMemFlag flag_1;
    unsigned char*     hotCodeBlock_1;
    unsigned char*     coldCodeBlock_1;
    unsigned char*     roDataBlock_1;
    void*              orig_hotCodeBlock_1;
    void*              orig_coldCodeBlock_1;
    void*              orig_roDataBlock_1;

    ULONG              hotCodeSize_2;
    ULONG              coldCodeSize_2;
    ULONG              roDataSize_2;
    ULONG              xcptnsCount_2;
    CorJitAllocMemFlag flag_2;
    unsigned char*     hotCodeBlock_2;
    unsigned char*     coldCodeBlock_2;
    unsigned char*     roDataBlock_2;
    void*              orig_hotCodeBlock_2;
    void*              orig_coldCodeBlock_2;
    void*              orig_roDataBlock_2;

    cr1->repAllocMem(&hotCodeSize_1, &coldCodeSize_1, &roDataSize_1, &xcptnsCount_1, &flag_1, &hotCodeBlock_1,
                     &coldCodeBlock_1, &roDataBlock_1, &orig_hotCodeBlock_1, &orig_coldCodeBlock_1,
                     &orig_roDataBlock_1);
    cr2->repAllocMem(&hotCodeSize_2, &coldCodeSize_2, &roDataSize_2, &xcptnsCount_2, &flag_2, &hotCodeBlock_2,
                     &coldCodeBlock_2, &roDataBlock_2, &orig_hotCodeBlock_2, &orig_coldCodeBlock_2,
                     &orig_roDataBlock_2);

    // On Arm64 the constant pool is appended at the end of the method code section, hence hotCodeSize_{1,2}
    // is a sum of their sizes. The following is to adjust their sizes and the roDataBlock_{1,2} pointers.
    if (GetSpmiTargetArchitecture() == SPMI_TARGET_ARCHITECTURE_ARM64)
    {
        BYTE*        nativeEntry_1;
        ULONG        nativeSizeOfCode_1;
        CorJitResult jitResult_1;

        BYTE*        nativeEntry_2;
        ULONG        nativeSizeOfCode_2;
        CorJitResult jitResult_2;

        cr1->repCompileMethod(&nativeEntry_1, &nativeSizeOfCode_1, &jitResult_1);
        cr2->repCompileMethod(&nativeEntry_2, &nativeSizeOfCode_2, &jitResult_2);

        roDataSize_1 = hotCodeSize_1 - nativeSizeOfCode_1;
        roDataSize_2 = hotCodeSize_2 - nativeSizeOfCode_2;

        roDataBlock_1 = hotCodeBlock_1 + nativeSizeOfCode_1;
        roDataBlock_2 = hotCodeBlock_2 + nativeSizeOfCode_2;

        orig_roDataBlock_1 = (void*)((size_t)orig_hotCodeBlock_1 + nativeSizeOfCode_1);
        orig_roDataBlock_2 = (void*)((size_t)orig_hotCodeBlock_2 + nativeSizeOfCode_2);

        hotCodeSize_1 = nativeSizeOfCode_1;
        hotCodeSize_2 = nativeSizeOfCode_2;
    }

    LogDebug("HCS1 %d CCS1 %d RDS1 %d xcpnt1 %d flag1 %08X, HCB %p CCB %p RDB %p ohcb %p occb %p odb %p", hotCodeSize_1,
             coldCodeSize_1, roDataSize_1, xcptnsCount_1, flag_1, hotCodeBlock_1, coldCodeBlock_1, roDataBlock_1,
             orig_hotCodeBlock_1, orig_coldCodeBlock_1, orig_roDataBlock_1);
    LogDebug("HCS2 %d CCS2 %d RDS2 %d xcpnt2 %d flag2 %08X, HCB %p CCB %p RDB %p ohcb %p occb %p odb %p", hotCodeSize_2,
             coldCodeSize_2, roDataSize_2, xcptnsCount_2, flag_2, hotCodeBlock_2, coldCodeBlock_2, roDataBlock_2,
             orig_hotCodeBlock_2, orig_coldCodeBlock_2, orig_roDataBlock_2);

    RelocContext rc;
    rc.mc                      = mc;

    rc.hotCodeAddress          = (size_t)hotCodeBlock_1;
    rc.hotCodeSize             = hotCodeSize_1;
    rc.coldCodeAddress         = (size_t)coldCodeBlock_1;
    rc.coldCodeSize            = coldCodeSize_1;
    rc.roDataAddress           = (size_t)roDataBlock_1;
    rc.roDataSize              = roDataSize_1;
    rc.originalHotCodeAddress  = (size_t)orig_hotCodeBlock_1;
    rc.originalColdCodeAddress = (size_t)orig_coldCodeBlock_1;
    rc.originalRoDataAddress   = (size_t)orig_roDataBlock_1;

    cr1->applyRelocs(&rc, hotCodeBlock_1, hotCodeSize_1, orig_hotCodeBlock_1);
    cr1->applyRelocs(&rc, coldCodeBlock_1, coldCodeSize_1, orig_coldCodeBlock_1);
    cr1->applyRelocs(&rc, roDataBlock_1, roDataSize_1, orig_roDataBlock_1);

    rc.hotCodeAddress          = (size_t)hotCodeBlock_2;
    rc.hotCodeSize             = hotCodeSize_2;
    rc.coldCodeAddress         = (size_t)coldCodeBlock_2;
    rc.coldCodeSize            = coldCodeSize_2;
    rc.roDataAddress           = (size_t)roDataBlock_2;
    rc.roDataSize              = roDataSize_2;
    rc.originalHotCodeAddress  = (size_t)orig_hotCodeBlock_2;
    rc.originalColdCodeAddress = (size_t)orig_coldCodeBlock_2;
    rc.originalRoDataAddress   = (size_t)orig_roDataBlock_2;

    cr2->applyRelocs(&rc, hotCodeBlock_2, hotCodeSize_2, orig_hotCodeBlock_2);
    cr2->applyRelocs(&rc, coldCodeBlock_2, coldCodeSize_2, orig_coldCodeBlock_2);
    cr2->applyRelocs(&rc, roDataBlock_2, roDataSize_2, orig_roDataBlock_2);

    if (!compareCodeSection(mc, cr1, cr2, hotCodeBlock_1, hotCodeSize_1, roDataBlock_1, roDataSize_1,
                            orig_hotCodeBlock_1, orig_roDataBlock_1, orig_coldCodeBlock_1, coldCodeSize_1,
                            hotCodeBlock_2, hotCodeSize_2, roDataBlock_2, roDataSize_2, orig_hotCodeBlock_2,
                            orig_roDataBlock_2, orig_coldCodeBlock_2, coldCodeSize_2))
        return false;

    if (!compareCodeSection(mc, cr1, cr2, coldCodeBlock_1, coldCodeSize_1, roDataBlock_1, roDataSize_1,
                            orig_coldCodeBlock_1, orig_roDataBlock_1, orig_hotCodeBlock_1, hotCodeSize_1,
                            coldCodeBlock_2, coldCodeSize_2, roDataBlock_2, roDataSize_2, orig_coldCodeBlock_2,
                            orig_roDataBlock_2, orig_hotCodeBlock_2, hotCodeSize_2))
        return false;

    if (!compareReadOnlyDataBlock(mc, cr1, cr2, roDataBlock_1, roDataSize_1, orig_roDataBlock_1, roDataBlock_2,
                                  roDataSize_2, orig_roDataBlock_2))
        return false;

    if (!compareEHInfo(mc, cr1, cr2))
        return false;

    if (!compareGCInfo(mc, cr1, cr2))
        return false;

    if (!compareVars(mc, cr1, cr2))
        return false;

    if (!compareBoundaries(mc, cr1, cr2))
        return false;

    return true;
}
