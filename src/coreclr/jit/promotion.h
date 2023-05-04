// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _PROMOTION_H
#define _PROMOTION_H

#include "compiler.h"
#include "vector.h"

// Represents a single replacement of a (field) access into a struct local.
struct Replacement
{
    unsigned  Offset;
    var_types AccessType;
    unsigned  LclNum;
    // Is the replacement local (given by LclNum) fresher than the value in the struct local?
    bool NeedsWriteBack = true;
    // Is the value in the struct local fresher than the replacement local?
    // Note that the invariant is that this is always false at the entrance to
    // a basic block, i.e. all predecessors would have read the replacement
    // back before transferring control if necessary.
    bool NeedsReadBack = false;
    // Arbitrary flag bit used e.g. by decomposition. Assumed to be false.
    bool Handled = false;
#ifdef DEBUG
    const char* Description;
#endif

    Replacement(unsigned offset, var_types accessType, unsigned lclNum DEBUGARG(const char* description))
        : Offset(offset)
        , AccessType(accessType)
        , LclNum(lclNum)
#ifdef DEBUG
        , Description(description)
#endif
    {
    }

    bool Overlaps(unsigned otherStart, unsigned otherSize) const;
};

class Promotion
{
    Compiler* m_compiler;

    friend class LocalUses;
    friend class LocalsUseVisitor;
    friend class ReplaceVisitor;

    void InsertInitialReadBack(unsigned lclNum, const jitstd::vector<Replacement>& replacements, Statement** prevStmt);
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
                while (mid > 0 && vec[mid - 1].*field == offset)
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

public:
    explicit Promotion(Compiler* compiler) : m_compiler(compiler)
    {
    }

    PhaseStatus Run();
};

class DecompositionStatementList;

class ReplaceVisitor : public GenTreeVisitor<ReplaceVisitor>
{
    Promotion*                    m_prom;
    jitstd::vector<Replacement>** m_replacements;
    bool                          m_madeChanges = false;

public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    ReplaceVisitor(Promotion* prom, jitstd::vector<Replacement>** replacements)
        : GenTreeVisitor(prom->m_compiler), m_prom(prom), m_replacements(replacements)
    {
    }

    bool MadeChanges()
    {
        return m_madeChanges;
    }

    void Reset()
    {
        m_madeChanges = false;
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user);

private:
    void LoadStoreAroundCall(GenTreeCall* call, GenTree* user);
    void ReplaceLocal(GenTree** use, GenTree* user);
    void StoreBeforeReturn(GenTreeUnOp* ret);
    void WriteBackBefore(GenTree** use, unsigned lcl, unsigned offs, unsigned size);
    void MarkForReadBack(unsigned lcl, unsigned offs, unsigned size);

    void HandleAssignment(GenTree** use, GenTree* user);
    bool OverlappingReplacements(GenTreeLclVarCommon* lcl,
                                 Replacement**        firstReplacement,
                                 Replacement**        endReplacement = nullptr);
    void EliminateCommasInBlockOp(GenTreeOp* asg, DecompositionStatementList* result);
    void UpdateEarlyRefCount(GenTree* candidate);
    void IncrementRefCount(unsigned lclNum);
    void InitFieldByField(Replacement*                firstRep,
                          Replacement*                endRep,
                          unsigned char               initVal,
                          DecompositionStatementList* result);
    void CopyIntoFields(Replacement*                firstRep,
                        Replacement*                endRep,
                        GenTreeLclVarCommon*        dst,
                        GenTree*                    src,
                        DecompositionStatementList* result);
};

#endif
