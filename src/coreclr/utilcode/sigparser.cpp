// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// sigparser.cpp
//

//
// Signature parsing code
//
#include "stdafx.h"
#include "sigparser.h"
#include "contract.h"

HRESULT SigParser::SkipExactlyOne()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    CorElementType typ;
    HRESULT hr = GetElemType(&typ);

    IfFailRet(hr);

    if (!CorIsPrimitiveType(typ))
    {
        switch ((DWORD)typ)
        {
            default:
                // _ASSERT(!"Illegal or unimplement type in COM+ sig.");
                return META_E_BAD_SIGNATURE;
                break;
            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_MVAR:
                IfFailRet(GetData(NULL));      // Skip variable number
                break;
            case ELEMENT_TYPE_VAR_ZAPSIG:
                IfFailRet(GetData(NULL));      // Skip RID
                break;
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_TYPEDBYREF:
            case ELEMENT_TYPE_CANON_ZAPSIG:
                break;

            case ELEMENT_TYPE_BYREF: //fallthru
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_PINNED:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
                IfFailRet(SkipExactlyOne());              // Skip referenced type
                break;

            case ELEMENT_TYPE_VALUETYPE: //fallthru
            case ELEMENT_TYPE_CLASS:
                IfFailRet(GetToken(NULL));          // Skip RID
                break;

            case ELEMENT_TYPE_MODULE_ZAPSIG:
                IfFailRet(GetData(NULL));      // Skip index
                IfFailRet(SkipExactlyOne());   // Skip type
                break;

            case ELEMENT_TYPE_FNPTR:
                IfFailRet(SkipSignature());
                break;

            case ELEMENT_TYPE_ARRAY:
                {
                    IfFailRet(SkipExactlyOne());     // Skip element type
                    uint32_t rank;
                    IfFailRet(GetData(&rank));    // Get rank
                    if (rank)
                    {
                        uint32_t nsizes;
                        IfFailRet(GetData(&nsizes)); // Get # of sizes
                        while (nsizes--)
                        {
                            IfFailRet(GetData(NULL));           // Skip size
                        }

                        uint32_t nlbounds;
                        IfFailRet(GetData(&nlbounds)); // Get # of lower bounds
                        while (nlbounds--)
                        {
                            IfFailRet(GetData(NULL));           // Skip lower bounds
                        }
                    }

                }
                break;

            case ELEMENT_TYPE_SENTINEL:
                // Should be unreachable since GetElem strips it
                break;

            case ELEMENT_TYPE_INTERNAL:
                IfFailRet(GetPointer(NULL));
                break;

            case ELEMENT_TYPE_GENERICINST:
              IfFailRet(SkipExactlyOne());          // Skip generic type
              uint32_t argCnt;
              IfFailRet(GetData(&argCnt)); // Get number of parameters
              while (argCnt--)
              {
                IfFailRet(SkipExactlyOne());        // Skip the parameters
              }
              break;

        }
    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Skip only a method header signature - not the sigs of the args to the method.
//
HRESULT
SigParser::SkipMethodHeaderSignature(
    uint32_t * pcArgs)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    HRESULT hr = S_OK;

    // Skip calling convention
    uint32_t uCallConv;
    IfFailRet(GetCallingConvInfo(&uCallConv));

    if ((uCallConv == IMAGE_CEE_CS_CALLCONV_FIELD) ||
        (uCallConv == IMAGE_CEE_CS_CALLCONV_LOCAL_SIG))
    {
        return META_E_BAD_SIGNATURE;
    }

    // Skip type parameter count
    if (uCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        IfFailRet(GetData(NULL));

    // Get arg count;
    IfFailRet(GetData(pcArgs));

    // Skip return type;
    IfFailRet(SkipExactlyOne());

    return hr;
} // SigParser::SkipMethodHeaderSignature

//---------------------------------------------------------------------------------------
//
// Skip a sub signature (as immediately follows an ELEMENT_TYPE_FNPTR).
HRESULT SigParser::SkipSignature()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    HRESULT hr = S_OK;

    uint32_t cArgs;

    IfFailRet(SkipMethodHeaderSignature(&cArgs));

    // Skip args.
    while (cArgs) {
        IfFailRet(SkipExactlyOne());
        cArgs--;
    }

    return hr;
}
