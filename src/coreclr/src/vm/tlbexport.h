// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//===========================================================================
// File: TlbExport.h
//

//
// Notes: Create a TypeLib from COM+ metadata.
//---------------------------------------------------------------------------


#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

class ITypeCreateTypeLib2;
struct ICreateTypeInfo2;
struct ITypeInfo;
struct ITypeLibExporterNotifySink;

class CDescPool;
struct ComMTMethodProps;
class ComMTMemberInfoMap;

static LPCSTR szVariantClassFullQual = g_VariantClassName;

//*************************************************************************
// Helper functions.
//*************************************************************************
HRESULT Utf2Quick(
    LPCUTF8     pStr,                   // The string to convert.
    CQuickArray<WCHAR> &rStr,           // The QuickArray<WCHAR> to convert it into.
    int         iCurLen = 0);           // Inital characters in the array to leave (default 0).

//*****************************************************************************
// Signature utilities.
//*****************************************************************************
class MetaSigExport : public MetaSig
{
public:
    MetaSigExport(MethodDesc *pMD) :
        MetaSig(pMD)
    {
        WRAPPER_NO_CONTRACT;
    }
        
    BOOL IsVbRefType()
    {
        CONTRACT (BOOL)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACT_END;
        
        // Get the arg, and skip decorations.
        SigPointer pt = GetArgProps();
        CorElementType mt;
        if (FAILED(pt.PeekElemType(&mt)))
            return FALSE;
        
        while (mt == ELEMENT_TYPE_BYREF || mt == ELEMENT_TYPE_PTR)
        {
            // Eat the one just examined, and peek at the next one.
            if (FAILED(pt.GetElemType(NULL)) || FAILED(pt.PeekElemType(&mt)))
                return FALSE;
        }
        
        // Is it just Object?
        if (mt == ELEMENT_TYPE_OBJECT)
            RETURN TRUE;
        
        // A particular class?
        if (mt == ELEMENT_TYPE_CLASS)
        {
            // Exclude "string".
            if (pt.IsStringType(m_pModule, GetSigTypeContext()))
                RETURN FALSE;
            RETURN TRUE;
        }
        
        // A particular valuetype?
        if (mt == ELEMENT_TYPE_VALUETYPE)
        {
            // Include "variant".
            if (pt.IsClass(m_pModule, szVariantClassFullQual, GetSigTypeContext()))
                RETURN TRUE;
            RETURN FALSE;
        }
        
        // An array, a string, or POD.
        RETURN FALSE;
    }
}; // class MetaSigExport : public MetaSig


//*************************************************************************
// Class to convert COM+ metadata to a TypeLib.
//*************************************************************************
class TypeLibExporter
{
private:
    class CExportedTypesInfo
    {
    public:
        MethodTable*        pClass;   // The class being exported.
        ICreateTypeInfo2*   pCTI;         // The ICreateTypeInfo2 for the EE class.
        ICreateTypeInfo2*   pCTIClassItf; // The ICreateTypeInfo2 for the IClassX.
        TYPEKIND            tkind;        // Typekind of the exported class.
        bool                bAutoProxy;   // If true, oleaut32 is the interface's proxy.
    };

    class CExportedTypesHash : public CClosedHashEx<CExportedTypesInfo, CExportedTypesHash>
    {
    protected:
        friend class CSortByToken;
        friend class CSortByName;

        class CSortByToken : public CQuickSort<CExportedTypesInfo*>
        {
        public:
            CSortByToken(CExportedTypesInfo **pBase, int iCount) :
                CQuickSort<CExportedTypesInfo*>(pBase, iCount)
            {
                WRAPPER_NO_CONTRACT;
            }
            virtual int Compare(CExportedTypesInfo **ps1, CExportedTypesInfo **ps2);
        };
        
        class CSortByName : public CQuickSort<CExportedTypesInfo*>
        {
        public:
            CSortByName(CExportedTypesInfo **pBase, int iCount) :
                CQuickSort<CExportedTypesInfo*>(pBase, iCount)
            {
                WRAPPER_NO_CONTRACT;
            }
            virtual int Compare(CExportedTypesInfo **ps1, CExportedTypesInfo **ps2);
        };

    public:
        typedef CClosedHashEx<CExportedTypesInfo, CExportedTypesHash> Base;
        typedef CExportedTypesInfo T;
        
        CExportedTypesHash() :
            Base(1009),
            m_iCount(0),
            m_Array(NULL)
        {
            WRAPPER_NO_CONTRACT;
        }
            
        ~CExportedTypesHash()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                SO_TOLERANT;
                MODE_ANY;
            }
            CONTRACTL_END;
            Clear();
            delete[] m_Array;
        }

        virtual void Clear();
        
        unsigned int Hash(const T *pData);
        unsigned int Compare(const T *p1, T *p2);
        ELEMENTSTATUS Status(T *p);
        void SetStatus(T *p, ELEMENTSTATUS s);
        void* GetKey(T *p);
        
        //@todo: move to CClosedHashEx
        T* GetFirst()
        {
            WRAPPER_NO_CONTRACT;
            return (T*)CClosedHashBase::GetFirst();
        }
        T* GetNext(T*prev)
        {
            WRAPPER_NO_CONTRACT;
            return (T*)CClosedHashBase::GetNext((BYTE*)prev);
        }
    
        void InitArray();
        void UpdateArray();
        
        T* operator[](ULONG ix)
        {
            CONTRACT (T*)
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                PRECONDITION(ix < m_iCount);
                POSTCONDITION(CheckPointer(RETVAL));
            }
            CONTRACT_END;

            RETURN m_Array[ix];
        }
        int Count()
        {
            LIMITED_METHOD_CONTRACT;
            return m_iCount;
        }

        void SortByName();
        void SortByToken();
        
    protected:
        CExportedTypesInfo**    m_Array;        
        ULONG                   m_iCount;
    };
    
protected:
    struct CErrorContext;

    class CHrefOfTIHashKey
    {
    public:
        ITypeInfo*  pITI;
        HREFTYPE    href;
    };

    class CHrefOfTIHash : public CClosedHash<class CHrefOfTIHashKey>
    {
    public:
        typedef CHrefOfTIHashKey T;

        CHrefOfTIHash() :
            CClosedHash<class CHrefOfTIHashKey>(101)
        {
            WRAPPER_NO_CONTRACT;
        }
        ~CHrefOfTIHash()
        {
            CONTRACTL { NOTHROW; SO_TOLERANT; } CONTRACTL_END;
            Clear();
        }

        virtual void Clear();
        
        unsigned int Hash(const T *pData);
        unsigned int Hash(const void *pData)
        {
            WRAPPER_NO_CONTRACT;
            return Hash((const T*)pData);
        }

        unsigned int Compare(const T *p1, T *p2);
        unsigned int Compare(const void *p1, BYTE *p2)
        {
            WRAPPER_NO_CONTRACT;
            return Compare((const T*)p1, (T*)p2);
        }

        ELEMENTSTATUS Status(T *p);
        ELEMENTSTATUS Status(BYTE *p)
        {
            WRAPPER_NO_CONTRACT;
            return Status((T*)p);
        }

        void SetStatus(T *p, ELEMENTSTATUS s);
        void SetStatus(BYTE *p, ELEMENTSTATUS s)
        {
            WRAPPER_NO_CONTRACT;
            SetStatus((T*)p, s);
        }

        void *GetKey(T *p);
        void* GetKey(BYTE *p)
        {
            WRAPPER_NO_CONTRACT;
            return GetKey((T*)p);
        }
    };

    class CHrefOfClassHashKey
    {
    public:
        MethodTable*    pClass;
        HREFTYPE    href;
    };

    class CHrefOfClassHash : public CClosedHash<class CHrefOfClassHashKey>
    {
    public:
        typedef CHrefOfClassHashKey T;

        CHrefOfClassHash() :
            CClosedHash<class CHrefOfClassHashKey>(101)
        {
            WRAPPER_NO_CONTRACT;
        }
        ~CHrefOfClassHash()
        {
            WRAPPER_NO_CONTRACT;
            Clear();
        }

        virtual void Clear();
        
        unsigned int Hash(const T *pData);
        unsigned int Hash(const void *pData)
        {
            WRAPPER_NO_CONTRACT;
            return Hash((const T*)pData);
        }


        unsigned int Compare(const T *p1, T *p2);
        unsigned int Compare(const void *p1, BYTE *p2)
        {
            WRAPPER_NO_CONTRACT;
            return Compare((const T*)p1, (T*)p2);
        }


        ELEMENTSTATUS Status(T *p);
        ELEMENTSTATUS Status(BYTE *p)
        {
            WRAPPER_NO_CONTRACT;
            return Status((T*)p);
        }


        void SetStatus(T *p, ELEMENTSTATUS s);
        void SetStatus(BYTE *p, ELEMENTSTATUS s)
        {
            WRAPPER_NO_CONTRACT;
            SetStatus((T*)p, s);
        }


        void *GetKey(T *p);
        void* GetKey(BYTE *p)
        {
            WRAPPER_NO_CONTRACT;
            return GetKey((T*)p);
        }
    };

    struct CErrorContext
    {
        CErrorContext() : 
            m_prev(0),
            m_szAssembly(0),
            m_tkType(mdTypeDefNil),
            m_pScope(0),
            m_szMember(0),
            m_szParam(0),
            m_ixParam(-1)
        {
            LIMITED_METHOD_CONTRACT;
        }

        // The following variables hold context info for error reporting.
        CErrorContext*  m_prev;         // A previous context.
        LPCUTF8         m_szAssembly;   // Current assembly name.
        mdToken         m_tkType;       // Current type's metadata token.
        IMDInternalImport *m_pScope;    // Current type's scope.
        LPCUTF8         m_szMember;     // Current member's name.
        LPCUTF8         m_szParam;      // Current param's name.
        int             m_ixParam;      // Current param index.
    };

public:
    TypeLibExporter(); 
    ~TypeLibExporter();

    void    Convert(Assembly *pAssembly, LPCWSTR szTlbName, ITypeLibExporterNotifySink *pNotify=0, int flags=0);
    void    LayOut();
    HRESULT GetTypeLib(REFGUID iid, IUnknown **ppTlb);
    void    ReleaseResources();

protected:
    void PreLoadNames();

    void    UpdateBitness(Assembly* pAssembly);
    HRESULT CheckBitness(Assembly* pAssembly);

    // TypeLib emit functions.
    HRESULT TokenToHref(ICreateTypeInfo2 *pTI, MethodTable *pClass, mdToken tk, BOOL bWarnOnUsingIUnknown, HREFTYPE *pHref);
    void    GetWellKnownInterface(MethodTable *pClass, ITypeInfo **ppTI);
    HRESULT EEClassToHref(ICreateTypeInfo2 *pTI, MethodTable *pClass, BOOL bWarnOnUsingIUnknown, HREFTYPE *pHref);
    void    StdOleTypeToHRef(ICreateTypeInfo2 *pCTI, REFGUID rGuid, HREFTYPE *pHref);
    void    ExportReferencedAssembly(Assembly *pAssembly);
    
    // Metadata import functions.
    void    AddModuleTypes(Module *pModule);
    void    AddAssemblyTypes(Assembly *pAssembly);

    void ConvertAllTypeDefs();
    HRESULT ConvertOneTypeDef(MethodTable *pClass);

    HRESULT GetTypeLibImportClassName(MethodTable *pClass, SString& pszName);

    void CreateITypeInfo(CExportedTypesInfo *pData, bool bNamespace=false, bool bResolveDup=true);
    void CreateIClassXITypeInfo(CExportedTypesInfo *pData, bool bNamespace=false, bool bResolveDup=true);
    void    ConvertImplTypes(CExportedTypesInfo *pData);
    void    ConvertDetails(CExportedTypesInfo *pData);
    
    void ConvertInterfaceImplTypes(ICreateTypeInfo2 *pICTI, MethodTable *pClass);
    void ConvertInterfaceDetails(ICreateTypeInfo2 *pICTI, MethodTable *pClass, int bAutoProxy);
    void ConvertRecord(CExportedTypesInfo *pData);
    void ConvertRecordBaseClass(CExportedTypesInfo *pData, MethodTable *pSubClass, ULONG &ixVar);
    void ConvertEnum(ICreateTypeInfo2 *pICTI, MethodTable *pClass);
    void ConvertClassImplTypes(ICreateTypeInfo2 *pICTI, ICreateTypeInfo2 *pIDefault, MethodTable *pClass);
    void ConvertClassDetails(ICreateTypeInfo2 *pICTI, ICreateTypeInfo2 *pIDefault, MethodTable *pClass, int bAutoProxy);

    BOOL HasDefaultCtor(MethodTable *pMT);

    void    ConvertIClassX(ICreateTypeInfo2 *pICTI, MethodTable *pClass, int bAutoProxy);
    BOOL    ConvertMethod(ICreateTypeInfo2 *pTI, ComMTMethodProps *pProps, ULONG iMD, ULONG ulIface);
    BOOL    ConvertFieldAsMethod(ICreateTypeInfo2 *pTI, ComMTMethodProps *pProps, ULONG iMD);
    BOOL    ConvertVariable(ICreateTypeInfo2 *pTI, MethodTable *pClass, mdFieldDef md, SString& sName, ULONG iMD);
    BOOL    ConvertEnumMember(ICreateTypeInfo2 *pTI, MethodTable *pClass, mdFieldDef md, SString& sName, ULONG iMD);

    // Error/status functions.
    void    InternalThrowHRWithContext(HRESULT hr, ...);
    void    FormatErrorContextString(CErrorContext *pContext, SString *pOut);
    void    FormatErrorContextString(SString *pOut);
    void    ReportError(HRESULT hr);
    void    ReportEvent(int ev, int hr, ...);
    void    ReportWarning(HRESULT hrReturn, HRESULT hrRpt, ...); 
    void    PostClassLoadError(LPCUTF8 pszName, SString& message);
    
    // Utility functions.
    void    ClassHasIClassX(MethodTable *pClass, CorClassIfaceAttr *pRslt);
    MethodTable * LoadClass(Module *pModule, mdToken tk);
    TypeHandle LoadClass(Module *pModule, LPCUTF8 pszName);
    HRESULT CorSigToTypeDesc(ICreateTypeInfo2 *pTI, MethodTable *pClass, PCCOR_SIGNATURE pbSig, PCCOR_SIGNATURE pbNativeSig, ULONG cbNativeSig, 
                             ULONG *cbElem, TYPEDESC *ptdesc, CDescPool *ppool, BOOL bMethodSig, BOOL *pbByRef=0);
    BOOL    IsVbRefType(PCCOR_SIGNATURE pbSig, IMDInternalImport *pInternalImport);

    BOOL    IsExportingAs64Bit();

    void ArrayToTypeDesc(ICreateTypeInfo2 *pCTI, CDescPool *ppool, ArrayMarshalInfo *pArrayMarshalInfo, TYPEDESC *pElementTypeDesc);

    VARTYPE GetVtForIntPtr();
    VARTYPE GetVtForUIntPtr();

    //BOOL ValidateSafeArrayElemVT(VARTYPE vt);
    
    BOOL GetDescriptionString(MethodTable *pClass, mdToken tk, BSTR &bstrDescr);
    BOOL GetStringCustomAttribute(IMDInternalImport *pImport, LPCSTR szName, mdToken tk, BSTR &bstrDescr);
    
    BOOL    GetAutomationProxyAttribute(IMDInternalImport *pImport, mdToken tk, int *bValue);

    TYPEKIND TKindFromClass(MethodTable *pClass);

protected:
    void GetRefTypeInfo(ICreateTypeInfo2 *pContainer, ITypeInfo *pReferenced, HREFTYPE *pHref);

    CHrefOfTIHash       m_HrefHash;            // Hashed table of HREFTYPEs of ITypeInfos
    CHrefOfClassHash    m_HrefOfClassHash;     // Hashed table of HREFTYPEs of ITypeInfos   
    CErrorContext       m_ErrorContext;

private:
    ClassLoader*        m_pLoader;             // Domain where the Module being converted was loaded
    ITypeInfo*          m_pIUnknown;           // TypeInfo for IUnknown.
    HREFTYPE            m_hIUnknown;           // href for IUnknown.
    ITypeInfo*          m_pIDispatch;          // TypeInfo for IDispatch.
    ITypeInfo*          m_pGuid;               // TypeInfo for GUID.
    
    ITypeLibExporterNotifySink* m_pNotify;     // Notification callback.

    ICreateTypeLib2*    m_pICreateTLB;         // The created typelib.
    
    int                 m_flags;                // Conversion flags.
    int                 m_bAutomationProxy;     // Should interfaces be marked such that oleaut32 is the proxy?
    int                 m_bWarnedOfNonPublic;

    CExportedTypesHash  m_Exports;
    CExportedTypesHash  m_InjectedExports;
};


// eof ------------------------------------------------------------------------
