// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _UNWIND_X86_H
#define _UNWIND_X86_H

// This file is shared between CoreCLR and NativeAOT. Some of the differences are handled
// with the FEATURE_NATIVEAOT and FEATURE_EH_FUNCLETS defines. There are three main methods
// that are used by both runtimes - DecodeGCHdrInfo, UnwindStackFrameX86, and EnumGcRefsX86.
//
// The IN_EH_FUNCLETS and IN_EH_FUNCLETS_COMMA macros are used to specify some parameters
// for the above methods that are specific for a certain runtime or configuration.
#ifdef FEATURE_EH_FUNCLETS
#define IN_EH_FUNCLETS(a) a
#define IN_EH_FUNCLETS_COMMA(a) a,
#else
#define IN_EH_FUNCLETS(a)
#define IN_EH_FUNCLETS_COMMA(a)
#endif

enum regNum
{
        REGI_EAX, REGI_ECX, REGI_EDX, REGI_EBX,
        REGI_ESP, REGI_EBP, REGI_ESI, REGI_EDI,
        REGI_COUNT,
        REGI_NA = REGI_COUNT
};

enum RegMask
{
    RM_EAX = 0x01,
    RM_ECX = 0x02,
    RM_EDX = 0x04,
    RM_EBX = 0x08,
    RM_ESP = 0x10,
    RM_EBP = 0x20,
    RM_ESI = 0x40,
    RM_EDI = 0x80,

    RM_NONE = 0x00,
    RM_ALL = (RM_EAX|RM_ECX|RM_EDX|RM_EBX|RM_ESP|RM_EBP|RM_ESI|RM_EDI),
    RM_CALLEE_SAVED = (RM_EBP|RM_EBX|RM_ESI|RM_EDI),
    RM_CALLEE_TRASHED = (RM_ALL & ~RM_CALLEE_SAVED),
};

#define CONSTRUCT_ptrArgTP(arg,shift)   ptrArgTP((arg), (shift))

// Bit vector structure that can hold MAX_PTRARG_OFS bits and efficiently
// handle small vectors.
class ptrArgTP
{
    typedef UINT_PTR ChunkType;  // The size of integer type that the machine can operate on directly

    enum
    {
        IS_BIG     = 1,                             // The low bit is used to discrimate m_val and m_vals
        CHUNK_BITS = sizeof(ChunkType)*8,           // The number of bits that we can manipuate as a chunk
        SMALL_BITS = CHUNK_BITS - 1,                // The number of bits we can fit in the small representation
        VALS_COUNT = MAX_PTRARG_OFS / CHUNK_BITS,   // The number of ChunkType elements in the Vals array
    };

    static const ChunkType MaxVal = ((ChunkType)1 << SMALL_BITS) - 1;    // Maximum value that can be stored in m_val

    struct Vals
    {
        unsigned m_encodedLength;         // An encoding of the current length of the 'm_chunks' array
        ChunkType m_chunks[VALS_COUNT];

        bool isBig() const
        {
            return ((m_encodedLength & IS_BIG) != 0);
        }

        unsigned GetLength() const
        {
            if (isBig())
            {
                unsigned length = (m_encodedLength >> 1);
                _ASSERTE(length > 0);
                return length;
            }
            else
            {
                return 0;
            }
        }

        void SetLength(unsigned length)
        {
            _ASSERTE(length > 0);
            _ASSERTE(length <= VALS_COUNT);

            m_encodedLength  = (ChunkType) (length << 1);
            m_encodedLength |= (ChunkType) IS_BIG;
        }
    };

    union
    {
        ChunkType m_val;     // if m_val bit 0 is false, then bits 1-N are the bit vector
        Vals      m_vals;    // if m_val bit 1 is true, then use Vals
    };

    bool isBig() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return ((m_val & IS_BIG) != 0);
    }

    void toBig()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (!isBig())
        {
            doBigInit(smallBits());
        }
    }

    ChunkType smallBits() const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(!isBig());
        return (m_val >> 1);
    }

    void doBigInit(ChunkType arg);
    void doBigInit(const ptrArgTP& arg);
    void doBigLeftShiftAssign(unsigned arg);
    void doBigRightShiftAssign(unsigned arg);
    void doBigDiffAssign(const ptrArgTP&);
    void doBigAndAssign(const ptrArgTP&);
    void doBigOrAssign(const ptrArgTP& arg);
    bool doBigEquals(const ptrArgTP&) const;
    bool doBigIntersect(const ptrArgTP&) const;

public:
    ptrArgTP()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        m_val = 0;
    }

    explicit ptrArgTP(ChunkType arg)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (arg > MaxVal)
        {
            doBigInit(arg);
        }
        else
        {
            m_val = ChunkType(arg << 1);
        }
    }

    ptrArgTP(ChunkType arg, UINT shift)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if ((arg > MaxVal) || (shift >= SMALL_BITS) || (arg > (MaxVal >> shift)))
        {
            doBigInit(arg);
            doBigLeftShiftAssign(shift);
        }
        else
        {
            m_val = ChunkType(arg << (shift+1));
        }
    }

    ptrArgTP operator &(const ptrArgTP& arg) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        ptrArgTP ret = *this;
        ret &= arg;
        return ret;
    }

    bool operator ==(const ptrArgTP& arg) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if ((m_val | arg.m_val) & IS_BIG)
        {
            return doBigEquals(arg);
        }
        else
        {
            return m_val == arg.m_val;
        }
    }

    bool operator !=(const ptrArgTP& arg) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return !(*this == arg);
    }

    void operator <<=(unsigned shift)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if ((m_val == 0) || (shift == 0))     // Zero is a special case, don't need to do anything
            return;

        if (isBig() || (shift >= SMALL_BITS) || (m_val > (MaxVal >> (shift-1))))
        {
            doBigLeftShiftAssign(shift);
        }
        else
        {
            m_val <<= shift;
        }
    }

    void operator >>=(unsigned shift)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (isBig())
        {
            doBigRightShiftAssign(shift);
        }
        else
        {
            m_val >>= shift;
            m_val &= ~IS_BIG;  // clear the isBig bit if it got set
        }
    }

    void operator |=(const ptrArgTP& arg)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (((m_val | arg.m_val) & IS_BIG) != 0)
        {
            doBigOrAssign(arg);
        }
        else
        {
            m_val |= arg.m_val;
        }
    }

    void operator &=(const ptrArgTP& arg)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (((m_val | arg.m_val) & IS_BIG) != 0)
        {
            doBigAndAssign(arg);
        }
        else
        {
            m_val &= arg.m_val;
        }
    }

    friend bool isZero(const ptrArgTP& arg)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return arg.m_val == 0;
    }

    friend bool intersect(const ptrArgTP& arg1, const ptrArgTP& arg2)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (((arg1.m_val | arg2.m_val) & IS_BIG) != 0)
        {
            return arg1.doBigIntersect(arg2);
        }
        else
        {
            return ((arg1.m_val & arg2.m_val) != 0);
        }
    }

    friend void setDiff(ptrArgTP& target, const ptrArgTP& arg)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (((target.m_val | arg.m_val) & IS_BIG) != 0)
        {
            target.doBigDiffAssign(arg);
        }
        else
        {
            target.m_val &= ~arg.m_val;
        }
    }

    friend ChunkType toUnsigned(const ptrArgTP& arg)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (arg.isBig())
        {
            return arg.m_vals.m_chunks[0];   // Note truncation
        }
        else
        {
            return arg.smallBits();
        }
    }
};

/*****************************************************************************
 *
 *  Helper to extract basic info from a method info block.
 */

struct hdrInfo
{
    unsigned int        methodSize;     // native code bytes
    unsigned int        argSize;        // in bytes
    unsigned int        stackSize;      // including callee saved registers
    unsigned int        rawStkSize;     // excluding callee saved registers
    ReturnKind          returnKind;     // The ReturnKind for this method.

    unsigned int        prologSize;

    // Size of the epilogs in the method.
    // For methods which use CEE_JMP, some epilogs may end with a "ret" instruction
    // and some may end with a "jmp". The epilogSize reported should be for the
    // epilog with the smallest size.
    unsigned int        epilogSize;

    unsigned char       epilogCnt;
    bool                epilogEnd;      // is the epilog at the end of the method

    bool                ebpFrame;       // locals and arguments addressed relative to EBP
    bool                doubleAlign;    // is the stack double-aligned? locals addressed relative to ESP, and arguments relative to EBP
    bool                interruptible;  // intr. at all times (excluding prolog/epilog), not just call sites

    bool                handlers;       // has callable handlers
    bool                localloc;       // uses localloc
    bool                editNcontinue;  // has been compiled in EnC mode
    bool                varargs;        // is this a varargs routine
    bool                profCallbacks;  // does the method have Enter-Leave callbacks
    bool                genericsContext;// has a reported generic context parameter
    bool                genericsContextIsMethodDesc;// reported generic context parameter is methoddesc
    bool                isSpeculativeStackWalk; // is the stackwalk seeded by an untrusted source (e.g., sampling profiler)?

    // These always includes EBP for EBP-frames and double-aligned-frames
    RegMask             savedRegMask:8; // which callee-saved regs are saved on stack

    // Count of the callee-saved registers, excluding the frame pointer.
    // This does not include EBP for EBP-frames and double-aligned-frames.
    unsigned int        savedRegsCountExclFP;

    unsigned int        untrackedCnt;
    unsigned int        varPtrTableSize;
    unsigned int        argTabOffset;   // INVALID_ARGTAB_OFFSET if argtab must be reached by stepping through ptr tables
    unsigned int        gsCookieOffset; // INVALID_GS_COOKIE_OFFSET if there is no GuardStack cookie

    unsigned int        syncStartOffset; // start/end code offset of the protected region in synchronized methods.
    unsigned int        syncEndOffset;   // INVALID_SYNC_OFFSET if there not synchronized method
    unsigned int        syncEpilogStart; // The start of the epilog. Synchronized methods are guaranteed to have no more than one epilog.
    unsigned int        revPInvokeOffset; // INVALID_REV_PINVOKE_OFFSET if there is no Reverse PInvoke frame

    enum { NOT_IN_PROLOG = -1, NOT_IN_EPILOG = -1 };

    int                 prologOffs;     // NOT_IN_PROLOG if not in prolog
    int                 epilogOffs;     // NOT_IN_EPILOG if not in epilog. It is never 0

    //
    // Results passed back from scanArgRegTable
    //
    regNum              thisPtrResult;  // register holding "this"
    RegMask             regMaskResult;  // registers currently holding GC ptrs
    RegMask            iregMaskResult;  // iptr qualifier for regMaskResult
    unsigned            argHnumResult;
    PTR_CBYTE            argTabResult;  // Table of encoded offsets of pending ptr args
    unsigned              argTabBytes;  // Number of bytes in argTabResult[]

    // These next two are now large structs (i.e 132 bytes each)

    ptrArgTP            argMaskResult;  // pending arguments mask
    ptrArgTP           iargMaskResult;  // iptr qualifier for argMaskResult
};

bool UnwindStackFrameX86(PREGDISPLAY     pContext,
                         PTR_CBYTE       methodStart,
                         DWORD           curOffs,
                         hdrInfo *       info,
                         PTR_CBYTE       table,
                         IN_EH_FUNCLETS_COMMA(PTR_CBYTE       funcletStart)
                         IN_EH_FUNCLETS_COMMA(bool            isFunclet)
                         bool            updateAllRegs);

size_t DecodeGCHdrInfo(GCInfoToken gcInfoToken,
                       unsigned    curOffset,
                       hdrInfo   * infoPtr);

#endif // _UNWIND_X86_H
