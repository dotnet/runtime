// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//===============================================================================
#include "phase.h"

//------------------------------------------------------------------------------
// Location - (tree, block) tuple is minimum context required to manipulate trees in the JIT
//------------------------------------------------------------------------------
class Location
{
public:
    GenTree* tree;
    BasicBlock* block;

    Location() : tree(nullptr), block(nullptr) {}

    Location(GenTree* t, BasicBlock* b) : tree(t), block(b)
    {
        DBEXEC(TRUE, Validate());
    }

    // construct a location consisting of the first tree after the start of the given block
    // (and the corresponding block, which may not be the same as the one passed in)
    Location(BasicBlock* b) : tree(nullptr), block(b)
    {
        Initialize();
    }

#ifdef DEBUG
    // Validate - basic validation that this (tree, block) tuple forms a real location
    void Validate()
    {
        if (tree != nullptr)
        {
            assert(Compiler::fgBlockContainsStatementBounded(block, tree));
            assert(tree->gtOper == GT_STMT);
        }
    }
#endif // DEBUG

    // Next - skip to next location,
    //        which means next tree in block, or next block's first tree, or a null location
    Location Next()
    {
        tree = tree->gtNext;
        while (tree == nullptr)
        {
            block = block->bbNext;
            if (block == nullptr)
            {
                return Location();
            }
            tree = block->bbTreeList;
        }
        assert(tree != nullptr);
        assert(tree->gtOper == GT_STMT);
        return *this;
    }

    void Reset(Compiler* comp)
    {
        block = comp->fgFirstBB;
        tree = nullptr;
        Initialize();
    }

private:
    void Initialize()
    {
        assert(tree == nullptr);
        tree = block->bbTreeList;
        while (tree == nullptr)
        {
            block = block->bbNext;
            if (block == nullptr)
            {
                block = nullptr;
                tree = nullptr;
                break;
            }
            tree = block->bbTreeList;
        }
        DBEXEC(TRUE, Validate());
    }
};

class Rationalizer : public Phase
{
    //===============================================================================
    // Methods
public:
    Rationalizer(Compiler* comp);
    Location TreeTransformRationalization (Location loc);

#ifdef DEBUG

    static void ValidateStatement(Location loc);
    static void ValidateStatement(GenTree* tree, BasicBlock* block);

    // general purpose sanity checking of de facto standard GenTree
    void SanityCheck(); 

    // sanity checking of rationalized IR
    void SanityCheckRational(); 

#endif // DEBUG

    virtual void DoPhase();
    typedef      ArrayStack<GenTree*> GenTreeStack;
    static void  MorphAsgIntoStoreLcl (GenTreeStmt* stmt, GenTreePtr pTree);

private:
    static Compiler::fgWalkResult CommaHelper          (GenTree** ppTree, Compiler::fgWalkData* data);
    static void                   RewriteOneComma      (GenTree** ppTree, Compiler::fgWalkData* data);
    static bool                   CommaUselessChild    (GenTree** ppTree, Compiler::fgWalkData* data);
    static void                   RecursiveRewriteComma(GenTree** ppTree, Compiler::fgWalkData* data, bool discard, bool nested);
    static bool                   RewriteArrElem       (GenTree** ppTree, Compiler::fgWalkData* data);

    static Compiler::fgWalkResult SimpleTransformHelper(GenTree** ppTree, Compiler::fgWalkData* data);

    static void       DuplicateCommaProcessOneTree (Compiler* comp, Rationalizer* irt, BasicBlock* block, GenTree* tree);

    static void       FixupIfCallArg               (GenTreeStack* parentStack,
                                                    GenTree* oldChild, 
                                                    GenTree* newChild);

    static void       FixupIfSIMDLocal             (Compiler* comp, GenTreeLclVarCommon* tree);

    static GenTreePtr CreateTempAssignment         (Compiler* comp,
                                                    unsigned lclNum,
                                                    GenTreePtr rhs);

    Location RewriteTopLevelComma(Location loc);
    
    // SIMD related transformations
    static void RewriteObj(GenTreePtr* ppTree, Compiler::fgWalkData* data);
    static void RewriteCopyBlk(GenTreePtr* ppTree, Compiler::fgWalkData* data);
    static void RewriteInitBlk(GenTreePtr* ppTree, Compiler::fgWalkData* data);

    // Intrinsic related    
    static void RewriteNodeAsCall(GenTreePtr* ppTree, Compiler::fgWalkData* data,
        CORINFO_METHOD_HANDLE callHnd,
#ifdef FEATURE_READYTORUN_COMPILER
        CORINFO_CONST_LOOKUP entryPoint,
#endif
        GenTreeArgList* args);
    static void RewriteIntrinsicAsUserCall(GenTreePtr* ppTree, Compiler::fgWalkData* data);    
};

inline Rationalizer::Rationalizer(Compiler* _comp)
                    : Phase(_comp, "IR Rationalize", PHASE_RATIONALIZE)
{
#ifdef DEBUG
    comp->compNumStatementLinksTraversed = 0;
#endif
}
