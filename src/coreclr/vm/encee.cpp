// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: EnC.CPP
//

//
// Handles EditAndContinue support in the EE
// ===========================================================================


#include "common.h"
#include "dbginterface.h"
#include "dllimport.h"
#include "eeconfig.h"
#include "excep.h"
#include "stackwalk.h"

#ifdef DACCESS_COMPILE
#include "../debug/daccess/gcinterface.dac.h"
#endif // DACCESS_COMPILE

#ifdef EnC_SUPPORTED

// can't get this on the helper thread at runtime in ResolveField, so make it static and get when add a field.
#ifdef _DEBUG
static int g_BreakOnEnCResolveField = -1;
#endif

#ifndef DACCESS_COMPILE


// Module initialization occurs in two phases: the constructor phase and the Initialize phase.
//
// The constructor phase initializes just enough so that Destruct() can be safely called.
// It cannot throw or fail.
//
EditAndContinueModule::EditAndContinueModule(Assembly *pAssembly, mdToken moduleRef, PEAssembly *pPEAssembly)
  : Module(pAssembly, moduleRef, pPEAssembly)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    LOG((LF_ENC,LL_INFO100,"EACM::ctor %p\n", this));

    m_applyChangesCount = CorDB_DEFAULT_ENC_FUNCTION_VERSION;
}

// Module initialization occurs in two phases: the constructor phase and the Initialize phase.
//
// The Initialize() phase completes the initialization after the constructor has run.
// It can throw exceptions but whether it throws or succeeds, it must leave the Module
// in a state where Destruct() can be safely called.
//
/*virtual*/
void EditAndContinueModule::Initialize(AllocMemTracker *pamTracker, LPCWSTR szName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LOG((LF_ENC,LL_INFO100,"EACM::Initialize %p\n", this));
    Module::Initialize(pamTracker, szName);
}

// Called when the module is being destroyed (eg. AD unload time)
void EditAndContinueModule::Destruct()
{
    LIMITED_METHOD_CONTRACT;
    LOG((LF_ENC,LL_EVERYTHING,"EACM::Destruct %p\n", this));

    // Call the superclass's Destruct method...
    Module::Destruct();
}

//---------------------------------------------------------------------------------------
//
// ApplyEditAndContinue - updates this module for an EnC
//
// Arguments:
//    cbDeltaMD  - number of bytes pointed to by pDeltaMD
//    pDeltaMD   - pointer to buffer holding the delta metadata
//    cbDeltaIL  - number of bytes pointed to by pDeltaIL
//    pDeltaIL   - pointer to buffer holding the delta IL
//
// Return Value:
//    S_OK on success.
//    if the edit fails for any reason, at any point in this function,
//    we are toasted, so return out and IDE will end debug session.
//

HRESULT EditAndContinueModule::ApplyEditAndContinue(
    DWORD cbDeltaMD,
    BYTE *pDeltaMD,
    DWORD cbDeltaIL,
    BYTE *pDeltaIL)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Update the module's EnC version number
    ++m_applyChangesCount;

    LOG((LF_ENC, LL_INFO100, "EACM::AEAC:\n"));

#ifdef _DEBUG
    // Debugging hook to optionally break when this method is called
    static BOOL shouldBreak = -1;
    if (shouldBreak == -1)
        shouldBreak = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EncApplyChanges);
    if (shouldBreak > 0) {
        _ASSERTE(!"EncApplyChanges");
    }

    // Debugging hook to dump out all edits to dmeta and dil files
    static BOOL dumpChanges = -1;

    if (dumpChanges == -1)

        dumpChanges = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EncDumpApplyChanges);

    if (dumpChanges> 0) {
        SString fn;
        int ec;
        fn.Printf("ApplyChanges.%d.dmeta", m_applyChangesCount);
        FILE *fp;
        ec = _wfopen_s(&fp, fn.GetUnicode(), W("wb"));
        _ASSERTE(SUCCEEDED(ec));
        fwrite(pDeltaMD, 1, cbDeltaMD, fp);
        fclose(fp);
        fn.Printf("ApplyChanges.%d.dil", m_applyChangesCount);
        ec = _wfopen_s(&fp, fn.GetUnicode(), W("wb"));
        _ASSERTE(SUCCEEDED(ec));
        fwrite(pDeltaIL, 1, cbDeltaIL, fp);
        fclose(fp);
    }
#endif

    HRESULT hr = S_OK;
    HENUMInternal enumENC;

    BYTE *pLocalILMemory = NULL;
    IMDInternalImport *pMDImport = NULL;
    IMDInternalImport *pNewMDImport = NULL;

    CONTRACT_VIOLATION(GCViolation);    // SafeComHolder goes to preemptive mode, which will trigger a GC
    SafeComHolder<IMDInternalImportENC> pIMDInternalImportENC;
    SafeComHolder<IMetaDataEmit> pEmitter;

    // Apply the changes. Note that ApplyEditAndContinue() requires read/write metadata. If the metadata is
    // not already RW, then ApplyEditAndContinue() will perform the conversion, invalidate the current
    // metadata importer, and return us a new one.  We can't let that happen. Other parts of the system are
    // already using the current metadata importer, some possibly in preemptive GC mode at this very moment.
    // Instead, we ensure that the metadata is RW by calling ConvertMDInternalToReadWrite(), which will make
    // a new importer if necessary and ensure that new accesses to the metadata use that while still managing
    // the lifetime of the old importer. Therefore, we can be sure that ApplyEditAndContinue() won't need to
    // make a new importer.

    // Ensure the metadata is RW.
    EX_TRY
    {
        // ConvertMDInternalToReadWrite should only ever be called on EnC capable files.
        _ASSERTE(IsEditAndContinueCapable()); // this also checks that the file is EnC capable
        GetPEAssembly()->ConvertMDInternalToReadWrite();
    }
    EX_CATCH_HRESULT(hr);

    IfFailGo(hr);

    // Grab the current importer.
    pMDImport = GetMDImport();

    // Apply the EnC delta to this module's metadata.
    IfFailGo(pMDImport->ApplyEditAndContinue(pDeltaMD, cbDeltaMD, &pNewMDImport));

    // The importer should not have changed!  We assert that, and back-stop in a retail build just to be sure.
    if (pNewMDImport != pMDImport)
    {
        _ASSERTE( !"ApplyEditAndContinue should not have needed to create a new metadata importer!" );
        IfFailGo(CORDBG_E_ENC_INTERNAL_ERROR);
    }

    // get the delta interface
    IfFailGo(pMDImport->QueryInterface(IID_IMDInternalImportENC, (void **)&pIMDInternalImportENC));

    // get an emitter interface
    IfFailGo(GetMetaDataPublicInterfaceFromInternal(pMDImport, IID_IMetaDataEmit, (void **)&pEmitter));

    // Copy the deltaIL into our RVAable IL memory
    pLocalILMemory = new BYTE[cbDeltaIL];
    memcpy(pLocalILMemory, pDeltaIL, cbDeltaIL);

    // Enumerate all of the EnC delta tokens
    HENUMInternal::ZeroEnum(&enumENC);
    IfFailGo(pIMDInternalImportENC->EnumDeltaTokensInit(&enumENC));

    mdToken token;
    while (pIMDInternalImportENC->EnumNext(&enumENC, &token))
    {
        STRESS_LOG3(LF_ENC, LL_INFO100, "EACM::AEAC: updated token %08x; type %08x; rid %08x\n", token, TypeFromToken(token), RidFromToken(token));

        switch (TypeFromToken(token))
        {
            case mdtMethodDef:

                // MethodDef token - update/add a method
                LOG((LF_ENC, LL_INFO10000, "EACM::AEAC: Found method %08x\n", token));

                ULONG dwMethodRVA;
                DWORD dwMethodFlags;
                IfFailGo(pMDImport->GetMethodImplProps(token, &dwMethodRVA, &dwMethodFlags));

                if (dwMethodRVA >= cbDeltaIL)
                {
                    LOG((LF_ENC, LL_INFO10000, "EACM::AEAC: Failure RVA of %d with cbDeltaIl %d\n", dwMethodRVA, cbDeltaIL));
                    IfFailGo(E_INVALIDARG);
                }

                SetDynamicIL(token, (TADDR)(pLocalILMemory + dwMethodRVA), FALSE);

                // use module to resolve to method
                MethodDesc *pMethod;
                pMethod = LookupMethodDef(token);
                if (pMethod)
                {
                    // Method exists already - update it
                    IfFailGo(UpdateMethod(pMethod));
                }
                else
                {
                    // This is a new method token - create a new method
                    IfFailGo(AddMethod(token));
                }

                break;

            case mdtFieldDef:

                // FieldDef token - add a new field
                LOG((LF_ENC, LL_INFO10000, "EACM::AEAC: Found field %08x\n", token));

                if (LookupFieldDef(token))
                {
                    // Field already exists - just ignore for now
                    continue;
                }

                // Field is new - add it
                IfFailGo(AddField(token));
                break;
        }
    }

    // Update the AvailableClassHash for reflection, etc. ensure that the new TypeRefs, AssemblyRefs and MethodDefs can be stored.
    ApplyMetaData();

ErrExit:
    if (pIMDInternalImportENC)
        pIMDInternalImportENC->EnumClose(&enumENC);

    return hr;
}

//---------------------------------------------------------------------------------------
//
// UpdateMethod - called when a method has been updated by EnC.
//
// The module's metadata has already been updated.  Here we notify the
// debugger of the update, and swap the new IL in as the current
// version of the method.
//
// Arguments:
//   pMethod  - the method being updated
//
// Return Value:
//    S_OK on success.
//    if the edit fails for any reason, at any point in this function,
//    we are toasted, so return out and IDE will end debug session.
//
// Assumptions:
//    The CLR must be suspended for debugging.
//
HRESULT EditAndContinueModule::UpdateMethod(MethodDesc *pMethod)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Notify the debugger of the update
    if (CORDebuggerAttached())
    {
        HRESULT hr = g_pDebugInterface->UpdateFunction(pMethod, m_applyChangesCount);
        if (FAILED(hr))
        {
            return hr;
        }
    }

    // Notify the JIT that we've got new IL for this method
    // This will ensure that all new calls to the method will go to the new version.
    // The runtime does this by never backpatching the methodtable slots in EnC-enabled modules.
    LOG((LF_ENC, LL_INFO100000, "EACM::UM: Updating function %s to version %d\n", pMethod->m_pszDebugMethodName, m_applyChangesCount));

    // Reset any flags relevant to the old code
    //
    // Note that this only works since we've very carefully made sure that _all_ references
    // to the Method's code must be to the call/jmp blob immediately in front of the
    // MethodDesc itself.  See MethodDesc::IsEnCMethod()
    //
    pMethod->ResetCodeEntryPointForEnC();

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// AddMethod - called when a new method is added by EnC.
//
// The module's metadata has already been updated.  Here we notify the
// debugger of the update, and create and add a new MethodDesc to the class.
//
// Arguments:
//   token    - methodDef token for the method being added
//
// Return Value:
//    S_OK on success.
//    if the edit fails for any reason, at any point in this function,
//    we are toasted, so return out and IDE will end debug session.
//
// Assumptions:
//    The CLR must be suspended for debugging.
//
HRESULT EditAndContinueModule::AddMethod(mdMethodDef token)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    mdTypeDef   parentTypeDef;
    HRESULT hr = GetMDImport()->GetParentToken(token, &parentTypeDef);
    if (FAILED(hr))
    {
        LOG((LF_ENC, LL_INFO100, "**Error** EnCModule::AM can't find parent token for method token %08x\n", token));
        return E_FAIL;
    }

    // see if the class is loaded yet.
    MethodTable * pParentType = LookupTypeDef(parentTypeDef).AsMethodTable();
    if (pParentType == NULL)
    {
        // Class isn't loaded yet, don't have to modify any existing EE data structures beyond the metadata.
        // Just notify debugger and return.
        LOG((LF_ENC, LL_INFO100, "EnCModule::AM class %08x not loaded (method %08x), our work is done\n", parentTypeDef, token));
        if (CORDebuggerAttached())
        {
            hr = g_pDebugInterface->UpdateNotYetLoadedFunction(token, this, m_applyChangesCount);
        }
        return hr;
    }

    // Add the method to the runtime's Class data structures
    LOG((LF_ENC, LL_INFO100000, "EACM::AM: Adding function %08x to type %08x\n", token, parentTypeDef));
    MethodDesc *pMethod = NULL;
    hr = EEClass::AddMethod(pParentType, token, 0, &pMethod);

    if (FAILED(hr))
    {
        _ASSERTE(!"Failed to add function");
        LOG((LF_ENC, LL_INFO100000, "**Error** EACM::AM: Failed to add function %08x with hr %08x\n", token, hr));
        return hr;
    }

    // Tell the debugger about the new method so it gets the version number properly
    if (CORDebuggerAttached())
    {
        hr = g_pDebugInterface->AddFunction(pMethod, m_applyChangesCount);
        if (FAILED(hr))
        {
            _ASSERTE(!"Failed to add function");
            LOG((LF_ENC, LL_INFO100000, "**Error** EACM::AF: Failed to add method %08x to debugger with hr %08x\n", token, hr));
        }
    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
// AddField - called when a new field is added by EnC.
//
// The module's metadata has already been updated.  Here we notify the
// debugger of the update,
//
// Arguments:
//   token    - fieldDef for the field being added
//
// Return Value:
//    S_OK on success.
//    if the edit fails for any reason, at any point in this function,
//    we are toasted, so return out and IDE will end debug session.
//
// Assumptions:
//    The CLR must be suspended for debugging.
//
HRESULT EditAndContinueModule::AddField(mdFieldDef token)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    mdTypeDef   parentTypeDef;
    HRESULT hr = GetMDImport()->GetParentToken(token, &parentTypeDef);

    if (FAILED(hr))
    {
        LOG((LF_ENC, LL_INFO100, "**Error** EnCModule::AF can't find parent token for field token %08x\n", token));
        return E_FAIL;
    }

    // see if the class is loaded yet. If not we don't need to do anything.  When this class is
    // loaded (with the updated metadata), it will have this field like any other normal field.
    // If the class hasn't been loaded, than the debugger shouldn't know anything about it
    // so there shouldn't be any harm in not notifying it of the update.  For completeness,
    // we may want to consider changing this to notify the debugger here as well.
    MethodTable * pParentType = LookupTypeDef(parentTypeDef).AsMethodTable();
    if (pParentType == NULL)
    {
        LOG((LF_ENC, LL_INFO100, "EnCModule::AF class %08x not loaded (field %08x), our work is done\n", parentTypeDef, token));
        return S_OK;
    }

    // Create a new EnCFieldDesc for the field and add it to the class
    LOG((LF_ENC, LL_INFO100000, "EACM::AM: Adding field %08x to type %08x\n", token, parentTypeDef));
    EnCFieldDesc *pField;
    hr = EEClass::AddField(pParentType, token, &pField);

    if (FAILED(hr))
    {
        LOG((LF_ENC, LL_INFO100000, "**Error** EACM::AF: Failed to add field %08x to EE with hr %08x\n", token, hr));
        return hr;
    }

    // Tell the debugger about the new field
    if (CORDebuggerAttached())
    {
        hr = g_pDebugInterface->AddField(pField, m_applyChangesCount);
        if (FAILED(hr))
        {
            LOG((LF_ENC, LL_INFO100000, "**Error** EACM::AF: Failed to add field %08x to debugger with hr %08x\n", token, hr));
        }
    }

#ifdef _DEBUG
    if (g_BreakOnEnCResolveField == -1)
    {
        g_BreakOnEnCResolveField = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnCResolveField);
    }
#endif

    return hr;
}

//---------------------------------------------------------------------------------------
//
// JitUpdatedFunction - Jit the new version of a function for EnC.
//
// Arguments:
//  pMD          - the MethodDesc for the method we want to JIT
//  pOrigContext - context of thread pointing into original version of the function
//
// Return value:
//  Return the address of the newly jitted code or NULL on failure.
//
PCODE EditAndContinueModule::JitUpdatedFunction( MethodDesc *pMD,
                                                 CONTEXT *pOrigContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_ENC, LL_INFO100, "EnCModule::JitUpdatedFunction for %s\n",
        pMD->m_pszDebugMethodName));

    PCODE jittedCode = NULL;

    GCX_COOP();

#ifdef _DEBUG
    BOOL shouldBreak = CLRConfig::GetConfigValue(
                                          CLRConfig::INTERNAL_EncJitUpdatedFunction);
    if (shouldBreak > 0) {
        _ASSERTE(!"EncJitUpdatedFunction");
    }
#endif

    // Setup a frame so that has context for the exception
    // so that gc can crawl the stack and do the right thing.
    _ASSERTE(pOrigContext);
    Thread *pCurThread = GetThread();
    FrameWithCookie<ResumableFrame> resFrame(pOrigContext);
    resFrame.Push(pCurThread);

    CONTEXT *pCtxTemp = NULL;
    // We need to zero out the filter context so a multi-threaded GC doesn't result
    // in somebody else tracing this thread & concluding that we're in JITted code.
    // We need to remove the filter context so that if we're in preemptive GC
    // mode, we'll either have the filter context, or the ResumableFrame,
    // but not both, set.
    // Since we're in cooperative mode here, we can swap the two non-atomically here.
    pCtxTemp = pCurThread->GetFilterContext();
    _ASSERTE(pCtxTemp != NULL); // currently called from within a filter context, protects us during GC-toggle.
    pCurThread->SetFilterContext(NULL);

    // get the code address (may jit the fcn if not already jitted)
    EX_TRY {
        if (!pMD->IsPointingToNativeCode())
        {
            GCX_PREEMP();
            pMD->DoPrestub(NULL);
            LOG((LF_ENC, LL_INFO100, "EnCModule::ResumeInUpdatedFunction JIT successful\n"));
        }
        else
        {
            LOG((LF_ENC, LL_INFO100, "EnCModule::ResumeInUpdatedFunction function already JITed\n"));
        }
        jittedCode = pMD->GetNativeCode();
    } EX_CATCH {
#ifdef _DEBUG
        {
            // This is debug-only code to print out the error string, but SString can throw.
            // This function is no-throw, and we can't put an EX_TRY inside an EX_CATCH block, so
            // we just have the violation.
            CONTRACT_VIOLATION(ThrowsViolation);

            StackSString exceptionMessage;
            SString errorMessage;
            GetExceptionMessage(GET_THROWABLE(), exceptionMessage);
            errorMessage.AppendASCII("**Error: Probable rude edit.**\n\n"
                                "EnCModule::JITUpdatedFunction JIT failed with the following exception:\n\n");
            errorMessage.Append(exceptionMessage);
            DbgAssertDialog(__FILE__, __LINE__, errorMessage.GetUTF8());
            LOG((LF_ENC, LL_INFO100, errorMessage.GetUTF8()));
        }
#endif
    } EX_END_CATCH(SwallowAllExceptions)

    resFrame.Pop(pCurThread);

    // Restore the filter context here (see comment above)
    pCurThread->SetFilterContext(pCtxTemp);

    return jittedCode;
}


//-----------------------------------------------------------------------------
// Called by EnC to resume the code in a new version of the function.
// This will:
// 1) jit the new function
// 2) set the IP to newILOffset within that new function
// 3) adjust local variables (particularly enregistered vars) to the new func.
// It will not return.
//
// Params:
//  pMD - method desc for method being updated. This is not enc-version aware.
//  oldDebuggerFuncHandle - Debugger DJI to uniquely identify old function.
//    This is enc-version aware.
//  newILOffset - the IL offset to resume execution at within the new function.
//  pOrigContext - context of thread pointing into original version of the function.
//
// This function must be called on the thread that's executing the old function.
// This function does not return. Instead, it will remap this thread directly
// to be executing the new function.
//-----------------------------------------------------------------------------
HRESULT EditAndContinueModule::ResumeInUpdatedFunction(
    MethodDesc *pMD,
    void *oldDebuggerFuncHandle,
    SIZE_T newILOffset,
    CONTEXT *pOrigContext)
{
#if defined(TARGET_ARM) || defined(TARGET_LOONGARCH64)
    return E_NOTIMPL;
#else
    LOG((LF_ENC, LL_INFO100, "EnCModule::ResumeInUpdatedFunction for %s at IL offset 0x%x, ",
        pMD->m_pszDebugMethodName, newILOffset));

#ifdef _DEBUG
    BOOL shouldBreak = CLRConfig::GetConfigValue(
                                          CLRConfig::INTERNAL_EncResumeInUpdatedFunction);
    if (shouldBreak > 0) {
        _ASSERTE(!"EncResumeInUpdatedFunction");
    }
#endif

    HRESULT hr = E_FAIL;

    // JIT-compile the updated version of the method
    PCODE jittedCode = JitUpdatedFunction(pMD, pOrigContext);
    if ( jittedCode == NULL )
        return CORDBG_E_ENC_JIT_CANT_UPDATE;

    GCX_COOP();

    // This will create a new frame and copy old vars to it
    // need pointer to old & new code, old & new info

    EECodeInfo oldCodeInfo(GetIP(pOrigContext));
    _ASSERTE(oldCodeInfo.GetMethodDesc() == pMD);

    // Get the new native offset & IP from the new IL offset
    LOG((LF_ENC, LL_INFO10000, "EACM::RIUF: About to map IL forwards!\n"));
    SIZE_T newNativeOffset = 0;
    g_pDebugInterface->MapILInfoToCurrentNative(pMD,
                                                newILOffset,
                                                jittedCode,
                                                &newNativeOffset);

    EECodeInfo newCodeInfo(jittedCode + newNativeOffset);
    _ASSERTE(newCodeInfo.GetMethodDesc() == pMD);

    _ASSERTE(newCodeInfo.GetRelOffset() == newNativeOffset);

    _ASSERTE(oldCodeInfo.GetCodeManager() == newCodeInfo.GetCodeManager());

#ifdef TARGET_ARM64
    // GCInfo for old method
    GcInfoDecoder oldGcDecoder(
        oldCodeInfo.GetGCInfoToken(),
        GcInfoDecoderFlags(DECODE_EDIT_AND_CONTINUE),
        0       // Instruction offset (not needed)
        );

    // GCInfo for new method
    GcInfoDecoder newGcDecoder(
        newCodeInfo.GetGCInfoToken(),
        GcInfoDecoderFlags(DECODE_EDIT_AND_CONTINUE),
        0       // Instruction offset (not needed)
        );

    DWORD oldFrameSize = oldGcDecoder.GetSizeOfEditAndContinueFixedStackFrame();
    DWORD newFrameSize = newGcDecoder.GetSizeOfEditAndContinueFixedStackFrame();
#else
    DWORD oldFrameSize = oldCodeInfo.GetFixedStackSize();
    DWORD newFrameSize = newCodeInfo.GetFixedStackSize();
#endif

    // FixContextAndResume() will replace the old stack frame of the function with the new
    // one and will initialize that new frame to null. Anything on the stack where that new
    // frame sits will be wiped out. This could include anything on the stack right up to or beyond our
    // current stack from in ResumeInUpdatedFunction. In order to prevent our current frame from being
    // trashed we determine the maximum amount that the stack could grow by and allocate this as a buffer using
    // alloca. Then we call FixContextAndResume which can safely rely on the stack because none of it's frames
    // state or anything lower can be reached by the new frame.

    if( newFrameSize > oldFrameSize)
    {
        DWORD frameIncrement = newFrameSize - oldFrameSize;
        // alloca() has __attribute__((warn_unused_result)) in glibc, for which gcc 11+ issue `-Wunused-result` even with `(void)alloca(..)`,
        // so we use additional NOT(!) operator to force unused-result suppression.
        (void)!alloca(frameIncrement);
    }

    // Ask the EECodeManager to actually fill in the context and stack for the new frame so that
    // values of locals etc. are preserved.
    LOG((LF_ENC, LL_INFO100, "EnCModule::ResumeInUpdatedFunction calling FixContextAndResume oldNativeOffset: 0x%x, newNativeOffset: 0x%x,"
        "oldFrameSize: 0x%x, newFrameSize: 0x%x\n",
        oldCodeInfo.GetRelOffset(), newCodeInfo.GetRelOffset(), oldFrameSize, newFrameSize));

    FixContextAndResume(pMD,
                        oldDebuggerFuncHandle,
                        pOrigContext,
                        &oldCodeInfo,
                        &newCodeInfo);

    // At this point we shouldn't have failed, so this is genuinely erroneous.
    LOG((LF_ENC, LL_ERROR, "**Error** EnCModule::ResumeInUpdatedFunction returned from ResumeAtJit"));
    _ASSERTE(!"Should not return from FixContextAndResume()");

    hr = E_FAIL;

    // If we fail for any reason we have already potentially trashed with new locals and we have also unwound any
    // Win32 handlers on the stack so cannot ever return from this function.
    EEPOLICY_HANDLE_FATAL_ERROR(CORDBG_E_ENC_INTERNAL_ERROR);
    return hr;
#endif // #if defined(TARGET_ARM) || defined(TARGET_LOONGARCH64)
}

//---------------------------------------------------------------------------------------
//
// FixContextAndResume - Modify the thread context for EnC remap and resume execution
//
// Arguments:
//    pMD      - MethodDesc for the method being remapped
//    oldDebuggerFuncHandle - Debugger DJI to uniquely identify old function.
//    pContext - the thread's original CONTEXT when the remap opportunity was hit
//    pOldCodeInfo - collection of various information about the current frame state
//    pNewCodeInfo - information about how we want the frame state to be after the remap
//
// Return Value:
//    Doesn't return
//
// Notes:
//   WARNING: This method cannot access any stack-data below its frame on the stack
//   (i.e. anything allocated in a caller frame), so all stack-based arguments must
//   EXPLICITLY be copied by value and this method cannot be inlined.  We may need to expand
//   the stack frame to accommodate the new method, and so extra buffer space must have
//   been allocated on the stack.  Note that passing a struct by value (via C++) is not
//   enough to ensure its data is really copied (on x64, large structs may internally be
//   passed by reference).  Thus we explicitly make copies of structs passed in, at the
//   beginning.
//

NOINLINE void EditAndContinueModule::FixContextAndResume(
        MethodDesc *pMD,
        void *oldDebuggerFuncHandle,
        T_CONTEXT *pContext,
        EECodeInfo *pOldCodeInfo,
        EECodeInfo *pNewCodeInfo)
{
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_GC_TRIGGERS; // Sends IPC event
    STATIC_CONTRACT_THROWS;

    // Create local copies of all structs passed as arguments to prevent them from being overwritten
    CONTEXT context;
    memcpy(&context, pContext, sizeof(CONTEXT));
    pContext = &context;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    // Since we made a copy of the incoming CONTEXT in context, clear any new flags we
    // don't understand (like XSAVE), since we'll eventually be passing a CONTEXT based
    // on this copy to ClrRestoreNonvolatileContext, and this copy doesn't have the extra info
    // required by the XSAVE or other flags.
    //
    // FUTURE: No reason to ifdef this for amd64-only, except to make this late fix as
    // surgical as possible.  Would be nice to enable this on x86 early in the next cycle.
    pContext->ContextFlags &= CONTEXT_ALL;
#endif // defined(TARGET_AMD64)

    EECodeInfo oldCodeInfo;
    memcpy(&oldCodeInfo, pOldCodeInfo, sizeof(EECodeInfo));
    pOldCodeInfo = &oldCodeInfo;

    EECodeInfo newCodeInfo;
    memcpy(&newCodeInfo, pNewCodeInfo, sizeof(EECodeInfo));
    pNewCodeInfo = &newCodeInfo;

    const ICorDebugInfo::NativeVarInfo *pOldVarInfo = NULL;
    const ICorDebugInfo::NativeVarInfo *pNewVarInfo = NULL;
    SIZE_T oldVarInfoCount = 0;
    SIZE_T newVarInfoCount = 0;

    // Get the var info which the codemanager will use for updating
    // enregistered variables correctly, or variables whose lifetimes differ
    // at the update point
    g_pDebugInterface->GetVarInfo(pMD, oldDebuggerFuncHandle, &oldVarInfoCount, &pOldVarInfo);
    g_pDebugInterface->GetVarInfo(pMD, NULL,                  &newVarInfoCount, &pNewVarInfo);

#ifdef TARGET_X86
    // save the frame pointer as FixContextForEnC might step on it.
    LPVOID oldSP = dac_cast<PTR_VOID>(GetSP(pContext));

    // need to pop the SEH records before write over the stack in FixContextForEnC
    PopSEHRecords(oldSP);
#endif

    // Ask the EECodeManager to actually fill in the context and stack for the new frame so that
    // values of locals etc. are preserved.
    HRESULT hr = pNewCodeInfo->GetCodeManager()->FixContextForEnC(
                    pContext,
                    pOldCodeInfo,
                    pOldVarInfo, oldVarInfoCount,
                    pNewCodeInfo,
                    pNewVarInfo, newVarInfoCount);

    // If FixContextForEnC succeeded, the stack is potentially trashed with any new locals and we have also unwound
    // any Win32 handlers on the stack so cannot ever return from this function. If FixContextForEnC failed, can't
    // assume that the stack is still intact so apply the proper policy for a fatal EE error to bring us down
    // "gracefully" (it's all relative).
    if (FAILED(hr))
    {
        LOG((LF_ENC, LL_INFO100, "**Error** EnCModule::ResumeInUpdatedFunction for FixContextForEnC failed\n"));
        EEPOLICY_HANDLE_FATAL_ERROR(hr);
    }

    // Set the new IP
    // Note that all we're really doing here is setting the IP register.  We unfortunately don't
    // share any code with the implementation of debugger SetIP, despite the similarities.
    LOG((LF_ENC, LL_INFO100, "EnCModule::ResumeInUpdatedFunction: Resume at EIP=%p\n", pNewCodeInfo->GetCodeAddress()));

    Thread *pCurThread = GetThread();
    pCurThread->SetFilterContext(pContext);
    SetIP(pContext, pNewCodeInfo->GetCodeAddress());

    // Notify the debugger that we're about to resume execution in the new version of the method
    HRESULT hrIgnore = g_pDebugInterface->RemapComplete(pMD, pNewCodeInfo->GetCodeAddress(), pNewCodeInfo->GetRelOffset());

    // Now jump into the new version of the method. Note that we can't just setup the filter context
    // and return because we are potentially writing new vars onto the stack.
    pCurThread->SetFilterContext( NULL );

#ifdef OUT_OF_PROCESS_SETTHREADCONTEXT
    if (g_pDebugInterface->IsOutOfProcessSetContextEnabled())
    {
        g_pDebugInterface->SendSetThreadContextNeeded(pContext);
    }
    else
    {
#endif // OUT_OF_PROCESS_SETTHREADCONTEXT
#if defined(TARGET_X86)
    ResumeAtJit(pContext, oldSP);
#else
    ClrRestoreNonvolatileContext(pContext);
#endif
#ifdef OUT_OF_PROCESS_SETTHREADCONTEXT
    }
#endif // OUT_OF_PROCESS_SETTHREADCONTEXT

    // At this point we shouldn't have failed, so this is genuinely erroneous.
    LOG((LF_ENC, LL_ERROR, "**Error** EnCModule::ResumeInUpdatedFunction returned from ResumeAtJit"));
    _ASSERTE(!"Should not return from ResumeAtJit()");
}
#endif // #ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
// ResolveField - get a pointer to the value of a field that was added by EnC
//
// Arguments:
//   thisPointer  - For instance fields, a pointer to the object instance of interest.
//                  For static fields this is unused and should be NULL.
//   pFD          - FieldDesc describing the field we're interested in
//   fAllocateNew - If storage doesn't yet exist for this field and fAllocateNew is true
//                  then we will attempt to allocate the storage (throwing an exception
//                  if it fails).  Otherwise, if fAllocateNew is false, then we will just
//                  return NULL when the storage is not yet available.
//
// Return Value:
//      If storage doesn't yet exist for this field we return NULL, otherwise, we return a pointer
//      to the contents of the field on success.
//---------------------------------------------------------------------------------------
PTR_CBYTE EditAndContinueModule::ResolveField(OBJECTREF      thisPointer,
                                              EnCFieldDesc * pFD)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    if (g_BreakOnEnCResolveField == 1)
    {
        _ASSERTE( !"EditAndContinueModule::ResolveField");
    }
#endif

    // If it's static, we stash in the EnCFieldDesc
    if (pFD->IsStatic())
    {
        _ASSERTE( thisPointer == NULL );
        EnCAddedStaticField *pAddedStatic = pFD->GetStaticFieldData();
        if (!pAddedStatic)
        {
            return NULL;
        }

        _ASSERTE( pAddedStatic->m_pFieldDesc == pFD );
        return PTR_CBYTE(pAddedStatic->GetFieldData());
    }

    // not static so get it out of the syncblock
    SyncBlock * pBlock = NULL;

    // Get the SyncBlock, failing if not available
    pBlock = thisPointer->PassiveGetSyncBlock();
    if( pBlock == NULL )
    {
        return NULL;
    }

    EnCSyncBlockInfo * pEnCInfo = NULL;

    // Attempt to get the EnC information from the sync block
    pEnCInfo = pBlock->GetEnCInfo();

    if (!pEnCInfo)
    {
        // No EnC info on this object yet, fail since we don't want to allocate it
        return NULL;
    }

    // Lookup the actual field value from the EnCSyncBlockInfo
    return pEnCInfo->ResolveField(thisPointer, pFD);
} // EditAndContinueModule::ResolveField

#ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
// ResolveOrAllocateField - get a pointer to the value of a field that was added by EnC,
// allocating storage for it if necessary
//
// Arguments:
//   thisPointer  - For instance fields, a pointer to the object instance of interest.
//                  For static fields this is unused and should be NULL.
//   pFD          - FieldDesc describing the field we're interested in
// Return Value:
//      Returns a pointer to the contents of the field on success. This should only fail due
//      to out-of-memory and will therefore throw an OOM exception.
//---------------------------------------------------------------------------------------
PTR_CBYTE EditAndContinueModule::ResolveOrAllocateField(OBJECTREF      thisPointer,
                                                        EnCFieldDesc * pFD)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    // first try getting a pre-existing field
    PTR_CBYTE fieldAddr = ResolveField(thisPointer, pFD);
    if (fieldAddr != NULL)
    {
        return fieldAddr;
    }

    // we didn't find the field already allocated
    if (pFD->IsStatic())
    {
        _ASSERTE(thisPointer == NULL);
        EnCAddedStaticField * pAddedStatic = pFD->GetOrAllocateStaticFieldData();
        _ASSERTE(pAddedStatic->m_pFieldDesc == pFD);
        return PTR_CBYTE(pAddedStatic->GetFieldData());
    }

     // not static so get it out of the syncblock
    SyncBlock* pBlock = NULL;

    // Get the SyncBlock, creating it if necessary
    pBlock = thisPointer->GetSyncBlock();

    EnCSyncBlockInfo * pEnCInfo = NULL;

    // Attempt to get the EnC information from the sync block
    pEnCInfo = pBlock->GetEnCInfo();

    if (!pEnCInfo)
    {
        // Attach new EnC field info to this object.
        pEnCInfo = new EnCSyncBlockInfo;
        if (!pEnCInfo)
        {
            COMPlusThrowOM();
        }
        pBlock->SetEnCInfo(pEnCInfo);
    }

    // Lookup the actual field value from the EnCSyncBlockInfo
    return pEnCInfo->ResolveOrAllocateField(thisPointer, pFD);
} // EditAndContinueModule::ResolveOrAllocateField

#endif // !DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Get or optionally create an EnCEEClassData object for the specified
// EEClass in this module.
//
// Arguments:
//   pClass  - the EEClass of interest
//   getOnly - if false (the default), we'll create a new entry of none exists yet
//
// Note: If called in a DAC build, GetOnly must be TRUE
//
PTR_EnCEEClassData EditAndContinueModule::GetEnCEEClassData(MethodTable * pMT, BOOL getOnly /*=FALSE*/ )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#ifdef DACCESS_COMPILE
    _ASSERTE(getOnly == TRUE);
#endif // DACCESS_COMPILE

    DPTR(PTR_EnCEEClassData) ppData = m_ClassList.Table();
    DPTR(PTR_EnCEEClassData) ppLast = ppData + m_ClassList.Count();

    // Look for an existing entry for the specified class
    while (ppData < ppLast)
    {
        PREFIX_ASSUME(ppLast != NULL);
        if ((*ppData)->GetMethodTable() == pMT)
            return *ppData;
        ++ppData;
    }

    // No match found. Return now if we don't want to create a new entry
    if (getOnly)
    {
        return NULL;
    }

#ifndef DACCESS_COMPILE
    // Create a new entry and add it to the end our our table
    EnCEEClassData *pNewData = (EnCEEClassData*)(void*)pMT->GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem_NoThrow(S_SIZE_T(sizeof(EnCEEClassData)));
    pNewData->Init(pMT);
    ppData = m_ClassList.Append();
    if (!ppData)
        return NULL;
    *ppData = pNewData;
    return pNewData;
#else
    DacNotImpl();
    return NULL;
#endif
}

// Computes the address of this field within the object "o"
void *EnCFieldDesc::GetAddress( void *o)
{
#ifndef DACCESS_COMPILE
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    // can't throw through FieldDesc::GetInstanceField if FORBIDGC_LOADER_USE_ENABLED
    _ASSERTE(! FORBIDGC_LOADER_USE_ENABLED());

    EditAndContinueModule *pModule = (EditAndContinueModule*)GetModule();
    _ASSERTE(pModule->IsEditAndContinueEnabled());

    // EnC added fields aren't just at some static offset in the object like normal fields
    // are.  Get the EditAndContinueModule to compute the address for us.
    return (void *)pModule->ResolveOrAllocateField(ObjectToOBJECTREF((Object *)o), this);
#else
    DacNotImpl();
    return NULL;
#endif
}

#ifndef DACCESS_COMPILE

// Do simple field initialization
// We do this when the process is suspended for debugging (in a GC_NOTRIGGER).
// Full initialization will be done in Fixup when the process is running.
void EnCFieldDesc::Init(mdFieldDef token, BOOL fIsStatic)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Clear out the FieldDesc incase someone attempts to use any of the fields
    memset( this, 0, sizeof(EnCFieldDesc) );

    // Initialize our members
    m_pStaticFieldData = NULL;
    m_bNeedsFixup = TRUE;

    // Initialize the bare minimum of FieldDesc necessary for now
    if (fIsStatic)
        FieldDesc::m_isStatic = TRUE;

    SetMemberDef(token);

    SetEnCNew();
}

// Allocate a new EnCAddedField instance and hook it up to hold the value for an instance
// field which was added by EnC to the specified object.  This effectively adds a reference from
// the object to the new field value so that the field's lifetime is managed properly.
//
// Arguments:
//  pFD         - description of the field being added
//  thisPointer - object instance to attach the new field to
//
EnCAddedField *EnCAddedField::Allocate(OBJECTREF thisPointer, EnCFieldDesc *pFD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    LOG((LF_ENC, LL_INFO1000, "\tEnCAF:Allocate for this %p, FD %p\n",  OBJECTREFToObject(thisPointer), pFD->GetMemberDef()));

    // Create a new EnCAddedField instance
    EnCAddedField *pEntry = new EnCAddedField;
    pEntry->m_pFieldDesc = pFD;

    AppDomain *pDomain = (AppDomain*) pFD->GetApproxEnclosingMethodTable()->GetDomain();

    // We need to associate the contents of the new field with the object it is attached to
    // in a way that mimics the lifetime behavior of a normal field reference.  Specifically,
    // when the object is collected, the field should also be collected (assuming there are no
    // other references), but references to the field shouldn't keep the object alive.
    // To achieve this, we have introduced the concept of a "dependent handle" which provides
    // the appropriate semantics.  The dependent handle has a weak reference to a "primary object"
    // (the object getting a new field in this case), and a strong reference to a secondary object.
    // When the primary object is collected, the reference to the secondary object is released.
    // See the definition of code:HNDTYPE_DEPENDENT and code:Ref_ScanDependentHandles for more details.
    //
    // We create a helper object and store it as the secondary object in the dependant handle
    // so that its liveliness can be maintained along with the primary object.
    // The helper then contains an object reference to the real field value that we are adding.
    // The reason for doing this is that we cannot hand out the handle address for
    // the OBJECTREF address so we need to hand out something else that is hooked up to the handle.

    GCPROTECT_BEGIN(thisPointer);
    MethodTable *pHelperMT = CoreLibBinder::GetClass(CLASS__ENC_HELPER);
    pEntry->m_FieldData = pDomain->CreateDependentHandle(thisPointer, AllocateObject(pHelperMT));
    GCPROTECT_END();

    LOG((LF_ENC, LL_INFO1000, "\tEnCAF:Allocate created dependent handle %p\n",pEntry->m_FieldData));

    // The EnC helper object stores a reference to the actual field value.  For fields which are
    // reference types, this is simply a normal object reference so we don't need to do anything
    // special here.

    if (pFD->GetFieldType() != ELEMENT_TYPE_CLASS)
    {
        // The field is a value type so we need to create storage on the heap to hold a boxed
        // copy of the value and have the helper's objectref point there.

        OBJECTREF obj = NULL;
        if (pFD->IsByValue())
        {
            // Create a boxed version of the value class. This allows the standard GC algorithm
            // to take care of internal pointers into the value class.
            obj = AllocateObject(pFD->GetFieldTypeHandleThrowing().GetMethodTable());
        }
        else
        {
            // In the case of primitive types, we use a reference to a 1-element array on the heap.
            // I'm not sure why we bother treating primitives specially, it seems like we should be able
            // to just box any value type including primitives.
            obj = AllocatePrimitiveArray(ELEMENT_TYPE_I1, GetSizeForCorElementType(pFD->GetFieldType()));
        }
        GCPROTECT_BEGIN (obj);

        // Get a FieldDesc for the object reference field in the EnC helper object (warning: triggers)
        FieldDesc *pHelperField = CoreLibBinder::GetField(FIELD__ENC_HELPER__OBJECT_REFERENCE);

        // store the empty boxed object into the helper object
        IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
        OBJECTREF pHelperObj = ObjectToOBJECTREF(mgr->GetDependentHandleSecondary(pEntry->m_FieldData));
        OBJECTREF *pHelperRef = (OBJECTREF *)pHelperField->GetAddress( pHelperObj->GetAddress() );
        SetObjectReference( pHelperRef, obj);

        GCPROTECT_END ();
    }

    return pEntry;
}
#endif // !DACCESS_COMPILE

//---------------------------------------------------------------------------------------
// EnCSyncBlockInfo::GetEnCFieldAddrFromHelperFieldDesc
// Gets the address of an EnC field accounting for its type: valuetype, class or primitive
// Arguments:
//     input:  pHelperFieldDesc - FieldDesc for the enc helper object
//             pHelper          - EnC helper (points to list of added fields)
//             pFD              - fieldDesc describing the field of interest
// Return value: the address of the EnC added field
//---------------------------------------------------------------------------------------
PTR_CBYTE EnCSyncBlockInfo::GetEnCFieldAddrFromHelperFieldDesc(FieldDesc *        pHelperFieldDesc,
                                                               OBJECTREF          pHelper,
                                                               EnCFieldDesc *     pFD)
{
     WRAPPER_NO_CONTRACT;
     SUPPORTS_DAC;

    _ASSERTE(pHelperFieldDesc != NULL);
    _ASSERTE(pHelper != NULL);

    // Get the address of the reference inside the helper object which points to
    // the field contents
    PTR_OBJECTREF pOR = dac_cast<PTR_OBJECTREF>(pHelperFieldDesc->GetAddress(pHelper->GetAddress()));
    _ASSERTE(pOR != NULL);

    PTR_CBYTE retAddr = NULL;

    // Compute the address to the actual field contents based on the field type
    // See the description above Allocate for details
    if (pFD->IsByValue())
    {
        // field value is a value type, we store it boxed so get the pointer to the first field
        retAddr = dac_cast<PTR_CBYTE>((*pOR)->UnBox());
    }
    else if (pFD->GetFieldType() == ELEMENT_TYPE_CLASS)
    {
        // field value is a reference type, we store the objref directly
        retAddr = dac_cast<PTR_CBYTE>(pOR);
    }
    else
    {
        // field value is a primitive, we store it inside a 1-element array
        OBJECTREF objRef = *pOR;
        I1ARRAYREF primitiveArray = dac_cast<I1ARRAYREF>(objRef);
        retAddr = dac_cast<PTR_CBYTE>(primitiveArray->GetDirectPointerToNonObjectElements());
    }

    LOG((LF_ENC, LL_INFO1000, "\tEnCSBI:RF address of %s type member is %p\n",
        (pFD->IsByValue() ? "ByValue" : pFD->GetFieldType() == ELEMENT_TYPE_CLASS ? "Class" : "Other"), retAddr));

    return retAddr;
} // EnCSyncBlockInfo::GetEnCFieldAddrFromHelperFieldDesc

//---------------------------------------------------------------------------------------
// EnCSyncBlockInfo::ResolveField
// Get the address of the data referenced by an instance field that was added with EnC
// Arguments:
//   thisPointer  - the object instance whose field to access
//   pFD          - fieldDesc describing the field of interest
// Return value: Returns a pointer to the data referenced by an EnC added instance field
//---------------------------------------------------------------------------------------
PTR_CBYTE EnCSyncBlockInfo::ResolveField(OBJECTREF thisPointer, EnCFieldDesc *pFD)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // We should only be passed FieldDescs for instance fields
    _ASSERTE(!pFD->IsStatic());

    PTR_EnCAddedField pEntry = NULL;

    LOG((LF_ENC, LL_INFO1000, "EnCSBI:RF for this %p, FD %08x\n", OBJECTREFToObject(thisPointer), pFD->GetMemberDef()));

    // This list is not synchronized--it hasn't proved a problem, but we could conceivably see race conditions
    // arise here.
    // Look for an entry for the requested field in our linked list
    pEntry = m_pList;
    while (pEntry && pEntry->m_pFieldDesc != pFD)
    {
        pEntry = pEntry->m_pNext;
    }

    if (!pEntry)
    {
        // No existing entry - we have to return NULL
        return NULL;
    }

    // we found a matching entry in the list of EnCAddedFields
    // Get the EnC helper object (see the detailed description in Allocate above)
#ifdef DACCESS_COMPILE
    OBJECTREF pHelper = GetDependentHandleSecondary(pEntry->m_FieldData);
#else // DACCESS_COMPILE
    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
    OBJECTREF pHelper = ObjectToOBJECTREF(mgr->GetDependentHandleSecondary(pEntry->m_FieldData));
#endif // DACCESS_COMPILE
    _ASSERTE(pHelper != NULL);

    FieldDesc *pHelperFieldDesc = NULL;

    // We _HAVE_ to call GetExistingField b/c (a) we can't throw exceptions, and
    // (b) we _DON'T_ want to run class init code, either.
    pHelperFieldDesc = CoreLibBinder::GetExistingField(FIELD__ENC_HELPER__OBJECT_REFERENCE);
    if (pHelperFieldDesc == NULL)
    {
        return NULL;
    }
    else
    {
        return GetEnCFieldAddrFromHelperFieldDesc(pHelperFieldDesc, pHelper, pFD);
    }
} // EnCSyncBlockInfo::ResolveField

#ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
// EnCSyncBlockInfo::ResolveOrAllocateField
// get the address of an EnC added field, allocating it if it doesn't yet exist
// Arguments:
//   thisPointer  - the object instance whose field to access
//   pFD          - fieldDesc describing the field of interest
// Return value: Returns a pointer to the data referenced by an instance field that was added with EnC
//---------------------------------------------------------------------------------------
PTR_CBYTE EnCSyncBlockInfo::ResolveOrAllocateField(OBJECTREF thisPointer, EnCFieldDesc *pFD)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        WRAPPER(THROWS);
    }
    CONTRACTL_END;

    // We should only be passed FieldDescs for instance fields
    _ASSERTE( !pFD->IsStatic() );

    // first try to get the address of a pre-existing field (storage has already been allocated)
    PTR_CBYTE retAddr = ResolveField(thisPointer, pFD);

    if (retAddr != NULL)
    {
        return retAddr;
    }

    // if the field doesn't yet have available storage, we'll have to allocate it.
    PTR_EnCAddedField pEntry = NULL;

    LOG((LF_ENC, LL_INFO1000, "EnCSBI:RF for this %p, FD %08x\n",  OBJECTREFToObject(thisPointer), pFD->GetMemberDef()));

    // This list is not synchronized--it hasn't proved a problem, but we could conceivably see race conditions
    // arise here.
    // Because we may have additions to the head of m_pList at any time, we have to keep searching this
    // until we either find a match or succeed in allocating a new entry and adding it to the list
    do
    {
        // Look for an entry for the requested field in our linked list (maybe it was just added)
        pEntry = m_pList;
        while (pEntry && pEntry->m_pFieldDesc != pFD)
        {
            pEntry = pEntry->m_pNext;
        }

        if (pEntry)
        {
            // match found
            break;
        }

        // Allocate an entry and tie it to the object instance
        pEntry = EnCAddedField::Allocate(thisPointer, pFD);

        // put at front of list so the list is in order of most recently added
        pEntry->m_pNext = m_pList;
        if (InterlockedCompareExchangeT(&m_pList, pEntry, pEntry->m_pNext) == pEntry->m_pNext)
            break;

        // There was a race and another thread modified the list here, so we need to try again
        // We should do this so rarely, and EnC perf is of relatively little
        // consequence, we should just be taking a lock here to simplify this code.
        // @todo - We leak a GC handle here. Allocate() above alloced a GC handle in m_FieldData.
        // There's no dtor for pEntry to free it.
        delete pEntry;
    } while (TRUE);

    // we found a matching entry in the list of EnCAddedFields
    // Get the EnC helper object (see the detailed description in Allocate above)
    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
    OBJECTREF pHelper = ObjectToOBJECTREF(mgr->GetDependentHandleSecondary(pEntry->m_FieldData));
    _ASSERTE(pHelper != NULL);

    FieldDesc * pHelperField = NULL;
    GCPROTECT_BEGIN (pHelper);
    pHelperField = CoreLibBinder::GetField(FIELD__ENC_HELPER__OBJECT_REFERENCE);
    GCPROTECT_END ();

    return GetEnCFieldAddrFromHelperFieldDesc(pHelperField, pHelper, pFD);
} // EnCSyncBlockInfo::ResolveOrAllocateField

// Free all the resources associated with the fields added to this object instance
// This is invoked after the object instance has been collected, and the SyncBlock is
// being reclaimed.
//
// Note, this is not threadsafe, and so should only be called when we know no-one else
// maybe using this SyncBlockInfo.
void EnCSyncBlockInfo::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Walk our linked list of all the fields that were added
    EnCAddedField *pEntry = m_pList;
    while (pEntry)
    {
        // Clean up the handle we created in EnCAddedField::Allocate
        DestroyDependentHandle(*(OBJECTHANDLE*)&pEntry->m_FieldData);

        // Delete this list entry and move onto the next
        EnCAddedField *next = pEntry->m_pNext;
        delete pEntry;
        pEntry = next;
    }

    // Finally, delete the sync block info itself
    delete this;
}

// Allocate space to hold the value for the new static field
EnCAddedStaticField *EnCAddedStaticField::Allocate(EnCFieldDesc *pFD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    AppDomain *pDomain = (AppDomain*) pFD->GetApproxEnclosingMethodTable()->GetDomain();

    // Compute the size of the fieldData entry
    size_t fieldSize;
    if (pFD->IsByValue() || pFD->GetFieldType() == ELEMENT_TYPE_CLASS) {
        // We store references to reference types or boxed value types
        fieldSize = sizeof(OBJECTREF*);
    } else {
       // We store primitives inline
        fieldSize = GetSizeForCorElementType(pFD->GetFieldType());
    }

    // allocate an instance with space for the field data
    EnCAddedStaticField *pEntry = (EnCAddedStaticField *)
        (void*)pDomain->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(offsetof(EnCAddedStaticField, m_FieldData)) + S_SIZE_T(fieldSize));
    pEntry->m_pFieldDesc = pFD;

    // Create a static objectref to point to the field contents, except for primitives
    // which will use the memory available in-line at m_FieldData for storage.
    // We use static object refs for static fields as these fields won't go away
    // unless the module is unloaded, and they can easily be found by GC.
    if (pFD->IsByValue())
    {
        // create a boxed version of the value class.  This allows the standard GC
        // algorithm to take care of internal pointers in the value class.
        OBJECTREF **pOR = (OBJECTREF**)&pEntry->m_FieldData;
        *pOR = pDomain->AllocateStaticFieldObjRefPtrs(1);
        OBJECTREF obj = AllocateObject(pFD->GetFieldTypeHandleThrowing().GetMethodTable());
        SetObjectReference( *pOR, obj);
    }
    else if (pFD->GetFieldType() == ELEMENT_TYPE_CLASS)
    {
        // references to reference-types are stored directly in the field data
        OBJECTREF **pOR = (OBJECTREF**)&pEntry->m_FieldData;
        *pOR = pDomain->AllocateStaticFieldObjRefPtrs(1);
    }

    return pEntry;
}
#endif // !DACCESS_COMPILE
// GetFieldData - return the ADDRESS where the field data is located
PTR_CBYTE EnCAddedStaticField::GetFieldData()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    if ( (m_pFieldDesc->IsByValue()) || (m_pFieldDesc->GetFieldType() == ELEMENT_TYPE_CLASS) )
    {
        // It's indirect via an ObjRef at m_FieldData. This is a TADDR, so we need to make a PTR_CBYTE from
        // the ObjRef
        return *(PTR_CBYTE *)&m_FieldData;
    }
    else
    {
        // An elementry type. It's stored directly in m_FieldData. In this case, we need to get the target
        // address of the m_FieldData data member and marshal it via the DAC.
        return dac_cast<PTR_CBYTE>(PTR_HOST_MEMBER_TADDR(EnCAddedStaticField, this, m_FieldData));
    }
}

// Gets a pointer to the field's contents (assuming this is a static field)
// We'll return NULL if we don't yet have a pointer to the data.
// Arguments: none
// Return value: address of the static field data if available or NULL otherwise
EnCAddedStaticField * EnCFieldDesc::GetStaticFieldData()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(IsStatic());

    return m_pStaticFieldData;
}

#ifndef DACCESS_COMPILE
// Gets a pointer to the field's contents (assuming this is a static field)
// Arguments: none
// Return value: address of the field data. If  we don't yet have a pointer to the data,
// this will allocate space to store it.
// May throw OOM.
EnCAddedStaticField * EnCFieldDesc::GetOrAllocateStaticFieldData()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(IsStatic());

    // If necessary and requested, allocate space for the static field data
    if (!m_pStaticFieldData)
    {
        m_pStaticFieldData = EnCAddedStaticField::Allocate(this);
    }

    return m_pStaticFieldData;
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
// Adds the provided new field to the appropriate linked list and updates the appropriate count
void EnCEEClassData::AddField(EnCAddedFieldElement *pAddedField)
{
    LIMITED_METHOD_CONTRACT;
    // Determine the appropriate field list and update the field counter
    EnCFieldDesc *pFD = &pAddedField->m_fieldDesc;
    EnCAddedFieldElement **pList;
    if (pFD->IsStatic())
    {
        ++m_dwNumAddedStaticFields;
        pList = &m_pAddedStaticFields;
    }
    else
    {
        ++m_dwNumAddedInstanceFields;
        pList = &m_pAddedInstanceFields;
    }

    // If the list is empty, just add this field as the only entry
    if (*pList == NULL)
    {
        *pList = pAddedField;
        return;
    }

    // Otherwise, add this field to the end of the field list
    EnCAddedFieldElement *pCur = *pList;
    while (pCur->m_next != NULL)
    {
        pCur = pCur->m_next;
    }
    pCur->m_next = pAddedField;
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
EnCEEClassData::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    if (m_pMT.IsValid())
    {
        m_pMT->EnumMemoryRegions(flags);
    }

    PTR_EnCAddedFieldElement elt = m_pAddedInstanceFields;
    while (elt.IsValid())
    {
        elt.EnumMem();
        elt = elt->m_next;
    }
    elt = m_pAddedStaticFields;
    while (elt.IsValid())
    {
        elt.EnumMem();
        elt = elt->m_next;
    }
}

void
EditAndContinueModule::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                         bool enumThis)
{
    SUPPORTS_DAC;

    if (enumThis)
    {
        DAC_ENUM_VTHIS();
    }

    Module::EnumMemoryRegions(flags, false);

    m_ClassList.EnumMemoryRegions();

    DPTR(PTR_EnCEEClassData) classData = m_ClassList.Table();
    DPTR(PTR_EnCEEClassData) classLast = classData + m_ClassList.Count();

    while (classData.IsValid() && classData < classLast)
    {
        if ((*classData).IsValid())
        {
            (*classData)->EnumMemoryRegions(flags);
        }

        classData++;
    }
}

#endif // #ifdef DACCESS_COMPILE


// Create a field iterator which includes EnC fields in addition to the fields from an
// underlying ApproxFieldDescIterator.
//
// Arguments:
//   pMT           - MethodTable indicating the type of interest
//   iteratorType  - one of the ApproxFieldDescIterator::IteratorType values specifying which fields
//                   are of interest.
//   fixupEnC      - if true, then any partially-initialized EnC FieldDescs will be fixed up to be complete
//                   initialized FieldDescs as they are returned by Next().  This may load types and do
//                   other things to trigger a GC.
//
EncApproxFieldDescIterator::EncApproxFieldDescIterator(MethodTable *pMT, int iteratorType, BOOL fixupEnC) :
      m_nonEnCIter( pMT, iteratorType )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    m_fixupEnC = fixupEnC;

#ifndef DACCESS_COMPILE
    // can't fixup for EnC on the debugger thread
    _ASSERTE((g_pDebugInterface->GetRCThreadId() != GetCurrentThreadId()) || fixupEnC == FALSE);
#endif

    m_pCurrListElem = NULL;
    m_encClassData = NULL;
    m_encFieldsReturned = 0;

    // If this is an EnC module, then grab a pointer to the EnC data
    if( pMT->GetModule()->IsEditAndContinueEnabled() )
    {
        PTR_EditAndContinueModule encMod = PTR_EditAndContinueModule(pMT->GetModule());
        m_encClassData = encMod->GetEnCEEClassData( pMT, TRUE);
    }
}

// Iterates through all fields, returns NULL when done.
PTR_FieldDesc EncApproxFieldDescIterator::Next()
{
    CONTRACTL
    {
        NOTHROW;
        if (m_fixupEnC) {GC_TRIGGERS;} else {GC_NOTRIGGER;}
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    // If we still have non-EnC fields to look at, return one of them
    if( m_nonEnCIter.CountRemaining() > 0 )
    {
        _ASSERTE( m_encFieldsReturned == 0 );
        return m_nonEnCIter.Next();
    }

    // Get the next EnC field Desc if any
    PTR_EnCFieldDesc pFD = NextEnC();
    if( pFD == NULL )
    {
        // No more fields
        return NULL;
    }

#ifndef DACCESS_COMPILE
    // Fixup the fieldDesc if requested and necessary
    if ( m_fixupEnC && (pFD->NeedsFixup()) )
    {
        // if we get an OOM during fixup, the field will just not get fixed up
        EX_TRY
        {
            FAULT_NOT_FATAL();
            pFD->Fixup(pFD->GetMemberDef());
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    // Either it's been fixed up so we can use it, or we're the Debugger RC thread, we can't fix it up,
    // but it's ok since our logic will check & make sure we don't try and use it.  If haven't asked to
    // have the field fixed up, should never be trying to get at non-fixed up field in
    // this list. Can't simply fixup the field always because loading triggers GC and many
    // code paths can't tolerate that.
    _ASSERTE( !(pFD->NeedsFixup()) ||
              ( g_pDebugInterface->GetRCThreadId() == GetCurrentThreadId() ) );
#endif

    return dac_cast<PTR_FieldDesc>(pFD);
}

// Returns the number of fields plus the number of add EnC fields
int EncApproxFieldDescIterator::Count()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    int count = m_nonEnCIter.Count();

    // If this module doesn't have any EnC data then there aren't any EnC fields
    if (m_encClassData == NULL)
    {
        return count;
    }

    BOOL doInst = ( GetIteratorType() & (int)ApproxFieldDescIterator::INSTANCE_FIELDS);
    BOOL doStatic = ( GetIteratorType() & (int)ApproxFieldDescIterator::STATIC_FIELDS);

    int cNumAddedInst    =  doInst ? m_encClassData->GetAddedInstanceFields() : 0;
    int cNumAddedStatics =  doStatic ? m_encClassData->GetAddedStaticFields() : 0;

    return count + cNumAddedInst + cNumAddedStatics;
}

// Iterate through EnC added fields.
// Returns NULL when done.
PTR_EnCFieldDesc EncApproxFieldDescIterator::NextEnC()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    // If this module doesn't have any EnC data then there aren't any EnC fields
    if( m_encClassData == NULL )
    {
        return NULL;
    }

    BOOL doInst = ( GetIteratorType() & (int)ApproxFieldDescIterator::INSTANCE_FIELDS);
    BOOL doStatic = ( GetIteratorType() & (int)ApproxFieldDescIterator::STATIC_FIELDS);

    int cNumAddedInst    =  doInst ? m_encClassData->GetAddedInstanceFields() : 0;
    int cNumAddedStatics =  doStatic ? m_encClassData->GetAddedStaticFields() : 0;

    // If we haven't returned anything yet
    if ( m_encFieldsReturned == 0 )
    {
        _ASSERTE(m_pCurrListElem == NULL);

        // We're at the start of the instance list.
        if ( doInst )
        {
            m_pCurrListElem = m_encClassData->m_pAddedInstanceFields;
        }
    }

    // If we've finished the instance fields (or never wanted to do any)
    if ( m_encFieldsReturned == cNumAddedInst)
    {
        // We should be at the end of the instance list if doInst is true
        _ASSERTE(m_pCurrListElem == NULL);

        // We're at the start of the statics list.
        if ( doStatic )
        {
            m_pCurrListElem = m_encClassData->m_pAddedStaticFields;
        }
    }

    // If we don't have any elements to return, then we're done
    if (m_pCurrListElem == NULL)
    {
        // Verify that we returned the number we expected to
        _ASSERTE( m_encFieldsReturned == cNumAddedInst + cNumAddedStatics );
        return NULL;
    }

    // Advance the list pointer and return the element
    m_encFieldsReturned++;
    PTR_EnCFieldDesc fd = PTR_EnCFieldDesc(PTR_HOST_MEMBER_TADDR(EnCAddedFieldElement, m_pCurrListElem, m_fieldDesc));
    m_pCurrListElem = m_pCurrListElem->m_next;
    return fd;
}

#endif // EnC_SUPPORTED
