// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: SymRead.h
//

// ===========================================================================

#ifndef SYMREAD_H_
#define SYMREAD_H_

class SymScope;
class SymReaderVar;
class SymDocument;

// -------------------------------------------------------------------------
// SymReader class
// -------------------------------------------------------------------------

class SymReader : public ISymUnmanagedReader
{
// ctor/dtor
public:
    SymReader()
    {
        m_refCount = 0;
        m_pPDBInfo = NULL;
        m_pDocs = NULL;
        m_pImporter = NULL;
        m_fInitialized = false;
        m_fInitializeFromStream = false;
        memset(&m_DataPointers, 0, sizeof(PDBDataPointers));
        m_szPath[0] = '\0';
    }
    virtual ~SymReader();
    static HRESULT NewSymReader( REFCLSID clsid, void** ppObj );

public:
    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject);

// ISymUnmanagedReader
public:
    STDMETHOD(GetDocument)(__in LPWSTR url,
                           GUID language,
                           GUID languageVendor,
                           GUID documentType,
                           ISymUnmanagedDocument **pRetVal);
    STDMETHOD(GetDocuments)(ULONG32 cDocs,
                            ULONG32 *pcDocs,
                            ISymUnmanagedDocument *pDocs[]);
    STDMETHOD(GetUserEntryPoint)(mdMethodDef *pRetVal);
    STDMETHOD(GetMethod)(mdMethodDef method,
                         ISymUnmanagedMethod **pRetVal);
    STDMETHOD(GetMethodByVersion)(mdMethodDef method,
                                  int version,
                                  ISymUnmanagedMethod **pRetVal);
    STDMETHOD(GetVariables)(mdToken parent,
                            ULONG32 cVars,
                            ULONG32 *pcVars,
                            ISymUnmanagedVariable *pVars[]);
    STDMETHOD(GetGlobalVariables)(ULONG32 cVars,
                                  ULONG32 *pcVars,
                                  ISymUnmanagedVariable *pVars[]);
    STDMETHOD(GetMethodFromDocumentPosition)(ISymUnmanagedDocument *document,
                                             ULONG32 line,
                                             ULONG32 column,
                                             ISymUnmanagedMethod **pRetVal);
    STDMETHOD(GetSymAttribute)(mdToken parent,
                               __in LPWSTR name,
                               ULONG32 cBuffer,
                               ULONG32 *pcBuffer,
                               __out_bcount_part_opt(cBuffer, *pcBuffer) BYTE buffer[]);
    STDMETHOD(GetNamespaces)(ULONG32 cNameSpaces,
                             ULONG32 *pcNameSpaces,
                             ISymUnmanagedNamespace *namespaces[]);
    STDMETHOD(Initialize)(IUnknown *importer,
                          const WCHAR* szFileName,
                          const WCHAR* szsearchPath,
                          IStream *pIStream);
    STDMETHOD(UpdateSymbolStore)(const WCHAR *filename,
                                 IStream *pIStream);

    STDMETHOD(ReplaceSymbolStore)(const WCHAR *filename,
                                  IStream *pIStream);

    STDMETHOD(GetSymbolStoreFileName)(ULONG32 cchName,
                      ULONG32 *pcchName,
                      __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);

    STDMETHOD(GetMethodsFromDocumentPosition)(ISymUnmanagedDocument* document,
                                             ULONG32 line,
                                             ULONG32 column,
                                             ULONG32 cMethod,
                                             ULONG32* pcMethod,
                                             ISymUnmanagedMethod* pRetVal[]);

    STDMETHOD(GetDocumentVersion)(ISymUnmanagedDocument *pDoc, int* version, BOOL* pbCurrent);

    STDMETHOD(GetMethodVersion)(ISymUnmanagedMethod* pMethod, int* version);

    //-----------------------------------------------------------
    // Methods not exposed via a COM interface.
    //-----------------------------------------------------------
public:
    HRESULT GetDocument(UINT32 DocumentEntry, SymDocument **ppDocument);
private:
    void Cleanup();

    HRESULT InitializeFromFile(const WCHAR* szFileName,
                               const WCHAR* szsearchPath);

    HRESULT InitializeFromStream(IStream * pIStream);

    HRESULT VerifyPEDebugInfo(const WCHAR* szFileName);

    HRESULT ValidateData();

    HRESULT ValidateBytes(UINT32 bytesIndex, UINT32 bytesLength);

private:
    // Data Members
    UINT32      m_refCount;

    // Symbol File Name
    WCHAR m_szPath[ _MAX_PATH ];
    WCHAR m_szStoredSymbolName[ _MAX_PATH ];

    PDBInfo *m_pPDBInfo;
    SymDocument **m_pDocs;
    IUnknown *m_pImporter;
    PDBDataPointers m_DataPointers;

    // Are we initialized yet?
    bool m_fInitialized;

    // Did we initialize from stream
    bool m_fInitializeFromStream;
 };

/* ------------------------------------------------------------------------- *
 * SymDocument class
 * ------------------------------------------------------------------------- */

class SymDocument : public ISymUnmanagedDocument
{
// ctor/dtor
public:
    SymDocument(SymReader *pReader,
                PDBDataPointers *pData,
                UINT32 CountOfMethods,
                UINT32 DocumentEntry)
    {
        m_refCount = 0;
        m_pData = pData;
        m_DocumentEntry = DocumentEntry;
        m_CountOfMethods = CountOfMethods;
        m_pReader = pReader;
        pReader->AddRef();

    }
    virtual ~SymDocument()
    {
        RELEASE(m_pReader);
    }

// IUnknown
public:
    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject);

// ISymUnmanagedDocument
public:
    STDMETHOD(GetURL)(ULONG32 cchUrl,
                      ULONG32 *pcchUrl,
                      __out_ecount_part_opt(cchUrl, *pcchUrl) WCHAR szUrl[]);
    STDMETHOD(GetDocumentType)(GUID *pRetVal);
    STDMETHOD(GetLanguage)(GUID *pRetVal);
    STDMETHOD(GetLanguageVendor)(GUID *pRetVal);
    STDMETHOD(GetCheckSumAlgorithmId)(GUID *pRetVal);
    STDMETHOD(GetCheckSum)(ULONG32 cData,
                           ULONG32 *pcData,
                           BYTE data[]);
    STDMETHOD(FindClosestLine)(ULONG32 line, ULONG32 *pRetVal);
    STDMETHOD(HasEmbeddedSource)(BOOL *pRetVal);
    STDMETHOD(GetSourceLength)(ULONG32 *pRetVal);
    STDMETHOD(GetSourceRange)(ULONG32 startLine,
                              ULONG32 startColumn,
                              ULONG32 endLine,
                              ULONG32 endColumn,
                              ULONG32 cSourceBytes,
                              ULONG32 *pcSourceBytes,
                              BYTE source[]);

    //-----------------------------------------------------------
    // Methods not exposed via a COM interface.
    //-----------------------------------------------------------
    UINT32 GetDocumentEntry()
    {
        return m_DocumentEntry;
    }

// Data members
private:
    UINT32      m_refCount;

    SymReader  *m_pReader;

    // Data Pointer
    PDBDataPointers *m_pData;

    // Entry into the document array
    UINT32 m_DocumentEntry;

    // Total number of methods in the ildb
    UINT32 m_CountOfMethods;

};

/* ------------------------------------------------------------------------- *
 * SymMethod class
 * ------------------------------------------------------------------------- */

class SymMethod : public ISymUnmanagedMethod
{
// ctor/dtor
public:
    SymMethod(SymReader *pSymReader, PDBDataPointers *pData, UINT32 MethodEntry)
    {
        m_pData = pData;
        m_MethodEntry = MethodEntry;
        m_refCount = 0;
        m_pReader = pSymReader;
        pSymReader->AddRef();
    }

    virtual ~SymMethod()
    {
        RELEASE(m_pReader);
    };

public:

    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject);

// ISymUnmanagedMethod
public:
    STDMETHOD(GetToken)(mdMethodDef *pRetVal);
    STDMETHOD(GetSequencePointCount)(ULONG32 *pRetVal);

    STDMETHOD(GetRootScope)(ISymUnmanagedScope **pRetVal);
    STDMETHOD(GetScopeFromOffset)(ULONG32 offset,
                                  ISymUnmanagedScope **pRetVal);
    STDMETHOD(GetOffset)(ISymUnmanagedDocument *document,
                         ULONG32 line,
                         ULONG32 column,
                         ULONG32 *pRetVal);
    STDMETHOD(GetRanges)(ISymUnmanagedDocument *document,
                         ULONG32 line,
                         ULONG32 column,
                         ULONG32 cRanges,
                         ULONG32 *pcRanges,
                         ULONG32 ranges[]);
    STDMETHOD(GetParameters)(ULONG32 cParams,
                             ULONG32 *pcParams,
                             ISymUnmanagedVariable *params[]);
    STDMETHOD(GetNamespace)(ISymUnmanagedNamespace **pRetVal);
    STDMETHOD(GetSourceStartEnd)(ISymUnmanagedDocument *docs[2],
                                 ULONG32 lines[2],
                                 ULONG32 columns[2],
                                 BOOL *pRetVal);
    STDMETHOD(GetSequencePoints)(ULONG32 cpoints,
                                 ULONG32* pcpoints,
                                 ULONG32 offsets[],
                                 ISymUnmanagedDocument *documents[],
                                 ULONG32 lines[],
                                 ULONG32 columns[],
                                 ULONG32 endlines[],
                                 ULONG32 endcolumns[]);

// Data members
private:
    // AddRef/Release support
    UINT32      m_refCount;

    // Data Pointer
    PDBDataPointers *m_pData;

    // SymReader
    SymReader *m_pReader;

    // Entry into the SymMethodInfo array
    UINT32 m_MethodEntry;

};

/* ------------------------------------------------------------------------- *
 * SymScope class
 * ------------------------------------------------------------------------- */

class SymScope : public ISymUnmanagedScope
{
// ctor/dtor
public:
    SymScope(
        ISymUnmanagedMethod *pSymMethod,
        PDBDataPointers *pData,
        UINT32 MethodEntry,
        UINT32 ScopeEntry)
    {
        m_pSymMethod = pSymMethod;
        m_pSymMethod->AddRef();
        m_pData = pData;
        m_MethodEntry = MethodEntry;
        m_ScopeEntry = ScopeEntry;
        m_refCount = 0;
    }
    virtual ~SymScope()
    {
        RELEASE(m_pSymMethod);
    }

public:
    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject);

// ISymUnmanagedScope
public:
    STDMETHOD(GetMethod)(ISymUnmanagedMethod **pRetVal);
    STDMETHOD(GetParent)(ISymUnmanagedScope **pRetVal);
    STDMETHOD(GetChildren)(ULONG32 cChildren,
                           ULONG32 *pcChildren,
                           ISymUnmanagedScope *children[]);
    STDMETHOD(GetStartOffset)(ULONG32 *pRetVal);
    STDMETHOD(GetEndOffset)(ULONG32 *pRetVal);
    STDMETHOD(GetLocalCount)(ULONG32 *pRetVal);
    STDMETHOD(GetLocals)(ULONG32 cLocals,
                         ULONG32 *pcLocals,
                         ISymUnmanagedVariable *locals[]);
    STDMETHOD(GetNamespaces)(ULONG32 cNameSpaces,
                             ULONG32 *pcNameSpaces,
                             ISymUnmanagedNamespace *namespaces[]);

// Data members
private:

    UINT32      m_refCount; // Add/Ref Release

    ISymUnmanagedMethod    *m_pSymMethod;

    // Data Pointer
    PDBDataPointers *m_pData;
    // Entry into the SymMethodInfo array
    UINT32 m_MethodEntry;
    // Entry into the scope array
    UINT32 m_ScopeEntry;
};

/* ------------------------------------------------------------------------- *
 * SymReaderVar class
 * ------------------------------------------------------------------------- */

class SymReaderVar : public ISymUnmanagedVariable
{
// ctor/dtor
public:
    SymReaderVar(SymScope *pScope, PDBDataPointers *pData, UINT32 VarEntry)
    {
        m_pData = pData;
        m_VarEntry = VarEntry;
        m_refCount = 0;
        m_pScope = pScope;
        pScope->AddRef();
    }
    virtual ~SymReaderVar()
    {
        RELEASE(m_pScope);
    }

public:
    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject);

// ISymUnmanagedReaderVar
public:
    STDMETHOD(GetName)(ULONG32 cchName,
                       ULONG32 *pcchName,
                       __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);
    STDMETHOD(GetAttributes)(ULONG32 *pRetVal);
    STDMETHOD(GetSignature)(ULONG32 cSig,
                            ULONG32 *pcSig,
                            BYTE sig[]);
    STDMETHOD(GetAddressKind)(ULONG32 *pRetVal);
    STDMETHOD(GetAddressField1)(ULONG32 *pRetVal);
    STDMETHOD(GetAddressField2)(ULONG32 *pRetVal);
    STDMETHOD(GetAddressField3)(ULONG32 *pRetVal);
    STDMETHOD(GetStartOffset)(ULONG32 *pRetVal);
    STDMETHOD(GetEndOffset)(ULONG32 *pRetVal);


// Data members
private:
    UINT32      m_refCount; // Add/Ref Release

    // Data Pointer
    PDBDataPointers *m_pData;

    // Scope of the variable
    SymScope *m_pScope;

    // Entry into the SymMethodInfo array
    UINT32 m_VarEntry;
};

class SymReaderNamespace : public ISymUnmanagedNamespace
{

public:
    SymReaderNamespace(SymScope *pScope, PDBDataPointers *pData, UINT32 NamespaceEntry)
    {
        m_pData = pData;
        m_NamespaceEntry = NamespaceEntry;
        m_refCount = 0;
        m_pScope = pScope;
        pScope->AddRef();
    }
    virtual ~SymReaderNamespace()
    {
    }

public:
    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject);

public:
    //-----------------------------------------------------------
    // ISymUnmanagedNamespace support
    //-----------------------------------------------------------
    STDMETHOD(GetName)(ULONG32 cchName,
                       ULONG32 *pcchName,
                       __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);
    STDMETHOD(GetNamespaces)(ULONG32 cNamespaces,
                             ULONG32 *pcNamespaces,
                             ISymUnmanagedNamespace* namespaces[]);
    STDMETHOD(GetVariables)(ULONG32 cchName,
                            ULONG32 *pcchName,
                            ISymUnmanagedVariable *pVars[]);

private:
    UINT32      m_refCount; // Add/Ref Release

    // Owning scope
    SymScope *m_pScope;

    // Data Pointer
    PDBDataPointers *m_pData;
    // Entry into the NameSpace array
    UINT32 m_NamespaceEntry;

};

#endif
