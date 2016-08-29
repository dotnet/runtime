// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _NODEINFO_H_
#define _NODEINFO_H_

struct GenTree;

class LinearScan;
typedef unsigned int LsraLocation;

class TreeNodeInfo
{
public:
    TreeNodeInfo()
    {
        loc                 = 0;
        _dstCount           = 0;
        _srcCount           = 0;
        _internalIntCount   = 0;
        _internalFloatCount = 0;

        srcCandsIndex         = 0;
        dstCandsIndex         = 0;
        internalCandsIndex    = 0;
        isLocalDefUse         = false;
        isHelperCallWithKills = false;
        isLsraAdded           = false;
        isDelayFree           = false;
        hasDelayFreeSrc       = false;
        isTgtPref             = false;
        regOptional           = false;
        definesAnyRegisters   = false;
#ifdef DEBUG
        isInitialized = false;
#endif
    }

    // dst
    __declspec(property(put = setDstCount, get = getDstCount)) int dstCount;
    void setDstCount(int count)
    {
        assert(count <= MAX_RET_REG_COUNT);
        _dstCount = (char)count;
    }
    int getDstCount()
    {
        return _dstCount;
    }

    // src
    __declspec(property(put = setSrcCount, get = getSrcCount)) int srcCount;
    void setSrcCount(int count)
    {
        _srcCount = (char)count;
        assert(_srcCount == count);
    }
    int getSrcCount()
    {
        return _srcCount;
    }

    // internalInt
    __declspec(property(put = setInternalIntCount, get = getInternalIntCount)) int internalIntCount;
    void setInternalIntCount(int count)
    {
        _internalIntCount = (char)count;
        assert(_internalIntCount == count);
    }
    int getInternalIntCount()
    {
        return _internalIntCount;
    }

    // internalFloat
    __declspec(property(put = setInternalFloatCount, get = getInternalFloatCount)) int internalFloatCount;
    void setInternalFloatCount(int count)
    {
        _internalFloatCount = (char)count;
        assert(_internalFloatCount == count);
    }
    int getInternalFloatCount()
    {
        return _internalFloatCount;
    }

    // SrcCandidates are constraints of the consuming (parent) operation applied to this node
    // (i.e. what registers it is constrained to consume).
    regMaskTP getSrcCandidates(LinearScan* lsra);
    void setSrcCandidates(LinearScan* lsra, regMaskTP mask);
    // DstCandidates are constraints of this node (i.e. what registers it is constrained to produce).
    regMaskTP getDstCandidates(LinearScan* lsra);
    void setDstCandidates(LinearScan* lsra, regMaskTP mask);
    // InternalCandidates are constraints of the registers used as temps in the evaluation of this node.
    regMaskTP getInternalCandidates(LinearScan* lsra);
    void setInternalCandidates(LinearScan* lsra, regMaskTP mask);
    void addInternalCandidates(LinearScan* lsra, regMaskTP mask);

    LsraLocation loc;

private:
    unsigned char _dstCount;
    unsigned char _srcCount;
    unsigned char _internalIntCount;
    unsigned char _internalFloatCount;

public:
    unsigned char srcCandsIndex;
    unsigned char dstCandsIndex;
    unsigned char internalCandsIndex;

    // isLocalDefUse identifies trees that produce a value that is not consumed elsewhere.
    // Examples include stack arguments to a call (they are immediately stored), lhs of comma
    // nodes, or top-level nodes that are non-void.
    unsigned char isLocalDefUse : 1;
    // isHelperCallWithKills is set when this is a helper call that kills more than just its in/out regs.
    unsigned char isHelperCallWithKills : 1;
    // Is this node added by LSRA, e.g. as a resolution or copy/reload move.
    unsigned char isLsraAdded : 1;
    // isDelayFree is set when the register defined by this node will interfere with the destination
    // of the consuming node, and therefore it must not be freed immediately after use.
    unsigned char isDelayFree : 1;
    // hasDelayFreeSrc is set when this node has sources that are marked "isDelayFree".  This is because,
    // we may eventually "contain" this node, in which case we don't want it's children (which have
    // already been marked "isDelayFree" to be handled that way when allocating.
    unsigned char hasDelayFreeSrc : 1;
    // isTgtPref is set to true when we have a rmw op, where we would like the result to be allocated
    // in the same register as op1.
    unsigned char isTgtPref : 1;
    // Whether a spilled second src can be treated as a contained operand
    unsigned char regOptional : 1;
    // Whether or not a node defines any registers, whether directly (for nodes where dstCout is non-zero)
    // or indirectly (for contained nodes, which propagate the transitive closure of the registers
    // defined by their inputs). Used during buildRefPositionsForNode in order to avoid unnecessary work.
    unsigned char definesAnyRegisters : 1;

#ifdef DEBUG
    // isInitialized is set when the tree node is handled.
    unsigned char isInitialized : 1;
#endif

public:
    // Initializes the TreeNodeInfo value with the given values.
    void Initialize(LinearScan* lsra, GenTree* node, LsraLocation location);

#ifdef DEBUG
    void dump(LinearScan* lsra);

    // This method checks to see whether the information has been initialized,
    // and is in a consistent state
    bool IsValid(LinearScan* lsra)
    {
        return (isInitialized &&
                ((getSrcCandidates(lsra) | getInternalCandidates(lsra) | getDstCandidates(lsra)) &
                 ~(RBM_ALLFLOAT | RBM_ALLINT)) == 0);
    }
#endif // DEBUG
};

#endif // _NODEINFO_H_
