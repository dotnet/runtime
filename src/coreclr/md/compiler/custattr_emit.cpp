// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CustAttr_Emit.cpp
//

//
// Implementation for the meta data custom attribute emit code (code:IMetaDataEmit).
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "mdperf.h"
#include "posterror.h"
#include "cahlprinternal.h"
#include "custattr.h"
#include "corhdr.h"
#include <metamodelrw.h>

#ifdef FEATURE_METADATA_EMIT

//*****************************************************************************
//*****************************************************************************
// Support for "Pseudo Custom Attributes"


//*****************************************************************************
// Enumeration of known custom attributes.
//*****************************************************************************
#define KnownCaList()                   \
    KnownCa(UNKNOWN)                    \
    KnownCa(DllImportAttribute)         \
    KnownCa(GuidAttribute)              \
    KnownCa(ComImportAttribute)         \
    KnownCa(InterfaceTypeAttribute)     \
    KnownCa(ClassInterfaceAttribute)    \
    KnownCa(SerializableAttribute)      \
    KnownCa(NonSerializedAttribute)     \
    KnownCa(MethodImplAttribute1)       \
    KnownCa(MethodImplAttribute2)       \
    KnownCa(MethodImplAttribute3)       \
    KnownCa(MarshalAsAttribute1)        \
    KnownCa(MarshalAsAttribute2)        \
    KnownCa(PreserveSigAttribute)       \
    KnownCa(InAttribute)                \
    KnownCa(OutAttribute)               \
    KnownCa(OptionalAttribute)          \
    KnownCa(StructLayoutAttribute1)     \
    KnownCa(StructLayoutAttribute2)     \
    KnownCa(FieldOffsetAttribute)       \
    KnownCa(TypeLibVersionAttribute)    \
    KnownCa(ComCompatibleVersionAttribute) \
    KnownCa(SpecialNameAttribute) \
    KnownCa(AllowPartiallyTrustedCallersAttribute) \
    KnownCa(WindowsRuntimeImportAttribute) \

// Ids for the CA's.  CA_DllImportAttribute, etc.
#define KnownCa(x) CA_##x,
enum {
    KnownCaList()
    CA_COUNT
};


//*****************************************************************************
// Properties of the known custom attributes.
//
// These tables describe the known custom attributes.  For each custom
//  attribute, we know the namespace and name of the custom attribute,
//  the types to which the CA applies, the ctor args, and possible named
//  args.  There is a flag which specifies whether the custom attribute
//  should be kept, in addition to any processing done with the data.
//*****************************************************************************
const BOOL bKEEPCA = TRUE;
const BOOL bDONTKEEPCA = FALSE;
const BOOL bMATCHBYSIG = TRUE;
const BOOL bMATCHBYNAME = FALSE;

struct KnownCaProp
{
    LPCUTF8             szNamespace;         // Namespace of the custom attribute.
    LPCUTF8             szName;              // Name of the custom attribute.
    const mdToken     * rTypes;              // Types that the CA applies to.
    BOOL                bKeepCa;             // Keep the CA after processing?
    const CaArg       * pArgs;               // List of ctor argument descriptors.
    ULONG               cArgs;               // Count of ctor argument descriptors.
    const CaNamedArg  * pNamedArgs;          // List of named arg descriptors.
    ULONG               cNamedArgs;          // Count of named arg descriptors.
    BOOL                bMatchBySig;         // For overloads; match by sig, not just name.
                                             // WARNING:  All overloads need the flag!
};

// Recognized targets for known custom attributes.
// If Target includes mdtAssembly, then make sure to include mdtTypeRef as well,
// aLink uses mdtTypeRef target temporarily for assembly target attributes
const mdToken DllImportTargets[]                    = {mdtMethodDef, (ULONG32) -1};
const mdToken GuidTargets[]                         = {mdtTypeDef, mdtTypeRef, mdtModule, mdtAssembly, (ULONG32) -1};
const mdToken ComImportTargets[]                    = {mdtTypeDef, (ULONG32) -1};
const mdToken InterfaceTypeTargets[]                = {mdtTypeDef, (ULONG32) -1};
const mdToken ClassInterfaceTargets[]               = {mdtTypeDef, mdtAssembly, mdtTypeRef, (ULONG32) -1};
const mdToken SerializableTargets[]                 = {mdtTypeDef, (ULONG32) -1};
const mdToken NotInGCHeapTargets[]                  = {mdtTypeDef, (ULONG32) -1};
const mdToken NonSerializedTargets[]                = {mdtFieldDef, (ULONG32) -1};
const mdToken MethodImplTargets[]                   = {mdtMethodDef, (ULONG32) -1};
const mdToken MarshalTargets[]                      = {mdtFieldDef, mdtParamDef, mdtProperty, (ULONG32) -1};
const mdToken PreserveSigTargets[]                  = {mdtMethodDef, (ULONG32) -1};
const mdToken InOutTargets[]                        = {mdtParamDef, (ULONG32) -1};
const mdToken StructLayoutTargets[]                 = {mdtTypeDef, (ULONG32) -1};
const mdToken FieldOffsetTargets[]                  = {mdtFieldDef, (ULONG32) -1};
const mdToken TypeLibVersionTargets[]               = {mdtAssembly, mdtTypeRef,(ULONG32) -1};
const mdToken ComCompatibleVersionTargets[]         = {mdtAssembly, mdtTypeRef, (ULONG32) -1};
const mdToken SpecialNameTargets[]                  = {mdtTypeDef, mdtMethodDef, mdtFieldDef, mdtProperty, mdtEvent, (ULONG32) -1};
const mdToken AllowPartiallyTrustedCallersTargets[] = {mdtAssembly, mdtTypeRef, (ULONG32) -1};
const mdToken WindowsRuntimeImportTargets[]         = {mdtTypeDef, (ULONG32) -1};


//#ifndef CEE_CALLCONV
// # define CEE_CALLCONV (IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS)
//#endif

#define DEFINE_CA_CTOR_ARGS(NAME)               \
    const CaArg r##NAME##Args[] =                     \
    {
#define DEFINE_CA_CTOR_ARG(TYPE) {{TYPE}},
#define DEFINE_CA_CTOR_ARGS_END()               \
    };



#define DEFINE_CA_NAMED_ARGS(NAME)              \
    const CaNamedArg r##NAME##NamedArgs[] =           \
    {

#define DEFINE_CA_NAMED_ARG(NAME, PROPORFIELD, TYPE, ARRAYTYPE, ENUMTYPE, ENUMNAME)                                 \
    { NAME, sizeof(NAME) - 1, PROPORFIELD,   {   TYPE, ARRAYTYPE, ENUMTYPE, ENUMNAME, sizeof(ENUMNAME) - 1   } },

#define DEFINE_CA_NAMED_PROP_I4ENUM(NAME, ENUM) \
    DEFINE_CA_NAMED_ARG(NAME, SERIALIZATION_TYPE_PROPERTY, SERIALIZATION_TYPE_ENUM, SERIALIZATION_TYPE_UNDEFINED, SERIALIZATION_TYPE_I4, ENUM)
#define DEFINE_CA_NAMED_FIELD_I4ENUM(NAME, ENUM) \
    DEFINE_CA_NAMED_ARG(NAME, SERIALIZATION_TYPE_FIELD, SERIALIZATION_TYPE_ENUM, SERIALIZATION_TYPE_UNDEFINED, SERIALIZATION_TYPE_I4, ENUM)
#define DEFINE_CA_NAMED_PROP(NAME, TYPE) \
    DEFINE_CA_NAMED_ARG(NAME, SERIALIZATION_TYPE_PROPERTY, TYPE, SERIALIZATION_TYPE_UNDEFINED, SERIALIZATION_TYPE_UNDEFINED, "")
#define DEFINE_CA_NAMED_FIELD(NAME, TYPE) \
    DEFINE_CA_NAMED_ARG(NAME, SERIALIZATION_TYPE_FIELD, TYPE, SERIALIZATION_TYPE_UNDEFINED, SERIALIZATION_TYPE_UNDEFINED, "")
#define DEFINE_CA_NAMED_PROP_BOOL(NAME) DEFINE_CA_NAMED_PROP(NAME, SERIALIZATION_TYPE_BOOLEAN)
#define DEFINE_CA_NAMED_FIELD_BOOL(NAME) DEFINE_CA_NAMED_FIELD(NAME, SERIALIZATION_TYPE_BOOLEAN)
#define DEFINE_CA_NAMED_PROP_STRING(NAME) DEFINE_CA_NAMED_PROP(NAME, SERIALIZATION_TYPE_STRING)
#define DEFINE_CA_NAMED_FIELD_STRING(NAME) DEFINE_CA_NAMED_FIELD(NAME, SERIALIZATION_TYPE_STRING)
#define DEFINE_CA_NAMED_PROP_TYPE(NAME) DEFINE_CA_NAMED_PROP(NAME, SERIALIZATION_TYPE_TYPE)
#define DEFINE_CA_NAMED_FIELD_TYPE(NAME) DEFINE_CA_NAMED_FIELD(NAME, SERIALIZATION_TYPE_TYPE)
#define DEFINE_CA_NAMED_PROP_I2(NAME) DEFINE_CA_NAMED_PROP(NAME, SERIALIZATION_TYPE_I2)
#define DEFINE_CA_NAMED_FIELD_I2(NAME) DEFINE_CA_NAMED_FIELD(NAME, SERIALIZATION_TYPE_I2)
#define DEFINE_CA_NAMED_PROP_I4(NAME) DEFINE_CA_NAMED_PROP(NAME, SERIALIZATION_TYPE_I4)
#define DEFINE_CA_NAMED_FIELD_I4(NAME) DEFINE_CA_NAMED_FIELD(NAME, SERIALIZATION_TYPE_I4)

#define DEFINE_CA_NAMED_ARGS_END()          \
    };

//-----------------------------------------------------------------------------
// index 0 is used as a placeholder.
const KnownCaProp UNKNOWNProps                   = {0};

//-----------------------------------------------------------------------------
// DllImport args, named args, and known attribute properties.
DEFINE_CA_CTOR_ARGS(DllImportAttribute)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_STRING)
DEFINE_CA_CTOR_ARGS_END()

// NOTE:  Keep this enum in sync with the array of named arguments.
enum DllImportNamedArgs
{
    DI_CallingConvention,
    DI_CharSet,
    DI_EntryPoint,
    DI_ExactSpelling,
    DI_SetLastError,
    DI_PreserveSig,
    DI_BestFitMapping,
    DI_ThrowOnUnmappableChar,
    DI_COUNT
};

DEFINE_CA_NAMED_ARGS(DllImportAttribute)
    DEFINE_CA_NAMED_FIELD_I4ENUM("CallingConvention", "System.Runtime.InteropServices.CallingConvention")
    DEFINE_CA_NAMED_FIELD_I4ENUM("CharSet", "System.Runtime.InteropServices.CharSet")
    DEFINE_CA_NAMED_FIELD_STRING("EntryPoint")
    DEFINE_CA_NAMED_FIELD_BOOL("ExactSpelling")
    DEFINE_CA_NAMED_FIELD_BOOL("SetLastError")
    DEFINE_CA_NAMED_FIELD_BOOL("PreserveSig")
    DEFINE_CA_NAMED_FIELD_BOOL("BestFitMapping")
    DEFINE_CA_NAMED_FIELD_BOOL("ThrowOnUnmappableChar")
DEFINE_CA_NAMED_ARGS_END()

const KnownCaProp DllImportAttributeProps         = {"System.Runtime.InteropServices", "DllImportAttribute", DllImportTargets, bDONTKEEPCA,
                                                rDllImportAttributeArgs, ARRAY_SIZE(rDllImportAttributeArgs),
                                                rDllImportAttributeNamedArgs, ARRAY_SIZE(rDllImportAttributeNamedArgs)};

//-----------------------------------------------------------------------------
// GUID args, named args (none), and known attribute properties.
DEFINE_CA_CTOR_ARGS(GuidAttribute)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_STRING)
DEFINE_CA_CTOR_ARGS_END()

const KnownCaProp GuidAttributeProps              = {"System.Runtime.InteropServices", "GuidAttribute", GuidTargets, bKEEPCA,
                                                rGuidAttributeArgs, ARRAY_SIZE(rGuidAttributeArgs)};

//-----------------------------------------------------------------------------
// ComImport args (none), named args (none), and known attribute properties.
const KnownCaProp ComImportAttributeProps         = {"System.Runtime.InteropServices", "ComImportAttribute", ComImportTargets};

//-----------------------------------------------------------------------------
// Interface type args, named args (none), and known attribute properties.
DEFINE_CA_CTOR_ARGS(InterfaceTypeAttribute)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_U2)
DEFINE_CA_CTOR_ARGS_END()

const KnownCaProp InterfaceTypeAttributeProps     = {"System.Runtime.InteropServices", "InterfaceTypeAttribute", InterfaceTypeTargets, bKEEPCA,
                                                rInterfaceTypeAttributeArgs, ARRAY_SIZE(rInterfaceTypeAttributeArgs)};

//-----------------------------------------------------------------------------
// Class interface type args, named args (none), and known attribute properties.
DEFINE_CA_CTOR_ARGS(ClassInterfaceAttribute)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_U2)
DEFINE_CA_CTOR_ARGS_END()

const KnownCaProp ClassInterfaceAttributeProps     = {"System.Runtime.InteropServices", "ClassInterfaceAttribute", ClassInterfaceTargets, bKEEPCA,
                                                rClassInterfaceAttributeArgs, ARRAY_SIZE(rClassInterfaceAttributeArgs)};

//-----------------------------------------------------------------------------
// Serializable args (none), named args (none), and known attribute properties.
const KnownCaProp SerializableAttributeProps      = {"System", "SerializableAttribute", SerializableTargets};

//-----------------------------------------------------------------------------
// NonSerialized args (none), named args (none), and known attribute properties.
const KnownCaProp NonSerializedAttributeProps     = {"System", "NonSerializedAttribute", NonSerializedTargets};

//-----------------------------------------------------------------------------
// SpecialName args (none), named args (none), and known attribute properties.
const KnownCaProp SpecialNameAttributeProps     = {"System.Runtime.CompilerServices", "SpecialNameAttribute", SpecialNameTargets, bDONTKEEPCA};

//-----------------------------------------------------------------------------
// WindowsRuntimeImport args (none), named args (none), and known attribute properties.
const KnownCaProp WindowsRuntimeImportAttributeProps = {"System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeImportAttribute", WindowsRuntimeImportTargets};


//-----------------------------------------------------------------------------
// MethodImpl #1 args (none), named args, and known attribute properties.
// MethodImpl #2 args (short), named args, and known attribute properties.
// MethodImpl #3 args (enum), named args, and known attribute properties.
// Note: first two match by signature; third by name only, because signature matching code is not
//  strong enough for enums.
DEFINE_CA_CTOR_ARGS(MethodImplAttribute2)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I2)
DEFINE_CA_CTOR_ARGS_END()

DEFINE_CA_CTOR_ARGS(MethodImplAttribute3)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_U4)
DEFINE_CA_CTOR_ARGS_END()

enum MethodImplAttributeNamedArgs
{
    MI_CodeType,
    MI_COUNT
};

DEFINE_CA_NAMED_ARGS(MethodImplAttribute)
    DEFINE_CA_NAMED_FIELD_I4ENUM("MethodCodeType", "System.Runtime.CompilerServices.MethodCodeType")
DEFINE_CA_NAMED_ARGS_END()

const KnownCaProp MethodImplAttribute1Props        = {"System.Runtime.CompilerServices", "MethodImplAttribute", MethodImplTargets, bDONTKEEPCA,
                                                0, 0,
                                                rMethodImplAttributeNamedArgs, ARRAY_SIZE(rMethodImplAttributeNamedArgs),
                                                bMATCHBYSIG};
const KnownCaProp MethodImplAttribute2Props        = {"System.Runtime.CompilerServices", "MethodImplAttribute", MethodImplTargets, bDONTKEEPCA,
                                                rMethodImplAttribute2Args, ARRAY_SIZE(rMethodImplAttribute2Args),
                                                rMethodImplAttributeNamedArgs, ARRAY_SIZE(rMethodImplAttributeNamedArgs),
                                                bMATCHBYSIG};
const KnownCaProp MethodImplAttribute3Props        = {"System.Runtime.CompilerServices", "MethodImplAttribute", MethodImplTargets, bDONTKEEPCA,
                                                rMethodImplAttribute3Args, ARRAY_SIZE(rMethodImplAttribute3Args),
                                                rMethodImplAttributeNamedArgs, ARRAY_SIZE(rMethodImplAttributeNamedArgs),
                                                bMATCHBYNAME};

//-----------------------------------------------------------------------------
// Marshal args, named args, and known attribute properties.
DEFINE_CA_CTOR_ARGS(MarshalAsAttribute2)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_U4)
DEFINE_CA_CTOR_ARGS_END()

DEFINE_CA_CTOR_ARGS(MarshalAsAttribute1)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I2)
DEFINE_CA_CTOR_ARGS_END()

// NOTE:  Keep this enum in sync with the array of named arguments.
enum MarshalNamedArgs
{
    M_ArraySubType,
    M_SafeArraySubType,
    M_SafeArrayUserDefinedSubType,
    M_SizeParamIndex,
    M_SizeConst,
    M_MarshalType,
    M_MarshalTypeRef,
    M_MarshalCookie,
    M_IidParameterIndex,
    M_COUNT
};

DEFINE_CA_NAMED_ARGS(MarshalAsAttribute)
    DEFINE_CA_NAMED_FIELD_I4ENUM("ArraySubType", "System.Runtime.InteropServices.UnmanagedType")
    DEFINE_CA_NAMED_FIELD_I4ENUM("SafeArraySubType", "System.Runtime.InteropServices.VarEnum")
    DEFINE_CA_NAMED_FIELD_TYPE("SafeArrayUserDefinedSubType")
    DEFINE_CA_NAMED_FIELD_I2("SizeParamIndex")
    DEFINE_CA_NAMED_FIELD_I4("SizeConst")
    DEFINE_CA_NAMED_FIELD_STRING("MarshalType")
    DEFINE_CA_NAMED_FIELD_TYPE("MarshalTypeRef")
    DEFINE_CA_NAMED_FIELD_STRING("MarshalCookie")
    DEFINE_CA_NAMED_FIELD_I4("IidParameterIndex")
DEFINE_CA_NAMED_ARGS_END()

const KnownCaProp MarshalAsAttribute1Props        = {"System.Runtime.InteropServices", "MarshalAsAttribute", MarshalTargets, bDONTKEEPCA,
                                              rMarshalAsAttribute1Args, ARRAY_SIZE(rMarshalAsAttribute1Args),
                                              rMarshalAsAttributeNamedArgs, ARRAY_SIZE(rMarshalAsAttributeNamedArgs),
                                              bMATCHBYSIG};

const KnownCaProp MarshalAsAttribute2Props        = {"System.Runtime.InteropServices", "MarshalAsAttribute", MarshalTargets, bDONTKEEPCA,
                                              rMarshalAsAttribute2Args, ARRAY_SIZE(rMarshalAsAttribute2Args),
                                              rMarshalAsAttributeNamedArgs, ARRAY_SIZE(rMarshalAsAttributeNamedArgs),
                                              bMATCHBYNAME};

//-----------------------------------------------------------------------------
// PreserveSignature args, named args (none), and known attribute properties.
const KnownCaProp PreserveSigAttributeProps        = {"System.Runtime.InteropServices", "PreserveSigAttribute", PreserveSigTargets, bDONTKEEPCA};

//-----------------------------------------------------------------------------
// In args (none), named args (none), and known attribute properties.
const KnownCaProp InAttributeProps     = {"System.Runtime.InteropServices", "InAttribute", InOutTargets};

//-----------------------------------------------------------------------------
// Out args (none), named args (none), and known attribute properties.
const KnownCaProp OutAttributeProps     = {"System.Runtime.InteropServices", "OutAttribute", InOutTargets};

//-----------------------------------------------------------------------------
// Optional args (none), named args (none), and known attribute properties.
const KnownCaProp OptionalAttributeProps     = {"System.Runtime.InteropServices", "OptionalAttribute", InOutTargets};

//-----------------------------------------------------------------------------
// StructLayout args, named args, and known attribute properties.
DEFINE_CA_CTOR_ARGS(StructLayoutAttribute2)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I4)
DEFINE_CA_CTOR_ARGS_END()

DEFINE_CA_CTOR_ARGS(StructLayoutAttribute1)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I2)
DEFINE_CA_CTOR_ARGS_END()

// NOTE:  Keep this enum in sync with the array of named arguments.
enum StructLayoutNamedArgs
{
    SL_Pack,
    SL_Size,
    SL_CharSet,
    SL_COUNT
};

DEFINE_CA_NAMED_ARGS(StructLayoutAttribute)
    DEFINE_CA_NAMED_FIELD_I4("Pack")
    DEFINE_CA_NAMED_FIELD_I4("Size")
    DEFINE_CA_NAMED_FIELD_I4ENUM("CharSet", "System.Runtime.InteropServices.CharSet")
DEFINE_CA_NAMED_ARGS_END()

const KnownCaProp StructLayoutAttribute1Props       = {"System.Runtime.InteropServices", "StructLayoutAttribute", StructLayoutTargets, bDONTKEEPCA,
                                                rStructLayoutAttribute1Args, ARRAY_SIZE(rStructLayoutAttribute1Args),
                                                rStructLayoutAttributeNamedArgs, ARRAY_SIZE(rStructLayoutAttributeNamedArgs),
                                                bMATCHBYSIG};
const KnownCaProp StructLayoutAttribute2Props       = {"System.Runtime.InteropServices", "StructLayoutAttribute", StructLayoutTargets, bDONTKEEPCA,
                                                rStructLayoutAttribute2Args, ARRAY_SIZE(rStructLayoutAttribute2Args),
                                                rStructLayoutAttributeNamedArgs, ARRAY_SIZE(rStructLayoutAttributeNamedArgs),
                                                bMATCHBYNAME};

//-----------------------------------------------------------------------------
// FieldOffset args, named args (none), and known attribute properties.
DEFINE_CA_CTOR_ARGS(FieldOffsetAttribute)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_U4)
DEFINE_CA_CTOR_ARGS_END()

const KnownCaProp FieldOffsetAttributeProps        = {"System.Runtime.InteropServices", "FieldOffsetAttribute", FieldOffsetTargets, bDONTKEEPCA,
                                                rFieldOffsetAttributeArgs, ARRAY_SIZE(rFieldOffsetAttributeArgs)};

DEFINE_CA_CTOR_ARGS(TypeLibVersionAttribute)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I4)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I4)
DEFINE_CA_CTOR_ARGS_END()

const KnownCaProp TypeLibVersionAttributeProps = {"System.Runtime.InteropServices", "TypeLibVersionAttribute", TypeLibVersionTargets, bKEEPCA,
                                            rTypeLibVersionAttributeArgs, ARRAY_SIZE(rTypeLibVersionAttributeArgs)};


DEFINE_CA_CTOR_ARGS(ComCompatibleVersionAttribute)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I4)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I4)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I4)
    DEFINE_CA_CTOR_ARG(SERIALIZATION_TYPE_I4)
DEFINE_CA_CTOR_ARGS_END()

const KnownCaProp ComCompatibleVersionAttributeProps = {"System.Runtime.InteropServices", "ComCompatibleVersionAttribute", ComCompatibleVersionTargets, bKEEPCA,
                                                  rComCompatibleVersionAttributeArgs, ARRAY_SIZE(rComCompatibleVersionAttributeArgs)};


//-----------------------------------------------------------------------------
// APTCA args (none), named args (none), and known attribute properties.
const KnownCaProp AllowPartiallyTrustedCallersAttributeProps         = {"System.Security", "AllowPartiallyTrustedCallersAttribute", AllowPartiallyTrustedCallersTargets, bKEEPCA};



//-----------------------------------------------------------------------------
// Array of known custom attribute properties
#undef KnownCa
#define KnownCa(x) &x##Props,
const KnownCaProp * const rKnownCaProps[CA_COUNT] =
{
    KnownCaList()
};

//*****************************************************************************
// Helper to turn on or off a single bit in a bitmask.
//*****************************************************************************
template<class T> FORCEINLINE void SetBitValue(T &bitmask, T bit, int bVal)
{
    if (bVal)
        bitmask |= bit;
    else
        bitmask &= ~bit;
} // template<class T> FORCEINLINE void SetBitValue()

HRESULT ParseEncodedType(
    CustomAttributeParser &ca,
    CaType* pCaType)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pCaType));
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    CorSerializationType* pType = &pCaType->tag;

    IfFailGo(ca.GetTag(pType));

    if (*pType == SERIALIZATION_TYPE_SZARRAY)
    {
        IfFailGo(ca.GetTag(&pCaType->arrayType));
        pType = &pCaType->arrayType;
    }

    if (*pType == SERIALIZATION_TYPE_ENUM)
    {
        // We cannot determine the underlying type without loading the Enum.
        pCaType->enumType = SERIALIZATION_TYPE_UNDEFINED;
        IfFailGo(ca.GetNonNullString(&pCaType->szEnumName, &pCaType->cEnumName));
    }

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Helper to parse the values for the ctor argument list and the named argument list.
//

HRESULT ParseKnownCaValue(
    CustomAttributeParser &ca,
    CaValue* pCaArg,
    CaType* pCaParam)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pCaArg));
        PRECONDITION(CheckPointer(pCaParam));
        PRECONDITION(pCaParam->tag != SERIALIZATION_TYPE_TAGGED_OBJECT && pCaParam->tag != SERIALIZATION_TYPE_SZARRAY);
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    CorSerializationType underlyingType;

    pCaArg->type = *pCaParam;

    underlyingType = pCaArg->type.tag == SERIALIZATION_TYPE_ENUM ? pCaArg->type.enumType : pCaArg->type.tag;

    // Grab the value.
    switch (underlyingType)
    {
    case SERIALIZATION_TYPE_BOOLEAN:
    case SERIALIZATION_TYPE_I1:
    case SERIALIZATION_TYPE_U1:
        IfFailGo(ca.GetU1(&pCaArg->u1));
        break;

    case SERIALIZATION_TYPE_CHAR:
    case SERIALIZATION_TYPE_I2:
    case SERIALIZATION_TYPE_U2:
        IfFailGo(ca.GetU2(&pCaArg->u2));
        break;

    case SERIALIZATION_TYPE_I4:
    case SERIALIZATION_TYPE_U4:
        IfFailGo(ca.GetU4(&pCaArg->u4));
        break;

    case SERIALIZATION_TYPE_I8:
    case SERIALIZATION_TYPE_U8:
        IfFailGo(ca.GetU8(&pCaArg->u8));
        break;

    case SERIALIZATION_TYPE_R4:
        IfFailGo(ca.GetR4(&pCaArg->r4));
        break;

    case SERIALIZATION_TYPE_R8:
        IfFailGo(ca.GetR8(&pCaArg->r8));
        break;

    case SERIALIZATION_TYPE_STRING:
    case SERIALIZATION_TYPE_TYPE:
        IfFailGo(ca.GetString(&pCaArg->str.pStr, &pCaArg->str.cbStr));
        break;

    default:
        // The arguments of all known custom attributes are Type, String, Enum, or primitive types.
        _ASSERTE(!"Unexpected internal error");
        hr = E_FAIL;
        break;
    } // End switch

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Helper to parse the nanmed argument list.
// This function is used by code:RegMeta::DefineCustomAttribute for IMetaDataEmit2 and
// should not have any VM dependency.
//

HRESULT ParseKnownCaNamedArgs(
    CustomAttributeParser &ca,          // The Custom Attribute blob.
    CaNamedArg  *pNamedParams,            // Array of argument descriptors.
    ULONG       cNamedParams)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;
    ULONG ixParam;
    INT32 ixArg;
    INT16 cActualArgs;
    CaNamedArgCtor namedArg;
    CaNamedArg* pNamedParam;

    // Get actual count of named arguments.
    if (FAILED(ca.GetI2(&cActualArgs)))
        cActualArgs = 0; // Everett behavior

    for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        pNamedParams[ixParam].val.type.tag = SERIALIZATION_TYPE_UNDEFINED;

    // For each named argument...
    for (ixArg = 0; ixArg < cActualArgs; ixArg++)
    {
        // Field or property?
        IfFailGo(ca.GetTag(&namedArg.propertyOrField));
        if (namedArg.propertyOrField != SERIALIZATION_TYPE_FIELD && namedArg.propertyOrField != SERIALIZATION_TYPE_PROPERTY)
            IfFailGo(PostError(META_E_CA_INVALID_ARGTYPE));

        // Get argument type information
        IfFailGo(ParseEncodedType(ca, &namedArg.type));

        // Get name of Arg.
        if (FAILED(ca.GetNonEmptyString(&namedArg.szName, &namedArg.cName)))
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));

        // Match arg by name and type
        for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        {
            pNamedParam = &pNamedParams[ixParam];

            // Match type
            if (pNamedParam->type.tag != SERIALIZATION_TYPE_TAGGED_OBJECT)
            {
                if (namedArg.type.tag != pNamedParam->type.tag)
                    continue;

                // Match array type
                if (namedArg.type.tag == SERIALIZATION_TYPE_SZARRAY &&
                    pNamedParam->type.arrayType != SERIALIZATION_TYPE_TAGGED_OBJECT &&
                    namedArg.type.arrayType != pNamedParam->type.arrayType)
                    continue;
            }

            // Match name (and its length to avoid substring matching)
            if ((pNamedParam->cName != namedArg.cName) ||
                (strncmp(pNamedParam->szName, namedArg.szName, namedArg.cName) != 0))
            {
                continue;
            }

            // If enum, match enum name.
            if (pNamedParam->type.tag == SERIALIZATION_TYPE_ENUM ||
                (pNamedParam->type.tag == SERIALIZATION_TYPE_SZARRAY && pNamedParam->type.arrayType == SERIALIZATION_TYPE_ENUM ))
            {
                if (pNamedParam->type.cEnumName > namedArg.type.cEnumName)
                    continue; // name cannot possibly match

                if (strncmp(pNamedParam->type.szEnumName, namedArg.type.szEnumName, pNamedParam->type.cEnumName) != 0 ||
                    (pNamedParam->type.cEnumName < namedArg.type.cEnumName &&
                     namedArg.type.szEnumName[pNamedParam->type.cEnumName] != ','))
                    continue;

                // TODO: For now assume the property\field array size is correct - later we should verify this
                namedArg.type.enumType = pNamedParam->type.enumType;
            }

            // Found a match.
            break;
        }

        // Better have found an argument.
        if (ixParam == cNamedParams)
        {
            IfFailGo(PostError(META_E_CA_UNKNOWN_ARGUMENT, namedArg.cName, namedArg.szName));
        }

        // Argument had better not have been seen already.
        if (pNamedParams[ixParam].val.type.tag != SERIALIZATION_TYPE_UNDEFINED)
        {
            IfFailGo(PostError(META_E_CA_REPEATED_ARG, namedArg.cName, namedArg.szName));
        }

        IfFailGo(ParseKnownCaValue(ca, &pNamedParams[ixParam].val, &namedArg.type));
    }

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Helper to parse the ctor argument list.
// This function is used by code:RegMeta::DefineCustomAttribute for IMetaDataEmit2 and
// should not have any VM dependency.
//

HRESULT ParseKnownCaArgs(
    CustomAttributeParser &ca,          // The Custom Attribute blob.
    CaArg* pArgs,                 // Array of argument descriptors.
    ULONG cArgs)                  // Count of argument descriptors.
{
    WRAPPER_NO_CONTRACT;

    HRESULT     hr = S_OK;              // A result.
    ULONG       ix;                     // Loop control.

    // If there is a blob, check the prolog.
    if (FAILED(ca.ValidateProlog()))
    {
        IfFailGo(PostError(META_E_CA_INVALID_BLOB));
    }

    // For each expected arg...
    for (ix=0; ix<cArgs; ++ix)
    {
        CaArg* pArg = &pArgs[ix];
        IfFailGo(ParseKnownCaValue(ca, &pArg->val, &pArg->type));
    }

ErrExit:
    return hr;
} // ParseKnownCaArgs

//*****************************************************************************
// Create a CustomAttribute record from a blob with the specified parent.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineCustomAttribute(
    mdToken     tkOwner,                // [IN] The object to put the value on.
    mdToken     tkCtor,                 // [IN] Constructor of the CustomAttribute type (MemberRef/MethodDef).
    void const  *pCustomAttribute,      // [IN] Custom Attribute data.
    ULONG       cbCustomAttribute,      // [IN] Size of custom Attribute data.
    mdCustomAttribute *pcv)             // [OUT, OPTIONAL] Put custom Attribute token here.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CustomAttributeRec  *pRecord = NULL; // New custom Attribute record.
    RID         iRecord;                // New custom Attribute RID.
    CMiniMdRW   *pMiniMd = &m_pStgdb->m_MiniMd;
    int         ixKnown;                // Index of known custom attribute.

    LOG((LOGMD, "RegMeta::DefineCustomAttribute(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n", tkOwner, tkCtor,
            pCustomAttribute, cbCustomAttribute, pcv));
    START_MD_PERF();
    LOCKWRITE();

    _ASSERTE(TypeFromToken(tkCtor) == mdtMethodDef || TypeFromToken(tkCtor) == mdtMemberRef);

    if (TypeFromToken(tkOwner) == mdtCustomAttribute)
        IfFailGo(E_INVALIDARG);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    if (IsNilToken(tkOwner) ||
        IsNilToken(tkCtor) ||
        (TypeFromToken(tkCtor) != mdtMethodDef &&
        TypeFromToken(tkCtor) != mdtMemberRef) )
    {
        IfFailGo(E_INVALIDARG);
    }

    // See if this is a known custom attribute.
    IfFailGo(_IsKnownCustomAttribute(tkCtor, &ixKnown));
    if (ixKnown != 0)
    {
        int bKeep = false;
        hr = _HandleKnownCustomAttribute(tkOwner, pCustomAttribute, cbCustomAttribute, ixKnown, &bKeep);
        if (pcv)
            *pcv = mdCustomAttributeNil;
        IfFailGo(hr);
        if (!bKeep)
            goto ErrExit;
    }

    if (((TypeFromToken(tkOwner) == mdtTypeDef) || (TypeFromToken(tkOwner) == mdtMethodDef)) &&
        (TypeFromToken(tkCtor) == mdtMethodDef || TypeFromToken(tkCtor) == mdtMemberRef))
    {
        CHAR        szBuffer[MAX_CLASS_NAME + 1];
        LPSTR       szName = szBuffer;
        LPCSTR      szNamespace;
        LPCSTR      szClass;
        TypeRefRec  *pTypeRefRec = NULL;
        TypeDefRec  *pTypeDefRec = NULL;
        mdToken     tkParent;

        if (TypeFromToken(tkCtor) == mdtMemberRef)
        {
            MemberRefRec *pMemberRefRec;
            IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tkCtor), &pMemberRefRec));
            tkParent = pMiniMd->getClassOfMemberRef(pMemberRefRec);
            if (TypeFromToken(tkParent) == mdtTypeRef)
            {
                IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tkParent), &pTypeRefRec));
                IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));
                IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szClass));
                ns::MakePath(szName, sizeof(szBuffer) - 1, szNamespace, szClass);
            }
            else if (TypeFromToken(tkParent) == mdtTypeDef)
            {
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeDefRec));
            }
        }
        else
        {
            IfFailGo(pMiniMd->FindParentOfMethodHelper(tkCtor, &tkParent));
            IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeDefRec));
        }

        if (pTypeDefRec != NULL)
        {
            IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeDefRec, &szNamespace));
            IfFailGo(pMiniMd->getNameOfTypeDef(pTypeDefRec, &szClass));
            ns::MakePath(szName, sizeof(szBuffer) - 1, szNamespace, szClass);
        }

        if ((TypeFromToken(tkOwner) == mdtMethodDef) && strcmp(szName, COR_REQUIRES_SECOBJ_ATTRIBUTE_ANSI) == 0)
        {
            // Turn REQ_SO attribute into flag bit on the methoddef.
            MethodRec *pMethod;
            IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tkOwner), &pMethod));
            pMethod->AddFlags(mdRequireSecObject);
            IfFailGo(UpdateENCLog(tkOwner));
            goto ErrExit;
        }
        else if (strcmp(szName, COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI) == 0)
        {
            // If we spot an unmanged code check suppression attribute, turn on
            // the bit that says there's declarative security on the
            // class/method, but still write the attribute itself.
            if (TypeFromToken(tkOwner) == mdtTypeDef)
            {
                IfFailGo(_TurnInternalFlagsOn(tkOwner, tdHasSecurity));
            }
            else if (TypeFromToken(tkOwner) == mdtMethodDef)
            {
                IfFailGo(_TurnInternalFlagsOn(tkOwner, mdHasSecurity));
            }
            IfFailGo(UpdateENCLog(tkOwner));
        }
    }

    IfFailGo(m_pStgdb->m_MiniMd.AddCustomAttributeRecord(&pRecord, &iRecord));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_CustomAttribute, CustomAttributeRec::COL_Type, pRecord, tkCtor));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_CustomAttribute, CustomAttributeRec::COL_Parent, pRecord, tkOwner));

    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_CustomAttribute, CustomAttributeRec::COL_Value, pRecord, pCustomAttribute, cbCustomAttribute));

    // Give token back to caller.
    if (pcv != NULL)
        *pcv = TokenFromRid(iRecord, mdtCustomAttribute);

    IfFailGo(m_pStgdb->m_MiniMd.AddCustomAttributesToHash(TokenFromRid(iRecord, mdtCustomAttribute)) );

    IfFailGo(UpdateENCLog(TokenFromRid(iRecord, mdtCustomAttribute)));

ErrExit:
    STOP_MD_PERF(DefineCustomAttribute);
    END_ENTRYPOINT_NOTHROW;

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineCustomAttribute

//*****************************************************************************
// Replace the blob of an existing custom attribute.
//*****************************************************************************
STDMETHODIMP RegMeta::SetCustomAttributeValue(  // Return code.
    mdCustomAttribute tkAttr,               // [IN] The object to be Attributed.
    void const  *pCustomAttribute,          // [IN] Custom Attribute data.
    ULONG       cbCustomAttribute)          // [IN] Size of custom Attribute data.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    CustomAttributeRec  *pRecord = NULL;// Existing custom Attribute record.

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tkAttr) == mdtCustomAttribute && !InvalidRid(tkAttr));

    // Retrieve and update the custom value.
    IfFailGo(m_pStgdb->m_MiniMd.GetCustomAttributeRecord(RidFromToken(tkAttr), &pRecord));
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_CustomAttribute, CustomAttributeRec::COL_Value, pRecord, pCustomAttribute, cbCustomAttribute));

    IfFailGo(UpdateENCLog(tkAttr));
ErrExit:

    STOP_MD_PERF(SetCustomAttributeValue);
    END_ENTRYPOINT_NOTHROW;

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetCustomAttributeValue

//*****************************************************************************
//*****************************************************************************
HRESULT RegMeta::_IsKnownCustomAttribute(        // S_OK, S_FALSE, or error.
    mdToken     tkCtor,                 // [IN] Token of custom attribute's constructor.
    int         *pca)                   // [OUT] Put value from KnownCustAttr enum here.
{
    HRESULT     hr = S_OK;              // A result.
    CCustAttrHashKey sLookup;           // For looking up a custom attribute.
    CCustAttrHashKey *pFound;           // Result of a lookup.
    LPCSTR      szNamespace = "";       // Namespace of custom attribute type.
    LPCSTR      szName = "";            // Name of custom attribute type.
    TypeDefRec  *pTypeDefRec = NULL;    // Parent record, when a TypeDef.
    TypeRefRec  *pTypeRefRec = NULL;    // Parent record, when a TypeRef.
    CMiniMdRW   *pMiniMd = &m_pStgdb->m_MiniMd;
    int         ixCa;                   // Index of Known CustomAttribute, or 0.
    int         i;                      // Loop control.
    mdToken     tkParent;

    *pca = 0;

    // Only for Custom Attributes.
    _ASSERTE(TypeFromToken(tkCtor) != mdtTypeRef && TypeFromToken(tkCtor) != mdtTypeDef);

    sLookup.tkType = tkCtor;

    // See if this custom attribute type has been seen before.
    if ((pFound = m_caHash.Find(&sLookup)))
    {   // Yes, already seen.
        *pca = pFound->ca;
        hr = (pFound->ca == CA_UNKNOWN) ? S_FALSE : S_OK;
        goto ErrExit;
    }

    // Hasn't been seen before.  See if it is well known.

    // Get the CA name.
    if (TypeFromToken(tkCtor) == mdtMemberRef)
    {
        MemberRefRec *pMember;
        IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tkCtor), &pMember));
        tkParent = pMiniMd->getClassOfMemberRef(pMember);
        if (TypeFromToken(tkParent) == mdtTypeRef)
        {
            IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tkParent), &pTypeRefRec));
            IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));
            IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szName));
        }
        else if (TypeFromToken(tkParent) == mdtTypeDef)
            IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeDefRec));
    }
    else
    {
        IfFailGo(pMiniMd->FindParentOfMethodHelper(tkCtor, &tkParent));
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeDefRec));
    }

    if (pTypeDefRec)
    {
        IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeDefRec, &szNamespace));
        IfFailGo(pMiniMd->getNameOfTypeDef(pTypeDefRec, &szName));
    }

    // Search in list of Known CAs.
    for (ixCa=0, i=1; i<CA_COUNT; ++i)
    {
        if (strcmp(szName, rKnownCaProps[i]->szName) != 0)
            continue;
        if (strcmp(szNamespace, rKnownCaProps[i]->szNamespace) == 0)
        {
            // Some custom attributes have overloaded ctors.  For those,
            //  see if this is the matching overload.
            if (rKnownCaProps[i]->bMatchBySig)
            {
                // Name matches.  Does the signature?
                PCCOR_SIGNATURE pSig = NULL;            // Signature of a method.
                ULONG       cbSig = 0;                  // Size of the signature.
                ULONG       cParams;                    // Count of signature parameters.
                ULONG       cb;                         // Size of an element
                ULONG       elem;                       // Signature element.
                ULONG       j;                          // Loop control.

                // Get the signature.
                if (TypeFromToken(tkCtor) == mdtMemberRef)
                {
                    MemberRefRec *pMember;
                    IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tkCtor), &pMember));
                    IfFailGo(pMiniMd->getSignatureOfMemberRef(pMember, &pSig, &cbSig));
                }
                else
                {
                    MethodRec *pMethod;
                    IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(tkCtor), &pMethod));
                    IfFailGo(pMiniMd->getSignatureOfMethod(pMethod, &pSig, &cbSig));
                }

                // Skip calling convention.
                cb = CorSigUncompressData(pSig, &elem);
                pSig += cb;
                cbSig -= cb;
                // Count of params.
                cb = CorSigUncompressData(pSig, &cParams);
                pSig += cb;
                cbSig -= cb;

                // If param count mismatch, not the right CA.
                if (cParams != rKnownCaProps[i]->cArgs)
                    continue;

                // Count is fine, check each param.  Skip return type (better be void).
                cb = CorSigUncompressData(pSig, &elem);
                _ASSERTE(elem == ELEMENT_TYPE_VOID);
                pSig += cb;
                cbSig -= cb;
                for (j=0; j<cParams; ++j)
                {
                    // Get next element from method signature.
                    cb = CorSigUncompressData(pSig, &elem);
                    pSig += cb;
                    cbSig -= cb;
                    if (rKnownCaProps[i]->pArgs[j].type.tag != (CorSerializationType) elem)
                        break;
                }

                // All matched?
                if (j != cParams)
                    continue;
            }
            // All matched.
            ixCa = i;
            break;
        }
    }

    // Add to hash.
    sLookup.ca = ixCa;
    pFound = m_caHash.Add(&sLookup);
    IfNullGo(pFound);
    *pFound = sLookup;
    *pca = ixCa;

ErrExit:
    return hr;
} // RegMeta::_IsKnownCustomAttribute

//*****************************************************************************
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT RegMeta::_HandleKnownCustomAttribute(    // S_OK or error.
    mdToken     tkObj,                  // [IN] Object being attributed.
    const void  *pData,                 // [IN] Custom Attribute data blob.
    ULONG       cbData,                 // [IN] Count of bytes in the data.
    int         ixCa,                   // [IN] Value from KnownCustAttr enum.
    int         *bKeep)                 // [OUT] If true, keep the CA after processing.
{
    HRESULT     hr = S_OK;              // A result.
    ULONG       ixTbl;                  // Index of table with object.
    void        *pRow;                  // Whatever sort of record it is.
    CMiniMdRW   *pMiniMd = &m_pStgdb->m_MiniMd;
    mdToken     tkObjType;              // Type of the object.
    ULONG       ix;                     // Loop control.
    KnownCaProp const *props=rKnownCaProps[ixCa]; // For convenience.
    CustomAttributeParser ca(pData, cbData);
    CQuickArray<CaArg>      qArgs;      // Un-named arguments.
    CQuickArray<CaNamedArg> qNamedArgs; // Named arguments.
    CQuickArray<BYTE>       qNativeType;// Native type string.

    _ASSERTE(ixCa > 0 && ixCa < CA_COUNT);
    *bKeep = props->bKeepCa || m_bKeepKnownCa;

    // Validate that target is valid for attribute.
    tkObjType = TypeFromToken(tkObj);
    for (ix=0; props->rTypes[ix] != (mdToken) -1; ++ix)
    {
        if (props->rTypes[ix] == tkObjType)
            break;
    }
    // Was the type found in list of valid targets?
    if (props->rTypes[ix] == (mdToken) -1)
    {   // No, error.
        IfFailGo(PostError(META_E_CA_INVALID_TARGET));
    }
    // Get the row.
    ixTbl = pMiniMd->GetTblForToken(tkObj);
    _ASSERTE(ixTbl >= 0 && ixTbl <= pMiniMd->GetCountTables());
    IfFailGo(pMiniMd->getRow(ixTbl, RidFromToken(tkObj), &pRow));

    // If this custom attribute expects any args...
    if (props->cArgs || props->cNamedArgs)
    {   // Initialize array ctor arg descriptors.
        IfFailGo(qArgs.ReSizeNoThrow(props->cArgs));
        for (ix=0; ix<props->cArgs; ++ix)
            qArgs[ix] = props->pArgs[ix];
        // Parse any ctor args (unnamed, fixed args).
        IfFailGo(ParseKnownCaArgs(ca, qArgs.Ptr(), props->cArgs));

        // If this custom attribute accepts named args, parse them, or if there
        //  are unused bytes, parse them.
        if (props->cNamedArgs || ca.BytesLeft() > 0)
        {   // Initialize array of named arg descriptors.
            IfFailGo(qNamedArgs.ReSizeNoThrow(props->cNamedArgs));
            for (ix=0; ix<props->cNamedArgs; ++ix)
                qNamedArgs[ix] = props->pNamedArgs[ix];
            // Parse named args.
            IfFailGo(ParseKnownCaNamedArgs(ca, qNamedArgs.Ptr(), props->cNamedArgs));
        }
    }

    switch (ixCa)
    {
    case CA_DllImportAttribute:
        {
        // Validate parameters.
        if (qArgs[0].val.str.cbStr == 0 || qArgs[0].val.str.pStr == NULL)
        {
            // no name for DllImport.
            IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        }

        // Retrieve / create a ModuleRef on the dll name.
        mdModuleRef mrModule;
        CQuickArray<char> qDllName;
        IfFailGo(qDllName.ReSizeNoThrow(qArgs[0].val.str.cbStr+1));
        memcpy(qDllName.Ptr(),  qArgs[0].val.str.pStr, qArgs[0].val.str.cbStr);
        qDllName[qArgs[0].val.str.cbStr] = '\0';
        hr = ImportHelper::FindModuleRef(pMiniMd, qDllName.Ptr(), &mrModule);
        if (hr != S_OK)
        {
            MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzDllName, qDllName.Ptr());
            if (wzDllName == NULL)
                IfFailGo(PostError(META_E_CA_INVALID_VALUE));
            IfFailGo(_DefineModuleRef(wzDllName, &mrModule));
        }

        // Create a p/invoke map entry.
        ULONG dwFlags; dwFlags=0;
        // Was a calling convention set?
        if (qNamedArgs[DI_CallingConvention].val.type.tag)
        {   // Calling convention makes no sense on a field.
            if (TypeFromToken(tkObj) == mdtFieldDef)
            {
                IfFailGo(PostError(META_E_CA_INVALID_ARG_FOR_TYPE, qNamedArgs[DI_CallingConvention].szName));
            }
            // Turn off all callconv bits, then turn on specified value.
            dwFlags &= ~pmCallConvMask;
            switch (qNamedArgs[DI_CallingConvention].val.u4)
            { //<TODO>@future: sigh.  keep in sync with System.Runtime.InteropServices.CallingConvention</TODO>
            case 0: break;  // 0 means "do nothing"
            case 1: dwFlags |= pmCallConvWinapi;   break;
            case 2: dwFlags |= pmCallConvCdecl;    break;
            case 3: dwFlags |= pmCallConvStdcall;  break;
            case 4: dwFlags |= pmCallConvThiscall; break;
            case 5: dwFlags |= pmCallConvFastcall; break;
            default:
                _ASSERTE(!"Flags are out of sync! ");
                break;
            }
        }
        else
        if (TypeFromToken(tkObj) == mdtMethodDef)
        {   // No calling convention specified for a method.  Default to pmCallConvWinApi.
            dwFlags = (dwFlags & ~pmCallConvMask) | pmCallConvWinapi;
        }

        // Charset
        if (qNamedArgs[DI_CharSet].val.type.tag)
        {   // Turn of all charset bits, then turn on specified bits.
            dwFlags &= ~pmCharSetMask;
            switch (qNamedArgs[DI_CharSet].val.u4)
            { //<TODO>@future: keep in sync with System.Runtime.InteropServices.CharSet</TODO>
            case 0: break;  // 0 means "do nothing"
            case 1: dwFlags |= pmCharSetNotSpec; break;
            case 2: dwFlags |= pmCharSetAnsi;    break;
            case 3: dwFlags |= pmCharSetUnicode; break;
            case 4: dwFlags |= pmCharSetAuto;    break;
            default:
                _ASSERTE(!"Flags are out of sync! ");
                break;
            }
        }
        if (qNamedArgs[DI_ExactSpelling].val.u1)
            dwFlags |= pmNoMangle;
        if (qNamedArgs[DI_SetLastError].val.type.tag)
        {   // SetLastError makes no sense on a field.
            if (TypeFromToken(tkObj) == mdtFieldDef)
            {
                IfFailGo(PostError(META_E_CA_INVALID_ARG_FOR_TYPE, qNamedArgs[DI_SetLastError].szName));
            }
            if (qNamedArgs[DI_SetLastError].val.u1)
                dwFlags |= pmSupportsLastError;
        }

        // If an entrypoint name was specified, use it, otherwise grab the name from the member.
        LPCWSTR wzEntry;
        if (qNamedArgs[DI_EntryPoint].val.type.tag)
        {
            if (qNamedArgs[DI_EntryPoint].val.str.cbStr > 0)
            {
                MAKE_WIDEPTR_FROMUTF8N_NOTHROW(wzEntryName, qNamedArgs[DI_EntryPoint].val.str.pStr, qNamedArgs[DI_EntryPoint].val.str.cbStr);
                if (wzEntryName == NULL)
                    IfFailGo(PostError(META_E_CA_INVALID_VALUE));
                wzEntry = wzEntryName;
            }
            else
                wzEntry = W("");
        }
        else
        {
            LPCUTF8 szMember = NULL;
            if (TypeFromToken(tkObj) == mdtMethodDef)
            {
                IfFailGo(pMiniMd->getNameOfMethod(reinterpret_cast<MethodRec*>(pRow), &szMember));
            }
            MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzMemberName, szMember);
            if (wzMemberName == NULL)
                IfFailGo(PostError(META_E_CA_INVALID_VALUE));
            wzEntry = wzMemberName;
        }

        // Set the miPreserveSig bit based on the value of the preserve sig flag.
        if (qNamedArgs[DI_PreserveSig].val.type.tag && !qNamedArgs[DI_PreserveSig].val.u1)
            reinterpret_cast<MethodRec*>(pRow)->RemoveImplFlags(miPreserveSig);
        else
            reinterpret_cast<MethodRec*>(pRow)->AddImplFlags(miPreserveSig);

        if (qNamedArgs[DI_BestFitMapping].val.type.tag)
        {
            if (qNamedArgs[DI_BestFitMapping].val.u1)
                dwFlags |= pmBestFitEnabled;
            else
                dwFlags |= pmBestFitDisabled;
        }

        if (qNamedArgs[DI_ThrowOnUnmappableChar].val.type.tag)
        {
            if (qNamedArgs[DI_ThrowOnUnmappableChar].val.u1)
                dwFlags |= pmThrowOnUnmappableCharEnabled;
            else
                dwFlags |= pmThrowOnUnmappableCharDisabled;
        }

        // Finally, create the PInvokeMap entry.,
        IfFailGo(_DefinePinvokeMap(tkObj, dwFlags, wzEntry, mrModule));
        goto ErrExit;
        }
        break;

    case CA_GuidAttribute:
        { // Just verify the attribute.  It still gets stored as a real custom attribute.
        // format is "{01234567-0123-0123-0123-001122334455}"
        GUID guid;
        WCHAR wzGuid[40];
        int cch = qArgs[0].val.str.cbStr;

        // Guid should be 36 characters; need to add curlies.
        if (cch == 36)
        {
            WszMultiByteToWideChar(CP_UTF8, 0, qArgs[0].val.str.pStr,cch, wzGuid+1,39);
            wzGuid[0] = '{';
            wzGuid[37] = '}';
            wzGuid[38] = 0;
            hr = IIDFromString(wzGuid, &guid);
        }
        else
            hr = META_E_CA_INVALID_UUID;
        if (hr != S_OK)
            IfFailGo(PostError(META_E_CA_INVALID_UUID));
        goto ErrExit;
        }
        break;

    case CA_ComImportAttribute:
        reinterpret_cast<TypeDefRec*>(pRow)->AddFlags(tdImport);
        break;

    case CA_InterfaceTypeAttribute:
        {
            // Verify the attribute.
            if (qArgs[0].val.u2 >= ifLast)
                IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        }
        break;

    case CA_ClassInterfaceAttribute:
        {
            // Verify the attribute.
            if (qArgs[0].val.u2 >= clsIfLast)
                IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        }
        break;

    case CA_SpecialNameAttribute:

        switch (TypeFromToken(tkObj))
        {
            case mdtTypeDef:
                reinterpret_cast<TypeDefRec*>(pRow)->AddFlags(tdSpecialName);
                break;

            case mdtMethodDef:
                reinterpret_cast<MethodRec*>(pRow)->AddFlags(mdSpecialName);
                break;

            case mdtFieldDef:
                reinterpret_cast<FieldRec*>(pRow)->AddFlags(fdSpecialName);
                break;

            case mdtProperty:
                reinterpret_cast<PropertyRec*>(pRow)->AddPropFlags(prSpecialName);
                break;

            case mdtEvent:
                reinterpret_cast<EventRec*>(pRow)->AddEventFlags(evSpecialName);
                break;

            default:
                _ASSERTE(!"Unfamilar type for SpecialName custom attribute");
                IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        }

        break;
    case CA_SerializableAttribute:
        reinterpret_cast<TypeDefRec*>(pRow)->AddFlags(tdSerializable);
        break;

    case CA_NonSerializedAttribute:
        reinterpret_cast<FieldRec*>(pRow)->AddFlags(fdNotSerialized);
        break;

    case CA_InAttribute:
        reinterpret_cast<ParamRec*>(pRow)->AddFlags(pdIn);
        break;

    case CA_OutAttribute:
        reinterpret_cast<ParamRec*>(pRow)->AddFlags(pdOut);
        break;

    case CA_OptionalAttribute:
        reinterpret_cast<ParamRec*>(pRow)->AddFlags(pdOptional);
        break;

    case CA_MethodImplAttribute2:
        // Force to wider value.
        qArgs[0].val.u4 = (unsigned)qArgs[0].val.i2;
        // Fall through to validation.
        FALLTHROUGH;
    case CA_MethodImplAttribute3:
        // Validate bits.
        if (qArgs[0].val.u2 & ~(miUserMask))
            IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        reinterpret_cast<MethodRec*>(pRow)->AddImplFlags(qArgs[0].val.u2);
        if (!qNamedArgs[MI_CodeType].val.type.tag)
            break;
        // fall through to set the code type.
        FALLTHROUGH;
    case CA_MethodImplAttribute1:
        {
        USHORT usFlags = reinterpret_cast<MethodRec*>(pRow)->GetImplFlags();
        if (qNamedArgs[MI_CodeType].val.u2 & ~(miCodeTypeMask))
            IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        // Mask out old value, put in new one.
        usFlags = (usFlags & ~miCodeTypeMask) | qNamedArgs[MI_CodeType].val.u2;
        reinterpret_cast<MethodRec*>(pRow)->SetImplFlags(usFlags);
        }
        break;

    case CA_MarshalAsAttribute1:
        // Force the U2 to a wider U4 value explicitly.
        qArgs[0].val.u4 = qArgs[0].val.u2;
        // Fall through to handle the CA.
        FALLTHROUGH;
    case CA_MarshalAsAttribute2:
        IfFailGo(_HandleNativeTypeCustomAttribute(tkObj, qArgs.Ptr(), qNamedArgs.Ptr(), qNativeType));
        break;

    case CA_PreserveSigAttribute:
        reinterpret_cast<MethodRec*>(pRow)->AddImplFlags(miPreserveSig);
        break;

    case CA_StructLayoutAttribute1:
        {
        // Convert the I2 to a U2, then wide to an I4, then fall through.
        qArgs[0].val.i4 = static_cast<int>(static_cast<USHORT>(qArgs[0].val.i2));
        }
        FALLTHROUGH;
    case CA_StructLayoutAttribute2:
        {
        // Get a copy of the flags to work with.
        ULONG dwFlags;
        dwFlags = reinterpret_cast<TypeDefRec*>(pRow)->GetFlags();
        // Class layout.  Keep in sync with LayoutKind.
        switch (qArgs[0].val.i4)
        {
        case 0: // tdSequentialLayout:
            dwFlags = (dwFlags & ~tdLayoutMask) | tdSequentialLayout;
            break;
        case 2: // tdExplicitLayout:
            dwFlags = (dwFlags & ~tdLayoutMask) | tdExplicitLayout;
            break;
        case 3: // tdAutoLayout:
            dwFlags = (dwFlags & ~tdLayoutMask) | tdAutoLayout;
            break;
        default:
            IfFailGo(PostError(META_E_CA_INVALID_VALUE));
            break;
        }

        // Class packing and size.
        ULONG ulSize, ulPack;
        ulPack = ulSize = UINT32_MAX;
        if (qNamedArgs[SL_Pack].val.type.tag)
        {    // Only 1,2,4,8,16,32,64,128 are legal values.
             ulPack = qNamedArgs[SL_Pack].val.u4;
             if ((ulPack > 128) ||
                 (ulPack & (ulPack-1)))
                 IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        }
        if (qNamedArgs[SL_Size].val.type.tag)
        {
            if (qNamedArgs[SL_Size].val.u4 > INT_MAX)
                IfFailGo(PostError(META_E_CA_INVALID_VALUE));
            ulSize = qNamedArgs[SL_Size].val.u4;
        }
        if (ulPack!=UINT32_MAX || ulSize!=UINT32_MAX)
            IfFailGo(_SetClassLayout(tkObj, ulPack, ulSize));

        // Class character set.
        if (qNamedArgs[SL_CharSet].val.type.tag)
        {
            switch (qNamedArgs[SL_CharSet].val.u4)
            {
            //case 1: // Not specified.
            //    IfFailGo(PostError(META_E_CA_INVALID_VALUE));
            //    break;
            case 2: // ANSI
                dwFlags = (dwFlags & ~tdStringFormatMask) | tdAnsiClass;
                break;
            case 3: // Unicode
                dwFlags = (dwFlags & ~tdStringFormatMask) | tdUnicodeClass;
                break;
            case 4: // Auto
                dwFlags = (dwFlags & ~tdStringFormatMask) | tdAutoClass;
                break;
            default:
                IfFailGo(PostError(META_E_CA_INVALID_VALUE));
                break;
            }
        }

        // Persist possibly-changed value of flags.
        reinterpret_cast<TypeDefRec*>(pRow)->SetFlags(dwFlags);
        }
        break;

    case CA_FieldOffsetAttribute:
        if (qArgs[0].val.u4 > INT_MAX)
            IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        IfFailGo(_SetFieldOffset(tkObj, qArgs[0].val.u4));
        break;

    case CA_TypeLibVersionAttribute:
        if ((qArgs[0].val.i4 < 0) || (qArgs[1].val.i4 < 0))
            IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        break;

    case CA_ComCompatibleVersionAttribute:
        if ( (qArgs[0].val.i4 < 0) || (qArgs[1].val.i4 < 0) || (qArgs[2].val.i4 < 0) || (qArgs[3].val.i4 < 0) )
            IfFailGo(PostError(META_E_CA_INVALID_VALUE));
        break;

    case CA_AllowPartiallyTrustedCallersAttribute:
        break;

    case CA_WindowsRuntimeImportAttribute:
        reinterpret_cast<TypeDefRec*>(pRow)->AddFlags(tdWindowsRuntime);
        break;

    default:
        _ASSERTE(!"Unexpected custom attribute type");
        // Turn into ordinary custom attribute.
        *bKeep = true;
        hr = S_OK;
        goto ErrExit;
        break;
    }

    IfFailGo(UpdateENCLog(tkObj));

ErrExit:
    return hr;
} // RegMeta::_HandleKnownCustomAttribute
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT RegMeta::_HandleNativeTypeCustomAttribute(// S_OK or error.
    mdToken     tkObj,                  // The token this CA is applied on.
    CaArg       *pArgs,                 // Pointer to args.
    CaNamedArg  *pNamedArgs,            // Pointer to named args.
    CQuickArray<BYTE> &qNativeType)     // Native type is built here.
{
    HRESULT     hr = S_OK;              // A result.
    int         cch = 0;                // Size of a string argument.
    ULONG       cb;                     // Count of some character operation.
    ULONG       cbNative;               // Size of native type string.
    ULONG       cbMax;                  // Max size of native type string.
    BYTE        *pbNative;              // Pointer into native type buffer.
    mdToken     tkObjType;              // The type of the token.
    mdToken     tkSetter;               // Token for Property setter.
    mdToken     tkGetter;               // Token for property getter.
    mdParamDef  tkParam;                // Parameter of getter/setter.
    ULONG       cParams;                // Count of params for getter/setter.
    HCORENUM    phEnum = 0;             // Enumerator for params.
    ULONG       ulSeq;                  // Sequence of a param.

    // Retrieve the type of the token.
    tkObjType = TypeFromToken(tkObj);

    // Compute maximum size of the native type.
    if (pArgs[0].val.i4 == NATIVE_TYPE_CUSTOMMARSHALER)
    {   // N_T_* + 3 string lengths
        cbMax = sizeof(ULONG) * 4;
        // Marshal type - name of the type
        cbMax += pNamedArgs[M_MarshalType].val.str.cbStr;
        // Marshal type - type of the custom marshaler
        cbMax += pNamedArgs[M_MarshalTypeRef].val.str.cbStr;
        // String cookie.
        cbMax += pNamedArgs[M_MarshalCookie].val.str.cbStr;
    }
    else if (pArgs[0].val.i4 == NATIVE_TYPE_SAFEARRAY)
    {   // N_T_* + safe array sub-type + string length.
        cbMax = sizeof(ULONG) * 3;
        // Safe array record sub type.
        cbMax += pNamedArgs[M_SafeArrayUserDefinedSubType].val.str.cbStr;
    }
    else
    {   // N_T_* + sub-type + size + additive + NativeTypeArrayFlags.
        cbMax = sizeof(ULONG) * 4 + sizeof(UINT16);
    }

    // IidParameterIndex.
    cbMax += sizeof(DWORD);

    // Extra space to prevent buffer overrun.
    cbMax += 8;

    // Size the array.
    IfFailGo(qNativeType.ReSizeNoThrow(cbMax));
    pbNative = qNativeType.Ptr();
    cbNative = 0;

    //<TODO>@FUTURE: check for valid combinations of args.</TODO>

    // Put in the NativeType.
    cb = CorSigCompressData(pArgs[0].val.i4, pbNative);
    if (cb == ((ULONG)(-1)))
    {
        IfFailGo(PostError(META_E_CA_INVALID_BLOB));
    }

    cbNative += cb;
    pbNative += cb;
    if (cbNative > cbMax)
        IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

    // Put in additional information, depending on native type.
    switch (pArgs[0].val.i4)
    {
    case NATIVE_TYPE_INTF:
    case NATIVE_TYPE_IUNKNOWN:
    case NATIVE_TYPE_IDISPATCH:
        // Validate that the IidParameterIndex field is valid if set.
        if (pNamedArgs[M_IidParameterIndex].val.type.tag)
        {
            int iidparam = pNamedArgs[M_IidParameterIndex].val.i4;
            if (iidparam < 0)
                IfFailGo(PostError(META_E_CA_NEGATIVE_PARAMINDEX));

            cb = CorSigCompressData(pNamedArgs[M_IidParameterIndex].val.i4, pbNative);
            if (cb == ((ULONG)(-1)))
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));

            cbNative += cb;
            pbNative += cb;
            if (cbNative > cbMax)
                IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        }
        break;


    case NATIVE_TYPE_FIXEDARRAY:
        // Validate that only fields valid for NATIVE_TYPE_FIXEDARRAY are set.
        if (pNamedArgs[M_SafeArraySubType].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        if (pNamedArgs[M_SizeParamIndex].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // This native type is only applicable on fields.
        if (tkObjType != mdtFieldDef)
            IfFailGo(PostError(META_E_CA_NT_FIELDONLY));

        if (pNamedArgs[M_SizeConst].val.type.tag)
        {
            // Make sure the size is not negative.
            if (pNamedArgs[M_SizeConst].val.i4 < 0)
                IfFailGo(PostError(META_E_CA_NEGATIVE_CONSTSIZE));

            cb = CorSigCompressData(pNamedArgs[M_SizeConst].val.i4, pbNative);
            if (cb == ((ULONG)(-1)))
            {
                IfFailGo(PostError(META_E_CA_NEGATIVE_CONSTSIZE));
            }

        }
        else
        {
            cb = CorSigCompressData(1, pbNative);
            if (cb == ((ULONG)(-1)))
            {
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            }
        }
        cbNative += cb;
        pbNative += cb;
        if (cbNative > cbMax)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // Is there a sub type?
        if (pNamedArgs[M_ArraySubType].val.type.tag)
        {
            // Put in the sub type.
            cb = CorSigCompressData(pNamedArgs[M_ArraySubType].val.i4, pbNative);
            if (cb == ((ULONG)(-1)))
            {
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            }
            cbNative += cb;
            pbNative += cb;
            if (cbNative > cbMax)
                IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        }
        break;

    case NATIVE_TYPE_FIXEDSYSSTRING:
        // Validate that the required fields are set.
        if (!pNamedArgs[M_SizeConst].val.type.tag)
            IfFailGo(PostError(META_E_CA_FIXEDSTR_SIZE_REQUIRED));

        // Validate that other array fields are not set.
        if (pNamedArgs[M_ArraySubType].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        if (pNamedArgs[M_SizeParamIndex].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        if (pNamedArgs[M_SafeArraySubType].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // This native type is only applicable on fields.
        if (tkObjType != mdtFieldDef)
            IfFailGo(PostError(META_E_CA_NT_FIELDONLY));

        // Put in the constant value.
        cb = CorSigCompressData(pNamedArgs[M_SizeConst].val.i4, pbNative);
        if (cb == ((ULONG)(-1)))
        {
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));
        }
        cbNative += cb;
        pbNative += cb;
        if (cbNative > cbMax)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        break;

    case NATIVE_TYPE_BYVALSTR:
        // This native type is only applicable on parameters.
        if (tkObjType != mdtParamDef)
            IfFailGo(PostError(META_E_CA_INVALID_TARGET));
        break;

    case NATIVE_TYPE_SAFEARRAY:
        // Validate that other array fields are not set.
        if (pNamedArgs[M_ArraySubType].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        if (pNamedArgs[M_SizeParamIndex].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        if (pNamedArgs[M_SizeConst].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // Is there a safe array sub type?
        if (pNamedArgs[M_SafeArraySubType].val.type.tag)
        {
            // Put in the safe array sub type.
            cb = CorSigCompressData(pNamedArgs[M_SafeArraySubType].val.i4, pbNative);
            if (cb == ((ULONG)(-1)))
            {
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            }
            cbNative += cb;
            pbNative += cb;
            if (cbNative > cbMax)
                IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

            // When the SAFEARRAY contains user defined types, the type of the
            // UDT can be specified in the SafeArrayUserDefinedSubType field.
            if (pNamedArgs[M_SafeArrayUserDefinedSubType].val.type.tag)
            {
                // Validate that this is only set for valid VT's.
                if (pNamedArgs[M_SafeArraySubType].val.i4 != VT_RECORD &&
                    pNamedArgs[M_SafeArraySubType].val.i4 != VT_DISPATCH &&
                    pNamedArgs[M_SafeArraySubType].val.i4 != VT_UNKNOWN)
                {
                    IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
                }

                // Encode the size of the string.
                cch = pNamedArgs[M_SafeArrayUserDefinedSubType].val.str.cbStr;
                cb = CorSigCompressData(cch, pbNative);
                if (cb == ((ULONG)(-1)))
                    IfFailGo(PostError(META_E_CA_INVALID_BLOB));
                cbNative += cb;
                pbNative += cb;

                // Check that memcpy will fit and then encode the type name itself.
                if (ovadd_gt(cbNative, cch, cbMax))
                    IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
                memcpy(pbNative, pNamedArgs[M_SafeArrayUserDefinedSubType].val.str.pStr, cch);
                cbNative += cch;
                pbNative += cch;
                _ASSERTE(cbNative <= cbMax);
            }
        }
        break;

    case NATIVE_TYPE_ARRAY:
        // Validate that the array sub type is not set.
        if (pNamedArgs[M_SafeArraySubType].val.type.tag)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // Is there a sub type?
        if (pNamedArgs[M_ArraySubType].val.type.tag)
        {
            // Do some validation on the array sub type.
            if (pNamedArgs[M_ArraySubType].val.i4 == NATIVE_TYPE_CUSTOMMARSHALER)
                IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

            // Put in the sub type.
            cb = CorSigCompressData(pNamedArgs[M_ArraySubType].val.i4, pbNative);
            if (cb == ((ULONG)(-1)))
            {
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            }
            cbNative += cb;
            pbNative += cb;
        }
        else
        {
            // Put in the sub type.
            cb = CorSigCompressData(NATIVE_TYPE_MAX, pbNative);
            if (cb == ((ULONG)(-1)))
            {
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            }
            cbNative += cb;
            pbNative += cb;
        }
        if (cbNative > cbMax)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // Is there a parameter index?
        if (pNamedArgs[M_SizeParamIndex].val.type.tag)
        {
            // Make sure the parameter index is not negative.
            if (pNamedArgs[M_SizeParamIndex].val.i4 < 0)
                IfFailGo(PostError(META_E_CA_NEGATIVE_PARAMINDEX));

            // Yes, put it in.
            cb = CorSigCompressData(pNamedArgs[M_SizeParamIndex].val.i2, pbNative);
            if (cb == ((ULONG)(-1)))
            {
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            }
            cbNative += cb;
            pbNative += cb;
            if (cbNative > cbMax)
                IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

            // Is there a const?
            if (pNamedArgs[M_SizeConst].val.type.tag)
            {
                // Make sure the size is not negative.
                if (pNamedArgs[M_SizeConst].val.i4 < 0)
                    IfFailGo(PostError(META_E_CA_NEGATIVE_CONSTSIZE));

                // Yes, put it in.
                cb = CorSigCompressData(pNamedArgs[M_SizeConst].val.i4, pbNative);
                if (cb == ((ULONG)(-1)))
                {
                    IfFailGo(PostError(META_E_CA_INVALID_BLOB));
                }
                cbNative += cb;
                pbNative += cb;
                if (cbNative > cbMax)
                    IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

                // Put in the flag indicating the size param index was specified.
                cb = CorSigCompressData((UINT16)ntaSizeParamIndexSpecified, pbNative);
                if (cb == ((ULONG)(-1)))
                {
                    IfFailGo(PostError(META_E_CA_INVALID_BLOB));
                }
                cbNative += cb;
                pbNative += cb;
                if (cbNative > cbMax)
                    IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
            }
        }
        else
        {
            // Is there a const?
            if (pNamedArgs[M_SizeConst].val.type.tag)
            {
                // Put in a param index of 0.
                cb = CorSigCompressData(0, pbNative);
                if (cb == ((ULONG)(-1)))
                {
                    IfFailGo(PostError(META_E_CA_INVALID_BLOB));
                }
                cbNative += cb;
                pbNative += cb;
                if (cbNative > cbMax)
                    IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

                // Put in the constant value.
                cb = CorSigCompressData(pNamedArgs[M_SizeConst].val.i4, pbNative);
                if (cb == ((ULONG)(-1)))
                {
                    IfFailGo(PostError(META_E_CA_INVALID_BLOB));
                }
                cbNative += cb;
                pbNative += cb;
                if (cbNative > cbMax)
                    IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

                // Set the flags field to 0 to indicate the size param index was not specified.
                cb = CorSigCompressData((UINT16)0, pbNative);
                if (cb == ((ULONG)(-1)))
                {
                    IfFailGo(PostError(META_E_CA_INVALID_BLOB));
                }
                cbNative += cb;
                pbNative += cb;
                if (cbNative > cbMax)
                    IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
            }
        }
        break;

    case NATIVE_TYPE_CUSTOMMARSHALER:
        // Validate that the marshaler type field is set.
        if (!pNamedArgs[M_MarshalType].val.type.tag && !pNamedArgs[M_MarshalTypeRef].val.type.tag)
            IfFailGo(PostError(META_E_CA_CUSTMARSH_TYPE_REQUIRED));

        // Put in the place holder for the unmanaged typelib guid.
        cb = CorSigCompressData(0, pbNative);
        if (cb == ((ULONG)(-1)))
        {
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));
        }
        cbNative += cb;
        pbNative += cb;
        if (cbNative > cbMax)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // Put in the place holder for the unmanaged type name.
        cb = CorSigCompressData(0, pbNative);
        if (cb == ((ULONG)(-1)))
        {
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));
        }
        cbNative += cb;
        pbNative += cb;
        if (cbNative > cbMax)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));

        // Put in the marshaler type name.
        if (pNamedArgs[M_MarshalType].val.type.tag)
        {
            cch = pNamedArgs[M_MarshalType].val.str.cbStr;
            cb = CorSigCompressData(cch, pbNative);
            if (cb == ((ULONG)(-1)))
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            cbNative += cb;
            pbNative += cb;
            // Check that memcpy will fit.
            if ((cbNative+cch) > cbMax)
                IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
            memcpy(pbNative, pNamedArgs[M_MarshalType].val.str.pStr, cch);
            cbNative += cch;
            pbNative += cch;
            _ASSERTE(cbNative <= cbMax);
        }
        else
        {
            cch = pNamedArgs[M_MarshalTypeRef].val.str.cbStr;
            cb = CorSigCompressData(cch, pbNative);
            if (cb == ((ULONG)(-1)))
                IfFailGo(PostError(META_E_CA_INVALID_BLOB));
            cbNative += cb;
            pbNative += cb;
            // Check that memcpy will fit.
            if ((cbNative+cch) > cbMax)
                IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
            memcpy(pbNative, pNamedArgs[M_MarshalTypeRef].val.str.pStr, cch);
            cbNative += cch;
            pbNative += cch;
            _ASSERTE(cbNative <= cbMax);
        }

        // Put in the cookie.
        cch = pNamedArgs[M_MarshalCookie].val.str.cbStr;
        cb = CorSigCompressData(cch, pbNative);
        if (cb == ((ULONG)(-1)))
        {
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));
        }
        cbNative += cb;
        pbNative += cb;
        // Check that memcpy will fit.
        if ((cbNative+cch) > cbMax)
            IfFailGo(PostError(META_E_CA_INVALID_MARSHALAS_FIELDS));
        memcpy(pbNative, pNamedArgs[M_MarshalCookie].val.str.pStr, cch);
        cbNative += cch;
        pbNative += cch;
        break;
    }
    _ASSERTE(cbNative <= cbMax);

    // Resize to actual size.
    IfFailGo(qNativeType.ReSizeNoThrow(cbNative));

    // Now apply the native type to actual token.  If it is a property token,
    //  apply to the methods.
    switch (TypeFromToken(tkObj))
    {
    case mdtParamDef:
    case mdtFieldDef:
        IfFailGo(_SetFieldMarshal(tkObj, (PCCOR_SIGNATURE)qNativeType.Ptr(), (DWORD)qNativeType.Size()));
        break;

    case mdtProperty:
        // Get any setter/getter methods.
        IfFailGo(GetPropertyProps(tkObj, 0,0,0,0,0,0,0,0,0,0, &tkSetter, &tkGetter, 0,0,0));
        // For getter, put the field marshal on the return value.
        if (!IsNilToken(tkGetter))
        {
            // Search for first param.
            mdToken tk;
            tkParam = mdParamDefNil;
            do {
                IfFailGo(EnumParams(&phEnum, tkGetter, &tk, 1, &cParams));
                if (cParams > 0)
                {
                    IfFailGo(GetParamProps(tk, 0, &ulSeq, 0,0,0,0,0,0,0));
                    if (ulSeq == 0)
                    {
                          tkParam = tk;
                          break;
                    }
                }

            } while (hr == S_OK);
            if (!IsNilToken(tkParam))
                IfFailGo(_SetFieldMarshal(tkParam, (PCCOR_SIGNATURE)qNativeType.Ptr(), (DWORD)qNativeType.Size()));
            CloseEnum(phEnum);
            phEnum = 0;
        }
        if (!IsNilToken(tkSetter))
        {
            // Determine the last param.
            PCCOR_SIGNATURE pSig;
            ULONG cbSig;
            mdToken tk;
            ULONG iSeq;
            IfFailGo(GetMethodProps(tkSetter, 0,0,0,0,0, &pSig,&cbSig, 0,0));
            tkParam = mdParamDefNil;
            CorSigUncompressData(pSig+1, &iSeq);
            // Search for last param.
            if (iSeq != 0)
            {
                do {
                    IfFailGo(EnumParams(&phEnum, tkSetter, &tk, 1, &cParams));
                    if (cParams > 0)
                    {
                        IfFailGo(GetParamProps(tk, 0, &ulSeq, 0,0,0,0,0,0,0));
                        if (ulSeq == iSeq)
                        {
                            tkParam = tk;
                            break;
                        }
                    }
                } while (hr == S_OK);
            }
            // If found one that is not return value
            if (!IsNilToken(tkParam))
                IfFailGo(_SetFieldMarshal(tkParam, (PCCOR_SIGNATURE)qNativeType.Ptr(), (DWORD)qNativeType.Size()));
            CloseEnum(phEnum);
            phEnum = 0;
        }
        break;

    default:
        _ASSERTE(!"Should not have this token type in _HandleNativeTypeCustomAttribute()");
        break;
    }

ErrExit:
    if (phEnum)
        CloseEnum(phEnum);
    return hr;
} // RegMeta::_HandleNativeTypeCustomAttribute
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif //FEATURE_METADATA_EMIT
