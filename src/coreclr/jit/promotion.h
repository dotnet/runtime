// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _PROMOTION_H
#define _PROMOTION_H

#include "compiler.h"
#include "vector.h"

// We limit the max number of fields that can be promoted in a single struct to
// avoid pathological cases (e.g. machine generated code). Furthermore,
// writebacks before struct uses introduce commas with nested trees for each
// field written back, so without a limit we could create arbitrarily deep
// trees.
const int PHYSICAL_PROMOTION_MAX_PROMOTIONS_PER_STRUCT = 64;

// Represents a single replacement of a (field) access into a struct local.
struct Replacement
{
    unsigned  Offset;
    var_types AccessType;
    unsigned  LclNum = BAD_VAR_NUM;
    // Is the replacement local (given by LclNum) fresher than the value in the struct local?
    bool NeedsWriteBack = true;
    // Is the value in the struct local fresher than the replacement local?
    // Note that the invariant is that this is always false at the entrance to
    // a basic block, i.e. all predecessors would have read the replacement
    // back before transferring control if necessary.
    bool NeedsReadBack = false;
#ifdef DEBUG
    const char* Description = "";
#endif

    Replacement(unsigned offset, var_types accessType) : Offset(offset), AccessType(accessType)
    {
    }

    bool Overlaps(unsigned otherStart, unsigned otherSize) const;
};

// Represents significant segments of a struct operation.
//
// Essentially a segment tree (but not stored as a tree) that supports boolean
// Add/Subtract operations of segments. Used to compute the remainder after
// replacements have been handled as part of a decomposed block operation.
class StructSegments
{
public:
    struct Segment
    {
        unsigned Start = 0;
        unsigned End   = 0;

        Segment()
        {
        }

        Segment(unsigned start, unsigned end) : Start(start), End(end)
        {
        }

        bool IntersectsOrAdjacent(const Segment& other) const;
        bool Intersects(const Segment& other) const;
        bool Contains(const Segment& other) const;
        void Merge(const Segment& other);
    };

private:
    jitstd::vector<Segment> m_segments;

public:
    explicit StructSegments(CompAllocator allocator) : m_segments(allocator)
    {
    }

    void Add(const Segment& segment);
    void Subtract(const Segment& segment);
    bool IsEmpty();
    bool CoveringSegment(Segment* result);
    bool Intersects(const Segment& segment);

#ifdef DEBUG
    void Dump();
#endif
};

// Represents information about an aggregate that now has replacements in it.
struct AggregateInfo
{
    jitstd::vector<Replacement> Replacements;
    unsigned                    LclNum;
    // Unpromoted parts of the struct local.
    StructSegments Unpromoted;
    // Min offset in the struct local of the unpromoted part.
    unsigned UnpromotedMin = 0;
    // Max offset in the struct local of the unpromoted part.
    unsigned UnpromotedMax = 0;

    AggregateInfo(CompAllocator alloc, unsigned lclNum) : Replacements(alloc), LclNum(lclNum), Unpromoted(alloc)
    {
    }

    bool OverlappingReplacements(unsigned      offset,
                                 unsigned      size,
                                 Replacement** firstReplacement,
                                 Replacement** endReplacement);
};

// Map that stores information about promotions made for each local.
class AggregateInfoMap
{
    jitstd::vector<AggregateInfo*> m_aggregates;
    unsigned                       m_numLocals;
    unsigned*                      m_lclNumToAggregateIndex;

public:
    AggregateInfoMap(CompAllocator allocator, unsigned numLocals);
    void Add(AggregateInfo* agg);
    AggregateInfo* Lookup(unsigned lclNum);

    jitstd::vector<AggregateInfo*>::iterator begin()
    {
        return m_aggregates.begin();
    }

    jitstd::vector<AggregateInfo*>::iterator end()
    {
        return m_aggregates.end();
    }
};

typedef JitHashTable<ClassLayout*, JitPtrKeyFuncs<ClassLayout>, class StructSegments*> ClassLayoutStructSegmentsMap;

class Promotion
{
    Compiler*                     m_compiler;
    ClassLayoutStructSegmentsMap* m_significantSegmentsCache = nullptr;

    friend class LocalUses;
    friend class LocalsUseVisitor;
    friend struct AggregateInfo;
    friend class PromotionLiveness;
    friend class ReplaceVisitor;
    friend class DecompositionPlan;
    friend class StructSegments;

    StructSegments SignificantSegments(ClassLayout* layout);

    void ExplicitlyZeroInitReplacementLocals(unsigned                           lclNum,
                                             const jitstd::vector<Replacement>& replacements,
                                             Statement**                        prevStmt);
    void InsertInitStatement(Statement** prevStmt, GenTree* tree);
    static GenTree* CreateWriteBack(Compiler* compiler, unsigned structLclNum, const Replacement& replacement);
    static GenTree* CreateReadBack(Compiler* compiler, unsigned structLclNum, const Replacement& replacement);

    //------------------------------------------------------------------------
    // BinarySearch:
    //   Find first entry with an equal offset, or bitwise complement of first
    //   entry with a higher offset.
    //
    // Parameters:
    //   vec    - The vector to binary search in
    //   offset - The offset to search for
    //
    // Returns:
    //    Index of the first entry with an equal offset, or bitwise complement of
    //    first entry with a higher offset.
    //
    template <typename T, unsigned(T::*field)>
    static size_t BinarySearch(const jitstd::vector<T>& vec, unsigned offset)
    {
        size_t min = 0;
        size_t max = vec.size();
        while (min < max)
        {
            size_t mid = min + (max - min) / 2;
            if (vec[mid].*field == offset)
            {
                while ((mid > 0) && (vec[mid - 1].*field == offset))
                {
                    mid--;
                }

                return mid;
            }
            if (vec[mid].*field < offset)
            {
                min = mid + 1;
            }
            else
            {
                max = mid;
            }
        }

        return ~min;
    }

    bool HaveCandidateLocals();

    static bool IsCandidateForPhysicalPromotion(LclVarDsc* dsc);
    static GenTree* EffectiveUser(Compiler::GenTreeStack& ancestors);

public:
    explicit Promotion(Compiler* compiler) : m_compiler(compiler)
    {
    }

    PhaseStatus Run();
};

// Class to represent liveness information for a struct local's fields and remainder.
class StructDeaths
{
    BitVec         m_deaths;
    AggregateInfo* m_aggregate = nullptr;

    friend class PromotionLiveness;

private:
    StructDeaths(BitVec deaths, AggregateInfo* agg) : m_deaths(deaths), m_aggregate(agg)
    {
    }

public:
    StructDeaths() : m_deaths(BitVecOps::UninitVal())
    {
    }

    bool IsRemainderDying() const;
    bool IsReplacementDying(unsigned index) const;
};

struct BasicBlockLiveness;

// Class to compute and track liveness information pertaining promoted structs.
class PromotionLiveness
{
    Compiler*           m_compiler;
    AggregateInfoMap&   m_aggregates;
    BitVecTraits*       m_bvTraits                = nullptr;
    unsigned*           m_structLclToTrackedIndex = nullptr;
    unsigned            m_numVars                 = 0;
    BasicBlockLiveness* m_bbInfo                  = nullptr;
    bool                m_hasPossibleBackEdge     = false;
    BitVec              m_liveIn;
    BitVec              m_ehLiveVars;
    JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, BitVec> m_aggDeaths;

public:
    PromotionLiveness(Compiler* compiler, AggregateInfoMap& aggregates)
        : m_compiler(compiler), m_aggregates(aggregates), m_aggDeaths(compiler->getAllocator(CMK_Promotion))
    {
    }

    void Run();
    bool IsReplacementLiveIn(BasicBlock* bb, unsigned structLcl, unsigned replacement);
    bool IsReplacementLiveOut(BasicBlock* bb, unsigned structLcl, unsigned replacement);
    StructDeaths GetDeathsForStructLocal(GenTreeLclVarCommon* use);

private:
    void MarkUseDef(GenTreeLclVarCommon* lcl, BitVec& useSet, BitVec& defSet);
    void MarkIndex(unsigned index, bool isUse, bool isDef, BitVec& useSet, BitVec& defSet);
    void ComputeUseDefSets();
    void InterBlockLiveness();
    bool PerBlockLiveness(BasicBlock* block);
    void AddHandlerLiveVars(BasicBlock* block, BitVec& ehLiveVars);
    void FillInLiveness();
    void FillInLiveness(BitVec& life, BitVec volatileVars, GenTreeLclVarCommon* lcl);
#ifdef DEBUG
    void DumpVarSet(BitVec set, BitVec allVars);
#endif
};

class DecompositionStatementList;
class DecompositionPlan;

class ReplaceVisitor : public GenTreeVisitor<ReplaceVisitor>
{
    friend class DecompositionPlan;

    Promotion*         m_promotion;
    AggregateInfoMap&  m_aggregates;
    PromotionLiveness* m_liveness;
    bool               m_madeChanges         = false;
    unsigned           m_numPendingReadBacks = 0;
    bool               m_mayHaveForwardSub   = false;
    Statement*         m_currentStmt         = nullptr;
    BasicBlock*        m_currentBlock        = nullptr;

public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true,
        ComputeStack      = true,
    };

    ReplaceVisitor(Promotion* prom, AggregateInfoMap& aggregates, PromotionLiveness* liveness)
        : GenTreeVisitor(prom->m_compiler), m_promotion(prom), m_aggregates(aggregates), m_liveness(liveness)
    {
    }

    bool MadeChanges()
    {
        return m_madeChanges;
    }

    bool MayHaveForwardSubOpportunity()
    {
        return m_mayHaveForwardSub;
    }

    void StartBlock(BasicBlock* block);
    void EndBlock();
    void StartStatement(Statement* stmt);

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user);

private:
    void SetNeedsWriteBack(Replacement& rep);
    void ClearNeedsWriteBack(Replacement& rep);
    void SetNeedsReadBack(Replacement& rep);
    void ClearNeedsReadBack(Replacement& rep);

    template <typename Func>
    void VisitOverlappingReplacements(unsigned lcl, unsigned offs, unsigned size, Func func);

    void      InsertPreStatementReadBacks();
    void      InsertPreStatementWriteBacks();
    GenTree** InsertMidTreeReadBacks(GenTree** use);

    void ReadBackAfterCall(GenTreeCall* call, GenTree* user);
    bool IsPromotedStructLocalDying(GenTreeLclVarCommon* structLcl);
    void ReplaceLocal(GenTree** use, GenTree* user);
    void CheckForwardSubForLastUse(unsigned lclNum);
    void WriteBackBeforeCurrentStatement(unsigned lcl, unsigned offs, unsigned size);
    void WriteBackBeforeUse(GenTree** use, unsigned lcl, unsigned offs, unsigned size);
    void MarkForReadBack(GenTreeLclVarCommon* lcl, unsigned size DEBUGARG(const char* reason));

    void HandleStructStore(GenTree** use, GenTree* user);
    bool OverlappingReplacements(GenTreeLclVarCommon* lcl,
                                 Replacement**        firstReplacement,
                                 Replacement**        endReplacement = nullptr);
    void EliminateCommasInBlockOp(GenTree* store, DecompositionStatementList* result);
    void InitFields(GenTreeLclVarCommon* dstStore, Replacement* firstRep, Replacement* endRep, DecompositionPlan* plan);
    void CopyBetweenFields(GenTree*                    store,
                           Replacement*                dstFirstRep,
                           Replacement*                dstEndRep,
                           GenTree*                    src,
                           Replacement*                srcFirstRep,
                           Replacement*                srcEndRep,
                           DecompositionStatementList* statements,
                           DecompositionPlan*          plan);
#ifdef DEBUG
    const char* LastUseString(GenTreeLclVarCommon* lcl, Replacement* rep);
#endif
};

#endif
