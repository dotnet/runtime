// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _OPTCSE_H
#define _OPTCSE_H

#include "compiler.h"

struct CSEdsc;
class CSE_Candidate;

// Base class for CSE Heuristics
//
// Also usable as a "do nothing" heuristic.
//
class CSE_HeuristicCommon
{
protected:
    CSE_HeuristicCommon(Compiler*);

    Compiler*              m_pCompiler;
    unsigned               m_addCSEcount;
    CSEdsc**               sortTab;
    size_t                 sortSiz;
    bool                   madeChanges;
    Compiler::codeOptimize codeOptKind;

public:
    virtual void Initialize()
    {
    }

    virtual void SortCandidates()
    {
    }

    virtual bool PromotionCheck(CSE_Candidate* candidate)
    {
        return false;
    }
    virtual void PerformCSE(CSE_Candidate* candidate);

    virtual void Cleanup()
    {
    }

    virtual bool ConsiderTree(GenTree* tree)
    {
        return false;
    }

    virtual void AdjustHeuristic(CSE_Candidate* candidate)
    {
    }

    void ConsiderCandidates();

    bool MadeChanges() const
    {
        return madeChanges;
    }

    Compiler::codeOptimize CodeOptKind() const
    {
        return codeOptKind;
    }

    bool IsCompatibleType(var_types cseLclVarTyp, var_types expTyp);
};

#ifdef DEBUG
// Randomized CSE heuristic
//
// Performs CSEs randomly, useful for stress
//
class CSE_HeuristicRandom : public CSE_HeuristicCommon
{

private:
    CLRRandom m_cseRNG;
    unsigned  m_bias;

public:
    CSE_HeuristicRandom(Compiler*);
    void SortCandidates();
    bool PromotionCheck(CSE_Candidate* candidate);
};

#endif

// Standard CSE heuristic
//
//  The following class handles the CSE heuristics
//  we use a complex set of heuristic rules
//  to determine if it is likely to be profitable to perform this CSE
//
class CSE_Heuristic : public CSE_HeuristicCommon
{
private:
    weight_t aggressiveRefCnt;
    weight_t moderateRefCnt;
    unsigned enregCount; // count of the number of predicted enregistered variables
    bool     largeFrame;
    bool     hugeFrame;

public:
    CSE_Heuristic(Compiler*);

    void Initialize();
    void SortCandidates();
    bool PromotionCheck(CSE_Candidate* candidate);
    void AdjustHeuristic(CSE_Candidate* candidate);
};

// Generic list of nodes - used by the CSE logic

struct treeStmtLst
{
    treeStmtLst* tslNext;
    GenTree*     tslTree;  // tree node
    Statement*   tslStmt;  // statement containing the tree
    BasicBlock*  tslBlock; // block containing the statement
};

// The following logic keeps track of expressions via a simple hash table.

struct CSEdsc
{
    CSEdsc*  csdNextInBucket;  // used by the hash table
    size_t   csdHashKey;       // the original hashkey
    ssize_t  csdConstDefValue; // When we CSE similar constants, this is the value that we use as the def
    ValueNum csdConstDefVN;    // When we CSE similar constants, this is the ValueNumber that we use for the LclVar
    // assignment
    unsigned csdIndex;         // 1..optCSECandidateCount
    bool     csdIsSharedConst; // true if this CSE is a shared const
    bool     csdLiveAcrossCall;

    unsigned short csdDefCount; // definition   count
    unsigned short csdUseCount; // use          count  (excluding the implicit uses at defs)

    weight_t csdDefWtCnt; // weighted def count
    weight_t csdUseWtCnt; // weighted use count  (excluding the implicit uses at defs)

    GenTree*    csdTree;  // treenode containing the 1st occurrence
    Statement*  csdStmt;  // stmt containing the 1st occurrence
    BasicBlock* csdBlock; // block containing the 1st occurrence

    treeStmtLst* csdTreeList; // list of matching tree nodes: head
    treeStmtLst* csdTreeLast; // list of matching tree nodes: tail

    // The exception set that is now required for all defs of this CSE.
    // This will be set to NoVN if we decide to abandon this CSE
    ValueNum defExcSetPromise;

    // The set of exceptions we currently can use for CSE uses.
    ValueNum defExcSetCurrent;

    // if all def occurrences share the same conservative normal value
    // number, this will reflect it; otherwise, NoVN.
    // not used for shared const CSE's
    ValueNum defConservNormVN;
};

//  The following class nested within CSE_Heuristic encapsulates the information
//  about the current CSE candidate that is under consideration
//
//  TODO-Cleanup: This is still very much based upon the old Lexical CSE implementation
//  and needs to be reworked for the Value Number based implementation
//
class CSE_Candidate
{
    CSE_HeuristicCommon* m_context;
    CSEdsc*              m_CseDsc;

    unsigned m_cseIndex;
    weight_t m_defCount;
    weight_t m_useCount;
    unsigned m_Cost;
    unsigned m_Size;

    // When this Candidate is successfully promoted to a CSE we record
    // the following information about what category was used when promoting it.
    //
    //  We will set m_Aggressive:
    //    When we believe that the CSE very valuable in terms of weighted ref counts,
    //    such that it would always be enregistered by the register allocator.
    //
    //  We will set m_Moderate:
    //    When we believe that the CSE is moderately valuable in terms of weighted ref counts,
    //    such that it is more likely than not to be enregistered by the register allocator
    //
    //  We will set m_Conservative:
    //    When we didn't set m_Aggressive or  m_Moderate.
    //    Such candidates typically are expensive to compute and thus are
    //    always profitable to promote even when they aren't enregistered.
    //
    //  We will set  m_StressCSE:
    //    When the candidate is only being promoted because of a Stress mode.
    //
    bool m_Aggressive;
    bool m_Moderate;
    bool m_Conservative;
    bool m_StressCSE;

public:
    CSE_Candidate(CSE_HeuristicCommon* context, CSEdsc* cseDsc)
        : m_context(context)
        , m_CseDsc(cseDsc)
        , m_cseIndex(m_CseDsc->csdIndex)
        , m_defCount(0)
        , m_useCount(0)
        , m_Cost(0)
        , m_Size(0)
        , m_Aggressive(false)
        , m_Moderate(false)
        , m_Conservative(false)
        , m_StressCSE(false)
    {
    }

    CSEdsc* CseDsc()
    {
        return m_CseDsc;
    }
    unsigned CseIndex()
    {
        return m_cseIndex;
    }
    weight_t DefCount()
    {
        return m_defCount;
    }
    weight_t UseCount()
    {
        return m_useCount;
    }
    // TODO-CQ: With ValNum CSE's the Expr and its cost can vary.
    GenTree* Expr()
    {
        return m_CseDsc->csdTree;
    }
    unsigned Cost()
    {
        return m_Cost;
    }
    unsigned Size()
    {
        return m_Size;
    }

    bool IsSharedConst()
    {
        return m_CseDsc->csdIsSharedConst;
    }

    bool LiveAcrossCall()
    {
        return m_CseDsc->csdLiveAcrossCall;
    }

    void SetAggressive()
    {
        m_Aggressive = true;
    }

    bool IsAggressive()
    {
        return m_Aggressive;
    }

    void SetModerate()
    {
        m_Moderate = true;
    }

    bool IsModerate()
    {
        return m_Moderate;
    }

    void SetConservative()
    {
        m_Conservative = true;
    }

    bool IsConservative()
    {
        return m_Conservative;
    }

    void SetStressCSE()
    {
        m_StressCSE = true;
    }

    bool IsStressCSE()
    {
        return m_StressCSE;
    }

    void InitializeCounts()
    {
        m_Size = Expr()->GetCostSz(); // always the GetCostSz()
        if (m_context->CodeOptKind() == Compiler::SMALL_CODE)
        {
            m_Cost     = m_Size;                // the estimated code size
            m_defCount = m_CseDsc->csdDefCount; // def count
            m_useCount = m_CseDsc->csdUseCount; // use count (excluding the implicit uses at defs)
        }
        else
        {
            m_Cost     = Expr()->GetCostEx();   // the estimated execution cost
            m_defCount = m_CseDsc->csdDefWtCnt; // weighted def count
            m_useCount = m_CseDsc->csdUseWtCnt; // weighted use count (excluding the implicit uses at defs)
        }
    }
};

#endif // _OPTCSE_H
