// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/****************************************************************************
 **                                                                        **
 ** Corhlprpriv.cpp - signature helpers.                                         **
 **                                                                        **
 ****************************************************************************/
#ifndef SOS_INCLUDE

#ifdef _BLD_CLR
#include "utilcode.h"
#endif
#include "corhlprpriv.h"
#include <stdlib.h>

/*************************************************************************************
*
* implementation of CQuickMemoryBase
*
*************************************************************************************/

template <SIZE_T SIZE, SIZE_T INCREMENT>
HRESULT CQuickMemoryBase<SIZE, INCREMENT>::ReSizeNoThrow(SIZE_T iItems)
{
#ifdef _BLD_CLR
#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    // Exercise heap for OOM-fault injection purposes
    // But we can't do this if current thread suspends EE
    if (!IsSuspendEEThread ())
    {
        BYTE *pTmp = NEW_NOTHROW(iItems);
        if (!pTmp)
        {
            return E_OUTOFMEMORY;
        }
        delete [] pTmp;
    }
#endif
#endif
#endif
    BYTE *pbBuffNew;
    if (iItems <= cbTotal)
    {
        iSize = iItems;
        return NOERROR;
    }

#ifdef _BLD_CLR
#ifndef DACCESS_COMPILE
    // not allowed to do allocation if current thread suspends EE
    if (IsSuspendEEThread ())
        return E_OUTOFMEMORY;
#endif
#endif
    pbBuffNew = NEW_NOTHROW(iItems + INCREMENT);
    if (!pbBuffNew)
        return E_OUTOFMEMORY;
    if (pbBuff)
    {
        memcpy(pbBuffNew, pbBuff, cbTotal);
        delete [] pbBuff;
    }
    else
    {
        _ASSERTE(cbTotal == SIZE);
        memcpy(pbBuffNew, rgData, cbTotal);
    }
    cbTotal = iItems + INCREMENT;
    iSize = iItems;
    pbBuff = pbBuffNew;
    return NOERROR;
}


/*************************************************************************************
*
* get number of bytes consumed by one argument/return type
*
*************************************************************************************/
#define CHECK_REMAINDER  if(cbTotal >= cbTotalMax){hr=E_FAIL; goto ErrExit;}
HRESULT _CountBytesOfOneArg(
    PCCOR_SIGNATURE pbSig,
    ULONG       *pcbTotal)  // Initially, *pcbTotal contains the remaining size of the sig blob
{
    ULONG       cb;
    ULONG       cbTotal=0;
    ULONG       cbTotalMax;
    CorElementType ulElementType;
    ULONG       ulData;
    ULONG       ulTemp;
    int         iData;
    mdToken     tk;
    ULONG       cArg;
    ULONG       callingconv;
    ULONG       cArgsIndex;
    HRESULT     hr = NOERROR;

    if(pcbTotal==NULL) return E_FAIL;
    cbTotalMax = *pcbTotal;

    CHECK_REMAINDER;
    cbTotal = CorSigUncompressElementType(pbSig, &ulElementType);
    while (CorIsModifierElementType((CorElementType) ulElementType))
    {
        CHECK_REMAINDER;
        cbTotal += CorSigUncompressElementType(&pbSig[cbTotal], &ulElementType);
    }
    switch (ulElementType)
    {
        case ELEMENT_TYPE_GENERICINST:
            // skip over generic type
            CHECK_REMAINDER;
            cb = cbTotalMax - cbTotal;
            IfFailGo( _CountBytesOfOneArg(&pbSig[cbTotal], &cb) );
            cbTotal += cb;

            // skip over number of parameters
            CHECK_REMAINDER;
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &cArg);

            // loop through type parameters
            for (cArgsIndex = 0; cArgsIndex < cArg; cArgsIndex++)
            {
                CHECK_REMAINDER;
                cb = cbTotalMax - cbTotal;
                IfFailGo( _CountBytesOfOneArg(&pbSig[cbTotal], &cb) );
                cbTotal += cb;
            }
            break;

        case ELEMENT_TYPE_SZARRAY:
        case 0x1e /* obsolete */:
            // skip over base type
            CHECK_REMAINDER;
            cb = cbTotalMax - cbTotal;
            IfFailGo( _CountBytesOfOneArg(&pbSig[cbTotal], &cb) );
            cbTotal += cb;
            break;

        case ELEMENT_TYPE_FNPTR:
            CHECK_REMAINDER;
            cbTotal += CorSigUncompressData (&pbSig[cbTotal], &callingconv);

            // remember number of bytes to represent the arg counts
            CHECK_REMAINDER;
            cbTotal += CorSigUncompressData (&pbSig[cbTotal], &cArg);

            // how many bytes to represent the return type
            CHECK_REMAINDER;
            cb = cbTotalMax - cbTotal;
            IfFailGo( _CountBytesOfOneArg( &pbSig[cbTotal], &cb) );
            cbTotal += cb;

            // loop through argument
            for (cArgsIndex = 0; cArgsIndex < cArg; cArgsIndex++)
            {
                CHECK_REMAINDER;
                cb = cbTotalMax - cbTotal;
                IfFailGo( _CountBytesOfOneArg( &pbSig[cbTotal], &cb) );
                cbTotal += cb;
            }

            break;

        case ELEMENT_TYPE_ARRAY:
            // syntax : ARRAY BaseType <rank> [i size_1... size_i] [j lowerbound_1 ... lowerbound_j]

            // skip over base type
            CHECK_REMAINDER;
            cb = cbTotalMax - cbTotal;
            IfFailGo( _CountBytesOfOneArg(&pbSig[cbTotal], &cb) );
            cbTotal += cb;

            // Parse for the rank
            CHECK_REMAINDER;
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulData);

            // if rank == 0, we are done
            if (ulData == 0)
                break;

            // any size of dimension specified?
            CHECK_REMAINDER;
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulData);
            while (ulData--)
            {
                CHECK_REMAINDER;
                cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulTemp);
            }

            // any lower bound specified?
            CHECK_REMAINDER;
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulData);

            while (ulData--)
            {
                CHECK_REMAINDER;
                cbTotal += CorSigUncompressSignedInt(&pbSig[cbTotal], &iData);
            }

            break;
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
            // count the bytes for the token compression
            CHECK_REMAINDER;
            cbTotal += CorSigUncompressToken(&pbSig[cbTotal], &tk);
            if ( ulElementType == ELEMENT_TYPE_CMOD_REQD ||
                 ulElementType == ELEMENT_TYPE_CMOD_OPT)
            {
                // skip over base type
                CHECK_REMAINDER;
                cb = cbTotalMax - cbTotal;
                IfFailGo( _CountBytesOfOneArg(&pbSig[cbTotal], &cb) );
                cbTotal += cb;
            }
            break;
        default:
            break;
    }

    *pcbTotal = cbTotal;
ErrExit:
    return hr;
}
#undef CHECK_REMAINDER

//*****************************************************************************
// copy fixed part of VarArg signature to a buffer
//*****************************************************************************
HRESULT _GetFixedSigOfVarArg(           // S_OK or error.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob of COM+ method signature
    ULONG   cbSigBlob,                  // [IN] size of signature
    CQuickBytes *pqbSig,                // [OUT] output buffer for fixed part of VarArg Signature
    ULONG   *pcbSigBlob)                // [OUT] number of bytes written to the above output buffer
{
    HRESULT     hr = NOERROR;
    ULONG       cbCalling;
    ULONG       cbTyArgsNumber = 0;     // number of bytes to store the type arg count (generics only)
    ULONG       cbArgsNumber;           // number of bytes to store the original arg count
    ULONG       cbArgsNumberTemp;       // number of bytes to store the fixed arg count
    ULONG       cbTotal = 0;            // total of number bytes for return type + all fixed arguments
    ULONG       cbCur = 0;              // index through the pvSigBlob
    ULONG       cb;
    ULONG       cArg;
    ULONG       cTyArg;
    ULONG       callingconv;
    ULONG       cArgsIndex;
    CorElementType ulElementType;
    BYTE        *pbSig;

    _ASSERTE (pvSigBlob && pcbSigBlob);

    // remember the number of bytes to represent the calling convention
    cbCalling = CorSigUncompressData (pvSigBlob, &callingconv);
    if (cbCalling == ((ULONG)(-1)))
    {
        return E_INVALIDARG;
    }
    _ASSERTE (isCallConv(callingconv, IMAGE_CEE_CS_CALLCONV_VARARG));
    cbCur += cbCalling;

    if (callingconv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        cbTyArgsNumber = CorSigUncompressData(&pvSigBlob[cbCur], &cTyArg);
        if (cbTyArgsNumber == ((ULONG)(-1)))
        {
            return E_INVALIDARG;
        }
        cbCur += cbTyArgsNumber;
    }

    // remember number of bytes to represent the arg counts
    cbArgsNumber= CorSigUncompressData (&pvSigBlob[cbCur], &cArg);
    if (cbArgsNumber == ((ULONG)(-1)))
    {
        return E_INVALIDARG;
    }

    cbCur += cbArgsNumber;

    // how many bytes to represent the return type
    cb = cbSigBlob-cbCur;
    IfFailGo( _CountBytesOfOneArg( &pvSigBlob[cbCur], &cb) );
    cbCur += cb;
    cbTotal += cb;

    // loop through argument until we found ELEMENT_TYPE_SENTINEL or run
    // out of arguments
    for (cArgsIndex = 0; cArgsIndex < cArg; cArgsIndex++)
    {
        _ASSERTE(cbCur < cbSigBlob);

        // peak the outer most ELEMENT_TYPE_*
        CorSigUncompressElementType (&pvSigBlob[cbCur], &ulElementType);
        if (ulElementType == ELEMENT_TYPE_SENTINEL)
            break;
        cb = cbSigBlob-cbCur;
        IfFailGo( _CountBytesOfOneArg( &pvSigBlob[cbCur], &cb) );
        cbTotal += cb;
        cbCur += cb;
    }

    cbArgsNumberTemp = CorSigCompressData(cArgsIndex, &cArg);

    // now cbCalling : the number of bytes needed to store the calling convention
    // cbArgNumberTemp : number of bytes to store the fixed arg count
    // cbTotal : the number of bytes to store the ret and fixed arguments

    *pcbSigBlob = cbCalling + cbArgsNumberTemp + cbTotal;

    // resize the buffer
    IfFailGo( pqbSig->ReSizeNoThrow(*pcbSigBlob) );
    pbSig = (BYTE *)pqbSig->Ptr();

    // copy over the calling convention
    cb = CorSigCompressData(callingconv, pbSig);

    // copy over the fixed arg count
    cbArgsNumberTemp = CorSigCompressData(cArgsIndex, &pbSig[cb]);

    // copy over the fixed args + ret type
    memcpy(&pbSig[cb + cbArgsNumberTemp], &pvSigBlob[cbCalling + cbArgsNumber], cbTotal);

ErrExit:
    return hr;
}


#endif // !SOS_INCLUDE
