// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** File: DebugDebugger.cpp
**
** Purpose: Native methods on System.Debug.Debugger
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

//
// Notes:
//    If a managed debugger is attached, this should send the managed UserBreak event.
//    Else if a native debugger is attached, this should send a native break event (kernel32!DebugBreak)
//    Else, this should invoke Watson.
//
extern "C" void QCALLTYPE DebugDebugger_Break()
{
    QCALL_CONTRACT;

#ifdef DEBUGGING_SUPPORTED
    BEGIN_QCALL;

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
    else if (minipal_is_native_debugger_present())
    {
        // No managed debugger, but a native debug is attached. Explicitly fire a native user breakpoint.
        // Don't rely on Watson support since that may have a different policy.

        // Confirm we're in preemptive before firing the debug event. This allows the debugger to suspend this
        // thread at the debug event.
        _ASSERTE(!GetThread()->PreemptiveGCDisabled());

        // This becomes an unmanaged breakpoint, such as int 3.
        DebugBreak();
    }

    END_QCALL;
#endif // DEBUGGING_SUPPORTED
}

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
        OutputDebugString(pwzModule);
        OutputDebugString(W(" : "));
    }

    if (pwzMessage != NULL)
    {
        OutputDebugString(pwzMessage);
    }

    // If we're not logging a module prefix, then don't log the newline either.
    // Thus if somebody is just logging messages, there won't be any extra newlines in there.
    // If somebody is also logging category / module information, then this call to OutputDebugString is
    // already prepending that to the message, so we append a newline for readability.
    if (pwzModule != NULL)
    {
        OutputDebugString(W("\n"));
    }


#ifdef DEBUGGING_SUPPORTED

    // Send message for logging only if the
    // debugger is attached and logging is enabled
    // for the given category
    if (CORDebuggerAttached())
    {
        if (g_pDebugInterface->IsLoggingEnabled() )
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

static StackWalkAction GetStackFramesCallback(CrawlFrame* pCf, VOID* data)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // <REVISIT_TODO>@todo: How do we know what kind of frame we have?</REVISIT_TODO>
    //        Can we always assume FramedMethodFrame?
    //        NOT AT ALL!!!, but we can assume it's a function
    //                       because we asked the stackwalker for it!
    MethodDesc* pFunc = pCf->GetFunction();

    DebugStackTrace::GetStackFramesData* pData = (DebugStackTrace::GetStackFramesData*)data;
    if (pData->cElements >= pData->cElementsAllocated)
    {
        DebugStackTrace::Element* pTemp = new (nothrow) DebugStackTrace::Element[2*pData->cElementsAllocated];
        if (pTemp == NULL)
        {
            return SWA_ABORT;
        }

        memcpy(pTemp, pData->pElements, pData->cElementsAllocated * sizeof(DebugStackTrace::Element));

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
        ip = (PCODE)NULL;
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

    // check if we already have the number of frames that the user had asked for
    if ((pData->NumFramesRequested != 0) && (pData->NumFramesRequested <= pData->cElements))
    {
        return SWA_ABORT;
    }

    return SWA_CONTINUE;
}

static void GetStackFrames(DebugStackTrace::GetStackFramesData *pData)
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
    pData->pElements = new DebugStackTrace::Element[pData->cElementsAllocated];
    GetThread()->StackWalkFrames(GetStackFramesCallback, pData, FUNCTIONSONLY | QUICKUNWIND, NULL);

    // Do a 2nd pass outside of any locks.
    // This will compute IL offsets.
    for (INT32 i = 0; i < pData->cElements; i++)
    {
        pData->pElements[i].InitPass2();
    }
}

extern "C" void QCALLTYPE StackTrace_GetStackFramesInternal(
    QCall::ObjectHandleOnStack stackFrameHelper,
    BOOL fNeedFileInfo,
    QCall::ObjectHandleOnStack exception)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    struct
    {
        STACKFRAMEHELPERREF pStackFrameHelper;
        OBJECTREF pException;
        PTRARRAYREF dynamicMethodArrayOrig;
    } gc{};
    gc.pStackFrameHelper = NULL;
    gc.pException = NULL;
    gc.dynamicMethodArrayOrig = NULL;
    GCPROTECT_BEGIN(gc);
    gc.pStackFrameHelper = (STACKFRAMEHELPERREF)stackFrameHelper.Get();
    gc.pException = exception.Get();

    DebugStackTrace::GetStackFramesData data;

    data.pDomain = GetAppDomain();

    data.NumFramesRequested = gc.pStackFrameHelper->iFrameCount;

    if (gc.pException == NULL)
    {
        GetStackFrames(&data);
    }
    else
    {
        // We also fetch the dynamic method array in a GC protected artifact to ensure
        // that the resolver objects, if any, are kept alive incase the exception object
        // is thrown again (resetting the dynamic method array reference in the object)
        // that may result in resolver objects getting collected before they can be reachable again
        // (from the code below).
        DebugStackTrace::GetStackFramesFromException(&gc.pException, &data, &gc.dynamicMethodArrayOrig);
    }

    if (data.cElements == 0)
    {
        gc.pStackFrameHelper->iFrameCount = 0;
    }
    else
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
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgMethodHandle), (OBJECTREF)methodInfoArray);

        // Allocate memory for the Offsets
        OBJECTREF offsets = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiOffset), (OBJECTREF)offsets);

        // Allocate memory for the ILOffsets
        OBJECTREF ilOffsets = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiILOffset), (OBJECTREF)ilOffsets);

        // Allocate memory for the array of assembly file names
        PTRARRAYREF assemblyPathArray = (PTRARRAYREF) AllocateObjectArray(data.cElements, g_pStringClass);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgAssemblyPath), (OBJECTREF)assemblyPathArray);

        // Allocate memory for the array of assemblies
        PTRARRAYREF assemblyArray = (PTRARRAYREF) AllocateObjectArray(data.cElements, g_pObjectClass);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgAssembly), (OBJECTREF)assemblyArray);

        // Allocate memory for the LoadedPeAddress
        BASEARRAYREF loadedPeAddressArray = (BASEARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgLoadedPeAddress), (OBJECTREF)loadedPeAddressArray);

        // Allocate memory for the LoadedPeSize
        OBJECTREF loadedPeSizeArray = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiLoadedPeSize), (OBJECTREF)loadedPeSizeArray);

        // Allocate memory for the IsFileLayout flags
        OBJECTREF isFileLayouts = AllocatePrimitiveArray(ELEMENT_TYPE_BOOLEAN, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiIsFileLayout), (OBJECTREF)isFileLayouts);

        // Allocate memory for the InMemoryPdbAddress
        BASEARRAYREF inMemoryPdbAddressArray = (BASEARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgInMemoryPdbAddress), (OBJECTREF)inMemoryPdbAddressArray);

        // Allocate memory for the InMemoryPdbSize
        OBJECTREF inMemoryPdbSizeArray = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiInMemoryPdbSize), (OBJECTREF)inMemoryPdbSizeArray);

        // Allocate memory for the MethodTokens
        OBJECTREF methodTokens = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiMethodToken), (OBJECTREF)methodTokens);

        // Allocate memory for the Filename string objects
        PTRARRAYREF filenameArray = (PTRARRAYREF) AllocateObjectArray(data.cElements, g_pStringClass);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgFilename), (OBJECTREF)filenameArray);

        // Allocate memory for the LineNumbers
        OBJECTREF lineNumbers = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiLineNumber), (OBJECTREF)lineNumbers);

        // Allocate memory for the ColumnNumbers
        OBJECTREF columnNumbers = AllocatePrimitiveArray(ELEMENT_TYPE_I4, data.cElements);
        SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiColumnNumber), (OBJECTREF)columnNumbers);

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

            SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiLastFrameFromForeignExceptionStackTrace), (OBJECTREF)IsLastFrameFromForeignStackTraceFlags);
        }
        else
        {
            SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->rgiLastFrameFromForeignExceptionStackTrace), NULL);
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
            SetObjectReference( (OBJECTREF *)&(gc.pStackFrameHelper->dynamicMethods), (OBJECTREF)dynamicDataArray);
        }

        int iNumValidFrames = 0;
        for (int i = 0; i < data.cElements; i++)
        {
            // The managed stacktrace classes always returns typical method definition, so we don't need to bother providing exact instantiation.
            // Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation(data.pElements[i].pFunc, data.pElements[i].pExactGenericArgsToken, &pExactMethod, &thExactType);
            MethodDesc* pFunc = data.pElements[i].pFunc;

            // Strip the instantiation to make sure that the reflection never gets a bad method desc back.
            if (pFunc->HasMethodInstantiation())
                pFunc = pFunc->StripMethodInstantiation();
            _ASSERTE(pFunc->IsRuntimeMethodHandle());

            // Method handle
            size_t *pElem = (size_t*)gc.pStackFrameHelper->rgMethodHandle->GetDataPtr();
            pElem[iNumValidFrames] = (size_t)pFunc;

            // Native offset
            CLR_I4 *pI4 = (CLR_I4 *)((I4ARRAYREF)gc.pStackFrameHelper->rgiOffset)->GetDirectPointerToNonObjectElements();
            pI4[iNumValidFrames] = data.pElements[i].dwOffset;

            // IL offset
            CLR_I4 *pILI4 = (CLR_I4 *)((I4ARRAYREF)gc.pStackFrameHelper->rgiILOffset)->GetDirectPointerToNonObjectElements();
            pILI4[iNumValidFrames] = data.pElements[i].dwILOffset;

            // Assembly
            OBJECTREF pAssembly = pFunc->GetAssembly()->GetExposedObject();
            gc.pStackFrameHelper->rgAssembly->SetAt(iNumValidFrames, pAssembly);

            if (data.fDoWeHaveAnyFramesFromForeignStackTrace)
            {
                // Set the BOOL indicating if the frame represents the last frame from a foreign exception stack trace.
                CLR_U1 *pIsLastFrameFromForeignExceptionStackTraceU1 = (CLR_U1 *)((BOOLARRAYREF)gc.pStackFrameHelper->rgiLastFrameFromForeignExceptionStackTrace)
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

                    ((PTRARRAYREF)gc.pStackFrameHelper->dynamicMethods)->SetAt(iCurDynamic++, pResolver);
                }
                else if (pMethod->GetMethodTable()->Collectible())
                {
                    OBJECTREF pLoaderAllocator = pMethod->GetMethodTable()->GetLoaderAllocator()->GetExposedObject();
                    _ASSERTE(pLoaderAllocator != NULL);
                    ((PTRARRAYREF)gc.pStackFrameHelper->dynamicMethods)->SetAt(iCurDynamic++, pLoaderAllocator);
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
                        CLR_I4 *pI4Line = (CLR_I4 *)((I4ARRAYREF)gc.pStackFrameHelper->rgiLineNumber)->GetDirectPointerToNonObjectElements();
                        pI4Line[iNumValidFrames] = sourceLine;

                        CLR_I4 *pI4Column = (CLR_I4 *)((I4ARRAYREF)gc.pStackFrameHelper->rgiColumnNumber)->GetDirectPointerToNonObjectElements();
                        pI4Column[iNumValidFrames] = sourceColumn;

                        // Set the file name
                        OBJECTREF obj = (OBJECTREF) StringObject::NewString(wszFileName);
                        gc.pStackFrameHelper->rgFilename->SetAt(iNumValidFrames, obj);
                    }
                }

                // If the above isym reader code did NOT set the source info either because the pdb is the new portable format on
                // Windows then set the information needed to call the portable pdb reader in the StackTraceHelper.
                if (fPortablePDB)
#endif // FEATURE_ISYM_READER
                {
                    // Save MethodToken for the function
                    CLR_I4 *pMethodToken = (CLR_I4 *)((I4ARRAYREF)gc.pStackFrameHelper->rgiMethodToken)->GetDirectPointerToNonObjectElements();
                    pMethodToken[iNumValidFrames] = pMethod->GetMemberDef();

                    PEAssembly *pPEAssembly = pModule->GetPEAssembly();

                    // Get the address and size of the loaded PE image
                    COUNT_T peSize;
                    PTR_CVOID peAddress = pPEAssembly->GetLoadedImageContents(&peSize);

                    // Save the PE address and size
                    PTR_CVOID *pLoadedPeAddress = (PTR_CVOID *)gc.pStackFrameHelper->rgLoadedPeAddress->GetDataPtr();
                    pLoadedPeAddress[iNumValidFrames] = peAddress;

                    CLR_I4 *pLoadedPeSize = (CLR_I4 *)((I4ARRAYREF)gc.pStackFrameHelper->rgiLoadedPeSize)->GetDirectPointerToNonObjectElements();
                    pLoadedPeSize[iNumValidFrames] = (CLR_I4)peSize;

                    // Set flag indicating PE file in memory has the on disk layout
                    if (!pPEAssembly->IsReflectionEmit())
                    {
                        // This flag is only available for non-dynamic assemblies.
                        CLR_U1 *pIsFileLayout = (CLR_U1 *)((BOOLARRAYREF)gc.pStackFrameHelper->rgiIsFileLayout)->GetDirectPointerToNonObjectElements();
                        pIsFileLayout[iNumValidFrames] = (CLR_U1) pPEAssembly->GetLoadedLayout()->IsFlat();
                    }

                    // If there is a in memory symbol stream
                    CGrowableStream* stream = pModule->GetInMemorySymbolStream();
                    if (stream != NULL)
                    {
                        MemoryRange range = stream->GetRawBuffer();

                        // Save the in-memory PDB address and size
                        PTR_VOID *pInMemoryPdbAddress = (PTR_VOID *)gc.pStackFrameHelper->rgInMemoryPdbAddress->GetDataPtr();
                        pInMemoryPdbAddress[iNumValidFrames] = range.StartAddress();

                        CLR_I4 *pInMemoryPdbSize = (CLR_I4 *)((I4ARRAYREF)gc.pStackFrameHelper->rgiInMemoryPdbSize)->GetDirectPointerToNonObjectElements();
                        pInMemoryPdbSize[iNumValidFrames] = (CLR_I4)range.Size();
                    }
                    else
                    {
                        // Set the pdb path (assembly file name)
                        const SString& assemblyPath = pPEAssembly->GetIdentityPath();
                        if (!assemblyPath.IsEmpty())
                        {
                            OBJECTREF obj = (OBJECTREF)StringObject::NewString(assemblyPath.GetUnicode());
                            gc.pStackFrameHelper->rgAssemblyPath->SetAt(iNumValidFrames, obj);
                        }
                    }
                }
            }

            iNumValidFrames++;
        }

        gc.pStackFrameHelper->iFrameCount = iNumValidFrames;
    }

    GCPROTECT_END();

    END_QCALL;
}

extern "C" MethodDesc* QCALLTYPE StackFrame_GetMethodDescFromNativeIP(LPVOID ip)
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
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, HolderDestroyStrongHandle, 0> StrongHandleHolder;

// receives a custom notification object from the target and sends it to the RS via
// code:Debugger::SendCustomDebuggerNotification
// Argument: dataUNSAFE - a pointer the custom notification object being sent
extern "C" void QCALLTYPE DebugDebugger_CustomNotification(QCall::ObjectHandleOnStack data)
{
    QCALL_CONTRACT;

#ifdef DEBUGGING_SUPPORTED
    // Send notification only if the debugger is attached
    if (!CORDebuggerAttached())
        return;

    BEGIN_QCALL;

    GCX_COOP();

    Thread * pThread = GetThread();
    AppDomain * pAppDomain = AppDomain::GetCurrentDomain();

    StrongHandleHolder objHandle = pAppDomain->CreateStrongHandle(data.Get());
    MethodTable* pMT = data.Get()->GetGCSafeMethodTable();
    Module* pModule = pMT->GetModule();
    DomainAssembly* pDomainAssembly = pModule->GetDomainAssembly();
    mdTypeDef classToken = pMT->GetCl();

    pThread->SetThreadCurrNotification(objHandle);
    g_pDebugInterface->SendCustomDebuggerNotification(pThread, pDomainAssembly, classToken);
    pThread->ClearThreadCurrNotification();

    if (pThread->IsAbortRequested())
    {
        pThread->HandleThreadAbort();
    }

    END_QCALL;
#endif // DEBUGGING_SUPPORTED
}

extern "C" BOOL QCALLTYPE DebugDebugger_IsLoggingHelper()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerAttached())
    {
        return g_pDebugInterface->IsLoggingEnabled();
    }
#endif // DEBUGGING_SUPPORTED

    return FALSE;
}

extern "C" BOOL QCALLTYPE DebugDebugger_IsManagedDebuggerAttached()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

#ifdef DEBUGGING_SUPPORTED
    return CORDebuggerAttached();
#else
    return FALSE;
#endif
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

    GCPROTECT_BEGIN(traceData);
        EXCEPTIONREF(*e)->GetStackTrace(traceData, pDynamicMethodArray);
        // The number of frame info elements in the stack trace info
        pData->cElements = static_cast<int>(traceData.Size());

        // By default, assume that we have no frames from foreign exception stack trace.
        pData->fDoWeHaveAnyFramesFromForeignStackTrace = FALSE;

        // Now we know the size, allocate the information for the data struct
        if (pData->cElements != 0)
        {
            // Allocate the memory to contain the data
            pData->pElements = new Element[pData->cElements];

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

                UINT_PTR ip = cur.ip;
#if defined(DACCESS_COMPILE) && defined(TARGET_AMD64)
                // Compensate for a bug in the old EH that for a frame that faulted
                // has the ip pointing to an address before the faulting instruction
                if ((i == 0) && ((cur.flags & STEF_IP_ADJUSTED) == 0))
                {
                    ip -= 1;
                }
#endif // DACCESS_COMPILE && TARGET_AMD64
                if (ip)
                {
                    EECodeInfo codeInfo(ip);
                    dwNativeOffset = codeInfo.GetRelOffset();
                }
                else
                {
                    dwNativeOffset = 0;
                }

                pData->pElements[i].InitPass1(
                    dwNativeOffset,
                    pMD,
                    (PCODE)ip,
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
void DebugStackTrace::Element::InitPass1(
    DWORD dwNativeOffset,
    MethodDesc *pFunc,
    PCODE ip,
    INT flags
)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pFunc != NULL);

    // May have a null IP for ecall frames. If IP is null, then dwNativeOffset should be 0 too.
    _ASSERTE ( (ip != (PCODE)NULL) || (dwNativeOffset == 0) );

    this->pFunc = pFunc;
    this->dwOffset = dwNativeOffset;
    this->ip = ip;
    this->flags = flags;
}

#ifndef DACCESS_COMPILE

struct WalkILOffsetsData
{
    WalkILOffsetsData(DWORD dwSearchNativeOffset, bool skipPrologsParam = false)
        : dwSearchNativeOffset(dwSearchNativeOffset),
        skipPrologs(skipPrologsParam)
    {
    }
    DWORD prevILOffsetFound = 0;
    DWORD dwILOffsetFound = 0;
    DWORD dwCurrentNativeOffset = 0;
    int greatestILOffsetFound = 0;
    const DWORD dwSearchNativeOffset;

    DWORD dwFinalILOffset = 0;
    const bool skipPrologs;
    bool skipPrologCase = false;
    bool firstCall = true;
    bool epilogCase = false;
};

size_t WalkILOffsetsCallback(ICorDebugInfo::OffsetMapping *pOffsetMapping, void *pContext)
{
    WalkILOffsetsData *pWalkData = (WalkILOffsetsData *)pContext;
    // Callbacks into this api are sorted by native offset, but not sorted by IL offset.
    // In addition, there are several special IL offsets that are not actual IL offsets.

    // Also, ignore all the callsite mappings since they are not relevant for the standard Native-IL mapping

    if ((pOffsetMapping->source & (DWORD)ICorDebugInfo::CALL_INSTRUCTION) == (DWORD)ICorDebugInfo::CALL_INSTRUCTION)
    {
        // This is a call instruction mapping, so we don't care about it.
        return 0;
    }

    if ((int32_t)pOffsetMapping->ilOffset > pWalkData->greatestILOffsetFound)
    {
        // Calculate the greatest IL offset found so far. We use this as the IL offset for epilogs
        pWalkData->greatestILOffsetFound = (int)pOffsetMapping->ilOffset;
    }

    if (pWalkData->epilogCase)
    {
        if (pOffsetMapping->nativeOffset == 0xFFFFFFFF)
        {
            pWalkData->dwFinalILOffset = pWalkData->greatestILOffsetFound;
            return 1;
        }
        else
        {
            return 0;
        }
    }

    // The general rule is that we need to find the lowest IL offset that applies to the given native offset
    // An IL offset in the mapping of ICorDebugInfo::NO_MAPPING indicates that the IL offset we report should be 0
    // An IL offset in the mapping of ICorDebugInfo::PROLOG indicates that we should report the "next" IL offset.
    // An IL offset in the mapping of ICorDebugInfo::EPILOG indicates that we should report the "previous" IL offset.

    if (pWalkData->firstCall)
    {
        pWalkData->firstCall = false;
        // This is the first time we are called, so set the current native offset to the one we are looking for
        pWalkData->dwCurrentNativeOffset = pOffsetMapping->nativeOffset;
        pWalkData->dwILOffsetFound = pOffsetMapping->ilOffset;
    }
    else
    {
        // If the current native offset is less than the one we are looking for, then we need to set the IL offset
        // to the one we are looking for.
        if (pOffsetMapping->nativeOffset > pWalkData->dwCurrentNativeOffset)
        {
            if (pWalkData->skipPrologCase)
            {
                if (pWalkData->prevILOffsetFound != ICorDebugInfo::NO_MAPPING &&
                    pWalkData->prevILOffsetFound != ICorDebugInfo::PROLOG && 
                    pWalkData->prevILOffsetFound != ICorDebugInfo::EPILOG)
                {
                    // We found a valid IL offset after the prolog, so set the final IL offset to the one we found
                    pWalkData->dwFinalILOffset = pWalkData->dwILOffsetFound;
                }
                else
                {
                    pWalkData->dwFinalILOffset = 0;
                }
                return 1;
            }
            if (pOffsetMapping->nativeOffset > pWalkData->dwSearchNativeOffset)
            {
                // We were searching for the IL offset, and we found it (unless we were in a PROLOG)
                if (pWalkData->dwILOffsetFound == (DWORD)ICorDebugInfo::EPILOG)
                {
                    // We found we were in an EPILOG. Report the last il offset reported as part of the method.
                    pWalkData->epilogCase = true;
                    if (pOffsetMapping->nativeOffset == 0xFFFFFFFF)
                    {
                        pWalkData->dwFinalILOffset = pWalkData->greatestILOffsetFound;
                        return 1;
                    }
                }
                else if (pWalkData->dwILOffsetFound == (DWORD)ICorDebugInfo::NO_MAPPING)
                {
                    pWalkData->dwFinalILOffset = 0;
                    return 1;
                }
                else if (!(pWalkData->dwILOffsetFound == (DWORD)ICorDebugInfo::PROLOG))
                {
                    pWalkData->dwFinalILOffset = pWalkData->dwILOffsetFound;
                    return 1;
                }
                else
                {
                    // PROLOG case
                    if (pWalkData->skipPrologs)
                    {
                        _ASSERTE(pWalkData->dwILOffsetFound == ICorDebugInfo::PROLOG);
                        pWalkData->skipPrologCase = true;
                    }
                    else
                    {
                        pWalkData->dwFinalILOffset = 0;
                        return 1;
                    }
                }
            }
            pWalkData->dwCurrentNativeOffset = pOffsetMapping->nativeOffset;
            pWalkData->prevILOffsetFound = pWalkData->dwILOffsetFound;
            pWalkData->dwCurrentNativeOffset = pOffsetMapping->nativeOffset;
            pWalkData->dwILOffsetFound = pOffsetMapping->ilOffset;
        }
        else if (((int32_t)pOffsetMapping->ilOffset < (int32_t)pWalkData->dwILOffsetFound) && (pOffsetMapping->ilOffset != (DWORD)ICorDebugInfo::NO_MAPPING))
        {
            // We found a new IL offset that is less than the one we are looking for
            pWalkData->dwILOffsetFound = pOffsetMapping->ilOffset;
        }
    }

    return 0;
}

#ifndef DACCESS_COMPILE
// This is an implementation of a cache of the Native->IL offset mappings used by managed stack traces. It exists for the following reasons:
// 1. When a large server experiences a large number of exceptions due to some other system failing, it can cause a tremendous number of stack traces to be generated, if customers are attempting to log.
// 2. The native->IL offset mapping is somewhat expensive to compute, and it is not necessary to compute it repeatedly for the same IP.
// 3. Often when these mappings are needed, the system is under stress, and throwing on MANY different threads with similar callstacks, so the cost of having locking around the cache may be significant.
//
// The cache is implemented as a simple hash table, where the key is the IP + fAdjustOffset
// flag, and the value is the IL offset. We use a version number to indicate when the cache
// is being updated, and to indicate that a found value is valid, and we use a simple linear
// probing algorithm to find the entry in the cache. The fallback from the cache is now
// designed to avoid locks when possible, and while it is substantially slower than the cache,
// it is still fast enough that we don't need to go to great lengths to avoid it.

static LONG s_stackWalkILToNativeCacheVersion = 0;
const DWORD s_stackWalkCacheSize = 1024; // This is the total size of the cache (We use a pointer+4 bytes for each entry, so on 64bit platforms 12KB of memory)
const DWORD s_stackWalkCacheWalk = 8; // Walk up to this many entries in the cache before giving up
const DWORD s_stackWalkCacheAdjustOffsetFlag = 0x80000000; // 2^31, put into the IL offset portion of the cache entry to check if the native offset was adjusted by STACKWALK_CONTROLPC_ADJUST_OFFSET
static void* s_stackWalkILToNativeCacheIP[s_stackWalkCacheSize];
static uint32_t s_stackWalkILToNativeCacheIL[s_stackWalkCacheSize];

bool CheckILToNativeCacheCore(void* ip, bool fAdjustOffset, uint32_t* pILOffset)
{
    // Check the cache for the IP
    int hashCode = MixPointerIntoHash(ip);
    int index = hashCode % s_stackWalkCacheSize;

    int count = 0;
    do
    {
        if (VolatileLoadWithoutBarrier(&s_stackWalkILToNativeCacheIP[index]) == ip)
        {
            // Cache hit
            uint32_t dwILOffset = VolatileLoad(&s_stackWalkILToNativeCacheIL[index]);
            if (fAdjustOffset != ((dwILOffset & s_stackWalkCacheAdjustOffsetFlag) == s_stackWalkCacheAdjustOffsetFlag))
            {
                continue; // The cache entry did not match on the adjust offset flag, so move to the next entry.
            }

            dwILOffset &= ~s_stackWalkCacheAdjustOffsetFlag; // Clear the adjust offset flag
            *pILOffset = dwILOffset;
            return true;
        }
    } while (index = (index + 1) % s_stackWalkCacheSize, count++ < s_stackWalkCacheWalk);

    return false; // Not found in cache
}

bool CheckILToNativeCache(void* ip, bool fAdjustOffset, uint32_t* pILOffset)
{
    LIMITED_METHOD_CONTRACT;

    LONG versionStart = VolatileLoad(&s_stackWalkILToNativeCacheVersion);

    if ((versionStart & 1) == 1)
    {
        // Cache is being updated, so we cannot use it
        return false;
    }

    if (CheckILToNativeCacheCore(ip, fAdjustOffset, pILOffset))
    {
        LONG versionEnd = VolatileLoadWithoutBarrier(&s_stackWalkILToNativeCacheVersion);
        if (versionEnd == versionStart)
        {
            // Cache was not updated while we were checking it, so we can use it
            return true;
        }
    }

    return false;
}

void InsertIntoILToNativeCache(void* ip, bool fAdjustOffset, uint32_t dwILOffset)
{
    LIMITED_METHOD_CONTRACT;

    // Insert the IP and IL offset into the cache
    
    LONG versionStart = VolatileLoadWithoutBarrier(&s_stackWalkILToNativeCacheVersion);
    if ((versionStart & 1) == 1)
    {
        // Cache is being updated by someone else, so we can't modify it
        return;
    }

    if (versionStart != InterlockedCompareExchange(&s_stackWalkILToNativeCacheVersion, versionStart, versionStart | 1))
    {
        // Someone else updated the cache version while we were attempting to take the lock, so abort updating the cache
        return;
    }
    // Now we have the lock, so we can safely update the cache

    // First check to see if the cache already has an entry
    uint32_t dwILOffsetFound;
    if (CheckILToNativeCacheCore(ip, fAdjustOffset, &dwILOffsetFound))
    {
        // The entry already exists, so we don't need to insert it again
        _ASSERTE(dwILOffsetFound == dwILOffset);

        // Store back the original version to indicate that the cache has not been updated, and is ready for use.
        VolatileStore(&s_stackWalkILToNativeCacheVersion, versionStart);
    }
    else
    {
        // Insert the IP and IL offset into the cache

        int hashCode = MixPointerIntoHash(ip);

        // Insert the entry into a semi-random location in the set of cache entries. The idea is to attempt to be somewhat collision resistant
        int index = (hashCode + (MixOneValueIntoHash(versionStart) % s_stackWalkCacheWalk)) % s_stackWalkCacheSize;

        s_stackWalkILToNativeCacheIP[index] = ip;
        s_stackWalkILToNativeCacheIL[index] = dwILOffset | (fAdjustOffset ? s_stackWalkCacheAdjustOffsetFlag : 0);

        // Increment the version to indicate that the cache has been updated, and is ready for use.
        VolatileStore(&s_stackWalkILToNativeCacheVersion, versionStart + 2);
    }
}

#ifdef DEBUG
size_t WalkILOffsetsCallback_Printer(ICorDebugInfo::OffsetMapping *pOffsetMapping, void *pContext)
{
    printf("IL Offset: %d, Native Offset: %d, %s%s%s%s%s\n",
          pOffsetMapping->ilOffset,
          pOffsetMapping->nativeOffset,
          pOffsetMapping->source & ICorDebugInfo::SEQUENCE_POINT ? "SEQUENCE_POINT " : "",
          pOffsetMapping->source & ICorDebugInfo::STACK_EMPTY ? "STACK_EMPTY " : "",
          pOffsetMapping->source & ICorDebugInfo::CALL_SITE ? "CALL_SITE " : "",
          pOffsetMapping->source & ICorDebugInfo::NATIVE_END_OFFSET_UNKNOWN ? "NATIVE_END_OFFSET_UNKNOWN " : "",
          pOffsetMapping->source & ICorDebugInfo::CALL_INSTRUCTION ? "CALL_INSTRUCTION " : "");
    return 0;
}

void ValidateILOffset(MethodDesc *pFunc, uint8_t* ip)
{
    EECodeInfo codeInfo((PCODE)ip);
    if (!codeInfo.IsValid())
    {
        return;
    }
    DWORD dwNativeOffset = codeInfo.GetRelOffset();

    DWORD dwILOffsetDebugInterface = 0;
    DWORD dwILOffsetWalk = 0;
    
    bool bResGetILOffsetFromNative = g_pDebugInterface->GetILOffsetFromNative(
        pFunc,
        (LPCBYTE)ip,
        dwNativeOffset,
        &dwILOffsetDebugInterface);
        
    WalkILOffsetsData data(dwNativeOffset);
    TADDR startAddress = codeInfo.GetStartAddress();
    DebugInfoRequest request;
    request.InitFromStartingAddr(codeInfo.GetMethodDesc(), startAddress);
    bool bWalkILOffsets;
    
    if (pFunc->IsDynamicMethod())
    {
        bWalkILOffsets = false;
    }
    else
    {
        bWalkILOffsets = codeInfo.GetJitManager()->WalkILOffsets(request, &data, WalkILOffsetsCallback);
        if (bWalkILOffsets)
            dwILOffsetWalk = data.dwFinalILOffset;
        else
        {
            dwILOffsetWalk = 0;
            bWalkILOffsets = true;
        }
    }

    if (bWalkILOffsets && bResGetILOffsetFromNative)
    {
        if (dwILOffsetWalk != dwILOffsetDebugInterface)
        {
            printf("Mismatch in IL offsets for %p at IP %p Native Offset %d:\n", pFunc, ip, dwNativeOffset);
            printf("  Debug Interface IL Offset: %d\n", dwILOffsetDebugInterface);
            printf("  Walk IL Offsets IL Offset: %d\n", dwILOffsetWalk);
            codeInfo.GetJitManager()->WalkILOffsets(request, NULL, WalkILOffsetsCallback_Printer);
        }
        _ASSERTE(dwILOffsetWalk == dwILOffsetDebugInterface);
    }

    if (bWalkILOffsets != bResGetILOffsetFromNative)
    {
        printf("Mismatch in IL offsets validity for %p at IP %p Native Offset %d:\n", pFunc, ip, dwNativeOffset);
        printf("  Debug Interface IL Offset: %d Valid = %s\n", dwILOffsetDebugInterface, bResGetILOffsetFromNative ? "true" : "false");
        printf("  Walk IL Offsets IL Offset: %d Valid = %s\n", dwILOffsetWalk, bWalkILOffsets ? "true" : "false");
        codeInfo.GetJitManager()->WalkILOffsets(request, NULL, WalkILOffsetsCallback_Printer);
        _ASSERTE(bWalkILOffsets == bResGetILOffsetFromNative);
    }
}

void ValidateILOffsets(MethodDesc *pFunc, uint8_t* ipColdStart, size_t coldLen, uint8_t* ipHotStart, size_t hotLen)
{
    LIMITED_METHOD_CONTRACT;

    uint8_t *ip;
    if (ipColdStart != NULL)
    {
        for (ip = ipColdStart; ip <= (ipColdStart + coldLen);ip++)
        {
            ValidateILOffset(pFunc, ip);
        }
    }

    if (ipHotStart != NULL)
    {
        for (ip = ipHotStart; ip <= (ipHotStart + hotLen);ip++)
        {
            ValidateILOffset(pFunc, ip);
        }
    }
}
#endif // DEBUG

#endif // !DACCESS_COMPILE

// Initialization done outside the TSL.
// This may need to call locking operations that aren't safe under the TSL.
void DebugStackTrace::Element::InitPass2()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadStore::HoldingThreadStore());

    bool bRes = false;

    bool fAdjustOffset = (this->flags & STEF_IP_ADJUSTED) == 0 && this->dwOffset > 0;
#if defined(DEBUGGING_SUPPORTED) && defined(DEBUG)

    // Calculate the IL offset using the debugging services
    if ((this->ip != (PCODE)NULL) && g_pDebugInterface)
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
        bRes = g_pDebugInterface->GetILOffsetFromNative(
            pFunc,
            (LPCBYTE)this->ip,
            fAdjustOffset ? this->dwOffset - STACKWALK_CONTROLPC_ADJUST_OFFSET : this->dwOffset,
            &this->dwILOffset);
    }
#endif // DEBUG

    if (this->ip != (PCODE)NULL)
    {
        // Check the cache!
        uint32_t dwILOffsetFromCache;
        if (CheckILToNativeCache((void*)this->ip, fAdjustOffset, &dwILOffsetFromCache))
        {
#if defined(DEBUGGING_SUPPORTED) && defined(DEBUG)
            _ASSERTE(this->dwILOffset == dwILOffsetFromCache); // This asserts that the IL offset computed here is the same as would be computed by g_pDebugInterface->GetILOffsetFromNative
#endif // DEBUGGING_SUPPORTED && DEBUG
            this->dwILOffset = dwILOffsetFromCache;
            bRes = true;
        }
        else
        {
            // Get IL Offset from the JitManager directly
            EECodeInfo codeInfo(this->ip);

            // We are allowed to get a result for non-dynamic methods only, other methods will either have good data, or be returned as IL offset 0.
            if (codeInfo.IsValid() && !codeInfo.GetMethodDesc()->IsDynamicMethod())
            {
                TADDR startAddress = codeInfo.GetStartAddress();
                WalkILOffsetsData data(fAdjustOffset ? this->dwOffset - STACKWALK_CONTROLPC_ADJUST_OFFSET : this->dwOffset);
                DebugInfoRequest request;
                request.InitFromStartingAddr(codeInfo.GetMethodDesc(), startAddress);
                if (!codeInfo.GetJitManager()->WalkILOffsets(request, &data, WalkILOffsetsCallback))
                {
                    data.dwFinalILOffset = 0; // If we didn't find any IL offsets, set the final IL offset to 0
                }

#if defined(DEBUGGING_SUPPORTED) && defined(DEBUG)
                _ASSERTE(this->dwILOffset == data.dwFinalILOffset); // This asserts that the IL offset computed here is the same as would be computed by g_pDebugInterface->GetILOffsetFromNative
#endif // DEBUGGING_SUPPORTED && DEBUG
                this->dwILOffset = data.dwFinalILOffset;
                bRes = true;

                MethodDesc* pMD = codeInfo.GetMethodDesc();
                if (!pMD->IsLCGMethod() && !pMD->GetLoaderAllocator()->IsCollectible())
                {
                    // Only insert into the cache if the value found will not change throughout the lifetime of the process.
                    InsertIntoILToNativeCache(
                        (void*)this->ip,
                        fAdjustOffset,
                        this->dwILOffset);
                }
            }
            else
            {
                bRes = false;
            }
        }
    }


    // If there was no mapping information, then set to an invalid value
    if (!bRes)
    {
        this->dwILOffset = (DWORD)-1;
    }
}

#endif // !DACCESS_COMPILE
