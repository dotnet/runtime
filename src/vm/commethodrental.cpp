// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////////



#include "common.h"
#include "commethodrental.h"
#include "corerror.h"

#ifdef FEATURE_METHOD_RENTAL
// SwapMethodBody
// This method will take the rgMethod as the new function body for a given method. 
//

void QCALLTYPE COMMethodRental::SwapMethodBody(EnregisteredTypeHandle cls, INT32 tkMethod, LPVOID rgMethod, INT32 iSize, INT32 flags, QCall::StackCrawlMarkHandle stackMark)
{
    QCALL_CONTRACT;
    
    BEGIN_QCALL;

    BYTE        *pNewCode       = NULL;
    MethodDesc  *pMethodDesc;
    ReflectionModule *module;
    ICeeGen*    pGen;
    ULONG       methodRVA;
    HRESULT     hr;

    if ( cls == NULL)
    {
        COMPlusThrowArgumentNull(W("cls"));
    }

    MethodTable *pMethodTable = TypeHandle::FromPtr(cls).GetMethodTable();
    PREFIX_ASSUME(pMethodTable != NULL);
    module = (ReflectionModule *) pMethodTable->GetModule();
    pGen = module->GetCeeGen();

    Assembly* caller = SystemDomain::GetCallersAssembly( stackMark );

    _ASSERTE( caller != NULL && "Unable to get calling assembly" );
    _ASSERTE( module->GetCreatingAssembly() != NULL && "ReflectionModule must have a creating assembly to be used with method rental" );

    if (module->GetCreatingAssembly() != caller)
    {
        COMPlusThrow(kSecurityException);
    }

    // Find the methoddesc given the method token
    pMethodDesc = MemberLoader::FindMethod(pMethodTable, tkMethod);
    if (pMethodDesc == NULL)
    {
        COMPlusThrowArgumentException(W("methodtoken"), NULL);
    }
    if (pMethodDesc->GetMethodTable() != pMethodTable || pMethodDesc->GetNumGenericClassArgs() != 0 || pMethodDesc->GetNumGenericMethodArgs() != 0)
    {
        COMPlusThrowArgumentException(W("methodtoken"), W("Argument_TypeDoesNotContainMethod"));
    }
    hr = pGen->AllocateMethodBuffer(iSize, &pNewCode, &methodRVA);    
    if (FAILED(hr))
        COMPlusThrowHR(hr);

    if (pNewCode == NULL)
    {
        COMPlusThrowOM();
    }

    // <TODO>
    // if method desc is pointing to the post-jitted native code block,
    // we want to recycle this code block

    // @todo: SEH handling. Will we need to support a method that can throw exception
    // If not, add an assertion to make sure that there is no SEH contains in the method header.

    // @todo: figure out a way not to copy the code block.

    // @todo: add link time security check. This function can be executed only if fully trusted.</TODO>

    // copy the new function body to the buffer
    memcpy(pNewCode, (void *) rgMethod, iSize);

    // add the starting address of the il blob to the il blob hash table
    // we need to find this information from out of process for debugger inspection
    // APIs so we have to store this information where we can get it later
    module->SetDynamicIL(mdToken(tkMethod), TADDR(pNewCode), FALSE);

    // Reset the methoddesc back to unjited state
    pMethodDesc->Reset();

    if (flags)
    {
        // JITImmediate
#if _DEBUG
        COR_ILMETHOD* ilHeader = pMethodDesc->GetILHeader(TRUE);
        _ASSERTE(((BYTE *)ilHeader) == pNewCode);
#endif
        COR_ILMETHOD_DECODER header((COR_ILMETHOD *)pNewCode, pMethodDesc->GetMDImport(), NULL); 

        // minimum validation on the correctness of method header
        if (header.GetCode() == NULL)
            COMPlusThrowHR(VLDTR_E_MD_BADHEADER);

#ifdef FEATURE_INTERPRETER
        pMethodDesc->MakeJitWorker(&header, CORJIT_FLG_MAKEFINALCODE, 0);
#else // !FEATURE_INTERPRETER
        pMethodDesc->MakeJitWorker(&header, 0, 0);
#endif // !FEATURE_INTERPRETER
    }

    // add feature::
    // If SQL is generating class with inheritance hierarchy, we may need to
    // check the whole vtable to find duplicate entries.

    END_QCALL;

}   // COMMethodRental::SwapMethodBody


#endif // FEATURE_METHOD_RENTAL
