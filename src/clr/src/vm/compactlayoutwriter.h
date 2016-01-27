// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

#include "corhdr.h"

#if !defined(_CompactLayoutWriter) && defined(MDIL)
#define _CompactLayoutWriter
#include "mdil.h"

class ZapImage;

struct InlineContext;

class ICompactLayoutWriter
{
public:
    // Prepare to write CTL for a type.
    virtual
    void Reset() = 0;

    virtual
    void GenericType(DWORD typeArgCount) = 0;

    enum ComFlags // additional flags on types besides CorTypeAttr
    {
        CF_COMOBJECTTYPE                    = 0x80000000,
        CF_COMCLASSINTERFACE                = 0x40000000,
        CF_COMEVENTINTERFACE                = 0x20000000,

        // com interface types
        CF_IFACEATTRMASK                    = 0x18000000,
        CF_DUAL                             = 0x00000000,
        CF_VTABLE                           = 0x08000000,
        CF_DISPATCH                         = 0x10000000,
        CF_INSPECTABLE                      = 0x18000000,

        // interfaces don't have finalizers so we can 
        // reuse the bits interfaces use to specify dispatch type
        CF_FINALIZER                        = 0x08000000,
        CF_CRITICALFINALIZER                = 0x10000000,

        // fixed value type statics
        CF_FIXED_ADDRESS_VT_STATICS         = 0x04000000,

        // type equivalence on struct parameters
        CF_DEPENDS_ON_COM_IMPORT_STRUCTS    = 0x02000000,

        // is type equivalent?
        CF_TYPE_EQUIVALENT                  = 0x01000000,

        CF_CONTAINS_STACK_PTR               = 0x00800000,

        // this type has the UnsafeValueType CA attached to it
        CF_UNSAFEVALUETYPE                  = 0x00400000,
    };

    enum SecurityFlags
    {
        SF_UNKNOWN                          = 0,
        SF_TRANSPARENT                      = 1,
        SF_ALL_TRANSPARENT                  = 2,
        SF_CRTIICAL                         = 3,
        SF_CRITICAL_TAS                     = 4,
        SF_ALLCRITICAL                      = 5,
        SF_ALLCRITICAL_TAS                  = 6,
        SF_TAS_NOTCRITICAL                  = 7,
    };

    virtual
    void StartType( DWORD  flags,							// CorTypeAttr plus perhaps other flags
                    DWORD  typeDefToken,					// typedef token for this type
                    DWORD  baseTypeToken,					// type this type is derived from, if any
                    DWORD  enclosingTypeToken,				// type this type is nested in, if any
                    DWORD  interfaceCount,					// how many times ImplementInterface() will be called
                    DWORD  fieldCount,						// how many times Field() will be called
                    DWORD  methodCount,						// how many times Method() will be called
                    DWORD  newVirtualMethodCount,			// how many new virtuals this type defines
                    DWORD  overrideVirtualMethodCount ) = 0;// how many virtuals this type overrides

    // Call once for each interface implemented by the
    // class directly (not those implemented in base classes)
    virtual
    void ImplementInterface( DWORD interfaceTypeToken ) = 0;


    virtual
    void ExtendedTypeFlags( DWORD flags ) = 0;

    virtual
    void SpecialType( SPECIAL_TYPE type) = 0;

    virtual 
    enum FieldStorage
    {
        FS_INSTANCE,
        FS_STATIC,
        FS_THREADLOCAL,
        FS_CONTEXTLOCAL,
        FS_RVA
    };

    enum FieldProtection // parallels CorFieldAttr
    {
        FP_PRIVATE_SCOPE     = 0x0,     // Member not referenceable.
        FP_PRIVATE           = 0x1,     // Accessible only by the parent type.
        FP_FAM_AND_ASSEM     = 0x2,     // Accessible by sub-types only in this Assembly.
        FP_ASSEMBLY          = 0x3,     // Accessibly by anyone in the Assembly.
        FP_FAMILY            = 0x4,     // Accessible only by type and sub-types.
        FP_FAM_OR_ASSEM      = 0x5,     // Accessibly by sub-types anywhere, plus anyone in assembly.
        FP_PUBLIC            = 0x6,     // Accessibly by anyone who has visibility to this scope.
    };

    // Call once for each field the class declares directly
    // valueTypeToken is non-0 iff fieldType == ELEMENT_TYPE_VALUETYPE
    // not all CorElementTypes are allowed
    virtual
    void Field( DWORD           fieldToken,		// an mdFieldDef
                FieldStorage    fieldStorage,
                FieldProtection fieldProtection,
                CorElementType  fieldType,
                DWORD			fieldOffset,
                DWORD           valueTypeToken) = 0;

    // call once for each method implementing a contract
    // in an interface.
    // declToken is the token of the method that is implemented
    // implToken is the method body that implements it
    virtual
    void ImplementInterfaceMethod(DWORD declToken,
                                  DWORD implToken) = 0;

    enum ImplHints
    {
        IH_CTOR                         = 0x0010,
        IH_DEFAULT_CTOR                 = 0x0020,   // this one will have IH_CTOR set also
        IH_CCTOR                        = 0x0040,

        IH_DELEGATE_INVOKE              = 0x0080,   // this the Invoke method of a delegate
        IH_DELEGATE_BEGIN_INVOKE        = 0x0090,   // this the BeginInvoke method of a delegate
        IH_DELEGATE_END_INVOKE          = 0x00A0,   // this the EndInvoke method of a delegate

        IH_TRANSPARENCY_MASK            = 0x0C00,
        IH_TRANSPARENCY_NO_INFO         = 0x0000,
        IH_TRANSPARENCY_TRANSPARENT     = 0x0400,
        IH_TRANSPARENCY_CRITICAL        = 0x0800,
        IH_TRANSPARENCY_TREAT_AS_SAFE   = 0x0C00,

        IH_BY_ORDINAL                   = 0x1000,   // this applies only to DllImport and DllExport methods 
        IH_IS_VERIFIED                  = 0x2000,   // IH_IS_VERIFIED and IH_IS_VERIFIABLE match
        IH_IS_VERIFIABLE                = 0x4000,   // IsVerified and IsVerifiable on the MethodDesc

        IH_HASMETHODIMPL                = 0x8000,   // Method overrides a (non-interface) method via MethodImpl,
                                                    // which will be reported via MethodImpl lateron
    };

    enum StubMethodFlags
    {
        SF_PINVOKE                   = 0,
        SF_DELEGATE_PINVOKE          = 1,
        SF_CALLI_PINVOKE             = 2,
        SF_REVERSE_PINVOKE           = 3,
        SF_CLR_TO_COM                = 4,
        SF_COM_TO_CLR                = 5,
        SF_STUB_KIND_MASK            = 0x0000000f,

        SF_HAS_COPY_CONSTRUCTED_ARGS = 0x00000010,
        SF_NEEDS_STUB_SIGNATURE      = 0x00000020,

        SF_STACK_ARG_SIZE_MASK       = 0xFFFC0000,
    };

    enum CorElementTypeMDIL
    {
        ELEMENT_TYPE_NATIVE_VALUETYPE = 0x08 | ELEMENT_TYPE_MODIFIER
    };

    // call once for each method except PInvoke methods
    virtual
    void Method(DWORD methodAttrs,
                DWORD implFlags,
                DWORD implHints, // have to figure how exactly we do this so it's not so tied to the CLR implementation
                DWORD methodToken,
                DWORD overriddenMethodToken) = 0;

    // call once for each PInvoke method
    virtual
    void PInvokeMethod( DWORD methodAttrs,
                        DWORD implFlags,
                        DWORD implHints, // have to figure how exactly we do this so it's not so tied to the CLR implementation
                        DWORD methodToken,
                        LPCSTR moduleName,
                        LPCSTR entryPointName,
                        WORD wLinkFlags) = 0;
    
    // call once for each DllExport method (Redhawk only feature, at least for now)
    virtual
    void DllExportMethod( DWORD methodAttrs,
                          DWORD implFlags,
                          DWORD implHints, // have to figure how exactly we do this so it's not so tied to the CLR implementation
                          DWORD methodToken,
                          LPCSTR entryPointName,
                          DWORD callingConvention) = 0;
    
    virtual
    void StubMethod( DWORD dwMethodFlags,
                     DWORD sigToken,
                     DWORD methodToken) = 0;

    virtual
    void StubAssociation( DWORD ownerToken,
                          DWORD *stubTokens,
                          size_t numStubs) = 0;

    // call once for each method impl
    virtual
    void MethodImpl(DWORD declToken,
                    DWORD implToken ) = 0;

    // set an explicit size for explicit layout structs
    virtual
    void SizeType(DWORD size) = 0;

    // specify the packing size
    virtual
    void PackType(DWORD packingSize) = 0;

    // specify a generic parameter to a type or method
    virtual
    void GenericParameter(DWORD genericParamToken, DWORD flags) = 0;

    enum NativeFlags
    {
        NF_BESTFITMAP             = 0x01,
        NF_THROWONUNMAPPABLECHAR  = 0x02,

        // sometimes we store a VARTYPE in the upper bits - this is how much to shift
        NF_VARTYPE_SHIFT          = 8,
    };

    // specify a field representation on the native side
    virtual
    void NativeField(DWORD            fieldToken,		// an mdFieldDef
                     DWORD            nativeType,       // really an NStructFieldType
                     DWORD			  nativeOffset,
                     DWORD            count,
                     DWORD            flags,
                     DWORD            typeToken1,
                     DWORD            typeToken2) = 0;

    // specify guid info for interface types
    virtual
    void GuidInformation(GuidInfo *guidInfo) = 0;

    // end the description of the type
    virtual
    void EndType() = 0;

    // return a token for a method desc
    // this is trivial in the case of a non-generic method in our own module, not so trivial
    // otherwise - we may have to generate new moduleref, typeref, typespec, methodspec tokens
    virtual
    mdMemberRef GetTokenForMethodDesc(MethodDesc *methodDesc, MethodTable *pMT = NULL) = 0;

    virtual
    mdTypeSpec GetTypeSpecToken(PCCOR_SIGNATURE pSig, DWORD cbSig) = 0;

    virtual
    mdToken GetTokenForType(MethodTable *pMT) = 0;

    virtual
    mdToken GetTokenForType(CORINFO_CLASS_HANDLE type) = 0;

    virtual 
    mdToken GetTokenForType(InlineContext *inlineContext, CORINFO_ARG_LIST_HANDLE argList) = 0;

    virtual
    mdToken GetTokenForMethod(CORINFO_METHOD_HANDLE method) = 0;

    virtual
    mdToken GetTokenForField(CORINFO_FIELD_HANDLE field) = 0;

    virtual
    mdToken GetTokenForSignature(PCCOR_SIGNATURE sig) = 0;

    virtual 
    mdToken GetTypeTokenForFieldOrMethod(mdToken fieldOrMethodToken) = 0;

    virtual
    mdToken GetEnclosingClassToken(InlineContext *inlineContext, CORINFO_METHOD_HANDLE methHnd) = 0;

    virtual
    InlineContext *ComputeInlineContext(InlineContext *outerContext, unsigned inlinedMethodToken, unsigned constraintTypeToken, CORINFO_METHOD_HANDLE methHnd) = 0;

    virtual
    DWORD GetFieldOrdinal(CORINFO_MODULE_HANDLE tokenScope, unsigned fieldToken) = 0;

    virtual
    unsigned TranslateToken(InlineContext *inlineContext, unsigned token) = 0;

    virtual
    CorInfoType GetFieldElementType(unsigned fieldToken, CORINFO_MODULE_HANDLE scope, CORINFO_METHOD_HANDLE methHnd, ICorJitInfo *info) = 0;

    virtual
    mdToken GetParentOfMemberRef(CORINFO_MODULE_HANDLE scope, mdMemberRef memberRefToken) = 0;

    virtual
    mdToken GetArrayElementToken(CORINFO_MODULE_HANDLE scope, mdTypeSpec arrayTypeToken) = 0;

    virtual
    mdToken GetCurrentMethodToken(InlineContext *inlineContext, CORINFO_METHOD_HANDLE methHnd) = 0;

    virtual
    bool IsDynamicScope(CORINFO_MODULE_HANDLE scope) = 0;

    virtual
    mdToken GetNextStubToken() = 0;

    virtual
    void Flush() = 0;

    virtual
    void FlushStubData() = 0;

    static ICompactLayoutWriter *MakeCompactLayoutWriter(Module *pModule, ZapImage *pZapImage);
};

#endif
