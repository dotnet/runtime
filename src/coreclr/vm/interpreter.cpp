// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
#include "common.h"

#ifdef FEATURE_INTERPRETER

#include "interpreter.h"
#include "interpreter.hpp"
#include "cgencpu.h"
#include "stublink.h"
#include "openum.h"
#include "fcall.h"
#include "frames.h"
#include "gcheaputilities.h"
#include <float.h>
#include "jitinterface.h"
#include "safemath.h"
#include "exceptmacros.h"
#include "runtimeexceptionkind.h"
#include "runtimehandles.h"
#include "vars.hpp"
#include "cycletimer.h"

inline CORINFO_CALLINFO_FLAGS combine(CORINFO_CALLINFO_FLAGS flag1, CORINFO_CALLINFO_FLAGS flag2)
{
    return (CORINFO_CALLINFO_FLAGS) (flag1 | flag2);
}

static CorInfoType asCorInfoType(CORINFO_CLASS_HANDLE clsHnd)
{
    TypeHandle typeHnd(clsHnd);
    return CEEInfo::asCorInfoType(typeHnd.GetInternalCorElementType(), typeHnd, NULL);
}

InterpreterMethodInfo::InterpreterMethodInfo(CEEInfo* comp, CORINFO_METHOD_INFO* methInfo)
    : m_method(methInfo->ftn),
      m_module(methInfo->scope),
      m_ILCode(methInfo->ILCode),
      m_ILCodeEnd(methInfo->ILCode + methInfo->ILCodeSize),
      m_maxStack(methInfo->maxStack),
#if INTERP_PROFILE
      m_totIlInstructionsExeced(0),
      m_maxIlInstructionsExeced(0),
#endif
      m_ehClauseCount(methInfo->EHcount),
      m_varArgHandleArgNum(NO_VA_ARGNUM),
      m_numArgs(methInfo->args.numArgs),
      m_numLocals(methInfo->locals.numArgs),
      m_flags(0),
      m_argDescs(NULL),
      m_returnType(methInfo->args.retType),
      m_invocations(0),
      m_methodCache(NULL)
{
    // Overflow sanity check. (Can ILCodeSize ever be zero?)
    _ASSERTE(m_ILCode <= m_ILCodeEnd);

    // Does the calling convention indicate an implicit "this" (first arg) or generic type context arg (last arg)?
    SetFlag<Flag_hasThisArg>((methInfo->args.callConv & CORINFO_CALLCONV_HASTHIS) != 0);
    if (GetFlag<Flag_hasThisArg>())
    {
        GCX_PREEMP();
        CORINFO_CLASS_HANDLE methClass = comp->getMethodClass(methInfo->ftn);
        DWORD attribs = comp->getClassAttribs(methClass);
        SetFlag<Flag_thisArgIsObjPtr>((attribs & CORINFO_FLG_VALUECLASS) == 0);
    }

#if INTERP_PROFILE || defined(_DEBUG)
    {
        const char* clsName;
#if defined(_DEBUG)
        m_methName = ::eeGetMethodFullName(comp, methInfo->ftn, &clsName);
#else
        m_methName = comp->getMethodName(methInfo->ftn, &clsName);
#endif
        char* myClsName = new char[strlen(clsName) + 1];
        strcpy(myClsName, clsName);
        m_clsName = myClsName;
    }
#endif // INTERP_PROFILE

    // Do we have a ret buff?  If its a struct or refany, then *maybe*, depending on architecture...
    bool hasRetBuff = (methInfo->args.retType == CORINFO_TYPE_VALUECLASS || methInfo->args.retType == CORINFO_TYPE_REFANY);
#if defined(FEATURE_HFA)
    // ... unless its an HFA type (and not varargs)...
    if (hasRetBuff && (comp->getHFAType(methInfo->args.retTypeClass) != CORINFO_HFA_ELEM_NONE) && methInfo->args.getCallConv() != CORINFO_CALLCONV_VARARG)
    {
        hasRetBuff = false;
    }
#endif

#if defined(UNIX_AMD64_ABI)
    // ...or it fits into two registers.
    if (hasRetBuff && getClassSize(methInfo->args.retTypeClass) <= 2 * sizeof(void*))
    {
        hasRetBuff = false;
    }
#elif defined(HOST_ARM) || defined(HOST_AMD64)|| defined(HOST_ARM64)
    // ...or it fits into one register.
    if (hasRetBuff && getClassSize(methInfo->args.retTypeClass) <= sizeof(void*))
    {
        hasRetBuff = false;
    }
#endif
    SetFlag<Flag_hasRetBuffArg>(hasRetBuff);

    MetaSig sig(reinterpret_cast<MethodDesc*>(methInfo->ftn));
    SetFlag<Flag_hasGenericsContextArg>((methInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE) != 0);
    SetFlag<Flag_isVarArg>((methInfo->args.callConv & CORINFO_CALLCONV_VARARG) != 0);
    SetFlag<Flag_typeHasGenericArgs>(methInfo->args.sigInst.classInstCount > 0);
    SetFlag<Flag_methHasGenericArgs>(methInfo->args.sigInst.methInstCount > 0);
    _ASSERTE_MSG(!GetFlag<Flag_hasGenericsContextArg>()
                 || ((GetFlag<Flag_typeHasGenericArgs>() & !(GetFlag<Flag_hasThisArg>() && GetFlag<Flag_thisArgIsObjPtr>())) || GetFlag<Flag_methHasGenericArgs>()),
                 "If the method takes a generic parameter, is a static method of generic class (or meth of a value class), and/or itself takes generic parameters");

    if (GetFlag<Flag_hasThisArg>())
    {
        m_numArgs++;
    }
    if (GetFlag<Flag_hasRetBuffArg>())
    {
        m_numArgs++;
    }
    if (GetFlag<Flag_isVarArg>())
    {
        m_numArgs++;
    }
    if (GetFlag<Flag_hasGenericsContextArg>())
    {
        m_numArgs++;
    }
    if (m_numArgs == 0)
    {
        m_argDescs = NULL;
    }
    else
    {
        m_argDescs = new ArgDesc[m_numArgs];
    }

    // Now we'll do the locals.
    m_localDescs = new LocalDesc[m_numLocals];
    // Allocate space for the pinning reference bits (lazily).
    m_localIsPinningRefBits = NULL;

    // Now look at each local.
    CORINFO_ARG_LIST_HANDLE localsPtr = methInfo->locals.args;
    CORINFO_CLASS_HANDLE vcTypeRet;
    unsigned curLargeStructOffset = 0;
    for (unsigned k = 0; k < methInfo->locals.numArgs; k++)
    {
        // TODO: if this optimization succeeds, the switch below on localType
        // can become much simpler.
        m_localDescs[k].m_offset = 0;
#ifdef _DEBUG
        vcTypeRet = NULL;
#endif
        CorInfoTypeWithMod localTypWithMod = comp->getArgType(&methInfo->locals, localsPtr, &vcTypeRet);
        // If the local vars is a pinning reference, set the bit to indicate this.
        if ((localTypWithMod & CORINFO_TYPE_MOD_PINNED) != 0)
        {
            SetPinningBit(k);
        }

        CorInfoType localType = strip(localTypWithMod);
        switch (localType)
        {
        case CORINFO_TYPE_VALUECLASS:
        case CORINFO_TYPE_REFANY: // Just a special case: vcTypeRet is handle for TypedReference in this case...
            {
                InterpreterType tp = InterpreterType(comp, vcTypeRet);
                unsigned size = static_cast<unsigned>(tp.Size(comp));
                size = max(size, sizeof(void*));
                m_localDescs[k].m_type = tp;
                if (tp.IsLargeStruct(comp))
                {
                    m_localDescs[k].m_offset = curLargeStructOffset;
                    curLargeStructOffset += size;
                }
            }
            break;

        case CORINFO_TYPE_VAR:
            NYI_INTERP("argument of generic parameter type");  // Should not happen;
            break;

        default:
            m_localDescs[k].m_type = InterpreterType(localType);
            break;
        }
        m_localDescs[k].m_typeStackNormal = m_localDescs[k].m_type.StackNormalize();
        localsPtr = comp->getArgNext(localsPtr);
    }
    m_largeStructLocalSize = curLargeStructOffset;
}

void InterpreterMethodInfo::InitArgInfo(CEEInfo* comp, CORINFO_METHOD_INFO* methInfo, short* argOffsets_)
{
    unsigned numSigArgsPlusThis = methInfo->args.numArgs;
    if (GetFlag<Flag_hasThisArg>())
    {
        numSigArgsPlusThis++;
    }

    // The m_argDescs array is constructed in the following "canonical" order:
    // 1. 'this' pointer
    // 2. signature arguments
    // 3. return buffer
    // 4. type parameter -or- vararg cookie
    //
    // argOffsets_ is passed in this order, and serves to establish the offsets to arguments
    // when the interpreter is invoked using the native calling convention (i.e., not directly).
    //
    // When the interpreter is invoked directly, the arguments will appear in the same order
    // and form as arguments passed to MethodDesc::CallDescr().  This ordering is as follows:
    // 1. 'this' pointer
    // 2. return buffer
    // 3. signature arguments
    //
    // MethodDesc::CallDescr() does not support generic parameters or varargs functions.

    _ASSERTE_MSG((methInfo->args.callConv & (CORINFO_CALLCONV_EXPLICITTHIS)) == 0,
        "Don't yet handle EXPLICITTHIS calling convention modifier.");
    switch (methInfo->args.callConv & CORINFO_CALLCONV_MASK)
    {
    case CORINFO_CALLCONV_DEFAULT:
    case CORINFO_CALLCONV_VARARG:
        {
            unsigned k = 0;
            ARG_SLOT* directOffset = NULL;
            short directRetBuffOffset = 0;
            short directVarArgOffset = 0;
            short directTypeParamOffset = 0;

            // If there's a "this" argument, handle it.
            if (GetFlag<Flag_hasThisArg>())
            {
                m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_UNDEF);
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
                MethodDesc *pMD = reinterpret_cast<MethodDesc*>(methInfo->ftn);
                // The signature of the ILStubs may be misleading.
                // If a StubTarget is ever set, we'll find the correct type by inspecting the
                // target, rather than the stub.
                if (pMD->IsILStub())
                {

                    if (pMD->AsDynamicMethodDesc()->IsUnboxingILStub())
                    {
                        // This is an unboxing stub where the thisptr is passed as a boxed VT.
                        m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_CLASS);
                    }
                    else
                    {
                        MethodDesc *pTargetMD = pMD->AsDynamicMethodDesc()->GetILStubResolver()->GetStubTargetMethodDesc();
                        if (pTargetMD != NULL)
                        {
                            if (pTargetMD->GetMethodTable()->IsValueType())
                            {
                                m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_BYREF);
                            }
                            else
                            {
                                m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_CLASS);
                            }

                        }
                    }
                }

#endif // FEATURE_INSTANTIATINGSTUB_AS_IL
                if (m_argDescs[k].m_type == InterpreterType(CORINFO_TYPE_UNDEF))
                {
                    CORINFO_CLASS_HANDLE cls = comp->getMethodClass(methInfo->ftn);
                    DWORD attribs = comp->getClassAttribs(cls);
                    if (attribs & CORINFO_FLG_VALUECLASS)
                    {
                        m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_BYREF);
                    }
                    else
                    {
                        m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_CLASS);
                    }
                }
                m_argDescs[k].m_typeStackNormal = m_argDescs[k].m_type;
                m_argDescs[k].m_nativeOffset = argOffsets_[k];
                m_argDescs[k].m_directOffset = static_cast<short>(reinterpret_cast<intptr_t>(ArgSlotEndianessFixup(directOffset, sizeof(void*))));
                directOffset++;
                k++;
            }

            // If there is a return buffer, it will appear next in the arguments list for a direct call.
            // Reserve its offset now, for use after the explicit arguments.
#if defined(HOST_ARM)
            // On ARM, for direct calls we always treat HFA return types as having ret buffs.
            // So figure out if we have an HFA return type.
            bool hasHFARetType =
                methInfo->args.retType == CORINFO_TYPE_VALUECLASS
                && (comp->getHFAType(methInfo->args.retTypeClass) != CORINFO_HFA_ELEM_NONE)
                && methInfo->args.getCallConv() != CORINFO_CALLCONV_VARARG;
#endif // defined(HOST_ARM)

            if (GetFlag<Flag_hasRetBuffArg>()
#if defined(HOST_ARM)
                // On ARM, for direct calls we always treat HFA return types as having ret buffs.
                || hasHFARetType
#endif // defined(HOST_ARM)
                )
            {
                directRetBuffOffset = static_cast<short>(reinterpret_cast<intptr_t>(ArgSlotEndianessFixup(directOffset, sizeof(void*))));
                directOffset++;
            }
#if defined(HOST_AMD64)
            if (GetFlag<Flag_isVarArg>())
            {
                directVarArgOffset = static_cast<short>(reinterpret_cast<intptr_t>(ArgSlotEndianessFixup(directOffset, sizeof(void*))));
                directOffset++;
            }
            if (GetFlag<Flag_hasGenericsContextArg>())
            {
                directTypeParamOffset = static_cast<short>(reinterpret_cast<intptr_t>(ArgSlotEndianessFixup(directOffset, sizeof(void*))));
                directOffset++;
            }
#endif

            // Now record the argument types for the rest of the arguments.
            InterpreterType it;
            CORINFO_CLASS_HANDLE vcTypeRet;
            CORINFO_ARG_LIST_HANDLE argPtr = methInfo->args.args;
            for (; k < numSigArgsPlusThis; k++)
            {
                CorInfoTypeWithMod argTypWithMod = comp->getArgType(&methInfo->args, argPtr, &vcTypeRet);
                CorInfoType argType = strip(argTypWithMod);
                switch (argType)
                {
                case CORINFO_TYPE_VALUECLASS:
                case CORINFO_TYPE_REFANY: // Just a special case: vcTypeRet is handle for TypedReference in this case...
                    it = InterpreterType(comp, vcTypeRet);
                    break;
                default:
                    // Everything else is just encoded as a shifted CorInfoType.
                    it = InterpreterType(argType);
                    break;
                }
                m_argDescs[k].m_type = it;
                m_argDescs[k].m_typeStackNormal = it.StackNormalize();
                m_argDescs[k].m_nativeOffset = argOffsets_[k];
                // When invoking the interpreter directly, large value types are always passed by reference.
                if (it.IsLargeStruct(comp))
                {
                    m_argDescs[k].m_directOffset = static_cast<short>(reinterpret_cast<intptr_t>(ArgSlotEndianessFixup(directOffset, sizeof(void*))));
                }
                else
                {
                    m_argDescs[k].m_directOffset = static_cast<short>(reinterpret_cast<intptr_t>(ArgSlotEndianessFixup(directOffset, it.Size(comp))));
                }
                argPtr = comp->getArgNext(argPtr);
                directOffset++;
            }

            if (GetFlag<Flag_hasRetBuffArg>())
            {
                // The generic type context is an unmanaged pointer (native int).
                m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_BYREF);
                m_argDescs[k].m_typeStackNormal = m_argDescs[k].m_type;
                m_argDescs[k].m_nativeOffset = argOffsets_[k];
                m_argDescs[k].m_directOffset = directRetBuffOffset;
                k++;
            }

            if (GetFlag<Flag_hasGenericsContextArg>())
            {
                // The vararg cookie is an unmanaged pointer (native int).
                m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_NATIVEINT);
                m_argDescs[k].m_typeStackNormal = m_argDescs[k].m_type;
                m_argDescs[k].m_nativeOffset = argOffsets_[k];
                m_argDescs[k].m_directOffset = directTypeParamOffset;
                directOffset++;
                k++;
            }
            if (GetFlag<Flag_isVarArg>())
            {
                // The generic type context is an unmanaged pointer (native int).
                m_argDescs[k].m_type = InterpreterType(CORINFO_TYPE_NATIVEINT);
                m_argDescs[k].m_typeStackNormal = m_argDescs[k].m_type;
                m_argDescs[k].m_nativeOffset = argOffsets_[k];
                m_argDescs[k].m_directOffset = directVarArgOffset;
                k++;
            }
        }
        break;

    case IMAGE_CEE_CS_CALLCONV_C:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- IMAGE_CEE_CS_CALLCONV_C");
        break;

    case IMAGE_CEE_CS_CALLCONV_STDCALL:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- IMAGE_CEE_CS_CALLCONV_STDCALL");
        break;

    case IMAGE_CEE_CS_CALLCONV_THISCALL:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- IMAGE_CEE_CS_CALLCONV_THISCALL");
        break;

    case IMAGE_CEE_CS_CALLCONV_FASTCALL:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- IMAGE_CEE_CS_CALLCONV_FASTCALL");
        break;

    case CORINFO_CALLCONV_FIELD:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- CORINFO_CALLCONV_FIELD");
        break;

    case CORINFO_CALLCONV_LOCAL_SIG:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- CORINFO_CALLCONV_LOCAL_SIG");
        break;

    case CORINFO_CALLCONV_PROPERTY:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- CORINFO_CALLCONV_PROPERTY");
        break;

    case CORINFO_CALLCONV_UNMANAGED:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- CORINFO_CALLCONV_UNMANAGED");
        break;

    case CORINFO_CALLCONV_NATIVEVARARG:
        NYI_INTERP("InterpreterMethodInfo::InitArgInfo -- CORINFO_CALLCONV_NATIVEVARARG");
        break;

    default:
        _ASSERTE_ALL_BUILDS(__FILE__, false); // shouldn't get here
    }
}

InterpreterMethodInfo::~InterpreterMethodInfo()
{
    if (m_methodCache != NULL)
    {
        delete reinterpret_cast<ILOffsetToItemCache*>(m_methodCache);
    }
}

void InterpreterMethodInfo::AllocPinningBitsIfNeeded()
{
    if (m_localIsPinningRefBits != NULL)
        return;

    unsigned numChars = (m_numLocals + 7) / 8;
    m_localIsPinningRefBits = new char[numChars];
    for (unsigned i = 0; i < numChars; i++)
    {
        m_localIsPinningRefBits[i] = char(0);
    }
}


void InterpreterMethodInfo::SetPinningBit(unsigned locNum)
{
    _ASSERTE_MSG(locNum < m_numLocals, "Precondition");
    AllocPinningBitsIfNeeded();

    unsigned ind = locNum / 8;
    unsigned bitNum = locNum - (ind * 8);
    m_localIsPinningRefBits[ind] |= (1 << bitNum);
}

bool InterpreterMethodInfo::GetPinningBit(unsigned locNum)
{
    _ASSERTE_MSG(locNum < m_numLocals, "Precondition");
    if (m_localIsPinningRefBits == NULL)
        return false;

    unsigned ind = locNum / 8;
    unsigned bitNum = locNum - (ind * 8);
    return (m_localIsPinningRefBits[ind] & (1 << bitNum)) != 0;
}

void Interpreter::ArgState::AddArg(unsigned canonIndex, short numSlots, bool noReg, bool twoSlotAlign)
{
#if defined(HOST_AMD64)
    _ASSERTE(!noReg);
    _ASSERTE(!twoSlotAlign);
    AddArgAmd64(canonIndex, numSlots, /*isFloatingType*/false);
    return;
#else // !HOST_AMD64
#if defined(HOST_X86) || defined(HOST_ARM64)
    _ASSERTE(!twoSlotAlign); // Shouldn't use this flag on x86 (it wouldn't work right in the stack, at least).
#endif
    // If the argument requires two-slot alignment, make sure we have it.  This is the
    // ARM model: both in regs and on the stack.
    if (twoSlotAlign)
    {
        if (!noReg && numRegArgs < NumberOfIntegerRegArgs())
        {
            if ((numRegArgs % 2) != 0)
            {
                numRegArgs++;
            }
        }
        else
        {
            if ((callerArgStackSlots % 2) != 0)
            {
                callerArgStackSlots++;
            }
        }
    }

#if defined(HOST_ARM64)
    // On ARM64 we're not going to place an argument 'partially' on the stack
    // if all slots fits into registers, they go into registers, otherwise they go into stack.
    if (!noReg && numRegArgs+numSlots <= NumberOfIntegerRegArgs())
#else
    if (!noReg && numRegArgs < NumberOfIntegerRegArgs())
#endif
    {
        argIsReg[canonIndex] = ARS_IntReg;
        argOffsets[canonIndex] = numRegArgs * sizeof(void*);
        numRegArgs += numSlots;
        // If we overflowed the regs, we consume some stack arg space.
        if (numRegArgs > NumberOfIntegerRegArgs())
        {
            callerArgStackSlots += (numRegArgs - NumberOfIntegerRegArgs());
        }
    }
    else
    {
#if defined(HOST_X86)
        // On X86, stack args are pushed in order.  We will add the total size of the arguments to this offset,
        // so we set this to a negative number relative to the SP before the first arg push.
        callerArgStackSlots += numSlots;
        ClrSafeInt<short> offset(-callerArgStackSlots);
#elif defined(HOST_ARM) || defined(HOST_ARM64)
        // On ARM, args are pushed in *reverse* order.  So we will create an offset relative to the address
        // of the first stack arg; later, we will add the size of the non-stack arguments.
        ClrSafeInt<short> offset(callerArgStackSlots);
#endif
        offset *= static_cast<short>(sizeof(void*));
        _ASSERTE(!offset.IsOverflow());
        argOffsets[canonIndex] = offset.Value();
#if defined(HOST_ARM) || defined(HOST_ARM64)
        callerArgStackSlots += numSlots;
#endif
    }
#endif // !HOST_AMD64
}

#if defined(HOST_AMD64)

#if defined(UNIX_AMD64_ABI)
void Interpreter::ArgState::AddArgAmd64(unsigned canonIndex, unsigned short numSlots, bool isFloatingType)
{
    int regSlots = numFPRegArgSlots + numRegArgs;
    if (isFloatingType && numFPRegArgSlots + 1 < MaxNumFPRegArgSlots)
    {
        _ASSERTE(numSlots == 1);
        argIsReg[canonIndex]   = ARS_FloatReg;
        argOffsets[canonIndex] = regSlots * sizeof(void*);
        fpArgsUsed |= (0x1 << regSlots);
        numFPRegArgSlots += 1;
        return;
    }
    else if (numSlots < 3 && (numRegArgs + numSlots <= NumberOfIntegerRegArgs()))
    {
        argIsReg[canonIndex]   = ARS_IntReg;
        argOffsets[canonIndex] = regSlots * sizeof(void*);
        numRegArgs += numSlots;
    }
    else
    {
        argIsReg[canonIndex] = ARS_NotReg;
        ClrSafeInt<short> offset(callerArgStackSlots * sizeof(void*));
        _ASSERTE(!offset.IsOverflow());
        argOffsets[canonIndex] = offset.Value();
        callerArgStackSlots += numSlots;
    }
}
#else
// Windows AMD64 calling convention allows any type that can be contained in 64 bits to be passed in registers,
// if not contained or they are of a size not a power of 2, then they are passed by reference on the stack.
// RCX, RDX, R8, R9 are the int arg registers. XMM0-3 overlap with the integer registers and are used
// for floating point arguments.
void Interpreter::ArgState::AddArgAmd64(unsigned canonIndex, unsigned short numSlots, bool isFloatingType)
{
    // If floating type and there are slots use a float reg slot.
    if (isFloatingType && (numFPRegArgSlots < MaxNumFPRegArgSlots))
    {
        _ASSERTE(numSlots == 1);
        argIsReg[canonIndex] = ARS_FloatReg;
        argOffsets[canonIndex] = numFPRegArgSlots * sizeof(void*);
        fpArgsUsed |= (0x1 << (numFPRegArgSlots + 1));
        numFPRegArgSlots += 1;
        numRegArgs += 1; // Increment int reg count due to shadowing.
        return;
    }

    // If we have an integer/aligned-struct arg or a reference of a struct that got copied on
    // to the stack, it would go into a register or a stack slot.
    if (numRegArgs != NumberOfIntegerRegArgs())
    {
        argIsReg[canonIndex] = ARS_IntReg;
        argOffsets[canonIndex] = numRegArgs * sizeof(void*);
        numRegArgs += 1;
        numFPRegArgSlots += 1; // Increment FP reg count due to shadowing.
    }
    else
    {
        argIsReg[canonIndex] = ARS_NotReg;
        ClrSafeInt<short> offset(callerArgStackSlots * sizeof(void*));
        _ASSERTE(!offset.IsOverflow());
        argOffsets[canonIndex] = offset.Value();
        callerArgStackSlots += 1;
    }
}
#endif //UNIX_AMD64_ABI
#endif

void Interpreter::ArgState::AddFPArg(unsigned canonIndex, unsigned short numSlots, bool twoSlotAlign)
{
#if defined(HOST_AMD64)
    _ASSERTE(!twoSlotAlign);
    _ASSERTE(numSlots == 1);
    AddArgAmd64(canonIndex, numSlots, /*isFloatingType*/ true);
#elif defined(HOST_X86)
    _ASSERTE(false);  // Don't call this on x86; we pass all FP on the stack.
#elif defined(HOST_ARM)
    // We require "numSlots" alignment.
    _ASSERTE(numFPRegArgSlots + numSlots <= MaxNumFPRegArgSlots);
    argIsReg[canonIndex] = ARS_FloatReg;

    if (twoSlotAlign)
    {
        // If we require two slot alignment, the number of slots must be a multiple of two.
        _ASSERTE((numSlots % 2) == 0);

        // Skip a slot if necessary.
        if ((numFPRegArgSlots % 2) != 0)
        {
            numFPRegArgSlots++;
        }
        // We always use new slots for two slot aligned args precision...
        argOffsets[canonIndex] = numFPRegArgSlots * sizeof(void*);
        for (unsigned short i = 0; i < numSlots/2; i++)
        {
            fpArgsUsed |= (0x3 << (numFPRegArgSlots + i));
        }
        numFPRegArgSlots += numSlots;
    }
    else
    {
        if (numSlots == 1)
        {
            // A single-precision (float) argument.  We must do "back-filling" where possible, searching
            // for previous unused registers.
            unsigned slot = 0;
            while (slot < 32 && (fpArgsUsed & (1 << slot))) slot++;
            _ASSERTE(slot < 32); // Search succeeded.
            _ASSERTE(slot <= numFPRegArgSlots);  // No bits at or above numFPRegArgSlots are set (regs used).
            argOffsets[canonIndex] = slot * sizeof(void*);
            fpArgsUsed |= (0x1 << slot);
            if (slot == numFPRegArgSlots)
                numFPRegArgSlots += numSlots;
        }
        else
        {
            // We can always allocate at after the last used slot.
            argOffsets[numFPRegArgSlots] = numFPRegArgSlots * sizeof(void*);
            for (unsigned i = 0; i < numSlots; i++)
            {
                fpArgsUsed |= (0x1 << (numFPRegArgSlots + i));
            }
            numFPRegArgSlots += numSlots;
        }
    }
#elif defined(HOST_ARM64)

    _ASSERTE(numFPRegArgSlots + numSlots <= MaxNumFPRegArgSlots);
    _ASSERTE(!twoSlotAlign);
    argIsReg[canonIndex] = ARS_FloatReg;

    argOffsets[canonIndex] = numFPRegArgSlots * sizeof(void*);
    for (unsigned i = 0; i < numSlots; i++)
    {
        fpArgsUsed |= (0x1 << (numFPRegArgSlots + i));
    }
    numFPRegArgSlots += numSlots;

#else
#error "Unsupported architecture"
#endif
}


// static
CorJitResult Interpreter::GenerateInterpreterStub(CEEInfo* comp,
                                                  CORINFO_METHOD_INFO* info,
                                                  /*OUT*/ BYTE **nativeEntry,
                                                  /*OUT*/ ULONG *nativeSizeOfCode,
                                                  InterpreterMethodInfo** ppInterpMethodInfo,
                                                  bool jmpCall)
{
    //
    // First, ensure that the compiler-specific statics are initialized.
    //

    InitializeCompilerStatics(comp);

    //
    // Next, use switches and IL scanning to determine whether to interpret this method.
    //

#if INTERP_TRACING
#define TRACE_SKIPPED(cls, meth, reason)                                                \
    if (s_DumpInterpreterStubsFlag.val(CLRConfig::INTERNAL_DumpInterpreterStubs)) {     \
        fprintf(GetLogFile(), "Skipping %s:%s (%s).\n", cls, meth, reason);             \
    }
#else
#define TRACE_SKIPPED(cls, meth, reason)
#endif


    // If jmpCall, we only need to do computations involving method info.
    if (!jmpCall)
    {
        const char* clsName;
        const char* methName = comp->getMethodName(info->ftn, &clsName);
        if (   !s_InterpretMeths.contains(methName, clsName, info->args.pSig)
            || s_InterpretMethsExclude.contains(methName, clsName, info->args.pSig))
        {
            TRACE_SKIPPED(clsName, methName, "not in set of methods to interpret");
            return CORJIT_SKIPPED;
        }

        unsigned methHash = comp->getMethodHash(info->ftn);
        if (   methHash < s_InterpretMethHashMin.val(CLRConfig::INTERNAL_InterpreterMethHashMin)
            || methHash > s_InterpretMethHashMax.val(CLRConfig::INTERNAL_InterpreterMethHashMax))
        {
            TRACE_SKIPPED(clsName, methName, "hash not within range to interpret");
            return CORJIT_SKIPPED;
        }

        MethodDesc* pMD = reinterpret_cast<MethodDesc*>(info->ftn);

#if !INTERP_ILSTUBS
        if (pMD->IsILStub())
        {
            TRACE_SKIPPED(clsName, methName, "interop stubs not supported");
            return CORJIT_SKIPPED;
        }
        else
#endif // !INTERP_ILSTUBS

        if (!s_InterpreterDoLoopMethods && MethodMayHaveLoop(info->ILCode, info->ILCodeSize))
        {
            TRACE_SKIPPED(clsName, methName, "has loop, not interpreting loop methods.");
            return CORJIT_SKIPPED;
        }

        s_interpreterStubNum++;

#if INTERP_TRACING
        if (s_interpreterStubNum < s_InterpreterStubMin.val(CLRConfig::INTERNAL_InterpreterStubMin)
                || s_interpreterStubNum > s_InterpreterStubMax.val(CLRConfig::INTERNAL_InterpreterStubMax))
        {
            TRACE_SKIPPED(clsName, methName, "stub num not in range, not interpreting.");
            return CORJIT_SKIPPED;
        }

        if (s_DumpInterpreterStubsFlag.val(CLRConfig::INTERNAL_DumpInterpreterStubs))
        {
            unsigned hash = comp->getMethodHash(info->ftn);
            fprintf(GetLogFile(), "Generating interpretation stub (# %d = 0x%x, hash = 0x%x) for %s:%s.\n",
                    s_interpreterStubNum, s_interpreterStubNum, hash, clsName, methName);
            fflush(GetLogFile());
        }
#endif
    }

    //
    // Finally, generate an interpreter entry-point stub.
    //

    // @TODO: this structure clearly needs some sort of lifetime management.  It is the moral equivalent
    // of compiled code, and should be associated with an app domain.  In addition, when I get to it, we should
    // delete it when/if we actually compile the method.  (Actually, that's complicated, since there may be
    // VSD stubs still bound to the interpreter stub.  The check there will get to the jitted code, but we want
    // to eventually clean those up at some safe point...)
    InterpreterMethodInfo* interpMethInfo = new InterpreterMethodInfo(comp, info);
    if (ppInterpMethodInfo != nullptr)
    {
        *ppInterpMethodInfo = interpMethInfo;
    }
    interpMethInfo->m_stubNum = s_interpreterStubNum;
    MethodDesc* methodDesc = reinterpret_cast<MethodDesc*>(info->ftn);
    if (!jmpCall)
    {
        interpMethInfo = RecordInterpreterMethodInfoForMethodHandle(info->ftn, interpMethInfo);
    }

#if FEATURE_INTERPRETER_DEADSIMPLE_OPT
    unsigned offsetOfLd;
    if (IsDeadSimpleGetter(comp, methodDesc, &offsetOfLd))
    {
        interpMethInfo->SetFlag<InterpreterMethodInfo::Flag_methIsDeadSimpleGetter>(true);
        if (offsetOfLd == ILOffsetOfLdFldInDeadSimpleInstanceGetterDbg)
        {
            interpMethInfo->SetFlag<InterpreterMethodInfo::Flag_methIsDeadSimpleGetterIsDbgForm>(true);
        }
        else
        {
            _ASSERTE(offsetOfLd == ILOffsetOfLdFldInDeadSimpleInstanceGetterOpt);
        }
    }
#endif // FEATURE_INTERPRETER_DEADSIMPLE_OPT

    // Used to initialize the arg offset information.
    Stub* stub = NULL;

    // We assume that the stack contains (with addresses growing upwards, assuming a downwards-growing stack):
    //
    //    [Non-reg arg N-1]
    //    ...
    //    [Non-reg arg <# of reg args>]
    //    [return PC]
    //
    // Then push the register args to get:
    //
    //    [Non-reg arg N-1]
    //    ...
    //    [Non-reg arg <# of reg args>]
    //    [return PC]
    //    [reg arg <# of reg args>-1]
    //    ...
    //    [reg arg 0]
    //
    // Pass the address of this argument array, and the MethodDesc pointer for the method, as arguments to
    // Interpret.
    //
    // So the structure of the code will look like this (in the non-ILstub case):
    //
#if defined(HOST_X86) || defined(HOST_AMD64)
    // push ebp
    // mov ebp, esp
    // [if there are register arguments in ecx or edx, push them]
    // ecx := addr of InterpretMethodInfo for the method to be intepreted.
    // edx = esp  /*pointer to argument structure*/
    // call to Interpreter::InterpretMethod
    // [if we pushed register arguments, increment esp by the right amount.]
    // pop ebp
    // ret <n>  ; where <n> is the number of argument stack slots in the call to the stub.
#elif defined (HOST_ARM)
    // TODO.
#endif

    // TODO: much of the interpreter stub code should be is shareable.  In the non-IL stub case,
    // at least, we could have a small per-method stub that puts the address of the method-specific
    // InterpreterMethodInfo into eax, and then branches to a shared part.  Probably we would want to
    // always push all integer args on x86, as we do already on ARM.  On ARM, we'd need several versions
    // of the shared stub, for different numbers of floating point register args, cross different kinds of
    // HFA return values.  But these could still be shared, and the per-method stub would decide which of
    // these to target.
    //
    // In the IL stub case, which uses eax, it would be problematic to do this sharing.

    StubLinkerCPU sl;
    MethodDesc* pMD = reinterpret_cast<MethodDesc*>(info->ftn);
    if (!jmpCall)
    {
        sl.Init();
#if defined(HOST_X86) || defined(HOST_AMD64)
#if defined(HOST_X86)
        sl.X86EmitPushReg(kEBP);
        sl.X86EmitMovRegReg(kEBP, static_cast<X86Reg>(kESP_Unsafe));
#endif
#elif defined(HOST_ARM)
        // On ARM we use R12 as a "scratch" register -- callee-trashed, not used
        // for arguments.
        ThumbReg r11 = ThumbReg(11);
        ThumbReg r12 = ThumbReg(12);

#elif defined(HOST_ARM64)
        // x8 through x15 are scratch registers on ARM64.
        IntReg x8 = IntReg(8);
        IntReg x9 = IntReg(9);
#else
#error unsupported platform
#endif
    }

    MetaSig sig(methodDesc);

    unsigned totalArgs = info->args.numArgs;
    unsigned sigArgsPlusThis = totalArgs;
    bool hasThis = false;
    bool hasRetBuff = false;
    bool isVarArg = false;
    bool hasGenericsContextArg = false;

    // Below, we will increment "totalArgs" for any of the "this" argument,
    // a ret buff argument, and/or a generics context argument.
    //
    // There will be four arrays allocated below, each with this increased "totalArgs" elements:
    // argOffsets, argIsReg, argPerm, and, later, m_argDescs.
    //
    // They will be indexed in the order (0-based, [] indicating optional)
    //
    //    [this] sigArgs [retBuff] [VASigCookie] [genCtxt]
    //
    // We will call this "canonical order".  It is architecture-independent, and
    // does not necessarily correspond to the architecture-dependent physical order
    // in which the registers are actually passed.  (That's actually the purpose of
    // "argPerm": to record the correspondence between canonical order and physical
    // order.)  We could have chosen any order for the first three of these, but it's
    // simplest to let m_argDescs have all the passed IL arguments passed contiguously
    // at the beginning, allowing it to be indexed by IL argument number.

    int genericsContextArgIndex = 0;
    int retBuffArgIndex = 0;
    int vaSigCookieIndex = 0;

    if (sig.HasThis())
    {
        _ASSERTE(info->args.callConv & CORINFO_CALLCONV_HASTHIS);
        hasThis = true;
        totalArgs++; sigArgsPlusThis++;
    }

    if (methodDesc->HasRetBuffArg())
    {
        hasRetBuff = true;
        retBuffArgIndex = totalArgs;
        totalArgs++;
    }

    if (sig.GetCallingConventionInfo() & CORINFO_CALLCONV_VARARG)
    {
        isVarArg = true;
        vaSigCookieIndex = totalArgs;
        totalArgs++;
    }

    if (sig.GetCallingConventionInfo() & CORINFO_CALLCONV_PARAMTYPE)
    {
        _ASSERTE(info->args.callConv & CORINFO_CALLCONV_PARAMTYPE);
        hasGenericsContextArg = true;
        genericsContextArgIndex = totalArgs;
        totalArgs++;
    }

    // The non-this sig args have indices starting after these.

    // We will first encode the arg offsets as *negative* offsets from the address above the first
    // stack arg, and later add in the total size of the stack args to get a positive offset.
    // The first sigArgsPlusThis elements are the offsets of the IL-addressable arguments.  After that,
    // there may be up to two more: generics context arg, if present, and return buff pointer, if present.
    // (Note that the latter is actually passed after the "this" pointer, or else first if no "this" pointer
    // is present.  We re-arrange to preserve the easy IL-addressability.)
    ArgState argState(totalArgs);

    // This is the permutation that translates from an index in the argOffsets/argIsReg arrays to
    // the platform-specific order in which the arguments are passed.
    unsigned* argPerm = new unsigned[totalArgs];

    // The number of register argument slots we end up pushing.
    unsigned short regArgsFound = 0;

    unsigned physArgIndex = 0;

#if defined(HOST_ARM)
    // The stub linker has a weird little limitation: all stubs it's used
    // for on ARM push some callee-saved register, so the unwind info
    // code was written assuming at least one would be pushed.  I don't know how to
    // fix it, so I'm meeting this requirement, by pushing one callee-save.
#define STUB_LINK_EMIT_PROLOG_REQUIRES_CALLEE_SAVE_PUSH 1

#if STUB_LINK_EMIT_PROLOG_REQUIRES_CALLEE_SAVE_PUSH
    const int NumberOfCalleeSaveRegsToPush = 1;
#else
    const int NumberOfCalleeSaveRegsToPush = 0;
#endif
    // The "1" here is for the return address.
    const int NumberOfFixedPushes = 1 + NumberOfCalleeSaveRegsToPush;
#elif defined(HOST_ARM64)
    // FP, LR
    const int NumberOfFixedPushes = 2;
#endif

#if defined(FEATURE_HFA)
#if defined(HOST_ARM) || defined(HOST_ARM64)
    // On ARM, a non-retBuffArg method that returns a struct type might be an HFA return.  Figure
    // that out.
    unsigned HFARetTypeSize = 0;
#endif
#if defined(HOST_ARM64)
    unsigned cHFAVars   = 0;
#endif
    if (info->args.retType == CORINFO_TYPE_VALUECLASS
        && (comp->getHFAType(info->args.retTypeClass) != CORINFO_HFA_ELEM_NONE)
        && info->args.getCallConv() != CORINFO_CALLCONV_VARARG)
    {
        HFARetTypeSize = getClassSize(info->args.retTypeClass);
#if defined(HOST_ARM)
        // Round up to a double boundary;
        HFARetTypeSize = ((HFARetTypeSize+ sizeof(double) - 1) / sizeof(double)) * sizeof(double);
#elif defined(HOST_ARM64)
        // We don't need to round it up to double. Unlike ARM, whether it's a float or a double each field will
        // occupy one slot. We'll handle the stack alignment in the prolog where we have all the information about
        // what is going to be pushed on the stack.
        // Instead on ARM64 we'll need to know how many slots we'll need.
        // for instance a VT with two float fields will have the same size as a VT with 1 double field. (ARM64TODO: Verify it)
        // It works on ARM because the overlapping layout of the floating point registers
        // but it won't work on ARM64.
        cHFAVars = (comp->getHFAType(info->args.retTypeClass) == CORINFO_HFA_ELEM_FLOAT) ? HFARetTypeSize/sizeof(float) : HFARetTypeSize/sizeof(double);
#endif
    }

#endif // defined(FEATURE_HFA)

    _ASSERTE_MSG((info->args.callConv & (CORINFO_CALLCONV_EXPLICITTHIS)) == 0,
        "Don't yet handle EXPLICITTHIS calling convention modifier.");

    switch (info->args.callConv & CORINFO_CALLCONV_MASK)
    {
    case CORINFO_CALLCONV_DEFAULT:
    case CORINFO_CALLCONV_VARARG:
        {
            unsigned firstSigArgIndex = 0;
            if (hasThis)
            {
                argPerm[0] = physArgIndex; physArgIndex++;
                argState.AddArg(0);
                firstSigArgIndex++;
            }

            if (hasRetBuff)
            {
                argPerm[retBuffArgIndex] = physArgIndex; physArgIndex++;
                argState.AddArg(retBuffArgIndex);
            }

            if (isVarArg)
            {
                argPerm[vaSigCookieIndex] = physArgIndex; physArgIndex++;
                interpMethInfo->m_varArgHandleArgNum = vaSigCookieIndex;
                argState.AddArg(vaSigCookieIndex);
            }

#if defined(HOST_ARM) || defined(HOST_AMD64) || defined(HOST_ARM64)
            // Generics context comes before args on ARM.  Would be better if I factored this out as a call,
            // to avoid large swatches of duplicate code.
            if (hasGenericsContextArg)
            {
                argPerm[genericsContextArgIndex] = physArgIndex; physArgIndex++;
                argState.AddArg(genericsContextArgIndex);
            }
#endif // HOST_ARM || HOST_AMD64 || HOST_ARM64

            CORINFO_ARG_LIST_HANDLE argPtr = info->args.args;
            // Some arguments are have been passed in registers, some in memory. We must generate code that
            // moves the register arguments to memory, and determines a pointer into the stack from which all
            // the arguments can be accessed, according to the offsets in "argOffsets."
            //
            // In the first pass over the arguments, we will label and count the register arguments, and
            // initialize entries in "argOffsets" for the non-register arguments -- relative to the SP at the
            // time of the call. Then when we have counted the number of register arguments, we will adjust
            // the offsets for the non-register arguments to account for those. Then, in the second pass, we
            // will push the register arguments on the stack, and capture the final stack pointer value as
            // the argument vector pointer.
            CORINFO_CLASS_HANDLE vcTypeRet;
            // This iteration starts at the first signature argument, and iterates over all the
            // canonical indices for the signature arguments.
            for (unsigned k = firstSigArgIndex; k < sigArgsPlusThis; k++)
            {
                argPerm[k] = physArgIndex; physArgIndex++;

                CorInfoTypeWithMod argTypWithMod = comp->getArgType(&info->args, argPtr, &vcTypeRet);
                CorInfoType argType = strip(argTypWithMod);
                switch (argType)
                {
                case CORINFO_TYPE_UNDEF:
                case CORINFO_TYPE_VOID:
                case CORINFO_TYPE_VAR:
                    _ASSERTE_ALL_BUILDS(__FILE__, false);  // Should not happen;
                    break;

                    // One integer slot arguments:
                case CORINFO_TYPE_BOOL:
                case CORINFO_TYPE_CHAR:
                case CORINFO_TYPE_BYTE:
                case CORINFO_TYPE_UBYTE:
                case CORINFO_TYPE_SHORT:
                case CORINFO_TYPE_USHORT:
                case CORINFO_TYPE_INT:
                case CORINFO_TYPE_UINT:
                case CORINFO_TYPE_NATIVEINT:
                case CORINFO_TYPE_NATIVEUINT:
                case CORINFO_TYPE_BYREF:
                case CORINFO_TYPE_CLASS:
                case CORINFO_TYPE_STRING:
                case CORINFO_TYPE_PTR:
                    argState.AddArg(k);
                    break;

                    // Two integer slot arguments.
                case CORINFO_TYPE_LONG:
                case CORINFO_TYPE_ULONG:
#if defined(HOST_X86)
                    // Longs are always passed on the stack -- with no obvious alignment.
                    argState.AddArg(k, 2, /*noReg*/true);
#elif defined(HOST_ARM)
                    // LONGS have 2-reg alignment; inc reg if necessary.
                    argState.AddArg(k, 2, /*noReg*/false, /*twoSlotAlign*/true);
#elif defined(HOST_AMD64) || defined(HOST_ARM64)
                    argState.AddArg(k);
#else
#error unknown platform
#endif
                    break;

                    // One float slot args:
                case CORINFO_TYPE_FLOAT:
#if defined(HOST_X86)
                    argState.AddArg(k, 1, /*noReg*/true);
#elif defined(HOST_ARM)
                    argState.AddFPArg(k, 1, /*twoSlotAlign*/false);
#elif defined(HOST_AMD64) || defined(HOST_ARM64)
                    argState.AddFPArg(k, 1, false);
#else
#error unknown platform
#endif
                    break;

                    // Two float slot args
                case CORINFO_TYPE_DOUBLE:
#if defined(HOST_X86)
                    argState.AddArg(k, 2, /*noReg*/true);
#elif defined(HOST_ARM)
                    argState.AddFPArg(k, 2, /*twoSlotAlign*/true);
#elif defined(HOST_AMD64) || defined(HOST_ARM64)
                    argState.AddFPArg(k, 1, false);
#else
#error unknown platform
#endif
                    break;

                    // Value class args:
                case CORINFO_TYPE_VALUECLASS:
                case CORINFO_TYPE_REFANY:
                    {
                        unsigned sz = getClassSize(vcTypeRet);
                        unsigned szSlots = max(1, sz / sizeof(void*));
#if defined(HOST_X86)
                        argState.AddArg(k, static_cast<short>(szSlots), /*noReg*/true);
#elif defined(HOST_AMD64)
                        argState.AddArg(k, static_cast<short>(szSlots));
#elif defined(HOST_ARM) || defined(HOST_ARM64)
                        // TODO: handle Vector64, Vector128 types
                        CorInfoHFAElemType hfaType = comp->getHFAType(vcTypeRet);
                        if (CorInfoTypeIsFloatingPoint(hfaType))
                        {
                            argState.AddFPArg(k, szSlots,
#if defined(HOST_ARM)
                                    /*twoSlotAlign*/ (hfaType == CORINFO_HFA_ELEM_DOUBLE)
#elif defined(HOST_ARM64)
                                    /*twoSlotAlign*/ false // unlike ARM32 FP args always consume 1 slot on ARM64
#endif
                                    );
                        }
                        else
                        {
                            unsigned align = comp->getClassAlignmentRequirement(vcTypeRet, FALSE);
                            argState.AddArg(k, static_cast<short>(szSlots), /*noReg*/false,
#if defined(HOST_ARM)
                                    /*twoSlotAlign*/ (align == 8)
#elif defined(HOST_ARM64)
                                    /*twoSlotAlign*/ false
#endif
                                    );
                        }
#else
#error unknown platform
#endif
                    }
                    break;


                default:
                    _ASSERTE_MSG(false, "should not reach here, unknown arg type");
                }
                argPtr = comp->getArgNext(argPtr);
            }

#if defined(HOST_X86)
            // Generics context comes last on HOST_X86.  Would be better if I factored this out as a call,
            // to avoid large swatches of duplicate code.
            if (hasGenericsContextArg)
            {
                argPerm[genericsContextArgIndex] = physArgIndex; physArgIndex++;
                argState.AddArg(genericsContextArgIndex);
            }

            // Now we have counted the number of register arguments, so we can update the offsets for the
            // non-register arguments.  "+ 2" below is to account for the return address from the call, and
            // pushing of EBP.
            unsigned short stackArgBaseOffset = (argState.numRegArgs + 2 + argState.callerArgStackSlots) * sizeof(void*);
            unsigned       intRegArgBaseOffset = 0;

#elif defined(HOST_ARM)

            // We're choosing to always push all arg regs on ARM -- this is the only option
            // that ThumbEmitProlog currently gives.
            argState.numRegArgs = 4;

            // On ARM, we push the (integer) arg regs before we push the return address, so we don't add an
            // extra constant.  And the offset is the address of the last pushed argument, which is the first
            // stack argument in signature order.

            // Round up to a double boundary...
            unsigned       fpStackSlots = ((argState.numFPRegArgSlots + 1) / 2) * 2;
            unsigned       intRegArgBaseOffset = (fpStackSlots + NumberOfFixedPushes) * sizeof(void*);
            unsigned short stackArgBaseOffset = intRegArgBaseOffset + (argState.numRegArgs) * sizeof(void*);
#elif defined(HOST_ARM64)

            // See StubLinkerCPU::EmitProlog for the layout of the stack
            unsigned       intRegArgBaseOffset = (argState.numFPRegArgSlots) * sizeof(void*);
            unsigned short stackArgBaseOffset = (unsigned short) ((argState.numRegArgs + argState.numFPRegArgSlots) * sizeof(void*));
#elif defined(UNIX_AMD64_ABI)
            unsigned       intRegArgBaseOffset = 0;
            unsigned short stackArgBaseOffset = (2 + argState.numRegArgs + argState.numFPRegArgSlots) * sizeof(void*);
#elif defined(HOST_AMD64)
            unsigned short stackArgBaseOffset = (argState.numRegArgs) * sizeof(void*);
#else
#error unsupported platform
#endif

#if defined(HOST_ARM)
            WORD regArgMask = 0;
#endif // defined(HOST_ARM)
            // argPerm maps from an index into the argOffsets/argIsReg arrays to
            // the order that the arguments are passed.
            unsigned* argPermInverse = new unsigned[totalArgs];
            for (unsigned t = 0; t < totalArgs; t++)
            {
                argPermInverse[argPerm[t]] = t;
            }

            for (unsigned kk = 0; kk < totalArgs; kk++)
            {
                // Let "k" be the index of the kk'th input in the argOffsets and argIsReg arrays.
                // To compute "k" we need to invert argPerm permutation -- determine the "k" such
                // that argPerm[k] == kk.
                unsigned k = argPermInverse[kk];

                _ASSERTE(k < totalArgs);

                if (argState.argIsReg[k] == ArgState::ARS_IntReg)
                {
                    regArgsFound++;
                    // If any int reg args are used on ARM, we push them all (in ThumbEmitProlog)
#if defined(HOST_X86)
                    if (regArgsFound == 1)
                    {
                        if (!jmpCall) { sl.X86EmitPushReg(kECX); }
                        argState.argOffsets[k] = (argState.numRegArgs - regArgsFound)*sizeof(void*);  // General form, good for general # of reg args.
                    }
                    else
                    {
                        _ASSERTE(regArgsFound == 2);
                        if (!jmpCall) { sl.X86EmitPushReg(kEDX); }
                        argState.argOffsets[k] = (argState.numRegArgs - regArgsFound)*sizeof(void*);
                    }
#elif defined(HOST_ARM) || defined(HOST_ARM64) || defined(UNIX_AMD64_ABI)
                    argState.argOffsets[k] += intRegArgBaseOffset;
#elif defined(HOST_AMD64)
                    // First home the register arguments in the stack space allocated by the caller.
                    // Refer to Stack Allocation on x64 [http://msdn.microsoft.com/en-US/library/ew5tede7(v=vs.80).aspx]
                    X86Reg argRegs[] = { kECX, kEDX, kR8, kR9 };
                    if (!jmpCall) { sl.X86EmitIndexRegStoreRSP(regArgsFound * sizeof(void*), argRegs[regArgsFound - 1]); }
                    argState.argOffsets[k] = (regArgsFound - 1) * sizeof(void*);
#else
#error unsupported platform
#endif
                }
#if defined(HOST_AMD64) && !defined(UNIX_AMD64_ABI)
                else if (argState.argIsReg[k] == ArgState::ARS_FloatReg)
                {
                    // Increment regArgsFound since float/int arguments have overlapping registers.
                    regArgsFound++;
                    // Home the float arguments.
                    X86Reg argRegs[] = { kXMM0, kXMM1, kXMM2, kXMM3 };
                    if (!jmpCall) { sl.X64EmitMovSDToMem(argRegs[regArgsFound - 1], static_cast<X86Reg>(kESP_Unsafe), regArgsFound * sizeof(void*)); }
                    argState.argOffsets[k] = (regArgsFound - 1) * sizeof(void*);
                }
#endif
                else if (argState.argIsReg[k] == ArgState::ARS_NotReg)
                {
                    argState.argOffsets[k] += stackArgBaseOffset;
                }
                // So far, x86 doesn't have any FP reg args, and ARM and ARM64 puts them at offset 0, so no
                // adjustment is necessary (yet) for arguments passed in those registers.
            }
            delete[] argPermInverse;
        }
        break;

    case IMAGE_CEE_CS_CALLCONV_C:
        NYI_INTERP("GenerateInterpreterStub -- IMAGE_CEE_CS_CALLCONV_C");
        break;

    case IMAGE_CEE_CS_CALLCONV_STDCALL:
        NYI_INTERP("GenerateInterpreterStub -- IMAGE_CEE_CS_CALLCONV_STDCALL");
        break;

    case IMAGE_CEE_CS_CALLCONV_THISCALL:
        NYI_INTERP("GenerateInterpreterStub -- IMAGE_CEE_CS_CALLCONV_THISCALL");
        break;

    case IMAGE_CEE_CS_CALLCONV_FASTCALL:
        NYI_INTERP("GenerateInterpreterStub -- IMAGE_CEE_CS_CALLCONV_FASTCALL");
        break;

    case CORINFO_CALLCONV_FIELD:
        NYI_INTERP("GenerateInterpreterStub -- CORINFO_CALLCONV_FIELD");
        break;

    case CORINFO_CALLCONV_LOCAL_SIG:
        NYI_INTERP("GenerateInterpreterStub -- CORINFO_CALLCONV_LOCAL_SIG");
        break;

    case CORINFO_CALLCONV_PROPERTY:
        NYI_INTERP("GenerateInterpreterStub -- CORINFO_CALLCONV_PROPERTY");
        break;

    case CORINFO_CALLCONV_UNMANAGED:
        NYI_INTERP("GenerateInterpreterStub -- CORINFO_CALLCONV_UNMANAGED");
        break;

    case CORINFO_CALLCONV_NATIVEVARARG:
        NYI_INTERP("GenerateInterpreterStub -- CORINFO_CALLCONV_NATIVEVARARG");
        break;

    default:
        _ASSERTE_ALL_BUILDS(__FILE__, false); // shouldn't get here
    }

    delete[] argPerm;

    PCODE interpretMethodFunc;
    if (!jmpCall)
    {
        switch (info->args.retType)
        {
        case CORINFO_TYPE_FLOAT:
            interpretMethodFunc = reinterpret_cast<PCODE>(&InterpretMethodFloat);
            break;
        case CORINFO_TYPE_DOUBLE:
            interpretMethodFunc = reinterpret_cast<PCODE>(&InterpretMethodDouble);
            break;
        default:
            interpretMethodFunc = reinterpret_cast<PCODE>(&InterpretMethod);
            break;
        }
        // The argument registers have been pushed by now, so we can use them.
#if defined(HOST_X86)
        // First arg is pointer to the base of the ILargs arr -- i.e., the current stack value.
        sl.X86EmitMovRegReg(kEDX, static_cast<X86Reg>(kESP_Unsafe));
        // InterpretMethod uses F_CALL_CONV == __fastcall; pass 2 args in regs.
#if INTERP_ILSTUBS
        if (pMD->IsILStub())
        {
            // Third argument is stubcontext, in eax.
            sl.X86EmitPushReg(kEAX);
        }
        else
#endif
        {
            // For a non-ILStub method, push NULL as the StubContext argument.
            sl.X86EmitZeroOutReg(kECX);
            sl.X86EmitPushReg(kECX);
        }
        // sl.X86EmitAddReg(kECX, reinterpret_cast<UINT>(interpMethInfo));
        sl.X86EmitRegLoad(kECX, reinterpret_cast<UINT>(interpMethInfo));
        sl.X86EmitCall(sl.NewExternalCodeLabel(interpretMethodFunc), 0);
        // Now we will deallocate the stack slots we pushed to hold register arguments.
        if (argState.numRegArgs > 0)
        {
            sl.X86EmitAddEsp(argState.numRegArgs * sizeof(void*));
        }
        sl.X86EmitPopReg(kEBP);
        sl.X86EmitReturn(static_cast<WORD>(argState.callerArgStackSlots * sizeof(void*)));
#elif defined(UNIX_AMD64_ABI)
        bool hasTowRetSlots = info->args.retType == CORINFO_TYPE_VALUECLASS &&
            getClassSize(info->args.retTypeClass) == 16;

        int fixedTwoSlotSize = 16;

        int argSize = (argState.numFPRegArgSlots +  argState.numRegArgs) * sizeof(void*);

        int stackSize = argSize + fixedTwoSlotSize; // Fixed two slot for possible "retbuf", access address by "m_ilArgs-16"

        if (stackSize % 16 == 0) { // for $rsp align requirement
            stackSize += 8;
        }

        sl.X86EmitSubEsp(stackSize);

        X86Reg intArgsRegs[] = {ARGUMENT_kREG1, ARGUMENT_kREG2, kRDX, kRCX, kR8, kR9};
        int    indexGP       = 0;
        int    indexFP       = 0;
        for (int i = 0; i < argState.numRegArgs + argState.numFPRegArgSlots; i++)
        {
            int offs = i * sizeof(void*) + 16;
            if (argState.fpArgsUsed & (1 << i))
            {
                sl.X64EmitMovSDToMem(static_cast<X86Reg>(indexFP), static_cast<X86Reg>(kESP_Unsafe), offs);
                indexFP++;
            }
            else
            {
                sl.X86EmitIndexRegStoreRSP(offs, intArgsRegs[indexGP]);
                indexGP++;
            }
        }

        // Pass "ilArgs", i.e. just the point where registers have been homed, as 2nd arg.
        sl.X86EmitIndexLeaRSP(ARGUMENT_kREG2, static_cast<X86Reg>(kESP_Unsafe), fixedTwoSlotSize);

        // If we have IL stubs pass the stub context in R10 or else pass NULL.
#if INTERP_ILSTUBS
        if (pMD->IsILStub())
        {
            sl.X86EmitMovRegReg(kRDX, kR10);
        }
        else
#endif
        {
            // For a non-ILStub method, push NULL as the StubContext argument.
            sl.X86EmitZeroOutReg(ARGUMENT_kREG1);
            sl.X86EmitMovRegReg(kRDX, ARGUMENT_kREG1);
        }
        sl.X86EmitRegLoad(ARGUMENT_kREG1, reinterpret_cast<UINT_PTR>(interpMethInfo));

        sl.X86EmitCall(sl.NewExternalCodeLabel(interpretMethodFunc), 0);
        if (hasTowRetSlots) {
            sl.X86EmitEspOffset(0x8b, kRAX, 0);
            sl.X86EmitEspOffset(0x8b, kRDX, 8);
        }
        sl.X86EmitAddEsp(stackSize);
        sl.X86EmitReturn(0);
#elif defined(HOST_AMD64)
        // Pass "ilArgs", i.e. just the point where registers have been homed, as 2nd arg
        sl.X86EmitIndexLeaRSP(ARGUMENT_kREG2, static_cast<X86Reg>(kESP_Unsafe), 8);

        // Allocate space for homing callee's (InterpretMethod's) arguments.
        // Calling convention requires a default allocation space of 4,
        // but to double align the stack frame, we'd allocate 5.
        int interpMethodArgSize = 5 * sizeof(void*);
        sl.X86EmitSubEsp(interpMethodArgSize);

        // If we have IL stubs pass the stub context in R10 or else pass NULL.
#if INTERP_ILSTUBS
        if (pMD->IsILStub())
        {
            sl.X86EmitMovRegReg(kR8, kR10);
        }
        else
#endif
        {
            // For a non-ILStub method, push NULL as the StubContext argument.
            sl.X86EmitZeroOutReg(ARGUMENT_kREG1);
            sl.X86EmitMovRegReg(kR8, ARGUMENT_kREG1);
        }
        sl.X86EmitRegLoad(ARGUMENT_kREG1, reinterpret_cast<UINT_PTR>(interpMethInfo));
        sl.X86EmitCall(sl.NewExternalCodeLabel(interpretMethodFunc), 0);
        sl.X86EmitAddEsp(interpMethodArgSize);
        sl.X86EmitReturn(0);
#elif defined(HOST_ARM)

        // We have to maintain 8-byte stack alignment.  So if the number of
        // slots we would normally push is not a multiple of two, add a random
        // register.  (We will not pop this register, but rather, increment
        // sp by an amount that includes it.)
        bool oddPushes = (((argState.numRegArgs + NumberOfFixedPushes) % 2) != 0);

        UINT stackFrameSize = 0;
        if (oddPushes) stackFrameSize = sizeof(void*);
        // Now, if any FP regs are used as arguments, we will copy those to the stack; reserve space for that here.
        // (We push doubles to keep the stack aligned...)
        unsigned short doublesToPush = (argState.numFPRegArgSlots + 1)/2;
        stackFrameSize += (doublesToPush*2*sizeof(void*));

        // The last argument here causes this to generate code to push all int arg regs.
        sl.ThumbEmitProlog(/*cCalleeSavedRegs*/NumberOfCalleeSaveRegsToPush, /*cbStackFrame*/stackFrameSize, /*fPushArgRegs*/TRUE);

        // Now we will generate code to copy the floating point registers to the stack frame.
        if (doublesToPush > 0)
        {
            sl.ThumbEmitStoreMultipleVFPDoubleReg(ThumbVFPDoubleReg(0), thumbRegSp, doublesToPush*2);
        }

#if INTERP_ILSTUBS
        if (pMD->IsILStub())
        {
            // Third argument is stubcontext, in r12.
            sl.ThumbEmitMovRegReg(ThumbReg(2), ThumbReg(12));
        }
        else
#endif
        {
            // For a non-ILStub method, push NULL as the third StubContext argument.
            sl.ThumbEmitMovConstant(ThumbReg(2), 0);
        }
        // Second arg is pointer to the base of the ILargs arr -- i.e., the current stack value.
        sl.ThumbEmitMovRegReg(ThumbReg(1), thumbRegSp);

        // First arg is the pointer to the interpMethInfo structure.
        sl.ThumbEmitMovConstant(ThumbReg(0), reinterpret_cast<int>(interpMethInfo));

        // If there's an HFA return, add space for that.
        if (HFARetTypeSize > 0)
        {
            sl.ThumbEmitSubSp(HFARetTypeSize);
        }

        // Now we can call the right method.
        // No "direct call" instruction, so load into register first.  Can use R3.
        sl.ThumbEmitMovConstant(ThumbReg(3), static_cast<int>(interpretMethodFunc));
        sl.ThumbEmitCallRegister(ThumbReg(3));

        // If there's an HFA return, copy to FP regs, and deallocate the stack space.
        if (HFARetTypeSize > 0)
        {
            sl.ThumbEmitLoadMultipleVFPDoubleReg(ThumbVFPDoubleReg(0), thumbRegSp, HFARetTypeSize/sizeof(void*));
            sl.ThumbEmitAddSp(HFARetTypeSize);
        }

        sl.ThumbEmitEpilog();

#elif defined(HOST_ARM64)

        UINT stackFrameSize = argState.numFPRegArgSlots;

        sl.EmitProlog(argState.numRegArgs, argState.numFPRegArgSlots, 0 /*cCalleeSavedRegs*/, static_cast<unsigned short>(cHFAVars*sizeof(void*)));

#if INTERP_ILSTUBS
        if (pMD->IsILStub())
        {
            // Third argument is stubcontext, in x12 (METHODDESC_REGISTER)
            sl.EmitMovReg(IntReg(2), IntReg(12));
        }
        else
#endif
        {
            // For a non-ILStub method, push NULL as the third stubContext argument
            sl.EmitMovConstant(IntReg(2), 0);
        }

        // Second arg is pointer to the basei of the ILArgs -- i.e., the current stack value
        sl.EmitAddImm(IntReg(1), RegSp, sl.GetSavedRegArgsOffset());

        // First arg is the pointer to the interpMethodInfo structure
#if INTERP_ILSTUBS
        if (!pMD->IsILStub())
#endif
        {
            // interpMethodInfo is already in x8, so copy it from x8
            sl.EmitMovReg(IntReg(0), IntReg(8));
        }
#if INTERP_ILSTUBS
        else
        {
            // We didn't do the short-circuiting, therefore interpMethInfo is
            // not stored in a register (x8) before. so do it now.
            sl.EmitMovConstant(IntReg(0), reinterpret_cast<UINT64>(interpMethInfo));
        }
#endif

        sl.EmitCallLabel(sl.NewExternalCodeLabel((LPVOID)interpretMethodFunc), FALSE, FALSE);

        // If there's an HFA return, copy to FP regs
        if (cHFAVars > 0)
        {
            for (unsigned i=0; i<=(cHFAVars/2)*2;i+=2)
                sl.EmitLoadStoreRegPairImm(StubLinkerCPU::eLOAD, VecReg(i), VecReg(i+1), RegSp, i*sizeof(void*));
            if ((cHFAVars % 2) == 1)
                sl.EmitLoadStoreRegImm(StubLinkerCPU::eLOAD,VecReg(cHFAVars-1), RegSp, cHFAVars*sizeof(void*));

        }

        sl.EmitEpilog();


#else
#error unsupported platform
#endif
        stub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap());

        *nativeSizeOfCode = static_cast<ULONG>(stub->GetNumCodeBytes());
        // TODO: manage reference count of interpreter stubs.  Look for examples...
        *nativeEntry = dac_cast<BYTE*>(stub->GetEntryPoint());
    }

    // Initialize the arg offset information.
    interpMethInfo->InitArgInfo(comp, info, argState.argOffsets);

#ifdef _DEBUG
    AddInterpMethInfo(interpMethInfo);
#endif // _DEBUG
    if (!jmpCall)
    {
        // Remember the mapping between code address and MethodDesc*.
        RecordInterpreterStubForMethodDesc(info->ftn, *nativeEntry);
    }

    return CORJIT_OK;
#undef TRACE_SKIPPED
}

size_t Interpreter::GetFrameSize(InterpreterMethodInfo* interpMethInfo)
{
    size_t sz = interpMethInfo->LocalMemSize();
#if COMBINE_OPSTACK_VAL_TYPE
    sz += (interpMethInfo->m_maxStack * sizeof(OpStackValAndType));
#else
    sz += (interpMethInfo->m_maxStack * (sizeof(INT64) + sizeof(InterpreterType*)));
#endif
    return sz;
}

// static
ARG_SLOT Interpreter::ExecuteMethodWrapper(struct InterpreterMethodInfo* interpMethInfo, bool directCall, BYTE* ilArgs, void* stubContext, _Out_ bool* pDoJmpCall, CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
#define INTERP_DYNAMIC_CONTRACTS 1
#if INTERP_DYNAMIC_CONTRACTS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
#else
    // Dynamic contract occupies too much stack.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
#endif

    size_t sizeWithGS = GetFrameSize(interpMethInfo) + sizeof(GSCookie);
    BYTE* frameMemoryGS = static_cast<BYTE*>(_alloca(sizeWithGS));

    ARG_SLOT retVal = 0;
    unsigned jmpCallToken = 0;

    Interpreter interp(interpMethInfo, directCall, ilArgs, stubContext, frameMemoryGS);

    // Make sure we can do a GC Scan properly.
    FrameWithCookie<InterpreterFrame> interpFrame(&interp);

    // Update the interpretation count.
    InterlockedIncrement(reinterpret_cast<LONG *>(&interpMethInfo->m_invocations));

    // Need to wait until this point to do this JITting, since it may trigger a GC.
    JitMethodIfAppropriate(interpMethInfo);

    // Pass buffers to get jmpCall flag and the token, if necessary.
    interp.ExecuteMethod(&retVal, pDoJmpCall, &jmpCallToken);

    if (*pDoJmpCall)
    {
        GCX_PREEMP();
        interp.ResolveToken(pResolvedToken, jmpCallToken, CORINFO_TOKENKIND_Method InterpTracingArg(RTK_Call));
    }

    interpFrame.Pop();
    return retVal;
}

// TODO: Add GSCookie checks

// static
inline ARG_SLOT Interpreter::InterpretMethodBody(struct InterpreterMethodInfo* interpMethInfo, bool directCall, BYTE* ilArgs, void* stubContext)
{
#if INTERP_DYNAMIC_CONTRACTS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
#else
    // Dynamic contract occupies too much stack.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
#endif

    CEEInfo* jitInfo = NULL;
    for (bool doJmpCall = true; doJmpCall; )
    {
        unsigned jmpCallToken = 0;
        CORINFO_RESOLVED_TOKEN methTokPtr;
        ARG_SLOT retVal = ExecuteMethodWrapper(interpMethInfo, directCall, ilArgs, stubContext, &doJmpCall, &methTokPtr);
        // Clear any allocated jitInfo.
        delete jitInfo;

        // Nothing to do if the recent method asks not to do a jmpCall.
        if (!doJmpCall)
        {
            return retVal;
        }

        // The recently executed method wants us to perform a jmpCall.
        MethodDesc* pMD = GetMethod(methTokPtr.hMethod);
        interpMethInfo = MethodHandleToInterpreterMethInfoPtr(CORINFO_METHOD_HANDLE(pMD));

        // Allocate a new jitInfo and also a new interpMethInfo.
        if (interpMethInfo == NULL)
        {
            _ASSERTE(doJmpCall);
            jitInfo = new CEEInfo(pMD, true);

            CORINFO_METHOD_INFO methInfo;

            GCX_PREEMP();
            jitInfo->getMethodInfo(CORINFO_METHOD_HANDLE(pMD), &methInfo);
            GenerateInterpreterStub(jitInfo, &methInfo, NULL, 0, &interpMethInfo, true);
        }
    }
    UNREACHABLE();
}

void Interpreter::JitMethodIfAppropriate(InterpreterMethodInfo* interpMethInfo, bool force)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    unsigned int MaxInterpretCount = s_InterpreterJITThreshold.val(CLRConfig::INTERNAL_InterpreterJITThreshold);
    bool scheduleTieringBackgroundWork = false;
    TieredCompilationManager *tieredCompilationManager = GetAppDomain()->GetTieredCompilationManager();

    if (force || interpMethInfo->m_invocations > MaxInterpretCount)
    {
        GCX_PREEMP();
        MethodDesc *md = reinterpret_cast<MethodDesc *>(interpMethInfo->m_method);
        PCODE stub = md->GetNativeCode();

        if (InterpretationStubToMethodInfo(stub) == md)
        {
#if INTERP_TRACING
            if (s_TraceInterpreterJITTransitionFlag.val(CLRConfig::INTERNAL_TraceInterpreterJITTransition))
            {
                fprintf(GetLogFile(), "JITting method %s:%s.\n", md->m_pszDebugClassName, md->m_pszDebugMethodName);
            }
#endif // INTERP_TRACING
            CORJIT_FLAGS jitFlags(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
            NewHolder<COR_ILMETHOD_DECODER> pDecoder(NULL);
            // Dynamic methods (e.g., IL stubs) do not have an IL decoder but may
            // require additional flags.  Ordinary methods require the opposite.
            if (md->IsDynamicMethod())
            {
                jitFlags.Add(md->AsDynamicMethodDesc()->GetILStubResolver()->GetJitFlags());
            }
            else
            {
                COR_ILMETHOD_DECODER::DecoderStatus status;
                pDecoder = new COR_ILMETHOD_DECODER(md->GetILHeader(TRUE),
                                                    md->GetMDImport(),
                                                    &status);
            }
            // This used to be a synchronous jit and could be made so again if desired,
            // but using ASP .NET MusicStore as an example scenario the performance is
            // better doing the JIT asynchronously. Given the not-on-by-default nature of the
            // interpreter I didn't wring my hands too much trying to determine the ideal
            // policy.
#ifdef FEATURE_TIERED_COMPILATION
            CodeVersionManager::LockHolder _lockHolder;
            NativeCodeVersion activeCodeVersion = md->GetCodeVersionManager()->GetActiveILCodeVersion(md).GetActiveNativeCodeVersion(md);
            ILCodeVersion ilCodeVersion = activeCodeVersion.GetILCodeVersion();
            if (activeCodeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTier0 &&
                !ilCodeVersion.HasAnyOptimizedNativeCodeVersion(activeCodeVersion))
            {
                tieredCompilationManager->AsyncPromoteToTier1(activeCodeVersion, &scheduleTieringBackgroundWork);
            }
#else
#error FEATURE_INTERPRETER depends on FEATURE_TIERED_COMPILATION now
#endif
        }
    }

    if (scheduleTieringBackgroundWork)
    {
        tieredCompilationManager->TryScheduleBackgroundWorkerWithoutGCTrigger_Locked();
    }
}

// static
HCIMPL3(float, InterpretMethodFloat, struct InterpreterMethodInfo* interpMethInfo, BYTE* ilArgs, void* stubContext)
{
    FCALL_CONTRACT;

    ARG_SLOT retVal = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);
    retVal = (ARG_SLOT)Interpreter::InterpretMethodBody(interpMethInfo, false, ilArgs, stubContext);
    HELPER_METHOD_FRAME_END();

    return *reinterpret_cast<float*>(ArgSlotEndianessFixup(&retVal, sizeof(float)));
}
HCIMPLEND

// static
HCIMPL3(double, InterpretMethodDouble, struct InterpreterMethodInfo* interpMethInfo, BYTE* ilArgs, void* stubContext)
{
    FCALL_CONTRACT;

    ARG_SLOT retVal = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);
    retVal = Interpreter::InterpretMethodBody(interpMethInfo, false, ilArgs, stubContext);
    HELPER_METHOD_FRAME_END();

    return *reinterpret_cast<double*>(ArgSlotEndianessFixup(&retVal, sizeof(double)));
}
HCIMPLEND

// static
HCIMPL3(INT64, InterpretMethod, struct InterpreterMethodInfo* interpMethInfo, BYTE* ilArgs, void* stubContext)
{
    FCALL_CONTRACT;

    ARG_SLOT retVal = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);
    retVal = Interpreter::InterpretMethodBody(interpMethInfo, false, ilArgs, stubContext);
    HELPER_METHOD_FRAME_END();

    return static_cast<INT64>(retVal);
}
HCIMPLEND

bool Interpreter::IsInCalleesFrames(void* stackPtr)
{
    // We assume a downwards_growing stack.
    return stackPtr < (m_localVarMemory - sizeof(GSCookie));
}

// I want an enumeration with values for the second byte of 2-byte opcodes.
enum OPCODE_2BYTE {
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) TWOBYTE_##c = unsigned(s2),
#include "opcode.def"
#undef OPDEF
};

// Optimize the interpreter loop for speed.
#ifdef _MSC_VER
#pragma optimize("t", on)
#endif

// Duplicating code from JitHelpers for MonEnter,MonExit,MonEnter_Static,
// MonExit_Static because it sets up helper frame for the JIT.
static void MonitorEnter(Object* obj, BYTE* pbLockTaken)
{

    OBJECTREF objRef = ObjectToOBJECTREF(obj);


    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    GCPROTECT_BEGININTERIOR(pbLockTaken);

    if (GET_THREAD()->CatchAtSafePointOpportunistic())
    {
        GET_THREAD()->PulseGCMode();
    }
    objRef->EnterObjMonitor();

    if (pbLockTaken != 0) *pbLockTaken = 1;

    GCPROTECT_END();
}

static void MonitorExit(Object* obj, BYTE* pbLockTaken)
{
    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    if (!objRef->LeaveObjMonitor())
        COMPlusThrow(kSynchronizationLockException);

    if (pbLockTaken != 0) *pbLockTaken = 0;

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }
}

static void MonitorEnterStatic(AwareLock *lock, BYTE* pbLockTaken)
{
    lock->Enter();
    MONHELPER_STATE(*pbLockTaken = 1;)
}

static void MonitorExitStatic(AwareLock *lock, BYTE* pbLockTaken)
{
    // Error, yield or contention
    if (!lock->Leave())
        COMPlusThrow(kSynchronizationLockException);

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }
}


AwareLock* Interpreter::GetMonitorForStaticMethod()
{
    MethodDesc* pMD = reinterpret_cast<MethodDesc*>(m_methInfo->m_method);
    CORINFO_LOOKUP_KIND kind;
    {
        GCX_PREEMP();
        m_interpCeeInfo.getLocationOfThisType(m_methInfo->m_method, &kind);
    }
    if (!kind.needsRuntimeLookup)
    {
        OBJECTREF ref = pMD->GetMethodTable()->GetManagedClassObject();
        return (AwareLock*) ref->GetSyncBlock()->GetMonitor();
    }
    else
    {
        CORINFO_CLASS_HANDLE classHnd = nullptr;
        switch (kind.runtimeLookupKind)
        {
        case CORINFO_LOOKUP_CLASSPARAM:
            {
                CORINFO_CONTEXT_HANDLE ctxHnd = GetPreciseGenericsContext();
                _ASSERTE_MSG((((size_t)ctxHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS), "Precise context not class context");
                classHnd = (CORINFO_CLASS_HANDLE) ((size_t)ctxHnd & ~CORINFO_CONTEXTFLAGS_CLASS);
            }
            break;
        case CORINFO_LOOKUP_METHODPARAM:
            {
                CORINFO_CONTEXT_HANDLE ctxHnd = GetPreciseGenericsContext();
                _ASSERTE_MSG((((size_t)ctxHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD), "Precise context not method context");
                MethodDesc* pMD = (MethodDesc*) (CORINFO_METHOD_HANDLE) ((size_t)ctxHnd & ~CORINFO_CONTEXTFLAGS_METHOD);
                classHnd = (CORINFO_CLASS_HANDLE) pMD->GetMethodTable();
            }
            break;
        default:
            NYI_INTERP("Unknown lookup for synchronized methods");
            break;
        }
        MethodTable* pMT = GetMethodTableFromClsHnd(classHnd);
        OBJECTREF ref = pMT->GetManagedClassObject();
        _ASSERTE(ref);
        return (AwareLock*) ref->GetSyncBlock()->GetMonitor();
    }
}

void Interpreter::DoMonitorEnterWork()
{
    MethodDesc* pMD = reinterpret_cast<MethodDesc*>(m_methInfo->m_method);
    if (pMD->IsSynchronized())
    {
        if (pMD->IsStatic())
        {
            AwareLock* lock = GetMonitorForStaticMethod();
            MonitorEnterStatic(lock, &m_monAcquired);
        }
        else
        {
            MonitorEnter((Object*) m_thisArg, &m_monAcquired);
        }
    }
}

void Interpreter::DoMonitorExitWork()
{
    MethodDesc* pMD = reinterpret_cast<MethodDesc*>(m_methInfo->m_method);
    if (pMD->IsSynchronized())
    {
        if (pMD->IsStatic())
        {
            AwareLock* lock = GetMonitorForStaticMethod();
            MonitorExitStatic(lock, &m_monAcquired);
        }
        else
        {
            MonitorExit((Object*) m_thisArg, &m_monAcquired);
        }
    }
}


void Interpreter::ExecuteMethod(ARG_SLOT* retVal, _Out_ bool* pDoJmpCall, _Out_ unsigned* pJmpCallToken)
{
#if INTERP_DYNAMIC_CONTRACTS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
#else
    // Dynamic contract occupies too much stack.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
#endif

    *pDoJmpCall = false;

    // Normally I'd prefer to declare these in small case-block scopes, but most C++ compilers
    // do not realize that their lifetimes do not overlap, so that makes for a large stack frame.
    // So I avoid that by outside declarations (sigh).
    char offsetc, valc;
    unsigned char argNumc;
    unsigned short argNums;
    INT32 vali;
    INT64 vall;
    InterpreterType it;
    size_t sz;

    unsigned short ops;

    // Make sure that the .cctor for the current method's class has been run.
    MethodDesc* pMD = reinterpret_cast<MethodDesc*>(m_methInfo->m_method);
    EnsureClassInit(pMD->GetMethodTable());

#if INTERP_TRACING
    const char* methName = eeGetMethodFullName(m_methInfo->m_method);
    unsigned ilOffset = 0;

    unsigned curInvocation = InterlockedIncrement(&s_totalInvocations);
    if (s_TraceInterpreterEntriesFlag.val(CLRConfig::INTERNAL_TraceInterpreterEntries))
    {
        fprintf(GetLogFile(), "Entering method #%d (= 0x%x): %s.\n", curInvocation, curInvocation, methName);
        fprintf(GetLogFile(), " arguments:\n");
        PrintArgs();
    }
#endif // INTERP_TRACING

#if LOOPS_VIA_INSTRS
    unsigned instrs = 0;
#else
#if INTERP_PROFILE
    unsigned instrs = 0;
#endif
#endif

EvalLoop:
    GCX_ASSERT_COOP();
    // Catch any exceptions raised.
    EX_TRY {
        // Optional features...
#define INTERPRETER_CHECK_LARGE_STRUCT_STACK_HEIGHT 1

#if INTERP_ILCYCLE_PROFILE
    m_instr = CEE_COUNT; // Flag to indicate first instruction.
    m_exemptCycles = 0;
#endif // INTERP_ILCYCLE_PROFILE

    DoMonitorEnterWork();

    INTERPLOG("START %d, %s\n", m_methInfo->m_stubNum, methName);
    for (;;)
    {
        // TODO: verify that m_ILCodePtr is legal, and we haven't walked off the end of the IL array? (i.e., bad IL).
        // Note that ExecuteBranch() should be called for every branch. That checks that we aren't either before or
        // after the IL range. Here, we would only need to check that we haven't gone past the end (not before the beginning)
        // because everything that doesn't call ExecuteBranch() should only add to m_ILCodePtr.

#if INTERP_TRACING
        ilOffset = CurOffset();
#endif // _DEBUG
#if INTERP_TRACING
        if (s_TraceInterpreterOstackFlag.val(CLRConfig::INTERNAL_TraceInterpreterOstack))
        {
            PrintOStack();
        }
#if INTERPRETER_CHECK_LARGE_STRUCT_STACK_HEIGHT
        _ASSERTE_MSG(LargeStructStackHeightIsValid(), "Large structure stack height invariant violated."); // Check the large struct stack invariant.
#endif
        if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
        {
            fprintf(GetLogFile(), "  %#4x: %s\n", ilOffset, ILOp(m_ILCodePtr));
            fflush(GetLogFile());
        }
#endif // INTERP_TRACING
#if LOOPS_VIA_INSTRS
        instrs++;
#else
#if INTERP_PROFILE
        instrs++;
#endif
#endif

#if INTERP_ILINSTR_PROFILE
#if INTERP_ILCYCLE_PROFILE
        UpdateCycleCount();
#endif // INTERP_ILCYCLE_PROFILE

        InterlockedIncrement(&s_ILInstrExecs[*m_ILCodePtr]);
#endif // INTERP_ILINSTR_PROFILE

        switch (*m_ILCodePtr)
        {
        case CEE_NOP:
            m_ILCodePtr++;
            continue;
        case CEE_BREAK:     // TODO: interact with the debugger?
            m_ILCodePtr++;
            continue;
        case CEE_LDARG_0:
            LdArg(0);
            break;
        case CEE_LDARG_1:
            LdArg(1);
            break;
        case CEE_LDARG_2:
            LdArg(2);
            break;
        case CEE_LDARG_3:
            LdArg(3);
            break;
        case CEE_LDLOC_0:
            LdLoc(0);
            m_ILCodePtr++;
            continue;
        case CEE_LDLOC_1:
            LdLoc(1);
            break;
        case CEE_LDLOC_2:
            LdLoc(2);
            break;
        case CEE_LDLOC_3:
            LdLoc(3);
            break;
        case CEE_STLOC_0:
            StLoc(0);
            break;
        case CEE_STLOC_1:
            StLoc(1);
            break;
        case CEE_STLOC_2:
            StLoc(2);
            break;
        case CEE_STLOC_3:
            StLoc(3);
            break;
        case CEE_LDARG_S:
            m_ILCodePtr++;
            argNumc = *m_ILCodePtr;
            LdArg(argNumc);
            break;
        case CEE_LDARGA_S:
            m_ILCodePtr++;
            argNumc = *m_ILCodePtr;
            LdArgA(argNumc);
            break;
        case CEE_STARG_S:
            m_ILCodePtr++;
            argNumc = *m_ILCodePtr;
            StArg(argNumc);
            break;
        case CEE_LDLOC_S:
            argNumc = *(m_ILCodePtr + 1);
            LdLoc(argNumc);
            m_ILCodePtr += 2;
            continue;
        case CEE_LDLOCA_S:
            m_ILCodePtr++;
            argNumc = *m_ILCodePtr;
            LdLocA(argNumc);
            break;
        case CEE_STLOC_S:
            argNumc = *(m_ILCodePtr + 1);
            StLoc(argNumc);
            m_ILCodePtr += 2;
            continue;
        case CEE_LDNULL:
            LdNull();
            break;
        case CEE_LDC_I4_M1:
            LdIcon(-1);
            break;
        case CEE_LDC_I4_0:
            LdIcon(0);
            break;
        case CEE_LDC_I4_1:
            LdIcon(1);
            m_ILCodePtr++;
            continue;
        case CEE_LDC_I4_2:
            LdIcon(2);
            break;
        case CEE_LDC_I4_3:
            LdIcon(3);
            break;
        case CEE_LDC_I4_4:
            LdIcon(4);
            break;
        case CEE_LDC_I4_5:
            LdIcon(5);
            break;
        case CEE_LDC_I4_6:
            LdIcon(6);
            break;
        case CEE_LDC_I4_7:
            LdIcon(7);
            break;
        case CEE_LDC_I4_8:
            LdIcon(8);
            break;
        case CEE_LDC_I4_S:
            valc = getI1(m_ILCodePtr + 1);
            LdIcon(valc);
            m_ILCodePtr += 2;
            continue;
        case CEE_LDC_I4:
            vali = getI4LittleEndian(m_ILCodePtr + 1);
            LdIcon(vali);
            m_ILCodePtr += 5;
            continue;
        case CEE_LDC_I8:
            vall = getI8LittleEndian(m_ILCodePtr + 1);
            LdLcon(vall);
            m_ILCodePtr += 9;
            continue;
        case CEE_LDC_R4:
            // We use I4 here because we just care about the bit pattern.
            // LdR4Con will push the right InterpreterType.
            vali = getI4LittleEndian(m_ILCodePtr + 1);
            LdR4con(vali);
            m_ILCodePtr += 5;
            continue;
        case CEE_LDC_R8:
            // We use I4 here because we just care about the bit pattern.
            // LdR8Con will push the right InterpreterType.
            vall = getI8LittleEndian(m_ILCodePtr + 1);
            LdR8con(vall);
            m_ILCodePtr += 9;
            continue;
        case CEE_DUP:
            _ASSERTE(m_curStackHt > 0);
            it = OpStackTypeGet(m_curStackHt - 1);
            OpStackTypeSet(m_curStackHt, it);
            if (it.IsLargeStruct(&m_interpCeeInfo))
            {
                sz = it.Size(&m_interpCeeInfo);
                void* dest = LargeStructOperandStackPush(sz);
                memcpy(dest, OpStackGet<void*>(m_curStackHt - 1), sz);
                OpStackSet<void*>(m_curStackHt, dest);
            }
            else
            {
                OpStackSet<INT64>(m_curStackHt, OpStackGet<INT64>(m_curStackHt - 1));
            }
            m_curStackHt++;
            break;
        case CEE_POP:
            _ASSERTE(m_curStackHt > 0);
            m_curStackHt--;
            it = OpStackTypeGet(m_curStackHt);
            if (it.IsLargeStruct(&m_interpCeeInfo))
            {
                LargeStructOperandStackPop(it.Size(&m_interpCeeInfo), OpStackGet<void*>(m_curStackHt));
            }
            break;

        case CEE_JMP:
            *pJmpCallToken = getU4LittleEndian(m_ILCodePtr + sizeof(BYTE));
            *pDoJmpCall = true;
            goto ExitEvalLoop;

        case CEE_CALL:
            DoCall(/*virtualCall*/false);
#if INTERP_TRACING
            if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
            {
                fprintf(GetLogFile(), "  Returning to method %s, stub num %d.\n", methName, m_methInfo->m_stubNum);
            }
#endif // INTERP_TRACING
            continue;

        case CEE_CALLVIRT:
            DoCall(/*virtualCall*/true);
#if INTERP_TRACING
            if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
            {
                fprintf(GetLogFile(), "  Returning to method %s, stub num %d.\n", methName, m_methInfo->m_stubNum);
            }
#endif // INTERP_TRACING
            continue;

            // HARD
        case CEE_CALLI:
            CallI();
            continue;

        case CEE_RET:
            if (m_methInfo->m_returnType == CORINFO_TYPE_VOID)
            {
                _ASSERTE(m_curStackHt == 0);
            }
            else
            {
                _ASSERTE(m_curStackHt == 1);
                InterpreterType retValIt = OpStackTypeGet(0);
                bool looseInt = s_InterpreterLooseRules &&
                    CorInfoTypeIsIntegral(m_methInfo->m_returnType) &&
                    (CorInfoTypeIsIntegral(retValIt.ToCorInfoType()) || CorInfoTypeIsPointer(retValIt.ToCorInfoType())) &&
                    (m_methInfo->m_returnType != retValIt.ToCorInfoType());

                bool looseFloat = s_InterpreterLooseRules &&
                    CorInfoTypeIsFloatingPoint(m_methInfo->m_returnType) &&
                    CorInfoTypeIsFloatingPoint(retValIt.ToCorInfoType()) &&
                    (m_methInfo->m_returnType != retValIt.ToCorInfoType());

                // Make sure that the return value "matches" (which allows certain relaxations) the declared return type.
                _ASSERTE((m_methInfo->m_returnType == CORINFO_TYPE_VALUECLASS && retValIt.ToCorInfoType() == CORINFO_TYPE_VALUECLASS) ||
                       (m_methInfo->m_returnType == CORINFO_TYPE_REFANY && retValIt.ToCorInfoType() == CORINFO_TYPE_VALUECLASS) ||
                       (m_methInfo->m_returnType == CORINFO_TYPE_REFANY && retValIt.ToCorInfoType() == CORINFO_TYPE_REFANY) ||
                       (looseInt || looseFloat) ||
                      InterpreterType(m_methInfo->m_returnType).StackNormalize().Matches(retValIt, &m_interpCeeInfo));

                size_t sz = retValIt.Size(&m_interpCeeInfo);
#if defined(FEATURE_HFA)
                CorInfoHFAElemType cit = CORINFO_HFA_ELEM_NONE;
                {
                    GCX_PREEMP();
                    if(m_methInfo->m_returnType == CORINFO_TYPE_VALUECLASS)
                        cit = m_interpCeeInfo.getHFAType(retValIt.ToClassHandle());
                }
#endif
                if (m_methInfo->GetFlag<InterpreterMethodInfo::Flag_hasRetBuffArg>())
                {
                    _ASSERTE((m_methInfo->m_returnType == CORINFO_TYPE_VALUECLASS && retValIt.ToCorInfoType() == CORINFO_TYPE_VALUECLASS) ||
                       (m_methInfo->m_returnType == CORINFO_TYPE_REFANY && retValIt.ToCorInfoType() == CORINFO_TYPE_VALUECLASS) ||
                       (m_methInfo->m_returnType == CORINFO_TYPE_REFANY && retValIt.ToCorInfoType() == CORINFO_TYPE_REFANY));
                    if (retValIt.ToCorInfoType() == CORINFO_TYPE_REFANY)
                    {
                        InterpreterType typedRefIT = GetTypedRefIT(&m_interpCeeInfo);
                        TypedByRef* ptr = OpStackGet<TypedByRef*>(0);
                        *((TypedByRef*) m_retBufArg) = *ptr;
                    }
                    else if (retValIt.IsLargeStruct(&m_interpCeeInfo))
                    {
                        MethodTable* clsMt = GetMethodTableFromClsHnd(retValIt.ToClassHandle());
                        // The ostack value is a pointer to the struct value.
                        CopyValueClassUnchecked(m_retBufArg, OpStackGet<void*>(0), clsMt);
                    }
                    else
                    {
                        MethodTable* clsMt = GetMethodTableFromClsHnd(retValIt.ToClassHandle());
                        // The ostack value *is* the struct value.
                        CopyValueClassUnchecked(m_retBufArg, OpStackGetAddr(0, sz), clsMt);
                    }
                }
#if defined(FEATURE_HFA)
                // Is it an HFA?
                else if (m_methInfo->m_returnType == CORINFO_TYPE_VALUECLASS
                         && (cit != CORINFO_HFA_ELEM_NONE)
                         && (MetaSig(reinterpret_cast<MethodDesc*>(m_methInfo->m_method)).GetCallingConventionInfo() & CORINFO_CALLCONV_VARARG) == 0)
                {
                    if (retValIt.IsLargeStruct(&m_interpCeeInfo))
                    {
                        // The ostack value is a pointer to the struct value.
                        memcpy(GetHFARetBuffAddr(static_cast<unsigned>(sz)), OpStackGet<void*>(0), sz);
                    }
                    else
                    {
                        // The ostack value *is* the struct value.
                        memcpy(GetHFARetBuffAddr(static_cast<unsigned>(sz)), OpStackGetAddr(0, sz), sz);
                    }
                }
#elif defined(UNIX_AMD64_ABI)
                // Is it an struct contained in $rax and $rdx
                else if (m_methInfo->m_returnType == CORINFO_TYPE_VALUECLASS
                         && sz == 16)
                {
                    //The Fixed Two slot return buffer address
                    memcpy(m_ilArgs-16, OpStackGet<void*>(0), sz);
                }
#endif
                else if (CorInfoTypeIsFloatingPoint(m_methInfo->m_returnType) &&
                    CorInfoTypeIsFloatingPoint(retValIt.ToCorInfoType()))
                {
                    double val = (sz <= sizeof(INT32)) ? OpStackGet<float>(0) : OpStackGet<double>(0);
                    if (m_methInfo->m_returnType == CORINFO_TYPE_DOUBLE)
                    {
                        memcpy(retVal, &val, sizeof(double));
                    }
                    else
                    {
                        float val2 = (float) val;
                        memcpy(retVal, &val2, sizeof(float));
                    }
                }
                else
                {
                    if (sz <= sizeof(INT32))
                    {
                        *retVal = OpStackGet<INT32>(0);
                    }
                    else
                    {
                        // If looseInt is true, we are relying on auto-downcast in case *retVal
                        // is small (but this is guaranteed not to happen by def'n of ARG_SLOT.)
                        //
                        // Note structs of size 5, 6, 7 may be returned as 8 byte ints.
                        _ASSERTE(sz <= sizeof(INT64));
                        *retVal = OpStackGet<INT64>(0);
                    }
                }
            }


#if INTERP_PROFILE
            // We're not capturing instructions executed in a method that terminates via exception,
            // but that's OK...
            m_methInfo->RecordExecInstrs(instrs);
#endif
#if INTERP_TRACING
            // We keep this live until we leave.
            delete methName;
#endif // INTERP_TRACING

#if INTERP_ILCYCLE_PROFILE
            // Finish off accounting for the "RET" before we return
            UpdateCycleCount();
#endif // INTERP_ILCYCLE_PROFILE

            goto ExitEvalLoop;

        case CEE_BR_S:
            m_ILCodePtr++;
            offsetc = *m_ILCodePtr;
            // The offset is wrt the beginning of the following instruction, so the +1 is to get to that
            // m_ILCodePtr value before adding the offset.
            ExecuteBranch(m_ILCodePtr + offsetc + 1);
            continue; // Skip the default m_ILCodePtr++ at bottom of loop.

        case CEE_LEAVE_S:
            // LEAVE empties the operand stack.
            m_curStackHt = 0;
            m_largeStructOperandStackHt = 0;
            offsetc = getI1(m_ILCodePtr + 1);

            {
                // The offset is wrt the beginning of the following instruction, so the +2 is to get to that
                // m_ILCodePtr value before adding the offset.
                BYTE* leaveTarget = m_ILCodePtr + offsetc + 2;
                unsigned leaveOffset = CurOffset();
                m_leaveInfoStack.Push(LeaveInfo(leaveOffset, leaveTarget));
                if (!SearchForCoveringFinally())
                {
                    m_leaveInfoStack.Pop();
                    ExecuteBranch(leaveTarget);
                }
            }
            continue; // Skip the default m_ILCodePtr++ at bottom of loop.

            // Abstract the next pair out to something common with templates.
        case CEE_BRFALSE_S:
            BrOnValue<false, 1>();
            continue;

        case CEE_BRTRUE_S:
            BrOnValue<true, 1>();
            continue;

        case CEE_BEQ_S:
            BrOnComparison<CO_EQ, false, 1>();
            continue;
        case CEE_BGE_S:
            _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_LT_UN, true, 1>();
                break;
            default:
                BrOnComparison<CO_LT, true, 1>();
                break;
            }
            continue;
        case CEE_BGT_S:
            BrOnComparison<CO_GT, false, 1>();
            continue;
        case CEE_BLE_S:
            _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_GT_UN, true, 1>();
                break;
            default:
                BrOnComparison<CO_GT, true, 1>();
                break;
            }
            continue;
        case CEE_BLT_S:
            BrOnComparison<CO_LT, false, 1>();
            continue;
        case CEE_BNE_UN_S:
            BrOnComparison<CO_EQ, true, 1>();
            continue;
        case CEE_BGE_UN_S:
            _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_LT, true, 1>();
                break;
            default:
                BrOnComparison<CO_LT_UN, true, 1>();
                break;
            }
            continue;
        case CEE_BGT_UN_S:
            BrOnComparison<CO_GT_UN, false, 1>();
            continue;
        case CEE_BLE_UN_S:
            _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_GT, true, 1>();
                break;
            default:
                BrOnComparison<CO_GT_UN, true, 1>();
                break;
            }
            continue;
        case CEE_BLT_UN_S:
            BrOnComparison<CO_LT_UN, false, 1>();
            continue;

        case CEE_BR:
            m_ILCodePtr++;
            vali = getI4LittleEndian(m_ILCodePtr);
            vali += 4; // +4 for the length of the offset.
            ExecuteBranch(m_ILCodePtr + vali);
            if (vali < 0)
            {
                // Backwards branch -- enable caching.
                BackwardsBranchActions(vali);
            }

            continue;

        case CEE_LEAVE:
            // LEAVE empties the operand stack.
            m_curStackHt = 0;
            m_largeStructOperandStackHt = 0;
            vali = getI4LittleEndian(m_ILCodePtr + 1);

            {
                // The offset is wrt the beginning of the following instruction, so the +5 is to get to that
                // m_ILCodePtr value before adding the offset.
                BYTE* leaveTarget =  m_ILCodePtr + (vali + 5);
                unsigned leaveOffset = CurOffset();
                m_leaveInfoStack.Push(LeaveInfo(leaveOffset, leaveTarget));
                if (!SearchForCoveringFinally())
                {
                    (void)m_leaveInfoStack.Pop();
                    if (vali < 0)
                    {
                        // Backwards branch -- enable caching.
                        BackwardsBranchActions(vali);
                    }
                    ExecuteBranch(leaveTarget);
                }
            }
            continue; // Skip the default m_ILCodePtr++ at bottom of loop.

        case CEE_BRFALSE:
            BrOnValue<false, 4>();
            continue;
        case CEE_BRTRUE:
            BrOnValue<true, 4>();
            continue;

        case CEE_BEQ:
            BrOnComparison<CO_EQ, false, 4>();
            continue;
        case CEE_BGE:
            _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_LT_UN, true, 4>();
                break;
            default:
                BrOnComparison<CO_LT, true, 4>();
                break;
            }
            continue;
        case CEE_BGT:
            BrOnComparison<CO_GT, false, 4>();
            continue;
        case CEE_BLE:
            _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_GT_UN, true, 4>();
                break;
            default:
                BrOnComparison<CO_GT, true, 4>();
                break;
            }
            continue;
        case CEE_BLT:
            BrOnComparison<CO_LT, false, 4>();
            continue;
        case CEE_BNE_UN:
            BrOnComparison<CO_EQ, true, 4>();
            continue;
        case CEE_BGE_UN:
            _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_LT, true, 4>();
                break;
            default:
                BrOnComparison<CO_LT_UN, true, 4>();
                break;
            }
            continue;
        case CEE_BGT_UN:
            BrOnComparison<CO_GT_UN, false, 4>();
            continue;
        case CEE_BLE_UN:
             _ASSERTE(m_curStackHt >= 2);
            // ECMA spec gives different semantics for different operand types:
            switch (OpStackTypeGet(m_curStackHt-1).ToCorInfoType())
            {
            case CORINFO_TYPE_FLOAT:
            case CORINFO_TYPE_DOUBLE:
                BrOnComparison<CO_GT, true, 4>();
                break;
            default:
                BrOnComparison<CO_GT_UN, true, 4>();
                break;
            }
            continue;
        case CEE_BLT_UN:
            BrOnComparison<CO_LT_UN, false, 4>();
            continue;

        case CEE_SWITCH:
            {
                _ASSERTE(m_curStackHt > 0);
                m_curStackHt--;
#if defined(_DEBUG) || defined(HOST_AMD64)
                CorInfoType cit = OpStackTypeGet(m_curStackHt).ToCorInfoType();
#endif // _DEBUG || HOST_AMD64
#ifdef _DEBUG
                _ASSERTE(cit == CORINFO_TYPE_INT || cit == CORINFO_TYPE_UINT || cit == CORINFO_TYPE_NATIVEINT);
#endif // _DEBUG
#if defined(HOST_AMD64)
                UINT32 val = (cit == CORINFO_TYPE_NATIVEINT) ? (INT32) OpStackGet<NativeInt>(m_curStackHt)
                                                             : OpStackGet<INT32>(m_curStackHt);
#else
                UINT32 val = OpStackGet<INT32>(m_curStackHt);
#endif
                UINT32 n = getU4LittleEndian(m_ILCodePtr + 1);
                UINT32 instrSize = 1 + (n + 1)*4;
                if (val < n)
                {
                    vali = getI4LittleEndian(m_ILCodePtr + (5 + val * 4));
                    ExecuteBranch(m_ILCodePtr + instrSize + vali);
                }
                else
                {
                    m_ILCodePtr += instrSize;
                }
            }
            continue;

        case CEE_LDIND_I1:
            LdIndShort<INT8, /*isUnsigned*/false>();
            break;
        case CEE_LDIND_U1:
            LdIndShort<UINT8, /*isUnsigned*/true>();
            break;
        case CEE_LDIND_I2:
            LdIndShort<INT16, /*isUnsigned*/false>();
            break;
        case CEE_LDIND_U2:
            LdIndShort<UINT16, /*isUnsigned*/true>();
            break;
        case CEE_LDIND_I4:
            LdInd<INT32, CORINFO_TYPE_INT>();
            break;
        case CEE_LDIND_U4:
            LdInd<UINT32, CORINFO_TYPE_INT>();
            break;
        case CEE_LDIND_I8:
            LdInd<INT64, CORINFO_TYPE_LONG>();
            break;
        case CEE_LDIND_I:
            LdInd<NativeInt, CORINFO_TYPE_NATIVEINT>();
            break;
        case CEE_LDIND_R4:
            LdInd<float, CORINFO_TYPE_FLOAT>();
            break;
        case CEE_LDIND_R8:
            LdInd<double, CORINFO_TYPE_DOUBLE>();
            break;
        case CEE_LDIND_REF:
            LdInd<Object*, CORINFO_TYPE_CLASS>();
            break;
        case CEE_STIND_REF:
            StInd_Ref();
            break;
        case CEE_STIND_I1:
            StInd<INT8>();
            break;
        case CEE_STIND_I2:
            StInd<INT16>();
            break;
        case CEE_STIND_I4:
            StInd<INT32>();
            break;
        case CEE_STIND_I8:
            StInd<INT64>();
            break;
        case CEE_STIND_R4:
            StInd<float>();
            break;
        case CEE_STIND_R8:
            StInd<double>();
            break;
        case CEE_ADD:
            BinaryArithOp<BA_Add>();
            m_ILCodePtr++;
            continue;
        case CEE_SUB:
            BinaryArithOp<BA_Sub>();
            break;
        case CEE_MUL:
            BinaryArithOp<BA_Mul>();
            break;
        case CEE_DIV:
            BinaryArithOp<BA_Div>();
            break;
        case CEE_DIV_UN:
            BinaryIntOp<BIO_DivUn>();
            break;
        case CEE_REM:
            BinaryArithOp<BA_Rem>();
            break;
        case CEE_REM_UN:
            BinaryIntOp<BIO_RemUn>();
            break;
        case CEE_AND:
            BinaryIntOp<BIO_And>();
            break;
        case CEE_OR:
            BinaryIntOp<BIO_Or>();
            break;
        case CEE_XOR:
            BinaryIntOp<BIO_Xor>();
            break;
        case CEE_SHL:
            ShiftOp<CEE_SHL>();
            break;
        case CEE_SHR:
            ShiftOp<CEE_SHR>();
            break;
        case CEE_SHR_UN:
            ShiftOp<CEE_SHR_UN>();
            break;
        case CEE_NEG:
            Neg();
            break;
        case CEE_NOT:
            Not();
            break;
        case CEE_CONV_I1:
            Conv<INT8, /*TIsUnsigned*/false, /*TCanHoldPtr*/false, /*TIsShort*/true, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_I2:
            Conv<INT16, /*TIsUnsigned*/false, /*TCanHoldPtr*/false, /*TIsShort*/true, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_I4:
            Conv<INT32, /*TIsUnsigned*/false, /*TCanHoldPtr*/false, /*TIsShort*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_I8:
            Conv<INT64, /*TIsUnsigned*/false, /*TCanHoldPtr*/true, /*TIsShort*/false, CORINFO_TYPE_LONG>();
            break;
        case CEE_CONV_R4:
            Conv<float, /*TIsUnsigned*/false, /*TCanHoldPtr*/false, /*TIsShort*/false, CORINFO_TYPE_FLOAT>();
            break;
        case CEE_CONV_R8:
            Conv<double, /*TIsUnsigned*/false, /*TCanHoldPtr*/false, /*TIsShort*/false, CORINFO_TYPE_DOUBLE>();
            break;
        case CEE_CONV_U4:
            Conv<UINT32, /*TIsUnsigned*/true, /*TCanHoldPtr*/false, /*TIsShort*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_U8:
            Conv<UINT64, /*TIsUnsigned*/true, /*TCanHoldPtr*/true, /*TIsShort*/false, CORINFO_TYPE_LONG>();
            break;

        case CEE_CPOBJ:
            CpObj();
            continue;
        case CEE_LDOBJ:
            LdObj();
            continue;
        case CEE_LDSTR:
            LdStr();
            continue;
        case CEE_NEWOBJ:
            NewObj();
#if INTERP_TRACING
            if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
            {
                fprintf(GetLogFile(), "  Returning to method %s, stub num %d.\n", methName, m_methInfo->m_stubNum);
            }
#endif // INTERP_TRACING
            continue;
        case CEE_CASTCLASS:
            CastClass();
            continue;
        case CEE_ISINST:
            IsInst();
            continue;
        case CEE_CONV_R_UN:
            ConvRUn();
            break;
        case CEE_UNBOX:
            Unbox();
            continue;
        case CEE_THROW:
            Throw();
            break;
        case CEE_LDFLD:
            LdFld();
            continue;
        case CEE_LDFLDA:
            LdFldA();
            continue;
        case CEE_STFLD:
            StFld();
            continue;
        case CEE_LDSFLD:
            LdSFld();
            continue;
        case CEE_LDSFLDA:
            LdSFldA();
            continue;
        case CEE_STSFLD:
            StSFld();
            continue;
        case CEE_STOBJ:
            StObj();
            continue;
        case CEE_CONV_OVF_I1_UN:
            ConvOvfUn<INT8, SCHAR_MIN, SCHAR_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_I2_UN:
            ConvOvfUn<INT16, SHRT_MIN, SHRT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_I4_UN:
            ConvOvfUn<INT32, INT_MIN, INT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_I8_UN:
            ConvOvfUn<INT64, _I64_MIN, _I64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_LONG>();
            break;
        case CEE_CONV_OVF_U1_UN:
            ConvOvfUn<UINT8, 0, UCHAR_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_U2_UN:
            ConvOvfUn<UINT16, 0, USHRT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_U4_UN:
            ConvOvfUn<UINT32, 0, UINT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_U8_UN:
            ConvOvfUn<UINT64, 0, _UI64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_LONG>();
            break;
        case CEE_CONV_OVF_I_UN:
            if (sizeof(NativeInt) == 4)
            {
                ConvOvfUn<NativeInt, INT_MIN, INT_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            else
            {
                _ASSERTE(sizeof(NativeInt) == 8);
                ConvOvfUn<NativeInt, _I64_MIN, _I64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            break;
        case CEE_CONV_OVF_U_UN:
            if (sizeof(NativeUInt) == 4)
            {
                ConvOvfUn<NativeUInt, 0, UINT_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            else
            {
                _ASSERTE(sizeof(NativeUInt) == 8);
                ConvOvfUn<NativeUInt, 0, _UI64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            break;
        case CEE_BOX:
            Box();
            continue;
        case CEE_NEWARR:
            NewArr();
            continue;
        case CEE_LDLEN:
            LdLen();
            break;
        case CEE_LDELEMA:
            LdElem</*takeAddr*/true>();
            continue;
        case CEE_LDELEM_I1:
            LdElemWithType<INT8, false, CORINFO_TYPE_INT>();
            break;
        case CEE_LDELEM_U1:
            LdElemWithType<UINT8, false, CORINFO_TYPE_INT>();
            break;
        case CEE_LDELEM_I2:
            LdElemWithType<INT16, false, CORINFO_TYPE_INT>();
            break;
        case CEE_LDELEM_U2:
            LdElemWithType<UINT16, false, CORINFO_TYPE_INT>();
            break;
        case CEE_LDELEM_I4:
            LdElemWithType<INT32, false, CORINFO_TYPE_INT>();
            break;
        case CEE_LDELEM_U4:
            LdElemWithType<UINT32, false, CORINFO_TYPE_INT>();
            break;
        case CEE_LDELEM_I8:
            LdElemWithType<INT64, false, CORINFO_TYPE_LONG>();
            break;
            // Note that the ECMA spec defines a "LDELEM_U8", but it is the same instruction number as LDELEM_I8 (since
            // when loading to the widest width, signed/unsigned doesn't matter).
        case CEE_LDELEM_I:
            LdElemWithType<NativeInt, false, CORINFO_TYPE_NATIVEINT>();
            break;
        case CEE_LDELEM_R4:
            LdElemWithType<float, false, CORINFO_TYPE_FLOAT>();
            break;
        case CEE_LDELEM_R8:
            LdElemWithType<double, false, CORINFO_TYPE_DOUBLE>();
            break;
        case CEE_LDELEM_REF:
            LdElemWithType<Object*, true, CORINFO_TYPE_CLASS>();
            break;
        case CEE_STELEM_I:
            StElemWithType<NativeInt, false>();
            break;
        case CEE_STELEM_I1:
            StElemWithType<INT8, false>();
            break;
        case CEE_STELEM_I2:
            StElemWithType<INT16, false>();
            break;
        case CEE_STELEM_I4:
            StElemWithType<INT32, false>();
            break;
        case CEE_STELEM_I8:
            StElemWithType<INT64, false>();
            break;
        case CEE_STELEM_R4:
            StElemWithType<float, false>();
            break;
        case CEE_STELEM_R8:
            StElemWithType<double, false>();
            break;
        case CEE_STELEM_REF:
            StElemWithType<Object*, true>();
            break;
        case CEE_LDELEM:
            LdElem</*takeAddr*/false>();
            continue;
        case CEE_STELEM:
            StElem();
            continue;
        case CEE_UNBOX_ANY:
            UnboxAny();
            continue;
        case CEE_CONV_OVF_I1:
            ConvOvf<INT8, SCHAR_MIN, SCHAR_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_U1:
            ConvOvf<UINT8, 0, UCHAR_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_I2:
            ConvOvf<INT16, SHRT_MIN, SHRT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_U2:
            ConvOvf<UINT16, 0, USHRT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_I4:
            ConvOvf<INT32, INT_MIN, INT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_U4:
            ConvOvf<UINT32, 0, UINT_MAX, /*TCanHoldPtr*/false, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_OVF_I8:
            ConvOvf<INT64, _I64_MIN, _I64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_LONG>();
            break;
        case CEE_CONV_OVF_U8:
            ConvOvf<UINT64, 0, _UI64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_LONG>();
            break;
        case CEE_REFANYVAL:
            RefanyVal();
            continue;
        case CEE_CKFINITE:
            CkFinite();
            break;
        case CEE_MKREFANY:
            MkRefany();
            continue;
        case CEE_LDTOKEN:
            LdToken();
            continue;
        case CEE_CONV_U2:
            Conv<UINT16, /*TIsUnsigned*/true, /*TCanHoldPtr*/false, /*TIsShort*/true, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_U1:
            Conv<UINT8, /*TIsUnsigned*/true, /*TCanHoldPtr*/false, /*TIsShort*/true, CORINFO_TYPE_INT>();
            break;
        case CEE_CONV_I:
            Conv<NativeInt, /*TIsUnsigned*/false, /*TCanHoldPtr*/true, /*TIsShort*/false, CORINFO_TYPE_NATIVEINT>();
            break;
        case CEE_CONV_OVF_I:
            if (sizeof(NativeInt) == 4)
            {
                ConvOvf<NativeInt, INT_MIN, INT_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            else
            {
                _ASSERTE(sizeof(NativeInt) == 8);
                ConvOvf<NativeInt, _I64_MIN, _I64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            break;
        case CEE_CONV_OVF_U:
            if (sizeof(NativeUInt) == 4)
            {
                ConvOvf<NativeUInt, 0, UINT_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            else
            {
                _ASSERTE(sizeof(NativeUInt) == 8);
                ConvOvf<NativeUInt, 0, _UI64_MAX, /*TCanHoldPtr*/true, CORINFO_TYPE_NATIVEINT>();
            }
            break;
        case CEE_ADD_OVF:
            BinaryArithOvfOp<BA_Add, /*asUnsigned*/false>();
            break;
        case CEE_ADD_OVF_UN:
            BinaryArithOvfOp<BA_Add, /*asUnsigned*/true>();
            break;
        case CEE_MUL_OVF:
            BinaryArithOvfOp<BA_Mul, /*asUnsigned*/false>();
            break;
        case CEE_MUL_OVF_UN:
            BinaryArithOvfOp<BA_Mul, /*asUnsigned*/true>();
            break;
        case CEE_SUB_OVF:
            BinaryArithOvfOp<BA_Sub, /*asUnsigned*/false>();
            break;
        case CEE_SUB_OVF_UN:
            BinaryArithOvfOp<BA_Sub, /*asUnsigned*/true>();
            break;
        case CEE_ENDFINALLY:
            // We have just ended a finally.
            // If we were called during exception dispatch,
            // rethrow the exception on our way out.
            if (m_leaveInfoStack.IsEmpty())
            {
                Object* finallyException = NULL;

                {
                    GCX_FORBID();
                    _ASSERTE(m_inFlightException != NULL);
                    finallyException = m_inFlightException;
                    INTERPLOG("endfinally handling for %s, %p, %p\n", methName, m_methInfo, finallyException);
                    m_inFlightException = NULL;
                }

                COMPlusThrow(ObjectToOBJECTREF(finallyException));
                UNREACHABLE();
            }
            // Otherwise, see if there's another finally block to
            // execute as part of processing the current LEAVE...
            else if (!SearchForCoveringFinally())
            {
                // No, there isn't -- go to the leave target.
                _ASSERTE(!m_leaveInfoStack.IsEmpty());
                LeaveInfo li = m_leaveInfoStack.Pop();
                ExecuteBranch(li.m_target);
            }
            // Yes, there, is, and SearchForCoveringFinally set us up to start executing it.
            continue; // Skip the default m_ILCodePtr++ at bottom of loop.

        case CEE_STIND_I:
            StInd<NativeInt>();
            break;
        case CEE_CONV_U:
            Conv<NativeUInt, /*TIsUnsigned*/true, /*TCanHoldPtr*/true, /*TIsShort*/false, CORINFO_TYPE_NATIVEINT>();
            break;
        case CEE_PREFIX7:
            NYI_INTERP("Unimplemented opcode: CEE_PREFIX7");
            break;
        case CEE_PREFIX6:
            NYI_INTERP("Unimplemented opcode: CEE_PREFIX6");
            break;
        case CEE_PREFIX5:
            NYI_INTERP("Unimplemented opcode: CEE_PREFIX5");
            break;
        case CEE_PREFIX4:
            NYI_INTERP("Unimplemented opcode: CEE_PREFIX4");
            break;
        case CEE_PREFIX3:
            NYI_INTERP("Unimplemented opcode: CEE_PREFIX3");
            break;
        case CEE_PREFIX2:
            NYI_INTERP("Unimplemented opcode: CEE_PREFIX2");
            break;
        case CEE_PREFIX1:
            // This is the prefix for all the 2-byte opcodes.
            // Figure out the second byte of the 2-byte opcode.
            ops = *(m_ILCodePtr + 1);
#if INTERP_ILINSTR_PROFILE
            // Take one away from PREFIX1, which we won't count.
            InterlockedDecrement(&s_ILInstrExecs[CEE_PREFIX1]);
            // Credit instead to the 2-byte instruction index.
            InterlockedIncrement(&s_ILInstr2ByteExecs[ops]);
#endif // INTERP_ILINSTR_PROFILE
            switch (ops)
            {
            case TWOBYTE_CEE_ARGLIST:
                // NYI_INTERP("Unimplemented opcode: TWOBYTE_CEE_ARGLIST");
                _ASSERTE(m_methInfo->m_varArgHandleArgNum != NO_VA_ARGNUM);
                LdArgA(m_methInfo->m_varArgHandleArgNum);
                m_ILCodePtr += 2;
                break;

            case TWOBYTE_CEE_CEQ:
                CompareOp<CO_EQ>();
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_CGT:
                CompareOp<CO_GT>();
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_CGT_UN:
                CompareOp<CO_GT_UN>();
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_CLT:
                CompareOp<CO_LT>();
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_CLT_UN:
                CompareOp<CO_LT_UN>();
                m_ILCodePtr += 2;
                break;

            case TWOBYTE_CEE_LDARG:
                m_ILCodePtr += 2;
                argNums = getU2LittleEndian(m_ILCodePtr);
                LdArg(argNums);
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_LDARGA:
                m_ILCodePtr += 2;
                argNums = getU2LittleEndian(m_ILCodePtr);
                LdArgA(argNums);
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_STARG:
                m_ILCodePtr += 2;
                argNums = getU2LittleEndian(m_ILCodePtr);
                StArg(argNums);
                m_ILCodePtr += 2;
                break;

            case TWOBYTE_CEE_LDLOC:
                m_ILCodePtr += 2;
                argNums = getU2LittleEndian(m_ILCodePtr);
                LdLoc(argNums);
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_LDLOCA:
                m_ILCodePtr += 2;
                argNums = getU2LittleEndian(m_ILCodePtr);
                LdLocA(argNums);
                m_ILCodePtr += 2;
                break;
            case TWOBYTE_CEE_STLOC:
                m_ILCodePtr += 2;
                argNums = getU2LittleEndian(m_ILCodePtr);
                StLoc(argNums);
                m_ILCodePtr += 2;
                break;

            case TWOBYTE_CEE_CONSTRAINED:
                RecordConstrainedCall();
                break;

            case TWOBYTE_CEE_VOLATILE:
                // Set a flag that causes a memory barrier to be associated with the next load or store.
                m_volatileFlag = true;
                m_ILCodePtr += 2;
                break;

            case TWOBYTE_CEE_LDFTN:
                LdFtn();
                break;

            case TWOBYTE_CEE_INITOBJ:
                InitObj();
                break;

            case TWOBYTE_CEE_LOCALLOC:
                LocAlloc();
                m_ILCodePtr += 2;
                break;

            case TWOBYTE_CEE_LDVIRTFTN:
                LdVirtFtn();
                break;

            case TWOBYTE_CEE_SIZEOF:
                Sizeof();
                break;

            case TWOBYTE_CEE_RETHROW:
                Rethrow();
                break;

            case TWOBYTE_CEE_READONLY:
                m_readonlyFlag = true;
                m_ILCodePtr += 2;
                // A comment in importer.cpp indicates that READONLY may also apply to calls.  We'll see.
                _ASSERTE_MSG(*m_ILCodePtr == CEE_LDELEMA, "According to the ECMA spec, READONLY may only precede LDELEMA");
                break;

            case TWOBYTE_CEE_INITBLK:
                InitBlk();
                break;

            case TWOBYTE_CEE_CPBLK:
                CpBlk();
                break;

            case TWOBYTE_CEE_ENDFILTER:
                EndFilter();
                break;

            case TWOBYTE_CEE_UNALIGNED:
                // Nothing to do here.
                m_ILCodePtr += 3;
                break;

            case TWOBYTE_CEE_TAILCALL:
                // TODO: Needs revisiting when implementing tail call.
                // NYI_INTERP("Unimplemented opcode: TWOBYTE_CEE_TAILCALL");
                m_ILCodePtr += 2;
                break;

            case TWOBYTE_CEE_REFANYTYPE:
                RefanyType();
                break;

            default:
                UNREACHABLE();
                break;
            }
            continue;

        case CEE_PREFIXREF:
            NYI_INTERP("Unimplemented opcode: CEE_PREFIXREF");
            m_ILCodePtr++;
            continue;

        default:
            UNREACHABLE();
            continue;
        }
        m_ILCodePtr++;
    }
ExitEvalLoop:;
        INTERPLOG("DONE %d, %s\n", m_methInfo->m_stubNum, m_methInfo->m_methName);
    }
    EX_CATCH
    {
        INTERPLOG("EXCEPTION %d (throw), %s\n", m_methInfo->m_stubNum, m_methInfo->m_methName);

        bool handleException = false;
        OBJECTREF orThrowable = NULL;
        GCX_COOP_NO_DTOR();

        orThrowable = GET_THROWABLE();

        if (m_filterNextScan != 0)
        {
            // We are in the middle of a filter scan and an exception is thrown inside
            // a filter. We are supposed to swallow it and assume the filter did not
            // handle the exception.
            m_curStackHt = 0;
            m_largeStructOperandStackHt = 0;
            LdIcon(0);
            EndFilter();
            handleException = true;
        }
        else
        {
            // orThrowable must be protected.  MethodHandlesException() will place orThrowable
            // into the operand stack (a permanently protected area) if it returns true.
            GCPROTECT_BEGIN(orThrowable);
            handleException = MethodHandlesException(orThrowable);
            GCPROTECT_END();
        }

        if (handleException)
        {
            GetThread()->SafeSetThrowables(orThrowable
                DEBUG_ARG(ThreadExceptionState::STEC_CurrentTrackerEqualNullOkForInterpreter));
            goto EvalLoop;
        }
        else
        {
            INTERPLOG("EXCEPTION %d (rethrow), %s\n", m_methInfo->m_stubNum, m_methInfo->m_methName);
            EX_RETHROW;
        }
    }
    EX_END_CATCH(RethrowTransientExceptions)
}

#ifdef _MSC_VER
#pragma optimize("", on)
#endif

void Interpreter::EndFilter()
{
    unsigned handles = OpStackGet<unsigned>(0);
    // If the filter decides to handle the exception, then go to the handler offset.
    if (handles)
    {
        // We decided to handle the exception, so give all EH entries a chance to
        // handle future exceptions. Clear scan.
        m_filterNextScan = 0;
        ExecuteBranch(m_methInfo->m_ILCode + m_filterHandlerOffset);
    }
    // The filter decided not to handle the exception, ask if there is some other filter
    // lined up to try to handle it or some other catch/finally handlers will handle it.
    // If no one handles the exception, rethrow and be done with it.
    else
    {
        bool handlesEx = false;
        {
            OBJECTREF orThrowable = ObjectToOBJECTREF(m_inFlightException);
            GCPROTECT_BEGIN(orThrowable);
            handlesEx = MethodHandlesException(orThrowable);
            GCPROTECT_END();
        }
        if (!handlesEx)
        {
            // Just clear scan before rethrowing to give any EH entry a chance to handle
            // the "rethrow".
            m_filterNextScan = 0;
            Object* filterException = NULL;
            {
                GCX_FORBID();
                _ASSERTE(m_inFlightException != NULL);
                filterException = m_inFlightException;
                INTERPLOG("endfilter handling for %s, %p, %p\n", m_methInfo->m_methName, m_methInfo, filterException);
                m_inFlightException = NULL;
            }

            COMPlusThrow(ObjectToOBJECTREF(filterException));
            UNREACHABLE();
        }
        else
        {
            // Let it do another round of filter:end-filter or handler block.
            // During the next end filter, we will reuse m_filterNextScan and
            // continue searching where we left off. Note however, while searching,
            // any of the filters could throw an exception. But this is supposed to
            // be swallowed and endfilter should be called with a value of 0 on the
            // stack.
        }
    }
}

bool Interpreter::MethodHandlesException(OBJECTREF orThrowable)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    bool handlesEx = false;

    if (orThrowable != NULL)
    {
        // Don't catch ThreadAbort and other uncatchable exceptions
        if (!IsUncatchable(&orThrowable))
        {
            // Does the current method catch this?  The clauses are defined by offsets, so get that.
            // However, if we are in the middle of a filter scan, make sure we get the offset of the
            // excepting code, rather than the offset of the filter body.
            DWORD curOffset = (m_filterNextScan != 0) ? m_filterExcILOffset : CurOffset();
            TypeHandle orThrowableTH = TypeHandle(orThrowable->GetMethodTable());

            GCPROTECT_BEGIN(orThrowable);
            GCX_PREEMP();

            // Perform a filter scan or regular walk of the EH Table. Filter scan is performed when
            // we are evaluating a series of filters to handle the exception until the first handler
            // (filter's or otherwise) that will handle the exception.
            for (unsigned XTnum = m_filterNextScan; XTnum < m_methInfo->m_ehClauseCount; XTnum++)
            {
                CORINFO_EH_CLAUSE clause;
                m_interpCeeInfo.getEHinfo(m_methInfo->m_method, XTnum, &clause);
                _ASSERTE(clause.HandlerLength != (unsigned)-1); // @DEPRECATED

                // First, is the current offset in the try block?
                if (clause.TryOffset <= curOffset && curOffset < clause.TryOffset + clause.TryLength)
                {
                    unsigned handlerOffset = 0;
                    // CORINFO_EH_CLAUSE_NONE represents 'catch' blocks
                    if (clause.Flags == CORINFO_EH_CLAUSE_NONE)
                    {
                        // Now, does the catch block handle the thrown exception type?
                        CORINFO_CLASS_HANDLE excType = FindClass(clause.ClassToken InterpTracingArg(RTK_CheckHandlesException));
                        if (ExceptionIsOfRightType(TypeHandle::FromPtr(excType), orThrowableTH))
                        {
                            GCX_COOP();
                            // Push the exception object onto the operand stack.
                            OpStackSet<OBJECTREF>(0, orThrowable);
                            OpStackTypeSet(0, InterpreterType(CORINFO_TYPE_CLASS));
                            m_curStackHt = 1;
                            m_largeStructOperandStackHt = 0;
                            handlerOffset = clause.HandlerOffset;
                            handlesEx = true;
                            m_filterNextScan = 0;
                        }
                        else
                        {
                            GCX_COOP();
                            // Handle a wrapped exception.
                            OBJECTREF orUnwrapped = PossiblyUnwrapThrowable(orThrowable, GetMethodDesc()->GetAssembly());
                            if (ExceptionIsOfRightType(TypeHandle::FromPtr(excType), orUnwrapped->GetTypeHandle()))
                            {
                                // Push the exception object onto the operand stack.
                                OpStackSet<OBJECTREF>(0, orUnwrapped);
                                OpStackTypeSet(0, InterpreterType(CORINFO_TYPE_CLASS));
                                m_curStackHt = 1;
                                m_largeStructOperandStackHt = 0;
                                handlerOffset = clause.HandlerOffset;
                                handlesEx = true;
                                m_filterNextScan = 0;
                            }
                        }
                    }
                    else if (clause.Flags == CORINFO_EH_CLAUSE_FILTER)
                    {
                        GCX_COOP();
                        // Push the exception object onto the operand stack.
                        OpStackSet<OBJECTREF>(0, orThrowable);
                        OpStackTypeSet(0, InterpreterType(CORINFO_TYPE_CLASS));
                        m_curStackHt = 1;
                        m_largeStructOperandStackHt = 0;
                        handlerOffset = clause.FilterOffset;
                        m_inFlightException = OBJECTREFToObject(orThrowable);
                        handlesEx = true;
                        m_filterHandlerOffset = clause.HandlerOffset;
                        m_filterNextScan = XTnum + 1;
                        m_filterExcILOffset = curOffset;
                    }
                    else if (clause.Flags == CORINFO_EH_CLAUSE_FAULT ||
                            clause.Flags == CORINFO_EH_CLAUSE_FINALLY)
                    {
                        GCX_COOP();
                        // Save the exception object to rethrow.
                        m_inFlightException = OBJECTREFToObject(orThrowable);
                        // Empty the operand stack.
                        m_curStackHt = 0;
                        m_largeStructOperandStackHt = 0;
                        handlerOffset = clause.HandlerOffset;
                        handlesEx = true;
                        m_filterNextScan = 0;
                    }

                    // Reset the interpreter loop in preparation of calling the handler.
                    if (handlesEx)
                    {
                        // Set the IL offset of the handler.
                        ExecuteBranch(m_methInfo->m_ILCode + handlerOffset);

                        // If an exception occurs while attempting to leave a protected scope,
                        // we empty the 'leave' info stack upon entering the handler.
                        while (!m_leaveInfoStack.IsEmpty())
                        {
                            m_leaveInfoStack.Pop();
                        }

                        // Some things are set up before a call, and must be cleared on an exception caught be the caller.
                        // A method that returns a struct allocates local space for the return value, and "registers" that
                        // space and the type so that it's scanned if a GC happens.  "Unregister" it if we throw an exception
                        // in the call, and handle it in the caller.  (If it's not handled by the caller, the Interpreter is
                        // deallocated, so it's value doesn't matter.)
                        m_structRetValITPtr = NULL;
                        m_callThisArg = NULL;
                        m_argsSize = 0;

                        break;
                    }
                }
            }
            GCPROTECT_END();
        }
        if (!handlesEx)
        {
            DoMonitorExitWork();
        }
    }
    return handlesEx;
}

static unsigned OpFormatExtraSize(opcode_format_t format) {
    switch (format)
    {
    case InlineNone:
        return 0;
    case InlineVar:
        return 2;
    case InlineI:
    case InlineBrTarget:
    case InlineMethod:
    case InlineField:
    case InlineType:
    case InlineString:
    case InlineSig:
    case InlineRVA:
    case InlineTok:
    case ShortInlineR:
        return 4;

    case InlineR:
    case InlineI8:
        return 8;

    case InlineSwitch:
        return 0;  // We'll handle this specially.

    case ShortInlineVar:
    case ShortInlineI:
    case ShortInlineBrTarget:
        return 1;

    default:
        _ASSERTE(false);
        return 0;
    }
}



static unsigned opSizes1Byte[CEE_COUNT];
static bool     opSizes1ByteInit = false;

static void OpSizes1ByteInit()
{
    if (opSizes1ByteInit) return;
#define OPDEF(name, stringname, stackpop, stackpush, params, kind, len, byte1, byte2, ctrl) \
    opSizes1Byte[name] = len + OpFormatExtraSize(params);
#include "opcode.def"
#undef  OPDEF
    opSizes1ByteInit = true;
};

// static
bool Interpreter::MethodMayHaveLoop(BYTE* ilCode, unsigned codeSize)
{
    OpSizes1ByteInit();
    int delta;
    BYTE* ilCodeLim = ilCode + codeSize;
    while (ilCode < ilCodeLim)
    {
        unsigned op = *ilCode;
        switch (op)
        {
        case CEE_BR_S: case CEE_BRFALSE_S: case CEE_BRTRUE_S:
        case CEE_BEQ_S: case CEE_BGE_S: case CEE_BGT_S: case CEE_BLE_S: case CEE_BLT_S:
        case CEE_BNE_UN_S: case CEE_BGE_UN_S: case CEE_BGT_UN_S: case CEE_BLE_UN_S: case CEE_BLT_UN_S:
        case CEE_LEAVE_S:
            delta = getI1(ilCode + 1);
            if (delta < 0) return true;
            ilCode += 2;
            break;

        case CEE_BR: case CEE_BRFALSE: case CEE_BRTRUE:
        case CEE_BEQ: case CEE_BGE: case CEE_BGT: case CEE_BLE: case CEE_BLT:
        case CEE_BNE_UN: case CEE_BGE_UN: case CEE_BGT_UN: case CEE_BLE_UN: case CEE_BLT_UN:
        case CEE_LEAVE:
            delta = getI4LittleEndian(ilCode + 1);
            if (delta < 0) return true;
            ilCode += 5;
            break;

        case CEE_SWITCH:
            {
                UINT32 n = getU4LittleEndian(ilCode + 1);
                UINT32 instrSize = 1 + (n + 1)*4;
                for (unsigned i = 0; i < n; i++) {
                    delta = getI4LittleEndian(ilCode + (5 + i * 4));
                    if (delta < 0) return true;
                }
                ilCode += instrSize;
                break;
            }

        case CEE_PREFIX1:
            op = *(ilCode + 1) + 0x100;
            _ASSERTE(op < CEE_COUNT);  // Bounds check for below.
            // deliberate fall-through here.
            __fallthrough;
        default:
            // For the rest of the 1-byte instructions, we'll use a table-driven approach.
            ilCode += opSizes1Byte[op];
            break;
        }
    }
    return false;

}

void Interpreter::BackwardsBranchActions(int offset)
{
    // TODO: Figure out how to do a GC poll.
}

bool Interpreter::SearchForCoveringFinally()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE_MSG(!m_leaveInfoStack.IsEmpty(), "precondition");

    LeaveInfo& li = m_leaveInfoStack.PeekRef();

    GCX_PREEMP();

    for (unsigned XTnum = li.m_nextEHIndex; XTnum < m_methInfo->m_ehClauseCount; XTnum++)
    {
        CORINFO_EH_CLAUSE clause;
        m_interpCeeInfo.getEHinfo(m_methInfo->m_method, XTnum, &clause);
        _ASSERTE(clause.HandlerLength != (unsigned)-1); // @DEPRECATED

        // First, is the offset of the leave instruction in the try block?
        unsigned tryEndOffset = clause.TryOffset + clause.TryLength;
        if (clause.TryOffset <= li.m_offset && li.m_offset < tryEndOffset)
        {
            // Yes: is it a finally, and is its target outside the try block?
            size_t targOffset = (li.m_target - m_methInfo->m_ILCode);
            if (clause.Flags == CORINFO_EH_CLAUSE_FINALLY
                && !(clause.TryOffset <= targOffset && targOffset < tryEndOffset))
            {
                m_ILCodePtr = m_methInfo->m_ILCode + clause.HandlerOffset;
                li.m_nextEHIndex = XTnum + 1;
                return true;
            }
        }
    }

    // Caller will handle popping the leave info stack.
    return false;
}

// static
void Interpreter::GCScanRoots(promote_func* pf, ScanContext* sc, void* interp0)
{
    Interpreter* interp = reinterpret_cast<Interpreter*>(interp0);
    interp->GCScanRoots(pf, sc);
}

void Interpreter::GCScanRoots(promote_func* pf, ScanContext* sc)
{
    // Report inbound arguments, if the interpreter has not been invoked directly.
    // (In the latter case, the arguments are reported by the calling method.)
    if (!m_directCall)
    {
        for (unsigned i = 0; i < m_methInfo->m_numArgs; i++)
        {
            GCScanRootAtLoc(reinterpret_cast<Object**>(GetArgAddr(i)), GetArgType(i), pf, sc);
        }
    }

    if (m_methInfo->GetFlag<InterpreterMethodInfo::Flag_hasThisArg>())
    {
        if (m_methInfo->GetFlag<InterpreterMethodInfo::Flag_thisArgIsObjPtr>())
        {
            GCScanRootAtLoc(&m_thisArg, InterpreterType(CORINFO_TYPE_CLASS), pf, sc);
        }
        else
        {
            GCScanRootAtLoc(&m_thisArg, InterpreterType(CORINFO_TYPE_BYREF), pf, sc);
        }
    }

    // This is the "this" argument passed in to DoCallWork.  (Note that we treat this as a byref; it
    // might be, for a struct instance method, and this covers the object pointer case as well.)
    GCScanRootAtLoc(reinterpret_cast<Object**>(&m_callThisArg), InterpreterType(CORINFO_TYPE_BYREF), pf, sc);

    // Scan the exception object that we'll rethrow at the end of the finally block.
    GCScanRootAtLoc(reinterpret_cast<Object**>(&m_inFlightException), InterpreterType(CORINFO_TYPE_CLASS), pf, sc);

    // A retBufArg, may, in some cases, be a byref into the heap.
    if (m_retBufArg != NULL)
    {
        GCScanRootAtLoc(reinterpret_cast<Object**>(&m_retBufArg), InterpreterType(CORINFO_TYPE_BYREF), pf, sc);
    }

    if (m_structRetValITPtr != NULL)
    {
        GCScanRootAtLoc(reinterpret_cast<Object**>(m_structRetValTempSpace), *m_structRetValITPtr, pf, sc);
    }

    // We'll conservatively assume that we might have a security object.
    GCScanRootAtLoc(reinterpret_cast<Object**>(&m_securityObject), InterpreterType(CORINFO_TYPE_CLASS), pf, sc);

    // Do locals.
    for (unsigned i = 0; i < m_methInfo->m_numLocals; i++)
    {
        InterpreterType it = m_methInfo->m_localDescs[i].m_type;
        void* localPtr = NULL;
        if (it.IsLargeStruct(&m_interpCeeInfo))
        {
            void* structPtr = ArgSlotEndianessFixup(reinterpret_cast<ARG_SLOT*>(FixedSizeLocalSlot(i)), sizeof(void**));
            localPtr = *reinterpret_cast<void**>(structPtr);
        }
        else
        {
            localPtr = ArgSlotEndianessFixup(reinterpret_cast<ARG_SLOT*>(FixedSizeLocalSlot(i)), it.Size(&m_interpCeeInfo));
        }
        GCScanRootAtLoc(reinterpret_cast<Object**>(localPtr), it, pf, sc, m_methInfo->GetPinningBit(i));
    }

    // Do current ostack.
    for (unsigned i = 0; i < m_curStackHt; i++)
    {
        InterpreterType it = OpStackTypeGet(i);
        if (it.IsLargeStruct(&m_interpCeeInfo))
        {
            Object** structPtr = reinterpret_cast<Object**>(OpStackGet<void*>(i));
            // If the ostack value is a pointer to a local var value, don't scan, since we already
            // scanned the variable value above.
            if (!IsInLargeStructLocalArea(structPtr))
            {
                GCScanRootAtLoc(structPtr, it, pf, sc);
            }
        }
        else
        {
            void* stackPtr = OpStackGetAddr(i, it.Size(&m_interpCeeInfo));
            GCScanRootAtLoc(reinterpret_cast<Object**>(stackPtr), it, pf, sc);
        }
    }

    // Any outgoing arguments for a call in progress.
    for (unsigned i = 0; i < m_argsSize; i++)
    {
        // If a call has a large struct argument, we'll have pushed a pointer to the entry for that argument on the
        // largeStructStack of the current Interpreter.  That will be scanned by the code above, so just skip it.
        InterpreterType undef(CORINFO_TYPE_UNDEF);
        InterpreterType it = m_argTypes[i];
        if (it != undef && !it.IsLargeStruct(&m_interpCeeInfo))
        {
            BYTE* argPtr = ArgSlotEndianessFixup(&m_args[i], it.Size(&m_interpCeeInfo));
            GCScanRootAtLoc(reinterpret_cast<Object**>(argPtr), it, pf, sc);
        }
    }
}

void Interpreter::GCScanRootAtLoc(Object** loc, InterpreterType it, promote_func* pf, ScanContext* sc, bool pinningRef)
{
    switch (it.ToCorInfoType())
    {
    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_STRING:
        {
            DWORD flags = 0;
            if (pinningRef) flags |= GC_CALL_PINNED;
            (*pf)(loc, sc, flags);
        }
        break;

    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_REFANY:
        {
            DWORD flags = GC_CALL_INTERIOR;
            if (pinningRef) flags |= GC_CALL_PINNED;
            (*pf)(loc, sc, flags);
        }
        break;

    case CORINFO_TYPE_VALUECLASS:
        _ASSERTE(!pinningRef);
        GCScanValueClassRootAtLoc(loc, it.ToClassHandle(), pf, sc);
        break;

    default:
        _ASSERTE(!pinningRef);
        break;
    }
}

void Interpreter::GCScanValueClassRootAtLoc(Object** loc, CORINFO_CLASS_HANDLE valueClsHnd, promote_func* pf, ScanContext* sc)
{
    MethodTable* valClsMT = GetMethodTableFromClsHnd(valueClsHnd);
    ReportPointersFromValueType(pf, sc, valClsMT, loc);
}

// Returns "true" iff "cit" is "stack-normal": all integer types with byte size less than 4
// are folded to CORINFO_TYPE_INT; all remaining unsigned types are folded to their signed counterparts.
bool IsStackNormalType(CorInfoType cit)
{
    LIMITED_METHOD_CONTRACT;

    switch (cit)
    {
    case CORINFO_TYPE_UNDEF:
    case CORINFO_TYPE_VOID:
    case CORINFO_TYPE_BOOL:
    case CORINFO_TYPE_CHAR:
    case CORINFO_TYPE_BYTE:
    case CORINFO_TYPE_UBYTE:
    case CORINFO_TYPE_SHORT:
    case CORINFO_TYPE_USHORT:
    case CORINFO_TYPE_UINT:
    case CORINFO_TYPE_NATIVEUINT:
    case CORINFO_TYPE_ULONG:
    case CORINFO_TYPE_VAR:
    case CORINFO_TYPE_STRING:
    case CORINFO_TYPE_PTR:
        return false;

    case CORINFO_TYPE_INT:
    case CORINFO_TYPE_NATIVEINT:
    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_LONG:
    case CORINFO_TYPE_VALUECLASS:
    case CORINFO_TYPE_REFANY:
        // I chose to consider both float and double stack-normal; together these comprise
        // the "F" type of the ECMA spec.  This means I have to consider these to freely
        // interconvert.
    case CORINFO_TYPE_FLOAT:
    case CORINFO_TYPE_DOUBLE:
        return true;

    default:
        UNREACHABLE();
    }
}

CorInfoType CorInfoTypeStackNormalize(CorInfoType cit)
{
    LIMITED_METHOD_CONTRACT;

    switch (cit)
    {
    case CORINFO_TYPE_UNDEF:
        return CORINFO_TYPE_UNDEF;

    case CORINFO_TYPE_VOID:
    case CORINFO_TYPE_VAR:
        _ASSERTE_MSG(false, "Type that cannot be on the ostack.");
        return CORINFO_TYPE_UNDEF;

    case CORINFO_TYPE_BOOL:
    case CORINFO_TYPE_CHAR:
    case CORINFO_TYPE_BYTE:
    case CORINFO_TYPE_UBYTE:
    case CORINFO_TYPE_SHORT:
    case CORINFO_TYPE_USHORT:
    case CORINFO_TYPE_UINT:
        return CORINFO_TYPE_INT;

    case CORINFO_TYPE_NATIVEUINT:
    case CORINFO_TYPE_PTR:
        return CORINFO_TYPE_NATIVEINT;

    case CORINFO_TYPE_ULONG:
        return CORINFO_TYPE_LONG;

    case CORINFO_TYPE_STRING:
        return CORINFO_TYPE_CLASS;

    case CORINFO_TYPE_INT:
    case CORINFO_TYPE_NATIVEINT:
    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_LONG:
    case CORINFO_TYPE_VALUECLASS:
    case CORINFO_TYPE_REFANY:
        // I chose to consider both float and double stack-normal; together these comprise
        // the "F" type of the ECMA spec.  This means I have to consider these to freely
        // interconvert.
    case CORINFO_TYPE_FLOAT:
    case CORINFO_TYPE_DOUBLE:
        _ASSERTE(IsStackNormalType(cit));
        return cit;

    default:
        UNREACHABLE();
    }
}

InterpreterType InterpreterType::StackNormalize() const
{
    LIMITED_METHOD_CONTRACT;

    switch (ToCorInfoType())
    {
    case CORINFO_TYPE_BOOL:
    case CORINFO_TYPE_CHAR:
    case CORINFO_TYPE_BYTE:
    case CORINFO_TYPE_UBYTE:
    case CORINFO_TYPE_SHORT:
    case CORINFO_TYPE_USHORT:
    case CORINFO_TYPE_UINT:
        return InterpreterType(CORINFO_TYPE_INT);

    case CORINFO_TYPE_NATIVEUINT:
    case CORINFO_TYPE_PTR:
        return InterpreterType(CORINFO_TYPE_NATIVEINT);

    case CORINFO_TYPE_ULONG:
        return InterpreterType(CORINFO_TYPE_LONG);

    case CORINFO_TYPE_STRING:
        return InterpreterType(CORINFO_TYPE_CLASS);

    case CORINFO_TYPE_INT:
    case CORINFO_TYPE_NATIVEINT:
    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_LONG:
    case CORINFO_TYPE_VALUECLASS:
    case CORINFO_TYPE_REFANY:
    case CORINFO_TYPE_FLOAT:
    case CORINFO_TYPE_DOUBLE:
        return *const_cast<InterpreterType*>(this);

    case CORINFO_TYPE_UNDEF:
    case CORINFO_TYPE_VOID:
    case CORINFO_TYPE_VAR:
    default:
        _ASSERTE_MSG(false, "should not reach here");
        return *const_cast<InterpreterType*>(this);
    }
}

#ifdef _DEBUG
bool InterpreterType::MatchesWork(const InterpreterType it2, CEEInfo* info) const
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    if (*this == it2) return true;

    // Otherwise...
    CorInfoType cit1 = ToCorInfoType();
    CorInfoType cit2 = it2.ToCorInfoType();

    GCX_PREEMP();

    // An approximation: valueclasses of the same size match.
    if (cit1 == CORINFO_TYPE_VALUECLASS &&
        cit2 == CORINFO_TYPE_VALUECLASS &&
        Size(info) == it2.Size(info))
    {
        return true;
    }

    // NativeInt matches byref.  (In unsafe code).
    if ((cit1 == CORINFO_TYPE_BYREF && cit2 == CORINFO_TYPE_NATIVEINT))
        return true;

    // apparently the VM may do the optimization of reporting the return type of a method that
    // returns a struct of a single nativeint field *as* nativeint; and similarly with at least some other primitive types.
    // So weaken this check to allow that.
    // (The check is actually a little weaker still, since I don't want to crack the return type and make sure
    // that it has only a single nativeint member -- so I just ensure that the total size is correct).
    switch (cit1)
    {
    case CORINFO_TYPE_NATIVEINT:
    case CORINFO_TYPE_NATIVEUINT:
        _ASSERTE(sizeof(NativeInt) == sizeof(NativeUInt));
        if (it2.Size(info) == sizeof(NativeInt))
            return true;
        break;

    case CORINFO_TYPE_INT:
    case CORINFO_TYPE_UINT:
        _ASSERTE(sizeof(INT32) == sizeof(UINT32));
        if (it2.Size(info) == sizeof(INT32))
            return true;
        break;

    default:
        break;
    }

    // See if the second is a value type synonym for a primitive.
    if (cit2 == CORINFO_TYPE_VALUECLASS)
    {
        CorInfoType cit2prim = info->getTypeForPrimitiveValueClass(it2.ToClassHandle());
        if (cit2prim != CORINFO_TYPE_UNDEF)
        {
            InterpreterType it2prim(cit2prim);
            if (*this == it2prim.StackNormalize())
                return true;
        }
    }

    // Otherwise...
    return false;
}
#endif // _DEBUG

// Static
size_t CorInfoTypeSizeArray[] =
{
    /*CORINFO_TYPE_UNDEF           = 0x0*/0,
    /*CORINFO_TYPE_VOID            = 0x1*/0,
    /*CORINFO_TYPE_BOOL            = 0x2*/1,
    /*CORINFO_TYPE_CHAR            = 0x3*/2,
    /*CORINFO_TYPE_BYTE            = 0x4*/1,
    /*CORINFO_TYPE_UBYTE           = 0x5*/1,
    /*CORINFO_TYPE_SHORT           = 0x6*/2,
    /*CORINFO_TYPE_USHORT          = 0x7*/2,
    /*CORINFO_TYPE_INT             = 0x8*/4,
    /*CORINFO_TYPE_UINT            = 0x9*/4,
    /*CORINFO_TYPE_LONG            = 0xa*/8,
    /*CORINFO_TYPE_ULONG           = 0xb*/8,
    /*CORINFO_TYPE_NATIVEINT       = 0xc*/sizeof(void*),
    /*CORINFO_TYPE_NATIVEUINT      = 0xd*/sizeof(void*),
    /*CORINFO_TYPE_FLOAT           = 0xe*/4,
    /*CORINFO_TYPE_DOUBLE          = 0xf*/8,
    /*CORINFO_TYPE_STRING          = 0x10*/sizeof(void*),
    /*CORINFO_TYPE_PTR             = 0x11*/sizeof(void*),
    /*CORINFO_TYPE_BYREF           = 0x12*/sizeof(void*),
    /*CORINFO_TYPE_VALUECLASS      = 0x13*/0,
    /*CORINFO_TYPE_CLASS           = 0x14*/sizeof(void*),
    /*CORINFO_TYPE_REFANY          = 0x15*/sizeof(void*)*2,
    /*CORINFO_TYPE_VAR             = 0x16*/0,
};

bool CorInfoTypeIsUnsigned(CorInfoType cit)
{
    LIMITED_METHOD_CONTRACT;

    switch (cit)
    {
    case CORINFO_TYPE_UINT:
    case CORINFO_TYPE_NATIVEUINT:
    case CORINFO_TYPE_ULONG:
    case CORINFO_TYPE_UBYTE:
    case CORINFO_TYPE_USHORT:
    case CORINFO_TYPE_CHAR:
        return true;

    default:
        return false;
    }
}

bool CorInfoTypeIsIntegral(CorInfoType cit)
{
    LIMITED_METHOD_CONTRACT;

    switch (cit)
    {
    case CORINFO_TYPE_UINT:
    case CORINFO_TYPE_NATIVEUINT:
    case CORINFO_TYPE_ULONG:
    case CORINFO_TYPE_UBYTE:
    case CORINFO_TYPE_USHORT:
    case CORINFO_TYPE_INT:
    case CORINFO_TYPE_NATIVEINT:
    case CORINFO_TYPE_LONG:
    case CORINFO_TYPE_BYTE:
    case CORINFO_TYPE_BOOL:
    case CORINFO_TYPE_SHORT:
        return true;

    default:
        return false;
    }
}

bool CorInfoTypeIsFloatingPoint(CorInfoType cit)
{
    return cit == CORINFO_TYPE_FLOAT || cit == CORINFO_TYPE_DOUBLE;
}

bool CorInfoTypeIsFloatingPoint(CorInfoHFAElemType cihet)
{
    return cihet == CORINFO_HFA_ELEM_FLOAT || cihet == CORINFO_HFA_ELEM_DOUBLE;
}

bool CorElemTypeIsUnsigned(CorElementType cet)
{
    LIMITED_METHOD_CONTRACT;

    switch (cet)
    {
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_U:
        return true;

    default:
        return false;
    }
}

bool CorInfoTypeIsPointer(CorInfoType cit)
{
    LIMITED_METHOD_CONTRACT;
    switch (cit)
    {
    case CORINFO_TYPE_PTR:
    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_NATIVEINT:
    case CORINFO_TYPE_NATIVEUINT:
        return true;

        // It seems like the ECMA spec doesn't allow this, but (at least) the managed C++
        // compiler expects the explicitly-sized pointer type of the platform pointer size to work:
    case CORINFO_TYPE_INT:
    case CORINFO_TYPE_UINT:
        return sizeof(NativeInt) == sizeof(INT32);
    case CORINFO_TYPE_LONG:
    case CORINFO_TYPE_ULONG:
        return sizeof(NativeInt) == sizeof(INT64);

    default:
        return false;
    }
}

void Interpreter::LdArg(int argNum)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    LdFromMemAddr(GetArgAddr(argNum), GetArgType(argNum));
}

void Interpreter::LdArgA(int argNum)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_BYREF));
    OpStackSet<void*>(m_curStackHt, reinterpret_cast<void*>(GetArgAddr(argNum)));
    m_curStackHt++;
}

void Interpreter::StArg(int argNum)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    StToLocalMemAddr(GetArgAddr(argNum), GetArgType(argNum));
}


void Interpreter::LdLocA(int locNum)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    InterpreterType tp = m_methInfo->m_localDescs[locNum].m_type;
    void* addr;
    if (tp.IsLargeStruct(&m_interpCeeInfo))
    {
        void* structPtr = ArgSlotEndianessFixup(reinterpret_cast<ARG_SLOT*>(FixedSizeLocalSlot(locNum)), sizeof(void**));
        addr = *reinterpret_cast<void**>(structPtr);
    }
    else
    {
        addr = ArgSlotEndianessFixup(reinterpret_cast<ARG_SLOT*>(FixedSizeLocalSlot(locNum)), tp.Size(&m_interpCeeInfo));
    }
    // The "addr" above, while a byref, is never a heap pointer, so we're robust if
    // any of these were to cause a GC.
    OpStackSet<void*>(m_curStackHt, addr);
    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_BYREF));
    m_curStackHt++;
}

void Interpreter::LdIcon(INT32 c)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_INT));
    OpStackSet<INT32>(m_curStackHt, c);
    m_curStackHt++;
}

void Interpreter::LdR4con(INT32 c)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_FLOAT));
    OpStackSet<INT32>(m_curStackHt, c);
    m_curStackHt++;
}

void Interpreter::LdLcon(INT64 c)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_LONG));
    OpStackSet<INT64>(m_curStackHt, c);
    m_curStackHt++;
}

void Interpreter::LdR8con(INT64 c)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_DOUBLE));
    OpStackSet<INT64>(m_curStackHt, c);
    m_curStackHt++;
}

void Interpreter::LdNull()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_CLASS));
    OpStackSet<void*>(m_curStackHt, NULL);
    m_curStackHt++;
}

template<typename T, CorInfoType cit>
void Interpreter::LdInd()
{
    _ASSERTE(TOSIsPtr());
    _ASSERTE(IsStackNormalType(cit));
    unsigned curStackInd = m_curStackHt-1;
    T* ptr = OpStackGet<T*>(curStackInd);
    ThrowOnInvalidPointer(ptr);
    OpStackSet<T>(curStackInd, *ptr);
    OpStackTypeSet(curStackInd, InterpreterType(cit));
    BarrierIfVolatile();
}

template<typename T, bool isUnsigned>
void Interpreter::LdIndShort()
{
    _ASSERTE(TOSIsPtr());
    _ASSERTE(sizeof(T) < 4);
    unsigned curStackInd = m_curStackHt-1;
    T* ptr = OpStackGet<T*>(curStackInd);
    ThrowOnInvalidPointer(ptr);
    if (isUnsigned)
    {
        OpStackSet<UINT32>(curStackInd, *ptr);
    }
    else
    {
        OpStackSet<INT32>(curStackInd, *ptr);
    }
    // All short integers are normalized to INT as their stack type.
    OpStackTypeSet(curStackInd, InterpreterType(CORINFO_TYPE_INT));
    BarrierIfVolatile();
}

template<typename T>
void Interpreter::StInd()
{
    _ASSERTE(m_curStackHt >= 2);
    _ASSERTE(CorInfoTypeIsPointer(OpStackTypeGet(m_curStackHt-2).ToCorInfoType()));
    BarrierIfVolatile();
    unsigned stackInd0 = m_curStackHt-2;
    unsigned stackInd1 = m_curStackHt-1;
    T val = OpStackGet<T>(stackInd1);
    T* ptr = OpStackGet<T*>(stackInd0);
    ThrowOnInvalidPointer(ptr);
    *ptr = val;
    m_curStackHt -= 2;

#if INTERP_TRACING
    if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL) &&
        IsInLocalArea(ptr))
    {
        PrintLocals();
    }
#endif // INTERP_TRACING
}

void Interpreter::StInd_Ref()
{
    _ASSERTE(m_curStackHt >= 2);
    _ASSERTE(CorInfoTypeIsPointer(OpStackTypeGet(m_curStackHt-2).ToCorInfoType()));
    BarrierIfVolatile();
    unsigned stackInd0 = m_curStackHt-2;
    unsigned stackInd1 = m_curStackHt-1;
    OBJECTREF val = ObjectToOBJECTREF(OpStackGet<Object*>(stackInd1));
    OBJECTREF* ptr = OpStackGet<OBJECTREF*>(stackInd0);
    ThrowOnInvalidPointer(ptr);
    SetObjectReference(ptr, val);
    m_curStackHt -= 2;

#if INTERP_TRACING
    if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL) &&
        IsInLocalArea(ptr))
    {
        PrintLocals();
    }
#endif // INTERP_TRACING
}


template<int op>
void Interpreter::BinaryArithOp()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned op1idx = m_curStackHt - 2;
    unsigned op2idx = m_curStackHt - 1;
    InterpreterType t1 = OpStackTypeGet(op1idx);
    _ASSERTE(IsStackNormalType(t1.ToCorInfoType()));
    // Looking at the generated code, it does seem to save some instructions to use the "shifted
    // types," though the effect on end-to-end time is variable.  So I'll leave it set.
    InterpreterType t2 = OpStackTypeGet(op2idx);
    _ASSERTE(IsStackNormalType(t2.ToCorInfoType()));

    // In all cases belows, since "op" is compile-time constant, "if" chains on it should fold away.
    switch (t1.ToCorInfoTypeShifted())
    {
    case CORINFO_TYPE_SHIFTED_INT:
        if (t1 == t2)
        {
            // Int op Int = Int
            INT32 val1 = OpStackGet<INT32>(op1idx);
            INT32 val2 = OpStackGet<INT32>(op2idx);
            BinaryArithOpWork<op, INT32, /*IsIntType*/true, CORINFO_TYPE_INT, /*TypeIsUnchanged*/true>(val1, val2);
        }
        else
        {
            CorInfoTypeShifted cits2 = t2.ToCorInfoTypeShifted();
            if (cits2 == CORINFO_TYPE_SHIFTED_NATIVEINT)
            {
                // Int op NativeInt = NativeInt
                NativeInt val1 = static_cast<NativeInt>(OpStackGet<INT32>(op1idx));
                NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/false>(val1, val2);
            }
            else if (s_InterpreterLooseRules && cits2 == CORINFO_TYPE_SHIFTED_LONG)
            {
                // Int op Long = Long
                INT64 val1 = static_cast<INT64>(OpStackGet<INT32>(op1idx));
                INT64 val2 = OpStackGet<INT64>(op2idx);
                BinaryArithOpWork<op, INT64, /*IsIntType*/true, CORINFO_TYPE_LONG, /*TypeIsUnchanged*/false>(val1, val2);
            }
            else if (cits2 == CORINFO_TYPE_SHIFTED_BYREF)
            {
                if (op == BA_Add || (s_InterpreterLooseRules && op == BA_Sub))
                {
                    // Int + ByRef = ByRef
                    NativeInt val1 = static_cast<NativeInt>(OpStackGet<INT32>(op1idx));
                    NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                    BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/false>(val1, val2);
                }
                else
                {
                    VerificationError("Operation not permitted on int and managed pointer.");
                }
            }
            else
            {
                VerificationError("Binary arithmetic operation type mismatch (int and ?)");
            }
        }
        break;

    case CORINFO_TYPE_SHIFTED_NATIVEINT:
        {
            NativeInt val1 = OpStackGet<NativeInt>(op1idx);
            if (t1 == t2)
            {
                // NativeInt op NativeInt = NativeInt
                NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                CorInfoTypeShifted cits2 = t2.ToCorInfoTypeShifted();
                if (cits2 == CORINFO_TYPE_SHIFTED_INT)
                {
                    // NativeInt op Int = NativeInt
                    NativeInt val2 = static_cast<NativeInt>(OpStackGet<INT32>(op2idx));
                    BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
                }
                // CLI spec does not allow adding a native int and an int64. So use loose rules.
                else if (s_InterpreterLooseRules && cits2 == CORINFO_TYPE_SHIFTED_LONG)
                {
                    // NativeInt op Int = NativeInt
                    NativeInt val2 = static_cast<NativeInt>(OpStackGet<INT64>(op2idx));
                    BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
                }
                else if (cits2 == CORINFO_TYPE_SHIFTED_BYREF)
                {
                    if (op == BA_Add || (s_InterpreterLooseRules && op == BA_Sub))
                    {
                        // NativeInt + ByRef = ByRef
                        NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                        BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/false>(val1, val2);
                    }
                    else
                    {
                        VerificationError("Operation not permitted on native int and managed pointer.");
                    }
                }
                else
                {
                    VerificationError("Binary arithmetic operation type mismatch (native int and ?)");
                }
            }
        }
        break;

    case CORINFO_TYPE_SHIFTED_LONG:
        {
            bool looseLong = false;
#if defined(HOST_AMD64)
            looseLong = (s_InterpreterLooseRules && (t2.ToCorInfoType() == CORINFO_TYPE_NATIVEINT ||
                    t2.ToCorInfoType() == CORINFO_TYPE_BYREF));
#endif
            if (t1 == t2 || looseLong)
            {
                // Long op Long = Long
                INT64 val1 = OpStackGet<INT64>(op1idx);
                INT64 val2 = OpStackGet<INT64>(op2idx);
                BinaryArithOpWork<op, INT64, /*IsIntType*/true, CORINFO_TYPE_LONG, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                VerificationError("Binary arithmetic operation type mismatch (long and ?)");
            }
        }
        break;

    case CORINFO_TYPE_SHIFTED_FLOAT:
        {
            if (t1 == t2)
            {
                // Float op Float = Float
                float val1 = OpStackGet<float>(op1idx);
                float val2 = OpStackGet<float>(op2idx);
                BinaryArithOpWork<op, float, /*IsIntType*/false, CORINFO_TYPE_FLOAT, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                CorInfoTypeShifted cits2 = t2.ToCorInfoTypeShifted();
                if (cits2 == CORINFO_TYPE_SHIFTED_DOUBLE)
                {
                    // Float op Double = Double
                    double val1 = static_cast<double>(OpStackGet<float>(op1idx));
                    double val2 = OpStackGet<double>(op2idx);
                    BinaryArithOpWork<op, double, /*IsIntType*/false, CORINFO_TYPE_DOUBLE, /*TypeIsUnchanged*/false>(val1, val2);
                }
                else
                {
                    VerificationError("Binary arithmetic operation type mismatch (float and ?)");
                }
            }
        }
        break;

    case CORINFO_TYPE_SHIFTED_DOUBLE:
        {
            if (t1 == t2)
            {
                // Double op Double = Double
                double val1 = OpStackGet<double>(op1idx);
                double val2 = OpStackGet<double>(op2idx);
                BinaryArithOpWork<op, double, /*IsIntType*/false, CORINFO_TYPE_DOUBLE, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                CorInfoTypeShifted cits2 = t2.ToCorInfoTypeShifted();
                if (cits2 == CORINFO_TYPE_SHIFTED_FLOAT)
                {
                    // Double op Float = Double
                    double val1 = OpStackGet<double>(op1idx);
                    double val2 = static_cast<double>(OpStackGet<float>(op2idx));
                    BinaryArithOpWork<op, double, /*IsIntType*/false, CORINFO_TYPE_DOUBLE, /*TypeIsUnchanged*/true>(val1, val2);
                }
                else
                {
                    VerificationError("Binary arithmetic operation type mismatch (double and ?)");
                }
            }
        }
        break;

    case CORINFO_TYPE_SHIFTED_BYREF:
        {
            NativeInt val1 = OpStackGet<NativeInt>(op1idx);
            CorInfoTypeShifted cits2 = t2.ToCorInfoTypeShifted();
            if (cits2 == CORINFO_TYPE_SHIFTED_INT)
            {
                if (op == BA_Add || op == BA_Sub)
                {
                    // ByRef +- Int = ByRef
                    NativeInt val2 = static_cast<NativeInt>(OpStackGet<INT32>(op2idx));
                    BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/true>(val1, val2);
                }
                else
                {
                    VerificationError("May only add/subtract managed pointer and integral value.");
                }
            }
            else if (cits2 == CORINFO_TYPE_SHIFTED_NATIVEINT)
            {
                if (op == BA_Add || op == BA_Sub)
                {
                    // ByRef +- NativeInt = ByRef
                    NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                    BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/true>(val1, val2);
                }
                else
                {
                    VerificationError("May only add/subtract managed pointer and integral value.");
                }
            }
            else if (cits2 == CORINFO_TYPE_SHIFTED_BYREF)
            {
                if (op == BA_Sub)
                {
                    // ByRef - ByRef = NativeInt
                    NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                    BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/false>(val1, val2);
                }
                else
                {
                    VerificationError("May only subtract managed pointer values.");
                }
            }
            // CLI spec does not allow adding a native int and an int64. So use loose rules.
            else if (s_InterpreterLooseRules && cits2 == CORINFO_TYPE_SHIFTED_LONG)
            {
                // NativeInt op Int = NativeInt
                NativeInt val2 = static_cast<NativeInt>(OpStackGet<INT64>(op2idx));
                BinaryArithOpWork<op, NativeInt, /*IsIntType*/true, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                VerificationError("Binary arithmetic operation not permitted on byref");
            }
        }
        break;

    case CORINFO_TYPE_SHIFTED_CLASS:
        VerificationError("Can't do binary arithmetic on object references.");
        break;

    default:
        _ASSERTE_MSG(false, "Non-stack-normal type on stack.");
    }

    // In all cases:
    m_curStackHt--;
}

template<int op, bool asUnsigned>
void Interpreter::BinaryArithOvfOp()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned op1idx = m_curStackHt - 2;
    unsigned op2idx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(op1idx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    InterpreterType t2 = OpStackTypeGet(op2idx);
    CorInfoType cit2 = t2.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit2));

    // In all cases belows, since "op" is compile-time constant, "if" chains on it should fold away.
    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        if (cit2 == CORINFO_TYPE_INT)
        {
            if (asUnsigned)
            {
                // UnsignedInt op UnsignedInt = UnsignedInt
                UINT32 val1 = OpStackGet<UINT32>(op1idx);
                UINT32 val2 = OpStackGet<UINT32>(op2idx);
                BinaryArithOvfOpWork<op, UINT32, CORINFO_TYPE_INT, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                // Int op Int = Int
                INT32 val1 = OpStackGet<INT32>(op1idx);
                INT32 val2 = OpStackGet<INT32>(op2idx);
                BinaryArithOvfOpWork<op, INT32, CORINFO_TYPE_INT, /*TypeIsUnchanged*/true>(val1, val2);
            }
        }
        else if (cit2 == CORINFO_TYPE_NATIVEINT)
        {
            if (asUnsigned)
            {
                // UnsignedInt op UnsignedNativeInt = UnsignedNativeInt
                NativeUInt val1 = static_cast<NativeUInt>(OpStackGet<UINT32>(op1idx));
                NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
                BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/false>(val1, val2);
            }
            else
            {
                // Int op NativeInt = NativeInt
                NativeInt val1 = static_cast<NativeInt>(OpStackGet<INT32>(op1idx));
                NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                BinaryArithOvfOpWork<op, NativeInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/false>(val1, val2);
            }
        }
        else if (cit2 == CORINFO_TYPE_BYREF)
        {
            if (asUnsigned && op == BA_Add)
            {
                // UnsignedInt + ByRef = ByRef
                NativeUInt val1 = static_cast<NativeUInt>(OpStackGet<UINT32>(op1idx));
                NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
                BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/false>(val1, val2);
            }
            else
            {
                VerificationError("Illegal arithmetic overflow operation for int and byref.");
            }
        }
        else
        {
            VerificationError("Binary arithmetic overflow operation type mismatch (int and ?)");
        }
        break;

    case CORINFO_TYPE_NATIVEINT:
        if (cit2 == CORINFO_TYPE_INT)
        {
            if (asUnsigned)
            {
                // UnsignedNativeInt op UnsignedInt = UnsignedNativeInt
                NativeUInt val1 = OpStackGet<NativeUInt>(op1idx);
                NativeUInt val2 = static_cast<NativeUInt>(OpStackGet<UINT32>(op2idx));
                BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                // NativeInt op Int = NativeInt
                NativeInt val1 = OpStackGet<NativeInt>(op1idx);
                NativeInt val2 = static_cast<NativeInt>(OpStackGet<INT32>(op2idx));
                BinaryArithOvfOpWork<op, NativeInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
            }
        }
        else if (cit2 == CORINFO_TYPE_NATIVEINT)
        {
            if (asUnsigned)
            {
                // UnsignedNativeInt op UnsignedNativeInt = UnsignedNativeInt
                NativeUInt val1 = OpStackGet<NativeUInt>(op1idx);
                NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
                BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                // NativeInt op NativeInt = NativeInt
                NativeInt val1 = OpStackGet<NativeInt>(op1idx);
                NativeInt val2 = OpStackGet<NativeInt>(op2idx);
                BinaryArithOvfOpWork<op, NativeInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
            }
        }
        else if (cit2 == CORINFO_TYPE_BYREF)
        {
            if (asUnsigned && op == BA_Add)
            {
                // UnsignedNativeInt op ByRef = ByRef
                NativeUInt val1 = OpStackGet<UINT32>(op1idx);
                NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
                BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/false>(val1, val2);
            }
            else
            {
                VerificationError("Illegal arithmetic overflow operation for native int and byref.");
            }
        }
        else
        {
            VerificationError("Binary arithmetic overflow operation type mismatch (native int and ?)");
        }
        break;

    case CORINFO_TYPE_LONG:
        if (cit2 == CORINFO_TYPE_LONG || (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_NATIVEINT))
        {
            if (asUnsigned)
            {
                // UnsignedLong op UnsignedLong = UnsignedLong
                UINT64 val1 = OpStackGet<UINT64>(op1idx);
                UINT64 val2 = OpStackGet<UINT64>(op2idx);
                BinaryArithOvfOpWork<op, UINT64, CORINFO_TYPE_LONG, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else
            {
                // Long op Long = Long
                INT64 val1 = OpStackGet<INT64>(op1idx);
                INT64 val2 = OpStackGet<INT64>(op2idx);
                BinaryArithOvfOpWork<op, INT64, CORINFO_TYPE_LONG, /*TypeIsUnchanged*/true>(val1, val2);
            }
        }
        else
        {
            VerificationError("Binary arithmetic overflow operation type mismatch (long and ?)");
        }
        break;

    case CORINFO_TYPE_BYREF:
        if (asUnsigned && (op == BA_Add || op == BA_Sub))
        {
            NativeUInt val1 = OpStackGet<NativeUInt>(op1idx);
            if (cit2 == CORINFO_TYPE_INT)
            {
                // ByRef +- UnsignedInt = ByRef
                NativeUInt val2 = static_cast<NativeUInt>(OpStackGet<INT32>(op2idx));
                BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else if (cit2 == CORINFO_TYPE_NATIVEINT)
            {
                // ByRef +- UnsignedNativeInt = ByRef
                NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
                BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/true>(val1, val2);
            }
            else if (cit2 == CORINFO_TYPE_BYREF)
            {
                if (op == BA_Sub)
                {
                    // ByRef - ByRef = UnsignedNativeInt
                    NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
                    BinaryArithOvfOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/false>(val1, val2);
                }
                else
                {
                    VerificationError("Illegal arithmetic overflow operation for byref and byref: may only subtract managed pointer values.");
                }
            }
            else
            {
                VerificationError("Binary arithmetic overflow operation not permitted on byref");
            }
        }
        else
        {
            if (!asUnsigned)
            {
                VerificationError("Signed binary arithmetic overflow operation not permitted on managed pointer values.");
            }
            else
            {
                _ASSERTE_MSG(op == BA_Mul, "Must be an overflow operation; tested for Add || Sub above.");
                VerificationError("Cannot multiply managed pointer values.");
            }
        }
        break;

    default:
        _ASSERTE_MSG(false, "Non-stack-normal type on stack.");
    }

    // In all cases:
    m_curStackHt--;
}

template<int op, typename T, CorInfoType cit, bool TypeIsUnchanged>
void Interpreter::BinaryArithOvfOpWork(T val1, T val2)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    ClrSafeInt<T> res;
    ClrSafeInt<T> safeV1(val1);
    ClrSafeInt<T> safeV2(val2);
    if (op == BA_Add)
    {
        res = safeV1 + safeV2;
    }
    else if (op == BA_Sub)
    {
        res = safeV1 - safeV2;
    }
    else if (op == BA_Mul)
    {
        res = safeV1 * safeV2;
    }
    else
    {
        _ASSERTE_MSG(false, "op should be one of the overflow ops...");
    }

    if (res.IsOverflow())
    {
        ThrowOverflowException();
    }

    unsigned residx = m_curStackHt - 2;
    OpStackSet<T>(residx, res.Value());
    if (!TypeIsUnchanged)
    {
        OpStackTypeSet(residx, InterpreterType(cit));
    }
}

template<int op>
void Interpreter::BinaryIntOp()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned op1idx = m_curStackHt - 2;
    unsigned op2idx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(op1idx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    InterpreterType t2 = OpStackTypeGet(op2idx);
    CorInfoType cit2 = t2.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit2));

    // In all cases belows, since "op" is compile-time constant, "if" chains on it should fold away.
    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        if (cit2 == CORINFO_TYPE_INT)
        {
            // Int op Int = Int
            UINT32 val1 = OpStackGet<UINT32>(op1idx);
            UINT32 val2 = OpStackGet<UINT32>(op2idx);
            BinaryIntOpWork<op, UINT32, CORINFO_TYPE_INT, /*TypeIsUnchanged*/true>(val1, val2);
        }
        else if (cit2 == CORINFO_TYPE_NATIVEINT)
        {
            // Int op NativeInt = NativeInt
            NativeUInt val1 = static_cast<NativeUInt>(OpStackGet<INT32>(op1idx));
            NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
            BinaryIntOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/false>(val1, val2);
        }
        else if (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_BYREF)
        {
            // Int op NativeUInt = NativeUInt
            NativeUInt val1 = static_cast<NativeUInt>(OpStackGet<INT32>(op1idx));
            NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
            BinaryIntOpWork<op, NativeUInt, CORINFO_TYPE_BYREF, /*TypeIsUnchanged*/false>(val1, val2);
        }
        else
        {
            VerificationError("Binary arithmetic operation type mismatch (int and ?)");
        }
        break;

    case CORINFO_TYPE_NATIVEINT:
        if (cit2 == CORINFO_TYPE_NATIVEINT)
        {
            // NativeInt op NativeInt = NativeInt
            NativeUInt val1 = OpStackGet<NativeUInt>(op1idx);
            NativeUInt val2 = OpStackGet<NativeUInt>(op2idx);
            BinaryIntOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
        }
        else if (cit2 == CORINFO_TYPE_INT)
        {
            // NativeInt op Int = NativeInt
            NativeUInt val1 = OpStackGet<NativeUInt>(op1idx);
            NativeUInt val2 = static_cast<NativeUInt>(OpStackGet<INT32>(op2idx));
            BinaryIntOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
        }
        // CLI spec does not allow adding a native int and an int64. So use loose rules.
        else if (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_LONG)
        {
            // NativeInt op Int = NativeInt
            NativeUInt val1 = OpStackGet<NativeUInt>(op1idx);
            NativeUInt val2 = static_cast<NativeUInt>(OpStackGet<INT64>(op2idx));
            BinaryIntOpWork<op, NativeUInt, CORINFO_TYPE_NATIVEINT, /*TypeIsUnchanged*/true>(val1, val2);
        }
        else
        {
            VerificationError("Binary arithmetic operation type mismatch (native int and ?)");
        }
        break;

    case CORINFO_TYPE_LONG:
        if (cit2 == CORINFO_TYPE_LONG || (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_NATIVEINT))
        {
            // Long op Long = Long
            UINT64 val1 = OpStackGet<UINT64>(op1idx);
            UINT64 val2 = OpStackGet<UINT64>(op2idx);
            BinaryIntOpWork<op, UINT64, CORINFO_TYPE_LONG, /*TypeIsUnchanged*/true>(val1, val2);
        }
        else
        {
            VerificationError("Binary arithmetic operation type mismatch (long and ?)");
        }
        break;

    default:
        VerificationError("Illegal operation for non-integral data type.");
    }

    // In all cases:
    m_curStackHt--;
}

template<int op, typename T, CorInfoType cit, bool TypeIsUnchanged>
void Interpreter::BinaryIntOpWork(T val1, T val2)
{
    T res;
    if (op == BIO_And)
    {
        res = val1 & val2;
    }
    else if (op == BIO_Or)
    {
        res = val1 | val2;
    }
    else if (op == BIO_Xor)
    {
        res = val1 ^ val2;
    }
    else
    {
        _ASSERTE(op == BIO_DivUn || op == BIO_RemUn);
        if (val2 == 0)
        {
            ThrowDivideByZero();
        }
        else if (val2 == static_cast<T>(-1) && val1 == static_cast<T>(((UINT64)1) << (sizeof(T)*8 - 1))) // min int / -1 is not representable.
        {
            ThrowSysArithException();
        }
        // Otherwise...
        if (op == BIO_DivUn)
        {
            res = val1 / val2;
        }
        else
        {
            res = val1 % val2;
        }
    }

    unsigned residx = m_curStackHt - 2;
    OpStackSet<T>(residx, res);
    if (!TypeIsUnchanged)
    {
        OpStackTypeSet(residx, InterpreterType(cit));
    }
}

template<int op>
void Interpreter::ShiftOp()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned op1idx = m_curStackHt - 2;
    unsigned op2idx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(op1idx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    InterpreterType t2 = OpStackTypeGet(op2idx);
    CorInfoType cit2 = t2.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit2));

    // In all cases belows, since "op" is compile-time constant, "if" chains on it should fold away.
    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        ShiftOpWork<op, INT32, UINT32>(op1idx, cit2);
        break;

    case CORINFO_TYPE_NATIVEINT:
        ShiftOpWork<op, NativeInt, NativeUInt>(op1idx, cit2);
        break;

    case CORINFO_TYPE_LONG:
        ShiftOpWork<op, INT64, UINT64>(op1idx, cit2);
        break;

    default:
        VerificationError("Illegal value type for shift operation.");
        break;
    }

    m_curStackHt--;
}

template<int op, typename T, typename UT>
void Interpreter::ShiftOpWork(unsigned op1idx, CorInfoType cit2)
{
    T val = OpStackGet<T>(op1idx);
    unsigned op2idx = op1idx + 1;
    T res = 0;

    if (cit2 == CORINFO_TYPE_INT)
    {
        INT32 shiftAmt = OpStackGet<INT32>(op2idx);
        if (op == CEE_SHL)
        {
            res = val << shiftAmt; // TODO: Check that C++ semantics matches IL.
        }
        else if (op == CEE_SHR)
        {
            res = val >> shiftAmt;
        }
        else
        {
            _ASSERTE(op == CEE_SHR_UN);
            res = (static_cast<UT>(val)) >> shiftAmt;
        }
    }
    else if (cit2 == CORINFO_TYPE_NATIVEINT)
    {
        NativeInt shiftAmt = OpStackGet<NativeInt>(op2idx);
        if (op == CEE_SHL)
        {
            res = val << shiftAmt; // TODO: Check that C++ semantics matches IL.
        }
        else if (op == CEE_SHR)
        {
            res = val >> shiftAmt;
        }
        else
        {
            _ASSERTE(op == CEE_SHR_UN);
            res = (static_cast<UT>(val)) >> shiftAmt;
        }
    }
    else
    {
        VerificationError("Operand type mismatch for shift operator.");
    }
    OpStackSet<T>(op1idx, res);
}


void Interpreter::Neg()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned opidx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(opidx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        OpStackSet<INT32>(opidx, -OpStackGet<INT32>(opidx));
        break;

    case CORINFO_TYPE_NATIVEINT:
        OpStackSet<NativeInt>(opidx, -OpStackGet<NativeInt>(opidx));
        break;

    case CORINFO_TYPE_LONG:
        OpStackSet<INT64>(opidx, -OpStackGet<INT64>(opidx));
        break;

    case CORINFO_TYPE_FLOAT:
        OpStackSet<float>(opidx, -OpStackGet<float>(opidx));
        break;

    case CORINFO_TYPE_DOUBLE:
        OpStackSet<double>(opidx, -OpStackGet<double>(opidx));
        break;

    default:
        VerificationError("Illegal operand type for Neg operation.");
    }
}

void Interpreter::Not()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned opidx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(opidx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        OpStackSet<INT32>(opidx, ~OpStackGet<INT32>(opidx));
        break;

    case CORINFO_TYPE_NATIVEINT:
        OpStackSet<NativeInt>(opidx, ~OpStackGet<NativeInt>(opidx));
        break;

    case CORINFO_TYPE_LONG:
        OpStackSet<INT64>(opidx, ~OpStackGet<INT64>(opidx));
        break;

    default:
        VerificationError("Illegal operand type for Not operation.");
    }
}

template<typename T, bool TIsUnsigned, bool TCanHoldPtr, bool TIsShort, CorInfoType cit>
void Interpreter::Conv()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned opidx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(opidx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    T val;
    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        if (TIsUnsigned)
        {
            // Must convert the 32 bit value to unsigned first, so that we zero-extend if necessary.
            val = static_cast<T>(static_cast<UINT32>(OpStackGet<INT32>(opidx)));
        }
        else
        {
            val = static_cast<T>(OpStackGet<INT32>(opidx));
        }
        break;

    case CORINFO_TYPE_NATIVEINT:
        if (TIsUnsigned)
        {
            // NativeInt might be 32 bits, so convert to unsigned before possibly widening.
            val = static_cast<T>(static_cast<NativeUInt>(OpStackGet<NativeInt>(opidx)));
        }
        else
        {
            val = static_cast<T>(OpStackGet<NativeInt>(opidx));
        }
        break;

    case CORINFO_TYPE_LONG:
        val = static_cast<T>(OpStackGet<INT64>(opidx));
        break;

        // TODO: Make sure that the C++ conversions do the right thing (truncate to zero...)
    case CORINFO_TYPE_FLOAT:
        val = static_cast<T>(OpStackGet<float>(opidx));
        break;

    case CORINFO_TYPE_DOUBLE:
        val = static_cast<T>(OpStackGet<double>(opidx));
        break;

    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_STRING:
        if (!TCanHoldPtr && !s_InterpreterLooseRules)
        {
            VerificationError("Conversion of pointer value to type that can't hold its value.");
        }

        // Otherwise...
        // (Must first convert to NativeInt, because the compiler believes this might be applied for T =
        // float or double.  It won't, by the test above, and the extra cast shouldn't generate any code...)
        val = static_cast<T>(reinterpret_cast<NativeInt>(OpStackGet<void*>(opidx)));
        break;

    default:
        VerificationError("Illegal operand type for conv.* operation.");
        UNREACHABLE();
    }

    if (TIsShort)
    {
        OpStackSet<INT32>(opidx, static_cast<INT32>(val));
    }
    else
    {
        OpStackSet<T>(opidx, val);
    }

    OpStackTypeSet(opidx, InterpreterType(cit));
}


void Interpreter::ConvRUn()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned opidx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(opidx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        OpStackSet<double>(opidx, static_cast<double>(OpStackGet<UINT32>(opidx)));
        break;

    case CORINFO_TYPE_NATIVEINT:
        OpStackSet<double>(opidx, static_cast<double>(OpStackGet<NativeUInt>(opidx)));
        break;

    case CORINFO_TYPE_LONG:
        OpStackSet<double>(opidx, static_cast<double>(OpStackGet<UINT64>(opidx)));
        break;

    case CORINFO_TYPE_DOUBLE:
        return;

    default:
        VerificationError("Illegal operand type for conv.r.un operation.");
    }

    OpStackTypeSet(opidx, InterpreterType(CORINFO_TYPE_DOUBLE));
}

template<typename T, INT64 TMin, UINT64 TMax, bool TCanHoldPtr, CorInfoType cit>
void Interpreter::ConvOvf()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned opidx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(opidx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        {
            INT32 i4 = OpStackGet<INT32>(opidx);
            if (!FitsIn<T>(i4))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(i4));
        }
        break;

    case CORINFO_TYPE_NATIVEINT:
        {
            NativeInt i = OpStackGet<NativeInt>(opidx);
            if (!FitsIn<T>(i))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(i));
        }
        break;

    case CORINFO_TYPE_LONG:
        {
            INT64 i8 = OpStackGet<INT64>(opidx);
            if (!FitsIn<T>(i8))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(i8));
        }
        break;

        // Make sure that the C++ conversions do the right thing (truncate to zero...)
    case CORINFO_TYPE_FLOAT:
        {
            float f = OpStackGet<float>(opidx);
            if (!FloatFitsInIntType<TMin, TMax>(f))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(f));
        }
        break;

    case CORINFO_TYPE_DOUBLE:
         {
            double d = OpStackGet<double>(opidx);
            if (!DoubleFitsInIntType<TMin, TMax>(d))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(d));
        }
        break;

    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_STRING:
        if (!TCanHoldPtr)
        {
            VerificationError("Conversion of pointer value to type that can't hold its value.");
        }

        // Otherwise...
        // (Must first convert to NativeInt, because the compiler believes this might be applied for T =
        // float or double.  It won't, by the test above, and the extra cast shouldn't generate any code...
        OpStackSet<T>(opidx, static_cast<T>(reinterpret_cast<NativeInt>(OpStackGet<void*>(opidx))));
        break;

    default:
        VerificationError("Illegal operand type for conv.ovf.* operation.");
    }

    _ASSERTE_MSG(IsStackNormalType(cit), "Precondition.");
    OpStackTypeSet(opidx, InterpreterType(cit));
}

template<typename T, INT64 TMin, UINT64 TMax, bool TCanHoldPtr, CorInfoType cit>
void Interpreter::ConvOvfUn()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned opidx = m_curStackHt - 1;

    InterpreterType t1 = OpStackTypeGet(opidx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));

    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        {
            UINT32 ui4 = OpStackGet<UINT32>(opidx);
            if (!FitsIn<T>(ui4))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(ui4));
        }
        break;

    case CORINFO_TYPE_NATIVEINT:
        {
            NativeUInt ui = OpStackGet<NativeUInt>(opidx);
            if (!FitsIn<T>(ui))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(ui));
        }
        break;

    case CORINFO_TYPE_LONG:
        {
            UINT64 ui8 = OpStackGet<UINT64>(opidx);
            if (!FitsIn<T>(ui8))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(ui8));
        }
        break;

        // Make sure that the C++ conversions do the right thing (truncate to zero...)
    case CORINFO_TYPE_FLOAT:
        {
            float f = OpStackGet<float>(opidx);
            if (!FloatFitsInIntType<TMin, TMax>(f))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(f));
        }
        break;

    case CORINFO_TYPE_DOUBLE:
         {
            double d = OpStackGet<double>(opidx);
            if (!DoubleFitsInIntType<TMin, TMax>(d))
            {
                ThrowOverflowException();
            }
            OpStackSet<T>(opidx, static_cast<T>(d));
        }
        break;

    case CORINFO_TYPE_BYREF:
    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_STRING:
        if (!TCanHoldPtr)
        {
            VerificationError("Conversion of pointer value to type that can't hold its value.");
        }

        // Otherwise...
        // (Must first convert to NativeInt, because the compiler believes this might be applied for T =
        // float or double.  It won't, by the test above, and the extra cast shouldn't generate any code...
        OpStackSet<T>(opidx, static_cast<T>(reinterpret_cast<NativeInt>(OpStackGet<void*>(opidx))));
        break;

    default:
        VerificationError("Illegal operand type for conv.ovf.*.un operation.");
    }

    _ASSERTE_MSG(IsStackNormalType(cit), "Precondition.");
    OpStackTypeSet(opidx, InterpreterType(cit));
}

void Interpreter::LdObj()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    BarrierIfVolatile();

    _ASSERTE(m_curStackHt > 0);
    unsigned ind = m_curStackHt - 1;

#ifdef _DEBUG
    CorInfoType cit = OpStackTypeGet(ind).ToCorInfoType();
    _ASSERTE_MSG(IsValidPointerType(cit), "Expect pointer on stack");
#endif // _DEBUG

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_LdObj]);
#endif // INTERP_TRACING

    // TODO: GetTypeFromToken also uses GCX_PREEMP(); can we merge it with the getClassAttribs() block below, and do it just once?
    CORINFO_CLASS_HANDLE clsHnd = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_LdObj));
    DWORD clsAttribs;
    {
        GCX_PREEMP();
        clsAttribs = m_interpCeeInfo.getClassAttribs(clsHnd);
    }

    void* src = OpStackGet<void*>(ind);
    ThrowOnInvalidPointer(src);

    if (clsAttribs & CORINFO_FLG_VALUECLASS)
    {
        LdObjValueClassWork(clsHnd, ind, src);
    }
    else
    {
        OpStackSet<void*>(ind, *reinterpret_cast<void**>(src));
        OpStackTypeSet(ind, InterpreterType(CORINFO_TYPE_CLASS));
    }
    m_ILCodePtr += 5;
}

void Interpreter::LdObjValueClassWork(CORINFO_CLASS_HANDLE valueClsHnd, unsigned ind, void* src)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // "src" is a byref, which may be into an object.  GCPROTECT for the call below.
    GCPROTECT_BEGININTERIOR(src);

    InterpreterType it = InterpreterType(&m_interpCeeInfo, valueClsHnd);
    size_t sz = it.Size(&m_interpCeeInfo);
    // Note that the memcpy's below are permissible because the destination is in the operand stack.
    if (sz > sizeof(INT64))
    {
        void* dest = LargeStructOperandStackPush(sz);
        memcpy(dest, src, sz);
        OpStackSet<void*>(ind, dest);
    }
    else
    {
        OpStackSet<INT64>(ind, GetSmallStructValue(src, sz));
    }

    OpStackTypeSet(ind, it.StackNormalize());

    GCPROTECT_END();
}

CORINFO_CLASS_HANDLE Interpreter::GetTypeFromToken(BYTE* codePtr, CorInfoTokenKind tokKind  InterpTracingArg(ResolveTokenKind rtk))
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    GCX_PREEMP();

    CORINFO_RESOLVED_TOKEN typeTok;
    ResolveToken(&typeTok, getU4LittleEndian(codePtr), tokKind InterpTracingArg(rtk));
    return typeTok.hClass;
}

bool Interpreter::IsValidPointerType(CorInfoType cit)
{
    bool isValid = (cit == CORINFO_TYPE_NATIVEINT || cit == CORINFO_TYPE_BYREF);
#if defined(HOST_AMD64)
    isValid = isValid || (s_InterpreterLooseRules && cit == CORINFO_TYPE_LONG);
#endif
    return isValid;
}

void Interpreter::CpObj()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned destInd = m_curStackHt - 2;
    unsigned srcInd  = m_curStackHt - 1;

#ifdef _DEBUG
    // Check that src and dest are both pointer types.
    CorInfoType cit = OpStackTypeGet(destInd).ToCorInfoType();
    _ASSERTE_MSG(IsValidPointerType(cit), "Expect pointer on stack for dest of cpobj");

    cit = OpStackTypeGet(srcInd).ToCorInfoType();
    _ASSERTE_MSG(IsValidPointerType(cit), "Expect pointer on stack for src of cpobj");
#endif // _DEBUG

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_CpObj]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE clsHnd = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_CpObj));
    DWORD clsAttribs;
    {
        GCX_PREEMP();
        clsAttribs = m_interpCeeInfo.getClassAttribs(clsHnd);
    }

    void* dest = OpStackGet<void*>(destInd);
    void* src  = OpStackGet<void*>(srcInd);

    ThrowOnInvalidPointer(dest);
    ThrowOnInvalidPointer(src);

    // dest and src are vulnerable byrefs.
    GCX_FORBID();

    if (clsAttribs & CORINFO_FLG_VALUECLASS)
    {
        CopyValueClassUnchecked(dest, src, GetMethodTableFromClsHnd(clsHnd));
    }
    else
    {
        OBJECTREF val = *reinterpret_cast<OBJECTREF*>(src);
        SetObjectReference(reinterpret_cast<OBJECTREF*>(dest), val);
    }
    m_curStackHt -= 2;
    m_ILCodePtr += 5;
}

void Interpreter::StObj()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned destInd = m_curStackHt - 2;
    unsigned valInd  = m_curStackHt - 1;

#ifdef _DEBUG
    // Check that dest is a pointer type.
    CorInfoType cit = OpStackTypeGet(destInd).ToCorInfoType();
    _ASSERTE_MSG(IsValidPointerType(cit), "Expect pointer on stack for dest of stobj");
#endif // _DEBUG

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_StObj]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE clsHnd = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_StObj));
    DWORD clsAttribs;
    {
        GCX_PREEMP();
        clsAttribs = m_interpCeeInfo.getClassAttribs(clsHnd);
    }

    if (clsAttribs & CORINFO_FLG_VALUECLASS)
    {
        MethodTable* clsMT = GetMethodTableFromClsHnd(clsHnd);
        size_t sz;
        {
            GCX_PREEMP();
            sz = getClassSize(clsHnd);
        }

        // Note that "dest" might be a pointer into the heap.  It is therefore important
        // to calculate it *after* any PREEMP transitions at which we might do a GC.
        void* dest = OpStackGet<void*>(destInd);
        ThrowOnInvalidPointer(dest);

#ifdef _DEBUG
        // Try and validate types
        InterpreterType vit = OpStackTypeGet(valInd);
        CorInfoType vitc = vit.ToCorInfoType();

        if (vitc == CORINFO_TYPE_VALUECLASS)
        {
            CORINFO_CLASS_HANDLE vClsHnd = vit.ToClassHandle();
            const bool isClass = (vClsHnd == clsHnd);
            const bool isPrim = (vitc == CorInfoTypeStackNormalize(GetTypeForPrimitiveValueClass(clsHnd)));
            bool isShared = false;

            // If operand type is shared we need a more complex check;
            // the IL type may not be shared
            if (!isPrim && !isClass)
            {
                DWORD vClsAttribs;
                {
                    GCX_PREEMP();
                    vClsAttribs = m_interpCeeInfo.getClassAttribs(vClsHnd);
                }

                if ((vClsAttribs & CORINFO_FLG_SHAREDINST) != 0)
                {
                    MethodTable* clsMT2 = clsMT->GetCanonicalMethodTable();
                    if (((CORINFO_CLASS_HANDLE) clsMT2) == vClsHnd)
                    {
                        isShared = true;
                    }
                }
            }

            _ASSERTE(isClass || isPrim || isShared);
        }
        else
        {
            const bool isSz = s_InterpreterLooseRules && sz <= sizeof(dest);
            _ASSERTE(isSz);
        }

#endif // _DEBUG

        GCX_FORBID();

        if (sz > sizeof(INT64))
        {
            // Large struct case -- ostack entry is pointer.
            void* src = OpStackGet<void*>(valInd);
            CopyValueClassUnchecked(dest, src, clsMT);
            LargeStructOperandStackPop(sz, src);
        }
        else
        {
            // The ostack entry contains the struct value.
            CopyValueClassUnchecked(dest, OpStackGetAddr(valInd, sz), clsMT);
        }
    }
    else
    {
        // The ostack entry is an object reference.
        _ASSERTE(OpStackTypeGet(valInd).ToCorInfoType() == CORINFO_TYPE_CLASS);

        // Note that "dest" might be a pointer into the heap.  It is therefore important
        // to calculate it *after* any PREEMP transitions at which we might do a GC.  (Thus,
        // we have to duplicate this code with the case above.
        void* dest = OpStackGet<void*>(destInd);
        ThrowOnInvalidPointer(dest);

        GCX_FORBID();

        OBJECTREF val = ObjectToOBJECTREF(OpStackGet<Object*>(valInd));
        SetObjectReference(reinterpret_cast<OBJECTREF*>(dest), val);
    }

    m_curStackHt -= 2;
    m_ILCodePtr += 5;

    BarrierIfVolatile();
}

void Interpreter::InitObj()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned destInd = m_curStackHt - 1;
#ifdef _DEBUG
    // Check that src and dest are both pointer types.
    CorInfoType cit = OpStackTypeGet(destInd).ToCorInfoType();
    _ASSERTE_MSG(IsValidPointerType(cit), "Expect pointer on stack");
#endif // _DEBUG

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_InitObj]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE clsHnd = GetTypeFromToken(m_ILCodePtr + 2, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_InitObj));
    size_t valueClassSz = 0;

    DWORD clsAttribs;
    {
        GCX_PREEMP();
        clsAttribs = m_interpCeeInfo.getClassAttribs(clsHnd);
        if (clsAttribs & CORINFO_FLG_VALUECLASS)
        {
            valueClassSz = getClassSize(clsHnd);
        }
    }

    void* dest = OpStackGet<void*>(destInd);
    ThrowOnInvalidPointer(dest);

    // dest is a vulnerable byref.
    GCX_FORBID();

    if (clsAttribs & CORINFO_FLG_VALUECLASS)
    {
        memset(dest, 0, valueClassSz);
    }
    else
    {
        // The ostack entry is an object reference.
        SetObjectReference(reinterpret_cast<OBJECTREF*>(dest), NULL);
    }
    m_curStackHt -= 1;
    m_ILCodePtr += 6;
}

void Interpreter::LdStr()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OBJECTHANDLE res = ConstructStringLiteral(m_methInfo->m_module, getU4LittleEndian(m_ILCodePtr + 1));
    {
        GCX_FORBID();
        OpStackSet<Object*>(m_curStackHt, *reinterpret_cast<Object**>(res));
        OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_CLASS));  // Stack-normal type for "string"
        m_curStackHt++;
    }
    m_ILCodePtr += 5;
}

void Interpreter::NewObj()
{
#if INTERP_DYNAMIC_CONTRACTS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
#else
    // Dynamic contract occupies too much stack.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
#endif

    unsigned ctorTok = getU4LittleEndian(m_ILCodePtr + 1);

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_NewObj]);
#endif // INTERP_TRACING

    CORINFO_CALL_INFO callInfo;
    CORINFO_RESOLVED_TOKEN methTok;

    {
        GCX_PREEMP();
        ResolveToken(&methTok, ctorTok, CORINFO_TOKENKIND_Ldtoken InterpTracingArg(RTK_NewObj));
        m_interpCeeInfo.getCallInfo(&methTok, NULL,
                                    m_methInfo->m_method,
                                    CORINFO_CALLINFO_FLAGS(0),
                                    &callInfo);
    }

    unsigned mflags = callInfo.methodFlags;

    if ((mflags & (CORINFO_FLG_STATIC|CORINFO_FLG_ABSTRACT)) != 0)
    {
        VerificationError("newobj on static or abstract method");
    }

    unsigned clsFlags = callInfo.classFlags;

#ifdef _DEBUG
    // What class are we allocating?
    const char* clsName;

    {
        GCX_PREEMP();
        clsName = m_interpCeeInfo.getClassName(methTok.hClass);
    }
#endif // _DEBUG

    // There are four cases:
    // 1) Value types (ordinary constructor, resulting VALUECLASS pushed)
    // 2) String (var-args constructor, result automatically pushed)
    // 3) MDArray (var-args constructor, resulting OBJECTREF pushed)
    // 4) Reference types (ordinary constructor, resulting OBJECTREF pushed)
    if (clsFlags & CORINFO_FLG_VALUECLASS)
    {
        void* tempDest;
        INT64 smallTempDest = 0;
        size_t sz = 0;
        {
            GCX_PREEMP();
            sz = getClassSize(methTok.hClass);
        }
        if (sz > sizeof(INT64))
        {
            // TODO: Make sure this is deleted in the face of exceptions.
            tempDest = new BYTE[sz];
        }
        else
        {
            tempDest = &smallTempDest;
        }
        memset(tempDest, 0, sz);
        InterpreterType structValRetIT(&m_interpCeeInfo, methTok.hClass);
        m_structRetValITPtr = &structValRetIT;
        m_structRetValTempSpace = tempDest;

        DoCallWork(/*virtCall*/false, tempDest, &methTok, &callInfo);

        if (sz > sizeof(INT64))
        {
            void* dest = LargeStructOperandStackPush(sz);
            memcpy(dest, tempDest, sz);
            delete[] reinterpret_cast<BYTE*>(tempDest);
            OpStackSet<void*>(m_curStackHt, dest);
        }
        else
        {
            OpStackSet<INT64>(m_curStackHt, GetSmallStructValue(tempDest, sz));
        }
        if (m_structRetValITPtr->IsStruct())
        {
            OpStackTypeSet(m_curStackHt, *m_structRetValITPtr);
        }
        else
        {
            // Must stack-normalize primitive types.
            OpStackTypeSet(m_curStackHt, m_structRetValITPtr->StackNormalize());
        }
        // "Unregister" the temp space for GC scanning...
        m_structRetValITPtr = NULL;
        m_curStackHt++;
    }
    else if ((clsFlags & CORINFO_FLG_VAROBJSIZE) && !(clsFlags & CORINFO_FLG_ARRAY))
    {
        // For a VAROBJSIZE class (currently == String), pass NULL as this to "pseudo-constructor."
        void* specialFlagArg = reinterpret_cast<void*>(0x1);  // Special value for "thisArg" argument of "DoCallWork": push NULL that's not on op stack.
        DoCallWork(/*virtCall*/false, specialFlagArg, &methTok, &callInfo);  // pushes result automatically
    }
    else
    {
        OBJECTREF thisArgObj = NULL;
        GCPROTECT_BEGIN(thisArgObj);

        if (clsFlags & CORINFO_FLG_ARRAY)
        {
            _ASSERTE(clsFlags & CORINFO_FLG_VAROBJSIZE);

            MethodDesc* methDesc = GetMethod(methTok.hMethod);

            PCCOR_SIGNATURE pSig;
            DWORD cbSigSize;
            methDesc->GetSig(&pSig, &cbSigSize);
            MetaSig msig(pSig, cbSigSize, methDesc->GetModule(), NULL);

            unsigned dwNumArgs = msig.NumFixedArgs();
            _ASSERTE(m_curStackHt >= dwNumArgs);
            m_curStackHt -= dwNumArgs;

            INT32* args = (INT32*)_alloca(dwNumArgs * sizeof(INT32));

            unsigned dwArg;
            for (dwArg = 0; dwArg < dwNumArgs; dwArg++)
            {
                unsigned stkInd = m_curStackHt + dwArg;
                bool loose = s_InterpreterLooseRules && (OpStackTypeGet(stkInd).ToCorInfoType() == CORINFO_TYPE_NATIVEINT);
                if (OpStackTypeGet(stkInd).ToCorInfoType() != CORINFO_TYPE_INT && !loose)
                {
                    VerificationError("MD array dimension bounds and sizes must be int.");
                }
                args[dwArg] = loose ? (INT32) OpStackGet<NativeInt>(stkInd) : OpStackGet<INT32>(stkInd);
            }

            thisArgObj = AllocateArrayEx(TypeHandle(methTok.hClass), args, dwNumArgs);
        }
        else
        {
            CorInfoHelpFunc newHelper;
            {
                GCX_PREEMP();
                bool sideEffect;
                newHelper = m_interpCeeInfo.getNewHelper(&methTok, m_methInfo->m_method, &sideEffect);
            }

            MethodTable * pNewObjMT = GetMethodTableFromClsHnd(methTok.hClass);
            switch (newHelper)
            {
            case CORINFO_HELP_NEWFAST:
            default:
                thisArgObj = AllocateObject(pNewObjMT);
                break;
            }

            DoCallWork(/*virtCall*/false, OBJECTREFToObject(thisArgObj), &methTok, &callInfo);
        }

        {
            GCX_FORBID();
            OpStackSet<Object*>(m_curStackHt, OBJECTREFToObject(thisArgObj));
            OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_CLASS));
            m_curStackHt++;
        }
        GCPROTECT_END();  // For "thisArgObj"
    }

    m_ILCodePtr += 5;
}

void Interpreter::NewArr()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned stkInd = m_curStackHt-1;
    CorInfoType cit = OpStackTypeGet(stkInd).ToCorInfoType();
    NativeInt sz = 0;
    switch (cit)
    {
    case CORINFO_TYPE_INT:
        sz = static_cast<NativeInt>(OpStackGet<INT32>(stkInd));
        break;
    case CORINFO_TYPE_NATIVEINT:
        sz = OpStackGet<NativeInt>(stkInd);
        break;
    default:
        VerificationError("Size operand of 'newarr' must be int or native int.");
    }

    unsigned elemTypeTok = getU4LittleEndian(m_ILCodePtr + 1);

    CORINFO_CLASS_HANDLE elemClsHnd;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_NewArr]);
#endif // INTERP_TRACING

    CORINFO_RESOLVED_TOKEN elemTypeResolvedTok;

    {
        GCX_PREEMP();
        ResolveToken(&elemTypeResolvedTok, elemTypeTok, CORINFO_TOKENKIND_Newarr InterpTracingArg(RTK_NewArr));
        elemClsHnd = elemTypeResolvedTok.hClass;
    }

    {
        if (sz < 0)
        {
            COMPlusThrow(kOverflowException);
        }

#ifdef HOST_64BIT
        // Even though ECMA allows using a native int as the argument to newarr instruction
        // (therefore size is INT_PTR), ArrayBase::m_NumComponents is 32-bit, so even on 64-bit
        // platforms we can't create an array whose size exceeds 32 bits.
        if (sz > INT_MAX)
        {
            EX_THROW(EEMessageException, (kOverflowException, IDS_EE_ARRAY_DIMENSIONS_EXCEEDED));
        }
#endif

        TypeHandle th(elemClsHnd);
        MethodTable* pArrayMT = th.GetMethodTable();
        pArrayMT->CheckRunClassInitThrowing();

        INT32 size32 = (INT32)sz;
        Object* newarray = OBJECTREFToObject(AllocateSzArray(pArrayMT, size32));

        GCX_FORBID();
        OpStackTypeSet(stkInd, InterpreterType(CORINFO_TYPE_CLASS));
        OpStackSet<Object*>(stkInd, newarray);
    }

    m_ILCodePtr += 5;
}

void Interpreter::IsInst()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_IsInst]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE cls = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Casting  InterpTracingArg(RTK_IsInst));

    _ASSERTE(m_curStackHt >= 1);
    unsigned idx = m_curStackHt - 1;
#ifdef _DEBUG
    CorInfoType cit = OpStackTypeGet(idx).ToCorInfoType();
    _ASSERTE(cit == CORINFO_TYPE_CLASS || cit == CORINFO_TYPE_STRING);
#endif // DEBUG

    Object * pObj = OpStackGet<Object*>(idx);
    if (pObj != NULL)
    {
        if (!ObjIsInstanceOf(pObj, TypeHandle(cls)))
            OpStackSet<Object*>(idx, NULL);
    }

    // Type stack stays unmodified.

    m_ILCodePtr += 5;
}

void Interpreter::CastClass()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_CastClass]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE cls = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Casting  InterpTracingArg(RTK_CastClass));

    _ASSERTE(m_curStackHt >= 1);
    unsigned idx = m_curStackHt - 1;
#ifdef _DEBUG
    CorInfoType cit = OpStackTypeGet(idx).ToCorInfoType();
    _ASSERTE(cit == CORINFO_TYPE_CLASS || cit == CORINFO_TYPE_STRING);
#endif // _DEBUG

    Object * pObj = OpStackGet<Object*>(idx);
    if (pObj != NULL)
    {
        if (!ObjIsInstanceOf(pObj, TypeHandle(cls), TRUE))
        {
            UNREACHABLE(); //ObjIsInstanceOf will throw if cast can't be done
        }
    }


    // Type stack stays unmodified.

    m_ILCodePtr += 5;
}

void Interpreter::LocAlloc()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned idx = m_curStackHt - 1;
    CorInfoType cit = OpStackTypeGet(idx).ToCorInfoType();
    NativeUInt sz = 0;
    if (cit == CORINFO_TYPE_INT || cit == CORINFO_TYPE_UINT)
    {
        sz = static_cast<NativeUInt>(OpStackGet<UINT32>(idx));
    }
    else if (cit == CORINFO_TYPE_NATIVEINT || cit == CORINFO_TYPE_NATIVEUINT)
    {
        sz = OpStackGet<NativeUInt>(idx);
    }
    else if (s_InterpreterLooseRules && cit == CORINFO_TYPE_LONG)
    {
        sz = (NativeUInt) OpStackGet<INT64>(idx);
    }
    else
    {
        VerificationError("localloc requires int or nativeint argument.");
    }
    if (sz == 0)
    {
        OpStackSet<void*>(idx, NULL);
    }
    else
    {
        void* res = GetLocAllocData()->Alloc(sz);
        if (res == NULL) ThrowStackOverflow();
        OpStackSet<void*>(idx, res);
    }
    OpStackTypeSet(idx, InterpreterType(CORINFO_TYPE_NATIVEINT));
}

void Interpreter::MkRefany()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_MkRefAny]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE cls = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_MkRefAny));
    _ASSERTE(m_curStackHt >= 1);
    unsigned idx = m_curStackHt - 1;

    CorInfoType cit = OpStackTypeGet(idx).ToCorInfoType();
    if (!(cit == CORINFO_TYPE_BYREF || cit == CORINFO_TYPE_NATIVEINT))
        VerificationError("MkRefany requires byref or native int (pointer) on the stack.");

    void* ptr = OpStackGet<void*>(idx);

    InterpreterType typedRefIT = GetTypedRefIT(&m_interpCeeInfo);
    TypedByRef* tbr;
#if defined(HOST_AMD64)
    _ASSERTE(typedRefIT.IsLargeStruct(&m_interpCeeInfo));
    tbr = (TypedByRef*) LargeStructOperandStackPush(GetTypedRefSize(&m_interpCeeInfo));
    OpStackSet<void*>(idx, tbr);
#elif defined(HOST_X86) || defined(HOST_ARM)
    _ASSERTE(!typedRefIT.IsLargeStruct(&m_interpCeeInfo));
    tbr = OpStackGetAddr<TypedByRef>(idx);
#elif defined(HOST_ARM64)
    tbr = NULL;
    NYI_INTERP("Unimplemented code: MkRefAny");
#else
#error "unsupported platform"
#endif
    tbr->data = ptr;
    tbr->type = TypeHandle(cls);
    OpStackTypeSet(idx, typedRefIT);

    m_ILCodePtr += 5;
}

void Interpreter::RefanyType()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned idx = m_curStackHt - 1;

    if (OpStackTypeGet(idx) != GetTypedRefIT(&m_interpCeeInfo))
        VerificationError("RefAnyVal requires a TypedRef on the stack.");

    TypedByRef* ptbr = OpStackGet<TypedByRef*>(idx);
    LargeStructOperandStackPop(sizeof(TypedByRef), ptbr);

    TypeHandle* pth = &ptbr->type;

    {
        OBJECTREF classobj = TypeHandleToTypeRef(pth);
        GCX_FORBID();
        OpStackSet<Object*>(idx, OBJECTREFToObject(classobj));
        OpStackTypeSet(idx, InterpreterType(CORINFO_TYPE_CLASS));
    }
    m_ILCodePtr += 2;
}

// This (unfortunately) duplicates code in JIT_GetRuntimeTypeHandle, which
// isn't callable because it sets up a Helper Method Frame.
OBJECTREF Interpreter::TypeHandleToTypeRef(TypeHandle* pth)
{
    OBJECTREF typePtr = NULL;
    if (!pth->IsTypeDesc())
    {
        // Most common... and fastest case
        typePtr = pth->AsMethodTable()->GetManagedClassObjectIfExists();
        if (typePtr == NULL)
        {
            typePtr = pth->GetManagedClassObject();
        }
    }
    else
    {
        typePtr = pth->GetManagedClassObject();
    }
    return typePtr;
}

CorInfoType Interpreter::GetTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    GCX_PREEMP();

    return m_interpCeeInfo.getTypeForPrimitiveValueClass(clsHnd);
}

void Interpreter::RefanyVal()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned idx = m_curStackHt - 1;

    if (OpStackTypeGet(idx) != GetTypedRefIT(&m_interpCeeInfo))
        VerificationError("RefAnyVal requires a TypedRef on the stack.");

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_RefAnyVal]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE cls = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_RefAnyVal));
    TypeHandle expected(cls);

    TypedByRef* ptbr = OpStackGet<TypedByRef*>(idx);
    LargeStructOperandStackPop(sizeof(TypedByRef), ptbr);
    if (expected != ptbr->type) ThrowInvalidCastException();

    OpStackSet<void*>(idx, static_cast<void*>(ptbr->data));
    OpStackTypeSet(idx, InterpreterType(CORINFO_TYPE_BYREF));

    m_ILCodePtr += 5;
}

void Interpreter::CkFinite()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned idx = m_curStackHt - 1;

    CorInfoType cit = OpStackTypeGet(idx).ToCorInfoType();
    double val = 0.0;

    switch (cit)
    {
    case CORINFO_TYPE_FLOAT:
        val = (double)OpStackGet<float>(idx);
        break;
    case CORINFO_TYPE_DOUBLE:
        val = OpStackGet<double>(idx);
        break;
    default:
        VerificationError("CkFinite requires a floating-point value on the stack.");
        break;
    }

    if (!_finite(val))
        ThrowSysArithException();
}

void Interpreter::LdToken()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    unsigned tokVal = getU4LittleEndian(m_ILCodePtr + 1);

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_LdToken]);
#endif // INTERP_TRACING


    CORINFO_RESOLVED_TOKEN tok;
    {
        GCX_PREEMP();
        ResolveToken(&tok, tokVal, CORINFO_TOKENKIND_Ldtoken InterpTracingArg(RTK_LdToken));
    }

    // To save duplication of the factored code at the bottom, I don't do GCX_FORBID for
    // these Object* values, but this comment documents the intent.
    if (tok.hMethod != NULL)
    {
        MethodDesc* pMethod = (MethodDesc*)tok.hMethod;
        Object* objPtr = OBJECTREFToObject((OBJECTREF)pMethod->GetStubMethodInfo());
        OpStackSet<Object*>(m_curStackHt, objPtr);
    }
    else if (tok.hField != NULL)
    {
        FieldDesc * pField = (FieldDesc *)tok.hField;
        Object* objPtr = OBJECTREFToObject((OBJECTREF)pField->GetStubFieldInfo());
        OpStackSet<Object*>(m_curStackHt, objPtr);
    }
    else
    {
        TypeHandle th(tok.hClass);
        Object* objPtr = OBJECTREFToObject(th.GetManagedClassObject());
        OpStackSet<Object*>(m_curStackHt, objPtr);
    }

    {
        GCX_FORBID();
        OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_CLASS));
        m_curStackHt++;
    }

    m_ILCodePtr += 5;
}

void Interpreter::LdFtn()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    unsigned tokVal = getU4LittleEndian(m_ILCodePtr + 2);

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_LdFtn]);
#endif // INTERP_TRACING

    CORINFO_RESOLVED_TOKEN tok;
    CORINFO_CALL_INFO callInfo;
    {
        GCX_PREEMP();
        ResolveToken(&tok, tokVal, CORINFO_TOKENKIND_Method InterpTracingArg(RTK_LdFtn));
        m_interpCeeInfo.getCallInfo(&tok, NULL, m_methInfo->m_method,
                                  combine(CORINFO_CALLINFO_SECURITYCHECKS,CORINFO_CALLINFO_LDFTN),
                                  &callInfo);
    }

    switch (callInfo.kind)
    {
    case CORINFO_CALL:
        {
            PCODE pCode = ((MethodDesc *)callInfo.hMethod)->GetMultiCallableAddrOfCode();
            OpStackSet<void*>(m_curStackHt, (void *)pCode);
            GetFunctionPointerStack()[m_curStackHt] = callInfo.hMethod;
        }
        break;
    case CORINFO_CALL_CODE_POINTER:
        NYI_INTERP("Indirect code pointer.");
        break;
    default:
        _ASSERTE_MSG(false, "Should not reach here: unknown call kind.");
        break;
    }
    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_NATIVEINT));
    m_curStackHt++;
    m_ILCodePtr += 6;
}

void Interpreter::LdVirtFtn()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned ind = m_curStackHt - 1;

    unsigned tokVal = getU4LittleEndian(m_ILCodePtr + 2);

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_LdVirtFtn]);
#endif // INTERP_TRACING

    CORINFO_RESOLVED_TOKEN tok;
    CORINFO_CALL_INFO callInfo;
    CORINFO_CLASS_HANDLE classHnd;
    CORINFO_METHOD_HANDLE methodHnd;
    {
        GCX_PREEMP();
        ResolveToken(&tok, tokVal, CORINFO_TOKENKIND_Method InterpTracingArg(RTK_LdVirtFtn));
        m_interpCeeInfo.getCallInfo(&tok, NULL, m_methInfo->m_method,
                                    combine(CORINFO_CALLINFO_CALLVIRT,
                                            combine(CORINFO_CALLINFO_SECURITYCHECKS,
                                                    CORINFO_CALLINFO_LDFTN)),
                                    &callInfo);


        classHnd = tok.hClass;
        methodHnd = tok.hMethod;
    }

    MethodDesc * pMD = (MethodDesc *)methodHnd;
    PCODE pCode;
    if (pMD->IsVtableMethod())
    {
        Object* obj = OpStackGet<Object*>(ind);
        ThrowOnInvalidPointer(obj);

        OBJECTREF objRef = ObjectToOBJECTREF(obj);
        GCPROTECT_BEGIN(objRef);
        pCode = pMD->GetMultiCallableAddrOfVirtualizedCode(&objRef, TypeHandle(classHnd));
        GCPROTECT_END();

        pMD = Entry2MethodDesc(pCode, TypeHandle(classHnd).GetMethodTable());
    }
    else
    {
        pCode = pMD->GetMultiCallableAddrOfCode();
    }
    OpStackSet<void*>(ind, (void *)pCode);
    GetFunctionPointerStack()[ind] = (CORINFO_METHOD_HANDLE)pMD;

    OpStackTypeSet(ind, InterpreterType(CORINFO_TYPE_NATIVEINT));
    m_ILCodePtr += 6;
}

void Interpreter::Sizeof()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_Sizeof]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE cls = GetTypeFromToken(m_ILCodePtr + 2, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_Sizeof));
    unsigned sz;
    {
        GCX_PREEMP();
        CorInfoType cit = ::asCorInfoType(cls);
        // For class types, the ECMA spec says to return the size of the object reference, not the referent
        // object.  Everything else should be a value type, for which we can just return the size as reported
        // by the EE.
        switch (cit)
        {
        case CORINFO_TYPE_CLASS:
            sz = sizeof(Object*);
            break;
        default:
            sz = getClassSize(cls);
            break;
        }
    }

    OpStackSet<UINT32>(m_curStackHt, sz);
    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_INT));
    m_curStackHt++;
    m_ILCodePtr += 6;
}


// static:
bool Interpreter::s_initialized = false;
bool Interpreter::s_compilerStaticsInitialized = false;
size_t Interpreter::s_TypedRefSize;
CORINFO_CLASS_HANDLE Interpreter::s_TypedRefClsHnd;
InterpreterType Interpreter::s_TypedRefIT;

// Must call GetTypedRefIT
size_t Interpreter::GetTypedRefSize(CEEInfo* info)
{
    _ASSERTE_MSG(s_compilerStaticsInitialized, "Precondition");
    return s_TypedRefSize;
}

InterpreterType Interpreter::GetTypedRefIT(CEEInfo* info)
{
    _ASSERTE_MSG(s_compilerStaticsInitialized, "Precondition");
    return s_TypedRefIT;
}

CORINFO_CLASS_HANDLE Interpreter::GetTypedRefClsHnd(CEEInfo* info)
{
    _ASSERTE_MSG(s_compilerStaticsInitialized, "Precondition");
    return s_TypedRefClsHnd;
}

void Interpreter::Initialize()
{
    _ASSERTE(!s_initialized);

    s_InterpretMeths.ensureInit(CLRConfig::INTERNAL_Interpret);
    s_InterpretMethsExclude.ensureInit(CLRConfig::INTERNAL_InterpretExclude);
    s_InterpreterUseCaching = (s_InterpreterUseCachingFlag.val(CLRConfig::INTERNAL_InterpreterUseCaching) != 0);
    s_InterpreterLooseRules = (s_InterpreterLooseRulesFlag.val(CLRConfig::INTERNAL_InterpreterLooseRules) != 0);
    s_InterpreterDoLoopMethods = (s_InterpreterDoLoopMethodsFlag.val(CLRConfig::INTERNAL_InterpreterDoLoopMethods) != 0);

    // Initialize the lock used to protect method locks.
    // TODO: it would be better if this were a reader/writer lock.
    s_methodCacheLock.Init(CrstLeafLock, CRST_DEFAULT);

    // Similarly, initialize the lock used to protect the map from
    // interpreter stub addresses to their method descs.
    s_interpStubToMDMapLock.Init(CrstLeafLock, CRST_DEFAULT);

    s_initialized = true;

#if INTERP_ILINSTR_PROFILE
    SetILInstrCategories();
#endif // INTERP_ILINSTR_PROFILE
}

void Interpreter::InitializeCompilerStatics(CEEInfo* info)
{
    if (!s_compilerStaticsInitialized)
    {
        // TODO: I believe I need no synchronization around this on x86, but I do
        // on more permissive memory models.  (Why it's OK on x86: each thread executes this
        // before any access to the initialized static variables; if several threads do
        // so, they perform idempotent initializing writes to the statics.
        GCX_PREEMP();
        s_TypedRefClsHnd = info->getBuiltinClass(CLASSID_TYPED_BYREF);
        s_TypedRefIT = InterpreterType(info, s_TypedRefClsHnd);
        s_TypedRefSize = getClassSize(s_TypedRefClsHnd);
        s_compilerStaticsInitialized = true;
        // TODO: Need store-store memory barrier here.
    }
}

void Interpreter::Terminate()
{
    if (s_initialized)
    {
        s_methodCacheLock.Destroy();
        s_interpStubToMDMapLock.Destroy();
        s_initialized = false;
    }
}

#if INTERP_ILINSTR_PROFILE
void Interpreter::SetILInstrCategories()
{
    // Start with the indentity maps
    for (unsigned short instr = 0; instr < 512; instr++) s_ILInstrCategories[instr] = instr;
    // Now make exceptions.
    for (unsigned instr = CEE_LDARG_0; instr <= CEE_LDARG_3; instr++) s_ILInstrCategories[instr] = CEE_LDARG;
    s_ILInstrCategories[CEE_LDARG_S] = CEE_LDARG;

    for (unsigned instr = CEE_LDLOC_0; instr <= CEE_LDLOC_3; instr++) s_ILInstrCategories[instr] = CEE_LDLOC;
    s_ILInstrCategories[CEE_LDLOC_S] = CEE_LDLOC;

    for (unsigned instr = CEE_STLOC_0; instr <= CEE_STLOC_3; instr++) s_ILInstrCategories[instr] = CEE_STLOC;
    s_ILInstrCategories[CEE_STLOC_S] = CEE_STLOC;

    s_ILInstrCategories[CEE_LDLOCA_S] = CEE_LDLOCA;

    for (unsigned instr = CEE_LDC_I4_M1; instr <= CEE_LDC_I4_S; instr++) s_ILInstrCategories[instr] = CEE_LDC_I4;

    for (unsigned instr = CEE_BR_S; instr <= CEE_BLT_UN; instr++) s_ILInstrCategories[instr] = CEE_BR;

    for (unsigned instr = CEE_LDIND_I1; instr <= CEE_LDIND_REF; instr++) s_ILInstrCategories[instr] = CEE_LDIND_I;

    for (unsigned instr = CEE_STIND_REF; instr <= CEE_STIND_R8; instr++) s_ILInstrCategories[instr] = CEE_STIND_I;

    for (unsigned instr = CEE_ADD; instr <= CEE_REM_UN; instr++) s_ILInstrCategories[instr] = CEE_ADD;

    for (unsigned instr = CEE_AND; instr <= CEE_NOT; instr++) s_ILInstrCategories[instr] = CEE_AND;

    for (unsigned instr = CEE_CONV_I1; instr <= CEE_CONV_U8; instr++) s_ILInstrCategories[instr] = CEE_CONV_I;
    for (unsigned instr = CEE_CONV_OVF_I1_UN; instr <= CEE_CONV_OVF_U_UN; instr++) s_ILInstrCategories[instr] = CEE_CONV_I;

    for (unsigned instr = CEE_LDELEM_I1; instr <= CEE_LDELEM_REF; instr++) s_ILInstrCategories[instr] = CEE_LDELEM;
    for (unsigned instr = CEE_STELEM_I; instr <= CEE_STELEM_REF; instr++) s_ILInstrCategories[instr] = CEE_STELEM;

    for (unsigned instr = CEE_CONV_OVF_I1; instr <= CEE_CONV_OVF_U8; instr++) s_ILInstrCategories[instr] = CEE_CONV_I;
    for (unsigned instr = CEE_CONV_U2; instr <= CEE_CONV_U1; instr++) s_ILInstrCategories[instr] = CEE_CONV_I;
    for (unsigned instr = CEE_CONV_OVF_I; instr <= CEE_CONV_OVF_U; instr++) s_ILInstrCategories[instr] = CEE_CONV_I;

    for (unsigned instr = CEE_ADD_OVF; instr <= CEE_SUB_OVF; instr++) s_ILInstrCategories[instr] = CEE_ADD_OVF;

    s_ILInstrCategories[CEE_LEAVE_S] = CEE_LEAVE;
    s_ILInstrCategories[CEE_CONV_U] = CEE_CONV_I;
}
#endif // INTERP_ILINSTR_PROFILE


template<int op>
void Interpreter::CompareOp()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned op1idx = m_curStackHt - 2;
    INT32 res = CompareOpRes<op>(op1idx);
    OpStackSet<INT32>(op1idx, res);
    OpStackTypeSet(op1idx, InterpreterType(CORINFO_TYPE_INT));
    m_curStackHt--;
}

template<int op>
INT32 Interpreter::CompareOpRes(unsigned op1idx)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= op1idx + 2);
    unsigned op2idx = op1idx + 1;
    InterpreterType t1 = OpStackTypeGet(op1idx);
    CorInfoType cit1 = t1.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit1));
    InterpreterType t2 = OpStackTypeGet(op2idx);
    CorInfoType cit2 = t2.ToCorInfoType();
    _ASSERTE(IsStackNormalType(cit2));
    INT32 res = 0;

    switch (cit1)
    {
    case CORINFO_TYPE_INT:
        if (cit2 == CORINFO_TYPE_INT)
        {
            INT32 val1 = OpStackGet<INT32>(op1idx);
            INT32 val2 = OpStackGet<INT32>(op2idx);
            if (op == CO_EQ)
            {
                if (val1 == val2) res = 1;
            }
            else if (op == CO_GT)
            {
                if (val1 > val2) res  = 1;
            }
            else if (op == CO_GT_UN)
            {
                if (static_cast<UINT32>(val1) > static_cast<UINT32>(val2)) res = 1;
            }
            else if (op == CO_LT)
            {
                if (val1 < val2) res  = 1;
            }
            else
            {
                _ASSERTE(op == CO_LT_UN);
                if (static_cast<UINT32>(val1) < static_cast<UINT32>(val2)) res = 1;
            }
        }
        else if (cit2 == CORINFO_TYPE_NATIVEINT ||
                 (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_BYREF) ||
                 (cit2 == CORINFO_TYPE_VALUECLASS
                  && CorInfoTypeStackNormalize(GetTypeForPrimitiveValueClass(t2.ToClassHandle())) == CORINFO_TYPE_NATIVEINT))
        {
            NativeInt val1 = OpStackGet<NativeInt>(op1idx);
            NativeInt val2 = OpStackGet<NativeInt>(op2idx);
            if (op == CO_EQ)
            {
                if (val1 == val2) res = 1;
            }
            else if (op == CO_GT)
            {
                if (val1 > val2) res  = 1;
            }
            else if (op == CO_GT_UN)
            {
                if (static_cast<NativeUInt>(val1) > static_cast<NativeUInt>(val2)) res = 1;
            }
            else if (op == CO_LT)
            {
                if (val1 < val2) res  = 1;
            }
            else
            {
                _ASSERTE(op == CO_LT_UN);
                if (static_cast<NativeUInt>(val1) < static_cast<NativeUInt>(val2)) res = 1;
            }
        }
        else if (cit2 == CORINFO_TYPE_VALUECLASS)
        {
            cit2 = GetTypeForPrimitiveValueClass(t2.ToClassHandle());
            INT32 val1 = OpStackGet<INT32>(op1idx);
            INT32 val2 = 0;
            if (CorInfoTypeStackNormalize(cit2) == CORINFO_TYPE_INT)
            {

                size_t sz = t2.Size(&m_interpCeeInfo);
                switch (sz)
                {
                case 1:
                    if (CorInfoTypeIsUnsigned(cit2))
                    {
                        val2 = OpStackGet<UINT8>(op2idx);
                    }
                    else
                    {
                        val2 = OpStackGet<INT8>(op2idx);
                    }
                    break;
                case 2:
                    if (CorInfoTypeIsUnsigned(cit2))
                    {
                        val2 = OpStackGet<UINT16>(op2idx);
                    }
                    else
                    {
                        val2 = OpStackGet<INT16>(op2idx);
                    }
                    break;
                case 4:
                    val2 = OpStackGet<INT32>(op2idx);
                    break;
                default:
                    UNREACHABLE();
                }
            }
            else
            {
                VerificationError("Can't compare with struct type.");
            }
            if (op == CO_EQ)
            {
                if (val1 == val2) res = 1;
            }
            else if (op == CO_GT)
            {
                if (val1 > val2) res  = 1;
            }
            else if (op == CO_GT_UN)
            {
                if (static_cast<UINT32>(val1) > static_cast<UINT32>(val2)) res = 1;
            }
            else if (op == CO_LT)
            {
                if (val1 < val2) res  = 1;
            }
            else
            {
                _ASSERTE(op == CO_LT_UN);
                if (static_cast<UINT32>(val1) < static_cast<UINT32>(val2)) res = 1;
            }
        }
        else
        {
            VerificationError("Binary comparision operation: type mismatch.");
        }
        break;
    case CORINFO_TYPE_NATIVEINT:
        if (cit2 == CORINFO_TYPE_NATIVEINT || cit2 == CORINFO_TYPE_INT
            || (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_LONG)
            || (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_BYREF)
            || (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_CLASS && OpStackGet<void*>(op2idx) == 0))
        {
            NativeInt val1 = OpStackGet<NativeInt>(op1idx);
            NativeInt val2;
            if (cit2 == CORINFO_TYPE_NATIVEINT)
            {
                val2 = OpStackGet<NativeInt>(op2idx);
            }
            else if (cit2 == CORINFO_TYPE_INT)
            {
                val2 = static_cast<NativeInt>(OpStackGet<INT32>(op2idx));
            }
            else if (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_LONG)
            {
                val2 = static_cast<NativeInt>(OpStackGet<INT64>(op2idx));
            }
            else if (cit2 == CORINFO_TYPE_CLASS)
            {
                _ASSERTE(OpStackGet<void*>(op2idx) == 0);
                val2 = 0;
            }
            else
            {
                _ASSERTE(s_InterpreterLooseRules && cit2 == CORINFO_TYPE_BYREF);
                val2 = reinterpret_cast<NativeInt>(OpStackGet<void*>(op2idx));
            }
            if (op == CO_EQ)
            {
                if (val1 == val2) res = 1;
            }
            else if (op == CO_GT)
            {
                if (val1 > val2) res  = 1;
            }
            else if (op == CO_GT_UN)
            {
                if (static_cast<NativeUInt>(val1) > static_cast<NativeUInt>(val2)) res = 1;
            }
            else if (op == CO_LT)
            {
                if (val1 < val2) res  = 1;
            }
            else
            {
                _ASSERTE(op == CO_LT_UN);
                if (static_cast<NativeUInt>(val1) < static_cast<NativeUInt>(val2)) res = 1;
            }
        }
        else
        {
            VerificationError("Binary comparision operation: type mismatch.");
        }
        break;
    case CORINFO_TYPE_LONG:
        {
            bool looseLong = false;
#if defined(HOST_AMD64)
            looseLong = s_InterpreterLooseRules && (cit2 == CORINFO_TYPE_NATIVEINT || cit2 == CORINFO_TYPE_BYREF);
#endif
            if (cit2 == CORINFO_TYPE_LONG || looseLong)
            {
                INT64 val1 = OpStackGet<INT64>(op1idx);
                INT64 val2 = OpStackGet<INT64>(op2idx);
                if (op == CO_EQ)
                {
                    if (val1 == val2) res = 1;
                }
                else if (op == CO_GT)
                {
                    if (val1 > val2) res  = 1;
                }
                else if (op == CO_GT_UN)
                {
                    if (static_cast<UINT64>(val1) > static_cast<UINT64>(val2)) res = 1;
                }
                else if (op == CO_LT)
                {
                    if (val1 < val2) res  = 1;
                }
                else
                {
                    _ASSERTE(op == CO_LT_UN);
                    if (static_cast<UINT64>(val1) < static_cast<UINT64>(val2)) res = 1;
                }
            }
            else
            {
                VerificationError("Binary comparision operation: type mismatch.");
            }
        }
        break;

    case CORINFO_TYPE_CLASS:
    case CORINFO_TYPE_STRING:
        if (cit2 == CORINFO_TYPE_CLASS || cit2 == CORINFO_TYPE_STRING)
        {
            GCX_FORBID();
            Object* val1 = OpStackGet<Object*>(op1idx);
            Object* val2 = OpStackGet<Object*>(op2idx);
            if (op == CO_EQ)
            {
                if (val1 == val2) res = 1;
            }
            else if (op == CO_GT_UN)
            {
                if (val1 != val2) res  = 1;
            }
            else
            {
                VerificationError("Binary comparision operation: type mismatch.");
            }
        }
        else
        {
            VerificationError("Binary comparision operation: type mismatch.");
        }
        break;


    case CORINFO_TYPE_FLOAT:
        {
            bool isDouble = (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_DOUBLE);
            if (cit2 == CORINFO_TYPE_FLOAT || isDouble)
            {
                float val1 = OpStackGet<float>(op1idx);
                float val2 = (isDouble) ? (float) OpStackGet<double>(op2idx) : OpStackGet<float>(op2idx);
                if (op == CO_EQ)
                {
                    // I'm assuming IEEE math here, so that if at least one is a NAN, the comparison will fail...
                    if (val1 == val2) res = 1;
                }
                else if (op == CO_GT)
                {
                    // I'm assuming that C++ arithmetic does the right thing here with infinities and NANs.
                    if (val1 > val2) res  = 1;
                }
                else if (op == CO_GT_UN)
                {
                    // Check for NAN's here: if either is a NAN, they're unordered, so this comparison returns true.
                    if (_isnan(val1) || _isnan(val2)) res = 1;
                    else if (val1 > val2) res = 1;
                }
                else if (op == CO_LT)
                {
                    if (val1 < val2) res  = 1;
                }
                else
                {
                    _ASSERTE(op == CO_LT_UN);
                    // Check for NAN's here: if either is a NAN, they're unordered, so this comparison returns true.
                    if (_isnan(val1) || _isnan(val2)) res = 1;
                    else if (val1 < val2) res = 1;
                }
            }
            else
            {
                VerificationError("Binary comparision operation: type mismatch.");
            }
        }
        break;

    case CORINFO_TYPE_DOUBLE:
        {
            bool isFloat = (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_FLOAT);
            if (cit2 == CORINFO_TYPE_DOUBLE || isFloat)
            {
                double val1 = OpStackGet<double>(op1idx);
                double val2 = (isFloat) ? (double) OpStackGet<float>(op2idx) : OpStackGet<double>(op2idx);
                if (op == CO_EQ)
                {
                    // I'm assuming IEEE math here, so that if at least one is a NAN, the comparison will fail...
                    if (val1 == val2) res = 1;
                }
                else if (op == CO_GT)
                {
                    // I'm assuming that C++ arithmetic does the right thing here with infinities and NANs.
                    if (val1 > val2) res  = 1;
                }
                else if (op == CO_GT_UN)
                {
                    // Check for NAN's here: if either is a NAN, they're unordered, so this comparison returns true.
                    if (_isnan(val1) || _isnan(val2)) res = 1;
                    else if (val1 > val2) res = 1;
                }
                else if (op == CO_LT)
                {
                    if (val1 < val2) res  = 1;
                }
                else
                {
                    _ASSERTE(op == CO_LT_UN);
                    // Check for NAN's here: if either is a NAN, they're unordered, so this comparison returns true.
                    if (_isnan(val1) || _isnan(val2)) res = 1;
                    else if (val1 < val2) res = 1;
                }
            }
            else
            {
                VerificationError("Binary comparision operation: type mismatch.");
            }
        }
        break;

    case CORINFO_TYPE_BYREF:
        if (cit2 == CORINFO_TYPE_BYREF || (s_InterpreterLooseRules && cit2 == CORINFO_TYPE_NATIVEINT))
        {
            NativeInt val1 = reinterpret_cast<NativeInt>(OpStackGet<void*>(op1idx));
            NativeInt val2;
            if (cit2 == CORINFO_TYPE_BYREF)
            {
                val2 = reinterpret_cast<NativeInt>(OpStackGet<void*>(op2idx));
            }
            else
            {
                _ASSERTE(s_InterpreterLooseRules && cit2 == CORINFO_TYPE_NATIVEINT);
                val2 = OpStackGet<NativeInt>(op2idx);
            }
            if (op == CO_EQ)
            {
                if (val1 == val2) res = 1;
            }
            else if (op == CO_GT)
            {
                if (val1 > val2) res  = 1;
            }
            else if (op == CO_GT_UN)
            {
                if (static_cast<NativeUInt>(val1) > static_cast<NativeUInt>(val2)) res = 1;
            }
            else if (op == CO_LT)
            {
                if (val1 < val2) res  = 1;
            }
            else
            {
                _ASSERTE(op == CO_LT_UN);
                if (static_cast<NativeUInt>(val1) < static_cast<NativeUInt>(val2)) res = 1;
            }
        }
        else
        {
            VerificationError("Binary comparision operation: type mismatch.");
        }
        break;

    case CORINFO_TYPE_VALUECLASS:
        {
            CorInfoType newCit1 = GetTypeForPrimitiveValueClass(t1.ToClassHandle());
            if (newCit1 == CORINFO_TYPE_UNDEF)
            {
                VerificationError("Can't compare a value class.");
            }
            else
            {
                NYI_INTERP("Must eliminate 'punning' value classes from the ostack.");
            }
        }
        break;

    default:
        _ASSERTE(false); // Should not be here if the type is stack-normal.
    }

    return res;
}

template<bool val, int targetLen>
void Interpreter::BrOnValue()
{
    _ASSERTE(targetLen == 1 || targetLen == 4);
    _ASSERTE(m_curStackHt > 0);
    unsigned stackInd = m_curStackHt - 1;
    InterpreterType it = OpStackTypeGet(stackInd);

    // It shouldn't be a value class, unless it's a punning name for a primitive integral type.
    if (it.ToCorInfoType() == CORINFO_TYPE_VALUECLASS)
    {
        GCX_PREEMP();
        CorInfoType cit = m_interpCeeInfo.getTypeForPrimitiveValueClass(it.ToClassHandle());
        if (CorInfoTypeIsIntegral(cit))
        {
            it = InterpreterType(cit);
        }
        else
        {
            VerificationError("Can't branch on the value of a value type that is not a primitive type.");
        }
    }

#ifdef _DEBUG
    switch (it.ToCorInfoType())
    {
    case CORINFO_TYPE_FLOAT:
    case CORINFO_TYPE_DOUBLE:
        VerificationError("Can't branch on the value of a float or double.");
        break;
    default:
        break;
    }
#endif // _DEBUG

    switch (it.SizeNotStruct())
    {
    case 4:
        {
            INT32 branchVal = OpStackGet<INT32>(stackInd);
            BrOnValueTakeBranch((branchVal != 0) == val, targetLen);
        }
        break;
    case 8:
        {
            INT64 branchVal = OpStackGet<INT64>(stackInd);
            BrOnValueTakeBranch((branchVal != 0) == val, targetLen);
        }
        break;

        // The value-class case handled above makes sizes 1 and 2 possible.
    case 1:
        {
            INT8 branchVal = OpStackGet<INT8>(stackInd);
            BrOnValueTakeBranch((branchVal != 0) == val, targetLen);
        }
        break;
    case 2:
        {
            INT16 branchVal = OpStackGet<INT16>(stackInd);
            BrOnValueTakeBranch((branchVal != 0) == val, targetLen);
        }
        break;
    default:
        UNREACHABLE();
        break;
    }
    m_curStackHt = stackInd;
}

// compOp is a member of the BranchComparisonOp enumeration.
template<int compOp, bool reverse, int targetLen>
void Interpreter::BrOnComparison()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(targetLen == 1 || targetLen == 4);
    _ASSERTE(m_curStackHt >= 2);
    unsigned v1Ind = m_curStackHt - 2;

    INT32 res = CompareOpRes<compOp>(v1Ind);
    if (reverse)
    {
        res = (res == 0) ? 1 : 0;
    }

    if  (res)
    {
        int offset;
        if (targetLen == 1)
        {
            // BYTE is unsigned...
            offset = getI1(m_ILCodePtr + 1);
        }
        else
        {
            offset = getI4LittleEndian(m_ILCodePtr + 1);
        }
        // 1 is the size of the current instruction; offset is relative to start of next.
        if (offset < 0)
        {
            // Backwards branch; enable caching.
            BackwardsBranchActions(offset);
        }
        ExecuteBranch(m_ILCodePtr + 1 + targetLen + offset);
    }
    else
    {
        m_ILCodePtr += targetLen + 1;
    }
    m_curStackHt -= 2;
}

void Interpreter::LdFld(FieldDesc* fldIn)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    BarrierIfVolatile();

    FieldDesc* fld = fldIn;
    CORINFO_CLASS_HANDLE valClsHnd = NULL;
    DWORD fldOffset;
    {
        GCX_PREEMP();
        unsigned ilOffset = CurOffset();
        if (fld == NULL && s_InterpreterUseCaching)
        {
#if INTERP_TRACING
            InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_LdFld]);
#endif // INTERP_TRACING
            fld = GetCachedInstanceField(ilOffset);
        }
        if (fld == NULL)
        {
            unsigned tok = getU4LittleEndian(m_ILCodePtr + sizeof(BYTE));
            fld = FindField(tok  InterpTracingArg(RTK_LdFld));
            _ASSERTE(fld != NULL);

            fldOffset = fld->GetOffset();
            if (s_InterpreterUseCaching && fldOffset < FIELD_OFFSET_LAST_REAL_OFFSET)
                CacheInstanceField(ilOffset, fld);
        }
        else
        {
            fldOffset = fld->GetOffset();
        }
    }
    CorInfoType valCit = CEEInfo::asCorInfoType(fld->GetFieldType());

    // If "fldIn" is non-NULL, it's not a "real" LdFld -- the caller should handle updating the instruction pointer.
    if (fldIn == NULL)
        m_ILCodePtr += 5;  // Last use above, so update now.

    // We need to construct the interpreter type for a struct type before we try to do coordinated
    // pushes of the value and type on the opstacks -- these must be atomic wrt GC, and constructing
    // a struct InterpreterType transitions to preemptive mode.
    InterpreterType structValIT;
    if (valCit == CORINFO_TYPE_VALUECLASS)
    {
        GCX_PREEMP();
        valCit = m_interpCeeInfo.getFieldType(CORINFO_FIELD_HANDLE(fld), &valClsHnd, nullptr);
        structValIT = InterpreterType(&m_interpCeeInfo, valClsHnd);
    }

    UINT sz = fld->GetSize();

    // Live vars: valCit, structValIt
    _ASSERTE(m_curStackHt > 0);
    unsigned stackInd = m_curStackHt - 1;
    InterpreterType addrIt = OpStackTypeGet(stackInd);
    CorInfoType addrCit = addrIt.ToCorInfoType();
    bool isUnsigned;

    if (addrCit == CORINFO_TYPE_CLASS)
    {
        OBJECTREF obj = OBJECTREF(OpStackGet<Object*>(stackInd));
        ThrowOnInvalidPointer(OBJECTREFToObject(obj));
        if (valCit == CORINFO_TYPE_VALUECLASS)
        {
            void* srcPtr = fld->GetInstanceAddress(obj);

            // srcPtr is now vulnerable.
            GCX_FORBID();

            MethodTable* valClsMT = GetMethodTableFromClsHnd(valClsHnd);
            if (sz > sizeof(INT64))
            {
                // Large struct case: allocate space on the large struct operand stack.
                void* destPtr = LargeStructOperandStackPush(sz);
                OpStackSet<void*>(stackInd, destPtr);
                CopyValueClass(destPtr, srcPtr, valClsMT);
            }
            else
            {
                // Small struct case -- is inline in operand stack.
                OpStackSet<INT64>(stackInd, GetSmallStructValue(srcPtr, sz));
            }
        }
        else
        {
            BYTE* fldStart = dac_cast<PTR_BYTE>(OBJECTREFToObject(obj)) + sizeof(Object) + fldOffset;
            // fldStart is now a vulnerable byref
            GCX_FORBID();

            switch (sz)
            {
            case 1:
                isUnsigned = CorInfoTypeIsUnsigned(valCit);
                if (isUnsigned)
                {
                    OpStackSet<UINT32>(stackInd, *reinterpret_cast<UINT8*>(fldStart));
                }
                else
                {
                    OpStackSet<INT32>(stackInd, *reinterpret_cast<INT8*>(fldStart));
                }
                break;
            case 2:
                isUnsigned = CorInfoTypeIsUnsigned(valCit);
                if (isUnsigned)
                {
                    OpStackSet<UINT32>(stackInd, *reinterpret_cast<UINT16*>(fldStart));
                }
                else
                {
                    OpStackSet<INT32>(stackInd, *reinterpret_cast<INT16*>(fldStart));
                }
                break;
            case 4:
                OpStackSet<INT32>(stackInd, *reinterpret_cast<INT32*>(fldStart));
                break;
            case 8:
                OpStackSet<INT64>(stackInd, *reinterpret_cast<INT64*>(fldStart));
                break;
            default:
                _ASSERTE_MSG(false, "Should not reach here.");
                break;
            }
        }
    }
    else
    {
        INT8* ptr = NULL;
        if (addrCit == CORINFO_TYPE_VALUECLASS)
        {
            size_t addrSize = addrIt.Size(&m_interpCeeInfo);
            // The ECMA spec allows ldfld to be applied to "an instance of a value type."
            // We will take the address of the ostack entry.
            if (addrIt.IsLargeStruct(&m_interpCeeInfo))
            {
                ptr = reinterpret_cast<INT8*>(OpStackGet<void*>(stackInd));
                // This is delicate.  I'm going to pop the large struct off the large-struct stack
                // now, even though the field value we push may go back on the large object stack.
                // We rely on the fact that this instruction doesn't do any other pushing, and
                // we assume that LargeStructOperandStackPop does not actually deallocate any memory,
                // and we rely on memcpy properly handling possibly-overlapping regions being copied.
                // Finally (wow, this really *is* delicate), we rely on the property that the large-struct
                // stack pop operation doesn't deallocate memory (the size of the allocated memory for the
                // large-struct stack only grows in a method execution), and that if we push the field value
                // on the large struct stack below, the size of the pushed item is at most the size of the
                // popped item, so the stack won't grow (which would allow a dealloc/realloc).
                // (All in all, maybe it would be better to just copy the value elsewhere then pop...but
                // that wouldn't be very aggressive.)
                LargeStructOperandStackPop(addrSize, ptr);
            }
            else
            {
                ptr = reinterpret_cast<INT8*>(OpStackGetAddr(stackInd, addrSize));
            }
        }
        else
        {
            _ASSERTE(CorInfoTypeIsPointer(addrCit));
            ptr = OpStackGet<INT8*>(stackInd);
            ThrowOnInvalidPointer(ptr);
        }

        _ASSERTE(ptr != NULL);
        ptr += fldOffset;

        if (valCit == CORINFO_TYPE_VALUECLASS)
        {
            if (sz > sizeof(INT64))
            {
                // Large struct case.
                void* dstPtr = LargeStructOperandStackPush(sz);
                memcpy(dstPtr, ptr, sz);
                OpStackSet<void*>(stackInd, dstPtr);
            }
            else
            {
                // Small struct case -- is inline in operand stack.
                OpStackSet<INT64>(stackInd, GetSmallStructValue(ptr, sz));
            }
            OpStackTypeSet(stackInd, structValIT.StackNormalize());
            return;
        }
        // Otherwise...
        switch (sz)
        {
        case 1:
            isUnsigned = CorInfoTypeIsUnsigned(valCit);
            if (isUnsigned)
            {
                OpStackSet<UINT32>(stackInd, *reinterpret_cast<UINT8*>(ptr));
            }
            else
            {
                OpStackSet<INT32>(stackInd, *reinterpret_cast<INT8*>(ptr));
            }
            break;
        case 2:
            isUnsigned = CorInfoTypeIsUnsigned(valCit);
            if (isUnsigned)
            {
                OpStackSet<UINT32>(stackInd, *reinterpret_cast<UINT16*>(ptr));
            }
            else
            {
                OpStackSet<INT32>(stackInd, *reinterpret_cast<INT16*>(ptr));
            }
            break;
        case 4:
            OpStackSet<INT32>(stackInd, *reinterpret_cast<INT32*>(ptr));
            break;
        case 8:
            OpStackSet<INT64>(stackInd, *reinterpret_cast<INT64*>(ptr));
            break;
        }
    }
    if (valCit == CORINFO_TYPE_VALUECLASS)
    {
        OpStackTypeSet(stackInd, structValIT.StackNormalize());
    }
    else
    {
        OpStackTypeSet(stackInd, InterpreterType(valCit).StackNormalize());
    }
}

void Interpreter::LdFldA()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    unsigned tok = getU4LittleEndian(m_ILCodePtr + sizeof(BYTE));

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_LdFldA]);
#endif // INTERP_TRACING

    unsigned offset = CurOffset();
    m_ILCodePtr += 5;  // Last use above, so update now.

    FieldDesc* fld = NULL;
    if (s_InterpreterUseCaching) fld = GetCachedInstanceField(offset);
    if (fld == NULL)
    {
        GCX_PREEMP();
        fld = FindField(tok  InterpTracingArg(RTK_LdFldA));
        if (s_InterpreterUseCaching) CacheInstanceField(offset, fld);
    }
    _ASSERTE(m_curStackHt > 0);
    unsigned stackInd = m_curStackHt - 1;
    CorInfoType addrCit = OpStackTypeGet(stackInd).ToCorInfoType();
    if (addrCit == CORINFO_TYPE_BYREF || addrCit == CORINFO_TYPE_CLASS || addrCit == CORINFO_TYPE_NATIVEINT)
    {
        NativeInt ptr = OpStackGet<NativeInt>(stackInd);
        ThrowOnInvalidPointer((void*)ptr);
        // The "offset" below does not include the Object (i.e., the MethodTable pointer) for object pointers, so add that in first.
        if (addrCit == CORINFO_TYPE_CLASS) ptr += sizeof(Object);
        // Now add the offset.
        ptr += fld->GetOffset();
        OpStackSet<NativeInt>(stackInd, ptr);
        if (addrCit == CORINFO_TYPE_NATIVEINT)
        {
            OpStackTypeSet(stackInd, InterpreterType(CORINFO_TYPE_NATIVEINT));
        }
        else
        {
            OpStackTypeSet(stackInd, InterpreterType(CORINFO_TYPE_BYREF));
        }
    }
    else
    {
        VerificationError("LdfldA requires object reference, managed or unmanaged pointer type.");
    }
}

void Interpreter::StFld()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_StFld]);
#endif // INTERP_TRACING

    FieldDesc* fld = NULL;
    DWORD fldOffset;
    {
        unsigned ilOffset = CurOffset();
        if (s_InterpreterUseCaching) fld = GetCachedInstanceField(ilOffset);
        if (fld == NULL)
        {
            unsigned tok = getU4LittleEndian(m_ILCodePtr + sizeof(BYTE));
            GCX_PREEMP();
            fld = FindField(tok  InterpTracingArg(RTK_StFld));
            _ASSERTE(fld != NULL);
            fldOffset = fld->GetOffset();
            if (s_InterpreterUseCaching && fldOffset < FIELD_OFFSET_LAST_REAL_OFFSET)
                CacheInstanceField(ilOffset, fld);
        }
        else
        {
            fldOffset = fld->GetOffset();
        }
    }
    m_ILCodePtr += 5;  // Last use above, so update now.

    UINT sz = fld->GetSize();
    _ASSERTE(m_curStackHt >= 2);
    unsigned addrInd = m_curStackHt - 2;
    CorInfoType addrCit = OpStackTypeGet(addrInd).ToCorInfoType();
    unsigned valInd = m_curStackHt - 1;
    CorInfoType valCit = OpStackTypeGet(valInd).ToCorInfoType();
    _ASSERTE(IsStackNormalType(addrCit) && IsStackNormalType(valCit));

    m_curStackHt -= 2;

    if (addrCit == CORINFO_TYPE_CLASS)
    {
        OBJECTREF obj = OBJECTREF(OpStackGet<Object*>(addrInd));
        ThrowOnInvalidPointer(OBJECTREFToObject(obj));

        if (valCit == CORINFO_TYPE_CLASS)
        {
            fld->SetRefValue(obj, ObjectToOBJECTREF(OpStackGet<Object*>(valInd)));
        }
        else if (valCit == CORINFO_TYPE_VALUECLASS)
        {
            MethodTable* valClsMT = GetMethodTableFromClsHnd(OpStackTypeGet(valInd).ToClassHandle());
            void* destPtr = fld->GetInstanceAddress(obj);

            // destPtr is now a vulnerable byref, so can't do GC.
            GCX_FORBID();

            // I use GCSafeMemCpy below to ensure that write barriers happen for the case in which
            // the value class contains GC pointers.  We could do better...
            if (sz > sizeof(INT64))
            {
                // Large struct case: stack slot contains pointer...
                void* srcPtr = OpStackGet<void*>(valInd);
                CopyValueClassUnchecked(destPtr, srcPtr, valClsMT);
                LargeStructOperandStackPop(sz, srcPtr);
            }
            else
            {
                // Small struct case -- is inline in operand stack.
                CopyValueClassUnchecked(destPtr, OpStackGetAddr(valInd, sz), valClsMT);
            }
            BarrierIfVolatile();
            return;
        }
        else
        {
            BYTE* fldStart = dac_cast<PTR_BYTE>(OBJECTREFToObject(obj)) + sizeof(Object) + fldOffset;
            // fldStart is now a vulnerable byref
            GCX_FORBID();

            switch (sz)
            {
            case 1:
                *reinterpret_cast<INT8*>(fldStart) = OpStackGet<INT8>(valInd);
                break;
            case 2:
                *reinterpret_cast<INT16*>(fldStart) = OpStackGet<INT16>(valInd);
                break;
            case 4:
                *reinterpret_cast<INT32*>(fldStart) = OpStackGet<INT32>(valInd);
                break;
            case 8:
                *reinterpret_cast<INT64*>(fldStart) = OpStackGet<INT64>(valInd);
                break;
            }
        }
    }
    else
    {
        _ASSERTE(addrCit == CORINFO_TYPE_BYREF || addrCit == CORINFO_TYPE_NATIVEINT);

        INT8* destPtr = OpStackGet<INT8*>(addrInd);
        ThrowOnInvalidPointer(destPtr);
        destPtr += fldOffset;

        if (valCit == CORINFO_TYPE_VALUECLASS)
        {
            MethodTable* valClsMT = GetMethodTableFromClsHnd(OpStackTypeGet(valInd).ToClassHandle());
            // I use GCSafeMemCpy below to ensure that write barriers happen for the case in which
            // the value class contains GC pointers.  We could do better...
            if (sz > sizeof(INT64))
            {
                // Large struct case: stack slot contains pointer...
                void* srcPtr = OpStackGet<void*>(valInd);
                CopyValueClassUnchecked(destPtr, srcPtr, valClsMT);
                LargeStructOperandStackPop(sz, srcPtr);
            }
            else
            {
                // Small struct case -- is inline in operand stack.
                CopyValueClassUnchecked(destPtr, OpStackGetAddr(valInd, sz), valClsMT);
            }
            BarrierIfVolatile();
            return;
        }
        else if (valCit == CORINFO_TYPE_CLASS)
        {
            OBJECTREF val = ObjectToOBJECTREF(OpStackGet<Object*>(valInd));
            SetObjectReference(reinterpret_cast<OBJECTREF*>(destPtr), val);
        }
        else
        {
            switch (sz)
            {
            case 1:
                *reinterpret_cast<INT8*>(destPtr) = OpStackGet<INT8>(valInd);
                break;
            case 2:
                *reinterpret_cast<INT16*>(destPtr) = OpStackGet<INT16>(valInd);
                break;
            case 4:
                *reinterpret_cast<INT32*>(destPtr) = OpStackGet<INT32>(valInd);
                break;
            case 8:
                *reinterpret_cast<INT64*>(destPtr) = OpStackGet<INT64>(valInd);
                break;
            }
        }
    }
    BarrierIfVolatile();
}

bool Interpreter::StaticFldAddrWork(CORINFO_ACCESS_FLAGS accessFlgs, /*out (byref)*/void** pStaticFieldAddr, /*out*/InterpreterType* pit, /*out*/UINT* pFldSize, /*out*/bool* pManagedMem)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    bool isCacheable = true;
    *pManagedMem = true;  // Default result.

    unsigned tok = getU4LittleEndian(m_ILCodePtr + sizeof(BYTE));
    m_ILCodePtr += 5;  // Above is last use of m_ILCodePtr in this method, so update now.

    FieldDesc* fld;
    CORINFO_FIELD_INFO fldInfo;
    CORINFO_RESOLVED_TOKEN fldTok;

    void* pFldAddr = NULL;
    {
        {
            GCX_PREEMP();

            ResolveToken(&fldTok, tok, CORINFO_TOKENKIND_Field InterpTracingArg(RTK_SFldAddr));
            fld = reinterpret_cast<FieldDesc*>(fldTok.hField);

            m_interpCeeInfo.getFieldInfo(&fldTok, m_methInfo->m_method, accessFlgs, &fldInfo);
        }

        EnsureClassInit(GetMethodTableFromClsHnd(fldTok.hClass));

        if (fldInfo.fieldAccessor == CORINFO_FIELD_STATIC_TLS)
        {
            NYI_INTERP("Thread-local static.");
        }
        else if (fldInfo.fieldAccessor == CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER
                 || fldInfo.fieldAccessor == CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER)
        {
            *pStaticFieldAddr = fld->GetCurrentStaticAddress();
            isCacheable = false;
        }
        else
        {
            *pStaticFieldAddr = fld->GetCurrentStaticAddress();
        }
    }
    if (fldInfo.structType != NULL && fldInfo.fieldType != CORINFO_TYPE_CLASS && fldInfo.fieldType != CORINFO_TYPE_PTR)
    {
        *pit = InterpreterType(&m_interpCeeInfo, fldInfo.structType);

        if ((fldInfo.fieldFlags & CORINFO_FLG_FIELD_UNMANAGED) == 0)
        {
            // For valuetypes in managed memory, the address returned contains a pointer into the heap, to a boxed version of the
            // static variable; return a pointer to the boxed struct.
            isCacheable = false;
        }
        else
        {
            *pManagedMem = false;
        }
    }
    else
    {
        *pit = InterpreterType(fldInfo.fieldType);
    }
    *pFldSize = fld->GetSize();

    return isCacheable;
}

void Interpreter::LdSFld()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    InterpreterType fldIt;
    UINT sz;
    bool managedMem;
    void* srcPtr = NULL;

    BarrierIfVolatile();

    GCPROTECT_BEGININTERIOR(srcPtr);

    StaticFldAddr(CORINFO_ACCESS_GET, &srcPtr, &fldIt, &sz, &managedMem);

    bool isUnsigned;

    if (fldIt.IsStruct())
    {
        // Large struct case.
        CORINFO_CLASS_HANDLE sh = fldIt.ToClassHandle();
        // This call is GC_TRIGGERS, so do it before we copy the value: no GC after this,
        // until the op stacks and ht are consistent.
        OpStackTypeSet(m_curStackHt, InterpreterType(&m_interpCeeInfo, sh).StackNormalize());
        if (fldIt.IsLargeStruct(&m_interpCeeInfo))
        {
            void* dstPtr = LargeStructOperandStackPush(sz);
            memcpy(dstPtr, srcPtr, sz);
            OpStackSet<void*>(m_curStackHt, dstPtr);
        }
        else
        {
            OpStackSet<INT64>(m_curStackHt, GetSmallStructValue(srcPtr, sz));
        }
    }
    else
    {
        CorInfoType valCit = fldIt.ToCorInfoType();
        switch (sz)
        {
        case 1:
            isUnsigned = CorInfoTypeIsUnsigned(valCit);
            if (isUnsigned)
            {
                OpStackSet<UINT32>(m_curStackHt, *reinterpret_cast<UINT8*>(srcPtr));
            }
            else
            {
                OpStackSet<INT32>(m_curStackHt, *reinterpret_cast<INT8*>(srcPtr));
            }
            break;
        case 2:
            isUnsigned = CorInfoTypeIsUnsigned(valCit);
            if (isUnsigned)
            {
                OpStackSet<UINT32>(m_curStackHt, *reinterpret_cast<UINT16*>(srcPtr));
            }
            else
            {
                OpStackSet<INT32>(m_curStackHt, *reinterpret_cast<INT16*>(srcPtr));
            }
            break;
        case 4:
            OpStackSet<INT32>(m_curStackHt, *reinterpret_cast<INT32*>(srcPtr));
            break;
        case 8:
            OpStackSet<INT64>(m_curStackHt, *reinterpret_cast<INT64*>(srcPtr));
            break;
        default:
            _ASSERTE_MSG(false, "LdSFld: this should have exhausted all the possible sizes.");
            break;
        }
        OpStackTypeSet(m_curStackHt, fldIt.StackNormalize());
    }
    m_curStackHt++;
    GCPROTECT_END();
}

void Interpreter::EnsureClassInit(MethodTable* pMT)
{
    if (!pMT->IsClassInited())
    {
        pMT->CheckRestore();
        // This is tantamount to a call, so exempt it from the cycle count.
#if INTERP_ILCYCLE_PROFILE
        unsigned __int64 startCycles;
        bool b = CycleTimer::GetThreadCyclesS(&startCycles); _ASSERTE(b);
#endif // INTERP_ILCYCLE_PROFILE

        pMT->CheckRunClassInitThrowing();

#if INTERP_ILCYCLE_PROFILE
        unsigned __int64 endCycles;
        b = CycleTimer::GetThreadCyclesS(&endCycles); _ASSERTE(b);
        m_exemptCycles += (endCycles - startCycles);
#endif // INTERP_ILCYCLE_PROFILE
    }
}

void Interpreter::LdSFldA()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    InterpreterType fldIt;
    UINT fldSz;
    bool managedMem;
    void* srcPtr = NULL;
    GCPROTECT_BEGININTERIOR(srcPtr);

    StaticFldAddr(CORINFO_ACCESS_ADDRESS, &srcPtr, &fldIt, &fldSz, &managedMem);

    OpStackSet<void*>(m_curStackHt, srcPtr);
    if (managedMem)
    {
        // Static variable in managed memory...
        OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_BYREF));
    }
    else
    {
        // RVA is in unmanaged memory.
        OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_NATIVEINT));
    }
    m_curStackHt++;

    GCPROTECT_END();
}

void Interpreter::StSFld()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
    InterpreterType fldIt;
    UINT sz;
    bool managedMem;
    void* dstPtr = NULL;
    GCPROTECT_BEGININTERIOR(dstPtr);

    StaticFldAddr(CORINFO_ACCESS_SET, &dstPtr, &fldIt, &sz, &managedMem);

    m_curStackHt--;
    InterpreterType valIt = OpStackTypeGet(m_curStackHt);
    CorInfoType valCit = valIt.ToCorInfoType();

    if (valCit == CORINFO_TYPE_VALUECLASS)
    {
        MethodTable* valClsMT = GetMethodTableFromClsHnd(valIt.ToClassHandle());
        if (sz > sizeof(INT64))
        {
            // Large struct case: value in operand stack is indirect pointer.
            void* srcPtr = OpStackGet<void*>(m_curStackHt);
            CopyValueClassUnchecked(dstPtr, srcPtr, valClsMT);
            LargeStructOperandStackPop(sz, srcPtr);
        }
        else
        {
            // Struct value is inline in the operand stack.
            CopyValueClassUnchecked(dstPtr, OpStackGetAddr(m_curStackHt, sz), valClsMT);
        }
    }
    else if (valCit == CORINFO_TYPE_CLASS)
    {
        SetObjectReference(reinterpret_cast<OBJECTREF*>(dstPtr), ObjectToOBJECTREF(OpStackGet<Object*>(m_curStackHt)));
    }
    else
    {
        switch (sz)
        {
        case 1:
            *reinterpret_cast<UINT8*>(dstPtr) = OpStackGet<UINT8>(m_curStackHt);
            break;
        case 2:
            *reinterpret_cast<UINT16*>(dstPtr) = OpStackGet<UINT16>(m_curStackHt);
            break;
        case 4:
            *reinterpret_cast<UINT32*>(dstPtr) = OpStackGet<UINT32>(m_curStackHt);
            break;
        case 8:
            *reinterpret_cast<UINT64*>(dstPtr) = OpStackGet<UINT64>(m_curStackHt);
            break;
        default:
            _ASSERTE_MSG(false, "This should have exhausted all the possible sizes.");
            break;
        }
    }
    GCPROTECT_END();

    BarrierIfVolatile();
}

template<typename T, bool IsObjType, CorInfoType cit>
void Interpreter::LdElemWithType()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned arrInd = m_curStackHt - 2;
    unsigned indexInd = m_curStackHt - 1;

    _ASSERTE(OpStackTypeGet(arrInd).ToCorInfoType() == CORINFO_TYPE_CLASS);

    ArrayBase* a = OpStackGet<ArrayBase*>(arrInd);
    ThrowOnInvalidPointer(a);
    int len = a->GetNumComponents();

    CorInfoType indexCit = OpStackTypeGet(indexInd).ToCorInfoType();
    if (indexCit == CORINFO_TYPE_INT)
    {
        int index = OpStackGet<INT32>(indexInd);
        if (index < 0 || index >= len) ThrowArrayBoundsException();

        GCX_FORBID();

        if (IsObjType)
        {
            OBJECTREF res = reinterpret_cast<PtrArray*>(a)->GetAt(index);
            OpStackSet<OBJECTREF>(arrInd, res);
        }
        else
        {
            intptr_t res_ptr = reinterpret_cast<intptr_t>(reinterpret_cast<Array<T>*>(a)->GetDirectConstPointerToNonObjectElements());
            if (cit == CORINFO_TYPE_INT)
            {
                _ASSERTE(std::is_integral<T>::value);

                // Widen narrow types.
                int ires;
                switch (sizeof(T))
                {
                case 1:
                    ires = std::is_same<T, INT8>::value ?
                           static_cast<int>(reinterpret_cast<INT8*>(res_ptr)[index]) :
                           static_cast<int>(reinterpret_cast<UINT8*>(res_ptr)[index]);
                    break;
                case 2:
                    ires = std::is_same<T, INT16>::value ?
                           static_cast<int>(reinterpret_cast<INT16*>(res_ptr)[index]) :
                           static_cast<int>(reinterpret_cast<UINT16*>(res_ptr)[index]);
                    break;
                case 4:
                    ires = std::is_same<T, INT32>::value ?
                           static_cast<int>(reinterpret_cast<INT32*>(res_ptr)[index]) :
                           static_cast<int>(reinterpret_cast<UINT32*>(res_ptr)[index]);
                    break;
                default:
                    _ASSERTE_MSG(false, "This should have exhausted all the possible sizes.");
                    break;
                }

                OpStackSet<int>(arrInd, ires);
            }
            else
            {
                OpStackSet<T>(arrInd, ((T*) res_ptr)[index]);
            }
        }
    }
    else
    {
        _ASSERTE(indexCit == CORINFO_TYPE_NATIVEINT);
        NativeInt index = OpStackGet<NativeInt>(indexInd);
        if (index < 0 || index >= NativeInt(len)) ThrowArrayBoundsException();

        GCX_FORBID();

        if (IsObjType)
        {
            OBJECTREF res = reinterpret_cast<PtrArray*>(a)->GetAt(index);
            OpStackSet<OBJECTREF>(arrInd, res);
        }
        else
        {
            T res = reinterpret_cast<Array<T>*>(a)->GetDirectConstPointerToNonObjectElements()[index];
            OpStackSet<T>(arrInd, res);
        }
    }

    OpStackTypeSet(arrInd, InterpreterType(cit));
    m_curStackHt--;
}

template<typename T, bool IsObjType>
void Interpreter::StElemWithType()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;


    _ASSERTE(m_curStackHt >= 3);
    unsigned arrInd = m_curStackHt - 3;
    unsigned indexInd = m_curStackHt - 2;
    unsigned valInd = m_curStackHt - 1;

    _ASSERTE(OpStackTypeGet(arrInd).ToCorInfoType() == CORINFO_TYPE_CLASS);

    ArrayBase* a = OpStackGet<ArrayBase*>(arrInd);
    ThrowOnInvalidPointer(a);
    int len = a->GetNumComponents();

    CorInfoType indexCit = OpStackTypeGet(indexInd).ToCorInfoType();
    if (indexCit == CORINFO_TYPE_INT)
    {
        int index = OpStackGet<INT32>(indexInd);
        if (index < 0 || index >= len) ThrowArrayBoundsException();
        if (IsObjType)
        {
            struct _gc {
                OBJECTREF val;
                OBJECTREF a;
            } gc;
            gc.val = ObjectToOBJECTREF(OpStackGet<Object*>(valInd));
            gc.a = ObjectToOBJECTREF(a);
            GCPROTECT_BEGIN(gc);
            if (gc.val != NULL &&
                !ObjIsInstanceOf(OBJECTREFToObject(gc.val), reinterpret_cast<PtrArray*>(a)->GetArrayElementTypeHandle()))
                COMPlusThrow(kArrayTypeMismatchException);
            reinterpret_cast<PtrArray*>(OBJECTREFToObject(gc.a))->SetAt(index, gc.val);
            GCPROTECT_END();
        }
        else
        {
            GCX_FORBID();
            T val = OpStackGet<T>(valInd);
            reinterpret_cast<Array<T>*>(a)->GetDirectPointerToNonObjectElements()[index] = val;
        }
    }
    else
    {
        _ASSERTE(indexCit == CORINFO_TYPE_NATIVEINT);
        NativeInt index = OpStackGet<NativeInt>(indexInd);
        if (index < 0 || index >= NativeInt(len)) ThrowArrayBoundsException();
        if (IsObjType)
        {
            struct _gc {
                OBJECTREF val;
                OBJECTREF a;
            } gc;
            gc.val = ObjectToOBJECTREF(OpStackGet<Object*>(valInd));
            gc.a = ObjectToOBJECTREF(a);
            GCPROTECT_BEGIN(gc);
            if (gc.val != NULL &&
                !ObjIsInstanceOf(OBJECTREFToObject(gc.val), reinterpret_cast<PtrArray*>(a)->GetArrayElementTypeHandle()))
                COMPlusThrow(kArrayTypeMismatchException);
            reinterpret_cast<PtrArray*>(OBJECTREFToObject(gc.a))->SetAt(index, gc.val);
            GCPROTECT_END();
        }
        else
        {
            GCX_FORBID();
            T val = OpStackGet<T>(valInd);
            reinterpret_cast<Array<T>*>(a)->GetDirectPointerToNonObjectElements()[index] = val;
        }
    }

    m_curStackHt -= 3;
}

template<bool takeAddress>
void Interpreter::LdElem()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned arrInd = m_curStackHt - 2;
    unsigned indexInd = m_curStackHt - 1;

    unsigned elemTypeTok = getU4LittleEndian(m_ILCodePtr + 1);

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_LdElem]);
#endif // INTERP_TRACING

    unsigned ilOffset = CurOffset();
    CORINFO_CLASS_HANDLE clsHnd = NULL;
    if (s_InterpreterUseCaching) clsHnd = GetCachedClassHandle(ilOffset);

    if (clsHnd == NULL)
    {

        CORINFO_RESOLVED_TOKEN elemTypeResolvedTok;
        {
            GCX_PREEMP();
            ResolveToken(&elemTypeResolvedTok, elemTypeTok, CORINFO_TOKENKIND_Class InterpTracingArg(RTK_LdElem));
            clsHnd = elemTypeResolvedTok.hClass;
        }
        if (s_InterpreterUseCaching) CacheClassHandle(ilOffset, clsHnd);
    }

    CorInfoType elemCit = ::asCorInfoType(clsHnd);

    m_ILCodePtr += 5;


    InterpreterType elemIt;
    if (elemCit == CORINFO_TYPE_VALUECLASS)
    {
        elemIt = InterpreterType(&m_interpCeeInfo, clsHnd);
    }
    else
    {
        elemIt = InterpreterType(elemCit);
    }

    _ASSERTE(OpStackTypeGet(arrInd).ToCorInfoType() == CORINFO_TYPE_CLASS);


    ArrayBase* a = OpStackGet<ArrayBase*>(arrInd);
    ThrowOnInvalidPointer(a);
    int len = a->GetNumComponents();

    NativeInt index;
    {
        GCX_FORBID();

        CorInfoType indexCit = OpStackTypeGet(indexInd).ToCorInfoType();
        if (indexCit == CORINFO_TYPE_INT)
        {
            index = static_cast<NativeInt>(OpStackGet<INT32>(indexInd));
        }
        else
        {
            _ASSERTE(indexCit == CORINFO_TYPE_NATIVEINT);
            index = OpStackGet<NativeInt>(indexInd);
        }
    }
    if (index < 0 || index >= len) ThrowArrayBoundsException();

    bool throwTypeMismatch = NULL;
    {
        void* elemPtr = a->GetDataPtr() + a->GetComponentSize() * index;
        // elemPtr is now a vulnerable byref.
        GCX_FORBID();

        if (takeAddress)
        {
            // If the element type is a class type, may have to do a type check.
            if (elemCit == CORINFO_TYPE_CLASS)
            {
                // Unless there was a readonly prefix, which removes the need to
                // do the (dynamic) type check.
                if (m_readonlyFlag)
                {
                    // Consume the readonly prefix, and don't do the type check below.
                    m_readonlyFlag = false;
                }
                else
                {
                    PtrArray* pa = reinterpret_cast<PtrArray*>(a);
                    // The element array type must be exactly the referent type of the managed
                    // pointer we'll be creating.
                    if (pa->GetArrayElementTypeHandle() != TypeHandle(clsHnd))
                    {
                        throwTypeMismatch = true;
                    }
                }
            }
            if (!throwTypeMismatch)
            {
                // If we're not going to throw the exception, we can take the address.
                OpStackSet<void*>(arrInd, elemPtr);
                OpStackTypeSet(arrInd, InterpreterType(CORINFO_TYPE_BYREF));
                m_curStackHt--;
            }
        }
        else
        {
            m_curStackHt -= 2;
            LdFromMemAddr(elemPtr, elemIt);
            return;
        }
    }

    // If we're going to throw, we do the throw outside the GCX_FORBID region above, since it requires GC_TRIGGERS.
    if (throwTypeMismatch)
    {
        COMPlusThrow(kArrayTypeMismatchException);
    }
}

void Interpreter::StElem()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 3);
    unsigned arrInd = m_curStackHt - 3;
    unsigned indexInd = m_curStackHt - 2;
    unsigned valInd = m_curStackHt - 1;

    CorInfoType valCit = OpStackTypeGet(valInd).ToCorInfoType();

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_StElem]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE typeFromTok = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_StElem));

    m_ILCodePtr += 5;

    CorInfoType typeFromTokCit;
    {
        GCX_PREEMP();
        typeFromTokCit = ::asCorInfoType(typeFromTok);
    }
    size_t sz;

#ifdef _DEBUG
    InterpreterType typeFromTokIt;
#endif // _DEBUG

    if (typeFromTokCit == CORINFO_TYPE_VALUECLASS)
    {
        GCX_PREEMP();
        sz = getClassSize(typeFromTok);
#ifdef _DEBUG
        typeFromTokIt = InterpreterType(&m_interpCeeInfo, typeFromTok);
#endif // _DEBUG
    }
    else
    {
        sz = CorInfoTypeSize(typeFromTokCit);
#ifdef _DEBUG
        typeFromTokIt = InterpreterType(typeFromTokCit);
#endif // _DEBUG
    }

#ifdef _DEBUG
    // Instead of debug, I need to parameterize the interpreter at the top level over whether
    // to do checks corresponding to verification.
    if (typeFromTokIt.StackNormalize().ToCorInfoType() != valCit)
    {
        // This is obviously only a partial test of the required condition.
        VerificationError("Value in stelem does not have the required type.");
    }
#endif // _DEBUG

    _ASSERTE(OpStackTypeGet(arrInd).ToCorInfoType() == CORINFO_TYPE_CLASS);

    ArrayBase* a = OpStackGet<ArrayBase*>(arrInd);
    ThrowOnInvalidPointer(a);
    int len = a->GetNumComponents();

    CorInfoType indexCit = OpStackTypeGet(indexInd).ToCorInfoType();
    NativeInt index = 0;
    if (indexCit == CORINFO_TYPE_INT)
    {
        index = static_cast<NativeInt>(OpStackGet<INT32>(indexInd));
    }
    else
    {
        index = OpStackGet<NativeInt>(indexInd);
    }

    if (index < 0 || index >= len) ThrowArrayBoundsException();

    if (typeFromTokCit == CORINFO_TYPE_CLASS)
    {
        struct _gc {
            OBJECTREF val;
            OBJECTREF a;
        } gc;
        gc.val = ObjectToOBJECTREF(OpStackGet<Object*>(valInd));
        gc.a = ObjectToOBJECTREF(a);
        GCPROTECT_BEGIN(gc);
        if (gc.val != NULL &&
            !ObjIsInstanceOf(OBJECTREFToObject(gc.val), reinterpret_cast<PtrArray*>(a)->GetArrayElementTypeHandle()))
            COMPlusThrow(kArrayTypeMismatchException);
        reinterpret_cast<PtrArray*>(OBJECTREFToObject(gc.a))->SetAt(index, gc.val);
        GCPROTECT_END();
    }
    else
    {
        GCX_FORBID();

        void* destPtr = a->GetDataPtr() + index * sz;;

        if (typeFromTokCit == CORINFO_TYPE_VALUECLASS)
        {
            MethodTable* valClsMT = GetMethodTableFromClsHnd(OpStackTypeGet(valInd).ToClassHandle());
            // I use GCSafeMemCpy below to ensure that write barriers happen for the case in which
            // the value class contains GC pointers.  We could do better...
            if (sz > sizeof(UINT64))
            {
                // Large struct case: stack slot contains pointer...
                void* src = OpStackGet<void*>(valInd);
                CopyValueClassUnchecked(destPtr, src, valClsMT);
                LargeStructOperandStackPop(sz, src);
            }
            else
            {
                // Small struct case -- is inline in operand stack.
                CopyValueClassUnchecked(destPtr, OpStackGetAddr(valInd, sz), valClsMT);
            }
        }
        else
        {
            switch (sz)
            {
            case 1:
                *reinterpret_cast<INT8*>(destPtr) = OpStackGet<INT8>(valInd);
                break;
            case 2:
                *reinterpret_cast<INT16*>(destPtr) = OpStackGet<INT16>(valInd);
                break;
            case 4:
                *reinterpret_cast<INT32*>(destPtr) = OpStackGet<INT32>(valInd);
                break;
            case 8:
                *reinterpret_cast<INT64*>(destPtr) = OpStackGet<INT64>(valInd);
                break;
            }
        }
    }

    m_curStackHt -= 3;
}

void Interpreter::InitBlk()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 3);
    unsigned addrInd = m_curStackHt - 3;
    unsigned valInd  = m_curStackHt - 2;
    unsigned sizeInd = m_curStackHt - 1;

#ifdef _DEBUG
    CorInfoType addrCIT = OpStackTypeGet(addrInd).ToCorInfoType();
    bool addrValidType = (addrCIT == CORINFO_TYPE_NATIVEINT || addrCIT == CORINFO_TYPE_BYREF);
#if defined(HOST_AMD64)
    if (s_InterpreterLooseRules && addrCIT == CORINFO_TYPE_LONG)
        addrValidType = true;
#endif
    if (!addrValidType)
        VerificationError("Addr of InitBlk must be native int or &.");

    CorInfoType valCIT = OpStackTypeGet(valInd).ToCorInfoType();
    if (valCIT != CORINFO_TYPE_INT)
        VerificationError("Value of InitBlk must be int");

#endif // _DEBUG

    CorInfoType sizeCIT = OpStackTypeGet(sizeInd).ToCorInfoType();
    bool isLong = s_InterpreterLooseRules && (sizeCIT == CORINFO_TYPE_LONG);

#ifdef _DEBUG
    if (sizeCIT != CORINFO_TYPE_INT && !isLong)
        VerificationError("Size of InitBlk must be int");
#endif // _DEBUG

    void* addr = OpStackGet<void*>(addrInd);
    ThrowOnInvalidPointer(addr);
    GCX_FORBID(); // addr is a potentially vulnerable byref.
    INT8 val = OpStackGet<INT8>(valInd);
    size_t size = (size_t) ((isLong) ? OpStackGet<UINT64>(sizeInd) : OpStackGet<UINT32>(sizeInd));
    memset(addr, val, size);

    m_curStackHt = addrInd;
    m_ILCodePtr += 2;

    BarrierIfVolatile();
}

void Interpreter::CpBlk()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 3);
    unsigned destInd = m_curStackHt - 3;
    unsigned srcInd  = m_curStackHt - 2;
    unsigned sizeInd = m_curStackHt - 1;

#ifdef _DEBUG
    CorInfoType destCIT = OpStackTypeGet(destInd).ToCorInfoType();
    bool destValidType = (destCIT == CORINFO_TYPE_NATIVEINT || destCIT == CORINFO_TYPE_BYREF);
#if defined(HOST_AMD64)
    if (s_InterpreterLooseRules && destCIT == CORINFO_TYPE_LONG)
        destValidType = true;
#endif
    if (!destValidType)
    {
        VerificationError("Dest addr of CpBlk must be native int or &.");
    }
    CorInfoType srcCIT = OpStackTypeGet(srcInd).ToCorInfoType();
    bool srcValidType = (srcCIT == CORINFO_TYPE_NATIVEINT || srcCIT == CORINFO_TYPE_BYREF);
#if defined(HOST_AMD64)
    if (s_InterpreterLooseRules && srcCIT == CORINFO_TYPE_LONG)
        srcValidType = true;
#endif
    if (!srcValidType)
        VerificationError("Src addr of CpBlk must be native int or &.");
#endif // _DEBUG

    CorInfoType sizeCIT = OpStackTypeGet(sizeInd).ToCorInfoType();
    bool isLong = s_InterpreterLooseRules && (sizeCIT == CORINFO_TYPE_LONG);

#ifdef _DEBUG
    if (sizeCIT != CORINFO_TYPE_INT && !isLong)
        VerificationError("Size of CpBlk must be int");
#endif // _DEBUG


    void* destAddr = OpStackGet<void*>(destInd);
    void* srcAddr = OpStackGet<void*>(srcInd);
    ThrowOnInvalidPointer(destAddr);
    ThrowOnInvalidPointer(srcAddr);
    GCX_FORBID(); // destAddr & srcAddr are potentially vulnerable byrefs.
    size_t size = (size_t)((isLong) ? OpStackGet<UINT64>(sizeInd) : OpStackGet<UINT32>(sizeInd));
    memcpyNoGCRefs(destAddr, srcAddr, size);

    m_curStackHt = destInd;
    m_ILCodePtr += 2;

    BarrierIfVolatile();
}

void Interpreter::Box()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned ind = m_curStackHt - 1;

    DWORD boxTypeAttribs = 0;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_Box]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE boxTypeClsHnd = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_Box));

    {
        GCX_PREEMP();
        boxTypeAttribs = m_interpCeeInfo.getClassAttribs(boxTypeClsHnd);
    }

    m_ILCodePtr += 5;

    if (boxTypeAttribs & CORINFO_FLG_VALUECLASS)
    {
        InterpreterType valIt = OpStackTypeGet(ind);

        void* valPtr;
        if (valIt.IsLargeStruct(&m_interpCeeInfo))
        {
            // Operand stack entry is pointer to the data.
            valPtr = OpStackGet<void*>(ind);
        }
        else
        {
            // Operand stack entry *is* the data.
            size_t classSize = getClassSize(boxTypeClsHnd);
            valPtr = OpStackGetAddr(ind, classSize);
        }

        TypeHandle th(boxTypeClsHnd);
        if (th.IsTypeDesc())
        {
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_TypeCannotBeBoxed"));
        }

        MethodTable* pMT = th.AsMethodTable();

        {
            Object* res = OBJECTREFToObject(pMT->Box(valPtr));

            GCX_FORBID();

            // If we're popping a large struct off the operand stack, make sure we clean up.
            if (valIt.IsLargeStruct(&m_interpCeeInfo))
            {
                LargeStructOperandStackPop(valIt.Size(&m_interpCeeInfo), valPtr);
            }
            OpStackSet<Object*>(ind, res);
            OpStackTypeSet(ind, InterpreterType(CORINFO_TYPE_CLASS));
        }
    }
}

void Interpreter::BoxStructRefAt(unsigned ind, CORINFO_CLASS_HANDLE valCls)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE_MSG(ind < m_curStackHt, "Precondition");
    {
        GCX_PREEMP();
        _ASSERTE_MSG(m_interpCeeInfo.getClassAttribs(valCls) & CORINFO_FLG_VALUECLASS, "Precondition");
    }
    _ASSERTE_MSG(OpStackTypeGet(ind).ToCorInfoType() == CORINFO_TYPE_BYREF, "Precondition");

    InterpreterType valIt = InterpreterType(&m_interpCeeInfo, valCls);

    void* valPtr = OpStackGet<void*>(ind);

    TypeHandle th(valCls);
    if (th.IsTypeDesc())
        COMPlusThrow(kInvalidOperationException,W("InvalidOperation_TypeCannotBeBoxed"));

    MethodTable* pMT = th.AsMethodTable();

    {
        Object* res = OBJECTREFToObject(pMT->Box(valPtr));

        GCX_FORBID();

        OpStackSet<Object*>(ind, res);
        OpStackTypeSet(ind, InterpreterType(CORINFO_TYPE_CLASS));
    }
}


void Interpreter::Unbox()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END

    _ASSERTE(m_curStackHt > 0);
    unsigned tos = m_curStackHt - 1;

#ifdef _DEBUG
    CorInfoType tosCIT = OpStackTypeGet(tos).ToCorInfoType();
    if (tosCIT != CORINFO_TYPE_CLASS)
        VerificationError("Unbox requires that TOS is an object pointer.");
#endif // _DEBUG

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_Unbox]);
#endif // INTERP_TRACING

    CORINFO_CLASS_HANDLE boxTypeClsHnd = GetTypeFromToken(m_ILCodePtr + 1, CORINFO_TOKENKIND_Class  InterpTracingArg(RTK_Unbox));

    CorInfoHelpFunc unboxHelper;

    {
        GCX_PREEMP();
        unboxHelper = m_interpCeeInfo.getUnBoxHelper(boxTypeClsHnd);
    }

    void* res = NULL;
    Object* obj = OpStackGet<Object*>(tos);

    switch (unboxHelper)
    {
    case CORINFO_HELP_UNBOX:
        {
            ThrowOnInvalidPointer(obj);

            MethodTable* pMT1 = (MethodTable*)boxTypeClsHnd;
            MethodTable* pMT2 = obj->GetMethodTable();

            if (pMT1->IsEquivalentTo(pMT2))
            {
                res = OpStackGet<Object*>(tos)->UnBox();
            }
            else
            {
                CorElementType type1 = pMT1->GetInternalCorElementType();
                CorElementType type2 = pMT2->GetInternalCorElementType();

                // we allow enums and their primitive type to be interchangable
                if (type1 == type2)
                {
                    if ((pMT1->IsEnum() || pMT1->IsTruePrimitive()) &&
                        (pMT2->IsEnum() || pMT2->IsTruePrimitive()))
                    {
                        res = OpStackGet<Object*>(tos)->UnBox();
                    }
                }
            }

            if (res == NULL)
            {
                COMPlusThrow(kInvalidCastException);
            }
        }
        break;

    case CORINFO_HELP_UNBOX_NULLABLE:
        {
            // For "unbox Nullable<T>", we need to create a new object (maybe in some temporary local
            // space (that we reuse every time we hit this IL instruction?), that gets reported to the GC,
            // maybe in the GC heap itself). That object will contain an embedded Nullable<T>. Then, we need to
            // get a byref to the data within the object.

            NYI_INTERP("Unhandled 'unbox' of Nullable<T>.");
        }
        break;

    default:
        NYI_INTERP("Unhandled 'unbox' helper.");
    }

    {
        GCX_FORBID();
        OpStackSet<void*>(tos, res);
        OpStackTypeSet(tos, InterpreterType(CORINFO_TYPE_BYREF));
    }

    m_ILCodePtr += 5;
}


void Interpreter::Throw()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END

    _ASSERTE(m_curStackHt >= 1);

    // Note that we can't decrement the stack height here, since the operand stack
    // protects the thrown object.  Nor do we need to, since the ostack will be cleared on
    // any catch within this method.
    unsigned exInd = m_curStackHt - 1;

#ifdef _DEBUG
    CorInfoType exCIT = OpStackTypeGet(exInd).ToCorInfoType();
    if (exCIT != CORINFO_TYPE_CLASS)
    {
        VerificationError("Can only throw an object.");
    }
#endif // _DEBUG

    Object* obj = OpStackGet<Object*>(exInd);
    ThrowOnInvalidPointer(obj);

    OBJECTREF oref = ObjectToOBJECTREF(obj);
    if (!IsException(oref->GetMethodTable()))
    {
        GCPROTECT_BEGIN(oref);
        WrapNonCompliantException(&oref);
        GCPROTECT_END();
    }
    COMPlusThrow(oref);
}

void Interpreter::Rethrow()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END

    OBJECTREF throwable = GetThread()->LastThrownObject();
    COMPlusThrow(throwable);
}

void Interpreter::UnboxAny()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned tos = m_curStackHt - 1;

    unsigned boxTypeTok = getU4LittleEndian(m_ILCodePtr + 1);
    m_ILCodePtr += 5;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_UnboxAny]);
#endif // INTERP_TRACING

    CORINFO_RESOLVED_TOKEN boxTypeResolvedTok;
    CORINFO_CLASS_HANDLE boxTypeClsHnd;
    DWORD boxTypeAttribs = 0;

    {
        GCX_PREEMP();
        ResolveToken(&boxTypeResolvedTok, boxTypeTok, CORINFO_TOKENKIND_Class InterpTracingArg(RTK_UnboxAny));
        boxTypeClsHnd = boxTypeResolvedTok.hClass;
        boxTypeAttribs = m_interpCeeInfo.getClassAttribs(boxTypeClsHnd);
    }

    CorInfoType unboxCIT = OpStackTypeGet(tos).ToCorInfoType();
    if (unboxCIT != CORINFO_TYPE_CLASS)
        VerificationError("Type mismatch in UNBOXANY.");

    if ((boxTypeAttribs & CORINFO_FLG_VALUECLASS) == 0)
    {
        Object* obj = OpStackGet<Object*>(tos);
        if (obj != NULL && !ObjIsInstanceOf(obj, TypeHandle(boxTypeClsHnd), TRUE))
        {
            UNREACHABLE(); //ObjIsInstanceOf will throw if cast can't be done
        }
    }
    else
    {
        CorInfoHelpFunc unboxHelper;

        {
            GCX_PREEMP();
            unboxHelper = m_interpCeeInfo.getUnBoxHelper(boxTypeClsHnd);
        }

        // Important that this *not* be factored out with the identical statement in the "if" branch:
        // delay read from GC-protected operand stack until after COOP-->PREEMP transition above.
        Object* obj = OpStackGet<Object*>(tos);

        switch (unboxHelper)
        {
        case CORINFO_HELP_UNBOX:
            {
                ThrowOnInvalidPointer(obj);

                MethodTable* pMT1 = (MethodTable*)boxTypeClsHnd;
                MethodTable* pMT2 = obj->GetMethodTable();

                void* res = NULL;
                if (pMT1->IsEquivalentTo(pMT2))
                {
                    res = OpStackGet<Object*>(tos)->UnBox();
                }
                else
                {
                    if (pMT1->GetInternalCorElementType() == pMT2->GetInternalCorElementType() &&
                            (pMT1->IsEnum() || pMT1->IsTruePrimitive()) &&
                            (pMT2->IsEnum() || pMT2->IsTruePrimitive()))
                    {
                        res = OpStackGet<Object*>(tos)->UnBox();
                    }
                }

                if (res == NULL)
                {
                    COMPlusThrow(kInvalidCastException);
                }

                // As the ECMA spec says, the rest is like a "ldobj".
                LdObjValueClassWork(boxTypeClsHnd, tos, res);
            }
            break;

        case CORINFO_HELP_UNBOX_NULLABLE:
            {
                InterpreterType it = InterpreterType(&m_interpCeeInfo, boxTypeClsHnd);
                size_t sz = it.Size(&m_interpCeeInfo);
                if (sz > sizeof(INT64))
                {
                    void* destPtr = LargeStructOperandStackPush(sz);
                    if (!Nullable::UnBox(destPtr, ObjectToOBJECTREF(obj), (MethodTable*)boxTypeClsHnd))
                    {
                        COMPlusThrow(kInvalidCastException);
                    }
                    OpStackSet<void*>(tos, destPtr);
                }
                else
                {
                    INT64 dest = 0;
                    if (!Nullable::UnBox(&dest, ObjectToOBJECTREF(obj), (MethodTable*)boxTypeClsHnd))
                    {
                        COMPlusThrow(kInvalidCastException);
                    }
                    OpStackSet<INT64>(tos, dest);
                }
                OpStackTypeSet(tos, it.StackNormalize());
            }
            break;

        default:
            NYI_INTERP("Unhandled 'unbox.any' helper.");
        }
    }
}

void Interpreter::LdLen()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 1);
    unsigned arrInd = m_curStackHt - 1;

    _ASSERTE(OpStackTypeGet(arrInd).ToCorInfoType() == CORINFO_TYPE_CLASS);

    GCX_FORBID();

    ArrayBase* a = OpStackGet<ArrayBase*>(arrInd);
    ThrowOnInvalidPointer(a);
    int len = a->GetNumComponents();

    OpStackSet<NativeUInt>(arrInd, NativeUInt(len));
    // The ECMA spec says that the type of the length value is NATIVEUINT, but this
    // doesn't make any sense -- unsigned types are not stack-normalized.  So I'm
    // using NATIVEINT, to get the width right.
    OpStackTypeSet(arrInd, InterpreterType(CORINFO_TYPE_NATIVEINT));
}


void Interpreter::DoCall(bool virtualCall)
{
#if INTERP_DYNAMIC_CONTRACTS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
#else
    // Dynamic contract occupies too much stack.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
#endif

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_Call]);
#endif // INTERP_TRACING

    DoCallWork(virtualCall);

    m_ILCodePtr += 5;
}

CORINFO_CONTEXT_HANDLE InterpreterMethodInfo::GetPreciseGenericsContext(Object* thisArg, void* genericsCtxtArg)
{
    // If the caller has a generic argument, then we need to get the exact methodContext.
    // There are several possibilities that lead to a generic argument:
    //     1) Static method of generic class: generic argument is the method table of the class.
    //     2) generic method of a class: generic argument is the precise MethodDesc* of the method.
    if (GetFlag<InterpreterMethodInfo::Flag_hasGenericsContextArg>())
    {
        _ASSERTE(GetFlag<InterpreterMethodInfo::Flag_methHasGenericArgs>() || GetFlag<InterpreterMethodInfo::Flag_typeHasGenericArgs>());
        if (GetFlag<InterpreterMethodInfo::Flag_methHasGenericArgs>())
        {
            return MAKE_METHODCONTEXT(reinterpret_cast<CORINFO_METHOD_HANDLE>(genericsCtxtArg));
        }
        else
        {
            MethodTable* methodClass = reinterpret_cast<MethodDesc*>(m_method)->GetMethodTable();
            MethodTable* contextClass = reinterpret_cast<MethodTable*>(genericsCtxtArg)->GetMethodTableMatchingParentClass(methodClass);
            return MAKE_CLASSCONTEXT(contextClass);
        }
    }
    // TODO: This condition isn't quite right.  If the actual class is a subtype of the declaring type of the method,
    // then it might be in another module, the scope and context won't agree.
    else if (GetFlag<InterpreterMethodInfo::Flag_typeHasGenericArgs>()
             && !GetFlag<InterpreterMethodInfo::Flag_methHasGenericArgs>()
             && GetFlag<InterpreterMethodInfo::Flag_hasThisArg>()
             && GetFlag<InterpreterMethodInfo::Flag_thisArgIsObjPtr>() && thisArg != NULL)
    {
        MethodTable* methodClass = reinterpret_cast<MethodDesc*>(m_method)->GetMethodTable();
        MethodTable* contextClass = thisArg->GetMethodTable()->GetMethodTableMatchingParentClass(methodClass);
        return MAKE_CLASSCONTEXT(contextClass);
    }
    else
    {
        return MAKE_METHODCONTEXT(m_method);
    }
}

void Interpreter::DoCallWork(bool virtualCall, void* thisArg, CORINFO_RESOLVED_TOKEN* methTokPtr, CORINFO_CALL_INFO* callInfoPtr)
{
#if INTERP_DYNAMIC_CONTRACTS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
#else
    // Dynamic contract occupies too much stack.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
#endif

#if INTERP_ILCYCLE_PROFILE
#if 0
    // XXX
    unsigned __int64 callStartCycles;
    bool b = CycleTimer::GetThreadCyclesS(&callStartCycles); _ASSERTE(b);
    unsigned __int64 callStartExemptCycles = m_exemptCycles;
#endif
#endif // INTERP_ILCYCLE_PROFILE

#if INTERP_TRACING
    InterlockedIncrement(&s_totalInterpCalls);
#endif // INTERP_TRACING
    unsigned tok = getU4LittleEndian(m_ILCodePtr + sizeof(BYTE));

    // It's possible for an IL method to push a capital-F Frame.  If so, we pop it and save it;
    // we'll push it back on after our GCPROTECT frame is popped.
    Frame* ilPushedFrame = NULL;

    // We can't protect "thisArg" with a GCPROTECT, because this pushes a Frame, and there
    // exist managed methods that push (and pop) Frames -- so that the Frame chain does not return
    // to its original state after a call.  Therefore, we can't have a Frame on the stack over the duration
    // of a call.  (I assume that any method that calls a Frame-pushing IL method performs a matching
    // call to pop that Frame before the caller method completes.  If this were not true, if one method could push
    // a Frame, but defer the pop to its caller, then we could *never* use a Frame in the interpreter, and
    // our implementation plan would be doomed.)
    _ASSERTE(m_callThisArg == NULL);
    m_callThisArg = thisArg;

    // Have we already cached a MethodDescCallSite for this call? (We do this only in loops
    // in the current execution).
    unsigned iloffset = CurOffset();
    CallSiteCacheData* pCscd = NULL;
    if (s_InterpreterUseCaching) pCscd = GetCachedCallInfo(iloffset);

    // If this is true, then we should not cache this call site.
    bool doNotCache;

    CORINFO_RESOLVED_TOKEN methTok;
    CORINFO_CALL_INFO callInfo;
    MethodDesc* methToCall = NULL;
    CORINFO_CLASS_HANDLE exactClass = NULL;
    CORINFO_SIG_INFO_SMALL sigInfo;
    if (pCscd != NULL)
    {
        GCX_PREEMP();
        methToCall = pCscd->m_pMD;
        sigInfo = pCscd->m_sigInfo;

        doNotCache = true; // We already have a cache entry.
    }
    else
    {
        doNotCache = false;  // Until we determine otherwise.
        if (callInfoPtr == NULL)
        {
            GCX_PREEMP();

            // callInfoPtr and methTokPtr must either both be NULL, or neither.
            _ASSERTE(methTokPtr == NULL);

            methTokPtr = &methTok;
            ResolveToken(methTokPtr, tok, CORINFO_TOKENKIND_Method InterpTracingArg(RTK_Call));
            OPCODE opcode = (OPCODE)(*m_ILCodePtr);

            m_interpCeeInfo.getCallInfo(methTokPtr,
                                        m_constrainedFlag ? & m_constrainedResolvedToken : NULL,
                                        m_methInfo->m_method,
                                        //this is how impImportCall invokes getCallInfo
                                        combine(combine(CORINFO_CALLINFO_ALLOWINSTPARAM,
                                                        CORINFO_CALLINFO_SECURITYCHECKS),
                                                (opcode == CEE_CALLVIRT) ? CORINFO_CALLINFO_CALLVIRT
                                                                           : CORINFO_CALLINFO_NONE),
                                        &callInfo);
#if INTERP_ILCYCLE_PROFILE
#if 0
            if (virtualCall)
            {
                unsigned __int64 callEndCycles;
                b = CycleTimer::GetThreadCyclesS(&callEndCycles); _ASSERTE(b);
                unsigned __int64 delta = (callEndCycles - callStartCycles);
                delta -= (m_exemptCycles - callStartExemptCycles);
                s_callCycles += delta;
                s_calls++;
            }
#endif
#endif // INTERP_ILCYCLE_PROFILE

            callInfoPtr = &callInfo;

            _ASSERTE(!callInfoPtr->exactContextNeedsRuntimeLookup);

            methToCall = reinterpret_cast<MethodDesc*>(methTok.hMethod);
            exactClass = methTok.hClass;
        }
        else
        {
            // callInfoPtr and methTokPtr must either both be NULL, or neither.
            _ASSERTE(methTokPtr != NULL);

            _ASSERTE(!callInfoPtr->exactContextNeedsRuntimeLookup);

            methToCall = reinterpret_cast<MethodDesc*>(callInfoPtr->hMethod);
            exactClass = methTokPtr->hClass;
        }

        // We used to take the sigInfo from the callInfo here, but that isn't precise, since
        // we may have made "methToCall" more precise wrt generics than the method handle in
        // the callinfo.  So look up th emore precise signature.
        GCX_PREEMP();

        CORINFO_SIG_INFO sigInfoFull;
        m_interpCeeInfo.getMethodSig(CORINFO_METHOD_HANDLE(methToCall), &sigInfoFull, nullptr);
        sigInfo.retTypeClass = sigInfoFull.retTypeClass;
        sigInfo.numArgs = sigInfoFull.numArgs;
        sigInfo.callConv = sigInfoFull.callConv;
        sigInfo.retType = sigInfoFull.retType;
    }

    // Point A in our cycle count.


    // TODO: enable when NamedIntrinsic is available to interpreter

    /*
    // Is the method an intrinsic?  If so, and if it's one we've written special-case code for
    // handle intrinsically.
    NamedIntrinsic intrinsicName;
    {
        GCX_PREEMP();
        intrinsicName = getIntrinsicName(CORINFO_METHOD_HANDLE(methToCall), nullptr);
    }

#if INTERP_TRACING
    if (intrinsicName == NI_Illegal)
        InterlockedIncrement(&s_totalInterpCallsToIntrinsics);
#endif // INTERP_TRACING
    bool didIntrinsic = false;
    if (!m_constrainedFlag)
    {
        switch (intrinsicId)
        {
        case NI_System_ByReference_ctor:
            DoByReferenceCtor();
            didIntrinsic = true;
            break;
        case NI_System_ByReference_get_Value:
            DoByReferenceValue();
            didIntrinsic = true;
            break;
#if INTERP_ILSTUBS
        case NI_System_StubHelpers_GetStubContext:
            OpStackSet<void*>(m_curStackHt, GetStubContext());
            OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_NATIVEINT));
            m_curStackHt++; didIntrinsic = true;
            break;
#endif // INTERP_ILSTUBS
        default:
#if INTERP_TRACING
            InterlockedIncrement(&s_totalInterpCallsToIntrinsicsUnhandled);
#endif // INTERP_TRACING
            break;
        }

        // Plus some other calls that we're going to treat "like" intrinsics...
        if (methToCall == CoreLibBinder::GetMethod(METHOD__STUBHELPERS__SET_LAST_ERROR))
        {
            // If we're interpreting a method that calls "SetLastError", it's very likely that the call(i) whose
            // error we're trying to capture was performed with MethodDescCallSite machinery that itself trashes
            // the last error.  We solve this by saving the last error in a special interpreter-specific field of
            // "Thread" in that case, and essentially implement SetLastError here, taking that field as the
            // source for the last error.
            Thread* thrd = GetThread();
            thrd->m_dwLastError = thrd->m_dwLastErrorInterp;
            didIntrinsic = true;
        }

        // TODO: The following check for hardware intrinsics is not a production-level
        //       solution and may produce incorrect results.
        static ConfigDWORD s_InterpreterHWIntrinsicsIsSupportedFalse;
        if (s_InterpreterHWIntrinsicsIsSupportedFalse.val(CLRConfig::INTERNAL_InterpreterHWIntrinsicsIsSupportedFalse) != 0)
        {
            GCX_PREEMP();

            // Hardware intrinsics are recognized by name.
            const char* namespaceName = NULL;
            const char* className = NULL;
            const char* methodName = m_interpCeeInfo.getMethodNameFromMetadata((CORINFO_METHOD_HANDLE)methToCall, &className, &namespaceName, NULL);
            if (
#if defined(TARGET_X86) || defined(TARGET_AMD64)
                strcmp(namespaceName, "System.Runtime.Intrinsics.X86") == 0 &&
#elif defined(TARGET_ARM64)
                strcmp(namespaceName, "System.Runtime.Intrinsics.Arm") == 0 &&
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
                strcmp(methodName, "get_IsSupported") == 0
            )
            {
                GCX_COOP();
                DoGetIsSupported();
                didIntrinsic = true;
            }
        }

#if FEATURE_SIMD
        if (fFeatureSIMD.val(CLRConfig::EXTERNAL_FeatureSIMD) != 0)
        {
            // Check for the simd class...
            _ASSERTE(exactClass != NULL);
            GCX_PREEMP();
            bool isIntrinsicType = m_interpCeeInfo.isIntrinsicType(exactClass);

            if (isIntrinsicType)
            {
                // SIMD intrinsics are recognized by name.
                const char* namespaceName = NULL;
                const char* className = NULL;
                const char* methodName = m_interpCeeInfo.getMethodNameFromMetadata((CORINFO_METHOD_HANDLE)methToCall, &className, &namespaceName, NULL);
                if ((strcmp(methodName, "get_IsHardwareAccelerated") == 0) && (strcmp(className, "Vector") == 0) && (strcmp(namespaceName, "System.Numerics") == 0))
                {
                    GCX_COOP();
                    DoSIMDHwAccelerated();
                    didIntrinsic = true;
                }
            }

            if (didIntrinsic)
            {
                // Must block caching or we lose easy access to the class
                doNotCache = true;
            }
        }
#endif // FEATURE_SIMD

    }

    if (didIntrinsic)
    {
        if (s_InterpreterUseCaching && !doNotCache)
        {
            // Cache the token resolution result...
            pCscd = new CallSiteCacheData(methToCall, sigInfo);
            CacheCallInfo(iloffset, pCscd);
        }
        // Now we can return.
        return;
    }
    */

    // Handle other simple special cases:

#if FEATURE_INTERPRETER_DEADSIMPLE_OPT
#ifndef DACCESS_COMPILE
    // Dead simple static getters.
    InterpreterMethodInfo* calleeInterpMethInfo;
    if (GetMethodHandleToInterpMethInfoPtrMap()->Lookup(CORINFO_METHOD_HANDLE(methToCall), &calleeInterpMethInfo))
    {
        if (calleeInterpMethInfo->GetFlag<InterpreterMethodInfo::Flag_methIsDeadSimpleGetter>())
        {
            if (methToCall->IsStatic())
            {
                // TODO
            }
            else
            {
                ILOffsetToItemCache* calleeCache;
                {
                    Object* thisArg = OpStackGet<Object*>(m_curStackHt-1);
                    GCX_FORBID();
                    // We pass NULL for the generic context arg, because a dead simple getter takes none, by definition.
                    calleeCache = calleeInterpMethInfo->GetCacheForCall(thisArg, /*genericsContextArg*/NULL);
                }
                // We've interpreted the getter at least once, so the cache for *some* generics context is populated -- but maybe not
                // this one.  We're hoping that it usually is.
                if (calleeCache != NULL)
                {
                    CachedItem cachedItem;
                    unsigned offsetOfLd;
                    if (calleeInterpMethInfo->GetFlag<InterpreterMethodInfo::Flag_methIsDeadSimpleGetterIsDbgForm>())
                        offsetOfLd = ILOffsetOfLdFldInDeadSimpleInstanceGetterOpt;
                    else
                        offsetOfLd = ILOffsetOfLdFldInDeadSimpleInstanceGetterOpt;

                    bool b = calleeCache->GetItem(offsetOfLd, cachedItem);
                    _ASSERTE_MSG(b, "If the cache exists for this generic context, it should an entry for the LdFld.");
                    _ASSERTE_MSG(cachedItem.m_tag == CIK_InstanceField, "If it's there, it should be an instance field cache.");
                    LdFld(cachedItem.m_value.m_instanceField);
#if INTERP_TRACING
                    InterlockedIncrement(&s_totalInterpCallsToDeadSimpleGetters);
                    InterlockedIncrement(&s_totalInterpCallsToDeadSimpleGettersShortCircuited);
#endif // INTERP_TRACING
                    return;
                }
            }
        }
    }
#endif // DACCESS_COMPILE
#endif // FEATURE_INTERPRETER_DEADSIMPLE_OPT

    unsigned totalSigArgs;
    CORINFO_VARARGS_HANDLE vaSigCookie = nullptr;
    if ((sigInfo.callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG ||
        (sigInfo.callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
    {
        GCX_PREEMP();
        CORINFO_SIG_INFO sig;
        m_interpCeeInfo.findCallSiteSig(m_methInfo->m_module, methTokPtr->token, MAKE_METHODCONTEXT(m_methInfo->m_method), &sig);
        sigInfo.retTypeClass = sig.retTypeClass;
        sigInfo.numArgs = sig.numArgs;
        sigInfo.callConv = sig.callConv;
        sigInfo.retType = sig.retType;
        // Adding 'this' pointer because, numArgs doesn't include the this pointer.
        totalSigArgs = sigInfo.numArgs + sigInfo.hasThis();

        if ((sigInfo.callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
        {
            Module* module = GetModule(sig.scope);
            vaSigCookie = CORINFO_VARARGS_HANDLE(module->GetVASigCookie(Signature(sig.pSig, sig.cbSig)));
        }
        doNotCache = true;
    }
    else
    {
        totalSigArgs = sigInfo.totalILArgs();
    }

    // Note that "totalNativeArgs()" includes space for ret buff arg.
    unsigned nSlots = totalSigArgs + 1;
    if (sigInfo.hasTypeArg()) nSlots++;
    if (sigInfo.isVarArg()) nSlots++;

    DelegateCtorArgs ctorData;
    // If any of these are non-null, they will be pushed as extra arguments (see the code below).
    ctorData.pArg3 = NULL;
    ctorData.pArg4 = NULL;
    ctorData.pArg5 = NULL;

    // Since we make "doNotCache" true below, well never have a non-null "pCscd" for a delegate
    // constructor.  But we have to check for a cached method first, since callInfoPtr may be null in the cached case.
    if (pCscd == NULL && callInfoPtr->classFlags & CORINFO_FLG_DELEGATE && callInfoPtr->methodFlags & CORINFO_FLG_CONSTRUCTOR)
    {
        // We won't cache this case.
        doNotCache = true;

        _ASSERTE_MSG(!sigInfo.hasTypeArg(), "I assume that this isn't possible.");
        GCX_PREEMP();

        ctorData.pMethod = methToCall;

        // Second argument to delegate constructor will be code address of the function the delegate wraps.
        _ASSERTE(TOSIsPtr() && OpStackTypeGet(m_curStackHt-1).ToCorInfoType() != CORINFO_TYPE_BYREF);
        CORINFO_METHOD_HANDLE targetMethodHnd = GetFunctionPointerStack()[m_curStackHt-1];
        _ASSERTE(targetMethodHnd != NULL);
        CORINFO_METHOD_HANDLE alternateCtorHnd = m_interpCeeInfo.GetDelegateCtor(reinterpret_cast<CORINFO_METHOD_HANDLE>(methToCall), methTokPtr->hClass, targetMethodHnd, &ctorData);
        MethodDesc* alternateCtor = reinterpret_cast<MethodDesc*>(alternateCtorHnd);
        if (alternateCtor != methToCall)
        {
            methToCall = alternateCtor;

            // Translate the method address argument from a method handle to the actual callable code address.
            void* val =  (void *)((MethodDesc *)targetMethodHnd)->GetMultiCallableAddrOfCode();
            // Change the method argument to the code pointer.
            OpStackSet<void*>(m_curStackHt-1, val);

            // Now if there are extra arguments, add them to the number of slots; we'll push them on the
            // arg list later.
            if (ctorData.pArg3) nSlots++;
            if (ctorData.pArg4) nSlots++;
            if (ctorData.pArg5) nSlots++;
        }
    }

    // Make sure that the operand stack has the required number of arguments.
    // (Note that this is IL args, not native.)
    //

    // The total number of arguments on the IL stack.  Initially we assume that all the IL arguments
    // the callee expects are on the stack, but may be adjusted downwards if the "this" argument
    // is provided by an allocation (the call is to a constructor).
    unsigned totalArgsOnILStack = totalSigArgs;
    if (m_callThisArg != NULL)
    {
        _ASSERTE(totalArgsOnILStack > 0);
        totalArgsOnILStack--;
    }

#if defined(FEATURE_HFA)
    // Does the callee have an HFA return type?
    unsigned HFAReturnArgSlots = 0;
    {
        GCX_PREEMP();

        if (sigInfo.retType == CORINFO_TYPE_VALUECLASS
            && (m_interpCeeInfo.getHFAType(sigInfo.retTypeClass) != CORINFO_HFA_ELEM_NONE)
            && (sigInfo.getCallConv() & CORINFO_CALLCONV_VARARG) == 0)
        {
            HFAReturnArgSlots = getClassSize(sigInfo.retTypeClass);
            // Round up to a multiple of double size.
            HFAReturnArgSlots = (HFAReturnArgSlots + sizeof(ARG_SLOT) - 1) / sizeof(ARG_SLOT);
        }
    }
#elif defined(UNIX_AMD64_ABI)
    unsigned HasTwoSlotBuf = sigInfo.retType == CORINFO_TYPE_VALUECLASS &&
        getClassSize(sigInfo.retTypeClass) == 16;
#endif

    // Point B

    const unsigned LOCAL_ARG_SLOTS = 8;
    ARG_SLOT localArgs[LOCAL_ARG_SLOTS];
    InterpreterType localArgTypes[LOCAL_ARG_SLOTS];

    ARG_SLOT* args;
    InterpreterType* argTypes;
#if defined(HOST_X86)
    unsigned totalArgSlots = nSlots;
#elif defined(HOST_ARM) || defined(HOST_ARM64)
    // ARM64TODO: Verify that the following statement is correct for ARM64.
    unsigned totalArgSlots = nSlots + HFAReturnArgSlots;
#elif defined(HOST_AMD64)
    unsigned totalArgSlots = nSlots;
#else
#error "unsupported platform"
#endif

    if (totalArgSlots <= LOCAL_ARG_SLOTS)
    {
        args = &localArgs[0];
        argTypes = &localArgTypes[0];
    }
    else
    {
        args = (ARG_SLOT*)_alloca(totalArgSlots * sizeof(ARG_SLOT));
#if defined(HOST_ARM)
        // The HFA return buffer, if any, is assumed to be at a negative
        // offset from the IL arg pointer, so adjust that pointer upward.
        args = args + HFAReturnArgSlots;
#endif // defined(HOST_ARM)
        argTypes = (InterpreterType*)_alloca(nSlots * sizeof(InterpreterType));
    }
    // Make sure that we don't scan any of these until we overwrite them with
    // the real types of the arguments.
    InterpreterType undefIt(CORINFO_TYPE_UNDEF);
    for (unsigned i = 0; i < nSlots; i++) argTypes[i] = undefIt;

    // GC-protect the argument array (as byrefs).
    m_args = args; m_argsSize = nSlots; m_argTypes = argTypes;

    // This is the index into the "args" array (where we copy the value to).
    int curArgSlot = 0;

    // The operand stack index of the first IL argument.
    _ASSERTE(m_curStackHt >= totalArgsOnILStack);
    int argsBase = m_curStackHt - totalArgsOnILStack;

    // Current on-stack argument index.
    unsigned arg = 0;

    // We do "this" -- in the case of a constructor, we "shuffle" the "m_callThisArg" argument in as the first
    // argument -- it isn't on the IL operand stack.

    if (m_constrainedFlag)
    {
        _ASSERTE(m_callThisArg == NULL);  // "m_callThisArg" non-null only for .ctor, which are not callvirts.

        CorInfoType argCIT = OpStackTypeGet(argsBase + arg).ToCorInfoType();
        if (argCIT != CORINFO_TYPE_BYREF)
            VerificationError("This arg of constrained call must be managed pointer.");

        // We only cache for the CORINFO_NO_THIS_TRANSFORM case, so we may assume that if we have a cached call site,
        // there's no thisTransform to perform.
        if (pCscd == NULL)
        {
            switch (callInfoPtr->thisTransform)
            {
            case CORINFO_NO_THIS_TRANSFORM:
                // It is a constrained call on a method implemented by a value type; this is already the proper managed pointer.
                break;

            case CORINFO_DEREF_THIS:
#ifdef _DEBUG
                {
                    GCX_PREEMP();
                    DWORD clsAttribs = m_interpCeeInfo.getClassAttribs(m_constrainedResolvedToken.hClass);
                    _ASSERTE((clsAttribs & CORINFO_FLG_VALUECLASS) == 0);
                }
#endif // _DEBUG
                {
                    // As per the spec, dereference the byref to the "this" pointer, and substitute it as the new "this" pointer.
                    GCX_FORBID();
                    Object** objPtrPtr = OpStackGet<Object**>(argsBase + arg);
                    OpStackSet<Object*>(argsBase + arg, *objPtrPtr);
                    OpStackTypeSet(argsBase + arg, InterpreterType(CORINFO_TYPE_CLASS));
                }
                doNotCache = true;
                break;

            case CORINFO_BOX_THIS:
                // This is the case where the call is to a virtual method of Object the given
                // struct class does not override -- the struct must be boxed, so that the
                // method can be invoked as a virtual.
                BoxStructRefAt(argsBase + arg, m_constrainedResolvedToken.hClass);
                doNotCache = true;
                break;
            }

            exactClass = m_constrainedResolvedToken.hClass;
            {
                GCX_PREEMP();
                DWORD exactClassAttribs = m_interpCeeInfo.getClassAttribs(exactClass);
                // If the constraint type is a value class, then it is the exact class (which will be the
                // "owner type" in the MDCS below.)  If it is not, leave it as the (precise) interface method.
                if (exactClassAttribs & CORINFO_FLG_VALUECLASS)
                {
                    MethodTable* exactClassMT = GetMethodTableFromClsHnd(exactClass);
                    // Find the method on exactClass corresponding to methToCall.
                    methToCall = MethodDesc::FindOrCreateAssociatedMethodDesc(
                        reinterpret_cast<MethodDesc*>(callInfoPtr->hMethod),  // pPrimaryMD
                        exactClassMT,                                         // pExactMT
                        FALSE,                                                // forceBoxedEntryPoint
                        methToCall->GetMethodInstantiation(),                 // methodInst
                        FALSE);                                               // allowInstParam
                }
                else
                {
                    exactClass = methTokPtr->hClass;
                }
            }
        }

        // We've consumed the constraint, so reset the flag.
        m_constrainedFlag = false;
    }

    if (pCscd == NULL)
    {
        if (callInfoPtr->methodFlags & CORINFO_FLG_STATIC)
        {
            MethodDesc* pMD = reinterpret_cast<MethodDesc*>(callInfoPtr->hMethod);
            EnsureClassInit(pMD->GetMethodTable());
        }
    }

    // Point C

    // We must do anything that might make a COOP->PREEMP transition before copying arguments out of the
    // operand stack (where they are GC-protected) into the args array (where they are not).
#ifdef _DEBUG
    const char* clsOfMethToCallName;;
    const char* methToCallName = NULL;
    {
        GCX_PREEMP();
        methToCallName = m_interpCeeInfo.getMethodName(CORINFO_METHOD_HANDLE(methToCall), &clsOfMethToCallName);
    }
#if INTERP_TRACING
    if (strncmp(methToCallName, "get_", 4) == 0)
    {
        InterlockedIncrement(&s_totalInterpCallsToGetters);
        size_t offsetOfLd;
        if (IsDeadSimpleGetter(&m_interpCeeInfo, methToCall, &offsetOfLd))
        {
            InterlockedIncrement(&s_totalInterpCallsToDeadSimpleGetters);
        }
    }
    else if (strncmp(methToCallName, "set_", 4) == 0)
    {
        InterlockedIncrement(&s_totalInterpCallsToSetters);
    }
#endif // INTERP_TRACING

    // Only do this check on the first call, since it should be the same each time.
    if (pCscd == NULL)
    {
        // Ensure that any value types used as argument types are loaded.  This property is checked
        // by the MethodDescCall site mechanisms.  Since enums are freely convertible with their underlying
        // integer type, this is at least one case where a caller may push a value convertible to a value type
        // without any code having caused the value type to be loaded.  This is DEBUG-only because if the callee
        // the integer-type value as the enum value type, it will have loaded the value type.
        MetaSig ms(methToCall);
        CorElementType argType;
        while ((argType = ms.NextArg()) != ELEMENT_TYPE_END)
        {
            if (argType == ELEMENT_TYPE_VALUETYPE)
            {
                TypeHandle th = ms.GetLastTypeHandleThrowing(ClassLoader::LoadTypes);
                CONSISTENCY_CHECK(th.CheckFullyLoaded());
                CONSISTENCY_CHECK(th.IsRestored_NoLogging());
            }
        }
    }
#endif

    // CYCLE PROFILE: BEFORE ARG PROCESSING.

    if (sigInfo.hasThis())
    {
        if (m_callThisArg != NULL)
        {
            if (size_t(m_callThisArg) == 0x1)
            {
                args[curArgSlot] = NULL;
            }
            else
            {
                args[curArgSlot] = PtrToArgSlot(m_callThisArg);
            }
            argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_BYREF);
        }
        else
        {
            args[curArgSlot] = PtrToArgSlot(OpStackGet<void*>(argsBase + arg));
            argTypes[curArgSlot] = OpStackTypeGet(argsBase + arg);
            arg++;
        }
        // AV -> NullRef translation is NYI for the interpreter,
        // so we should manually check and throw the correct exception.
        if (args[curArgSlot] == NULL)
        {
            // If we're calling a constructor, we bypass this check since the runtime
            // should have thrown OOM if it was unable to allocate an instance.
            if (m_callThisArg == NULL)
            {
                _ASSERTE(!methToCall->IsStatic());
                ThrowNullPointerException();
            }
            // ...except in the case of strings, which are both
            // allocated and initialized by their special constructor.
            else
            {
                _ASSERTE(methToCall->IsCtor() && methToCall->GetMethodTable()->IsString());
            }
        }
        curArgSlot++;
    }

    // This is the argument slot that will be used to hold the return value.
    // In UNIX_AMD64_ABI, return type may have need tow ARG_SLOTs.
    ARG_SLOT retVals[2] = {0, 0};
#if !defined(HOST_ARM) && !defined(UNIX_AMD64_ABI)
    _ASSERTE (NUMBER_RETURNVALUE_SLOTS == 1);
#endif

    // If the return type is a structure, then these will be initialized.
    CORINFO_CLASS_HANDLE retTypeClsHnd = NULL;
    InterpreterType retTypeIt;
    size_t retTypeSz = 0;

    // If non-null, space allocated to hold a large struct return value.  Should be deleted later.
    // (I could probably optimize this pop all the arguments first, then allocate space for the return value
    // on the large structure operand stack, and pass a pointer directly to that space, avoiding the extra
    // copy we have below.  But this seemed more expedient, and this should be a pretty rare case.)
    BYTE* pLargeStructRetVal = NULL;

    // If there's a "GetFlag<Flag_hasRetBuffArg>()" struct return value, it will be stored in this variable if it fits,
    // otherwise, we'll dynamically allocate memory for it.
    ARG_SLOT smallStructRetVal = 0;

    // We should have no return buffer temp space registered here...unless this is a constructor, in which
    // case it will return void.  In particular, if the return type VALUE_CLASS, then this should be NULL.
    _ASSERTE_MSG((pCscd != NULL) || sigInfo.retType == CORINFO_TYPE_VOID || m_structRetValITPtr == NULL, "Invariant.");

    // Is it the return value a struct with a ret buff?
    _ASSERTE_MSG(methToCall != NULL, "assumption");
    bool hasRetBuffArg = false;
    if (sigInfo.retType == CORINFO_TYPE_VALUECLASS || sigInfo.retType == CORINFO_TYPE_REFANY)
    {
        hasRetBuffArg = !!methToCall->HasRetBuffArg();
        retTypeClsHnd = sigInfo.retTypeClass;

        MetaSig ms(methToCall);


        // On ARM, if there's an HFA return type, we must also allocate a return buffer, since the
        // MDCS calling convention requires it.
        if (hasRetBuffArg
#if defined(HOST_ARM)
            || HFAReturnArgSlots > 0
#endif // defined(HOST_ARM)
            )
        {
            _ASSERTE(retTypeClsHnd != NULL);
            retTypeIt = InterpreterType(&m_interpCeeInfo, retTypeClsHnd);
            retTypeSz = retTypeIt.Size(&m_interpCeeInfo);

#if defined(HOST_ARM)
            if (HFAReturnArgSlots > 0)
            {
                args[curArgSlot] = PtrToArgSlot(args - HFAReturnArgSlots);
            }
            else
#endif // defined(HOST_ARM)

            if (retTypeIt.IsLargeStruct(&m_interpCeeInfo))
            {
                size_t retBuffSize = retTypeSz;
                // If the target architecture can sometimes return a struct in several registers,
                // MethodDescCallSite will reserve a return value array big enough to hold the maximum.
                // It will then copy *all* of this into the return buffer area we allocate.  So make sure
                // we allocate at least that much.
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
                retBuffSize = max(retTypeSz, ENREGISTERED_RETURNTYPE_MAXSIZE);
#endif // ENREGISTERED_RETURNTYPE_MAXSIZE
                pLargeStructRetVal = (BYTE*)_alloca(retBuffSize);
                // Clear this in case a GC happens.
                for (unsigned i = 0; i < retTypeSz; i++) pLargeStructRetVal[i] = 0;
                // Register this as location needing GC.
                m_structRetValTempSpace = pLargeStructRetVal;
                // Set it as the return buffer.
                args[curArgSlot] = PtrToArgSlot(pLargeStructRetVal);
            }
            else
            {
                // Clear this in case a GC happens.
                smallStructRetVal = 0;
                // Register this as location needing GC.
                m_structRetValTempSpace = &smallStructRetVal;
                // Set it as the return buffer.
                args[curArgSlot] = PtrToArgSlot(&smallStructRetVal);
            }
            m_structRetValITPtr = &retTypeIt;
            argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
            curArgSlot++;
        }
        else
        {
            // The struct type might "normalize" to a primitive type.
            if (retTypeClsHnd == NULL)
            {
                retTypeIt = InterpreterType(CEEInfo::asCorInfoType(ms.GetReturnTypeNormalized()));
            }
            else
            {
                retTypeIt = InterpreterType(&m_interpCeeInfo, retTypeClsHnd);
            }
        }
    }

    if (((sigInfo.callConv & CORINFO_CALLCONV_VARARG) != 0) && sigInfo.isVarArg())
    {
        _ASSERTE(vaSigCookie != nullptr);
        args[curArgSlot] = PtrToArgSlot(vaSigCookie);
        argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
        curArgSlot++;
    }

    if (pCscd == NULL)
    {
        if (sigInfo.hasTypeArg())
        {
            GCX_PREEMP();
            // We will find the instantiating stub for the method, and call that instead.
            CORINFO_SIG_INFO sigInfoFull;
            Instantiation methodInst = methToCall->GetMethodInstantiation();
            BOOL fNeedUnboxingStub = virtualCall && TypeHandle(exactClass).IsValueType() && methToCall->IsVirtual();
            methToCall = MethodDesc::FindOrCreateAssociatedMethodDesc(methToCall,
                TypeHandle(exactClass).GetMethodTable(), fNeedUnboxingStub, methodInst, FALSE, TRUE);
            m_interpCeeInfo.getMethodSig(CORINFO_METHOD_HANDLE(methToCall), &sigInfoFull, nullptr);
            sigInfo.retTypeClass = sigInfoFull.retTypeClass;
            sigInfo.numArgs = sigInfoFull.numArgs;
            sigInfo.callConv = sigInfoFull.callConv;
            sigInfo.retType = sigInfoFull.retType;
        }

        if (sigInfo.hasTypeArg())
        {
            // If we still have a type argument, we're calling an ArrayOpStub and need to pass the array TypeHandle.
            _ASSERTE(methToCall->IsArray());
            doNotCache = true;
            args[curArgSlot] = PtrToArgSlot(exactClass);
            argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
            curArgSlot++;
        }
    }

    // Now we do the non-this arguments.
    size_t largeStructSpaceToPop = 0;
    for (; arg < totalArgsOnILStack; arg++)
    {
        InterpreterType argIt = OpStackTypeGet(argsBase + arg);
        size_t sz = OpStackTypeGet(argsBase + arg).Size(&m_interpCeeInfo);
        switch (sz)
        {
        case 1:
            args[curArgSlot] = OpStackGet<INT8>(argsBase + arg);
            break;
        case 2:
            args[curArgSlot] = OpStackGet<INT16>(argsBase + arg);
            break;
        case 4:
            args[curArgSlot] = OpStackGet<INT32>(argsBase + arg);
            break;
        case 8:
        default:
            if (sz > 8)
            {
                void* srcPtr = OpStackGet<void*>(argsBase + arg);
                args[curArgSlot] = PtrToArgSlot(srcPtr);
                if (!IsInLargeStructLocalArea(srcPtr))
                    largeStructSpaceToPop += sz;
            }
            else
            {
                args[curArgSlot] = OpStackGet<INT64>(argsBase + arg);
            }
            break;
        }
        argTypes[curArgSlot] = argIt;
        curArgSlot++;
    }

    if (ctorData.pArg3)
    {
        args[curArgSlot] = PtrToArgSlot(ctorData.pArg3);
        argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
        curArgSlot++;
    }
    if (ctorData.pArg4)
    {
        args[curArgSlot] = PtrToArgSlot(ctorData.pArg4);
        argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
        curArgSlot++;
    }
    if (ctorData.pArg5)
    {
        args[curArgSlot] = PtrToArgSlot(ctorData.pArg5);
        argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
        curArgSlot++;
    }

    // CYCLE PROFILE: AFTER ARG PROCESSING.
    {
        Thread* thr = GetThread();

        Object** thisArgHnd = NULL;
        ARG_SLOT nullThisArg = NULL;
        if (sigInfo.hasThis())
        {
            if (m_callThisArg != NULL)
            {
                if (size_t(m_callThisArg) == 0x1)
                {
                    thisArgHnd = reinterpret_cast<Object**>(&nullThisArg);
                }
                else
                {
                    thisArgHnd = reinterpret_cast<Object**>(&m_callThisArg);
                }
            }
            else
            {
                thisArgHnd = OpStackGetAddr<Object*>(argsBase);
            }
        }

        Frame* topFrameBefore = thr->GetFrame();

#if INTERP_ILCYCLE_PROFILE
        unsigned __int64 startCycles;
#endif // INTERP_ILCYCLE_PROFILE

        // CYCLE PROFILE: BEFORE MDCS CREATION.

        PCODE target = NULL;
        MethodDesc *exactMethToCall = methToCall;

        // Determine the target of virtual calls.
        if (virtualCall && methToCall->IsVtableMethod())
        {
            PCODE pCode;

            _ASSERTE(thisArgHnd != NULL);
            OBJECTREF objRef = ObjectToOBJECTREF(*thisArgHnd);
            GCPROTECT_BEGIN(objRef);
            pCode = methToCall->GetMultiCallableAddrOfVirtualizedCode(&objRef, methToCall->GetMethodTable());
            GCPROTECT_END();

            exactMethToCall = Entry2MethodDesc(pCode, objRef->GetMethodTable());
        }

        // Compile the target in advance of calling.
        if (exactMethToCall->IsPointingToPrestub())
        {
            MethodTable* dispatchingMT = NULL;
            if (exactMethToCall->IsVtableMethod())
            {
                _ASSERTE(thisArgHnd != NULL);
                dispatchingMT = (*thisArgHnd)->GetMethodTable();
            }
            GCX_PREEMP();
            target = exactMethToCall->DoPrestub(dispatchingMT);
        }
        else
        {
            target = exactMethToCall->GetMethodEntryPoint();
        }

        // If we're interpreting the method, simply call it directly.
        if (InterpretationStubToMethodInfo(target) == exactMethToCall)
        {
            _ASSERTE(!exactMethToCall->IsILStub());
            InterpreterMethodInfo* methInfo = MethodHandleToInterpreterMethInfoPtr(CORINFO_METHOD_HANDLE(exactMethToCall));
            _ASSERTE(methInfo != NULL);
#if INTERP_ILCYCLE_PROFILE
            bool b = CycleTimer::GetThreadCyclesS(&startCycles); _ASSERTE(b);
#endif // INTERP_ILCYCLE_PROFILE
            retVals[0] = InterpretMethodBody(methInfo, true, reinterpret_cast<BYTE*>(args), NULL);
            pCscd = NULL;  // Nothing to cache.
        }
        else
        {
            MetaSig msig(exactMethToCall);
            // We've already resolved the virtual call target above, so there is no need to do it again.
            MethodDescCallSite mdcs(exactMethToCall, &msig, target);
#if INTERP_ILCYCLE_PROFILE
            bool b = CycleTimer::GetThreadCyclesS(&startCycles); _ASSERTE(b);
#endif // INTERP_ILCYCLE_PROFILE

#if defined(UNIX_AMD64_ABI)
            mdcs.CallTargetWorker(args, retVals, HasTwoSlotBuf ? 16: 8);
#else
            mdcs.CallTargetWorker(args, retVals, 8);
#endif

            if (pCscd != NULL)
            {
                // We will do a check at the end to determine whether to cache pCscd, to set
                // to NULL here to make sure we don't.
                pCscd = NULL;
            }
            else
            {
                // For now, we won't cache virtual calls to virtual methods.
                // TODO: fix this somehow.
                if (virtualCall && (callInfoPtr->methodFlags & CORINFO_FLG_VIRTUAL)) doNotCache = true;

                if (s_InterpreterUseCaching && !doNotCache)
                {
                    // We will add this to the cache later; the locking provokes a GC,
                    // and "retVal" is vulnerable.
                    pCscd = new CallSiteCacheData(exactMethToCall, sigInfo);
                }
            }
        }
#if INTERP_ILCYCLE_PROFILE
        unsigned __int64 endCycles;
        bool b = CycleTimer::GetThreadCyclesS(&endCycles); _ASSERTE(b);
        m_exemptCycles += (endCycles - startCycles);
#endif // INTERP_ILCYCLE_PROFILE

        // retVal is now vulnerable.
        GCX_FORBID();

        // Some managed methods, believe it or not, can push capital-F Frames on the Frame chain.
        // If this happens, executing the EX_CATCH below will pop it, which is bad.
        // So detect that case, pop the explicitly-pushed frame, and push it again after the EX_CATCH.
        // (Asserting that there is only 1 such frame!)
        if (thr->GetFrame() != topFrameBefore)
        {
            ilPushedFrame = thr->GetFrame();
            if (ilPushedFrame != NULL)
            {
                ilPushedFrame->Pop(thr);
                if (thr->GetFrame() != topFrameBefore)
                {
                    // This wasn't an IL-pushed frame, so restore.
                    ilPushedFrame->Push(thr);
                    ilPushedFrame = NULL;
                }
            }
        }
    }

    // retVal is still vulnerable.
    {
        GCX_FORBID();
        m_argsSize = 0;

        // At this point, the call has happened successfully.  We can delete the arguments from the operand stack.
        m_curStackHt -= totalArgsOnILStack;
        // We've already checked that "largeStructSpaceToPop
        LargeStructOperandStackPop(largeStructSpaceToPop, NULL);

        if (size_t(m_callThisArg) == 0x1)
        {
            _ASSERTE_MSG(sigInfo.retType == CORINFO_TYPE_VOID, "Constructor for var-sized object becomes factory method that returns result.");
            OpStackSet<Object*>(m_curStackHt, reinterpret_cast<Object*>(retVals[0]));
            OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_CLASS));
            m_curStackHt++;
        }
        else if (sigInfo.retType != CORINFO_TYPE_VOID)
        {
            switch (sigInfo.retType)
            {
            case CORINFO_TYPE_BOOL:
            case CORINFO_TYPE_BYTE:
                OpStackSet<INT32>(m_curStackHt, static_cast<INT8>(retVals[0]));
                break;
            case CORINFO_TYPE_UBYTE:
                OpStackSet<UINT32>(m_curStackHt, static_cast<UINT8>(retVals[0]));
                break;
            case CORINFO_TYPE_SHORT:
                OpStackSet<INT32>(m_curStackHt, static_cast<INT16>(retVals[0]));
                break;
            case CORINFO_TYPE_USHORT:
            case CORINFO_TYPE_CHAR:
                OpStackSet<UINT32>(m_curStackHt, static_cast<UINT16>(retVals[0]));
                break;
            case CORINFO_TYPE_INT:
            case CORINFO_TYPE_UINT:
            case CORINFO_TYPE_FLOAT:
                OpStackSet<INT32>(m_curStackHt, static_cast<INT32>(retVals[0]));
                break;
            case CORINFO_TYPE_LONG:
            case CORINFO_TYPE_ULONG:
            case CORINFO_TYPE_DOUBLE:
                OpStackSet<INT64>(m_curStackHt, static_cast<INT64>(retVals[0]));
                break;
            case CORINFO_TYPE_NATIVEINT:
            case CORINFO_TYPE_NATIVEUINT:
            case CORINFO_TYPE_PTR:
                OpStackSet<NativeInt>(m_curStackHt, static_cast<NativeInt>(retVals[0]));
                break;
            case CORINFO_TYPE_CLASS:
                OpStackSet<Object*>(m_curStackHt, reinterpret_cast<Object*>(retVals[0]));
                break;
            case CORINFO_TYPE_BYREF:
                OpStackSet<void*>(m_curStackHt, reinterpret_cast<void*>(retVals[0]));
                break;
            case CORINFO_TYPE_VALUECLASS:
            case CORINFO_TYPE_REFANY:
                {
                    // We must be careful here to write the value, the type, and update the stack height in one
                    // sequence that has no COOP->PREEMP transitions in it, so no GC's happen until the value
                    // is protected by being fully "on" the operandStack.
#if defined(HOST_ARM)
                    // Is the return type an HFA?
                    if (HFAReturnArgSlots > 0)
                    {
                        ARG_SLOT* hfaRetBuff = args - HFAReturnArgSlots;
                        if (retTypeIt.IsLargeStruct(&m_interpCeeInfo))
                        {
                            void* dst = LargeStructOperandStackPush(retTypeSz);
                            memcpy(dst, hfaRetBuff, retTypeSz);
                            OpStackSet<void*>(m_curStackHt, dst);
                        }
                        else
                        {
                            memcpy(OpStackGetAddr<UINT64>(m_curStackHt), hfaRetBuff, retTypeSz);
                        }
                    }
                    else
#endif // defined(HOST_ARM)
                    if (pLargeStructRetVal != NULL)
                    {
                        _ASSERTE(hasRetBuffArg);
                        void* dst = LargeStructOperandStackPush(retTypeSz);
                        CopyValueClassUnchecked(dst, pLargeStructRetVal, GetMethodTableFromClsHnd(retTypeClsHnd));
                        OpStackSet<void*>(m_curStackHt, dst);
                    }
                    else if (hasRetBuffArg)
                    {
                        OpStackSet<INT64>(m_curStackHt, GetSmallStructValue(&smallStructRetVal, retTypeSz));
                    }
#if defined(UNIX_AMD64_ABI)
                    else if (HasTwoSlotBuf)
                    {
                        void* dst = LargeStructOperandStackPush(16);
                        CopyValueClassUnchecked(dst, retVals, GetMethodTableFromClsHnd(retTypeClsHnd));
                        OpStackSet<void*>(m_curStackHt, dst);
                    }
#endif
                    else
                    {
                        OpStackSet<UINT64>(m_curStackHt, retVals[0]);
                    }
                    // We already created this interpreter type, so use it.
                    OpStackTypeSet(m_curStackHt, retTypeIt.StackNormalize());
                    m_curStackHt++;


                    // In the value-class case, the call might have used a ret buff, which we would have registered for GC scanning.
                    // Make sure it's unregistered.
                    m_structRetValITPtr = NULL;
                }
                break;
            default:
                NYI_INTERP("Unhandled return type");
                break;
            }
            _ASSERTE_MSG(m_structRetValITPtr == NULL, "Invariant.");

            // The valueclass case is handled fully in the switch above.
            if (sigInfo.retType != CORINFO_TYPE_VALUECLASS &&
                    sigInfo.retType != CORINFO_TYPE_REFANY)
            {
                OpStackTypeSet(m_curStackHt, InterpreterType(sigInfo.retType).StackNormalize());
                m_curStackHt++;
            }
        }
    }

    // Originally, this assertion was in the ValueClass case above, but it does a COOP->PREEMP
    // transition, and therefore causes a GC, and we're GCX_FORBIDden from doing a GC while retVal
    // is vulnerable.  So, for completeness, do it here.
    _ASSERTE(sigInfo.retType != CORINFO_TYPE_VALUECLASS || retTypeIt == InterpreterType(&m_interpCeeInfo, retTypeClsHnd));

    // If we created a cached call site, cache it now (when it's safe to take a GC).
    if (pCscd != NULL && !doNotCache)
    {
        CacheCallInfo(iloffset, pCscd);
    }

    m_callThisArg = NULL;

    // If the call we just made pushed a Frame, we popped it above, so re-push it.
    if (ilPushedFrame != NULL) ilPushedFrame->Push();
}

#include "metadata.h"

void Interpreter::CallI()
{
#if INTERP_DYNAMIC_CONTRACTS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
#else
    // Dynamic contract occupies too much stack.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
#endif

#if INTERP_TRACING
    InterlockedIncrement(&s_totalInterpCalls);
#endif // INTERP_TRACING

    unsigned tok = getU4LittleEndian(m_ILCodePtr + sizeof(BYTE));

    CORINFO_SIG_INFO sigInfo;

    {
        GCX_PREEMP();
        m_interpCeeInfo.findSig(m_methInfo->m_module, tok, GetPreciseGenericsContext(), &sigInfo);
    }

    // I'm assuming that a calli can't depend on the generics context, so the simple form of type
    // context should suffice?
    MethodDesc* pMD = reinterpret_cast<MethodDesc*>(m_methInfo->m_method);
    SigTypeContext sigTypeCtxt(pMD);
    MetaSig mSig(sigInfo.pSig, sigInfo.cbSig, GetModule(sigInfo.scope), &sigTypeCtxt);

    unsigned totalSigArgs = sigInfo.totalILArgs();

    // Note that "totalNativeArgs()" includes space for ret buff arg.
    unsigned nSlots = totalSigArgs + 1;
    if ((sigInfo.callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
    {
        nSlots++;
    }

    // Make sure that the operand stack has the required number of arguments.
    // (Note that this is IL args, not native.)
    //

    // The total number of arguments on the IL stack.  Initially we assume that all the IL arguments
    // the callee expects are on the stack, but may be adjusted downwards if the "this" argument
    // is provided by an allocation (the call is to a constructor).
    unsigned totalArgsOnILStack = totalSigArgs;

    const unsigned LOCAL_ARG_SLOTS = 8;
    ARG_SLOT localArgs[LOCAL_ARG_SLOTS];
    InterpreterType localArgTypes[LOCAL_ARG_SLOTS];

    ARG_SLOT* args;
    InterpreterType* argTypes;
    if (nSlots <= LOCAL_ARG_SLOTS)
    {
        args = &localArgs[0];
        argTypes = &localArgTypes[0];
    }
    else
    {
        args = (ARG_SLOT*)_alloca(nSlots * sizeof(ARG_SLOT));
        argTypes = (InterpreterType*)_alloca(nSlots * sizeof(InterpreterType));
    }
    // Make sure that we don't scan any of these until we overwrite them with
    // the real types of the arguments.
    InterpreterType undefIt(CORINFO_TYPE_UNDEF);
    for (unsigned i = 0; i < nSlots; i++)
    {
        argTypes[i] = undefIt;
    }

    // GC-protect the argument array (as byrefs).
    m_args = args;
    m_argsSize = nSlots;
    m_argTypes = argTypes;

    // This is the index into the "args" array (where we copy the value to).
    int curArgSlot = 0;

    // The operand stack index of the first IL argument.
    unsigned totalArgPositions = totalArgsOnILStack + 1;  // + 1 for the ftn argument.
    _ASSERTE(m_curStackHt >= totalArgPositions);
    int argsBase = m_curStackHt - totalArgPositions;

    // Current on-stack argument index.
    unsigned arg = 0;

    if (sigInfo.hasThis())
    {
        args[curArgSlot] = PtrToArgSlot(OpStackGet<void*>(argsBase + arg));
        argTypes[curArgSlot] = OpStackTypeGet(argsBase + arg);
        // AV -> NullRef translation is NYI for the interpreter,
        // so we should manually check and throw the correct exception.
        ThrowOnInvalidPointer((void*)args[curArgSlot]);
        arg++;
        curArgSlot++;
    }

    // This is the argument slot that will be used to hold the return value.
    ARG_SLOT retVal = 0;

    // If the return type is a structure, then these will be initialized.
    CORINFO_CLASS_HANDLE retTypeClsHnd = NULL;
    InterpreterType retTypeIt;
    size_t retTypeSz = 0;

    // If non-null, space allocated to hold a large struct return value.  Should be deleted later.
    // (I could probably optimize this pop all the arguments first, then allocate space for the return value
    // on the large structure operand stack, and pass a pointer directly to that space, avoiding the extra
    // copy we have below.  But this seemed more expedient, and this should be a pretty rare case.)
    BYTE* pLargeStructRetVal = NULL;

    // If there's a "GetFlag<Flag_hasRetBuffArg>()" struct return value, it will be stored in this variable if it fits,
    // otherwise, we'll dynamically allocate memory for it.
    ARG_SLOT smallStructRetVal = 0;

    // We should have no return buffer temp space registered here...unless this is a constructor, in which
    // case it will return void.  In particular, if the return type VALUE_CLASS, then this should be NULL.
    _ASSERTE_MSG(sigInfo.retType == CORINFO_TYPE_VOID || m_structRetValITPtr == NULL, "Invariant.");

    // Is it the return value a struct with a ret buff?
    bool hasRetBuffArg = false;
    if (sigInfo.retType == CORINFO_TYPE_VALUECLASS)
    {
        retTypeClsHnd = sigInfo.retTypeClass;
        retTypeIt = InterpreterType(&m_interpCeeInfo, retTypeClsHnd);
        retTypeSz = retTypeIt.Size(&m_interpCeeInfo);

#if defined(UNIX_AMD64_ABI)
        //
#elif defined(HOST_AMD64)
        // TODO: Investigate why HasRetBuffArg can't be used. pMD is a hacked up MD for the
        // calli because it belongs to the current method. Doing what the JIT does.
        hasRetBuffArg = (retTypeSz > sizeof(void*)) || ((retTypeSz & (retTypeSz - 1)) != 0);
#else
        hasRetBuffArg = !!pMD->HasRetBuffArg();
#endif
        if (hasRetBuffArg)
        {
            if (retTypeIt.IsLargeStruct(&m_interpCeeInfo))
            {
                size_t retBuffSize = retTypeSz;
                // If the target architecture can sometimes return a struct in several registers,
                // MethodDescCallSite will reserve a return value array big enough to hold the maximum.
                // It will then copy *all* of this into the return buffer area we allocate.  So make sure
                // we allocate at least that much.
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
                retBuffSize = max(retTypeSz, ENREGISTERED_RETURNTYPE_MAXSIZE);
#endif // ENREGISTERED_RETURNTYPE_MAXSIZE
                pLargeStructRetVal = (BYTE*)_alloca(retBuffSize);

                // Clear this in case a GC happens.
                for (unsigned i = 0; i < retTypeSz; i++)
                {
                    pLargeStructRetVal[i] = 0;
                }

                // Register this as location needing GC.
                m_structRetValTempSpace = pLargeStructRetVal;

                // Set it as the return buffer.
                args[curArgSlot] = PtrToArgSlot(pLargeStructRetVal);
            }
            else
            {
                // Clear this in case a GC happens.
                smallStructRetVal = 0;

                // Register this as location needing GC.
                m_structRetValTempSpace = &smallStructRetVal;

                // Set it as the return buffer.
                args[curArgSlot] = PtrToArgSlot(&smallStructRetVal);
            }
            m_structRetValITPtr = &retTypeIt;
            argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
            curArgSlot++;
        }
    }

    if ((sigInfo.callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
    {
        Module* module = GetModule(sigInfo.scope);
        CORINFO_VARARGS_HANDLE handle = CORINFO_VARARGS_HANDLE(module->GetVASigCookie(Signature(sigInfo.pSig, sigInfo.cbSig)));
        args[curArgSlot] = PtrToArgSlot(handle);
        argTypes[curArgSlot] = InterpreterType(CORINFO_TYPE_NATIVEINT);
        curArgSlot++;
    }

    // Now we do the non-this arguments.
    size_t largeStructSpaceToPop = 0;
    for (; arg < totalArgsOnILStack; arg++)
    {
        InterpreterType argIt = OpStackTypeGet(argsBase + arg);
        size_t sz = OpStackTypeGet(argsBase + arg).Size(&m_interpCeeInfo);
        switch (sz)
        {
        case 1:
            args[curArgSlot] = OpStackGet<INT8>(argsBase + arg);
            break;
        case 2:
            args[curArgSlot] = OpStackGet<INT16>(argsBase + arg);
            break;
        case 4:
            args[curArgSlot] = OpStackGet<INT32>(argsBase + arg);
            break;
        case 8:
        default:
            if (sz > 8)
            {
                void* srcPtr = OpStackGet<void*>(argsBase + arg);
                args[curArgSlot] = PtrToArgSlot(srcPtr);
                if (!IsInLargeStructLocalArea(srcPtr))
                {
                    largeStructSpaceToPop += sz;
                }
            }
            else
            {
                args[curArgSlot] = OpStackGet<INT64>(argsBase + arg);
            }
            break;
        }
        argTypes[curArgSlot] = argIt;
        curArgSlot++;
    }

    // Finally, we get the code pointer.
    unsigned ftnInd = m_curStackHt - 1;
#ifdef _DEBUG
    CorInfoType ftnType = OpStackTypeGet(ftnInd).ToCorInfoType();
    _ASSERTE(ftnType == CORINFO_TYPE_NATIVEINT
             || ftnType == CORINFO_TYPE_INT
             || ftnType == CORINFO_TYPE_LONG);
#endif // DEBUG

    PCODE ftnPtr = OpStackGet<PCODE>(ftnInd);

    {
        MethodDesc* methToCall;
        // If we're interpreting the target, simply call it directly.
        if ((methToCall = InterpretationStubToMethodInfo((PCODE)ftnPtr)) != NULL)
        {
            InterpreterMethodInfo* methInfo = MethodHandleToInterpreterMethInfoPtr(CORINFO_METHOD_HANDLE(methToCall));
            _ASSERTE(methInfo != NULL);
#if INTERP_ILCYCLE_PROFILE
            bool b = CycleTimer::GetThreadCyclesS(&startCycles); _ASSERTE(b);
#endif // INTERP_ILCYCLE_PROFILE
            retVal = InterpretMethodBody(methInfo, true, reinterpret_cast<BYTE*>(args), NULL);
        }
        else
        {
            // This is not a great workaround.  For the most part, we really don't care what method desc we're using, since
            // we're providing the signature and function pointer -- other than that it's well-formed and "activated."
            // And also, one more thing: whether it is static or not.  Which is actually determined by the signature.
            // So we query the signature we have to determine whether we need a static or instance MethodDesc, and then
            // use one of the appropriate staticness that happens to be sitting around in global variables.  For static
            // we use "RuntimeHelpers.PrepareConstrainedRegions", for instance we use the default constructor of "Object."
            // TODO: make this cleaner -- maybe invent a couple of empty methods with instructive names, just for this purpose.
            MethodDesc* pMD;
            if (mSig.HasThis())
            {
                pMD = g_pObjectFinalizerMD;
            }
            else
            {
                pMD = CoreLibBinder::GetMethod(METHOD__INTERLOCKED__COMPARE_EXCHANGE_OBJECT);  // A random static method.
            }
            MethodDescCallSite mdcs(pMD, &mSig, ftnPtr);
#if 0
            // If the current method being interpreted is an IL stub, we're calling native code, so
            // change the GC mode.  (We'll only do this at the call if the calling convention turns out
            // to be a managed calling convention.)
            MethodDesc* pStubContextMD = reinterpret_cast<MethodDesc*>(m_stubContext);
            bool transitionToPreemptive = (pStubContextMD != NULL && !pStubContextMD->IsIL());
            mdcs.CallTargetWorker(args, &retVal, sizeof(retVal), transitionToPreemptive);
#else
            // TODO The code above triggers assertion at threads.cpp:6861:
            //     _ASSERTE(thread->PreemptiveGCDisabled());  // Should have been in managed code
            // The workaround will likely break more things than what it is fixing:
            // just do not make transition to preemptive GC for now.
            mdcs.CallTargetWorker(args, &retVal, sizeof(retVal));
#endif
        }
        // retVal is now vulnerable.
        GCX_FORBID();
    }

    // retVal is still vulnerable.
    {
        GCX_FORBID();
        m_argsSize = 0;

        // At this point, the call has happened successfully.  We can delete the arguments from the operand stack.
        m_curStackHt -= totalArgPositions;

        // We've already checked that "largeStructSpaceToPop
        LargeStructOperandStackPop(largeStructSpaceToPop, NULL);

        if (size_t(m_callThisArg) == 0x1)
        {
            _ASSERTE_MSG(sigInfo.retType == CORINFO_TYPE_VOID, "Constructor for var-sized object becomes factory method that returns result.");
            OpStackSet<Object*>(m_curStackHt, reinterpret_cast<Object*>(retVal));
            OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_CLASS));
            m_curStackHt++;
        }
        else if (sigInfo.retType != CORINFO_TYPE_VOID)
        {
            switch (sigInfo.retType)
            {
            case CORINFO_TYPE_BOOL:
            case CORINFO_TYPE_BYTE:
                OpStackSet<INT32>(m_curStackHt, static_cast<INT8>(retVal));
                break;
            case CORINFO_TYPE_UBYTE:
                OpStackSet<UINT32>(m_curStackHt, static_cast<UINT8>(retVal));
                break;
            case CORINFO_TYPE_SHORT:
                OpStackSet<INT32>(m_curStackHt, static_cast<INT16>(retVal));
                break;
            case CORINFO_TYPE_USHORT:
            case CORINFO_TYPE_CHAR:
                OpStackSet<UINT32>(m_curStackHt, static_cast<UINT16>(retVal));
                break;
            case CORINFO_TYPE_INT:
            case CORINFO_TYPE_UINT:
            case CORINFO_TYPE_FLOAT:
                OpStackSet<INT32>(m_curStackHt, static_cast<INT32>(retVal));
                break;
            case CORINFO_TYPE_LONG:
            case CORINFO_TYPE_ULONG:
            case CORINFO_TYPE_DOUBLE:
                OpStackSet<INT64>(m_curStackHt, static_cast<INT64>(retVal));
                break;
            case CORINFO_TYPE_NATIVEINT:
            case CORINFO_TYPE_NATIVEUINT:
            case CORINFO_TYPE_PTR:
                OpStackSet<NativeInt>(m_curStackHt, static_cast<NativeInt>(retVal));
                break;
            case CORINFO_TYPE_CLASS:
                OpStackSet<Object*>(m_curStackHt, reinterpret_cast<Object*>(retVal));
                break;
            case CORINFO_TYPE_VALUECLASS:
                {
                    // We must be careful here to write the value, the type, and update the stack height in one
                    // sequence that has no COOP->PREEMP transitions in it, so no GC's happen until the value
                    // is protected by being fully "on" the operandStack.
                    if (pLargeStructRetVal != NULL)
                    {
                        _ASSERTE(hasRetBuffArg);
                        void* dst = LargeStructOperandStackPush(retTypeSz);
                        CopyValueClassUnchecked(dst, pLargeStructRetVal, GetMethodTableFromClsHnd(retTypeClsHnd));
                        OpStackSet<void*>(m_curStackHt, dst);
                    }
                    else if (hasRetBuffArg)
                    {
                        OpStackSet<INT64>(m_curStackHt, GetSmallStructValue(&smallStructRetVal, retTypeSz));
                    }
                    else
                    {
                        OpStackSet<UINT64>(m_curStackHt, retVal);
                    }
                    // We already created this interpreter type, so use it.
                    OpStackTypeSet(m_curStackHt, retTypeIt.StackNormalize());
                    m_curStackHt++;

                    // In the value-class case, the call might have used a ret buff, which we would have registered for GC scanning.
                    // Make sure it's unregistered.
                    m_structRetValITPtr = NULL;
                }
                break;
            default:
                NYI_INTERP("Unhandled return type");
                break;
            }
            _ASSERTE_MSG(m_structRetValITPtr == NULL, "Invariant.");

            // The valueclass case is handled fully in the switch above.
            if (sigInfo.retType != CORINFO_TYPE_VALUECLASS)
            {
                OpStackTypeSet(m_curStackHt, InterpreterType(sigInfo.retType).StackNormalize());
                m_curStackHt++;
            }
        }
    }

    // Originally, this assertion was in the ValueClass case above, but it does a COOP->PREEMP
    // transition, and therefore causes a GC, and we're GCX_FORBIDden from doing a GC while retVal
    // is vulnerable.  So, for completeness, do it here.
    _ASSERTE(sigInfo.retType != CORINFO_TYPE_VALUECLASS || retTypeIt == InterpreterType(&m_interpCeeInfo, retTypeClsHnd));

    m_ILCodePtr += 5;
}

// static
bool Interpreter::IsDeadSimpleGetter(CEEInfo* info, MethodDesc* pMD, size_t* offsetOfLd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    DWORD flags = pMD->GetAttrs();
    CORINFO_METHOD_INFO methInfo;
    {
        GCX_PREEMP();
        bool b = info->getMethodInfo(CORINFO_METHOD_HANDLE(pMD), &methInfo);
        if (!b) return false;
    }

    // If the method takes a generic type argument, it's not dead simple...
    if (methInfo.args.callConv & CORINFO_CALLCONV_PARAMTYPE) return false;

    BYTE* codePtr = methInfo.ILCode;

    if (flags & CORINFO_FLG_STATIC)
    {
        if (methInfo.ILCodeSize != 6)
            return false;
        if (*codePtr != CEE_LDSFLD)
            return false;
        _ASSERTE(ILOffsetOfLdSFldInDeadSimpleStaticGetter == 0);
        *offsetOfLd = 0;
        codePtr += 5;
        return (*codePtr == CEE_RET);
    }
    else
    {
        // We handle two forms, one for DBG IL, and one for OPT IL.
        bool dbg = false;
        if (methInfo.ILCodeSize == 0xc)
            dbg = true;
        else if (methInfo.ILCodeSize != 7)
            return false;

        if (dbg)
        {
            if (*codePtr != CEE_NOP)
                return false;
            codePtr += 1;
        }
        if (*codePtr != CEE_LDARG_0)
            return false;
        codePtr += 1;
        if (*codePtr != CEE_LDFLD)
            return false;
        *offsetOfLd = codePtr - methInfo.ILCode;
        _ASSERTE((dbg && ILOffsetOfLdFldInDeadSimpleInstanceGetterDbg == *offsetOfLd)
                 || (!dbg && ILOffsetOfLdFldInDeadSimpleInstanceGetterOpt == *offsetOfLd));
        codePtr += 5;
        if (dbg)
        {
            if (*codePtr != CEE_STLOC_0)
                return false;
            codePtr += 1;
            if (*codePtr != CEE_BR)
                return false;
            if (getU4LittleEndian(codePtr + 1) != 0)
                return false;
            codePtr += 5;
            if (*codePtr != CEE_LDLOC_0)
                return false;
        }
        return (*codePtr == CEE_RET);
    }
}

void Interpreter::DoStringLength()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned ind = m_curStackHt - 1;

#ifdef _DEBUG
    CorInfoType stringCIT = OpStackTypeGet(ind).ToCorInfoType();
    if (stringCIT != CORINFO_TYPE_CLASS)
    {
        VerificationError("StringLength called on non-string.");
    }
#endif // _DEBUG

    Object* obj = OpStackGet<Object*>(ind);

    if (obj == NULL)
    {
        ThrowNullPointerException();
    }

#ifdef _DEBUG
    if (obj->GetMethodTable() != g_pStringClass)
    {
        VerificationError("StringLength called on non-string.");
    }
#endif // _DEBUG

    StringObject* str = reinterpret_cast<StringObject*>(obj);
    INT32 len = str->GetStringLength();
    OpStackSet<INT32>(ind, len);
    OpStackTypeSet(ind, InterpreterType(CORINFO_TYPE_INT));
}

void Interpreter::DoStringGetChar()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt >= 2);
    unsigned strInd = m_curStackHt - 2;
    unsigned indexInd = strInd + 1;

#ifdef _DEBUG
    CorInfoType stringCIT = OpStackTypeGet(strInd).ToCorInfoType();
    if (stringCIT != CORINFO_TYPE_CLASS)
    {
        VerificationError("StringGetChar called on non-string.");
    }
#endif // _DEBUG

    Object* obj = OpStackGet<Object*>(strInd);

    if (obj == NULL)
    {
        ThrowNullPointerException();
    }

#ifdef _DEBUG
    if (obj->GetMethodTable() != g_pStringClass)
    {
        VerificationError("StringGetChar called on non-string.");
    }
#endif // _DEBUG

    StringObject* str = reinterpret_cast<StringObject*>(obj);

#ifdef _DEBUG
    CorInfoType indexCIT = OpStackTypeGet(indexInd).ToCorInfoType();
    if (indexCIT != CORINFO_TYPE_INT)
    {
        VerificationError("StringGetChar needs integer index.");
    }
#endif // _DEBUG

    INT32 ind = OpStackGet<INT32>(indexInd);
    if (ind < 0)
        ThrowArrayBoundsException();
    UINT32 uind = static_cast<UINT32>(ind);
    if (uind >= str->GetStringLength())
        ThrowArrayBoundsException();

    // Otherwise...
    GCX_FORBID(); // str is vulnerable.
    UINT16* dataPtr = reinterpret_cast<UINT16*>(reinterpret_cast<INT8*>(str) + StringObject::GetBufferOffset());
    UINT32 filledChar = dataPtr[ind];
    OpStackSet<UINT32>(strInd, filledChar);
    OpStackTypeSet(strInd, InterpreterType(CORINFO_TYPE_INT));
    m_curStackHt = indexInd;
}

void Interpreter::DoGetTypeFromHandle()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned ind = m_curStackHt - 1;

#ifdef _DEBUG
    CorInfoType handleCIT = OpStackTypeGet(ind).ToCorInfoType();
    if (handleCIT != CORINFO_TYPE_VALUECLASS && handleCIT != CORINFO_TYPE_CLASS)
    {
        VerificationError("HandleGetTypeFromHandle called on non-RuntimeTypeHandle/non-RuntimeType.");
    }
    Object* obj = OpStackGet<Object*>(ind);
    if (obj->GetMethodTable() != g_pRuntimeTypeClass)
    {
        VerificationError("HandleGetTypeFromHandle called on non-RuntimeTypeHandle/non-RuntimeType.");
    }
#endif // _DEBUG

    OpStackTypeSet(ind, InterpreterType(CORINFO_TYPE_CLASS));
}

void Interpreter::DoByReferenceCtor()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // Note 'this' is not passed on the operand stack...
    _ASSERTE(m_curStackHt > 0);
    _ASSERTE(m_callThisArg != NULL);
    unsigned valInd = m_curStackHt - 1;
    CorInfoType valCit = OpStackTypeGet(valInd).ToCorInfoType();

#ifdef _DEBUG
    if (valCit != CORINFO_TYPE_BYREF)
    {
        VerificationError("ByReference<T>.ctor called with non-byref value.");
    }
#endif // _DEBUG

#if INTERP_TRACING
    if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
    {
        fprintf(GetLogFile(), "    ByReference<T>.ctor -- intrinsic\n");
    }
#endif // INTERP_TRACING

    GCX_FORBID();
    void** thisPtr = reinterpret_cast<void**>(m_callThisArg);
    void* val = OpStackGet<void*>(valInd);
    *thisPtr = val;
    m_curStackHt--;
}

void Interpreter::DoByReferenceValue()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(m_curStackHt > 0);
    unsigned slot = m_curStackHt - 1;
    CorInfoType thisCit = OpStackTypeGet(slot).ToCorInfoType();

#ifdef _DEBUG
    if (thisCit != CORINFO_TYPE_BYREF)
    {
        VerificationError("ByReference<T>.get_Value called with non-byref this");
    }
#endif // _DEBUG

#if INTERP_TRACING
    if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
    {
        fprintf(GetLogFile(), "    ByReference<T>.getValue -- intrinsic\n");
    }
#endif // INTERP_TRACING

    GCX_FORBID();
    void** thisPtr = OpStackGet<void**>(slot);
    void* value = *thisPtr;
    OpStackSet<void*>(slot, value);
    OpStackTypeSet(slot, InterpreterType(CORINFO_TYPE_BYREF));
}

void Interpreter::DoSIMDHwAccelerated()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

#if INTERP_TRACING
    if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
    {
        fprintf(GetLogFile(), "    System.Numerics.Vector.IsHardwareAccelerated -- intrinsic\n");
    }
#endif // INTERP_TRACING

    LdIcon(1);
}


void Interpreter::DoGetIsSupported()
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OpStackSet<BOOL>(m_curStackHt, false);
    OpStackTypeSet(m_curStackHt, InterpreterType(CORINFO_TYPE_INT));
    m_curStackHt++;
}

void Interpreter::RecordConstrainedCall()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_Constrained]);
#endif // INTERP_TRACING

    {
        GCX_PREEMP();
        ResolveToken(&m_constrainedResolvedToken, getU4LittleEndian(m_ILCodePtr + 2), CORINFO_TOKENKIND_Constrained InterpTracingArg(RTK_Constrained));
    }

    m_constrainedFlag = true;

    m_ILCodePtr += 6;
}

void Interpreter::LargeStructOperandStackEnsureCanPush(size_t sz)
{
    size_t remaining = m_largeStructOperandStackAllocSize - m_largeStructOperandStackHt;
    if (remaining < sz)
    {
        size_t newAllocSize = max(m_largeStructOperandStackAllocSize + sz * 4, m_largeStructOperandStackAllocSize * 2);
        BYTE* newStack = new BYTE[newAllocSize];
        m_largeStructOperandStackAllocSize = newAllocSize;
        if (m_largeStructOperandStack != NULL)
        {
            memcpy(newStack, m_largeStructOperandStack, m_largeStructOperandStackHt);
            delete[] m_largeStructOperandStack;
        }
        m_largeStructOperandStack = newStack;
    }
}

void* Interpreter::LargeStructOperandStackPush(size_t sz)
{
    LargeStructOperandStackEnsureCanPush(sz);
    _ASSERTE(m_largeStructOperandStackAllocSize >= m_largeStructOperandStackHt + sz);
    void* res = &m_largeStructOperandStack[m_largeStructOperandStackHt];
    m_largeStructOperandStackHt += sz;
    return res;
}

void Interpreter::LargeStructOperandStackPop(size_t sz, void* fromAddr)
{
    if (!IsInLargeStructLocalArea(fromAddr))
    {
        _ASSERTE(m_largeStructOperandStackHt >= sz);
        m_largeStructOperandStackHt -= sz;
    }
}

#ifdef _DEBUG
bool Interpreter::LargeStructStackHeightIsValid()
{
    size_t sz2 = 0;
    for (unsigned k = 0; k < m_curStackHt; k++)
    {
        if (OpStackTypeGet(k).IsLargeStruct(&m_interpCeeInfo) && !IsInLargeStructLocalArea(OpStackGet<void*>(k)))
        {
            sz2 += OpStackTypeGet(k).Size(&m_interpCeeInfo);
        }
    }
    _ASSERTE(sz2 == m_largeStructOperandStackHt);
    return sz2 == m_largeStructOperandStackHt;
}
#endif // _DEBUG

void Interpreter::VerificationError(const char* msg)
{
    // TODO: Should raise an exception eventually; for now:
    const char* const msgPrefix = "Verification Error: ";
    size_t len = strlen(msgPrefix) + strlen(msg) + 1;
    char* msgFinal = (char*)_alloca(len);
    strcpy_s(msgFinal, len, msgPrefix);
    strcat_s(msgFinal, len, msg);
    _ASSERTE_MSG(false, msgFinal);
}

void Interpreter::ThrowDivideByZero()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    COMPlusThrow(kDivideByZeroException);
}

void Interpreter::ThrowSysArithException()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // According to the ECMA spec, this should be an ArithmeticException; however,
    // the JITs throw an OverflowException and consistency is top priority...
    COMPlusThrow(kOverflowException);
}

void Interpreter::ThrowNullPointerException()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    COMPlusThrow(kNullReferenceException);
}

void Interpreter::ThrowOverflowException()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    COMPlusThrow(kOverflowException);
}

void Interpreter::ThrowArrayBoundsException()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    COMPlusThrow(kIndexOutOfRangeException);
}

void Interpreter::ThrowInvalidCastException()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    COMPlusThrow(kInvalidCastException);
}

void Interpreter::ThrowStackOverflow()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    COMPlusThrow(kStackOverflowException);
}

float Interpreter::RemFunc(float v1, float v2)
{
    return fmodf(v1, v2);
}

double Interpreter::RemFunc(double v1, double v2)
{
    return fmod(v1, v2);
}

// Static members and methods.
Interpreter::AddrToMDMap* Interpreter::s_addrToMDMap = NULL;

unsigned Interpreter::s_interpreterStubNum = 0;

// TODO: contracts and synchronization for the AddrToMDMap methods.
// Requires caller to hold "s_interpStubToMDMapLock".
Interpreter::AddrToMDMap* Interpreter::GetAddrToMdMap()
{
#if 0
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;
#endif

    if (s_addrToMDMap == NULL)
    {
        s_addrToMDMap = new AddrToMDMap();
    }
    return s_addrToMDMap;
}

void Interpreter::RecordInterpreterStubForMethodDesc(CORINFO_METHOD_HANDLE md, void* addr)
{
#if 0
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
#endif

    CrstHolder ch(&s_interpStubToMDMapLock);

    AddrToMDMap* map = Interpreter::GetAddrToMdMap();
#ifdef _DEBUG
    CORINFO_METHOD_HANDLE dummy;
    _ASSERTE(!map->Lookup(addr, &dummy));
#endif // DEBUG
    map->AddOrReplace(KeyValuePair<void*,CORINFO_METHOD_HANDLE>(addr, md));
}

MethodDesc* Interpreter::InterpretationStubToMethodInfo(PCODE addr)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;


    // This query function will never allocate the table...
    if (s_addrToMDMap == NULL)
        return NULL;

    // Otherwise...if we observe s_addrToMdMap non-null, the lock below must be initialized.
    // CrstHolder ch(&s_interpStubToMDMapLock);

    AddrToMDMap* map = Interpreter::GetAddrToMdMap();
    CORINFO_METHOD_HANDLE result = NULL;
    (void)map->Lookup((void*)addr, &result);
    return (MethodDesc*)result;
}

Interpreter::MethodHandleToInterpMethInfoPtrMap* Interpreter::s_methodHandleToInterpMethInfoPtrMap = NULL;

// Requires caller to hold "s_interpStubToMDMapLock".
Interpreter::MethodHandleToInterpMethInfoPtrMap* Interpreter::GetMethodHandleToInterpMethInfoPtrMap()
{
#if 0
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;
#endif

    if (s_methodHandleToInterpMethInfoPtrMap == NULL)
    {
        s_methodHandleToInterpMethInfoPtrMap = new MethodHandleToInterpMethInfoPtrMap();
    }
    return s_methodHandleToInterpMethInfoPtrMap;
}

InterpreterMethodInfo* Interpreter::RecordInterpreterMethodInfoForMethodHandle(CORINFO_METHOD_HANDLE md, InterpreterMethodInfo* methInfo)
{
#if 0
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;
#endif

    CrstHolder ch(&s_interpStubToMDMapLock);

    MethodHandleToInterpMethInfoPtrMap* map = Interpreter::GetMethodHandleToInterpMethInfoPtrMap();

    MethInfo mi;
    if (map->Lookup(md, &mi))
    {
        // If there's already an entry, make sure it was created by another thread -- the same thread shouldn't create two
        // of these.
        _ASSERTE_MSG(mi.m_thread != GetThread(), "Two InterpMethInfo's for same meth by same thread.");
        // If we were creating an interpreter stub at the same time as another thread, and we lost the race to
        // insert it, use the already-existing one, and delete this one.
        delete methInfo;
        return mi.m_info;
    }

    mi.m_info = methInfo;
#ifdef _DEBUG
    mi.m_thread = GetThread();
#endif

    _ASSERTE_MSG(map->LookupPtr(md) == NULL, "Multiple InterpMethInfos for method desc.");
    map->Add(md, mi);
    return methInfo;
}

InterpreterMethodInfo* Interpreter::MethodHandleToInterpreterMethInfoPtr(CORINFO_METHOD_HANDLE md)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    // This query function will never allocate the table...
    if (s_methodHandleToInterpMethInfoPtrMap == NULL)
        return NULL;

    // Otherwise...if we observe s_addrToMdMap non-null, the lock below must be initialized.
    CrstHolder ch(&s_interpStubToMDMapLock);

    MethodHandleToInterpMethInfoPtrMap* map = Interpreter::GetMethodHandleToInterpMethInfoPtrMap();

    MethInfo mi;
    mi.m_info = NULL;
    (void)map->Lookup(md, &mi);
    return mi.m_info;
}


#ifndef DACCESS_COMPILE

// Requires that the current thread holds "s_methodCacheLock."
ILOffsetToItemCache* InterpreterMethodInfo::GetCacheForCall(Object* thisArg, void* genericsCtxtArg, bool alloc)
{
    // First, does the current method have dynamic generic information, and, if so,
    // what kind?
    CORINFO_CONTEXT_HANDLE context = GetPreciseGenericsContext(thisArg, genericsCtxtArg);
    if (context == MAKE_METHODCONTEXT(m_method))
    {
        // No dynamic generics context information.  The caching field in "m_methInfo" is the
        // ILoffset->Item cache directly.
        // First, ensure that it's allocated.
        if (m_methodCache == NULL && alloc)
        {
            // Lazy init via compare-exchange.
            ILOffsetToItemCache* cache = new ILOffsetToItemCache();
            void* prev = InterlockedCompareExchangeT<void*>(&m_methodCache, cache, NULL);
            if (prev != NULL) delete cache;
        }
        return reinterpret_cast<ILOffsetToItemCache*>(m_methodCache);
    }
    else
    {
        // Otherwise, it does have generic info, so find the right cache.
        // First ensure that the top-level generics-context --> cache cache exists.
        GenericContextToInnerCache* outerCache = reinterpret_cast<GenericContextToInnerCache*>(m_methodCache);
        if (outerCache == NULL)
        {
            if (alloc)
            {
                // Lazy init via compare-exchange.
                outerCache = new GenericContextToInnerCache();
                void* prev = InterlockedCompareExchangeT<void*>(&m_methodCache, outerCache, NULL);
                if (prev != NULL)
                {
                    delete outerCache;
                    outerCache = reinterpret_cast<GenericContextToInnerCache*>(prev);
                }
            }
            else
            {
                return NULL;
            }
        }
        // Does the outerCache already have an entry for this instantiation?
        ILOffsetToItemCache* innerCache = NULL;
        if (!outerCache->GetItem(size_t(context), innerCache) && alloc)
        {
            innerCache = new ILOffsetToItemCache();
            outerCache->AddItem(size_t(context), innerCache);
        }
        return innerCache;
    }
}

void Interpreter::CacheCallInfo(unsigned iloffset, CallSiteCacheData* callInfo)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(true);
    // Insert, but if the item is already there, delete "mdcs" (which would have been owned
    // by the cache).
    // (Duplicate entries can happen because of recursive calls -- F makes a recursive call to F, and when it
    // returns wants to cache it, but the recursive call makes a furher recursive call, and caches that, so the
    // first call finds the iloffset already occupied.)
    if (!cache->AddItem(iloffset, CachedItem(callInfo)))
    {
        delete callInfo;
    }
}

CallSiteCacheData* Interpreter::GetCachedCallInfo(unsigned iloffset)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(false);
    if (cache == NULL) return NULL;
    // Otherwise...
    CachedItem item;
    if (cache->GetItem(iloffset, item))
    {
        _ASSERTE_MSG(item.m_tag == CIK_CallSite, "Wrong cached item tag.");
        return item.m_value.m_callSiteInfo;
    }
    else
    {
        return NULL;
    }
}

void Interpreter::CacheInstanceField(unsigned iloffset, FieldDesc* fld)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(true);
    cache->AddItem(iloffset, CachedItem(fld));
}

FieldDesc* Interpreter::GetCachedInstanceField(unsigned iloffset)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(false);
    if (cache == NULL) return NULL;
    // Otherwise...
    CachedItem item;
    if (cache->GetItem(iloffset, item))
    {
        _ASSERTE_MSG(item.m_tag == CIK_InstanceField, "Wrong cached item tag.");
        return item.m_value.m_instanceField;
    }
    else
    {
        return NULL;
    }
}

void Interpreter::CacheStaticField(unsigned iloffset, StaticFieldCacheEntry* pEntry)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(true);
    // If (say) a concurrent thread has beaten us to this, delete the entry (which otherwise would have
    // been owned by the cache).
    if (!cache->AddItem(iloffset, CachedItem(pEntry)))
    {
        delete pEntry;
    }
}

StaticFieldCacheEntry* Interpreter::GetCachedStaticField(unsigned iloffset)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(false);
    if (cache == NULL)
        return NULL;

    // Otherwise...
    CachedItem item;
    if (cache->GetItem(iloffset, item))
    {
        _ASSERTE_MSG(item.m_tag == CIK_StaticField, "Wrong cached item tag.");
        return item.m_value.m_staticFieldAddr;
    }
    else
    {
        return NULL;
    }
}


void Interpreter::CacheClassHandle(unsigned iloffset, CORINFO_CLASS_HANDLE clsHnd)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(true);
    cache->AddItem(iloffset, CachedItem(clsHnd));
}

CORINFO_CLASS_HANDLE Interpreter::GetCachedClassHandle(unsigned iloffset)
{
    CrstHolder ch(&s_methodCacheLock);

    ILOffsetToItemCache* cache = GetThisExecCache(false);
    if (cache == NULL)
        return NULL;

    // Otherwise...
    CachedItem item;
    if (cache->GetItem(iloffset, item))
    {
        _ASSERTE_MSG(item.m_tag == CIK_ClassHandle, "Wrong cached item tag.");
        return item.m_value.m_clsHnd;
    }
    else
    {
        return NULL;
    }
}
#endif // DACCESS_COMPILE

// Statics

// Theses are not debug-only.
ConfigMethodSet Interpreter::s_InterpretMeths;
ConfigMethodSet Interpreter::s_InterpretMethsExclude;
ConfigDWORD Interpreter::s_InterpretMethHashMin;
ConfigDWORD Interpreter::s_InterpretMethHashMax;
ConfigDWORD Interpreter::s_InterpreterJITThreshold;
ConfigDWORD Interpreter::s_InterpreterDoLoopMethodsFlag;
ConfigDWORD Interpreter::s_InterpreterUseCachingFlag;
ConfigDWORD Interpreter::s_InterpreterLooseRulesFlag;

bool Interpreter::s_InterpreterDoLoopMethods;
bool Interpreter::s_InterpreterUseCaching;
bool Interpreter::s_InterpreterLooseRules;

CrstExplicitInit Interpreter::s_methodCacheLock;
CrstExplicitInit Interpreter::s_interpStubToMDMapLock;

// The static variables below are debug-only.
#if INTERP_TRACING
LONG Interpreter::s_totalInvocations = 0;
LONG Interpreter::s_totalInterpCalls = 0;
LONG Interpreter::s_totalInterpCallsToGetters = 0;
LONG Interpreter::s_totalInterpCallsToDeadSimpleGetters = 0;
LONG Interpreter::s_totalInterpCallsToDeadSimpleGettersShortCircuited = 0;
LONG Interpreter::s_totalInterpCallsToSetters = 0;
LONG Interpreter::s_totalInterpCallsToIntrinsics = 0;
LONG Interpreter::s_totalInterpCallsToIntrinsicsUnhandled = 0;

LONG Interpreter::s_tokenResolutionOpportunities[RTK_Count] = {0, };
LONG Interpreter::s_tokenResolutionCalls[RTK_Count] = {0, };
const char* Interpreter::s_tokenResolutionKindNames[RTK_Count] =
{
    "Undefined",
    "Constrained",
    "NewObj",
    "NewArr",
    "LdToken",
    "LdFtn",
    "LdVirtFtn",
    "SFldAddr",
    "LdElem",
    "Call",
    "LdObj",
    "StObj",
    "CpObj",
    "InitObj",
    "IsInst",
    "CastClass",
    "MkRefAny",
    "RefAnyVal",
    "Sizeof",
    "StElem",
    "Box",
    "Unbox",
    "UnboxAny",
    "LdFld",
    "LdFldA",
    "StFld",
    "FindClass",
    "Exception",
};

FILE*       Interpreter::s_InterpreterLogFile = NULL;
ConfigDWORD Interpreter::s_DumpInterpreterStubsFlag;
ConfigDWORD Interpreter::s_TraceInterpreterEntriesFlag;
ConfigDWORD Interpreter::s_TraceInterpreterILFlag;
ConfigDWORD Interpreter::s_TraceInterpreterOstackFlag;
ConfigDWORD Interpreter::s_TraceInterpreterVerboseFlag;
ConfigDWORD Interpreter::s_TraceInterpreterJITTransitionFlag;
ConfigDWORD Interpreter::s_InterpreterStubMin;
ConfigDWORD Interpreter::s_InterpreterStubMax;
#endif // INTERP_TRACING

#if INTERP_ILINSTR_PROFILE
unsigned short Interpreter::s_ILInstrCategories[512];

int        Interpreter::s_ILInstrExecs[256] = {0, };
int        Interpreter::s_ILInstrExecsByCategory[512] = {0, };
int        Interpreter::s_ILInstr2ByteExecs[Interpreter::CountIlInstr2Byte] = {0, };
#if INTERP_ILCYCLE_PROFILE
unsigned __int64       Interpreter::s_ILInstrCycles[512] = { 0, };
unsigned __int64       Interpreter::s_ILInstrCyclesByCategory[512] = { 0, };
// XXX
unsigned __int64       Interpreter::s_callCycles = 0;
unsigned               Interpreter::s_calls = 0;

void Interpreter::UpdateCycleCount()
{
    unsigned __int64 endCycles;
    bool b = CycleTimer::GetThreadCyclesS(&endCycles); _ASSERTE(b);
    if (m_instr != CEE_COUNT)
    {
        unsigned __int64 delta = (endCycles - m_startCycles);
        if (m_exemptCycles > 0)
        {
            delta = delta - m_exemptCycles;
            m_exemptCycles = 0;
        }
        CycleTimer::InterlockedAddU64(&s_ILInstrCycles[m_instr], delta);
    }
    // In any case, set the instruction to the current one, and record it's start time.
    m_instr = (*m_ILCodePtr);
    if (m_instr == CEE_PREFIX1) {
        m_instr = *(m_ILCodePtr + 1) + 0x100;
    }
    b = CycleTimer::GetThreadCyclesS(&m_startCycles); _ASSERTE(b);
}

#endif // INTERP_ILCYCLE_PROFILE
#endif // INTERP_ILINSTR_PROFILE

#ifdef _DEBUG
InterpreterMethodInfo** Interpreter::s_interpMethInfos = NULL;
unsigned Interpreter::s_interpMethInfosAllocSize = 0;
unsigned Interpreter::s_interpMethInfosCount = 0;

bool Interpreter::TOSIsPtr()
{
    if (m_curStackHt == 0)
        return false;

    return CorInfoTypeIsPointer(OpStackTypeGet(m_curStackHt - 1).ToCorInfoType());
}
#endif // DEBUG

ConfigDWORD Interpreter::s_PrintPostMortemFlag;

// InterpreterCache.
template<typename Key, typename Val>
InterpreterCache<Key,Val>::InterpreterCache() : m_pairs(NULL), m_allocSize(0), m_count(0)
{
#ifdef _DEBUG
    AddAllocBytes(sizeof(*this));
#endif
}

#ifdef _DEBUG
// static
static unsigned InterpreterCacheAllocBytes = 0;
const unsigned KBYTE = 1024;
const unsigned MBYTE = KBYTE*KBYTE;
const unsigned InterpreterCacheAllocBytesIncrement = 16*KBYTE;
static unsigned InterpreterCacheAllocBytesNextTarget = InterpreterCacheAllocBytesIncrement;

template<typename Key, typename Val>
void InterpreterCache<Key,Val>::AddAllocBytes(unsigned bytes)
{
    // Reinstate this code if you want to track bytes attributable to caching.
#if 0
    InterpreterCacheAllocBytes += bytes;
    if (InterpreterCacheAllocBytes > InterpreterCacheAllocBytesNextTarget)
    {
        printf("Total cache alloc = %d bytes.\n", InterpreterCacheAllocBytes);
        fflush(stdout);
        InterpreterCacheAllocBytesNextTarget += InterpreterCacheAllocBytesIncrement;
    }
#endif
}
#endif // _DEBUG

template<typename Key, typename Val>
void InterpreterCache<Key,Val>::EnsureCanInsert()
{
    if (m_count < m_allocSize)
        return;

    // Otherwise, must make room.
    if (m_allocSize == 0)
    {
        _ASSERTE(m_count == 0);
        m_pairs = new KeyValPair[InitSize];
        m_allocSize = InitSize;
#ifdef _DEBUG
        AddAllocBytes(m_allocSize * sizeof(KeyValPair));
#endif
    }
    else
    {
        unsigned short newSize = min(m_allocSize * 2, USHRT_MAX);

        KeyValPair* newPairs = new KeyValPair[newSize];
        memcpy(newPairs, m_pairs, m_count * sizeof(KeyValPair));
        delete[] m_pairs;
        m_pairs = newPairs;
#ifdef _DEBUG
        AddAllocBytes((newSize - m_allocSize) * sizeof(KeyValPair));
#endif
        m_allocSize = newSize;
    }
}

template<typename Key, typename Val>
bool InterpreterCache<Key,Val>::AddItem(Key key, Val val)
{
    EnsureCanInsert();
    // Find the index to insert before.
    unsigned firstGreaterOrEqual = 0;
    for (; firstGreaterOrEqual < m_count; firstGreaterOrEqual++)
    {
        if (m_pairs[firstGreaterOrEqual].m_key >= key)
            break;
    }
    if (firstGreaterOrEqual < m_count && m_pairs[firstGreaterOrEqual].m_key == key)
    {
        _ASSERTE(m_pairs[firstGreaterOrEqual].m_val == val);
        return false;
    }
    // Move everything starting at firstGreater up one index (if necessary)
    if (m_count > 0)
    {
        for (unsigned k = m_count-1; k >= firstGreaterOrEqual; k--)
        {
            m_pairs[k + 1] = m_pairs[k];
            if (k == 0)
                break;
        }
    }
    // Now we can insert the new element.
    m_pairs[firstGreaterOrEqual].m_key = key;
    m_pairs[firstGreaterOrEqual].m_val = val;
    m_count++;
    return true;
}

template<typename Key, typename Val>
bool InterpreterCache<Key,Val>::GetItem(Key key, Val& v)
{
    unsigned lo = 0;
    unsigned hi = m_count;
    // Invariant: we've determined that the pair for "iloffset", if present,
    // is in the index interval [lo, hi).
    while (lo < hi)
    {
        unsigned mid = (hi + lo)/2;
        Key midKey = m_pairs[mid].m_key;
        if (key == midKey)
        {
            v = m_pairs[mid].m_val;
            return true;
        }
        else if (key < midKey)
        {
            hi = mid;
        }
        else
        {
            _ASSERTE(key > midKey);
            lo = mid + 1;
        }
    }
    // If we reach here without returning, it's not here.
    return false;
}

// TODO: add a header comment here describing this function.
void Interpreter::OpStackNormalize()
{
    size_t largeStructStackOffset = 0;
    // Yes, I've written a quadratic algorithm here.  I don't think it will matter in practice.
    for (unsigned i = 0; i < m_curStackHt; i++)
    {
        InterpreterType tp = OpStackTypeGet(i);
        if (tp.IsLargeStruct(&m_interpCeeInfo))
        {
            size_t sz = tp.Size(&m_interpCeeInfo);

            void* addr = OpStackGet<void*>(i);
            if (IsInLargeStructLocalArea(addr))
            {
                // We're going to allocate space at the top for the new value, then copy everything above the current slot
                // up into that new space, then copy the value into the vacated space.
                // How much will we have to copy?
                size_t toCopy = m_largeStructOperandStackHt - largeStructStackOffset;

                // Allocate space for the new value.
                void* dummy = LargeStructOperandStackPush(sz);

                // Remember where we're going to write to.
                BYTE* fromAddr = m_largeStructOperandStack + largeStructStackOffset;
                BYTE* toAddr = fromAddr + sz;
                memcpy(toAddr, fromAddr, toCopy);

                // Now copy the local variable value.
                memcpy(fromAddr, addr, sz);
                OpStackSet<void*>(i, fromAddr);
            }
            largeStructStackOffset += sz;
        }
    }
    // When we've normalized the stack, it contains no pointers to locals.
    m_orOfPushedInterpreterTypes = 0;
}

#if INTERP_TRACING

// Code copied from eeinterface.cpp in "compiler".  Should be common...

static const char* CorInfoTypeNames[] = {
    "undef",
    "void",
    "bool",
    "char",
    "byte",
    "ubyte",
    "short",
    "ushort",
    "int",
    "uint",
    "long",
    "ulong",
    "nativeint",
    "nativeuint",
    "float",
    "double",
    "string",
    "ptr",
    "byref",
    "valueclass",
    "class",
    "refany",
    "var"
};

const char* eeGetMethodFullName(CEEInfo* info, CORINFO_METHOD_HANDLE hnd, const char** clsName)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    const char* returnType = NULL;

    const char* className;
    const char* methodName = info->getMethodName(hnd, &className);
    if (clsName != NULL)
    {
        *clsName = className;
    }

    size_t length = 0;
    unsigned i;

    /* Generating the full signature is a two-pass process. First we have to walk
       the components in order to assess the total size, then we allocate the buffer
       and copy the elements into it.
     */

    /* Right now there is a race-condition in the EE, className can be NULL */

    /* initialize length with length of className and '.' */

    if (className)
    {
        length = strlen(className) + 1;
    }
    else
    {
        _ASSERTE(strlen("<NULL>.") == 7);
        length = 7;
    }

    /* add length of methodName and opening bracket */
    length += strlen(methodName) + 1;

    CORINFO_SIG_INFO sig;
    info->getMethodSig(hnd, &sig, nullptr);
    CORINFO_ARG_LIST_HANDLE argLst = sig.args;

    CORINFO_CLASS_HANDLE dummyCls;
    for (i = 0; i < sig.numArgs; i++)
    {
        CorInfoType type = strip(info->getArgType(&sig, argLst, &dummyCls));

        length += strlen(CorInfoTypeNames[type]);
        argLst = info->getArgNext(argLst);
    }

    /* add ',' if there is more than one argument */

    if (sig.numArgs > 1)
    {
        length += (sig.numArgs - 1);
    }

    if (sig.retType != CORINFO_TYPE_VOID)
    {
        returnType = CorInfoTypeNames[sig.retType];
        length += strlen(returnType) + 1; // don't forget the delimiter ':'
    }

    /* add closing bracket and null terminator */

    length += 2;

    char* retName = new char[length];

    /* Now generate the full signature string in the allocated buffer */

    if (className)
    {
        strcpy_s(retName, length, className);
        strcat_s(retName, length, ":");
    }
    else
    {
        strcpy_s(retName, length, "<NULL>.");
    }

    strcat_s(retName, length, methodName);

    // append the signature
    strcat_s(retName, length, "(");

    argLst = sig.args;

    for (i = 0; i < sig.numArgs; i++)
    {
        CorInfoType type = strip(info->getArgType(&sig, argLst, &dummyCls));
        strcat_s(retName, length, CorInfoTypeNames[type]);

        argLst = info->getArgNext(argLst);
        if (i + 1 < sig.numArgs)
        {
            strcat_s(retName, length, ",");
        }
    }

    strcat_s(retName, length, ")");

    if (returnType)
    {
        strcat_s(retName, length, ":");
        strcat_s(retName, length, returnType);
    }

    _ASSERTE(strlen(retName) == length - 1);

    return(retName);
}

const char* Interpreter::eeGetMethodFullName(CORINFO_METHOD_HANDLE hnd)
{
    return ::eeGetMethodFullName(&m_interpCeeInfo, hnd);
}

const char* ILOpNames[256*2];
bool ILOpNamesInited = false;

void InitILOpNames()
{
    if (!ILOpNamesInited)
    {
        // Initialize the array.
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) if (s1 == 0xfe || s1 == 0xff) { int ind ((unsigned(s1) << 8) + unsigned(s2)); ind -= 0xfe00; ILOpNames[ind] = s; }
#include "opcode.def"
#undef OPDEF
        ILOpNamesInited = true;
    }
};
const char* Interpreter::ILOp(BYTE* m_ILCodePtr)
{
    InitILOpNames();
    BYTE b = *m_ILCodePtr;
    if (b == 0xfe)
    {
        return ILOpNames[*(m_ILCodePtr + 1)];
    }
    else
    {
        return ILOpNames[(0x1 << 8) + b];
    }
}
const char* Interpreter::ILOp1Byte(unsigned short ilInstrVal)
{
    InitILOpNames();
    return ILOpNames[(0x1 << 8) + ilInstrVal];
}
const char* Interpreter::ILOp2Byte(unsigned short ilInstrVal)
{
    InitILOpNames();
    return ILOpNames[ilInstrVal];
}

void Interpreter::PrintOStack()
{
    if (m_curStackHt == 0)
    {
        fprintf(GetLogFile(), "      <empty>\n");
    }
    else
    {
        for (unsigned k = 0; k < m_curStackHt; k++)
        {
            CorInfoType cit = OpStackTypeGet(k).ToCorInfoType();
            _ASSERTE(IsStackNormalType(cit));
            fprintf(GetLogFile(), "      %4d: %10s: ", k, CorInfoTypeNames[cit]);
            PrintOStackValue(k);
            fprintf(GetLogFile(), "\n");
        }
    }
    fflush(GetLogFile());
}

void Interpreter::PrintOStackValue(unsigned index)
{
    _ASSERTE_MSG(index < m_curStackHt, "precondition");
    InterpreterType it = OpStackTypeGet(index);
    if (it.IsLargeStruct(&m_interpCeeInfo))
    {
        PrintValue(it, OpStackGet<BYTE*>(index));
    }
    else
    {
        PrintValue(it, reinterpret_cast<BYTE*>(OpStackGetAddr(index, it.Size(&m_interpCeeInfo))));
    }
}

void Interpreter::PrintLocals()
{
    if (m_methInfo->m_numLocals == 0)
    {
        fprintf(GetLogFile(), "      <no locals>\n");
    }
    else
    {
        for (unsigned i = 0; i < m_methInfo->m_numLocals; i++)
        {
            InterpreterType it = m_methInfo->m_localDescs[i].m_type;
            CorInfoType cit = it.ToCorInfoType();
            void* localPtr = NULL;
            if (it.IsLargeStruct(&m_interpCeeInfo))
            {
                void* structPtr = ArgSlotEndianessFixup(reinterpret_cast<ARG_SLOT*>(FixedSizeLocalSlot(i)), sizeof(void**));
                localPtr = *reinterpret_cast<void**>(structPtr);
            }
            else
            {
                localPtr = ArgSlotEndianessFixup(reinterpret_cast<ARG_SLOT*>(FixedSizeLocalSlot(i)), it.Size(&m_interpCeeInfo));
            }
            fprintf(GetLogFile(), "      loc%-4d: %10s: ", i, CorInfoTypeNames[cit]);
            PrintValue(it, reinterpret_cast<BYTE*>(localPtr));
            fprintf(GetLogFile(), "\n");
        }
    }
    fflush(GetLogFile());
}

void Interpreter::PrintArgs()
{
    for (unsigned k = 0; k < m_methInfo->m_numArgs; k++)
    {
        CorInfoType cit = GetArgType(k).ToCorInfoType();
        fprintf(GetLogFile(), "      %4d: %10s: ", k, CorInfoTypeNames[cit]);
        PrintArgValue(k);
        fprintf(GetLogFile(), "\n");
    }
    fprintf(GetLogFile(), "\n");
    fflush(GetLogFile());
}

void Interpreter::PrintArgValue(unsigned argNum)
{
    _ASSERTE_MSG(argNum < m_methInfo->m_numArgs, "precondition");
    InterpreterType it = GetArgType(argNum);
    PrintValue(it, GetArgAddr(argNum));
}

// Note that this is used to print non-stack-normal values, so
// it must handle all cases.
void Interpreter::PrintValue(InterpreterType it, BYTE* valAddr)
{
    switch (it.ToCorInfoType())
    {
    case CORINFO_TYPE_BOOL:
        fprintf(GetLogFile(), "%s", ((*reinterpret_cast<INT8*>(valAddr)) ? "true" : "false"));
        break;
    case CORINFO_TYPE_BYTE:
        fprintf(GetLogFile(), "%d", *reinterpret_cast<INT8*>(valAddr));
        break;
    case CORINFO_TYPE_UBYTE:
        fprintf(GetLogFile(), "%u", *reinterpret_cast<UINT8*>(valAddr));
        break;

    case CORINFO_TYPE_SHORT:
        fprintf(GetLogFile(), "%d", *reinterpret_cast<INT16*>(valAddr));
        break;
    case CORINFO_TYPE_USHORT: case CORINFO_TYPE_CHAR:
        fprintf(GetLogFile(), "%u", *reinterpret_cast<UINT16*>(valAddr));
        break;

    case CORINFO_TYPE_INT:
        fprintf(GetLogFile(), "%d", *reinterpret_cast<INT32*>(valAddr));
        break;
    case CORINFO_TYPE_UINT:
        fprintf(GetLogFile(), "%u", *reinterpret_cast<UINT32*>(valAddr));
        break;

    case CORINFO_TYPE_NATIVEINT:
        {
            INT64 val = static_cast<INT64>(*reinterpret_cast<NativeInt*>(valAddr));
            fprintf(GetLogFile(), "%lld (= 0x%llx)", val, val);
        }
        break;
    case CORINFO_TYPE_NATIVEUINT:
        {
            UINT64 val = static_cast<UINT64>(*reinterpret_cast<NativeUInt*>(valAddr));
            fprintf(GetLogFile(), "%lld (= 0x%llx)", val, val);
        }
        break;

    case CORINFO_TYPE_BYREF:
        fprintf(GetLogFile(), "0x%p", *reinterpret_cast<void**>(valAddr));
        break;

    case CORINFO_TYPE_LONG:
        {
            INT64 val = *reinterpret_cast<INT64*>(valAddr);
            fprintf(GetLogFile(), "%lld (= 0x%llx)", val, val);
        }
        break;
    case CORINFO_TYPE_ULONG:
        fprintf(GetLogFile(), "%lld", *reinterpret_cast<UINT64*>(valAddr));
        break;

    case CORINFO_TYPE_CLASS:
        {
            Object* obj = *reinterpret_cast<Object**>(valAddr);
            if (obj == NULL)
            {
                fprintf(GetLogFile(), "null");
            }
            else
            {
#ifdef _DEBUG
                fprintf(GetLogFile(), "0x%p (%s) [", obj, obj->GetMethodTable()->GetDebugClassName());
#else
                fprintf(GetLogFile(), "0x%p (MT=0x%p) [", obj, obj->GetMethodTable());
#endif
                unsigned sz = obj->GetMethodTable()->GetBaseSize();
                BYTE* objBytes = reinterpret_cast<BYTE*>(obj);
                for (unsigned i = 0; i < sz; i++)
                {
                    if (i > 0)
                    {
                        fprintf(GetLogFile(), " ");
                    }
                    fprintf(GetLogFile(), "0x%x", objBytes[i]);
                }
                fprintf(GetLogFile(), "]");
            }
        }
        break;
    case CORINFO_TYPE_VALUECLASS:
        {
            GCX_PREEMP();
            fprintf(GetLogFile(), "<%s>: [", m_interpCeeInfo.getClassName(it.ToClassHandle()));
            unsigned sz = getClassSize(it.ToClassHandle());
            for (unsigned i = 0; i < sz; i++)
            {
                if (i > 0)
                {
                    fprintf(GetLogFile(), " ");
                }
                fprintf(GetLogFile(), "0x%02x", valAddr[i]);
            }
            fprintf(GetLogFile(), "]");
        }
        break;
    case CORINFO_TYPE_REFANY:
        fprintf(GetLogFile(), "<refany>");
        break;
    case CORINFO_TYPE_FLOAT:
        fprintf(GetLogFile(), "%f", *reinterpret_cast<float*>(valAddr));
        break;
    case CORINFO_TYPE_DOUBLE:
        fprintf(GetLogFile(), "%g", *reinterpret_cast<double*>(valAddr));
        break;
    case CORINFO_TYPE_PTR:
        fprintf(GetLogFile(), "0x%p", *reinterpret_cast<void**>(valAddr));
        break;
    default:
        _ASSERTE_MSG(false, "Unknown type in PrintValue.");
        break;
    }
}
#endif // INTERP_TRACING

#ifdef _DEBUG
void Interpreter::AddInterpMethInfo(InterpreterMethodInfo* methInfo)
{
    typedef InterpreterMethodInfo* InterpreterMethodInfoPtr;
    // TODO: this requires synchronization.
    const unsigned InitSize = 128;
    if (s_interpMethInfos == NULL)
    {
        s_interpMethInfos = new InterpreterMethodInfoPtr[InitSize];
        s_interpMethInfosAllocSize = InitSize;
    }
    if (s_interpMethInfosAllocSize == s_interpMethInfosCount)
    {
        unsigned newSize = s_interpMethInfosAllocSize * 2;
        InterpreterMethodInfoPtr* tmp = new InterpreterMethodInfoPtr[newSize];
        memcpy(tmp, s_interpMethInfos, s_interpMethInfosCount * sizeof(InterpreterMethodInfoPtr));
        delete[] s_interpMethInfos;
        s_interpMethInfos = tmp;
        s_interpMethInfosAllocSize = newSize;
    }
    s_interpMethInfos[s_interpMethInfosCount] = methInfo;
    s_interpMethInfosCount++;
}

int _cdecl Interpreter::CompareMethInfosByInvocations(const void* mi0in, const void* mi1in)
{
    const InterpreterMethodInfo* mi0 = *((const InterpreterMethodInfo**)mi0in);
    const InterpreterMethodInfo* mi1 = *((const InterpreterMethodInfo**)mi1in);
    if (mi0->m_invocations < mi1->m_invocations)
    {
        return -1;
    }
    else if (mi0->m_invocations == mi1->m_invocations)
    {
        return 0;
    }
    else
    {
        _ASSERTE(mi0->m_invocations > mi1->m_invocations);
        return 1;
    }
}

#if INTERP_PROFILE
int _cdecl Interpreter::CompareMethInfosByILInstrs(const void* mi0in, const void* mi1in)
{
    const InterpreterMethodInfo* mi0 = *((const InterpreterMethodInfo**)mi0in);
    const InterpreterMethodInfo* mi1 = *((const InterpreterMethodInfo**)mi1in);
    if (mi0->m_totIlInstructionsExeced < mi1->m_totIlInstructionsExeced) return 1;
    else if (mi0->m_totIlInstructionsExeced == mi1->m_totIlInstructionsExeced) return 0;
    else
    {
        _ASSERTE(mi0->m_totIlInstructionsExeced > mi1->m_totIlInstructionsExeced);
        return -1;
    }
}
#endif // INTERP_PROFILE
#endif // _DEBUG

const int MIL = 1000000;

// Leaving this disabled for now.
#if 0
unsigned __int64 ForceSigWalkCycles = 0;
#endif

void Interpreter::PrintPostMortemData()
{
    if (s_PrintPostMortemFlag.val(CLRConfig::INTERNAL_InterpreterPrintPostMortem) == 0)
        return;

    // Otherwise...

#if INTERP_TRACING
    // Let's print two things: the number of methods that are 0-10, or more, and
    // For each 10% of methods, cumulative % of invocations they represent.  By 1% for last 10%.

    // First one doesn't require any sorting.
    const unsigned HistoMax = 11;
    unsigned histo[HistoMax];
    unsigned numExecs[HistoMax];
    for (unsigned k = 0; k < HistoMax; k++)
    {
        histo[k] = 0; numExecs[k] = 0;
    }
    for (unsigned k = 0; k < s_interpMethInfosCount; k++)
    {
        unsigned invokes = s_interpMethInfos[k]->m_invocations;
        if (invokes > HistoMax - 1)
        {
            invokes = HistoMax - 1;
        }
        histo[invokes]++;
        numExecs[invokes] += s_interpMethInfos[k]->m_invocations;
    }

    fprintf(GetLogFile(), "Histogram of method executions:\n");
    fprintf(GetLogFile(), "   # of execs   |   # meths (%%)    |   cum %% | %% cum execs\n");
    fprintf(GetLogFile(), "   -------------------------------------------------------\n");
    float fTotMeths = float(s_interpMethInfosCount);
    float fTotExecs = float(s_totalInvocations);
    float numPct = 0.0f;
    float numExecPct = 0.0f;
    for (unsigned k = 0; k < HistoMax; k++)
    {
        fprintf(GetLogFile(), "   %10d", k);
        if (k == HistoMax)
        {
            fprintf(GetLogFile(), "+  ");
        }
        else
        {
            fprintf(GetLogFile(), "   ");
        }
        float pct = float(histo[k])*100.0f/fTotMeths;
        numPct += pct;
        float execPct = float(numExecs[k])*100.0f/fTotExecs;
        numExecPct += execPct;
        fprintf(GetLogFile(), "| %7d (%5.2f%%) | %6.2f%% | %6.2f%%\n", histo[k], pct, numPct, numExecPct);
    }

    // This sorts them in ascending order of number of invocations.
    qsort(&s_interpMethInfos[0], s_interpMethInfosCount, sizeof(InterpreterMethodInfo*), &CompareMethInfosByInvocations);

    fprintf(GetLogFile(), "\nFor methods sorted in ascending # of executions order, cumulative %% of executions:\n");
    if (s_totalInvocations > 0)
    {
        fprintf(GetLogFile(), "   %% of methods  | max execs | cum %% of execs\n");
        fprintf(GetLogFile(), "   ------------------------------------------\n");
        unsigned methNum = 0;
        unsigned nNumExecs = 0;
        float totExecsF = float(s_totalInvocations);
        for (unsigned k = 10; k < 100; k += 10)
        {
            unsigned targ = unsigned((float(k)/100.0f)*float(s_interpMethInfosCount));
            unsigned targLess1 = (targ > 0 ? targ - 1 : 0);
            while (methNum < targ)
            {
                nNumExecs += s_interpMethInfos[methNum]->m_invocations;
                methNum++;
            }
            float pctExecs = float(nNumExecs) * 100.0f / totExecsF;

            fprintf(GetLogFile(), "   %8d%%     | %9d |    %8.2f%%\n", k, s_interpMethInfos[targLess1]->m_invocations, pctExecs);

            if (k == 90)
            {
                k++;
                for (; k < 100; k++)
                {
                    unsigned targ = unsigned((float(k)/100.0f)*float(s_interpMethInfosCount));
                    while (methNum < targ)
                    {
                        nNumExecs += s_interpMethInfos[methNum]->m_invocations;
                        methNum++;
                    }
                    pctExecs = float(nNumExecs) * 100.0f / totExecsF;

                    fprintf(GetLogFile(), "     %8d%%   | %9d |    %8.2f%%\n", k, s_interpMethInfos[targLess1]->m_invocations, pctExecs);
                }

                // Now do 100%.
                targ = s_interpMethInfosCount;
                while (methNum < targ)
                {
                    nNumExecs += s_interpMethInfos[methNum]->m_invocations;
                    methNum++;
                }
                pctExecs = float(nNumExecs) * 100.0f / totExecsF;
                fprintf(GetLogFile(), "   %8d%%     | %9d |    %8.2f%%\n", k, s_interpMethInfos[targLess1]->m_invocations, pctExecs);
            }
        }
    }

    fprintf(GetLogFile(), "\nTotal number of calls from interpreted code: %d.\n", s_totalInterpCalls);
    fprintf(GetLogFile(), "    Also, %d are intrinsics; %d of these are not currently handled intrinsically.\n",
        s_totalInterpCallsToIntrinsics, s_totalInterpCallsToIntrinsicsUnhandled);
    fprintf(GetLogFile(), "    Of these, %d to potential property getters (%d of these dead simple), %d to setters.\n",
        s_totalInterpCallsToGetters, s_totalInterpCallsToDeadSimpleGetters, s_totalInterpCallsToSetters);
    fprintf(GetLogFile(), "    Of the dead simple getter calls, %d have been short-circuited.\n",
        s_totalInterpCallsToDeadSimpleGettersShortCircuited);

    fprintf(GetLogFile(), "\nToken resolutions by category:\n");
    fprintf(GetLogFile(), "Category     |  opportunities  |   calls   |      %%\n");
    fprintf(GetLogFile(), "---------------------------------------------------\n");
    for (unsigned i = RTK_Undefined; i < RTK_Count; i++)
    {
        float pct = 0.0;
        if (s_tokenResolutionOpportunities[i] > 0)
            pct = 100.0f * float(s_tokenResolutionCalls[i]) / float(s_tokenResolutionOpportunities[i]);
        fprintf(GetLogFile(), "%12s | %15d | %9d | %6.2f%%\n",
            s_tokenResolutionKindNames[i], s_tokenResolutionOpportunities[i], s_tokenResolutionCalls[i], pct);
    }

#if INTERP_PROFILE
    fprintf(GetLogFile(), "Information on num of execs:\n");

    UINT64 totILInstrs = 0;
    for (unsigned i = 0; i < s_interpMethInfosCount; i++) totILInstrs += s_interpMethInfos[i]->m_totIlInstructionsExeced;

    float totILInstrsF = float(totILInstrs);

    fprintf(GetLogFile(), "\nTotal instructions = %lld.\n", totILInstrs);
    fprintf(GetLogFile(), "\nTop <=10 methods by # of IL instructions executed.\n");
    fprintf(GetLogFile(), "%10s | %9s | %10s | %10s | %8s | %s\n", "tot execs", "# invokes", "code size", "ratio", "% of tot", "Method");
    fprintf(GetLogFile(), "----------------------------------------------------------------------------\n");

    qsort(&s_interpMethInfos[0], s_interpMethInfosCount, sizeof(InterpreterMethodInfo*), &CompareMethInfosByILInstrs);

    for (unsigned i = 0; i < min(10, s_interpMethInfosCount); i++)
    {
        unsigned ilCodeSize = unsigned(s_interpMethInfos[i]->m_ILCodeEnd - s_interpMethInfos[i]->m_ILCode);
        fprintf(GetLogFile(), "%10lld | %9d | %10d | %10.2f | %8.2f%% | %s:%s\n",
            s_interpMethInfos[i]->m_totIlInstructionsExeced,
            s_interpMethInfos[i]->m_invocations,
            ilCodeSize,
            float(s_interpMethInfos[i]->m_totIlInstructionsExeced) / float(ilCodeSize),
            float(s_interpMethInfos[i]->m_totIlInstructionsExeced) * 100.0f / totILInstrsF,
            s_interpMethInfos[i]->m_clsName,
            s_interpMethInfos[i]->m_methName);
    }
#endif // INTERP_PROFILE
#endif // _DEBUG

#if INTERP_ILINSTR_PROFILE
    fprintf(GetLogFile(), "\nIL instruction profiling:\n");
    // First, classify by categories.
    unsigned totInstrs = 0;
#if INTERP_ILCYCLE_PROFILE
    unsigned __int64 totCycles = 0;
    unsigned __int64 perMeasurementOverhead = CycleTimer::QueryOverhead();
#endif // INTERP_ILCYCLE_PROFILE
    for (unsigned i = 0; i < 256; i++)
    {
        s_ILInstrExecsByCategory[s_ILInstrCategories[i]] += s_ILInstrExecs[i];
        totInstrs += s_ILInstrExecs[i];
#if INTERP_ILCYCLE_PROFILE
        unsigned __int64 cycles = s_ILInstrCycles[i];
        if (cycles > s_ILInstrExecs[i] * perMeasurementOverhead) cycles -= s_ILInstrExecs[i] * perMeasurementOverhead;
        else cycles = 0;
        s_ILInstrCycles[i] = cycles;
        s_ILInstrCyclesByCategory[s_ILInstrCategories[i]] += cycles;
        totCycles += cycles;
#endif // INTERP_ILCYCLE_PROFILE
    }
    unsigned totInstrs2Byte = 0;
#if INTERP_ILCYCLE_PROFILE
    unsigned __int64 totCycles2Byte = 0;
#endif // INTERP_ILCYCLE_PROFILE
    for (unsigned i = 0; i < CountIlInstr2Byte; i++)
    {
        unsigned ind = 0x100 + i;
        s_ILInstrExecsByCategory[s_ILInstrCategories[ind]] += s_ILInstr2ByteExecs[i];
        totInstrs += s_ILInstr2ByteExecs[i];
        totInstrs2Byte +=  s_ILInstr2ByteExecs[i];
#if INTERP_ILCYCLE_PROFILE
        unsigned __int64 cycles = s_ILInstrCycles[ind];
        if (cycles > s_ILInstrExecs[ind] * perMeasurementOverhead) cycles -= s_ILInstrExecs[ind] * perMeasurementOverhead;
        else cycles = 0;
        s_ILInstrCycles[i] = cycles;
        s_ILInstrCyclesByCategory[s_ILInstrCategories[ind]] += cycles;
        totCycles += cycles;
        totCycles2Byte +=  cycles;
#endif // INTERP_ILCYCLE_PROFILE
    }

    // Now sort the categories by # of occurrences.

    InstrExecRecord ieps[256 + CountIlInstr2Byte];
    for (unsigned short i = 0; i < 256; i++)
    {
        ieps[i].m_instr = i; ieps[i].m_is2byte = false; ieps[i].m_execs = s_ILInstrExecs[i];
#if INTERP_ILCYCLE_PROFILE
        if (i == CEE_BREAK)
        {
            ieps[i].m_cycles = 0;
            continue; // Don't count these if they occur...
        }
        ieps[i].m_cycles = s_ILInstrCycles[i];
        _ASSERTE((ieps[i].m_execs != 0) || (ieps[i].m_cycles == 0)); // Cycles can be zero for non-zero execs because of measurement correction.
#endif // INTERP_ILCYCLE_PROFILE
    }
    for (unsigned short i = 0; i < CountIlInstr2Byte; i++)
    {
        int ind = 256 + i;
        ieps[ind].m_instr = i; ieps[ind].m_is2byte = true; ieps[ind].m_execs = s_ILInstr2ByteExecs[i];
#if INTERP_ILCYCLE_PROFILE
        ieps[ind].m_cycles = s_ILInstrCycles[ind];
        _ASSERTE((ieps[i].m_execs != 0) || (ieps[i].m_cycles == 0)); // Cycles can be zero for non-zero execs because of measurement correction.
#endif // INTERP_ILCYCLE_PROFILE
    }

    qsort(&ieps[0], 256 + CountIlInstr2Byte, sizeof(InstrExecRecord), &InstrExecRecord::Compare);

    fprintf(GetLogFile(), "\nInstructions (%d total, %d 1-byte):\n", totInstrs, totInstrs - totInstrs2Byte);
#if INTERP_ILCYCLE_PROFILE
    if (s_callCycles > s_calls * perMeasurementOverhead) s_callCycles -= s_calls * perMeasurementOverhead;
    else s_callCycles = 0;
    fprintf(GetLogFile(), "      MCycles (%lld total, %lld 1-byte, %lld calls (%d calls, %10.2f cyc/call):\n",
            totCycles/MIL, (totCycles - totCycles2Byte)/MIL, s_callCycles/MIL, s_calls, float(s_callCycles)/float(s_calls));
#if 0
    extern unsigned __int64 MetaSigCtor1Cycles;
    fprintf(GetLogFile(), "      MetaSig(MethodDesc, TypeHandle) ctor: %lld MCycles.\n",
        MetaSigCtor1Cycles/MIL);
    fprintf(GetLogFile(), "      ForceSigWalk: %lld MCycles.\n",
        ForceSigWalkCycles/MIL);
#endif
#endif // INTERP_ILCYCLE_PROFILE

    PrintILProfile(&ieps[0], totInstrs
#if INTERP_ILCYCLE_PROFILE
                   , totCycles
#endif // INTERP_ILCYCLE_PROFILE
                  );

    fprintf(GetLogFile(), "\nInstructions grouped by category: (%d total, %d 1-byte):\n", totInstrs, totInstrs - totInstrs2Byte);
#if INTERP_ILCYCLE_PROFILE
    fprintf(GetLogFile(), "                           MCycles (%lld total, %lld 1-byte):\n",
            totCycles/MIL, (totCycles - totCycles2Byte)/MIL);
#endif // INTERP_ILCYCLE_PROFILE
    for (unsigned short i = 0; i < 256 + CountIlInstr2Byte; i++)
    {
        if (i < 256)
        {
            ieps[i].m_instr = i; ieps[i].m_is2byte = false;
        }
        else
        {
            ieps[i].m_instr = i - 256; ieps[i].m_is2byte = true;
        }
        ieps[i].m_execs = s_ILInstrExecsByCategory[i];
#if INTERP_ILCYCLE_PROFILE
        ieps[i].m_cycles = s_ILInstrCyclesByCategory[i];
#endif // INTERP_ILCYCLE_PROFILE
    }
    qsort(&ieps[0], 256 + CountIlInstr2Byte, sizeof(InstrExecRecord), &InstrExecRecord::Compare);
    PrintILProfile(&ieps[0], totInstrs
#if INTERP_ILCYCLE_PROFILE
                   , totCycles
#endif // INTERP_ILCYCLE_PROFILE
                   );

#if 0
    // Early debugging code.
    fprintf(GetLogFile(), "\nInstructions grouped category mapping:\n", totInstrs, totInstrs - totInstrs2Byte);
    for (unsigned short i = 0; i < 256; i++)
    {
        unsigned short cat = s_ILInstrCategories[i];
        if (cat < 256) {
            fprintf(GetLogFile(), "Instr: %12s  ==>  %12s.\n", ILOp1Byte(i), ILOp1Byte(cat));
        } else {
            fprintf(GetLogFile(), "Instr: %12s  ==>  %12s.\n", ILOp1Byte(i), ILOp2Byte(cat - 256));
        }
    }
    for (unsigned short i = 0; i < CountIlInstr2Byte; i++)
    {
        unsigned ind = 256 + i;
        unsigned short cat = s_ILInstrCategories[ind];
        if (cat < 256) {
            fprintf(GetLogFile(), "Instr: %12s  ==>  %12s.\n", ILOp2Byte(i), ILOp1Byte(cat));
        } else {
            fprintf(GetLogFile(), "Instr: %12s  ==>  %12s.\n", ILOp2Byte(i), ILOp2Byte(cat - 256));
        }
    }
#endif
#endif // INTERP_ILINSTR_PROFILE
}

#if INTERP_ILINSTR_PROFILE

const int K = 1000;

// static
void Interpreter::PrintILProfile(Interpreter::InstrExecRecord *recs, unsigned int totInstrs
#if INTERP_ILCYCLE_PROFILE
                                 , unsigned __int64 totCycles
#endif // INTERP_ILCYCLE_PROFILE
                                 )
{
    float fTotInstrs = float(totInstrs);
    fprintf(GetLogFile(), "Instruction  |   execs   |       %% |   cum %%");
#if INTERP_ILCYCLE_PROFILE
    float fTotCycles = float(totCycles);
    fprintf(GetLogFile(), "|      KCycles |       %% |   cum %% |   cyc/inst\n");
    fprintf(GetLogFile(), "--------------------------------------------------"
            "-----------------------------------------\n");
#else
    fprintf(GetLogFile(), "\n-------------------------------------------\n");
#endif
    float numPct = 0.0f;
#if INTERP_ILCYCLE_PROFILE
    float numCyclePct = 0.0f;
#endif // INTERP_ILCYCLE_PROFILE
    for (unsigned i = 0; i < 256 + CountIlInstr2Byte; i++)
    {
        float pct = 0.0f;
        if (totInstrs > 0) pct = float(recs[i].m_execs) * 100.0f / fTotInstrs;
        numPct += pct;
        if (recs[i].m_execs > 0)
        {
            fprintf(GetLogFile(), "%12s | %9d | %6.2f%% | %6.2f%%",
            (recs[i].m_is2byte ? ILOp2Byte(recs[i].m_instr) : ILOp1Byte(recs[i].m_instr)), recs[i].m_execs,
            pct, numPct);
#if INTERP_ILCYCLE_PROFILE
            pct = 0.0f;
            if (totCycles > 0) pct = float(recs[i].m_cycles) * 100.0f / fTotCycles;
            numCyclePct += pct;
            float cyclesPerInst = float(recs[i].m_cycles) / float(recs[i].m_execs);
            fprintf(GetLogFile(), "| %12llu | %6.2f%% | %6.2f%% | %11.2f",
                    recs[i].m_cycles/K, pct, numCyclePct, cyclesPerInst);
#endif // INTERP_ILCYCLE_PROFILE
            fprintf(GetLogFile(), "\n");
        }
    }
}
#endif // INTERP_ILINSTR_PROFILE

#endif // FEATURE_INTERPRETER
