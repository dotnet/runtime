// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
 /*vim: set foldmethod=marker: */
#include <stdafx.h>

#if defined(FEATURE_PREJIT)
#include "nidump.h"

#include <metadataexports.h>

#include <comcallablewrapper.h>
#include <gcdump.h>
#include <fieldmarshaler.h>

#if !defined(FEATURE_CORESYSTEM)
#include <algorithm>
#endif


#include <formattype.h>

#include <pedecoder.h>


#include <mdfileformat.h>

#if !defined(FEATURE_CORESYSTEM)
#include <cassert>
#undef _ASSERTE
#define _ASSERTE(x) assert(x)
#endif

#include <compile.h>

#ifdef USE_GC_INFO_DECODER
#include <gcinfodecoder.h>
#endif

#include <ngenhash.inl>

#define FEATURE_MSDIS

//----------------------------------------------------------------------------
//
// ClrDump functionality
//
//----------------------------------------------------------------------------

/////////////////////////////////////////////////////////////////////////////////////////////
//
// Given a compressed integer(*pData), expand the compressed int to *pDataOut.
// Return value is the number of bytes that the integer occupies in the compressed format
// It is caller's responsibility to ensure pDataOut has at least 4 bytes to be written to.
//
// This function returns -1 if pass in with an incorrectly compressed data, such as
// (*pBytes & 0xE0) == 0XE0.
/////////////////////////////////////////////////////////////////////////////////////////////
/* XXX Wed 09/14/2005
 * copied from cor.h.  Modified to operate on PTR_PCCOR_SIGNATUREs
 */
inline ULONG DacSigUncompressBigData(
    PTR_CCOR_SIGNATURE &pData)             // [IN,OUT] compressed data
{
    ULONG res;

    // 1 byte data is handled in DacSigUncompressData
    //  _ASSERTE(*pData & 0x80);

    // Medium.
    if ((*pData & 0xC0) == 0x80)  // 10?? ????
    {
        res = (ULONG)((*pData++ & 0x3f) << 8);
        res |= *pData++;
    }
    else // 110? ????
    {
        res = (*pData++ & 0x1f) << 24;
        res |= *pData++ << 16;
        res |= *pData++ << 8;
        res |= *pData++;
    }
    return res;
}
FORCEINLINE ULONG DacSigUncompressData(
    PTR_CCOR_SIGNATURE &pData)             // [IN,OUT] compressed data
{
    // Handle smallest data inline.
    if ((*pData & 0x80) == 0x00)        // 0??? ????
        return *pData++;
    return DacSigUncompressBigData(pData);
}
//const static mdToken g_tkCorEncodeToken[4] ={mdtTypeDef, mdtTypeRef, mdtTypeSpec, mdtBaseType};

// uncompress a token
inline mdToken DacSigUncompressToken(   // return the token.
    PTR_CCOR_SIGNATURE &pData)             // [IN,OUT] compressed data
{
    mdToken     tk;
    mdToken     tkType;

    tk = DacSigUncompressData(pData);
    tkType = g_tkCorEncodeToken[tk & 0x3];
    tk = TokenFromRid(tk >> 2, tkType);
    return tk;
}
// uncompress encoded element type
FORCEINLINE CorElementType DacSigUncompressElementType(//Element type
    PTR_CCOR_SIGNATURE &pData)             // [IN,OUT] compressed data
{
    return (CorElementType)*pData++;
}



const char * g_helperNames[] =
{
#define JITHELPER(val, fn, sig) # val,
#include <jithelpers.h>
#undef JITHELPER
};



#define dim(x) (sizeof(x)/sizeof((x)[0]))

void EnumFlagsToString( DWORD value,
                        const NativeImageDumper::EnumMnemonics * table,
                        int count, const WCHAR * sep, SString& output )
{
    bool firstValue = true;
    for( int i = 0; i < count; ++i )
    {
        bool match = false;
        const NativeImageDumper::EnumMnemonics& entry = table[i];
        if( entry.mask != 0 )
            match = ((entry.mask & value) == entry.value);
        else
            match = (entry.value == value);

        if( match )
        {
            if( !firstValue )
                output.Append(sep);
            firstValue = false;

            output.Append( table[i].mnemonic );

            value &= ~entry.value;
        }
    }
}

const NativeImageDumper::EnumMnemonics s_ImageSections[] =
{
#define IS_ENTRY(v, s) NativeImageDumper::EnumMnemonics(v, s)
    IS_ENTRY(IMAGE_SCN_MEM_READ, W("read")),
    IS_ENTRY(IMAGE_SCN_MEM_WRITE, W("write")),
    IS_ENTRY(IMAGE_SCN_MEM_EXECUTE, W("execute")),
    IS_ENTRY(IMAGE_SCN_CNT_CODE, W("code")),
    IS_ENTRY(IMAGE_SCN_CNT_INITIALIZED_DATA, W("init data")),
    IS_ENTRY(IMAGE_SCN_CNT_UNINITIALIZED_DATA, W("uninit data")),
#undef IS_ENTRY
};
inline int CheckFlags( DWORD source, DWORD flags )
{
    return (source & flags) == flags;
}

HRESULT ClrDataAccess::DumpNativeImage(CLRDATA_ADDRESS loadedBase,
                                       LPCWSTR name,
                                       IXCLRDataDisplay * display,
                                       IXCLRLibrarySupport * support,
                                       IXCLRDisassemblySupport *dis)
{
    DAC_ENTER();
    /* REVISIT_TODO Fri 09/09/2005
     * catch exceptions
     */
    NativeImageDumper dump(dac_cast<PTR_VOID>(CLRDATA_ADDRESS_TO_TADDR(loadedBase)), name, display,
                           support, dis);
    dump.DumpNativeImage();
    DAC_LEAVE();
    return S_OK;
}



static ULONG bigBufferSize = 8192;
static WCHAR bigBuffer[8192];
static BYTE bigByteBuffer[1024];

//----------------------------------------------------------------------------
//
// NativeImageDumper
//
//----------------------------------------------------------------------------
template<typename T>
inline T combine(T a, T b)
{
    return (T)(((DWORD)a) | ((DWORD)b));
}
template<typename T>
inline T combine(T a, T b, T c)
{
    return (T)(((DWORD)a) | ((DWORD)b) | ((DWORD)c));
}
#define CLRNATIVEIMAGE_ALWAYS ((CLRNativeImageDumpOptions)~0)
#define CHECK_OPT(opt) CheckOptions(CLRNATIVEIMAGE_ ## opt)
#define IF_OPT(opt) if( CHECK_OPT(opt) )
#define IF_OPT_AND(opt1, opt2) if( CHECK_OPT(opt1) && CHECK_OPT(opt2) )
#define IF_OPT_OR(opt1, opt2) if( CHECK_OPT(opt1) || CHECK_OPT(opt2) )
#define IF_OPT_OR3(opt1, opt2, opt3) if( CHECK_OPT(opt1) || CHECK_OPT(opt2) || CHECK_OPT(opt3) )
#define IF_OPT_OR4(opt1, opt2, opt3, opt4) if( CHECK_OPT(opt1) || CHECK_OPT(opt2) || CHECK_OPT(opt3) || CHECK_OPT(opt4) )
#define IF_OPT_OR5(opt1, opt2, opt3, opt4, opt5) if( CHECK_OPT(opt1) || CHECK_OPT(opt2) || CHECK_OPT(opt3) || CHECK_OPT(opt4) || CHECK_OPT(opt5) )

#define fieldsize(type, field) (sizeof(((type*)NULL)->field))


/*{{{Display helpers*/
#define DisplayStartCategory(name, filter)\
    do { IF_OPT(filter) m_display->StartCategory(name); } while(0)
#define DisplayEndCategory(filter)\
    do { IF_OPT(filter) m_display->EndCategory(); } while(0)
#define DisplayStartArray(name, fmt, filter) \
    do { IF_OPT(filter) m_display->StartArray(name, fmt); } while(0)
#define DisplayStartArrayWithOffset(field, fmt, type, filter) \
    do { IF_OPT(filter) m_display->StartArrayWithOffset( # field, offsetof(type, field), fieldsize(type, field), fmt); } while(0)

#define DisplayStartElement( name, filter ) \
    do { IF_OPT(filter) m_display->StartElement( name ); } while(0)
#define DisplayStartStructure( name, ptr, size, filter ) \
    do { IF_OPT(filter) m_display->StartStructure( name, ptr, size ); } while(0)
#define DisplayStartList(fmt, filter) \
    do { IF_OPT(filter) m_display->StartList(fmt); } while(0)

#define DisplayStartStructureWithOffset( field, ptr, size, type, filter ) \
    do { IF_OPT(filter) m_display->StartStructureWithOffset( # field, offsetof(type, field), fieldsize(type, field), ptr, size ); } while(0)
#define DisplayStartVStructure( name, filter ) \
    do { IF_OPT(filter) m_display->StartVStructure( name ); } while(0)
#define DisplayEndVStructure( filter ) \
    do { IF_OPT(filter) m_display->EndVStructure(); } while(0)

#define DisplayEndList(filter) \
    do { IF_OPT(filter) m_display->EndList(); } while(0)
#define DisplayEndArray(footer, filter) \
    do { IF_OPT(filter) m_display->EndArray(footer); } while(0)
#define DisplayEndStructure(filter) \
    do { IF_OPT(filter) m_display->EndStructure(); } while(0)

#define DisplayEndElement(filter) \
    do { IF_OPT(filter) m_display->EndElement(); } while(0)

#define DisplayWriteElementString(name, value, filter) \
    do { IF_OPT(filter) m_display->WriteElementString(name, value); } while(0)
#define DisplayWriteElementStringW(name, value, filter) \
    do { IF_OPT(filter) m_display->WriteElementStringW(name, value); } while(0)

#define DisplayWriteElementStringW(name, value, filter) \
    do { IF_OPT(filter) m_display->WriteElementStringW(name, value); } while(0)

#define DisplayWriteElementInt(name, value, filter) \
    do { IF_OPT(filter) m_display->WriteElementInt(name, value); } while(0)
#define DisplayWriteElementIntWithSuppress(name, value, defVal, filter) \
    do { IF_OPT(filter) m_display->WriteElementIntWithSuppress(name, value, defVal); } while(0)
#define DisplayWriteElementUInt(name, value, filter) \
    do { IF_OPT(filter) m_display->WriteElementUInt(name, value); } while(0)
#define DisplayWriteElementFlag(name, value, filter) \
    do { IF_OPT(filter) m_display->WriteElementFlag(name, value); } while(0)
#define DisplayWriteElementPointer(name, value, filter) \
    do { IF_OPT(filter) m_display->WriteElementPointer(name, value); } while(0)
#define DisplayWriteElementPointerAnnotated(name, value, annotation, filter) \
    do { IF_OPT(filter) m_display->WriteElementPointerAnnotated(name, value, annotation ); } while(0)
#define DisplayWriteEmptyElement(name, filter) \
    do { IF_OPT(filter) m_display->WriteEmptyElement(name); } while(0)

#define DisplayWriteElementEnumerated(name, value, mnemonics, sep, filter) \
    do { \
    IF_OPT(filter) { \
        TempBuffer buf; \
        EnumFlagsToString(value, mnemonics, _countof(mnemonics), sep, buf);\
        m_display->WriteElementEnumerated( name, value, (const WCHAR*)buf ); \
    }\
    }while(0)
#define DisplayWriteFieldEnumerated(field, value, type, mnemonics, sep, filter)\
    do { \
    IF_OPT(filter) { \
        TempBuffer buf; \
        EnumFlagsToString(value, mnemonics, _countof(mnemonics), sep, buf);\
        m_display->WriteFieldEnumerated( # field, offsetof(type, field), \
                                         fieldsize(type, field), value,\
                                         (const WCHAR*)buf ); \
    }\
    }while(0)
#define DisplayWriteElementAddress(name, ptr, size, filter) \
    do { IF_OPT(filter) m_display->WriteElementAddress( name, ptr, size ); } while(0)
#define DisplayWriteElementAddressNamed(eltName, name, ptr, size, filter) \
    do { IF_OPT(filter) m_display->WriteElementAddressNamed( eltName, name, ptr, size ); } while(0)
#define DisplayWriteElementAddressNamedW(eltName, name, ptr, size, filter) \
    do { IF_OPT(filter) m_display->WriteElementAddressNamedW( eltName, name, ptr, size ); } while(0)

#define DisplayWriteFieldString(field, value, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldString( # field, offsetof(type, field), fieldsize(type, field), value ); } while(0)
#define DisplayWriteFieldStringW(field, value, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldStringW( # field, offsetof(type, field), fieldsize(type, field), value ); } while(0)
#define DisplayWriteFieldInt(field, value, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldInt( # field, offsetof(type, field), fieldsize(type, field), value ); } while(0)
#define DisplayWriteFieldUInt(field, value, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldUInt( # field, offsetof(type, field), fieldsize(type, field), value ); } while(0)
#define DisplayWriteFieldPointer(field, ptr, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldPointer( # field, offsetof(type, field), fieldsize(type, field), ptr ); } while(0)
#define DisplayWriteFieldPointerWithSize(field, ptr, size, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldPointerWithSize( # field, offsetof(type, field), fieldsize(type, field), ptr, size ); } while(0)
#define DisplayWriteFieldEmpty(field, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldEmpty( # field, offsetof(type, field), fieldsize(type, field) ); } while(0)
#define DisplayWriteFieldFlag(field, value, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldFlag(# field, offsetof(type, field), fieldsize(type, field), value); } while(0)
#define WriteFieldFieldDesc(field, ptr, type, filter) \
    do { IF_OPT(filter) DoWriteFieldFieldDesc( # field, offsetof(type, field), fieldsize(type, field), ptr ); } while(0)
#define WriteFieldMethodDesc(field, ptr, type, filter) \
    do { IF_OPT(filter) DoWriteFieldMethodDesc( # field, offsetof(type, field), fieldsize(type, field), ptr ); } while(0)
#define WriteFieldStr(field, ptr, type, filter) \
    do { IF_OPT(filter) DoWriteFieldStr( ptr, # field, offsetof(type, field), fieldsize(type, field) ); } while(0)
#define WriteFieldMethodTable(field, ptr, type, filter) \
    do { IF_OPT(filter) DoWriteFieldMethodTable( # field, offsetof(type, field), fieldsize(type, field), ptr ); } while(0)
#define WriteFieldMDToken(field, token, type, filter) \
    do { IF_OPT(filter) DoWriteFieldMDToken( # field, offsetof(type, field), fieldsize(type, field), token ); } while(0)
#define WriteFieldAsHex(field, ptr, type, filter) \
    do { IF_OPT(filter) DoWriteFieldAsHex( # field, offsetof(type, field), fieldsize(type, field), ptr, fieldsize(type, field)); } while(0)
#define WriteFieldMDTokenImport(field, token, type, filter, import) \
    do { IF_OPT(filter) DoWriteFieldMDToken( # field, offsetof(type, field), fieldsize(type, field), token, import); } while(0)
#define WriteFieldTypeHandle(field, ptr, type, filter) \
    do { IF_OPT(filter) DoWriteFieldTypeHandle( # field, offsetof(type, field), fieldsize(type, field), ptr ); } while(0)
#define WriteFieldCorElementType(field, et, type, filter) \
    do { IF_OPT(filter) DoWriteFieldCorElementType( # field, offsetof(type, field), fieldsize(type, field), et ); } while(0)
#define DumpFieldStub(field, ptr, type, filter) \
    do { IF_OPT(filter) DoDumpFieldStub( ptr, offsetof(type, field), fieldsize(type, field), # field ); } while(0)
#define DumpComPlusCallInfo(compluscall, filter) \
    do { IF_OPT(filter) DoDumpComPlusCallInfo( compluscall ); } while(0)
#define DisplayWriteFieldPointerAnnotated(field, ptr, annotation, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldPointer( # field, offsetof(type, field), fieldsize(type, field), ptr ); } while(0)
#define DisplayWriteFieldAddress(field, ptr, size, type, filter) \
    do { IF_OPT(filter) m_display->WriteFieldAddress( # field, offsetof(type, field), fieldsize(type, field), ptr, size ); } while(0)
#define DisplayStartTextElement(name, filter) \
    do { IF_OPT(filter) m_display->StartTextElement( name ); } while(0)
#define DisplayEndTextElement(filter) \
    do { IF_OPT(filter) m_display->EndTextElement(); } while(0)
#define DisplayWriteXmlText( args, filter ) \
    do { IF_OPT(filter) m_display->WriteXmlText args; } while(0)
#define DisplayWriteXmlTextBlock( args, filter ) \
    do { IF_OPT(filter) m_display->WriteXmlTextBlock args; } while(0)

#define CoverageRead( ptr, size ) \
    do { IF_OPT(DEBUG_COVERAGE) PTR_READ(TO_TADDR(ptr), size); } while(0)
#define CoverageReadString( taddr ) \
    do { PTR_BYTE ptr(TO_TADDR(taddr)); while( *ptr++ ); }while(0)

void AppendNilToken( mdToken token, SString& buf )
{
    _ASSERTE(RidFromToken(token) == mdTokenNil);

    const WCHAR * id = NULL;
    switch(token)
    {
#define mdNilEnt(x) case x: \
        id = W(#x); \
        break
        mdNilEnt(mdModuleNil);
        mdNilEnt(mdTypeRefNil);
        mdNilEnt(mdTypeDefNil);
        mdNilEnt(mdFieldDefNil);
        mdNilEnt(mdMethodDefNil);
        mdNilEnt(mdParamDefNil);
        mdNilEnt(mdInterfaceImplNil);
        mdNilEnt(mdMemberRefNil);
        mdNilEnt(mdCustomAttributeNil);
        mdNilEnt(mdPermissionNil);
        mdNilEnt(mdSignatureNil);
        mdNilEnt(mdEventNil);
        mdNilEnt(mdPropertyNil);
        mdNilEnt(mdModuleRefNil);
        mdNilEnt(mdTypeSpecNil);
        mdNilEnt(mdAssemblyNil);
        mdNilEnt(mdAssemblyRefNil);
        mdNilEnt(mdFileNil);
        mdNilEnt(mdExportedTypeNil);
        mdNilEnt(mdManifestResourceNil);

        mdNilEnt(mdGenericParamNil);
        mdNilEnt(mdGenericParamConstraintNil);
        mdNilEnt(mdMethodSpecNil);

        mdNilEnt(mdStringNil);
#undef mdNilEnt
    }
    buf.Append( id );
}
void appendByteArray(SString& buf, const BYTE * bytes, ULONG cbBytes)
{
    for( COUNT_T i = 0; i < cbBytes; ++i )
    {
        buf.AppendPrintf(W("%02x"), bytes[i]);
    }
}
/*}}}*/




struct OptionDependencies
{
    OptionDependencies(CLRNativeImageDumpOptions value,
                       CLRNativeImageDumpOptions dep) : m_value(value),
                                                        m_dep(dep)
    {

    }
    CLRNativeImageDumpOptions m_value;
    CLRNativeImageDumpOptions m_dep;
};

static OptionDependencies g_dependencies[] =
{
#define OPT_DEP(value, dep) OptionDependencies(CLRNATIVEIMAGE_ ## value,\
                                               CLRNATIVEIMAGE_ ## dep)
    OPT_DEP(RESOURCES, COR_INFO),
    OPT_DEP(METADATA, COR_INFO),
    OPT_DEP(PRECODES, MODULE),
    //Does methoddescs require ModuleTables?
    OPT_DEP(VERBOSE_TYPES, METHODDESCS),
    OPT_DEP(GC_INFO, METHODS),
    OPT_DEP(FROZEN_SEGMENT, MODULE),
    OPT_DEP(SLIM_MODULE_TBLS, MODULE),
    OPT_DEP(MODULE_TABLES, SLIM_MODULE_TBLS),
    OPT_DEP(DISASSEMBLE_CODE, METHODS),

    OPT_DEP(FIXUP_HISTOGRAM, FIXUP_TABLES),
    OPT_DEP(FIXUP_THUNKS, FIXUP_TABLES),

#undef OPT_DEP
};

// Metadata helpers for DAC
// This is mostly copied from mscoree.cpp which isn't available in mscordacwks.dll.
//

// This function gets the Dispenser interface given the CLSID and REFIID.
STDAPI DLLEXPORT MetaDataGetDispenser(
    REFCLSID     rclsid,    // The class to desired.
    REFIID       riid,      // Interface wanted on class factory.
    LPVOID FAR * ppv)       // Return interface pointer here.
{
    _ASSERTE(rclsid == CLSID_CorMetaDataDispenser);

    return InternalCreateMetaDataDispenser(riid, ppv);
}


NativeImageDumper::NativeImageDumper(PTR_VOID loadedBase,
                                     const WCHAR * const name,
                                     IXCLRDataDisplay * display,
                                     IXCLRLibrarySupport * support,
                                     IXCLRDisassemblySupport *dis)
    :
    m_decoder(loadedBase),
    m_name(name),
    m_baseAddress(loadedBase),
    m_display(display),
    m_librarySupport(support),
    m_import(NULL),
    m_assemblyImport(NULL),
    m_manifestAssemblyImport(NULL),
    m_dependencies(NULL),
    m_imports(NULL),
    m_dis(dis),
    m_MetadataSize(0),
    m_ILHostCopy(NULL),
    m_isCoreLibHardBound(false),
    m_sectionAlignment(0)
{
    IfFailThrow(m_display->GetDumpOptions(&m_dumpOptions));

    //set up mscorwks stuff.
    m_mscorwksBase = DacGlobalBase();
    _ASSERTE(m_mscorwksBase);
    PEDecoder mscorwksDecoder(dac_cast<PTR_VOID>(m_mscorwksBase));
    m_mscorwksSize = mscorwksDecoder.GetSize();
    m_mscorwksPreferred = TO_TADDR(mscorwksDecoder.GetPreferredBase());
    //add implied options (i.e. if you want to dump the module, you also have
    //to dump the native info.
    CLRNativeImageDumpOptions current;
    do
    {
        current = m_dumpOptions;
        for( unsigned i = 0; i < _countof(g_dependencies); ++i )
        {
            if( m_dumpOptions & g_dependencies[i].m_value )
                m_dumpOptions |= g_dependencies[i].m_dep;
        }
    }while( current != m_dumpOptions );
    IF_OPT(DISASSEMBLE_CODE)
    {
        //configure the disassembler
        m_dis->SetTranslateAddrCallback(TranslateAddressCallback);
        m_dis->SetTranslateFixupCallback(TranslateFixupCallback);
        m_dis->PvClientSet(this);
    }
}

void GuidToString( GUID& guid, SString& s )
{
    WCHAR guidString[64];
    GuidToLPWSTR(guid, guidString, sizeof(guidString) / sizeof(WCHAR));
    //prune the { and }
    _ASSERTE(guidString[0] == W('{')
             && guidString[wcslen(guidString) - 1] == W('}'));
    guidString[wcslen(guidString) - 1] = W('\0');
    s.Append( guidString + 1 );
}

NativeImageDumper::~NativeImageDumper()
{
}

inline const void * ptr_add(const void * ptr, COUNT_T size)
{
    return reinterpret_cast<const BYTE *>(ptr) + size;
}

//This does pointer arithmetic on a DPtr.
template<typename T>
inline const DPTR(T) dptr_add(T* ptr, COUNT_T offset)
{
    return DPTR(T)(PTR_HOST_TO_TADDR(ptr) + (offset * sizeof(T)));
}

template<typename T>
inline const DPTR(T) dptr_sub(T* ptr, COUNT_T offset)
{
    return DPTR(T)(PTR_HOST_TO_TADDR(ptr) - (offset * sizeof(T)));
}
template<typename T>
inline const DPTR(T) dptr_sub(DPTR(T)* ptr, COUNT_T offset)
{
    return DPTR(T)(PTR_HOST_TO_TADDR(ptr) - (offset * sizeof(T)));
}

struct MDTableType
{
    MDTableType(unsigned t, const char * n) : m_token(t), m_name(n)  { }
    unsigned m_token;
    const char * m_name;
};

static unsigned s_tableTypes[] =
{
    /*
#ifdef MiniMdTable
#undef MiniMdTable
#endif
#define MiniMdTable(x) TBL_##x << 24,
    MiniMdTables()
#undef MiniMdTable
    mdtName
    */
    mdtModule,
    mdtTypeRef,
    mdtTypeDef,
    mdtFieldDef,
    mdtMethodDef,
    mdtParamDef,
    mdtInterfaceImpl,
    mdtMemberRef,
    mdtCustomAttribute,
    mdtPermission,
    mdtSignature,
    mdtEvent,
    mdtProperty,
    mdtModuleRef,
    mdtTypeSpec,
    mdtAssembly,
    mdtAssemblyRef,
    mdtFile,
    mdtExportedType,
    mdtManifestResource,
    mdtGenericParam,
    mdtMethodSpec,
    mdtGenericParamConstraint,
};

const NativeImageDumper::EnumMnemonics s_CorHdrFlags[] =
{
#define CHF_ENTRY(f,v) NativeImageDumper::EnumMnemonics(f, v)
    CHF_ENTRY(COMIMAGE_FLAGS_ILONLY, W("IL Only")),
    CHF_ENTRY(COMIMAGE_FLAGS_32BITREQUIRED, W("32-bit Required")),
    CHF_ENTRY(COMIMAGE_FLAGS_IL_LIBRARY, W("IL Library")),
    CHF_ENTRY(COMIMAGE_FLAGS_STRONGNAMESIGNED, W("Strong Name Signed")),
    CHF_ENTRY(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT, W("Has Native Entrypoint")),
    CHF_ENTRY(COMIMAGE_FLAGS_TRACKDEBUGDATA, W("Track Debug Data")),
    CHF_ENTRY(COMIMAGE_FLAGS_32BITPREFERRED, W("32-bit Preferred"))
#undef CHF_ENTRY
};

void NativeImageDumper::DumpAssemblySignature(CORCOMPILE_ASSEMBLY_SIGNATURE & assemblySignature)
{
    {
        TempBuffer buf;
        GuidToString(assemblySignature.mvid, buf);
        DisplayWriteFieldStringW( mvid, (const WCHAR*)buf,
                                  CORCOMPILE_ASSEMBLY_SIGNATURE,
                                  COR_INFO );
    }
    DisplayWriteFieldInt( timeStamp, assemblySignature.timeStamp,
                          CORCOMPILE_ASSEMBLY_SIGNATURE, COR_INFO );
    DisplayWriteFieldInt( ilImageSize,
                          assemblySignature.ilImageSize,
                          CORCOMPILE_ASSEMBLY_SIGNATURE, COR_INFO );
}


//error code return?
void
NativeImageDumper::DumpNativeImage()
{
    COUNT_T size;
    const void *data;

    m_display->StartDocument();

    DisplayStartCategory( "File", PE_INFO );
    DisplayWriteElementStringW( "path", m_name, PE_INFO );

    DisplayWriteElementInt( "diskSize", m_decoder.GetSize(), PE_INFO );
    _ASSERTE(sizeof(IMAGE_DOS_HEADER) < m_decoder.GetSize());

    PTR_IMAGE_DOS_HEADER dosHeader =
        PTR_IMAGE_DOS_HEADER(dac_cast<TADDR>(m_baseAddress));
    DisplayWriteElementAddress( "IMAGE_DOS_HEADER",
                                DPtrToPreferredAddr(dosHeader),
                                sizeof(*dosHeader), PE_INFO );

    // NT headers

    if (!m_decoder.HasNTHeaders())
    {
        IF_OPT(PE_INFO)
        {
            DisplayWriteElementString("isPEFile", "false", PE_INFO);
            DisplayEndCategory(PE_INFO);
        }
        else
            m_display->ErrorPrintF("Non-PE file");

        m_display->EndDocument();
        return;
    }

    CONSISTENCY_CHECK(m_decoder.CheckNTHeaders());
    if (!m_decoder.CheckNTHeaders())
    {
        m_display->ErrorPrintF("*** NT headers are not valid ***");
        return;
    }

    DisplayWriteElementString("imageType", m_decoder.Has32BitNTHeaders()
                              ? "32 bit image" : "64 bit image", PE_INFO);
    DisplayWriteElementAddress("address", (SIZE_T)m_decoder.GetNativePreferredBase(),
                               m_decoder.GetVirtualSize(), PE_INFO);
    DisplayWriteElementInt( "TimeDateStamp", m_decoder.GetTimeDateStamp(),
                            PE_INFO );

    if( m_decoder.Has32BitNTHeaders() )
    {
        PTR_IMAGE_NT_HEADERS32 ntHeaders(m_decoder.GetNTHeaders32());
        //base, size, sectionAlign
        _ASSERTE(ntHeaders->OptionalHeader.SectionAlignment >=
                 ntHeaders->OptionalHeader.FileAlignment);
        m_imageSize = ntHeaders->OptionalHeader.SizeOfImage;
        m_display->NativeImageDimensions(PTR_TO_TADDR(m_decoder.GetBase()),
                                         ntHeaders->OptionalHeader.SizeOfImage,
                                         ntHeaders->OptionalHeader.SectionAlignment);
        /* REVISIT_TODO Mon 11/21/2005
         * I don't understand this.  Sections start on a two page boundary, but
         * data ends on a one page boundary.  What's up with that?
         */
        m_sectionAlignment = GetOsPageSize(); //ntHeaders->OptionalHeader.SectionAlignment;
        unsigned ntHeaderSize = sizeof(*ntHeaders)
            - sizeof(ntHeaders->OptionalHeader)
            + ntHeaders->FileHeader.SizeOfOptionalHeader;
        DisplayWriteElementAddress( "IMAGE_NT_HEADERS32",
                                    DPtrToPreferredAddr(ntHeaders),
                                    ntHeaderSize, PE_INFO );

    }
    else
    {
        PTR_IMAGE_NT_HEADERS64 ntHeaders(m_decoder.GetNTHeaders64());
        //base, size, sectionAlign
        _ASSERTE(ntHeaders->OptionalHeader.SectionAlignment >=
                 ntHeaders->OptionalHeader.FileAlignment);
        m_imageSize = ntHeaders->OptionalHeader.SizeOfImage;
        m_display->NativeImageDimensions((SIZE_T)ntHeaders->OptionalHeader.ImageBase,
                                         ntHeaders->OptionalHeader.SizeOfImage,
                                         ntHeaders->OptionalHeader.SectionAlignment);
        m_sectionAlignment = ntHeaders->OptionalHeader.SectionAlignment;
        unsigned ntHeaderSize = sizeof(*ntHeaders)
            - sizeof(ntHeaders->OptionalHeader)
            + ntHeaders->FileHeader.SizeOfOptionalHeader;
        DisplayWriteElementAddress( "IMAGE_NT_HEADERS64",
                                    DPtrToPreferredAddr(ntHeaders),
                                    ntHeaderSize, PE_INFO );
    }
    DisplayEndCategory(PE_INFO);

    // PE Section info

    DisplayStartArray("Sections", W("%-8s%s\t(disk %s)  %s"), PE_INFO);

    for (COUNT_T i = 0; i < m_decoder.GetNumberOfSections(); i++)
    {
        PTR_IMAGE_SECTION_HEADER section = m_decoder.FindFirstSection() + i;
        m_display->Section(reinterpret_cast<char *>(section->Name),
                           section->VirtualAddress,
                           section->SizeOfRawData);
        DisplayStartStructure( "Section", DPtrToPreferredAddr(section),
                               sizeof(*section), PE_INFO );
        DisplayWriteElementString("name", (const char *)section->Name, PE_INFO);
        DisplayWriteElementAddress( "address", RvaToDisplay(section->VirtualAddress),
                                    section->Misc.VirtualSize, PE_INFO );
        DisplayWriteElementAddress( "disk", section->PointerToRawData,
                                    section->SizeOfRawData, PE_INFO );

        DisplayWriteElementEnumerated( "access", section->Characteristics,
                                       s_ImageSections, W(", "), PE_INFO );
        DisplayEndStructure( PE_INFO ); //Section
    }
    DisplayEndArray("Total Sections", PE_INFO);

    // Image directory info

    DisplayStartArray( "Directories", W("%-40s%s"), PE_INFO );

    for ( COUNT_T i = 0; i < IMAGE_NUMBEROF_DIRECTORY_ENTRIES; i++)
    {
        static const char *directoryNames[] =
        {
            /* 0*/"IMAGE_DIRECTORY_ENTRY_EXPORT",
            /* 1*/"IMAGE_DIRECTORY_ENTRY_IMPORT",
            /* 2*/"IMAGE_DIRECTORY_ENTRY_RESOURCE",
            /* 3*/"IMAGE_DIRECTORY_ENTRY_EXCEPTION",
            /* 4*/"IMAGE_DIRECTORY_ENTRY_SECURITY",
            /* 5*/"IMAGE_DIRECTORY_ENTRY_BASERELOC",
            /* 6*/"IMAGE_DIRECTORY_ENTRY_DEBUG",
            /* 7*/"IMAGE_DIRECTORY_ENTRY_ARCHITECTURE",
            /* 8*/"IMAGE_DIRECTORY_ENTRY_GLOBALPTR",
            /* 9*/"IMAGE_DIRECTORY_ENTRY_TLS",
            /*10*/"IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG",
            /*11*/"IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT",
            /*12*/"IMAGE_DIRECTORY_ENTRY_IAT",
            /*13*/"IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT",
            /*14*/"IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR",
            /* 2*/"",
        };

        IMAGE_DATA_DIRECTORY *entry = m_decoder.GetDirectoryEntry(i);

        if (entry->VirtualAddress != 0)
        {
            DisplayStartElement("Directory", PE_INFO);
            DisplayWriteElementString("name", directoryNames[i], PE_INFO);
            DisplayWriteElementAddress("address",
                                       RvaToDisplay(entry->VirtualAddress),
                                       entry->Size, PE_INFO);

            DisplayEndElement( PE_INFO ); //Directory
        }
    }
    DisplayEndArray("Total Directories", PE_INFO); //Directories

    // COM+ info

    if (!m_decoder.HasCorHeader())
    {
        IF_OPT(COR_INFO)
            DisplayWriteElementString("CLRInfo", "<none>", COR_INFO);
        else
            m_display->ErrorPrintF("Non-CLR image\n");

        m_display->EndDocument();
        return;
    }

    CONSISTENCY_CHECK(m_decoder.CheckCorHeader());
    if (!m_decoder.CheckCorHeader())
    {
        m_display->ErrorPrintF("*** INVALID CLR Header ***");
        m_display->EndDocument();
        return;
    }

    DisplayStartCategory("CLRInfo", COR_INFO);
    PTR_IMAGE_COR20_HEADER pCor(m_decoder.GetCorHeader());
    {
#define WRITE_COR20_FIELD( name ) m_display->WriteFieldAddress( \
            # name, offsetof(IMAGE_COR20_HEADER, name),         \
            fieldsize(IMAGE_COR20_HEADER, name),                \
            RvaToDisplay( pCor-> name . VirtualAddress ),       \
            pCor-> name . Size  )

        m_display->StartStructure( "IMAGE_COR20_HEADER",
                                   DPtrToPreferredAddr(pCor),
                                   sizeof(*pCor) );

        DisplayWriteFieldUInt( MajorRuntimeVersion, pCor->MajorRuntimeVersion, IMAGE_COR20_HEADER, COR_INFO );
        DisplayWriteFieldUInt( MinorRuntimeVersion, pCor->MinorRuntimeVersion, IMAGE_COR20_HEADER, COR_INFO );

        // Symbol table and startup information
        WRITE_COR20_FIELD(MetaData);
        DisplayWriteFieldEnumerated( Flags, pCor->Flags, IMAGE_COR20_HEADER, s_CorHdrFlags, W(", "), COR_INFO );
        DisplayWriteFieldUInt( EntryPointToken, pCor->EntryPointToken, IMAGE_COR20_HEADER, COR_INFO );

        // Binding information
        WRITE_COR20_FIELD(Resources);
        WRITE_COR20_FIELD(StrongNameSignature);

        // Regular fixup and binding information
        WRITE_COR20_FIELD(CodeManagerTable);
        WRITE_COR20_FIELD(VTableFixups);
        WRITE_COR20_FIELD(ExportAddressTableJumps);

        // Precompiled image info
        WRITE_COR20_FIELD(ManagedNativeHeader);

        m_display->EndStructure(); //IMAGE_COR20_HEADER
#undef WRITE_COR20_FIELD
    }

    //make sure to touch the strong name signature even if we won't print it.
    if (m_decoder.HasStrongNameSignature())
    {
        if (m_decoder.IsStrongNameSigned())
        {
            DACCOP_IGNORE(CastBetweenAddressSpaces,"nidump is in-proc and doesn't maintain a clean separation of address spaces (target and host are the same.");
            data = reinterpret_cast<void*>(dac_cast<TADDR>(m_decoder.GetStrongNameSignature(&size)));

            IF_OPT(COR_INFO)
            {
                TempBuffer sig;

                appendByteArray(sig, (BYTE*)data, size);

                DisplayWriteElementStringW( "StrongName", (const WCHAR *)sig,
                                            COR_INFO );
            }
        }
        else
        {
            DisplayWriteEmptyElement("DelaySigned", COR_INFO);
        }
    }

#ifdef FEATURE_READYTORUN
    if (m_decoder.HasReadyToRunHeader())
        DisplayWriteElementString( "imageType", "ReadyToRun image", COR_INFO);
    else
#endif
    if (m_decoder.IsILOnly())
        DisplayWriteElementString( "imageType", "IL only image", COR_INFO);
    else
    if (m_decoder.HasNativeHeader())
        DisplayWriteElementString( "imageType", "Native image", COR_INFO);
    else
        DisplayWriteElementString( "imageType", "Mixed image", COR_INFO);

    DACCOP_IGNORE(CastBetweenAddressSpaces,"nidump is in-proc and doesn't maintain a clean separation of address spaces (target and host are the same.");
    data = reinterpret_cast<void*>(dac_cast<TADDR>(m_decoder.GetMetadata(&size)));
    OpenMetadata();
    IF_OPT(METADATA)
    {
        DWORD dwAssemblyFlags = 0;
        IfFailThrow(m_manifestAssemblyImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL,
                                                          NULL, NULL,
                                                          NULL, NULL,
                                                          NULL, &dwAssemblyFlags));
        if ((afContentType_WindowsRuntime & dwAssemblyFlags) == afContentType_WindowsRuntime)
        {
            DisplayWriteElementString ("Metadata", "Not supported by WinRT", COR_INFO);
        }
        else
        {
            WriteElementsMetadata( "Metadata", TO_TADDR(data), size );
        }
    }

    CoverageRead(TO_TADDR(data), size);

    if (m_decoder.HasNativeHeader())
    {
        DACCOP_IGNORE(CastBetweenAddressSpaces,"nidump is in-proc and doesn't maintain a clean separation of address spaces (target and host are the same.");
        data = reinterpret_cast<void*>(dac_cast<TADDR>(m_decoder.GetNativeManifestMetadata(&size)));

        IF_OPT(METADATA)
        {
            WriteElementsMetadata( "NativeManifestMetadata", TO_TADDR(data), size );
        }
        else
        {
            DisplayWriteElementAddress( "NativeManifestMetadata",
                                        DataPtrToDisplay((TADDR)data), size,
                                        COR_INFO );
        }


        /* REVISIT_TODO Tue 09/20/2005
         * Anything to display in the native metadata?
         */
        CoverageRead(TO_TADDR(data), size);

        /* REVISIT_TODO Tue 09/20/2005
         * Dump the debug map?  Indexed by method RID... probably a good idea
         */
        data = reinterpret_cast<void*>(m_decoder.GetNativeDebugMap(&size));

        DisplayWriteElementAddress( "debugMap", DataPtrToDisplay((TADDR)data), size,
                                    COR_INFO);
        CoverageRead(TO_TADDR(data), size);

        //also read the entire debug map section
        IMAGE_SECTION_HEADER * dbgmap = FindSection( ".dbgmap" );
        if (dbgmap != NULL)
        {
            CoverageRead(TO_TADDR(dbgmap->VirtualAddress)
                         + PTR_TO_TADDR(m_decoder.GetBase()),
                         (ULONG32)ALIGN_UP(dbgmap->Misc.VirtualSize, GetSectionAlignment()));
        }

        //read the .il and .rsrc sections in their entirety
        IF_OPT(DEBUG_COVERAGE)
        {
            IMAGE_SECTION_HEADER *hdr;
            hdr = FindSection( ".rsrc" );
            if( hdr != NULL )
            {
                CoverageRead( m_decoder.GetRvaData(hdr->VirtualAddress),
                              (ULONG32)hdr->Misc.VirtualSize );
            }
        }
        IF_OPT_OR(DEBUG_COVERAGE, IL)
        {
            IMAGE_SECTION_HEADER *hdr = FindSection( ".text" );

            if( hdr != NULL )
            {
                m_ILSectionStart = hdr->VirtualAddress;
                m_ILHostCopy = (BYTE*)PTR_READ(m_decoder.GetRvaData(hdr->VirtualAddress), hdr->Misc.VirtualSize);
#ifdef _DEBUG
                m_ILSectionSize = hdr->Misc.VirtualSize;
#endif
            }
            else
            {
                m_ILSectionStart = 0;
                m_ILHostCopy = NULL;
#ifdef _DEBUG
                m_ILSectionSize = 0;
#endif
            }
            _ASSERTE( (((TADDR)m_ILHostCopy) & 3) == 0 );
            _ASSERTE((m_ILSectionStart & 3) == 0);

        }
    }

    data = m_decoder.GetResources(&size);
    IF_OPT(RESOURCES)
    {
        DisplayStartStructure( "resource", DataPtrToDisplay((TADDR)data), size,
                               COR_INFO );
        DisplayStartArray( "Resources", NULL, COR_INFO );
        HCORENUM hEnum = NULL;
        for(;;)
        {
            mdManifestResource resTokens[1];
            ULONG numTokens = 0;
            IfFailThrow(m_assemblyImport->EnumManifestResources(&hEnum,
                                                                resTokens,
                                                                1,
                                                                &numTokens));
            if( numTokens == 0 )
                break;

            WCHAR resourceName[256];
            ULONG nameLen;
            mdToken impl;
            DWORD offset, flags;
            IfFailThrow(m_assemblyImport->GetManifestResourceProps(resTokens[0],
                                                           resourceName,
                                                           _countof(resourceName),
                                                           &nameLen,
                                                           &impl,
                                                           &offset,
                                                           &flags));
            if( RidFromToken(impl) != 0 )
                continue; //skip all non-zero providers
            resourceName[nameLen] = W('\0');
            DPTR(DWORD UNALIGNED) res(TO_TADDR(data) + offset);
            DWORD resSize = *res;
            DisplayWriteElementAddressNamedW( "Resource", resourceName,
                                              DPtrToPreferredAddr(res),
                                              resSize + sizeof(DWORD),
                                              RESOURCES );
        }
        DisplayEndArray( "Total Resources", COR_INFO ); //Resources
        DisplayEndStructure( COR_INFO ); //resource
    }
    else
    {
        DisplayWriteElementAddress( "resource", DataPtrToDisplay((TADDR)data), size,
                                    COR_INFO );
    }

    ULONG resultSize;
    GUID mvid;
    m_manifestImport->GetScopeProps(bigBuffer, bigBufferSize, &resultSize, &mvid);
    /* REVISIT_TODO Wed 09/07/2005
     * The name is the .module entry.  Why isn't it present in the ngen image?
     */
    TempBuffer guidString;
    GuidToString( mvid, guidString );
    if( wcslen(bigBuffer) )
        DisplayWriteElementStringW( "scopeName", bigBuffer, COR_INFO );
    DisplayWriteElementStringW( "mvid", (const WCHAR *)guidString, COR_INFO );

    if (m_decoder.HasManagedEntryPoint())
    {
        DisplayStartVStructure( "ManagedEntryPoint", COR_INFO );
        unsigned token = m_decoder.GetEntryPointToken();
        DisplayWriteElementUInt( "Token", token, COR_INFO );
        TempBuffer buf;
        AppendTokenName( token, buf );
        DisplayWriteElementStringW( "TokenName", (const WCHAR *)buf, COR_INFO );
        DisplayEndVStructure( COR_INFO );
    }
    else if (m_decoder.HasNativeEntryPoint())
    {
        DisplayWriteElementPointer( "NativeEntryPoint", (SIZE_T)m_decoder.GetNativeEntryPoint(),
                                    COR_INFO );
    }

    /* REVISIT_TODO Mon 11/21/2005
     * Dump the version info completely
     */
    if( m_decoder.HasNativeHeader() )
    {
        PTR_CORCOMPILE_VERSION_INFO versionInfo( m_decoder.GetNativeVersionInfo() );

        DisplayStartStructure("CORCOMPILE_VERSION_INFO",
                              DPtrToPreferredAddr(versionInfo),
                              sizeof(*versionInfo), COR_INFO);

        DisplayStartStructureWithOffset( sourceAssembly,
                                         DPtrToPreferredAddr(versionInfo) + offsetof(CORCOMPILE_VERSION_INFO, sourceAssembly),
                                         sizeof(versionInfo->sourceAssembly),
                                         CORCOMPILE_VERSION_INFO, COR_INFO );
        DumpAssemblySignature(versionInfo->sourceAssembly);
        DisplayEndStructure(COR_INFO); //sourceAssembly

        COUNT_T numDeps;
        PTR_CORCOMPILE_DEPENDENCY deps(TO_TADDR(m_decoder.GetNativeDependencies(&numDeps)));

        DisplayStartArray( "Dependencies", NULL, COR_INFO );

        for( COUNT_T i = 0; i < numDeps; ++i )
        {
            DisplayStartStructure("CORCOMPILE_DEPENDENCY", DPtrToPreferredAddr(deps + i),
                                  sizeof(deps[i]), COR_INFO );
            WriteFieldMDTokenImport( dwAssemblyRef, deps[i].dwAssemblyRef,
                                     CORCOMPILE_DEPENDENCY, COR_INFO,
                                     m_manifestImport );
            WriteFieldMDTokenImport( dwAssemblyDef, deps[i].dwAssemblyDef,
                                     CORCOMPILE_DEPENDENCY, COR_INFO,
                                     m_manifestImport );
            DisplayStartStructureWithOffset( signAssemblyDef,
                                             DPtrToPreferredAddr(deps + i) + offsetof(CORCOMPILE_DEPENDENCY, signAssemblyDef),
                                             sizeof(deps[i]).signAssemblyDef,
                                             CORCOMPILE_DEPENDENCY, COR_INFO );
            DumpAssemblySignature(deps[i].signAssemblyDef);
            DisplayEndStructure(COR_INFO); //signAssemblyDef

            {
                TempBuffer buf;
                if( deps[i].signNativeImage == INVALID_NGEN_SIGNATURE )
                {
                    buf.Append( W("INVALID_NGEN_SIGNATURE") );
                }
                else
                {
                    GuidToString(deps[i].signNativeImage, buf);
                }
                DisplayWriteFieldStringW( signNativeImage, (const WCHAR*)buf,
                                          CORCOMPILE_DEPENDENCY, COR_INFO );
#if 0
                if( m_librarySupport
                    && deps[i].signNativeImage != INVALID_NGEN_SIGNATURE )
                {
                    buf.Clear();
                    AppendTokenName(deps[i].dwAssemblyRef, buf, m_import );
                    IfFailThrow(m_librarySupport->LoadDependency( (const WCHAR*)buf,
                                                                  deps[i].signNativeImage ));
                }
#endif

            }


            DisplayEndStructure(COR_INFO); //CORCOMPILE_DEPENDENCY
        }
        DisplayEndArray( "Total Dependencies", COR_INFO );
        DisplayEndStructure(COR_INFO); //CORCOMPILE_VERSION_INFO

        NativeImageDumper::Dependency * traceDependency = OpenDependency(0);
        TraceDumpDependency( 0, traceDependency );

        for( COUNT_T i = 0; i < numDeps; ++i )
        {
            traceDependency = OpenDependency( i + 1 );
            TraceDumpDependency( i + 1, traceDependency );
        }
        _ASSERTE(m_dependencies[0].pModule != NULL);

        /* XXX Wed 12/14/2005
         * Now for the real insanity.  I need to initialize static classes in
         * the DAC.  First I need to find CoreLib's dependency entry.  Search
         * through all of the dependencies to find the one marked as
         * fIsCoreLib.  If I don't find anything marked that way, then "self"
         * is CoreLib.
         */
        Dependency * corelib = NULL;
        for( COUNT_T i = 0; i < m_numDependencies; ++i )
        {
            if( m_dependencies[i].fIsCoreLib )
            {
                corelib = &m_dependencies[i];
                break;
            }
        }

        //If we're actually dumping CoreLib, remap the CoreLib dependency to our own native image.
        if( (corelib == NULL) || !wcscmp(m_name, CoreLibName_W))
        {
            corelib = GetDependency(0);
            corelib->fIsCoreLib = TRUE;
            _ASSERTE(corelib->fIsHardbound);
        }

        _ASSERTE(corelib != NULL);
        if( corelib->fIsHardbound )
        {
            m_isCoreLibHardBound = true;
        }
        if( m_isCoreLibHardBound )
        {
            //go through the module to the binder.
            PTR_Module corelibModule = corelib->pModule;

            PTR_CoreLibBinder binder = corelibModule->m_pBinder;
            g_CoreLib = *binder;

            PTR_MethodTable mt = CoreLibBinder::GetExistingClass(CLASS__OBJECT);
            g_pObjectClass = mt;
        }


        if (g_pObjectClass == NULL)
        {
            //if CoreLib is not hard bound, then warn the user (many features of nidump are shut off)
            m_display->ErrorPrintF( "Assembly %S is soft bound to CoreLib.  nidump cannot dump MethodTables completely.\n", m_name );
            // TritonTODO: reason?
            // reset "hard bound state"
            m_isCoreLibHardBound = false;

        }
    }


    // @todo: VTable Fixups

    // @todo: EAT Jumps

    DisplayEndCategory(COR_INFO); //CLRInfo

#ifdef FEATURE_READYTORUN
    if (m_decoder.HasReadyToRunHeader())
    {
        DumpReadyToRun();
    }
    else
#endif
    if (m_decoder.HasNativeHeader())
    {
        DumpNative();
    }

    m_display->EndDocument();
}

void NativeImageDumper::DumpNative()
{
    DisplayStartCategory("NativeInfo", NATIVE_INFO);

    CONSISTENCY_CHECK(m_decoder.CheckNativeHeader());
    if (!m_decoder.CheckNativeHeader())
    {
        m_display->ErrorPrintF("*** INVALID NATIVE HEADER ***\n");
        return;
    }

    IF_OPT(NATIVE_INFO)
        DumpNativeHeader();

    //host pointer
    CORCOMPILE_EE_INFO_TABLE * infoTable = m_decoder.GetNativeEEInfoTable();

    DisplayStartStructure( "CORCOMPILE_EE_INFO_TABLE",
                           DataPtrToDisplay(PTR_HOST_TO_TADDR(infoTable)),
                           sizeof(*infoTable), NATIVE_INFO );

    /* REVISIT_TODO Mon 09/26/2005
     * Move this further down to include the dumping of the module, and
     * other things.
     */
    DisplayEndStructure(NATIVE_INFO); //NativeInfoTable
    DisplayEndCategory(NATIVE_INFO); //NativeInfo
#if LATER
    //come back here and dump all the fields of the CORCOMPILE_EE_INFO_TABLE
#endif

    IF_OPT(RELOCATIONS)
        DumpBaseRelocs();

    IF_OPT(NATIVE_TABLES)
        DumpHelperTable();

    PTR_Module module = (TADDR)m_decoder.GetPersistedModuleImage();

    //this needs to run for precodes to load the tables that identify precode ranges
    IF_OPT_OR5(MODULE, METHODTABLES, EECLASSES, TYPEDESCS, PRECODES)
        DumpModule(module);

    IF_OPT_OR3(FIXUP_TABLES, FIXUP_HISTOGRAM, FIXUP_THUNKS)
        DumpFixupTables( module );
    IF_OPT_OR3(METHODS, GC_INFO, DISASSEMBLE_CODE )
        DumpMethods( module );
    IF_OPT_OR3(METHODTABLES, EECLASSES, TYPEDESCS)
        DumpTypes( module );
}

void NativeImageDumper::TraceDumpDependency(int idx, NativeImageDumper::Dependency * dependency)
{
    IF_OPT(DEBUG_TRACE)
    {
        m_display->ErrorPrintF("Dependency: %d (%p)\n", idx, dependency);
        m_display->ErrorPrintF("\tPreferred: %p\n", dependency->pPreferredBase);
        m_display->ErrorPrintF("\tLoaded: %p\n", dependency->pLoadedAddress);
        m_display->ErrorPrintF("\tSize: %x (%d)\n", dependency->size, dependency->size);
        m_display->ErrorPrintF("\tModule: P=%p, L=%p\n", DataPtrToDisplay(dac_cast<TADDR>(dependency->pModule)),
                               PTR_TO_TADDR(dependency->pModule));
        m_display->ErrorPrintF("CoreLib=%s, Hardbound=%s\n",
                               (dependency->fIsCoreLib ? "true" : "false"),
                               (dependency->fIsHardbound ? "true" : "false"));
        m_display->ErrorPrintF("Name: %S\n", dependency->name);
    }
}

void NativeImageDumper::WriteElementsMetadata( const char * elementName,
                                               TADDR data, SIZE_T size )
{
    DisplayStartStructure( elementName,
                           DataPtrToDisplay(data), size, ALWAYS );

    /* XXX Mon 03/13/2006
     * Create new metatadata dispenser.  When I define the Emit for defining
     * assemblyrefs for dependencies, I copy the memory and totally hork any
     * mapping back to base addresses.
     */
    ReleaseHolder<IMetaDataTables> tables;
    ReleaseHolder<IMetaDataDispenserEx> pDispenser;
    IfFailThrow(MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
                                     IID_IMetaDataDispenserEx, (void **) &pDispenser));

    VARIANT opt;

    TADDR hostCopyStart = TO_TADDR(PTR_READ(data, (ULONG32)size));
    TADDR rebasedPointer;

    IfFailThrow(pDispenser->GetOption(MetaDataCheckDuplicatesFor, &opt));
    V_UI4(&opt) |= MDDupAssemblyRef | MDDupFile;
    IfFailThrow(pDispenser->SetOption(MetaDataCheckDuplicatesFor, &opt));

    IfFailThrow(pDispenser->OpenScopeOnMemory((const void *)hostCopyStart, (DWORD)size,
                                              ofRead, IID_IMetaDataTables,
                                              (IUnknown **) &tables));
    DisplayStartArray( "Tables", W("%s"), ALWAYS );

    for( unsigned i = 0; i < _countof(s_tableTypes); ++i )
    {
        HRESULT hr = S_OK;
        ULONG idx = 0;
        hr = tables->GetTableIndex(s_tableTypes[i], &idx);
        _ASSERTE(SUCCEEDED(hr));
        ULONG cbRow = 0, cRows = 0, cCols = 0, iKey = 0;
        const char * name = NULL;
        BYTE * ptr = NULL;
        hr = tables->GetTableInfo(idx, &cbRow, &cRows, &cCols,
                                  &iKey, &name);
        _ASSERTE(SUCCEEDED(hr) || hr == E_INVALIDARG);
        if( hr == E_INVALIDARG || cRows == 0 )
        {
            continue; //no such table.
        }

        hr = tables->GetRow(idx, 1, (void**)&ptr);
        IfFailThrow(hr);
        _ASSERTE(SUCCEEDED(hr));
        //compute address
        rebasedPointer = data + (TO_TADDR(ptr) - hostCopyStart);
        _ASSERTE( rebasedPointer >= data && rebasedPointer < (data + size) );
        DisplayWriteElementAddressNamed( "table", name,
                                         DataPtrToDisplay(rebasedPointer),
                                         cbRow * cRows , ALWAYS );
#if 0
        DisplayStartElement( "table", ALWAYS );
        DisplayWriteElementString( "name", name, ALWAYS );
        //compute address
        rebasedPointer = data + (TO_TADDR(ptr) - hostCopyStart);
        _ASSERTE( rebasedPointer >= data && rebasedPointer < (data + size) );
        DisplayWriteElementAddress( "address", DataPtrToDisplay(rebasedPointer),
                                    cbRow * cRows, ALWAYS );
        DisplayEndElement(  ALWAYS ); //Table
#endif
    }
    DisplayEndArray( "Total Tables",  ALWAYS );

    PTR_STORAGESIGNATURE root(data);
    _ASSERTE(root->lSignature == STORAGE_MAGIC_SIG);
    //the root is followed by the version string who's length is
    //root->iVersionString.  After that is a storage header that counts the
    //number of streams.
    PTR_STORAGEHEADER sHdr(data + sizeof(*root) + root->iVersionString);
    DisplayStartArray( "Pools", NULL, ALWAYS );

    //now check the pools

    //start of stream headers
    PTR_STORAGESTREAM streamHeader( PTR_TO_TADDR(sHdr) + sizeof(*sHdr) );
    for( unsigned i = 0; i < sHdr->iStreams; ++i )
    {
        if( streamHeader->iSize > 0 )
        {
            DisplayWriteElementAddressNamed( "heap", streamHeader->rcName,
                                             DataPtrToDisplay( data + streamHeader->iOffset ),
                                             streamHeader->iSize, ALWAYS );
        }
        //Stream headers aren't fixed size.  the size is aligned up based on a
        //variable length string at the end.
        streamHeader = PTR_STORAGESTREAM(PTR_TO_TADDR(streamHeader)
                                         + ALIGN_UP(offsetof(STORAGESTREAM, rcName) + strlen(streamHeader->rcName) + 1, 4));
    }

    DisplayEndArray( "Total Pools", ALWAYS ); //Pools
    DisplayEndStructure( ALWAYS ); //nativeMetadata
}
void NativeImageDumper::OpenMetadata()
{
    COUNT_T size;

    DACCOP_IGNORE(CastBetweenAddressSpaces,"nidump is in-proc and doesn't maintain a clean separation of address spaces (target and host are the same.");
    const void *data = reinterpret_cast<void*>(dac_cast<TADDR>(m_decoder.GetMetadata(&size)));

    ReleaseHolder<IMetaDataDispenserEx> pDispenser;
    IfFailThrow(MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
                                     IID_IMetaDataDispenserEx, (void **) &pDispenser));

    VARIANT opt;
    IfFailThrow(pDispenser->GetOption(MetaDataCheckDuplicatesFor, &opt));
    V_UI4(&opt) |= MDDupAssemblyRef | MDDupFile;
    IfFailThrow(pDispenser->SetOption(MetaDataCheckDuplicatesFor, &opt));

    data = PTR_READ(TO_TADDR(data), size);
    IfFailThrow(pDispenser->OpenScopeOnMemory(data, size, ofRead,
                                              IID_IMetaDataImport2, (IUnknown **) &m_import));

    IfFailThrow(m_import->QueryInterface(IID_IMetaDataAssemblyImport,
                                          (void **)&m_assemblyImport));

    m_MetadataStartTarget = TO_TADDR(data);
    m_MetadataSize = size;
    data = PTR_READ(TO_TADDR(data), size);
    m_MetadataStartHost = TO_TADDR(data);

    if (m_decoder.HasNativeHeader())
    {
        DACCOP_IGNORE(CastBetweenAddressSpaces,"nidump is in-proc and doesn't maintain a clean separation of address spaces (target and host are the same.");
        data = reinterpret_cast<void*>(dac_cast<TADDR>(m_decoder.GetNativeManifestMetadata(&size)));

        IfFailThrow(pDispenser->OpenScopeOnMemory(data, size, ofRead,
                                                  IID_IMetaDataImport2, (IUnknown **) &m_manifestImport));

        IfFailThrow(m_manifestImport->QueryInterface(IID_IMetaDataAssemblyImport,
                                              (void **)&m_manifestAssemblyImport));
    }
    else
    {
        m_manifestImport =  m_import;
        m_manifestImport->AddRef();

        m_manifestAssemblyImport = m_assemblyImport;
        m_manifestAssemblyImport->AddRef();
    }
}
void
NativeImageDumper::AppendTokenName(mdToken token, SString& buf)
{
    AppendTokenName(token, buf, NULL);
}
void
NativeImageDumper::AppendTokenName(mdToken token, SString& buf,
                                   IMetaDataImport2 *pImport,
                                   bool force)
{
    mdToken parent;
    ULONG size;
    DWORD attr;
    PCCOR_SIGNATURE pSig;
    PTR_CCOR_SIGNATURE dacSig;
    ULONG cSig;
    DWORD flags;
    ULONG rva;
    CQuickBytes bytes;

    if( CHECK_OPT(DISABLE_NAMES) && !force )
    {
        buf.Append( W("Disabled") );
        return;
    }

    if (pImport == NULL)
        pImport = m_import;
    if( RidFromToken(token) == mdTokenNil )
    {
        AppendNilToken( token, buf );
    }
    else
    {
        switch (TypeFromToken(token))
        {
        case mdtTypeDef:
            IfFailThrow(pImport->GetTypeDefProps(token, bigBuffer, bigBufferSize, &size, &flags, &parent));
            buf.Append(bigBuffer);
            break;

        case mdtTypeRef:
		    // TritonTODO: consolidate with desktop
            // IfFailThrow(pImport->GetTypeRefProps(token, &parent, bigBuffer, bigBufferSize, &size));
            if (FAILED(pImport->GetTypeRefProps(token, &parent, bigBuffer, bigBufferSize, &size)))
                buf.Append(W("ADDED TYPEREF (?)"));
            else
                buf.Append(bigBuffer);
            break;

        case mdtTypeSpec:
            IfFailThrow(pImport->GetTypeSpecFromToken(token, &pSig, &cSig));
            dacSig = metadataToHostDAC(pSig, pImport);
            TypeToString(dacSig, buf, pImport);
            break;

        case mdtFieldDef:
            IfFailThrow(pImport->GetFieldProps(token, &parent, bigBuffer, bigBufferSize, &size, &attr,
                                               &pSig, &cSig, &flags, NULL, NULL));
            AppendTokenName(parent, buf, pImport);
            IfFailThrow(pImport->GetFieldProps(token, &parent, bigBuffer, bigBufferSize, &size, &attr,
                                               &pSig, &cSig, &flags, NULL, NULL));
            buf.AppendPrintf( W("::%s"), bigBuffer );
            break;

        case mdtMethodDef:
            IfFailThrow(pImport->GetMethodProps(token, &parent, bigBuffer, bigBufferSize, &size, &attr,
                                                &pSig, &cSig, &rva, &flags));
            AppendTokenName(parent, buf, pImport);
            IfFailThrow(pImport->GetMethodProps(token, &parent, bigBuffer, bigBufferSize, &size, &attr,
                                                &pSig, &cSig, &rva, &flags));
            buf.AppendPrintf( W("::%s"), bigBuffer );
            break;

        case mdtMemberRef:
            IfFailThrow(pImport->GetMemberRefProps(token, &parent, bigBuffer, bigBufferSize, &size,
                                                   &pSig, &cSig));
            AppendTokenName(parent, buf, pImport);
            IfFailThrow(pImport->GetMemberRefProps(token, &parent, bigBuffer, bigBufferSize, &size,
                                                   &pSig, &cSig));
            buf.AppendPrintf( W("::%s"), bigBuffer );
            break;

        case mdtSignature:
            IfFailThrow(pImport->GetSigFromToken(token, &pSig, &cSig));
#if LATER
            PrettyPrintSig(pSig, cSig, W(""), &bytes, pImport);
            m_display->ErrorPrintF("%S", bytes.Ptr());
#else
            _ASSERTE(!"Unimplemented");
            m_display->ErrorPrintF( "unimplemented" );
#endif
            break;

        case mdtString:
            IfFailThrow(pImport->GetUserString(token, bigBuffer, bigBufferSize, &size));
            bigBuffer[min(size, bigBufferSize-1)] = 0;
            buf.Append( bigBuffer );
            break;

        case mdtAssembly:
        case mdtAssemblyRef:
        case mdtFile:
        case mdtExportedType:
            {
                ReleaseHolder<IMetaDataAssemblyImport> pAssemblyImport;
                IfFailThrow(pImport->QueryInterface(IID_IMetaDataAssemblyImport,
                                                    (void **)&pAssemblyImport));
                PrintManifestTokenName(token, buf, pAssemblyImport, force);
            }
            break;

        case mdtGenericParam:
            {
                ULONG nameLen;
                IfFailThrow(pImport->GetGenericParamProps(token, NULL, NULL, NULL, NULL, bigBuffer,
                                                          _countof(bigBuffer), &nameLen));
                bigBuffer[min(nameLen, _countof(bigBuffer) - 1)] = 0;
                buf.Append( bigBuffer );
            }
            break;

        default:
            _ASSERTE( !"Unknown token type in AppendToken" );
            buf.AppendPrintf( W("token 0x%x"), token );
        }
    }
}
void NativeImageDumper::PrintManifestTokenName(mdToken token, SString& str)
{
    PrintManifestTokenName(token, str, NULL);
}
void
NativeImageDumper::PrintManifestTokenName(mdToken token,
                                          SString& buf,
                                          IMetaDataAssemblyImport *pAssemblyImport,
                                          bool force)
{
    ULONG size;
    const void *pSig;
    ULONG cSig;
    DWORD flags;
    CQuickBytes bytes;
    ULONG hash;

    if( CHECK_OPT(DISABLE_NAMES) && !force )
    {
        buf.Append( W("Disabled") );
        return;
    }

    if (pAssemblyImport == NULL)
        pAssemblyImport = m_manifestAssemblyImport;

    if( RidFromToken(token) == mdTokenNil )
    {
        AppendNilToken( token, buf );
    }
    else
    {
        switch (TypeFromToken(token))
        {
        case mdtAssembly:
            IfFailThrow(pAssemblyImport->GetAssemblyProps(token, &pSig, &cSig,
                                                          &hash, bigBuffer,
                                                          bigBufferSize, &size,
                                                          NULL, &flags));

            buf.Append(bigBuffer);
            break;

        case mdtAssemblyRef:
            IfFailThrow(pAssemblyImport->GetAssemblyRefProps(token, &pSig,
                                                             &cSig, bigBuffer,
                                                             bigBufferSize,
                                                             &size, NULL, NULL,
                                                             NULL, &flags));
            buf.Append(bigBuffer);
            break;

        case mdtFile:
            IfFailThrow(pAssemblyImport->GetFileProps(token, bigBuffer,
                                                      bigBufferSize, &size,
                                                      NULL, NULL, &flags));

            buf.Append(bigBuffer);
            break;

        case mdtExportedType:
            IfFailThrow(pAssemblyImport->GetExportedTypeProps(token, bigBuffer,
                                                      bigBufferSize, &size,
                                                      NULL, NULL, &flags));

            buf.Append(bigBuffer);
            break;

        default:
            buf.AppendPrintf(W("token %x"), token);
        }
    }
}

BOOL NativeImageDumper::HandleFixupForHistogram(PTR_CORCOMPILE_IMPORT_SECTION pSection,
                                                SIZE_T fixupIndex,
                                                SIZE_T *fixupCell)
{
    COUNT_T nImportSections;
    PTR_CORCOMPILE_IMPORT_SECTION pImportSections = m_decoder.GetNativeImportSections(&nImportSections);

    COUNT_T tableSize;
    TADDR tableBase = m_decoder.GetDirectoryData(&pSection->Section, &tableSize);

    COUNT_T table = (COUNT_T)(pSection - pImportSections);
    _ASSERTE(table < nImportSections);

    SIZE_T offset = dac_cast<TADDR>(fixupCell) - tableBase;
    _ASSERTE( offset < tableSize );

    COUNT_T entry = (COUNT_T)(offset / sizeof(TADDR));
    m_fixupHistogram[table][entry]++;

    return TRUE;
}

void NativeImageDumper::ComputeMethodFixupHistogram( PTR_Module module )
{
    COUNT_T nImportSections;
    PTR_CORCOMPILE_IMPORT_SECTION pImportSections = m_decoder.GetNativeImportSections(&nImportSections);

    m_fixupHistogram = new COUNT_T * [nImportSections];

    for (COUNT_T i=0; i < nImportSections; i++)
    {
        PTR_CORCOMPILE_IMPORT_SECTION pSection = m_decoder.GetNativeImportSectionFromIndex(i);

        COUNT_T count = pSection->Section.Size / sizeof(TADDR);

        m_fixupHistogram[i] = new COUNT_T [count];
        ZeroMemory(m_fixupHistogram[i], count * sizeof(COUNT_T));
    }

    ZeroMemory(&m_fixupCountHistogram, sizeof(m_fixupCountHistogram));
    // profiled hot code

    MethodIterator mi(module, &m_decoder, MethodIterator::Hot);
    while (mi.Next())
    {
        m_fixupCount = 0;

        TADDR pFixupList = mi.GetMethodDesc()->GetFixupList();

        if (pFixupList != NULL)
        {
            COUNT_T nImportSections;
            PTR_CORCOMPILE_IMPORT_SECTION pImportSections = m_decoder.GetNativeImportSections(&nImportSections);

            module->FixupDelayListAux(pFixupList, this,
                &NativeImageDumper::HandleFixupForHistogram,
                pImportSections, nImportSections,
                &m_decoder);
        }

        if (m_fixupCount < COUNT_HISTOGRAM_SIZE)
            m_fixupCountHistogram[m_fixupCount]++;
        else
            m_fixupCountHistogram[COUNT_HISTOGRAM_SIZE-1]++;
    }

    // unprofiled code
    MethodIterator miUnprofiled(module, &m_decoder, MethodIterator::Unprofiled);

    while(miUnprofiled.Next())
    {
        m_fixupCount = 0;

        TADDR pFixupList = miUnprofiled.GetMethodDesc()->GetFixupList();

        if (pFixupList != NULL)
        {
            COUNT_T nImportSections;
            PTR_CORCOMPILE_IMPORT_SECTION pImportSections = m_decoder.GetNativeImportSections(&nImportSections);

            module->FixupDelayListAux(pFixupList, this,
                &NativeImageDumper::HandleFixupForHistogram,
                pImportSections, nImportSections,
                &m_decoder);
        }

        if (m_fixupCount < COUNT_HISTOGRAM_SIZE)
            m_fixupCountHistogram[m_fixupCount]++;
        else
            m_fixupCountHistogram[COUNT_HISTOGRAM_SIZE-1]++;
    }
}

void NativeImageDumper::DumpFixupTables( PTR_Module module )
{
    IF_OPT(FIXUP_HISTOGRAM)
        ComputeMethodFixupHistogram( module );

    DisplayStartCategory( "Imports", FIXUP_TABLES );

    COUNT_T nImportSections;
    PTR_CORCOMPILE_IMPORT_SECTION pImportSections = m_decoder.GetNativeImportSections(&nImportSections);

    for (COUNT_T iImportSections = 0; iImportSections < nImportSections; iImportSections++)
    {
        PTR_CORCOMPILE_IMPORT_SECTION pImportSection = pImportSections + iImportSections;

        COUNT_T size;
        TADDR pTable(m_decoder.GetDirectoryData(&pImportSection->Section, &size));
        TADDR pTableEnd = pTable + size;

        TADDR pDataTable(NULL);

        if (pImportSection->Signatures != 0)
            pDataTable = m_decoder.GetRvaData(pImportSection->Signatures);

        switch (pImportSection->Type)
        {
        case CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD:
            {
                COUNT_T entrySize = pImportSection->EntrySize;
                COUNT_T count = size / entrySize;
                _ASSERTE(entrySize == sizeof(CORCOMPILE_VIRTUAL_IMPORT_THUNK));

                for (TADDR pEntry = pTable; pEntry < pTableEnd; pEntry += entrySize)
                {
                    PTR_CORCOMPILE_VIRTUAL_IMPORT_THUNK pThunk = pEntry;

                    DisplayStartStructure("VirtualImportThunk", DPtrToPreferredAddr(pThunk),
                                          entrySize, FIXUP_THUNKS );

                    DisplayWriteElementInt( "Slot", pThunk->slotNum, FIXUP_THUNKS);

                    DisplayEndStructure( FIXUP_THUNKS );
                }
            }
            break;

        case CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD:
            {
                COUNT_T entrySize = pImportSection->EntrySize;
                COUNT_T count = size / entrySize;
                _ASSERTE(entrySize == sizeof(CORCOMPILE_EXTERNAL_METHOD_THUNK));

                for (TADDR pEntry = pTable; pEntry < pTableEnd; pEntry += entrySize)
                {
                    PTR_CORCOMPILE_EXTERNAL_METHOD_THUNK      pThunk = pEntry;

                    DisplayStartStructure("ExternalImportThunk", DPtrToPreferredAddr(pThunk),
                                          entrySize, FIXUP_THUNKS );

                    TADDR pDataAddr  = pDataTable + ((pEntry - pTable) / entrySize) * sizeof(DWORD);
                    PTR_DWORD                                 pData  = pDataAddr;

                    DisplayWriteElementPointer( "DataAddress ",  pDataAddr,  FIXUP_THUNKS );

                    TADDR blobSigAddr = RvaToDisplay(*pData);
                    DisplayWriteElementPointer( "TargetSigAddress",  blobSigAddr, FIXUP_THUNKS );
                    TempBuffer buf;
                    FixupBlobToString(*pData, buf);
                    DisplayWriteElementStringW( "TargetName", (const WCHAR*)buf, FIXUP_THUNKS );

                    DisplayEndStructure( FIXUP_THUNKS );
                }
            }
            break;

        default:
            {
                COUNT_T count = size / sizeof(TADDR);

                for (COUNT_T j = 0; j < count; j++)
                {
                    if (dac_cast<PTR_TADDR>(pTable)[j] == 0)
                        continue;

                    SIZE_T nNextEntry = j + 1;
                    while (nNextEntry < count && dac_cast<PTR_TADDR>(pTable)[nNextEntry] == 0)
                        nNextEntry++;

                    DisplayStartStructure("ImportEntry", DPtrToPreferredAddr(dac_cast<PTR_TADDR>(pTable) + j),
                                          (nNextEntry - j) * sizeof(TADDR), FIXUP_TABLES );

                    if (pDataTable != NULL)
                    {
                        DWORD rva = dac_cast<PTR_DWORD>(pDataTable)[j];
                        WriteElementsFixupTargetAndName(rva);
                    }
                    else
                    {
                        SIZE_T token = dac_cast<PTR_TADDR>(pTable)[j];
                        DisplayWriteElementPointer( "TaggedValue", token, FIXUP_TABLES );
                        WriteElementsFixupBlob(pImportSection, token);
                    }

                    DisplayWriteElementInt( "index", j, FIXUP_HISTOGRAM);
                    DisplayWriteElementInt( "ReferenceCount", m_fixupHistogram[iImportSections][j], FIXUP_HISTOGRAM );

                    DisplayEndStructure( FIXUP_TABLES );
                }
            }
        }
    }
    DisplayEndCategory( FIXUP_TABLES );
}

void NativeImageDumper::FixupThunkToString(PTR_CORCOMPILE_IMPORT_SECTION pImportSection, TADDR addr, SString& buf)
{
    switch (pImportSection->Type)
    {
    case CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD:
        {
            PTR_CORCOMPILE_VIRTUAL_IMPORT_THUNK pThunk = addr;
            buf.AppendPrintf( W("slot %d"), pThunk->slotNum );
        }
        break;

    case CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD:
    case CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH:
        {
            TADDR pTable(m_decoder.GetDirectoryData(&pImportSection->Section));
            COUNT_T index = (COUNT_T)(addr - pTable) / pImportSection->EntrySize;
            TADDR pDataTable(m_decoder.GetRvaData(pImportSection->Signatures));
            TADDR pDataAddr  = pDataTable  + (index * sizeof(DWORD));
            PTR_DWORD pData  = pDataAddr;
            FixupBlobToString(*pData, buf);
        }
        break;

    default:
        _ASSERTE(!"Unknown import type");
    }
}

void NativeImageDumper::WriteElementsFixupBlob(PTR_CORCOMPILE_IMPORT_SECTION pSection, SIZE_T fixup)
{
    if (pSection != NULL && !CORCOMPILE_IS_FIXUP_TAGGED(fixup, pSection))
    {
        TempBuffer buf;
        if (pSection->Type == CORCOMPILE_IMPORT_TYPE_TYPE_HANDLE)
        {
            TypeHandleToString(TypeHandle::FromTAddr((TADDR)fixup), buf);
        }
        else
        if (pSection->Type == CORCOMPILE_IMPORT_TYPE_METHOD_HANDLE)
        {
            MethodDescToString(PTR_MethodDesc((TADDR)fixup), buf);
        }
        else
        {
            _ASSERTE(!"Unknown Type");
            IfFailThrow(E_FAIL);
        }
        m_display->WriteElementStringW( "FixupTargetName", (const WCHAR*)buf );
        return;
    }

    RVA rva = CORCOMPILE_UNTAG_TOKEN(fixup);

    WriteElementsFixupTargetAndName(rva);
}

const NativeImageDumper::EnumMnemonics s_EncodeMethodSigFlags[] =
{
#define EMS_ENTRY(f) NativeImageDumper::EnumMnemonics(ENCODE_METHOD_SIG_ ## f, W(#f))
    EMS_ENTRY(UnboxingStub),
    EMS_ENTRY(InstantiatingStub),
    EMS_ENTRY(MethodInstantiation),
    EMS_ENTRY(SlotInsteadOfToken),
    EMS_ENTRY(MemberRefToken),
    EMS_ENTRY(Constrained),
    EMS_ENTRY(OwnerType),
#undef EMS_ENTRY
};

void NativeImageDumper::FixupBlobToString(RVA rva, SString& buf)
{
    PTR_CCOR_SIGNATURE sig = (TADDR) m_decoder.GetRvaData(rva);
    BYTE kind = *sig++;

    CorTokenType tkType = (CorTokenType)0;

    IMetaDataImport2 * pImport = m_import;

    if (kind & ENCODE_MODULE_OVERRIDE)
    {
        Import *import = OpenImport(DacSigUncompressData(sig));
        kind &= ~ENCODE_MODULE_OVERRIDE;

        Dependency *pDep = import->dependency;
        if (pDep == NULL)
        {
            return;
        }

        pImport = pDep->pImport;

        _ASSERTE(pImport != NULL);

        // print assembly/module info

        mdToken realRef =
            MapAssemblyRefToManifest(TokenFromRid(import->index,
                                                    mdtAssemblyRef),
                                        m_assemblyImport);
        AppendToken(realRef, buf, m_manifestImport);
        buf.Append( W(" ") );
    }

    // print further info

    mdToken token;

    switch (kind)
    {
    case ENCODE_MODULE_HANDLE:
        // No further info
        break;

    case ENCODE_TYPE_HANDLE:
    EncodeType:
        if (pImport != NULL)
            TypeToString(sig, buf, pImport);
        else
            buf.Append( W("<unresolved type> ") );

        break;

    case ENCODE_METHOD_HANDLE:
    EncodeMethod:
        {
            //Flags are first
            DWORD methodFlags = DacSigUncompressData(sig);

            // If the type portion for this generic method signature
            // is from a different module then both the generic type and the
            // generic method tokens are interpreted in the context of that module,
            // and not the current import.  This is returned by TypeToString.
            //
            IMetaDataImport2 * pMethodImport = pImport;
            if (pImport != NULL)
            {
                if (methodFlags & ENCODE_METHOD_SIG_OwnerType)
                {
                    pMethodImport = TypeToString(sig, buf, pImport);
                }
            }
            else
            {
                buf.Append( W("<unresolved method signature>") );
                break;
            }

            //If we have SlotInsteadOfToken set then this is a slot number (i.e. for an array)
            if( methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken )
            {
                buf.AppendPrintf( W(" method slot %d"), DacSigUncompressData(sig) );
            }
            else
            {
                // decode the methodToken (a rid is encoded)
                RID rid = DacSigUncompressData(sig);

                mdMethodDef methodToken = ((methodFlags & ENCODE_METHOD_SIG_MemberRefToken) ? mdtMemberRef : mdtMethodDef) | rid;

                buf.Append( W(" ") );

                // Get the full signature of method from external module
                // Need temporary buffer because method name will be inserted
                // in between the signature

                TempBuffer tempName;

                AppendTokenName( methodToken, tempName, pMethodImport );

                if( methodFlags & ENCODE_METHOD_SIG_MethodInstantiation )
                {
                    //for each generic arg, there is a type handle.
                    ULONG numParams = DacSigUncompressData(sig);

                    tempName.Append( W("<") );
                    for( unsigned i = 0;i < numParams; ++i )
                    {
                        if( i != 0 )
                            tempName.Append( W(", ") );

                        // switch back to using pImport to resolve tokens
                        TypeToString(sig, tempName, pImport);
                    }
                    tempName.Append( W(">") );
                }

                PCCOR_SIGNATURE pvSigBlob;
                ULONG cbSigBlob;

                if (methodFlags & ENCODE_METHOD_SIG_MemberRefToken)
                {
                    IfFailThrow(pMethodImport->GetMemberRefProps(methodToken,
                        NULL,
                        NULL,
                        0,
                        NULL,
                        &pvSigBlob,
                        &cbSigBlob));
                }
                else
                {
                    IfFailThrow(pMethodImport->GetMethodProps(methodToken,
                        NULL,
                        NULL,
                        0,
                        NULL,
                        NULL,
                        &pvSigBlob,
                        &cbSigBlob,
                        NULL,
                        NULL));
                }

                CQuickBytes prettySig;
                ReleaseHolder<IMDInternalImport> pInternal;
                IfFailThrow(GetMDInternalInterfaceFromPublic(pMethodImport, IID_IMDInternalImport,
                    (void**)&pInternal));
                StackScratchBuffer buffer;
                const ANSI * ansi = tempName.GetANSI(buffer);
                ansi = PrettyPrintSig(pvSigBlob, cbSigBlob, ansi, &prettySig, pInternal, NULL);
                tempName.SetANSI( ansi );
                buf.Append(tempName);
            }

            buf.Append( W(" flags=(") );
            EnumFlagsToString( methodFlags, s_EncodeMethodSigFlags, _countof(s_EncodeMethodSigFlags),
                               W(", "), buf );
            buf.Append( W(")") );
        }
        break;

    case ENCODE_FIELD_HANDLE:
    EncodeField:
        {
            //Flags are first
            DWORD fieldFlags = DacSigUncompressData(sig);

            IMetaDataImport2 * pFieldImport = pImport;
            if (pImport != NULL)
            {
                if (fieldFlags & ENCODE_FIELD_SIG_OwnerType)
                {
                    pFieldImport = TypeToString(sig, buf, pImport);
                }
            }
            else
                buf.Append( W("<unresolved type>") );

            if (fieldFlags & ENCODE_FIELD_SIG_IndexInsteadOfToken)
            {
                buf.AppendPrintf( W(" field index %d"), DacSigUncompressData(sig) );
            }
            else
            {
                // decode the methodToken (a rid is encoded)
                RID rid = DacSigUncompressData(sig);

                mdMethodDef fieldToken = ((fieldFlags & ENCODE_FIELD_SIG_MemberRefToken) ? mdtMemberRef : mdtFieldDef) | rid;

                buf.Append( W(" ") );

                AppendTokenName( fieldToken, buf, pFieldImport );
            }
        }
        break;

    case ENCODE_STRING_HANDLE:
        token = TokenFromRid(DacSigUncompressData(sig), mdtString);
        if (pImport != NULL)
            AppendToken(token, buf, pImport);
        else
            buf.AppendPrintf( W("<unresolved token %d>"), token );
        break;

    case ENCODE_VARARGS_SIG:
        tkType = mdtFieldDef;
        goto DataToTokenCore;
    case ENCODE_VARARGS_METHODREF:
        tkType = mdtMemberRef;
        goto DataToTokenCore;
    case ENCODE_VARARGS_METHODDEF:
        tkType = mdtMemberRef;
        goto DataToTokenCore;
DataToTokenCore:
        token = TokenFromRid(DacSigUncompressData(sig), tkType);
        if (pImport != NULL)
            AppendToken(token, buf, pImport);
        else
            buf.AppendPrintf( "<unresolved token %d>", token );
        break;

    case ENCODE_METHOD_ENTRY:
        buf.Append( W("Entrypoint for ") );
        goto EncodeMethod;

    case ENCODE_METHOD_ENTRY_DEF_TOKEN:
        {
            buf.Append( W("Entrypoint for ") );
            token = TokenFromRid(DacSigUncompressData(sig), mdtMethodDef);
            AppendTokenName(token, buf, pImport);
        }
        break;

    case ENCODE_METHOD_ENTRY_REF_TOKEN:
        {
            buf.Append( W("Entrypoint for ref ") );
            token = TokenFromRid(DacSigUncompressData(sig), mdtMemberRef);
            AppendTokenName(token, buf, pImport);
        }
        break;

    case ENCODE_VIRTUAL_ENTRY:
        buf.Append( W("Entrypoint for ") );
        goto EncodeMethod;

    case ENCODE_VIRTUAL_ENTRY_DEF_TOKEN:
        {
            buf.Append( W("Virtual call for ") );
            token = TokenFromRid(DacSigUncompressData(sig), mdtMethodDef);
            AppendTokenName(token, buf, pImport);
        }
        break;

    case ENCODE_VIRTUAL_ENTRY_REF_TOKEN:
        {
            buf.Append( W("Virtual call for ref ") );
            token = TokenFromRid(DacSigUncompressData(sig), mdtMemberRef);
            AppendTokenName(token, buf, pImport);
        }
        break;

    case ENCODE_VIRTUAL_ENTRY_SLOT:
        {
            buf.Append( W("Virtual call for ") );
            int slot = DacSigUncompressData(sig);
            buf.AppendPrintf( W("slot %d "), slot );
            goto EncodeType;
        }

    case ENCODE_MODULE_ID_FOR_STATICS:
        buf.Append( W("Module For Statics") );
        // No further info
        break;

    case ENCODE_MODULE_ID_FOR_GENERIC_STATICS:
        buf.Append( W("Module For Statics for ") );
        goto EncodeType;

    case ENCODE_CLASS_ID_FOR_STATICS:
        buf.Append( W("Statics ID for ") );
        goto EncodeType;

    case ENCODE_STATIC_FIELD_ADDRESS:
        buf.Append( W("Static field address for ") );
        goto EncodeField;

    case ENCODE_SYNC_LOCK:
        buf.Append( W("Synchronization handle for ") );
        break;

    case ENCODE_INDIRECT_PINVOKE_TARGET:
        buf.Append( W("Indirect P/Invoke target for ") );
        break;

    case ENCODE_PINVOKE_TARGET:
        buf.Append( W("P/Invoke target for ") );
        break;

    case ENCODE_PROFILING_HANDLE:
        buf.Append( W("Profiling handle for ") );
        goto EncodeMethod;

    case ENCODE_ACTIVE_DEPENDENCY:
        {
            buf.Append( W("Active dependency for  ") );

            int targetModuleIndex = DacSigUncompressData(sig);
            Import *targetImport = OpenImport(targetModuleIndex);

            mdToken realRef =
                MapAssemblyRefToManifest(TokenFromRid(targetImport->index,
                                                        mdtAssemblyRef),
                                            m_assemblyImport);
            AppendToken(realRef, buf, m_manifestImport);
            buf.Append( W(" ") );
        }
        break;

    default:
        buf.Append( W("Unknown fixup kind") );
        _ASSERTE(!"Unknown fixup kind");
    }
}

void NativeImageDumper::WriteElementsFixupTargetAndName(RVA rva)
{
    if( rva == NULL )
    {
        /* XXX Tue 04/11/2006
         * This should only happen for static fields.  If the field is
         * unaligned, we need an extra cell for an indirection.
         */
        m_display->WriteElementPointer( "FixupTargetValue", NULL );
        m_display->WriteElementStringW( "FixupTargetName", W("NULL") );
        return;
    }

    m_display->WriteElementPointer( "FixupTargetValue", RvaToDisplay(rva) );

    TempBuffer buf;
    FixupBlobToString(rva, buf);

    m_display->WriteElementStringW( "FixupTargetName", (const WCHAR*)buf );
}

NativeImageDumper::Dependency * NativeImageDumper::GetDependency(mdAssemblyRef token, IMetaDataAssemblyImport *pImport)
{
    if (RidFromToken(token) == 0)
        return OpenDependency(0);

    if (pImport == NULL)
        pImport = m_assemblyImport;

    // Need to map from IL token to manifest token
    mdAssemblyRef manifestToken = MapAssemblyRefToManifest(token, pImport);

    if( manifestToken == mdAssemblyNil )
    {
        //this is "self"
        return OpenDependency(0);
    }

    COUNT_T count;
    PTR_CORCOMPILE_DEPENDENCY deps(TO_TADDR(m_decoder.GetNativeDependencies(&count)));

    for (COUNT_T i = 0; i < count; i++)
    {
        if (deps[i].dwAssemblyRef == manifestToken)
            return OpenDependency(i+1);
    }

    TempBuffer buf;
    AppendTokenName(manifestToken, buf, m_manifestImport);
    m_display->ErrorPrintF("Error: unlisted assembly dependency %S\n", (const WCHAR*)buf);

    return NULL;
}

mdAssemblyRef NativeImageDumper::MapAssemblyRefToManifest(mdAssemblyRef token, IMetaDataAssemblyImport *pAssemblyImport)
{
    // Reference may be to self
    if (TypeFromToken(token) == mdtAssembly)
        return token;

    // Additional tokens not originally present overflow to manifest automatically during emit
    /* REVISIT_TODO Tue 01/31/2006
     * Factor this code out so that it is shared with the module index code in the CLR that looks
     * exactly thes same
     */
    //count the assembly refs.
    ULONG count = 0;

    HCORENUM iter = NULL;
    for (;;)
    {
        ULONG tokens = 0;
        mdAssemblyRef tmp;
        IfFailThrow(pAssemblyImport->EnumAssemblyRefs(&iter, &tmp, 1,
                                                      &tokens));
        if (tokens == 0)
            break;
        count ++;
    }
    pAssemblyImport->CloseEnum(iter);

    if( RidFromToken(token) > count )
    {
        //out of range import.  This means that it has spilled over.  Subtract
        //off the max number of assembly refs and return it as a manifest
        //token.
        return token - (count + 1);
    }

    ULONG cchName;
    ASSEMBLYMETADATA metadata;

    ZeroMemory(&metadata, sizeof(metadata));

    IfFailThrow(pAssemblyImport->GetAssemblyRefProps(token, NULL, NULL,
                                                     NULL, 0, &cchName,
                                                     &metadata, NULL, NULL,
                                                     NULL));

    LPWSTR szAssemblyName           = NULL;

    if (cchName > 0)
        szAssemblyName = (LPWSTR) _alloca(cchName * sizeof(WCHAR));

    if (metadata.cbLocale > 0)
        metadata.szLocale = (LPWSTR) _alloca(metadata.cbLocale * sizeof(WCHAR));
    if (metadata.ulProcessor > 0)
        metadata.rProcessor = (DWORD*) _alloca(metadata.ulProcessor * sizeof(DWORD));
    if (metadata.ulOS > 0)
        metadata.rOS = (OSINFO*) _alloca(metadata.ulOS * sizeof(OSINFO));

    const void *pbPublicKey;
    ULONG cbPublicKey;
    DWORD flags;
    const void *pbHashValue;
    ULONG cbHashValue;


    IfFailThrow(pAssemblyImport->GetAssemblyRefProps(token, &pbPublicKey, &cbPublicKey,
                                                     szAssemblyName, cchName, NULL,
                                                     &metadata, &pbHashValue, &cbHashValue,
                                                     &flags));

    //Notice that we're searching for the provided metadata for the dependency info and then looking in the
    //image we're dumping for the dependency.
    //
    //Also, sometimes we find "self" in these searches.  If so, return mdAssemblyDefNil as a canary value.

    if( !wcscmp(szAssemblyName, m_name) )
    {
        //we need "self".
        return mdAssemblyNil;
    }

    mdAssemblyRef ret = mdAssemblyRefNil;
    /*HCORENUM*/ iter = NULL;
    for(;;)
    {
        //Walk through all the assemblyRefs and search for a match.  I would use DefineAssemblyRef here, but
        //if I do it will create an assemblyRef is one is not found.  Then I fail in a bad place.  This
        //way I can fail in a less bad place.
        mdAssemblyRef currentRef;
        //ULONG count;
        IfFailThrow(m_manifestAssemblyImport->EnumAssemblyRefs(&iter, &currentRef, 1, &count));
        if( 0 == count )
            break;

        //get the information about the assembly ref and compare.
        const void * publicKeyToken;
        ULONG pktSize = 0;
        WCHAR name[128];
        /*ULONG*/ cchName = _countof(name);
        ASSEMBLYMETADATA curMD = {0};

        IfFailThrow(m_manifestAssemblyImport->GetAssemblyRefProps(currentRef, &publicKeyToken, &pktSize, name,
                                                                  cchName, &cchName, &curMD,
                                                                  NULL /*ppbHashValue*/, NULL/*pcbHashValue*/,
                                                                  NULL/*pdwAssemblyRefFlags*/));
        if( !wcscmp(name, szAssemblyName) )
        {
            if( cbPublicKey == pktSize && !memcmp(pbPublicKey, publicKeyToken, pktSize)
                && curMD.usMajorVersion == metadata.usMajorVersion
                && curMD.usMinorVersion == metadata.usMinorVersion)
            {
                ret = currentRef;
                break;
            }
            else if (wcscmp(szAssemblyName, CoreLibName_W) == 0)
            {
                // CoreLib is special - version number and public key token are ignored.
                ret = currentRef;
                break;
            }
            else if (metadata.usMajorVersion == 255 &&
                     metadata.usMinorVersion == 255 &&
                     metadata.usBuildNumber == 255 &&
                     metadata.usRevisionNumber == 255)
            {
                // WinMDs encode all assemblyrefs with version 255.255.255.255 including CLR assembly dependencies (corelib, System).
                ret = currentRef;
            }
            else
            {
                //there was an assembly with the correct name, but with the wrong version number.  Let the
                //user know.
                m_display->ErrorPrintF("MapAssemblyRefToManifest: found %S with version %d.%d in manifest.  Wanted version %d.%d.\n", szAssemblyName, curMD.usMajorVersion, curMD.usMinorVersion, metadata.usMajorVersion, metadata.usMinorVersion);
                // TritonTODO: why?
                ret = currentRef;
                break;
            }

        }
    }
    pAssemblyImport->CloseEnum(iter);
    if( ret == mdAssemblyRefNil )
    {
        TempBuffer pkt;
        appendByteArray(pkt, (const BYTE*)pbPublicKey, cbPublicKey);
        m_display->ErrorPrintF("MapAssemblyRefToManifest could not find token for %S, Version=%d.%d, PublicKeyToken=%S\n", szAssemblyName, metadata.usMajorVersion, metadata.usMinorVersion, (const WCHAR *)pkt);
        _ASSERTE(!"MapAssemblyRefToManifest failed to find a match");
    }

    return ret;
}

NativeImageDumper::Import * NativeImageDumper::OpenImport(int i)
{
    if (m_imports == NULL)
    {
        COUNT_T count;
        m_decoder.GetNativeDependencies(&count);
        m_numImports = count;
        m_imports = new Import [count];
        ZeroMemory(m_imports, count * sizeof(m_imports[0]));
    }

    if (m_imports[i].index == 0)
    {
        //GetNativeImportFromIndex returns a host pointer.
        m_imports[i].index = i;

        /*
        mdToken tok = TokenFromRid(entry->index, mdtAssemblyRef);
        Dependency * dependency = GetDependency( MapAssemblyRefToManifest(tok,
        */
        Dependency *dependency = GetDependency(TokenFromRid(i, mdtAssemblyRef));
        m_imports[i].dependency = dependency;
        _ASSERTE(dependency); //Why can this be null?

    }

    return &m_imports[i];
}


const NativeImageDumper::Dependency *NativeImageDumper::GetDependencyForFixup(RVA rva)
{
    PTR_CCOR_SIGNATURE sig = (TADDR) m_decoder.GetRvaData(rva);
    if (*sig++ & ENCODE_MODULE_OVERRIDE)
    {
        unsigned idx = DacSigUncompressData(sig);

        _ASSERTE(idx >= 0 && idx < m_numImports);
        return OpenImport(idx)->dependency;
    }

    return &m_dependencies[0];
}


void NativeImageDumper::AppendToken(mdToken token, SString& buf)
{
    return NativeImageDumper::AppendToken(token, buf, NULL);
}
void NativeImageDumper::AppendToken(mdToken token, SString& buf,
                                    IMetaDataImport2 *pImport)
{
    IF_OPT(DISABLE_NAMES)
    {
        buf.Append( W("Disabled") );
        return;
    }
    switch (TypeFromToken(token))
    {
    case mdtTypeDef:
        buf.Append( W("TypeDef ") );
        break;

    case mdtTypeRef:
        buf.Append( W("TypeRef ") );
        break;

    case mdtTypeSpec:
        buf.Append( W("TypeRef ") );
        break;

    case mdtFieldDef:
        buf.Append( W("FieldDef "));
        break;

    case mdtMethodDef:
        buf.Append( W("MethodDef ") );
        break;

    case mdtMemberRef:
        buf.Append( W("MemberRef ") );
        break;

    case mdtAssemblyRef:
        buf.Append( W("AssemblyRef ") );
        break;

    case mdtFile:
        buf.Append( W("File ") );
        break;

    case mdtString:
        buf.Append( W("String ") );
        break;

    case mdtSignature:
        buf.Append( W("Signature ") );
        break;

    }
    if( RidFromToken(token) == mdTokenNil )
        buf.Append( W("Nil") );
    else
        AppendTokenName(token, buf, pImport);
}

NativeImageDumper::Dependency *NativeImageDumper::OpenDependency(int index)
{
    CORCOMPILE_VERSION_INFO *info = m_decoder.GetNativeVersionInfo();

    if (m_dependencies == NULL)
    {
        COUNT_T count;
        m_decoder.GetNativeDependencies(&count);

        // Add one for self
        count++;

        m_numDependencies = count;
        m_dependencies = new Dependency [count];
        ZeroMemory(m_dependencies, count * sizeof (Dependency));
    }

    if (m_dependencies[index].entry == NULL)
    {
        CORCOMPILE_DEPENDENCY *entry;

        if (index == 0)
        {
            //  Make dummy entry for self
            entry = &m_self;
            m_self.dwAssemblyRef = TokenFromRid(1, mdtAssembly);
            m_self.dwAssemblyDef = TokenFromRid(1, mdtAssembly);
            m_self.signAssemblyDef = info->sourceAssembly;
            m_manifestImport->GetScopeProps(NULL, NULL, 0, &m_self.signNativeImage);
            m_dependencies[index].pLoadedAddress = dac_cast<TADDR>(m_baseAddress);
            m_dependencies[index].pPreferredBase =
                TO_TADDR(m_decoder.GetNativePreferredBase());
            m_dependencies[index].size = m_imageSize;
            m_dependencies[index].pImport = m_import;
            m_dependencies[index].pMetadataStartTarget =
                m_MetadataStartTarget;
            m_dependencies[index].pMetadataStartHost =
                m_MetadataStartHost;
            m_dependencies[index].MetadataSize = m_MetadataSize;
            m_dependencies[index].pModule =
                (TADDR)m_decoder.GetPersistedModuleImage();
            m_dependencies[index].fIsHardbound = TRUE;
            _ASSERTE( (m_dependencies[index].pModule
                       > m_dependencies[index].pLoadedAddress)
                      && (m_dependencies[index].pModule
                          < m_dependencies[index].pLoadedAddress
                          + m_dependencies[index].size) );
            // patch the Module vtable so that the DAC is able to instantiate it
            TADDR vtbl = DacGetTargetVtForHostVt(Module::VPtrHostVTable(), true);
            DacWriteAll( m_dependencies[index].pModule.GetAddr(), &vtbl, sizeof(vtbl), false );
        }
        else
        {
            COUNT_T numDeps;
            PTR_CORCOMPILE_DEPENDENCY deps(TO_TADDR(m_decoder.GetNativeDependencies(&numDeps)));

            entry = deps + (index-1);

            //load the dependency, get the pointer, and use the PEDecoder
            //to open the metadata.

            TempBuffer buf;
            TADDR loadedBase;
            /* REVISIT_TODO Tue 11/22/2005
             * Is this the right name?
             */
            Dependency& dependency = m_dependencies[index];
            AppendTokenName(entry->dwAssemblyRef, buf, m_manifestImport, true);
            bool isHardBound = !!(entry->signNativeImage != INVALID_NGEN_SIGNATURE);
            SString corelibStr(SString::Literal, CoreLibName_W);
            bool isCoreLib = (0 == buf.Compare( corelibStr ));
            dependency.fIsHardbound = isHardBound;
            wcscpy_s(dependency.name, _countof(dependency.name),
                     (const WCHAR*)buf);
            if( isHardBound )
            {
                IfFailThrow(m_librarySupport->LoadHardboundDependency((const WCHAR*)buf,
                                                                      entry->signNativeImage, &loadedBase));

                dependency.pLoadedAddress = loadedBase;
            }
            else
            {
                ASSEMBLYMETADATA asmData = {0};
                const void * hashValue;
                ULONG hashLength, size, flags;
                IfFailThrow(m_manifestAssemblyImport->GetAssemblyRefProps(entry->dwAssemblyRef, &hashValue, &hashLength, bigBuffer, bigBufferSize, &size, &asmData, NULL, NULL, &flags));


                HRESULT hr =
                    m_librarySupport->LoadSoftboundDependency((const WCHAR*)buf,
                                                              (const BYTE*)&asmData, (const BYTE*)hashValue, hashLength,
                                                              &loadedBase);
                if( FAILED(hr) )
                {
                    TempBuffer pkt;
                    if( hashLength > 0 )
                    {
                        appendByteArray(pkt, (BYTE*)hashValue, hashLength);
                    }
                    else
                    {
                        pkt.Set( W("<No Hash>") );
                    }
                    //try to continue without loading this softbound
                    //dependency.
                    m_display->ErrorPrintF( "WARNING Failed to load softbound dependency:\n\t%S,Version=%d.%d.0.0,PublicKeyToken=%S.\n\tAttempting to continue.  May crash later in due to missing metadata\n",
                                            (const WCHAR *)buf, asmData.usMajorVersion,
                                            asmData.usMinorVersion, (const WCHAR *)pkt );
                    m_dependencies[index].entry = entry;
                    return &m_dependencies[index];

                }
                //save this off to the side so OpenImport can find the metadata.
                m_dependencies[index].pLoadedAddress = loadedBase;
            }
            /* REVISIT_TODO Wed 11/23/2005
             * Refactor this with OpenMetadata from above.
             */
            //now load the metadata from the new image.
            PEDecoder decoder(dac_cast<PTR_VOID>(loadedBase));
            if( isHardBound )
            {
                dependency.pPreferredBase =
                    TO_TADDR(decoder.GetNativePreferredBase());
                dependency.size = decoder.Has32BitNTHeaders() ?
                    decoder.GetNTHeaders32()->OptionalHeader.SizeOfImage :
                    decoder.GetNTHeaders64()->OptionalHeader.SizeOfImage;
            }
            ReleaseHolder<IMetaDataDispenserEx> pDispenser;
            IfFailThrow(MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
                                             IID_IMetaDataDispenserEx,
                                             (void **) &pDispenser));

            VARIANT opt;
            IfFailThrow(pDispenser->GetOption(MetaDataCheckDuplicatesFor,
                                              &opt));
            V_UI4(&opt) |= MDDupAssemblyRef | MDDupFile;
            IfFailThrow(pDispenser->SetOption(MetaDataCheckDuplicatesFor,
                                              &opt));
            if( decoder.HasNativeHeader() )
            {
                dependency.pModule =
                    TO_TADDR(decoder.GetPersistedModuleImage());
                _ASSERTE( (PTR_TO_TADDR(dependency.pModule) > loadedBase)
                          && (PTR_TO_TADDR(dependency.pModule) < loadedBase +
                              decoder.GetSize()) );
                // patch the Module vtable so that the DAC is able to instantiate it
                TADDR vtbl = DacGetTargetVtForHostVt(Module::VPtrHostVTable(), true);
                DacWriteAll( m_dependencies[index].pModule.GetAddr(), &vtbl, sizeof(vtbl), false );
            }
            else
            {
                dependency.pModule = NULL;
            }

            const void * data;
            COUNT_T size;

            DACCOP_IGNORE(CastBetweenAddressSpaces,"nidump is in-proc and doesn't maintain a clean separation of address spaces (target and host are the same.");
            data = reinterpret_cast<void*>(dac_cast<TADDR>(decoder.GetMetadata(&size)));

            dependency.pMetadataStartTarget = TO_TADDR(data);
            dependency.MetadataSize = size;
            data = PTR_READ(TO_TADDR(data), size);
            dependency.pMetadataStartHost = TO_TADDR(data);
            IfFailThrow(pDispenser->OpenScopeOnMemory(data, size,
                                                      ofRead,
                                                      IID_IMetaDataImport2,
                                                      (IUnknown **) &dependency.pImport));
            dependency.fIsCoreLib = isCoreLib;
        }

        m_dependencies[index].entry = entry;

    }

    return &m_dependencies[index];
}

IMetaDataImport2* NativeImageDumper::TypeToString(PTR_CCOR_SIGNATURE &sig, SString& buf)
{
    return TypeToString(sig, buf, NULL);
}
#if 0
void NativeImageDumper::TypeToString(PTR_CCOR_SIGNATURE &sig,
                                  IMetaDataImport2 *pImport)
{
    CQuickBytes tmp;

    if (pImport == NULL)
        pImport = m_import;

    LPCWSTR type = PrettyPrintSig( sig, INT_MAX, W(""), &tmp, pImport );
    _ASSERTE(type);
    m_display->ErrorPrintF( "%S", type );
}
#endif

IMetaDataImport2 * NativeImageDumper::TypeToString(PTR_CCOR_SIGNATURE &sig,
                                                   SString& buf,
                                                   IMetaDataImport2 *pImport,
                                                   IMetaDataImport2 *pOrigImport /* =NULL */)

{
    IF_OPT(DISABLE_NAMES)
    {
        buf.Append( W("Disabled") );
        return pImport;
    }

    if (pImport == NULL)
        pImport = m_import;
    if (pOrigImport == NULL)
        pOrigImport = pImport;

    IMetaDataImport2 * pRet = pImport;
#define TYPEINFO(enumName, classSpace, className, size, gcType, isArray, isPrim, isFloat, isModifier, isGenVar) \
     className,
    static const char *elementNames[] = {
#include "cortypeinfo.h"
    };
#undef TYPEINFO

    CorElementType type = DacSigUncompressElementType(sig);

    if (type == (CorElementType) ELEMENT_TYPE_MODULE_ZAPSIG)
    {
        unsigned idx = DacSigUncompressData(sig);
        buf.AppendPrintf( W("module %d "), idx );
        //switch module
        const Import * import = OpenImport(idx);
        pImport = import->dependency->pImport;

        //if there was a module switch, return the import for the new module.
        //This is useful for singatures, where the module index applies to
        //subsequent tokens.
        pRet = pImport;

        type = DacSigUncompressElementType(sig);
    }
    if (type >= 0 && (size_t)type < _countof(elementNames)
             && elementNames[type] != NULL)
    {
        buf.AppendPrintf( "%s", elementNames[type] );
    }
    else switch ((DWORD)type)
    {
    case ELEMENT_TYPE_CANON_ZAPSIG:
        buf.Append( W("System.__Canon") );
        break;

    case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
        {
            buf.Append( W("native ") );
            TypeToString(sig, buf, pImport);
        }
        break;

    case ELEMENT_TYPE_VALUETYPE:
    case ELEMENT_TYPE_CLASS:
        {
            if (type == ELEMENT_TYPE_VALUETYPE)
                buf.Append( W("struct ") );

            mdToken token = DacSigUncompressToken(sig);
            AppendTokenName(token, buf, pImport);
        }
        break;

    case ELEMENT_TYPE_SZARRAY:
        TypeToString(sig, buf, pImport);
        buf.Append( W("[]") );
        break;

    case ELEMENT_TYPE_ARRAY:
        {
            TypeToString(sig, buf, pImport, pOrigImport);
            unsigned rank = DacSigUncompressData(sig);
            if (rank == 0)
                buf.Append( W("[??]") );
            else
            {
                size_t cbLowerBounds;
                if (!ClrSafeInt<size_t>::multiply(rank, 2*sizeof(int), cbLowerBounds/* passed by ref */))
                    ThrowHR(COR_E_OVERFLOW);
                int* lowerBounds = (int*) _alloca(cbLowerBounds);
                int* sizes       = &lowerBounds[rank];
                memset(lowerBounds, 0, sizeof(int)*2*rank);

                unsigned numSizes = DacSigUncompressData(sig);
                _ASSERTE(numSizes <= rank);
                unsigned int i;
                for(i =0; i < numSizes; i++)
                    sizes[i] = DacSigUncompressData(sig);

                unsigned numLowBounds = DacSigUncompressData(sig);
                _ASSERTE(numLowBounds <= rank);
                for(i = 0; i < numLowBounds; i++)
                    lowerBounds[i] = DacSigUncompressData(sig);

                buf.Append(W("["));
                for(i = 0; i < rank; i++)
                {
                    if (sizes[i] != 0 && lowerBounds[i] != 0)
                    {
                        buf.AppendPrintf( W("%d ..."), lowerBounds[i] );
                        if (sizes[i] != 0)
                            buf.AppendPrintf( W("%d"),
                                              lowerBounds[i] + sizes[i]
                                              + 1 );
                    }
                    if (i < rank-1)
                        buf.Append( W(",") );
                }
                buf.Append( W("]") );
            }
        }
        break;

    case ELEMENT_TYPE_MVAR:
        buf.Append( W("!") );
        // fall through
    case ELEMENT_TYPE_VAR:
        buf.AppendPrintf( W("!%d"), DacSigUncompressData(sig));
        break;

    case ELEMENT_TYPE_VAR_ZAPSIG:
        {
            buf.Append( W("var ") );

            mdToken token = TokenFromRid(DacSigUncompressData(sig), mdtGenericParam);
            AppendTokenName(token, buf, pImport);
        }
        break;

    case ELEMENT_TYPE_GENERICINST:
        {
            TypeToString(sig, buf, pImport, pOrigImport);
            unsigned ntypars = DacSigUncompressData(sig);
            buf.Append( W("<") );
            for (unsigned i = 0; i < ntypars; i++)
            {
                if (i > 0)
                    buf.Append( W(",") );
                // switch pImport back to our original Metadata importer
                TypeToString(sig, buf, pOrigImport, pOrigImport);
            }
            buf.Append( W(">") );
        }
        break;

    case ELEMENT_TYPE_FNPTR:
        buf.Append( W("(fnptr)") );
        break;

        // Modifiers or depedant types
    case ELEMENT_TYPE_PINNED:
        TypeToString(sig, buf, pImport, pOrigImport);
        buf.Append( W(" pinned") );
        break;

    case ELEMENT_TYPE_PTR:
        TypeToString(sig, buf, pImport, pOrigImport);
        buf.Append( W("*") );
        break;

    case ELEMENT_TYPE_BYREF:
        TypeToString(sig, buf, pImport, pOrigImport);
        buf.Append( W("&") );
        break;

    case ELEMENT_TYPE_SENTINEL:
    case ELEMENT_TYPE_END:
    default:
        _ASSERTE(!"Unknown Type");
        IfFailThrow(E_FAIL);
        break;
    }
    return pRet;
}

void NativeImageDumper::DumpMethods(PTR_Module module)
{
    COUNT_T hotCodeSize;
    PCODE hotCode = m_decoder.GetNativeHotCode(&hotCodeSize);


    COUNT_T codeSize;
    PCODE code = m_decoder.GetNativeCode(&codeSize);

    COUNT_T coldCodeSize;
    PCODE coldCode = m_decoder.GetNativeColdCode(&coldCodeSize);

    DisplayStartCategory( "Code", METHODS );
    DisplayWriteElementAddress( "HotCode", DataPtrToDisplay(hotCode),
                                hotCodeSize, METHODS );

    DisplayWriteElementAddress( "UnprofiledCode",
                                DataPtrToDisplay(code),
                                codeSize, METHODS );
    DisplayWriteElementAddress( "ColdCode",
                                DataPtrToDisplay(coldCode),
                                coldCodeSize, METHODS );

    PTR_CORCOMPILE_CODE_MANAGER_ENTRY codeEntry(m_decoder.GetNativeCodeManagerTable());

    DisplayWriteElementAddress( "ROData",
                                RvaToDisplay(codeEntry->ROData.VirtualAddress),
                                codeEntry->ROData.Size, METHODS );

    DisplayWriteElementAddress( "HotCommonCode",
                                DataPtrToDisplay(hotCode),
                                codeEntry->HotIBCMethodOffset, METHODS );

    DisplayWriteElementAddress( "HotIBCMethodCode",
                                DataPtrToDisplay(hotCode
                                    + codeEntry->HotIBCMethodOffset),
                                codeEntry->HotGenericsMethodOffset
                                    - codeEntry->HotIBCMethodOffset,
                                METHODS );

    DisplayWriteElementAddress( "HotGenericsMethodCode",
                                DataPtrToDisplay(hotCode
                                    + codeEntry->HotGenericsMethodOffset),
                                hotCodeSize - codeEntry->HotGenericsMethodOffset,
                                METHODS );

    DisplayWriteElementAddress( "ColdIBCMethodCode",
                                DataPtrToDisplay(coldCode),
                                codeEntry->ColdUntrainedMethodOffset,
                                METHODS );

    MethodIterator mi(module, &m_decoder);

    DisplayStartArray( "Methods", NULL, METHODS );

    while( mi.Next() )
    {
        DumpCompleteMethod( module, mi );
    }

    DisplayEndArray( "Total Methods", METHODS ); //Methods

    /* REVISIT_TODO Wed 12/14/2005
     * I have this coverage read in here because there is some other data between the
     * methods in debug builds.  For now just whack the whole text section.  Go
     * back later and check out that I really got everything.
     */
    CoverageRead( hotCode, hotCodeSize );
    CoverageRead( coldCode, coldCodeSize );
#ifdef USE_CORCOMPILE_HEADER
    CoverageRead( hotCodeTable, hotCodeTableSize );
    CoverageRead( coldCodeTable, coldCodeTableSize );
#endif

    DisplayEndCategory( METHODS ); //Code

    //m_display->StartCategory( "Methods" );
}

static SString g_holdStringOutData;

static void stringOut( const char* fmt, ... )
{
    va_list args;
    va_start(args, fmt);
    g_holdStringOutData.AppendVPrintf(fmt, args);
    va_end(args);
}

static void nullStringOut( const char * fmt, ... ) { }

const NativeImageDumper::EnumMnemonics s_CorExceptionFlags[] =
{
#define CEF_ENTRY(f,v) NativeImageDumper::EnumMnemonics(f, v)
    CEF_ENTRY(COR_ILEXCEPTION_CLAUSE_NONE, W("none")),
    CEF_ENTRY(COR_ILEXCEPTION_CLAUSE_FILTER, W("filter")),
    CEF_ENTRY(COR_ILEXCEPTION_CLAUSE_FINALLY, W("finally")),
    CEF_ENTRY(COR_ILEXCEPTION_CLAUSE_FAULT, W("fault")),
    CEF_ENTRY(COR_ILEXCEPTION_CLAUSE_DUPLICATED, W("duplicated")),
#undef CEF_ENTRY
};

void NativeImageDumper::DumpCompleteMethod(PTR_Module module, MethodIterator& mi)
{
    PTR_MethodDesc md = mi.GetMethodDesc();

#ifdef FEATURE_EH_FUNCLETS
    PTR_RUNTIME_FUNCTION pRuntimeFunction = mi.GetRuntimeFunction();
#endif

    //Read the GCInfo to get the total method size.
    unsigned methodSize = 0;
    unsigned gcInfoSize = UINT_MAX;

    //parse GCInfo for size information.
    GCInfoToken gcInfoToken = mi.GetGCInfoToken();
    PTR_CBYTE gcInfo = dac_cast<PTR_CBYTE>(gcInfoToken.Info);

    void (* stringOutFn)(const char *, ...);
    IF_OPT(GC_INFO)
    {
        stringOutFn = stringOut;
    }
    else
    {
        stringOutFn = nullStringOut;
    }
    if (gcInfo != NULL)
    {
        PTR_CBYTE curGCInfoPtr = gcInfo;
        g_holdStringOutData.Clear();
        GCDump gcDump(gcInfoToken.Version);
        gcDump.gcPrintf = stringOutFn;
#if !defined(TARGET_X86) && defined(USE_GC_INFO_DECODER)
        GcInfoDecoder gcInfoDecoder(gcInfoToken, DECODE_CODE_LENGTH);
        methodSize = gcInfoDecoder.GetCodeLength();
#endif

        //dump the data to a string first so we can get the gcinfo size.
#ifdef TARGET_X86
        InfoHdr hdr;
        stringOutFn( "method info Block:\n" );
        curGCInfoPtr += gcDump.DumpInfoHdr(curGCInfoPtr, &hdr, &methodSize, 0);
        stringOutFn( "\n" );
#endif

        IF_OPT(METHODS)
        {
#ifdef TARGET_X86
            stringOutFn( "PointerTable:\n" );
            curGCInfoPtr += gcDump.DumpGCTable( curGCInfoPtr,
                                                hdr,
                                                methodSize, 0);
            gcInfoSize = curGCInfoPtr - gcInfo;
#elif defined(USE_GC_INFO_DECODER)
            stringOutFn( "PointerTable:\n" );
            curGCInfoPtr += gcDump.DumpGCTable( curGCInfoPtr,
                                                methodSize, 0);
            gcInfoSize = (unsigned)(curGCInfoPtr - gcInfo);
#endif
        }

        //data is output below.
    }

    TADDR hotCodePtr = mi.GetMethodStartAddress();
    TADDR coldCodePtr = mi.GetMethodColdStartAddress();

    size_t hotCodeSize = methodSize;
    size_t coldCodeSize = 0;

    if (coldCodePtr != NULL)
    {
        hotCodeSize = mi.GetHotCodeSize();
        coldCodeSize = methodSize - hotCodeSize;
    }

    _ASSERTE(!CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(md)));
    const Dependency* mdDep = GetDependencyFromMD(md);
    TempBuffer buffer;
    _ASSERTE(mdDep->pImport);
    MethodDescToString(md, buffer);

    DisplayStartElement( "Method", METHODS );
    DisplayWriteElementStringW( "Name", (const WCHAR *)buffer, METHODS );

    /* REVISIT_TODO Mon 10/24/2005
     * Do I have to annotate this?
     */
    DisplayWriteElementPointer("m_methodDesc",
                            DPtrToPreferredAddr(md),
                            METHODS);

    DisplayStartStructure( "m_gcInfo",
                           DPtrToPreferredAddr(gcInfo),
                           gcInfoSize,
                           METHODS );

    DisplayStartTextElement( "Contents", GC_INFO );
    DisplayWriteXmlTextBlock( ("%S", (const WCHAR *)g_holdStringOutData), GC_INFO );
    DisplayEndTextElement( GC_INFO ); //Contents

    DisplayEndStructure( METHODS ); //GCInfo

    PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE pExceptionInfoTable (PTR_TO_TADDR(module->GetNGenLayoutInfo()->m_ExceptionInfoLookupTable.StartAddress()));
    if (pExceptionInfoTable)
    {
        COUNT_T numLookupEntries = (COUNT_T) (module->GetNGenLayoutInfo()->m_ExceptionInfoLookupTable.Size() / sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY));
        DWORD methodStartRVA = m_decoder.GetDataRva(TO_TADDR(hotCodePtr));

        COUNT_T ehInfoSize = 0;
        DWORD exceptionInfoRVA = NativeExceptionInfoLookupTable::LookupExceptionInfoRVAForMethod(pExceptionInfoTable,
                                                                                                                      numLookupEntries,
                                                                                                                      methodStartRVA,
                                                                                                                      &ehInfoSize);

        if( exceptionInfoRVA != 0 )
        {
            PTR_CORCOMPILE_EXCEPTION_CLAUSE pExceptionInfoArray = dac_cast<PTR_CORCOMPILE_EXCEPTION_CLAUSE>(PTR_TO_TADDR(m_decoder.GetBase()) + exceptionInfoRVA);
            COUNT_T ehCount = ehInfoSize / sizeof(CORCOMPILE_EXCEPTION_CLAUSE);
            _ASSERTE(ehCount > 0);
            DisplayStartArray("EHClauses", NULL, METHODS );
            for( unsigned i = 0; i < ehCount; ++i )
            {
                PTR_CORCOMPILE_EXCEPTION_CLAUSE host = pExceptionInfoArray + i;

                DisplayStartStructure( "Clause", DPtrToPreferredAddr(host), sizeof(PTR_CORCOMPILE_EXCEPTION_CLAUSE), METHODS);
                DisplayWriteFieldEnumerated( Flags, host->Flags,
                    EE_ILEXCEPTION_CLAUSE,
                    s_CorExceptionFlags, W(", "),
                    METHODS );
                DisplayWriteFieldUInt( TryStartPC, host->TryStartPC,
                    EE_ILEXCEPTION_CLAUSE, METHODS );
                DisplayWriteFieldUInt( TryEndPC, host->TryEndPC,
                    EE_ILEXCEPTION_CLAUSE, METHODS );
                DisplayWriteFieldUInt( HandlerStartPC,
                    host->HandlerStartPC,
                    EE_ILEXCEPTION_CLAUSE, METHODS );
                DisplayWriteFieldUInt( HandlerEndPC,
                    host->HandlerEndPC,
                    EE_ILEXCEPTION_CLAUSE, METHODS );
                if( host->Flags & COR_ILEXCEPTION_CLAUSE_FILTER )
                {
                    DisplayWriteFieldUInt( FilterOffset, host->FilterOffset,
                        EE_ILEXCEPTION_CLAUSE, METHODS );
                }
                else if( !(host->Flags & (COR_ILEXCEPTION_CLAUSE_FAULT | COR_ILEXCEPTION_CLAUSE_FINALLY)) )
                {
                    WriteFieldMDTokenImport( ClassToken, host->ClassToken,
                        EE_ILEXCEPTION_CLAUSE, METHODS,
                        mdDep->pImport );
                }
                DisplayEndStructure( METHODS ); //Clause
            }
            DisplayEndArray("Total EHClauses", METHODS ); // Clauses
        }
    }

    TADDR fixupList = md->GetFixupList();
    if (fixupList != NULL)
    {
        DisplayStartArray( "Fixups", NULL, METHODS );
        DumpMethodFixups(module, fixupList);
        DisplayEndArray(NULL, METHODS); //Fixups
    }

    DisplayStartStructure( "Code", DataPtrToDisplay(hotCodePtr), hotCodeSize,
                           METHODS );

    IF_OPT(DISASSEMBLE_CODE)
    {
        // Disassemble hot code.  Read the code into the host process.
        /* REVISIT_TODO Mon 10/24/2005
         * Is this align up right?
         */
        BYTE * codeStartHost =
            reinterpret_cast<BYTE*>(PTR_READ(hotCodePtr,
                                             (ULONG32)ALIGN_UP(hotCodeSize,
                                                      CODE_SIZE_ALIGN)));
        DisassembleMethod( codeStartHost, hotCodeSize );
    }
    else
    {
        CoverageRead(hotCodePtr,
                     (ULONG32)ALIGN_UP(hotCodeSize, CODE_SIZE_ALIGN));
    }

    DisplayEndStructure(METHODS); //HotCode

    if( coldCodePtr != NULL )
    {
        DisplayStartStructure( "ColdCode", DataPtrToDisplay(coldCodePtr),
                               coldCodeSize, METHODS );
        IF_OPT(DISASSEMBLE_CODE)
        {
            // Disassemble cold code.  Read the code into the host process.
            BYTE * codeStartHost =
                reinterpret_cast<BYTE*>(PTR_READ(coldCodePtr,
                                                 (ULONG32)ALIGN_UP(coldCodeSize,
                                                          CODE_SIZE_ALIGN)));
            DisassembleMethod( codeStartHost, coldCodeSize );
        }
        else
        {
            CoverageRead(coldCodePtr,
                         (ULONG32)ALIGN_UP(coldCodeSize, CODE_SIZE_ALIGN));

        }
        DisplayEndStructure( METHODS ); //ColdCode
    }
    DisplayEndElement( METHODS ); //Method
}
#undef IDC_SWITCH



void NativeImageDumper::DisassembleMethod(BYTE *code, SIZE_T size)
{
    _ASSERTE(CHECK_OPT(DISASSEMBLE_CODE));

    m_display->StartTextElement( "NativeCode" );

#ifdef FEATURE_MSDIS

    BYTE *codeStart = code;

    /* XXX Wed 8/22/2007
     * The way I compute code size includes the switch tables at the end of the hot and/or cold section.
     * When the disassembler gets there, it has a tendency to crash as it runs off the end of mapped
     * memory.  In order to properly compute this I need to look at the UnwindData (which is a
     * kernel32!RUNTIME_FUNCTION structure that gives the address range for the code.  However, I also need
     * to chase through the list of funclets to make sure I disassemble everything.  Instead of doing that,
     * I'll just trap the AV.
     */
    EX_TRY
    {
        while (code < (codeStart + size))
        {
            const size_t count = m_dis->CbDisassemble(0, code, size);

            if (count == 0)
            {
                m_display->WriteXmlText( "%04x\tUnknown instruction (%02x)\n", code-codeStart, *code);
                code++;
                continue;
            }

            /* XXX Fri 09/16/2005
             * PTR_HOST_TO_TADDR doesn't work on interior pointers.
             */
            m_currentAddress = m_decoder.GetDataRva(PTR_HOST_TO_TADDR(codeStart)
                                                    + (code - codeStart))
                + PTR_TO_TADDR(m_decoder.GetBase());

            const size_t cinstr = m_dis->Cinstruction();
            size_t inum = 0;
            while (true)
            {
                WCHAR szOpcode[4096];
                size_t len = m_dis->CchFormatInstr(szOpcode, _countof(szOpcode));
                _ASSERTE(szOpcode[len-1] == 0);
                m_display->WriteXmlText( "%04x\t%S\n", (code-codeStart) + (inum * 4), szOpcode );

NEXT_INSTR:
                if (++inum >= cinstr)
                    break;

                _ASSERTE((inum * 4) < count); // IA64 has 3 instructions per bundle commonly
                                              // referenced as offset 0, 4, and 8
                if (!m_dis->FSelectInstruction(inum))
                {
                    m_display->WriteXmlText( "%04x\tUnknown instruction within bundle\n", (code-codeStart) + (inum * 4));
                    goto NEXT_INSTR;
                }
            }

            code += count;
        }
    }
    EX_CATCH
    {

    }
    EX_END_CATCH(SwallowAllExceptions);

#else // FEATURE_MSDIS

    m_display->WriteXmlText( "Disassembly not supported\n" );

#endif // FEATURE_MSDIS

    m_display->EndTextElement(); //NativeCode
}

SIZE_T NativeImageDumper::TranslateAddressCallback(IXCLRDisassemblySupport *dis,
                                                   CLRDATA_ADDRESS addr,
                                                   __out_ecount(nameSize) WCHAR *name, SIZE_T nameSize,
                                                   DWORDLONG *offset)
{
    NativeImageDumper *pThis = (NativeImageDumper *) dis->PvClient();

    SIZE_T ret = pThis->TranslateSymbol(dis,
                                        addr+(SIZE_T)pThis->m_currentAddress,
                                        name, nameSize, offset);
#ifdef _DEBUG
    if( ret == 0 )
    {
        _snwprintf_s(name, nameSize, _TRUNCATE, W("@TRANSLATED ADDRESS@ %p"),
                     (TADDR)(addr + (SIZE_T)pThis->m_currentAddress) );
        ret = wcslen(name);
        *offset = -1;
    }
#endif
    return ret;
}
SIZE_T NativeImageDumper::TranslateFixupCallback(IXCLRDisassemblySupport *dis,
                                                 CLRDATA_ADDRESS addr,
                                                 SIZE_T size, __out_ecount(nameSize) WCHAR *name,
                                                 SIZE_T nameSize,
                                                 DWORDLONG *offset)
{
    NativeImageDumper *pThis = (NativeImageDumper *) dis->PvClient();
    if( !dis->TargetIsAddress() )
        return 0;

    TADDR taddr = TO_TADDR(pThis->m_currentAddress) + (TADDR)addr;
    SSIZE_T targetOffset;
    switch (size)
    {
    case sizeof(void*):
        targetOffset = *PTR_SIZE_T(taddr);
        break;
#ifdef HOST_64BIT
    case sizeof(INT32):
        targetOffset = *PTR_INT32(taddr);
        break;
#endif
    case sizeof(short):
        targetOffset = *(short*)(WORD*)PTR_WORD(taddr);
        break;
    case sizeof(signed char):
        targetOffset = *PTR_SBYTE(taddr);
        break;
    default:
        return 0;
    }

    CLRDATA_ADDRESS address = targetOffset + TO_TADDR(pThis->m_currentAddress) + addr + size;

    SIZE_T ret = pThis->TranslateSymbol(dis, address, name, nameSize, offset);
    if( ret == 0 )
    {
        _snwprintf_s(name, nameSize, _TRUNCATE, W("@TRANSLATED FIXUP@ %p"), (TADDR)address);
        ret = wcslen(name);
        *offset = -1;
    }
    return ret;
}

size_t NativeImageDumper::TranslateSymbol(IXCLRDisassemblySupport *dis,
                                          CLRDATA_ADDRESS addr, __out_ecount(nameSize) WCHAR *name,
                                          SIZE_T nameSize, DWORDLONG *offset)
{
#ifdef FEATURE_READYTORUN
    if (m_pReadyToRunHeader != NULL)
        return 0;
#endif

    if (isInRange((TADDR)addr))
    {
        COUNT_T rva = (COUNT_T)(addr - PTR_TO_TADDR(m_decoder.GetBase()));

        COUNT_T helperTableSize;
        void *helperTable = m_decoder.GetNativeHelperTable(&helperTableSize);

        if (rva >= m_decoder.GetDataRva(TO_TADDR(helperTable))
            && rva < (m_decoder.GetDataRva(TO_TADDR(helperTable))
                      +helperTableSize))
        {
            int helperIndex = (USHORT)*PTR_DWORD(TO_TADDR(addr));
//            _ASSERTE(helperIndex < CORINFO_HELP_COUNT);
            // because of literal blocks we might have bogus values
            if (helperIndex < CORINFO_HELP_COUNT)
                _snwprintf_s(name, nameSize, _TRUNCATE, W("<%S>"), g_helperNames[helperIndex]);
            else
                _snwprintf_s(name, nameSize, _TRUNCATE, W("Illegal HelperIndex<%04X>"), helperIndex);
            *offset = 0;
            return wcslen(name);
        }

        PTR_Module module = (TADDR)m_decoder.GetPersistedModuleImage();
        PTR_NGenLayoutInfo pNgenLayout = module->GetNGenLayoutInfo();

        for (int iRange = 0; iRange < 2; iRange++)
        {
            if (pNgenLayout->m_CodeSections[iRange].IsInRange((TADDR)addr))
            {
                int MethodIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(rva, pNgenLayout->m_pRuntimeFunctions[iRange], 0, pNgenLayout->m_nRuntimeFunctions[iRange] - 1);
                if (MethodIndex >= 0)
                {
#ifdef FEATURE_EH_FUNCLETS
                    while (pNgenLayout->m_MethodDescs[iRange][MethodIndex] == 0)
                        MethodIndex--;
#endif

                    PTR_RUNTIME_FUNCTION pRuntimeFunction = pNgenLayout->m_pRuntimeFunctions[iRange] + MethodIndex;

                    PTR_MethodDesc pMD = NativeUnwindInfoLookupTable::GetMethodDesc(pNgenLayout, pRuntimeFunction, PTR_TO_TADDR(m_decoder.GetBase()));
                    TempBuffer buf;
                    MethodDescToString( pMD, buf );
                    _snwprintf_s(name, nameSize, _TRUNCATE, W("%s "), (const WCHAR *)buf );
                    *offset = rva - RUNTIME_FUNCTION__BeginAddress(pRuntimeFunction);
                    return wcslen(name);
                }
            }
        }

        if (pNgenLayout->m_CodeSections[2].IsInRange((TADDR)addr))
        {
            int ColdMethodIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(rva, pNgenLayout->m_pRuntimeFunctions[2], 0, pNgenLayout->m_nRuntimeFunctions[2] - 1);
            if (ColdMethodIndex >= 0)
            {
                PTR_RUNTIME_FUNCTION pRuntimeFunction;

                PTR_CORCOMPILE_COLD_METHOD_ENTRY pColdCodeMap = pNgenLayout->m_ColdCodeMap;

#ifdef FEATURE_EH_FUNCLETS
                while (pColdCodeMap[ColdMethodIndex].mainFunctionEntryRVA == 0)
                    ColdMethodIndex--;

                pRuntimeFunction = dac_cast<PTR_RUNTIME_FUNCTION>(PTR_TO_TADDR(m_decoder.GetBase()) + pColdCodeMap[ColdMethodIndex].mainFunctionEntryRVA);
#else
                DWORD ColdUnwindData = pNgenLayout->m_pRuntimeFunctions[2][ColdMethodIndex].UnwindData;
                _ASSERTE((ColdUnwindData & RUNTIME_FUNCTION_INDIRECT) != 0);
                pRuntimeFunction = dac_cast<PTR_RUNTIME_FUNCTION>(PTR_TO_TADDR(m_decoder.GetBase()) + (ColdUnwindData & ~RUNTIME_FUNCTION_INDIRECT));
#endif

                PTR_MethodDesc pMD = NativeUnwindInfoLookupTable::GetMethodDesc(pNgenLayout, pRuntimeFunction, PTR_TO_TADDR(m_decoder.GetBase()));
                TempBuffer buf;
                MethodDescToString( pMD, buf );
                _snwprintf_s(name, nameSize, _TRUNCATE, W("%s (cold region)"), (const WCHAR *)buf );
                *offset = rva - RUNTIME_FUNCTION__BeginAddress(&pNgenLayout->m_pRuntimeFunctions[2][ColdMethodIndex]);
                return wcslen(name);
            }
        }

        //Dumping precodes by name requires some information from the module (the precode ranges).
        IF_OPT_OR(PRECODES, MODULE)
        {
            TempBuffer precodeBuf;
            //maybe it is a precode
            PTR_Precode maybePrecode((TADDR)addr);
            const char * precodeName = NULL;
            if (isPrecode((TADDR)addr))
            {
                switch(maybePrecode->GetType())
                {
                case PRECODE_INVALID:
                    precodeName = "InvalidPrecode"; break;
                case PRECODE_STUB:
                    precodeName = "StubPrecode"; break;
#ifdef HAS_NDIRECT_IMPORT_PRECODE
                case PRECODE_NDIRECT_IMPORT:
                    precodeName = "NDirectImportPrecode"; break;
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
                case PRECODE_FIXUP:
                    precodeName = "FixupPrecode"; break;
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
                case PRECODE_THISPTR_RETBUF:
                    precodeName = "ThisPtrRetBufPrecode"; break;
#endif // HAS_THISPTR_RETBUF_PRECODE
                }

                if( precodeName )
                {
                    //hot or cold?
                    precodeBuf.AppendPrintf( W("%S (0x%p)"), precodeName, addr );
                }
                //get MethodDesc from precode and dump the target
                PTR_MethodDesc precodeMD(maybePrecode->GetMethodDesc());
                precodeBuf.Append( W(" for ") );
                MethodDescToString(precodeMD, precodeBuf);

                _snwprintf_s(name, nameSize, _TRUNCATE, W("%s"), (const WCHAR *)precodeBuf);

                *offset = 0;
                return wcslen(name);
            }
        }

        PTR_CORCOMPILE_IMPORT_SECTION pImportSection = m_decoder.GetNativeImportSectionForRVA(rva);
        if (pImportSection != NULL)
        {
            const char * wbRangeName = NULL;
            switch (pImportSection->Type)
            {
            case CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD:
                wbRangeName = "ExternalMethod";
                break;

#if 0
            case CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD:
                wbRangeName = "VirtualMethod";
                break;

            case CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH:
                wbRangeName = "StubDispatch";
                break;
#endif

            // This method is only ever called for targets of direct calls right now and so the only
            // import that can meaninfully show up here is external method thunk.
            default:
                return 0;
            }

            TempBuffer fixupThunkBuf;
            fixupThunkBuf.AppendPrintf( W("%S (0x%p) for "), wbRangeName, addr );
            FixupThunkToString(pImportSection, (TADDR)addr, fixupThunkBuf);

            _snwprintf_s(name, nameSize, _TRUNCATE, W("%s"), (const WCHAR *)fixupThunkBuf);

            *offset = 0;
            return wcslen(name);
        }
    }
    else if( g_dacImpl->GetJitHelperFunctionName(addr,
                                                 _countof(bigByteBuffer),
                                                 (char*)bigByteBuffer,
                                                 NULL ) == S_OK )
    {
        *offset = 0;
        _snwprintf_s( name, nameSize, _TRUNCATE, W("%S"), bigByteBuffer );
        return wcslen(name);
    }
    else
    {
        //check mscorwks
        if( m_mscorwksBase <= addr &&
            addr < (m_mscorwksBase + m_mscorwksSize) )
        {
            *offset = addr - m_mscorwksBase;
            _snwprintf_s( name, nameSize, _TRUNCATE, W("clr") );
            return wcslen(name);
        }
        for( COUNT_T i = 0; i < m_numDependencies; ++i )
        {
            const Dependency& dep = m_dependencies[i];
            if( dep.pLoadedAddress <= addr &&
                addr < (dep.pLoadedAddress + dep.size) )
            {
                *offset = addr - dep.pLoadedAddress;
                _snwprintf_s( name, nameSize, _TRUNCATE, W("%s.ni"), dep.name );
                return wcslen(name);
            }
        }
    }

    return 0;
}

BOOL NativeImageDumper::HandleFixupForMethodDump(PTR_CORCOMPILE_IMPORT_SECTION pSection, SIZE_T fixupIndex, SIZE_T *fixupCell)
{
    PTR_SIZE_T fixupPtr(TO_TADDR(fixupCell));
    m_display->StartElement( "Fixup" );
    m_display->WriteElementPointer( "Address",
                                    DataPtrToDisplay( TO_TADDR(fixupCell) ) );
    m_display->WriteElementUInt( "TaggedValue", (DWORD)*fixupPtr );
    WriteElementsFixupBlob(pSection, *fixupPtr);
    m_display->EndElement();

    return TRUE;
}

void NativeImageDumper::DumpMethodFixups(PTR_Module module,
                                         TADDR fixupList)
{
    _ASSERTE( CHECK_OPT(METHODS) );

    COUNT_T nImportSections;
    PTR_CORCOMPILE_IMPORT_SECTION pImportSections = m_decoder.GetNativeImportSections(&nImportSections);

    //create the first element outside of the callback.  The callback creates
    //subsequent elements.
    module->FixupDelayListAux( fixupList, this,
                               &NativeImageDumper::HandleFixupForMethodDump,
                               pImportSections, nImportSections,
                               &m_decoder );
}

IMAGE_SECTION_HEADER * NativeImageDumper::FindSection( char const * name )
{
    COUNT_T numberOfSections = m_decoder.GetNumberOfSections();
    PTR_IMAGE_SECTION_HEADER curSection( m_decoder.FindFirstSection() );

    for ( ; numberOfSections > 0; --numberOfSections, ++curSection )
    {
        if ( ! strncmp( reinterpret_cast< char * >( curSection->Name ), name, 8 ) )
            break;
    }

    if ( ! numberOfSections )
        return NULL;

    return curSection;
}

NativeImageDumper::EnumMnemonics NativeImageDumper::s_ModulePersistedFlags[] =
{
#define MPF_ENTRY(f) NativeImageDumper::EnumMnemonics(Module::f, W(#f))
    MPF_ENTRY(COMPUTED_GLOBAL_CLASS),

    MPF_ENTRY(COMPUTED_STRING_INTERNING),
    MPF_ENTRY(NO_STRING_INTERNING),

    MPF_ENTRY(COMPUTED_WRAP_EXCEPTIONS),
    MPF_ENTRY(WRAP_EXCEPTIONS),

    MPF_ENTRY(COMPUTED_RELIABILITY_CONTRACT),

    MPF_ENTRY(COLLECTIBLE_MODULE),
    MPF_ENTRY(COMPUTED_IS_PRE_V4_ASSEMBLY),
    MPF_ENTRY(IS_PRE_V4_ASSEMBLY),
    MPF_ENTRY(DEFAULT_DLL_IMPORT_SEARCH_PATHS_IS_CACHED),
    MPF_ENTRY(DEFAULT_DLL_IMPORT_SEARCH_PATHS_STATUS),

    MPF_ENTRY(COMPUTED_METHODDEF_TO_PROPERTYINFO_MAP),
    MPF_ENTRY(LOW_LEVEL_SYSTEM_ASSEMBLY_BY_NAME),
#undef MPF_ENTRY
};

//VirtualSectionTypes.
#define TEXTIFY(x) W(#x)
static const NativeImageDumper::EnumMnemonics s_virtualSectionFlags [] =
{

#define CORCOMPILE_SECTION_IBCTYPE(ibcType, _value) NativeImageDumper::EnumMnemonics(_value, TEXTIFY(ibcType)),
    CORCOMPILE_SECTION_IBCTYPES()
#undef CORCOMPILE_SECTION_IBCTYPE

#define CORCOMPILE_SECTION_RANGE_TYPE(rangeType, _value) NativeImageDumper::EnumMnemonics(_value, TEXTIFY(rangeType) W("Range")),
    CORCOMPILE_SECTION_RANGE_TYPES()
#undef CORCOMPILE_SECTION_RANGE_TYPE
};
const WCHAR * g_sectionNames[] =
{
    W("SECTION_DUMMY"), // the first section start at 0x1. Make the array 1 based.
#define CORCOMPILE_SECTION_TYPE(section) W("SECTION_") TEXTIFY(section),
    CORCOMPILE_SECTION_TYPES()
#undef CORCOMPILE_SECTION

};
#undef TEXTIFY


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif

void NativeImageDumper::DumpModule( PTR_Module module )
{

    //the module is the fisrt thing in the .data section.  We use this fact for
    //the sectionBases down below.
//    _ASSERTE(m_decoder.GetDataRva(PTR_TO_TADDR(module))
//             == FindSection(".data")->VirtualAddress );

    DisplayStartStructure( "module", DPtrToPreferredAddr(module),
                           sizeof(*module), MODULE );
    PTR_PEFile file = module->m_file;
    _ASSERTE(file == NULL);
    DisplayWriteFieldPointer( m_file, DPtrToPreferredAddr(file), Module,
                              MODULE );

    PTR_MethodDesc dllMain( TO_TADDR(module->m_pDllMain) );
    WriteFieldMethodDesc( m_pDllMain, dllMain, Module,
                          MODULE );

    _ASSERTE(module->m_dwTransientFlags == 0U);
    DisplayWriteFieldUInt(m_dwTransientFlags, module->m_dwTransientFlags,
                          Module, MODULE );



    DisplayWriteFieldEnumerated( m_dwPersistedFlags, module->m_dwPersistedFlags,
                                 Module, s_ModulePersistedFlags, W("|"), MODULE );

    DisplayWriteFieldPointer( m_pAssembly,
                              DPtrToPreferredAddr(module->m_pAssembly),
                              Module, MODULE );
    _ASSERTE(module->m_pAssembly == NULL); //never appears in the image

    DisplayWriteFieldUInt( m_moduleRef, module->m_moduleRef, Module, MODULE );
    DisplayWriteFieldInt( m_dwDebuggerJMCProbeCount,
                          module->m_dwDebuggerJMCProbeCount, Module, MODULE );
    /* REVISIT_TODO Fri 10/14/2005
     * Dump the binder
     */
    PTR_CoreLibBinder binder = module->m_pBinder;
    if( NULL != binder )
    {
        DisplayStartStructureWithOffset( m_pBinder, DPtrToPreferredAddr(binder),
                                         sizeof(*binder), Module,
                                         MODULE );

        //these four fields don't have anything useful in ngen images.
        DisplayWriteFieldPointer( m_classDescriptions,
                                  DPtrToPreferredAddr(binder->m_classDescriptions),
                                  CoreLibBinder, MODULE );
        DisplayWriteFieldPointer( m_methodDescriptions,
                                  DPtrToPreferredAddr(binder->m_methodDescriptions),
                                  CoreLibBinder, MODULE );
        DisplayWriteFieldPointer( m_fieldDescriptions,
                                  DPtrToPreferredAddr(binder->m_fieldDescriptions),
                                  CoreLibBinder, MODULE );
        DisplayWriteFieldPointer( m_pModule,
                                  DPtrToPreferredAddr(binder->m_pModule),
                                  CoreLibBinder, MODULE );

        DisplayWriteFieldInt( m_cClasses, binder->m_cClasses, CoreLibBinder,
                              MODULE );
        DisplayWriteFieldAddress( m_pClasses,
                                  DPtrToPreferredAddr(binder->m_pClasses),
                                  sizeof(*binder->m_pClasses)
                                  * binder->m_cClasses,
                                  CoreLibBinder, MODULE );
        DisplayWriteFieldInt( m_cFields, binder->m_cFields, CoreLibBinder,
                              MODULE );
        DisplayWriteFieldAddress( m_pFields,
                                  DPtrToPreferredAddr(binder->m_pFields),
                                  sizeof(*binder->m_pFields)
                                  * binder->m_cFields,
                                  CoreLibBinder, MODULE );
        DisplayWriteFieldInt( m_cMethods, binder->m_cMethods, CoreLibBinder,
                              MODULE );
        DisplayWriteFieldAddress( m_pMethods,
                                  DPtrToPreferredAddr(binder->m_pMethods),
                                  sizeof(*binder->m_pMethods)
                                  * binder->m_cMethods,
                                  CoreLibBinder, MODULE );

        DisplayEndStructure( MODULE ); //m_pBinder
    }
    else
    {
        DisplayWriteFieldPointer( m_pBinder, NULL, Module, MODULE );
    }


    /* REVISIT_TODO Tue 10/25/2005
     * unconditional dependencies, activations, class dependencies, thunktable
     */


    //round trip the LookupMap back through the DAC so that we don't have an
    //interior host pointer.
    PTR_LookupMapBase lookupMap( PTR_TO_TADDR(module)
                             + offsetof(Module, m_TypeDefToMethodTableMap) );
    TraverseMap( lookupMap, "m_TypeDefToMethodTableMap",
                 offsetof(Module, m_TypeDefToMethodTableMap),
                 fieldsize(Module, m_TypeDefToMethodTableMap),
                 &NativeImageDumper::IterateTypeDefToMTCallback );

    lookupMap = PTR_LookupMapBase( PTR_TO_TADDR(module)
                               + offsetof(Module, m_TypeRefToMethodTableMap) );

    TraverseMap( lookupMap, "m_TypeRefToMethodTableMap",
                 offsetof(Module, m_TypeRefToMethodTableMap),
                 fieldsize(Module, m_TypeRefToMethodTableMap),
                 &NativeImageDumper::IterateTypeRefToMTCallback );

    lookupMap = PTR_LookupMapBase( PTR_TO_TADDR(module)
                               + offsetof(Module, m_MethodDefToDescMap) );
    TraverseMap( lookupMap, "m_MethodDefToDescMap",
                 offsetof(Module, m_MethodDefToDescMap),
                 fieldsize(Module, m_MethodDefToDescMap),
                 &NativeImageDumper::IterateMethodDefToMDCallback);

    lookupMap = PTR_LookupMapBase( PTR_TO_TADDR(module)
                               + offsetof(Module, m_FieldDefToDescMap) );
    TraverseMap( lookupMap, "m_FieldDefToDescMap",
                 offsetof(Module, m_FieldDefToDescMap),
                 fieldsize(Module, m_FieldDefToDescMap),
                 &NativeImageDumper::IterateFieldDefToFDCallback);

    TraverseMemberRefToDescHash(module->m_pMemberRefToDescHashTable, "m_pMemberRefToDescHashTable",
                                offsetof(Module, m_pMemberRefToDescHashTable),
                                fieldsize(Module, m_pMemberRefToDescHashTable),
                                FALSE);

    lookupMap = PTR_LookupMapBase( PTR_TO_TADDR(module)
                               + offsetof(Module, m_GenericParamToDescMap) );

    TraverseMap( lookupMap, "m_GenericParamToDescMap",
                 offsetof(Module, m_GenericParamToDescMap),
                 fieldsize(Module, m_GenericParamToDescMap),
                 &NativeImageDumper::IterateGenericParamToDescCallback);

    lookupMap = PTR_LookupMapBase( PTR_TO_TADDR(module)
                               + offsetof(Module, m_GenericTypeDefToCanonMethodTableMap) );

    TraverseMap( lookupMap, "m_GenericTypeDefToCanonMethodTableMap",
                 offsetof(Module, m_GenericTypeDefToCanonMethodTableMap),
                 fieldsize(Module, m_GenericTypeDefToCanonMethodTableMap),
                 &NativeImageDumper::IterateTypeDefToMTCallback );

    lookupMap = PTR_LookupMapBase( PTR_TO_TADDR(module)
                               + offsetof(Module, m_FileReferencesMap) );
    TraverseMap( lookupMap, "m_FileReferencesMap",
                 offsetof(Module, m_FileReferencesMap),
                 fieldsize(Module, m_FileReferencesMap),
                 &NativeImageDumper::IterateMemberRefToDescCallback);

    lookupMap = PTR_LookupMapBase(PTR_TO_TADDR(module)
                              + offsetof(Module,m_ManifestModuleReferencesMap));

    TraverseMap( lookupMap, "m_ManifestModuleReferencesMap",
                 offsetof(Module, m_ManifestModuleReferencesMap),
                 fieldsize(Module, m_ManifestModuleReferencesMap),
                 &NativeImageDumper::IterateManifestModules);

    TraverseClassHash( module->m_pAvailableClasses, "m_pAvailableClasses",
                       offsetof(Module, m_pAvailableClasses),
                       fieldsize(Module, m_pAvailableClasses), true );

    TraverseTypeHash( module->m_pAvailableParamTypes, "m_pAvailableParamTypes",
                      offsetof(Module, m_pAvailableParamTypes),
                      fieldsize(Module, m_pAvailableParamTypes) );
    TraverseInstMethodHash( module->m_pInstMethodHashTable,
                            "m_pInstMethodHashTable",
                            offsetof(Module, m_pInstMethodHashTable),
                            fieldsize(Module, m_pInstMethodHashTable),
                            module );
    TraverseStubMethodHash( module->m_pStubMethodHashTable,
                            "m_pStubMethodHashTable",
                            offsetof(Module, m_pStubMethodHashTable),
                            fieldsize(Module, m_pStubMethodHashTable),
                            module );

    IF_OPT(MODULE)
    {
        TraverseClassHash( module->m_pAvailableClassesCaseIns,
                           "m_pAvailableClassesCaseIns",
                           offsetof(Module, m_pAvailableClassesCaseIns),
                           fieldsize(Module, m_pAvailableClassesCaseIns),
                           false );
    }

    _ASSERTE(module->m_pProfilingBlobTable == NULL);

    DisplayWriteFieldFlag( m_nativeImageProfiling,
                           module->m_nativeImageProfiling, Module, MODULE );

    DisplayWriteFieldPointer( m_methodProfileList,
                              DataPtrToDisplay((TADDR)module->m_methodProfileList),
                              Module, MODULE );
    _ASSERTE(module->m_methodProfileList == NULL);

    /* REVISIT_TODO Tue 10/04/2005
     * Dump module->m_moduleCtorInfo
     */
    PTR_ModuleCtorInfo ctorInfo( PTR_HOST_MEMBER_TADDR(Module, module,
                                                       m_ModuleCtorInfo) );

    DisplayStartStructureWithOffset( m_ModuleCtorInfo,
                                     DPtrToPreferredAddr(ctorInfo),
                                     sizeof(*ctorInfo),
                                     Module, SLIM_MODULE_TBLS );
    DisplayWriteFieldInt( numElements, ctorInfo->numElements, ModuleCtorInfo,
                          SLIM_MODULE_TBLS );
    DisplayWriteFieldInt( numLastAllocated, ctorInfo->numLastAllocated,
                          ModuleCtorInfo, SLIM_MODULE_TBLS );
    DisplayWriteFieldInt( numElementsHot, ctorInfo->numElementsHot,
                          ModuleCtorInfo, SLIM_MODULE_TBLS );
    DisplayWriteFieldAddress( ppMT, DPtrToPreferredAddr(ctorInfo->ppMT),
                              ctorInfo->numElements * sizeof(RelativePointer<MethodTable*>),
                              ModuleCtorInfo, SLIM_MODULE_TBLS );
    /* REVISIT_TODO Tue 03/21/2006
     * is cctorInfoHot and cctorInfoCold actually have anything interesting
     * inside of them?
     */
    DisplayWriteFieldAddress( cctorInfoHot,
                              DPtrToPreferredAddr(ctorInfo->cctorInfoHot),
                              sizeof(*ctorInfo->cctorInfoHot)
                                * ctorInfo->numElementsHot,
                              ModuleCtorInfo, SLIM_MODULE_TBLS );
    DisplayWriteFieldAddress( cctorInfoCold,
                              DPtrToPreferredAddr(ctorInfo->cctorInfoCold),
                              sizeof(*ctorInfo->cctorInfoCold)
                                * (ctorInfo->numElements
                                   - ctorInfo->numElementsHot),
                              ModuleCtorInfo, SLIM_MODULE_TBLS );
    /* XXX Thu 03/23/2006
     * See ModuleCtorInfo::Save for why these are +1.
     */
    DisplayWriteFieldAddress( hotHashOffsets,
                              DPtrToPreferredAddr(ctorInfo->hotHashOffsets),
                              (ctorInfo->numHotHashes + 1)
                              * sizeof(*ctorInfo->hotHashOffsets),
                              ModuleCtorInfo, SLIM_MODULE_TBLS );
    DisplayWriteFieldAddress( coldHashOffsets,
                              DPtrToPreferredAddr(ctorInfo->coldHashOffsets),
                              (ctorInfo->numColdHashes + 1)
                              * sizeof(*ctorInfo->coldHashOffsets),
                              ModuleCtorInfo, SLIM_MODULE_TBLS );

    DisplayWriteFieldInt( numHotHashes, ctorInfo->numHotHashes, ModuleCtorInfo,
                          SLIM_MODULE_TBLS );
    DisplayWriteFieldInt( numColdHashes, ctorInfo->numColdHashes,
                          ModuleCtorInfo, SLIM_MODULE_TBLS );

    DisplayWriteFieldAddress( ppHotGCStaticsMTs,
                              DPtrToPreferredAddr(ctorInfo->ppHotGCStaticsMTs),
                              ctorInfo->numHotGCStaticsMTs
                              * sizeof(*ctorInfo->ppHotGCStaticsMTs),
                              ModuleCtorInfo, SLIM_MODULE_TBLS );
    DisplayWriteFieldAddress( ppColdGCStaticsMTs,
                              DPtrToPreferredAddr(ctorInfo->ppColdGCStaticsMTs),
                              ctorInfo->numColdGCStaticsMTs
                              * sizeof(*ctorInfo->ppColdGCStaticsMTs),
                              ModuleCtorInfo, SLIM_MODULE_TBLS );
    DisplayWriteFieldInt( numHotGCStaticsMTs, ctorInfo->numHotGCStaticsMTs,
                          ModuleCtorInfo, SLIM_MODULE_TBLS );
    DisplayWriteFieldInt( numColdGCStaticsMTs, ctorInfo->numColdGCStaticsMTs,
                          ModuleCtorInfo, SLIM_MODULE_TBLS );

    DisplayEndStructure( SLIM_MODULE_TBLS ); //m_ModuleCtorInfo

    _ASSERTE(module->m_pNgenStats == NULL);

    DisplayWriteFieldPointer( m_pNgenStats,
                              DataPtrToDisplay((TADDR)module->m_pNgenStats),
                              Module, MODULE );

    DisplayWriteFieldAddress(m_propertyNameSet,
                             DPtrToPreferredAddr(module->m_propertyNameSet),
                             sizeof(module->m_propertyNameSet[0]) *
                             module->m_nPropertyNameSet,
                             Module, MODULE);

    DisplayWriteFieldPointer( m_ModuleID,
                              DataPtrToDisplay(dac_cast<TADDR>(module->m_ModuleID)),
                              Module, MODULE );
    _ASSERTE(module->m_ModuleID == NULL);

    /* XXX Tue 04/11/2006
     * Value is either -1 or 0, so no need to rebase.
     */
    DisplayWriteFieldPointer( m_pRegularStaticOffsets,
                              PTR_TO_TADDR(module->m_pRegularStaticOffsets),
                              Module, MODULE );
    _ASSERTE(module->m_pRegularStaticOffsets == (void*)-1
             || module->m_pRegularStaticOffsets == 0 );

    DisplayWriteFieldInt( m_dwMaxGCRegularStaticHandles,
                          module->m_dwMaxGCRegularStaticHandles, Module, MODULE );
    DisplayWriteFieldInt( m_dwRegularStaticsBlockSize, module->m_dwRegularStaticsBlockSize,
                          Module, MODULE );
    DisplayWriteFieldAddress( m_pDynamicStaticsInfo,
                              DataPtrToDisplay((TADDR)module->m_pDynamicStaticsInfo),
                              module->m_maxDynamicEntries
                                * sizeof(*(module->m_pDynamicStaticsInfo)),
                              Module, MODULE );

    DisplayWriteFieldInt( m_cDynamicEntries,
                          (int)module->m_cDynamicEntries, Module, MODULE );

    CoverageRead(TO_TADDR(module->m_pDynamicStaticsInfo),
                 (int)(module->m_maxDynamicEntries
                 * sizeof(*(module->m_pDynamicStaticsInfo))));



    _ASSERTE(module->m_debuggerSpecificData.m_pDynamicILCrst == NULL);
    DisplayWriteFieldPointer( m_debuggerSpecificData.m_pDynamicILCrst,
                              DataPtrToDisplay(dac_cast<TADDR>(module->m_debuggerSpecificData.m_pDynamicILCrst)),
                              Module, MODULE );


    /* REVISIT_TODO Wed 09/21/2005
     * Get me in the debugger and look at the activations and module/class
     * dependencies.
     * As well as the thunks.
     */

    /* REVISIT_TODO Wed 09/21/2005
     * Dump the following
     */
    //file
    //assembly

    DisplayWriteFieldInt( m_DefaultDllImportSearchPathsAttributeValue,
                          module->m_DefaultDllImportSearchPathsAttributeValue, Module, MODULE );


    DisplayEndStructure(MODULE); //Module
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

bool NativeImageDumper::isPrecode(TADDR maybePrecode)
{
    PTR_Module module = (TADDR)m_decoder.GetPersistedModuleImage();

    return !!module->IsZappedPrecode(maybePrecode);
}


void NativeImageDumper::IterateTypeDefToMTCallback( TADDR mtTarget,
                                                    TADDR flags,
                                                    PTR_LookupMapBase map,
                                                    DWORD rid )
{
    DisplayStartElement( "Entry", MODULE_TABLES );

    PTR_MethodTable mt(mtTarget);

    DisplayWriteElementUInt( "Token", rid | mdtTypeDef, MODULE_TABLES );
    /* REVISIT_TODO Fri 10/21/2005
     * Can I use WriteElementMethodTable here?
     */
    DisplayWriteElementPointer( "MethodTable", DPtrToPreferredAddr(mt),
                                MODULE_TABLES );
    DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
    /* REVISIT_TODO Fri 09/30/2005
     * This handles the extra entries in the type table that shouldn't be there.
     */
    if( rid == 0 || ((rid != 1) && (mtTarget == NULL)) )
    {
        DisplayWriteElementString( "Name", "mdTypeDefNil", MODULE_TABLES );
    }
    else
    {
        TempBuffer buf;
        MethodTableToString( mt, buf );
        DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );
    }
    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement( MODULE_TABLES );

    if( isInRange(PTR_TO_TADDR(mt)) )
    {
        m_discoveredMTs.AppendEx(mt);
        PTR_EEClass clazz = GetClassFromMT(mt);
        if( isInRange(PTR_TO_TADDR(clazz)) )
            m_discoveredClasses.AppendEx(mt);
    }
}

void NativeImageDumper::IterateTypeRefToMTCallback( TADDR mtTarget,
                                                    TADDR flags,
                                                    PTR_LookupMapBase map,
                                                    DWORD rid )
{
    DisplayStartElement( "Entry", MODULE_TABLES );

    mtTarget = ((FixupPointer<TADDR>&)mtTarget).GetValue();

    PTR_MethodTable mt(mtTarget);

#if 0
    RecordTypeRef(rid | mdtTypeRef, mt);
#endif

    DisplayWriteElementUInt( "Token", rid | mdtTypeRef, MODULE_TABLES );

    DisplayWriteElementPointer( "MethodTable", DPtrToPreferredAddr(mt),
                                MODULE_TABLES );

    if( rid == 0 )
    {
        DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
        DisplayWriteElementString( "Name", "mdtTypeRefNil", MODULE_TABLES );
    }
    else if( mt == NULL )
    {
        DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
        IF_OPT(MODULE_TABLES)
            WriteElementMDToken( "Name", mdtTypeRef | rid );
    }
    else if( CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(mt)) )
    {
        RVA rva = CORCOMPILE_UNTAG_TOKEN(PTR_TO_TADDR(mt));
        //
        // This writes two things FixupTargetValue and FixupTargetName
        //
        WriteElementsFixupBlob( NULL,PTR_TO_TADDR(mt));
    }
    else
    {
        TempBuffer buf;
        MethodTableToString( mt, buf );
        DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
        DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );
    }
    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement( MODULE_TABLES );
    if( isInRange(mtTarget) )
    {
        m_discoveredMTs.AppendEx(mt);
        PTR_EEClass clazz = GetClassFromMT(mt);
        if( isInRange(PTR_TO_TADDR(clazz)) )
            m_discoveredClasses.AppendEx(mt);
    }
}

void NativeImageDumper::IterateMethodDefToMDCallback( TADDR mdTarget,
                                                      TADDR flags,
                                                      PTR_LookupMapBase map,
                                                      DWORD rid )
{
    DisplayStartElement( "Entry", MODULE_TABLES );

    PTR_MethodDesc md(mdTarget);

    DisplayWriteElementUInt( "Token", rid | mdtMethodDef, MODULE_TABLES );

    DisplayWriteElementPointer( "MethodDesc", DPtrToPreferredAddr(md),
                                MODULE_TABLES );

    DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
    if( rid == 0 )
    {
        DisplayWriteElementString( "Name", "mdtMethodDefNil", MODULE_TABLES );
    }
    else
    {
        TempBuffer buf;
        MethodDescToString( md, buf );
        DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );
    }
    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement( MODULE_TABLES );
    //m_discoveredMDs.AppendEx(md);
}

void NativeImageDumper::IterateFieldDefToFDCallback( TADDR fdTarget,
                                                     TADDR flags,
                                                     PTR_LookupMapBase map,
                                                     DWORD rid )
{
    PTR_FieldDesc fd(fdTarget);
    DisplayStartElement( "Entry", MODULE_TABLES );


    DisplayWriteElementUInt( "Token", rid | mdtFieldDef, MODULE_TABLES );

    DisplayWriteElementPointer( "FieldDef", DPtrToPreferredAddr(fd),
                                MODULE_TABLES );

    DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
    if( rid == 0 )
    {
        DisplayWriteElementString( "Name", "mdtFieldDefNil", MODULE_TABLES );
    }
    else
    {
        TempBuffer buf;
        FieldDescToString( fd, mdtFieldDef | rid, buf );
        DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );
    }
    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement( MODULE_TABLES );
    /* XXX Mon 10/17/2005
     * All FieldDescs are reachable from the EEClasses
     */
    //m_discoveredFDs.AppendEx(PTR_FieldDesc(fdTarget));
}

void NativeImageDumper::IterateMemberRefToDescCallback( TADDR mrTarget,
                                                        TADDR flags,
                                                        PTR_LookupMapBase map,
                                                        DWORD rid )
{
    DisplayStartElement( "Entry", MODULE_TABLES );


    bool isFieldRef = (flags & IS_FIELD_MEMBER_REF) != 0;
    mdToken targetToken =  mdtMemberRef | rid;
    mrTarget = ((FixupPointer<TADDR>&)mrTarget).GetValue();
    DisplayWriteElementUInt( "Token", targetToken, MODULE_TABLES );
    DisplayWriteElementPointer( isFieldRef ? "FieldDesc" : "MethodDesc",
                                DataPtrToDisplay(mrTarget), MODULE_TABLES );

    TempBuffer buf;
    if( rid == 0 )
    {
        buf.Append( W("mdtMemberDefNil") );
    }
    else if( CORCOMPILE_IS_POINTER_TAGGED(mrTarget) )
    {
        WriteElementsFixupBlob( NULL, mrTarget );
    }
    else if( isFieldRef )
    {
        FieldDescToString( PTR_FieldDesc(mrTarget), buf );
    }
    else
    {
        MethodDescToString( PTR_MethodDesc(mrTarget), buf );
    }
    DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
    DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );

    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement(MODULE_TABLES);
    //m_discoveredMTs.AppendEx(mt);
}

void NativeImageDumper::IterateGenericParamToDescCallback( TADDR tdTarget,
                                                     TADDR flags,
                                                     PTR_LookupMapBase map,
                                                     DWORD rid )
{
    PTR_TypeDesc td(tdTarget);
    DisplayStartElement( "Entry", MODULE_TABLES );


    DisplayWriteElementUInt( "Token", rid | mdtGenericParam, MODULE_TABLES );

    DisplayWriteElementPointer( "GenericParam", DPtrToPreferredAddr(td),
                                MODULE_TABLES );

    DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
    if( rid == 0 || td == NULL )
    {
        DisplayWriteElementString( "Name", "mdtGenericParamNil", MODULE_TABLES );
    }
    else
    {
        TempBuffer buf;
        TypeDescToString( td, buf );
        DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );
    }
    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement( MODULE_TABLES );
}

#if 0
void NativeImageDumper::IterateFileReferencesCallback(TADDR moduleTarget,
                                                      TADDR flags,
                                                      PTR_LookupMapBase map,
                                                      DWORD rid)
{
    DisplayStartElement( "Entry", MODULE_TABLES );

    PTR_Module module(moduleTarget);

    DisplayWriteElementUInt( "Token", rid | mdtFile, MODULE_TABLES );

    DisplayWriteElementPointer( "Module", DPtrToPreferredAddr(module),
                                MODULE_TABLES );

    DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
    if( rid == 0 || (module == NULL) )
    {
        DisplayWriteElementString( "Name", "mdtFileNil", MODULE_TABLES );
    }
    else
    {
        TempBuffer buf;
        AppendTokenName(mdtFile | rid, buf);
        DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );
    }
    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement( MODULE_TABLES );
    //m_discoveredFDs.AppendEx(mt);
}
#endif

void NativeImageDumper::IterateManifestModules( TADDR moduleTarget,
                                               TADDR flags,
                                               PTR_LookupMapBase map,
                                               DWORD rid )
{
    DisplayStartElement( "Entry", MODULE_TABLES );

    moduleTarget = ((FixupPointer<TADDR>&)moduleTarget).GetValue();

    PTR_Module module(moduleTarget);

    DisplayWriteElementUInt( "Token", rid | mdtAssemblyRef, MODULE_TABLES );

    DisplayWriteElementPointer( "Module", DPtrToPreferredAddr(module),
                                MODULE_TABLES );
    DisplayWriteElementFlag( "fake", false, MODULE_TABLES );
    if( rid == 0 || (module == NULL) )
    {
        DisplayWriteElementString( "Name", "mdtAssemblyRefNil", MODULE_TABLES );
    }
    else
    {
        TempBuffer buf;
        AppendTokenName(mdtAssemblyRef | rid, buf, m_import);
        DisplayWriteElementStringW( "Name", (const WCHAR*)buf, MODULE_TABLES );
    }
    DisplayWriteElementFlag( "hot", !!map->FindHotItemValuePtr(rid),
                             MODULE_TABLES );
    DisplayEndElement( MODULE_TABLES );
    //m_discoveredFDs.AppendEx(mt);
}

void NativeImageDumper::TraverseMap(PTR_LookupMapBase map, const char * name,
                                    unsigned offset, unsigned fieldSize,
                                    void(NativeImageDumper::*cb)(TADDR,
                                                                 TADDR,
                                                                 PTR_LookupMapBase,
                                                                 DWORD))
{
    if( map == NULL )
    {
        IF_OPT(MODULE)
            m_display->WriteFieldPointer( name, offset, fieldSize, NULL );
        return;
    }
    DisplayStartVStructure(name, MODULE);

    DisplayStartArray( "Tables", W("%s"), MODULE );
    PTR_LookupMapBase current = map;
    do
    {
        DWORD cbTable = map->MapIsCompressed() ? map->cbTable : map->dwCount * sizeof(*map->pTable);

        IF_OPT(MODULE)
        {
            DisplayWriteElementAddress( "Table",
                                        DPtrToPreferredAddr(map->pTable),
                                        cbTable,
                                        MODULE);
        }

        CoverageRead( PTR_TO_TADDR(map->pTable), cbTable );
        _ASSERTE(current == map || current->hotItemList == NULL);
        current = current->pNext;
    }while( current != NULL );

    DisplayEndArray( "Total Tables", MODULE ); //Tables

    DisplayWriteFieldAddress( hotItemList,
                              DPtrToPreferredAddr(map->hotItemList),
                              map->dwNumHotItems * sizeof(*map->hotItemList),
                              LookupMapBase, MODULE );

    DisplayStartArray( "Map", W("[%s]: %s %s%s  %s %s %s"), MODULE_TABLES );

    IF_OPT_OR3(MODULE_TABLES, EECLASSES, METHODTABLES)
    {
        LookupMap<TADDR>::Iterator iter(dac_cast<DPTR(LookupMap<TADDR>)>(map));
        DWORD rid = 0;
        while(iter.Next())
        {
            TADDR flags = 0;
            TADDR element = iter.GetElementAndFlags(&flags);
            (this->*cb)( element, flags, map, rid );
            rid++;
        }

    }
    CoverageRead( PTR_TO_TADDR(map->hotItemList),
                  map->dwNumHotItems * sizeof(*map->hotItemList) );
    DisplayEndArray( "Total" , MODULE_TABLES );//Map

    DisplayEndVStructure(MODULE); //name
}

// Templated method containing the core code necessary to traverse hash tables based on NgenHash (see
// vm\NgenHash.h).
template<typename HASH_CLASS, typename HASH_ENTRY_CLASS>
void NativeImageDumper::TraverseNgenHash(DPTR(HASH_CLASS) pTable,
                                         const char * name,
                                         unsigned offset,
                                         unsigned fieldSize,
                                         bool saveClasses,
                                         void (NativeImageDumper::*DisplayEntryFunction)(void *, DPTR(HASH_ENTRY_CLASS), bool),
                                         void *pContext)
{
    if (pTable == NULL)
    {
        IF_OPT(MODULE)
            m_display->WriteFieldPointer(name, offset, fieldSize, NULL);
        return;
    }
    IF_OPT(MODULE)
    {
        m_display->StartStructureWithOffset(name, offset, fieldSize,
                                            DPtrToPreferredAddr(pTable),
                                            sizeof(HASH_CLASS));
    }

    DisplayWriteFieldPointer(m_pModule,
                             DPtrToPreferredAddr(pTable->GetModule()),
                             HASH_CLASS, MODULE);

    // Dump warm (volatile) entries.
    DisplayWriteFieldUInt(m_cWarmEntries, pTable->m_cWarmEntries, HASH_CLASS, MODULE);
    DisplayWriteFieldUInt(m_cWarmBuckets, pTable->m_cWarmBuckets, HASH_CLASS, MODULE);
    DisplayWriteFieldAddress(m_pWarmBuckets,
                             DPtrToPreferredAddr(pTable->GetWarmBuckets()),
                             sizeof(HASH_ENTRY_CLASS*) * pTable->m_cWarmBuckets,
                             HASH_CLASS, MODULE);

    // Dump hot (persisted) entries.
    DPTR(typename HASH_CLASS::PersistedEntries) pHotEntries(PTR_HOST_MEMBER_TADDR(HASH_CLASS, pTable, m_sHotEntries));
    DisplayStartStructureWithOffset(m_sHotEntries, DPtrToPreferredAddr(pHotEntries),
                                    sizeof(typename HASH_CLASS::PersistedEntries),
                                    HASH_CLASS, MODULE);
    TraverseNgenPersistedEntries<HASH_CLASS, HASH_ENTRY_CLASS>(pTable, pHotEntries, saveClasses, DisplayEntryFunction, pContext);
    DisplayEndStructure(MODULE); // Hot entries

    // Dump cold (persisted) entries.
    DPTR(typename HASH_CLASS::PersistedEntries) pColdEntries(PTR_HOST_MEMBER_TADDR(HASH_CLASS, pTable, m_sColdEntries));
    DisplayStartStructureWithOffset(m_sColdEntries, DPtrToPreferredAddr(pColdEntries),
                                    sizeof(typename HASH_CLASS::PersistedEntries),
                                    HASH_CLASS, MODULE);
    TraverseNgenPersistedEntries<HASH_CLASS, HASH_ENTRY_CLASS>(pTable, pColdEntries, saveClasses, DisplayEntryFunction, pContext);
    DisplayEndStructure(MODULE); // Cold entries

    DisplayEndStructure(MODULE); // pTable
}

// Helper used by TraverseNgenHash above to traverse an ngen persisted section of a table (separated out here
// because NgenHash-based tables can have two such sections, one for hot and one for cold entries).
template<typename HASH_CLASS, typename HASH_ENTRY_CLASS>
void NativeImageDumper::TraverseNgenPersistedEntries(DPTR(HASH_CLASS) pTable,
                                                     DPTR(typename HASH_CLASS::PersistedEntries) pEntries,
                                                     bool saveClasses,
                                                     void (NativeImageDumper::*DisplayEntryFunction)(void *, DPTR(HASH_ENTRY_CLASS), bool),
                                                     void *pContext)
{
    // Display top-level fields.
    DisplayWriteFieldUInt(m_cEntries, pEntries->m_cEntries, typename HASH_CLASS::PersistedEntries, MODULE);
    DisplayWriteFieldUInt(m_cBuckets, pEntries->m_cBuckets, typename HASH_CLASS::PersistedEntries, MODULE);
    DisplayWriteFieldAddress(m_pBuckets,
                             DPtrToPreferredAddr(pTable->GetPersistedBuckets(pEntries)),
                             pEntries->m_cBuckets ? pTable->GetPersistedBuckets(pEntries)->GetSize(pEntries->m_cBuckets) : 0,
                             typename HASH_CLASS::PersistedEntries, MODULE);
    DisplayWriteFieldAddress(m_pEntries,
                             DPtrToPreferredAddr(pTable->GetPersistedEntries(pEntries)),
                             sizeof(typename HASH_CLASS::PersistedEntry) * pEntries->m_cEntries,
                             typename HASH_CLASS::PersistedEntries, MODULE);

    // Display entries (or maybe just the classes referenced by those entries).
    DisplayStartArray("Entries", NULL, SLIM_MODULE_TBLS);

    // Enumerate bucket list.
    for (DWORD i = 0; i < pEntries->m_cBuckets; ++i)
    {
        // Get index of the first entry and the count of entries in the bucket.
        DWORD dwEntryId, cEntries;
        pTable->GetPersistedBuckets(pEntries)->GetBucket(i, &dwEntryId, &cEntries);

        // Loop over entries.
        while (cEntries && (CHECK_OPT(SLIM_MODULE_TBLS)
                            || CHECK_OPT(EECLASSES)
                            || CHECK_OPT(METHODTABLES)))
        {
            // Lookup entry in the array via the index we have.
            typename HASH_CLASS::PTR_PersistedEntry pEntry(PTR_TO_TADDR(pTable->GetPersistedEntries(pEntries)) +
                                                        (dwEntryId * sizeof(typename HASH_CLASS::PersistedEntry)));

            IF_OPT(SLIM_MODULE_TBLS)
            {
                DisplayStartStructure("PersistedEntry",
                                      DPtrToPreferredAddr(pEntry),
                                      sizeof(typename HASH_CLASS::PersistedEntry), SLIM_MODULE_TBLS);
            }

            // Display entry via a member function specific to the type of hash table we're traversing. Each
            // sub-class of NgenHash hash its own entry structure that is embedded NgenHash's entry. The
            // helper function expects a pointer to this inner entry.
            DPTR(HASH_ENTRY_CLASS) pInnerEntry(PTR_TO_MEMBER_TADDR(typename HASH_CLASS::PersistedEntry, pEntry, m_sValue));
            (this->*DisplayEntryFunction)(pContext, pInnerEntry, saveClasses);

            IF_OPT(SLIM_MODULE_TBLS)
            {
                DisplayWriteFieldUInt(m_iHashValue, pEntry->m_iHashValue,
                                      typename HASH_CLASS::PersistedEntry, SLIM_MODULE_TBLS);

                DisplayEndStructure(SLIM_MODULE_TBLS); // Entry
            }

            dwEntryId++;
            cEntries--;
        }
    }

    DisplayEndArray("Total Entries", SLIM_MODULE_TBLS); // Entry array
}

void NativeImageDumper::TraverseClassHashEntry(void *pContext, PTR_EEClassHashEntry pEntry, bool saveClasses)
{
    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayStartStructure("EEClassHashEntry",
                              DPtrToPreferredAddr(pEntry),
                              sizeof(EEClassHashEntry), SLIM_MODULE_TBLS);
    }

    size_t datum = size_t(PTR_TO_TADDR(pEntry->GetData()));

    if (datum & EECLASSHASH_TYPEHANDLE_DISCR)
    {
        IF_OPT(SLIM_MODULE_TBLS)
        {
            /* REVISIT_TODO Tue 10/25/2005
             * Raw data with annotation?
             */
            mdTypeDef tk;
            tk = EEClassHashTable::UncompressModuleAndClassDef(pEntry->GetData());
            DoWriteFieldMDToken("Token",
                                offsetof(EEClassHashEntry, m_Data),
                                fieldsize(EEClassHashEntry, m_Data),
                                tk);
        }
    }
    else
    {
        PTR_MethodTable pMT(TO_TADDR(datum));
        IF_OPT(SLIM_MODULE_TBLS)
        {
            DoWriteFieldMethodTable("MethodTable",
                                    offsetof(EEClassHashEntry, m_Data),
                                    fieldsize(EEClassHashEntry, m_Data),
                                    pMT);
        }

        if (saveClasses)
        {
            // These are MethodTables.  Get back to the EEClass from there.
            if (isInRange(PTR_TO_TADDR(pMT)))
                m_discoveredMTs.AppendEx(pMT);
            if (pMT != NULL)
            {
                PTR_EEClass pClass = GetClassFromMT(pMT);
                if (isInRange(PTR_TO_TADDR(pClass)))
                    m_discoveredClasses.AppendEx(pMT);
            }
        }
    }

    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayWriteFieldPointer(m_pEncloser,
                                 DPtrToPreferredAddr(pEntry->GetEncloser()),
                                 EEClassHashEntry, SLIM_MODULE_TBLS);
        DisplayEndStructure(SLIM_MODULE_TBLS);
    }
}

void NativeImageDumper::TraverseClassHash(PTR_EEClassHashTable pTable,
                                          const char * name,
                                          unsigned offset,
                                          unsigned fieldSize,
                                          bool saveClasses)
{
    TraverseNgenHash<EEClassHashTable, EEClassHashEntry>(pTable,
                                                         name,
                                                         offset,
                                                         fieldSize,
                                                         saveClasses,
                                                         &NativeImageDumper::TraverseClassHashEntry,
                                                         NULL);
}

#ifdef FEATURE_COMINTEROP

void NativeImageDumper::TraverseGuidToMethodTableEntry(void *pContext, PTR_GuidToMethodTableEntry pEntry, bool saveClasses)
{
    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayStartStructure("GuidToMethodTableEntry",
                              DPtrToPreferredAddr(pEntry),
                              sizeof(GuidToMethodTableEntry), SLIM_MODULE_TBLS);
    }

    WriteFieldMethodTable(m_pMT, pEntry->m_pMT, GuidToMethodTableEntry, ALWAYS);

    TempBuffer buf;
    GuidToString( *(pEntry->m_Guid), buf );
    DisplayWriteFieldStringW( m_Guid, (const WCHAR *)buf, GuidToMethodTableEntry, ALWAYS );

    DisplayEndStructure( SLIM_MODULE_TBLS );
}

void NativeImageDumper::TraverseGuidToMethodTableHash(PTR_GuidToMethodTableHashTable pTable,
                        const char * name,
                        unsigned offset,
                        unsigned fieldSize,
                        bool saveClasses)
{
    TraverseNgenHash<GuidToMethodTableHashTable, GuidToMethodTableEntry>(pTable,
                        name,
                        offset,
                        fieldSize,
                        saveClasses,
                        &NativeImageDumper::TraverseGuidToMethodTableEntry,
                        NULL);
}

#endif // FEATURE_COMINTEROP

void NativeImageDumper::TraverseMemberRefToDescHashEntry(void *pContext, PTR_MemberRefToDescHashEntry pEntry, bool saveClasses)
{
    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayStartStructure("MemberRefToDescHashEntry",
                              DPtrToPreferredAddr(pEntry),
                              sizeof(MemberRefToDescHashEntry), SLIM_MODULE_TBLS);
    }

    if(pEntry->m_value & IS_FIELD_MEMBER_REF)
        WriteFieldFieldDesc(m_value, dac_cast<PTR_FieldDesc>(pEntry->m_value & (~MEMBER_REF_MAP_ALL_FLAGS)), MemberRefToDescHashEntry, MODULE_TABLES);
    else
        WriteFieldMethodDesc(m_value, dac_cast<PTR_MethodDesc>(pEntry->m_value), MemberRefToDescHashEntry, MODULE_TABLES);

    DisplayEndStructure( SLIM_MODULE_TBLS );
}

void NativeImageDumper::TraverseMemberRefToDescHash(PTR_MemberRefToDescHashTable pTable,
                        const char * name,
                        unsigned offset,
                        unsigned fieldSize,
                        bool saveClasses)
{
    TraverseNgenHash<MemberRefToDescHashTable, MemberRefToDescHashEntry>(pTable,
                        name,
                        offset,
                        fieldSize,
                        saveClasses,
                        &NativeImageDumper::TraverseMemberRefToDescHashEntry,
                        NULL);
}


void NativeImageDumper::TraverseTypeHashEntry(void *pContext, PTR_EETypeHashEntry pEntry, bool saveClasses)
{
    TypeHandle th = pEntry->GetTypeHandle();
    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayStartStructure("EETypeHashEntry",
                              DPtrToPreferredAddr(pEntry),
                              sizeof(EETypeHashEntry), SLIM_MODULE_TBLS);

        DoWriteFieldTypeHandle("TypeHandle",
                               offsetof(EETypeHashEntry, m_data),
                               fieldsize(EETypeHashEntry, m_data),
                               th);
    }

    if (!CORCOMPILE_IS_POINTER_TAGGED(th.AsTAddr()) && th.IsTypeDesc())
    {
        PTR_TypeDesc td(th.AsTypeDesc());
        if (isInRange(PTR_TO_TADDR(td)))
            m_discoveredTypeDescs.AppendEx(td);
        if (td->HasTypeParam())
        {
            PTR_ParamTypeDesc ptd(td);

            /* REVISIT_TODO Thu 12/15/2005
             * Check OwnsTemplateMethodTable.  However, this asserts in
             * this special completely unrestored and messed up state
             * (also, it chases through MT->GetClass()).  There isn't
             * all that much harm here (bloats m_discoveredMTs though,
             * but not by a huge amount.
             */
            PTR_MethodTable mt(ptd->GetTemplateMethodTableInternal());
            if (isInRange(PTR_TO_TADDR(mt)))
            {
                m_discoveredMTs.AppendEx(mt);
                if (mt->IsClassPointerValid())
                {
                    PTR_EEClass pClass = mt->GetClass();
                    if (isInRange(PTR_TO_TADDR(pClass)))
                        m_discoveredClasses.AppendEx(mt);
                }
            }
        }
    }
    else
    {
        PTR_MethodTable mt(th.AsTAddr());

        if (isInRange( PTR_TO_TADDR(mt)))
            m_discoveredMTs.AppendEx(mt);
        //don't use GetClassFromMT here.  mt->m_pEEClass might be a
        //fixup.  In that case, just skip it.
        if (mt->IsClassPointerValid())
        {
            PTR_EEClass pClass = mt->GetClass();
            if (isInRange(PTR_TO_TADDR(pClass)))
                m_discoveredClasses.AppendEx(mt);
        }
    }

    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayEndStructure(SLIM_MODULE_TBLS);
    }
}

void NativeImageDumper::TraverseTypeHash(PTR_EETypeHashTable pTable,
                                         const char * name,
                                         unsigned offset,
                                         unsigned fieldSize)
{
    TraverseNgenHash<EETypeHashTable, EETypeHashEntry>(pTable,
                                                       name,
                                                       offset,
                                                       fieldSize,
                                                       true,
                                                       &NativeImageDumper::TraverseTypeHashEntry,
                                                       NULL);
}

void NativeImageDumper::TraverseInstMethodHashEntry(void *pContext, PTR_InstMethodHashEntry pEntry, bool saveClasses)
{
    PTR_Module pModule((TADDR)pContext);

    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayStartStructure("InstMethodHashEntry",
                              DPtrToPreferredAddr(pEntry),
                              sizeof(InstMethodHashEntry), SLIM_MODULE_TBLS);
    }

    IF_OPT_OR(SLIM_MODULE_TBLS, METHODDESCS)
    {
        IF_OPT(METHODDESCS)
        {
            PTR_MethodDesc md = pEntry->GetMethod();
            _ASSERTE(md != NULL);

            //if we want methoddescs, write the data field as a
            //structure with the whole contents of the method desc.
            m_display->StartVStructureWithOffset("data", offsetof(InstMethodHashEntry, data),
                                                  sizeof(pEntry->data));
            DumpMethodDesc(md, pModule);
            DisplayEndVStructure(ALWAYS); //data
        }
        else
        {
            PTR_MethodDesc md = pEntry->GetMethod();
            WriteFieldMethodDesc(data, md,
                                 InstMethodHashEntry, ALWAYS);
        }
    }
    else
        CoverageRead(PTR_TO_TADDR(pEntry), sizeof(*pEntry));

    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayEndStructure(SLIM_MODULE_TBLS);
    }
}

void NativeImageDumper::TraverseStubMethodHashEntry(void *pContext, PTR_StubMethodHashEntry pEntry, bool saveClasses)
{
    PTR_Module pModule((TADDR)pContext);

    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayStartStructure("StubMethodHashEntry",
                              DPtrToPreferredAddr(pEntry),
                              sizeof(StubMethodHashEntry), SLIM_MODULE_TBLS);
    }

    IF_OPT_OR(SLIM_MODULE_TBLS, METHODDESCS)
    {
        PTR_MethodDesc md = pEntry->GetMethod();
        _ASSERTE(md != NULL);

        PTR_MethodDesc stub = pEntry->GetStubMethod();
        _ASSERTE(stub != NULL);

        IF_OPT(METHODDESCS)
        {
            //if we want methoddescs, write the data fields as a
            //structure with the whole contents of the method desc.
            m_display->StartVStructureWithOffset("pMD", offsetof(StubMethodHashEntry, pMD),
                                                  sizeof(pEntry->pMD));
            DumpMethodDesc(md, pModule);
            DisplayEndVStructure(ALWAYS); //pMD

            m_display->StartVStructureWithOffset("pStubMD", offsetof(StubMethodHashEntry, pStubMD),
                                                  sizeof(pEntry->pStubMD));
            DumpMethodDesc(stub, pModule);
            DisplayEndVStructure(ALWAYS); //pStubMD
        }
        else
        {
            WriteFieldMethodDesc(pMD, md,
                                 StubMethodHashEntry, ALWAYS);
            WriteFieldMethodDesc(pStubMD, stub,
                                 StubMethodHashEntry, ALWAYS);
        }
    }
    else
        CoverageRead(PTR_TO_TADDR(pEntry), sizeof(*pEntry));

    IF_OPT(SLIM_MODULE_TBLS)
    {
        DisplayEndStructure(SLIM_MODULE_TBLS);
    }
}

void NativeImageDumper::TraverseInstMethodHash(PTR_InstMethodHashTable pTable,
                                               const char * name,
                                               unsigned fieldOffset,
                                               unsigned fieldSize,
                                               PTR_Module module)
{
    TraverseNgenHash<InstMethodHashTable, InstMethodHashEntry>(pTable,
                                                               name,
                                                               fieldOffset,
                                                               fieldSize,
                                                               true,
                                                               &NativeImageDumper::TraverseInstMethodHashEntry,
                                                               (void*)dac_cast<TADDR>(module));
}

void NativeImageDumper::TraverseStubMethodHash(PTR_StubMethodHashTable pTable,
                                               const char * name,
                                               unsigned fieldOffset,
                                               unsigned fieldSize,
                                               PTR_Module module)
{
    TraverseNgenHash<StubMethodHashTable, StubMethodHashEntry>(pTable,
                                                               name,
                                                               fieldOffset,
                                                               fieldSize,
                                                               true,
                                                               &NativeImageDumper::TraverseStubMethodHashEntry,
                                                               (void*)dac_cast<TADDR>(module));
}

const NativeImageDumper::Dependency *
NativeImageDumper::GetDependencyForModule( PTR_Module module )
{
    for( COUNT_T i = 0; i < m_numDependencies; ++i )
    {
        if( m_dependencies[i].pModule == module )
            return &m_dependencies[i];
    }
    return NULL;
}

#if 0
const NativeImageDumper::Import *
NativeImageDumper::GetImportForPointer( TADDR ptr )
{
    for( int i = 0; i < m_numImports; ++i )
    {
        const Import * import = &m_imports[i];
        if( import->dependency->pPreferredBase == NULL )
            continue;
        if( import->dependency->pPreferredBase <= ptr
            && ((import->dependency->pPreferredBase
                 + import->dependency->size) > ptr) )
        {
            //found the right target
            return import;
        }
    }
    return NULL;
}
#endif
const NativeImageDumper::Dependency *
NativeImageDumper::GetDependencyForPointer( TADDR ptr )
{
    for( COUNT_T i = 0; i < m_numDependencies; ++i )
    {
        const Dependency * dependency = &m_dependencies[i];
        if( dependency->pLoadedAddress == NULL )
            continue;
        if( dependency->pLoadedAddress <= ptr
            && ((dependency->pLoadedAddress + dependency->size) > ptr) )
        {
            //found the right target
            return dependency;
        }
    }
    return NULL;
}

void NativeImageDumper::DictionaryToArgString( PTR_Dictionary dictionary, unsigned numArgs, SString& buf )
{
    //this can be called with numArgs == 0 for value type instantiations.
    buf.Append( W("<") );

    for( unsigned i = 0; i < numArgs; ++i )
    {
        if( i > 0 )
            buf.Append( W(",") );

        TypeHandle th = dictionary->GetInstantiation()[i].GetValue();
        if( CORCOMPILE_IS_POINTER_TAGGED(th.AsTAddr()) )
        {
            if (!isSelf(GetDependencyForPointer(PTR_TO_TADDR(dictionary))))
            {
                //this is an RVA from another hardbound dependency.  We cannot decode it
                buf.Append(W("OUT_OF_MODULE_FIXUP"));
            }
            else
            {
                RVA rva = CORCOMPILE_UNTAG_TOKEN(th.AsTAddr());
                FixupBlobToString(rva, buf);
            }
        }
        else
        {
            TypeHandleToString( th, buf );
        }
    }
    buf.Append( W(">") );
}

void NativeImageDumper::MethodTableToString( PTR_MethodTable mt, SString& buf )
{
    bool hasCompleteExtents = true;
    IF_OPT(DISABLE_NAMES)
    {
        buf.Append( W("Disabled") );
        return;
    }
    mdToken token = mdTokenNil;
    if( mt == NULL )
        buf.Append( W("mdTypeDefNil") );
    else
    {
        _ASSERTE(!CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(mt)));
        const Dependency * dependency;
        if( !mt->IsClassPointerValid() )
        {
            if( isSelf(GetDependencyForPointer(PTR_TO_TADDR(mt))) )
            {

                hasCompleteExtents = false;
                RVA rva = CORCOMPILE_UNTAG_TOKEN(mt->GetCanonicalMethodTableFixup());
                PTR_CCOR_SIGNATURE sig = (TADDR) m_decoder.GetRvaData(rva);

                BYTE kind = *sig++;

                if (kind & ENCODE_MODULE_OVERRIDE)
                {
                    /* int moduleIndex = */ DacSigUncompressData(sig);
                    kind &= ~ENCODE_MODULE_OVERRIDE;
                }

                _ASSERTE(kind == ENCODE_TYPE_HANDLE);
                CorElementType et = DacSigUncompressElementType(sig);
                if( et == ELEMENT_TYPE_GENERICINST )
                {
                    //generic instances have another element type
                    et = DacSigUncompressElementType(sig);
                }
                if (et == ELEMENT_TYPE_VALUETYPE || et == ELEMENT_TYPE_CLASS)
                {
                    token = DacSigUncompressToken(sig);
                }
                else
                {
                    // Arrays, etc.
                    token = mdtTypeDef;
                }
                dependency = GetDependencyForFixup(rva);
            }
            else
            {
                //this is an RVA from another hardbound dependency.  We cannot decode it
                buf.Append(W("OUT_OF_MODULE_FIXUP"));
                return;
            }
        }
        else
        {
            token = mt->GetCl();
            dependency = GetDependencyFromMT(mt);
        }

        if( !isSelf(dependency) )
        {
            AppendTokenName( dependency->entry->dwAssemblyRef, buf,
                             m_manifestImport );
            buf.Append(W("!"));
        }

        _ASSERTE(dependency->pImport);
        if( token == mdtTypeDef )
            buf.Append( W("No Token") );
        else
            AppendTokenName( token, buf, dependency->pImport );

        if( mt->HasPerInstInfo() )
        {
            unsigned numDicts;
            if( hasCompleteExtents )
            {
                numDicts = mt->GetNumDicts();
                _ASSERTE(numDicts == CountDictionariesInClass(token, dependency->pImport));
            }
            else
            {
                numDicts = (DWORD)CountDictionariesInClass(token, dependency->pImport);
            }

            TADDR base = dac_cast<TADDR>(&(mt->GetPerInstInfo()[numDicts-1]));

            PTR_Dictionary dictionary( MethodTable::PerInstInfoElem_t::GetValueAtPtr(base) );
            unsigned numArgs = mt->GetNumGenericArgs();

            DictionaryToArgString( dictionary, numArgs, buf );
        }
    }
}

mdToken NativeImageDumper::ConvertToTypeDef( mdToken typeToken, IMetaDataImport2* (&pImport) )
{
    _ASSERTE( (TypeFromToken(typeToken) == mdtTypeDef) || (TypeFromToken(typeToken) == mdtTypeRef)
              || (TypeFromToken(typeToken) == mdtTypeSpec) );
    if( mdtTypeDef == TypeFromToken(typeToken) )
        return typeToken;
    if( mdtTypeRef == TypeFromToken(typeToken) )
    {
        //convert the ref to a def.
        mdToken scope;
        WCHAR trName[MAX_CLASS_NAME];
        ULONG trNameLen;
        IfFailThrow(pImport->GetTypeRefProps(typeToken, &scope, trName, _countof(trName), &trNameLen));
        _ASSERTE(trName[trNameLen-1] == 0);

        //scope is now a moduleRef or assemblyRef.  Find the IMetaData import for that Ref
        /* REVISIT_TODO Fri 10/6/2006
         * How do I handle moduleRefs?
         */
        _ASSERTE(TypeFromToken(scope) == mdtAssemblyRef);
        ReleaseHolder<IMetaDataAssemblyImport> pAssemblyImport;
        IfFailThrow(pImport->QueryInterface(IID_IMetaDataAssemblyImport,
                                            (void **)&pAssemblyImport));
        NativeImageDumper::Dependency * dep = GetDependency(scope, pAssemblyImport);

        pImport = dep->pImport;

        /* REVISIT_TODO Fri 10/6/2006
         * Does this work for inner types?
         */
        //now I have the correct MetaData.  Find the typeDef
        HRESULT hr = pImport->FindTypeDefByName(trName, mdTypeDefNil, &typeToken);
        while (hr == CLDB_E_RECORD_NOTFOUND)
        {
            // No matching TypeDef, try ExportedType
            pAssemblyImport = NULL;
            IfFailThrow(pImport->QueryInterface(IID_IMetaDataAssemblyImport,
                                                (void **)&pAssemblyImport));
            mdExportedType tkExportedType = mdExportedTypeNil;
            IfFailThrow(pAssemblyImport->FindExportedTypeByName(trName, mdExportedTypeNil, &tkExportedType));
            mdToken tkImplementation;
            IfFailThrow(pAssemblyImport->GetExportedTypeProps(tkExportedType, NULL, 0, NULL, &tkImplementation, NULL, NULL));
            dep = GetDependency(tkImplementation, pAssemblyImport);

            pImport = dep->pImport;
            hr = pImport->FindTypeDefByName(trName, mdTypeDefNil, &typeToken);
        }
        IfFailThrow(hr);
    }
    else
    {
        PCCOR_SIGNATURE pSig;
        ULONG cbSig;
        IfFailThrow(pImport->GetTypeSpecFromToken(typeToken, &pSig, &cbSig));
        //GENERICINST (CLASS|VALUETYPE) typeDefOrRef
        CorElementType et = CorSigUncompressElementType(pSig);
        _ASSERTE(et == ELEMENT_TYPE_GENERICINST);
        et = CorSigUncompressElementType(pSig);
        _ASSERTE((et == ELEMENT_TYPE_CLASS) || (et == ELEMENT_TYPE_VALUETYPE));
        typeToken = CorSigUncompressToken(pSig);
    }

    //we just removed one level of indirection.  We still might have a ref or spec.
    typeToken = ConvertToTypeDef(typeToken, pImport);
    _ASSERTE(TypeFromToken(typeToken) == mdtTypeDef);
    return typeToken;
}

SIZE_T NativeImageDumper::CountDictionariesInClass( mdToken typeToken, IMetaDataImport2 * pImport )
{
    SIZE_T myDicts; //either 0 or 1

    _ASSERTE((TypeFromToken(typeToken) == mdtTypeDef) || (TypeFromToken(typeToken) == mdtTypeRef)
             || (TypeFromToken(typeToken) == mdtTypeSpec));


    //for refs and specs, convert to a def.  This is a nop for defs.
    typeToken = ConvertToTypeDef(typeToken, pImport);

    _ASSERTE(TypeFromToken(typeToken) == mdtTypeDef);


    //count the number of generic arguments.  If there are any, then we have a dictionary.
    HCORENUM hEnum = NULL;
    mdGenericParam params[2];
    ULONG numParams = 0;
    IfFailThrow(pImport->EnumGenericParams(&hEnum, typeToken, params, _countof(params), &numParams));
    myDicts = (numParams > 0) ? 1 : 0;

    pImport->CloseEnum(hEnum);

    //get my parent for the recursive call.
    mdToken parent;
    IfFailThrow(pImport->GetTypeDefProps(typeToken, NULL, 0, NULL, NULL, &parent));
    return myDicts + (IsNilToken(parent) ? 0 : CountDictionariesInClass(parent, pImport));
}

const NativeImageDumper::EnumMnemonics s_Subsystems[] =
{
#define S_ENTRY(f,v) NativeImageDumper::EnumMnemonics(f, 0, v)
    S_ENTRY(IMAGE_SUBSYSTEM_UNKNOWN, W("Unknown")),
    S_ENTRY(IMAGE_SUBSYSTEM_NATIVE, W("Native")),
    S_ENTRY(IMAGE_SUBSYSTEM_WINDOWS_CUI, W("Windows CUI")),
    S_ENTRY(IMAGE_SUBSYSTEM_WINDOWS_GUI, W("Windows GUI")),
    S_ENTRY(IMAGE_SUBSYSTEM_OS2_CUI, W("OS/2 CUI")),
    S_ENTRY(IMAGE_SUBSYSTEM_POSIX_CUI, W("POSIX CUI")),
    S_ENTRY(IMAGE_SUBSYSTEM_WINDOWS_CE_GUI, W("WinCE GUI")),
    S_ENTRY(IMAGE_SUBSYSTEM_XBOX, W("XBox"))
#undef S_ENTRY
};

const NativeImageDumper::EnumMnemonics s_CorCompileHdrFlags[] =
{
#define CCHF_ENTRY(f) NativeImageDumper::EnumMnemonics(f, W(#f))
    CCHF_ENTRY(CORCOMPILE_HEADER_HAS_SECURITY_DIRECTORY),
    CCHF_ENTRY(CORCOMPILE_HEADER_IS_IBC_OPTIMIZED),
    CCHF_ENTRY(CORCOMPILE_HEADER_IS_READY_TO_RUN),
#undef CCHF_ENTRY
};

const NativeImageDumper::EnumMnemonics s_CorPEKind[] =
{
#define CPEK_ENTRY(f) NativeImageDumper::EnumMnemonics(f, W(#f))
    CPEK_ENTRY(peNot),
    CPEK_ENTRY(peILonly),
    CPEK_ENTRY(pe32BitRequired),
    CPEK_ENTRY(pe32Plus),
    CPEK_ENTRY(pe32Unmanaged),
    CPEK_ENTRY(pe32BitPreferred)
#undef CPEK_ENTRY
};
const NativeImageDumper::EnumMnemonics s_IFH_Machine[] =
{
#define IFH_ENTRY(f) NativeImageDumper::EnumMnemonics(f, 0, W(#f))
    IFH_ENTRY(IMAGE_FILE_MACHINE_UNKNOWN),
    IFH_ENTRY(IMAGE_FILE_MACHINE_I386),
    IFH_ENTRY(IMAGE_FILE_MACHINE_AMD64),
    IFH_ENTRY(IMAGE_FILE_MACHINE_ARMNT),
#undef IFH_ENTRY
};

const NativeImageDumper::EnumMnemonics s_IFH_Characteristics[] =
{
#define IFH_ENTRY(f) NativeImageDumper::EnumMnemonics(f, W(#f))
    IFH_ENTRY(IMAGE_FILE_RELOCS_STRIPPED),
    IFH_ENTRY(IMAGE_FILE_EXECUTABLE_IMAGE),
    IFH_ENTRY(IMAGE_FILE_LINE_NUMS_STRIPPED),
    IFH_ENTRY(IMAGE_FILE_LOCAL_SYMS_STRIPPED),
    IFH_ENTRY(IMAGE_FILE_AGGRESIVE_WS_TRIM),
    IFH_ENTRY(IMAGE_FILE_LARGE_ADDRESS_AWARE),
    IFH_ENTRY(IMAGE_FILE_BYTES_REVERSED_LO),
    IFH_ENTRY(IMAGE_FILE_32BIT_MACHINE),
    IFH_ENTRY(IMAGE_FILE_DEBUG_STRIPPED),
    IFH_ENTRY(IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP),
    IFH_ENTRY(IMAGE_FILE_NET_RUN_FROM_SWAP),
    IFH_ENTRY(IMAGE_FILE_SYSTEM),
    IFH_ENTRY(IMAGE_FILE_DLL),
    IFH_ENTRY(IMAGE_FILE_UP_SYSTEM_ONLY),
    IFH_ENTRY(IMAGE_FILE_BYTES_REVERSED_HI),
#undef IFH_ENTRY
};

const NativeImageDumper::EnumMnemonics s_ImportSectionType[] =
{
#define IST_ENTRY(f) NativeImageDumper::EnumMnemonics(f, 0, W(#f))
    IST_ENTRY(CORCOMPILE_IMPORT_TYPE_UNKNOWN),
    IST_ENTRY(CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD),
    IST_ENTRY(CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH),
    IST_ENTRY(CORCOMPILE_IMPORT_TYPE_STRING_HANDLE),
    IST_ENTRY(CORCOMPILE_IMPORT_TYPE_TYPE_HANDLE),
    IST_ENTRY(CORCOMPILE_IMPORT_TYPE_METHOD_HANDLE),
    IST_ENTRY(CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD),
#undef IST_ENTRY
};

const NativeImageDumper::EnumMnemonics s_ImportSectionFlags[] =
{
#define IST_FLAGS(f) NativeImageDumper::EnumMnemonics(f, W(#f))
    IST_FLAGS(CORCOMPILE_IMPORT_FLAGS_EAGER),
    IST_FLAGS(CORCOMPILE_IMPORT_FLAGS_CODE),
    IST_FLAGS(CORCOMPILE_IMPORT_FLAGS_PCODE),
#undef IST_FLAGS
};

void NativeImageDumper::DumpNativeHeader()
{
    PTR_CORCOMPILE_HEADER nativeHeader(m_decoder.GetNativeHeader());

    IF_OPT(NATIVE_INFO)
    {

#define WRITE_NATIVE_FIELD( name ) m_display->WriteFieldAddress(\
    # name, offsetof(CORCOMPILE_HEADER, name), \
    fieldsize(CORCOMPILE_HEADER, name), \
    RvaToDisplay( nativeHeader-> name . VirtualAddress ), \
    nativeHeader-> name . Size  )

        m_display->StartStructure( "CORCOMPILE_HEADER",
                                   DPtrToPreferredAddr(nativeHeader),
                                   sizeof(*nativeHeader) );

        DisplayWriteFieldUInt( Signature, nativeHeader->Signature, CORCOMPILE_HEADER, ALWAYS );
        DisplayWriteFieldUInt( MajorVersion, nativeHeader->MajorVersion, CORCOMPILE_HEADER, ALWAYS );
        DisplayWriteFieldUInt( MinorVersion, nativeHeader->MinorVersion, CORCOMPILE_HEADER, ALWAYS );

        WRITE_NATIVE_FIELD(HelperTable);

        WRITE_NATIVE_FIELD(ImportSections);
        PTR_CORCOMPILE_IMPORT_SECTION pImportSections =
            nativeHeader->ImportSections.VirtualAddress
            + PTR_TO_TADDR(m_decoder.GetBase());
        DisplayStartArray( "ImportSections", NULL, ALWAYS );
        for( COUNT_T i = 0; i < nativeHeader->ImportSections.Size
             / sizeof(*pImportSections); ++i )
        {
            DisplayStartStructure( "CORCOMPILE_IMPORT_SECTION",
                                   DPtrToPreferredAddr(pImportSections + i),
                                   sizeof(pImportSections[i]), ALWAYS );
            DisplayWriteElementAddress( "Section",
                                        RvaToDisplay(pImportSections[i].Section.VirtualAddress),
                                        pImportSections[i].Section.Size, ALWAYS );

            DisplayWriteFieldEnumerated( Flags, pImportSections[i].Flags,
                                         CORCOMPILE_IMPORT_SECTION, s_ImportSectionFlags, W(", "), ALWAYS );
            DisplayWriteFieldEnumerated( Type, pImportSections[i].Type,
                                         CORCOMPILE_IMPORT_SECTION, s_ImportSectionType, W(""), ALWAYS );

            DisplayWriteFieldUInt( EntrySize, pImportSections[i].EntrySize,
                                   CORCOMPILE_IMPORT_SECTION, ALWAYS );
            DisplayWriteFieldUInt( Signatures, pImportSections[i].Signatures,
                                   CORCOMPILE_IMPORT_SECTION, ALWAYS );
            DisplayWriteFieldUInt( AuxiliaryData, pImportSections[i].AuxiliaryData,
                                    CORCOMPILE_IMPORT_SECTION, ALWAYS );
            DisplayEndStructure( ALWAYS ); //PTR_CORCOMPILE_IMPORT_SECTION

        }
        DisplayEndArray( NULL, ALWAYS ); //delayLoads

        WRITE_NATIVE_FIELD(VersionInfo);
        WRITE_NATIVE_FIELD(DebugMap);
        WRITE_NATIVE_FIELD(ModuleImage);
        WRITE_NATIVE_FIELD(CodeManagerTable);
        WRITE_NATIVE_FIELD(ProfileDataList);
        WRITE_NATIVE_FIELD(ManifestMetaData);

        WRITE_NATIVE_FIELD(VirtualSectionsTable);
        DisplayStartArray( "VirtualSections", W("%-48s%s"), SLIM_MODULE_TBLS );
        PTR_CORCOMPILE_VIRTUAL_SECTION_INFO sects( nativeHeader->VirtualSectionsTable.VirtualAddress + PTR_TO_TADDR(m_decoder.GetBase()) );
        COUNT_T numVirtualSections = nativeHeader->VirtualSectionsTable.Size / sizeof (CORCOMPILE_VIRTUAL_SECTION_INFO);

        for( COUNT_T i = 0; i < numVirtualSections; ++i )
        {
            TempBuffer sectionNameBuf;
            TempBuffer sectionFlags;
            StackScratchBuffer scratch;

            sectionNameBuf.Append(g_sectionNames[VirtualSectionData::VirtualSectionType(sects[i].SectionType)]);

            EnumFlagsToString( sects[i].SectionType, s_virtualSectionFlags, dim(s_virtualSectionFlags),
                       W(" | "), sectionFlags);

            sectionNameBuf.Append(W(" ["));
            sectionNameBuf.Append(sectionFlags);
            sectionNameBuf.Append(W("]"));

            DisplayStartElement( "Section", SLIM_MODULE_TBLS );
            DisplayWriteElementString("Name", sectionNameBuf.GetANSI(scratch), SLIM_MODULE_TBLS);

            DisplayWriteElementAddress( "Address",
                                        RvaToDisplay(sects[i].VirtualAddress),
                                        sects[i].Size,
                                        SLIM_MODULE_TBLS );
            DisplayEndElement( SLIM_MODULE_TBLS ); //Section
        }
        DisplayEndArray( "Total VirtualSections", SLIM_MODULE_TBLS );

        WRITE_NATIVE_FIELD(EEInfoTable);

#undef WRITE_NATIVE_FIELD
        DisplayWriteFieldEnumerated( Flags, nativeHeader->Flags,
                                     CORCOMPILE_HEADER, s_CorCompileHdrFlags, W(", "),
                                     NATIVE_INFO );

        DisplayWriteFieldEnumerated( PEKind, nativeHeader->PEKind,
                                     CORCOMPILE_HEADER, s_CorPEKind, W(", "),
                                     NATIVE_INFO );

        DisplayWriteFieldEnumerated( COR20Flags, nativeHeader->COR20Flags,
                                     CORCOMPILE_HEADER, s_CorHdrFlags, W(", "),
                                     NATIVE_INFO );

        DisplayWriteFieldEnumerated( Machine, nativeHeader->Machine,
                                     CORCOMPILE_HEADER, s_IFH_Machine,
                                     W(""), NATIVE_INFO );
        DisplayWriteFieldEnumerated( Characteristics,
                                     nativeHeader->Characteristics,
                                     CORCOMPILE_HEADER, s_IFH_Characteristics,
                                     W(", "), NATIVE_INFO );

        m_display->EndStructure(); //CORCOMPILE_HEADER
    }
}

const NativeImageDumper::EnumMnemonics s_RelocType[] =
{
#define REL_ENTRY(x) NativeImageDumper::EnumMnemonics( x, 0, W(#x))
    REL_ENTRY(IMAGE_REL_BASED_ABSOLUTE),
    REL_ENTRY(IMAGE_REL_BASED_HIGHLOW),
    REL_ENTRY(IMAGE_REL_BASED_DIR64),
    REL_ENTRY(IMAGE_REL_BASED_THUMB_MOV32),
#undef REL_ENTRY
};

void NativeImageDumper::DumpBaseRelocs()
{
    COUNT_T size;
    TADDR data;

    data = m_decoder.GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_BASERELOC, &size);

    if (size != 0)
    {
        DisplayStartStructure( "Relocations", DataPtrToDisplay(data), size,
                                    ALWAYS );

        while (size != 0)
        {
            IMAGE_BASE_RELOCATION * pBaseRelocation = dac_cast<DPTR(IMAGE_BASE_RELOCATION)>(data);
            _ASSERTE(size >= pBaseRelocation->SizeOfBlock);

            SIZE_T rel = sizeof(IMAGE_BASE_RELOCATION);
            while (rel < pBaseRelocation->SizeOfBlock)
            {
                USHORT typeOffset = *PTR_USHORT(data + rel);

                DisplayStartElement( "Entry", ALWAYS );

                DisplayWriteElementPointer( "Address", RvaToDisplay(pBaseRelocation->VirtualAddress + (typeOffset & 0xFFF)), ALWAYS );

                DisplayWriteElementEnumerated( "Type", (typeOffset >> 12),
                                        s_RelocType, W(", "), ALWAYS );

                DisplayEndElement( ALWAYS ); //Entry

                rel += sizeof(USHORT);
            }

            data += pBaseRelocation->SizeOfBlock;
            size -= pBaseRelocation->SizeOfBlock;
        }

        DisplayEndStructure( ALWAYS ); //Relocations
    }
}

void NativeImageDumper::DumpHelperTable()
{
    COUNT_T size;
    TADDR data;

    data = TO_TADDR(m_decoder.GetNativeHelperTable(&size));
    if( size != 0 )
    {
        DisplayStartStructure( "HelperTable", DataPtrToDisplay(data), size,
                                    ALWAYS );

        TADDR curEntry   = data;
        TADDR tableEnd   = data + size;

        while (curEntry < tableEnd)
        {
            DWORD dwHelper = *PTR_DWORD(curEntry);

            int iHelper = (USHORT)dwHelper;
            _ASSERTE(iHelper < CORINFO_HELP_COUNT);

            DisplayStartStructure( "Helper",
                                   DataPtrToDisplay(curEntry), (dwHelper & CORCOMPILE_HELPER_PTR) ? sizeof(TADDR) : HELPER_TABLE_ENTRY_LEN,
                                   ALWAYS );

            DisplayWriteElementUInt( "dwHelper", dwHelper, ALWAYS );
            DisplayWriteElementString( "Name", g_helperNames[iHelper], ALWAYS );

            DisplayEndStructure( ALWAYS ); //Helper

            curEntry += (dwHelper & CORCOMPILE_HELPER_PTR) ? sizeof(TADDR) : HELPER_TABLE_ENTRY_LEN;
        }

        DisplayEndStructure( ALWAYS ); //HelperTable
    }
}

// TODO: fix these to work with the updated flags in MethodTable, AND to understand
//       the new overloading of component size...

NativeImageDumper::EnumMnemonics s_MTFlagsLow[] =
{
#define MTFLAG_ENTRY(x) \
    NativeImageDumper::EnumMnemonics(MethodTable::enum_flag_ ## x, W(#x))

    MTFLAG_ENTRY(UNUSED_ComponentSize_1),
    MTFLAG_ENTRY(StaticsMask),
    MTFLAG_ENTRY(StaticsMask_NonDynamic),
    MTFLAG_ENTRY(StaticsMask_Dynamic),
    MTFLAG_ENTRY(StaticsMask_Generics),
    MTFLAG_ENTRY(StaticsMask_CrossModuleGenerics),
    MTFLAG_ENTRY(NotInPZM),
    MTFLAG_ENTRY(GenericsMask),
    MTFLAG_ENTRY(GenericsMask_NonGeneric),
    MTFLAG_ENTRY(GenericsMask_GenericInst),
    MTFLAG_ENTRY(GenericsMask_SharedInst),
    MTFLAG_ENTRY(GenericsMask_TypicalInst),
    MTFLAG_ENTRY(HasVariance),
    MTFLAG_ENTRY(HasDefaultCtor),
    MTFLAG_ENTRY(HasPreciseInitCctors),
#if defined(FEATURE_HFA)
    MTFLAG_ENTRY(IsHFA),
#endif // FEATURE_HFA
#if defined(UNIX_AMD64_ABI)
    MTFLAG_ENTRY(IsRegStructPassed),
#endif // UNIX_AMD64_ABI
    MTFLAG_ENTRY(IsByRefLike),
    MTFLAG_ENTRY(UNUSED_ComponentSize_5),
    MTFLAG_ENTRY(UNUSED_ComponentSize_6),
    MTFLAG_ENTRY(UNUSED_ComponentSize_7),
#undef MTFLAG_ENTRY
};

NativeImageDumper::EnumMnemonics s_MTFlagsHigh[] =
{
#define MTFLAG_ENTRY(x) \
    NativeImageDumper::EnumMnemonics(MethodTable::enum_flag_ ## x, W(#x))

#define MTFLAG_CATEGORY_ENTRY(x) \
    NativeImageDumper::EnumMnemonics(MethodTable::enum_flag_Category_ ## x, MethodTable::enum_flag_Category_Mask, W("Category_") W(#x))

#define MTFLAG_CATEGORY_ENTRY_WITH_MASK(x, m) \
    NativeImageDumper::EnumMnemonics(MethodTable::enum_flag_Category_ ## x, MethodTable::enum_flag_Category_ ## m, W("Category_") W(#x))

    MTFLAG_CATEGORY_ENTRY(Class),
    MTFLAG_CATEGORY_ENTRY(Unused_1),
    MTFLAG_CATEGORY_ENTRY(Unused_2),
    MTFLAG_CATEGORY_ENTRY(Unused_3),
    MTFLAG_CATEGORY_ENTRY(ValueType),
    MTFLAG_CATEGORY_ENTRY(Nullable),
    MTFLAG_CATEGORY_ENTRY(PrimitiveValueType),
    MTFLAG_CATEGORY_ENTRY(TruePrimitive),

    MTFLAG_CATEGORY_ENTRY(Interface),
    MTFLAG_CATEGORY_ENTRY(Unused_4),
    MTFLAG_CATEGORY_ENTRY(Unused_5),
    MTFLAG_CATEGORY_ENTRY(Unused_6),

    MTFLAG_CATEGORY_ENTRY_WITH_MASK(Array, Array_Mask),
    MTFLAG_CATEGORY_ENTRY_WITH_MASK(IfArrayThenSzArray, IfArrayThenSzArray),

#undef MTFLAG_CATEGORY_ENTRY_WITH_MASK
#undef MTFLAG_CATEGORY_ENTRY

    MTFLAG_ENTRY(HasFinalizer),
    MTFLAG_ENTRY(IfNotInterfaceThenMarshalable),
    MTFLAG_ENTRY(IDynamicInterfaceCastable),
#if defined(FEATURE_ICASTABLE)
    MTFLAG_ENTRY(ICastable),
#endif
    MTFLAG_ENTRY(HasIndirectParent),
    MTFLAG_ENTRY(ContainsPointers),
    MTFLAG_ENTRY(HasTypeEquivalence),
    MTFLAG_ENTRY(HasCriticalFinalizer),
    MTFLAG_ENTRY(Collectible),
    MTFLAG_ENTRY(ContainsGenericVariables),
#if defined(FEATURE_COMINTEROP)
    MTFLAG_ENTRY(ComObject),
#endif
    MTFLAG_ENTRY(HasComponentSize),
#undef MTFLAG_ENTRY
};


NativeImageDumper::EnumMnemonics s_MTFlags2[] =
{
#define MTFLAG2_ENTRY(x) \
    NativeImageDumper::EnumMnemonics(MethodTable::enum_flag_ ## x, W(#x))
    MTFLAG2_ENTRY(HasPerInstInfo),
    MTFLAG2_ENTRY(HasInterfaceMap),
    MTFLAG2_ENTRY(HasDispatchMapSlot),
    MTFLAG2_ENTRY(HasNonVirtualSlots),
    MTFLAG2_ENTRY(HasModuleOverride),
    MTFLAG2_ENTRY(IsZapped),
    MTFLAG2_ENTRY(IsPreRestored),
    MTFLAG2_ENTRY(HasModuleDependencies),
    MTFLAG2_ENTRY(IsIntrinsicType),
    MTFLAG2_ENTRY(RequiresDispatchTokenFat),
    MTFLAG2_ENTRY(HasCctor),
#ifdef FEATURE_64BIT_ALIGNMENT
    MTFLAG2_ENTRY(RequiresAlign8),
#endif
    MTFLAG2_ENTRY(HasBoxedRegularStatics),
    MTFLAG2_ENTRY(HasSingleNonVirtualSlot),
    MTFLAG2_ENTRY(DependsOnEquivalentOrForwardedStructs),
#undef MTFLAG2_ENTRY
};

NativeImageDumper::EnumMnemonics s_WriteableMTFlags[] =
{
#define WMTFLAG_ENTRY(x) \
    NativeImageDumper::EnumMnemonics(MethodTableWriteableData::enum_flag_ ## x,\
                                     W(#x))

        WMTFLAG_ENTRY(Unrestored),
        WMTFLAG_ENTRY(HasApproxParent),
        WMTFLAG_ENTRY(UnrestoredTypeKey),
        WMTFLAG_ENTRY(IsNotFullyLoaded),
        WMTFLAG_ENTRY(DependenciesLoaded),

#ifdef _DEBUG
        WMTFLAG_ENTRY(ParentMethodTablePointerValid),
#endif

        WMTFLAG_ENTRY(NGEN_IsFixedUp),
        WMTFLAG_ENTRY(NGEN_IsNeedsRestoreCached),
        WMTFLAG_ENTRY(NGEN_CachedNeedsRestore),
#undef WMTFLAG_ENTRY
};

static NativeImageDumper::EnumMnemonics s_CorElementType[] =
{
#define CET_ENTRY(x) NativeImageDumper::EnumMnemonics(ELEMENT_TYPE_ ## x, 0, W("ELEMENT_TYPE_") W(#x))
    CET_ENTRY(END),
    CET_ENTRY(VOID),
    CET_ENTRY(BOOLEAN),
    CET_ENTRY(CHAR),
    CET_ENTRY(I1),
    CET_ENTRY(U1),
    CET_ENTRY(I2),
    CET_ENTRY(U2),
    CET_ENTRY(I4),
    CET_ENTRY(U4),
    CET_ENTRY(I8),
    CET_ENTRY(U8),
    CET_ENTRY(R4),
    CET_ENTRY(R8),
    CET_ENTRY(STRING),
    CET_ENTRY(PTR),
    CET_ENTRY(BYREF),
    CET_ENTRY(VALUETYPE),
    CET_ENTRY(CLASS),
    CET_ENTRY(VAR),
    CET_ENTRY(ARRAY),
    CET_ENTRY(GENERICINST),
    CET_ENTRY(TYPEDBYREF),
    CET_ENTRY(VALUEARRAY_UNSUPPORTED),
    CET_ENTRY(I),
    CET_ENTRY(U),
    CET_ENTRY(R_UNSUPPORTED),
    CET_ENTRY(FNPTR),
    CET_ENTRY(OBJECT),
    CET_ENTRY(SZARRAY),
    CET_ENTRY(MVAR),
    CET_ENTRY(CMOD_REQD),
    CET_ENTRY(CMOD_OPT),
    CET_ENTRY(INTERNAL),

    CET_ENTRY(SENTINEL),
    CET_ENTRY(PINNED),
#undef CET_ENTRY
};

void NativeImageDumper::DoWriteFieldCorElementType( const char * name,
                                                    unsigned offset,
                                                    unsigned fieldSize,
                                                    CorElementType type )
{
    TempBuffer buf;
    EnumFlagsToString( (int)type, s_CorElementType, dim(s_CorElementType),
                       W(""), buf );
    m_display->WriteFieldEnumerated( name, offset, fieldSize, (unsigned)type,
                                     (const WCHAR *) buf );

}

static NativeImageDumper::EnumMnemonics s_CorTypeAttr[] =
{
#define CTA_ENTRY(x) NativeImageDumper::EnumMnemonics( x, W(#x) )

#define CTA_VISIBILITY_ENTRY(x) NativeImageDumper::EnumMnemonics( x, tdVisibilityMask, W(#x) )
    CTA_VISIBILITY_ENTRY(tdNotPublic),
    CTA_VISIBILITY_ENTRY(tdPublic),
    CTA_VISIBILITY_ENTRY(tdNestedPublic),
    CTA_VISIBILITY_ENTRY(tdNestedPrivate),
    CTA_VISIBILITY_ENTRY(tdNestedFamily),
    CTA_VISIBILITY_ENTRY(tdNestedAssembly),
    CTA_VISIBILITY_ENTRY(tdNestedFamANDAssem),
    CTA_VISIBILITY_ENTRY(tdNestedFamORAssem),
#undef CTA_VISIBILITY_ENTRY

    CTA_ENTRY(tdSequentialLayout),
    CTA_ENTRY(tdExplicitLayout),

    CTA_ENTRY(tdInterface),

    CTA_ENTRY(tdAbstract),
    CTA_ENTRY(tdSealed),
    CTA_ENTRY(tdSpecialName),

    CTA_ENTRY(tdImport),
    CTA_ENTRY(tdSerializable),

    CTA_ENTRY(tdUnicodeClass),
    CTA_ENTRY(tdAutoClass),
    CTA_ENTRY(tdCustomFormatClass),
    CTA_ENTRY(tdCustomFormatMask),

    CTA_ENTRY(tdBeforeFieldInit),
    CTA_ENTRY(tdForwarder),

    CTA_ENTRY(tdRTSpecialName),
    CTA_ENTRY(tdHasSecurity)
#undef CTA_ENTRY
};
static NativeImageDumper::EnumMnemonics s_VMFlags[] =
{
#define VMF_ENTRY(x) NativeImageDumper::EnumMnemonics( EEClass::VMFLAG_ ## x, W(#x) )

#ifdef FEATURE_READYTORUN
        VMF_ENTRY(LAYOUT_DEPENDS_ON_OTHER_MODULES),
#endif
        VMF_ENTRY(DELEGATE),
        VMF_ENTRY(FIXED_ADDRESS_VT_STATICS),
        VMF_ENTRY(HASLAYOUT),
        VMF_ENTRY(ISNESTED),
        VMF_ENTRY(IS_EQUIVALENT_TYPE),

        VMF_ENTRY(HASOVERLAYEDFIELDS),
        VMF_ENTRY(HAS_FIELDS_WHICH_MUST_BE_INITED),
        VMF_ENTRY(UNSAFEVALUETYPE),

        VMF_ENTRY(BESTFITMAPPING_INITED),
        VMF_ENTRY(BESTFITMAPPING),
        VMF_ENTRY(THROWONUNMAPPABLECHAR),

        VMF_ENTRY(NO_GUID),
        VMF_ENTRY(HASNONPUBLICFIELDS),
        VMF_ENTRY(PREFER_ALIGN8),

#ifdef FEATURE_COMINTEROP
        VMF_ENTRY(SPARSE_FOR_COMINTEROP),
        VMF_ENTRY(HASCOCLASSATTRIB),
        VMF_ENTRY(COMEVENTITFMASK),
#endif // FEATURE_COMINTEROP

        VMF_ENTRY(NOT_TIGHTLY_PACKED),
        VMF_ENTRY(CONTAINS_METHODIMPLS),
#ifdef FEATURE_COMINTEROP
        VMF_ENTRY(MARSHALINGTYPE_MASK),
        VMF_ENTRY(MARSHALINGTYPE_INHIBIT),
        VMF_ENTRY(MARSHALINGTYPE_FREETHREADED),
        VMF_ENTRY(MARSHALINGTYPE_STANDARD),
#endif
#undef VMF_ENTRY
};
static NativeImageDumper::EnumMnemonics s_CorFieldAttr[] =
{
#define CFA_ENTRY(x) NativeImageDumper::EnumMnemonics( x, W(#x) )

#define CFA_ACCESS_ENTRY(x) NativeImageDumper::EnumMnemonics( x, fdFieldAccessMask, W(#x) )
    CFA_ENTRY(fdPrivateScope),
    CFA_ENTRY(fdPrivate),
    CFA_ENTRY(fdFamANDAssem),
    CFA_ENTRY(fdAssembly),
    CFA_ENTRY(fdFamily),
    CFA_ENTRY(fdFamORAssem),
    CFA_ENTRY(fdPublic),
#undef CFA_ACCESS_ENTRY

    CFA_ENTRY(fdStatic),
    CFA_ENTRY(fdInitOnly),
    CFA_ENTRY(fdLiteral),
    CFA_ENTRY(fdNotSerialized),

    CFA_ENTRY(fdSpecialName),

    CFA_ENTRY(fdPinvokeImpl),

    CFA_ENTRY(fdRTSpecialName),
    CFA_ENTRY(fdHasFieldMarshal),
    CFA_ENTRY(fdHasDefault),
    CFA_ENTRY(fdHasFieldRVA),
#undef CFA_ENTRY
};

NativeImageDumper::EnumMnemonics NativeImageDumper::s_MDFlag2[] =
{
#define MDF2_ENTRY(x) NativeImageDumper::EnumMnemonics( MethodDesc::enum_flag2_ ## x, W("enum_flag2_") W(#x) )
    MDF2_ENTRY(HasStableEntryPoint),
    MDF2_ENTRY(HasPrecode),
    MDF2_ENTRY(IsUnboxingStub),
    MDF2_ENTRY(HasNativeCodeSlot),
#undef MDF2_ENTRY
};

NativeImageDumper::EnumMnemonics NativeImageDumper::s_MDC[] =
{
#define MDC_ENTRY(x) NativeImageDumper::EnumMnemonics( x, W(#x) )

#define MDC_ENTRY_CLASSIFICATION(x) NativeImageDumper::EnumMnemonics( x, mdcClassification, W(#x) )
    MDC_ENTRY_CLASSIFICATION(mcIL),
    MDC_ENTRY_CLASSIFICATION(mcFCall),
    MDC_ENTRY_CLASSIFICATION(mcNDirect),
    MDC_ENTRY_CLASSIFICATION(mcEEImpl),
    MDC_ENTRY_CLASSIFICATION(mcArray),
    MDC_ENTRY_CLASSIFICATION(mcInstantiated),
#ifdef FEATURE_COMINTEROP
    MDC_ENTRY_CLASSIFICATION(mcComInterop),
#endif // FEATURE_COMINTEROP
    MDC_ENTRY_CLASSIFICATION(mcDynamic),
#undef MDC_ENTRY_CLASSIFICATION

    MDC_ENTRY(mdcHasNonVtableSlot),
    MDC_ENTRY(mdcMethodImpl),

    // Method is static
    MDC_ENTRY(mdcStatic),

    MDC_ENTRY(mdcDuplicate),
    MDC_ENTRY(mdcVerifiedState),
    MDC_ENTRY(mdcVerifiable),
    MDC_ENTRY(mdcNotInline),
    MDC_ENTRY(mdcSynchronized),
    MDC_ENTRY(mdcRequiresFullSlotNumber),
#undef MDC_ENTRY
};



void NativeImageDumper::DumpTypes(PTR_Module module)
{
    _ASSERTE(CHECK_OPT(EECLASSES) || CHECK_OPT(METHODTABLES)
             || CHECK_OPT(TYPEDESCS));

    IF_OPT_OR3(METHODTABLES, EECLASSES, TYPEDESCS)
        m_display->StartCategory( "Types" );
    IF_OPT(METHODTABLES)
    {
        //there may be duplicates in the list.  Remove them before moving on.
        COUNT_T mtCount = m_discoveredMTs.GetCount();

#if !defined(FEATURE_CORESYSTEM) // no STL right now
        std::sort(&*m_discoveredMTs.Begin(),
                  (&*m_discoveredMTs.Begin())
                  + (m_discoveredMTs.End() - m_discoveredMTs.Begin()));
        PTR_MethodTable* newEnd = std::unique(&*m_discoveredMTs.Begin(),
                                              (&*m_discoveredMTs.Begin())
                                              + (m_discoveredMTs.End()
                                                 - m_discoveredMTs.Begin()));
        mtCount = (COUNT_T)(newEnd - &*m_discoveredMTs.Begin());
#endif

        DisplayStartArray( "MethodTables", NULL, METHODTABLES );
        for(COUNT_T i = 0; i < mtCount; ++i )
        {
            PTR_MethodTable mt = m_discoveredMTs[i];
            if( mt == NULL )
                continue;
            DumpMethodTable( mt, "MethodTable", module );
        }

        DisplayEndArray( "Total MethodTables", METHODTABLES );

        DisplayStartArray( "MethodTableSlotChunks", NULL, METHODTABLES );
        {
            COUNT_T slotChunkCount = m_discoveredSlotChunks.GetCount();
#if !defined(FEATURE_CORESYSTEM) // no STL right now
            std::sort(&*m_discoveredSlotChunks.Begin(),
                      (&*m_discoveredSlotChunks.Begin())
                      + (m_discoveredSlotChunks.End() - m_discoveredSlotChunks.Begin()));
            SlotChunk *newEndChunks = std::unique(&*m_discoveredSlotChunks.Begin(),
                                              (&*m_discoveredSlotChunks.Begin())
                                              + (m_discoveredSlotChunks.End() - m_discoveredSlotChunks.Begin()));
            slotChunkCount = (COUNT_T)(newEndChunks - &*m_discoveredSlotChunks.Begin());
#endif

            for (COUNT_T i = 0; i < slotChunkCount; ++i)
            {
                DumpMethodTableSlotChunk(m_discoveredSlotChunks[i].addr,
                                         m_discoveredSlotChunks[i].nSlots,
                                         m_discoveredSlotChunks[i].isRelative);
            }
        }
        DisplayEndArray( "Total MethodTableSlotChunks", METHODTABLES );
    }
    IF_OPT(EECLASSES)
    {
        DisplayStartArray( "EEClasses", NULL, EECLASSES );

        //there may be duplicates in the list.  Remove them before moving on.
        COUNT_T clazzCount = m_discoveredClasses.GetCount();
#if !defined(FEATURE_CORESYSTEM) // no STL right now
        std::sort(&*m_discoveredClasses.Begin(),
                  (&*m_discoveredClasses.Begin())
                  + (m_discoveredClasses.End() - m_discoveredClasses.Begin()));
        PTR_MethodTable * newEndClazz = std::unique(&*m_discoveredClasses.Begin(),
                                               (&*m_discoveredClasses.Begin())
                                               +(m_discoveredClasses.End()
                                                 -m_discoveredClasses.Begin()));
        clazzCount = (COUNT_T)(newEndClazz - &*m_discoveredClasses.Begin());
#endif

        for(COUNT_T i = 0; i < clazzCount; ++i )
        {
            PTR_MethodTable mt = m_discoveredClasses[i];
            if( mt == NULL )
                continue;
            DumpEEClassForMethodTable( mt );
        }

        DisplayEndArray( "Total EEClasses", EECLASSES ); //EEClasses

    }
    IF_OPT(TYPEDESCS)
    {
        DisplayStartArray( "TypeDescs", NULL, TYPEDESCS );

        //there may be duplicates in the list.  Remove them before moving on.
        COUNT_T tdCount = m_discoveredTypeDescs.GetCount();
#if !defined(FEATURE_CORESYSTEM) // no STL right now
        std::sort(&*m_discoveredTypeDescs.Begin(),
                  (&*m_discoveredTypeDescs.Begin())
                  + (m_discoveredTypeDescs.End()
                     - m_discoveredTypeDescs.Begin()));
        PTR_TypeDesc* newEndTD = std::unique(&*m_discoveredTypeDescs.Begin(),
                                             (&*m_discoveredTypeDescs.Begin())
                                             +(m_discoveredTypeDescs.End()
                                               -m_discoveredTypeDescs.Begin()));
        tdCount = (COUNT_T)(newEndTD - &*m_discoveredTypeDescs.Begin());
#endif

        for(COUNT_T i = 0; i < tdCount; ++i )
        {
            PTR_TypeDesc td = m_discoveredTypeDescs[i];
            if( td == NULL )
                continue;
            DumpTypeDesc( td );
        }

        DisplayEndArray( "Total TypeDescs", TYPEDESCS ); //EEClasses

    }
    IF_OPT_OR3(EECLASSES, METHODTABLES, TYPEDESCS)
        m_display->EndCategory(); //Types
}

PTR_EEClass NativeImageDumper::GetClassFromMT( PTR_MethodTable mt )
{
    /* REVISIT_TODO Tue 10/11/2005
     * Handle fixups
     */
    _ASSERTE( mt->IsClassPointerValid() );
    PTR_EEClass clazz( mt->GetClass() );
    return clazz;
}
PTR_MethodTable NativeImageDumper::GetParent( PTR_MethodTable mt )
{
    /* REVISIT_TODO Thu 12/01/2005
     * Handle fixups
     */
    PTR_MethodTable parent( ReadPointerMaybeNull((MethodTable*) mt, &MethodTable::m_pParentMethodTable, mt->GetFlagHasIndirectParent()) );
    _ASSERTE(!CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(parent)));
    return parent;
}

//Counts the FieldDescs in a class.  This is all of the non-static and static
//non-literal fields.
SIZE_T NativeImageDumper::CountFields( PTR_MethodTable mt )
{
    SIZE_T fieldCount = 0;

    HCORENUM hEnum = NULL;

    const Dependency * dep = GetDependencyFromMT(mt);
    mdToken classToken = mt->GetCl();

    _ASSERTE(dep);
    _ASSERTE(dep->pImport);

    //Arrays have no token.
    if( RidFromToken(classToken) == 0 )
        return 0;

    for (;;)
    {
        mdToken fields[1];
        ULONG numFields;

        IfFailThrow(dep->pImport->EnumFields( &hEnum, classToken, fields,
                                       1, &numFields));

        if (numFields == 0)
            break;

        DWORD dwAttr;
        IfFailThrow(dep->pImport->GetFieldProps( fields[0], NULL, NULL, 0,
                                                 NULL, & dwAttr, NULL, NULL,
                                                 NULL, NULL, NULL ) );
        if( !IsFdStatic(dwAttr) || !IsFdLiteral(dwAttr) )
            ++fieldCount;
    }
    dep->pImport->CloseEnum(hEnum);
    return fieldCount;
}
const NativeImageDumper::Dependency*
NativeImageDumper::GetDependencyFromMT( PTR_MethodTable mt )
{
    if( !mt->IsClassPointerValid() )
    {
        //This code will not work for out of module dependencies.
        _ASSERTE(isSelf(GetDependencyForPointer(PTR_TO_TADDR(mt))));

        //the EEClass is a fixup.  The home for that fixup tells us the
        //home for the metadata.
        unsigned rva = CORCOMPILE_UNTAG_TOKEN(mt->GetCanonicalMethodTableFixup());
        return GetDependencyForFixup(rva);
    }
    PTR_Module module = mt->GetModule();
    if( CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(module)) )
    {
        unsigned rva = CORCOMPILE_UNTAG_TOKEN(PTR_TO_TADDR(module));
        return GetDependencyForFixup(rva);
    }
    return GetDependencyForModule(module);
}
const NativeImageDumper::Dependency*
NativeImageDumper::GetDependencyFromFD( PTR_FieldDesc fd )
{
    PTR_MethodTable mt = fd->GetApproxEnclosingMethodTable();
    if( CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(mt)) )
    {
        //This code will not work for out of module dependencies.
        _ASSERTE(isSelf(GetDependencyForPointer(PTR_TO_TADDR(fd))));

        //the MethodTable has a fixup.  The home for that fixup tells us the
        //home for the metadata.
        unsigned rva = CORCOMPILE_UNTAG_TOKEN(PTR_TO_TADDR(mt));
        return GetDependencyForFixup(rva);
    }
    return GetDependencyFromMT(mt);
}
const NativeImageDumper::Dependency*
NativeImageDumper::GetDependencyFromMD( PTR_MethodDesc md )
{
    PTR_MethodDescChunk chunk( md->GetMethodDescChunk() );
    PTR_MethodTable mt = chunk->GetMethodTable();
    if( CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(mt)) )
    {
        //This code will not work for out of module dependencies.
        _ASSERTE(isSelf(GetDependencyForPointer(PTR_TO_TADDR(md))));

        //the MethodTable has a fixup.  The home for that fixup tells us the
        //home for the metadata.
        unsigned rva = CORCOMPILE_UNTAG_TOKEN(PTR_TO_TADDR(mt));
        return GetDependencyForFixup(rva);
    }
    return GetDependencyFromMT(mt);
}

BOOL NativeImageDumper::DoWriteFieldAsFixup( const char * name,
                                             unsigned offset,
                                             unsigned fieldSize, TADDR fixup)
{
    if( !CORCOMPILE_IS_POINTER_TAGGED(fixup) )
        return FALSE;
    if( UINT_MAX == offset )
        m_display->StartVStructure( name );
    else
        m_display->StartVStructureWithOffset( name, offset, fieldSize );

    WriteElementsFixupBlob( NULL, fixup );
    m_display->EndVStructure(); //name
    return TRUE;
}

void AppendTypeQualifier( CorElementType kind, DWORD rank, SString& buf )
{
    switch( kind )
    {
    case ELEMENT_TYPE_BYREF :
        buf.Append( W("&") );
        break;
    case ELEMENT_TYPE_PTR :
        buf.Append( W("*") );
        break;
    case ELEMENT_TYPE_SZARRAY :
        buf.Append( W("[]") );
        break;
    case ELEMENT_TYPE_ARRAY :
        if( rank == 1 )
        {
            buf.Append( W("[*]") );
        }
        else
        {
            buf.Append( W("[") );
            for( COUNT_T i = 0; i < rank; ++i )
                buf.Append( W(","));
            buf.Append( W("]") );
        }
        break;
    default :
        break;
    }
}
void NativeImageDumper::TypeDescToString( PTR_TypeDesc td, SString& buf )
{
    _ASSERTE(!(PTR_TO_TADDR(td) & 0x2));
    if( td->IsGenericVariable() )
    {
        PTR_TypeVarTypeDesc tvtd( PTR_TO_TADDR(td) );
        //From code:TypeString::AppendType
        mdGenericParam token = tvtd->GetToken();
        PTR_Module module(tvtd->GetModule());
        IMetaDataImport2 * pImport;
        if( CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(module)) )
        {
            if (!isSelf(GetDependencyForPointer(PTR_TO_TADDR(td))))
            {
                //this is an RVA from another hardbound dependency.  We cannot decode it
                buf.Append(W("OUT_OF_MODULE_FIXUP"));
                return;
            }
            else
            {
                RVA rva = CORCOMPILE_UNTAG_TOKEN(PTR_TO_TADDR(module));
                pImport = GetDependencyForFixup(rva)->pImport;
            }
        }
        else
        {
            pImport = GetDependencyForModule(module)->pImport;
        }
        AppendTokenName(token, buf, pImport);
    }
    else if( ELEMENT_TYPE_FNPTR == td->GetInternalCorElementType() )
    {
        PTR_FnPtrTypeDesc fptd( PTR_TO_TADDR(td) );
        buf.Append( W("(fnptr)") );
    }
    else if(td->HasTypeParam())
    {
        PTR_ParamTypeDesc ptd(PTR_TO_TADDR(td));

        _ASSERTE(td->HasTypeParam());
        TypeHandle th(ptd->GetTypeParam());
        _ASSERTE( !CORCOMPILE_IS_POINTER_TAGGED(th.AsTAddr()) );
        _ASSERTE( th.AsTAddr() );
        TypeHandleToString(th, buf);

        AppendTypeQualifier( td->GetInternalCorElementType(), /*rank*/ 0, buf );
    }
    else
    {
        //generic typedesc?
        EnumFlagsToString( (int)td->GetInternalCorElementType(), s_CorElementType, dim(s_CorElementType),
                           W(""), buf );
    }
}
void NativeImageDumper::TypeHandleToString( TypeHandle th, SString& buf )
{
    TADDR arg = th.AsTAddr();
    /* REVISIT_TODO Thu 10/5/2006
     * Is this constant somewhere?
     */
    //0x2 is the subtle hint that this is a typedesc.  code:TypeHandle::AsTypeDesc
    if( arg & 0x2 )
    {
        PTR_TypeDesc argTD( arg & ~0x2 );
        TypeDescToString( argTD, buf );
    }
    else
    {
        PTR_MethodTable argMT( th.AsTAddr() );
        MethodTableToString( argMT, buf );
    }
}

void NativeImageDumper::DoWriteFieldTypeHandle( const char * name,
                                                unsigned offset,
                                                unsigned fieldSize,
                                                TypeHandle th )
{
    TempBuffer buf;
    TADDR ptr = th.AsTAddr();
    if( DoWriteFieldAsFixup(name, offset, fieldSize, th.AsTAddr() ) )
        return;
    else
    {
        TypeHandleToString(th, buf);

        buf.Append( W(" (from TypeHandle)") );
        /* REVISIT_TODO Fri 10/14/2005
         * Do a better job of this
         */
        if( offset == UINT_MAX )
        {
            m_display->WriteElementPointerAnnotated( name,
                                                     DataPtrToDisplay(ptr),
                                                     (const WCHAR*) buf );
        }
        else
        {
            m_display->WriteFieldPointerAnnotated( name, offset, fieldSize,
                                                   DataPtrToDisplay(ptr),
                                                   (const WCHAR*) buf );
        }
    }
}
void NativeImageDumper::WriteElementTypeHandle( const char * name,
                                                TypeHandle th )
{
    DoWriteFieldTypeHandle( name, UINT_MAX, UINT_MAX, th );
}

void NativeImageDumper::DoDumpFieldStub( PTR_Stub stub, unsigned offset,
                                         unsigned fieldSize, const char * name )
{
    _ASSERTE(CHECK_OPT(EECLASSES));
    if( stub == NULL )
    {
        m_display->WriteFieldPointer( name, offset, fieldSize, NULL );
    }
    else
    {
        m_display->StartStructureWithOffset( name, offset, fieldSize,
                                             DPtrToPreferredAddr(stub),
                                             sizeof(*stub) );
        /* REVISIT_TODO Fri 10/14/2005
         * Dump stub
         */
        m_display->EndStructure();
    }
}

#ifdef FEATURE_COMINTEROP
void NativeImageDumper::DoDumpComPlusCallInfo( PTR_ComPlusCallInfo compluscall )
{
    m_display->StartStructure( "ComPlusCallInfo",
                               DPtrToPreferredAddr(compluscall),
                               sizeof(*compluscall) );

    DisplayWriteFieldPointer( m_pILStub, compluscall->m_pILStub,
                   ComPlusCallInfo, ALWAYS);
    /* REVISIT_TODO Fri 12/16/2005
     * Coverage read stub?
     */
    WriteFieldMethodTable(m_pInterfaceMT,
                          compluscall->m_pInterfaceMT,
                          ComPlusCallInfo, ALWAYS);

    PTR_MethodDesc pEventProviderMD = PTR_MethodDesc((TADDR)compluscall->m_pEventProviderMD);
    WriteFieldMethodDesc(m_pEventProviderMD,
                         pEventProviderMD,
                         ComPlusCallInfo, ALWAYS);
    DisplayWriteFieldInt( m_cachedComSlot, compluscall->m_cachedComSlot,
                          ComPlusCallInfo, ALWAYS );

    /* REVISIT_TODO Fri 12/16/2005
     * Dump these as mnemonics
     */
    DisplayWriteFieldInt( m_flags, compluscall->m_flags,
                          ComPlusCallInfo, ALWAYS );
    WriteFieldMethodDesc( m_pStubMD,
                          compluscall->m_pStubMD.GetValueMaybeNull(PTR_HOST_MEMBER_TADDR(ComPlusCallInfo, compluscall, m_pStubMD)),
                          ComPlusCallInfo, ALWAYS );

#ifdef TARGET_X86
    DisplayWriteFieldInt( m_cbStackArgumentSize, compluscall->m_cbStackArgumentSize,
                          ComPlusCallInfo, ALWAYS );

    DisplayWriteFieldPointer( m_pRetThunk,
                              DataPtrToDisplay((TADDR)compluscall->m_pRetThunk),
                              ComPlusCallInfo, ALWAYS );
#endif
    m_display->EndStructure(); //ComPlusCallInfo
}
#endif // FEATURE_COMINTEROP

void NativeImageDumper::DoWriteFieldStr( PTR_BYTE ptr, const char * name,
                                         unsigned offset, unsigned fieldSize )
{
    if( ptr == NULL )
    {
        if( UINT_MAX == offset )
            m_display->WriteElementPointer( name, NULL );
        else
            m_display->WriteFieldPointer( name, offset, fieldSize, NULL );
    }
    else
    {
        /* REVISIT_TODO Wed 03/22/2006
         * Obviously this does the wrong thing for UTF-8.
         */
        TempBuffer buf;
        BYTE b;
        TADDR taddr = DPtrToPreferredAddr(ptr);
        PTR_BYTE current = ptr;
        /* REVISIT_TODO Mon 03/27/2006
         * Actually handle UTF-8 properly
         */
        while( (b = *current++) != 0 )
            buf.Append( (WCHAR)b );
        /* REVISIT_TODO Wed 03/22/2006
         * This seems way way way more verbose than it needs to be.
         */
        if( UINT_MAX == offset )
        {
            m_display->StartStructure( name, DataPtrToDisplay(taddr),
                                       current - ptr );
        }
        else
        {
            m_display->StartStructureWithOffset( name, offset, fieldSize,
                                                 DataPtrToDisplay(taddr),
                                                 current - ptr );
        }
        DisplayWriteElementStringW( "Value", (const WCHAR *)buf, ALWAYS );
        m_display->EndStructure();
        /*
        m_display->WriteFieldPointerAnnotated( name, offset, fieldSize,
                                               taddr, (const WCHAR *)buf );
                                               */
    }
}
void NativeImageDumper::WriteFieldDictionaryLayout(const char * name,
                                                   unsigned offset,
                                                   unsigned fieldSize,
                                                   PTR_DictionaryLayout layout,
                                                   IMetaDataImport2 * import)
{
    if( layout == NULL )
    {
        m_display->WriteFieldPointer(name, NULL, offset, fieldSize);
        return;
    }
    m_display->StartVStructureWithOffset( name, offset, fieldSize );
    DisplayStartArray( "DictionaryLayouts", NULL, ALWAYS );
    do
    {
        DisplayStartStructure( "DictionaryLayout", DPtrToPreferredAddr(layout),
                               sizeof(DictionaryLayout)
                               + sizeof(DictionaryEntryLayout)
                               * (layout->m_numSlots - 1), ALWAYS );


        DisplayWriteFieldPointer( m_pNext, DataPtrToDisplay((TADDR)layout->m_pNext),
                                  DictionaryLayout, ALWAYS );
        DisplayWriteFieldInt( m_numSlots, layout->m_numSlots,
                              DictionaryLayout, ALWAYS );
        DisplayStartArrayWithOffset( m_slots, NULL, DictionaryLayout, ALWAYS );
        for( unsigned i = 0; i < layout->m_numSlots; ++i )
        {
            PTR_DictionaryEntryLayout entry( PTR_HOST_MEMBER_TADDR(DictionaryLayout, layout, m_slots) + (i * sizeof(DictionaryEntryLayout)) );
            DisplayStartStructure( "DictionaryEntryLayout",
                                   DPtrToPreferredAddr(entry), sizeof(*entry),
                                   ALWAYS );
            const char * kind = NULL;
            switch( entry->GetKind() )
            {
#define KIND_ENTRY(x) case x : kind = # x ; break
                KIND_ENTRY(EmptySlot);
                KIND_ENTRY(TypeHandleSlot);
                KIND_ENTRY(MethodDescSlot);
                KIND_ENTRY(MethodEntrySlot);
                KIND_ENTRY(ConstrainedMethodEntrySlot);
                KIND_ENTRY(DispatchStubAddrSlot);
                KIND_ENTRY(FieldDescSlot);
#undef KIND_ENTRY
            default:
                _ASSERTE( !"unreachable" );
            }
            DisplayWriteElementString( "Kind", kind, ALWAYS );
            DisplayWriteElementPointer( "Signature", DPtrToPreferredAddr(entry->m_signature), ALWAYS );
            DisplayEndStructure( ALWAYS ); //DictionaryEntryLayout
        }
        DisplayEndArray( "Total Dictionary Entries",  ALWAYS ); //m_slots
        DisplayEndStructure( ALWAYS ); //Layout
        layout = PTR_DictionaryLayout(TO_TADDR(layout->m_pNext));
    }while( layout != NULL );
    DisplayEndArray( "Total Dictionary Layouts", ALWAYS ); //DictionaryLayouts


    DisplayEndVStructure( ALWAYS ); // name
}
void NativeImageDumper::DoWriteFieldFieldDesc( const char * name,
                                               unsigned offset,
                                               unsigned fieldSize,
                                               PTR_FieldDesc fd )
{
    if( fd == NULL )
    {
        m_display->WriteFieldPointer( name, offset, fieldSize, NULL );
    }
    else
    {
        TempBuffer buf;
        FieldDescToString( fd, buf );
        m_display->WriteFieldPointerAnnotated( name, offset, fieldSize,
                                               DPtrToPreferredAddr(fd),
                                               (const WCHAR*) buf );
    }

}
void NativeImageDumper::DoWriteFieldMethodDesc( const char * name,
                                                unsigned offset,
                                                unsigned fieldSize,
                                                PTR_MethodDesc md )
{
    if( md == NULL )
    {
        m_display->WriteFieldPointer( name, offset, fieldSize, NULL );
    }
    else if( DoWriteFieldAsFixup(name, offset, fieldSize, PTR_TO_TADDR(md)) )
    {
        return;
    }
    else
    {
        TempBuffer buf;
        MethodDescToString( md, buf );
        m_display->WriteFieldPointerAnnotated( name, offset, fieldSize,
                                               DPtrToPreferredAddr(md),
                                               (const WCHAR*) buf );
    }
}

void NativeImageDumper::EntryPointToString( TADDR pEntryPoint,
                                            SString& buf )
{
    const Dependency * dependency = GetDependencyForPointer(pEntryPoint);

    PTR_MethodDesc md;
    if (dependency->pModule->IsZappedPrecode(pEntryPoint))
    {
        md = dac_cast<PTR_MethodDesc>(Precode::GetPrecodeFromEntryPoint(pEntryPoint)->GetMethodDesc());
    }
    else
    {
        PTR_Module module = (TADDR)m_decoder.GetPersistedModuleImage();
        PTR_NGenLayoutInfo pNgenLayout = module->GetNGenLayoutInfo();
        DWORD rva = (DWORD)(pEntryPoint - PTR_TO_TADDR(m_decoder.GetBase()));

        for (int iRange = 0; iRange < 2; iRange++)
        {
            if (pNgenLayout->m_CodeSections[iRange].IsInRange(pEntryPoint))
            {
                int MethodIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(rva, pNgenLayout->m_pRuntimeFunctions[iRange], 0, pNgenLayout->m_nRuntimeFunctions[iRange] - 1);
                if (MethodIndex >= 0)
                {
#ifdef FEATURE_EH_FUNCLETS
                    while (pNgenLayout->m_MethodDescs[iRange][MethodIndex] == 0)
                        MethodIndex--;
#endif

                    PTR_RUNTIME_FUNCTION pRuntimeFunction = pNgenLayout->m_pRuntimeFunctions[iRange] + MethodIndex;

                    md = NativeUnwindInfoLookupTable::GetMethodDesc(pNgenLayout, pRuntimeFunction, PTR_TO_TADDR(m_decoder.GetBase()));
                    break;
                }
            }
        }
    }

    MethodDescToString(md, buf);
}

void NativeImageDumper::MethodDescToString( PTR_MethodDesc md,
                                            SString& buf )
{
    if( md == NULL )
        buf.Append( W("mdMethodDefNil") );
    else if( md->IsILStub() )
        buf.AppendUTF8(md->AsDynamicMethodDesc()->GetName());
    else
    {
        //write the name to a temporary location, since I'm going to insert it
        //into the middle of a signature.
        TempBuffer tempName;

        _ASSERTE(!CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(md)));
        //work back to the EEClass.  That gives us the context for the token.
        PTR_MethodDescChunk chunk(md->GetMethodDescChunk());
        //chunk is implicitly remapped because it's calculated from the pointer
        //to MD.
        PTR_MethodTable mt = chunk->GetMethodTable();
        const Dependency * dependency;
        if( CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(mt)) )
        {
            //This code will not work for out of module dependencies.
            _ASSERTE(isSelf(GetDependencyForPointer(PTR_TO_TADDR(md))));

            RVA rva = CORCOMPILE_UNTAG_TOKEN(PTR_TO_TADDR(mt));
            dependency = GetDependencyForFixup(rva);
            mt = NULL; //make sure we don't use this for anything.
        }
        else
            dependency = GetDependencyFromMT(mt);

        _ASSERTE(dependency);


        /* REVISIT_TODO Fri 10/13/2006
         * Don't I need the array type name here?
         */
        _ASSERTE(dependency->pImport);
        if( md->GetClassification() == mcArray )
        {

            //We don't need to append the dependency all the time.
            //MethodTableToString() already appends it to the MethodTable.
            //Only do it in cases where we don't call MethodTableToString.
            if( !isSelf(dependency) )
            {
                AppendTokenName( dependency->entry->dwAssemblyRef, tempName,
                                 m_manifestImport );
                tempName.Append(W("!"));
            }

            _ASSERTE(PTR_TO_TADDR(mt));
            MethodTableToString( mt, tempName );
            tempName.Append( W("::") );

            //there are four hard coded names for array method descs, use these
            //instead of the token.
            PTR_ArrayMethodDesc amd(PTR_TO_TADDR(md));
            tempName.AppendUTF8( amd->GetMethodName() );
        }
        else
        {
            //if we have a MethodTable, use that and compose the name
            //ourselves.  That way we can get generic arguments.
            if( mt )
            {
                ULONG size;
                MethodTableToString( mt, tempName );
                tempName.Append( W("::") );
                IfFailThrow(dependency->pImport->GetMethodProps(md->GetMemberDef(), NULL, bigBuffer,
                                                                bigBufferSize, &size, NULL, NULL, NULL, NULL,
                                                                NULL));
                tempName.Append(bigBuffer);
            }
            else
            {
                //We don't need to append the dependency all the time.
                //MethodTableToString() already appends it to the MethodTable.
                //Only do it in cases where we don't call MethodTableToString.
                if( !isSelf(dependency) )
                {
                    AppendTokenName( dependency->entry->dwAssemblyRef, tempName,
                                     m_manifestImport );
                    tempName.Append(W("!"));
                }
                AppendTokenName( md->GetMemberDef(), tempName, dependency->pImport );
            }

            if( mcInstantiated == md->GetClassification() )
            {
                PTR_InstantiatedMethodDesc imd(PTR_TO_TADDR(md));
                unsigned numArgs = imd->m_wNumGenericArgs;
                PTR_Dictionary dict(imd->IMD_GetMethodDictionary());
                if( dict != NULL )
                {
                    DictionaryToArgString( dict, numArgs, tempName );
                }
            }

            PCCOR_SIGNATURE pvSigBlob;
            ULONG cbSigBlob;
            IfFailThrow(dependency->pImport->GetMethodProps(md->GetMemberDef(),
                                                            NULL,
                                                            NULL,
                                                            0,
                                                            NULL,
                                                            NULL,
                                                            &pvSigBlob,
                                                            &cbSigBlob,
                                                            NULL,
                                                            NULL));


            CQuickBytes prettySig;
            ReleaseHolder<IMDInternalImport> pInternal;
            IfFailThrow(GetMDInternalInterfaceFromPublic(dependency->pImport, IID_IMDInternalImport,
                                                               (void**)&pInternal));
            StackScratchBuffer buffer;
            const ANSI * ansi = tempName.GetANSI(buffer);
            ansi = PrettyPrintSig(pvSigBlob, cbSigBlob, ansi, &prettySig, pInternal, NULL);
            tempName.SetANSI( ansi );
        }
        buf.Append(tempName);
    }
}
void NativeImageDumper::WriteElementMethodDesc( const char * name,
                                                PTR_MethodDesc md )
{
    if( md == NULL )
    {
        m_display->WriteElementPointer( name, NULL );
    }
    else
    {
        TempBuffer buf;
        MethodDescToString( md, buf );
        m_display->WriteElementPointerAnnotated( name, DPtrToPreferredAddr(md),
                                                 (const WCHAR*) buf );
    }
}
void NativeImageDumper::FieldDescToString( PTR_FieldDesc fd, SString& buf )
{
    FieldDescToString( fd, mdFieldDefNil, buf );
}
void NativeImageDumper::FieldDescToString( PTR_FieldDesc fd, mdFieldDef tok,
                                           SString& buf )
{
    IF_OPT(DISABLE_NAMES)
    {
        buf.Append( W("Disabled") );
        return;
    }
    if( fd == NULL )
    {
        if( tok == mdFieldDefNil )
            buf.Append( W("mdFieldDefNil") );
        else
            AppendTokenName( tok, buf );
    }
    else
    {
        _ASSERTE(!CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(fd)));
        IMetaDataImport2 * importMD = NULL;
        if( !isInRange(PTR_TO_TADDR(fd)) )
        {
            const Dependency * dependency = GetDependencyFromFD(fd);
            _ASSERTE(dependency);
            AppendTokenName( dependency->entry->dwAssemblyRef, buf,
                             m_manifestImport );
            buf.Append(W("!"));
            importMD = dependency->pImport;
            _ASSERTE(importMD);

        }
        else
        {
            importMD = m_import;
        }
        AppendTokenName( fd->GetMemberDef(), buf, importMD );
    }
}

void NativeImageDumper::DoWriteFieldAsHex( const char * name, unsigned offset,
                                           unsigned fieldSize, PTR_BYTE ptr,
                                           unsigned dataLen )
{
    TempBuffer buffer;
    for( unsigned i = 0; i < dataLen; ++i )
    {
        unsigned char b = ptr[i];
        buffer.AppendPrintf( W("%02x%02x"), (b & 0xf0) >> 4, b & 0xf );
    }
    if( offset == UINT_MAX )
    {
        m_display->WriteElementStringW( name, (const WCHAR *)buffer );
    }
    else
    {
        m_display->WriteFieldStringW( name, offset, fieldSize,
                                      (const WCHAR *)buffer );
    }
}
void NativeImageDumper::WriteElementMDToken( const char * name, mdToken token )
{
    DoWriteFieldMDToken( name, UINT_MAX, UINT_MAX, token );
}
void NativeImageDumper::DoWriteFieldMDToken( const char * name, unsigned offset,
                                             unsigned fieldSize, mdToken token,
                                             IMetaDataImport2 * import )
{
    TempBuffer buf;
    if( RidFromToken(token) == mdTokenNil )
    {
        AppendNilToken( token, buf );
    }
    else
    {
        AppendToken( token, buf, import );
    }
    if( UINT_MAX == offset )
        m_display->WriteElementEnumerated( name, token, (const WCHAR *)buf );
    else
    {
        m_display->WriteFieldEnumerated(name, offset, fieldSize, token,
                                        (const WCHAR*)buf);
    }
}

void NativeImageDumper::WriteElementMethodTable( const char * name,
                                                 PTR_MethodTable mt )
{
    DoWriteFieldMethodTable( name, UINT_MAX, UINT_MAX, mt );
}
void NativeImageDumper::DoWriteFieldMethodTable( const char * name,
                                                 unsigned offset,
                                                 unsigned fieldSize,
                                                 PTR_MethodTable mt )
{
    if( mt == NULL )
    {
        if( UINT_MAX == offset )
            m_display->WriteElementPointer( name, NULL );
        else
            m_display->WriteFieldPointer( name, offset, fieldSize, NULL );
    }
    else if( DoWriteFieldAsFixup( name, offset, fieldSize, PTR_TO_TADDR(mt) ) )
    {
        return;
    }
    else
    {
        TempBuffer buf;
        MethodTableToString( mt, buf );
        if( UINT_MAX == offset )
        {

            m_display->WriteElementPointerAnnotated( name,
                                                     DPtrToPreferredAddr(mt),
                                                     (const WCHAR*) buf );
        }
        else
        {
            m_display->WriteFieldPointerAnnotated( name, offset, fieldSize,
                                                   DPtrToPreferredAddr(mt),
                                                   (const WCHAR*) buf );
        }
    }
}

const char * s_VTSCallbackNames[] =
{
#define VTSCB_ENTRY(x) # x
    VTSCB_ENTRY(VTS_CALLBACK_ON_SERIALIZING),
    VTSCB_ENTRY(VTS_CALLBACK_ON_SERIALIZED),
    VTSCB_ENTRY(VTS_CALLBACK_ON_DESERIALIZING),
    VTSCB_ENTRY(VTS_CALLBACK_ON_DESERIALIZED),
#undef VTSCB_ENTRY
};
void NativeImageDumper::DumpFieldDesc( PTR_FieldDesc fd, const char * name )
{
    DisplayStartStructure( name, DPtrToPreferredAddr(fd), sizeof(*fd),
                           ALWAYS );
    WriteFieldMethodTable( m_pMTOfEnclosingClass,
                           fd->GetApproxEnclosingMethodTable(), FieldDesc, ALWAYS );
    m_display->WriteFieldUInt( "m_mb", offsetof(FieldDesc, m_dword1),
                               fieldsize(FieldDesc, m_dword1),
                               fd->GetMemberDef() );
    m_display->WriteFieldFlag( "m_isStatic",
                               offsetof(FieldDesc, m_dword1),
                               fieldsize(FieldDesc, m_dword1),
                               fd->m_isStatic );
    m_display->WriteFieldFlag( "m_isThreadLocal",
                               offsetof(FieldDesc, m_dword1),
                               fieldsize(FieldDesc, m_dword1),
                               fd->m_isThreadLocal );
    m_display->WriteFieldFlag( "m_isRVA", offsetof(FieldDesc, m_dword1),
                               fieldsize(FieldDesc, m_dword1),
                               fd->m_isRVA );

    {
        TempBuffer buf;
        EnumFlagsToString( fd->m_prot, s_CorFieldAttr,
                           _countof(s_CorFieldAttr), W(" "), buf );
        m_display->WriteFieldEnumerated( "m_prot",
                                         offsetof(FieldDesc, m_dword1),
                                         fieldsize(FieldDesc, m_dword1),
                                         fd->m_prot, (const WCHAR *)buf );
    }
    m_display->WriteFieldFlag( "m_requiresFullMbValue",
                               offsetof(FieldDesc, m_dword1),
                               fieldsize(FieldDesc, m_dword1),
                               fd->m_requiresFullMbValue );
    m_display->WriteFieldInt( "m_dwOffset",
                              offsetof(FieldDesc, m_dword2),
                              fieldsize(FieldDesc, m_dword2),
                              fd->m_dwOffset );
    DoWriteFieldCorElementType( "m_type",
                                offsetof(FieldDesc, m_dword2),
                                fieldsize(FieldDesc, m_dword2),
                                (CorElementType)fd->m_type );
#ifdef _DEBUG
    WriteFieldStr( m_debugName, PTR_BYTE(TO_TADDR(fd->m_debugName)),
                   FieldDesc, ALWAYS );
#endif
    DisplayEndStructure( ALWAYS ); //name
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void
NativeImageDumper::DumpMethodTable( PTR_MethodTable mt, const char * name,
                                    PTR_Module module )
{
    _ASSERTE(NULL != mt);
    TADDR start, end;
    bool haveCompleteExtents = true;
    PTR_EEClass clazz = NULL;
    if( !mt->IsCanonicalMethodTable() && CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(mt->GetCanonicalMethodTable())) )
    {
        /* REVISIT_TODO Wed 02/01/2006
         * GetExtent requires the class in order to compute GetInstAndDictSize.
         * If the EEClass isn't present, I cannot compute the size.  If we are
         * in this case, skip all of the generic dictionaries.
         */
        haveCompleteExtents = false;
        TempBuffer buf;
        MethodTableToString( mt, buf );
        m_display->ErrorPrintF( "WARNING! MethodTable %S is generic but is not hard bound to its EEClass.  Cannot compute generic dictionary sizes.\n", (const WCHAR *)buf );
    }
    else if( !m_isCoreLibHardBound )
    {
        /* REVISIT_TODO Mon 8/20/2007
         * If we're not hard bound to CoreLib, most things don't work.  They depend on knowing what
         * g_pObjectClass is.  Without the hard binding to CoreLib, I can't figure that out.
         */
        haveCompleteExtents = false;
    }
    if( haveCompleteExtents )
    {
        mt->GetSavedExtent(&start, &end);
        clazz = mt->GetClass();
    }
    else
    {
        start = PTR_TO_TADDR(mt);
        end = start + sizeof(*mt);
    }
    IF_OPT(METHODTABLES)
    {
        m_display->StartStructureWithNegSpace( name, DPtrToPreferredAddr(mt),
                                               DataPtrToDisplay(start), end - start );
    }

    IF_OPT(METHODTABLES)
    {
        {
            TempBuffer buf;
            MethodTableToString( mt, buf );
            DisplayWriteElementStringW( "Name", (const WCHAR *)buf, ALWAYS );
        }
        if( mt->ContainsPointers() )
        {
            PTR_CGCDesc cgc = CGCDesc::GetCGCDescFromMT(mt);
            unsigned size = (unsigned)cgc->GetSize();
            /* REVISIT_TODO Tue 12/13/2005
             * Does anyone actually care about what's inside here?
             */
            m_display->WriteFieldEmpty( "CGCDesc", ~size + 1, size );
        }
    }

    /* XXX Mon 10/24/2005
     * The MT might have a component size as the low WORD of the m_dwFlags
     * field, if it doesn't then that field instead represents a number of
     * flags, which we know as the "low flags"
     */
    if (mt->HasComponentSize())
    {
        DisplayWriteElementInt( "ComponentSize", mt->RawGetComponentSize(),
                                METHODTABLES );
    }
    else
    {
        DisplayWriteFieldEnumerated( m_dwFlags, mt->m_dwFlags & 0xFFFF, MethodTable,
                                     s_MTFlagsLow, W(", "), METHODTABLES );
    }

    /* XXX Fri 10/07/2005
     * The low WORD of the flags is used for either a component size or flags
     * (see above), the high WORD is always flags. If this changes then this
     * might be busted.
     */
    DisplayWriteFieldEnumerated( m_dwFlags, mt->m_dwFlags & ~0xFFFF, MethodTable,
                                 s_MTFlagsHigh, W(", "), METHODTABLES );

    DisplayWriteFieldInt( m_BaseSize, mt->m_BaseSize, MethodTable,
                          METHODTABLES );

    DisplayWriteFieldEnumerated( m_wFlags2, mt->m_wFlags2, MethodTable,
                                 s_MTFlags2, W(", "), METHODTABLES );

    DisplayWriteFieldInt( m_wToken, mt->m_wToken, MethodTable,
                          METHODTABLES );
    DisplayWriteFieldInt( m_wNumVirtuals, mt->m_wNumVirtuals, MethodTable,
                          METHODTABLES );
    DisplayWriteFieldInt( m_wNumInterfaces, mt->m_wNumInterfaces, MethodTable,
                          METHODTABLES );



    PTR_MethodTable parent = ReadPointerMaybeNull((MethodTable*) mt, &MethodTable::m_pParentMethodTable, mt->GetFlagHasIndirectParent());
    if( parent == NULL )
    {
        DisplayWriteFieldPointer( m_pParentMethodTable, NULL, MethodTable,
                                  METHODTABLES );
    }
    else
    {
        IF_OPT(METHODTABLES)
        {
            DoWriteFieldMethodTable( "m_pParentMethodTable",
                                     offsetof(MethodTable, m_pParentMethodTable),
                                     fieldsize(MethodTable, m_pParentMethodTable),
                                     mt->GetParentMethodTable() );
        }
    }
    DisplayWriteFieldPointer( m_pLoaderModule,
                              DPtrToPreferredAddr(mt->GetLoaderModule()),
                              MethodTable, METHODTABLES );

    PTR_MethodTableWriteableData wd = ReadPointer((MethodTable *)mt, &MethodTable::m_pWriteableData);
    _ASSERTE(wd != NULL);
    DisplayStartStructureWithOffset( m_pWriteableData, DPtrToPreferredAddr(wd),
                                     sizeof(*wd), MethodTable, METHODTABLES );
    DisplayWriteFieldEnumerated( m_dwFlags, wd->m_dwFlags,
                                 MethodTableWriteableData, s_WriteableMTFlags,
                                 W(", "), METHODTABLES );
    DisplayWriteFieldPointer( m_hExposedClassObject,
                              DataPtrToDisplay(wd->m_hExposedClassObject),
                              MethodTableWriteableData, METHODTABLES );
    _ASSERTE(wd->m_hExposedClassObject == 0);
    DisplayEndStructure( METHODTABLES ); //m_pWriteableData

    if( !mt->IsCanonicalMethodTable() )
    {
        WriteFieldMethodTable( m_pCanonMT, mt->GetCanonicalMethodTable(),
                               MethodTable, METHODTABLES );
    }
    else
    {
        DisplayWriteFieldPointer( m_pEEClass, DPtrToPreferredAddr(mt->GetClass()),
                                  MethodTable, METHODTABLES );
    }

    if( mt->IsArray() )
    {
        WriteFieldTypeHandle( m_ElementTypeHnd,
                              mt->GetArrayElementTypeHandle(),
                              MethodTable, METHODTABLES );
    }

    if( mt->HasPerInstInfo() && haveCompleteExtents )
    {
        //print out the generics dictionary info, and then print out
        //the contents of those dictionaries.
        PTR_GenericsDictInfo di = mt->GetGenericsDictInfo();
        _ASSERTE(NULL != di);

        DisplayStartStructure("GenericsDictInfo", DPtrToPreferredAddr(di), sizeof(*di), METHODTABLES);

        DisplayWriteFieldInt( m_wNumDicts, di->m_wNumDicts, GenericsDictInfo,
                                METHODTABLES );
        DisplayWriteFieldInt( m_wNumTyPars, di->m_wNumTyPars,
                                GenericsDictInfo, METHODTABLES);
        DisplayEndStructure( METHODTABLES ); //GenericsDictInfo

        DPTR(MethodTable::PerInstInfoElem_t) perInstInfo = mt->GetPerInstInfo();

        DisplayStartStructure( "PerInstInfo",
                               DPtrToPreferredAddr(perInstInfo),
                               mt->GetPerInstInfoSize(),
                               METHODTABLES );
        /* XXX Tue 10/11/2005
         * Only dump this type's dictionary, rather than the inherited
         * dictionaries. (there are multiple entries in m_pPerInstInfo, but
         * only print the last one, which is the one for this class).
         * cloned from Genericdict.cpp
         */
        PTR_Dictionary currentDictionary(mt->GetDictionary());
        if( currentDictionary != NULL )
        {
            PTR_DictionaryEntry entry(currentDictionary->EntryAddr(0));

            PTR_DictionaryLayout layout( clazz->GetDictionaryLayout() );

            DisplayStartStructure( "Dictionary",
                                   DPtrToPreferredAddr(currentDictionary),
                                   //if there is a layout, use it to compute
                                   //the size, otherwise there is just the one
                                   //entry.
                                   DictionaryLayout::GetDictionarySizeFromLayout(mt->GetNumGenericArgs(), layout),
                                   METHODTABLES );

            DisplayStartArrayWithOffset( m_pEntries, NULL, Dictionary,
                                         METHODTABLES );

            /* REVISIT_TODO Thu 12/15/2005
             * use VERBOSE_TYPES here.
             */
            _ASSERTE(CHECK_OPT(METHODTABLES));

            //for each generic arg, there is a type handle slot
            for( unsigned i = 0; i < mt->GetNumGenericArgs(); ++i )
                DumpDictionaryEntry("Entry", TypeHandleSlot, entry + i);

            //now check for a layout.  If it is present, then there are more
            //entries.
            if( layout != NULL && (layout->GetNumUsedSlots() > 0) )
            {
                unsigned numUsedSlots = layout->GetNumUsedSlots();
                for( unsigned i = 0; i < numUsedSlots; ++i )
                {
                    //DictionaryLayout::GetEntryLayout
                    PTR_DictionaryEntryLayout entryLayout(layout->GetEntryLayout(i));

                    //Dictionary::GetSlotAddr
                    PTR_DictionaryEntry ent(currentDictionary->EntryAddr(mt->GetNumGenericArgs() + i));

                    DumpDictionaryEntry( "Entry", entryLayout->GetKind(), ent );
                }
            }
            if( layout != NULL )
            {
                /* REVISIT_TODO Thu 12/15/2005
                 * Where is this data?
                 */
            }
            DisplayEndArray( "Total Per instance Info",
                             METHODTABLES ); //m_pEntries
            DisplayEndStructure( METHODTABLES ); //Dictionary
        }
        DisplayEndStructure( METHODTABLES ); //m_pPerInstInfo
    }

#ifdef _DEBUG
    WriteFieldStr( debug_m_szClassName,
                   PTR_BYTE(TO_TADDR(mt->debug_m_szClassName)), MethodTable,
                   METHODTABLES );
#if 0 //already dumping the optional member
    PTR_InterfaceInfo imap( TO_TADDR(mt->m_pIMapDEBUG) );
    /* REVISIT_TODO Mon 10/24/2005
     * Dump interface map
     */
    DisplayStartArrayWithOffset( m_pIMapDEBUG, NULL, MethodTable,
                                 METHODTABLES );
    DisplayEndArray( "Total Interfaces", METHODTABLES );
#endif
#endif

    if( mt->HasDispatchMapSlot() )
    {
        PTR_DispatchMap dispatchMap(mt->GetDispatchMap());

        DisplayStartStructure( "DispatchMap",
                               DPtrToPreferredAddr(dispatchMap),
                               DispatchMap::GetObjectSize(dispatchMap->GetMapSize()),
                               METHODTABLES );

        IF_OPT(VERBOSE_TYPES )
        {
            DispatchMap::Iterator iter(mt);
            DisplayStartArray( "DispatchMap", NULL, VERBOSE_TYPES );
            while( iter.Next() )
            {
                DispatchMapEntry * ent = iter.Entry();

                DisplayStartElement( "Entry", METHODTABLES );
                DisplayStartVStructure( "TypeID", METHODTABLES );
                DispatchMapTypeID typeID = ent->GetTypeID();
                if( typeID.IsThisClass() )
                    DisplayWriteElementFlag("IsThisClass", true, METHODTABLES );
                else if( typeID.IsImplementedInterface() )
                {
                    DisplayWriteElementFlag( "IsImplementedInterface",
                                             true, METHODTABLES );
                    DisplayWriteElementInt( "GetInterfaceNum",
                                            typeID.GetInterfaceNum(), METHODTABLES );
                }
                DisplayEndStructure( METHODTABLES ); //TypeID
                m_display->WriteElementInt( "SlotNumber",
                                            ent->GetSlotNumber() );
                DisplayWriteElementInt( "TargetSlotNumber",
                                        ent->GetSlotNumber(), METHODTABLES );

                m_display->EndElement(); //Entry
            }
            //DispatchMap
            DisplayEndArray("Total Dispatch Map Entries", METHODTABLES );
        }
        else
        {
            CoverageRead(PTR_TO_TADDR(dispatchMap),
                         DispatchMap::GetObjectSize(dispatchMap->GetMapSize()));
        }

        DisplayEndStructure( METHODTABLES ); //DispatchMap
    }

    IF_OPT( METHODTABLES )
    {
        m_display->StartStructureWithOffset("Vtable",
                                            mt->GetVtableOffset(),
                                            mt->GetNumVtableIndirections() * sizeof(MethodTable::VTableIndir_t),
                                            DataPtrToDisplay(PTR_TO_TADDR(mt) + mt->GetVtableOffset()),
                                            mt->GetNumVtableIndirections() * sizeof(MethodTable::VTableIndir_t));


        MethodTable::VtableIndirectionSlotIterator itIndirect = mt->IterateVtableIndirectionSlots();
        while (itIndirect.Next())
        {
            SlotChunk sc;
            sc.addr = dac_cast<TADDR>(itIndirect.GetIndirectionSlot());
            sc.nSlots = (WORD)itIndirect.GetNumSlots();
            sc.isRelative = MethodTable::VTableIndir2_t::isRelative;
            m_discoveredSlotChunks.AppendEx(sc);
        }

        IF_OPT(VERBOSE_TYPES)
        {
            DisplayStartList( W("[%-4s]: %s (%s)"), ALWAYS );
            for( unsigned i = 0; i < mt->GetNumVtableIndirections(); ++i )
            {
                DisplayStartElement( "Slot", ALWAYS );
                DisplayWriteElementInt( "Index", i, ALWAYS );
                TADDR base = dac_cast<TADDR>(&(mt->GetVtableIndirections()[i]));
                DPTR(MethodTable::VTableIndir2_t) tgt = MethodTable::VTableIndir_t::GetValueMaybeNullAtPtr(base);
                DisplayWriteElementPointer( "Pointer",
                                            DataPtrToDisplay(dac_cast<TADDR>(tgt)),
                                            ALWAYS );
                DisplayWriteElementString( "Type", "chunk indirection",
                                           ALWAYS );
                DisplayEndElement( ALWAYS ); //Slot
            }

            if (mt->HasNonVirtualSlotsArray())
            {
                DisplayStartElement( "Slot", ALWAYS );
                DisplayWriteElementInt( "Index", -1, ALWAYS );
                PTR_PCODE tgt = mt->GetNonVirtualSlotsArray();
                DisplayWriteElementPointer( "Pointer",
                                            DataPtrToDisplay(dac_cast<TADDR>(tgt)),
                                            ALWAYS );
                DisplayWriteElementString( "Type", "non-virtual chunk indirection",
                                           ALWAYS );
                DisplayEndElement( ALWAYS ); //Slot

                SlotChunk sc;
                sc.addr = dac_cast<TADDR>(tgt);
                sc.nSlots = (mt->GetNumVtableSlots() - mt->GetNumVirtuals());
                sc.isRelative = false;
                m_discoveredSlotChunks.AppendEx(sc);
            }
            else if (mt->HasSingleNonVirtualSlot())
            {
                DumpSlot((unsigned)-1, mt->GetSlot(mt->GetNumVirtuals()));
            }

            DisplayEndList( ALWAYS ); //vtable
        }
        else
        {
            CoverageRead( PTR_TO_TADDR(mt) + mt->GetVtableOffset(),
                          mt->GetNumVtableIndirections() * sizeof(MethodTable::VTableIndir_t) );

            if (mt->HasNonVirtualSlotsArray())
            {
                CoverageRead( PTR_TO_TADDR(mt->GetNonVirtualSlotsArray()),
                              mt->GetNonVirtualSlotsArraySize() );
            }

        }
        DisplayEndStructure(ALWAYS); //Vtable
    }

    if( mt->HasInterfaceMap() && CHECK_OPT(METHODTABLES) )
    {
        PTR_InterfaceInfo ifMap = mt->GetInterfaceMap();
        m_display->StartArrayWithOffset( "InterfaceMap",
                                         offsetof(MethodTable, m_pInterfaceMap),
                                         sizeof(void*),
                                         NULL );
        for( unsigned i = 0; i < mt->GetNumInterfaces(); ++i )
        {
            PTR_InterfaceInfo info = ifMap + i;
            DisplayStartStructure( "InterfaceInfo_t", DPtrToPreferredAddr(info),
                                   sizeof(*info), METHODTABLES );
            WriteFieldMethodTable( m_pMethodTable,
                                   info->GetMethodTable(),
                                   InterfaceInfo_t, METHODTABLES );
            DisplayEndStructure( METHODTABLES ); //InterfaceInfo_t
        }
        DisplayEndArray( "Total InterfaceInfos",
                         METHODTABLES ); //InterfaceMap
    }

    //rest of the optional members

    //GenericStatics comes after the generic dictionaries.  So if I
    //don't have extents, I can't print them.
    if( haveCompleteExtents &&
        mt->HasGenericsStaticsInfo() &&
        CHECK_OPT(METHODTABLES)
        )
    {
        PTR_GenericsStaticsInfo genStatics = mt->GetGenericsStaticsInfo();
        m_display->StartStructureWithOffset( "OptionalMember_"
                                                "GenericsStaticsInfo",
                                             mt->GetOffsetOfOptionalMember(MethodTable::OptionalMember_GenericsStaticsInfo),
                                             sizeof(*genStatics),
                                             DPtrToPreferredAddr(genStatics),
                                             sizeof(*genStatics) );

        PTR_FieldDesc fieldDescs = ReadPointerMaybeNull((GenericsStaticsInfo *) genStatics, &GenericsStaticsInfo::m_pFieldDescs);
        if( fieldDescs == NULL )
        {
            DisplayWriteFieldPointer( m_pFieldDescs, NULL, GenericsStaticsInfo,
                                      ALWAYS );
        }
        else
        {
            DisplayStartArrayWithOffset( m_pFieldDescs, NULL,
                                         GenericsStaticsInfo, ALWAYS );
            _ASSERTE(clazz == GetClassFromMT(mt));
            for( int i = 0; i < clazz->GetNumStaticFields(); ++i )
            {
                PTR_FieldDesc fd = fieldDescs + i;
                DumpFieldDesc( fd, "FieldDesc" );
            }
            DisplayEndArray( "Total Static Fields", ALWAYS ); // m_pFieldDescs
        }
        DisplayWriteFieldUInt( m_DynamicTypeID, (DWORD)genStatics->m_DynamicTypeID,
                               GenericsStaticsInfo, METHODTABLES );

        DisplayEndStructure( METHODTABLES );//OptionalMember_GenericsStaticsInfo

    }

    DisplayEndStructure( METHODTABLES ); //MethodTable
} // NativeImageDumper::DumpMethodTable
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void
NativeImageDumper::DumpMethodTableSlotChunk( TADDR slotChunk, COUNT_T numSlots, bool isRelative )
{
    IF_OPT( METHODTABLES )
    {
        COUNT_T slotsSize;
        if (isRelative)
        {
            slotsSize = numSlots * sizeof(RelativePointer<PCODE>);
        }
        else
        {
            slotsSize = numSlots * sizeof(PCODE);
        }
        DisplayStartStructure( "MethodTableSlotChunk", DataPtrToDisplay(slotChunk), slotsSize, METHODTABLES );

        IF_OPT(VERBOSE_TYPES)
        {
            DisplayStartList( W("[%-4s]: %s (%s)"), ALWAYS );
            for( unsigned i = 0; i < numSlots; ++i )
            {
                PCODE target;
                if (isRelative)
                {
                    target = RelativePointer<PCODE>::GetValueMaybeNullAtPtr(slotChunk + i * sizeof(RelativePointer<PCODE>));
                }
                else
                {
                    target = dac_cast<PTR_PCODE>(slotChunk)[i];
                }

                DumpSlot(i, target);
            }
            DisplayEndList( ALWAYS ); //Slot list
        }
        else
            CoverageRead( slotChunk, slotsSize );
        DisplayEndStructure(ALWAYS); //Slot chunk
    }
}


void
NativeImageDumper::DumpSlot( unsigned index, PCODE tgt )
{
    IF_OPT(VERBOSE_TYPES)
    {
        DisplayStartElement( "Slot", ALWAYS );
        DisplayWriteElementInt( "Index", index, ALWAYS );
        DisplayWriteElementPointer( "Pointer",
                                    DataPtrToDisplay(tgt),
                                    ALWAYS );
        if( !isInRange(TO_TADDR(tgt)) )
        {
            DisplayWriteElementString( "Type", "external",
                                       ALWAYS );
        }
        else if( isPrecode(TO_TADDR(tgt))
                           && Precode::IsValidType(PTR_Precode(TO_TADDR(tgt))->GetType()) )
        {
            PTR_Precode precode(TO_TADDR(tgt));
            DisplayWriteElementString( "Type", "precode",
                                       ALWAYS );
            //DumpPrecode( precode, module );
        }
        else
        {
            DisplayWriteElementString( "Type", "code pointer",
                                       ALWAYS );
        }
        DisplayEndElement( ALWAYS ); //Slot
    }
}

NativeImageDumper::EnumMnemonics NativeImageDumper::s_SSMDExtendedFlags[] =
{
#define SSMD_ENTRY(x) NativeImageDumper::EnumMnemonics( x, W(#x) )

#define SSMD_ACCESS_ENTRY(x) NativeImageDumper::EnumMnemonics( x, mdMemberAccessMask, W(#x) )
    SSMD_ACCESS_ENTRY(mdPrivateScope),
    SSMD_ACCESS_ENTRY(mdPrivate),
    SSMD_ACCESS_ENTRY(mdFamANDAssem),
    SSMD_ACCESS_ENTRY(mdAssem),
    SSMD_ACCESS_ENTRY(mdFamily),
    SSMD_ACCESS_ENTRY(mdFamORAssem),
    SSMD_ACCESS_ENTRY(mdPublic),
#undef SSMD_ACCESS_ENTRY

    SSMD_ENTRY(mdStatic),
    SSMD_ENTRY(mdFinal),
    SSMD_ENTRY(mdVirtual),
    SSMD_ENTRY(mdHideBySig),

    SSMD_ENTRY(mdVtableLayoutMask),
    SSMD_ENTRY(mdNewSlot),

    SSMD_ENTRY(mdCheckAccessOnOverride),
    SSMD_ENTRY(mdAbstract),
    SSMD_ENTRY(mdSpecialName),

    SSMD_ENTRY(mdPinvokeImpl),
    SSMD_ENTRY(mdUnmanagedExport),

    SSMD_ENTRY(mdRTSpecialName),
    SSMD_ENTRY(mdHasSecurity),
    SSMD_ENTRY(mdRequireSecObject),

    NativeImageDumper::EnumMnemonics( DynamicMethodDesc::nomdILStub,
                                      W("nomdILStub") ),
    NativeImageDumper::EnumMnemonics( DynamicMethodDesc::nomdLCGMethod,
                                      W("nomdLCGMethod") ),
#undef SSMD_ENTRY
};

//maps MethodClassification to a name for a MethodDesc
const char * const s_MDTypeName[] =
{
    "MethodDesc", //mcIL
    "FCallMethodDesc", //mcFCall
    "NDirectMethodDesc", //mcNDirect
    "EEImplMethodDesc", //mcEEImpl - //public StoredSigMethodDesc
    "ArrayMethodDesc", //mcArray - //public StoredSigMethodDesc
    "InstantiatedMethodDesc", //mcInstantiated
#if defined(FEATURE_COMINTEROP)
    "ComPlusCallMethodDesc", //mcComInterop
#else
    "",
#endif
    "DynamicMethodDesc", //mcDynamic -- //public StoredSigMethodDesc
};

unsigned s_MDSizes[] =
{
    sizeof(MethodDesc),                 //mcIL
    sizeof(FCallMethodDesc),            //mcFCall
    sizeof(NDirectMethodDesc),          //mcNDirect
    sizeof(EEImplMethodDesc),           //mcEEImpl
    sizeof(ArrayMethodDesc),            //mcArray
    sizeof(InstantiatedMethodDesc),     //mcInstantiated
#if defined(FEATURE_COMINTEROP)
    sizeof(ComPlusCallMethodDesc),      //mcComInterop
#else
    0,
#endif
    sizeof(DynamicMethodDesc),          //mcDynamic
};

static NativeImageDumper::EnumMnemonics g_NDirectFlags[] =
{
#define NDF_ENTRY(x) NativeImageDumper::EnumMnemonics( NDirectMethodDesc:: x, W(#x) )
        NDF_ENTRY(kEarlyBound),
        NDF_ENTRY(kHasSuppressUnmanagedCodeAccess),
        NDF_ENTRY(kIsMarshalingRequiredCached),
        NDF_ENTRY(kCachedMarshalingRequired),
        NDF_ENTRY(kNativeAnsi),
        NDF_ENTRY(kLastError),
        NDF_ENTRY(kNativeNoMangle),
        NDF_ENTRY(kVarArgs),
        NDF_ENTRY(kStdCall),
        NDF_ENTRY(kThisCall),
        NDF_ENTRY(kIsQCall),
        NDF_ENTRY(kStdCallWithRetBuf),
#undef NDF_ENTRY
};
NativeImageDumper::EnumMnemonics NativeImageDumper::s_IMDFlags[] =
{
#define IMD_ENTRY(x) NativeImageDumper::EnumMnemonics( InstantiatedMethodDesc:: x, W(#x) )

#define IMD_KIND_ENTRY(x) NativeImageDumper::EnumMnemonics( InstantiatedMethodDesc:: x, InstantiatedMethodDesc::KindMask, W(#x) )
        IMD_KIND_ENTRY(GenericMethodDefinition),
        IMD_KIND_ENTRY(UnsharedMethodInstantiation),
        IMD_KIND_ENTRY(SharedMethodInstantiation),
        IMD_KIND_ENTRY(WrapperStubWithInstantiations),
#undef IMD_KIND_ENTRY

#ifdef EnC_SUPPORTED
        // Method is a new virtual function added through EditAndContinue.
        IMD_ENTRY(EnCAddedMethod),
#endif // EnC_SUPPORTED

        IMD_ENTRY(Unrestored),

#ifdef FEATURE_COMINTEROP
        IMD_ENTRY(HasComPlusCallInfo),
#endif // FEATURE_COMINTEROP

#undef IMD_ENTRY
};

void NativeImageDumper::DumpPrecode( PTR_Precode precode, PTR_Module module )
{
    _ASSERTE(isPrecode(PTR_TO_TADDR(precode)));

    PrecodeType pType = precode->GetType();
    switch(pType)
    {
#define DISPLAY_PRECODE(type) \
        IF_OPT_AND(PRECODES, METHODDESCS)   \
        { \
            PTR_ ## type p( precode->As ## type () ); \
            DisplayStartStructure( # type, \
                                   DPtrToPreferredAddr(p), \
                                   sizeof(*p), ALWAYS ); \
            WriteFieldMethodDesc( m_pMethodDesc, \
                                  p->m_pMethodDesc, \
                                  type, ALWAYS ); \
            TADDR target = p->GetTarget(); \
            DisplayWriteElementPointer("Target",\
                                        DataPtrToDisplay(target),\
                                        ALWAYS );\
            DisplayEndStructure( ALWAYS ); \
        }

    case PRECODE_STUB:
        DISPLAY_PRECODE(StubPrecode); break;
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    case PRECODE_NDIRECT_IMPORT:
        DISPLAY_PRECODE(NDirectImportPrecode); break;
#endif
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        IF_OPT_AND(PRECODES, METHODDESCS)
        {
            PTR_FixupPrecode p( precode->AsFixupPrecode() );
            DisplayStartStructure( "FixupPrecode",
                                   DPtrToPreferredAddr(p),
                                   sizeof(*p),
                                   ALWAYS );
            PTR_MethodDesc precodeMD(p->GetMethodDesc());
#ifdef HAS_FIXUP_PRECODE_CHUNKS
            {
                DisplayWriteFieldInt( m_MethodDescChunkIndex,
                                      p->m_MethodDescChunkIndex, FixupPrecode,
                                      ALWAYS );
                DisplayWriteFieldInt( m_PrecodeChunkIndex,
                                      p->m_PrecodeChunkIndex, FixupPrecode,
                                      ALWAYS );
                if( p->m_PrecodeChunkIndex == 0 )
                {
                    //dump the location of the Base
                    DisplayWriteElementAddress( "PrecodeChunkBase",
                                                DataPtrToDisplay(p->GetBase()),
                                                sizeof(void*), ALWAYS );
                }
                //Make sure I align up if there is no code slot to make
                //sure that I get the padding
                TADDR mdPtrStart = p->GetBase()
                    + (p->m_MethodDescChunkIndex * MethodDesc::ALIGNMENT);
                TADDR mdPtrEnd = ALIGN_UP( mdPtrStart + sizeof(MethodDesc*),
                                           8 );
                CoverageRead( mdPtrStart, (ULONG32)(mdPtrEnd - mdPtrStart) );
                TADDR precodeMDSlot = p->GetBase()
                    + p->m_MethodDescChunkIndex * MethodDesc::ALIGNMENT;
                DoWriteFieldMethodDesc( "MethodDesc",
                                        (DWORD)(precodeMDSlot - PTR_TO_TADDR(p)),
                                        sizeof(TADDR), precodeMD );
            }
#else //HAS_FIXUP_PRECODE_CHUNKS
            WriteFieldMethodDesc( m_pMethodDesc,
                                  p->m_pMethodDesc,
                                  FixupPrecode, ALWAYS );
#endif //HAS_FIXUP_PRECODE_CHUNKS
            TADDR target = p->GetTarget();
            DisplayWriteElementPointer("Target",
                                        DataPtrToDisplay(target),
                                        ALWAYS );
            /* REVISIT_TODO Thu 01/05/2006
             * dump slot with offset if it is here
             */
            DisplayEndStructure( ALWAYS ); //FixupPrecode
        }
        break;
#endif
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        DISPLAY_PRECODE(ThisPtrRetBufPrecode); break;
#endif
    default:
        _ASSERTE( !"Unsupported precode type" );
#undef DISPLAY_PRECODE
#undef PrecodeMDWrite
    }
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void NativeImageDumper::DumpMethodDesc( PTR_MethodDesc md, PTR_Module module )
{
    //StoredSigMethodDesc

    MethodClassification mc =
        (MethodClassification)md->GetClassification();
    _ASSERTE(mc >= 0 && mc < mcCount);
    const char * mdTypeName = s_MDTypeName[mc];
    unsigned mdSize = (unsigned)md->SizeOf();

    DisplayStartStructure( mdTypeName, DPtrToPreferredAddr(md),
                           mdSize, METHODDESCS );
    IF_OPT(METHODDESCS)
    {
        TempBuffer buf;
        MethodDescToString( md, buf );
        DisplayWriteElementStringW( "Name", (const WCHAR *)buf, METHODDESCS );
    }
#ifdef _DEBUG
    IF_OPT(METHODDESCS)
    {
        WriteFieldStr(m_pszDebugMethodName,
                      PTR_BYTE(TO_TADDR(md->m_pszDebugMethodName)),
                      MethodDesc, METHODDESCS);
        WriteFieldStr(m_pszDebugClassName,
                      PTR_BYTE(TO_TADDR(md->m_pszDebugClassName)),
                      MethodDesc, METHODDESCS);
        WriteFieldStr(m_pszDebugMethodSignature,
                      PTR_BYTE(TO_TADDR(md->m_pszDebugMethodSignature)),
                      MethodDesc, METHODDESCS);
    }
    else
    {
        CoverageReadString(TO_TADDR(md->m_pszDebugMethodName));
        CoverageReadString(TO_TADDR(md->m_pszDebugClassName));
        CoverageReadString(TO_TADDR(md->m_pszDebugMethodSignature));
    }
#endif

    DisplayWriteFieldInt( m_wFlags3AndTokenRemainder, md->m_wFlags3AndTokenRemainder,
                          MethodDesc, METHODDESCS );

    DisplayWriteFieldInt( m_chunkIndex, md->m_chunkIndex,
                          MethodDesc, METHODDESCS );

    /* XXX Fri 03/24/2006
     * This is a workaround.  The InstantiatedMethodDescs are in chunks, but there's
     * no obvious place to display the chunk, so display the bounds here.
     */
    if( mc == mcInstantiated && md->m_chunkIndex == 0 )
    {
        PTR_MethodDescChunk chunk( md->GetMethodDescChunk() );
        DisplayWriteElementAddress( "MethodDescChunk", DPtrToPreferredAddr(chunk),
                                    chunk->SizeOf(), METHODDESCS );
    }

    DisplayWriteFieldEnumerated( m_bFlags2, md->m_bFlags2, MethodDesc,
                                 s_MDFlag2, W(", "), METHODDESCS );

    DisplayWriteFieldInt( m_wSlotNumber, md->GetSlot(), MethodDesc,
                          METHODDESCS );
    DisplayWriteFieldEnumerated( m_wFlags, md->m_wFlags, MethodDesc,
                                 s_MDC, W(", "), METHODDESCS );

    IF_OPT(IL)
    {
        if( md->IsIL() )
        {
            PTR_MethodDescChunk chunk(md->GetMethodDescChunk());
            //chunk is implicitly remapped because it's calculated from the pointer
            //to MD.
            PTR_MethodTable mt = chunk->GetMethodTable();
            if( !CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(mt)) )
            {
                if ( md->IsTypicalMethodDefinition() )
                {
                    DWORD dwRVA = 0;
                    m_import->GetMethodProps(md->GetMemberDef(), NULL, NULL, NULL, 0,
                        NULL, NULL, NULL, &dwRVA, NULL);

                    if (dwRVA != 0)
                    {
                        _ASSERTE(m_ILHostCopy);
                        _ASSERTE(m_ILSectionStart);
                        _ASSERTE(dwRVA >= m_ILSectionStart);
                        _ASSERTE(dwRVA < (m_ILSectionStart + m_ILSectionSize));
                        //The RVA is from the start of the file, so convert it
                        //to an RVA to the start of the .text section.
                        TADDR pILTarget = (TADDR)m_decoder.GetRvaData(dwRVA);
                        COR_ILMETHOD * pILHeader = (COR_ILMETHOD*)(m_ILHostCopy + dwRVA - m_ILSectionStart);

                        COR_ILMETHOD_DECODER decoder(pILHeader);

                        DisplayStartStructure( "IL",
                                       DataPtrToDisplay(pILTarget),
                                       PEDecoder::ComputeILMethodSize(pILTarget),
                                       ALWAYS );

                        DisplayWriteElementInt( "CodeSize", decoder.GetCodeSize(), ALWAYS );

                        // Dump the disassembled IL code?

                        DisplayEndStructure( ALWAYS );
                    }
                }
            }
        }
    }
    if( md->HasPrecode() )
    {
        PTR_Precode precode( md->GetPrecode() );

        DumpPrecode( precode, module );
    }
    if ( md->HasNonVtableSlot() )
    {
        DisplayWriteElementInt( "Slot", (DWORD)(md->GetAddrOfSlot() - PTR_TO_TADDR(md)), ALWAYS);
    }
    if (md->HasNativeCodeSlot())
    {
        DisplayWriteElementInt( "NativeCode", DWORD(md->GetAddrOfNativeCodeSlot() - PTR_TO_TADDR(md)), ALWAYS);
        //m_display->WriteFieldPointer( "NativeCode",
        //                              DWORD(md->GetAddrOfNativeCodeSlot() - PTR_TO_TADDR(md)),
        //                              sizeof(TADDR),
        //                              md->GetNativeCode() );
    }
    if (md->HasMethodImplSlot())
    {
        DisplayStartVStructure( "MethodImpl", METHODDESCS );
        PTR_MethodImpl impl(md->GetMethodImpl());
        PTR_DWORD slots = impl->GetSlots() - 1;  // GetSlots returns the address of the first real slot (past the size)
        unsigned numSlots = impl->GetSize();
        _ASSERTE(!numSlots || numSlots == slots[0]);
        _ASSERTE(slots == NULL || isInRange(PTR_TO_TADDR(slots)));
        if ((slots != NULL) && isInRange(PTR_TO_TADDR(slots)))
        {
            DisplayWriteFieldAddress(pdwSlots, DataPtrToDisplay(dac_cast<TADDR>(slots)),
                                     (numSlots + 1) * sizeof(*slots),
                                     MethodImpl, METHODDESCS);
        }
        else
        {
            DisplayWriteFieldPointer(pdwSlots, DataPtrToDisplay(dac_cast<TADDR>(slots)),
                                     MethodImpl, METHODDESCS);

        }
        _ASSERTE(impl->pImplementedMD.IsNull()
                 || isInRange(PTR_TO_TADDR(impl->GetImpMDsNonNull())));
        if (!impl->pImplementedMD.IsNull() &&
            isInRange(PTR_TO_TADDR(impl->GetImpMDsNonNull())))
        {
            DisplayWriteFieldAddress( pImplementedMD,
                                      DataPtrToDisplay(dac_cast<TADDR>(impl->GetImpMDsNonNull())),
                                      numSlots * sizeof(RelativePointer <MethodDesc*>),
                                      MethodImpl, METHODDESCS );
        }
        else
        {
            DisplayWriteFieldPointer( pImplementedMD,
                                      DataPtrToDisplay(dac_cast<TADDR>(impl->GetImpMDs())),
                                      MethodImpl, METHODDESCS );
        }
        DisplayEndVStructure( METHODDESCS );
    }
    if (md->HasStoredSig())
    {
        DisplayStartVStructure( "StoredSigMethodDesc", METHODDESCS );
        PTR_StoredSigMethodDesc ssmd(md);
        //display signature information.
        if( isInRange(ssmd->GetSigRVA()) )
        {
            DisplayWriteFieldAddress(m_pSig, DataPtrToDisplay(ssmd->GetSigRVA()),
                                     ssmd->m_cSig, StoredSigMethodDesc,
                                     METHODDESCS);
        }
        else
        {
            DisplayWriteFieldPointer(m_pSig, DataPtrToDisplay(ssmd->GetSigRVA()),
                                     StoredSigMethodDesc, METHODDESCS);

        }
        CoverageRead(TO_TADDR(ssmd->GetSigRVA()), ssmd->m_cSig);
        DisplayWriteFieldInt( m_cSig, ssmd->m_cSig,
                              StoredSigMethodDesc, METHODDESCS );
#ifdef HOST_64BIT
        DisplayWriteFieldEnumerated( m_dwExtendedFlags,
                                     ssmd->m_dwExtendedFlags,
                                     StoredSigMethodDesc,
                                     s_SSMDExtendedFlags, W(", "),
                                     METHODDESCS );
#endif
        DisplayEndVStructure( METHODDESCS ); //StoredSigMethodDesc
    }
    if( mc == mcDynamic )
    {
        PTR_DynamicMethodDesc dmd(md);
        DisplayStartVStructure( "DynamicMethodDesc", METHODDESCS );
        WriteFieldStr( m_pszMethodName, PTR_BYTE(dmd->GetMethodName()),
                       DynamicMethodDesc, METHODDESCS );
        if( !CHECK_OPT(METHODDESCS) )
            CoverageReadString( PTR_TO_TADDR(dmd->GetMethodName()) );
        DisplayWriteFieldPointer( m_pResolver,
                                  DPtrToPreferredAddr(dmd->m_pResolver),
                                  DynamicMethodDesc, METHODDESCS );
#ifndef HOST_64BIT
        DisplayWriteFieldEnumerated( m_dwExtendedFlags,
                                     dmd->m_dwExtendedFlags,
                                     DynamicMethodDesc,
                                     s_SSMDExtendedFlags, W(", "),
                                     METHODDESCS );
#endif
        DisplayEndVStructure( METHODDESCS );
    }
    if (mc == mcFCall )
    {
        PTR_FCallMethodDesc fcmd(md);
        DisplayStartVStructure( "FCallMethodDesc", METHODDESCS );

        DisplayWriteFieldInt( m_dwECallID,
                              fcmd->m_dwECallID,
                              FCallMethodDesc,
                              METHODDESCS );

        DisplayEndVStructure( METHODDESCS ); //NDirectMethodDesc
    }
    if( mc == mcNDirect )
    {
        PTR_NDirectMethodDesc ndmd(md);
        DisplayStartVStructure( "NDirectMethodDesc", METHODDESCS );
        DPTR(NDirectMethodDesc::temp1) nd( PTR_HOST_MEMBER_TADDR(NDirectMethodDesc, ndmd, ndirect) );
        DisplayStartStructureWithOffset( ndirect,
                                         DPtrToPreferredAddr(nd),
                                         sizeof(*nd), NDirectMethodDesc,
                                         METHODDESCS );
        DisplayWriteFieldPointer( m_pNativeNDirectTarget,
                                  DataPtrToDisplay((TADDR)nd->m_pNativeNDirectTarget),
                                  NDirectMethodDesc::temp1,
                                  METHODDESCS );
        DisplayWriteFieldEnumerated( m_wFlags, nd->m_wFlags,
                                     NDirectMethodDesc::temp1,
                                     g_NDirectFlags, W(", "),
                                     METHODDESCS );

        WriteFieldStr( m_pszEntrypointName,
                       PTR_BYTE(dac_cast<TADDR>(ndmd->GetEntrypointName())),
                       NDirectMethodDesc::temp1, METHODDESCS );
        if( !CHECK_OPT(METHODDESCS) )
            CoverageReadString(dac_cast<TADDR>(ndmd->GetEntrypointName()));
        if (md->IsQCall())
        {
            DisplayWriteFieldInt( m_dwECallID,
                                  nd->m_dwECallID,
                                  NDirectMethodDesc::temp1,
                                  METHODDESCS );
        }
        else
        {
            WriteFieldStr( m_pszLibName,
                           PTR_BYTE(dac_cast<TADDR>(ndmd->GetLibNameRaw())),
                           NDirectMethodDesc::temp1, METHODDESCS );
        }
        if( !CHECK_OPT(METHODDESCS) )
            CoverageReadString( dac_cast<TADDR>(ndmd->GetLibNameRaw()) );

        PTR_NDirectWriteableData wnd( ndmd->GetWriteableData() );
        DisplayStartStructureWithOffset( m_pWriteableData,
                                         DPtrToPreferredAddr(wnd),
                                         sizeof(*wnd),
                                         NDirectMethodDesc::temp1,
                                         METHODDESCS );
        DisplayWriteFieldPointer( m_pNDirectTarget,
                                  DataPtrToDisplay((TADDR)wnd->m_pNDirectTarget), NDirectWriteableData, METHODDESCS );
        if( !CHECK_OPT(METHODDESCS) )
            CoverageRead( PTR_TO_TADDR(wnd), sizeof(*wnd) );
        DisplayEndStructure( METHODDESCS ); //m_pWriteableData

        PTR_NDirectImportThunkGlue glue(ndmd->GetNDirectImportThunkGlue());

#ifdef HAS_NDIRECT_IMPORT_PRECODE
        if (glue == NULL)
        {
            // import thunk glue is not needed for P/Invoke that is not inlinable
            DisplayWriteFieldPointer( m_pImportThunkGlue,
                                      NULL,
                                      NDirectMethodDesc::temp1,
                                      METHODDESCS );
        }
        else
        {
            DisplayStartStructureWithOffset( m_pImportThunkGlue,
                                             DPtrToPreferredAddr(glue),
                                             sizeof(*glue),
                                             NDirectMethodDesc::temp1,
                                             METHODDESCS);
#else
            DisplayStartStructureWithOffset( m_ImportThunkGlue,
                                             DPtrToPreferredAddr(glue),
                                             sizeof(*glue),
                                             NDirectMethodDesc::temp1,
                                             METHODDESCS);
#endif
#ifdef HAS_NDIRECT_IMPORT_PRECODE
            /* REVISIT_TODO Thu 01/05/2006
             * Dump this properly as a precode
             */
            WriteFieldMethodDesc( m_pMethodDesc, glue->m_pMethodDesc,
                                  NDirectImportThunkGlue, METHODDESCS );
            {
                PTR_Precode p(glue);
                DumpPrecode( p, module );
            }
            if( !CHECK_OPT(METHODDESCS) )
                CoverageRead(PTR_TO_TADDR(glue), sizeof(*glue));
            /* REVISIT_TODO Fri 12/16/2005
             * Factor out this code into some shared precode dumping code
             */
#else //!HAS_NDIRECT_IMPORT_PRECODE
            /* REVISIT_TODO Fri 10/27/2006
             * For Whidbey AMD64 (!HAS_NDIRECT_IMPORT_PRECODE), I don't have this data structure in the output.
             */
#endif //HAS_NDIRECT_IMPORT_PRECODE

            DisplayEndStructure( METHODDESCS ); //m_pImportThunkGlue
#ifdef HAS_NDIRECT_IMPORT_PRECODE
        }
#endif

#ifdef TARGET_X86
        DisplayWriteFieldInt( m_cbStackArgumentSize,
                              nd->m_cbStackArgumentSize,
                              NDirectMethodDesc::temp1, METHODDESCS );
#endif

        WriteFieldMethodDesc( m_pStubMD,
                              nd->m_pStubMD.GetValueMaybeNull(PTR_HOST_MEMBER_TADDR(NDirectMethodDesc::temp1, nd, m_pStubMD)),
                              NDirectMethodDesc::temp1, METHODDESCS );

        DisplayEndStructure( METHODDESCS ); //ndirect


        DisplayEndVStructure( METHODDESCS ); //NDirectMethodDesc
    }
    if( mc == mcEEImpl )
    {
        DisplayStartVStructure( "EEImplMethodDesc", METHODDESCS );
        DisplayEndVStructure( METHODDESCS );
    }
#if defined(FEATURE_COMINTEROP)
    if( mc == mcComInterop )
    {
        PTR_ComPlusCallMethodDesc cpmd(md);
        DisplayStartVStructure( "ComPlusCallMethodDesc", METHODDESCS );
        PTR_ComPlusCallInfo compluscall((TADDR)cpmd->m_pComPlusCallInfo);

        if (compluscall == NULL)
        {
            DisplayWriteFieldPointer( m_pComPlusCallInfo,
                                      NULL,
                                      ComPlusCallMethodDesc,
                                      METHODDESCS );
        }
        else
        {
            DumpComPlusCallInfo( compluscall, METHODDESCS );
        }

        DisplayEndVStructure( METHODDESCS ); //ComPlusCallMethodDesc
    }
#endif
    if( mc == mcInstantiated )
    {
        PTR_InstantiatedMethodDesc imd(md);
        DisplayStartVStructure( "InstantiatedMethodDesc", METHODDESCS );
        unsigned kind = imd->m_wFlags2
            & InstantiatedMethodDesc::KindMask;
        if( kind == InstantiatedMethodDesc::SharedMethodInstantiation )
        {
            PTR_DictionaryLayout layout(dac_cast<TADDR>(imd->GetDictLayoutRaw()));
            IF_OPT(METHODDESCS)
            {
                WriteFieldDictionaryLayout( "m_pDictLayout",
                                            offsetof(InstantiatedMethodDesc, m_pDictLayout ),
                                            fieldsize(InstantiatedMethodDesc, m_pDictLayout),
                                            layout,
                                            GetDependencyFromMD(md)->pImport );
            }
            else
            {
                while( layout != NULL )
                {
                    CoverageRead( PTR_TO_TADDR(layout),
                                  sizeof(DictionaryLayout)
                                  + sizeof(DictionaryEntryLayout)
                                  * (layout->m_numSlots - 1) );
                    layout = PTR_DictionaryLayout(TO_TADDR(layout->m_pNext));
                }
            }
        }
        else if( kind ==
                 InstantiatedMethodDesc::WrapperStubWithInstantiations )
        {
            PTR_MethodDesc wimd(imd->IMD_GetWrappedMethodDesc());
            if( wimd == NULL || !DoWriteFieldAsFixup( "m_pWrappedMethodDesc",
                                                      offsetof(InstantiatedMethodDesc, m_pWrappedMethodDesc),
                                                      fieldsize(InstantiatedMethodDesc, m_pWrappedMethodDesc),
                                                      PTR_TO_TADDR(wimd) ) )
            {
                WriteFieldMethodDesc( m_pWrappedMethodDesc, wimd,
                                      InstantiatedMethodDesc, METHODDESCS );
            }
        }
        else
        {
            _ASSERTE(imd->m_pDictLayout.IsNull());
            DisplayWriteFieldPointer( m_pDictLayout, NULL,
                                      InstantiatedMethodDesc,
                                      METHODDESCS );
        }
        //now handle the contents of the m_pMethInst/m_pPerInstInfo union.
        unsigned numSlots = imd->m_wNumGenericArgs;
        PTR_Dictionary inst(imd->IMD_GetMethodDictionary());
        unsigned dictSize;
        if( kind == InstantiatedMethodDesc::SharedMethodInstantiation )
        {
            dictSize = sizeof(TypeHandle);
        }
        else if( kind == InstantiatedMethodDesc::WrapperStubWithInstantiations )
        {
            PTR_InstantiatedMethodDesc wrapped =
                PTR_InstantiatedMethodDesc(imd->IMD_GetWrappedMethodDesc());
            if( CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(wrapped)) )
            {
                /* XXX Mon 03/27/2006
                 * Note that 4 is the correct answer for all IMDs at this time.
                 */
                TempBuffer buf;
                MethodDescToString( md, buf );
                //m_display->ErrorPrintF( "WARNING! InstantiatedMethodDesc %S wraps a MethodDesc that is a fixup.  I cannot accurately determine the size of the associated generic dictionary.  Assuming 4.\n", (const WCHAR *)buf );
                dictSize = (imd->GetNumGenericMethodArgs() + 4) * sizeof(void*);
            }
            else
            {
                PTR_DictionaryLayout layout(wrapped->IsSharedByGenericMethodInstantiations()
                                            ? dac_cast<TADDR>(wrapped->GetDictLayoutRaw()) : NULL );
                dictSize = DictionaryLayout::GetDictionarySizeFromLayout(imd->GetNumGenericMethodArgs(),
                                                                          layout);
            }
        }
        else
        {
            dictSize = sizeof(TypeHandle);
        }
        //instantiations has the number of slots of
        //GetNumGenericMethodArgs.
        if( inst == NULL )
        {
            m_display->WriteFieldPointer( "m_pPerInstInfo",
                                          offsetof(InstantiatedMethodDesc, m_pPerInstInfo),
                                          fieldsize(InstantiatedMethodDesc, m_pPerInstInfo),
                                          NULL );
        }
        else
        {
            IF_OPT(METHODDESCS)
            {

                m_display->StartStructureWithOffset( "m_pPerInstInfo",
                                                     offsetof(InstantiatedMethodDesc, m_pPerInstInfo),
                                                     fieldsize(InstantiatedMethodDesc, m_pPerInstInfo),
                                                     DPtrToPreferredAddr(inst),
                                                     dictSize );
            }
            DisplayStartArray( "InstantiationInfo", W("[%-2s]: %s"),
                               METHODDESCS );
            /* REVISIT_TODO Thu 03/23/2006
             * This doesn't dump the contents of the dictionary which are
             * hanging around after the real slots.  Get around to doing that.
             */
            for( unsigned i = 0; i < numSlots
                 && CHECK_OPT(METHODDESCS); ++i )
            {
                DisplayStartElement( "Handle", METHODDESCS );
                DisplayWriteElementInt( "Index", i, METHODDESCS );

                TypeHandle thArg = inst->GetInstantiation()[i].GetValue();
                IF_OPT(METHODDESCS)
                    WriteElementTypeHandle( "TypeHandle", thArg);

                /* XXX Fri 03/24/2006
                 * There is no really good home for TypeDescs, so I gotta check
                 * lots of places for them.
                 */
                if( !CORCOMPILE_IS_POINTER_TAGGED(thArg.AsTAddr()) &&
                    thArg.IsTypeDesc() )
                {
                    PTR_TypeDesc td(thArg.AsTypeDesc());
                    if( isInRange(PTR_TO_TADDR(td)) )
                    {
                        m_discoveredTypeDescs.AppendEx(td);
                    }
                }
                DisplayEndElement( METHODDESCS ); //Handle
            }
            //Instantiation Info
            DisplayEndArray( "Total TypeHandles", METHODDESCS );

            DisplayEndVStructure(METHODDESCS); //m_pPerInstInfo;
            if( !CHECK_OPT(METHODDESCS) )
                CoverageRead(PTR_TO_TADDR(inst), numSlots * sizeof(*inst));
        }

        DisplayWriteFieldEnumerated( m_wFlags2, imd->m_wFlags2,
                                     InstantiatedMethodDesc, s_IMDFlags,
                                     W(", "), METHODDESCS );
        DisplayWriteFieldInt( m_wNumGenericArgs, imd->m_wNumGenericArgs,
                              InstantiatedMethodDesc, METHODDESCS );

#ifdef FEATURE_COMINTEROP
        if (imd->IsGenericComPlusCall())
        {
            PTR_ComPlusCallInfo compluscall = imd->IMD_GetComPlusCallInfo();
            DumpComPlusCallInfo( compluscall, METHODDESCS );
        }
#endif // FEATURE_COMINTEROP

        DisplayEndStructure( METHODDESCS );
    }

    DisplayEndStructure( METHODDESCS ); //MethodDesc (mdTypeName)
    if( !CHECK_OPT(METHODDESCS) )
        CoverageRead( PTR_TO_TADDR(md), mdSize );

}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

NativeImageDumper::EnumMnemonics NativeImageDumper::s_EECLIFlags[] =
{
#define EECLI_FLAGS_ENTRY(x) NativeImageDumper::EnumMnemonics( EEClassLayoutInfo:: x, W(#x) )
    EECLI_FLAGS_ENTRY(e_BLITTABLE),
    EECLI_FLAGS_ENTRY(e_MANAGED_SEQUENTIAL),
    EECLI_FLAGS_ENTRY(e_ZERO_SIZED),
    EECLI_FLAGS_ENTRY(e_HAS_EXPLICIT_SIZE),
#undef EECLI_FLAGS_ENTRY
};


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void
NativeImageDumper::DumpEEClassForMethodTable( PTR_MethodTable mt )
{
    PTR_EEClass clazz = mt->GetClass();

    _ASSERTE(CHECK_OPT(EECLASSES));
    _ASSERTE(clazz != NULL);
    _ASSERTE(isInRange(PTR_TO_TADDR(clazz)));

    const char * eeClassType;

    if( clazz->HasLayout() )
        eeClassType = "LayoutEEClass";
    else if( mt->IsArray() )
        eeClassType = "ArrayClass";
    else if( clazz->IsDelegate() )
        eeClassType = "DelegateEEClass";
    else
        eeClassType = "EEClass";

    DisplayStartStructure( eeClassType, DPtrToPreferredAddr(clazz), clazz->GetSize(),
                           EECLASSES );
    {
        TempBuffer buf;
        MethodTableToString( mt, buf );
        DisplayWriteElementStringW( "Name", (const WCHAR *)buf, EECLASSES );
    }

    PTR_GuidInfo guidInfo = clazz->GetGuidInfo();
    if(guidInfo != NULL)
    {
        DisplayStartStructureWithOffset( m_pGuidInfo,
                                         DPtrToPreferredAddr(guidInfo),
                                         sizeof(*guidInfo), EEClass,
                                         EECLASSES );
        TempBuffer buf;
        GuidToString( guidInfo->m_Guid, buf );
        DisplayWriteFieldStringW( m_Guid, (const WCHAR *)buf, GuidInfo,
                                  EECLASSES );
        DisplayWriteFieldFlag( m_bGeneratedFromName,
                               guidInfo->m_bGeneratedFromName,
                               GuidInfo, EECLASSES );
        DisplayEndStructure( EECLASSES ); //guidinfo
    }
    else
    {
        /* XXX Fri 10/14/2005
         * if Clazz isn't an interface, m_pGuidInfo is undefined.
         */
        DisplayWriteFieldPointerAnnotated( m_pGuidInfo, PTR_TO_TADDR(guidInfo),
                                           W("Invalid"), EEClass, EECLASSES );
    }


#ifdef _DEBUG
    WriteFieldStr( m_szDebugClassName,
                   PTR_BYTE(TO_TADDR(clazz->m_szDebugClassName)),
                   EEClass, EECLASSES );
    DisplayWriteFieldFlag( m_fDebuggingClass, clazz->m_fDebuggingClass,
                           EEClass, EECLASSES );
#endif

    WriteFieldMethodTable( m_pMethodTable, clazz->GetMethodTable(), EEClass,
                           EECLASSES );

    WriteFieldCorElementType( m_NormType, (CorElementType)clazz->m_NormType,
                              EEClass, EECLASSES );

    PTR_FieldDesc fdList = clazz->GetFieldDescList();

    ULONG fieldCount = (ULONG)CountFields(mt);
    _ASSERTE((fdList == NULL) == (fieldCount == 0));

    IF_OPT(EECLASSES)
    {
        m_display->StartStructureWithOffset( "m_pFieldDescList",
                                             offsetof(EEClass, m_pFieldDescList),
                                             fieldsize(EEClass, m_pFieldDescList),
                                             DPtrToPreferredAddr(fdList),
                                             fdList != NULL ?
                                                sizeof(*fdList) * fieldCount :
                                                0 );
    }
    IF_OPT(VERBOSE_TYPES)
    {
        if( fdList != NULL )
        {
            DisplayStartArray( "FieldDescs", NULL, EECLASSES );
            for( SIZE_T i = 0; i < fieldCount; ++i )
            {
                PTR_FieldDesc fd = fdList + i;
                IF_OPT(EECLASSES)
                    DumpFieldDesc( fd, "FieldDesc" );
            }
            DisplayEndArray( "Total FieldDescs", EECLASSES ); //FieldDescs
        }
    }
    else if( (fdList != NULL) && CHECK_OPT(DEBUG_COVERAGE) )
    {
        for( SIZE_T i = 0; i < fieldCount; ++i )
        {
            PTR_FieldDesc fd = fdList + i;
#ifdef _DEBUG
            if( fd != NULL && fd->m_debugName != NULL )
                CoverageReadString( fd->m_debugName );
#endif
        }
        CoverageRead( PTR_TO_TADDR(fdList), sizeof(*fdList) * fieldCount );
    }

    DisplayEndStructure( EECLASSES ); //FieldDescList

    DisplayWriteFieldEnumerated( m_dwAttrClass, clazz->GetAttrClass(),
                                 EEClass, s_CorTypeAttr, W(" "), EECLASSES );
    DisplayWriteFieldEnumerated( m_VMFlags, clazz->m_VMFlags, EEClass,
                                 s_VMFlags, W(", "), EECLASSES );

    PTR_MethodDescChunk chunk = clazz->GetChunks();

    DisplayStartArrayWithOffset( m_pChunks, NULL, EEClass, EECLASSES );
    while( chunk != NULL )
    {
        DisplayStartStructure( "MethodDescChunk",
                               DPtrToPreferredAddr(chunk),
                               chunk->SizeOf(), EECLASSES );
        _ASSERTE(!CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(chunk->GetMethodTable())));
        PTR_MethodTable chunkMT = chunk->GetMethodTable();
        DisplayWriteFieldPointer( m_methodTable,
                                  DPtrToPreferredAddr(chunkMT),
                                  MethodDescChunk, EECLASSES );
        PTR_MethodDescChunk chunkNext = chunk->GetNextChunk();
        DisplayWriteFieldPointer( m_next,
                                  DPtrToPreferredAddr(chunkNext),
                                  MethodDescChunk, EECLASSES );
        DisplayWriteFieldInt( m_size, chunk->m_size, MethodDescChunk,
                              EECLASSES );
        DisplayWriteFieldInt( m_count, chunk->m_count, MethodDescChunk,
                              EECLASSES );
        DisplayWriteFieldInt( m_flagsAndTokenRange, chunk->m_flagsAndTokenRange, MethodDescChunk,
                              EECLASSES );
        /* XXX Wed 12/14/2005
         * Don't skip walking this array.  I need to make sure I touch the
         * precodes.
         */
        DisplayStartArray( "MethodDescs", NULL, METHODDESCS );
        PTR_MethodDesc md(chunk->GetFirstMethodDesc());
        while (md != NULL)
        {
            IF_OPT_OR(METHODDESCS, DEBUG_COVERAGE)
            {
                PTR_Module module = mt->GetModule();
                if(CORCOMPILE_IS_POINTER_TAGGED(PTR_TO_TADDR(module) ))
                    DumpMethodDesc( md, PTR_Module((TADDR)0) );
                else
                    DumpMethodDesc( md, module );
            }

            // Check whether the next MethodDesc is within the bounds of the current chunks
            TADDR pNext = PTR_HOST_TO_TADDR(md) + md->SizeOf();
            TADDR pEnd = PTR_HOST_TO_TADDR(chunk) + chunk->SizeOf();

            md = (pNext < pEnd) ? PTR_MethodDesc(pNext) : NULL;
        }

        DisplayEndArray( "Total MethodDescs", METHODDESCS); //MethodDescs

        chunk = chunk->GetNextChunk();

        DisplayEndStructure( EECLASSES ); //MethodDescChunk
    }

    DisplayEndArray( "Total MethodDescChunks", EECLASSES );
    /* REVISIT_TODO Fri 10/14/2005
     * Dump the class dependencies
     */
    //_ASSERTE(!clazz->m_classDependencies.TestAnyBit());

    /* REVISIT_TODO Mon 10/24/2005
     * Create vstructure for union?
     */
    //decode union here
#ifdef FEATURE_COMINTEROP
    if( clazz->IsBlittable() || clazz->HasLayout() )
    {
        DisplayWriteFieldInt(m_cbNativeSize, clazz->m_cbNativeSize, EEClass,
                             EECLASSES );
    }
    else if( clazz->IsInterface() )
    {
        DisplayWriteFieldPointer( m_ohDelegate,
                                  DataPtrToDisplay(clazz->m_ohDelegate),
                                  EEClass, EECLASSES );
    }
    else
    {
        static const WCHAR * ifnames[] ={W("Dual"),W("Vtable"),W("Dispatch")};
        m_display->WriteFieldEnumerated( "ComInterfaceType",
                                         offsetof(EEClass,
                                                  m_ComInterfaceType),
                                         fieldsize(EEClass,
                                                   m_ComInterfaceType),
                                         (int)clazz->m_ComInterfaceType,
                                         ifnames[(int)clazz->m_ComInterfaceType] );
    }
#else
    DisplayWriteFieldInt( m_cbNativeSize, clazz->m_cbNativeSize,
                          EEClass, EECLASSES );
#endif

#if defined(FEATURE_COMINTEROP)
    PTR_ComCallWrapperTemplate ccwTemplate(TO_TADDR(clazz->m_pccwTemplate));
    if( ccwTemplate != NULL )
    {
        DisplayWriteFieldPointer( m_pccwTemplate, NULL, EEClass,
                                  EECLASSES );
    }
    else
    {
        /* REVISIT_TODO Fri 10/14/2005
         * Dump CcwTemplate
         */
        DisplayWriteFieldPointer( m_pccwTemplate,
                                  DPtrToPreferredAddr(ccwTemplate), EEClass,
                                  EECLASSES );
    }
#endif // defined(FEATURE_COMINTEROP)

    //fields for classes that aren't just EEClasses.
    if( clazz->HasLayout() )
    {
        PTR_LayoutEEClass layoutClass(PTR_TO_TADDR(clazz));
        DisplayStartVStructure("LayoutEEClass", EECLASSES );

        PTR_EEClassLayoutInfo eecli( PTR_HOST_MEMBER_TADDR( LayoutEEClass,
                                                            layoutClass,
                                                            m_LayoutInfo ) );
        DisplayStartStructureWithOffset( m_LayoutInfo,
                                         DPtrToPreferredAddr(eecli),
                                         sizeof(EEClassLayoutInfo),
                                         LayoutEEClass, EECLASSES );
        /* REVISIT_TODO Fri 10/14/2005
         * Dump EEClassLayoutInfo
         */
        DisplayWriteFieldInt( m_cbNativeSize, eecli->m_cbNativeSize,
                              EEClassLayoutInfo, VERBOSE_TYPES );
        DisplayWriteFieldInt( m_cbManagedSize, eecli->m_cbManagedSize,
                              EEClassLayoutInfo, VERBOSE_TYPES );
        DisplayWriteFieldInt( m_ManagedLargestAlignmentRequirementOfAllMembers,
                              eecli->m_ManagedLargestAlignmentRequirementOfAllMembers,
                              EEClassLayoutInfo, VERBOSE_TYPES );
        DisplayWriteFieldEnumerated( m_bFlags, eecli->m_bFlags,
                                     EEClassLayoutInfo, s_EECLIFlags, W(", "),
                                     VERBOSE_TYPES );
        DisplayWriteFieldInt( m_numCTMFields, eecli->m_numCTMFields,
                              EEClassLayoutInfo, VERBOSE_TYPES );
        PTR_NativeFieldDescriptor fmArray = eecli->GetNativeFieldDescriptors();
        DisplayWriteFieldAddress( m_pNativeFieldDescriptors,
                                  DPtrToPreferredAddr(fmArray),
                                  eecli->m_numCTMFields
                                  * sizeof(NativeFieldDescriptor),
                                  EEClassLayoutInfo, VERBOSE_TYPES );
        /* REVISIT_TODO Wed 03/22/2006
         * Dump the various types of NativeFieldDescriptors.
         */
#if 0
        DisplayStartArrayWithOffset( m_pNativeFieldDescriptors, NULL,
                                     EEClassLayoutInfo, VERBOSE_TYPES );
        for( unsigned i = 0; i < eecli->m_numCTMFields; ++i )
        {
            /* REVISIT_TODO Wed 03/22/2006
             * Try to display the type of the field marshaler in the future.
             */
            PTR_NativeFieldDescriptor current = fmArray + i;
            DisplayStartStructure( "NativeFieldDescriptor",
                                   DPtrToPreferredAddr(current),
                                   sizeof(*current), VERBOSE_TYPES );
            WriteFieldFieldDesc( m_pFD, PTR_FieldDesc(TO_TADDR(current->m_pFD)),
                                 NativeFieldDescriptor, VERBOSE_TYPES );
            DisplayWriteFieldInt( m_offset,
                                  current->m_offset, NativeFieldDescriptor,
                                  VERBOSE_TYPES );
            DisplayEndStructure( VERBOSE_TYPES ); //FieldMarshaler
        }

        DisplayEndArray( "Number of NativeFieldDescriptors", VERBOSE_TYPES ); //m_pNativeFieldDescriptors
#endif

        DisplayEndStructure( EECLASSES ); //LayoutInfo

        DisplayEndVStructure( EECLASSES ); //LayoutEEClass
    }
    else if( mt->IsArray() )
    {
        PTR_ArrayClass arrayClass(PTR_TO_TADDR(clazz));
        DisplayStartVStructure( "ArrayClass", EECLASSES);
        IF_OPT(EECLASSES)
        {
            m_display->WriteFieldInt( "m_rank", offsetof(ArrayClass, m_rank),
                                      fieldsize(ArrayClass, m_rank),
                                      arrayClass->GetRank() );
        }
        DoWriteFieldCorElementType( "m_ElementType",
                                    offsetof(ArrayClass, m_ElementType),
                                    fieldsize(ArrayClass, m_ElementType),
                                    arrayClass->GetArrayElementType() );

        DisplayEndVStructure( EECLASSES ); //ArrayClass
    }
    else if( clazz->IsDelegate() )
    {
        PTR_DelegateEEClass delegateClass(PTR_TO_TADDR(clazz));
        DisplayStartVStructure( "DelegateEEClass", EECLASSES );

        DumpFieldStub( m_pStaticCallStub, delegateClass->m_pStaticCallStub,
                       DelegateEEClass, EECLASSES );
        DumpFieldStub( m_pInstRetBuffCallStub,
                       delegateClass->m_pInstRetBuffCallStub,
                       DelegateEEClass, EECLASSES );

        WriteFieldMethodDesc( m_pInvokeMethod,
                              delegateClass->GetInvokeMethod(),
                              DelegateEEClass, EECLASSES );
        DumpFieldStub( m_pMultiCastInvokeStub,
                       delegateClass->m_pMultiCastInvokeStub,
                       DelegateEEClass, EECLASSES );

        DPTR(UMThunkMarshInfo)
            umInfo(TO_TADDR(delegateClass->m_pUMThunkMarshInfo));

        if( umInfo == NULL )
        {
            DisplayWriteFieldPointer( m_pUMThunkMarshInfo, NULL,
                                      DelegateEEClass, EECLASSES );
        }
        else
        {
            DisplayStartStructureWithOffset( m_pUMThunkMarshInfo,
                                             DPtrToPreferredAddr(umInfo),
                                             sizeof(*umInfo),
                                             DelegateEEClass, EECLASSES );
            /* REVISIT_TODO Fri 10/14/2005
             * DumpUMThunkMarshInfo
             */
            DisplayEndStructure( EECLASSES ); //UMThunkMarshInfo
        }

        WriteFieldMethodDesc( m_pBeginInvokeMethod,
                              delegateClass->GetBeginInvokeMethod(),
                              DelegateEEClass, EECLASSES );
        WriteFieldMethodDesc( m_pEndInvokeMethod,
                              delegateClass->GetEndInvokeMethod(),
                              DelegateEEClass, EECLASSES );
        DisplayWriteFieldPointer( m_pMarshalStub, delegateClass->m_pMarshalStub,
                       DelegateEEClass, EECLASSES );

        WriteFieldMethodDesc( m_pForwardStubMD,
                              PTR_MethodDesc(TO_TADDR(delegateClass->m_pForwardStubMD)),
                              DelegateEEClass, EECLASSES );
        WriteFieldMethodDesc( m_pReverseStubMD,
                              PTR_MethodDesc(TO_TADDR(delegateClass->m_pReverseStubMD)),
                              DelegateEEClass, EECLASSES );

#ifdef FEATURE_COMINTEROP
        DPTR(ComPlusCallInfo) compluscall((TADDR)delegateClass->m_pComPlusCallInfo);
        if (compluscall == NULL)
        {
            DisplayWriteFieldPointer( m_pComPlusCallInfo,
                                      NULL,
                                      DelegateEEClass,
                                      EECLASSES );
        }
        else
        {
            DumpComPlusCallInfo( compluscall, EECLASSES );
        }
#endif // FEATURE_COMINTEROP

        DisplayEndVStructure( EECLASSES ); //DelegateEEClass
    }

    DisplayEndStructure( EECLASSES ); //eeClassType

    PTR_EEClassOptionalFields pClassOptional = clazz->GetOptionalFields();
    if (pClassOptional)
    {
        DisplayStartStructure( "EEClassOptionalFields", DPtrToPreferredAddr(pClassOptional), sizeof(EEClassOptionalFields),
                               EECLASSES );

#ifdef FEATURE_COMINTEROP
        PTR_SparseVTableMap sparseVTMap(TO_TADDR(pClassOptional->m_pSparseVTableMap));
        if( sparseVTMap == NULL )
        {
            DisplayWriteFieldPointer( m_pSparseVTableMap, NULL, EEClassOptionalFields,
                                      EECLASSES );
        }
        else
        {
            _ASSERTE( !"Untested code" );
            IF_OPT(EECLASSES)
            {
                m_display->StartStructure( "m_SparseVTableMap",
                                           DPtrToPreferredAddr(sparseVTMap),
                                           sizeof(*sparseVTMap) );
            }
            _ASSERTE(sparseVTMap->m_MapList != NULL);
            PTR_SparseVTableMap_Entry mapList(TO_TADDR(sparseVTMap->m_MapList));
            DisplayStartArray( "m_MapList", NULL, EECLASSES );
            for( WORD i = 0; i < sparseVTMap->m_MapEntries; ++i )
            {
                DisplayWriteFieldInt( m_Start, mapList[i].m_Start,
                                      SparseVTableMap::Entry, EECLASSES );
                DisplayWriteFieldInt( m_Span, mapList[i].m_Span,
                                      SparseVTableMap::Entry, EECLASSES );
                DisplayWriteFieldInt( m_Span, mapList[i].m_MapTo,
                                      SparseVTableMap::Entry, EECLASSES );
            }

            DisplayEndArray( "Total Entries", EECLASSES ); //m_MapList

            DisplayWriteFieldInt( m_MapEntries, sparseVTMap->m_MapEntries,
                                  SparseVTableMap, EECLASSES );
            DisplayWriteFieldInt( m_Allocated, sparseVTMap->m_Allocated,
                                  SparseVTableMap, EECLASSES );
            DisplayWriteFieldInt( m_LastUsed, sparseVTMap->m_LastUsed,
                                  SparseVTableMap, EECLASSES );
            DisplayWriteFieldInt( m_VTSlot, sparseVTMap->m_VTSlot,
                                  SparseVTableMap, EECLASSES );
            DisplayWriteFieldInt( m_MTSlot, sparseVTMap->m_MTSlot,
                                  SparseVTableMap, EECLASSES );

            DisplayEndStructure( EECLASSES ); //SparseVTableMap
        }

        WriteFieldTypeHandle( m_pCoClassForIntf, pClassOptional->m_pCoClassForIntf,
                              EEClassOptionalFields, EECLASSES );

        PTR_ClassFactoryBase classFactory(TO_TADDR(pClassOptional->m_pClassFactory));
        if( classFactory != NULL )
        {
            DisplayWriteFieldPointer( m_pClassFactory, NULL, EEClassOptionalFields,
                                      EECLASSES );
        }
        else
        {
            /* REVISIT_TODO Fri 10/14/2005
             * Dump ComClassFactory
             */
            DisplayWriteFieldPointer( m_pClassFactory,
                                      DPtrToPreferredAddr(classFactory),
                                      EEClassOptionalFields, EECLASSES );
        }
#endif // FEATURE_COMINTEROP

        PTR_DictionaryLayout layout = pClassOptional->m_pDictLayout;
        if( layout == NULL )
        {
            DisplayWriteFieldPointer( m_pDictLayout, NULL, EEClassOptionalFields, EECLASSES );
        }
        else
        {
            IF_OPT(VERBOSE_TYPES)
            {
                WriteFieldDictionaryLayout( "m_pDictLayout",
                                            offsetof(EEClassOptionalFields, m_pDictLayout),
                                            fieldsize(EEClassOptionalFields, m_pDictLayout),
                                            layout, GetDependencyFromMT(mt)->pImport );
            }
            else
            {
                while( layout != NULL )
                {
                    CoverageRead( PTR_TO_TADDR(layout),
                                  sizeof(DictionaryLayout)
                                  + sizeof(DictionaryEntryLayout)
                                  * (layout->m_numSlots - 1) );
                    layout = PTR_DictionaryLayout(TO_TADDR(layout->m_pNext));
                }
            }
        }
        PTR_BYTE varianceInfo = pClassOptional->GetVarianceInfo();
        if( varianceInfo == NULL )
        {
            DisplayWriteFieldPointer( m_pVarianceInfo, NULL,
                                      EEClassOptionalFields, EECLASSES );
        }
        else
        {
            /* REVISIT_TODO Fri 10/14/2005
             * Dump variance info
             */
            DisplayWriteFieldPointer( m_pVarianceInfo,
                                      DPtrToPreferredAddr(varianceInfo), EEClassOptionalFields,
                                      EECLASSES );
        }

        DisplayWriteFieldInt( m_cbModuleDynamicID, pClassOptional->m_cbModuleDynamicID,
                              EEClassOptionalFields, EECLASSES );

        DisplayEndStructure( EECLASSES ); // EEClassOptionalFields
    }
} // NativeImageDumper::DumpEEClassForMethodTable
#ifdef _PREFAST_
#pragma warning(pop)
#endif

enum TypeDescType
{
    TDT_IsTypeDesc,
    TDT_IsParamTypeDesc,
    TDT_IsTypeVarTypeDesc,
    TDT_IsFnPtrTypeDesc
};
const char * const g_typeDescTypeNames[] =
{
    "TypeDesc",
    "ParamTypeDesc",
    "TypeVarTypeDesc",
    "FnPtrTypeDesc"
};
int g_typeDescSizes[] =
{
    sizeof(TypeDesc),
    sizeof(ParamTypeDesc),
    sizeof(TypeVarTypeDesc),
    -1//sizeof(FnPtrTypeDesc) -- variable size
};
TypeDescType getTypeDescType( PTR_TypeDesc td )
{
    _ASSERTE(td != NULL);
    if( td->HasTypeParam() )
        return TDT_IsParamTypeDesc;
    if( td->IsGenericVariable() )
        return TDT_IsTypeVarTypeDesc;
    if( td->GetInternalCorElementType() == ELEMENT_TYPE_FNPTR )
        return TDT_IsFnPtrTypeDesc;
    return TDT_IsTypeDesc;
}
NativeImageDumper::EnumMnemonics NativeImageDumper::s_TDFlags[] =
{

#define TDF_ENTRY(x) NativeImageDumper::EnumMnemonics(TypeDesc:: x, W(#x) )
        TDF_ENTRY(enum_flag_NeedsRestore),
        TDF_ENTRY(enum_flag_PreRestored),
        TDF_ENTRY(enum_flag_Unrestored),
        TDF_ENTRY(enum_flag_UnrestoredTypeKey),
        TDF_ENTRY(enum_flag_IsNotFullyLoaded),
        TDF_ENTRY(enum_flag_DependenciesLoaded),
#undef TDF_ENTRY
};

NativeImageDumper::EnumMnemonics s_CConv[] =
{
#define CC_ENTRY(x) NativeImageDumper::EnumMnemonics( x, W(#x) )

#define CC_CALLCONV_ENTRY(x) NativeImageDumper::EnumMnemonics( x, IMAGE_CEE_CS_CALLCONV_MASK, W(#x) )
    CC_CALLCONV_ENTRY(IMAGE_CEE_CS_CALLCONV_VARARG),
    CC_CALLCONV_ENTRY(IMAGE_CEE_CS_CALLCONV_FIELD),
    CC_CALLCONV_ENTRY(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG),
    CC_CALLCONV_ENTRY(IMAGE_CEE_CS_CALLCONV_PROPERTY),
    CC_CALLCONV_ENTRY(IMAGE_CEE_CS_CALLCONV_UNMANAGED),
    CC_CALLCONV_ENTRY(IMAGE_CEE_CS_CALLCONV_GENERICINST),
    CC_CALLCONV_ENTRY(IMAGE_CEE_CS_CALLCONV_NATIVEVARARG),
#undef CC_CALLCONV_ENTRY

    CC_ENTRY(IMAGE_CEE_CS_CALLCONV_HASTHIS),
    CC_ENTRY(IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS),
    CC_ENTRY(IMAGE_CEE_CS_CALLCONV_GENERIC)
};


void NativeImageDumper::DumpTypeDesc( PTR_TypeDesc td )
{
    _ASSERTE(CHECK_OPT(TYPEDESCS));
    TypeDescType tdt = getTypeDescType(td);
    int size = g_typeDescSizes[(int)tdt];
    if( size == -1 )
    {
        _ASSERTE(tdt == TDT_IsFnPtrTypeDesc);
        size = FnPtrTypeDesc::DacSize(PTR_TO_TADDR(td));
    }
    DisplayStartStructure( g_typeDescTypeNames[(int)tdt],
                           DPtrToPreferredAddr(td), size, TYPEDESCS );

    //first handle the fields of typedesc
    WriteFieldCorElementType( m_typeAndFlags, td->GetInternalCorElementType(),
                              TypeDesc, TYPEDESCS );
    DisplayWriteFieldEnumerated( m_typeAndFlags, td->m_typeAndFlags, TypeDesc,
                                 s_TDFlags, W(", "), TYPEDESCS );
    if( tdt == TDT_IsParamTypeDesc )
    {
        PTR_ParamTypeDesc ptd(td);
        DisplayStartVStructure( "ParamTypeDesc", TYPEDESCS );
        WriteFieldMethodTable( m_TemplateMT, ptd->GetTemplateMethodTableInternal(),
                               ParamTypeDesc, TYPEDESCS );
        WriteFieldTypeHandle( m_Arg, ptd->m_Arg,
                              ParamTypeDesc, TYPEDESCS );
        DisplayWriteFieldPointer( m_hExposedClassObject,
                                  DataPtrToDisplay(ptd->m_hExposedClassObject),
                                  ParamTypeDesc, TYPEDESCS );

        DisplayEndVStructure( TYPEDESCS ); //ParamTypeDesc
    }
    else if( tdt == TDT_IsFnPtrTypeDesc )
    {
        PTR_FnPtrTypeDesc ftd(td);
        DisplayStartVStructure( "FnPtrTypeDesc", TYPEDESCS );
        DisplayWriteFieldInt( m_NumArgs, ftd->m_NumArgs, FnPtrTypeDesc,
                              TYPEDESCS );
        DisplayWriteFieldEnumerated( m_CallConv, ftd->m_CallConv,
                                     FnPtrTypeDesc, s_CConv, W(", "),
                                     TYPEDESCS );
        DisplayStartArrayWithOffset( m_RetAndArgTypes, W("[%-4s]: %s"),
                                     FnPtrTypeDesc, TYPEDESCS );
        PTR_TypeHandle args( PTR_HOST_MEMBER_TADDR(FnPtrTypeDesc, ftd,
                                                   m_RetAndArgTypes) );
        for( unsigned i = 0; i < ftd->m_NumArgs; ++i )
        {
            DisplayStartElement( "Argument", TYPEDESCS );
            DisplayWriteElementInt( "Index", i, TYPEDESCS );
            IF_OPT( TYPEDESCS )
                WriteElementTypeHandle( "TypeHandle", args[i] );
            DisplayEndElement( TYPEDESCS );
        }
        DisplayEndArray( "Total Arguments", TYPEDESCS );
        DisplayEndVStructure( TYPEDESCS );
    }
    else if( tdt == TDT_IsTypeVarTypeDesc )
    {
        PTR_TypeVarTypeDesc tvtd(td);
        DisplayStartVStructure( "TypeVarTypeDesc", TYPEDESCS );
        DisplayWriteFieldPointer( m_pModule,
                                  DPtrToPreferredAddr(tvtd->GetModule()),
                                  TypeVarTypeDesc, TYPEDESCS );
        DisplayWriteFieldUInt( m_typeOrMethodDef,
                               tvtd->m_typeOrMethodDef,
                               TypeVarTypeDesc, TYPEDESCS );
        DisplayWriteFieldInt( m_numConstraints, tvtd->m_numConstraints,
                              TypeVarTypeDesc, TYPEDESCS );
        if( tvtd->m_constraints == NULL )
        {
            DisplayWriteFieldPointer( m_constraints, NULL, TypeVarTypeDesc,
                                      TYPEDESCS );
        }
        else
        {
            DisplayStartStructureWithOffset( m_constraints,
                                             DPtrToPreferredAddr(tvtd->m_constraints),
                                             sizeof(*tvtd->m_constraints) *
                                             tvtd->m_numConstraints,
                                             TypeVarTypeDesc, TYPEDESCS );
            DisplayStartArray( "Constraints", NULL, TYPEDESCS );
            for( unsigned i = 0; i < tvtd->m_numConstraints; ++i )
            {
                WriteElementTypeHandle( "TypeHandle", tvtd->m_constraints[i] );
            }
            DisplayEndArray( "Total Constraints", TYPEDESCS ); //Constraints
            DisplayEndStructure( TYPEDESCS ); //m_constraints
        }
        DisplayWriteFieldPointer( m_hExposedClassObject,
                                  DataPtrToDisplay(tvtd->m_hExposedClassObject),
                                  TypeVarTypeDesc, TYPEDESCS );
        DisplayWriteFieldUInt( m_token, tvtd->m_token, TypeVarTypeDesc,
                               TYPEDESCS );
        DisplayWriteFieldInt( m_index, tvtd->m_index, TypeVarTypeDesc,
                              TYPEDESCS );

        DisplayEndVStructure( TYPEDESCS ); //TypeVarTypeDesc
    }


    DisplayEndStructure( TYPEDESCS ); // g_typeDescTypeNames

}

void NativeImageDumper::DumpDictionaryEntry( const char * elementName,
                                             DictionaryEntryKind kind,
                                             PTR_DictionaryEntry entry )
{
    m_display->StartElement( elementName );
    const char * name = NULL;
    switch(kind)
    {
    case EmptySlot:
        m_display->WriteEmptyElement("EmptySlot");
        break;
    case TypeHandleSlot:
        {
            TypeHandle th = dac_cast<DPTR(FixupPointer<TypeHandle>)>(entry)->GetValue();
            WriteElementTypeHandle( "TypeHandle", th );
            /* XXX Fri 03/24/2006
             * There is no straightforward home for these, so make sure to
             * record them
             */
            if( !CORCOMPILE_IS_POINTER_TAGGED(th.AsTAddr()) && th.IsTypeDesc() )
            {
                PTR_TypeDesc td(th.AsTypeDesc());
                if( isInRange(PTR_TO_TADDR(td)) )
                {
                    m_discoveredTypeDescs.AppendEx(td);
                }
            }
        }
        break;
    case MethodDescSlot:
        {
            TempBuffer buf;
            PTR_MethodDesc md(TO_TADDR(*entry));
            WriteElementMethodDesc( "MethodDesc", md );
        }
        break;
    case MethodEntrySlot:
        name = "MethodEntry";
        goto StandardEntryDisplay;
    case ConstrainedMethodEntrySlot:
        name = "ConstrainedMethodEntry";
        goto StandardEntryDisplay;
    case DispatchStubAddrSlot:
        name = "DispatchStubAddr";
        goto StandardEntryDisplay;
        /* REVISIT_TODO Tue 10/11/2005
         * Print out name information here
         */
    case FieldDescSlot:
        name = "FieldDescSlot";
StandardEntryDisplay:
        m_display->WriteElementPointer(name, DataPtrToDisplay((TADDR)*entry));
        break;
    default:
        _ASSERTE( !"unreachable" );
    }
    m_display->EndElement(); //elementName
}

#ifdef FEATURE_READYTORUN
IMAGE_DATA_DIRECTORY * NativeImageDumper::FindReadyToRunSection(ReadyToRunSectionType type)
{
    PTR_READYTORUN_SECTION pSections = dac_cast<PTR_READYTORUN_SECTION>(dac_cast<TADDR>(m_pReadyToRunHeader) + sizeof(READYTORUN_HEADER));
    for (DWORD i = 0; i < m_pReadyToRunHeader->NumberOfSections; i++)
    {
        // Verify that section types are sorted
        _ASSERTE(i == 0 || (pSections[i - 1].Type < pSections[i].Type));

        READYTORUN_SECTION * pSection = pSections + i;
        if (pSection->Type == type)
            return &pSection->Section;
    }
    return NULL;
}

//
// Ready to Run specific dumping methods
//
void NativeImageDumper::DumpReadyToRun()
{
    m_pReadyToRunHeader = m_decoder.GetReadyToRunHeader();

    m_nativeReader = NativeFormat::NativeReader(dac_cast<PTR_BYTE>(m_decoder.GetBase()), m_decoder.GetVirtualSize());

    IMAGE_DATA_DIRECTORY * pRuntimeFunctionsDir = FindReadyToRunSection(ReadyToRunSectionType::RuntimeFunctions);
    if (pRuntimeFunctionsDir != NULL)
    {
        m_pRuntimeFunctions = dac_cast<PTR_RUNTIME_FUNCTION>(m_decoder.GetDirectoryData(pRuntimeFunctionsDir));
        m_nRuntimeFunctions = pRuntimeFunctionsDir->Size / sizeof(T_RUNTIME_FUNCTION);
    }
    else
    {
        m_nRuntimeFunctions = 0;
    }

    IMAGE_DATA_DIRECTORY * pEntryPointsDir = FindReadyToRunSection(ReadyToRunSectionType::MethodDefEntryPoints);
    if (pEntryPointsDir != NULL)
        m_methodDefEntryPoints = NativeFormat::NativeArray((TADDR)&m_nativeReader, pEntryPointsDir->VirtualAddress);

    DisplayStartCategory("NativeInfo", NATIVE_INFO);

    IF_OPT(NATIVE_INFO)
        DumpReadyToRunHeader();

    DisplayEndCategory(NATIVE_INFO); //NativeInfo

    IF_OPT_OR3(METHODS, GC_INFO, DISASSEMBLE_CODE)
        DumpReadyToRunMethods();

    IF_OPT(RELOCATIONS)
        DumpBaseRelocs();
}

const NativeImageDumper::EnumMnemonics s_ReadyToRunFlags[] =
{
#define RTR_FLAGS(f) NativeImageDumper::EnumMnemonics(f, W(#f))
    RTR_FLAGS(READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE),
#undef RTR_FLAGS
};

void NativeImageDumper::DumpReadyToRunHeader()
{
    IF_OPT(NATIVE_INFO)
    {
        m_display->StartStructure( "READYTORUN_HEADER",
                                   DPtrToPreferredAddr(dac_cast<PTR_READYTORUN_HEADER>(m_pReadyToRunHeader)),
                                   sizeof(*m_pReadyToRunHeader) );

        DisplayWriteFieldUInt( Signature, m_pReadyToRunHeader->Signature, READYTORUN_HEADER, ALWAYS );
        DisplayWriteFieldUInt( MajorVersion, m_pReadyToRunHeader->MajorVersion, READYTORUN_HEADER, ALWAYS );
        DisplayWriteFieldUInt( MinorVersion, m_pReadyToRunHeader->MinorVersion, READYTORUN_HEADER, ALWAYS );

        DisplayWriteFieldEnumerated( Flags, m_pReadyToRunHeader->Flags,
                                     READYTORUN_HEADER, s_ReadyToRunFlags, W(", "),
                                     NATIVE_INFO );

        m_display->EndStructure(); //READYTORUN_HEADER
    }
}

void NativeImageDumper::DumpReadyToRunMethods()
{
    DisplayStartArray("Methods", NULL, METHODS);

    for (uint rid = 1; rid <= m_methodDefEntryPoints.GetCount(); rid++)
    {
        uint offset;
        if (!m_methodDefEntryPoints.TryGetAt(rid - 1, &offset))
            continue;

        uint id;
        offset = m_nativeReader.DecodeUnsigned(offset, &id);

        if (id & 1)
        {
            if (id & 2)
            {
                uint val;
                m_nativeReader.DecodeUnsigned(offset, &val);
                offset -= val;
            }

            // TODO: Dump fixups from dac_cast<TADDR>(m_pLayout->GetBase()) + offset

            id >>= 2;
        }
        else
        {
            id >>= 1;
        }

        _ASSERTE(id < m_nRuntimeFunctions);
        PTR_RUNTIME_FUNCTION pRuntimeFunction = m_pRuntimeFunctions + id;
        PCODE pEntryPoint = dac_cast<TADDR>(m_decoder.GetBase()) + pRuntimeFunction->BeginAddress;

        SString buf;
        AppendTokenName(TokenFromRid(rid, mdtMethodDef), buf, m_import);

        DumpReadyToRunMethod(pEntryPoint, pRuntimeFunction, buf);
    }

    DisplayEndArray("Total Methods", METHODS); //Methods
}

extern PTR_VOID GetUnwindDataBlob(TADDR moduleBase, PTR_RUNTIME_FUNCTION pRuntimeFunction, /* out */ SIZE_T * pSize);

void NativeImageDumper::DumpReadyToRunMethod(PCODE pEntryPoint, PTR_RUNTIME_FUNCTION pRuntimeFunction, SString& name)
{
    //Read the GCInfo to get the total method size.
    unsigned methodSize = 0;
    unsigned gcInfoSize = UINT_MAX;

    SIZE_T nUnwindDataSize;
    PTR_VOID pUnwindData = GetUnwindDataBlob(dac_cast<TADDR>(m_decoder.GetBase()), pRuntimeFunction, &nUnwindDataSize);

    // GCInfo immediatelly follows unwind data
    PTR_CBYTE gcInfo = dac_cast<PTR_CBYTE>(pUnwindData) + nUnwindDataSize;

    void(*stringOutFn)(const char *, ...);
    IF_OPT(GC_INFO)
    {
        stringOutFn = stringOut;
    }
    else
    {
        stringOutFn = nullStringOut;
    }
    if (gcInfo != NULL)
    {
        PTR_CBYTE curGCInfoPtr = gcInfo;
        g_holdStringOutData.Clear();
        GCDump gcDump(GCINFO_VERSION);
        gcDump.gcPrintf = stringOutFn;
        UINT32 r2rversion = m_pReadyToRunHeader->MajorVersion;
        UINT32 gcInfoVersion = GCInfoToken::ReadyToRunVersionToGcInfoVersion(r2rversion);
        GCInfoToken gcInfoToken = { curGCInfoPtr, gcInfoVersion };

#if !defined(TARGET_X86) && defined(USE_GC_INFO_DECODER)
        GcInfoDecoder gcInfoDecoder(gcInfoToken, DECODE_CODE_LENGTH);
        methodSize = gcInfoDecoder.GetCodeLength();
#endif

        //dump the data to a string first so we can get the gcinfo size.
#ifdef TARGET_X86
        InfoHdr hdr;
        stringOutFn("method info Block:\n");
        curGCInfoPtr += gcDump.DumpInfoHdr(curGCInfoPtr, &hdr, &methodSize, 0);
        stringOutFn("\n");
#endif

        IF_OPT(METHODS)
        {
#ifdef TARGET_X86
            stringOutFn("PointerTable:\n");
            curGCInfoPtr += gcDump.DumpGCTable(curGCInfoPtr,
                hdr,
                methodSize, 0);
            gcInfoSize = curGCInfoPtr - gcInfo;
#elif defined(USE_GC_INFO_DECODER)
            stringOutFn("PointerTable:\n");
            curGCInfoPtr += gcDump.DumpGCTable(curGCInfoPtr,
                methodSize, 0);
            gcInfoSize = (unsigned)(curGCInfoPtr - gcInfo);
#endif
        }

        //data is output below.
    }

    DisplayStartElement("Method", METHODS);
    DisplayWriteElementStringW("Name", (const WCHAR *)name, METHODS);

    DisplayStartStructure("GCInfo",
        DPtrToPreferredAddr(gcInfo),
        gcInfoSize,
        METHODS);

    DisplayStartTextElement("Contents", GC_INFO);
    DisplayWriteXmlTextBlock(("%S", (const WCHAR *)g_holdStringOutData), GC_INFO);
    DisplayEndTextElement(GC_INFO); //Contents

    DisplayEndStructure(METHODS); //GCInfo

    DisplayStartStructure("Code", DataPtrToDisplay(pEntryPoint), methodSize,
        METHODS);

    IF_OPT(DISASSEMBLE_CODE)
    {
        // Disassemble hot code.  Read the code into the host process.
        /* REVISIT_TODO Mon 10/24/2005
        * Is this align up right?
        */
        BYTE * codeStartHost =
            reinterpret_cast<BYTE*>(PTR_READ(pEntryPoint,
                (ULONG32)ALIGN_UP(methodSize,
                    CODE_SIZE_ALIGN)));
        DisassembleMethod(codeStartHost, methodSize);
    }

    DisplayEndStructure(METHODS); //Code

    DisplayEndElement(METHODS); //Method
}
#endif // FEATURE_READYTORUN

#if 0
void NativeImageDumper::RecordTypeRef( mdTypeRef token, PTR_MethodTable mt )
{
    if( mt != NULL )
        m_mtToTypeRefMap.Add( mt, token );
}
mdTypeRef NativeImageDumper::FindTypeRefForMT( PTR_MethodTable mt )
{
    return m_mtToTypeRefMap.Find(mt);
}
#endif

#else //!FEATURE_PREJIT
//dummy implementation for dac
HRESULT ClrDataAccess::DumpNativeImage(CLRDATA_ADDRESS loadedBase,
    LPCWSTR name,
    IXCLRDataDisplay* display,
    IXCLRLibrarySupport* support,
    IXCLRDisassemblySupport* dis)
{
    return E_FAIL;
}
#endif //FEATURE_PREJIT

/* REVISIT_TODO Mon 10/10/2005
 * Here is where it gets bad.  There is no DAC build of gcdump, so instead
 * build it directly into the the dac.  That's what all these ugly defines
 * are all about.
 */
#ifdef __MSC_VER
#pragma warning(disable:4244)   // conversion from 'unsigned int' to 'unsigned short', possible loss of data
#pragma warning(disable:4189)   // local variable is initialized but not referenced
#endif // __MSC_VER

#undef assert
#define assert(a)
#define NOTHROW
#define GC_NOTRIGGER
#include <gcdecoder.cpp>
#undef NOTHROW
#undef GC_NOTRIGGER

#if defined _DEBUG && defined TARGET_X86
#ifdef _MSC_VER
// disable FPO for checked build
#pragma optimize("y", off)
#endif // _MSC_VER
#endif

#undef _ASSERTE
#define _ASSERTE(a) do {} while (0)
#ifdef TARGET_X86
#include <gcdump.cpp>
#endif

#undef LIMITED_METHOD_CONTRACT
#undef WRAPPER_NO_CONTRACT
#ifdef TARGET_X86
#include <i386/gcdumpx86.cpp>
#else // !TARGET_X86
#undef PREGDISPLAY
#include <gcdumpnonx86.cpp>
#endif // !TARGET_X86

#ifdef __MSC_VER
#pragma warning(default:4244)
#pragma warning(default:4189)
#endif // __MSC_VER
