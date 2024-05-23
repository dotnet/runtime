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
typedef BitVec          EXPSET_TP;
typedef BitVec_ValArg_T EXPSET_VALARG_TP;
typedef BitVec_ValRet_T EXPSET_VALRET_TP;

#define EXPSET_SZ 64

typedef BitVec          ASSERT_TP;
typedef BitVec_ValArg_T ASSERT_VALARG_TP;
typedef BitVec_ValRet_T ASSERT_VALRET_TP;

// We use the following format when printing the BasicBlock number: bbNum
// This define is used with string concatenation to put this in printf format strings  (Note that %u means unsigned int)
#define FMT_BB "BB%02u"

// Use this format for loop indices
#define FMT_LP "L%02u"

// Use this format for profile weights
#define FMT_WT "%.7g"

// Use this format for profile weights where we want to conserve horizontal space, at the expense of displaying
// less precision.
#define FMT_WT_NARROW "%.3g"

/*****************************************************************************
 *
 *  Each basic block ends with a jump which is described as a value
 *  of the following enumeration.
 */

// clang-format off

enum BBKinds : BYTE
{
    BBJ_EHFINALLYRET,// block ends with 'endfinally' (for finally)
    BBJ_EHFAULTRET,  // block ends with 'endfinally' (IL alias for 'endfault') (for fault)
    BBJ_EHFILTERRET, // block ends with 'endfilter'
    BBJ_EHCATCHRET,  // block ends with a leave out of a catch
    BBJ_THROW,       // block ends with 'throw'
    BBJ_RETURN,      // block ends with 'ret'
    BBJ_ALWAYS,      // block always jumps to the target
    BBJ_LEAVE,       // block always jumps to the target, maybe out of guarded region. Only used until importing.
    BBJ_CALLFINALLY, // block always calls the target finally
    BBJ_CALLFINALLYRET, // block targets the return from finally, aka "finally continuation". Always paired with BBJ_CALLFINALLY.
    BBJ_COND,        // block conditionally jumps to the target
    BBJ_SWITCH,      // block ends with a switch statement

    BBJ_COUNT
};

#ifdef DEBUG
const char* const bbKindNames[] = {
    "BBJ_EHFINALLYRET",
    "BBJ_EHFAULTRET",
    "BBJ_EHFILTERRET",
    "BBJ_EHCATCHRET",
    "BBJ_THROW",
    "BBJ_RETURN",
    "BBJ_ALWAYS",
    "BBJ_LEAVE",
    "BBJ_CALLFINALLY",
    "BBJ_CALLFINALLYRET",
    "BBJ_COND",
    "BBJ_SWITCH",
    "BBJ_COUNT"
};
#endif // DEBUG

// clang-format on

struct GenTree;
struct Statement;
struct BasicBlock;
class Compiler;
class typeInfo;
struct BasicBlockList;
struct FlowEdge;
struct EHblkDsc;
struct BBswtDesc;
struct BBehfDesc;

struct StackEntry
{
    GenTree* val;
    typeInfo seTypeInfo;
};

struct EntryState
{
    unsigned    esStackDepth; // size of esStack
    StackEntry* esStack;      // ptr to  stack
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
    explicit inline MemoryKindIterator(int val)
        : value(val)
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

// PredEdgeList: adapter class for forward iteration of the predecessor edge linked list using range-based `for`,
// normally used via BasicBlock::PredEdges(), e.g.:
//    for (FlowEdge* const edge : block->PredEdges()) ...
//
class PredEdgeList
{
    FlowEdge* m_begin;

    // Forward iterator for the predecessor edges linked list.
    // The caller can't make changes to the preds list when using this.
    //
    class iterator
    {
        FlowEdge* m_pred;

#ifdef DEBUG
        // Try to guard against the user of the iterator from making changes to the IR that would invalidate
        // the iterator: cache the edge we think should be next, then check it when we actually do the `++`
        // operation. This is a bit conservative, but attempts to protect against callers assuming too much about
        // this iterator implementation.
        FlowEdge* m_next;
#endif

    public:
        iterator(FlowEdge* pred);

        FlowEdge* operator*() const
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
    PredEdgeList(FlowEdge* pred)
        : m_begin(pred)
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
// allowEdits controls whether the iterator should be resilient to changes to the predecessor list.
//
template <bool allowEdits>
class PredBlockList
{
    FlowEdge* m_begin;

    // Forward iterator for the predecessor edges linked list, yielding the predecessor block, not the edge.
    // The caller can't make changes to the preds list when using this.
    //
    class iterator
    {
        FlowEdge* m_pred;

        // When allowEdits=false, try to guard against the user of the iterator from modifying the predecessor list
        // being traversed: cache the edge we think should be next, then check it when we actually do the `++`
        // operation. This is a bit conservative, but attempts to protect against callers assuming too much about
        // this iterator implementation.
        // When allowEdits=true, m_next is always used to update m_pred, so changes to m_pred don't break the iterator.
        FlowEdge* m_next;

    public:
        iterator(FlowEdge* pred);

        BasicBlock* operator*() const;

        iterator& operator++();

        bool operator!=(const iterator& i) const
        {
            return m_pred != i.m_pred;
        }
    };

public:
    PredBlockList(FlowEdge* pred)
        : m_begin(pred)
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

// BBArrayIterator: forward iterator for an array of BasicBlock*.
// It is an error (with assert) to yield a nullptr BasicBlock* in this array.
// `m_edgeEntry` can be nullptr, but it only makes sense if both the begin and end of an iteration range are nullptr
// (meaning, no actual iteration will happen).
//
class BBArrayIterator
{
    FlowEdge* const* m_edgeEntry;

public:
    BBArrayIterator(FlowEdge* const* edgeEntry)
        : m_edgeEntry(edgeEntry)
    {
    }

    BasicBlock* operator*() const;

    BBArrayIterator& operator++()
    {
        assert(m_edgeEntry != nullptr);
        ++m_edgeEntry;
        return *this;
    }

    bool operator!=(const BBArrayIterator& i) const
    {
        return m_edgeEntry != i.m_edgeEntry;
    }
};

// FlowEdgeArrayIterator: forward iterator for an array of FlowEdge*, such as the BBswtDesc->bbsDstTab.
// It is an error (with assert) to yield a nullptr FlowEdge* in this array.
// `m_edgeEntry` can be nullptr, but it only makes sense if both the begin and end of an iteration range are nullptr
// (meaning, no actual iteration will happen).
//
class FlowEdgeArrayIterator
{
    FlowEdge* const* m_edgeEntry;

public:
    FlowEdgeArrayIterator(FlowEdge* const* edgeEntry)
        : m_edgeEntry(edgeEntry)
    {
    }

    FlowEdge* operator*() const
    {
        assert(m_edgeEntry != nullptr);
        FlowEdge* const edge = *m_edgeEntry;
        assert(edge != nullptr);
        return edge;
    }

    FlowEdgeArrayIterator& operator++()
    {
        assert(m_edgeEntry != nullptr);
        ++m_edgeEntry;
        return *this;
    }

    bool operator!=(const FlowEdgeArrayIterator& i) const
    {
        return m_edgeEntry != i.m_edgeEntry;
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

// BBEhfSuccList: adapter class for forward iteration of BBJ_EHFINALLYRET blocks, using range-based `for`,
// normally used via BasicBlock::EHFinallyRetSuccs(), e.g.:
//    for (BasicBlock* const succ : block->EHFinallyRetSuccs()) ...
//
class BBEhfSuccList
{
    BBehfDesc* m_bbeDesc;

public:
    BBEhfSuccList(BBehfDesc* bbeDesc);
    BBArrayIterator begin() const;
    BBArrayIterator end() const;
};

//------------------------------------------------------------------------
// BasicBlockFlags: a bitmask of flags for BasicBlock
//
// clang-format off
enum BasicBlockFlags : uint64_t
{
#define MAKE_BBFLAG(bit) (1ULL << (bit))
    BBF_EMPTY                = 0,

    BBF_IS_LIR               = MAKE_BBFLAG( 0), // Set if the basic block contains LIR (as opposed to HIR)
    BBF_MARKED               = MAKE_BBFLAG( 1), // BB marked  during optimizations
    BBF_REMOVED              = MAKE_BBFLAG( 2), // BB has been removed from bb-list
    BBF_DONT_REMOVE          = MAKE_BBFLAG( 3), // BB should not be removed during flow graph optimizations
    BBF_IMPORTED             = MAKE_BBFLAG( 4), // BB byte-code has been imported
    BBF_INTERNAL             = MAKE_BBFLAG( 5), // BB has been added by the compiler
    BBF_FAILED_VERIFICATION  = MAKE_BBFLAG( 6), // BB has verification exception
    BBF_NEEDS_GCPOLL         = MAKE_BBFLAG( 7), // BB may need a GC poll because it uses the slow tail call helper
    BBF_FUNCLET_BEG          = MAKE_BBFLAG( 8), // BB is the beginning of a funclet
    BBF_CLONED_FINALLY_BEGIN = MAKE_BBFLAG( 9), // First block of a cloned finally region
    BBF_CLONED_FINALLY_END   = MAKE_BBFLAG(10), // Last block of a cloned finally region
    BBF_HAS_NULLCHECK        = MAKE_BBFLAG(11), // BB contains a null check
    BBF_HAS_SUPPRESSGC_CALL  = MAKE_BBFLAG(12), // BB contains a call to a method with SuppressGCTransitionAttribute
    BBF_RUN_RARELY           = MAKE_BBFLAG(13), // BB is rarely run (catch clauses, blocks with throws etc)
    BBF_LOOP_HEAD            = MAKE_BBFLAG(14), // BB is the head of a loop (can reach a predecessor)
    BBF_HAS_LABEL            = MAKE_BBFLAG(15), // BB needs a label
    BBF_LOOP_ALIGN           = MAKE_BBFLAG(16), // Block is lexically the first block in a loop we intend to align.
    BBF_HAS_ALIGN            = MAKE_BBFLAG(17), // BB ends with 'align' instruction
    BBF_HAS_JMP              = MAKE_BBFLAG(18), // BB executes a JMP instruction (instead of return)
    BBF_GC_SAFE_POINT        = MAKE_BBFLAG(19), // BB has a GC safe point (e.g. a call)
    BBF_HAS_IDX_LEN          = MAKE_BBFLAG(20), // BB contains simple index or length expressions on an SD array local var.
    BBF_HAS_MD_IDX_LEN       = MAKE_BBFLAG(21), // BB contains simple index, length, or lower bound expressions on an MD array local var.
    BBF_HAS_MDARRAYREF       = MAKE_BBFLAG(22), // Block has a multi-dimensional array reference
    BBF_HAS_NEWOBJ           = MAKE_BBFLAG(23), // BB contains 'new' of an object type.

    BBF_RETLESS_CALL                   = MAKE_BBFLAG(24), // BBJ_CALLFINALLY that will never return (and therefore, won't need a paired
                                                          // BBJ_CALLFINALLYRET); see isBBCallFinallyPair().
    BBF_COLD                           = MAKE_BBFLAG(25), // BB is cold
    BBF_PROF_WEIGHT                    = MAKE_BBFLAG(26), // BB weight is computed from profile data
    BBF_KEEP_BBJ_ALWAYS                = MAKE_BBFLAG(27), // A special BBJ_ALWAYS block, used by EH code generation. Keep the jump kind
                                                          // as BBJ_ALWAYS. Used on x86 for the final step block out of a finally.
    BBF_HAS_CALL                       = MAKE_BBFLAG(28), // BB contains a call
    BBF_DOMINATED_BY_EXCEPTIONAL_ENTRY = MAKE_BBFLAG(29), // Block is dominated by exceptional entry.
    BBF_BACKWARD_JUMP                  = MAKE_BBFLAG(30), // BB is surrounded by a backward jump/switch arc
    BBF_BACKWARD_JUMP_SOURCE           = MAKE_BBFLAG(31), // Block is a source of a backward jump
    BBF_BACKWARD_JUMP_TARGET           = MAKE_BBFLAG(32), // Block is a target of a backward jump
    BBF_PATCHPOINT                     = MAKE_BBFLAG(33), // Block is a patchpoint
    BBF_PARTIAL_COMPILATION_PATCHPOINT = MAKE_BBFLAG(34), // Block is a partial compilation patchpoint
    BBF_HAS_HISTOGRAM_PROFILE          = MAKE_BBFLAG(35), // BB contains a call needing a histogram profile
    BBF_TAILCALL_SUCCESSOR             = MAKE_BBFLAG(36), // BB has pred that has potential tail call
    BBF_RECURSIVE_TAILCALL             = MAKE_BBFLAG(37), // Block has recursive tailcall that may turn into a loop
    BBF_NO_CSE_IN                      = MAKE_BBFLAG(38), // Block should kill off any incoming CSE
    BBF_CAN_ADD_PRED                   = MAKE_BBFLAG(39), // Ok to add pred edge to this block, even when "safe" edge creation disabled
    BBF_HAS_VALUE_PROFILE              = MAKE_BBFLAG(40), // Block has a node that needs a value probing

    // The following are sets of flags.

    // Flags to update when two blocks are compacted

    BBF_COMPACT_UPD = BBF_GC_SAFE_POINT | BBF_NEEDS_GCPOLL | BBF_HAS_JMP | BBF_HAS_IDX_LEN | BBF_HAS_MD_IDX_LEN | BBF_BACKWARD_JUMP | \
                      BBF_HAS_NEWOBJ | BBF_HAS_NULLCHECK | BBF_HAS_MDARRAYREF,

    // Flags a block should not have had before it is split.

    BBF_SPLIT_NONEXIST = BBF_LOOP_HEAD | BBF_RETLESS_CALL | BBF_COLD,

    // Flags lost by the top block when a block is split.
    // Note, this is a conservative guess.
    // For example, the top block might or might not have BBF_GC_SAFE_POINT,
    // but we assume it does not have BBF_GC_SAFE_POINT any more.

    BBF_SPLIT_LOST = BBF_GC_SAFE_POINT | BBF_NEEDS_GCPOLL | BBF_HAS_JMP | BBF_KEEP_BBJ_ALWAYS | BBF_CLONED_FINALLY_END | BBF_RECURSIVE_TAILCALL,

    // Flags gained by the bottom block when a block is split.
    // Note, this is a conservative guess.
    // For example, the bottom block might or might not have BBF_HAS_NULLCHECK, but we assume it has BBF_HAS_NULLCHECK.
    // TODO: Should BBF_RUN_RARELY be added to BBF_SPLIT_GAINED ?

    BBF_SPLIT_GAINED = BBF_DONT_REMOVE | BBF_HAS_JMP | BBF_BACKWARD_JUMP | BBF_HAS_IDX_LEN | BBF_HAS_MD_IDX_LEN | BBF_PROF_WEIGHT | \
                       BBF_HAS_NEWOBJ | BBF_KEEP_BBJ_ALWAYS | BBF_CLONED_FINALLY_END | BBF_HAS_NULLCHECK | BBF_HAS_HISTOGRAM_PROFILE | BBF_HAS_VALUE_PROFILE | BBF_HAS_MDARRAYREF | BBF_NEEDS_GCPOLL,

    // Flags that must be propagated to a new block if code is copied from a block to a new block. These are flags that
    // limit processing of a block if the code in question doesn't exist. This is conservative; we might not
    // have actually copied one of these type of tree nodes, but if we only copy a portion of the block's statements,
    // we don't know (unless we actually pay close attention during the copy).

    BBF_COPY_PROPAGATE = BBF_HAS_NEWOBJ | BBF_HAS_NULLCHECK | BBF_HAS_IDX_LEN | BBF_HAS_MD_IDX_LEN | BBF_HAS_MDARRAYREF,
};

FORCEINLINE
constexpr BasicBlockFlags operator ~(BasicBlockFlags a)
{
    return (BasicBlockFlags)(~(uint64_t)a);
}

FORCEINLINE
constexpr BasicBlockFlags operator |(BasicBlockFlags a, BasicBlockFlags b)
{
    return (BasicBlockFlags)((uint64_t)a | (uint64_t)b);
}

FORCEINLINE
constexpr BasicBlockFlags operator &(BasicBlockFlags a, BasicBlockFlags b)
{
    return (BasicBlockFlags)((uint64_t)a & (uint64_t)b);
}

FORCEINLINE 
BasicBlockFlags& operator |=(BasicBlockFlags& a, BasicBlockFlags b)
{
    return a = (BasicBlockFlags)((uint64_t)a | (uint64_t)b);
}

FORCEINLINE 
BasicBlockFlags& operator &=(BasicBlockFlags& a, BasicBlockFlags b)
{
    return a = (BasicBlockFlags)((uint64_t)a & (uint64_t)b);
}

enum class BasicBlockVisit
{
    Continue,
    Abort,
};

// clang-format on

//-------------------------------------------------------------------------
// FlowEdge -- control flow edge
//
// In compiler terminology the control flow between two BasicBlocks
// is typically referred to as an "edge".  Most well known are the
// backward branches for loops, which are often called "back-edges".
//
// "struct FlowEdge" is the type that represents our control flow edges.
// This type is a linked list of zero or more "edges".
// (The list of zero edges is represented by NULL.)
// Every BasicBlock has a field called bbPreds of this type.  This field
// represents the list of "edges" that flow into this BasicBlock.
// The FlowEdge type only stores the BasicBlock* of the source for the
// control flow edge.  The destination block for the control flow edge
// is implied to be the block which contained the bbPreds field.
//
// For a switch branch target there may be multiple "edges" that have
// the same source block (and destination block).  We need to count the
// number of these edges so that during optimization we will know when
// we have zero of them.  Rather than have extra FlowEdge entries we
// track this via the DupCount property.
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
// The bbPreds list is initially created by Compiler::fgLinkBasicBlocks()
// and is incrementally kept up to date.
//
// The edge weight are computed by Compiler::fgComputeEdgeWeights()
// the edge weights are used to straighten conditional branches
// by Compiler::fgReorderBlocks()
//
struct FlowEdge
{
private:
    // The next predecessor edge in the list, nullptr for end of list.
    FlowEdge* m_nextPredEdge;

    // The source of the control flow
    BasicBlock* m_sourceBlock;

    // The destination of the control flow
    BasicBlock* m_destBlock;

    // Likelihood that m_sourceBlock transfers control along this edge.
    // Values in range [0..1]
    weight_t m_likelihood;

    // The count of duplicate "edges" (used for switch stmts or degenerate branches)
    unsigned m_dupCount;

    // True if likelihood has been set
    INDEBUG(bool m_likelihoodSet);

public:
    FlowEdge(BasicBlock* sourceBlock, BasicBlock* destBlock, FlowEdge* rest)
        : m_nextPredEdge(rest)
        , m_sourceBlock(sourceBlock)
        , m_destBlock(destBlock)
        , m_likelihood(0)
        , m_dupCount(0)
#ifdef DEBUG
        , m_likelihoodSet(false)
#endif // DEBUG
    {
    }

    FlowEdge* getNextPredEdge() const
    {
        return m_nextPredEdge;
    }

    FlowEdge** getNextPredEdgeRef()
    {
        return &m_nextPredEdge;
    }

    void setNextPredEdge(FlowEdge* newEdge)
    {
        m_nextPredEdge = newEdge;
    }

    BasicBlock* getSourceBlock() const
    {
        assert(m_sourceBlock != nullptr);
        return m_sourceBlock;
    }

    void setSourceBlock(BasicBlock* newBlock)
    {
        assert(newBlock != nullptr);
        m_sourceBlock = newBlock;
    }

    BasicBlock* getDestinationBlock() const
    {
        assert(m_destBlock != nullptr);
        return m_destBlock;
    }

    void setDestinationBlock(BasicBlock* newBlock)
    {
        assert(newBlock != nullptr);
        m_destBlock = newBlock;
    }

    weight_t getLikelihood() const
    {
        assert(m_likelihoodSet);
        return m_likelihood;
    }

    void setLikelihood(weight_t likelihood);
    void addLikelihood(weight_t addedLikelihod);

    void clearLikelihood()
    {
        m_likelihood = 0.0;
        INDEBUG(m_likelihoodSet = false);
    }

#ifdef DEBUG
    bool hasLikelihood() const
    {
        return m_likelihoodSet;
    }
#endif // DEBUG

    weight_t getLikelyWeight() const;

    unsigned getDupCount() const
    {
        return m_dupCount;
    }

    void incrementDupCount()
    {
        m_dupCount++;
    }

    void decrementDupCount()
    {
        assert(m_dupCount >= 1);
        m_dupCount--;
    }
};

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

private:
    BasicBlock* bbNext; // next BB in ascending PC offset order
    BasicBlock* bbPrev;

    BBKinds bbKind; // jump (if any) at the end of this block

    /* The following union describes the jump target(s) of this block */
    union
    {
        unsigned   bbTargetOffs; // PC offset (temporary only)
        FlowEdge*  bbTargetEdge; // successor edge for block kinds with only one successor (BBJ_ALWAYS, etc)
        FlowEdge*  bbTrueEdge;   // BBJ_COND successor edge when its condition is true (alias for bbTargetEdge)
        BBswtDesc* bbSwtTargets; // switch descriptor
        BBehfDesc* bbEhfTargets; // BBJ_EHFINALLYRET descriptor
    };

    // Successor edge of a BBJ_COND block if bbTrueEdge is not taken
    FlowEdge* bbFalseEdge;

public:
    static BasicBlock* New(Compiler* compiler);
    static BasicBlock* New(Compiler* compiler, BBKinds kind);
    static BasicBlock* New(Compiler* compiler, BBehfDesc* ehfTargets);
    static BasicBlock* New(Compiler* compiler, BBswtDesc* swtTargets);
    static BasicBlock* New(Compiler* compiler, BBKinds kind, unsigned targetOffs);

    BBKinds GetKind() const
    {
        return bbKind;
    }

    void SetKind(BBKinds kind)
    {
        // If this block's jump kind requires a target, ensure it is already set
        assert(!HasTarget() || HasInitializedTarget());
        bbKind = kind;
        // If new jump kind requires a target, ensure a target is already set
        assert(!HasTarget() || HasInitializedTarget());
    }

    BasicBlock* Prev() const
    {
        return bbPrev;
    }

    void SetPrev(BasicBlock* prev)
    {
        assert(prev != nullptr);
        bbPrev       = prev;
        prev->bbNext = this;
    }

    void SetPrevToNull()
    {
        bbPrev = nullptr;
    }

    BasicBlock* Next() const
    {
        return bbNext;
    }

    void SetNext(BasicBlock* next)
    {
        assert(next != nullptr);
        bbNext       = next;
        next->bbPrev = this;
    }

    void SetNextToNull()
    {
        bbNext = nullptr;
    }

    bool IsFirst() const
    {
        return (bbPrev == nullptr);
    }

    bool IsLast() const
    {
        return (bbNext == nullptr);
    }

    bool PrevIs(const BasicBlock* block) const
    {
        return (bbPrev == block);
    }

    bool NextIs(const BasicBlock* block) const
    {
        return (bbNext == block);
    }

    bool IsLastHotBlock(Compiler* compiler) const;

    bool IsFirstColdBlock(Compiler* compiler) const;

    bool CanRemoveJumpToNext(Compiler* compiler) const;

    bool CanRemoveJumpToTarget(BasicBlock* target, Compiler* compiler) const;

    unsigned GetTargetOffs() const
    {
        return bbTargetOffs;
    }

    bool HasTarget() const
    {
        // These block types should always have bbTargetEdge set
        return KindIs(BBJ_ALWAYS, BBJ_CALLFINALLY, BBJ_CALLFINALLYRET, BBJ_EHCATCHRET, BBJ_EHFILTERRET, BBJ_LEAVE);
    }

    BasicBlock* GetTarget() const
    {
        return GetTargetEdge()->getDestinationBlock();
    }

    FlowEdge* GetTargetEdge() const
    {
        // Only block kinds that use `bbTargetEdge` can access it, and it must be non-null.
        assert(HasInitializedTarget());
        assert(bbTargetEdge->getSourceBlock() == this);
        assert(bbTargetEdge->getDestinationBlock() != nullptr);
        return bbTargetEdge;
    }

    void SetTargetEdge(FlowEdge* targetEdge)
    {
        // SetKindAndTarget() nulls target for non-jump kinds,
        // so don't use SetTargetEdge() to null bbTargetEdge without updating bbKind.
        bbTargetEdge = targetEdge;
        assert(HasInitializedTarget());
        assert(bbTargetEdge->getSourceBlock() == this);
        assert(bbTargetEdge->getDestinationBlock() != nullptr);

        // This is the only successor edge for this block, so likelihood should be 1.0
        bbTargetEdge->setLikelihood(1.0);
    }

    BasicBlock* GetTrueTarget() const
    {
        return GetTrueEdge()->getDestinationBlock();
    }

    FlowEdge* GetTrueEdge() const
    {
        assert(KindIs(BBJ_COND));
        assert(bbTrueEdge != nullptr);
        assert(bbTrueEdge->getSourceBlock() == this);
        assert(bbTrueEdge->getDestinationBlock() != nullptr);
        return bbTrueEdge;
    }

    void SetTrueEdge(FlowEdge* trueEdge)
    {
        assert(KindIs(BBJ_COND));
        bbTrueEdge = trueEdge;
        assert(bbTrueEdge != nullptr);
        assert(bbTrueEdge->getSourceBlock() == this);
        assert(bbTrueEdge->getDestinationBlock() != nullptr);
    }

    bool TrueTargetIs(const BasicBlock* target) const
    {
        return (GetTrueTarget() == target);
    }

    bool TrueEdgeIs(const FlowEdge* targetEdge) const
    {
        return (GetTrueEdge() == targetEdge);
    }

    BasicBlock* GetFalseTarget() const
    {
        return GetFalseEdge()->getDestinationBlock();
    }

    FlowEdge* GetFalseEdge() const
    {
        assert(KindIs(BBJ_COND));
        assert(bbFalseEdge != nullptr);
        assert(bbFalseEdge->getSourceBlock() == this);
        assert(bbFalseEdge->getDestinationBlock() != nullptr);
        return bbFalseEdge;
    }

    void SetFalseEdge(FlowEdge* falseEdge)
    {
        assert(KindIs(BBJ_COND));
        bbFalseEdge = falseEdge;
        assert(bbFalseEdge != nullptr);
        assert(bbFalseEdge->getSourceBlock() == this);
        assert(bbFalseEdge->getDestinationBlock() != nullptr);
    }

    bool FalseTargetIs(const BasicBlock* target) const
    {
        return (GetFalseTarget() == target);
    }

    bool FalseEdgeIs(const FlowEdge* targetEdge) const
    {
        return (GetFalseEdge() == targetEdge);
    }

    void SetCond(FlowEdge* trueEdge, FlowEdge* falseEdge)
    {
        bbKind = BBJ_COND;
        SetTrueEdge(trueEdge);
        SetFalseEdge(falseEdge);
    }

    // In most cases, a block's true and false targets are known by the time SetCond is called.
    // To simplify the few cases where the false target isn't available until later,
    // overload SetCond to initialize only the true target.
    // This simplifies, for example, lowering switch blocks into jump sequences.
    void SetCond(FlowEdge* trueEdge)
    {
        bbKind = BBJ_COND;
        SetTrueEdge(trueEdge);
    }

    // Set both the block kind and target edge.
    void SetKindAndTargetEdge(BBKinds kind, FlowEdge* targetEdge)
    {
        bbKind       = kind;
        bbTargetEdge = targetEdge;
        assert(HasInitializedTarget());

        // This is the only successor edge for this block, so likelihood should be 1.0
        bbTargetEdge->setLikelihood(1.0);
    }

    // Set the block kind, and clear bbTargetEdge.
    void SetKindAndTargetEdge(BBKinds kind)
    {
        bbKind       = kind;
        bbTargetEdge = nullptr;
        assert(!HasTarget());
    }

    bool HasInitializedTarget() const
    {
        assert(HasTarget());
        return (bbTargetEdge != nullptr);
    }

    bool TargetIs(const BasicBlock* target) const
    {
        return (GetTarget() == target);
    }

    bool JumpsToNext() const
    {
        return (GetTarget() == bbNext);
    }

    BBswtDesc* GetSwitchTargets() const
    {
        assert(KindIs(BBJ_SWITCH));
        assert(bbSwtTargets != nullptr);
        return bbSwtTargets;
    }

    void SetSwitch(BBswtDesc* swtTarget)
    {
        assert(swtTarget != nullptr);
        bbKind       = BBJ_SWITCH;
        bbSwtTargets = swtTarget;
    }

    BBehfDesc* GetEhfTargets() const
    {
        assert(KindIs(BBJ_EHFINALLYRET));
        return bbEhfTargets;
    }

    void SetEhfTargets(BBehfDesc* ehfTarget)
    {
        assert(KindIs(BBJ_EHFINALLYRET));
        bbEhfTargets = ehfTarget;
    }

    void SetEhf(BBehfDesc* ehfTarget)
    {
        assert(ehfTarget != nullptr);
        bbKind       = BBJ_EHFINALLYRET;
        bbEhfTargets = ehfTarget;
    }

    // BBJ_CALLFINALLYRET uses the `bbTargetEdge` field. However, also treat it specially:
    // for callers that know they want a continuation, use this function instead of the
    // general `GetTarget()` to allow asserting on the block kind.
    BasicBlock* GetFinallyContinuation() const
    {
        assert(KindIs(BBJ_CALLFINALLYRET));
        return GetTarget();
    }

#ifdef DEBUG

    // Return the block target; it might be null. Only used during dumping.
    BasicBlock* GetTargetRaw() const
    {
        assert(HasTarget());
        return (bbTargetEdge == nullptr) ? nullptr : bbTargetEdge->getDestinationBlock();
    }

    // Return the BBJ_COND true target; it might be null. Only used during dumping.
    BasicBlock* GetTrueTargetRaw() const
    {
        assert(KindIs(BBJ_COND));
        return (bbTrueEdge == nullptr) ? nullptr : bbTrueEdge->getDestinationBlock();
    }

    // Return the BBJ_COND false target; it might be null. Only used during dumping.
    BasicBlock* GetFalseTargetRaw() const
    {
        assert(KindIs(BBJ_COND));
        return (bbFalseEdge == nullptr) ? nullptr : bbFalseEdge->getDestinationBlock();
    }

    // Return the target edge; it might be null. Only used during dumping.
    FlowEdge* GetTargetEdgeRaw() const
    {
        assert(HasTarget());
        return bbTargetEdge;
    }

    // Return the BBJ_COND true target edge; it might be null. Only used during dumping.
    FlowEdge* GetTrueEdgeRaw() const
    {
        assert(KindIs(BBJ_COND));
        return bbTrueEdge;
    }

    // Return the BBJ_COND false target edge; it might be null. Only used during dumping.
    FlowEdge* GetFalseEdgeRaw() const
    {
        assert(KindIs(BBJ_COND));
        return bbFalseEdge;
    }

#endif // DEBUG

private:
    BasicBlockFlags bbFlags;

public:
    // MSVC doesn't inline this method in large callers by default
    FORCEINLINE BasicBlockFlags HasFlag(const BasicBlockFlags flag) const
    {
        // Assert flag is not multiple BasicBlockFlags OR'd together
        // by checking if it is a power of 2
        // (HasFlag expects to check only one flag at a time)
        assert(isPow2(flag));
        return (bbFlags & flag);
    }

    // HasAnyFlag takes a set of flags OR'd together. It requires at least
    // two flags to be set (or else you should use `HasFlag`).
    // It is true if *any* of those flags are set on the block.
    BasicBlockFlags HasAnyFlag(const BasicBlockFlags flags) const
    {
        assert((flags != BBF_EMPTY) && !isPow2(flags));
        return (bbFlags & flags);
    }

    // HasAllFlags takes a set of flags OR'd together. It requires at least
    // two flags to be set (or else you should use `HasFlag`).
    // It is true if *all* of those flags are set on the block.
    bool HasAllFlags(const BasicBlockFlags flags) const
    {
        assert((flags != BBF_EMPTY) && !isPow2(flags));
        return (bbFlags & flags) == flags;
    }

    // Copy all the flags from another block. This is a complete copy; any flags
    // that were previously set on this block are overwritten.
    void CopyFlags(const BasicBlock* block)
    {
        bbFlags = block->bbFlags;
    }

    // Copy the values of a specific set of flags from another block. All flags
    // not in the mask are preserved. Note however, that only set flags are copied;
    // if a flag in the mask is already set in this block, it will not be reset!
    // (Perhaps we should have a `ReplaceFlags` function that first clears the
    // bits in `mask` before doing the copy. Possibly we should assert that
    // `(bbFlags & mask) == 0` under the assumption that we copy flags when
    // creating a new block from scratch.)
    void CopyFlags(const BasicBlock* block, const BasicBlockFlags mask)
    {
        bbFlags |= (block->bbFlags & mask);
    }

    // MSVC doesn't inline this method in large callers by default
    FORCEINLINE void SetFlags(const BasicBlockFlags flags)
    {
        bbFlags |= flags;
    }

    void RemoveFlags(const BasicBlockFlags flags)
    {
        bbFlags &= ~flags;
    }

    BasicBlockFlags GetFlagsRaw() const
    {
        return bbFlags;
    }

    void SetFlagsRaw(const BasicBlockFlags flags)
    {
        bbFlags = flags;
    }

    static_assert_no_msg((BBF_SPLIT_NONEXIST & BBF_SPLIT_LOST) == 0);
    static_assert_no_msg((BBF_SPLIT_NONEXIST & BBF_SPLIT_GAINED) == 0);

    unsigned bbNum; // the block's number

    unsigned bbRefs; // number of blocks that can reach here, either by fall-through or a branch. If this falls to zero,
                     // the block is unreachable.

    bool isRunRarely() const
    {
        return HasFlag(BBF_RUN_RARELY);
    }
    bool isLoopHead() const
    {
        return HasFlag(BBF_LOOP_HEAD);
    }

    bool isLoopAlign() const
    {
        return HasFlag(BBF_LOOP_ALIGN);
    }

    bool hasAlign() const
    {
        return HasFlag(BBF_HAS_ALIGN);
    }

#ifdef DEBUG
    void     dspFlags() const;             // Print the flags
    unsigned dspPreds() const;             // Print the predecessors (bbPreds)
    void     dspSuccs(Compiler* compiler); // Print the successors. The 'compiler' argument determines whether EH
                                           // regions are printed: see NumSucc() for details.
    void dspKind() const;                  // Print the block jump kind (e.g., BBJ_ALWAYS, BBJ_COND, etc.).

    // Print a simple basic block header for various output, including a list of predecessors and successors.
    void dspBlockHeader(Compiler* compiler, bool showKind = true, bool showFlags = false, bool showPreds = true);

    const char* dspToString(int blockNumPadding = 0) const;
#endif // DEBUG

#define BB_UNITY_WEIGHT          100.0 // how much a normal execute once block weighs
#define BB_UNITY_WEIGHT_UNSIGNED 100   // how much a normal execute once block weighs
#define BB_LOOP_WEIGHT_SCALE     8.0   // synthetic profile scale factor for loops
#define BB_ZERO_WEIGHT           0.0
#define BB_MAX_WEIGHT            FLT_MAX // maximum finite weight  -- needs rethinking.

    weight_t bbWeight; // The dynamic execution weight of this block

    // getCalledCount -- get the value used to normalize weights for this method
    static weight_t getCalledCount(Compiler* comp);

    // getBBWeight -- get the normalized weight of this block
    weight_t getBBWeight(Compiler* comp) const;

    // hasProfileWeight -- Returns true if this block's weight came from profile data
    bool hasProfileWeight() const
    {
        return this->HasFlag(BBF_PROF_WEIGHT);
    }

    // setBBProfileWeight -- Set the profile-derived weight for a basic block
    // and update the run rarely flag as appropriate.
    void setBBProfileWeight(weight_t weight)
    {
        this->SetFlags(BBF_PROF_WEIGHT);
        this->bbWeight = weight;

        if (weight == BB_ZERO_WEIGHT)
        {
            this->SetFlags(BBF_RUN_RARELY);
        }
        else
        {
            this->RemoveFlags(BBF_RUN_RARELY);
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
            this->SetFlags(BBF_PROF_WEIGHT);
        }
        else
        {
            this->RemoveFlags(BBF_PROF_WEIGHT);
        }

        if (this->bbWeight == BB_ZERO_WEIGHT)
        {
            this->SetFlags(BBF_RUN_RARELY);
        }
        else
        {
            this->RemoveFlags(BBF_RUN_RARELY);
        }
    }

    // Scale a blocks' weight by some factor.
    //
    void scaleBBWeight(weight_t scale)
    {
        this->bbWeight = this->bbWeight * scale;

        if (this->bbWeight == BB_ZERO_WEIGHT)
        {
            this->SetFlags(BBF_RUN_RARELY);
        }
        else
        {
            this->RemoveFlags(BBF_RUN_RARELY);
        }
    }

    // Set block weight to zero, and set run rarely flag.
    //
    void bbSetRunRarely()
    {
        this->scaleBBWeight(BB_ZERO_WEIGHT);
    }

    bool isMaxBBWeight() const
    {
        return (bbWeight >= BB_MAX_WEIGHT);
    }

    // Returns "true" if the block is empty. Empty here means there are no statement
    // trees *except* PHI definitions.
    bool isEmpty() const;

    bool isValid() const;

    // Returns "true" iff "this" is the first block of a BBJ_CALLFINALLY/BBJ_CALLFINALLYRET pair --
    // a block corresponding to an exit from the try of a try/finally.
    bool isBBCallFinallyPair() const;

    // Returns "true" iff "this" is the last block of a BBJ_CALLFINALLY/BBJ_CALLFINALLYRET pair --
    // a block corresponding to an exit from the try of a try/finally.
    bool isBBCallFinallyPairTail() const;

    bool KindIs(BBKinds kind) const
    {
        return bbKind == kind;
    }

    template <typename... T>
    bool KindIs(BBKinds kind, T... rest) const
    {
        return KindIs(kind) || KindIs(rest...);
    }

    bool HasTerminator()
    {
        return KindIs(BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET, BBJ_COND, BBJ_SWITCH, BBJ_RETURN);
    }

    // NumSucc() gives the number of successors, and GetSucc() returns a given numbered successor.
    //
    // There are two versions of these functions: ones that take a Compiler* and ones that don't. You must
    // always use a matching set. Thus, if you call NumSucc() without a Compiler*, you must also call
    // GetSucc() without a Compiler*.
    //
    // The behavior of NumSucc()/GetSucc() is different when passed a Compiler* for blocks that end in:
    // (1) BBJ_SWITCH
    //
    // For BBJ_SWITCH, if Compiler* is not passed, then all switch successors are returned. If Compiler*
    // is passed, then only unique switch successors are returned; the duplicate successors are omitted.
    //
    // Note that for BBJ_COND, which has two successors (fall through (condition false), and condition true
    // branch target), only the unique targets are returned. Thus, if both targets are the same, NumSucc()
    // will only return 1 instead of 2.
    //
    // NumSucc: Returns the number of successors of "this".
    unsigned NumSucc() const;
    unsigned NumSucc(Compiler* comp);

    // GetSuccEdge: Returns the "i"th successor edge. Requires (0 <= i < NumSucc()).
    FlowEdge* GetSuccEdge(unsigned i) const;
    FlowEdge* GetSuccEdge(unsigned i, Compiler* comp);

    // GetSucc: Returns the "i"th successor block. Requires (0 <= i < NumSucc()).
    BasicBlock* GetSucc(unsigned i) const;
    BasicBlock* GetSucc(unsigned i, Compiler* comp);

    // SwitchTargets: convenience method for enabling range-based `for` iteration over a switch block's targets, e.g.:
    //    for (BasicBlock* const bTarget : block->SwitchTargets()) ...
    //
    BBSwitchTargetList SwitchTargets() const
    {
        assert(bbKind == BBJ_SWITCH);
        return BBSwitchTargetList(bbSwtTargets);
    }

    // EHFinallyRetSuccs: convenience method for enabling range-based `for` iteration over BBJ_EHFINALLYRET block
    // successors, e.g.:
    //    for (BasicBlock* const succ : block->EHFinallyRetSuccs()) ...
    //
    BBEhfSuccList EHFinallyRetSuccs() const
    {
        assert(bbKind == BBJ_EHFINALLYRET);
        return BBEhfSuccList(bbEhfTargets);
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

    EntryState* bbEntryState; // verifier tracked state of all entries in stack.

#define NO_BASE_TMP UINT_MAX // base# to use when we have none

    union
    {
        unsigned bbStkTempsIn;       // base# for input stack temps
        int      bbCountSchemaIndex; // schema index for count instrumentation
    };

    union
    {
        unsigned bbStkTempsOut;          // base# for output stack temps
        int      bbHistogramSchemaIndex; // schema index for histogram instrumentation
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

    void copyTryIndex(const BasicBlock* from)
    {
        bbTryIndex = from->bbTryIndex;
    }

    void copyHndIndex(const BasicBlock* from)
    {
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
#define BBCT_NONE                   0x00000000
#define BBCT_FAULT                  0xFFFFFFFC
#define BBCT_FINALLY                0xFFFFFFFD
#define BBCT_FILTER                 0xFFFFFFFE
#define BBCT_FILTER_HANDLER         0xFFFFFFFF
#define handlerGetsXcptnObj(hndTyp) ((hndTyp) != BBCT_NONE && (hndTyp) != BBCT_FAULT && (hndTyp) != BBCT_FINALLY)

    // TODO-Cleanup: Get rid of bbStkDepth and use bbStackDepthOnEntry() instead
    unsigned short bbStkDepth; // stack depth on entry

    // Basic block predecessor lists. Predecessor lists are created by fgLinkBasicBlocks(), stored
    // in 'bbPreds', and then maintained throughout compilation. 'fgPredsComputed' will be 'true' after the
    // predecessor lists are created.
    //
    FlowEdge* bbPreds; // ptr to list of predecessors

    // PredEdges: convenience method for enabling range-based `for` iteration over predecessor edges, e.g.:
    //    for (FlowEdge* const edge : block->PredEdges()) ...
    //
    PredEdgeList PredEdges() const
    {
        return PredEdgeList(bbPreds);
    }

    // PredBlocks: convenience method for enabling range-based `for` iteration over predecessor blocks, e.g.:
    //    for (BasicBlock* const predBlock : block->PredBlocks()) ...
    //
    PredBlockList<false> PredBlocks() const
    {
        return PredBlockList<false>(bbPreds);
    }

    // PredBlocksEditing: convenience method for enabling range-based `for` iteration over predecessor blocks, e.g.:
    //    for (BasicBlock* const predBlock : block->PredBlocksEditing()) ...
    // This iterator tolerates modifications to bbPreds.
    //
    PredBlockList<true> PredBlocksEditing() const
    {
        return PredBlockList<true>(bbPreds);
    }

    // Pred list maintenance
    //
    bool checkPredListOrder();
    void ensurePredListOrder(Compiler* compiler);
    void reorderPredList(Compiler* compiler);

    union
    {
        BasicBlock* bbIDom;          // Represent the closest dominator to this block (called the Immediate
                                     // Dominator) used to compute the dominance tree.
        FlowEdge* bbLastPred;        // Used early on by fgLinkBasicBlock/fgAddRefPred
        void*     bbSparseProbeList; // Used early on by fgInstrument
    };

    void* bbSparseCountInfo; // Used early on by fgIncorporateEdgeCounts

    unsigned bbPreorderNum;  // the block's  preorder number in the graph [0...postOrderCount)
    unsigned bbPostorderNum; // the block's postorder number in the graph [0...postOrderCount)

    IL_OFFSET bbCodeOffs;    // IL offset of the beginning of the block
    IL_OFFSET bbCodeOffsEnd; // IL offset past the end of the block. Thus, the [bbCodeOffs..bbCodeOffsEnd)
                             // range is not inclusive of the end offset. The count of IL bytes in the block
                             // is bbCodeOffsEnd - bbCodeOffs, assuming neither are BAD_IL_OFFSET.

#ifdef DEBUG
    void dspBlockILRange() const; // Display the block's IL range as [XXX...YYY), where XXX and YYY might be "???" for
                                  // BAD_IL_OFFSET.
#endif                            // DEBUG

    VARSET_TP bbVarUse; // variables used     by block (before a definition)
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

        MemoryPhiArg(unsigned ssaNum, MemoryPhiArg* nextArg = nullptr)
            : m_ssaNum(ssaNum)
            , m_nextArg(nextArg)
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

    union
    {
        EXPSET_TP bbCseGen;             // CSEs computed by block
        ASSERT_TP bbAssertionGen;       // assertions created by block (global prop)
        ASSERT_TP bbAssertionOutIfTrue; // assertions available on exit along true/jump edge (BBJ_COND, local prop)
    };

    union
    {
        EXPSET_TP bbCseIn;       // CSEs available on entry
        ASSERT_TP bbAssertionIn; // assertions available on entry (global prop)
    };

    union
    {
        EXPSET_TP bbCseOut;              // CSEs available on exit
        ASSERT_TP bbAssertionOut;        // assertions available on exit (global prop, local prop & !BBJ_COND)
        ASSERT_TP bbAssertionOutIfFalse; // assertions available on exit along false/next edge (BBJ_COND, local prop)
    };

    void* bbEmitCookie;

    //-------------------------------------------------------------------------

#if MEASURE_BLOCK_SIZE
    static size_t s_Size;
    static size_t s_Count;
#endif // MEASURE_BLOCK_SIZE

    bool bbFallsThrough() const;

    // Our slop fraction is 1/50 of the block weight.
    static weight_t GetSlopFraction(weight_t weightBlk)
    {
        return weightBlk / 50.0;
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

    unsigned    bbStackDepthOnEntry() const;
    void        bbSetStack(StackEntry* stack);
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
    bool       hasSingleStmt() const;

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

    GenTree* lastNode() const;

    bool endsWithJmpMethod(Compiler* comp) const;

    bool endsWithTailCall(Compiler* comp,
                          bool      fastTailCallsOnly,
                          bool      tailCallsConvertibleToLoopOnly,
                          GenTree** tailCall) const;

    bool endsWithTailCallOrJmp(Compiler* comp, bool fastTailCallsOnly = false) const;

    bool endsWithTailCallConvertibleToLoop(Compiler* comp, GenTree** tailCall) const;

    // Returns the first statement in the statement list of "this" that is
    // not an SSA definition (a lcl = phi(...) store).
    Statement* FirstNonPhiDef() const;
    Statement* FirstNonPhiDefOrCatchArgStore() const;

    BasicBlock()
        : bbStmtList(nullptr)
        , bbLiveIn(VarSetOps::UninitVal())
        , bbLiveOut(VarSetOps::UninitVal())
    {
    }

    // Iteratable collection of successors of a block.
    template <typename TPosition>
    class Successors
    {
        Compiler*   m_comp;
        BasicBlock* m_block;

    public:
        Successors(Compiler* comp, BasicBlock* block)
            : m_comp(comp)
            , m_block(block)
        {
        }

        class iterator
        {
            Compiler*   m_comp;
            BasicBlock* m_block;
            TPosition   m_pos;

        public:
            iterator(Compiler* comp, BasicBlock* block)
                : m_comp(comp)
                , m_block(block)
                , m_pos(comp, block)
            {
            }

            iterator()
                : m_pos()
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

    template <typename TFunc>
    BasicBlockVisit VisitEHEnclosedHandlerSecondPassSuccs(Compiler* comp, TFunc func);

    template <typename TFunc>
    BasicBlockVisit VisitAllSuccs(Compiler* comp, TFunc func, const bool useProfile = false);

    template <typename TFunc>
    BasicBlockVisit VisitEHSuccs(Compiler* comp, TFunc func);

    template <typename TFunc>
    BasicBlockVisit VisitRegularSuccs(Compiler* comp, TFunc func);

    bool HasPotentialEHSuccs(Compiler* comp);

    // Base class for Successor block/edge iterators.
    //
    class SuccList
    {
    protected:
        // For one or two successors, pre-compute and stash the successors inline, in m_succs[], so we don't
        // need to call a function or execute another `switch` to get them. Also, pre-compute the begin and end
        // points of the iteration, for use by BBArrayIterator. `m_begin` and `m_end` will either point at
        // `m_succs` or at the switch table successor array.
        FlowEdge*        m_succs[2];
        FlowEdge* const* m_begin;
        FlowEdge* const* m_end;

        SuccList(const BasicBlock* block);
    };

    // BBSuccList: adapter class for forward iteration of block successors, using range-based `for`,
    // normally used via BasicBlock::Succs(), e.g.:
    //    for (BasicBlock* const target : block->Succs()) ...
    //
    class BBSuccList : private SuccList
    {
    public:
        BBSuccList(const BasicBlock* block)
            : SuccList(block)
        {
        }

        BBArrayIterator begin() const
        {
            return BBArrayIterator(m_begin);
        }

        BBArrayIterator end() const
        {
            return BBArrayIterator(m_end);
        }
    };

    // BBSuccEdgeList: adapter class for forward iteration of block successors edges, using range-based `for`,
    // normally used via BasicBlock::SuccEdges(), e.g.:
    //    for (FlowEdge* const succEdge : block->SuccEdges()) ...
    //
    class BBSuccEdgeList : private SuccList
    {
    public:
        BBSuccEdgeList(const BasicBlock* block)
            : SuccList(block)
        {
        }

        FlowEdgeArrayIterator begin() const
        {
            return FlowEdgeArrayIterator(m_begin);
        }

        FlowEdgeArrayIterator end() const
        {
            return FlowEdgeArrayIterator(m_end);
        }
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

        // iterator: forward iterator for an array of BasicBlock*
        //
        class iterator
        {
            Compiler*   m_comp;
            BasicBlock* m_block;
            unsigned    m_succNum;

        public:
            iterator(Compiler* comp, BasicBlock* block, unsigned succNum)
                : m_comp(comp)
                , m_block(block)
                , m_succNum(succNum)
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
        BBCompilerSuccList(Compiler* comp, BasicBlock* block)
            : m_comp(comp)
            , m_block(block)
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

    // BBCompilerSuccEdgeList: adapter class for forward iteration of block successors edges, using range-based `for`,
    // normally used via BasicBlock::SuccEdges(), e.g.:
    //    for (FlowEdge* const succEdge : block->SuccEdges(compiler)) ...
    //
    // This version uses NumSucc(Compiler*)/GetSucc(Compiler*). See the documentation there for the explanation
    // of the implications of this versus the version that does not take `Compiler*`.
    class BBCompilerSuccEdgeList
    {
        Compiler*   m_comp;
        BasicBlock* m_block;

        // iterator: forward iterator for an array of BasicBlock*
        //
        class iterator
        {
            Compiler*   m_comp;
            BasicBlock* m_block;
            unsigned    m_succNum;

        public:
            iterator(Compiler* comp, BasicBlock* block, unsigned succNum)
                : m_comp(comp)
                , m_block(block)
                , m_succNum(succNum)
            {
            }

            FlowEdge* operator*() const
            {
                assert(m_block != nullptr);
                FlowEdge* succEdge = m_block->GetSuccEdge(m_succNum, m_comp);
                assert(succEdge != nullptr);
                return succEdge;
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
        BBCompilerSuccEdgeList(Compiler* comp, BasicBlock* block)
            : m_comp(comp)
            , m_block(block)
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

    BBSuccEdgeList SuccEdges()
    {
        return BBSuccEdgeList(this);
    }

    BBCompilerSuccEdgeList SuccEdges(Compiler* comp)
    {
        return BBCompilerSuccEdgeList(comp, this);
    }

    // Clone block state and statements from `from` block to `to` block (which must be new/empty)
    static void CloneBlockState(Compiler* compiler, BasicBlock* to, const BasicBlock* from);

    // Copy the block kind and take memory ownership of the targets.
    void TransferTarget(BasicBlock* from);

    void MakeLIR(GenTree* firstNode, GenTree* lastNode);
    bool IsLIR() const;

    void SetDominatedByExceptionalEntryFlag()
    {
        SetFlags(BBF_DOMINATED_BY_EXCEPTIONAL_ENTRY);
    }

    bool IsDominatedByExceptionalEntryFlag() const
    {
        return HasFlag(BBF_DOMINATED_BY_EXCEPTIONAL_ENTRY);
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
    BasicBlockIterator(BasicBlock* block)
        : m_block(block)
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
        assert(m_block->IsLast() || m_block->Next()->PrevIs(m_block));
        assert(m_block->IsFirst() || m_block->Prev()->NextIs(m_block));

        m_block = m_block->Next();
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
    BasicBlockSimpleList(BasicBlock* begin)
        : m_begin(begin)
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
// Note that endBlock->bbNext is captured at the beginning of the iteration. Thus, any blocks
// inserted before that will continue the iteration. In particular, inserting blocks between endBlock
// and endBlock->bbNext will yield unexpected results, as the iteration will continue longer than desired.
//
class BasicBlockRangeList
{
    BasicBlock* m_begin;
    BasicBlock* m_end;

public:
    BasicBlockRangeList(BasicBlock* begin, BasicBlock* end)
        : m_begin(begin)
        , m_end(end)
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
        return BasicBlockIterator(m_end->Next()); // walk until we see the block *following* the `m_end` block
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
    FlowEdge** bbsDstTab; // case label table address
    unsigned   bbsCount;  // count of cases (includes 'default' if bbsHasDefault)

    // Case number and likelihood of most likely case
    // (only known with PGO, only valid if bbsHasDominantCase is true)
    unsigned bbsDominantCase;
    weight_t bbsDominantFraction;

    bool bbsHasDefault;      // true if last switch case is a default case
    bool bbsHasDominantCase; // true if switch has a dominant case

    BBswtDesc()
        : bbsHasDefault(true)
        , bbsHasDominantCase(false)
    {
    }

    BBswtDesc(const BBswtDesc* other);

    BBswtDesc(Compiler* comp, const BBswtDesc* other);

    void removeDefault()
    {
        assert(bbsHasDefault);
        assert(bbsCount > 0);
        bbsHasDefault = false;
        bbsCount--;
    }

    FlowEdge* getDefault()
    {
        assert(bbsHasDefault);
        assert(bbsCount > 0);
        return bbsDstTab[bbsCount - 1];
    }
};

// BBSwitchTargetList out-of-class-declaration implementations (here due to C++ ordering requirements).
//

inline BBSwitchTargetList::BBSwitchTargetList(BBswtDesc* bbsDesc)
    : m_bbsDesc(bbsDesc)
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

// BBehfDesc -- descriptor for a BBJ_EHFINALLYRET block
//
struct BBehfDesc
{
    FlowEdge** bbeSuccs; // array of `FlowEdge*` pointing to BBJ_EHFINALLYRET block successors
    unsigned   bbeCount; // size of `bbeSuccs` array

    BBehfDesc()
        : bbeSuccs(nullptr)
        , bbeCount(0)
    {
    }

    BBehfDesc(Compiler* comp, const BBehfDesc* other);
};

// BBEhfSuccList out-of-class-declaration implementations (here due to C++ ordering requirements).
//

inline BBEhfSuccList::BBEhfSuccList(BBehfDesc* bbeDesc)
    : m_bbeDesc(bbeDesc)
{
    assert(m_bbeDesc != nullptr);
    assert((m_bbeDesc->bbeSuccs != nullptr) || (m_bbeDesc->bbeCount == 0));
}

inline BBArrayIterator BBEhfSuccList::begin() const
{
    return BBArrayIterator(m_bbeDesc->bbeSuccs);
}

inline BBArrayIterator BBEhfSuccList::end() const
{
    return BBArrayIterator(m_bbeDesc->bbeSuccs + m_bbeDesc->bbeCount);
}

// SuccList out-of-class-declaration implementations
//
inline BasicBlock::SuccList::SuccList(const BasicBlock* block)
{
    assert(block != nullptr);

    switch (block->bbKind)
    {
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_EHFAULTRET:
            // We don't need m_succs.
            m_begin = nullptr;
            m_end   = nullptr;
            break;

        case BBJ_CALLFINALLY:
        case BBJ_CALLFINALLYRET:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
        case BBJ_LEAVE:
            m_succs[0] = block->GetTargetEdge();
            m_begin    = &m_succs[0];
            m_end      = &m_succs[1];
            break;

        case BBJ_COND:
            m_succs[0] = block->GetFalseEdge();
            m_begin    = &m_succs[0];

            // If both fall-through and branch successors are identical, then only include
            // them once in the iteration (this is the same behavior as NumSucc()/GetSucc()).
            if (block->TrueEdgeIs(block->GetFalseEdge()))
            {
                m_end = &m_succs[1];
            }
            else
            {
                m_succs[1] = block->GetTrueEdge();
                m_end      = &m_succs[2];
            }
            break;

        case BBJ_EHFINALLYRET:
            // We don't use the m_succs in-line data; use the existing successor table in the block.
            // We must tolerate iterating successors early in the system, before EH_FINALLYRET successors have
            // been computed.
            if (block->GetEhfTargets() == nullptr)
            {
                m_begin = nullptr;
                m_end   = nullptr;
            }
            else
            {
                m_begin = block->GetEhfTargets()->bbeSuccs;
                m_end   = block->GetEhfTargets()->bbeSuccs + block->GetEhfTargets()->bbeCount;
            }
            break;

        case BBJ_SWITCH:
            // We don't use the m_succs in-line data for switches; use the existing jump table in the block.
            assert(block->bbSwtTargets != nullptr);
            assert(block->bbSwtTargets->bbsDstTab != nullptr);
            m_begin = block->bbSwtTargets->bbsDstTab;
            m_end   = block->bbSwtTargets->bbsDstTab + block->bbSwtTargets->bbsCount;
            break;

        default:
            unreached();
    }

    assert(m_end >= m_begin);
}

// We have a simpler struct, BasicBlockList, which is simply a singly-linked
// list of blocks.

struct BasicBlockList
{
    BasicBlockList* next;  // The next BasicBlock in the list, nullptr for end of list.
    BasicBlock*     block; // The BasicBlock of interest.

    BasicBlockList()
        : next(nullptr)
        , block(nullptr)
    {
    }

    BasicBlockList(BasicBlock* blk, BasicBlockList* rest)
        : next(rest)
        , block(blk)
    {
    }
};

// FlowEdge implementations (that are required to be defined after the declaration of BasicBlock)

inline weight_t FlowEdge::getLikelyWeight() const
{
    assert(m_likelihoodSet);
    return m_likelihood * m_sourceBlock->bbWeight;
}

// BasicBlock iterator implementations (that are required to be defined after the declaration of FlowEdge)

inline BasicBlock* BBArrayIterator::operator*() const
{
    assert(m_edgeEntry != nullptr);
    FlowEdge* edgeTarget = *m_edgeEntry;
    assert(edgeTarget != nullptr);
    assert(edgeTarget->getDestinationBlock() != nullptr);
    return edgeTarget->getDestinationBlock();
}

// Pred list iterator implementations (that are required to be defined after the declaration of BasicBlock and FlowEdge)

inline PredEdgeList::iterator::iterator(FlowEdge* pred)
    : m_pred(pred)
{
#ifdef DEBUG
    m_next = (m_pred == nullptr) ? nullptr : m_pred->getNextPredEdge();
#endif
}

inline PredEdgeList::iterator& PredEdgeList::iterator::operator++()
{
    FlowEdge* next = m_pred->getNextPredEdge();

#ifdef DEBUG
    // Check that the next block is the one we expect to see.
    assert(next == m_next);
    m_next = (next == nullptr) ? nullptr : next->getNextPredEdge();
#endif // DEBUG

    m_pred = next;
    return *this;
}

template <bool allowEdits>
inline PredBlockList<allowEdits>::iterator::iterator(FlowEdge* pred)
    : m_pred(pred)
{
    bool initNextPointer = allowEdits;
    INDEBUG(initNextPointer = true);
    if (initNextPointer)
    {
        m_next = (m_pred == nullptr) ? nullptr : m_pred->getNextPredEdge();
    }
}

template <bool allowEdits>
inline BasicBlock* PredBlockList<allowEdits>::iterator::operator*() const
{
    return m_pred->getSourceBlock();
}

template <bool allowEdits>
inline typename PredBlockList<allowEdits>::iterator& PredBlockList<allowEdits>::iterator::operator++()
{
    if (allowEdits)
    {
        // For editing iterators, m_next is always used and maintained
        m_pred = m_next;
        m_next = (m_next == nullptr) ? nullptr : m_next->getNextPredEdge();
    }
    else
    {
        FlowEdge* next = m_pred->getNextPredEdge();

#ifdef DEBUG
        // If allowEdits=false, check that the next block is the one we expect to see.
        assert(next == m_next);
        m_next = (m_next == nullptr) ? nullptr : m_next->getNextPredEdge();
#endif // DEBUG

        m_pred = next;
    }

    return *this;
}

/*****************************************************************************
 *
 *  The following call-backs supplied by the client; it's used by the code
 *  emitter to convert a basic block to its corresponding emitter cookie.
 */

void* emitCodeGetCookie(const BasicBlock* block);

// An enumerator of a block's all successors. In some cases (e.g. SsaBuilder::TopologicalSort)
// using iterators is not exactly efficient, at least because they contain an unnecessary
// member - a pointer to the Compiler object.
class AllSuccessorEnumerator
{
    BasicBlock* m_block;
    union
    {
        // We store up to 4 successors inline in the enumerator. For ASP.NET
        // and libraries.pmi this is enough in 99.7% of cases.
        BasicBlock*  m_successors[4];
        BasicBlock** m_pSuccessors;
    };

    unsigned m_numSuccs;
    unsigned m_curSucc = UINT_MAX;

public:
    // Constructs an enumerator of all `block`'s successors.
    AllSuccessorEnumerator(Compiler* comp, BasicBlock* block, const bool useProfile = false);

    // Gets the block whose successors are enumerated.
    BasicBlock* Block()
    {
        return m_block;
    }

    // Returns the next available successor or `nullptr` if there are no more successors.
    BasicBlock* NextSuccessor()
    {
        m_curSucc++;
        if (m_curSucc >= m_numSuccs)
        {
            return nullptr;
        }

        if (m_numSuccs <= ArrLen(m_successors))
        {
            return m_successors[m_curSucc];
        }

        return m_pSuccessors[m_curSucc];
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
