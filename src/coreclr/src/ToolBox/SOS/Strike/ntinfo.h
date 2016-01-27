// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#ifndef _ntinfo_h__
#define _ntinfo_h__

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
class  ContextStackNode;
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
    // jsimmons 5/23/2001
    // Alert Alert:  nefarious folks (eg, URT) are looking in our TLS at
    // various stuff.   They expect that pCurrentCtx will be at a certain
    // offset from the beginning of the tls struct. So don't add, delete, or 
    // move any members within this block.

/////////////////////////////////////////////////////////////////////////////////////////
// ********* BEGIN "NO MUCKING AROUND" BLOCK ********* 
/////////////////////////////////////////////////////////////////////////////////////////
    // Docfile multiple allocator support
    void               *pvThreadBase;       // per thread base pointer
    CSmAllocator       *pSmAllocator;       // per thread docfile allocator

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
/////////////////////////////////////////////////////////////////////////////////////////
//  ********* END "NO MUCKING AROUND" BLOCK ********* 
/////////////////////////////////////////////////////////////////////////////////////////

    CObjectContext     *pEmptyCtx;          // Empty context

    CObjectContext     *pNativeCtx;         // Native context
    ULONGLONG           ContextId;          // Uniquely identifies the current context
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

    IUnknown           *punkError;          // Per thread error object.
    ULONG               cbErrorData;        // Maximum size of error data.

    IUnknown           *punkActiveXSafetyProvider;

#if DBG==1
    LONG                cTraceNestingLevel; // call nesting level for OLETRACE
#endif

    ContextStackNode* pContextStack;

} SOleTlsData;

#endif //_ntinfo_h__

