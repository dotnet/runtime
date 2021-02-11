// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ildasmpch.h"

#include <metadata.h>
#include <utilcode.h>
#include "debugmacros.h"
#include "dasm_sz.h"
#include "ceeload.h"

extern PELoader *g_pPELoader;

unsigned SizeOfValueType(mdToken tk, IMDInternalImport* pImport)
{
    unsigned ret = 0xFFFFFFFF;
    if((TypeFromToken(tk)==mdtTypeDef)&&RidFromToken(tk)&&pImport)
    {
        DWORD dwAttrs;
        mdToken tkExtends;
        if (FAILED(pImport->GetTypeDefProps(tk, &dwAttrs, &tkExtends)))
        {
            return ret;
        }
        if(!(IsTdInterface(dwAttrs)||IsTdAbstract(dwAttrs)||IsTdImport(dwAttrs)))
        {
            mdToken tkField;
            DWORD dwFieldAttrs;
            unsigned uFieldSize;
            ULONG ulPack=0,
                  ulSize = 0,
                  ulInstFieldSize = 0;

            if (FAILED(pImport->GetClassPackSize(tk,&ulPack))) ulPack = 0;
            if (FAILED(pImport->GetClassTotalSize(tk,&ulSize))) ulSize = 0;

            if (IsTdExplicitLayout(dwAttrs))
            {
                MD_CLASS_LAYOUT hLayout;
                if (SUCCEEDED(pImport->GetClassLayoutInit(tk,&hLayout)))
                {
                    ULONG ulOffset;
                    while (SUCCEEDED(pImport->GetClassLayoutNext(&hLayout,&tkField,&ulOffset)) && RidFromToken(tkField))
                    {
                        if (FAILED(pImport->GetFieldDefProps(tkField, &dwFieldAttrs)))
                        {
                            break;
                        }
                        if (!(IsFdStatic(dwFieldAttrs)||IsFdLiteral(dwFieldAttrs)))
                        {
                            uFieldSize = SizeOfField(tkField,pImport);
                            if (uFieldSize == 0xFFFFFFFF) return uFieldSize;
                            uFieldSize += ulOffset;
                            if (uFieldSize > ulInstFieldSize) ulInstFieldSize = uFieldSize;
                        }
                    }
                }
            }
            else
            {
                HENUMInternal hEnumField;
                unsigned      cFieldsMax = 0;
                if (SUCCEEDED(pImport->EnumInit(mdtFieldDef, tk, &hEnumField)))
                {
                    if ((cFieldsMax = pImport->EnumGetCount(&hEnumField)) != 0)
                    {
                        while (pImport->EnumNext(&hEnumField, &tkField) && RidFromToken(tkField))
                        {
                            if (FAILED(pImport->GetFieldDefProps(tkField, &dwFieldAttrs)))
                            {
                                break;
                            }
                            if (!IsFdStatic(dwFieldAttrs) && !IsFdLiteral(dwFieldAttrs))
                            {
                                uFieldSize = SizeOfField(tkField,pImport);
                                if (uFieldSize == 0xFFFFFFFF) return uFieldSize;
                                if (ulPack > 1)
                                {
                                    ULONG ulDelta = ulInstFieldSize % ulPack;
                                    if (ulDelta != 0) ulInstFieldSize += ulPack - ulDelta;
                                }
                                ulInstFieldSize += uFieldSize;
                            }
                        }
                    }
                    pImport->EnumClose(&hEnumField);
                }
            }
            ret = (ulInstFieldSize > ulSize) ? ulInstFieldSize : ulSize;
            if(ret == 0) ret = 1; // zero-sized value types automatically get 1 byte
        }
    }
    return ret;
}

unsigned SizeOfField(mdToken tk, IMDInternalImport* pImport)
{
    unsigned ret = 0xFFFFFFFF;
    if((TypeFromToken(tk) == mdtFieldDef) && RidFromToken(tk) && pImport)
    {
        PCCOR_SIGNATURE pSig;
        ULONG           cSig;
        if (FAILED(pImport->GetSigOfFieldDef(tk, &cSig, &pSig)))
        {
            return ret;
        }
        ret = SizeOfField(&pSig,cSig,pImport);
    }
    return ret;
}

unsigned SizeOfField(PCCOR_SIGNATURE *ppSig, ULONG cSig, IMDInternalImport* pImport)
{
    unsigned ret = 0xFFFFFFFF;
    if(ppSig && *ppSig && cSig && pImport)
    {
        unsigned callConv = CorSigUncompressData(*ppSig);
        if (isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_FIELD))
        {
            mdToken  tk;
            int typ;
            BOOL Reiterate;
            unsigned uElementNumber = 1;
            PCCOR_SIGNATURE pSigStart = *ppSig;
            PCCOR_SIGNATURE pSigEnd = *ppSig+cSig;

            // Size of the pointer depends on bitness of the assembly
            unsigned nSizeOfPointer = g_pPELoader->IsPE32() ? 4 : 8;

            do {
                Reiterate = FALSE;
                switch(typ = *(*ppSig)++) {
                    case ELEMENT_TYPE_VOID          :
                        return 0;

                    case ELEMENT_TYPE_I1            :
                    case ELEMENT_TYPE_U1            :
                    case ELEMENT_TYPE_BOOLEAN       :
                        return uElementNumber;

                    case ELEMENT_TYPE_CHAR          :
                    case ELEMENT_TYPE_I2            :
                    case ELEMENT_TYPE_U2            :
                        return (uElementNumber << 1);

                    case ELEMENT_TYPE_I4            :
                    case ELEMENT_TYPE_U4            :
                    case ELEMENT_TYPE_R4            :
                        return (uElementNumber << 2);

                    case ELEMENT_TYPE_I8            :
                    case ELEMENT_TYPE_U8            :
                    case ELEMENT_TYPE_R8            :
                        return (uElementNumber << 3);

                    case ELEMENT_TYPE_OBJECT        :
                    case ELEMENT_TYPE_STRING        :
                    case ELEMENT_TYPE_FNPTR :
                    case ELEMENT_TYPE_CLASS         :
                    case ELEMENT_TYPE_PTR           :
                    case ELEMENT_TYPE_BYREF         :
                    //case ELEMENT_TYPE_VAR        :
                    case ELEMENT_TYPE_U             :
                    case ELEMENT_TYPE_I             :
                        return (uElementNumber * nSizeOfPointer);

                    case ELEMENT_TYPE_TYPEDBYREF        :   // pair of ptrs
                        return (uElementNumber * nSizeOfPointer * 2);

                    case ELEMENT_TYPE_VALUETYPE    :
                        *ppSig += CorSigUncompressToken(*ppSig, &tk);
                        ret = SizeOfValueType(tk,pImport);
                        if(ret != 0xFFFFFFFF) ret *= uElementNumber;
                        return ret;

                        // Modifiers or depedant types

                    case ELEMENT_TYPE_ARRAY       :
                        ret = SizeOfField(ppSig, cSig-(unsigned)((*ppSig)-pSigStart), pImport);
                        if(ret != 0xFFFFFFFF)
                        {
                            unsigned rank = CorSigUncompressData(*ppSig);
                            if (rank == 0) ret = 0xFFFFFFFF;
                            else
                            {
                                int* lowerBounds = new (nothrow) int[2*rank];
                                int* sizes       = &lowerBounds[rank];
                                memset(lowerBounds, 0, sizeof(int)*2*rank);

                                unsigned numSizes = CorSigUncompressData(*ppSig);
                                _ASSERTE(numSizes <= rank);
                                unsigned i;
                                for(i =0; i < numSizes; i++)
                                    sizes[i] = CorSigUncompressData(*ppSig);

                                unsigned numLowBounds = CorSigUncompressData(*ppSig);
                                _ASSERTE(numLowBounds <= rank);
                                for(i = 0; i < numLowBounds; i++)
                                    *ppSig+=CorSigUncompressSignedInt(*ppSig,&lowerBounds[i]);

                                for(i = 0; i < numSizes; i++)
                                {
                                    if (sizes[i]) uElementNumber *= sizes[i];
                                }
                                ret *= uElementNumber;
                                delete[] lowerBounds;
                            }
                        }
                        return ret;

                    case ELEMENT_TYPE_CMOD_OPT  :
                    case ELEMENT_TYPE_CMOD_REQD :
                        *ppSig += CorSigUncompressToken(*ppSig, &tk);
                        FALLTHROUGH;
                    case ELEMENT_TYPE_PINNED    :
                    case ELEMENT_TYPE_SZARRAY    : // uElementNumber doesn't change
                        if(*ppSig < pSigEnd) Reiterate = TRUE;
                        break;

                    default:
                    case ELEMENT_TYPE_SENTINEL      :
                    case ELEMENT_TYPE_END           :
                        break;
                } // end switch
            } while(Reiterate);
        } // end if(CALLCONV_FIELD)
    } // end if(signature && import)
    return ret;
}
