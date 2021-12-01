// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/************************************************************************/
/*                           Assembler.h                                */
/************************************************************************/

#ifndef Assember_h
#define Assember_h

#define NEW_INLINE_NAMES

#include "binstr.h"

#include "specstrings.h"

#include "asmenum.h"
#include "asmtemplates.h"

#include "portable_pdb.h"
#include "portablepdbmdi.h"

// Disable the "initialization of static local vars is no thread safe" error
#ifdef _MSC_VER
#pragma warning(disable : 4640)
#endif

#ifdef ResetEvent
#undef ResetEvent
#endif

#define OUTPUT_BUFFER_SIZE          8192      // initial size of asm code for a single method
#define OUTPUT_BUFFER_INCREMENT     1024      // size of code buffer increment when it's full
#define MAX_FILENAME_LENGTH         2048      //256
#define MAX_SIGNATURE_LENGTH        256       // unused
#define MAX_LABEL_SIZE              256       //64
#define MAX_CALL_SIG_SIZE           32        // unused
#define MAX_SCOPE_LENGTH            _MAX_PATH // follow the RegMeta::SetModuleProps limitation

#define MAX_NAMESPACE_LENGTH        1024      //256    //64
#define MAX_MEMBER_NAME_LENGTH      1024      //256    //64

#define MAX_INTERFACES_IMPLEMENTED  16        // initial number; extended by 16 when needed
#define GLOBAL_DATA_SIZE            8192      // initial size of global data buffer
#define GLOBAL_DATA_INCREMENT       1024      // size of global data buffer increment when it's full
#define MAX_METHODS                 1024      // unused
#define MAX_INPUT_LINE_LEN          1024      // unused
#define MAX_TYPAR                   8
#define BASE_OBJECT_CLASSNAME   "System.Object"
#define MAX_MANIFEST_RESOURCES      1024

// Fully-qualified class name separators:
#define NESTING_SEP     ((char)0xF8)

#define dwUniBuf 16384

#ifdef TARGET_UNIX
extern char *g_pszExeFile;
#endif

extern WCHAR   wzUniBuf[]; // Unicode conversion global buffer (assem.cpp)

class Class;
class Method;
class PermissionDecl;
class PermissionSetDecl;

unsigned hash(                // defined in assem.cpp
     __in_ecount(length) const BYTE *k,        /* the key */
     unsigned  length,   /* the length of the key */
     unsigned  initval);  /* the previous hash, or an arbitrary value */

struct MemberRefDescriptor
{
    mdToken             m_tdClass;
    Class*              m_pClass;
    char*               m_szName;
    DWORD               m_dwName;
    BinStr*             m_pSigBinStr;
    mdToken             m_tkResolved;
};
typedef FIFO<MemberRefDescriptor> MemberRefDList;


struct MethodImplDescriptor
{
    mdToken             m_tkImplementedMethod;
    mdToken             m_tkImplementingMethod;
    mdToken             m_tkDefiningClass;
    BOOL                m_fNew;
};
typedef FIFO<MethodImplDescriptor> MethodImplDList;

struct LocalMemberRefFixup
{
    mdToken tk;
    size_t  offset;
    BOOL    m_fNew;
    LocalMemberRefFixup(mdToken TK, size_t Offset)
    {
        tk = TK;
        offset = Offset;
        m_fNew = TRUE;
    }
};
typedef FIFO<LocalMemberRefFixup> LocalMemberRefFixupList;

struct CustomDescr
{
    mdToken tkType;
    mdToken tkOwner;
    mdToken tkInterfacePair; // Needed for InterfaceImpl CA's
    BinStr* pBlob;

    CustomDescr(mdToken tko, mdToken tk, BinStr* pblob)
    {
        tkType = tk;
        pBlob = pblob;
        tkOwner = tko;
        tkInterfacePair = 0;
    };
    CustomDescr(mdToken tk, BinStr* pblob)
    {
        tkType = tk;
        pBlob = pblob;
        tkOwner = 0;
        tkInterfacePair = 0;
    };
    CustomDescr(CustomDescr* pOrig)
    {
        tkType = pOrig->tkType;
        pBlob = new BinStr();
        pBlob->append(pOrig->pBlob);
        tkOwner = pOrig->tkOwner;
        tkInterfacePair = pOrig->tkInterfacePair;
    };
    ~CustomDescr()
    {
        if(pBlob)
            delete pBlob;
    };
};
typedef FIFO<CustomDescr> CustomDescrList;
typedef LIFO<CustomDescrList> CustomDescrListStack;

class GenericParamConstraintDescriptor
{
public:
    GenericParamConstraintDescriptor()
    {
        m_tk = mdTokenNil;
        m_tkOwner = mdTokenNil;
        m_iGenericParamIndex = -1;
        m_tkTypeConstraint = mdTokenNil;
    };
    ~GenericParamConstraintDescriptor()
    {
        m_lstCA.RESET(true);
    };
    void Init(int index, mdToken typeConstraint)
    {
        m_iGenericParamIndex = index;
        m_tkTypeConstraint = typeConstraint;
    };
    void Token(mdToken tk)
    {
        m_tk = tk;
    };
    mdToken Token()
    {
        return m_tk;
    };
    void SetOwner(mdToken tk)
    {
        m_tkOwner = tk;
    };
    mdToken GetOwner()
    {
        return m_tkOwner;
    };
    int GetParamIndex()
    {
        return m_iGenericParamIndex;
    };
    mdToken GetTypeConstraint()
    {
        return m_tkTypeConstraint;
    };
    CustomDescrList* CAList()
    {
        return &m_lstCA;
    };

private:
    mdGenericParamConstraint m_tk;
    mdToken m_tkOwner;
    int m_iGenericParamIndex;
    mdToken m_tkTypeConstraint;

    CustomDescrList m_lstCA;
};
typedef FIFO<GenericParamConstraintDescriptor> GenericParamConstraintList;
/**************************************************************************/
#include "typar.hpp"
#include "method.hpp"
#include "iceefilegen.h"
#include "asmman.hpp"

#include "nvpair.h"


typedef enum
{
    STATE_OK,
    STATE_FAIL,
    STATE_ENDMETHOD,
    STATE_ENDFILE
} state_t;


class GlobalLabel
{
public:
    LPCUTF8         m_szName;
    DWORD           m_GlobalOffset;
    HCEESECTION     m_Section;
    unsigned        m_Hash;

    GlobalLabel(LPCUTF8 pszName, DWORD GlobalOffset, HCEESECTION section)
    {
        m_GlobalOffset  = GlobalOffset;
        m_Section       = section;
        m_szName = pszName;
        m_Hash = hash((const BYTE*)pszName, (unsigned)strlen(pszName),10);
    }

    ~GlobalLabel(){ delete [] m_szName; }

    int ComparedTo(GlobalLabel* L)
    {
        return (m_Hash == L->m_Hash) ? strcmp(m_szName, L->m_szName)
                                     : ((m_Hash > L->m_Hash) ? 1 : -1);
    }

    //int ComparedTo(GlobalLabel* L) { return strcmp(m_szName,L->m_szName); };
    //int Compare(char* L) { return strcmp(L, m_szNam); };
    //char* NameOf() { return m_szName; };
};
//typedef SORTEDARRAY<GlobalLabel> GlobalLabelList;
typedef RBTREE<GlobalLabel> GlobalLabelList;
//typedef FIFO_INDEXED<GlobalLabel> GlobalLabelList;

class CeeFileGenWriter;
class CeeSection;

class BinStr;

/************************************************************************/
/* represents an object that knows how to report errors back to the user */

class ErrorReporter
{
public:
    virtual void error(const char* fmt, ...) = 0;
    virtual void warn(const char* fmt, ...) = 0;
    virtual void msg(const char* fmt, ...) = 0;
};

/**************************************************************************/
/* represents a switch table before the lables are bound */

struct Labels {
    Labels(__in __nullterminated char* aLabel, Labels* aNext, bool aIsLabel) : Label(aLabel), Next(aNext), isLabel(aIsLabel) {}
    ~Labels() { if(isLabel && Label) delete [] Label; delete Next; }

    char*       Label;
    Labels*     Next;
    bool        isLabel;
};

/**************************************************************************/
/* descriptor of the structured exception handling construct  */
struct SEH_Descriptor
{
    DWORD       sehClause;  // catch/filter/finally
    DWORD       tryFrom;    // start of try block
    DWORD       tryTo;      // end of try block
    DWORD       sehHandler; // start of exception handler
    DWORD       sehHandlerTo; // end of exception handler
    union {
        DWORD       sehFilter;  // start of filter block
        mdTypeRef   cException; // what to catch
    };

    SEH_Descriptor()
    {
        memset(this, 0, sizeof(*this));
    }
};


typedef LIFO<char> StringStack;
typedef LIFO<SEH_Descriptor> SEHD_Stack;

typedef FIFO<Method> MethodList;
//typedef SORTEDARRAY<Method> MethodSortedList;
typedef FIFO<mdToken> TokenList;
/**************************************************************************/
/* The field, event and property descriptor structures            */

struct FieldDescriptor
{
    mdTypeDef       m_tdClass;
    char*           m_szName;
    DWORD           m_dwName;
    mdFieldDef      m_fdFieldTok;
    ULONG           m_ulOffset;
    char*           m_rvaLabel;         // if field has RVA associated with it, label for it goes here.
    BinStr*         m_pbsSig;
    Class*			m_pClass;
    BinStr*			m_pbsValue;
    BinStr*			m_pbsMarshal;
	PInvokeDescriptor*	m_pPInvoke;
    CustomDescrList     m_CustomDescrList;
    DWORD			m_dwAttr;
    BOOL            m_fNew;
    // Security attributes
    PermissionDecl* m_pPermissions;
    PermissionSetDecl* m_pPermissionSets;
    FieldDescriptor()  { m_szName = NULL; m_pbsSig = NULL; m_fNew = TRUE; };
    ~FieldDescriptor() { if(m_szName) delete [] m_szName; if(m_pbsSig) delete m_pbsSig; };
};
typedef FIFO<FieldDescriptor> FieldDList;

struct EventDescriptor
{
    mdTypeDef           m_tdClass;
    char*               m_szName;
    DWORD               m_dwAttr;
    mdToken             m_tkEventType;
    mdToken             m_tkAddOn;
    mdToken             m_tkRemoveOn;
    mdToken             m_tkFire;
    TokenList           m_tklOthers;
    mdEvent             m_edEventTok;
    BOOL                m_fNew;
    CustomDescrList     m_CustomDescrList;
    ~EventDescriptor() { m_tklOthers.RESET(false); };
};
typedef FIFO<EventDescriptor> EventDList;

struct PropDescriptor
{
    mdTypeDef           m_tdClass;
    char*               m_szName;
    DWORD               m_dwAttr;
    COR_SIGNATURE*      m_pSig;
    DWORD               m_dwCSig;
    DWORD               m_dwCPlusTypeFlag;
    PVOID               m_pValue;
    DWORD				m_cbValue;
    mdToken             m_tkSet;
    mdToken             m_tkGet;
    TokenList           m_tklOthers;
    mdProperty          m_pdPropTok;
    BOOL                m_fNew;
    CustomDescrList     m_CustomDescrList;
    ~PropDescriptor() { m_tklOthers.RESET(false); };
};
typedef FIFO<PropDescriptor> PropDList;

struct ImportDescriptor
{
    char*   szDllName;
//    char   szDllName[MAX_FILENAME_LENGTH];
    DWORD  dwDllName;
    mdModuleRef mrDll;
    ImportDescriptor(__in __nullterminated char* sz, DWORD l)
    {
        if((sz != NULL)&&(l > 0))
        {
            szDllName = new char[l+1];
            if(szDllName != NULL)
            {
                memcpy(szDllName,sz,l);
                szDllName[l] = 0;
                dwDllName = l;
            }
        }
        else
        {
            szDllName = NULL;
            dwDllName = 0;
        }
    };
    ~ImportDescriptor() { delete [] szDllName; };
};
typedef FIFO<ImportDescriptor> ImportList;


/**************************************************************************/
#include "class.hpp"
typedef LIFO<Class> ClassStack;
typedef FIFO<Class> ClassList;
//typedef SORTEDARRAY<Class> ClassHash;
typedef RBTREE<Class> ClassHash;
//typedef FIFO_INDEXED<Class> ClassHash;

/**************************************************************************/
/* Classes to hold lists of security permissions and permission sets. We build
   these lists as we find security directives in the input stream and drain
   them every time we see a class or method declaration (to which the
   security info is attached). */

class PermissionDecl
{
public:
    PermissionDecl(CorDeclSecurity action, mdToken type, NVPair *pairs)
    {
        m_Action = action;
        m_TypeSpec = type;
        m_pbsBlob = NULL;
        BuildConstructorBlob(action, pairs);
        m_Next = NULL;
    }

    PermissionDecl(CorDeclSecurity action, mdToken type, BinStr* pbsPairs)
    {
        m_Action = action;
        m_TypeSpec = type;

        m_pbsBlob = new BinStr();
        m_pbsBlob->appendInt16(VAL16(1));           // prolog 0x01 0x00
        m_pbsBlob->appendInt32(VAL32((int)action)); // 4-byte action
        if(pbsPairs)                                // name-value pairs if any
        {
            if(pbsPairs->length() > 2)
                m_pbsBlob->appendFrom(pbsPairs,2);
            delete pbsPairs;
        }
        if(m_pbsBlob->length() == 6) // no pairs added
            m_pbsBlob->appendInt16(0);
        m_Blob = m_pbsBlob->ptr();
        m_BlobLength = m_pbsBlob->length();
        m_Next = NULL;
    }

    ~PermissionDecl()
    {
        if(m_pbsBlob) delete m_pbsBlob;
        else delete [] m_Blob;
    }

    CorDeclSecurity     m_Action;
    mdToken             m_TypeSpec;
    BYTE               *m_Blob;
    BinStr             *m_pbsBlob;
    long                m_BlobLength;
    PermissionDecl     *m_Next;

private:
    void BuildConstructorBlob(CorDeclSecurity action, NVPair *pairs)
    {
        NVPair *p = pairs;
        int count = 0;
        int bytes = 8;
        int length;
        int i;
        BYTE *pBlob;

        // Calculate number of name/value pairs and the memory required for the
        // custom attribute blob.
        while (p) {
            BYTE *pVal = (BYTE*)p->Value()->ptr();
            count++;
            bytes += 2; // One byte field/property specifier, one byte type code

            length = (int)strlen((const char *)p->Name()->ptr());
            bytes += CPackedLen::Size(length) + length;

            switch (pVal[0]) {
            case SERIALIZATION_TYPE_BOOLEAN:
                bytes += 1;
                break;
            case SERIALIZATION_TYPE_I4:
                bytes += 4;
                break;
            case SERIALIZATION_TYPE_STRING:
                length = (int)strlen((const char *)&pVal[1]);
                bytes += CPackedLen::Size(length) + length;
                break;
            case SERIALIZATION_TYPE_ENUM:
                length = (int)strlen((const char *)&pVal[1]);
                bytes += CPackedLen::Size((ULONG)length) + length;
                bytes += 4;
                break;
            }
            p = p->Next();
        }

        m_Blob = new BYTE[bytes];
        if(m_Blob==NULL)
        {
            fprintf(stderr,"\nOut of memory!\n");
            return;
        }

        m_Blob[0] = 0x01;           // Version
        m_Blob[1] = 0x00;
        m_Blob[2] = (BYTE)action;   // Constructor arg (security action code)
        m_Blob[3] = 0x00;
        m_Blob[4] = 0x00;
        m_Blob[5] = 0x00;
        m_Blob[6] = (BYTE)count;    // Property/field count
        m_Blob[7] = (BYTE)(count >> 8);

        for (i = 0, pBlob = &m_Blob[8], p = pairs; i < count; i++, p = p->Next()) {
            BYTE *pVal = (BYTE*)p->Value()->ptr();
            char *szType;

            // Set field/property setter type.
            *pBlob++ = SERIALIZATION_TYPE_PROPERTY;

            // Set type code. There's additional info for enums (the enum class
            // name).
            *pBlob++ = pVal[0];
            if (pVal[0] == SERIALIZATION_TYPE_ENUM) {
                szType = (char *)&pVal[1];
                length = (int)strlen(szType);
                pBlob = (BYTE*)CPackedLen::PutLength(pBlob, length);
                strcpy_s((char *)pBlob, bytes, szType);
                pBlob += length;
            }

            // Record the field/property name.
            length = (int)strlen((const char *)p->Name()->ptr());
            pBlob = (BYTE*)CPackedLen::PutLength(pBlob, length);
            strcpy_s((char *)pBlob, bytes-(pBlob-m_Blob), (const char *)p->Name()->ptr());
            pBlob += length;

            // Record the serialized value.
            switch (pVal[0]) {
            case SERIALIZATION_TYPE_BOOLEAN:
                *pBlob++ = pVal[1];
                break;
            case SERIALIZATION_TYPE_I4:
                *(__int32*)pBlob = *(__int32*)&pVal[1];
                pBlob += 4;
                break;
            case SERIALIZATION_TYPE_STRING:
                length = (int)strlen((const char *)&pVal[1]);
                pBlob = (BYTE*)CPackedLen::PutLength(pBlob, length);
                strcpy_s((char *)pBlob, bytes-(pBlob-m_Blob), (const char *)&pVal[1]);
                pBlob += length;
                break;
            case SERIALIZATION_TYPE_ENUM:
                length = (int)strlen((const char *)&pVal[1]);
                // We can have enums with base type of I1, I2 and I4.
                switch (pVal[1 + length + 1]) {
                case 1:
                    *(__int8*)pBlob = *(__int8*)&pVal[1 + length + 2];
                    pBlob += 1;
                    break;
                case 2:
                    *(__int16*)pBlob = *(__int16*)&pVal[1 + length + 2];
                    pBlob += 2;
                    break;
                case 4:
                    *(__int32*)pBlob = *(__int32*)&pVal[1 + length + 2];
                    pBlob += 4;
                    break;
                default:
                    _ASSERTE(!"Invalid enum size");
                }
                break;
            }

        }

        _ASSERTE((pBlob - m_Blob) == bytes);

        m_BlobLength = (long)bytes;
    }
};

class PermissionSetDecl
{
public:
    PermissionSetDecl(CorDeclSecurity action, BinStr *value)
    {
        m_Action = action;
        m_Value = value;
        m_Next = NULL;
    }

    ~PermissionSetDecl()
    {
        delete m_Value;
    }

    CorDeclSecurity     m_Action;
    BinStr             *m_Value;
    PermissionSetDecl  *m_Next;
};

struct VTFEntry
{
    char*   m_szLabel;
    WORD    m_wCount;
    WORD    m_wType;
    VTFEntry(WORD wCount, WORD wType, __in __nullterminated char* szLabel) { m_wCount = wCount; m_wType = wType; m_szLabel = szLabel; }
    ~VTFEntry() { delete m_szLabel; }
};
typedef FIFO<VTFEntry> VTFList;

struct	EATEntry
{
	DWORD	dwStubRVA;
	DWORD	dwOrdinal;
	char*	szAlias;
};
typedef FIFO<EATEntry> EATList;

/**************************************************************************/
/* The assembler object does all the code generation (dealing with meta-data)
   writing a PE file etc etc. But does NOT deal with syntax (that is what
   AsmParse is for).  Thus the API below is how AsmParse 'controls' the
   Assember.  Note that the Assembler object does know about the
   AsmParse object (that is Assember is more fundamental than AsmParse) */
struct Instr
{
    int opcode;
    unsigned linenum;
	unsigned column;
    unsigned linenum_end;
	unsigned column_end;
    unsigned pc;
    Document* pOwnerDocument;
};
#define INSTR_POOL_SIZE 16

// For code folding:
struct MethodBody
{
    BinStr* pbsBody;
    unsigned RVA;
    BYTE*   pCode;
};
typedef FIFO<MethodBody> MethodBodyList;

struct Clockwork
{
    DWORD  cBegin;
    DWORD  cEnd;
    DWORD  cParsBegin;
    DWORD  cParsEnd;
    DWORD  cMDInitBegin;
    DWORD  cMDInitEnd;
    DWORD  cMDEmitBegin;
    DWORD  cMDEmitEnd;
    DWORD  cMDEmit1;
    DWORD  cMDEmit2;
    DWORD  cMDEmit3;
    DWORD  cMDEmit4;
    DWORD  cRef2DefBegin;
    DWORD  cRef2DefEnd;
    DWORD  cFilegenBegin;
    DWORD  cFilegenEnd;
};

struct TypeDefDescr
{
    char* m_szName;
    union
    {
        BinStr* m_pbsTypeSpec;
        CustomDescr* m_pCA;
    };
    mdToken m_tkTypeSpec;
    TypeDefDescr(__in_opt __nullterminated char *pszName, BinStr* pbsTypeSpec, mdToken tkTypeSpec)
    {
        m_szName = pszName;
        m_pbsTypeSpec = pbsTypeSpec;
        m_tkTypeSpec = tkTypeSpec;
    };
    ~TypeDefDescr() { delete [] m_szName; delete m_pbsTypeSpec; };
    int ComparedTo(TypeDefDescr* T) { return strcmp(m_szName,T->m_szName); };
    //int Compare(char* T) { return strcmp(T,m_szName); };
};
typedef SORTEDARRAY<TypeDefDescr> TypeDefDList;

struct Indx
{
    void* table[128];
    Indx() { memset(table,0,sizeof(table)); };
    ~Indx()
    {
        for(int i = 1; i < 128; i++) delete ((Indx*)(table[i]));
    };
    void IndexString(__in_z __in char* psz, void* pkywd)
    {
        int i = (int) *psz;
        if(i == 0)
            table[0] = pkywd;
        else
        {
            _ASSERTE((i > 0)&&(i <= 127));
            Indx* pInd = (Indx*)(table[i]);
            if(pInd == NULL)
            {
                pInd = new Indx;
                _ASSERTE(pInd);
                table[i] = pInd;
            }
            pInd->IndexString(psz+1,pkywd);
        }
    }
    void*  FindString(__in __nullterminated char* psz)
    {
        if(*psz > 0)
        {
            unsigned char uch = (unsigned char) *psz;
            if(table[uch] != NULL)
                return ((Indx*)(table[uch]))->FindString(psz+1);
        }
        else if(*psz == 0) return table[0];
        return NULL;
    }
};

class Assembler {
public:
    Assembler();
    ~Assembler();
    //--------------------------------------------------------
	GlobalLabelList m_lstGlobalLabel;
	GlobalFixupList m_lstGlobalFixup;

    LabelList       m_lstLabel;

    Class *			m_pModuleClass;
    ClassList		m_lstClass;
    ClassHash		m_hshClass;

    Indx            indxKeywords;

    BYTE *  m_pOutputBuffer;
    BYTE *  m_pCurOutputPos;
    BYTE *  m_pEndOutputPos;


    DWORD   m_CurPC;
    BOOL    m_fStdMapping;
    BOOL    m_fDisplayTraceOutput;
    BOOL    m_fInitialisedMetaData;
    BOOL    m_fAutoInheritFromObject;
    BOOL    m_fReportProgress;
    BOOL    m_fIsMscorlib;
    BOOL    m_fTolerateDupMethods;
    BOOL    m_fOptimize;
    mdToken m_tkSysObject;
    mdToken m_tkSysString;
    mdToken m_tkSysValue;
    mdToken m_tkSysEnum;
    BOOL    m_fDidCoInitialise;

    IMetaDataDispenserEx2 *m_pDisp;
    IMetaDataEmit3      *m_pEmitter;
    ICeeFileGen        *m_pCeeFileGen;
    IMetaDataImport2    *m_pImporter;			// Import interface.
    HCEEFILE m_pCeeFile;
    HCEESECTION m_pGlobalDataSection;
    HCEESECTION m_pILSection;
    HCEESECTION m_pTLSSection;
    HCEESECTION m_pCurSection;      // The section EmitData* things go to

    AsmMan*     m_pManifest;

    char    m_szScopeName[MAX_SCOPE_LENGTH];
    char    *m_szNamespace; //[MAX_NAMESPACE_LENGTH];
    char    *m_szFullNS; //[MAX_NAMESPACE_LENGTH];
	unsigned	m_ulFullNSLen;

    WCHAR   *m_wzMetadataVersion;

    StringStack m_NSstack;
    mdTypeSpec      m_crExtends;

    //    char    m_szExtendsClause[MAX_CLASSNAME_LENGTH];

    // The (resizable) array of "implements" types
    mdToken   *m_crImplList;
    int     m_nImplList;
    int     m_nImplListSize;

    TyParList       *m_TyParList;

    Method *m_pCurMethod;
    Class   *m_pCurClass;
    ClassStack m_ClassStack; // for nested classes
    Class   *dummyClass; // for FindCreateClass

    // moved to Class
    //MethodList  m_MethodList;

    BOOL    m_fDLL;
    BOOL    m_fEntryPointPresent;
    BOOL    m_fHaveFieldsWithRvas;
    BOOL    m_fFoldCode;
    DWORD   m_dwMethodsFolded;

    state_t m_State;

    BinStr* m_pbsMD;

    Instr   m_Instr[INSTR_POOL_SIZE]; // 16
    inline  Instr* GetInstr()
    {
        int i;
        for(i=0; (i<INSTR_POOL_SIZE)&&(m_Instr[i].opcode != -1); i++);
        if(i<INSTR_POOL_SIZE) return &m_Instr[i];
        report->error("Instruction pool exhausted: source contains invalid instructions\n");
        return NULL;
    }
    // Labels, fixups and IL fixups are defined in Method.hpp,.cpp
    void AddLabel(DWORD CurPC, __in __nullterminated char *pszName);
    void AddDeferredFixup(__in __nullterminated char *pszLabel, BYTE *pBytes, DWORD RelativeToPC, BYTE FixupSize);
    void AddDeferredILFixup(ILFixupType Kind);
    void AddDeferredILFixup(ILFixupType Kind, GlobalFixup *GFixup);
    void DoDeferredILFixups(Method* pMethod);
    BOOL DoFixups(Method* pMethod);
    //--------------------------------------------------------------------------------
    void    ClearImplList(void);
    void    AddToImplList(mdToken);
    void    ClearBoundList(void);
    //--------------------------------------------------------------------------------
    BOOL Init(BOOL generatePdb);
    void ProcessLabel(__in_z __in char *pszName);
    GlobalLabel *FindGlobalLabel(LPCUTF8 pszName);
    GlobalFixup *AddDeferredGlobalFixup(__in __nullterminated char *pszLabel, BYTE* reference);
    //void AddDeferredDescrFixup(__in __nullterminated char *pszLabel);
    BOOL DoGlobalFixups();
    BOOL DoDescrFixups();
    OPCODE DecodeOpcode(const BYTE *pCode, DWORD *pdwLen);
    BOOL AddMethod(Method *pMethod);
    void SetTLSSection() { m_pCurSection = m_pTLSSection; }
    void SetILSection() { m_pCurSection = m_pILSection; }
    void SetDataSection()       { m_pCurSection = m_pGlobalDataSection; }
    BOOL EmitMethod(Method *pMethod);
    BOOL EmitMethodBody(Method* pMethod, BinStr* pbsOut);
    BOOL EmitClass(Class *pClass);
    HRESULT CreatePEFile(__in __nullterminated WCHAR *pwzOutputFilename);
    HRESULT CreateTLSDirectory();
    HRESULT CreateDebugDirectory();
    HRESULT InitMetaData();
    Class *FindCreateClass(__in __nullterminated const char *pszFQN);
    BOOL EmitFieldRef(__in_z __in char *pszArg, int opcode);
    BOOL EmitSwitchData(__in_z __in char *pszArg);
    mdToken ResolveClassRef(mdToken tkResScope, __in __nullterminated const char *pszClassName, Class** ppClass);
    mdToken ResolveTypeSpec(BinStr* typeSpec);
    mdToken GetBaseAsmRef();
    mdToken GetAsmRef(__in __nullterminated const char* szName);
    mdToken GetModRef(__in __nullterminated char* szName);
    mdToken GetInterfaceImpl(mdToken tsClass, mdToken tsInterface);
    char* ReflectionNotation(mdToken tk);
    HRESULT ConvLocalSig(__in char* localsSig, CQuickBytes* corSig, DWORD* corSigLen, BYTE*& localTypes);
    DWORD GetCurrentILSectionOffset();
    BOOL EmitCALLISig(__in char *p);
    void AddException(DWORD pcStart, DWORD pcEnd, DWORD pcHandler, DWORD pcHandlerTo, mdTypeRef crException, BOOL isFilter, BOOL isFault, BOOL isFinally);
    state_t CheckLocalTypeConsistancy(int instr, unsigned arg);
    state_t AddGlobalLabel(__in __nullterminated char *pszName, HCEESECTION section);
    void SetDLL(BOOL);
    void ResetForNextMethod();
    void ResetLineNumbers();
    void SetStdMapping(BOOL val = TRUE) { m_fStdMapping = val; };

    //--------------------------------------------------------------------------------
    BOOL isShort(unsigned instr) { return ((OpcodeInfo[instr].Type & 16) != 0); };
    unsigned ShortOf(unsigned opcode);
    void SetErrorReporter(ErrorReporter* aReport) { report = aReport; if(m_pManifest) m_pManifest->SetErrorReporter(aReport); }

    void StartNameSpace(__in __nullterminated char* name);
    void EndNameSpace();
    void StartClass(__in __nullterminated char* name, DWORD attr, TyParList *typars);
    DWORD CheckClassFlagsIfNested(Class* pEncloser, DWORD attr);
    void AddClass();
    void EndClass();
    void StartMethod(__in __nullterminated char* name, BinStr* sig, CorMethodAttr flags, BinStr* retMarshal, DWORD retAttr, TyParList *typars = NULL);
    void EndMethod();

    void AddField(__inout_z __inout char* name, BinStr* sig, CorFieldAttr flags, __in __nullterminated char* rvaLabel, BinStr* pVal, ULONG ulOffset);
	BOOL EmitField(FieldDescriptor* pFD);
    void EmitByte(int val);
    //void EmitTry(enum CorExceptionFlag kind, char* beginLabel, char* endLabel, char* handleLabel, char* filterOrClass);
    void EmitMaxStack(unsigned val);
    void EmitLocals(BinStr* sig);
    void EmitEntryPoint();
    void EmitZeroInit();
    void SetImplAttr(unsigned short attrval);

    // Emits zeros if the buffer parameter is NULL.
    void EmitData(__in_opt void *buffer, unsigned len);

    void EmitDD(__in __nullterminated char *str);
    void EmitDataString(BinStr* str);

    void EmitInstrVar(Instr* instr, int var);
    void EmitInstrVarByName(Instr* instr, __in __nullterminated char* label);
    void EmitInstrI(Instr* instr, int val);
    void EmitInstrI8(Instr* instr, __int64* val);
    void EmitInstrR(Instr* instr, double* val);
    void EmitInstrBrOffset(Instr* instr, int offset);
    void EmitInstrBrTarget(Instr* instr, __in __nullterminated char* label);
    mdToken MakeMemberRef(mdToken typeSpec, __in __nullterminated char* name, BinStr* sig);
    mdToken MakeMethodSpec(mdToken tkParent, BinStr* sig);
    void SetMemberRefFixup(mdToken tk, unsigned opcode_len);
    mdToken MakeTypeRef(mdToken tkResScope, LPCUTF8 szFullName);
    void EmitInstrStringLiteral(Instr* instr, BinStr* literal, BOOL ConvertToUnicode, BOOL Swap = FALSE);
    void EmitInstrSig(Instr* instr, BinStr* sig);
    void EmitInstrSwitch(Instr* instr, Labels* targets);
    void EmitLabel(__in __nullterminated char* label);
    void EmitDataLabel(__in __nullterminated char* label);

    unsigned OpcodeLen(Instr* instr); //returns opcode length
    // Emit just the opcode (no parameters to the instruction stream.
    void EmitOpcode(Instr* instr);

    // Emit primitive types to the instruction stream.
    void EmitBytes(BYTE*, unsigned len);

    ErrorReporter* report;

	BOOL EmitFieldsMethods(Class* pClass);
	BOOL EmitEventsProps(Class* pClass);

    // named args/vars paraphernalia:
public:
    void addArgName(__in_opt __nullterminated char *szNewName, BinStr* pbSig, BinStr* pbMarsh, DWORD dwAttr)
    {
        if(pbSig && (*(pbSig->ptr()) == ELEMENT_TYPE_VOID))
            report->error("Illegal use of type 'void'\n");
        if(m_lastArgName)
        {
            m_lastArgName->pNext = new ARG_NAME_LIST(m_lastArgName->nNum+1,szNewName,pbSig,pbMarsh,dwAttr);
            m_lastArgName = m_lastArgName->pNext;
        }
        else
        {
            m_lastArgName = new ARG_NAME_LIST(0,szNewName,pbSig,pbMarsh,dwAttr);
            m_firstArgName = m_lastArgName;
        }
    };
    ARG_NAME_LIST *getArgNameList(void)
    { ARG_NAME_LIST *pRet = m_firstArgName; m_firstArgName=NULL; m_lastArgName=NULL; return pRet;};
    // Added because recursive destructor of ARG_NAME_LIST may overflow the system stack
    void delArgNameList(ARG_NAME_LIST *pFirst)
    {
        ARG_NAME_LIST *pArgList=pFirst, *pArgListNext;
        for(; pArgList; pArgListNext=pArgList->pNext,
                        delete pArgList,
                        pArgList=pArgListNext);
    };

    ARG_NAME_LIST   *findArg(ARG_NAME_LIST *pFirst, int num)
    {
        ARG_NAME_LIST *pAN;
        for(pAN=pFirst; pAN; pAN = pAN->pNext)
        {
            if(pAN->nNum == num) return pAN;
        }
        return NULL;
    };
    ARG_NAME_LIST *m_firstArgName;
    ARG_NAME_LIST *m_lastArgName;
    void ResetArgNameList();

    // Structured exception handling paraphernalia:
public:
    SEH_Descriptor  *m_SEHD;    // current descriptor ptr
    void NewSEHDescriptor(void); //sets m_SEHD
    void SetTryLabels(__in __nullterminated char * szFrom, __in __nullterminated char *szTo);
    void SetFilterLabel(__in __nullterminated char *szFilter);
    void SetCatchClass(mdToken catchClass);
    void SetHandlerLabels(__in __nullterminated char *szHandlerFrom, __in __nullterminated char *szHandlerTo);
    void EmitTry(void);         //uses m_SEHD

//private:
    SEHD_Stack  m_SEHDstack;

    // Events and Properties paraphernalia:
public:
    void EndEvent(void);    //emits event definition
    void EndProp(void);     //emits property definition
    void ResetEvent(__inout_z __inout char * szName, mdToken typeSpec, DWORD dwAttr);
    void ResetProp(__inout_z __inout char * szName, BinStr* bsType, DWORD dwAttr, BinStr* bsValue);
    void SetEventMethod(int MethodCode, mdToken tk);
    void SetPropMethod(int MethodCode, mdToken tk);
    BOOL EmitEvent(EventDescriptor* pED);   // impl. in ASSEM.CPP
    BOOL EmitProp(PropDescriptor* pPD); // impl. in ASSEM.CPP
    EventDescriptor*    m_pCurEvent;
    PropDescriptor*     m_pCurProp;

private:
    MemberRefDList           m_LocalMethodRefDList;
    MemberRefDList           m_LocalFieldRefDList;
    LocalMemberRefFixupList  m_LocalMemberRefFixupList;
    MethodBodyList           m_MethodBodyList;
    MemberRefDList           m_MethodSpecList;
public:
    HRESULT ResolveLocalMemberRefs();
    HRESULT DoLocalMemberRefFixups();
    mdToken ResolveLocalMemberRef(mdToken tok);

    // PInvoke paraphernalia
public:
    PInvokeDescriptor*  m_pPInvoke;
    ImportList  m_ImportList;
    void SetPinvoke(BinStr* DllName, int Ordinal, BinStr* Alias, int Attrs);
    HRESULT EmitPinvokeMap(mdToken tk, PInvokeDescriptor* pDescr);
    ImportDescriptor* EmitImport(BinStr* DllName);
    void EmitImports();

    // Debug metadata paraphernalia
public:
    ULONG m_ulCurLine; // set by Parser
    ULONG m_ulCurColumn; // set by Parser
    ULONG m_ulLastDebugLine;
    ULONG m_ulLastDebugColumn;
    ULONG m_ulLastDebugLineEnd;
    ULONG m_ulLastDebugColumnEnd;
    DWORD m_dwIncludeDebugInfo;
    BOOL  m_fGeneratePDB;
    char m_szSourceFileName[MAX_FILENAME_LENGTH*3+1];
    WCHAR m_wzOutputFileName[MAX_FILENAME_LENGTH];
    WCHAR m_wzSourceFileName[MAX_FILENAME_LENGTH];
	GUID	m_guidLang;
	GUID	m_guidLangVendor;
	GUID	m_guidDoc;

    // Portable PDB paraphernalia
public:
    PortablePdbWriter* m_pPortablePdbWriter;
    char                m_szPdbFileName[MAX_FILENAME_LENGTH * 3 + 1];
    WCHAR               m_wzPdbFileName[MAX_FILENAME_LENGTH];

    // Sets the pdb file name of the assembled file.
    void SetPdbFileName(__in __nullterminated char* szName);
    // Saves the pdb file.
    HRESULT SavePdbFile();

    // Security paraphernalia
public:
    void AddPermissionDecl(CorDeclSecurity action, mdToken type, NVPair *pairs)
    {
        PermissionDecl *decl = new PermissionDecl(action, type, pairs);
        if(decl==NULL)
        {
            report->error("\nOut of memory!\n");
            return;
        }
        if (m_pCurMethod) {
            decl->m_Next = m_pCurMethod->m_pPermissions;
            m_pCurMethod->m_pPermissions = decl;
        } else if (m_pCurClass) {
            decl->m_Next = m_pCurClass->m_pPermissions;
            m_pCurClass->m_pPermissions = decl;
        } else if (m_pManifest && m_pManifest->m_pAssembly) {
            decl->m_Next = m_pManifest->m_pAssembly->m_pPermissions;
            m_pManifest->m_pAssembly->m_pPermissions = decl;
        } else {
            report->error("Cannot declare security permissions without the owner\n");
            delete decl;
        }
    };

    void AddPermissionDecl(CorDeclSecurity action, mdToken type, BinStr *pbsPairs)
    {
        PermissionDecl *decl = new PermissionDecl(action, type, pbsPairs);
        if(decl==NULL)
        {
            report->error("\nOut of memory!\n");
            return;
        }
        if (m_pCurMethod) {
            decl->m_Next = m_pCurMethod->m_pPermissions;
            m_pCurMethod->m_pPermissions = decl;
        } else if (m_pCurClass) {
            decl->m_Next = m_pCurClass->m_pPermissions;
            m_pCurClass->m_pPermissions = decl;
        } else if (m_pManifest && m_pManifest->m_pAssembly) {
            decl->m_Next = m_pManifest->m_pAssembly->m_pPermissions;
            m_pManifest->m_pAssembly->m_pPermissions = decl;
        } else {
            report->error("Cannot declare security permissions without the owner\n");
            delete decl;
        }
    };

    void AddPermissionSetDecl(CorDeclSecurity action, BinStr *value)
    {
        PermissionSetDecl *decl = new PermissionSetDecl(action, value);
        if(decl==NULL)
        {
            report->error("\nOut of memory!\n");
            return;
        }
        if (m_pCurMethod) {
            decl->m_Next = m_pCurMethod->m_pPermissionSets;
            m_pCurMethod->m_pPermissionSets = decl;
        } else if (m_pCurClass) {
            decl->m_Next = m_pCurClass->m_pPermissionSets;
            m_pCurClass->m_pPermissionSets = decl;
        } else if (m_pManifest && m_pManifest->m_pAssembly) {
            decl->m_Next = m_pManifest->m_pAssembly->m_pPermissionSets;
            m_pManifest->m_pAssembly->m_pPermissionSets = decl;
        } else {
            report->error("Cannot declare security permission sets without the owner\n");
            delete decl;
        }
    };
    void EmitSecurityInfo(mdToken           token,
                          PermissionDecl*   pPermissions,
                          PermissionSetDecl*pPermissionSets);
    BinStr* EncodeSecAttr(__in __nullterminated char* szReflName, BinStr* pbsSecAttrBlob, unsigned nProps);

    HRESULT AllocateStrongNameSignature();

    // Custom values paraphernalia:
public:
    mdToken m_tkCurrentCVOwner;
    CustomDescrList* m_pCustomDescrList;
    CustomDescrListStack m_CustomDescrListStack;
    CustomDescrList  m_CustomDescrList;

    void DefineCV(CustomDescr* pCD)
    {
        if(pCD)
        {
            ULONG           cTemp = 0;
            void *          pBlobBody = NULL;
            mdToken         cv;
            mdToken tkOwnerType, tkTypeType = TypeFromToken(pCD->tkType);

            if((tkTypeType != 0x99000000)&&(tkTypeType != 0x98000000))
            {
                tkOwnerType = TypeFromToken(pCD->tkOwner);
                if((tkOwnerType != 0x99000000)&&(tkOwnerType != 0x98000000))
                {
                    if(pCD->pBlob)
                    {
                        pBlobBody = (void *)(pCD->pBlob->ptr());
                        cTemp = pCD->pBlob->length();
                    }
                    if (pCD->tkInterfacePair)
                    {
                        pCD->tkOwner = GetInterfaceImpl(pCD->tkOwner, pCD->tkInterfacePair);
                    }
                    m_pEmitter->DefineCustomAttribute(pCD->tkOwner,pCD->tkType,pBlobBody,cTemp,&cv);

                    delete pCD;
                    return;
                }
            }
            m_CustomDescrList.PUSH(pCD);
        }
    };
    void EmitCustomAttributes(mdToken tok, CustomDescrList* pCDL)
    {
        CustomDescr *pCD;
        if(pCDL == NULL || RidFromToken(tok)==0) return;
        while((pCD = pCDL->POP()))
        {
            pCD->tkOwner = tok;
            DefineCV(pCD);
        }
    };
    void EmitUnresolvedCustomAttributes(); // implementation: writer.cpp
    // VTable blob (if any)
public:
    BinStr *m_pVTable;
    // Field marshaling
    BinStr *m_pMarshal;
    // VTable fixup list
    VTFList m_VTFList;
	// Export Address Table entries list
	EATList m_EATList;
	HRESULT CreateExportDirectory();
	DWORD	EmitExportStub(DWORD dwVTFSlotRVA);

    // Method implementation paraphernalia:
private:
    MethodImplDList m_MethodImplDList;
public:
    void AddMethodImpl(mdToken tkImplementedTypeSpec, __in __nullterminated char* szImplementedName, BinStr* pImplementedSig,
                    mdToken tkImplementingTypeSpec, __in_opt __nullterminated char* szImplementingName, BinStr* pImplementingSig);
    BOOL EmitMethodImpls();
    // source file name paraphernalia
    BOOL m_fSourceFileSet;
    void SetSourceFileName(__in __nullterminated char* szName);
    void SetSourceFileName(BinStr* pbsName);
    // header flags
    DWORD   m_dwSubsystem;
    WORD    m_wSSVersionMajor;
    WORD    m_wSSVersionMinor;
    DWORD   m_dwComImageFlags;
	DWORD	m_dwFileAlignment;
	ULONGLONG	m_stBaseAddress;
    size_t  m_stSizeOfStackReserve;
    DWORD   m_dwCeeFileFlags;
    WORD    m_wMSVmajor;
    WORD    m_wMSVminor;
    BOOL    m_fAppContainer;
    BOOL    m_fHighEntropyVA;

    // Former globals
    WCHAR *m_wzResourceFile;
    WCHAR *m_wzKeySourceName;
    bool OnErrGo;
    void SetCodePage(unsigned val) { g_uCodePage = val; };
    Clockwork* bClock;
    void SetClock(Clockwork* val) { bClock = val; };

    // Syntactic sugar paraphernalia
private:
    TypeDefDList m_TypeDefDList;
public:
    void AddTypeDef(BinStr* pbsTypeSpec, __in_z __in char* szName)
    {
        m_TypeDefDList.PUSH(new TypeDefDescr(szName, pbsTypeSpec, ResolveTypeSpec(pbsTypeSpec)));
    };
    void AddTypeDef(mdToken tkTypeSpec, __in_z __in char* szName)
    {
        m_TypeDefDList.PUSH(new TypeDefDescr(szName, NULL, tkTypeSpec));
    };
    void AddTypeDef(CustomDescr* pCA, __in_z __in char* szName)
    {
        TypeDefDescr* pNew = new TypeDefDescr(szName,NULL,mdtCustomAttribute);
        pNew->m_pCA = pCA;
        m_TypeDefDList.PUSH(pNew);
    };
    TypeDefDescr* FindTypeDef(__in_z __in char* szName)
    {
        CHECK_LOCAL_STATIC_VAR(static TypeDefDescr X(NULL, NULL, 0));

        X.m_szName = szName;
        TypeDefDescr* Y = m_TypeDefDList.FIND(&X);
        X.m_szName = NULL; // to avoid deletion when X goes out of scope
        return Y;
        //return m_TypeDefDList.FIND(szName);
    };
    unsigned NumTypeDefs() {return m_TypeDefDList.COUNT();};
private:
    HRESULT GetCAName(mdToken tkCA, __out LPWSTR *ppszName);

public:
    void RecordTypeConstraints(GenericParamConstraintList* pGPCList, int numTyPars, TyParDescr* tyPars);

    void AddGenericParamConstraint(int index, char * pStrGenericParam, mdToken tkTypeConstraint);

    void CheckAddGenericParamConstraint(GenericParamConstraintList* pGPCList, int index, mdToken tkTypeConstraint, bool isParamDirective);

    void EmitGenericParamConstraints(int numTyPars, TyParDescr* pTyPars, mdToken tkOwner, GenericParamConstraintList* pGPCL);

};

#endif  // Assember_h

#ifdef _MSC_VER
#pragma warning(default : 4640)
#endif


