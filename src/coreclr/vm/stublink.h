// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// STUBLINK.H
//

//
// A StubLinker object provides a way to link several location-independent
// code sources into one executable stub, resolving references,
// and choosing the shortest possible instruction size. The StubLinker
// abstracts out the notion of a "reference" so it is completely CPU
// independent. This StubLinker is intended not only to create method
// stubs but to create the PCode-marshaling stubs for Native/Direct.
//
// A StubLinker's typical life-cycle is:
//
//   1. Create a new StubLinker (it accumulates state for the stub being
//      generated.)
//   2. Emit code bytes and references (requiring fixups) into the StubLinker.
//   3. Call the Link() method to produce the final stub.
//   4. Destroy the StubLinker.
//
// StubLinkers are not multithread-aware: they're intended to be
// used entirely on a single thread. Also, StubLinker's report errors
// using COMPlusThrow. StubLinker's do have a destructor: to prevent
// C++ object unwinding from clashing with COMPlusThrow,
// you must use COMPLUSCATCH to ensure the StubLinker's cleanup in the
// event of an exception: the following code would do it:
//
//  StubLinker stublink;
//  Inner();
//
//
//  // Have to separate into inner function because VC++ forbids
//  // mixing __try & local objects in the same function.
//  void Inner() {
//      COMPLUSTRY {
//          ... do stuff ...
//          pLinker->Link();
//      } COMPLUSCATCH {
//      }
//  }
//


// This file should only be included via the platform-specific cgencpu.h.

#include "cgensys.h"

#ifndef __stublink_h__
#define __stublink_h__

#include "crst.h"
#include "util.hpp"
#include "eecontract.h"

//-------------------------------------------------------------------------
// Forward refs
//-------------------------------------------------------------------------
class  InstructionFormat;
class  Stub;
class  InterceptStub;
class  CheckDuplicatedStructLayouts;
class  CodeBasedStubCache;
struct  CodeLabel;

struct CodeRun;
struct LabelRef;
struct CodeElement;
struct IntermediateUnwindInfo;

#if !defined(TARGET_X86) && !defined(TARGET_UNIX)
#define STUBLINKER_GENERATES_UNWIND_INFO
#endif // !TARGET_X86 && !TARGET_UNIX


#ifdef STUBLINKER_GENERATES_UNWIND_INFO

typedef DPTR(struct StubUnwindInfoHeaderSuffix) PTR_StubUnwindInfoHeaderSuffix;
struct StubUnwindInfoHeaderSuffix
{
    UCHAR nUnwindInfoSize;  // Size of unwind info in bytes
};

// Variable-sized struct that preceeds a Stub when the stub requires unwind
// information.  Followed by a StubUnwindInfoHeaderSuffix.
typedef DPTR(struct StubUnwindInfoHeader) PTR_StubUnwindInfoHeader;
struct StubUnwindInfoHeader
{
    PTR_StubUnwindInfoHeader pNext;
    T_RUNTIME_FUNCTION FunctionEntry;
    UNWIND_INFO UnwindInfo;  // variable length

    // Computes the size needed for this variable-sized struct.
    static SIZE_T ComputeSize(UINT nUnwindInfoSize);

    void Init ();

    bool IsRegistered ();
};

// List of stub address ranges, in increasing address order.
struct StubUnwindInfoHeapSegment
{
    PBYTE pbBaseAddress;
    SIZE_T cbSegment;
    StubUnwindInfoHeader *pUnwindHeaderList;
    StubUnwindInfoHeapSegment *pNext;

#ifdef HOST_64BIT
    class UnwindInfoTable* pUnwindInfoTable;       // Used to publish unwind info to ETW stack crawler
#endif
};

VOID UnregisterUnwindInfoInLoaderHeap (UnlockedLoaderHeap *pHeap);

#endif // STUBLINKER_GENERATES_UNWIND_INFO


//-------------------------------------------------------------------------
// A non-multithreaded object that fixes up and emits one executable stub.
//-------------------------------------------------------------------------
class StubLinker
{
    public:
        //---------------------------------------------------------------
        // Construction
        //---------------------------------------------------------------
        StubLinker();


        //---------------------------------------------------------------
        // Create a new undefined label. Label must be assigned to a code
        // location using EmitLabel() prior to final linking.
        // Throws exception on failure.
        //---------------------------------------------------------------
        CodeLabel* NewCodeLabel();

        //---------------------------------------------------------------
        // Create a new undefined label for which we want the absolute
        // address, not offset. Label must be assigned to a code
        // location using EmitLabel() prior to final linking.
        // Throws exception on failure.
        //---------------------------------------------------------------
        CodeLabel* NewAbsoluteCodeLabel();

        //---------------------------------------------------------------
        // Combines NewCodeLabel() and EmitLabel() for convenience.
        // Throws exception on failure.
        //---------------------------------------------------------------
        CodeLabel* EmitNewCodeLabel();


        //---------------------------------------------------------------
        // Returns final location of label as an offset from the start
        // of the stub. Can only be called after linkage.
        //---------------------------------------------------------------
        UINT32 GetLabelOffset(CodeLabel *pLabel);

        //---------------------------------------------------------------
        // Append code bytes.
        //---------------------------------------------------------------
        VOID EmitBytes(const BYTE *pBytes, UINT numBytes);
        VOID Emit8 (unsigned __int8  u8);
        VOID Emit16(unsigned __int16 u16);
        VOID Emit32(unsigned __int32 u32);
        VOID Emit64(unsigned __int64 u64);
        VOID EmitPtr(const VOID *pval);

        //---------------------------------------------------------------
        // Emit a UTF8 string
        //---------------------------------------------------------------
        VOID EmitUtf8(LPCUTF8 pUTF8)
        {
            WRAPPER_NO_CONTRACT;

            LPCUTF8 p = pUTF8;
            while (*(p++)) {
                //nothing
            }
            EmitBytes((const BYTE *)pUTF8, (unsigned int)(p-pUTF8-1));
        }

        //---------------------------------------------------------------
        // Append an instruction containing a reference to a label.
        //
        //      target             - the label being referenced.
        //      instructionFormat  - a platform-specific InstructionFormat object
        //                           that gives properties about the reference.
        //      variationCode      - uninterpreted data passed to the pInstructionFormat methods.
        //---------------------------------------------------------------
        VOID EmitLabelRef(CodeLabel* target, const InstructionFormat & instructionFormat, UINT variationCode);


        //---------------------------------------------------------------
        // Sets the label to point to the current "instruction pointer"
        // It is invalid to call EmitLabel() twice on
        // the same label.
        //---------------------------------------------------------------
        VOID EmitLabel(CodeLabel* pCodeLabel);

        //---------------------------------------------------------------
        // Emits the patch label for the stub.
        // Throws exception on failure.
        //---------------------------------------------------------------
        void EmitPatchLabel();

        //---------------------------------------------------------------
        // Create a new label to an external address.
        // Throws exception on failure.
        //---------------------------------------------------------------
        CodeLabel* NewExternalCodeLabel(LPVOID pExternalAddress);
        CodeLabel* NewExternalCodeLabel(PCODE pExternalAddress)
        {
            return NewExternalCodeLabel((LPVOID)pExternalAddress);
        }

        //---------------------------------------------------------------
        // Push and Pop can be used to keep track of stack growth.
        // These should be adjusted by opcodes written to the stream.
        //
        // Note that popping & pushing stack size as opcodes are emitted
        // is naive & may not be accurate in many cases,
        // so complex stubs may have to manually adjust the stack size.
        // However it should work for the vast majority of cases we care
        // about.
        //---------------------------------------------------------------
        void Push(UINT size);
        void Pop(UINT size);

        INT GetStackSize() { LIMITED_METHOD_CONTRACT; return m_stackSize; }
        void SetStackSize(SHORT size) { LIMITED_METHOD_CONTRACT; m_stackSize = size; }

        void SetDataOnly(BOOL fDataOnly = TRUE) { LIMITED_METHOD_CONTRACT; m_fDataOnly = fDataOnly; }

#ifdef TARGET_ARM
        void DescribeProlog(UINT cCalleeSavedRegs, UINT cbStackFrame, BOOL fPushArgRegs);
#elif defined(TARGET_ARM64)
        void DescribeProlog(UINT cIntRegArgs, UINT cVecRegArgs, UINT cCalleeSavedRegs, UINT cbStackFrame);
        UINT GetSavedRegArgsOffset();
        UINT GetStackFrameSize();
#endif

        //===========================================================================
        // Unwind information

        // Records location of preserved or parameter register
        VOID UnwindSavedReg (UCHAR reg, ULONG SPRelativeOffset);
        VOID UnwindPushedReg (UCHAR reg);

        // Records "sub rsp, xxx"
        VOID UnwindAllocStack (SHORT FrameSizeIncrement);

        // Records frame pointer register
        VOID UnwindSetFramePointer (UCHAR reg);

        // In DEBUG, emits a call to m_pUnwindInfoCheckLabel (via
        // EmitUnwindInfoCheckWorker).  Code at that label will call to a
        // helper that will attempt to RtlVirtualUnwind through the stub.  The
        // helper will preserve ALL registers.
        VOID EmitUnwindInfoCheck();

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO) && !defined(CROSSGEN_COMPILE)
protected:

        // Injects a call to the given label.
        virtual VOID EmitUnwindInfoCheckWorker (CodeLabel *pCheckLabel) { _ASSERTE(!"override me"); }

        // Emits a call to a helper that will attempt to RtlVirtualUnwind
        // through the stub.  The helper will preserve ALL registers.
        virtual VOID EmitUnwindInfoCheckSubfunction() { _ASSERTE(!"override me"); }
#endif

public:

        //---------------------------------------------------------------
        // Generate the actual stub. The returned stub has a refcount of 1.
        // No other methods (other than the destructor) should be called
        // after calling Link().
        //
        // Throws exception on failure.
        //---------------------------------------------------------------
        Stub *Link(LoaderHeap *heap, DWORD flags = 0);

    private:
        CodeElement   *m_pCodeElements;     // stored in *reverse* order
        CodeLabel     *m_pFirstCodeLabel;   // linked list of CodeLabels
        LabelRef      *m_pFirstLabelRef;    // linked list of references
        CodeLabel     *m_pPatchLabel;       // label of stub patch offset
                                            // currently just for multicast
                                            // frames.
        SHORT         m_stackSize;          // count of pushes/pops
        CQuickHeap    m_quickHeap;          // throwaway heap for
                                            //   labels, and
                                            //   internals.
        BOOL          m_fDataOnly;          // the stub contains only data - does not need FlushInstructionCache

#ifdef TARGET_ARM
protected:
        BOOL            m_fProlog;              // True if DescribeProlog has been called
        UINT            m_cCalleeSavedRegs;     // Count of callee saved registers (0 == none, 1 == r4, 2 ==
                                                // r4-r5 etc. up to 8 == r4-r11)
        UINT            m_cbStackFrame;         // Count of bytes in the stack frame (excl of saved regs)
        BOOL            m_fPushArgRegs;         // If true, r0-r3 are saved before callee saved regs
#endif // TARGET_ARM

#ifdef TARGET_ARM64
protected:
        BOOL            m_fProlog;              // True if DescribeProlog has been called
        UINT            m_cIntRegArgs;          // Count of int register arguments (x0 - x7)
        UINT            m_cVecRegArgs;          // Count of FP register arguments (v0 - v7)
        UINT            m_cCalleeSavedRegs;     // Count of callee saved registers (x19 - x28)
        UINT            m_cbStackSpace;         // Additional stack space for return buffer and stack alignment
#endif // TARGET_ARM64

#ifdef STUBLINKER_GENERATES_UNWIND_INFO

#ifdef _DEBUG
        CodeLabel     *m_pUnwindInfoCheckLabel;  // subfunction to call to unwind info check helper.
                                                 // On AMD64, the prologue is restricted to 256
                                                 // bytes, so this reduces the size of the injected
                                                 // code from 14 to 5 bytes.
#endif

#ifdef TARGET_AMD64
        IntermediateUnwindInfo *m_pUnwindInfoList;
        UINT          m_nUnwindSlots;       // number of slots to allocate at end, == UNWIND_INFO::CountOfCodes
        BOOL          m_fHaveFramePointer;  // indicates stack operations no longer need to be recorded

        //
        // Returns total UnwindInfoSize, including RUNTIME_FUNCTION entry
        //
        UINT UnwindInfoSize(UINT codeSize)
        {
            if (m_nUnwindSlots == 0) return 0;

            return sizeof(T_RUNTIME_FUNCTION) + offsetof(UNWIND_INFO, UnwindCode) + m_nUnwindSlots * sizeof(UNWIND_CODE);
        }
#endif // TARGET_AMD64

#ifdef TARGET_ARM
#define MAX_UNWIND_CODE_WORDS 5  /* maximum number of 32-bit words to store unwind codes */
        // Cache information about the stack frame set up in the prolog and use it in the generation of the
        // epilog.
private:
        // Reserve fixed size block that's big enough to fit any unwind info we can have
        static const int c_nUnwindInfoSize = sizeof(T_RUNTIME_FUNCTION) + sizeof(DWORD) + MAX_UNWIND_CODE_WORDS *4;

        //
        // Returns total UnwindInfoSize, including RUNTIME_FUNCTION entry
        //
        UINT UnwindInfoSize(UINT codeSize)
        {
            if (!m_fProlog) return 0;

            return c_nUnwindInfoSize;
        }
#endif // TARGET_ARM

#ifdef TARGET_ARM64
#define MAX_UNWIND_CODE_WORDS 5  /* maximum number of 32-bit words to store unwind codes */

private:
        // Reserve fixed size block that's big enough to fit any unwind info we can have
        static const int c_nUnwindInfoSize = sizeof(T_RUNTIME_FUNCTION) + sizeof(DWORD) + MAX_UNWIND_CODE_WORDS *4;
        UINT UnwindInfoSize(UINT codeSize)
        {
            if (!m_fProlog) return 0;

            return c_nUnwindInfoSize;
        }

#endif // TARGET_ARM64

#endif // STUBLINKER_GENERATES_UNWIND_INFO

        CodeRun *AppendNewEmptyCodeRun();


        // Returns pointer to last CodeElement or NULL.
        CodeElement *GetLastCodeElement()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pCodeElements;
        }

        // Appends a new CodeElement.
        VOID AppendCodeElement(CodeElement *pCodeElement);


        // Calculates the size of the stub code that is allocate
        // immediately after the stub object. Returns the
        // total size. GlobalSize contains the size without
        // that data part.
        virtual int CalculateSize(int* globalsize);

        // Writes out the code element into memory following the
        // stub object.
        bool EmitStub(Stub* pStub, int globalsize, LoaderHeap* pHeap);

        CodeRun *GetLastCodeRunIfAny();

        bool EmitUnwindInfo(Stub* pStub, int globalsize, LoaderHeap* pHeap);

#if defined(TARGET_AMD64) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
        UNWIND_CODE *AllocUnwindInfo (UCHAR Op, UCHAR nExtraSlots = 0);
#endif // defined(TARGET_AMD64) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
};

//************************************************************************
// CodeLabel
//************************************************************************
struct CodeLabel
{
    // Link pointer for StubLink's list of labels
    CodeLabel       *m_next;

    // if FALSE, label refers to some code within the same stub
    // if TRUE, label refers to some externally supplied address.
    BOOL             m_fExternal;

    // if TRUE, means we want the actual address of the label and
    // not an offset to it
    BOOL             m_fAbsolute;

    union {

        // Internal
        struct {
            // Indicates the position of the label, expressed
            // as an offset into a CodeRun.
            CodeRun         *m_pCodeRun;
            UINT             m_localOffset;

        } i;


        // External
        struct {
            LPVOID           m_pExternalAddress;
        } e;
    };
};

enum NewStubFlags
{
    NEWSTUB_FL_MULTICAST        = 0x00000002,
    NEWSTUB_FL_EXTERNAL         = 0x00000004,
    NEWSTUB_FL_LOADERHEAP       = 0x00000008
};


//-------------------------------------------------------------------------
// An executable stub. These can only be created by the StubLinker().
// Each stub has a reference count (which is maintained in a thread-safe
// manner.) When the ref-count goes to zero, the stub automatically
// cleans itself up.
//-------------------------------------------------------------------------
typedef DPTR(class Stub) PTR_Stub;
typedef DPTR(PTR_Stub) PTR_PTR_Stub;
class Stub
{
    friend class CheckDuplicatedStructLayouts;
    friend class CheckAsmOffsets;

    protected:
    enum
    {
        MULTICAST_DELEGATE_BIT = 0x80000000,
        EXTERNAL_ENTRY_BIT     = 0x40000000,
        LOADER_HEAP_BIT        = 0x20000000,
        UNWIND_INFO_BIT        = 0x08000000,

        PATCH_OFFSET_MASK      = UNWIND_INFO_BIT - 1,
        MAX_PATCH_OFFSET       = PATCH_OFFSET_MASK + 1,
    };

    static_assert_no_msg(PATCH_OFFSET_MASK < UNWIND_INFO_BIT);

    public:
        //-------------------------------------------------------------------
        // Inc the refcount.
        //-------------------------------------------------------------------
        VOID IncRef();


        //-------------------------------------------------------------------
        // Dec the refcount.
        // Returns true if the count went to zero and the stub was deleted
        //-------------------------------------------------------------------
        BOOL DecRef();



        //-------------------------------------------------------------------
        // Used for throwing out unused stubs from stub caches. This
        // method cannot be 100% accurate due to race conditions. This
        // is ok because stub cache management is robust in the face
        // of missed or premature cleanups.
        //-------------------------------------------------------------------
        BOOL HeuristicLooksOrphaned()
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(m_signature == kUsedStub);
            return (m_refcount == 1);
        }

        //-------------------------------------------------------------------
        // Used by the debugger to help step through stubs
        //-------------------------------------------------------------------
        BOOL IsMulticastDelegate()
        {
            LIMITED_METHOD_CONTRACT;
            return (m_patchOffset & MULTICAST_DELEGATE_BIT) != 0;
        }

        //-------------------------------------------------------------------
        // For stubs which execute user code, a patch offset needs to be set
        // to tell the debugger how far into the stub code the debugger has
        // to step until the frame is set up.
        //-------------------------------------------------------------------
        USHORT GetPatchOffset()
        {
            LIMITED_METHOD_CONTRACT;

            return (USHORT)(m_patchOffset & PATCH_OFFSET_MASK);
        }

        void SetPatchOffset(USHORT offset)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(GetPatchOffset() == 0);
            m_patchOffset |= offset;
            _ASSERTE(GetPatchOffset() == offset);
        }

        TADDR GetPatchAddress()
        {
            WRAPPER_NO_CONTRACT;

            return dac_cast<TADDR>(GetEntryPointInternal()) + GetPatchOffset();
        }

        //-------------------------------------------------------------------
        // Unwind information.
        //-------------------------------------------------------------------

#ifdef STUBLINKER_GENERATES_UNWIND_INFO

        BOOL HasUnwindInfo()
        {
            LIMITED_METHOD_CONTRACT;
            return (m_patchOffset & UNWIND_INFO_BIT) != 0;
        }

        StubUnwindInfoHeaderSuffix *GetUnwindInfoHeaderSuffix()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                FORBID_FAULT;
            }
            CONTRACTL_END

            _ASSERTE(HasUnwindInfo());

            TADDR info = dac_cast<TADDR>(this);

            return PTR_StubUnwindInfoHeaderSuffix
                (info - sizeof(StubUnwindInfoHeaderSuffix));
        }

        StubUnwindInfoHeader *GetUnwindInfoHeader()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                FORBID_FAULT;
            }
            CONTRACTL_END

            StubUnwindInfoHeaderSuffix *pSuffix = GetUnwindInfoHeaderSuffix();

            TADDR suffixEnd = dac_cast<TADDR>(pSuffix) + sizeof(*pSuffix);

            return PTR_StubUnwindInfoHeader(suffixEnd -
                                            StubUnwindInfoHeader::ComputeSize(pSuffix->nUnwindInfoSize));
        }

#endif // STUBLINKER_GENERATES_UNWIND_INFO

        //-------------------------------------------------------------------
        // Returns pointer to the start of the allocation containing this Stub.
        //-------------------------------------------------------------------
        TADDR GetAllocationBase();

        //-------------------------------------------------------------------
        // Return executable entrypoint after checking the ref count.
        //-------------------------------------------------------------------
        PCODE GetEntryPoint()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(m_signature == kUsedStub);
            _ASSERTE(m_refcount > 0);

            TADDR pEntryPoint = dac_cast<TADDR>(GetEntryPointInternal());

#ifdef TARGET_ARM

#ifndef THUMB_CODE
#define THUMB_CODE 1
#endif

            pEntryPoint |= THUMB_CODE;
#endif

            return pEntryPoint;
        }

        UINT GetNumCodeBytes()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            return m_numCodeBytes;
        }

        //-------------------------------------------------------------------
        // Return start of the stub blob
        //-------------------------------------------------------------------
        PTR_CBYTE GetBlob()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(m_signature == kUsedStub);
            _ASSERTE(m_refcount > 0);

            return GetEntryPointInternal();
        }

        //-------------------------------------------------------------------
        // Return the Stub as in GetEntryPoint and size of the stub+code in bytes
        //   WARNING: Depending on the stub kind this may be just Stub size as
        //            not all stubs have the info about the code size.
        //            It's the caller responsibility to determine that
        //-------------------------------------------------------------------
        static Stub* RecoverStubAndSize(PCODE pEntryPoint, DWORD *pSize)
        {
            CONTRACT(Stub*)
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;

                PRECONDITION(pEntryPoint && pSize);
            }
            CONTRACT_END;

            Stub *pStub = Stub::RecoverStub(pEntryPoint);
            *pSize = sizeof(Stub) + pStub->m_numCodeBytes;
            RETURN pStub;
        }

        HRESULT CloneStub(BYTE *pBuffer, DWORD dwBufferSize)
        {
            LIMITED_METHOD_CONTRACT;
            if ((pBuffer == NULL) ||
                (dwBufferSize < (sizeof(*this) + m_numCodeBytes)))
            {
                return E_INVALIDARG;
            }

            memcpyNoGCRefs(pBuffer, this, sizeof(*this) + m_numCodeBytes);
            reinterpret_cast<Stub *>(pBuffer)->m_refcount = 1;

            return S_OK;
        }

        //-------------------------------------------------------------------
        // Reverse GetEntryPoint.
        //-------------------------------------------------------------------
        static Stub* RecoverStub(PCODE pEntryPoint)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_GC_NOTRIGGER;

            TADDR pStubData = PCODEToPINSTR(pEntryPoint);

            Stub *pStub = PTR_Stub(pStubData - sizeof(*pStub));

#if !defined(DACCESS_COMPILE)
            _ASSERTE(pStub->m_signature == kUsedStub);
            _ASSERTE(pStub->GetEntryPoint() == pEntryPoint);
#elif defined(_DEBUG)
            // If this isn't really a stub we don't want
            // to continue with it.
            // TODO: This should be removed once IsStub
            // can adverstise whether it's safe to call
            // further StubManager methods.
            if (pStub->m_signature != kUsedStub ||
                pStub->GetEntryPoint() != pEntryPoint)
            {
                DacError(E_INVALIDARG);
            }
#endif
            return pStub;
        }

        //-------------------------------------------------------------------
        // Returns TRUE if entry point is not inside the Stub allocation.
        //-------------------------------------------------------------------
        BOOL HasExternalEntryPoint() const
        {
            LIMITED_METHOD_CONTRACT;

            return (m_patchOffset & EXTERNAL_ENTRY_BIT) != 0;
        }

        //-------------------------------------------------------------------
        // This creates stubs.
        //-------------------------------------------------------------------
        static Stub* NewStub(LoaderHeap *pLoaderHeap, UINT numCodeBytes,
                             DWORD flags = 0
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
                             , UINT nUnwindInfoSize = 0
#endif
                             );

        static Stub* NewStub(PTR_VOID pCode, DWORD flags = 0);
        static Stub* NewStub(PCODE pCode, DWORD flags = 0)
        {
            return NewStub((PTR_VOID)pCode, flags);
        }

        //-------------------------------------------------------------------
        // One-time init
        //-------------------------------------------------------------------
        static void Init();

    protected:
        // fMC: Set to true if the stub is a multicast delegate, false otherwise
        void SetupStub(int numCodeBytes, DWORD flags
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
                       , UINT nUnwindInfoSlots
#endif
                       );
        void DeleteStub();

        //-------------------------------------------------------------------
        // Return executable entrypoint without checking the ref count.
        //-------------------------------------------------------------------
        inline PTR_CBYTE GetEntryPointInternal()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(m_signature == kUsedStub);


            if (HasExternalEntryPoint())
            {
                return dac_cast<PTR_BYTE>(*dac_cast<PTR_PCODE>(dac_cast<TADDR>(this) + sizeof(*this)));
            }
            else
            {
                // StubLink always puts the entrypoint first.
                return dac_cast<PTR_CBYTE>(this) + sizeof(*this);
            }
        }

        ULONG   m_refcount;
        ULONG   m_patchOffset;

        UINT    m_numCodeBytes;

#ifdef _DEBUG
        enum {
            kUsedStub  = 0x42555453,     // 'STUB'
            kFreedStub = 0x46555453,     // 'STUF'
        };

        UINT32  m_signature;
#else
#ifdef HOST_64BIT
        //README ALIGNMENT: in retail mode UINT m_numCodeBytes does not align to 16byte for the code
        //                   after the Stub struct. This is to pad properly
        UINT    m_pad_code_bytes;
#endif // HOST_64BIT
#endif // _DEBUG

#ifdef _DEBUG
        Stub()      // Stubs are created by NewStub(), not "new". Hide the
        { LIMITED_METHOD_CONTRACT; }          //  constructor to enforce this.
#endif

};


//-------------------------------------------------------------------------
// Each platform encodes the "branch" instruction in a different
// way. We use objects derived from InstructionFormat to abstract this
// information away. InstructionFormats don't contain any variable data
// so they should be allocated statically.
//
// Note that StubLinker does not create or define any InstructionFormats.
// The client does.
//
// The following example shows how to define a InstructionFormat for the
// X86 jump near instruction which takes on two forms:
//
//   EB xx        jmp  rel8    ;; SHORT JMP (signed 8-bit offset)
//   E9 xxxxxxxx  jmp  rel32   ;; NEAR JMP (signed 32-bit offset)
//
// InstructionFormat's provide StubLinker the following information:
//
//   RRT.m_allowedSizes
//
//     What are the possible sizes that the reference can
//     take? The X86 jump can take either an 8-bit or 32-bit offset
//     so this value is set to (k8|k32). StubLinker will try to
//     use the smallest size possible.
//
//
//   RRT.m_fTreatSizesAsSigned
//     Sign-extend or zero-extend smallsizes offsets to the platform
//     code pointer size? For x86, this field is set to TRUE (rel8
//     is considered signed.)
//
//
//   UINT RRT.GetSizeOfInstruction(refsize, variationCode)
//     Returns the total size of the instruction in bytes for a given
//     refsize. For this example:
//
//          if (refsize==k8) return 2;
//          if (refsize==k32) return 5;
//
//
//   UINT RRT.GetSizeOfData(refsize, variationCode)
//     Returns the total size of the seperate data area (if any) that the
//     instruction needs in bytes for a given refsize. For this example
//     on the SH3
//          if (refsize==k32) return 4; else return 0;
//
//   The default implem of this returns 0, so CPUs that don't have need
//   for a seperate constant area don't have to worry about it.
//
//
//   BOOL CanReach(refsize, variationcode, fExternal, offset)
//     Returns whether the instruction with the given variationcode &
//     refsize can reach the given offset. In the case of External
//     calls, fExternal is set and offset is the target address. In this case an
//     implementation should return TRUE only if refsize is big enough to fit a
//     full machine-sized pointer to anywhere in the address space.
//
//
//   VOID RRT.EmitInstruction(UINT     refsize,
//                            __int64  fixedUpReference,
//                            BYTE    *pOutBuffer,
//                            UINT     variationCode,
//                            BYTE    *pDataBuffer)
//
//     Given a chosen size (refsize) and the final offset value
//     computed by StubLink (fixedUpReference), write out the
//     instruction into the provided buffer (guaranteed to be
//     big enough provided you told the truth with GetSizeOfInstruction()).
//     If needed (e.g. on SH3) a data buffer is also passed in for
//     storage of constants.
//
//     For x86 jmp near:
//
//          if (refsize==k8) {
//              pOutBuffer[0] = 0xeb;
//              pOutBuffer[1] = (__int8)fixedUpReference;
//          } else if (refsize == k32) {
//              pOutBuffer[0] = 0xe9;
//              *((__int32*)(1+pOutBuffer)) = (__int32)fixedUpReference;
//          } else {
//              CRASH("Bad input.");
//          }
//
// VOID RRT.GetHotSpotOffset(UINT refsize, UINT variationCode)
//
//     The reference offset is always relative to some IP: this
//     method tells StubLinker where that IP is relative to the
//     start of the instruction. For X86, the offset is always
//     relative to the start of the *following* instruction so
//     the correct implementation is:
//
//          return GetSizeOfInstruction(refsize, variationCode);
//
//     Actually, InstructionFormat() provides a default implementation of this
//     method that does exactly this so X86 need not override this at all.
//
//
// The extra "variationCode" argument is an __int32 that StubLinker receives
// from EmitLabelRef() and passes uninterpreted to each RRT method.
// This allows one RRT to handle a family of related instructions,
// for example, the family of conditional jumps on the X86.
//
//-------------------------------------------------------------------------
class InstructionFormat
{
    private:
        enum
        {
        // if you want to add a size, insert it in-order (e.g. a 18-bit size would
        // go between k16 and k32) and shift all the higher values up. All values
        // must be a power of 2 since the get ORed together.

            _k8,
#ifdef INSTRFMT_K9
            _k9,
#endif
#ifdef INSTRFMT_K13
            _k13,
#endif
            _k16,
#ifdef INSTRFMT_K24
            _k24,
#endif
#ifdef INSTRFMT_K26
            _k26,
#endif
            _k32,
#ifdef INSTRFMT_K64SMALL
            _k64Small,
#endif
#ifdef INSTRFMT_K64
            _k64,
#endif
            _kAllowAlways,
        };

    public:

        enum
        {
            k8          = (1 << _k8),
#ifdef INSTRFMT_K9
            k9          = (1 << _k9),
#endif
#ifdef INSTRFMT_K13
            k13         = (1 << _k13),
#endif
            k16         = (1 << _k16),
#ifdef INSTRFMT_K24
            k24         = (1 << _k24),
#endif
#ifdef INSTRFMT_K26
            k26         = (1 << _k26),
#endif
            k32         = (1 << _k32),
#ifdef INSTRFMT_K64SMALL
            k64Small    = (1 << _k64Small),
#endif
#ifdef INSTRFMT_K64
            k64         = (1 << _k64),
#endif
            kAllowAlways= (1 << _kAllowAlways),
            kMax = kAllowAlways,
        };

        const UINT m_allowedSizes;         // OR mask using above "k" values
        InstructionFormat(UINT allowedSizes) : m_allowedSizes(allowedSizes)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode) = 0;
        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pCodeBuffer, UINT variationCode, BYTE *pDataBuffer) = 0;
        virtual UINT GetHotSpotOffset(UINT refsize, UINT variationCode)
        {
            WRAPPER_NO_CONTRACT;
            // Default implementation: the offset is added to the
            // start of the following instruction.
            return GetSizeOfInstruction(refsize, variationCode);
        }

        virtual UINT GetSizeOfData(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT;
            // Default implementation: 0 extra bytes needed (most CPUs)
            return 0;
        }

        virtual BOOL CanReach(UINT refsize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            LIMITED_METHOD_CONTRACT;

            if (fExternal) {
                // For external, we don't have enough info to predict
                // the offset yet so we only accept if the offset size
                // is at least as large as the native pointer size.
                switch(refsize) {
                    case InstructionFormat::k8: // intentional fallthru
                    case InstructionFormat::k16: // intentional fallthru
#ifdef INSTRFMT_K24
                    case InstructionFormat::k24: // intentional fallthru
#endif
#ifdef INSTRFMT_K26
                    case InstructionFormat::k26: // intentional fallthru
#endif
                        return FALSE;           // no 8 or 16-bit platforms

                    case InstructionFormat::k32:
                        return sizeof(LPVOID) <= 4;
#ifdef INSTRFMT_K64
                    case InstructionFormat::k64:
                        return sizeof(LPVOID) <= 8;
#endif
                    case InstructionFormat::kAllowAlways:
                        return TRUE;

                    default:
                        _ASSERTE(0);
                        return FALSE;
                }
            } else {
                switch(refsize)
                {
                    case InstructionFormat::k8:
                        return FitsInI1(offset);

                    case InstructionFormat::k16:
                        return FitsInI2(offset);

#ifdef INSTRFMT_K24
                    case InstructionFormat::k24:
                        return FitsInI2(offset>>8);
#endif

#ifdef INSTRFMT_K26
                    case InstructionFormat::k26:
                        return FitsInI2(offset>>10);
#endif
                    case InstructionFormat::k32:
                        return FitsInI4(offset);
#ifdef INSTRFMT_K64
                    case InstructionFormat::k64:
                        // intentional fallthru
#endif
                    case InstructionFormat::kAllowAlways:
                        return TRUE;
                    default:
                        _ASSERTE(0);
                        return FALSE;

                }
            }
        }
};





//-------------------------------------------------------------------------
// This stub cache associates stubs with an integer key.  For some clients,
// this might represent the size of the argument stack in some cpu-specific
// units (for the x86, the size is expressed in DWORDS.)  For other clients,
// this might take into account the style of stub (e.g. whether it returns
// an object reference or not).
//-------------------------------------------------------------------------
class ArgBasedStubCache
{
    public:
       ArgBasedStubCache(UINT fixedSize = NUMFIXEDSLOTS);
       ~ArgBasedStubCache();

       //-----------------------------------------------------------------
       // Retrieves the stub associated with the given key.
       //-----------------------------------------------------------------
       Stub *GetStub(UINT_PTR key);

       //-----------------------------------------------------------------
       // Tries to associate the stub with the given key.
       // It may fail because another thread might swoop in and
       // do the association before you do. Thus, you must use the
       // return value stub rather than the pStub.
       //-----------------------------------------------------------------
       Stub* AttemptToSetStub(UINT_PTR key, Stub *pStub);


       // Suggestions for number of slots
       enum {
 #ifdef _DEBUG
             NUMFIXEDSLOTS = 3,
 #else
             NUMFIXEDSLOTS = 16,
 #endif
       };

#ifdef _DEBUG
       VOID Dump();  //Diagnostic dump
#endif

    private:

       // How many low-numbered keys have direct access?
       UINT      m_numFixedSlots;

       // For 'm_numFixedSlots' low-numbered keys, we store them in an array.
       Stub    **m_aStub;


       struct SlotEntry
       {
           Stub             *m_pStub;
           UINT_PTR         m_key;
           SlotEntry        *m_pNext;
       };

       // High-numbered keys are stored in a sparse linked list.
       SlotEntry            *m_pSlotEntries;


       Crst                  m_crst;
};


#define CPUSTUBLINKER StubLinkerCPU

class NDirectStubLinker;
class CPUSTUBLINKER;

#endif // __stublink_h__
