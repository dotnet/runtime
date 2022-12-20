// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _BINDERMODULE_H_
#define _BINDERMODULE_H_

class DataImage;
class Module;
class MethodTable;
class MethodDesc;
class FieldDesc;

typedef const struct HardCodedMetaSig *LPHARDCODEDMETASIG;

// As hard-coded metasigs are constant data ordinarily it
// wouldn't be necessary to use PTR access.  However, access
// through the Binder class requires it.
typedef DPTR(const struct HardCodedMetaSig) PTR_HARDCODEDMETASIG;

struct HardCodedMetaSig
{
    const BYTE* m_pMetaSig; // metasig prefixed with INT8 length:
                            // length > 0 - resolved, length < 0 - has unresolved type references
};

#define DEFINE_METASIG(body)            extern const body
#define DEFINE_METASIG_T(body)          extern body
#define METASIG_BODY(varname, types)    HardCodedMetaSig gsig_ ## varname;
#include "metasig.h"

//
// Use the Binder objects to avoid doing unnecessary name lookup
// (esp. in the prejit case)
//
// E.g. CoreLibBinder::GetClass(CLASS__APP_DOMAIN);
//

// BinderClassIDs are of the form CLASS__XXX

enum BinderClassID
{
#define TYPEINFO(e,ns,c,s,g,ia,ip,if,im,gv)   CLASS__ ## e,
#include "cortypeinfo.h"
#undef TYPEINFO

#define DEFINE_CLASS(i,n,s)         CLASS__ ## i,
#include "corelib.h"

    CLASS__CORELIB_COUNT,

    // Aliases for element type classids
    CLASS__NIL      = CLASS__ELEMENT_TYPE_END,
    CLASS__VOID     = CLASS__ELEMENT_TYPE_VOID,
    CLASS__BOOLEAN  = CLASS__ELEMENT_TYPE_BOOLEAN,
    CLASS__CHAR     = CLASS__ELEMENT_TYPE_CHAR,
    CLASS__BYTE     = CLASS__ELEMENT_TYPE_U1,
    CLASS__SBYTE    = CLASS__ELEMENT_TYPE_I1,
    CLASS__INT16    = CLASS__ELEMENT_TYPE_I2,
    CLASS__UINT16   = CLASS__ELEMENT_TYPE_U2,
    CLASS__INT32    = CLASS__ELEMENT_TYPE_I4,
    CLASS__UINT32   = CLASS__ELEMENT_TYPE_U4,
    CLASS__INT64    = CLASS__ELEMENT_TYPE_I8,
    CLASS__UINT64   = CLASS__ELEMENT_TYPE_U8,
    CLASS__SINGLE   = CLASS__ELEMENT_TYPE_R4,
    CLASS__DOUBLE   = CLASS__ELEMENT_TYPE_R8,
    CLASS__STRING   = CLASS__ELEMENT_TYPE_STRING,
    CLASS__TYPED_REFERENCE = CLASS__ELEMENT_TYPE_TYPEDBYREF,
    CLASS__INTPTR   = CLASS__ELEMENT_TYPE_I,
    CLASS__UINTPTR  = CLASS__ELEMENT_TYPE_U,
    CLASS__OBJECT   = CLASS__ELEMENT_TYPE_OBJECT
};


// BinderMethodIDs are of the form METHOD__XXX__YYY,
// where X is the class and Y is the method

enum BinderMethodID : int
{
    METHOD__NIL = 0,

#define DEFINE_METHOD(c,i,s,g)      METHOD__ ## c ## __ ## i,
#include "corelib.h"

    METHOD__CORELIB_COUNT,
};

// BinderFieldIDs are of the form FIELD__XXX__YYY,
// where X is the class and Y is the field

enum BinderFieldID
{
    FIELD__NIL = 0,

#define DEFINE_FIELD(c,i,s)                 FIELD__ ## c ## __ ## i,
#include "corelib.h"

    FIELD__CORELIB_COUNT,
};

struct CoreLibClassDescription
{
    PTR_CSTR nameSpace;
    PTR_CSTR name;
};

struct CoreLibMethodDescription
{
    BinderClassID classID;
    PTR_CSTR name;
    PTR_HARDCODEDMETASIG sig;
};

struct CoreLibFieldDescription
{
    BinderClassID classID;
    PTR_CSTR name;
};

class CoreLibBinder
{
  public:

    //
    // Note that the frequently called methods are intentionally static to reduce code bloat.
    // Instance methods would push the address of the global object at every callsite.
    //

    static PTR_Module GetModule();

    //
    // Retrieve structures from ID.
    //
    // Note that none of the CoreLibBinder methods trigger static
    // constructors. The JITed code takes care of triggering them.
    //
    static PTR_MethodTable GetClass(BinderClassID id);
    static MethodDesc * GetMethod(BinderMethodID id);
    static FieldDesc * GetField(BinderFieldID id);

    //
    // A slightly faster version that assumes that the class was fetched
    // by the binder earlier.
    //
    static PTR_MethodTable GetExistingClass(BinderClassID id);
    static MethodDesc * GetExistingMethod(BinderMethodID id);
    static FieldDesc * GetExistingField(BinderFieldID id);

    //
    // Utilities for classes
    //
    static FORCEINLINE BOOL IsClass(MethodTable *pMT, BinderClassID id)
    {
        return dac_cast<TADDR>(GetClass(id)) == dac_cast<TADDR>(pMT);
    }

    // Get the class only if it has been loaded already
    static PTR_MethodTable GetClassIfExist(BinderClassID id);

    static LPCUTF8 GetClassNameSpace(BinderClassID id);
    static LPCUTF8 GetClassName(BinderClassID id);

    //
    // Utilities for methods
    //
    static LPCUTF8 GetMethodName(BinderMethodID id);
    static LPHARDCODEDMETASIG GetMethodSig(BinderMethodID id);

    static Signature GetMethodSignature(BinderMethodID id)
    {
        WRAPPER_NO_CONTRACT;
        return GetSignature(GetMethodSig(id));
    }

    //
    // Utilities for fields
    //
    static LPCUTF8 GetFieldName(BinderFieldID id);

    static DWORD GetFieldOffset(BinderFieldID id);

    //
    // Utilities for exceptions
    //

    static MethodTable *GetException(RuntimeExceptionKind kind)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(kind < kLastException);
        BinderClassID id = (BinderClassID) (kind + CLASS__CORELIB_COUNT);
        return GetClass(id);
    }

    static BOOL IsException(MethodTable *pMT, RuntimeExceptionKind kind)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(kind < kLastException);
        BinderClassID id = (BinderClassID) (kind + CLASS__CORELIB_COUNT);
        return dac_cast<TADDR>(GetClassIfExist(id)) == dac_cast<TADDR>(pMT);
    }

    static LPCUTF8 GetExceptionName(RuntimeExceptionKind kind)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(kind < kLastException);
        BinderClassID id = (BinderClassID) (kind + CLASS__CORELIB_COUNT);
        return GetClassName(id);
    }

    //
    // Utilities for signature element types
    //

    static PTR_MethodTable GetElementType(CorElementType type)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetExistingClass((BinderClassID) (type));
    }

    // This should be called during CLR initialization only
    static PTR_MethodTable LoadPrimitiveType(CorElementType et);

    // Get the metasig, do a one-time conversion if necessary
    static Signature GetSignature(LPHARDCODEDMETASIG pHardcodedSig);
    static Signature GetTargetSignature(LPHARDCODEDMETASIG pHardcodedSig);

    //
    // Static initialization
    //
    static void Startup();

    //
    // These are called by initialization code:
    //
    static void AttachModule(Module *pModule);

#ifdef _DEBUG
    void Check();
    void CheckExtended();
#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

private:

    // We have two different instances of the binder in crossgen. The instance local methods
    // are used when it is necessary to differentiate between them.
    PTR_MethodTable LookupClassLocal(BinderClassID id);
    MethodDesc * LookupMethodLocal(BinderMethodID id);
    FieldDesc * LookupFieldLocal(BinderFieldID id);

    PTR_MethodTable GetClassLocal(BinderClassID id);
    MethodDesc * GetMethodLocal(BinderMethodID id);
    FieldDesc * GetFieldLocal(BinderFieldID id);

    static PTR_MethodTable LookupClass(BinderClassID id);
    static MethodDesc * LookupMethod(BinderMethodID id);
    static FieldDesc * LookupField(BinderFieldID id);

    static PTR_MethodTable LookupClassIfExist(BinderClassID id);

    Signature GetSignatureLocal(LPHARDCODEDMETASIG pHardcodedSig);

    bool ConvertType(const BYTE*& pSig, SigBuilder * pSigBuilder);
    void BuildConvertedSignature(const BYTE* pSig, SigBuilder * pSigBuilder);
    const BYTE* ConvertSignature(LPHARDCODEDMETASIG pHardcodedSig, const BYTE* pSig);

    void SetDescriptions(Module * pModule,
        const CoreLibClassDescription * pClassDescriptions, USHORT nClasses,
        const CoreLibMethodDescription * pMethodDescriptions, USHORT nMethods,
        const CoreLibFieldDescription * pFieldDescriptions, USHORT nFields);

    void AllocateTables();

#ifdef _DEBUG
    static void TriggerGCUnderStress();
#endif

    PTR_Module m_pModule;

    DPTR(PTR_MethodTable) m_pClasses;
    DPTR(PTR_MethodDesc) m_pMethods;
    DPTR(PTR_FieldDesc) m_pFields;

    // This is necessary to avoid embeding copy of the descriptions into mscordacwks
    DPTR(const CoreLibClassDescription) m_classDescriptions;
    DPTR(const CoreLibMethodDescription) m_methodDescriptions;
    DPTR(const CoreLibFieldDescription) m_fieldDescriptions;

    USHORT m_cClasses;
    USHORT m_cMethods;
    USHORT m_cFields;

    static CrstStatic s_SigConvertCrst;

#ifdef _DEBUG

    struct OffsetAndSizeCheck
    {
        PTR_CSTR classNameSpace;
        PTR_CSTR className;
        SIZE_T expectedClassSize;

        PTR_CSTR fieldName;
        SIZE_T expectedFieldOffset;
        SIZE_T expectedFieldSize;
    };

    static const OffsetAndSizeCheck OffsetsAndSizes[];

#endif
};

//
// Global bound modules:
//

GVAL_DECL(CoreLibBinder, g_CoreLib);

FORCEINLINE PTR_MethodTable CoreLibBinder::GetClass(BinderClassID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());

        PRECONDITION(id != CLASS__NIL);
        PRECONDITION((&g_CoreLib)->m_cClasses > 0);  // Make sure CoreLib has been loaded.
        PRECONDITION(id <= (&g_CoreLib)->m_cClasses);
    }
    CONTRACTL_END;

    // Force a GC here under stress because type loading could trigger GC nondeterministicly
    INDEBUG(TriggerGCUnderStress());

    PTR_MethodTable pMT = VolatileLoad(&((&g_CoreLib)->m_pClasses[id]));
    if (pMT == NULL)
        return LookupClass(id);
    return pMT;
}

FORCEINLINE MethodDesc * CoreLibBinder::GetMethod(BinderMethodID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());

        PRECONDITION(id != METHOD__NIL);
        PRECONDITION(id <= (&g_CoreLib)->m_cMethods);
    }
    CONTRACTL_END;

    // Force a GC here under stress because type loading could trigger GC nondeterministicly
    INDEBUG(TriggerGCUnderStress());

    MethodDesc * pMD = VolatileLoad(&((&g_CoreLib)->m_pMethods[id]));
    if (pMD == NULL)
        return LookupMethod(id);
    return pMD;
}

FORCEINLINE FieldDesc * CoreLibBinder::GetField(BinderFieldID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());

        PRECONDITION(id != FIELD__NIL);
        PRECONDITION(id <= (&g_CoreLib)->m_cFields);
    }
    CONTRACTL_END;

    // Force a GC here under stress because type loading could trigger GC nondeterministicly
    INDEBUG(TriggerGCUnderStress());

    FieldDesc * pFD = VolatileLoad(&((&g_CoreLib)->m_pFields[id]));
    if (pFD == NULL)
        return LookupField(id);
    return pFD;
}

FORCEINLINE PTR_MethodTable CoreLibBinder::GetExistingClass(BinderClassID id)
{
    LIMITED_METHOD_DAC_CONTRACT;
    PTR_MethodTable pMT = (&g_CoreLib)->m_pClasses[id];
    _ASSERTE(pMT != NULL);
    return pMT;
}

FORCEINLINE MethodDesc * CoreLibBinder::GetExistingMethod(BinderMethodID id)
{
    LIMITED_METHOD_DAC_CONTRACT;
    MethodDesc * pMD = (&g_CoreLib)->m_pMethods[id];
    _ASSERTE(pMD != NULL);
    return pMD;
}

FORCEINLINE FieldDesc * CoreLibBinder::GetExistingField(BinderFieldID id)
{
    LIMITED_METHOD_DAC_CONTRACT;
    FieldDesc * pFD = (&g_CoreLib)->m_pFields[id];
    _ASSERTE(pFD != NULL);
    return pFD;
}

FORCEINLINE PTR_MethodTable CoreLibBinder::GetClassIfExist(BinderClassID id)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        FORBID_FAULT;
        MODE_ANY;

        PRECONDITION(id != CLASS__NIL);
        PRECONDITION(id <= (&g_CoreLib)->m_cClasses);
    }
    CONTRACTL_END;

    PTR_MethodTable pMT = VolatileLoad(&((&g_CoreLib)->m_pClasses[id]));
    if (pMT == NULL)
        return LookupClassIfExist(id);
    return pMT;
}


FORCEINLINE PTR_Module CoreLibBinder::GetModule()
{
    LIMITED_METHOD_DAC_CONTRACT;
    PTR_Module pModule = (&g_CoreLib)->m_pModule;
    _ASSERTE(pModule != NULL);
    return pModule;
}

FORCEINLINE LPCUTF8 CoreLibBinder::GetClassNameSpace(BinderClassID id)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(id != CLASS__NIL);
    _ASSERTE(id <= (&g_CoreLib)->m_cClasses);
    return (&g_CoreLib)->m_classDescriptions[id].nameSpace;
}

FORCEINLINE LPCUTF8 CoreLibBinder::GetClassName(BinderClassID id)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(id != CLASS__NIL);
    _ASSERTE(id <= (&g_CoreLib)->m_cClasses);
    return (&g_CoreLib)->m_classDescriptions[id].name;
}

FORCEINLINE LPCUTF8 CoreLibBinder::GetMethodName(BinderMethodID id)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(id != METHOD__NIL);
    _ASSERTE(id <= (&g_CoreLib)->m_cMethods);
    return (&g_CoreLib)->m_methodDescriptions[id-1].name;
}

FORCEINLINE LPHARDCODEDMETASIG CoreLibBinder::GetMethodSig(BinderMethodID id)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(id != METHOD__NIL);
    _ASSERTE(id <= (&g_CoreLib)->m_cMethods);
    return (&g_CoreLib)->m_methodDescriptions[id-1].sig;
}

FORCEINLINE LPCUTF8 CoreLibBinder::GetFieldName(BinderFieldID id)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(id != FIELD__NIL);
    _ASSERTE(id <= (&g_CoreLib)->m_cFields);
    return (&g_CoreLib)->m_fieldDescriptions[id-1].name;
}

#endif // _BINDERMODULE_H_
