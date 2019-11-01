// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


// 
// This file defines tables for references between VM and mscorlib.
//
// When compiling crossgen, this file is compiled with the FEATURE_XXX define settings matching the target.
// It allows us to strip features (e.g. refection only load) from crossgen without stripping them from the target.
//

#ifdef CROSSGEN_MSCORLIB
// Use minimal set of headers for crossgen
#include "windows.h"
#include "corinfo.h"
#else
#include "common.h"
#include "ecall.h"
#endif // CROSSGEN_MSCORLIB

#ifndef CROSSGEN_COMPILE
//
// Headers for all ECall entrypoints
//
#include "arraynative.h"
#include "stringnative.h"
#include "objectnative.h"
#include "comdelegate.h"
#include "customattribute.h"
#include "comdynamic.h"
#include "excep.h"
#include "fcall.h"
#include "clrconfignative.h"
#include "commodule.h"
#include "marshalnative.h"
#include "nativelibrarynative.h"
#include "system.h"
#include "comutilnative.h"
#include "comsynchronizable.h"
#include "floatdouble.h"
#include "floatsingle.h"
#include "comdatetime.h"
#include "compatibilityswitch.h"
#include "debugdebugger.h"
#include "assemblyname.hpp"
#include "assemblynative.hpp"
#include "comthreadpool.h"
#include "comwaithandle.h"
#include "nativeoverlapped.h"

#include "proftoeeinterfaceimpl.h"

#include "appdomainnative.hpp"
#include "arrayhelpers.h"
#include "runtimehandles.h"
#include "reflectioninvocation.h"
#include "managedmdimport.hpp"
#include "synchronizationcontextnative.h"
#include "typestring.h"
#include "comdependenthandle.h"
#include "weakreferencenative.h"
#include "varargsnative.h"
#include "mlinfo.h"

#ifdef FEATURE_COMINTEROP
#include "variant.h"
#include "oavariant.h"
#include "mngstdinterfaces.h"
#include "extensibleclassfactory.h"
#endif // FEATURE_COMINTEROP

#include "stubhelpers.h"
#include "ilmarshalers.h"

#ifdef FEATURE_MULTICOREJIT
#include "multicorejit.h"
#endif

#if defined(FEATURE_EVENTSOURCE_XPLAT)
#include "nativeeventsource.h"
#include "eventpipe.h"
#include "eventpipeinternal.h"
#endif //defined(FEATURE_EVENTSOURCE_XPLAT)

#ifdef FEATURE_PERFTRACING
#include "eventpipe.h"
#include "eventpipeinternal.h"
#endif //FEATURE_PERFTRACING

#endif // CROSSGEN_MSCORLIB


#ifdef CROSSGEN_MSCORLIB

///////////////////////////////////////////////////////////////////////////////
//
// Duplicate definitions of constants and datastructures required to define the tables
//

#define NumItems(s) (sizeof(s) / sizeof(s[0]))

#define GetEEFuncEntryPoint(pfn) 0x1001

enum {
    FCFuncFlag_EndOfArray   = 0x01,
    FCFuncFlag_HasSignature = 0x02,
    FCFuncFlag_Unreferenced = 0x04, // Suppress unused fcall check
    FCFuncFlag_QCall        = 0x08, // QCall - mscorlib.dll to mscorwks.dll transition implemented as PInvoke
};

struct ECClass
{
    LPCSTR      m_szClassName;
    LPCSTR      m_szNameSpace;
    const LPVOID *  m_pECFunc;
};

struct HardCodedMetaSig
{
    const BYTE* m_pMetaSig; // metasig prefixed with INT8 length:
                            // length > 0 - resolved, lenght < 0 - has unresolved type references
};

enum BinderClassID
{
#define TYPEINFO(e,ns,c,s,g,ia,ip,if,im,gv)   CLASS__ ## e,
#include "cortypeinfo.h"
#undef TYPEINFO

#define DEFINE_CLASS(i,n,s)         CLASS__ ## i,
#include "mscorlib.h"

    CLASS__MSCORLIB_COUNT,

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

struct MscorlibClassDescription
{
    LPCSTR  nameSpace;
    LPCSTR  name;
};

struct MscorlibMethodDescription
{
    BinderClassID classID;
    LPCSTR  name;
    const HardCodedMetaSig * sig;
};

struct MscorlibFieldDescription
{
    BinderClassID classID;
    LPCSTR  name;
};

#endif // CROSSGEN_MSCORLIB


#ifdef CROSSGEN_MSCORLIB
// When compiling crossgen this namespace creates the second version of the tables than matches the target
namespace CrossGenMscorlib {
#endif


///////////////////////////////////////////////////////////////////////////////
//
// Hardcoded Meta-Sig
//

//
// Helper enum with metasig lengths
//
// iterate over the metasig recursing into the complex types
#define DEFINE_METASIG(body)            body,
#define METASIG_ATOM(x)                 + 1
#define METASIG_RECURSE                 1
#define SM(varname, args, retval)       gsigl_SM_ ## varname = 1 + 1 retval args
#define IM(varname, args, retval)       gsigl_IM_ ## varname = 1 + 1 retval args
#define GM(varname, conv, n, args, retval) gsigl_GM_ ## varname = 1 + 1 + 1 + retval args
#define Fld(varname, val)               gsigl_Fld_ ## varname = 1 val
enum _gsigl {
#include "metasig.h"
};

//
// Helper enum with metasig argcount
//
// iterate over the metasig without recursing into the complex types
#define DEFINE_METASIG(body)            body,
#define METASIG_ATOM(x)                 + 1
#define METASIG_RECURSE                 0
#define SM(varname, args, retval)       gsigc_SM_ ## varname = 0 args
#define IM(varname, args, retval)       gsigc_IM_ ## varname = 0 args
#define GM(varname, conv, n, args, retval) gsigc_GM_ ## varname = 0 args
#define Fld(varname, val)               gsigc_Fld_ ## varname = 0
enum _gsigc {
#include "metasig.h"
};


//
// The actual array with the hardcoded metasig:
//
// There are 3 variations of the macros for Fields, Static Methods and Instance Methods.
//
// Each of them has 2 flavors: one for the fully baked signatures, and the other 
// for the signatures with unresolved type references
//
// The signatures with unresolved type references are marked with negative size, 
// and the pointer to them is non-const because of it will be overwritten with
// the pointer to the resolved signature at runtime.
//

#define DEFINE_METASIG(body)            body
#define DEFINE_METASIG_T(body)          _##body
#define METASIG_ATOM(x)                 x,
#define METASIG_RECURSE                 1

// define gsig_ ## varname before gsige_ ## varname to give a hint to the compiler about the desired layout

#define SM(varname, args, retval) extern const BYTE gsige_SM_ ## varname[];     \
    const HardCodedMetaSig gsig_SM_ ## varname = { gsige_SM_ ## varname };      \
    const BYTE gsige_SM_ ## varname[] = { gsigl_SM_ ## varname,                 \
        IMAGE_CEE_CS_CALLCONV_DEFAULT, gsigc_SM_ ## varname, retval args };

#define IM(varname, args, retval) extern const BYTE gsige_IM_ ## varname[];     \
    const HardCodedMetaSig gsig_IM_ ## varname = { gsige_IM_ ## varname };      \
    const BYTE gsige_IM_ ## varname[] = { gsigl_IM_ ## varname,                 \
        IMAGE_CEE_CS_CALLCONV_HASTHIS, gsigc_IM_ ## varname, retval args };

#define GM(varname, conv, n, args, retval) extern const BYTE gsige_GM_ ## varname[];    \
    const HardCodedMetaSig gsig_GM_ ## varname = { gsige_GM_ ## varname };              \
    const BYTE gsige_GM_ ## varname[] = { gsigl_GM_ ## varname,                         \
        conv | IMAGE_CEE_CS_CALLCONV_GENERIC, n, gsigc_GM_ ## varname, retval args };

#define Fld(varname, val) extern const BYTE gsige_Fld_ ## varname[];            \
    const HardCodedMetaSig gsig_Fld_ ## varname = { gsige_Fld_ ## varname };    \
    const BYTE gsige_Fld_ ## varname[] = { gsigl_Fld_ ## varname,               \
        IMAGE_CEE_CS_CALLCONV_FIELD, val };

#define _SM(varname, args, retval) extern const BYTE gsige_SM_ ## varname[];    \
    HardCodedMetaSig gsig_SM_ ## varname = { gsige_SM_ ## varname };            \
    const BYTE gsige_SM_ ## varname[] = { (BYTE) -gsigl_SM_ ## varname,         \
        IMAGE_CEE_CS_CALLCONV_DEFAULT, gsigc_SM_ ## varname, retval args };

#define _IM(varname, args, retval) extern const BYTE gsige_IM_ ## varname[];    \
    HardCodedMetaSig gsig_IM_ ## varname = { gsige_IM_ ## varname };            \
    const BYTE gsige_IM_ ## varname[] = { (BYTE) -gsigl_IM_ ## varname,         \
        IMAGE_CEE_CS_CALLCONV_HASTHIS, gsigc_IM_ ## varname, retval args };

#define _Fld(varname, val) extern const BYTE gsige_Fld_ ## varname[];           \
    HardCodedMetaSig gsig_Fld_ ## varname = { gsige_Fld_ ## varname };          \
    const BYTE gsige_Fld_ ## varname[] = { (BYTE) -gsigl_Fld_ ## varname,       \
        IMAGE_CEE_CS_CALLCONV_FIELD, val };

#include "metasig.h"

#undef _SM
#undef _IM
#undef _Fld



#ifdef _DEBUG

//
// Make sure DEFINE_METASIG is used for signatures that do not reference other types
//
// counts number of type references in the signature and C_ASSERTs that 
// it is zero. An assertion failure results in error C2118: negative subscript.
#define DEFINE_METASIG(body)            body
#define DEFINE_METASIG_T(body)
#define METASIG_BODY(varname, types)    C_ASSERT(types 0 == 0);
#define METASIG_ATOM(x)                 0+
#define METASIG_RECURSE                 1
#define C(x)                            1+
#define g(x)                            1+
#define Q(x)                            1+
#include "metasig.h"

//
// Make sure DEFINE_METASIG_T is used only for signatures that reference 
// other types.
//
// counts number of type references in the signature and C_ASSERTs that 
// it is non zero. An assertion failure results in error C2118: negative subscript.
#define DEFINE_METASIG(body)
#define DEFINE_METASIG_T(body)          body
#define METASIG_BODY(varname, types)    C_ASSERT(types 0 != 0);
#define METASIG_ATOM(x)                 0+
#define METASIG_RECURSE                 1
#define C(x)                            1+
#define g(x)                            1+
#define Q(x)                            1+
#include "metasig.h"

#endif





///////////////////////////////////////////////////////////////////////////////
//
// Mscorlib binder
//

// Extern definitions so that binder.cpp can see these tables
extern const MscorlibClassDescription c_rgMscorlibClassDescriptions[];
extern const USHORT c_nMscorlibClassDescriptions;

extern const MscorlibMethodDescription c_rgMscorlibMethodDescriptions[];
extern const USHORT c_nMscorlibMethodDescriptions;

extern const MscorlibFieldDescription c_rgMscorlibFieldDescriptions[];
extern const USHORT c_nMscorlibFieldDescriptions;

const MscorlibClassDescription c_rgMscorlibClassDescriptions[] =
{
    #define TYPEINFO(e,ns,c,s,g,ia,ip,if,im,gv)   { ns, c },
    #include "cortypeinfo.h"
    #undef TYPEINFO
    
    #define DEFINE_CLASS(i,n,s)        { g_ ## n ## NS, # s },
    #include "namespace.h"
    #include "mscorlib.h"
    
    // Include all exception types here that are defined in mscorlib.  Omit exceptions defined elsewhere.
    #define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...) { ns , # reKind },
    #define DEFINE_EXCEPTION_HR_WINRT_ONLY(ns, reKind, ...)
    #define DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY(ns, reKind, assemblySimpleName, publicKeyToken, bHRformessage, ...)
    #include "rexcep.h"
};
const USHORT c_nMscorlibClassDescriptions = NumItems(c_rgMscorlibClassDescriptions);

#define gsig_NoSig (*(HardCodedMetaSig *)NULL)

const MscorlibMethodDescription c_rgMscorlibMethodDescriptions[] =
{
    #define DEFINE_METHOD(c,i,s,g)          { CLASS__ ## c , # s, & gsig_ ## g },
    #include "mscorlib.h"
};
const USHORT c_nMscorlibMethodDescriptions = NumItems(c_rgMscorlibMethodDescriptions) + 1;

const MscorlibFieldDescription c_rgMscorlibFieldDescriptions[] =
{
    #define DEFINE_FIELD(c,i,s)           { CLASS__ ## c , # s },
    #include "mscorlib.h"
};
const USHORT c_nMscorlibFieldDescriptions = NumItems(c_rgMscorlibFieldDescriptions) + 1;





///////////////////////////////////////////////////////////////////////////////
//
// ECalls
//

// When compiling crossgen, we only need the target version of the ecall tables
#if !defined(CROSSGEN_COMPILE) || defined(CROSSGEN_MSCORLIB)

#ifdef CROSSGEN_COMPILE

#define QCFuncElement(name,impl) \
    FCFuncFlag_QCall + FCFuncFlags(CORINFO_INTRINSIC_Illegal, ECall::InvalidDynamicFCallId), NULL, (LPVOID)name,

#define FCFuncFlags(intrinsicID, dynamicID) \
    (BYTE*)( (((BYTE)intrinsicID) << 16) )

#else

#define QCFuncElement(name,impl) \
    FCFuncFlag_QCall + FCFuncFlags(CORINFO_INTRINSIC_Illegal, ECall::InvalidDynamicFCallId), (LPVOID)(impl), (LPVOID)name,

#define FCFuncFlags(intrinsicID, dynamicID) \
    (BYTE*)( (((BYTE)intrinsicID) << 16) + (((BYTE)dynamicID) << 24) )

#endif

#define FCFuncElement(name, impl) FCFuncFlags(CORINFO_INTRINSIC_Illegal, ECall::InvalidDynamicFCallId), \
    (LPVOID)GetEEFuncEntryPoint(impl), (LPVOID)name,

#define FCFuncElementSig(name,sig,impl) \
    FCFuncFlag_HasSignature + FCFuncElement(name, impl) (LPVOID)sig,

#define FCIntrinsic(name,impl,intrinsicID) FCFuncFlags(intrinsicID, ECall::InvalidDynamicFCallId), \
    (LPVOID)GetEEFuncEntryPoint(impl), (LPVOID)name,

#define FCIntrinsicSig(name,sig,impl,intrinsicID) \
    FCFuncFlag_HasSignature + FCIntrinsic(name,impl,intrinsicID) (LPVOID)sig,

#define FCDynamic(name,intrinsicID,dynamicID) FCFuncFlags(intrinsicID, dynamicID), \
    NULL, (LPVOID)name,

#define FCDynamicSig(name,sig,intrinsicID,dynamicID) \
    FCFuncFlag_HasSignature + FCDynamic(name,intrinsicID,dynamicID) (LPVOID)sig,

#define FCUnreferenced FCFuncFlag_Unreferenced +

#define FCFuncStart(name) static const LPVOID name[] = {
#define FCFuncEnd() FCFuncFlag_EndOfArray + FCFuncFlags(CORINFO_INTRINSIC_Illegal, ECall::InvalidDynamicFCallId) };

#include "ecalllist.h"


// Extern definitions so that ecall.cpp can see these tables
extern const ECClass c_rgECClasses[];
extern const int c_nECClasses;

const ECClass c_rgECClasses[] =
{
#define FCClassElement(name,namespace,funcs) {name, namespace, funcs},
#include "ecalllist.h"
};  // c_rgECClasses[]

const int c_nECClasses = NumItems(c_rgECClasses);

#endif // !CROSSGEN_COMPILE && CROSSGEN_MSCORLIB


#ifdef CROSSGEN_MSCORLIB
}; // namespace CrossGenMscorlib
#endif
