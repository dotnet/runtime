// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//+---------------------------------------------------------------------------
//
//  File:       oletls.h
//

//
//  Purpose:    manage thread local storage for OLE
//
//  Notes:      The gTlsIndex is initialized at process attach time.
//              The per-thread data is allocated in CoInitialize in
//              single-threaded apartments or on first use in
//              multi-threaded apartments.
//
//----------------------------------------------------------------------------

#ifndef _OLETLS_H_
#define _OLETLS_H_

#ifndef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#error FEATURE_COMINTEROP_APARTMENT_SUPPORT is required for this file
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

//+---------------------------------------------------------------------------
//
// forward declarations (in order to avoid type casting when accessing
// data members of the SOleTlsData structure).
//
//+---------------------------------------------------------------------------

class  CAptCallCtrl;                        // see callctrl.hxx
class  CSrvCallState;                       // see callctrl.hxx
class  CObjServer;                          // see sobjact.hxx
class  CSmAllocator;                        // see stg\h\smalloc.hxx
class  CMessageCall;                        // see call.hxx
class  CClientCall;                         // see call.hxx
class  CAsyncCall;                          // see call.hxx
class  CClipDataObject;                     // see ole232\clipbrd\clipdata.h
class  CSurrogatedObjectList;               // see com\inc\comsrgt.hxx
class  CCtxCall;                            // see PSTable.hxx
class  CPolicySet;                          // see PSTable.hxx
class  CObjectContext;                      // see context.hxx
class  CComApartment;                       // see aprtmnt.hxx

//+-------------------------------------------------------------------
//
//  Struct:     CallEntry
//
//  Synopsis:   Call Table Entry.
//
//+-------------------------------------------------------------------
typedef struct tagCallEntry
{
    void  *pNext;        // ptr to next entry
    void  *pvObject;     // Entry object
} CallEntry;



//+---------------------------------------------------------------------------
//
//  Enum:       OLETLSFLAGS
//
//  Synopsys:   bit values for dwFlags field of SOleTlsData. If you just want
//              to store a BOOL in TLS, use this enum and the dwFlag field.
//
//+---------------------------------------------------------------------------
typedef enum tagOLETLSFLAGS
{
    OLETLS_LOCALTID             = 0x01,   // This TID is in the current process.
    OLETLS_UUIDINITIALIZED      = 0x02,   // This Logical thread is init'd.
    OLETLS_INTHREADDETACH       = 0x04,   // This is in thread detach. Needed
                                          // due to NT's special thread detach
                                          // rules.
    OLETLS_CHANNELTHREADINITIALZED = 0x08,// This channel has been init'd
    OLETLS_WOWTHREAD            = 0x10,   // This thread is a 16-bit WOW thread.
    OLETLS_THREADUNINITIALIZING = 0x20,   // This thread is in CoUninitialize.
    OLETLS_DISABLE_OLE1DDE      = 0x40,   // This thread can't use a DDE window.
    OLETLS_APARTMENTTHREADED    = 0x80,   // This is an STA apartment thread
    OLETLS_MULTITHREADED        = 0x100,  // This is an MTA apartment thread
    OLETLS_IMPERSONATING        = 0x200,  // This thread is impersonating
    OLETLS_DISABLE_EVENTLOGGER  = 0x400,  // Prevent recursion in event logger
    OLETLS_INNEUTRALAPT         = 0x800,  // This thread is in the NTA
    OLETLS_DISPATCHTHREAD       = 0x1000, // This is a dispatch thread
    OLETLS_HOSTTHREAD           = 0x2000, // This is a host thread
    OLETLS_ALLOWCOINIT          = 0x4000, // This thread allows inits
    OLETLS_PENDINGUNINIT        = 0x8000, // This thread has pending uninit
    OLETLS_FIRSTMTAINIT         = 0x10000,// First thread to attempt an MTA init
    OLETLS_FIRSTNTAINIT         = 0x20000,// First thread to attempt an NTA init
    OLETLS_APTINITIALIZING      = 0x40000 // Apartment Object is initializing
}  OLETLSFLAGS;


//+---------------------------------------------------------------------------
//
//  Structure:  SOleTlsData
//
//  Synopsis:   structure holding per thread state needed by OLE32
//
//+---------------------------------------------------------------------------
typedef struct tagSOleTlsData
{
#if !defined(_CHICAGO_)
    // Docfile multiple allocator support
    void               *pvThreadBase;       // per thread base pointer
    CSmAllocator       *pSmAllocator;       // per thread docfile allocator
#endif
    DWORD               dwApartmentID;      // Per thread "process ID"
    DWORD               dwFlags;            // see OLETLSFLAGS above

    LONG                TlsMapIndex;        // index in the global TLSMap
    void              **ppTlsSlot;          // Back pointer to the thread tls slot
    DWORD               cComInits;          // number of per-thread inits
    DWORD               cOleInits;          // number of per-thread OLE inits

    DWORD               cCalls;             // number of outstanding calls
    CMessageCall       *pCallInfo;          // channel call info
    CAsyncCall         *pFreeAsyncCall;     // ptr to available call object for this thread.
    CClientCall        *pFreeClientCall;    // ptr to available call object for this thread.

    CObjServer         *pObjServer;         // Activation Server Object for this apartment.
    DWORD               dwTIDCaller;        // TID of current calling app
    CObjectContext     *pCurrentCtx;        // Current context
    CObjectContext     *pEmptyCtx;          // Empty context

    CObjectContext     *pNativeCtx;         // Native context
    CComApartment      *pNativeApt;         // Native apartment for the thread.
    IUnknown           *pCallContext;       // call context object
    CCtxCall           *pCtxCall;           // Context call object

    CPolicySet         *pPS;                // Policy set
    PVOID               pvPendingCallsFront;// Per Apt pending async calls
    PVOID               pvPendingCallsBack;
    CAptCallCtrl       *pCallCtrl;          // call control for RPC for this apartment

    CSrvCallState      *pTopSCS;            // top server-side callctrl state
    IMessageFilter     *pMsgFilter;         // temp storage for App MsgFilter
    HWND                hwndSTA;            // STA server window same as poxid->hServerSTA
                                            // ...needed on Win95 before oxid registration
    LONG                cORPCNestingLevel;  // call nesting level (DBG only)

    DWORD               cDebugData;         // count of bytes of debug data in call
    ULONG               cPreRegOidsAvail;   // count of server-side OIDs avail
    unsigned hyper     *pPreRegOids;        // ptr to array of pre-reg OIDs

    UUID                LogicalThreadId;    // current logical thread id

    HANDLE              hThread;            // Thread handle used for cancel
    HANDLE              hRevert;            // Token before first impersonate.
    IUnknown           *pAsyncRelease;      // Controlling unknown for async release
    // DDE data
    HWND                hwndDdeServer;      // Per thread Common DDE server

    HWND                hwndDdeClient;      // Per thread Common DDE client
    ULONG               cServeDdeObjects;   // non-zero if objects DDE should serve
    // ClassCache data
    LPVOID              pSTALSvrsFront;     // Chain of LServers registers in this thread if STA
    // upper layer data
    HWND                hwndClip;           // Clipboard window

    IDataObject         *pDataObjClip;      // Current Clipboard DataObject
    DWORD               dwClipSeqNum;       // Clipboard Sequence # for the above DataObject
    DWORD               fIsClipWrapper;     // Did we hand out the wrapper Clipboard DataObject?
    IUnknown            *punkState;         // Per thread "state" object
    // cancel data
    DWORD              cCallCancellation;   // count of CoEnableCallCancellation
    // async sends data
    DWORD              cAsyncSends;         // count of async sends outstanding

    CAsyncCall*           pAsyncCallList;   // async calls outstanding
    CSurrogatedObjectList *pSurrogateList;  // Objects in the surrogate

    LockEntry             lockEntry;        // Locks currently held by the thread
    CallEntry             CallEntry;        // client-side call chain for this thread

#ifdef WX86OLE
    IUnknown           *punkStateWx86;      // Per thread "state" object for Wx86
#endif
    void               *pDragCursors;       // Per thread drag cursor table.

#ifdef _CHICAGO_
    LPVOID              pWcstokContext;     // Scan context for wcstok
#endif

    IUnknown           *punkError;          // Per thread error object.
    ULONG               cbErrorData;        // Maximum size of error data.

#if(_WIN32_WINNT >= 0x0500)
    IUnknown           *punkActiveXSafetyProvider;
#endif //(_WIN32_WINNT >= 0x0500)

#if DBG==1
    LONG                cTraceNestingLevel; // call nesting level for OLETRACE
#endif

} SOleTlsData;

#ifdef INITGUID
#include "initguid.h"
#endif

#define DEFINE_OLEGUID(name, l, w1, w2) \
    DEFINE_GUID(name, l, w1, w2, 0xC0,0,0,0,0,0,0,0x46)

DEFINE_OLEGUID(IID_IStdIdentity,        0x0000001bL, 0, 0);
DEFINE_OLEGUID(IID_IStdWrapper,         0x000001caL, 0, 0);

#endif // _OLETLS_H_
