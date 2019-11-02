// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: SymWrite.h
//

// ===========================================================================

#ifndef SYMWRITE_H_
#define SYMWRITE_H_
#ifdef _MSC_VER
#pragma warning(disable:4786)
#endif

#include <windows.h>
#include <stdlib.h>
#include <stdio.h>

#include "cor.h"
#include "umisc.h"
#include "stgpool.h"
#include "safemath.h"

#include <corsym.h>
#include "pdbdata.h"

class SymDocumentWriter;

#if BIGENDIAN
/***
*PUBLIC void VariantSwap
*Purpose:
*  Swap the Variant members
*
*Entry:
*  SrcInBigEndian = whether pvarg is in BIGENDIAN or not
*  pvargDest = Destination variant
*  pvarg = pointer to a VARIANT to swap
*
*Exit:
*  Filled in pvarDest
*
***********************************************************************/
inline HRESULT VariantSwap(bool SrcInBigEndian, VARIANT FAR *pvargDest, VARIANT FAR* pvarg)
{
    if (pvargDest == NULL || pvarg == NULL)
        return E_INVALIDARG;
    VARTYPE vt = VT_EMPTY;

    if (SrcInBigEndian)
    {
        vt = V_VT(pvarg);
    }
    *(UINT32*)pvargDest = VAL32(*(UINT32*)pvarg);
    if (!SrcInBigEndian)
    {
        vt = V_VT(pvargDest);
    }

    switch (vt)
    {
        case VT_EMPTY:
        case VT_NULL:
            // No Value to swap
            break;

            // 1 byte
        case VT_I1:
        case VT_UI1:
            V_I1(pvargDest) = V_I1(pvarg);
            break;

            // 2 bytes
        case VT_I2:
        case VT_UI2:
        case VT_INT:
        case VT_UINT:
        case VT_BOOL:
            V_I2(pvargDest) = VAL16(V_I2(pvarg));
            break;

            // 4 bytes
        case VT_I4:
        case VT_UI4:
        case VT_R4:
            V_I4(pvargDest) = VAL32(V_I4(pvarg));
            break;

            // 8 bytes
        case VT_I8:
        case VT_UI8:
        case VT_R8:
        case VT_DATE:
            V_I8(pvargDest) = VAL64(V_I8(pvarg));
            break;

        case VT_DECIMAL:
            DECIMAL_HI32(V_DECIMAL(pvargDest)) = VAL32(DECIMAL_HI32(V_DECIMAL(pvarg)));
            DECIMAL_LO32(V_DECIMAL(pvargDest)) = VAL32(DECIMAL_LO32(V_DECIMAL(pvarg)));
            DECIMAL_MID32(V_DECIMAL(pvargDest)) = VAL32(DECIMAL_MID32(V_DECIMAL(pvarg)));
            break;

        // These aren't currently supported
        case VT_CY:         //6
        case VT_BSTR:       //8
        case VT_DISPATCH:   //9
        case VT_ERROR:      //10
        case VT_VARIANT:    //12
        case VT_UNKNOWN:    //13
        case VT_VOID:       //24
        case VT_HRESULT:    //25
        case VT_PTR:        //26
        case VT_SAFEARRAY:  //27
        case VT_CARRAY:     //28
        case VT_USERDEFINED://29
        case VT_LPSTR:      //30
        case VT_LPWSTR:     //31
        case VT_FILETIME:   //64
        case VT_BLOB:       //65
        case VT_STREAM:     //66
        case VT_STORAGE:    //67
        case VT_STREAMED_OBJECT:    //68
        case VT_STORED_OBJECT:      //69
        case VT_BLOB_OBJECT:        //70
        case VT_CF:                 //71
        case VT_CLSID:              //72
        default:
            _ASSERTE(!"NYI");
            break;
    }
    return NOERROR;
}
#endif // BIGENDIAN

// Default space sizes for the various arrays. Make it too small in a
// checked build so we exercise the growing code.
#ifdef _DEBUG
#define DEF_LOCAL_SPACE 2
#define DEF_MISC_SPACE  64
#else
#define DEF_LOCAL_SPACE 64
#define DEF_MISC_SPACE  1024
#endif

/* ------------------------------------------------------------------------- *
 * SymVariable struct
 * ------------------------------------------------------------------------- */
struct SymVariable
{
private:
    UINT32           m_Scope;       // index of parent scope
    UINT32           m_Name;        // index into misc byte array
    ULONG32          m_Attributes;  // Attributes
    UINT32           m_Signature;       // index into misc byte array
    ULONG32          m_SignatureSize;   // Signature size
    ULONG32          m_AddrKind;    // Address Kind
    ULONG32          m_Addr1;       // Additional info
    ULONG32          m_Addr2;
    ULONG32          m_Addr3;
    ULONG32          m_StartOffset; // StartOffset
    ULONG32          m_EndOffset;   // EndOffset
    ULONG32          m_Sequence;
    BOOL             m_IsParam;     // parameter?
    BOOL             m_IsHidden;    // Is not visible to the user

public:
    UINT32 Scope()
    {
        return VAL32(m_Scope);
    }
    void SetScope(UINT32 Scope)
    {
        m_Scope = VAL32(Scope);
    }

    UINT32 Name()
    {
        return VAL32(m_Name);
    }
    void SetName(UINT32 Name)
    {
        m_Name = VAL32(Name);
    }

    ULONG32 Attributes()
    {
        return VAL32(m_Attributes);
    }
    void SetAttributes(ULONG32 Attributes)
    {
        m_Attributes = VAL32(Attributes);
    }

    UINT32 Signature()
    {
        return VAL32(m_Signature);
    }
    void SetSignature(UINT32 Signature)
    {
        m_Signature = VAL32(Signature);
    }
    ULONG32 SignatureSize()
    {
        return VAL32(m_SignatureSize);
    }
    void SetSignatureSize(ULONG32 SignatureSize)
    {
        m_SignatureSize = VAL32(SignatureSize);
    }

    ULONG32 AddrKind()
    {
        return VAL32(m_AddrKind);
    }
    void SetAddrKind(ULONG32 AddrKind)
    {
        m_AddrKind = VAL32(AddrKind);
    }
    ULONG32 Addr1()
    {
        return VAL32(m_Addr1);
    }
    void SetAddr1(ULONG32 Addr1)
    {
        m_Addr1 = VAL32(Addr1);
    }

    ULONG32 Addr2()
    {
        return VAL32(m_Addr2);
    }
    void SetAddr2(ULONG32 Addr2)
    {
        m_Addr2 = VAL32(Addr2);
    }

    ULONG32 Addr3()
    {
        return VAL32(m_Addr3);
    }
    void SetAddr3(ULONG32 Addr3)
    {
        m_Addr3 = VAL32(Addr3);
    }

    ULONG32 StartOffset()
    {
        return VAL32(m_StartOffset);
    }
    void SetStartOffset(ULONG32 StartOffset)
    {
        m_StartOffset = VAL32(StartOffset);
    }
    ULONG32 EndOffset()
    {
        return VAL32(m_EndOffset);
    }
    void SetEndOffset(ULONG EndOffset)
    {
        m_EndOffset = VAL32(EndOffset);
    }
    ULONG32 Sequence()
    {
        return VAL32(m_Sequence);
    }
    void SetSequence(ULONG32 Sequence)
    {
        m_Sequence = VAL32(Sequence);
    }

    BOOL    IsParam()
    {
        return VAL32(m_IsParam);
    }
    void SetIsParam(BOOL IsParam)
    {
        m_IsParam = IsParam;
    }
    BOOL    IsHidden()
    {
        return VAL32(m_IsHidden);
    }
    void SetIsHidden(BOOL IsHidden)
    {
        m_IsHidden = IsHidden;
    }
};

/* ------------------------------------------------------------------------- *
 * SymLexicalScope struct
 * ------------------------------------------------------------------------- */
struct SymLexicalScope
{
private:

    UINT32 m_ParentScope;          // parent index (-1 for no parent)
    ULONG32 m_StartOffset;    // start offset
    ULONG32 m_EndOffset;      // end offset
    BOOL    m_HasChildren;    // scope has children
    BOOL    m_HasVars;        // scope has vars?
public:
    UINT32  ParentScope()
    {
        return VAL32(m_ParentScope);
    }
    void SetParentScope(UINT32 ParentScope)
    {
        m_ParentScope = VAL32(ParentScope);
    }

    ULONG32 StartOffset()
    {
        return VAL32(m_StartOffset);
    }
    void SetStartOffset(ULONG32 StartOffset)
    {
        m_StartOffset = VAL32(StartOffset);
    }
    ULONG32 EndOffset()
    {
        return VAL32(m_EndOffset);
    }
    void SetEndOffset(ULONG32 EndOffset)
    {
        m_EndOffset = VAL32(EndOffset);
    }
    BOOL    HasChildren()
    {
        return m_HasChildren;
    }
    void SetHasChildren(BOOL HasChildren)
    {
        m_HasChildren = HasChildren;
    }
    BOOL    HasVars()
    {
        return m_HasVars;
    }
    void SetHasVars(BOOL HasVars)
    {
        m_HasVars = HasVars;
    }

};

/* ------------------------------------------------------------------------- *
 * SymUsingNamespace struct
 * ------------------------------------------------------------------------- */
struct SymUsingNamespace
{
private:

    UINT32  m_ParentScope;  // index of parent scope
    UINT32  m_Name;         // Index of name
public:
    UINT32  ParentScope()
    {
        return VAL32(m_ParentScope);
    }
    void SetParentScope(UINT32 ParentScope)
    {
        m_ParentScope = VAL32(ParentScope);
    }
    UINT32  Name()
    {
        return VAL32(m_Name);
    }
    void SetName(UINT32 Name)
    {
        m_Name = VAL32(Name);
    }
};

/* ------------------------------------------------------------------------- *
 * SymConstant struct
 * ------------------------------------------------------------------------- */
struct SymConstant
{
private:

    VARIANT m_Value;   // Constant Value
    UINT32 m_ParentScope;   // Parent scope
    UINT32 m_Name;          // Name index
    UINT32 m_Signature;     // Signature index
    ULONG32 m_SignatureSize;// Signature size
    UINT32 m_ValueBstr; // If the variant is a bstr, store the string

public:
    UINT32  ParentScope()
    {
        return VAL32(m_ParentScope);
    }
    void SetParentScope(UINT32 ParentScope)
    {
        m_ParentScope = VAL32(ParentScope);
    }
    UINT32  Name()
    {
        return VAL32(m_Name);
    }
    void SetName(UINT32 Name)
    {
        m_Name = VAL32(Name);
    }
    UINT32 Signature()
    {
        return VAL32(m_Signature);
    }
    void SetSignature(UINT32 Signature)
    {
        m_Signature = VAL32(Signature);
    }
    ULONG32 SignatureSize()
    {
        return VAL32(m_SignatureSize);
    }
    void SetSignatureSize(ULONG32 SignatureSize)
    {
        m_SignatureSize = VAL32(SignatureSize);
    }
    VARIANT Value(UINT32 *pValueBstr)
    {
        *pValueBstr = VAL32(m_ValueBstr);
#if BIGENDIAN
        VARIANT VariantValue;
        VariantInit(&VariantValue);
        // VT_BSTR's are dealt with ValueBStr
        if (m_ValueBstr)
        {
            V_VT(&VariantValue) = VT_BSTR;
        }
        else
        {
            VariantSwap(false, &VariantValue, &m_Value);
        }
        return VariantValue;
#else
        return m_Value;
#endif
    }
    void SetValue(VARIANT VariantValue, UINT32 ValueBstr)
    {
        m_Value = VariantValue;
        m_ValueBstr = VAL32(ValueBstr);
#if BIGENDIAN
        // VT_BSTR's are dealt with ValueBStr
        if (m_ValueBstr)
        {
            V_VT(&m_Value) = VAL16(VT_BSTR);
        }
        else
        {
            VariantSwap(true, &m_Value, &VariantValue);
        }
#endif
    }
};

/* ------------------------------------------------------------------------- *
 * SymMethodInfo struct
 * ------------------------------------------------------------------------- */
struct SymMethodInfo
{
private:

    mdMethodDef     m_MethodToken;    // Method token

    // Start/End Entries into the respective tables
    // End values are extents - one past the last index (and so may actually be an index off
    // the end of the array).  Start may equal end if the method has none of the item.
    UINT32          m_StartScopes;
    UINT32          m_EndScopes;
    UINT32          m_StartVars;
    UINT32          m_EndVars;
    UINT32          m_StartUsing;
    UINT32          m_EndUsing;
    UINT32          m_StartConstant;
    UINT32          m_EndConstant;
    UINT32          m_StartDocuments;
    UINT32          m_EndDocuments;
    UINT32          m_StartSequencePoints;
    UINT32          m_EndSequencePoints;

public:
    static int __cdecl compareMethods(const void *elem1, const void *elem2 );

    mdMethodDef     MethodToken()
    {
        return VAL32(m_MethodToken);
    }
    void SetMethodToken(mdMethodDef MethodToken)
    {
        m_MethodToken = VAL32(MethodToken);
    }
    UINT32  StartScopes()
    {
        return VAL32(m_StartScopes);
    }
    void SetStartScopes(UINT32 StartScopes)
    {
        m_StartScopes = VAL32(StartScopes);
    }
    UINT32  EndScopes()
    {
        return VAL32(m_EndScopes);
    }
    void SetEndScopes(UINT32 EndScopes)
    {
        m_EndScopes = VAL32(EndScopes);
    }
    UINT32 StartVars()
    {
        return VAL32(m_StartVars);
    }
    void SetStartVars(UINT32 StartVars)
    {
        m_StartVars = VAL32(StartVars);
    }
    UINT32 EndVars()
    {
        return VAL32(m_EndVars);
    }
    void SetEndVars(UINT32 EndVars)
    {
        m_EndVars = VAL32(EndVars);
    }
    UINT32 StartUsing()
    {
        return VAL32(m_StartUsing);
    }
    void SetStartUsing(UINT32 StartUsing)
    {
        m_StartUsing = VAL32(StartUsing);
    }
    UINT32 EndUsing()
    {
        return VAL32(m_EndUsing);
    }
    void SetEndUsing(UINT32 EndUsing)
    {
        m_EndUsing = VAL32(EndUsing);
    }
    UINT32 StartConstant()
    {
        return VAL32(m_StartConstant);
    }
    void SetStartConstant(UINT32 StartConstant)
    {
        m_StartConstant = VAL32(StartConstant);
    }
    UINT32 EndConstant()
    {
        return VAL32(m_EndConstant);
    }
    void SetEndConstant(UINT32 EndConstant)
    {
        m_EndConstant = VAL32(EndConstant);
    }
    UINT32 StartDocuments()
    {
        return VAL32(m_StartDocuments);
    }
    void SetStartDocuments(UINT32 StartDocuments)
    {
        m_StartDocuments = VAL32(StartDocuments);
    }
    UINT32 EndDocuments()
    {
        return VAL32(m_EndDocuments);
    }
    void SetEndDocuments(UINT32 EndDocuments)
    {
        m_EndDocuments = VAL32(EndDocuments);
    }
    UINT32 StartSequencePoints()
    {
        return VAL32(m_StartSequencePoints);
    }
    void SetStartSequencePoints(UINT32 StartSequencePoints)
    {
        m_StartSequencePoints = VAL32(StartSequencePoints);
    }
    UINT32 EndSequencePoints()
    {
        return VAL32(m_EndSequencePoints);
    }
    void SetEndSequencePoints(UINT32 EndSequencePoints)
    {
        m_EndSequencePoints = VAL32(EndSequencePoints);
    }
};

/* ------------------------------------------------------------------------- *
 * SymMap struct
 * ------------------------------------------------------------------------- */
struct SymMap
{
    mdMethodDef     m_MethodToken;    // New Method token
    UINT32          MethodEntry;      // Method Entry
};

/* ------------------------------------------------------------------------- *
 * SequencePoint struct
 * ------------------------------------------------------------------------- */
struct SequencePoint {

private:

    DWORD   m_Offset;
    DWORD   m_StartLine;
    DWORD   m_StartColumn;
    DWORD   m_EndLine;
    DWORD   m_EndColumn;
    DWORD   m_Document;

public:
    bool IsWithin(ULONG32 line, ULONG32 column);
    bool IsWithinLineOnly(ULONG32 line);
    bool IsGreaterThan(ULONG32 line, ULONG32 column);
    bool IsLessThan(ULONG32 line, ULONG32 column);
    bool IsUserLine();
    static int __cdecl compareAuxLines(const void *elem1, const void *elem2 );

    DWORD Offset()
    {
        return VAL32(m_Offset);
    }
    void SetOffset(DWORD Offset)
    {
        m_Offset = VAL32(Offset);
    }
    DWORD StartLine()
    {
        return VAL32(m_StartLine);
    }
    void SetStartLine(DWORD StartLine)
    {
        m_StartLine = VAL32(StartLine);
    }

    DWORD StartColumn()
    {
        return VAL32(m_StartColumn);
    }
    void SetStartColumn(DWORD StartColumn)
    {
        m_StartColumn = VAL32(StartColumn);
    }

    DWORD EndLine()
    {
        return VAL32(m_EndLine);
    }
    void SetEndLine(DWORD EndLine)
    {
        m_EndLine = VAL32(EndLine);
    }
    DWORD EndColumn()
    {
        return VAL32(m_EndColumn);
    }
    void SetEndColumn(DWORD EndColumn)
    {
        m_EndColumn = VAL32(EndColumn);
    }
    DWORD Document()
    {
        return VAL32(m_Document);
    }
    void SetDocument(DWORD Document)
    {
        m_Document = VAL32(Document);
    }
};


/* ------------------------------------------------------------------------- *
 * DocumentInfo struct
 * ------------------------------------------------------------------------- */
typedef struct DocumentInfo {

private:

    GUID                m_Language;
    GUID                m_LanguageVendor;
    GUID                m_DocumentType;
    GUID                m_AlgorithmId;
    DWORD               m_CheckSumSize;
    UINT32              m_CheckSumEntry;
    UINT32              m_SourceSize;
    UINT32              m_SourceEntry;
    UINT32              m_UrlEntry;
    SymDocumentWriter * m_pDocumentWriter;

public:

    GUID Language()
    {
        GUID TmpGuid = m_Language;
        SwapGuid(&TmpGuid);
        return TmpGuid;
    }
    void SetLanguage(GUID Language)
    {
        SwapGuid(&Language);
        m_Language = Language;
    }
    GUID LanguageVendor()
    {
        GUID TmpGuid = m_LanguageVendor;
        SwapGuid(&TmpGuid);
        return TmpGuid;
    }
    void SetLanguageVendor(GUID LanguageVendor)
    {
        SwapGuid(&LanguageVendor);
        m_LanguageVendor = LanguageVendor;
    }
    GUID DocumentType()
    {
        GUID TmpGuid = m_DocumentType;
        SwapGuid(&TmpGuid);
        return TmpGuid;
    }
    void SetDocumentType(GUID DocumentType)
    {
        SwapGuid(&DocumentType);
        m_DocumentType = DocumentType;
    }

    // Set the pointer to the SymDocumentWriter instance corresponding to this instance of DocumentInfo
    // An argument of NULL will call Release
    void SetDocumentWriter(SymDocumentWriter * pDoc);

    // get the associated SymDocumentWriter
    SymDocumentWriter * DocumentWriter()
    {
        return m_pDocumentWriter;
    }

    GUID AlgorithmId()
    {
        GUID TmpGuid = m_AlgorithmId;
        SwapGuid(&TmpGuid);
        return TmpGuid;
    }
    void SetAlgorithmId(GUID AlgorithmId)
    {
        SwapGuid(&AlgorithmId);
        m_AlgorithmId = AlgorithmId;
    }

    DWORD CheckSumSize()
    {
        return VAL32(m_CheckSumSize);
    }
    void SetCheckSymSize(DWORD CheckSumSize)
    {
        m_CheckSumSize = VAL32(CheckSumSize);
    }
    UINT32 CheckSumEntry()
    {
        return VAL32(m_CheckSumEntry);
    }
    void SetCheckSumEntry(UINT32 CheckSumEntry)
    {
        m_CheckSumEntry = VAL32(CheckSumEntry);
    }
    UINT32 SourceSize()
    {
        return VAL32(m_SourceSize);
    }
    void SetSourceSize(UINT32 SourceSize)
    {
        m_SourceSize = VAL32(SourceSize);
    }
    UINT32 SourceEntry()
    {
        return VAL32(m_SourceEntry);
    }
    void SetSourceEntry(UINT32 SourceEntry)
    {
        m_SourceEntry = VAL32(SourceEntry);
    }
    UINT32 UrlEntry()
    {
        return VAL32(m_UrlEntry);
    }
    void SetUrlEntry(UINT32 UrlEntry)
    {
        m_UrlEntry = VAL32(UrlEntry);
    }

} DocumentInfo;

template <class T>
class ArrayStorage
{
public:

    ArrayStorage( int initialSize = 0 )
        : m_spaceSize(0),  m_instanceCount( 0 ), m_array( NULL )
    {
        grow( initialSize );
    }
    ~ArrayStorage()
    {

        if ( m_array )
            DELETEARRAY(m_array);
        m_array = NULL;
        m_spaceSize = 0;
        m_instanceCount = 0;
    }
    T* next()
    {
        if( !grow ( m_instanceCount ) )
            return NULL;
        _ASSERTE( m_instanceCount < m_spaceSize );
        return &m_array[ m_instanceCount++ ];
    }
    bool grab(UINT32 n, UINT32 * pIndex)
    {
        S_UINT32 newSize = S_UINT32(m_instanceCount) + S_UINT32(n);
        if (newSize.IsOverflow())
            return false;
        if (!grow(newSize.Value()))
            return false;
        _ASSERTE( m_instanceCount < m_spaceSize );
        *pIndex = m_instanceCount;
        m_instanceCount += n;
        return true;
    }

    T& operator[]( UINT32 i ) {
        _ASSERTE( i < m_instanceCount );
        if (i >= m_instanceCount)
        {
            // Help mitigate the impact of buffer overflow
            // Fail fast with a null-reference AV
            volatile char* nullPointer = nullptr;
            *nullPointer;
        }
        return m_array[ i ];
    }
    void reset() {
        m_instanceCount = 0;
    }
    UINT32 size() {
        return m_spaceSize;
    }
    UINT32 count() {
        return m_instanceCount;
    }

    UINT32      m_spaceSize;     // Total size of array in elements
    UINT32      m_instanceCount;   // total T's in the file
    T          *m_array;         // array of T's
private:
    bool grow( UINT32 n )
    {
        if (n >= m_spaceSize)
        {
            // Make a new, bigger array.
            UINT32 newSpaceSize;

            if (n == 0)
                newSpaceSize = DEF_LOCAL_SPACE;
            else
                newSpaceSize = max( m_spaceSize * 2, n);

            // Make sure we're not asking for more than 4GB of bytes to ensure no integer-overflow attacks are possible
            S_UINT32 newBytes = S_UINT32(newSpaceSize) * S_UINT32(sizeof(T));
            if (newBytes.IsOverflow())
                return false;

            T *newTs;
            newTs = NEW(T[newSpaceSize]);
            if ( newTs == NULL )
                return false;

            // Copy over the old Ts.
            memcpy(newTs, m_array,
                   sizeof(T) * m_spaceSize);

            // Delete the old Ts.
            DELETEARRAY(m_array);

            // Hang onto the new array.
            m_array = newTs;
            m_spaceSize = newSpaceSize;
        }
        return true;
    }

};

typedef struct MethodInfo {

    ArrayStorage<SymMethodInfo> m_methods;    // Methods information
    ArrayStorage<SymLexicalScope> m_scopes;   // Scope information for the method
    ArrayStorage<SymVariable> m_vars;         // Variables
    ArrayStorage<SymUsingNamespace> m_usings; // using/imports
    ArrayStorage<SymConstant> m_constants;    // Constants
    ArrayStorage<DocumentInfo> m_documents;   // Document Source Format
    ArrayStorage<SequencePoint>  m_auxSequencePoints;  // Sequence Points
    // Array of various bytes (variable signature, etc)
    ArrayStorage<BYTE>  m_bytes;


public:

  MethodInfo() :
      m_bytes( DEF_MISC_SPACE )
  {
  }
} MethodInfo;

/* ------------------------------------------------------------------------- *
 * SymWriter class
 * ------------------------------------------------------------------------- */

class SymWriter : public ISymUnmanagedWriter3
{
public:
    SymWriter();
    virtual ~SymWriter();

    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        // Note that this must be thread-safe - it may be invoked on the finalizer thread
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ISymUnmanagedWriter
    //-----------------------------------------------------------
    COM_METHOD DefineDocument(const WCHAR *url,
                              const GUID *language,
                              const GUID *languageVendor,
                              const GUID *documentType,
                              ISymUnmanagedDocumentWriter **pRetVal);
    COM_METHOD SetUserEntryPoint(mdMethodDef entryMethod);
    COM_METHOD OpenMethod(mdMethodDef method);
    COM_METHOD CloseMethod();
    COM_METHOD DefineSequencePoints(ISymUnmanagedDocumentWriter *document,
                                    ULONG32 spCount,
                                    ULONG32 offsets[],
                                    ULONG32 lines[],
                                    ULONG32 columns[],
                                    ULONG32 endLines[],
                                    ULONG32 encColumns[]);
    COM_METHOD OpenScope(ULONG32 startOffset, ULONG32 *scopeID);
    COM_METHOD CloseScope(ULONG32 endOffset);
    COM_METHOD SetScopeRange(ULONG32 scopeID, ULONG32 startOffset, ULONG32 endOffset);
    COM_METHOD DefineLocalVariable(const WCHAR *name,
                                   ULONG32 attributes,
                                   ULONG32 cSig,
                                   BYTE signature[],
                                   ULONG32 addrKind,
                                   ULONG32 addr1, ULONG32 addr2, ULONG32 addr3,
                                   ULONG32 startOffset, ULONG32 endOffset);
    COM_METHOD DefineParameter(const WCHAR *name,
                               ULONG32 attributes,
                               ULONG32 sequence,
                               ULONG32 addrKind,
                               ULONG32 addr1, ULONG32 addr2, ULONG32 addr3);
    COM_METHOD DefineField(mdTypeDef parent,
                           const WCHAR *name,
                           ULONG32 attributes,
                           ULONG32 cSig,
                           BYTE signature[],
                           ULONG32 addrKind,
                           ULONG32 addr1, ULONG32 addr2, ULONG32 addr3);
    COM_METHOD DefineGlobalVariable(const WCHAR *name,
                                    ULONG32 attributes,
                                    ULONG32 cSig,
                                    BYTE signature[],
                                    ULONG32 addrKind,
                                    ULONG32 addr1, ULONG32 addr2, ULONG32 addr3);
    COM_METHOD Close();
    COM_METHOD SetSymAttribute(mdToken parent,
                               const WCHAR *name,
                               ULONG32 cData,
                               BYTE data[]);
    COM_METHOD OpenNamespace(const WCHAR *name);
    COM_METHOD CloseNamespace();
    COM_METHOD UsingNamespace(const WCHAR *fullName);
    COM_METHOD SetMethodSourceRange(ISymUnmanagedDocumentWriter *startDoc,
                                    ULONG32 startLine,
                                    ULONG32 startColumn,
                                    ISymUnmanagedDocumentWriter *endDoc,
                                    ULONG32 endLine,
                                    ULONG32 endColumn);
    COM_METHOD GetDebugCVInfo(DWORD cData,
                           DWORD *pcData,
                           BYTE data[]);

    COM_METHOD Initialize(IUnknown *emitter,
                        const WCHAR *filename,
                        IStream *pIStream,
                        BOOL fFullBuild);

    COM_METHOD Initialize2(IUnknown *emitter,
                        const WCHAR *pdbTempPath,   // location to write pdb file
                        IStream *pIStream,
                        BOOL fFullBuild,
                        const WCHAR *pdbFinalPath); // location exe should contain for pdb file

    COM_METHOD GetDebugInfo(IMAGE_DEBUG_DIRECTORY *pIDD,
                         DWORD cData,
                         DWORD *pcData,
                         BYTE data[]);

    COM_METHOD RemapToken(mdToken oldToken,
                          mdToken newToken);

    COM_METHOD DefineConstant(const WCHAR __RPC_FAR *name,
                        VARIANT value,
                        ULONG32 cSig,
                        unsigned char __RPC_FAR signature[  ]);

    COM_METHOD Abort(void);

    //-----------------------------------------------------------
    // ISymUnmanagedWriter2
    //-----------------------------------------------------------
    COM_METHOD DefineLocalVariable2(const WCHAR *name,
                        ULONG32 attributes,
                        mdSignature sigToken,
                        ULONG32 addrKind,
                        ULONG32 addr1,
                        ULONG32 addr2,
                        ULONG32 addr3,
                        ULONG32 startOffset,
                        ULONG32 endOffset);

    COM_METHOD DefineGlobalVariable2(const WCHAR *name,
                        ULONG32 attributes,
                        mdSignature sigToken,
                        ULONG32 addrKind,
                        ULONG32 addr1,
                        ULONG32 addr2,
                        ULONG32 addr3);

    COM_METHOD DefineConstant2(const WCHAR *name,
                        VARIANT value,
                        mdSignature sigToken);

    //-----------------------------------------------------------
    // ISymUnmanagedWriter3
    //-----------------------------------------------------------

    COM_METHOD OpenMethod2(mdMethodDef method,
                        ULONG32 isect,
                        ULONG32 offset);

    COM_METHOD Commit();

    //-----------------------------------------------------------
    // Methods not exposed via a COM interface.
    //-----------------------------------------------------------

    static HRESULT NewSymWriter(REFIID clsid, void** ppObj);
    HRESULT SetDocumentCheckSum(
            UINT32 DocumentEntry,
            GUID  AlgorithmId,
            DWORD CheckSumSize,
            BYTE* pCheckSum);
    HRESULT SetDocumentSrc(UINT32 DocumentEntry,
            DWORD SourceSize,
            BYTE* pSource);

    COM_METHOD Write(void *pData, DWORD SizeOfData);
    COM_METHOD WriteStringPool();
    COM_METHOD WritePDB();

    COM_METHOD Initialize(const WCHAR *szFilename, IStream *pIStream);

    void SetFullPathName(const WCHAR *szFullPathName)
    {

    }

private:
    // Helper API for CloserScope
    COM_METHOD CloseScopeInternal(ULONG32 endOffset);
    HRESULT GetOrCreateDocument(
        const WCHAR *wcsUrl,          // Document name
        const GUID *pLanguage,        // What Language we're compiling
        const GUID *pLanguageVendor,  // What vendor
        const GUID *pDocumentType,    // Type
        ISymUnmanagedDocumentWriter **ppRetVal // [out] Created DocumentWriter
    );
    HRESULT CreateDocument(
        const WCHAR *wcsUrl,          // Document name
        const GUID *pLanguage,        // What Language we're compiling
        const GUID *pLanguageVendor,  // What vendor
        const GUID *pDocumentType,    // Type
        ISymUnmanagedDocumentWriter **ppRetVal // [out] Created DocumentWriter
    );


    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------
private:
    UINT32      m_refCount; // AddRef/Release

    mdMethodDef         m_openMethodToken;
    mdMethodDef         m_LargestMethodToken;
    SymMethodInfo     * m_pmethod;

    // index of currently open scope
    UINT32 m_currentScope;

    // special scope "index" meaning there is no such scope
    static const UINT32 k_noScope = (UINT32)-1;

    // maximum scope end offset seen so far in this method
    ULONG32 m_maxScopeEnd;

    MethodInfo m_MethodInfo;
    ArrayStorage<SymMap> m_MethodMap;    // Methods information

    // Symbol File Name
    WCHAR m_szPath[ _MAX_PATH ];
    // File Handle
    HANDLE m_hFile;
    // Stream we're storing into if asked to.
    IStream* m_pIStream;

    // StringPool we use to store the string into
    StgStringPool *m_pStringPool;

    // Project level symbol information
    PDBInfo ModuleLevelInfo;

    bool                m_closed;       // Have we closed the file yet?
    bool                m_sortLines;    // sort the line for current method
    bool                m_sortMethodEntries; // Sort the method entries


};

/* ------------------------------------------------------------------------- *
 * SymDocumentWriter class
 * ------------------------------------------------------------------------- */

class SymDocumentWriter : public ISymUnmanagedDocumentWriter
{
public:
    SymDocumentWriter(UINT32 DocumentEntry,
                      SymWriter  *pEmitter);

    virtual ~SymDocumentWriter();

    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        // Note that this must be thread-safe - it may be invoked on the finalizer thread
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ISymUnmanagedDocumentWriter
    //-----------------------------------------------------------
    COM_METHOD SetSource(ULONG32 sourceSize, BYTE source[]);
    COM_METHOD SetCheckSum(GUID algorithmId,
                           ULONG32 checkSumSize, BYTE checkSum[]);

    //-----------------------------------------------------------
    // Methods not exposed via a COM interface.
    //-----------------------------------------------------------
    //
    // Commit the doc to the pdb
    //
    UINT32 GetDocumentEntry()
    {
        return m_DocumentEntry;
    }

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------
private:
    UINT32 m_refCount; // AddRef/Release
    UINT32 m_DocumentEntry; // Entry into the documents array
    SymWriter *m_pEmitter;  // Associated SymWriter
};

// Debug Info
struct RSDSI                       // RSDS debug info
{
    DWORD   dwSig;                 // RSDS
    GUID    guidSig;
    DWORD   age;
    char    szPDB[0];  // followed by a zero-terminated UTF8 file name
};

#endif /* SYMWRITE_H_ */
