// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "formattype.h"

#define MAX_INTERFACES_IMPLEMENTED  256     // unused
#define MAX_CLASSNAME_LENGTH        1024    // single global buffer size
#define MAX_MEMBER_LENGTH           1024    // single global buffer size
#define MAX_SIGNATURE_LENGTH        2048    // single global buffer size
#define DESCR_SIZE                  8       // unused

#define MAX_FILENAME_LENGTH         2048     //256

#define MODE_DUMP_ALL               0
#define MODE_DUMP_CLASS             1
#define MODE_DUMP_CLASS_METHOD      2
#define MODE_DUMP_CLASS_METHOD_SIG  3
#define MODE_GUI                    4

BOOL Disassemble(IMDInternalImport *pImport, BYTE *pCode, void *GUICookie, mdToken FuncToken, ParamDescriptor* pszArgname, ULONG ulArgs);
BOOL Decompile(IMDInternalImport *pImport, BYTE *pCode);
OPCODE DecodeOpcode(const BYTE *pCode, DWORD *pdwLen);
struct LineCodeDescr
{
    ULONG Line;
    ULONG Column;
    ULONG LineEnd;
    ULONG ColumnEnd;
    ULONG PC;
    ULONG_PTR FileToken;
};

void printLine(void* GUICookie, _In_ __nullterminated const char* string);
void printLineW(void* GUICookie, _In_ __nullterminated const WCHAR* string);
void printError(void* GUICookie, _In_ __nullterminated const char* string);

char* AnsiToUtf(_In_ __nullterminated const char* sz);
char* UnicodeToAnsi(_In_ __nullterminated const WCHAR* wz);
char* UnicodeToUtf(_In_ __nullterminated const WCHAR* wz);
WCHAR* UtfToUnicode(_In_ __nullterminated const char* sz);
WCHAR* AnsiToUnicode(_In_ __nullterminated const char* sz);
void GetInputFileFullPath();

char* RstrUTF(unsigned id);
WCHAR* RstrW(unsigned id);
char* RstrANSI(unsigned id);


BOOL DumpMethod(mdToken FuncToken, const char *pszClassName, DWORD dwEntryPointToken,void *GUICookie,BOOL DumpBody);
BOOL DumpField(mdToken FuncToken, const char *pszClassName,void *GUICookie, BOOL DumpBody);
BOOL DumpEvent(mdToken FuncToken, const char *pszClassName, DWORD dwClassAttrs, void *GUICookie, BOOL DumpBody);
BOOL DumpProp(mdToken FuncToken, const char *pszClassName, DWORD dwClassAttrs, void *GUICookie, BOOL DumpBody);
void dumpEHInfo(IMDInternalImport *pImport, void *GUICookie);
BOOL DumpClass(mdTypeDef cl, DWORD dwEntryPointToken, void* GUICookie, ULONG WhatToDump);
// WhatToDump: 0-title only; 1-pack,size and custom attrs; 2-everything
BOOL GetClassLayout(mdTypeDef cl, ULONG* pulPackSize, ULONG* pulClassSize);
void DumpCustomAttribute(mdCustomAttribute tkCA, void *GUICookie, bool bWithOwner);
void DumpCustomAttributes(mdToken tkOwner, void *GUICookie);
void DumpByteArray(__inout __nullterminated char* szString, const BYTE* pBlob, ULONG ulLen, void* GUICookie);
char* DumpDataPtr(__inout __nullterminated char* buffer, DWORD ptr, DWORD size);

void PrettyPrintToken(__inout __nullterminated char* szString, mdToken tk, IMDInternalImport *pImport,void* GUICookie,mdToken FuncToken); //TypeDef,TypeRef,TypeSpec,MethodDef,FieldDef,MemberRef,MethodSpec,String
void DumpPermissions(mdToken tkOwner, void* GUICookie);
void DumpHeader(IMAGE_COR20_HEADER *CORHeader, void* GUICookie);
void DumpHeaderDetails(IMAGE_COR20_HEADER *CORHeader, void* GUICookie);
void DumpMetaInfo(_In_ __nullterminated const WCHAR* pszFileName, _In_opt_z_ const char* pszObjFileName, void* GUICookie);
void DumpStatistics(IMAGE_COR20_HEADER *CORHeader, void* GUICookie);
BOOL DumpFile();
void Cleanup();
void CreateProgressBar(LONG lRange);
BOOL ProgressStep();
void DestroyProgressBar();
char * DumpQString(void* GUICookie,
                   _In_ __nullterminated const char* szToDump,
                   _In_ __nullterminated const char* szPrefix,
                   unsigned uMaxLen);
void DumpVtable(void* GUICookie);
char* DumpUnicodeString(void* GUICookie,
                        __inout __nullterminated char* szString,
                        _In_reads_(cbString) WCHAR* pszString,
                        ULONG cbString,
                        bool SwapString = false);

void TokenSigInit(IMDInternalImport *pImport);
void TokenSigDelete();
bool IsSpecialNumber(const char*);


//---------------- see DMAN.CPP--------------------------------------------------
struct LocalComTypeDescr
{
    mdExportedType      tkComTypeTok;
    mdTypeDef           tkTypeDef;
    mdToken             tkImplementation;
    WCHAR*              wzName;
    DWORD               dwFlags;
    LocalComTypeDescr(mdExportedType exportedType, mdTypeDef typeDef, mdToken impl, WCHAR* name, DWORD flags)
        : tkComTypeTok{exportedType}
        , tkTypeDef{typeDef}
        , tkImplementation{impl}
        , wzName{name}
        , dwFlags{flags}
    { };
    ~LocalComTypeDescr() { if(wzName) SDELETE(wzName); };
};

struct  MTokName
{
    mdFile  tok;
    WCHAR*  name;
    MTokName() { tok = 0; name = NULL; };
    ~MTokName() { if(name) SDELETE(name); };
};
extern BOOL g_fPrettyPrint;
extern MTokName*    rExeloc;
extern ULONG    nExelocs;
void DumpImplementation(mdToken tkImplementation,
                        DWORD dwOffset,
                        __inout __nullterminated char* szString,
                        void* GUICookie);
void DumpComType(LocalComTypeDescr* pCTD,
                 __inout __nullterminated char* szString,
                 void* GUICookie);
void DumpManifest(void* GUICookie);
void DumpTypedefs(void* GUICookie);
IMetaDataAssemblyImport* GetAssemblyImport(void* GUICookie);

void DumpRTFPrefix(void* GUICookie, BOOL fFontDefault);
void DumpRTFPostfix(void* GUICookie);

//-------------------------------------------------------------------------------
#define NEW_TRY_BLOCK   0x80000000
#define PUT_INTO_CODE   0x40000000
#define ERR_OUT_OF_CODE 0x20000000
#define SEH_NEW_PUT_MASK    (NEW_TRY_BLOCK | PUT_INTO_CODE | ERR_OUT_OF_CODE)
//-------------------------------------------------------------------------------

// As much as 2 * SZSTRING_SIZE seems to be needed for some corner cases. ILDasm is
// unfortunately full of constants and artificial limits, and may produce invalid
// output or overrun a buffer when fed with something unusual (like a type with
// thousands of generic parameters). Fixing all such problems effectively means
// rewriting the tool from scratch. Until this happens, let's at least try to keep it
// working for reasonable input.
#define UNIBUF_SIZE 262144
extern WCHAR   wzUniBuf[]; // defined in dis.cpp

#define SZSTRING_SIZE 131072
extern char   szString[]; // defined in dis.cpp

char *DumpGenericPars(_Inout_updates_(SZSTRING_SIZE) char* szString,
                      mdToken tok,
                      void* GUICookie=NULL,
                      BOOL fSplit=FALSE);

#include "safemath.h"
#define SZSTRING_SIZE_M4 (SZSTRING_SIZE - 4)
#define CHECK_REMAINING_SIZE if(ovadd_le((size_t)szString, SZSTRING_SIZE_M4, (size_t)szptr)) break;
#define SZSTRING_REMAINING_SIZE(x) (ovadd_le((size_t)szString,SZSTRING_SIZE,(size_t)(x))?0:(SZSTRING_SIZE-((size_t)(x)-(size_t)szString)))


