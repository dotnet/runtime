// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          BasicBlock                                       XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
#ifndef _BLOCK_H_
#define _BLOCK_H_
/*****************************************************************************/

#include "vartype.h" // For "var_types.h"
#include "_typeinfo.h"
/*****************************************************************************/

// Defines VARSET_TP
#include "varset.h"

#include "blockset.h"
#include "jitstd.h"
#include "bitvec.h"
#include "jithashtable.h"

/*****************************************************************************/
typedef BitVec EXPSET_TP;
#if LARGE_EXPSET
#define EXPSET_SZ 64
#else
#define EXPSET_SZ 32
#endif

typedef BitVec          ASSERT_TP;
typedef BitVec_ValArg_T ASSERT_VALARG_TP;
typedef BitVec_ValRet_T ASSERT_VALRET_TP;

// We use the following format when printing the BasicBlock number: bbNum
// This define is used with string concatenation to put this in printf format strings  (Note that %u means unsigned int)
#define FMT_BB "BB%02u"

// Use this format for loop table indices.
#define FMT_LP "L%02u"

// And this format for profile weights
#define FMT_WT "%.7g"

/*****************************************************************************
 *
 *  Each basic block ends with a jump which is described as a value
 *  of the following enumeration.
 */

// clang-format off

enum BBjumpKinds : BYTE
{
    BBJ_EHFINALLYRET,// block ends with 'endfinally' (for finally or fault)
    BBJ_EHFILTERRET, // block ends with 'endfilter'
    BBJ_EHCATCHRET,  // block ends with a leave out of a catch (only #if defined(FEATURE_EH_FUNCLETS))
    BBJ_THROW,       // block ends with 'throw'
    BBJ_RETURN,      // block ends with 'ret'
    BBJ_NONE,        // block flows into the next one (no jump)
    BBJ_ALWAYS,      // block always jumps to the target
    BBJ_LEAVE,       // block always jumps to the target, maybe out of guarded region. Only used until importing.
    BBJ_CALLFINALLY, // block always calls the target finally
    BBJ_COND,        // block conditionally jumps to the target
    BBJ_SWITCH,      // block ends with a switch statement

    BBJ_COUNT
};

// clang-format on

struct GenTree;
struct Statement;
struct BasicBlock;
class Compiler;
class typeInfo;
struct BasicBlockList;
struct flowList;
struct EHblkDsc;
struct BBswtDesc;

struct StackEntry
{
    GenTree* val;
    typeInfo seTypeInfo;
};
/*****************************************************************************/

enum ThisInitState
{
    TIS_Bottom, // We don't know anything about the 'this' pointer.
    TIS_Uninit, // The 'this' pointer for this constructor is known to be uninitialized.
    TIS_Init,   // The 'this' pointer for this constructor is known to be initialized.
    TIS_Top,    // This results from merging the state of two blocks one with TIS_Unint and the other with TIS_Init.
                // We use this in fault blocks to prevent us from accessing the 'this' pointer, but otherwise
                // allowing the fault block to generate code.
};

struct EntryState
{
    ThisInitState thisInitialized; // used to track whether the this ptr is initialized.
    unsigned      esStackDepth;    // size of esStack
    StackEntry*   esStack;         // ptr to  stack
};

// Enumeration of the kinds of memory whose state changes the compiler tracks
enum MemoryKind
{
    ByrefExposed = 0, // Includes anything byrefs can read/write (everything in GcHeap, address-taken locals,
                      //                                          unmanaged heap, callers' locals, etc.)
    GcHeap,           // Includes actual GC heap, and also static fields
    MemoryKindCount,  // Number of MemoryKinds
};
#ifdef DEBUG
const char* const memoryKindNames[] = {"ByrefExposed", "GcHeap"};
#endif // DEBUG

// Bitmask describing a set of memory kinds (usable in bitfields)
typedef unsigned int MemoryKindSet;

// Bitmask for a MemoryKindSet containing just the specified MemoryKind
inline MemoryKindSet memoryKindSet(MemoryKind memoryKind)
{
    return (1U << memoryKind);
}

// Bitmask for a MemoryKindSet containing the specified MemoryKinds
template <typename... MemoryKinds>
inline MemoryKindSet memoryKindSet(MemoryKind memoryKind, MemoryKinds... memoryKinds)
{
    return memoryKindSet(memoryKind) | memoryKindSet(memoryKinds...);
}

// Bitmask containing all the MemoryKinds
const MemoryKindSet fullMemoryKindSet = (1 << MemoryKindCount) - 1;

// Bitmask containing no MemoryKinds
const MemoryKindSet emptyMemoryKindSet = 0;

// Standard iterator class for iterating through MemoryKinds
class MemoryKindIterator
{
    int value;

public:
    explicit inline MemoryKindIterator(int val) : value(val)
    {
    }
    inline MemoryKindIterator& operator++()
    {
        ++value;
        return *this;
    }
    inline MemoryKindIterator operator++(int)
    {
        return MemoryKindIterator(value++);
    }
    inline MemoryKind operator*()
    {
        return static_cast<MemoryKind>(value);
    }
    friend bool operator==(const MemoryKindIterator& left, const MemoryKindIterator& right)
    {
        return left.value == right.value;
    }
    friend bool operator!=(const MemoryKindIterator& left, const MemoryKindIterator& right)
    {
        return left.value != right.value;
    }
};

// Empty struct that allows enumerating memory kinds via `for(MemoryKind kind : allMemoryKinds())`
struct allMemoryKinds
{
    inline allMemoryKinds()
    {
    }
    inline MemoryKindIterator begin()
    {
        return MemoryKindIterator(0);
    }
    inline MemoryKindIterator end()
    {
        return MemoryKindIterator(MemoryKindCount);
    }
};

// This encapsulates the "exception handling" successors of a block.  That is,
// if a basic block BB1 occurs in a try block, we consider the first basic block
// BB2 of the corresponding handler to be an "EH successor" of BB1.  Because we
// make the conservative assumption that control flow can jump from a try block
// to its handler at any time, the immediate (regular control flow)
// predecessor(s) of the the first block of a try block are also considered to
// have the first block of the handler as an EH successor.  This makes variables that
// are "live-in" to the handler become "live-out" for these try-predecessor block,
// so that they become live-in to the try -- which we require.
//
// This class maintains the minimum amount of state necessary to implement
// successor iteration. The basic block whose successors are enumerated and
// the compiler need to be provided by Advance/Current's callers. In addition
// to iterators, this allows the use of other approaches that are more space
// efficient.
class EHSuccessorIterPosition
{
    // The number of "regular" (i.e., non-exceptional) successors that remain to
    // be considered.  If BB1 has successor BB2, and BB2 is the first block of a
    // try block, then we consider the catch block of BB2's try to be an EH
    // successor of BB1.  This captures the iteration over the successors of BB1
    // for this purpose.  (In reverse order; we're done when this field is 0).
    unsigned m_remainingRegSuccs;

    // The current "regular" successor of "m_block" that we're considering.
    BasicBlock* m_curRegSucc;

    // The current try block.  If non-null, then the current successor "m_curRegSucc"
    // is the first block of the handler of this block.  While this try block has
    // enclosing try's that also start with "m_curRegSucc", the corresponding handlers will be
    // further EH successors.
    EHblkDsc* m_curTry;

    // Requires that "m_curTry" is NULL.  Determines whether there is, as
    // discussed just above, a regular successor that's the first block of a
    // try; if so, sets "m_curTry" to that try block.  (As noted above, selecting
    // the try containing the current regular successor as the "current try" may cause
    // multiple first-blocks of catches to be yielded as EH successors: trys enclosing
    // the current try are also included if they also start with the current EH successor.)
    void FindNextRegSuccTry(Compiler* comp, BasicBlock* block);

public:
    // Constructs a position that "points" to the first EH successor of `block`.
    EHSuccessorIterPosition(Compiler* comp, BasicBlock* block);

    // Constructs a position that "points" past the last EH successor of `block` ("end" position).
    EHSuccessorIterPosition() : m_remainingRegSuccs(0), m_curTry(nullptr)
    {
    }

    // Go on to the next EH successor.
    void Advance(Compiler* comp, BasicBlock* block);

    // Returns the current EH successor.
    // Requires that "*this" is not equal to the "end" position.
    BasicBlock* Current(Compiler* comp, BasicBlock* block);

    // Returns "true" iff "*this" is equal to "ehsi".
    bool operator==(const EHSuccessorIterPosition& ehsi)
    {
        return m_curTry == ehsi.m_curTry && m_remainingRegSuccs == ehsi.m_remainingRegSuccs;
    }

    bool operator!=(const EHSuccessorIterPosition& ehsi)
    {
        return !((*this) == ehsi);
    }
};

// Yields both normal and EH successors (in that order) in one iteration.
//
// This class maintains the minimum amount of state necessary to implement
// successor iteration. The basic block whose successors are enumerated and
// the compiler need to be provided by Advance/Current's callers. In addition
// to iterators, this allows the use of other approaches that are more space
// efficient.
class AllSuccessorIterPosition
{
    // Normal successor position
    unsigned m_numNormSuccs;
    unsigned m_remainingNormSucc;
    // EH successor position
    EHSuccessorIterPosition m_ehIter;

    // True iff m_blk is a BBJ_CALLFINALLY block, and the current try block of m_ehIter,
    // the first block of whose handler would be next yielded, is the jump target of m_blk.
    inline bool CurTryIsBlkCallFinallyTarget(Compiler* comp, BasicBlock* block);

public:
    // Constructs a position that "points" to the first successor of `block`.
    inline AllSuccessorIterPosition(Compiler* comp, BasicBlock* block);

    // Constructs a position that "points" past the last successor of `block` ("end" position).
    AllSuccessorIterPosition() : m_remainingNormSucc(0), m_ehIter()
    {
    }

    // Go on to the next successor.
    inline void Advance(Compiler* comp, BasicBlock* block);

    // Returns the current successor.
    // Requires that "*this" is not equal to the "end" position.
    inline BasicBlock* Current(Compiler* comp, BasicBlock* block);

    bool IsCurrentEH()
    {
        return m_remainingNormSucc == 0;
    }

    bool HasCurrent()
    {
        return *this != AllSuccessorIterPosition();
    }

    // Returns "true" iff "*this" is equal to "asi".
    bool operator==(const AllSuccessorIterPosition& asi)
    {
        return (m_remainingNormSucc == asi.m_remainingNormSucc) && (m_ehIter == asi.m_ehIter);
    }

    bool operator!=(const AllSuccessorIterPosition& asi)
    {
        return !((*this) == asi);
    }
};

// PredEdgeList: adapter class for forward iteration of the predecessor edge linked list using range-based `for`,
// normally used via BasicBlock::PredEdges(), e.g.:
//    for (flowList* const edge : block->PredEdges()) ...
//
class PredEdgeList
{
    flowList* m_begin;

    // Forward iterator for the predecessor edges linked list.
    // The caller can't make changes to the preds list when using this.
    //
    class iterator
    {
        flowList* m_pred;

#ifdef DEBUG
        // Try to guard against the user of the iterator from making changes to the IR that would invalidate
        // the iterator: cache the edge we think should be next, then check it when we actually do the `++`
        // operation. This is a bit conservative, but attempts to protect against callers assuming too much about
        // this iterator implementation.
        flowList* m_next;
#endif

    public:
        iterator(flowList* pred);

        flowList* operator*() const
        {
            return m_pred;
        }

        iterator& operator++();

        bool operator!=(const iterator& i) const
        {
            return m_pred != i.m_pred;
        }
    };

public:
    PredEdgeList(flowList* pred) : m_begin(pred)
    {
    }

    iterator begin() const
    {
        return iterator(m_begin);
    }

    iterator end() const
    {
        return iterator(nullptr);
    }
};

// PredBlockList: adapter class for forward iteration of the predecessor edge linked list yielding
// predecessor blocks, using range-based `for`, normally used via BasicBlock::PredBlocks(), e.g.:
//    for (BasicBlock* const predBlock : block->PredBlocks()) ...
//
class PredBlockList
{
    flowList* m_begin;

    // Forward iterator for the predecessor edges linked list, yielding the predecessor block, not the edge.
    // The caller can't make changes to the preds list when using this.
    //
    class iterator
    {
        flowList* m_pred;

#ifdef DEBUG
        // Try to guard against the user of the iterator from making changes to the IR that would invalidate
        // the iterator: cache the edge we think should be next, then check it when we actually do the `++`
        // operation. This is a bit conservative, but attempts to protect against callers assuming too much about
        // this iterator implementation.
        flowList* m_next;
#endif

    public:
        iterator(flowList* pred);

        BasicBlock* operator*() const;

        iterator& operator++();

        bool operator!=(const iterator& i) const
        {
            return m_pred != i.m_pred;
        }
    };

public:
    PredBlockList(flowList* pred) : m_begin(pred)
    {
    }

    iterator begin() const
    {
        return iterator(m_begin);
    }

    iterator end() const
    {
        return iterator(nullptr);
    }
};

// BBArrayIterator: forward iterator for an array of BasicBlock*, such as the BBswtDesc->bbsDstTab.
// It is an error (with assert) to yield a nullptr BasicBlock* in this array.
// `m_bbEntry` can be nullptr, but it only makes sense if both the begin and end of an iteration range are nullptr
// (meaning, no actual iteration will happen).
//
class BBArrayIterator
{
    BasicBlock* const* m_bbEntry;

public:
    BBArrayIterator(BasicBlock* const* bbEntry) : m_bbEntry(bbEntry)
    {
    }

    BasicBlock* operator*() const
    {
        assert(m_bbEntry != nullptr);
        BasicBlock* bTarget = *m_bbEntry;
        assert(bTarget != nullptr);
        return bTarget;
    }

    BBArrayIterator& operator++()
    {
        assert(m_bbEntry != nullptr);
        ++m_bbEntry;
        return *this;
    }

    bool operator!=(const BBArrayIterator& i) const
    {
        return m_bbEntry != i.m_bbEntry;
    }
};

// BBSwitchTargetList: adapter class for forward iteration of switch targets, using range-based `for`,
// normally used via BasicBlock::SwitchTargets(), e.g.:
//    for (BasicBlock* const target : block->SwitchTargets()) ...
//
class BBSwitchTargetList
{
    BBswtDesc* m_bbsDesc;

public:
    BBSwitchTargetList(BBswtDesc* bbsDesc);
    BBArrayIterator begin() const;
    BBArrayIterator end() const;
};

//------------------------------------------------------------------------
// BasicBlockFlags: a bitmask of flags for BasicBlock
//
// clang-format off
enum BasicBlockFlags : unsigned __int64
{
#define MAKE_BBFLAG(bit) (1ULL << (bit))
    BBF_EMPTY                = 0,

    BBF_VISITED              = MAKE_BBFLAG( 0), // BB visited during optimizations
    BBF_MARKED               = MAKE_BBFLAG( 1), // BB marked  during optimizations
    BBF_CHANGED              = MAKE_BBFLAG( 2), // input/output of this block has changed
    BBF_REMOVED              = MAKE_BBFLAG( 3), // BB has been removed from bb-list

    BBF_DONT_REMOVE          = MAKE_BBFLAG( 4), // BB should not be removed during flow graph optimizations
    BBF_IMPORTED             = MAKE_BBFLAG( 5), // BB byte-code has been imported
    BBF_INTERNAL             = MAKE_BBFLAG( 6), // BB has been added by the compiler
    BBF_FAILED_VERIFICATION  = MAKE_BBFLAG( 7), // BB has verification exception

    BBF_TRY_BEG              = MAKE_BBFLAG( 8), // BB starts a 'try' block
    BBF_FUNCLET_BEG          = MAKE_BBFLAG( 9), // BB is the beginning of a funclet
    BBF_HAS_NULLCHECK        = MAKE_BBFLAG(10), // BB contains a null check
    BBF_HAS_SUPPRESSGC_CALL  = MAKE_BBFLAG(11), // BB contains a call to a method with SuppressGCTransitionAttribute

    BBF_RUN_RARELY           = MAKE_BBFLAG(12), // BB is rarely run (catch clauses, blocks with throws etc)
    BBF_LOOP_HEAD            = MAKE_BBFLAG(13), // BB is the head of a loop
    BBF_LOOP_CALL0           = MAKE_BBFLAG(14), // BB starts a loop that sometimes won't call
    BBF_LOOP_CALL1           = MAKE_BBFLAG(15), // BB starts a loop that will always     call

    BBF_HAS_LABEL            = MAKE_BBFLAG(16), // BB needs a label
    BBF_LOOP_ALIGN           = MAKE_BBFLAG(17), // Block is lexically the first block in a loop we intend to align.
    BBF_HAS_JMP              = MAKE_BBFLAG(18), // BB executes a JMP instruction (instead of return)
    BBF_GC_SAFE_POINT        = MAKE_BBFLAG(19), // BB has a GC safe point (a call).  More abstractly, BB does not require a
                                                // (further) poll -- this may be because this BB has a call, or, in some
                                                // cases, because the BB occurs in a loop, and we've determined that all
                                                // paths in the loop body leading to BB include a call.

    BBF_HAS_IDX_LEN          = MAKE_BBFLAG(20), // BB contains simple index or length expressions on an array local var.
    BBF_HAS_NEWARRAY         = MAKE_BBFLAG(21), // BB contains 'new' of an array
    BBF_HAS_NEWOBJ           = MAKE_BBFLAG(22), // BB contains 'new' of an object type.

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

    BBF_FINALLY_TARGET       = MAKE_BBFLAG(23), // BB is the target of a finally return: where a finally will return during
                                                // non-exceptional flow. Because the ARM calling sequence for calling a
                                                // finally explicitly sets the return address to the finally target and jumps
                                                // to the finally, instead of using a call instruction, ARM needs this to
                                                // generate correct code at the finally target, to allow for proper stack
                                                // unwind from within a non-exceptional call to a finally.

#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

    BBF_BACKWARD_JUMP        = MAKE_BBFLAG(24), // BB is surrounded by a backward jump/switch arc
    BBF_RETLESS_CALL         = MAKE_BBFLAG(25), // BBJ_CALLFINALLY that will never return (and therefore, won't need a paired
                                                // BBJ_ALWAYS); see isBBCallAlwaysPair().
    BBF_LOOP_PREHEADER       = MAKE_BBFLAG(26), // BB is a loop preheader block
    BBF_COLD                 = MAKE_BBFLAG(27), // BB is cold

    BBF_PROF_WEIGHT          = MAKE_BBFLAG(28), // BB weight is computed from profile data
    BBF_IS_LIR               = MAKE_BBFLAG(29), // Set if the basic block contains LIR (as opposed to HIR)
    BBF_KEEP_BBJ_ALWAYS      = MAKE_BBFLAG(30), // A special BBJ_ALWAYS block, used by EH code generation. Keep the jump kind
                                                // as BBJ_ALWAYS. Used for the paired BBJ_ALWAYS block following the
                                                // BBJ_CALLFINALLY block, as well as, on x86, the final step block out of a
                                                // finally.
    BBF_CLONED_FINALLY_BEGIN = MAKE_BBFLAG(31), // First block of a cloned finally region

    BBF_CLONED_FINALLY_END   = MAKE_BBFLAG(32), // Last block of a cloned finally region
    BBF_HAS_CALL             = MAKE_BBFLAG(33), // BB contains a call
    BBF_DOMINATED_BY_EXCEPTIONAL_ENTRY = MAKE_BBFLAG(34), // Block is dominated by exceptional entry.
    BBF_BACKWARD_JUMP_TARGET = MAKE_BBFLAG(35), // Block is a target of a backward jump

    BBF_PATCHPOINT           = MAKE_BBFLAG(36), // Block is a patchpoint
    BBF_HAS_CLASS_PROFILE    = MAKE_BBFLAG(37), // BB contains a call needing a class profile

    // The following are sets of flags.

    // Flags that relate blocks to loop structure.

    BBF_LOOP_FLAGS = BBF_LOOP_PREHEADER | BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1,

    // Flags to update when two blocks are compacted

    BBF_COMPACT_UPD = BBF_CHANGED | BBF_GC_SAFE_POINT | BBF_HAS_JMP | BBF_HAS_IDX_LEN | BBF_BACKWARD_JUMP | BBF_HAS_NEWARRAY | \
                      BBF_HAS_NEWOBJ | BBF_HAS_NULLCHECK,

    // Flags a block should not have had before it is split.

    BBF_SPLIT_NONEXIST = BBF_CHANGED | BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1 | BBF_RETLESS_CALL | BBF_LOOP_PREHEADER | BBF_COLD,

    // Flags lost by the top block when a block is split.
    // Note, this is a conservative guess.
    // For example, the top block might or might not have BBF_GC_SAFE_POINT,
    // but we assume it does not have BBF_GC_SAFE_POINT any more.

    BBF_SPLIT_LOST = BBF_GC_SAFE_POINT | BBF_HAS_JMP | BBF_KEEP_BBJ_ALWAYS | BBF_CLONED_FINALLY_END,

    // Flags gained by the bottom block when a block is split.
    // Note, this is a conservative guess.
    // For example, the bottom block might or might not have BBF_HAS_NEWARRAY or BBF_HAS_NULLCHECK,
    // but we assume it has BBF_HAS_NEWARRAY and BBF_HAS_NULLCHECK.
    // TODO: Should BBF_RUN_RARELY be added to BBF_SPLIT_GAINED ?

    BBF_SPLIT_GAINED = BBF_DONT_REMOVE | BBF_HAS_JMP | BBF_BACKWARD_JUMP | BBF_HAS_IDX_LEN | BBF_HAS_NEWARRAY | BBF_PROF_WEIGHT | \
                       BBF_HAS_NEWOBJ | BBF_KEEP_BBJ_ALWAYS | BBF_CLONED_FINALLY_END | BBF_HAS_NULLCHECK | BBF_HAS_CLASS_PROFILE,
};

inline constexpr BasicBlockFlags operator ~(BasicBlockFlags a)
{
    return (BasicBlockFlags)(~(unsigned __int64)a);
}

inline constexpr BasicBlockFlags operator |(BasicBlockFlags a, BasicBlockFlags b)
{
    return (BasicBlockFlags)((unsigned __int64)a | (unsigned __int64)b);
}

inline constexpr BasicBlockFlags operator &(BasicBlockFlags a, BasicBlockFlags b)
{
    return (BasicBlockFlags)((unsigned __int64)a & (unsigned __int64)b);
}

inline BasicBlockFlags& operator |=(BasicBlockFlags& a, BasicBlockFlags b)
{
    return a = (BasicBlockFlags)((unsigned __int64)a | (unsigned __int64)b);
}

inline BasicBlockFlags& operator &=(BasicBlockFlags& a, BasicBlockFlags b)
{
    return a = (BasicBlockFlags)((unsigned __int64)a & (unsigned __int64)b);
}

// clang-format on

//------------------------------------------------------------------------
// BasicBlock: describes a basic block in the flowgraph.
//
// Note that this type derives from LIR::Range in order to make the LIR
// utilities that are polymorphic over basic block and scratch ranges
// faster and simpler.
//
struct BasicBlock : private LIR::Range
{
    friend class LIR;

    BasicBlock* bbNext; // next BB in ascending PC offset order
    BasicBlock* bbPrev;

    void setNext(BasicBlock* next)
    {
        bbNext = next;
        if (next)
        {
            next->bbPrev = this;
        }
    }

    BasicBlockFlags bbFlags;

    static_assert_no_msg((BBF_SPLIT_NONEXIST & BBF_SPLIT_LOST) == 0);
    static_assert_no_msg((BBF_SPLIT_NONEXIST & BBF_SPLIT_GAINED) == 0);

    unsigned bbNum; // the block's number

    unsigned bbRefs; // number of blocks that can reach here, either by fall-through or a branch. If this falls to zero,
                     // the block is unreachable.

    bool isRunRarely() const
    {
        return ((bbFlags & BBF_RUN_RARELY) != 0);
    }
    bool isLoopHead() const
    {
        return ((bbFlags & BBF_LOOP_HEAD) != 0);
    }
    bool isLoopAlign() const
    {
        return ((bbFlags & BBF_LOOP_ALIGN) != 0);
    }

#ifdef DEBUG
    void     dspFlags();               // Print the flags
    unsigned dspCheapPreds();          // Print the predecessors (bbCheapPreds)
    unsigned dspPreds();               // Print the predecessors (bbPreds)
    void dspSuccs(Compiler* compiler); // Print the successors. The 'compiler' argument determines whether EH
                                       // regions are printed: see NumSucc() for details.
    void dspJumpKind();                // Print the block jump kind (e.g., BBJ_NONE, BBJ_COND, etc.).

    // Print a simple basic block header for various output, including a list of predecessors and successors.
    void dspBlockHeader(Compiler* compiler, bool showKind = true, bool showFlags = false, bool showPreds = true);

    const char* dspToString(int blockNumPadding = 0);
#endif // DEBUG

    // Type used to hold block and edge weights
    typedef float weight_t;

#define BB_UNITY_WEIGHT 100.0f       // how much a normal execute once block weighs
#define BB_UNITY_WEIGHT_UNSIGNED 100 // how much a normal execute once block weighs
#define BB_LOOP_WEIGHT_SCALE 8.0f    // synthetic profile scale factor for loops
#define BB_ZERO_WEIGHT 0.0f
#define BB_MAX_WEIGHT FLT_MAX // maximum finite weight  -- needs rethinking.

    weight_t bbWeight; // The dynamic execution weight of this block

    // getCalledCount -- get the value used to normalize weights for this method
    static weight_t getCalledCount(Compiler* comp);

    // getBBWeight -- get the normalized weight of this block
    weight_t getBBWeight(Compiler* comp);

    // hasProfileWeight -- Returns true if this block's weight came from profile data
    bool hasProfileWeight() const
    {
        return ((this->bbFlags & BBF_PROF_WEIGHT) != 0);
    }

    // setBBProfileWeight -- Set the profile-derived weight for a basic block
    // and update the run rarely flag as appropriate.
    void setBBProfileWeight(weight_t weight)
    {
        this->bbFlags |= BBF_PROF_WEIGHT;
        this->bbWeight = weight;

        if (weight == BB_ZERO_WEIGHT)
        {
            this->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            this->bbFlags &= ~BBF_RUN_RARELY;
        }
    }

    // this block will inherit the same weight and relevant bbFlags as bSrc
    //
    void inheritWeight(BasicBlock* bSrc)
    {
        inheritWeightPercentage(bSrc, 100);
    }

    // Similar to inheritWeight(), but we're splitting a block (such as creating blocks for qmark removal).
    // So, specify a percentage (0 to 100) of the weight the block should inherit.
    //
    // Can be invoked as a self-rescale, eg: block->inheritWeightPecentage(block, 50))
    //
    void inheritWeightPercentage(BasicBlock* bSrc, unsigned percentage)
    {
        assert(0 <= percentage && percentage <= 100);

        this->bbWeight = (bSrc->bbWeight * percentage) / 100;

        if (bSrc->hasProfileWeight())
        {
            this->bbFlags |= BBF_PROF_WEIGHT;
        }
        else
        {
            this->bbFlags &= ~BBF_PROF_WEIGHT;
        }

        if (this->bbWeight == BB_ZERO_WEIGHT)
        {
            this->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            this->bbFlags &= ~BBF_RUN_RARELY;
        }
    }

    // Scale a blocks' weight by some factor.
    //
    void scaleBBWeight(BasicBlock::weight_t scale)
    {
        this->bbWeight = this->bbWeight * scale;

        if (this->bbWeight == BB_ZERO_WEIGHT)
        {
            this->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            this->bbFlags &= ~BBF_RUN_RARELY;
        }
    }

    // Set block weight to zero, and set run rarely flag.
    //
    void bbSetRunRarely()
    {
        this->scaleBBWeight(BB_ZERO_WEIGHT);
    }

    // makeBlockHot()
    //     This is used to override any profiling data
    //     and force a block to be in the hot region.
    //     We only call this method for handler entry point
    //     and only when HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION is 1.
    //     Doing this helps fgReorderBlocks() by telling
    //     it to try to move these blocks into the hot region.
    //     Note that we do this strictly as an optimization,
    //     not for correctness. fgDetermineFirstColdBlock()
    //     will find all handler entry points and ensure that
    //     for now we don't place them in the cold section.
    //
    void makeBlockHot()
    {
        if (this->bbWeight == BB_ZERO_WEIGHT)
        {
            this->bbFlags &= ~BBF_RUN_RARELY;  // Clear any RarelyRun flag
            this->bbFlags &= ~BBF_PROF_WEIGHT; // Clear any profile-derived flag
            this->bbWeight = 1;
        }
    }

    bool isMaxBBWeight() const
    {
        return (bbWeight >= BB_MAX_WEIGHT);
    }

    // Returns "true" if the block is empty. Empty here means there are no statement
    // trees *except* PHI definitions.
    bool isEmpty() const;

    bool isValid() const;

    // Returns "true" iff "this" is the first block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair --
    // a block corresponding to an exit from the try of a try/finally.
    bool isBBCallAlwaysPair() const;

    // Returns "true" iff "this" is the last block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair --
    // a block corresponding to an exit from the try of a try/finally.
    bool isBBCallAlwaysPairTail() const;

    BBjumpKinds bbJumpKind; // jump (if any) at the end of this block

    /* The following union describes the jump target(s) of this block */
    union {
        unsigned    bbJumpOffs; // PC offset (temporary only)
        BasicBlock* bbJumpDest; // basic block
        BBswtDesc*  bbJumpSwt;  // switch descriptor
    };

    // NumSucc() gives the number of successors, and GetSucc() returns a given numbered successor.
    //
    // There are two versions of these functions: ones that take a Compiler* and ones that don't. You must
    // always use a matching set. Thus, if you call NumSucc() without a Compiler*, you must also call
    // GetSucc() without a Compiler*.
    //
    // The behavior of NumSucc()/GetSucc() is different when passed a Compiler* for blocks that end in:
    // (1) BBJ_EHFINALLYRET (a return from a finally or fault block)
    // (2) BBJ_EHFILTERRET (a return from EH filter block)
    // (3) BBJ_SWITCH
    //
    // For BBJ_EHFINALLYRET, if no Compiler* is passed, then the block is considered to have no
    // successor. If Compiler* is passed, we figure out the actual successors. Some cases will want one behavior,
    // other cases the other. For example, IL verification requires that these blocks end in an empty operand
    // stack, and since the dataflow analysis of IL verification is concerned only with the contents of the
    // operand stack, we can consider the finally block to have no successors. But a more general dataflow
    // analysis that is tracking the contents of local variables might want to consider *all* successors,
    // and would pass the current Compiler object.
    //
    // Similarly, BBJ_EHFILTERRET blocks are assumed to have no successors if Compiler* is not passed; if
    // Compiler* is passed, NumSucc/GetSucc yields the first block of the try block's handler.
    //
    // For BBJ_SWITCH, if Compiler* is not passed, then all switch successors are returned. If Compiler*
    // is passed, then only unique switch successors are returned; the duplicate successors are omitted.
    //
    // Note that for BBJ_COND, which has two successors (fall through and condition true branch target),
    // only the unique targets are returned. Thus, if both targets are the same, NumSucc() will only return 1
    // instead of 2.
    //
    // NumSucc: Returns the number of successors of "this".
    unsigned NumSucc() const;
    unsigned NumSucc(Compiler* comp);

    // GetSucc: Returns the "i"th successor. Requires (0 <= i < NumSucc()).
    BasicBlock* GetSucc(unsigned i) const;
    BasicBlock* GetSucc(unsigned i, Compiler* comp);

    // SwitchTargets: convenience methods for enabling range-based `for` iteration over a switch block's targets, e.g.:
    //    for (BasicBlock* const bTarget : block->SwitchTargets()) ...
    //
    BBSwitchTargetList SwitchTargets() const
    {
        assert(bbJumpKind == BBJ_SWITCH);
        return BBSwitchTargetList(bbJumpSwt);
    }

    BasicBlock* GetUniquePred(Compiler* comp) const;

    BasicBlock* GetUniqueSucc() const;

    unsigned countOfInEdges() const
    {
        return bbRefs;
    }

    Statement* bbStmtList;

    GenTree* GetFirstLIRNode() const
    {
        return m_firstNode;
    }

    void SetFirstLIRNode(GenTree* tree)
    {
        m_firstNode = tree;
    }

    union {
        EntryState* bbEntryState; // verifier tracked state of all entries in stack.
        flowList*   bbLastPred;   // last pred list entry
    };

#define NO_BASE_TMP UINT_MAX // base# to use when we have none

    union {
        unsigned bbStkTempsIn;       // base# for input stack temps
        int      bbCountSchemaIndex; // schema index for count instrumentation
    };

    union {
        unsigned bbStkTempsOut;      // base# for output stack temps
        int      bbClassSchemaIndex; // schema index for class instrumentation
    };

#define MAX_XCPTN_INDEX (USHRT_MAX - 1)

    // It would be nice to make bbTryIndex and bbHndIndex private, but there is still code that uses them directly,
    // especially Compiler::fgNewBBinRegion() and friends.

    // index, into the compHndBBtab table, of innermost 'try' clause containing the BB (used for raising exceptions).
    // Stored as index + 1; 0 means "no try index".
    unsigned short bbTryIndex;

    // index, into the compHndBBtab table, of innermost handler (filter, catch, fault/finally) containing the BB.
    // Stored as index + 1; 0 means "no handler index".
    unsigned short bbHndIndex;

    // Given two EH indices that are either bbTryIndex or bbHndIndex (or related), determine if index1 might be more
    // deeply nested than index2. Both index1 and index2 are in the range [0..compHndBBtabCount], where 0 means
    // "main function" and otherwise the value is an index into compHndBBtab[]. Note that "sibling" EH regions will
    // have a numeric index relationship that doesn't indicate nesting, whereas a more deeply nested region must have
    // a lower index than the region it is nested within. Note that if you compare a single block's bbTryIndex and
    // bbHndIndex, there is guaranteed to be a nesting relationship, since that block can't be simultaneously in two
    // sibling EH regions. In that case, "maybe" is actually "definitely".
    static bool ehIndexMaybeMoreNested(unsigned index1, unsigned index2)
    {
        if (index1 == 0)
        {
            // index1 is in the main method. It can't be more deeply nested than index2.
            return false;
        }
        else if (index2 == 0)
        {
            // index1 represents an EH region, whereas index2 is the main method. Thus, index1 is more deeply nested.
            assert(index1 > 0);
            return true;
        }
        else
        {
            // If index1 has a smaller index, it might be more deeply nested than index2.
            assert(index1 > 0);
            assert(index2 > 0);
            return index1 < index2;
        }
    }

    // catch type: class token of handler, or one of BBCT_*. Only set on first block of catch handler.
    unsigned bbCatchTyp;

    bool hasTryIndex() const
    {
        return bbTryIndex != 0;
    }
    bool hasHndIndex() const
    {
        return bbHndIndex != 0;
    }
    unsigned getTryIndex() const
    {
        assert(bbTryIndex != 0);
        return bbTryIndex - 1;
    }
    unsigned getHndIndex() const
    {
        assert(bbHndIndex != 0);
        return bbHndIndex - 1;
    }
    void setTryIndex(unsigned val)
    {
        bbTryIndex = (unsigned short)(val + 1);
        assert(bbTryIndex != 0);
    }
    void setHndIndex(unsigned val)
    {
        bbHndIndex = (unsigned short)(val + 1);
        assert(bbHndIndex != 0);
    }
    void clearTryIndex()
    {
        bbTryIndex = 0;
    }
    void clearHndIndex()
    {
        bbHndIndex = 0;
    }

    void copyEHRegion(const BasicBlock* from)
    {
        bbTryIndex = from->bbTryIndex;
        bbHndIndex = from->bbHndIndex;
    }

    static bool sameTryRegion(const BasicBlock* blk1, const BasicBlock* blk2)
    {
        return blk1->bbTryIndex == blk2->bbTryIndex;
    }
    static bool sameHndRegion(const BasicBlock* blk1, const BasicBlock* blk2)
    {
        return blk1->bbHndIndex == blk2->bbHndIndex;
    }
    static bool sameEHRegion(const BasicBlock* blk1, const BasicBlock* blk2)
    {
        return sameTryRegion(blk1, blk2) && sameHndRegion(blk1, blk2);
    }

    bool hasEHBoundaryIn() const;
    bool hasEHBoundaryOut() const;

// Some non-zero value that will not collide with real tokens for bbCatchTyp
#define BBCT_NONE 0x00000000
#define BBCT_FAULT 0xFFFFFFFC
#define BBCT_FINALLY 0xFFFFFFFD
#define BBCT_FILTER 0xFFFFFFFE
#define BBCT_FILTER_HANDLER 0xFFFFFFFF
#define handlerGetsXcptnObj(hndTyp) ((hndTyp) != BBCT_NONE && (hndTyp) != BBCT_FAULT && (hndTyp) != BBCT_FINALLY)

    // The following fields are used for loop detection
    typedef unsigned char loopNumber;
    static const unsigned NOT_IN_LOOP = UCHAR_MAX;

    loopNumber bbNatLoopNum; // Index, in optLoopTable, of most-nested loop that contains this block,
                             // or else NOT_IN_LOOP if this block is not in a loop.

#define MAX_LOOP_NUM 16       // we're using a 'short' for the mask
#define LOOP_MASK_TP unsigned // must be big enough for a mask

    // TODO-Cleanup: Get rid of bbStkDepth and use bbStackDepthOnEntry() instead
    union {
        unsigned short bbStkDepth; // stack depth on entry
        unsigned short bbFPinVars; // number of inner enregistered FP vars
    };

    // Basic block predecessor lists. Early in compilation, some phases might need to compute "cheap" predecessor
    // lists. These are stored in bbCheapPreds, computed by fgComputeCheapPreds(). If bbCheapPreds is valid,
    // 'fgCheapPredsValid' will be 'true'. Later, the "full" predecessor lists are created by fgComputePreds(), stored
    // in 'bbPreds', and then maintained throughout compilation. 'fgComputePredsDone' will be 'true' after the
    // full predecessor lists are created. See the comment at fgComputeCheapPreds() to see how those differ from
    // the "full" variant.
    union {
        BasicBlockList* bbCheapPreds; // ptr to list of cheap predecessors (used before normal preds are computed)
        flowList*       bbPreds;      // ptr to list of predecessors
    };

    // PredEdges: convenience method for enabling range-based `for` iteration over predecessor edges, e.g.:
    //    for (flowList* const edge : block->PredEdges()) ...
    //
    PredEdgeList PredEdges() const
    {
        return PredEdgeList(bbPreds);
    }

    // PredBlocks: convenience method for enabling range-based `for` iteration over predecessor blocks, e.g.:
    //    for (BasicBlock* const predBlock : block->PredBlocks()) ...
    //
    PredBlockList PredBlocks() const
    {
        return PredBlockList(bbPreds);
    }

    // Pred list maintenance
    //
    bool checkPredListOrder();
    void ensurePredListOrder(Compiler* compiler);
    void reorderPredList(Compiler* compiler);

    BlockSet bbReach; // Set of all blocks that can reach this one

    union {
        BasicBlock* bbIDom;      // Represent the closest dominator to this block (called the Immediate
                                 // Dominator) used to compute the dominance tree.
        void* bbSparseProbeList; // Used early on by fgInstrument
        void* bbSparseCountInfo; // Used early on by fgIncorporateEdgeCounts
    };

    unsigned bbPostOrderNum; // the block's post order number in the graph.

    IL_OFFSET bbCodeOffs;    // IL offset of the beginning of the block
    IL_OFFSET bbCodeOffsEnd; // IL offset past the end of the block. Thus, the [bbCodeOffs..bbCodeOffsEnd)
                             // range is not inclusive of the end offset. The count of IL bytes in the block
                             // is bbCodeOffsEnd - bbCodeOffs, assuming neither are BAD_IL_OFFSET.

#ifdef DEBUG
    void dspBlockILRange() const; // Display the block's IL range as [XXX...YYY), where XXX and YYY might be "???" for
                                  // BAD_IL_OFFSET.
#endif                            // DEBUG

    VARSET_TP bbVarUse; // variables used     by block (before an assignment)
    VARSET_TP bbVarDef; // variables assigned by block (before a use)

    VARSET_TP bbLiveIn;  // variables live on entry
    VARSET_TP bbLiveOut; // variables live on exit

    // Use, def, live in/out information for the implicit memory variable.
    MemoryKindSet bbMemoryUse : MemoryKindCount; // must be set for any MemoryKinds this block references
    MemoryKindSet bbMemoryDef : MemoryKindCount; // must be set for any MemoryKinds this block mutates
    MemoryKindSet bbMemoryLiveIn : MemoryKindCount;
    MemoryKindSet bbMemoryLiveOut : MemoryKindCount;
    MemoryKindSet bbMemoryHavoc : MemoryKindCount; // If true, at some point the block does an operation
                                                   // that leaves memory in an unknown state. (E.g.,
                                                   // unanalyzed call, store through unknown pointer...)

    // We want to make phi functions for the special implicit var memory.  But since this is not a real
    // lclVar, and thus has no local #, we can't use a GenTreePhiArg.  Instead, we use this struct.
    struct MemoryPhiArg
    {
        unsigned      m_ssaNum;  // SSA# for incoming value.
        MemoryPhiArg* m_nextArg; // Next arg in the list, else NULL.

        unsigned GetSsaNum()
        {
            return m_ssaNum;
        }

        MemoryPhiArg(unsigned ssaNum, MemoryPhiArg* nextArg = nullptr) : m_ssaNum(ssaNum), m_nextArg(nextArg)
        {
        }

        void* operator new(size_t sz, class Compiler* comp);
    };
    static MemoryPhiArg* EmptyMemoryPhiDef; // Special value (0x1, FWIW) to represent a to-be-filled in Phi arg list
                                            // for Heap.
    MemoryPhiArg* bbMemorySsaPhiFunc[MemoryKindCount]; // If the "in" Heap SSA var is not a phi definition, this value
                                                       // is NULL.
    // Otherwise, it is either the special value EmptyMemoryPhiDefn, to indicate
    // that Heap needs a phi definition on entry, or else it is the linked list
    // of the phi arguments.
    unsigned bbMemorySsaNumIn[MemoryKindCount];  // The SSA # of memory on entry to the block.
    unsigned bbMemorySsaNumOut[MemoryKindCount]; // The SSA # of memory on exit from the block.

    VARSET_TP bbScope; // variables in scope over the block

    void InitVarSets(class Compiler* comp);

    /* The following are the standard bit sets for dataflow analysis.
     *  We perform CSE and range-checks at the same time
     *  and assertion propagation separately,
     *  thus we can union them since the two operations are completely disjunct.
     */

    union {
        EXPSET_TP bbCseGen; // CSEs computed by block
#if ASSERTION_PROP
        ASSERT_TP bbAssertionGen; // value assignments computed by block
#endif
    };

    union {
        EXPSET_TP bbCseIn; // CSEs available on entry
#if ASSERTION_PROP
        ASSERT_TP bbAssertionIn; // value assignments available on entry
#endif
    };

    union {
        EXPSET_TP bbCseOut; // CSEs available on exit
#if ASSERTION_PROP
        ASSERT_TP bbAssertionOut; // value assignments available on exit
#endif
    };

    void* bbEmitCookie;

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    void* bbUnwindNopEmitCookie;
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

#ifdef VERIFIER
    stackDesc bbStackIn;  // stack descriptor for  input
    stackDesc bbStackOut; // stack descriptor for output

    verTypeVal* bbTypesIn;  // list of variable types on  input
    verTypeVal* bbTypesOut; // list of variable types on output
#endif                      // VERIFIER

//-------------------------------------------------------------------------

#if MEASURE_BLOCK_SIZE
    static size_t s_Size;
    static size_t s_Count;
#endif // MEASURE_BLOCK_SIZE

    bool bbFallsThrough() const;

    // Our slop fraction is 1/128 of the block weight rounded off
    static weight_t GetSlopFraction(weight_t weightBlk)
    {
        return ((weightBlk + 64) / 128);
    }

    // Given an the edge b1 -> b2, calculate the slop fraction by
    // using the higher of the two block weights
    static weight_t GetSlopFraction(BasicBlock* b1, BasicBlock* b2)
    {
        return GetSlopFraction(max(b1->bbWeight, b2->bbWeight));
    }

#ifdef DEBUG
    unsigned        bbTgtStkDepth; // Native stack depth on entry (for throw-blocks)
    static unsigned s_nMaxTrees;   // The max # of tree nodes in any BB

    // This is used in integrity checks.  We semi-randomly pick a traversal stamp, label all blocks
    // in the BB list with that stamp (in this field); then we can tell if (e.g.) predecessors are
    // still in the BB list by whether they have the same stamp (with high probability).
    unsigned bbTraversalStamp;

    // bbID is a unique block identifier number that does not change: it does not get renumbered, like bbNum.
    unsigned bbID;
#endif // DEBUG

    ThisInitState bbThisOnEntry() const;
    unsigned      bbStackDepthOnEntry() const;
    void bbSetStack(void* stackBuffer);
    StackEntry* bbStackOnEntry() const;

    // "bbNum" is one-based (for unknown reasons); it is sometimes useful to have the corresponding
    // zero-based number for use as an array index.
    unsigned bbInd() const
    {
        assert(bbNum > 0);
        return bbNum - 1;
    }

    Statement* firstStmt() const;
    Statement* lastStmt() const;

    // Statements: convenience method for enabling range-based `for` iteration over the statement list, e.g.:
    //    for (Statement* const stmt : block->Statements())
    //
    StatementList Statements() const
    {
        return StatementList(firstStmt());
    }

    // NonPhiStatements: convenience method for enabling range-based `for` iteration over the statement list,
    // excluding any initial PHI statements, e.g.:
    //    for (Statement* const stmt : block->NonPhiStatements())
    //
    StatementList NonPhiStatements() const
    {
        return StatementList(FirstNonPhiDef());
    }

    GenTree* firstNode() const;
    GenTree* lastNode() const;

    bool endsWithJmpMethod(Compiler* comp) const;

    bool endsWithTailCall(Compiler* comp,
                          bool      fastTailCallsOnly,
                          bool      tailCallsConvertibleToLoopOnly,
                          GenTree** tailCall) const;

    bool endsWithTailCallOrJmp(Compiler* comp, bool fastTailCallsOnly = false) const;

    bool endsWithTailCallConvertibleToLoop(Compiler* comp, GenTree** tailCall) const;

    // Returns the first statement in the statement list of "this" that is
    // not an SSA definition (a lcl = phi(...) assignment).
    Statement* FirstNonPhiDef() const;
    Statement* FirstNonPhiDefOrCatchArgAsg() const;

    BasicBlock() : bbStmtList(nullptr), bbLiveIn(VarSetOps::UninitVal()), bbLiveOut(VarSetOps::UninitVal())
    {
    }

    // Iteratable collection of successors of a block.
    template <typename TPosition>
    class Successors
    {
        Compiler*   m_comp;
        BasicBlock* m_block;

    public:
        Successors(Compiler* comp, BasicBlock* block) : m_comp(comp), m_block(block)
        {
        }

        class iterator
        {
            Compiler*   m_comp;
            BasicBlock* m_block;
            TPosition   m_pos;

        public:
            iterator(Compiler* comp, BasicBlock* block) : m_comp(comp), m_block(block), m_pos(comp, block)
            {
            }

            iterator() : m_pos()
            {
            }

            void operator++(void)
            {
                m_pos.Advance(m_comp, m_block);
            }

            BasicBlock* operator*()
            {
                return m_pos.Current(m_comp, m_block);
            }

            bool operator==(const iterator& other)
            {
                return m_pos == other.m_pos;
            }

            bool operator!=(const iterator& other)
            {
                return m_pos != other.m_pos;
            }
        };

        iterator begin()
        {
            return iterator(m_comp, m_block);
        }

        iterator end()
        {
            return iterator();
        }
    };

    Successors<EHSuccessorIterPosition> GetEHSuccs(Compiler* comp)
    {
        return Successors<EHSuccessorIterPosition>(comp, this);
    }

    Successors<AllSuccessorIterPosition> GetAllSuccs(Compiler* comp)
    {
        return Successors<AllSuccessorIterPosition>(comp, this);
    }

    // BBSuccList: adapter class for forward iteration of block successors, using range-based `for`,
    // normally used via BasicBlock::Succs(), e.g.:
    //    for (BasicBlock* const target : block->Succs()) ...
    //
    class BBSuccList
    {
        // For one or two successors, pre-compute and stash the successors inline, in m_succs[], so we don't
        // need to call a function or execute another `switch` to get them. Also, pre-compute the begin and end
        // points of the iteration, for use by BBArrayIterator. `m_begin` and `m_end` will either point at
        // `m_succs` or at the switch table successor array.
        BasicBlock*        m_succs[2];
        BasicBlock* const* m_begin;
        BasicBlock* const* m_end;

    public:
        BBSuccList(const BasicBlock* block);
        BBArrayIterator begin() const;
        BBArrayIterator end() const;
    };

    // BBCompilerSuccList: adapter class for forward iteration of block successors, using range-based `for`,
    // normally used via BasicBlock::Succs(), e.g.:
    //    for (BasicBlock* const target : block->Succs(compiler)) ...
    //
    // This version uses NumSucc(Compiler*)/GetSucc(Compiler*). See the documentation there for the explanation
    // of the implications of this versus the version that does not take `Compiler*`.
    class BBCompilerSuccList
    {
        Compiler*   m_comp;
        BasicBlock* m_block;

        // iterator: forward iterator for an array of BasicBlock*, such as the BBswtDesc->bbsDstTab.
        //
        class iterator
        {
            Compiler*   m_comp;
            BasicBlock* m_block;
            unsigned    m_succNum;

        public:
            iterator(Compiler* comp, BasicBlock* block, unsigned succNum)
                : m_comp(comp), m_block(block), m_succNum(succNum)
            {
            }

            BasicBlock* operator*() const
            {
                assert(m_block != nullptr);
                BasicBlock* bTarget = m_block->GetSucc(m_succNum, m_comp);
                assert(bTarget != nullptr);
                return bTarget;
            }

            iterator& operator++()
            {
                ++m_succNum;
                return *this;
            }

            bool operator!=(const iterator& i) const
            {
                return m_succNum != i.m_succNum;
            }
        };

    public:
        BBCompilerSuccList(Compiler* comp, BasicBlock* block) : m_comp(comp), m_block(block)
        {
        }

        iterator begin() const
        {
            return iterator(m_comp, m_block, 0);
        }

        iterator end() const
        {
            return iterator(m_comp, m_block, m_block->NumSucc(m_comp));
        }
    };

    // Succs: convenience methods for enabling range-based `for` iteration over a block's successors, e.g.:
    //    for (BasicBlock* const succ : block->Succs()) ...
    //
    // There are two options: one that takes a Compiler* and one that doesn't. These correspond to the
    // NumSucc()/GetSucc() functions that do or do not take a Compiler*. See the comment for NumSucc()/GetSucc()
    // for the distinction.
    BBSuccList Succs() const
    {
        return BBSuccList(this);
    }

    BBCompilerSuccList Succs(Compiler* comp)
    {
        return BBCompilerSuccList(comp, this);
    }

    // Try to clone block state and statements from `from` block to `to` block (which must be new/empty),
    // optionally replacing uses of local `varNum` with IntCns `varVal`.  Return true if all statements
    // in the block are cloned successfully, false (with partially-populated `to` block) if one fails.
    static bool CloneBlockState(
        Compiler* compiler, BasicBlock* to, const BasicBlock* from, unsigned varNum = (unsigned)-1, int varVal = 0);

    void MakeLIR(GenTree* firstNode, GenTree* lastNode);
    bool IsLIR() const;

    void SetDominatedByExceptionalEntryFlag()
    {
        bbFlags |= BBF_DOMINATED_BY_EXCEPTIONAL_ENTRY;
    }

    bool IsDominatedByExceptionalEntryFlag() const
    {
        return (bbFlags & BBF_DOMINATED_BY_EXCEPTIONAL_ENTRY) != 0;
    }

#ifdef DEBUG
    bool Contains(const GenTree* node) const
    {
        assert(IsLIR());
        for (Iterator iter = begin(); iter != end(); ++iter)
        {
            if (*iter == node)
            {
                return true;
            }
        }
        return false;
    }
#endif // DEBUG
};

template <>
struct JitPtrKeyFuncs<BasicBlock> : public JitKeyFuncsDefEquals<const BasicBlock*>
{
public:
    // Make sure hashing is deterministic and not on "ptr."
    static unsigned GetHashCode(const BasicBlock* ptr);
};

// A set of blocks.
typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, bool> BlkSet;

// A vector of blocks.
typedef jitstd::vector<BasicBlock*> BlkVector;

// A map of block -> set of blocks, can be used as sparse block trees.
typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, BlkSet*> BlkToBlkSetMap;

// A map of block -> vector of blocks, can be used as sparse block trees.
typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, BlkVector> BlkToBlkVectorMap;

// Map from Block to Block.  Used for a variety of purposes.
typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, BasicBlock*> BlockToBlockMap;

// BasicBlockIterator: forward iterator for the BasicBlock linked list.
// It is allowed to make changes to the BasicBlock list as long as the current block remains in the list.
// E.g., the current block `m_bbNext` pointer can be altered (such as when inserting a following block),
// as long as the current block is still in the list.
// The block list is expected to be properly doubly-linked.
//
class BasicBlockIterator
{
    BasicBlock* m_block;

public:
    BasicBlockIterator(BasicBlock* block) : m_block(block)
    {
    }

    BasicBlock* operator*() const
    {
        return m_block;
    }

    BasicBlockIterator& operator++()
    {
        assert(m_block != nullptr);
        // Check that we haven't been spliced out of the list.
        assert((m_block->bbNext == nullptr) || (m_block->bbNext->bbPrev == m_block));
        assert((m_block->bbPrev == nullptr) || (m_block->bbPrev->bbNext == m_block));

        m_block = m_block->bbNext;
        return *this;
    }

    bool operator!=(const BasicBlockIterator& i) const
    {
        return m_block != i.m_block;
    }
};

// BasicBlockSimpleList: adapter class for forward iteration of a lexically contiguous range of
// BasicBlock, starting at `begin` and going to the end of the function, using range-based `for`,
// normally used via Compiler::Blocks(), e.g.:
//    for (BasicBlock* const block : Blocks()) ...
//
class BasicBlockSimpleList
{
    BasicBlock* m_begin;

public:
    BasicBlockSimpleList(BasicBlock* begin) : m_begin(begin)
    {
    }

    BasicBlockIterator begin() const
    {
        return BasicBlockIterator(m_begin);
    }

    BasicBlockIterator end() const
    {
        return BasicBlockIterator(nullptr);
    }
};

// BasicBlockRangeList: adapter class for forward iteration of a lexically contiguous range of
// BasicBlock specified with both `begin` and `end` blocks. `begin` and `end` are *inclusive*
// and must be non-null. E.g.,
//    for (BasicBlock* const block : BasicBlockRangeList(startBlock, endBlock)) ...
//
class BasicBlockRangeList
{
    BasicBlock* m_begin;
    BasicBlock* m_end;

public:
    BasicBlockRangeList(BasicBlock* begin, BasicBlock* end) : m_begin(begin), m_end(end)
    {
        assert(begin != nullptr);
        assert(end != nullptr);
    }

    BasicBlockIterator begin() const
    {
        return BasicBlockIterator(m_begin);
    }

    BasicBlockIterator end() const
    {
        return BasicBlockIterator(m_end->bbNext); // walk until we see the block *following* the `m_end` block
    }
};

// BBswtDesc -- descriptor for a switch block
//
//  Things to know:
//  1. If bbsHasDefault is true, the default case is the last one in the array of basic block addresses
//     namely bbsDstTab[bbsCount - 1].
//  2. bbsCount must be at least 1, for the default case. bbsCount cannot be zero. It appears that the ECMA spec
//     allows for a degenerate switch with zero cases. Normally, the optimizer will optimize degenerate
//     switches with just a default case to a BBJ_ALWAYS branch, and a switch with just two cases to a BBJ_COND.
//     However, in debuggable code, we might not do that, so bbsCount might be 1.
//
struct BBswtDesc
{
    BasicBlock** bbsDstTab; // case label table address
    unsigned     bbsCount;  // count of cases (includes 'default' if bbsHasDefault)

    // Case number and likelihood of most likely case
    // (only known with PGO, only valid if bbsHasDominantCase is true)
    unsigned             bbsDominantCase;
    BasicBlock::weight_t bbsDominantFraction;

    bool bbsHasDefault;      // true if last switch case is a default case
    bool bbsHasDominantCase; // true if switch has a dominant case

    BBswtDesc() : bbsHasDefault(true), bbsHasDominantCase(false)
    {
    }

    BBswtDesc(Compiler* comp, const BBswtDesc* other);

    void removeDefault()
    {
        assert(bbsHasDefault);
        assert(bbsCount > 0);
        bbsHasDefault = false;
        bbsCount--;
    }

    BasicBlock* getDefault()
    {
        assert(bbsHasDefault);
        assert(bbsCount > 0);
        return bbsDstTab[bbsCount - 1];
    }
};

// BBSwitchTargetList out-of-class-declaration implementations (here due to C++ ordering requirements).
//

inline BBSwitchTargetList::BBSwitchTargetList(BBswtDesc* bbsDesc) : m_bbsDesc(bbsDesc)
{
    assert(m_bbsDesc != nullptr);
    assert(m_bbsDesc->bbsDstTab != nullptr);
}

inline BBArrayIterator BBSwitchTargetList::begin() const
{
    return BBArrayIterator(m_bbsDesc->bbsDstTab);
}

inline BBArrayIterator BBSwitchTargetList::end() const
{
    return BBArrayIterator(m_bbsDesc->bbsDstTab + m_bbsDesc->bbsCount);
}

// BBSuccList out-of-class-declaration implementations
//
inline BasicBlock::BBSuccList::BBSuccList(const BasicBlock* block)
{
    assert(block != nullptr);
    switch (block->bbJumpKind)
    {
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_EHFINALLYRET:
        case BBJ_EHFILTERRET:
            // We don't need m_succs.
            m_begin = nullptr;
            m_end   = nullptr;
            break;

        case BBJ_CALLFINALLY:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_LEAVE:
            m_succs[0] = block->bbJumpDest;
            m_begin    = &m_succs[0];
            m_end      = &m_succs[1];
            break;

        case BBJ_NONE:
            m_succs[0] = block->bbNext;
            m_begin    = &m_succs[0];
            m_end      = &m_succs[1];
            break;

        case BBJ_COND:
            m_succs[0] = block->bbNext;
            m_begin    = &m_succs[0];

            // If both fall-through and branch successors are identical, then only include
            // them once in the iteration (this is the same behavior as NumSucc()/GetSucc()).
            if (block->bbJumpDest == block->bbNext)
            {
                m_end = &m_succs[1];
            }
            else
            {
                m_succs[1] = block->bbJumpDest;
                m_end      = &m_succs[2];
            }
            break;

        case BBJ_SWITCH:
            // We don't use the m_succs in-line data for switches; use the existing jump table in the block.
            assert(block->bbJumpSwt != nullptr);
            assert(block->bbJumpSwt->bbsDstTab != nullptr);
            m_begin = block->bbJumpSwt->bbsDstTab;
            m_end   = block->bbJumpSwt->bbsDstTab + block->bbJumpSwt->bbsCount;
            break;

        default:
            unreached();
    }

    assert(m_end >= m_begin);
}

inline BBArrayIterator BasicBlock::BBSuccList::begin() const
{
    return BBArrayIterator(m_begin);
}

inline BBArrayIterator BasicBlock::BBSuccList::end() const
{
    return BBArrayIterator(m_end);
}

// In compiler terminology the control flow between two BasicBlocks
// is typically referred to as an "edge".  Most well known are the
// backward branches for loops, which are often called "back-edges".
//
// "struct flowList" is the type that represents our control flow edges.
// This type is a linked list of zero or more "edges".
// (The list of zero edges is represented by NULL.)
// Every BasicBlock has a field called bbPreds of this type.  This field
// represents the list of "edges" that flow into this BasicBlock.
// The flowList type only stores the BasicBlock* of the source for the
// control flow edge.  The destination block for the control flow edge
// is implied to be the block which contained the bbPreds field.
//
// For a switch branch target there may be multiple "edges" that have
// the same source block (and destination block).  We need to count the
// number of these edges so that during optimization we will know when
// we have zero of them.  Rather than have extra flowList entries we
// increment the flDupCount field.
//
// When we have Profile weight for the BasicBlocks we can usually compute
// the number of times each edge was executed by examining the adjacent
// BasicBlock weights.  As we are doing for BasicBlocks, we call the number
// of times that a control flow edge was executed the "edge weight".
// In order to compute the edge weights we need to use a bounded range
// for every edge weight. These two fields, 'flEdgeWeightMin' and 'flEdgeWeightMax'
// are used to hold a bounded range.  Most often these will converge such
// that both values are the same and that value is the exact edge weight.
// Sometimes we are left with a rage of possible values between [Min..Max]
// which represents an inexact edge weight.
//
// The bbPreds list is initially created by Compiler::fgComputePreds()
// and is incrementally kept up to date.
//
// The edge weight are computed by Compiler::fgComputeEdgeWeights()
// the edge weights are used to straighten conditional branches
// by Compiler::fgReorderBlocks()
//
// We have a simpler struct, BasicBlockList, which is simply a singly-linked
// list of blocks. This is used for various purposes, but one is as a "cheap"
// predecessor list, computed by fgComputeCheapPreds(), and stored as a list
// on BasicBlock pointed to by bbCheapPreds.

struct BasicBlockList
{
    BasicBlockList* next;  // The next BasicBlock in the list, nullptr for end of list.
    BasicBlock*     block; // The BasicBlock of interest.

    BasicBlockList() : next(nullptr), block(nullptr)
    {
    }

    BasicBlockList(BasicBlock* blk, BasicBlockList* rest) : next(rest), block(blk)
    {
    }
};

// flowList -- control flow edge
//
struct flowList
{
public:
    flowList* flNext; // The next BasicBlock in the list, nullptr for end of list.

private:
    BasicBlock*          m_block; // The BasicBlock of interest.
    BasicBlock::weight_t flEdgeWeightMin;
    BasicBlock::weight_t flEdgeWeightMax;

public:
    unsigned flDupCount; // The count of duplicate "edges" (use only for switch stmts)

public:
    BasicBlock* getBlock() const
    {
        return m_block;
    }

    void setBlock(BasicBlock* newBlock)
    {
        m_block = newBlock;
    }

    BasicBlock::weight_t edgeWeightMin() const
    {
        return flEdgeWeightMin;
    }

    BasicBlock::weight_t edgeWeightMax() const
    {
        return flEdgeWeightMax;
    }

    // These two methods are used to set new values for flEdgeWeightMin and flEdgeWeightMax
    // they are used only during the computation of the edge weights
    // They return false if the newWeight is not between the current [min..max]
    // when slop is non-zero we allow for the case where our weights might be off by 'slop'
    //
    bool setEdgeWeightMinChecked(BasicBlock::weight_t newWeight,
                                 BasicBlock*          bDst,
                                 BasicBlock::weight_t slop,
                                 bool*                wbUsedSlop);
    bool setEdgeWeightMaxChecked(BasicBlock::weight_t newWeight,
                                 BasicBlock*          bDst,
                                 BasicBlock::weight_t slop,
                                 bool*                wbUsedSlop);
    void setEdgeWeights(BasicBlock::weight_t newMinWeight, BasicBlock::weight_t newMaxWeight, BasicBlock* bDst);

    flowList(BasicBlock* block, flowList* rest)
        : flNext(rest), m_block(block), flEdgeWeightMin(0), flEdgeWeightMax(0), flDupCount(0)
    {
    }
};

// Pred list iterator implementations (that are required to be defined after the declaration of BasicBlock and flowList)

inline PredEdgeList::iterator::iterator(flowList* pred) : m_pred(pred)
{
#ifdef DEBUG
    m_next = (m_pred == nullptr) ? nullptr : m_pred->flNext;
#endif
}

inline PredEdgeList::iterator& PredEdgeList::iterator::operator++()
{
    flowList* next = m_pred->flNext;

#ifdef DEBUG
    // Check that the next block is the one we expect to see.
    assert(next == m_next);
    m_next = (next == nullptr) ? nullptr : next->flNext;
#endif // DEBUG

    m_pred = next;
    return *this;
}

inline PredBlockList::iterator::iterator(flowList* pred) : m_pred(pred)
{
#ifdef DEBUG
    m_next = (m_pred == nullptr) ? nullptr : m_pred->flNext;
#endif
}

inline BasicBlock* PredBlockList::iterator::operator*() const
{
    return m_pred->getBlock();
}

inline PredBlockList::iterator& PredBlockList::iterator::operator++()
{
    flowList* next = m_pred->flNext;

#ifdef DEBUG
    // Check that the next block is the one we expect to see.
    assert(next == m_next);
    m_next = (next == nullptr) ? nullptr : next->flNext;
#endif // DEBUG

    m_pred = next;
    return *this;
}

// This enum represents a pre/post-visit action state to emulate a depth-first
// spanning tree traversal of a tree or graph.
enum DfsStackState
{
    DSS_Invalid, // The initialized, invalid error state
    DSS_Pre,     // The DFS pre-order (first visit) traversal state
    DSS_Post     // The DFS post-order (last visit) traversal state
};

// These structs represents an entry in a stack used to emulate a non-recursive
// depth-first spanning tree traversal of a graph. The entry contains either a
// block pointer or a block number depending on which is more useful.
struct DfsBlockEntry
{
    DfsStackState dfsStackState; // The pre/post traversal action for this entry
    BasicBlock*   dfsBlock;      // The corresponding block for the action

    DfsBlockEntry(DfsStackState state, BasicBlock* basicBlock) : dfsStackState(state), dfsBlock(basicBlock)
    {
    }
};

/*****************************************************************************
 *
 *  The following call-backs supplied by the client; it's used by the code
 *  emitter to convert a basic block to its corresponding emitter cookie.
 */

void* emitCodeGetCookie(BasicBlock* block);

AllSuccessorIterPosition::AllSuccessorIterPosition(Compiler* comp, BasicBlock* block)
    : m_numNormSuccs(block->NumSucc(comp)), m_remainingNormSucc(m_numNormSuccs), m_ehIter(comp, block)
{
    if (CurTryIsBlkCallFinallyTarget(comp, block))
    {
        m_ehIter.Advance(comp, block);
    }
}

bool AllSuccessorIterPosition::CurTryIsBlkCallFinallyTarget(Compiler* comp, BasicBlock* block)
{
    return (block->bbJumpKind == BBJ_CALLFINALLY) && (m_ehIter != EHSuccessorIterPosition()) &&
           (block->bbJumpDest == m_ehIter.Current(comp, block));
}

void AllSuccessorIterPosition::Advance(Compiler* comp, BasicBlock* block)
{
    if (m_remainingNormSucc > 0)
    {
        m_remainingNormSucc--;
    }
    else
    {
        m_ehIter.Advance(comp, block);

        // If the original block whose successors we're iterating over
        // is a BBJ_CALLFINALLY, that finally clause's first block
        // will be yielded as a normal successor.  Don't also yield as
        // an exceptional successor.
        if (CurTryIsBlkCallFinallyTarget(comp, block))
        {
            m_ehIter.Advance(comp, block);
        }
    }
}

// Requires that "this" is not equal to the standard "end" iterator.  Returns the
// current successor.
BasicBlock* AllSuccessorIterPosition::Current(Compiler* comp, BasicBlock* block)
{
    if (m_remainingNormSucc > 0)
    {
        return block->GetSucc(m_numNormSuccs - m_remainingNormSucc, comp);
    }
    else
    {
        return m_ehIter.Current(comp, block);
    }
}

typedef BasicBlock::Successors<EHSuccessorIterPosition>::iterator  EHSuccessorIter;
typedef BasicBlock::Successors<AllSuccessorIterPosition>::iterator AllSuccessorIter;

// An enumerator of a block's all successors. In some cases (e.g. SsaBuilder::TopologicalSort)
// using iterators is not exactly efficient, at least because they contain an unnecessary
// member - a pointer to the Compiler object.
class AllSuccessorEnumerator
{
    BasicBlock*              m_block;
    AllSuccessorIterPosition m_pos;

public:
    // Constructs an enumerator of all `block`'s successors.
    AllSuccessorEnumerator(Compiler* comp, BasicBlock* block) : m_block(block), m_pos(comp, block)
    {
    }

    // Gets the block whose successors are enumerated.
    BasicBlock* Block()
    {
        return m_block;
    }

    // Returns true if the next successor is an EH successor.
    bool IsNextEHSuccessor()
    {
        return m_pos.IsCurrentEH();
    }

    // Returns the next available successor or `nullptr` if there are no more successors.
    BasicBlock* NextSuccessor(Compiler* comp)
    {
        if (!m_pos.HasCurrent())
        {
            return nullptr;
        }

        BasicBlock* succ = m_pos.Current(comp, m_block);
        m_pos.Advance(comp, m_block);
        return succ;
    }
};

// Simple dominator tree node that keeps track of a node's first child and next sibling.
// The parent is provided by BasicBlock::bbIDom.
struct DomTreeNode
{
    BasicBlock* firstChild;
    BasicBlock* nextSibling;
};

/*****************************************************************************/
#endif // _BLOCK_H_
/*****************************************************************************/
