// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
/******************************************************************************/
/*                                 formatType.cpp                             */
/******************************************************************************/
#include "formattype.h"

/******************************************************************************/
char* asString(CQuickBytes *out) {
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    SIZE_T oldSize = out->Size();
    out->ReSizeThrows(oldSize + 1);
    char* cur = &((char*) out->Ptr())[oldSize];
    *cur = 0;
    out->ReSizeThrows(oldSize);     // Don't count the null character
    return((char*) out->Ptr());
}

void appendStr(CQuickBytes *out, const char* str, unsigned len) {
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if(len == (unsigned)(-1)) len = (unsigned)strlen(str);
    SIZE_T oldSize = out->Size();
    out->ReSizeThrows(oldSize + len);
    char* cur = &((char*) out->Ptr())[oldSize];
    memcpy(cur, str, len);
        // Note no trailing null!
}

void appendChar(CQuickBytes *out, char chr) {
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    SIZE_T oldSize = out->Size();
    out->ReSizeThrows(oldSize + 1);
    ((char*) out->Ptr())[oldSize] = chr;
        // Note no trailing null!
}

void insertStr(CQuickBytes *out, const char* str) {
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    unsigned len = (unsigned)strlen(str);
    SIZE_T oldSize = out->Size();
    out->ReSizeThrows(oldSize + len);
    char* cur = &((char*) out->Ptr())[len];
    memmove(cur,out->Ptr(),oldSize);
    memcpy(out->Ptr(), str, len);
        // Note no trailing null!
}

static void appendStrNum(CQuickBytes *out, int num) {
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    char buff[16];
    sprintf_s(buff, ARRAY_SIZE(buff), "%d", num);
    appendStr(out, buff);
}

PCCOR_SIGNATURE PrettyPrintSignature(
    PCCOR_SIGNATURE typePtr,            // type to convert,
    unsigned typeLen,                   // the length of 'typePtr'
    const char* name,                   // can be "", the name of the method for this sig 0 means local var sig
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI,           // ptr to IMDInternalImport class with ComSig
	_In_opt_z_ const char* inlabel,       // prefix for names (NULL if no names required)
    BOOL printTyArity=FALSE);


PCCOR_SIGNATURE PrettyPrintTypeOrDef(
    PCCOR_SIGNATURE typePtr,            // type to convert,
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI);          // ptr to IMDInternal class with ComSig

//*****************************************************************************
// Parse a length, return the length, size of the length.
//*****************************************************************************
ULONG GetLength(            // Length or -1 on error.
    void const *pData,      // First byte of length.
    int        *pSizeLen)   // Put size of length here, if not 0.
{
    LIMITED_METHOD_CONTRACT;

    BYTE const *pBytes = reinterpret_cast<BYTE const*>(pData);

    if(pBytes)
    {
        if ((*pBytes & 0x80) == 0x00)       // 0??? ????
        {
            if (pSizeLen) *pSizeLen = 1;
            return (*pBytes & 0x7f);
        }

        if ((*pBytes & 0xC0) == 0x80)       // 10?? ????
        {
            if (pSizeLen) *pSizeLen = 2;
            return ((*pBytes & 0x3f) << 8 | *(pBytes+1));
        }

        if ((*pBytes & 0xE0) == 0xC0)       // 110? ????
        {
            if (pSizeLen) *pSizeLen = 4;
            return ((*pBytes & 0x1f) << 24 | *(pBytes+1) << 16 | *(pBytes+2) << 8 | *(pBytes+3));
        }
    }
    if(pSizeLen) *pSizeLen = 0;
    return 0;
}


/******************************************************************************/
const char* PrettyPrintSig(
    PCCOR_SIGNATURE typePtr,            // type to convert,
    unsigned typeLen,                   // the length of 'typePtr'
    const char* name,                   // can be "", the name of the method for this sig 0 means local var sig
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI,           // ptr to IMDInternalImport class with ComSig
    const char* inlabel,                // prefix for names (NULL if no names required)
    BOOL printTyArity)
{
    STATIC_CONTRACT_THROWS;

    EX_TRY
    {
        PrettyPrintSignature(typePtr,
                             typeLen,
                             name,
                             out,
                             pIMDI,
                             inlabel,
                             printTyArity);
    }
    EX_CATCH
    {
        out->Shrink(0);
        appendStr(out,"ERROR PARSING THE SIGNATURE");
    }
    EX_END_CATCH(SwallowAllExceptions);

    return(asString(out));
}

/********************************************************************************/
// Converts a com signature to a printable signature.
// Note that return value is pointing at the CQuickBytes buffer,

PCCOR_SIGNATURE PrettyPrintSignature(
    PCCOR_SIGNATURE typePtr,    // type to convert,
    unsigned typeLen,           // the length of 'typePtr'
    const char* name,           // can be "", the name of the method for this sig 0 means local var sig
    CQuickBytes *out,           // where to put the pretty printed string
    IMDInternalImport *pIMDI,   // ptr to IMDInternalImport class with ComSig
    const char* inlabel,        // prefix for names (NULL if no names required)
    BOOL printTyArity)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    unsigned numArgs;
    unsigned numTyArgs = 0;
    PCCOR_SIGNATURE typeEnd = typePtr + typeLen;
    unsigned ixArg= 0; //arg index
    char argname[1024];
    char label[MAX_PREFIX_SIZE];
    const char* openpar = "(";
    const char* closepar = ")";
    ParamDescriptor* pszArgName = NULL; // ptr to array of names (if provided by debug info)

    if(inlabel && *inlabel) // check for *inlabel is totally unnecessary, added to pacify the PREFIX
    {
        strcpy_s(label,MAX_PREFIX_SIZE,inlabel);
        ixArg = label[strlen(label)-1] - '0';
        label[strlen(label)-1] = 0;
        if(label[0] == '@') // it's pointer!
        {
#ifdef HOST_64BIT
            pszArgName = (ParamDescriptor*)_atoi64(&label[1]);
#else // !HOST_64BIT
            pszArgName = (ParamDescriptor*)(size_t)atoi(&label[1]);
#endif // HOST_64BIT
        }
    }

    // 0 means a local var sig
    if (name != 0)
    {
        // get the calling convention out
        unsigned callConv = CorSigUncompressData(typePtr);

        // should not be a local var sig
        _ASSERTE(!isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_LOCAL_SIG));

        if (isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_FIELD))
        {
            typePtr = PrettyPrintTypeOrDef(typePtr, out, pIMDI);
            if (*name)
            {
                appendChar(out, ' ');
                appendStr(out, name);
            }
            return(typePtr);
        }

        if (callConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
            appendStr(out, KEYWORD("explicit "));

        if (callConv & IMAGE_CEE_CS_CALLCONV_HASTHIS)
            appendStr(out, KEYWORD("instance "));

        if (isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_GENERICINST))
        {
          openpar = LTN();
          closepar = GTN();
        }
        else
        {
            const char* const callConvUndefined = (const char*)-1;
            static const char* const callConvNames[16] = {
                "",
                "unmanaged cdecl ",
                "unmanaged stdcall ",
                "unmanaged thiscall ",
                "unmanaged fastcall ",
                "vararg ",
                callConvUndefined, // field
                callConvUndefined, // local sig
                callConvUndefined, // property
                "unmanaged ",
                callConvUndefined,
                callConvUndefined,
                callConvUndefined,
                callConvUndefined,
                callConvUndefined,
                callConvUndefined
                };
            static_assert_no_msg(ARRAY_SIZE(callConvNames) == (IMAGE_CEE_CS_CALLCONV_MASK + 1));

            char tmp[32];
            unsigned callConvIdx = callConv & IMAGE_CEE_CS_CALLCONV_MASK;
            const char* name_cc = callConvNames[callConvIdx];
            if (name_cc == callConvUndefined)
            {
                sprintf_s(tmp, ARRAY_SIZE(tmp), "callconv(%u) ", callConvIdx);
                name_cc = tmp;
            }

            appendStr(out, KEYWORD(name_cc));
        }

        if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
          numTyArgs = CorSigUncompressData(typePtr);
        }
        numArgs = CorSigUncompressData(typePtr);
        if (!isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_GENERICINST))
        {
                // do return type
            if(pszArgName)
            {
                argname[0] = 0;
                DumpParamAttr(argname, ARRAY_SIZE(argname), pszArgName[ixArg+numArgs].attr);
                appendStr(out,argname);
            }
            typePtr = PrettyPrintTypeOrDef(typePtr, out, pIMDI);
            if(pszArgName)
            {
                argname[0] = ' '; argname[1] = 0;
                DumpMarshaling(pIMDI,argname, ARRAY_SIZE(argname), pszArgName[ixArg+numArgs].tok);
                appendStr(out,argname);
            }
            if(*name != 0)
            {
                appendChar(out, ' ');
                appendStr(out, name);
            }
            if((numTyArgs != 0)&&printTyArity)
            {
                appendStr(out,LTN());
                appendChar(out,'[');
                appendStrNum(out,numTyArgs);
                appendChar(out,']');
                appendStr(out,GTN());
            }
        }
    }
    else
    {
        // get the calling convention out
#ifdef _DEBUG
        unsigned callConv =
#endif
            CorSigUncompressData(typePtr);
#ifdef _DEBUG
        (void)callConv; //prevent "unused variable" warning from GCC
        // should be a local var sig
        _ASSERTE(callConv == IMAGE_CEE_CS_CALLCONV_LOCAL_SIG);
#endif

        numArgs = CorSigUncompressData(typePtr);
    }

    appendStr(out, openpar);

    bool needComma = false;
    while(typePtr < typeEnd)
    {
        if(name) // printing the arguments
        {
            PREFIX_ASSUME(typePtr != NULL);
            if (*typePtr == ELEMENT_TYPE_SENTINEL)
            {
                if (needComma)
                    appendChar(out, ',');
                appendStr(out, "...");
                typePtr++;
            }
            else
            {
                if (numArgs <= 0)
                    break;
                if (needComma)
                    appendChar(out, ',');
                if(pszArgName)
                {
                    argname[0] = 0;
                    DumpParamAttr(argname, ARRAY_SIZE(argname), pszArgName[ixArg].attr);
                    appendStr(out,argname);
                }
                typePtr = PrettyPrintTypeOrDef(typePtr, out, pIMDI);
                if(inlabel)
                {
                    if(pszArgName)
                    {
                        argname[0] = ' '; argname[1] = 0;
                        DumpMarshaling(pIMDI,argname, ARRAY_SIZE(argname), pszArgName[ixArg].tok);
                        strcat_s(argname, ARRAY_SIZE(argname), ProperName(pszArgName[ixArg++].name));
                    }
                    else sprintf_s(argname, ARRAY_SIZE(argname), " %s_%d", label,ixArg++);
                    appendStr(out,argname);
                }
                --numArgs;
            }
        }
        else // printing local vars
        {
            if (numArgs <= 0)
                break;
            if(pszArgName)
            {
                if(pszArgName[ixArg].attr == 0xFFFFFFFF)
                {
                    CQuickBytes fake_out;
                    typePtr = PrettyPrintTypeOrDef(typePtr, &fake_out, pIMDI);
                    ixArg++;
                    numArgs--;
                    continue;
                }
            }
            if (needComma)
                appendChar(out, ',');
            if(pszArgName)
            {
                sprintf_s(argname,ARRAY_SIZE(argname),"[%d] ",pszArgName[ixArg].attr);
                appendStr(out,argname);
            }
            typePtr = PrettyPrintTypeOrDef(typePtr, out, pIMDI);
            if(inlabel)
            {
                if(pszArgName)
                {
                    sprintf_s(argname,ARRAY_SIZE(argname)," %s",ProperLocalName(pszArgName[ixArg++].name));
                }
                else sprintf_s(argname,ARRAY_SIZE(argname)," %s_%d",label,ixArg++);
                appendStr(out,argname);
            }
            --numArgs;
        }
        needComma = true;
    }
        // Have we finished printing all the arguments?
    if (numArgs > 0) {
        appendStr(out, ERRORMSG(" [SIGNATURE ENDED PREMATURELY]"));
    }

    appendStr(out, closepar);
    return(typePtr);
}


/******************************************************************************/
// pretty prints 'type' or its 'typedef' to the buffer 'out' returns a pointer to the next type,
// or 0 on a format failure; outside ILDASM -- simple wrapper for PrettyPrintType

PCCOR_SIGNATURE PrettyPrintTypeOrDef(
    PCCOR_SIGNATURE typePtr,            // type to convert,
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI)           // ptr to IMDInternal class with ComSig
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PCCOR_SIGNATURE pBegin, pEnd=NULL;

#ifdef __ILDASM__
    ULONG L = (ULONG)(out->Size());
#endif
    pBegin = typePtr;
    pEnd = PrettyPrintType(typePtr,out,pIMDI);
#ifdef __ILDASM__
    if(pEnd > pBegin) // PrettyPrintType can return NULL
    {
        DWORD i;
        ULONG l = (ULONG)(pEnd - pBegin);
        for(i=0; i < g_NumTypedefs; i++)
        {
            if(((*g_typedefs)[i].cb == l)
               && (memcmp((*g_typedefs)[i].psig,pBegin,l)==0))
            {
                out->Shrink(L); // discard output of PrettyPrintType
                appendStr(out, JUMPPT(ProperName((*g_typedefs)[i].szName),(*g_typedefs)[i].tkSelf));
                break;
            }
        }
    }
#endif
    return pEnd;
}

/******************************************************************************/
// pretty prints 'type' to the buffer 'out' returns a pointer to the next type,
// or 0 on a format failure

PCCOR_SIGNATURE PrettyPrintType(
    PCCOR_SIGNATURE typePtr,            // type to convert,
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI)           // ptr to IMDInternal class with ComSig
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    mdToken  tk;
    const char* str;
    int typ;
    CQuickBytes tmp;
    CQuickBytes Appendix;
    BOOL Reiterate;
    int n;

    do {
        Reiterate = FALSE;
        switch(typ = *typePtr++) {
            case ELEMENT_TYPE_VOID          :
                str = "void"; goto APPEND;
            case ELEMENT_TYPE_BOOLEAN       :
                str = "bool"; goto APPEND;
            case ELEMENT_TYPE_CHAR          :
                str = "char"; goto APPEND;
            case ELEMENT_TYPE_I1            :
                str = "int8"; goto APPEND;
            case ELEMENT_TYPE_U1            :
                str = "uint8"; goto APPEND;
            case ELEMENT_TYPE_I2            :
                str = "int16"; goto APPEND;
            case ELEMENT_TYPE_U2            :
                str = "uint16"; goto APPEND;
            case ELEMENT_TYPE_I4            :
                str = "int32"; goto APPEND;
            case ELEMENT_TYPE_U4            :
                str = "uint32"; goto APPEND;
            case ELEMENT_TYPE_I8            :
                str = "int64"; goto APPEND;
            case ELEMENT_TYPE_U8            :
                str = "uint64"; goto APPEND;
            case ELEMENT_TYPE_R4            :
                str = "float32"; goto APPEND;
            case ELEMENT_TYPE_R8            :
                str = "float64"; goto APPEND;
            case ELEMENT_TYPE_U             :
                str = "native uint"; goto APPEND;
            case ELEMENT_TYPE_I             :
                str = "native int"; goto APPEND;
            case ELEMENT_TYPE_OBJECT        :
                str = "object"; goto APPEND;
            case ELEMENT_TYPE_STRING        :
                str = "string"; goto APPEND;
            case ELEMENT_TYPE_TYPEDBYREF        :
                str = "typedref"; goto APPEND;
            APPEND:
                appendStr(out, KEYWORD((char*)str));
                break;

            case ELEMENT_TYPE_VALUETYPE    :
                str = "valuetype ";
                goto DO_CLASS;
            case ELEMENT_TYPE_CLASS         :
                str = "class ";
                goto DO_CLASS;

            DO_CLASS:
                appendStr(out, KEYWORD((char*)str));
                typePtr += CorSigUncompressToken(typePtr, &tk);
                if(IsNilToken(tk))
                {
                    appendStr(out, "[ERROR! NIL TOKEN]");
                }
                else PrettyPrintClass(out, tk, pIMDI);
                REGISTER_REF(g_tkRefUser,tk)
                break;

            case ELEMENT_TYPE_SZARRAY    :
                insertStr(&Appendix,"[]");
                Reiterate = TRUE;
                break;

            case ELEMENT_TYPE_ARRAY       :
                {
                typePtr = PrettyPrintTypeOrDef(typePtr, out, pIMDI);
                PREFIX_ASSUME(typePtr != NULL);
                unsigned rank = CorSigUncompressData(typePtr);
                    // <TODO> what is the syntax for the rank 0 case? </TODO>
                if (rank == 0) {
                    appendStr(out, ERRORMSG("[BAD: RANK == 0!]"));
                }
                else {
                    _ASSERTE(rank != 0);

#ifdef _PREFAST_
#pragma prefast(push)
#pragma prefast(disable:22009 "Suppress PREFAST warnings about integer overflow")
#endif
                    int* lowerBounds = (int*) _alloca(sizeof(int)*2*rank);
                    int* sizes       = &lowerBounds[rank];
                    memset(lowerBounds, 0, sizeof(int)*2*rank);

                    unsigned numSizes = CorSigUncompressData(typePtr);
                    _ASSERTE(numSizes <= rank);
                    unsigned i;
                    for(i =0; i < numSizes; i++)
                        sizes[i] = CorSigUncompressData(typePtr);

                    unsigned numLowBounds = CorSigUncompressData(typePtr);
                    _ASSERTE(numLowBounds <= rank);
                    for(i = 0; i < numLowBounds; i++)
                        typePtr+=CorSigUncompressSignedInt(typePtr,&lowerBounds[i]);

                    appendChar(out, '[');
                    if (rank == 1 && numSizes == 0 && numLowBounds == 0)
                        appendStr(out, "...");
                    else {
                        for(i = 0; i < rank; i++)
                        {
                            //if (sizes[i] != 0 || lowerBounds[i] != 0)
                            {
                                if (lowerBounds[i] == 0 && i < numSizes)
                                    appendStrNum(out, sizes[i]);
                                else
                                {
                                    if(i < numLowBounds)
                                    {
                                        appendStrNum(out, lowerBounds[i]);
                                        appendStr(out, "...");
                                        if (/*sizes[i] != 0 && */i < numSizes)
                                            appendStrNum(out, lowerBounds[i] + sizes[i] - 1);
                                    }
                                }
                            }
                            if (i < rank-1)
                                appendChar(out, ',');
                        }
                    }
                    appendChar(out, ']');
#ifdef _PREFAST_
#pragma prefast(pop)
#endif
                }
                } break;

            case ELEMENT_TYPE_VAR        :
                appendChar(out, '!');
                n  = CorSigUncompressData(typePtr);
#ifdef __ILDASM__
                if(!PrettyPrintGP(g_tkVarOwner,out,n))
#endif
                    appendStrNum(out, n);
                break;

            case ELEMENT_TYPE_MVAR        :
                appendChar(out, '!');
                appendChar(out, '!');
                n  = CorSigUncompressData(typePtr);
#ifdef __ILDASM__
                if(!PrettyPrintGP(g_tkMVarOwner,out,n))
#endif
                    appendStrNum(out, n);
                break;

            case ELEMENT_TYPE_FNPTR :
                appendStr(out, KEYWORD("method "));
                typePtr = PrettyPrintSignature(typePtr, 0x7FFF, "*", out, pIMDI, NULL);
                break;

            case ELEMENT_TYPE_GENERICINST :
            {
              typePtr = PrettyPrintTypeOrDef(typePtr, out, pIMDI);
              appendStr(out, LTN());
              unsigned numArgs = CorSigUncompressData(typePtr);
              bool needComma = false;
              while(numArgs--)
              {
                  if (needComma)
                      appendChar(out, ',');
                  typePtr = PrettyPrintTypeOrDef(typePtr, out, pIMDI);
                  needComma = true;
              }
              appendStr(out, GTN());
              break;
            }

#ifndef __ILDASM__
            case ELEMENT_TYPE_INTERNAL :
            {
                // ELEMENT_TYPE_INTERNAL <TypeHandle>
                _ASSERTE(sizeof(TypeHandle) == sizeof(void *));
                TypeHandle typeHandle;
                typePtr += CorSigUncompressPointer(typePtr, (void **)&typeHandle);

                MethodTable *pMT = NULL;
                if (typeHandle.IsTypeDesc())
                {
                    pMT = typeHandle.AsTypeDesc()->GetMethodTable();
                    if (pMT)
                    {
                        PrettyPrintClass(out, pMT->GetCl(), pMT->GetMDImport());

                        // It could be a "native version" of the managed type used in interop
                        if (typeHandle.AsTypeDesc()->IsNativeValueType())
                            appendStr(out, "_NativeValueType");
                    }
                    else
                        appendStr(out, "(null)");
                }
                else
                {
                    pMT = typeHandle.AsMethodTable();
                    if (pMT)
                        PrettyPrintClass(out, pMT->GetCl(), pMT->GetMDImport());
                    else
                        appendStr(out, "(null)");
                }

                char sz[32];
                sprintf_s(sz, ARRAY_SIZE(sz), " /* MT: 0x%p */", pMT);
                appendStr(out, sz);
                break;
            }
#endif


                // Modifiers or depedent types
            case ELEMENT_TYPE_CMOD_OPT	:
                str = " modopt("; goto ADDCLASSTOCMOD;
            case ELEMENT_TYPE_CMOD_REQD	:
                str = " modreq(";
            ADDCLASSTOCMOD:
                typePtr += CorSigUncompressToken(typePtr, &tk);
                if (IsNilToken(tk))
                {
                    Debug_ReportError("Nil token in custom modifier");
                }
                tmp.Shrink(0);
                appendStr(&tmp, KEYWORD((char*)str));
                PrettyPrintClass(&tmp, tk, pIMDI);
                appendChar(&tmp,')');
                str = (const char *) asString(&tmp);
                goto MODIFIER;
            case ELEMENT_TYPE_PINNED :
                str = " pinned"; goto MODIFIER;
            case ELEMENT_TYPE_PTR           :
                str = "*"; goto MODIFIER;
            case ELEMENT_TYPE_BYREF         :
                str = AMP(); goto MODIFIER;
            MODIFIER:
                insertStr(&Appendix, str);
                Reiterate = TRUE;
                break;

            default:
            case ELEMENT_TYPE_SENTINEL      :
            case ELEMENT_TYPE_END           :
                //_ASSERTE(!"Unknown Type");
                if(typ)
                {
                    char sz[64];
                    sprintf_s(sz,ARRAY_SIZE(sz),"/* UNKNOWN TYPE (0x%X)*/",typ);
                    appendStr(out, ERRORMSG(sz));
                }
                break;
        } // end switch
    } while(Reiterate);
    appendStr(out,asString(&Appendix));
    return(typePtr);
}

/******************************************************************/
const char* PrettyPrintClass(
    CQuickBytes *out,           // where to put the pretty printed string
    mdToken tk,                 // The class token to look up
    IMDInternalImport *pIMDI)   // ptr to IMDInternalImport class with ComSig
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if(tk == mdTokenNil)  // Zero resolution scope for "somewhere here" TypeRefs
    {
        appendStr(out,"[*]");
        return(asString(out));
    }
    if(!pIMDI->IsValidToken(tk))
    {
        char str[1024];
        sprintf_s(str,ARRAY_SIZE(str)," [ERROR: INVALID TOKEN 0x%8.8X] ",tk);
        appendStr(out,ERRORMSG(str));
        return(asString(out));
    }
    switch(TypeFromToken(tk))
    {
        case mdtTypeRef:
        case mdtTypeDef:
#ifdef __ILDASM__
            DWORD ix;
            for(ix = 0; ix < g_NumTypedefs; ix++)
            {
                if((*g_typedefs)[ix].tkTypeSpec == tk) break;
            }
            if(ix < g_NumTypedefs)
            {
                appendStr(out,JUMPPT(ProperName((*g_typedefs)[ix].szName),(*g_typedefs)[ix].tkSelf));
            }
            else
#endif
            {
                const char *nameSpace = 0;
                const char *name = 0;
                mdToken tkEncloser;

                if (TypeFromToken(tk) == mdtTypeRef)
                {
                    if (FAILED(pIMDI->GetResolutionScopeOfTypeRef(tk, &tkEncloser)))
                    {
                        tkEncloser = mdTypeDefNil;
                    }
                    if (FAILED(pIMDI->GetNameOfTypeRef(tk, &nameSpace, &name)))
                    {
                        nameSpace = name = "Invalid TypeRef record";
                    }
                }
                else
                {
                    if (FAILED(pIMDI->GetNestedClassProps(tk,&tkEncloser)))
                    {
                        tkEncloser = mdTypeDefNil;
                    }
                    if (FAILED(pIMDI->GetNameOfTypeDef(tk, &name, &nameSpace)))
                    {
                        nameSpace = name = "Invalid TypeDef record";
                    }
                }
                MAKE_NAME_IF_NONE(name,tk);
                if((tkEncloser == mdTokenNil) || RidFromToken(tkEncloser))
                {
                    PrettyPrintClass(out,tkEncloser,pIMDI);
                    if (TypeFromToken(tkEncloser) == mdtTypeRef || TypeFromToken(tkEncloser) == mdtTypeDef)
                    {
                        appendChar(out, '/');
                        //nameSpace = ""; //don't print namespaces for nested classes!
                    }
                }
                if(TypeFromToken(tk)==mdtTypeDef)
                {
                    unsigned L = (unsigned)strlen(ProperName(name))+1;
                    char* szFN = NULL;
                    if(nameSpace && *nameSpace)
                    {
                        const char* sz = ProperName(nameSpace);
                        L+= (unsigned)strlen(sz)+1;
                        szFN = new char[L];
                        sprintf_s(szFN,L,"%s.",sz);
                    }
                    else
                    {
                        szFN = new char[L];
                        *szFN = 0;
                    }
                    strcat_s(szFN,L, ProperName(name));
                    appendStr(out,JUMPPT(szFN,tk));
                    VDELETE(szFN);
                }
                else
                {
                    if (nameSpace && *nameSpace) {
                        appendStr(out, ProperName(nameSpace));
                        appendChar(out, '.');
                    }

                    appendStr(out, ProperName(name));
                }
                if(g_fDumpTokens)
                {
                    char tmp[16];
                    sprintf_s(tmp,ARRAY_SIZE(tmp),"/*%08X*/",tk);
                    appendStr(out,COMMENT(tmp));
                }
            }
            break;

        case mdtAssemblyRef:
            {
                LPCSTR szName = NULL;
#ifdef __ILDASM__
                if (rAsmRefName && (RidFromToken(tk) <= ulNumAsmRefs))
                {
                    szName = rAsmRefName[RidFromToken(tk)-1];
                }
                else
#endif
                {
                    if (FAILED(pIMDI->GetAssemblyRefProps(tk,NULL,NULL,&szName,NULL,NULL,NULL,NULL)))
                    {
                        szName = NULL;
                    }
                }
                if ((szName != NULL) && ((*szName) != 0 ))
                {
                    appendChar(out, '[');
                    appendStr(out,JUMPPT(ProperName(szName),tk));
                    if(g_fDumpTokens)
                    {
                        char tmp[16];
                        sprintf_s(tmp,ARRAY_SIZE(tmp),"/*%08X*/",tk);
                        appendStr(out,COMMENT(tmp));
                    }
                    appendChar(out, ']');
                }
            }
            break;
        case mdtAssembly:
            {
                LPCSTR szName;
                if (FAILED(pIMDI->GetAssemblyProps(tk,NULL,NULL,NULL,&szName,NULL,NULL)))
                {
                    szName = NULL;
                }
                if ((szName != NULL) && ((*szName) != 0))
                {
                    appendChar(out, '[');
                    appendStr(out,JUMPPT(ProperName(szName),tk));
                    if(g_fDumpTokens)
                    {
                        char tmp[16];
                        sprintf_s(tmp,ARRAY_SIZE(tmp),"/* %08X */",tk);
                        appendStr(out,COMMENT(tmp));
                    }
                    appendChar(out, ']');
                }
            }
            break;
        case mdtModuleRef:
            {
                LPCSTR szName;
                if (FAILED(pIMDI->GetModuleRefProps(tk, &szName)))
                {
                    szName = NULL;
                }
                if ((szName != NULL) && ((*szName) != 0))
                {
                    appendChar(out, '[');
                    appendStr(out,KEYWORD(".module "));
                    appendStr(out,JUMPPT(ProperName(szName),tk));
                    if(g_fDumpTokens)
                    {
                        char tmp[16];
                        sprintf_s(tmp,ARRAY_SIZE(tmp),"/*%08X*/",tk);
                        appendStr(out,COMMENT(tmp));
                    }
                    appendChar(out, ']');
                }
            }
            break;

        case mdtTypeSpec:
            {
#ifdef __ILDASM__
                DWORD ix;
                for(ix = 0; ix < g_NumTypedefs; ix++)
                {
                    if((*g_typedefs)[ix].tkTypeSpec == tk) break;
                }
                if(ix < g_NumTypedefs)
                {
                    appendStr(out,JUMPPT(ProperName((*g_typedefs)[ix].szName),(*g_typedefs)[ix].tkSelf));
                }
                else
#endif
                {
                    ULONG cSig;
                    PCCOR_SIGNATURE sig;
                    if (FAILED(pIMDI->GetSigFromToken(tk, &cSig, &sig)))
                    {
                        char tmp[64];
                        sprintf_s(tmp, ARRAY_SIZE(tmp), "/*Invalid %08X record*/", tk);
                        appendStr(out, COMMENT(tmp));
                    }
                    else
                    {
                        PrettyPrintType(sig, out, pIMDI);
                    }
                }
                if(g_fDumpTokens)
                {
                    char tmp[16];
                    sprintf_s(tmp,ARRAY_SIZE(tmp),"/*%08X*/",tk);
                    appendStr(out,COMMENT(tmp));
                }
            }
            break;

        case mdtModule:
            break;

        default:
            {
                char str[128];
                sprintf_s(str,ARRAY_SIZE(str)," [ERROR: INVALID TOKEN TYPE 0x%8.8X] ",tk);
                appendStr(out,ERRORMSG(str));
            }
    }
    return(asString(out));
}

const char* TrySigUncompressAndDumpSimpleNativeType(
        PCCOR_SIGNATURE pData,              // [IN] compressed data
        ULONG       *pDataOut,              // [OUT] the expanded *pData
        ULONG       &cbCur,
        SString     &buf)
{
    const char* sz = NULL;
    ULONG ulSize = CorSigUncompressData(pData, pDataOut);
    if (ulSize != (ULONG)-1)
    {
        switch (*pDataOut)
        {
            case NATIVE_TYPE_VOID:      sz = " void"; break;
            case NATIVE_TYPE_BOOLEAN:   sz = " bool"; break;
            case NATIVE_TYPE_I1:        sz = " int8"; break;
            case NATIVE_TYPE_U1:        sz = " unsigned int8"; break;
            case NATIVE_TYPE_I2:        sz = " int16"; break;
            case NATIVE_TYPE_U2:        sz = " unsigned int16"; break;
            case NATIVE_TYPE_I4:        sz = " int32"; break;
            case NATIVE_TYPE_U4:        sz = " unsigned int32"; break;
            case NATIVE_TYPE_I8:        sz = " int64"; break;
            case NATIVE_TYPE_U8:        sz = " unsigned int64"; break;
            case NATIVE_TYPE_R4:        sz = " float32"; break;
            case NATIVE_TYPE_R8:        sz = " float64"; break;
            case NATIVE_TYPE_SYSCHAR:   sz = " syschar"; break;
            case NATIVE_TYPE_VARIANT:   sz = " variant"; break;
            case NATIVE_TYPE_CURRENCY:  sz = " currency"; break;
            case NATIVE_TYPE_DECIMAL:   sz = " decimal"; break;
            case NATIVE_TYPE_DATE:      sz = " date"; break;
            case NATIVE_TYPE_BSTR:      sz = " bstr"; break;
            case NATIVE_TYPE_LPSTR:     sz = " lpstr"; break;
            case NATIVE_TYPE_LPWSTR:    sz = " lpwstr"; break;
            case NATIVE_TYPE_LPTSTR:    sz = " lptstr"; break;
            case NATIVE_TYPE_OBJECTREF: sz = " objectref"; break;
            case NATIVE_TYPE_STRUCT:    sz = " struct"; break;
            case NATIVE_TYPE_ERROR:     sz = " error"; break;
            case NATIVE_TYPE_INT:       sz = " int"; break;
            case NATIVE_TYPE_UINT:      sz = " uint"; break;
            case NATIVE_TYPE_NESTEDSTRUCT: sz = " nested struct"; break;
            case NATIVE_TYPE_BYVALSTR:  sz = " byvalstr"; break;
            case NATIVE_TYPE_ANSIBSTR:  sz = " ansi bstr"; break;
            case NATIVE_TYPE_TBSTR:     sz = " tbstr"; break;
            case NATIVE_TYPE_VARIANTBOOL: sz = " variant bool"; break;
            case NATIVE_TYPE_FUNC:      sz = " method"; break;
            case NATIVE_TYPE_ASANY:     sz = " as any"; break;
            case NATIVE_TYPE_LPSTRUCT:  sz = " lpstruct"; break;
            case NATIVE_TYPE_PTR:
            case NATIVE_TYPE_SAFEARRAY:
            case NATIVE_TYPE_ARRAY:
            case NATIVE_TYPE_FIXEDSYSSTRING:
            case NATIVE_TYPE_FIXEDARRAY:
            case NATIVE_TYPE_INTF:
            case NATIVE_TYPE_IUNKNOWN:
            case NATIVE_TYPE_IDISPATCH:
            case NATIVE_TYPE_CUSTOMMARSHALER:
            case NATIVE_TYPE_END:
            case NATIVE_TYPE_MAX:
                sz = ""; break;
            default: sz = NULL;
        }
    }
    if (sz)
        cbCur += ulSize;
    else
        buf.Clear();

    return sz;
}

bool TrySigUncompress(PCCOR_SIGNATURE pData,              // [IN] compressed data
                      ULONG       *pDataOut,              // [OUT] the expanded *pData
                      ULONG       &cbCur,
                      SString     &buf)
{
    ULONG ulSize = CorSigUncompressData(pData, pDataOut);
    if (ulSize == (ULONG)-1)
    {
        buf.Clear();
        return false;
    } else
    {
        cbCur += ulSize;
        return true;
    }
}


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
char* DumpMarshaling(IMDInternalImport* pImport,
                     _Inout_updates_(cchszString) char* szString,
                     DWORD cchszString,
                     mdToken tok)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PCCOR_SIGNATURE pSigNativeType = NULL;
    ULONG           cbNativeType = 0;
    SString         buf;
    if (RidFromToken(tok) &&
        SUCCEEDED(pImport->GetFieldMarshal( // return error if no native type associate with the token
            tok,                // [IN] given fielddef
            &pSigNativeType,    // [OUT] the native type signature
            &cbNativeType)))    // [OUT] the count of bytes of *ppvNativeType
    {
        ULONG cbCur = 0;
        ULONG ulData;
        const char *sz = NULL;
        BOOL  fAddAsterisk = FALSE, fAddBrackets = FALSE;
        buf.AppendPrintf(" %s(", KEYWORD("marshal"));
        while (cbCur < cbNativeType)
        {
            ulData = NATIVE_TYPE_MAX;
            sz = TrySigUncompressAndDumpSimpleNativeType(&pSigNativeType[cbCur], &ulData, cbCur, buf);
            if (!sz)
                goto error;
            if(*sz == 0)
            {
                switch (ulData)
                {
                case NATIVE_TYPE_PTR:
                    sz = "";
                    fAddAsterisk = TRUE;
                    break;
                case NATIVE_TYPE_SAFEARRAY:
                    sz = "";
                    buf.AppendASCII(KEYWORD(" safearray"));
                    ulData = VT_EMPTY;
                    if (cbCur < cbNativeType)
                    {
                        if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                            goto error;
                    }
                    switch(ulData & VT_TYPEMASK)
                    {
                        case VT_EMPTY:      sz=""; break;
                        case VT_NULL:       sz=" null"; break;
                        case VT_VARIANT:    sz=" variant"; break;
                        case VT_CY:         sz=" currency"; break;
                        case VT_VOID:       sz=" void"; break;
                        case VT_BOOL:       sz=" bool"; break;
                        case VT_I1:         sz=" int8"; break;
                        case VT_I2:         sz=" int16"; break;
                        case VT_I4:         sz=" int32"; break;
                        case VT_I8:         sz=" int64"; break;
                        case VT_R4:         sz=" float32"; break;
                        case VT_R8:         sz=" float64"; break;
                        case VT_UI1:        sz=" unsigned int8"; break;
                        case VT_UI2:        sz=" unsigned int16"; break;
                        case VT_UI4:        sz=" unsigned int32"; break;
                        case VT_UI8:        sz=" unsigned int64"; break;
                        case VT_PTR:        sz=" *"; break;
                        case VT_DECIMAL:    sz=" decimal"; break;
                        case VT_DATE:       sz=" date"; break;
                        case VT_BSTR:       sz=" bstr"; break;
                        case VT_LPSTR:      sz=" lpstr"; break;
                        case VT_LPWSTR:     sz=" lpwstr"; break;
                        case VT_UNKNOWN:    sz=" iunknown"; break;
                        case VT_DISPATCH:   sz=" idispatch"; break;
                        case VT_SAFEARRAY:  sz=" safearray"; break;
                        case VT_INT:        sz=" int"; break;
                        case VT_UINT:       sz=" unsigned int"; break;
                        case VT_ERROR:      sz=" error"; break;
                        case VT_HRESULT:    sz=" hresult"; break;
                        case VT_CARRAY:     sz=" carray"; break;
                        case VT_USERDEFINED:    sz=" userdefined"; break;
                        case VT_RECORD:     sz=" record"; break;
                        case VT_FILETIME:   sz=" filetime"; break;
                        case VT_BLOB:       sz=" blob"; break;
                        case VT_STREAM:     sz=" stream"; break;
                        case VT_STORAGE:    sz=" storage"; break;
                        case VT_STREAMED_OBJECT:    sz=" streamed_object"; break;
                        case VT_STORED_OBJECT:      sz=" stored_object"; break;
                        case VT_BLOB_OBJECT:        sz=" blob_object"; break;
                        case VT_CF:         sz=" cf"; break;
                        case VT_CLSID:      sz=" clsid"; break;
                        default:            sz=NULL; break;
                    }
                    if(sz) buf.AppendASCII(KEYWORD(sz));
                    else
                    {
                        // buf.AppendPrintf(ERRORMSG(" [ILLEGAL VARIANT TYPE 0x%X]"),ulData & VT_TYPEMASK);
                        buf.Clear();
                        goto error;
                    }
                    sz="";
                    switch(ulData & (~VT_TYPEMASK))
                    {
                        case VT_ARRAY: sz = "[]"; break;
                        case VT_VECTOR: sz = " vector"; break;
                        case VT_BYREF: sz = "&"; break;
                        case VT_BYREF|VT_ARRAY: sz = "&[]"; break;
                        case VT_BYREF|VT_VECTOR: sz = "& vector"; break;
                        case VT_ARRAY|VT_VECTOR: sz = "[] vector"; break;
                        case VT_BYREF|VT_ARRAY|VT_VECTOR: sz = "&[] vector"; break;
                    }
                    buf.AppendASCII(KEYWORD(sz));
                    sz="";

                    // Extract the user defined sub type name.
                    if (cbCur < cbNativeType)
                    {
                        LPUTF8 strTemp = NULL;
                        int strLen = 0;
                        int ByteCountLength = 0;
                        strLen = GetLength(&pSigNativeType[cbCur], &ByteCountLength);
                        cbCur += ByteCountLength;
                        if(strLen)
                        {
#ifdef _PREFAST_
#pragma prefast(push)
#pragma prefast(disable:22009 "Suppress PREFAST warnings about integer overflow")
#endif
                            strTemp = (LPUTF8)_alloca(strLen + 1);
                            memcpy(strTemp, (LPUTF8)&pSigNativeType[cbCur], strLen);
                            strTemp[strLen] = 0;
                            buf.AppendPrintf(", \"%s\"", UnquotedProperName(strTemp));
                            cbCur += strLen;
#ifdef _PREFAST_
#pragma prefast(pop)
#endif
                        }
                    }
                    break;

                case NATIVE_TYPE_ARRAY:
                    sz = "";
                    fAddBrackets = TRUE;
                    break;
                case NATIVE_TYPE_FIXEDSYSSTRING:
                    {
                        sz = "";
                        buf.AppendASCII(KEYWORD(" fixed sysstring"));
                        buf.AppendASCII(" [");
                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                                goto error;
                            buf.AppendPrintf("%d",ulData);
                        }
                        buf.AppendASCII("]");
                    }
                    break;
                case NATIVE_TYPE_FIXEDARRAY:
                    {
                        sz = "";
                        buf.AppendASCII(KEYWORD(" fixed array"));
                        buf.AppendASCII(" [");
                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                                goto error;
                            buf.AppendPrintf("%d",ulData);
                        }
                        buf.AppendASCII("]");
                        if (cbCur < cbNativeType)
                        {
                            sz = TrySigUncompressAndDumpSimpleNativeType(&pSigNativeType[cbCur], &ulData, cbCur, buf);
                            if (!sz)
                                goto error;
                        }
                    }
                    break;

                case NATIVE_TYPE_INTF:
                        buf.AppendASCII(KEYWORD(" interface"));
                        goto DumpIidParamIndex;
                case NATIVE_TYPE_IUNKNOWN:
                        buf.AppendASCII(KEYWORD(" iunknown"));
                        goto DumpIidParamIndex;
                case NATIVE_TYPE_IDISPATCH:
                         buf.AppendASCII(KEYWORD(" idispatch"));
                     DumpIidParamIndex:
                         sz = " ";
                         if (cbCur < cbNativeType)
                         {
                             if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                                 goto error;
                             buf.AppendPrintf("(%s = %d)",KEYWORD("iidparam"),ulData);
                         }
                         break;

                case NATIVE_TYPE_CUSTOMMARSHALER:
                    {
                        LPUTF8 strTemp = NULL;
                        int strLen = 0;
                        int ByteCountLength = 0;
                        BOOL fFourStrings = FALSE;

                        sz = "";
                        buf.AppendASCII(KEYWORD(" custom"));
                        buf.AppendASCII(" (");
                        // Extract the typelib GUID.
                        strLen = GetLength(&pSigNativeType[cbCur], &ByteCountLength);
                        cbCur += ByteCountLength;
                        if(strLen)
                        {
                            fFourStrings = TRUE;
                            strTemp = (LPUTF8)(new char[strLen + 1]);
                            if(strTemp)
                            {
                                memcpy(strTemp, (LPUTF8)&pSigNativeType[cbCur], strLen);
                                strTemp[strLen] = 0;
                                buf.AppendPrintf("\"%s\",",UnquotedProperName(strTemp));
                                cbCur += strLen;
                                VDELETE(strTemp);
                            }
                        }
                        if(cbCur >= cbNativeType)
                        {
                            // buf.AppendASCII(ERRORMSG("/* INCOMPLETE MARSHALER INFO */"));
                            buf.Clear();
                            goto error;
                        }
                        else
                        {
                            //_ASSERTE(cbCur < cbNativeType);

                            // Extract the name of the native type.
                            strLen = GetLength(&pSigNativeType[cbCur], &ByteCountLength);
                            cbCur += ByteCountLength;
                            if(fFourStrings)
                            {
                                if(strLen)
                                {
                                    strTemp = (LPUTF8)(new char[strLen + 1]);
                                    if(strTemp)
                                    {
                                        memcpy(strTemp, (LPUTF8)&pSigNativeType[cbCur], strLen);
                                        strTemp[strLen] = 0;
                                        buf.AppendPrintf("\"%s\",",UnquotedProperName(strTemp));
                                        cbCur += strLen;
                                        VDELETE(strTemp);
                                    }
                                }
                                else buf.AppendASCII("\"\",");
                            }
                            if(cbCur >= cbNativeType)
                            {
                                // buf.AppendASCII(ERRORMSG("/* INCOMPLETE MARSHALER INFO */"));
                                buf.Clear();
                                goto error;
                            }
                            else
                            {
                                //_ASSERTE(cbCur < cbNativeType);

                                // Extract the name of the custom marshaler.
                                strLen = GetLength(&pSigNativeType[cbCur], &ByteCountLength);
                                cbCur += ByteCountLength;
                                if(strLen)
                                {
                                    strTemp = (LPUTF8)(new char[strLen + 1]);
                                    if(strTemp)
                                    {
                                        memcpy(strTemp, (LPUTF8)&pSigNativeType[cbCur], strLen);
                                        strTemp[strLen] = 0;
                                        buf.AppendPrintf("\"%s\",",UnquotedProperName(strTemp));
                                        cbCur += strLen;
                                        VDELETE(strTemp);
                                    }
                                }
                                else buf.AppendASCII("\"\",");
                                if(cbCur >= cbNativeType)
                                {
                                    // buf.AppendASCII(ERRORMSG("/* INCOMPLETE MARSHALER INFO */"));
                                    buf.Clear();
                                    goto error;
                                }
                                else
                                {
                                    // Extract the cookie string.
                                    strLen = GetLength(&pSigNativeType[cbCur], &ByteCountLength);
                                    cbCur += ByteCountLength;

                                    if(cbCur+strLen > cbNativeType)
                                    {
                                        // buf.AppendASCII(ERRORMSG("/* INCOMPLETE MARSHALER INFO */"));
                                        buf.Clear();
                                        goto error;
                                    }
                                    else
                                    {
                                        if(strLen)
                                        {
                                            strTemp = (LPUTF8)(new (nothrow) char[strLen + 1]);
                                            if(strTemp)
                                            {
                                                memcpy(strTemp, (LPUTF8)&pSigNativeType[cbCur], strLen);
                                                strTemp[strLen] = 0;

                                                buf.AppendASCII("\"");
                                                // Copy the cookie string and transform the embedded nulls into \0's.
                                                for (int i = 0; i < strLen - 1; i++, cbCur++)
                                                {
                                                    if (strTemp[i] == 0)
                                                        buf.AppendASCII("\\0");
                                                    else
                                                    {
                                                        buf.AppendPrintf("%c", strTemp[i]);
                                                    }
                                                }
                                                buf.AppendPrintf("%c\"", strTemp[strLen - 1]);
                                                cbCur++;
                                                VDELETE(strTemp);
                                            }
                                        }
                                        else
                                            buf.AppendASCII("\"\"");
                                        //_ASSERTE(cbCur <= cbNativeType);
                                    }
                                }
                            }
                        }
                        buf.AppendASCII(")");
                    }
                    break;
                default:
                    {
                        sz = "";
                    }
                } // end switch
            }
            if(*sz)
            {
                buf.AppendASCII(KEYWORD(sz));
                if(fAddAsterisk)
                {
                    buf.AppendASCII("*");
                    fAddAsterisk = FALSE;
                }
                if(fAddBrackets)
                {
                    ULONG ulSizeParam=(ULONG)-1,ulSizeConst=(ULONG)-1;
                    buf.AppendASCII("[");
                    fAddBrackets = FALSE;
                    if (cbCur < cbNativeType)
                    {
                        if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                            goto error;
                        ulSizeParam = ulData;
                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                                goto error;
                            ulSizeConst = ulData;
                            if (cbCur < cbNativeType)
                            {
                                // retrieve flags
                                if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                                    goto error;
                                if((ulData & 1) == 0) ulSizeParam = 0xFFFFFFFF;
                            }
                        }
                    }
                    if(ulSizeConst != 0xFFFFFFFF)
                    {
                        buf.AppendPrintf("%d",ulSizeConst);
                        // if(ulSizeParam == 0) ulSizeParam = 0xFFFFFFFF; // don't need +0
                    }
                    if(ulSizeParam != 0xFFFFFFFF)
                    {
                        buf.AppendPrintf(" + %d",ulSizeParam);
                    }
                    buf.AppendASCII("]");
                 }

            }

            if (ulData >= NATIVE_TYPE_MAX)
                break;
        } // end while (cbCur < cbNativeType)
        // still can have outstanding asterisk or brackets
        if(fAddAsterisk)
        {
            buf.AppendASCII("*");
            fAddAsterisk = FALSE;
        }
        if(fAddBrackets)
        {
            ULONG ulSizeParam=(ULONG)-1,ulSizeConst=(ULONG)-1;
            buf.AppendASCII("[");
            fAddBrackets = FALSE;
            if (cbCur < cbNativeType)
            {
                if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                    goto error;
                ulSizeParam = ulData;
                if (cbCur < cbNativeType)
                {
                    if (!TrySigUncompress(&pSigNativeType[cbCur], &ulData, cbCur, buf))
                        goto error;
                    ulSizeConst = ulData;
                }
            }
            if(ulSizeConst != 0xFFFFFFFF)
            {
                buf.AppendPrintf("%d",ulSizeConst);
                // if(ulSizeParam == 0) ulSizeParam = 0xFFFFFFFF; // don't need +0
            }
            if(ulSizeParam != 0xFFFFFFFF)
            {
                buf.AppendPrintf(" + %d",ulSizeParam);
            }
            buf.AppendASCII("]");
        }
        buf.AppendASCII(") ");
    }// end if(SUCCEEDED
error:
    if (buf.IsEmpty() && cbNativeType != 0)
    {
        // There was something that we didn't grok in the signature.
        // Just dump out the blob as hex
        buf.AppendPrintf(" %s({", KEYWORD("marshal"));
        while (cbNativeType--)
            buf.AppendPrintf(" %2.2X", *pSigNativeType++);
        buf.AppendASCII(" }) ");

        char * tgt = szString + strlen(szString);
        int sprintf_ret = sprintf_s(tgt, cchszString - (tgt - szString), "%S", buf.GetUnicode());
        if (sprintf_ret == -1)
        {
            // Hit an error. Oh well, nothing to do...
            return tgt;
        }
        else
        {
            return tgt + sprintf_ret;
        }
    }
    else
    {
        char * tgt = szString + strlen(szString);
        int sprintf_ret = sprintf_s(tgt, cchszString - (tgt - szString), "%S", buf.GetUnicode());
        if (sprintf_ret == -1)
        {
            // There was an error, possibly with converting the Unicode characters.
            buf.Clear();
            if (cbNativeType != 0)
                goto error;
            return tgt; // Oh well, nothing to do...
        }
        else
        {
            return tgt + sprintf_ret;
        }
    }
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

char* DumpParamAttr(_Inout_updates_(cchszString) char* szString, DWORD cchszString, DWORD dwAttr)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    char *szptr = &szString[strlen(szString)];
    char *was_szptr = szptr;
    if(IsPdIn(dwAttr))
    {
        szptr+=sprintf_s(szptr,cchszString - (szptr - was_szptr), KEYWORD("[in]"));
    }
    if(IsPdOut(dwAttr))
    {
        szptr+=sprintf_s(szptr,cchszString - (szptr - was_szptr),KEYWORD("[out]"));
    }
    if(IsPdOptional(dwAttr))
    {
        szptr+=sprintf_s(szptr,cchszString - (szptr - was_szptr),KEYWORD("[opt]"));
    }
    if(szptr != was_szptr)
    {
        szptr+=sprintf_s(szptr,cchszString - (szptr - was_szptr)," ");
    }
    return szptr;
}
