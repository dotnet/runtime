// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//

//
// ==--==

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  SSA                                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once
#pragma warning(disable:4503) // 'identifier' : decorated name length exceeded, name was truncated

#undef SSA_FEATURE_USEDEF
#undef SSA_FEATURE_DOMARR

#include "compiler.h"

struct SsaRenameState;

typedef int LclVarNum;

// Pair of a local var name eg: V01 and Ssa number; eg: V01_01
typedef jitstd::pair<LclVarNum, int> SsaVarName;

class SsaBuilder
{
private:
    struct SsaVarNameHasher
    {
        /**
         * Hash functor used in maps to hash a given key.
         *
         * @params key SsaVarName which is a pair of lclNum and ssaNum which defines a variable.
         * @return Hash value corresponding to a key.
         */
        size_t operator()(const SsaVarName& key) const
        {
            return jitstd::hash<__int64>()((((__int64)key.first) << sizeof(int)) | key.second);
        }
    };

    // Used to maintain a map of a given SSA numbering to its use or def.
    typedef jitstd::unordered_map<SsaVarName, jitstd::vector<GenTree*>, SsaVarNameHasher> VarToUses;
    typedef jitstd::unordered_map<SsaVarName, GenTree*, SsaVarNameHasher> VarToDef;

    inline void EndPhase(Phases phase)
    {
        m_pCompiler->EndPhase(phase);
    }

public:

    // Constructor
    SsaBuilder(Compiler* pCompiler, IAllocator* pIAllocator);

    // Requires stmt nodes to be already sequenced in evaluation order. Analyzes the graph
    // for introduction of phi-nodes as GT_PHI tree nodes at the beginning of each block.
    // Each GT_LCL_VAR is given its ssa number through its gtSsaNum field in the node.
    // Each GT_PHI node will have gtOp1 set to lhs of the phi node and the gtOp2 to be a
    // GT_LIST of GT_PHI_ARG. Each use or def is denoted by the corresponding GT_LCL_VAR
    // tree. For example, to get all uses of a particular variable fully defined by its
    // lclNum and ssaNum, one would use m_uses and look up all the uses. Similarly, a single
    // def of an SSA variable can be looked up similarly using m_defs member.
    void Build();

    // Requires "bbIDom" of each block to be computed. Requires "domTree" to be allocated
    // and can be updated, i.e., by adding mapping from a block to it's dominated children.
    // Using IDom of each basic block, compute the whole domTree. If a block "b" has IDom "i",
    // then, block "b" is dominated by "i". The mapping then is i -> { ..., b, ... }, in
    // other words, "domTree" is a tree represented by nodes mapped to their children.
    static
    void ComputeDominators(Compiler* pCompiler, BlkToBlkSetMap* domTree);

private:

    // Ensures that the basic block graph has a root for the dominator graph, by ensuring
    // that there is a first block that is not in a try region (adding an empty block for that purpose
    // if necessary).  Eventually should move to Compiler.
    void SetupBBRoot();

    // Requires "postOrder" to be an array of size "count". Requires "count" to at least
    // be the size of the flow graph. Sorts the current compiler's flow-graph and places
    // the blocks in post order (i.e., a node's children first) in the array. Returns the
    // number of nodes visited while sorting the graph. In other words, valid entries in
    // the output array.
    int TopologicalSort(BasicBlock** postOrder, int count);

    // Requires "postOrder" to hold the blocks of the flowgraph in topologically sorted
    // order. Requires count to be the valid entries in the "postOrder" array. Computes
    // each block's immediate dominator and records it in the BasicBlock in bbIDom.
    void ComputeImmediateDom(BasicBlock** postOrder, int count);

#ifdef SSA_FEATURE_DOMARR
    // Requires "curBlock" to be the first basic block at the first step of the recursion.
    // Requires "domTree" to be a adjacency list (actually, a set of blocks with a set of blocks
    // as children.) Requires "preIndex" and "postIndex" to be initialized to 0 at entry into recursion.
    // Computes arrays "m_pDomPreOrder" and "m_pDomPostOrder" of block indices such that the blocks of a
    // "domTree" are in pre and postorder respectively.
    void DomTreeWalk(BasicBlock* curBlock, const BlkToBlkSetMap& domTree, int* preIndex, int* postIndex);
#endif

    // Requires all blocks to have computed "bbIDom." Requires "domTree" to be a preallocated BlkToBlkSetMap.
    // Helper to compute "domTree" from the pre-computed bbIDom of the basic blocks.
    static
    void ConstructDomTreeForBlock(Compiler* pCompiler, BasicBlock* block, BlkToBlkSetMap* domTree);
      
    // Requires "postOrder" to hold the blocks of the flowgraph in topologically sorted order. Requires
    // count to be the valid entries in the "postOrder" array. Computes "domTree" as a adjacency list
    // like object, i.e., a set of blocks with a set of blocks as children defining the DOM relation.
    void ComputeDominators(BasicBlock** postOrder, int count, BlkToBlkSetMap* domTree);

#ifdef DEBUG
    // Display the dominator tree.
    static
    void DisplayDominators(BlkToBlkSetMap* domTree);
#endif // DEBUG

    // Requires "postOrder" to hold the blocks of the flowgraph in topologically sorted order. Requires
    // count to be the valid entries in the "postOrder" array.  Returns a mapping from blocks to their
    // iterated dominance frontiers.  (Recall that the dominance frontier of a block B is the set of blocks
    // B3 such that there exists some B2 s.t. B3 is a successor of B2, and B dominates B2.  Note that this dominance
    // need not be strict -- B2 and B may be the same node.  The iterated dominance frontier is formed by a closure
    // operation: the IDF of B is the smallest set that includes B's dominance frontier, and also includes the dominance frontier
    // of all elements of the set.)
    BlkToBlkSetMap* ComputeIteratedDominanceFrontier(BasicBlock** postOrder, int count);

    // Requires "postOrder" to hold the blocks of the flowgraph in topologically sorted order. Requires
    // count to be the valid entries in the "postOrder" array. Inserts GT_PHI nodes at the beginning
    // of basic blocks that require them like so:
    // GT_ASG(GT_LCL_VAR, GT_PHI(GT_PHI_ARG(GT_LCL_VAR, Block*), GT_LIST(GT_PHI_ARG(GT_LCL_VAR, Block*), NULL));
    void InsertPhiFunctions(BasicBlock** postOrder, int count);

    // Requires "domTree" to be the dominator tree relation defined by a DOM b. 
    // Requires "pRenameState" to have counts and stacks at their initial state.
    // Assigns gtSsaNames to all variables.
    void RenameVariables(BlkToBlkSetMap* domTree, SsaRenameState* pRenameState);

    // Requires "block" to be any basic block participating in variable renaming, and has at least a
    // definition that pushed a ssa number into the rename stack for a variable. Requires "pRenameState"
    // to have variable stacks that have counts pushed into them for the block while assigning def
    // numbers. Pops the stack for any local variable that has an entry for block on top.
    void BlockPopStacks(BasicBlock* block, SsaRenameState* pRenameState);

    // Requires "block" to be non-NULL; and is searched for defs and uses to assign ssa numbers.
    // Requires "pRenameState" to be non-NULL and be currently used for variables renaming.
    void BlockRenameVariables(BasicBlock* block, SsaRenameState* pRenameState);

    // Requires "tree" (assumed to be a statement in "block") to be searched for defs and uses to assign ssa numbers. Requires "pRenameState"
    // to be non-NULL and be currently used for variables renaming.  Assumes that "isPhiDefn" implies that any definition occurring within "tree"
    // is a phi definition.
    void TreeRenameVariables(GenTree* tree, BasicBlock* block, SsaRenameState* pRenameState, bool isPhiDefn);

    // Assumes that "block" contains a definition for local var "lclNum", with SSA number "count".
    // IF "block" is within one or more try blocks,
    // and the local variable is live at the start of the corresponding handlers,
    // add this SSA number "count" to the argument list of the phi for the variable in the start
    // block of those handlers.
    void AddDefToHandlerPhis(BasicBlock* block, unsigned lclNum, unsigned count);

    // Same as above, for "Heap".
    void AddHeapDefToHandlerPhis(BasicBlock* block, unsigned count);

    // Requires "block" to be non-NULL.  Requires "pRenameState" to be non-NULL and be currently used
    // for variables renaming. Assigns the rhs arguments to the phi, i.e., block's phi node arguments.
    void AssignPhiNodeRhsVariables(BasicBlock* block, SsaRenameState* pRenameState);

    // Requires "tree" to be a local variable node. Maintains a map of <lclNum, ssaNum> -> tree
    // information in m_defs.
    void AddDefPoint(GenTree* tree, BasicBlock* blk);
#ifdef SSA_FEATURE_USEDEF
    // Requires "tree" to be a local variable node. Maintains a map of <lclNum, ssaNum> -> tree
    // information in m_uses.
    void AddUsePoint(GenTree* tree);
#endif

    // Returns true, and sets "*ppIndirAssign", if "tree" has been recorded as an indirect assignment.
    // (If the tree is an assignment, it's a definition only if it's labeled as an indirect definition, where
    // we took the address of the local elsewhere in the extended tree.)
    bool IsIndirectAssign(GenTreePtr tree, Compiler::IndirectAssignmentAnnotation** ppIndirAssign);

#ifdef DEBUG
    void Print(BasicBlock** postOrder, int count);
#endif

private:

#ifdef SSA_FEATURE_USEDEF
    // Use Def information after SSA. To query the uses and def of a given ssa var,
    // probe these data structures.
    // Do not move these outside of this class, use accessors/interface methods.
    VarToUses m_uses;
    VarToDef m_defs;
#endif

#ifdef SSA_FEATURE_DOMARR
    // To answer queries of type a DOM b.
    // Do not move these outside of this class, use accessors/interface methods.
    int* m_pDomPreOrder;
    int* m_pDomPostOrder;
#endif

    Compiler* m_pCompiler;

    // Used to allocate space for jitstd data structures.
    jitstd::allocator<void> m_allocator;
};
