// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

#include "common.h"

#include "compactlayoutwriter.h"
#include "zapper.h"
#include "..\zap\zapwriter.h"
#include "..\zap\zapimage.h"
#include "..\zap\wellknowntypes.h"
#include "sigbuilder.h"
#include "winrthelpers.h"
#include "caparser.h"

#define TRITON_STRESS_IMPL
#include "tritonstress.h"
enum LoadFailureEnum
{
    ThrowOnLoadFailure,
    ReturnNullOnLoadFailure,
};


class ByteStreamWriter
{
private:
    BYTE		*m_bufStart;
    BYTE		*m_bufPtr;
    BYTE		*m_bufEnd;
    size_t		 m_bufSize;

    void Grow()
    {
        STANDARD_VM_CONTRACT;

        size_t newBufSize = m_bufSize*2;
        BYTE *newBuffer = new BYTE[newBufSize];
        for (size_t i = 0; i < m_bufSize; i++)
            newBuffer[i] = m_bufStart[i];
        delete[] m_bufStart;
        m_bufPtr = newBuffer + (m_bufPtr - m_bufStart);
        m_bufStart = newBuffer;
        m_bufEnd = newBuffer + newBufSize;
        m_bufSize = newBufSize;
    }

public:
    ByteStreamWriter()
    {
        STANDARD_VM_CONTRACT;

        m_bufSize = 100;
        m_bufStart = new BYTE[m_bufSize];
        m_bufPtr = m_bufStart;
        m_bufEnd = m_bufStart + m_bufSize;
    }

    void Reset()
    {
        LIMITED_METHOD_CONTRACT;
        m_bufPtr = m_bufStart;
    }

    BYTE *GetBuffer(size_t &size)
    {
        LIMITED_METHOD_CONTRACT;
        size = m_bufPtr - m_bufStart;
        return m_bufStart;
    }

    void WriteByte(BYTE b)
    {
        STANDARD_VM_CONTRACT;

        *m_bufPtr++ = b;
        if (m_bufPtr >= m_bufEnd)
            Grow();
    }

    void WriteWord(WORD w)
    {
        STANDARD_VM_CONTRACT;

        WriteByte((BYTE)w);
        WriteByte((BYTE)(w >> 8));
    }

    void WriteDWord(DWORD d)
    {
        STANDARD_VM_CONTRACT;

        WriteWord((WORD)d);
        WriteWord((WORD)(d >> 16));
    }

    void WriteUnsigned(DWORD d)
    {
        STANDARD_VM_CONTRACT;

        if (d < 128)
        {
            WriteByte((BYTE)(d*2 + 0));
        }
        else if (d < 128*128)
        {
            WriteByte((BYTE)(d*4 + 1));
            WriteByte((BYTE)(d >> 6));
        }
        else if (d < 128*128*128)
        {
            WriteByte((BYTE)(d*8 + 3));
            WriteByte((BYTE)(d >> 5));
            WriteByte((BYTE)(d >> 13));
        }
        else if (d < 128*128*128*128)
        {
            WriteByte((BYTE)(d*16 + 7));
            WriteByte((BYTE)(d >> 4));
            WriteByte((BYTE)(d >> 12));
            WriteByte((BYTE)(d >> 20));
        }
        else
        {
            WriteByte(15);
            WriteDWord(d);
        }
    }

    void WriteSigned(INT32 i)
    {
        STANDARD_VM_CONTRACT;

        DWORD d = (DWORD)i;
        if (d + 64 < 128)
        {
            WriteByte((BYTE)(d*2 + 0));
        }
        else if (d + 64*128 < 128*128)
        {
            WriteByte((BYTE)(d*4 + 1));
            WriteByte((BYTE)(d >> 6));
        }
        else if (d + 64*128*128 < 128*128*128)
        {
            WriteByte((BYTE)(d*8 + 3));
            WriteByte((BYTE)(d >> 5));
            WriteByte((BYTE)(d >> 13));
        }
        else if (d + 64*128*128*128 < 128*128*128*128)
        {
            WriteByte((BYTE)(d*16 + 7));
            WriteByte((BYTE)(d >> 4));
            WriteByte((BYTE)(d >> 12));
            WriteByte((BYTE)(d >> 20));
        }
        else
        {
            WriteByte(15);
            WriteDWord(d);
        }
    }
};

enum	CompactLayoutToken
{
    CLT_INVALID,

    CLT_START_TYPE,
    CLT_SMALL_START_TYPE,
    CLT_SIMPLE_START_TYPE,
    CLT_MODEST_START_TYPE,

    CLT_END_TYPE,
    
    CLT_IMPLEMENT_INTERFACE,
    
    CLT_ADVANCE_ENCLOSING_TYPEDEF,
    CLT_ADVANCE_METHODDEF,
    CLT_ADVANCE_METHODDEF_SHORT_MINUS_8,
    CLT_ADVANCE_METHODDEF_SHORT_0 = CLT_ADVANCE_METHODDEF_SHORT_MINUS_8 + 8,
    CLT_ADVANCE_METHODDEF_SHORT_PLUS_8 = CLT_ADVANCE_METHODDEF_SHORT_0 + 8,

    CLT_ADVANCE_FIELDDEF,
    CLT_ADVANCE_FIELDDEF_SHORT_MINUS_8,
    CLT_ADVANCE_FIELDDEF_SHORT_0 = CLT_ADVANCE_FIELDDEF_SHORT_MINUS_8 + 8,
    CLT_ADVANCE_FIELDDEF_SHORT_PLUS_8 = CLT_ADVANCE_FIELDDEF_SHORT_0 + 8,

    CLT_FIELD_OFFSET,

    CLT_IMPLEMENT_INTERFACE_METHOD,

    CLT_METHOD,
    CLT_NORMAL_METHOD,
    CLT_SIMPLE_METHOD,

    CLT_PINVOKE_METHOD = CLT_SIMPLE_METHOD + 32,
    CLT_METHOD_IMPL,

    CLT_FIELD_INSTANCE,
    CLT_FIELD_STATIC,
    CLT_FIELD_THREADLOCAL,
    CLT_FIELD_CONTEXTLOCAL,
    CLT_FIELD_RVA,

    CLT_FIELD_SIMPLE,

    CLT_FIELD_MAX = CLT_FIELD_SIMPLE + 16,

    CLT_DLLEXPORT_METHOD = CLT_FIELD_MAX,
    CLT_RUNTIME_IMPORT_METHOD,
    CLT_RUNTIME_EXPORT_METHOD,

    CLT_GENERIC_TYPE_1,     // prefix before CLT_START_TYPE : this is a generic type with 1 type arg
    CLT_GENERIC_TYPE_2,     // prefix before CLT_START_TYPE : this is a generic type with 2 type args
    CLT_GENERIC_TYPE_N,     // prefix before CLT_START_TYPE : this is a generic type with N type args (byte follows)

    CLT_PACK,               // unsigned follows specifying the maximum field alignment
    CLT_SIZE,               // unsigned follows specifying the struct size

    CLT_GENERIC_PARAM,      // unsigned follows specifying generic param rid, i.e. token = mdtGenericParam + rid

    CLT_NATIVE_FIELD,       // unsigned follows specifying native type along with flags specifying what other 
                            // information follows (see NFI_*flags):
                            // - size
                            // - flags
                            // - type token 1
                            // - type token 2

    CLT_GUIDINFO,           // guid info for interfaces - guid itself followed by flags

    CLT_STUB_METHOD,        // IL stub method

    CLT_TYPE_FLAGS,         // additional type information (if necessary)
    
    CLT_SPECIAL_TYPE,       // unsigned follows describing the specific type. This CTL code must not exist in versionable mdil. (See SPECIAL_TYPE enum)
                            // This is used to encode information to the binder that would be inappropriate to put into MDIL directly and is runtime specific.

    CLT_LAST,
};

struct InlineContext
{
    static const int MAX_TYPE_ARGS = 64*1024;
    struct TypeArg
    {
        PCCOR_SIGNATURE pSig;
        size_t          cbSig;
    };
    Module  *m_currentModule;
    Module  *m_inlineeModule;
    ULONG classTypeArgCount;
    TypeArg *classTypeArgs;
    ULONG methodTypeArgCount;
    TypeArg *methodTypeArgs;
    
    InlineContext(Module *currentModule)
    {
        LIMITED_METHOD_CONTRACT;

        m_currentModule = currentModule;
        m_inlineeModule = currentModule;
        classTypeArgCount = 0;
        classTypeArgs = NULL;
        methodTypeArgCount = 0;
        methodTypeArgs = NULL;
    }

    InlineContext(const InlineContext &inlineContext)
    {
        LIMITED_METHOD_CONTRACT;

        memcpy(this, &inlineContext, sizeof(InlineContext));
    }

    bool IsTrivial()
    {
        LIMITED_METHOD_CONTRACT;

        return m_currentModule == m_inlineeModule && classTypeArgCount == 0 && methodTypeArgCount == 0;
    }

    Module *GetModule()
    {
        LIMITED_METHOD_CONTRACT;

        return m_inlineeModule;
    }
};

class TokenToSig
{
    struct Entry
    {
        mdToken         m_token;
        PCCOR_SIGNATURE m_pSig;
        ULONG           m_cbSig;
    };
    Entry     * m_entries;
    ULONG       m_capacity;
    ULONG       m_count;

    void Grow()
    {
        STANDARD_VM_CONTRACT;

        m_capacity = m_capacity > 0 ? m_capacity*2 : 10;
        Entry *newEntries = new Entry[m_capacity];
        memcpy(newEntries, m_entries, sizeof(Entry)*m_count);
        delete m_entries;
        m_entries = newEntries;
    }

    PCCOR_SIGNATURE Find(mdToken token, ULONG &cbSig)
    {
        LIMITED_METHOD_CONTRACT;

        // simple linear search for now - replace with hash if appropriate
        for (ULONG i = 0; i < m_count; i++)
        {
            if (m_entries[i].m_token == token)
            {
                cbSig = m_entries[i].m_cbSig;
                return m_entries[i].m_pSig;
            }
        }
        return NULL;
    }

public:
    TokenToSig()
    {
        STANDARD_VM_CONTRACT;

        m_count = 0;
        m_capacity = 10;
        m_entries = new Entry[m_capacity];
    }

    PCCOR_SIGNATURE Get(mdToken token, ULONG &cbSig)
    {
        LIMITED_METHOD_CONTRACT;

        PCCOR_SIGNATURE pSig = Find(token, cbSig);
        assert(pSig != NULL);
        return pSig;
    }

    void Set(mdToken token, PCCOR_SIGNATURE pSig, ULONG &cbSig)
    {
        STANDARD_VM_CONTRACT;

        ULONG oldCbSig;
        PCCOR_SIGNATURE oldPSig = Find(token, oldCbSig);
        if (oldPSig != NULL)
        {
            // we can't quite assert that because there may be modifiers
            // in the signatures that are irrelevant (and ignored) for the time being
            // assert(cbSig == oldCbSig && memcmp(pSig, oldPSig, cbSig) == 0);
            return;
        }
        if (m_count >= m_capacity)
        {
            Grow();
        }
        COR_SIGNATURE *newSig = new COR_SIGNATURE[cbSig];
        memcpy(newSig, pSig, cbSig);
        Entry *entry = &m_entries[m_count++];
        entry->m_token = token;
        entry->m_pSig = newSig;
        entry->m_cbSig = cbSig;
    }
};

class CompactLayoutWriter : public ICompactLayoutWriter
{
private:
    // Reset to an empty stream at Reset. EndType will store the contents of the stream.
    ByteStreamWriter   *m_stream;

    // Reset to new values at each Reset, StartType will fill in with meaningful values
    DWORD               m_typeDefToken;
    DWORD               m_prevFieldDefToken;
    DWORD               m_prevMethodDefToken;
    DWORD               m_typeFlags;
    
    // Accumulates over the lifetime of the process and is used once.
    ByteStreamWriter   *m_stubAssocStream;
    DWORD               m_prevStubDefToken;
    DWORD               m_stubMethodCount;
#ifdef _DEBUG
    bool                m_generatingStubs;
#endif // _DEBUG

    // Accumulates of the lifetime of the process and is safe for use even if the type that initially added data fails to be generated.
    TokenToSig          m_tokenToSig;

    // Constant, and never modified
    Module * const      m_pModule;
    ZapImage * const    m_pZapImage; 

    void AdvanceEnclosingTypeDef(unsigned enclosingTypeToken)
    {
        STANDARD_VM_CONTRACT;

        if (enclosingTypeToken != 0)
        {
            assert(TypeFromToken(enclosingTypeToken) == mdtTypeDef);
            m_stream->WriteByte(CLT_ADVANCE_ENCLOSING_TYPEDEF);
            m_stream->WriteSigned(RidFromToken(enclosingTypeToken));
        }
    }

    void AdvanceMethodDef(unsigned methodToken)
    {
        STANDARD_VM_CONTRACT;

        int tokenDiff = methodToken - m_prevMethodDefToken - 1;
        if (tokenDiff != 0)
        {
            if (-8 <= tokenDiff && tokenDiff <= 8)
                m_stream->WriteByte(BYTE(CLT_ADVANCE_METHODDEF_SHORT_0 + tokenDiff));
            else
            {
                m_stream->WriteByte(CLT_ADVANCE_METHODDEF);
                m_stream->WriteSigned(tokenDiff);
            }
        }
        m_prevMethodDefToken = methodToken;
    }

    void AdvanceFieldDef(unsigned fieldToken)
    {
        STANDARD_VM_CONTRACT;

        assert(TypeFromToken(fieldToken) == mdtFieldDef);
        int tokenDiff = fieldToken - m_prevFieldDefToken - 1;
        if (tokenDiff != 0)
        {
            if (-8 <= tokenDiff && tokenDiff <= 8)
                m_stream->WriteByte(BYTE(CLT_ADVANCE_FIELDDEF_SHORT_0 + tokenDiff));
            else
            {
                m_stream->WriteByte(CLT_ADVANCE_FIELDDEF);
                m_stream->WriteSigned(tokenDiff);
            }
        }
        m_prevFieldDefToken = fieldToken;
    }

    // Read NeutralResourcesLanguageAttribute and store results
    void EmitNeutralResourceData(IMDInternalImport *pMDImport)
    {
        STANDARD_VM_CONTRACT;

        mdToken token;
        IfFailThrow(pMDImport->GetAssemblyFromScope(&token));

        const BYTE *pVal = NULL;
        ULONG cbVal = 0;

        LPCUTF8 cultureName = NULL;
        ULONG cultureNameLength = 0;
        INT16 fallbackLocation = 0;
        // Check for the existance of the attribute.
        HRESULT hr = pMDImport->GetCustomAttributeByName(token, "System.Resources.NeutralResourcesLanguageAttribute", (const void **)&pVal, &cbVal);
        if (hr == S_OK)
        {
            CustomAttributeParser cap(pVal, cbVal);
            IfFailThrow(cap.SkipProlog());
            IfFailThrow(cap.GetString(&cultureName, &cultureNameLength));
            IfFailThrow(cap.GetI2(&fallbackLocation));
#ifdef FEATURE_LEGACYNETCF
            if (m_pModule->GetDomain()->GetAppDomainCompatMode() == BaseDomain::APPDOMAINCOMPAT_APP_EARLIER_THAN_WP8)
            {
                fallbackLocation = 0; // UltimateResourceFallbackLocation.MainAssembly
            }
#endif
        }

        DWORD cultureNameId = EmitName(cultureName);

        m_pZapImage->SetNeutralResourceInfo(cultureNameLength, cultureNameId, fallbackLocation);
    }

    void WriteTypeDefOrRef(unsigned typeToken, ByteStreamWriter *stream)
    {
        STANDARD_VM_CONTRACT;

        DWORD encoding = RidFromToken(typeToken) << 2;
        switch	(TypeFromToken(typeToken))
        {
        case    0: assert(typeToken == 0);  break;
        case	mdtTypeDef:	 encoding += 1;	break; 
        case	mdtTypeRef:	 encoding += 2;	break; 
        case	mdtTypeSpec: encoding += 3;	break;
        default: assert(0);  encoding = (typeToken << 2) + 3;	break; 
        }
        stream->WriteUnsigned(encoding);
    }

    void WriteMethodDefOrRef(unsigned methodToken, ByteStreamWriter *stream)
    {
        STANDARD_VM_CONTRACT;

        DWORD encoding = RidFromToken(methodToken) << 2;
        switch	(TypeFromToken(methodToken))
        {
        case    0:  assert(methodToken == 0);   break;
        case	mdtMethodDef:  encoding += 1;	break; 
        case	mdtMemberRef:  encoding += 2;	break; 
        case	mdtMethodSpec: encoding += 3;	break;
        default: assert(0);    encoding = (methodToken << 2);	break; 
        }
        stream->WriteUnsigned(encoding);
    }

public:
    CompactLayoutWriter(Module *pModule, ZapImage *pZapImage) :
      m_pModule(pModule),
      m_pZapImage(pZapImage)
    {
        STANDARD_VM_CONTRACT;

        ULONG assembly = 0;
        ULONG locale = 0;
        IMDInternalImport *pMDImport = pModule->GetMDImport();
        LPCSTR pszName = NULL;
        AssemblyMetaDataInternal metaData;
        DWORD flags = 0;
        HRESULT hr;

        m_prevFieldDefToken      = 0;
        m_prevMethodDefToken     = 0;
        
        m_stream = new ByteStreamWriter();
        
        HENUMInternalHolder hEnum(pMDImport);
        hEnum.EnumAllInit(mdtMethodDef);
        m_prevStubDefToken = TokenFromRid(hEnum.EnumGetCount(), mdtMethodDef);

        m_stubAssocStream = new ByteStreamWriter;
        m_stubMethodCount = 0;
        INDEBUG(m_generatingStubs = false);

        // initialize string buffer (with empty string being first)
        if (m_pZapImage->m_namePool.GetCount() == 0){
                m_pZapImage->m_namePool.SetCount(1);
                m_pZapImage->m_namePool[0] = 0;
        }

        hr = pMDImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly),
                                         NULL, NULL, // not yet interested in public key data
                                         NULL,       // not interested in HashAlgID
                                         &pszName,   // 
                                         &metaData,
                                         &flags);
        if (hr == S_OK) {

            // WindowsRuntime assembly names are annotated with the first winrt type in them.
#ifdef FEATURE_COMINTEROP
            if ((flags & afContentType_Mask) == afContentType_WindowsRuntime)
            {
                LPCSTR szNameSpace;
                LPCSTR szTypeName;
                LPCWSTR wszAssemblyPath = pModule->GetAssembly()->GetManifestFile()->GetPath();
                SString ssFakeNameSpaceAllocationBuffer;
                
                IfFailThrow(GetFirstWinRTTypeDef(pMDImport, &szNameSpace, &szTypeName, wszAssemblyPath, &ssFakeNameSpaceAllocationBuffer));

                StackSString sNamespaceAndType(SString::Utf8, pszName);
                sNamespaceAndType.Append(W("!"));
                sNamespaceAndType.AppendUTF8(szNameSpace);
                sNamespaceAndType.Append(W("."));
                sNamespaceAndType.AppendUTF8(szTypeName);

                StackScratchBuffer scratchBufferUtf8;
                assembly = EmitName(sNamespaceAndType.GetUTF8(scratchBufferUtf8));
            }
            else
#endif
            {
                assembly = EmitName(pszName);
            }
            locale = EmitName(metaData.szLocale);
        }

        pZapImage->SetAssemblyNameAndLocale(assembly, locale, &metaData);
        EmitNeutralResourceData(pMDImport);
    }

    // This is used to prepare the CompactLayoutWriter for writing out another type.
    virtual void Reset()
    {
        LIMITED_METHOD_CONTRACT;

        m_stream->Reset();
        m_typeDefToken = 0;
        m_prevFieldDefToken      = 0x04000000;
        m_prevMethodDefToken     = 0x06000000;
        m_typeFlags = 0;
    }

    // This is a prefix for generic types
    virtual
    void GenericType(DWORD typeArgCount)
    {
        STANDARD_VM_CONTRACT;

        if (typeArgCount == 1)
            m_stream->WriteByte(CLT_GENERIC_TYPE_1);
        else if (typeArgCount == 2)
            m_stream->WriteByte(CLT_GENERIC_TYPE_2);
        else
        {
            m_stream->WriteByte(CLT_GENERIC_TYPE_N);
            m_stream->WriteUnsigned(typeArgCount);
        }
    }

    // This starts serialization/deserialization of new type
    virtual
    void StartType( DWORD  flags,							// CorTypeAttr plus perhaps other flags
                    DWORD  typeDefToken,					// typedef token for this type
                    DWORD  baseTypeToken,					// type this type is derived from, if any
                    DWORD  enclosingTypeToken,				// type this type is nested in, if any
                    DWORD  interfaceCount,					// how many times ImplementInterface() will be called
                    DWORD  fieldCount,						// how many times Field() will be called
                    DWORD  methodCount,						// how many times Method() will be called
                    DWORD  newVirtualMethodCount,			// how many new virtuals this type defines
                    DWORD  overrideVirtualMethodCount )     // how many virtuals this type overrides
    {
        STANDARD_VM_CONTRACT;

        // we write out all types before we start generating stubs
        assert(!m_generatingStubs);

        m_typeDefToken = typeDefToken;

        AdvanceEnclosingTypeDef(enclosingTypeToken);

        if (interfaceCount == 0 && newVirtualMethodCount == 0 && overrideVirtualMethodCount == 0)
        {
            if (fieldCount <= 7)
            {
                m_stream->WriteByte(CLT_SMALL_START_TYPE);
                m_stream->WriteUnsigned(flags);
                WriteTypeDefOrRef(baseTypeToken, m_stream);
                m_stream->WriteUnsigned(fieldCount + methodCount*8);
            }
            else
            {
                m_stream->WriteByte(CLT_SIMPLE_START_TYPE);
                m_stream->WriteUnsigned(flags);
                WriteTypeDefOrRef(baseTypeToken, m_stream);
                m_stream->WriteUnsigned(fieldCount);
                m_stream->WriteUnsigned(methodCount);
            }
        }
        else if (interfaceCount <= 3 && newVirtualMethodCount <= 3)
        {
            m_stream->WriteByte(CLT_MODEST_START_TYPE);
            m_stream->WriteUnsigned(flags);
            WriteTypeDefOrRef(baseTypeToken, m_stream);
            m_stream->WriteUnsigned(fieldCount);
            m_stream->WriteUnsigned(methodCount);
            m_stream->WriteUnsigned(interfaceCount + newVirtualMethodCount*4 + overrideVirtualMethodCount*16);
        }
        else
        {
            m_stream->WriteByte(CLT_START_TYPE);
            m_stream->WriteUnsigned(flags);
            WriteTypeDefOrRef(baseTypeToken, m_stream);
            m_stream->WriteUnsigned(interfaceCount);
            m_stream->WriteUnsigned(fieldCount);
            m_stream->WriteUnsigned(methodCount);
            m_stream->WriteUnsigned(newVirtualMethodCount);
            m_stream->WriteUnsigned(overrideVirtualMethodCount);
        }

        m_typeFlags = flags;
        m_prevFieldDefToken      = 0x04000000;
        m_prevMethodDefToken     = 0x06000000;
    }

    // Call once for each interface implemented by the
    // class directly (not those implemented in base classes)
    virtual
    void ImplementInterface( DWORD interfaceTypeToken )
    {
        STANDARD_VM_CONTRACT;

        m_stream->WriteByte(CLT_IMPLEMENT_INTERFACE);
        WriteTypeDefOrRef(interfaceTypeToken, m_stream);
    }

    virtual
    void ExtendedTypeFlags( DWORD flags )
    {
        STANDARD_VM_CONTRACT;

        m_stream->WriteByte(CLT_TYPE_FLAGS);
        m_stream->WriteUnsigned(flags);
    }

    virtual
    void SpecialType( SPECIAL_TYPE type) 
    {
        STANDARD_VM_CONTRACT;

        m_stream->WriteByte(CLT_SPECIAL_TYPE);
        m_stream->WriteUnsigned((DWORD)type);
    }

    // Call once for each field the class declares directly
    // valueTypeToken is non-0 
    // iff fieldType == ELEMENT_TYPE_VALUETYPE
    // not all CorElementTypes may be allowed - TBD
    virtual
    void Field( DWORD           fieldToken,		// an mdFieldDef
                FieldStorage    fieldStorage,
                FieldProtection fieldProtection,
                CorElementType  fieldType,
                DWORD			fieldOffset,
                DWORD           valueTypeToken)
    {
        STANDARD_VM_CONTRACT;

        AdvanceFieldDef(fieldToken);

        // We should have an explicit field offset iff the containing type has explicit layout or we have an RVA field
        assert((fieldOffset != ~0) == (IsTdExplicitLayout(m_typeFlags) != 0 || fieldStorage == FS_RVA));
        if (fieldOffset != ~0)
        {
            m_stream->WriteByte(CLT_FIELD_OFFSET);
            m_stream->WriteUnsigned(fieldOffset);
        }

//        assert((fieldType == ELEMENT_TYPE_VALUETYPE) == (valueTypeToken != 0));
//        disable for now -fires for some generic stuff (?)
        if ((fieldType == ELEMENT_TYPE_VALUETYPE) != (valueTypeToken != 0))
        {
//            printf("fieldType = %d  valueTypeToken = %08x\n", fieldType, valueTypeToken);
        }
        assert((unsigned)fieldStorage < 8);

        assert(CLT_FIELD_INSTANCE + FS_INSTANCE     == CLT_FIELD_INSTANCE);
        assert(CLT_FIELD_INSTANCE + FS_STATIC       == CLT_FIELD_STATIC);
        assert(CLT_FIELD_INSTANCE + FS_THREADLOCAL  == CLT_FIELD_THREADLOCAL);
        assert(CLT_FIELD_INSTANCE + FS_CONTEXTLOCAL == CLT_FIELD_CONTEXTLOCAL);
        assert(CLT_FIELD_INSTANCE + FS_RVA          == CLT_FIELD_RVA);

        assert(
            fieldType == ELEMENT_TYPE_I1 ||
            fieldType == ELEMENT_TYPE_BOOLEAN ||
            fieldType == ELEMENT_TYPE_U1 ||
            fieldType == ELEMENT_TYPE_I2 ||
            fieldType == ELEMENT_TYPE_U2 ||
            fieldType == ELEMENT_TYPE_CHAR ||
            fieldType == ELEMENT_TYPE_I4 ||
            fieldType == ELEMENT_TYPE_U4 ||
            fieldType == ELEMENT_TYPE_I8 ||
            fieldType == ELEMENT_TYPE_I  ||
            fieldType == ELEMENT_TYPE_U  ||
            fieldType == ELEMENT_TYPE_U8 ||
            fieldType == ELEMENT_TYPE_R4 ||
            fieldType == ELEMENT_TYPE_R8 ||
            fieldType == ELEMENT_TYPE_CLASS ||
            fieldType == ELEMENT_TYPE_VALUETYPE ||
            fieldType == ELEMENT_TYPE_PTR ||
            fieldType == ELEMENT_TYPE_FNPTR
            );

        assert((unsigned)fieldType < 32);
        assert((unsigned)fieldProtection < 8);

        static DWORD encodingTable[16] =
        {
            0x0112, // 9369 (sum = 9369)
            0x1112, // 4106 (sum = 13475)
            0x0608, // 3805 (sum = 17280)
            0x0108, // 3245 (sum = 20525)
            0x0102, // 2110 (sum = 22635)
            0x0312, // 1690 (sum = 24325)
            0x0612, // 1364 (sum = 25689)
            0x1108, // 1234 (sum = 26923)
            0x0308, // 910 (sum = 27833)
            0x1612, // 815 (sum = 28648)
            0x0111, // 762 (sum = 29410)
            0x1312, // 742 (sum = 30152)
            0x0618, // 665 (sum = 30817)
            0x0309, // 627 (sum = 31444)
            0x0609, // 414 (sum = 31858)
            0x0311, // 409 (sum = 32267)
        };

        DWORD encoding = fieldStorage*16*256 + fieldProtection*256 + fieldType;
        DWORD encodingIndex;
        for (encodingIndex = 0; encodingIndex < sizeof(encodingTable)/sizeof(encodingTable[0]); encodingIndex++)
        {
            if (encoding == encodingTable[encodingIndex])
                break;
        }
        if (encodingIndex < sizeof(encodingTable)/sizeof(encodingTable[0]))
        {
            m_stream->WriteByte(BYTE(CLT_FIELD_SIMPLE + encodingIndex));
        }
        else
        {
            m_stream->WriteByte(BYTE(CLT_FIELD_INSTANCE + (unsigned)fieldStorage));
            m_stream->WriteByte(BYTE((unsigned)fieldProtection*32 + (unsigned)fieldType));
        }
        if (fieldType == ELEMENT_TYPE_VALUETYPE)
        {
            WriteTypeDefOrRef(valueTypeToken, m_stream);
        }
    };

    // call once for each method implementing a contract
    // in an interface. Parameters see OverrideMethod
    virtual
    void ImplementInterfaceMethod(DWORD declToken,
                                  DWORD implToken)
    {
        STANDARD_VM_CONTRACT;

        AdvanceMethodDef(implToken);

        m_stream->WriteByte(CLT_IMPLEMENT_INTERFACE_METHOD);
        WriteMethodDefOrRef(declToken, m_stream);
    }

    // call once for each method, including those mentioned in
    // OverrideMethod, NewVirtual, ImplementInterfaceMethod
    virtual
    void Method(DWORD methodAttrs,
                DWORD implFlags,
                DWORD implHints, // have to figure how exactly we do this so it's not so tied to the CLR implementation
                DWORD methodToken,
                DWORD overriddenMethodToken)
    {
        STANDARD_VM_CONTRACT;

        AdvanceMethodDef(methodToken);

        DWORD encodingIndex = 0xffff;

        if (methodAttrs < 0x10000 && implFlags < 0x100 && implHints < 0x100)
        {
            // common method attribute values - implHints in upper 8 bits,
            // implFlags in following 8 bits, methodAttrs in bottom 16 bits
            static DWORD encodingTable[32] =
            {
                0x00000886, // 0886, 00, 00: 17837 (sum = 17837)
                0x00000081, // 0081, 00, 00: 9726 (sum = 27563)
                0x060005c6, // 05c6, 00, 06: 7042 (sum = 34605)
                0x00000086, // 0086, 00, 00: 6841 (sum = 41446)
                0x000000c6, // 00c6, 00, 00: 5922 (sum = 47368)
                0x00000083, // 0083, 00, 00: 5217 (sum = 52585)
                0x000008c6, // 08c6, 00, 00: 4369 (sum = 56954)
                0x10001886, // 1886, 00, 10: 4129 (sum = 61083)
                0x00000883, // 0883, 00, 00: 3662 (sum = 64745)
                0x00000096, // 0096, 00, 00: 3577 (sum = 68322)
                0x000000c4, // 00c4, 00, 00: 3042 (sum = 71364)
                0x00000896, // 0896, 00, 00: 2860 (sum = 74224)
                0x000001e1, // 01e1, 00, 00: 2586 (sum = 76810)
                0x00000093, // 0093, 00, 00: 2574 (sum = 79384)
                0x00000091, // 0091, 00, 00: 2544 (sum = 81928)
                0x30001886, // 1886, 00, 30: 2449 (sum = 84377)
                0x000001c6, // 01c6, 00, 00: 2179 (sum = 86556)
                0x000001e6, // 01e6, 00, 00: 2028 (sum = 88584)
                0x000001c4, // 01c4, 00, 00: 1835 (sum = 90419)
                0x10001883, // 1883, 00, 10: 1812 (sum = 92231)
                0x000009c6, // 09c6, 00, 00: 1706 (sum = 93937)
                0x40001891, // 1891, 00, 40: 1482 (sum = 95419)
                0x06000dc6, // 0dc6, 00, 06: 1473 (sum = 96892)
                0x030301c6, // 01c6, 03, 03: 1449 (sum = 98341)
                0x02802096, // 2096, 80, 02: 1235 (sum = 99576)
                0x000002c3, // 02c3, 00, 00: 1144 (sum = 100720)
                0x02802093, // 2093, 80, 02: 1090 (sum = 101810)
                0x00000881, // 0881, 00, 00: 926 (sum = 102736)
                0x000009e6, // 09e6, 00, 00: 903 (sum = 103639)
                0x000009e1, // 09e1, 00, 00: 860 (sum = 104499)
                0x00000084, // 0084, 00, 00: 720 (sum = 105219)
                0x068005c6, // 05c6, 80, 06: 653 (sum = 105872)
            };

            DWORD encoding = (implHints<<24) + (implFlags <<16) + methodAttrs;
            for (encodingIndex = 0; encodingIndex < sizeof(encodingTable)/sizeof(encodingTable[0]); encodingIndex++)
            {
                if (encoding == encodingTable[encodingIndex])
                    break;
            }
        }
        if (encodingIndex < 32)
        {
            m_stream->WriteByte(BYTE(CLT_SIMPLE_METHOD + encodingIndex));
        }
        else if (implFlags == 0 && implHints == 0)
        {
            m_stream->WriteByte(CLT_NORMAL_METHOD);

            methodAttrs ^= mdHideBySig;  // this being the default for C#, it's on for most of the base libs

            m_stream->WriteUnsigned(methodAttrs);
        }
        else
        {
            m_stream->WriteByte(CLT_METHOD);

            methodAttrs ^= mdHideBySig;  // this being the default for C#, it's on for most of the base libs

            m_stream->WriteUnsigned(methodAttrs);
            m_stream->WriteUnsigned(implFlags);
            m_stream->WriteUnsigned(implHints);
        }
        // if the method is not virtual, or it's newslot, it can't override anything
        assert((IsMdVirtual(methodAttrs) && !IsMdNewSlot(methodAttrs)) || overriddenMethodToken == 0);
        if (IsMdVirtual(methodAttrs) && !IsMdNewSlot(methodAttrs))
            WriteMethodDefOrRef(overriddenMethodToken, m_stream);
    }

    // call once for each PInvoke method
    virtual
    void PInvokeMethod( DWORD methodAttrs,
                        DWORD implFlags,
                        DWORD implHints, // have to figure how exactly we do this so it's not so tied to the CLR implementation
                        DWORD methodToken,
                        LPCSTR moduleName,
                        LPCSTR entryPointName,
                        WORD wLinkFlags)
    {
        STANDARD_VM_CONTRACT;

        AdvanceMethodDef(methodToken);

        m_stream->WriteByte(CLT_PINVOKE_METHOD);

        methodAttrs ^= mdHideBySig;  // this being the default for C#, it's on for most of the base libs

        m_stream->WriteUnsigned(methodAttrs);
        m_stream->WriteUnsigned(implFlags);

        DWORD entryPointNameIndexOrOrdinal;
        if (entryPointName != NULL && entryPointName[0] == '#')
        {
            // this is import-by-ordinal
            char *endPtr;
            entryPointNameIndexOrOrdinal = strtoul(&entryPointName[1], &endPtr, 10);
            assert(*endPtr == '\0');
            implHints |= IH_BY_ORDINAL;
        }
        else
        {
            // this is import-by-name
            entryPointNameIndexOrOrdinal = EmitName(entryPointName);
        }
        m_stream->WriteUnsigned(implHints);

        // the method should not be virtual
        assert(!IsMdVirtual(methodAttrs));

        m_stream->WriteUnsigned(EmitName(moduleName));

        m_stream->WriteUnsigned(entryPointNameIndexOrOrdinal);

        m_stream->WriteUnsigned(wLinkFlags); // calling convention, Ansi/Unicode, ...
    }

        // call once for each DllExport method (Redhawk only feature, at least for now)
    virtual
    void DllExportMethod( DWORD methodAttrs,
                          DWORD implFlags,
                          DWORD implHints, // have to figure how exactly we do this so it's not so tied to the CLR implementation
                          DWORD methodToken,
                          LPCSTR entryPointName,
                          DWORD callingConvention)
    {
        STANDARD_VM_CONTRACT;

        AdvanceMethodDef(methodToken);

        m_stream->WriteByte(CLT_DLLEXPORT_METHOD);

        methodAttrs ^= mdHideBySig;  // this being the default for C#, it's on for most of the base libs

        m_stream->WriteUnsigned(methodAttrs);
        m_stream->WriteUnsigned(implFlags);

        DWORD entryPointNameIndexOrOrdinal;
        if (entryPointName[0] == '#')
        {
            // this is export-by-ordinal
            char *endPtr;
            entryPointNameIndexOrOrdinal = strtoul(&entryPointName[1], &endPtr, 10);
            assert(*endPtr == '\0');
            implHints |= IH_BY_ORDINAL;
        }
        else
        {
            // this is import-by-name
            entryPointNameIndexOrOrdinal = EmitName(entryPointName);
        }
        m_stream->WriteUnsigned(implHints);

        // the method should not be virtual
        assert(!IsMdVirtual(methodAttrs));
       
        // in fact the method should be static
        assert(IsMdStatic(methodAttrs));

        m_stream->WriteUnsigned(entryPointNameIndexOrOrdinal);
        m_stream->WriteUnsigned(callingConvention);
    }

    virtual
    void StubMethod( DWORD dwMethodFlags,
                     DWORD sigToken,
                     DWORD methodToken)
    {
        STANDARD_VM_CONTRACT;

        assert(m_generatingStubs || m_prevMethodDefToken == 0x06000000);
        INDEBUG(m_generatingStubs = true);

        AdvanceMethodDef(methodToken);

        m_stream->WriteByte(CLT_STUB_METHOD);
        m_stream->WriteUnsigned(dwMethodFlags);

        if (dwMethodFlags & SF_NEEDS_STUB_SIGNATURE)
        {
            assert(TypeFromToken(sigToken) == mdtSignature);
            assert(RidFromToken(sigToken) > 0);

            m_stream->WriteUnsigned(RidFromToken(sigToken));
        }

        m_stubMethodCount++;
    }

    virtual
    void StubAssociation( DWORD ownerToken,
                          DWORD *stubTokens,
                          size_t numStubs)
    {
        STANDARD_VM_CONTRACT;

        // note that we may be generating associations without previously calling StubMethod
        // if the stub method is an ordinary (as opposed to dynamically generated) method
        WriteMethodDefOrRef(ownerToken, m_stubAssocStream);
        m_stubAssocStream->WriteUnsigned((DWORD)numStubs);
        
        for (size_t i = 0; i < numStubs; i++)
        {
            WriteMethodDefOrRef(stubTokens[i], m_stubAssocStream);
        }
    }

    // call once for each method impl
    virtual
    void MethodImpl(DWORD declToken,
                    DWORD implToken )
    {
        STANDARD_VM_CONTRACT;

        AdvanceMethodDef(implToken);

        m_stream->WriteByte(CLT_METHOD_IMPL);
        WriteMethodDefOrRef(declToken, m_stream);
    }

    // set an explicit size for explicit layout structs
    virtual
    void SizeType(DWORD size)
    {
        STANDARD_VM_CONTRACT;

        m_stream->WriteByte(CLT_SIZE);
        m_stream->WriteUnsigned(size);
    }

    // specify the packing size
    virtual
    void PackType(DWORD packingSize)
    {
        STANDARD_VM_CONTRACT;

        m_stream->WriteByte(CLT_PACK);
        m_stream->WriteUnsigned(packingSize);
    }

    // specify a generic parameter to a type or method
    virtual
    void GenericParameter(DWORD genericParamToken, DWORD flags)
    {
        STANDARD_VM_CONTRACT;

        assert(TypeFromToken(genericParamToken) == mdtGenericParam);
        m_stream->WriteByte(CLT_GENERIC_PARAM);
        m_stream->WriteUnsigned(RidFromToken(genericParamToken));
        m_stream->WriteUnsigned(flags);
    }

    // specify a field representation on the native side
    virtual
    void NativeField(DWORD            fieldToken,		// an mdFieldDef
                     DWORD            nativeType,       // really an NStructFieldType
                     DWORD			  nativeOffset,
                     DWORD            count,
                     DWORD            flags,
                     DWORD            typeToken1,
                     DWORD            typeToken2)
    {
        STANDARD_VM_CONTRACT;

        assert(TypeFromToken(fieldToken) == mdtFieldDef);
        AdvanceFieldDef(fieldToken);

        if (nativeOffset != ~0)
        {
            m_stream->WriteByte(CLT_FIELD_OFFSET);
            m_stream->WriteUnsigned(nativeOffset);
        }
        m_stream->WriteByte(CLT_NATIVE_FIELD);

        // we encode the native type together with flags that tell us
        // what other information follows
        enum    NativeInformationFlags
        {
            NFI_TYPEMASK   = 0x3F, // we assume the native type fits into this
            NFI_FIRSTFLAG  = 0x40,
            NFI_COUNT      = NFI_FIRSTFLAG,
            NFI_FLAGS      = NFI_COUNT<<1,
            NFI_TYPETOKEN1 = NFI_FLAGS<<1,
            NFI_TYPETOKEN2 = NFI_TYPETOKEN1<<1,
        };

        assert((nativeType & NFI_TYPEMASK) == nativeType);
        if (typeToken1 != 0)
            nativeType |= NFI_TYPETOKEN1;
        if (count != 0)
            nativeType |= NFI_COUNT;
        if (flags != 0)
            nativeType |= NFI_FLAGS;
        if (typeToken2 != 0)
            nativeType |= NFI_TYPETOKEN2;

        m_stream->WriteUnsigned(nativeType);

        if (nativeType & NFI_COUNT)
            m_stream->WriteUnsigned(count);

        if (nativeType & NFI_FLAGS)
            m_stream->WriteUnsigned(flags);

        if (nativeType & NFI_TYPETOKEN1)
            m_stream->WriteUnsigned(typeToken1);

        if (nativeType & NFI_TYPETOKEN2)
            m_stream->WriteUnsigned(typeToken2);
    }

    // specify guid info for interface types
    virtual
    void GuidInformation(GuidInfo *guidInfo)
    {
        STANDARD_VM_CONTRACT;

        m_stream->WriteByte(CLT_GUIDINFO);

        // write the actual guid data verbatim
        BYTE *guidData = (BYTE *)&guidInfo->m_Guid;
        for (size_t i = 0; i < sizeof(guidInfo->m_Guid); i++)
            m_stream->WriteByte(guidData[i]);

        // write the flag - use WriteUnsigned for future extensibility
        m_stream->WriteUnsigned(guidInfo->m_bGeneratedFromName);
    }

    // end the description of the type
    virtual
    void EndType()
    {
        STANDARD_VM_CONTRACT;
        TritonStress(TritonStress_GenerateCTL, this->m_typeDefToken, 0, TritonStressFlag_MainModule);

        m_stream->WriteByte(CLT_END_TYPE);

        size_t size;
        BYTE *buffer = m_stream->GetBuffer(size);
        
        m_pZapImage->FlushCompactLayoutData(this->m_typeDefToken, buffer, (ULONG)size);
    }

    ULONG FindOrCreateExtModuleID(Module *module)
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pMDImport = module->GetMDImport();
        LPCSTR assemblyName = NULL;
        pMDImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, &assemblyName, NULL, NULL);
        ULONG emittedName;
        if (assemblyName == NULL)
        {
            pMDImport->GetScopeProps(&assemblyName, NULL);
            emittedName = EmitName(assemblyName);
        }
        else
        {
            emittedName = EmitAssemblyName(pMDImport, TokenFromRid(1, mdtAssembly));
        }

        COUNT_T tableSize = m_pZapImage->m_extModRef.GetCount();
        for (COUNT_T i = 1; i < tableSize; i++)
        {
            // Take advantage of the emitted name hash that ensures that there will only be one copy of an emitted name.
            if (m_pZapImage->m_extModRef[i].name == emittedName)
            {
                return i;
            }
        }

        // not found, create a new entry
        m_pZapImage->m_extModRef.SetCount(tableSize + 1);
        m_pZapImage->m_extModRef[tableSize].name = emittedName;
        m_pZapImage->m_extModRef[tableSize].flags = ZapImage::ExtModRef::NO_FLAGS;

        return tableSize;
    }

    ULONG FindOrCreateExtTypeRef(MethodTable *pMT)
    {
        STANDARD_VM_CONTRACT;

        DWORD typeToken = pMT->GetCl();
        ULONG typeOrdinal = RidFromToken(typeToken);
        ULONG moduleID = FindOrCreateExtModuleID(pMT->GetModule());

        COUNT_T tableSize = m_pZapImage->m_extTypeRef.GetCount();
        COUNT_T tableSize2 = m_pZapImage->m_extTypeRefExtend.GetCount();

        for (COUNT_T i = 1; i < tableSize; i++)
        {
            if (typeOrdinal == m_pZapImage->m_extTypeRef[i].ordinal && moduleID == m_pZapImage->m_extTypeRef[i].module)
                return i;
        }

        // not found, create a new entry

        LPCUTF8 pszNamespace;
        LPCUTF8 pszName;
        pszName = pMT->GetFullyQualifiedNameInfo(&pszNamespace);
        ULONG offsName = EmitName(pszName);
        ULONG offsNamespace = EmitName(pszNamespace);
        ULONG resolutionScope = 0;
        mdToken tkEncloser = 0;

        if (SUCCEEDED(pMT->GetModule()->GetMDImport()->GetNestedClassProps(pMT->GetCl(), &tkEncloser)))
        {
            EEClass *eeClass = LoadTypeDef(pMT->GetModule(), tkEncloser);
            assert(eeClass != 0);
            resolutionScope = FindOrCreateExtTypeRef(eeClass->GetMethodTable());

            // Re-acquire tableSize and tableSize2 as the above call to FindOrCreateExtTypeRef may have invalidated 
            // the existing local variables.
            tableSize = m_pZapImage->m_extTypeRef.GetCount();
            tableSize2 = m_pZapImage->m_extTypeRefExtend.GetCount();
        }

        m_pZapImage->m_extTypeRef.SetCount(tableSize + 1);
        m_pZapImage->m_extTypeRef[tableSize].module = moduleID;
        m_pZapImage->m_extTypeRef[tableSize].ordinal = typeOrdinal;

        m_pZapImage->m_extTypeRefExtend.SetCount(tableSize2 + 1);

        m_pZapImage->m_extTypeRefExtend[tableSize2].name = offsName;
        m_pZapImage->m_extTypeRefExtend[tableSize2].name_space = offsNamespace;
        m_pZapImage->m_extTypeRefExtend[tableSize2].resolutionScope = resolutionScope;

        return tableSize;
    }

    mdMemberRef FindOrCreateExtMemberRef(DWORD typeToken, ULONG isField, ULONG memberOrdinal, LPCUTF8 pszName, Module *pModule, mdToken tkDefToken)
    {
        STANDARD_VM_CONTRACT;

        assert(TypeFromToken(typeToken) == mdtTypeRef || TypeFromToken(typeToken) == mdtTypeSpec);
        ULONG typeRid = RidFromToken(typeToken);
        unsigned isTypeSpec = TypeFromToken(typeToken) == mdtTypeSpec;

        COUNT_T tableSize = m_pZapImage->m_extMemberRef.GetCount();
        COUNT_T tableSize2 = m_pZapImage->m_extMemberRefExtend.GetCount();

        for (COUNT_T i = 1; i < tableSize; i++)
        {
            if (isTypeSpec    == m_pZapImage->m_extMemberRef[i].isTypeSpec &&
                typeRid       == m_pZapImage->m_extMemberRef[i].typeRid &&
                isField       == m_pZapImage->m_extMemberRef[i].isField &&
                memberOrdinal == m_pZapImage->m_extMemberRef[i].ordinal)
            {
                return TokenFromRid(i, mdtMemberRef);
            }
        }

        // not found, create a new entry
        ULONG memberRefName = EmitName(pszName);

        InlineContext context(pModule);
        PCCOR_SIGNATURE pSig;
        DWORD           cbSig;
        IMDInternalImport *pMDImport = pModule->GetMDImport();

        switch (TypeFromToken(tkDefToken))
        {
        case    mdtMethodDef:
            IfFailThrow(pMDImport->GetSigOfMethodDef(tkDefToken, &cbSig, &pSig));
            break;

        case    mdtFieldDef:
            IfFailThrow(pMDImport->GetSigOfFieldDef(tkDefToken, &cbSig, &pSig));
            break;
        default:
            assert(!"bad token type");
            return 0;
        }

        SigBuilder sigBuilder;
        EncodeMemberRefSignature(&context, pSig, cbSig, sigBuilder);

        DWORD size; 
        BYTE *newBuffer = (BYTE *)sigBuilder.GetSignature(&size);

        BOOL fCreateNewSig = TRUE;
        ULONG offsOfSig = 0;

        // Check to see if we've already created this signature
        for (COUNT_T i = 1; i < tableSize2; i++)
        {
            COUNT_T offs = m_pZapImage->m_extMemberRefExtend[i].signature;
            BYTE *oldBuffer = &m_pZapImage->m_compactLayoutBuffer[offs];
            if (memcmp(oldBuffer, newBuffer, size) == 0)
            {
                fCreateNewSig = FALSE;
                offsOfSig = offs;
                break;
            }
        }

        m_pZapImage->m_extMemberRef.SetCount(tableSize + 1);
        m_pZapImage->m_extMemberRef[tableSize].isTypeSpec = isTypeSpec;
        m_pZapImage->m_extMemberRef[tableSize].typeRid    = typeRid;
        m_pZapImage->m_extMemberRef[tableSize].isField    = isField;
        m_pZapImage->m_extMemberRef[tableSize].ordinal    = memberOrdinal;

        m_pZapImage->m_extMemberRefExtend.SetCount(tableSize2 + 1);
        m_pZapImage->m_extMemberRefExtend[tableSize2].name = memberRefName;
        m_pZapImage->m_extMemberRefExtend[tableSize2].signature = offsOfSig;

        mdToken extMemberRef2Token = TokenFromRid(tableSize2, mdtMemberRef);
        if (fCreateNewSig)
        {
             m_pZapImage->FlushCompactLayoutData(extMemberRef2Token, newBuffer, size);
        }

        return TokenFromRid(tableSize, mdtMemberRef);
    }

    mdMethodSpec FindOrCreateMethodSpec(ByteStreamWriter &stream)
    {
        STANDARD_VM_CONTRACT;

        size_t size;
        BYTE *newBuffer = stream.GetBuffer(size);
        COUNT_T methodSpecCount = m_pZapImage->m_methodSpecToOffs.GetCount();
        for (COUNT_T i = 1; i < methodSpecCount; i++)
        {
            COUNT_T offs = m_pZapImage->m_methodSpecToOffs[i];
            BYTE *oldBuffer = &m_pZapImage->m_compactLayoutBuffer[offs];
            if (memcmp(oldBuffer, newBuffer, size) == 0)
                return TokenFromRid(mdtMethodSpec, i);
        }
        // not found, hence create
        m_pZapImage->m_methodSpecToOffs.SetCount(methodSpecCount+1);
        mdMethodSpec resultToken = TokenFromRid(methodSpecCount, mdtMethodSpec);
        m_pZapImage->FlushCompactLayoutData(resultToken, newBuffer, (COUNT_T)size);
        return resultToken;
    }

    mdSignature FindOrCreateSignature(ByteStreamWriter &stream)
    {
        STANDARD_VM_CONTRACT;

        size_t size;
        BYTE *newBuffer = stream.GetBuffer(size);
        COUNT_T signatureCount = m_pZapImage->m_signatureToOffs.GetCount();
        for (COUNT_T i = 1; i < signatureCount; i++)
        {
            COUNT_T offs = m_pZapImage->m_signatureToOffs[i];
            BYTE *oldBuffer = &m_pZapImage->m_compactLayoutBuffer[offs];
            if (memcmp(oldBuffer, newBuffer, size) == 0)
                return TokenFromRid(mdtSignature, i);
        }
        // not found, hence create
        m_pZapImage->m_signatureToOffs.SetCount(signatureCount+1);
        mdSignature resultToken = TokenFromRid(signatureCount, mdtSignature);
        m_pZapImage->FlushCompactLayoutData(resultToken, newBuffer, (COUNT_T)size);
        return resultToken;
    }

    void EncodeMethodSpec(mdToken parentToken, PCCOR_SIGNATURE pSig, DWORD cbSig, ByteStreamWriter *stream)
    {
        STANDARD_VM_CONTRACT;

        SigPointer sig(pSig, cbSig);
        WriteMethodDefOrRef(parentToken, stream);
        BYTE b;
        sig.GetByte(&b);
        assert(b == IMAGE_CEE_CS_CALLCONV_GENERICINST);
        ULONG typeArgCount;
        sig.GetData(&typeArgCount);
        stream->WriteUnsigned(typeArgCount);
        for (ULONG typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
            EncodeType(&sig, stream);
    }

    void EncodeMemberRefSignature(InlineContext *context, PCCOR_SIGNATURE pSig, DWORD cbSig, SigBuilder &sigBuilder)
    {
        STANDARD_VM_CONTRACT;

        SigPointer sp(pSig, cbSig);
        
        BYTE b;
        IfFailThrow(sp.GetByte(&b));
        sigBuilder.AppendByte(b);

        ULONG paramCount;

        if (b == IMAGE_CEE_CS_CALLCONV_FIELD)
        {
            // FieldSigs are encoded like methodRefSigs with 0 parameters
            paramCount = 0;
        }
        else
        {
            if ((b & IMAGE_CEE_CS_CALLCONV_GENERIC) != 0)
            {
                ULONG genericArgCount;

                // Copy Generic Argument Count
                IfFailThrow(sp.GetData(&genericArgCount));
                sigBuilder.AppendData(genericArgCount);
            }

            // Copy Parameter count
            IfFailThrow(sp.GetData(&paramCount));
            sigBuilder.AppendData(paramCount);
        }

        // Copy Parameters and return value across (return value is param 0)
        for (ULONG paramIndex = 0; paramIndex <= paramCount; paramIndex++)
        {
            IfFailThrow(sp.PeekByte(&b));
            if (b == ELEMENT_TYPE_SENTINEL)
            {
                IfFailThrow(sp.GetByte(&b));
                sigBuilder.AppendByte(b);
            }

            expandSignature(sp, context, sigBuilder);
        }

        return;
    }

    mdMethodSpec FindOrCreateMethodSpec(mdToken parentToken, SigBuilder &sb)
    {
        STANDARD_VM_CONTRACT;

        // recode this as a CTL sig
        DWORD cbSig;
        PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sb.GetSignature(&cbSig);
        ByteStreamWriter stream;
        EncodeMethodSpec(parentToken, pSig, cbSig, &stream);
        // look it up in the CTL tokens we have already generated
        mdToken methodSpecToken = FindOrCreateMethodSpec(stream);
        // associate the methodSpecToken with the IL sig
        m_tokenToSig.Set(methodSpecToken, pSig, cbSig);
        return methodSpecToken;
    }

    void EncodeSignature(PCCOR_SIGNATURE pSig, DWORD cbSig, ByteStreamWriter *stream)
    {
        STANDARD_VM_CONTRACT;

        SigPointer sig(pSig, cbSig);
        BYTE b;
        sig.GetByte(&b);
        stream->WriteByte(b);
        ULONG argCount;
        sig.GetData(&argCount);
        stream->WriteUnsigned(argCount);
        for (ULONG argIndex = 0; argIndex <= argCount; argIndex++)
            EncodeType(&sig, stream);
    }

    mdSignature FindOrCreateSignature(SigBuilder &sb)
    {
        STANDARD_VM_CONTRACT;

        // recode this as a CTL sig
        DWORD cbSig;
        PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sb.GetSignature(&cbSig);
        ByteStreamWriter stream;
        EncodeSignature(pSig, cbSig, &stream);
        // look it up in the CTL tokens we have already generated
        mdToken signatureToken = FindOrCreateSignature(stream);
        // associate the signatureToken with the IL sig
        m_tokenToSig.Set(signatureToken, pSig, cbSig);
        return TokenFromRid(RidFromToken(signatureToken), mdtSignature);
    }

    DWORD GetToken(MethodTable *pMT)
    {
        STANDARD_VM_CONTRACT;

        DWORD typeToken = pMT->GetCl();
        if (pMT->GetModule() != m_pModule)
        {
            ULONG extTypeID = FindOrCreateExtTypeRef(pMT);
            typeToken = TokenFromRid(extTypeID, mdtTypeRef);
        }
        return typeToken;
    }

    void Encode(TypeHandle th, ByteStreamWriter *stream)
    {
        STANDARD_VM_CONTRACT;

        if (th.IsGenericVariable())
        {
            TypeVarTypeDesc *tvtd = th.AsGenericVariable();
            stream->WriteByte(BYTE(tvtd->GetInternalCorElementType()));
            stream->WriteUnsigned(tvtd->GetIndex());
        }
        else if (!th.IsTypeDesc())
        {
            Encode(th.AsMethodTable(), stream);
        }
        else if (th.IsArray())
        {
            ArrayTypeDesc *atd = th.AsArray();
            CorElementType elType = atd->GetInternalCorElementType();
            stream->WriteByte((BYTE)elType);
            Encode(atd->GetArrayElementTypeHandle(), stream);
            if (elType == ELEMENT_TYPE_ARRAY)
            {
                stream->WriteUnsigned(atd->GetRank());
                stream->WriteUnsigned(0);
                stream->WriteUnsigned(0);
            }
        }
        else
        {
            assert(!"NYI");
        }
    }

    void Encode(MethodTable *pMT, ByteStreamWriter *stream)
    {
        STANDARD_VM_CONTRACT;

        CorElementType elType = pMT->GetSignatureCorElementType();
        if (pMT->HasInstantiation())
        {
            stream->WriteByte(ELEMENT_TYPE_GENERICINST);
            elType = pMT->IsValueType() ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS;
            stream->WriteByte(BYTE(elType));
            WriteTypeDefOrRef(GetToken(pMT), stream);
            stream->WriteUnsigned(pMT->GetNumGenericArgs());
            Instantiation instantiation = pMT->GetInstantiation();
            for (DWORD i = 0; i < instantiation.GetNumArgs(); i++)
            {
                Encode(instantiation[i], stream);
            }
        }
        else
        {
            stream->WriteByte(BYTE(elType));
            if (elType == ELEMENT_TYPE_CLASS || elType == ELEMENT_TYPE_VALUETYPE)
                WriteTypeDefOrRef(GetToken(pMT), stream);
            else
                assert(elType <= ELEMENT_TYPE_STRING || elType == ELEMENT_TYPE_I || elType == ELEMENT_TYPE_U);
        }
    }

    mdTypeSpec GetTypeSpecToken(ByteStreamWriter *stream)
    {
        STANDARD_VM_CONTRACT;

        size_t size;
        BYTE *buffer = stream->GetBuffer(size);
        for (COUNT_T i = 1; i < m_pZapImage->m_typeSpecToOffs.GetCount(); i++)
        {
            COUNT_T startOffs = m_pZapImage->m_typeSpecToOffs[i];
            if (memcmp(&m_pZapImage->m_compactLayoutBuffer[startOffs], buffer, size) == 0)
                return TokenFromRid(i, mdtTypeSpec);
        }
        // we have not found it - let's add it to the CTL type spec table

        // we need to give it a new token and expand the table
        DWORD typeSpecToken = TokenFromRid(m_pZapImage->m_typeSpecToOffs.GetCount(), mdtTypeSpec);

        m_pZapImage->m_typeSpecToOffs.SetCount(m_pZapImage->m_typeSpecToOffs.GetCount()+1);
        m_pZapImage->FlushCompactLayoutData(typeSpecToken, buffer, (ULONG)size);

        return typeSpecToken;
    }

    // translate a method def in another module or a generic type
    // a member ref in our own module (which we may have to create)
    virtual
    mdMemberRef GetTokenForMethodDesc(MethodDesc *methodDesc, MethodTable *pMT)
    {
        STANDARD_VM_CONTRACT;

        // check for the trivial case - non-generic method in the same module
        if (methodDesc->GetModule() == m_pModule && !methodDesc->HasClassOrMethodInstantiation())
            return methodDesc->GetMemberDef();

        Module *module = methodDesc->GetModule();
        if (pMT == NULL)
            pMT = methodDesc->GetMethodTable();

        // translate an external typedef into a typeref
        mdToken typeToken = GetToken(pMT);
        
        if (pMT->HasInstantiation())
        {
            ByteStreamWriter stream;

            Encode(pMT, &stream);

            typeToken = GetTypeSpecToken(&stream);
        }

        if (methodDesc->HasMethodInstantiation())
        {
            _ASSERTE(methodDesc->IsGenericMethodDefinition());
            if (TypeFromToken(typeToken) == mdtTypeDef)
                return methodDesc->GetMemberDef();
        }

        HENUMInternalHolder hEnumMethodDef(module->GetMDImport());
        hEnumMethodDef.EnumInit(mdtMethodDef, pMT->GetCl());
        ULONG methodCount = hEnumMethodDef.EnumGetCount();
        mdMethodDef firstMethodDefToken;
        hEnumMethodDef.EnumNext(&firstMethodDefToken);
        mdMethodDef methodDefToken = methodDesc->GetMemberDef();
        ULONG methodOrdinal = methodDefToken - firstMethodDefToken;
        assert(methodOrdinal < methodCount);

        return FindOrCreateExtMemberRef(typeToken, FALSE, methodOrdinal, methodDesc->GetNameOnNonArrayClass(), methodDesc->GetModule(), methodDefToken);
    }

    // we get passed a generic instantatiation - find or create a type spec
    // token for it
    virtual 
    mdTypeSpec GetTypeSpecToken(PCCOR_SIGNATURE pGenericInstSig, DWORD cbGenericInstSig)
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pMDImport = m_pModule->GetMDImport();

        // search linearly through the existing type specs to see if we have a match
        HENUMInternalHolder hEnumTypeSpec(pMDImport);
        hEnumTypeSpec.EnumAllInit(mdtTypeSpec);
        ULONG typeSpecCount = hEnumTypeSpec.EnumGetCount();
        mdTypeSpec typeSpecToken;
        while (hEnumTypeSpec.EnumNext(&typeSpecToken))
        {
            PCCOR_SIGNATURE pSig;
            ULONG cbSig;
            pMDImport->GetTypeSpecFromToken(typeSpecToken, &pSig, &cbSig);
            
            // does this type spec match the generic instantiation?
            if (cbGenericInstSig == cbSig && memcmp(pGenericInstSig, pSig, cbSig) == 0)
                return typeSpecToken;
        }
        
        ByteStreamWriter stream;

        // we need to encode it
        SigPointer sig(pGenericInstSig, cbGenericInstSig);
        EncodeType(&sig, &stream);

        typeSpecToken = GetTypeSpecToken(&stream);

        m_tokenToSig.Set(typeSpecToken, pGenericInstSig, cbGenericInstSig);

        return typeSpecToken;
    }

    virtual
    mdToken GetTokenForType(MethodTable *pMT)
    {
        STANDARD_VM_CONTRACT;

        if (pMT == NULL)
            return 0;

        mdToken typeToken = GetToken(pMT);
        
        if (pMT->HasInstantiation())
        {
            ByteStreamWriter stream;

            Encode(pMT, &stream);

            typeToken = GetTypeSpecToken(&stream);
        }
        return typeToken;
    }

    void GetSignatureForType(TypeHandle th, SigBuilder *pSig)
    {
        STANDARD_VM_CONTRACT;

        if (!th.IsTypeDesc())
        {
            MethodTable *pMT = th.AsMethodTable();
            CorElementType elType = pMT->GetSignatureCorElementType();
            if (pMT->HasInstantiation())
            {
                pSig->AppendElementType(ELEMENT_TYPE_GENERICINST);
                pSig->AppendElementType(pMT->IsValueType() ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS);
                pSig->AppendToken(GetToken(pMT));
                pSig->AppendData(pMT->GetNumGenericArgs());
                Instantiation inst = pMT->GetInstantiation();
                for (DWORD i = 0; i < inst.GetNumArgs(); i++)
                {
                    TypeHandle t = inst[i];
                    CONSISTENCY_CHECK(!t.IsNull() && !t.IsEncodedFixup());
                    GetSignatureForType(t, pSig);
                }
            }
            else
            {
                pSig->AppendElementType(elType);
                if (elType == ELEMENT_TYPE_CLASS || elType == ELEMENT_TYPE_VALUETYPE)
                    pSig->AppendToken(GetToken(pMT));
                else
                    assert(elType <= ELEMENT_TYPE_STRING || elType == ELEMENT_TYPE_I || elType == ELEMENT_TYPE_U);
            }
        }
        else
        {
            TypeDesc *pDesc = th.AsTypeDesc();
            CorElementType et = pDesc->GetInternalCorElementType();
            switch (et)
            {
                case ELEMENT_TYPE_PTR:
                case ELEMENT_TYPE_BYREF:
                case ELEMENT_TYPE_SZARRAY:
                {
                    pSig->AppendElementType(et);
                    GetSignatureForType(th.GetTypeParam(), pSig);
                    break;
                }

                case ELEMENT_TYPE_VALUETYPE:
                {
                    pSig->AppendElementType((CorElementType)ELEMENT_TYPE_NATIVE_VALUETYPE);
                    GetSignatureForType(th.GetTypeParam(), pSig);
                    break;
                }

                case ELEMENT_TYPE_ARRAY:
                {
                    pSig->AppendElementType(et);
                    GetSignatureForType(th.GetTypeParam(), pSig);

                    ArrayTypeDesc *arrayDesc = th.AsArray();

                    ULONG rank = arrayDesc->GetRank();
                    pSig->AppendData(rank);

                    if (rank != 0)
                    {
                        pSig->AppendData(0); // sizes
                        pSig->AppendData(0); // bounds
                    }
                    break;
                }

                default:
                {
                    UNREACHABLE_MSG("Unexpected typedesc type");
                }
            }
        }
    }

    virtual
    mdToken GetTokenForType(CORINFO_CLASS_HANDLE type)
    {
        STANDARD_VM_CONTRACT;

        TypeHandle th(type);

        if (!th.IsTypeDesc())
        {
            return GetTokenForType(th.AsMethodTable());
        }
        else
        {
            SigBuilder sb;
            GetSignatureForType(th, &sb);

            DWORD cbSig;
            PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sb.GetSignature(&cbSig);
                        
            return GetTypeSpecToken(pSig, cbSig);
        }
    }

    struct NameHashElement
    {
        const CHAR *GetKey()
        {
            return m_name;
        }
        CHAR *m_name;
        ULONG m_nameOffs;
    };

    class NameHashElementTraits : public StringSHashTraits<NameHashElement, CHAR>
    {
public:
        static inline void OnDestructPerEntryCleanupAction(NameHashElement *e)
        {
            STANDARD_VM_CONTRACT;
            delete [] e->m_name;
        }
        static const bool s_DestructPerEntryCleanupAction = true;
    };

    typedef SHash<NameHashElementTraits> NameHash;
    NameHash m_nameHash;

    ULONG EmitName(LPCSTR name)
    {
        STANDARD_VM_CONTRACT;

        if (name == NULL || *name == 0)
        {
            return 0; // again, empty string at offset 0;
        }

        NameHashElement *pCacheElement = m_nameHash.Lookup(name);
        if (pCacheElement != NULL)
            return pCacheElement->m_nameOffs;

        // emit name into the name pool, return offset
        ULONG nameOffs = m_pZapImage->m_namePool.GetCount();
        ULONG nameLength = (ULONG)(strlen(name) + 1);
        m_pZapImage->m_namePool.SetCount(nameOffs + nameLength);
        memcpy(&m_pZapImage->m_namePool[(COUNT_T)nameOffs], name, nameLength);

        // Insert into hash
        NewHolder<NameHashElement> pNewCacheElement = new NameHashElement();
        size_t cchNameBuffer = strlen(name) + 1;
        pNewCacheElement->m_name = new CHAR[cchNameBuffer];
        strcpy_s(pNewCacheElement->m_name, cchNameBuffer, name);
        pNewCacheElement->m_nameOffs = nameOffs;

        m_nameHash.Add(pNewCacheElement);
        pNewCacheElement.SuppressRelease();

        return  nameOffs;
    }

    EEClass *LoadTypeRef(Module *module, mdTypeRef typeRefToken, LoadFailureEnum loadflag = ThrowOnLoadFailure)
    {
        STANDARD_VM_CONTRACT;

        const size_t NUM_ARGS = 1024;
        TypeHandle instArgs[NUM_ARGS];
        for (size_t i = 0; i < NUM_ARGS; i++)
        {
            instArgs[i] = TypeHandle(g_pCanonMethodTableClass);
        }
        Instantiation typeInstantiation(instArgs, NUM_ARGS);
        Instantiation methodInstantiation(instArgs, NUM_ARGS);
        SigTypeContext typeContext(typeInstantiation, methodInstantiation);

        EEClass *result = NULL;
        if (loadflag == ReturnNullOnLoadFailure)
        {
            EX_TRY
            {
                // load the type ref
                TypeHandle th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(module, typeRefToken, &typeContext,
                                                                            ClassLoader::ThrowIfNotFound,
                                                                            ClassLoader::PermitUninstDefOrRef);
                if (!th.IsTypeDesc())
                    result = th.AsMethodTable()->GetClass();
                else
                    result = NULL;
            }
            EX_CATCH
            {
                // do nothing
            }
            EX_END_CATCH(RethrowCorruptingExceptions)
        }
        else
        {
            // load the type ref
            TypeHandle th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(module, typeRefToken, &typeContext,
                                                                        ClassLoader::ThrowIfNotFound,
                                                                        ClassLoader::PermitUninstDefOrRef);
            if (!th.IsTypeDesc())
                result = th.AsMethodTable()->GetClass();
            else
            {
                IfFailThrow(E_UNEXPECTED);
            }
        }

        return result;
    }

    EEClass *LoadTypeDef(Module *module, mdTypeDef typeDefToken, LoadFailureEnum loadflag = ThrowOnLoadFailure)
    {
        STANDARD_VM_CONTRACT;

        const size_t NUM_ARGS = 1024;
        TypeHandle instArgs[NUM_ARGS];
        for (size_t i = 0; i < NUM_ARGS; i++)
        {
            instArgs[i] = TypeHandle(g_pCanonMethodTableClass);
        }
        Instantiation typeInstantiation(instArgs, NUM_ARGS);
        Instantiation methodInstantiation(instArgs, NUM_ARGS);
        SigTypeContext typeContext(typeInstantiation, methodInstantiation);

        EEClass *result = NULL;
        if (loadflag == ReturnNullOnLoadFailure)
        {
            EX_TRY
            {
                // load the type ref
                TypeHandle th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(module, typeDefToken, &typeContext,
                                                                            ClassLoader::ThrowIfNotFound,
                                                                            ClassLoader::PermitUninstDefOrRef);
                if (!th.IsTypeDesc())
                    result = th.AsMethodTable()->GetClass();
                else
                    result = NULL;
            }
            EX_CATCH
            {
                // do nothing
            }
            EX_END_CATCH(RethrowCorruptingExceptions)
        }
        else
        {
            // load the type def
            TypeHandle th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(module, typeDefToken, &typeContext,
                                                                        ClassLoader::ThrowIfNotFound,
                                                                        ClassLoader::PermitUninstDefOrRef);
            if (!th.IsTypeDesc())
                result = th.AsMethodTable()->GetClass();
            else
            {
                IfFailThrow(E_UNEXPECTED);
            }
        }

        return result;
    }

    MethodDesc *LoadMethod(Module *module, mdMemberRef memberRefToken, LoadFailureEnum loadflag = ThrowOnLoadFailure)
    {
        STANDARD_VM_CONTRACT;

        const size_t NUM_ARGS = 1024;
        TypeHandle instArgs[NUM_ARGS];
        for (size_t i = 0; i < NUM_ARGS; i++)
        {
            instArgs[i] = TypeHandle(g_pCanonMethodTableClass);
        }
        Instantiation typeInstantiation(instArgs, NUM_ARGS);
        Instantiation methodInstantiation(instArgs, NUM_ARGS);
        SigTypeContext typeContext(typeInstantiation, methodInstantiation);

        MethodDesc *result = NULL;

        /// MDIL_NEEDS_REVIEW
        /// GetMethodDescFromMemberDefOrRefOrSpecThrowing has been apparently renamed (the "throw" part has been deleted)
        /// Confirm this....
        /// The same seems to be true for GetFieldDescFromMemberRefThrowing .....

        EX_TRY
        {
            result = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(module, memberRefToken, &typeContext, FALSE, TRUE);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowCorruptingExceptions)

        if (result != NULL)
            return result;

        // retry with strictMetadataChecks = TRUE - the above may have failed because of generic constraint checking
        if (loadflag == ReturnNullOnLoadFailure)
        {
            EX_TRY
            {
                result = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(module, memberRefToken, &typeContext, TRUE, TRUE);
            }
            EX_CATCH
            {
                // do nothing
            }
            EX_END_CATCH(RethrowCorruptingExceptions)
        }
        else
        {
            result = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(module, memberRefToken, &typeContext, TRUE, TRUE);
        }

        return result;
    }

    FieldDesc *LoadField(Module *module, mdMemberRef memberRefToken, LoadFailureEnum loadflag = ThrowOnLoadFailure)
    {
        STANDARD_VM_CONTRACT;

        const size_t NUM_ARGS = 1024;
        TypeHandle instArgs[NUM_ARGS];
        for (size_t i = 0; i < NUM_ARGS; i++)
        {
            instArgs[i] = TypeHandle(g_pCanonMethodTableClass);
        }
        Instantiation typeInstantiation(instArgs, NUM_ARGS);
        Instantiation methodInstantiation(instArgs, NUM_ARGS);
        SigTypeContext typeContext(typeInstantiation, methodInstantiation);

        FieldDesc *result = NULL;

        if (loadflag == ReturnNullOnLoadFailure)
        {
            EX_TRY
            {
                result = MemberLoader::GetFieldDescFromMemberDefOrRef(module, memberRefToken, &typeContext, true);
            }
            EX_CATCH
            {
                // do nothing
            }
            EX_END_CATCH(RethrowCorruptingExceptions)
        }
        else
        {
            result = MemberLoader::GetFieldDescFromMemberDefOrRef(module, memberRefToken, &typeContext, true);
        }

        return result;
    }

    void EncodeType(SigPointer *sig, ByteStreamWriter *stream)
    {

        STANDARD_VM_CONTRACT;
        BYTE b;
        sig->GetByte(&b);
        switch (b)
        {
        case    ELEMENT_TYPE_VOID:
        case    ELEMENT_TYPE_BOOLEAN:
        case    ELEMENT_TYPE_CHAR:
        case    ELEMENT_TYPE_I1:
        case    ELEMENT_TYPE_U1:
        case    ELEMENT_TYPE_I2:
        case    ELEMENT_TYPE_U2:
        case    ELEMENT_TYPE_I4:
        case    ELEMENT_TYPE_U4:
        case    ELEMENT_TYPE_I8:
        case    ELEMENT_TYPE_U8:
        case    ELEMENT_TYPE_R4:
        case    ELEMENT_TYPE_R8:
        case    ELEMENT_TYPE_STRING:
        case    ELEMENT_TYPE_I:
        case    ELEMENT_TYPE_U:
        case    ELEMENT_TYPE_TYPEDBYREF:
        case    ELEMENT_TYPE_OBJECT:
            stream->WriteByte(b);
            break;

        // every type above PTR will be simple type
        case    ELEMENT_TYPE_PTR:
        case    ELEMENT_TYPE_BYREF:
        case    ELEMENT_TYPE_SZARRAY:
        case    ELEMENT_TYPE_NATIVE_VALUETYPE:
            stream->WriteByte(b);
            EncodeType(sig, stream);
            break;

        // Please use case    ELEMENT_TYPE_VALUETYPE. case    ELEMENT_TYPE_VALUECLASS is deprecated.
        case    ELEMENT_TYPE_VALUETYPE:
        case    ELEMENT_TYPE_CLASS:
        {
            stream->WriteByte(b);
            mdToken typeToken;
            sig->GetToken(&typeToken);
            WriteTypeDefOrRef(typeToken, stream);
            break;
        }

        case    ELEMENT_TYPE_VAR:
        case    ELEMENT_TYPE_MVAR:
            {
                stream->WriteByte(b);
                ULONG index;
                sig->GetData(&index);
                stream->WriteUnsigned(index);
            }
            break;

        case    ELEMENT_TYPE_GENERICINST:
            {
                stream->WriteByte(b);
                EncodeType(sig, stream);
                ULONG typeArgCount;
                sig->GetData(&typeArgCount);
                stream->WriteUnsigned(typeArgCount);
                for (ULONG typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
                {
                    EncodeType(sig, stream);
                }
            }
            break;

        case    ELEMENT_TYPE_ARRAY:
            {
                stream->WriteByte(b);
                EncodeType(sig, stream);
                ULONG rank;
                sig->GetData(&rank);
                stream->WriteUnsigned(rank);
                ULONG boundCount;
                sig->GetData(&boundCount);
                stream->WriteUnsigned(boundCount);
                for (unsigned i = 0; i < boundCount; i++)
                {
                    ULONG bound;
                    sig->GetData(&bound);
                    stream->WriteUnsigned(bound);
                }
                ULONG lowerBoundCount;
                sig->GetData(&lowerBoundCount);
                stream->WriteUnsigned(lowerBoundCount);
                for (unsigned i = 0; i < lowerBoundCount; i++)
                {
                    ULONG lowerBound;
                    sig->GetData(&lowerBound);
                    stream->WriteUnsigned(lowerBound);
                }
            }
            break;

        case    ELEMENT_TYPE_CMOD_REQD:
        case    ELEMENT_TYPE_CMOD_OPT:
            {
                mdToken typeToken;
                sig->GetToken(&typeToken);
                EncodeType(sig, stream);
            }
            break;

        case    ELEMENT_TYPE_FNPTR:
            {
                stream->WriteByte(b);
                ULONG callingConvInfo;
                sig->GetCallingConvInfo(&callingConvInfo);
                stream->WriteUnsigned(callingConvInfo);
                ULONG argCount;
                sig->GetData(&argCount);
                stream->WriteUnsigned(argCount);
                for (ULONG argIndex = 0; argIndex <= argCount; argIndex++)
                {
                    EncodeType(sig, stream);
                }
            }
            break;

        default:
            stream->WriteByte(ELEMENT_TYPE_I);
            printf("type spec not yet impemented: %x\n", b);
            break;
        }
    }

    DWORD TypeDefOfPrimitive(CorElementType elType)
    {
        STANDARD_VM_CONTRACT;

        MethodTable *pMT = MscorlibBinder::GetElementType(elType);
        if (pMT == NULL)
        {
            printf("Primitive type not found: 0x%x\n", elType);
            return 0;
        }
        else
            return pMT->GetCl();
    }

    DWORD TypeDefOfNamedType(__in_z char *nameSpace, __in_z char *name)
    {
        STANDARD_VM_CONTRACT;

        MethodTable *pMT = ClassLoader::LoadTypeByNameThrowing(   m_pModule->GetAssembly(), nameSpace, name, 
                                                                  ClassLoader::ReturnNullIfNotFound, 
                                                                  // == FailIfNotLoadedOrNotRestored
                                                                  ClassLoader::LoadTypes,
                                                                  CLASS_LOADED).AsMethodTable();
        if (pMT == NULL)
        {
            printf("Named type not found: %s.%s\n", nameSpace, name);
            return 0;
        }
        else
            return pMT->GetCl();
    }

    IAssemblyName *CreateAssemblyNameFromAssemblyToken(IMDInternalImport *pMDImport, mdToken assemblyToken)
    {
        STANDARD_VM_CONTRACT;

        LPCSTR assemblyName;
        AssemblyMetaDataInternal asmMetadataInternal = {0};
        const void *pvPublicKeyToken;
        ULONG dwPublicKeyToken;
        DWORD dwAssemblyRefFlags;

        if (TypeFromToken(assemblyToken) == mdtAssemblyRef)
        {
            // Gather assembly ref information
            IfFailThrow(pMDImport->GetAssemblyRefProps(assemblyToken, &pvPublicKeyToken, &dwPublicKeyToken, &assemblyName, &asmMetadataInternal, NULL, NULL, &dwAssemblyRefFlags));
        }
        else
        {
            IfFailThrow(pMDImport->GetAssemblyProps(assemblyToken, &pvPublicKeyToken, &dwPublicKeyToken, NULL, &assemblyName, &asmMetadataInternal, &dwAssemblyRefFlags));
        }

        SString szName(SString::Utf8, assemblyName);

        // Create AssemblyName object
        ReleaseHolder<IAssemblyName> pName;

        IfFailThrow(CreateAssemblyNameObject(&pName, szName.GetUnicode(), 0, NULL));

        IfFailThrow(pName->SetProperty(ASM_NAME_MAJOR_VERSION, &asmMetadataInternal.usMajorVersion, sizeof(WORD)));
        IfFailThrow(pName->SetProperty(ASM_NAME_MINOR_VERSION, &asmMetadataInternal.usMinorVersion, sizeof(WORD)));
        IfFailThrow(pName->SetProperty(ASM_NAME_REVISION_NUMBER, &asmMetadataInternal.usRevisionNumber, sizeof(WORD)));
        IfFailThrow(pName->SetProperty(ASM_NAME_BUILD_NUMBER, &asmMetadataInternal.usBuildNumber, sizeof(WORD)));

        if (asmMetadataInternal.szLocale)
        {
            SString szLocaleString;
            szLocaleString.SetUTF8(asmMetadataInternal.szLocale);
            IfFailThrow(pName->SetProperty(ASM_NAME_CULTURE, szLocaleString.GetUnicode(), (szLocaleString.GetCount() + 1) * sizeof(WCHAR)));
        }

        // See if the assembly[def] is retargetable (ie, for a generic assembly).
        if (IsAfRetargetable(dwAssemblyRefFlags)) {
            BOOL bTrue = TRUE;
            IfFailThrow(pName->SetProperty(ASM_NAME_RETARGET, &bTrue, sizeof(bTrue)));
        }

        // Set public key or public key token
        if (IsAfPublicKey(dwAssemblyRefFlags)) {
            IfFailThrow(pName->SetProperty(((pvPublicKeyToken && dwPublicKeyToken) ? (ASM_NAME_PUBLIC_KEY) : (ASM_NAME_NULL_PUBLIC_KEY)),
                                 pvPublicKeyToken, dwPublicKeyToken * sizeof(BYTE)));
        }
        else {
            IfFailThrow(pName->SetProperty(((pvPublicKeyToken && dwPublicKeyToken) ? (ASM_NAME_PUBLIC_KEY_TOKEN) : (ASM_NAME_NULL_PUBLIC_KEY_TOKEN)),
                                pvPublicKeyToken, dwPublicKeyToken * sizeof(BYTE)));
        }

        // Set Content Type
        if (!IsAfContentType_Default(dwAssemblyRefFlags))
        {
            if (IsAfContentType_WindowsRuntime(dwAssemblyRefFlags))
            {
                DWORD dwContentType = AssemblyContentType_WindowsRuntime;
                IfFailThrow(pName->SetProperty(ASM_NAME_CONTENT_TYPE, (LPBYTE)&dwContentType, sizeof(dwContentType)));
            }
            else
            {
                IfFailThrow(COR_E_BADIMAGEFORMAT);
            }
        }

        pName.SuppressRelease();
        return pName;
    }

    void GetAssemblyDisplayNameFromIAssemblyName(IAssemblyName *pAssemblyName, SString *pStringName)
    {
        STANDARD_VM_CONTRACT;

        DWORD cchDisplayName = 0;
        HRESULT hr = pAssemblyName->GetDisplayName(NULL, &cchDisplayName, ASM_DISPLAYF_FULL);
        if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            IfFailThrow(hr);
        }

        IfFailThrow(pAssemblyName->GetDisplayName(pStringName->OpenUnicodeBuffer(cchDisplayName), &cchDisplayName, ASM_DISPLAYF_FULL));
        pStringName->CloseBuffer(cchDisplayName-1);
    }

    ULONG EmitAssemblyName(IMDInternalImport *pMDImport, mdToken assemblyToken)
    {
        STANDARD_VM_CONTRACT;

        ReleaseHolder<IAssemblyName> pAsmName = CreateAssemblyNameFromAssemblyToken(pMDImport, assemblyToken);
        StackSString ssAssemblyName;
        GetAssemblyDisplayNameFromIAssemblyName(pAsmName, &ssAssemblyName);
        StackScratchBuffer scratchBufferUtf8;

        return EmitName(ssAssemblyName.GetUTF8(scratchBufferUtf8));
    }

    void CreateExternalReferences()
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pMDImport = m_pModule->GetMDImport();

        HENUMInternalHolder hEnumAssemblyRef(pMDImport);
        hEnumAssemblyRef.EnumAllInit(mdtAssemblyRef);
        COUNT_T assemblyRefCount = hEnumAssemblyRef.EnumGetCount();
        m_pZapImage->m_extModRef.SetCount(assemblyRefCount+1);
        mdAssemblyRef assemblyRefToken;
//		printf("Assembly refs: %d\n", assemblyRefCount);

        // Initialize values
        m_pZapImage->m_extModRef[0].name = 0xdeafbeef;
        m_pZapImage->m_extModRef[0].flags = ZapImage::ExtModRef::NO_FLAGS;

        while (hEnumAssemblyRef.EnumNext(&assemblyRefToken))
        {
            COUNT_T rid = RidFromToken(assemblyRefToken);
            m_pZapImage->m_extModRef[rid].name = EmitAssemblyName(pMDImport, assemblyRefToken);
            m_pZapImage->m_extModRef[rid].flags = ZapImage::ExtModRef::IS_FROM_IL_METADATA;
//			printf("  %d = %s\n", rid, assemblyName);
        }


        HENUMInternalHolder hEnumModuleRef(pMDImport);
        hEnumModuleRef.EnumAllInit(mdtModuleRef);
        COUNT_T moduleRefCount = hEnumModuleRef.EnumGetCount();
        m_pZapImage->m_extModRef.SetCount(assemblyRefCount+moduleRefCount+1);
//		printf("Module refs: %d\n", moduleRefCount);
        mdModuleRef moduleRefToken;
        while (hEnumModuleRef.EnumNext(&moduleRefToken))
        {
            COUNT_T rid = RidFromToken(moduleRefToken);
            // we set the module names lazily because we don't want names from PInvokes
            // (e.g. kernel32.dll) to end up in here. We set the name only when we
            // encounter a type ref from this module
            m_pZapImage->m_extModRef[assemblyRefCount + rid].name = 0;
            m_pZapImage->m_extModRef[assemblyRefCount + rid].flags = ZapImage::ExtModRef::IS_MODULE_REF;
        }

        HENUMInternalHolder hEnumTypeRef(pMDImport);
        hEnumTypeRef.EnumAllInit(mdtTypeRef);
        ULONG typeRefCount = hEnumTypeRef.EnumGetCount();
        mdTypeRef typeRefToken;

        m_pZapImage->m_extTypeRef.Preallocate((typeRefCount + 1) * 2);
        m_pZapImage->m_extTypeRefExtend.Preallocate((typeRefCount + 1) * 2);

        m_pZapImage->m_extTypeRef.SetCount(typeRefCount+1);
        // Initialize (unused) value
        m_pZapImage->m_extTypeRef[0].module = 0;
        m_pZapImage->m_extTypeRef[0].ordinal = 0;
        while (hEnumTypeRef.EnumNext(&typeRefToken))
        {
            COUNT_T rid = RidFromToken(typeRefToken);
            LPCSTR nameSpace;
            LPCSTR typeName;
            pMDImport->GetNameOfTypeRef(typeRefToken, &nameSpace, &typeName);
//            printf("%08x %s.%s", typeRefToken, nameSpace, typeName);
            mdToken scopeToken;
            pMDImport->GetResolutionScopeOfTypeRef(typeRefToken, &scopeToken);

            // We found a case where a typeref had itself as the resolution scope,
            // possibly to hinder reverse engineering.
            // so limit the number of iterations and pretend the type has not been loaded
            // if we reach the limit (bug #286371).
            int iter = 0;
            const int maxIter = 1000;
            while (pMDImport->IsValidToken(scopeToken) && TypeFromToken(scopeToken) == mdtTypeRef && iter < maxIter)
            {
                pMDImport->GetNameOfTypeRef(scopeToken, &nameSpace, &typeName);
//                printf(" from %08x %s.%s", scopeToken, nameSpace, typeName);
                pMDImport->GetResolutionScopeOfTypeRef(scopeToken, &scopeToken);
                iter++;
            }

            EEClass *extType = NULL;
            if (iter < maxIter)
                extType = LoadTypeRef(m_pModule, typeRefToken, ReturnNullOnLoadFailure);
            m_pZapImage->m_extTypeRef[rid].ordinal =
                extType ? RidFromToken(extType->GetMethodTable()->GetCl())
                        : 0;

            COUNT_T scopeRid = RidFromToken(scopeToken);
            if (!pMDImport->IsValidToken(scopeToken))
            {
                m_pZapImage->m_extTypeRef[rid].module = 0;
            }
            else if (TypeFromToken(scopeToken) == mdtAssemblyRef)
            {
                if (extType != NULL)
                {
                    Module *extModule = extType->GetMethodTable()->GetModule();

                    // Use the assembly ref rid as module id, and let the binder do any resolution to correct
                    // assembly, or any type forwarding necessary. This allows type-forwarding detection from
                    // within signatures to operate properly.
                    scopeRid = RidFromToken(scopeToken);

                    LoadHintEnum loadHint = LoadDefault;
                    LoadHintEnum defaultLoadHint = LoadDefault;
                    m_pZapImage->GetCompileInfo()->GetLoadHint((CORINFO_ASSEMBLY_HANDLE)m_pModule->GetAssembly(),
                                                               (CORINFO_ASSEMBLY_HANDLE)extModule->GetAssembly(),
                                                               &loadHint,
                                                               &defaultLoadHint);
                    if (loadHint == LoadAlways)
                    {
                        m_pZapImage->m_extModRef[scopeRid].flags = 
                          (ZapImage::ExtModRef::ExtModRefFlags)(m_pZapImage->m_extModRef[scopeRid].flags | ZapImage::ExtModRef::IS_EAGERLY_BOUND);
                    }
                }
                m_pZapImage->m_extTypeRef[rid].module = scopeRid;
            }
            else if (TypeFromToken(scopeToken) == mdtModuleRef)
            {
//                assert(!"TypeRef with ModuleRef scope currently not supported");
                COUNT_T moduleRid = assemblyRefCount + scopeRid;
                m_pZapImage->m_extTypeRef[rid].module = moduleRid;

                // set the module name because now we know the module is referenced
                if (m_pZapImage->m_extModRef[moduleRid].name == 0)
                {
                    LPCSTR moduleName;
                    assert((m_pZapImage->m_extModRef[moduleRid].flags & ZapImage::ExtModRef::IS_MODULE_REF) != 0);
                    pMDImport->GetModuleRefProps(scopeToken, &moduleName);
                    m_pZapImage->m_extModRef[moduleRid].name = EmitName(moduleName);
                    m_pZapImage->m_extModRef[moduleRid].flags = 
                      (ZapImage::ExtModRef::ExtModRefFlags)(m_pZapImage->m_extModRef[moduleRid].flags | ZapImage::ExtModRef::IS_LOCAL_MODULE);
                }
            }
            else if (TypeFromToken(scopeToken) == mdtModule)
            {
                // haven't figured out that yet - skip for now
                // there are also bogus entries with a resolution scope of 0 (??)
                assert(scopeToken == TokenFromRid(1, mdtModule) || scopeToken == 0);
                m_pZapImage->m_extTypeRef[rid].module = 0;
            }
            else if (TypeFromToken(scopeToken) == mdtTypeRef)
            {
                // this must be the case of the long (or infinite)
                // typeref chain - give up
                assert(iter >= maxIter);
                m_pZapImage->m_extTypeRef[rid].module = 0;
            }
            else
            {
                // hmm, ignore these cases for now
                assert(!"NYI");
//                printf("scopeToken = %08x\n", scopeToken);
            }
        }

        HENUMInternalHolder hEnumTypeSpec(pMDImport);
        hEnumTypeSpec.EnumAllInit(mdtTypeSpec);
        ULONG typeSpecCount = hEnumTypeSpec.EnumGetCount();
        m_pZapImage->m_typeSpecToOffs.SetCount(typeSpecCount+1);

        // init (unused) data
        m_pZapImage->m_typeSpecToOffs[0] = 0;

        mdTypeSpec typeSpecToken;
        size_t typeSpecSize = 0;
        while (hEnumTypeSpec.EnumNext(&typeSpecToken))
        {
            PCCOR_SIGNATURE pSig;
            ULONG cbSig;
            pMDImport->GetTypeSpecFromToken(typeSpecToken, &pSig, &cbSig);
            
            //printf("%08x: ", typeSpecToken);
            //for (ULONG i = 0; i < cbSig; i++)
            //    printf("%02x ", pSig[i]);
            //printf("\n");

            SigPointer sig(pSig, cbSig);
            EncodeType(&sig, m_stream);

            size_t size;
            BYTE *buffer = m_stream->GetBuffer(size);
            m_pZapImage->FlushCompactLayoutData(typeSpecToken, buffer, (ULONG)size);
            m_stream->Reset();
            typeSpecSize += size;
        }
//        printf("total encoded typespec size is %u for %d typespecs - average %5.1f\n", typeSpecSize, typeSpecCount, (double)typeSpecSize/typeSpecCount);

        HENUMInternalHolder hEnumMemberRef(pMDImport);
        hEnumMemberRef.EnumAllInit(mdtMemberRef);
        ULONG memberRefCount = hEnumMemberRef.EnumGetCount();

        m_pZapImage->m_extMemberRef.Preallocate((memberRefCount + 1) * 2);
        m_pZapImage->m_extMemberRefExtend.Preallocate((memberRefCount + 1) * 2);

        m_pZapImage->m_extMemberRef.SetCount(memberRefCount+1);
        mdMemberRef memberRefToken;

        // Initialize (unused) value
        m_pZapImage->m_extMemberRef[0].isTypeSpec = false;
        m_pZapImage->m_extMemberRef[0].typeRid    = 0;
        m_pZapImage->m_extMemberRef[0].isField    = false;
        m_pZapImage->m_extMemberRef[0].ordinal    = 0;

        while (hEnumMemberRef.EnumNext(&memberRefToken))
        {
            PCCOR_SIGNATURE pSig;
            ULONG cbSig;
            LPCSTR name;
            IfFailThrow(pMDImport->GetNameAndSigOfMemberRef(memberRefToken, &pSig, &cbSig, &name));
            mdToken parentToken;
            IfFailThrow(pMDImport->GetParentOfMemberRef(memberRefToken, &parentToken));

//            printf("%08x Parent = %08x  Name = %s\n", memberRefToken, parentToken, name);
            DWORD extRefRid = RidFromToken(memberRefToken);
            if (TypeFromToken(parentToken) != mdtTypeRef && TypeFromToken(parentToken) != mdtTypeSpec && TypeFromToken(parentToken) != mdtTypeDef)
            {
                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].isTypeSpec = false;;
                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].typeRid = 0;
                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].isField = FALSE;
                if (TypeFromToken(parentToken) == mdtMethodDef)
                {
                    m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].ordinal = RidFromToken(parentToken);
                }
                else
                {
                    m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].ordinal = 0;
                    printf("MemberRef parent is %08x - giving up\n", parentToken);
                }
                continue;
            }

            EEClass *extType = NULL;
            if (TypeFromToken(parentToken) == mdtTypeDef)
            {
                extType = LoadTypeDef(m_pModule, parentToken, ReturnNullOnLoadFailure);

                // Create a TypeSpec token to point to the type. (The MDIL file format does not 
                // allow a TypeDef to parent a MemberRef, but IL does. We work around this by simply 
                // converting the typedef into a typespec.

                ByteStreamWriter stream;
                if ((extType == NULL) || !extType->GetMethodTable()->IsValueType()) 
                {
                    // If extType is NULL, whether or not the type is a valuetype does not matter.
                    stream.WriteByte(ELEMENT_TYPE_CLASS);
                }
                else
                {
                    stream.WriteByte(ELEMENT_TYPE_VALUETYPE);
                }
                WriteTypeDefOrRef(parentToken, &stream);

                // Get the new TypeSpec token.
                parentToken = GetTypeSpecToken(&stream);
            }
            else
            {
                extType = LoadTypeRef(m_pModule, parentToken, ReturnNullOnLoadFailure);
            }

            m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].isTypeSpec = TypeFromToken(parentToken) == mdtTypeSpec;
            m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].typeRid = RidFromToken(parentToken);

            mdToken extTypeDefToken = 0;
            Module *extModule = NULL;
            if (extType != NULL)
            {
                extTypeDefToken = extType->GetMethodTable()->GetCl();
                extModule = extType->GetMethodTable()->GetModule();
            }

            SigPointer sigPointer(pSig, cbSig);
            ULONG callingConv;
            sigPointer.GetCallingConv(&callingConv);

            if (callingConv != IMAGE_CEE_CS_CALLCONV_FIELD)
            {
                MethodDesc *pMD = LoadMethod(m_pModule, memberRefToken, ReturnNullOnLoadFailure);
                ULONG ordinal;
                if (pMD == NULL)
                {
                    ordinal = ~0;
                }
                else
                {
                    mdMethodDef methodDefToken = pMD->GetMemberDef();
                    if (IsNilToken(methodDefToken))
                    {
                        // this should only happen for arrays
                        assert(pMD->GetClass()->GetInternalCorElementType() == ELEMENT_TYPE_ARRAY
                            || pMD->GetClass()->GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY);
                        if (pMD->GetClass() != extType)
                        {
                            extType = pMD->GetClass();
                            extTypeDefToken = extType->GetMethodTable()->GetCl();
                            extModule = extType->GetMethodTable()->GetModule();
                            if (TypeFromToken(parentToken) == mdtTypeRef)
                            {
                                ULONG parentOrdinal = FindOrCreateExtTypeRef(extType->GetMethodTable());
                                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].typeRid = parentOrdinal;
                            }
                        }
                        ordinal = pMD->GetSlot();
                    }
                    // handle the case where the member specified is in a baseclass of the type
                    // referenced. In this case we generate not a proper methodIndex and defer to
                    // binding by name/signature
                    else if (pMD->GetClass() != extType)
                    {
                        ordinal = ~0;
                    }
                    else
                    {
                        ordinal = GetMethodOrdinal(extModule, extTypeDefToken, methodDefToken);
                    }
                }
                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].isField = FALSE;
                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].ordinal = ordinal;
            }
            else
            {
                ULONG ordinal;
                FieldDesc *pFD = LoadField(m_pModule, memberRefToken, ReturnNullOnLoadFailure);
                if (pFD == NULL)
                    ordinal = ~0;
                else
                {
                    // handle the case where the member specified is in a baseclass of the type
                    // referenced. In this case what we generate here is not as versionable as the original IL
                    // this is something we need to think about...
                    if (pFD->GetApproxEnclosingMethodTable()->GetClass() != extType)
                    {
                        extType = pFD->GetApproxEnclosingMethodTable()->GetClass();
                        extTypeDefToken = extType->GetMethodTable()->GetCl();
                        extModule = extType->GetMethodTable()->GetModule();
                        ULONG parentOrdinal = FindOrCreateExtTypeRef(extType->GetMethodTable());
                        m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].typeRid = parentOrdinal;
                    }
                    ordinal = GetFieldOrdinal(extModule, extTypeDefToken, pFD->GetMemberDef());
                }
                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].isField = TRUE;
                m_pZapImage->m_extMemberRef[(COUNT_T)extRefRid].ordinal = ordinal;
            }
        }

        HENUMInternalHolder hEnumMethodSpec(pMDImport);
        hEnumMethodSpec.EnumAllInit(mdtMethodSpec);
        ULONG methodSpecCount = hEnumMethodSpec.EnumGetCount();
        m_pZapImage->m_methodSpecToOffs.SetCount(methodSpecCount+1);
        mdMethodSpec methodSpecToken;
        size_t methodSpecSize = 0;

        // Initialize (unused) value
        m_pZapImage->m_methodSpecToOffs[0] = 0;
        while (hEnumMethodSpec.EnumNext(&methodSpecToken))
        {
            PCCOR_SIGNATURE pSig;
            ULONG cbSig;
            unsigned parentToken;
            pMDImport->GetMethodSpecProps(methodSpecToken, &parentToken, &pSig, &cbSig);
            
            //printf("%08x: %08x ", methodSpecToken, parentToken);
            //for (ULONG i = 0; i < cbSig; i++)
            //    printf("%02x ", pSig[i]);
            //printf("\n");

            if (TypeFromToken(parentToken) == mdtMemberRef)
            {
                MethodDesc *pMD = LoadMethod(m_pModule, methodSpecToken, ReturnNullOnLoadFailure);
                if (pMD != NULL)
                {
                    COUNT_T extRefRid = RidFromToken(parentToken);
                    assert(m_pZapImage->m_extMemberRef[extRefRid].isField == FALSE);

                    MethodTable *pMT = pMD->GetMethodTable();
                    DWORD extTypeDefToken = pMT->GetCl();
                    Module *extModule = pMT->GetModule();
                    assert(extTypeDefToken != 0);
                    DWORD methodDefToken = pMD->GetMemberDef();
                    HENUMInternalHolder hEnumMethodDef(extModule->GetMDImport());
                    hEnumMethodDef.EnumInit(mdtMethodDef, extTypeDefToken);
                    ULONG methodCount = hEnumMethodDef.EnumGetCount();
                    mdMethodDef firstMethodDefToken;
                    hEnumMethodDef.EnumNext(&firstMethodDefToken);
                    ULONG ordinal = methodDefToken - firstMethodDefToken;
                    assert(ordinal < methodCount);

                    if (m_pZapImage->m_extMemberRef[extRefRid].ordinal == 0x7fff)
                        m_pZapImage->m_extMemberRef[extRefRid].ordinal = ordinal;
                    else
                        assert(m_pZapImage->m_extMemberRef[extRefRid].ordinal == ordinal);
                }
            }

            EncodeMethodSpec(parentToken, pSig, cbSig, m_stream);

            size_t size;
            BYTE *buffer = m_stream->GetBuffer(size);
            m_pZapImage->FlushCompactLayoutData(methodSpecToken, buffer, (ULONG)size);
            m_stream->Reset();
            methodSpecSize += size;
        }

        m_pZapImage->m_signatureToOffs.SetCount(1);

        // Initialize (unused) value
        m_pZapImage->m_signatureToOffs[0] = 0;

//        printf("total encoded method spec size is %u for %d methodspecs - average %5.1f\n", methodSpecSize, methodSpecCount, (double)methodSpecSize/methodSpecCount);

        if (m_pModule->IsSystem())
        {
            DWORD wellKnownTypes[WKT_COUNT];
            wellKnownTypes[WKT_OBJECT]    = TypeDefOfPrimitive(ELEMENT_TYPE_OBJECT);
            wellKnownTypes[WKT_STRING]    = TypeDefOfPrimitive(ELEMENT_TYPE_STRING);
            wellKnownTypes[WKT_VALUETYPE] = g_pValueTypeClass->GetCl();
            wellKnownTypes[WKT_ENUM]      = g_pEnumClass->GetCl();
            wellKnownTypes[WKT_ARRAY]     = g_pArrayClass->GetCl();

            wellKnownTypes[WKT_BOOLEAN]   = TypeDefOfPrimitive(ELEMENT_TYPE_BOOLEAN);
            wellKnownTypes[WKT_VOID]      = TypeDefOfPrimitive(ELEMENT_TYPE_VOID);
            wellKnownTypes[WKT_CHAR]      = TypeDefOfPrimitive(ELEMENT_TYPE_CHAR);
            wellKnownTypes[WKT_I1]        = TypeDefOfPrimitive(ELEMENT_TYPE_I1);
            wellKnownTypes[WKT_U1]        = TypeDefOfPrimitive(ELEMENT_TYPE_U1);
            wellKnownTypes[WKT_I2]        = TypeDefOfPrimitive(ELEMENT_TYPE_I2);
            wellKnownTypes[WKT_U2]        = TypeDefOfPrimitive(ELEMENT_TYPE_U2);
            wellKnownTypes[WKT_I4]        = TypeDefOfPrimitive(ELEMENT_TYPE_I4);
            wellKnownTypes[WKT_U4]        = TypeDefOfPrimitive(ELEMENT_TYPE_U4);
            wellKnownTypes[WKT_I8]        = TypeDefOfPrimitive(ELEMENT_TYPE_I8);
            wellKnownTypes[WKT_U8]        = TypeDefOfPrimitive(ELEMENT_TYPE_U8);
            wellKnownTypes[WKT_R4]        = TypeDefOfPrimitive(ELEMENT_TYPE_R4);
            wellKnownTypes[WKT_R8]        = TypeDefOfPrimitive(ELEMENT_TYPE_R8);
            wellKnownTypes[WKT_I]         = TypeDefOfPrimitive(ELEMENT_TYPE_I);
            wellKnownTypes[WKT_U]         = TypeDefOfPrimitive(ELEMENT_TYPE_U);

#ifndef FEATURE_CORECLR
            wellKnownTypes[WKT_MARSHALBYREFOBJECT] = TypeDefOfNamedType("System", "MarshalByRefObject");
#else
            wellKnownTypes[WKT_MARSHALBYREFOBJECT] = 0;
#endif
            wellKnownTypes[WKT_MULTICASTDELEGATE] = g_pMulticastDelegateClass->GetCl();
            wellKnownTypes[WKT_NULLABLE]  = g_pNullableClass->GetCl();
            wellKnownTypes[WKT_CANON]     = g_pCanonMethodTableClass->GetCl();
#ifndef FEATURE_CORECLR
            wellKnownTypes[WKT_TRANSPARENTPROXY] = TypeDefOfNamedType("System.Runtime.Remoting.Proxies", g_TransparentProxyName);
#else
            wellKnownTypes[WKT_TRANSPARENTPROXY] = 0;
#endif
#ifdef FEATURE_COMINTEROP
            wellKnownTypes[WKT_COMOBJECT] = g_pBaseCOMObject->GetCl();
            wellKnownTypes[WKT_WINDOWS_RUNTIME_OBJECT] = TypeDefOfNamedType("System.Runtime.InteropServices.WindowsRuntime", "RuntimeClass");
#else
            wellKnownTypes[WKT_COMOBJECT] = 0;
            wellKnownTypes[WKT_WINDOWS_RUNTIME_OBJECT] = 0;
#endif
            wellKnownTypes[WKT_CONTEXTBOUNDOBJECT] = TypeDefOfNamedType("System", "ContextBoundObject");
    //        for (int i = WKT_FIRST; i < WKT_COUNT; i++)
    //            printf("Well known type %u = %08x\n", i, wellKnownTypes[i]);

            wellKnownTypes[WKT_DECIMAL] = TypeDefOfNamedType("System", "Decimal");

            wellKnownTypes[WKT_TYPEDREFERENCE] = TypeDefOfNamedType("System", "TypedReference");

            m_pZapImage->FlushWellKnownTypes(wellKnownTypes, WKT_COUNT);
        }
    }

    virtual 
    mdToken GetTokenForType(InlineContext *inlineContext, CORINFO_ARG_LIST_HANDLE argList)
    {
        STANDARD_VM_CONTRACT;

        PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)argList;
        SigBuilder sigBuilder;
        if (inlineContext != NULL && !inlineContext->IsTrivial())
        {
            // expand the signature plugging in the type arguments
            SigParser sp1(pSig);
            expandSignature(sp1, inlineContext, sigBuilder);

            DWORD cSig;
            pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cSig);
        }
        SigParser sp(pSig);
        CorElementType elType;
        sp.GetElemType(&elType);
        while (elType == ELEMENT_TYPE_CMOD_REQD || elType == ELEMENT_TYPE_CMOD_OPT || elType == ELEMENT_TYPE_PINNED)
        {
            if (elType != ELEMENT_TYPE_PINNED)
            {
                mdToken modifierToken;
                sp.GetToken(&modifierToken);
            }
            sp.GetElemType(&elType);
        }
        switch (elType)
        {
        case    ELEMENT_TYPE_VALUETYPE:
            {
                mdToken structTypeToken = 0;
                sp.GetToken(&structTypeToken);
                return structTypeToken;
            }

        case    ELEMENT_TYPE_TYPEDBYREF:
            return GetTokenForType(g_TypedReferenceMT);

        case    ELEMENT_TYPE_GENERICINST:
        {
            // if this is an instantiation of a generic class rather
            // than a generic value type, we don't need a token, because
            // this is going to be a reference type no matter what the instantiation is
            sp.PeekElemType(&elType);
            if (elType == ELEMENT_TYPE_CLASS)
                return 0;

            // skip the type token of the generic type
            sp.SkipExactlyOne();
            // skip the instantiation arguments
            ULONG typeArgCount;
            sp.GetData(&typeArgCount);
            for (ULONG typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
            {
                sp.SkipExactlyOne();
            }
            break;
        }

        case    ELEMENT_TYPE_VAR:
        {
            ULONG argIndex;
            sp.GetData(&argIndex);
            break;
        }

        case    ELEMENT_TYPE_MVAR:
        {
            ULONG argIndex;
            sp.GetData(&argIndex);
            break;
        }

        case    ELEMENT_TYPE_INTERNAL:
        {
            CORINFO_CLASS_HANDLE type;
            sp.GetPointer((void **)&type);
            return GetTokenForType(type);
        }

        case    ELEMENT_TYPE_VOID:
        case    ELEMENT_TYPE_BOOLEAN:
        case    ELEMENT_TYPE_CHAR:
        case    ELEMENT_TYPE_I1:
        case    ELEMENT_TYPE_U1:
        case    ELEMENT_TYPE_I2:
        case    ELEMENT_TYPE_U2:
        case    ELEMENT_TYPE_I4:
        case    ELEMENT_TYPE_U4:
        case    ELEMENT_TYPE_I8:
        case    ELEMENT_TYPE_U8:
        case    ELEMENT_TYPE_R4:
        case    ELEMENT_TYPE_R8:
        case    ELEMENT_TYPE_STRING:
        case    ELEMENT_TYPE_PTR:
        case    ELEMENT_TYPE_BYREF:
        case    ELEMENT_TYPE_CLASS:
        case    ELEMENT_TYPE_ARRAY:
        case    ELEMENT_TYPE_I:
        case    ELEMENT_TYPE_U:
        case    ELEMENT_TYPE_FNPTR:
        case    ELEMENT_TYPE_OBJECT:
        case    ELEMENT_TYPE_SZARRAY:
            return 0;

        default:
            // oops?
            assert(!"signature not yet supported");
            break;
        }

        // Ok, we parsed to the end of this argument or local in the signature -
        // now try to find a matching typespec
        PCCOR_SIGNATURE end = sp.GetPtr();
        ULONG length = (ULONG)(end - pSig);

        return GetTypeSpecToken(pSig, length);
    }

    virtual
    mdToken GetTokenForMethod(CORINFO_METHOD_HANDLE method)
    {
        STANDARD_VM_CONTRACT;

        MethodDesc *pMD = GetMethod(method);
        InlineContext context(pMD->GetModule());

        return TranslateToken(&context, pMD->GetMemberDef());
    }

    virtual
    mdToken GetTokenForField(CORINFO_FIELD_HANDLE field)
    {
        STANDARD_VM_CONTRACT;

        FieldDesc *pFD = GetField(field);
        InlineContext context(pFD->GetModule());

        return TranslateToken(&context, pFD->GetMemberDef());
    }

    virtual
    mdToken GetTokenForSignature(PCCOR_SIGNATURE sig)
    {
        STANDARD_VM_CONTRACT;

        SigBuilder sigBuilder;
        SigParser sp(sig);
        
        BYTE b;
        sp.GetByte(&b);
        sigBuilder.AppendByte(b);

        ULONG argCount;
        sp.GetData(&argCount);
        sigBuilder.AppendData(argCount);

        for (ULONG argIndex = 0; argIndex <= argCount; argIndex++)
            expandSignature(sp, NULL, sigBuilder);

        return FindOrCreateSignature(sigBuilder);
    }

    void expandSignature(SigParser &sp, InlineContext *context, SigBuilder &sigBuilder)
    {
        STANDARD_VM_CONTRACT;

        BYTE elType;
        sp.GetByte(&elType);
        mdToken token;
        while (elType == ELEMENT_TYPE_CMOD_REQD || elType == ELEMENT_TYPE_CMOD_OPT || elType == ELEMENT_TYPE_PINNED)
        {
            sigBuilder.AppendByte(elType);
            if (elType != ELEMENT_TYPE_PINNED)
            {
                sp.GetToken(&token);
                token = TranslateToken(context, token);
                sigBuilder.AppendToken(token);
           }
            sp.GetByte(&elType);
        }
        switch (elType)
        {
        case    ELEMENT_TYPE_VALUETYPE:
        case    ELEMENT_TYPE_CLASS:
        {
            sigBuilder.AppendByte(elType);
            sp.GetToken(&token);
            token = TranslateToken(context, token);
            sigBuilder.AppendToken(token);
            break;
        }

        case    ELEMENT_TYPE_INTERNAL:
        {
            TypeHandle th;
            sp.GetPointer((void **)&th);

            if (th.IsNativeValueType())
            {
                sigBuilder.AppendByte((CorElementType)ELEMENT_TYPE_NATIVE_VALUETYPE);
                th = th.GetTypeParam();
            }
            token = GetTokenForType((CORINFO_CLASS_HANDLE)th.AsPtr());

            sigBuilder.AppendElementType(th.IsValueType() ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS);
            sigBuilder.AppendToken(token);
            break;
        }

        case    ELEMENT_TYPE_GENERICINST:
        {
            sigBuilder.AppendByte(elType);
            sp.GetByte(&elType);
            assert(elType == ELEMENT_TYPE_CLASS || elType == ELEMENT_TYPE_VALUETYPE);
            sigBuilder.AppendByte(elType);
            sp.GetToken(&token);
            token = TranslateToken(context, token);
            sigBuilder.AppendToken(token);
            ULONG typeArgCount;
            sp.GetData(&typeArgCount);
            sigBuilder.AppendData(typeArgCount);
            for (ULONG typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
            {
                expandSignature(sp, context, sigBuilder);
            }
            break;
        }

        case    ELEMENT_TYPE_VAR:
        {
            ULONG argIndex;
            sp.GetData(&argIndex);
            if (context != NULL && context->classTypeArgCount != 0)
            {
                _ASSERTE(argIndex < context->classTypeArgCount);

                // a type argument to be expanded
                SigParser argSp(context->classTypeArgs[argIndex].pSig, (DWORD)context->classTypeArgs[argIndex].cbSig);
                expandSignature(argSp, NULL, sigBuilder);
            }
            else
            {
                // a type argument to be just copied
                sigBuilder.AppendByte(elType);
                sigBuilder.AppendData(argIndex);
            }
            break;
        }

        case    ELEMENT_TYPE_MVAR:
        {
            ULONG argIndex;
            sp.GetData(&argIndex);
            if (context != NULL && context->methodTypeArgCount != 0)
            {
                _ASSERTE(argIndex < context->methodTypeArgCount);

                // a type argument to be expanded
                SigParser argSp(context->methodTypeArgs[argIndex].pSig, (DWORD)context->methodTypeArgs[argIndex].cbSig);
                expandSignature(argSp, NULL, sigBuilder);
            }
            else
            {
                // a type argument to be just copied
                sigBuilder.AppendByte(elType);
                sigBuilder.AppendData(argIndex);
            }
            break;
        }

        case    ELEMENT_TYPE_VOID:
        case    ELEMENT_TYPE_BOOLEAN:
        case    ELEMENT_TYPE_CHAR:
        case    ELEMENT_TYPE_I1:
        case    ELEMENT_TYPE_U1:
        case    ELEMENT_TYPE_I2:
        case    ELEMENT_TYPE_U2:
        case    ELEMENT_TYPE_I4:
        case    ELEMENT_TYPE_U4:
        case    ELEMENT_TYPE_I8:
        case    ELEMENT_TYPE_U8:
        case    ELEMENT_TYPE_R4:
        case    ELEMENT_TYPE_R8:
        case    ELEMENT_TYPE_STRING:
        case    ELEMENT_TYPE_I:
        case    ELEMENT_TYPE_U:
        case    ELEMENT_TYPE_OBJECT:
        case    ELEMENT_TYPE_TYPEDBYREF:
            sigBuilder.AppendByte(elType);
            break;

        case    ELEMENT_TYPE_PTR:
        case    ELEMENT_TYPE_BYREF:
        case    ELEMENT_TYPE_SZARRAY:
        case    ELEMENT_TYPE_NATIVE_VALUETYPE:
            sigBuilder.AppendByte(elType);
            expandSignature(sp, context, sigBuilder);
            break;

        case    ELEMENT_TYPE_ARRAY:
            {
                sigBuilder.AppendByte(elType);
                expandSignature(sp, context, sigBuilder);
                ULONG rank;
                sp.GetData(&rank);
                sigBuilder.AppendData(rank);
                ULONG boundCount;
                sp.GetData(&boundCount);
                sigBuilder.AppendData(boundCount);
                for (ULONG i = 0; i < boundCount; i++)
                {
                    ULONG bound;
                    sp.GetData(&bound);
                    sigBuilder.AppendData(bound);
                }
                ULONG lowerBoundCount;
                sp.GetData(&lowerBoundCount);
                sigBuilder.AppendData(lowerBoundCount);
                for (ULONG i = 0; i < lowerBoundCount; i++)
                {
                    ULONG bound;
                    sp.GetData(&bound);
                    sigBuilder.AppendData(bound);
                }
            }
            break;

        case    ELEMENT_TYPE_FNPTR:
            {
                BYTE callConv;
                sp.GetByte(&callConv);

                ULONG argCount;
                sp.GetData(&argCount);

                sigBuilder.AppendByte(elType);
                sigBuilder.AppendByte(callConv);
                sigBuilder.AppendData(argCount);

                for (ULONG i = 0; i <= argCount; i++)
                {
                    expandSignature(sp, context, sigBuilder);
                }
            }
            break;

        default:
            // oops?
            assert(!"signature not yet supported");
            break;
        }
    }

    void expandFieldOrMethodSignature(PCCOR_SIGNATURE pSig, ULONG cbSig, InlineContext *context, SigBuilder &sigBuilder)
    {
        STANDARD_VM_CONTRACT;

        SigParser sp(pSig, cbSig);
        ULONG callingConvention;
        sp.GetCallingConvInfo(&callingConvention);
        sigBuilder.AppendByte((BYTE)callingConvention);
        if (callingConvention == IMAGE_CEE_CS_CALLCONV_FIELD)
        {
            // nothing to do here...
        }
        else
        {
            if (callingConvention & IMAGE_CEE_CS_CALLCONV_GENERIC)
            {
                // uncompress number of generic type args
                ULONG genericArgCount;
                sp.GetData(&genericArgCount);
                sigBuilder.AppendData(genericArgCount);
            }
            // uncompress number of args
            ULONG argCount;
            sp.GetData(&argCount);
            sigBuilder.AppendData(argCount);
        }
        expandSignature(sp, context, sigBuilder);
    }

    virtual
    mdToken GetParentOfMemberRef(CORINFO_MODULE_HANDLE scope, mdMemberRef memberRefToken)
    {
        STANDARD_VM_CONTRACT;

        unsigned parentToken = 0;
        IMDInternalImport *pMDImport = NULL;

        HRESULT hr = E_FAIL;
        if (!IsDynamicScope(scope))
        {
            Module *module = GetModule(scope);
            pMDImport = module->GetMDImport();
            hr = pMDImport->GetParentOfMemberRef(memberRefToken, &parentToken);
        }

        if (FAILED(hr))
        {
            COUNT_T memberRefRid = RidFromToken(memberRefToken);
            COUNT_T typeRid = m_pZapImage->m_extMemberRef[memberRefRid].typeRid;
            if (m_pZapImage->m_extMemberRef[memberRefRid].isTypeSpec)
                parentToken = TokenFromRid(typeRid, mdtTypeSpec);
            else
                parentToken = TokenFromRid(typeRid, mdtTypeRef);
        }
        // For varargs, a memberref can point to a methodDef
        if (TypeFromToken(parentToken) == mdtMethodDef)
        {
            IfFailThrow(pMDImport->GetParentToken(parentToken, &parentToken));
        }
        return parentToken;
    }

    virtual
    mdToken GetArrayElementToken(CORINFO_MODULE_HANDLE scope, mdTypeSpec arrayTypeToken)
    {
        STANDARD_VM_CONTRACT;

        assert(TypeFromToken(arrayTypeToken) == mdtTypeSpec);
        assert(!IsDynamicScope(scope));
        Module *module = GetModule(scope);
        PCCOR_SIGNATURE pSig = NULL;
        ULONG cbSig;
        IMDInternalImport *pMDImport = module->GetMDImport();
        pMDImport->GetTypeSpecFromToken(arrayTypeToken, &pSig, &cbSig);
        assert(pSig[0] == ELEMENT_TYPE_ARRAY);

        InlineContext context(module);
    
        return GetTokenForType(&context, (CORINFO_ARG_LIST_HANDLE)&pSig[1]);
    }

    PCCOR_SIGNATURE GetSigOfTypeSpec(Module *module, mdTypeSpec typeSpecToken, ULONG &cbSig)
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pMDImport = module->GetMDImport();
        PCCOR_SIGNATURE pSig = 0;
        if (FAILED(pMDImport->GetTypeSpecFromToken(typeSpecToken, &pSig, &cbSig)))
        {
            // this could be a new token injected by the compilation process -
            // in this case it has to be local to the current module
            assert(module == m_pModule);

            pSig = m_tokenToSig.Get(typeSpecToken, cbSig);
            assert(pSig != NULL);
        }
        return pSig;
    }

    unsigned ReadUnsigned(BYTE *buffer)
    {
        LIMITED_METHOD_CONTRACT;

        unsigned firstByte = buffer[0];
        unsigned numberOfBytes = 1;
        while (firstByte & 1)
        {
            numberOfBytes++;
            firstByte >>= 1;
        }
        switch (numberOfBytes)
        {
        case    1:  return (buffer[0]) >> 1; 
        case    2:  return (buffer[0] + buffer[1]*256) >> 2;  
        case    3:  return (buffer[0] + buffer[1]*256 + buffer[2]*(256*256)) >> 3;
        case    4:  return (buffer[0] + buffer[1]*256 + buffer[2]*(256*256) + buffer[3]*(256*256*256)) >> 4;
        case    5:  return (buffer[1] + buffer[2]*256 + buffer[3]*(256*256) + buffer[4]*(256*256*256));
        default: assert(!"invalid encoding"); return 0;
        }
    }

    mdToken ReadMethodDefOrRef(BYTE *buffer)
    {
        LIMITED_METHOD_CONTRACT;

        static const mdToken tokenTypes[4] = { 0, mdtMethodDef, mdtMemberRef, mdtMethodSpec };
        unsigned encoding = ReadUnsigned(buffer);
        return TokenFromRid(tokenTypes[encoding & 0x3], encoding>>2);
    }

    unsigned GetParentOfMethodSpec(Module *module, mdMethodSpec methodSpecToken)
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pMDImport = module->GetMDImport();
        unsigned parentToken = 0;
        PCCOR_SIGNATURE pSig = 0;
        ULONG cbSig = 0;
        if (SUCCEEDED(pMDImport->GetMethodSpecProps(methodSpecToken, &parentToken, &pSig, &cbSig)))
        {
            return parentToken;
        }

        // this could be a new token injected by the compilation process - in this case
        // it HAS to be local to the current module
        assert(module == m_pModule);

        COUNT_T methodSpecRid = RidFromToken(methodSpecToken);
        COUNT_T offs = m_pZapImage->m_methodSpecToOffs[methodSpecRid];
        BYTE *buffer = &m_pZapImage->m_compactLayoutBuffer[offs];
        return ReadMethodDefOrRef(buffer);
    }

    PCCOR_SIGNATURE GetSigOfMethodSpec(Module *module, mdMethodSpec methodSpecToken, ULONG &cbSig)
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pMDImport = module->GetMDImport();
        unsigned parentToken = 0;
        PCCOR_SIGNATURE pSig = 0;
        if (SUCCEEDED(pMDImport->GetMethodSpecProps(methodSpecToken, &parentToken, &pSig, &cbSig)))
        {
            return pSig;
        }

        // this could be a new token injected by the compilation process - in this case
        // it HAS to be local to the current module
        assert(module == m_pModule);

        return m_tokenToSig.Get(methodSpecToken, cbSig);
    }

    void FillInlineContext(InlineContext *inlineContext, InlineContext *outerContext, unsigned methodOrFieldToken, unsigned constraintTypeToken)
    {
        STANDARD_VM_CONTRACT;

        Module *module = outerContext == NULL ? m_pModule : outerContext->GetModule();
        IMDInternalImport *pMDImport = module ->GetMDImport();
        unsigned parentToken = 0;
        _ASSERTE(inlineContext->IsTrivial());
        switch (TypeFromToken(methodOrFieldToken))
        {
        case    mdtMethodDef:
            break;

        case    mdtFieldDef:
            break;

        case    mdtMemberRef:
            parentToken = GetParentOfMemberRef((CORINFO_MODULE_HANDLE)module, methodOrFieldToken);
            break;

        case    mdtMethodSpec:
            {
                parentToken = GetParentOfMethodSpec(module, methodOrFieldToken);
                ULONG cbSig = 0;
                PCCOR_SIGNATURE pSig = GetSigOfMethodSpec(module, methodOrFieldToken, cbSig);
                SigPointer sp(pSig, cbSig);
                BYTE callingConvention;
                sp.GetByte(&callingConvention);
                assert(callingConvention == IMAGE_CEE_CS_CALLCONV_GENERICINST);
                ULONG typeArgCount;
                sp.GetData(&typeArgCount);
                _ASSERTE(typeArgCount < InlineContext::MAX_TYPE_ARGS);
                inlineContext->methodTypeArgCount = typeArgCount;
                inlineContext->methodTypeArgs = new InlineContext::TypeArg[typeArgCount];
                for (ULONG typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
                {
                    inlineContext->methodTypeArgs[typeArgIndex].pSig = sp.GetPtr();
                    sp.SkipExactlyOne();
                    inlineContext->methodTypeArgs[typeArgIndex].cbSig = sp.GetPtr() - inlineContext->methodTypeArgs[typeArgIndex].pSig;
                }
                switch (TypeFromToken(parentToken))
                {
                case    mdtMethodDef:
                    break;

                case    mdtMemberRef:
                    parentToken = GetParentOfMemberRef((CORINFO_MODULE_HANDLE)module, parentToken);
                    break;

                default:
                    assert(!"bad token type");
                    return;
                }
            }
            break;
        }

        if (constraintTypeToken != 0)
        {
            // Rationale: we will only inline a method different from the declared
            // method if the definition/implementation is in a value type,
            // otherwise we let the normal virtual or interface dispatch happen
            // (see MethodTable::TryResolveConstraintMethodApprox)
            //
            // if the constraint type is a value type, then the
            // method we're inlining is defined in the value type
            //
            // this only matters if this is a generic value type instantiation
            // if the implementation is from a non-generic value type,
            // the instantiation type arguments will not matter, because there can be no references to them

            if (TypeFromToken(constraintTypeToken) == mdtTypeSpec)
            {
                ULONG cbctSig;
                PCCOR_SIGNATURE pctSig = GetSigOfTypeSpec(module, constraintTypeToken, cbctSig);

                SigParser sp((PCCOR_SIGNATURE)pctSig, cbctSig);

                CorElementType elType;
                sp.GetElemType(&elType);
                if (elType == ELEMENT_TYPE_GENERICINST)
                {
                    sp.GetElemType(&elType);
                    assert(elType == ELEMENT_TYPE_CLASS || elType == ELEMENT_TYPE_VALUETYPE);
                    if (elType == ELEMENT_TYPE_VALUETYPE)
                       parentToken = constraintTypeToken;
                }
            }
        }

        if (TypeFromToken(parentToken) == mdtTypeSpec)
        {
            ULONG cbtSig;
            PCCOR_SIGNATURE ptSig = GetSigOfTypeSpec(module, parentToken, cbtSig);

            SigParser sp((PCCOR_SIGNATURE)ptSig, cbtSig);

            CorElementType elType;
            sp.GetElemType(&elType);
            if (elType == ELEMENT_TYPE_GENERICINST)
            {
                sp.GetElemType(&elType);
                assert(elType == ELEMENT_TYPE_CLASS || elType == ELEMENT_TYPE_VALUETYPE);
                sp.GetToken(&parentToken);
                // skip the instantiation arguments
                ULONG typeArgCount;
                sp.GetData(&typeArgCount);
                _ASSERTE(typeArgCount < InlineContext::MAX_TYPE_ARGS);
                inlineContext->classTypeArgCount = typeArgCount;
                inlineContext->classTypeArgs = new InlineContext::TypeArg[typeArgCount];
                for (ULONG typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
                {
                    inlineContext->classTypeArgs[typeArgIndex].pSig = sp.GetPtr();
                    sp.SkipExactlyOne();
                    inlineContext->classTypeArgs[typeArgIndex].cbSig = sp.GetPtr() - inlineContext->classTypeArgs[typeArgIndex].pSig;
                }
            }
        }
    }

    PCCOR_SIGNATURE GetSigOfMemberRef(Module *module, mdMemberRef memberRefToken, ULONG &cbSig)
    {
        STANDARD_VM_CONTRACT;

        LPCSTR szName_Ignore;
        IMDInternalImport *pMDImport = module->GetMDImport();
        PCCOR_SIGNATURE pSig = 0;
        if (FAILED(pMDImport->GetNameAndSigOfMemberRef(memberRefToken, &pSig, &cbSig, &szName_Ignore)))
        {
            // this could be a new token injected by the compilation process - in this case
            // it HAS to be local to the current module
            assert(module == m_pModule);

            pSig = m_tokenToSig.Get(memberRefToken, cbSig);
            assert(pSig != NULL);
        }
        return pSig;
    }

    virtual 
    mdToken GetTypeTokenForFieldOrMethod(mdToken fieldOrMethodToken)
    {
        STANDARD_VM_CONTRACT;

        Module *module = m_pModule;
        IMDInternalImport *pMDImport = module->GetMDImport();
        PCCOR_SIGNATURE pSig = 0;
        ULONG cbSig = 0;
        unsigned parentToken = 0;
        InlineContext context(m_pModule);

        FillInlineContext(&context, NULL, fieldOrMethodToken, 0);
        switch (TypeFromToken(fieldOrMethodToken))
        {
        case    mdtMethodDef:
            IfFailThrow(pMDImport->GetSigOfMethodDef(fieldOrMethodToken, &cbSig, &pSig));
            break;

        case    mdtFieldDef:
            IfFailThrow(pMDImport->GetSigOfFieldDef(fieldOrMethodToken, &cbSig, &pSig));
            break;

        case    mdtMemberRef:
            pSig = GetSigOfMemberRef(module, fieldOrMethodToken, cbSig);
            parentToken = GetParentOfMemberRef((CORINFO_MODULE_HANDLE)module, fieldOrMethodToken);
            break;

        case    mdtMethodSpec:
            {
                parentToken = GetParentOfMethodSpec(module, fieldOrMethodToken);

                switch (TypeFromToken(parentToken))
                {
                case    mdtMethodDef:
                    IfFailThrow(pMDImport->GetSigOfMethodDef(parentToken, &cbSig, &pSig));
                    break;

                case    mdtMemberRef:
                    pSig = GetSigOfMemberRef(module, parentToken, cbSig);
                    parentToken = GetParentOfMemberRef((CORINFO_MODULE_HANDLE)module, parentToken);
                    break;

                default:
                    assert(!"bad token type");
                    return 0;
                }
                break;
            }

        default:
            assert(!"bad token type");
            return 0;
        }

        BYTE *sigPtr = (BYTE *)pSig;
        BYTE callingConvention = *sigPtr++;
        pSig = (PCCOR_SIGNATURE)sigPtr;
        if (callingConvention == IMAGE_CEE_CS_CALLCONV_FIELD)
        {
            // nothing to do here...
        }
        else
        {
            if (callingConvention & IMAGE_CEE_CS_CALLCONV_GENERIC)
            {
                // uncompress number of generic type args
                CorSigUncompressData(pSig);
            }
            // uncompress number of args
            CorSigUncompressData(pSig);
        }

        return GetTokenForType(&context, (CORINFO_ARG_LIST_HANDLE)pSig);
    }

    virtual
    mdToken GetEnclosingClassToken(InlineContext *inlineContext, CORINFO_METHOD_HANDLE methHnd)
    {
        STANDARD_VM_CONTRACT;

        MethodDesc *methodDesc = (MethodDesc *)methHnd;
        assert(methodDesc->GetModule() == (inlineContext != NULL ? inlineContext->m_inlineeModule : m_pModule));
        MethodTable *pMT = methodDesc->GetMethodTable();

        unsigned typeToken = GetToken(pMT);
        if (!pMT->HasInstantiation())
            return typeToken;

        // encode type<!0,!1,...> and get a typespec token for it
        SigBuilder sb;
        sb.AppendElementType(ELEMENT_TYPE_GENERICINST);
        CorElementType elType = pMT->IsValueType() ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS;
        sb.AppendElementType(elType);
        sb.AppendToken(TranslateToken(inlineContext, typeToken));
        sb.AppendData(pMT->GetNumGenericArgs());
        Instantiation instantiation = pMT->GetInstantiation();
        for (DWORD i = 0; i < instantiation.GetNumArgs(); i++)
        {
            if (inlineContext != NULL && inlineContext->classTypeArgCount != 0)
            {
                _ASSERTE(i < inlineContext->classTypeArgCount);

                // a type argument to be expanded
                SigParser argSp(inlineContext->classTypeArgs[i].pSig, (DWORD)inlineContext->classTypeArgs[i].cbSig);
                expandSignature(argSp, NULL, sb);
            }
            else
            {
                sb.AppendElementType(ELEMENT_TYPE_VAR);
                sb.AppendData(i);
            }
        }
        DWORD cbSig;
        PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sb.GetSignature(&cbSig);

        return GetTypeSpecToken(pSig, cbSig);
    }

    virtual
    mdToken GetCurrentMethodToken(InlineContext *inlineContext, CORINFO_METHOD_HANDLE methHnd)
    {
        STANDARD_VM_CONTRACT;

        MethodDesc *methodDesc = (MethodDesc *)methHnd;
        MethodTable *methodTable = methodDesc->GetMethodTable();
        Module *methodModule = methodTable->GetModule();
        unsigned methodToken = methodDesc->GetMemberDef();
        if (!methodDesc->HasClassOrMethodInstantiation() && methodModule == m_pModule)
            return methodToken;
        unsigned enclosingClassToken = GetEnclosingClassToken(inlineContext, methHnd);
        if (TypeFromToken(enclosingClassToken) != mdtTypeDef)
        {
            HENUMInternalHolder hEnumMethodDef(methodModule->GetMDImport());
            hEnumMethodDef.EnumInit(mdtMethodDef, methodTable->GetCl());
            ULONG methodCount = hEnumMethodDef.EnumGetCount();
            mdMethodDef firstMethodDefToken;
            hEnumMethodDef.EnumNext(&firstMethodDefToken);
            ULONG ordinal = methodToken - firstMethodDefToken;
            assert(ordinal < methodCount);
            methodToken = FindOrCreateExtMemberRef(enclosingClassToken, FALSE, ordinal, methodDesc->GetNameOnNonArrayClass(), methodModule, methodToken);
        }
        if (!methodDesc->HasMethodInstantiation())
            return methodToken;

        // encode methodToken<!0,!1,...> and get a methodspec token for it
        SigBuilder sb;
        sb.AppendByte(IMAGE_CEE_CS_CALLCONV_GENERICINST);
        sb.AppendData(methodDesc->GetNumGenericMethodArgs());
        Instantiation instantiation = methodDesc->GetMethodInstantiation();
        for (DWORD i = 0; i < methodDesc->GetNumGenericMethodArgs(); i++)
        {
            if (inlineContext != NULL && inlineContext->methodTypeArgCount != 0)
            {
                _ASSERTE(i < inlineContext->methodTypeArgCount);

                // a type argument to be expanded
                SigParser argSp(inlineContext->methodTypeArgs[i].pSig, (DWORD)inlineContext->methodTypeArgs[i].cbSig);
                expandSignature(argSp, NULL, sb);
            }
            else
            {
                sb.AppendElementType(ELEMENT_TYPE_MVAR);
                sb.AppendData(i);
            }
        }

        return FindOrCreateMethodSpec(methodToken, sb);
    }

    virtual
    bool IsDynamicScope(CORINFO_MODULE_HANDLE scope)
    {
        STANDARD_VM_CONTRACT;
        return ::IsDynamicScope(scope);
    }

    virtual
    InlineContext *ComputeInlineContext(InlineContext *outerContext, unsigned inlinedMethodToken, unsigned constraintTypeToken, CORINFO_METHOD_HANDLE methHnd)
    {
        STANDARD_VM_CONTRACT;

        InlineContext inlineContext(m_pModule);

        FillInlineContext(&inlineContext, outerContext, inlinedMethodToken, constraintTypeToken);

        MethodDesc *inlineeMethod = (MethodDesc *)methHnd;
        inlineContext.m_inlineeModule = inlineeMethod->GetModule();

        if (inlineContext.IsTrivial())
            return NULL;
        else
            return new InlineContext(inlineContext);
    }

    ULONG GetFieldOrdinal(Module *module, mdTypeDef parentToken, mdFieldDef fieldDefToken)
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pMDImport = module->GetMDImport();
        HENUMInternalHolder hEnumFieldDef(pMDImport);
        hEnumFieldDef.EnumInit(mdtFieldDef, parentToken);
        ULONG fieldCount = hEnumFieldDef.EnumGetCount();
        mdFieldDef firstFieldDefToken;
        while (hEnumFieldDef.EnumNext(&firstFieldDefToken))
        {
            DWORD fieldAttr;
            IfFailThrow(pMDImport->GetFieldDefProps(firstFieldDefToken, &fieldAttr));
            if (!IsFdLiteral(fieldAttr))
                break;
        }
        ULONG ordinal = fieldDefToken - firstFieldDefToken;
        assert(ordinal < fieldCount);
        return ordinal;
    }

    virtual DWORD GetFieldOrdinal(CORINFO_MODULE_HANDLE tokenScope, unsigned fieldToken)
    {
        STANDARD_VM_CONTRACT;

        assert(TypeFromToken(fieldToken) == mdtFieldDef);
        Module *module = GetModule(tokenScope);
        mdToken parentToken;
        IMDInternalImport *pMDImport = module->GetMDImport();
        IfFailThrow(pMDImport->GetParentToken(fieldToken, &parentToken));
        return GetFieldOrdinal(module, parentToken, fieldToken);
    }

    ULONG GetMethodOrdinal(Module *module, mdTypeDef parentToken, mdMethodDef methodDefToken)
    {
        STANDARD_VM_CONTRACT;

        assert(parentToken != 0);
        HENUMInternalHolder hEnumMethodDef(module->GetMDImport());
        hEnumMethodDef.EnumInit(mdtMethodDef, parentToken);
        ULONG methodCount = hEnumMethodDef.EnumGetCount();
        mdMethodDef firstMethodDefToken;
        hEnumMethodDef.EnumNext(&firstMethodDefToken);
        ULONG ordinal = methodDefToken - firstMethodDefToken;
        assert(ordinal < methodCount);
        return ordinal;
    }

    void SetTranslatedSig(Module *module, mdToken translatedToken, PCCOR_SIGNATURE pOrgSig, ULONG cbOrgSig)
    {
        STANDARD_VM_CONTRACT;

        InlineContext inlineContext(m_pModule);
        inlineContext.m_inlineeModule = module;

        SigBuilder sigBuilder;
        expandFieldOrMethodSignature(pOrgSig, cbOrgSig, &inlineContext, sigBuilder);
        ULONG cbTranslatedSig;
        PCCOR_SIGNATURE pTranslatedSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cbTranslatedSig);
        m_tokenToSig.Set(translatedToken, pTranslatedSig, cbTranslatedSig);
    }

    virtual
    unsigned TranslateToken(InlineContext *inlineContext, mdToken token)
    {
        STANDARD_VM_CONTRACT;

        Module *module = inlineContext == NULL ? m_pModule : inlineContext->GetModule();
        IMDInternalImport *pMDImport = module->GetMDImport();

        switch (TypeFromToken(token))
        {
        case    mdtTypeDef:
            {
                if (module == m_pModule)
                    return token;

                EEClass *eeClass = LoadTypeDef(module, token);
                assert(eeClass != 0);
                MethodTable *pMT = eeClass->GetMethodTable();

                ULONG typeRefRid = FindOrCreateExtTypeRef(pMT);
                return TokenFromRid(mdtTypeRef, typeRefRid);
            }
            break;

        case    mdtFieldDef:
            {
                if (module == m_pModule)
                    return token;
                mdToken parentToken;
                IfFailThrow(pMDImport->GetParentToken(token, &parentToken));
                mdToken translatedParentToken = TranslateToken(inlineContext, parentToken);
                ULONG fieldOrdinal = GetFieldOrdinal(module, parentToken, token);

                LPCUTF8 pszName;
                IfFailThrow(pMDImport->GetNameOfFieldDef(token, &pszName));

                mdToken translatedToken = FindOrCreateExtMemberRef(translatedParentToken, true, fieldOrdinal, pszName, module, token);
                ULONG cbSig;
                PCCOR_SIGNATURE pSig;
                IfFailThrow(pMDImport->GetSigOfFieldDef(token, &cbSig, &pSig));
                SetTranslatedSig(module, translatedToken, pSig, cbSig);
                return translatedToken;
            }

        case    mdtMethodDef:
            {
                if (module == m_pModule)
                    return token;
                mdToken parentToken;
                IfFailThrow(pMDImport->GetParentToken(token, &parentToken));
                mdToken translatedParentToken = TranslateToken(inlineContext, parentToken);
                ULONG methodOrdinal = GetMethodOrdinal(module, parentToken, token);

                LPCUTF8 pszName;
                IfFailThrow(pMDImport->GetNameOfMethodDef(token, &pszName));

                mdToken translatedToken = FindOrCreateExtMemberRef(translatedParentToken, false, methodOrdinal, pszName, module, token);
                ULONG cbSig;
                PCCOR_SIGNATURE pSig;
                IfFailThrow(pMDImport->GetSigOfMethodDef(token, &cbSig, &pSig));
                SetTranslatedSig(module, translatedToken, pSig, cbSig);
                return translatedToken;
            }

        case    mdtTypeRef:
            {
                if (module == m_pModule)
                    return token;
                EEClass *eeClass = LoadTypeRef(module, token);
                assert(eeClass != 0);
                MethodTable *pMT = eeClass->GetMethodTable();
                if (pMT->GetModule() == m_pModule)
                {
                    // if this happens to be from our own module, we can just
                    // return the typedef token
                    return pMT->GetCl();
                }
                else
                {
                    ULONG typeRefRid = FindOrCreateExtTypeRef(pMT);
                    return TokenFromRid(mdtTypeRef, typeRefRid);
                }
            }

        case    mdtSignature:
            // we don't know how to translate these if they are from another module
            assert(module == m_pModule);
            return token;

        case    mdtMemberRef:
            {
                mdToken parentToken = GetParentOfMemberRef((CORINFO_MODULE_HANDLE)module, token);
                mdToken translatedParentToken = TranslateToken(inlineContext, parentToken);
                if (translatedParentToken == parentToken && module == m_pModule)
                    return token;
                ULONG cbSig;
                PCCOR_SIGNATURE pSig;
                LPCSTR szName;
                IfFailThrow(pMDImport->GetNameAndSigOfMemberRef(token, &pSig, &cbSig, &szName));
                assert(cbSig >= 2);
                SigPointer sigPointer(pSig, cbSig);
                ULONG callingConv;
                sigPointer.GetCallingConv(&callingConv);

                ULONG memberOrdinal;
                mdToken tkMemberDefToken;
                Module *pModuleMemberDef;

                if (callingConv != IMAGE_CEE_CS_CALLCONV_FIELD)
                {
                    MethodDesc *method = LoadMethod(module, token);
                    assert(method != NULL);
                    EEClass *eeClass = method->GetClass();
                    Module *methodModule = pModuleMemberDef = eeClass->GetMethodTable()->GetModule();
                    mdToken methodDefToken = tkMemberDefToken = method->GetMemberDef();
                    // if this happens to be in a typedef from our own module, we can just
                    // return the methoddef token
                    if (TypeFromToken(translatedParentToken) == mdtTypeDef)
                        return methodDefToken;
                    mdToken typeDefToken = eeClass->GetMethodTable()->GetCl();
                    memberOrdinal = GetMethodOrdinal(methodModule, typeDefToken, methodDefToken);
                }
                else
                {
                    FieldDesc *field = LoadField(module, token);
                    assert(field != NULL);
                    EEClass *eeClass = field->GetApproxEnclosingMethodTable()->GetClass();
                    Module *fieldModule = pModuleMemberDef = eeClass->GetMethodTable()->GetModule();
                    mdToken fieldDefToken = tkMemberDefToken = field->GetMemberDef();
                    // if this happens to be in a typedef from our own module, we can just
                    // return the fielddef token
                    if (TypeFromToken(translatedParentToken) == mdtTypeDef)
                        return fieldDefToken;
                    mdToken typeDefToken = eeClass->GetMethodTable()->GetCl();
                    memberOrdinal = GetFieldOrdinal(fieldModule, typeDefToken, fieldDefToken);
                }
                mdToken translatedToken = FindOrCreateExtMemberRef(translatedParentToken, callingConv == IMAGE_CEE_CS_CALLCONV_FIELD, memberOrdinal, szName, pModuleMemberDef, tkMemberDefToken);
                SetTranslatedSig(module, translatedToken, pSig, cbSig);
                return translatedToken;
            }
            break;

        case    mdtTypeSpec:
            {
                ULONG cbSig;
                PCCOR_SIGNATURE pSig = GetSigOfTypeSpec(module, token, cbSig);

                SigBuilder sigBuilder;

                // expand the signature plugging in the type arguments
                SigParser sp(pSig, cbSig);
                expandSignature(sp, inlineContext, sigBuilder);

                pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cbSig);

                mdToken translatedToken = GetTypeSpecToken(pSig, cbSig);

                return translatedToken;
            }
            break;

        case    mdtMethodSpec:
            {
                mdToken parentToken = GetParentOfMethodSpec(module, token);
                mdToken translatedParentToken = TranslateToken(inlineContext, parentToken);
                ULONG cbSig;
                PCCOR_SIGNATURE pSig = GetSigOfMethodSpec(module, token, cbSig);
                SigBuilder sigBuilder;
                SigParser sp(pSig, cbSig);
                BYTE b;
                sp.GetByte(&b);
                assert(b == IMAGE_CEE_CS_CALLCONV_GENERICINST);
                sigBuilder.AppendByte(b);
                ULONG typeArgCount;
                sp.GetData(&typeArgCount);
                sigBuilder.AppendData(typeArgCount);
                for (ULONG typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
                    expandSignature(sp, inlineContext, sigBuilder);
                mdToken translatedToken = FindOrCreateMethodSpec(translatedParentToken, sigBuilder);
                return translatedToken;
            }
            break;

        default:
            _ASSERTE(!"Unexpected token type");
            break;
        }
        UNREACHABLE();
    }

    virtual
    CorInfoType GetFieldElementType(unsigned fieldToken, CORINFO_MODULE_HANDLE scope, CORINFO_METHOD_HANDLE methHnd, ICorJitInfo *info)
    {
        STANDARD_VM_CONTRACT;

        Module *module = GetModule(scope);
        
        IMDInternalImport *pMDImport = module->GetMDImport();
        PCCOR_SIGNATURE pSig = 0;
        ULONG cbSig = 0;
        InlineContext context(m_pModule);

        SigBuilder sigBuilder;
        FillInlineContext(&context, NULL, fieldToken, 0);
        if (TypeFromToken(fieldToken) == mdtTypeSpec)
        {
            pSig = GetSigOfTypeSpec(module, fieldToken, cbSig);
        }
        else
        {
            if (TypeFromToken(fieldToken) == mdtFieldDef)
                IfFailThrow(pMDImport->GetSigOfFieldDef(fieldToken, &cbSig, &pSig));
            else
            {
                assert(TypeFromToken(fieldToken) == mdtMemberRef);
                pSig = GetSigOfMemberRef(module, fieldToken, cbSig);
            }
            assert(cbSig >= 2);
            BYTE *sigPtr = (BYTE *)pSig;
            BYTE callingConvention = *sigPtr++;
            cbSig--;
            pSig = (PCCOR_SIGNATURE)sigPtr;
            assert(callingConvention == IMAGE_CEE_CS_CALLCONV_FIELD);
        }
        if (!context.IsTrivial())
        {
            // expand the signature plugging in the type arguments
            SigParser sp1(pSig, cbSig);
            expandSignature(sp1, &context, sigBuilder);

            pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cbSig);
        }
        SigParser sp(pSig, cbSig);
        CorElementType elType;
        sp.GetElemType(&elType);
        while (elType == ELEMENT_TYPE_CMOD_REQD || elType == ELEMENT_TYPE_CMOD_OPT)
        {
            mdToken token;
            sp.GetToken(&token);
            sp.GetElemType(&elType);
        }
        while (true)
        {
            switch (elType)
            {
            case    ELEMENT_TYPE_END:       return  CORINFO_TYPE_UNDEF;
            case    ELEMENT_TYPE_VOID:      return  CORINFO_TYPE_VOID;
            case    ELEMENT_TYPE_BOOLEAN:   return  CORINFO_TYPE_BOOL;
            case    ELEMENT_TYPE_CHAR:      return  CORINFO_TYPE_BYTE;
            case    ELEMENT_TYPE_I1:        return  CORINFO_TYPE_UBYTE;
            case    ELEMENT_TYPE_U1:        return  CORINFO_TYPE_UBYTE;
            case    ELEMENT_TYPE_I2:        return  CORINFO_TYPE_SHORT;
            case    ELEMENT_TYPE_U2:        return  CORINFO_TYPE_USHORT;
            case    ELEMENT_TYPE_I4:        return  CORINFO_TYPE_INT;
            case    ELEMENT_TYPE_U4:        return  CORINFO_TYPE_UINT;
            case    ELEMENT_TYPE_I8:        return  CORINFO_TYPE_LONG;
            case    ELEMENT_TYPE_U8:        return  CORINFO_TYPE_ULONG;
            case    ELEMENT_TYPE_R4:        return  CORINFO_TYPE_FLOAT;
            case    ELEMENT_TYPE_R8:        return  CORINFO_TYPE_DOUBLE;
            case    ELEMENT_TYPE_I:         return  CORINFO_TYPE_NATIVEINT;
            case    ELEMENT_TYPE_U:         return  CORINFO_TYPE_NATIVEUINT;

            case    ELEMENT_TYPE_STRING:
            case    ELEMENT_TYPE_CLASS:
            case    ELEMENT_TYPE_ARRAY:
            case    ELEMENT_TYPE_OBJECT:
            case    ELEMENT_TYPE_SZARRAY:   return  CORINFO_TYPE_CLASS;

            case    ELEMENT_TYPE_PTR:
            case    ELEMENT_TYPE_FNPTR:     return  CORINFO_TYPE_PTR;

            case    ELEMENT_TYPE_BYREF:     return  CORINFO_TYPE_BYREF;

            case    ELEMENT_TYPE_TYPEDBYREF:return  CORINFO_TYPE_REFANY;

            case    ELEMENT_TYPE_VALUETYPE:
                {
                    mdToken typeToken;
                    sp.GetToken(&typeToken);
                    assert(TypeFromToken(typeToken) == mdtTypeDef || TypeFromToken(typeToken) == mdtTypeRef);
                    EEClass *eeClass = LoadTypeRef(module, typeToken);
                    if (eeClass != NULL && eeClass->GetMethodTable()->IsEnum())
                    {
                        elType = eeClass->GetInternalCorElementType();
                        continue;
                    }
                }
                return  CORINFO_TYPE_VALUECLASS;

            case    ELEMENT_TYPE_GENERICINST:
                sp.GetElemType(&elType);
                return elType == ELEMENT_TYPE_CLASS ? CORINFO_TYPE_CLASS : CORINFO_TYPE_VALUECLASS;

            case    ELEMENT_TYPE_VAR:
            case    ELEMENT_TYPE_MVAR:
                {
                    ULONG argIndex;
                    sp.GetData(&argIndex);
                    CORINFO_CLASS_HANDLE typeParameter = info->getTypeParameter(methHnd, elType == ELEMENT_TYPE_VAR, argIndex);
                    return info->asCorInfoType(typeParameter);
                }

            default:
                assert(!"unexpected element type");
                return CORINFO_TYPE_NATIVEINT;
            }
            break;
        }
    }

    virtual
    mdToken GetNextStubToken()
    {
        STANDARD_VM_CONTRACT;

        if (RidFromToken(m_prevStubDefToken) == 0xFFFFFF)
        {
            // we ran out of methoddefs
            return mdMethodDefNil;
        }
        return ++m_prevStubDefToken;
    }

    virtual
    void Flush()
    {
        STANDARD_VM_CONTRACT;

        CopyUserStringPool();
    }

    virtual
    void FlushStubData()
    {
        STANDARD_VM_CONTRACT;

        size_t size, sizeSize, assocSize;
        BYTE *buffer = m_stream->GetBuffer(size);
        BYTE *assocBuffer = m_stubAssocStream->GetBuffer(assocSize);

        m_stream->WriteUnsigned(m_stubMethodCount);
        m_stream->GetBuffer(sizeSize);

        m_pZapImage->FlushStubData(buffer + size,
                                   (COUNT_T)(sizeSize - size),
                                   buffer, (COUNT_T)size,
                                   assocBuffer, (COUNT_T)assocSize);
    }

    void CopyUserStringPool()
    {
        STANDARD_VM_CONTRACT;
#ifdef REDHAWK
        IMDInternalImport *pMDImport = m_pModule->GetMDImport();
        void *table;
        size_t tableSize;
        pMDImport->GetTableInfoWithIndex(TBL_COUNT + MDPoolUSBlobs, &table, (void **)&tableSize);
//        printf("User string pool at %p taking %u bytes\n", pTable, pTableSize);
        m_pZapImage->FlushUserStringPool((BYTE *)table, (ULONG)tableSize);
#endif
    }
};

// static
ICompactLayoutWriter *ICompactLayoutWriter::MakeCompactLayoutWriter(Module *pModule, ZapImage *pZapImage)
{
    STANDARD_VM_CONTRACT;

    if (pZapImage->DoCompactLayout())
    {
        CompactLayoutWriter *compactLayoutWriter = new CompactLayoutWriter(pModule, pZapImage);
        compactLayoutWriter->CreateExternalReferences();

        pZapImage->SetCompactLayoutWriter(compactLayoutWriter);

        return compactLayoutWriter;
    }
    else
    {
        return NULL;
    }
}
