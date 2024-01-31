// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          DisAsm                                           XX
XX                                                                           XX
XX  The "late disassembler" to display the native code generated             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
#ifndef _DIS_H_
#define _DIS_H_
/*****************************************************************************/
#ifdef LATE_DISASM

#ifdef USE_COREDISTOOLS
#include "coredistools.h"
#endif // USE_COREDISTOOLS

#ifdef USE_MSVCDIS

// free() is deprecated (we should only allocate and free memory through CLR hosting interfaces)
// and is redefined in clrhost.h to cause a compiler error.
// We don't call free(), but this function is mentioned in STL headers included by msvcdis.h
// (and free() is only called by STL functions that we don't use).
// To avoid the compiler error, but at the same time ensure that we don't accidentally use free(),
// free() is redefined to cause a runtime error instead of a compile time error.
#undef free
#ifdef DEBUG
#define free(x) assert(false && "Must not call free(). Use a ClrXXX function instead.")
#endif

#if CHECK_STRUCT_PADDING
#pragma warning(pop)
#endif // CHECK_STRUCT_PADDING

#define _OLD_IOSTREAMS
// This pragma is needed because public\vc\inc\xiosbase contains
// a static local variable
#pragma warning(disable : 4640)
#include "msvcdis.h"
#pragma warning(default : 4640)

#ifdef TARGET_XARCH
#include "disx86.h"
#elif defined(TARGET_ARM64)
#include "disarm64.h"
#else // TARGET*
#error Unsupported or unset target architecture
#endif

#if CHECK_STRUCT_PADDING
#pragma warning(push)
#pragma warning(default : 4820) // 'bytes' bytes padding added after construct 'member_name'
#endif                          // CHECK_STRUCT_PADDING

/*****************************************************************************/

#endif // USE_MSVCDIS

#ifdef HOST_64BIT
template <typename T>
struct SizeTKeyFuncs : JitLargePrimitiveKeyFuncs<T>
{
};
#else  // !HOST_64BIT
template <typename T>
struct SizeTKeyFuncs : JitSmallPrimitiveKeyFuncs<T>
{
};
#endif // HOST_64BIT

typedef JitHashTable<size_t, SizeTKeyFuncs<size_t>, CORINFO_METHOD_HANDLE> AddrToMethodHandleMap;
typedef JitHashTable<size_t, SizeTKeyFuncs<size_t>, size_t>                AddrToAddrMap;

class Compiler;

class DisAssembler
{
public:
    // Constructor
    void disInit(Compiler* pComp);

    // Destructor
    void disDone();

    // Initialize the class for the current method being generated.
    void disOpenForLateDisAsm(const char* curMethodName, const char* curClassName, PCCOR_SIGNATURE sig);

    // Disassemble a buffer: called after code for a method is generated.
    void disAsmCode(BYTE*  hotCodePtr,
                    BYTE*  hotCodePtrRW,
                    size_t hotCodeSize,
                    BYTE*  coldCodePtr,
                    BYTE*  coldCodePtrRW,
                    size_t coldCodeSize);

    // Register an address to be associated with a method handle.
    void disSetMethod(size_t addr, CORINFO_METHOD_HANDLE methHnd);

    // Register a relocation address.
    void disRecordRelocation(size_t relocAddr, size_t targetAddr);

private:
    /* Address of the hot and cold code blocks to disassemble */
    size_t disHotCodeBlock;
    size_t disColdCodeBlock;

    /* Size of the hot and cold code blocks to disassemble */
    size_t disHotCodeSize;
    size_t disColdCodeSize;

    /* Total code size (simply cached version of disHotCodeSize + disColdCodeSize) */
    size_t disTotalCodeSize;

    /* Address where the code block is to be loaded */
    size_t disStartAddr;

    /* Current offset in the code block */
    size_t disCurOffset;

    /* Size (in bytes) of current disassembled instruction */
    size_t disInstSize;

    /* Target address of a jump */
    size_t disTarget;

    /* temporary buffer for function names */
    // TODO-Review: there is some issue here where this is never set!
    char disFuncTempBuf[1024];

    /* Method and class name to output */
    const char* disCurMethodName;
    const char* disCurClassName;

    /* flag that signals when replacing a symbol name has been deferred for following callbacks */
    // TODO-Review: there is some issue here where this is never set to 'true'!
    bool disHasName;

    /* An array of labels, for jumps, LEAs, etc. There is one element in the array for each byte in the generated code.
     * That byte is zero if the corresponding byte of generated code is not a label. Otherwise, the value
     * is a label number.
     */
    BYTE* disLabels;

    void DisasmBuffer(FILE* pfile, bool printit);

    /* For the purposes of disassembly, we pretend that the hot and cold sections are linear, and not split.
     * These functions create this model for the rest of the disassembly code.
     */

    /* Given a linear offset into the code, find a pointer to the actual code (either in the hot or cold section) */
    const BYTE* disGetLinearAddr(size_t offset);

    /* Given a linear offset into the code, determine how many bytes are left in the hot or cold buffer the offset
     * points to */
    size_t disGetBufferSize(size_t offset);

    // Map of instruction addresses to call target method handles for normal calls.
    AddrToMethodHandleMap* disAddrToMethodHandleMap;
    AddrToMethodHandleMap* GetAddrToMethodHandleMap();

    // Map of instruction addresses to call target method handles for JIT helper calls.
    AddrToMethodHandleMap* disHelperAddrToMethodHandleMap;
    AddrToMethodHandleMap* GetHelperAddrToMethodHandleMap();

    // Map of relocation addresses to relocation target.
    AddrToAddrMap* disRelocationMap;
    AddrToAddrMap* GetRelocationMap();

    const char* disGetMethodFullName(size_t addr);

    FILE* disAsmFile;

    Compiler* disComp;

    bool disDiffable; // 'true' if the output should be diffable (hide or obscure absolute addresses)

    template <typename T>
    T dspAddr(T addr)
    {
// silence warning of cast to greater size. It is easier to silence than construct code the compiler is happy with, and
// it is safe in this case
#pragma warning(push)
#pragma warning(disable : 4312)
        return (addr == 0) ? 0 : (disDiffable ? T(0xD1FFAB1E) : addr);
#pragma warning(pop)
    }

#ifdef USE_MSVCDIS

    /* Callbacks from msdis */

    static size_t __stdcall disCchAddr(
        const DIS* pdis, DIS::ADDR addr, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORDLONG* pdwDisp);

    size_t disCchAddrMember(
        const DIS* pdis, DIS::ADDR addr, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORDLONG* pdwDisp);

    static size_t __stdcall disCchFixup(const DIS*                  pdis,
                                        DIS::ADDR                   addr,
                                        size_t                      size,
                                        _In_reads_(cchMax) wchar_t* wz,
                                        size_t                      cchMax,
                                        DWORDLONG*                  pdwDisp);

    size_t disCchFixupMember(const DIS*                  pdis,
                             DIS::ADDR                   addr,
                             size_t                      size,
                             _In_reads_(cchMax) wchar_t* wz,
                             size_t                      cchMax,
                             DWORDLONG*                  pdwDisp);

    static size_t __stdcall disCchRegRel(
        const DIS* pdis, DIS::REGA reg, DWORD disp, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORD* pdwDisp);

    size_t disCchRegRelMember(
        const DIS* pdis, DIS::REGA reg, DWORD disp, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORD* pdwDisp);

    static size_t __stdcall disCchReg(const DIS* pdis, DIS::REGA reg, _In_reads_(cchMax) wchar_t* wz, size_t cchMax);

    size_t disCchRegMember(const DIS* pdis, DIS::REGA reg, _In_reads_(cchMax) wchar_t* wz, size_t cchMax);

    /* Disassemble helper */

    size_t CbDisassemble(DIS*        pdis,
                         size_t      offs,
                         DIS::ADDR   addr,
                         const BYTE* pb,
                         size_t      cbMax,
                         FILE*       pfile,
                         bool        findLabels,
                         bool        printit       = false,
                         bool        dispOffs      = false,
                         bool        dispCodeBytes = false);

#endif // USE_MSVCDIS

#ifdef USE_COREDISTOOLS

    bool                      InitCoredistoolsLibrary();            // Load the coredistools library
    static LONG               s_disCoreDisToolsLibraryInitializing; // 0 = not initializing; 1 = initializing
    static bool               s_disCoreDisToolsLibraryInitialized;
    static bool               s_disCoreDisToolsLibraryLoadSuccessful;
    static NewDisasm_t*       s_PtrNewDisasm;
    static DumpInstruction_t* s_PtrDumpInstruction;
    static FinishDisasm_t*    s_PtrFinishDisasm;

    bool       InitCoredistoolsDisasm(); // Prepare for disassembly
    void       DoneCoredistoolsDisasm(); // Done with disassembly
    CorDisasm* corDisasm;

#endif // USE_COREDISTOOLS
};

/*****************************************************************************/
#endif // LATE_DISASM
/*****************************************************************************/
#endif // _DIS_H_
/*****************************************************************************/
