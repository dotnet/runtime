
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************/

#ifndef _LSRA_H_
#define _LSRA_H_

#include "arraylist.h"
#include "smallhash.h"

// Minor and forward-reference types
class Interval;
class RefPosition;
class LinearScan;
class RegRecord;

template <class T>
class ArrayStack;

// LsraLocation tracks the linearized order of the nodes.
// Each node is assigned two LsraLocations - one for all the uses and all but the last
// def, and a second location for the last def (if any)

typedef unsigned int LsraLocation;
const unsigned int   MinLocation = 0;
const unsigned int   MaxLocation = UINT_MAX;
// max number of registers an operation could require internally (in addition to uses and defs)
const unsigned int MaxInternalRegisters = 8;
const unsigned int RegisterTypeCount    = 2;

/*****************************************************************************
* Register types
*****************************************************************************/
typedef var_types RegisterType;

#define IntRegisterType TYP_INT
#define FloatRegisterType TYP_FLOAT
#define MaskRegisterType TYP_MASK

//------------------------------------------------------------------------
// regType: Return the RegisterType to use for a given type
//
// Arguments:
//    type - the type of interest
//
template <class T>
RegisterType regType(T type)
{
    if (varTypeUsesIntReg(type))
    {
        return IntRegisterType;
    }
#if defined(TARGET_XARCH) && defined(FEATURE_SIMD)
    else if (varTypeUsesMaskReg(type))
    {
        return MaskRegisterType;
    }
#endif // TARGET_XARCH && FEATURE_SIMD
    else
    {
        assert(varTypeUsesFloatReg(type));
        return FloatRegisterType;
    }
}

//------------------------------------------------------------------------
// useFloatReg: Check if the given var_type should be allocated to a FloatRegisterType
//
inline bool useFloatReg(var_types type)
{
    return (regType(type) == FloatRegisterType);
}

//------------------------------------------------------------------------
// RefInfo: Captures the necessary information for a definition that is "in-flight"
//          during `buildIntervals` (i.e. a tree-node definition has been encountered,
//          but not its use). This includes the RefPosition and its associated
//          GenTree node.
//
struct RefInfo
{
    RefPosition* ref;
    GenTree*     treeNode;

    RefInfo(RefPosition* r, GenTree* t) : ref(r), treeNode(t)
    {
    }

    // default constructor for data structures
    RefInfo()
    {
    }
};

//------------------------------------------------------------------------
// RefInfoListNode: used to store a single `RefInfo` value for a
//                  node during `buildIntervals`.
//
// This is the node type for `RefInfoList` below.
//
class RefInfoListNode final : public RefInfo
{
    friend class RefInfoList;
    friend class RefInfoListNodePool;

    RefInfoListNode* m_next; // The next node in the list

public:
    RefInfoListNode(RefPosition* r, GenTree* t) : RefInfo(r, t)
    {
    }

    //------------------------------------------------------------------------
    // RefInfoListNode::Next: Returns the next node in the list.
    RefInfoListNode* Next() const
    {
        return m_next;
    }
};

//------------------------------------------------------------------------
// RefInfoList: used to store a list of `RefInfo` values for a
//                   node during `buildIntervals`.
//
// This list of 'RefInfoListNode's contains the source nodes consumed by
// a node, and is created by 'BuildNode'.
//
class RefInfoList final
{
    friend class RefInfoListNodePool;

    RefInfoListNode* m_head; // The head of the list
    RefInfoListNode* m_tail; // The tail of the list

public:
    RefInfoList() : m_head(nullptr), m_tail(nullptr)
    {
    }

    RefInfoList(RefInfoListNode* node) : m_head(node), m_tail(node)
    {
        assert(m_head->m_next == nullptr);
    }

    //------------------------------------------------------------------------
    // RefInfoList::IsEmpty: Returns true if the list is empty.
    //
    bool IsEmpty() const
    {
        return m_head == nullptr;
    }

    //------------------------------------------------------------------------
    // RefInfoList::Begin: Returns the first node in the list.
    //
    RefInfoListNode* Begin() const
    {
        return m_head;
    }

    //------------------------------------------------------------------------
    // RefInfoList::End: Returns the position after the last node in the
    //                        list. The returned value is suitable for use as
    //                        a sentinel for iteration.
    //
    RefInfoListNode* End() const
    {
        return nullptr;
    }

    //------------------------------------------------------------------------
    // RefInfoList::End: Returns the position after the last node in the
    //                        list. The returned value is suitable for use as
    //                        a sentinel for iteration.
    //
    RefInfoListNode* Last() const
    {
        return m_tail;
    }

    //------------------------------------------------------------------------
    // RefInfoList::Append: Appends a node to the list.
    //
    // Arguments:
    //    node - The node to append. Must not be part of an existing list.
    //
    void Append(RefInfoListNode* node)
    {
        assert(node->m_next == nullptr);

        if (m_tail == nullptr)
        {
            assert(m_head == nullptr);
            m_head = node;
        }
        else
        {
            m_tail->m_next = node;
        }

        m_tail = node;
    }
    //------------------------------------------------------------------------
    // RefInfoList::Append: Appends another list to this list.
    //
    // Arguments:
    //    other - The list to append.
    //
    void Append(RefInfoList other)
    {
        if (m_tail == nullptr)
        {
            assert(m_head == nullptr);
            m_head = other.m_head;
        }
        else
        {
            m_tail->m_next = other.m_head;
        }

        m_tail = other.m_tail;
    }

    //------------------------------------------------------------------------
    // RefInfoList::Prepend: Prepends a node to the list.
    //
    // Arguments:
    //    node - The node to prepend. Must not be part of an existing list.
    //
    void Prepend(RefInfoListNode* node)
    {
        assert(node->m_next == nullptr);

        if (m_head == nullptr)
        {
            assert(m_tail == nullptr);
            m_tail = node;
        }
        else
        {
            node->m_next = m_head;
        }

        m_head = node;
    }

    //------------------------------------------------------------------------
    // RefInfoList::Add: Adds a node to the list.
    //
    // Arguments:
    //    node    - The node to add. Must not be part of an existing list.
    //    prepend - True if it should be prepended (otherwise is appended)
    //
    void Add(RefInfoListNode* node, bool prepend)
    {
        if (prepend)
        {
            Prepend(node);
        }
        else
        {
            Append(node);
        }
    }

    //------------------------------------------------------------------------
    // removeListNode - retrieve the RefInfo for the given node
    //
    // Notes:
    //     The BuildNode methods use this helper to retrieve the RefInfo for child nodes
    //     from the useList being constructed.
    //
    RefInfoListNode* removeListNode(RefInfoListNode* listNode, RefInfoListNode* prevListNode)
    {
        RefInfoListNode* nextNode = listNode->Next();
        if (prevListNode == nullptr)
        {
            m_head = nextNode;
        }
        else
        {
            prevListNode->m_next = nextNode;
        }
        if (nextNode == nullptr)
        {
            m_tail = prevListNode;
        }
        listNode->m_next = nullptr;
        return listNode;
    }

    // removeListNode - remove the RefInfoListNode for the given GenTree node from the defList
    RefInfoListNode* removeListNode(GenTree* node);
    // Same as above but takes a multiRegIdx to support multi-reg nodes.
    RefInfoListNode* removeListNode(GenTree* node, unsigned multiRegIdx);

    //------------------------------------------------------------------------
    // GetRefPosition - retrieve the RefPosition for the given node
    //
    // Notes:
    //     The Build methods use this helper to retrieve the RefPosition for child nodes
    //     from the useList being constructed. Note that, if the user knows the order of the operands,
    //     it is expected that they should just retrieve them directly.

    RefPosition* GetRefPosition(GenTree* node)
    {
        for (RefInfoListNode *listNode = Begin(), *end = End(); listNode != end; listNode = listNode->Next())
        {
            if (listNode->treeNode == node)
            {
                return listNode->ref;
            }
        }
        assert(!"GetRefPosition didn't find the node");
        unreached();
    }

    //------------------------------------------------------------------------
    // RefInfoList::GetSecond: Gets the second node in the list.
    //
    // Arguments:
    //    (DEBUG ONLY) treeNode - The GenTree* we expect to be in the second node.
    //
    RefInfoListNode* GetSecond(INDEBUG(GenTree* treeNode))
    {
        noway_assert((Begin() != nullptr) && (Begin()->Next() != nullptr));
        RefInfoListNode* second = Begin()->Next();
        assert(second->treeNode == treeNode);
        return second;
    }

#ifdef DEBUG
    // Count - return the number of nodes in the list (DEBUG only)
    int Count()
    {
        int count = 0;
        for (RefInfoListNode *listNode = Begin(), *end = End(); listNode != end; listNode = listNode->Next())
        {
            count++;
        }
        return count;
    }
#endif // DEBUG
};

//------------------------------------------------------------------------
// RefInfoListNodePool: manages a pool of `RefInfoListNode`
//                      values to decrease overall memory usage
//                      during `buildIntervals`.
//
// `buildIntervals` involves creating a list of RefInfo items per
// node that either directly produces a set of registers or that is a
// contained node with register-producing sources. However, these lists
// are short-lived: they are destroyed once the use of the corresponding
// node is processed. As such, there is typically only a small number of
// `RefInfoListNode` values in use at any given time. Pooling these
// values avoids otherwise frequent allocations.
class RefInfoListNodePool final
{
    RefInfoListNode*      m_freeList;
    Compiler*             m_compiler;
    static const unsigned defaultPreallocation = 8;

public:
    RefInfoListNodePool(Compiler* compiler, unsigned preallocate = defaultPreallocation);
    RefInfoListNode* GetNode(RefPosition* r, GenTree* t);
    void ReturnNode(RefInfoListNode* listNode);
};

#if TRACK_LSRA_STATS
enum LsraStat
{
#define LSRA_STAT_DEF(enum_name, enum_str) enum_name,
#include "lsra_stats.h"
#undef LSRA_STAT_DEF
#define REG_SEL_DEF(enum_name, value, short_str, orderSeqId) STAT_##enum_name,
#define BUSY_REG_SEL_DEF(enum_name, value, short_str, orderSeqId) REG_SEL_DEF(enum_name, value, short_str, orderSeqId)
#include "lsra_score.h"
    COUNT
};
#endif // TRACK_LSRA_STATS

struct LsraBlockInfo
{
    // bbNum of the predecessor to use for the register location of live-in variables.
    // 0 for fgFirstBB.
    unsigned int predBBNum;
    weight_t     weight;
    bool         hasCriticalInEdge : 1;
    bool         hasCriticalOutEdge : 1;
    bool         hasEHBoundaryIn : 1;
    bool         hasEHBoundaryOut : 1;
    bool         hasEHPred : 1;

#if TRACK_LSRA_STATS
    // Per block maintained LSRA statistics.
    unsigned stats[LsraStat::COUNT];
#endif // TRACK_LSRA_STATS
};

enum RegisterScore
{
#define REG_SEL_DEF(enum_name, value, short_str, orderSeqId) enum_name = value,
#define BUSY_REG_SEL_DEF(enum_name, value, short_str, orderSeqId) REG_SEL_DEF(enum_name, value, short_str, orderSeqId)
#include "lsra_score.h"
    NONE = 0
};

// This is sort of a bit mask
// The low order 2 bits will be 1 for defs, and 2 for uses
enum RefType : unsigned char
{
#define DEF_REFTYPE(memberName, memberValue, shortName) memberName = memberValue,
#include "lsra_reftypes.h"
#undef DEF_REFTYPE
};

// position in a block (for resolution)
enum BlockStartOrEnd
{
    BlockPositionStart = 0,
    BlockPositionEnd   = 1,
    PositionCount      = 2
};

inline bool RefTypeIsUse(RefType refType)
{
    return ((refType & RefTypeUse) == RefTypeUse);
}

inline bool RefTypeIsDef(RefType refType)
{
    return ((refType & RefTypeDef) == RefTypeDef);
}

typedef regNumberSmall* VarToRegMap;

typedef jitstd::list<Interval>                      IntervalList;
typedef jitstd::list<RefPosition>                   RefPositionList;
typedef jitstd::list<RefPosition>::iterator         RefPositionIterator;
typedef jitstd::list<RefPosition>::reverse_iterator RefPositionReverseIterator;

class Referenceable
{
public:
    Referenceable()
    {
        firstRefPosition  = nullptr;
        recentRefPosition = nullptr;
        lastRefPosition   = nullptr;
    }

    // A linked list of RefPositions.  These are only traversed in the forward
    // direction, and are not moved, so they don't need to be doubly linked
    // (see RefPosition).

    RefPosition* firstRefPosition;
    RefPosition* recentRefPosition;
    RefPosition* lastRefPosition;

    // Get the position of the next reference which is at or greater than
    // the current location (relies upon recentRefPosition being updated
    // during traversal).
    RefPosition* getNextRefPosition();
    LsraLocation getNextRefLocation();
};

class RegRecord : public Referenceable
{
public:
    RegRecord()
    {
        assignedInterval = nullptr;
        previousInterval = nullptr;
        regNum           = REG_NA;
        isCalleeSave     = false;
        registerType     = IntRegisterType;
    }

    void init(regNumber reg)
    {
#ifdef TARGET_ARM64
        // The Zero register, or the SP
        if ((reg == REG_ZR) || (reg == REG_SP))
        {
            // IsGeneralRegister returns false for REG_ZR and REG_SP
            regNum       = reg;
            registerType = IntRegisterType;
        }
        else
#endif
            if (emitter::isGeneralRegister(reg))
        {
            assert(registerType == IntRegisterType);
        }
        else if (emitter::isFloatReg(reg))
        {
            registerType = FloatRegisterType;
        }
#if defined(TARGET_XARCH) && defined(FEATURE_SIMD)
        else
        {
            assert(emitter::isMaskReg(reg));
            registerType = MaskRegisterType;
        }
#endif
        regNum       = reg;
        isCalleeSave = ((RBM_CALLEE_SAVED & genRegMask(reg)) != 0);
    }

#ifdef DEBUG
    // print out representation
    void dump();
    // concise representation for embedding
    void tinyDump();
#endif // DEBUG

    // DATA

    // interval to which this register is currently allocated.
    // If the interval is inactive (isActive == false) then it is not currently live,
    // and the register can be unassigned (i.e. setting assignedInterval to nullptr)
    // without spilling the register.
    Interval* assignedInterval;
    // Interval to which this register was previously allocated, and which was unassigned
    // because it was inactive.  This register will be reassigned to this Interval when
    // assignedInterval becomes inactive.
    Interval* previousInterval;

    regNumber     regNum;
    bool          isCalleeSave;
    RegisterType  registerType;
    unsigned char regOrder;
};

inline bool leafInRange(GenTree* leaf, int lower, int upper)
{
    if (!leaf->IsIntCnsFitsInI32())
    {
        return false;
    }
    if (leaf->AsIntCon()->gtIconVal < lower)
    {
        return false;
    }
    if (leaf->AsIntCon()->gtIconVal > upper)
    {
        return false;
    }

    return true;
}

inline bool leafInRange(GenTree* leaf, int lower, int upper, int multiple)
{
    if (!leafInRange(leaf, lower, upper))
    {
        return false;
    }
    if (leaf->AsIntCon()->gtIconVal % multiple)
    {
        return false;
    }

    return true;
}

inline bool leafAddInRange(GenTree* leaf, int lower, int upper, int multiple = 1)
{
    if (leaf->OperGet() != GT_ADD)
    {
        return false;
    }
    return leafInRange(leaf->gtGetOp2(), lower, upper, multiple);
}

inline bool isCandidateVar(const LclVarDsc* varDsc)
{
    return varDsc->lvLRACandidate;
}

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           LinearScan                                      XX
XX                                                                           XX
XX This is the container for the Linear Scan data structures and methods.    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
// OPTION 1: The algorithm as described in "Optimized Interval Splitting in a
// Linear Scan Register Allocator".  It is driven by iterating over the Interval
// lists.  In this case, we need multiple IntervalLists, and Intervals will be
// moved between them so they must be easily updated.

// OPTION 2: The algorithm is driven by iterating over the RefPositions.  In this
// case, we only need a single IntervalList, and it won't be updated.
// The RefPosition must refer to its Interval, and we need to be able to traverse
// to the next RefPosition in code order
// THIS IS THE OPTION CURRENTLY BEING PURSUED

class LinearScan : public LinearScanInterface
{
    friend class RefPosition;
    friend class Interval;
    friend class Lowering;

public:
    // This could use further abstraction.  From Compiler we need the tree,
    // the flowgraph and the allocator.
    LinearScan(Compiler* theCompiler);

    // This is the main driver
    virtual PhaseStatus doLinearScan();

    static bool isSingleRegister(regMaskTP regMask)
    {
        return (genExactlyOneBit(regMask));
    }

    // Initialize the block traversal for LSRA.
    // This resets the bbVisitedSet, and on the first invocation sets the blockSequence array,
    // which determines the order in which blocks will be allocated (currently called during Lowering).
    BasicBlock* startBlockSequence();
    // Move to the next block in sequence, updating the current block information.
    BasicBlock* moveToNextBlock();
    // Get the next block to be scheduled without changing the current block,
    // but updating the blockSequence during the first iteration if it is not fully computed.
    BasicBlock* getNextBlock();

    // This is called during code generation to update the location of variables
    virtual void recordVarLocationsAtStartOfBB(BasicBlock* bb);

    // This does the dataflow analysis and builds the intervals
    template <bool localVarsEnregistered>
    void           buildIntervals();

    // This is where the actual assignment is done for scenarios where
    // no local var enregistration is done.
    void allocateRegistersMinimal();

// This is where the actual assignment is done
#ifdef TARGET_ARM64
    template <bool hasConsecutiveRegister = false>
#endif
    void allocateRegisters();
    // This is the resolution phase, where cross-block mismatches are fixed up
    template <bool localVarsEnregistered>
    void           resolveRegisters();

    void writeRegisters(RefPosition* currentRefPosition, GenTree* tree);

    // Insert a copy in the case where a tree node value must be moved to a different
    // register at the point of use, or it is reloaded to a different register
    // than the one it was spilled from
    void insertCopyOrReload(BasicBlock* block, GenTree* tree, unsigned multiRegIdx, RefPosition* refPosition);

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    void makeUpperVectorInterval(unsigned varIndex);
    Interval* getUpperVectorInterval(unsigned varIndex);

    // Save the upper half of a vector that lives in a callee-save register at the point of a call.
    void insertUpperVectorSave(GenTree*     tree,
                               RefPosition* refPosition,
                               Interval*    upperVectorInterval,
                               BasicBlock*  block);
    // Restore the upper half of a vector that's been partially spilled prior to a use in 'tree'.
    void insertUpperVectorRestore(GenTree*     tree,
                                  RefPosition* refPosition,
                                  Interval*    upperVectorInterval,
                                  BasicBlock*  block);
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

    // resolve along one block-block edge
    enum ResolveType
    {
        ResolveSplit,
        ResolveJoin,
        ResolveCritical,
        ResolveSharedCritical,
        ResolveTypeCount
    };
#ifdef DEBUG
    static const char* resolveTypeName[ResolveTypeCount];
#endif

    enum WhereToInsert
    {
        InsertAtTop,
        InsertAtBottom
    };

#ifdef TARGET_ARM
    void addResolutionForDouble(BasicBlock*     block,
                                GenTree*        insertionPoint,
                                Interval**      sourceIntervals,
                                regNumberSmall* location,
                                regNumber       toReg,
                                regNumber       fromReg,
                                ResolveType resolveType DEBUG_ARG(BasicBlock* fromBlock)
                                    DEBUG_ARG(BasicBlock* toBlock));
#endif

    void addResolution(BasicBlock* block,
                       GenTree*    insertionPoint,
                       Interval*   interval,
                       regNumber   outReg,
                       regNumber inReg DEBUG_ARG(BasicBlock* fromBlock) DEBUG_ARG(BasicBlock* toBlock)
                           DEBUG_ARG(const char* reason));

    void handleOutgoingCriticalEdges(BasicBlock* block);

    void resolveEdge(BasicBlock*      fromBlock,
                     BasicBlock*      toBlock,
                     ResolveType      resolveType,
                     VARSET_VALARG_TP liveSet,
                     regMaskTP        terminatorConsumedRegs);

    void resolveEdges();

    // Keep track of how many temp locations we'll need for spill
    void initMaxSpill();
    void updateMaxSpill(RefPosition* refPosition);
    void recordMaxSpill();

    // max simultaneous spill locations used of every type
    unsigned int maxSpill[TYP_COUNT];
    unsigned int currentSpill[TYP_COUNT];
    bool         needFloatTmpForFPCall;
    bool         needDoubleTmpForFPCall;
    bool         needNonIntegerRegisters;

#ifdef DEBUG
private:
    //------------------------------------------------------------------------
    // Should we stress lsra? This uses the DOTNET_JitStressRegs variable.
    //
    // The mask bits are currently divided into fields in which each non-zero value
    // is a distinct stress option (e.g. 0x3 is not a combination of 0x1 and 0x2).
    // However, subject to possible constraints (to be determined), the different
    // fields can be combined (e.g. 0x7 is a combination of 0x3 and 0x4).
    // Note that the field values are declared in a public enum, but the actual bits are
    // only accessed via accessors.

    unsigned lsraStressMask;

    // This controls the registers available for allocation
    enum LsraStressLimitRegs
    {
        LSRA_LIMIT_NONE      = 0,
        LSRA_LIMIT_CALLEE    = 0x1,
        LSRA_LIMIT_CALLER    = 0x2,
        LSRA_LIMIT_SMALL_SET = 0x3,
#if defined(TARGET_AMD64)
        LSRA_LIMIT_UPPER_SIMD_SET = 0x2000,
        LSRA_LIMIT_MASK           = 0x2003
#else
        LSRA_LIMIT_MASK = 0x3
#endif
    };

    // When LSRA_LIMIT_SMALL_SET is specified, it is desirable to select a "mixed" set of caller- and callee-save
    // registers, so as to get different coverage than limiting to callee or caller.
    // At least for x86 and AMD64, and potentially other architecture that will support SIMD,
    // we need a minimum of 5 fp regs in order to support the InitN intrinsic for Vector4.
    // Hence the "SmallFPSet" has 5 elements.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_AMD64)
#ifdef UNIX_AMD64_ABI
    // On System V the RDI and RSI are not callee saved. Use R12 ans R13 as callee saved registers.
    static const regMaskTP LsraLimitSmallIntSet =
        (RBM_EAX | RBM_ECX | RBM_EBX | RBM_ETW_FRAMED_EBP | RBM_R12 | RBM_R13);
#else  // !UNIX_AMD64_ABI
    // On Windows Amd64 use the RDI and RSI as callee saved registers.
    static const regMaskTP LsraLimitSmallIntSet =
        (RBM_EAX | RBM_ECX | RBM_EBX | RBM_ETW_FRAMED_EBP | RBM_ESI | RBM_EDI);
#endif // !UNIX_AMD64_ABI
    static const regMaskTP LsraLimitSmallFPSet = (RBM_XMM0 | RBM_XMM1 | RBM_XMM2 | RBM_XMM6 | RBM_XMM7);
    static const regMaskTP LsraLimitUpperSimdSet =
        (RBM_XMM16 | RBM_XMM17 | RBM_XMM18 | RBM_XMM19 | RBM_XMM20 | RBM_XMM21 | RBM_XMM22 | RBM_XMM23 | RBM_XMM24 |
         RBM_XMM25 | RBM_XMM26 | RBM_XMM27 | RBM_XMM28 | RBM_XMM29 | RBM_XMM30 | RBM_XMM31);
#elif defined(TARGET_ARM)
    // On ARM, we may need two registers to set up the target register for a virtual call, so we need
    // to have at least the maximum number of arg registers, plus 2.
    static const regMaskTP LsraLimitSmallIntSet = (RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4 | RBM_R5);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_F0 | RBM_F1 | RBM_F2 | RBM_F16 | RBM_F17);
#elif defined(TARGET_ARM64)
    static const regMaskTP LsraLimitSmallIntSet = (RBM_R0 | RBM_R1 | RBM_R2 | RBM_R19 | RBM_R20);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_V0 | RBM_V1 | RBM_V2 | RBM_V8 | RBM_V9);
#elif defined(TARGET_X86)
    static const regMaskTP LsraLimitSmallIntSet = (RBM_EAX | RBM_ECX | RBM_EDI);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_XMM0 | RBM_XMM1 | RBM_XMM2 | RBM_XMM6 | RBM_XMM7);
#elif defined(TARGET_LOONGARCH64)
    static const regMaskTP LsraLimitSmallIntSet = (RBM_T1 | RBM_T3 | RBM_A0 | RBM_A1 | RBM_T0);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_F0 | RBM_F1 | RBM_F2 | RBM_F8 | RBM_F9);
#elif defined(TARGET_RISCV64)
    static const regMaskTP LsraLimitSmallIntSet = (RBM_T1 | RBM_T3 | RBM_A0 | RBM_A1 | RBM_T0);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_F0 | RBM_F1 | RBM_F2 | RBM_F8 | RBM_F9);
#else
#error Unsupported or unset target architecture
#endif // target

    LsraStressLimitRegs getStressLimitRegs()
    {
        return (LsraStressLimitRegs)(lsraStressMask & LSRA_LIMIT_MASK);
    }

    regMaskTP getConstrainedRegMask(RefPosition* refPosition,
                                    regMaskTP    regMaskActual,
                                    regMaskTP    regMaskConstrain,
                                    unsigned     minRegCount);
    regMaskTP stressLimitRegs(RefPosition* refPosition, regMaskTP mask);

    // This controls the heuristics used to select registers
    // These can be combined.
    enum LsraSelect{LSRA_SELECT_DEFAULT = 0, LSRA_SELECT_REVERSE_HEURISTICS = 0x04,
                    LSRA_SELECT_REVERSE_CALLER_CALLEE = 0x08, LSRA_SELECT_NEAREST = 0x10, LSRA_SELECT_MASK = 0x1c};
    LsraSelect getSelectionHeuristics()
    {
        return (LsraSelect)(lsraStressMask & LSRA_SELECT_MASK);
    }
    bool doReverseSelect()
    {
        return ((lsraStressMask & LSRA_SELECT_REVERSE_HEURISTICS) != 0);
    }
    bool doReverseCallerCallee()
    {
        return ((lsraStressMask & LSRA_SELECT_REVERSE_CALLER_CALLEE) != 0);
    }
    bool doSelectNearest()
    {
        return ((lsraStressMask & LSRA_SELECT_NEAREST) != 0);
    }

    // This controls the order in which basic blocks are visited during allocation
    enum LsraTraversalOrder{LSRA_TRAVERSE_LAYOUT = 0x20, LSRA_TRAVERSE_PRED_FIRST = 0x40,
                            LSRA_TRAVERSE_RANDOM  = 0x60, // NYI
                            LSRA_TRAVERSE_DEFAULT = LSRA_TRAVERSE_PRED_FIRST, LSRA_TRAVERSE_MASK = 0x60};
    LsraTraversalOrder getLsraTraversalOrder()
    {
        if ((lsraStressMask & LSRA_TRAVERSE_MASK) == 0)
        {
            return LSRA_TRAVERSE_DEFAULT;
        }
        return (LsraTraversalOrder)(lsraStressMask & LSRA_TRAVERSE_MASK);
    }
    bool isTraversalLayoutOrder()
    {
        return getLsraTraversalOrder() == LSRA_TRAVERSE_LAYOUT;
    }
    bool isTraversalPredFirstOrder()
    {
        return getLsraTraversalOrder() == LSRA_TRAVERSE_PRED_FIRST;
    }

    // This controls whether lifetimes should be extended to the entire method.
    // Note that this has no effect under MinOpts
    enum LsraExtendLifetimes{LSRA_DONT_EXTEND = 0, LSRA_EXTEND_LIFETIMES = 0x80, LSRA_EXTEND_LIFETIMES_MASK = 0x80};
    LsraExtendLifetimes getLsraExtendLifeTimes()
    {
        return (LsraExtendLifetimes)(lsraStressMask & LSRA_EXTEND_LIFETIMES_MASK);
    }
    bool extendLifetimes()
    {
        return getLsraExtendLifeTimes() == LSRA_EXTEND_LIFETIMES;
    }

    // This controls whether variables locations should be set to the previous block in layout order
    // (LSRA_BLOCK_BOUNDARY_LAYOUT), or to that of the highest-weight predecessor (LSRA_BLOCK_BOUNDARY_PRED -
    // the default), or rotated (LSRA_BLOCK_BOUNDARY_ROTATE).
    enum LsraBlockBoundaryLocations{LSRA_BLOCK_BOUNDARY_PRED = 0, LSRA_BLOCK_BOUNDARY_LAYOUT = 0x100,
                                    LSRA_BLOCK_BOUNDARY_ROTATE = 0x200, LSRA_BLOCK_BOUNDARY_MASK = 0x300};
    LsraBlockBoundaryLocations getLsraBlockBoundaryLocations()
    {
        return (LsraBlockBoundaryLocations)(lsraStressMask & LSRA_BLOCK_BOUNDARY_MASK);
    }
    regNumber rotateBlockStartLocation(Interval* interval, regNumber targetReg, regMaskTP availableRegs);

    // This controls whether we always insert a GT_RELOAD instruction after a spill
    // Note that this can be combined with LSRA_SPILL_ALWAYS (or not)
    enum LsraReload{LSRA_NO_RELOAD_IF_SAME = 0, LSRA_ALWAYS_INSERT_RELOAD = 0x400, LSRA_RELOAD_MASK = 0x400};
    LsraReload getLsraReload()
    {
        return (LsraReload)(lsraStressMask & LSRA_RELOAD_MASK);
    }
    bool alwaysInsertReload()
    {
        return getLsraReload() == LSRA_ALWAYS_INSERT_RELOAD;
    }

    // This controls whether we spill everywhere
    enum LsraSpill{LSRA_DONT_SPILL_ALWAYS = 0, LSRA_SPILL_ALWAYS = 0x800, LSRA_SPILL_MASK = 0x800};
    LsraSpill getLsraSpill()
    {
        return (LsraSpill)(lsraStressMask & LSRA_SPILL_MASK);
    }
    bool spillAlways()
    {
        return getLsraSpill() == LSRA_SPILL_ALWAYS;
    }

    // This controls whether RefPositions that lower/codegen indicated as reg optional be
    // allocated a reg at all.
    enum LsraRegOptionalControl{LSRA_REG_OPTIONAL_DEFAULT = 0, LSRA_REG_OPTIONAL_NO_ALLOC = 0x1000,
                                LSRA_REG_OPTIONAL_MASK = 0x1000};

    LsraRegOptionalControl getLsraRegOptionalControl()
    {
        return (LsraRegOptionalControl)(lsraStressMask & LSRA_REG_OPTIONAL_MASK);
    }

    bool regOptionalNoAlloc()
    {
        return getLsraRegOptionalControl() == LSRA_REG_OPTIONAL_NO_ALLOC;
    }

    bool candidatesAreStressLimited()
    {
        return ((lsraStressMask & (LSRA_LIMIT_MASK | LSRA_SELECT_MASK)) != 0);
    }

    // Dump support
    void dumpDefList();
    void lsraDumpIntervals(const char* msg);
    void dumpRefPositions(const char* msg);
    void dumpVarRefPositions(const char* msg);

    // Checking code
    static bool IsLsraAdded(GenTree* node)
    {
        return ((node->gtDebugFlags & GTF_DEBUG_NODE_LSRA_ADDED) != 0);
    }
    static void SetLsraAdded(GenTree* node)
    {
        node->gtDebugFlags |= GTF_DEBUG_NODE_LSRA_ADDED;
    }
    static bool IsResolutionMove(GenTree* node);
    static bool IsResolutionNode(LIR::Range& containingRange, GenTree* node);

    void verifyFreeRegisters(regMaskTP regsToFree);
    void verifyFinalAllocation();
    void verifyResolutionMove(GenTree* resolutionNode, LsraLocation currentLocation);
#else  // !DEBUG
    bool doSelectNearest()
    {
        return false;
    }
    bool extendLifetimes()
    {
        return false;
    }
    bool spillAlways()
    {
        return false;
    }
    // In a retail build we support only the default traversal order
    bool isTraversalLayoutOrder()
    {
        return false;
    }
    bool isTraversalPredFirstOrder()
    {
        return true;
    }
    bool getLsraExtendLifeTimes()
    {
        return false;
    }
    static void SetLsraAdded(GenTree* node)
    {
        // do nothing; checked only under #DEBUG
    }
    bool candidatesAreStressLimited()
    {
        return false;
    }
#endif // !DEBUG

public:
    // Used by Lowering when considering whether to split Longs, as well as by identifyCandidates().
    bool isRegCandidate(LclVarDsc* varDsc);

    bool isContainableMemoryOp(GenTree* node);

private:
    // Determine which locals are candidates for allocation
    template <bool localVarsEnregistered>
    void           identifyCandidates();

    // determine which locals are used in EH constructs we don't want to deal with
    void identifyCandidatesExceptionDataflow();

    void buildPhysRegRecords();

#ifdef DEBUG
    void checkLastUses(BasicBlock* block);
    int ComputeOperandDstCount(GenTree* operand);
    int ComputeAvailableSrcCount(GenTree* node);
#endif // DEBUG

    void setFrameType();

    // Update allocations at start/end of block
    void unassignIntervalBlockStart(RegRecord* regRecord, VarToRegMap inVarToRegMap);
    template <bool localVarsEnregistered>
    void processBlockEndAllocation(BasicBlock* current);

    // Record variable locations at start/end of block
    void processBlockStartLocations(BasicBlock* current);
    void processBlockEndLocations(BasicBlock* current);
    void resetAllRegistersState();

#ifdef TARGET_ARM
    bool isSecondHalfReg(RegRecord* regRec, Interval* interval);
    RegRecord* getSecondHalfRegRec(RegRecord* regRec);
    RegRecord* findAnotherHalfRegRec(RegRecord* regRec);
    regNumber findAnotherHalfRegNum(regNumber regNum);
    bool canSpillDoubleReg(RegRecord* physRegRecord, LsraLocation refLocation);
    void unassignDoublePhysReg(RegRecord* doubleRegRecord);
#endif
    void clearAssignedInterval(RegRecord* reg ARM_ARG(RegisterType regType));
    void updateAssignedInterval(RegRecord* reg, Interval* interval ARM_ARG(RegisterType regType));
    void updatePreviousInterval(RegRecord* reg, Interval* interval ARM_ARG(RegisterType regType));
    bool canRestorePreviousInterval(RegRecord* regRec, Interval* assignedInterval);
    bool isAssignedToInterval(Interval* interval, RegRecord* regRec);
    bool isRefPositionActive(RefPosition* refPosition, LsraLocation refLocation);
    bool canSpillReg(RegRecord* physRegRecord, LsraLocation refLocation);
    weight_t getSpillWeight(RegRecord* physRegRecord);

    // insert refpositions representing prolog zero-inits which will be added later
    void insertZeroInitRefPositions();

    // add physreg refpositions for a tree node, based on calling convention and instruction selection predictions
    void addRefsForPhysRegMask(regMaskTP mask, LsraLocation currentLoc, RefType refType, bool isLastUse);

    void resolveConflictingDefAndUse(Interval* interval, RefPosition* defRefPosition);

    void buildRefPositionsForNode(GenTree* tree, LsraLocation loc);

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    void buildUpperVectorSaveRefPositions(GenTree* tree, LsraLocation currentLoc, regMaskTP fpCalleeKillSet);
    void buildUpperVectorRestoreRefPosition(
        Interval* lclVarInterval, LsraLocation currentLoc, GenTree* node, bool isUse, unsigned multiRegIdx);
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

#if defined(UNIX_AMD64_ABI) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // For AMD64 on SystemV machines. This method
    // is called as replacement for raUpdateRegStateForArg
    // that is used on Windows. On System V systems a struct can be passed
    // partially using registers from the 2 register files.
    //
    // For LoongArch64's ABI, a struct can be passed
    // partially using registers from the 2 register files.
    void UpdateRegStateForStructArg(LclVarDsc* argDsc);
#endif // defined(UNIX_AMD64_ABI) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

    // Update reg state for an incoming register argument
    void updateRegStateForArg(LclVarDsc* argDsc);

    inline bool isCandidateLocalRef(GenTree* tree)
    {
        if (tree->IsLocal())
        {
            const LclVarDsc* varDsc = compiler->lvaGetDesc(tree->AsLclVarCommon());
            return isCandidateVar(varDsc);
        }
        return false;
    }

    // Helpers for getKillSetForNode().
    regMaskTP getKillSetForStoreInd(GenTreeStoreInd* tree);
    regMaskTP getKillSetForShiftRotate(GenTreeOp* tree);
    regMaskTP getKillSetForMul(GenTreeOp* tree);
    regMaskTP getKillSetForCall(GenTreeCall* call);
    regMaskTP getKillSetForModDiv(GenTreeOp* tree);
    regMaskTP getKillSetForBlockStore(GenTreeBlk* blkNode);
    regMaskTP getKillSetForReturn();
    regMaskTP getKillSetForProfilerHook();
#ifdef FEATURE_HW_INTRINSICS
    regMaskTP getKillSetForHWIntrinsic(GenTreeHWIntrinsic* node);
#endif // FEATURE_HW_INTRINSICS

// Return the registers killed by the given tree node.
// This is used only for an assert, and for stress, so it is only defined under DEBUG.
// Otherwise, the Build methods should obtain the killMask from the appropriate method above.
#ifdef DEBUG
    regMaskTP getKillSetForNode(GenTree* tree);
#endif

    // Given some tree node add refpositions for all the registers this node kills
    bool buildKillPositionsForNode(GenTree* tree, LsraLocation currentLoc, regMaskTP killMask);

    regMaskTP allRegs(RegisterType rt);
    regMaskTP allByteRegs();
    regMaskTP allSIMDRegs();
    regMaskTP lowSIMDRegs();
    regMaskTP internalFloatRegCandidates();

    void makeRegisterInactive(RegRecord* physRegRecord);
    void freeRegister(RegRecord* physRegRecord);
    void freeRegisters(regMaskTP regsToFree);

    // Get the type that this tree defines.
    var_types getDefType(GenTree* tree)
    {
        var_types type = tree->TypeGet();
        if (type == TYP_STRUCT)
        {
            assert(tree->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR));
            GenTreeLclVar* lclVar = tree->AsLclVar();
            LclVarDsc*     varDsc = compiler->lvaGetDesc(lclVar);
            type                  = varDsc->GetRegisterType(lclVar);
        }
        assert(type != TYP_UNDEF && type != TYP_STRUCT);
        return type;
    }

    // Managing internal registers during the BuildNode process.
    RefPosition* defineNewInternalTemp(GenTree* tree, RegisterType regType, regMaskTP candidates);
    RefPosition* buildInternalIntRegisterDefForNode(GenTree* tree, regMaskTP internalCands = RBM_NONE);
    RefPosition* buildInternalFloatRegisterDefForNode(GenTree* tree, regMaskTP internalCands = RBM_NONE);
#if defined(FEATURE_SIMD)
    RefPosition* buildInternalMaskRegisterDefForNode(GenTree* tree, regMaskTP internalCands = RBM_NONE);
#endif
    void buildInternalRegisterUses();

    void writeLocalReg(GenTreeLclVar* lclNode, unsigned varNum, regNumber reg);
    void resolveLocalRef(BasicBlock* block, GenTreeLclVar* treeNode, RefPosition* currentRefPosition);

    void insertMove(BasicBlock* block, GenTree* insertionPoint, unsigned lclNum, regNumber inReg, regNumber outReg);

    void insertSwap(
        BasicBlock* block, GenTree* insertionPoint, unsigned lclNum1, regNumber reg1, unsigned lclNum2, regNumber reg2);

private:
    Interval* newInterval(RegisterType regType);

    Interval* getIntervalForLocalVar(unsigned varIndex)
    {
        assert(varIndex < compiler->lvaTrackedCount);
        assert(localVarIntervals[varIndex] != nullptr);
        return localVarIntervals[varIndex];
    }

    Interval* getIntervalForLocalVarNode(GenTreeLclVarCommon* tree)
    {
        const LclVarDsc* varDsc = compiler->lvaGetDesc(tree);
        assert(varDsc->lvTracked);
        return getIntervalForLocalVar(varDsc->lvVarIndex);
    }

    RegRecord* getRegisterRecord(regNumber regNum);

    RefPosition* newRefPositionRaw(LsraLocation nodeLocation, GenTree* treeNode, RefType refType);

    RefPosition* newRefPosition(Interval*    theInterval,
                                LsraLocation theLocation,
                                RefType      theRefType,
                                GenTree*     theTreeNode,
                                regMaskTP    mask,
                                unsigned     multiRegIdx = 0);

    RefPosition* newRefPosition(
        regNumber reg, LsraLocation theLocation, RefType theRefType, GenTree* theTreeNode, regMaskTP mask);

    void applyCalleeSaveHeuristics(RefPosition* rp);

    void checkConflictingDefUse(RefPosition* rp);

    void associateRefPosWithInterval(RefPosition* rp);

    weight_t getWeight(RefPosition* refPos);

    /*****************************************************************************
     * Register management
     ****************************************************************************/
    RegisterType getRegisterType(Interval* currentInterval, RefPosition* refPosition);

#ifdef DEBUG
    const char* getScoreName(RegisterScore score);
#endif
    template <bool needsConsecutiveRegisters = false>
    regNumber allocateReg(Interval* current, RefPosition* refPosition DEBUG_ARG(RegisterScore* registerScore));
    regNumber allocateRegMinimal(Interval* current, RefPosition* refPosition DEBUG_ARG(RegisterScore* registerScore));
    template <bool needsConsecutiveRegisters = false>
    regNumber assignCopyReg(RefPosition* refPosition);
    regNumber assignCopyRegMinimal(RefPosition* refPosition);

    bool isMatchingConstant(RegRecord* physRegRecord, RefPosition* refPosition);
    bool isSpillCandidate(Interval* current, RefPosition* refPosition, RegRecord* physRegRecord);
    void checkAndAssignInterval(RegRecord* regRec, Interval* interval);
    void assignPhysReg(RegRecord* regRec, Interval* interval);
    void assignPhysReg(regNumber reg, Interval* interval)
    {
        assignPhysReg(getRegisterRecord(reg), interval);
    }

    bool isAssigned(RegRecord* regRec ARM_ARG(RegisterType newRegType));
    void checkAndClearInterval(RegRecord* regRec, RefPosition* spillRefPosition);
    void unassignPhysReg(RegRecord* regRec ARM_ARG(RegisterType newRegType));
    void unassignPhysReg(RegRecord* regRec, RefPosition* spillRefPosition);
    void unassignPhysRegNoSpill(RegRecord* reg);
    void unassignPhysReg(regNumber reg)
    {
        unassignPhysReg(getRegisterRecord(reg), nullptr);
    }

    void setIntervalAsSpilled(Interval* interval);
    void setIntervalAsSplit(Interval* interval);
    void spillInterval(Interval* interval, RefPosition* fromRefPosition DEBUGARG(RefPosition* toRefPosition));

    void spillGCRefs(RefPosition* killRefPosition);

/*****************************************************************************
* Register selection
****************************************************************************/

#if defined(TARGET_ARM64)
    bool canAssignNextConsecutiveRegisters(RefPosition* firstRefPosition, regNumber firstRegAssigned);
    void assignConsecutiveRegisters(RefPosition* firstRefPosition, regNumber firstRegAssigned);
    regMaskTP getConsecutiveCandidates(regMaskTP candidates, RefPosition* refPosition, regMaskTP* busyCandidates);
    regMaskTP filterConsecutiveCandidates(regMaskTP    candidates,
                                          unsigned int registersNeeded,
                                          regMaskTP*   allConsecutiveCandidates);
    regMaskTP filterConsecutiveCandidatesForSpill(regMaskTP consecutiveCandidates, unsigned int registersNeeded);
#endif // TARGET_ARM64

    regMaskTP getFreeCandidates(regMaskTP candidates ARM_ARG(var_types regType))
    {
        regMaskTP result = candidates & m_AvailableRegs;
#ifdef TARGET_ARM
        // For TYP_DOUBLE on ARM, we can only use register for which the odd half is
        // also available.
        if (regType == TYP_DOUBLE)
        {
            result &= (m_AvailableRegs >> 1);
        }
#endif // TARGET_ARM
        return result;
    }

#ifdef DEBUG
    class RegisterSelection;
    // For lsra ordering experimentation

    typedef void (LinearScan::RegisterSelection::*HeuristicFn)();
    typedef JitHashTable<RegisterScore, JitSmallPrimitiveKeyFuncs<RegisterScore>, HeuristicFn> ScoreMappingTable;
#define REGSELECT_HEURISTIC_COUNT 17
#endif

    class RegisterSelection
    {
    public:
        RegisterSelection(LinearScan* linearScan);

        // Perform register selection and update currentInterval or refPosition
        template <bool hasConsecutiveRegister = false>
        FORCEINLINE regMaskTP select(Interval*    currentInterval,
                                     RefPosition* refPosition DEBUG_ARG(RegisterScore* registerScore));

        FORCEINLINE regMaskTP selectMinimal(Interval*    currentInterval,
                                            RefPosition* refPosition DEBUG_ARG(RegisterScore* registerScore));

        // If the register is from unassigned set such that it was not already
        // assigned to the current interval
        FORCEINLINE bool foundUnassignedReg()
        {
            assert(found && isSingleRegister(foundRegBit));
            bool isUnassignedReg = ((foundRegBit & unassignedSet) != RBM_NONE);
            return isUnassignedReg && !isAlreadyAssigned();
        }

        // Did register selector decide to spill this interval
        FORCEINLINE bool isSpilling()
        {
            return (foundRegBit & freeCandidates) == RBM_NONE;
        }

        // Is the value one of the constant that is already in a register
        FORCEINLINE bool isMatchingConstant()
        {
            assert(found && isSingleRegister(foundRegBit));
            return (matchingConstants & foundRegBit) != RBM_NONE;
        }

        // Did we apply CONST_AVAILABLE heuristics
        FORCEINLINE bool isConstAvailable()
        {
            return constAvailableApplied;
        }

    private:
#ifdef DEBUG
        RegisterScore      RegSelectionOrder[REGSELECT_HEURISTIC_COUNT] = {NONE};
        ScoreMappingTable* mappingTable                                 = nullptr;
#endif
        LinearScan*  linearScan      = nullptr;
        Interval*    currentInterval = nullptr;
        RefPosition* refPosition     = nullptr;

        RegisterType regType = RegisterType::TYP_UNKNOWN;

        regMaskTP candidates;
        regMaskTP preferences     = RBM_NONE;
        Interval* relatedInterval = nullptr;

        regMaskTP    relatedPreferences = RBM_NONE;
        LsraLocation rangeEndLocation;
        LsraLocation relatedLastLocation;
        bool         preferCalleeSave = false;
        RefPosition* rangeEndRefPosition;
        RefPosition* lastRefPosition;
        regMaskTP    callerCalleePrefs = RBM_NONE;
        LsraLocation lastLocation;

        regMaskTP foundRegBit;

        regMaskTP prevRegBit = RBM_NONE;

        // These are used in the post-selection updates, and must be set for any selection.
        regMaskTP freeCandidates;
        regMaskTP matchingConstants;
        regMaskTP unassignedSet;

        // Compute the sets for COVERS, OWN_PREFERENCE, COVERS_RELATED, COVERS_FULL and UNASSIGNED together,
        // as they all require similar computation.
        regMaskTP coversSet;
        regMaskTP preferenceSet;
        regMaskTP coversRelatedSet;
        regMaskTP coversFullSet;
        bool      coversSetsCalculated  = false;
        bool      found                 = false;
        bool      skipAllocation        = false;
        bool      coversFullApplied     = false;
        bool      constAvailableApplied = false;

        // If the selected register is already assigned to the current internal
        FORCEINLINE bool isAlreadyAssigned()
        {
            assert(found && isSingleRegister(candidates));
            return (prevRegBit & preferences) == foundRegBit;
        }

        bool applySelection(int selectionScore, regMaskTP selectionCandidates);
        bool applySingleRegSelection(int selectionScore, regMaskTP selectionCandidate);
        FORCEINLINE void calculateCoversSets();
        FORCEINLINE void calculateUnassignedSets();
        FORCEINLINE void reset(Interval* interval, RefPosition* refPosition);
        FORCEINLINE void resetMinimal(Interval* interval, RefPosition* refPosition);

#define REG_SEL_DEF(stat, value, shortname, orderSeqId) FORCEINLINE void try_##stat();
#define BUSY_REG_SEL_DEF(stat, value, shortname, orderSeqId) REG_SEL_DEF(stat, value, shortname, orderSeqId)
#include "lsra_score.h"
    };

    RegisterSelection* regSelector;

    /*****************************************************************************
     * For Resolution phase
     ****************************************************************************/
    // TODO-Throughput: Consider refactoring this so that we keep a map from regs to vars for better scaling
    unsigned int regMapCount;

    // When we split edges, we create new blocks, and instead of expanding the VarToRegMaps, we
    // rely on the property that the "in" map is the same as the "from" block of the edge, and the
    // "out" map is the same as the "to" block of the edge (by construction).
    // So, for any block whose bbNum is greater than bbNumMaxBeforeResolution, we use the
    // splitBBNumToTargetBBNumMap.
    // TODO-Throughput: We may want to look into the cost/benefit tradeoff of doing this vs. expanding
    // the arrays.

    unsigned bbNumMaxBeforeResolution;
    struct SplitEdgeInfo
    {
        unsigned fromBBNum;
        unsigned toBBNum;
    };
    typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, SplitEdgeInfo> SplitBBNumToTargetBBNumMap;
    SplitBBNumToTargetBBNumMap* splitBBNumToTargetBBNumMap;
    SplitBBNumToTargetBBNumMap* getSplitBBNumToTargetBBNumMap()
    {
        if (splitBBNumToTargetBBNumMap == nullptr)
        {
            splitBBNumToTargetBBNumMap =
                new (getAllocator(compiler)) SplitBBNumToTargetBBNumMap(getAllocator(compiler));
        }
        return splitBBNumToTargetBBNumMap;
    }
    SplitEdgeInfo getSplitEdgeInfo(unsigned int bbNum);

    void initVarRegMaps();
    void setInVarRegForBB(unsigned int bbNum, unsigned int varNum, regNumber reg);
    void setOutVarRegForBB(unsigned int bbNum, unsigned int varNum, regNumber reg);
    VarToRegMap getInVarToRegMap(unsigned int bbNum);
    VarToRegMap getOutVarToRegMap(unsigned int bbNum);
    void setVarReg(VarToRegMap map, unsigned int trackedVarIndex, regNumber reg);
    regNumber getVarReg(VarToRegMap map, unsigned int trackedVarIndex);
    // Initialize the incoming VarToRegMap to the given map values (generally a predecessor of
    // the block)
    VarToRegMap setInVarToRegMap(unsigned int bbNum, VarToRegMap srcVarToRegMap);

    regNumber getTempRegForResolution(BasicBlock*      fromBlock,
                                      BasicBlock*      toBlock,
                                      var_types        type,
                                      VARSET_VALARG_TP sharedCriticalLiveSet,
                                      regMaskTP        terminatorConsumedRegs);

#ifdef TARGET_ARM64
    typedef JitHashTable<RefPosition*, JitPtrKeyFuncs<RefPosition>, RefPosition*> NextConsecutiveRefPositionsMap;
    NextConsecutiveRefPositionsMap* nextConsecutiveRefPositionMap;
    NextConsecutiveRefPositionsMap* getNextConsecutiveRefPositionsMap()
    {
        if (nextConsecutiveRefPositionMap == nullptr)
        {
            nextConsecutiveRefPositionMap =
                new (getAllocator(compiler)) NextConsecutiveRefPositionsMap(getAllocator(compiler));
        }
        return nextConsecutiveRefPositionMap;
    }
    FORCEINLINE RefPosition* getNextConsecutiveRefPosition(RefPosition* refPosition);
#endif

#ifdef DEBUG
    void dumpVarToRegMap(VarToRegMap map);
    void dumpInVarToRegMap(BasicBlock* block);
    void dumpOutVarToRegMap(BasicBlock* block);

    // There are three points at which a tuple-style dump is produced, and each
    // differs slightly:
    //   - In LSRA_DUMP_PRE, it does a simple dump of each node, with indications of what
    //     tree nodes are consumed.
    //   - In LSRA_DUMP_REFPOS, which is after the intervals are built, but before
    //     register allocation, each node is dumped, along with all of the RefPositions,
    //     The Intervals are identifed as Lnnn for lclVar intervals, Innn for other
    //     intervals, and Tnnn for internal temps.
    //   - In LSRA_DUMP_POST, which is after register allocation, the registers are
    //     shown.

    enum LsraTupleDumpMode{LSRA_DUMP_PRE, LSRA_DUMP_REFPOS, LSRA_DUMP_POST};
    void lsraGetOperandString(GenTree* tree, LsraTupleDumpMode mode, char* operandString, unsigned operandStringLength);
    void lsraDispNode(GenTree* tree, LsraTupleDumpMode mode, bool hasDest);
    void DumpOperandDefs(
        GenTree* operand, bool& first, LsraTupleDumpMode mode, char* operandString, const unsigned operandStringLength);
    void TupleStyleDump(LsraTupleDumpMode mode);

    LsraLocation maxNodeLocation;

    // Width of various fields - used to create a streamlined dump during allocation that shows the
    // state of all the registers in columns.
    int regColumnWidth;
    int regTableIndent;

    const char* columnSeparator;
    const char* line;
    const char* leftBox;
    const char* middleBox;
    const char* rightBox;

    static const int MAX_FORMAT_CHARS = 12;
    char             intervalNameFormat[MAX_FORMAT_CHARS];
    char             smallLocalsIntervalNameFormat[MAX_FORMAT_CHARS]; // used for V01 to V09 (to match V%02u format)
    char             regNameFormat[MAX_FORMAT_CHARS];
    char             shortRefPositionFormat[MAX_FORMAT_CHARS];
    char             emptyRefPositionFormat[MAX_FORMAT_CHARS];
    char             indentFormat[MAX_FORMAT_CHARS];
    static const int MAX_LEGEND_FORMAT_CHARS = 34;
    char             bbRefPosFormat[MAX_LEGEND_FORMAT_CHARS];
    char             legendFormat[MAX_LEGEND_FORMAT_CHARS];

    // How many rows have we printed since last printing a "title row"?
    static const int MAX_ROWS_BETWEEN_TITLES = 50;
    int              rowCountSinceLastTitle;
    // Current mask of registers being printed in the dump.
    regMaskTP lastDumpedRegisters;
    regMaskTP registersToDump;
    int       lastUsedRegNumIndex;
    bool shouldDumpReg(regNumber regNum)
    {
        return (registersToDump & genRegMask(regNum)) != 0;
    }

    void dumpRegRecordHeader();
    void dumpRegRecordTitle();
    void dumpRegRecordTitleIfNeeded();
    void dumpRegRecordTitleLines();
    void dumpRegRecords();
    void dumpNewBlock(BasicBlock* currentBlock, LsraLocation location);
    // An abbreviated RefPosition dump for printing with column-based register state
    void dumpRefPositionShort(RefPosition* refPosition, BasicBlock* currentBlock);
    // Print the number of spaces occupied by a dumpRefPositionShort()
    void dumpEmptyRefPosition();
    // Print the number of spaces occupied by tree ID.
    void dumpEmptyTreeID();
    // A dump of Referent, in exactly regColumnWidth characters
    void dumpIntervalName(Interval* interval);

    // Events during the allocation phase that cause some dump output
    enum LsraDumpEvent{
        // Conflicting def/use
        LSRA_EVENT_DEFUSE_CONFLICT, LSRA_EVENT_DEFUSE_FIXED_DELAY_USE, LSRA_EVENT_DEFUSE_CASE1, LSRA_EVENT_DEFUSE_CASE2,
        LSRA_EVENT_DEFUSE_CASE3, LSRA_EVENT_DEFUSE_CASE4, LSRA_EVENT_DEFUSE_CASE5, LSRA_EVENT_DEFUSE_CASE6,

        // Spilling
        LSRA_EVENT_SPILL, LSRA_EVENT_SPILL_EXTENDED_LIFETIME, LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL,
        LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL_AFTER_SPILL, LSRA_EVENT_DONE_KILL_GC_REFS, LSRA_EVENT_NO_GC_KILLS,

        // Block boundaries
        LSRA_EVENT_START_BB, LSRA_EVENT_END_BB,

        // Miscellaneous
        LSRA_EVENT_FREE_REGS, LSRA_EVENT_UPPER_VECTOR_SAVE, LSRA_EVENT_UPPER_VECTOR_RESTORE,

        // Characteristics of the current RefPosition
        LSRA_EVENT_INCREMENT_RANGE_END, // ???
        LSRA_EVENT_LAST_USE, LSRA_EVENT_LAST_USE_DELAYED, LSRA_EVENT_NEEDS_NEW_REG,

        // Allocation decisions
        LSRA_EVENT_FIXED_REG, LSRA_EVENT_EXP_USE, LSRA_EVENT_ZERO_REF, LSRA_EVENT_NO_ENTRY_REG_ALLOCATED,
        LSRA_EVENT_KEPT_ALLOCATION, LSRA_EVENT_COPY_REG, LSRA_EVENT_MOVE_REG, LSRA_EVENT_ALLOC_REG,
        LSRA_EVENT_NO_REG_ALLOCATED, LSRA_EVENT_RELOAD, LSRA_EVENT_SPECIAL_PUTARG, LSRA_EVENT_REUSE_REG,
    };
    void dumpLsraAllocationEvent(LsraDumpEvent event,
                                 Interval*     interval      = nullptr,
                                 regNumber     reg           = REG_NA,
                                 BasicBlock*   currentBlock  = nullptr,
                                 RegisterScore registerScore = NONE);

    void validateIntervals();
#endif // DEBUG

#if TRACK_LSRA_STATS
    unsigned regCandidateVarCount;
    void updateLsraStat(LsraStat stat, unsigned currentBBNum);
    void dumpLsraStats(FILE* file);
    LsraStat getLsraStatFromScore(RegisterScore registerScore);
    LsraStat firstRegSelStat = STAT_FREE;

public:
    virtual void dumpLsraStatsCsv(FILE* file);
    virtual void dumpLsraStatsSummary(FILE* file);
    static const char* getStatName(unsigned stat);

#define INTRACK_STATS(x) x
#define INTRACK_STATS_IF(condition, work)                                                                              \
    if (condition)                                                                                                     \
    {                                                                                                                  \
        work;                                                                                                          \
    }

#else // !TRACK_LSRA_STATS
#define INTRACK_STATS(x)
#define INTRACK_STATS_IF(condition, work)
#endif // !TRACK_LSRA_STATS

private:
    Compiler*     compiler;
    CompAllocator getAllocator(Compiler* comp)
    {
        return comp->getAllocator(CMK_LSRA);
    }

#ifdef DEBUG
    // This is used for dumping
    RefPosition* activeRefPosition;
#endif // DEBUG

    IntervalList intervals;

    RegRecord physRegs[REG_COUNT];

    // Map from tracked variable index to Interval*.
    Interval** localVarIntervals;

    // Set of blocks that have been visited.
    BlockSet bbVisitedSet;
    void markBlockVisited(BasicBlock* block)
    {
        BlockSetOps::AddElemD(compiler, bbVisitedSet, block->bbNum);
    }
    void clearVisitedBlocks()
    {
        BlockSetOps::ClearD(compiler, bbVisitedSet);
    }
    bool isBlockVisited(BasicBlock* block)
    {
        return BlockSetOps::IsMember(compiler, bbVisitedSet, block->bbNum);
    }

#if DOUBLE_ALIGN
    bool doDoubleAlign;
#endif

    // A map from bbNum to the block information used during register allocation.
    LsraBlockInfo* blockInfo;

    BasicBlock* findPredBlockForLiveIn(BasicBlock* block, BasicBlock* prevBlock DEBUGARG(bool* pPredBlockIsAllocated));

    // The order in which the blocks will be allocated.
    // This is any array of BasicBlock*, in the order in which they should be traversed.
    BasicBlock** blockSequence;
    // The verifiedAllBBs flag indicates whether we have verified that all BBs have been
    // included in the blockSeuqence above, during setBlockSequence().
    bool verifiedAllBBs;
    void setBlockSequence();
    int compareBlocksForSequencing(BasicBlock* block1, BasicBlock* block2, bool useBlockWeights);
    BasicBlockList* blockSequenceWorkList;
    bool            blockSequencingDone;
#ifdef DEBUG
    // LSRA must not change number of blocks and blockEpoch that it initializes at start.
    unsigned blockEpoch;
#endif // DEBUG
    void addToBlockSequenceWorkList(BlockSet sequencedBlockSet, BasicBlock* block, BlockSet& predSet);
    void removeFromBlockSequenceWorkList(BasicBlockList* listNode, BasicBlockList* prevNode);
    BasicBlock* getNextCandidateFromWorkList();

    // Indicates whether the allocation pass has been completed.
    bool allocationPassComplete;

    // The bbNum of the block being currently allocated or resolved.
    unsigned int curBBNum;
    // The current location
    LsraLocation currentLoc;
    // The first location in a cold or funclet block.
    LsraLocation firstColdLoc;
    // The ordinal of the block we're on (i.e. this is the curBBSeqNum-th block we've allocated).
    unsigned int curBBSeqNum;
    // The number of blocks that we've sequenced.
    unsigned int bbSeqCount;
    // The Location of the start of the current block.
    LsraLocation curBBStartLocation;
    // True if the method contains any critical edges.
    bool hasCriticalEdges;

#ifdef DEBUG
    // Tracks the GenTree* for which intervals are being
    // built. Use for displaying in allocation table.
    GenTree* currBuildNode;
#endif

    // True if there are any register candidate lclVars available for allocation.
    bool enregisterLocalVars;

    virtual bool willEnregisterLocalVars() const
    {
        return enregisterLocalVars;
    }

    // Ordered list of RefPositions
    RefPositionList refPositions;

    // Per-block variable location mappings: an array indexed by block number that yields a
    // pointer to an array of regNumber, one per variable.
    VarToRegMap* inVarToRegMaps;
    VarToRegMap* outVarToRegMaps;

    // A temporary VarToRegMap used during the resolution of critical edges.
    VarToRegMap          sharedCriticalVarToRegMap;
    PhasedVar<regMaskTP> actualRegistersMask;
    PhasedVar<regMaskTP> availableIntRegs;
    PhasedVar<regMaskTP> availableFloatRegs;
    PhasedVar<regMaskTP> availableDoubleRegs;
#if defined(TARGET_XARCH)
    PhasedVar<regMaskTP> availableMaskRegs;
#endif
    PhasedVar<regMaskTP>* availableRegs[TYP_COUNT];

#if defined(TARGET_XARCH)
#define allAvailableRegs (availableIntRegs | availableFloatRegs | availableMaskRegs)
#else
#define allAvailableRegs (availableIntRegs | availableFloatRegs)
#endif

    // Register mask of argument registers currently occupied because we saw a
    // PUTARG_REG node. Tracked between the PUTARG_REG and its corresponding
    // CALL node and is used to avoid preferring these registers for locals
    // which would otherwise force a spill.
    regMaskTP placedArgRegs;

    struct PlacedLocal
    {
        unsigned  VarIndex;
        regNumber Reg;
    };

    // Locals that are currently placed in registers via PUTARG_REG. These
    // locals are available due to the special PUTARG treatment, and we keep
    // track of them between the PUTARG_REG and CALL to ensure we keep the
    // register they are placed in in the preference set.
    PlacedLocal placedArgLocals[REG_COUNT];
    size_t      numPlacedArgLocals;

    // The set of all register candidates. Note that this may be a subset of tracked vars.
    VARSET_TP registerCandidateVars;
    // Current set of live register candidate vars, used during building of RefPositions to determine
    // whether to give preference to callee-save.
    VARSET_TP currentLiveVars;
    // Set of variables that may require resolution across an edge.
    // This is first constructed during interval building, to contain all the lclVars that are live at BB edges.
    // Then, any lclVar that is always in the same register is removed from the set.
    VARSET_TP resolutionCandidateVars;
    // This set contains all the lclVars that are ever spilled or split.
    VARSET_TP splitOrSpilledVars;
    // Set of floating point variables to consider for callee-save registers.
    VARSET_TP fpCalleeSaveCandidateVars;
    // Set of variables exposed on EH flow edges.
    VARSET_TP exceptVars;
    // Set of variables exposed on finally edges. These must be zero-init if they are refs or if compInitMem is true.
    VARSET_TP finallyVars;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
#if defined(TARGET_AMD64)
    static const var_types LargeVectorSaveType = TYP_SIMD16;
#elif defined(TARGET_ARM64)
    static const var_types LargeVectorSaveType  = TYP_DOUBLE;
#endif // !defined(TARGET_AMD64) && !defined(TARGET_ARM64)
    // Set of large vector (TYP_SIMD32 on AVX) variables.
    VARSET_TP largeVectorVars;
    // Set of large vector (TYP_SIMD32 on AVX) variables to consider for callee-save registers.
    VARSET_TP largeVectorCalleeSaveCandidateVars;
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

    //-----------------------------------------------------------------------
    // Register status
    //-----------------------------------------------------------------------

    regMaskTP m_AvailableRegs;
    regNumber getRegForType(regNumber reg, var_types regType)
    {
#ifdef TARGET_ARM
        if ((regType == TYP_DOUBLE) && !genIsValidDoubleReg(reg))
        {
            reg = REG_PREV(reg);
        }
#endif // TARGET_ARM
        return reg;
    }

    regMaskTP getRegMask(regNumber reg, var_types regType)
    {
        reg               = getRegForType(reg, regType);
        regMaskTP regMask = genRegMask(reg);
#ifdef TARGET_ARM
        if (regType == TYP_DOUBLE)
        {
            assert(genIsValidDoubleReg(reg));
            regMask |= (regMask << 1);
        }
#endif // TARGET_ARM
        return regMask;
    }

    void resetAvailableRegs()
    {
        m_AvailableRegs          = allAvailableRegs;
        m_RegistersWithConstants = RBM_NONE;
    }

    bool isRegAvailable(regNumber reg, var_types regType)
    {
        regMaskTP regMask = getRegMask(reg, regType);
        return (m_AvailableRegs & regMask) == regMask;
    }
    void setRegsInUse(regMaskTP regMask)
    {
        m_AvailableRegs &= ~regMask;
    }
    void setRegInUse(regNumber reg, var_types regType)
    {
        regMaskTP regMask = getRegMask(reg, regType);
        setRegsInUse(regMask);
    }
    void makeRegsAvailable(regMaskTP regMask)
    {
        m_AvailableRegs |= regMask;
    }
    void makeRegAvailable(regNumber reg, var_types regType)
    {
        regMaskTP regMask = getRegMask(reg, regType);
        makeRegsAvailable(regMask);
    }

    void clearAllNextIntervalRef();
    void clearNextIntervalRef(regNumber reg, var_types regType);
    void updateNextIntervalRef(regNumber reg, Interval* interval);

    void clearAllSpillCost();
    void clearSpillCost(regNumber reg, var_types regType);
    void updateSpillCost(regNumber reg, Interval* interval);

    FORCEINLINE void updateRegsFreeBusyState(RefPosition& refPosition,
                                             regMaskTP    regsBusy,
                                             regMaskTP*   regsToFree,
                                             regMaskTP* delayRegsToFree DEBUG_ARG(Interval* interval)
                                                 DEBUG_ARG(regNumber assignedReg));

    regMaskTP m_RegistersWithConstants;
    void clearConstantReg(regNumber reg, var_types regType)
    {
        m_RegistersWithConstants &= ~getRegMask(reg, regType);
    }
    void setConstantReg(regNumber reg, var_types regType)
    {
        m_RegistersWithConstants |= getRegMask(reg, regType);
    }
    bool isRegConstant(regNumber reg, var_types regType)
    {
        reg               = getRegForType(reg, regType);
        regMaskTP regMask = getRegMask(reg, regType);
        return (m_RegistersWithConstants & regMask) == regMask;
    }
    regMaskTP getMatchingConstants(regMaskTP mask, Interval* currentInterval, RefPosition* refPosition);

    regMaskTP    fixedRegs;
    LsraLocation nextFixedRef[REG_COUNT];
    void updateNextFixedRef(RegRecord* regRecord, RefPosition* nextRefPosition);
    LsraLocation getNextFixedRef(regNumber regNum, var_types regType)
    {
        LsraLocation loc = nextFixedRef[regNum];
#ifdef TARGET_ARM
        if (regType == TYP_DOUBLE)
        {
            loc = Min(loc, nextFixedRef[regNum + 1]);
        }
#endif
        return loc;
    }

    LsraLocation nextIntervalRef[REG_COUNT];
    LsraLocation getNextIntervalRef(regNumber regNum, var_types regType)
    {
        LsraLocation loc = nextIntervalRef[regNum];
#ifdef TARGET_ARM
        if (regType == TYP_DOUBLE)
        {
            loc = Min(loc, nextIntervalRef[regNum + 1]);
        }
#endif
        return loc;
    }
    weight_t spillCost[REG_COUNT];

    regMaskTP regsBusyUntilKill;
    regMaskTP regsInUseThisLocation;
    regMaskTP regsInUseNextLocation;
#ifdef TARGET_ARM64
    regMaskTP consecutiveRegsInUseThisLocation;
#endif
    bool isRegBusy(regNumber reg, var_types regType)
    {
        regMaskTP regMask = getRegMask(reg, regType);
        return (regsBusyUntilKill & regMask) != RBM_NONE;
    }
    void setRegBusyUntilKill(regNumber reg, var_types regType)
    {
        regsBusyUntilKill |= getRegMask(reg, regType);
    }
    void clearRegBusyUntilKill(regNumber reg)
    {
        regsBusyUntilKill &= ~genRegMask(reg);
    }

    bool isRegInUse(regNumber reg, var_types regType)
    {
        regMaskTP regMask = getRegMask(reg, regType);
        return (regsInUseThisLocation & regMask) != RBM_NONE;
    }

    void resetRegState()
    {
        resetAvailableRegs();
        regsBusyUntilKill = RBM_NONE;
    }

    bool conflictingFixedRegReference(regNumber regNum, RefPosition* refPosition);

    // This method should not be used and is here to retain old behavior.
    // It should be replaced by isRegAvailable().
    // See comment in allocateReg();
    bool isFree(RegRecord* regRecord);

    //-----------------------------------------------------------------------
    // Build methods
    //-----------------------------------------------------------------------

    // The listNodePool is used to maintain the RefInfo for nodes that are "in flight"
    // i.e. whose consuming node has not yet been handled.
    RefInfoListNodePool listNodePool;

    // When Def RefPositions are built for a node, their RefInfoListNode
    // (GenTree* to RefPosition* mapping) is placed in the defList.
    // As the consuming node is handled, it removes the RefInfoListNode from the
    // defList, use the interval associated with the corresponding Def RefPosition and
    // use it to build the Use RefPosition.
    RefInfoList defList;

    // As we build uses, we may want to preference the next definition (i.e. the register produced
    // by the current node) to the same register as one of its uses. This is done by setting
    // 'tgtPrefUse' to that RefPosition.
    RefPosition* tgtPrefUse  = nullptr;
    RefPosition* tgtPrefUse2 = nullptr;

public:
    // The following keep track of information about internal (temporary register) intervals
    // during the building of a single node.
    static const int MaxInternalCount = 5;

private:
    RefPosition* internalDefs[MaxInternalCount];
    int          internalCount = 0;
    bool         setInternalRegsDelayFree;

    // When a RefTypeUse is marked as 'delayRegFree', we also want to mark the RefTypeDef
    // in the next Location as 'hasInterferingUses'. This is accomplished by setting this
    // 'pendingDelayFree' to true as they are created, and clearing it as a new node is
    // handled in 'BuildNode'.
    bool pendingDelayFree;

    // This method clears the "build state" before starting to handle a new node.
    void clearBuildState()
    {
        tgtPrefUse               = nullptr;
        tgtPrefUse2              = nullptr;
        internalCount            = 0;
        setInternalRegsDelayFree = false;
        pendingDelayFree         = false;
    }

    bool isCandidateMultiRegLclVar(GenTreeLclVar* lclNode);
    bool checkContainedOrCandidateLclVar(GenTreeLclVar* lclNode);

    RefPosition* BuildUse(GenTree* operand, regMaskTP candidates = RBM_NONE, int multiRegIdx = 0);
    void setDelayFree(RefPosition* use);
    int BuildBinaryUses(GenTreeOp* node, regMaskTP candidates = RBM_NONE);
    int BuildCastUses(GenTreeCast* cast, regMaskTP candidates);
#ifdef TARGET_XARCH
    int BuildRMWUses(GenTree* node, GenTree* op1, GenTree* op2, regMaskTP candidates = RBM_NONE);
    inline regMaskTP BuildEvexIncompatibleMask(GenTree* tree);
#endif // !TARGET_XARCH
    int BuildSelect(GenTreeOp* select);
    // This is the main entry point for building the RefPositions for a node.
    // These methods return the number of sources.
    int BuildNode(GenTree* tree);

    void UpdatePreferencesOfDyingLocal(Interval* interval);
    void getTgtPrefOperands(GenTree* tree, GenTree* op1, GenTree* op2, bool* prefOp1, bool* prefOp2);
    bool supportsSpecialPutArg();

    int BuildSimple(GenTree* tree);
    int BuildOperandUses(GenTree* node, regMaskTP candidates = RBM_NONE);
    void AddDelayFreeUses(RefPosition* refPosition, GenTree* rmwNode);
    int BuildDelayFreeUses(GenTree*      node,
                           GenTree*      rmwNode        = nullptr,
                           regMaskTP     candidates     = RBM_NONE,
                           RefPosition** useRefPosition = nullptr);
    int BuildIndirUses(GenTreeIndir* indirTree, regMaskTP candidates = RBM_NONE);
    int BuildAddrUses(GenTree* addr, regMaskTP candidates = RBM_NONE);
    void HandleFloatVarArgs(GenTreeCall* call, GenTree* argNode, bool* callHasFloatRegArgs);
    RefPosition* BuildDef(GenTree* tree, regMaskTP dstCandidates = RBM_NONE, int multiRegIdx = 0);
    void BuildDefs(GenTree* tree, int dstCount, regMaskTP dstCandidates = RBM_NONE);
    void BuildDefsWithKills(GenTree* tree, int dstCount, regMaskTP dstCandidates, regMaskTP killMask);

    int BuildReturn(GenTree* tree);
#ifdef TARGET_XARCH
    // This method, unlike the others, returns the number of sources, since it may be called when
    // 'tree' is contained.
    int BuildShiftRotate(GenTree* tree);
#endif // TARGET_XARCH
#ifdef TARGET_ARM
    int BuildShiftLongCarry(GenTree* tree);
#endif
    int BuildPutArgReg(GenTreeUnOp* node);
    int BuildCall(GenTreeCall* call);
    int BuildCmp(GenTree* tree);
    int BuildCmpOperands(GenTree* tree);
    int BuildBlockStore(GenTreeBlk* blkNode);
    int BuildModDiv(GenTree* tree);
    int BuildIntrinsic(GenTree* tree);
    void BuildStoreLocDef(GenTreeLclVarCommon* storeLoc, LclVarDsc* varDsc, RefPosition* singleUseRef, int index);
    int BuildMultiRegStoreLoc(GenTreeLclVar* storeLoc);
    int BuildStoreLoc(GenTreeLclVarCommon* tree);
    int BuildIndir(GenTreeIndir* indirTree);
    int BuildGCWriteBarrier(GenTree* tree);
    int BuildCast(GenTreeCast* cast);

#if defined(TARGET_XARCH)
    // returns true if the tree can use the read-modify-write memory instruction form
    bool isRMWRegOper(GenTree* tree);
    int BuildMul(GenTree* tree);
    void SetContainsAVXFlags(unsigned sizeOfSIMDVector = 0);
#endif // defined(TARGET_XARCH)

#if defined(TARGET_X86)
    // Move the last use bit, if any, from 'fromTree' to 'toTree'; 'fromTree' must be contained.
    void CheckAndMoveRMWLastUse(GenTree* fromTree, GenTree* toTree)
    {
        // If 'fromTree' is not a last-use lclVar, there's nothing to do.
        if ((fromTree == nullptr) || !fromTree->OperIs(GT_LCL_VAR) || ((fromTree->gtFlags & GTF_VAR_DEATH) == 0))
        {
            return;
        }
        // If 'fromTree' was a lclVar, it must be contained and 'toTree' must match.
        if (!fromTree->isContained() || (toTree == nullptr) || !toTree->OperIs(GT_LCL_VAR) ||
            (fromTree->AsLclVarCommon()->GetLclNum() != toTree->AsLclVarCommon()->GetLclNum()))
        {
            assert(!"Unmatched RMW indirections");
            return;
        }
        // This is probably not necessary, but keeps things consistent.
        fromTree->gtFlags &= ~GTF_VAR_DEATH;
        toTree->gtFlags |= GTF_VAR_DEATH;
    }
#endif // TARGET_X86

#ifdef FEATURE_HW_INTRINSICS
    int BuildHWIntrinsic(GenTreeHWIntrinsic* intrinsicTree, int* pDstCount);
#ifdef TARGET_ARM64
    int BuildConsecutiveRegistersForUse(GenTree* treeNode, GenTree* rmwNode = nullptr);
    void BuildConsecutiveRegistersForDef(GenTree* treeNode, int fieldCount);
#endif // TARGET_ARM64
#endif // FEATURE_HW_INTRINSICS

#ifdef DEBUG
    LsraLocation consecutiveRegistersLocation;
#endif // DEBUG

    int BuildPutArgStk(GenTreePutArgStk* argNode);
#if FEATURE_ARG_SPLIT
    int BuildPutArgSplit(GenTreePutArgSplit* tree);
#endif // FEATURE_ARG_SPLIT
    int BuildLclHeap(GenTree* tree);

#if defined(TARGET_AMD64)
    regMaskTP rbmAllFloat;
    regMaskTP rbmFltCalleeTrash;

    FORCEINLINE regMaskTP get_RBM_ALLFLOAT() const
    {
        return this->rbmAllFloat;
    }
    FORCEINLINE regMaskTP get_RBM_FLT_CALLEE_TRASH() const
    {
        return this->rbmFltCalleeTrash;
    }
#endif // TARGET_AMD64

#if defined(TARGET_XARCH)
    regMaskTP rbmAllMask;
    regMaskTP rbmMskCalleeTrash;

    FORCEINLINE regMaskTP get_RBM_ALLMASK() const
    {
        return this->rbmAllMask;
    }
    FORCEINLINE regMaskTP get_RBM_MSK_CALLEE_TRASH() const
    {
        return this->rbmMskCalleeTrash;
    }
#endif // TARGET_XARCH

    unsigned availableRegCount;

    FORCEINLINE unsigned get_AVAILABLE_REG_COUNT() const
    {
        return this->availableRegCount;
    }

    //------------------------------------------------------------------------
    // calleeSaveRegs: Get the set of callee-save registers of the given RegisterType
    //
    // NOTE: we currently don't need a LinearScan `this` pointer for this definition, and some callers
    // don't have one available, so make is static.
    //
    static FORCEINLINE regMaskTP calleeSaveRegs(RegisterType rt)
    {
        static const regMaskTP varTypeCalleeSaveRegs[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) csr,
#include "typelist.h"
#undef DEF_TP
        };

        assert((unsigned)rt < ArrLen(varTypeCalleeSaveRegs));
        return varTypeCalleeSaveRegs[rt];
    }

#if defined(TARGET_XARCH)
    // Not all of the callee trash values are constant, so don't declare this as a method local static
    // doing so results in significantly more complex codegen and we'd rather just initialize this once
    // as part of initializing LSRA instead
    regMaskTP varTypeCalleeTrashRegs[TYP_COUNT];
#endif // TARGET_XARCH

    //------------------------------------------------------------------------
    // callerSaveRegs: Get the set of caller-save registers of the given RegisterType
    //
    FORCEINLINE regMaskTP callerSaveRegs(RegisterType rt) const
    {
#if !defined(TARGET_XARCH)
        static const regMaskTP varTypeCalleeTrashRegs[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) ctr,
#include "typelist.h"
#undef DEF_TP
        };
#endif // !TARGET_XARCH

        assert((unsigned)rt < ArrLen(varTypeCalleeTrashRegs));
        return varTypeCalleeTrashRegs[rt];
    }
};

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Interval                                        XX
XX                                                                           XX
XX This is the fundamental data structure for linear scan register           XX
XX allocation.  It represents the live range(s) for a variable or temp.      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

class Interval : public Referenceable
{
public:
    Interval(RegisterType registerType, regMaskTP registerPreferences)
        : registerPreferences(registerPreferences)
        , registerAversion(RBM_NONE)
        , relatedInterval(nullptr)
        , assignedReg(nullptr)
        , varNum(0)
        , physReg(REG_COUNT)
        , registerType(registerType)
        , isActive(false)
        , isLocalVar(false)
        , isSplit(false)
        , isSpilled(false)
        , isInternal(false)
        , isStructField(false)
        , isPromotedStruct(false)
        , hasConflictingDefUse(false)
        , hasInterferingUses(false)
        , isSpecialPutArg(false)
        , preferCalleeSave(false)
        , isConstant(false)
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        , isUpperVector(false)
        , isPartiallySpilled(false)
#endif
        , isWriteThru(false)
        , isSingleDef(false)
#ifdef DEBUG
        , intervalIndex(0)
#endif
    {
    }

#ifdef DEBUG
    // print out representation
    void dump(Compiler* compiler);
    // concise representation for embedding
    void tinyDump();
    // extremely concise representation
    void microDump();
#endif // DEBUG

    void setLocalNumber(Compiler* compiler, unsigned lclNum, LinearScan* l);

    // Fixed registers for which this Interval has a preference
    regMaskTP registerPreferences;

    // Registers that should be avoided for this interval
    regMaskTP registerAversion;

    // The relatedInterval is:
    //  - for any other interval, it is the interval to which this interval
    //    is currently preferenced (e.g. because they are related by a copy)
    Interval* relatedInterval;

    // The assignedReg is the RegRecord for the register to which this interval
    // has been assigned at some point - if the interval is active, this is the
    // register it currently occupies.
    RegRecord* assignedReg;

    unsigned int varNum; // This is the "variable number": the index into the lvaTable array

    // The register to which it is currently assigned.
    regNumber physReg;

    RegisterType registerType;

    // Is this Interval currently in a register and live?
    bool isActive;

    bool isLocalVar : 1;
    // Indicates whether this interval has been assigned to different registers
    bool isSplit : 1;
    // Indicates whether this interval is ever spilled
    bool isSpilled : 1;
    // indicates an interval representing the internal requirements for
    // generating code for a node (temp registers internal to the node)
    // Note that this interval may live beyond a node in the GT_ARR_LENREF/GT_IND
    // case (though never lives beyond a stmt)
    bool isInternal : 1;
    // true if this is a LocalVar for a struct field
    bool isStructField : 1;
    // true iff this is a GT_LDOBJ for a fully promoted (PROMOTION_TYPE_INDEPENDENT) struct
    bool isPromotedStruct : 1;
    // true if this is an SDSU interval for which the def and use have conflicting register
    // requirements
    bool hasConflictingDefUse : 1;
    // true if this interval's defining node has "delayRegFree" uses, either due to it being an RMW instruction,
    // OR because it requires an internal register that differs from the target.
    bool hasInterferingUses : 1;

    // True if this interval is defined by a putArg, whose source is a non-last-use lclVar.
    // During allocation, this flag will be cleared if the source is not already in the required register.
    // Otherwise, we will leave the register allocated to the lclVar, but mark the RegRecord as
    // isBusyUntilKill, so that it won't be reused if the lclVar goes dead before the call.
    bool isSpecialPutArg : 1;

    // True if this interval interferes with a call.
    bool preferCalleeSave : 1;

    // True if this interval is defined by a constant node that may be reused and/or may be
    // able to reuse a constant that's already in a register.
    bool isConstant : 1;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    // True if this is a special interval for saving the upper half of a large vector.
    bool isUpperVector : 1;
    // This is a convenience method to avoid ifdef's everywhere this is used.
    bool IsUpperVector() const
    {
        return isUpperVector;
    }

    // True if this interval has been partially spilled
    bool isPartiallySpilled : 1;
#else
    bool IsUpperVector() const
    {
        return false;
    }
#endif

    // True if this interval is associated with a lclVar that is written to memory at each definition.
    bool isWriteThru : 1;

    // True if this interval has a single definition.
    bool isSingleDef : 1;

#ifdef DEBUG
    unsigned int intervalIndex;
#endif // DEBUG

    LclVarDsc* getLocalVar(Compiler* comp)
    {
        assert(isLocalVar);
        return comp->lvaGetDesc(this->varNum);
    }

    // Get the local tracked variable "index" (lvVarIndex), used in bitmasks.
    unsigned getVarIndex(Compiler* comp)
    {
        LclVarDsc* varDsc = getLocalVar(comp);
        assert(varDsc->lvTracked); // If this isn't true, we shouldn't be calling this function!
        return varDsc->lvVarIndex;
    }

    bool isAssignedTo(regNumber regNum)
    {
        // This uses regMasks to handle the case where a double actually occupies two registers
        // TODO-Throughput: This could/should be done more cheaply.
        return (physReg != REG_NA && (genRegMask(physReg, registerType) & genRegMask(regNum)) != RBM_NONE);
    }

    // Assign the related interval.
    void assignRelatedInterval(Interval* newRelatedInterval)
    {
#ifdef DEBUG
        if (VERBOSE)
        {
            printf("Assigning related ");
            newRelatedInterval->microDump();
            printf(" to ");
            this->microDump();
            printf("\n");
        }
#endif // DEBUG
        relatedInterval = newRelatedInterval;
    }

    // Assign the related interval, but only if it isn't already assigned.
    bool assignRelatedIntervalIfUnassigned(Interval* newRelatedInterval)
    {
        if (relatedInterval == nullptr)
        {
            assignRelatedInterval(newRelatedInterval);
            return true;
        }
        else
        {
#ifdef DEBUG
            if (VERBOSE)
            {
                printf("Interval ");
                this->microDump();
                printf(" already has a related interval\n");
            }
#endif // DEBUG
            return false;
        }
    }

    // Get the current preferences for this Interval.
    // Note that when we have an assigned register we don't necessarily update the
    // registerPreferences to that register, as there may be multiple, possibly disjoint,
    // definitions. This method will return the current assigned register if any, or
    // the 'registerPreferences' otherwise.
    //
    regMaskTP getCurrentPreferences()
    {
        return (assignedReg == nullptr) ? registerPreferences : genRegMask(assignedReg->regNum);
    }

    void mergeRegisterPreferences(regMaskTP preferences)
    {
        // We require registerPreferences to have been initialized.
        assert(registerPreferences != RBM_NONE);
        // It is invalid to update with empty preferences
        assert(preferences != RBM_NONE);

        preferences &= ~registerAversion;
        if (preferences == RBM_NONE)
        {
            // Do not include the preferences if all they contain
            // are the registers we recorded as want to avoid.
            return;
        }

        regMaskTP commonPreferences = (registerPreferences & preferences);
        if (commonPreferences != RBM_NONE)
        {
            registerPreferences = commonPreferences;
            return;
        }

        // There are no preferences in common.
        // Preferences need to reflect both cases where a var must occupy a specific register,
        // as well as cases where a var is live when a register is killed.
        // In the former case, we would like to record all such registers, however we don't
        // really want to use any registers that will interfere.
        // To approximate this, we never "or" together multi-reg sets, which are generally kill sets.

        if (!genMaxOneBit(preferences))
        {
            // The new preference value is a multi-reg set, so it's probably a kill.
            // Keep the new value.
            registerPreferences = preferences;
            return;
        }

        if (!genMaxOneBit(registerPreferences))
        {
            // The old preference value is a multi-reg set.
            // Keep the existing preference set, as it probably reflects one or more kills.
            // It may have been a union of multiple individual registers, but we can't
            // distinguish that case without extra cost.
            return;
        }

        // If we reach here, we have two disjoint single-reg sets.
        // Keep only the callee-save preferences, if not empty.
        // Otherwise, take the union of the preferences.

        regMaskTP newPreferences = registerPreferences | preferences;

        if (preferCalleeSave)
        {
            regMaskTP calleeSaveMask = (LinearScan::calleeSaveRegs(this->registerType) & newPreferences);
            if (calleeSaveMask != RBM_NONE)
            {
                newPreferences = calleeSaveMask;
            }
        }
        registerPreferences = newPreferences;
    }

    // Update the registerPreferences on the interval.
    // If there are conflicting requirements on this interval, set the preferences to
    // the union of them.  That way maybe we'll get at least one of them.
    // An exception is made in the case where one of the existing or new
    // preferences are all callee-save, in which case we "prefer" the callee-save

    void updateRegisterPreferences(regMaskTP preferences)
    {
        // If this interval is preferenced, that interval may have already been assigned a
        // register, and we want to include that in the preferences.
        if ((relatedInterval != nullptr) && !relatedInterval->isActive)
        {
            mergeRegisterPreferences(relatedInterval->getCurrentPreferences());
        }

        // Now merge the new preferences.
        mergeRegisterPreferences(preferences);
    }
};

class RefPosition
{
public:
    // A RefPosition refers to either an Interval or a RegRecord. 'referent' points to one
    // of these types. If it refers to a RegRecord, then 'isPhysRegRef()' is true. If it
    // refers to an Interval, then 'isPhysRegRef()' is false.
    // referent can never be null.

    Referenceable* referent;

    // nextRefPosition is the next in code order.
    // Note that in either case there is no need for these to be doubly linked, as they
    // are only traversed in the forward direction, and are not moved.
    RefPosition* nextRefPosition;

    // The remaining fields are common to both options
    GenTree*     treeNode;
    unsigned int bbNum;

    LsraLocation nodeLocation;

    // Prior to the allocation pass, registerAssignment captures the valid registers
    // for this RefPosition.
    // After the allocation pass, this contains the actual assignment
    regMaskTP registerAssignment;

    RefType refType;

    // NOTE: C++ only packs bitfields if the base type is the same. So make all the base
    // NOTE: types of the logically "bool" types that follow 'unsigned char', so they match
    // NOTE: RefType that precedes this, and multiRegIdx can also match.

    // Indicates whether this ref position is to be allocated a reg only if profitable. Currently these are the
    // ref positions that lower/codegen has indicated as reg optional and is considered a contained memory operand if
    // no reg is allocated.
    unsigned char regOptional : 1;

    // Used by RefTypeDef/Use positions of a multi-reg call node.
    // Indicates the position of the register that this ref position refers to.
    // The max bits needed is based on max value of MAX_MULTIREG_COUNT value
    // across all targets and that happened to be 4 on Arm.  Hence index value
    // would be 0..MAX_MULTIREG_COUNT-1.
    unsigned char multiRegIdx : 2;

#ifdef TARGET_ARM64
    // If this refposition needs consecutive register assignment
    unsigned char needsConsecutive : 1;

    // How many consecutive registers does this and subsequent refPositions need
    unsigned char regCount : 3;
#endif // TARGET_ARM64

    // Last Use - this may be true for multiple RefPositions in the same Interval
    unsigned char lastUse : 1;

    // Spill and Copy info
    //   reload indicates that the value was spilled, and must be reloaded here.
    //   spillAfter indicates that the value is spilled here, so a spill must be added.
    //   singleDefSpill indicates that it is associated with a single-def var and if it
    //      is decided to get spilled, it will be spilled at firstRefPosition def. That
    //      way, the value of stack will always be up-to-date and no more spills or
    //      resolutions (from reg to stack) will be needed for such single-def var.
    //   copyReg indicates that the value needs to be copied to a specific register,
    //      but that it will also retain its current assigned register.
    //   moveReg indicates that the value needs to be moved to a different register,
    //      and that this will be its new assigned register.
    // A RefPosition may have any flag individually or the following combinations:
    //  - reload and spillAfter (i.e. it remains in memory), but not in combination with copyReg or moveReg
    //    (reload cannot exist with copyReg or moveReg; it should be reloaded into the appropriate reg)
    //  - spillAfter and copyReg (i.e. it must be copied to a new reg for use, but is then spilled)
    //  - spillAfter and moveReg (i.e. it most be both spilled and moved)
    //    NOTE: a moveReg involves an explicit move, and would usually not be needed for a fixed Reg if it is going
    //    to be spilled, because the code generator will do the move to the fixed register, and doesn't need to
    //    record the new register location as the new "home" location of the lclVar. However, if there is a conflicting
    //    use at the same location (e.g. lclVar V1 is in rdx and needs to be in rcx, but V2 needs to be in rdx), then
    //    we need an explicit move.
    //  - copyReg and moveReg must not exist with each other.

    unsigned char reload : 1;
    unsigned char spillAfter : 1;
    unsigned char singleDefSpill : 1;
    unsigned char writeThru : 1; // true if this var is defined in a register and also spilled. spillAfter must NOT be
                                 // set.

    unsigned char copyReg : 1;
    unsigned char moveReg : 1; // true if this var is moved to a new register

    unsigned char isPhysRegRef : 1; // true if 'referent' points of a RegRecord, false if it points to an Interval
    unsigned char isFixedRegRef : 1;
    unsigned char isLocalDefUse : 1;

    // delayRegFree indicates that the register should not be freed right away, but instead wait
    // until the next Location after it would normally be freed.  This is used for the case of
    // non-commutative binary operators, where op2 must not be assigned the same register as
    // the target.  We do this by not freeing it until after the target has been defined.
    // Another option would be to actually change the Location of the op2 use until the same
    // Location as the def, but then it could potentially reuse a register that has been freed
    // from the other source(s), e.g. if it's a lastUse or spilled.
    unsigned char delayRegFree : 1;

    // outOfOrder is marked on a (non-def) RefPosition that doesn't follow a definition of the
    // register currently assigned to the Interval.  This happens when we use the assigned
    // register from a predecessor that is not the most recently allocated BasicBlock.
    unsigned char outOfOrder : 1;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    // If upper vector save/restore can be avoided.
    unsigned char skipSaveRestore : 1;
#endif

#ifdef DEBUG
    // Minimum number registers that needs to be ensured while
    // constraining candidates for this ref position under
    // LSRA stress.
    unsigned minRegCandidateCount;

    // The unique RefPosition number, equal to its index in the
    // refPositions list. Only used for debugging dumps.
    unsigned rpNum;

    // Tracks the GenTree* for which this refposition was built.
    // Use for displaying in allocation table.
    GenTree* buildNode;
#endif // DEBUG

    RefPosition(unsigned int bbNum,
                LsraLocation nodeLocation,
                GenTree*     treeNode,
                RefType refType DEBUG_ARG(GenTree* buildNode))
        : referent(nullptr)
        , nextRefPosition(nullptr)
        , treeNode(treeNode)
        , bbNum(bbNum)
        , nodeLocation(nodeLocation)
        , registerAssignment(RBM_NONE)
        , refType(refType)
        , multiRegIdx(0)
#ifdef TARGET_ARM64
        , needsConsecutive(false)
        , regCount(0)
#endif
        , lastUse(false)
        , reload(false)
        , spillAfter(false)
        , singleDefSpill(false)
        , writeThru(false)
        , copyReg(false)
        , moveReg(false)
        , isPhysRegRef(false)
        , isFixedRegRef(false)
        , isLocalDefUse(false)
        , delayRegFree(false)
        , outOfOrder(false)
#ifdef DEBUG
        , minRegCandidateCount(1)
        , rpNum(0)
        , buildNode(buildNode)
#endif
    {
    }

    Interval* getInterval()
    {
        assert(!isPhysRegRef);
        return (Interval*)referent;
    }
    void setInterval(Interval* i)
    {
        referent     = i;
        isPhysRegRef = false;
    }

    RegRecord* getReg()
    {
        assert(isPhysRegRef);
        return (RegRecord*)referent;
    }
    void setReg(RegRecord* r)
    {
        referent           = r;
        isPhysRegRef       = true;
        registerAssignment = genRegMask(r->regNum);
    }

    regNumber assignedReg()
    {
        if (registerAssignment == RBM_NONE)
        {
            return REG_NA;
        }

        return genRegNumFromMask(registerAssignment);
    }

    // Returns true if it is a reference on a GenTree node.
    bool IsActualRef()
    {
        switch (refType)
        {
            case RefTypeDef:
            case RefTypeUse:
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            case RefTypeUpperVectorSave:
            case RefTypeUpperVectorRestore:
#endif
                return true;

            // These must always be marked RegOptional.
            case RefTypeExpUse:
            case RefTypeParamDef:
            case RefTypeDummyDef:
            case RefTypeZeroInit:
                assert(RegOptional());
                return false;

            default:
                return false;
        }
    }

    bool IsPhysRegRef()
    {
        return ((refType == RefTypeFixedReg) || (refType == RefTypeKill));
    }

    void setRegOptional(bool val)
    {
        regOptional = val;
    }

    // Returns true whether this ref position is to be allocated
    // a reg only if it is profitable.
    bool RegOptional()
    {
        // TODO-CQ: Right now if a ref position is marked as
        // copyreg or movereg, then it is not treated as
        // 'allocate if profitable'. This is an implementation
        // limitation that needs to be addressed.
        return regOptional && !copyReg && !moveReg;
    }

    void setMultiRegIdx(unsigned idx)
    {
        multiRegIdx = idx;
        assert(multiRegIdx == idx);
    }

    unsigned getMultiRegIdx()
    {
        return multiRegIdx;
    }

    LsraLocation getRefEndLocation()
    {
        return delayRegFree ? nodeLocation + 1 : nodeLocation;
    }

    RefPosition* getRangeEndRef()
    {
        if (lastUse || nextRefPosition == nullptr || spillAfter)
        {
            return this;
        }
        // It would seem to make sense to only return 'nextRefPosition' if it is a lastUse,
        // and otherwise return `lastRefPosition', but that tends to  excessively lengthen
        // the range for heuristic purposes.
        // TODO-CQ: Look into how this might be improved .
        return nextRefPosition;
    }

    LsraLocation getRangeEndLocation()
    {
        return getRangeEndRef()->getRefEndLocation();
    }

    bool isIntervalRef()
    {
        return (!IsPhysRegRef() && (referent != nullptr));
    }

    // isFixedRefOfRegMask indicates that the RefPosition has a fixed assignment to the register
    // specified by the given mask
    bool isFixedRefOfRegMask(regMaskTP regMask)
    {
        assert(genMaxOneBit(regMask));
        return (registerAssignment == regMask);
    }

    // isFixedRefOfReg indicates that the RefPosition has a fixed assignment to the given register
    bool isFixedRefOfReg(regNumber regNum)
    {
        return (isFixedRefOfRegMask(genRegMask(regNum)));
    }

#ifdef TARGET_ARM64
    /// For consecutive registers, returns true if this RefPosition is
    /// the first of the series.
    FORCEINLINE bool isFirstRefPositionOfConsecutiveRegisters()
    {
        if (needsConsecutive)
        {
            return regCount != 0;
        }
        return false;
    }

#ifdef DEBUG
    bool isLiveAtConsecutiveRegistersLoc(LsraLocation consecutiveRegistersLocation);
#endif
#endif // TARGET_ARM64

    FORCEINLINE bool IsExtraUpperVectorSave() const
    {
        assert(refType == RefTypeUpperVectorSave);
        return (nextRefPosition == nullptr) || (nextRefPosition->refType != RefTypeUpperVectorRestore);
    }
#ifdef DEBUG
    // operator= copies everything except 'rpNum', which must remain unique
    RefPosition& operator=(const RefPosition& rp)
    {
        unsigned rpNumSave = rpNum;
        memcpy(this, &rp, sizeof(rp));
        rpNum = rpNumSave;
        return *this;
    }

    void dump(LinearScan* linearScan);
#endif // DEBUG
};

/*****************************************************************************/
#endif //_LSRA_H_
/*****************************************************************************/
