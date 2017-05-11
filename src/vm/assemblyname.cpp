// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  AssemblyName.cpp
**
** Purpose: Implements AssemblyName (loader domain) architecture
**
**


**
===========================================================*/

#include "common.h"

#include <stdlib.h>
#include <shlwapi.h>

#include "assemblyname.hpp"
#include "security.h"
#include "field.h"
#include "strongname.h"
#include "eeconfig.h"

#ifndef URL_ESCAPE_AS_UTF8
#define URL_ESCAPE_AS_UTF8              0x00040000  // Percent-encode all non-ASCII characters as their UTF-8 equivalents.
#endif 

FCIMPL1(Object*, AssemblyNameNative::GetFileInformation, StringObject* filenameUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        ASSEMBLYNAMEREF result;
        STRINGREF       filename;
    } gc;

    gc.result   = NULL;
    gc.filename = (STRINGREF) filenameUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    if (gc.filename == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_FileName"));

    if (gc.filename->GetStringLength() == 0)
        COMPlusThrow(kArgumentException, W("Argument_EmptyFileName"));

    gc.result = (ASSEMBLYNAMEREF) AllocateObject(MscorlibBinder::GetClass(CLASS__ASSEMBLY_NAME));


    ///////////////////////////////////////////////
    SString sFileName(gc.filename->GetBuffer());
    PEImageHolder pImage = PEImage::OpenImage(sFileName, MDInternalImport_NoCache);

    // Allow AssemblyLoadContext.GetAssemblyName for native images on CoreCLR
    if (pImage->HasNTHeaders() && pImage->HasCorHeader() && pImage->HasNativeHeader())
        pImage->VerifyIsNIAssembly();
    else
        pImage->VerifyIsAssembly();

    SString sUrl = sFileName;
    PEAssembly::PathToUrl(sUrl);

    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(mdtAssembly,1),pImage->GetMDImport(),NULL,TRUE);
    spec.AssemblyNameInit(&gc.result, pImage);
    
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.result);
}
FCIMPLEND

FCIMPL1(Object*, AssemblyNameNative::ToString, Object* refThisUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF pObj          = NULL;
    ASSEMBLYNAMEREF pThis   = (ASSEMBLYNAMEREF) (OBJECTREF) refThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(pThis);

    if (pThis == NULL)
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));

    Thread *pThread = GetThread();

    CheckPointHolder cph(pThread->m_MarshalAlloc.GetCheckpoint()); //hold checkpoint for autorelease

    AssemblySpec spec;
    spec.InitializeSpec(&(pThread->m_MarshalAlloc), (ASSEMBLYNAMEREF*) &pThis, FALSE, FALSE); 

    StackSString name;
    spec.GetFileOrDisplayName(ASM_DISPLAYF_VERSION |
                              ASM_DISPLAYF_CULTURE |
                              ASM_DISPLAYF_PUBLIC_KEY_TOKEN,
                              name);

    pObj = (OBJECTREF) StringObject::NewString(name);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(pObj);
}
FCIMPLEND


FCIMPL1(Object*, AssemblyNameNative::GetPublicKeyToken, Object* refThisUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF orOutputArray = NULL;
    OBJECTREF refThis       = (OBJECTREF) refThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    if (refThis == NULL)
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));

    ASSEMBLYNAMEREF orThis = (ASSEMBLYNAMEREF)refThis;
    U1ARRAYREF orPublicKey = orThis->GetPublicKey();

    if (orPublicKey != NULL) {
        DWORD cb = orPublicKey->GetNumComponents();
        StrongNameBufferHolder<BYTE> pbToken;

        if (cb) {    
            CQuickBytes qb;
            BYTE *pbKey = (BYTE*) qb.AllocThrows(cb);
            memcpy(pbKey, orPublicKey->GetDataPtr(), cb);

            {
                GCX_PREEMP();
                if (!StrongNameTokenFromPublicKey(pbKey, cb, &pbToken, &cb))
                    COMPlusThrowHR(StrongNameErrorInfo());
            }
        }

        Security::CopyEncodingToByteArray(pbToken, cb, &orOutputArray);
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(orOutputArray);
}
FCIMPLEND


FCIMPL4(void, AssemblyNameNative::Init, Object * refThisUNSAFE, OBJECTREF * pAssemblyRef, CLR_BOOL fForIntrospection, CLR_BOOL fRaiseResolveEvent)
{
    FCALL_CONTRACT;

    ASSEMBLYNAMEREF pThis = (ASSEMBLYNAMEREF) (OBJECTREF) refThisUNSAFE;
    HRESULT hr = S_OK;
    
    HELPER_METHOD_FRAME_BEGIN_1(pThis);
    
    *pAssemblyRef = NULL;

    if (pThis == NULL)
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));

    Thread * pThread = GetThread();

    CheckPointHolder cph(pThread->m_MarshalAlloc.GetCheckpoint()); //hold checkpoint for autorelease

    AssemblySpec spec;
    hr = spec.InitializeSpec(&(pThread->m_MarshalAlloc), (ASSEMBLYNAMEREF *) &pThis, TRUE, FALSE); 

    if (SUCCEEDED(hr))
    {
        spec.AssemblyNameInit(&pThis,NULL);
    }
    else if ((hr == FUSION_E_INVALID_NAME) && fRaiseResolveEvent)
    {
        Assembly * pAssembly = GetAppDomain()->RaiseAssemblyResolveEvent(&spec, fForIntrospection, FALSE);

        if (pAssembly == NULL)
        {
            EEFileLoadException::Throw(&spec, hr);
        }
        else
        {
            *((OBJECTREF *) (&(*pAssemblyRef))) = pAssembly->GetExposedObject();
        }
    }
    else
    {
        ThrowHR(hr);
    }
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


