// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    int     m_idxFloatReg;  // First floating point register used (or -1)
    int     m_cFloatReg;    // Count of floating point registers used (or 0)

    int     m_idxGenReg;    // First general register used (or -1)
    int     m_cGenReg;      // Count of general registers used (or 0)

    int     m_idxStack;     // First stack slot used (or -1)
    int     m_cStack;       // Count of stack slots used (or 0)

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

    EEClass* m_eeClass;     // For structs passed in register, it points to the EEClass of the struct

#endif // UNIX_AMD64_ABI && FEATURE_UNIX_AMD64_STRUCT_PASSING

#if defined(_TARGET_ARM_)
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
        m_idxStack = -1;
        m_cStack = 0;
#if defined(_TARGET_ARM_)
        m_fRequires64BitAlignment = FALSE;
#endif
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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
#if defined(_TARGET_X86_)
    ArgumentRegisters       m_argumentRegisters;
    CalleeSavedRegisters    m_calleeSavedRegisters;
    TADDR                   m_ReturnAddress;
#elif defined(_TARGET_AMD64_)
#ifdef UNIX_AMD64_ABI
    ArgumentRegisters       m_argumentRegisters;
#endif
    CalleeSavedRegisters    m_calleeSavedRegisters;
    TADDR                   m_ReturnAddress;
#elif defined(_TARGET_ARM_)
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
#elif defined(_TARGET_ARM64_)
    union {
        CalleeSavedRegisters m_calleeSavedRegisters;
        struct {
            INT64 x29; // frame pointer
            TADDR m_ReturnAddress;
            INT64 x19, x20, x21, x22, x23, x24, x25, x26, x27, x28;
        };
    };
    ArgumentRegisters       m_argumentRegisters;
    TADDR padding; // Keep size of TransitionBlock as multiple of 16-byte. Simplifies code in PROLOG_WITH_TRANSITION_BLOCK
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

    static BYTE GetOffsetOfArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return sizeof(TransitionBlock);
    }

    static int GetOffsetOfArgumentRegisters()
    {
        LIMITED_METHOD_CONTRACT;
        int offs;
#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)
        offs = sizeof(TransitionBlock);
#else
        offs = offsetof(TransitionBlock, m_argumentRegisters);
#endif
        return offs;
    }

    static BOOL IsStackArgumentOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        return offset >= sizeof(TransitionBlock);
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

#ifndef _TARGET_X86_
    static UINT GetArgumentIndexFromOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        _ASSERTE(offset != TransitionBlock::StructInRegsOffset);
#endif        
        return (offset - GetOffsetOfArgumentRegisters()) / sizeof(TADDR);
    }

    static UINT GetStackArgumentIndexFromOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;

        return (offset - TransitionBlock::GetOffsetOfArgs()) / STACK_ELEM_SIZE;
    }

#endif

#ifdef CALLDESCR_FPARGREGS
    static BOOL IsFloatArgumentRegisterOffset(int offset)
    {
        LIMITED_METHOD_CONTRACT;
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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
    #if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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
#ifdef _TARGET_ARM_
        negSpaceSize += sizeof(TADDR); // padding to make FloatArgumentRegisters address 8-byte aligned
#endif
        return negSpaceSize;
    }

    static const int InvalidOffset = -1;
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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

#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)
        // The argument registers are not included in the stack size on AMD64
        size += ARGUMENTREGISTERS_SIZE;
#endif

        return size;
    }

    //------------------------------------------------------------------------

#ifdef _TARGET_X86_
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

#ifdef _TARGET_X86_
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
    static BOOL IsArgumentInRegister(int * pNumRegistersUsed, CorElementType typ)
    {
        LIMITED_METHOD_CONTRACT;
        if ( (*pNumRegistersUsed) < NUM_ARGUMENT_REGISTERS) {
            if (gElementTypeInfo[typ].m_enregister) {
                (*pNumRegistersUsed)++;
                return(TRUE);
            }
        }

        return(FALSE);
    }
#endif // _TARGET_X86_

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
#ifdef _TARGET_AMD64_
        return IsArgPassedByRef(size);
#elif defined(_TARGET_ARM64_)
        // Composites greater than 16 bytes are passed by reference
        return ((size > ENREGISTERED_PARAMTYPE_MAXSIZE) && !th.IsHFA());
#else
        PORTABILITY_ASSERT("ArgIteratorTemplate::IsArgPassedByRef");
        return FALSE;
#endif
    }

#ifdef _TARGET_AMD64_
    // This overload should only be used in AMD64-specific code only.
    static BOOL IsArgPassedByRef(size_t size)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        // No arguments are passed by reference on AMD64 on Unix
        return FALSE;
#else
        // If the size is bigger than ENREGISTERED_PARAM_TYPE_MAXSIZE, or if the size is NOT a power of 2, then
        // the argument is passed by reference.
        return (size > ENREGISTERED_PARAMTYPE_MAXSIZE) || ((size & (size-1)) != 0);
#endif        
    }
#endif // _TARGET_AMD64_

    // This overload should be used for varargs only.
    static BOOL IsVarArgPassedByRef(size_t size)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef _TARGET_AMD64_
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        PORTABILITY_ASSERT("ArgIteratorTemplate::IsVarArgPassedByRef");                
        return FALSE;
#else // FEATURE_UNIX_AMD64_STRUCT_PASSING
        return IsArgPassedByRef(size);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

#else
        return (size > ENREGISTERED_PARAMTYPE_MAXSIZE);
#endif
    }

    BOOL IsArgPassedByRef()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef _TARGET_AMD64_
        return IsArgPassedByRef(m_argSize);
#elif defined(_TARGET_ARM64_)
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

#ifndef _TARGET_X86_
    // Accessors for built in argument descriptions of the special implicit parameters not mentioned directly
    // in signatures (this pointer and the like). Whether or not these can be used successfully before all the
    // explicit arguments have been scanned is platform dependent.
    void GetThisLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetThisOffset(), pLoc); }
    void GetRetBuffArgLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetRetBuffArgOffset(), pLoc); }
    void GetParamTypeLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetParamTypeArgOffset(), pLoc); }
    void GetVASigCookieLoc(ArgLocDesc * pLoc) { WRAPPER_NO_CONTRACT; GetSimpleLoc(GetVASigCookieOffset(), pLoc); }
#endif // !_TARGET_X86_

    ArgLocDesc* GetArgLocDescForStructInRegs()
    {
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        return m_hasArgLocDescForStructInRegs ? &m_argLocDescForStructInRegs : NULL;
#else
        return NULL;
#endif
    }

#ifdef _TARGET_ARM_
    // Get layout information for the argument that the ArgIterator is currently visiting.
    void GetArgLoc(int argOffset, ArgLocDesc *pLoc)
    {
        LIMITED_METHOD_CONTRACT;

        pLoc->Init();

        pLoc->m_fRequires64BitAlignment = m_fRequires64BitAlignment;

        int cSlots = (GetArgSize() + 3) / 4;

        if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
        {
            pLoc->m_idxFloatReg = (argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters()) / 4;
            pLoc->m_cFloatReg = cSlots;
            return;
        }

        if (!TransitionBlock::IsStackArgumentOffset(argOffset))
        {
            pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);

            if (cSlots <= (4 - pLoc->m_idxGenReg))
            {
                pLoc->m_cGenReg = cSlots;
            }
            else
            {
                pLoc->m_cGenReg = 4 - pLoc->m_idxGenReg;

                pLoc->m_idxStack = 0;
                pLoc->m_cStack = cSlots - pLoc->m_cGenReg;
            }
        }
        else
        {
            pLoc->m_idxStack = TransitionBlock::GetStackArgumentIndexFromOffset(argOffset);
            pLoc->m_cStack = cSlots;
        }
    }
#endif // _TARGET_ARM_

#ifdef _TARGET_ARM64_
    // Get layout information for the argument that the ArgIterator is currently visiting.
    void GetArgLoc(int argOffset, ArgLocDesc *pLoc)
    {
        LIMITED_METHOD_CONTRACT;

        pLoc->Init();

        if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
        {
            // Dividing by 8 as size of each register in FloatArgumentRegisters is 8 bytes.
            pLoc->m_idxFloatReg = (argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters()) / 8;

            if (!m_argTypeHandle.IsNull() && m_argTypeHandle.IsHFA())
            {
                CorElementType type = m_argTypeHandle.GetHFAType();
                pLoc->m_cFloatReg = (type == ELEMENT_TYPE_R4)? GetArgSize()/sizeof(float): GetArgSize()/sizeof(double);
            }
            else
            {
                pLoc->m_cFloatReg = 1;
            }
            return;
        }

        int cSlots = (GetArgSize() + 7)/ 8;

        // Composites greater than 16bytes are passed by reference
        if (GetArgType() == ELEMENT_TYPE_VALUETYPE && GetArgSize() > ENREGISTERED_PARAMTYPE_MAXSIZE)
        {
            cSlots = 1;
        }

        if (!TransitionBlock::IsStackArgumentOffset(argOffset))
        {
            pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);
            pLoc->m_cGenReg = cSlots;
         }
        else
        {
            pLoc->m_idxStack = TransitionBlock::GetStackArgumentIndexFromOffset(argOffset);
            pLoc->m_cStack = cSlots;
        }
    }
#endif // _TARGET_ARM64_

#if defined(_TARGET_AMD64_) && defined(UNIX_AMD64_ABI)
    // Get layout information for the argument that the ArgIterator is currently visiting.
    void GetArgLoc(int argOffset, ArgLocDesc* pLoc)
    {
        LIMITED_METHOD_CONTRACT;

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        if (m_hasArgLocDescForStructInRegs)
        {
            *pLoc = m_argLocDescForStructInRegs;
            return;
        }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

        if (argOffset == TransitionBlock::StructInRegsOffset)
        {
            // We always already have argLocDesc for structs passed in registers, we 
            // compute it in the GetNextOffset for those since it is always needed.
            _ASSERTE(false);
            return;
        }

        pLoc->Init();

        if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
        {
            // Dividing by 16 as size of each register in FloatArgumentRegisters is 16 bytes.
            pLoc->m_idxFloatReg = (argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters()) / 16;
            pLoc->m_cFloatReg = 1;
        }
        else if (!TransitionBlock::IsStackArgumentOffset(argOffset))
        {
            pLoc->m_idxGenReg = TransitionBlock::GetArgumentIndexFromOffset(argOffset);
            pLoc->m_cGenReg = 1;
        }
        else
        {
            pLoc->m_idxStack = TransitionBlock::GetStackArgumentIndexFromOffset(argOffset);
            pLoc->m_cStack = (GetArgSize() + STACK_ELEM_SIZE - 1) / STACK_ELEM_SIZE;
        }
    }
#endif // _TARGET_AMD64_ && UNIX_AMD64_ABI

protected:
    DWORD               m_dwFlags;              // Cached flags
    int                 m_nSizeOfArgStack;      // Cached value of SizeOfArgStack

    DWORD               m_argNum;

    // Cached information about last argument
    CorElementType      m_argType;
    int                 m_argSize;
    TypeHandle          m_argTypeHandle;
#if defined(_TARGET_AMD64_) && defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    ArgLocDesc          m_argLocDescForStructInRegs;
    bool                m_hasArgLocDescForStructInRegs;
#endif // _TARGET_AMD64_ && UNIX_AMD64_ABI && FEATURE_UNIX_AMD64_STRUCT_PASSING

#ifdef _TARGET_X86_
    int                 m_curOfs;           // Current position of the stack iterator
    int                 m_numRegistersUsed;
#endif

#ifdef _TARGET_AMD64_
#ifdef UNIX_AMD64_ABI
    int                 m_idxGenReg;        // Next general register to be assigned a value
    int                 m_idxStack;         // Next stack slot to be assigned a value
    int                 m_idxFPReg;         // Next floating point register to be assigned a value
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    bool                m_fArgInRegisters;  // Indicates that the current argument is stored in registers
#endif    
#else
    int                 m_curOfs;           // Current position of the stack iterator
#endif
#endif

#ifdef _TARGET_ARM_
    int                 m_idxGenReg;        // Next general register to be assigned a value
    int                 m_idxStack;         // Next stack slot to be assigned a value

    WORD                m_wFPRegs;          // Bitmask of available floating point argument registers (s0-s15/d0-d7)
    bool                m_fRequires64BitAlignment; // Cached info about the current arg
#endif

#ifdef _TARGET_ARM64_
    int             m_idxGenReg;        // Next general register to be assigned a value
    int             m_idxStack;         // Next stack slot to be assigned a value
    int             m_idxFPReg;         // Next FP register to be assigned a value
#endif

    enum {
        ITERATION_STARTED               = 0x0001,   // Started iterating over arguments
        SIZE_OF_ARG_STACK_COMPUTED      = 0x0002,
        RETURN_FLAGS_COMPUTED           = 0x0004,
        RETURN_HAS_RET_BUFFER           = 0x0008,   // Cached value of HasRetBuffArg

#ifdef _TARGET_X86_
        PARAM_TYPE_REGISTER_MASK        = 0x0030,
        PARAM_TYPE_REGISTER_STACK       = 0x0010,
        PARAM_TYPE_REGISTER_ECX         = 0x0020,
        PARAM_TYPE_REGISTER_EDX         = 0x0030,
#endif

        METHOD_INVOKE_NEEDS_ACTIVATION  = 0x0040,   // Flag used by ArgIteratorForMethodInvoke

        RETURN_FP_SIZE_SHIFT            = 8,        // The rest of the flags is cached value of GetFPReturnSize
    };

    void ComputeReturnFlags();

#ifndef _TARGET_X86_
    void GetSimpleLoc(int offset, ArgLocDesc * pLoc)
    { 
        WRAPPER_NO_CONTRACT; 
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

#ifdef _TARGET_X86_
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

#if _TARGET_X86_
    // x86 is special as always
    ret += this->HasThis() ? offsetof(ArgumentRegisters, EDX) : offsetof(ArgumentRegisters, ECX);
#elif _TARGET_ARM64_
    ret += (int) offsetof(ArgumentRegisters, x[8]);
#else
    if (this->HasThis())
        ret += sizeof(void *);
#endif

    return ret;
}

template<class ARGITERATOR_BASE>
int ArgIteratorTemplate<ARGITERATOR_BASE>::GetVASigCookieOffset()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(this->IsVarArg());

#if defined(_TARGET_X86_)
    // x86 is special as always
    return sizeof(TransitionBlock);
#else
    // VaSig cookie is after this and retbuf arguments by default.
    int ret = TransitionBlock::GetOffsetOfArgumentRegisters();

    if (this->HasThis())
    {
        ret += sizeof(void*);
    }

    if (this->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
    {
        ret += sizeof(void*);
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

#ifdef _TARGET_X86_
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
        ret += sizeof(void*);
    }

    if (this->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
    {
        ret += sizeof(void*);
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

#ifndef _TARGET_X86_
        if (this->IsVarArg() || this->HasParamType())
        {
            numRegistersUsed++;
        }
#endif

#ifdef _TARGET_X86_
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
            m_curOfs = TransitionBlock::GetOffsetOfArgs() + numRegistersUsed * sizeof(void *); 
            m_fUnmanagedCallConv = true;
            break;

        case IMAGE_CEE_CS_CALLCONV_THISCALL:
        case IMAGE_CEE_CS_CALLCONV_FASTCALL:
            _ASSERTE_MSG(false, "Unsupported calling convention.");

        default:
            m_fUnmanagedCallConv = false;
            m_numRegistersUsed = numRegistersUsed;
            m_curOfs = TransitionBlock::GetOffsetOfArgs() + SizeOfArgStack();
        }
#else
        m_numRegistersUsed = numRegistersUsed;
        m_curOfs = TransitionBlock::GetOffsetOfArgs() + SizeOfArgStack();
#endif

#elif defined(_TARGET_AMD64_)
#ifdef UNIX_AMD64_ABI
        m_idxGenReg = numRegistersUsed;
        m_idxStack = 0;
        m_idxFPReg = 0;
#else
        m_curOfs = TransitionBlock::GetOffsetOfArgs() + numRegistersUsed * sizeof(void *);
#endif
#elif defined(_TARGET_ARM_)
        m_idxGenReg = numRegistersUsed;
        m_idxStack = 0;

        m_wFPRegs = 0;
#elif defined(_TARGET_ARM64_)
        m_idxGenReg = numRegistersUsed;
        m_idxStack = 0;

        m_idxFPReg = 0;
#else
        PORTABILITY_ASSERT("ArgIteratorTemplate::GetNextOffset");
#endif

        m_argNum = 0;

        m_dwFlags |= ITERATION_STARTED;
    }

    if (m_argNum == this->NumFixedArgs())
        return TransitionBlock::InvalidOffset;

    TypeHandle thValueType;
    CorElementType argType = this->GetNextArgumentType(m_argNum++, &thValueType);

    int argSize = MetaSig::GetElemSize(argType, thValueType);

    m_argType = argType;
    m_argSize = argSize;
    m_argTypeHandle = thValueType;

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    m_hasArgLocDescForStructInRegs = false;
#endif

#ifdef _TARGET_X86_
#ifdef FEATURE_INTERPRETER
    if (m_fUnmanagedCallConv)
    {
        int argOfs = m_curOfs;
        m_curOfs += StackElemSize(argSize);
        return argOfs;
    }
#endif
    if (IsArgumentInRegister(&m_numRegistersUsed, argType))
    {
        return TransitionBlock::GetOffsetOfArgumentRegisters() + (NUM_ARGUMENT_REGISTERS - m_numRegistersUsed) * sizeof(void *);
    }

    m_curOfs -= StackElemSize(argSize);
    _ASSERTE(m_curOfs >= TransitionBlock::GetOffsetOfArgs());
    return m_curOfs;
#elif defined(_TARGET_AMD64_)
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
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        MethodTable *pMT = m_argTypeHandle.AsMethodTable();
        if (pMT->IsRegPassedStruct())
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

#else // FEATURE_UNIX_AMD64_STRUCT_PASSING
        argSize = sizeof(TADDR);        
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

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

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    m_fArgInRegisters = false;
#endif        

    int argOfs = TransitionBlock::GetOffsetOfArgs() + m_idxStack * STACK_ELEM_SIZE;

    int cArgSlots = cbArg / STACK_ELEM_SIZE;
    m_idxStack += cArgSlots;

    return argOfs;
#else
    // Each argument takes exactly one slot on AMD64 on Windows
    int argOfs = m_curOfs;
    m_curOfs += sizeof(void *);
    return argOfs;
#endif
#elif defined(_TARGET_ARM_)
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
            fFloatingPoint = true;
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
    int cArgSlots = cbArg / 4;

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
            m_idxStack = ALIGN_UP(m_idxStack, 2);

        // Indicate the stack location of the argument to the caller.
        int argOfs = TransitionBlock::GetOffsetOfArgs() + m_idxStack * 4;

        // Record the stack usage.
        m_idxStack += cArgSlots;

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
            m_idxGenReg = ALIGN_UP(m_idxGenReg, 2);
        }

        int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_idxGenReg * 4;

        int cRemainingRegs = 4 - m_idxGenReg;
        if (cArgSlots <= cRemainingRegs)
        {
            // Mark the registers just allocated as used.
            m_idxGenReg += cArgSlots;
            return argOfs;
        }

        // The ABI supports splitting a non-FP argument across registers and the stack. But this is
        // disabled if the FP arguments already overflowed onto the stack (i.e. the stack index is not
        // zero). The following code marks the general argument registers as exhausted if this condition
        // holds.  See steps C.5 in the algorithm in the ABI spec.

        m_idxGenReg = 4;

        if (m_idxStack == 0)
        {
            m_idxStack += cArgSlots - cRemainingRegs;
            return argOfs;
        }
    }

    if (fRequiresAlign64Bit)
    {
        // The argument requires 64-bit alignment. If it is going to be passed on the stack, align
        // the next stack slot.  See step C.6 in the algorithm in the ABI spec.  
        m_idxStack = ALIGN_UP(m_idxStack, 2);
    }

    int argOfs = TransitionBlock::GetOffsetOfArgs() + m_idxStack * 4;

    // Advance the stack pointer over the argument just placed.
    m_idxStack += cArgSlots;

    return argOfs;
#elif defined(_TARGET_ARM64_)

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
        // Handle HFAs: packed structures of 2-4 floats or doubles that are passed in FP argument
        // registers if possible.
        if (thValueType.IsHFA())
        {
            CorElementType type = thValueType.GetHFAType();
            cFPRegs = (type == ELEMENT_TYPE_R4)? (argSize/sizeof(float)): (argSize/sizeof(double));
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

    int cbArg = StackElemSize(argSize);
    int cArgSlots = cbArg / STACK_ELEM_SIZE;

    if (cFPRegs>0 && !this->IsVarArg())
    {
        if (cFPRegs + m_idxFPReg <= 8)
        {
            int argOfs = TransitionBlock::GetOffsetOfFloatArgumentRegisters() + m_idxFPReg * 8;
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
        if (m_idxGenReg + cArgSlots <= 8)
        {
            int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_idxGenReg * 8;
            m_idxGenReg += cArgSlots;
            return argOfs;
        }
        else
        {
            m_idxGenReg = 8;
        }
    }

    int argOfs = TransitionBlock::GetOffsetOfArgs() + m_idxStack * 8;
    m_idxStack += cArgSlots;
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
        flags |= sizeof(float) << RETURN_FP_SIZE_SHIFT;
        break;

    case ELEMENT_TYPE_R8:
        flags |= sizeof(double) << RETURN_FP_SIZE_SHIFT;
        break;

    case ELEMENT_TYPE_VALUETYPE:
#ifdef ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
        {
            _ASSERTE(!thValueType.IsNull());

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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
#else // UNIX_AMD64_ABI && FEATURE_UNIX_AMD64_STRUCT_PASSING

#ifdef FEATURE_HFA
            if (thValueType.IsHFA() && !this->IsVarArg())
            {
                CorElementType hfaType = thValueType.GetHFAType();

                flags |= (hfaType == ELEMENT_TYPE_R4) ? 
                    ((4 * sizeof(float)) << RETURN_FP_SIZE_SHIFT) : 
                    ((4 * sizeof(double)) << RETURN_FP_SIZE_SHIFT);

                break;
            }
#endif

            size_t size = thValueType.GetSize();

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
            // Return value types of size which are not powers of 2 using a RetBuffArg
            if ((size & (size-1)) != 0)
            {
                flags |= RETURN_HAS_RET_BUFFER;
                break;
            }
#endif

            if  (size <= ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE)
                break;
#endif // UNIX_AMD64_ABI && FEATURE_UNIX_AMD64_STRUCT_PASSING
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

#ifdef _TARGET_X86_
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
     }
#endif // FEATURE_INTERPRETER

    DWORD nArgs = this->NumFixedArgs();
    for (DWORD i = 0; i < nArgs; i++)
    {        
        TypeHandle thValueType;        
        CorElementType type = this->GetNextArgumentType(i, &thValueType);

        if (!IsArgumentInRegister(&numRegistersUsed, type))
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

#else // _TARGET_X86_

    int maxOffset = TransitionBlock::GetOffsetOfArgs();

    int ofs;
    while (TransitionBlock::InvalidOffset != (ofs = GetNextOffset()))
    {
        int stackElemSize;

#ifdef _TARGET_AMD64_
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        if (m_fArgInRegisters)
        {
            // Arguments passed in registers don't consume any stack 
            continue;
        }

        stackElemSize = StackElemSize(GetArgSize());
#else // FEATURE_UNIX_AMD64_STRUCT_PASSING
        // All stack arguments take just one stack slot on AMD64 because of arguments bigger 
        // than a stack slot are passed by reference. 
        stackElemSize = STACK_ELEM_SIZE;
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
#else // _TARGET_AMD64_
        stackElemSize = StackElemSize(GetArgSize());
#if defined(ENREGISTERED_PARAMTYPE_MAXSIZE)
        if (IsArgPassedByRef())
            stackElemSize = STACK_ELEM_SIZE;
#endif
#endif // _TARGET_AMD64_

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

#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)
    nSizeOfArgStack = (nSizeOfArgStack > (int)sizeof(ArgumentRegisters)) ?
        (nSizeOfArgStack - sizeof(ArgumentRegisters)) : 0;
#endif

#endif // _TARGET_X86_

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
};

// Conventience helper
inline BOOL HasRetBuffArg(MetaSig * pSig)
{
    WRAPPER_NO_CONTRACT;
    ArgIterator argit(pSig);
    return argit.HasRetBuffArg();
}

inline BOOL IsRetBuffPassedAsFirstArg()
{
    WRAPPER_NO_CONTRACT;
#ifndef _TARGET_ARM64_
    return TRUE;
#else
    return FALSE;
#endif        
}

#endif // __CALLING_CONVENTION_INCLUDED
