// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
//
// disasm.cpp : Defines the entry point for the console application.
//
#ifndef FEATURE_PAL
#include <tchar.h>
#endif
#include "strike.h"
#include "util.h"
#include "strsafe.h"
//#ifndef FEATURE_PAL
//#include "gcinfo.h"
//#endif
#include "disasm.h"
#include <dbghelp.h>

#include "corhdr.h"

#include "cor.h"
#include "dacprivate.h"

#include "openum.h"

#include "sos_md.h"

#define SOS_INCLUDE 1
#include "corhlpr.h"
#include "corhlpr.cpp"

//////////////////////////////////////////////////////////////////////////////////////////////////////////
#undef printf
#define printf ExtOut

// typedef unsigned char BYTE;
struct OpCode
{
    int code;
    const char *name;
    int args;
    BYTE b1;
    BYTE b2;

    unsigned int getCode() { 
        if (b1==0xFF) return b2;
        else return (0xFE00 | b2);
    }
};

#define OPCODES_LENGTH 0x122

#undef OPDEF
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) {c, s, args, s1, s2},
static OpCode opcodes[] =
{
#include "opcode.def"
};

static ULONG position = 0;
static BYTE *pBuffer = NULL;

// The UNALIGNED is because on IA64 alignment rules would prevent
// us from reading a pointer from an unaligned source.
template <typename T>
T readData ( ) {
    T val = *((T UNALIGNED*)(pBuffer+position));
    position += sizeof(T);
    return val;
}

unsigned int readOpcode()
{
    unsigned int c = readData<BYTE>();
    if (c == 0xFE)
    {
        c = readData<BYTE>();
        c |= 0x100;
    }
    return c;
}

void DisassembleToken(IMetaDataImport *i,
                      DWORD token)
{
    HRESULT hr;

    switch (TypeFromToken(token))
    {
    default:
        printf("<unknown token type %08x>", TypeFromToken(token));
        break;

    case mdtTypeDef:
        {
            ULONG cLen;
            WCHAR szName[50];

            hr = i->GetTypeDefProps(token, szName, 49, &cLen, NULL, NULL);

            if (FAILED(hr))
                StringCchCopyW(szName, COUNTOF(szName), W("<unknown type def>"));

            printf("%S", szName);
        }
        break;

    case mdtTypeRef:
        {
            ULONG cLen;
            WCHAR szName[50];

            hr = i->GetTypeRefProps(token, NULL, szName, 49, &cLen);

            if (FAILED(hr))
                StringCchCopyW(szName, COUNTOF(szName), W("<unknown type ref>"));

            printf("%S", szName);
        }
        break;

    case mdtFieldDef:
        {
            ULONG cLen;
            WCHAR szFieldName[50];
            WCHAR szClassName[50];
            mdTypeDef mdClass;

            hr = i->GetFieldProps(token, &mdClass, szFieldName, 49, &cLen,
                                  NULL, NULL, NULL, NULL, NULL, NULL);

            if (FAILED(hr))
                StringCchCopyW(szFieldName, COUNTOF(szFieldName), W("<unknown field def>"));

            hr = i->GetTypeDefProps(mdClass, szClassName, 49, &cLen,
                                    NULL, NULL);

            if (FAILED(hr))
                StringCchCopyW(szClassName, COUNTOF(szClassName), W("<unknown type def>"));

            printf("%S::%S", szClassName, szFieldName);
        }
        break;

    case mdtMethodDef:
        {
            ULONG cLen;
            WCHAR szFieldName[50];
            WCHAR szClassName[50];
            mdTypeDef mdClass;

            hr = i->GetMethodProps(token, &mdClass, szFieldName, 49, &cLen,
                                   NULL, NULL, NULL, NULL, NULL);

            if (FAILED(hr))
                StringCchCopyW(szFieldName, COUNTOF(szFieldName), W("<unknown method def>"));

            hr = i->GetTypeDefProps(mdClass, szClassName, 49, &cLen,
                                    NULL, NULL);

            if (FAILED(hr))
                StringCchCopyW(szClassName, COUNTOF(szClassName), W("<unknown type def>"));

            printf("%S::%S", szClassName, szFieldName);
        }
        break;

    case mdtMemberRef:
        {
            mdTypeRef cr = mdTypeRefNil;
            LPCWSTR pMemberName;
            WCHAR memberName[50];
            ULONG memberNameLen;

            hr = i->GetMemberRefProps(token, &cr, memberName, 49,
                                      &memberNameLen, NULL, NULL);

            if (FAILED(hr))
            {
                pMemberName = W("<unknown member ref>");
            }
            else
                pMemberName = memberName;

            ULONG cLen;
            WCHAR szName[50];

            if(TypeFromToken(cr) == mdtTypeRef)
            {
                if (FAILED(i->GetTypeRefProps(cr, NULL, szName, 50, &cLen)))
                {
                    StringCchCopyW(szName, COUNTOF(szName), W("<unknown type ref>"));
                }
            }
            else if(TypeFromToken(cr) == mdtTypeDef)
            {
                if (FAILED(i->GetTypeDefProps(cr, szName, 49, &cLen,
                                              NULL, NULL)))
                {
                    StringCchCopyW(szName, COUNTOF(szName), W("<unknown type def>"));
                }
            }
            else if(TypeFromToken(cr) == mdtTypeSpec)
            {
                IMDInternalImport *pIMDI = NULL;
                if (SUCCEEDED(GetMDInternalFromImport(i, &pIMDI)))
                {
                    CQuickBytes out;
                    ULONG cSig;
                    PCCOR_SIGNATURE sig;
                    if (FAILED(pIMDI->GetSigFromToken(cr, &cSig, &sig)))
                    {
                        StringCchCopyW(szName, COUNTOF(szName), W("<Invalid record>"));
                    }
                    else
                    {
                        PrettyPrintType(sig, &out, pIMDI);
                        MultiByteToWideChar (CP_ACP, 0, asString(&out), -1, szName, 50);
                    }

                    pIMDI->Release();
                }
                else
                {
                    StringCchCopyW(szName, COUNTOF(szName), W("<unknown type spec>"));
                }
            }
            else
            {
                StringCchCopyW(szName, COUNTOF(szName), W("<unknown type token>"));
            }
            
            printf("%S::%S ", szName, pMemberName);
        }
        break;
    }
}

ULONG GetILSize(DWORD_PTR ilAddr)
{
    ULONG uRet = 0;

    // workaround: read enough bytes at ilAddr to presumably get the entire header.
    // Could be error prone.

    static BYTE headerArray[1024];
    HRESULT Status = g_ExtData->ReadVirtual(TO_CDADDR(ilAddr), headerArray, sizeof(headerArray), NULL);    
    if (SUCCEEDED(Status))
    {            
        COR_ILMETHOD_DECODER header((COR_ILMETHOD *)headerArray);
        // uRet = header.GetHeaderSize();
        uRet = header.GetOnDiskSize((COR_ILMETHOD *)headerArray);
    }

    return uRet;
}
  
HRESULT DecodeILFromAddress(IMetaDataImport *pImport, TADDR ilAddr)
{
    HRESULT Status = S_OK;

    ULONG Size = GetILSize(ilAddr);
    if (Size == 0)
    {
        ExtOut("error decoding IL\n");
        return Status;
    }

    ExtOut("ilAddr = %p\n", SOS_PTR(ilAddr));

    // Read the memory into a local buffer
    ArrayHolder<BYTE> pArray = new BYTE[Size];
    Status = g_ExtData->ReadVirtual(TO_CDADDR(ilAddr), pArray, Size, NULL);
    if (Status != S_OK)
    {
        ExtOut("Failed to read memory\n");
        return Status;
    }

    DecodeIL(pImport, pArray, Size);

    return Status;
}
            
void DecodeIL(IMetaDataImport *pImport, BYTE *buffer, ULONG bufSize)
{
    // First decode the header
    COR_ILMETHOD *pHeader = (COR_ILMETHOD *) buffer;    
    COR_ILMETHOD_DECODER header(pHeader);    

    // Set globals
    position = 0;	
    pBuffer = (BYTE *) header.Code;

    UINT indentCount = 0;
    ULONG endCodePosition = header.GetCodeSize();
    while(position < endCodePosition)
    {	
        for (unsigned e=0;e<header.EHCount();e++)
        {
            IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehBuff;
            const IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;
            
            ehInfo = header.EH->EHClause(e,&ehBuff);
            if (ehInfo->TryOffset == position)
            {
                printf ("%*s.try\n%*s{\n", indentCount, "", indentCount, "");
                indentCount+=2;
            }
            else if ((ehInfo->TryOffset + ehInfo->TryLength) == position)
            {
                indentCount-=2;
                printf("%*s} // end .try\n", indentCount, "");
            }

            if (ehInfo->HandlerOffset == position)
            {
                if (ehInfo->Flags == COR_ILEXCEPTION_CLAUSE_FINALLY)
                    printf("%*s.finally\n%*s{\n", indentCount, "", indentCount, "");
                else
                    printf("%*s.catch\n%*s{\n", indentCount, "", indentCount, "");

                indentCount+=2;
            }
            else if ((ehInfo->HandlerOffset + ehInfo->HandlerLength) == position)
            {
                indentCount-=2;
                
                if (ehInfo->Flags == COR_ILEXCEPTION_CLAUSE_FINALLY)
                    printf("%*s} // end .finally\n", indentCount, "");
                else
                    printf("%*s} // end .catch\n", indentCount, "");
            }
        }        
        
        printf("%*sIL_%04x: ", indentCount, "", position);
        unsigned int c = readOpcode();
        OpCode opcode = opcodes[c];
        printf("%s ", opcode.name);

        switch(opcode.args)
        {
        case InlineNone: break;
        
        case ShortInlineVar:
            printf("VAR OR ARG %d",readData<BYTE>()); break;
        case InlineVar:
            printf("VAR OR ARG %d",readData<WORD>()); break;
        case InlineI:
            printf("%d",readData<LONG>());
            break;
        case InlineR:
            printf("%f",readData<double>());
            break;
        case InlineBrTarget:
            printf("IL_%04x",readData<LONG>() + position); break;
        case ShortInlineBrTarget:
            printf("IL_%04x",readData<BYTE>()  + position); break;
        case InlineI8:
            printf("%ld", readData<__int64>()); break;
            
        case InlineMethod:
        case InlineField:
        case InlineType:
        case InlineTok:
        case InlineSig:        
        {
            LONG l = readData<LONG>();
            if (pImport != NULL)
            {
                DisassembleToken(pImport, l);
            }
            else
            {
                printf("TOKEN %x", l); 
            }
            break;
        }
            
        case InlineString:
        {
            LONG l = readData<LONG>();

            ULONG numChars;
            WCHAR str[84];

            if ((pImport != NULL) && (pImport->GetUserString((mdString) l, str, 80, &numChars) == S_OK))
            {
                if (numChars < 80)
                    str[numChars] = 0;
                wcscpy_s(&str[79], 4, W("..."));
                WCHAR* ptr = str;
                while(*ptr != 0) {
                    if (*ptr < 0x20 || * ptr >= 0x80) {
                        *ptr = '.';
                    }
                    ptr++;
                }

                printf("\"%S\"", str);
            }
            else
            {
                printf("STRING %x", l); 
            }
            break;
        }
            
        case InlineSwitch:
        {
            LONG cases = readData<LONG>();
            LONG *pArray = new LONG[cases];
            LONG i=0;
            for(i=0;i<cases;i++)
            {
                pArray[i] = readData<LONG>();
            }
            printf("(");
            for(i=0;i<cases;i++)
            {
                if (i != 0)
                    printf(", ");
                printf("IL_%04x",pArray[i] + position);
            }
            printf(")");
            delete [] pArray;
            break;
        }
        case ShortInlineI:
            printf("%d", readData<BYTE>()); break;
        case ShortInlineR:		
            printf("%f", readData<float>()); break;
        default: printf("Error, unexpected opcode type\n"); break;
        }

        printf("\n");
    }
}

DWORD_PTR GetObj(DacpObjectData& tokenArray, UINT item)
{
    if (item < tokenArray.dwNumComponents)
    {
        DWORD_PTR dwAddr = (DWORD_PTR) (tokenArray.ArrayDataPtr + tokenArray.dwComponentSize*item);
        DWORD_PTR objPtr;
        if (SUCCEEDED(MOVE(objPtr, dwAddr)))
        {
            return objPtr;
        }
    }
    return NULL;
}


void DisassembleToken(DacpObjectData& tokenArray,
                      DWORD token)
{    
    switch (TypeFromToken(token))
    {
    default:
        printf("<unknown token type (token=%08x)>", token);
        break;

    case mdtTypeDef:
        {
            DWORD_PTR runtimeTypeHandle = GetObj(tokenArray, RidFromToken(token));

            DWORD_PTR runtimeType = NULL;
            MOVE(runtimeType, runtimeTypeHandle + sizeof(DWORD_PTR));

            int offset = GetObjFieldOffset(runtimeType, W("m_handle"));

            DWORD_PTR methodTable = NULL;
            MOVE(methodTable, runtimeType + offset);

            if (NameForMT_s(methodTable, g_mdName,mdNameLen))
            {
                printf("%x \"%S\"", token, g_mdName);
            }
            else
            {
                printf("<invalid MethodTable>");
            }
        }
        break;

    case mdtSignature:
    case mdtTypeRef:
        {
            printf ("%x (%p)", token, SOS_PTR(GetObj(tokenArray, RidFromToken(token))));
        }
        break;

    case mdtFieldDef:
        {
            printf ("%x (%p)", token, SOS_PTR(GetObj(tokenArray, RidFromToken(token))));
        }
        break;

    case mdtMethodDef:
        {
            CLRDATA_ADDRESS runtimeMethodHandle = GetObj(tokenArray, RidFromToken(token));            
            int offset = GetObjFieldOffset(runtimeMethodHandle, W("m_value"));

            TADDR runtimeMethodInfo = NULL;
            MOVE(runtimeMethodInfo, runtimeMethodHandle+offset);

            offset = GetObjFieldOffset(runtimeMethodInfo, W("m_handle"));

            TADDR methodDesc = NULL;
            MOVE(methodDesc, runtimeMethodInfo+offset);

            NameForMD_s((DWORD_PTR)methodDesc, g_mdName, mdNameLen);
            printf ("%x %S", token, g_mdName);
        }
        break;

    case mdtMemberRef:
        {
            printf ("%x (%p)", token, SOS_PTR(GetObj(tokenArray, RidFromToken(token))));
        }
        break;
    case mdtString:
        {
            DWORD_PTR strObj = GetObj(tokenArray, RidFromToken(token));
            printf ("%x \"", token);
            StringObjectContent (strObj, FALSE, 40);
            printf ("\"");
        }
        break;
    }
}

// Similar to the function above. TODO: factor them together before checkin.
void DecodeDynamicIL(BYTE *data, ULONG Size, DacpObjectData& tokenArray)
{
    // There is no header for this dynamic guy.
    // Set globals
    position = 0;	
    pBuffer = data;

    // At this time no exception information will be displayed (fix soon)
    UINT indentCount = 0;
    ULONG endCodePosition = Size;
    while(position < endCodePosition)
    {	        
        printf("%*sIL_%04x: ", indentCount, "", position);
        unsigned int c = readOpcode();
        OpCode opcode = opcodes[c];
        printf("%s ", opcode.name);

        switch(opcode.args)
        {
        case InlineNone: break;
        
        case ShortInlineVar:
            printf("VAR OR ARG %d",readData<BYTE>()); break;
        case InlineVar:
            printf("VAR OR ARG %d",readData<WORD>()); break;
        case InlineI:
            printf("%d",readData<LONG>());
            break;
        case InlineR:
            printf("%f",readData<double>());
            break;
        case InlineBrTarget:
            printf("IL_%04x",readData<LONG>() + position); break;
        case ShortInlineBrTarget:
            printf("IL_%04x",readData<BYTE>()  + position); break;
        case InlineI8:
            printf("%ld", readData<__int64>()); break;
            
        case InlineMethod:
        case InlineField:
        case InlineType:
        case InlineTok:
        case InlineSig:        
        case InlineString:            
        {
            LONG l = readData<LONG>();
            DisassembleToken(tokenArray, l);            
            break;
        }
                        
        case InlineSwitch:
        {
            LONG cases = readData<LONG>();
            LONG *pArray = new LONG[cases];
            LONG i=0;
            for(i=0;i<cases;i++)
            {
                pArray[i] = readData<LONG>();
            }
            printf("(");
            for(i=0;i<cases;i++)
            {
                if (i != 0)
                    printf(", ");
                printf("IL_%04x",pArray[i] + position);
            }
            printf(")");
            delete [] pArray;
            break;
        }
        case ShortInlineI:
            printf("%d", readData<BYTE>()); break;
        case ShortInlineR:		
            printf("%f", readData<float>()); break;
        default: printf("Error, unexpected opcode type\n"); break;
        }

        printf("\n");
    }
}



/******************************************************************************/
// CQuickBytes utilities
static char* asString(CQuickBytes *out) {
    SIZE_T oldSize = out->Size();
    out->ReSize(oldSize + 1);
    char* cur = &((char*) out->Ptr())[oldSize]; 
    *cur = 0;   
    out->ReSize(oldSize);   		// Don't count the null character
    return((char*) out->Ptr()); 
}

static void appendStr(CQuickBytes *out, const char* str, unsigned len=-1) {
    if(len == (unsigned)(-1)) len = (unsigned)strlen(str); 
    SIZE_T oldSize = out->Size();
    out->ReSize(oldSize + len);
    char* cur = &((char*) out->Ptr())[oldSize]; 
    memcpy(cur, str, len);  
        // Note no trailing null!   
}

static void appendChar(CQuickBytes *out, char chr) {
    SIZE_T oldSize = out->Size();
    out->ReSize(oldSize + 1); 
    ((char*) out->Ptr())[oldSize] = chr; 
        // Note no trailing null!   
}

static void insertStr(CQuickBytes *out, const char* str) {
    unsigned len = (unsigned)strlen(str); 
    SIZE_T oldSize = out->Size();
    out->ReSize(oldSize + len); 
    char* cur = &((char*) out->Ptr())[len];
    memmove(cur,out->Ptr(),oldSize);
    memcpy(out->Ptr(), str, len);  
        // Note no trailing null!   
}

static void appendStrNum(CQuickBytes *out, int num) {
    char buff[16];  
    sprintf_s(buff, COUNTOF(buff), "%d", num);   
    appendStr(out, buff);   
}


//PrettyPrinting type names
PCCOR_SIGNATURE PrettyPrintType(
    PCCOR_SIGNATURE typePtr,            // type to convert,     
    CQuickBytes *out,                   // where to put the pretty printed string   
    IMDInternalImport *pIMDI,           // ptr to IMDInternal class with ComSig
    DWORD formatFlags /*= formatILDasm*/)
{
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
                appendStr(out, (char*)str);
                break;  

            case ELEMENT_TYPE_VALUETYPE    :   
                if ((formatFlags & FormatKwInNames) != 0) 
                    str = "valuetype ";   
                else str = "";
                goto DO_CLASS;  
            case ELEMENT_TYPE_CLASS         :   
                if ((formatFlags & FormatKwInNames) != 0) 
                    str = "class "; 
                else str = "";
                goto DO_CLASS;  

            DO_CLASS:
                appendStr(out, (char*)str);
                typePtr += CorSigUncompressToken(typePtr, &tk); 
                if(IsNilToken(tk))
                {
                    appendStr(out, "[ERROR! NIL TOKEN]");
                }
                else PrettyPrintClass(out, tk, pIMDI, formatFlags);
                break;  

            case ELEMENT_TYPE_SZARRAY    :   
                insertStr(&Appendix,"[]");
                Reiterate = TRUE;
                break;
            
            case ELEMENT_TYPE_ARRAY       :   
                {   
                typePtr = PrettyPrintType(typePtr, out, pIMDI, formatFlags);
                unsigned rank = CorSigUncompressData(typePtr);  
                    // <TODO> what is the syntax for the rank 0 case? </TODO> 
                if (rank == 0) {
                    appendStr(out, "[BAD: RANK == 0!]");
                }
                else {
                    _ASSERTE(rank != 0);
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22009) // "Suppress PREfast warnings about integer overflow"
// PREFAST warns about using _alloca in a loop.  However when we're in this switch case we do NOT
// set Reiterate to true, so we only execute through the loop once!
#pragma warning(disable:6263) // "Suppress PREfast warnings about stack overflow due to _alloca in a loop."
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
                                if (i < numSizes && lowerBounds[i] == 0)
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
#pragma warning(pop)
#endif
                }
                } break;    

            case ELEMENT_TYPE_VAR        :   
                appendChar(out, '!');
                n  = CorSigUncompressData(typePtr);
                appendStrNum(out, n);
                break;

            case ELEMENT_TYPE_MVAR        :   
                appendChar(out, '!');    
                appendChar(out, '!');    
                n  = CorSigUncompressData(typePtr);
                appendStrNum(out, n);
                break;

            case ELEMENT_TYPE_FNPTR :   
                appendStr(out, "method ");  
                appendStr(out, "METHOD"); // was: typePtr = PrettyPrintSignature(typePtr, 0x7FFF, "*", out, pIMDI, NULL);
                break;

            case ELEMENT_TYPE_GENERICINST :
            {
              typePtr = PrettyPrintType(typePtr, out, pIMDI, formatFlags);
              if ((formatFlags & FormatSignature) == 0)
                  break;

              if ((formatFlags & FormatAngleBrackets) != 0)
                  appendStr(out, "<");
              else
                  appendStr(out,"[");
              unsigned numArgs = CorSigUncompressData(typePtr);    
              bool needComma = false;
              while(numArgs--)
              {
                  if (needComma)
                      appendChar(out, ',');
                  typePtr = PrettyPrintType(typePtr, out, pIMDI, formatFlags);
                  needComma = true;
              }
              if ((formatFlags & FormatAngleBrackets) != 0)
                  appendStr(out, ">");
              else
                  appendStr(out,"]");
              break;
            }

            case ELEMENT_TYPE_PINNED	:
                str = " pinned"; goto MODIFIER;
            case ELEMENT_TYPE_PTR           :
                str = "*"; goto MODIFIER;
            case ELEMENT_TYPE_BYREF         :
                str = "&"; goto MODIFIER;
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
                    sprintf_s(sz,COUNTOF(sz),"/* UNKNOWN TYPE (0x%X)*/",typ);
                    appendStr(out, sz);
                }
                break;  
        } // end switch
    } while(Reiterate);
    if (Appendix.Size() > 0)
        appendStr(out,asString(&Appendix));

    return(typePtr);    
}

// Protection against null names, used by ILDASM/SOS
const char *const szStdNamePrefix[] = {"MO","TR","TD","","FD","","MD","","PA","II","MR","","CA","","PE","","","SG","","","EV",
"","","PR","","","MOR","TS","","","","","AS","","","AR","","","FL","ET","MAR"};

#define MAKE_NAME_IF_NONE(psz, tk) { if(!(psz && *psz)) { char* sz = (char*)_alloca(16); \
sprintf_s(sz,16,"$%s$%X",szStdNamePrefix[tk>>24],tk&0x00FFFFFF); psz = sz; } }

const char* PrettyPrintClass(
    CQuickBytes *out,                   // where to put the pretty printed string   
    mdToken tk,					 		// The class token to look up 
    IMDInternalImport *pIMDI,           // ptr to IMDInternalImport class with ComSig
    DWORD formatFlags /*= formatILDasm*/)
{
    if(tk == mdTokenNil)  // Zero resolution scope for "somewhere here" TypeRefs
    {
        appendStr(out,"[*]");
        return(asString(out));
    }
    if (!pIMDI->IsValidToken(tk))
    {
        char str[1024];
        sprintf_s(str,COUNTOF(str)," [ERROR: INVALID TOKEN 0x%8.8X] ",tk);
        appendStr(out, str);
        return(asString(out));
    }
    switch (TypeFromToken(tk))
    {
        case mdtTypeRef:
        case mdtTypeDef:
            {
                const char *nameSpace = 0;  
                const char *name = 0;
                mdToken tkEncloser = mdTokenNil;
                
                if (TypeFromToken(tk) == mdtTypeRef)
                {
                    if (((formatFlags & FormatAssembly) && FAILED(pIMDI->GetResolutionScopeOfTypeRef(tk, &tkEncloser))) || 
                        FAILED(pIMDI->GetNameOfTypeRef(tk, &nameSpace, &name)))
                    {
                        char str[1024];
                        sprintf_s(str, COUNTOF(str), " [ERROR: Invalid TypeRef record 0x%8.8X] ", tk);
                        appendStr(out, str);
                        return asString(out);
                    }
                }
                else 
                {
                    if (((formatFlags & FormatNamespace) == 0) || FAILED(pIMDI->GetNestedClassProps(tk,&tkEncloser)))
                    {
                        tkEncloser = mdTypeDefNil;
                    }
                    if (FAILED(pIMDI->GetNameOfTypeDef(tk, &name, &nameSpace)))
                    {
                        char str[1024];
                        sprintf_s(str, COUNTOF(str), " [ERROR: Invalid TypeDef record 0x%8.8X] ", tk);
                        appendStr(out, str);
                        return asString(out);
                    }
                }
                MAKE_NAME_IF_NONE(name,tk);
                if((tkEncloser == mdTokenNil) || RidFromToken(tkEncloser))
                {
                    if (TypeFromToken(tkEncloser) == mdtTypeRef || TypeFromToken(tkEncloser) == mdtTypeDef)
                    {
                        PrettyPrintClass(out,tkEncloser,pIMDI, formatFlags);
                        if (formatFlags & FormatSlashSep)
                            appendChar(out, '/');    
                        else
                            appendChar(out, '+');
                        //nameSpace = ""; //don't print namespaces for nested classes!
                    }
                    else if (formatFlags & FormatAssembly)
                    {
                        PrettyPrintClass(out,tkEncloser,pIMDI, formatFlags);
                    }
                }
                if(TypeFromToken(tk)==mdtTypeDef)
                {
                    unsigned L = (unsigned)strlen(name)+1;
                    char* szFN = NULL;
                    if(((formatFlags & FormatNamespace) != 0) && nameSpace && *nameSpace)
                    {
                        const char* sz = nameSpace;
                        L+= (unsigned)strlen(sz)+1;
                        szFN = new char[L];
                        sprintf_s(szFN,L,"%s.",sz);
                    }
                    else
                    {
                        szFN = new char[L];
                        *szFN = 0;
                    }
                    strcat_s(szFN,L, name);
                    appendStr(out, szFN);
                    if (szFN) delete[] (szFN);
                }
                else
                {
                    if (((formatFlags & FormatNamespace) != 0) && nameSpace && *nameSpace) {
                        appendStr(out, nameSpace);  
                        appendChar(out, '.');    
                    }

                    appendStr(out, name);
                }
            }
            break;

        case mdtAssemblyRef:
            {
                LPCSTR	szName = NULL;
                pIMDI->GetAssemblyRefProps(tk,NULL,NULL,&szName,NULL,NULL,NULL,NULL);
                if(szName && *szName)
                {
                    appendChar(out, '[');    
                    appendStr(out, szName);
                    appendChar(out, ']');    
                }
            }
            break;
        case mdtAssembly:
            {
                LPCSTR	szName = NULL;
                pIMDI->GetAssemblyProps(tk,NULL,NULL,NULL,&szName,NULL,NULL);
                if(szName && *szName)
                {
                    appendChar(out, '[');    
                    appendStr(out, szName);
                    appendChar(out, ']');    
                }
            }
            break;
        case mdtModuleRef:
            {
                LPCSTR	szName = NULL;
                pIMDI->GetModuleRefProps(tk,&szName);
                if(szName && *szName)
                {
                    appendChar(out, '[');    
                    appendStr(out, ".module ");
                    appendStr(out, szName);
                    appendChar(out, ']');    
                }
            }
            break;

        case mdtTypeSpec:
            {
                ULONG cSig;
                PCCOR_SIGNATURE sig;
                if (FAILED(pIMDI->GetSigFromToken(tk, &cSig, &sig)))
                {
                    char str[128];
                    sprintf_s(str, COUNTOF(str), " [ERROR: Invalid token 0x%8.8X] ", tk);
                    appendStr(out, str);
                }
                else
                {
                    PrettyPrintType(sig, out, pIMDI, formatFlags);
                }
            }
            break;

        case mdtModule:
            break;
        
        default:
            {
                char str[128];
                sprintf_s(str,COUNTOF(str)," [ERROR: INVALID TOKEN TYPE 0x%8.8X] ",tk);
                appendStr(out, str);
            }
    }
    return(asString(out));
}

// This function takes a module and a token and prints the representation in the mdName buffer.
void PrettyPrintClassFromToken(
    TADDR moduleAddr,                   // the module containing the token
    mdToken tok,                        // the class token to look up
    __out_ecount(cbName) WCHAR* mdName, // where to put the pretty printed string
    size_t cbName,                      // the capacity of the buffer
    DWORD formatFlags /*= FormatCSharp*/)
{
    // set the default value
    swprintf_s(mdName, cbName, W("token_0x%8.8X"), tok);

    DacpModuleData dmd;
    if (dmd.Request(g_sos, TO_CDADDR(moduleAddr)) != S_OK)
        return;

    ToRelease<IMetaDataImport> pImport(MDImportForModule(&dmd));
    ToRelease<IMDInternalImport> pIMDI = NULL;

    if ((pImport == NULL) || FAILED(GetMDInternalFromImport(pImport, &pIMDI)))
        return;

    CQuickBytes qb;
    PrettyPrintClass(&qb, tok, pIMDI, formatFlags);
    MultiByteToWideChar (CP_ACP, 0, asString(&qb), -1, mdName, (int) cbName);
}
