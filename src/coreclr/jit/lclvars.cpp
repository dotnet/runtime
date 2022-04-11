// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           LclVarsInfo                                     XX
XX                                                                           XX
XX   The variables to be used by the code generator.                         XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "emit.h"
#include "register_arg_convention.h"
#include "jitstd/algorithm.h"
#include "patchpointinfo.h"

/*****************************************************************************/

#ifdef DEBUG
#if DOUBLE_ALIGN
/* static */
unsigned Compiler::s_lvaDoubleAlignedProcsCount = 0;
#endif
#endif

/*****************************************************************************/

void Compiler::lvaInit()
{
    /* We haven't allocated stack variables yet */
    lvaRefCountState = RCS_INVALID;

    lvaGenericsContextInUse = false;

    lvaTrackedToVarNumSize = 0;
    lvaTrackedToVarNum     = nullptr;

    lvaTrackedFixed = false; // false: We can still add new tracked variables

    lvaDoneFrameLayout = NO_FRAME_LAYOUT;
#if !defined(FEATURE_EH_FUNCLETS)
    lvaShadowSPslotsVar = BAD_VAR_NUM;
#endif // !FEATURE_EH_FUNCLETS
    lvaInlinedPInvokeFrameVar = BAD_VAR_NUM;
    lvaReversePInvokeFrameVar = BAD_VAR_NUM;
#if FEATURE_FIXED_OUT_ARGS
    lvaPInvokeFrameRegSaveVar = BAD_VAR_NUM;
    lvaOutgoingArgSpaceVar    = BAD_VAR_NUM;
    lvaOutgoingArgSpaceSize   = PhasedVar<unsigned>();
#endif // FEATURE_FIXED_OUT_ARGS
#ifdef JIT32_GCENCODER
    lvaLocAllocSPvar = BAD_VAR_NUM;
#endif // JIT32_GCENCODER
    lvaNewObjArrayArgs  = BAD_VAR_NUM;
    lvaGSSecurityCookie = BAD_VAR_NUM;
#ifdef TARGET_X86
    lvaVarargsBaseOfStkArgs = BAD_VAR_NUM;
#endif // TARGET_X86
    lvaVarargsHandleArg = BAD_VAR_NUM;
    lvaStubArgumentVar  = BAD_VAR_NUM;
    lvaArg0Var          = BAD_VAR_NUM;
    lvaMonAcquired      = BAD_VAR_NUM;
    lvaRetAddrVar       = BAD_VAR_NUM;

    lvaInlineeReturnSpillTemp = BAD_VAR_NUM;

    gsShadowVarInfo = nullptr;
#if defined(FEATURE_EH_FUNCLETS)
    lvaPSPSym = BAD_VAR_NUM;
#endif
#if FEATURE_SIMD
    lvaSIMDInitTempVarNum = BAD_VAR_NUM;
#endif // FEATURE_SIMD
    lvaCurEpoch = 0;

    structPromotionHelper = new (this, CMK_Generic) StructPromotionHelper(this);
}

/*****************************************************************************/

void Compiler::lvaInitTypeRef()
{

    /* x86 args look something like this:
        [this ptr] [hidden return buffer] [declared arguments]* [generic context] [var arg cookie]

       x64 is closer to the native ABI:
        [this ptr] [hidden return buffer] [generic context] [var arg cookie] [declared arguments]*
        (Note: prior to .NET Framework 4.5.1 for Windows 8.1 (but not .NET Framework 4.5.1 "downlevel"),
        the "hidden return buffer" came before the "this ptr". Now, the "this ptr" comes first. This
        is different from the C++ order, where the "hidden return buffer" always comes first.)

       ARM and ARM64 are the same as the current x64 convention:
        [this ptr] [hidden return buffer] [generic context] [var arg cookie] [declared arguments]*

       Key difference:
           The var arg cookie and generic context are swapped with respect to the user arguments
    */

    /* Set compArgsCount and compLocalsCount */

    info.compArgsCount = info.compMethodInfo->args.numArgs;

    // Is there a 'this' pointer

    if (!info.compIsStatic)
    {
        info.compArgsCount++;
    }
    else
    {
        info.compThisArg = BAD_VAR_NUM;
    }

    info.compILargsCount = info.compArgsCount;

#ifdef FEATURE_SIMD
    if (info.compRetNativeType == TYP_STRUCT)
    {
        var_types structType = impNormStructType(info.compMethodInfo->args.retTypeClass);
        info.compRetType     = structType;
    }
#endif // FEATURE_SIMD

    // Are we returning a struct using a return buffer argument?
    //
    const bool hasRetBuffArg = impMethodInfo_hasRetBuffArg(info.compMethodInfo, info.compCallConv);

    // Possibly change the compRetNativeType from TYP_STRUCT to a "primitive" type
    // when we are returning a struct by value and it fits in one register
    //
    if (!hasRetBuffArg && varTypeIsStruct(info.compRetNativeType))
    {
        CORINFO_CLASS_HANDLE retClsHnd = info.compMethodInfo->args.retTypeClass;

        Compiler::structPassingKind howToReturnStruct;
        var_types returnType = getReturnTypeForStruct(retClsHnd, info.compCallConv, &howToReturnStruct);

        // We can safely widen the return type for enclosed structs.
        if ((howToReturnStruct == SPK_PrimitiveType) || (howToReturnStruct == SPK_EnclosingType))
        {
            assert(returnType != TYP_UNKNOWN);
            assert(returnType != TYP_STRUCT);

            info.compRetNativeType = returnType;

            // ToDo: Refactor this common code sequence into its own method as it is used 4+ times
            if ((returnType == TYP_LONG) && (compLongUsed == false))
            {
                compLongUsed = true;
            }
            else if (((returnType == TYP_FLOAT) || (returnType == TYP_DOUBLE)) && (compFloatingPointUsed == false))
            {
                compFloatingPointUsed = true;
            }
        }
    }

    // Do we have a RetBuffArg?

    if (hasRetBuffArg)
    {
        info.compArgsCount++;
    }
    else
    {
        info.compRetBuffArg = BAD_VAR_NUM;
    }

    /* There is a 'hidden' cookie pushed last when the
       calling convention is varargs */

    if (info.compIsVarArgs)
    {
        info.compArgsCount++;
    }

    // Is there an extra parameter used to pass instantiation info to
    // shared generic methods and shared generic struct instance methods?
    if (info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        info.compArgsCount++;
    }
    else
    {
        info.compTypeCtxtArg = BAD_VAR_NUM;
    }

    lvaCount = info.compLocalsCount = info.compArgsCount + info.compMethodInfo->locals.numArgs;

    info.compILlocalsCount = info.compILargsCount + info.compMethodInfo->locals.numArgs;

    /* Now allocate the variable descriptor table */

    if (compIsForInlining())
    {
        lvaTable    = impInlineInfo->InlinerCompiler->lvaTable;
        lvaCount    = impInlineInfo->InlinerCompiler->lvaCount;
        lvaTableCnt = impInlineInfo->InlinerCompiler->lvaTableCnt;

        // No more stuff needs to be done.
        return;
    }

    lvaTableCnt = lvaCount * 2;

    if (lvaTableCnt < 16)
    {
        lvaTableCnt = 16;
    }

    lvaTable         = getAllocator(CMK_LvaTable).allocate<LclVarDsc>(lvaTableCnt);
    size_t tableSize = lvaTableCnt * sizeof(*lvaTable);
    memset(lvaTable, 0, tableSize);
    for (unsigned i = 0; i < lvaTableCnt; i++)
    {
        new (&lvaTable[i], jitstd::placement_t()) LclVarDsc(); // call the constructor.
    }

    //-------------------------------------------------------------------------
    // Count the arguments and initialize the respective lvaTable[] entries
    //
    // First the implicit arguments
    //-------------------------------------------------------------------------

    InitVarDscInfo varDscInfo;
#ifdef TARGET_X86
    // x86 unmanaged calling conventions limit the number of registers supported
    // for accepting arguments. As a result, we need to modify the number of registers
    // when we emit a method with an unmanaged calling convention.
    switch (info.compCallConv)
    {
        case CorInfoCallConvExtension::Thiscall:
            // In thiscall the this parameter goes into a register.
            varDscInfo.Init(lvaTable, hasRetBuffArg, 1, 0);
            break;
        case CorInfoCallConvExtension::C:
        case CorInfoCallConvExtension::Stdcall:
        case CorInfoCallConvExtension::CMemberFunction:
        case CorInfoCallConvExtension::StdcallMemberFunction:
            varDscInfo.Init(lvaTable, hasRetBuffArg, 0, 0);
            break;
        case CorInfoCallConvExtension::Managed:
        case CorInfoCallConvExtension::Fastcall:
        case CorInfoCallConvExtension::FastcallMemberFunction:
        default:
            varDscInfo.Init(lvaTable, hasRetBuffArg, MAX_REG_ARG, MAX_FLOAT_REG_ARG);
            break;
    }
#else
    varDscInfo.Init(lvaTable, hasRetBuffArg, MAX_REG_ARG, MAX_FLOAT_REG_ARG);
#endif

    lvaInitArgs(&varDscInfo);

    //-------------------------------------------------------------------------
    // Finally the local variables
    //-------------------------------------------------------------------------

    unsigned                varNum    = varDscInfo.varNum;
    LclVarDsc*              varDsc    = varDscInfo.varDsc;
    CORINFO_ARG_LIST_HANDLE localsSig = info.compMethodInfo->locals.args;

#ifdef TARGET_ARM
    compHasSplitParam = varDscInfo.hasSplitParam;
#endif

    for (unsigned i = 0; i < info.compMethodInfo->locals.numArgs;
         i++, varNum++, varDsc++, localsSig = info.compCompHnd->getArgNext(localsSig))
    {
        CORINFO_CLASS_HANDLE typeHnd;
        CorInfoTypeWithMod   corInfoTypeWithMod =
            info.compCompHnd->getArgType(&info.compMethodInfo->locals, localsSig, &typeHnd);
        CorInfoType corInfoType = strip(corInfoTypeWithMod);

        lvaInitVarDsc(varDsc, varNum, corInfoType, typeHnd, localsSig, &info.compMethodInfo->locals);

        if ((corInfoTypeWithMod & CORINFO_TYPE_MOD_PINNED) != 0)
        {
            if ((corInfoType == CORINFO_TYPE_CLASS) || (corInfoType == CORINFO_TYPE_BYREF))
            {
                JITDUMP("Setting lvPinned for V%02u\n", varNum);
                varDsc->lvPinned = 1;

                if (opts.IsOSR())
                {
                    // OSR method may not see any references to the pinned local,
                    // but must still report it in GC info.
                    //
                    varDsc->lvImplicitlyReferenced = 1;
                }
            }
            else
            {
                JITDUMP("Ignoring pin for non-GC type V%02u\n", varNum);
            }
        }

        varDsc->lvOnFrame = true; // The final home for this local variable might be our local stack frame

        if (corInfoType == CORINFO_TYPE_CLASS)
        {
            CORINFO_CLASS_HANDLE clsHnd = info.compCompHnd->getArgClass(&info.compMethodInfo->locals, localsSig);
            lvaSetClass(varNum, clsHnd);
        }
    }

    if ( // If there already exist unsafe buffers, don't mark more structs as unsafe
        // as that will cause them to be placed along with the real unsafe buffers,
        // unnecessarily exposing them to overruns. This can affect GS tests which
        // intentionally do buffer-overruns.
        !getNeedsGSSecurityCookie() &&
        // GS checks require the stack to be re-ordered, which can't be done with EnC
        !opts.compDbgEnC && compStressCompile(STRESS_UNSAFE_BUFFER_CHECKS, 25))
    {
        setNeedsGSSecurityCookie();
        compGSReorderStackLayout = true;

        for (unsigned i = 0; i < lvaCount; i++)
        {
            if ((lvaTable[i].lvType == TYP_STRUCT) && compStressCompile(STRESS_GENERIC_VARN, 60))
            {
                lvaTable[i].lvIsUnsafeBuffer = true;
            }
        }
    }

    // If this is an OSR method, mark all the OSR locals and model OSR exposure.
    //
    // Do this before we add the GS Cookie Dummy or Outgoing args to the locals
    // so we don't have to do special checks to exclude them.
    //
    if (opts.IsOSR())
    {
        for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
        {
            LclVarDsc* const varDsc = lvaGetDesc(lclNum);
            varDsc->lvIsOSRLocal    = true;

            if (info.compPatchpointInfo->IsExposed(lclNum))
            {
                JITDUMP("-- V%02u is OSR exposed\n", lclNum);
                varDsc->lvHasLdAddrOp = 1;

                // todo: Why does it apply only to non-structs?
                //
                if (!varTypeIsStruct(varDsc) && !varTypeIsSIMD(varDsc))
                {
                    lvaSetVarAddrExposed(lclNum DEBUGARG(AddressExposedReason::OSR_EXPOSED));
                }
            }
        }
    }

    if (getNeedsGSSecurityCookie())
    {
        // Ensure that there will be at least one stack variable since
        // we require that the GSCookie does not have a 0 stack offset.
        unsigned   dummy         = lvaGrabTempWithImplicitUse(false DEBUGARG("GSCookie dummy"));
        LclVarDsc* gsCookieDummy = lvaGetDesc(dummy);
        gsCookieDummy->lvType    = TYP_INT;
        gsCookieDummy->lvIsTemp  = true; // It is not alive at all, set the flag to prevent zero-init.
        lvaSetVarDoNotEnregister(dummy DEBUGARG(DoNotEnregisterReason::VMNeedsStackAddr));
    }

    // Allocate the lvaOutgoingArgSpaceVar now because we can run into problems in the
    // emitter when the varNum is greater that 32767 (see emitLclVarAddr::initLclVarAddr)
    lvaAllocOutgoingArgSpaceVar();

#ifdef DEBUG
    if (verbose)
    {
        lvaTableDump(INITIAL_FRAME_LAYOUT);
    }
#endif
}

/*****************************************************************************/
void Compiler::lvaInitArgs(InitVarDscInfo* varDscInfo)
{
    compArgSize = 0;

#if defined(TARGET_ARM) && defined(PROFILING_SUPPORTED)
    // Prespill all argument regs on to stack in case of Arm when under profiler.
    if (compIsProfilerHookNeeded())
    {
        codeGen->regSet.rsMaskPreSpillRegArg |= RBM_ARG_REGS;
    }
#endif

    //----------------------------------------------------------------------

    /* Is there a "this" pointer ? */
    lvaInitThisPtr(varDscInfo);

    unsigned numUserArgsToSkip = 0;
    unsigned numUserArgs       = info.compMethodInfo->args.numArgs;
#if !defined(TARGET_ARM)
    if (TargetOS::IsWindows && callConvIsInstanceMethodCallConv(info.compCallConv))
    {
        // If we are a native instance method, handle the first user arg
        // (the unmanaged this parameter) and then handle the hidden
        // return buffer parameter.
        assert(numUserArgs >= 1);
        lvaInitUserArgs(varDscInfo, 0, 1);
        numUserArgsToSkip++;
        numUserArgs--;

        lvaInitRetBuffArg(varDscInfo, false);
    }
    else
#endif
    {
        /* If we have a hidden return-buffer parameter, that comes here */
        lvaInitRetBuffArg(varDscInfo, true);
    }

//======================================================================

#if USER_ARGS_COME_LAST
    //@GENERICS: final instantiation-info argument for shared generic methods
    // and shared generic struct instance methods
    lvaInitGenericsCtxt(varDscInfo);

    /* If the method is varargs, process the varargs cookie */
    lvaInitVarArgsHandle(varDscInfo);
#endif

    //-------------------------------------------------------------------------
    // Now walk the function signature for the explicit user arguments
    //-------------------------------------------------------------------------
    lvaInitUserArgs(varDscInfo, numUserArgsToSkip, numUserArgs);

#if !USER_ARGS_COME_LAST
    //@GENERICS: final instantiation-info argument for shared generic methods
    // and shared generic struct instance methods
    lvaInitGenericsCtxt(varDscInfo);

    /* If the method is varargs, process the varargs cookie */
    lvaInitVarArgsHandle(varDscInfo);
#endif

    //----------------------------------------------------------------------

    // We have set info.compArgsCount in compCompile()
    noway_assert(varDscInfo->varNum == info.compArgsCount);
    assert(varDscInfo->intRegArgNum <= MAX_REG_ARG);

    codeGen->intRegState.rsCalleeRegArgCount   = varDscInfo->intRegArgNum;
    codeGen->floatRegState.rsCalleeRegArgCount = varDscInfo->floatRegArgNum;

#if FEATURE_FASTTAILCALL
    // Save the stack usage information
    // We can get register usage information using codeGen->intRegState and
    // codeGen->floatRegState
    info.compArgStackSize = varDscInfo->stackArgSize;
#endif // FEATURE_FASTTAILCALL

    // The total argument size must be aligned.
    noway_assert((compArgSize % TARGET_POINTER_SIZE) == 0);

#ifdef TARGET_X86
    /* We can not pass more than 2^16 dwords as arguments as the "ret"
       instruction can only pop 2^16 arguments. Could be handled correctly
       but it will be very difficult for fully interruptible code */

    if (compArgSize != (size_t)(unsigned short)compArgSize)
        IMPL_LIMITATION("Too many arguments for the \"ret\" instruction to pop");
#endif
}

/*****************************************************************************/
void Compiler::lvaInitThisPtr(InitVarDscInfo* varDscInfo)
{
    LclVarDsc* varDsc = varDscInfo->varDsc;
    if (!info.compIsStatic)
    {
        varDsc->lvIsParam = 1;
        varDsc->lvIsPtr   = 1;

        lvaArg0Var = info.compThisArg = varDscInfo->varNum;
        noway_assert(info.compThisArg == 0);

        if (eeIsValueClass(info.compClassHnd))
        {
            varDsc->lvType = TYP_BYREF;
#ifdef FEATURE_SIMD
            CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
            var_types   type            = impNormStructType(info.compClassHnd, &simdBaseJitType);
            if (simdBaseJitType != CORINFO_TYPE_UNDEF)
            {
                assert(varTypeIsSIMD(type));
                varDsc->lvSIMDType = true;
                varDsc->SetSimdBaseJitType(simdBaseJitType);
                varDsc->lvExactSize = genTypeSize(type);
            }
#endif // FEATURE_SIMD
        }
        else
        {
            varDsc->lvType = TYP_REF;
            lvaSetClass(varDscInfo->varNum, info.compClassHnd);
        }

        varDsc->lvVerTypeInfo = typeInfo();

        // Mark the 'this' pointer for the method
        varDsc->lvVerTypeInfo.SetIsThisPtr();

        varDsc->lvIsRegArg = 1;
        noway_assert(varDscInfo->intRegArgNum == 0);

        varDsc->SetArgReg(genMapRegArgNumToRegNum(varDscInfo->allocRegArg(TYP_INT), varDsc->TypeGet()));
#if FEATURE_MULTIREG_ARGS
        varDsc->SetOtherArgReg(REG_NA);
#endif
        varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

#ifdef DEBUG
        if (verbose)
        {
            printf("'this'    passed in register %s\n", getRegName(varDsc->GetArgReg()));
        }
#endif
        compArgSize += TARGET_POINTER_SIZE;

        varDscInfo->varNum++;
        varDscInfo->varDsc++;
    }
}

/*****************************************************************************/
void Compiler::lvaInitRetBuffArg(InitVarDscInfo* varDscInfo, bool useFixedRetBufReg)
{
    LclVarDsc* varDsc        = varDscInfo->varDsc;
    bool       hasRetBuffArg = impMethodInfo_hasRetBuffArg(info.compMethodInfo, info.compCallConv);

    // These two should always match
    noway_assert(hasRetBuffArg == varDscInfo->hasRetBufArg);

    if (hasRetBuffArg)
    {
        info.compRetBuffArg = varDscInfo->varNum;
        varDsc->lvType      = TYP_BYREF;
        varDsc->lvIsParam   = 1;
        varDsc->lvIsRegArg  = 0;

        if (useFixedRetBufReg && hasFixedRetBuffReg())
        {
            varDsc->lvIsRegArg = 1;
            varDsc->SetArgReg(theFixedRetBuffReg());
        }
        else if (varDscInfo->canEnreg(TYP_INT))
        {
            varDsc->lvIsRegArg     = 1;
            unsigned retBuffArgNum = varDscInfo->allocRegArg(TYP_INT);
            varDsc->SetArgReg(genMapIntRegArgNumToRegNum(retBuffArgNum));
        }

#if FEATURE_MULTIREG_ARGS
        varDsc->SetOtherArgReg(REG_NA);
#endif
        varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

#ifdef FEATURE_SIMD
        if (varTypeIsSIMD(info.compRetType))
        {
            varDsc->lvSIMDType = true;

            CorInfoType simdBaseJitType =
                getBaseJitTypeAndSizeOfSIMDType(info.compMethodInfo->args.retTypeClass, &varDsc->lvExactSize);
            varDsc->SetSimdBaseJitType(simdBaseJitType);

            assert(varDsc->GetSimdBaseType() != TYP_UNKNOWN);
        }
#endif // FEATURE_SIMD

        assert(!varDsc->lvIsRegArg || isValidIntArgReg(varDsc->GetArgReg()));

#ifdef DEBUG
        if (varDsc->lvIsRegArg && verbose)
        {
            printf("'__retBuf'  passed in register %s\n", getRegName(varDsc->GetArgReg()));
        }
#endif

        /* Update the total argument size, count and varDsc */

        compArgSize += TARGET_POINTER_SIZE;
        varDscInfo->varNum++;
        varDscInfo->varDsc++;
    }
}

//-----------------------------------------------------------------------------
// lvaInitUserArgs:
//     Initialize local var descriptions for incoming user arguments
//
// Arguments:
//    varDscInfo     - the local var descriptions
//    skipArgs       - the number of user args to skip processing.
//    takeArgs       - the number of user args to process (after skipping skipArgs number of args)
//
void Compiler::lvaInitUserArgs(InitVarDscInfo* varDscInfo, unsigned skipArgs, unsigned takeArgs)
{
//-------------------------------------------------------------------------
// Walk the function signature for the explicit arguments
//-------------------------------------------------------------------------

#if defined(TARGET_X86)
    // Only (some of) the implicit args are enregistered for varargs
    if (info.compIsVarArgs)
    {
        varDscInfo->maxIntRegArgNum = varDscInfo->intRegArgNum;
    }
#elif defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
    // On System V type environment the float registers are not indexed together with the int ones.
    varDscInfo->floatRegArgNum = varDscInfo->intRegArgNum;
#endif // TARGET*

    CORINFO_ARG_LIST_HANDLE argLst = info.compMethodInfo->args.args;

    const unsigned argSigLen = info.compMethodInfo->args.numArgs;

    // We will process at most takeArgs arguments from the signature after skipping skipArgs arguments
    const int64_t numUserArgs = min(takeArgs, (argSigLen - (int64_t)skipArgs));

    // If there are no user args or less than skipArgs args, return here since there's no work to do.
    if (numUserArgs <= 0)
    {
        return;
    }

#ifdef TARGET_ARM
    regMaskTP doubleAlignMask = RBM_NONE;
#endif // TARGET_ARM

    // Skip skipArgs arguments from the signature.
    for (unsigned i = 0; i < skipArgs; i++, argLst = info.compCompHnd->getArgNext(argLst))
    {
        ;
    }

    // Process each user arg.
    for (unsigned i = 0; i < numUserArgs;
         i++, varDscInfo->varNum++, varDscInfo->varDsc++, argLst = info.compCompHnd->getArgNext(argLst))
    {
        LclVarDsc*           varDsc  = varDscInfo->varDsc;
        CORINFO_CLASS_HANDLE typeHnd = nullptr;

        CorInfoTypeWithMod corInfoType = info.compCompHnd->getArgType(&info.compMethodInfo->args, argLst, &typeHnd);
        varDsc->lvIsParam              = 1;

        lvaInitVarDsc(varDsc, varDscInfo->varNum, strip(corInfoType), typeHnd, argLst, &info.compMethodInfo->args);

        if (strip(corInfoType) == CORINFO_TYPE_CLASS)
        {
            CORINFO_CLASS_HANDLE clsHnd = info.compCompHnd->getArgClass(&info.compMethodInfo->args, argLst);
            lvaSetClass(varDscInfo->varNum, clsHnd);
        }

        // For ARM, ARM64, LOONGARCH64, and AMD64 varargs, all arguments go in integer registers
        var_types argType = mangleVarArgsType(varDsc->TypeGet());

        var_types origArgType = argType;

        // ARM softfp calling convention should affect only the floating point arguments.
        // Otherwise there appear too many surplus pre-spills and other memory operations
        // with the associated locations .
        bool     isSoftFPPreSpill = opts.compUseSoftFP && varTypeIsFloating(varDsc->TypeGet());
        unsigned argSize          = eeGetArgSize(argLst, &info.compMethodInfo->args);
        unsigned cSlots =
            (argSize + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE; // the total number of slots of this argument
        bool      isHfaArg = false;
        var_types hfaType  = TYP_UNDEF;

        // Methods that use VarArg or SoftFP cannot have HFA arguments except
        // Native varargs on arm64 unix use the regular calling convention.
        if (((TargetOS::IsUnix && TargetArchitecture::IsArm64) || !info.compIsVarArgs) && !opts.compUseSoftFP)
        {
            // If the argType is a struct, then check if it is an HFA
            if (varTypeIsStruct(argType))
            {
                // hfaType is set to float, double, or SIMD type if it is an HFA, otherwise TYP_UNDEF
                hfaType  = GetHfaType(typeHnd);
                isHfaArg = varTypeIsValidHfaType(hfaType);
            }
        }
        else if (info.compIsVarArgs)
        {
            // Currently native varargs is not implemented on non windows targets.
            //
            // Note that some targets like Arm64 Unix should not need much work as
            // the ABI is the same. While other targets may only need small changes
            // such as amd64 Unix, which just expects RAX to pass numFPArguments.
            if (TargetOS::IsUnix)
            {
                NYI("InitUserArgs for Vararg callee is not yet implemented on non Windows targets.");
            }
        }

        if (isHfaArg)
        {
            // We have an HFA argument, so from here on out treat the type as a float, double, or vector.
            // The orginal struct type is available by using origArgType.
            // We also update the cSlots to be the number of float/double/vector fields in the HFA.
            argType = hfaType; // TODO-Cleanup: remove this asignment and mark `argType` as const.
            varDsc->SetHfaType(hfaType);
            cSlots = varDsc->lvHfaSlots();
        }
        // The number of slots that must be enregistered if we are to consider this argument enregistered.
        // This is normally the same as cSlots, since we normally either enregister the entire object,
        // or none of it. For structs on ARM, however, we only need to enregister a single slot to consider
        // it enregistered, as long as we can split the rest onto the stack.
        unsigned cSlotsToEnregister = cSlots;

#if defined(TARGET_ARM64)

        if (compFeatureArgSplit())
        {
            // On arm64 Windows we will need to properly handle the case where a >8byte <=16byte
            // struct is split between register r7 and virtual stack slot s[0]
            // We will only do this for calls to vararg methods on Windows Arm64
            //
            // !!This does not affect the normal arm64 calling convention or Unix Arm64!!
            if (this->info.compIsVarArgs && argType == TYP_STRUCT)
            {
                if (varDscInfo->canEnreg(TYP_INT, 1) &&     // The beginning of the struct can go in a register
                    !varDscInfo->canEnreg(TYP_INT, cSlots)) // The end of the struct can't fit in a register
                {
                    cSlotsToEnregister = 1; // Force the split
                }
            }
        }

#endif // defined(TARGET_ARM64)

#ifdef TARGET_ARM
        // On ARM we pass the first 4 words of integer arguments and non-HFA structs in registers.
        // But we pre-spill user arguments in varargs methods and structs.
        //
        unsigned cAlign;
        bool     preSpill = info.compIsVarArgs || isSoftFPPreSpill;

        switch (origArgType)
        {
            case TYP_STRUCT:
                assert(varDsc->lvSize() == argSize);
                cAlign = varDsc->lvStructDoubleAlign ? 2 : 1;

                // HFA arguments go on the stack frame. They don't get spilled in the prolog like struct
                // arguments passed in the integer registers but get homed immediately after the prolog.
                if (!isHfaArg)
                {
                    // TODO-Arm32-Windows: vararg struct should be forced to split like
                    // ARM64 above.
                    cSlotsToEnregister = 1; // HFAs must be totally enregistered or not, but other structs can be split.
                    preSpill           = true;
                }
                break;

            case TYP_DOUBLE:
            case TYP_LONG:
                cAlign = 2;
                break;

            default:
                cAlign = 1;
                break;
        }

        if (isRegParamType(argType))
        {
            compArgSize += varDscInfo->alignReg(argType, cAlign) * REGSIZE_BYTES;
        }

        if (argType == TYP_STRUCT)
        {
            // Are we going to split the struct between registers and stack? We can do that as long as
            // no floating-point arguments have been put on the stack.
            //
            // From the ARM Procedure Call Standard:
            // Rule C.5: "If the NCRN is less than r4 **and** the NSAA is equal to the SP,"
            // then split the argument between registers and stack. Implication: if something
            // has already been spilled to the stack, then anything that would normally be
            // split between the core registers and the stack will be put on the stack.
            // Anything that follows will also be on the stack. However, if something from
            // floating point regs has been spilled to the stack, we can still use r0-r3 until they are full.

            if (varDscInfo->canEnreg(TYP_INT, 1) &&       // The beginning of the struct can go in a register
                !varDscInfo->canEnreg(TYP_INT, cSlots) && // The end of the struct can't fit in a register
                varDscInfo->existAnyFloatStackArgs())     // There's at least one stack-based FP arg already
            {
                varDscInfo->setAllRegArgUsed(TYP_INT); // Prevent all future use of integer registers
                preSpill = false;                      // This struct won't be prespilled, since it will go on the stack
            }
        }

        if (preSpill)
        {
            for (unsigned ix = 0; ix < cSlots; ix++)
            {
                if (!varDscInfo->canEnreg(TYP_INT, ix + 1))
                {
                    break;
                }
                regMaskTP regMask = genMapArgNumToRegMask(varDscInfo->regArgNum(TYP_INT) + ix, TYP_INT);
                if (cAlign == 2)
                {
                    doubleAlignMask |= regMask;
                }
                codeGen->regSet.rsMaskPreSpillRegArg |= regMask;
            }
        }
#else // !TARGET_ARM

#if defined(UNIX_AMD64_ABI)
        SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
        if (varTypeIsStruct(argType))
        {
            assert(typeHnd != nullptr);
            eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
            if (structDesc.passedInRegisters)
            {
                unsigned intRegCount   = 0;
                unsigned floatRegCount = 0;

                for (unsigned int i = 0; i < structDesc.eightByteCount; i++)
                {
                    if (structDesc.IsIntegralSlot(i))
                    {
                        intRegCount++;
                    }
                    else if (structDesc.IsSseSlot(i))
                    {
                        floatRegCount++;
                    }
                    else
                    {
                        assert(false && "Invalid eightbyte classification type.");
                        break;
                    }
                }

                if (intRegCount != 0 && !varDscInfo->canEnreg(TYP_INT, intRegCount))
                {
                    structDesc.passedInRegisters = false; // No register to enregister the eightbytes.
                }

                if (floatRegCount != 0 && !varDscInfo->canEnreg(TYP_FLOAT, floatRegCount))
                {
                    structDesc.passedInRegisters = false; // No register to enregister the eightbytes.
                }
            }
        }
#endif // UNIX_AMD64_ABI
#endif // !TARGET_ARM

        // The final home for this incoming register might be our local stack frame.
        // For System V platforms the final home will always be on the local stack frame.
        varDsc->lvOnFrame = true;

        bool canPassArgInRegisters = false;

#if defined(UNIX_AMD64_ABI)
        if (varTypeIsStruct(argType))
        {
            canPassArgInRegisters = structDesc.passedInRegisters;
        }
        else
#elif defined(TARGET_X86)
        if (varTypeIsStruct(argType) && isTrivialPointerSizedStruct(typeHnd))
        {
            canPassArgInRegisters = varDscInfo->canEnreg(TYP_I_IMPL, cSlotsToEnregister);
        }
        else
#elif defined(TARGET_LOONGARCH64)
        uint32_t  floatFlags          = STRUCT_NO_FLOAT_FIELD;
        var_types argRegTypeInStruct1 = TYP_UNKNOWN;
        var_types argRegTypeInStruct2 = TYP_UNKNOWN;

        if ((strip(corInfoType) == CORINFO_TYPE_VALUECLASS) && (argSize <= MAX_PASS_MULTIREG_BYTES))
        {
            floatFlags = info.compCompHnd->getLoongArch64PassStructInRegisterFlags(typeHnd);
        }

        if ((floatFlags & STRUCT_HAS_FLOAT_FIELDS_MASK) != 0)
        {
            assert(varTypeIsStruct(argType));
            int floatNum = 0;
            if ((floatFlags & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
            {
                assert(argSize <= 8);
                assert(varDsc->lvExactSize <= argSize);

                floatNum              = 1;
                canPassArgInRegisters = varDscInfo->canEnreg(argRegTypeInStruct1, 1);

                argRegTypeInStruct1 = (varDsc->lvExactSize == 8) ? TYP_DOUBLE : TYP_FLOAT;
            }
            else if ((floatFlags & STRUCT_FLOAT_FIELD_ONLY_TWO) != 0)
            {
                floatNum              = 2;
                canPassArgInRegisters = varDscInfo->canEnreg(TYP_DOUBLE, 2);

                argRegTypeInStruct1 = (floatFlags & STRUCT_FIRST_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
                argRegTypeInStruct2 = (floatFlags & STRUCT_SECOND_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
            }
            else if ((floatFlags & STRUCT_FLOAT_FIELD_FIRST) != 0)
            {
                floatNum              = 1;
                canPassArgInRegisters = varDscInfo->canEnreg(TYP_DOUBLE, 1);
                canPassArgInRegisters = canPassArgInRegisters && varDscInfo->canEnreg(TYP_I_IMPL, 1);

                argRegTypeInStruct1 = (floatFlags & STRUCT_FIRST_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
                argRegTypeInStruct2 = (floatFlags & STRUCT_SECOND_FIELD_SIZE_IS8) ? TYP_LONG : TYP_INT;
            }
            else if ((floatFlags & STRUCT_FLOAT_FIELD_SECOND) != 0)
            {
                floatNum              = 1;
                canPassArgInRegisters = varDscInfo->canEnreg(TYP_DOUBLE, 1);
                canPassArgInRegisters = canPassArgInRegisters && varDscInfo->canEnreg(TYP_I_IMPL, 1);

                argRegTypeInStruct1 = (floatFlags & STRUCT_FIRST_FIELD_SIZE_IS8) ? TYP_LONG : TYP_INT;
                argRegTypeInStruct2 = (floatFlags & STRUCT_SECOND_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
            }

            assert((floatNum == 1) || (floatNum == 2));

            if (!canPassArgInRegisters)
            {
                // On LoongArch64, if there aren't any remaining floating-point registers to pass the argument,
                // integer registers (if any) are used instead.
                varDscInfo->setAllRegArgUsed(TYP_DOUBLE);
                canPassArgInRegisters = varDscInfo->canEnreg(argType, cSlotsToEnregister);

                argRegTypeInStruct1 = TYP_UNKNOWN;
                argRegTypeInStruct2 = TYP_UNKNOWN;

                if (cSlotsToEnregister == 2)
                {
                    if (!canPassArgInRegisters && varDscInfo->canEnreg(TYP_I_IMPL, 1))
                    {
                        // Here a struct-arg which needs two registers but only one integer register available,
                        // it has to be split.
                        argRegTypeInStruct1   = TYP_I_IMPL;
                        canPassArgInRegisters = true;
                    }
                }
            }
        }
        else
#endif // defined(TARGET_LOONGARCH64)
        {
            canPassArgInRegisters = varDscInfo->canEnreg(argType, cSlotsToEnregister);
#if defined(TARGET_LOONGARCH64)
            // On LoongArch64, if there aren't any remaining floating-point registers to pass the argument,
            // integer registers (if any) are used instead.
            if (!canPassArgInRegisters && varTypeIsFloating(argType))
            {
                canPassArgInRegisters = varDscInfo->canEnreg(TYP_I_IMPL, cSlotsToEnregister);
                argType               = canPassArgInRegisters ? TYP_I_IMPL : argType;
            }
            if (!canPassArgInRegisters && (cSlots > 1))
            {
                // If a struct-arg which needs two registers but only one integer register available,
                // it has to be split.
                canPassArgInRegisters = varDscInfo->canEnreg(TYP_I_IMPL, 1);
                argRegTypeInStruct1   = canPassArgInRegisters ? TYP_I_IMPL : TYP_UNKNOWN;
            }
#endif
        }

        if (canPassArgInRegisters)
        {
            /* Another register argument */

            // Allocate the registers we need. allocRegArg() returns the first argument register number of the set.
            // For non-HFA structs, we still "try" to enregister the whole thing; it will just max out if splitting
            // to the stack happens.
            unsigned firstAllocatedRegArgNum = 0;

#if FEATURE_MULTIREG_ARGS
            varDsc->SetOtherArgReg(REG_NA);
#endif // FEATURE_MULTIREG_ARGS

#if defined(UNIX_AMD64_ABI)
            unsigned  secondAllocatedRegArgNum = 0;
            var_types firstEightByteType       = TYP_UNDEF;
            var_types secondEightByteType      = TYP_UNDEF;

            if (varTypeIsStruct(argType))
            {
                if (structDesc.eightByteCount >= 1)
                {
                    firstEightByteType      = GetEightByteType(structDesc, 0);
                    firstAllocatedRegArgNum = varDscInfo->allocRegArg(firstEightByteType, 1);
                }
            }
            else
#elif defined(TARGET_LOONGARCH64)
            if (argRegTypeInStruct1 != TYP_UNKNOWN)
            {
                firstAllocatedRegArgNum = varDscInfo->allocRegArg(argRegTypeInStruct1, 1);
            }
            else
#endif // defined(TARGET_LOONGARCH64)
            {
                firstAllocatedRegArgNum = varDscInfo->allocRegArg(argType, cSlots);
            }

            if (isHfaArg)
            {
                // We need to save the fact that this HFA is enregistered
                // Note that we can have HVAs of SIMD types even if we are not recognizing intrinsics.
                // In that case, we won't have normalized the vector types on the varDsc, so if we have a single vector
                // register, we need to set the type now. Otherwise, later we'll assume this is passed by reference.
                if (varDsc->lvHfaSlots() != 1)
                {
                    varDsc->lvIsMultiRegArg = true;
                }
            }

            varDsc->lvIsRegArg = 1;

#if FEATURE_MULTIREG_ARGS
#ifdef TARGET_ARM64
            if (argType == TYP_STRUCT)
            {
                varDsc->SetArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, TYP_I_IMPL));
                if (cSlots == 2)
                {
                    varDsc->SetOtherArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum + 1, TYP_I_IMPL));
                    varDsc->lvIsMultiRegArg = true;
                }
            }
#elif defined(UNIX_AMD64_ABI)
            if (varTypeIsStruct(argType))
            {
                varDsc->SetArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, firstEightByteType));

                // If there is a second eightbyte, get a register for it too and map the arg to the reg number.
                if (structDesc.eightByteCount >= 2)
                {
                    secondEightByteType      = GetEightByteType(structDesc, 1);
                    secondAllocatedRegArgNum = varDscInfo->allocRegArg(secondEightByteType, 1);
                    varDsc->lvIsMultiRegArg  = true;
                }

                if (secondEightByteType != TYP_UNDEF)
                {
                    varDsc->SetOtherArgReg(genMapRegArgNumToRegNum(secondAllocatedRegArgNum, secondEightByteType));
                }
            }
#elif defined(TARGET_LOONGARCH64)
            if (argType == TYP_STRUCT)
            {
                if (argRegTypeInStruct1 != TYP_UNKNOWN)
                {
                    varDsc->SetArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, argRegTypeInStruct1));
                    varDsc->lvIs4Field1 = (genTypeSize(argRegTypeInStruct1) == 4) ? 1 : 0;
                    if (argRegTypeInStruct2 != TYP_UNKNOWN)
                    {
                        unsigned secondAllocatedRegArgNum = varDscInfo->allocRegArg(argRegTypeInStruct2, 1);
                        varDsc->SetOtherArgReg(genMapRegArgNumToRegNum(secondAllocatedRegArgNum, argRegTypeInStruct2));
                        varDsc->lvIs4Field2 = (genTypeSize(argRegTypeInStruct2) == 4) ? 1 : 0;
                    }
                    else if (cSlots > 1)
                    {
                        // Here a struct-arg which needs two registers but only one integer register available,
                        // it has to be split. But we reserved extra 8-bytes for the whole struct.
                        varDsc->lvIsSplit = 1;
                        varDsc->SetOtherArgReg(REG_STK);
                        varDscInfo->setAllRegArgUsed(argRegTypeInStruct1);
#if FEATURE_FASTTAILCALL
                        varDscInfo->stackArgSize += TARGET_POINTER_SIZE;
#endif
                    }
                }
                else
                {
                    varDsc->SetArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, TYP_I_IMPL));
                    if (cSlots == 2)
                    {
                        varDsc->SetOtherArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum + 1, TYP_I_IMPL));
                    }
                }
            }
#else  // ARM32
            if (varTypeIsStruct(argType))
            {
                varDsc->SetArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, TYP_I_IMPL));
            }
#endif // ARM32
            else
#endif // FEATURE_MULTIREG_ARGS
            {
                varDsc->SetArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, argType));
            }

#ifdef TARGET_ARM
            if (varDsc->TypeGet() == TYP_LONG)
            {
                varDsc->SetOtherArgReg(genMapRegArgNumToRegNum(firstAllocatedRegArgNum + 1, TYP_INT));
            }

#if FEATURE_FASTTAILCALL
            // Check if arg was split between registers and stack.
            if (!varTypeUsesFloatReg(argType))
            {
                unsigned firstRegArgNum = genMapIntRegNumToRegArgNum(varDsc->GetArgReg());
                unsigned lastRegArgNum  = firstRegArgNum + cSlots - 1;
                if (lastRegArgNum >= varDscInfo->maxIntRegArgNum)
                {
                    assert(varDscInfo->stackArgSize == 0);
                    unsigned numEnregistered = varDscInfo->maxIntRegArgNum - firstRegArgNum;
                    varDsc->SetStackOffset(-(int)numEnregistered * REGSIZE_BYTES);
                    varDscInfo->stackArgSize += (cSlots - numEnregistered) * REGSIZE_BYTES;
                    varDscInfo->hasSplitParam = true;
                    JITDUMP("set user arg V%02u offset to %d\n", varDscInfo->varNum, varDsc->GetStackOffset());
                }
            }
#endif
#endif // TARGET_ARM

#ifdef DEBUG
            if (verbose)
            {
                printf("Arg #%u    passed in register(s) ", varDscInfo->varNum);

#if defined(UNIX_AMD64_ABI)
                if (varTypeIsStruct(argType))
                {
                    // Print both registers, just to be clear
                    if (firstEightByteType == TYP_UNDEF)
                    {
                        printf("firstEightByte: <not used>");
                    }
                    else
                    {
                        printf("firstEightByte: %s",
                               getRegName(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, firstEightByteType)));
                    }

                    if (secondEightByteType == TYP_UNDEF)
                    {
                        printf(", secondEightByte: <not used>");
                    }
                    else
                    {
                        printf(", secondEightByte: %s",
                               getRegName(genMapRegArgNumToRegNum(secondAllocatedRegArgNum, secondEightByteType)));
                    }
                }
                else
#endif // defined(UNIX_AMD64_ABI)
                {
                    bool     isFloat   = varTypeUsesFloatReg(argType);
                    unsigned regArgNum = genMapRegNumToRegArgNum(varDsc->GetArgReg(), argType);

                    for (unsigned ix = 0; ix < cSlots; ix++, regArgNum++)
                    {
                        if (ix > 0)
                        {
                            printf(",");
                        }

                        if (!isFloat && (regArgNum >= varDscInfo->maxIntRegArgNum)) // a struct has been split between
                                                                                    // registers and stack
                        {
                            printf(" stack slots:%d", cSlots - ix);
                            break;
                        }

#ifdef TARGET_ARM
                        if (isFloat)
                        {
                            // Print register size prefix
                            if (argType == TYP_DOUBLE)
                            {
                                // Print both registers, just to be clear
                                printf("%s/%s", getRegName(genMapRegArgNumToRegNum(regArgNum, argType)),
                                       getRegName(genMapRegArgNumToRegNum(regArgNum + 1, argType)));

                                // doubles take 2 slots
                                assert(ix + 1 < cSlots);
                                ++ix;
                                ++regArgNum;
                            }
                            else
                            {
                                printf("%s", getRegName(genMapRegArgNumToRegNum(regArgNum, argType)));
                            }
                        }
                        else
#endif // TARGET_ARM
                        {
                            printf("%s", getRegName(genMapRegArgNumToRegNum(regArgNum, argType)));
                        }
                    }
                }
                printf("\n");
            }
#endif    // DEBUG
        } // end if (canPassArgInRegisters)
        else
        {
#if defined(TARGET_ARM)
            varDscInfo->setAllRegArgUsed(argType);
            if (varTypeUsesFloatReg(argType))
            {
                varDscInfo->setAnyFloatStackArgs();
            }

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

            // If we needed to use the stack in order to pass this argument then
            // record the fact that we have used up any remaining registers of this 'type'
            // This prevents any 'backfilling' from occuring on ARM64/LoongArch64.
            //
            varDscInfo->setAllRegArgUsed(argType);

#endif // TARGET_XXX

#if FEATURE_FASTTAILCALL
#ifdef TARGET_ARM
            unsigned argAlignment = cAlign * TARGET_POINTER_SIZE;
#else
            unsigned argAlignment = eeGetArgSizeAlignment(origArgType, (hfaType == TYP_FLOAT));
            // We expect the following rounding operation to be a noop on all
            // ABIs except ARM (where we have 8-byte aligned args) and macOS
            // ARM64 (that allows to pack multiple smaller parameters in a
            // single stack slot).
            assert(compMacOsArm64Abi() || ((varDscInfo->stackArgSize % argAlignment) == 0));
#endif
            varDscInfo->stackArgSize = roundUp(varDscInfo->stackArgSize, argAlignment);

            JITDUMP("set user arg V%02u offset to %u\n", varDscInfo->varNum, varDscInfo->stackArgSize);
            varDsc->SetStackOffset(varDscInfo->stackArgSize);
            varDscInfo->stackArgSize += argSize;
#endif // FEATURE_FASTTAILCALL
        }

#ifdef UNIX_AMD64_ABI
        // The arg size is returning the number of bytes of the argument. For a struct it could return a size not a
        // multiple of TARGET_POINTER_SIZE. The stack allocated space should always be multiple of TARGET_POINTER_SIZE,
        // so round it up.
        compArgSize += roundUp(argSize, TARGET_POINTER_SIZE);
#else  // !UNIX_AMD64_ABI
        compArgSize += argSize;
#endif // !UNIX_AMD64_ABI
        if (info.compIsVarArgs || isSoftFPPreSpill)
        {
#if defined(TARGET_X86)
            varDsc->SetStackOffset(compArgSize);
#else  // !TARGET_X86
            // TODO-CQ: We shouldn't have to go as far as to declare these
            // address-exposed -- DoNotEnregister should suffice.

            lvaSetVarAddrExposed(varDscInfo->varNum DEBUGARG(AddressExposedReason::TOO_CONSERVATIVE));
#endif // !TARGET_X86
        }
    }

    compArgSize = GetOutgoingArgByteSize(compArgSize);

#ifdef TARGET_ARM
    if (doubleAlignMask != RBM_NONE)
    {
        assert(RBM_ARG_REGS == 0xF);
        assert((doubleAlignMask & RBM_ARG_REGS) == doubleAlignMask);
        if (doubleAlignMask != RBM_NONE && doubleAlignMask != RBM_ARG_REGS)
        {
            // 'double aligned types' can begin only at r0 or r2 and we always expect at least two registers to be used
            // Note that in rare cases, we can have double-aligned structs of 12 bytes (if specified explicitly with
            // attributes)
            assert((doubleAlignMask == 0b0011) || (doubleAlignMask == 0b1100) ||
                   (doubleAlignMask == 0b0111) /* || 0b1111 is if'ed out */);

            // Now if doubleAlignMask is xyz1 i.e., the struct starts in r0, and we prespill r2 or r3
            // but not both, then the stack would be misaligned for r0. So spill both
            // r2 and r3.
            //
            // ; +0 --- caller SP double aligned ----
            // ; -4 r2    r3
            // ; -8 r1    r1
            // ; -c r0    r0   <-- misaligned.
            // ; callee saved regs
            bool startsAtR0 = (doubleAlignMask & 1) == 1;
            bool r2XorR3    = ((codeGen->regSet.rsMaskPreSpillRegArg & RBM_R2) == 0) !=
                           ((codeGen->regSet.rsMaskPreSpillRegArg & RBM_R3) == 0);
            if (startsAtR0 && r2XorR3)
            {
                codeGen->regSet.rsMaskPreSpillAlign =
                    (~codeGen->regSet.rsMaskPreSpillRegArg & ~doubleAlignMask) & RBM_ARG_REGS;
            }
        }
    }
#endif // TARGET_ARM
}

/*****************************************************************************/
void Compiler::lvaInitGenericsCtxt(InitVarDscInfo* varDscInfo)
{
    //@GENERICS: final instantiation-info argument for shared generic methods
    // and shared generic struct instance methods
    if (info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        info.compTypeCtxtArg = varDscInfo->varNum;

        LclVarDsc* varDsc = varDscInfo->varDsc;
        varDsc->lvIsParam = 1;
        varDsc->lvType    = TYP_I_IMPL;

        if (varDscInfo->canEnreg(TYP_I_IMPL))
        {
            /* Another register argument */

            varDsc->lvIsRegArg = 1;
            varDsc->SetArgReg(genMapRegArgNumToRegNum(varDscInfo->regArgNum(TYP_INT), varDsc->TypeGet()));
#if FEATURE_MULTIREG_ARGS
            varDsc->SetOtherArgReg(REG_NA);
#endif
            varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

            varDscInfo->intRegArgNum++;

#ifdef DEBUG
            if (verbose)
            {
                printf("'GenCtxt'   passed in register %s\n", getRegName(varDsc->GetArgReg()));
            }
#endif
        }
        else
        {
            // We need to mark these as being on the stack, as this is not done elsewhere in the case that canEnreg
            // returns false.
            varDsc->lvOnFrame = true;
#if FEATURE_FASTTAILCALL
            varDsc->SetStackOffset(varDscInfo->stackArgSize);
            varDscInfo->stackArgSize += TARGET_POINTER_SIZE;
#endif // FEATURE_FASTTAILCALL
        }

        compArgSize += TARGET_POINTER_SIZE;

#if defined(TARGET_X86)
        if (info.compIsVarArgs)
            varDsc->SetStackOffset(compArgSize);
#endif // TARGET_X86

        varDscInfo->varNum++;
        varDscInfo->varDsc++;
    }
}

/*****************************************************************************/
void Compiler::lvaInitVarArgsHandle(InitVarDscInfo* varDscInfo)
{
    if (info.compIsVarArgs)
    {
        lvaVarargsHandleArg = varDscInfo->varNum;

        LclVarDsc* varDsc = varDscInfo->varDsc;
        varDsc->lvType    = TYP_I_IMPL;
        varDsc->lvIsParam = 1;
#if defined(TARGET_X86)
        // Codegen will need it for x86 scope info.
        varDsc->lvImplicitlyReferenced = 1;
#endif // TARGET_X86

        lvaSetVarDoNotEnregister(lvaVarargsHandleArg DEBUGARG(DoNotEnregisterReason::VMNeedsStackAddr));

        assert(mostRecentlyActivePhase == PHASE_PRE_IMPORT);

        // TODO-Cleanup: this is preImportation phase, why do we try to work with regs here?
        // Should it be just deleted?
        if (varDscInfo->canEnreg(TYP_I_IMPL))
        {
            /* Another register argument */

            unsigned varArgHndArgNum = varDscInfo->allocRegArg(TYP_I_IMPL);

            varDsc->lvIsRegArg = 1;
            varDsc->SetArgReg(genMapRegArgNumToRegNum(varArgHndArgNum, TYP_I_IMPL));
#if FEATURE_MULTIREG_ARGS
            varDsc->SetOtherArgReg(REG_NA);
#endif
            varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame
#ifdef TARGET_ARM
            // This has to be spilled right in front of the real arguments and we have
            // to pre-spill all the argument registers explicitly because we only have
            // have symbols for the declared ones, not any potential variadic ones.
            for (unsigned ix = varArgHndArgNum; ix < ArrLen(intArgMasks); ix++)
            {
                codeGen->regSet.rsMaskPreSpillRegArg |= intArgMasks[ix];
            }
#endif // TARGET_ARM

#ifdef DEBUG
            if (verbose)
            {
                printf("'VarArgHnd' passed in register %s\n", getRegName(varDsc->GetArgReg()));
            }
#endif // DEBUG
        }
        else
        {
            // We need to mark these as being on the stack, as this is not done elsewhere in the case that canEnreg
            // returns false.
            varDsc->lvOnFrame = true;
#if FEATURE_FASTTAILCALL
            varDsc->SetStackOffset(varDscInfo->stackArgSize);
            varDscInfo->stackArgSize += TARGET_POINTER_SIZE;
#endif // FEATURE_FASTTAILCALL
        }

        /* Update the total argument size, count and varDsc */

        compArgSize += TARGET_POINTER_SIZE;

        varDscInfo->varNum++;
        varDscInfo->varDsc++;

#if defined(TARGET_X86)
        varDsc->SetStackOffset(compArgSize);

        // Allocate a temp to point at the beginning of the args

        lvaVarargsBaseOfStkArgs                  = lvaGrabTemp(false DEBUGARG("Varargs BaseOfStkArgs"));
        lvaTable[lvaVarargsBaseOfStkArgs].lvType = TYP_I_IMPL;

#endif // TARGET_X86
    }
}

/*****************************************************************************/
void Compiler::lvaInitVarDsc(LclVarDsc*              varDsc,
                             unsigned                varNum,
                             CorInfoType             corInfoType,
                             CORINFO_CLASS_HANDLE    typeHnd,
                             CORINFO_ARG_LIST_HANDLE varList,
                             CORINFO_SIG_INFO*       varSig)
{
    noway_assert(varDsc == lvaGetDesc(varNum));

    switch (corInfoType)
    {
        // Mark types that looks like a pointer for doing shadow-copying of
        // parameters if we have an unsafe buffer.
        // Note that this does not handle structs with pointer fields. Instead,
        // we rely on using the assign-groups/equivalence-groups in
        // gsFindVulnerableParams() to determine if a buffer-struct contains a
        // pointer. We could do better by having the EE determine this for us.
        // Note that we want to keep buffers without pointers at lower memory
        // addresses than buffers with pointers.
        case CORINFO_TYPE_PTR:
        case CORINFO_TYPE_BYREF:
        case CORINFO_TYPE_CLASS:
        case CORINFO_TYPE_STRING:
        case CORINFO_TYPE_VAR:
        case CORINFO_TYPE_REFANY:
            varDsc->lvIsPtr = 1;
            break;
        default:
            break;
    }

    var_types type = JITtype2varType(corInfoType);
    if (varTypeIsFloating(type))
    {
        compFloatingPointUsed = true;
    }

    if (typeHnd)
    {
        unsigned cFlags = info.compCompHnd->getClassAttribs(typeHnd);

        // We can get typeHnds for primitive types, these are value types which only contain
        // a primitive. We will need the typeHnd to distinguish them, so we store it here.
        if ((cFlags & CORINFO_FLG_VALUECLASS) && !varTypeIsStruct(type))
        {
            // printf("This is a struct that the JIT will treat as a primitive\n");
            varDsc->lvVerTypeInfo = verMakeTypeInfo(typeHnd);
        }

        varDsc->lvOverlappingFields = StructHasOverlappingFields(cFlags);
    }

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    varDsc->lvIsImplicitByRef = 0;
#elif defined(TARGET_LOONGARCH64)
    varDsc->lvIsImplicitByRef = 0;
    varDsc->lvIs4Field1       = 0;
    varDsc->lvIs4Field2       = 0;
    varDsc->lvIsSplit         = 0;
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

    // Set the lvType (before this point it is TYP_UNDEF).

    if (GlobalJitOptions::compFeatureHfa)
    {
        varDsc->SetHfaType(TYP_UNDEF);
    }
    if ((varTypeIsStruct(type)))
    {
        lvaSetStruct(varNum, typeHnd, typeHnd != nullptr, true);
        if (info.compIsVarArgs)
        {
            lvaSetStructUsedAsVarArg(varNum);
        }
    }
    else
    {
        varDsc->lvType = type;
    }

    if (type == TYP_BOOL)
    {
        varDsc->lvIsBoolean = true;
    }

#ifdef DEBUG
    varDsc->SetStackOffset(BAD_STK_OFFS);
#endif

#if FEATURE_MULTIREG_ARGS
    varDsc->SetOtherArgReg(REG_NA);
#endif // FEATURE_MULTIREG_ARGS
}

/*****************************************************************************
 * Returns our internal varNum for a given IL variable.
 * Asserts assume it is called after lvaTable[] has been set up.
 */

unsigned Compiler::compMapILvarNum(unsigned ILvarNum)
{
    noway_assert(ILvarNum < info.compILlocalsCount || ILvarNum > unsigned(ICorDebugInfo::UNKNOWN_ILNUM));

    unsigned varNum;

    if (ILvarNum == (unsigned)ICorDebugInfo::VARARGS_HND_ILNUM)
    {
        // The varargs cookie is the last argument in lvaTable[]
        noway_assert(info.compIsVarArgs);

        varNum = lvaVarargsHandleArg;
        noway_assert(lvaTable[varNum].lvIsParam);
    }
    else if (ILvarNum == (unsigned)ICorDebugInfo::RETBUF_ILNUM)
    {
        noway_assert(info.compRetBuffArg != BAD_VAR_NUM);
        varNum = info.compRetBuffArg;
    }
    else if (ILvarNum == (unsigned)ICorDebugInfo::TYPECTXT_ILNUM)
    {
        noway_assert(info.compTypeCtxtArg >= 0);
        varNum = unsigned(info.compTypeCtxtArg);
    }
    else if (ILvarNum < info.compILargsCount)
    {
        // Parameter
        varNum = compMapILargNum(ILvarNum);
        noway_assert(lvaTable[varNum].lvIsParam);
    }
    else if (ILvarNum < info.compILlocalsCount)
    {
        // Local variable
        unsigned lclNum = ILvarNum - info.compILargsCount;
        varNum          = info.compArgsCount + lclNum;
        noway_assert(!lvaTable[varNum].lvIsParam);
    }
    else
    {
        unreached();
    }

    noway_assert(varNum < info.compLocalsCount);
    return varNum;
}

/*****************************************************************************
 * Returns the IL variable number given our internal varNum.
 * Special return values are VARG_ILNUM, RETBUF_ILNUM, TYPECTXT_ILNUM.
 *
 * Returns UNKNOWN_ILNUM if it can't be mapped.
 */

unsigned Compiler::compMap2ILvarNum(unsigned varNum) const
{
    if (compIsForInlining())
    {
        return impInlineInfo->InlinerCompiler->compMap2ILvarNum(varNum);
    }

    noway_assert(varNum < lvaCount);

    if (varNum == info.compRetBuffArg)
    {
        return (unsigned)ICorDebugInfo::RETBUF_ILNUM;
    }

    // Is this a varargs function?
    if (info.compIsVarArgs && varNum == lvaVarargsHandleArg)
    {
        return (unsigned)ICorDebugInfo::VARARGS_HND_ILNUM;
    }

    // We create an extra argument for the type context parameter
    // needed for shared generic code.
    if ((info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE) && varNum == (unsigned)info.compTypeCtxtArg)
    {
        return (unsigned)ICorDebugInfo::TYPECTXT_ILNUM;
    }

#if FEATURE_FIXED_OUT_ARGS
    if (varNum == lvaOutgoingArgSpaceVar)
    {
        return (unsigned)ICorDebugInfo::UNKNOWN_ILNUM; // Cannot be mapped
    }
#endif // FEATURE_FIXED_OUT_ARGS

    // Now mutate varNum to remove extra parameters from the count.
    if ((info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE) && varNum > (unsigned)info.compTypeCtxtArg)
    {
        varNum--;
    }

    if (info.compIsVarArgs && varNum > lvaVarargsHandleArg)
    {
        varNum--;
    }

    /* Is there a hidden argument for the return buffer.
       Note that this code works because if the RetBuffArg is not present,
       compRetBuffArg will be BAD_VAR_NUM */
    if (info.compRetBuffArg != BAD_VAR_NUM && varNum > info.compRetBuffArg)
    {
        varNum--;
    }

    if (varNum >= info.compLocalsCount)
    {
        return (unsigned)ICorDebugInfo::UNKNOWN_ILNUM; // Cannot be mapped
    }

    return varNum;
}

/*****************************************************************************
 * Returns true if variable "varNum" may be address-exposed.
 */

bool Compiler::lvaVarAddrExposed(unsigned varNum) const
{
    const LclVarDsc* varDsc = lvaGetDesc(varNum);
    return varDsc->IsAddressExposed();
}

/*****************************************************************************
 * Returns true iff variable "varNum" should not be enregistered (or one of several reasons).
 */

bool Compiler::lvaVarDoNotEnregister(unsigned varNum)
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);
    return varDsc->lvDoNotEnregister;
}

//------------------------------------------------------------------------
// lvInitializeDoNotEnregFlag: a helper to initialize `lvDoNotEnregister` flag
//    for locals that were created before the compiler decided its optimization level.
//
// Assumptions:
//    compEnregLocals() value is finalized and is set to false.
//
void Compiler::lvSetMinOptsDoNotEnreg()
{
    JITDUMP("compEnregLocals() is false, setting doNotEnreg flag for all locals.");
    assert(!compEnregLocals());
    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::NoRegVars));
    }
}

/*****************************************************************************
 * Returns the handle to the class of the local variable varNum
 */

CORINFO_CLASS_HANDLE Compiler::lvaGetStruct(unsigned varNum)
{
    const LclVarDsc* varDsc = lvaGetDesc(varNum);
    return varDsc->GetStructHnd();
}

//--------------------------------------------------------------------------------------------
// lvaFieldOffsetCmp - a static compare function passed to jitstd::sort() by Compiler::StructPromotionHelper;
//   compares fields' offsets.
//
// Arguments:
//   field1 - pointer to the first field;
//   field2 - pointer to the second field.
//
// Return value:
//   0 if the fields' offsets are equal, 1 if the first field has bigger offset, -1 otherwise.
//
bool Compiler::lvaFieldOffsetCmp::operator()(const lvaStructFieldInfo& field1, const lvaStructFieldInfo& field2)
{
    return field1.fldOffset < field2.fldOffset;
}

//------------------------------------------------------------------------
// StructPromotionHelper constructor.
//
// Arguments:
//   compiler - pointer to a compiler to get access to an allocator, compHandle etc.
//
Compiler::StructPromotionHelper::StructPromotionHelper(Compiler* compiler)
    : compiler(compiler)
    , structPromotionInfo()
#ifdef DEBUG
    , retypedFieldsMap(compiler->getAllocator(CMK_DebugOnly))
#endif // DEBUG
{
}

//--------------------------------------------------------------------------------------------
// TryPromoteStructVar - promote struct var if it is possible and profitable.
//
// Arguments:
//   lclNum - struct number to try.
//
// Return value:
//   true if the struct var was promoted.
//
bool Compiler::StructPromotionHelper::TryPromoteStructVar(unsigned lclNum)
{
    if (CanPromoteStructVar(lclNum))
    {
#if 0
            // Often-useful debugging code: if you've narrowed down a struct-promotion problem to a single
            // method, this allows you to select a subset of the vars to promote (by 1-based ordinal number).
            static int structPromoVarNum = 0;
            structPromoVarNum++;
            if (atoi(getenv("structpromovarnumlo")) <= structPromoVarNum && structPromoVarNum <= atoi(getenv("structpromovarnumhi")))
#endif // 0
        if (ShouldPromoteStructVar(lclNum))
        {
            PromoteStructVar(lclNum);
            return true;
        }
    }
    return false;
}

#ifdef DEBUG
//--------------------------------------------------------------------------------------------
// CheckRetypedAsScalar - check that the fldType for this fieldHnd was retyped as requested type.
//
// Arguments:
//   fieldHnd      - the field handle;
//   requestedType - as which type the field was accessed;
//
// Notes:
//   For example it can happen when such struct A { struct B { long c } } is compiled and we access A.B.c,
//   it could look like "GT_FIELD struct B.c -> ADDR -> GT_FIELD struct A.B -> ADDR -> LCL_VAR A" , but
//   "GT_FIELD struct A.B -> ADDR -> LCL_VAR A" can be promoted to "LCL_VAR long A.B" and then
//   there is type mistmatch between "GT_FIELD struct B.c" and  "LCL_VAR long A.B".
//
void Compiler::StructPromotionHelper::CheckRetypedAsScalar(CORINFO_FIELD_HANDLE fieldHnd, var_types requestedType)
{
    assert(retypedFieldsMap.Lookup(fieldHnd));
    assert(retypedFieldsMap[fieldHnd] == requestedType);
}
#endif // DEBUG

//--------------------------------------------------------------------------------------------
// CanPromoteStructType - checks if the struct type can be promoted.
//
// Arguments:
//   typeHnd - struct handle to check.
//
// Return value:
//   true if the struct type can be promoted.
//
// Notes:
//   The last analyzed type is memorized to skip the check if we ask about the same time again next.
//   However, it was not found profitable to memorize all analyzed types in a map.
//
//   The check initializes only nessasary fields in lvaStructPromotionInfo,
//   so if the promotion is rejected early than most fields will be uninitialized.
//
bool Compiler::StructPromotionHelper::CanPromoteStructType(CORINFO_CLASS_HANDLE typeHnd)
{
    assert(typeHnd != nullptr);
    if (!compiler->eeIsValueClass(typeHnd))
    {
        // TODO-ObjectStackAllocation: Enable promotion of fields of stack-allocated objects.
        return false;
    }

    if (structPromotionInfo.typeHnd == typeHnd)
    {
        // Asking for the same type of struct as the last time.
        // Nothing need to be done.
        // Fall through ...
        return structPromotionInfo.canPromote;
    }

    // Analyze this type from scratch.
    structPromotionInfo = lvaStructPromotionInfo(typeHnd);

    // sizeof(double) represents the size of the largest primitive type that we can struct promote.
    // In the future this may be changing to XMM_REGSIZE_BYTES.
    // Note: MaxOffset is used below to declare a local array, and therefore must be a compile-time constant.
    CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(FEATURE_SIMD)
#if defined(TARGET_XARCH)
    // This will allow promotion of 4 Vector<T> fields on AVX2 or Vector256<T> on AVX,
    // or 8 Vector<T>/Vector128<T> fields on SSE2.
    const int MaxOffset = MAX_NumOfFieldsInPromotableStruct * YMM_REGSIZE_BYTES;
#elif defined(TARGET_ARM64)
    const int MaxOffset      = MAX_NumOfFieldsInPromotableStruct * FP_REGSIZE_BYTES;
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64)
#else  // !FEATURE_SIMD
    const int MaxOffset       = MAX_NumOfFieldsInPromotableStruct * sizeof(double);
#endif // !FEATURE_SIMD

    assert((BYTE)MaxOffset == MaxOffset); // because lvaStructFieldInfo.fldOffset is byte-sized
    assert((BYTE)MAX_NumOfFieldsInPromotableStruct ==
           MAX_NumOfFieldsInPromotableStruct); // because lvaStructFieldInfo.fieldCnt is byte-sized

    bool containsGCpointers = false;

    COMP_HANDLE compHandle = compiler->info.compCompHnd;

    unsigned structSize = compHandle->getClassSize(typeHnd);
    if (structSize > MaxOffset)
    {
        return false; // struct is too large
    }

    unsigned fieldCnt = compHandle->getClassNumInstanceFields(typeHnd);
    if (fieldCnt == 0 || fieldCnt > MAX_NumOfFieldsInPromotableStruct)
    {
        return false; // struct must have between 1 and MAX_NumOfFieldsInPromotableStruct fields
    }

    structPromotionInfo.fieldCnt = (unsigned char)fieldCnt;
    DWORD typeFlags              = compHandle->getClassAttribs(typeHnd);

    bool overlappingFields = StructHasOverlappingFields(typeFlags);
    if (overlappingFields)
    {
        return false;
    }

    // Don't struct promote if we have an CUSTOMLAYOUT flag on an HFA type
    if (StructHasCustomLayout(typeFlags) && compiler->IsHfa(typeHnd))
    {
        return false;
    }

#ifdef TARGET_ARM
    // On ARM, we have a requirement on the struct alignment; see below.
    unsigned structAlignment = roundUp(compHandle->getClassAlignmentRequirement(typeHnd), TARGET_POINTER_SIZE);
#endif // TARGET_ARM

    // If we have "Custom Layout" then we might have an explicit Size attribute
    // Managed C++ uses this for its structs, such C++ types will not contain GC pointers.
    //
    // The current VM implementation also incorrectly sets the CORINFO_FLG_CUSTOMLAYOUT
    // whenever a managed value class contains any GC pointers.
    // (See the comment for VMFLAG_NOT_TIGHTLY_PACKED in class.h)
    //
    // It is important to struct promote managed value classes that have GC pointers
    // So we compute the correct value for "CustomLayout" here
    //
    if (StructHasCustomLayout(typeFlags) && ((typeFlags & CORINFO_FLG_CONTAINS_GC_PTR) == 0))
    {
        structPromotionInfo.customLayout = true;
    }

    if (StructHasDontDigFieldsFlagSet(typeFlags))
    {
        return CanConstructAndPromoteField(&structPromotionInfo);
    }

    unsigned fieldsSize = 0;

    for (BYTE ordinal = 0; ordinal < fieldCnt; ++ordinal)
    {
        CORINFO_FIELD_HANDLE fieldHnd       = compHandle->getFieldInClass(typeHnd, ordinal);
        structPromotionInfo.fields[ordinal] = GetFieldInfo(fieldHnd, ordinal);
        const lvaStructFieldInfo& fieldInfo = structPromotionInfo.fields[ordinal];

        noway_assert(fieldInfo.fldOffset < structSize);

        if (fieldInfo.fldSize == 0)
        {
            // Not a scalar type.
            return false;
        }

        if ((fieldInfo.fldOffset % fieldInfo.fldSize) != 0)
        {
            // The code in Compiler::genPushArgList that reconstitutes
            // struct values on the stack from promoted fields expects
            // those fields to be at their natural alignment.
            return false;
        }

        if (varTypeIsGC(fieldInfo.fldType))
        {
            containsGCpointers = true;
        }

        // The end offset for this field should never be larger than our structSize.
        noway_assert(fieldInfo.fldOffset + fieldInfo.fldSize <= structSize);

        fieldsSize += fieldInfo.fldSize;

#ifdef TARGET_ARM
        // On ARM, for struct types that don't use explicit layout, the alignment of the struct is
        // at least the max alignment of its fields.  We take advantage of this invariant in struct promotion,
        // so verify it here.
        if (fieldInfo.fldSize > structAlignment)
        {
            // Don't promote vars whose struct types violates the invariant.  (Alignment == size for primitives.)
            return false;
        }
#endif // TARGET_ARM
    }

    // If we saw any GC pointer or by-ref fields above then CORINFO_FLG_CONTAINS_GC_PTR or
    // CORINFO_FLG_BYREF_LIKE has to be set!
    noway_assert((containsGCpointers == false) ||
                 ((typeFlags & (CORINFO_FLG_CONTAINS_GC_PTR | CORINFO_FLG_BYREF_LIKE)) != 0));

    // Check if this promoted struct contains any holes.
    assert(!overlappingFields);
    if (fieldsSize != structSize)
    {
        // If sizes do not match it means we have an overlapping fields or holes.
        // Overlapping fields were rejected early, so here it can mean only holes.
        structPromotionInfo.containsHoles = true;
    }

    // Cool, this struct is promotable.

    structPromotionInfo.canPromote = true;
    return true;
}

//--------------------------------------------------------------------------------------------
// CanConstructAndPromoteField - checks if we can construct field types without asking about them directly.
//
// Arguments:
//   structPromotionInfo - struct promotion candidate information.
//
// Return value:
//   true if we can figure out the fields from available knowledge.
//
// Notes:
//   This is needed for AOT R2R compilation when we can't cross compilation bubble borders
//   so we should not ask about fields that are not directly referenced. If we do VM will have
//   to emit a type check for this field type but it does not have enough information about it.
//   As a workaround for perfomance critical corner case: struct with 1 gcref, we try to construct
//   the field information from indirect observations.
//
bool Compiler::StructPromotionHelper::CanConstructAndPromoteField(lvaStructPromotionInfo* structPromotionInfo)
{
    const CORINFO_CLASS_HANDLE typeHnd    = structPromotionInfo->typeHnd;
    const COMP_HANDLE          compHandle = compiler->info.compCompHnd;
    const DWORD                typeFlags  = compHandle->getClassAttribs(typeHnd);
    if (structPromotionInfo->fieldCnt != 1)
    {
        // Can't find out values for several fields.
        return false;
    }
    if ((typeFlags & CORINFO_FLG_CONTAINS_GC_PTR) == 0)
    {
        // Can't find out type of a non-gc field.
        return false;
    }

    const unsigned structSize = compHandle->getClassSize(typeHnd);
    if (structSize != TARGET_POINTER_SIZE)
    {
        return false;
    }

    assert(!structPromotionInfo->containsHoles);
    assert(!structPromotionInfo->customLayout);
    lvaStructFieldInfo& fldInfo = structPromotionInfo->fields[0];

    fldInfo.fldHnd = compHandle->getFieldInClass(typeHnd, 0);

    // We should not read it anymore.
    fldInfo.fldTypeHnd = 0;

    fldInfo.fldOffset  = 0;
    fldInfo.fldOrdinal = 0;
    fldInfo.fldSize    = TARGET_POINTER_SIZE;
    fldInfo.fldType    = TYP_REF;

    structPromotionInfo->canPromote = true;
    return true;
}

//--------------------------------------------------------------------------------------------
// CanPromoteStructVar - checks if the struct can be promoted.
//
// Arguments:
//   lclNum - struct number to check.
//
// Return value:
//   true if the struct var can be promoted.
//
bool Compiler::StructPromotionHelper::CanPromoteStructVar(unsigned lclNum)
{
    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);

    assert(varTypeIsStruct(varDsc));
    assert(!varDsc->lvPromoted); // Don't ask again :)

    // If this lclVar is used in a SIMD intrinsic, then we don't want to struct promote it.
    // Note, however, that SIMD lclVars that are NOT used in a SIMD intrinsic may be
    // profitably promoted.
    if (varDsc->lvIsUsedInSIMDIntrinsic())
    {
        JITDUMP("  struct promotion of V%02u is disabled because lvIsUsedInSIMDIntrinsic()\n", lclNum);
        return false;
    }

    // Reject struct promotion of parameters when -GS stack reordering is enabled
    // as we could introduce shadow copies of them.
    if (varDsc->lvIsParam && compiler->compGSReorderStackLayout)
    {
        JITDUMP("  struct promotion of V%02u is disabled because lvIsParam and compGSReorderStackLayout\n", lclNum);
        return false;
    }

    if (!compiler->lvaEnregMultiRegVars && varDsc->lvIsMultiRegArgOrRet())
    {
        JITDUMP("  struct promotion of V%02u is disabled because lvIsMultiRegArgOrRet()\n", lclNum);
        return false;
    }

    // If the local was exposed at Tier0, we currently have to assume it's aliased for OSR.
    //
    if (compiler->lvaIsOSRLocal(lclNum) && compiler->info.compPatchpointInfo->IsExposed(lclNum))
    {
        JITDUMP("  struct promotion of V%02u is disabled because it is an exposed OSR local\n", lclNum);
        return false;
    }

    CORINFO_CLASS_HANDLE typeHnd = varDsc->GetStructHnd();
    assert(typeHnd != NO_CLASS_HANDLE);

    bool canPromote = CanPromoteStructType(typeHnd);
    if (canPromote && varDsc->lvIsMultiRegArgOrRet())
    {
        unsigned fieldCnt = structPromotionInfo.fieldCnt;
        if (fieldCnt > MAX_MULTIREG_COUNT)
        {
            canPromote = false;
        }
#if defined(TARGET_ARMARCH)
        else
        {
            for (unsigned i = 0; canPromote && (i < fieldCnt); i++)
            {
                var_types fieldType = structPromotionInfo.fields[i].fldType;
                // Non-HFA structs are always passed in general purpose registers.
                // If there are any floating point fields, don't promote for now.
                // Likewise, since HVA structs are passed in SIMD registers
                // promotion of non FP or SIMD type fields is disallowed.
                // TODO-1stClassStructs: add support in Lowering and prolog generation
                // to enable promoting these types.
                if (varDsc->lvIsParam && (varDsc->lvIsHfa() != varTypeUsesFloatReg(fieldType)))
                {
                    canPromote = false;
                }
#if defined(FEATURE_SIMD)
                // If we have a register-passed struct with mixed non-opaque SIMD types (i.e. with defined fields)
                // and non-SIMD types, we don't currently handle that case in the prolog, so we can't promote.
                else if ((fieldCnt > 1) && varTypeIsStruct(fieldType) &&
                         !compiler->isOpaqueSIMDType(structPromotionInfo.fields[i].fldTypeHnd))
                {
                    canPromote = false;
                }
#endif // FEATURE_SIMD
            }
        }
#elif defined(UNIX_AMD64_ABI)
        else
        {
            SortStructFields();
            // Only promote if the field types match the registers, unless we have a single SIMD field.
            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            compiler->eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
            unsigned regCount = structDesc.eightByteCount;
            if ((structPromotionInfo.fieldCnt == 1) && varTypeIsSIMD(structPromotionInfo.fields[0].fldType))
            {
                // Allow the case of promoting a single SIMD field, even if there are multiple registers.
                // We will fix this up in the prolog.
            }
            else if (structPromotionInfo.fieldCnt != regCount)
            {
                canPromote = false;
            }
            else
            {
                for (unsigned i = 0; canPromote && (i < regCount); i++)
                {
                    lvaStructFieldInfo* fieldInfo = &(structPromotionInfo.fields[i]);
                    var_types           fieldType = fieldInfo->fldType;
                    // We don't currently support passing SIMD types in registers.
                    if (varTypeIsSIMD(fieldType))
                    {
                        canPromote = false;
                    }
                    else if (varTypeUsesFloatReg(fieldType) !=
                             (structDesc.eightByteClassifications[i] == SystemVClassificationTypeSSE))
                    {
                        canPromote = false;
                    }
                }
            }
        }
#endif // UNIX_AMD64_ABI
    }
    return canPromote;
}

//--------------------------------------------------------------------------------------------
// ShouldPromoteStructVar - Should a struct var be promoted if it can be promoted?
// This routine mainly performs profitability checks.  Right now it also has
// some correctness checks due to limitations of down-stream phases.
//
// Arguments:
//   lclNum - struct local number;
//
// Return value:
//   true if the struct should be promoted.
//
bool Compiler::StructPromotionHelper::ShouldPromoteStructVar(unsigned lclNum)
{
    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);
    assert(varTypeIsStruct(varDsc));
    assert(varDsc->GetStructHnd() == structPromotionInfo.typeHnd);
    assert(structPromotionInfo.canPromote);

    bool shouldPromote = true;

    // We *can* promote; *should* we promote?
    // We should only do so if promotion has potential savings.  One source of savings
    // is if a field of the struct is accessed, since this access will be turned into
    // an access of the corresponding promoted field variable.  Even if there are no
    // field accesses, but only block-level operations on the whole struct, if the struct
    // has only one or two fields, then doing those block operations field-wise is probably faster
    // than doing a whole-variable block operation (e.g., a hardware "copy loop" on x86).
    // Struct promotion also provides the following benefits: reduce stack frame size,
    // reduce the need for zero init of stack frame and fine grained constant/copy prop.
    // Asm diffs indicate that promoting structs up to 3 fields is a net size win.
    // So if no fields are accessed independently, and there are four or more fields,
    // then do not promote.
    //
    // TODO: Ideally we would want to consider the impact of whether the struct is
    // passed as a parameter or assigned the return value of a call. Because once promoted,
    // struct copying is done by field by field assignment instead of a more efficient
    // rep.stos or xmm reg based copy.
    if (structPromotionInfo.fieldCnt > 3 && !varDsc->lvFieldAccessed)
    {
        JITDUMP("Not promoting promotable struct local V%02u: #fields = %d, fieldAccessed = %d.\n", lclNum,
                structPromotionInfo.fieldCnt, varDsc->lvFieldAccessed);
        shouldPromote = false;
    }
    else if (varDsc->lvIsMultiRegRet && structPromotionInfo.containsHoles && structPromotionInfo.customLayout)
    {
        JITDUMP("Not promoting multi-reg returned struct local V%02u with holes.\n", lclNum);
        shouldPromote = false;
    }
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64)
    // TODO-PERF - Only do this when the LclVar is used in an argument context
    // TODO-ARM64 - HFA support should also eliminate the need for this.
    // TODO-ARM32 - HFA support should also eliminate the need for this.
    // TODO-LSRA - Currently doesn't support the passing of floating point LCL_VARS in the integer registers
    //
    // For now we currently don't promote structs with a single float field
    // Promoting it can cause us to shuffle it back and forth between the int and
    //  the float regs when it is used as a argument, which is very expensive for XARCH
    //
    else if ((structPromotionInfo.fieldCnt == 1) && varTypeIsFloating(structPromotionInfo.fields[0].fldType))
    {
        JITDUMP("Not promoting promotable struct local V%02u: #fields = %d because it is a struct with "
                "single float field.\n",
                lclNum, structPromotionInfo.fieldCnt);
        shouldPromote = false;
    }
#endif // TARGET_AMD64 || TARGET_ARM64 || TARGET_ARM || TARGET_LOONGARCH64
    else if (varDsc->lvIsParam && !compiler->lvaIsImplicitByRefLocal(lclNum) && !varDsc->lvIsHfa())
    {
#if FEATURE_MULTIREG_STRUCT_PROMOTE
        // Is this a variable holding a value with exactly two fields passed in
        // multiple registers?
        if (compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
        {
            if (structPromotionInfo.containsHoles && structPromotionInfo.customLayout)
            {
                JITDUMP("Not promoting multi-reg struct local V%02u with holes.\n", lclNum);
                shouldPromote = false;
            }
            else if ((structPromotionInfo.fieldCnt != 2) &&
                     !((structPromotionInfo.fieldCnt == 1) && varTypeIsSIMD(structPromotionInfo.fields[0].fldType)))
            {
                JITDUMP("Not promoting multireg struct local V%02u, because lvIsParam is true, #fields != 2 and it's "
                        "not a single SIMD.\n",
                        lclNum);
                shouldPromote = false;
            }
        }
        else
#endif // !FEATURE_MULTIREG_STRUCT_PROMOTE

            // TODO-PERF - Implement struct promotion for incoming single-register structs.
            //             Also the implementation of jmp uses the 4 byte move to store
            //             byte parameters to the stack, so that if we have a byte field
            //             with something else occupying the same 4-byte slot, it will
            //             overwrite other fields.
            if (structPromotionInfo.fieldCnt != 1)
        {
            JITDUMP("Not promoting promotable struct local V%02u, because lvIsParam is true and #fields = "
                    "%d.\n",
                    lclNum, structPromotionInfo.fieldCnt);
            shouldPromote = false;
        }
    }
    else if ((lclNum == compiler->genReturnLocal) && (structPromotionInfo.fieldCnt > 1))
    {
        // TODO-1stClassStructs: a temporary solution to keep diffs small, it will be fixed later.
        shouldPromote = false;
    }
#if defined(DEBUG)
    else if (compiler->compPromoteFewerStructs(lclNum))
    {
        // Do not promote some structs, that can be promoted, to stress promoted/unpromoted moves.
        JITDUMP("Not promoting promotable struct local V%02u, because of STRESS_PROMOTE_FEWER_STRUCTS\n", lclNum);
        shouldPromote = false;
    }
#endif

    //
    // If the lvRefCnt is zero and we have a struct promoted parameter we can end up with an extra store of
    // the the incoming register into the stack frame slot.
    // In that case, we would like to avoid promortion.
    // However we haven't yet computed the lvRefCnt values so we can't do that.
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

    return shouldPromote;
}

//--------------------------------------------------------------------------------------------
// SortStructFields - sort the fields according to the increasing order of the field offset.
//
// Notes:
//   This is needed because the fields need to be pushed on stack (when referenced as a struct) in offset order.
//
void Compiler::StructPromotionHelper::SortStructFields()
{
    if (!structPromotionInfo.fieldsSorted)
    {
        jitstd::sort(structPromotionInfo.fields, structPromotionInfo.fields + structPromotionInfo.fieldCnt,
                     lvaFieldOffsetCmp());
        structPromotionInfo.fieldsSorted = true;
    }
}

//--------------------------------------------------------------------------------------------
// GetFieldInfo - get struct field information.
// Arguments:
//   fieldHnd - field handle to get info for;
//   ordinal  - field ordinal.
//
// Return value:
//  field information.
//
Compiler::lvaStructFieldInfo Compiler::StructPromotionHelper::GetFieldInfo(CORINFO_FIELD_HANDLE fieldHnd, BYTE ordinal)
{
    lvaStructFieldInfo fieldInfo;
    fieldInfo.fldHnd = fieldHnd;

    unsigned fldOffset  = compiler->info.compCompHnd->getFieldOffset(fieldInfo.fldHnd);
    fieldInfo.fldOffset = (BYTE)fldOffset;

    fieldInfo.fldOrdinal = ordinal;
    CorInfoType corType  = compiler->info.compCompHnd->getFieldType(fieldInfo.fldHnd, &fieldInfo.fldTypeHnd);
    fieldInfo.fldType    = JITtype2varType(corType);
    fieldInfo.fldSize    = genTypeSize(fieldInfo.fldType);

#ifdef FEATURE_SIMD
    // Check to see if this is a SIMD type.
    // We will only check this if we have already found a SIMD type, which will be true if
    // we have encountered any SIMD intrinsics.
    if (compiler->usesSIMDTypes() && (fieldInfo.fldSize == 0) && compiler->isSIMDorHWSIMDClass(fieldInfo.fldTypeHnd))
    {
        unsigned    simdSize;
        CorInfoType simdBaseJitType = compiler->getBaseJitTypeAndSizeOfSIMDType(fieldInfo.fldTypeHnd, &simdSize);
        // We will only promote fields of SIMD types that fit into a SIMD register.
        if (simdBaseJitType != CORINFO_TYPE_UNDEF)
        {
            if ((simdSize >= compiler->minSIMDStructBytes()) && (simdSize <= compiler->maxSIMDStructBytes()))
            {
                fieldInfo.fldType = compiler->getSIMDTypeForSize(simdSize);
                fieldInfo.fldSize = simdSize;
#ifdef DEBUG
                retypedFieldsMap.Set(fieldInfo.fldHnd, fieldInfo.fldType, RetypedAsScalarFieldsMap::Overwrite);
#endif // DEBUG
            }
        }
    }
#endif // FEATURE_SIMD

    if (fieldInfo.fldSize == 0)
    {
        TryPromoteStructField(fieldInfo);
    }

    return fieldInfo;
}

//--------------------------------------------------------------------------------------------
// TryPromoteStructField - checks that this struct's field is a struct that can be promoted as scalar type
//   aligned at its natural boundary. Promotes the field as a scalar if the check succeeded.
//
// Arguments:
//   fieldInfo - information about the field in the outer struct.
//
// Return value:
//   true if the internal struct was promoted.
//
bool Compiler::StructPromotionHelper::TryPromoteStructField(lvaStructFieldInfo& fieldInfo)
{
    // Size of TYP_BLK, TYP_FUNC, TYP_VOID and TYP_STRUCT is zero.
    // Early out if field type is other than TYP_STRUCT.
    // This is a defensive check as we don't expect a struct to have
    // fields of TYP_BLK, TYP_FUNC or TYP_VOID.
    if (fieldInfo.fldType != TYP_STRUCT)
    {
        return false;
    }

    COMP_HANDLE compHandle = compiler->info.compCompHnd;

    // Do not promote if the struct field in turn has more than one field.
    if (compHandle->getClassNumInstanceFields(fieldInfo.fldTypeHnd) != 1)
    {
        return false;
    }

    // Do not promote if the single field is not aligned at its natural boundary within
    // the struct field.
    CORINFO_FIELD_HANDLE innerFieldHndl   = compHandle->getFieldInClass(fieldInfo.fldTypeHnd, 0);
    unsigned             innerFieldOffset = compHandle->getFieldOffset(innerFieldHndl);
    if (innerFieldOffset != 0)
    {
        return false;
    }

    CorInfoType fieldCorType = compHandle->getFieldType(innerFieldHndl);
    var_types   fieldVarType = JITtype2varType(fieldCorType);
    unsigned    fieldSize    = genTypeSize(fieldVarType);

    // Do not promote if the field is not a primitive type, is floating-point,
    // or is not properly aligned.
    //
    // TODO-PERF: Structs containing a single floating-point field on Amd64
    // need to be passed in integer registers. Right now LSRA doesn't support
    // passing of floating-point LCL_VARS in integer registers.  Enabling promotion
    // of such structs results in an assert in lsra right now.
    //
    // TODO-CQ: Right now we only promote an actual SIMD typed field, which would cause
    // a nested SIMD type to fail promotion.
    if (fieldSize == 0 || fieldSize > TARGET_POINTER_SIZE || varTypeIsFloating(fieldVarType))
    {
        JITDUMP("Promotion blocked: struct contains struct field with one field,"
                " but that field has invalid size or type.\n");
        return false;
    }

    if (fieldSize != TARGET_POINTER_SIZE)
    {
        unsigned outerFieldOffset = compHandle->getFieldOffset(fieldInfo.fldHnd);

        if ((outerFieldOffset % fieldSize) != 0)
        {
            JITDUMP("Promotion blocked: struct contains struct field with one field,"
                    " but the outer struct offset %u is not a multiple of the inner field size %u.\n",
                    outerFieldOffset, fieldSize);
            return false;
        }
    }

    // Insist this wrapped field occupy all of its parent storage.
    unsigned innerStructSize = compHandle->getClassSize(fieldInfo.fldTypeHnd);

    if (fieldSize != innerStructSize)
    {
        JITDUMP("Promotion blocked: struct contains struct field with one field,"
                " but that field is not the same size as its parent.\n");
        return false;
    }

    // Retype the field as the type of the single field of the struct.
    // This is a hack that allows us to promote such fields before we support recursive struct promotion
    // (tracked by #10019).
    fieldInfo.fldType = fieldVarType;
    fieldInfo.fldSize = fieldSize;
#ifdef DEBUG
    retypedFieldsMap.Set(fieldInfo.fldHnd, fieldInfo.fldType, RetypedAsScalarFieldsMap::Overwrite);
#endif // DEBUG
    return true;
}

//--------------------------------------------------------------------------------------------
// PromoteStructVar - promote struct variable.
//
// Arguments:
//   lclNum - struct local number;
//
void Compiler::StructPromotionHelper::PromoteStructVar(unsigned lclNum)
{
    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);

    // We should never see a reg-sized non-field-addressed struct here.
    assert(!varDsc->lvRegStruct);

    assert(varDsc->GetStructHnd() == structPromotionInfo.typeHnd);
    assert(structPromotionInfo.canPromote);

    varDsc->lvFieldCnt      = structPromotionInfo.fieldCnt;
    varDsc->lvFieldLclStart = compiler->lvaCount;
    varDsc->lvPromoted      = true;
    varDsc->lvContainsHoles = structPromotionInfo.containsHoles;
    varDsc->lvCustomLayout  = structPromotionInfo.customLayout;

#ifdef DEBUG
    // Don't change the source to a TYP_BLK either.
    varDsc->lvKeepType = 1;
#endif

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\nPromoting struct local V%02u (%s):", lclNum, compiler->eeGetClassName(varDsc->GetStructHnd()));
    }
#endif

    SortStructFields();

    for (unsigned index = 0; index < structPromotionInfo.fieldCnt; ++index)
    {
        const lvaStructFieldInfo* pFieldInfo = &structPromotionInfo.fields[index];

        if (varTypeUsesFloatReg(pFieldInfo->fldType))
        {
            // Whenever we promote a struct that contains a floating point field
            // it's possible we transition from a method that originally only had integer
            // local vars to start having FP.  We have to communicate this through this flag
            // since LSRA later on will use this flag to determine whether or not to track FP register sets.
            compiler->compFloatingPointUsed = true;
        }

// Now grab the temp for the field local.

#ifdef DEBUG
        char buf[200];
        sprintf_s(buf, sizeof(buf), "%s V%02u.%s (fldOffset=0x%x)", "field", lclNum,
                  compiler->eeGetFieldName(pFieldInfo->fldHnd), pFieldInfo->fldOffset);

        // We need to copy 'buf' as lvaGrabTemp() below caches a copy to its argument.
        size_t len  = strlen(buf) + 1;
        char*  bufp = compiler->getAllocator(CMK_DebugOnly).allocate<char>(len);
        strcpy_s(bufp, len, buf);

        if (index > 0)
        {
            noway_assert(pFieldInfo->fldOffset > (pFieldInfo - 1)->fldOffset);
        }
#endif

        // Lifetime of field locals might span multiple BBs, so they must be long lifetime temps.
        const unsigned varNum = compiler->lvaGrabTemp(false DEBUGARG(bufp));

        // lvaGrabTemp can reallocate the lvaTable, so
        // refresh the cached varDsc for lclNum.
        varDsc = compiler->lvaGetDesc(lclNum);

        LclVarDsc* fieldVarDsc       = compiler->lvaGetDesc(varNum);
        fieldVarDsc->lvType          = pFieldInfo->fldType;
        fieldVarDsc->lvExactSize     = pFieldInfo->fldSize;
        fieldVarDsc->lvIsStructField = true;
        fieldVarDsc->lvFieldHnd      = pFieldInfo->fldHnd;
        fieldVarDsc->lvFldOffset     = pFieldInfo->fldOffset;
        fieldVarDsc->lvFldOrdinal    = pFieldInfo->fldOrdinal;
        fieldVarDsc->lvParentLcl     = lclNum;
        fieldVarDsc->lvIsParam       = varDsc->lvIsParam;
        fieldVarDsc->lvIsOSRLocal    = varDsc->lvIsOSRLocal;

        // This new local may be the first time we've seen a long typed local.
        if (fieldVarDsc->lvType == TYP_LONG)
        {
            compiler->compLongUsed = true;
        }

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

        // Reset the implicitByRef flag.
        fieldVarDsc->lvIsImplicitByRef = 0;

#endif

        // Do we have a parameter that can be enregistered?
        //
        if (varDsc->lvIsRegArg)
        {
            fieldVarDsc->lvIsRegArg = true;
            regNumber parentArgReg  = varDsc->GetArgReg();
#if FEATURE_MULTIREG_ARGS
            if (!compiler->lvaIsImplicitByRefLocal(lclNum))
            {
#ifdef UNIX_AMD64_ABI
                if (varTypeIsSIMD(fieldVarDsc) && (varDsc->lvFieldCnt == 1))
                {
                    // This SIMD typed field may be passed in multiple registers.
                    fieldVarDsc->SetArgReg(parentArgReg);
                    fieldVarDsc->SetOtherArgReg(varDsc->GetOtherArgReg());
                }
                else
#endif // UNIX_AMD64_ABI
                {
                    regNumber fieldRegNum;
                    if (index == 0)
                    {
                        fieldRegNum = parentArgReg;
                    }
                    else if (varDsc->lvIsHfa())
                    {
                        unsigned regIncrement = fieldVarDsc->lvFldOrdinal;
#ifdef TARGET_ARM
                        // TODO: Need to determine if/how to handle split args.
                        if (varDsc->GetHfaType() == TYP_DOUBLE)
                        {
                            regIncrement *= 2;
                        }
#endif // TARGET_ARM
                        fieldRegNum = (regNumber)(parentArgReg + regIncrement);
                    }
                    else
                    {
                        assert(index == 1);
                        fieldRegNum = varDsc->GetOtherArgReg();
                    }
                    fieldVarDsc->SetArgReg(fieldRegNum);
                }
            }
            else
#endif // FEATURE_MULTIREG_ARGS && defined(FEATURE_SIMD)
            {
                fieldVarDsc->SetArgReg(parentArgReg);
            }
        }

#ifdef FEATURE_SIMD
        if (varTypeIsSIMD(pFieldInfo->fldType))
        {
            // Set size to zero so that lvaSetStruct will appropriately set the SIMD-relevant fields.
            fieldVarDsc->lvExactSize = 0;
            compiler->lvaSetStruct(varNum, pFieldInfo->fldTypeHnd, false, true);
            // We will not recursively promote this, so mark it as 'lvRegStruct' (note that we wouldn't
            // be promoting this if we didn't think it could be enregistered.
            fieldVarDsc->lvRegStruct = true;
        }
#endif // FEATURE_SIMD

#ifdef DEBUG
        // This temporary should not be converted to a double in stress mode,
        // because we introduce assigns to it after the stress conversion
        fieldVarDsc->lvKeepType = 1;
#endif
    }
}

//--------------------------------------------------------------------------------------------
// lvaGetFieldLocal - returns the local var index for a promoted field in a promoted struct var.
//
// Arguments:
//   varDsc    - the promoted struct var descriptor;
//   fldOffset - field offset in the struct.
//
// Return value:
//   the index of the local that represents this field.
//
unsigned Compiler::lvaGetFieldLocal(const LclVarDsc* varDsc, unsigned int fldOffset)
{
    noway_assert(varTypeIsStruct(varDsc));
    noway_assert(varDsc->lvPromoted);

    for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
    {
        noway_assert(lvaTable[i].lvIsStructField);
        noway_assert(lvaTable[i].lvParentLcl == (unsigned)(varDsc - lvaTable));
        if (lvaTable[i].lvFldOffset == fldOffset)
        {
            return i;
        }
    }

    // This is the not-found error return path, the caller should check for BAD_VAR_NUM
    return BAD_VAR_NUM;
}

/*****************************************************************************
 *
 *  Set the local var "varNum" as address-exposed.
 *  If this is a promoted struct, label it's fields the same way.
 */

void Compiler::lvaSetVarAddrExposed(unsigned varNum DEBUGARG(AddressExposedReason reason))
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);

    varDsc->SetAddressExposed(true DEBUGARG(reason));

    if (varDsc->lvPromoted)
    {
        noway_assert(varTypeIsStruct(varDsc));

        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            noway_assert(lvaTable[i].lvIsStructField);
            lvaTable[i].SetAddressExposed(true DEBUGARG(AddressExposedReason::PARENT_EXPOSED));
            lvaSetVarDoNotEnregister(i DEBUGARG(DoNotEnregisterReason::AddrExposed));
        }
    }

    lvaSetVarDoNotEnregister(varNum DEBUGARG(DoNotEnregisterReason::AddrExposed));
}

//------------------------------------------------------------------------
// lvaSetHiddenBufferStructArg: Set the local var "varNum" as hidden buffer struct arg.
//
// Arguments:
//    varNum - the varNum of the local
//
// Notes:
//    Most ABIs "return" large structures via return buffers, where the callee takes an address as the
//    argument, and writes the result to it. This presents a problem: ordinarily, addresses of locals
//    that escape to calls leave the local in question address-exposed. For this very special case of
//    a return buffer, however, it is known that the callee will not do anything with it except write
//    to it, once. As such, we handle addresses of locals that represent return buffers specially: we
//    *do not* mark the local address-exposed, instead saving the address' "Use*" in the call node, and
//    treat the call much like an "ASG(IND(addr), ...)" node throughout the compilation. A complicating
//    factor here is that the address can be moved to the late args list, and we have to fetch it from
//    the ASG setup node in that case. In the future, we should make it such that these addresses do
//    not ever need temps (currently they may because of conservative GLOB_REF setting on FIELD nodes).
//
//    TODO-ADDR-Bug: currently, we rely on these locals not being present in call argument lists,
//    outside of the buffer address argument itself, as liveness - currently - treats the location node
//    associated with the address itself as the definition point, and call arguments can be reordered
//    rather arbitrarily. We should fix liveness to treat the call as the definition point instead and
//    enable this optimization for "!lvIsTemp" locals.
//

void Compiler::lvaSetHiddenBufferStructArg(unsigned varNum)
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);

#ifdef DEBUG
    varDsc->SetHiddenBufferStructArg(true);
#endif

    if (varDsc->lvPromoted)
    {
        noway_assert(varTypeIsStruct(varDsc));

        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            noway_assert(lvaTable[i].lvIsStructField);
#ifdef DEBUG
            lvaTable[i].SetHiddenBufferStructArg(true);
#endif

            lvaSetVarDoNotEnregister(i DEBUGARG(DoNotEnregisterReason::HiddenBufferStructArg));
        }
    }

    lvaSetVarDoNotEnregister(varNum DEBUGARG(DoNotEnregisterReason::HiddenBufferStructArg));
}

//------------------------------------------------------------------------
// lvaSetVarLiveInOutOfHandler: Set the local varNum as being live in and/or out of a handler
//
// Arguments:
//    varNum - the varNum of the local
//
void Compiler::lvaSetVarLiveInOutOfHandler(unsigned varNum)
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);

    varDsc->lvLiveInOutOfHndlr = 1;

    if (varDsc->lvPromoted)
    {
        noway_assert(varTypeIsStruct(varDsc));

        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            noway_assert(lvaTable[i].lvIsStructField);
            lvaTable[i].lvLiveInOutOfHndlr = 1;
            // For now, only enregister an EH Var if it is a single def and whose refCnt > 1.
            if (!lvaEnregEHVars || !lvaTable[i].lvSingleDefRegCandidate || lvaTable[i].lvRefCnt() <= 1)
            {
                lvaSetVarDoNotEnregister(i DEBUGARG(DoNotEnregisterReason::LiveInOutOfHandler));
            }
        }
    }

    // For now, only enregister an EH Var if it is a single def and whose refCnt > 1.
    if (!lvaEnregEHVars || !varDsc->lvSingleDefRegCandidate || varDsc->lvRefCnt() <= 1)
    {
        lvaSetVarDoNotEnregister(varNum DEBUGARG(DoNotEnregisterReason::LiveInOutOfHandler));
    }
#ifdef JIT32_GCENCODER
    else if (lvaKeepAliveAndReportThis() && (varNum == info.compThisArg))
    {
        // For the JIT32_GCENCODER, when lvaKeepAliveAndReportThis is true, we must either keep the "this" pointer
        // in the same register for the entire method, or keep it on the stack. If it is EH-exposed, we can't ever
        // keep it in a register, since it must also be live on the stack. Therefore, we won't attempt to allocate it.
        lvaSetVarDoNotEnregister(varNum DEBUGARG(DoNotEnregisterReason::LiveInOutOfHandler));
    }
#endif // JIT32_GCENCODER
}

/*****************************************************************************
 *
 *  Record that the local var "varNum" should not be enregistered (for one of several reasons.)
 */

void Compiler::lvaSetVarDoNotEnregister(unsigned varNum DEBUGARG(DoNotEnregisterReason reason))
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);

    const bool wasAlreadyMarkedDoNotEnreg = (varDsc->lvDoNotEnregister == 1);
    varDsc->lvDoNotEnregister             = 1;

#ifdef DEBUG
    if (!wasAlreadyMarkedDoNotEnreg)
    {
        varDsc->SetDoNotEnregReason(reason);
    }

    if (verbose)
    {
        printf("\nLocal V%02u should not be enregistered because: ", varNum);
    }

    switch (reason)
    {
        case DoNotEnregisterReason::AddrExposed:
            JITDUMP("it is address exposed\n");
            assert(varDsc->IsAddressExposed());
            break;
        case DoNotEnregisterReason::HiddenBufferStructArg:
            JITDUMP("it is hidden buffer struct arg\n");
            assert(varDsc->IsHiddenBufferStructArg());
            break;
        case DoNotEnregisterReason::DontEnregStructs:
            JITDUMP("struct enregistration is disabled\n");
            assert(varTypeIsStruct(varDsc));
            break;
        case DoNotEnregisterReason::NotRegSizeStruct:
            JITDUMP("struct size does not match reg size\n");
            assert(varTypeIsStruct(varDsc));
            break;
        case DoNotEnregisterReason::LocalField:
            JITDUMP("was accessed as a local field\n");
            break;
        case DoNotEnregisterReason::VMNeedsStackAddr:
            JITDUMP("VM needs stack addr\n");
            break;
        case DoNotEnregisterReason::LiveInOutOfHandler:
            JITDUMP("live in/out of a handler\n");
            varDsc->lvLiveInOutOfHndlr = 1;
            break;
        case DoNotEnregisterReason::BlockOp:
            JITDUMP("written/read in a block op\n");
            break;
        case DoNotEnregisterReason::IsStructArg:
            if (varTypeIsStruct(varDsc))
            {
                JITDUMP("it is a struct arg\n");
            }
            else
            {
                JITDUMP("it is reinterpreted as a struct arg\n");
            }
            break;
        case DoNotEnregisterReason::DepField:
            JITDUMP("field of a dependently promoted struct\n");
            assert(varDsc->lvIsStructField && (lvaGetParentPromotionType(varNum) != PROMOTION_TYPE_INDEPENDENT));
            break;
        case DoNotEnregisterReason::NoRegVars:
            JITDUMP("opts.compFlags & CLFLG_REGVAR is not set\n");
            assert(!compEnregLocals());
            break;
        case DoNotEnregisterReason::MinOptsGC:
            JITDUMP("it is a GC Ref and we are compiling MinOpts\n");
            assert(!JitConfig.JitMinOptsTrackGCrefs() && varTypeIsGC(varDsc->TypeGet()));
            break;
#if !defined(TARGET_64BIT)
        case DoNotEnregisterReason::LongParamField:
            JITDUMP("it is a decomposed field of a long parameter\n");
            break;
#endif
#ifdef JIT32_GCENCODER
        case DoNotEnregisterReason::PinningRef:
            JITDUMP("pinning ref\n");
            assert(varDsc->lvPinned);
            break;
#endif
        case DoNotEnregisterReason::LclAddrNode:
            JITDUMP("LclAddrVar/Fld takes the address of this node\n");
            break;

        case DoNotEnregisterReason::CastTakesAddr:
            JITDUMP("cast takes addr\n");
            break;

        case DoNotEnregisterReason::StoreBlkSrc:
            JITDUMP("the local is used as store block src\n");
            break;

        case DoNotEnregisterReason::OneAsgRetyping:
            JITDUMP("OneAsg forbids enreg\n");
            break;

        case DoNotEnregisterReason::SwizzleArg:
            JITDUMP("SwizzleArg\n");
            break;

        case DoNotEnregisterReason::BlockOpRet:
            JITDUMP("return uses a block op\n");
            break;

        case DoNotEnregisterReason::ReturnSpCheck:
            JITDUMP("Used for SP check\n");
            break;

        case DoNotEnregisterReason::SimdUserForcesDep:
            JITDUMP("Promoted struct used by a SIMD/HWI node\n");
            break;

        default:
            unreached();
            break;
    }
#endif
}

// Returns true if this local var is a multireg struct.
// TODO-Throughput: This does a lookup on the class handle, and in the outgoing arg context
// this information is already available on the fgArgTabEntry, and shouldn't need to be
// recomputed.
//
bool Compiler::lvaIsMultiregStruct(LclVarDsc* varDsc, bool isVarArg)
{
    if (varTypeIsStruct(varDsc->TypeGet()))
    {
        CORINFO_CLASS_HANDLE clsHnd = varDsc->GetStructHnd();
        structPassingKind    howToPassStruct;

        var_types type = getArgTypeForStruct(clsHnd, &howToPassStruct, isVarArg, varDsc->lvExactSize);

        if (howToPassStruct == SPK_ByValueAsHfa)
        {
            assert(type == TYP_STRUCT);
            return true;
        }

#if defined(UNIX_AMD64_ABI) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
        if (howToPassStruct == SPK_ByValue)
        {
            assert(type == TYP_STRUCT);
            return true;
        }
#endif
    }
    return false;
}

/*****************************************************************************
 * Set the lvClass for a local variable of a struct type */

void Compiler::lvaSetStruct(unsigned varNum, CORINFO_CLASS_HANDLE typeHnd, bool unsafeValueClsCheck, bool setTypeInfo)
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);
    if (setTypeInfo)
    {
        varDsc->lvVerTypeInfo = typeInfo(TI_STRUCT, typeHnd);
    }

    // Set the type and associated info if we haven't already set it.
    if (varDsc->lvType == TYP_UNDEF)
    {
        varDsc->lvType = TYP_STRUCT;
    }
    if (varDsc->GetLayout() == nullptr)
    {
        ClassLayout* layout = typGetObjLayout(typeHnd);
        varDsc->SetLayout(layout);

        assert(varDsc->lvExactSize == 0);
        varDsc->lvExactSize = layout->GetSize();
        assert(varDsc->lvExactSize != 0);

        if (layout->IsValueClass())
        {
            CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
            varDsc->lvType              = impNormStructType(typeHnd, &simdBaseJitType);

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
            // Mark implicit byref struct parameters
            if (varDsc->lvIsParam && !varDsc->lvIsStructField)
            {
                structPassingKind howToReturnStruct;
                getArgTypeForStruct(typeHnd, &howToReturnStruct, this->info.compIsVarArgs, varDsc->lvExactSize);

                if (howToReturnStruct == SPK_ByReference)
                {
                    JITDUMP("Marking V%02i as a byref parameter\n", varNum);
                    varDsc->lvIsImplicitByRef = 1;
                }
            }
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

#if FEATURE_SIMD
            if (simdBaseJitType != CORINFO_TYPE_UNDEF)
            {
                assert(varTypeIsSIMD(varDsc));
                varDsc->lvSIMDType = true;
                varDsc->SetSimdBaseJitType(simdBaseJitType);
            }
#endif // FEATURE_SIMD
            if (GlobalJitOptions::compFeatureHfa)
            {
                // For structs that are small enough, we check and set HFA element type
                if (varDsc->lvExactSize <= MAX_PASS_MULTIREG_BYTES)
                {
                    // hfaType is set to float, double or SIMD type if it is an HFA, otherwise TYP_UNDEF
                    var_types hfaType = GetHfaType(typeHnd);
                    if (varTypeIsValidHfaType(hfaType))
                    {
                        varDsc->SetHfaType(hfaType);

                        // hfa variables can never contain GC pointers
                        assert(!layout->HasGCPtr());
                        // The size of this struct should be evenly divisible by 4 or 8
                        assert((varDsc->lvExactSize % genTypeSize(hfaType)) == 0);
                        // The number of elements in the HFA should fit into our MAX_ARG_REG_COUNT limit
                        assert((varDsc->lvExactSize / genTypeSize(hfaType)) <= MAX_ARG_REG_COUNT);
                    }
                }
            }
        }
    }
    else
    {
#if FEATURE_SIMD
        assert(!varTypeIsSIMD(varDsc) || (varDsc->GetSimdBaseType() != TYP_UNKNOWN));
#endif // FEATURE_SIMD
        ClassLayout* layout = typGetObjLayout(typeHnd);
        assert(ClassLayout::AreCompatible(varDsc->GetLayout(), layout));
        // Inlining could replace a canon struct type with an exact one.
        varDsc->SetLayout(layout);
        assert(varDsc->lvExactSize != 0);
    }

#ifndef TARGET_64BIT
    bool fDoubleAlignHint = false;
#ifdef TARGET_X86
    fDoubleAlignHint = true;
#endif

    if (info.compCompHnd->getClassAlignmentRequirement(typeHnd, fDoubleAlignHint) == 8)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("Marking struct in V%02i with double align flag\n", varNum);
        }
#endif
        varDsc->lvStructDoubleAlign = 1;
    }
#endif // not TARGET_64BIT

    unsigned classAttribs = info.compCompHnd->getClassAttribs(typeHnd);

    varDsc->lvOverlappingFields = StructHasOverlappingFields(classAttribs);

    // Check whether this local is an unsafe value type and requires GS cookie protection.
    // GS checks require the stack to be re-ordered, which can't be done with EnC.
    if (unsafeValueClsCheck && (classAttribs & CORINFO_FLG_UNSAFE_VALUECLASS) && !opts.compDbgEnC)
    {
        setNeedsGSSecurityCookie();
        compGSReorderStackLayout = true;
        varDsc->lvIsUnsafeBuffer = true;
    }
#ifdef DEBUG
    if (JitConfig.EnableExtraSuperPmiQueries())
    {
        makeExtraStructQueries(typeHnd, 2);
    }
#endif // DEBUG
}

#ifdef DEBUG
//------------------------------------------------------------------------
// makeExtraStructQueries: Query the information for the given struct handle.
//
// Arguments:
//    structHandle -- The handle for the struct type we're querying.
//    level        -- How many more levels to recurse.
//
void Compiler::makeExtraStructQueries(CORINFO_CLASS_HANDLE structHandle, int level)
{
    if (level <= 0)
    {
        return;
    }
    assert(structHandle != NO_CLASS_HANDLE);
    (void)typGetObjLayout(structHandle);
    DWORD typeFlags = info.compCompHnd->getClassAttribs(structHandle);
    if (StructHasDontDigFieldsFlagSet(typeFlags))
    {
        // In AOT ReadyToRun compilation, don't query fields of types
        // outside of the current version bubble.
        return;
    }
    unsigned fieldCnt = info.compCompHnd->getClassNumInstanceFields(structHandle);
    impNormStructType(structHandle);
#ifdef TARGET_ARMARCH
    GetHfaType(structHandle);
#endif
    for (unsigned int i = 0; i < fieldCnt; i++)
    {
        CORINFO_FIELD_HANDLE fieldHandle      = info.compCompHnd->getFieldInClass(structHandle, i);
        unsigned             fldOffset        = info.compCompHnd->getFieldOffset(fieldHandle);
        CORINFO_CLASS_HANDLE fieldClassHandle = NO_CLASS_HANDLE;
        CorInfoType          fieldCorType     = info.compCompHnd->getFieldType(fieldHandle, &fieldClassHandle);
        var_types            fieldVarType     = JITtype2varType(fieldCorType);
        if (fieldClassHandle != NO_CLASS_HANDLE)
        {
            if (varTypeIsStruct(fieldVarType))
            {
                makeExtraStructQueries(fieldClassHandle, level - 1);
            }
        }
    }
}
#endif // DEBUG

//------------------------------------------------------------------------
// lvaSetStructUsedAsVarArg: update hfa information for vararg struct args
//
// Arguments:
//    varNum   -- number of the variable
//
// Notes:
//    This only affects arm64 varargs on windows where we need to pass
//    hfa arguments as if they are not HFAs.
//
//    This function should only be called if the struct is used in a varargs
//    method.

void Compiler::lvaSetStructUsedAsVarArg(unsigned varNum)
{
    if (GlobalJitOptions::compFeatureHfa && TargetOS::IsWindows)
    {
#if defined(TARGET_ARM64)
        LclVarDsc* varDsc = lvaGetDesc(varNum);
        // For varargs methods incoming and outgoing arguments should not be treated
        // as HFA.
        varDsc->SetHfaType(TYP_UNDEF);
#endif // defined(TARGET_ARM64)
    }
}

//------------------------------------------------------------------------
// lvaSetClass: set class information for a local var.
//
// Arguments:
//    varNum -- number of the variable
//    clsHnd -- class handle to use in set or update
//    isExact -- true if class is known exactly
//
// Notes:
//    varNum must not already have a ref class handle.

void Compiler::lvaSetClass(unsigned varNum, CORINFO_CLASS_HANDLE clsHnd, bool isExact)
{
    noway_assert(varNum < lvaCount);

    // If we are just importing, we cannot reliably track local ref types,
    // since the jit maps CORINFO_TYPE_VAR to TYP_REF.
    if (compIsForImportOnly())
    {
        return;
    }

    // Else we should have a type handle.
    assert(clsHnd != nullptr);

    LclVarDsc* varDsc = lvaGetDesc(varNum);
    assert(varDsc->lvType == TYP_REF);

    // We shoud not have any ref type information for this var.
    assert(varDsc->lvClassHnd == NO_CLASS_HANDLE);
    assert(!varDsc->lvClassIsExact);

    JITDUMP("\nlvaSetClass: setting class for V%02i to (%p) %s %s\n", varNum, dspPtr(clsHnd), eeGetClassName(clsHnd),
            isExact ? " [exact]" : "");

    varDsc->lvClassHnd     = clsHnd;
    varDsc->lvClassIsExact = isExact;
}

//------------------------------------------------------------------------
// lvaSetClass: set class information for a local var from a tree or stack type
//
// Arguments:
//    varNum -- number of the variable. Must be a single def local
//    tree  -- tree establishing the variable's value
//    stackHnd -- handle for the type from the evaluation stack
//
// Notes:
//    Preferentially uses the tree's type, when available. Since not all
//    tree kinds can track ref types, the stack type is used as a
//    fallback. If there is no stack type, then the class is set to object.

void Compiler::lvaSetClass(unsigned varNum, GenTree* tree, CORINFO_CLASS_HANDLE stackHnd)
{
    bool                 isExact   = false;
    bool                 isNonNull = false;
    CORINFO_CLASS_HANDLE clsHnd    = gtGetClassHandle(tree, &isExact, &isNonNull);

    if (clsHnd != nullptr)
    {
        lvaSetClass(varNum, clsHnd, isExact);
    }
    else if (stackHnd != nullptr)
    {
        lvaSetClass(varNum, stackHnd);
    }
    else
    {
        lvaSetClass(varNum, impGetObjectClass());
    }
}

//------------------------------------------------------------------------
// lvaUpdateClass: update class information for a local var.
//
// Arguments:
//    varNum -- number of the variable
//    clsHnd -- class handle to use in set or update
//    isExact -- true if class is known exactly
//
// Notes:
//
//    This method models the type update rule for an assignment.
//
//    Updates currently should only happen for single-def user args or
//    locals, when we are processing the expression actually being
//    used to initialize the local (or inlined arg). The update will
//    change the local from the declared type to the type of the
//    initial value.
//
//    These updates should always *improve* what we know about the
//    type, that is making an inexact type exact, or changing a type
//    to some subtype. However the jit lacks precise type information
//    for shared code, so ensuring this is so is currently not
//    possible.

void Compiler::lvaUpdateClass(unsigned varNum, CORINFO_CLASS_HANDLE clsHnd, bool isExact)
{
    assert(varNum < lvaCount);

    // If we are just importing, we cannot reliably track local ref types,
    // since the jit maps CORINFO_TYPE_VAR to TYP_REF.
    if (compIsForImportOnly())
    {
        return;
    }

    // Else we should have a class handle to consider
    assert(clsHnd != nullptr);

    LclVarDsc* varDsc = lvaGetDesc(varNum);
    assert(varDsc->lvType == TYP_REF);

    // We should already have a class
    assert(varDsc->lvClassHnd != NO_CLASS_HANDLE);

    // We should only be updating classes for single-def locals.
    assert(varDsc->lvSingleDef);

    // Now see if we should update.
    //
    // New information may not always be "better" so do some
    // simple analysis to decide if the update is worthwhile.
    const bool isNewClass   = (clsHnd != varDsc->lvClassHnd);
    bool       shouldUpdate = false;

    // Are we attempting to update the class? Only check this when we have
    // an new type and the existing class is inexact... we should not be
    // updating exact classes.
    if (!varDsc->lvClassIsExact && isNewClass)
    {
        shouldUpdate = !!info.compCompHnd->isMoreSpecificType(varDsc->lvClassHnd, clsHnd);
    }
    // Else are we attempting to update exactness?
    else if (isExact && !varDsc->lvClassIsExact && !isNewClass)
    {
        shouldUpdate = true;
    }

#if DEBUG
    if (isNewClass || (isExact != varDsc->lvClassIsExact))
    {
        JITDUMP("\nlvaUpdateClass:%s Updating class for V%02u", shouldUpdate ? "" : " NOT", varNum);
        JITDUMP(" from (%p) %s%s", dspPtr(varDsc->lvClassHnd), eeGetClassName(varDsc->lvClassHnd),
                varDsc->lvClassIsExact ? " [exact]" : "");
        JITDUMP(" to (%p) %s%s\n", dspPtr(clsHnd), eeGetClassName(clsHnd), isExact ? " [exact]" : "");
    }
#endif // DEBUG

    if (shouldUpdate)
    {
        varDsc->lvClassHnd     = clsHnd;
        varDsc->lvClassIsExact = isExact;

#if DEBUG
        // Note we've modified the type...
        varDsc->lvClassInfoUpdated = true;
#endif // DEBUG
    }

    return;
}

//------------------------------------------------------------------------
// lvaUpdateClass: Uupdate class information for a local var from a tree
//  or stack type
//
// Arguments:
//    varNum -- number of the variable. Must be a single def local
//    tree  -- tree establishing the variable's value
//    stackHnd -- handle for the type from the evaluation stack
//
// Notes:
//    Preferentially uses the tree's type, when available. Since not all
//    tree kinds can track ref types, the stack type is used as a
//    fallback.

void Compiler::lvaUpdateClass(unsigned varNum, GenTree* tree, CORINFO_CLASS_HANDLE stackHnd)
{
    bool                 isExact   = false;
    bool                 isNonNull = false;
    CORINFO_CLASS_HANDLE clsHnd    = gtGetClassHandle(tree, &isExact, &isNonNull);

    if (clsHnd != nullptr)
    {
        lvaUpdateClass(varNum, clsHnd, isExact);
    }
    else if (stackHnd != nullptr)
    {
        lvaUpdateClass(varNum, stackHnd);
    }
}

//------------------------------------------------------------------------
// lvaLclSize: returns size of a local variable, in bytes
//
// Arguments:
//    varNum -- variable to query
//
// Returns:
//    Number of bytes needed on the frame for such a local.

unsigned Compiler::lvaLclSize(unsigned varNum)
{
    assert(varNum < lvaCount);

    var_types varType = lvaTable[varNum].TypeGet();

    switch (varType)
    {
        case TYP_STRUCT:
        case TYP_BLK:
            return lvaTable[varNum].lvSize();

        case TYP_LCLBLK:
#if FEATURE_FIXED_OUT_ARGS
            // Note that this operation performs a read of a PhasedVar
            noway_assert(varNum == lvaOutgoingArgSpaceVar);
            return lvaOutgoingArgSpaceSize;
#else // FEATURE_FIXED_OUT_ARGS
            assert(!"Unknown size");
            NO_WAY("Target doesn't support TYP_LCLBLK");

#endif // FEATURE_FIXED_OUT_ARGS

        default: // This must be a primitive var. Fall out of switch statement
            break;
    }
#ifdef TARGET_64BIT
    // We only need this Quirk for TARGET_64BIT
    if (lvaTable[varNum].lvQuirkToLong)
    {
        noway_assert(lvaTable[varNum].IsAddressExposed());
        return genTypeStSz(TYP_LONG) * sizeof(int); // return 8  (2 * 4)
    }
#endif
    return genTypeStSz(varType) * sizeof(int);
}

//
// Return the exact width of local variable "varNum" -- the number of bytes
// you'd need to copy in order to overwrite the value.
//
unsigned Compiler::lvaLclExactSize(unsigned varNum)
{
    assert(varNum < lvaCount);

    var_types varType = lvaTable[varNum].TypeGet();

    switch (varType)
    {
        case TYP_STRUCT:
        case TYP_BLK:
            return lvaTable[varNum].lvExactSize;

        case TYP_LCLBLK:
#if FEATURE_FIXED_OUT_ARGS
            // Note that this operation performs a read of a PhasedVar
            noway_assert(lvaOutgoingArgSpaceSize >= 0);
            noway_assert(varNum == lvaOutgoingArgSpaceVar);
            return lvaOutgoingArgSpaceSize;

#else // FEATURE_FIXED_OUT_ARGS
            assert(!"Unknown size");
            NO_WAY("Target doesn't support TYP_LCLBLK");

#endif // FEATURE_FIXED_OUT_ARGS

        default: // This must be a primitive var. Fall out of switch statement
            break;
    }

    return genTypeSize(varType);
}

// getCalledCount -- get the value used to normalized weights for this method
//  if we don't have profile data then getCalledCount will return BB_UNITY_WEIGHT (100)
//  otherwise it returns the number of times that profile data says the method was called.
//
// static
weight_t BasicBlock::getCalledCount(Compiler* comp)
{
    // when we don't have profile data then fgCalledCount will be BB_UNITY_WEIGHT (100)
    weight_t calledCount = comp->fgCalledCount;

    // If we haven't yet reach the place where we setup fgCalledCount it could still be zero
    // so return a reasonable value to use until we set it.
    //
    if (calledCount == 0)
    {
        if (comp->fgIsUsingProfileWeights())
        {
            // When we use profile data block counts we have exact counts,
            // not multiples of BB_UNITY_WEIGHT (100)
            calledCount = 1;
        }
        else
        {
            calledCount = comp->fgFirstBB->bbWeight;

            if (calledCount == 0)
            {
                calledCount = BB_UNITY_WEIGHT;
            }
        }
    }
    return calledCount;
}

// getBBWeight -- get the normalized weight of this block
weight_t BasicBlock::getBBWeight(Compiler* comp)
{
    if (this->bbWeight == BB_ZERO_WEIGHT)
    {
        return BB_ZERO_WEIGHT;
    }
    else
    {
        weight_t calledCount = getCalledCount(comp);

        // Normalize the bbWeights by multiplying by BB_UNITY_WEIGHT and dividing by the calledCount.
        //
        weight_t fullResult = this->bbWeight * BB_UNITY_WEIGHT / calledCount;

        return fullResult;
    }
}

// LclVarDsc "less" comparer used to compare the weight of two locals, when optimizing for small code.
class LclVarDsc_SmallCode_Less
{
    const LclVarDsc* m_lvaTable;
    INDEBUG(unsigned m_lvaCount;)

public:
    LclVarDsc_SmallCode_Less(const LclVarDsc* lvaTable DEBUGARG(unsigned lvaCount))
        : m_lvaTable(lvaTable)
#ifdef DEBUG
        , m_lvaCount(lvaCount)
#endif
    {
    }

    bool operator()(unsigned n1, unsigned n2)
    {
        assert(n1 < m_lvaCount);
        assert(n2 < m_lvaCount);

        const LclVarDsc* dsc1 = &m_lvaTable[n1];
        const LclVarDsc* dsc2 = &m_lvaTable[n2];

        // We should not be sorting untracked variables
        assert(dsc1->lvTracked);
        assert(dsc2->lvTracked);
        // We should not be sorting after registers have been allocated
        assert(!dsc1->lvRegister);
        assert(!dsc2->lvRegister);

        unsigned weight1 = dsc1->lvRefCnt();
        unsigned weight2 = dsc2->lvRefCnt();

#ifndef TARGET_ARM
        // ARM-TODO: this was disabled for ARM under !FEATURE_FP_REGALLOC; it was probably a left-over from
        // legacy backend. It should be enabled and verified.

        // Force integer candidates to sort above float candidates.
        const bool isFloat1 = isFloatRegType(dsc1->lvType);
        const bool isFloat2 = isFloatRegType(dsc2->lvType);

        if (isFloat1 != isFloat2)
        {
            if ((weight2 != 0) && isFloat1)
            {
                return false;
            }

            if ((weight1 != 0) && isFloat2)
            {
                return true;
            }
        }
#endif

        if (weight1 != weight2)
        {
            return weight1 > weight2;
        }

        // If the weighted ref counts are different then use their difference.
        if (dsc1->lvRefCntWtd() != dsc2->lvRefCntWtd())
        {
            return dsc1->lvRefCntWtd() > dsc2->lvRefCntWtd();
        }

        // We have equal ref counts and weighted ref counts.
        // Break the tie by:
        //   - Increasing the weight by 2   if we are a register arg.
        //   - Increasing the weight by 0.5 if we are a GC type.
        //
        // Review: seems odd that this is mixing counts and weights.

        if (weight1 != 0)
        {
            if (dsc1->lvIsRegArg)
            {
                weight1 += 2 * BB_UNITY_WEIGHT_UNSIGNED;
            }

            if (varTypeIsGC(dsc1->TypeGet()))
            {
                weight1 += BB_UNITY_WEIGHT_UNSIGNED / 2;
            }
        }

        if (weight2 != 0)
        {
            if (dsc2->lvIsRegArg)
            {
                weight2 += 2 * BB_UNITY_WEIGHT_UNSIGNED;
            }

            if (varTypeIsGC(dsc2->TypeGet()))
            {
                weight2 += BB_UNITY_WEIGHT_UNSIGNED / 2;
            }
        }

        if (weight1 != weight2)
        {
            return weight1 > weight2;
        }

        // To achieve a stable sort we use the LclNum (by way of the pointer address).
        return dsc1 < dsc2;
    }
};

// LclVarDsc "less" comparer used to compare the weight of two locals, when optimizing for blended code.
class LclVarDsc_BlendedCode_Less
{
    const LclVarDsc* m_lvaTable;
    INDEBUG(unsigned m_lvaCount;)

public:
    LclVarDsc_BlendedCode_Less(const LclVarDsc* lvaTable DEBUGARG(unsigned lvaCount))
        : m_lvaTable(lvaTable)
#ifdef DEBUG
        , m_lvaCount(lvaCount)
#endif
    {
    }

    bool operator()(unsigned n1, unsigned n2)
    {
        assert(n1 < m_lvaCount);
        assert(n2 < m_lvaCount);

        const LclVarDsc* dsc1 = &m_lvaTable[n1];
        const LclVarDsc* dsc2 = &m_lvaTable[n2];

        // We should not be sorting untracked variables
        assert(dsc1->lvTracked);
        assert(dsc2->lvTracked);
        // We should not be sorting after registers have been allocated
        assert(!dsc1->lvRegister);
        assert(!dsc2->lvRegister);

        weight_t weight1 = dsc1->lvRefCntWtd();
        weight_t weight2 = dsc2->lvRefCntWtd();

#ifndef TARGET_ARM
        // ARM-TODO: this was disabled for ARM under !FEATURE_FP_REGALLOC; it was probably a left-over from
        // legacy backend. It should be enabled and verified.

        // Force integer candidates to sort above float candidates.
        const bool isFloat1 = isFloatRegType(dsc1->lvType);
        const bool isFloat2 = isFloatRegType(dsc2->lvType);

        if (isFloat1 != isFloat2)
        {
            if (!Compiler::fgProfileWeightsEqual(weight2, 0) && isFloat1)
            {
                return false;
            }

            if (!Compiler::fgProfileWeightsEqual(weight1, 0) && isFloat2)
            {
                return true;
            }
        }
#endif

        if (!Compiler::fgProfileWeightsEqual(weight1, 0) && dsc1->lvIsRegArg)
        {
            weight1 += 2 * BB_UNITY_WEIGHT;
        }

        if (!Compiler::fgProfileWeightsEqual(weight2, 0) && dsc2->lvIsRegArg)
        {
            weight2 += 2 * BB_UNITY_WEIGHT;
        }

        if (!Compiler::fgProfileWeightsEqual(weight1, weight2))
        {
            return weight1 > weight2;
        }

        // If the weighted ref counts are different then try the unweighted ref counts.
        if (dsc1->lvRefCnt() != dsc2->lvRefCnt())
        {
            return dsc1->lvRefCnt() > dsc2->lvRefCnt();
        }

        // If one is a GC type and the other is not the GC type wins.
        if (varTypeIsGC(dsc1->TypeGet()) != varTypeIsGC(dsc2->TypeGet()))
        {
            return varTypeIsGC(dsc1->TypeGet());
        }

        // To achieve a stable sort we use the LclNum (by way of the pointer address).
        return dsc1 < dsc2;
    }
};

/*****************************************************************************
 *
 *  Sort the local variable table by refcount and assign tracking indices.
 */

void Compiler::lvaSortByRefCount()
{
    lvaTrackedCount             = 0;
    lvaTrackedCountInSizeTUnits = 0;

#ifdef DEBUG
    VarSetOps::AssignNoCopy(this, lvaTrackedVars, VarSetOps::MakeEmpty(this));
#endif

    if (lvaCount == 0)
    {
        return;
    }

    /* We'll sort the variables by ref count - allocate the sorted table */

    if (lvaTrackedToVarNumSize < lvaCount)
    {
        lvaTrackedToVarNumSize = lvaCount;
        lvaTrackedToVarNum     = new (getAllocator(CMK_LvaTable)) unsigned[lvaTrackedToVarNumSize];
    }

    unsigned  trackedCount = 0;
    unsigned* tracked      = lvaTrackedToVarNum;

    // Fill in the table used for sorting

    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        LclVarDsc* varDsc = lvaGetDesc(lclNum);

        // Start by assuming that the variable will be tracked.
        varDsc->lvTracked = 1;

        if (varDsc->lvRefCnt() == 0)
        {
            // Zero ref count, make this untracked.
            varDsc->lvTracked = 0;
            varDsc->setLvRefCntWtd(0);
        }

#if !defined(TARGET_64BIT)
        if (varTypeIsLong(varDsc) && varDsc->lvPromoted)
        {
            varDsc->lvTracked = 0;
        }
#endif // !defined(TARGET_64BIT)

        // Variables that are address-exposed, and all struct locals, are never enregistered, or tracked.
        // (The struct may be promoted, and its field variables enregistered/tracked, or the VM may "normalize"
        // its type so that its not seen by the JIT as a struct.)
        // Pinned variables may not be tracked (a condition of the GCInfo representation)
        // or enregistered, on x86 -- it is believed that we can enregister pinned (more properly, "pinning")
        // references when using the general GC encoding.
        if (varDsc->IsAddressExposed())
        {
            varDsc->lvTracked = 0;
            assert(varDsc->lvType != TYP_STRUCT ||
                   varDsc->lvDoNotEnregister); // For structs, should have set this when we set m_addrExposed.
        }
        if (varTypeIsStruct(varDsc))
        {
            // Promoted structs will never be considered for enregistration anyway,
            // and the DoNotEnregister flag was used to indicate whether promotion was
            // independent or dependent.
            if (varDsc->lvPromoted)
            {
                varDsc->lvTracked = 0;
            }
            else if (!varDsc->IsEnregisterableType())
            {
                lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::NotRegSizeStruct));
            }
            else if (varDsc->lvType == TYP_STRUCT)
            {
                if (!varDsc->lvRegStruct && !compEnregStructLocals())
                {
                    lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::DontEnregStructs));
                }
                else if (varDsc->lvIsMultiRegArgOrRet())
                {
                    // Prolog and return generators do not support SIMD<->general register moves.
                    lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::IsStructArg));
                }
#if defined(TARGET_ARM)
                else if (varDsc->lvIsParam)
                {
                    // On arm we prespill all struct args,
                    // TODO-Arm-CQ: keep them in registers, it will need a fix
                    // to "On the ARM we will spill any incoming struct args" logic in codegencommon.
                    lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::IsStructArg));
                }
#endif // TARGET_ARM
            }
        }
        if (varDsc->lvIsStructField && (lvaGetParentPromotionType(lclNum) != PROMOTION_TYPE_INDEPENDENT))
        {
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::DepField));
        }
        if (varDsc->lvPinned)
        {
            varDsc->lvTracked = 0;
#ifdef JIT32_GCENCODER
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::PinningRef));
#endif
        }
        if (opts.MinOpts() && !JitConfig.JitMinOptsTrackGCrefs() && varTypeIsGC(varDsc->TypeGet()))
        {
            varDsc->lvTracked = 0;
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::MinOptsGC));
        }
        if (!compEnregLocals())
        {
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::NoRegVars));
        }
#if defined(JIT32_GCENCODER) && defined(FEATURE_EH_FUNCLETS)
        if (lvaIsOriginalThisArg(lclNum) && (info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0)
        {
            // For x86/Linux, we need to track "this".
            // However we cannot have it in tracked variables, so we set "this" pointer always untracked
            varDsc->lvTracked = 0;
        }
#endif

        //  Are we not optimizing and we have exception handlers?
        //   if so mark all args and locals "do not enregister".
        //
        if (opts.MinOpts() && compHndBBtabCount > 0)
        {
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::LiveInOutOfHandler));
        }
        else
        {
            var_types type = genActualType(varDsc->TypeGet());

            switch (type)
            {
                case TYP_FLOAT:
                case TYP_DOUBLE:
                case TYP_INT:
                case TYP_LONG:
                case TYP_REF:
                case TYP_BYREF:
#ifdef FEATURE_SIMD
                case TYP_SIMD8:
                case TYP_SIMD12:
                case TYP_SIMD16:
                case TYP_SIMD32:
#endif // FEATURE_SIMD
                case TYP_STRUCT:
                    break;

                case TYP_UNDEF:
                case TYP_UNKNOWN:
                    noway_assert(!"lvType not set correctly");
                    varDsc->lvType = TYP_INT;

                    FALLTHROUGH;

                default:
                    varDsc->lvTracked = 0;
            }
        }

        if (varDsc->lvTracked)
        {
            tracked[trackedCount++] = lclNum;
        }
    }

    // Now sort the tracked variable table by ref-count
    if (compCodeOpt() == SMALL_CODE)
    {
        jitstd::sort(tracked, tracked + trackedCount, LclVarDsc_SmallCode_Less(lvaTable DEBUGARG(lvaCount)));
    }
    else
    {
        jitstd::sort(tracked, tracked + trackedCount, LclVarDsc_BlendedCode_Less(lvaTable DEBUGARG(lvaCount)));
    }

    lvaTrackedCount = min((unsigned)JitConfig.JitMaxLocalsToTrack(), trackedCount);

    JITDUMP("Tracked variable (%u out of %u) table:\n", lvaTrackedCount, lvaCount);

    // Assign indices to all the variables we've decided to track
    for (unsigned varIndex = 0; varIndex < lvaTrackedCount; varIndex++)
    {
        LclVarDsc* varDsc = lvaGetDesc(tracked[varIndex]);
        assert(varDsc->lvTracked);
        varDsc->lvVarIndex = static_cast<unsigned short>(varIndex);

        INDEBUG(if (verbose) { gtDispLclVar(tracked[varIndex]); })
        JITDUMP(" [%6s]: refCnt = %4u, refCntWtd = %6s\n", varTypeName(varDsc->TypeGet()), varDsc->lvRefCnt(),
                refCntWtd2str(varDsc->lvRefCntWtd()));
    }

    JITDUMP("\n");

    // Mark all variables past the first 'lclMAX_TRACKED' as untracked
    for (unsigned varIndex = lvaTrackedCount; varIndex < trackedCount; varIndex++)
    {
        LclVarDsc* varDsc = lvaGetDesc(tracked[varIndex]);
        assert(varDsc->lvTracked);
        varDsc->lvTracked = 0;
    }

    // We have a new epoch, and also cache the tracked var count in terms of size_t's sufficient to hold that many bits.
    lvaCurEpoch++;
    lvaTrackedCountInSizeTUnits =
        roundUp((unsigned)lvaTrackedCount, (unsigned)(sizeof(size_t) * 8)) / unsigned(sizeof(size_t) * 8);

#ifdef DEBUG
    VarSetOps::AssignNoCopy(this, lvaTrackedVars, VarSetOps::MakeFull(this));
#endif
}

/*****************************************************************************
 *
 *  This is called by lvaMarkLclRefs to disqualify a variable from being
 *  considered by optAddCopies()
 */
void LclVarDsc::lvaDisqualifyVar()
{
    this->lvDisqualify = true;
    this->lvSingleDef  = false;
    this->lvDefStmt    = nullptr;
}

#ifdef FEATURE_SIMD
var_types LclVarDsc::GetSimdBaseType() const
{
    CorInfoType simdBaseJitType = GetSimdBaseJitType();

    if (simdBaseJitType == CORINFO_TYPE_UNDEF)
    {
        return TYP_UNKNOWN;
    }
    return JitType2PreciseVarType(simdBaseJitType);
}
#endif // FEATURE_SIMD

unsigned LclVarDsc::lvSize() const // Size needed for storage representation. Only used for structs or TYP_BLK.
{
    // TODO-Review: Sometimes we get called on ARM with HFA struct variables that have been promoted,
    // where the struct itself is no longer used because all access is via its member fields.
    // When that happens, the struct is marked as unused and its type has been changed to
    // TYP_INT (to keep the GC tracking code from looking at it).
    // See Compiler::raAssignVars() for details. For example:
    //      N002 (  4,  3) [00EA067C] -------------               return    struct $346
    //      N001 (  3,  2) [00EA0628] -------------                  lclVar    struct(U) V03 loc2
    //                                                                        float  V03.f1 (offs=0x00) -> V12 tmp7
    //                                                                        f8 (last use) (last use) $345
    // Here, the "struct(U)" shows that the "V03 loc2" variable is unused. Not shown is that V03
    // is now TYP_INT in the local variable table. It's not really unused, because it's in the tree.

    assert(varTypeIsStruct(lvType) || (lvType == TYP_BLK) || (lvPromoted && lvUnusedStruct));

    if (lvIsParam)
    {
        assert(varTypeIsStruct(lvType));
        const bool     isFloatHfa       = (lvIsHfa() && (GetHfaType() == TYP_FLOAT));
        const unsigned argSizeAlignment = Compiler::eeGetArgSizeAlignment(lvType, isFloatHfa);
        return roundUp(lvExactSize, argSizeAlignment);
    }

#if defined(FEATURE_SIMD) && !defined(TARGET_64BIT)
    // For 32-bit architectures, we make local variable SIMD12 types 16 bytes instead of just 12. We can't do
    // this for arguments, which must be passed according the defined ABI. We don't want to do this for
    // dependently promoted struct fields, but we don't know that here. See lvaMapSimd12ToSimd16().
    // (Note that for 64-bits, we are already rounding up to 16.)
    if (lvType == TYP_SIMD12)
    {
        assert(!lvIsParam);
        assert(lvExactSize == 12);
        return 16;
    }
#endif // defined(FEATURE_SIMD) && !defined(TARGET_64BIT)

    return roundUp(lvExactSize, TARGET_POINTER_SIZE);
}

/**********************************************************************************
* Get stack size of the varDsc.
*/
size_t LclVarDsc::lvArgStackSize() const
{
    // Make sure this will have a stack size
    assert(!this->lvIsRegArg);

    size_t stackSize = 0;
    if (varTypeIsStruct(this))
    {
#if defined(WINDOWS_AMD64_ABI)
        // Structs are either passed by reference or can be passed by value using one pointer
        stackSize = TARGET_POINTER_SIZE;
#elif defined(TARGET_ARM64) || defined(UNIX_AMD64_ABI) || defined(TARGET_LOONGARCH64)
        // lvSize performs a roundup.
        stackSize = this->lvSize();

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
        if ((stackSize > TARGET_POINTER_SIZE * 2) && (!this->lvIsHfa()))
        {
            // If the size is greater than 16 bytes then it will
            // be passed by reference.
            stackSize = TARGET_POINTER_SIZE;
        }
#endif // defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

#else // !TARGET_ARM64 !WINDOWS_AMD64_ABI !UNIX_AMD64_ABI !TARGET_LOONGARCH64

        NYI("Unsupported target.");
        unreached();

#endif //  !TARGET_ARM64 !WINDOWS_AMD64_ABI !UNIX_AMD64_ABI
    }
    else
    {
        stackSize = TARGET_POINTER_SIZE;
    }

    return stackSize;
}

//------------------------------------------------------------------------
// GetRegisterType: Determine register type for this local var.
//
// Arguments:
//    tree - node that uses the local, its type is checked first.
//
// Return Value:
//    TYP_UNDEF if the layout is not enregistrable, the register type otherwise.
//
var_types LclVarDsc::GetRegisterType(const GenTreeLclVarCommon* tree) const
{
    var_types targetType = tree->gtType;
    var_types lclVarType = TypeGet();

    if (targetType == TYP_STRUCT)
    {
        if (lclVarType == TYP_STRUCT)
        {
            assert(!tree->OperIsLocalField() && "do not expect struct local fields.");
            lclVarType = GetLayout()->GetRegisterType();
        }
        targetType = lclVarType;
    }

#ifdef DEBUG
    if ((targetType != TYP_UNDEF) && tree->OperIs(GT_STORE_LCL_VAR) && lvNormalizeOnStore())
    {
        const bool phiStore = (tree->gtGetOp1()->OperIsNonPhiLocal() == false);
        // Ensure that the lclVar node is typed correctly,
        // does not apply to phi-stores because they do not produce code in the merge block.
        assert(phiStore || targetType == genActualType(lclVarType));
    }
#endif
    return targetType;
}

//------------------------------------------------------------------------
// GetRegisterType: Determine register type for this local var.
//
// Return Value:
//    TYP_UNDEF if the layout is not enregistrable, the register type otherwise.
//
var_types LclVarDsc::GetRegisterType() const
{
    if (TypeGet() != TYP_STRUCT)
    {
#if !defined(TARGET_64BIT)
        if (TypeGet() == TYP_LONG)
        {
            return TYP_UNDEF;
        }
#endif
        return TypeGet();
    }
    assert(m_layout != nullptr);
    return m_layout->GetRegisterType();
}

//------------------------------------------------------------------------
// GetStackSlotHomeType:
//   Get the canonical type of the stack slot that this enregistrable local is
//   using when stored on the stack.
//
// Return Value:
//   TYP_UNDEF if the layout is not enregistrable. Otherwise returns the type
//    of the stack slot home for the local.
//
// Remarks:
//   This function always returns a canonical type: for all 4-byte types
//   (structs, floats, ints) it will return TYP_INT. It is meant to be used
//   when moving locals between register and stack. Because of this the
//   returned type is usually at least one 4-byte stack slot. However, there
//   are certain exceptions for promoted fields in OSR methods (that may refer
//   back to the original frame) and due to macOS arm64 where subsequent small
//   parameters can be packed into the same stack slot.
//
var_types LclVarDsc::GetStackSlotHomeType() const
{
    if (varTypeIsSmall(TypeGet()))
    {
        if (compMacOsArm64Abi() && lvIsParam && !lvIsRegArg)
        {
            // Allocated by caller and potentially only takes up a small slot
            return GetRegisterType();
        }

        if (lvIsOSRLocal && lvIsStructField)
        {
#if defined(TARGET_X86)
            // Revisit when we support OSR on x86
            unreached();
#else
            return GetRegisterType();
#endif
        }
    }

    return genActualType(GetRegisterType());
}

//----------------------------------------------------------------------------------------------
// CanBeReplacedWithItsField: check if a whole struct reference could be replaced by a field.
//
// Arguments:
//    comp - the compiler instance;
//
// Return Value:
//    true if that can be replaced, false otherwise.
//
// Notes:
//    The replacement can be made only for independently promoted structs
//    with 1 field without holes.
//
bool LclVarDsc::CanBeReplacedWithItsField(Compiler* comp) const
{
    if (!lvPromoted)
    {
        return false;
    }

    if (comp->lvaGetPromotionType(this) != Compiler::PROMOTION_TYPE_INDEPENDENT)
    {
        return false;
    }
    if (lvFieldCnt != 1)
    {
        return false;
    }
    if (lvContainsHoles)
    {
        return false;
    }

#if defined(FEATURE_SIMD)
    // If we return `struct A { SIMD16 a; }` we split the struct into several fields.
    // In order to do that we have to have its field `a` in memory. Right now lowering cannot
    // handle RETURN struct(multiple registers)->SIMD16(one register), but it can be improved.
    LclVarDsc* fieldDsc = comp->lvaGetDesc(lvFieldLclStart);
    if (varTypeIsSIMD(fieldDsc))
    {
        return false;
    }
#endif // FEATURE_SIMD

    return true;
}

//------------------------------------------------------------------------
// lvaMarkLclRefs: increment local var references counts and more
//
// Arguments:
//     tree - some node in a tree
//     block - block that the tree node belongs to
//     stmt - stmt that the tree node belongs to
//     isRecompute - true if we should just recompute counts
//
// Notes:
//     Invoked via the MarkLocalVarsVisitor
//
//     Primarily increments the regular and weighted local var ref
//     counts for any local referred to directly by tree.
//
//     Also:
//
//     Accounts for implicit references to frame list root for
//     pinvokes that will be expanded later.
//
//     Determines if locals of TYP_BOOL can safely be considered
//     to hold only 0 or 1 or may have a broader range of true values.
//
//     Does some setup work for assertion prop, noting locals that are
//     eligible for assertion prop, single defs, and tracking which blocks
//     hold uses.
//
//     Looks for uses of generic context and sets lvaGenericsContextInUse.
//
//     In checked builds:
//
//     Verifies that local accesses are consistenly typed.
//     Verifies that casts remain in bounds.

void Compiler::lvaMarkLclRefs(GenTree* tree, BasicBlock* block, Statement* stmt, bool isRecompute)
{
    const weight_t weight = block->getBBWeight(this);

    /* Is this a call to unmanaged code ? */
    if (tree->IsCall() && compMethodRequiresPInvokeFrame())
    {
        assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers())
        {
            /* Get the special variable descriptor */
            LclVarDsc* varDsc = lvaGetDesc(info.compLvFrameListRoot);

            /* Increment the ref counts twice */
            varDsc->incRefCnts(weight, this);
            varDsc->incRefCnts(weight, this);
        }
    }

    if (!isRecompute)
    {
        /* Is this an assigment? */

        if (tree->OperIs(GT_ASG))
        {
            GenTree* op1 = tree->AsOp()->gtOp1;
            GenTree* op2 = tree->AsOp()->gtOp2;

            /* Is this an assignment to a local variable? */

            if (op1->gtOper == GT_LCL_VAR && op2->gtType != TYP_BOOL)
            {
                /* Only simple assignments allowed for booleans */

                if (tree->gtOper != GT_ASG)
                {
                    goto NOT_BOOL;
                }

                /* Is the RHS clearly a boolean value? */

                switch (op2->gtOper)
                {
                    unsigned lclNum;

                    case GT_CNS_INT:

                        if (op2->AsIntCon()->gtIconVal == 0)
                        {
                            break;
                        }
                        if (op2->AsIntCon()->gtIconVal == 1)
                        {
                            break;
                        }

                        // Not 0 or 1, fall through ....
                        FALLTHROUGH;

                    default:

                        if (op2->OperIsCompare())
                        {
                            break;
                        }

                    NOT_BOOL:

                        lclNum = op1->AsLclVarCommon()->GetLclNum();
                        noway_assert(lclNum < lvaCount);

                        lvaTable[lclNum].lvIsBoolean = false;
                        break;
                }
            }
        }
    }

    if (tree->OperIsLocalAddr())
    {
        LclVarDsc* varDsc = lvaGetDesc(tree->AsLclVarCommon());
        assert(varDsc->IsAddressExposed());
        varDsc->incRefCnts(weight, this);
        return;
    }

    if ((tree->gtOper != GT_LCL_VAR) && (tree->gtOper != GT_LCL_FLD))
    {
        return;
    }

    /* This must be a local variable reference */

    // See if this is a generics context use.
    if ((tree->gtFlags & GTF_VAR_CONTEXT) != 0)
    {
        assert(tree->OperIs(GT_LCL_VAR));
        if (!lvaGenericsContextInUse)
        {
            JITDUMP("-- generic context in use at [%06u]\n", dspTreeID(tree));
            lvaGenericsContextInUse = true;
        }
    }

    assert((tree->gtOper == GT_LCL_VAR) || (tree->gtOper == GT_LCL_FLD));
    unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();

    LclVarDsc* varDsc = lvaGetDesc(lclNum);

    /* Increment the reference counts */

    varDsc->incRefCnts(weight, this);

#ifdef DEBUG
    if (varDsc->lvIsStructField)
    {
        // If ref count was increased for struct field, ensure that the
        // parent struct is still promoted.
        LclVarDsc* parentStruct = lvaGetDesc(varDsc->lvParentLcl);
        assert(!parentStruct->lvUndoneStructPromotion);
    }
#endif

    if (!isRecompute)
    {
        if (lvaVarAddrExposed(lclNum))
        {
            varDsc->lvIsBoolean = false;
        }

        if (tree->gtOper == GT_LCL_FLD)
        {
            // variables that have uses inside a GT_LCL_FLD
            // cause problems, so we will disqualify them here
            varDsc->lvaDisqualifyVar();
            return;
        }

        if (fgDomsComputed && IsDominatedByExceptionalEntry(block))
        {
            SetVolatileHint(varDsc);
        }

        /* Record if the variable has a single def or not */

        if (!varDsc->lvDisqualify) // If this variable is already disqualified, we can skip this
        {
            if (tree->gtFlags & GTF_VAR_DEF) // Is this is a def of our variable
            {
                /*
                   If we have one of these cases:
                       1.    We have already seen a definition (i.e lvSingleDef is true)
                       2. or info.CompInitMem is true (thus this would be the second definition)
                       3. or we have an assignment inside QMARK-COLON trees
                       4. or we have an update form of assignment (i.e. +=, -=, *=)
                   Then we must disqualify this variable for use in optAddCopies()

                   Note that all parameters start out with lvSingleDef set to true
                */
                if ((varDsc->lvSingleDef == true) || (info.compInitMem == true) || (tree->gtFlags & GTF_COLON_COND) ||
                    (tree->gtFlags & GTF_VAR_USEASG))
                {
                    varDsc->lvaDisqualifyVar();
                }
                else
                {
                    varDsc->lvSingleDef = true;
                    varDsc->lvDefStmt   = stmt;
                }
            }
            else // otherwise this is a ref of our variable
            {
                if (BlockSetOps::MayBeUninit(varDsc->lvRefBlks))
                {
                    // Lazy initialization
                    BlockSetOps::AssignNoCopy(this, varDsc->lvRefBlks, BlockSetOps::MakeEmpty(this));
                }
                BlockSetOps::AddElemD(this, varDsc->lvRefBlks, block->bbNum);
            }
        }

        if (!varDsc->lvDisqualifySingleDefRegCandidate) // If this var is already disqualified, we can skip this
        {
            if (tree->gtFlags & GTF_VAR_DEF) // Is this is a def of our variable
            {
                bool bbInALoop  = (block->bbFlags & BBF_BACKWARD_JUMP) != 0;
                bool bbIsReturn = block->bbJumpKind == BBJ_RETURN;
                // TODO: Zero-inits in LSRA are created with below condition. But if filter out based on that condition
                // we filter lot of interesting variables that would benefit otherwise with EH var enregistration.
                // bool needsExplicitZeroInit = !varDsc->lvIsParam && (info.compInitMem ||
                // varTypeIsGC(varDsc->TypeGet()));
                bool needsExplicitZeroInit = fgVarNeedsExplicitZeroInit(lclNum, bbInALoop, bbIsReturn);

                if (varDsc->lvSingleDefRegCandidate || needsExplicitZeroInit)
                {
#ifdef DEBUG
                    if (needsExplicitZeroInit)
                    {
                        varDsc->lvSingleDefDisqualifyReason = 'Z';
                        JITDUMP("V%02u needs explicit zero init. Disqualified as a single-def register candidate.\n",
                                lclNum);
                    }
                    else
                    {
                        varDsc->lvSingleDefDisqualifyReason = 'M';
                        JITDUMP("V%02u has multiple definitions. Disqualified as a single-def register candidate.\n",
                                lclNum);
                    }

#endif // DEBUG
                    varDsc->lvSingleDefRegCandidate           = false;
                    varDsc->lvDisqualifySingleDefRegCandidate = true;
                }
                else if (!varDsc->lvDoNotEnregister)
                {
                    // Variables can be marked as DoNotEngister in earlier stages like LocalAddressVisitor.
                    // No need to track them for single-def.
                    CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                    // TODO-CQ: If the varType needs partial callee save, conservatively do not enregister
                    // such variable. In future, need to enable enregisteration for such variables.
                    if (!varTypeNeedsPartialCalleeSave(varDsc->GetRegisterType()))
#endif
                    {
                        varDsc->lvSingleDefRegCandidate = true;
                        JITDUMP("Marking EH Var V%02u as a register candidate.\n", lclNum);
                    }
                }
            }
        }

        bool allowStructs = false;
#ifdef UNIX_AMD64_ABI
        // On System V the type of the var could be a struct type.
        allowStructs = varTypeIsStruct(varDsc);
#endif // UNIX_AMD64_ABI

        /* Variables must be used as the same type throughout the method */
        noway_assert(varDsc->lvType == TYP_UNDEF || tree->gtType == TYP_UNKNOWN || allowStructs ||
                     genActualType(varDsc->TypeGet()) == genActualType(tree->gtType) ||
                     (tree->gtType == TYP_BYREF && varDsc->TypeGet() == TYP_I_IMPL) ||
                     (tree->gtType == TYP_I_IMPL && varDsc->TypeGet() == TYP_BYREF) || (tree->gtFlags & GTF_VAR_CAST) ||
                     (varTypeIsFloating(varDsc) && varTypeIsFloating(tree)) ||
                     (varTypeIsStruct(varDsc) == varTypeIsStruct(tree)));

        /* Remember the type of the reference */

        if (tree->gtType == TYP_UNKNOWN || varDsc->lvType == TYP_UNDEF)
        {
            varDsc->lvType = tree->gtType;
            noway_assert(genActualType(varDsc->TypeGet()) == tree->gtType); // no truncation
        }

#ifdef DEBUG
        if (tree->gtFlags & GTF_VAR_CAST)
        {
            // it should never be bigger than the variable slot

            // Trees don't store the full information about structs
            // so we can't check them.
            if (tree->TypeGet() != TYP_STRUCT)
            {
                unsigned treeSize = genTypeSize(tree->TypeGet());
                unsigned varSize  = genTypeSize(varDsc->TypeGet());
                if (varDsc->TypeGet() == TYP_STRUCT)
                {
                    varSize = varDsc->lvSize();
                }

                assert(treeSize <= varSize);
            }
        }
#endif
    }
}

//------------------------------------------------------------------------
// IsDominatedByExceptionalEntry: Check is the block dominated by an exception entry block.
//
// Arguments:
//    block - the checking block.
//
bool Compiler::IsDominatedByExceptionalEntry(BasicBlock* block)
{
    assert(fgDomsComputed);
    return block->IsDominatedByExceptionalEntryFlag();
}

//------------------------------------------------------------------------
// SetVolatileHint: Set a local var's volatile hint.
//
// Arguments:
//    varDsc - the local variable that needs the hint.
//
void Compiler::SetVolatileHint(LclVarDsc* varDsc)
{
    varDsc->lvVolatileHint = true;
}

//------------------------------------------------------------------------
// lvaMarkLocalVars: update local var ref counts for IR in a basic block
//
// Arguments:
//    block - the block in question
//    isRecompute - true if counts are being recomputed
//
// Notes:
//    Invokes lvaMarkLclRefs on each tree node for each
//    statement in the block.

void Compiler::lvaMarkLocalVars(BasicBlock* block, bool isRecompute)
{
    class MarkLocalVarsVisitor final : public GenTreeVisitor<MarkLocalVarsVisitor>
    {
    private:
        BasicBlock* m_block;
        Statement*  m_stmt;
        bool        m_isRecompute;

    public:
        enum
        {
            DoPreOrder = true,
        };

        MarkLocalVarsVisitor(Compiler* compiler, BasicBlock* block, Statement* stmt, bool isRecompute)
            : GenTreeVisitor<MarkLocalVarsVisitor>(compiler), m_block(block), m_stmt(stmt), m_isRecompute(isRecompute)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            // TODO: Stop passing isRecompute once we are sure that this assert is never hit.
            assert(!m_isRecompute);
            m_compiler->lvaMarkLclRefs(*use, m_block, m_stmt, m_isRecompute);
            return WALK_CONTINUE;
        }
    };

    JITDUMP("\n*** %s local variables in block " FMT_BB " (weight=%s)\n", isRecompute ? "recomputing" : "marking",
            block->bbNum, refCntWtd2str(block->getBBWeight(this)));

    for (Statement* const stmt : block->NonPhiStatements())
    {
        MarkLocalVarsVisitor visitor(this, block, stmt, isRecompute);
        DISPSTMT(stmt);
        visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    }
}

//------------------------------------------------------------------------
// lvaMarkLocalVars: enable normal ref counting, compute initial counts, sort locals table
//
// Notes:
//    Now behaves differently in minopts / debug. Instead of actually inspecting
//    the IR and counting references, the jit assumes all locals are referenced
//    and does not sort the locals table.
//
//    Also, when optimizing, lays the groundwork for assertion prop and more.
//    See details in lvaMarkLclRefs.

void Compiler::lvaMarkLocalVars()
{

    JITDUMP("\n*************** In lvaMarkLocalVars()");

    // If we have direct pinvokes, verify the frame list root local was set up properly
    if (compMethodRequiresPInvokeFrame())
    {
        assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers())
        {
            noway_assert(info.compLvFrameListRoot >= info.compLocalsCount && info.compLvFrameListRoot < lvaCount);
        }
    }

#if !defined(FEATURE_EH_FUNCLETS)

    // Grab space for exception handling

    if (ehNeedsShadowSPslots())
    {
        // The first slot is reserved for ICodeManager::FixContext(ppEndRegion)
        // ie. the offset of the end-of-last-executed-filter
        unsigned slotsNeeded = 1;

        unsigned handlerNestingLevel = ehMaxHndNestingCount;

        if (opts.compDbgEnC && (handlerNestingLevel < (unsigned)MAX_EnC_HANDLER_NESTING_LEVEL))
            handlerNestingLevel = (unsigned)MAX_EnC_HANDLER_NESTING_LEVEL;

        slotsNeeded += handlerNestingLevel;

        // For a filter (which can be active at the same time as a catch/finally handler)
        slotsNeeded++;
        // For zero-termination of the shadow-Stack-pointer chain
        slotsNeeded++;

        lvaShadowSPslotsVar           = lvaGrabTempWithImplicitUse(false DEBUGARG("lvaShadowSPslotsVar"));
        LclVarDsc* shadowSPslotsVar   = lvaGetDesc(lvaShadowSPslotsVar);
        shadowSPslotsVar->lvType      = TYP_BLK;
        shadowSPslotsVar->lvExactSize = (slotsNeeded * TARGET_POINTER_SIZE);
    }

#endif // !FEATURE_EH_FUNCLETS

    // PSPSym and LocAllocSPvar are not used by the CoreRT ABI
    if (!IsTargetAbi(CORINFO_CORERT_ABI))
    {
#if defined(FEATURE_EH_FUNCLETS)
        if (ehNeedsPSPSym())
        {
            lvaPSPSym            = lvaGrabTempWithImplicitUse(false DEBUGARG("PSPSym"));
            LclVarDsc* lclPSPSym = lvaGetDesc(lvaPSPSym);
            lclPSPSym->lvType    = TYP_I_IMPL;
            lvaSetVarDoNotEnregister(lvaPSPSym DEBUGARG(DoNotEnregisterReason::VMNeedsStackAddr));
        }
#endif // FEATURE_EH_FUNCLETS

#ifdef JIT32_GCENCODER
        // LocAllocSPvar is only required by the implicit frame layout expected by the VM on x86. Whether
        // a function contains a Localloc is conveyed in the GC information, in the InfoHdrSmall.localloc
        // field. The function must have an EBP frame. Then, the VM finds the LocAllocSP slot by assuming
        // the following stack layout:
        //
        //      -- higher addresses --
        //      saved EBP                       <-- EBP points here
        //      other callee-saved registers    // InfoHdrSmall.savedRegsCountExclFP specifies this size
        //      optional GS cookie              // InfoHdrSmall.security is 1 if this exists
        //      LocAllocSP slot
        //      -- lower addresses --
        //
        // See also eetwain.cpp::GetLocallocSPOffset() and its callers.
        if (compLocallocUsed)
        {
            lvaLocAllocSPvar         = lvaGrabTempWithImplicitUse(false DEBUGARG("LocAllocSPvar"));
            LclVarDsc* locAllocSPvar = lvaGetDesc(lvaLocAllocSPvar);
            locAllocSPvar->lvType    = TYP_I_IMPL;
        }
#endif // JIT32_GCENCODER
    }

    // Ref counting is now enabled normally.
    lvaRefCountState = RCS_NORMAL;

#if defined(DEBUG)
    const bool setSlotNumbers = true;
#else
    const bool setSlotNumbers = opts.compScopeInfo && (info.compVarScopesCount > 0);
#endif // defined(DEBUG)

    const bool isRecompute = false;
    lvaComputeRefCounts(isRecompute, setSlotNumbers);

    // If we don't need precise reference counts, e.g. we're not optimizing, we're done.
    if (!PreciseRefCountsRequired())
    {
        return;
    }

    const bool reportParamTypeArg = lvaReportParamTypeArg();

    // Update bookkeeping on the generic context.
    if (lvaKeepAliveAndReportThis())
    {
        lvaGetDesc(0u)->lvImplicitlyReferenced = reportParamTypeArg;
    }
    else if (lvaReportParamTypeArg())
    {
        // We should have a context arg.
        assert(info.compTypeCtxtArg != (int)BAD_VAR_NUM);
        lvaGetDesc(info.compTypeCtxtArg)->lvImplicitlyReferenced = reportParamTypeArg;
    }

    assert(PreciseRefCountsRequired());

    // Note: optAddCopies() depends on lvaRefBlks, which is set in lvaMarkLocalVars(BasicBlock*), called above.
    optAddCopies();
}

//------------------------------------------------------------------------
// lvaComputeRefCounts: compute ref counts for locals
//
// Arguments:
//    isRecompute -- true if we just want ref counts and no other side effects;
//                   false means to also look for true boolean locals, lay
//                   groundwork for assertion prop, check type consistency, etc.
//                   See lvaMarkLclRefs for details on what else goes on.
//    setSlotNumbers -- true if local slot numbers should be assigned.
//
// Notes:
//    Some implicit references are given actual counts or weight bumps here
//    to match pre-existing behavior.
//
//    In fast-jitting modes where we don't ref count locals, this bypasses
//    actual counting, and makes all locals implicitly referenced on first
//    compute. It asserts all locals are implicitly referenced on recompute.
//
//    When optimizing we also recompute lvaGenericsContextInUse based
//    on specially flagged LCL_VAR appearances.
//
void Compiler::lvaComputeRefCounts(bool isRecompute, bool setSlotNumbers)
{
    JITDUMP("\n*** lvaComputeRefCounts ***\n");
    unsigned   lclNum = 0;
    LclVarDsc* varDsc = nullptr;

    // Fast path for minopts and debug codegen.
    //
    // On first compute: mark all locals as implicitly referenced and untracked.
    // On recompute: do nothing.
    if (!PreciseRefCountsRequired())
    {
        if (isRecompute)
        {

#if defined(DEBUG)
            // All local vars should be marked as implicitly referenced
            // and not tracked.
            for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
            {
                const bool isSpecialVarargsParam = varDsc->lvIsParam && raIsVarargsStackArg(lclNum);

                if (isSpecialVarargsParam)
                {
                    assert(varDsc->lvRefCnt() == 0);
                }
                else
                {
                    assert(varDsc->lvImplicitlyReferenced);
                }

                assert(!varDsc->lvTracked);
            }
#endif // defined (DEBUG)

            return;
        }

        // First compute.
        for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
        {
            // Using lvImplicitlyReferenced here ensures that we can't
            // accidentally make locals be unreferenced later by decrementing
            // the ref count to zero.
            //
            // If, in minopts/debug, we really want to allow locals to become
            // unreferenced later, we'll have to explicitly clear this bit.
            varDsc->setLvRefCnt(0);
            varDsc->setLvRefCntWtd(BB_ZERO_WEIGHT);

            // Special case for some varargs params ... these must
            // remain unreferenced.
            const bool isSpecialVarargsParam = varDsc->lvIsParam && raIsVarargsStackArg(lclNum);

            if (!isSpecialVarargsParam)
            {
                varDsc->lvImplicitlyReferenced = 1;
            }

            varDsc->lvTracked = 0;

            if (setSlotNumbers)
            {
                varDsc->lvSlotNum = lclNum;
            }

            // Assert that it's ok to bypass the type repair logic in lvaMarkLclRefs
            assert((varDsc->lvType != TYP_UNDEF) && (varDsc->lvType != TYP_VOID) && (varDsc->lvType != TYP_UNKNOWN));
        }

        lvaCurEpoch++;
        lvaTrackedCount             = 0;
        lvaTrackedCountInSizeTUnits = 0;
        return;
    }

    // Slower path we take when optimizing, to get accurate counts.
    //
    // First, reset all explicit ref counts and weights.
    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        varDsc->setLvRefCnt(0);
        varDsc->setLvRefCntWtd(BB_ZERO_WEIGHT);

        if (setSlotNumbers)
        {
            varDsc->lvSlotNum = lclNum;
        }

        // Set initial value for lvSingleDef for explicit and implicit
        // argument locals as they are "defined" on entry.
        // However, if we are just recomputing the ref counts, retain the value
        // that was set by past phases.
        if (!isRecompute)
        {
            varDsc->lvSingleDef             = varDsc->lvIsParam;
            varDsc->lvSingleDefRegCandidate = varDsc->lvIsParam;
        }
    }

    // Remember current state of generic context use, and prepare
    // to compute new state.
    const bool oldLvaGenericsContextInUse = lvaGenericsContextInUse;
    lvaGenericsContextInUse               = false;

    JITDUMP("\n*** lvaComputeRefCounts -- explicit counts ***\n");

    // Second, account for all explicit local variable references
    for (BasicBlock* const block : Blocks())
    {
        if (block->IsLIR())
        {
            assert(isRecompute);

            const weight_t weight = block->getBBWeight(this);
            for (GenTree* node : LIR::AsRange(block))
            {
                switch (node->OperGet())
                {
                    case GT_LCL_VAR:
                    case GT_LCL_FLD:
                    case GT_LCL_VAR_ADDR:
                    case GT_LCL_FLD_ADDR:
                    case GT_STORE_LCL_VAR:
                    case GT_STORE_LCL_FLD:
                    {
                        LclVarDsc* varDsc = lvaGetDesc(node->AsLclVarCommon());
                        // If this is an EH var, use a zero weight for defs, so that we don't
                        // count those in our heuristic for register allocation, since they always
                        // must be stored, so there's no value in enregistering them at defs; only
                        // if there are enough uses to justify it.
                        if (varDsc->lvLiveInOutOfHndlr && !varDsc->lvDoNotEnregister &&
                            ((node->gtFlags & GTF_VAR_DEF) != 0))
                        {
                            varDsc->incRefCnts(0, this);
                        }
                        else
                        {
                            varDsc->incRefCnts(weight, this);
                        }

                        if ((node->gtFlags & GTF_VAR_CONTEXT) != 0)
                        {
                            assert(node->OperIs(GT_LCL_VAR));
                            lvaGenericsContextInUse = true;
                        }
                        break;
                    }

                    default:
                        break;
                }
            }
        }
        else
        {
            lvaMarkLocalVars(block, isRecompute);
        }
    }

    if (oldLvaGenericsContextInUse && !lvaGenericsContextInUse)
    {
        // Context was in use but no longer is. This can happen
        // if we're able to optimize, so just leave a note.
        JITDUMP("\n** Generics context no longer in use\n");
    }
    else if (lvaGenericsContextInUse && !oldLvaGenericsContextInUse)
    {
        // Context was not in use but now is.
        //
        // Changing from unused->used should never happen; creation of any new IR
        // for context use should also be settting lvaGenericsContextInUse.
        assert(!"unexpected new use of generics context");
    }

    JITDUMP("\n*** lvaComputeRefCounts -- implicit counts ***\n");

    // Third, bump ref counts for some implicit prolog references
    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        // Todo: review justification for these count bumps.
        if (varDsc->lvIsRegArg)
        {
            if ((lclNum < info.compArgsCount) && (varDsc->lvRefCnt() > 0))
            {
                // Fix 388376 ARM JitStress WP7
                varDsc->incRefCnts(BB_UNITY_WEIGHT, this);
                varDsc->incRefCnts(BB_UNITY_WEIGHT, this);
            }

            // Ref count bump that was in lvaPromoteStructVar
            //
            // This was formerly done during RCS_EARLY counting,
            // and we did not used to reset counts like we do now.
            if (varDsc->lvIsStructField)
            {
                varDsc->incRefCnts(BB_UNITY_WEIGHT, this);
            }
        }

        // If we have JMP, all arguments must have a location
        // even if we don't use them inside the method
        if (compJmpOpUsed && varDsc->lvIsParam && (varDsc->lvRefCnt() == 0))
        {
            // except when we have varargs and the argument is
            // passed on the stack.  In that case, it's important
            // for the ref count to be zero, so that we don't attempt
            // to track them for GC info (which is not possible since we
            // don't know their offset in the stack).  See the assert at the
            // end of raMarkStkVars and bug #28949 for more info.
            if (!raIsVarargsStackArg(lclNum))
            {
                varDsc->lvImplicitlyReferenced = 1;
            }
        }
    }
}

void Compiler::lvaAllocOutgoingArgSpaceVar()
{
#if FEATURE_FIXED_OUT_ARGS

    // Setup the outgoing argument region, in case we end up using it later

    if (lvaOutgoingArgSpaceVar == BAD_VAR_NUM)
    {
        lvaOutgoingArgSpaceVar = lvaGrabTemp(false DEBUGARG("OutgoingArgSpace"));

        lvaTable[lvaOutgoingArgSpaceVar].lvType                 = TYP_LCLBLK;
        lvaTable[lvaOutgoingArgSpaceVar].lvImplicitlyReferenced = 1;
    }

    noway_assert(lvaOutgoingArgSpaceVar >= info.compLocalsCount && lvaOutgoingArgSpaceVar < lvaCount);

#endif // FEATURE_FIXED_OUT_ARGS
}

inline void Compiler::lvaIncrementFrameSize(unsigned size)
{
    if (size > MAX_FrameSize || compLclFrameSize + size > MAX_FrameSize)
    {
        BADCODE("Frame size overflow");
    }

    compLclFrameSize += size;
}

/****************************************************************************
*
*  Return true if absolute offsets of temps are larger than vars, or in other
*  words, did we allocate temps before of after vars.  The /GS buffer overrun
*  checks want temps to be at low stack addresses than buffers
*/
bool Compiler::lvaTempsHaveLargerOffsetThanVars()
{
#ifdef TARGET_ARM
    // We never want to place the temps with larger offsets for ARM
    return false;
#else
    if (compGSReorderStackLayout)
    {
        return codeGen->isFramePointerUsed();
    }
    else
    {
        return true;
    }
#endif
}

/****************************************************************************
*
*  Return an upper bound estimate for the size of the compiler spill temps
*
*/
unsigned Compiler::lvaGetMaxSpillTempSize()
{
    unsigned result = 0;

    if (codeGen->regSet.hasComputedTmpSize())
    {
        result = codeGen->regSet.tmpGetTotalSize();
    }
    else
    {
        result = MAX_SPILL_TEMP_SIZE;
    }
    return result;
}

// clang-format off
/*****************************************************************************
 *
 *  Compute stack frame offsets for arguments, locals and optionally temps.
 *
 *  The frame is laid out as follows for x86:
 *
 *              ESP frames
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      |-----------------------| <---- Virtual '0'
 *      |    return address     |
 *      +=======================+
 *      |Callee saved registers |
 *      |-----------------------|
 *      |       Temps           |
 *      |-----------------------|
 *      |       Variables       |
 *      |-----------------------| <---- Ambient ESP
 *      |   Arguments for the   |
 *      ~    next function      ~
 *      |                       |
 *      |       |               |
 *      |       | Stack grows   |
 *              | downward
 *              V
 *
 *
 *              EBP frames
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      |-----------------------| <---- Virtual '0'
 *      |    return address     |
 *      +=======================+
 *      |    incoming EBP       |
 *      |-----------------------| <---- EBP
 *      |Callee saved registers |
 *      |-----------------------|
 *      |   security object     |
 *      |-----------------------|
 *      |     ParamTypeArg      |
 *      |-----------------------|
 *      |  Last-executed-filter |
 *      |-----------------------|
 *      |                       |
 *      ~      Shadow SPs       ~
 *      |                       |
 *      |-----------------------|
 *      |                       |
 *      ~      Variables        ~
 *      |                       |
 *      ~-----------------------|
 *      |       Temps           |
 *      |-----------------------|
 *      |       localloc        |
 *      |-----------------------| <---- Ambient ESP
 *      |   Arguments for the   |
 *      |    next function      ~
 *      |                       |
 *      |       |               |
 *      |       | Stack grows   |
 *              | downward
 *              V
 *
 *
 *  The frame is laid out as follows for x64:
 *
 *              RSP frames
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      |-----------------------|
 *      |   4 fixed incoming    |
 *      |    argument slots     |
 *      |-----------------------| <---- Caller's SP & Virtual '0'
 *      |    return address     |
 *      +=======================+
 *      | Callee saved Int regs |
 *      -------------------------
 *      |        Padding        | <---- this padding (0 or 8 bytes) is to ensure flt registers are saved at a mem location aligned at 16-bytes
 *      |                       |       so that we can save 128-bit callee saved xmm regs using performant "movaps" instruction instead of "movups"
 *      -------------------------
 *      | Callee saved Flt regs | <----- entire 128-bits of callee saved xmm registers are stored here
 *      |-----------------------|
 *      |         Temps         |
 *      |-----------------------|
 *      |       Variables       |
 *      |-----------------------|
 *      |   Arguments for the   |
 *      ~    next function      ~
 *      |                       |
 *      |-----------------------|
 *      |   4 fixed outgoing    |
 *      |    argument slots     |
 *      |-----------------------| <---- Ambient RSP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 *              RBP frames
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      |-----------------------|
 *      |   4 fixed incoming    |
 *      |    argument slots     |
 *      |-----------------------| <---- Caller's SP & Virtual '0'
 *      |    return address     |
 *      +=======================+
 *      | Callee saved Int regs |
 *      -------------------------
 *      |        Padding        |
 *      -------------------------
 *      | Callee saved Flt regs |
 *      |-----------------------|
 *      |   security object     |
 *      |-----------------------|
 *      |     ParamTypeArg      |
 *      |-----------------------|
 *      |                       |
 *      |                       |
 *      ~       Variables       ~
 *      |                       |
 *      |                       |
 *      |-----------------------|
 *      |        Temps          |
 *      |-----------------------|
 *      |                       |
 *      ~       localloc        ~   // not in frames with EH
 *      |                       |
 *      |-----------------------|
 *      |        PSPSym         |   // only in frames with EH (thus no localloc)
 *      |                       |
 *      |-----------------------| <---- RBP in localloc frames (max 240 bytes from Initial-SP)
 *      |   Arguments for the   |
 *      ~    next function      ~
 *      |                       |
 *      |-----------------------|
 *      |   4 fixed outgoing    |
 *      |    argument slots     |
 *      |-----------------------| <---- Ambient RSP (before localloc, this is Initial-SP)
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 *  The frame is laid out as follows for ARM (this is a general picture; details may differ for different conditions):
 *
 *              SP frames
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |  Pre-spill registers  |
 *      |-----------------------| <---- Virtual '0'
 *      |Callee saved registers |
 *      |-----------------------|
 *      ~ possible double align ~
 *      |-----------------------|
 *      |   security object     |
 *      |-----------------------|
 *      |     ParamTypeArg      |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |       Variables       |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |        Temps          |
 *      |-----------------------|
 *      |   Stub Argument Var   |
 *      |-----------------------|
 *      |Inlined PInvoke Frame V|
 *      |-----------------------|
 *      ~ possible double align ~
 *      |-----------------------|
 *      |   Arguments for the   |
 *      ~    next function      ~
 *      |                       |
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 *              FP / R11 frames
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |  Pre-spill registers  |
 *      |-----------------------| <---- Virtual '0'
 *      |Callee saved registers |
 *      |-----------------------|
 *      |        PSPSym         |   // Only for frames with EH, which means FP-based frames
 *      |-----------------------|
 *      ~ possible double align ~
 *      |-----------------------|
 *      |   security object     |
 *      |-----------------------|
 *      |     ParamTypeArg      |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |       Variables       |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |        Temps          |
 *      |-----------------------|
 *      |   Stub Argument Var   |
 *      |-----------------------|
 *      |Inlined PInvoke Frame V|
 *      |-----------------------|
 *      ~ possible double align ~
 *      |-----------------------|
 *      |       localloc        |
 *      |-----------------------|
 *      |   Arguments for the   |
 *      ~    next function      ~
 *      |                       |
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 *  The frame is laid out as follows for ARM64 (this is a general picture; details may differ for different conditions):
 *  NOTE: SP must be 16-byte aligned, so there may be alignment slots in the frame.
 *  We will often save and establish a frame pointer to create better ETW stack walks.
 *
 *              SP frames
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |         homed         | // this is only needed if reg argument need to be homed, e.g., for varargs
 *      |   register arguments  |
 *      |-----------------------| <---- Virtual '0'
 *      |Callee saved registers |
 *      |   except fp/lr        |
 *      |-----------------------|
 *      |   security object     |
 *      |-----------------------|
 *      |     ParamTypeArg      |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |       Variables       |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |        Temps          |
 *      |-----------------------|
 *      |   Stub Argument Var   |
 *      |-----------------------|
 *      |Inlined PInvoke Frame V|
 *      |-----------------------|
 *      |      Saved LR         |
 *      |-----------------------|
 *      |      Saved FP         | <---- Frame pointer
 *      |-----------------------|
 *      |  Stack arguments for  |
 *      |   the next function   |
 *      |-----------------------| <---- SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 *              FP (R29 / x29) frames
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |     optional homed    | // this is only needed if reg argument need to be homed, e.g., for varargs
 *      |   register arguments  |
 *      |-----------------------| <---- Virtual '0'
 *      |Callee saved registers |
 *      |   except fp/lr        |
 *      |-----------------------|
 *      |        PSPSym         | // Only for frames with EH, which requires FP-based frames
 *      |-----------------------|
 *      |   security object     |
 *      |-----------------------|
 *      |     ParamTypeArg      |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |       Variables       |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |        Temps          |
 *      |-----------------------|
 *      |   Stub Argument Var   |
 *      |-----------------------|
 *      |Inlined PInvoke Frame V|
 *      |-----------------------|
 *      |      Saved LR         |
 *      |-----------------------|
 *      |      Saved FP         | <---- Frame pointer
 *      |-----------------------|
 *      ~       localloc        ~
 *      |-----------------------|
 *      |  Stack arguments for  |
 *      |   the next function   |
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 *              FP (R29 / x29) frames where FP/LR are stored at the top of the frame (frames requiring GS that have localloc)
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |     optional homed    | // this is only needed if reg argument need to be homed, e.g., for varargs
 *      |   register arguments  |
 *      |-----------------------| <---- Virtual '0'
 *      |      Saved LR         |
 *      |-----------------------|
 *      |      Saved FP         | <---- Frame pointer
 *      |-----------------------|
 *      |Callee saved registers |
 *      |-----------------------|
 *      |        PSPSym         | // Only for frames with EH, which requires FP-based frames
 *      |-----------------------|
 *      |   security object     |
 *      |-----------------------|
 *      |     ParamTypeArg      |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |       Variables       |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |        Temps          |
 *      |-----------------------|
 *      |   Stub Argument Var   |
 *      |-----------------------|
 *      |Inlined PInvoke Frame V|
 *      |-----------------------|
 *      ~       localloc        ~
 *      |-----------------------|
 *      |  Stack arguments for  |
 *      |   the next function   |
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 *  Doing this all in one pass is 'hard'.  So instead we do it in 2 basic passes:
 *    1. Assign all the offsets relative to the Virtual '0'. Offsets above (the
 *      incoming arguments) are positive. Offsets below (everything else) are
 *      negative.  This pass also calcuates the total frame size (between Caller's
 *      SP/return address and the Ambient SP).
 *    2. Figure out where to place the frame pointer, and then adjust the offsets
 *      as needed for the final stack size and whether the offset is frame pointer
 *      relative or stack pointer relative.
 *
 */
// clang-format on

void Compiler::lvaAssignFrameOffsets(FrameLayoutState curState)
{
    noway_assert((lvaDoneFrameLayout < curState) || (curState == REGALLOC_FRAME_LAYOUT));

    lvaDoneFrameLayout = curState;

#ifdef DEBUG
    if (verbose)
    {

        printf("*************** In lvaAssignFrameOffsets");
        if (curState == INITIAL_FRAME_LAYOUT)
        {
            printf("(INITIAL_FRAME_LAYOUT)");
        }
        else if (curState == PRE_REGALLOC_FRAME_LAYOUT)
        {
            printf("(PRE_REGALLOC_FRAME_LAYOUT)");
        }
        else if (curState == REGALLOC_FRAME_LAYOUT)
        {
            printf("(REGALLOC_FRAME_LAYOUT)");
        }
        else if (curState == TENTATIVE_FRAME_LAYOUT)
        {
            printf("(TENTATIVE_FRAME_LAYOUT)");
        }
        else if (curState == FINAL_FRAME_LAYOUT)
        {
            printf("(FINAL_FRAME_LAYOUT)");
        }
        else
        {
            printf("(UNKNOWN)");
            unreached();
        }
        printf("\n");
    }
#endif

#if FEATURE_FIXED_OUT_ARGS
    assert(lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
#endif // FEATURE_FIXED_OUT_ARGS

    /*-------------------------------------------------------------------------
     *
     * First process the arguments.
     *
     *-------------------------------------------------------------------------
     */

    lvaAssignVirtualFrameOffsetsToArgs();

    /*-------------------------------------------------------------------------
     *
     * Now compute stack offsets for any variables that don't live in registers
     *
     *-------------------------------------------------------------------------
     */

    lvaAssignVirtualFrameOffsetsToLocals();

    lvaAlignFrame();

    /*-------------------------------------------------------------------------
     *
     * Now patch the offsets
     *
     *-------------------------------------------------------------------------
     */

    lvaFixVirtualFrameOffsets();

    // Modify the stack offset for fields of promoted structs.
    lvaAssignFrameOffsetsToPromotedStructs();

    /*-------------------------------------------------------------------------
     *
     * Finalize
     *
     *-------------------------------------------------------------------------
     */

    // If it's not the final frame layout, then it's just an estimate. This means
    // we're allowed to once again write to these variables, even if we've read
    // from them to make tentative code generation or frame layout decisions.
    if (curState < FINAL_FRAME_LAYOUT)
    {
        codeGen->resetFramePointerUsedWritePhase();
    }
}

/*****************************************************************************
 *  lvaFixVirtualFrameOffsets() : Now that everything has a virtual offset,
 *  determine the final value for the frame pointer (if needed) and then
 *  adjust all the offsets appropriately.
 *
 *  This routine fixes virtual offset to be relative to frame pointer or SP
 *  based on whether varDsc->lvFramePointerBased is true or false respectively.
 */
void Compiler::lvaFixVirtualFrameOffsets()
{
    LclVarDsc* varDsc;

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_AMD64)
    if (lvaPSPSym != BAD_VAR_NUM)
    {
        // We need to fix the offset of the PSPSym so there is no padding between it and the outgoing argument space.
        // Without this code, lvaAlignFrame might have put the padding lower than the PSPSym, which would be between
        // the PSPSym and the outgoing argument space.
        varDsc = lvaGetDesc(lvaPSPSym);
        assert(varDsc->lvFramePointerBased); // We always access it RBP-relative.
        assert(!varDsc->lvMustInit);         // It is never "must init".
        varDsc->SetStackOffset(codeGen->genCallerSPtoInitialSPdelta() + lvaLclSize(lvaOutgoingArgSpaceVar));

        if (opts.IsOSR())
        {
            // With OSR RBP points at the base of the OSR frame, but the virtual offsets
            // are from the base of the Tier0 frame. Adjust.
            //
            varDsc->SetStackOffset(varDsc->GetStackOffset() - info.compPatchpointInfo->TotalFrameSize());
        }
    }
#endif

    // The delta to be added to virtual offset to adjust it relative to frame pointer or SP
    int delta = 0;

#ifdef TARGET_XARCH
    delta += REGSIZE_BYTES; // pushed PC (return address) for x86/x64
    JITDUMP("--- delta bump %d for RA\n", REGSIZE_BYTES);

    if (codeGen->doubleAlignOrFramePointerUsed())
    {
        JITDUMP("--- delta bump %d for FP\n", REGSIZE_BYTES);
        delta += REGSIZE_BYTES; // pushed EBP (frame pointer)
    }
#endif

    if (!codeGen->isFramePointerUsed())
    {
        // pushed registers, return address, and padding
        JITDUMP("--- delta bump %d for RSP frame\n", codeGen->genTotalFrameSize());
        delta += codeGen->genTotalFrameSize();
    }
#if defined(TARGET_ARM)
    else
    {
        // We set FP to be after LR, FP
        delta += 2 * REGSIZE_BYTES;
    }
#elif defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
    else
    {
        // FP is used.
        JITDUMP("--- delta bump %d for FP frame\n", codeGen->genTotalFrameSize() - codeGen->genSPtoFPdelta());
        delta += codeGen->genTotalFrameSize() - codeGen->genSPtoFPdelta();
    }
#endif // TARGET_AMD64 || TARGET_ARM64 || TARGET_LOONGARCH64

    if (opts.IsOSR())
    {
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
        // Stack offset includes Tier0 frame.
        //
        JITDUMP("--- delta bump %d for OSR + Tier0 frame\n", info.compPatchpointInfo->TotalFrameSize());
        delta += info.compPatchpointInfo->TotalFrameSize();
#endif
    }

    JITDUMP("--- virtual stack offset to actual stack offset delta is %d\n", delta);

    unsigned lclNum;
    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        bool doAssignStkOffs = true;

        // Can't be relative to EBP unless we have an EBP
        noway_assert(!varDsc->lvFramePointerBased || codeGen->doubleAlignOrFramePointerUsed());

        // Is this a non-param promoted struct field?
        //   if so then set doAssignStkOffs to false.
        //
        if (varDsc->lvIsStructField)
        {
            LclVarDsc*       parentvarDsc  = lvaGetDesc(varDsc->lvParentLcl);
            lvaPromotionType promotionType = lvaGetPromotionType(parentvarDsc);

#if defined(TARGET_X86)
            // On x86, we set the stack offset for a promoted field
            // to match a struct parameter in lvAssignFrameOffsetsToPromotedStructs.
            if ((!varDsc->lvIsParam || parentvarDsc->lvIsParam) && promotionType == PROMOTION_TYPE_DEPENDENT)
#else
            if (!varDsc->lvIsParam && promotionType == PROMOTION_TYPE_DEPENDENT)
#endif
            {
                doAssignStkOffs = false; // Assigned later in lvaAssignFrameOffsetsToPromotedStructs()
            }
        }

        if (!varDsc->lvOnFrame)
        {
            if (!varDsc->lvIsParam
#if !defined(TARGET_AMD64)
                || (varDsc->lvIsRegArg
#if defined(TARGET_ARM) && defined(PROFILING_SUPPORTED)
                    && compIsProfilerHookNeeded() &&
                    !lvaIsPreSpilled(lclNum, codeGen->regSet.rsMaskPreSpillRegs(false)) // We need assign stack offsets
                                                                                        // for prespilled arguments
#endif
                    )
#endif // !defined(TARGET_AMD64)
                    )
            {
                doAssignStkOffs = false; // Not on frame or an incomming stack arg
            }
        }

        if (doAssignStkOffs)
        {
            JITDUMP("-- V%02u was %d, now %d\n", lclNum, varDsc->GetStackOffset(), varDsc->GetStackOffset() + delta);
            varDsc->SetStackOffset(varDsc->GetStackOffset() + delta);

#if DOUBLE_ALIGN
            if (genDoubleAlign() && !codeGen->isFramePointerUsed())
            {
                if (varDsc->lvFramePointerBased)
                {
                    varDsc->SetStackOffset(varDsc->GetStackOffset() - delta);

                    // We need to re-adjust the offsets of the parameters so they are EBP
                    // relative rather than stack/frame pointer relative

                    varDsc->SetStackOffset(varDsc->GetStackOffset() +
                                           (2 * TARGET_POINTER_SIZE)); // return address and pushed EBP

                    noway_assert(varDsc->GetStackOffset() >= FIRST_ARG_STACK_OFFS);
                }
            }
#endif
            // On System V environments the stkOffs could be 0 for params passed in registers.
            //
            // For normal methods only EBP relative references can have negative offsets.
            assert(codeGen->isFramePointerUsed() || varDsc->GetStackOffset() >= 0);
        }
    }

    assert(codeGen->regSet.tmpAllFree());
    for (TempDsc* temp = codeGen->regSet.tmpListBeg(); temp != nullptr; temp = codeGen->regSet.tmpListNxt(temp))
    {
        temp->tdAdjustTempOffs(delta);
    }

    lvaCachedGenericContextArgOffs += delta;

#if FEATURE_FIXED_OUT_ARGS

    if (lvaOutgoingArgSpaceVar != BAD_VAR_NUM)
    {
        varDsc = lvaGetDesc(lvaOutgoingArgSpaceVar);
        varDsc->SetStackOffset(0);
        varDsc->lvFramePointerBased = false;
        varDsc->lvMustInit          = false;
    }

#endif // FEATURE_FIXED_OUT_ARGS

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
    // We normally add alignment below the locals between them and the outgoing
    // arg space area. When we store fp/lr(ra) at the bottom, however, this will
    // be below the alignment. So we should not apply the alignment adjustment to
    // them. It turns out we always store these at +0 and +8 of the FP,
    // so instead of dealing with skipping adjustment just for them we just set
    // them here always.
    assert(codeGen->isFramePointerUsed());
    if (lvaRetAddrVar != BAD_VAR_NUM)
    {
        lvaTable[lvaRetAddrVar].SetStackOffset(REGSIZE_BYTES);
    }
#endif // TARGET_ARM64 || TARGET_LOONGARCH64
}

#ifdef TARGET_ARM
bool Compiler::lvaIsPreSpilled(unsigned lclNum, regMaskTP preSpillMask)
{
    const LclVarDsc& desc = lvaTable[lclNum];
    return desc.lvIsRegArg && (preSpillMask & genRegMask(desc.GetArgReg()));
}
#endif // TARGET_ARM

//------------------------------------------------------------------------
// lvaUpdateArgWithInitialReg: Set the initial register of a local variable
//                             to the one assigned by the register allocator.
//
// Arguments:
//    varDsc - the local variable descriptor
//
void Compiler::lvaUpdateArgWithInitialReg(LclVarDsc* varDsc)
{
    noway_assert(varDsc->lvIsParam);

    if (varDsc->lvIsRegCandidate())
    {
        varDsc->SetRegNum(varDsc->GetArgInitReg());
    }
}

//------------------------------------------------------------------------
// lvaUpdateArgsWithInitialReg() : For each argument variable descriptor, update
//     its current register with the initial register as assigned by LSRA.
//
void Compiler::lvaUpdateArgsWithInitialReg()
{
    if (!compLSRADone)
    {
        return;
    }

    for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
    {
        LclVarDsc* varDsc = lvaGetDesc(lclNum);

        if (varDsc->lvPromotedStruct())
        {
            for (unsigned fieldVarNum = varDsc->lvFieldLclStart;
                 fieldVarNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++fieldVarNum)
            {
                LclVarDsc* fieldVarDsc = lvaGetDesc(fieldVarNum);
                lvaUpdateArgWithInitialReg(fieldVarDsc);
            }
        }
        else
        {
            lvaUpdateArgWithInitialReg(varDsc);
        }
    }
}

/*****************************************************************************
 *  lvaAssignVirtualFrameOffsetsToArgs() : Assign virtual stack offsets to the
 *  arguments, and implicit arguments (this ptr, return buffer, generics,
 *  and varargs).
 */
void Compiler::lvaAssignVirtualFrameOffsetsToArgs()
{
    unsigned lclNum  = 0;
    int      argOffs = 0;
#ifdef UNIX_AMD64_ABI
    int callerArgOffset = 0;
#endif // UNIX_AMD64_ABI

    /*
        Assign stack offsets to arguments (in reverse order of passing).

        This means that if we pass arguments left->right, we start at
        the end of the list and work backwards, for right->left we start
        with the first argument and move forward.

        This is all relative to our Virtual '0'
     */

    if (info.compArgOrder == Target::ARG_ORDER_L2R)
    {
        argOffs = compArgSize;
    }

    /* Update the argOffs to reflect arguments that are passed in registers */

    noway_assert(codeGen->intRegState.rsCalleeRegArgCount <= MAX_REG_ARG);
    noway_assert(compMacOsArm64Abi() || compArgSize >= codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES);

    if (info.compArgOrder == Target::ARG_ORDER_L2R)
    {
        argOffs -= codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES;
    }

    // Update the arg initial register locations.
    lvaUpdateArgsWithInitialReg();

    /* Is there a "this" argument? */

    if (!info.compIsStatic)
    {
        noway_assert(lclNum == info.compThisArg);
#ifndef TARGET_X86
        argOffs =
            lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
#endif // TARGET_X86
        lclNum++;
    }

    unsigned userArgsToSkip = 0;
#if !defined(TARGET_ARM)
    // In the native instance method calling convention on Windows,
    // the this parameter comes before the hidden return buffer parameter.
    // So, we want to process the native "this" parameter before we process
    // the native return buffer parameter.
    if (TargetOS::IsWindows && callConvIsInstanceMethodCallConv(info.compCallConv))
    {
#ifdef TARGET_X86
        if (!lvaTable[lclNum].lvIsRegArg)
        {
            argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs);
        }
#elif !defined(UNIX_AMD64_ABI)
        argOffs              = lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs);
#endif // TARGET_X86
        lclNum++;
        userArgsToSkip++;
    }
#endif

    /* if we have a hidden buffer parameter, that comes here */

    if (info.compRetBuffArg != BAD_VAR_NUM)
    {
        noway_assert(lclNum == info.compRetBuffArg);
        argOffs =
            lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
        lclNum++;
    }

#if USER_ARGS_COME_LAST

    //@GENERICS: extra argument for instantiation info
    if (info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        noway_assert(lclNum == (unsigned)info.compTypeCtxtArg);
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, REGSIZE_BYTES,
                                                   argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
    }

    if (info.compIsVarArgs)
    {
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, REGSIZE_BYTES,
                                                   argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
    }

#endif // USER_ARGS_COME_LAST

    CORINFO_ARG_LIST_HANDLE argLst    = info.compMethodInfo->args.args;
    unsigned                argSigLen = info.compMethodInfo->args.numArgs;
    // Skip any user args that we've already processed.
    assert(userArgsToSkip <= argSigLen);
    argSigLen -= userArgsToSkip;
    for (unsigned i = 0; i < userArgsToSkip; i++, argLst = info.compCompHnd->getArgNext(argLst))
    {
        ;
    }

#ifdef TARGET_ARM
    //
    // struct_n { int; int; ... n times };
    //
    // Consider signature:
    //
    // Foo (float a,double b,float c,double d,float e,double f,float g,double h,
    //      float i,double j,float k,double l,struct_3 m) { }
    //
    // Basically the signature is: (all float regs full, 1 double, struct_3);
    //
    // The double argument occurs before pre spill in the argument iteration and
    // computes an argOffset of 0. struct_3 offset becomes 8. This is wrong.
    // Because struct_3 is prespilled and double occurs after prespill.
    // The correct offsets are double = 16 (aligned stk), struct_3 = 0..12,
    // Offset 12 will be skipped for double alignment of double.
    //
    // Another example is (struct_2, all float regs full, double, struct_2);
    // Here, notice the order is similarly messed up because of 2 pre-spilled
    // struct_2.
    //
    // Succinctly,
    // ARG_INDEX(i) > ARG_INDEX(j) DOES NOT IMPLY |ARG_OFFSET(i)| > |ARG_OFFSET(j)|
    //
    // Therefore, we'll do a two pass offset calculation, one that considers pre-spill
    // and the next, stack args.
    //

    unsigned argLcls = 0;

    // Take care of pre spill registers first.
    regMaskTP preSpillMask = codeGen->regSet.rsMaskPreSpillRegs(false);
    regMaskTP tempMask     = RBM_NONE;
    for (unsigned i = 0, preSpillLclNum = lclNum; i < argSigLen; ++i, ++preSpillLclNum)
    {
        if (lvaIsPreSpilled(preSpillLclNum, preSpillMask))
        {
            unsigned argSize = eeGetArgSize(argLst, &info.compMethodInfo->args);
            argOffs          = lvaAssignVirtualFrameOffsetToArg(preSpillLclNum, argSize, argOffs);
            argLcls++;

            // Early out if we can. If size is 8 and base reg is 2, then the mask is 0x1100
            tempMask |= ((((1 << (roundUp(argSize, TARGET_POINTER_SIZE) / REGSIZE_BYTES))) - 1)
                         << lvaTable[preSpillLclNum].GetArgReg());
            if (tempMask == preSpillMask)
            {
                // We won't encounter more pre-spilled registers,
                // so don't bother iterating further.
                break;
            }
        }
        argLst = info.compCompHnd->getArgNext(argLst);
    }

    // Take care of non pre-spilled stack arguments.
    argLst = info.compMethodInfo->args.args;
    for (unsigned i = 0, stkLclNum = lclNum; i < argSigLen; ++i, ++stkLclNum)
    {
        if (!lvaIsPreSpilled(stkLclNum, preSpillMask))
        {
            const unsigned argSize = eeGetArgSize(argLst, &info.compMethodInfo->args);
            argOffs                = lvaAssignVirtualFrameOffsetToArg(stkLclNum, argSize, argOffs);
            argLcls++;
        }
        argLst = info.compCompHnd->getArgNext(argLst);
    }

    lclNum += argLcls;
#else  // !TARGET_ARM
    for (unsigned i = 0; i < argSigLen; i++)
    {
        unsigned argumentSize = eeGetArgSize(argLst, &info.compMethodInfo->args);

        assert(compMacOsArm64Abi() || argumentSize % TARGET_POINTER_SIZE == 0);

        argOffs =
            lvaAssignVirtualFrameOffsetToArg(lclNum++, argumentSize, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
        argLst = info.compCompHnd->getArgNext(argLst);
    }
#endif // !TARGET_ARM

#if !USER_ARGS_COME_LAST

    //@GENERICS: extra argument for instantiation info
    if (info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        noway_assert(lclNum == (unsigned)info.compTypeCtxtArg);
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, REGSIZE_BYTES,
                                                   argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
    }

    if (info.compIsVarArgs)
    {
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, REGSIZE_BYTES,
                                                   argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
    }

#endif // USER_ARGS_COME_LAST
}

#ifdef UNIX_AMD64_ABI
//
//  lvaAssignVirtualFrameOffsetToArg() : Assign virtual stack offsets to an
//  individual argument, and return the offset for the next argument.
//  Note: This method only calculates the initial offset of the stack passed/spilled arguments
//  (if any - the RA might decide to spill(home on the stack) register passed arguments, if rarely used.)
//        The final offset is calculated in lvaFixVirtualFrameOffsets method. It accounts for FP existance,
//        ret address slot, stack frame padding, alloca instructions, etc.
//  Note: This is the implementation for UNIX_AMD64 System V platforms.
//
int Compiler::lvaAssignVirtualFrameOffsetToArg(unsigned lclNum,
                                               unsigned argSize,
                                               int argOffs UNIX_AMD64_ABI_ONLY_ARG(int* callerArgOffset))
{
    noway_assert(lclNum < info.compArgsCount);
    noway_assert(argSize);

    if (info.compArgOrder == Target::ARG_ORDER_L2R)
    {
        argOffs -= argSize;
    }

    unsigned fieldVarNum = BAD_VAR_NUM;

    LclVarDsc* varDsc = lvaGetDesc(lclNum);

    noway_assert(varDsc->lvIsParam);

    if (varDsc->lvIsRegArg)
    {
        // Argument is passed in a register, don't count it
        // when updating the current offset on the stack.

        if (varDsc->lvOnFrame)
        {
            // The offset for args needs to be set only for the stack homed arguments for System V.
            varDsc->SetStackOffset(argOffs);
        }
        else
        {
            varDsc->SetStackOffset(0);
        }
    }
    else
    {
        // For Windows AMD64 there are 4 slots for the register passed arguments on the top of the caller's stack.
        // This is where they are always homed. So, they can be accessed with positive offset.
        // On System V platforms, if the RA decides to home a register passed arg on the stack, it creates a stack
        // location on the callee stack (like any other local var.) In such a case, the register passed, stack homed
        // arguments are accessed using negative offsets and the stack passed arguments are accessed using positive
        // offset (from the caller's stack.)
        // For  System V platforms if there is no frame pointer the caller stack parameter offset should include the
        // callee allocated space. If frame register is used, the callee allocated space should not be included for
        // accessing the caller stack parameters. The last two requirements are met in lvaFixVirtualFrameOffsets
        // method, which fixes the offsets, based on frame pointer existence, existence of alloca instructions, ret
        // address pushed, ets.

        varDsc->SetStackOffset(*callerArgOffset);
        // Structs passed on stack could be of size less than TARGET_POINTER_SIZE.
        // Make sure they get at least TARGET_POINTER_SIZE on the stack - this is required for alignment.
        if (argSize > TARGET_POINTER_SIZE)
        {
            *callerArgOffset += (int)roundUp(argSize, TARGET_POINTER_SIZE);
        }
        else
        {
            *callerArgOffset += TARGET_POINTER_SIZE;
        }
    }

    // For struct promoted parameters we need to set the offsets for the field lclVars.
    //
    // For a promoted struct we also assign the struct fields stack offset
    if (varDsc->lvPromotedStruct())
    {
        unsigned firstFieldNum = varDsc->lvFieldLclStart;
        int      offset        = varDsc->GetStackOffset();
        for (unsigned i = 0; i < varDsc->lvFieldCnt; i++)
        {
            LclVarDsc* fieldVarDsc = lvaGetDesc(firstFieldNum + i);
            fieldVarDsc->SetStackOffset(offset + fieldVarDsc->lvFldOffset);
        }
    }

    if (info.compArgOrder == Target::ARG_ORDER_R2L && !varDsc->lvIsRegArg)
    {
        argOffs += argSize;
    }

    return argOffs;
}

#else // !UNIX_AMD64_ABI

//
//  lvaAssignVirtualFrameOffsetToArg() : Assign virtual stack offsets to an
//  individual argument, and return the offset for the next argument.
//  Note: This method only calculates the initial offset of the stack passed/spilled arguments
//  (if any - the RA might decide to spill(home on the stack) register passed arguments, if rarely used.)
//        The final offset is calculated in lvaFixVirtualFrameOffsets method. It accounts for FP existance,
//        ret address slot, stack frame padding, alloca instructions, etc.
//  Note: This implementation for all the platforms but UNIX_AMD64 OSs (System V 64 bit.)
int Compiler::lvaAssignVirtualFrameOffsetToArg(unsigned lclNum,
                                               unsigned argSize,
                                               int argOffs UNIX_AMD64_ABI_ONLY_ARG(int* callerArgOffset))
{
    noway_assert(lclNum < info.compArgsCount);
    noway_assert(argSize);

    if (info.compArgOrder == Target::ARG_ORDER_L2R)
    {
        argOffs -= argSize;
    }

    unsigned fieldVarNum = BAD_VAR_NUM;

    LclVarDsc* varDsc = lvaGetDesc(lclNum);

    noway_assert(varDsc->lvIsParam);

    if (varDsc->lvIsRegArg)
    {
        /* Argument is passed in a register, don't count it
         * when updating the current offset on the stack */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(TARGET_ARMARCH) && !defined(TARGET_LOONGARCH64)
#if DEBUG
        // TODO: Remove this noway_assert and replace occurrences of TARGET_POINTER_SIZE with argSize
        // Also investigate why we are incrementing argOffs for X86 as this seems incorrect
        //
        noway_assert(argSize == TARGET_POINTER_SIZE);
#endif // DEBUG
#endif

#if defined(TARGET_X86)
        argOffs += TARGET_POINTER_SIZE;
#elif defined(TARGET_AMD64)
        // Register arguments on AMD64 also takes stack space. (in the backing store)
        varDsc->SetStackOffset(argOffs);
        argOffs += TARGET_POINTER_SIZE;
#elif defined(TARGET_ARM64)
        // Register arguments on ARM64 only take stack space when they have a frame home.
        // Unless on windows and in a vararg method.
        if (compFeatureArgSplit() && this->info.compIsVarArgs)
        {
            if (varDsc->lvType == TYP_STRUCT && varDsc->GetOtherArgReg() >= MAX_REG_ARG &&
                varDsc->GetOtherArgReg() != REG_NA)
            {
                // This is a split struct. It will account for an extra (8 bytes)
                // of alignment.
                varDsc->SetStackOffset(varDsc->GetStackOffset() + TARGET_POINTER_SIZE);
                argOffs += TARGET_POINTER_SIZE;
            }
        }

#elif defined(TARGET_ARM)
        // On ARM we spill the registers in codeGen->regSet.rsMaskPreSpillRegArg
        // in the prolog, so we have to do SetStackOffset() here
        //
        regMaskTP regMask = genRegMask(varDsc->GetArgReg());
        if (codeGen->regSet.rsMaskPreSpillRegArg & regMask)
        {
            // Signature: void foo(struct_8, int, struct_4)
            // ------- CALLER SP -------
            // r3 struct_4
            // r2 int - not prespilled, but added for alignment. argOffs should skip this.
            // r1 struct_8
            // r0 struct_8
            // -------------------------
            // If we added alignment we need to fix argOffs for all registers above alignment.
            if (codeGen->regSet.rsMaskPreSpillAlign != RBM_NONE)
            {
                assert(genCountBits(codeGen->regSet.rsMaskPreSpillAlign) == 1);
                // Is register beyond the alignment pos?
                if (regMask > codeGen->regSet.rsMaskPreSpillAlign)
                {
                    // Increment argOffs just once for the _first_ register after alignment pos
                    // in the prespill mask.
                    if (!BitsBetween(codeGen->regSet.rsMaskPreSpillRegArg, regMask,
                                     codeGen->regSet.rsMaskPreSpillAlign))
                    {
                        argOffs += TARGET_POINTER_SIZE;
                    }
                }
            }

            switch (varDsc->lvType)
            {
                case TYP_STRUCT:
                    if (!varDsc->lvStructDoubleAlign)
                    {
                        break;
                    }
                    FALLTHROUGH;

                case TYP_DOUBLE:
                case TYP_LONG:
                {
                    //
                    // Let's assign offsets to arg1, a double in r2. argOffs has to be 4 not 8.
                    //
                    // ------- CALLER SP -------
                    // r3
                    // r2 double   -- argOffs = 4, but it doesn't need to be skipped, because there is no skipping.
                    // r1 VACookie -- argOffs = 0
                    // -------------------------
                    //
                    // Consider argOffs as if it accounts for number of prespilled registers before the current
                    // register. In the above example, for r2, it is r1 that is prespilled, but since r1 is
                    // accounted for by argOffs being 4, there should have been no skipping. Instead, if we didn't
                    // assign r1 to any variable, then argOffs would still be 0 which implies it is not accounting
                    // for r1, equivalently r1 is skipped.
                    //
                    // If prevRegsSize is unaccounted for by a corresponding argOffs, we must have skipped a register.
                    int prevRegsSize =
                        genCountBits(codeGen->regSet.rsMaskPreSpillRegArg & (regMask - 1)) * TARGET_POINTER_SIZE;
                    if (argOffs < prevRegsSize)
                    {
                        // We must align up the argOffset to a multiple of 8 to account for skipped registers.
                        argOffs = roundUp((unsigned)argOffs, 2 * TARGET_POINTER_SIZE);
                    }
                    // We should've skipped only a single register.
                    assert(argOffs == prevRegsSize);
                }
                break;

                default:
                    // No alignment of argOffs required
                    break;
            }
            varDsc->SetStackOffset(argOffs);
            argOffs += argSize;
        }

#elif defined(TARGET_LOONGARCH64)

        if (varDsc->lvIsSplit)
        {
            assert((varDsc->lvType == TYP_STRUCT) && (varDsc->GetOtherArgReg() == REG_STK));
            // This is a split struct. It will account for an extra (8 bytes) for the whole struct.
            varDsc->SetStackOffset(varDsc->GetStackOffset() + TARGET_POINTER_SIZE);
            argOffs += TARGET_POINTER_SIZE;
        }

#else // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*
    }
    else
    {
#if defined(TARGET_ARM)
        // Dev11 Bug 42817: incorrect codegen for DrawFlatCheckBox causes A/V in WinForms
        //
        // Here we have method with a signature (int a1, struct a2, struct a3, int a4, int a5).
        // Struct parameter 'a2' is 16-bytes with no alignment requirements;
        //  it uses r1,r2,r3 and [OutArg+0] when passed.
        // Struct parameter 'a3' is 16-bytes that is required to be double aligned;
        //  the caller skips [OutArg+4] and starts the argument at [OutArg+8].
        // Thus the caller generates the correct code to pass the arguments.
        // When generating code to receive the arguments we set codeGen->regSet.rsMaskPreSpillRegArg to [r1,r2,r3]
        //  and spill these three registers as the first instruction in the prolog.
        // Then when we layout the arguments' stack offsets we have an argOffs 0 which
        //  points at the location that we spilled r1 into the stack.  For this first
        //  struct we take the lvIsRegArg path above with "codeGen->regSet.rsMaskPreSpillRegArg &" matching.
        // Next when we calculate the argOffs for the second 16-byte struct we have an argOffs
        //  of 16, which appears to be aligned properly so we don't skip a stack slot.
        //
        // To fix this we must recover the actual OutArg offset by subtracting off the
        //  sizeof of the PreSpill register args.
        // Then we align this offset to a multiple of 8 and add back the sizeof
        //  of the PreSpill register args.
        //
        // Dev11 Bug 71767: failure of assert(sizeofPreSpillRegArgs <= argOffs)
        //
        // We have a method with 'this' passed in r0, RetBuf arg in r1, VarArgs cookie
        // in r2. The first user arg is a 144 byte struct with double alignment required,
        // r3 is skipped, and the struct is passed on the stack. However, 'r3' is added
        // to the codeGen->regSet.rsMaskPreSpillRegArg mask by the VarArgs cookie code, since we need to
        // home all the potential varargs arguments in registers, even if we don't have
        // signature type information for the variadic arguments. However, due to alignment,
        // we have skipped a register that doesn't have a corresponding symbol. Make up
        // for that by increasing argOffs here.
        //

        int sizeofPreSpillRegArgs = genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;

        if (argOffs < sizeofPreSpillRegArgs)
        {
            // This can only happen if we skipped the last register spot because current stk arg
            // is a struct requiring alignment or a pre-spill alignment was required because the
            // first reg arg needed alignment.
            //
            // Example 1: First Stk Argument requiring alignment in vararg case (same as above comment.)
            //            Signature (int a0, int a1, int a2, struct {long} a3, ...)
            //
            // stk arg    a3             --> argOffs here will be 12 (r0-r2) but pre-spill will be 16.
            // ---- Caller SP ----
            // r3                        --> Stack slot is skipped in this case.
            // r2    int  a2
            // r1    int  a1
            // r0    int  a0
            //
            // Example 2: First Reg Argument requiring alignment in no-vararg case.
            //            Signature (struct {long} a0, struct {int} a1, int a2, int a3)
            //
            // stk arg                  --> argOffs here will be 12 {r0-r2} but pre-spill will be 16.
            // ---- Caller SP ----
            // r3    int             a2 --> pushed (not pre-spilled) for alignment of a0 by lvaInitUserArgs.
            // r2    struct { int }  a1
            // r0-r1 struct { long } a0
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef PROFILING_SUPPORTED
            // On Arm under profiler, r0-r3 are always prespilled on stack.
            // It is possible to have methods that accept only HFAs as parameters e.g. Signature(struct hfa1, struct
            // hfa2), in which case hfa1 and hfa2 will be en-registered in co-processor registers and will have an
            // argument offset less than size of preSpill.
            //
            // For this reason the following conditions are asserted when not under profiler.
            if (!compIsProfilerHookNeeded())
#endif
            {
                bool cond = ((info.compIsVarArgs || opts.compUseSoftFP) &&
                             // Does cur stk arg require double alignment?
                             ((varDsc->lvType == TYP_STRUCT && varDsc->lvStructDoubleAlign) ||
                              (varDsc->lvType == TYP_DOUBLE) || (varDsc->lvType == TYP_LONG))) ||
                            // Did first reg arg require alignment?
                            (codeGen->regSet.rsMaskPreSpillAlign & genRegMask(REG_ARG_LAST));

                noway_assert(cond);
                noway_assert(sizeofPreSpillRegArgs <=
                             argOffs + TARGET_POINTER_SIZE); // at most one register of alignment
            }
            argOffs = sizeofPreSpillRegArgs;
        }

        noway_assert(argOffs >= sizeofPreSpillRegArgs);
        int argOffsWithoutPreSpillRegArgs = argOffs - sizeofPreSpillRegArgs;

        switch (varDsc->lvType)
        {
            case TYP_STRUCT:
                if (!varDsc->lvStructDoubleAlign)
                    break;

                FALLTHROUGH;

            case TYP_DOUBLE:
            case TYP_LONG:
                // We must align up the argOffset to a multiple of 8
                argOffs =
                    roundUp((unsigned)argOffsWithoutPreSpillRegArgs, 2 * TARGET_POINTER_SIZE) + sizeofPreSpillRegArgs;
                break;

            default:
                // No alignment of argOffs required
                break;
        }
#endif // TARGET_ARM
        const bool     isFloatHfa   = (varDsc->lvIsHfa() && (varDsc->GetHfaType() == TYP_FLOAT));
        const unsigned argAlignment = eeGetArgSizeAlignment(varDsc->lvType, isFloatHfa);
        if (compMacOsArm64Abi())
        {
            argOffs = roundUp(argOffs, argAlignment);
        }

        assert((argSize % argAlignment) == 0);
        assert((argOffs % argAlignment) == 0);
        varDsc->SetStackOffset(argOffs);
    }

    // For struct promoted parameters we need to set the offsets for both LclVars.
    //
    // For a dependent promoted struct we also assign the struct fields stack offset
    CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(TARGET_64BIT)
    if ((varDsc->TypeGet() == TYP_LONG) && varDsc->lvPromoted)
    {
        noway_assert(varDsc->lvFieldCnt == 2);
        fieldVarNum = varDsc->lvFieldLclStart;
        lvaTable[fieldVarNum].SetStackOffset(varDsc->GetStackOffset());
        lvaTable[fieldVarNum + 1].SetStackOffset(varDsc->GetStackOffset() + genTypeSize(TYP_INT));
    }
    else
#endif // !defined(TARGET_64BIT)
        if (varDsc->lvPromotedStruct())
    {
        unsigned firstFieldNum = varDsc->lvFieldLclStart;
        for (unsigned i = 0; i < varDsc->lvFieldCnt; i++)
        {
            LclVarDsc* fieldVarDsc = lvaGetDesc(firstFieldNum + i);

            JITDUMP("Adjusting offset of dependent V%02u of arg V%02u: parent %u field %u net %u\n", lclNum,
                    firstFieldNum + i, varDsc->GetStackOffset(), fieldVarDsc->lvFldOffset,
                    varDsc->GetStackOffset() + fieldVarDsc->lvFldOffset);

            fieldVarDsc->SetStackOffset(varDsc->GetStackOffset() + fieldVarDsc->lvFldOffset);
        }
    }

    if (info.compArgOrder == Target::ARG_ORDER_R2L && !varDsc->lvIsRegArg)
    {
        argOffs += argSize;
    }

    return argOffs;
}
#endif // !UNIX_AMD64_ABI

//-----------------------------------------------------------------------------
// lvaAssingVirtualFrameOffsetsToLocals: compute the virtual stack offsets for
//  all elements on the stackframe.
//
// Notes:
//  Can be called multiple times. Early calls can be used to estimate various
//  frame offsets, but details may change.
//
void Compiler::lvaAssignVirtualFrameOffsetsToLocals()
{
    // (1) Account for things that are set up by the prolog and undone by the epilog.
    //
    int stkOffs              = 0;
    int originalFrameStkOffs = 0;
    int originalFrameSize    = 0;
    // codeGen->isFramePointerUsed is set in regalloc phase. Initialize it to a guess for pre-regalloc layout.
    if (lvaDoneFrameLayout <= PRE_REGALLOC_FRAME_LAYOUT)
    {
        codeGen->setFramePointerUsed(codeGen->isFramePointerRequired());
    }

#ifdef TARGET_ARM64
    // Decide where to save FP and LR registers. We store FP/LR registers at the bottom of the frame if there is
    // a frame pointer used (so we get positive offsets from the frame pointer to access locals), but not if we
    // need a GS cookie AND localloc is used, since we need the GS cookie to protect the saved return value,
    // and also the saved frame pointer. See CodeGen::genPushCalleeSavedRegisters() for more details about the
    // frame types. Since saving FP/LR at high addresses is a relatively rare case, force using it during stress.
    // (It should be legal to use these frame types for every frame).

    if (opts.compJitSaveFpLrWithCalleeSavedRegisters == 0)
    {
        // Default configuration
        codeGen->SetSaveFpLrWithAllCalleeSavedRegisters((getNeedsGSSecurityCookie() && compLocallocUsed) ||
                                                        compStressCompile(STRESS_GENERIC_VARN, 20));
    }
    else if (opts.compJitSaveFpLrWithCalleeSavedRegisters == 1)
    {
        codeGen->SetSaveFpLrWithAllCalleeSavedRegisters(false); // Disable using new frames
    }
    else if (opts.compJitSaveFpLrWithCalleeSavedRegisters == 2)
    {
        codeGen->SetSaveFpLrWithAllCalleeSavedRegisters(true); // Force using new frames
    }
#endif // TARGET_ARM64

#ifdef TARGET_XARCH
    // On x86/amd64, the return address has already been pushed by the call instruction in the caller.
    stkOffs -= TARGET_POINTER_SIZE; // return address;
    if (lvaRetAddrVar != BAD_VAR_NUM)
    {
        lvaTable[lvaRetAddrVar].SetStackOffset(stkOffs);
    }
#endif

    // If we are an OSR method, we "inherit" the frame of the original method
    //
    if (opts.IsOSR())
    {
        originalFrameSize    = info.compPatchpointInfo->TotalFrameSize();
        originalFrameStkOffs = stkOffs;
        stkOffs -= originalFrameSize;
    }

#ifdef TARGET_XARCH
    // TODO-AMD64-CQ: for X64 eventually this should be pushed with all the other
    // calleeregs.  When you fix this, you'll also need to fix
    // the assert at the bottom of this method
    if (codeGen->doubleAlignOrFramePointerUsed())
    {
        stkOffs -= REGSIZE_BYTES;
    }
#endif

    int  preSpillSize    = 0;
    bool mustDoubleAlign = false;

#ifdef TARGET_ARM
    mustDoubleAlign = true;
    preSpillSize    = genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
#else // !TARGET_ARM
#if DOUBLE_ALIGN
    if (genDoubleAlign())
    {
        mustDoubleAlign = true; // X86 only
    }
#endif
#endif // !TARGET_ARM

#ifdef TARGET_ARM64
    // If the frame pointer is used, then we'll save FP/LR at the bottom of the stack.
    // Otherwise, we won't store FP, and we'll store LR at the top, with the other callee-save
    // registers (if any).

    int initialStkOffs = 0;
    if (info.compIsVarArgs)
    {
        // For varargs we always save all of the integer register arguments
        // so that they are contiguous with the incoming stack arguments.
        initialStkOffs = MAX_REG_ARG * REGSIZE_BYTES;
        stkOffs -= initialStkOffs;
    }

    if (codeGen->IsSaveFpLrWithAllCalleeSavedRegisters() ||
        !isFramePointerUsed()) // Note that currently we always have a frame pointer
    {
        stkOffs -= compCalleeRegsPushed * REGSIZE_BYTES;
    }
    else
    {
        // Subtract off FP and LR.
        assert(compCalleeRegsPushed >= 2);
        stkOffs -= (compCalleeRegsPushed - 2) * REGSIZE_BYTES;
    }

#elif defined(TARGET_LOONGARCH64)

    // Subtract off FP and RA.
    assert(compCalleeRegsPushed >= 2);
    stkOffs -= (compCalleeRegsPushed - 2) * REGSIZE_BYTES;

#else // !TARGET_LOONGARCH64
#ifdef TARGET_ARM
    // On ARM32 LR is part of the pushed registers and is always stored at the
    // top.
    if (lvaRetAddrVar != BAD_VAR_NUM)
    {
        lvaTable[lvaRetAddrVar].SetStackOffset(stkOffs - REGSIZE_BYTES);
    }
#endif

    stkOffs -= compCalleeRegsPushed * REGSIZE_BYTES;
#endif // !TARGET_LOONGARCH64

    // (2) Account for the remainder of the frame
    //
    // From this point on the code must generally adjust both
    // stkOffs and the local frame size. The latter is done via:
    //
    //   lvaIncrementFrameSize -- for space not associated with a local var
    //   lvaAllocLocalAndSetVirtualOffset -- for space associated with a local var
    //
    // One exception to the above: OSR locals that have offsets within the Tier0
    // portion of the frame.
    //
    compLclFrameSize = 0;

#ifdef TARGET_AMD64
    // For methods with patchpoints, the Tier0 method must reserve
    // space for all the callee saves, as this area is shared with the
    // OSR method, and we have to anticipate that collectively the
    // Tier0 and OSR methods end up saving all callee saves.
    //
    // Currently this is x64 only.
    //
    if (doesMethodHavePatchpoints() || doesMethodHavePartialCompilationPatchpoints())
    {
        const unsigned regsPushed    = compCalleeRegsPushed + (codeGen->isFramePointerUsed() ? 1 : 0);
        const unsigned extraSlots    = genCountBits(RBM_OSR_INT_CALLEE_SAVED) - regsPushed;
        const unsigned extraSlotSize = extraSlots * REGSIZE_BYTES;

        JITDUMP("\nMethod has patchpoints and has %u callee saves.\n"
                "Reserving %u extra slots (%u bytes) for potential OSR method callee saves\n",
                regsPushed, extraSlots, extraSlotSize);

        stkOffs -= extraSlotSize;
        lvaIncrementFrameSize(extraSlotSize);
    }

    // In case of Amd64 compCalleeRegsPushed does not include float regs (Xmm6-xmm15) that
    // need to be pushed.  But Amd64 doesn't support push/pop of xmm registers.
    // Instead we need to allocate space for them on the stack and save them in prolog.
    // Therefore, we consider xmm registers being saved while computing stack offsets
    // but space for xmm registers is considered part of compLclFrameSize.
    // Notes
    //  1) We need to save the entire 128-bits of xmm register to stack, since amd64
    //     prolog unwind codes allow encoding of an instruction that stores the entire xmm reg
    //     at an offset relative to SP
    //  2) We adjust frame size so that SP is aligned at 16-bytes after pushing integer registers.
    //     This means while saving the first xmm register to its allocated stack location we might
    //     have to skip 8-bytes.  The reason for padding is to use efficient "movaps" to save/restore
    //     xmm registers to/from stack to match Jit64 codegen.  Without the aligning on 16-byte
    //     boundary we would have to use movups when offset turns out unaligned.  Movaps is more
    //     performant than movups.
    const unsigned calleeFPRegsSavedSize = genCountBits(compCalleeFPRegsSavedMask) * XMM_REGSIZE_BYTES;

    // For OSR the alignment pad computation should not take the original frame into account.
    // Original frame size includes the pseudo-saved RA and so is always = 8 mod 16.
    const int offsetForAlign = -(stkOffs + originalFrameSize);

    if ((calleeFPRegsSavedSize > 0) && ((offsetForAlign % XMM_REGSIZE_BYTES) != 0))
    {
        // Take care of alignment
        int alignPad = (int)AlignmentPad((unsigned)offsetForAlign, XMM_REGSIZE_BYTES);
        assert(alignPad != 0);
        stkOffs -= alignPad;
        lvaIncrementFrameSize(alignPad);
    }

    stkOffs -= calleeFPRegsSavedSize;
    lvaIncrementFrameSize(calleeFPRegsSavedSize);

    // Quirk for VS debug-launch scenario to work
    if (compVSQuirkStackPaddingNeeded > 0)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nAdding VS quirk stack padding of %d bytes between save-reg area and locals\n",
                   compVSQuirkStackPaddingNeeded);
        }
#endif // DEBUG

        stkOffs -= compVSQuirkStackPaddingNeeded;
        lvaIncrementFrameSize(compVSQuirkStackPaddingNeeded);
    }
#endif // TARGET_AMD64

#if defined(FEATURE_EH_FUNCLETS) && (defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64))
    if (lvaPSPSym != BAD_VAR_NUM)
    {
        // On ARM/ARM64, if we need a PSPSym, allocate it first, before anything else, including
        // padding (so we can avoid computing the same padding in the funclet
        // frame). Note that there is no special padding requirement for the PSPSym.
        noway_assert(codeGen->isFramePointerUsed()); // We need an explicit frame pointer
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaPSPSym, TARGET_POINTER_SIZE, stkOffs);
    }
#endif // FEATURE_EH_FUNCLETS && (TARGET_ARMARCH || TARGET_LOONGARCH64)

    if (mustDoubleAlign)
    {
        if (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)
        {
            // Allocate a pointer sized stack slot, since we may need to double align here
            // when lvaDoneFrameLayout == FINAL_FRAME_LAYOUT
            //
            lvaIncrementFrameSize(TARGET_POINTER_SIZE);
            stkOffs -= TARGET_POINTER_SIZE;

            // If we have any TYP_LONG, TYP_DOUBLE or double aligned structs
            // then we need to allocate a second pointer sized stack slot,
            // since we may need to double align that LclVar when we see it
            // in the loop below.  We will just always do this so that the
            // offsets that we calculate for the stack frame will always
            // be greater (or equal) to what they can be in the final layout.
            //
            lvaIncrementFrameSize(TARGET_POINTER_SIZE);
            stkOffs -= TARGET_POINTER_SIZE;
        }
        else // FINAL_FRAME_LAYOUT
        {
            if (((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) != 0)
            {
                lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                stkOffs -= TARGET_POINTER_SIZE;
            }
            // We should now have a double-aligned (stkOffs+preSpillSize)
            noway_assert(((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) == 0);
        }
    }

    if (lvaMonAcquired != BAD_VAR_NUM)
    {
        // For OSR we use the flag set up by the original method.
        //
        if (opts.IsOSR())
        {
            assert(info.compPatchpointInfo->HasMonitorAcquired());
            int originalOffset = info.compPatchpointInfo->MonitorAcquiredOffset();
            int offset         = originalFrameStkOffs + originalOffset;

            JITDUMP(
                "---OSR--- V%02u (on tier0 frame, monitor aquired) tier0 FP-rel offset %d tier0 frame offset %d new "
                "virt offset %d\n",
                lvaMonAcquired, originalOffset, originalFrameStkOffs, offset);

            lvaTable[lvaMonAcquired].SetStackOffset(offset);
        }
        else
        {
            // This var must go first, in what is called the 'frame header' for EnC so that it is
            // preserved when remapping occurs.  See vm\eetwain.cpp for detailed comment specifying frame
            // layout requirements for EnC to work.
            stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaMonAcquired, lvaLclSize(lvaMonAcquired), stkOffs);
        }
    }

#ifdef JIT32_GCENCODER
    if (lvaLocAllocSPvar != BAD_VAR_NUM)
    {
        noway_assert(codeGen->isFramePointerUsed()); // else offsets of locals of frameless methods will be incorrect
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaLocAllocSPvar, TARGET_POINTER_SIZE, stkOffs);
    }
#endif // JIT32_GCENCODER

    // For OSR methods, param type args are always reportable via the root method frame slot.
    // (see gcInfoBlockHdrSave) and so do not need a new slot on the frame.
    //
    // OSR methods may also be able to use the root frame kept alive this, if the root
    // method needed to report this.
    //
    // Inlining done under OSR may introduce new reporting, in which case the OSR frame
    // must allocate a slot.
    if (lvaReportParamTypeArg())
    {
#ifdef JIT32_GCENCODER
        noway_assert(codeGen->isFramePointerUsed());
#endif
        if (opts.IsOSR())
        {
            PatchpointInfo* ppInfo = info.compPatchpointInfo;
            assert(ppInfo->HasGenericContextArgOffset());
            const int originalOffset       = ppInfo->GenericContextArgOffset();
            lvaCachedGenericContextArgOffs = originalFrameStkOffs + originalOffset;
        }
        else
        {
            // For CORINFO_CALLCONV_PARAMTYPE (if needed)
            lvaIncrementFrameSize(TARGET_POINTER_SIZE);
            stkOffs -= TARGET_POINTER_SIZE;
            lvaCachedGenericContextArgOffs = stkOffs;
        }
    }
#ifndef JIT32_GCENCODER
    else if (lvaKeepAliveAndReportThis())
    {
        bool canUseExistingSlot = false;
        if (opts.IsOSR())
        {
            PatchpointInfo* ppInfo = info.compPatchpointInfo;
            if (ppInfo->HasKeptAliveThis())
            {
                const int originalOffset       = ppInfo->KeptAliveThisOffset();
                lvaCachedGenericContextArgOffs = originalFrameStkOffs + originalOffset;
                canUseExistingSlot             = true;
            }
        }

        if (!canUseExistingSlot)
        {
            // When "this" is also used as generic context arg.
            lvaIncrementFrameSize(TARGET_POINTER_SIZE);
            stkOffs -= TARGET_POINTER_SIZE;
            lvaCachedGenericContextArgOffs = stkOffs;
        }
    }
#endif

#if !defined(FEATURE_EH_FUNCLETS)
    /* If we need space for slots for shadow SP, reserve it now */
    if (ehNeedsShadowSPslots())
    {
        noway_assert(codeGen->isFramePointerUsed()); // else offsets of locals of frameless methods will be incorrect
        if (!lvaReportParamTypeArg())
        {
#ifndef JIT32_GCENCODER
            if (!lvaKeepAliveAndReportThis())
#endif
            {
                // In order to keep the gc info encoding smaller, the VM assumes that all methods with EH
                // have also saved space for a ParamTypeArg, so we need to do that here
                lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                stkOffs -= TARGET_POINTER_SIZE;
            }
        }
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaShadowSPslotsVar, lvaLclSize(lvaShadowSPslotsVar), stkOffs);
    }
#endif // !FEATURE_EH_FUNCLETS

    if (compGSReorderStackLayout)
    {
        assert(getNeedsGSSecurityCookie());

        if (!opts.IsOSR() || !info.compPatchpointInfo->HasSecurityCookie())
        {
            stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaGSSecurityCookie, lvaLclSize(lvaGSSecurityCookie), stkOffs);
        }
    }

    /*
        If we're supposed to track lifetimes of pointer temps, we'll
        assign frame offsets in the following order:

            non-pointer local variables (also untracked pointer variables)
                pointer local variables
                pointer temps
            non-pointer temps
     */

    enum Allocation
    {
        ALLOC_NON_PTRS                 = 0x1, // assign offsets to non-ptr
        ALLOC_PTRS                     = 0x2, // Second pass, assign offsets to tracked ptrs
        ALLOC_UNSAFE_BUFFERS           = 0x4,
        ALLOC_UNSAFE_BUFFERS_WITH_PTRS = 0x8
    };
    UINT alloc_order[5];

    unsigned int cur = 0;

    if (compGSReorderStackLayout)
    {
        noway_assert(getNeedsGSSecurityCookie());

        if (codeGen->isFramePointerUsed())
        {
            alloc_order[cur++] = ALLOC_UNSAFE_BUFFERS;
            alloc_order[cur++] = ALLOC_UNSAFE_BUFFERS_WITH_PTRS;
        }
    }

    bool tempsAllocated = false;

    if (lvaTempsHaveLargerOffsetThanVars() && !codeGen->isFramePointerUsed())
    {
        // Because we want the temps to have a larger offset than locals
        // and we're not using a frame pointer, we have to place the temps
        // above the vars.  Otherwise we place them after the vars (at the
        // bottom of the frame).
        noway_assert(!tempsAllocated);
        stkOffs        = lvaAllocateTemps(stkOffs, mustDoubleAlign);
        tempsAllocated = true;
    }

    alloc_order[cur++] = ALLOC_NON_PTRS;

    if (opts.compDbgEnC)
    {
        /* We will use just one pass, and assign offsets to all variables */
        alloc_order[cur - 1] |= ALLOC_PTRS;
        noway_assert(compGSReorderStackLayout == false);
    }
    else
    {
        alloc_order[cur++] = ALLOC_PTRS;
    }

    if (!codeGen->isFramePointerUsed() && compGSReorderStackLayout)
    {
        alloc_order[cur++] = ALLOC_UNSAFE_BUFFERS_WITH_PTRS;
        alloc_order[cur++] = ALLOC_UNSAFE_BUFFERS;
    }

    alloc_order[cur] = 0;

    noway_assert(cur < ArrLen(alloc_order));

    // Force first pass to happen
    UINT assignMore             = 0xFFFFFFFF;
    bool have_LclVarDoubleAlign = false;

    for (cur = 0; alloc_order[cur]; cur++)
    {
        if ((assignMore & alloc_order[cur]) == 0)
        {
            continue;
        }

        assignMore = 0;

        unsigned   lclNum;
        LclVarDsc* varDsc;

        for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
        {
            /* Ignore field locals of the promotion type PROMOTION_TYPE_FIELD_DEPENDENT.
               In other words, we will not calculate the "base" address of the struct local if
               the promotion type is PROMOTION_TYPE_FIELD_DEPENDENT.
            */
            if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
            {
                continue;
            }

#if FEATURE_FIXED_OUT_ARGS
            // The scratch mem is used for the outgoing arguments, and it must be absolutely last
            if (lclNum == lvaOutgoingArgSpaceVar)
            {
                continue;
            }
#endif

            bool allocateOnFrame = varDsc->lvOnFrame;

            if (varDsc->lvRegister && (lvaDoneFrameLayout == REGALLOC_FRAME_LAYOUT) &&
                ((varDsc->TypeGet() != TYP_LONG) || (varDsc->GetOtherReg() != REG_STK)))
            {
                allocateOnFrame = false;
            }

            // For OSR args and locals, we use the slots on the original frame.
            //
            // Note we must do this even for "non frame" locals, as we sometimes
            // will refer to their memory homes.
            if (lvaIsOSRLocal(lclNum))
            {
                if (varDsc->lvIsStructField)
                {
                    const unsigned parentLclNum         = varDsc->lvParentLcl;
                    const int      parentOriginalOffset = info.compPatchpointInfo->Offset(parentLclNum);
                    const int      offset = originalFrameStkOffs + parentOriginalOffset + varDsc->lvFldOffset;

                    JITDUMP("---OSR--- V%02u (promoted field of V%02u; on tier0 frame) tier0 FP-rel offset %d tier0 "
                            "frame offset %d field offset %d new virt offset "
                            "%d\n",
                            lclNum, parentLclNum, parentOriginalOffset, originalFrameStkOffs, varDsc->lvFldOffset,
                            offset);

                    lvaTable[lclNum].SetStackOffset(offset);
                }
                else
                {
                    // Add frampointer-relative offset of this OSR live local in the original frame
                    // to the offset of original frame in our new frame.
                    const int originalOffset = info.compPatchpointInfo->Offset(lclNum);
                    const int offset         = originalFrameStkOffs + originalOffset;

                    JITDUMP(
                        "---OSR--- V%02u (on tier0 frame) tier0 FP-rel offset %d tier0 frame offset %d new virt offset "
                        "%d\n",
                        lclNum, originalOffset, originalFrameStkOffs, offset);

                    lvaTable[lclNum].SetStackOffset(offset);
                }
                continue;
            }

            /* Ignore variables that are not on the stack frame */

            if (!allocateOnFrame)
            {
                /* For EnC, all variables have to be allocated space on the
                   stack, even though they may actually be enregistered. This
                   way, the frame layout can be directly inferred from the
                   locals-sig.
                 */

                if (!opts.compDbgEnC)
                {
                    continue;
                }
                else if (lclNum >= info.compLocalsCount)
                { // ignore temps for EnC
                    continue;
                }
            }
            else if (lvaGSSecurityCookie == lclNum && getNeedsGSSecurityCookie())
            {
                // Special case for OSR. If the original method had a cookie,
                // we use its slot on the original frame.
                if (opts.IsOSR() && info.compPatchpointInfo->HasSecurityCookie())
                {
                    int originalOffset = info.compPatchpointInfo->SecurityCookieOffset();
                    int offset         = originalFrameStkOffs + originalOffset;

                    JITDUMP("---OSR--- V%02u (on tier0 frame, security cookie) tier0 FP-rel offset %d tier0 frame "
                            "offset %d new "
                            "virt offset %d\n",
                            lclNum, originalOffset, originalFrameStkOffs, offset);

                    lvaTable[lclNum].SetStackOffset(offset);
                }

                continue;
            }

            // These need to be located as the very first variables (highest memory address)
            // and so they have already been assigned an offset
            if (
#if defined(FEATURE_EH_FUNCLETS)
                lclNum == lvaPSPSym ||
#else
                lclNum == lvaShadowSPslotsVar ||
#endif // FEATURE_EH_FUNCLETS
#ifdef JIT32_GCENCODER
                lclNum == lvaLocAllocSPvar ||
#endif // JIT32_GCENCODER
                lclNum == lvaRetAddrVar)
            {
                assert(varDsc->GetStackOffset() != BAD_STK_OFFS);
                continue;
            }

            if (lclNum == lvaMonAcquired)
            {
                continue;
            }

            // This should be low on the stack. Hence, it will be assigned later.
            if (lclNum == lvaStubArgumentVar)
            {
#ifdef JIT32_GCENCODER
                noway_assert(codeGen->isFramePointerUsed());
#endif
                continue;
            }

            // This should be low on the stack. Hence, it will be assigned later.
            if (lclNum == lvaInlinedPInvokeFrameVar)
            {
                noway_assert(codeGen->isFramePointerUsed());
                continue;
            }

            if (varDsc->lvIsParam)
            {
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)

                // On Windows AMD64 we can use the caller-reserved stack area that is already setup
                assert(varDsc->GetStackOffset() != BAD_STK_OFFS);
                continue;

#else // !TARGET_AMD64

                //  A register argument that is not enregistered ends up as
                //  a local variable which will need stack frame space.
                //
                if (!varDsc->lvIsRegArg)
                {
                    continue;
                }

#ifdef TARGET_ARM64
                if (info.compIsVarArgs && varDsc->GetArgReg() != theFixedRetBuffArgNum())
                {
                    // Stack offset to varargs (parameters) should point to home area which will be preallocated.
                    const unsigned regArgNum = genMapIntRegNumToRegArgNum(varDsc->GetArgReg());
                    varDsc->SetStackOffset(-initialStkOffs + regArgNum * REGSIZE_BYTES);
                    continue;
                }

#endif

#ifdef TARGET_ARM
                // On ARM we spill the registers in codeGen->regSet.rsMaskPreSpillRegArg
                // in the prolog, thus they don't need stack frame space.
                //
                if ((codeGen->regSet.rsMaskPreSpillRegs(false) & genRegMask(varDsc->GetArgReg())) != 0)
                {
                    assert(varDsc->GetStackOffset() != BAD_STK_OFFS);
                    continue;
                }
#endif

#endif // !TARGET_AMD64
            }

            /* Make sure the type is appropriate */

            if (varDsc->lvIsUnsafeBuffer && compGSReorderStackLayout)
            {
                if (varDsc->lvIsPtr)
                {
                    if ((alloc_order[cur] & ALLOC_UNSAFE_BUFFERS_WITH_PTRS) == 0)
                    {
                        assignMore |= ALLOC_UNSAFE_BUFFERS_WITH_PTRS;
                        continue;
                    }
                }
                else
                {
                    if ((alloc_order[cur] & ALLOC_UNSAFE_BUFFERS) == 0)
                    {
                        assignMore |= ALLOC_UNSAFE_BUFFERS;
                        continue;
                    }
                }
            }
            else if (varTypeIsGC(varDsc->TypeGet()) && varDsc->lvTracked)
            {
                if ((alloc_order[cur] & ALLOC_PTRS) == 0)
                {
                    assignMore |= ALLOC_PTRS;
                    continue;
                }
            }
            else
            {
                if ((alloc_order[cur] & ALLOC_NON_PTRS) == 0)
                {
                    assignMore |= ALLOC_NON_PTRS;
                    continue;
                }
            }

            /* Need to align the offset? */

            if (mustDoubleAlign && (varDsc->lvType == TYP_DOUBLE // Align doubles for ARM and x86
#ifdef TARGET_ARM
                                    || varDsc->lvType == TYP_LONG // Align longs for ARM
#endif
#ifndef TARGET_64BIT
                                    || varDsc->lvStructDoubleAlign // Align when lvStructDoubleAlign is true
#endif                                                             // !TARGET_64BIT
                                    ))
            {
                noway_assert((compLclFrameSize % TARGET_POINTER_SIZE) == 0);

                if ((lvaDoneFrameLayout != FINAL_FRAME_LAYOUT) && !have_LclVarDoubleAlign)
                {
                    // If this is the first TYP_LONG, TYP_DOUBLE or double aligned struct
                    // then we have seen in this loop then we allocate a pointer sized
                    // stack slot since we may need to double align this LclVar
                    // when lvaDoneFrameLayout == FINAL_FRAME_LAYOUT
                    //
                    lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                    stkOffs -= TARGET_POINTER_SIZE;
                }
                else
                {
                    if (((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) != 0)
                    {
                        lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                        stkOffs -= TARGET_POINTER_SIZE;
                    }

                    // We should now have a double-aligned (stkOffs+preSpillSize)
                    noway_assert(((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) == 0);
                }

                // Remember that we had to double align a LclVar
                have_LclVarDoubleAlign = true;
            }

            // Reserve the stack space for this variable
            stkOffs = lvaAllocLocalAndSetVirtualOffset(lclNum, lvaLclSize(lclNum), stkOffs);
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64)
            // If we have an incoming register argument that has a struct promoted field
            // then we need to copy the lvStkOff (the stack home) from the reg arg to the field lclvar
            //
            if (varDsc->lvIsRegArg && varDsc->lvPromotedStruct())
            {
                unsigned firstFieldNum = varDsc->lvFieldLclStart;
                for (unsigned i = 0; i < varDsc->lvFieldCnt; i++)
                {
                    LclVarDsc* fieldVarDsc = lvaGetDesc(firstFieldNum + i);
                    fieldVarDsc->SetStackOffset(varDsc->GetStackOffset() + fieldVarDsc->lvFldOffset);
                }
            }
#ifdef TARGET_ARM
            // If we have an incoming register argument that has a promoted long
            // then we need to copy the lvStkOff (the stack home) from the reg arg to the field lclvar
            //
            else if (varDsc->lvIsRegArg && varDsc->lvPromoted)
            {
                assert(varTypeIsLong(varDsc) && (varDsc->lvFieldCnt == 2));

                unsigned fieldVarNum = varDsc->lvFieldLclStart;
                lvaTable[fieldVarNum].SetStackOffset(varDsc->GetStackOffset());
                lvaTable[fieldVarNum + 1].SetStackOffset(varDsc->GetStackOffset() + 4);
            }
#endif // TARGET_ARM
#endif // TARGET_ARM64 || TARGET_LOONGARCH64
        }
    }

    if (getNeedsGSSecurityCookie() && !compGSReorderStackLayout)
    {
        if (!opts.IsOSR() || !info.compPatchpointInfo->HasSecurityCookie())
        {
            // LOCALLOC used, but we have no unsafe buffer.  Allocated cookie last, close to localloc buffer.
            stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaGSSecurityCookie, lvaLclSize(lvaGSSecurityCookie), stkOffs);
        }
    }

    if (tempsAllocated == false)
    {
        /*-------------------------------------------------------------------------
         *
         * Now the temps
         *
         *-------------------------------------------------------------------------
         */
        stkOffs = lvaAllocateTemps(stkOffs, mustDoubleAlign);
    }

    /*-------------------------------------------------------------------------
     *
     * Now do some final stuff
     *
     *-------------------------------------------------------------------------
     */

    // lvaInlinedPInvokeFrameVar and lvaStubArgumentVar need to be assigned last
    // Important: The stack walker depends on lvaStubArgumentVar immediately
    // following lvaInlinedPInvokeFrameVar in the frame.

    if (lvaStubArgumentVar != BAD_VAR_NUM)
    {
#ifdef JIT32_GCENCODER
        noway_assert(codeGen->isFramePointerUsed());
#endif
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaStubArgumentVar, lvaLclSize(lvaStubArgumentVar), stkOffs);
    }

    if (lvaInlinedPInvokeFrameVar != BAD_VAR_NUM)
    {
        noway_assert(codeGen->isFramePointerUsed());
        stkOffs =
            lvaAllocLocalAndSetVirtualOffset(lvaInlinedPInvokeFrameVar, lvaLclSize(lvaInlinedPInvokeFrameVar), stkOffs);
    }

    if (mustDoubleAlign)
    {
        if (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)
        {
            // Allocate a pointer sized stack slot, since we may need to double align here
            // when lvaDoneFrameLayout == FINAL_FRAME_LAYOUT
            //
            lvaIncrementFrameSize(TARGET_POINTER_SIZE);
            stkOffs -= TARGET_POINTER_SIZE;

            if (have_LclVarDoubleAlign)
            {
                // If we have any TYP_LONG, TYP_DOUBLE or double aligned structs
                // the we need to allocate a second pointer sized stack slot,
                // since we may need to double align the last LclVar that we saw
                // in the loop above. We do this so that the offsets that we
                // calculate for the stack frame are always greater than they will
                // be in the final layout.
                //
                lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                stkOffs -= TARGET_POINTER_SIZE;
            }
        }
        else // FINAL_FRAME_LAYOUT
        {
            if (((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) != 0)
            {
                lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                stkOffs -= TARGET_POINTER_SIZE;
            }
            // We should now have a double-aligned (stkOffs+preSpillSize)
            noway_assert(((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) == 0);
        }
    }

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_AMD64)
    if (lvaPSPSym != BAD_VAR_NUM)
    {
        // On AMD64, if we need a PSPSym, allocate it last, immediately above the outgoing argument
        // space. Any padding will be higher on the stack than this
        // (including the padding added by lvaAlignFrame()).
        noway_assert(codeGen->isFramePointerUsed()); // We need an explicit frame pointer
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaPSPSym, TARGET_POINTER_SIZE, stkOffs);
    }
#endif // FEATURE_EH_FUNCLETS && defined(TARGET_AMD64)

#ifdef TARGET_ARM64
    if (!codeGen->IsSaveFpLrWithAllCalleeSavedRegisters() &&
        isFramePointerUsed()) // Note that currently we always have a frame pointer
    {
        // Create space for saving FP and LR.
        stkOffs -= 2 * REGSIZE_BYTES;
    }
#endif // TARGET_ARM64

#if defined(TARGET_LOONGARCH64)
    assert(isFramePointerUsed()); // Note that currently we always have a frame pointer
    stkOffs -= 2 * REGSIZE_BYTES;
#endif // TARGET_LOONGARCH64

#if FEATURE_FIXED_OUT_ARGS
    if (lvaOutgoingArgSpaceSize > 0)
    {
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI) // No 4 slots for outgoing params on System V.
        noway_assert(lvaOutgoingArgSpaceSize >= (4 * TARGET_POINTER_SIZE));
#endif
        noway_assert((lvaOutgoingArgSpaceSize % TARGET_POINTER_SIZE) == 0);

        // Give it a value so we can avoid asserts in CHK builds.
        // Since this will always use an SP relative offset of zero
        // at the end of lvaFixVirtualFrameOffsets, it will be set to absolute '0'

        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaOutgoingArgSpaceVar, lvaLclSize(lvaOutgoingArgSpaceVar), stkOffs);
    }
#endif // FEATURE_FIXED_OUT_ARGS

    // compLclFrameSize equals our negated virtual stack offset minus the pushed registers and return address
    // and the pushed frame pointer register which for some strange reason isn't part of 'compCalleeRegsPushed'.
    int pushedCount = compCalleeRegsPushed;

#ifdef TARGET_ARM64
    if (info.compIsVarArgs)
    {
        pushedCount += MAX_REG_ARG;
    }
#endif

#ifdef TARGET_XARCH
    if (codeGen->doubleAlignOrFramePointerUsed())
    {
        pushedCount += 1; // pushed EBP (frame pointer)
    }
    pushedCount += 1; // pushed PC (return address)
#endif

    noway_assert(compLclFrameSize + originalFrameSize ==
                 (unsigned)-(stkOffs + (pushedCount * (int)TARGET_POINTER_SIZE)));
}

int Compiler::lvaAllocLocalAndSetVirtualOffset(unsigned lclNum, unsigned size, int stkOffs)
{
    noway_assert(lclNum != BAD_VAR_NUM);

#ifdef TARGET_64BIT
    // Before final frame layout, assume the worst case, that every >=8 byte local will need
    // maximum padding to be aligned. This is because we generate code based on the stack offset
    // computed during tentative frame layout. These offsets cannot get bigger during final
    // frame layout, as that would possibly require different code generation (for example,
    // using a 4-byte offset instead of a 1-byte offset in an instruction). The offsets can get
    // smaller. It is possible there is different alignment at the point locals are allocated
    // between tentative and final frame layout which would introduce padding between locals
    // and thus increase the offset (from the stack pointer) of one of the locals. Hence the
    // need to assume the worst alignment before final frame layout.
    // We could probably improve this by sorting all the objects by alignment,
    // such that all 8 byte objects are together, 4 byte objects are together, etc., which
    // would require at most one alignment padding per group.
    //
    // TYP_SIMD structs locals have alignment preference given by getSIMDTypeAlignment() for
    // better performance.
    if ((size >= 8) && ((lvaDoneFrameLayout != FINAL_FRAME_LAYOUT) || ((stkOffs % 8) != 0)
#if defined(FEATURE_SIMD) && ALIGN_SIMD_TYPES
                        || lclVarIsSIMDType(lclNum)
#endif
                            ))
    {
        // Note that stack offsets are negative or equal to zero
        assert(stkOffs <= 0);

        // alignment padding
        unsigned pad = 0;
#if defined(FEATURE_SIMD) && ALIGN_SIMD_TYPES
        if (lclVarIsSIMDType(lclNum) && !lvaIsImplicitByRefLocal(lclNum))
        {
            int alignment = getSIMDTypeAlignment(lvaTable[lclNum].lvType);

            if (stkOffs % alignment != 0)
            {
                if (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)
                {
                    pad = alignment - 1;
                    // Note that all the objects will probably be misaligned, but we'll fix that in final layout.
                }
                else
                {
                    pad = alignment + (stkOffs % alignment); // +1 to +(alignment-1) bytes
                }
            }
        }
        else
#endif // FEATURE_SIMD && ALIGN_SIMD_TYPES
        {
            if (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)
            {
                pad = 7;
                // Note that all the objects will probably be misaligned, but we'll fix that in final layout.
            }
            else
            {
                pad = 8 + (stkOffs % 8); // +1 to +7 bytes
            }
        }
        // Will the pad ever be anything except 4? Do we put smaller-than-4-sized objects on the stack?
        lvaIncrementFrameSize(pad);
        stkOffs -= pad;

#ifdef DEBUG
        if (verbose)
        {
            printf("Pad ");
            gtDispLclVar(lclNum, /*pad*/ false);
            printf(", size=%d, stkOffs=%c0x%x, pad=%d\n", size, stkOffs < 0 ? '-' : '+',
                   stkOffs < 0 ? -stkOffs : stkOffs, pad);
        }
#endif
    }
#endif // TARGET_64BIT

    /* Reserve space on the stack by bumping the frame size */

    lvaIncrementFrameSize(size);
    stkOffs -= size;
    lvaTable[lclNum].SetStackOffset(stkOffs);

#ifdef DEBUG
    if (verbose)
    {
        printf("Assign ");
        gtDispLclVar(lclNum, /*pad*/ false);
        printf(", size=%d, stkOffs=%c0x%x\n", size, stkOffs < 0 ? '-' : '+', stkOffs < 0 ? -stkOffs : stkOffs);
    }
#endif

    return stkOffs;
}

#ifdef TARGET_AMD64
/*****************************************************************************
 *  lvaIsCalleeSavedIntRegCountEven() :  returns true if the number of integer registers
 *  pushed onto stack is even including RBP if used as frame pointer
 *
 *  Note that this excludes return address (PC) pushed by caller.  To know whether
 *  the SP offset after pushing integer registers is aligned, we need to take
 *  negation of this routine.
 */
bool Compiler::lvaIsCalleeSavedIntRegCountEven()
{
    unsigned regsPushed = compCalleeRegsPushed + (codeGen->isFramePointerUsed() ? 1 : 0);
    return (regsPushed % (16 / REGSIZE_BYTES)) == 0;
}
#endif // TARGET_AMD64

/*****************************************************************************
 *  lvaAlignFrame() :  After allocating everything on the frame, reserve any
 *  extra space needed to keep the frame aligned
 */
void Compiler::lvaAlignFrame()
{
#if defined(TARGET_AMD64)

    // Leaf frames do not need full alignment, but the unwind info is smaller if we
    // are at least 8 byte aligned (and we assert as much)
    if ((compLclFrameSize % 8) != 0)
    {
        lvaIncrementFrameSize(8 - (compLclFrameSize % 8));
    }
    else if (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)
    {
        // If we are not doing final layout, we don't know the exact value of compLclFrameSize
        // and thus do not know how much we will need to add in order to be aligned.
        // We add 8 so compLclFrameSize is still a multiple of 8.
        lvaIncrementFrameSize(8);
    }
    assert((compLclFrameSize % 8) == 0);

    // Ensure that the stack is always 16-byte aligned by grabbing an unused QWORD
    // if needed, but off by 8 because of the return value.
    // And don't forget that compCalleeRegsPused does *not* include RBP if we are
    // using it as the frame pointer.
    //
    bool regPushedCountAligned = lvaIsCalleeSavedIntRegCountEven();
    bool lclFrameSizeAligned   = (compLclFrameSize % 16) == 0;

    // If this isn't the final frame layout, assume we have to push an extra QWORD
    // Just so the offsets are true upper limits.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef UNIX_AMD64_ABI
    // The compNeedToAlignFrame flag  is indicating if there is a need to align the frame.
    // On AMD64-Windows, if there are calls, 4 slots for the outgoing ars are allocated, except for
    // FastTailCall. This slots makes the frame size non-zero, so alignment logic will be called.
    // On AMD64-Unix, there are no such slots. There is a possibility to have calls in the method with frame size of 0.
    // The frame alignment logic won't kick in. This flags takes care of the AMD64-Unix case by remembering that there
    // are calls and making sure the frame alignment logic is executed.
    bool stackNeedsAlignment = (compLclFrameSize != 0 || opts.compNeedToAlignFrame);
#else  // !UNIX_AMD64_ABI
    bool stackNeedsAlignment = compLclFrameSize != 0;
#endif // !UNIX_AMD64_ABI
    if ((!codeGen->isFramePointerUsed() && (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)) ||
        (stackNeedsAlignment && (regPushedCountAligned == lclFrameSizeAligned)))
    {
        lvaIncrementFrameSize(REGSIZE_BYTES);
    }

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

    // The stack on ARM64/LoongArch64 must be 16 byte aligned.

    // First, align up to 8.
    if ((compLclFrameSize % 8) != 0)
    {
        lvaIncrementFrameSize(8 - (compLclFrameSize % 8));
    }
    else if (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)
    {
        // If we are not doing final layout, we don't know the exact value of compLclFrameSize
        // and thus do not know how much we will need to add in order to be aligned.
        // We add 8 so compLclFrameSize is still a multiple of 8.
        lvaIncrementFrameSize(8);
    }
    assert((compLclFrameSize % 8) == 0);

    // Ensure that the stack is always 16-byte aligned by grabbing an unused QWORD
    // if needed.
    bool regPushedCountAligned = (compCalleeRegsPushed % (16 / REGSIZE_BYTES)) == 0;
    bool lclFrameSizeAligned   = (compLclFrameSize % 16) == 0;

    // If this isn't the final frame layout, assume we have to push an extra QWORD
    // Just so the offsets are true upper limits.
    if ((lvaDoneFrameLayout != FINAL_FRAME_LAYOUT) || (regPushedCountAligned != lclFrameSizeAligned))
    {
        lvaIncrementFrameSize(REGSIZE_BYTES);
    }

#elif defined(TARGET_ARM)

    // Ensure that stack offsets will be double-aligned by grabbing an unused DWORD if needed.
    //
    bool lclFrameSizeAligned   = (compLclFrameSize % sizeof(double)) == 0;
    bool regPushedCountAligned = ((compCalleeRegsPushed + genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true))) %
                                  (sizeof(double) / TARGET_POINTER_SIZE)) == 0;

    if (regPushedCountAligned != lclFrameSizeAligned)
    {
        lvaIncrementFrameSize(TARGET_POINTER_SIZE);
    }

#elif defined(TARGET_X86)

#if DOUBLE_ALIGN
    if (genDoubleAlign())
    {
        // Double Frame Alignment for x86 is handled in Compiler::lvaAssignVirtualFrameOffsetsToLocals()

        if (compLclFrameSize == 0)
        {
            // This can only happen with JitStress=1 or JitDoubleAlign=2
            lvaIncrementFrameSize(TARGET_POINTER_SIZE);
        }
    }
#endif

    if (STACK_ALIGN > REGSIZE_BYTES)
    {
        if (lvaDoneFrameLayout != FINAL_FRAME_LAYOUT)
        {
            // If we are not doing final layout, we don't know the exact value of compLclFrameSize
            // and thus do not know how much we will need to add in order to be aligned.
            // We add the maximum pad that we could ever have (which is 12)
            lvaIncrementFrameSize(STACK_ALIGN - REGSIZE_BYTES);
        }

        // Align the stack with STACK_ALIGN value.
        int  adjustFrameSize = compLclFrameSize;
#if defined(UNIX_X86_ABI)
        bool isEbpPushed     = codeGen->isFramePointerUsed();
#if DOUBLE_ALIGN
        isEbpPushed |= genDoubleAlign();
#endif
        // we need to consider spilled register(s) plus return address and/or EBP
        int adjustCount = compCalleeRegsPushed + 1 + (isEbpPushed ? 1 : 0);
        adjustFrameSize += (adjustCount * REGSIZE_BYTES) % STACK_ALIGN;
#endif
        if ((adjustFrameSize % STACK_ALIGN) != 0)
        {
            lvaIncrementFrameSize(STACK_ALIGN - (adjustFrameSize % STACK_ALIGN));
        }
    }

#else
    NYI("TARGET specific lvaAlignFrame");
#endif // !TARGET_AMD64
}

/*****************************************************************************
 *  lvaAssignFrameOffsetsToPromotedStructs() :  Assign offsets to fields
 *  within a promoted struct (worker for lvaAssignFrameOffsets).
 */
void Compiler::lvaAssignFrameOffsetsToPromotedStructs()
{
    LclVarDsc* varDsc = lvaTable;
    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++, varDsc++)
    {
        // For promoted struct fields that are params, we will
        // assign their offsets in lvaAssignVirtualFrameOffsetToArg().
        // This is not true for the System V systems since there is no
        // outgoing args space. Assign the dependently promoted fields properly.
        //
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(UNIX_AMD64_ABI) || defined(TARGET_ARM) || defined(TARGET_X86)
        // ARM: lo/hi parts of a promoted long arg need to be updated.
        //
        // For System V platforms there is no outgoing args space.
        //
        // For System V and x86, a register passed struct arg is homed on the stack in a separate local var.
        // The offset of these structs is already calculated in lvaAssignVirtualFrameOffsetToArg methos.
        // Make sure the code below is not executed for these structs and the offset is not changed.
        //
        const bool mustProcessParams = true;
#else
        // OSR must also assign offsets here.
        //
        const bool mustProcessParams = opts.IsOSR();
#endif // defined(UNIX_AMD64_ABI) || defined(TARGET_ARM) || defined(TARGET_X86)

        if (varDsc->lvIsStructField && (!varDsc->lvIsParam || mustProcessParams))
        {
            LclVarDsc*       parentvarDsc  = lvaGetDesc(varDsc->lvParentLcl);
            lvaPromotionType promotionType = lvaGetPromotionType(parentvarDsc);

            if (promotionType == PROMOTION_TYPE_INDEPENDENT)
            {
                // The stack offset for these field locals must have been calculated
                // by the normal frame offset assignment.
                continue;
            }
            else
            {
                noway_assert(promotionType == PROMOTION_TYPE_DEPENDENT);
                noway_assert(varDsc->lvOnFrame);
                if (parentvarDsc->lvOnFrame)
                {
                    JITDUMP("Adjusting offset of dependent V%02u of V%02u: parent %u field %u net %u\n", lclNum,
                            varDsc->lvParentLcl, parentvarDsc->GetStackOffset(), varDsc->lvFldOffset,
                            parentvarDsc->GetStackOffset() + varDsc->lvFldOffset);
                    varDsc->SetStackOffset(parentvarDsc->GetStackOffset() + varDsc->lvFldOffset);
                }
                else
                {
                    varDsc->lvOnFrame = false;
                    noway_assert(varDsc->lvRefCnt() == 0);
                }
            }
        }
    }
}

/*****************************************************************************
 *  lvaAllocateTemps() :  Assign virtual offsets to temps (always negative).
 */
int Compiler::lvaAllocateTemps(int stkOffs, bool mustDoubleAlign)
{
    unsigned spillTempSize = 0;

    if (lvaDoneFrameLayout == FINAL_FRAME_LAYOUT)
    {
        int preSpillSize = 0;
#ifdef TARGET_ARM
        preSpillSize = genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true)) * TARGET_POINTER_SIZE;
#endif

        /* Allocate temps */

        assert(codeGen->regSet.tmpAllFree());

        for (TempDsc* temp = codeGen->regSet.tmpListBeg(); temp != nullptr; temp = codeGen->regSet.tmpListNxt(temp))
        {
            var_types tempType = temp->tdTempType();
            unsigned  size     = temp->tdTempSize();

            /* Figure out and record the stack offset of the temp */

            /* Need to align the offset? */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_64BIT
            if (varTypeIsGC(tempType) && ((stkOffs % TARGET_POINTER_SIZE) != 0))
            {
                // Calculate 'pad' as the number of bytes to align up 'stkOffs' to be a multiple of TARGET_POINTER_SIZE
                // In practice this is really just a fancy way of writing 4. (as all stack locations are at least 4-byte
                // aligned). Note stkOffs is always negative, so (stkOffs % TARGET_POINTER_SIZE) yields a negative
                // value.
                //
                int alignPad = (int)AlignmentPad((unsigned)-stkOffs, TARGET_POINTER_SIZE);

                spillTempSize += alignPad;
                lvaIncrementFrameSize(alignPad);
                stkOffs -= alignPad;

                noway_assert((stkOffs % TARGET_POINTER_SIZE) == 0);
            }
#endif

            if (mustDoubleAlign && (tempType == TYP_DOUBLE)) // Align doubles for x86 and ARM
            {
                noway_assert((compLclFrameSize % TARGET_POINTER_SIZE) == 0);

                if (((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) != 0)
                {
                    spillTempSize += TARGET_POINTER_SIZE;
                    lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                    stkOffs -= TARGET_POINTER_SIZE;
                }
                // We should now have a double-aligned (stkOffs+preSpillSize)
                noway_assert(((stkOffs + preSpillSize) % (2 * TARGET_POINTER_SIZE)) == 0);
            }

            spillTempSize += size;
            lvaIncrementFrameSize(size);
            stkOffs -= size;
            temp->tdSetTempOffs(stkOffs);
        }
#ifdef TARGET_ARM
        // Only required for the ARM platform that we have an accurate estimate for the spillTempSize
        noway_assert(spillTempSize <= lvaGetMaxSpillTempSize());
#endif
    }
    else // We haven't run codegen, so there are no Spill temps yet!
    {
        unsigned size = lvaGetMaxSpillTempSize();

        lvaIncrementFrameSize(size);
        stkOffs -= size;
    }

    return stkOffs;
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Dump the register a local is in right now. It is only the current location, since the location changes and it
 *  is updated throughout code generation based on LSRA register assignments.
 */

void Compiler::lvaDumpRegLocation(unsigned lclNum)
{
    const LclVarDsc* varDsc = lvaGetDesc(lclNum);

#ifdef TARGET_ARM
    if (varDsc->TypeGet() == TYP_DOUBLE)
    {
        // The assigned registers are `lvRegNum:RegNext(lvRegNum)`
        printf("%3s:%-3s    ", getRegName(varDsc->GetRegNum()), getRegName(REG_NEXT(varDsc->GetRegNum())));
    }
    else
#endif // TARGET_ARM
    {
        printf("%3s        ", getRegName(varDsc->GetRegNum()));
    }
}

/*****************************************************************************
 *
 *  Dump the frame location assigned to a local.
 *  It's the home location, even though the variable doesn't always live
 *  in its home location.
 */

void Compiler::lvaDumpFrameLocation(unsigned lclNum)
{
    int       offset;
    regNumber baseReg;

#ifdef TARGET_ARM
    offset = lvaFrameAddress(lclNum, compLocallocUsed, &baseReg, 0, /* isFloatUsage */ false);
#else
    bool EBPbased;
    offset  = lvaFrameAddress(lclNum, &EBPbased);
    baseReg = EBPbased ? REG_FPBASE : REG_SPBASE;
#endif

    printf("[%2s%1s%02XH]  ", getRegName(baseReg), (offset < 0 ? "-" : "+"), (offset < 0 ? -offset : offset));
}

/*****************************************************************************
 *
 *  dump a single lvaTable entry
 */

void Compiler::lvaDumpEntry(unsigned lclNum, FrameLayoutState curState, size_t refCntWtdWidth)
{
    LclVarDsc* varDsc = lvaGetDesc(lclNum);
    var_types  type   = varDsc->TypeGet();

    if (curState == INITIAL_FRAME_LAYOUT)
    {
        printf(";  ");
        gtDispLclVar(lclNum);

        printf(" %7s ", varTypeName(type));
        gtDispLclVarStructType(lclNum);
    }
    else
    {
        if (varDsc->lvRefCnt() == 0)
        {
            // Print this with a special indicator that the variable is unused. Even though the
            // variable itself is unused, it might be a struct that is promoted, so seeing it
            // can be useful when looking at the promoted struct fields. It's also weird to see
            // missing var numbers if these aren't printed.
            printf(";* ");
        }
#if FEATURE_FIXED_OUT_ARGS
        // Since lvaOutgoingArgSpaceSize is a PhasedVar we can't read it for Dumping until
        // after we set it to something.
        else if ((lclNum == lvaOutgoingArgSpaceVar) && lvaOutgoingArgSpaceSize.HasFinalValue() &&
                 (lvaOutgoingArgSpaceSize == 0))
        {
            // Similar to above; print this anyway.
            printf(";# ");
        }
#endif // FEATURE_FIXED_OUT_ARGS
        else
        {
            printf(";  ");
        }

        gtDispLclVar(lclNum);

        printf("[V%02u", lclNum);
        if (varDsc->lvTracked)
        {
            printf(",T%02u]", varDsc->lvVarIndex);
        }
        else
        {
            printf("    ]");
        }

        printf(" (%3u,%*s)", varDsc->lvRefCnt(), (int)refCntWtdWidth, refCntWtd2str(varDsc->lvRefCntWtd()));

        printf(" %7s ", varTypeName(type));
        if (genTypeSize(type) == 0)
        {
            printf("(%2d) ", lvaLclSize(lclNum));
        }
        else
        {
            printf(" ->  ");
        }

        // The register or stack location field is 11 characters wide.
        if ((varDsc->lvRefCnt() == 0) && !varDsc->lvImplicitlyReferenced)
        {
            printf("zero-ref   ");
        }
        else if (varDsc->lvRegister != 0)
        {
            // It's always a register, and always in the same register.
            lvaDumpRegLocation(lclNum);
        }
        else if (varDsc->lvOnFrame == 0)
        {
            printf("registers  ");
        }
        else
        {
            // For RyuJIT backend, it might be in a register part of the time, but it will definitely have a stack home
            // location. Otherwise, it's always on the stack.
            if (lvaDoneFrameLayout != NO_FRAME_LAYOUT)
            {
                lvaDumpFrameLocation(lclNum);
            }
        }
    }

    if (varDsc->lvIsHfa())
    {
        printf(" HFA(%s) ", varTypeName(varDsc->GetHfaType()));
    }

    if (varDsc->lvDoNotEnregister)
    {
        printf(" do-not-enreg[");
        if (varDsc->IsAddressExposed())
        {
            printf("X");
        }
        if (varDsc->IsHiddenBufferStructArg())
        {
            printf("H");
        }
        if (varTypeIsStruct(varDsc))
        {
            printf("S");
        }
        if (varDsc->GetDoNotEnregReason() == DoNotEnregisterReason::VMNeedsStackAddr)
        {
            printf("V");
        }
        if (lvaEnregEHVars && varDsc->lvLiveInOutOfHndlr)
        {
            printf("%c", varDsc->lvSingleDefDisqualifyReason);
        }
        if (varDsc->GetDoNotEnregReason() == DoNotEnregisterReason::LocalField)
        {
            printf("F");
        }
        if (varDsc->GetDoNotEnregReason() == DoNotEnregisterReason::BlockOp)
        {
            printf("B");
        }
        if (varDsc->lvIsMultiRegArg)
        {
            printf("A");
        }
        if (varDsc->lvIsMultiRegRet)
        {
            printf("R");
        }
#ifdef JIT32_GCENCODER
        if (varDsc->lvPinned)
            printf("P");
#endif // JIT32_GCENCODER
        printf("]");
    }

    if (varDsc->lvIsMultiRegArg)
    {
        printf(" multireg-arg");
    }
    if (varDsc->lvIsMultiRegRet)
    {
        printf(" multireg-ret");
    }
    if (varDsc->lvMustInit)
    {
        printf(" must-init");
    }
    if (varDsc->IsAddressExposed())
    {
        printf(" addr-exposed");
    }
    if (varDsc->IsHiddenBufferStructArg())
    {
        printf(" hidden-struct-arg");
    }
    if (varDsc->lvHasLdAddrOp)
    {
        printf(" ld-addr-op");
    }
    if (varDsc->lvVerTypeInfo.IsThisPtr())
    {
        printf(" this");
    }
    if (varDsc->lvPinned)
    {
        printf(" pinned");
    }
    if (varDsc->lvStackByref)
    {
        printf(" stack-byref");
    }
    if (varDsc->lvClassHnd != NO_CLASS_HANDLE)
    {
        printf(" class-hnd");
    }
    if (varDsc->lvClassIsExact)
    {
        printf(" exact");
    }
    if (varDsc->lvLiveInOutOfHndlr)
    {
        printf(" EH-live");
    }
    if (varDsc->lvSpillAtSingleDef)
    {
        printf(" spill-single-def");
    }
    else if (varDsc->lvSingleDefRegCandidate)
    {
        printf(" single-def");
    }
    if (lvaIsOSRLocal(lclNum) && varDsc->lvOnFrame)
    {
        printf(" tier0-frame");
    }

#ifndef TARGET_64BIT
    if (varDsc->lvStructDoubleAlign)
        printf(" double-align");
#endif // !TARGET_64BIT
    if (varDsc->lvOverlappingFields)
    {
        printf(" overlapping-fields");
    }

    if (compGSReorderStackLayout && !varDsc->lvRegister)
    {
        if (varDsc->lvIsPtr)
        {
            printf(" ptr");
        }
        if (varDsc->lvIsUnsafeBuffer)
        {
            printf(" unsafe-buffer");
        }
    }
    if (varDsc->lvIsStructField)
    {
        LclVarDsc* parentvarDsc = lvaGetDesc(varDsc->lvParentLcl);
#if !defined(TARGET_64BIT)
        if (varTypeIsLong(parentvarDsc))
        {
            bool isLo = (lclNum == parentvarDsc->lvFieldLclStart);
            printf(" V%02u.%s(offs=0x%02x)", varDsc->lvParentLcl, isLo ? "lo" : "hi", isLo ? 0 : genTypeSize(TYP_INT));
        }
        else
#endif // !defined(TARGET_64BIT)
        {
            CORINFO_CLASS_HANDLE typeHnd = parentvarDsc->GetStructHnd();
            CORINFO_FIELD_HANDLE fldHnd  = info.compCompHnd->getFieldInClass(typeHnd, varDsc->lvFldOrdinal);

            printf(" V%02u.%s(offs=0x%02x)", varDsc->lvParentLcl, eeGetFieldName(fldHnd), varDsc->lvFldOffset);

            lvaPromotionType promotionType = lvaGetPromotionType(parentvarDsc);
            switch (promotionType)
            {
                case PROMOTION_TYPE_NONE:
                    printf(" P-NONE");
                    break;
                case PROMOTION_TYPE_DEPENDENT:
                    printf(" P-DEP");
                    break;
                case PROMOTION_TYPE_INDEPENDENT:
                    printf(" P-INDEP");
                    break;
            }
        }
    }

    if (varDsc->lvReason != nullptr)
    {
        printf(" \"%s\"", varDsc->lvReason);
    }

    printf("\n");
}

/*****************************************************************************
*
*  dump the lvaTable
*/

void Compiler::lvaTableDump(FrameLayoutState curState)
{
    if (curState == NO_FRAME_LAYOUT)
    {
        curState = lvaDoneFrameLayout;
        if (curState == NO_FRAME_LAYOUT)
        {
            // Still no layout? Could be a bug, but just display the initial layout
            curState = INITIAL_FRAME_LAYOUT;
        }
    }

    if (curState == INITIAL_FRAME_LAYOUT)
    {
        printf("; Initial");
    }
    else if (curState == PRE_REGALLOC_FRAME_LAYOUT)
    {
        printf("; Pre-RegAlloc");
    }
    else if (curState == REGALLOC_FRAME_LAYOUT)
    {
        printf("; RegAlloc");
    }
    else if (curState == TENTATIVE_FRAME_LAYOUT)
    {
        printf("; Tentative");
    }
    else if (curState == FINAL_FRAME_LAYOUT)
    {
        printf("; Final");
    }
    else
    {
        printf("UNKNOWN FrameLayoutState!");
        unreached();
    }

    printf(" local variable assignments\n");
    printf(";\n");

    unsigned   lclNum;
    LclVarDsc* varDsc;

    // Figure out some sizes, to help line things up

    size_t refCntWtdWidth = 6; // Use 6 as the minimum width

    if (curState != INITIAL_FRAME_LAYOUT) // don't need this info for INITIAL_FRAME_LAYOUT
    {
        for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
        {
            size_t width = strlen(refCntWtd2str(varDsc->lvRefCntWtd()));
            if (width > refCntWtdWidth)
            {
                refCntWtdWidth = width;
            }
        }
    }

    // Do the actual output

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        lvaDumpEntry(lclNum, curState, refCntWtdWidth);
    }

    //-------------------------------------------------------------------------
    // Display the code-gen temps

    assert(codeGen->regSet.tmpAllFree());
    for (TempDsc* temp = codeGen->regSet.tmpListBeg(); temp != nullptr; temp = codeGen->regSet.tmpListNxt(temp))
    {
        printf(";  TEMP_%02u %26s%*s%7s  -> ", -temp->tdTempNum(), " ", refCntWtdWidth, " ",
               varTypeName(temp->tdTempType()));
        int offset = temp->tdTempOffs();
        printf(" [%2s%1s0x%02X]\n", isFramePointerUsed() ? STR_FPBASE : STR_SPBASE, (offset < 0 ? "-" : "+"),
               (offset < 0 ? -offset : offset));
    }

    if (curState >= TENTATIVE_FRAME_LAYOUT)
    {
        printf(";\n");
        printf("; Lcl frame size = %d\n", compLclFrameSize);
    }
}
#endif // DEBUG

/*****************************************************************************
 *
 *  Conservatively estimate the layout of the stack frame.
 *
 *  This function is only used before final frame layout. It conservatively estimates the
 *  number of callee-saved registers that must be saved, then calls lvaAssignFrameOffsets().
 *  To do final frame layout, the callee-saved registers are known precisely, so
 *  lvaAssignFrameOffsets() is called directly.
 *
 *  Returns the (conservative, that is, overly large) estimated size of the frame,
 *  including the callee-saved registers. This is only used by the emitter during code
 *  generation when estimating the size of the offset of instructions accessing temps,
 *  and only if temps have a larger offset than variables.
 */

unsigned Compiler::lvaFrameSize(FrameLayoutState curState)
{
    assert(curState < FINAL_FRAME_LAYOUT);

    unsigned result;

    /* Layout the stack frame conservatively.
       Assume all callee-saved registers are spilled to stack */

    compCalleeRegsPushed = CNT_CALLEE_SAVED;

#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64)
    if (compFloatingPointUsed)
        compCalleeRegsPushed += CNT_CALLEE_SAVED_FLOAT;

    compCalleeRegsPushed++; // we always push LR/RA.  See genPushCalleeSavedRegisters
#elif defined(TARGET_AMD64)
    if (compFloatingPointUsed)
    {
        compCalleeFPRegsSavedMask = RBM_FLT_CALLEE_SAVED;
    }
    else
    {
        compCalleeFPRegsSavedMask = RBM_NONE;
    }
#endif

#if DOUBLE_ALIGN
    if (genDoubleAlign())
    {
        // X86 only - account for extra 4-byte pad that may be created by "and  esp, -8"  instruction
        compCalleeRegsPushed++;
    }
#endif

#ifdef TARGET_XARCH
    // Since FP/EBP is included in the SAVED_REG_MAXSZ we need to
    // subtract 1 register if codeGen->isFramePointerUsed() is true.
    if (codeGen->isFramePointerUsed())
    {
        compCalleeRegsPushed--;
    }
#endif

    lvaAssignFrameOffsets(curState);

    unsigned calleeSavedRegMaxSz = CALLEE_SAVED_REG_MAXSZ;
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64)
    if (compFloatingPointUsed)
    {
        calleeSavedRegMaxSz += CALLEE_SAVED_FLOAT_MAXSZ;
    }
    calleeSavedRegMaxSz += REGSIZE_BYTES; // we always push LR/RA.  See genPushCalleeSavedRegisters
#endif

    result = compLclFrameSize + calleeSavedRegMaxSz;
    return result;
}

//------------------------------------------------------------------------
// lvaGetSPRelativeOffset: Given a variable, return the offset of that
// variable in the frame from the stack pointer. This number will be positive,
// since the stack pointer must be at a lower address than everything on the
// stack.
//
// This can't be called for localloc functions, since the stack pointer
// varies, and thus there is no fixed offset to a variable from the stack pointer.
//
// Arguments:
//    varNum - the variable number
//
// Return Value:
//    The offset.

int Compiler::lvaGetSPRelativeOffset(unsigned varNum)
{
    assert(!compLocallocUsed);
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);
    const LclVarDsc* varDsc = lvaGetDesc(varNum);
    assert(varDsc->lvOnFrame);
    int spRelativeOffset;

    if (varDsc->lvFramePointerBased)
    {
        // The stack offset is relative to the frame pointer, so convert it to be
        // relative to the stack pointer (which makes no sense for localloc functions).
        spRelativeOffset = varDsc->GetStackOffset() + codeGen->genSPtoFPdelta();
    }
    else
    {
        spRelativeOffset = varDsc->GetStackOffset();
    }

    assert(spRelativeOffset >= 0);
    return spRelativeOffset;
}

/*****************************************************************************
 *
 *  Return the caller-SP-relative stack offset of a local/parameter.
 *  Requires the local to be on the stack and frame layout to be complete.
 */

int Compiler::lvaGetCallerSPRelativeOffset(unsigned varNum)
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);
    const LclVarDsc* varDsc = lvaGetDesc(varNum);
    assert(varDsc->lvOnFrame);

    return lvaToCallerSPRelativeOffset(varDsc->GetStackOffset(), varDsc->lvFramePointerBased);
}

//-----------------------------------------------------------------------------
// lvaToCallerSPRelativeOffset: translate a frame offset into an offset from
//    the caller's stack pointer.
//
// Arguments:
//    offset - frame offset
//    isFpBase - if true, offset is from FP, otherwise offset is from SP
//    forRootFrame - if the current method is an OSR method, adjust the offset
//      to be relative to the SP for the root method, instead of being relative
//      to the SP for the OSR method.
//
// Returins:
//    suitable offset
//
int Compiler::lvaToCallerSPRelativeOffset(int offset, bool isFpBased, bool forRootFrame) const
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);

    if (isFpBased)
    {
        offset += codeGen->genCallerSPtoFPdelta();
    }
    else
    {
        offset += codeGen->genCallerSPtoInitialSPdelta();
    }

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    if (forRootFrame && opts.IsOSR())
    {
        const PatchpointInfo* const ppInfo = info.compPatchpointInfo;

#if defined(TARGET_AMD64)
        // The offset computed above already includes the OSR frame adjustment, plus the
        // pop of the "pseudo return address" from the OSR frame.
        //
        // To get to root method caller-SP, we need to subtract off the tier0 frame
        // size and the pushed return address and RBP for the tier0 frame (which we know is an
        // RPB frame).
        //
        // ppInfo's TotalFrameSize also accounts for the popped pseudo return address
        // between the tier0 method frame and the OSR frame. So the net adjustment
        // is simply TotalFrameSize plus one register.
        //
        const int adjustment = ppInfo->TotalFrameSize() + REGSIZE_BYTES;

#elif defined(TARGET_ARM64)

        const int adjustment = ppInfo->TotalFrameSize();
#endif

        offset -= adjustment;
    }
#else
    // OSR NYI for other targets.
    assert(!opts.IsOSR());
#endif

    return offset;
}

/*****************************************************************************
 *
 *  Return the Initial-SP-relative stack offset of a local/parameter.
 *  Requires the local to be on the stack and frame layout to be complete.
 */

int Compiler::lvaGetInitialSPRelativeOffset(unsigned varNum)
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);
    const LclVarDsc* varDsc = lvaGetDesc(varNum);
    assert(varDsc->lvOnFrame);

    return lvaToInitialSPRelativeOffset(varDsc->GetStackOffset(), varDsc->lvFramePointerBased);
}

// Given a local variable offset, and whether that offset is frame-pointer based, return its offset from Initial-SP.
// This is used, for example, to figure out the offset of the frame pointer from Initial-SP.
int Compiler::lvaToInitialSPRelativeOffset(unsigned offset, bool isFpBased)
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);
#ifdef TARGET_AMD64
    if (isFpBased)
    {
        // Currently, the frame starts by pushing ebp, ebp points to the saved ebp
        // (so we have ebp pointer chaining). Add the fixed-size frame size plus the
        // size of the callee-saved regs (not including ebp itself) to find Initial-SP.

        assert(codeGen->isFramePointerUsed());
        offset += codeGen->genSPtoFPdelta();
    }
    else
    {
        // The offset is correct already!
    }
#else  // !TARGET_AMD64
    NYI("lvaToInitialSPRelativeOffset");
#endif // !TARGET_AMD64

    return offset;
}

/*****************************************************************************/

#ifdef DEBUG
/*****************************************************************************
 *  Pick a padding size at "random" for the local.
 *  0 means that it should not be converted to a GT_LCL_FLD
 */

static unsigned LCL_FLD_PADDING(unsigned lclNum)
{
    // Convert every 2nd variable
    if (lclNum % 2)
    {
        return 0;
    }

    // Pick a padding size at "random"
    unsigned size = lclNum % 7;

    return size;
}

/*****************************************************************************
 *
 *  Callback for fgWalkAllTreesPre()
 *  Convert as many GT_LCL_VAR's to GT_LCL_FLD's
 */

/* static */
/*
    The stress mode does 2 passes.

    In the first pass we will mark the locals where we CAN't apply the stress mode.
    In the second pass we will do the appropiate morphing wherever we've not determined we can't do it.
*/
Compiler::fgWalkResult Compiler::lvaStressLclFldCB(GenTree** pTree, fgWalkData* data)
{
    GenTree*   tree = *pTree;
    genTreeOps oper = tree->OperGet();
    GenTree*   lcl;

    switch (oper)
    {
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
            lcl = tree;
            break;

        case GT_ADDR:
            if (tree->AsOp()->gtOp1->gtOper != GT_LCL_VAR)
            {
                return WALK_CONTINUE;
            }
            lcl = tree->AsOp()->gtOp1;
            break;

        default:
            return WALK_CONTINUE;
    }

    noway_assert(lcl->OperIs(GT_LCL_VAR, GT_LCL_VAR_ADDR));

    Compiler* const  pComp      = ((lvaStressLclFldArgs*)data->pCallbackData)->m_pCompiler;
    const bool       bFirstPass = ((lvaStressLclFldArgs*)data->pCallbackData)->m_bFirstPass;
    const unsigned   lclNum     = lcl->AsLclVarCommon()->GetLclNum();
    var_types        type       = lcl->TypeGet();
    LclVarDsc* const varDsc     = pComp->lvaGetDesc(lclNum);

    if (varDsc->lvNoLclFldStress)
    {
        // Already determined we can't do anything for this var
        return WALK_SKIP_SUBTREES;
    }

    if (bFirstPass)
    {
        // Ignore arguments and temps
        if (varDsc->lvIsParam || lclNum >= pComp->info.compLocalsCount)
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }

        // Ignore OSR locals; if in memory, they will live on the
        // Tier0 frame and so can't have their storage adjusted.
        //
        if (pComp->lvaIsOSRLocal(lclNum))
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }

        // Likewise for Tier0 methods with patchpoints --
        // if we modify them we'll misreport their locations in the patchpoint info.
        //
        if (pComp->doesMethodHavePatchpoints() || pComp->doesMethodHavePartialCompilationPatchpoints())
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }

        // Fix for lcl_fld stress mode
        if (varDsc->lvKeepType)
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }

        // Can't have GC ptrs in TYP_BLK.
        if (!varTypeIsArithmetic(type))
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }

        // The noway_assert in the second pass below, requires that these types match, or we have a TYP_BLK
        //
        if ((varDsc->lvType != lcl->gtType) && (varDsc->lvType != TYP_BLK))
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }

        // Weed out "small" types like TYP_BYTE as we don't mark the GT_LCL_VAR
        // node with the accurate small type. If we bash lvaTable[].lvType,
        // then there will be no indication that it was ever a small type.
        var_types varType = varDsc->TypeGet();
        if (varType != TYP_BLK && genTypeSize(varType) != genTypeSize(genActualType(varType)))
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }

        // Offset some of the local variable by a "random" non-zero amount
        unsigned padding = LCL_FLD_PADDING(lclNum);
        if (padding == 0)
        {
            varDsc->lvNoLclFldStress = true;
            return WALK_SKIP_SUBTREES;
        }
    }
    else
    {
        // Do the morphing
        noway_assert((varDsc->lvType == lcl->gtType) || (varDsc->lvType == TYP_BLK));
        var_types varType = varDsc->TypeGet();

        // Calculate padding
        unsigned padding = LCL_FLD_PADDING(lclNum);

#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64)
        // We need to support alignment requirements to access memory.
        unsigned alignment = 1;
        pComp->codeGen->InferOpSizeAlign(lcl, &alignment);
        alignment = roundUp(alignment, TARGET_POINTER_SIZE);
        padding   = roundUp(padding, alignment);
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64

        // Change the variable to a TYP_BLK
        if (varType != TYP_BLK)
        {
            varDsc->lvExactSize = roundUp(padding + pComp->lvaLclSize(lclNum), TARGET_POINTER_SIZE);
            varDsc->lvType      = TYP_BLK;
            pComp->lvaSetVarAddrExposed(lclNum DEBUGARG(AddressExposedReason::STRESS_LCL_FLD));
        }

        tree->gtFlags |= GTF_GLOB_REF;

        /* Now morph the tree appropriately */
        if (oper == GT_LCL_VAR)
        {
            /* Change lclVar(lclNum) to lclFld(lclNum,padding) */

            tree->ChangeOper(GT_LCL_FLD);
            tree->AsLclFld()->SetLclOffs(padding);
        }
        else if (oper == GT_LCL_VAR_ADDR)
        {
            tree->ChangeOper(GT_LCL_FLD_ADDR);
            tree->AsLclFld()->SetLclOffs(padding);
        }
        else
        {
            /* Change addr(lclVar) to addr(lclVar)+padding */

            noway_assert(oper == GT_ADDR);
            GenTree* paddingTree = pComp->gtNewIconNode(padding);
            GenTree* newAddr     = pComp->gtNewOperNode(GT_ADD, tree->gtType, tree, paddingTree);

            *pTree = newAddr;

            lcl->gtType = TYP_BLK;
        }
    }

    return WALK_SKIP_SUBTREES;
}

/*****************************************************************************/

void Compiler::lvaStressLclFld()
{
    if (!compStressCompile(STRESS_LCL_FLDS, 5))
    {
        return;
    }

    lvaStressLclFldArgs Args;
    Args.m_pCompiler  = this;
    Args.m_bFirstPass = true;

    // Do First pass
    fgWalkAllTreesPre(lvaStressLclFldCB, &Args);

    // Second pass
    Args.m_bFirstPass = false;
    fgWalkAllTreesPre(lvaStressLclFldCB, &Args);
}

#endif // DEBUG

/*****************************************************************************
 *
 *  A little routine that displays a local variable bitset.
 *  'set' is mask of variables that have to be displayed
 *  'allVars' is the complete set of interesting variables (blank space is
 *    inserted if its corresponding bit is not in 'set').
 */

#ifdef DEBUG
void Compiler::lvaDispVarSet(VARSET_VALARG_TP set)
{
    VARSET_TP allVars(VarSetOps::MakeEmpty(this));
    lvaDispVarSet(set, allVars);
}

void Compiler::lvaDispVarSet(VARSET_VALARG_TP set, VARSET_VALARG_TP allVars)
{
    printf("{");

    bool needSpace = false;

    for (unsigned index = 0; index < lvaTrackedCount; index++)
    {
        if (VarSetOps::IsMember(this, set, index))
        {
            unsigned   lclNum;
            LclVarDsc* varDsc;

            /* Look for the matching variable */

            for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
            {
                if ((varDsc->lvVarIndex == index) && varDsc->lvTracked)
                {
                    break;
                }
            }

            if (needSpace)
            {
                printf(" ");
            }
            else
            {
                needSpace = true;
            }

            printf("V%02u", lclNum);
        }
        else if (VarSetOps::IsMember(this, allVars, index))
        {
            if (needSpace)
            {
                printf(" ");
            }
            else
            {
                needSpace = true;
            }

            printf("   ");
        }
    }

    printf("}");
}

#endif // DEBUG
