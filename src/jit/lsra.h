// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************/

#ifndef _LSRA_H_
#define _LSRA_H_

#include "arraylist.h"
#include "smallhash.h"
#include "nodeinfo.h"

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

typedef var_types RegisterType;
#define IntRegisterType TYP_INT
#define FloatRegisterType TYP_FLOAT

inline regMaskTP calleeSaveRegs(RegisterType rt)
{
    return varTypeIsIntegralOrI(rt) ? RBM_INT_CALLEE_SAVED : RBM_FLT_CALLEE_SAVED;
}

struct LocationInfo
{
    LsraLocation loc;

    // Reg Index in case of multi-reg result producing call node.
    // Indicates the position of the register that this location refers to.
    // The max bits needed is based on max value of MAX_RET_REG_COUNT value
    // across all targets and that happens 4 on on Arm.  Hence index value
    // would be 0..MAX_RET_REG_COUNT-1.
    unsigned multiRegIdx : 2;

    Interval* interval;
    GenTree*  treeNode;

    LocationInfo(LsraLocation l, Interval* i, GenTree* t, unsigned regIdx = 0)
        : loc(l), multiRegIdx(regIdx), interval(i), treeNode(t)
    {
        assert(multiRegIdx == regIdx);
    }

    // default constructor for data structures
    LocationInfo()
    {
    }
};

struct LsraBlockInfo
{
    // bbNum of the predecessor to use for the register location of live-in variables.
    // 0 for fgFirstBB.
    BasicBlock::weight_t weight;
    unsigned int         predBBNum;
    bool                 hasCriticalInEdge;
    bool                 hasCriticalOutEdge;
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

typedef regNumber* VarToRegMap;

template <typename ElementType, CompMemKind MemKind>
class ListElementAllocator
{
private:
    template <typename U, CompMemKind CMK>
    friend class ListElementAllocator;

    Compiler* m_compiler;

public:
    ListElementAllocator(Compiler* compiler) : m_compiler(compiler)
    {
    }

    template <typename U>
    ListElementAllocator(const ListElementAllocator<U, MemKind>& other) : m_compiler(other.m_compiler)
    {
    }

    ElementType* allocate(size_t count)
    {
        return reinterpret_cast<ElementType*>(m_compiler->compGetMem(sizeof(ElementType) * count, MemKind));
    }

    void deallocate(ElementType* pointer, size_t count)
    {
    }

    template <typename U>
    struct rebind
    {
        typedef ListElementAllocator<U, MemKind> allocator;
    };
};

typedef ListElementAllocator<Interval, CMK_LSRA_Interval>       LinearScanMemoryAllocatorInterval;
typedef ListElementAllocator<RefPosition, CMK_LSRA_RefPosition> LinearScanMemoryAllocatorRefPosition;

typedef jitstd::list<Interval, LinearScanMemoryAllocatorInterval>       IntervalList;
typedef jitstd::list<RefPosition, LinearScanMemoryAllocatorRefPosition> RefPositionList;

class Referenceable
{
public:
    Referenceable()
    {
        firstRefPosition  = nullptr;
        recentRefPosition = nullptr;
        lastRefPosition   = nullptr;
        isActive          = false;
    }

    // A linked list of RefPositions.  These are only traversed in the forward
    // direction, and are not moved, so they don't need to be doubly linked
    // (see RefPosition).

    RefPosition* firstRefPosition;
    RefPosition* recentRefPosition;
    RefPosition* lastRefPosition;

    bool isActive;

    // Get the position of the next reference which is at or greater than
    // the current location (relies upon recentRefPosition being udpated
    // during traversal).
    RefPosition* getNextRefPosition();
    LsraLocation getNextRefLocation();
};

class RegRecord : public Referenceable
{
public:
    RegRecord()
    {
        assignedInterval    = nullptr;
        previousInterval    = nullptr;
        regNum              = REG_NA;
        isCalleeSave        = false;
        registerType        = IntRegisterType;
        isBusyUntilNextKill = false;
    }

    void init(regNumber reg)
    {
#ifdef _TARGET_ARM64_
        // The Zero register, or the SP
        if ((reg == REG_ZR) || (reg == REG_SP))
        {
            // IsGeneralRegister returns false for REG_ZR and REG_SP
            regNum       = reg;
            registerType = IntRegisterType;
        }
        else
#endif
            if (emitter::isFloatReg(reg))
        {
            registerType = FloatRegisterType;
        }
        else
        {
            // The constructor defaults to IntRegisterType
            assert(emitter::isGeneralRegister(reg) && registerType == IntRegisterType);
        }
        regNum       = reg;
        isCalleeSave = ((RBM_CALLEE_SAVED & genRegMask(reg)) != 0);
    }

#ifdef DEBUG
    // print out representation
    void dump();
    // concise representation for embedding
    void tinyDump();
#endif // DEBUG

    bool isFree();

    // RefPosition   * getNextRefPosition();
    // LsraLocation    getNextRefLocation();

    // DATA

    // interval to which this register is currently allocated.
    // If the interval is inactive (isActive == false) then it is not currently live,
    // and the register call be unassigned (i.e. setting assignedInterval to nullptr)
    // without spilling the register.
    Interval* assignedInterval;
    // Interval to which this register was previously allocated, and which was unassigned
    // because it was inactive.  This register will be reassigned to this Interval when
    // assignedInterval becomes inactive.
    Interval* previousInterval;

    regNumber    regNum;
    bool         isCalleeSave;
    RegisterType registerType;
    // This register must be considered busy until the next time it is explicitly killed.
    // This is used so that putarg_reg can avoid killing its lclVar source, while avoiding
    // the problem with the reg becoming free if the last-use is encountered before the call.
    bool isBusyUntilNextKill;

    bool conflictingFixedRegReference(RefPosition* refPosition);
};

inline bool leafInRange(GenTree* leaf, int lower, int upper)
{
    if (!leaf->IsIntCnsFitsInI32())
    {
        return false;
    }
    if (leaf->gtIntCon.gtIconVal < lower)
    {
        return false;
    }
    if (leaf->gtIntCon.gtIconVal > upper)
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
    if (leaf->gtIntCon.gtIconVal % multiple)
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
    return leafInRange(leaf->gtOp.gtOp2, lower, upper, multiple);
}

inline bool isCandidateVar(LclVarDsc* varDsc)
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

class LocationInfoList;
class LocationInfoListNodePool;

class LinearScan : public LinearScanInterface
{
    friend class RefPosition;
    friend class Interval;
    friend class Lowering;
    friend class TreeNodeInfo;

public:
    // This could use further abstraction.  From Compiler we need the tree,
    // the flowgraph and the allocator.
    LinearScan(Compiler* theCompiler);

    // This is the main driver
    virtual void doLinearScan();

    // TreeNodeInfo contains three register masks: src candidates, dst candidates, and internal condidates.
    // Instead of storing actual register masks, however, which are large, we store a small index into a table
    // of register masks, stored in this class. We create only as many distinct register masks as are needed.
    // All identical register masks get the same index. The register mask table contains:
    // 1. A mask containing all eligible integer registers.
    // 2. A mask containing all elibible floating-point registers.
    // 3. A mask for each of single register.
    // 4. A mask for each combination of registers, created dynamically as required.
    //
    // Currently, the maximum number of masks allowed is a constant defined by 'numMasks'. The register mask
    // table is never resized. It is also limited by the size of the index, currently an unsigned char.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_ARM64_)
    static const int numMasks = 128;
#else
    static const int numMasks = 64;
#endif

    regMaskTP* regMaskTable;
    int        nextFreeMask;

    typedef int RegMaskIndex;

    // allint is 0, allfloat is 1, all the single-bit masks start at 2
    enum KnownRegIndex
    {
        ALLINT_IDX           = 0,
        ALLFLOAT_IDX         = 1,
        FIRST_SINGLE_REG_IDX = 2
    };

    RegMaskIndex GetIndexForRegMask(regMaskTP mask);
    regMaskTP GetRegMaskForIndex(RegMaskIndex index);
    void RemoveRegisterFromMasks(regNumber reg);

#ifdef DEBUG
    void dspRegisterMaskTable();
#endif // DEBUG

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
    void buildIntervals();

    // This is where the actual assignment is done
    void allocateRegisters();

    // This is the resolution phase, where cross-block mismatches are fixed up
    void resolveRegisters();

    void writeRegisters(RefPosition* currentRefPosition, GenTree* tree);

    // Insert a copy in the case where a tree node value must be moved to a different
    // register at the point of use, or it is reloaded to a different register
    // than the one it was spilled from
    void insertCopyOrReload(BasicBlock* block, GenTreePtr tree, unsigned multiRegIdx, RefPosition* refPosition);

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    // Insert code to save and restore the upper half of a vector that lives
    // in a callee-save register at the point of a call (the upper half is
    // not preserved).
    void insertUpperVectorSaveAndReload(GenTreePtr tree, RefPosition* refPosition, BasicBlock* block);
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

    void addResolution(
        BasicBlock* block, GenTreePtr insertionPoint, Interval* interval, regNumber outReg, regNumber inReg);

    void handleOutgoingCriticalEdges(BasicBlock* block);

    void resolveEdge(BasicBlock* fromBlock, BasicBlock* toBlock, ResolveType resolveType, VARSET_VALARG_TP liveSet);

    void resolveEdges();

    // Finally, the register assignments are written back to the tree nodes.
    void recordRegisterAssignments();

    // Keep track of how many temp locations we'll need for spill
    void initMaxSpill();
    void updateMaxSpill(RefPosition* refPosition);
    void recordMaxSpill();

    // max simultaneous spill locations used of every type
    unsigned int maxSpill[TYP_COUNT];
    unsigned int currentSpill[TYP_COUNT];
    bool         needFloatTmpForFPCall;
    bool         needDoubleTmpForFPCall;

#ifdef DEBUG
private:
    //------------------------------------------------------------------------
    // Should we stress lsra?
    // This uses the same COMPLUS variable as rsStressRegs (COMPlus_JitStressRegs)
    // However, the possible values and their interpretation are entirely different.
    //
    // The mask bits are currently divided into fields in which each non-zero value
    // is a distinct stress option (e.g. 0x3 is not a combination of 0x1 and 0x2).
    // However, subject to possible constraints (to be determined), the different
    // fields can be combined (e.g. 0x7 is a combination of 0x3 and 0x4).
    // Note that the field values are declared in a public enum, but the actual bits are
    // only accessed via accessors.

    unsigned lsraStressMask;

    // This controls the registers available for allocation
    enum LsraStressLimitRegs{LSRA_LIMIT_NONE = 0, LSRA_LIMIT_CALLEE = 0x1, LSRA_LIMIT_CALLER = 0x2,
                             LSRA_LIMIT_SMALL_SET = 0x3, LSRA_LIMIT_MASK = 0x3};

    // When LSRA_LIMIT_SMALL_SET is specified, it is desirable to select a "mixed" set of caller- and callee-save
    // registers, so as to get different coverage than limiting to callee or caller.
    // At least for x86 and AMD64, and potentially other architecture that will support SIMD,
    // we need a minimum of 5 fp regs in order to support the InitN intrinsic for Vector4.
    // Hence the "SmallFPSet" has 5 elements.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_AMD64_)
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
#elif defined(_TARGET_ARM_)
    static const regMaskTP LsraLimitSmallIntSet = (RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_F0 | RBM_F1 | RBM_F2 | RBM_F16 | RBM_F17);
#elif defined(_TARGET_ARM64_)
    static const regMaskTP LsraLimitSmallIntSet = (RBM_R0 | RBM_R1 | RBM_R2 | RBM_R19 | RBM_R20);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_V0 | RBM_V1 | RBM_V2 | RBM_V8 | RBM_V9);
#elif defined(_TARGET_X86_)
    static const regMaskTP LsraLimitSmallIntSet = (RBM_EAX | RBM_ECX | RBM_EDI);
    static const regMaskTP LsraLimitSmallFPSet  = (RBM_XMM0 | RBM_XMM1 | RBM_XMM2 | RBM_XMM6 | RBM_XMM7);
#else
#error Unsupported or unset target architecture
#endif // target

    LsraStressLimitRegs getStressLimitRegs()
    {
        return (LsraStressLimitRegs)(lsraStressMask & LSRA_LIMIT_MASK);
    }
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
    // Note that this can be combined with LsraSpillAlways (or not)
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

    // Dump support
    void lsraDumpIntervals(const char* msg);
    void dumpRefPositions(const char* msg);
    void dumpVarRefPositions(const char* msg);

    static bool IsResolutionMove(GenTree* node);
    static bool IsResolutionNode(LIR::Range& containingRange, GenTree* node);

    void verifyFinalAllocation();
    void verifyResolutionMove(GenTree* resolutionNode, LsraLocation currentLocation);
#else  // !DEBUG
    bool             doSelectNearest()
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
#endif // !DEBUG

public:
    // Used by Lowering when considering whether to split Longs, as well as by identifyCandidates().
    bool isRegCandidate(LclVarDsc* varDsc);

private:
    // Determine which locals are candidates for allocation
    void identifyCandidates();

    // determine which locals are used in EH constructs we don't want to deal with
    void identifyCandidatesExceptionDataflow();

    void buildPhysRegRecords();

    void setLastUses(BasicBlock* block);

    void setFrameType();

    // Update allocations at start/end of block
    void processBlockEndAllocation(BasicBlock* current);

    // Record variable locations at start/end of block
    void processBlockStartLocations(BasicBlock* current, bool allocationPass);
    void processBlockEndLocations(BasicBlock* current);

    RefType CheckBlockType(BasicBlock* block, BasicBlock* prevBlock);

    // insert refpositions representing prolog zero-inits which will be added later
    void insertZeroInitRefPositions();

    void AddMapping(GenTree* node, LsraLocation loc);

    // add physreg refpositions for a tree node, based on calling convention and instruction selection predictions
    void addRefsForPhysRegMask(regMaskTP mask, LsraLocation currentLoc, RefType refType, bool isLastUse);

    void resolveConflictingDefAndUse(Interval* interval, RefPosition* defRefPosition);

    void buildRefPositionsForNode(GenTree*                  tree,
                                  BasicBlock*               block,
                                  LocationInfoListNodePool& listNodePool,
                                  HashTableBase<GenTree*, LocationInfoList>& operandToLocationInfoMap,
                                  LsraLocation loc);

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    VARSET_VALRET_TP buildUpperVectorSaveRefPositions(GenTree* tree, LsraLocation currentLoc);
    void buildUpperVectorRestoreRefPositions(GenTree* tree, LsraLocation currentLoc, VARSET_VALARG_TP liveLargeVectors);
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    // For AMD64 on SystemV machines. This method
    // is called as replacement for raUpdateRegStateForArg
    // that is used on Windows. On System V systems a struct can be passed
    // partially using registers from the 2 register files.
    void unixAmd64UpdateRegStateForArg(LclVarDsc* argDsc);
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

    // Update reg state for an incoming register argument
    void updateRegStateForArg(LclVarDsc* argDsc);

    inline void setTreeNodeInfo(GenTree* tree, TreeNodeInfo info)
    {
        tree->gtLsraInfo = info;
        tree->gtClearReg(compiler);

        DBEXEC(VERBOSE, info.dump(this));
    }

    inline void clearDstCount(GenTree* tree)
    {
        tree->gtLsraInfo.dstCount = 0;
    }

    inline void clearOperandCounts(GenTree* tree)
    {
        TreeNodeInfo& info = tree->gtLsraInfo;
        info.srcCount      = 0;
        info.dstCount      = 0;
    }

    inline bool isLocalDefUse(GenTree* tree)
    {
        return tree->gtLsraInfo.isLocalDefUse;
    }

    inline bool isCandidateLocalRef(GenTree* tree)
    {
        if (tree->IsLocal())
        {
            unsigned int lclNum = tree->gtLclVarCommon.gtLclNum;
            assert(lclNum < compiler->lvaCount);
            LclVarDsc* varDsc = compiler->lvaTable + tree->gtLclVarCommon.gtLclNum;

            return isCandidateVar(varDsc);
        }
        return false;
    }

    static Compiler::fgWalkResult markAddrModeOperandsHelperMD(GenTreePtr tree, void* p);

    // Return the registers killed by the given tree node.
    regMaskTP getKillSetForNode(GenTree* tree);

    // Given some tree node add refpositions for all the registers this node kills
    bool buildKillPositionsForNode(GenTree* tree, LsraLocation currentLoc);

    regMaskTP allRegs(RegisterType rt);
    regMaskTP allRegs(GenTree* tree);
    regMaskTP allMultiRegCallNodeRegs(GenTreeCall* tree);
    regMaskTP allSIMDRegs();
    regMaskTP internalFloatRegCandidates();

    bool registerIsFree(regNumber regNum, RegisterType regType);
    bool registerIsAvailable(RegRecord*    physRegRecord,
                             LsraLocation  currentLoc,
                             LsraLocation* nextRefLocationPtr,
                             RegisterType  regType);
    void freeRegister(RegRecord* physRegRecord);
    void freeRegisters(regMaskTP regsToFree);

    regMaskTP getUseCandidates(GenTree* useNode);
    regMaskTP getDefCandidates(GenTree* tree);
    var_types getDefType(GenTree* tree);

    RefPosition* defineNewInternalTemp(GenTree* tree, RegisterType regType, LsraLocation currentLoc, regMaskTP regMask);

    int buildInternalRegisterDefsForNode(GenTree* tree, LsraLocation currentLoc, RefPosition* defs[]);

    void buildInternalRegisterUsesForNode(GenTree* tree, LsraLocation currentLoc, RefPosition* defs[], int total);

    void resolveLocalRef(BasicBlock* block, GenTreePtr treeNode, RefPosition* currentRefPosition);

    void insertMove(BasicBlock* block, GenTreePtr insertionPoint, unsigned lclNum, regNumber inReg, regNumber outReg);

    void insertSwap(BasicBlock* block,
                    GenTreePtr  insertionPoint,
                    unsigned    lclNum1,
                    regNumber   reg1,
                    unsigned    lclNum2,
                    regNumber   reg2);

public:
    // TODO-Cleanup: unused?
    class PhysRegIntervalIterator
    {
    public:
        PhysRegIntervalIterator(LinearScan* theLinearScan)
        {
            nextRegNumber = (regNumber)0;
            linearScan    = theLinearScan;
        }
        RegRecord* GetNext()
        {
            return &linearScan->physRegs[nextRegNumber];
        }

    private:
        // This assumes that the physical registers are contiguous, starting
        // with a register number of 0
        regNumber   nextRegNumber;
        LinearScan* linearScan;
    };

private:
    Interval* newInterval(RegisterType regType);

    Interval* getIntervalForLocalVar(unsigned varNum)
    {
        return localVarIntervals[varNum];
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

    void associateRefPosWithInterval(RefPosition* rp);

    void associateRefPosWithRegister(RefPosition* rp);

    unsigned getWeight(RefPosition* refPos);

    /*****************************************************************************
     * Register management
     ****************************************************************************/
    RegisterType getRegisterType(Interval* currentInterval, RefPosition* refPosition);
    regNumber tryAllocateFreeReg(Interval* current, RefPosition* refPosition);
    RegRecord* findBestPhysicalReg(RegisterType regType,
                                   LsraLocation endLocation,
                                   regMaskTP    candidates,
                                   regMaskTP    preferences);
    regNumber allocateBusyReg(Interval* current, RefPosition* refPosition, bool allocateIfProfitable);
    regNumber assignCopyReg(RefPosition* refPosition);

    void checkAndAssignInterval(RegRecord* regRec, Interval* interval);
    void assignPhysReg(RegRecord* regRec, Interval* interval);
    void assignPhysReg(regNumber reg, Interval* interval)
    {
        assignPhysReg(getRegisterRecord(reg), interval);
    }

    void checkAndClearInterval(RegRecord* regRec, RefPosition* spillRefPosition);
    void unassignPhysReg(RegRecord* regRec, RefPosition* spillRefPosition);
    void unassignPhysRegNoSpill(RegRecord* reg);
    void unassignPhysReg(regNumber reg)
    {
        unassignPhysReg(getRegisterRecord(reg), nullptr);
    }

    void spillInterval(Interval* interval, RefPosition* fromRefPosition, RefPosition* toRefPosition);

    void spillGCRefs(RefPosition* killRefPosition);

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
    typedef SimplerHashTable<unsigned, SmallPrimitiveKeyFuncs<unsigned>, SplitEdgeInfo, JitSimplerHashBehavior>
                                SplitBBNumToTargetBBNumMap;
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
    regNumber getVarReg(VarToRegMap map, unsigned int varNum);
    // Initialize the incoming VarToRegMap to the given map values (generally a predecessor of
    // the block)
    VarToRegMap setInVarToRegMap(unsigned int bbNum, VarToRegMap srcVarToRegMap);

    regNumber getTempRegForResolution(BasicBlock* fromBlock, BasicBlock* toBlock, var_types type);

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
    //     The Intervals are identifed as Lnnn for lclVar intervals, Innn for for other
    //     intervals, and Tnnn for internal temps.
    //   - In LSRA_DUMP_POST, which is after register allocation, the registers are
    //     shown.

    enum LsraTupleDumpMode{LSRA_DUMP_PRE, LSRA_DUMP_REFPOS, LSRA_DUMP_POST};
    void lsraGetOperandString(GenTreePtr        tree,
                              LsraTupleDumpMode mode,
                              char*             operandString,
                              unsigned          operandStringLength);
    void lsraDispNode(GenTreePtr tree, LsraTupleDumpMode mode, bool hasDest);
    void DumpOperandDefs(GenTree* operand,
                         bool& first,
                         LsraTupleDumpMode mode,
                         char* operandString,
                         const unsigned operandStringLength);
    void TupleStyleDump(LsraTupleDumpMode mode);

    bool         dumpTerse;
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
    char             regNameFormat[MAX_FORMAT_CHARS];
    char             shortRefPositionFormat[MAX_FORMAT_CHARS];
    char             emptyRefPositionFormat[MAX_FORMAT_CHARS];
    char             indentFormat[MAX_FORMAT_CHARS];
    static const int MAX_LEGEND_FORMAT_CHARS = 25;
    char             bbRefPosFormat[MAX_LEGEND_FORMAT_CHARS];
    char             legendFormat[MAX_LEGEND_FORMAT_CHARS];

    // How many rows have we printed since last printing a "title row"?
    static const int MAX_ROWS_BETWEEN_TITLES = 50;
    int              rowCountSinceLastTitle;

    void dumpRegRecordHeader();
    void dumpRegRecordTitle();
    void dumpRegRecordTitleLines();
    int  getLastUsedRegNumIndex();
    void dumpRegRecords();
    // An abbreviated RefPosition dump for printing with column-based register state
    void dumpRefPositionShort(RefPosition* refPosition, BasicBlock* currentBlock);
    // Print the number of spaces occupied by a dumpRefPositionShort()
    void dumpEmptyRefPosition();
    // A dump of Referent, in exactly regColumnWidth characters
    void dumpIntervalName(Interval* interval);

    // Events during the allocation phase that cause some dump output, which differs depending
    // upon whether dumpTerse is set:
    enum LsraDumpEvent{
        // Conflicting def/use
        LSRA_EVENT_DEFUSE_CONFLICT, LSRA_EVENT_DEFUSE_FIXED_DELAY_USE, LSRA_EVENT_DEFUSE_CASE1, LSRA_EVENT_DEFUSE_CASE2,
        LSRA_EVENT_DEFUSE_CASE3, LSRA_EVENT_DEFUSE_CASE4, LSRA_EVENT_DEFUSE_CASE5, LSRA_EVENT_DEFUSE_CASE6,

        // Spilling
        LSRA_EVENT_SPILL, LSRA_EVENT_SPILL_EXTENDED_LIFETIME, LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL,
        LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL_AFTER_SPILL, LSRA_EVENT_DONE_KILL_GC_REFS,

        // Block boundaries
        LSRA_EVENT_START_BB, LSRA_EVENT_END_BB,

        // Miscellaneous
        LSRA_EVENT_FREE_REGS,

        // Characteristics of the current RefPosition
        LSRA_EVENT_INCREMENT_RANGE_END, // ???
        LSRA_EVENT_LAST_USE, LSRA_EVENT_LAST_USE_DELAYED, LSRA_EVENT_NEEDS_NEW_REG,

        // Allocation decisions
        LSRA_EVENT_FIXED_REG, LSRA_EVENT_EXP_USE, LSRA_EVENT_ZERO_REF, LSRA_EVENT_NO_ENTRY_REG_ALLOCATED,
        LSRA_EVENT_KEPT_ALLOCATION, LSRA_EVENT_COPY_REG, LSRA_EVENT_MOVE_REG, LSRA_EVENT_ALLOC_REG,
        LSRA_EVENT_ALLOC_SPILLED_REG, LSRA_EVENT_NO_REG_ALLOCATED, LSRA_EVENT_RELOAD, LSRA_EVENT_SPECIAL_PUTARG,
        LSRA_EVENT_REUSE_REG,
    };
    void dumpLsraAllocationEvent(LsraDumpEvent event,
                                 Interval*     interval     = nullptr,
                                 regNumber     reg          = REG_NA,
                                 BasicBlock*   currentBlock = nullptr);

    void dumpBlockHeader(BasicBlock* block);

    void validateIntervals();
#endif // DEBUG

    Compiler* compiler;

private:
#if MEASURE_MEM_ALLOC
    IAllocator* lsraIAllocator;
#endif

    IAllocator* getAllocator(Compiler* comp)
    {
#if MEASURE_MEM_ALLOC
        if (lsraIAllocator == nullptr)
        {
            lsraIAllocator = new (comp, CMK_LSRA) CompAllocator(comp, CMK_LSRA);
        }
        return lsraIAllocator;
#else
        return comp->getAllocator();
#endif
    }

#ifdef DEBUG
    // This is used for dumping
    RefPosition* activeRefPosition;
#endif // DEBUG

    IntervalList intervals;

    RegRecord physRegs[REG_COUNT];

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
    void addToBlockSequenceWorkList(BlockSet sequencedBlockSet, BasicBlock* block);
    void removeFromBlockSequenceWorkList(BasicBlockList* listNode, BasicBlockList* prevNode);
    BasicBlock* getNextCandidateFromWorkList();

    // The bbNum of the block being currently allocated or resolved.
    unsigned int curBBNum;
    // The ordinal of the block we're on (i.e. this is the curBBSeqNum-th block we've allocated).
    unsigned int curBBSeqNum;
    // The number of blocks that we've sequenced.
    unsigned int bbSeqCount;
    // The Location of the start of the current block.
    LsraLocation curBBStartLocation;

    // Ordered list of RefPositions
    RefPositionList refPositions;

    // Per-block variable location mappings: an array indexed by block number that yields a
    // pointer to an array of regNumber, one per variable.
    VarToRegMap* inVarToRegMaps;
    VarToRegMap* outVarToRegMaps;

    // A temporary VarToRegMap used during the resolution of critical edges.
    VarToRegMap sharedCriticalVarToRegMap;

    PhasedVar<regMaskTP> availableIntRegs;
    PhasedVar<regMaskTP> availableFloatRegs;
    PhasedVar<regMaskTP> availableDoubleRegs;

    // Current set of live tracked vars, used during building of RefPositions to determine whether
    // to preference to callee-save
    VARSET_TP currentLiveVars;
    // Set of floating point variables to consider for callee-save registers.
    VARSET_TP fpCalleeSaveCandidateVars;
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
#if defined(_TARGET_AMD64_)
    static const var_types LargeVectorType     = TYP_SIMD32;
    static const var_types LargeVectorSaveType = TYP_SIMD16;
#elif defined(_TARGET_ARM64_)
    static const var_types LargeVectorType      = TYP_SIMD16;
    static const var_types LargeVectorSaveType  = TYP_DOUBLE;
#else // !defined(_TARGET_AMD64_) && !defined(_TARGET_ARM64_)
#error("Unknown target architecture for FEATURE_SIMD")
#endif // !defined(_TARGET_AMD64_) && !defined(_TARGET_ARM64_)

    // Set of large vector (TYP_SIMD32 on AVX) variables.
    VARSET_TP largeVectorVars;
    // Set of large vector (TYP_SIMD32 on AVX) variables to consider for callee-save registers.
    VARSET_TP largeVectorCalleeSaveCandidateVars;
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
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
        , relatedInterval(nullptr)
        , assignedReg(nullptr)
        , registerType(registerType)
        , isLocalVar(false)
        , isSplit(false)
        , isSpilled(false)
        , isInternal(false)
        , isStructField(false)
        , isPromotedStruct(false)
        , hasConflictingDefUse(false)
        , hasNonCommutativeRMWDef(false)
        , isSpecialPutArg(false)
        , preferCalleeSave(false)
        , isConstant(false)
        , physReg(REG_COUNT)
#ifdef DEBUG
        , intervalIndex(0)
#endif
        , varNum(0)
    {
    }

#ifdef DEBUG
    // print out representation
    void dump();
    // concise representation for embedding
    void tinyDump();
    // extremely concise representation
    void microDump();
#endif // DEBUG

    void setLocalNumber(unsigned localNum, LinearScan* l);

    // Fixed registers for which this Interval has a preference
    regMaskTP registerPreferences;

    // The relatedInterval is:
    //  - for any other interval, it is the interval to which this interval
    //    is currently preferenced (e.g. because they are related by a copy)
    Interval* relatedInterval;

    // The assignedReg is the RecRecord for the register to which this interval
    // has been assigned at some point - if the interval is active, this is the
    // register it currently occupies.
    RegRecord* assignedReg;

    // DECIDE : put this in a union or do something w/ inheritance?
    // this is an interval for a physical register, not a allocatable entity

    RegisterType registerType;
    bool         isLocalVar : 1;
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
    // true if this interval is defined by a non-commutative 2-operand instruction
    bool hasNonCommutativeRMWDef : 1;

    // True if this interval is defined by a putArg, whose source is a non-last-use lclVar.
    // During allocation, this flag will be cleared if the source is not already in the required register.
    // Othewise, we will leave the register allocated to the lclVar, but mark the RegRecord as
    // isBusyUntilNextKill, so that it won't be reused if the lclVar goes dead before the call.
    bool isSpecialPutArg : 1;

    // True if this interval interferes with a call.
    bool preferCalleeSave : 1;

    // True if this interval is defined by a constant node that may be reused and/or may be
    // able to reuse a constant that's already in a register.
    bool isConstant : 1;

    // The register to which it is currently assigned.
    regNumber physReg;

#ifdef DEBUG
    unsigned int intervalIndex;
#endif // DEBUG

    unsigned int varNum; // This is the "variable number": the index into the lvaTable array

    LclVarDsc* getLocalVar(Compiler* comp)
    {
        assert(isLocalVar);
        return &(comp->lvaTable[this->varNum]);
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
    void assignRelatedIntervalIfUnassigned(Interval* newRelatedInterval)
    {
        if (relatedInterval == nullptr)
        {
            assignRelatedInterval(newRelatedInterval);
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
        }
    }

    // Update the registerPreferences on the interval.
    // If there are conflicting requirements on this interval, set the preferences to
    // the union of them.  That way maybe we'll get at least one of them.
    // An exception is made in the case where one of the existing or new
    // preferences are all callee-save, in which case we "prefer" the callee-save

    void updateRegisterPreferences(regMaskTP preferences)
    {
        // We require registerPreferences to have been initialized.
        assert(registerPreferences != RBM_NONE);
        // It is invalid to update with empty preferences
        assert(preferences != RBM_NONE);

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
            regMaskTP calleeSaveMask = (calleeSaveRegs(this->registerType) & (newPreferences));
            if (calleeSaveMask != RBM_NONE)
            {
                newPreferences = calleeSaveMask;
            }
        }
        registerPreferences = newPreferences;
    }
};

class RefPosition
{
public:
    RefPosition(unsigned int bbNum, LsraLocation nodeLocation, GenTree* treeNode, RefType refType)
        : referent(nullptr)
        , nextRefPosition(nullptr)
        , treeNode(treeNode)
        , bbNum(bbNum)
        , nodeLocation(nodeLocation)
        , registerAssignment(RBM_NONE)
        , refType(refType)
        , multiRegIdx(0)
        , lastUse(false)
        , reload(false)
        , spillAfter(false)
        , copyReg(false)
        , moveReg(false)
        , isPhysRegRef(false)
        , isFixedRegRef(false)
        , isLocalDefUse(false)
        , delayRegFree(false)
        , outOfOrder(false)
#ifdef DEBUG
        , rpNum(0)
#endif
    {
    }

    // A RefPosition refers to either an Interval or a RegRecord. 'referent' points to one
    // of these types. If it refers to a RegRecord, then 'isPhysRegRef' is true. If it
    // refers to an Interval, then 'isPhysRegRef' is false.
    //
    // Q: can 'referent' be NULL?

    Referenceable* referent;

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

    // nextRefPosition is the next in code order.
    // Note that in either case there is no need for these to be doubly linked, as they
    // are only traversed in the forward direction, and are not moved.
    RefPosition* nextRefPosition;

    // The remaining fields are common to both options
    GenTree*     treeNode;
    unsigned int bbNum;

    // Prior to the allocation pass, registerAssignment captures the valid registers
    // for this RefPosition. An empty set means that any register is valid.  A non-empty
    // set means that it must be one of the given registers (may be the full set if the
    // only constraint is that it must reside in SOME register)
    // After the allocation pass, this contains the actual assignment
    LsraLocation nodeLocation;
    regMaskTP    registerAssignment;

    regNumber assignedReg()
    {
        if (registerAssignment == RBM_NONE)
        {
            return REG_NA;
        }

        return genRegNumFromMask(registerAssignment);
    }

    RefType refType;

    // Returns true if it is a reference on a gentree node.
    bool IsActualRef()
    {
        return (refType == RefTypeDef || refType == RefTypeUse);
    }

    bool RequiresRegister()
    {
        return (IsActualRef()
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                || refType == RefTypeUpperVectorSaveDef || refType == RefTypeUpperVectorSaveUse
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                ) &&
               !AllocateIfProfitable();
    }

    // Indicates whether this ref position is to be allocated
    // a reg only if profitable. Currently these are the
    // ref positions that lower/codegen has indicated as reg
    // optional and is considered a contained memory operand if
    // no reg is allocated.
    unsigned allocRegIfProfitable : 1;

    void setAllocateIfProfitable(unsigned val)
    {
        allocRegIfProfitable = val;
    }

    // Returns true whether this ref position is to be allocated
    // a reg only if it is profitable.
    bool AllocateIfProfitable()
    {
        // TODO-CQ: Right now if a ref position is marked as
        // copyreg or movereg, then it is not treated as
        // 'allocate if profitable'. This is an implementation
        // limitation that needs to be addressed.
        return allocRegIfProfitable && !copyReg && !moveReg;
    }

    // Used by RefTypeDef/Use positions of a multi-reg call node.
    // Indicates the position of the register that this ref position refers to.
    // The max bits needed is based on max value of MAX_RET_REG_COUNT value
    // across all targets and that happens 4 on on Arm.  Hence index value
    // would be 0..MAX_RET_REG_COUNT-1.
    unsigned multiRegIdx : 2;

    void setMultiRegIdx(unsigned idx)
    {
        multiRegIdx = idx;
        assert(multiRegIdx == idx);
    }

    unsigned getMultiRegIdx()
    {
        return multiRegIdx;
    }

    // Last Use - this may be true for multiple RefPositions in the same Interval
    bool lastUse : 1;

    // Spill and Copy info
    //   reload indicates that the value was spilled, and must be reloaded here.
    //   spillAfter indicates that the value is spilled here, so a spill must be added.
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

    bool reload : 1;
    bool spillAfter : 1;
    bool copyReg : 1;
    bool moveReg : 1; // true if this var is moved to a new register

    bool isPhysRegRef : 1; // true if 'referent' points of a RegRecord, false if it points to an Interval
    bool isFixedRegRef : 1;
    bool isLocalDefUse : 1;

    // delayRegFree indicates that the register should not be freed right away, but instead wait
    // until the next Location after it would normally be freed.  This is used for the case of
    // non-commutative binary operators, where op2 must not be assigned the same register as
    // the target.  We do this by not freeing it until after the target has been defined.
    // Another option would be to actually change the Location of the op2 use until the same
    // Location as the def, but then it could potentially reuse a register that has been freed
    // from the other source(s), e.g. if it's a lastUse or spilled.
    bool delayRegFree : 1;

    // outOfOrder is marked on a (non-def) RefPosition that doesn't follow a definition of the
    // register currently assigned to the Interval.  This happens when we use the assigned
    // register from a predecessor that is not the most recently allocated BasicBlock.
    bool outOfOrder : 1;

    LsraLocation getRefEndLocation()
    {
        return delayRegFree ? nodeLocation + 1 : nodeLocation;
    }

#ifdef DEBUG
    unsigned rpNum; // The unique RefPosition number, equal to its index in the refPositions list. Only used for
                    // debugging dumps.
#endif              // DEBUG

    bool isIntervalRef()
    {
        return (!isPhysRegRef && (referent != nullptr));
    }

    // isTrueDef indicates that the RefPosition is a non-update def of a non-internal
    // interval
    bool isTrueDef()
    {
        return (refType == RefTypeDef && isIntervalRef() && !getInterval()->isInternal);
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

#ifdef DEBUG
    // operator= copies everything except 'rpNum', which must remain unique
    RefPosition& operator=(const RefPosition& rp)
    {
        unsigned rpNumSave = rpNum;
        memcpy(this, &rp, sizeof(rp));
        rpNum = rpNumSave;
        return *this;
    }

    void dump();
#endif // DEBUG
};

#ifdef DEBUG
void dumpRegMask(regMaskTP regs);
#endif // DEBUG

/*****************************************************************************/
#endif //_LSRA_H_
/*****************************************************************************/
