// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** File: DebugDebugger.cpp
**
** Purpose: Native methods on System.Debug.Debugger
**
**

===========================================================*/

#include "common.h"

#include <object.h>
#include "ceeload.h"

#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "field.h"
#include "gcheaputilities.h"
#include "jitinterface.h"
#include "debugdebugger.h"
#include "dbginterface.h"
#include "cordebug.h"
#include "corsym.h"
#include "generics.h"
#include "stackwalk.h"

#define PORTABLE_PDB_MINOR_VERSION              20557
#define IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB  17

#ifndef DACCESS_COMPILE
//----------------------------------------------------------------------------
//
// FindMostRecentUserCodeOnStack - find out the most recent user managed code on stack
//
//
// Arguments:
//    pContext - [optional] pointer to the context to be restored the user code's context if found
//
// Return Value:
//    The most recent user managed code or NULL if not found.
//
// Note:
//    It is a heuristic approach to get the address of the user managed code that calls into
//    BCL like System.Diagnostics.Debugger.Break assuming that we can find the original user
//    code caller with stack walking.
//
//    DoWatsonForUserBreak has the address returned from the helper frame that points to an
//    internal BCL helpful function doing permission check.  From bucketing perspetive it is
//    more preferable to report the user managed code that invokes Debugger.Break instead.
//
//    User managed code is managed code in non-system assembly.   Currently, only CoreLib
//    is marked as system assembly.
//
//----------------------------------------------------------------------------
UINT_PTR FindMostRecentUserCodeOnStack(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    Thread * pThread = GetThread();
    UINT_PTR address = NULL;

    CONTEXT ctx;
    REGDISPLAY rd;
    SetUpRegdisplayForStackWalk(pThread, &ctx, &rd);

    StackFrameIterator frameIter;
    frameIter.Init(pThread, pThread->GetFrame(), &rd, FUNCTIONSONLY | LIGHTUNWIND);

    while (frameIter.IsValid())
    {
        MethodDesc * pMD = frameIter.m_crawl.GetFunction();

        // Is it not a system assembly?  User manged user will not be in system assembly.
        if ((pMD != NULL) && (!pMD->GetAssembly()->IsSystem()))
        {
            CrawlFrame * pCF = &(frameIter.m_crawl);
            address = (UINT_PTR)GetControlPC(pCF->GetRegisterSet());
            break;
        }

        if (frameIter.Next() != SWA_CONTINUE)
        {
            break;
        }
    }

    return address;
}


// This does a user break, triggered by System.Diagnostics.Debugger.Break, or the IL opcode for break.
//
// Notes:
//    If a managed debugger is attached, this should send the managed UserBreak event.
//    Else if a native debugger is attached, this should send a native break event (kernel32!DebugBreak)
//    Else, this should invoke Watson.
//
// Historical trivia:
// - In whidbey, this would still invoke Watson if a native-only debugger is attached.
// - In arrowhead, the managed debugging pipeline switched to be built on the native pipeline.
FCIMPL0(void, DebugDebugger::Break)
{
    FCALL_CONTRACT;

#ifdef DEBUGGING_SUPPORTED
    HELPER_METHOD_FRAME_BEGIN_0();

#ifdef _DEBUG
    {
        static int fBreakOnDebugBreak = -1;
        if (fBreakOnDebugBreak == -1)
            fBreakOnDebugBreak = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnDebugBreak);
        _ASSERTE(fBreakOnDebugBreak == 0 && "BreakOnDebugBreak");
    }

    static BOOL fDbgInjectFEE = -1;
    if (fDbgInjectFEE == -1)
        fDbgInjectFEE = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgInjectFEE);
#endif

    // WatsonLastChance has its own complex (and changing) policy of how to behave if a debugger is attached.
    // So caller should explicitly enforce any debugger-related policy before handing off to watson.
    // Check managed-only first, since managed debugging may be built on native-debugging.
    if (CORDebuggerAttached() INDEBUG(|| fDbgInjectFEE))
    {
        // A managed debugger is already attached -- let it handle the event.
        g_pDebugInterface->SendUserBreakpoint(GetThread());
    }
    else if (IsDebuggerPresent())
    {
        // No managed debugger, but a native debug is attached. Explicitly fire a native user breakpoint.
        // Don't rely on Watson support since that may have a different policy.

        // Toggle to preemptive before firing the debug event. This allows the debugger to suspend this
        // thread at the debug event.
        GCX_PREEMP();

        // This becomes an unmanaged breakpoint, such as int 3.
        DebugBreak();
    }
    else
    {
    }

    HELPER_METHOD_FRAME_END();
#endif // DEBUGGING_SUPPORTED
}
FCIMPLEND

extern "C" BOOL QCALLTYPE DebugDebugger_Launch()
{
    QCALL_CONTRACT;

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerAttached())
    {
        return TRUE;
    }

    if (g_pDebugInterface != NULL)
    {
        HRESULT hr = g_pDebugInterface->LaunchDebuggerForUser(GetThread(), NULL, TRUE, TRUE);
        return SUCCEEDED(hr);
    }
#endif // DEBUGGING_SUPPORTED

    return FALSE;
}

FCIMPL0(FC_BOOL_RET, DebugDebugger::IsDebuggerAttached)
{
    FCALL_CONTRACT;

    FC_GC_POLL_RET();

#ifdef DEBUGGING_SUPPORTED
    FC_RETURN_BOOL(CORDebuggerAttached());
#else // DEBUGGING_SUPPORTED
    FC_RETURN_BOOL(FALSE);
#endif
}
FCIMPLEND

namespace
{
    BOOL IsLoggingHelper()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

    #ifdef DEBUGGING_SUPPORTED
        if (CORDebuggerAttached())
        {
            return (g_pDebugInterface->IsLoggingEnabled());
        }
    #endif // DEBUGGING_SUPPORTED
        return FALSE;
    }
}


// Log to managed debugger.
// It will send a managed log event, which will faithfully send the two string parameters here without
// appending a newline to anything.
// It will also call OutputDebugString() which will send a native debug event. The message
// string there will be a composite of the two managed string parameters and may include a newline.
extern "C" void QCALLTYPE DebugDebugger_Log(INT32 Level, PCWSTR pwzModule, PCWSTR pwzMessage)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pwzModule, NULL_OK));
        PRECONDITION(CheckPointer(pwzMessage, NULL_OK));
    }
    CONTRACTL_END;

    // OutputDebugString will log to native/interop debugger.
    if (pwzModule != NULL)
    {
        WszOutputDebugString(pwzModule);
        WszOutputDebugString(W(" : "));
    }

    if (pwzMessage != NULL)
    {
        WszOutputDebugString(pwzMessage);
    }

    // If we're not logging a module prefix, then don't log the newline either.
    // Thus if somebody is just logging messages, there won't be any extra newlines in there.
    // If somebody is also logging category / module information, then this call to OutputDebugString is
    // already prepending that to the message, so we append a newline for readability.
    if (pwzModule != NULL)
    {
        WszOutputDebugString(W("\n"));
    }


#ifdef DEBUGGING_SUPPORTED

    // Send message for logging only if the
    // debugger is attached and logging is enabled
    // for the given category
    if (CORDebuggerAttached())
    {
        if (IsLoggingHelper() )
        {
            // Copy log message and category into our own SString to protect against GC
            // Strings may contain embedded nulls, but we need to handle null-terminated
            // strings, so use truncate now.
            StackSString switchName;
            if (pwzModule != NULL)
            {
                // truncate if necessary
                COUNT_T iLen = (COUNT_T) u16_strlen(pwzModule);
                if (iLen > MAX_LOG_SWITCH_NAME_LEN)
                {
                    iLen = MAX_LOG_SWITCH_NAME_LEN;
                }
                switchName.Set(pwzModule, iLen);
            }

            SString message;
            if (pwzMessage != NULL)
            {
                message.Set(pwzMessage, (COUNT_T) u16_strlen(pwzMessage));
            }

            g_pDebugInterface->SendLogMessage (Level, &switchName, &message);
        }
    }

#endif // DEBUGGING_SUPPORTED
}

FCIMPL0(FC_BOOL_RET, DebugDebugger::IsLogging)
{
    FCALL_CONTRACT;

    FC_GC_POLL_RET();

    FC_RETURN_BOOL(IsLoggingHelper());
}
FCIMPLEND

FCIMPL4(void, DebugStackTrace::GetStackFramesInternal,
        StackFrameHelper* pStackFrameHelperUNSAFE,
        INT32 iSkip,
        CLR_BOOL fNeedFileInfo,
        Object* pExceptionUNSAFE
       )
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pStackFrameHelperUNSAFE));
        PRECONDITION(CheckPointer(pExceptionUNSAFE, NULL_OK));
    }
    CONTRACTL_END;

    STACKFRAMEHELPERREF pStackFrameHelper   = (STACKFRAMEHELPERREF)ObjectToOBJECTREF(pStackFrameHelperUNSAFE);
    OBJECTREF           pException          = ObjectToOBJECTREF(pExceptionUNSAFE);
    PTRARRAYREF         dynamicMethodArrayOrig = NULL;

    HELPER_METHOD_FRAME_BEGIN_2(pStackFrameHelper, pException);

    GCPROTECT_BEGIN(dynamicMethodArrayOrig);

    ASSERT(iSkip >= 0);

    GetStackFramesData data;

    data.pDomain = GetAppDomain();

    data.skip = iSkip;

    data.NumFramesRequested = pStackFrameHelper->iFrameCount;

    if (pException == NULL)
    {
        // Thread is NULL if it's the current thread.
        data.TargetThread = pStackFrameHelper->targetThread;
        GetStackFrames(NULL, (void*)-1, &data);
    }
    else
    {
        // We also fetch the dynamic method array in a GC protected artifact to ensure
        // that the resolver objects, if any, are kept alive incase the exception object
        // is thrown again (resetting the dynamic method array reference in the object)
        // that may result in resolver objects getting collected before they can be reachable again
        // (from the code below).
        GetStackFramesFromException(&pException, &data, &dynamicMethodArrayOrig);
    }

    if (data.cElements != 0)
    {
#if defined(FEATURE_ISYM_READER) && defined(FEATURE_COMINTEROP)
        if (fNeedFileInfo)
        {
             // Calls to COM up ahead.
            EnsureComStarted();
        }
#endif // FEATURE_ISYM_READER && FEATURE_COMINTEROP

        // Allocate memory for the MethodInfo objects
        BASEARRAYREF methodInfoArray = (BASEARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgMethodHandle), (OBJECTREF)methodInfoArray);

        // Allocate memory for the Offsets
        OBJECTREF offsets = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiOffset), (OBJECTREF)offsets);

        // Allocate memory for the ILOffsets
        OBJECTREF ilOffsets = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiILOffset), (OBJECTREF)ilOffsets);

        // Allocate memory for the array of assembly file names
        PTRARRAYREF assemblyPathArray = (PTRARRAYREF) AllocateObjectArray(data.cElements, g_pStringClass);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgAssemblyPath), (OBJECTREF)assemblyPathArray);

        // Allocate memory for the array of assemblies
        PTRARRAYREF assemblyArray = (PTRARRAYREF) AllocateObjectArray(data.cElements, g_pObjectClass);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgAssembly), (OBJECTREF)assemblyArray);

        // Allocate memory for the LoadedPeAddress
        BASEARRAYREF loadedPeAddressArray = (BASEARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgLoadedPeAddress), (OBJECTREF)loadedPeAddressArray);

        // Allocate memory for the LoadedPeSize
        OBJECTREF loadedPeSizeArray = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiLoadedPeSize), (OBJECTREF)loadedPeSizeArray);

        // Allocate memory for the IsFileLayout flags
        OBJECTREF isFileLayouts = AllocatePrimitiveArray(ELEMENT_TYPE_BOOLEAN, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiIsFileLayout), (OBJECTREF)isFileLayouts);

        // Allocate memory for the InMemoryPdbAddress
        BASEARRAYREF inMemoryPdbAddressArray = (BASEARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgInMemoryPdbAddress), (OBJECTREF)inMemoryPdbAddressArray);

        // Allocate memory for the InMemoryPdbSize
        OBJECTREF inMemoryPdbSizeArray = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiInMemoryPdbSize), (OBJECTREF)inMemoryPdbSizeArray);

        // Allocate memory for the MethodTokens
        OBJECTREF methodTokens = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiMethodToken), (OBJECTREF)methodTokens);

        // Allocate memory for the Filename string objects
        PTRARRAYREF filenameArray = (PTRARRAYREF) AllocateObjectArray(data.cElements, g_pStringClass);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgFilename), (OBJECTREF)filenameArray);

        // Allocate memory for the LineNumbers
        OBJECTREF lineNumbers = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiLineNumber), (OBJECTREF)lineNumbers);

        // Allocate memory for the ColumnNumbers
        OBJECTREF columnNumbers = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiColumnNumber), (OBJECTREF)columnNumbers);

        // Allocate memory for the flag indicating if this frame represents the last one from a foreign
        // exception stack trace provided we have any such frames. Otherwise, set it to null.
        // When StackFrameHelper.IsLastFrameFromForeignExceptionStackTrace is invoked in managed code,
        // it will return false for the null case.
        //
        // This is an optimization for us to not allocate the BOOL array if we do not have any frames
        // from a foreign stack trace.
        OBJECTREF IsLastFrameFromForeignStackTraceFlags = NULL;
        if (data.fDoWeHaveAnyFramesFromForeignStackTrace)
        {
            IsLastFrameFromForeignStackTraceFlags = AllocatePrimitiveArray(ELEMENT_TYPE_BOOLEAN, data.cElements);

            SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiLastFrameFromForeignExceptionStackTrace), (OBJECTREF)IsLastFrameFromForeignStackTraceFlags);
        }
        else
        {
            SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->rgiLastFrameFromForeignExceptionStackTrace), NULL);
        }

        // Determine if there are any dynamic methods in the stack trace.  If there are,
        // allocate an ObjectArray large enough to hold an ObjRef to each one.
        unsigned iNumDynamics = 0;
        unsigned iCurDynamic = 0;
        for (int iElement=0; iElement < data.cElements; iElement++)
        {
            MethodDesc *pMethod = data.pElements[iElement].pFunc;
            if (pMethod->IsLCGMethod())
            {
                iNumDynamics++;
            }
            else
            if (pMethod->GetMethodTable()->Collectible())
            {
                iNumDynamics++;
            }
        }

        if (iNumDynamics)
        {
            PTRARRAYREF dynamicDataArray = (PTRARRAYREF) AllocateObjectArray(iNumDynamics, g_pObjectClass);
            SetObjectReference( (OBJECTREF *)&(pStackFrameHelper->dynamicMethods), (OBJECTREF)dynamicDataArray);
        }

        int iNumValidFrames = 0;
        for (int i = 0; i < data.cElements; i++)
        {
            MethodDesc* pFunc = data.pElements[i].pFunc;

            // Method handle
            size_t *pElem = (size_t*)pStackFrameHelper->rgMethodHandle->GetDataPtr();
            pElem[iNumValidFrames] = (size_t)pFunc;

            // Native offset
            CLR_I4 *pI4 = (CLR_I4 *)((I4ARRAYREF)pStackFrameHelper->rgiOffset)->GetDirectPointerToNonObjectElements();
            pI4[iNumValidFrames] = data.pElements[i].dwOffset;

            // IL offset
            CLR_I4 *pILI4 = (CLR_I4 *)((I4ARRAYREF)pStackFrameHelper->rgiILOffset)->GetDirectPointerToNonObjectElements();
            pILI4[iNumValidFrames] = data.pElements[i].dwILOffset;

            // Assembly
            OBJECTREF pAssembly = pFunc->GetAssembly()->GetExposedObject();
            pStackFrameHelper->rgAssembly->SetAt(iNumValidFrames, pAssembly);

            if (data.fDoWeHaveAnyFramesFromForeignStackTrace)
            {
                // Set the BOOL indicating if the frame represents the last frame from a foreign exception stack trace.
                CLR_U1 *pIsLastFrameFromForeignExceptionStackTraceU1 = (CLR_U1 *)((BOOLARRAYREF)pStackFrameHelper->rgiLastFrameFromForeignExceptionStackTrace)
                                            ->GetDirectPointerToNonObjectElements();
                pIsLastFrameFromForeignExceptionStackTraceU1 [iNumValidFrames] = (CLR_U1)(data.pElements[i].flags & STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE);
            }

            MethodDesc *pMethod = data.pElements[i].pFunc;

            // If there are any dynamic methods, and this one is one of them, store
            // a reference to it's managed resolver to keep it alive.
            if (iNumDynamics)
            {
                if (pMethod->IsLCGMethod())
                {
                    DynamicMethodDesc *pDMD = pMethod->AsDynamicMethodDesc();
                    OBJECTREF pResolver = pDMD->GetLCGMethodResolver()->GetManagedResolver();
                    _ASSERTE(pResolver != NULL);

                    ((PTRARRAYREF)pStackFrameHelper->dynamicMethods)->SetAt(iCurDynamic++, pResolver);
                }
                else if (pMethod->GetMethodTable()->Collectible())
                {
                    OBJECTREF pLoaderAllocator = pMethod->GetMethodTable()->GetLoaderAllocator()->GetExposedObject();
                    _ASSERTE(pLoaderAllocator != NULL);
                    ((PTRARRAYREF)pStackFrameHelper->dynamicMethods)->SetAt(iCurDynamic++, pLoaderAllocator);
                }
            }

            Module *pModule = pMethod->GetModule();

            // If it's an EnC method, then don't give back any line info, b/c the PDB is out of date.
            // (We're using the stale PDB, not one w/ Edits applied).
            // Since the MethodDesc is always the most recent, v1 instances of EnC methods on the stack
            // will appeared to be Enc. This means we err on the side of not showing line numbers for EnC methods.
            // If any method in the file was changed, then our line numbers could be wrong. Since we don't
            // have updated PDBs from EnC, we can at best look at the module's version number as a rough guess
            // to if this file has been updated.
            bool fIsEnc = false;
#ifdef FEATURE_METADATA_UPDATER
            if (pModule->IsEditAndContinueEnabled())
            {
                EditAndContinueModule *eacm = (EditAndContinueModule *)pModule;
                if (eacm->GetApplyChangesCount() != 1)
                {
                    fIsEnc = true;
                }
            }
#endif
            // Check if the user wants the filenumber, linenumber info and that it is possible.
            if (!fIsEnc && fNeedFileInfo)
            {
#ifdef FEATURE_ISYM_READER
                BOOL fPortablePDB = FALSE;
                // We are checking if the PE image's debug directory contains a portable or embedded PDB because
                // the native diasymreader's portable PDB support has various bugs (crashes on certain PDBs) and
                // limitations (doesn't support in-memory or embedded PDBs).
                if (pModule->GetPEAssembly()->HasLoadedPEImage())
                {
                    PEDecoder* pe = pModule->GetPEAssembly()->GetLoadedLayout();
                    IMAGE_DATA_DIRECTORY* debugDirectoryEntry = pe->GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG);
                    if (debugDirectoryEntry != nullptr)
                    {
                        IMAGE_DEBUG_DIRECTORY* debugDirectory = (IMAGE_DEBUG_DIRECTORY*)pe->GetDirectoryData(debugDirectoryEntry);
                        if (debugDirectory != nullptr)
                        {
                            size_t nbytes = 0;
                            while (nbytes < debugDirectoryEntry->Size)
                            {
                                if ((debugDirectory->Type == IMAGE_DEBUG_TYPE_CODEVIEW && debugDirectory->MinorVersion == PORTABLE_PDB_MINOR_VERSION) ||
                                    (debugDirectory->Type == IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB))
                                {
                                    fPortablePDB = TRUE;
                                    break;
                                }
                                debugDirectory++;
                                nbytes += sizeof(*debugDirectory);
                            }
                        }
                    }
                }
                if (!fPortablePDB)
                {
                    // We didn't see a portable PDB in the debug directory but to just make sure we defensively assume that is
                    // portable and if the diasymreader doesn't exist or fails, we go down the portable PDB path.
                    fPortablePDB = TRUE;

                    BOOL fFileInfoSet = FALSE;
                    ULONG32 sourceLine = 0;
                    ULONG32 sourceColumn = 0;
                    WCHAR wszFileName[MAX_LONGPATH];
                    ULONG32 fileNameLength = 0;
                    {
                        // Note: we need to enable preemptive GC when accessing the unmanages symbol store.
                        GCX_PREEMP();

                        // Note: we use the NoThrow version of GetISymUnmanagedReader. If getting the unmanaged
                        // reader fails, then just leave the pointer NULL and leave any symbol info off of the
                        // stack trace.
                        ReleaseHolder<ISymUnmanagedReader> pISymUnmanagedReader(
                            pModule->GetISymUnmanagedReaderNoThrow());

                        if (pISymUnmanagedReader != NULL)
                        {
                            // Found a ISymUnmanagedReader for the regular PDB so don't attempt to
                            // read it as a portable PDB in CoreLib's StackFrameHelper.
                            fPortablePDB = FALSE;

                            ReleaseHolder<ISymUnmanagedMethod> pISymUnmanagedMethod;
                            HRESULT hr = pISymUnmanagedReader->GetMethod(pMethod->GetMemberDef(),
                                                                         &pISymUnmanagedMethod);

                            if (SUCCEEDED(hr))
                            {
                                // get all the sequence points and the documents
                                // associated with those sequence points.
                                // from the doument get the filename using GetURL()
                                ULONG32 SeqPointCount = 0;
                                ULONG32 RealSeqPointCount = 0;

                                hr = pISymUnmanagedMethod->GetSequencePointCount(&SeqPointCount);
                                _ASSERTE (SUCCEEDED(hr) || (hr == E_OUTOFMEMORY) );

                                if (SUCCEEDED(hr) && SeqPointCount > 0)
                                {
                                    // allocate memory for the objects to be fetched
                                    NewArrayHolder<ULONG32> offsets    (new (nothrow) ULONG32 [SeqPointCount]);
                                    NewArrayHolder<ULONG32> lines      (new (nothrow) ULONG32 [SeqPointCount]);
                                    NewArrayHolder<ULONG32> columns    (new (nothrow) ULONG32 [SeqPointCount]);
                                    NewArrayHolder<ULONG32> endlines   (new (nothrow) ULONG32 [SeqPointCount]);
                                    NewArrayHolder<ULONG32> endcolumns (new (nothrow) ULONG32 [SeqPointCount]);

                                    // we free the array automatically, but we have to manually call release
                                    // on each element in the array when we're done with it.
                                    NewArrayHolder<ISymUnmanagedDocument*> documents (
                                        (ISymUnmanagedDocument **)new PVOID [SeqPointCount]);

                                    if ((offsets && lines && columns && documents && endlines && endcolumns))
                                    {
                                        hr = pISymUnmanagedMethod->GetSequencePoints (
                                                            SeqPointCount,
                                                            &RealSeqPointCount,
                                                            offsets,
                                                            (ISymUnmanagedDocument **)documents,
                                                            lines,
                                                            columns,
                                                            endlines,
                                                            endcolumns);

                                        _ASSERTE(SUCCEEDED(hr) || (hr == E_OUTOFMEMORY) );

                                        if (SUCCEEDED(hr))
                                        {
                                            _ASSERTE(RealSeqPointCount == SeqPointCount);

    #ifdef _DEBUG
                                            {
                                                // This is just some debugging code to help ensure that the array
                                                // returned contains valid interface pointers.
                                                for (ULONG32 i = 0; i < RealSeqPointCount; i++)
                                                {
                                                    _ASSERTE(documents[i] != NULL);
                                                    documents[i]->AddRef();
                                                    documents[i]->Release();
                                                }
                                            }
    #endif

                                            // This is the IL offset of the current frame
                                            DWORD dwCurILOffset = data.pElements[i].dwILOffset;

                                            // search for the correct IL offset
                                            DWORD j;
                                            for (j=0; j<RealSeqPointCount; j++)
                                            {
                                                // look for the entry matching the one we're looking for
                                                if (offsets[j] >= dwCurILOffset)
                                                {
                                                    // if this offset is > what we're looking for, adjust the index
                                                    if (offsets[j] > dwCurILOffset && j > 0)
                                                    {
                                                        j--;
                                                    }

                                                    break;
                                                }
                                            }

                                            // If we didn't find a match, default to the last sequence point
                                            if  (j == RealSeqPointCount)
                                            {
                                                j--;
                                            }

                                            while (lines[j] == 0x00feefee && j > 0)
                                            {
                                                j--;
                                            }

    #ifdef DEBUGGING_SUPPORTED
                                            if (lines[j] != 0x00feefee)
                                            {
                                                sourceLine = lines [j];
                                                sourceColumn = columns [j];
                                            }
                                            else
    #endif // DEBUGGING_SUPPORTED
                                            {
                                                sourceLine = 0;
                                                sourceColumn = 0;
                                            }

                                            // Also get the filename from the document...
                                            _ASSERTE (documents [j] != NULL);

                                            hr = documents [j]->GetURL (MAX_LONGPATH, &fileNameLength, wszFileName);
                                            _ASSERTE ( SUCCEEDED(hr) || (hr == E_OUTOFMEMORY) || (hr == HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY)) );

                                            // indicate that the requisite information has been set!
                                            fFileInfoSet = TRUE;

                                            // release the documents set by GetSequencePoints
                                            for (DWORD x=0; x<RealSeqPointCount; x++)
                                            {
                                                documents [x]->Release();
                                            }
                                        } // if got sequence points

                                    }  // if all memory allocations succeeded

                                    // holders will now delete the arrays.
                                }
                            }
                            // Holder will release pISymUnmanagedMethod
                        }

                    } // GCX_PREEMP()

                    if (fFileInfoSet)
                    {
                        // Set the line and column numbers
                        CLR_I4 *pI4Line = (CLR_I4 *)((I4ARRAYREF)pStackFrameHelper->rgiLineNumber)->GetDirectPointerToNonObjectElements();
                        pI4Line[iNumValidFrames] = sourceLine;

                        CLR_I4 *pI4Column = (CLR_I4 *)((I4ARRAYREF)pStackFrameHelper->rgiColumnNumber)->GetDirectPointerToNonObjectElements();
                        pI4Column[iNumValidFrames] = sourceColumn;

                        // Set the file name
                        OBJECTREF obj = (OBJECTREF) StringObject::NewString(wszFileName);
                        pStackFrameHelper->rgFilename->SetAt(iNumValidFrames, obj);
                    }
                }

                // If the above isym reader code did NOT set the source info either because the pdb is the new portable format on
                // Windows then set the information needed to call the portable pdb reader in the StackTraceHelper.
                if (fPortablePDB)
#endif // FEATURE_ISYM_READER
                {
                    // Save MethodToken for the function
                    CLR_I4 *pMethodToken = (CLR_I4 *)((I4ARRAYREF)pStackFrameHelper->rgiMethodToken)->GetDirectPointerToNonObjectElements();
                    pMethodToken[iNumValidFrames] = pMethod->GetMemberDef();

                    PEAssembly *pPEAssembly = pModule->GetPEAssembly();

                    // Get the address and size of the loaded PE image
                    COUNT_T peSize;
                    PTR_CVOID peAddress = pPEAssembly->GetLoadedImageContents(&peSize);

                    // Save the PE address and size
                    PTR_CVOID *pLoadedPeAddress = (PTR_CVOID *)pStackFrameHelper->rgLoadedPeAddress->GetDataPtr();
                    pLoadedPeAddress[iNumValidFrames] = peAddress;

                    CLR_I4 *pLoadedPeSize = (CLR_I4 *)((I4ARRAYREF)pStackFrameHelper->rgiLoadedPeSize)->GetDirectPointerToNonObjectElements();
                    pLoadedPeSize[iNumValidFrames] = (CLR_I4)peSize;

                    // Set flag indicating PE file in memory has the on disk layout
                    if (!pPEAssembly->IsDynamic())
                    {
                        // This flag is only available for non-dynamic assemblies.
                        CLR_U1 *pIsFileLayout = (CLR_U1 *)((BOOLARRAYREF)pStackFrameHelper->rgiIsFileLayout)->GetDirectPointerToNonObjectElements();
                        pIsFileLayout[iNumValidFrames] = (CLR_U1) pPEAssembly->GetLoadedLayout()->IsFlat();
                    }

                    // If there is a in memory symbol stream
                    CGrowableStream* stream = pModule->GetInMemorySymbolStream();
                    if (stream != NULL)
                    {
                        MemoryRange range = stream->GetRawBuffer();

                        // Save the in-memory PDB address and size
                        PTR_VOID *pInMemoryPdbAddress = (PTR_VOID *)pStackFrameHelper->rgInMemoryPdbAddress->GetDataPtr();
                        pInMemoryPdbAddress[iNumValidFrames] = range.StartAddress();

                        CLR_I4 *pInMemoryPdbSize = (CLR_I4 *)((I4ARRAYREF)pStackFrameHelper->rgiInMemoryPdbSize)->GetDirectPointerToNonObjectElements();
                        pInMemoryPdbSize[iNumValidFrames] = (CLR_I4)range.Size();
                    }
                    else
                    {
                        // Set the pdb path (assembly file name)
                        const SString& assemblyPath = pPEAssembly->GetIdentityPath();
                        if (!assemblyPath.IsEmpty())
                        {
                            OBJECTREF obj = (OBJECTREF)StringObject::NewString(assemblyPath.GetUnicode());
                            pStackFrameHelper->rgAssemblyPath->SetAt(iNumValidFrames, obj);
                        }
                    }
                }
            }

            iNumValidFrames++;
        }

        pStackFrameHelper->iFrameCount = iNumValidFrames;
    }
    else
    {
        pStackFrameHelper->iFrameCount = 0;
    }

    GCPROTECT_END();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

extern MethodDesc* QCALLTYPE StackFrame_GetMethodDescFromNativeIP(LPVOID ip)
{
    QCALL_CONTRACT;

    MethodDesc* pResult = nullptr;

    BEGIN_QCALL;

    // TODO: There is a race for dynamic and collectible methods here between getting
    // the MethodDesc here and when the managed wrapper converts it into a MethodBase
    // where the method could be collected.
    EECodeInfo codeInfo((PCODE)ip);
    if (codeInfo.IsValid())
    {
        pResult = codeInfo.GetMethodDesc();
    }

    END_QCALL;

    return pResult;
}

FORCEINLINE void HolderDestroyStrongHandle(OBJECTHANDLE h) { if (h != NULL) DestroyStrongHandle(h); }
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, HolderDestroyStrongHandle, NULL> StrongHandleHolder;

// receives a custom notification object from the target and sends it to the RS via
// code:Debugger::SendCustomDebuggerNotification
// Argument: dataUNSAFE - a pointer the custom notification object being sent
FCIMPL1(void, DebugDebugger::CustomNotification, Object * dataUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    OBJECTREF pData = ObjectToOBJECTREF(dataUNSAFE);

#ifdef DEBUGGING_SUPPORTED
    // Send notification only if the debugger is attached
    if (CORDebuggerAttached() )
    {
        HELPER_METHOD_FRAME_BEGIN_PROTECT(pData);

        Thread * pThread = GetThread();
        AppDomain * pAppDomain = pThread->GetDomain();

        StrongHandleHolder objHandle = pAppDomain->CreateStrongHandle(pData);
        MethodTable * pMT = pData->GetGCSafeMethodTable();
        Module * pModule = pMT->GetModule();
        DomainAssembly * pDomainAssembly = pModule->GetDomainAssembly();
        mdTypeDef classToken = pMT->GetCl();

        pThread->SetThreadCurrNotification(objHandle);
        g_pDebugInterface->SendCustomDebuggerNotification(pThread, pDomainAssembly, classToken);
        pThread->ClearThreadCurrNotification();

        if (pThread->IsAbortRequested())
        {
            pThread->HandleThreadAbort();
        }

        HELPER_METHOD_FRAME_END();
    }

#endif // DEBUGGING_SUPPORTED

}
FCIMPLEND


void DebugStackTrace::GetStackFramesHelper(Frame *pStartFrame,
                                           void* pStopStack,
                                           GetStackFramesData *pData
                                          )
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    ASSERT (pData != NULL);

    pData->cElements = 0;

    // if the caller specified (< 20) frames are required, then allocate
    // only that many
    if ((pData->NumFramesRequested > 0) && (pData->NumFramesRequested < 20))
    {
        pData->cElementsAllocated = pData->NumFramesRequested;
    }
    else
    {
        pData->cElementsAllocated = 20;
    }

    // Allocate memory for the initial 'n' frames
    pData->pElements = new DebugStackTraceElement[pData->cElementsAllocated];

    if (pData->TargetThread == NULL ||
        pData->TargetThread->GetInternal() == GetThread())
    {
        // Null target thread specifies current thread.
        GetThread()->StackWalkFrames(GetStackFramesCallback, pData, FUNCTIONSONLY, pStartFrame);
    }
    else
    {
        Thread *pThread = pData->TargetThread->GetInternal();
        _ASSERTE (pThread != NULL);

        // Here's the timeline for the TS_SyncSuspended bit.
        // 0) TS_SyncSuspended is not set.
        // 1) The suspending thread grabs the thread store lock
        //    then puts in place trip wires for the suspendee (if it is in managed code)
        //    and releases the thread store lock.
        // 2) The suspending thread waits for the "SafeEvent".
        // 3) The suspendee continues execution until it tries to enter preemptive mode.
        //    If it trips over the wires put in place by the suspending thread,
        //    it will try to enter preemptive mode.
        // 4) The suspendee sets TS_SyncSuspended and the "SafeEvent".
        // 5) AT THIS POINT, IT IS SAFE TO WALK THE SUSPENDEE'S STACK.
        // 6) The suspendee clears the TS_SyncSuspended flag.
        //
        // In other words, it is safe to trace the thread's stack IF we're holding the
        // thread store lock AND TS_SyncSuspended is set.
        //
        // This is because:
        // - If we were not holding the thread store lock, the thread could be resumed
        //   underneath us.
        // - When TS_SyncSuspended is set, we race against it resuming execution.

        ThreadStoreLockHolder tsl;

        // We erect a barrier so that if the thread tries to disable preemptive GC,
        // it could resume execution of managed code during our stack walk.
        TSSuspendHolder shTrap;

        Thread::ThreadState state = pThread->GetSnapshotState();
        if (state & (Thread::TS_Unstarted|Thread::TS_Dead|Thread::TS_Detached))
        {
            goto LSafeToTrace;
        }

        COMPlusThrow(kThreadStateException, IDS_EE_THREAD_BAD_STATE);

    LSafeToTrace:
        pThread->StackWalkFrames(GetStackFramesCallback,
                                 pData,
                                 FUNCTIONSONLY|ALLOW_ASYNC_STACK_WALK,
                                 pStartFrame);
    }

    // Do a 2nd pass outside of any locks.
    // This will compute IL offsets.
    for (INT32 i = 0; i < pData->cElements; i++)
    {
        pData->pElements[i].InitPass2();
    }

}


void DebugStackTrace::GetStackFrames(Frame *pStartFrame,
                                     void* pStopStack,
                                     GetStackFramesData *pData
                                    )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GetStackFramesHelper(pStartFrame, pStopStack, pData);
}


StackWalkAction DebugStackTrace::GetStackFramesCallback(CrawlFrame* pCf, VOID* data)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GetStackFramesData* pData = (GetStackFramesData*)data;

    if (pData->skip > 0)
    {
        pData->skip--;
        return SWA_CONTINUE;
    }

    // <REVISIT_TODO>@todo: How do we know what kind of frame we have?</REVISIT_TODO>
    //        Can we always assume FramedMethodFrame?
    //        NOT AT ALL!!!, but we can assume it's a function
    //                       because we asked the stackwalker for it!
    MethodDesc* pFunc = pCf->GetFunction();
    if (pFunc != NULL && pFunc->HasClassOrMethodInstantiation() && pFunc->IsSharedByGenericInstantiations())
    {
        pCf->InitializeExactGenericInstantiations();
        pFunc = pCf->GetFunction();
    }

    if (pData->cElements >= pData->cElementsAllocated)
    {

        DebugStackTraceElement* pTemp = new (nothrow) DebugStackTraceElement[2*pData->cElementsAllocated];

        if (!pTemp)
        {
            return SWA_ABORT;
        }

        memcpy(pTemp, pData->pElements, pData->cElementsAllocated * sizeof(DebugStackTraceElement));

        delete [] pData->pElements;

        pData->pElements = pTemp;
        pData->cElementsAllocated *= 2;
    }

    PCODE ip;
    DWORD dwNativeOffset;

    if (pCf->IsFrameless())
    {
        // Real method with jitted code.
        dwNativeOffset = pCf->GetRelOffset();
        ip = GetControlPC(pCf->GetRegisterSet());
    }
    else
    {
        ip = NULL;
        dwNativeOffset = 0;
    }

    // Pass on to InitPass2 that the IP has already been adjusted (decremented by 1)
    INT flags = pCf->IsIPadjusted() ? STEF_IP_ADJUSTED : 0;

    pData->pElements[pData->cElements].InitPass1(
            dwNativeOffset,
            pFunc,
            ip,
            flags);

    // We'll init the IL offsets outside the TSL lock.

    ++pData->cElements;

    // Since we may be asynchronously walking another thread's stack,
    // check (frequently) for stack-buffer-overrun corruptions after
    // any long operation
    pCf->CheckGSCookies();

    // check if we already have the number of frames that the user had asked for
    if ((pData->NumFramesRequested != 0) && (pData->NumFramesRequested <= pData->cElements))
    {
        return SWA_ABORT;
    }

    return SWA_CONTINUE;
}
#endif // !DACCESS_COMPILE

void DebugStackTrace::GetStackFramesFromException(OBJECTREF * e,
                                                  GetStackFramesData *pData,
                                                  PTRARRAYREF * pDynamicMethodArray /*= NULL*/
                                                 )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION (IsProtectedByGCFrame (e));
        PRECONDITION ((pDynamicMethodArray == NULL) || IsProtectedByGCFrame (pDynamicMethodArray));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    ASSERT (pData != NULL);

    // Reasonable default, will indicate error on failure
    pData->cElements = 0;

#ifndef DACCESS_COMPILE
    // for DAC builds this has already been validated
    // Get the class for the exception
    MethodTable *pExcepClass = (*e)->GetMethodTable();

    _ASSERTE(IsException(pExcepClass));     // what is the pathway for this?
    if (!IsException(pExcepClass))
    {
        return;
    }
#endif // DACCESS_COMPILE

    // Now get the _stackTrace reference
    StackTraceArray traceData;
    EXCEPTIONREF(*e)->GetStackTrace(traceData, pDynamicMethodArray);

    GCPROTECT_BEGIN(traceData);
        // The number of frame info elements in the stack trace info
        pData->cElements = static_cast<int>(traceData.Size());

        // By default, assume that we have no frames from foreign exception stack trace.
        pData->fDoWeHaveAnyFramesFromForeignStackTrace = FALSE;

        // Now we know the size, allocate the information for the data struct
        if (pData->cElements != 0)
        {
            // Allocate the memory to contain the data
            pData->pElements = new DebugStackTraceElement[pData->cElements];

            // Fill in the data
            for (unsigned i = 0; i < (unsigned)pData->cElements; i++)
            {
                StackTraceElement const & cur = traceData[i];

                // If we come across any frame representing foreign exception stack trace,
                // then set the flag indicating so. This will be used to allocate the
                // corresponding array in StackFrameHelper.
                if ((cur.flags & STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE) != 0)
                {
                    pData->fDoWeHaveAnyFramesFromForeignStackTrace = TRUE;
                }

                // Fill out the MethodDesc*
                MethodDesc *pMD = cur.pFunc;
                _ASSERTE(pMD);

                // Calculate the native offset
                // This doesn't work for framed methods, since internal calls won't
                // push frames and the method body is therefore non-contiguous.
                // Currently such methods always return an IP of 0, so they're easy
                // to spot.
                DWORD dwNativeOffset;

                if (cur.ip)
                {
                    EECodeInfo codeInfo(cur.ip);
                    dwNativeOffset = codeInfo.GetRelOffset();
                }
                else
                {
                    dwNativeOffset = 0;
                }

                pData->pElements[i].InitPass1(
                    dwNativeOffset,
                    pMD,
                    (PCODE)cur.ip,
                    cur.flags);
#ifndef DACCESS_COMPILE
                pData->pElements[i].InitPass2();
#endif
            }
        }
        else
        {
            pData->pElements = NULL;
        }
    GCPROTECT_END();

    return;
}

// Init a stack-trace element.
// Initialization done potentially under the TSL.
void DebugStackTrace::DebugStackTraceElement::InitPass1(
    DWORD dwNativeOffset,
    MethodDesc *pFunc,
    PCODE ip,
    INT flags
)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pFunc != NULL);

    // May have a null IP for ecall frames. If IP is null, then dwNativeOffset should be 0 too.
    _ASSERTE ( (ip != NULL) || (dwNativeOffset == 0) );

    this->pFunc = pFunc;
    this->dwOffset = dwNativeOffset;
    this->ip = ip;
    this->flags = flags;
}

#ifndef DACCESS_COMPILE

// Initialization done outside the TSL.
// This may need to call locking operations that aren't safe under the TSL.
void DebugStackTrace::DebugStackTraceElement::InitPass2()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadStore::HoldingThreadStore());

    bool bRes = false;

#ifdef DEBUGGING_SUPPORTED
    // Calculate the IL offset using the debugging services
    if ((this->ip != NULL) && g_pDebugInterface)
    {
        // To get the source line number of the actual code that threw an exception, the dwOffset needs to be
        // adjusted in certain cases when calculating the IL offset.
        //
        // The dwOffset of the stack frame points to either:
        //
        // 1) The instruction that caused a hardware exception (div by zero, null ref, etc).
        // 2) The instruction after the call to an internal runtime function (FCALL like IL_Throw, IL_Rethrow,
        //    JIT_OverFlow, etc.) that caused a software exception.
        // 3) The instruction after the call to a managed function (non-leaf node).
        //
        // #2 and #3 are the cases that need to adjust dwOffset because they point after the call instruction
        // and may point to the next (incorrect) IL instruction/source line. If STEF_IP_ADJUSTED is set,
        // IP/dwOffset has already be decremented so don't decrement it again.
        //
        // When the dwOffset needs to be adjusted it is a lot simpler to decrement instead of trying to figure out
        // the beginning of the instruction. It is enough for GetILOffsetFromNative to return the IL offset of the
        // instruction throwing the exception.
        bool fAdjustOffset = (this->flags & STEF_IP_ADJUSTED) == 0 && this->dwOffset > 0;
        bRes = g_pDebugInterface->GetILOffsetFromNative(
            pFunc,
            (LPCBYTE)this->ip,
            fAdjustOffset ? this->dwOffset - 1 : this->dwOffset,
            &this->dwILOffset);
    }

#endif // !DEBUGGING_SUPPORTED

    // If there was no mapping information, then set to an invalid value
    if (!bRes)
    {
        this->dwILOffset = (DWORD)-1;
    }
}

#endif // !DACCESS_COMPILE
