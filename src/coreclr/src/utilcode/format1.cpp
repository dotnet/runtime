// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***************************************************************************/
/* routines for parsing file format stuff ... */
/* this is split off from format.cpp because this uses meta-data APIs that
   are not present in many builds.  Thus if someone needs things in the format.cpp
   file but does not have the meta-data APIs, I want it to link */

#include "stdafx.h"
#include "cor.h"
#include "corpriv.h"

//---------------------------------------------------------------------------------------
//
static LONG FilterAllExceptions(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    if ((pExceptionPointers->ExceptionRecord->ExceptionCode == EXCEPTION_ACCESS_VIOLATION) ||
        (pExceptionPointers->ExceptionRecord->ExceptionCode == EXCEPTION_ARRAY_BOUNDS_EXCEEDED) ||
        (pExceptionPointers->ExceptionRecord->ExceptionCode == EXCEPTION_IN_PAGE_ERROR))
        return EXCEPTION_EXECUTE_HANDLER;

    return EXCEPTION_CONTINUE_SEARCH;
}

//---------------------------------------------------------------------------------------
//
COR_ILMETHOD_DECODER::COR_ILMETHOD_DECODER(
    COR_ILMETHOD *  header,
    void *          pInternalImport,
    DecoderStatus * wbStatus)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    // Can't put contract because of SEH
    // CONTRACTL
    // {
    //    NOTHROW;
    //    GC_NOTRIGGER;
    //    FORBID_FAULT;
    // }
    // CONTRACTL_END

    bool fErrorInInit = false;
    struct Param
    {
        COR_ILMETHOD_DECODER * pThis;
        COR_ILMETHOD * header;
    } param;
    param.pThis = this;
    param.header = header;

    PAL_TRY(Param *, pParam, &param)
    {
        // Decode the COR header into a more convenient form
        DecoderInit(pParam->pThis, pParam->header);
    }
    PAL_EXCEPT_FILTER(FilterAllExceptions)
    {
        fErrorInInit = true;
        Code = 0;
        SetLocalVarSigTok(0);
        if (wbStatus != NULL)
        {
            *wbStatus = FORMAT_ERROR;
        }
    }
    PAL_ENDTRY

    if (fErrorInInit)
    {
        return;
    }

    // If there is a local variable sig, fetch it into 'LocalVarSig'
    if ((GetLocalVarSigTok() != 0) && (pInternalImport != NULL))
    {
        IMDInternalImport * pMDI = reinterpret_cast<IMDInternalImport *>(pInternalImport);

        if (wbStatus != NULL)
        {
            if ((!pMDI->IsValidToken(GetLocalVarSigTok())) ||
                (TypeFromToken(GetLocalVarSigTok()) != mdtSignature) ||
                (RidFromToken(GetLocalVarSigTok()) == 0))
            {
                *wbStatus = FORMAT_ERROR;         // failure bad local variable signature token
                return;
            }
        }

        if (FAILED(pMDI->GetSigFromToken(GetLocalVarSigTok(), &cbLocalVarSig, &LocalVarSig)))
        {
            // Failure bad local variable signature token
            if (wbStatus != NULL)
            {
                *wbStatus = FORMAT_ERROR;
            }
            LocalVarSig = NULL;
            cbLocalVarSig = 0;
            return;
        }

        if (wbStatus != NULL)
        {
            if (FAILED(validateTokenSig(GetLocalVarSigTok(), LocalVarSig, cbLocalVarSig, 0, pMDI)) ||
                (*LocalVarSig != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG))
            {
                *wbStatus = VERIFICATION_ERROR;   // failure validating local variable signature
                return;
            }
        }
    }

    if (wbStatus != NULL)
    {
        *wbStatus = SUCCESS;
    }
} // COR_ILMETHOD_DECODER::COR_ILMETHOD_DECODER
