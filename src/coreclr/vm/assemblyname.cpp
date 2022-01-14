// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "strongnameholders.h"
#include "strongnameinternal.h"
#include "eeconfig.h"

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

    gc.result = (ASSEMBLYNAMEREF) AllocateObject(CoreLibBinder::GetClass(CLASS__ASSEMBLY_NAME));


    ///////////////////////////////////////////////
    SString sFileName(gc.filename->GetBuffer());
    PEImageHolder pImage = PEImage::OpenImage(sFileName, MDInternalImport_NoCache);

    // Ask for FLAT. We will only look at metadata and release the image shortly.
    // Besides we may be getting the assembly name for images that contain native code for a
    // non-native platform and would end up using flat anyways.
    PEImageLayout* pLayout = pImage->GetOrCreateLayout(PEImageLayout::LAYOUT_FLAT);

    pImage->VerifyIsAssembly();

    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(1,mdtAssembly),pImage->GetMDImport(),NULL);
    spec.AssemblyNameInit(&gc.result, pImage);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.result);
}
FCIMPLEND
