// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              Unwind Info                                  XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef TARGET_ARMARCH

// Windows no longer imposes a maximum prolog size. However, we still have an
// assert here just to inform us if we increase the size of the prolog
// accidentally, as there is still a slight performance advantage in the
// OS unwinder to having as few unwind codes as possible.
// You can increase this "max" number if necessary.

#if defined(TARGET_ARM)
const unsigned MAX_PROLOG_SIZE_BYTES = 44;
const unsigned MAX_EPILOG_SIZE_BYTES = 44;
#define UWC_END 0xFF // "end" unwind code
#define UW_MAX_FRAGMENT_SIZE_BYTES (1U << 19)
#define UW_MAX_CODE_WORDS_COUNT 15      // Max number that can be encoded in the "Code Words" field of the .pdata record
#define UW_MAX_EPILOG_START_INDEX 0xFFU // Max number that can be encoded in the "Epilog Start Index" field
                                        // of the .pdata record
#elif defined(TARGET_ARM64)
const unsigned MAX_PROLOG_SIZE_BYTES = 100;
const unsigned MAX_EPILOG_SIZE_BYTES = 100;
#define UWC_END 0xE4   // "end" unwind code
#define UWC_END_C 0xE5 // "end_c" unwind code
#define UW_MAX_FRAGMENT_SIZE_BYTES (1U << 20)
#define UW_MAX_CODE_WORDS_COUNT 31
#define UW_MAX_EPILOG_START_INDEX 0x3FFU
#endif // TARGET_ARM64

#define UW_MAX_EPILOG_COUNT 31                 // Max number that can be encoded in the "Epilog count" field
                                               // of the .pdata record
#define UW_MAX_EXTENDED_CODE_WORDS_COUNT 0xFFU // Max number that can be encoded in the "Extended Code Words"
                                               // field of the .pdata record
#define UW_MAX_EXTENDED_EPILOG_COUNT 0xFFFFU   // Max number that can be encoded in the "Extended Epilog Count"
                                               // field of the .pdata record
#define UW_MAX_EPILOG_START_OFFSET 0x3FFFFU    // Max number that can be encoded in the "Epilog Start Offset"
                                               // field of the .pdata record

//
// Forward declaration of class defined in emit.h
//

class emitLocation;

//
// Forward declarations of classes defined in this file
//

class UnwindCodesBase;
class UnwindPrologCodes;
class UnwindEpilogCodes;
class UnwindEpilogInfo;
class UnwindFragmentInfo;
class UnwindInfo;

// UnwindBase: A base class shared by the the unwind classes that require
// a Compiler* for memory allocation.

class UnwindBase
{
protected:
    UnwindBase(Compiler* comp) : uwiComp(comp)
    {
    }

    UnwindBase()
    {
    }
    ~UnwindBase()
    {
    }

// TODO: How do we get the ability to access uwiComp without error on Clang?
#if defined(DEBUG) && !defined(__GNUC__)

    template <typename T>
    T dspPtr(T p)
    {
        return uwiComp->dspPtr(p);
    }

    template <typename T>
    T dspOffset(T o)
    {
        return uwiComp->dspOffset(o);
    }

    static const char* dspBool(bool b)
    {
        return (b) ? "true" : "false";
    }

#endif // DEBUG

    //
    // Data
    //

    Compiler* uwiComp;
};

// UnwindCodesBase: A base class shared by the the classes used to represent the prolog
// and epilog unwind codes.

class UnwindCodesBase
{
public:
    // Add a single unwind code.

    virtual void AddCode(BYTE b1) = 0;
    virtual void AddCode(BYTE b1, BYTE b2) = 0;
    virtual void AddCode(BYTE b1, BYTE b2, BYTE b3) = 0;
    virtual void AddCode(BYTE b1, BYTE b2, BYTE b3, BYTE b4) = 0;

    // Get access to the unwind codes

    virtual BYTE* GetCodes() = 0;

    bool IsEndCode(BYTE b)
    {
#if defined(TARGET_ARM)
        return b >= 0xFD;
#elif defined(TARGET_ARM64)
        return (b == UWC_END); // TODO-ARM64-Bug?: what about the "end_c" code?
#endif // TARGET_ARM64
    }

#ifdef DEBUG

    unsigned GetCodeSizeFromUnwindCodes(bool isProlog);

#endif // DEBUG
};

// UnwindPrologCodes: represents the unwind codes for a prolog sequence.
// Prolog unwind codes arrive in reverse order from how they will be emitted.
// Store them as a stack, storing from the end of an array towards the beginning.
// This class is also re-used as the final location of the consolidated unwind
// information for a function, including unwind info header, the prolog codes,
// and any epilog codes.

class UnwindPrologCodes : public UnwindBase, public UnwindCodesBase
{
    // UPC_LOCAL_COUNT is the amount of memory local to this class. For ARM CoreLib, the maximum size is 34.
    // Here is a histogram of other interesting sizes:
    //     <=16  79%
    //     <=24  96%
    //     <=32  99%
    // From this data, we choose to use 24.

    static const int UPC_LOCAL_COUNT = 24;

public:
    UnwindPrologCodes(Compiler* comp)
        : UnwindBase(comp)
        , upcMem(upcMemLocal)
        , upcMemSize(UPC_LOCAL_COUNT)
        , upcCodeSlot(UPC_LOCAL_COUNT)
        , upcHeaderSlot(-1)
        , upcEpilogSlot(-1)
    {
        // Assume we've got a normal end code.
        // Push four so we can generate an array that is a multiple of 4 bytes in size with the
        // end codes (and padding) already in place. One is the end code for the prolog codes,
        // three are end-of-array alignment padding.
        PushByte(UWC_END);
        PushByte(UWC_END);
        PushByte(UWC_END);
        PushByte(UWC_END);
    }

    //
    // Implementation of UnwindCodesBase
    //

    virtual void AddCode(BYTE b1)
    {
        PushByte(b1);
    }

    virtual void AddCode(BYTE b1, BYTE b2)
    {
        PushByte(b2);
        PushByte(b1);
    }

    virtual void AddCode(BYTE b1, BYTE b2, BYTE b3)
    {
        PushByte(b3);
        PushByte(b2);
        PushByte(b1);
    }

    virtual void AddCode(BYTE b1, BYTE b2, BYTE b3, BYTE b4)
    {
        PushByte(b4);
        PushByte(b3);
        PushByte(b2);
        PushByte(b1);
    }

    // Return a pointer to the first unwind code byte
    virtual BYTE* GetCodes()
    {
        assert(upcCodeSlot < upcMemSize); // There better be at least one code!
        return &upcMem[upcCodeSlot];
    }

    ///////////////////////////////////////////////////////////////////////////

    BYTE GetByte(int index)
    {
        assert(upcCodeSlot <= index && index < upcMemSize);
        return upcMem[index];
    }

    // Push a single byte on the unwind code stack
    void PushByte(BYTE b)
    {
        if (upcCodeSlot == 0)
        {
            // We've run out of space! Reallocate, and copy everything to a new array.
            EnsureSize(upcMemSize + 1);
        }

        --upcCodeSlot;
        noway_assert(0 <= upcCodeSlot && upcCodeSlot < upcMemSize);

        upcMem[upcCodeSlot] = b;
    }

    // Return the size of the unwind codes, in bytes. The size is the exact size, not an aligned size.
    // The size includes exactly one "end" code.
    int Size()
    {
        // -3 because we put 4 "end" codes at the end in the constructor, and we shouldn't count that here
        return upcMemSize - upcCodeSlot - 3;
    }

    void SetFinalSize(int headerBytes, int epilogBytes);

    void AddHeaderWord(DWORD d);

    void GetFinalInfo(/* OUT */ BYTE** ppUnwindBlock, /* OUT */ ULONG* pUnwindBlockSize);

    // AppendEpilog: copy the epilog bytes to the next epilog bytes slot
    void AppendEpilog(UnwindEpilogInfo* pEpi);

    // Match the prolog codes to a set of epilog codes
    int Match(UnwindEpilogInfo* pEpi);

    // Copy the prolog codes from another prolog
    void CopyFrom(UnwindPrologCodes* pCopyFrom);

    UnwindPrologCodes()
    {
    }
    ~UnwindPrologCodes()
    {
    }

#ifdef DEBUG
    void Dump(int indent = 0);
#endif // DEBUG

private:
    void EnsureSize(int requiredSize);

    // No copy constructor or operator=
    UnwindPrologCodes(const UnwindPrologCodes& info);
    UnwindPrologCodes& operator=(const UnwindPrologCodes&);

    //
    // Data
    //

    // To store the unwind codes, we first use a local array that should satisfy almost all cases.
    // If there are more unwind codes, we dynamically allocate memory.
    BYTE  upcMemLocal[UPC_LOCAL_COUNT];
    BYTE* upcMem;

    // upcMemSize is the number of bytes in upcMem. This is equal to UPC_LOCAL_COUNT unless
    // we've dynamically allocated memory to store the codes.
    int upcMemSize;

    // upcCodeSlot points to the last unwind code added to the array. The array is filled in from
    // the end, so it starts pointing one beyond the array end.
    int upcCodeSlot;

    // upcHeaderSlot points to the last header byte prepended to the array. Headers bytes are
    // filled in from the beginning, and only after SetFinalSize() is called.
    int upcHeaderSlot;

    // upcEpilogSlot points to the next epilog location to fill
    int upcEpilogSlot;

    // upcUnwindBlockSlot is only set after SetFinalSize() is called. It is the index of the first
    // byte of the final unwind data, namely the first byte of the header.
    int upcUnwindBlockSlot;
};

// UnwindEpilogCodes: represents the unwind codes for a single epilog sequence.
// Epilog unwind codes arrive in the order they will be emitted. Store them as an array,
// adding new ones to the end of the array.

class UnwindEpilogCodes : public UnwindBase, public UnwindCodesBase
{
    // UEC_LOCAL_COUNT is the amount of memory local to this class. For ARM CoreLib, the maximum size is 6,
    // while 89% of epilogs fit in 4. So, set it to 4 to maintain array alignment and hit most cases.
    static const int UEC_LOCAL_COUNT = 4;

public:
    UnwindEpilogCodes(Compiler* comp)
        : UnwindBase(comp)
        , uecMem(uecMemLocal)
        , firstByteOfLastCode(0)
        , uecMemSize(UEC_LOCAL_COUNT)
        , uecCodeSlot(-1)
        , uecFinalized(false)
    {
    }

    //
    // Implementation of UnwindCodesBase
    //

    virtual void AddCode(BYTE b1)
    {
        AppendByte(b1);

        firstByteOfLastCode = b1;
    }

    virtual void AddCode(BYTE b1, BYTE b2)
    {
        AppendByte(b1);
        AppendByte(b2);

        firstByteOfLastCode = b1;
    }

    virtual void AddCode(BYTE b1, BYTE b2, BYTE b3)
    {
        AppendByte(b1);
        AppendByte(b2);
        AppendByte(b3);

        firstByteOfLastCode = b1;
    }

    virtual void AddCode(BYTE b1, BYTE b2, BYTE b3, BYTE b4)
    {
        AppendByte(b1);
        AppendByte(b2);
        AppendByte(b3);
        AppendByte(b4);

        firstByteOfLastCode = b1;
    }

    // Return a pointer to the first unwind code byte
    virtual BYTE* GetCodes()
    {
        assert(uecFinalized);

        // Codes start at the beginning
        return uecMem;
    }

    ///////////////////////////////////////////////////////////////////////////

    BYTE GetByte(int index)
    {
        assert(0 <= index && index <= uecCodeSlot);
        return uecMem[index];
    }

    // Add a single byte on the unwind code array
    void AppendByte(BYTE b)
    {
        if (uecCodeSlot == uecMemSize - 1)
        {
            // We've run out of space! Reallocate, and copy everything to a new array.
            EnsureSize(uecMemSize + 1);
        }

        ++uecCodeSlot;
        noway_assert(0 <= uecCodeSlot && uecCodeSlot < uecMemSize);

        uecMem[uecCodeSlot] = b;
    }

    // Return the size of the unwind codes, in bytes. The size is the exact size, not an aligned size.
    int Size()
    {
        if (uecFinalized)
        {
            // Add one because uecCodeSlot is 0-based
            return uecCodeSlot + 1;
        }
        else
        {
            // Add one because uecCodeSlot is 0-based, and one for an "end" code that isn't stored (yet).
            return uecCodeSlot + 2;
        }
    }

    void FinalizeCodes()
    {
        assert(!uecFinalized);
        noway_assert(0 <= uecCodeSlot && uecCodeSlot < uecMemSize); // There better be at least one code!

        if (!IsEndCode(firstByteOfLastCode)) // If the last code is an end code, we don't need to append one.
        {
            AppendByte(UWC_END);           // Add a default "end" code to the end of the array of unwind codes
            firstByteOfLastCode = UWC_END; // Update firstByteOfLastCode in case we use it later
        }

        uecFinalized = true; // With the "end" code in place, now we're done

#ifdef DEBUG
        unsigned codeSize = GetCodeSizeFromUnwindCodes(false);
        assert(codeSize <= MAX_EPILOG_SIZE_BYTES);
#endif // DEBUG
    }

    UnwindEpilogCodes()
    {
    }
    ~UnwindEpilogCodes()
    {
    }

#ifdef DEBUG
    void Dump(int indent = 0);
#endif // DEBUG

private:
    void EnsureSize(int requiredSize);

    // No destructor, copy constructor or operator=
    UnwindEpilogCodes(const UnwindEpilogCodes& info);
    UnwindEpilogCodes& operator=(const UnwindEpilogCodes&);

    //
    // Data
    //

    // To store the unwind codes, we first use a local array that should satisfy almost all cases.
    // If there are more unwind codes, we dynamically allocate memory.
    BYTE  uecMemLocal[UEC_LOCAL_COUNT];
    BYTE* uecMem;
    BYTE  firstByteOfLastCode;

    // uecMemSize is the number of bytes/slots in uecMem. This is equal to UEC_LOCAL_COUNT unless
    // we've dynamically allocated memory to store the codes.
    int uecMemSize;

    // uecCodeSlot points to the last unwind code added to the array. The array is filled in from
    // the beginning, so it starts at -1.
    int uecCodeSlot;

    // Is the unwind information finalized? Finalized info has an end code appended.
    bool uecFinalized;
};

// UnwindEpilogInfo: represents the unwind information for a single epilog sequence. Epilogs for a
// single function/funclet are in a linked list.

class UnwindEpilogInfo : public UnwindBase
{
    friend class UnwindFragmentInfo;

    static const unsigned EPI_ILLEGAL_OFFSET = 0xFFFFFFFF;

public:
    UnwindEpilogInfo(Compiler* comp)
        : UnwindBase(comp)
        , epiNext(NULL)
        , epiEmitLocation(NULL)
        , epiCodes(comp)
        , epiStartOffset(EPI_ILLEGAL_OFFSET)
        , epiMatches(false)
        , epiStartIndex(-1)
    {
    }

    void CaptureEmitLocation();

    void FinalizeOffset();

    void FinalizeCodes()
    {
        epiCodes.FinalizeCodes();
    }

    UNATIVE_OFFSET GetStartOffset()
    {
        assert(epiStartOffset != EPI_ILLEGAL_OFFSET);
        return epiStartOffset;
    }

    int GetStartIndex()
    {
        assert(epiStartIndex != -1);
        return epiStartIndex; // The final "Epilog Start Index" of this epilog's unwind codes
    }

    void SetStartIndex(int index)
    {
        assert(epiStartIndex == -1);
        epiStartIndex = (int)index;
    }

    void SetMatches()
    {
        epiMatches = true;
    }

    bool Matches()
    {
        return epiMatches;
    }

    // Size of epilog unwind codes in bytes
    int Size()
    {
        return epiCodes.Size();
    }

    // Return a pointer to the first unwind code byte
    BYTE* GetCodes()
    {
        return epiCodes.GetCodes();
    }

    // Match the codes to a set of epilog codes
    int Match(UnwindEpilogInfo* pEpi);

    UnwindEpilogInfo()
    {
    }
    ~UnwindEpilogInfo()
    {
    }

#ifdef DEBUG
    void Dump(int indent = 0);
#endif // DEBUG

private:
    // No copy constructor or operator=
    UnwindEpilogInfo(const UnwindEpilogInfo& info);
    UnwindEpilogInfo& operator=(const UnwindEpilogInfo&);

    //
    // Data
    //

    UnwindEpilogInfo* epiNext;
    emitLocation*     epiEmitLocation; // The emitter location of the beginning of the epilog
    UnwindEpilogCodes epiCodes;
    UNATIVE_OFFSET    epiStartOffset; // Actual offset of the epilog, in bytes, from the start of the function. Set in
                                      // FinalizeOffset().
    bool epiMatches;   // Do the epilog unwind codes match some other set of codes? If so, we don't copy these to the
                       // final set; we just point to another set.
    int epiStartIndex; // The final "Epilog Start Index" of this epilog's unwind codes
};

// UnwindFragmentInfo: represents all the unwind information for a single fragment of a function or funclet.
// A fragment is a section with a code size less than the maximum unwind code size: either 512K bytes, or
// that specified by COMPlus_JitSplitFunctionSize. In most cases, there will be exactly one fragment.

class UnwindFragmentInfo : public UnwindBase
{
    friend class UnwindInfo;

    static const unsigned UFI_ILLEGAL_OFFSET = 0xFFFFFFFF;

public:
    UnwindFragmentInfo(Compiler* comp, emitLocation* emitLoc, bool hasPhantomProlog);

    void FinalizeOffset();

    UNATIVE_OFFSET GetStartOffset()
    {
        assert(ufiStartOffset != UFI_ILLEGAL_OFFSET);
        return ufiStartOffset;
    }

    // Add an unwind code. It could be for a prolog, or for the current epilog.
    // A single unwind code can be from 1 to 4 bytes.

    void AddCode(BYTE b1)
    {
        assert(ufiInitialized == UFI_INITIALIZED_PATTERN);
        ufiCurCodes->AddCode(b1);
    }

    void AddCode(BYTE b1, BYTE b2)
    {
        assert(ufiInitialized == UFI_INITIALIZED_PATTERN);
        ufiCurCodes->AddCode(b1, b2);
    }

    void AddCode(BYTE b1, BYTE b2, BYTE b3)
    {
        assert(ufiInitialized == UFI_INITIALIZED_PATTERN);
        ufiCurCodes->AddCode(b1, b2, b3);
    }

    void AddCode(BYTE b1, BYTE b2, BYTE b3, BYTE b4)
    {
        assert(ufiInitialized == UFI_INITIALIZED_PATTERN);
        ufiCurCodes->AddCode(b1, b2, b3, b4);
    }

    unsigned EpilogCount()
    {
        unsigned count = 0;
        for (UnwindEpilogInfo* pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
        {
            ++count;
        }
        return count;
    }

    void AddEpilog();

    void MergeCodes();

    void CopyPrologCodes(UnwindFragmentInfo* pCopyFrom);

    void SplitEpilogCodes(emitLocation* emitLoc, UnwindFragmentInfo* pSplitFrom);

    bool IsAtFragmentEnd(UnwindEpilogInfo* pEpi);

    // Return the full, final size of unwind block. This will be used to allocate memory for
    // the unwind block. This is called before the code offsets are finalized.
    // Size is in bytes.
    ULONG Size()
    {
        assert(ufiSize != 0);
        return ufiSize;
    }

    void Finalize(UNATIVE_OFFSET functionLength);

    // GetFinalInfo: return a pointer to the final unwind info to hand to the VM, and the size of this info in bytes
    void GetFinalInfo(/* OUT */ BYTE** ppUnwindBlock, /* OUT */ ULONG* pUnwindBlockSize)
    {
        ufiPrologCodes.GetFinalInfo(ppUnwindBlock, pUnwindBlockSize);
    }

    void Reserve(BOOL isFunclet, bool isHotCode);

    void Allocate(
        CorJitFuncKind funKind, void* pHotCode, void* pColdCode, UNATIVE_OFFSET funcEndOffset, bool isHotCode);

    UnwindFragmentInfo()
    {
    }
    ~UnwindFragmentInfo()
    {
    }

#ifdef DEBUG
    void Dump(int indent = 0);
#endif // DEBUG

private:
    // No copy constructor or operator=
    UnwindFragmentInfo(const UnwindFragmentInfo& info);
    UnwindFragmentInfo& operator=(const UnwindFragmentInfo&);

    //
    // Data
    //

    UnwindFragmentInfo* ufiNext;             // The next fragment
    emitLocation*       ufiEmitLoc;          // Emitter location for start of fragment
    bool                ufiHasPhantomProlog; // Are the prolog codes for a phantom prolog, or a real prolog?
                                             //   (For a phantom prolog, this code fragment represents a fragment in
                                             //   the sense of the unwind info spec; something without a real prolog.)
    UnwindPrologCodes ufiPrologCodes;        // The unwind codes for the prolog
    UnwindEpilogInfo  ufiEpilogFirst;        // In-line the first epilog to avoid separate memory allocation, since
                                             //   almost all functions will have at least one epilog. It is pointed
                                             //   to by ufiEpilogList when the first epilog is added.
    UnwindEpilogInfo* ufiEpilogList;         // The head of the epilog list
    UnwindEpilogInfo* ufiEpilogLast;         // The last entry in the epilog list (the last epilog added)
    UnwindCodesBase*  ufiCurCodes;           // Pointer to current unwind codes, either prolog or epilog

    // Some data computed when merging the unwind codes, and used when finalizing the
    // unwind block for emission.
    unsigned       ufiSize; // The size of the unwind data for this fragment, in bytes
    bool           ufiSetEBit;
    bool           ufiNeedExtendedCodeWordsEpilogCount;
    unsigned       ufiCodeWords;
    unsigned       ufiEpilogScopes;
    UNATIVE_OFFSET ufiStartOffset;

#ifdef DEBUG

    unsigned ufiNum;

    // Are we processing the prolog? The prolog must come first, followed by a (possibly empty)
    // set of epilogs, for this function/funclet.
    bool ufiInProlog;

    static const unsigned UFI_INITIALIZED_PATTERN = 0x0FACADE0; // Something unlikely to be the fill pattern for
                                                                // uninitialized memory
    unsigned ufiInitialized;

#endif // DEBUG
};

// UnwindInfo: represents all the unwind information for a single function or funclet

class UnwindInfo : public UnwindBase
{
public:
    void InitUnwindInfo(Compiler* comp, emitLocation* startLoc, emitLocation* endLoc);

    void HotColdSplitCodes(UnwindInfo* puwi);

    // The following act on all the fragments that make up the unwind info for this function or funclet.

    void Split();

    static void EmitSplitCallback(void* context, emitLocation* emitLoc);

    void Reserve(BOOL isFunclet, bool isHotCode);

    void Allocate(CorJitFuncKind funKind, void* pHotCode, void* pColdCode, bool isHotCode);

    // The following act on the current fragment (the one pointed to by 'uwiFragmentLast').

    // Add an unwind code. It could be for a prolog, or for the current epilog.
    // A single unwind code can be from 1 to 4 bytes.

    void AddCode(BYTE b1)
    {
        assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
        assert(uwiFragmentLast != NULL);
        INDEBUG(CheckOpsize(b1));

        uwiFragmentLast->AddCode(b1);
        CaptureLocation();
    }

    void AddCode(BYTE b1, BYTE b2)
    {
        assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
        assert(uwiFragmentLast != NULL);
        INDEBUG(CheckOpsize(b1));

        uwiFragmentLast->AddCode(b1, b2);
        CaptureLocation();
    }

    void AddCode(BYTE b1, BYTE b2, BYTE b3)
    {
        assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
        assert(uwiFragmentLast != NULL);
        INDEBUG(CheckOpsize(b1));

        uwiFragmentLast->AddCode(b1, b2, b3);
        CaptureLocation();
    }

    void AddCode(BYTE b1, BYTE b2, BYTE b3, BYTE b4)
    {
        assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
        assert(uwiFragmentLast != NULL);
        INDEBUG(CheckOpsize(b1));

        uwiFragmentLast->AddCode(b1, b2, b3, b4);
        CaptureLocation();
    }

    void AddEpilog();

    emitLocation* GetCurrentEmitterLocation()
    {
        return uwiCurLoc;
    }

#if defined(TARGET_ARM)
    unsigned GetInstructionSize();
#endif // defined(TARGET_ARM)

    void CaptureLocation();

    UnwindInfo()
    {
    }
    ~UnwindInfo()
    {
    }

#ifdef DEBUG

#if defined(TARGET_ARM)
    // Given the first byte of the unwind code, check that its opsize matches
    // the last instruction added in the emitter.
    void CheckOpsize(BYTE b1);
#elif defined(TARGET_ARM64)
    void CheckOpsize(BYTE b1)
    {
    } // nothing to do; all instructions are 4 bytes
#endif // defined(TARGET_ARM64)

    void Dump(bool isHotCode, int indent = 0);

    bool uwiAddingNOP;

#endif // DEBUG

private:
    void AddFragment(emitLocation* emitLoc);

    // No copy constructor or operator=
    UnwindInfo(const UnwindInfo& info);
    UnwindInfo& operator=(const UnwindInfo&);

    //
    // Data
    //

    UnwindFragmentInfo uwiFragmentFirst; // The first fragment is directly here, so it doesn't need to be separately
                                         // allocated.
    UnwindFragmentInfo* uwiFragmentLast; // The last entry in the fragment list (the last fragment added)
    emitLocation*       uwiEndLoc;       // End emitter location of this function/funclet (NULL == end of all code)
    emitLocation*       uwiCurLoc; // The current emitter location (updated after an unwind code is added), used for NOP
                                   // padding, and asserts.

#ifdef DEBUG

    static const unsigned UWI_INITIALIZED_PATTERN = 0x0FACADE1; // Something unlikely to be the fill pattern for
                                                                // uninitialized memory
    unsigned uwiInitialized;

#endif // DEBUG
};

#ifdef DEBUG

// Forward declaration
void DumpUnwindInfo(Compiler*         comp,
                    bool              isHotCode,
                    UNATIVE_OFFSET    startOffset,
                    UNATIVE_OFFSET    endOffset,
                    const BYTE* const pHeader,
                    ULONG             unwindBlockSize);

#endif // DEBUG

#endif // TARGET_ARMARCH
