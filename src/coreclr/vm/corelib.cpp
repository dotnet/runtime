// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


//
// This file defines tables for references between VM and corelib.
//
#include "common.h"
#include "ecall.h"

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
#include "assemblynative.hpp"
#include "comthreadpool.h"
#include "comwaithandle.h"

#include "proftoeeinterfaceimpl.h"

#include "appdomainnative.hpp"
#include "runtimehandles.h"
#include "reflectioninvocation.h"
#include "managedmdimport.hpp"
#include "typestring.h"
#include "comdependenthandle.h"
#include "weakreferencenative.h"
#include "varargsnative.h"
#include "mlinfo.h"

#ifdef FEATURE_COMINTEROP
#include "variant.h"
#include "oavariant.h"
#include "mngstdinterfaces.h"
#endif // FEATURE_COMINTEROP

#include "interoplibinterface.h"

#include "stubhelpers.h"
#include "ilmarshalers.h"

#ifdef FEATURE_MULTICOREJIT
#include "multicorejit.h"
#endif

#if defined(FEATURE_EVENTSOURCE_XPLAT)
#include "eventpipeadapter.h"
#include "eventpipeinternal.h"
#include "nativeeventsource.h"
#endif //defined(FEATURE_EVENTSOURCE_XPLAT)

#ifdef FEATURE_PERFTRACING
#include "eventpipeadapter.h"
#include "eventpipeinternal.h"
#include "nativeeventsource.h"
#endif //FEATURE_PERFTRACING

#include "tailcallhelp.h"

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
// CoreLib binder
//

// Extern definitions so that binder.cpp can see these tables
extern const CoreLibClassDescription c_rgCoreLibClassDescriptions[];
extern const USHORT c_nCoreLibClassDescriptions;

extern const CoreLibMethodDescription c_rgCoreLibMethodDescriptions[];
extern const USHORT c_nCoreLibMethodDescriptions;

extern const CoreLibFieldDescription c_rgCoreLibFieldDescriptions[];
extern const USHORT c_nCoreLibFieldDescriptions;

const CoreLibClassDescription c_rgCoreLibClassDescriptions[] =
{
    #define TYPEINFO(e,ns,c,s,g,ia,ip,if,im,gv)   { ns, c },
    #include "cortypeinfo.h"
    #undef TYPEINFO

    #define DEFINE_CLASS(i,n,s)        { g_ ## n ## NS, # s },
    #include "namespace.h"
    #include "corelib.h"

    // Include all exception types here that are defined in corelib.  Omit exceptions defined elsewhere.
    #define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...) { ns , # reKind },
    #include "rexcep.h"
};
const USHORT c_nCoreLibClassDescriptions = ARRAY_SIZE(c_rgCoreLibClassDescriptions);

#define gsig_NoSig (*(HardCodedMetaSig *)NULL)

const CoreLibMethodDescription c_rgCoreLibMethodDescriptions[] =
{
    #define DEFINE_METHOD(c,i,s,g)          { CLASS__ ## c , # s, & gsig_ ## g },
    #include "corelib.h"
};
const USHORT c_nCoreLibMethodDescriptions = ARRAY_SIZE(c_rgCoreLibMethodDescriptions) + 1;

const CoreLibFieldDescription c_rgCoreLibFieldDescriptions[] =
{
    #define DEFINE_FIELD(c,i,s)           { CLASS__ ## c , # s },
    #include "corelib.h"
};
const USHORT c_nCoreLibFieldDescriptions = ARRAY_SIZE(c_rgCoreLibFieldDescriptions) + 1;

///////////////////////////////////////////////////////////////////////////////
//
// ECalls
//

// When compiling crossgen, we only need the target version of the ecall tables

#define FCFuncFlags(dynamicID) \
    (BYTE*)( (((BYTE)dynamicID) << 24) )

#define FCFuncElement(name, impl) FCFuncFlags(ECall::InvalidDynamicFCallId), \
    (LPVOID)GetEEFuncEntryPoint(impl), (LPVOID)name,

#define FCFuncElementSig(name,sig,impl) \
    FCFuncFlag_HasSignature + FCFuncElement(name, impl) (LPVOID)sig,

#define FCDynamic(name,dynamicID) FCFuncFlags(dynamicID), \
    NULL, (LPVOID)name,

#define FCDynamicSig(name,sig,dynamicID) \
    FCFuncFlag_HasSignature + FCDynamic(name,dynamicID) (LPVOID)sig,

#define FCUnreferenced FCFuncFlag_Unreferenced +

#define FCFuncStart(name) static const LPVOID name[] = {
#define FCFuncEnd() FCFuncFlag_EndOfArray + FCFuncFlags(ECall::InvalidDynamicFCallId) };

#include "ecalllist.h"

// Extern definitions so that ecall.cpp can see these tables
extern const ECClass c_rgECClasses[];
extern const int c_nECClasses;

const ECClass c_rgECClasses[] =
{
#define FCClassElement(name,namespace,funcs) {name, namespace, funcs},
#include "ecalllist.h"
};  // c_rgECClasses[]

const int c_nECClasses = ARRAY_SIZE(c_rgECClasses);
