// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "simplerhash.h"

/*****************************************************************************/

#if LARGE_EXPSET
typedef unsigned __int64 EXPSET_TP;
#define EXPSET_SZ 64
#else
typedef unsigned int EXPSET_TP;
#define EXPSET_SZ 32
#endif

#define EXPSET_ALL ((EXPSET_TP)0 - 1)

typedef BitVec          ASSERT_TP;
typedef BitVec_ValArg_T ASSERT_VALARG_TP;
typedef BitVec_ValRet_T ASSERT_VALRET_TP;

/*****************************************************************************
 *
 *  Each basic block ends with a jump which is described as a value
 *  of the following enumeration.
 */

DECLARE_TYPED_ENUM(BBjumpKinds, BYTE)
{
    BBJ_EHFINALLYRET,    // block ends with 'endfinally' (for finally or fault)
        BBJ_EHFILTERRET, // block ends with 'endfilter'
        BBJ_EHCATCHRET,  // block ends with a leave out of a catch (only #if FEATURE_EH_FUNCLETS)
        BBJ_THROW,       // block ends with 'throw'
        BBJ_RETURN,      // block ends with 'ret'

        BBJ_NONE, // block flows into the next one (no jump)

        BBJ_ALWAYS,      // block always jumps to the target
        BBJ_LEAVE,       // block always jumps to the target, maybe out of guarded
                         // region. Used temporarily until importing
        BBJ_CALLFINALLY, // block always calls the target finally
        BBJ_COND,        // block conditionally jumps to the target
        BBJ_SWITCH,      // block ends with a switch statement

        BBJ_COUNT
}
END_DECLARE_TYPED_ENUM(BBjumpKinds, BYTE)

struct GenTree;
struct GenTreeStmt;
struct BasicBlock;
class Compiler;
class typeInfo;
struct BasicBlockList;
struct flowList;
struct EHblkDsc;

#if FEATURE_STACK_FP_X87
struct FlatFPStateX87;
#endif

/*****************************************************************************
 *
 *  The following describes a switch block.
 *
 *  Things to know:
 *  1. If bbsHasDefault is true, the default case is the last one in the array of basic block addresses
 *     namely bbsDstTab[bbsCount - 1].
 *  2. bbsCount must be at least 1, for the default case. bbsCount cannot be zero. It appears that the ECMA spec
 *     allows for a degenerate switch with zero cases. Normally, the optimizer will optimize degenerate
 *     switches with just a default case to a BBJ_ALWAYS branch, and a switch with just two cases to a BBJ_COND.
 *     However, in debuggable code, we might not do that, so bbsCount might be 1.
 */
struct BBswtDesc
{
    unsigned     bbsCount;  // count of cases (includes 'default' if bbsHasDefault)
    BasicBlock** bbsDstTab; // case label table address
    bool         bbsHasDefault;

    BBswtDesc() : bbsHasDefault(true)
    {
    }

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
    ThisInitState thisInitialized : 8; // used to track whether the this ptr is initialized (we could use
                                       // fewer bits here)
    unsigned    esStackDepth : 24;     // size of esStack
    StackEntry* esStack;               // ptr to  stack
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
class EHSuccessorIter
{
    // The current compilation.
    Compiler* m_comp;

    // The block whose EH successors we are iterating over.
    BasicBlock* m_block;

    // The current "regular" successor of "m_block" that we're considering.
    BasicBlock* m_curRegSucc;

    // The current try block.  If non-null, then the current successor "m_curRegSucc"
    // is the first block of the handler of this block.  While this try block has
    // enclosing try's that also start with "m_curRegSucc", the corresponding handlers will be
    // further EH successors.
    EHblkDsc* m_curTry;

    // The number of "regular" (i.e., non-exceptional) successors that remain to
    // be considered.  If BB1 has successor BB2, and BB2 is the first block of a
    // try block, then we consider the catch block of BB2's try to be an EH
    // successor of BB1.  This captures the iteration over the successors of BB1
    // for this purpose.  (In reverse order; we're done when this field is 0).
    int m_remainingRegSuccs;

    // Requires that "m_curTry" is NULL.  Determines whether there is, as
    // discussed just above, a regular successor that's the first block of a
    // try; if so, sets "m_curTry" to that try block.  (As noted above, selecting
    // the try containing the current regular successor as the "current try" may cause
    // multiple first-blocks of catches to be yielded as EH successors: trys enclosing
    // the current try are also included if they also start with the current EH successor.)
    void FindNextRegSuccTry();

public:
    // Returns the standard "end" iterator.
    EHSuccessorIter()
        : m_comp(nullptr), m_block(nullptr), m_curRegSucc(nullptr), m_curTry(nullptr), m_remainingRegSuccs(0)
    {
    }

    // Initializes the iterator to represent the EH successors of "block".
    EHSuccessorIter(Compiler* comp, BasicBlock* block);

    // Go on to the next EH successor.
    void operator++(void);

    // Requires that "this" is not equal to the standard "end" iterator.  Returns the
    // current EH successor.
    BasicBlock* operator*();

    // Returns "true" iff "*this" is equal to "ehsi" -- ignoring the "m_comp"
    // and "m_block" fields.
    bool operator==(const EHSuccessorIter& ehsi)
    {
        // Ignore the compiler; we'll assume that's the same.
        return m_curTry == ehsi.m_curTry && m_remainingRegSuccs == ehsi.m_remainingRegSuccs;
    }

    bool operator!=(const EHSuccessorIter& ehsi)
    {
        return !((*this) == ehsi);
    }
};

// Yields both normal and EH successors (in that order) in one iteration.
class AllSuccessorIter
{
    // Normal succ state.
    Compiler*       m_comp;
    BasicBlock*     m_blk;
    unsigned        m_normSucc;
    unsigned        m_numNormSuccs;
    EHSuccessorIter m_ehIter;

    // True iff m_blk is a BBJ_CALLFINALLY block, and the current try block of m_ehIter,
    // the first block of whose handler would be next yielded, is the jump target of m_blk.
    inline bool CurTryIsBlkCallFinallyTarget();

public:
    inline AllSuccessorIter()
    {
    }

    // Initializes "this" to iterate over all successors of "block."
    inline AllSuccessorIter(Compiler* comp, BasicBlock* block);

    // Used for constructing an appropriate "end" iter.  Should be called with
    // the number of normal successors of the block being iterated.
    AllSuccessorIter(unsigned numSuccs) : m_normSucc(numSuccs), m_numNormSuccs(numSuccs), m_ehIter()
    {
    }

    // Go on to the next successor.
    inline void operator++(void);

    // Requires that "this" is not equal to the standard "end" iterator.  Returns the
    // current successor.
    inline BasicBlock* operator*();

    // Returns "true" iff "*this" is equal to "asi" -- ignoring the "m_comp"
    // and "m_block" fields.
    bool operator==(const AllSuccessorIter& asi)
    {
        return m_normSucc == asi.m_normSucc && m_ehIter == asi.m_ehIter;
    }

    bool operator!=(const AllSuccessorIter& asi)
    {
        return !((*this) == asi);
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

    unsigned bbNum; // the block's number

    unsigned bbPostOrderNum; // the block's post order number in the graph.
    unsigned bbRefs; // number of blocks that can reach here, either by fall-through or a branch. If this falls to zero,
                     // the block is unreachable.

    unsigned bbFlags; // see BBF_xxxx below

#define BBF_VISITED 0x00000001 // BB visited during optimizations
#define BBF_MARKED 0x00000002  // BB marked  during optimizations
#define BBF_CHANGED 0x00000004 // input/output of this block has changed
#define BBF_REMOVED 0x00000008 // BB has been removed from bb-list

#define BBF_DONT_REMOVE 0x00000010         // BB should not be removed during flow graph optimizations
#define BBF_IMPORTED 0x00000020            // BB byte-code has been imported
#define BBF_INTERNAL 0x00000040            // BB has been added by the compiler
#define BBF_FAILED_VERIFICATION 0x00000080 // BB has verification exception

#define BBF_TRY_BEG 0x00000100       // BB starts a 'try' block
#define BBF_FUNCLET_BEG 0x00000200   // BB is the beginning of a funclet
#define BBF_HAS_NULLCHECK 0x00000400 // BB contains a null check
#define BBF_NEEDS_GCPOLL 0x00000800  // This BB is the source of a back edge and needs a GC Poll

#define BBF_RUN_RARELY 0x00001000 // BB is rarely run (catch clauses, blocks with throws etc)
#define BBF_LOOP_HEAD 0x00002000  // BB is the head of a loop
#define BBF_LOOP_CALL0 0x00004000 // BB starts a loop that sometimes won't call
#define BBF_LOOP_CALL1 0x00008000 // BB starts a loop that will always     call

#define BBF_HAS_LABEL 0x00010000     // BB needs a label
#define BBF_JMP_TARGET 0x00020000    // BB is a target of an implicit/explicit jump
#define BBF_HAS_JMP 0x00040000       // BB executes a JMP instruction (instead of return)
#define BBF_GC_SAFE_POINT 0x00080000 // BB has a GC safe point (a call).  More abstractly, BB does not
                                     // require a (further) poll -- this may be because this BB has a
                                     // call, or, in some cases, because the BB occurs in a loop, and
                                     // we've determined that all paths in the loop body leading to BB
                                     // include a call.
#define BBF_HAS_VTABREF 0x00100000   // BB contains reference of vtable
#define BBF_HAS_IDX_LEN 0x00200000   // BB contains simple index or length expressions on an array local var.
#define BBF_HAS_NEWARRAY 0x00400000  // BB contains 'new' of an array
#define BBF_HAS_NEWOBJ 0x00800000    // BB contains 'new' of an object type.

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
#define BBF_FINALLY_TARGET 0x01000000 // BB is the target of a finally return: where a finally will return during
                                      // non-exceptional flow. Because the ARM calling sequence for calling a
                                      // finally explicitly sets the return address to the finally target and jumps
                                      // to the finally, instead of using a call instruction, ARM needs this to
                                      // generate correct code at the finally target, to allow for proper stack
                                      // unwind from within a non-exceptional call to a finally.
#endif                                // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
#define BBF_BACKWARD_JUMP 0x02000000  // BB is surrounded by a backward jump/switch arc
#define BBF_RETLESS_CALL 0x04000000   // BBJ_CALLFINALLY that will never return (and therefore, won't need a paired
                                      // BBJ_ALWAYS); see isBBCallAlwaysPair().
#define BBF_LOOP_PREHEADER 0x08000000 // BB is a loop preheader block

#define BBF_COLD 0x10000000        // BB is cold
#define BBF_PROF_WEIGHT 0x20000000 // BB weight is computed from profile data
#ifdef LEGACY_BACKEND
#define BBF_FORWARD_SWITCH 0x40000000  // Aux flag used in FP codegen to know if a jmptable entry has been forwarded
#else                                  // !LEGACY_BACKEND
#define BBF_IS_LIR 0x40000000          // Set if the basic block contains LIR (as opposed to HIR)
#endif                                 // LEGACY_BACKEND
#define BBF_KEEP_BBJ_ALWAYS 0x80000000 // A special BBJ_ALWAYS block, used by EH code generation. Keep the jump kind
                                       // as BBJ_ALWAYS. Used for the paired BBJ_ALWAYS block following the
                                       // BBJ_CALLFINALLY block, as well as, on x86, the final step block out of a
                                       // finally.

    bool isRunRarely()
    {
        return ((bbFlags & BBF_RUN_RARELY) != 0);
    }
    bool isLoopHead()
    {
        return ((bbFlags & BBF_LOOP_HEAD) != 0);
    }

// Flags to update when two blocks are compacted

#define BBF_COMPACT_UPD                                                                                                \
    (BBF_CHANGED | BBF_GC_SAFE_POINT | BBF_HAS_JMP | BBF_NEEDS_GCPOLL | BBF_HAS_IDX_LEN | BBF_BACKWARD_JUMP |          \
     BBF_HAS_NEWARRAY | BBF_HAS_NEWOBJ)

// Flags a block should not have had before it is split.

#ifdef LEGACY_BACKEND
#define BBF_SPLIT_NONEXIST                                                                                             \
    (BBF_CHANGED | BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1 | BBF_RETLESS_CALL | BBF_LOOP_PREHEADER |           \
     BBF_COLD | BBF_FORWARD_SWITCH)
#else // !LEGACY_BACKEND
#define BBF_SPLIT_NONEXIST                                                                                             \
    (BBF_CHANGED | BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1 | BBF_RETLESS_CALL | BBF_LOOP_PREHEADER | BBF_COLD)
#endif // LEGACY_BACKEND

// Flags lost by the top block when a block is split.
// Note, this is a conservative guess.
// For example, the top block might or might not have BBF_GC_SAFE_POINT,
// but we assume it does not have BBF_GC_SAFE_POINT any more.

#define BBF_SPLIT_LOST (BBF_GC_SAFE_POINT | BBF_HAS_JMP | BBF_KEEP_BBJ_ALWAYS)

// Flags gained by the bottom block when a block is split.
// Note, this is a conservative guess.
// For example, the bottom block might or might not have BBF_HAS_NEWARRAY,
// but we assume it has BBF_HAS_NEWARRAY.

// TODO: Should BBF_RUN_RARELY be added to BBF_SPLIT_GAINED ?

#define BBF_SPLIT_GAINED                                                                                               \
    (BBF_DONT_REMOVE | BBF_HAS_LABEL | BBF_HAS_JMP | BBF_BACKWARD_JUMP | BBF_HAS_IDX_LEN | BBF_HAS_NEWARRAY |          \
     BBF_PROF_WEIGHT | BBF_HAS_NEWOBJ | BBF_KEEP_BBJ_ALWAYS)

#ifndef __GNUC__ // GCC doesn't like C_ASSERT at global scope
    static_assert_no_msg((BBF_SPLIT_NONEXIST & BBF_SPLIT_LOST) == 0);
    static_assert_no_msg((BBF_SPLIT_NONEXIST & BBF_SPLIT_GAINED) == 0);
#endif

#ifdef DEBUG
    void     dspFlags();                   // Print the flags
    unsigned dspCheapPreds();              // Print the predecessors (bbCheapPreds)
    unsigned dspPreds();                   // Print the predecessors (bbPreds)
    unsigned dspSuccs(Compiler* compiler); // Print the successors. The 'compiler' argument determines whether EH
                                           // regions are printed: see NumSucc() for details.
    void dspJumpKind();                    // Print the block jump kind (e.g., BBJ_NONE, BBJ_COND, etc.).
    void dspBlockHeader(Compiler* compiler,
                        bool      showKind  = true,
                        bool      showFlags = false,
                        bool showPreds = true); // Print a simple basic block header for various output, including a
                                                // list of predecessors and successors.
#endif                                          // DEBUG

    typedef unsigned weight_t; // Type used to hold block and edge weights
                               // Note that for CLR v2.0 and earlier our
                               // block weights were stored using unsigned shorts

#define BB_UNITY_WEIGHT 100 // how much a normal execute once block weights
#define BB_LOOP_WEIGHT 8    // how much more loops are weighted
#define BB_ZERO_WEIGHT 0
#define BB_MAX_WEIGHT ULONG_MAX // we're using an 'unsigned' for the weight
#define BB_VERY_HOT_WEIGHT 256  // how many average hits a BB has (per BBT scenario run) for this block
                                // to be considered as very hot

    weight_t bbWeight; // The dynamic execution weight of this block

    // getBBWeight -- get the normalized weight of this block
    unsigned getBBWeight(Compiler* comp);

    // setBBWeight -- if the block weight is not derived from a profile, then set the weight to the input
    // weight, but make sure to not overflow BB_MAX_WEIGHT
    void setBBWeight(unsigned weight)
    {
        if (!(this->bbFlags & BBF_PROF_WEIGHT))
        {
            this->bbWeight = min(weight, BB_MAX_WEIGHT);
        }
    }

    // modifyBBWeight -- same as setBBWeight, but also make sure that if the block is rarely run, it stays that
    // way, and if it's not rarely run then its weight never drops below 1.
    void modifyBBWeight(unsigned weight)
    {
        if (this->bbWeight != BB_ZERO_WEIGHT)
        {
            setBBWeight(max(weight, 1));
        }
    }

    // setBBProfileWeight -- Set the profile-derived weight for a basic block
    void setBBProfileWeight(unsigned weight)
    {
        this->bbFlags |= BBF_PROF_WEIGHT;
        // Check if the multiplication by BB_UNITY_WEIGHT will overflow.
        this->bbWeight = (weight <= BB_MAX_WEIGHT / BB_UNITY_WEIGHT) ? weight * BB_UNITY_WEIGHT : BB_MAX_WEIGHT;
    }

    // this block will inherit the same weight and relevant bbFlags as bSrc
    void inheritWeight(BasicBlock* bSrc)
    {
        this->bbWeight = bSrc->bbWeight;

        if (bSrc->bbFlags & BBF_PROF_WEIGHT)
        {
            this->bbFlags |= BBF_PROF_WEIGHT;
        }
        else
        {
            this->bbFlags &= ~BBF_PROF_WEIGHT;
        }

        if (this->bbWeight == 0)
        {
            this->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            this->bbFlags &= ~BBF_RUN_RARELY;
        }
    }

    // Similar to inheritWeight(), but we're splitting a block (such as creating blocks for qmark removal).
    // So, specify a percentage (0 to 99; if it's 100, just use inheritWeight()) of the weight that we're
    // going to inherit. Since the number isn't exact, clear the BBF_PROF_WEIGHT flag.
    void inheritWeightPercentage(BasicBlock* bSrc, unsigned percentage)
    {
        assert(0 <= percentage && percentage < 100);

        // Check for overflow
        if (bSrc->bbWeight * 100 <= bSrc->bbWeight)
        {
            this->bbWeight = bSrc->bbWeight;
        }
        else
        {
            this->bbWeight = bSrc->bbWeight * percentage / 100;
        }

        this->bbFlags &= ~BBF_PROF_WEIGHT;

        if (this->bbWeight == 0)
        {
            this->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            this->bbFlags &= ~BBF_RUN_RARELY;
        }
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

    bool isMaxBBWeight()
    {
        return (bbWeight == BB_MAX_WEIGHT);
    }

    // Returns "true" if the block is empty. Empty here means there are no statement
    // trees *except* PHI definitions.
    bool isEmpty();

    // Returns "true" iff "this" is the first block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair --
    // a block corresponding to an exit from the try of a try/finally.  In the flow graph,
    // this becomes a block that calls the finally, and a second, immediately
    // following empty block (in the bbNext chain) to which the finally will return, and which
    // branches unconditionally to the next block to be executed outside the try/finally.
    // Note that code is often generated differently than this description. For example, on ARM,
    // the target of the BBJ_ALWAYS is loaded in LR (the return register), and a direct jump is
    // made to the 'finally'. The effect is that the 'finally' returns directly to the target of
    // the BBJ_ALWAYS. A "retless" BBJ_CALLFINALLY is one that has no corresponding BBJ_ALWAYS.
    // This can happen if the finally is known to not return (e.g., it contains a 'throw'). In
    // that case, the BBJ_CALLFINALLY flags has BBF_RETLESS_CALL set. Note that ARM never has
    // "retless" BBJ_CALLFINALLY blocks due to a requirement to use the BBJ_ALWAYS for
    // generating code.
    bool isBBCallAlwaysPair()
    {
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
        if (this->bbJumpKind == BBJ_CALLFINALLY)
#else
        if ((this->bbJumpKind == BBJ_CALLFINALLY) && !(this->bbFlags & BBF_RETLESS_CALL))
#endif
        {
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
            // On ARM, there are no retless BBJ_CALLFINALLY.
            assert(!(this->bbFlags & BBF_RETLESS_CALL));
#endif
            // Some asserts that the next block is a BBJ_ALWAYS of the proper form.
            assert(this->bbNext != nullptr);
            assert(this->bbNext->bbJumpKind == BBJ_ALWAYS);
            assert(this->bbNext->bbFlags & BBF_KEEP_BBJ_ALWAYS);
            assert(this->bbNext->isEmpty());

            return true;
        }
        else
        {
            return false;
        }
    }

    BBjumpKinds bbJumpKind; // jump (if any) at the end of this block

    /* The following union describes the jump target(s) of this block */
    union {
        unsigned    bbJumpOffs; // PC offset (temporary only)
        BasicBlock* bbJumpDest; // basic block
        BBswtDesc*  bbJumpSwt;  // switch descriptor
    };

    // NumSucc() gives the number of successors, and GetSucc() allows one to iterate over them.
    //
    // The behavior of both for blocks that end in BBJ_EHFINALLYRET (a return from a finally or fault block)
    // depends on whether "comp" is non-null. If it is null, then the block is considered to have no
    // successor. If it is non-null, we figure out the actual successors. Some cases will want one behavior,
    // other cases the other.  For example, IL verification requires that these blocks end in an empty operand
    // stack, and since the dataflow analysis of IL verification is concerned only with the contents of the
    // operand stack, we can consider the finally block to have no successors. But a more general dataflow
    // analysis that is tracking the contents of local variables might want to consider *all* successors,
    // and would pass the current Compiler object.
    //
    // Similarly, BBJ_EHFILTERRET blocks are assumed to have no successors if "comp" is null; if non-null,
    // NumSucc/GetSucc yields the first block of the try blocks handler.
    //
    // Also, the behavior for switches changes depending on the value of "comp". If it is null, then all
    // switch successors are returned. If it is non-null, then only unique switch successors are returned;
    // the duplicate successors are omitted.
    //
    // Note that for BBJ_COND, which has two successors (fall through and condition true branch target),
    // only the unique targets are returned. Thus, if both targets are the same, NumSucc() will only return 1
    // instead of 2.
    //
    // Returns the number of successors of "this".
    unsigned NumSucc(Compiler* comp = nullptr);

    // Returns the "i"th successor.  Requires (0 <= i < NumSucc()).
    BasicBlock* GetSucc(unsigned i, Compiler* comp = nullptr);

    BasicBlock* GetUniquePred(Compiler* comp);

    BasicBlock* GetUniqueSucc();

    unsigned countOfInEdges() const
    {
        return bbRefs;
    }

    __declspec(property(get = getBBTreeList, put = setBBTreeList)) GenTree* bbTreeList; // the body of the block.

    GenTree* getBBTreeList() const
    {
        return m_firstNode;
    }

    void setBBTreeList(GenTree* tree)
    {
        m_firstNode = tree;
    }

    EntryState* bbEntryState; // verifier tracked state of all entries in stack.

#define NO_BASE_TMP UINT_MAX // base# to use when we have none
    unsigned bbStkTempsIn;   // base# for input stack temps
    unsigned bbStkTempsOut;  // base# for output stack temps

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

// Some non-zero value that will not collide with real tokens for bbCatchTyp
#define BBCT_NONE 0x00000000
#define BBCT_FAULT 0xFFFFFFFC
#define BBCT_FINALLY 0xFFFFFFFD
#define BBCT_FILTER 0xFFFFFFFE
#define BBCT_FILTER_HANDLER 0xFFFFFFFF
#define handlerGetsXcptnObj(hndTyp) ((hndTyp) != BBCT_NONE && (hndTyp) != BBCT_FAULT && (hndTyp) != BBCT_FINALLY)

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

    BlockSet    bbReach; // Set of all blocks that can reach this one
    BasicBlock* bbIDom;  // Represent the closest dominator to this block (called the Immediate
                         // Dominator) used to compute the dominance tree.
    unsigned bbDfsNum;   // The index of this block in DFS reverse post order
                         // relative to the flow graph.

#if ASSERTION_PROP
    // A set of blocks which dominate this one *except* the normal entry block. This is lazily initialized
    // and used only by Assertion Prop, intersected with fgEnterBlks!
    BlockSet bbDoms;
#endif

    IL_OFFSET bbCodeOffs;    // IL offset of the beginning of the block
    IL_OFFSET bbCodeOffsEnd; // IL offset past the end of the block. Thus, the [bbCodeOffs..bbCodeOffsEnd)
                             // range is not inclusive of the end offset. The count of IL bytes in the block
                             // is bbCodeOffsEnd - bbCodeOffs, assuming neither are BAD_IL_OFFSET.

#ifdef DEBUG
    void dspBlockILRange(); // Display the block's IL range as [XXX...YYY), where XXX and YYY might be "???" for
                            // BAD_IL_OFFSET.
#endif                      // DEBUG

    VARSET_TP bbVarUse; // variables used     by block (before an assignment)
    VARSET_TP bbVarDef; // variables assigned by block (before a use)
    VARSET_TP bbVarTmp; // TEMP: only used by FP enregistering code!

    VARSET_TP bbLiveIn;  // variables live on entry
    VARSET_TP bbLiveOut; // variables live on exit

    // Use, def, live in/out information for the implicit "Heap" variable.
    unsigned bbHeapUse : 1;
    unsigned bbHeapDef : 1;
    unsigned bbHeapLiveIn : 1;
    unsigned bbHeapLiveOut : 1;
    unsigned bbHeapHavoc : 1; // If true, at some point the block does an operation that leaves the heap
                              // in an unknown state. (E.g., unanalyzed call, store through unknown
                              // pointer...)

    // We want to make phi functions for the special implicit var "Heap".  But since this is not a real
    // lclVar, and thus has no local #, we can't use a GenTreePhiArg.  Instead, we use this struct.
    struct HeapPhiArg
    {
        bool m_isSsaNum; // If true, the phi arg is an SSA # for an internal try block heap state, being
                         // added to the phi of a catch block.  If false, it's a pred block.
        union {
            BasicBlock* m_predBB; // Predecessor block from which the SSA # flows.
            unsigned    m_ssaNum; // SSA# for internal block heap state.
        };
        HeapPhiArg* m_nextArg; // Next arg in the list, else NULL.

        unsigned GetSsaNum()
        {
            if (m_isSsaNum)
            {
                return m_ssaNum;
            }
            else
            {
                assert(m_predBB != nullptr);
                return m_predBB->bbHeapSsaNumOut;
            }
        }

        HeapPhiArg(BasicBlock* predBB, HeapPhiArg* nextArg = nullptr)
            : m_isSsaNum(false), m_predBB(predBB), m_nextArg(nextArg)
        {
        }
        HeapPhiArg(unsigned ssaNum, HeapPhiArg* nextArg = nullptr)
            : m_isSsaNum(true), m_ssaNum(ssaNum), m_nextArg(nextArg)
        {
        }

        void* operator new(size_t sz, class Compiler* comp);
    };
    static HeapPhiArg* EmptyHeapPhiDef; // Special value (0x1, FWIW) to represent a to-be-filled in Phi arg list
                                        // for Heap.
    HeapPhiArg* bbHeapSsaPhiFunc;       // If the "in" Heap SSA var is not a phi definition, this value is NULL.
                                        // Otherwise, it is either the special value EmptyHeapPhiDefn, to indicate
                                        // that Heap needs a phi definition on entry, or else it is the linked list
                                        // of the phi arguments.
    unsigned bbHeapSsaNumIn;            // The SSA # of "Heap" on entry to the block.
    unsigned bbHeapSsaNumOut;           // The SSA # of "Heap" on exit from the block.

#ifdef DEBUGGING_SUPPORT
    VARSET_TP bbScope; // variables in scope over the block
#endif

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
#if ASSERTION_PROP
        ASSERT_TP bbAssertionKill; // value assignments killed   by block
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

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    void* bbUnwindNopEmitCookie;
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

#ifdef VERIFIER
    stackDesc bbStackIn;  // stack descriptor for  input
    stackDesc bbStackOut; // stack descriptor for output

    verTypeVal* bbTypesIn;  // list of variable types on  input
    verTypeVal* bbTypesOut; // list of variable types on output
#endif                      // VERIFIER

#if FEATURE_STACK_FP_X87
    FlatFPStateX87* bbFPStateX87; // State of FP stack on entry to the basic block
#endif                            // FEATURE_STACK_FP_X87

    /* The following fields used for loop detection */

    typedef unsigned char loopNumber;
    static const unsigned NOT_IN_LOOP = UCHAR_MAX;

#ifdef DEBUG
    // This is the label a loop gets as part of the second, reachability-based
    // loop discovery mechanism.  This is apparently only used for debugging.
    // We hope we'll eventually just have one loop-discovery mechanism, and this will go away.
    loopNumber bbLoopNum; // set to 'n' for a loop #n header
#endif                    // DEBUG

    loopNumber bbNatLoopNum; // Index, in optLoopTable, of most-nested loop that contains this block,
                             // or else NOT_IN_LOOP if this block is not in a loop.

#define MAX_LOOP_NUM 16       // we're using a 'short' for the mask
#define LOOP_MASK_TP unsigned // must be big enough for a mask

//-------------------------------------------------------------------------

#if MEASURE_BLOCK_SIZE
    static size_t s_Size;
    static size_t s_Count;
#endif // MEASURE_BLOCK_SIZE

    bool bbFallsThrough();

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

    unsigned bbStmtNum; // The statement number of the first stmt in this block

    // This is used in integrity checks.  We semi-randomly pick a traversal stamp, label all blocks
    // in the BB list with that stamp (in this field); then we can tell if (e.g.) predecessors are
    // still in the BB list by whether they have the same stamp (with high probability).
    unsigned bbTraversalStamp;
#endif // DEBUG

    ThisInitState bbThisOnEntry();
    unsigned      bbStackDepthOnEntry();
    void bbSetStack(void* stackBuffer);
    StackEntry* bbStackOnEntry();
    void        bbSetRunRarely();

    // "bbNum" is one-based (for unknown reasons); it is sometimes useful to have the corresponding
    // zero-based number for use as an array index.
    unsigned bbInd()
    {
        assert(bbNum > 0);
        return bbNum - 1;
    }

    GenTreeStmt* firstStmt();
    GenTreeStmt* lastStmt();
    GenTreeStmt* lastTopLevelStmt();

    GenTree* firstNode();
    GenTree* lastNode();

    bool containsStatement(GenTree* statement);

    bool endsWithJmpMethod(Compiler* comp);

    bool endsWithTailCall(Compiler* comp,
                          bool      fastTailCallsOnly,
                          bool      tailCallsConvertibleToLoopOnly,
                          GenTree** tailCall);

    bool endsWithTailCallOrJmp(Compiler* comp, bool fastTailCallsOnly = false);

    bool endsWithTailCallConvertibleToLoop(Compiler* comp, GenTree** tailCall);

    // Returns the first statement in the statement list of "this" that is
    // not an SSA definition (a lcl = phi(...) assignment).
    GenTreeStmt* FirstNonPhiDef();
    GenTree*     FirstNonPhiDefOrCatchArgAsg();

    BasicBlock()
        :
#if ASSERTION_PROP
        BLOCKSET_INIT_NOCOPY(bbDoms, BlockSetOps::UninitVal())
        ,
#endif // ASSERTION_PROP
        VARSET_INIT_NOCOPY(bbLiveIn, VarSetOps::UninitVal())
        , VARSET_INIT_NOCOPY(bbLiveOut, VarSetOps::UninitVal())
    {
    }

private:
    EHSuccessorIter StartEHSuccs(Compiler* comp)
    {
        return EHSuccessorIter(comp, this);
    }
    EHSuccessorIter EndEHSuccs()
    {
        return EHSuccessorIter();
    }

    friend struct EHSuccs;

    AllSuccessorIter StartAllSuccs(Compiler* comp)
    {
        return AllSuccessorIter(comp, this);
    }
    AllSuccessorIter EndAllSuccs(Compiler* comp)
    {
        return AllSuccessorIter(NumSucc(comp));
    }

    friend struct AllSuccs;

public:
    // Iteratable collection of the EH successors of a block.
    class EHSuccs
    {
        Compiler*   m_comp;
        BasicBlock* m_block;

    public:
        EHSuccs(Compiler* comp, BasicBlock* block) : m_comp(comp), m_block(block)
        {
        }

        EHSuccessorIter begin()
        {
            return m_block->StartEHSuccs(m_comp);
        }
        EHSuccessorIter end()
        {
            return EHSuccessorIter();
        }
    };

    EHSuccs GetEHSuccs(Compiler* comp)
    {
        return EHSuccs(comp, this);
    }

    class AllSuccs
    {
        Compiler*   m_comp;
        BasicBlock* m_block;

    public:
        AllSuccs(Compiler* comp, BasicBlock* block) : m_comp(comp), m_block(block)
        {
        }

        AllSuccessorIter begin()
        {
            return m_block->StartAllSuccs(m_comp);
        }
        AllSuccessorIter end()
        {
            return AllSuccessorIter(m_block->NumSucc(m_comp));
        }
    };

    AllSuccs GetAllSuccs(Compiler* comp)
    {
        return AllSuccs(comp, this);
    }

    // Clone block state and statements from 'from' block to 'to' block.
    // Assumes that "to" is an empty block.
    static void CloneBlockState(Compiler* compiler, BasicBlock* to, const BasicBlock* from);

    void MakeLIR(GenTree* firstNode, GenTree* lastNode);
    bool IsLIR();
};

template <>
struct PtrKeyFuncs<BasicBlock> : public KeyFuncsDefEquals<const BasicBlock*>
{
public:
    // Make sure hashing is deterministic and not on "ptr."
    static unsigned GetHashCode(const BasicBlock* ptr);
};

// A set of blocks.
typedef SimplerHashTable<BasicBlock*, PtrKeyFuncs<BasicBlock>, bool, JitSimplerHashBehavior> BlkSet;

// A map of block -> set of blocks, can be used as sparse block trees.
typedef SimplerHashTable<BasicBlock*, PtrKeyFuncs<BasicBlock>, BlkSet*, JitSimplerHashBehavior> BlkToBlkSetMap;

// Map from Block to Block.  Used for a variety of purposes.
typedef SimplerHashTable<BasicBlock*, PtrKeyFuncs<BasicBlock>, BasicBlock*, JitSimplerHashBehavior> BlockToBlockMap;

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

struct flowList
{
    flowList*   flNext;  // The next BasicBlock in the list, nullptr for end of list.
    BasicBlock* flBlock; // The BasicBlock of interest.

    BasicBlock::weight_t flEdgeWeightMin;
    BasicBlock::weight_t flEdgeWeightMax;

    unsigned flDupCount; // The count of duplicate "edges" (use only for switch stmts)

    // These two methods are used to set new values for flEdgeWeightMin and flEdgeWeightMax
    // they are used only during the computation of the edge weights
    // They return false if the newWeight is not between the current [min..max]
    // when slop is non-zero we allow for the case where our weights might be off by 'slop'
    //
    bool setEdgeWeightMinChecked(BasicBlock::weight_t newWeight, BasicBlock::weight_t slop, bool* wbUsedSlop);
    bool setEdgeWeightMaxChecked(BasicBlock::weight_t newWeight, BasicBlock::weight_t slop, bool* wbUsedSlop);

    flowList() : flNext(nullptr), flBlock(nullptr), flEdgeWeightMin(0), flEdgeWeightMax(0), flDupCount(0)
    {
    }

    flowList(BasicBlock* blk, flowList* rest)
        : flNext(rest), flBlock(blk), flEdgeWeightMin(0), flEdgeWeightMax(0), flDupCount(0)
    {
    }
};

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

    DfsBlockEntry() : dfsStackState(DSS_Invalid), dfsBlock(nullptr)
    {
    }

    DfsBlockEntry(DfsStackState state, BasicBlock* basicBlock) : dfsStackState(state), dfsBlock(basicBlock)
    {
    }
};

struct DfsNumEntry
{
    DfsStackState dfsStackState; // The pre/post traversal action for this entry
    unsigned      dfsNum;        // The corresponding block number for the action

    DfsNumEntry() : dfsStackState(DSS_Invalid), dfsNum(0)
    {
    }

    DfsNumEntry(DfsStackState state, unsigned bbNum) : dfsStackState(state), dfsNum(bbNum)
    {
    }
};

/*****************************************************************************/

extern BasicBlock* __cdecl verAllocBasicBlock();

#ifdef DEBUG
extern void __cdecl verDispBasicBlocks();
#endif

/*****************************************************************************
 *
 *  The following call-backs supplied by the client; it's used by the code
 *  emitter to convert a basic block to its corresponding emitter cookie.
 */

void* emitCodeGetCookie(BasicBlock* block);

AllSuccessorIter::AllSuccessorIter(Compiler* comp, BasicBlock* block)
    : m_comp(comp), m_blk(block), m_normSucc(0), m_numNormSuccs(block->NumSucc(comp)), m_ehIter(comp, block)
{
    if (CurTryIsBlkCallFinallyTarget())
    {
        ++m_ehIter;
    }
}

bool AllSuccessorIter::CurTryIsBlkCallFinallyTarget()
{
    return (m_blk->bbJumpKind == BBJ_CALLFINALLY) && (m_ehIter != EHSuccessorIter()) &&
           (m_blk->bbJumpDest == (*m_ehIter));
}

void AllSuccessorIter::operator++(void)
{
    if (m_normSucc < m_numNormSuccs)
    {
        m_normSucc++;
    }
    else
    {
        ++m_ehIter;

        // If the original block whose successors we're iterating over
        // is a BBJ_CALLFINALLY, that finally clause's first block
        // will be yielded as a normal successor.  Don't also yield as
        // an exceptional successor.
        if (CurTryIsBlkCallFinallyTarget())
        {
            ++m_ehIter;
        }
    }
}

// Requires that "this" is not equal to the standard "end" iterator.  Returns the
// current successor.
BasicBlock* AllSuccessorIter::operator*()
{
    if (m_normSucc < m_numNormSuccs)
    {
        return m_blk->GetSucc(m_normSucc, m_comp);
    }
    else
    {
        return *m_ehIter;
    }
}
/*****************************************************************************/
#endif // _BLOCK_H_
/*****************************************************************************/
