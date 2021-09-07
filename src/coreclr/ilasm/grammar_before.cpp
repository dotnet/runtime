// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <asmparse.h>
#include <assembler.h>

// Disable the "initialization of static local vars is no thread safe" error
#ifdef _MSC_VER
#pragma warning(disable : 4640)
#endif

#define YYMAXDEPTH 0x80000
#define YYLOCAL int
//#define YYRECURSIVE

//#define DEBUG_PARSING
#ifdef DEBUG_PARSING
bool parseDBFlag = true;
#define dbprintf(x)     if (parseDBFlag) printf x
#define YYDEBUG 1
#else
#define dbprintf(x)
#endif

#define FAIL_UNLESS(cond, msg) if (!(cond)) { parser->success = false; parser->error msg; }

static AsmParse* parser = 0;
#define PASM    (parser->assem)
#define PASMM   (parser->assem->m_pManifest)
#define PENV    (parser->penv)



PFN_NEXTCHAR    nextchar;
PFN_SYM         Sym;
PFN_NEWSTRFROMTOKEN NewStrFromToken;
PFN_NEWSTATICSTRFROMTOKEN NewStaticStrFromToken;
PFN_GETDOUBLE   GetDouble;

void SetFunctionPtrs()
{
    nextchar = PENV->pfn_nextchar;
    Sym = PENV->pfn_Sym;
    NewStrFromToken = PENV->pfn_NewStrFromToken;
    NewStaticStrFromToken = PENV->pfn_NewStaticStrFromToken;
    GetDouble = PENV->pfn_GetDouble;
}


static char* newStringWDel(__in __nullterminated char* str1, char delimiter, __in __nullterminated char* str3 = 0);
static char* newString(__in __nullterminated const char* str1);
static void corEmitInt(BinStr* buff, unsigned data);
static void AppendStringWithLength(BinStr* pbs, __in __nullterminated char* sz);
static void AppendFieldToCustomBlob(BinStr* pBlob, __in BinStr* pField);
bool bParsingByteArray = FALSE;
int iOpcodeLen = 0;
int iCallConv = 0;
unsigned IfEndif = 0;
unsigned IfEndifSkip = 0;
unsigned nCustomBlobNVPairs = 0;
unsigned nSecAttrBlobs = 0;
unsigned  nCurrPC = 0;
BOOL SkipToken = FALSE;
BOOL neg = FALSE;
BOOL newclass = FALSE;

extern unsigned int g_uConsoleCP;

struct VarName
{
    char* name;
    BinStr* pbody;
    VarName(__in_opt __nullterminated char* sz, BinStr* pbs) { name = sz; pbody = pbs; };
    ~VarName() { delete [] name; delete pbody; };
    int ComparedTo(VarName* pN) { return strcmp(name,pN->name); };
};
SORTEDARRAY<VarName> VarNameList;
void DefineVar(__in __nullterminated char* sz, BinStr* pbs) { VarNameList.PUSH(new VarName(sz,pbs)); };
void UndefVar(__in __nullterminated char* sz)
{
    CHECK_LOCAL_STATIC_VAR(static VarName VN(NULL,NULL));

    VN.name = sz;
    VarNameList.DEL(&VN);
    VN.name = NULL;
    delete [] sz;
}
VarName* FindVarDef(__in __nullterminated char* sz)
{
    CHECK_LOCAL_STATIC_VAR(static VarName VN(NULL,NULL));

    VarName* Ret = NULL;
    VN.name = sz;
    Ret = VarNameList.FIND(&VN);
    VN.name = NULL;
    delete [] sz;
    return Ret;
}
BOOL IsVarDefined(__in __nullterminated char* sz)
{
    return (FindVarDef(sz) != NULL);
}

int  nTemp=0;

unsigned int uMethodBeginLine,uMethodBeginColumn;

#define ELEMENT_TYPE_VARFIXUP (ELEMENT_TYPE_MAX+2)
#define ELEMENT_TYPE_MVARFIXUP (ELEMENT_TYPE_MAX+3)

FIFO<char> TyParFixupList;
void FixupTyPars(PCOR_SIGNATURE pSig, ULONG cSig);
void FixupTyPars(BinStr* pbstype);
void FixupConstraints()
{
    if((TyParFixupList.COUNT()) && (PASM->m_TyParList))
    {
        TyParList* pTPL;
        for(pTPL = PASM->m_TyParList; pTPL; pTPL=pTPL->Next())
        {
            mdToken* ptk;
            for(ptk = (mdToken*)(pTPL->Bound()->ptr()); *ptk; ptk++)
            {
                if(TypeFromToken(*ptk)==mdtTypeSpec)
                {
                    PCOR_SIGNATURE pSig;
                    ULONG cSig;
                    PASM->m_pImporter->GetTypeSpecFromToken(*ptk,(PCCOR_SIGNATURE*)&pSig,&cSig);
                    if((pSig)&&(cSig))
                    {
                        FixupTyPars(pSig,cSig);
                    } // end if((pSig)&&(cSig))
                } // end if(TypeFromToken(*ptk)==mdtTypeSpec)
            } //end for(ptk
        } // end for(pTPL
    } //end if((TyParFixupList.COUNT())
}

#define SET_PA(x,y,z) {x = (CorAssemblyFlags)(((y) & ~afPA_FullMask)|(z)|afPA_Specified);}
