// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ---------------------------------------------------------------------------
// Ex.cpp
// ---------------------------------------------------------------------------

#include "stdafx.h"
#include "string.h"
#include "ex.h"
#include "holder.h"

// error codes
#include "corerror.h"

#include "../dlls/mscorrc/resource.h"

#include "olectl.h"

#include "corexcep.h"

#define MAX_EXCEPTION_MSG   200

// Set if fatal error (like stack overflow or out of memory) occurred in this process.
GVAL_IMPL_INIT(HRESULT, g_hrFatalError, S_OK);

// Helper function to get an exception object from outside the exception.  In
//  the CLR, it may be from the Thread object.  Non-CLR users have no thread object,
//  and it will do nothing.
void GetLastThrownObjectExceptionFromThread(Exception **ppException);

// Helper function to get pointer to clr module base
void* GetClrModuleBase();

Exception *Exception::g_OOMException = NULL;

// avoid global constructors
static BYTE g_OOMExceptionInstance[sizeof(OutOfMemoryException)];

Exception * Exception::GetOOMException()
{
    LIMITED_METHOD_CONTRACT;

    if (!g_OOMException) {
        // Create a local copy on the stack and then copy it over to the static instance.
        // This avoids race conditions caused by multiple initializations of vtable in the constructor

        OutOfMemoryException local(TRUE);  // Construct a "preallocated" instance.
        memcpy((void*)&g_OOMExceptionInstance, (void*)&local, sizeof(OutOfMemoryException));

        g_OOMException = (OutOfMemoryException*)&g_OOMExceptionInstance;
    }

    return g_OOMException;
}

/*virtual*/ Exception *OutOfMemoryException::Clone()
{
    LIMITED_METHOD_CONTRACT;

    return GetOOMException();
}

//------------------------------------------------------------------------------
void Exception::Delete(Exception* pvMemory)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC_HOST_ONLY;   // Exceptions aren't currently marshalled by DAC - just used in the host
    }
    CONTRACTL_END;

    if ((pvMemory == 0) || pvMemory->IsPreallocatedException())
    {
        return;
    }

    ::delete((Exception *) pvMemory);
}

void Exception::GetMessage(SString &result)
{
    WRAPPER_NO_CONTRACT;

    return GenerateTopLevelHRExceptionMessage(GetHR(), result);
}

void HRMsgException::GetMessage(SString &result)
{
    WRAPPER_NO_CONTRACT;

    if (m_msg.IsEmpty())
        HRException::GetMessage(result);
    else
        result = m_msg;
}

Exception *Exception::Clone()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
    }
    CONTRACTL_END;

    NewHolder<Exception> retExcep(CloneHelper());
    if (m_innerException)
    {
        retExcep->m_innerException = m_innerException->Clone();
    }

    retExcep.SuppressRelease();
    return retExcep;
}

Exception *Exception::CloneHelper()
{
    StackSString s;
    GetMessage(s);
    return new HRMsgException(GetHR(), s);
}

Exception *Exception::DomainBoundClone()
{
    CONTRACTL
    {
        // Because we may call DomainBoundCloneHelper() of ObjrefException or CLRLastThrownObjectException
        // this should be GC_TRIGGERS, but we can not include EE contracts in Utilcode.
        THROWS;
    }
    CONTRACTL_END;

    NewHolder<Exception> retExcep(DomainBoundCloneHelper());
    if (m_innerException)
    {
        retExcep->m_innerException = m_innerException->DomainBoundClone();
    }

    retExcep.SuppressRelease();
    return retExcep;
}

BOOL Exception::IsTerminal()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;

        // CLRException::GetHR() can eventually call BaseDomain::CreateHandle(),
        // which can indirectly cause a lock if we get a miss in the handle table
        // cache (TableCacheMissOnAlloc).  Since CLRException::GetHR() is virtual,
        // SCAN won't find this for you (though 40 minutes of one of the sql stress
        // tests will :-))
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr = GetHR();
    return (COR_E_THREADABORTED == hr);
}

BOOL Exception::IsTransient()
{
    WRAPPER_NO_CONTRACT;

    return IsTransient(GetHR());
}

/* static */
BOOL Exception::IsTransient(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;

    return (hr == COR_E_THREADABORTED
            || hr == COR_E_THREADINTERRUPTED
            || hr == COR_E_THREADSTOP
            || hr == COR_E_APPDOMAINUNLOADED
            || hr == E_OUTOFMEMORY
            || hr == HRESULT_FROM_WIN32(ERROR_COMMITMENT_LIMIT) // ran out of room in pagefile
            || hr == HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY)
            || hr == (HRESULT)STATUS_NO_MEMORY
            || hr == COR_E_STACKOVERFLOW
            || hr == MSEE_E_ASSEMBLYLOADINPROGRESS);
}

//------------------------------------------------------------------------------
// Functions to manage the preallocated exceptions.
// Virtual
BOOL Exception::IsPreallocatedException()
{   // Most exceptions can't be preallocated.  If they can be, their class
    //  should provide a virtual override of this function.
    return FALSE;
}

BOOL Exception::IsPreallocatedOOMException()
{   // This is the preallocated OOM if it is preallocated and is OOM.
    return IsPreallocatedException() && (GetInstanceType() == OutOfMemoryException::GetType());
}

//------------------------------------------------------------------------------
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
LPCSTR Exception::GetHRSymbolicName(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;

#define CASE_HRESULT(hrname) case hrname: return #hrname;

    switch (hr)
    {
        CASE_HRESULT(S_OK)//                             0x00000000L
        CASE_HRESULT(S_FALSE)//                          0x00000001L

        CASE_HRESULT(E_UNEXPECTED)//                     0x8000FFFFL
        CASE_HRESULT(E_NOTIMPL)//                        0x80004001L
        CASE_HRESULT(E_OUTOFMEMORY)//                    0x8007000EL
        CASE_HRESULT(E_INVALIDARG)//                     0x80070057L
        CASE_HRESULT(E_NOINTERFACE)//                    0x80004002L
        CASE_HRESULT(E_POINTER)//                        0x80004003L
        CASE_HRESULT(E_HANDLE)//                         0x80070006L
        CASE_HRESULT(E_ABORT)//                          0x80004004L
        CASE_HRESULT(E_FAIL)//                           0x80004005L
        CASE_HRESULT(E_ACCESSDENIED)//                   0x80070005L

#ifdef FEATURE_COMINTEROP
        CASE_HRESULT(CO_E_INIT_TLS)//                    0x80004006L
        CASE_HRESULT(CO_E_INIT_SHARED_ALLOCATOR)//       0x80004007L
        CASE_HRESULT(CO_E_INIT_MEMORY_ALLOCATOR)//       0x80004008L
        CASE_HRESULT(CO_E_INIT_CLASS_CACHE)//            0x80004009L
        CASE_HRESULT(CO_E_INIT_RPC_CHANNEL)//            0x8000400AL
        CASE_HRESULT(CO_E_INIT_TLS_SET_CHANNEL_CONTROL)// 0x8000400BL
        CASE_HRESULT(CO_E_INIT_TLS_CHANNEL_CONTROL)//    0x8000400CL
        CASE_HRESULT(CO_E_INIT_UNACCEPTED_USER_ALLOCATOR)// 0x8000400DL
        CASE_HRESULT(CO_E_INIT_SCM_MUTEX_EXISTS)//       0x8000400EL
        CASE_HRESULT(CO_E_INIT_SCM_FILE_MAPPING_EXISTS)// 0x8000400FL
        CASE_HRESULT(CO_E_INIT_SCM_MAP_VIEW_OF_FILE)//   0x80004010L
        CASE_HRESULT(CO_E_INIT_SCM_EXEC_FAILURE)//       0x80004011L
        CASE_HRESULT(CO_E_INIT_ONLY_SINGLE_THREADED)//   0x80004012L

// ******************
// FACILITY_ITF
// ******************

        CASE_HRESULT(OLE_E_OLEVERB)//                    0x80040000L
        CASE_HRESULT(OLE_E_ADVF)//                       0x80040001L
        CASE_HRESULT(OLE_E_ENUM_NOMORE)//                0x80040002L
        CASE_HRESULT(OLE_E_ADVISENOTSUPPORTED)//         0x80040003L
        CASE_HRESULT(OLE_E_NOCONNECTION)//               0x80040004L
        CASE_HRESULT(OLE_E_NOTRUNNING)//                 0x80040005L
        CASE_HRESULT(OLE_E_NOCACHE)//                    0x80040006L
        CASE_HRESULT(OLE_E_BLANK)//                      0x80040007L
        CASE_HRESULT(OLE_E_CLASSDIFF)//                  0x80040008L
        CASE_HRESULT(OLE_E_CANT_GETMONIKER)//            0x80040009L
        CASE_HRESULT(OLE_E_CANT_BINDTOSOURCE)//          0x8004000AL
        CASE_HRESULT(OLE_E_STATIC)//                     0x8004000BL
        CASE_HRESULT(OLE_E_PROMPTSAVECANCELLED)//        0x8004000CL
        CASE_HRESULT(OLE_E_INVALIDRECT)//                0x8004000DL
        CASE_HRESULT(OLE_E_WRONGCOMPOBJ)//               0x8004000EL
        CASE_HRESULT(OLE_E_INVALIDHWND)//                0x8004000FL
        CASE_HRESULT(OLE_E_NOT_INPLACEACTIVE)//          0x80040010L
        CASE_HRESULT(OLE_E_CANTCONVERT)//                0x80040011L
        CASE_HRESULT(OLE_E_NOSTORAGE)//                  0x80040012L
        CASE_HRESULT(DV_E_FORMATETC)//                   0x80040064L
        CASE_HRESULT(DV_E_DVTARGETDEVICE)//              0x80040065L
        CASE_HRESULT(DV_E_STGMEDIUM)//                   0x80040066L
        CASE_HRESULT(DV_E_STATDATA)//                    0x80040067L
        CASE_HRESULT(DV_E_LINDEX)//                      0x80040068L
        CASE_HRESULT(DV_E_TYMED)//                       0x80040069L
        CASE_HRESULT(DV_E_CLIPFORMAT)//                  0x8004006AL
        CASE_HRESULT(DV_E_DVASPECT)//                    0x8004006BL
        CASE_HRESULT(DV_E_DVTARGETDEVICE_SIZE)//         0x8004006CL
        CASE_HRESULT(DV_E_NOIVIEWOBJECT)//               0x8004006DL
        CASE_HRESULT(DRAGDROP_E_NOTREGISTERED)//         0x80040100L
        CASE_HRESULT(DRAGDROP_E_ALREADYREGISTERED)//     0x80040101L
        CASE_HRESULT(DRAGDROP_E_INVALIDHWND)//           0x80040102L
        CASE_HRESULT(CLASS_E_NOAGGREGATION)//            0x80040110L
        CASE_HRESULT(CLASS_E_CLASSNOTAVAILABLE)//        0x80040111L
        CASE_HRESULT(VIEW_E_DRAW)//                      0x80040140L
        CASE_HRESULT(REGDB_E_READREGDB)//                0x80040150L
        CASE_HRESULT(REGDB_E_WRITEREGDB)//               0x80040151L
        CASE_HRESULT(REGDB_E_KEYMISSING)//               0x80040152L
        CASE_HRESULT(REGDB_E_INVALIDVALUE)//             0x80040153L
        CASE_HRESULT(REGDB_E_CLASSNOTREG)//              0x80040154L
        CASE_HRESULT(CACHE_E_NOCACHE_UPDATED)//          0x80040170L
        CASE_HRESULT(OLEOBJ_E_NOVERBS)//                 0x80040180L
        CASE_HRESULT(INPLACE_E_NOTUNDOABLE)//            0x800401A0L
        CASE_HRESULT(INPLACE_E_NOTOOLSPACE)//            0x800401A1L
        CASE_HRESULT(CONVERT10_E_OLESTREAM_GET)//        0x800401C0L
        CASE_HRESULT(CONVERT10_E_OLESTREAM_PUT)//        0x800401C1L
        CASE_HRESULT(CONVERT10_E_OLESTREAM_FMT)//        0x800401C2L
        CASE_HRESULT(CONVERT10_E_OLESTREAM_BITMAP_TO_DIB)// 0x800401C3L
        CASE_HRESULT(CONVERT10_E_STG_FMT)//              0x800401C4L
        CASE_HRESULT(CONVERT10_E_STG_NO_STD_STREAM)//    0x800401C5L
        CASE_HRESULT(CONVERT10_E_STG_DIB_TO_BITMAP)//    0x800401C6L
        CASE_HRESULT(CLIPBRD_E_CANT_OPEN)//              0x800401D0L
        CASE_HRESULT(CLIPBRD_E_CANT_EMPTY)//             0x800401D1L
        CASE_HRESULT(CLIPBRD_E_CANT_SET)//               0x800401D2L
        CASE_HRESULT(CLIPBRD_E_BAD_DATA)//               0x800401D3L
        CASE_HRESULT(CLIPBRD_E_CANT_CLOSE)//             0x800401D4L
        CASE_HRESULT(MK_E_CONNECTMANUALLY)//             0x800401E0L
        CASE_HRESULT(MK_E_EXCEEDEDDEADLINE)//            0x800401E1L
        CASE_HRESULT(MK_E_NEEDGENERIC)//                 0x800401E2L
        CASE_HRESULT(MK_E_UNAVAILABLE)//                 0x800401E3L
        CASE_HRESULT(MK_E_SYNTAX)//                      0x800401E4L
        CASE_HRESULT(MK_E_NOOBJECT)//                    0x800401E5L
        CASE_HRESULT(MK_E_INVALIDEXTENSION)//            0x800401E6L
        CASE_HRESULT(MK_E_INTERMEDIATEINTERFACENOTSUPPORTED)// 0x800401E7L
        CASE_HRESULT(MK_E_NOTBINDABLE)//                 0x800401E8L
        CASE_HRESULT(MK_E_NOTBOUND)//                    0x800401E9L
        CASE_HRESULT(MK_E_CANTOPENFILE)//                0x800401EAL
        CASE_HRESULT(MK_E_MUSTBOTHERUSER)//              0x800401EBL
        CASE_HRESULT(MK_E_NOINVERSE)//                   0x800401ECL
        CASE_HRESULT(MK_E_NOSTORAGE)//                   0x800401EDL
        CASE_HRESULT(MK_E_NOPREFIX)//                    0x800401EEL
        CASE_HRESULT(MK_E_ENUMERATION_FAILED)//          0x800401EFL
        CASE_HRESULT(CO_E_NOTINITIALIZED)//              0x800401F0L
        CASE_HRESULT(CO_E_ALREADYINITIALIZED)//          0x800401F1L
        CASE_HRESULT(CO_E_CANTDETERMINECLASS)//          0x800401F2L
        CASE_HRESULT(CO_E_CLASSSTRING)//                 0x800401F3L
        CASE_HRESULT(CO_E_IIDSTRING)//                   0x800401F4L
        CASE_HRESULT(CO_E_APPNOTFOUND)//                 0x800401F5L
        CASE_HRESULT(CO_E_APPSINGLEUSE)//                0x800401F6L
        CASE_HRESULT(CO_E_ERRORINAPP)//                  0x800401F7L
        CASE_HRESULT(CO_E_DLLNOTFOUND)//                 0x800401F8L
        CASE_HRESULT(CO_E_ERRORINDLL)//                  0x800401F9L
        CASE_HRESULT(CO_E_WRONGOSFORAPP)//               0x800401FAL
        CASE_HRESULT(CO_E_OBJNOTREG)//                   0x800401FBL
        CASE_HRESULT(CO_E_OBJISREG)//                    0x800401FCL
        CASE_HRESULT(CO_E_OBJNOTCONNECTED)//             0x800401FDL
        CASE_HRESULT(CO_E_APPDIDNTREG)//                 0x800401FEL
        CASE_HRESULT(CO_E_RELEASED)//                    0x800401FFL

        CASE_HRESULT(OLE_S_USEREG)//                     0x00040000L
        CASE_HRESULT(OLE_S_STATIC)//                     0x00040001L
        CASE_HRESULT(OLE_S_MAC_CLIPFORMAT)//             0x00040002L
        CASE_HRESULT(DRAGDROP_S_DROP)//                  0x00040100L
        CASE_HRESULT(DRAGDROP_S_CANCEL)//                0x00040101L
        CASE_HRESULT(DRAGDROP_S_USEDEFAULTCURSORS)//     0x00040102L
        CASE_HRESULT(DATA_S_SAMEFORMATETC)//             0x00040130L
        CASE_HRESULT(VIEW_S_ALREADY_FROZEN)//            0x00040140L
        CASE_HRESULT(CACHE_S_FORMATETC_NOTSUPPORTED)//   0x00040170L
        CASE_HRESULT(CACHE_S_SAMECACHE)//                0x00040171L
        CASE_HRESULT(CACHE_S_SOMECACHES_NOTUPDATED)//    0x00040172L
        CASE_HRESULT(OLEOBJ_S_INVALIDVERB)//             0x00040180L
        CASE_HRESULT(OLEOBJ_S_CANNOT_DOVERB_NOW)//       0x00040181L
        CASE_HRESULT(OLEOBJ_S_INVALIDHWND)//             0x00040182L
        CASE_HRESULT(INPLACE_S_TRUNCATED)//              0x000401A0L
        CASE_HRESULT(CONVERT10_S_NO_PRESENTATION)//      0x000401C0L
        CASE_HRESULT(MK_S_REDUCED_TO_SELF)//             0x000401E2L
        CASE_HRESULT(MK_S_ME)//                          0x000401E4L
        CASE_HRESULT(MK_S_HIM)//                         0x000401E5L
        CASE_HRESULT(MK_S_US)//                          0x000401E6L
        CASE_HRESULT(MK_S_MONIKERALREADYREGISTERED)//    0x000401E7L

// ******************
// FACILITY_WINDOWS
// ******************

        CASE_HRESULT(CO_E_CLASS_CREATE_FAILED)//         0x80080001L
        CASE_HRESULT(CO_E_SCM_ERROR)//                   0x80080002L
        CASE_HRESULT(CO_E_SCM_RPC_FAILURE)//             0x80080003L
        CASE_HRESULT(CO_E_BAD_PATH)//                    0x80080004L
        CASE_HRESULT(CO_E_SERVER_EXEC_FAILURE)//         0x80080005L
        CASE_HRESULT(CO_E_OBJSRV_RPC_FAILURE)//          0x80080006L
        CASE_HRESULT(MK_E_NO_NORMALIZED)//               0x80080007L
        CASE_HRESULT(CO_E_SERVER_STOPPING)//             0x80080008L
        CASE_HRESULT(MEM_E_INVALID_ROOT)//               0x80080009L
        CASE_HRESULT(MEM_E_INVALID_LINK)//               0x80080010L
        CASE_HRESULT(MEM_E_INVALID_SIZE)//               0x80080011L

// ******************
// FACILITY_DISPATCH
// ******************

        CASE_HRESULT(DISP_E_UNKNOWNINTERFACE)//          0x80020001L
        CASE_HRESULT(DISP_E_MEMBERNOTFOUND)//            0x80020003L
        CASE_HRESULT(DISP_E_PARAMNOTFOUND)//             0x80020004L
        CASE_HRESULT(DISP_E_TYPEMISMATCH)//              0x80020005L
        CASE_HRESULT(DISP_E_UNKNOWNNAME)//               0x80020006L
        CASE_HRESULT(DISP_E_NONAMEDARGS)//               0x80020007L
        CASE_HRESULT(DISP_E_BADVARTYPE)//                0x80020008L
        CASE_HRESULT(DISP_E_EXCEPTION)//                 0x80020009L
        CASE_HRESULT(DISP_E_OVERFLOW)//                  0x8002000AL
        CASE_HRESULT(DISP_E_BADINDEX)//                  0x8002000BL
        CASE_HRESULT(DISP_E_UNKNOWNLCID)//               0x8002000CL
        CASE_HRESULT(DISP_E_ARRAYISLOCKED)//             0x8002000DL
        CASE_HRESULT(DISP_E_BADPARAMCOUNT)//             0x8002000EL
        CASE_HRESULT(DISP_E_PARAMNOTOPTIONAL)//          0x8002000FL
        CASE_HRESULT(DISP_E_BADCALLEE)//                 0x80020010L
        CASE_HRESULT(DISP_E_NOTACOLLECTION)//            0x80020011L
        CASE_HRESULT(TYPE_E_BUFFERTOOSMALL)//            0x80028016L
        CASE_HRESULT(TYPE_E_INVDATAREAD)//               0x80028018L
        CASE_HRESULT(TYPE_E_UNSUPFORMAT)//               0x80028019L
        CASE_HRESULT(TYPE_E_REGISTRYACCESS)//            0x8002801CL
        CASE_HRESULT(TYPE_E_LIBNOTREGISTERED)//          0x8002801DL
        CASE_HRESULT(TYPE_E_UNDEFINEDTYPE)//             0x80028027L
        CASE_HRESULT(TYPE_E_QUALIFIEDNAMEDISALLOWED)//   0x80028028L
        CASE_HRESULT(TYPE_E_INVALIDSTATE)//              0x80028029L
        CASE_HRESULT(TYPE_E_WRONGTYPEKIND)//             0x8002802AL
        CASE_HRESULT(TYPE_E_ELEMENTNOTFOUND)//           0x8002802BL
        CASE_HRESULT(TYPE_E_AMBIGUOUSNAME)//             0x8002802CL
        CASE_HRESULT(TYPE_E_NAMECONFLICT)//              0x8002802DL
        CASE_HRESULT(TYPE_E_UNKNOWNLCID)//               0x8002802EL
        CASE_HRESULT(TYPE_E_DLLFUNCTIONNOTFOUND)//       0x8002802FL
        CASE_HRESULT(TYPE_E_BADMODULEKIND)//             0x800288BDL
        CASE_HRESULT(TYPE_E_SIZETOOBIG)//                0x800288C5L
        CASE_HRESULT(TYPE_E_DUPLICATEID)//               0x800288C6L
        CASE_HRESULT(TYPE_E_INVALIDID)//                 0x800288CFL
        CASE_HRESULT(TYPE_E_TYPEMISMATCH)//              0x80028CA0L
        CASE_HRESULT(TYPE_E_OUTOFBOUNDS)//               0x80028CA1L
        CASE_HRESULT(TYPE_E_IOERROR)//                   0x80028CA2L
        CASE_HRESULT(TYPE_E_CANTCREATETMPFILE)//         0x80028CA3L
        CASE_HRESULT(TYPE_E_CANTLOADLIBRARY)//           0x80029C4AL
        CASE_HRESULT(TYPE_E_INCONSISTENTPROPFUNCS)//     0x80029C83L
        CASE_HRESULT(TYPE_E_CIRCULARTYPE)//              0x80029C84L

// ******************
// FACILITY_STORAGE
// ******************

        CASE_HRESULT(STG_E_INVALIDFUNCTION)//            0x80030001L
        CASE_HRESULT(STG_E_FILENOTFOUND)//               0x80030002L
        CASE_HRESULT(STG_E_PATHNOTFOUND)//               0x80030003L
        CASE_HRESULT(STG_E_TOOMANYOPENFILES)//           0x80030004L
        CASE_HRESULT(STG_E_ACCESSDENIED)//               0x80030005L
        CASE_HRESULT(STG_E_INVALIDHANDLE)//              0x80030006L
        CASE_HRESULT(STG_E_INSUFFICIENTMEMORY)//         0x80030008L
        CASE_HRESULT(STG_E_INVALIDPOINTER)//             0x80030009L
        CASE_HRESULT(STG_E_NOMOREFILES)//                0x80030012L
        CASE_HRESULT(STG_E_DISKISWRITEPROTECTED)//       0x80030013L
        CASE_HRESULT(STG_E_SEEKERROR)//                  0x80030019L
        CASE_HRESULT(STG_E_WRITEFAULT)//                 0x8003001DL
        CASE_HRESULT(STG_E_READFAULT)//                  0x8003001EL
        CASE_HRESULT(STG_E_SHAREVIOLATION)//             0x80030020L
        CASE_HRESULT(STG_E_LOCKVIOLATION)//              0x80030021L
        CASE_HRESULT(STG_E_FILEALREADYEXISTS)//          0x80030050L
        CASE_HRESULT(STG_E_INVALIDPARAMETER)//           0x80030057L
        CASE_HRESULT(STG_E_MEDIUMFULL)//                 0x80030070L
        CASE_HRESULT(STG_E_ABNORMALAPIEXIT)//            0x800300FAL
        CASE_HRESULT(STG_E_INVALIDHEADER)//              0x800300FBL
        CASE_HRESULT(STG_E_INVALIDNAME)//                0x800300FCL
        CASE_HRESULT(STG_E_UNKNOWN)//                    0x800300FDL
        CASE_HRESULT(STG_E_UNIMPLEMENTEDFUNCTION)//      0x800300FEL
        CASE_HRESULT(STG_E_INVALIDFLAG)//                0x800300FFL
        CASE_HRESULT(STG_E_INUSE)//                      0x80030100L
        CASE_HRESULT(STG_E_NOTCURRENT)//                 0x80030101L
        CASE_HRESULT(STG_E_REVERTED)//                   0x80030102L
        CASE_HRESULT(STG_E_CANTSAVE)//                   0x80030103L
        CASE_HRESULT(STG_E_OLDFORMAT)//                  0x80030104L
        CASE_HRESULT(STG_E_OLDDLL)//                     0x80030105L
        CASE_HRESULT(STG_E_SHAREREQUIRED)//              0x80030106L
        CASE_HRESULT(STG_E_NOTFILEBASEDSTORAGE)//        0x80030107L
        CASE_HRESULT(STG_S_CONVERTED)//                  0x00030200L

// ******************
// FACILITY_RPC
// ******************

        CASE_HRESULT(RPC_E_CALL_REJECTED)//              0x80010001L
        CASE_HRESULT(RPC_E_CALL_CANCELED)//              0x80010002L
        CASE_HRESULT(RPC_E_CANTPOST_INSENDCALL)//        0x80010003L
        CASE_HRESULT(RPC_E_CANTCALLOUT_INASYNCCALL)//    0x80010004L
        CASE_HRESULT(RPC_E_CANTCALLOUT_INEXTERNALCALL)// 0x80010005L
        CASE_HRESULT(RPC_E_CONNECTION_TERMINATED)//      0x80010006L
        CASE_HRESULT(RPC_E_SERVER_DIED)//                0x80010007L
        CASE_HRESULT(RPC_E_CLIENT_DIED)//                0x80010008L
        CASE_HRESULT(RPC_E_INVALID_DATAPACKET)//         0x80010009L
        CASE_HRESULT(RPC_E_CANTTRANSMIT_CALL)//          0x8001000AL
        CASE_HRESULT(RPC_E_CLIENT_CANTMARSHAL_DATA)//    0x8001000BL
        CASE_HRESULT(RPC_E_CLIENT_CANTUNMARSHAL_DATA)//  0x8001000CL
        CASE_HRESULT(RPC_E_SERVER_CANTMARSHAL_DATA)//    0x8001000DL
        CASE_HRESULT(RPC_E_SERVER_CANTUNMARSHAL_DATA)//  0x8001000EL
        CASE_HRESULT(RPC_E_INVALID_DATA)//               0x8001000FL
        CASE_HRESULT(RPC_E_INVALID_PARAMETER)//          0x80010010L
        CASE_HRESULT(RPC_E_CANTCALLOUT_AGAIN)//          0x80010011L
        CASE_HRESULT(RPC_E_SERVER_DIED_DNE)//            0x80010012L
        CASE_HRESULT(RPC_E_SYS_CALL_FAILED)//            0x80010100L
        CASE_HRESULT(RPC_E_OUT_OF_RESOURCES)//           0x80010101L
        CASE_HRESULT(RPC_E_ATTEMPTED_MULTITHREAD)//      0x80010102L
        CASE_HRESULT(RPC_E_NOT_REGISTERED)//             0x80010103L
        CASE_HRESULT(RPC_E_FAULT)//                      0x80010104L
        CASE_HRESULT(RPC_E_SERVERFAULT)//                0x80010105L
        CASE_HRESULT(RPC_E_CHANGED_MODE)//               0x80010106L
        CASE_HRESULT(RPC_E_INVALIDMETHOD)//              0x80010107L
        CASE_HRESULT(RPC_E_DISCONNECTED)//               0x80010108L
        CASE_HRESULT(RPC_E_RETRY)//                      0x80010109L
        CASE_HRESULT(RPC_E_SERVERCALL_RETRYLATER)//      0x8001010AL
        CASE_HRESULT(RPC_E_SERVERCALL_REJECTED)//        0x8001010BL
        CASE_HRESULT(RPC_E_INVALID_CALLDATA)//           0x8001010CL
        CASE_HRESULT(RPC_E_CANTCALLOUT_ININPUTSYNCCALL)// 0x8001010DL
        CASE_HRESULT(RPC_E_WRONG_THREAD)//               0x8001010EL
        CASE_HRESULT(RPC_E_THREAD_NOT_INIT)//            0x8001010FL
        CASE_HRESULT(RPC_E_UNEXPECTED)//                 0x8001FFFFL

// ******************
// FACILITY_CTL
// ******************

        CASE_HRESULT(CTL_E_ILLEGALFUNCTIONCALL)
        CASE_HRESULT(CTL_E_OVERFLOW)
        CASE_HRESULT(CTL_E_OUTOFMEMORY)
        CASE_HRESULT(CTL_E_DIVISIONBYZERO)
        CASE_HRESULT(CTL_E_OUTOFSTRINGSPACE)
        CASE_HRESULT(CTL_E_OUTOFSTACKSPACE)
        CASE_HRESULT(CTL_E_BADFILENAMEORNUMBER)
        CASE_HRESULT(CTL_E_FILENOTFOUND)
        CASE_HRESULT(CTL_E_BADFILEMODE)
        CASE_HRESULT(CTL_E_FILEALREADYOPEN)
        CASE_HRESULT(CTL_E_DEVICEIOERROR)
        CASE_HRESULT(CTL_E_FILEALREADYEXISTS)
        CASE_HRESULT(CTL_E_BADRECORDLENGTH)
        CASE_HRESULT(CTL_E_DISKFULL)
        CASE_HRESULT(CTL_E_BADRECORDNUMBER)
        CASE_HRESULT(CTL_E_BADFILENAME)
        CASE_HRESULT(CTL_E_TOOMANYFILES)
        CASE_HRESULT(CTL_E_DEVICEUNAVAILABLE)
        CASE_HRESULT(CTL_E_PERMISSIONDENIED)
        CASE_HRESULT(CTL_E_DISKNOTREADY)
        CASE_HRESULT(CTL_E_PATHFILEACCESSERROR)
        CASE_HRESULT(CTL_E_PATHNOTFOUND)
        CASE_HRESULT(CTL_E_INVALIDPATTERNSTRING)
        CASE_HRESULT(CTL_E_INVALIDUSEOFNULL)
        CASE_HRESULT(CTL_E_INVALIDFILEFORMAT)
        CASE_HRESULT(CTL_E_INVALIDPROPERTYVALUE)
        CASE_HRESULT(CTL_E_INVALIDPROPERTYARRAYINDEX)
        CASE_HRESULT(CTL_E_SETNOTSUPPORTEDATRUNTIME)
        CASE_HRESULT(CTL_E_SETNOTSUPPORTED)
        CASE_HRESULT(CTL_E_NEEDPROPERTYARRAYINDEX)
        CASE_HRESULT(CTL_E_SETNOTPERMITTED)
        CASE_HRESULT(CTL_E_GETNOTSUPPORTEDATRUNTIME)
        CASE_HRESULT(CTL_E_GETNOTSUPPORTED)
        CASE_HRESULT(CTL_E_PROPERTYNOTFOUND)
        CASE_HRESULT(CTL_E_INVALIDCLIPBOARDFORMAT)
        CASE_HRESULT(CTL_E_INVALIDPICTURE)
        CASE_HRESULT(CTL_E_PRINTERERROR)
        CASE_HRESULT(CTL_E_CANTSAVEFILETOTEMP)
        CASE_HRESULT(CTL_E_SEARCHTEXTNOTFOUND)
        CASE_HRESULT(CTL_E_REPLACEMENTSTOOLONG)
#endif // FEATURE_COMINTEROP

#ifdef _DEBUG  // @todo: do we want to burn strings for this in a free build?

    CASE_HRESULT(COR_E_APPDOMAINUNLOADED)
    CASE_HRESULT(COR_E_CANNOTUNLOADAPPDOMAIN)
    CASE_HRESULT(MSEE_E_ASSEMBLYLOADINPROGRESS)
    CASE_HRESULT(FUSION_E_CACHEFILE_FAILED)
    CASE_HRESULT(FUSION_E_REF_DEF_MISMATCH)
    CASE_HRESULT(FUSION_E_PRIVATE_ASM_DISALLOWED)
    CASE_HRESULT(FUSION_E_INVALID_NAME)
    CASE_HRESULT(CLDB_E_FILE_BADREAD)
    CASE_HRESULT(CLDB_E_FILE_BADWRITE)
    CASE_HRESULT(CLDB_S_TRUNCATION)
    CASE_HRESULT(CLDB_E_FILE_OLDVER)
    CASE_HRESULT(CLDB_E_SMDUPLICATE)
    CASE_HRESULT(CLDB_E_NO_DATA)
    CASE_HRESULT(CLDB_E_INCOMPATIBLE)
    CASE_HRESULT(CLDB_E_FILE_CORRUPT)
    CASE_HRESULT(CLDB_E_BADUPDATEMODE)
    CASE_HRESULT(CLDB_E_INDEX_NOTFOUND)
    CASE_HRESULT(CLDB_E_RECORD_NOTFOUND)
    CASE_HRESULT(CLDB_E_RECORD_OUTOFORDER)
    CASE_HRESULT(CLDB_E_TOO_BIG)
    CASE_HRESULT(META_E_BADMETADATA)
    CASE_HRESULT(META_E_BAD_SIGNATURE)
    CASE_HRESULT(META_E_BAD_INPUT_PARAMETER)
    CASE_HRESULT(META_E_CANNOTRESOLVETYPEREF)
    CASE_HRESULT(META_S_DUPLICATE)
    CASE_HRESULT(META_E_STRINGSPACE_FULL)
    CASE_HRESULT(META_E_HAS_UNMARKALL)
    CASE_HRESULT(META_E_MUST_CALL_UNMARKALL)
    CASE_HRESULT(META_E_CA_INVALID_TARGET)
    CASE_HRESULT(META_E_CA_INVALID_VALUE)
    CASE_HRESULT(META_E_CA_INVALID_BLOB)
    CASE_HRESULT(META_E_CA_REPEATED_ARG)
    CASE_HRESULT(META_E_CA_UNKNOWN_ARGUMENT)
    CASE_HRESULT(META_E_CA_UNEXPECTED_TYPE)
    CASE_HRESULT(META_E_CA_INVALID_ARGTYPE)
    CASE_HRESULT(META_E_CA_INVALID_ARG_FOR_TYPE)
    CASE_HRESULT(META_E_CA_INVALID_UUID)
    CASE_HRESULT(META_E_CA_INVALID_MARSHALAS_FIELDS)
    CASE_HRESULT(META_E_CA_NT_FIELDONLY)
    CASE_HRESULT(META_E_CA_NEGATIVE_PARAMINDEX)
    CASE_HRESULT(META_E_CA_NEGATIVE_CONSTSIZE)
    CASE_HRESULT(META_E_CA_FIXEDSTR_SIZE_REQUIRED)
    CASE_HRESULT(META_E_CA_CUSTMARSH_TYPE_REQUIRED)
    CASE_HRESULT(META_E_CA_BAD_FRIENDS_ARGS)
    CASE_HRESULT(VLDTR_E_RID_OUTOFRANGE)
    CASE_HRESULT(VLDTR_E_STRING_INVALID)
    CASE_HRESULT(VLDTR_E_GUID_INVALID)
    CASE_HRESULT(VLDTR_E_BLOB_INVALID)
    CASE_HRESULT(VLDTR_E_MR_BADCALLINGCONV)
    CASE_HRESULT(VLDTR_E_SIGNULL)
    CASE_HRESULT(VLDTR_E_MD_BADCALLINGCONV)
    CASE_HRESULT(VLDTR_E_MD_THISSTATIC)
    CASE_HRESULT(VLDTR_E_MD_NOTTHISNOTSTATIC)
    CASE_HRESULT(VLDTR_E_MD_NOARGCNT)
    CASE_HRESULT(VLDTR_E_SIG_MISSELTYPE)
    CASE_HRESULT(VLDTR_E_SIG_MISSTKN)
    CASE_HRESULT(VLDTR_E_SIG_TKNBAD)
    CASE_HRESULT(VLDTR_E_SIG_MISSFPTR)
    CASE_HRESULT(VLDTR_E_SIG_MISSFPTRARGCNT)
    CASE_HRESULT(VLDTR_E_SIG_MISSRANK)
    CASE_HRESULT(VLDTR_E_SIG_MISSNSIZE)
    CASE_HRESULT(VLDTR_E_SIG_MISSSIZE)
    CASE_HRESULT(VLDTR_E_SIG_MISSNLBND)
    CASE_HRESULT(VLDTR_E_SIG_MISSLBND)
    CASE_HRESULT(VLDTR_E_SIG_BADELTYPE)
    CASE_HRESULT(VLDTR_E_TD_ENCLNOTNESTED)
    CASE_HRESULT(VLDTR_E_FMD_PINVOKENOTSTATIC)
    CASE_HRESULT(VLDTR_E_SIG_SENTINMETHODDEF)
    CASE_HRESULT(VLDTR_E_SIG_SENTMUSTVARARG)
    CASE_HRESULT(VLDTR_E_SIG_MULTSENTINELS)
    CASE_HRESULT(VLDTR_E_SIG_MISSARG)
    CASE_HRESULT(VLDTR_E_SIG_BYREFINFIELD)
    CASE_HRESULT(VLDTR_E_SIG_BADVOID)
    CASE_HRESULT(CORDBG_E_UNRECOVERABLE_ERROR)
    CASE_HRESULT(CORDBG_E_PROCESS_TERMINATED)
    CASE_HRESULT(CORDBG_E_PROCESS_NOT_SYNCHRONIZED)
    CASE_HRESULT(CORDBG_E_CLASS_NOT_LOADED)
    CASE_HRESULT(CORDBG_E_IL_VAR_NOT_AVAILABLE)
    CASE_HRESULT(CORDBG_E_BAD_REFERENCE_VALUE)
    CASE_HRESULT(CORDBG_E_FIELD_NOT_AVAILABLE)
    CASE_HRESULT(CORDBG_E_NON_NATIVE_FRAME)
    CASE_HRESULT(CORDBG_E_CODE_NOT_AVAILABLE)
    CASE_HRESULT(CORDBG_E_FUNCTION_NOT_IL)
    CASE_HRESULT(CORDBG_S_BAD_START_SEQUENCE_POINT)
    CASE_HRESULT(CORDBG_S_BAD_END_SEQUENCE_POINT)
    CASE_HRESULT(CORDBG_E_CANT_SET_IP_INTO_FINALLY)
    CASE_HRESULT(CORDBG_E_CANT_SET_IP_OUT_OF_FINALLY)
    CASE_HRESULT(CORDBG_E_CANT_SET_IP_INTO_CATCH)
    CASE_HRESULT(CORDBG_E_SET_IP_NOT_ALLOWED_ON_NONLEAF_FRAME)
    CASE_HRESULT(CORDBG_E_SET_IP_IMPOSSIBLE)
    CASE_HRESULT(CORDBG_E_FUNC_EVAL_BAD_START_POINT)
    CASE_HRESULT(CORDBG_E_INVALID_OBJECT)
    CASE_HRESULT(CORDBG_E_FUNC_EVAL_NOT_COMPLETE)
    CASE_HRESULT(CORDBG_S_FUNC_EVAL_HAS_NO_RESULT)
    CASE_HRESULT(CORDBG_S_VALUE_POINTS_TO_VOID)
    CASE_HRESULT(CORDBG_S_FUNC_EVAL_ABORTED)
    CASE_HRESULT(CORDBG_E_STATIC_VAR_NOT_AVAILABLE)
    CASE_HRESULT(CORDBG_E_CANT_SETIP_INTO_OR_OUT_OF_FILTER)
    CASE_HRESULT(CORDBG_E_CANT_CHANGE_JIT_SETTING_FOR_ZAP_MODULE)
    CASE_HRESULT(CORDBG_E_CANT_SET_TO_JMC)
    CASE_HRESULT(CORDBG_E_BAD_THREAD_STATE)
    CASE_HRESULT(CORDBG_E_DEBUGGER_ALREADY_ATTACHED)
    CASE_HRESULT(CORDBG_E_SUPERFLOUS_CONTINUE)
    CASE_HRESULT(CORDBG_E_SET_VALUE_NOT_ALLOWED_ON_NONLEAF_FRAME)
    CASE_HRESULT(CORDBG_E_ENC_MODULE_NOT_ENC_ENABLED)
    CASE_HRESULT(CORDBG_E_SET_IP_NOT_ALLOWED_ON_EXCEPTION)
    CASE_HRESULT(CORDBG_E_VARIABLE_IS_ACTUALLY_LITERAL)
    CASE_HRESULT(CORDBG_E_PROCESS_DETACHED)
    CASE_HRESULT(CORDBG_E_ENC_CANT_ADD_FIELD_TO_VALUE_OR_LAYOUT_CLASS)
    CASE_HRESULT(CORDBG_E_FIELD_NOT_STATIC)
    CASE_HRESULT(CORDBG_E_FIELD_NOT_INSTANCE)
    CASE_HRESULT(CORDBG_E_ENC_JIT_CANT_UPDATE)
    CASE_HRESULT(CORDBG_E_ENC_INTERNAL_ERROR)
    CASE_HRESULT(CORDBG_E_ENC_HANGING_FIELD)
    CASE_HRESULT(CORDBG_E_MODULE_NOT_LOADED)
    CASE_HRESULT(CORDBG_E_UNABLE_TO_SET_BREAKPOINT)
    CASE_HRESULT(CORDBG_E_DEBUGGING_NOT_POSSIBLE)
    CASE_HRESULT(CORDBG_E_KERNEL_DEBUGGER_ENABLED)
    CASE_HRESULT(CORDBG_E_KERNEL_DEBUGGER_PRESENT)
    CASE_HRESULT(CORDBG_E_INCOMPATIBLE_PROTOCOL)
    CASE_HRESULT(CORDBG_E_TOO_MANY_PROCESSES)
    CASE_HRESULT(CORDBG_E_INTEROP_NOT_SUPPORTED)
    CASE_HRESULT(CORDBG_E_NO_REMAP_BREAKPIONT)
    CASE_HRESULT(CORDBG_E_OBJECT_NEUTERED)
    CASE_HRESULT(CORPROF_E_FUNCTION_NOT_COMPILED)
    CASE_HRESULT(CORPROF_E_DATAINCOMPLETE)
    CASE_HRESULT(CORPROF_E_FUNCTION_NOT_IL)
    CASE_HRESULT(CORPROF_E_NOT_MANAGED_THREAD)
    CASE_HRESULT(CORPROF_E_CALL_ONLY_FROM_INIT)
    CASE_HRESULT(CORPROF_E_NOT_YET_AVAILABLE)
    CASE_HRESULT(CLDB_E_INTERNALERROR)
    CASE_HRESULT(CORSEC_E_POLICY_EXCEPTION)
    CASE_HRESULT(CORSEC_E_MIN_GRANT_FAIL)
    CASE_HRESULT(CORSEC_E_NO_EXEC_PERM)
    CASE_HRESULT(CORSEC_E_XMLSYNTAX)
    CASE_HRESULT(CORSEC_E_INVALID_STRONGNAME)
    CASE_HRESULT(CORSEC_E_INVALID_IMAGE_FORMAT)
    CASE_HRESULT(CORSEC_E_CRYPTO)
    CASE_HRESULT(CORSEC_E_CRYPTO_UNEX_OPER)
    CASE_HRESULT(COR_E_APPLICATION)
    CASE_HRESULT(COR_E_ARGUMENTOUTOFRANGE)
    CASE_HRESULT(COR_E_ARITHMETIC)
    CASE_HRESULT(COR_E_ARRAYTYPEMISMATCH)
    CASE_HRESULT(COR_E_CONTEXTMARSHAL)
    CASE_HRESULT(COR_E_TIMEOUT)
    CASE_HRESULT(COR_E_DIVIDEBYZERO)
    CASE_HRESULT(COR_E_EXCEPTION)
    CASE_HRESULT(COR_E_EXECUTIONENGINE)
    CASE_HRESULT(COR_E_FIELDACCESS)
    CASE_HRESULT(COR_E_FORMAT)
    CASE_HRESULT(COR_E_BADIMAGEFORMAT)
    CASE_HRESULT(COR_E_ASSEMBLYEXPECTED)
    CASE_HRESULT(COR_E_TYPEUNLOADED)
    CASE_HRESULT(COR_E_INDEXOUTOFRANGE)
    CASE_HRESULT(COR_E_INVALIDOPERATION)
    CASE_HRESULT(COR_E_INVALIDPROGRAM)
    CASE_HRESULT(COR_E_MEMBERACCESS)
    CASE_HRESULT(COR_E_METHODACCESS)
    CASE_HRESULT(COR_E_MISSINGFIELD)
    CASE_HRESULT(COR_E_MISSINGMANIFESTRESOURCE)
    CASE_HRESULT(COR_E_MISSINGMEMBER)
    CASE_HRESULT(COR_E_MISSINGMETHOD)
    CASE_HRESULT(COR_E_MULTICASTNOTSUPPORTED)
    CASE_HRESULT(COR_E_NOTFINITENUMBER)
    CASE_HRESULT(COR_E_DUPLICATEWAITOBJECT)
    CASE_HRESULT(COR_E_PLATFORMNOTSUPPORTED)
    CASE_HRESULT(COR_E_NOTSUPPORTED)
    CASE_HRESULT(COR_E_OVERFLOW)
    CASE_HRESULT(COR_E_RANK)
    CASE_HRESULT(COR_E_SECURITY)
    CASE_HRESULT(COR_E_SERIALIZATION)
    CASE_HRESULT(COR_E_STACKOVERFLOW)
    CASE_HRESULT(COR_E_SYNCHRONIZATIONLOCK)
    CASE_HRESULT(COR_E_SYSTEM)
    CASE_HRESULT(COR_E_THREADABORTED)
    CASE_HRESULT(COR_E_THREADINTERRUPTED)
    CASE_HRESULT(COR_E_THREADSTATE)
    CASE_HRESULT(COR_E_THREADSTOP)
    CASE_HRESULT(COR_E_TYPEINITIALIZATION)
    CASE_HRESULT(COR_E_TYPELOAD)
    CASE_HRESULT(COR_E_ENTRYPOINTNOTFOUND)
    CASE_HRESULT(COR_E_DLLNOTFOUND)
    CASE_HRESULT(COR_E_VERIFICATION)
    CASE_HRESULT(COR_E_INVALIDCOMOBJECT)
    CASE_HRESULT(COR_E_MARSHALDIRECTIVE)
    CASE_HRESULT(COR_E_INVALIDOLEVARIANTTYPE)
    CASE_HRESULT(COR_E_SAFEARRAYTYPEMISMATCH)
    CASE_HRESULT(COR_E_SAFEARRAYRANKMISMATCH)
    CASE_HRESULT(COR_E_INVALIDFILTERCRITERIA)
    CASE_HRESULT(COR_E_REFLECTIONTYPELOAD)
    CASE_HRESULT(COR_E_TARGET)
    CASE_HRESULT(COR_E_TARGETINVOCATION)
    CASE_HRESULT(COR_E_CUSTOMATTRIBUTEFORMAT)
    CASE_HRESULT(COR_E_ENDOFSTREAM)
    CASE_HRESULT(COR_E_FILELOAD)
    CASE_HRESULT(COR_E_FILENOTFOUND)
    CASE_HRESULT(COR_E_IO)
    CASE_HRESULT(COR_E_DIRECTORYNOTFOUND)
    CASE_HRESULT(COR_E_PATHTOOLONG)
    CASE_HRESULT(COR_E_OBJECTDISPOSED)
    CASE_HRESULT(COR_E_NEWER_RUNTIME)
    CASE_HRESULT(CLR_E_SHIM_RUNTIMELOAD)
    CASE_HRESULT(VER_E_FIELD_SIG)
    CASE_HRESULT(CORDBG_E_THREAD_NOT_SCHEDULED)
#endif

        default:
            return NULL;
    }
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


// ---------------------------------------------------------------------------
// HRException class.  Implements exception API for exceptions from HRESULTS
// ---------------------------------------------------------------------------

HRESULT HRException::GetHR()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_hr;
}

// ---------------------------------------------------------------------------
// COMException class. - moved to COMEx.cpp
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// SEHException class.  Implements exception API for SEH exception info
// ---------------------------------------------------------------------------

HRESULT SEHException::GetHR()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsComPlusException(&m_exception)) // EE exception
        return (HRESULT) m_exception.ExceptionInformation[0];
    else
        return m_exception.ExceptionCode;
}

IErrorInfo *SEHException::GetErrorInfo()
{
    LIMITED_METHOD_CONTRACT;
    return NULL;
}

void SEHException::GetMessage(SString &string)
{
    WRAPPER_NO_CONTRACT;

    if (IsComPlusException(&m_exception)) // EE exception
    {
        GenerateTopLevelHRExceptionMessage(GetHR(), string);
    }
    else
    {
        if (m_exception.ExceptionCode != 0)
        {
            string.Printf("Exception code 0x%.8x", m_exception.ExceptionCode);
        }
        else
        {
            // If we don't have a valid exception code, then give a generic message that's a little nicer than
            // "code 0x00000000".
            string.Printf("Unknown exception");
        }
    }
}

//==============================================================================
// DelegatingException class.  Implements exception API for "foreign" exceptions.
//==============================================================================

DelegatingException::DelegatingException()
 : m_delegatedException((Exception*)DELEGATE_NOT_YET_SET)
{
    LIMITED_METHOD_DAC_CONTRACT;
} // DelegatingException::DelegatingException()

//------------------------------------------------------------------------------
DelegatingException::~DelegatingException()
{
    WRAPPER_NO_CONTRACT;

    // If there is a valid delegate pointer (inited and non-NULL), delete it.
    if (IsDelegateValid())
        Delete(m_delegatedException);

    // Avoid confusion.
    m_delegatedException = NULL;
} // DelegatingException::~DelegatingException()

//------------------------------------------------------------------------------
// Retrieve the delegating exception, or get one from the Thread, or get NULL.
Exception* DelegatingException::GetDelegate()
{
    WRAPPER_NO_CONTRACT;

    // If we haven't gotten the exception pointer before..
    if (!IsDelegateSet())
    {
        // .. get it now.  NULL in case there isn't one and we take default action.
        m_delegatedException = NULL;
        GetLastThrownObjectExceptionFromThread(&m_delegatedException);
    }

    return m_delegatedException;
} // Exception* DelegatingException::GetDelegate()

//------------------------------------------------------------------------------
// Virtual overrides
HRESULT DelegatingException::GetHR()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    // Retrieve any delegating exception.
    Exception *pDelegate = GetDelegate();

    // If there is a delegate exception, defer to it.  Otherwise,
    //  default to E_FAIL.
    return pDelegate ? pDelegate->GetHR() : E_FAIL;

} // HRESULT DelegatingException::GetHR()

//------------------------------------------------------------------------------
IErrorInfo *DelegatingException::GetErrorInfo()
{
    WRAPPER_NO_CONTRACT;

    // Retrieve any delegating exception.
    Exception *pDelegate = GetDelegate();

    // If there is a delegate exception, defer to it.  Otherwise,
    //  default to NULL.
    return pDelegate ? pDelegate->GetErrorInfo() : NULL;

} // IErrorInfo *DelegatingException::GetErrorInfo()

//------------------------------------------------------------------------------
void DelegatingException::GetMessage(SString &result)
{
    WRAPPER_NO_CONTRACT;

    // Retrieve any delegating exception.
    Exception *pDelegate = GetDelegate();

    // If there is a delegate exception, defer to it.  Otherwise,
    //  default to a generic message.
    if (pDelegate)
    {
        pDelegate->GetMessage(result);
    }
    else
    {
        // If we don't have a valid exception code, then give a generic message
        //  that's a little nicer than "code 0x00000000".
        result.Printf("Unknown exception");
    }
} // void DelegatingException::GetMessage()

//------------------------------------------------------------------------------
Exception *DelegatingException::Clone()
{
    WRAPPER_NO_CONTRACT;

    // Clone the base exception, this will also take care of cloning the inner
    // exception if there is one.
    NewHolder<DelegatingException> retExcep((DelegatingException*)Exception::Clone());

    // If there is a valid delegating exception...
    if (IsDelegateValid())
    {   // ... clone it.
        retExcep->m_delegatedException = m_delegatedException->Clone();
    }
    else
    {   // ... but if there is not, just copy -- either NULL or DELEGATE_NOT_YET_SET
        retExcep->m_delegatedException = m_delegatedException;
    }

    retExcep.SuppressRelease();
    return retExcep;
} // virtual Exception *DelegatingException::Clone()

//==============================================================================
//==============================================================================

void DECLSPEC_NORETURN ThrowHR(HRESULT hr)
{
    WRAPPER_NO_CONTRACT;

    STRESS_LOG1(LF_EH, LL_INFO100, "ThrowHR: HR = %x\n", hr);

    if (hr == E_OUTOFMEMORY)
        ThrowOutOfMemory();

    // Catchers assume only failing hresults
    _ASSERTE(FAILED(hr));
    if (hr == S_OK)
        hr = E_FAIL;

    EX_THROW(HRException, (hr));
}

void DECLSPEC_NORETURN ThrowHR(HRESULT hr, SString const &msg)
{
    WRAPPER_NO_CONTRACT;

    STRESS_LOG1(LF_EH, LL_INFO100, "ThrowHR: HR = %x\n", hr);

    if (hr == E_OUTOFMEMORY)
        ThrowOutOfMemory();

    // Catchers assume only failing hresults
    _ASSERTE(FAILED(hr));
    if (hr == S_OK)
        hr = E_FAIL;

    EX_THROW(HRMsgException, (hr, msg));
}

void DECLSPEC_NORETURN ThrowHR(HRESULT hr, UINT uText)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    if (hr == E_OUTOFMEMORY)
        ThrowOutOfMemory();

    // Catchers assume only failing hresults
    _ASSERTE(FAILED(hr));
    if (hr == S_OK)
        hr = E_FAIL;

    SString sExceptionText;

    // We won't check the return value here. If it fails, we'll just
    // throw the HR
    sExceptionText.LoadResource(CCompRC::Error, uText);

    EX_THROW(HRMsgException, (hr, sExceptionText));
}

void DECLSPEC_NORETURN ThrowWin32(DWORD err)
{
    WRAPPER_NO_CONTRACT;
    if (err == ERROR_NOT_ENOUGH_MEMORY)
    {
        ThrowOutOfMemory();
    }
    else
    {
        ThrowHR(HRESULT_FROM_WIN32(err));
    }
}

void DECLSPEC_NORETURN ThrowLastError()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    ThrowWin32(GetLastError());
}

void DECLSPEC_NORETURN ThrowOutOfMemory()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

    // Use volatile store to prevent compiler from optimizing the static variable away
    VolatileStoreWithoutBarrier<HRESULT>(&g_hrFatalError, COR_E_OUTOFMEMORY);

    // Regular CLR builds - throw our pre-created OOM exception object
    PAL_CPP_THROW(Exception *, Exception::GetOOMException());

#else

    // DAC builds - raise a DacError
    DacError(E_OUTOFMEMORY);

    // DacError always throws but isn't marked DECLSPEC_NORETURN so we have to
    // tell the compiler that this code is unreachable. We could mark DacError
    // (and DacNotImpl) as DECLSPEC_NORETURN, but then we've have to update a
    // lot of code where we do something afterwards. Also, due to inlining,
    // we'd sometimes have to change functions which call functions that only
    // call DacNotImpl. I have these changes in a bbpack and some of them look
    // nice, but I'm not sure if it's worth the risk of merge conflicts.
    UNREACHABLE();

#endif
}

#include "corexcep.h"

//--------------------------------------------------------------------------------
// Helper for EX_THROW_WITH_INNER()
//
// Clones an exception into the current domain. Also handles special cases for
// OOM and other stuff. Making this a function so we don't inline all this logic
// every place we call EX_THROW_WITH_INNER.
//
// If the "inner" is a transient exception such as OOM or ThreadAbort, this function
// will just throw it rather than allow it to be wrapped in another exception.
//--------------------------------------------------------------------------------
Exception *ExThrowWithInnerHelper(Exception *inner)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    // Yes, NULL is a legal case. Makes it easier to author uniform helpers for
    // both wrapped and normal exceptions.
    if (inner == NULL)
    {
        return NULL;
    }

    if (inner == Exception::GetOOMException())
    {
        // We don't want to do allocations if we're already throwing an OOM!
        PAL_CPP_THROW(Exception*, inner);
    }

    inner = inner->DomainBoundClone();

    // It isn't useful to wrap OOMs and StackOverflows in other exceptions. Just throw them now.
    //
    if (inner->IsTransient())
    {
        PAL_CPP_THROW(Exception*, inner);
    }
    return inner;
}

#ifdef _DEBUG

#ifdef _MSC_VER
#pragma optimize("", off)
#endif // _MSC_VER

void ExThrowTrap(const char *fcn, const char *file, int line, const char *szType, HRESULT hr, const char *args)
{
    SUPPORTS_DAC;
    return;
}

#ifdef _MSC_VER
#pragma optimize("", on)
#endif // _MSC_VER

#endif




//-------------------------------------------------------------------------------------------
// This routine will generate the most descriptive possible error message for an hresult.
// It will generate at minimum the hex value. It will also try to generate the symbolic name
// (E_POINTER) and the friendly description (from the message tables.)
//
// bNoGeekStuff suppresses hex HR codes. Use this sparingly as most error strings generated by the
// CLR are aimed at developers, not end-users.
//-------------------------------------------------------------------------------------------
void GetHRMsg(HRESULT hr, SString &result, BOOL bNoGeekStuff/* = FALSE*/)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
    }
    CONTRACTL_END;

    result = W("");     // Make sure this routine isn't an inadvertent data-leak exploit!



    SString strDescr;
    BOOL    fHaveDescr = FALSE;

    if (FAILED(hr) && HRESULT_FACILITY(hr) == FACILITY_URT && HRESULT_CODE(hr) < MAX_URT_HRESULT_CODE)
    {
        fHaveDescr = strDescr.LoadResource(CCompRC::Error, MSG_FOR_URT_HR(hr));
    }
    else
    {
        DWORD dwFlags = FORMAT_MESSAGE_FROM_SYSTEM;
        dwFlags |= FORMAT_MESSAGE_MAX_WIDTH_MASK;

        fHaveDescr = strDescr.FormatMessage(dwFlags, 0, hr, 0);
    }

    LPCSTR name = Exception::GetHRSymbolicName(hr);

    // If we can't get a resource string, print the hresult regardless.
    if (!fHaveDescr)
    {
        bNoGeekStuff = FALSE;
    }

    if (fHaveDescr)
    {
        result.Append(strDescr);
    }

    if (!bNoGeekStuff)
    {
        if (fHaveDescr)
        {
            result.Append(W(" ("));
        }

        result.AppendPrintf(W("0x%.8X"), hr);
        if (name != NULL)
        {
            result.AppendPrintf(W(" (%S)"), name);
        }

        if (fHaveDescr)
        {
            result.Append(W(")"));
        }
    }
}


//-------------------------------------------------------------------------------------------
// Similar to GetHRMsg but phrased for top-level exception message.
//-------------------------------------------------------------------------------------------
void GenerateTopLevelHRExceptionMessage(HRESULT hresult, SString &result)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
    }
    CONTRACTL_END;

    result = W("");     // Make sure this routine isn't an inadvertent data-leak exploit!

    GetHRMsg(hresult, result);
}

//===========================================================================================
// These abstractions hide the difference between legacy desktop CLR's (that don't support
// side-by-side-inproc and rely on a fixed SEH code to identify managed exceptions) and
// new CLR's that support side-by-side inproc.
//
// The new CLR's use a different set of SEH codes to avoid conflicting with the legacy CLR's.
// In addition, to distinguish between EH's raised by different inproc instances of the CLR,
// the module handle of the owning CLR is stored in ExceptionRecord.ExceptionInformation[4].
//
// (Note: all existing SEH's use either only slot [0] or no slots at all. We are leaving
//  slots [1] thru [3] open for future expansion.)
//===========================================================================================

// Is this exception code one of the special CLR-specific SEH codes that participate in the
// instance-tagging scheme?
BOOL IsInstanceTaggedSEHCode(DWORD dwExceptionCode)
{
   LIMITED_METHOD_DAC_CONTRACT;

    return dwExceptionCode == EXCEPTION_COMPLUS;
}

// This set of overloads generates the NumberParameters and ExceptionInformation[] array to
// pass to RaiseException().
//
// Parameters:
//    exceptionArgs:   a fixed-size array of size INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE.
//                     This will get filled in by this function. (The module handle goes
//                     in the last slot if this is a side-by-side-inproc enabled build.)
//
//    exceptionArg1... up to four arguments that go in slots [0]..[3]. These depends
//                     the specific requirements of your exception code.
//
// Returns:
//    The NumberParameters to pass to RaiseException().
//
//    Basically, this is  either INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE or the count of your
//    fixed arguments depending on whether this tagged-SEH-enabled build.
//
// This function is not permitted to fail.

// (the existing system can support more overloads up to 4 fixed arguments but we don't need them at this time.)

static DWORD MarkAsThrownByUsWorker(UINT numArgs, /*out*/ ULONG_PTR exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE], ULONG_PTR arg0 = 0)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    _ASSERTE(numArgs < INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE);
    FillMemory(exceptionArgs, sizeof(ULONG_PTR) * INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE, 0);

    exceptionArgs[0] = arg0;

#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
    exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE - 1] = (ULONG_PTR)GetClrModuleBase();
#endif // !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)

    return INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE;
}

DWORD MarkAsThrownByUs(/*out*/ ULONG_PTR exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE])
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    return MarkAsThrownByUsWorker(0, exceptionArgs);
}

DWORD MarkAsThrownByUs(/*out*/ ULONG_PTR exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE], ULONG_PTR arg0)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    return MarkAsThrownByUsWorker(1, exceptionArgs, arg0);
}

// Given an exception record, checks if it's exception code matches a specific exception code
// *and* whether it was tagged by the calling instance of the CLR.
//
// If this is a non-tagged-SEH-enabled build, it is blindly assumed to be tagged by the
// calling instance of the CLR.
BOOL WasThrownByUs(const EXCEPTION_RECORD *pcER, DWORD dwExceptionCode)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    _ASSERTE(IsInstanceTaggedSEHCode(dwExceptionCode));
    _ASSERTE(pcER != NULL);
    if (dwExceptionCode != pcER->ExceptionCode)
    {
        return FALSE;
    }

    if (pcER->NumberParameters != INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE)
    {
        return FALSE;
    }
#if!defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
    if ((ULONG_PTR)GetClrModuleBase() != pcER->ExceptionInformation[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE - 1] )
    {
        return FALSE;
    }
    return TRUE;
#else // !(!defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
    return FALSE;
#endif // !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
}



//-----------------------------------------------------------------------------------
// The following group wraps the basic abstracts specifically for EXCEPTION_COMPLUS.
//-----------------------------------------------------------------------------------
BOOL IsComPlusException(const EXCEPTION_RECORD *pcER)
{
    STATIC_CONTRACT_WRAPPER;

    return WasThrownByUs(pcER, EXCEPTION_COMPLUS);
}

VOID RaiseComPlusException()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    ULONG_PTR exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE];
    DWORD     numParams = MarkAsThrownByUs(exceptionArgs);
    RaiseException(EXCEPTION_COMPLUS, 0, numParams, exceptionArgs);
}

//===========================================================================================
//===========================================================================================
