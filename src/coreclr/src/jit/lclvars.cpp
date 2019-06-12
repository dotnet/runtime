// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    lvaGenericsContextUseCount = 0;

    lvaTrackedToVarNum = nullptr;

    lvaTrackedFixed = false; // false: We can still add new tracked variables

    lvaDoneFrameLayout = NO_FRAME_LAYOUT;
#if !FEATURE_EH_FUNCLETS
    lvaShadowSPslotsVar = BAD_VAR_NUM;
#endif // !FEATURE_EH_FUNCLETS
    lvaInlinedPInvokeFrameVar = BAD_VAR_NUM;
    lvaReversePInvokeFrameVar = BAD_VAR_NUM;
#if FEATURE_FIXED_OUT_ARGS
    lvaPInvokeFrameRegSaveVar = BAD_VAR_NUM;
    lvaOutgoingArgSpaceVar    = BAD_VAR_NUM;
    lvaOutgoingArgSpaceSize   = PhasedVar<unsigned>();
#endif // FEATURE_FIXED_OUT_ARGS
#ifdef _TARGET_ARM_
    lvaPromotedStructAssemblyScratchVar = BAD_VAR_NUM;
#endif // _TARGET_ARM_
#ifdef JIT32_GCENCODER
    lvaLocAllocSPvar = BAD_VAR_NUM;
#endif // JIT32_GCENCODER
    lvaNewObjArrayArgs  = BAD_VAR_NUM;
    lvaGSSecurityCookie = BAD_VAR_NUM;
#ifdef _TARGET_X86_
    lvaVarargsBaseOfStkArgs = BAD_VAR_NUM;
#endif // _TARGET_X86_
    lvaVarargsHandleArg = BAD_VAR_NUM;
    lvaSecurityObject   = BAD_VAR_NUM;
    lvaStubArgumentVar  = BAD_VAR_NUM;
    lvaArg0Var          = BAD_VAR_NUM;
    lvaMonAcquired      = BAD_VAR_NUM;

    lvaInlineeReturnSpillTemp = BAD_VAR_NUM;

    gsShadowVarInfo = nullptr;
#if FEATURE_EH_FUNCLETS
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
    if (supportSIMDTypes() && (info.compRetNativeType == TYP_STRUCT))
    {
        var_types structType = impNormStructType(info.compMethodInfo->args.retTypeClass);
        info.compRetType     = structType;
    }
#endif // FEATURE_SIMD

    // Are we returning a struct using a return buffer argument?
    //
    const bool hasRetBuffArg = impMethodInfo_hasRetBuffArg(info.compMethodInfo);

    // Possibly change the compRetNativeType from TYP_STRUCT to a "primitive" type
    // when we are returning a struct by value and it fits in one register
    //
    if (!hasRetBuffArg && varTypeIsStruct(info.compRetNativeType))
    {
        CORINFO_CLASS_HANDLE retClsHnd = info.compMethodInfo->args.retTypeClass;

        Compiler::structPassingKind howToReturnStruct;
        var_types                   returnType = getReturnTypeForStruct(retClsHnd, &howToReturnStruct);

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
    varDscInfo.Init(lvaTable, hasRetBuffArg);

    lvaInitArgs(&varDscInfo);

    //-------------------------------------------------------------------------
    // Finally the local variables
    //-------------------------------------------------------------------------

    unsigned                varNum    = varDscInfo.varNum;
    LclVarDsc*              varDsc    = varDscInfo.varDsc;
    CORINFO_ARG_LIST_HANDLE localsSig = info.compMethodInfo->locals.args;

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

    if (getNeedsGSSecurityCookie())
    {
        // Ensure that there will be at least one stack variable since
        // we require that the GSCookie does not have a 0 stack offset.
        unsigned dummy         = lvaGrabTempWithImplicitUse(false DEBUGARG("GSCookie dummy"));
        lvaTable[dummy].lvType = TYP_INT;
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

#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
    // Prespill all argument regs on to stack in case of Arm when under profiler.
    if (compIsProfilerHookNeeded())
    {
        codeGen->regSet.rsMaskPreSpillRegArg |= RBM_ARG_REGS;
    }
#endif

    //----------------------------------------------------------------------

    /* Is there a "this" pointer ? */
    lvaInitThisPtr(varDscInfo);

    /* If we have a hidden return-buffer parameter, that comes here */
    lvaInitRetBuffArg(varDscInfo);

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
    lvaInitUserArgs(varDscInfo);

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
    info.compArgStackSize     = varDscInfo->stackArgSize;
    info.compHasMultiSlotArgs = varDscInfo->hasMultiSlotStruct;
#endif // FEATURE_FASTTAILCALL

    // The total argument size must be aligned.
    noway_assert((compArgSize % TARGET_POINTER_SIZE) == 0);

#ifdef _TARGET_X86_
    /* We can not pass more than 2^16 dwords as arguments as the "ret"
       instruction can only pop 2^16 arguments. Could be handled correctly
       but it will be very difficult for fully interruptible code */

    if (compArgSize != (size_t)(unsigned short)compArgSize)
        NO_WAY("Too many arguments for the \"ret\" instruction to pop");
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
            if (supportSIMDTypes())
            {
                var_types simdBaseType = TYP_UNKNOWN;
                var_types type         = impNormStructType(info.compClassHnd, nullptr, nullptr, &simdBaseType);
                if (simdBaseType != TYP_UNKNOWN)
                {
                    assert(varTypeIsSIMD(type));
                    varDsc->lvSIMDType  = true;
                    varDsc->lvBaseType  = simdBaseType;
                    varDsc->lvExactSize = genTypeSize(type);
                }
            }
#endif // FEATURE_SIMD
        }
        else
        {
            varDsc->lvType = TYP_REF;
            lvaSetClass(varDscInfo->varNum, info.compClassHnd);
        }

        if (tiVerificationNeeded)
        {
            varDsc->lvVerTypeInfo = verMakeTypeInfo(info.compClassHnd);

            if (varDsc->lvVerTypeInfo.IsValueClass())
            {
                varDsc->lvVerTypeInfo.MakeByRef();
            }
        }
        else
        {
            varDsc->lvVerTypeInfo = typeInfo();
        }

        // Mark the 'this' pointer for the method
        varDsc->lvVerTypeInfo.SetIsThisPtr();

        varDsc->lvIsRegArg = 1;
        noway_assert(varDscInfo->intRegArgNum == 0);

        varDsc->lvArgReg = genMapRegArgNumToRegNum(varDscInfo->allocRegArg(TYP_INT), varDsc->TypeGet());
#if FEATURE_MULTIREG_ARGS
        varDsc->lvOtherArgReg = REG_NA;
#endif
        varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

#ifdef DEBUG
        if (verbose)
        {
            printf("'this'    passed in register %s\n", getRegName(varDsc->lvArgReg));
        }
#endif
        compArgSize += TARGET_POINTER_SIZE;

        varDscInfo->varNum++;
        varDscInfo->varDsc++;
    }
}

/*****************************************************************************/
void Compiler::lvaInitRetBuffArg(InitVarDscInfo* varDscInfo)
{
    LclVarDsc* varDsc        = varDscInfo->varDsc;
    bool       hasRetBuffArg = impMethodInfo_hasRetBuffArg(info.compMethodInfo);

    // These two should always match
    noway_assert(hasRetBuffArg == varDscInfo->hasRetBufArg);

    if (hasRetBuffArg)
    {
        info.compRetBuffArg = varDscInfo->varNum;
        varDsc->lvType      = TYP_BYREF;
        varDsc->lvIsParam   = 1;
        varDsc->lvIsRegArg  = 1;

        if (hasFixedRetBuffReg())
        {
            varDsc->lvArgReg = theFixedRetBuffReg();
        }
        else
        {
            unsigned retBuffArgNum = varDscInfo->allocRegArg(TYP_INT);
            varDsc->lvArgReg       = genMapIntRegArgNumToRegNum(retBuffArgNum);
        }

#if FEATURE_MULTIREG_ARGS
        varDsc->lvOtherArgReg = REG_NA;
#endif
        varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

        info.compRetBuffDefStack = 0;
        if (info.compRetType == TYP_STRUCT)
        {
            CORINFO_SIG_INFO sigInfo;
            info.compCompHnd->getMethodSig(info.compMethodHnd, &sigInfo);
            assert(JITtype2varType(sigInfo.retType) == info.compRetType); // Else shouldn't have a ret buff.

            info.compRetBuffDefStack =
                (info.compCompHnd->isStructRequiringStackAllocRetBuf(sigInfo.retTypeClass) == TRUE);
            if (info.compRetBuffDefStack)
            {
                // If we're assured that the ret buff argument points into a callers stack, we will type it as
                // "TYP_I_IMPL"
                // (native int/unmanaged pointer) so that it's not tracked as a GC ref.
                varDsc->lvType = TYP_I_IMPL;
            }
        }
#ifdef FEATURE_SIMD
        else if (supportSIMDTypes() && varTypeIsSIMD(info.compRetType))
        {
            varDsc->lvSIMDType = true;
            varDsc->lvBaseType =
                getBaseTypeAndSizeOfSIMDType(info.compMethodInfo->args.retTypeClass, &varDsc->lvExactSize);
            assert(varDsc->lvBaseType != TYP_UNKNOWN);
        }
#endif // FEATURE_SIMD

        assert(isValidIntArgReg(varDsc->lvArgReg));

#ifdef DEBUG
        if (verbose)
        {
            printf("'__retBuf'  passed in register %s\n", getRegName(varDsc->lvArgReg));
        }
#endif

        /* Update the total argument size, count and varDsc */

        compArgSize += TARGET_POINTER_SIZE;
        varDscInfo->varNum++;
        varDscInfo->varDsc++;
    }
}

/*****************************************************************************/
void Compiler::lvaInitUserArgs(InitVarDscInfo* varDscInfo)
{
//-------------------------------------------------------------------------
// Walk the function signature for the explicit arguments
//-------------------------------------------------------------------------

#if defined(_TARGET_X86_)
    // Only (some of) the implicit args are enregistered for varargs
    varDscInfo->maxIntRegArgNum = info.compIsVarArgs ? varDscInfo->intRegArgNum : MAX_REG_ARG;
#elif defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)
    // On System V type environment the float registers are not indexed together with the int ones.
    varDscInfo->floatRegArgNum = varDscInfo->intRegArgNum;
#endif // _TARGET_*

    CORINFO_ARG_LIST_HANDLE argLst = info.compMethodInfo->args.args;

    const unsigned argSigLen = info.compMethodInfo->args.numArgs;

#ifdef _TARGET_ARM_
    regMaskTP doubleAlignMask = RBM_NONE;
#endif // _TARGET_ARM_

    for (unsigned i = 0; i < argSigLen;
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

        // For ARM, ARM64, and AMD64 varargs, all arguments go in integer registers
        var_types argType = mangleVarArgsType(varDsc->TypeGet());

#ifdef _TARGET_ARM_
        var_types origArgType = argType;
#endif // TARGET_ARM

        // ARM softfp calling convention should affect only the floating point arguments.
        // Otherwise there appear too many surplus pre-spills and other memory operations
        // with the associated locations .
        bool      isSoftFPPreSpill = opts.compUseSoftFP && varTypeIsFloating(varDsc->TypeGet());
        unsigned  argSize          = eeGetArgSize(argLst, &info.compMethodInfo->args);
        unsigned  cSlots           = argSize / TARGET_POINTER_SIZE; // the total number of slots of this argument
        bool      isHfaArg         = false;
        var_types hfaType          = TYP_UNDEF;

#if defined(_TARGET_ARM64_) && defined(_TARGET_UNIX_)
        // Native varargs on arm64 unix use the regular calling convention.
        if (!opts.compUseSoftFP)
#else
        // Methods that use VarArg or SoftFP cannot have HFA arguments
        if (!info.compIsVarArgs && !opts.compUseSoftFP)
#endif // defined(_TARGET_ARM64_) && defined(_TARGET_UNIX_)
        {
            // If the argType is a struct, then check if it is an HFA
            if (varTypeIsStruct(argType))
            {
                // hfaType is set to float, double or SIMD type if it is an HFA, otherwise TYP_UNDEF.
                hfaType  = GetHfaType(typeHnd);
                isHfaArg = varTypeIsValidHfaType(hfaType);
            }
        }
        else if (info.compIsVarArgs)
        {
#ifdef _TARGET_UNIX_
            // Currently native varargs is not implemented on non windows targets.
            //
            // Note that some targets like Arm64 Unix should not need much work as
            // the ABI is the same. While other targets may only need small changes
            // such as amd64 Unix, which just expects RAX to pass numFPArguments.
            NYI("InitUserArgs for Vararg callee is not yet implemented on non Windows targets.");
#endif
        }

        if (isHfaArg)
        {
            // We have an HFA argument, so from here on out treat the type as a float, double or vector.
            // The orginal struct type is available by using origArgType
            // We also update the cSlots to be the number of float/double fields in the HFA
            argType = hfaType;
            varDsc->SetHfaType(hfaType);
            cSlots = varDsc->lvHfaSlots();
        }
        // The number of slots that must be enregistered if we are to consider this argument enregistered.
        // This is normally the same as cSlots, since we normally either enregister the entire object,
        // or none of it. For structs on ARM, however, we only need to enregister a single slot to consider
        // it enregistered, as long as we can split the rest onto the stack.
        unsigned cSlotsToEnregister = cSlots;

#if defined(_TARGET_ARM64_) && FEATURE_ARG_SPLIT

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

#endif // defined(_TARGET_ARM64_) && FEATURE_ARG_SPLIT

#ifdef _TARGET_ARM_
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
#else // !_TARGET_ARM_
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
#endif // !_TARGET_ARM_

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
#endif // defined(UNIX_AMD64_ABI)
        {
            canPassArgInRegisters = varDscInfo->canEnreg(argType, cSlotsToEnregister);
        }

        if (canPassArgInRegisters)
        {
            /* Another register argument */

            // Allocate the registers we need. allocRegArg() returns the first argument register number of the set.
            // For non-HFA structs, we still "try" to enregister the whole thing; it will just max out if splitting
            // to the stack happens.
            unsigned firstAllocatedRegArgNum = 0;

#if FEATURE_MULTIREG_ARGS
            varDsc->lvOtherArgReg = REG_NA;
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
#endif // defined(UNIX_AMD64_ABI)
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
#ifdef _TARGET_ARM64_
            if (argType == TYP_STRUCT)
            {
                varDsc->lvArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum, TYP_I_IMPL);
                if (cSlots == 2)
                {
                    varDsc->lvOtherArgReg          = genMapRegArgNumToRegNum(firstAllocatedRegArgNum + 1, TYP_I_IMPL);
                    varDscInfo->hasMultiSlotStruct = true;
                }
            }
#elif defined(UNIX_AMD64_ABI)
            if (varTypeIsStruct(argType))
            {
                varDsc->lvArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum, firstEightByteType);

                // If there is a second eightbyte, get a register for it too and map the arg to the reg number.
                if (structDesc.eightByteCount >= 2)
                {
                    secondEightByteType            = GetEightByteType(structDesc, 1);
                    secondAllocatedRegArgNum       = varDscInfo->allocRegArg(secondEightByteType, 1);
                    varDscInfo->hasMultiSlotStruct = true;
                }

                if (secondEightByteType != TYP_UNDEF)
                {
                    varDsc->lvOtherArgReg = genMapRegArgNumToRegNum(secondAllocatedRegArgNum, secondEightByteType);
                }
            }
#else  // ARM32
            if (varTypeIsStruct(argType))
            {
                varDsc->lvArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum, TYP_I_IMPL);
            }
#endif // ARM32
            else
#endif // FEATURE_MULTIREG_ARGS
            {
                varDsc->lvArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum, argType);
            }

#ifdef _TARGET_ARM_
            if (varDsc->TypeGet() == TYP_LONG)
            {
                varDsc->lvOtherReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum + 1, TYP_INT);
            }
#endif // _TARGET_ARM_

#ifdef DEBUG
            if (verbose)
            {
                printf("Arg #%u    passed in register(s) ", varDscInfo->varNum);
                bool isFloat = false;
#if defined(UNIX_AMD64_ABI)
                if (varTypeIsStruct(argType) && (structDesc.eightByteCount >= 1))
                {
                    isFloat = varTypeIsFloating(firstEightByteType);
                }
                else
#endif // !UNIX_AMD64_ABI
                {
                    isFloat = varTypeIsFloating(argType);
                }

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
                               getRegName(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, firstEightByteType),
                                          isFloat));
                    }

                    if (secondEightByteType == TYP_UNDEF)
                    {
                        printf(", secondEightByte: <not used>");
                    }
                    else
                    {
                        printf(", secondEightByte: %s",
                               getRegName(genMapRegArgNumToRegNum(secondAllocatedRegArgNum, secondEightByteType),
                                          varTypeIsFloating(secondEightByteType)));
                    }
                }
                else
#endif // defined(UNIX_AMD64_ABI)
                {
                    isFloat            = varTypeIsFloating(argType);
                    unsigned regArgNum = genMapRegNumToRegArgNum(varDsc->lvArgReg, argType);

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

#ifdef _TARGET_ARM_
                        if (isFloat)
                        {
                            // Print register size prefix
                            if (argType == TYP_DOUBLE)
                            {
                                // Print both registers, just to be clear
                                printf("%s/%s", getRegName(genMapRegArgNumToRegNum(regArgNum, argType), isFloat),
                                       getRegName(genMapRegArgNumToRegNum(regArgNum + 1, argType), isFloat));

                                // doubles take 2 slots
                                assert(ix + 1 < cSlots);
                                ++ix;
                                ++regArgNum;
                            }
                            else
                            {
                                printf("%s", getRegName(genMapRegArgNumToRegNum(regArgNum, argType), isFloat));
                            }
                        }
                        else
#endif // _TARGET_ARM_
                        {
                            printf("%s", getRegName(genMapRegArgNumToRegNum(regArgNum, argType), isFloat));
                        }
                    }
                }
                printf("\n");
            }
#endif    // DEBUG
        } // end if (canPassArgInRegisters)
        else
        {
#if defined(_TARGET_ARM_)
            varDscInfo->setAllRegArgUsed(argType);
            if (varTypeIsFloating(argType))
            {
                varDscInfo->setAnyFloatStackArgs();
            }

#elif defined(_TARGET_ARM64_)

            // If we needed to use the stack in order to pass this argument then
            // record the fact that we have used up any remaining registers of this 'type'
            // This prevents any 'backfilling' from occuring on ARM64
            //
            varDscInfo->setAllRegArgUsed(argType);

#endif // _TARGET_XXX_

#if FEATURE_FASTTAILCALL
            varDscInfo->stackArgSize += roundUp(argSize, TARGET_POINTER_SIZE);
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
        if (info.compIsVarArgs || isHfaArg || isSoftFPPreSpill)
        {
#if defined(_TARGET_X86_)
            varDsc->lvStkOffs = compArgSize;
#else  // !_TARGET_X86_
            // TODO-CQ: We shouldn't have to go as far as to declare these
            // address-exposed -- DoNotEnregister should suffice.
            lvaSetVarAddrExposed(varDscInfo->varNum);
#endif // !_TARGET_X86_
        }
    } // for each user arg

#ifdef _TARGET_ARM_
    if (doubleAlignMask != RBM_NONE)
    {
        assert(RBM_ARG_REGS == 0xF);
        assert((doubleAlignMask & RBM_ARG_REGS) == doubleAlignMask);
        if (doubleAlignMask != RBM_NONE && doubleAlignMask != RBM_ARG_REGS)
        {
            // doubleAlignMask can only be 0011 and/or 1100 as 'double aligned types' can
            // begin at r0 or r2.
            assert(doubleAlignMask == 0x3 || doubleAlignMask == 0xC /* || 0xF is if'ed out */);

            // Now if doubleAlignMask is 0011 i.e., {r0,r1} and we prespill r2 or r3
            // but not both, then the stack would be misaligned for r0. So spill both
            // r2 and r3.
            //
            // ; +0 --- caller SP double aligned ----
            // ; -4 r2    r3
            // ; -8 r1    r1
            // ; -c r0    r0   <-- misaligned.
            // ; callee saved regs
            if (doubleAlignMask == 0x3 && doubleAlignMask != codeGen->regSet.rsMaskPreSpillRegArg)
            {
                codeGen->regSet.rsMaskPreSpillAlign =
                    (~codeGen->regSet.rsMaskPreSpillRegArg & ~doubleAlignMask) & RBM_ARG_REGS;
            }
        }
    }
#endif // _TARGET_ARM_
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
            varDsc->lvArgReg   = genMapRegArgNumToRegNum(varDscInfo->regArgNum(TYP_INT), varDsc->TypeGet());
#if FEATURE_MULTIREG_ARGS
            varDsc->lvOtherArgReg = REG_NA;
#endif
            varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

            varDscInfo->intRegArgNum++;

#ifdef DEBUG
            if (verbose)
            {
                printf("'GenCtxt'   passed in register %s\n", getRegName(varDsc->lvArgReg));
            }
#endif
        }
        else
        {
            // We need to mark these as being on the stack, as this is not done elsewhere in the case that canEnreg
            // returns false.
            varDsc->lvOnFrame = true;
#if FEATURE_FASTTAILCALL
            varDscInfo->stackArgSize += TARGET_POINTER_SIZE;
#endif // FEATURE_FASTTAILCALL
        }

        compArgSize += TARGET_POINTER_SIZE;

#if defined(_TARGET_X86_)
        if (info.compIsVarArgs)
            varDsc->lvStkOffs = compArgSize;
#endif // _TARGET_X86_

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
        // Make sure this lives in the stack -- address may be reported to the VM.
        // TODO-CQ: This should probably be:
        //   lvaSetVarDoNotEnregister(varDscInfo->varNum DEBUGARG(DNER_VMNeedsStackAddr));
        // But that causes problems, so, for expedience, I switched back to this heavyweight
        // hammer.  But I think it should be possible to switch; it may just work now
        // that other problems are fixed.
        lvaSetVarAddrExposed(varDscInfo->varNum);

        if (varDscInfo->canEnreg(TYP_I_IMPL))
        {
            /* Another register argument */

            unsigned varArgHndArgNum = varDscInfo->allocRegArg(TYP_I_IMPL);

            varDsc->lvIsRegArg = 1;
            varDsc->lvArgReg   = genMapRegArgNumToRegNum(varArgHndArgNum, TYP_I_IMPL);
#if FEATURE_MULTIREG_ARGS
            varDsc->lvOtherArgReg = REG_NA;
#endif
            varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame
#ifdef _TARGET_ARM_
            // This has to be spilled right in front of the real arguments and we have
            // to pre-spill all the argument registers explicitly because we only have
            // have symbols for the declared ones, not any potential variadic ones.
            for (unsigned ix = varArgHndArgNum; ix < ArrLen(intArgMasks); ix++)
            {
                codeGen->regSet.rsMaskPreSpillRegArg |= intArgMasks[ix];
            }
#endif // _TARGET_ARM_

#ifdef DEBUG
            if (verbose)
            {
                printf("'VarArgHnd' passed in register %s\n", getRegName(varDsc->lvArgReg));
            }
#endif // DEBUG
        }
        else
        {
            // We need to mark these as being on the stack, as this is not done elsewhere in the case that canEnreg
            // returns false.
            varDsc->lvOnFrame = true;
#if FEATURE_FASTTAILCALL
            varDscInfo->stackArgSize += TARGET_POINTER_SIZE;
#endif // FEATURE_FASTTAILCALL
        }

        /* Update the total argument size, count and varDsc */

        compArgSize += TARGET_POINTER_SIZE;

        varDscInfo->varNum++;
        varDscInfo->varDsc++;

#if defined(_TARGET_X86_)
        varDsc->lvStkOffs = compArgSize;

        // Allocate a temp to point at the beginning of the args

        lvaVarargsBaseOfStkArgs                  = lvaGrabTemp(false DEBUGARG("Varargs BaseOfStkArgs"));
        lvaTable[lvaVarargsBaseOfStkArgs].lvType = TYP_I_IMPL;

#endif // _TARGET_X86_
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
    noway_assert(varDsc == &lvaTable[varNum]);

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

    if (tiVerificationNeeded)
    {
        varDsc->lvVerTypeInfo = verParseArgSigToTypeInfo(varSig, varList);
    }

    if (tiVerificationNeeded)
    {
        if (varDsc->lvIsParam)
        {
            // For an incoming ValueType we better be able to have the full type information
            // so that we can layout the parameter offsets correctly

            if (varTypeIsStruct(type) && varDsc->lvVerTypeInfo.IsDead())
            {
                BADCODE("invalid ValueType parameter");
            }

            // For an incoming reference type we need to verify that the actual type is
            // a reference type and not a valuetype.

            if (type == TYP_REF &&
                !(varDsc->lvVerTypeInfo.IsType(TI_REF) || varDsc->lvVerTypeInfo.IsUnboxedGenericTypeVar()))
            {
                BADCODE("parameter type mismatch");
            }
        }

        // Disallow byrefs to byref like objects (ArgTypeHandle)
        // techncally we could get away with just not setting them
        if (varDsc->lvVerTypeInfo.IsByRef() && verIsByRefLike(DereferenceByRef(varDsc->lvVerTypeInfo)))
        {
            varDsc->lvVerTypeInfo = typeInfo();
        }

        // we don't want the EE to assert in lvaSetStruct on bad sigs, so change
        // the JIT type to avoid even trying to call back
        if (varTypeIsStruct(type) && varDsc->lvVerTypeInfo.IsDead())
        {
            type = TYP_VOID;
        }
    }

    if (typeHnd)
    {
        unsigned cFlags = info.compCompHnd->getClassAttribs(typeHnd);

        // We can get typeHnds for primitive types, these are value types which only contain
        // a primitive. We will need the typeHnd to distinguish them, so we store it here.
        if ((cFlags & CORINFO_FLG_VALUECLASS) && !varTypeIsStruct(type))
        {
            if (tiVerificationNeeded == false)
            {
                // printf("This is a struct that the JIT will treat as a primitive\n");
                varDsc->lvVerTypeInfo = verMakeTypeInfo(typeHnd);
            }
        }

        varDsc->lvOverlappingFields = StructHasOverlappingFields(cFlags);
    }

    if (varTypeIsGC(type))
    {
        varDsc->lvStructGcCount = 1;
    }

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    varDsc->lvIsImplicitByRef = 0;
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

// Set the lvType (before this point it is TYP_UNDEF).

#ifdef FEATURE_HFA
    varDsc->SetHfaType(TYP_UNDEF);
#endif
    if ((varTypeIsStruct(type)))
    {
        lvaSetStruct(varNum, typeHnd, typeHnd != nullptr, !tiVerificationNeeded);
        if (info.compIsVarArgs)
        {
            lvaSetStructUsedAsVarArg(varNum);
        }
    }
    else
    {
        varDsc->lvType = type;
    }

#if OPT_BOOL_OPS
    if (type == TYP_BOOL)
    {
        varDsc->lvIsBoolean = true;
    }
#endif

#ifdef DEBUG
    varDsc->lvStkOffs = BAD_STK_OFFS;
#endif

#if FEATURE_MULTIREG_ARGS
    varDsc->lvOtherArgReg = REG_NA;
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

bool Compiler::lvaVarAddrExposed(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    LclVarDsc* varDsc = &lvaTable[varNum];

    return varDsc->lvAddrExposed;
}

/*****************************************************************************
 * Returns true iff variable "varNum" should not be enregistered (or one of several reasons).
 */

bool Compiler::lvaVarDoNotEnregister(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    LclVarDsc* varDsc = &lvaTable[varNum];

    return varDsc->lvDoNotEnregister;
}

/*****************************************************************************
 * Returns the handle to the class of the local variable varNum
 */

CORINFO_CLASS_HANDLE Compiler::lvaGetStruct(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    LclVarDsc* varDsc = &lvaTable[varNum];

    return varDsc->lvVerTypeInfo.GetClassHandleForValueClass();
}

//--------------------------------------------------------------------------------------------
// lvaFieldOffsetCmp - a static compare function passed to qsort() by Compiler::StructPromotionHelper;
//   compares fields' offsets.
//
// Arguments:
//   field1 - pointer to the first field;
//   field2 - pointer to the second field.
//
// Return value:
//   0 if the fields' offsets are equal, 1 if the first field has bigger offset, -1 otherwise.
//
int __cdecl Compiler::lvaFieldOffsetCmp(const void* field1, const void* field2)
{
    lvaStructFieldInfo* pFieldInfo1 = (lvaStructFieldInfo*)field1;
    lvaStructFieldInfo* pFieldInfo2 = (lvaStructFieldInfo*)field2;

    if (pFieldInfo1->fldOffset == pFieldInfo2->fldOffset)
    {
        return 0;
    }
    else
    {
        return (pFieldInfo1->fldOffset > pFieldInfo2->fldOffset) ? +1 : -1;
    }
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
#ifdef _TARGET_ARM_
    , requiresScratchVar(false)
#endif // _TARGET_ARM_
#ifdef DEBUG
    , retypedFieldsMap(compiler->getAllocator(CMK_DebugOnly))
#endif // DEBUG
{
}

#ifdef _TARGET_ARM_
//--------------------------------------------------------------------------------------------
// GetRequiresScratchVar - do we need a stack area to assemble small fields in order to place them in a register.
//
// Return value:
//   true if there was a small promoted variable and scratch var is required .
//
bool Compiler::StructPromotionHelper::GetRequiresScratchVar()
{
    return requiresScratchVar;
}

#endif // _TARGET_ARM_

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
#if defined(_TARGET_XARCH_)
    // This will allow promotion of 4 Vector<T> fields on AVX2 or Vector256<T> on AVX,
    // or 8 Vector<T>/Vector128<T> fields on SSE2.
    const int MaxOffset = MAX_NumOfFieldsInPromotableStruct * YMM_REGSIZE_BYTES;
#elif defined(_TARGET_ARM64_)
    const int MaxOffset = MAX_NumOfFieldsInPromotableStruct * FP_REGSIZE_BYTES;
#endif // defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
#else  // !FEATURE_SIMD
    const int MaxOffset = MAX_NumOfFieldsInPromotableStruct * sizeof(double);
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

#ifdef _TARGET_ARM_
    // On ARM, we have a requirement on the struct alignment; see below.
    unsigned structAlignment = roundUp(compHandle->getClassAlignmentRequirement(typeHnd), TARGET_POINTER_SIZE);
#endif // _TARGET_ARM_

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

#ifdef _TARGET_ARM_
        // On ARM, for struct types that don't use explicit layout, the alignment of the struct is
        // at least the max alignment of its fields.  We take advantage of this invariant in struct promotion,
        // so verify it here.
        if (fieldInfo.fldSize > structAlignment)
        {
            // Don't promote vars whose struct types violates the invariant.  (Alignment == size for primitives.)
            return false;
        }
        // If we have any small fields we will allocate a single PromotedStructScratch local var for the method.
        // This is a stack area that we use to assemble the small fields in order to place them in a register
        // argument.
        //
        if (fieldInfo.fldSize < TARGET_POINTER_SIZE)
        {
            requiresScratchVar = true;
        }
#endif // _TARGET_ARM_
    }

    // If we saw any GC pointer or by-ref fields above then CORINFO_FLG_CONTAINS_GC_PTR or
    // CORINFO_FLG_CONTAINS_STACK_PTR has to be set!
    noway_assert((containsGCpointers == false) ||
                 ((typeFlags & (CORINFO_FLG_CONTAINS_GC_PTR | CORINFO_FLG_CONTAINS_STACK_PTR)) != 0));

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

    // Explicitly check for HFA reg args and reject them for promotion here.
    // Promoting HFA args will fire an assert in lvaAssignFrameOffsets
    // when the HFA reg arg is struct promoted.
    //
    // TODO-PERF - Allow struct promotion for HFA register arguments
    if (varDsc->lvIsHfaRegArg())
    {
        JITDUMP("  struct promotion of V%02u is disabled because lvIsHfaRegArg()\n", lclNum);
        return false;
    }

#if !FEATURE_MULTIREG_STRUCT_PROMOTE
    if (varDsc->lvIsMultiRegArg)
    {
        JITDUMP("  struct promotion of V%02u is disabled because lvIsMultiRegArg\n", lclNum);
        return false;
    }
#endif

    if (varDsc->lvIsMultiRegRet)
    {
        JITDUMP("  struct promotion of V%02u is disabled because lvIsMultiRegRet\n", lclNum);
        return false;
    }

    CORINFO_CLASS_HANDLE typeHnd = varDsc->lvVerTypeInfo.GetClassHandle();
    return CanPromoteStructType(typeHnd);
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
    assert(lclNum < compiler->lvaCount);

    LclVarDsc* varDsc = &compiler->lvaTable[lclNum];
    assert(varTypeIsStruct(varDsc));
    assert(varDsc->lvVerTypeInfo.GetClassHandle() == structPromotionInfo.typeHnd);
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
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_) || defined(_TARGET_ARM_)
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
#endif // _TARGET_AMD64_ || _TARGET_ARM64_ || _TARGET_ARM_
    else if (varDsc->lvIsParam && !compiler->lvaIsImplicitByRefLocal(lclNum))
    {
#if FEATURE_MULTIREG_STRUCT_PROMOTE
        // Is this a variable holding a value with exactly two fields passed in
        // multiple registers?
        if ((structPromotionInfo.fieldCnt != 2) && compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
        {
            JITDUMP("Not promoting multireg struct local V%02u, because lvIsParam is true and #fields != 2\n", lclNum);
            shouldPromote = false;
        }
        else
#endif // !FEATURE_MULTIREG_STRUCT_PROMOTE

            // TODO-PERF - Implement struct promotion for incoming multireg structs
            //             Currently it hits assert(lvFieldCnt==1) in lclvar.cpp line 4417
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
    assert(!structPromotionInfo.fieldsSorted);
    qsort(structPromotionInfo.fields, structPromotionInfo.fieldCnt, sizeof(*structPromotionInfo.fields),
          lvaFieldOffsetCmp);
    structPromotionInfo.fieldsSorted = true;
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
        unsigned  simdSize;
        var_types simdBaseType = compiler->getBaseTypeAndSizeOfSIMDType(fieldInfo.fldTypeHnd, &simdSize);
        if (simdBaseType != TYP_UNKNOWN)
        {
            fieldInfo.fldType = compiler->getSIMDTypeForSize(simdSize);
            fieldInfo.fldSize = simdSize;
#ifdef DEBUG
            retypedFieldsMap.Set(fieldInfo.fldHnd, fieldInfo.fldType, RetypedAsScalarFieldsMap::Overwrite);
#endif // DEBUG
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
    LclVarDsc* varDsc = &compiler->lvaTable[lclNum];

    // We should never see a reg-sized non-field-addressed struct here.
    assert(!varDsc->lvRegStruct);

    assert(varDsc->lvVerTypeInfo.GetClassHandle() == structPromotionInfo.typeHnd);
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
        printf("\nPromoting struct local V%02u (%s):", lclNum,
               compiler->eeGetClassName(varDsc->lvVerTypeInfo.GetClassHandle()));
    }
#endif

    if (!structPromotionInfo.fieldsSorted)
    {
        SortStructFields();
    }

    for (unsigned index = 0; index < structPromotionInfo.fieldCnt; ++index)
    {
        const lvaStructFieldInfo* pFieldInfo = &structPromotionInfo.fields[index];

        if (varTypeIsFloating(pFieldInfo->fldType) || varTypeIsSIMD(pFieldInfo->fldType))
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
        unsigned varNum = compiler->lvaGrabTemp(false DEBUGARG(bufp));

        varDsc = &compiler->lvaTable[lclNum]; // lvaGrabTemp can reallocate the lvaTable

        LclVarDsc* fieldVarDsc       = &compiler->lvaTable[varNum];
        fieldVarDsc->lvType          = pFieldInfo->fldType;
        fieldVarDsc->lvExactSize     = pFieldInfo->fldSize;
        fieldVarDsc->lvIsStructField = true;
        fieldVarDsc->lvFieldHnd      = pFieldInfo->fldHnd;
        fieldVarDsc->lvFldOffset     = pFieldInfo->fldOffset;
        fieldVarDsc->lvFldOrdinal    = pFieldInfo->fldOrdinal;
        fieldVarDsc->lvParentLcl     = lclNum;
        fieldVarDsc->lvIsParam       = varDsc->lvIsParam;
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

        // Reset the implicitByRef flag.
        fieldVarDsc->lvIsImplicitByRef = 0;

        // Do we have a parameter that can be enregistered?
        //
        if (varDsc->lvIsRegArg)
        {
            fieldVarDsc->lvIsRegArg = true;
            fieldVarDsc->lvArgReg   = varDsc->lvArgReg;
#if FEATURE_MULTIREG_ARGS && defined(FEATURE_SIMD)
            if (varTypeIsSIMD(fieldVarDsc) && !compiler->lvaIsImplicitByRefLocal(lclNum))
            {
                // This field is a SIMD type, and will be considered to be passed in multiple registers
                // if the parent struct was. Note that this code relies on the fact that if there is
                // a SIMD field of an enregisterable struct, it is the only field.
                // We will assert that, in case future changes are made to the ABI.
                assert(varDsc->lvFieldCnt == 1);
                fieldVarDsc->lvOtherArgReg = varDsc->lvOtherArgReg;
            }
#endif // FEATURE_MULTIREG_ARGS && defined(FEATURE_SIMD)
        }
#endif

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

#if !defined(_TARGET_64BIT_)
//------------------------------------------------------------------------
// lvaPromoteLongVars: "Struct promote" all register candidate longs as if they are structs of two ints.
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
void Compiler::lvaPromoteLongVars()
{
    if ((opts.compFlags & CLFLG_REGVAR) == 0)
    {
        return;
    }

    // The lvaTable might grow as we grab temps. Make a local copy here.
    unsigned startLvaCount = lvaCount;
    for (unsigned lclNum = 0; lclNum < startLvaCount; lclNum++)
    {
        LclVarDsc* varDsc = &lvaTable[lclNum];
        if (!varTypeIsLong(varDsc) || varDsc->lvDoNotEnregister || varDsc->lvIsMultiRegArgOrRet() ||
            (varDsc->lvRefCnt() == 0) || varDsc->lvIsStructField || (fgNoStructPromotion && varDsc->lvIsParam))
        {
            continue;
        }

        varDsc->lvFieldCnt      = 2;
        varDsc->lvFieldLclStart = lvaCount;
        varDsc->lvPromoted      = true;
        varDsc->lvContainsHoles = false;

#ifdef DEBUG
        if (verbose)
        {
            printf("\nPromoting long local V%02u:", lclNum);
        }
#endif

        bool isParam = varDsc->lvIsParam;

        for (unsigned index = 0; index < 2; ++index)
        {
            // Grab the temp for the field local.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            char buf[200];
            sprintf_s(buf, sizeof(buf), "%s V%02u.%s (fldOffset=0x%x)", "field", lclNum, index == 0 ? "lo" : "hi",
                      index * 4);

            // We need to copy 'buf' as lvaGrabTemp() below caches a copy to its argument.
            size_t len  = strlen(buf) + 1;
            char*  bufp = getAllocator(CMK_DebugOnly).allocate<char>(len);
            strcpy_s(bufp, len, buf);
#endif

            unsigned varNum = lvaGrabTemp(false DEBUGARG(bufp)); // Lifetime of field locals might span multiple BBs, so
                                                                 // they are long lifetime temps.

            LclVarDsc* fieldVarDsc       = &lvaTable[varNum];
            fieldVarDsc->lvType          = TYP_INT;
            fieldVarDsc->lvExactSize     = genTypeSize(TYP_INT);
            fieldVarDsc->lvIsStructField = true;
            fieldVarDsc->lvFldOffset     = (unsigned char)(index * genTypeSize(TYP_INT));
            fieldVarDsc->lvFldOrdinal    = (unsigned char)index;
            fieldVarDsc->lvParentLcl     = lclNum;
            // Currently we do not support enregistering incoming promoted aggregates with more than one field.
            if (isParam)
            {
                fieldVarDsc->lvIsParam = true;
                lvaSetVarDoNotEnregister(varNum DEBUGARG(DNER_LongParamField));
            }
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nlvaTable after lvaPromoteLongVars\n");
        lvaTableDump();
    }
#endif // DEBUG
}
#endif // !defined(_TARGET_64BIT_)

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

void Compiler::lvaSetVarAddrExposed(unsigned varNum)
{
    noway_assert(varNum < lvaCount);

    LclVarDsc* varDsc = &lvaTable[varNum];

    varDsc->lvAddrExposed = 1;

    if (varDsc->lvPromoted)
    {
        noway_assert(varTypeIsStruct(varDsc));

        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            noway_assert(lvaTable[i].lvIsStructField);
            lvaTable[i].lvAddrExposed = 1; // Make field local as address-exposed.
            lvaSetVarDoNotEnregister(i DEBUGARG(DNER_AddrExposed));
        }
    }

    lvaSetVarDoNotEnregister(varNum DEBUGARG(DNER_AddrExposed));
}

/*****************************************************************************
 *
 *  Record that the local var "varNum" should not be enregistered (for one of several reasons.)
 */

void Compiler::lvaSetVarDoNotEnregister(unsigned varNum DEBUGARG(DoNotEnregisterReason reason))
{
    noway_assert(varNum < lvaCount);
    LclVarDsc* varDsc         = &lvaTable[varNum];
    varDsc->lvDoNotEnregister = 1;

#ifdef DEBUG
    if (verbose)
    {
        printf("\nLocal V%02u should not be enregistered because: ", varNum);
    }
    switch (reason)
    {
        case DNER_AddrExposed:
            JITDUMP("it is address exposed\n");
            assert(varDsc->lvAddrExposed);
            break;
        case DNER_IsStruct:
            JITDUMP("it is a struct\n");
            assert(varTypeIsStruct(varDsc));
            break;
        case DNER_IsStructArg:
            JITDUMP("it is a struct arg\n");
            assert(varTypeIsStruct(varDsc));
            break;
        case DNER_BlockOp:
            JITDUMP("written in a block op\n");
            varDsc->lvLclBlockOpAddr = 1;
            break;
        case DNER_LocalField:
            JITDUMP("was accessed as a local field\n");
            varDsc->lvLclFieldExpr = 1;
            break;
        case DNER_VMNeedsStackAddr:
            JITDUMP("needs stack addr\n");
            varDsc->lvVMNeedsStackAddr = 1;
            break;
        case DNER_LiveInOutOfHandler:
            JITDUMP("live in/out of a handler\n");
            varDsc->lvLiveInOutOfHndlr = 1;
            break;
        case DNER_LiveAcrossUnmanagedCall:
            JITDUMP("live across unmanaged call\n");
            varDsc->lvLiveAcrossUCall = 1;
            break;
        case DNER_DepField:
            JITDUMP("field of a dependently promoted struct\n");
            assert(varDsc->lvIsStructField && (lvaGetParentPromotionType(varNum) != PROMOTION_TYPE_INDEPENDENT));
            break;
        case DNER_NoRegVars:
            JITDUMP("opts.compFlags & CLFLG_REGVAR is not set\n");
            assert((opts.compFlags & CLFLG_REGVAR) == 0);
            break;
        case DNER_MinOptsGC:
            JITDUMP("It is a GC Ref and we are compiling MinOpts\n");
            assert(!JitConfig.JitMinOptsTrackGCrefs() && varTypeIsGC(varDsc->TypeGet()));
            break;
#ifdef JIT32_GCENCODER
        case DNER_PinningRef:
            JITDUMP("pinning ref\n");
            assert(varDsc->lvPinned);
            break;
#endif
#if !defined(_TARGET_64BIT_)
        case DNER_LongParamField:
            JITDUMP("it is a decomposed field of a long parameter\n");
            break;
#endif
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
        CORINFO_CLASS_HANDLE clsHnd = varDsc->lvVerTypeInfo.GetClassHandleForValueClass();
        structPassingKind    howToPassStruct;

        var_types type = getArgTypeForStruct(clsHnd, &howToPassStruct, isVarArg, varDsc->lvExactSize);

        if (howToPassStruct == SPK_ByValueAsHfa)
        {
            assert(type == TYP_STRUCT);
            return true;
        }

#if defined(UNIX_AMD64_ABI) || defined(_TARGET_ARM64_)
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
    noway_assert(varNum < lvaCount);

    LclVarDsc* varDsc = &lvaTable[varNum];
    if (setTypeInfo)
    {
        varDsc->lvVerTypeInfo = typeInfo(TI_STRUCT, typeHnd);
    }

    // Set the type and associated info if we haven't already set it.
    if (varDsc->lvType == TYP_UNDEF)
    {
        varDsc->lvType = TYP_STRUCT;
    }
    if (varDsc->lvExactSize == 0)
    {
        BOOL isValueClass = info.compCompHnd->isValueClass(typeHnd);

        if (isValueClass)
        {
            varDsc->lvExactSize = info.compCompHnd->getClassSize(typeHnd);
        }
        else
        {
            varDsc->lvExactSize = info.compCompHnd->getHeapClassSize(typeHnd);
        }

        // Normalize struct types, and fill in GC info for all types
        unsigned lvSize = varDsc->lvSize();
        // The struct needs to be a multiple of TARGET_POINTER_SIZE bytes for getClassGClayout() to be valid.
        assert((lvSize % TARGET_POINTER_SIZE) == 0);
        varDsc->lvGcLayout     = getAllocator(CMK_LvaTable).allocate<BYTE>(lvSize / TARGET_POINTER_SIZE);
        unsigned  numGCVars    = 0;
        var_types simdBaseType = TYP_UNKNOWN;
        if (isValueClass)
        {
            varDsc->lvType = impNormStructType(typeHnd, varDsc->lvGcLayout, &numGCVars, &simdBaseType);
        }
        else
        {
            numGCVars = info.compCompHnd->getClassGClayout(typeHnd, varDsc->lvGcLayout);
        }

        // We only save the count of GC vars in a struct up to 7.
        if (numGCVars >= 8)
        {
            numGCVars = 7;
        }

        varDsc->lvStructGcCount = numGCVars;

        if (isValueClass)
        {

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
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
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

#if FEATURE_SIMD
            if (simdBaseType != TYP_UNKNOWN)
            {
                assert(varTypeIsSIMD(varDsc));
                varDsc->lvSIMDType = true;
                varDsc->lvBaseType = simdBaseType;
            }
#endif // FEATURE_SIMD
#ifdef FEATURE_HFA
            // for structs that are small enough, we check and set lvIsHfa and lvHfaTypeIsFloat
            if (varDsc->lvExactSize <= MAX_PASS_MULTIREG_BYTES)
            {
                var_types hfaType = GetHfaType(typeHnd); // set to float or double if it is an HFA, otherwise TYP_UNDEF
                if (varTypeIsValidHfaType(hfaType))
                {
                    varDsc->SetHfaType(hfaType);

                    // hfa variables can never contain GC pointers
                    assert(varDsc->lvStructGcCount == 0);
                    // The size of this struct should be evenly divisible by 4 or 8
                    assert((varDsc->lvExactSize % genTypeSize(hfaType)) == 0);
                    // The number of elements in the HFA should fit into our MAX_ARG_REG_COUNT limit
                    assert((varDsc->lvExactSize / genTypeSize(hfaType)) <= MAX_ARG_REG_COUNT);
                }
            }
#endif // FEATURE_HFA
        }
    }
    else
    {
#if FEATURE_SIMD
        assert(!varTypeIsSIMD(varDsc) || (varDsc->lvBaseType != TYP_UNKNOWN));
#endif // FEATURE_SIMD
    }

#ifndef _TARGET_64BIT_
    BOOL fDoubleAlignHint = FALSE;
#ifdef _TARGET_X86_
    fDoubleAlignHint = TRUE;
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
#endif // not _TARGET_64BIT_

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
}

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
#ifdef FEATURE_HFA
#if defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)
    LclVarDsc* varDsc = &lvaTable[varNum];
    // For varargs methods incoming and outgoing arguments should not be treated
    // as HFA.
    varDsc->SetHfaType(TYP_UNDEF);
#endif // defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)
#endif // FEATURE_HFA
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

    LclVarDsc* varDsc = &lvaTable[varNum];
    assert(varDsc->lvType == TYP_REF);

    // We shoud not have any ref type information for this var.
    assert(varDsc->lvClassHnd == nullptr);
    assert(!varDsc->lvClassIsExact);

    JITDUMP("\nlvaSetClass: setting class for V%02i to (%p) %s %s\n", varNum, dspPtr(clsHnd),
            info.compCompHnd->getClassName(clsHnd), isExact ? " [exact]" : "");

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
//    fallback.

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

    LclVarDsc* varDsc = &lvaTable[varNum];
    assert(varDsc->lvType == TYP_REF);

    // We should already have a class
    assert(varDsc->lvClassHnd != nullptr);

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
        JITDUMP(" from (%p) %s%s", dspPtr(varDsc->lvClassHnd), info.compCompHnd->getClassName(varDsc->lvClassHnd),
                varDsc->lvClassIsExact ? " [exact]" : "");
        JITDUMP(" to (%p) %s%s\n", dspPtr(clsHnd), info.compCompHnd->getClassName(clsHnd), isExact ? " [exact]" : "");
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

/*****************************************************************************
 * Returns the array of BYTEs containing the GC layout information
 */

BYTE* Compiler::lvaGetGcLayout(unsigned varNum)
{
    assert(varTypeIsStruct(lvaTable[varNum].lvType) && (lvaTable[varNum].lvExactSize >= TARGET_POINTER_SIZE));

    return lvaTable[varNum].lvGcLayout;
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

            // Keep prefast happy
            __fallthrough;

#endif // FEATURE_FIXED_OUT_ARGS

        default: // This must be a primitive var. Fall out of switch statement
            break;
    }
#ifdef _TARGET_64BIT_
    // We only need this Quirk for _TARGET_64BIT_
    if (lvaTable[varNum].lvQuirkToLong)
    {
        noway_assert(lvaTable[varNum].lvAddrExposed);
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

            // Keep prefast happy
            __fallthrough;

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
BasicBlock::weight_t BasicBlock::getCalledCount(Compiler* comp)
{
    // when we don't have profile data then fgCalledCount will be BB_UNITY_WEIGHT (100)
    BasicBlock::weight_t calledCount = comp->fgCalledCount;

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
BasicBlock::weight_t BasicBlock::getBBWeight(Compiler* comp)
{
    if (this->bbWeight == 0)
    {
        return 0;
    }
    else
    {
        weight_t calledCount = getCalledCount(comp);

        // Normalize the bbWeights by multiplying by BB_UNITY_WEIGHT and dividing by the calledCount.
        //
        // 1. For methods that do not have IBC data the called weight will always be 100 (BB_UNITY_WEIGHT)
        //     and the entry point bbWeight value is almost always 100 (BB_UNITY_WEIGHT)
        // 2.  For methods that do have IBC data the called weight is the actual number of calls
        //     from the IBC data and the entry point bbWeight value is almost always the actual
        //     number of calls from the IBC data.
        //
        // "almost always" - except for the rare case where a loop backedge jumps to BB01
        //
        // We also perform a rounding operation by adding half of the 'calledCount' before performing
        // the division.
        //
        // Thus for both cases we will return 100 (BB_UNITY_WEIGHT) for the entry point BasicBlock
        //
        // Note that with a 100 (BB_UNITY_WEIGHT) values between 1 and 99 represent decimal fractions.
        // (i.e. 33 represents 33% and 75 represents 75%, and values greater than 100 require
        //  some kind of loop backedge)
        //

        if (this->bbWeight < (BB_MAX_WEIGHT / BB_UNITY_WEIGHT))
        {
            // Calculate the result using unsigned arithmetic
            weight_t result = ((this->bbWeight * BB_UNITY_WEIGHT) + (calledCount / 2)) / calledCount;

            // We don't allow a value of zero, as that would imply rarely run
            return max(1, result);
        }
        else
        {
            // Calculate the full result using floating point
            double fullResult = ((double)this->bbWeight * (double)BB_UNITY_WEIGHT) / (double)calledCount;

            if (fullResult < (double)BB_MAX_WEIGHT)
            {
                // Add 0.5 and truncate to unsigned
                return (weight_t)(fullResult + 0.5);
            }
            else
            {
                return BB_MAX_WEIGHT;
            }
        }
    }
}

/*****************************************************************************
 *
 *  Compare function passed to qsort() by Compiler::lclVars.lvaSortByRefCount().
 *  when generating SMALL_CODE.
 *    Return positive if dsc2 has a higher ref count
 *    Return negative if dsc1 has a higher ref count
 *    Return zero     if the ref counts are the same
 */

/* static */
int __cdecl Compiler::RefCntCmp(const void* op1, const void* op2)
{
    LclVarDsc* dsc1 = *(LclVarDsc**)op1;
    LclVarDsc* dsc2 = *(LclVarDsc**)op2;

    /* Make sure we preference tracked variables over untracked variables */

    if (dsc1->lvTracked != dsc2->lvTracked)
    {
        return (dsc2->lvTracked) ? +1 : -1;
    }

    unsigned weight1 = dsc1->lvRefCnt();
    unsigned weight2 = dsc2->lvRefCnt();

#ifndef _TARGET_ARM_
    // ARM-TODO: this was disabled for ARM under !FEATURE_FP_REGALLOC; it was probably a left-over from
    // legacy backend. It should be enabled and verified.

    /* Force integer candidates to sort above float candidates */

    bool isFloat1 = isFloatRegType(dsc1->lvType);
    bool isFloat2 = isFloatRegType(dsc2->lvType);

    if (isFloat1 != isFloat2)
    {
        if (weight2 && isFloat1)
        {
            return +1;
        }
        if (weight1 && isFloat2)
        {
            return -1;
        }
    }
#endif

    int diff = weight2 - weight1;

    if (diff != 0)
    {
        return diff;
    }

    /* The unweighted ref counts were the same */
    /* If the weighted ref counts are different then use their difference */
    diff = dsc2->lvRefCntWtd() - dsc1->lvRefCntWtd();

    if (diff != 0)
    {
        return diff;
    }

    /* We have equal ref counts and weighted ref counts */

    /* Break the tie by: */
    /* Increasing the weight by 2   if we are a register arg */
    /* Increasing the weight by 0.5 if we are a GC type */
    /* Increasing the weight by 0.5 if we were enregistered in the previous pass  */

    if (weight1)
    {
        if (dsc1->lvIsRegArg)
        {
            weight2 += 2 * BB_UNITY_WEIGHT;
        }

        if (varTypeIsGC(dsc1->TypeGet()))
        {
            weight1 += BB_UNITY_WEIGHT / 2;
        }

        if (dsc1->lvRegister)
        {
            weight1 += BB_UNITY_WEIGHT / 2;
        }
    }

    if (weight2)
    {
        if (dsc2->lvIsRegArg)
        {
            weight2 += 2 * BB_UNITY_WEIGHT;
        }

        if (varTypeIsGC(dsc2->TypeGet()))
        {
            weight2 += BB_UNITY_WEIGHT / 2;
        }

        if (dsc2->lvRegister)
        {
            weight2 += BB_UNITY_WEIGHT / 2;
        }
    }

    diff = weight2 - weight1;

    if (diff != 0)
    {
        return diff;
    }

    /* To achieve a Stable Sort we use the LclNum (by way of the pointer address) */

    if (dsc1 < dsc2)
    {
        return -1;
    }
    if (dsc1 > dsc2)
    {
        return +1;
    }

    return 0;
}

/*****************************************************************************
 *
 *  Compare function passed to qsort() by Compiler::lclVars.lvaSortByRefCount().
 *  when not generating SMALL_CODE.
 *    Return positive if dsc2 has a higher weighted ref count
 *    Return negative if dsc1 has a higher weighted ref count
 *    Return zero     if the ref counts are the same
 */

/* static */
int __cdecl Compiler::WtdRefCntCmp(const void* op1, const void* op2)
{
    LclVarDsc* dsc1 = *(LclVarDsc**)op1;
    LclVarDsc* dsc2 = *(LclVarDsc**)op2;

    /* Make sure we preference tracked variables over untracked variables */

    if (dsc1->lvTracked != dsc2->lvTracked)
    {
        return (dsc2->lvTracked) ? +1 : -1;
    }

    unsigned weight1 = dsc1->lvRefCntWtd();
    unsigned weight2 = dsc2->lvRefCntWtd();

#ifndef _TARGET_ARM_
    // ARM-TODO: this was disabled for ARM under !FEATURE_FP_REGALLOC; it was probably a left-over from
    // legacy backend. It should be enabled and verified.

    /* Force integer candidates to sort above float candidates */

    bool isFloat1 = isFloatRegType(dsc1->lvType);
    bool isFloat2 = isFloatRegType(dsc2->lvType);

    if (isFloat1 != isFloat2)
    {
        if (weight2 && isFloat1)
        {
            return +1;
        }
        if (weight1 && isFloat2)
        {
            return -1;
        }
    }
#endif

    if (weight1 && dsc1->lvIsRegArg)
    {
        weight1 += 2 * BB_UNITY_WEIGHT;
    }

    if (weight2 && dsc2->lvIsRegArg)
    {
        weight2 += 2 * BB_UNITY_WEIGHT;
    }

    if (weight2 > weight1)
    {
        return 1;
    }
    else if (weight2 < weight1)
    {
        return -1;
    }

    // Otherwise, we have equal weighted ref counts.

    /* If the unweighted ref counts are different then use their difference */
    int diff = (int)dsc2->lvRefCnt() - (int)dsc1->lvRefCnt();

    if (diff != 0)
    {
        return diff;
    }

    /* If one is a GC type and the other is not the GC type wins */
    if (varTypeIsGC(dsc1->TypeGet()) != varTypeIsGC(dsc2->TypeGet()))
    {
        if (varTypeIsGC(dsc1->TypeGet()))
        {
            diff = -1;
        }
        else
        {
            diff = +1;
        }

        return diff;
    }

    /* If one was enregistered in the previous pass then it wins */
    if (dsc1->lvRegister != dsc2->lvRegister)
    {
        if (dsc1->lvRegister)
        {
            diff = -1;
        }
        else
        {
            diff = +1;
        }

        return diff;
    }

    /* We have a tie! */

    /* To achieve a Stable Sort we use the LclNum (by way of the pointer address) */

    if (dsc1 < dsc2)
    {
        return -1;
    }
    if (dsc1 > dsc2)
    {
        return +1;
    }

    return 0;
}

/*****************************************************************************
 *
 *  Sort the local variable table by refcount and assign tracking indices.
 */

void Compiler::lvaSortOnly()
{
    /* Now sort the variable table by ref-count */

    qsort(lvaRefSorted, lvaCount, sizeof(*lvaRefSorted), (compCodeOpt() == SMALL_CODE) ? RefCntCmp : WtdRefCntCmp);
    lvaDumpRefCounts();
}

void Compiler::lvaDumpRefCounts()
{
#ifdef DEBUG

    if (verbose && lvaCount)
    {
        printf("refCnt table for '%s':\n", info.compMethodName);

        for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
        {
            unsigned refCnt = lvaRefSorted[lclNum]->lvRefCnt();
            if (refCnt == 0)
            {
                break;
            }
            unsigned refCntWtd = lvaRefSorted[lclNum]->lvRefCntWtd();

            printf("   ");
            gtDispLclVar((unsigned)(lvaRefSorted[lclNum] - lvaTable));
            printf(" [%6s]: refCnt = %4u, refCntWtd = %6s", varTypeName(lvaRefSorted[lclNum]->TypeGet()), refCnt,
                   refCntWtd2str(refCntWtd));
            printf("\n");
        }

        printf("\n");
    }

#endif
}

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

    unsigned   lclNum;
    LclVarDsc* varDsc;

    LclVarDsc** refTab;

    /* We'll sort the variables by ref count - allocate the sorted table */

    lvaRefSorted = refTab = new (this, CMK_LvaTable) LclVarDsc*[lvaCount];

    /* Fill in the table used for sorting */

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        /* Append this variable to the table for sorting */

        *refTab++ = varDsc;

        /* For now assume we'll be able to track all locals */

        varDsc->lvTracked = 1;

        /* If the ref count is zero */
        if (varDsc->lvRefCnt() == 0)
        {
            /* Zero ref count, make this untracked */
            varDsc->lvTracked = 0;
            varDsc->setLvRefCntWtd(0);
        }

#if !defined(_TARGET_64BIT_)
        if (varTypeIsLong(varDsc) && varDsc->lvPromoted)
        {
            varDsc->lvTracked = 0;
        }
#endif // !defined(_TARGET_64BIT_)

        // Variables that are address-exposed, and all struct locals, are never enregistered, or tracked.
        // (The struct may be promoted, and its field variables enregistered/tracked, or the VM may "normalize"
        // its type so that its not seen by the JIT as a struct.)
        // Pinned variables may not be tracked (a condition of the GCInfo representation)
        // or enregistered, on x86 -- it is believed that we can enregister pinned (more properly, "pinning")
        // references when using the general GC encoding.
        if (varDsc->lvAddrExposed)
        {
            varDsc->lvTracked = 0;
            assert(varDsc->lvType != TYP_STRUCT ||
                   varDsc->lvDoNotEnregister); // For structs, should have set this when we set lvAddrExposed.
        }
        else if (varTypeIsStruct(varDsc))
        {
            // Promoted structs will never be considered for enregistration anyway,
            // and the DoNotEnregister flag was used to indicate whether promotion was
            // independent or dependent.
            if (varDsc->lvPromoted)
            {
                varDsc->lvTracked = 0;
            }
            else if ((varDsc->lvType == TYP_STRUCT) && !varDsc->lvRegStruct)
            {
                lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_IsStruct));
            }
        }
        else if (varDsc->lvIsStructField && (lvaGetParentPromotionType(lclNum) != PROMOTION_TYPE_INDEPENDENT))
        {
            // SSA must exclude struct fields that are not independently promoted
            // as dependent fields could be assigned using a CopyBlock
            // resulting in a single node causing multiple SSA definitions
            // which isn't currently supported by SSA
            //
            // TODO-CQ:  Consider using lvLclBlockOpAddr and only marking these LclVars
            // untracked when a blockOp is used to assign the struct.
            //
            varDsc->lvTracked = 0; // so, don't mark as tracked
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_DepField));
        }
        else if (varDsc->lvPinned)
        {
            varDsc->lvTracked = 0;
#ifdef JIT32_GCENCODER
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_PinningRef));
#endif
        }
        else if (opts.MinOpts() && !JitConfig.JitMinOptsTrackGCrefs() && varTypeIsGC(varDsc->TypeGet()))
        {
            varDsc->lvTracked = 0;
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_MinOptsGC));
        }
        else if ((opts.compFlags & CLFLG_REGVAR) == 0)
        {
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_NoRegVars));
        }
#if defined(JIT32_GCENCODER) && defined(WIN64EXCEPTIONS)
        else if (lvaIsOriginalThisArg(lclNum) && (info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0)
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
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_LiveInOutOfHandler));
            continue;
        }

        var_types type = genActualType(varDsc->TypeGet());

        switch (type)
        {
#if CPU_HAS_FP_SUPPORT
            case TYP_FLOAT:
            case TYP_DOUBLE:
#endif
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

                __fallthrough;

            default:
                varDsc->lvTracked = 0;
        }
    }

    /* Now sort the variable table by ref-count */

    lvaSortOnly();

    /* Decide which variables will be worth tracking */

    if (lvaCount > lclMAX_TRACKED)
    {
        /* Mark all variables past the first 'lclMAX_TRACKED' as untracked */

        for (lclNum = lclMAX_TRACKED; lclNum < lvaCount; lclNum++)
        {
            lvaRefSorted[lclNum]->lvTracked = 0;
        }
    }

    if (lvaTrackedToVarNum == nullptr)
    {
        lvaTrackedToVarNum = new (getAllocator(CMK_LvaTable)) unsigned[lclMAX_TRACKED];
    }

#ifdef DEBUG
    // Re-Initialize to -1 for safety in debug build.
    memset(lvaTrackedToVarNum, -1, lclMAX_TRACKED * sizeof(unsigned));
#endif

    /* Assign indices to all the variables we've decided to track */

    for (lclNum = 0; lclNum < min(lvaCount, lclMAX_TRACKED); lclNum++)
    {
        varDsc = lvaRefSorted[lclNum];
        if (varDsc->lvTracked)
        {
            noway_assert(varDsc->lvRefCnt() > 0);

            /* This variable will be tracked - assign it an index */

            lvaTrackedToVarNum[lvaTrackedCount] = (unsigned)(varDsc - lvaTable); // The type of varDsc and lvaTable
            // is LclVarDsc. Subtraction will give us
            // the index.
            varDsc->lvVarIndex = lvaTrackedCount++;
        }
    }

    // We have a new epoch, and also cache the tracked var count in terms of size_t's sufficient to hold that many bits.
    lvaCurEpoch++;
    lvaTrackedCountInSizeTUnits =
        roundUp((unsigned)lvaTrackedCount, (unsigned)(sizeof(size_t) * 8)) / unsigned(sizeof(size_t) * 8);

#ifdef DEBUG
    VarSetOps::AssignNoCopy(this, lvaTrackedVars, VarSetOps::MakeFull(this));
#endif
}

#if ASSERTION_PROP
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
#endif // ASSERTION_PROP

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
#elif defined(_TARGET_ARM64_) || defined(UNIX_AMD64_ABI)
        // lvSize performs a roundup.
        stackSize = this->lvSize();

#if defined(_TARGET_ARM64_)
        if ((stackSize > TARGET_POINTER_SIZE * 2) && (!this->lvIsHfa()))
        {
            // If the size is greater than 16 bytes then it will
            // be passed by reference.
            stackSize = TARGET_POINTER_SIZE;
        }
#endif // defined(_TARGET_ARM64_)

#else // !_TARGET_ARM64_ !WINDOWS_AMD64_ABI !UNIX_AMD64_ABI

        NYI("Unsupported target.");
        unreached();

#endif //  !_TARGET_ARM64_ !WINDOWS_AMD64_ABI !UNIX_AMD64_ABI
    }
    else
    {
        stackSize = TARGET_POINTER_SIZE;
    }

    return stackSize;
}

/**********************************************************************************
* Get type of a variable when passed as an argument.
*/
var_types LclVarDsc::lvaArgType()
{
    var_types type = TypeGet();

#ifdef _TARGET_AMD64_
#ifdef UNIX_AMD64_ABI
    if (type == TYP_STRUCT)
    {
        NYI("lvaArgType");
    }
#else  //! UNIX_AMD64_ABI
    if (type == TYP_STRUCT)
    {
        switch (lvExactSize)
        {
            case 1:
                type = TYP_BYTE;
                break;
            case 2:
                type = TYP_SHORT;
                break;
            case 4:
                type = TYP_INT;
                break;
            case 8:
                switch (*lvGcLayout)
                {
                    case TYPE_GC_NONE:
                        type = TYP_I_IMPL;
                        break;

                    case TYPE_GC_REF:
                        type = TYP_REF;
                        break;

                    case TYPE_GC_BYREF:
                        type = TYP_BYREF;
                        break;

                    default:
                        unreached();
                }
                break;

            default:
                type = TYP_BYREF;
                break;
        }
    }
#endif // !UNIX_AMD64_ABI
#elif defined(_TARGET_ARM64_)
    if (type == TYP_STRUCT)
    {
        NYI("lvaArgType");
    }
#elif defined(_TARGET_X86_)
// Nothing to do; use the type as is.
#else
    NYI("lvaArgType");
#endif //_TARGET_AMD64_

    return type;
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
//     In checked builds:
//
//     Verifies that local accesses are consistenly typed.
//     Verifies that casts remain in bounds.

void Compiler::lvaMarkLclRefs(GenTree* tree, BasicBlock* block, GenTreeStmt* stmt, bool isRecompute)
{
    const BasicBlock::weight_t weight = block->getBBWeight(this);

    /* Is this a call to unmanaged code ? */
    if (tree->gtOper == GT_CALL && tree->gtFlags & GTF_CALL_UNMANAGED)
    {
        assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers())
        {
            /* Get the special variable descriptor */

            unsigned lclNum = info.compLvFrameListRoot;

            noway_assert(lclNum <= lvaCount);
            LclVarDsc* varDsc = lvaTable + lclNum;

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
            GenTree* op1 = tree->gtOp.gtOp1;
            GenTree* op2 = tree->gtOp.gtOp2;

#if OPT_BOOL_OPS

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

                        if (op2->gtIntCon.gtIconVal == 0)
                        {
                            break;
                        }
                        if (op2->gtIntCon.gtIconVal == 1)
                        {
                            break;
                        }

                        // Not 0 or 1, fall through ....
                        __fallthrough;

                    default:

                        if (op2->OperIsCompare())
                        {
                            break;
                        }

                    NOT_BOOL:

                        lclNum = op1->gtLclVarCommon.gtLclNum;
                        noway_assert(lclNum < lvaCount);

                        lvaTable[lclNum].lvIsBoolean = false;
                        break;
                }
            }
#endif
        }
    }

    if ((tree->gtOper != GT_LCL_VAR) && (tree->gtOper != GT_LCL_FLD))
    {
        return;
    }

    /* This must be a local variable reference */

    assert((tree->gtOper == GT_LCL_VAR) || (tree->gtOper == GT_LCL_FLD));
    unsigned lclNum = tree->gtLclVarCommon.gtLclNum;

    noway_assert(lclNum < lvaCount);
    LclVarDsc* varDsc = lvaTable + lclNum;

    /* Increment the reference counts */

    varDsc->incRefCnts(weight, this);

    if (!isRecompute)
    {
        if (lvaVarAddrExposed(lclNum))
        {
            varDsc->lvIsBoolean = false;
        }

        if (tree->gtOper == GT_LCL_FLD)
        {
#if ASSERTION_PROP
            // variables that have uses inside a GT_LCL_FLD
            // cause problems, so we will disqualify them here
            varDsc->lvaDisqualifyVar();
#endif // ASSERTION_PROP
            return;
        }

#if ASSERTION_PROP
        if (fgDomsComputed && IsDominatedByExceptionalEntry(block))
        {
            SetVolatileHint(varDsc);
        }

        /* Record if the variable has a single def or not */

        if (!varDsc->lvDisqualify) // If this variable is already disqualified we can skip this
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
#endif // ASSERTION_PROP

        bool allowStructs = false;
#ifdef UNIX_AMD64_ABI
        // On System V the type of the var could be a struct type.
        allowStructs = varTypeIsStruct(varDsc);
#endif // UNIX_AMD64_ABI

        /* Variables must be used as the same type throughout the method */
        noway_assert(tiVerificationNeeded || varDsc->lvType == TYP_UNDEF || tree->gtType == TYP_UNKNOWN ||
                     allowStructs || genActualType(varDsc->TypeGet()) == genActualType(tree->gtType) ||
                     (tree->gtType == TYP_BYREF && varDsc->TypeGet() == TYP_I_IMPL) ||
                     (tree->gtType == TYP_I_IMPL && varDsc->TypeGet() == TYP_BYREF) || (tree->gtFlags & GTF_VAR_CAST) ||
                     varTypeIsFloating(varDsc->TypeGet()) && varTypeIsFloating(tree->gtType));

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
        BasicBlock*  m_block;
        GenTreeStmt* m_stmt;
        bool         m_isRecompute;

    public:
        enum
        {
            DoPreOrder = true,
        };

        MarkLocalVarsVisitor(Compiler* compiler, BasicBlock* block, GenTreeStmt* stmt, bool isRecompute)
            : GenTreeVisitor<MarkLocalVarsVisitor>(compiler), m_block(block), m_stmt(stmt), m_isRecompute(isRecompute)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            m_compiler->lvaMarkLclRefs(*use, m_block, m_stmt, m_isRecompute);
            return WALK_CONTINUE;
        }
    };

    JITDUMP("\n*** %s local variables in block " FMT_BB " (weight=%s)\n", isRecompute ? "recomputing" : "marking",
            block->bbNum, refCntWtd2str(block->getBBWeight(this)));

    for (GenTreeStmt* stmt = block->FirstNonPhiDef(); stmt != nullptr; stmt = stmt->getNextStmt())
    {
        MarkLocalVarsVisitor visitor(this, block, stmt, isRecompute);
        DISPTREE(stmt);
        visitor.WalkTree(&stmt->gtStmtExpr, nullptr);
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
    if (info.compCallUnmanaged != 0)
    {
        assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers())
        {
            noway_assert(info.compLvFrameListRoot >= info.compLocalsCount && info.compLvFrameListRoot < lvaCount);
        }
    }

#if !FEATURE_EH_FUNCLETS

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
        LclVarDsc* shadowSPslotsVar   = &lvaTable[lvaShadowSPslotsVar];
        shadowSPslotsVar->lvType      = TYP_BLK;
        shadowSPslotsVar->lvExactSize = (slotsNeeded * TARGET_POINTER_SIZE);
    }

#endif // !FEATURE_EH_FUNCLETS

    // PSPSym and LocAllocSPvar are not used by the CoreRT ABI
    if (!IsTargetAbi(CORINFO_CORERT_ABI))
    {
#if FEATURE_EH_FUNCLETS
        if (ehNeedsPSPSym())
        {
            lvaPSPSym            = lvaGrabTempWithImplicitUse(false DEBUGARG("PSPSym"));
            LclVarDsc* lclPSPSym = &lvaTable[lvaPSPSym];
            lclPSPSym->lvType    = TYP_I_IMPL;
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
            LclVarDsc* locAllocSPvar = &lvaTable[lvaLocAllocSPvar];
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

    // If we're not optimizing, we're done.
    if (opts.OptimizationDisabled())
    {
        return;
    }

#if ASSERTION_PROP
    assert(opts.OptimizationEnabled());

    // Note: optAddCopies() depends on lvaRefBlks, which is set in lvaMarkLocalVars(BasicBlock*), called above.
    optAddCopies();
#endif

    if (lvaKeepAliveAndReportThis())
    {
        lvaTable[0].lvImplicitlyReferenced = 1;
        // This isn't strictly needed as we will make a copy of the param-type-arg
        // in the prolog. However, this ensures that the LclVarDsc corresponding to
        // info.compTypeCtxtArg is valid.
    }
    else if (lvaReportParamTypeArg())
    {
        lvaTable[info.compTypeCtxtArg].lvImplicitlyReferenced = 1;
    }

    lvaSortByRefCount();
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

void Compiler::lvaComputeRefCounts(bool isRecompute, bool setSlotNumbers)
{
    JITDUMP("\n*** lvaComputeRefCounts ***\n");
    unsigned   lclNum = 0;
    LclVarDsc* varDsc = nullptr;

    // Fast path for minopts and debug codegen.
    //
    // On first compute: mark all locals as implicitly referenced and untracked.
    // On recompute: do nothing.
    if (opts.OptimizationDisabled())
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
            varDsc->setLvRefCntWtd(0);

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
        varDsc->lvSingleDef = varDsc->lvIsParam;
    }

    JITDUMP("\n*** lvaComputeRefCounts -- explicit counts ***\n");

    // Second, account for all explicit local variable references
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        if (block->IsLIR())
        {
            assert(isRecompute);

            const BasicBlock::weight_t weight = block->getBBWeight(this);
            for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
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
                        const unsigned lclNum = node->AsLclVarCommon()->gtLclNum;
                        lvaTable[lclNum].incRefCnts(weight, this);
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
#ifdef _TARGET_ARM_
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

    if (lvaDoneFrameLayout >= REGALLOC_FRAME_LAYOUT)
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

#if FEATURE_EH_FUNCLETS && defined(_TARGET_AMD64_)
    if (lvaPSPSym != BAD_VAR_NUM)
    {
        // We need to fix the offset of the PSPSym so there is no padding between it and the outgoing argument space.
        // Without this code, lvaAlignFrame might have put the padding lower than the PSPSym, which would be between
        // the PSPSym and the outgoing argument space.
        varDsc = &lvaTable[lvaPSPSym];
        assert(varDsc->lvFramePointerBased); // We always access it RBP-relative.
        assert(!varDsc->lvMustInit);         // It is never "must init".
        varDsc->lvStkOffs = codeGen->genCallerSPtoInitialSPdelta() + lvaLclSize(lvaOutgoingArgSpaceVar);
    }
#endif

    // The delta to be added to virtual offset to adjust it relative to frame pointer or SP
    int delta = 0;

#ifdef _TARGET_XARCH_
    delta += REGSIZE_BYTES; // pushed PC (return address) for x86/x64

    if (codeGen->doubleAlignOrFramePointerUsed())
    {
        delta += REGSIZE_BYTES; // pushed EBP (frame pointer)
    }
#endif

    if (!codeGen->isFramePointerUsed())
    {
        // pushed registers, return address, and padding
        delta += codeGen->genTotalFrameSize();
    }
#if defined(_TARGET_ARM_)
    else
    {
        // We set FP to be after LR, FP
        delta += 2 * REGSIZE_BYTES;
    }
#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    else
    {
        // FP is used.
        delta += codeGen->genTotalFrameSize() - codeGen->genSPtoFPdelta();
    }
#endif //_TARGET_AMD64_

    unsigned lclNum;
    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        bool doAssignStkOffs = true;

        // Can't be relative to EBP unless we have an EBP
        noway_assert(!varDsc->lvFramePointerBased || codeGen->doubleAlignOrFramePointerUsed());

        // Is this a non-param promoted struct field?
        //   if so then set doAssignStkOffs to false.
        //
        if (varDsc->lvIsStructField && !varDsc->lvIsParam)
        {
            LclVarDsc*       parentvarDsc  = &lvaTable[varDsc->lvParentLcl];
            lvaPromotionType promotionType = lvaGetPromotionType(parentvarDsc);

            if (promotionType == PROMOTION_TYPE_DEPENDENT)
            {
                doAssignStkOffs = false; // Assigned later in lvaAssignFrameOffsetsToPromotedStructs()
            }
        }

        if (!varDsc->lvOnFrame)
        {
            if (!varDsc->lvIsParam
#if !defined(_TARGET_AMD64_)
                || (varDsc->lvIsRegArg
#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
                    && compIsProfilerHookNeeded() &&
                    !lvaIsPreSpilled(lclNum, codeGen->regSet.rsMaskPreSpillRegs(false)) // We need assign stack offsets
                                                                                        // for prespilled arguments
#endif
                    )
#endif // !defined(_TARGET_AMD64_)
                    )
            {
                doAssignStkOffs = false; // Not on frame or an incomming stack arg
            }
        }

        if (doAssignStkOffs)
        {
            varDsc->lvStkOffs += delta;

#if DOUBLE_ALIGN
            if (genDoubleAlign() && !codeGen->isFramePointerUsed())
            {
                if (varDsc->lvFramePointerBased)
                {
                    varDsc->lvStkOffs -= delta;

                    // We need to re-adjust the offsets of the parameters so they are EBP
                    // relative rather than stack/frame pointer relative

                    varDsc->lvStkOffs += (2 * TARGET_POINTER_SIZE); // return address and pushed EBP

                    noway_assert(varDsc->lvStkOffs >= FIRST_ARG_STACK_OFFS);
                }
            }
#endif
            // On System V environments the stkOffs could be 0 for params passed in registers.
            assert(codeGen->isFramePointerUsed() ||
                   varDsc->lvStkOffs >= 0); // Only EBP relative references can have negative offsets
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
        varDsc                      = &lvaTable[lvaOutgoingArgSpaceVar];
        varDsc->lvStkOffs           = 0;
        varDsc->lvFramePointerBased = false;
        varDsc->lvMustInit          = false;
    }

#endif // FEATURE_FIXED_OUT_ARGS
}

#ifdef _TARGET_ARM_
bool Compiler::lvaIsPreSpilled(unsigned lclNum, regMaskTP preSpillMask)
{
    const LclVarDsc& desc = lvaTable[lclNum];
    return desc.lvIsRegArg && (preSpillMask & genRegMask(desc.lvArgReg));
}
#endif // _TARGET_ARM_

/*****************************************************************************
 *  lvaUpdateArgsWithInitialReg() : For each argument variable descriptor, update
 *  its current register with the initial register as assigned by LSRA.
 */
void Compiler::lvaUpdateArgsWithInitialReg()
{
    if (!compLSRADone)
    {
        return;
    }

    for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
    {
        LclVarDsc* varDsc = lvaTable + lclNum;

        if (varDsc->lvPromotedStruct())
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = lvaTable + fieldVarNum;
        }

        noway_assert(varDsc->lvIsParam);

        if (varDsc->lvIsRegCandidate())
        {
            varDsc->lvRegNum = varDsc->lvArgInitReg;
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

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_L2R)
    {
        argOffs = compArgSize;
    }

    /* Update the argOffs to reflect arguments that are passed in registers */

    noway_assert(codeGen->intRegState.rsCalleeRegArgCount <= MAX_REG_ARG);
    noway_assert(compArgSize >= codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES);

#ifdef _TARGET_X86_
    argOffs -= codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES;
#endif

    // Update the arg initial register locations.
    lvaUpdateArgsWithInitialReg();

    /* Is there a "this" argument? */

    if (!info.compIsStatic)
    {
        noway_assert(lclNum == info.compThisArg);
#ifndef _TARGET_X86_
        argOffs =
            lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
#endif // _TARGET_X86_
        lclNum++;
    }

    /* if we have a hidden buffer parameter, that comes here */

    if (info.compRetBuffArg != BAD_VAR_NUM)
    {
        noway_assert(lclNum == info.compRetBuffArg);
        noway_assert(lvaTable[lclNum].lvIsRegArg);
#ifndef _TARGET_X86_
        argOffs =
            lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
#endif // _TARGET_X86_
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

#ifdef _TARGET_ARM_
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
                         << lvaTable[preSpillLclNum].lvArgReg);
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
            argOffs =
                lvaAssignVirtualFrameOffsetToArg(stkLclNum, eeGetArgSize(argLst, &info.compMethodInfo->args), argOffs);
            argLcls++;
        }
        argLst = info.compCompHnd->getArgNext(argLst);
    }

    lclNum += argLcls;
#else // !_TARGET_ARM_
    for (unsigned i = 0; i < argSigLen; i++)
    {
        unsigned argumentSize = eeGetArgSize(argLst, &info.compMethodInfo->args);

#ifdef UNIX_AMD64_ABI
        // On the stack frame the homed arg always takes a full number of slots
        // for proper stack alignment. Make sure the real struct size is properly rounded up.
        argumentSize = roundUp(argumentSize, TARGET_POINTER_SIZE);
#endif // UNIX_AMD64_ABI

        argOffs =
            lvaAssignVirtualFrameOffsetToArg(lclNum++, argumentSize, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
        argLst = info.compCompHnd->getArgNext(argLst);
    }
#endif // !_TARGET_ARM_

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

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_L2R)
    {
        argOffs -= argSize;
    }

    unsigned fieldVarNum = BAD_VAR_NUM;

    noway_assert(lclNum < lvaCount);
    LclVarDsc* varDsc = lvaTable + lclNum;

    if (varDsc->lvPromotedStruct())
    {
        noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here
        fieldVarNum = varDsc->lvFieldLclStart;

        lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

        if (promotionType == PROMOTION_TYPE_INDEPENDENT)
        {
            lclNum = fieldVarNum;
            noway_assert(lclNum < lvaCount);
            varDsc = lvaTable + lclNum;
            assert(varDsc->lvIsStructField);
        }
    }

    noway_assert(varDsc->lvIsParam);

    if (varDsc->lvIsRegArg)
    {
        // Argument is passed in a register, don't count it
        // when updating the current offset on the stack.

        if (varDsc->lvOnFrame)
        {
            // The offset for args needs to be set only for the stack homed arguments for System V.
            varDsc->lvStkOffs = argOffs;
        }
        else
        {
            varDsc->lvStkOffs = 0;
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

        varDsc->lvStkOffs = *callerArgOffset;
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

    // For struct promoted parameters we need to set the offsets for both LclVars.
    //
    // For a dependent promoted struct we also assign the struct fields stack offset
    if (varDsc->lvPromotedStruct())
    {
        lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

        if (promotionType == PROMOTION_TYPE_DEPENDENT)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            assert(fieldVarNum == varDsc->lvFieldLclStart);
            lvaTable[fieldVarNum].lvStkOffs = varDsc->lvStkOffs;
        }
    }
    // For an independent promoted struct field we also assign the parent struct stack offset
    else if (varDsc->lvIsStructField)
    {
        noway_assert(varDsc->lvParentLcl < lvaCount);
        lvaTable[varDsc->lvParentLcl].lvStkOffs = varDsc->lvStkOffs;
    }

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L && !varDsc->lvIsRegArg)
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

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_L2R)
    {
        argOffs -= argSize;
    }

    unsigned fieldVarNum = BAD_VAR_NUM;

    noway_assert(lclNum < lvaCount);
    LclVarDsc* varDsc = lvaTable + lclNum;

    if (varDsc->lvPromotedStruct())
    {
        noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here
        fieldVarNum = varDsc->lvFieldLclStart;

        lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

        if (promotionType == PROMOTION_TYPE_INDEPENDENT)
        {
            lclNum = fieldVarNum;
            noway_assert(lclNum < lvaCount);
            varDsc = lvaTable + lclNum;
            assert(varDsc->lvIsStructField);
        }
    }

    noway_assert(varDsc->lvIsParam);

    if (varDsc->lvIsRegArg)
    {
        /* Argument is passed in a register, don't count it
         * when updating the current offset on the stack */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(_TARGET_ARMARCH_)
#if DEBUG
        // TODO: Remove this noway_assert and replace occurrences of TARGET_POINTER_SIZE with argSize
        // Also investigate why we are incrementing argOffs for X86 as this seems incorrect
        //
        noway_assert(argSize == TARGET_POINTER_SIZE);
#endif // DEBUG
#endif

#if defined(_TARGET_X86_)
        argOffs += TARGET_POINTER_SIZE;
#elif defined(_TARGET_AMD64_)
        // Register arguments on AMD64 also takes stack space. (in the backing store)
        varDsc->lvStkOffs = argOffs;
        argOffs += TARGET_POINTER_SIZE;
#elif defined(_TARGET_ARM64_)
// Register arguments on ARM64 only take stack space when they have a frame home.
// Unless on windows and in a vararg method.
#if FEATURE_ARG_SPLIT
        if (this->info.compIsVarArgs)
        {
            if (varDsc->lvType == TYP_STRUCT && varDsc->lvOtherArgReg >= MAX_REG_ARG && varDsc->lvOtherArgReg != REG_NA)
            {
                // This is a split struct. It will account for an extra (8 bytes)
                // of alignment.
                varDsc->lvStkOffs += TARGET_POINTER_SIZE;
                argOffs += TARGET_POINTER_SIZE;
            }
        }
#endif // FEATURE_ARG_SPLIT

#elif defined(_TARGET_ARM_)
        // On ARM we spill the registers in codeGen->regSet.rsMaskPreSpillRegArg
        // in the prolog, so we have to fill in lvStkOffs here
        //
        regMaskTP regMask = genRegMask(varDsc->lvArgReg);
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
                    __fallthrough;

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
            varDsc->lvStkOffs = argOffs;
            argOffs += argSize;
        }
#else // _TARGET_*
#error Unsupported or unset target architecture
#endif // _TARGET_*
    }
    else
    {
#if defined(_TARGET_ARM_)
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

                __fallthrough;

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
#endif // _TARGET_ARM_

        varDsc->lvStkOffs = argOffs;
    }

    // For struct promoted parameters we need to set the offsets for both LclVars.
    //
    // For a dependent promoted struct we also assign the struct fields stack offset
    CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(_TARGET_64BIT_)
    if ((varDsc->TypeGet() == TYP_LONG) && varDsc->lvPromoted)
    {
        noway_assert(varDsc->lvFieldCnt == 2);
        fieldVarNum                         = varDsc->lvFieldLclStart;
        lvaTable[fieldVarNum].lvStkOffs     = varDsc->lvStkOffs;
        lvaTable[fieldVarNum + 1].lvStkOffs = varDsc->lvStkOffs + genTypeSize(TYP_INT);
    }
    else
#endif // !defined(_TARGET_64BIT_)
        if (varDsc->lvPromotedStruct())
    {
        lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

        if (promotionType == PROMOTION_TYPE_DEPENDENT)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            assert(fieldVarNum == varDsc->lvFieldLclStart);
            lvaTable[fieldVarNum].lvStkOffs = varDsc->lvStkOffs;
        }
    }
    // For an independent promoted struct field we also assign the parent struct stack offset
    else if (varDsc->lvIsStructField)
    {
        noway_assert(varDsc->lvParentLcl < lvaCount);
        lvaTable[varDsc->lvParentLcl].lvStkOffs = varDsc->lvStkOffs;
    }

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L && !varDsc->lvIsRegArg)
    {
        argOffs += argSize;
    }

    return argOffs;
}
#endif // !UNIX_AMD64_ABI

/*****************************************************************************
 *  lvaAssignVirtualFrameOffsetsToLocals() : Assign virtual stack offsets to
 *  locals, temps, and anything else.  These will all be negative offsets
 *  (stack grows down) relative to the virtual '0'/return address
 */
void Compiler::lvaAssignVirtualFrameOffsetsToLocals()
{
    int stkOffs = 0;
    // codeGen->isFramePointerUsed is set in regalloc phase. Initialize it to a guess for pre-regalloc layout.
    if (lvaDoneFrameLayout <= PRE_REGALLOC_FRAME_LAYOUT)
    {
        codeGen->setFramePointerUsed(codeGen->isFramePointerRequired());
    }

#ifdef _TARGET_ARM64_
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
#endif // _TARGET_ARM64_

#ifdef _TARGET_XARCH_
    // On x86/amd64, the return address has already been pushed by the call instruction in the caller.
    stkOffs -= TARGET_POINTER_SIZE; // return address;

    // TODO-AMD64-CQ: for X64 eventually this should be pushed with all the other
    // calleeregs.  When you fix this, you'll also need to fix
    // the assert at the bottom of this method
    if (codeGen->doubleAlignOrFramePointerUsed())
    {
        stkOffs -= REGSIZE_BYTES;
    }
#endif //_TARGET_XARCH_

    int  preSpillSize    = 0;
    bool mustDoubleAlign = false;

#ifdef _TARGET_ARM_
    mustDoubleAlign = true;
    preSpillSize    = genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
#else // !_TARGET_ARM_
#if DOUBLE_ALIGN
    if (genDoubleAlign())
    {
        mustDoubleAlign = true; // X86 only
    }
#endif
#endif // !_TARGET_ARM_

#ifdef _TARGET_ARM64_
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

#else  // !_TARGET_ARM64_
    stkOffs -= compCalleeRegsPushed * REGSIZE_BYTES;
#endif // !_TARGET_ARM64_

    compLclFrameSize = 0;

#ifdef _TARGET_AMD64_
    // In case of Amd64 compCalleeRegsPushed includes float regs (Xmm6-xmm15) that
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
    unsigned calleeFPRegsSavedSize = genCountBits(compCalleeFPRegsSavedMask) * XMM_REGSIZE_BYTES;
    if (calleeFPRegsSavedSize > 0 && ((stkOffs % XMM_REGSIZE_BYTES) != 0))
    {
        // Take care of alignment
        int alignPad = (int)AlignmentPad((unsigned)-stkOffs, XMM_REGSIZE_BYTES);
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
#endif //_TARGET_AMD64_

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARMARCH_)
    if (lvaPSPSym != BAD_VAR_NUM)
    {
        // On ARM/ARM64, if we need a PSPSym, allocate it first, before anything else, including
        // padding (so we can avoid computing the same padding in the funclet
        // frame). Note that there is no special padding requirement for the PSPSym.
        noway_assert(codeGen->isFramePointerUsed()); // We need an explicit frame pointer
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaPSPSym, TARGET_POINTER_SIZE, stkOffs);
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARMARCH_)

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
        // This var must go first, in what is called the 'frame header' for EnC so that it is
        // preserved when remapping occurs.  See vm\eetwain.cpp for detailed comment specifying frame
        // layout requirements for EnC to work.
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaMonAcquired, lvaLclSize(lvaMonAcquired), stkOffs);
    }

    if (opts.compNeedSecurityCheck)
    {
#ifdef JIT32_GCENCODER
        /* This can't work without an explicit frame, so make sure */
        noway_assert(codeGen->isFramePointerUsed());
#endif
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaSecurityObject, TARGET_POINTER_SIZE, stkOffs);
    }

#ifdef JIT32_GCENCODER
    if (lvaLocAllocSPvar != BAD_VAR_NUM)
    {
        noway_assert(codeGen->isFramePointerUsed()); // else offsets of locals of frameless methods will be incorrect
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaLocAllocSPvar, TARGET_POINTER_SIZE, stkOffs);
    }
#endif // JIT32_GCENCODER

    if (lvaReportParamTypeArg())
    {
#ifdef JIT32_GCENCODER
        noway_assert(codeGen->isFramePointerUsed());
#endif
        // For CORINFO_CALLCONV_PARAMTYPE (if needed)
        lvaIncrementFrameSize(TARGET_POINTER_SIZE);
        stkOffs -= TARGET_POINTER_SIZE;
        lvaCachedGenericContextArgOffs = stkOffs;
    }
#ifndef JIT32_GCENCODER
    else if (lvaKeepAliveAndReportThis())
    {
        // When "this" is also used as generic context arg.
        lvaIncrementFrameSize(TARGET_POINTER_SIZE);
        stkOffs -= TARGET_POINTER_SIZE;
        lvaCachedGenericContextArgOffs = stkOffs;
    }
#endif

#if !FEATURE_EH_FUNCLETS
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
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaGSSecurityCookie, lvaLclSize(lvaGSSecurityCookie), stkOffs);
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

    noway_assert(cur < _countof(alloc_order));

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
                ((varDsc->TypeGet() != TYP_LONG) || (varDsc->lvOtherReg != REG_STK)))
            {
                allocateOnFrame = false;
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
                continue; // This is allocated outside of this loop.
            }

            // These need to be located as the very first variables (highest memory address)
            // and so they have already been assigned an offset
            if (
#if FEATURE_EH_FUNCLETS
                lclNum == lvaPSPSym ||
#else
                lclNum == lvaShadowSPslotsVar ||
#endif // FEATURE_EH_FUNCLETS
#ifdef JIT32_GCENCODER
                lclNum == lvaLocAllocSPvar ||
#endif // JIT32_GCENCODER
                lclNum == lvaSecurityObject)
            {
                assert(varDsc->lvStkOffs != BAD_STK_OFFS);
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
#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)

                // On Windows AMD64 we can use the caller-reserved stack area that is already setup
                assert(varDsc->lvStkOffs != BAD_STK_OFFS);
                continue;

#else // !_TARGET_AMD64_

                //  A register argument that is not enregistered ends up as
                //  a local variable which will need stack frame space.
                //
                if (!varDsc->lvIsRegArg)
                {
                    continue;
                }

#ifdef _TARGET_ARM64_
                if (info.compIsVarArgs && varDsc->lvArgReg != theFixedRetBuffArgNum())
                {
                    // Stack offset to varargs (parameters) should point to home area which will be preallocated.
                    varDsc->lvStkOffs =
                        -initialStkOffs + genMapIntRegNumToRegArgNum(varDsc->GetArgReg()) * REGSIZE_BYTES;
                    continue;
                }

#endif

#ifdef _TARGET_ARM_
                // On ARM we spill the registers in codeGen->regSet.rsMaskPreSpillRegArg
                // in the prolog, thus they don't need stack frame space.
                //
                if ((codeGen->regSet.rsMaskPreSpillRegs(false) & genRegMask(varDsc->lvArgReg)) != 0)
                {
                    assert(varDsc->lvStkOffs != BAD_STK_OFFS);
                    continue;
                }
#endif

#endif // !_TARGET_AMD64_
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
#ifdef _TARGET_ARM_
                                    || varDsc->lvType == TYP_LONG // Align longs for ARM
#endif
#ifndef _TARGET_64BIT_
                                    || varDsc->lvStructDoubleAlign // Align when lvStructDoubleAlign is true
#endif                                                             // !_TARGET_64BIT_
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
#ifdef _TARGET_ARM64_
            // If we have an incoming register argument that has a struct promoted field
            // then we need to copy the lvStkOff (the stack home) from the reg arg to the field lclvar
            //
            if (varDsc->lvIsRegArg && varDsc->lvPromotedStruct())
            {
                noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

                unsigned fieldVarNum            = varDsc->lvFieldLclStart;
                lvaTable[fieldVarNum].lvStkOffs = varDsc->lvStkOffs;
            }
#endif // _TARGET_ARM64_
#ifdef _TARGET_ARM_
            // If we have an incoming register argument that has a promoted long
            // then we need to copy the lvStkOff (the stack home) from the reg arg to the field lclvar
            //
            if (varDsc->lvIsRegArg && varDsc->lvPromoted)
            {
                assert(varTypeIsLong(varDsc) && (varDsc->lvFieldCnt == 2));

                unsigned fieldVarNum                = varDsc->lvFieldLclStart;
                lvaTable[fieldVarNum].lvStkOffs     = varDsc->lvStkOffs;
                lvaTable[fieldVarNum + 1].lvStkOffs = varDsc->lvStkOffs + 4;
            }
#endif // _TARGET_ARM_
        }
    }

    if (getNeedsGSSecurityCookie() && !compGSReorderStackLayout)
    {
        // LOCALLOC used, but we have no unsafe buffer.  Allocated cookie last, close to localloc buffer.
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaGSSecurityCookie, lvaLclSize(lvaGSSecurityCookie), stkOffs);
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

#if FEATURE_EH_FUNCLETS && defined(_TARGET_AMD64_)
    if (lvaPSPSym != BAD_VAR_NUM)
    {
        // On AMD64, if we need a PSPSym, allocate it last, immediately above the outgoing argument
        // space. Any padding will be higher on the stack than this
        // (including the padding added by lvaAlignFrame()).
        noway_assert(codeGen->isFramePointerUsed()); // We need an explicit frame pointer
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaPSPSym, TARGET_POINTER_SIZE, stkOffs);
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_AMD64_)

#ifdef _TARGET_ARM64_
    if (!codeGen->IsSaveFpLrWithAllCalleeSavedRegisters() &&
        isFramePointerUsed()) // Note that currently we always have a frame pointer
    {
        // Create space for saving FP and LR.
        stkOffs -= 2 * REGSIZE_BYTES;
    }
#endif // _TARGET_ARM64_

#if FEATURE_FIXED_OUT_ARGS
    if (lvaOutgoingArgSpaceSize > 0)
    {
#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI) // No 4 slots for outgoing params on System V.
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

#ifdef _TARGET_ARM64_
    if (info.compIsVarArgs)
    {
        pushedCount += MAX_REG_ARG;
    }
#endif

#ifdef _TARGET_XARCH_
    if (codeGen->doubleAlignOrFramePointerUsed())
    {
        pushedCount += 1; // pushed EBP (frame pointer)
    }
    pushedCount += 1; // pushed PC (return address)
#endif

    noway_assert(compLclFrameSize == (unsigned)-(stkOffs + (pushedCount * (int)TARGET_POINTER_SIZE)));
}

int Compiler::lvaAllocLocalAndSetVirtualOffset(unsigned lclNum, unsigned size, int stkOffs)
{
    noway_assert(lclNum != BAD_VAR_NUM);

#ifdef _TARGET_64BIT_
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
#endif // _TARGET_64BIT_

    /* Reserve space on the stack by bumping the frame size */

    lvaIncrementFrameSize(size);
    stkOffs -= size;
    lvaTable[lclNum].lvStkOffs = stkOffs;

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

#ifdef _TARGET_AMD64_
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
#endif //_TARGET_AMD64_

/*****************************************************************************
 *  lvaAlignFrame() :  After allocating everything on the frame, reserve any
 *  extra space needed to keep the frame aligned
 */
void Compiler::lvaAlignFrame()
{
#if defined(_TARGET_AMD64_)

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

#elif defined(_TARGET_ARM64_)

    // The stack on ARM64 must be 16 byte aligned.

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

#elif defined(_TARGET_ARM_)

    // Ensure that stack offsets will be double-aligned by grabbing an unused DWORD if needed.
    //
    bool lclFrameSizeAligned   = (compLclFrameSize % sizeof(double)) == 0;
    bool regPushedCountAligned = ((compCalleeRegsPushed + genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true))) %
                                  (sizeof(double) / TARGET_POINTER_SIZE)) == 0;

    if (regPushedCountAligned != lclFrameSizeAligned)
    {
        lvaIncrementFrameSize(TARGET_POINTER_SIZE);
    }

#elif defined(_TARGET_X86_)

#if DOUBLE_ALIGN
    if (genDoubleAlign())
    {
        // Double Frame Alignement for x86 is handled in Compiler::lvaAssignVirtualFrameOffsetsToLocals()

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
#endif // !_TARGET_AMD64_
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
        if (varDsc->lvIsStructField
#ifndef UNIX_AMD64_ABI
#if !defined(_TARGET_ARM_)
            // ARM: lo/hi parts of a promoted long arg need to be updated.

            // For System V platforms there is no outgoing args space.
            // A register passed struct arg is homed on the stack in a separate local var.
            // The offset of these structs is already calculated in lvaAssignVirtualFrameOffsetToArg methos.
            // Make sure the code below is not executed for these structs and the offset is not changed.
            && !varDsc->lvIsParam
#endif // !defined(_TARGET_ARM_)
#endif // !UNIX_AMD64_ABI
            )
        {
            LclVarDsc*       parentvarDsc  = &lvaTable[varDsc->lvParentLcl];
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
                    varDsc->lvStkOffs = parentvarDsc->lvStkOffs + varDsc->lvFldOffset;
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
#ifdef _TARGET_ARM_
        preSpillSize = genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true)) * TARGET_POINTER_SIZE;
#endif
        bool assignDone;
        bool assignNptr;
        bool assignPtrs = true;

        /* Allocate temps */

        if (TRACK_GC_TEMP_LIFETIMES)
        {
            /* first pointers, then non-pointers in second pass */
            assignNptr = false;
            assignDone = false;
        }
        else
        {
            /* Pointers and non-pointers together in single pass */
            assignNptr = true;
            assignDone = true;
        }

        assert(codeGen->regSet.tmpAllFree());

    AGAIN2:

        for (TempDsc* temp = codeGen->regSet.tmpListBeg(); temp != nullptr; temp = codeGen->regSet.tmpListNxt(temp))
        {
            var_types tempType = temp->tdTempType();
            unsigned  size;

            /* Make sure the type is appropriate */

            if (!assignPtrs && varTypeIsGC(tempType))
            {
                continue;
            }
            if (!assignNptr && !varTypeIsGC(tempType))
            {
                continue;
            }

            size = temp->tdTempSize();

            /* Figure out and record the stack offset of the temp */

            /* Need to align the offset? */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_
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
#ifdef _TARGET_ARM_
        // Only required for the ARM platform that we have an accurate estimate for the spillTempSize
        noway_assert(spillTempSize <= lvaGetMaxSpillTempSize());
#endif

        /* If we've only assigned some temps, go back and do the rest now */

        if (!assignDone)
        {
            assignNptr = !assignNptr;
            assignPtrs = !assignPtrs;
            assignDone = true;

            goto AGAIN2;
        }
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
    LclVarDsc* varDsc = lvaTable + lclNum;

#ifdef _TARGET_ARM_
    if (varDsc->TypeGet() == TYP_DOUBLE)
    {
        // The assigned registers are `lvRegNum:RegNext(lvRegNum)`
        printf("%3s:%-3s    ", getRegName(varDsc->lvRegNum), getRegName(REG_NEXT(varDsc->lvRegNum)));
    }
    else
#endif // _TARGET_ARM_
    {
        printf("%3s        ", getRegName(varDsc->lvRegNum));
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

#ifdef _TARGET_ARM_
    offset = lvaFrameAddress(lclNum, compLocallocUsed, &baseReg, 0, /* isFloatUsage */ false);
#else
    bool EBPbased;
    offset  = lvaFrameAddress(lclNum, &EBPbased);
    baseReg = EBPbased ? REG_FPBASE : REG_SPBASE;
#endif

    printf("[%2s%1s0x%02X]  ", getRegName(baseReg), (offset < 0 ? "-" : "+"), (offset < 0 ? -offset : offset));
}

/*****************************************************************************
 *
 *  dump a single lvaTable entry
 */

void Compiler::lvaDumpEntry(unsigned lclNum, FrameLayoutState curState, size_t refCntWtdWidth)
{
    LclVarDsc* varDsc = lvaTable + lclNum;
    var_types  type   = varDsc->TypeGet();

    if (curState == INITIAL_FRAME_LAYOUT)
    {
        printf(";  ");
        gtDispLclVar(lclNum);

        printf(" %7s ", varTypeName(type));
        if (genTypeSize(type) == 0)
        {
#if FEATURE_FIXED_OUT_ARGS
            if (lclNum == lvaOutgoingArgSpaceVar)
            {
                // Since lvaOutgoingArgSpaceSize is a PhasedVar we can't read it for Dumping until
                // after we set it to something.
                if (lvaOutgoingArgSpaceSize.HasFinalValue())
                {
                    // A PhasedVar<T> can't be directly used as an arg to a variadic function
                    unsigned value = lvaOutgoingArgSpaceSize;
                    printf("(%2d) ", value);
                }
                else
                {
                    printf("(na) "); // The value hasn't yet been determined
                }
            }
            else
#endif // FEATURE_FIXED_OUT_ARGS
            {
                printf("(%2d) ", lvaLclSize(lclNum));
            }
        }
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
        if (varDsc->lvRefCnt() == 0)
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
        if (varDsc->lvAddrExposed)
        {
            printf("X");
        }
        if (varTypeIsStruct(varDsc))
        {
            printf("S");
        }
        if (varDsc->lvVMNeedsStackAddr)
        {
            printf("V");
        }
        if (varDsc->lvLiveInOutOfHndlr)
        {
            printf("H");
        }
        if (varDsc->lvLclFieldExpr)
        {
            printf("F");
        }
        if (varDsc->lvLclBlockOpAddr)
        {
            printf("B");
        }
        if (varDsc->lvLiveAcrossUCall)
        {
            printf("U");
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
    if (varDsc->lvAddrExposed)
    {
        printf(" addr-exposed");
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
    if (varDsc->lvClassHnd != nullptr)
    {
        printf(" class-hnd");
    }
    if (varDsc->lvClassIsExact)
    {
        printf(" exact");
    }
#ifndef _TARGET_64BIT_
    if (varDsc->lvStructDoubleAlign)
        printf(" double-align");
#endif // !_TARGET_64BIT_
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
        LclVarDsc* parentvarDsc = &lvaTable[varDsc->lvParentLcl];
#if !defined(_TARGET_64BIT_)
        if (varTypeIsLong(parentvarDsc))
        {
            bool isLo = (lclNum == parentvarDsc->lvFieldLclStart);
            printf(" V%02u.%s(offs=0x%02x)", varDsc->lvParentLcl, isLo ? "lo" : "hi", isLo ? 0 : genTypeSize(TYP_INT));
        }
        else
#endif // !defined(_TARGET_64BIT_)
        {
            CORINFO_CLASS_HANDLE typeHnd = parentvarDsc->lvVerTypeInfo.GetClassHandle();
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

#if defined(_TARGET_ARMARCH_)
    if (compFloatingPointUsed)
        compCalleeRegsPushed += CNT_CALLEE_SAVED_FLOAT;

    compCalleeRegsPushed++; // we always push LR.  See genPushCalleeSavedRegisters
#elif defined(_TARGET_AMD64_)
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

#ifdef _TARGET_XARCH_
    // Since FP/EBP is included in the SAVED_REG_MAXSZ we need to
    // subtract 1 register if codeGen->isFramePointerUsed() is true.
    if (codeGen->isFramePointerUsed())
    {
        compCalleeRegsPushed--;
    }
#endif

    lvaAssignFrameOffsets(curState);

    unsigned calleeSavedRegMaxSz = CALLEE_SAVED_REG_MAXSZ;
#if defined(_TARGET_ARMARCH_)
    if (compFloatingPointUsed)
    {
        calleeSavedRegMaxSz += CALLEE_SAVED_FLOAT_MAXSZ;
    }
    calleeSavedRegMaxSz += REGSIZE_BYTES; // we always push LR.  See genPushCalleeSavedRegisters
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
    assert(varNum < lvaCount);
    const LclVarDsc* varDsc = lvaTable + varNum;
    assert(varDsc->lvOnFrame);
    int spRelativeOffset;

    if (varDsc->lvFramePointerBased)
    {
        // The stack offset is relative to the frame pointer, so convert it to be
        // relative to the stack pointer (which makes no sense for localloc functions).
        spRelativeOffset = varDsc->lvStkOffs + codeGen->genSPtoFPdelta();
    }
    else
    {
        spRelativeOffset = varDsc->lvStkOffs;
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
    assert(varNum < lvaCount);
    LclVarDsc* varDsc = lvaTable + varNum;
    assert(varDsc->lvOnFrame);

    return lvaToCallerSPRelativeOffset(varDsc->lvStkOffs, varDsc->lvFramePointerBased);
}

int Compiler::lvaToCallerSPRelativeOffset(int offset, bool isFpBased) const
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
    assert(varNum < lvaCount);
    LclVarDsc* varDsc = lvaTable + varNum;
    assert(varDsc->lvOnFrame);

    return lvaToInitialSPRelativeOffset(varDsc->lvStkOffs, varDsc->lvFramePointerBased);
}

// Given a local variable offset, and whether that offset is frame-pointer based, return its offset from Initial-SP.
// This is used, for example, to figure out the offset of the frame pointer from Initial-SP.
int Compiler::lvaToInitialSPRelativeOffset(unsigned offset, bool isFpBased)
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);
#ifdef _TARGET_AMD64_
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
#else  // !_TARGET_AMD64_
    NYI("lvaToInitialSPRelativeOffset");
#endif // !_TARGET_AMD64_

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
            lcl = tree;
            break;

        case GT_ADDR:
            if (tree->gtOp.gtOp1->gtOper != GT_LCL_VAR)
            {
                return WALK_CONTINUE;
            }
            lcl = tree->gtOp.gtOp1;
            break;

        default:
            return WALK_CONTINUE;
    }

    Compiler* pComp      = ((lvaStressLclFldArgs*)data->pCallbackData)->m_pCompiler;
    bool      bFirstPass = ((lvaStressLclFldArgs*)data->pCallbackData)->m_bFirstPass;
    noway_assert(lcl->gtOper == GT_LCL_VAR);
    unsigned   lclNum = lcl->gtLclVarCommon.gtLclNum;
    var_types  type   = lcl->TypeGet();
    LclVarDsc* varDsc = &pComp->lvaTable[lclNum];

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
        noway_assert(varDsc->lvType == lcl->gtType || varDsc->lvType == TYP_BLK);
        var_types varType = varDsc->TypeGet();

        // Calculate padding
        unsigned padding = LCL_FLD_PADDING(lclNum);

#ifdef _TARGET_ARMARCH_
        // We need to support alignment requirements to access memory on ARM ARCH
        unsigned alignment = 1;
        pComp->codeGen->InferOpSizeAlign(lcl, &alignment);
        alignment = roundUp(alignment, TARGET_POINTER_SIZE);
        padding   = roundUp(padding, alignment);
#endif // _TARGET_ARMARCH_

        // Change the variable to a TYP_BLK
        if (varType != TYP_BLK)
        {
            varDsc->lvExactSize = roundUp(padding + pComp->lvaLclSize(lclNum), TARGET_POINTER_SIZE);
            varDsc->lvType      = TYP_BLK;
            pComp->lvaSetVarAddrExposed(lclNum);
        }

        tree->gtFlags |= GTF_GLOB_REF;

        /* Now morph the tree appropriately */
        if (oper == GT_LCL_VAR)
        {
            /* Change lclVar(lclNum) to lclFld(lclNum,padding) */

            tree->ChangeOper(GT_LCL_FLD);
            tree->gtLclFld.gtLclOffs = padding;
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
