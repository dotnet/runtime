// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DEBUGGER_COMMON_H
#define DEBUGGER_COMMON_H

//
// Conversions between pointers and CORDB_ADDRESS
// These are 3gb safe - we use zero-extension for CORDB_ADDRESS.
// Note that this is a different semantics from CLRDATA_ADDRESS which is sign-extended.
//
// @dbgtodo : This confuses the host and target address spaces.  Ideally we'd have
// conversions between PTR types (eg. DPTR) and CORDB_ADDRESS, and not need conversions
// from host pointer types to CORDB_ADDRESS.
//
#if defined(TARGET_X86) || defined(TARGET_ARM)
inline CORDB_ADDRESS PTR_TO_CORDB_ADDRESS(const void* ptr)
{
    SUPPORTS_DAC;
    // Cast a void* to a ULONG is not 64-bit safe and triggers compiler warning C3411.
    // But this is x86 only, so we know it's ok. Use PtrToUlong to do the conversion
    // without invoking the error.
    return (CORDB_ADDRESS)(PtrToUlong(ptr));
}
inline CORDB_ADDRESS PTR_TO_CORDB_ADDRESS(UINT_PTR ptr)
{
    SUPPORTS_DAC;
    // PtrToUlong
    return (CORDB_ADDRESS)(ULONG)(ptr);
}
#else
#define PTR_TO_CORDB_ADDRESS(_ptr) (CORDB_ADDRESS)(ULONG_PTR)(_ptr)
#endif //TARGET_X86 || TARGET_ARM

#define CORDB_ADDRESS_TO_PTR(_cordb_addr) ((LPVOID)(SIZE_T)(_cordb_addr))


// Determine if an exception record is for a CLR debug event, and get the payload.
CORDB_ADDRESS IsEventDebuggerNotification(const EXCEPTION_RECORD * pRecord, CORDB_ADDRESS pClrBaseAddress);
#if defined(FEATURE_DBGIPC_TRANSPORT_DI) || defined(FEATURE_DBGIPC_TRANSPORT_VM)
struct DebuggerIPCEvent;
void InitEventForDebuggerNotification(DEBUG_EVENT *      pDebugEvent,
                                      CORDB_ADDRESS      pClrBaseAddress,
                                      DebuggerIPCEvent * pIPCEvent);
#endif // (FEATURE_DBGIPC_TRANSPORT_DI || FEATURE_DBGIPC_TRANSPORT_VM)


void GetPidDecoratedName(_Out_writes_z_(cBufSizeInChars) WCHAR * pBuf,
                         int cBufSizeInChars,
                         const WCHAR * pPrefix,
                         DWORD pid);


//
// This macro is used in CORDbgCopyThreadContext().
//
// CORDbgCopyThreadContext() does an intelligent copy
// from pSrc to pDst, respecting the ContextFlags of both contexts.
//
#define CopyContextChunk(_t, _f, _end, _flag)                                  \
{                                                                              \
    LOG((LF_CORDB, LL_INFO1000000,                                             \
         "CP::CTC: copying " #_flag  ":" FMT_ADDR "<---" FMT_ADDR "(%d)\n",    \
         DBG_ADDR(_t), DBG_ADDR(_f), ((UINT_PTR)(_end) - (UINT_PTR)_t)));      \
    memcpy((_t), (_f), ((UINT_PTR)(_end) - (UINT_PTR)(_t)));                     \
}

//
// CORDbgCopyThreadContext() does an intelligent copy from pSrc to pDst,
// respecting the ContextFlags of both contexts.
//
struct DebuggerREGDISPLAY;

extern void CORDbgCopyThreadContext(DT_CONTEXT* pDst, const DT_CONTEXT* pSrc);
extern void CORDbgSetDebuggerREGDISPLAYFromContext(DebuggerREGDISPLAY *pDRD,
                                                   DT_CONTEXT* pContext);

//---------------------------------------------------------------------------------------
//
// Return the size of the CONTEXT required for the specified context flags.
//
// Arguments:
//    flags - this is the equivalent of the ContextFlags field of a CONTEXT
//
// Return Value:
//    size of the CONTEXT required
//
// Notes:
//    On WIN64 platforms this function will always return sizeof(CONTEXT).
//

inline
ULONG32 ContextSizeForFlags(ULONG32 flags)
{
#if defined(CONTEXT_EXTENDED_REGISTERS) && defined(TARGET_X86)
    // Older platforms didn't have extended registers in
    // the context definition so only enforce that size
    // if the extended register flag is set.
    if ((flags & CONTEXT_EXTENDED_REGISTERS) != CONTEXT_EXTENDED_REGISTERS)
    {
        return offsetof(T_CONTEXT, ExtendedRegisters);
    }
    else
#endif // TARGET_X86
    {
        return sizeof(T_CONTEXT);
    }
}

//---------------------------------------------------------------------------------------
//
// Given the size of a buffer and the context flags, check whether the buffer is sufficient large
// to hold the CONTEXT.
//
// Arguments:
//    size  - size of a buffer
//    flags - this is the equivalent of the ContextFlags field of a CONTEXT
//
// Return Value:
//    TRUE if the buffer is large enough to hold the CONTEXT
//

inline
BOOL CheckContextSizeForFlags(ULONG32 size, ULONG32 flags)
{
    return (size >= ContextSizeForFlags(flags));
}

//---------------------------------------------------------------------------------------
//
// Given the size of a buffer and the BYTE array representation of a CONTEXT,
// check whether the buffer is sufficient large to hold the CONTEXT.
//
// Arguments:
//    size  - size of a buffer
//    flags - this is the equivalent of the ContextFlags field of a CONTEXT
//
// Return Value:
//    TRUE if the buffer is large enough to hold the CONTEXT
//

inline
BOOL CheckContextSizeForBuffer(ULONG32 size, const BYTE * pbBuffer)
{
    return ( ( size >= (offsetof(T_CONTEXT, ContextFlags) + sizeof(ULONG32)) ) &&
             CheckContextSizeForFlags(size, (reinterpret_cast<const T_CONTEXT *>(pbBuffer))->ContextFlags) );
}

/* ------------------------------------------------------------------------- *
 * Constant declarations
 * ------------------------------------------------------------------------- */

enum
{
    NULL_THREAD_ID = -1,
    NULL_PROCESS_ID = -1
};

/* ------------------------------------------------------------------------- *
 * Macros
 * ------------------------------------------------------------------------- */

//
// CANNOT USE IsBad*Ptr() methods here.  They are *banned* APIs because of various
// reasons (see http://winweb/wincet/bannedapis.htm).
//

#define VALIDATE_POINTER_TO_OBJECT(ptr, type)                                \
if ((ptr) == NULL)                                                           \
{                                                                            \
    return E_INVALIDARG;                                                     \
}

#define VALIDATE_POINTER_TO_OBJECT_OR_NULL(ptr, type)

//
// CANNOT USE IsBad*Ptr() methods here.  They are *banned* APIs because of various
// reasons (see http://winweb/wincet/bannedapis.htm).
//
#define VALIDATE_POINTER_TO_OBJECT_ARRAY(ptr, type, cElt, fRead, fWrite)     \
if ((ptr) == NULL)                                                           \
{                                                                            \
    return E_INVALIDARG;                                                     \
}

#define VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(ptr, type,cElt,fRead,fWrite)

/* ------------------------------------------------------------------------- *
 * Function Prototypes
 * ------------------------------------------------------------------------- */



// Linear search through an array of NativeVarInfos, to find
// the variable of index dwIndex, valid at the given ip.
//
// returns CORDBG_E_IL_VAR_NOT_AVAILABLE if the variable isn't
// valid at the given ip.
//
// This should be inlined
HRESULT FindNativeInfoInILVariableArray(DWORD dwIndex,
                                        SIZE_T ip,
                                        ICorDebugInfo::NativeVarInfo **ppNativeInfo,
                                        unsigned int nativeInfoCount,
                                        ICorDebugInfo::NativeVarInfo *nativeInfo);

//  struct DebuggerILToNativeMap:   Holds the IL to Native offset map
//  Great pains are taken to ensure that this each entry corresponds to the
//  first IL instruction in a source line.  It isn't actually a mapping
//  of _every_ IL instruction in a method, just those for source lines.
//  SIZE_T ilOffset:  IL offset of a source line.
//  SIZE_T nativeStartOffset:  Offset within the method where the native
//      instructions corresponding to the IL offset begin.
//  SIZE_T nativeEndOffset:  Offset within the method where the native
//      instructions corresponding to the IL offset end.
//
//  Note: any changes to this struct need to be reflected in
//  COR_DEBUG_IL_TO_NATIVE_MAP in CorDebug.idl. These structs must
//  match exactly.
//
struct DebuggerILToNativeMap
{
    ULONG ilOffset;
    ULONG nativeStartOffset;
    ULONG nativeEndOffset;
    ICorDebugInfo::SourceTypes source;
};

void ExportILToNativeMap(ULONG32 cMap,
             COR_DEBUG_IL_TO_NATIVE_MAP mapExt[],
             struct DebuggerILToNativeMap mapInt[],
             SIZE_T sizeOfCode);

#include <primitives.h>

// ----------------------------------------------------------------------------
// IsPatchInRequestedRange
//
// Description:
//    This function checks if a patch falls (fully or partially) in the requested range of memory.
//
// Arguments:
//    * requestedAddr - the address of the memory range
//    * requestedSize - the size of the memory range
//    * patchAddr     - the address of the patch
//    * pPRD          - the opcode of the patch
//
// Return Value:
//    Return TRUE if the patch is fully or partially in the requested memory range.
//
// Notes:
//    Currently this function is called both from the RS (via code:CordbProcess.ReadMemory and
//    code:CordbProcess.WriteMemory) and from DAC.  When we DACize the two functions mentioned above,
//    this function should be called from DAC only, and we should use a MemoryRange here.
//

inline bool IsPatchInRequestedRange(CORDB_ADDRESS requestedAddr,
                                    SIZE_T requestedSize,
                                    CORDB_ADDRESS patchAddr)
{
    SUPPORTS_DAC;

    if (requestedAddr == 0)
        return false;

    // Note that patchEnd points to the byte immediately AFTER the patch, so patchEnd is NOT
    // part of the patch.
    CORDB_ADDRESS patchEnd = GetPatchEndAddr(patchAddr);

    // We have three cases:
    // 1) the entire patch is in the requested range
    // 2) the beginning of the requested range is covered by the patch
    // 3) the end of the requested range is covered by the patch
    //
    // Note that on x86, since the break instruction only takes up one byte, the following condition
    // degenerates to case 1 only.
    return (((requestedAddr <= patchAddr) && (patchEnd <= (requestedAddr + requestedSize))) ||
            ((patchAddr <= requestedAddr) && (requestedAddr < patchEnd)) ||
            ((patchAddr <= (requestedAddr + requestedSize - 1)) && ((requestedAddr + requestedSize - 1) < patchEnd)));
}

inline CORDB_ADDRESS ALIGN_ADDRESS( CORDB_ADDRESS val, CORDB_ADDRESS alignment )
{
    LIMITED_METHOD_DAC_CONTRACT;

    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    _ASSERTE( 0 == (alignment & (alignment - 1)) );
    CORDB_ADDRESS result = (val + (alignment - 1)) & ~(alignment - 1);
    _ASSERTE( result >= val );      // check for overflow
    return result;
}

#include "dacprivate.h" // for MSLAYOUT
#include "dumpcommon.h"

#endif //DEBUGGER_COMMON_H
