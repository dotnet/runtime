// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CLASS.H

#ifndef CLASSCOMPAT_H
#define CLASSCOMPAT_H

#ifdef FEATURE_COMINTEROP

/*
 *  Include Files
 */
#include "eecontract.h"
#include "argslot.h"
#include "vars.hpp"
#include "cor.h"
#include "clrex.h"
#include "hash.h"
#include "crst.h"
#include "cgensys.h"
#include "stdinterfaces.h"
#include "slist.h"
#include "spinlock.h"
#include "typehandle.h"
#include "methodtable.h"
#include "eeconfig.h"
#include "typectxt.h"
#include "stackingallocator.h"
#include "class.h"

/*
 *  Forward declarations
 */
class   AppDomain;
class   ArrayClass;
class   ArrayMethodDesc;
class   Assembly;
class   ClassLoader;
class   FCallMethodDesc;
class   EEClass;
class   LayoutEEClass;
class   EnCFieldDesc;
class   FieldDesc;
struct  LayoutRawFieldInfo;
class   MetaSig;
class   MethodDesc;
class   MethodDescChunk;
class   MethodNameHash;
class   MethodTable;
class   Module;
struct  ModuleCtorInfo;
class   Object;
class   Stub;
class   Substitution;
class   SystemDomain;
class   TypeHandle;
class   AllocMemTracker;
class   ZapCodeMap;
class   InteropMethodTableSlotDataMap;
class   LoadingEntry_LockHolder;
class   DispatchMapBuilder;

namespace ClassCompat
{

//*******************************************************************************
// workaround: These classification bits need cleanup bad: for now, this gets around
// IJW setting both mdUnmanagedExport & mdPinvokeImpl on expored methods.
#define IsReallyMdPinvokeImpl(x) ( ((x) & mdPinvokeImpl) && !((x) & mdUnmanagedExport) )

//*******************************************************************************
//
// The MethodNameHash is a temporary loader structure which may be allocated if there are a large number of
// methods in a class, to quickly get from a method name to a MethodDesc (potentially a chain of MethodDescs).
//

//*******************************************************************************
// Entry in the method hash table
class MethodHashEntry
{
public:
    MethodHashEntry *   m_pNext;        // Next item with same hash value
    DWORD               m_dwHashValue;  // Hash value
    MethodDesc *        m_pDesc;
    LPCUTF8             m_pKey;         // Method name
};

//*******************************************************************************
class MethodNameHash
{
public:

    MethodHashEntry **m_pBuckets;       // Pointer to first entry for each bucket
    DWORD             m_dwNumBuckets;
    BYTE *            m_pMemory;        // Current pointer into preallocated memory for entries
    BYTE *            m_pMemoryStart;   // Start pointer of pre-allocated memory fo entries
    MethodNameHash   *m_pNext;          // Chain them for stub dispatch lookup
    INDEBUG( BYTE *            m_pDebugEndMemory; )

    MethodNameHash()
    {
        LIMITED_METHOD_CONTRACT;
        m_pMemoryStart = NULL;
        m_pNext = NULL;
    }

    ~MethodNameHash()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_pMemoryStart != NULL)
            delete(m_pMemoryStart);
    }

    // Throws on error
    void Init(DWORD dwMaxEntries, StackingAllocator *pAllocator = NULL);

    // Insert new entry at head of list
    void Insert(
        LPCUTF8 pszName,
        MethodDesc *pDesc);

    // Return the first MethodHashEntry with this name, or NULL if there is no such entry
    MethodHashEntry *Lookup(
        LPCUTF8 pszName,
        DWORD dwHash);

    void SetNext(MethodNameHash *pNext) { m_pNext = pNext; }
    MethodNameHash *GetNext() { return m_pNext; }
};


//*******************************************************************************
//
// This structure is used only when the classloader is building the interface map.  Before the class
// is resolved, the EEClass contains an array of these, which are all interfaces *directly* declared
// for this class/interface by the metadata - inherited interfaces will not be present if they are
// not specifically declared.
//
// This structure is destroyed after resolving has completed.
//
typedef struct
{
    // The interface method table; for instantiated interfaces, this is the generic interface
    MethodTable     *m_pMethodTable;
} BuildingInterfaceInfo_t;

//*******************************************************************************
struct InterfaceInfo_t
{
    enum {
        interface_declared_on_class = 0x1,
        interface_implemented_on_parent = 0x2,
    };

    MethodTable* m_pMethodTable;        // Method table of the interface
    WORD         m_wFlags;

private:
    WORD         m_wStartSlot;          // starting slot of interface in vtable

public:
    WORD         GetInteropStartSlot()
    {
        return m_wStartSlot;
    }
    void         SetInteropStartSlot(WORD wStartSlot)
    {
        m_wStartSlot = wStartSlot;
    }

    BOOL         IsDeclaredOnClass()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_wFlags & interface_declared_on_class);
    }

    BOOL         IsImplementedByParent()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_wFlags & interface_implemented_on_parent);
    }
};

//*******************************************************************************
// MethodTableBuilder simply acts as a holder for the
// large algorithm that "compiles" a type into
// a MethodTable/EEClass/DispatchMap/VTable etc. etc.
//
// The user of this class (the ClassLoader) currently builds the EEClass
// first, and does a couple of other things too, though all
// that work should probably be folded into BuildMethodTableThrowing.
//
class MethodTableBuilder
{
public:
    MethodTableBuilder(MethodTable * pMT, StackingAllocator *pStackingAllocator)
    {
        LIMITED_METHOD_CONTRACT;
        m_pHalfBakedMT = pMT;
        m_pHalfBakedClass = pMT->GetClass();
        m_pStackingAllocator = pStackingAllocator;
        NullBMTData();
    }
public:

    // This method is purely for backward compatibility of COM Interop, and its
    // implementation can be found in ClassCompat.cpp
    InteropMethodTableData *BuildInteropVTable(AllocMemTracker *pamTracker);
    InteropMethodTableData *BuildInteropVTableForArray(AllocMemTracker *pamTracker);

    LPCWSTR GetPathForErrorMessages();

private:
    enum e_METHOD_IMPL
    {
        METHOD_IMPL_NOT,
#ifndef STUB_DISPATCH_ALL
        METHOD_IMPL,
#endif
        METHOD_IMPL_COUNT
    };

    enum e_METHOD_TYPE
    {
        METHOD_TYPE_NORMAL,
        METHOD_TYPE_FCALL,
        METHOD_TYPE_EEIMPL,
        METHOD_TYPE_NDIRECT,
        METHOD_TYPE_INTEROP,
        METHOD_TYPE_INSTANTIATED,
        METHOD_TYPE_COUNT
    };

private:
    // <NICE> Get rid of this.</NICE>
    EEClass *m_pHalfBakedClass;
    MethodTable * m_pHalfBakedMT;
    StackingAllocator *m_pStackingAllocator;

    StackingAllocator* GetStackingAllocator() { return m_pStackingAllocator; }

    // GetHalfBakedClass: The EEClass you get back from this function may not have all its fields filled in yet.
    // Thus you have to make sure that the relevant item which you are accessing has
    // been correctly initialized in the EEClass/MethodTable construction sequence
    // at the point at which you access it.
    //
    // Gradually we will move the code to a model where the process of constructing an EEClass/MethodTable
    // is more obviously correct, e.g. by relying much less on reading information using GetHalfBakedClass
    // and GetHalfBakedMethodTable.
    //
    // <NICE> Get rid of this.</NICE>
    EEClass *GetHalfBakedClass() { LIMITED_METHOD_CONTRACT; return m_pHalfBakedClass; }
    MethodTable *GetHalfBakedMethodTable() { WRAPPER_NO_CONTRACT; return m_pHalfBakedMT; }

    mdTypeDef GetCl()    { LIMITED_METHOD_CONTRACT; return bmtType->cl; }
    BOOL IsGlobalClass() { WRAPPER_NO_CONTRACT; return GetCl() == COR_GLOBAL_PARENT_TOKEN; }
    BOOL IsEnum() { LIMITED_METHOD_CONTRACT; return bmtProp->fIsEnum; }
    DWORD GetAttrClass() { LIMITED_METHOD_CONTRACT; return bmtType->dwAttr; }
    BOOL IsInterface() { WRAPPER_NO_CONTRACT; return IsTdInterface(GetAttrClass()); }
    BOOL IsValueClass() { LIMITED_METHOD_CONTRACT; return bmtProp->fIsValueClass; }
    BOOL IsAbstract() { LIMITED_METHOD_CONTRACT; return IsTdAbstract(bmtType->dwAttr); }
    BOOL HasLayout() { LIMITED_METHOD_CONTRACT; return bmtProp->fHasLayout; }
    BOOL IsDelegate() { LIMITED_METHOD_CONTRACT; return bmtProp->fIsDelegate; }
    Module *GetModule() { LIMITED_METHOD_CONTRACT; return bmtType->pModule; }
    Assembly *GetAssembly() { WRAPPER_NO_CONTRACT; return GetModule()->GetAssembly(); }
    ClassLoader *GetClassLoader() { WRAPPER_NO_CONTRACT; return GetModule()->GetClassLoader(); }
    IMDInternalImport* GetMDImport()  { WRAPPER_NO_CONTRACT; return GetModule()->GetMDImport(); }
#ifdef _DEBUG
    LPCUTF8 GetDebugClassName() { LIMITED_METHOD_CONTRACT; return bmtProp->szDebugClassName; }
#endif // _DEBUG
     BOOL IsComImport() { WRAPPER_NO_CONTRACT; return IsTdImport(GetAttrClass()); }
    BOOL IsComClassInterface() { LIMITED_METHOD_CONTRACT; return bmtProp->fIsComClassInterface; }

    // <NOTE> The following functions are used during MethodTable construction to setup information
    // about the type being constructedm in particular information stored in the EEClass.
    // USE WITH CAUTION!!  TRY NOT TO ADD MORE OF THESE!! </NOTE>
    //
    // <NICE> Get rid of all of these - we should be able to evaluate these conditions BEFORE
    // we create the EEClass object, and thus set the flags immediately at the point
    // we create that object.</NICE>
    void SetIsValueClass() { LIMITED_METHOD_CONTRACT; bmtProp->fIsValueClass = TRUE; }
    void SetEnum() { LIMITED_METHOD_CONTRACT; bmtProp->fIsEnum = TRUE; }
    void SetHasLayout() { LIMITED_METHOD_CONTRACT; bmtProp->fHasLayout = TRUE; }
    void SetIsDelegate() { LIMITED_METHOD_CONTRACT; bmtProp->fIsDelegate = TRUE; }
#ifdef _DEBUG
    void SetDebugClassName(LPUTF8 x) { LIMITED_METHOD_CONTRACT; bmtProp->szDebugClassName = x; }
#endif
     void SetIsComClassInterface() { LIMITED_METHOD_CONTRACT; bmtProp->fIsComClassInterface = TRUE; }

    /************************************
     *  PRIVATE INTERNAL STRUCTS
     ************************************/
private:
    struct bmtErrorInfo
    {
        UINT resIDWhy;
        LPCUTF8 szMethodNameForError;
        mdToken dMethodDefInError;
        Module* pModule;
        mdTypeDef cl;
        OBJECTREF *pThrowable;

        // Set the reason and the offending method def. If the method information
        // is not from this class set the method name and it will override the method def.
        inline bmtErrorInfo() : resIDWhy(0), szMethodNameForError(NULL), dMethodDefInError(mdMethodDefNil), pThrowable(NULL) {LIMITED_METHOD_CONTRACT; }
    };

    struct bmtProperties
    {
        BOOL fSparse;                           // Set to true if a sparse interface is being used.

         // Com Interop, ComWrapper classes extend from ComObject
        BOOL fIsComObjectType;                  // whether this class is an instance of ComObject class

        BOOL fIsMngStandardItf;                 // Set to true if the interface is a manages standard interface.
        BOOL fComEventItfType;                  // Set to true if the class is a special COM event interface.

        BOOL fIsValueClass;
        BOOL fIsEnum;
        BOOL fIsComClassInterface;
        BOOL fHasLayout;
        BOOL fIsDelegate;

        LPUTF8 szDebugClassName;

        inline bmtProperties()
        {
            LIMITED_METHOD_CONTRACT;
            memset((void *)this, NULL, sizeof(*this));
        }
    };

    struct bmtVtable
    {
        WORD wCurrentVtableSlot;
        WORD wCurrentNonVtableSlot;

        // Temporary vtable - use GetMethodDescForSlot/SetMethodDescForSlot for access.
        // pVtableMD is initialized lazily from pVtable
        // pVtable is invalidated if the slot is overwritten.
        PCODE* pVtable;
        MethodDesc** pVtableMD;
        MethodTable *pParentMethodTable;

        MethodDesc** pNonVtableMD;
        InteropMethodTableSlotData **ppSDVtable;
        InteropMethodTableSlotData **ppSDNonVtable;
        DWORD dwMaxVtableSize;                  // Upper bound on size of vtable
        InteropMethodTableSlotDataMap *pInteropData;

        DispatchMapBuilder *pDispatchMapBuilder;

        MethodDesc* GetMethodDescForSlot(WORD slot)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
            }
            CONTRACTL_END;
            if (pVtable[slot] != NULL && pVtableMD[slot] == NULL)
                pVtableMD[slot] = pParentMethodTable->GetMethodDescForSlot(slot);
            _ASSERTE((pVtable[slot] == NULL) ||
                (MethodTable::GetMethodDescForSlotAddress(pVtable[slot]) == pVtableMD[slot]));
            return pVtableMD[slot];
        }

        void SetMethodDescForSlot(WORD slot, MethodDesc* pMD)
        {
            WRAPPER_NO_CONTRACT;
            pVtable[slot] = NULL;
            pVtableMD[slot] = pMD;
        }

        inline bmtVtable() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };

    struct bmtParentInfo
    {
        WORD wNumParentInterfaces;
        MethodDesc **ppParentMethodDescBuf;     // Cache for declared methods
        MethodDesc **ppParentMethodDescBufPtr;  // Pointer for iterating over the cache

        MethodNameHash *pParentMethodHash;
        Substitution parentSubst;
        MethodTable *pParentMethodTable;
        mdToken token;

        inline bmtParentInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };

    struct bmtInterfaceInfo
    {
        DWORD dwTotalNewInterfaceMethods;
        InterfaceInfo_t *pInterfaceMap;         // Temporary interface map

        // ppInterfaceSubstitutionChains[i][0] holds the primary substitution for each interface
        // ppInterfaceSubstitutionChains[i][0..depth[i] ] is the chain of substitutions for each interface
        Substitution **ppInterfaceSubstitutionChains;

        DWORD *pdwOriginalStart;                // If an interface is moved this is the original starting location.
        WORD  wInterfaceMapSize;                // # members in interface map
        DWORD dwLargestInterfaceSize;           // # members in largest interface we implement
        DWORD dwMaxExpandedInterfaces;          // Upper bound on size of interface map
        MethodDesc **ppInterfaceMethodDescList; // List of MethodDescs for current interface
        MethodDesc **ppInterfaceDeclMethodDescList; // List of MethodDescs for the interface itself

        MethodDesc ***pppInterfaceImplementingMD; // List of MethodDescs that implement interface methods
        MethodDesc ***pppInterfaceDeclaringMD;    // List of MethodDescs from the interface itself

        inline bmtInterfaceInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };

    struct bmtMethodInfo
    {
        DWORD                           cMethAndGaps;                       // # meta-data methods of this class ( including the gaps )

        WORD                            cMethods;                           // # meta-data methods of this class
        mdToken *                       rgMethodTokens;                     // Enumeration of metadata methods
        DWORD   *                       rgMethodAttrs;                      // Enumeration of the attributes of the methods
        DWORD   *                       rgMethodImplFlags;                  // Enumeration of the method implementation flags
        ULONG   *                       rgMethodRVA;                        // Enumeration of the method RVA's
        DWORD   *                       rgMethodClassifications;            // Enumeration of the method classifications
        LPCSTR  *                       rgszMethodName;                     // Enumeration of the method names
        BYTE    *                       rgMethodImpl;                       // Enumeration of impl value
        BYTE    *                       rgMethodType;                       // Enumeration of type value

        HENUMInternalHolder             hEnumMethod;

        MethodDesc **                   ppUnboxMethodDescList;              // Keep track unboxed entry points (for value classes)
        MethodDesc **                   ppMethodDescList;                   // MethodDesc pointer for each member

        inline bmtMethodInfo(IMDInternalImport *pMDImport)
            : cMethAndGaps(0),
              cMethods(0),
              rgMethodTokens(NULL),
              rgMethodAttrs(NULL),
              rgMethodImplFlags(NULL),
              rgMethodRVA(NULL),
              rgMethodClassifications(NULL),
              rgszMethodName(NULL),
              rgMethodImpl(NULL),
              hEnumMethod(pMDImport),
              ppUnboxMethodDescList(NULL),
              ppMethodDescList(NULL)
        {
            WRAPPER_NO_CONTRACT;
        }

        inline void SetMethodData(int idx,
            mdToken tok,
            DWORD dwAttrs,
            DWORD dwRVA,
            DWORD dwImplFlags,
            DWORD classification,
            LPCSTR szMethodName,
            BYTE  impl,
            BYTE  type)
        {
            LIMITED_METHOD_CONTRACT;
            rgMethodTokens[idx] = tok;
            rgMethodAttrs[idx] = dwAttrs;
            rgMethodRVA[idx] = dwRVA;
            rgMethodImplFlags[idx] = dwImplFlags;
            rgMethodClassifications[idx] = classification;
            rgszMethodName[idx] = szMethodName;
            rgMethodImpl[idx] = impl;
            rgMethodType[idx] = type;
        }
    };

    struct bmtTypeInfo
    {
        IMDInternalImport * pMDImport;
        Module *            pModule;
        mdToken             cl;
        DWORD               dwAttr;

        inline bmtTypeInfo() { LIMITED_METHOD_CONTRACT; memset((void *)this, NULL, sizeof(*this)); }
    };

    struct bmtMethodImplInfo
    {
        DWORD                           dwNumberMethodImpls;  // Number of method impls defined for this type
        HENUMInternalMethodImplHolder   hEnumMethodImpl;

        struct MethodImplTokenPair
        {
            mdToken methodBody;             // MethodDef's for the bodies of MethodImpls. Must be defined in this type.
            mdToken methodDecl;             // Method token that body implements. Is a MethodDef or MemberRef
            static int __cdecl Compare(const void *elem1, const void *elem2);
            static BOOL Equal(const MethodImplTokenPair *elem1, const MethodImplTokenPair *elem2);
        };

        MethodImplTokenPair *           rgMethodImplTokens;
        Substitution *                  pMethodDeclSubsts;    // Used to interpret generic variables in the interface of the declaring type

        DWORD            pIndex;     // Next open spot in array, we load the BodyDesc's up in order of appearance in the
                                     // type's list of methods (a body can appear more then once in the list of MethodImpls)
        struct Entry
        {
            mdToken      declToken;  // Either the token or the method desc is set for the declaration
            Substitution declSubst;  // Signature instantiations of parent types for Declaration (NULL if not instantiated)
            MethodDesc*  pDeclDesc;  // Method descs for Declaration. If null then Declaration is in this type and use the token
            MethodDesc*  pBodyDesc;  // Method descs created for Method impl bodies
            DWORD        dwFlags;
        };

        Entry *rgEntries;

        void AddMethod(MethodDesc* pImplDesc, MethodDesc* pDeclDesc, mdToken mdDecl, Substitution *pDeclSubst);

        MethodDesc* GetDeclarationMethodDesc(DWORD i)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(i < pIndex);
            return rgEntries[i].pDeclDesc;
        }

        mdToken GetDeclarationToken(DWORD i)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(i < pIndex);
            return rgEntries[i].declToken;
        }

        const Substitution *GetDeclarationSubst(DWORD i)
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(i < pIndex);
            return &rgEntries[i].declSubst;
        }

        MethodDesc* GetBodyMethodDesc(DWORD i)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(i < pIndex);
            return rgEntries[i].pBodyDesc;
        }

        // Returns TRUE if tok acts as a body for any methodImpl entry. FALSE, otherwise.
        BOOL IsBody(mdToken tok);

        inline bmtMethodImplInfo(IMDInternalImport * pMDImport)
            : dwNumberMethodImpls(0),
              hEnumMethodImpl(pMDImport),
              pIndex(0),
              rgEntries(NULL)
        {
            LIMITED_METHOD_CONTRACT;
        }
    };

    // The following structs, defined as private members of MethodTableBuilder, contain the necessary local
    // parameters needed for BuildMethodTable

    // Look at the struct definitions for a detailed list of all parameters available
    // to BuildMethodTable.

    bmtErrorInfo *bmtError;
    bmtProperties *bmtProp;
    bmtVtable *bmtVT;
    bmtParentInfo *bmtParent;
    bmtInterfaceInfo *bmtInterface;
    bmtMethodInfo *bmtMethod;
    bmtTypeInfo *bmtType;
    bmtMethodImplInfo *bmtMethodImpl;

    void SetBMTData(
        bmtErrorInfo *bmtError,
        bmtProperties *bmtProp,
        bmtVtable *bmtVT,
        bmtParentInfo *bmtParent,
        bmtInterfaceInfo *bmtInterface,
        bmtMethodInfo *bmtMethod,
        bmtTypeInfo *bmtType,
        bmtMethodImplInfo *bmtMethodImpl);

    void NullBMTData();

    class DeclaredMethodIterator
    {
      private:
        MethodTableBuilder &m_mtb;
        int                 m_idx;

      public:
        inline                  DeclaredMethodIterator(MethodTableBuilder &mtb);
        inline int              CurrentIndex();
        inline BOOL             Next();
        inline mdToken          Token();
        inline DWORD            Attrs();
        inline DWORD            RVA();
        inline DWORD            ImplFlags();
        inline DWORD            Classification();
        inline LPCSTR           Name();
        inline PCCOR_SIGNATURE  GetSig(DWORD *pcbSig);
        inline BYTE             MethodImpl();
        inline BOOL             IsMethodImpl();
        inline BYTE             MethodType();
        inline MethodDesc      *GetMethodDesc();
        inline void             SetMethodDesc(MethodDesc *pMD);
        inline MethodDesc      *GetParentMethodDesc();
        inline void             SetParentMethodDesc(MethodDesc *pMD);
        inline MethodDesc      *GetUnboxedMethodDesc();
    };
    friend class DeclaredMethodIterator;

    inline WORD NumDeclaredMethods() { LIMITED_METHOD_CONTRACT; return bmtMethod->cMethods; }
    inline void  IncNumDeclaredMethods() { LIMITED_METHOD_CONTRACT; bmtMethod->cMethods++; }

private:
    static VOID DECLSPEC_NORETURN BuildMethodTableThrowException(HRESULT hr,
                                              const bmtErrorInfo & bmtError);


    inline VOID DECLSPEC_NORETURN BuildMethodTableThrowException(
                                              HRESULT hr,
                                              UINT idResWhy,
                                              mdMethodDef tokMethodDef)
    {
        STANDARD_VM_CONTRACT;
        bmtError->resIDWhy = idResWhy;
        bmtError->dMethodDefInError = tokMethodDef;
        bmtError->szMethodNameForError = NULL;
        bmtError->cl = GetCl();
        BuildMethodTableThrowException(hr, *bmtError);
    }

    inline VOID DECLSPEC_NORETURN BuildMethodTableThrowException(
        HRESULT hr,
        UINT idResWhy,
        LPCUTF8 szMethodName)
    {
        STANDARD_VM_CONTRACT;
        bmtError->resIDWhy = idResWhy;
        bmtError->dMethodDefInError = mdMethodDefNil;
        bmtError->szMethodNameForError = szMethodName;
        bmtError->cl = GetCl();
        BuildMethodTableThrowException(hr, *bmtError);
    }

    inline VOID DECLSPEC_NORETURN BuildMethodTableThrowException(
                                              UINT idResWhy,
                                              mdMethodDef tokMethodDef = mdMethodDefNil)
    {
        STANDARD_VM_CONTRACT;
        BuildMethodTableThrowException(COR_E_TYPELOAD, idResWhy, tokMethodDef);
    }

    inline VOID DECLSPEC_NORETURN BuildMethodTableThrowException(
        UINT idResWhy,
        LPCUTF8 szMethodName)
    {
        STANDARD_VM_CONTRACT;
        BuildMethodTableThrowException(COR_E_TYPELOAD, idResWhy, szMethodName);
    }

private:
    MethodNameHash *CreateMethodChainHash(
        MethodTable *pMT);

    HRESULT LoaderFindMethodInClass(
        LPCUTF8             pszMemberName,
        Module*             pModule,
        mdMethodDef         mdToken,
        MethodDesc **       ppMethodDesc,
        PCCOR_SIGNATURE *   ppMemberSignature,
        DWORD *             pcMemberSignature,
        DWORD               dwHashName,
        BOOL *              pMethodConstraintsMatch);

    // Finds a method declaration from a MemberRef or Def. It handles the case where
    // the Ref or Def point back to this class even though it has not been fully
    // laid out.
    HRESULT FindMethodDeclarationForMethodImpl(
        IMDInternalImport *pMDInternalImport, // Scope in which tkClass and tkMethod are defined.
        mdTypeDef          tkClass,           // Type that the method def resides in
        mdToken            tkMethod,          // Token that is being located (MemberRef or MethodDef)
        mdToken*           ptkMethodDef);     // Method definition for Member

    // Enumerates the method impl token pairs and resolves the impl tokens to mdtMethodDef
    // tokens, since we currently have the limitation that all impls are in the current class.
    VOID EnumerateMethodImpls();

    VOID EnumerateClassMethods();

    // Allocate temporary memory for tracking all information used in building the MethodTable
    VOID AllocateMethodWorkingMemory();

    VOID BuildInteropVTable_InterfaceList(
        BuildingInterfaceInfo_t **ppBuildingInterfaceList,
        WORD *pcBuildingInterfaceList);

    VOID BuildInteropVTable_PlaceMembers(
        bmtTypeInfo* bmtType,
        DWORD numDeclaredInterfaces,
        BuildingInterfaceInfo_t *pBuildingInterfaceList,
        bmtMethodInfo* bmtMethod,
        bmtErrorInfo* bmtError,
        bmtProperties* bmtProp,
        bmtParentInfo* bmtParent,
        bmtInterfaceInfo* bmtInterface,
        bmtMethodImplInfo* bmtMethodImpl,
        bmtVtable* bmtVT);

    VOID BuildInteropVTable_ResolveInterfaces(
        BuildingInterfaceInfo_t *pBuildingInterfaceList,
        bmtTypeInfo* bmtType,
        bmtInterfaceInfo* bmtInterface,
        bmtVtable* bmtVT,
        bmtParentInfo* bmtParent,
        const bmtErrorInfo & bmtError);

    VOID BuildInteropVTable_CreateInterfaceMap(
        BuildingInterfaceInfo_t *pBuildingInterfaceList,
        bmtInterfaceInfo* bmtInterface,
        WORD *pwInterfaceListSize,
        DWORD *pdwMaxInterfaceMethods,
        MethodTable *pParentMethodTable);

    VOID BuildInteropVTable_ExpandInterface(
        InterfaceInfo_t *pInterfaceMap,
        MethodTable *pNewInterface,
        WORD *pwInterfaceListSize,
        DWORD *pdwMaxInterfaceMethods,
        BOOL fDirect);

    VOID BuildInteropVTable_PlaceVtableMethods(
        bmtInterfaceInfo* bmtInterface,
        DWORD numDeclaredInterfaces,
        BuildingInterfaceInfo_t *pBuildingInterfaceList,
        bmtVtable* bmtVT,
        bmtMethodInfo* bmtMethod,
        bmtTypeInfo* bmtType,
        bmtErrorInfo* bmtError,
        bmtProperties* bmtProp,
        bmtParentInfo* bmtParent);

    VOID BuildInteropVTable_PlaceMethodImpls(
        bmtTypeInfo* bmtType,
        bmtMethodImplInfo* bmtMethodImpl,
        bmtErrorInfo* bmtError,
        bmtInterfaceInfo* bmtInterface,
        bmtVtable* bmtVT,
        bmtParentInfo* bmtParent);

    VOID BuildInteropVTable_PlaceLocalDeclaration(
        mdMethodDef      mdef,
        MethodDesc*      body,
        bmtTypeInfo* bmtType,
        bmtErrorInfo*    bmtError,
        bmtVtable*       bmtVT,
        DWORD*           slots,
        MethodDesc**     replaced,
        DWORD*           pSlotIndex,
        PCCOR_SIGNATURE* ppBodySignature,
        DWORD*           pcBodySignature);

    VOID BuildInteropVTable_PlaceInterfaceDeclaration(
        MethodDesc*       pDecl,
        MethodDesc*       pImplBody,
        const Substitution *pDeclSubst,
        bmtTypeInfo*  bmtType,
        bmtInterfaceInfo* bmtInterface,
        bmtErrorInfo*     bmtError,
        bmtVtable*        bmtVT,
        DWORD*            slots,
        MethodDesc**      replaced,
        DWORD*            pSlotIndex,
        PCCOR_SIGNATURE*  ppBodySignature,
        DWORD*            pcBodySignature);

    VOID BuildInteropVTable_PlaceParentDeclaration(
        MethodDesc*       pDecl,
        MethodDesc*       pImplBody,
        const Substitution *pDeclSubst,
        bmtTypeInfo*  bmtType,
        bmtErrorInfo*     bmtError,
        bmtVtable*        bmtVT,
        bmtParentInfo*    bmtParent,
        DWORD*            slots,
        MethodDesc**      replaced,
        DWORD*            pSlotIndex,
        PCCOR_SIGNATURE*  ppBodySignature,
        DWORD*            pcBodySignature);

    VOID   BuildInteropVTable_PropagateInheritance(
        bmtVtable *bmtVT);

    VOID FinalizeInteropVTable(
        AllocMemTracker *pamTracker,
        LoaderAllocator*,
        bmtVtable*,
        bmtInterfaceInfo*,
        bmtTypeInfo*,
        bmtProperties*,
        bmtMethodInfo*,
        bmtErrorInfo*,
        bmtParentInfo*,
        InteropMethodTableData**);
}; // MethodTableBuilder

}; // Namespace ClassCompat

#endif // FEATURE_COMINTEROP

#endif // !CLASSCOMPAT_H
