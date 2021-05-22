// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "callconvbuilder.hpp"
#include "comdelegate.h"
#include "../md/compiler/custattr.h"
#include "customattribute.h"
#include "siginfo.hpp"

// According to ECMA-335, type name strings are UTF-8. Since we are
// looking for type names that are equivalent in ASCII and UTF-8,
// using a const char constant is acceptable. Type name strings are
// in Fully Qualified form, so we include the ',' delimiter.
#define MAKE_FULLY_QUALIFIED_CALLCONV_TYPE_NAME_PREFIX(callConvTypeName) CMOD_CALLCONV_NAMESPACE "." callConvTypeName ","

namespace
{
    // Function to compute if a char string begins with another char string.
    bool BeginsWith(size_t s1Len, const char* s1, size_t s2Len, const char* s2)
    {
        WRAPPER_NO_CONTRACT;

        if (s1Len < s2Len)
            return false;

        return (0 == strncmp(s1, s2, s2Len));
    }

    // Function to compute if a char string is equal to another char string.
    bool Equals(size_t s1Len, const char* s1, size_t s2Len, const char* s2)
    {
        return (s1Len == s2Len) && (0 == strcmp(s1, s2));
    }

    // All base calling conventions and modifiers should be defined below.
    // The declaration macros will then be used to construct static data to
    // be read when parsing strings from metadata.
#define DECLARE_BASE_CALL_CONVS                             \
    BASE_CALL_CONV(CMOD_CALLCONV_NAME_CDECL, C)             \
    BASE_CALL_CONV(CMOD_CALLCONV_NAME_STDCALL, Stdcall)     \
    BASE_CALL_CONV(CMOD_CALLCONV_NAME_THISCALL, Thiscall)   \
    BASE_CALL_CONV(CMOD_CALLCONV_NAME_FASTCALL, Fastcall)

#define DECLARE_MOD_CALL_CONVS \
    CALL_CONV_MODIFIER(CMOD_CALLCONV_NAME_SUPPRESSGCTRANSITION, CALL_CONV_MOD_SUPPRESSGCTRANSITION) \
    CALL_CONV_MODIFIER(CMOD_CALLCONV_NAME_MEMBERFUNCTION, CALL_CONV_MOD_MEMBERFUNCTION)

    template<typename FLAGTYPE>
    struct TypeWithFlag
    {
        const char* Name;
        const size_t NameLength;
        const FLAGTYPE Flag;
        bool (* const Matches)(size_t s1Len, const char* s1, size_t s2Len, const char* s2);
    };

    const TypeWithFlag<CorInfoCallConvExtension> FullyQualifiedTypeBaseCallConvs[] =
    {
#define BASE_CALL_CONV(name, flag) { \
        MAKE_FULLY_QUALIFIED_CALLCONV_TYPE_NAME_PREFIX(name), \
        lengthof(MAKE_FULLY_QUALIFIED_CALLCONV_TYPE_NAME_PREFIX(name)) - 1, \
        CorInfoCallConvExtension::flag, \
        BeginsWith },

        DECLARE_BASE_CALL_CONVS

#undef BASE_CALL_CONV
    };

    const TypeWithFlag<CorInfoCallConvExtension> TypeBaseCallConvs[] =
    {
#define BASE_CALL_CONV(name, flag) { \
        name, \
        lengthof(name) - 1, \
        CorInfoCallConvExtension::flag, \
        Equals },

        DECLARE_BASE_CALL_CONVS

#undef BASE_CALL_CONV
    };

    const TypeWithFlag<CallConvBuilder::CallConvModifiers> FullyQualifiedTypeModCallConvs[] =
    {
#define CALL_CONV_MODIFIER(name, flag) { \
        MAKE_FULLY_QUALIFIED_CALLCONV_TYPE_NAME_PREFIX(name), \
        lengthof(MAKE_FULLY_QUALIFIED_CALLCONV_TYPE_NAME_PREFIX(name)) - 1, \
        CallConvBuilder::flag, \
        BeginsWith },

        DECLARE_MOD_CALL_CONVS

#undef CALL_CONV_MODIFIER
    };

    const TypeWithFlag<CallConvBuilder::CallConvModifiers> TypeModCallConvs[] =
    {
#define CALL_CONV_MODIFIER(name, flag) { \
        name, \
        lengthof(name) - 1, \
        CallConvBuilder::flag, \
        Equals },

        DECLARE_MOD_CALL_CONVS

#undef CALL_CONV_MODIFIER
    };

#undef DECLARE_CALL_CONVS

    template<size_t BASECOUNT, size_t MODCOUNT>
    bool ProcessName(
        _Inout_ CallConvBuilder::State& state,
        _In_ size_t typeLength,
        _In_z_ LPCSTR typeName,
        _In_ const TypeWithFlag<CorInfoCallConvExtension> (&baseTypes)[BASECOUNT],
        _In_ const TypeWithFlag<CallConvBuilder::CallConvModifiers> (&modTypes)[MODCOUNT])
    {
        LIMITED_METHOD_CONTRACT;

        // Check if the type is a base calling convention.
        for (size_t i = 0; i < BASECOUNT; ++i)
        {
            const TypeWithFlag<CorInfoCallConvExtension>& entry = baseTypes[i];
            if (!entry.Matches(typeLength, typeName, entry.NameLength, entry.Name))
                continue;

            // If the base calling convention is already set, then we are observing an error.
            if (state.CallConvBase != CallConvBuilder::UnsetValue)
                return false;

            state.CallConvBase = entry.Flag;
            return true;
        }

        // Check if the type is a modifier calling convention.
        for (size_t i = 0; i < MODCOUNT; ++i)
        {
            const TypeWithFlag<CallConvBuilder::CallConvModifiers>& entry = modTypes[i];
            if (!entry.Matches(typeLength, typeName, entry.NameLength, entry.Name))
                continue;

            // Combine the current modifier with the existing ones.
            state.CallConvModifiers = (CallConvBuilder::CallConvModifiers)(state.CallConvModifiers | entry.Flag);
            return true;
        }

        // Unknown type. This is okay since we should be resiliant against new types that
        // we don't know anything about.
        return true;
    }

    CorInfoCallConvExtension GetMemberFunctionUnmanagedCallingConventionVariant(CorInfoCallConvExtension baseCallConv)
    {
        switch (baseCallConv)
        {
        case CorInfoCallConvExtension::C:
            return CorInfoCallConvExtension::CMemberFunction;
        case CorInfoCallConvExtension::Stdcall:
            return CorInfoCallConvExtension::StdcallMemberFunction;
        case CorInfoCallConvExtension::Fastcall:
            return CorInfoCallConvExtension::FastcallMemberFunction;
        case CorInfoCallConvExtension::Thiscall:
            return CorInfoCallConvExtension::Thiscall;
        default:
            _ASSERTE("Calling convention is not an unmanaged base calling convention.");
            return baseCallConv;
        }
    }
}

const CorInfoCallConvExtension CallConvBuilder::UnsetValue = CorInfoCallConvExtension::Managed;

CallConvBuilder::CallConvBuilder()
    : _state{ UnsetValue , CALL_CONV_MOD_NONE }
{
    LIMITED_METHOD_CONTRACT;
}

bool CallConvBuilder::AddFullyQualifiedTypeName(
    _In_ size_t typeLength,
    _In_z_ LPCSTR typeName)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(typeName != NULL);
    }
    CONTRACTL_END;

    return ProcessName(
        _state,
        typeLength,
        typeName,
        FullyQualifiedTypeBaseCallConvs,
        FullyQualifiedTypeModCallConvs);
}

bool CallConvBuilder::AddTypeName(
    _In_ size_t typeLength,
    _In_z_ LPCSTR typeName)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(typeName != NULL);
    }
    CONTRACTL_END;

    return ProcessName(
        _state,
        typeLength,
        typeName,
        TypeBaseCallConvs,
        TypeModCallConvs);
}

CorInfoCallConvExtension CallConvBuilder::GetCurrentCallConv() const
{
    LIMITED_METHOD_CONTRACT;

    if (IsCurrentCallConvModSet(CallConvBuilder::CALL_CONV_MOD_MEMBERFUNCTION))
    {
        CorInfoCallConvExtension baseMaybe = _state.CallConvBase;
        if (baseMaybe == CallConvBuilder::UnsetValue)
        {
            // In this case, the only specified calling convention is CallConvMemberFunction.
            // When the Member function modifier is defined with no base type, we assume
            // the default unmanaged calling convention.
            baseMaybe = CallConv::GetDefaultUnmanagedCallingConvention();
        }

        return GetMemberFunctionUnmanagedCallingConventionVariant(baseMaybe);
    }

    return _state.CallConvBase;
}

bool CallConvBuilder::IsCurrentCallConvModSet(_In_ CallConvModifiers mod) const
{
    LIMITED_METHOD_CONTRACT;
    return (mod & _state.CallConvModifiers) != CALL_CONV_MOD_NONE;
}

namespace
{
    HRESULT GetNameOfTypeRefOrDef(
        _In_ const Module *pModule,
        _In_ mdToken token,
        _Out_ LPCSTR *namespaceOut,
        _Out_ LPCSTR *nameOut)
    {
        STANDARD_VM_CONTRACT;

        IMDInternalImport *pInternalImport = pModule->GetMDImport();
        if (TypeFromToken(token) == mdtTypeDef)
        {
            HRESULT hr = pInternalImport->GetNameOfTypeDef(token, nameOut, namespaceOut);
            if (FAILED(hr))
                return hr;
        }
        else if (TypeFromToken(token) == mdtTypeRef)
        {
            HRESULT hr = pInternalImport->GetNameOfTypeRef(token, namespaceOut, nameOut);
            if (FAILED(hr))
                return hr;
        }
        else
        {
            return E_INVALIDARG;
        }

        return S_OK;
    }

    HRESULT GetNameOfTypeRefOrDef(
        _In_ DynamicResolver *pResolver,
        _In_ mdToken token,
        _Out_ LPCSTR *namespaceOut,
        _Out_ LPCSTR *nameOut)
    {
        STANDARD_VM_CONTRACT;

        TypeHandle type;
        MethodDesc* pMD;
        FieldDesc* pFD;

        pResolver->ResolveToken(token, &type, &pMD, &pFD);

        _ASSERTE(!type.IsNull());

        *nameOut = type.GetMethodTable()->GetFullyQualifiedNameInfo(namespaceOut);

        return S_OK;
    }

    HRESULT GetNameOfTypeRefOrDef(
        _In_ CORINFO_MODULE_HANDLE pModule,
        _In_ mdToken token,
        _Out_ LPCSTR *namespaceOut,
        _Out_ LPCSTR *nameOut)
    {
        STANDARD_VM_CONTRACT;

        if (IsDynamicScope(pModule))
        {
            return GetNameOfTypeRefOrDef(GetDynamicResolver(pModule), token, namespaceOut, nameOut);
        }
        else
        {
            return GetNameOfTypeRefOrDef(GetModule(pModule), token, namespaceOut, nameOut);
        }
    }
}

HRESULT CallConv::TryGetUnmanagedCallingConventionFromModOpt(
    _In_ CORINFO_MODULE_HANDLE pModule,
    _In_ PCCOR_SIGNATURE pSig,
    _In_ ULONG cSig,
    _Inout_ CallConvBuilder* builder,
    _Out_ UINT *errorResID)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(builder != NULL);
        PRECONDITION(errorResID != NULL);
    }
    CONTRACTL_END

    HRESULT hr;

    // Instantiations aren't relevant here
    SigPointer sigPtr(pSig, cSig);
    uint32_t sigCallConv = 0;
    IfFailRet(sigPtr.GetCallingConvInfo(&sigCallConv)); // call conv
    if (sigCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        IfFailRet(sigPtr.GetData(NULL)); // type param count
    }
    IfFailRet(sigPtr.GetData(NULL)); // arg count

    PCCOR_SIGNATURE pWalk = sigPtr.GetPtr();
    _ASSERTE(pWalk <= pSig + cSig);

    CallConvBuilder& callConvBuilder = *builder;
    while ((pWalk < (pSig + cSig)) && ((*pWalk == ELEMENT_TYPE_CMOD_OPT) || (*pWalk == ELEMENT_TYPE_CMOD_REQD)))
    {
        BOOL fIsOptional = (*pWalk == ELEMENT_TYPE_CMOD_OPT);

        pWalk++;
        if (pWalk + CorSigUncompressedDataSize(pWalk) > pSig + cSig)
        {
            *errorResID = BFA_BAD_SIGNATURE;
            return COR_E_BADIMAGEFORMAT; // Bad formatting
        }

        mdToken tk;
        pWalk += CorSigUncompressToken(pWalk, &tk);

        if (!fIsOptional)
            continue;

        LPCSTR typeNamespace;
        LPCSTR typeName;

        // Check for CallConv types specified in modopt
        if (FAILED(GetNameOfTypeRefOrDef(pModule, tk, &typeNamespace, &typeName)))
            continue;

        if (::strcmp(typeNamespace, CMOD_CALLCONV_NAMESPACE) != 0)
            continue;

        if (!callConvBuilder.AddTypeName(::strlen(typeName), typeName))
        {
            // Error if there are multiple recognized base calling conventions
            *errorResID = IDS_EE_MULTIPLE_CALLCONV_UNSUPPORTED;
            return COR_E_INVALIDPROGRAM;
        }
    }

    return  S_OK;
}

#ifndef CROSSGEN_COMPILE
namespace
{
    bool TryGetCallingConventionFromTypeArray(_In_ CaValue* arrayOfTypes, _Inout_ CallConvBuilder* builder)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(arrayOfTypes != NULL);
            PRECONDITION(builder != NULL);
        }
        CONTRACTL_END

        for (ULONG i = 0; i < arrayOfTypes->arr.length; i++)
        {
            CaValue& typeNameValue = arrayOfTypes->arr[i];
            if (!builder->AddFullyQualifiedTypeName(typeNameValue.str.cbStr, typeNameValue.str.pStr))
            {
                // We found a second base calling convention.
                return false;
            }
        }

        return true;
    }
}

HRESULT CallConv::TryGetCallingConventionFromUnmanagedCallConv(
    _In_ MethodDesc* pMD,
    _Inout_ CallConvBuilder* builder,
    _Out_opt_ UINT* errorResID)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(pMD != NULL);
        PRECONDITION(builder != NULL);
    }
    CONTRACTL_END;

    BYTE *pData = NULL;
    LONG cData = 0;

    // System.Runtime.InteropServices.UnmanagedCallConvAttribute
    HRESULT hr = pMD->GetCustomAttribute(WellKnownAttribute::UnmanagedCallConv, (const VOID **)(&pData), (ULONG *)&cData);
    if (hr != S_OK) // GetCustomAttribute returns S_FALSE if the method does not have the attribute
        return hr;

    _ASSERTE(cData > 0);
    CustomAttributeParser ca(pData, cData);

    // CallConvs named argument
    CaTypeCtor callConvsType(SERIALIZATION_TYPE_SZARRAY, SERIALIZATION_TYPE_TYPE, SERIALIZATION_TYPE_UNDEFINED, NULL, 0);
    CaNamedArg callConvsArg;
    callConvsArg.Init("CallConvs", SERIALIZATION_TYPE_SZARRAY, callConvsType);

    InlineFactory<SArray<CaValue>, 4> caValueArrayFactory;
    DomainAssembly* domainAssembly = pMD->GetLoaderModule()->GetDomainAssembly();
    IfFailThrow(Attribute::ParseAttributeArgumentValues(
        pData,
        cData,
        &caValueArrayFactory,
        NULL,
        0,
        &callConvsArg,
        1,
        domainAssembly));

    // Value isn't defined
    if (callConvsArg.val.type.tag == SERIALIZATION_TYPE_UNDEFINED)
        return S_FALSE;

    if (!TryGetCallingConventionFromTypeArray(&callConvsArg.val, builder))
    {
        if (errorResID != NULL)
            *errorResID = IDS_EE_MULTIPLE_CALLCONV_UNSUPPORTED;

        return COR_E_INVALIDPROGRAM;
    }

    return S_OK;
}

bool CallConv::TryGetCallingConventionFromUnmanagedCallersOnly(_In_ MethodDesc* pMD, _Out_ CorInfoCallConvExtension* pCallConv)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMD != NULL && pMD->HasUnmanagedCallersOnlyAttribute());

    // Validate usage
    COMDelegate::ThrowIfInvalidUnmanagedCallersOnlyUsage(pMD);

    BYTE* pData = NULL;
    LONG cData = 0;

    bool nativeCallableInternalData = false;
    HRESULT hr = pMD->GetCustomAttribute(WellKnownAttribute::UnmanagedCallersOnly, (const VOID **)(&pData), (ULONG *)&cData);
    if (hr == S_FALSE)
    {
        hr = pMD->GetCustomAttribute(WellKnownAttribute::NativeCallableInternal, (const VOID **)(&pData), (ULONG *)&cData);
        nativeCallableInternalData = SUCCEEDED(hr);
    }

    IfFailThrow(hr);

    _ASSERTE(cData > 0);

    CustomAttributeParser ca(pData, cData);

    // UnmanagedCallersOnly and NativeCallableInternal each
    // have optional named arguments.
    CaNamedArg namedArgs[2];

    // For the UnmanagedCallersOnly scenario.
    CaType caCallConvs;

    // Define attribute specific optional named properties
    if (nativeCallableInternalData)
    {
        namedArgs[0].InitI4FieldEnum("CallingConvention", "System.Runtime.InteropServices.CallingConvention", (ULONG)(CorPinvokeMap)0);
    }
    else
    {
        caCallConvs.Init(SERIALIZATION_TYPE_SZARRAY, SERIALIZATION_TYPE_TYPE, SERIALIZATION_TYPE_UNDEFINED, NULL, 0);
        namedArgs[0].Init("CallConvs", SERIALIZATION_TYPE_SZARRAY, caCallConvs);
    }

    // Define common optional named properties
    CaTypeCtor caEntryPoint(SERIALIZATION_TYPE_STRING);
    namedArgs[1].Init("EntryPoint", SERIALIZATION_TYPE_STRING, caEntryPoint);

    InlineFactory<SArray<CaValue>, 4> caValueArrayFactory;
    DomainAssembly* domainAssembly = pMD->GetLoaderModule()->GetDomainAssembly();
    IfFailThrow(Attribute::ParseAttributeArgumentValues(
        pData,
        cData,
        &caValueArrayFactory,
        NULL,
        0,
        namedArgs,
        lengthof(namedArgs),
        domainAssembly));

    // If the value isn't defined, then return without setting anything.
    if (namedArgs[0].val.type.tag == SERIALIZATION_TYPE_UNDEFINED)
        return false;

    CorInfoCallConvExtension callConvLocal;
    if (nativeCallableInternalData)
    {
        callConvLocal = (CorInfoCallConvExtension)(namedArgs[0].val.u4 << 8);
    }
    else
    {
        CallConvBuilder builder;
        if (!TryGetCallingConventionFromTypeArray(&namedArgs[0].val, &builder))
        {
            // We found a second base calling convention.
            return false;
        }

        callConvLocal = builder.GetCurrentCallConv();
        if (callConvLocal == CallConvBuilder::UnsetValue)
        {
            callConvLocal = CallConv::GetDefaultUnmanagedCallingConvention();
        }
    }

    *pCallConv = callConvLocal;
    return true;
}

#endif // CROSSGEN_COMPILE
