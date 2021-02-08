// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Type-safe helper wrapper to get an EXCEPTION_RECORD slot as a CORDB_ADDRESS
//
// Arguments:
//    pRecord - exception record
//    idxSlot - slot to retrieve from.
//
// Returns:
//    contents of slot as a CordbAddress.
CORDB_ADDRESS GetExceptionInfoAsAddress(const EXCEPTION_RECORD * pRecord, int idxSlot)
{
    _ASSERTE((idxSlot >= 0) && (idxSlot < EXCEPTION_MAXIMUM_PARAMETERS));

    // ExceptionInformation is an array of ULONG_PTR.  CORDB_ADDRESS is a 0-extended ULONG64.
    // So the implicit cast will work here on x86.  On 64-bit, it's basically a nop.
    return pRecord->ExceptionInformation[idxSlot];
}


// Determine if an exception event is a Debug event for this flavor of the CLR.
//
// Arguments:
//    pRecord - exception record
//    pClrBaseAddress - clr Instance ID for which CLR in the target we're checking against.
//
// Returns:
//    NULL if the exception is not a CLR managed debug event for the given Clr instance.
//    Else, address in target process of managed debug event described by the exception (the payload).
//
// Notes:
//    This decodes events raised by code:Debugger.SendRawEvent
//    Anybody can spoof our exception, so this is not a reliably safe method.
//    With multiple CLRs in the same process, it's essential to use the proper pClrBaseAddress.
CORDB_ADDRESS IsEventDebuggerNotification(
    const EXCEPTION_RECORD * pRecord,
    CORDB_ADDRESS pClrBaseAddress
    )
{
    _ASSERTE(pRecord != NULL);

    // Must specify a CLR instance.
    _ASSERTE(pClrBaseAddress != NULL);

    // If it's not even our exception code, then it's not ours.
    if (pRecord->ExceptionCode != CLRDBG_NOTIFICATION_EXCEPTION_CODE)
    {
        return NULL;
    }

    //
    // Format of an ExceptionInformation parameter is:
    //  0: cookie (CLRDBG_EXCEPTION_DATA_CHECKSUM)
    //  1: Base address of mscorwks. This identifies the instance of the CLR.
    //  2: Target Address of DebuggerIPCEvent, which contains the "real" event.
    //
    if (pRecord->NumberParameters != 3)
    {
        return NULL;
    }

    // 1st argument should always be the cookie.
    // If cookie doesn't match, very likely it's a stray exception that happens to be using
    // our code.
    DWORD cookie = (DWORD) pRecord->ExceptionInformation[0];
    if (cookie != CLRDBG_EXCEPTION_DATA_CHECKSUM)
    {
        return NULL;
    }

    // TODO: We don't do this check in case of non-windows debugging now, because we don't support
    // multi-instance debugging.
#if !defined(FEATURE_DBGIPC_TRANSPORT_VM) && !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    // If base-address doesn't match, then it's likely an event from another version of the CLR
    // in the target.
    // We need to be careful here.  CORDB_ADDRESS is a ULONG64, whereas ExceptionInformation[1]
    // is ULONG_PTR.  So on 32-bit, their sizes don't match.
    CORDB_ADDRESS pTargetBase = GetExceptionInfoAsAddress(pRecord, 1);
    if (pTargetBase != pClrBaseAddress)
    {
        return NULL;
    }
#endif

    // It passes all the format checks. So now get the payload.
    CORDB_ADDRESS ptrRemoteManagedEvent = GetExceptionInfoAsAddress(pRecord, 2);

    return ptrRemoteManagedEvent;
}

#if defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)
void InitEventForDebuggerNotification(DEBUG_EVENT *      pDebugEvent,
                                      CORDB_ADDRESS      pClrBaseAddress,
                                      DebuggerIPCEvent * pIPCEvent)
{
    pDebugEvent->dwDebugEventCode = EXCEPTION_DEBUG_EVENT;

    pDebugEvent->u.Exception.dwFirstChance = TRUE;
    pDebugEvent->u.Exception.ExceptionRecord.ExceptionCode    = CLRDBG_NOTIFICATION_EXCEPTION_CODE;
    pDebugEvent->u.Exception.ExceptionRecord.ExceptionFlags   = 0;
    pDebugEvent->u.Exception.ExceptionRecord.ExceptionRecord  = NULL;
    pDebugEvent->u.Exception.ExceptionRecord.ExceptionAddress = 0;

    //
    // Format of an ExceptionInformation parameter is:
    //  0: cookie (CLRDBG_EXCEPTION_DATA_CHECKSUM)
    //  1: Base address of mscorwks. This identifies the instance of the CLR.
    //  2: Target Address of DebuggerIPCEvent, which contains the "real" event.
    //
    pDebugEvent->u.Exception.ExceptionRecord.NumberParameters = 3;
    pDebugEvent->u.Exception.ExceptionRecord.ExceptionInformation[0] = CLRDBG_EXCEPTION_DATA_CHECKSUM;
    pDebugEvent->u.Exception.ExceptionRecord.ExceptionInformation[1] = (ULONG_PTR)CORDB_ADDRESS_TO_PTR(pClrBaseAddress);
    pDebugEvent->u.Exception.ExceptionRecord.ExceptionInformation[2] = (ULONG_PTR)pIPCEvent;

    _ASSERTE(IsEventDebuggerNotification(&(pDebugEvent->u.Exception.ExceptionRecord), pClrBaseAddress) ==
             PTR_TO_CORDB_ADDRESS(pIPCEvent));
}
#endif // defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)

//-----------------------------------------------------------------------------
// Helper to get the proper decorated name
// Caller ensures that pBufSize is large enough. We'll assert just to check,
// but no runtime failure.
// pBuf - the output buffer to write the decorated name in
// cBufSizeInChars - the size of the buffer in characters, including the null.
// pPrefx - The undecorated name of the event.
//-----------------------------------------------------------------------------
void GetPidDecoratedName(__out_z __out_ecount(cBufSizeInChars) WCHAR * pBuf, int cBufSizeInChars, const WCHAR * pPrefix, DWORD pid)
{
    const WCHAR szGlobal[] = W("Global\\");
    int szGlobalLen;
    szGlobalLen = NumItems(szGlobal) - 1;

    // Caller should always give us a big enough buffer.
    _ASSERTE(cBufSizeInChars > (int) wcslen(pPrefix) + szGlobalLen);

    // PERF: We are no longer calling GetSystemMetrics in an effort to prevent
    //       superfluous DLL loading on startup.  Instead, we're prepending
    //       "Global\" to named kernel objects if we are on NT5 or above.  The
    //       only bad thing that results from this is that you can't debug
    //       cross-session on NT4.  Big bloody deal.
    wcscpy_s(pBuf, cBufSizeInChars, szGlobal);
    pBuf += szGlobalLen;
    cBufSizeInChars -= szGlobalLen;

    int ret;
    ret = _snwprintf_s(pBuf, cBufSizeInChars, _TRUNCATE, pPrefix, pid);

    // Since this is all determined at compile time, we know we should have enough buffer.
    _ASSERTE (ret != STRUNCATE);
}

//-----------------------------------------------------------------------------
// The 'internal' version of our IL to Native map (the DebuggerILToNativeMap struct)
// has an extra field - ICorDebugInfo::SourceTypes source.  The 'external/user-visible'
// version (COR_DEBUG_IL_TO_NATIVE_MAP) lacks that field, so we need to translate our
// internal version to the external version.
// "Export" seemed more succinct than "CopyInternalToExternalILToNativeMap" :)
//-----------------------------------------------------------------------------
void ExportILToNativeMap(ULONG32 cMap,             // [in] Min size of mapExt, mapInt
             COR_DEBUG_IL_TO_NATIVE_MAP mapExt[],  // [in] Filled in here
             struct DebuggerILToNativeMap mapInt[],// [in] Source of info
             SIZE_T sizeOfCode)                    // [in] Total size of method (bytes)
{
    ULONG32 iMap;
    _ASSERTE(mapExt != NULL);
    _ASSERTE(mapInt != NULL);

    for(iMap=0; iMap < cMap; iMap++)
    {
        mapExt[iMap].ilOffset = mapInt[iMap].ilOffset ;
        mapExt[iMap].nativeStartOffset = mapInt[iMap].nativeStartOffset ;
        mapExt[iMap].nativeEndOffset = mapInt[iMap].nativeEndOffset ;

        // An element that has an end offset of zero, means "till the end of
        // the method".  Pretty this up so that customers don't have to care about
        // this.
        if ((DWORD)mapInt[iMap].source & (DWORD)ICorDebugInfo::NATIVE_END_OFFSET_UNKNOWN)
        {
            mapExt[iMap].nativeEndOffset = (ULONG32)sizeOfCode;
        }

#if defined(_DEBUG)
        {
            // UnsafeGetConfigDWORD
            SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
            static int fReturnSourceTypeForTesting = -1;
            if (fReturnSourceTypeForTesting == -1)
                fReturnSourceTypeForTesting = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ReturnSourceTypeForTesting);

            if (fReturnSourceTypeForTesting)
            {
                // Steal the most significant four bits from the native end offset for the source type.
                _ASSERTE( (mapExt[iMap].nativeEndOffset >> 28) == 0x0 );
                _ASSERTE( (ULONG32)(mapInt[iMap].source) < 0xF );
                mapExt[iMap].nativeEndOffset |= ((ULONG32)(mapInt[iMap].source) << 28);
            }
        }
#endif // _DEBUG
    }
}

const IPCEventTypeNameMapping DbgIPCEventTypeNames[] =
{
    #define IPC_EVENT_TYPE0(type, val)  { type, #type },
    #define IPC_EVENT_TYPE1(type, val)  { type, #type },
    #define IPC_EVENT_TYPE2(type, val)  { type, #type },
    #include "dbgipceventtypes.h"
    #undef IPC_EVENT_TYPE2
    #undef IPC_EVENT_TYPE1
    #undef IPC_EVENT_TYPE0
    { DB_IPCE_INVALID_EVENT, "DB_IPCE_Error" }
};

const size_t nameCount = sizeof(DbgIPCEventTypeNames) / sizeof(DbgIPCEventTypeNames[0]);
