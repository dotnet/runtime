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

    // Load the temporary image using a flat layout, instead of
    // waiting for it to happen during HasNTHeaders. This allows us to
    // get the assembly name for images that contain native code for a
    // non-native platform.
    PEImageLayoutHolder pLayout(pImage->GetLayout(PEImageLayout::LAYOUT_FLAT, PEImage::LAYOUT_CREATEIFNEEDED));

    // Allow AssemblyLoadContext.GetAssemblyName for native images on CoreCLR
    if (pImage->HasNTHeaders() && pImage->HasCorHeader() && pImage->HasNativeHeader())
        pImage->VerifyIsNIAssembly();
    else
        pImage->VerifyIsAssembly();

    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(mdtAssembly,1),pImage->GetMDImport(),NULL);
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

    ACQUIRE_STACKING_ALLOCATOR(pStackingAllocator);

    AssemblySpec spec;
    spec.InitializeSpec(pStackingAllocator, (ASSEMBLYNAMEREF*) &pThis, FALSE); 

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

    U1ARRAYREF orOutputArray = NULL;
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

        orOutputArray = (U1ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_U1, cb);
        memcpyNoGCRefs(orOutputArray->m_Array, pbToken, cb);
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(orOutputArray);
}
FCIMPLEND


FCIMPL1(void, AssemblyNameNative::Init, Object * refThisUNSAFE)
{
    FCALL_CONTRACT;

    ASSEMBLYNAMEREF pThis = (ASSEMBLYNAMEREF) (OBJECTREF) refThisUNSAFE;
    HRESULT hr = S_OK;
    
    HELPER_METHOD_FRAME_BEGIN_1(pThis);
    
    if (pThis == NULL)
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));

    ACQUIRE_STACKING_ALLOCATOR(pStackingAllocator);

    AssemblySpec spec;
    hr = spec.InitializeSpec(pStackingAllocator, (ASSEMBLYNAMEREF *) &pThis, TRUE); 

    if (SUCCEEDED(hr))
    {
        spec.AssemblyNameInit(&pThis,NULL);
    }
    else
    {
        ThrowHR(hr);
    }
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


