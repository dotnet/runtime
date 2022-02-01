// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern ICorJitHost* g_jitHost;

class CILJit : public ICorJitCompiler
{
    CorJitResult compileMethod(ICorJitInfo*         comp,            /* IN */
                               CORINFO_METHOD_INFO* methodInfo,      /* IN */
                               unsigned             flags,           /* IN */
                               uint8_t**            nativeEntry,     /* OUT */
                               uint32_t*            nativeSizeOfCode /* OUT */
                               );

    void ProcessShutdownWork(ICorStaticInfo* statInfo);

    void getVersionIdentifier(GUID* versionIdentifier /* OUT */
                              );

    unsigned getMaxIntrinsicSIMDVectorLength(CORJIT_FLAGS cpuCompileFlags);

    void setTargetOS(CORINFO_OS os);
};

/*****************************************************************************
 *
 *              Functions to get various handles
 */

FORCEINLINE
void Compiler::eeGetCallInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                             CORINFO_RESOLVED_TOKEN* pConstrainedToken,
                             CORINFO_CALLINFO_FLAGS  flags,
                             CORINFO_CALL_INFO*      pResult)
{
    info.compCompHnd->getCallInfo(pResolvedToken, pConstrainedToken, info.compMethodHnd, flags, pResult);
}

FORCEINLINE
void Compiler::eeGetFieldInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                              CORINFO_ACCESS_FLAGS    accessFlags,
                              CORINFO_FIELD_INFO*     pResult)
{
    info.compCompHnd->getFieldInfo(pResolvedToken, info.compMethodHnd, accessFlags, pResult);
}

/*****************************************************************************
 *
 *          VOS info, method sigs, etc
 */

FORCEINLINE
bool Compiler::eeIsValueClass(CORINFO_CLASS_HANDLE clsHnd)
{
    return info.compCompHnd->isValueClass(clsHnd);
}

FORCEINLINE
bool Compiler::eeIsIntrinsic(CORINFO_METHOD_HANDLE ftn)
{
    return info.compCompHnd->isIntrinsic(ftn);
}

FORCEINLINE
bool Compiler::eeIsFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    return info.compCompHnd->isFieldStatic(fldHnd);
}

FORCEINLINE
var_types Compiler::eeGetFieldType(CORINFO_FIELD_HANDLE fldHnd, CORINFO_CLASS_HANDLE* pStructHnd)
{
    return JITtype2varType(info.compCompHnd->getFieldType(fldHnd, pStructHnd));
}

FORCEINLINE
void Compiler::eeGetSig(unsigned               sigTok,
                        CORINFO_MODULE_HANDLE  scope,
                        CORINFO_CONTEXT_HANDLE context,
                        CORINFO_SIG_INFO*      retSig)
{
    info.compCompHnd->findSig(scope, sigTok, context, retSig);

    assert(!varTypeIsComposite(JITtype2varType(retSig->retType)) || retSig->retTypeClass != nullptr);
}

FORCEINLINE
void Compiler::eeGetMethodSig(CORINFO_METHOD_HANDLE methHnd, CORINFO_SIG_INFO* sigRet, CORINFO_CLASS_HANDLE owner)
{
    info.compCompHnd->getMethodSig(methHnd, sigRet, owner);

    assert(!varTypeIsComposite(JITtype2varType(sigRet->retType)) || sigRet->retTypeClass != nullptr);
}

/**********************************************************************
 * For varargs we need the number of arguments at the call site
 */

FORCEINLINE
void Compiler::eeGetCallSiteSig(unsigned               sigTok,
                                CORINFO_MODULE_HANDLE  scope,
                                CORINFO_CONTEXT_HANDLE context,
                                CORINFO_SIG_INFO*      sigRet)
{
    info.compCompHnd->findCallSiteSig(scope, sigTok, context, sigRet);

    assert(!varTypeIsComposite(JITtype2varType(sigRet->retType)) || sigRet->retTypeClass != nullptr);
}

/*****************************************************************************/
inline var_types Compiler::eeGetArgType(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig)
{
    CORINFO_CLASS_HANDLE argClass;
    return (JITtype2varType(strip(info.compCompHnd->getArgType(sig, list, &argClass))));
}

/*****************************************************************************/
inline var_types Compiler::eeGetArgType(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig, bool* isPinned)
{
    CORINFO_CLASS_HANDLE argClass;
    CorInfoTypeWithMod   type = info.compCompHnd->getArgType(sig, list, &argClass);
    *isPinned                 = ((type & ~CORINFO_TYPE_MASK) != 0);
    return JITtype2varType(strip(type));
}

/*****************************************************************************/
inline CORINFO_CLASS_HANDLE Compiler::eeGetArgClass(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_HANDLE list)
{
    CORINFO_CLASS_HANDLE argClass = info.compCompHnd->getArgClass(sig, list);
    return argClass;
}

/*****************************************************************************/
inline CORINFO_CLASS_HANDLE Compiler::eeGetClassFromContext(CORINFO_CONTEXT_HANDLE context)
{
    if (context == METHOD_BEING_COMPILED_CONTEXT())
    {
        return impInlineRoot()->info.compClassHnd;
    }

    if (((SIZE_T)context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
    {
        return CORINFO_CLASS_HANDLE((SIZE_T)context & ~CORINFO_CONTEXTFLAGS_MASK);
    }
    else
    {
        return info.compCompHnd->getMethodClass(CORINFO_METHOD_HANDLE((SIZE_T)context & ~CORINFO_CONTEXTFLAGS_MASK));
    }
}

/*****************************************************************************
 *
 *                  Native Direct Optimizations
 */

inline CORINFO_EE_INFO* Compiler::eeGetEEInfo()
{
    if (!eeInfoInitialized)
    {
        info.compCompHnd->getEEInfo(&eeInfo);
        eeInfoInitialized = true;
    }

    return &eeInfo;
}

/*****************************************************************************
 *
 *  Convert the type returned from the VM to a var_type.
 */

inline var_types JITtype2varType(CorInfoType type)
{

    static const unsigned char varTypeMap[CORINFO_TYPE_COUNT] = {
        // see the definition of enum CorInfoType in file inc/corinfo.h
        TYP_UNDEF,  // CORINFO_TYPE_UNDEF           = 0x0,
        TYP_VOID,   // CORINFO_TYPE_VOID            = 0x1,
        TYP_BOOL,   // CORINFO_TYPE_BOOL            = 0x2,
        TYP_USHORT, // CORINFO_TYPE_CHAR            = 0x3,
        TYP_BYTE,   // CORINFO_TYPE_BYTE            = 0x4,
        TYP_UBYTE,  // CORINFO_TYPE_UBYTE           = 0x5,
        TYP_SHORT,  // CORINFO_TYPE_SHORT           = 0x6,
        TYP_USHORT, // CORINFO_TYPE_USHORT          = 0x7,
        TYP_INT,    // CORINFO_TYPE_INT             = 0x8,
        TYP_INT,    // CORINFO_TYPE_UINT            = 0x9,
        TYP_LONG,   // CORINFO_TYPE_LONG            = 0xa,
        TYP_LONG,   // CORINFO_TYPE_ULONG           = 0xb,
        TYP_I_IMPL, // CORINFO_TYPE_NATIVEINT       = 0xc,
        TYP_I_IMPL, // CORINFO_TYPE_NATIVEUINT      = 0xd,
        TYP_FLOAT,  // CORINFO_TYPE_FLOAT           = 0xe,
        TYP_DOUBLE, // CORINFO_TYPE_DOUBLE          = 0xf,
        TYP_REF,    // CORINFO_TYPE_STRING          = 0x10,         // Not used, should remove
        TYP_I_IMPL, // CORINFO_TYPE_PTR             = 0x11,
        TYP_BYREF,  // CORINFO_TYPE_BYREF           = 0x12,
        TYP_STRUCT, // CORINFO_TYPE_VALUECLASS      = 0x13,
        TYP_REF,    // CORINFO_TYPE_CLASS           = 0x14,
        TYP_STRUCT, // CORINFO_TYPE_REFANY          = 0x15,

        // Generic type variables only appear when we're doing
        // verification of generic code, in which case we're running
        // in "import only" mode.  Annoyingly the "import only"
        // mode of the JIT actually does a fair bit of compilation,
        // so we have to trick the compiler into thinking it's compiling
        // a real instantiation.  We do that by just pretending we're
        // compiling the "object" instantiation of the code, i.e. by
        // turing all generic type variables refs, except for a few
        // choice places to do with verification, where we use
        // verification types and CLASS_HANDLEs to track the difference.

        TYP_REF, // CORINFO_TYPE_VAR             = 0x16,
    };

    // spot check to make certain enumerations have not changed

    assert(varTypeMap[CORINFO_TYPE_CLASS] == TYP_REF);
    assert(varTypeMap[CORINFO_TYPE_BYREF] == TYP_BYREF);
    assert(varTypeMap[CORINFO_TYPE_PTR] == TYP_I_IMPL);
    assert(varTypeMap[CORINFO_TYPE_INT] == TYP_INT);
    assert(varTypeMap[CORINFO_TYPE_UINT] == TYP_INT);
    assert(varTypeMap[CORINFO_TYPE_DOUBLE] == TYP_DOUBLE);
    assert(varTypeMap[CORINFO_TYPE_VOID] == TYP_VOID);
    assert(varTypeMap[CORINFO_TYPE_VALUECLASS] == TYP_STRUCT);
    assert(varTypeMap[CORINFO_TYPE_REFANY] == TYP_STRUCT);

    assert(type < CORINFO_TYPE_COUNT);
    assert(varTypeMap[type] != TYP_UNDEF);

    return ((var_types)varTypeMap[type]);
};

// Convert the type returned from the VM to a precise var_type
inline var_types JitType2PreciseVarType(CorInfoType type)
{

    static const unsigned char preciseVarTypeMap[CORINFO_TYPE_COUNT] = {
        // see the definition of enum CorInfoType in file inc/corinfo.h
        TYP_UNDEF,  // CORINFO_TYPE_UNDEF           = 0x0,
        TYP_VOID,   // CORINFO_TYPE_VOID            = 0x1,
        TYP_BOOL,   // CORINFO_TYPE_BOOL            = 0x2,
        TYP_USHORT, // CORINFO_TYPE_CHAR            = 0x3,
        TYP_BYTE,   // CORINFO_TYPE_BYTE            = 0x4,
        TYP_UBYTE,  // CORINFO_TYPE_UBYTE           = 0x5,
        TYP_SHORT,  // CORINFO_TYPE_SHORT           = 0x6,
        TYP_USHORT, // CORINFO_TYPE_USHORT          = 0x7,
        TYP_INT,    // CORINFO_TYPE_INT             = 0x8,
        TYP_UINT,   // CORINFO_TYPE_UINT            = 0x9,
        TYP_LONG,   // CORINFO_TYPE_LONG            = 0xa,
        TYP_ULONG,  // CORINFO_TYPE_ULONG           = 0xb,
        TYP_I_IMPL, // CORINFO_TYPE_NATIVEINT       = 0xc,
        TYP_U_IMPL, // CORINFO_TYPE_NATIVEUINT      = 0xd,
        TYP_FLOAT,  // CORINFO_TYPE_FLOAT           = 0xe,
        TYP_DOUBLE, // CORINFO_TYPE_DOUBLE          = 0xf,
        TYP_REF,    // CORINFO_TYPE_STRING          = 0x10,         // Not used, should remove
        TYP_U_IMPL, // CORINFO_TYPE_PTR             = 0x11,
        TYP_BYREF,  // CORINFO_TYPE_BYREF           = 0x12,
        TYP_STRUCT, // CORINFO_TYPE_VALUECLASS      = 0x13,
        TYP_REF,    // CORINFO_TYPE_CLASS           = 0x14,
        TYP_STRUCT, // CORINFO_TYPE_REFANY          = 0x15,

        // Generic type variables only appear when we're doing
        // verification of generic code, in which case we're running
        // in "import only" mode.  Annoyingly the "import only"
        // mode of the JIT actually does a fair bit of compilation,
        // so we have to trick the compiler into thinking it's compiling
        // a real instantiation.  We do that by just pretending we're
        // compiling the "object" instantiation of the code, i.e. by
        // turing all generic type variables refs, except for a few
        // choice places to do with verification, where we use
        // verification types and CLASS_HANDLEs to track the difference.

        TYP_REF, // CORINFO_TYPE_VAR             = 0x16,
    };

    // spot check to make certain enumerations have not changed

    assert(preciseVarTypeMap[CORINFO_TYPE_CLASS] == TYP_REF);
    assert(preciseVarTypeMap[CORINFO_TYPE_BYREF] == TYP_BYREF);
    assert(preciseVarTypeMap[CORINFO_TYPE_PTR] == TYP_U_IMPL);
    assert(preciseVarTypeMap[CORINFO_TYPE_INT] == TYP_INT);
    assert(preciseVarTypeMap[CORINFO_TYPE_UINT] == TYP_UINT);
    assert(preciseVarTypeMap[CORINFO_TYPE_DOUBLE] == TYP_DOUBLE);
    assert(preciseVarTypeMap[CORINFO_TYPE_VOID] == TYP_VOID);
    assert(preciseVarTypeMap[CORINFO_TYPE_VALUECLASS] == TYP_STRUCT);
    assert(preciseVarTypeMap[CORINFO_TYPE_REFANY] == TYP_STRUCT);

    assert(type < CORINFO_TYPE_COUNT);
    assert(preciseVarTypeMap[type] != TYP_UNDEF);

    return ((var_types)preciseVarTypeMap[type]);
};

inline CORINFO_CALLINFO_FLAGS combine(CORINFO_CALLINFO_FLAGS flag1, CORINFO_CALLINFO_FLAGS flag2)
{
    return (CORINFO_CALLINFO_FLAGS)(flag1 | flag2);
}
