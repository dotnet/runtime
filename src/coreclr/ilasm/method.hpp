// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// method.hpp
//
#ifndef _METHOD_HPP
#define _METHOD_HPP

class Assembler;
class PermissionDecl;
class PermissionSetDecl;

#define MAX_EXCEPTIONS 16   // init.number; increased by 16 when needed

extern unsigned int g_uCodePage;
extern WCHAR    wzUniBuf[];

/**************************************************************************/
struct LinePC
{
    ULONG   Line;
    ULONG   Column;
    ULONG   LineEnd;
    ULONG   ColumnEnd;
    ULONG   PC;
    Document* pOwnerDocument;
    BOOL    IsHidden;
};
typedef FIFO<LinePC> LinePCList;


struct PInvokeDescriptor
{
    mdModuleRef mrDll;
    char*   szAlias;
    DWORD   dwAttrs;
};

struct TokenRelocDescr // for OBJ generation only!
{
    DWORD   offset;
    mdToken token;
    TokenRelocDescr(DWORD off, mdToken tk) { offset = off; token = tk; };
};
typedef FIFO<TokenRelocDescr> TRDList;
/* structure - element of [local] signature name list */

struct  ARG_NAME_LIST
{
    LPCUTF8 szName; //szName[1024];
    DWORD dwName;
    BinStr*   pSig; // argument's signature  ptr
    BinStr*   pMarshal;
    BinStr*   pValue;
    int  nNum;
    DWORD     dwAttr;
    CustomDescrList CustDList;
    ARG_NAME_LIST *pNext;
    __forceinline ARG_NAME_LIST(int i, LPCUTF8 sz, BinStr *pbSig, BinStr *pbMarsh, DWORD attr)
    {
        nNum = i;
        //dwName = (DWORD)strlen(sz);
        //strcpy(szName,sz);
        szName = sz;
        dwName = (sz == NULL) ? 0 : (DWORD)strlen(sz);
        pNext = NULL;
        pSig=pbSig;
        pMarshal = pbMarsh;
        dwAttr = attr;
        pValue=NULL;
    };
    inline ~ARG_NAME_LIST()
    {
        if(pSig) delete pSig;
        if(pMarshal) delete pMarshal;
        if(pValue) delete pValue;
        if(szName) delete [] szName;
    }
};

class Scope;
typedef FIFO<Scope> ScopeList;
class Scope
{
public:
    DWORD   dwStart;
    DWORD   dwEnd;
    ARG_NAME_LIST*  pLocals;
    ScopeList       SubScope;
    Scope*          pSuperScope;
    Scope() { dwStart = dwEnd = 0; pLocals = NULL; pSuperScope = NULL; };
    ~Scope() { Reset(); };
    void Reset()
    {
        ARG_NAME_LIST* pNext;
        while(pLocals) { pNext = pLocals->pNext; delete pLocals; pLocals = pNext; }
        Scope* pS;
        while((pS = SubScope.POP()) != NULL) delete pS;
        pSuperScope = NULL;
        dwStart = dwEnd = 0;
    };
};
struct VarDescr
{
    DWORD   dwSlot;
    BinStr* pbsSig;
    BOOL    bInScope;
    VarDescr() { dwSlot = (DWORD) -1; pbsSig = NULL; bInScope = FALSE; };
};
typedef FIFO<VarDescr> VarDescrList;


struct Label
{
//public:
    LPCUTF8  m_szName;
    DWORD   m_PC;

    Label() :m_szName(NULL),m_PC(0){};
    Label(LPCUTF8 pszName, DWORD PC):m_szName(pszName),m_PC(PC){};
    ~Label(){ delete [] m_szName; };
    int ComparedTo(Label* L) { return strcmp(m_szName,L->m_szName); };
    //int Compare(char* L) { return strcmp(L,m_szName); };
    LPCUTF8 NameOf() { return m_szName; };
};
//typedef SORTEDARRAY<Label> LabelList;
typedef FIFO_INDEXED<Label> LabelList;

class GlobalFixup
{
public:
    LPCUTF8  m_szLabel;
    BYTE *  m_pReference;               // The place to fix up

    GlobalFixup(LPCUTF8 pszName, BYTE* pReference)
    {
        m_pReference   = pReference;
        m_szLabel = pszName;
    }
    ~GlobalFixup(){ delete [] m_szLabel; }
};
typedef FIFO<GlobalFixup> GlobalFixupList;


class Fixup
{
public:
    LPCUTF8  m_szLabel;
    BYTE *  m_pBytes; // where to make the fixup
    DWORD   m_RelativeToPC;
    BYTE    m_FixupSize;

    Fixup(LPCUTF8 pszName, BYTE *pBytes, DWORD RelativeToPC, BYTE FixupSize)
    {
        m_pBytes        = pBytes;
        m_RelativeToPC  = RelativeToPC;
        m_FixupSize     = FixupSize;
        m_szLabel = pszName;
    }
    ~Fixup(){ delete [] m_szLabel; }
};
typedef FIFO<Fixup> FixupList;

typedef enum { ilRVA, ilToken, ilGlobal} ILFixupType;

class ILFixup
{
public:
    ILFixupType   m_Kind;
    DWORD         m_OffsetInMethod;
    GlobalFixup * m_Fixup;

    ILFixup(DWORD Offset, ILFixupType Kind, GlobalFixup *Fix)
    {
      m_Kind           = Kind;
      m_OffsetInMethod = Offset;
      m_Fixup          = Fix;
    }
};
typedef FIFO<ILFixup> ILFixupList;

class Method
{
public:
    Class  *m_pClass;
    //BinStr **m_TyParBounds;
    //LPCWSTR *m_TyParNames;
    TyParDescr* m_TyPars;
    DWORD   m_NumTyPars;
    GenericParamConstraintList m_GPCList;

    DWORD   m_SigInfoCount;
    USHORT  m_MaxStack;
    mdSignature  m_LocalsSig;
    DWORD   m_Flags;
    char*   m_szName;
    DWORD   m_dwName;
    char*   m_szExportAlias;
    DWORD   m_dwExportOrdinal;
    COR_ILMETHOD_SECT_EH_CLAUSE_FAT *m_ExceptionList;
    DWORD   m_dwNumExceptions;
    DWORD   m_dwMaxNumExceptions;
    DWORD*  m_EndfilterOffsetList;
    DWORD   m_dwNumEndfilters;
    DWORD   m_dwMaxNumEndfilters;
    DWORD   m_Attr;
    BOOL    m_fEntryPoint;
    BOOL    m_fGlobalMethod;
    BOOL    m_fNewBody;
    BOOL    m_fNew;
    DWORD   m_methodOffset;
    DWORD   m_headerOffset;
    BYTE *  m_pCode;
    DWORD   m_CodeSize;
    WORD    m_wImplAttr;
    ULONG   m_ulLines[2];
    ULONG   m_ulColumns[2];
    // PInvoke attributes
    PInvokeDescriptor* m_pPInvoke;
    // Security attributes
    PermissionDecl* m_pPermissions;
    PermissionSetDecl* m_pPermissionSets;
    // VTable attributes
    WORD            m_wVTEntry;
    WORD            m_wVTSlot;
    // Return marshaling
    BinStr* m_pRetMarshal;
    BinStr* m_pRetValue;
    DWORD   m_dwRetAttr;
    CustomDescrList m_RetCustDList;
    ILFixupList     m_lstILFixup;
    FixupList       m_lstFixup;
//    LabelList       m_lstLabel;
    // Member ref fixups
    LocalMemberRefFixupList  m_LocalMemberRefFixupList;
    // Method body (header+code+EH)
    BinStr* m_pbsBody;
    mdToken m_Tok;
    Method(Assembler *pAssembler, Class *pClass, _In_ __nullterminated char *pszName, BinStr* pbsSig, DWORD Attr);
    ~Method()
    {
        m_lstFixup.RESET(true);
        //m_lstLabel.RESET(true);
        delete [] m_szName;
        if(m_szExportAlias) delete [] m_szExportAlias;
        delArgNameList(m_firstArgName);
        delArgNameList(m_firstVarName);
        delete m_pbsMethodSig;
        delete [] m_ExceptionList;
        delete [] m_EndfilterOffsetList;
        if(m_pRetMarshal) delete m_pRetMarshal;
        if(m_pRetValue) delete m_pRetValue;
        while(m_MethodImplDList.POP()); // ptrs in m_MethodImplDList are dups of those in Assembler
        if(m_pbsBody) delete m_pbsBody;
        if(m_TyPars) delete [] m_TyPars;
    };

    BOOL IsGlobalMethod()
    {
        return m_fGlobalMethod;
    };

    void SetIsGlobalMethod()
    {
        m_fGlobalMethod = TRUE;
    };

    void delArgNameList(ARG_NAME_LIST *pFirst)
    {
        ARG_NAME_LIST *pArgList=pFirst, *pArgListNext;
        for(; pArgList; pArgListNext=pArgList->pNext,
                        delete pArgList,
                        pArgList=pArgListNext);
    };

    ARG_NAME_LIST *catArgNameList(ARG_NAME_LIST *pBase, ARG_NAME_LIST *pAdd)
    {
        if(pAdd) //even if nothing to concatenate, result == head
        {
            ARG_NAME_LIST *pAN = pBase;
            if(pBase)
            {
                int i;
                for(; pAN->pNext; pAN = pAN->pNext) ;
                pAN->pNext = pAdd;
                i = pAN->nNum;
                for(pAN = pAdd; pAN; pAN->nNum = ++i, pAN = pAN->pNext);
            }
            else pBase = pAdd; //nothing to concatenate to, result == tail
        }
        return pBase;
    };

    int findArgNum(ARG_NAME_LIST *pFirst, LPCUTF8 szArgName, DWORD dwArgName)
    {
        int ret=-1;
        if(dwArgName)
        {
            ARG_NAME_LIST *pAN;
            for(pAN=pFirst; pAN; pAN = pAN->pNext)
            {
                if((pAN->dwName == dwArgName)&& ((dwArgName==0)||(!strcmp(pAN->szName,szArgName))))
                {
                    ret = pAN->nNum;
                    break;
                }
            }
        }
        return ret;
    };

    int findLocSlot(ARG_NAME_LIST *pFirst, LPCUTF8 szArgName, DWORD dwArgName)
    {
        int ret=-1;
        if(dwArgName)
        {
            ARG_NAME_LIST *pAN;
            for(pAN=pFirst; pAN; pAN = pAN->pNext)
            {
                if((pAN->dwName == dwArgName)&& ((dwArgName==0)||(!strcmp(pAN->szName,szArgName))))
                {
                    ret = (int)(pAN->dwAttr);
                    break;
                }
            }
        }
        return ret;
    };

    BinStr  *m_pbsMethodSig;
    COR_SIGNATURE*  m_pMethodSig;
    DWORD   m_dwMethodCSig;
    ARG_NAME_LIST *m_firstArgName;
    ARG_NAME_LIST *m_firstVarName;
    // to call error() from Method:
    const char* m_FileName;
    unsigned m_LineNum;
    // debug info
    LinePCList m_LinePCList;
    // custom values
    CustomDescrList m_CustomDescrList;
    // token relocs (used for OBJ generation only)
    TRDList m_TRDList;
    // method's own list of method impls
    MethodImplDList m_MethodImplDList;
    // lexical scope handling
    Assembler*      m_pAssembler;
    Scope           m_MainScope;
    Scope*          m_pCurrScope;
    VarDescrList    m_Locals;
    void OpenScope();
    void CloseScope();

    Label *FindLabel(LPCUTF8 pszName);
    Label *FindLabel(DWORD PC);

    int FindTyPar(_In_ __nullterminated WCHAR* wz)
    {
        int i,retval=-1;
        for(i=0; i < (int)m_NumTyPars; i++)
        {
            if(!wcscmp(wz,m_TyPars[i].Name()))
            {
                retval = i;
            }
        }
        return retval;
    };
    int FindTyPar(_In_ __nullterminated char* sz)
    {
        if(sz)
        {
            wzUniBuf[0] = 0;
            WszMultiByteToWideChar(g_uCodePage,0,sz,-1,wzUniBuf,dwUniBuf);
            return FindTyPar(wzUniBuf);
        }
        else return -1;
    };

    void AddGenericParamConstraint(int index, char * pStrGenericParam, mdToken tkTypeConstraint);
};

#endif /* _METHOD_HPP */

