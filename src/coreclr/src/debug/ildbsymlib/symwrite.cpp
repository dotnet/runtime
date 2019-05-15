// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: symwrite.cpp
//

//
// Note: The various SymWriter_* and SymDocumentWriter_* are entry points
// called via PInvoke from the managed symbol wrapper used by managed languages
// to emit debug information (such as jscript)
// ===========================================================================

#include "pch.h"
#include "symwrite.h"


// ------------------------------------------------------------------------- 
// SymWriter class
// ------------------------------------------------------------------------- 

// This is a COM object which is called both directly from the runtime, and from managed code
// via PInvoke (CoreSymWrapper) and IJW (ISymWrapper).  This is an unusual pattern, and it's not
// clear exactly how best to address it.  Eg., should we be using BEGIN_EXTERNAL_ENTRYPOINT
// macros?  Conceptually this is just a drop-in replacement for diasymreader.dll, and could
// live in a different DLL instead of being statically linked into the runtime.  But since it
// relies on utilcode (and actually gets the runtime utilcode, not the nohost utilcode like
// other external tools), it does have some properties of runtime code.
// 

//-----------------------------------------------------------
// NewSymWriter
// Static function used to create a new instance of SymWriter
//-----------------------------------------------------------
HRESULT SymWriter::NewSymWriter(const GUID& id, void **object)
{
    if (id != IID_ISymUnmanagedWriter)
        return (E_UNEXPECTED);

    SymWriter *writer = NEW(SymWriter());

    if (writer == NULL)
        return (E_OUTOFMEMORY);

    *object = (ISymUnmanagedWriter*)writer;
    writer->AddRef();

    return (S_OK);
}

//-----------------------------------------------------------
// SymWriter Constuctor
//-----------------------------------------------------------
SymWriter::SymWriter() :
    m_refCount(0),
    m_openMethodToken(mdMethodDefNil),
    m_LargestMethodToken(mdMethodDefNil),
    m_pmethod(NULL),
    m_currentScope(k_noScope),
    m_hFile(NULL),
    m_pIStream(NULL),
    m_pStringPool(NULL),
    m_closed( false ),
    m_sortLines (false),
    m_sortMethodEntries(false)
{
    memset(m_szPath, 0, sizeof(m_szPath));
    memset(&ModuleLevelInfo, 0, sizeof(PDBInfo));
}

//-----------------------------------------------------------
// SymWriter QI
//-----------------------------------------------------------
COM_METHOD SymWriter::QueryInterface(REFIID riid, void **ppInterface)
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ISymUnmanagedWriter )
        *ppInterface = (ISymUnmanagedWriter*)this;
    else if (riid == IID_ISymUnmanagedWriter2 )
        *ppInterface = (ISymUnmanagedWriter2*)this;
    else if (riid == IID_ISymUnmanagedWriter3 )
        *ppInterface = (ISymUnmanagedWriter3*)this;
    else if (riid == IID_IUnknown)
        *ppInterface = (IUnknown*)(ISymUnmanagedWriter*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

//-----------------------------------------------------------
// SymWriter Destructor
//-----------------------------------------------------------
SymWriter::~SymWriter()
{
    // Note that this must be thread-safe - it may be invoked on the finalizer thread
    // But since this dtor can only be invoked when all references have been released,
    // no other threads can be manipulating the writer.
    // Ideally we'd probably just add locking to all methods, but this is low-priority
    // because diasymreader.dll isn't thread-safe and so we need to ensure the CLR's use
    // of these interfaces are properly syncrhonized.
    if ( !m_closed )
        Close();
    RELEASE(m_pIStream);
    DELETE(m_pStringPool);
}

//-----------------------------------------------------------
// SymWriter Initialize the SymWriter
//-----------------------------------------------------------
COM_METHOD SymWriter::Initialize
(   
    IUnknown *emitter,        // Emitter (IMetaData Emit/Import) - unused by ILDB
    const WCHAR *szFilename,  // FileName of the exe we're creating
    IStream *pIStream,        // Stream to store into
    BOOL fFullBuild           // Is this a full build or an incremental build
)
{    
    HRESULT hr = S_OK;
    
    // Incremental compile not implemented
    _ASSERTE(fFullBuild);
    
    if (emitter == NULL)
        return E_INVALIDARG;
    
    if (pIStream != NULL)
    {
        m_pIStream = pIStream;
        pIStream->AddRef();
    }
    else
    {
        if (szFilename == NULL)
        {
            IfFailRet(E_INVALIDARG);
        }
    }
    
    m_pStringPool = NEW(StgStringPool());
    IfFailRet(m_pStringPool->InitNew());
    
    if (szFilename != NULL)
    {
        wchar_t fullpath[_MAX_PATH];
        wchar_t drive[_MAX_DRIVE];
        wchar_t dir[_MAX_DIR];
        wchar_t fname[_MAX_FNAME];
        _wsplitpath_s( szFilename, drive, COUNTOF(drive), dir, COUNTOF(dir), fname, COUNTOF(fname), NULL, 0 );
        _wmakepath_s( fullpath, COUNTOF(fullpath), drive, dir, fname, W("ildb") );
        if (wcsncpy_s( m_szPath, COUNTOF(m_szPath), fullpath, _TRUNCATE) == STRUNCATE)
            return HrFromWin32(ERROR_INSUFFICIENT_BUFFER);
    }
    
    // Note that we don't need the emitter - ILDB is agnostic to the module metadata.
    
    return hr;
}

//-----------------------------------------------------------
// SymWriter Initialize2 the SymWriter
// Delegate to Initialize then use the szFullPathName param
//-----------------------------------------------------------
COM_METHOD SymWriter::Initialize2
(
    IUnknown *emitter,         // Emitter (IMetaData Emit/Import)
    const WCHAR *szTempPath,   // Location of the file
    IStream *pIStream,         // Stream to store into
    BOOL fFullBuild,           // Full build or not
    const WCHAR *szFullPathName   // Final destination of the ildb
)    
{
    HRESULT hr = S_OK;
    IfFailGo( Initialize( emitter, szTempPath, pIStream, fFullBuild ) );
    // We don't need the final location of the ildb

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter GetorCreateDocument
// creates a new symbol document writer for a specified source
// Arguments: 
//     input:  wcsUrl   - The source file name 
//     output: ppRetVal - The new document writer
// Return Value: hr - S_OK if success, OOM otherwise
//-----------------------------------------------------------
HRESULT SymWriter::GetOrCreateDocument(
    const WCHAR *wcsUrl,          // Document name
    const GUID *pLanguage,        // What Language we're compiling
    const GUID *pLanguageVendor,  // What vendor
    const GUID *pDocumentType,    // Type
    ISymUnmanagedDocumentWriter **ppRetVal // [out] Created DocumentWriter
)
{
    ULONG UrlEntry;
    DWORD strLength = WszWideCharToMultiByte(CP_UTF8, 0, wcsUrl, -1, 0, 0, 0, 0);
    LPSTR multiByteURL = (LPSTR) new char [strLength];
    HRESULT hr  = S_OK;

    if (multiByteURL == NULL)
    {
        return E_OUTOFMEMORY;
    }

    WszWideCharToMultiByte(CP_UTF8, 0, wcsUrl, -1, multiByteURL, strLength, 0, 0);

    if (m_pStringPool->FindString(multiByteURL, &UrlEntry) == S_FALSE) // no file of that name has been seen before
    {
        hr = CreateDocument(wcsUrl, pLanguage, pLanguageVendor, pDocumentType, ppRetVal);
    }
    else // we already have a writer for this file
    {
        UINT32 docInfo = 0;
        
        CRITSEC_COOKIE cs = ClrCreateCriticalSection(CrstLeafLock, CRST_DEFAULT);
        
        ClrEnterCriticalSection(cs);

        while ((docInfo < m_MethodInfo.m_documents.count()) && (m_MethodInfo.m_documents[docInfo].UrlEntry() != UrlEntry)) 
        {
            docInfo++;
        }

        if (docInfo == m_MethodInfo.m_documents.count()) // something went wrong and we didn't find the writer
        {
            hr = CreateDocument(wcsUrl, pLanguage, pLanguageVendor, pDocumentType, ppRetVal);
        }
        else 
        {
            *ppRetVal = m_MethodInfo.m_documents[docInfo].DocumentWriter();
            (*ppRetVal)->AddRef();
        }
        ClrLeaveCriticalSection(cs);
    }

    delete [] multiByteURL;
    return hr;

} // SymWriter::GetOrCreateDocument

//-----------------------------------------------------------
// SymWriter CreateDocument
// creates a new symbol document writer for a specified source
// Arguments: 
//     input:  wcsUrl   - The source file name 
//     output: ppRetVal - The new document writer
// Return Value: hr - S_OK if success, OOM otherwise
//-----------------------------------------------------------
HRESULT SymWriter::CreateDocument(const WCHAR *wcsUrl,                   // Document name
                                  const GUID *pLanguage,                 // What Language we're compiling
                                  const GUID *pLanguageVendor,           // What vendor
                                  const GUID *pDocumentType,             // Type
                                  ISymUnmanagedDocumentWriter **ppRetVal // [out] Created DocumentWriter
)

{
    DocumentInfo* pDocument = NULL;
    SymDocumentWriter *sdw = NULL;
    UINT32 DocumentEntry;
    ULONG UrlEntry;
    HRESULT hr = NOERROR;

    DocumentEntry = m_MethodInfo.m_documents.count();
    IfNullGo(pDocument = m_MethodInfo.m_documents.next());
    memset(pDocument, 0, sizeof(DocumentInfo));

    // Create the new document writer.
    sdw = NEW(SymDocumentWriter(DocumentEntry, this));
    IfNullGo(sdw);

    pDocument->SetLanguage(*pLanguage);
    pDocument->SetLanguageVendor(*pLanguageVendor);
    pDocument->SetDocumentType(*pDocumentType);
    pDocument->SetDocumentWriter(sdw);

    // stack check needed to call back into utilcode
    hr = m_pStringPool->AddStringW(wcsUrl, (UINT32 *)&UrlEntry);
    IfFailGo(hr);

    pDocument->SetUrlEntry(UrlEntry);

    // Pass out the new ISymUnmanagedDocumentWriter.
    sdw->AddRef();
    *ppRetVal = (ISymUnmanagedDocumentWriter*)sdw;
    sdw = NULL;

ErrExit:
    DELETE(sdw);
    return hr;
}

//-----------------------------------------------------------
// SymWriter DefineDocument
//-----------------------------------------------------------
COM_METHOD SymWriter::DefineDocument(
    const WCHAR *wcsUrl,          // Document name
    const GUID *pLanguage,        // What Language we're compiling
    const GUID *pLanguageVendor,  // What vendor
    const GUID *pDocumentType,    // Type
    ISymUnmanagedDocumentWriter **ppRetVal // [out] Created DocumentWriter
)
{
    HRESULT hr = NOERROR;

    IfFalseGo(wcsUrl, E_INVALIDARG);
    IfFalseGo(pLanguage, E_INVALIDARG);
    IfFalseGo(pLanguageVendor, E_INVALIDARG);
    IfFalseGo(pDocumentType, E_INVALIDARG);
    IfFalseGo(ppRetVal, E_INVALIDARG);

    // Init out parameter
    *ppRetVal = NULL;

    hr = GetOrCreateDocument(wcsUrl, pLanguage, pLanguageVendor, pDocumentType, ppRetVal);
ErrExit:
    return hr;
}


//-----------------------------------------------------------
// SymWriter SetDocumentSrc
//-----------------------------------------------------------
HRESULT SymWriter::SetDocumentSrc(
    UINT32 DocumentEntry,
    DWORD SourceSize,
    BYTE* pSource
)
{
    DocumentInfo* pDocument = NULL;
    HRESULT hr = S_OK;

    IfFalseGo( SourceSize == 0 || pSource, E_INVALIDARG);
    IfFalseGo( DocumentEntry < m_MethodInfo.m_documents.count(), E_INVALIDARG);

    pDocument = &m_MethodInfo.m_documents[DocumentEntry];

    if (pSource)
    {
        UINT32 i;
        IfFalseGo( m_MethodInfo.m_bytes.grab(SourceSize, &i), E_OUTOFMEMORY);
        memcpy(&m_MethodInfo.m_bytes[i], pSource, SourceSize);
        pDocument->SetSourceEntry(i);
        pDocument->SetSourceSize(SourceSize);
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter SetDocumentCheckSum
//-----------------------------------------------------------
HRESULT SymWriter::SetDocumentCheckSum(
    UINT32 DocumentEntry,
    GUID  AlgorithmId,
    DWORD CheckSumSize,
    BYTE* pCheckSum
)
{
    DocumentInfo* pDocument = NULL;
    HRESULT hr = S_OK;

    IfFalseGo( CheckSumSize == 0 || pCheckSum, E_INVALIDARG);
    IfFalseGo( DocumentEntry < m_MethodInfo.m_documents.count(), E_INVALIDARG);

    pDocument = &m_MethodInfo.m_documents[DocumentEntry];

    if (pCheckSum)
    {
        UINT32 i;
        IfFalseGo( m_MethodInfo.m_bytes.grab(CheckSumSize, &i), E_OUTOFMEMORY);
        memcpy(&m_MethodInfo.m_bytes[i], pCheckSum, CheckSumSize);
        pDocument->SetCheckSumEntry(i);
        pDocument->SetCheckSymSize(CheckSumSize);
    }

    pDocument->SetAlgorithmId(AlgorithmId);

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter SetUserEntryPoint
//-----------------------------------------------------------
COM_METHOD SymWriter::SetUserEntryPoint(mdMethodDef entryMethod)
{
    HRESULT hr = S_OK;

    // Make sure that an entry point hasn't already been set.
    if (ModuleLevelInfo.m_userEntryPoint == 0)
        ModuleLevelInfo.m_userEntryPoint = entryMethod;

    return hr;
}

//-----------------------------------------------------------
// SymWriter OpenMethod
// Get ready to get information about a new method
//-----------------------------------------------------------
COM_METHOD SymWriter::OpenMethod(mdMethodDef method)
{
    HRESULT hr = S_OK;

    // We can only have one open method at a time.
    if (m_openMethodToken != mdMethodDefNil)
        return E_INVALIDARG;

    m_LargestMethodToken = max(method, m_LargestMethodToken);

    if (m_LargestMethodToken != method)
    {
        m_sortMethodEntries = true;
        // Check to see if we're trying to open a method we've already done
        unsigned i;
        for (i = 0; i < m_MethodInfo.m_methods.count(); i++)
        {
            if (m_MethodInfo.m_methods[i].MethodToken() == method)
            {
                return E_INVALIDARG;                
            }
        }
    }

    // Remember the token for this method.
    m_openMethodToken = method;

    IfNullGo( m_pmethod = m_MethodInfo.m_methods.next() );
    m_pmethod->SetMethodToken(m_openMethodToken);
    m_pmethod->SetStartScopes(m_MethodInfo.m_scopes.count());
    m_pmethod->SetStartVars(m_MethodInfo.m_vars.count());
    m_pmethod->SetStartUsing(m_MethodInfo.m_usings.count());
    m_pmethod->SetStartConstant(m_MethodInfo.m_constants.count());
    m_pmethod->SetStartDocuments(m_MethodInfo.m_documents.count());
    m_pmethod->SetStartSequencePoints(m_MethodInfo.m_auxSequencePoints.count());

    // By default assume the lines are inserted in the correct order
    m_sortLines = false;

    // Initialize the maximum scope end offset for this method
    m_maxScopeEnd = 1;

    // Open the implicit root scope for the method
    _ASSERTE(m_currentScope == k_noScope);

    IfFailRet(OpenScope(0, NULL));

    _ASSERTE(m_currentScope != k_noScope);

ErrExit:
    return hr;
}

COM_METHOD SymWriter::OpenMethod2(
    mdMethodDef method,
    ULONG32 isect,
    ULONG32 offset)
{
    // This symbol writer doesn't support section offsets
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// compareAuxLines
// Used to sort SequencePoint
//-----------------------------------------------------------
int __cdecl SequencePoint::compareAuxLines(const void *elem1, const void *elem2 )
{
    SequencePoint* p1 = (SequencePoint*)elem1;
    SequencePoint* p2 = (SequencePoint*)elem2;
    return p1->Offset() - p2->Offset();
}

//-----------------------------------------------------------
// SymWriter CloseMethod
// We're done with this function, write it out.
//-----------------------------------------------------------
COM_METHOD SymWriter::CloseMethod()
{
    HRESULT hr = S_OK;
    UINT32 CountOfSequencePoints;

    // Must have an open method.
    if (m_openMethodToken == mdMethodDefNil)
        return E_UNEXPECTED;

    // All scopes up to the root must have been closed (and the root must not have been closed).
    _ASSERTE(m_currentScope != k_noScope);
    if (m_MethodInfo.m_scopes[m_currentScope].ParentScope() != k_noScope)
        return E_FAIL;

    // Close the implicit root scope using the largest end offset we've seen in this method, or 1 if none.
    IfFailRet(CloseScopeInternal(m_maxScopeEnd));

    m_pmethod->SetEndScopes(m_MethodInfo.m_scopes.count());
    m_pmethod->SetEndVars(m_MethodInfo.m_vars.count());
    m_pmethod->SetEndUsing(m_MethodInfo.m_usings.count());
    m_pmethod->SetEndConstant(m_MethodInfo.m_constants.count());
    m_pmethod->SetEndDocuments(m_MethodInfo.m_documents.count());
    m_pmethod->SetEndSequencePoints(m_MethodInfo.m_auxSequencePoints.count());

    CountOfSequencePoints = m_pmethod->EndSequencePoints() - m_pmethod->StartSequencePoints();
     // Write any sequence points.
    if (CountOfSequencePoints > 0 ) {
        // sort the sequence points
        if ( m_sortLines )
        {
            qsort(&m_MethodInfo.m_auxSequencePoints[m_pmethod->StartSequencePoints()],
                  CountOfSequencePoints,
                  sizeof( SequencePoint ),
                  SequencePoint::compareAuxLines );
        }
    }

    // All done with this method.
    m_openMethodToken = mdMethodDefNil;
    
    return hr;
}

//-----------------------------------------------------------
// SymWriter DefineSequencePoints
// Define the sequence points for this function
//-----------------------------------------------------------
COM_METHOD SymWriter::DefineSequencePoints(
    ISymUnmanagedDocumentWriter *document,  // 
    ULONG32 spCount,        // Count of sequence points
    ULONG32 offsets[],      // Offsets
    ULONG32 lines[],        // Beginning Lines
    ULONG32 columns[],      // [optional] Columns
    ULONG32 endLines[],     // [optional] End Lines
    ULONG32 endColumns[]    // [optional] End Columns
)
{
    HRESULT hr = S_OK;
    DWORD docnum;

    // We must have a document, offsets, and lines.
    IfFalseGo(document && offsets && lines, E_INVALIDARG);
    // Must have some sequence points
    IfFalseGo(spCount != 0, E_INVALIDARG);
    // Must have an open method.
    IfFalseGo(m_openMethodToken != mdMethodDefNil, E_INVALIDARG);

    // Remember that we've loaded the sequence points and
    // which document they were for.
    docnum = (DWORD)((SymDocumentWriter *)document)->GetDocumentEntry();;

    // if sets of lines have been inserted out-of-order, remember to sort when emitting
    if ( m_MethodInfo.m_auxSequencePoints.count() > 0 && m_MethodInfo.m_auxSequencePoints[ m_MethodInfo.m_auxSequencePoints.count()-1 ].Offset() > offsets[0] )
        m_sortLines = true;

    // Copy the incomming arrays into the internal format.

    for ( UINT32 i = 0; i < spCount; i++)
    {
        SequencePoint * paux;
        IfNullGo(paux = m_MethodInfo.m_auxSequencePoints.next());
        paux->SetOffset(offsets[i]);
        paux->SetStartLine(lines[i]);
        paux->SetStartColumn(columns ? columns[i] : 0);
        // If no endLines specified, assume same as start
        paux->SetEndLine(endLines ? endLines[i] : lines[i]);
        paux->SetEndColumn(endColumns ? endColumns[i]: 0);
        paux->SetDocument(docnum);
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter OpenScope
// Open a new scope for this function
//-----------------------------------------------------------
COM_METHOD SymWriter::OpenScope(ULONG32 startOffset, ULONG32 *scopeID)
{
    HRESULT hr = S_OK;

    // Make sure the startOffset is within the current scope.
    if ((m_currentScope != k_noScope) &&
        (unsigned int)startOffset < m_MethodInfo.m_scopes[m_currentScope].StartOffset())
        return E_INVALIDARG;

    // Fill in the new scope.
    UINT32 newScope = m_MethodInfo.m_scopes.count();

    // Make sure that adding 1 below won't overflow (although "next" should fail much
    // sooner if we were anywhere near close enough).
    if (newScope >= UINT_MAX)
        return E_UNEXPECTED;

    SymLexicalScope *sc;
    IfNullGo( sc = m_MethodInfo.m_scopes.next());
    sc->SetParentScope(m_currentScope); // parent is the current scope.
    sc->SetStartOffset(startOffset);
    sc->SetHasChildren(FALSE);
    sc->SetHasVars(FALSE);
    sc->SetEndOffset(0);

    // The current scope has a child now.
    if (m_currentScope != k_noScope)
        m_MethodInfo.m_scopes[m_currentScope].SetHasChildren(TRUE);

    // The new scope is now the current scope.
    m_currentScope = newScope;
    _ASSERTE(m_currentScope != k_noScope);

    // Pass out the "scope id", which is a _1_ based id for the scope.
    if (scopeID)
        *scopeID = m_currentScope + 1;

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter CloseScope
//-----------------------------------------------------------
COM_METHOD SymWriter::CloseScope(
    ULONG32 endOffset // Closing offset of scope
)
{
    // This API can only be used to close explicit user scopes.
    // The implicit root scope is only closed internally by CloseMethod.
    if ((m_currentScope == k_noScope) || (m_MethodInfo.m_scopes[m_currentScope].ParentScope() == k_noScope))
        return E_FAIL;
    
    HRESULT hr = CloseScopeInternal(endOffset);

    _ASSERTE(m_currentScope != k_noScope);

    return hr;
}

//-----------------------------------------------------------
// CloseScopeInternal
// Implementation for ISymUnmanagedWriter::CloseScope but can be called even to
// close the implicit root scope.
//-----------------------------------------------------------
COM_METHOD SymWriter::CloseScopeInternal(
    ULONG32 endOffset // Closing offset of scope
)
{
    _ASSERTE(m_currentScope != k_noScope);

    // Capture the end offset
    m_MethodInfo.m_scopes[m_currentScope].SetEndOffset(endOffset);

    // The current scope is now the parent scope.
    m_currentScope = m_MethodInfo.m_scopes[m_currentScope].ParentScope();

    // Update the maximum scope end offset for this method
    if (endOffset > m_maxScopeEnd)
        m_maxScopeEnd = endOffset;

    return S_OK;
}

//-----------------------------------------------------------
// SymWriter SetScopeRange
// Set the Start/End Offset for this scope
//-----------------------------------------------------------
COM_METHOD SymWriter::SetScopeRange(
    ULONG32 scopeID,      // ID for the scope
    ULONG32 startOffset,  // Start Offset
    ULONG32 endOffset     // End Offset
)
{
    if (scopeID <= 0)
        return E_INVALIDARG;

    if (scopeID > m_MethodInfo.m_scopes.count() )
        return E_INVALIDARG;

    // Remember the new start and end offsets. Also remember that the
    // scopeID is _1_ based!!!
    SymLexicalScope *sc = &(m_MethodInfo.m_scopes[scopeID - 1]);
    sc->SetStartOffset(startOffset);
    sc->SetEndOffset(endOffset);

    // Update the maximum scope end offset for this method
    if (endOffset > m_maxScopeEnd)
        m_maxScopeEnd = endOffset;

    return S_OK;
}

//-----------------------------------------------------------
// SymWriter DefineLocalVariable
//-----------------------------------------------------------
COM_METHOD SymWriter::DefineLocalVariable(
    const WCHAR *name,    // Name of the variable
    ULONG32 attributes,   // Attributes for the var
    ULONG32 cSig,         // Signature for the variable
    BYTE signature[],
    ULONG32 addrKind,
    ULONG32 addr1, ULONG32 addr2, ULONG32 addr3,
    ULONG32 startOffset, ULONG32 endOffset)
{
    HRESULT hr = S_OK;
    ULONG NameEntry;

    // We must have a current scope.
    if (m_currentScope == k_noScope)
        return E_FAIL;

    // We must have a name and a signature.
    if (!name || !signature)
        return E_INVALIDARG;

    if (cSig == 0)
        return E_INVALIDARG;

    // Make a new local variable and copy the data.
    SymVariable *var;
    IfNullGo( var = m_MethodInfo.m_vars.next());
    var->SetIsParam(FALSE);
    var->SetAttributes(attributes);
    var->SetAddrKind(addrKind);
    var->SetIsHidden(attributes & VAR_IS_COMP_GEN);
    var->SetAddr1(addr1);
    var->SetAddr2(addr2);
    var->SetAddr3(addr3);


    // Length of the sig?
    ULONG32 sigLen;
    sigLen = cSig;

    // Copy the name.
    hr = m_pStringPool->AddStringW(name, (UINT32 *)&NameEntry);
    IfFailGo(hr);
    var->SetName(NameEntry);

    // Copy the signature
    // Note that we give this back exactly as-is, but callers typically remove any calling 
    // convention prefix.
    UINT32 i;
    IfFalseGo(m_MethodInfo.m_bytes.grab(sigLen, &i), E_OUTOFMEMORY);
    memcpy(&m_MethodInfo.m_bytes[i], signature, sigLen);
    var->SetSignature(i);
    var->SetSignatureSize(sigLen);

    // This var is in the current scope
    var->SetScope(m_currentScope);
    m_MethodInfo.m_scopes[m_currentScope].SetHasVars(TRUE);

    var->SetStartOffset(startOffset);
    var->SetEndOffset(endOffset);

ErrExit:
    return hr;
}

COM_METHOD SymWriter::DefineLocalVariable2(
    const WCHAR *name,
    ULONG32 attributes,
    mdSignature sigToken,
    ULONG32 addrKind,
    ULONG32 addr1, ULONG32 addr2, ULONG32 addr3,
    ULONG32 startOffset, ULONG32 endOffset)
{
    // This symbol writer doesn't support definiting signatures via tokens
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// SymWriter DefineParameter
//-----------------------------------------------------------
COM_METHOD SymWriter::DefineParameter(
    const WCHAR *name,    // Param name
    ULONG32 attributes,   // Attribute for the parameter
    ULONG32 sequence,
    ULONG32 addrKind,
    ULONG32 addr1, ULONG32 addr2, ULONG32 addr3)
{
    HRESULT hr = S_OK;
    ULONG NameEntry;

    // We must have a method.
    if (m_openMethodToken == mdMethodDefNil)
        return E_INVALIDARG;

    // We must have a name.
    if (!name)
        return E_INVALIDARG;

    SymVariable *var;
    IfNullGo( var = m_MethodInfo.m_vars.next());
    var->SetIsParam(TRUE);
    var->SetAttributes(attributes);
    var->SetAddrKind(addrKind);
    var->SetIsHidden(attributes & VAR_IS_COMP_GEN);
    var->SetAddr1(addr1);
    var->SetAddr2(addr2);
    var->SetAddr3(addr3);
    var->SetSequence(sequence);


    // Copy the name.
    hr = m_pStringPool->AddStringW(name, (UINT32 *)&NameEntry);
    IfFailGo(hr);
    var->SetName(NameEntry);

    // This var is in the current scope
    if (m_currentScope != k_noScope)
        m_MethodInfo.m_scopes[m_currentScope].SetHasVars(TRUE);

    var->SetStartOffset(0);
    var->SetEndOffset(0);

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// verifyConstTypes
// Verify that the type is a type we support
//-----------------------------------------------------------
static bool verifyConstTypes( DWORD vt )
{
    switch ( vt ) {
    case VT_UI8:
    case VT_I8:
    case VT_I4:
    case VT_UI1:    // value < LF_NUMERIC
    case VT_I2:
    case VT_R4:
    case VT_R8:
    case VT_BOOL:   // value < LF_NUMERIC
    case VT_DATE:
    case VT_BSTR:
    case VT_I1:
    case VT_UI2:
    case VT_UI4:
    case VT_INT:
    case VT_UINT:
    case VT_DECIMAL:
        return true;
    }
    return false;
}

//-----------------------------------------------------------
// SymWriter DefineConstant
//-----------------------------------------------------------
COM_METHOD SymWriter::DefineConstant(
    const WCHAR __RPC_FAR *name,
    VARIANT value,
    ULONG32 cSig,
    unsigned char __RPC_FAR signature[])
{
    HRESULT hr = S_OK;
    ULONG ValueBstr = 0;
    ULONG Name;

    // currently we only support local constants

    // We must have a method.
    if (m_openMethodToken == mdMethodDefNil)
        return E_INVALIDARG;

    // We must have a name and signature.
    IfFalseGo(name, E_INVALIDARG);
    IfFalseGo(signature, E_INVALIDARG);
    IfFalseGo(cSig > 0, E_INVALIDARG);

    //
    // Support byref decimal values
    //
    if ( (V_VT(&value)) == ( VT_BYREF | VT_DECIMAL ) ) {
        if ( V_DECIMALREF(&value) == NULL )
            return E_INVALIDARG;
        V_DECIMAL(&value) = *V_DECIMALREF(&value);
        V_VT(&value) = VT_DECIMAL;
    }

    // we only support non-ref constants
    if ( ( V_VT(&value) & VT_BYREF ) != 0 )
        return E_INVALIDARG;

    if ( !verifyConstTypes( V_VT(&value) ) )
        return E_INVALIDARG;

    // If it's a BSTR, we need to persist the Bstr as an entry into
    // the stringpool
    if (V_VT(&value) == VT_BSTR)
    {
        // Copy the bstrValue.
        hr = m_pStringPool->AddStringW(V_BSTR(&value), (UINT32 *)&ValueBstr);
        IfFailGo(hr);
        V_BSTR(&value) = NULL;
    }

    SymConstant *con;
    IfNullGo( con = m_MethodInfo.m_constants.next());
    con->SetValue(value, ValueBstr);


    // Copy the name.
    hr = m_pStringPool->AddStringW(name, (UINT32 *)&Name);
    IfFailGo(hr);
    con->SetName(Name);

    // Copy the signature
    UINT32 i;
    IfFalseGo(m_MethodInfo.m_bytes.grab(cSig, &i), E_OUTOFMEMORY);
    memcpy(&m_MethodInfo.m_bytes[i], signature, cSig);
    con->SetSignature(i);
    con->SetSignatureSize(cSig);

    // This const is in the current scope
    con->SetParentScope(m_currentScope);
    m_MethodInfo.m_scopes[m_currentScope].SetHasVars(TRUE);

ErrExit:
    return hr;
}

COM_METHOD SymWriter::DefineConstant2(
    const WCHAR *name,
    VARIANT value,
    mdSignature sigToken)
{
    // This symbol writer doesn't support definiting signatures via tokens
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// SymWriter Abort
//-----------------------------------------------------------
COM_METHOD SymWriter::Abort(void)
{
    m_closed = true;
    return S_OK;
}

//-----------------------------------------------------------
// SymWriter DefineField
//-----------------------------------------------------------
COM_METHOD SymWriter::DefineField(
    mdTypeDef parent,
    const WCHAR *name,
    ULONG32 attributes,
    ULONG32 csig,
    BYTE signature[],
    ULONG32 addrKind,
    ULONG32 addr1, ULONG32 addr2, ULONG32 addr3)
{
    // This symbol store doesn't support extra random variable
    // definitions. 
    return S_OK;
}

//-----------------------------------------------------------
// SymWriter DefineGlobalVariable
//-----------------------------------------------------------
COM_METHOD SymWriter::DefineGlobalVariable(
    const WCHAR *name,
    ULONG32 attributes,
    ULONG32 csig,
    BYTE signature[],
    ULONG32 addrKind,
    ULONG32 addr1, ULONG32 addr2, ULONG32 addr3)
{
    // This symbol writer doesn't support global variables
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

COM_METHOD SymWriter::DefineGlobalVariable2(
    const WCHAR *name,
    ULONG32 attributes,
    mdSignature sigToken,
    ULONG32 addrKind,
    ULONG32 addr1, ULONG32 addr2, ULONG32 addr3)
{
    // This symbol writer doesn't support global variables
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// compareMethods
// Used to sort method entries
//-----------------------------------------------------------
int __cdecl SymMethodInfo::compareMethods(const void *elem1, const void *elem2 )
{
    SymMethodInfo* p1 = (SymMethodInfo*)elem1;
    SymMethodInfo* p2 = (SymMethodInfo*)elem2;
    return p1->MethodToken() - p2->MethodToken();
}

//-----------------------------------------------------------
// SymWriter Close
//-----------------------------------------------------------
COM_METHOD SymWriter::Close()
{
    HRESULT hr = Commit();
    m_closed = true;
    for (UINT32 docInfo = 0; docInfo < m_MethodInfo.m_documents.count(); docInfo++)
    {
        m_MethodInfo.m_documents[docInfo].SetDocumentWriter(NULL);
    }
    return hr;
}

//-----------------------------------------------------------
// SymWriter Commit
//-----------------------------------------------------------
COM_METHOD SymWriter::Commit(void)
{
    // Sort the entries if need be
    if (m_sortMethodEntries)
    {
        // First remap any tokens we need to
        if (m_MethodMap.count())
        {
            unsigned i;
            for (i = 0; i< m_MethodMap.count(); i++)
            {
                m_MethodInfo.m_methods[m_MethodMap[i].MethodEntry].SetMethodToken(m_MethodMap[i].m_MethodToken);
            }
        }

        // Now sort the array
        qsort(&m_MethodInfo.m_methods[0],
              m_MethodInfo.m_methods.count(),
              sizeof( SymMethodInfo ),
              SymMethodInfo::compareMethods );
        m_sortMethodEntries = false;
    }
    return WritePDB();
}

//-----------------------------------------------------------
// SymWriter SetSymAttribute
//-----------------------------------------------------------
COM_METHOD SymWriter::SetSymAttribute(
    mdToken parent,
    const WCHAR *name,
    ULONG32 cData,
    BYTE data[])
{
    // Setting attributes on the symbol isn't supported
    return S_OK;
}

//-----------------------------------------------------------
// SymWriter OpenNamespace
//-----------------------------------------------------------
COM_METHOD SymWriter::OpenNamespace(const WCHAR *name)
{
    // This symbol store doesn't support namespaces.
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// SymWriter OpenNamespace
//-----------------------------------------------------------
COM_METHOD SymWriter::CloseNamespace()
{
    // This symbol store doesn't support namespaces.
    return S_OK;
}

//-----------------------------------------------------------
// SymWriter UsingNamespace
// Add a Namespace to the list of namespace for this method
//-----------------------------------------------------------
COM_METHOD SymWriter::UsingNamespace(const WCHAR *fullName)
{
    HRESULT hr = S_OK;
    ULONG Name;

    // We must have a current scope.
    if (m_currentScope == k_noScope)
        return E_FAIL;

    // We must have a name.
    if (!fullName)
        return E_INVALIDARG;


    SymUsingNamespace *use;
    IfNullGo( use = m_MethodInfo.m_usings.next());

    // Copy the name.
    hr = m_pStringPool->AddStringW(fullName, (UINT32 *)&Name);
    IfFailGo(hr);
    use->SetName(Name);

    use->SetParentScope(m_currentScope);

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter SetMethodSourceRange
//-----------------------------------------------------------
COM_METHOD SymWriter::SetMethodSourceRange(
    ISymUnmanagedDocumentWriter *startDoc,
    ULONG32 startLine,
    ULONG32 startColumn,
    ISymUnmanagedDocumentWriter *endDoc,
    ULONG32 endLine,
    ULONG32 endColumn)
{
    // This symbol store doesn't support source ranges.
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// UnicodeToUTF8
// Translate the Unicode string to a UTF8 string
// Return the length in UTF8 of the Unicode string
//    Including NULL terminator
//-----------------------------------------------------------
inline int WINAPI UnicodeToUTF8(
    LPCWSTR pUni,  // Unicode string
    __out_bcount_opt(cbUTF) PSTR pUTF8,   // [optional, out] Buffer for UTF8 string
    int cbUTF     // length of UTF8 buffer
)
{
    // Pass in the length including the NULL terminator
    int cchSrc = (int)wcslen(pUni)+1;
    return WideCharToMultiByte(CP_UTF8, 0, pUni, cchSrc, pUTF8, cbUTF, NULL, NULL);
}

//-----------------------------------------------------------
// SymWriter GetDebugCVInfo
// Get the size and potentially the debug info
//-----------------------------------------------------------
COM_METHOD SymWriter::GetDebugCVInfo(
    DWORD cbBuf,    // [optional] Size of buf
    DWORD *pcbBuf,  // [out] Size needed for the DebugInfo
    BYTE buf[])       // [optional, out] Buffer for DebugInfo
{

    if ( m_szPath == NULL || *m_szPath == 0 )
        return E_UNEXPECTED;

    // We need to change the .ildb extension to .pdb to be
    // compatible with VS7
    wchar_t fullpath[_MAX_PATH];
    wchar_t drive[_MAX_DRIVE];
    wchar_t dir[_MAX_DIR];
    wchar_t fname[_MAX_FNAME];
    if (_wsplitpath_s( m_szPath, drive, COUNTOF(drive), dir, COUNTOF(dir), fname, COUNTOF(fname), NULL, 0 ))
        return E_FAIL;
    if (_wmakepath_s( fullpath, COUNTOF(fullpath), drive, dir, fname, W("pdb") ))
        return E_FAIL;

    // Get UTF-8 string size, including the Null Terminator
    int Utf8Length = UnicodeToUTF8( fullpath, NULL, 0 );
    if (Utf8Length < 0 )
        return HRESULT_FROM_GetLastError();

    DWORD dwSize = sizeof(RSDSI) + DWORD(Utf8Length);

    // If the caller is just checking for the size
    if ( cbBuf == 0 && pcbBuf != NULL ) 
    {   
        *pcbBuf = dwSize;
        return S_OK;
    }

    if (cbBuf < dwSize) 
    {    
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    if ( buf == NULL ) 
    {
        return E_INVALIDARG;
    }

    RSDSI* pRsdsi = (RSDSI*)buf;
    pRsdsi->dwSig = VAL32(0x53445352); // "SDSR";
    pRsdsi->guidSig = ILDB_VERSION_GUID;
    SwapGuid(&(pRsdsi->guidSig));
    // Age of 0 represent VC6.0 format so make sure it's 1
    pRsdsi->age = VAL32(1);
    UnicodeToUTF8( fullpath, pRsdsi->szPDB, Utf8Length );
    if ( pcbBuf )
        *pcbBuf = dwSize;
    return S_OK;
}

//-----------------------------------------------------------
// SymWriter GetDebugInfo
// Get the size and potentially the debug info
//-----------------------------------------------------------
COM_METHOD SymWriter::GetDebugInfo(
    IMAGE_DEBUG_DIRECTORY *pIDD,  // [out] IDD to fill in
    DWORD cData,                  // [optional] size of data
    DWORD *pcData,                // [optional, out] return needed size for DebugInfo
    BYTE data[])                  // [optional] Buffer to store into
{
    HRESULT hr = S_OK;
    if ( cData == 0 && pcData != NULL ) 
    {   
        // just checking for the size
        return GetDebugCVInfo( 0, pcData, NULL );
    }

    if ( pIDD == NULL )
        return E_INVALIDARG;

    DWORD cTheData = 0;
    IfFailGo( GetDebugCVInfo( cData, &cTheData, data ) );

    memset( pIDD, 0, sizeof( *pIDD ) );
    pIDD->Type = VAL32(IMAGE_DEBUG_TYPE_CODEVIEW);
    pIDD->SizeOfData = VAL32(cTheData);

    if ( pcData ) {
        *pcData = cTheData;
    }

ErrExit:
    return hr;
}

COM_METHOD SymWriter::RemapToken(mdToken oldToken, mdToken newToken)
{
    HRESULT hr = NOERROR;
    if (oldToken != newToken)
    {
        // We only care about methods
        if ((TypeFromToken(oldToken) == mdtMethodDef) ||
            (TypeFromToken(newToken) == mdtMethodDef))
        {
            // Make sure they are both methods
            _ASSERTE(TypeFromToken(newToken) == mdtMethodDef);
            _ASSERTE(TypeFromToken(oldToken) == mdtMethodDef);

            // Make sure we sort before saving
            m_sortMethodEntries = true;

            // Check to see if we're trying to map a token we know about
            unsigned i;
            for (i = 0; i < m_MethodInfo.m_methods.count(); i++)
            {
                if (m_MethodInfo.m_methods[i].MethodToken() == oldToken)
                {
                    // Remember the map, we need to actually do the actual
                    // mapping later because we might already have a function
                    // with a token 'newToken'
                    SymMap *pMethodMap;
                    IfNullGo( pMethodMap = m_MethodMap.next() );
                    pMethodMap->m_MethodToken = newToken;
                    pMethodMap->MethodEntry = i;                        
                    break;
                }
            }
        }
    }
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter Write
// Write the information to a file or to a stream
//-----------------------------------------------------------
COM_METHOD SymWriter::Write(void *pData, DWORD SizeOfData)
{
    HRESULT hr = NOERROR;
    DWORD NumberOfBytesWritten = 0;
    if (m_pIStream)
    {
        IfFailGo(m_pIStream->Write(pData,
                                   SizeOfData,
                                   &NumberOfBytesWritten));
    }
    else
    {
        // Write out a signature to recognize that we're an ildb
        if (!WriteFile(m_hFile, pData, SizeOfData, &NumberOfBytesWritten, NULL))
            return HrFromWin32(GetLastError());
    }
    _ASSERTE(NumberOfBytesWritten == SizeOfData);
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymWriter WriteStringPool
// Write the information to a file or to a stream
//-----------------------------------------------------------
COM_METHOD SymWriter::WriteStringPool()
{
    IStream *pIStream = NULL;
    BYTE *pStreamMem = NULL;

    HRESULT hr = NOERROR;
    if (m_pIStream)
    {
        IfFailGo(m_pStringPool->PersistToStream(m_pIStream));
    }
    else
    {
        LARGE_INTEGER disp = { {0, 0} };
        DWORD NumberOfBytes;
        DWORD SizeOfData;
        STATSTG statStg;

        IfFailGo(CreateStreamOnHGlobal(NULL,
                                       TRUE,
                                       &pIStream));

        IfFailGo(m_pStringPool->PersistToStream(pIStream));

        IfFailGo(pIStream->Stat(&statStg, STATFLAG_NONAME));
        SizeOfData = statStg.cbSize.u.LowPart;

        IfFailGo(pIStream->Seek(disp, STREAM_SEEK_SET, NULL));

        pStreamMem = NEW(BYTE[SizeOfData]);
        IfFailGo(pIStream->Read(pStreamMem, SizeOfData, &NumberOfBytes));

        if (!WriteFile(m_hFile, pStreamMem, SizeOfData, &NumberOfBytes, NULL))
            return HrFromWin32(GetLastError());

        _ASSERTE(NumberOfBytes == SizeOfData);

    }
ErrExit:
    RELEASE(pIStream);
    DELETEARRAY(pStreamMem);
    return hr;
}

//-----------------------------------------------------------
// SymWriter WritePDB
// Write the PDB information to a file or to a stream
//-----------------------------------------------------------
COM_METHOD SymWriter::WritePDB()
{

    HRESULT hr = NOERROR;
    GUID ildb_guid = ILDB_VERSION_GUID;

    // Make sure the ModuleLevelInfo is set
    ModuleLevelInfo.m_CountOfVars = VAL32(m_MethodInfo.m_vars.count());
    ModuleLevelInfo.m_CountOfBytes = VAL32(m_MethodInfo.m_bytes.count());
    ModuleLevelInfo.m_CountOfUsing = VAL32(m_MethodInfo.m_usings.count());
    ModuleLevelInfo.m_CountOfScopes = VAL32(m_MethodInfo.m_scopes.count());
    ModuleLevelInfo.m_CountOfMethods = VAL32(m_MethodInfo.m_methods.count());
    if (m_pStringPool)
    {
        DWORD dwSaveSize;
        IfFailGo(m_pStringPool->GetSaveSize((UINT32 *)&dwSaveSize));
        ModuleLevelInfo.m_CountOfStringBytes = VAL32(dwSaveSize);
    }
    else
    {
        ModuleLevelInfo.m_CountOfStringBytes = 0;
    }
    ModuleLevelInfo.m_CountOfConstants = VAL32(m_MethodInfo.m_constants.count());
    ModuleLevelInfo.m_CountOfDocuments = VAL32(m_MethodInfo.m_documents.count());
    ModuleLevelInfo.m_CountOfSequencePoints = VAL32(m_MethodInfo.m_auxSequencePoints.count());

    // Open the file
    if (m_pIStream == NULL)
    {
        // We need to open the output file.
        m_hFile = WszCreateFile(m_szPath,
                            GENERIC_WRITE,
                            0,
                            NULL,
                            CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL,
                            NULL);

        if (m_hFile == INVALID_HANDLE_VALUE)
        {
            IfFailGo(HrFromWin32(GetLastError()));
        }
    }
    else
    {
        // We're writing to a stream.  Make sure we're at the beginning
        // (eg. if this is being called more than once).
        // Note that technically we should probably call SetSize to truncate the
        // stream to ensure we don't leave reminants of the previous contents
        // at the end of the new stream.  But with our current CGrowableStream
        // implementation, this would have a big performance impact (causing us to
        // do linear growth and lots of reallocations at every write).  We only
        // ever add data to a symbol writer (don't remove anything), and so subsequent
        // streams should always get larger.  Regardless, ILDB supports trailing garbage
        // without a problem (we used to always have the remainder of a page at the end
        // of the stream), and so this is not an issue of correctness.
        LARGE_INTEGER pos0;
        pos0.QuadPart = 0;
        IfFailGo(m_pIStream->Seek(pos0, STREAM_SEEK_SET, NULL));
    }

#if _DEBUG
    // We need to make sure the Variant entry in the constants is 8 byte
    // aligned so make sure everything up to the there is aligned correctly
    if ((ILDB_SIGNATURE_SIZE % 8) || 
        (sizeof(PDBInfo) % 8) ||
        (sizeof(GUID) % 8))
    {
        _ASSERTE(!"We need to safe the data in an aligned format");
    }
#endif

    // Write out a signature to recognize that we're an ildb
    IfFailGo(Write((void *)ILDB_SIGNATURE, ILDB_SIGNATURE_SIZE));
    // Write out a guid representing the version
    SwapGuid(&ildb_guid);
    IfFailGo(Write((void *)&ildb_guid, sizeof(GUID)));

    // Now we need to write the Project level 
    IfFailGo(Write(&ModuleLevelInfo, sizeof(PDBInfo)));

    // Now we have to write out each array as appropriate
    IfFailGo(Write(m_MethodInfo.m_constants.m_array, sizeof(SymConstant) * m_MethodInfo.m_constants.count()));

    // These members are all 4 byte aligned
    IfFailGo(Write(m_MethodInfo.m_methods.m_array, sizeof(SymMethodInfo) * m_MethodInfo.m_methods.count()));
    IfFailGo(Write(m_MethodInfo.m_scopes.m_array, sizeof(SymLexicalScope) * m_MethodInfo.m_scopes.count()));
    IfFailGo(Write(m_MethodInfo.m_vars.m_array, sizeof(SymVariable) * m_MethodInfo.m_vars.count()));
    IfFailGo(Write(m_MethodInfo.m_usings.m_array, sizeof(SymUsingNamespace) * m_MethodInfo.m_usings.count()));
    IfFailGo(Write(m_MethodInfo.m_auxSequencePoints.m_array, sizeof(SequencePoint) * m_MethodInfo.m_auxSequencePoints.count()));
    IfFailGo(Write(m_MethodInfo.m_documents.m_array, sizeof(DocumentInfo) * m_MethodInfo.m_documents.count()));
    IfFailGo(Write(m_MethodInfo.m_bytes.m_array, sizeof(BYTE) * m_MethodInfo.m_bytes.count()));
    IfFailGo(WriteStringPool());

ErrExit:
    if (m_hFile)
        CloseHandle(m_hFile);
    return hr;
}

/* ------------------------------------------------------------------------- *
 * SymDocumentWriter class
 * ------------------------------------------------------------------------- */
SymDocumentWriter::SymDocumentWriter(
    UINT32 DocumentEntry,
    SymWriter  *pEmitter
) :
    m_refCount ( 0 ),
    m_DocumentEntry ( DocumentEntry ),
    m_pEmitter( pEmitter )
{
    _ASSERTE(pEmitter);
    m_pEmitter->AddRef();
}

SymDocumentWriter::~SymDocumentWriter()
{
    // Note that this must be thread-safe - it may be invoked on the finalizer thread
    RELEASE(m_pEmitter);
}

COM_METHOD SymDocumentWriter::QueryInterface(REFIID riid, void **ppInterface)
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ISymUnmanagedDocumentWriter)
        *ppInterface = (ISymUnmanagedDocumentWriter*)this;
    else if (riid == IID_IUnknown)
        *ppInterface = (IUnknown*)(ISymUnmanagedDocumentWriter*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

//-----------------------------------------------------------
// SymDocumentWriter SetSource
//-----------------------------------------------------------
COM_METHOD SymDocumentWriter::SetSource(ULONG32 sourceSize,
                                        BYTE source[])
{
    return m_pEmitter->SetDocumentSrc(m_DocumentEntry, sourceSize, source);
}

//-----------------------------------------------------------
// SymDocumentWriter SetCheckSum
//-----------------------------------------------------------
COM_METHOD SymDocumentWriter::SetCheckSum(GUID algorithmId,
                                          ULONG32 checkSumSize,
                                          BYTE checkSum[])
{
    return m_pEmitter->SetDocumentCheckSum(m_DocumentEntry, algorithmId, checkSumSize, checkSum);
}


//-----------------------------------------------------------
// DocumentInfo SetDocumentWriter
//-----------------------------------------------------------
// Set the pointer to the SymDocumentWriter instance corresponding to this instance of DocumentInfo
// An argument of NULL will call Release
// Arguments
//     input: pDoc - pointer to the associated SymDocumentWriter or NULL

void DocumentInfo::SetDocumentWriter(SymDocumentWriter * pDoc)
{
    if (m_pDocumentWriter != NULL)
    {
        m_pDocumentWriter->Release();
    }
    m_pDocumentWriter = pDoc;
    if (m_pDocumentWriter != NULL)
    {
        pDoc->AddRef();
    }
}
