//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
unsigned            Compiler::s_lvaDoubleAlignedProcsCount = 0;
#endif
#endif

/*****************************************************************************/

void                Compiler::lvaInit()
{
    /* We haven't allocated stack variables yet */
    lvaRefCountingStarted = false;
    lvaLocalVarRefCounted = false;

    lvaSortAgain          = false;  // false: We don't need to call lvaSortOnly()
    lvaTrackedFixed       = false;  // false: We can still add new tracked variables

    lvaDoneFrameLayout = NO_FRAME_LAYOUT;
#if !FEATURE_EH_FUNCLETS
    lvaShadowSPslotsVar = BAD_VAR_NUM;
#endif // !FEATURE_EH_FUNCLETS
    lvaInlinedPInvokeFrameVar = BAD_VAR_NUM;
#if FEATURE_FIXED_OUT_ARGS
#if INLINE_NDIRECT
    lvaPInvokeFrameRegSaveVar = BAD_VAR_NUM;
#endif // !INLINE_NDIRECT
    lvaOutgoingArgSpaceVar = BAD_VAR_NUM;
#endif // FEATURE_FIXED_OUT_ARGS
#ifdef _TARGET_ARM_
    lvaPromotedStructAssemblyScratchVar = BAD_VAR_NUM;
#endif // _TARGET_ARM_
    lvaLocAllocSPvar = BAD_VAR_NUM;
    lvaGSSecurityCookie  = BAD_VAR_NUM;
#ifdef _TARGET_X86_
    lvaVarargsBaseOfStkArgs = BAD_VAR_NUM;
#endif // _TARGET_X86_
    lvaVarargsHandleArg = BAD_VAR_NUM;
    lvaSecurityObject = BAD_VAR_NUM;
    lvaStubArgumentVar = BAD_VAR_NUM;
    lvaArg0Var = BAD_VAR_NUM;
    lvaMonAcquired = BAD_VAR_NUM;
    
    lvaInlineeReturnSpillTemp = BAD_VAR_NUM;

    gsShadowVarInfo = NULL;
#if FEATURE_EH_FUNCLETS
    lvaPSPSym = BAD_VAR_NUM;
#endif
#if FEATURE_SIMD
    lvaSIMDInitTempVarNum = BAD_VAR_NUM;
#endif // FEATURE_SIMD
    lvaCurEpoch = 0;
}

/*****************************************************************************/

void                Compiler::lvaInitTypeRef()
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

    info.compArgsCount      = info.compMethodInfo->args.numArgs;
    
    // Is there a 'this' pointer 

    if (!info.compIsStatic)
    {
        info.compArgsCount++;
    }
    else
    {
        info.compThisArg = BAD_VAR_NUM;
    }

    info.compILargsCount    = info.compArgsCount;

#ifdef FEATURE_SIMD
    if (featureSIMD && (info.compRetNativeType == TYP_STRUCT))
    {
        var_types structType = impNormStructType(info.compMethodInfo->args.retTypeClass);
        info.compRetType = structType;
    }
#endif // FEATURE_SIMD

    // Are we returning a struct by value? 
    
    const bool hasRetBuffArg = impMethodInfo_hasRetBuffArg(info.compMethodInfo);

    // Change the compRetNativeType if we are returning a struct by value in a register
    if (!hasRetBuffArg && varTypeIsStruct(info.compRetNativeType))
    {
#ifdef _TARGET_ARM_
        // TODO-ARM64-NYI: HFA
        if (!info.compIsVarArgs && IsHfa(info.compMethodInfo->args.retTypeClass))
        {
            info.compRetNativeType = TYP_STRUCT;
        }
        else
#endif
        {
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            eeGetSystemVAmd64PassStructInRegisterDescriptor(info.compMethodInfo->args.retTypeClass, &structDesc);
            if (structDesc.eightByteCount > 1)
            {
                info.compRetNativeType = TYP_STRUCT;
            }
            else
            {
                info.compRetNativeType = getEightByteType(structDesc, 0);
            }
#else // !FEATURE_UNIX_AMD64_STRUCT_PASSING
            // Check for TYP_STRUCT argument that can fit into a single register
            var_types argRetType = argOrReturnTypeForStruct(info.compMethodInfo->args.retTypeClass, true /* forReturn */);
            info.compRetNativeType = argRetType;
            if (argRetType == TYP_UNKNOWN)
            {
                assert(!"Unexpected size when returning struct by value");
            }
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING
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
        info.compTypeCtxtArg =  BAD_VAR_NUM;
    }

    lvaCount                =
    info.compLocalsCount    = info.compArgsCount +
                              info.compMethodInfo->locals.numArgs;

    info.compILlocalsCount  = info.compILargsCount +
                              info.compMethodInfo->locals.numArgs;

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
        lvaTableCnt = 16;

    lvaTable = (LclVarDsc*)compGetMemArray(lvaTableCnt, sizeof(*lvaTable), CMK_LvaTable);
    size_t tableSize = lvaTableCnt * sizeof(*lvaTable);
    memset(lvaTable, 0, tableSize);
    for (unsigned i = 0; i < lvaTableCnt; i++)
    {
        new (&lvaTable[i], jitstd::placement_t()) LclVarDsc(this); // call the constructor.
    }

    //-------------------------------------------------------------------------
    // Count the arguments and initialize the respective lvaTable[] entries
    //
    // First the implicit arguments
    //-------------------------------------------------------------------------

    InitVarDscInfo varDscInfo;
    varDscInfo.Init(lvaTable,  hasRetBuffArg);

    lvaInitArgs(&varDscInfo);

    //-------------------------------------------------------------------------
    // Finally the local variables
    //-------------------------------------------------------------------------
    
    unsigned    varNum = varDscInfo.varNum;
    LclVarDsc * varDsc = varDscInfo.varDsc;
    CORINFO_ARG_LIST_HANDLE     localsSig = info.compMethodInfo->locals.args;

    for (unsigned i = 0;
        i < info.compMethodInfo->locals.numArgs; 
        i++, varNum++, varDsc++, localsSig = info.compCompHnd->getArgNext(localsSig))
    {
        CORINFO_CLASS_HANDLE typeHnd;
        CorInfoTypeWithMod corInfoType = info.compCompHnd->getArgType(
                                                &info.compMethodInfo->locals,
                                                localsSig,
                                                &typeHnd);
        lvaInitVarDsc(varDsc,
                      varNum,
                      strip(corInfoType),
                      typeHnd,
                      localsSig,
                      &info.compMethodInfo->locals);

        varDsc->lvPinned = ((corInfoType & CORINFO_TYPE_MOD_PINNED) != 0);
        varDsc->lvOnFrame = true;   // The final home for this local variable might be our local stack frame
    }

    if (// If there already exist unsafe buffers, don't mark more structs as unsafe 
        // as that will cause them to be placed along with the real unsafe buffers, 
        // unnecessarily exposing them to overruns. This can affect GS tests which 
        // intentionally do buffer-overruns.
        !getNeedsGSSecurityCookie() &&
        // GS checks require the stack to be re-ordered, which can't be done with EnC
        !opts.compDbgEnC &&
        compStressCompile(STRESS_UNSAFE_BUFFER_CHECKS, 25))
    {
        setNeedsGSSecurityCookie();
        compGSReorderStackLayout = true;

        for (unsigned i = 0; i < lvaCount; i++)
        {
            if ((lvaTable[i].lvType == TYP_STRUCT) && compStressCompile(STRESS_GENERIC_VARN, 60))
                lvaTable[i].lvIsUnsafeBuffer = true;
        }            
    }

    if (getNeedsGSSecurityCookie())
    {
        // Ensure that there will be at least one stack variable since
        // we require that the GSCookie does not have a 0 stack offset.
        unsigned dummy = lvaGrabTempWithImplicitUse(false DEBUGARG("GSCookie dummy"));
        lvaTable[dummy].lvType = TYP_INT;
    }

#ifdef DEBUG
    if (verbose)
        lvaTableDump(INITIAL_FRAME_LAYOUT);
#endif
}

/*****************************************************************************/
void                Compiler::lvaInitArgs(InitVarDscInfo *          varDscInfo)
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
    assert (varDscInfo->intRegArgNum <= MAX_REG_ARG);

    codeGen->intRegState.rsCalleeRegArgNum = varDscInfo->intRegArgNum;

#if !FEATURE_STACK_FP_X87
    codeGen->floatRegState.rsCalleeRegArgNum = varDscInfo->floatRegArgNum;
#endif // FEATURE_STACK_FP_X87

    // The total argument size must be aligned.
    noway_assert((compArgSize % sizeof(void*)) == 0);

#ifdef _TARGET_X86_
    /* We can not pass more than 2^16 dwords as arguments as the "ret"
       instruction can only pop 2^16 arguments. Could be handled correctly
       but it will be very difficult for fully interruptible code */

    if (compArgSize != (size_t)(unsigned short)compArgSize)
        NO_WAY("Too many arguments for the \"ret\" instruction to pop");
#endif
}

/*****************************************************************************/
void                Compiler::lvaInitThisPtr(InitVarDscInfo *       varDscInfo)
{
    LclVarDsc * varDsc = varDscInfo->varDsc;
    if  (!info.compIsStatic)
    {
        varDsc->lvIsParam   = 1;
#if ASSERTION_PROP
        varDsc->lvSingleDef = 1;
#endif

        varDsc->lvIsPtr = 1;

        lvaArg0Var = info.compThisArg = varDscInfo->varNum;
        noway_assert(info.compThisArg == 0);

        if (eeIsValueClass(info.compClassHnd))
        {
            varDsc->lvType = TYP_BYREF;
#ifdef FEATURE_SIMD
            if (featureSIMD)
            {
                var_types simdBaseType = TYP_UNKNOWN;
                var_types type = impNormStructType(info.compClassHnd, nullptr, nullptr, &simdBaseType);
                if (simdBaseType != TYP_UNKNOWN)
                {
                    assert(varTypeIsSIMD(type));
                    varDsc->lvSIMDType = true;
                    varDsc->lvBaseType = simdBaseType;
                }
            }
#endif // FEATURE_SIMD
        }
        else
        {
            varDsc->lvType = TYP_REF;
        }

        if (tiVerificationNeeded) 
        {
            varDsc->lvVerTypeInfo = verMakeTypeInfo(info.compClassHnd);        

            if (varDsc->lvVerTypeInfo.IsValueClass())
                varDsc->lvVerTypeInfo.MakeByRef();
        }
        else
        {
            varDsc->lvVerTypeInfo = typeInfo();
        }

        // Mark the 'this' pointer for the method
        varDsc->lvVerTypeInfo.SetIsThisPtr();

        varDsc->lvIsRegArg = 1;
        noway_assert(varDscInfo->intRegArgNum == 0);

        varDsc->lvArgReg  = genMapRegArgNumToRegNum(varDscInfo->allocRegArg(TYP_INT), varDsc->TypeGet());
        varDsc->setPrefReg(varDsc->lvArgReg, this);
        varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

#ifdef  DEBUG
        if  (verbose)
        {
            printf("'this'    passed in register %s\n", getRegName(varDsc->lvArgReg));
        }
#endif
        compArgSize       += TARGET_POINTER_SIZE;

        varDscInfo->varNum++;
        varDscInfo->varDsc++;
    }
}

/*****************************************************************************/
void                Compiler::lvaInitRetBuffArg(InitVarDscInfo *    varDscInfo)
{
    LclVarDsc * varDsc = varDscInfo->varDsc;
    bool hasRetBuffArg = impMethodInfo_hasRetBuffArg(info.compMethodInfo);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    if (varTypeIsStruct(info.compRetNativeType))
    {
        if (IsRegisterPassable(info.compMethodInfo->args.retTypeClass))
        {
            hasRetBuffArg = false;
        }
    }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    if (hasRetBuffArg)
    {
        info.compRetBuffArg = varDscInfo->varNum;
        varDsc->lvType      = TYP_BYREF;
        varDsc->lvIsParam   = 1;
        varDsc->lvIsRegArg  = 1;
#if ASSERTION_PROP
        varDsc->lvSingleDef = 1;
#endif
        varDsc->lvArgReg  = genMapRegArgNumToRegNum(varDscInfo->allocRegArg(TYP_INT), varDsc->TypeGet());
        varDsc->setPrefReg(varDsc->lvArgReg, this);
        varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

        info.compRetBuffDefStack = 0;
        if (info.compRetType == TYP_STRUCT)
        {
            CORINFO_SIG_INFO sigInfo;
            info.compCompHnd->getMethodSig(info.compMethodHnd, &sigInfo);
            assert(JITtype2varType(sigInfo.retType) == info.compRetType);  // Else shouldn't have a ret buff.

            info.compRetBuffDefStack = (info.compCompHnd->isStructRequiringStackAllocRetBuf(sigInfo.retTypeClass) == TRUE);
            if (info.compRetBuffDefStack)
            {
                // If we're assured that the ret buff argument points into a callers stack, we will type it as "TYP_I_IMPL"
                // (native int/unmanaged pointer) so that it's not tracked as a GC ref.
                varDsc->lvType = TYP_I_IMPL;
            }
        }

        assert(genMapIntRegNumToRegArgNum(varDsc->lvArgReg) < MAX_REG_ARG);

#ifdef  DEBUG
        if  (verbose)
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
void                Compiler::lvaInitUserArgs(InitVarDscInfo *      varDscInfo)
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

    CORINFO_ARG_LIST_HANDLE argLst  = info.compMethodInfo->args.args;

    const unsigned argSigLen        = info.compMethodInfo->args.numArgs;

    regMaskTP doubleAlignMask = RBM_NONE;
    for (unsigned i = 0;
         i < argSigLen; 
         i++, varDscInfo->varNum++, varDscInfo->varDsc++, argLst = info.compCompHnd->getArgNext(argLst))
    {
        LclVarDsc * varDsc = varDscInfo->varDsc;
        CORINFO_CLASS_HANDLE typeHnd = NULL;

        CorInfoTypeWithMod corInfoType = info.compCompHnd->getArgType(&info.compMethodInfo->args, 
                                                                      argLst,
                                                                      &typeHnd);
        varDsc->lvIsParam = 1;
#if ASSERTION_PROP
        varDsc->lvSingleDef = 1;
#endif

        lvaInitVarDsc(  varDsc,
                        varDscInfo->varNum,
                        strip(corInfoType),
                        typeHnd,
                        argLst,
                        &info.compMethodInfo->args);

        // For ARM, ARM64, and AMD64 varargs, all arguments go in integer registers
        var_types argType = mangleVarArgsType(varDsc->TypeGet());
        unsigned argSize = eeGetArgSize(argLst, &info.compMethodInfo->args);
        unsigned cSlots = argSize / TARGET_POINTER_SIZE;    // the total number of slots of this argument

        // The number of slots that must be enregistered if we are to consider this argument enregistered.
        // This is normally the same as cSlots, since we normally either enregister the entire object,
        // or none of it. For structs on ARM, however, we only need to enregister a single slot to consider
        // it enregistered, as long as we can split the rest onto the stack.
        // TODO-ARM64-NYI: we can enregister a struct <= 16 bytes into two consecutive registers, if there are enough remaining argument registers.
        // TODO-ARM64-NYI: HFA
        unsigned cSlotsToEnregister = cSlots;

#ifdef _TARGET_ARM_

        var_types hfaType = (varTypeIsStruct(argType) ? GetHfaType(typeHnd) : TYP_UNDEF;
        bool isHfaArg = !info.compIsVarArgs && varTypeIsFloating(hfaType);

        // On ARM we pass the first 4 words of integer arguments and non-HFA structs in registers.
        // But we pre-spill user arguments in varargs methods and structs.
        // 
        unsigned cAlign;
        bool  preSpill = info.compIsVarArgs;

        switch (argType)
        {
        case TYP_STRUCT:
            assert(varDsc->lvSize() == argSize);
            cAlign = varDsc->lvStructDoubleAlign ? 2 : 1;

            // HFA arguments go on the stack frame. They don't get spilled in the prolog like struct
            // arguments passed in the integer registers but get homed immediately after the prolog.
            if (!isHfaArg)
            {
                cSlotsToEnregister = 1; // HFAs must be totally enregistered or not, but other structs can be split.
                preSpill = true;
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

        if (isHfaArg)
        {
            // We've got the HFA size and alignment, so from here on out treat
            // the type as a float or double.
            argType = hfaType;
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

            if (varDscInfo->canEnreg(TYP_INT, 1) &&         // The beginning of the struct can go in a register
                !varDscInfo->canEnreg(TYP_INT, cSlots) &&   // The end of the struct can't fit in a register
                varDscInfo->existAnyFloatStackArgs())       // There's at least one stack-based FP arg already
            {
                varDscInfo->setAllRegArgUsed(TYP_INT);  // Prevent all future use of integer registers
                preSpill = false;                       // This struct won't be prespilled, since it will go on the stack
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
        else
        {
            varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame
        }

#else // !_TARGET_ARM_
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
        if (varTypeIsStruct(argType))
        {
            assert(typeHnd != nullptr);
            eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
            if (structDesc.passedInRegisters)
            {
                unsigned intRegCount = 0;
                unsigned floatRegCount = 0;

                for (unsigned int i = 0; i < structDesc.eightByteCount; i++)
                {
                    switch (structDesc.eightByteClassifications[i])
                    {
                    case SystemVClassificationTypeInteger:
                    case SystemVClassificationTypeIntegerReference:
                        intRegCount++;
                        break;
                    case SystemVClassificationTypeSSE:
                        floatRegCount++;
                        break;
                    default:
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
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

        // The final home for this incoming register might be our local stack frame
        // For System V platforms the final home will always be on the local stack frame.
        varDsc->lvOnFrame = true;

#endif // !_TARGET_ARM_

        bool canPassArgInRegisters = false;

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        if (varTypeIsStruct(argType))
        {
            canPassArgInRegisters = structDesc.passedInRegisters;
        }
        else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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

#if FEATURE_MULTIREG_STRUCT_ARGS
            varDsc->lvOtherArgReg = REG_NA;
#endif

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            unsigned secondAllocatedRegArgNum = 0;
            var_types firstEightByteType  = TYP_UNDEF;
            var_types secondEightByteType = TYP_UNDEF;

            if (varTypeIsStruct(argType))
            {
                if (structDesc.eightByteCount >= 1)
                {
                    firstEightByteType = getEightByteType(structDesc, 0);
                    firstAllocatedRegArgNum = varDscInfo->allocRegArg(firstEightByteType, 1);
                }
            }
            else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            {
                firstAllocatedRegArgNum = varDscInfo->allocRegArg(argType, cSlots);
            }

#ifdef _TARGET_ARM_
            if (isHfaArg)
            {
                // We need to save the fact that this HFA is enregistered
                varDsc->lvIsHfaRegArg = true;
                varDsc->SetHfaType(argType);
            }
#endif // _TARGET_ARM_

            varDsc->lvIsRegArg = 1;

#if FEATURE_MULTIREG_STRUCT_ARGS
            if (varTypeIsStruct(argType))
            {
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                varDsc->lvArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum, firstEightByteType);

                // If there is a second eightbyte, get a register for it too and map the arg to the reg number.
                if (structDesc.eightByteCount >= 2)
                {
                    secondEightByteType = getEightByteType(structDesc, 1);
                    secondAllocatedRegArgNum = varDscInfo->allocRegArg(secondEightByteType, 1);
                }

                if (secondEightByteType != TYP_UNDEF)
                {
                    varDsc->lvOtherArgReg = genMapRegArgNumToRegNum(secondAllocatedRegArgNum, secondEightByteType);
                    varDsc->addPrefReg(genRegMask(varDsc->lvOtherArgReg), this);
                }
#else // ARM32 or ARM64
                varDsc->lvArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum, TYP_I_IMPL);
#ifdef _TARGET_ARM64_
                if (cSlots == 2)
                {
                    varDsc->lvOtherArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum+1, TYP_I_IMPL);
                    varDsc->addPrefReg(genRegMask(varDsc->lvOtherArgReg), this);
                }
#endif //  _TARGET_ARM64_
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            }
            else
#endif // FEATURE_MULTIREG_STRUCT_ARGS
            {
                varDsc->lvArgReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum, argType);
            }

            varDsc->setPrefReg(varDsc->lvArgReg, this);

#ifdef _TARGET_ARM_
            if (varDsc->TypeGet() == TYP_LONG)
            {
                varDsc->lvOtherReg = genMapRegArgNumToRegNum(firstAllocatedRegArgNum + 1, TYP_INT);
                varDsc->addPrefReg(genRegMask(varDsc->lvOtherReg), this);
            }
#endif // _TARGET_ARM_

#ifdef  DEBUG
            if  (verbose)
            {
                printf("Arg #%u    passed in register(s) ", varDscInfo->varNum);
                bool isFloat = false;
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                // In case of one eightbyte struct the type is already normalized earlier.
                // The varTypeIsFloating(argType) is good for this case.
                if (varTypeIsStruct(argType) && (structDesc.eightByteCount >= 1))
                {
                    isFloat = varTypeIsFloating(firstEightByteType);
                }
                else
#else // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                {
                    isFloat = varTypeIsFloating(argType);
                }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                if (varTypeIsStruct(argType))
                {
                    // Print both registers, just to be clear
                    if (firstEightByteType == TYP_UNDEF)
                    {
                        printf("firstEightByte: <not used>");
                    }
                    else
                    {
                        printf("firstEightByte: %s", getRegName(genMapRegArgNumToRegNum(firstAllocatedRegArgNum, firstEightByteType), isFloat));
                    }

                    if (secondEightByteType == TYP_UNDEF)
                    {
                        printf(", secondEightByte: <not used>");
                    }
                    else
                    {
                        printf(", secondEightByte: %s", getRegName(genMapRegArgNumToRegNum(secondAllocatedRegArgNum, secondEightByteType), varTypeIsFloating(secondEightByteType)));
                    }
                }
                else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                {
                    unsigned regArgNum = genMapRegNumToRegArgNum(varDsc->lvArgReg, argType);

                    for (unsigned ix = 0; ix < cSlots; ix++, regArgNum++)
                    {
                        if (ix > 0)
                            printf(",");

                        if (!isFloat && (regArgNum >= varDscInfo->maxIntRegArgNum)) // a struct has been split between registers and stack
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
                                printf("%s/%s", getRegName(genMapRegArgNumToRegNum(regArgNum, argType),     isFloat), 
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
#endif // DEBUG
        } // end if (canPassArgInRegisters) 
        else
        {
#ifdef _TARGET_ARM_
            varDscInfo->setAllRegArgUsed(argType);
            if (varTypeIsFloating(argType))
            {
                varDscInfo->setAnyFloatStackArgs();
            }
#endif
        }

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        // The arg size is returning the number of bytes of the argument. For a struct it could return a size not a multiple of 
        // TARGET_POINTER_SIZE. The stack allocated space should always be multiple of TARGET_POINTER_SIZE, so round it up.
        compArgSize += (unsigned)roundUp(argSize, TARGET_POINTER_SIZE);
#else // !FEATURE_UNIX_AMD64_STRUCT_PASSING
        compArgSize += argSize;
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING
        if (info.compIsVarArgs)
        {
#if defined(_TARGET_X86_)
            varDsc->lvStkOffs       = compArgSize;
#else // !_TARGET_X86_
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
                codeGen->regSet.rsMaskPreSpillAlign = (~codeGen->regSet.rsMaskPreSpillRegArg & ~doubleAlignMask) & RBM_ARG_REGS;
            }
        }
    }
#endif // _TARGET_ARM_
}

/*****************************************************************************/
void                Compiler::lvaInitGenericsCtxt(InitVarDscInfo *  varDscInfo)
{
    //@GENERICS: final instantiation-info argument for shared generic methods
    // and shared generic struct instance methods
    if (info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        info.compTypeCtxtArg = varDscInfo->varNum;

        LclVarDsc * varDsc = varDscInfo->varDsc;
        varDsc->lvIsParam   = 1;
#if ASSERTION_PROP
        varDsc->lvSingleDef = 1;
#endif

        varDsc->lvType   = TYP_I_IMPL;

        if (varDscInfo->canEnreg(TYP_I_IMPL))
        {
            /* Another register argument */

            varDsc->lvIsRegArg = 1;
            varDsc->lvArgReg   = genMapRegArgNumToRegNum(varDscInfo->regArgNum(TYP_INT), varDsc->TypeGet());
            varDsc->setPrefReg(varDsc->lvArgReg, this);
            varDsc->lvOnFrame = true; // The final home for this incoming register might be our local stack frame

            varDscInfo->intRegArgNum++;

#ifdef  DEBUG
            if  (verbose)
            {
                printf("'GenCtxt'   passed in register %s\n", getRegName(varDsc->lvArgReg));
            }
#endif
        }

        compArgSize += TARGET_POINTER_SIZE;

#if defined(_TARGET_X86_)
        if (info.compIsVarArgs)
            varDsc->lvStkOffs       = compArgSize;
#endif // _TARGET_X86_

        varDscInfo->varNum++;
        varDscInfo->varDsc++;
    }
}

/*****************************************************************************/
void                Compiler::lvaInitVarArgsHandle(InitVarDscInfo * varDscInfo)
{
    if (info.compIsVarArgs)
    {
        lvaVarargsHandleArg = varDscInfo->varNum;

        LclVarDsc * varDsc = varDscInfo->varDsc;
        varDsc->lvType      = TYP_I_IMPL;
        varDsc->lvIsParam   = 1;
        // Make sure this lives in the stack -- address may be reported to the VM.
        // TODO-CQ: This should probably be:
        //   lvaSetVarDoNotEnregister(varDscInfo->varNum DEBUG_ARG(DNER_VMNeedsStackAddr));
        // But that causes problems, so, for expedience, I switched back to this heavyweight
        // hammer.  But I think it should be possible to switch; it may just work now
        // that other problems are fixed.
        lvaSetVarAddrExposed(varDscInfo->varNum);
        
#if ASSERTION_PROP
        varDsc->lvSingleDef = 1;
#endif

        if (varDscInfo->canEnreg(TYP_I_IMPL))
        {
            /* Another register argument */

            unsigned varArgHndArgNum = varDscInfo->allocRegArg(TYP_I_IMPL);

            varDsc->lvIsRegArg = 1;
            varDsc->lvArgReg   = genMapRegArgNumToRegNum(varArgHndArgNum, TYP_I_IMPL);
            varDsc->setPrefReg(varDsc->lvArgReg, this);
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

#ifdef  DEBUG
            if  (verbose)
            {
                printf("'VarArgHnd' passed in register %s\n", getRegName(varDsc->lvArgReg));
            }
#endif // DEBUG
        }
#ifndef LEGACY_BACKEND
        else
        {
            // For the RyuJIT backend, we need to mark these as being on the stack,
            // as this is not done elsewhere in the case that canEnreg returns false.
            varDsc->lvOnFrame = true;
        }
#endif // !LEGACY_BACKEND

        /* Update the total argument size, count and varDsc */

        compArgSize += TARGET_POINTER_SIZE;

        varDscInfo->varNum++;
        varDscInfo->varDsc++;

#if defined(_TARGET_X86_)
        varDsc->lvStkOffs       = compArgSize;

        // Allocate a temp to point at the beginning of the args

        lvaVarargsBaseOfStkArgs = lvaGrabTemp(false DEBUGARG("Varargs BaseOfStkArgs"));
        lvaTable[lvaVarargsBaseOfStkArgs].lvType = TYP_I_IMPL;

#endif // _TARGET_X86_
    }
}

/*****************************************************************************/
void                Compiler::lvaInitVarDsc(LclVarDsc *              varDsc,
                                            unsigned                 varNum,
                                            CorInfoType              corInfoType,
                                            CORINFO_CLASS_HANDLE     typeHnd,
                                            CORINFO_ARG_LIST_HANDLE  varList, 
                                            CORINFO_SIG_INFO *       varSig)
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

    if (tiVerificationNeeded || compIsMethodForLRSampling)    
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

            if (type == TYP_REF && !(varDsc->lvVerTypeInfo.IsType(TI_REF) || 
                varDsc->lvVerTypeInfo.IsUnboxedGenericTypeVar()))
            {
                BADCODE("parameter type mismatch");
            }
        }

        // Disallow byrefs to byref like objects (ArgTypeHandle)
        // techncally we could get away with just not setting them
        if (varDsc->lvVerTypeInfo.IsByRef() && verIsByRefLike(DereferenceByRef(varDsc->lvVerTypeInfo)))
            varDsc->lvVerTypeInfo = typeInfo();
        
        // we don't want the EE to assert in lvaSetStruct on bad sigs, so change
        // the JIT type to avoid even trying to call back
        if (varTypeIsStruct(type) && varDsc->lvVerTypeInfo.IsDead())
            type = TYP_VOID;
    }

    if (typeHnd)
    { 
        unsigned cFlags = info.compCompHnd->getClassAttribs(typeHnd); 

        // We can get typeHnds for primitive types, these are value types which only contain
        // a primitive. We will need the typeHnd to distinguish them, so we store it here.
        if ((cFlags & CORINFO_FLG_VALUECLASS) &&
            !varTypeIsStruct(type))
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
        varDsc->lvStructGcCount = 1;

    // Set the lvType (before this point it is TYP_UNDEF).
    if ((varTypeIsStruct(type)))
    {
        lvaSetStruct(varNum, typeHnd, typeHnd!=NULL, !tiVerificationNeeded);
    }
    else
    {
        varDsc->lvType = type;
    }
    
#if OPT_BOOL_OPS
    if  (type == TYP_BOOL)
        varDsc->lvIsBoolean = true;
#endif

#ifdef DEBUG
    varDsc->lvStkOffs = BAD_STK_OFFS;
#endif
}

/*****************************************************************************
 * Returns our internal varNum for a given IL variable.
 * Asserts assume it is called after lvaTable[] has been set up.
 */

unsigned                Compiler::compMapILvarNum(unsigned ILvarNum)
{
    noway_assert(ILvarNum < info.compILlocalsCount ||
                 ILvarNum > unsigned(ICorDebugInfo::UNKNOWN_ILNUM));

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
        varNum = info.compArgsCount + lclNum;
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

unsigned                Compiler::compMap2ILvarNum(unsigned varNum)
{
    if (compIsForInlining())
    {
        return impInlineInfo->InlinerCompiler->compMap2ILvarNum(varNum);
    }
    
    noway_assert(varNum < lvaCount);

    if (varNum == info.compRetBuffArg)
        return (unsigned)ICorDebugInfo::RETBUF_ILNUM;

    // Is this a varargs function?
    if (info.compIsVarArgs && varNum == lvaVarargsHandleArg)
        return (unsigned)ICorDebugInfo::VARARGS_HND_ILNUM;

    // We create an extra argument for the type context parameter
    // needed for shared generic code.
    if ((info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE) && 
        varNum == (unsigned)info.compTypeCtxtArg)
        return (unsigned)ICorDebugInfo::TYPECTXT_ILNUM;

    // Now mutate varNum to remove extra parameters from the count.
    if ((info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE) && 
        varNum > (unsigned)info.compTypeCtxtArg)
        varNum--;

    if (info.compIsVarArgs && varNum > lvaVarargsHandleArg)
        varNum--;

    /* Is there a hidden argument for the return buffer.
       Note that this code works because if the RetBuffArg is not present, 
       compRetBuffArg will be BAD_VAR_NUM */
    if (info.compRetBuffArg != BAD_VAR_NUM && varNum > info.compRetBuffArg)
        varNum--;

    if (varNum >= info.compLocalsCount)
        return (unsigned)ICorDebugInfo::UNKNOWN_ILNUM;  // Cannot be mapped

    return varNum;
}


/*****************************************************************************
 * Returns true if variable "varNum" may be address-exposed.
 */

bool                Compiler::lvaVarAddrExposed(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    LclVarDsc   *   varDsc = &lvaTable[varNum];

    return varDsc->lvAddrExposed;
}

/*****************************************************************************
 * Returns true iff variable "varNum" should not be enregistered (or one of several reasons).
 */

bool                Compiler::lvaVarDoNotEnregister(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    LclVarDsc   *   varDsc = &lvaTable[varNum];

    return varDsc->lvDoNotEnregister;
}



/*****************************************************************************
 * Returns the handle to the class of the local variable varNum
 */

CORINFO_CLASS_HANDLE        Compiler::lvaGetStruct(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    LclVarDsc   *   varDsc = &lvaTable[varNum];

    return varDsc->lvVerTypeInfo.GetClassHandleForValueClass();
}

/*****************************************************************************
 *
 *  Compare function passed to qsort() by Compiler::lvaCanPromoteStructVar().
 */

/* static */
int __cdecl         Compiler::lvaFieldOffsetCmp(const void * field1, const void * field2)
{
    lvaStructFieldInfo * pFieldInfo1 = (lvaStructFieldInfo *)field1;
    lvaStructFieldInfo * pFieldInfo2 = (lvaStructFieldInfo *)field2;

    if (pFieldInfo1->fldOffset == pFieldInfo2->fldOffset)
    {
        return 0;
    }
    else
    {
        return (pFieldInfo1->fldOffset > pFieldInfo2->fldOffset) ? +1 : -1;
    }    
}

/*****************************************************************************
 * Is this type promotable? */

void   Compiler::lvaCanPromoteStructType(CORINFO_CLASS_HANDLE     typeHnd, 
                                         lvaStructPromotionInfo * StructPromotionInfo,
                                         bool                     sortFields)
{    
    assert(eeIsValueClass(typeHnd));
    
    if (typeHnd != StructPromotionInfo->typeHnd)
    {
        // sizeof(double) represents the size of the largest primitive type that we can struct promote
        // In the future this may be changing to XMM_REGSIZE_BYTES 
        const int MaxOffset = MAX_NumOfFieldsInPromotableStruct * sizeof(double);              // must be a compile time constant 

        assert((BYTE)MaxOffset == MaxOffset);                                                  // because lvaStructFieldInfo.fldOffset is byte-sized
        assert((BYTE)MAX_NumOfFieldsInPromotableStruct == MAX_NumOfFieldsInPromotableStruct);  // because lvaStructFieldInfo.fieldCnt is byte-sized

        bool  requiresScratchVar = false;
        bool  containsHoles      = false;
        bool  customLayout       = false;
        bool  containsGCpointers = false;
        
        StructPromotionInfo->typeHnd    = typeHnd;      
        StructPromotionInfo->canPromote = false;

        unsigned structSize = info.compCompHnd->getClassSize(typeHnd);
        if (structSize >= MaxOffset)
        {
            return;  // struct is too large
        }

        unsigned fieldCnt   = info.compCompHnd->getClassNumInstanceFields(typeHnd);
        if (fieldCnt == 0 || 
            fieldCnt > MAX_NumOfFieldsInPromotableStruct)
        {
            return;  // struct must have between 1 and MAX_NumOfFieldsInPromotableStruct fields
        }

        StructPromotionInfo->fieldCnt = (BYTE)fieldCnt;
        DWORD typeFlags = info.compCompHnd->getClassAttribs(typeHnd); 

        bool treatAsOverlapping = StructHasOverlappingFields(typeFlags);

#if 1   // TODO-Cleanup: Consider removing this entire #if block in the future

        // This method has two callers. The one in Importer.cpp passes sortFields == false
        // and the other passes sortFields == true.
        // This is a workaround that leave the inlining behavior the same and before while still
        // performing extra struct promotions when compiling the method.
        // 
        if (!sortFields)   // the condition "!sortFields" really means "we are inlining"
        {
            treatAsOverlapping = StructHasCustomLayout(typeFlags);
        }
#endif

        if (treatAsOverlapping)
        {
            return;
        }

#ifdef _TARGET_ARM_        
        // For ARM don't struct promote if we have an CUSTOMLAYOUT flag on an HFA type 
        if (StructHasCustomLayout(typeFlags) &&  IsHfa(typeHnd))
        {
            return;
        }

        // On ARM, we have a requirement on the struct alignment; see below.
        unsigned structAlignment = roundUp(info.compCompHnd->getClassAlignmentRequirement(typeHnd), TARGET_POINTER_SIZE);
#endif // _TARGET_ARM

        bool isHole[MaxOffset];         // isHole[] is initialized to true for every valid offset in the struct and false for the rest
        unsigned i;                     // then as we process the fields we clear the isHole[] values that the field spans.
        for (i=0; i < MaxOffset; i++)
        {
            isHole[i] = (i < structSize) ? true : false;
        }

        for (BYTE ordinal=0; 
             ordinal < fieldCnt; 
             ++ordinal)
        {   
            lvaStructFieldInfo * pFieldInfo = &StructPromotionInfo->fields[ordinal];
            pFieldInfo->fldHnd    = info.compCompHnd->getFieldInClass(typeHnd, ordinal); 
            unsigned fldOffset = info.compCompHnd->getFieldOffset(pFieldInfo->fldHnd); 

            // The fldOffset value should never be larger than our structSize.
            if (fldOffset >= structSize)
            {
                noway_assert(false);
                return;
            }

            pFieldInfo->fldOffset  = (BYTE)fldOffset; 
            pFieldInfo->fldOrdinal = ordinal;  
            CorInfoType corType = info.compCompHnd->getFieldType(pFieldInfo->fldHnd, &pFieldInfo->fldTypeHnd);      
            var_types   varType = JITtype2varType(corType);
            pFieldInfo->fldType = varType;
            pFieldInfo->fldSize = genTypeSize(varType);

            if (varTypeIsGC(varType))
            {
                containsGCpointers = true;
            }

            if (pFieldInfo->fldSize == 0)
            {
                // Non-primitive struct field. Don't promote.
                return;            
            }  

            if ((pFieldInfo->fldOffset % pFieldInfo->fldSize) != 0)
            {
                // The code in Compiler::genPushArgList that reconstitutes
                // struct values on the stack from promoted fields expects
                // those fields to be at their natural alignment.
                return;
            }

            // The end offset for this field should never be larger than our structSize.
            noway_assert(fldOffset + pFieldInfo->fldSize <= structSize);

            for (i=0; i < pFieldInfo->fldSize; i++)
            {
                isHole[fldOffset+i] = false;
            }
            
#ifdef _TARGET_ARM_
            // On ARM, for struct types that don't use explicit layout, the alignment of the struct is
            // at least the max alignment of its fields.  We take advantage of this invariant in struct promotion,
            // so verify it here.
            if (pFieldInfo->fldSize > structAlignment)
            {
                // Don't promote vars whose struct types violates the invariant.  (Alignment == size for primitives.)
                return;
            }
            // If we have any small fields we will allocate a single PromotedStructScratch local var for the method.
            // This is a stack area that we use to assemble the small fields in order to place them in a register argument.
            // 
            if (pFieldInfo->fldSize < TARGET_POINTER_SIZE)
            {
                requiresScratchVar = true;
            }
#endif // _TARGET_ARM_
        }

        // If we saw any GC pointer fields above then the CORINFO_FLG_CONTAINS_GC_PTR has to be set!
        noway_assert((containsGCpointers == false) || ((typeFlags & CORINFO_FLG_CONTAINS_GC_PTR) != 0));

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
        if (StructHasCustomLayout(typeFlags) &&
            ((typeFlags & CORINFO_FLG_CONTAINS_GC_PTR) == 0)   )
        {
            customLayout = true;
        }

        // Check if this promoted struct contains any holes
        //
        for (i=0; i < structSize; i++)
        {
            if (isHole[i])
            {
                containsHoles = true;
                break;
            }
        }
             
        // Cool, this struct is promotable.
        StructPromotionInfo->canPromote         = true;
        StructPromotionInfo->requiresScratchVar = requiresScratchVar;
        StructPromotionInfo->containsHoles      = containsHoles;
        StructPromotionInfo->customLayout       = customLayout;

        if (sortFields)
        {
            // Sort the fields according to the increasing order of the field offset.
            // This is needed because the fields need to be pushed on stack (for GT_LDOBJ) in order.
            qsort(StructPromotionInfo->fields, 
                  StructPromotionInfo->fieldCnt, 
                  sizeof(*StructPromotionInfo->fields), 
                  lvaFieldOffsetCmp);
        }
    }
    else
    {
        // Asking for the same type of struct as the last time.
        // Nothing need to be done.
        // Fall through ...
    }
}


/*****************************************************************************
 * Is this struct type local variable promotable? */

void   Compiler::lvaCanPromoteStructVar(unsigned lclNum, lvaStructPromotionInfo * StructPromotionInfo) 
{    
    noway_assert(lclNum < lvaCount);
    
    LclVarDsc *  varDsc = &lvaTable[lclNum];
        
    noway_assert(varTypeIsStruct(varDsc));
    noway_assert(!varDsc->lvPromoted);     // Don't ask again :)

#ifdef FEATURE_SIMD
    // If this lclVar is used in a SIMD intrinsic, then we don't want to struct promote it.
    // Note, however, that SIMD lclVars that are NOT used in a SIMD intrinsic may be
    // profitably promoted.
    if (varDsc->lvIsUsedInSIMDIntrinsic())
    {
        StructPromotionInfo->canPromote = false;
        return;
    }
    
#endif

#ifdef _TARGET_ARM_
    // Explicitly check for HFA reg args and reject them for promotion here.
    // Promoting HFA args will fire an assert in lvaAssignFrameOffsets 
    // when the HFA reg arg is struct promoted.
    //
    if (varDsc->lvIsHfaRegArg)
    {     
        StructPromotionInfo->canPromote = false;
        return;
    }
#endif

    CORINFO_CLASS_HANDLE typeHnd = varDsc->lvVerTypeInfo.GetClassHandle();
    lvaCanPromoteStructType(typeHnd, StructPromotionInfo, true);
}


/*****************************************************************************
 * Promote a struct type local */

void   Compiler::lvaPromoteStructVar(unsigned      lclNum, lvaStructPromotionInfo * StructPromotionInfo)
{                
    LclVarDsc *  varDsc = &lvaTable[lclNum];

    // We should never see a reg-sized non-field-addressed struct here.
    noway_assert(!varDsc->lvRegStruct);

    noway_assert(StructPromotionInfo->canPromote);   
    noway_assert(StructPromotionInfo->typeHnd == varDsc->lvVerTypeInfo.GetClassHandle());
    
    varDsc->lvFieldCnt      = StructPromotionInfo->fieldCnt;
    varDsc->lvFieldLclStart = lvaCount; 
    varDsc->lvPromoted      = true;
    varDsc->lvContainsHoles = StructPromotionInfo->containsHoles;
    varDsc->lvCustomLayout  = StructPromotionInfo->customLayout;

#ifdef DEBUG
    //Don't change the source to a TYP_BLK either.
    varDsc->lvKeepType = 1;
#endif

#ifdef DEBUG
    if (verbose)
    {
        printf("\nPromoting struct local V%02u (%s):",
               lclNum, eeGetClassName(StructPromotionInfo->typeHnd));
    }
#endif    
           
    for (unsigned index=0; 
         index<StructPromotionInfo->fieldCnt; 
         ++index)
    {         
        lvaStructFieldInfo * pFieldInfo = &StructPromotionInfo->fields[index]; 

        if (varTypeIsFloating(pFieldInfo->fldType))
        {        
            lvaTable[lclNum].lvContainsFloatingFields = 1;   
            // Whenever we promote a struct that contains a floating point field
            // it's possible we transition from a method that originally only had integer
            // local vars to start having FP.  We have to communicate this through this flag
            // since LSRA later on will use this flag to determine whether or not to track FP register sets.
            compFloatingPointUsed = true;
        }
   
        // Now grab the temp for the field local.

#ifdef DEBUG
        char    buf[200];
        char *  bufp     = &buf[0];

        sprintf_s(bufp, sizeof(buf), "%s V%02u.%s (fldOffset=0x%x)", 
                  "field", 
                  lclNum,
                  eeGetFieldName(pFieldInfo->fldHnd),
                  pFieldInfo->fldOffset);

        if (index>0)
        {
            noway_assert(pFieldInfo->fldOffset > (pFieldInfo-1)->fldOffset);    
        }
#endif
        
        unsigned varNum = lvaGrabTemp(false DEBUGARG(bufp)); // Lifetime of field locals might span multiple BBs, so they are long lifetime temps.

        LclVarDsc *  fieldVarDsc      = &lvaTable[varNum];
        fieldVarDsc->lvType           = pFieldInfo->fldType;            
        fieldVarDsc->lvExactSize      = pFieldInfo->fldSize;
        fieldVarDsc->lvIsStructField  = true;
        fieldVarDsc->lvFldOffset      = pFieldInfo->fldOffset;
        fieldVarDsc->lvFldOrdinal     = pFieldInfo->fldOrdinal;            
        fieldVarDsc->lvParentLcl      = lclNum;
        fieldVarDsc->lvIsParam        = varDsc->lvIsParam;
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
        // Do we have a parameter that can be enregistered?
        //
        if (varDsc->lvIsRegArg)
        {
            fieldVarDsc->lvIsRegArg = true;
            fieldVarDsc->lvArgReg = varDsc->lvArgReg;
            fieldVarDsc->setPrefReg(varDsc->lvArgReg, this);   // Set the preferred register

            lvaMarkRefsWeight = BB_UNITY_WEIGHT;               // incRefCnts can use this compiler global variable
            fieldVarDsc->incRefCnts(BB_UNITY_WEIGHT, this);    // increment the ref count for prolog initialization
        }
#endif

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
void   Compiler::lvaPromoteLongVars()
{
    if ((opts.compFlags & CLFLG_REGVAR) == 0)
    {
        return;
    }
    // The lvaTable might grow as we grab temps. Make a local copy here.
    unsigned        startLvaCount = lvaCount;
    for (unsigned lclNum = 0;
         lclNum < startLvaCount;
         lclNum++)
    {
        LclVarDsc *  varDsc = &lvaTable[lclNum];
        if(!varTypeIsLong(varDsc) || varDsc->lvDoNotEnregister || (varDsc->lvRefCnt == 0))
        {
            continue;
        }

        // Will this work ???
        // We can't have nested promoted structs.
        if (varDsc->lvIsStructField)
        {
            if (lvaGetPromotionType(varDsc->lvParentLcl) != PROMOTION_TYPE_INDEPENDENT)
            {
                continue;
            }
            varDsc->lvIsStructField = false;
            varDsc->lvTracked = false;
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
           
        for (unsigned index=0; index < 2; ++index)
        {         
            // Grab the temp for the field local.

#ifdef DEBUG
            char    buf[200];
            char *  bufp     = &buf[0];

            sprintf_s(bufp, sizeof(buf), "%s V%02u.%s (fldOffset=0x%x)", 
                      "field", 
                      lclNum,
                      index == 0 ? "lo" : "hi",
                      index * 4);
#endif
            unsigned varNum = lvaGrabTemp(false DEBUGARG(bufp)); // Lifetime of field locals might span multiple BBs, so they are long lifetime temps.

            LclVarDsc *  fieldVarDsc      = &lvaTable[varNum];
            fieldVarDsc->lvType           = TYP_INT;            
            fieldVarDsc->lvExactSize      = genTypeSize(TYP_INT);
            fieldVarDsc->lvIsStructField  = true;
            fieldVarDsc->lvFldOffset      = (unsigned char)(index * genTypeSize(TYP_INT));
            fieldVarDsc->lvFldOrdinal     = (unsigned char)index;            
            fieldVarDsc->lvParentLcl      = lclNum;
            fieldVarDsc->lvIsParam        = isParam;
        }
    }
}
#endif // !_TARGET_64BIT_

/*****************************************************************************
 * Given a fldOffset in a promoted struct var, return the index of the local
   that represents this field.
*/

unsigned   Compiler::lvaGetFieldLocal(LclVarDsc *  varDsc, unsigned int fldOffset)
{   
    noway_assert(varTypeIsStruct(varDsc));
    noway_assert(varDsc->lvPromoted);

    for (unsigned i = varDsc->lvFieldLclStart;
         i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt;
         ++i)
    {        
        noway_assert(lvaTable[i].lvIsStructField);
        noway_assert(lvaTable[i].lvParentLcl == (unsigned) (varDsc-lvaTable));
        if (lvaTable[i].lvFldOffset == fldOffset)
        {
            return i;
        }
    }

    noway_assert(!"Cannot find field local.");
    return BAD_VAR_NUM;
}

/*****************************************************************************
 *
 *  Set the local var "varNum" as address-exposed.
 *  If this is a promoted struct, label it's fields the same way.
 */

void               Compiler::lvaSetVarAddrExposed(unsigned varNum)
{              
    noway_assert(varNum < lvaCount);
                
    LclVarDsc   *   varDsc = &lvaTable[varNum];
   
    varDsc->lvAddrExposed = 1;
    
    if (varDsc->lvPromoted)
    {
        noway_assert(varTypeIsStruct(varDsc));
        
        for (unsigned i = varDsc->lvFieldLclStart;
             i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt;
             ++i)
        {        
            noway_assert(lvaTable[i].lvIsStructField);            
            lvaTable[i].lvAddrExposed = 1;   // Make field local as address-exposed.
            lvaSetVarDoNotEnregister(i DEBUG_ARG(DNER_AddrExposed));
        }
    }

    lvaSetVarDoNotEnregister(varNum DEBUG_ARG(DNER_AddrExposed));
}


/*****************************************************************************
 *
 *  Record that the local var "varNum" should not be enregistered (for one of several reasons.)
 */

void               Compiler::lvaSetVarDoNotEnregister(unsigned varNum DEBUG_ARG(DoNotEnregisterReason reason))
{              
    noway_assert(varNum < lvaCount);
    LclVarDsc   *   varDsc = &lvaTable[varNum];
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
#ifdef JIT32_GCENCODER
    case DNER_PinningRef:
        JITDUMP("pinning ref\n");
        assert(varDsc->lvPinned);
        break;
#endif
    default:
        unreached();
        break;
    }
#endif
}

/*****************************************************************************
 * Set the lvClass for a local variable of a struct type */

void   Compiler::lvaSetStruct(unsigned varNum, CORINFO_CLASS_HANDLE typeHnd, bool unsafeValueClsCheck, bool setTypeInfo)
{
    noway_assert(varNum < lvaCount);

    LclVarDsc *  varDsc = &lvaTable[varNum];
    if (setTypeInfo)
        varDsc->lvVerTypeInfo = typeInfo(TI_STRUCT, typeHnd);

    // Set the type and associated info if we haven't already set it.
    var_types structType = varDsc->lvType;
    if (varDsc->lvType == TYP_UNDEF)
    {
        varDsc->lvType = TYP_STRUCT;
    }
    if (varDsc->lvExactSize == 0)
    {
        varDsc->lvExactSize = info.compCompHnd->getClassSize(typeHnd);

        size_t lvSize = varDsc->lvSize();
        assert((lvSize % sizeof(void*)) == 0); // The struct needs to be a multiple of sizeof(void*) bytes for getClassGClayout() to be valid.
        varDsc->lvGcLayout = (BYTE*)compGetMemA((lvSize / sizeof(void*)) * sizeof(BYTE), CMK_LvaTable);
        unsigned numGCVars;
        var_types simdBaseType = TYP_UNKNOWN;
        varDsc->lvType = impNormStructType(typeHnd, varDsc->lvGcLayout, &numGCVars, &simdBaseType);

        // We only save the count of GC vars in a struct up to 7.
        if (numGCVars >= 8)
            numGCVars = 7;
        varDsc->lvStructGcCount = numGCVars;
#if FEATURE_SIMD
        if (simdBaseType != TYP_UNKNOWN)
        {
            assert(varTypeIsSIMD(varDsc));
            varDsc->lvSIMDType = true;
            varDsc->lvBaseType = simdBaseType;
        }
#endif // FEATURE_SIMD
    }
    else
    {
        assert(varDsc->lvExactSize != 0);
#if FEATURE_SIMD
        assert(!varTypeIsSIMD(varDsc) || (varDsc->lvBaseType != TYP_UNKNOWN));
#endif // FEATURE_SIMD
    }

#ifndef _TARGET_64BIT_
    bool fDoubleAlignHint = FALSE;
# ifdef _TARGET_X86_
    fDoubleAlignHint = TRUE;
# endif

    if (info.compCompHnd->getClassAlignmentRequirement(typeHnd, fDoubleAlignHint) == 8)
    {
#ifdef DEBUG    
        if  (verbose)
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
    if (unsafeValueClsCheck && 
        (classAttribs & CORINFO_FLG_UNSAFE_VALUECLASS) &&
        !opts.compDbgEnC)
    {    
        setNeedsGSSecurityCookie();
        compGSReorderStackLayout = true;
        varDsc->lvIsUnsafeBuffer = true;
    }
}

/*****************************************************************************
 * Returns the array of BYTEs containing the GC layout information
 */

BYTE *             Compiler::lvaGetGcLayout(unsigned varNum)
{
    noway_assert(varTypeIsStruct(lvaTable[varNum].lvType) && (lvaTable[varNum].lvExactSize >= TARGET_POINTER_SIZE));

    return lvaTable[varNum].lvGcLayout;
}

/*****************************************************************************
 * Return the number of bytes needed for a local variable
 */

unsigned            Compiler::lvaLclSize(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    
    var_types   varType = lvaTable[varNum].TypeGet();

    switch (varType)
    {
    case TYP_STRUCT:
    case TYP_BLK:
        return lvaTable[varNum].lvSize();

    case TYP_LCLBLK:
#if FEATURE_FIXED_OUT_ARGS
        noway_assert(lvaOutgoingArgSpaceSize >= 0);
        noway_assert(varNum == lvaOutgoingArgSpaceVar);
        return lvaOutgoingArgSpaceSize;

#else // FEATURE_FIXED_OUT_ARGS
        assert(!"Unknown size");
        NO_WAY("Target doesn't support TYP_LCLBLK");

        // Keep prefast happy
        __fallthrough;

#endif // FEATURE_FIXED_OUT_ARGS

    default:    // This must be a primitive var. Fall out of switch statement
        break;
    }
    // We only need this Quirk for _TARGET_64BIT_
#ifdef _TARGET_64BIT_
    if (lvaTable[varNum].lvQuirkToLong)
    {
        noway_assert(lvaTable[varNum].lvAddrExposed);
        return genTypeStSz(TYP_LONG)*sizeof(int);         // return 8  (2 * 4)
    }
#endif
    return genTypeStSz(varType)*sizeof(int);
}

//
// Return the exact width of local variable "varNum" -- the number of bytes
// you'd need to copy in order to overwrite the value.
// 
unsigned            Compiler::lvaLclExactSize(unsigned varNum)
{
    noway_assert(varNum < lvaCount);
    
    var_types   varType = lvaTable[varNum].TypeGet();

    switch (varType)
    {
    case TYP_STRUCT:
    case TYP_BLK:
        return lvaTable[varNum].lvExactSize;

    case TYP_LCLBLK:
#if FEATURE_FIXED_OUT_ARGS
        noway_assert(lvaOutgoingArgSpaceSize >= 0);
        noway_assert(varNum == lvaOutgoingArgSpaceVar);
        return lvaOutgoingArgSpaceSize;

#else // FEATURE_FIXED_OUT_ARGS
        assert(!"Unknown size");
        NO_WAY("Target doesn't support TYP_LCLBLK");

        // Keep prefast happy
        __fallthrough;

#endif // FEATURE_FIXED_OUT_ARGS

    default:    // This must be a primitive var. Fall out of switch statement
        break;
    }

    return genTypeSize(varType);
}

//getBBWeight -- get the normalized weight of this block
unsigned  BasicBlock::getBBWeight(Compiler * comp)
{
    if (this->bbWeight == 0)
        return 0;
    else
    {
        unsigned calledWeight = comp->fgCalledWeight;
        if (calledWeight == 0)
        {
            calledWeight = comp->fgFirstBB->bbWeight;
            if (calledWeight == 0)
                calledWeight = BB_UNITY_WEIGHT;
        }
        if (this->bbWeight < (BB_MAX_WEIGHT / BB_UNITY_WEIGHT)) 
            return max(1, (((this->bbWeight * BB_UNITY_WEIGHT) + (calledWeight/2)) / calledWeight));
        else
            return (unsigned) ((((double)this->bbWeight * (double)BB_UNITY_WEIGHT) / (double)calledWeight) + 0.5);
    }
}


/*****************************************************************************
 *
 *  Callback used by the tree walker to call lvaDecRefCnts
 */
Compiler::fgWalkResult      Compiler::lvaDecRefCntsCB(GenTreePtr *pTree, fgWalkData *data)
{
    data->compiler->lvaDecRefCnts(*pTree);
    return WALK_CONTINUE;
}


// Decrement the ref counts for all locals contained in the tree and its children.
void Compiler::lvaRecursiveDecRefCounts(GenTreePtr tree)
{
    assert(lvaLocalVarRefCounted);

    // We could just use the recursive walker for all cases but that is a 
    // fairly heavyweight thing to spin up when we're usually just handling a leaf.
    if (tree->OperIsLeaf())
    {
        if (tree->OperIsLocal())
        {
            lvaDecRefCnts(tree);
        }
    }
    else
    {
        fgWalkTreePre(&tree, Compiler::lvaDecRefCntsCB, (void *)this, true);
    }
}

// Increment the ref counts for all locals contained in the tree and its children.
void Compiler::lvaRecursiveIncRefCounts(GenTreePtr tree)
{
    assert(lvaLocalVarRefCounted);

    // We could just use the recursive walker for all cases but that is a 
    // fairly heavyweight thing to spin up when we're usually just handling a leaf.
    if (tree->OperIsLeaf())
    {
        if (tree->OperIsLocal())
        {
            lvaIncRefCnts(tree);
        }
    }
    else
    {
        fgWalkTreePre(&tree, Compiler::lvaIncRefCntsCB, (void *)this, true);
    }
}

/*****************************************************************************
 *
 *  Helper passed to the tree walker to decrement the refCnts for
 *  all local variables in an expression
 */
void               Compiler::lvaDecRefCnts(GenTreePtr tree)
{
    unsigned        lclNum;
    LclVarDsc   *   varDsc;

    noway_assert(lvaRefCountingStarted || lvaLocalVarRefCounted);

    if ((tree->gtOper == GT_CALL) && (tree->gtFlags & GTF_CALL_UNMANAGED))
    {
        /* Get the special variable descriptor */

        lclNum = info.compLvFrameListRoot;
            
        noway_assert(lclNum <= lvaCount);
        varDsc = lvaTable + lclNum;
            
        /* Decrement the reference counts twice */

        varDsc->decRefCnts(compCurBB->getBBWeight(this), this);  
        varDsc->decRefCnts(compCurBB->getBBWeight(this), this);
    }
    else
    {
        /* This must be a local variable */

        noway_assert(tree->OperIsLocal());

        /* Get the variable descriptor */

        lclNum = tree->gtLclVarCommon.gtLclNum;

        noway_assert(lclNum < lvaCount);
        varDsc = lvaTable + lclNum;

        /* Decrement its lvRefCnt and lvRefCntWtd */

        varDsc->decRefCnts(compCurBB->getBBWeight(this), this);
    }
}

/*****************************************************************************
 *
 *  Callback used by the tree walker to call lvaIncRefCnts
 */
Compiler::fgWalkResult      Compiler::lvaIncRefCntsCB(GenTreePtr *pTree, fgWalkData *data)
{
    data->compiler->lvaIncRefCnts(*pTree);
    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Helper passed to the tree walker to increment the refCnts for
 *  all local variables in an expression
 */
void               Compiler::lvaIncRefCnts(GenTreePtr tree)
{
    unsigned        lclNum;
    LclVarDsc   *   varDsc;

    noway_assert(lvaRefCountingStarted || lvaLocalVarRefCounted);

    if ((tree->gtOper == GT_CALL) && (tree->gtFlags & GTF_CALL_UNMANAGED))
    {
        /* Get the special variable descriptor */

        lclNum = info.compLvFrameListRoot;
            
        noway_assert(lclNum <= lvaCount);
        varDsc = lvaTable + lclNum;
            
        /* Increment the reference counts twice */

        varDsc->incRefCnts(compCurBB->getBBWeight(this), this);  
        varDsc->incRefCnts(compCurBB->getBBWeight(this), this);
    }
    else
    {
        /* This must be a local variable */

        noway_assert(tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_FLD || tree->gtOper == GT_STORE_LCL_VAR || tree->gtOper == GT_STORE_LCL_FLD);

        /* Get the variable descriptor */

        lclNum = tree->gtLclVarCommon.gtLclNum;

        noway_assert(lclNum < lvaCount);
        varDsc = lvaTable + lclNum;

        /* Increment its lvRefCnt and lvRefCntWtd */

        varDsc->incRefCnts(compCurBB->getBBWeight(this), this);
    }
}


/*****************************************************************************
 *
 *  Compare function passed to qsort() by Compiler::lclVars.lvaSortByRefCount().
 *  when generating SMALL_CODE.
 *    Return positive if dsc2 has a higher ref count
 *    Return negative if dsc1 has a higher ref count
 *    Return zero     if the ref counts are the same
 *    lvPrefReg is only used to break ties
 */

/* static */
int __cdecl         Compiler::RefCntCmp(const void *op1, const void *op2)
{
    LclVarDsc *     dsc1 = *(LclVarDsc * *)op1;
    LclVarDsc *     dsc2 = *(LclVarDsc * *)op2;

    /* Make sure we preference tracked variables over untracked variables */

    if  (dsc1->lvTracked != dsc2->lvTracked)
    {
        return (dsc2->lvTracked) ? +1 : -1;
    }


    unsigned weight1 = dsc1->lvRefCnt;
    unsigned weight2 = dsc2->lvRefCnt;

#if !FEATURE_FP_REGALLOC
    /* Force integer candidates to sort above float candidates */

    bool     isFloat1 = isFloatRegType(dsc1->lvType);
    bool     isFloat2 = isFloatRegType(dsc2->lvType);

    if  (isFloat1 != isFloat2)
    {
        if (weight2 && isFloat1)
            return +1;
        if (weight1 && isFloat2)
            return -1;
    }
#endif

    int diff = weight2 - weight1;

    if  (diff != 0)
       return diff;

    /* The unweighted ref counts were the same */
    /* If the weighted ref counts are different then use their difference */
    diff = dsc2->lvRefCntWtd - dsc1->lvRefCntWtd;

    if  (diff != 0)
       return diff;

    /* We have equal ref counts and weighted ref counts */

    /* Break the tie by: */
    /* Increasing the weight by 2   if we have exactly one bit set in lvPrefReg   */
    /* Increasing the weight by 1   if we have more than one bit set in lvPrefReg */
    /* Increasing the weight by 0.5 if we are a GC type */
    /* Increasing the weight by 0.5 if we were enregistered in the previous pass  */

    if (weight1)
    {
        if (dsc1->lvPrefReg)
        {
            if ( (dsc1->lvPrefReg & ~RBM_BYTE_REG_FLAG) && genMaxOneBit((unsigned)dsc1->lvPrefReg))
                weight1 += 2 * BB_UNITY_WEIGHT;
            else
                weight1 += 1 * BB_UNITY_WEIGHT;
        }
        if (varTypeIsGC(dsc1->TypeGet()))
            weight1 += BB_UNITY_WEIGHT / 2;

        if (dsc1->lvRegister)
            weight1 += BB_UNITY_WEIGHT / 2;
    }

    if (weight2)
    {
        if (dsc2->lvPrefReg)
        {
            if ( (dsc2->lvPrefReg & ~RBM_BYTE_REG_FLAG) && genMaxOneBit((unsigned)dsc2->lvPrefReg))
                weight2 += 2 * BB_UNITY_WEIGHT;
            else
                weight2 += 1 * BB_UNITY_WEIGHT;
        }
        if (varTypeIsGC(dsc2->TypeGet()))
            weight1 += BB_UNITY_WEIGHT / 2;

        if (dsc2->lvRegister)
            weight2 += BB_UNITY_WEIGHT / 2;
    }

    diff = weight2 - weight1;

    if (diff != 0)
        return diff;

    /* To achieve a Stable Sort we use the LclNum (by way of the pointer address) */

    if (dsc1 < dsc2)
        return -1;
    if (dsc1 > dsc2)
        return +1;

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
int __cdecl         Compiler::WtdRefCntCmp(const void *op1, const void *op2)
{
    LclVarDsc *     dsc1 = *(LclVarDsc * *)op1;
    LclVarDsc *     dsc2 = *(LclVarDsc * *)op2;

    /* Make sure we preference tracked variables over untracked variables */

    if  (dsc1->lvTracked != dsc2->lvTracked)
    {
        return (dsc2->lvTracked) ? +1 : -1;
    }

    unsigned weight1 = dsc1->lvRefCntWtd;
    unsigned weight2 = dsc2->lvRefCntWtd;

#if !FEATURE_FP_REGALLOC
    /* Force integer candidates to sort above float candidates */

    bool     isFloat1 = isFloatRegType(dsc1->lvType);
    bool     isFloat2 = isFloatRegType(dsc2->lvType);

    if  (isFloat1 != isFloat2)
    {
        if (weight2 && isFloat1)
            return +1;
        if (weight1 && isFloat2)
            return -1;
    }
#endif

    /* Increase the weight by 2 if we have exactly one bit set in lvPrefReg */
    /* Increase the weight by 1 if we have more than one bit set in lvPrefReg */

    if (weight1 && dsc1->lvPrefReg)
    {
        if ( (dsc1->lvPrefReg & ~RBM_BYTE_REG_FLAG) && genMaxOneBit((unsigned)dsc1->lvPrefReg))
            weight1 += 2 * BB_UNITY_WEIGHT;
        else
            weight1 += 1 * BB_UNITY_WEIGHT;
    }

    if (weight2 && dsc2->lvPrefReg)
    {
        if ( (dsc2->lvPrefReg & ~RBM_BYTE_REG_FLAG) && genMaxOneBit((unsigned)dsc2->lvPrefReg))
            weight2 += 2 * BB_UNITY_WEIGHT;
        else
            weight2 += 1 * BB_UNITY_WEIGHT;
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
    int diff = (int)dsc2->lvRefCnt - (int)dsc1->lvRefCnt;

    if  (diff != 0)
       return diff;

    /* If one is a GC type and the other is not the GC type wins */
    if (varTypeIsGC(dsc1->TypeGet()) != varTypeIsGC(dsc2->TypeGet()))
    {
        if (varTypeIsGC(dsc1->TypeGet()))
            diff = -1;
        else
            diff = +1;

        return diff;
    }
        
    /* If one was enregistered in the previous pass then it wins */
    if (dsc1->lvRegister != dsc2->lvRegister)
    {
        if (dsc1->lvRegister)
            diff = -1;
        else
            diff = +1;

        return diff;
    }   

    /* We have a tie! */

    /* To achieve a Stable Sort we use the LclNum (by way of the pointer address) */

    if (dsc1 < dsc2)
        return -1;
    if (dsc1 > dsc2)
        return +1;

    return 0;
}


/*****************************************************************************
 *
 *  Sort the local variable table by refcount and assign tracking indices.
 */

void                Compiler::lvaSortOnly()
{
    /* Now sort the variable table by ref-count */

    qsort(lvaRefSorted, lvaCount, sizeof(*lvaRefSorted),
          (compCodeOpt() == SMALL_CODE) ? RefCntCmp
                                        : WtdRefCntCmp);

    lvaSortAgain = false;

    lvaDumpRefCounts();

}

void 
Compiler::lvaDumpRefCounts()
{
#ifdef  DEBUG

    if  (verbose && lvaCount)
    {
        printf("refCnt table for '%s':\n", info.compMethodName);

        for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
        {
            unsigned refCnt = lvaRefSorted[lclNum]->lvRefCnt;
            if  (refCnt == 0)
                break;
            unsigned refCntWtd = lvaRefSorted[lclNum]->lvRefCntWtd;

            printf("   ");
            gtDispLclVar((unsigned)(lvaRefSorted[lclNum] - lvaTable));
            printf(" [%6s]: refCnt = %4u, refCntWtd = %6s",
                   varTypeName(lvaRefSorted[lclNum]->TypeGet()),
                   refCnt,
                   refCntWtd2str(refCntWtd));

            regMaskSmall pref = lvaRefSorted[lclNum]->lvPrefReg;
            if (pref)
            {
                printf(" pref ");
                dspRegMask(pref);
            }
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

void                Compiler::lvaSortByRefCount()
{
    lvaTrackedCount = 0;
    lvaTrackedCountInSizeTUnits = 0;

    if (lvaCount == 0)
        return;

    unsigned        lclNum;
    LclVarDsc   *   varDsc;

    LclVarDsc * *   refTab;

    /* We'll sort the variables by ref count - allocate the sorted table */

    lvaRefSorted = refTab = new (this, CMK_LvaTable) LclVarDsc*[lvaCount];

    /* Fill in the table used for sorting */

    for (lclNum = 0, varDsc = lvaTable;
         lclNum < lvaCount;
         lclNum++  , varDsc++)
    {
        /* Append this variable to the table for sorting */

        *refTab++ = varDsc;

        /* If we have JMP, all arguments must have a location
         * even if we don't use them inside the method */

        if  (compJmpOpUsed && varDsc->lvIsParam)
        {
            /* ...except when we have varargs and the argument is
              passed on the stack.  In that case, it's important
              for the ref count to be zero, so that we don't attempt
              to track them for GC info (which is not possible since we
              don't know their offset in the stack).  See the assert at the
              end of raMarkStkVars and bug #28949 for more info. */

            if (!raIsVarargsStackArg(lclNum))
            {
                varDsc->incRefCnts(1, this);
            }
        }

        /* For now assume we'll be able to track all locals */

        varDsc->lvTracked = 1;

        /* If the ref count is zero */
        if  (varDsc->lvRefCnt == 0)
        {
            /* Zero ref count, make this untracked */
            varDsc->lvTracked   = 0;
            varDsc->lvRefCntWtd = 0;
        }

#if !defined(_TARGET_64BIT_) && !defined(LEGACY_BACKEND)
        if (varTypeIsLong(varDsc) && varDsc->lvPromoted)
        {
            varDsc->lvTracked = 0;
        }
#endif // !defined(_TARGET_64BIT_) && !defined(LEGACY_BACKEND)

        // Variables that are address-exposed, and all struct locals, are never enregistered, or tracked.
        // (The struct may be promoted, and its field variables enregistered/tracked, or the VM may "normalize"
        // its type so that its not seen by the JIT as a struct.)
        // Pinned variables may not be tracked (a condition of the GCInfo representation)
        // or enregistered, on x86 -- it is believed that we can enregister pinned (more properly, "pinning")
        // references when using the general GC encoding.
        if  (varDsc->lvAddrExposed)
        {
            varDsc->lvTracked  = 0;
            assert(varDsc->lvType != TYP_STRUCT || varDsc->lvDoNotEnregister);  // For structs, should have set this when we set lvAddrExposed.
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
                lvaSetVarDoNotEnregister(lclNum DEBUG_ARG(DNER_IsStruct));
            }
        }
        else if (varDsc->lvIsStructField &&
                (lvaGetParentPromotionType(lclNum) != PROMOTION_TYPE_INDEPENDENT))
        {
            // SSA must exclude struct fields that are not independently promoted
            // as dependent fields could be assigned using a CopyBlock 
            // resulting in a single node causing multiple SSA definitions
            // which isn't currently supported by SSA
            //
            // TODO-CQ:  Consider using lvLclBlockOpAddr and only marking these LclVars 
            // untracked when a blockOp is used to assign the struct.
            //
            varDsc->lvTracked = 0;  // so, don't mark as tracked
        }
        else if (varDsc->lvPinned)
        {
            varDsc->lvTracked = 0;
#ifdef JIT32_GCENCODER
            lvaSetVarDoNotEnregister(lclNum DEBUG_ARG(DNER_PinningRef));
#endif
        }

        //  Are we not optimizing and we have exception handlers?
        //   if so mark all args and locals "do not enregister".
        //
        if  (opts.MinOpts() && compHndBBtabCount > 0)
        {
            lvaSetVarDoNotEnregister(lclNum DEBUG_ARG(DNER_LiveInOutOfHandler));
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

    if  (lvaCount > lclMAX_TRACKED)
    {
        /* Mark all variables past the first 'lclMAX_TRACKED' as untracked */

        for (lclNum = lclMAX_TRACKED; lclNum < lvaCount; lclNum++)
        {
            lvaRefSorted[lclNum]->lvTracked = 0;
        }
    }

#ifdef DEBUG
    // Re-Initialize to -1 for safety in debug build.
    memset(lvaTrackedToVarNum, -1, sizeof(lvaTrackedToVarNum));
#endif

    /* Assign indices to all the variables we've decided to track */

    for (lclNum = 0; lclNum < min(lvaCount,lclMAX_TRACKED); lclNum++)
    {
        varDsc = lvaRefSorted[lclNum];
        if  (varDsc->lvTracked)
        {
            noway_assert(varDsc->lvRefCnt > 0);

            /* This variable will be tracked - assign it an index */

            lvaTrackedToVarNum[lvaTrackedCount] = (unsigned)(varDsc - lvaTable); // The type of varDsc and lvaTable 
                                                                                 // is LclVarDsc. Subtraction will give us
                                                                                 // the index.
            varDsc->lvVarIndex = lvaTrackedCount++;
        }
    }

    // We have a new epoch, and also cache the tracked var count in terms of size_t's sufficient to hold that many bits.
    lvaCurEpoch++;
    lvaTrackedCountInSizeTUnits = unsigned(roundUp(lvaTrackedCount, sizeof(size_t)*8))/unsigned(sizeof(size_t)*8);

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
void                LclVarDsc::lvaDisqualifyVar()
{
    this->lvDisqualify  = true;
    this->lvSingleDef   = false;
    this->lvDefStmt     = NULL;
}
#endif // ASSERTION_PROP

#ifndef LEGACY_BACKEND
/********************************************************************************** 
 * Get type of a variable when passed as an argument.
 */
var_types           LclVarDsc::lvaArgType()
{
    var_types type = TypeGet();

#ifdef _TARGET_AMD64_
    if (type == TYP_STRUCT)
    {
        switch (lvExactSize)
        {
           case 1: type = TYP_BYTE;  break;
           case 2: type = TYP_SHORT; break;
           case 4: type = TYP_INT;   break;
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
#else
    NYI("unknown target");
#endif //_TARGET_AMD64_

    return type;
}
#endif  // !LEGACY_BACKEND


/*****************************************************************************
 *
 *  This is called by lvaMarkLclRefsCallback() to do variable ref marking
 */

void                Compiler::lvaMarkLclRefs(GenTreePtr tree)
{
#if INLINE_NDIRECT
    /* Is this a call to unmanaged code ? */
    if (tree->gtOper == GT_CALL && tree->gtFlags & GTF_CALL_UNMANAGED) 
    {
        /* Get the special variable descriptor */

        unsigned lclNum = info.compLvFrameListRoot;
            
        noway_assert(lclNum <= lvaCount);
        LclVarDsc * varDsc = lvaTable + lclNum;

        /* Increment the ref counts twice */
        varDsc->incRefCnts(lvaMarkRefsWeight, this);
        varDsc->incRefCnts(lvaMarkRefsWeight, this);
    }
#endif
        
    /* Is this an assigment? */

    if (tree->OperKind() & GTK_ASGOP)
    {
        GenTreePtr      op1 = tree->gtOp.gtOp1;
        GenTreePtr      op2 = tree->gtOp.gtOp2;


        /* Set target register for RHS local if assignment is of a "small" type */

        if (varTypeIsByte(tree->gtType))
        {
            unsigned      lclNum;
            LclVarDsc *   varDsc = NULL;

            /* GT_CHS is special it doesn't have a valid op2 */
            if (tree->gtOper == GT_CHS) 
            {
                if  (op1->gtOper == GT_LCL_VAR)
                {      
                    lclNum = op1->gtLclVarCommon.gtLclNum;
                    noway_assert(lclNum < lvaCount);
                    varDsc = &lvaTable[lclNum];
                }
            }
            else 
            {
                if  (op2->gtOper == GT_LCL_VAR)
                {
                    lclNum = op2->gtLclVarCommon.gtLclNum;
                    noway_assert(lclNum < lvaCount);
                    varDsc = &lvaTable[lclNum];
                }
            }
#if CPU_HAS_BYTE_REGS
            if (varDsc)
                varDsc->addPrefReg(RBM_BYTE_REG_FLAG, this);
#endif
        }

#if OPT_BOOL_OPS

        /* Is this an assignment to a local variable? */

        if  (op1->gtOper == GT_LCL_VAR && op2->gtType != TYP_BOOL)
        {
            /* Only simple assignments allowed for booleans */

            if  (tree->gtOper != GT_ASG)
                goto NOT_BOOL;

            /* Is the RHS clearly a boolean value? */

            switch (op2->gtOper)
            {
                unsigned        lclNum;

            case GT_CNS_INT:

                if  (op2->gtIntCon.gtIconVal == 0)
                    break;
                if  (op2->gtIntCon.gtIconVal == 1)
                    break;

                // Not 0 or 1, fall through ....
                __fallthrough;

            default:

                if (op2->OperIsCompare())
                    break;

            NOT_BOOL:

                lclNum = op1->gtLclVarCommon.gtLclNum;
                noway_assert(lclNum < lvaCount);

                lvaTable[lclNum].lvIsBoolean = false;
                break;
            }
        }
#endif
    }

#if FANCY_ARRAY_OPT

    /* Special case: assignment node */

    if  (tree->gtOper == GT_ASG)
    {
        if  (tree->gtType == TYP_INT)
        {
            unsigned        lclNum1;
            LclVarDsc   *   varDsc1;

            GenTreePtr      op1 = tree->gtOp.gtOp1;

            if  (op1->gtOper != GT_LCL_VAR)
                return;

            lclNum1 = op1->gtLclVarCommon.gtLclNum;
            noway_assert(lclNum1 < lvaCount);
            varDsc1 = lvaTable + lclNum1;

            if  (varDsc1->lvAssignOne)
                varDsc1->lvAssignTwo = true;
            else
                varDsc1->lvAssignOne = true;
        }

        return;
    }

#endif

#ifdef _TARGET_XARCH_
    /* Special case: integer shift node by a variable amount */

    if  (tree->gtOper == GT_LSH ||
         tree->gtOper == GT_RSH ||
         tree->gtOper == GT_RSZ ||
         tree->gtOper == GT_ROL ||
         tree->gtOper == GT_ROR)
    {
        if  (tree->gtType == TYP_INT)
        {
            GenTreePtr      op2 = tree->gtOp.gtOp2;

            if  (op2->gtOper == GT_LCL_VAR)
            {
                unsigned lclNum = op2->gtLclVarCommon.gtLclNum;
                noway_assert(lclNum < lvaCount);
                lvaTable[lclNum].setPrefReg(REG_ECX, this);
            }
        }

        return;
    }
#endif

    if  ((tree->gtOper != GT_LCL_VAR) && (tree->gtOper != GT_LCL_FLD))
        return;

    /* This must be a local variable reference */

    noway_assert((tree->gtOper == GT_LCL_VAR) || (tree->gtOper == GT_LCL_FLD));
    unsigned lclNum = tree->gtLclVarCommon.gtLclNum;

    noway_assert(lclNum < lvaCount);
    LclVarDsc * varDsc = lvaTable + lclNum;

    /* Increment the reference counts */

    varDsc->incRefCnts(lvaMarkRefsWeight, this);
  
    if (lvaVarAddrExposed(lclNum))
        varDsc->lvIsBoolean = false;

    if  (tree->gtOper == GT_LCL_FLD)
    {
#if ASSERTION_PROP
        // variables that have uses inside a GT_LCL_FLD 
        // cause problems, so we will disqualify them here
        varDsc->lvaDisqualifyVar();
#endif // ASSERTION_PROP
        return;
    }

#if ASSERTION_PROP
    /* Exclude the normal entry block */
    if (fgDomsComputed &&
        (lvaMarkRefsCurBlock->bbNum != 1) &&
        lvaMarkRefsCurBlock->bbIDom != NULL)
    {
        // If any entry block except the normal entry block dominates the block, then mark the local with the lvVolatileHint flag.

        if (BlockSetOps::MayBeUninit(lvaMarkRefsCurBlock->bbDoms))
        {
            // Lazy init (If a block is not dominated by any other block, we'll redo this every time, but it'll be fast)
            BlockSetOps::AssignNoCopy(this, lvaMarkRefsCurBlock->bbDoms, fgGetDominatorSet(lvaMarkRefsCurBlock));
            BlockSetOps::RemoveElemD(this, lvaMarkRefsCurBlock->bbDoms, fgFirstBB->bbNum);
        }
        assert(fgEnterBlksSetValid);
        if (!BlockSetOps::IsEmptyIntersection(this, lvaMarkRefsCurBlock->bbDoms, fgEnterBlks))
        {
            varDsc->lvVolatileHint = 1;
        }
    }

    /* Record if the variable has a single def or not */

    if (!varDsc->lvDisqualify)    // If this variable is already disqualified we can skip this
    {
        if  (tree->gtFlags & GTF_VAR_DEF)    // Is this is a def of our variable
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
            if ((varDsc->lvSingleDef == true)    ||
                (info.compInitMem == true)       ||
                (tree->gtFlags & GTF_COLON_COND) ||
                (tree->gtFlags & GTF_VAR_USEASG)   )
            {
                varDsc->lvaDisqualifyVar();
            }
            else 
            {
                varDsc->lvSingleDef   = true;
                varDsc->lvDefStmt     = lvaMarkRefsCurStmt;
            }
        }
        else  // otherwise this is a ref of our variable
        {
            if (BlockSetOps::MayBeUninit(varDsc->lvRefBlks))
            {
                // Lazy initialization
                BlockSetOps::AssignNoCopy(this, varDsc->lvRefBlks, BlockSetOps::MakeEmpty(this));
            }
            BlockSetOps::AddElemD(this, varDsc->lvRefBlks, lvaMarkRefsCurBlock->bbNum);
        }
    }
#endif // ASSERTION_PROP

    bool allowStructs = false;
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    // On System V the type of the var could be a struct type.
    allowStructs = varTypeIsStruct(varDsc);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    /* Variables must be used as the same type throughout the method */
    noway_assert(tiVerificationNeeded   ||
           varDsc->lvType == TYP_UNDEF  ||  tree->gtType == TYP_UNKNOWN    ||
           allowStructs ||
           genActualType(varDsc->TypeGet()) == genActualType(tree->gtType) ||
           (tree->gtType == TYP_BYREF && varDsc->TypeGet() == TYP_I_IMPL)  ||
           (tree->gtType == TYP_I_IMPL && varDsc->TypeGet() == TYP_BYREF)  ||
           (tree->gtFlags & GTF_VAR_CAST) ||
           varTypeIsFloating(varDsc->TypeGet()) && varTypeIsFloating(tree->gtType));

    /* Remember the type of the reference */

    if (tree->gtType == TYP_UNKNOWN || varDsc->lvType == TYP_UNDEF)
    {
        varDsc->lvType = tree->gtType;
        noway_assert(genActualType(varDsc->TypeGet()) == tree->gtType); // no truncation
    }

#ifdef DEBUG
    if  (tree->gtFlags & GTF_VAR_CAST)
    {
        // it should never be bigger than the variable slot

        // Trees don't store the full information about structs
        // so we can't check them.
        if (tree->TypeGet() != TYP_STRUCT)
        {
            unsigned treeSize = genTypeSize(tree->TypeGet());
            unsigned varSize  = genTypeSize(varDsc->TypeGet());
            if (varDsc->TypeGet() == TYP_STRUCT)
                varSize = varDsc->lvSize();

            assert(treeSize <= varSize);
        }
    }
#endif
}


/*****************************************************************************
 *
 *  Helper passed to Compiler::fgWalkTreePre() to do variable ref marking.
 */

/* static */
Compiler::fgWalkResult  Compiler::lvaMarkLclRefsCallback(GenTreePtr *pTree, fgWalkData *data)
{
    data->compiler->lvaMarkLclRefs(*pTree);

    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Update the local variable reference counts for one basic block
 */

void                Compiler::lvaMarkLocalVars(BasicBlock * block)
{
#if ASSERTION_PROP
    lvaMarkRefsCurBlock = block;
#endif
    lvaMarkRefsWeight   = block->getBBWeight(this); 

#ifdef DEBUG
    if (verbose)
        printf("\n*** marking local variables in block BB%02u (weight=%s)\n",
               block->bbNum, refCntWtd2str(lvaMarkRefsWeight));
#endif

#if JIT_FEATURE_SSA_SKIP_DEFS
    for (GenTreePtr tree = block->FirstNonPhiDef(); tree; tree = tree->gtNext)
#else
    for (GenTreePtr tree = block->bbTreeList; tree; tree = tree->gtNext)
#endif
    {
        noway_assert(tree->gtOper == GT_STMT);
        
#if ASSERTION_PROP
        lvaMarkRefsCurStmt = tree;
#endif

#ifdef DEBUG
        if (verbose)
            gtDispTree(tree);
#endif

        fgWalkTreePre(&tree->gtStmt.gtStmtExpr, 
                      Compiler::lvaMarkLclRefsCallback, 
                      (void *) this, 
                      false);
    }
}

/*****************************************************************************
 *
 *  Create the local variable table and compute local variable reference
 *  counts.
 */

void                Compiler::lvaMarkLocalVars()
{

#ifdef DEBUG
    if (verbose)
        printf("\n*************** In lvaMarkLocalVars()");
#endif


#if INLINE_NDIRECT

    /* If there is a call to an unmanaged target, we already grabbed a
       local slot for the current thread control block.
     */

    if (info.compCallUnmanaged != 0)
    {
        noway_assert(info.compLvFrameListRoot >= info.compLocalsCount &&
                     info.compLvFrameListRoot <  lvaCount);

        lvaTable[info.compLvFrameListRoot].lvType       = TYP_I_IMPL;

        /* Set the refCnt, it is used in the prolog and return block(s) */

        lvaTable[info.compLvFrameListRoot].lvRefCnt     = 2;
        lvaTable[info.compLvFrameListRoot].lvRefCntWtd  = 2 * BB_UNITY_WEIGHT;        
    }
#endif

    lvaAllocOutgoingArgSpace();

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

        lvaShadowSPslotsVar = lvaGrabTempWithImplicitUse(false DEBUGARG("lvaShadowSPslotsVar"));
        LclVarDsc * shadowSPslotsVar = &lvaTable[lvaShadowSPslotsVar];
        shadowSPslotsVar->lvType = TYP_BLK;
        shadowSPslotsVar->lvExactSize = (slotsNeeded * TARGET_POINTER_SIZE);
    }

#endif // !FEATURE_EH_FUNCLETS

#if FEATURE_EH_FUNCLETS
    if (ehNeedsPSPSym())
    {
        lvaPSPSym = lvaGrabTempWithImplicitUse(false DEBUGARG("PSPSym"));
        LclVarDsc * lclPSPSym = &lvaTable[lvaPSPSym];
        lclPSPSym->lvType = TYP_I_IMPL;
    }
#endif // FEATURE_EH_FUNCLETS

    if (compLocallocUsed)
    {
        lvaLocAllocSPvar = lvaGrabTempWithImplicitUse(false DEBUGARG("LocAllocSPvar"));
        LclVarDsc * locAllocSPvar = &lvaTable[lvaLocAllocSPvar];
        locAllocSPvar->lvType = TYP_I_IMPL;
    }
    
    BasicBlock *    block;

#if defined(DEBUGGING_SUPPORT) || defined(DEBUG)

    // Assign slot numbers to all variables.
    // If compiler generated local variables, slot numbers will be
    // invalid (out of range of info.compVarScopes).

    // Also have to check if variable was not reallocated to another
    // slot in which case we have to register the original slot #.

    // We don't need to do this for IL, but this keeps lvSlotNum consistent.

#ifndef DEBUG
    if (opts.compScopeInfo && (info.compVarScopesCount > 0))
#endif
    {
        unsigned        lclNum;
        LclVarDsc *     varDsc;

        for (lclNum = 0, varDsc = lvaTable;
             lclNum < lvaCount;
             lclNum++  , varDsc++)
        {
            varDsc->lvSlotNum = lclNum;
        }
    }

#endif // defined(DEBUGGING_SUPPORT) || defined(DEBUG)

    /* Mark all local variable references */
    
    lvaRefCountingStarted = true;
    for (block = fgFirstBB;
         block;
         block = block->bbNext)
    {
        lvaMarkLocalVars(block);
    }

    /*  For incoming register arguments, if there are references in the body
     *  then we will have to copy them to the final home in the prolog
     *  This counts as an extra reference with a weight of 2
     */

    unsigned        lclNum;
    LclVarDsc *     varDsc;

    for (lclNum = 0, varDsc = lvaTable;
            lclNum < lvaCount;
            lclNum++  , varDsc++)
    {
        if (lclNum >= info.compArgsCount)
            break;  // early exit for loop

        if ((varDsc->lvIsRegArg) && (varDsc->lvRefCnt > 0))
        {
            // Fix 388376 ARM JitStress WP7
            varDsc->incRefCnts(BB_UNITY_WEIGHT, this); 
            varDsc->incRefCnts(BB_UNITY_WEIGHT, this);  
        }
    }

#if ASSERTION_PROP
    if  (!opts.MinOpts() && !opts.compDbgCode)
    {
        // Note: optAddCopies() depends on lvaRefBlks, which is set in lvaMarkLocalVars(BasicBlock*), called above.
        optAddCopies();
    }
#endif

    if (lvaKeepAliveAndReportThis() && lvaTable[0].lvRefCnt == 0)
        lvaTable[0].lvRefCnt = 1;
    // This isn't strictly needed as we will make a copy of the param-type-arg
    // in the prolog. However, this ensures that the LclVarDsc corresponding to
    // info.compTypeCtxtArg is valid.
    else if (lvaReportParamTypeArg() && lvaTable[info.compTypeCtxtArg].lvRefCnt == 0)
        lvaTable[info.compTypeCtxtArg].lvRefCnt = 1;

    lvaLocalVarRefCounted = true;
    lvaRefCountingStarted = false;
    
    lvaSortByRefCount();
}

void Compiler::lvaAllocOutgoingArgSpace()
{
#if FEATURE_FIXED_OUT_ARGS

    // Setup the outgoing argument region, in case we end up using it later

    if (lvaOutgoingArgSpaceVar == BAD_VAR_NUM)
    {
        lvaOutgoingArgSpaceVar = lvaGrabTemp(false DEBUGARG("OutgoingArgSpace"));

        lvaTable[lvaOutgoingArgSpaceVar].lvType = TYP_LCLBLK;

        /* Set the refCnts */

        lvaTable[lvaOutgoingArgSpaceVar].lvRefCnt     = 1;
        lvaTable[lvaOutgoingArgSpaceVar].lvRefCntWtd  = BB_UNITY_WEIGHT;

#if defined(PROFILING_SUPPORTED) && defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI) // No 4 slots for outgoing params on System V.
        // If we are generating profiling Enter/Leave/TailCall hooks, make sure
        // that outgoing arg space size is minimum 4 slots.  This will ensure
        // that even methods without any calls will have 4-slot outgoing arg area.
        if (compIsProfilerHookNeeded() && (lvaOutgoingArgSpaceSize == 0))
        {
            lvaOutgoingArgSpaceSize = 4 * REGSIZE_BYTES;            
        }
#endif // PROFILING_SUPPORTED && _TARGET_AMD64_ && !UNIX_AMD64_ABI
    }

    noway_assert(lvaOutgoingArgSpaceVar >= info.compLocalsCount &&
                 lvaOutgoingArgSpaceVar <  lvaCount);

#endif // FEATURE_FIXED_OUT_ARGS
}

inline void         Compiler::lvaIncrementFrameSize(unsigned size)
{
    if (size > MAX_FrameSize || compLclFrameSize + size > MAX_FrameSize)
        BADCODE("Frame size overflow");

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

#ifndef LEGACY_BACKEND
    if (lvaDoneFrameLayout >= REGALLOC_FRAME_LAYOUT)
    {
        result = tmpSize;
    }
    else
    {
        result = MAX_SPILL_TEMP_SIZE;
    }
#else // LEGACY_BACKEND
    if (lvaDoneFrameLayout >= FINAL_FRAME_LAYOUT)
    {
        result = tmpSize;
    }
    else
    {
        if (lvaDoneFrameLayout >= REGALLOC_FRAME_LAYOUT)
        {
            unsigned maxTmpSize = sizeof(double) + sizeof(int);
            
            maxTmpSize += (tmpDoubleSpillMax * sizeof(double)) +
                          (tmpIntSpillMax    * sizeof(int));
            
            result = maxTmpSize;
        }
        else
        {
            result = MAX_SPILL_TEMP_SIZE;
        }
#ifdef DEBUG
        // When StressRegs is >=1, there can  be a bunch of spills that are not
        // predicted by the predictor (see logic in rsPickReg).  It is very hard
        // to teach the predictor about the behavior of rsPickReg for StressRegs >= 1,
        // so instead let's make MaxTmpSize large enough so that we won't be wrong.

        if (codeGen->regSet.rsStressRegs() >= 1)
        {
            result += (REG_TMP_ORDER_COUNT * REGSIZE_BYTES);
        }
#endif // DEBUG
    }
#endif // LEGACY_BACKEND
    return result;
}

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
 *  TODO-ARM64-NYI: this is preliminary (copied from ARM and modified), and needs to be reviewed.
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

void                Compiler::lvaAssignFrameOffsets(FrameLayoutState curState)
{
    noway_assert(lvaDoneFrameLayout < curState);

    lvaDoneFrameLayout = curState;

#ifdef DEBUG
    if  (verbose)
    {

        printf("*************** In lvaAssignFrameOffsets");
        if (curState == INITIAL_FRAME_LAYOUT)
            printf("(INITIAL_FRAME_LAYOUT)");
        else if (curState == PRE_REGALLOC_FRAME_LAYOUT)
            printf("(PRE_REGALLOC_FRAME_LAYOUT)");
        else if (curState == REGALLOC_FRAME_LAYOUT)
            printf("(REGALLOC_FRAME_LAYOUT)");
        else if (curState == TENTATIVE_FRAME_LAYOUT)
            printf("(TENTATIVE_FRAME_LAYOUT)");
        else if (curState == FINAL_FRAME_LAYOUT)
            printf("(FINAL_FRAME_LAYOUT)");
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
    // The delta to be added to virtual offset to adjust it relative to frame pointer or SP
    int delta = 0;

#ifdef _TARGET_XARCH_
    delta += REGSIZE_BYTES;                            // pushed PC (return address) for x86/x64

    if (codeGen->doubleAlignOrFramePointerUsed())
        delta += REGSIZE_BYTES;                        // pushed EBP (frame pointer)
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
    LclVarDsc * varDsc;
    for (lclNum = 0, varDsc = lvaTable;
         lclNum < lvaCount;
         lclNum++  , varDsc++)
    {
        bool doAssignStkOffs = true;

        // Can't be relative to EBP unless we have an EBP
        noway_assert(!varDsc->lvFramePointerBased || codeGen->doubleAlignOrFramePointerUsed());

        // Is this a non-param promoted struct field?
        //   if so then set doAssignStkOffs to false.
        //
        if (varDsc->lvIsStructField && !varDsc->lvIsParam)
        {
            LclVarDsc *      parentvarDsc  = &lvaTable[varDsc->lvParentLcl];
            lvaPromotionType promotionType = lvaGetPromotionType(parentvarDsc);
            
            if (promotionType == PROMOTION_TYPE_DEPENDENT)
            {
                doAssignStkOffs = false;  // Assigned later in lvaAssignFrameOffsetsToPromotedStructs()
            }
        }

        if (!varDsc->lvOnFrame)
        {
            if (!varDsc->lvIsParam 
#if !defined(_TARGET_AMD64_)
                || (varDsc->lvIsRegArg
#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
                && compIsProfilerHookNeeded() && !lvaIsPreSpilled(lclNum, codeGen->regSet.rsMaskPreSpillRegs(false))   // We need assign stack offsets for prespilled arguments
#endif
                )
#endif // !defined(_TARGET_AMD64_)
                )
            {
                doAssignStkOffs = false;   // Not on frame or an incomming stack arg
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

                    varDsc->lvStkOffs += (2 * sizeof(void *)); // return address and pushed EBP

                    noway_assert(varDsc->lvStkOffs >= FIRST_ARG_STACK_OFFS);
                }
            }
#endif
            // On System V environments the stkOffs could be 0 for params passed in registers.
            assert(codeGen->isFramePointerUsed() || varDsc->lvStkOffs >= 0); // Only EBP relative references can have negative offsets
        }
    }

    assert(tmpAllFree());
    for (TempDsc* temp = tmpListBeg();
                  temp != nullptr;
                  temp = tmpListNxt(temp))
    {
        temp->tdAdjustTempOffs(delta);
    }

    lvaCachedGenericContextArgOffs += delta;

#if FEATURE_EH_FUNCLETS && defined(_TARGET_AMD64_)
    if (ehNeedsPSPSym())
    {
        assert(lvaPSPSym != BAD_VAR_NUM);
        varDsc = &lvaTable[lvaPSPSym];
        varDsc->lvFramePointerBased = false;
        varDsc->lvMustInit = false;
        varDsc->lvStkOffs = lvaLclSize(lvaOutgoingArgSpaceVar); // put the PSPSym just above the outgoing arg space
    }
#endif

#if FEATURE_FIXED_OUT_ARGS

    if (lvaOutgoingArgSpaceVar != BAD_VAR_NUM) 
    {
        varDsc = &lvaTable[lvaOutgoingArgSpaceVar];
        varDsc->lvStkOffs = 0;
        varDsc->lvFramePointerBased = false;
        varDsc->lvMustInit = false;
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

#ifndef LEGACY_BACKEND
/*****************************************************************************
 *  lvaUpdateArgsWithInitialReg() : For each argument variable descriptor, update
 *  its current register with the initial register as assigned by LSRA.
 */
void Compiler::lvaUpdateArgsWithInitialReg()
{
    if (!compLSRADone)
        return;

    for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
    {
        LclVarDsc* varDsc = lvaTable + lclNum;

        if (varDsc->lvPromotedStruct())
        {
            noway_assert(varDsc->lvFieldCnt == 1);  // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc = lvaTable + fieldVarNum;
        }

        noway_assert(varDsc->lvIsParam);

        if (varDsc->lvIsRegCandidate())
        {
            if (varTypeIsMultiReg(varDsc))
            {
                regPairNo initialRegPair = varDsc->lvArgInitRegPair;
                varDsc->lvRegNum   = genRegPairLo(initialRegPair);
                varDsc->lvOtherReg = genRegPairHi(initialRegPair);
            }
            else
            {
                varDsc->lvRegNum = varDsc->lvArgInitReg;
            }
        }
    }
}
#endif // !LEGACY_BACKEND

/*****************************************************************************
 *  lvaAssignVirtualFrameOffsetsToArgs() : Assign virtual stack offsets to the
 *  arguments, and implicit arguments (this ptr, return buffer, generics,
 *  and varargs).
 */
void Compiler::lvaAssignVirtualFrameOffsetsToArgs()
{
    unsigned lclNum     = 0;
    int      argOffs    = 0;
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
        argOffs  = compArgSize;

    /* Update the argOffs to reflect arguments that are passed in registers */

    noway_assert(codeGen->intRegState.rsCalleeRegArgNum <= MAX_REG_ARG);
    noway_assert(compArgSize >= codeGen->intRegState.rsCalleeRegArgNum * sizeof(void *));

#ifdef _TARGET_X86_
    argOffs -= codeGen->intRegState.rsCalleeRegArgNum * sizeof(void *);
#endif

#ifndef LEGACY_BACKEND
    // Update the arg initial register locations.
    lvaUpdateArgsWithInitialReg();
#endif // !LEGACY_BACKEND

    /* Is there a "this" argument? */

    if  (!info.compIsStatic)
    {
        noway_assert(lclNum == info.compThisArg);
#ifndef _TARGET_X86_
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
#endif // _TARGET_X86_
        lclNum++;
    }

    /* if we have a hidden buffer parameter, that comes here */

    if (info.compRetBuffArg != BAD_VAR_NUM)
    {
        noway_assert(lclNum == info.compRetBuffArg);
        noway_assert(lvaTable[lclNum].lvIsRegArg);
#ifndef _TARGET_X86_
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum, REGSIZE_BYTES, argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
#endif // _TARGET_X86_
        lclNum++;
    }

#if USER_ARGS_COME_LAST

    //@GENERICS: extra argument for instantiation info 
    if (info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        noway_assert(lclNum == (unsigned)info.compTypeCtxtArg);
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, sizeof(void *), argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
    }

    if (info.compIsVarArgs)
    {
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, sizeof(void *), argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
    }

#endif // USER_ARGS_COME_LAST

    CORINFO_ARG_LIST_HANDLE argLst = info.compMethodInfo->args.args;
    unsigned argSigLen = info.compMethodInfo->args.numArgs;

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
    regMaskTP tempMask = RBM_NONE;
    for (unsigned i = 0, preSpillLclNum = lclNum; i < argSigLen; ++i, ++preSpillLclNum)
    {
        if (lvaIsPreSpilled(preSpillLclNum, preSpillMask))
        {
            unsigned argSize = eeGetArgSize(argLst, &info.compMethodInfo->args);
            argOffs = lvaAssignVirtualFrameOffsetToArg(
                preSpillLclNum,
                argSize,
                argOffs);
            argLcls++;

            // Early out if we can. If size is 8 and base reg is 2, then the mask is 0x1100
            tempMask |= ((((1 << (roundUp(argSize) / REGSIZE_BYTES))) - 1) << lvaTable[preSpillLclNum].lvArgReg);
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
            argOffs = lvaAssignVirtualFrameOffsetToArg(
                stkLclNum,
                eeGetArgSize(argLst, &info.compMethodInfo->args),
                argOffs);
            argLcls++;
        }
        argLst = info.compCompHnd->getArgNext(argLst);
    }

    lclNum += argLcls;
#else // !_TARGET_ARM_
    for (unsigned i = 0; i < argSigLen; i++)
    {
        unsigned argumentSize = eeGetArgSize(argLst, &info.compMethodInfo->args);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        // On the stack frame the homed arg always takes a full number of slots
        // for proper stack alignment. Make sure the real struct size is properly rounded up.
        argumentSize = (unsigned)roundUp(argumentSize, TARGET_POINTER_SIZE);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++,
            argumentSize,
            argOffs
            UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
        argLst = info.compCompHnd->getArgNext(argLst);
    }
#endif // !_TARGET_ARM_

#if !USER_ARGS_COME_LAST

    //@GENERICS: extra argument for instantiation info 
    if (info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        noway_assert(lclNum == (unsigned)info.compTypeCtxtArg);
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, sizeof(void *), argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
    }

    if (info.compIsVarArgs)
    {
        argOffs = lvaAssignVirtualFrameOffsetToArg(lclNum++, sizeof(void *), argOffs UNIX_AMD64_ABI_ONLY_ARG(&callerArgOffset));
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
int Compiler::lvaAssignVirtualFrameOffsetToArg(unsigned lclNum, unsigned argSize, int argOffs UNIX_AMD64_ABI_ONLY_ARG(int * callerArgOffset))
{
    noway_assert(lclNum < info.compArgsCount);
    noway_assert(argSize);

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_L2R)
        argOffs -= argSize;

    unsigned fieldVarNum = BAD_VAR_NUM;

    noway_assert(lclNum < lvaCount);
    LclVarDsc * varDsc = lvaTable + lclNum;

    if (varDsc->lvPromotedStruct())
    {
        noway_assert(varDsc->lvFieldCnt == 1);  // We only handle one field here
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
        // For Windows AMD64 there are 4 slots for the register passed arguments on the top of the caller's stack. This is where they are always homed.
        // So, they can be accessed with positive offset.
        // On System V platforms, if the RA decides to home a register passed arg on the stack,
        // it creates a stack location on the callee stack (like any other local var.) In such a case, the register passed, stack homed arguments
        // are accessed using negative offsets and the stack passed arguments are accessed using positive offset (from the caller's stack.)
        // For  System V platforms if there is no frame pointer the caller stack parameter offset should include the callee allocated space.
        // If frame register is used, the callee allocated space should not be included for accessing the caller stack parameters.
        // The last two requirements are met in lvaFixVirtualFrameOffsets method, which fixes the offsets, based on frame pointer existence, 
        // existence of alloca instructions, ret address pushed, ets.

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
            noway_assert(varDsc->lvFieldCnt == 1);  // We only handle one field here

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
        argOffs += argSize;

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
int Compiler::lvaAssignVirtualFrameOffsetToArg(unsigned lclNum, unsigned argSize, int argOffs UNIX_AMD64_ABI_ONLY_ARG(int * callerArgOffset))
{
    noway_assert(lclNum < info.compArgsCount);
    noway_assert(argSize);

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_L2R)
        argOffs -= argSize;

    unsigned fieldVarNum = BAD_VAR_NUM;

    noway_assert(lclNum < lvaCount);
    LclVarDsc * varDsc = lvaTable + lclNum;

    if (varDsc->lvPromotedStruct())
    {
        noway_assert(varDsc->lvFieldCnt == 1);  // We only handle one field here
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

#if !defined(_TARGET_ARMARCH_)
        // TODO: Remove this noway_assert and replace occurrences of sizeof(void *) with argSize
        // Also investigate why we are incrementing argOffs for X86 as this seems incorrect
        // 
#if DEBUG
        noway_assert(argSize == sizeof(void *));
#endif // DEBUG
#endif

#if defined(_TARGET_X86_)
        argOffs += sizeof(void *);
#elif defined(_TARGET_AMD64_)
        // Register arguments on AMD64 also takes stack space. (in the backing store)
        varDsc->lvStkOffs = argOffs;
        argOffs += sizeof(void *);
#elif defined(_TARGET_ARM64_)
        // Register arguments on ARM64 only take stack space when they have a frame home.
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
                    if (!BitsBetween(codeGen->regSet.rsMaskPreSpillRegArg, regMask, codeGen->regSet.rsMaskPreSpillAlign))
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
                // Consider argOffs as if it accounts for number of prespilled registers before the current register.
                // In the above example, for r2, it is r1 that is prespilled, but since r1 is accounted for by argOffs
                // being 4, there should have been no skipping. Instead, if we didn't assign r1 to any variable, then
                // argOffs would still be 0 which implies it is not accounting for r1, equivalently r1 is skipped.
                //
                // If prevRegsSize is unaccounted for by a corresponding argOffs, we must have skipped a register.
                int prevRegsSize = genCountBits(codeGen->regSet.rsMaskPreSpillRegArg & (regMask - 1)) * TARGET_POINTER_SIZE;
                if (argOffs < prevRegsSize)
                {
                    // We must align up the argOffset to a multiple of 8 to account for skipped registers.
                    argOffs = roundUp(argOffs, 2 * TARGET_POINTER_SIZE);
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

#ifdef PROFILING_SUPPORTED
            // On Arm under profiler, r0-r3 are always prespilled on stack.
            // It is possible to have methods that accept only HFAs as parameters e.g. Signature(struct hfa1, struct hfa2)
            // In which case hfa1 and hfa2 will be en-registered in co-processor registers and will have an argument offset
            // less than size of preSpill.
            //
            // For this reason the following conditions are asserted when not under profiler.
            if (!compIsProfilerHookNeeded())
#endif
            {
                bool cond = (info.compIsVarArgs &&
                    // Does cur stk arg require double alignment?
                    ((varDsc->lvType == TYP_STRUCT && varDsc->lvStructDoubleAlign) ||
                    (varDsc->lvType == TYP_DOUBLE) ||
                    (varDsc->lvType == TYP_LONG))
                    ) ||
                    // Did first reg arg require alignment?
                    (codeGen->regSet.rsMaskPreSpillAlign & genRegMask(REG_ARG_LAST));

                noway_assert(cond);
                noway_assert(sizeofPreSpillRegArgs <= argOffs + TARGET_POINTER_SIZE); // at most one register of alignment
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
            argOffs = roundUp(argOffsWithoutPreSpillRegArgs, 2 * TARGET_POINTER_SIZE) + sizeofPreSpillRegArgs;
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
#if !defined(_TARGET_64BIT_)
    if ((varDsc->TypeGet() == TYP_LONG) && varDsc->lvPromoted)
    {
        noway_assert(varDsc->lvFieldCnt == 2);
        fieldVarNum = varDsc->lvFieldLclStart;
        lvaTable[fieldVarNum].lvStkOffs = varDsc->lvStkOffs;
        lvaTable[fieldVarNum + 1].lvStkOffs = varDsc->lvStkOffs + genTypeSize(TYP_INT);
    }
    else
#endif // !defined(_TARGET_64BIT_)
        if (varDsc->lvPromotedStruct())
        {
            lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

            if (promotionType == PROMOTION_TYPE_DEPENDENT)
            {
                noway_assert(varDsc->lvFieldCnt == 1);  // We only handle one field here

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
        argOffs += argSize;

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
        codeGen->setFramePointerUsed(codeGen->isFramePointerRequired());

#ifdef _TARGET_XARCH_
    // On x86/amd64, the return address has already been pushed by the call instruction in the caller.
    stkOffs -= sizeof(void *); // return address;

    // TODO-AMD64-CQ: for X64 eventually this should be pushed with all the other
    // calleeregs.  When you fix this, you'll also need to fix
    // the assert at the bottom of this method
    if (codeGen->doubleAlignOrFramePointerUsed())
    {
        stkOffs -= REGSIZE_BYTES;
    }
#endif //_TARGET_XARCH_

    int preSpillSize = 0;
    bool mustDoubleAlign = false;

#ifdef _TARGET_ARM_
    mustDoubleAlign = true;  
    preSpillSize = genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
#else // !_TARGET_ARM_
 #if DOUBLE_ALIGN
    if (genDoubleAlign())
    {
        mustDoubleAlign = true;     // X86 only
    }
 #endif
#endif // !_TARGET_ARM_

#ifdef _TARGET_ARM64_
    // If the frame pointer is used, then we'll save FP/LR at the bottom of the stack.
    // Otherwise, we won't store FP, and we'll store LR at the top, with the other callee-save
    // registers (if any).
    if (isFramePointerUsed())
    {
        // Subtract off FP and LR.
        assert(compCalleeRegsPushed >= 2);
        stkOffs -= (compCalleeRegsPushed - 2) * REGSIZE_BYTES;
    }
    else
    {
        stkOffs -= compCalleeRegsPushed * REGSIZE_BYTES;
    }
#else // !_TARGET_ARM64_
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
            printf("\nAdding VS quirk stack padding of %d bytes between save-reg area and locals\n", compVSQuirkStackPaddingNeeded);
        }
#endif // DEBUG

        stkOffs -= compVSQuirkStackPaddingNeeded;
        lvaIncrementFrameSize(compVSQuirkStackPaddingNeeded);
    }
#endif //_TARGET_AMD64_

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARMARCH_)
    if (ehNeedsPSPSym())
    {
        // On ARM/ARM64, if we need a PSPSym, allocate it first, before anything else, including
        // padding (so we can avoid computing the same padding in the funclet
        // frame). Note that there is no special padding requirement for the PSPSym.
        noway_assert(codeGen->isFramePointerUsed());          // We need an explicit frame pointer
        assert(lvaPSPSym != BAD_VAR_NUM);   // We should have created the PSPSym variable
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
            if (((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) != 0)
            {
                lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                stkOffs -= TARGET_POINTER_SIZE;
            }
            // We should now have a double-aligned (stkOffs+preSpillSize) 
            noway_assert(((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) == 0);
        }
    }

    if (lvaMonAcquired != BAD_VAR_NUM)
    {
        // This var must go first, in what is called the 'frame header' for EnC so that it is 
        // preserved when remapping occurs.  See vm\eetwain.cpp for detailed comment specifying frame
        // layout requirements for EnC to work.
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaMonAcquired, lvaLclSize(lvaMonAcquired), stkOffs);
    } 

    if  (opts.compNeedSecurityCheck)
    {
        /* This can't work without an explicit frame, so make sure */
#ifdef JIT32_GCENCODER
        noway_assert(codeGen->isFramePointerUsed());
#endif
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaSecurityObject, TARGET_POINTER_SIZE, stkOffs);
    }

    if (compLocallocUsed)
    {
#ifdef JIT32_GCENCODER
        noway_assert(codeGen->isFramePointerUsed()); // else offsets of locals of frameless methods will be incorrect
#endif
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaLocAllocSPvar, TARGET_POINTER_SIZE, stkOffs);
    }

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

    enum Allocation{
        ALLOC_NON_PTRS                  = 0x1,         // assign offsets to non-ptr
        ALLOC_PTRS                      = 0x2,         // Second pass, assign offsets to tracked ptrs
        ALLOC_UNSAFE_BUFFERS            = 0x4,
        ALLOC_UNSAFE_BUFFERS_WITH_PTRS  = 0x8
    };
    UINT  alloc_order[5];
    
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

    bool    tempsAllocated = false;

#ifdef _TARGET_ARM_
    // On ARM, SP based offsets use smaller encoding. Since temps are relatively
    // rarer than lcl usage, allocate them farther from SP.
    if (!opts.MinOpts() && !compLocallocUsed)
#else
    if (lvaTempsHaveLargerOffsetThanVars() && !codeGen->isFramePointerUsed())
#endif
    {
        // Because we want the temps to have a larger offset than locals
        // and we're not using a frame pointer, we have to place the temps
        // above the vars.  Otherwise we place them after the vars (at the
        // bottom of the frame).
        noway_assert(!tempsAllocated);
        stkOffs = lvaAllocateTemps(stkOffs, mustDoubleAlign);
        tempsAllocated = true;
    }

    alloc_order[cur++] = ALLOC_NON_PTRS;

    if  (opts.compDbgEnC)
    {
        /* We will use just one pass, and assign offsets to all variables */
        alloc_order[cur-1] |= ALLOC_PTRS;
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

    noway_assert(cur < sizeof(alloc_order)/sizeof(alloc_order[0]));
    
    // Force first pass to happen
    UINT assignMore = 0xFFFFFFFF;
    bool have_LclVarDoubleAlign = false;

    for (cur = 0; alloc_order[cur]; cur++)
    {
        if ((assignMore & alloc_order[cur]) == 0)
            continue;
            
        assignMore = 0;

        unsigned lclNum;
        LclVarDsc * varDsc;

        for (lclNum = 0, varDsc = lvaTable;
             lclNum < lvaCount;
             lclNum++  , varDsc++)
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

            if (varDsc->lvRegister &&
                (lvaDoneFrameLayout == REGALLOC_FRAME_LAYOUT) && 
                ((varDsc->TypeGet() != TYP_LONG) || (varDsc->lvOtherReg != REG_STK)))
            {
                allocateOnFrame = false;
            }

            /* Ignore variables that are not on the stack frame */

            if  (!allocateOnFrame)
            {
                /* For EnC, all variables have to be allocated space on the
                   stack, even though they may actually be enregistered. This
                   way, the frame layout can be directly inferred from the
                   locals-sig.
                 */

                if (!opts.compDbgEnC)
                    continue;
                else if (lclNum >= info.compLocalsCount) // ignore temps for EnC
                    continue;
            } 
            else if (lvaGSSecurityCookie == lclNum && getNeedsGSSecurityCookie())
            {
                continue;   // This is allocated outside of this loop.
            }

            // These need to be located as the very first variables (highest memory address)
            // and so they have already been assigned an offset
            if (
#if FEATURE_EH_FUNCLETS
                lclNum == lvaPSPSym ||
#else
                lclNum == lvaShadowSPslotsVar ||
#endif // FEATURE_EH_FUNCLETS
                lclNum == lvaLocAllocSPvar ||
                lclNum == lvaSecurityObject)
            {
                assert(varDsc->lvStkOffs != BAD_STK_OFFS);
                continue;
            }

            if (lclNum == lvaMonAcquired)
                continue;

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

            if  (varDsc->lvIsParam)
            {
#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)

                // On Windows AMD64 we can use the caller-reserved stack area that is already setup 
                assert(varDsc->lvStkOffs != BAD_STK_OFFS);
                continue;

#else // !_TARGET_AMD64_

                //  A register argument that is not enregistered ends up as
                //  a local variable which will need stack frame space.
                //
                if  (!varDsc->lvIsRegArg)
                    continue;

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

            if (mustDoubleAlign && (
                varDsc->lvType == TYP_DOUBLE       // Align doubles for ARM and x86
#ifdef _TARGET_ARM_
                || varDsc->lvType == TYP_LONG      // Align longs for ARM
#endif
#ifndef _TARGET_64BIT_
                || varDsc->lvStructDoubleAlign     // Align when lvStructDoubleAlign is true
#endif // !_TARGET_64BIT_
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
                    if (((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) != 0)
                    {
                        lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                        stkOffs -= TARGET_POINTER_SIZE;
                    }

                    // We should now have a double-aligned (stkOffs+preSpillSize) 
                    noway_assert(((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) == 0);
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
                noway_assert(varDsc->lvFieldCnt == 1);  // We only handle one field here

                unsigned fieldVarNum = varDsc->lvFieldLclStart;
                lvaTable[fieldVarNum].lvStkOffs = varDsc->lvStkOffs;
            }
#endif
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
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaInlinedPInvokeFrameVar, lvaLclSize(lvaInlinedPInvokeFrameVar), stkOffs);
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
            if (((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) != 0)
            {
                lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                stkOffs -= TARGET_POINTER_SIZE;
            }
            // We should now have a double-aligned (stkOffs+preSpillSize) 
            noway_assert(((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) == 0);
        }
    }

#if FEATURE_EH_FUNCLETS && defined(_TARGET_AMD64_)
    if (ehNeedsPSPSym())
    {
        // On AMD64, if we need a PSPSym, allocate it last, immediately above the outgoing argument
        // space. Any padding will be higher on the stack than this
        // (including the padding added by lvaAlignFrame()). Here, we will give it an offset, but really we
        // will set its value at the end of lvaFixVirtualFrameOffsets, as for lvaOutgoingArgSpace.
        // There is a comment above that the P/Invoke vars "need to be assigned last". We are ignoring
        // that here (TODO-AMD64-Bug?: is that ok? JIT64 does things this way).
        noway_assert(codeGen->isFramePointerUsed());  // We need an explicit frame pointer
        assert(lvaPSPSym != BAD_VAR_NUM);   // We should have created the PSPSym variable
        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaPSPSym, TARGET_POINTER_SIZE, stkOffs);
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_AMD64_)

#ifdef _TARGET_ARM64_
    if (isFramePointerUsed())
    {
        // Create space for saving FP and LR.
        stkOffs -= 2 * REGSIZE_BYTES;
    }
#endif // _TARGET_ARM64_

#if FEATURE_FIXED_OUT_ARGS
    if (lvaOutgoingArgSpaceSize > 0) 
    {
#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI) // No 4 slots for outgoing params on System V.
        noway_assert(lvaOutgoingArgSpaceSize >= (4 * sizeof(void*)));
#endif
        noway_assert((lvaOutgoingArgSpaceSize % sizeof(void*)) == 0);

        // Give it a value so we can avoid asserts in CHK builds.
        // Since this will always use an SP relative offset of zero 
        // at the end of lvaFixVirtualFrameOffsets, it will be set to absolute '0'

        stkOffs = lvaAllocLocalAndSetVirtualOffset(lvaOutgoingArgSpaceVar, lvaLclSize(lvaOutgoingArgSpaceVar), stkOffs);
    }
#endif // FEATURE_FIXED_OUT_ARGS

    // compLclFrameSize equals our negated virtual stack offset minus the pushed registers and return address
    // and the pushed frame pointer register which for some strange reason isn't part of 'compCalleeRegsPushed'.
    int pushedCount = compCalleeRegsPushed;
#ifdef _TARGET_XARCH_
    if (codeGen->doubleAlignOrFramePointerUsed())
        pushedCount += 1;                        // pushed EBP (frame pointer)
    pushedCount += 1;                            // pushed PC (return address)
#endif

    noway_assert(compLclFrameSize == (unsigned)-(stkOffs + (pushedCount * (int) sizeof(void *))));
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
    if ((size >= 8) &&
        ((lvaDoneFrameLayout != FINAL_FRAME_LAYOUT) || 
         ((stkOffs % 8) != 0)
#if defined(FEATURE_SIMD) && ALIGN_SIMD_TYPES
         || lclVarIsSIMDType(lclNum)
#endif
       ))
    {
        // Note that stack offsets are negative
        assert(stkOffs < 0);

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
                    pad = alignment-1;
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
            printf(", size=%d, stkOffs=%c0x%x, pad=%d\n",
                size,
                stkOffs < 0 ? '-' : '+',
                stkOffs < 0 ? -stkOffs : stkOffs,
                pad);
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
        printf(", size=%d, stkOffs=%c0x%x\n",
            size,
            stkOffs < 0 ? '-' : '+',
            stkOffs < 0 ? -stkOffs : stkOffs);
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
    return (regsPushed % (16/REGSIZE_BYTES)) == 0;
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
#ifdef UNIX_AMD64_ABI
    // The compNeedToAlignFrame flag  is indicating if there is a need to align the frame.
    // On AMD64-Windows, if there are calls, 4 slots for the outgoing ars are allocated, except for
    // FastTailCall. This slots makes the frame size non-zero, so alignment logic will be called.
    // On AMD64-Unix, there are no such slots. There is a possibility to have calls in the method with frame size of 0.
    // The frame alignment logic won't kick in. This flags takes care of the AMD64-Unix case by remembering that there
    // are calls and making sure the frame alignment logic is executed.
    bool stackNeedsAlignment = (compLclFrameSize != 0 || opts.compNeedToAlignFrame);
#else // !UNIX_AMD64_ABI
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
    bool regPushedCountAligned = (compCalleeRegsPushed % (16/REGSIZE_BYTES)) == 0;
    bool lclFrameSizeAligned   = (compLclFrameSize % 16) == 0;

    // If this isn't the final frame layout, assume we have to push an extra QWORD
    // Just so the offsets are true upper limits.
    if ((lvaDoneFrameLayout != FINAL_FRAME_LAYOUT) || 
        (regPushedCountAligned != lclFrameSizeAligned))
    {
        lvaIncrementFrameSize(REGSIZE_BYTES);
    }

#elif defined(_TARGET_ARM_)

    // Ensure that stack offsets will be double-aligned by grabbing an unused DWORD if needed.
    //
    bool lclFrameSizeAligned   = (compLclFrameSize % sizeof(double)) == 0;
    bool regPushedCountAligned = ((compCalleeRegsPushed + genCountBits(codeGen->regSet.rsMaskPreSpillRegs(true)))
                                 % (sizeof(double) / sizeof(void *))) == 0;


    if (regPushedCountAligned != lclFrameSizeAligned)
    {
        lvaIncrementFrameSize(sizeof(void *));
    }

#elif defined(_TARGET_X86_)

    if (genDoubleAlign())
    {
        // Double Frame Alignement for x86 is handled in Compiler::lvaAssignVirtualFrameOffsetsToLocals()

        if (compLclFrameSize == 0)
        {
            // This can only happen with JitStress=1 or JitDoubleAlign=2
            lvaIncrementFrameSize(sizeof(void*));
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
    LclVarDsc * varDsc = lvaTable;
    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++, varDsc++)
    {     
        // For promoted struct fields that are params, we will
        // assign their offsets in lvaAssignVirtualFrameOffsetToArg().
        // This is not true for the System V systems since there is no 
        // outgoing args space. Assign the dependently promoted fields properly.
        //
        if (varDsc->lvIsStructField 
#ifndef UNIX_AMD64_ABI
        // For System V platforms there is no outgoing args space. 
        // A register passed struct arg is homed on the stack in a separate local var.
        // The offset of these structs is already calculated in lvaAssignVirtualFrameOffsetToArg methos.
        // Make sure the code below is not executed for these structs and the offset is not changed.
            && !varDsc->lvIsParam
#endif // UNIX_AMD64_ABI
            )
        {
            LclVarDsc *      parentvarDsc  = &lvaTable[varDsc->lvParentLcl];
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
                varDsc->lvStkOffs = parentvarDsc->lvStkOffs + varDsc->lvFldOffset;
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
        bool    assignDone; 
        bool    assignNptr; 
        bool    assignPtrs = true; 

        /* Allocate temps */

        if  (TRACK_GC_TEMP_LIFETIMES)
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

        assert(tmpAllFree());

AGAIN2:

        for (TempDsc* temp = tmpListBeg();
                      temp != nullptr;
                      temp = tmpListNxt(temp))
        {
            var_types       tempType = temp->tdTempType();
            unsigned        size;

            /* Make sure the type is appropriate */

            if  (!assignPtrs &&  varTypeIsGC(tempType))
                continue;
            if  (!assignNptr && !varTypeIsGC(tempType))
                continue;

            size = temp->tdTempSize();

            /* Figure out and record the stack offset of the temp */

            /* Need to align the offset? */

#ifdef  _TARGET_64BIT_
            if (varTypeIsGC(tempType) && ((stkOffs % TARGET_POINTER_SIZE) != 0))
            {
                // Calculate 'pad' as the number of bytes to align up 'stkOffs' to be a multiple of TARGET_POINTER_SIZE
                // In practice this is really just a fancy way of writing 4. (as all stack locations are at least 4-byte aligned)
                // Note stkOffs is always negative, so (stkOffs % TARGET_POINTER_SIZE) yields a negative value.
                //
                int alignPad = (int)AlignmentPad((unsigned)-stkOffs, TARGET_POINTER_SIZE);

                spillTempSize += alignPad;
                lvaIncrementFrameSize(alignPad);
                stkOffs -= alignPad;

                noway_assert((stkOffs % TARGET_POINTER_SIZE) == 0);
            }
#endif

            if (mustDoubleAlign && (tempType == TYP_DOUBLE))       // Align doubles for x86 and ARM
            {
                noway_assert((compLclFrameSize % TARGET_POINTER_SIZE) == 0);

                if (((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) != 0)
                {
                    spillTempSize += TARGET_POINTER_SIZE;
                    lvaIncrementFrameSize(TARGET_POINTER_SIZE);
                    stkOffs -= TARGET_POINTER_SIZE;
                }
                // We should now have a double-aligned (stkOffs+preSpillSize) 
                noway_assert(((stkOffs+preSpillSize) % (2*TARGET_POINTER_SIZE)) == 0);
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

        if  (!assignDone)
        {
            assignNptr = !assignNptr;
            assignPtrs = !assignPtrs;
            assignDone = true;

            goto AGAIN2;
        }
    }
    else  // We haven't run codegen, so there are no Spill temps yet!  
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
 *  Dump the register a local is in right now.
 *  For non-LSRA, this will be the register it is always in. For LSRA, it's only the current
 *  location, since the location changes and it is updated throughout code generation based on
 *  LSRA register assignments.
 */

void   Compiler::lvaDumpRegLocation(unsigned lclNum)
{
    LclVarDsc * varDsc = lvaTable + lclNum;
    var_types type = varDsc->TypeGet();

#if  FEATURE_STACK_FP_X87  
    if (varTypeIsFloating(type))
    {
        printf("fpu stack   ");
    }
    else 
#endif
    if (isRegPairType(type))
    {
        if (!doLSRA()) noway_assert(varDsc->lvRegNum != REG_STK);
        if (doLSRA() && varDsc->lvRegNum == REG_STK)
        {
            /* Hi-only enregistered long */
            int  offset  = varDsc->lvStkOffs;
            printf("%-3s:[%1s0x%02X]",
                   getRegName(varDsc->lvOtherReg),    // hi32
                   (offset < 0 ? "-"     : "+"),
                   (offset < 0 ? -offset : offset));
        }
        else if (varDsc->lvOtherReg != REG_STK)
        {
            /* Fully enregistered long */
            printf("%3s:%-3s    ",
                   getRegName(varDsc->lvOtherReg),  // hi32
                   getRegName(varDsc->lvRegNum));   // lo32
        }
        else
        {
            /* Partially enregistered long */
            int  offset  = varDsc->lvStkOffs+4;
            printf("[%1s0x%02X]:%-3s",
                   (offset < 0 ? "-"     : "+"),
                   (offset < 0 ? -offset : offset),
                   getRegName(varDsc->lvRegNum));    // lo32
        }
    }
#ifdef _TARGET_ARM_
    else if (varDsc->TypeGet() == TYP_DOUBLE)
    {
        printf("%3s:%-3s    ", getRegName(varDsc->lvRegNum), getRegName(varDsc->lvOtherReg));
    }
#endif
    else
    {
        printf("%3s        ", getRegName(varDsc->lvRegNum));
    }
}

/*****************************************************************************
 *
 *  Dump the frame location assigned to a local.
 *  For non-LSRA, this will only be valid if there is no assigned register.
 *  For LSRA, it's the home location, even though the variable doesn't always live
 *  in its home location.
 */

void   Compiler::lvaDumpFrameLocation(unsigned lclNum)
{
    int       offset;
    regNumber baseReg;

#ifdef _TARGET_ARM_
    offset  = lvaFrameAddress(lclNum, compLocallocUsed, &baseReg, 0);
#else
    bool EBPbased;
    offset  = lvaFrameAddress(lclNum, &EBPbased);
    baseReg = EBPbased ? REG_FPBASE : REG_SPBASE;
#endif

    printf("[%2s%1s0x%02X]  ",
           getRegName(baseReg),
           (offset < 0 ? "-"     : "+"),
           (offset < 0 ? -offset : offset));
}

/*****************************************************************************
 *
 *  dump a single lvaTable entry
 */

void   Compiler::lvaDumpEntry(unsigned lclNum, FrameLayoutState curState, size_t refCntWtdWidth)
{
    LclVarDsc * varDsc = lvaTable + lclNum;
    var_types type = varDsc->TypeGet();

    if (curState == INITIAL_FRAME_LAYOUT)
    {
        printf(";  ");
        gtDispLclVar(lclNum);

        printf(" %7s ", varTypeName(type));
        if (genTypeSize(type) == 0)
            printf("(%2d) ", lvaLclSize(lclNum));
    }
    else
    {
        if (varDsc->lvRefCnt == 0)
        {
            // Print this with a special indicator that the variable is unused. Even though the
            // variable itself is unused, it might be a struct that is promoted, so seeing it
            // can be useful when looking at the promoted struct fields. It's also weird to see
            // missing var numbers if these aren't printed.
            printf(";* ");
        }
        else
#if FEATURE_FIXED_OUT_ARGS
        if ((lclNum == lvaOutgoingArgSpaceVar) && (lvaLclSize(lclNum) == 0))
        {
            // Similar to above; print this anyway.
            printf(";# ");
        }
        else
#endif
        {
            printf(";  ");
        }

        gtDispLclVar(lclNum);

        printf("[V%02u", lclNum);
        if (varDsc->lvTracked)      printf(",T%02u]", varDsc->lvVarIndex);
        else                        printf("    ]");

        printf(" (%3u,%*s)",
               varDsc->lvRefCnt,
               (int)refCntWtdWidth,
               refCntWtd2str(varDsc->lvRefCntWtd));

        printf(" %7s ",    varTypeName(type));
        if (genTypeSize(type) == 0)
            printf("(%2d) ", lvaLclSize(lclNum));
        else
            printf(" ->  ");

        // The register or stack location field is 11 characters wide.
        if (varDsc->lvRefCnt == 0)
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
            printf("multi-reg  ");
        }
        else
        {
            // For RyuJIT backend, it might be in a register part of the time, but it will definitely have a stack home location.
            // Otherwise, it's always on the stack.
            if (lvaDoneFrameLayout != NO_FRAME_LAYOUT)
                lvaDumpFrameLocation(lclNum);
        }
    }

#ifdef _TARGET_ARM_
    if (varDsc->lvIsHfaRegArg)
    {
        if (varDsc->lvHfaTypeIsFloat)
        {
            printf(" (enregistered HFA: float) ");
        }
        else
        {
            printf(" (enregistered HFA: double)");
        }
    }
#endif // _TARGET_ARM_

    if (varDsc->lvDoNotEnregister)           
    {
        printf(" do-not-enreg[");
        if (varDsc->lvAddrExposed)                 printf("X");
        if (varTypeIsStruct(varDsc))               printf("S");
        if (varDsc->lvVMNeedsStackAddr)            printf("V");
        if (varDsc->lvLiveInOutOfHndlr)            printf("H");
        if (varDsc->lvLclFieldExpr)                printf("F");
        if (varDsc->lvLclBlockOpAddr)              printf("B");
        if (varDsc->lvLiveAcrossUCall)             printf("U");
#ifdef JIT32_GCENCODER
        if (varDsc->lvPinned)                      printf("P");
#endif // JIT32_GCENCODER
        printf("]");
    }

    if (varDsc->lvMustInit)                  printf(" must-init");
    if (varDsc->lvAddrExposed)               printf(" addr-exposed");
    if (varDsc->lvHasLdAddrOp)               printf(" ld-addr-op");
    if (varDsc->lvVerTypeInfo.IsThisPtr())   printf(" this");
    if (varDsc->lvPinned)                    printf(" pinned");
    if (varDsc->lvRefAssign)                 printf(" ref-asgn");
    if (varDsc->lvStackByref)                printf(" stack-byref");
#ifndef _TARGET_64BIT_
    if (varDsc->lvStructDoubleAlign)         printf(" double-align");
#endif // !_TARGET_64BIT_
    if (varDsc->lvOverlappingFields)         printf(" overlapping-fields");

    if (compGSReorderStackLayout && !varDsc->lvRegister)
    {
        if (varDsc->lvIsPtr)                 printf(" ptr");
        if (varDsc->lvIsUnsafeBuffer)        printf(" unsafe-buffer");
    }
    if (varDsc->lvIsStructField)
    {
        LclVarDsc *  parentvarDsc = &lvaTable[varDsc->lvParentLcl];
#if !defined(_TARGET_64BIT_)
        if (varTypeIsLong(parentvarDsc))
        {
            bool isLo = (lclNum == parentvarDsc->lvFieldLclStart);
            printf(" V%02u.%s(offs=0x%02x)",
                   varDsc->lvParentLcl,
                   isLo ? "lo" : "hi",
                   isLo ? 0 : genTypeSize(TYP_INT)
                  );
        }
        else
#endif // !defined(_TARGET_64BIT_)
        {
            CORINFO_CLASS_HANDLE  typeHnd = parentvarDsc->lvVerTypeInfo.GetClassHandle();
            CORINFO_FIELD_HANDLE  fldHnd  = info.compCompHnd->getFieldInClass(typeHnd, varDsc->lvFldOrdinal);

            printf(" V%02u.%s(offs=0x%02x)",
                   varDsc->lvParentLcl,
                   eeGetFieldName(fldHnd),
                   varDsc->lvFldOffset
                 );

            lvaPromotionType promotionType = lvaGetPromotionType(parentvarDsc);
            // We should never have lvIsStructField set if it is a reg-sized non-field-addressed struct.
            assert(!varDsc->lvRegStruct);
            switch (promotionType)
            {
            case PROMOTION_TYPE_NONE:           printf(" P-NONE");  break;
            case PROMOTION_TYPE_DEPENDENT:      printf(" P-DEP");   break;
            case PROMOTION_TYPE_INDEPENDENT:    printf(" P-INDEP"); break;
            }
        }
    }

    printf("\n");
}

/*****************************************************************************
*
*  dump the lvaTable
*/

void   Compiler::lvaTableDump(FrameLayoutState curState)
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
        printf("; Initial");
    else if (curState == PRE_REGALLOC_FRAME_LAYOUT)
        printf("; Pre-RegAlloc");
    else if (curState == REGALLOC_FRAME_LAYOUT)
        printf("; RegAlloc");
    else if (curState == TENTATIVE_FRAME_LAYOUT)
        printf("; Tentative");
    else if (curState == FINAL_FRAME_LAYOUT)
        printf("; Final");
    else
    {
        printf("UNKNOWN FrameLayoutState!");
        unreached();
    }

    printf(" local variable assignments\n");
    printf(";\n");

    unsigned        lclNum;
    LclVarDsc *     varDsc;

    // Figure out some sizes, to help line things up

    size_t          refCntWtdWidth = 6; // Use 6 as the minimum width

    if (curState != INITIAL_FRAME_LAYOUT)   // don't need this info for INITIAL_FRAME_LAYOUT
    {
        for (lclNum = 0, varDsc = lvaTable;
             lclNum < lvaCount;
             lclNum++  , varDsc++)
        {
            size_t width = strlen(refCntWtd2str(varDsc->lvRefCntWtd));
            if (width > refCntWtdWidth)
                refCntWtdWidth = width;
        }
    }

    // Do the actual output

    for (lclNum = 0, varDsc = lvaTable;
         lclNum < lvaCount;
         lclNum++  , varDsc++)
    {
        lvaDumpEntry(lclNum, curState, refCntWtdWidth);
    }

    //-------------------------------------------------------------------------
    // Display the code-gen temps

    assert(tmpAllFree());
    for (TempDsc* temp = tmpListBeg();
                  temp != nullptr;
                  temp = tmpListNxt(temp))
    {
        printf(";  TEMP_%02u %26s%*s%7s  -> ",
            -temp->tdTempNum(),
            " ",
            refCntWtdWidth,
            " ",
            varTypeName(temp->tdTempType()));
        int  offset  = temp->tdTempOffs();
        printf(" [%2s%1s0x%02X]\n",
               isFramePointerUsed() ? STR_FPBASE : STR_SPBASE,
               (offset < 0  ? "-"     : "+"),
               (offset < 0  ? -offset : offset));
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

unsigned            Compiler::lvaFrameSize(FrameLayoutState curState)
{
    assert(curState < FINAL_FRAME_LAYOUT);

    unsigned result;

    /* Layout the stack frame conservatively.
       Assume all callee-saved registers are spilled to stack */

    compCalleeRegsPushed = CNT_CALLEE_SAVED;

#if defined(_TARGET_ARMARCH_)
    if (compFloatingPointUsed)
        compCalleeRegsPushed += CNT_CALLEE_SAVED_FLOAT;

    compCalleeRegsPushed++;  // we always push LR.  See genPushCalleeSavedRegisters
#elif defined(_TARGET_AMD64_)
    if (compFloatingPointUsed)
        compCalleeFPRegsSavedMask = RBM_FLT_CALLEE_SAVED;
    else
        compCalleeFPRegsSavedMask = RBM_NONE;
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
        compCalleeRegsPushed--;
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

int                 Compiler::lvaGetSPRelativeOffset(unsigned varNum)
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

int                 Compiler::lvaGetCallerSPRelativeOffset(unsigned varNum)
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);
    assert(varNum < lvaCount);
    LclVarDsc * varDsc = lvaTable + varNum;
    assert(varDsc->lvOnFrame);

    return lvaToCallerSPRelativeOffset(varDsc->lvStkOffs, varDsc->lvFramePointerBased);
}

int                Compiler::lvaToCallerSPRelativeOffset(int offset, bool isFpBased)
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

int                 Compiler::lvaGetInitialSPRelativeOffset(unsigned varNum)
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);
    assert(varNum < lvaCount);
    LclVarDsc * varDsc = lvaTable + varNum;
    assert(varDsc->lvOnFrame);

    return lvaToInitialSPRelativeOffset(varDsc->lvStkOffs, varDsc->lvFramePointerBased);
}

// Given a local variable offset, and whether that offset is frame-pointer based, return its offset from Initial-SP.
// This is used, for example, to figure out the offset of the frame pointer from Initial-SP.
int                Compiler::lvaToInitialSPRelativeOffset(unsigned offset, bool isFpBased)
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
#else // !_TARGET_AMD64_
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

static
unsigned            LCL_FLD_PADDING(unsigned lclNum)
{
    // Convert every 2nd variable
    if (lclNum % 2)
        return 0;

    // Pick a padding size at "random"
    unsigned    size = lclNum % 7;

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
Compiler::fgWalkResult      Compiler::lvaStressLclFldCB(GenTreePtr *pTree, fgWalkData *data)
{
    GenTreePtr  tree    = *pTree;
    genTreeOps  oper    = tree->OperGet();
    GenTreePtr  lcl;

    switch (oper)
    {
    case GT_LCL_VAR:
        lcl = tree;
        break;

    case GT_ADDR:
        if (tree->gtOp.gtOp1->gtOper != GT_LCL_VAR)
            return WALK_CONTINUE;
        lcl = tree->gtOp.gtOp1;
        break;

    default:
        return WALK_CONTINUE;
    }

    Compiler *  pComp      = ((lvaStressLclFldArgs*)data->pCallbackData)->m_pCompiler;
    bool        bFirstPass = ((lvaStressLclFldArgs*)data->pCallbackData)->m_bFirstPass;
    noway_assert(lcl->gtOper == GT_LCL_VAR);
    unsigned    lclNum  = lcl->gtLclVarCommon.gtLclNum;
    var_types   type    = lcl->TypeGet();
    LclVarDsc * varDsc  = &pComp->lvaTable[lclNum];

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
        if (varType != TYP_BLK &&
            genTypeSize(varType) != genTypeSize(genActualType(varType)))
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
        
        // Change the variable to a TYP_BLK
        if (varType != TYP_BLK)
        {
            varDsc->lvExactSize = (unsigned)(roundUp(padding + pComp->lvaLclSize(lclNum)));
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
            GenTreePtr  newAddr = new(pComp, GT_NONE) GenTreeOp(*tree->AsOp());

            tree->ChangeOper(GT_ADD);
            tree->gtOp.gtOp1 = newAddr;
            tree->gtOp.gtOp2 = pComp->gtNewIconNode(padding);

            lcl->gtType = TYP_BLK;
        }
    }
                
    return WALK_SKIP_SUBTREES;
}

/*****************************************************************************/

void                Compiler::lvaStressLclFld()
{
    if (!compStressCompile(STRESS_LCL_FLDS, 5))
        return;

    lvaStressLclFldArgs Args;
    Args.m_pCompiler  = this;
    Args.m_bFirstPass = true;

    // Do First pass
    fgWalkAllTreesPre(lvaStressLclFldCB, &Args);

    // Second pass
    Args.m_bFirstPass = false;
    fgWalkAllTreesPre(lvaStressLclFldCB, &Args);
}

/*****************************************************************************/
#endif // DEBUG
/*****************************************************************************
 *
 *  A little routine that displays a local variable bitset.
 *  'set' is mask of variables that have to be displayed
 *  'allVars' is the complete set of interesting variables (blank space is
 *    inserted if its corresponding bit is not in 'set').
 */

#ifdef  DEBUG
void                Compiler::lvaDispVarSet(VARSET_VALARG_TP set)
{
    VARSET_TP VARSET_INIT_NOCOPY(allVars, VarSetOps::MakeEmpty(this));
    lvaDispVarSet(set, allVars);
}


void                Compiler::lvaDispVarSet(VARSET_VALARG_TP set, VARSET_VALARG_TP allVars)
{
    printf("{");

    bool needSpace = false;

    for (unsigned index = 0; index < lvaTrackedCount; index++)
    {
        if (VarSetOps::IsMember(this, set, index))
        {
            unsigned        lclNum;
            LclVarDsc   *   varDsc;

            /* Look for the matching variable */

            for (lclNum = 0, varDsc = lvaTable;
                 lclNum < lvaCount;
                 lclNum++  , varDsc++)
            {
                if  ((varDsc->lvVarIndex == index) && varDsc->lvTracked)
                    break;
            }

            if (needSpace)
                printf(" ");
            else
                needSpace = true;

            printf("V%02u", lclNum);
        }
        else if (VarSetOps::IsMember(this, allVars, index))
        {
            if (needSpace)
                printf(" ");
            else
                needSpace = true;

            printf("   ");
        }
    }

    printf("}");
}

#endif // DEBUG
