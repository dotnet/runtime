// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


//
// Provides an abstraction over platform specific calling conventions (specifically, the calling convention
// utilized by the JIT on that platform). The caller enumerates each argument of a signature in turn, and is
// provided with information mapping that argument into registers and/or stack locations.
//

#ifndef __CALLING_CONVENTION_INCLUDED
#define __CALLING_CONVENTION_INCLUDED

BOOL IsRetBuffPassedAsFirstArg();

// Describes how a single argument is laid out in registers and/or stack locations when given as an input to a
// managed method as part of a larger signature.
//
// Locations are split into floating point registers, general registers and stack offsets. Registers are
// obviously architecture dependent but are represented as a zero-based index into the usual sequence in which
// such registers are allocated for input on the platform in question. For instance:
//      X86: 0 == ecx, 1 == edx
//      ARM: 0 == r0, 1 == r1, 2 == r2 etc.
//
// Stack locations are represented as offsets from the stack pointer (at the point of the call). The offset is
// given as an index of a pointer sized slot. Similarly the size of data on the stack is given in slot-sized
// units. For instance, given an index of 2 and a size of 3:
//      X86:   argument starts at [ESP + 8] and is 12 bytes long
//      AMD64: argument starts at [RSP + 16] and is 24 bytes long
//
// The structure is flexible enough to describe an argument that is split over several (consecutive) registers
// and possibly on to the stack as well.
struct ArgLocDesc
{
    int     m_idxFloatReg;        // First floating point register used (or -1)
    int     m_cFloatReg;          // Count of floating point registers used (or 0)

    int     m_idxGenReg;          // First general register used (or -1)
    int     m_cGenReg;            // Count of general registers used (or 0)

    int     m_byteStackIndex;     // Stack offset in bytes (or -1)
    int     m_byteStackSize;      // Stack size in bytes

#if defined(UNIX_AMD64_ABI)

    EEClass* m_eeClass;           // For structs passed in register, it points to the EEClass of the struct

#endif // UNIX_AMD64_ABI

#ifdef FEATURE_HFA
    static unsigned getHFAFieldSize(CorInfoHFAElemType  hfaType)
    {
        switch (hfaType)
        {
        case CORINFO_HFA_ELEM_FLOAT: return 4;
        case CORINFO_HFA_ELEM_DOUBLE: return 8;
        case CORINFO_HFA_ELEM_VECTOR64: return 8;
        case CORINFO_HFA_ELEM_VECTOR128: return 16;
        default: _ASSERTE(!"Invalid HFA Type"); return 0;
        }
    }
#endif
#if defined(TARGET_ARM64)
    unsigned m_hfaFieldSize;      // Size of HFA field in bytes.
    void setHFAFieldSize(CorInfoHFAElemType  hfaType)
    {
        m_hfaFieldSize = getHFAFieldSize(hfaType);
    }
#endif // defined(TARGET_ARM64)

#if defined(TARGET_ARM)
    BOOL    m_fRequires64BitAlignment; // True if the argument should always be aligned (in registers or on the stack
#endif

    ArgLocDesc()
    {
        Init();
    }

    // Initialize to represent a non-placed argument (no register or stack slots referenced).
    void Init()
    {
        m_idxFloatReg = -1;
        m_cFloatReg = 0;
        m_idxGenReg = -1;
        m_cGenReg = 0;
        m_byteStackIndex = -1;
        m_byteStackSize = 0;
#if defined(TARGET_ARM)
        m_fRequires64BitAlignment = FALSE;
#endif
#if defined(TARGET_ARM64)
        m_hfaFieldSize = 0;
#endif // defined(TARGET_ARM64)
#if defined(UNIX_AMD64_ABI)
        m_eeClass = NULL;
#endif
    }
};

//
// TransitionBlock is layout of stack frame of method call, saved argument registers and saved callee saved registers. Even though not
// all fields are used all the time, we use uniform form for simplicity.
//
struct TransitionBlock
{
#if defined(TARGET_X86)
    ArgumentRegisters       m_argumentRegisters;
    CalleeSavedRegisters    m_calleeSavedRegisters;
    TADDR                   m_ReturnAddress;
#elif defined(TARGET_AMD64)
#ifdef UNIX_AMD64_ABI
    ArgumentRegisters       m_argumentRegisters;
#endif
    CalleeSavedRegisters    m_calleeSavedRegisters;
    TADDR                   m_ReturnAddress;
#elif defined(TARGET_ARM)
    union {
        CalleeSavedRegisters m_calleeSavedRegisters;
        // alias saved link register as m_ReturnAddress
        struct {
            INT32 r4, r5, r6, r7, r8, r9, r10;
            INT32 r11;
            TADDR m_ReturnAddress;
        };
    };
    ArgumentRegisters       m_argumentRegisters;
#elif defined(TARGET_ARM64)
    union {
        CalleeSavedRegisters m_calleeSavedRegisters;
        struct {
            INT64 x29; // frame pointer
            TADDR m_ReturnAddress;
            INT64 x19, x20, x21, x22, x23, x24, x25, x26, x27, x28;
        };
    };
    TADDR padding; // Keep size of TransitionBlock as multiple of 16-byte. Simplifies code in PROLOG_WITH_TRANSITION_BLOCK
    INT64 m_x8RetBuffReg;
    ArgumentRegisters       m_argumentRegisters;
#else
    PORTABILITY_ASSERT("TransitionBlock");
#endif

    // The transition block should define everything pushed by callee. The code assumes in number of places that
    // end of the transition block is caller's stack pointer.

    static int GetOffsetOfReturnAddress()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(TransitionBlock, m_ReturnAddress);
    }

#ifdef TARGET_ARM64
    static int GetOffsetOfRetBuffArgReg()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(TransitionBlock, m_x8RetBuffReg);
    }

    static int GetOffsetOfFirstGCRefMapSlot()
    {
        return GetOffsetOfRetBuffArgReg();
    }
#else
    static int GetOffsetOfFirstGCRefMapSlot()
    {
        return GetOffsetOfArgumentRegisters();
    }
#endif

    static BYTE GetOffsetOfArgs()
    {
        LIMITED_METHOD_CONTRACT;

        // Offset of the stack args (which are after the TransitionBlock)
        return sizeof(TransitionBlock);
    }

    static int GetOffsetOfArgumentRegisters()
    {
        LIMITED_METHOD_CONTRACT;
        int offs;
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
        offs = sizeof(TransitionBlock);
#else
        offs = offsetof(TransitionBlock, m_argumentRegisters);
#endif
        return offs;
    }

    static BOOL IsStackArgumentOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(UNIX_AMD64_ABI)
        return offset >= (int)sizeof(TransitionBlock);
#else
        int ofsArgRegs = GetOffsetOfArgumentRegisters();

        return offset >= (int) (ofsArgRegs + ARGUMENTREGISTERS_SIZE);
#endif
    }

    static BOOL IsArgumentRegisterOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

        int ofsArgRegs = GetOffsetOfArgumentRegisters();

        return offset >= ofsArgRegs && offset < (int) (ofsArgRegs + ARGUMENTREGISTERS_SIZE);
    }

    static UINT GetArgumentIndexFromOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(UNIX_AMD64_ABI)
        _ASSERTE(offset != TransitionBlock::StructInRegsOffset);
#endif
        offset -= GetOffsetOfArgumentRegisters();
        _ASSERTE((offset % TARGET_POINTER_SIZE) == 0);
        return offset / TARGET_POINTER_SIZE;
    }

    static UINT GetStackArgumentIndexFromOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

        return (offset - TransitionBlock::GetOffsetOfArgs()) / TARGET_POINTER_SIZE;
    }

    static UINT GetStackArgumentByteIndexFromOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

        return (offset - TransitionBlock::GetOffsetOfArgs());
    }

#ifdef CALLDESCR_FPARGREGS
    static BOOL IsFloatArgumentRegisterOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;
#if defined(UNIX_AMD64_ABI)
        return (offset != TransitionBlock::StructInRegsOffset) && (offset < 0);
#else
        return offset < 0;
#endif
    }

    // Check if an argument has floating point register, that means that it is
    // either a floating point argument or a struct passed in registers that
    // has a floating point member.
    static BOOL HasFloatRegister(int offset, ArgLocDesc* argLocDescForStructInRegs)
    {
        LIMITED_METHOD_CONTRACT;
    #if defined(UNIX_AMD64_ABI)
        if (offset == TransitionBlock::StructInRegsOffset)
        {
            return argLocDescForStructInRegs->m_cFloatReg > 0;
        }
    #endif
        return offset < 0;
    }

    static int GetOffsetOfFloatArgumentRegisters()
    {
        LIMITED_METHOD_CONTRACT;
        return -GetNegSpaceSize();
    }
#endif // CALLDESCR_FPARGREGS

    static int GetOffsetOfCalleeSavedRegisters()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(TransitionBlock, m_calleeSavedRegisters);
    }

    static int GetNegSpaceSize()
    {
        LIMITED_METHOD_CONTRACT;
        int negSpaceSize = 0;
#ifdef CALLDESCR_FPARGREGS
        negSpaceSize += sizeof(FloatArgumentRegisters);
#endif
#ifdef TARGET_ARM
        negSpaceSize += TARGET_POINTER_SIZE; // padding to make FloatArgumentRegisters address 8-byte aligned
#endif
        return negSpaceSize;
    }

    static const int InvalidOffset = -1;
#if defined(UNIX_AMD64_ABI)
    // Special offset value to represent  struct passed in registers. Such a struct can span both
    // general purpose and floating point registers, so it can have two different offsets.
    static const int StructInRegsOffset = -2;
#endif
};

//-----------------------------------------------------------------------
// ArgIterator is helper for dealing with calling conventions.
// It is tightly coupled with TransitionBlock. It uses offsets into
// TransitionBlock to represent argument locations for efficiency
// reasons. Alternatively, it can also return ArgLocDesc for less
// performance critical code.
//
// The ARGITERATOR_BASE argument of the template is provider of the parsed
// method signature. Typically, the arg iterator works on top of MetaSig.
// Reflection invoke uses alternative implementation to save signature parsing
// time because of it has the parsed signature available.
//-----------------------------------------------------------------------
template<class ARGITERATOR_BASE>
class ArgIteratorTemplate : public ARGITERATOR_BASE
{
public:
    //------------------------------------------------------------
    // Constructor
    //------------------------------------------------------------
    ArgIteratorTemplate()
    {
        WRAPPER_NO_CONTRACT;
        m_dwFlags = 0;
    }

    UINT SizeOfArgStack()
    {
        WRAPPER_NO_CONTRACT;
        if (!(m_dwFlags & SIZE_OF_ARG_STACK_COMPUTED))
            ForceSigWalk();
        _ASSERTE((m_dwFlags & SIZE_OF_ARG_STACK_COMPUTED) != 0);
        _ASSERTE((m_nSizeOfArgStack % TARGET_POINTER_SIZE) == 0);
        return m_nSizeOfArgStack;
    }

    // For use with ArgIterator. This function computes the amount of additional
    // memory required above the TransitionBlock.  The parameter offsets
    // returned by ArgIteratorTemplate::GetNextOffset are relative to a
    // FramedMethodFrame, and may be in either of these regions.
    UINT SizeOfFrameArgumentArray()
    {
        WRAPPER_NO_CONTRACT;

        UINT size = SizeOfArgStack();

#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
        // The argument registers are not included in the stack size on AMD64
        size += ARGUMENTREGISTERS_SIZE;
#endif
        _ASSERTE((size % TARGET_POINTER_SIZE) == 0);
        return size;
    }

    //------------------------------------------------------------------------

#ifdef TARGET_X86
    UINT CbStackPop()
    {
        WRAPPER_NO_CONTRACT;

        if (this->IsVarArg())
            return 0;
        else
            return SizeOfArgStack();
    }
#endif

    // Is there a hidden parameter for the return parameter?
    //
    BOOL HasRetBuffArg()
    {
        WRAPPER_NO_CONTRACT;
        if (!(m_dwFlags & RETURN_FLAGS_COMPUTED))
            ComputeReturnFlags();
        return (m_dwFlags & RETURN_HAS_RET_BUFFER);
    }

    UINT GetFPReturnSize()
    {
        WRAPPER_NO_CONTRACT;
        if (!(m_dwFlags & RETURN_FLAGS_COMPUTED))
            ComputeReturnFlags();
        return m_dwFlags >> RETURN_FP_SIZE_SHIFT;
    }

#ifdef TARGET_X86
    //=========================================================================
    // Indicates whether an argument is to be put in a register using the
    // default IL calling convention. This should be called on each parameter
    // in the order it appears in the call signature. For a non-static method,
    // this function should also be called once for the "this" argument, prior
    // to calling it for the "real" arguments. Pass in a typ of ELEMENT_TYPE_CLASS.
    //
    //  *pNumRegistersUsed:  [in,out]: keeps track of the number of argument
    //                       registers assigned previously. The caller should
    //                       initialize this variable to 0 - then each call
    //                       will update it.
    //
    //  typ:                 the signature type
    //=========================================================================
    static BOOL IsArgumentInRegister(int * pNumRegistersUsed, CorElementType typ, TypeHandle hnd)
    {
        LIMITED_METHOD_CONTRACT;
        if ( (*pNumRegistersUsed) < NUM_ARGUMENT_REGISTERS)
        {
            if (typ == ELEMENT_TYPE_VALUETYPE)
            {
                // The JIT enables passing trivial pointer sized structs in registers.
                MethodTable* pMT = hnd.GetMethodTable();

                while (typ == ELEMENT_TYPE_VALUETYPE &&
                    pMT->GetNumInstanceFields() == 1 && (!pMT->HasLayout()	||
                    pMT->GetNumInstanceFieldBytes() == 4	
                    )) // Don't do the optimization if we're getting specified anything but the trivial layout.	
                {	
                    FieldDesc * pFD = pMT->GetApproxFieldDescListRaw();	
                    CorElementType type = pFD->GetFieldType();

                    bool exitLoop = false;
                    switch (type)	
                    {
                        case ELEMENT_TYPE_VALUETYPE:
                        {
                            //@todo: Is it more apropos to call LookupApproxFieldTypeHandle() here?	
                            TypeHandle fldHnd = pFD->GetApproxFieldTypeHandleThrowing();	
                            CONSISTENCY_CHECK(!fldHnd.IsNull());
                            pMT = fldHnd.GetMethodTable();
                            FALLTHROUGH;
                        }	
                        case ELEMENT_TYPE_PTR:
                        case ELEMENT_TYPE_I:
                        case ELEMENT_TYPE_U:
                        case ELEMENT_TYPE_I4:	
                        case ELEMENT_TYPE_U4:
                        {	
                            typ = type;
                            break;	
                        }
                        default:
                            exitLoop = true;
                            break;
                    }

                    if (exitLoop)
                    {
                        break;
                    }
                }
            }
            if (gElementTypeInfo[typ].m_enregister)
            {
                (*pNumRegistersUsed)++;
                return(TRUE);
            }
        }

        return(FALSE);
    }
#endif // TARGET_X86

#if defined(ENREGISTERED_PARAMTYPE_MAXSIZE)

    // Note that this overload does not handle varargs
    static BOOL IsArgPassedByRef(TypeHandle th)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(!th.IsNull());

        // This method only works for valuetypes. It includes true value types,
        // primitives, enums and TypedReference.
        _ASSERTE(th.IsValueType());

        size_t size = th.GetSize();
#ifdef TARGET_AMD64
        return IsArgPassedByRef(size);
#elif defined(TARGET_ARM64)
        // Composites greater than 16 bytes are passed by reference
        return ((size > ENREGISTERED_PARAMTYPE_MAXSIZE) && !th.IsHFA());
#else
        PORTABILITY_ASSERT("ArgIteratorTemplate::IsArgPassedByRef");
        return FALSE;
#endif
    }

#ifdef TARGET_AMD64
    // This overload should only be used in AMD64-specific code only.
    static BOOL IsArgPassedByRef(size_t size)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef UNIX_AMD64_ABI
        // No arguments are passed by reference on AMD64 on Unix
        return FALSE;
#else
        // If the size is bigger than ENREGISTERED_PARAM_TYPE_MAXSIZE, or if the size is NOT a power of 2, then
        // the argument is passed by reference.
        return (size > ENREGISTERED_PARAMTYPE_MAXSIZE) || ((size & (size-1)) != 0);
#endif
    }
#endif // TARGET_AMD64

    // This overload should be used for varargs only.
    static BOOL IsVarArgPassedByRef(size_t size)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef TARGET_AMD64
#ifdef UNIX_AMD64_ABI
        PORTABILITY_ASSERT("ArgIteratorTemplate::IsVarArgPassedByRef");
        return FALSE;
#else // UNIX_AMD64_ABI
        return IsArgPassedByRef(size);
#endif // UNIX_AMD64_ABI

#else
        return (size > ENREGISTERED_PARAMTYPE_MAXSIZE);
#endif
    }

    BOOL IsArgPassedByRef()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef TARGET_AMD64
        return IsArgPassedByRef(m_argSize);
#elif defined(TARGET_ARM64)
        if (m_argType == ELEMENT_TYPE_VALUETYPE)
        {
            _ASSERTE(!m_argTypeHandle.IsNull());
            return ((m_argSize > ENREGISTERED_PARAMTYPE_MAXSIZE) && (!m_argTypeHandle.IsHFA() || this->IsVarArg()));
        }
        return FALSE;
#else
        PORTABILITY_ASSERT("ArgIteratorTemplate::IsArgPassedByRef");
        return FALSE;
#endif
    }

#endif // ENREGISTERED_PARAMTYPE_MAXSIZE

    //------------------------------------------------------------
    // Return the offsets of the special arguments
    //------------------------------------------------------------

    static int GetThisOffset();

    int GetRetBuffArgOffset();
    int GetVASigCookieOffset();
    int GetParamTypeArgOffset();

    //------------------------------------------------------------
    // Each time this is called, this returns a byte offset of the next
    // argument from the TransitionBlock* pointer.
    //
    // Returns TransitionBlock::InvalidOffset once you've hit the end
    // of the list.
    //------------------------------------------------------------
    int GetNextOffset();

    CorElementType GetArgType(TypeHandle *pTypeHandle = NULL)
    {
        LIMITED_METHOD_CONTRACT;
        if (pTypeHandle != NULL)
        {
            *pTypeHandle = m_argTypeHandle;
        }
        return m_argType;
    }

    int GetArgSize()
    {
        LIMITED_METHOD_CONTRACT;
        return m_argSize;
    }

    void ForceSigWalk();

#ifndef TARGET_X86
    // Accessors for built in argument descriptions of the special implicit parameters not mentioned directly
    // in signatures (this pointer and the like). Whether or not these can be used successfully before all the
    // explicit arguments have been scanned is platform dependent.
    void GetThisLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetThisOffset(), pLoc); }
    void GetParamTypeLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetParamTypeArgOffset(), pLoc); }
    void GetVASigCookieLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetVASigCookieOffset(), pLoc); }

#ifndef CALLDESCR_RETBUFFARGREG
    void GetRetBuffArgLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetRetBuffArgOffset(), pLoc); }
#endif

#endif // !TARGET_X86

    ArgLocDesc* GetArgLocDescForStructInRegs()
    {
#if defined(UNIX_AMD64_ABI) || defined (TARGET_ARM64)
        return m_hasArgLocDescForStructInRegs ? &m_argLocDescForStructInRegs : NULL;
#else
        return NULL;
#endif
    }

#ifdef TARGET_X86
    // Get layout information for the argument that the ArgIterator is currently visiting.
    void GetArgLoc(int argOffset, ArgLocDesc *pLoc)
    {
        LIMITED_METHOD_CONTRACT;

        pLoc->Init();

        if (!TransitionBlock::IsStackArgumentOffset(argOffset))
        {
            pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);
            _ASSERTE(GetArgSize() <= TARGET_POINTER_SIZE);
            pLoc->m_cGenReg = 1;
        }
        else
        {
            pLoc->m_byteStackSize = GetArgSize();
            pLoc->m_byteStackIndex = TransitionBlock::GetStackArgumentByteIndexFromOffset(argOffset);
        }
    }
#endif

#ifdef TARGET_ARM
    // Get layout information for the argument that the ArgIterator is currently visiting.
    void GetArgLoc(int argOffset, ArgLocDesc *pLoc)
    {
        LIMITED_METHOD_CONTRACT;

        pLoc->Init();

        pLoc->m_fRequires64BitAlignment = m_fRequires64BitAlignment;

        const int byteArgSize = GetArgSize();
        if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
        {
            const int floatRegOfsInBytes = argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters();
            _ASSERTE((floatRegOfsInBytes % FLOAT_REGISTER_SIZE) == 0);
            pLoc->m_idxFloatReg = floatRegOfsInBytes / FLOAT_REGISTER_SIZE;
            pLoc->m_cFloatReg = ALIGN_UP(byteArgSize, FLOAT_REGISTER_SIZE) / FLOAT_REGISTER_SIZE;
            return;
        }

        if (!TransitionBlock::IsStackArgumentOffset(argOffset))
        {
            pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);

            if (byteArgSize <= (4 - pLoc->m_idxGenReg) * TARGET_POINTER_SIZE)
            {
                pLoc->m_cGenReg = ALIGN_UP(byteArgSize, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE;
            }
            else
            {
                pLoc->m_cGenReg = 4 - pLoc->m_idxGenReg;
                pLoc->m_byteStackIndex = 0;
                pLoc->m_byteStackSize = StackElemSize(byteArgSize) - pLoc->m_cGenReg * TARGET_POINTER_SIZE;
            }
        }
        else
        {
            pLoc->m_byteStackIndex = TransitionBlock::GetStackArgumentByteIndexFromOffset(argOffset);
            pLoc->m_byteStackSize = StackElemSize(byteArgSize);
        }
    }
#endif // TARGET_ARM

#ifdef TARGET_ARM64
    // Get layout information for the argument that the ArgIterator is currently visiting.
    void GetArgLoc(int argOffset, ArgLocDesc *pLoc)
    {
        LIMITED_METHOD_CONTRACT;

        pLoc->Init();


        if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
        {
            const int floatRegOfsInBytes = argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters();
            _ASSERTE((floatRegOfsInBytes % FLOAT_REGISTER_SIZE) == 0);
            pLoc->m_idxFloatReg = floatRegOfsInBytes / FLOAT_REGISTER_SIZE;

            if (!m_argTypeHandle.IsNull() && m_argTypeHandle.IsHFA())
            {
                CorInfoHFAElemType type = m_argTypeHandle.GetHFAType();
                pLoc->setHFAFieldSize(type);
                pLoc->m_cFloatReg = GetArgSize() / pLoc->m_hfaFieldSize;

            }
            else
            {
                pLoc->m_cFloatReg = 1;
            }
            return;
        }

        unsigned byteArgSize = GetArgSize();

        // Question: why do not arm and x86 have similar checks?
        // Composites greater than 16 bytes are passed by reference
        if ((GetArgType() == ELEMENT_TYPE_VALUETYPE) && (byteArgSize > ENREGISTERED_PARAMTYPE_MAXSIZE))
        {
            byteArgSize = TARGET_POINTER_SIZE;
        }


        // Sanity check to make sure no caller is trying to get an ArgLocDesc that
        // describes the return buffer reg field that's in the TransitionBlock.
        _ASSERTE(argOffset != TransitionBlock::GetOffsetOfRetBuffArgReg());

        if (!TransitionBlock::IsStackArgumentOffset(argOffset))
        {
            pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);
            pLoc->m_cGenReg = ALIGN_UP(byteArgSize, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE;;
        }
        else
        {
            pLoc->m_byteStackIndex = TransitionBlock::GetStackArgumentByteIndexFromOffset(argOffset);
            const bool isValueType = (m_argType == ELEMENT_TYPE_VALUETYPE);
            const bool isFloatHfa = (isValueType && !m_argTypeHandle.IsNull() && m_argTypeHandle.IsHFA());
            if (isFloatHfa)
            {
                CorInfoHFAElemType type = m_argTypeHandle.GetHFAType();
                pLoc->setHFAFieldSize(type);
            }
            pLoc->m_byteStackSize = StackElemSize(byteArgSize, isValueType, isFloatHfa);
        }
    }
#endif // TARGET_ARM64

#if defined(TARGET_AMD64)
    // Get layout information for the argument that the ArgIterator is currently visiting.
    void GetArgLoc(int argOffset, ArgLocDesc* pLoc)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(UNIX_AMD64_ABI)
        if (m_hasArgLocDescForStructInRegs)
        {
            *pLoc = m_argLocDescForStructInRegs;
            return;
        }

        if (argOffset == TransitionBlock::StructInRegsOffset)
        {
            // We always already have argLocDesc for structs passed in registers, we
            // compute it in the GetNextOffset for those since it is always needed.
            _ASSERTE(false);
            return;
        }
#endif // UNIX_AMD64_ABI

        pLoc->Init();

#if defined(UNIX_AMD64_ABI)
        if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
        {
            const int floatRegOfsInBytes = argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters();
            _ASSERTE((floatRegOfsInBytes % FLOAT_REGISTER_SIZE) == 0);
            pLoc->m_idxFloatReg = floatRegOfsInBytes / FLOAT_REGISTER_SIZE;
            pLoc->m_cFloatReg = 1;
        }
        else 
#endif // UNIX_AMD64_ABI
        if (!TransitionBlock::IsStackArgumentOffset(argOffset))
        {
#if !defined(UNIX_AMD64_ABI)
            // On Windows x64, we re-use the location in the transition block for both the integer and floating point registers
            if ((m_argType == ELEMENT_TYPE_R4) || (m_argType == ELEMENT_TYPE_R8))
            {
                pLoc->m_idxFloatReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);
                pLoc->m_cFloatReg = 1;
            }
            else
#endif
            {
                pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);
                pLoc->m_cGenReg = 1;
            }
        }
        else
        {
            pLoc->m_byteStackIndex = TransitionBlock::GetStackArgumentByteIndexFromOffset(argOffset);
            int argSizeInBytes;
            if (IsArgPassedByRef())
                argSizeInBytes = TARGET_POINTER_SIZE;
            else
                argSizeInBytes = GetArgSize();
            pLoc->m_byteStackSize = StackElemSize(argSizeInBytes);
        }
    }
#endif // TARGET_AMD64

protected:
    DWORD               m_dwFlags;              // Cached flags
    int                 m_nSizeOfArgStack;      // Cached value of SizeOfArgStack

    DWORD               m_argNum;

    // Cached information about last argument
    CorElementType      m_argType;
    int                 m_argSize;
    TypeHandle          m_argTypeHandle;
#if (defined(TARGET_AMD64) && defined(UNIX_AMD64_ABI)) || defined(TARGET_ARM64)
    ArgLocDesc          m_argLocDescForStructInRegs;
    bool                m_hasArgLocDescForStructInRegs;
#endif // (TARGET_AMD64 && UNIX_AMD64_ABI) || TARGET_ARM64

    int                 m_ofsStack;           // Current position of the stack iterator, in bytes

#ifdef TARGET_X86
    int                 m_numRegistersUsed;
#ifdef FEATURE_INTERPRETER
    bool                m_fUnmanagedCallConv;
#endif
#endif

#ifdef UNIX_AMD64_ABI
    int                 m_idxGenReg;        // Next general register to be assigned a value
    int                 m_idxFPReg;         // Next floating point register to be assigned a value
    bool                m_fArgInRegisters;  // Indicates that the current argument is stored in registers
#endif

#ifdef TARGET_ARM
    int                 m_idxGenReg;        // Next general register to be assigned a value
    WORD                m_wFPRegs;          // Bitmask of available floating point argument registers (s0-s15/d0-d7)
    bool                m_fRequires64BitAlignment; // Cached info about the current arg
#endif

#ifdef TARGET_ARM64
    int             m_idxGenReg;        // Next general register to be assigned a value
    int             m_idxFPReg;         // Next FP register to be assigned a value
#endif

    enum {
        ITERATION_STARTED               = 0x0001,   // Started iterating over arguments
        SIZE_OF_ARG_STACK_COMPUTED      = 0x0002,
        RETURN_FLAGS_COMPUTED           = 0x0004,
        RETURN_HAS_RET_BUFFER           = 0x0008,   // Cached value of HasRetBuffArg

#ifdef TARGET_X86
        PARAM_TYPE_REGISTER_MASK        = 0x0030,
        PARAM_TYPE_REGISTER_STACK       = 0x0010,
        PARAM_TYPE_REGISTER_ECX         = 0x0020,
        PARAM_TYPE_REGISTER_EDX         = 0x0030,
#endif

        METHOD_INVOKE_NEEDS_ACTIVATION  = 0x0040,   // Flag used by ArgIteratorForMethodInvoke

        RETURN_FP_SIZE_SHIFT            = 8,        // The rest of the flags is cached value of GetFPReturnSize
    };

    void ComputeReturnFlags();

#ifndef TARGET_X86
    void GetSimpleLoc(int offset, ArgLocDesc * pLoc)
    {
        WRAPPER_NO_CONTRACT;

#ifdef CALLDESCR_RETBUFFARGREG
        // Codepaths where this could happen have been removed. If this occurs, something
        // has been missed and this needs another look.
        _ASSERTE(offset != TransitionBlock::GetOffsetOfRetBuffArgReg());
#endif

        pLoc->Init();
        pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(offset);
        pLoc->m_cGenReg = 1;
    }
#endif
};


template<class ARGITERATOR_BASE>
int ArgIteratorTemplate<ARGITERATOR_BASE>::GetThisOffset()
{
    WRAPPER_NO_CONTRACT;

    // This pointer is in the first argument register by default
    int ret = TransitionBlock::GetOffsetOfArgumentRegisters();

#ifdef TARGET_X86
    // x86 is special as always
    ret += offsetof(ArgumentRegisters, ECX);
#endif

    return ret;
}

template<class ARGITERATOR_BASE>
int ArgIteratorTemplate<ARGITERATOR_BASE>::GetRetBuffArgOffset()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(this->HasRetBuffArg());

    // RetBuf arg is in the second argument register by default
    int ret = TransitionBlock::GetOffsetOfArgumentRegisters();

#if TARGET_X86
    // x86 is special as always
    ret += this->HasThis() ? offsetof(ArgumentRegisters, EDX) : offsetof(ArgumentRegisters, ECX);
#elif TARGET_ARM64
    ret = TransitionBlock::GetOffsetOfRetBuffArgReg();
#else
    if (this->HasThis())
        ret += TARGET_POINTER_SIZE;
#endif

    return ret;
}

template<class ARGITERATOR_BASE>
int ArgIteratorTemplate<ARGITERATOR_BASE>::GetVASigCookieOffset()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(this->IsVarArg());

#if defined(TARGET_X86)
    // x86 is special as always
    return sizeof(TransitionBlock);
#else
    // VaSig cookie is after this and retbuf arguments by default.
    int ret = TransitionBlock::GetOffsetOfArgumentRegisters();

    if (this->HasThis())
    {
        ret += TARGET_POINTER_SIZE;
    }

    if (this->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
    {
        ret += TARGET_POINTER_SIZE;
    }

    return ret;
#endif
}

//-----------------------------------------------------------
// Get the extra param offset for shared generic code
//-----------------------------------------------------------
template<class ARGITERATOR_BASE>
int ArgIteratorTemplate<ARGITERATOR_BASE>::GetParamTypeArgOffset()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
    }
    CONTRACTL_END

    _ASSERTE(this->HasParamType());

#ifdef TARGET_X86
    // x86 is special as always
    if (!(m_dwFlags & SIZE_OF_ARG_STACK_COMPUTED))
        ForceSigWalk();

    switch (m_dwFlags & PARAM_TYPE_REGISTER_MASK)
    {
    case PARAM_TYPE_REGISTER_ECX:
        return TransitionBlock::GetOffsetOfArgumentRegisters() + offsetof(ArgumentRegisters, ECX);
    case PARAM_TYPE_REGISTER_EDX:
        return TransitionBlock::GetOffsetOfArgumentRegisters() + offsetof(ArgumentRegisters, EDX);
    default:
        break;
    }

    // The param type arg is last stack argument otherwise
    return sizeof(TransitionBlock);
#else
    // The hidden arg is after this and retbuf arguments by default.
    int ret = TransitionBlock::GetOffsetOfArgumentRegisters();

    if (this->HasThis())
    {
        ret += TARGET_POINTER_SIZE;
    }

    if (this->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
    {
        ret += TARGET_POINTER_SIZE;
    }

    return ret;
#endif
}

// To avoid corner case bugs, limit maximum size of the arguments with sufficient margin
#define MAX_ARG_SIZE 0xFFFFFF

//------------------------------------------------------------
// Each time this is called, this returns a byte offset of the next
// argument from the Frame* pointer. This offset can be positive *or* negative.
//
// Returns TransitionBlock::InvalidOffset once you've hit the end of the list.
//------------------------------------------------------------
template<class ARGITERATOR_BASE>
int ArgIteratorTemplate<ARGITERATOR_BASE>::GetNextOffset()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (!(m_dwFlags & ITERATION_STARTED))
    {
        int numRegistersUsed = 0;

        if (this->HasThis())
            numRegistersUsed++;

        if (this->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
            numRegistersUsed++;

        _ASSERTE(!this->IsVarArg() || !this->HasParamType());

#ifndef TARGET_X86
        if (this->IsVarArg() || this->HasParamType())
        {
            numRegistersUsed++;
        }
#endif

#ifdef TARGET_X86
        if (this->IsVarArg())
        {
            numRegistersUsed = NUM_ARGUMENT_REGISTERS; // Nothing else gets passed in registers for varargs
        }

#ifdef FEATURE_INTERPRETER
        BYTE callconv = CallConv();
        switch (callconv)
        {
        case IMAGE_CEE_CS_CALLCONV_C:
        case IMAGE_CEE_CS_CALLCONV_STDCALL:
            m_numRegistersUsed = NUM_ARGUMENT_REGISTERS;
            m_ofsStack = TransitionBlock::GetOffsetOfArgs() + numRegistersUsed * sizeof(void *);
            m_fUnmanagedCallConv = true;
            break;

        case IMAGE_CEE_CS_CALLCONV_THISCALL:
        case IMAGE_CEE_CS_CALLCONV_FASTCALL:
            _ASSERTE_MSG(false, "Unsupported calling convention.");

        default:
            m_fUnmanagedCallConv = false;
            m_numRegistersUsed = numRegistersUsed;
            m_ofsStack = TransitionBlock::GetOffsetOfArgs() + SizeOfArgStack();
            break;
        }
#else
        m_numRegistersUsed = numRegistersUsed;
        m_ofsStack = TransitionBlock::GetOffsetOfArgs() + SizeOfArgStack();
#endif

#elif defined(TARGET_AMD64)
#ifdef UNIX_AMD64_ABI
        m_idxGenReg = numRegistersUsed;
        m_ofsStack = 0;
        m_idxFPReg = 0;
#else
        m_ofsStack = TransitionBlock::GetOffsetOfArgs() + numRegistersUsed * sizeof(void *);
#endif
#elif defined(TARGET_ARM)
        m_idxGenReg = numRegistersUsed;
        m_ofsStack = 0;

        m_wFPRegs = 0;
#elif defined(TARGET_ARM64)
        m_idxGenReg = numRegistersUsed;
        m_ofsStack = 0;

        m_idxFPReg = 0;
#else
        PORTABILITY_ASSERT("ArgIteratorTemplate::GetNextOffset");
#endif

        m_argNum = 0;

        m_dwFlags |= ITERATION_STARTED;
    }

    // We're done going through the args for this MetaSig
    if (m_argNum == this->NumFixedArgs())
        return TransitionBlock::InvalidOffset;

    TypeHandle thValueType;
    CorElementType argType = this->GetNextArgumentType(m_argNum++, &thValueType);

    int argSize = MetaSig::GetElemSize(argType, thValueType);

    m_argType = argType;
    m_argSize = argSize;
    m_argTypeHandle = thValueType;

#if defined(UNIX_AMD64_ABI) || defined (TARGET_ARM64)
    m_hasArgLocDescForStructInRegs = false;
#endif

#ifdef TARGET_X86
#ifdef FEATURE_INTERPRETER
    if (m_fUnmanagedCallConv)
    {
        int argOfs = m_ofsStack;
        m_ofsStack += StackElemSize(argSize);
        return argOfs;
    }
#endif
    if (IsArgumentInRegister(&m_numRegistersUsed, argType, thValueType))
    {
        return TransitionBlock::GetOffsetOfArgumentRegisters() + (NUM_ARGUMENT_REGISTERS - m_numRegistersUsed) * sizeof(void *);
    }

    m_ofsStack -= StackElemSize(argSize);
    _ASSERTE(m_ofsStack >= TransitionBlock::GetOffsetOfArgs());
    return m_ofsStack;
#elif defined(TARGET_AMD64)
#ifdef UNIX_AMD64_ABI

    m_fArgInRegisters = true;

    int cFPRegs = 0;
    int cGenRegs = 0;
    int cbArg = StackElemSize(argSize);

    switch (argType)
    {

    case ELEMENT_TYPE_R4:
        // 32-bit floating point argument.
        cFPRegs = 1;
        break;

    case ELEMENT_TYPE_R8:
        // 64-bit floating point argument.
        cFPRegs = 1;
        break;

    case ELEMENT_TYPE_VALUETYPE:
    {
        MethodTable *pMT = m_argTypeHandle.GetMethodTable();
        if (this->IsRegPassedStruct(pMT))
        {
            EEClass* eeClass = pMT->GetClass();
            cGenRegs = 0;
            for (int i = 0; i < eeClass->GetNumberEightBytes(); i++)
            {
                switch (eeClass->GetEightByteClassification(i))
                {
                    case SystemVClassificationTypeInteger:
                    case SystemVClassificationTypeIntegerReference:
                    case SystemVClassificationTypeIntegerByRef:
                        cGenRegs++;
                        break;
                    case SystemVClassificationTypeSSE:
                        cFPRegs++;
                        break;
                    default:
                        _ASSERTE(false);
                        break;
                }
            }

            // Check if we have enough registers available for the struct passing
            if ((cFPRegs + m_idxFPReg <= NUM_FLOAT_ARGUMENT_REGISTERS) && (cGenRegs + m_idxGenReg) <= NUM_ARGUMENT_REGISTERS)
            {
                m_argLocDescForStructInRegs.Init();
                m_argLocDescForStructInRegs.m_cGenReg = cGenRegs;
                m_argLocDescForStructInRegs.m_cFloatReg = cFPRegs;
                m_argLocDescForStructInRegs.m_idxGenReg = m_idxGenReg;
                m_argLocDescForStructInRegs.m_idxFloatReg = m_idxFPReg;
                m_argLocDescForStructInRegs.m_eeClass = eeClass;

                m_hasArgLocDescForStructInRegs = true;

                m_idxGenReg += cGenRegs;
                m_idxFPReg += cFPRegs;

                return TransitionBlock::StructInRegsOffset;
            }
        }

        // Set the register counts to indicate that this argument will not be passed in registers
        cFPRegs = 0;
        cGenRegs = 0;
        break;
    }

    default:
        cGenRegs = cbArg / 8; // GP reg size
        break;
    }

    if ((cFPRegs > 0) && (cFPRegs + m_idxFPReg <= NUM_FLOAT_ARGUMENT_REGISTERS))
    {
        int argOfs = TransitionBlock::GetOffsetOfFloatArgumentRegisters() + m_idxFPReg * 16;
        m_idxFPReg += cFPRegs;
        return argOfs;
    }
    else if ((cGenRegs > 0) && (m_idxGenReg + cGenRegs <= NUM_ARGUMENT_REGISTERS))
    {
        int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_idxGenReg * 8;
        m_idxGenReg += cGenRegs;
        return argOfs;
    }

    m_fArgInRegisters = false;

    int argOfs = TransitionBlock::GetOffsetOfArgs() + m_ofsStack;

    m_ofsStack += cbArg;

    return argOfs;
#else
    // Each argument takes exactly one slot on AMD64 on Windows
    int argOfs = m_ofsStack;
    m_ofsStack += sizeof(void *);
    return argOfs;
#endif
#elif defined(TARGET_ARM)
    // First look at the underlying type of the argument to determine some basic properties:
    //  1) The size of the argument in bytes (rounded up to the stack slot size of 4 if necessary).
    //  2) Whether the argument represents a floating point primitive (ELEMENT_TYPE_R4 or ELEMENT_TYPE_R8).
    //  3) Whether the argument requires 64-bit alignment (anything that contains a Int64/UInt64).

    bool fFloatingPoint = false;
    bool fRequiresAlign64Bit = false;

    switch (argType)
    {
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
        // 64-bit integers require 64-bit alignment on ARM.
        fRequiresAlign64Bit = true;
        break;

    case ELEMENT_TYPE_R4:
        // 32-bit floating point argument.
        fFloatingPoint = true;
        break;

    case ELEMENT_TYPE_R8:
        // 64-bit floating point argument.
        fFloatingPoint = true;
        fRequiresAlign64Bit = true;
        break;

    case ELEMENT_TYPE_VALUETYPE:
    {
        // Value type case: extract the alignment requirement, note that this has to handle
        // the interop "native value types".
        fRequiresAlign64Bit = thValueType.RequiresAlign8();

#ifdef FEATURE_HFA
        // Handle HFAs: packed structures of 1-4 floats or doubles that are passed in FP argument
        // registers if possible.
        if (thValueType.IsHFA())
        {
            fFloatingPoint = true;
        }
#endif

        break;
    }

    default:
        // The default is are 4-byte arguments (or promoted to 4 bytes), non-FP and don't require any
        // 64-bit alignment.
        break;
    }

    // Now attempt to place the argument into some combination of floating point or general registers and
    // the stack.

    // Save the alignment requirement
    m_fRequires64BitAlignment = fRequiresAlign64Bit;

    int cbArg = StackElemSize(argSize);
    _ASSERTE((cbArg % TARGET_POINTER_SIZE) == 0);

    // Ignore floating point argument placement in registers if we're dealing with a vararg function (the ABI
    // specifies this so that vararg processing on the callee side is simplified).
#ifndef ARM_SOFTFP
    if (fFloatingPoint && !this->IsVarArg())
    {
        // Handle floating point (primitive) arguments.

        // First determine whether we can place the argument in VFP registers. There are 16 32-bit
        // and 8 64-bit argument registers that share the same register space (e.g. D0 overlaps S0 and
        // S1). The ABI specifies that VFP values will be passed in the lowest sequence of registers that
        // haven't been used yet and have the required alignment. So the sequence (float, double, float)
        // would be mapped to (S0, D1, S1) or (S0, S2/S3, S1).
        //
        // We use a 16-bit bitmap to record which registers have been used so far.
        //
        // So we can use the same basic loop for each argument type (float, double or HFA struct) we set up
        // the following input parameters based on the size and alignment requirements of the arguments:
        //   wAllocMask : bitmask of the number of 32-bit registers we need (1 for 1, 3 for 2, 7 for 3 etc.)
        //   cSteps     : number of loop iterations it'll take to search the 16 registers
        //   cShift     : how many bits to shift the allocation mask on each attempt

        WORD wAllocMask = (1 << (cbArg / 4)) - 1;
        WORD cSteps = (WORD)(fRequiresAlign64Bit ? 9 - (cbArg / 8) : 17 - (cbArg / 4));
        WORD cShift = fRequiresAlign64Bit ? 2 : 1;

        // Look through the availability bitmask for a free register or register pair.
        for (WORD i = 0; i < cSteps; i++)
        {
            if ((m_wFPRegs & wAllocMask) == 0)
            {
                // We found one, mark the register or registers as used.
                m_wFPRegs |= wAllocMask;

                // Indicate the registers used to the caller and return.
                return TransitionBlock::GetOffsetOfFloatArgumentRegisters() + (i * cShift * 4);
            }
            wAllocMask <<= cShift;
        }

        // The FP argument is going to live on the stack. Once this happens the ABI demands we mark all FP
        // registers as unavailable.
        m_wFPRegs = 0xffff;

        // Doubles or HFAs containing doubles need the stack aligned appropriately.
        if (fRequiresAlign64Bit)
        {
            m_ofsStack = (int)ALIGN_UP(m_ofsStack, TARGET_POINTER_SIZE * 2);
        }

        // Indicate the stack location of the argument to the caller.
        int argOfs = TransitionBlock::GetOffsetOfArgs() + m_ofsStack;

        // Record the stack usage.
        m_ofsStack += cbArg;

        return argOfs;
    }
#endif // ARM_SOFTFP

    //
    // Handle the non-floating point case.
    //

    if (m_idxGenReg < 4)
    {
        if (fRequiresAlign64Bit)
        {
            // The argument requires 64-bit alignment. Align either the next general argument register if
            // we have any left.  See step C.3 in the algorithm in the ABI spec.
            m_idxGenReg = (int)ALIGN_UP(m_idxGenReg, 2);
        }

        int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_idxGenReg * 4;

        int cRemainingRegs = 4 - m_idxGenReg;
        if (cbArg <= cRemainingRegs * TARGET_POINTER_SIZE)
        {
            // Mark the registers just allocated as used.
            m_idxGenReg += ALIGN_UP(cbArg, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE;
            return argOfs;
        }

        // The ABI supports splitting a non-FP argument across registers and the stack. But this is
        // disabled if the FP arguments already overflowed onto the stack (i.e. the stack index is not
        // zero). The following code marks the general argument registers as exhausted if this condition
        // holds.  See steps C.5 in the algorithm in the ABI spec.

        m_idxGenReg = 4;

        if (m_ofsStack == 0)
        {
            m_ofsStack += cbArg - cRemainingRegs * TARGET_POINTER_SIZE;
            return argOfs;
        }
    }

    if (fRequiresAlign64Bit)
    {
        // The argument requires 64-bit alignment. If it is going to be passed on the stack, align
        // the next stack slot.  See step C.6 in the algorithm in the ABI spec.
        m_ofsStack = (int)ALIGN_UP(m_ofsStack, TARGET_POINTER_SIZE * 2);
    }

    int argOfs = TransitionBlock::GetOffsetOfArgs() + m_ofsStack;

    // Advance the stack pointer over the argument just placed.
    m_ofsStack += cbArg;

    return argOfs;
#elif defined(TARGET_ARM64)

    int cFPRegs = 0;

    switch (argType)
    {

    case ELEMENT_TYPE_R4:
        // 32-bit floating point argument.
        cFPRegs = 1;
        break;

    case ELEMENT_TYPE_R8:
        // 64-bit floating point argument.
        cFPRegs = 1;
        break;

    case ELEMENT_TYPE_VALUETYPE:
    {
        // Handle HFAs: packed structures of 1-4 floats, doubles, or short vectors
        // that are passed in FP argument registers if possible.
        if (thValueType.IsHFA())
        {
            CorInfoHFAElemType type = thValueType.GetHFAType();

            m_argLocDescForStructInRegs.Init();
            m_argLocDescForStructInRegs.m_idxFloatReg = m_idxFPReg;

            m_argLocDescForStructInRegs.setHFAFieldSize(type);
            cFPRegs = argSize/m_argLocDescForStructInRegs.m_hfaFieldSize;
            m_argLocDescForStructInRegs.m_cFloatReg = cFPRegs;

            // Check if we have enough registers available for the HFA passing
            if ((cFPRegs + m_idxFPReg) <= 8)
            {
                m_hasArgLocDescForStructInRegs = true;
            }
        }
        else
        {
            // Composite greater than 16bytes should be passed by reference
            if (argSize > ENREGISTERED_PARAMTYPE_MAXSIZE)
            {
                argSize = sizeof(TADDR);
            }
        }

        break;
    }

    default:
        break;
    }
    const bool isValueType = (argType == ELEMENT_TYPE_VALUETYPE);
    const bool isFloatHfa = thValueType.IsFloatHfa();
    const int cbArg = StackElemSize(argSize, isValueType, isFloatHfa);
    if (cFPRegs>0 && !this->IsVarArg())
    {
        if (cFPRegs + m_idxFPReg <= 8)
        {
            // Each floating point register in the argument area is 16 bytes.
            int argOfs = TransitionBlock::GetOffsetOfFloatArgumentRegisters() + m_idxFPReg * 16;
            m_idxFPReg += cFPRegs;
            return argOfs;
        }
        else
        {
            m_idxFPReg = 8;
        }
    }
    else
    {
#if !defined(OSX_ARM64_ABI)
        _ASSERTE((cbArg% TARGET_POINTER_SIZE) == 0);
#endif
        const int regSlots = ALIGN_UP(cbArg, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE;
        // Only x0-x7 are valid argument registers (x8 is always the return buffer)
        if (m_idxGenReg + regSlots <= 8)
        {
            // The entirety of the arg fits in the register slots.

            int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_idxGenReg * 8;
            m_idxGenReg += regSlots;
            return argOfs;
        }
        else
        {
#ifdef _WIN32
            if (this->IsVarArg() && m_idxGenReg < 8)
            {
                // Address the Windows ARM64 varargs case where an arg is split between regs and stack.
                // This can happen in the varargs case because the first 64 bytes of the stack are loaded
                // into x0-x7, and any remaining stack arguments are placed normally.
                int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_idxGenReg * 8;

                // Increase m_ofsStack to account for the space used for the remainder of the arg after
                // registers are filled.
                m_ofsStack += cbArg + (m_idxGenReg - 8) * TARGET_POINTER_SIZE;

                // We used up the remaining reg slots.
                m_idxGenReg = 8;

                return argOfs;
            }
            else
#endif
            {
                // Don't use reg slots for this. It will be passed purely on the stack arg space.
                m_idxGenReg = 8;
            }
        }
    }

#ifdef OSX_ARM64_ABI
    int alignment;
    if (!isValueType)
    {
        _ASSERTE((cbArg & (cbArg - 1)) == 0);
        alignment = cbArg;
    }
    else if (isFloatHfa)
    {
        alignment = 4;
    }
    else
    {
        alignment = 8;
    }
    m_ofsStack = (int)ALIGN_UP(m_ofsStack, alignment);
#endif // OSX_ARM64_ABI

    int argOfs = TransitionBlock::GetOffsetOfArgs() + m_ofsStack;
    m_ofsStack += cbArg;
    return argOfs;
#else
    PORTABILITY_ASSERT("ArgIteratorTemplate::GetNextOffset");
    return TransitionBlock::InvalidOffset;
#endif
}

template<class ARGITERATOR_BASE>
void ArgIteratorTemplate<ARGITERATOR_BASE>::ComputeReturnFlags()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
    }
    CONTRACTL_END

    TypeHandle thValueType;
    CorElementType type = this->GetReturnType(&thValueType);

    DWORD flags = RETURN_FLAGS_COMPUTED;
    switch (type)
    {
    case ELEMENT_TYPE_TYPEDBYREF:
#ifdef ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
        if (sizeof(TypedByRef) > ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE)
            flags |= RETURN_HAS_RET_BUFFER;
#else
        flags |= RETURN_HAS_RET_BUFFER;
#endif
        break;

    case ELEMENT_TYPE_R4:
#ifndef ARM_SOFTFP
        flags |= sizeof(float) << RETURN_FP_SIZE_SHIFT;
#endif
        break;

    case ELEMENT_TYPE_R8:
#ifndef ARM_SOFTFP
        flags |= sizeof(double) << RETURN_FP_SIZE_SHIFT;
#endif
        break;

    case ELEMENT_TYPE_VALUETYPE:
#ifdef ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
        {
            _ASSERTE(!thValueType.IsNull());

#if defined(UNIX_AMD64_ABI)
            MethodTable *pMT = thValueType.AsMethodTable();
            if (pMT->IsRegPassedStruct())
            {
                EEClass* eeClass = pMT->GetClass();

                if (eeClass->GetNumberEightBytes() == 1)
                {
                    // Structs occupying just one eightbyte are treated as int / double
                    if (eeClass->GetEightByteClassification(0) == SystemVClassificationTypeSSE)
                    {
                        flags |= sizeof(double) << RETURN_FP_SIZE_SHIFT;
                    }
                }
                else
                {
                    // Size of the struct is 16 bytes
                    flags |= (16 << RETURN_FP_SIZE_SHIFT);
                    // The lowest two bits of the size encode the order of the int and SSE fields
                    if (eeClass->GetEightByteClassification(0) == SystemVClassificationTypeSSE)
                    {
                        flags |= (1 << RETURN_FP_SIZE_SHIFT);
                    }

                    if (eeClass->GetEightByteClassification(1) == SystemVClassificationTypeSSE)
                    {
                        flags |= (2 << RETURN_FP_SIZE_SHIFT);
                    }
                }

                break;
            }
#else // UNIX_AMD64_ABI

#ifdef FEATURE_HFA
            if (thValueType.IsHFA() && !this->IsVarArg())
            {
                CorInfoHFAElemType hfaType = thValueType.GetHFAType();

                int hfaFieldSize = ArgLocDesc::getHFAFieldSize(hfaType);
                flags |= ((4 * hfaFieldSize) << RETURN_FP_SIZE_SHIFT);
                break;
            }
#endif

            size_t size = thValueType.GetSize();

#if defined(TARGET_X86) || defined(TARGET_AMD64)
            // Return value types of size which are not powers of 2 using a RetBuffArg
            if ((size & (size-1)) != 0)
            {
                flags |= RETURN_HAS_RET_BUFFER;
                break;
            }
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

            if  (size <= ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE)
                break;
#endif // UNIX_AMD64_ABI
        }
#endif // ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE

        // Value types are returned using return buffer by default
        flags |= RETURN_HAS_RET_BUFFER;
        break;

    default:
        break;
    }

    m_dwFlags |= flags;
}

template<class ARGITERATOR_BASE>
void ArgIteratorTemplate<ARGITERATOR_BASE>::ForceSigWalk()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
    }
    CONTRACTL_END

    // This can be only used before the actual argument iteration started
    _ASSERTE((m_dwFlags & ITERATION_STARTED) == 0);

#ifdef TARGET_X86
    //
    // x86 is special as always
    //

    int numRegistersUsed = 0;
    int nSizeOfArgStack = 0;

    if (this->HasThis())
        numRegistersUsed++;

    if (this->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
        numRegistersUsed++;

    if (this->IsVarArg())
    {
        nSizeOfArgStack += sizeof(void *);
        numRegistersUsed = NUM_ARGUMENT_REGISTERS; // Nothing else gets passed in registers for varargs
    }

#ifdef FEATURE_INTERPRETER
    BYTE callconv = CallConv();
    switch (callconv)
    {
    case IMAGE_CEE_CS_CALLCONV_C:
    case IMAGE_CEE_CS_CALLCONV_STDCALL:
        numRegistersUsed = NUM_ARGUMENT_REGISTERS;
        nSizeOfArgStack = TransitionBlock::GetOffsetOfArgs() + numRegistersUsed * sizeof(void *);
        break;

    case IMAGE_CEE_CS_CALLCONV_THISCALL:
    case IMAGE_CEE_CS_CALLCONV_FASTCALL:
        _ASSERTE_MSG(false, "Unsupported calling convention.");
    default:
        break;
    }
#endif // FEATURE_INTERPRETER

    DWORD nArgs = this->NumFixedArgs();
    for (DWORD i = 0; i < nArgs; i++)
    {
        TypeHandle thValueType;
        CorElementType type = this->GetNextArgumentType(i, &thValueType);

        if (!IsArgumentInRegister(&numRegistersUsed, type, thValueType))
        {
            int structSize = MetaSig::GetElemSize(type, thValueType);

            nSizeOfArgStack += StackElemSize(structSize);

#ifndef DACCESS_COMPILE
            if (nSizeOfArgStack > MAX_ARG_SIZE)
            {
#ifdef _DEBUG
                // We should not ever throw exception in the "FORBIDGC_LOADER_USE_ENABLED" mode.
                // The contract violation is required to workaround bug in the static contract analyzer.
                _ASSERTE(!FORBIDGC_LOADER_USE_ENABLED());
                CONTRACT_VIOLATION(ThrowsViolation);
#endif
                COMPlusThrow(kNotSupportedException);
            }
#endif
        }
    }

    if (this->HasParamType())
    {
        DWORD paramTypeFlags = 0;
        if (numRegistersUsed < NUM_ARGUMENT_REGISTERS)
        {
            numRegistersUsed++;
            paramTypeFlags = (numRegistersUsed == 1) ?
                PARAM_TYPE_REGISTER_ECX : PARAM_TYPE_REGISTER_EDX;
        }
        else
        {
            nSizeOfArgStack += sizeof(void *);
            paramTypeFlags = PARAM_TYPE_REGISTER_STACK;
        }
        m_dwFlags |= paramTypeFlags;
    }

#else // TARGET_X86

    int maxOffset = TransitionBlock::GetOffsetOfArgs();

    int ofs;
    while (TransitionBlock::InvalidOffset != (ofs = GetNextOffset()))
    {
        int stackElemSize;

#ifdef TARGET_AMD64
#ifdef UNIX_AMD64_ABI
        if (m_fArgInRegisters)
        {
            // Arguments passed in registers don't consume any stack
            continue;
        }

        stackElemSize = StackElemSize(GetArgSize());
#else // UNIX_AMD64_ABI
        // All stack arguments take just one stack slot on AMD64 because of arguments bigger
        // than a stack slot are passed by reference.
        stackElemSize = TARGET_POINTER_SIZE;
#endif // UNIX_AMD64_ABI
#else // TARGET_AMD64

        TypeHandle thValueType;
        const CorElementType argType = GetArgType(&thValueType);
        const bool isValueType = (argType == ELEMENT_TYPE_VALUETYPE);
        stackElemSize = StackElemSize(GetArgSize(), isValueType, thValueType.IsFloatHfa());
#if defined(ENREGISTERED_PARAMTYPE_MAXSIZE)
        if (IsArgPassedByRef())
            stackElemSize = TARGET_POINTER_SIZE;
#endif
#endif // TARGET_AMD64

        int endOfs = ofs + stackElemSize;
        if (endOfs > maxOffset)
        {
#if !defined(DACCESS_COMPILE)
            if (endOfs > MAX_ARG_SIZE)
            {
#ifdef _DEBUG
                // We should not ever throw exception in the "FORBIDGC_LOADER_USE_ENABLED" mode.
                // The contract violation is required to workaround bug in the static contract analyzer.
                _ASSERTE(!FORBIDGC_LOADER_USE_ENABLED());
                CONTRACT_VIOLATION(ThrowsViolation);
#endif
                COMPlusThrow(kNotSupportedException);
            }
#endif
            maxOffset = endOfs;
        }
    }
    // Clear the iterator started flag
    m_dwFlags &= ~ITERATION_STARTED;

    int nSizeOfArgStack = maxOffset - TransitionBlock::GetOffsetOfArgs();

#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
    nSizeOfArgStack = (nSizeOfArgStack > (int)sizeof(ArgumentRegisters)) ?
        (nSizeOfArgStack - sizeof(ArgumentRegisters)) : 0;
#endif

#endif // TARGET_X86

    // arg stack size is rounded to the pointer size on all platforms.
    nSizeOfArgStack = (int)ALIGN_UP(nSizeOfArgStack, TARGET_POINTER_SIZE);

    // Cache the result
    m_nSizeOfArgStack = nSizeOfArgStack;
    m_dwFlags |= SIZE_OF_ARG_STACK_COMPUTED;

    this->Reset();
}

class ArgIteratorBase
{
protected:
    MetaSig * m_pSig;

    FORCEINLINE CorElementType GetReturnType(TypeHandle * pthValueType)
    {
        WRAPPER_NO_CONTRACT;
#ifdef ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
        return m_pSig->GetReturnTypeNormalized(pthValueType);
#else
        return m_pSig->GetReturnTypeNormalized();
#endif
    }

    FORCEINLINE CorElementType GetNextArgumentType(DWORD iArg, TypeHandle * pthValueType)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(iArg == m_pSig->GetArgNum());
        CorElementType et = m_pSig->PeekArgNormalized(pthValueType);
        m_pSig->SkipArg();
        return et;
    }

    FORCEINLINE void Reset()
    {
        WRAPPER_NO_CONTRACT;
        m_pSig->Reset();
    }

    FORCEINLINE BOOL IsRegPassedStruct(MethodTable* pMT)
    {
        return pMT->IsRegPassedStruct();
    }

public:
    BOOL HasThis()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pSig->HasThis();
    }

    BOOL HasParamType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pSig->GetCallingConventionInfo() & CORINFO_CALLCONV_PARAMTYPE;
    }

    BOOL IsVarArg()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pSig->IsVarArg() || m_pSig->IsTreatAsVarArg();
    }

    DWORD NumFixedArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pSig->NumFixedArgs();
    }

#ifdef FEATURE_INTERPRETER
    BYTE CallConv()
    {
        return m_pSig->GetCallingConvention();
    }
#endif // FEATURE_INTERPRETER

    //
    // The following is used by the profiler to dig into the iterator for
    // discovering if the method has a This pointer or a return buffer.
    // Do not use this to re-initialize the signature, use the exposed Init()
    // method in this class.
    //
    MetaSig *GetSig(void)
    {
        return m_pSig;
    }
};

class ArgIterator : public ArgIteratorTemplate<ArgIteratorBase>
{
public:
    ArgIterator(MetaSig * pSig)
    {
        m_pSig = pSig;
    }

    // This API returns true if we are returning a structure in registers instead of using a byref return buffer
    BOOL HasNonStandardByvalReturn()
    {
        WRAPPER_NO_CONTRACT;

#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
        CorElementType type = m_pSig->GetReturnTypeNormalized();
        return (type == ELEMENT_TYPE_VALUETYPE || type == ELEMENT_TYPE_TYPEDBYREF) && !HasRetBuffArg();
#else
        return FALSE;
#endif
    }

    BOOL HasValueTypeReturn()
    {
        WRAPPER_NO_CONTRACT;

        TypeHandle thValueType;
        CorElementType type = m_pSig->GetReturnTypeNormalized(&thValueType);
        // Enums are normalized to their underlying type when passing to and from functions.
        // This occurs in both managed and native calling conventions.
        return type == ELEMENT_TYPE_VALUETYPE && !thValueType.IsEnum();
    }
};

// Conventience helper
inline BOOL HasRetBuffArg(MetaSig * pSig)
{
    WRAPPER_NO_CONTRACT;
    ArgIterator argit(pSig);
    return argit.HasRetBuffArg();
}

#ifdef UNIX_X86_ABI
// For UNIX_X86_ABI and unmanaged function, we always need RetBuf if the return type is VALUETYPE
inline BOOL HasRetBuffArgUnmanagedFixup(MetaSig * pSig)
{
    WRAPPER_NO_CONTRACT;
    // We cannot just pSig->GetReturnType() here since it will return ELEMENT_TYPE_VALUETYPE for enums
    CorElementType type = pSig->GetRetTypeHandleThrowing().GetVerifierCorElementType();
    return type == ELEMENT_TYPE_VALUETYPE;
}
#endif

inline BOOL IsRetBuffPassedAsFirstArg()
{
    WRAPPER_NO_CONTRACT;
#ifndef TARGET_ARM64
    return TRUE;
#else
    return FALSE;
#endif
}

#endif // __CALLING_CONVENTION_INCLUDED
