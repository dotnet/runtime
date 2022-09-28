// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// This code supports formatting a method and it's signature in a friendly
// and consistent format.
//
//*****************************************************************************
#include "stdafx.h"
#include "prettyprintsig.h"
#include "utilcode.h"
#include "metadata.h"
#include "corpriv.h"

/***********************************************************************/
// Null-terminates the string held in "out"

static WCHAR* asStringW(CQuickBytes *out)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END

    SIZE_T oldSize = out->Size();
    if (FAILED(out->ReSizeNoThrow(oldSize + 1)))
        return 0;
    WCHAR * cur = (WCHAR *) ((BYTE *) out->Ptr() + oldSize);
    *cur = 0;
    return((WCHAR*) out->Ptr());
} // static WCHAR* asStringW()

// Null-terminates the string held in "out"

static CHAR* asStringA(CQuickBytes *out)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END

    SIZE_T oldSize = out->Size();
    if (FAILED(out->ReSizeNoThrow(oldSize + 1)))
        return 0;
    CHAR * cur = (CHAR *) ((BYTE *) out->Ptr() + oldSize);
    *cur = 0;
    return((CHAR*) out->Ptr());
} // static CHAR* asStringA()

/***********************************************************************/
// Appends the str to "out"
// The string held in "out" is not NULL-terminated. asStringW() needs to
// be called for the NULL-termination

static HRESULT appendStrW(CQuickBytes *out, const WCHAR* str)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    SIZE_T len = wcslen(str) * sizeof(WCHAR);
    SIZE_T oldSize = out->Size();
    if (FAILED(out->ReSizeNoThrow(oldSize + len)))
        return E_OUTOFMEMORY;
    WCHAR * cur = (WCHAR *) ((BYTE *) out->Ptr() + oldSize);
    memcpy(cur, str, len);
    // Note no trailing null!
    return S_OK;
} // static HRESULT appendStrW()

// Appends the str to "out"
// The string held in "out" is not NULL-terminated. asStringA() needs to
// be called for the NULL-termination

static HRESULT appendStrA(CQuickBytes *out, const CHAR* str)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    SIZE_T len = strlen(str) * sizeof(CHAR);
    SIZE_T oldSize = out->Size();
    if (FAILED(out->ReSizeNoThrow(oldSize + len)))
        return E_OUTOFMEMORY;
    CHAR * cur = (CHAR *) ((BYTE *) out->Ptr() + oldSize);
    memcpy(cur, str, len);
    // Note no trailing null!
    return S_OK;
} // static HRESULT appendStrA()


static HRESULT appendStrNumW(CQuickBytes *out, int num)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    WCHAR buff[MaxSigned32BitDecString + 1];
    FormatInteger(buff, ARRAY_SIZE(buff), "%d", num);
    return appendStrW(out, buff);
} // static HRESULT appendStrNumW()

static HRESULT appendStrNumA(CQuickBytes *out, int num)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    CHAR buff[32];
    sprintf_s(buff, 32, "%d", num);
    return appendStrA(out, buff);
} // static HRESULT appendStrNumA()

static HRESULT appendStrHexW(CQuickBytes *out, int num)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    WCHAR buff[Max32BitHexString + 1];
    FormatInteger(buff, ARRAY_SIZE(buff), "%08X", num);
    return appendStrW(out, buff);
} // static HRESULT appendStrHexW()

static HRESULT appendStrHexA(CQuickBytes *out, int num)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    CHAR buff[32];
    sprintf_s(buff, 32, "%08X", num);
    return appendStrA(out, buff);
} // static HRESULT appendStrHexA()

/***********************************************************************/

LPCWSTR PrettyPrintSigWorker(
               PCCOR_SIGNATURE &  typePtr,      // type to convert,
               size_t             typeLen,      // length of type
               const WCHAR      * name,         // can be "", the name of the method for this sig
               CQuickBytes *      out,          // where to put the pretty printed string
               IMetaDataImport *  pIMDI);       // Import api to use.

//*****************************************************************************
//*****************************************************************************
// pretty prints 'type' to the buffer 'out' returns a pointer to the next type,
// or 0 on a format failure

static PCCOR_SIGNATURE PrettyPrintType(
               PCCOR_SIGNATURE    typePtr,      // type to convert,
               size_t             typeLen,      // Maximum length of the type
               CQuickBytes *      out,          // where to put the pretty printed string
               IMetaDataImport *  pIMDI)        // ptr to IMDInternal class with ComSig
{
    mdToken             tk;
    const WCHAR *       str;
    WCHAR               rcname[MAX_CLASS_NAME];
    HRESULT             hr;
    unsigned __int8     elt = *typePtr++;
    PCCOR_SIGNATURE     typeEnd = typePtr + typeLen;

    switch(elt)
    {
    case ELEMENT_TYPE_VOID:
        str = W("void");
        goto APPEND;

    case ELEMENT_TYPE_BOOLEAN:
        str = W("bool");
        goto APPEND;

    case ELEMENT_TYPE_CHAR:
        str = W("wchar");
        goto APPEND;

    case ELEMENT_TYPE_I1:
        str = W("int8");
        goto APPEND;

    case ELEMENT_TYPE_U1:
        str = W("unsigned int8");
        goto APPEND;

    case ELEMENT_TYPE_I2:
        str = W("int16");
        goto APPEND;

    case ELEMENT_TYPE_U2:
        str = W("unsigned int16");
        goto APPEND;

    case ELEMENT_TYPE_I4:
        str = W("int32");
        goto APPEND;

    case ELEMENT_TYPE_U4:
        str = W("unsigned int32");
        goto APPEND;

    case ELEMENT_TYPE_I8:
        str = W("int64");
        goto APPEND;

    case ELEMENT_TYPE_U8:
        str = W("unsigned int64");
        goto APPEND;

    case ELEMENT_TYPE_R4:
        str = W("float32");
        goto APPEND;

    case ELEMENT_TYPE_R8:
        str = W("float64");
        goto APPEND;

    case ELEMENT_TYPE_U:
        str = W("unsigned int");
        goto APPEND;

    case ELEMENT_TYPE_I:
        str = W("int");
        goto APPEND;

    case ELEMENT_TYPE_OBJECT:
        str = W("class System.Object");
        goto APPEND;

    case ELEMENT_TYPE_STRING:
        str = W("class System.String");
        goto APPEND;

    case ELEMENT_TYPE_CANON_ZAPSIG:
        str = W("class System.__Canon");
        goto APPEND;

    case ELEMENT_TYPE_TYPEDBYREF:
        str = W("refany");
        goto APPEND;

    APPEND:
        appendStrW(out, str);
        break;

    case ELEMENT_TYPE_VALUETYPE:
        str = W("value class ");
        goto DO_CLASS;

    case ELEMENT_TYPE_CLASS:
        str = W("class ");
        goto DO_CLASS;

    DO_CLASS:
        typePtr += CorSigUncompressToken(typePtr, &tk);
        appendStrW(out, str);
        rcname[0] = 0;
        str = rcname;

        if (TypeFromToken(tk) == mdtTypeRef)
        {
            hr = pIMDI->GetTypeRefProps(tk, 0, rcname, ARRAY_SIZE(rcname), 0);
        }
        else if (TypeFromToken(tk) == mdtTypeDef)
        {
            hr = pIMDI->GetTypeDefProps(tk, rcname, ARRAY_SIZE(rcname), 0, 0, 0);
        }
        else
        {
            _ASSERTE(!"Unknown token type encountered in signature.");
            str = W("<UNKNOWN>");
        }

        appendStrW(out, str);
        break;

    case ELEMENT_TYPE_SZARRAY:
        typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
        appendStrW(out, W("[]"));
        break;

    case ELEMENT_TYPE_ARRAY:
        {
            typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
            unsigned rank = CorSigUncompressData(typePtr);
            PREFIX_ASSUME(rank <= 0xffffff);

            // <TODO>TODO what is the syntax for the rank 0 case? </TODO>
            if (rank == 0)
            {
                appendStrW(out, W("[??]"));
            }
            else
            {
                _ASSERTE(rank != 0);
                int* lowerBounds = (int*) _alloca(sizeof(int)*2*rank);
                int* sizes               = &lowerBounds[rank];
                memset(lowerBounds, 0, sizeof(int)*2*rank);

                unsigned numSizes = CorSigUncompressData(typePtr);
                _ASSERTE(numSizes <= rank);
                unsigned int i;
                for(i =0; i < numSizes; i++)
                    sizes[i] = CorSigUncompressData(typePtr);

                unsigned numLowBounds = CorSigUncompressData(typePtr);
                _ASSERTE(numLowBounds <= rank);
                for(i = 0; i < numLowBounds; i++)
                    lowerBounds[i] = CorSigUncompressData(typePtr);

                appendStrW(out, W("["));
                for(i = 0; i < rank; i++)
                {
                    if (sizes[i] != 0 && lowerBounds[i] != 0)
                    {
                        appendStrNumW(out, lowerBounds[i]);
                        appendStrW(out, W("..."));
                        appendStrNumW(out, lowerBounds[i] + sizes[i] + 1);
                    }
                    if (i < rank-1)
                        appendStrW(out, W(","));
                }
                appendStrW(out, W("]"));
            }
        }
        break;

    case ELEMENT_TYPE_MVAR:
        appendStrW(out, W("!!"));
        appendStrNumW(out, CorSigUncompressData(typePtr));
        break;

    case ELEMENT_TYPE_VAR:
        appendStrW(out, W("!"));
        appendStrNumW(out, CorSigUncompressData(typePtr));
        break;

    case ELEMENT_TYPE_GENERICINST:
        {
            typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
            unsigned ntypars = CorSigUncompressData(typePtr);
            appendStrW(out, W("<"));
            for (unsigned i = 0; i < ntypars; i++)
            {
                if (i > 0)
                    appendStrW(out, W(","));
                typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
            }
            appendStrW(out, W(">"));
        }
        break;

    case ELEMENT_TYPE_MODULE_ZAPSIG:
        appendStrW(out, W("[module#"));
        appendStrNumW(out, CorSigUncompressData(typePtr));
        appendStrW(out, W(", token#"));
        typePtr += CorSigUncompressToken(typePtr, &tk);
        appendStrHexW(out, tk);
        appendStrW(out, W("]"));
        break;

    case ELEMENT_TYPE_FNPTR:
        appendStrW(out, W("fnptr "));
        PrettyPrintSigWorker(typePtr, (typeEnd - typePtr), W(""), out, pIMDI);
        break;

    case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
        appendStrW(out, W("native "));
        typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
        break;

    // Modifiers or depedant types
    case ELEMENT_TYPE_PINNED:
        str = W(" pinned");
        goto MODIFIER;

    case ELEMENT_TYPE_PTR:
        str = W("*");
        goto MODIFIER;

    case ELEMENT_TYPE_BYREF:
        str = W("&");
        goto MODIFIER;

    MODIFIER:
        typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
        appendStrW(out, str);
        break;

    default:
    case ELEMENT_TYPE_SENTINEL:
    case ELEMENT_TYPE_END:
        _ASSERTE(!"Unknown Type");
        return(typePtr);
        break;
    }
    return(typePtr);
} // static PCCOR_SIGNATURE PrettyPrintType()

//*****************************************************************************
// Converts a com signature to a text signature.
//
// Note that this function DOES NULL terminate the result signature string.
//*****************************************************************************
LPCWSTR PrettyPrintSigLegacy(
        PCCOR_SIGNATURE   typePtr,      // type to convert,
        unsigned          typeLen,      // length of type
        const WCHAR     * name,         // can be "", the name of the method for this sig
        CQuickBytes     * out,          // where to put the pretty printed string
        IMetaDataImport * pIMDI)        // Import api to use.
{
    return PrettyPrintSigWorker(typePtr, typeLen, name, out, pIMDI);
} // LPCWSTR PrettyPrintSigLegacy()

LPCWSTR PrettyPrintSigWorker(
        PCCOR_SIGNATURE & typePtr,      // type to convert,
        size_t            typeLen,      // length of type
        const WCHAR     * name,         // can be "", the name of the method for this sig
        CQuickBytes     * out,          // where to put the pretty printed string
        IMetaDataImport * pIMDI)        // Import api to use.
{
    out->Shrink(0);
    unsigned numTyArgs = 0;
    unsigned numArgs;
    PCCOR_SIGNATURE typeEnd = typePtr + typeLen;    // End of the signature.

    if (name != 0)                      // 0 means a local var sig
    {
        // get the calling convention out
        unsigned callConv = CorSigUncompressData(typePtr);

        if (isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_FIELD))
        {
            PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
            if (name != 0 && *name != 0)
            {
                appendStrW(out, W(" "));
                appendStrW(out, name);
            }
            return(asStringW(out));
        }

        if (callConv & IMAGE_CEE_CS_CALLCONV_HASTHIS)
            appendStrW(out, W("instance "));

        if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            appendStrW(out, W("generic "));
            numTyArgs = CorSigUncompressData(typePtr);
        }

        static const WCHAR * const callConvNames[IMAGE_CEE_CS_CALLCONV_MAX] =
        {
            W(""),
            W("unmanaged cdecl "),
            W("unmanaged stdcall "),
            W("unmanaged thiscall "),
            W("unmanaged fastcall "),
            W("vararg "),
            W("<error> "),
            W("<error> "),
            W(""),
            W(""),
            W(""),
            W("native vararg ")
        };

        if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) < IMAGE_CEE_CS_CALLCONV_MAX)
        {
        appendStrW(out, callConvNames[callConv & IMAGE_CEE_CS_CALLCONV_MASK]);
        }


        numArgs = CorSigUncompressData(typePtr);
        // do return type
        typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);

    }
    else
    {
        numArgs = CorSigUncompressData(typePtr);
    }

    if (name != 0 && *name != 0)
    {
        appendStrW(out, W(" "));
        appendStrW(out, name);
    }
    appendStrW(out, W("("));

    bool needComma = false;

    while (numArgs)
    {
        if (typePtr >= typeEnd)
            break;

        if (*typePtr == ELEMENT_TYPE_SENTINEL)
        {
            if (needComma)
                appendStrW(out, W(","));
            appendStrW(out, W("..."));
            typePtr++;
        }
        else
        {
            if (numArgs <= 0)
                break;
            if (needComma)
                appendStrW(out, W(","));
            typePtr = PrettyPrintType(typePtr, (typeEnd - typePtr), out, pIMDI);
            --numArgs;
        }
        needComma = true;
    }
    appendStrW(out, W(")"));
    return (asStringW(out));
} // LPCWSTR PrettyPrintSigWorker()


// Internal implementation of PrettyPrintSig().

HRESULT PrettyPrintSigWorkerInternal(
        PCCOR_SIGNATURE   & typePtr,    // type to convert,
        size_t              typeLen,    // length of type
        const CHAR        * name,       // can be "", the name of the method for this sig
        CQuickBytes       * out,        // where to put the pretty printed string
        IMDInternalImport * pIMDI);     // Import api to use.

static HRESULT PrettyPrintClass(
    PCCOR_SIGNATURE     &typePtr,   // type to convert
    PCCOR_SIGNATURE     typeEnd,    // end of the signature.
    CQuickBytes         *out,       // where to put the pretty printed string
    IMDInternalImport   *pIMDI);    // ptr to IMDInternal class with ComSig


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
//*****************************************************************************
//*****************************************************************************
// pretty prints 'type' to the buffer 'out' returns a pointer to the next type,
// or 0 on a format failure

__checkReturn
static HRESULT PrettyPrintTypeA(
    PCCOR_SIGNATURE   &typePtr,     // type to convert,
    size_t             typeLen,     // Maximum length of the type.
    CQuickBytes       *out,         // where to put the pretty printed string
    IMDInternalImport *pIMDI)       // ptr to IMDInternal class with ComSig
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    mdToken     tk;                     // A type's token.
    const CHAR  *str;                   // Temporary string.
    HRESULT     hr;                     // A result.

    PCCOR_SIGNATURE typeEnd = typePtr + typeLen;    // End of the signature.
    unsigned __int8     elt = *typePtr++;

    switch(elt)  {
    case ELEMENT_TYPE_VOID:
        str = "void";
        goto APPEND;

    case ELEMENT_TYPE_BOOLEAN:
        str = "bool";
        goto APPEND;

    case ELEMENT_TYPE_CHAR:
        str = "wchar";
        goto APPEND;

    case ELEMENT_TYPE_I1:
        str = "int8";
        goto APPEND;

    case ELEMENT_TYPE_U1:
        str = "unsigned int8";
        goto APPEND;

    case ELEMENT_TYPE_I2:
        str = "int16";
        goto APPEND;

    case ELEMENT_TYPE_U2:
        str = "unsigned int16";
        goto APPEND;

    case ELEMENT_TYPE_I4:
        str = "int32";
        goto APPEND;

    case ELEMENT_TYPE_U4:
        str = "unsigned int32";
        goto APPEND;

    case ELEMENT_TYPE_I8:
        str = "int64";
        goto APPEND;

    case ELEMENT_TYPE_U8:
        str = "unsigned int64";
        goto APPEND;

    case ELEMENT_TYPE_R4:
        str = "float32";
        goto APPEND;

    case ELEMENT_TYPE_R8:
        str = "float64";
        goto APPEND;

    case ELEMENT_TYPE_U:
        str = "unsigned int";
        goto APPEND;

    case ELEMENT_TYPE_I:
        str = "int";
        goto APPEND;

    case ELEMENT_TYPE_OBJECT:
        str = "class System.Object";
        goto APPEND;

    case ELEMENT_TYPE_STRING:
        str = "class System.String";
        goto APPEND;

    case ELEMENT_TYPE_CANON_ZAPSIG:
        str = "class System.__Canon";
        goto APPEND;

    case ELEMENT_TYPE_TYPEDBYREF:
        str = "refany";
        goto APPEND;

    APPEND:
        IfFailGo(appendStrA(out, str));
        break;

    case ELEMENT_TYPE_INTERNAL:
        void* pMT;
        pMT = *((void* UNALIGNED *)typePtr);
        typePtr += sizeof(void*);
        CHAR tempBuffer[64];
        sprintf_s(tempBuffer, 64, "pMT: %p", pMT);
        IfFailGo(appendStrA(out, tempBuffer));
        break;

    case ELEMENT_TYPE_VALUETYPE:
        str = "value class ";
        goto DO_CLASS;

    case ELEMENT_TYPE_CLASS:
        str = "class ";
        goto DO_CLASS;

    DO_CLASS:
        IfFailGo(appendStrA(out, str));
        IfFailGo(PrettyPrintClass(typePtr, typeEnd, out, pIMDI));
        break;

    case ELEMENT_TYPE_CMOD_REQD:
        str = "required_modifier ";
        goto CMOD;

    case ELEMENT_TYPE_CMOD_OPT:
        str = "optional_modifier ";
        goto CMOD;

    CMOD:
        IfFailGo(appendStrA(out, str));
        IfFailGo(PrettyPrintClass(typePtr, typeEnd, out, pIMDI));
        IfFailGo(appendStrA(out, " "));
        IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
        break;

    case ELEMENT_TYPE_SZARRAY:
        IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
        IfFailGo(appendStrA(out, "[]"));
        break;

    case ELEMENT_TYPE_ARRAY:
        {
            IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
            unsigned rank = CorSigUncompressData(typePtr);
            PREFIX_ASSUME(rank <= 0xffffff);
            // <TODO>TODO what is the syntax for the rank 0 case? </TODO>
            if (rank == 0)
            {
                IfFailGo(appendStrA(out, "[??]"));
            }
            else
            {
                _ASSERTE(rank != 0);
                int* lowerBounds = (int*) _alloca(sizeof(int)*2*rank);
                int* sizes       = &lowerBounds[rank];
                memset(lowerBounds, 0, sizeof(int)*2*rank);

                unsigned numSizes = CorSigUncompressData(typePtr);
                _ASSERTE(numSizes <= rank);
                unsigned int i;
                for(i =0; i < numSizes; i++)
                    sizes[i] = CorSigUncompressData(typePtr);

                unsigned numLowBounds = CorSigUncompressData(typePtr);
                _ASSERTE(numLowBounds <= rank);
                for(i = 0; i < numLowBounds; i++)
                    lowerBounds[i] = CorSigUncompressData(typePtr);

                IfFailGo(appendStrA(out, "["));
                for(i = 0; i < rank; i++)
                {
                    if (sizes[i] != 0 && lowerBounds[i] != 0)
                    {
                         appendStrNumA(out, lowerBounds[i]);
                         IfFailGo(appendStrA(out, "..."));
                         appendStrNumA(out, lowerBounds[i] + sizes[i] + 1);
                    }
                    if (i < rank-1)
                        IfFailGo(appendStrA(out, ","));
                }
                IfFailGo(appendStrA(out, "]"));
            }
        }
        break;

    case ELEMENT_TYPE_MVAR:
        IfFailGo(appendStrA(out, "!!"));
        appendStrNumA(out, CorSigUncompressData(typePtr));
        break;

    case ELEMENT_TYPE_VAR:
        IfFailGo(appendStrA(out, "!"));
        appendStrNumA(out, CorSigUncompressData(typePtr));
        break;

    case ELEMENT_TYPE_GENERICINST:
        {
            IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
            unsigned ntypars = CorSigUncompressData(typePtr);
            IfFailGo(appendStrA(out, "<"));
            for (unsigned i = 0; i < ntypars; i++)
            {
                if (i > 0)
                    IfFailGo(appendStrA(out, ","));
                IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
            }
            IfFailGo(appendStrA(out, ">"));
        }
        break;

    case ELEMENT_TYPE_MODULE_ZAPSIG:
        IfFailGo(appendStrA(out, "[module#"));
        appendStrNumA(out, CorSigUncompressData(typePtr));
        IfFailGo(appendStrA(out, ", token#"));
        typePtr += CorSigUncompressToken(typePtr, &tk);
        IfFailGo(appendStrHexA(out, tk));
        IfFailGo(appendStrA(out, "]"));
        break;

    case ELEMENT_TYPE_FNPTR:
        {
            IfFailGo(appendStrA(out, "fnptr "));
            CQuickBytes qbOut;
            IfFailGo(PrettyPrintSigWorkerInternal(typePtr, (typeEnd - typePtr), "", &qbOut,pIMDI));
            IfFailGo(appendStrA(out, (char *)qbOut.Ptr()));
        }
        break;

    case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
        IfFailGo(appendStrA(out, "native "));
        IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
        break;

        // Modifiers or dependent types
    case ELEMENT_TYPE_PINNED:
        str = " pinned";
        goto MODIFIER;

    case ELEMENT_TYPE_PTR:
        str = "*";
        goto MODIFIER;

    case ELEMENT_TYPE_BYREF:
        str = "&";
        goto MODIFIER;

    MODIFIER:
        IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
        IfFailGo(appendStrA(out, str));
        break;

    default:
    case ELEMENT_TYPE_SENTINEL:
    case ELEMENT_TYPE_END:
        hr = E_INVALIDARG;
        break;
    }
 ErrExit:
    return hr;
} // PrettyPrintTypeA
#ifdef _PREFAST_
#pragma warning(pop)
#endif

// pretty prints the class 'type' to the buffer 'out'
static HRESULT PrettyPrintClass(
    PCCOR_SIGNATURE     &typePtr,   // type to convert
    PCCOR_SIGNATURE     typeEnd,    // end of the signature.
    CQuickBytes         *out,       // where to put the pretty printed string
    IMDInternalImport   *pIMDI)     // ptr to IMDInternal class with ComSig
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    mdToken     tk;
    const CHAR  *str;   // type's token.
    LPCUTF8     pNS;    // type's namespace.
    LPCUTF8     pN;     // type's name.
    HRESULT     hr;     // result

    IfFailGo(CorSigUncompressToken_EndPtr(typePtr, typeEnd, &tk));
    str = "<UNKNOWN>";

    if (TypeFromToken(tk) == mdtTypeSpec)
    {
        ULONG cSig;
        PCCOR_SIGNATURE sig;
        IfFailGo(pIMDI->GetSigFromToken(tk, &cSig, &sig));
        IfFailGo(PrettyPrintTypeA(sig, cSig, out, pIMDI));
    }
    else
    {
        if (TypeFromToken(tk) == mdtTypeRef)
        {
            //<TODO>@consider: assembly name?</TODO>
            if (FAILED(pIMDI->GetNameOfTypeRef(tk, &pNS, &pN)))
            {
                pNS = pN = "Invalid TypeRef record";
            }
        }
        else
        {
            _ASSERTE(TypeFromToken(tk) == mdtTypeDef);
            if (FAILED(pIMDI->GetNameOfTypeDef(tk, &pN, &pNS)))
            {
                pNS = pN = "Invalid TypeDef record";
            }
        }

        if (pNS && *pNS)
        {
            IfFailGo(appendStrA(out, pNS));
            IfFailGo(appendStrA(out, NAMESPACE_SEPARATOR_STR));
        }
        IfFailGo(appendStrA(out, pN));
    }
    return S_OK;

ErrExit:
    return hr;
} // static HRESULT PrettyPrintClass()

//*****************************************************************************
// Converts a com signature to a text signature.
//
// Note that this function DOES NULL terminate the result signature string.
//*****************************************************************************
HRESULT PrettyPrintSigInternalLegacy(
          PCCOR_SIGNATURE     typePtr,  // type to convert,
          unsigned            typeLen,  // length of type
          const CHAR        * name,     // can be "", the name of the method for this sig
          CQuickBytes       * out,      // where to put the pretty printed string
          IMDInternalImport * pIMDI)    // Import api to use.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    return PrettyPrintSigWorkerInternal(typePtr, typeLen, name, out, pIMDI);
} // HRESULT PrettyPrintSigInternalLegacy()

HRESULT PrettyPrintSigWorkerInternal(
          PCCOR_SIGNATURE   & typePtr,  // type to convert,
          size_t              typeLen,  // length of type
          const CHAR        * name,     // can be "", the name of the method for this sig
          CQuickBytes       * out,      // where to put the pretty printed string
          IMDInternalImport * pIMDI)    // Import api to use.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT     hr = S_OK;
    unsigned    numArgs;     // Count of arguments to function, or count of local vars.
    unsigned numTyArgs = 0;
    PCCOR_SIGNATURE typeEnd = typePtr + typeLen;
    bool needComma = false;

    out->Shrink(0);

    if (name != 0)           // 0 means a local var sig
    {
        // get the calling convention out
        unsigned callConv = CorSigUncompressData(typePtr);

        if (isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_FIELD))
        {
            IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
            if (name != 0 && *name != 0)

            {
                IfFailGo(appendStrA(out, " "));
                IfFailGo(appendStrA(out, name));
            }
            goto ErrExit;
        }

        if (callConv & IMAGE_CEE_CS_CALLCONV_HASTHIS)
            IfFailGo(appendStrA(out, "instance "));

        if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            IfFailGo(appendStrA(out, "generic "));
            numTyArgs = CorSigUncompressData(typePtr);
        }

        static const CHAR* const callConvNames[IMAGE_CEE_CS_CALLCONV_MAX] =
        {
            "",
            "unmanaged cdecl ",
            "unmanaged stdcall ",
            "unmanaged thiscall ",
            "unmanaged fastcall ",
            "vararg ",
            "<error> ",
            "<error> ",
            "",
            "",
            "",
            "native vararg "
        };

        if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) < IMAGE_CEE_CS_CALLCONV_MAX)
        {
        appendStrA(out, callConvNames[callConv & IMAGE_CEE_CS_CALLCONV_MASK]);
        }

        numArgs = CorSigUncompressData(typePtr);
        // do return type
        IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
    }
    else
    {
        numArgs = CorSigUncompressData(typePtr);
    }

    if (name != 0 && *name != 0)
    {
        IfFailGo(appendStrA(out, " "));
        IfFailGo(appendStrA(out, name));
    }
    IfFailGo(appendStrA(out, "("));

    while (numArgs)
    {
        if (typePtr >= typeEnd)
            break;

        if (*typePtr == ELEMENT_TYPE_SENTINEL)
        {
            if (needComma)
                IfFailGo(appendStrA(out, ","));
            IfFailGo(appendStrA(out, "..."));
            ++typePtr;
        }
        else
        {
            if (needComma)
                IfFailGo(appendStrA(out, ","));
            IfFailGo(PrettyPrintTypeA(typePtr, (typeEnd - typePtr), out, pIMDI));
            --numArgs;
        }
        needComma = true;
    }
    IfFailGo(appendStrA(out, ")"));
    if (asStringA(out) == 0)
        IfFailGo(E_OUTOFMEMORY);

 ErrExit:
    return hr;
} // HRESULT PrettyPrintSigWorkerInternal()
