// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _formatType_h
#define _formatType_h

#include "corpriv.h"					// for IMDInternalImport

// ILDASM code doesn't memcpy on gc pointers, so it prefers the real
// memcpy rather than GCSafeMemCpy.
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
#ifdef memcpy
#undef memcpy
#endif
#endif

#define MAX_PREFIX_SIZE 32

struct ParamDescriptor
{
    char*	name;
    mdToken tok;
    DWORD	attr;
};

char* DumpMarshaling(IMDInternalImport* pImport,
                     _Inout_updates_(cchszString) char* szString,
                     DWORD cchszString,
                     mdToken tok);
char* DumpParamAttr(_Inout_updates_(cchszString) char* szString,
                    DWORD cchszString,
                    DWORD dwAttr);

void appendStr(CQuickBytes *out, const char* str, unsigned len=(unsigned)-1);
void insertStr(CQuickBytes *out, const char* str);
char* asString(CQuickBytes *out);

const char* PrettyPrintSig(
    PCCOR_SIGNATURE typePtr,            // type to convert,
    unsigned typeLen,					// the length of 'typePtr'
    const char* name,                   // can be "", the name of the method for this sig 0 means local var sig
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI,           // ptr to IMDInternalImport class with ComSig
    _In_opt_ const char* inlabel,			// prefix for names (NULL if no names required)
    BOOL printTyArity=FALSE);           // flag to print Type Param number (MemberRefs only)

PCCOR_SIGNATURE PrettyPrintType(
    PCCOR_SIGNATURE typePtr,            // type to convert,
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI);          // ptr to IMDInternal class with ComSig

PCCOR_SIGNATURE PrettyPrintTypeOrDef(   // outside ILDASM - simple wrapper of PrettyPrintType
    PCCOR_SIGNATURE typePtr,            // type to convert,
    CQuickBytes *out,                   // where to put the pretty printed string
    IMDInternalImport *pIMDI);          // ptr to IMDInternal class with ComSig


const char* PrettyPrintClass(
    CQuickBytes *out,                   // where to put the pretty printed string
    mdToken tk,					 		// The class token to look up
    IMDInternalImport *pIMDI);          // ptr to IMDInternalImport class with ComSig

//================= ILDASM-specific ==================================================================

#ifdef __ILDASM__

#include "../ildasm/dynamicarray.h"

bool IsNameToQuote(const char *name);
bool IsLocalToQuote(const char *name);
const char* UnquotedProperName(_In_ __nullterminated const char* name, unsigned len=(unsigned)-1);
const char* ProperName(_In_ __nullterminated const char* name, bool isLocalName = false);
#define ProperLocalName(x) ProperName(x, true)
const char* KEYWORD(_In_opt_z_ const char* szOrig);
const char* COMMENT(_In_opt_z_ const char* szOrig);
const char* ERRORMSG(_In_opt_z_ const char* szOrig);
const char* ANCHORPT(_In_ __nullterminated const char* szOrig, mdToken tk);
const char* JUMPPT(_In_ __nullterminated const char* szOrig, mdToken tk);
const char* SCOPE(void);
const char* UNSCOPE(void);
const char* LTN(void);
const char* GTN(void);
const char* AMP(void);

extern BOOL g_fDumpRTF,g_fDumpHTML; // declared in FormatType.cpp
//-------------------------------------------------------------------------------
// Protection against null names
extern const char* const szStdNamePrefix[]; //declared in formatType.cpp

extern DynamicArray<mdToken>   *g_dups;
extern DWORD                   g_NumDups;
inline BOOL IsDup(mdToken tk)
{
    if(g_NumDups)
    {
        mdToken tktype = TypeFromToken(tk);
        if((tktype==mdtTypeDef)||(tktype==mdtMethodDef)||(tktype==mdtFieldDef))
        {
            for (unsigned i=0; i<g_NumDups; i++)
            {
                if((*g_dups)[i] == tk) return TRUE;
            }
        }
    }
    return FALSE;
}
#define MAKE_NAME_IF_NONE(psz, tk) { if((!(psz && *psz))||IsDup(tk)) { char* sz = (char*)_alloca(16); \
sprintf_s(sz,16,"$%s$%X",szStdNamePrefix[tk>>24],tk&0x00FFFFFF); psz = sz; } }


struct TypeDefDescr
{
    char* szName;
    mdToken tkTypeSpec;
    mdToken tkSelf;
    PCCOR_SIGNATURE psig;
    ULONG   cb;
};
extern DynamicArray<TypeDefDescr>   *g_typedefs;
extern DWORD                   g_NumTypedefs;


//-------------------------------------------------------------------------------
// Reference analysis (ILDASM)
struct TokPair
{
    mdToken tkUser;
    mdToken tkRef;
    TokPair() { tkUser = tkRef = 0; };
};
extern DynamicArray<TokPair>    *g_refs;
extern DWORD                    g_NumRefs;
extern mdToken                  g_tkRefUser; // for PrettyPrintSig
#define REGISTER_REF(x,y)  if(g_refs && (x)){ (*g_refs)[g_NumRefs].tkUser = x; (*g_refs)[g_NumRefs++].tkRef = y;}

//--------------------------------------------------------------------------------
// No-throw deallocators

#define SDELETE(x) {PAL_CPP_TRY{ delete (x); } PAL_CPP_CATCH_ALL { _ASSERTE(!"AV in scalar deallocator");} PAL_CPP_ENDTRY; (x)=NULL; }
#define VDELETE(x) {PAL_CPP_TRY{ delete [] (x); } PAL_CPP_CATCH_ALL { _ASSERTE(!"AV in vector deallocator");}; PAL_CPP_ENDTRY; (x)=NULL; }

//--------------------------------------------------------------------------------
// Generic param names
extern mdToken  g_tkVarOwner;
extern mdToken  g_tkMVarOwner;
//void SetVarOwner(mdToken tk) { g_tkVarOwner = tk; }
//void SetMVarOwner(mdToken tk) { g_tkMVarOwner = tk; }
BOOL PrettyPrintGP(                      // defined in dasm.cpp
    mdToken tkOwner,                    // Class, method or 0
    CQuickBytes *out,                   // where to put the pretty printed generic param
    int n);	      			 		    // Index of generic param

//============== End of ILDASM-specific ================================================================

#else

#define IsNameToQuote(x) false
#define IsLocalToQuote(x) false
#define UnquotedProperName(x) x
#define ProperName(x) x
#define ProperLocalName(x) x
#define KEYWORD(x) x
#define COMMENT(x) x
#define ERRORMSG(x) x
#define ANCHORPT(x,y) x
#define JUMPPT(x,y) x
#define SCOPE() "{"
#define UNSCOPE() "}"
#define LTN() "<"
#define GTN() ">"
#define AMP() "&"
#define REGISTER_REF(x,y) {}
#define MAKE_NAME_IF_NONE(x,y) { }
#define g_fDumpTokens false
#define SDELETE(x) delete (x)
#define VDELETE(x) delete [] (x)

#endif

#endif
