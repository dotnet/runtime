// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CeeFileGenWriterTokens.cpp
//
// This code will walk the byte codes for all methods before they are saved
// to disk and apply the token fix-ups if they have moved.
//
//*****************************************************************************
#include "stdafx.h"
#include "ceegen.h"
#include "../../ildasm/dasmenum.hpp"
#define MAX_CLASSNAME_LENGTH    1024

//********** Locals. **********************************************************
OPCODE DecodeOpcode(const BYTE *pCode, DWORD *pdwLen);



//********** Code. ************************************************************


//*****************************************************************************
// Method bodies are kept in the text section.  The meta data contains the
// RVA of each method body in the image.  The TextSection object contains the
// actual raw bytes where the method bodies are located.  This code will
// determine the raw offset of each method based on the RVA kept in the meta
// data.
//*****************************************************************************

HRESULT CeeFileGenWriter::MapTokens(
    CeeGenTokenMapper *pMapper,
    IMetaDataImport *pImport)
{
    mdTypeDef   td;
    mdMethodDef md;
    ULONG       count;
    ULONG       MethodRVA;
    ULONG       codeOffset;
    ULONG       BaseRVA;
    DWORD       dwFlags;
    DWORD       iFlags;
    HCORENUM    hTypeDefs = 0, hEnum = 0;
    WCHAR       rcwName[MAX_CLASSNAME_LENGTH];
    HRESULT     hr;
    CeeSection  TextSection = getTextSection();

    // Ask for the base RVA of the first method in the stream.  All other
    // method RVA's are >= that value, and will give us the raw offset required.

    hr = getMethodRVA(0, &BaseRVA);
    _ASSERTE(SUCCEEDED(hr));
    // do globals first
    while ((hr = pImport->EnumMethods(&hEnum, mdTokenNil, &md, 1, &count)) == S_OK)
    {
        hr = pImport->GetMethodProps(md, NULL,
                    rcwName, ARRAY_SIZE(rcwName), NULL,
                    &dwFlags, NULL, NULL,
                    &MethodRVA, &iFlags);
        _ASSERTE(SUCCEEDED(hr));

        if (MethodRVA == 0 || ((IsMdAbstract(dwFlags) || IsMiInternalCall(iFlags)) ||
                                   (! IsMiIL(iFlags) && ! IsMiOPTIL(iFlags))))
            continue;

        // The raw offset of the method is the RVA in the image minus
        // the first method in the text section.
        codeOffset = MethodRVA - BaseRVA;
        hr = MapTokensForMethod(pMapper,
                    (BYTE *) TextSection.computePointer(codeOffset),
                    rcwName);
        if (FAILED(hr))
            goto ErrExit;
    }
    if (hEnum) pImport->CloseEnum(hEnum);
    hEnum = 0;

    while ((hr = pImport->EnumTypeDefs(&hTypeDefs, &td, 1, &count)) == S_OK)
    {
        while ((hr = pImport->EnumMethods(&hEnum, td, &md, 1, &count)) == S_OK)
        {
            hr = pImport->GetMethodProps(md, NULL,
                        rcwName, ARRAY_SIZE(rcwName), NULL,
                        &dwFlags, NULL, NULL,
                        &MethodRVA, &iFlags);
            _ASSERTE(SUCCEEDED(hr));

            if (MethodRVA == 0 || ((IsMdAbstract(dwFlags) || IsMiInternalCall(iFlags)) ||
                                   (! IsMiIL(iFlags) && ! IsMiOPTIL(iFlags))))
                continue;


            // The raw offset of the method is the RVA in the image minus
            // the first method in the text section.
            codeOffset = MethodRVA - BaseRVA;
            hr = MapTokensForMethod(pMapper,
                        (BYTE *) TextSection.computePointer(codeOffset),
                        rcwName);
            if (FAILED(hr))
                goto ErrExit;
        }

        if (hEnum) pImport->CloseEnum(hEnum);
        hEnum = 0;
    }

ErrExit:
    if (hTypeDefs) pImport->CloseEnum(hTypeDefs);
    if (hEnum) pImport->CloseEnum(hEnum);
    return (hr);
}


//*****************************************************************************
// This method will walk the byte codes of an IL method looking for tokens.
// For each token found, it will check to see if it has been moved, and if
// so, apply the new token value in its place.
//*****************************************************************************
HRESULT CeeFileGenWriter::MapTokensForMethod(
    CeeGenTokenMapper *pMapper,
    BYTE        *pCode,
    LPCWSTR     szMethodName)
{
    mdToken     tkTo;
    DWORD       PC;

    COR_ILMETHOD_DECODER method((COR_ILMETHOD*) pCode);

    pCode = const_cast<BYTE*>(method.Code);

    PC = 0;
    while (PC < method.GetCodeSize())
    {
        DWORD   Len;
        DWORD   i;
        OPCODE  instr;

        instr = DecodeOpcode(&pCode[PC], &Len);

        if (instr == CEE_COUNT)
        {
            _ASSERTE(0 && "Instruction decoding error\n");
            return E_FAIL;
        }


        PC += Len;

        switch (OpcodeInfo[instr].Type)
        {
            DWORD tk;

            default:
            {
                _ASSERTE(0 && "Unknown instruction\n");
                return E_FAIL;
            }

            case InlineNone:
            break;

            case ShortInlineI:
            case ShortInlineVar:
            case ShortInlineBrTarget:
            PC++;
            break;

            case InlineVar:
            PC += 2;
            break;

            case InlineI:
            case ShortInlineR:
            case InlineBrTarget:
            case InlineRVA:
            PC += 4;
            break;

            case InlineI8:
            case InlineR:
            PC += 8;
            break;

            case InlinePhi:
                {
                    DWORD cases = pCode[PC];
                    PC += 2 * cases + 1;
                    break;
                }

            case InlineSwitch:
            {
                DWORD cases = pCode[PC] + (pCode[PC+1] << 8) + (pCode[PC+2] << 16) + (pCode[PC+3] << 24);

                PC += 4;
                for (i = 0; i < cases; i++)
                {
                    PC += 4;
                }

                // skip bottom of loop which prints szString
                continue;
            }

            case InlineTok:
            case InlineSig:
            case InlineMethod:
            case InlineField:
            case InlineType:
            case InlineString:
            {
                tk = GET_UNALIGNED_VAL32(&pCode[PC]);

                if (pMapper->HasTokenMoved(tk, tkTo))
                {
                    SET_UNALIGNED_VAL32(&pCode[PC], tkTo);
                }

                PC += 4;
                break;
            }
        }
    }

    return S_OK;
}




OPCODE DecodeOpcode(const BYTE *pCode, DWORD *pdwLen)
{
    OPCODE opcode;

    *pdwLen = 1;
    opcode = OPCODE(pCode[0]);
    switch(opcode) {
        case CEE_PREFIX1:
            opcode = OPCODE(pCode[1] + 256);
            if (opcode < 0 || opcode >= CEE_COUNT)
                opcode = CEE_COUNT;
            *pdwLen = 2;
            break;
        case CEE_PREFIXREF:
        case CEE_PREFIX2:
        case CEE_PREFIX3:
        case CEE_PREFIX4:
        case CEE_PREFIX5:
        case CEE_PREFIX6:
        case CEE_PREFIX7:
            *pdwLen = 3;
            return CEE_COUNT;
        default:
            break;
        }
    return opcode;
}
