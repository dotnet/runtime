// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                            ee_jit.cpp                                     XX
XX                                                                           XX
XX   The functionality needed for the JIT DLL. Includes the DLL entry point  XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "emit.h"
#include "corexcep.h"

#if !defined(HOST_UNIX)
#include <io.h>    // For _dup, _setmode
#include <fcntl.h> // For _O_TEXT
#include <errno.h> // For EINVAL
#endif

#ifndef DLLEXPORT
#define DLLEXPORT
#endif // !DLLEXPORT

/*****************************************************************************/

FILE* jitstdout = nullptr;

ICorJitHost* g_jitHost        = nullptr;
bool         g_jitInitialized = false;

/*****************************************************************************/

extern "C" DLLEXPORT void jitStartup(ICorJitHost* jitHost)
{
    if (g_jitInitialized)
    {
        if (jitHost != g_jitHost)
        {
            // We normally don't expect jitStartup() to be invoked more than once.
            // (We check whether it has been called once due to an abundance of caution.)
            // However, during SuperPMI playback of MCH file, we need to JIT many different methods.
            // Each one carries its own environment configuration state.
            // So, we need the JIT to reload the JitConfig state for each change in the environment state of the
            // replayed compilations.
            // We do this by calling jitStartup with a different ICorJitHost,
            // and have the JIT re-initialize its JitConfig state when this happens.
            JitConfig.destroy(g_jitHost);
            JitConfig.initialize(jitHost);
            g_jitHost = jitHost;
        }
        return;
    }

#ifdef HOST_UNIX
    int err = PAL_InitializeDLL();
    if (err != 0)
    {
        return;
    }
#endif

    g_jitHost = jitHost;

    assert(!JitConfig.isInitialized());
    JitConfig.initialize(jitHost);

    const WCHAR* jitStdOutFile = JitConfig.JitStdOutFile();
    if (jitStdOutFile != nullptr)
    {
        jitstdout = _wfopen(jitStdOutFile, W("a"));
        assert(jitstdout != nullptr);
    }

#if !defined(HOST_UNIX)
    if (jitstdout == nullptr)
    {
        int stdoutFd = _fileno(procstdout());
        // Check fileno error output(s) -1 may overlap with errno result
        // but is included for completeness.
        // We want to detect the case where the initial handle is null
        // or bogus and avoid making further calls.
        if ((stdoutFd != -1) && (stdoutFd != -2) && (errno != EINVAL))
        {
            int jitstdoutFd = _dup(_fileno(procstdout()));
            // Check the error status returned by dup.
            if (jitstdoutFd != -1)
            {
                _setmode(jitstdoutFd, _O_TEXT);
                jitstdout = _fdopen(jitstdoutFd, "w");
                assert(jitstdout != nullptr);

                // Prevent the FILE* from buffering its output in order to avoid calls to
                // `fflush()` throughout the code.
                setvbuf(jitstdout, nullptr, _IONBF, 0);
            }
        }
    }
#endif // !HOST_UNIX

    // If jitstdout is still null, fallback to whatever procstdout() was
    // initially set to.
    if (jitstdout == nullptr)
    {
        jitstdout = procstdout();
    }

#ifdef FEATURE_TRACELOGGING
    JitTelemetry::NotifyDllProcessAttach();
#endif
    Compiler::compStartup();

    g_jitInitialized = true;
}

#ifndef DEBUG
void jitprintf(const char* fmt, ...)
{
    va_list vl;
    va_start(vl, fmt);
    vfprintf(jitstdout, fmt, vl);
    va_end(vl);
}
#endif

void jitShutdown(bool processIsTerminating)
{
    if (!g_jitInitialized)
    {
        return;
    }

    Compiler::compShutdown();

    if (jitstdout != procstdout())
    {
        // When the process is terminating, the fclose call is unnecessary and is also prone to
        // crashing since the UCRT itself often frees the backing memory earlier on in the
        // termination sequence.
        if (!processIsTerminating)
        {
            fclose(jitstdout);
        }
    }

#ifdef FEATURE_TRACELOGGING
    JitTelemetry::NotifyDllProcessDetach();
#endif

    g_jitInitialized = false;
}

/*****************************************************************************/

static CILJit g_CILJit;

DLLEXPORT ICorJitCompiler* getJit()
{
    if (!g_jitInitialized)
    {
        return nullptr;
    }

    return &g_CILJit;
}

/*****************************************************************************/

// Information kept in thread-local storage. This is used in the noway_assert exceptional path.
// If you are using it more broadly in retail code, you would need to understand the
// performance implications of accessing TLS.

thread_local void* gJitTls = nullptr;

static void* GetJitTls()
{
    return gJitTls;
}

void SetJitTls(void* value)
{
    gJitTls = value;
}

#if defined(DEBUG)

JitTls::JitTls(ICorJitInfo* jitInfo) : m_compiler(nullptr), m_logEnv(jitInfo)
{
    m_next = reinterpret_cast<JitTls*>(GetJitTls());
    SetJitTls(this);
}

JitTls::~JitTls()
{
    SetJitTls(m_next);
}

LogEnv* JitTls::GetLogEnv()
{
    return &reinterpret_cast<JitTls*>(GetJitTls())->m_logEnv;
}

Compiler* JitTls::GetCompiler()
{
    return reinterpret_cast<JitTls*>(GetJitTls())->m_compiler;
}

void JitTls::SetCompiler(Compiler* compiler)
{
    reinterpret_cast<JitTls*>(GetJitTls())->m_compiler = compiler;
}

#else // !defined(DEBUG)

JitTls::JitTls(ICorJitInfo* jitInfo)
{
}

JitTls::~JitTls()
{
}

Compiler* JitTls::GetCompiler()
{
    return reinterpret_cast<Compiler*>(GetJitTls());
}

void JitTls::SetCompiler(Compiler* compiler)
{
    SetJitTls(compiler);
}

#endif // !defined(DEBUG)

//****************************************************************************
// The main JIT function for the 32 bit JIT.  See code:ICorJitCompiler#EEToJitInterface for more on the EE-JIT
// interface. Things really don't get going inside the JIT until the code:Compiler::compCompile#Phases
// method.  Usually that is where you want to go.

CorJitResult CILJit::compileMethod(ICorJitInfo*         compHnd,
                                   CORINFO_METHOD_INFO* methodInfo,
                                   unsigned             flags,
                                   uint8_t**            entryAddress,
                                   uint32_t*            nativeSizeOfCode)
{
    JitFlags jitFlags;

    assert(flags == CORJIT_FLAGS::CORJIT_FLAG_CALL_GETJITFLAGS);
    CORJIT_FLAGS corJitFlags;
    DWORD        jitFlagsSize = compHnd->getJitFlags(&corJitFlags, sizeof(corJitFlags));
    assert(jitFlagsSize == sizeof(corJitFlags));
    jitFlags.SetFromFlags(corJitFlags);

    int                   result;
    void*                 methodCodePtr = nullptr;
    CORINFO_METHOD_HANDLE methodHandle  = methodInfo->ftn;

    JitTls jitTls(compHnd); // Initialize any necessary thread-local state

    assert(methodInfo->ILCode);

    result = jitNativeCode(methodHandle, methodInfo->scope, compHnd, methodInfo, &methodCodePtr, nativeSizeOfCode,
                           &jitFlags, nullptr);

    if (result == CORJIT_OK)
    {
        *entryAddress = (BYTE*)methodCodePtr;
    }

    return CorJitResult(result);
}

void CILJit::ProcessShutdownWork(ICorStaticInfo* statInfo)
{
    jitShutdown(false);

    Compiler::ProcessShutdownWork(statInfo);
}

/*****************************************************************************
 * Verify the JIT/EE interface identifier.
 */
void CILJit::getVersionIdentifier(GUID* versionIdentifier)
{
    assert(versionIdentifier != nullptr);
    memcpy(versionIdentifier, &JITEEVersionIdentifier, sizeof(GUID));
}

#ifdef TARGET_OS_RUNTIMEDETERMINED
bool TargetOS::OSSettingConfigured = false;
bool TargetOS::IsWindows           = false;
bool TargetOS::IsUnix              = false;
bool TargetOS::IsMacOS             = false;
#endif

/*****************************************************************************
 * Set the OS that this JIT should be generating code for. The contract with the VM
 * is that this must be called before compileMethod is called.
 */
void CILJit::setTargetOS(CORINFO_OS os)
{
#ifdef TARGET_OS_RUNTIMEDETERMINED
    TargetOS::IsMacOS             = os == CORINFO_MACOS;
    TargetOS::IsUnix              = (os == CORINFO_UNIX) || (os == CORINFO_MACOS);
    TargetOS::IsWindows           = os == CORINFO_WINNT;
    TargetOS::OSSettingConfigured = true;
#endif
}

/*****************************************************************************
 * Determine the maximum length of SIMD vector supported by this JIT.
 */

unsigned CILJit::getMaxIntrinsicSIMDVectorLength(CORJIT_FLAGS cpuCompileFlags)
{
    JitFlags jitFlags;
    jitFlags.SetFromFlags(cpuCompileFlags);

#ifdef FEATURE_SIMD
#if defined(TARGET_XARCH)
    if (!jitFlags.IsSet(JitFlags::JIT_FLAG_PREJIT) &&
        jitFlags.GetInstructionSetFlags().HasInstructionSet(InstructionSet_AVX2))
    {
        if (GetJitTls() != nullptr && JitTls::GetCompiler() != nullptr)
        {
            JITDUMP("getMaxIntrinsicSIMDVectorLength: returning 32\n");
        }
        return 32;
    }
#endif // defined(TARGET_XARCH)
    if (GetJitTls() != nullptr && JitTls::GetCompiler() != nullptr)
    {
        JITDUMP("getMaxIntrinsicSIMDVectorLength: returning 16\n");
    }
    return 16;
#else  // !FEATURE_SIMD
    if (GetJitTls() != nullptr && JitTls::GetCompiler() != nullptr)
    {
        JITDUMP("getMaxIntrinsicSIMDVectorLength: returning 0\n");
    }
    return 0;
#endif // !FEATURE_SIMD
}

//------------------------------------------------------------------------
// eeGetArgSize: Returns the number of bytes required for the given type argument
//   including padding after the actual value.
//
// Arguments:
//   list - the arg list handle pointing to the argument
//   sig  - the signature for the arg's method
//
// Return value:
//   the number of stack slots in stack arguments for the call.
//
// Notes:
//   - On most platforms arguments are passed with TARGET_POINTER_SIZE alignment,
//   so all types take an integer number of TARGET_POINTER_SIZE slots.
//   It is different for arm64 apple that packs some types without alignment and padding.
//   If the argument is passed by reference then the method returns REF size.
//
unsigned Compiler::eeGetArgSize(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig)
{
#if defined(TARGET_AMD64)

    // Everything fits into a single 'slot' size
    // to accommodate irregular sized structs, they are passed byref
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef UNIX_AMD64_ABI
    CORINFO_CLASS_HANDLE argClass;
    CorInfoType          argTypeJit = strip(info.compCompHnd->getArgType(sig, list, &argClass));
    var_types            argType    = JITtype2varType(argTypeJit);
    if (varTypeIsStruct(argType))
    {
        unsigned structSize = info.compCompHnd->getClassSize(argClass);
        return roundUp(structSize, TARGET_POINTER_SIZE);
    }
#endif // UNIX_AMD64_ABI
    return TARGET_POINTER_SIZE;

#else // !TARGET_AMD64

    CORINFO_CLASS_HANDLE argClass;
    CorInfoType          argTypeJit = strip(info.compCompHnd->getArgType(sig, list, &argClass));
    var_types            argType    = JITtype2varType(argTypeJit);
    unsigned             argSize;

    var_types hfaType = TYP_UNDEF;
    bool      isHfa   = false;

    if (varTypeIsStruct(argType))
    {
        hfaType             = GetHfaType(argClass);
        isHfa               = (hfaType != TYP_UNDEF);
        unsigned structSize = info.compCompHnd->getClassSize(argClass);

        // make certain the EE passes us back the right thing for refanys
        assert(argTypeJit != CORINFO_TYPE_REFANY || structSize == 2 * TARGET_POINTER_SIZE);

        // For each target that supports passing struct args in multiple registers
        // apply the target specific rules for them here:
        CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_MULTIREG_ARGS
#if defined(TARGET_ARM64)
        // Any structs that are larger than MAX_PASS_MULTIREG_BYTES are always passed by reference
        if (structSize > MAX_PASS_MULTIREG_BYTES)
        {
            // This struct is passed by reference using a single 'slot'
            return TARGET_POINTER_SIZE;
        }
        else
        {
            // Is the struct larger than 16 bytes
            if (structSize > (2 * TARGET_POINTER_SIZE))
            {

                if (TargetOS::IsWindows && info.compIsVarArgs)
                {
                    // Arm64 Varargs ABI requires passing in general purpose
                    // registers. Force the decision of whether this is an HFA
                    // to false to correctly pass as if it was not an HFA.
                    isHfa = false;
                }
                if (!isHfa)
                {
                    // This struct is passed by reference using a single 'slot'
                    return TARGET_POINTER_SIZE;
                }
            }
        }
#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // Any structs that are larger than MAX_PASS_MULTIREG_BYTES are always passed by reference
        if (structSize > MAX_PASS_MULTIREG_BYTES)
        {
            // This struct is passed by reference using a single 'slot'
            return TARGET_POINTER_SIZE;
        }
//  otherwise will we pass this struct by value in multiple registers
#elif !defined(TARGET_ARM)
        NYI("unknown target");
#endif // defined(TARGET_XXX)
#endif // FEATURE_MULTIREG_ARGS

        // Otherwise we will pass this struct by value in multiple registers/stack bytes.
        argSize = structSize;
    }
    else
    {
        argSize = genTypeSize(argType);
    }

    const unsigned argSizeAlignment = eeGetArgSizeAlignment(argType, (hfaType == TYP_FLOAT));
    const unsigned alignedArgSize   = roundUp(argSize, argSizeAlignment);
    return alignedArgSize;

#endif
}

//------------------------------------------------------------------------
// eeGetArgSizeAlignment: Return alignment for an argument size.
//
// Arguments:
//   type - the argument type
//   isFloatHfa - is it an HFA<float> type
//
// Return value:
//   the required argument size alignment in bytes.
//
// Notes:
//   Usually values passed on the stack are aligned to stack slot (i.e. pointer size), except for
//   on macOS ARM ABI that allows packing multiple args into a single stack slot.
//
//   The arg size alignment can be different from the normal alignment. One
//   example is on arm32 where a struct containing a double and float can
//   explicitly have size 12 but with alignment 8, in which case the size is
//   aligned to 4 (the stack slot size) while frame layout must still handle
//   aligning the argument to 8.
//
// static
unsigned Compiler::eeGetArgSizeAlignment(var_types type, bool isFloatHfa)
{
    if (compMacOsArm64Abi())
    {
        if (isFloatHfa)
        {
            assert(varTypeIsStruct(type));
            return sizeof(float);
        }
        if (varTypeIsStruct(type))
        {
            return TARGET_POINTER_SIZE;
        }
        const unsigned argSize = genTypeSize(type);
        assert((0 < argSize) && (argSize <= TARGET_POINTER_SIZE));
        return argSize;
    }
    else
    {
        return TARGET_POINTER_SIZE;
    }
}

/*****************************************************************************/

GenTree* Compiler::eeGetPInvokeCookie(CORINFO_SIG_INFO* szMetaSig)
{
    void *cookie, *pCookie;
    cookie = info.compCompHnd->GetCookieForPInvokeCalliSig(szMetaSig, &pCookie);
    assert((cookie == nullptr) != (pCookie == nullptr));

    return gtNewIconEmbHndNode(cookie, pCookie, GTF_ICON_PINVKI_HDL, szMetaSig);
}

//------------------------------------------------------------------------
// eeGetArrayDataOffset: Gets the offset of a SDArray's first element
//
// Return Value:
//    The offset to the first array element.
//
// Notes:
//    See the comments at the definition of CORINFO_Array for a description of how arrays are laid out in memory.
//
// static
unsigned Compiler::eeGetArrayDataOffset()
{
    return OFFSETOF__CORINFO_Array__data;
}

//------------------------------------------------------------------------
// eeGetMDArrayDataOffset: Gets the offset of a MDArray's first element
//
// Arguments:
//    rank - The array rank
//
// Return Value:
//    The offset to the first array element.
//
// Assumptions:
//    The rank should be greater than 0.
//
// static
unsigned Compiler::eeGetMDArrayDataOffset(unsigned rank)
{
    assert(rank > 0);
    // Note that below we're specifically using genTypeSize(TYP_INT) because array
    // indices are not native int.
    return eeGetArrayDataOffset() + 2 * genTypeSize(TYP_INT) * rank;
}

//------------------------------------------------------------------------
// eeGetMDArrayLengthOffset: Returns the offset from the Array object to the
//   size for the given dimension.
//
// Arguments:
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
//
// static
unsigned Compiler::eeGetMDArrayLengthOffset(unsigned rank, unsigned dimension)
{
    // Note that we don't actually need the `rank` value for this calculation, but we pass it anyway,
    // to be consistent with other MD array functions.
    assert(rank > 0);
    assert(dimension < rank);
    // Note that the lower bound and length fields of the Array object are always TYP_INT, even on 64-bit targets.
    return eeGetArrayDataOffset() + genTypeSize(TYP_INT) * dimension;
}

//------------------------------------------------------------------------
// eeGetMDArrayLowerBoundOffset: Returns the offset from the Array object to the
//   lower bound for the given dimension.
//
// Arguments:
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
//
// static
unsigned Compiler::eeGetMDArrayLowerBoundOffset(unsigned rank, unsigned dimension)
{
    assert(rank > 0);
    assert(dimension < rank);
    // Note that the lower bound and length fields of the Array object are always TYP_INT, even on 64-bit targets.
    return eeGetArrayDataOffset() + genTypeSize(TYP_INT) * (dimension + rank);
}

/*****************************************************************************/

void Compiler::eeGetStmtOffsets()
{
    ULONG32                      offsetsCount;
    uint32_t*                    offsets;
    ICorDebugInfo::BoundaryTypes offsetsImplicit;

    if (compIsForInlining())
    {
        // We do not get explicit boundaries for inlinees, only implicit ones.
        offsetsImplicit = impInlineRoot()->info.compStmtOffsetsImplicit;
        offsetsCount    = 0;
        offsets         = nullptr;
    }
    else
    {
        info.compCompHnd->getBoundaries(info.compMethodHnd, &offsetsCount, &offsets, &offsetsImplicit);
    }

    /* Set the implicit boundaries */

    info.compStmtOffsetsImplicit = (ICorDebugInfo::BoundaryTypes)offsetsImplicit;

    /* Process the explicit boundaries */

    info.compStmtOffsetsCount = 0;

    if (offsetsCount == 0)
    {
        return;
    }

    info.compStmtOffsets = new (this, CMK_DebugInfo) IL_OFFSET[offsetsCount];

    for (unsigned i = 0; i < offsetsCount; i++)
    {
        if (offsets[i] > info.compILCodeSize)
        {
            continue;
        }

        info.compStmtOffsets[info.compStmtOffsetsCount] = offsets[i];
        info.compStmtOffsetsCount++;
    }

    info.compCompHnd->freeArray(offsets);
}

/*****************************************************************************
 *
 *                  Debugging support - Local var info
 */

void Compiler::eeSetLVcount(unsigned count)
{
    assert(opts.compScopeInfo);

    JITDUMP("VarLocInfo count is %d\n", count);

    eeVarsCount = count;
    if (eeVarsCount)
    {
        eeVars = (VarResultInfo*)info.compCompHnd->allocateArray(eeVarsCount * sizeof(eeVars[0]));
    }
    else
    {
        eeVars = nullptr;
    }
}

void Compiler::eeSetLVinfo(unsigned                          which,
                           UNATIVE_OFFSET                    startOffs,
                           UNATIVE_OFFSET                    length,
                           unsigned                          varNum,
                           const CodeGenInterface::siVarLoc& varLoc)
{
    // ICorDebugInfo::VarLoc and CodeGenInterface::siVarLoc have to overlap
    // This is checked in siInit()

    assert(opts.compScopeInfo);
    assert(eeVarsCount > 0);
    assert(which < eeVarsCount);

    if (eeVars != nullptr)
    {
        eeVars[which].startOffset = startOffs;
        eeVars[which].endOffset   = startOffs + length;
        eeVars[which].varNumber   = varNum;
        eeVars[which].loc         = varLoc;
    }
}

void Compiler::eeSetLVdone()
{
    // necessary but not sufficient condition that the 2 struct definitions overlap
    assert(sizeof(eeVars[0]) == sizeof(ICorDebugInfo::NativeVarInfo));
    assert(opts.compScopeInfo);

#ifdef DEBUG
    if (verbose || opts.dspDebugInfo)
    {
        eeDispVars(info.compMethodHnd, eeVarsCount, (ICorDebugInfo::NativeVarInfo*)eeVars);
    }
#endif // DEBUG
    if ((eeVarsCount == 0) && (eeVars != nullptr))
    {
        // We still call setVars with nullptr when eeVarsCount is 0 as part of the contract.
        // We also need to free the nonused memory.
        info.compCompHnd->freeArray(eeVars);
        eeVars = nullptr;
    }

    info.compCompHnd->setVars(info.compMethodHnd, eeVarsCount, (ICorDebugInfo::NativeVarInfo*)eeVars);
    eeVars = nullptr; // We give up ownership after setVars()
}

void Compiler::eeGetVars()
{
    ICorDebugInfo::ILVarInfo* varInfoTable;
    ULONG32                   varInfoCount;
    bool                      extendOthers;

    info.compCompHnd->getVars(info.compMethodHnd, &varInfoCount, &varInfoTable, &extendOthers);

#ifdef DEBUG
    if (verbose)
    {
        printf("getVars() returned cVars = %d, extendOthers = %s\n", varInfoCount, extendOthers ? "true" : "false");
    }
#endif

    // Over allocate in case extendOthers is set.

    SIZE_T varInfoCountExtra = varInfoCount;
    if (extendOthers)
    {
        varInfoCountExtra += info.compLocalsCount;
    }

    if (varInfoCountExtra == 0)
    {
        return;
    }

    info.compVarScopes = new (this, CMK_DebugInfo) VarScopeDsc[varInfoCountExtra];

    VarScopeDsc*              localVarPtr = info.compVarScopes;
    ICorDebugInfo::ILVarInfo* v           = varInfoTable;

    for (unsigned i = 0; i < varInfoCount; i++, v++)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("var:%d start:%d end:%d\n", v->varNumber, v->startOffset, v->endOffset);
        }
#endif

        if (v->startOffset >= v->endOffset)
        {
            continue;
        }

        assert(v->startOffset <= info.compILCodeSize);
        assert(v->endOffset <= info.compILCodeSize);

        localVarPtr->vsdLifeBeg = v->startOffset;
        localVarPtr->vsdLifeEnd = v->endOffset;
        localVarPtr->vsdLVnum   = i;
        localVarPtr->vsdVarNum  = compMapILvarNum(v->varNumber);

#ifdef DEBUG
        localVarPtr->vsdName = gtGetLclVarName(localVarPtr->vsdVarNum);
#endif

        localVarPtr++;
        info.compVarScopesCount++;
    }

    /* If extendOthers is set, then assume the scope of unreported vars
       is the entire method. Note that this will cause fgExtendDbgLifetimes()
       to zero-initialize all of them. This will be expensive if it's used
       for too many variables.
     */
    if (extendOthers)
    {
        // Allocate a bit-array for all the variables and initialize to false

        bool*    varInfoProvided = getAllocator(CMK_Unknown).allocate<bool>(info.compLocalsCount);
        unsigned i;
        for (i = 0; i < info.compLocalsCount; i++)
        {
            varInfoProvided[i] = false;
        }

        // Find which vars have absolutely no varInfo provided

        for (i = 0; i < info.compVarScopesCount; i++)
        {
            varInfoProvided[info.compVarScopes[i].vsdVarNum] = true;
        }

        // Create entries for the variables with no varInfo

        for (unsigned varNum = 0; varNum < info.compLocalsCount; varNum++)
        {
            if (varInfoProvided[varNum])
            {
                continue;
            }

            // Create a varInfo with scope over the entire method

            localVarPtr->vsdLifeBeg = 0;
            localVarPtr->vsdLifeEnd = info.compILCodeSize;
            localVarPtr->vsdVarNum  = varNum;
            localVarPtr->vsdLVnum   = info.compVarScopesCount;

#ifdef DEBUG
            localVarPtr->vsdName = gtGetLclVarName(localVarPtr->vsdVarNum);
#endif

            localVarPtr++;
            info.compVarScopesCount++;
        }
    }

    assert(localVarPtr <= info.compVarScopes + varInfoCountExtra);

    if (varInfoCount != 0)
    {
        info.compCompHnd->freeArray(varInfoTable);
    }

#ifdef DEBUG
    if (verbose)
    {
        compDispLocalVars();
    }
#endif // DEBUG
}

#ifdef DEBUG
void Compiler::eeDispVar(ICorDebugInfo::NativeVarInfo* var)
{
    const char* name = nullptr;

    if (var->varNumber == (DWORD)ICorDebugInfo::VARARGS_HND_ILNUM)
    {
        name = "varargsHandle";
    }
    else if (var->varNumber == (DWORD)ICorDebugInfo::RETBUF_ILNUM)
    {
        name = "retBuff";
    }
    else if (var->varNumber == (DWORD)ICorDebugInfo::TYPECTXT_ILNUM)
    {
        name = "typeCtx";
    }
    if (0 <= var->varNumber && var->varNumber < lvaCount)
    {
        printf("(");
        gtDispLclVar(var->varNumber, false);
        printf(")");
    }
    else
    {
        printf("(%10s)", (VarNameToStr(name) == nullptr) ? "UNKNOWN" : VarNameToStr(name));
    }
    printf(" : From %08Xh to %08Xh, in ", var->startOffset, var->endOffset);

    switch ((CodeGenInterface::siVarLocType)var->loc.vlType)
    {
        case CodeGenInterface::VLT_REG:
        case CodeGenInterface::VLT_REG_BYREF:
        case CodeGenInterface::VLT_REG_FP:
            printf("%s", getRegName(var->loc.vlReg.vlrReg));
            if (var->loc.vlType == (ICorDebugInfo::VarLocType)CodeGenInterface::VLT_REG_BYREF)
            {
                printf(" byref");
            }
            break;

        case CodeGenInterface::VLT_STK:
        case CodeGenInterface::VLT_STK_BYREF:
            if ((int)var->loc.vlStk.vlsBaseReg != (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                printf("%s[%d] (1 slot)", getRegName(var->loc.vlStk.vlsBaseReg), var->loc.vlStk.vlsOffset);
            }
            else
            {
                printf(STR_SPBASE "'[%d] (1 slot)", var->loc.vlStk.vlsOffset);
            }
            if (var->loc.vlType == (ICorDebugInfo::VarLocType)CodeGenInterface::VLT_REG_BYREF)
            {
                printf(" byref");
            }
            break;

        case CodeGenInterface::VLT_REG_REG:
            printf("%s-%s", getRegName(var->loc.vlRegReg.vlrrReg1), getRegName(var->loc.vlRegReg.vlrrReg2));
            break;

#ifndef TARGET_AMD64
        case CodeGenInterface::VLT_REG_STK:
            if ((int)var->loc.vlRegStk.vlrsStk.vlrssBaseReg != (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                printf("%s-%s[%d]", getRegName(var->loc.vlRegStk.vlrsReg),
                       getRegName(var->loc.vlRegStk.vlrsStk.vlrssBaseReg), var->loc.vlRegStk.vlrsStk.vlrssOffset);
            }
            else
            {
                printf("%s-" STR_SPBASE "'[%d]", getRegName(var->loc.vlRegStk.vlrsReg),
                       var->loc.vlRegStk.vlrsStk.vlrssOffset);
            }
            break;

        case CodeGenInterface::VLT_STK_REG:
            unreached(); // unexpected

        case CodeGenInterface::VLT_STK2:
            if ((int)var->loc.vlStk2.vls2BaseReg != (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                printf("%s[%d] (2 slots)", getRegName(var->loc.vlStk2.vls2BaseReg), var->loc.vlStk2.vls2Offset);
            }
            else
            {
                printf(STR_SPBASE "'[%d] (2 slots)", var->loc.vlStk2.vls2Offset);
            }
            break;

        case CodeGenInterface::VLT_FPSTK:
            printf("ST(L-%d)", var->loc.vlFPstk.vlfReg);
            break;

        case CodeGenInterface::VLT_FIXED_VA:
            printf("fxd_va[%d]", var->loc.vlFixedVarArg.vlfvOffset);
            break;
#endif // !TARGET_AMD64

        default:
            unreached(); // unexpected
    }

    printf("\n");
}

// Same parameters as ICorStaticInfo::setVars().
void Compiler::eeDispVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars, ICorDebugInfo::NativeVarInfo* vars)
{
    // Estimate number of unique vars with debug info
    //
    ALLVARSET_TP uniqueVars(AllVarSetOps::MakeEmpty(this));
    for (unsigned i = 0; i < cVars; i++)
    {
        // ignore "special vars" and out of bounds vars
        if ((((int)vars[i].varNumber) >= 0) && (vars[i].varNumber < lclMAX_ALLSET_TRACKED))
        {
            AllVarSetOps::AddElemD(this, uniqueVars, vars[i].varNumber);
        }
    }

    printf("; Variable debug info: %d live ranges, %d vars for method %s\n", cVars,
           AllVarSetOps::Count(this, uniqueVars), info.compFullName);

    for (unsigned i = 0; i < cVars; i++)
    {
        eeDispVar(&vars[i]);
    }
}
#endif // DEBUG

/*****************************************************************************
 *
 *                  Debugging support - Line number info
 */

void Compiler::eeSetLIcount(unsigned count)
{
    assert(opts.compDbgInfo);

    eeBoundariesCount = count;
    if (eeBoundariesCount)
    {
        eeBoundaries =
            (ICorDebugInfo::OffsetMapping*)info.compCompHnd->allocateArray(eeBoundariesCount * sizeof(eeBoundaries[0]));
    }
    else
    {
        eeBoundaries = nullptr;
    }
}

void Compiler::eeSetLIinfo(unsigned which, UNATIVE_OFFSET nativeOffset, IPmappingDscKind kind, const ILLocation& loc)
{
    assert(opts.compDbgInfo);
    assert(eeBoundariesCount > 0 && eeBoundaries != nullptr);
    assert(which < eeBoundariesCount);

    eeBoundaries[which].nativeOffset = nativeOffset;
    eeBoundaries[which].source       = (ICorDebugInfo::SourceTypes)0;

    switch (kind)
    {
        case IPmappingDscKind::Normal:
            eeBoundaries[which].ilOffset = loc.GetOffset();
            eeBoundaries[which].source   = loc.EncodeSourceTypes();
            break;
        case IPmappingDscKind::Prolog:
            eeBoundaries[which].ilOffset = ICorDebugInfo::PROLOG;
            eeBoundaries[which].source   = ICorDebugInfo::STACK_EMPTY;
            break;
        case IPmappingDscKind::Epilog:
            eeBoundaries[which].ilOffset = ICorDebugInfo::EPILOG;
            eeBoundaries[which].source   = ICorDebugInfo::STACK_EMPTY;
            break;
        case IPmappingDscKind::NoMapping:
            eeBoundaries[which].ilOffset = ICorDebugInfo::NO_MAPPING;
            eeBoundaries[which].source   = ICorDebugInfo::STACK_EMPTY;
            break;
        default:
            unreached();
    }
}

void Compiler::eeSetLIdone()
{
    assert(opts.compDbgInfo);

#if defined(DEBUG)
    if (verbose || opts.dspDebugInfo)
    {
        eeDispLineInfos();
    }
#endif // DEBUG

    // necessary but not sufficient condition that the 2 struct definitions overlap
    assert(sizeof(eeBoundaries[0]) == sizeof(ICorDebugInfo::OffsetMapping));

    info.compCompHnd->setBoundaries(info.compMethodHnd, eeBoundariesCount, (ICorDebugInfo::OffsetMapping*)eeBoundaries);

    eeBoundaries = nullptr; // we give up ownership after setBoundaries();
}

#if defined(DEBUG)

void Compiler::eeDispILOffs(IL_OFFSET offs)
{
    printf("0x%04X", offs);
}

/* static */
void Compiler::eeDispSourceMappingOffs(uint32_t offs)
{
    const char* specialOffs[] = {"EPILOG", "PROLOG", "NO_MAP"};

    switch ((int)offs) // Need the cast since offs is unsigned and the case statements are comparing to signed.
    {
        case ICorDebugInfo::EPILOG:
        case ICorDebugInfo::PROLOG:
        case ICorDebugInfo::NO_MAPPING:
            assert(DWORD(ICorDebugInfo::EPILOG) + 1 == (unsigned)ICorDebugInfo::PROLOG);
            assert(DWORD(ICorDebugInfo::EPILOG) + 2 == (unsigned)ICorDebugInfo::NO_MAPPING);
            int specialOffsNum;
            specialOffsNum = offs - DWORD(ICorDebugInfo::EPILOG);
            printf("%s", specialOffs[specialOffsNum]);
            break;
        default:
            eeDispILOffs(offs);
            break;
    }
}

/* static */
void Compiler::eeDispLineInfo(const ICorDebugInfo::OffsetMapping* line)
{
    printf("IL offs ");

    eeDispSourceMappingOffs(line->ilOffset);

    printf(" : 0x%08X", line->nativeOffset);
    if (line->source != 0)
    {
        // It seems like it should probably never be zero since ICorDebugInfo::SOURCE_TYPE_INVALID is zero.
        // However, the JIT has always generated this and printed "stack non-empty".

        printf(" ( ");
        if ((line->source & ICorDebugInfo::STACK_EMPTY) != 0)
        {
            printf("STACK_EMPTY ");
        }
        if ((line->source & ICorDebugInfo::CALL_INSTRUCTION) != 0)
        {
            printf("CALL_INSTRUCTION ");
        }
        if ((line->source & ICorDebugInfo::CALL_SITE) != 0)
        {
            printf("CALL_SITE ");
        }
        printf(")");
    }
    printf("\n");

    // We don't expect to see any other bits.
    assert((line->source & ~(ICorDebugInfo::STACK_EMPTY | ICorDebugInfo::CALL_INSTRUCTION)) == 0);
}

void Compiler::eeDispLineInfos()
{
    printf("IP mapping count : %d\n", eeBoundariesCount); // this might be zero
    for (unsigned i = 0; i < eeBoundariesCount; i++)
    {
        eeDispLineInfo(&eeBoundaries[i]);
    }
    printf("\n");
}
#endif // DEBUG

/*****************************************************************************
 *
 *                      ICorJitInfo wrapper functions
 *
 * In many cases here, we don't tell the VM about various unwind or EH information if
 * we're an altjit for an unexpected architecture. If it's not a same architecture JIT
 * (e.g., host AMD64, target ARM64), then VM will get confused anyway.
 */

void Compiler::eeAllocMem(AllocMemArgs* args, const UNATIVE_OFFSET roDataSectionAlignment)
{
#ifdef DEBUG

    // Fake splitting implementation: place hot/cold code in contiguous section.
    UNATIVE_OFFSET coldCodeOffset = 0;
    if (JitConfig.JitFakeProcedureSplitting() && (args->coldCodeSize > 0))
    {
        coldCodeOffset = args->hotCodeSize;
        assert(coldCodeOffset > 0);
        args->hotCodeSize += args->coldCodeSize;
        args->coldCodeSize = 0;
    }

#endif // DEBUG

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

    // For arm64/LoongArch64, we want to allocate JIT data always adjacent to code similar to what native compiler does.
    // This way allows us to use a single `ldr` to access such data like float constant/jmp table.
    // For LoongArch64 using `pcaddi + ld` to access such data.

    UNATIVE_OFFSET roDataAlignmentDelta = 0;
    if (args->roDataSize > 0)
    {
        roDataAlignmentDelta = AlignmentPad(args->hotCodeSize, roDataSectionAlignment);
    }

    const UNATIVE_OFFSET roDataOffset = args->hotCodeSize + roDataAlignmentDelta;
    args->hotCodeSize                 = roDataOffset + args->roDataSize;
    args->roDataSize                  = 0;

#endif // defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

    info.compCompHnd->allocMem(args);

#ifdef DEBUG

    if (JitConfig.JitFakeProcedureSplitting() && (coldCodeOffset > 0))
    {
        // Fix up cold code pointers. Cold section is adjacent to hot section.
        assert(args->coldCodeBlock == nullptr);
        assert(args->coldCodeBlockRW == nullptr);
        args->coldCodeBlock   = ((BYTE*)args->hotCodeBlock) + coldCodeOffset;
        args->coldCodeBlockRW = ((BYTE*)args->hotCodeBlockRW) + coldCodeOffset;
    }

#endif // DEBUG

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

    // Fix up data section pointers.
    assert(args->roDataBlock == nullptr);
    assert(args->roDataBlockRW == nullptr);
    args->roDataBlock   = ((BYTE*)args->hotCodeBlock) + roDataOffset;
    args->roDataBlockRW = ((BYTE*)args->hotCodeBlockRW) + roDataOffset;

#endif // defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
}

void Compiler::eeReserveUnwindInfo(bool isFunclet, bool isColdCode, ULONG unwindSize)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("reserveUnwindInfo(isFunclet=%s, isColdCode=%s, unwindSize=0x%x)\n", isFunclet ? "true" : "false",
               isColdCode ? "true" : "false", unwindSize);
    }
#endif // DEBUG

    if (info.compMatchedVM)
    {
        info.compCompHnd->reserveUnwindInfo(isFunclet, isColdCode, unwindSize);
    }
}

void Compiler::eeAllocUnwindInfo(BYTE*          pHotCode,
                                 BYTE*          pColdCode,
                                 ULONG          startOffset,
                                 ULONG          endOffset,
                                 ULONG          unwindSize,
                                 BYTE*          pUnwindBlock,
                                 CorJitFuncKind funcKind)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("allocUnwindInfo(pHotCode=0x%p, pColdCode=0x%p, startOffset=0x%x, endOffset=0x%x, unwindSize=0x%x, "
               "pUnwindBlock=0x%p, funKind=%d",
               dspPtr(pHotCode), dspPtr(pColdCode), startOffset, endOffset, unwindSize, dspPtr(pUnwindBlock), funcKind);
        switch (funcKind)
        {
            case CORJIT_FUNC_ROOT:
                printf(" (main function)");
                break;
            case CORJIT_FUNC_HANDLER:
                printf(" (handler)");
                break;
            case CORJIT_FUNC_FILTER:
                printf(" (filter)");
                break;
            default:
                printf(" (ILLEGAL)");
                break;
        }
        printf(")\n");
    }
#endif // DEBUG

    if (info.compMatchedVM)
    {
        info.compCompHnd->allocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock,
                                          funcKind);
    }
}

void Compiler::eeSetEHcount(unsigned cEH)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("setEHcount(cEH=%u)\n", cEH);
    }
#endif // DEBUG

    if (info.compMatchedVM)
    {
        info.compCompHnd->setEHcount(cEH);
    }
}

void Compiler::eeSetEHinfo(unsigned EHnumber, const CORINFO_EH_CLAUSE* clause)
{
#ifdef DEBUG
    if (opts.dspEHTable)
    {
        dispOutgoingEHClause(EHnumber, *clause);
    }
#endif // DEBUG

    if (info.compMatchedVM)
    {
        info.compCompHnd->setEHinfo(EHnumber, clause);
    }
}

WORD Compiler::eeGetRelocTypeHint(void* target)
{
    if (info.compMatchedVM)
    {
        return info.compCompHnd->getRelocTypeHint(target);
    }
    else
    {
        // No hints
        return (WORD)-1;
    }
}

CORINFO_FIELD_HANDLE Compiler::eeFindJitDataOffs(unsigned dataOffs)
{
    // Data offsets are marked by the fact that the low two bits are 0b01 0x1
    assert(dataOffs < 0x40000000);
    return (CORINFO_FIELD_HANDLE)(size_t)((dataOffs << iaut_SHIFT) | iaut_DATA_OFFSET);
}

bool Compiler::eeIsJitDataOffs(CORINFO_FIELD_HANDLE field)
{
    // if 'field' is a jit data offset it has to fit into a 32-bit unsigned int
    unsigned value = static_cast<unsigned>(reinterpret_cast<uintptr_t>(field));
    if (((CORINFO_FIELD_HANDLE)(size_t)value) != field)
    {
        return false; // some bits in the upper 32 bits were set, not a jit data offset
    }

    // Data offsets are marked by the fact that the low two bits are 0b01
    return (value & iaut_MASK) == iaut_DATA_OFFSET;
}

int Compiler::eeGetJitDataOffs(CORINFO_FIELD_HANDLE field)
{
    // Data offsets are marked by the fact that the low two bits are 0b01 0x1
    if (eeIsJitDataOffs(field))
    {
        unsigned dataOffs = static_cast<unsigned>(reinterpret_cast<uintptr_t>(field));
        assert(((CORINFO_FIELD_HANDLE)(size_t)dataOffs) == field);
        assert(dataOffs < 0x40000000);

        // Shift away the low two bits
        return (static_cast<int>(reinterpret_cast<intptr_t>(field))) >> iaut_SHIFT;
    }
    else
    {
        return -1;
    }
}

/*****************************************************************************
 *
 *                      ICorStaticInfo wrapper functions
 */

#if defined(UNIX_AMD64_ABI)

#ifdef DEBUG
void Compiler::dumpSystemVClassificationType(SystemVClassificationType ct)
{
    switch (ct)
    {
        case SystemVClassificationTypeUnknown:
            printf("UNKNOWN");
            break;
        case SystemVClassificationTypeStruct:
            printf("Struct");
            break;
        case SystemVClassificationTypeNoClass:
            printf("NoClass");
            break;
        case SystemVClassificationTypeMemory:
            printf("Memory");
            break;
        case SystemVClassificationTypeInteger:
            printf("Integer");
            break;
        case SystemVClassificationTypeIntegerReference:
            printf("IntegerReference");
            break;
        case SystemVClassificationTypeIntegerByRef:
            printf("IntegerByReference");
            break;
        case SystemVClassificationTypeSSE:
            printf("SSE");
            break;
        default:
            printf("ILLEGAL");
            break;
    }
}
#endif // DEBUG

void Compiler::eeGetSystemVAmd64PassStructInRegisterDescriptor(
    /*IN*/ CORINFO_CLASS_HANDLE                                  structHnd,
    /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    bool ok = info.compCompHnd->getSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr);
    noway_assert(ok);

#ifdef DEBUG
    if (verbose)
    {
        printf("**** getSystemVAmd64PassStructInRegisterDescriptor(0x%x (%s), ...) =>\n", dspPtr(structHnd),
               eeGetClassName(structHnd));
        printf("        passedInRegisters = %s\n", dspBool(structPassInRegDescPtr->passedInRegisters));
        if (structPassInRegDescPtr->passedInRegisters)
        {
            printf("        eightByteCount   = %d\n", structPassInRegDescPtr->eightByteCount);
            for (unsigned int i = 0; i < structPassInRegDescPtr->eightByteCount; i++)
            {
                printf("        eightByte #%d -- classification: ", i);
                dumpSystemVClassificationType(structPassInRegDescPtr->eightByteClassifications[i]);
                printf(", byteSize: %d, byteOffset: %d\n", structPassInRegDescPtr->eightByteSizes[i],
                       structPassInRegDescPtr->eightByteOffsets[i]);
            }
        }
    }
#endif // DEBUG
}

#endif // UNIX_AMD64_ABI

bool Compiler::eeTryResolveToken(CORINFO_RESOLVED_TOKEN* resolvedToken)
{
    return info.compCompHnd->tryResolveToken(resolvedToken);
}

bool Compiler::eeRunWithErrorTrapImp(void (*function)(void*), void* param)
{
    return info.compCompHnd->runWithErrorTrap(function, param);
}

bool Compiler::eeRunWithSPMIErrorTrapImp(void (*function)(void*), void* param)
{
    return info.compCompHnd->runWithSPMIErrorTrap(function, param);
}

#ifdef DEBUG
//------------------------------------------------------------------------
// eeTryGetClassSize: wraps getClassSize but if doing SuperPMI replay
// and the value isn't found, use a bogus size.
//
// NOTE: This is only allowed for JitDump output.
//
// Return value:
//      Either the actual class size, or (unsigned)-1 if SuperPMI didn't have it.
//
unsigned Compiler::eeTryGetClassSize(CORINFO_CLASS_HANDLE clsHnd)
{
    unsigned classSize = UINT_MAX;
    eeRunFunctorWithSPMIErrorTrap([&]() { classSize = info.compCompHnd->getClassSize(clsHnd); });

    return classSize;
}

#endif // !DEBUG
