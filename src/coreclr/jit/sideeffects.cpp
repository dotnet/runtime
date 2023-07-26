// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "sideeffects.h"

LclVarSet::LclVarSet() : m_bitVector(nullptr), m_hasAnyLcl(false), m_hasBitVector(false)
{
}

//------------------------------------------------------------------------
// LclVarSet::Add:
//    Adds the given lclNum to the LclVarSet.
//
// Arguments:
//    compiler - The compiler context
//    lclNum - The lclNum to add.
//
void LclVarSet::Add(Compiler* compiler, unsigned lclNum)
{
    if (!m_hasAnyLcl)
    {
        m_lclNum    = lclNum;
        m_hasAnyLcl = true;
    }
    else
    {
        if (!m_hasBitVector)
        {
            unsigned singleLclNum = m_lclNum;
            m_bitVector           = hashBv::Create(compiler);
            m_bitVector->setBit(singleLclNum);
            m_hasBitVector = true;
        }

        m_bitVector->setBit(lclNum);
    }
}

//------------------------------------------------------------------------
// LclVarSet::Intersects:
//    Returns true if this LclVarSet intersects with the given LclVarSet.
//
// Arguments:
//    other - The other lclVarSet.
//
bool LclVarSet::Intersects(const LclVarSet& other) const
{
    // If neither set has ever contained anything, the sets do not intersect.
    if (!m_hasAnyLcl || !other.m_hasAnyLcl)
    {
        return false;
    }

    // If this set is not represented by a bit vector, see if the single lclNum is contained in the other set.
    if (!m_hasBitVector)
    {
        if (!other.m_hasBitVector)
        {
            return m_lclNum == other.m_lclNum;
        }

        return other.m_bitVector->testBit(m_lclNum);
    }

    // If this set is represented by a bit vector but the other set is not, see if the single lclNum in the other
    // set is contained in this set.
    if (!other.m_hasBitVector)
    {
        return m_bitVector->testBit(other.m_lclNum);
    }

    // Both sets are represented by bit vectors. Check to see if they intersect.
    return m_bitVector->Intersects(other.m_bitVector);
}

//------------------------------------------------------------------------
// LclVarSet::Contains:
//    Returns true if this LclVarSet contains the given lclNum.
//
// Arguments:
//    lclNum - The lclNum in question.
//
bool LclVarSet::Contains(unsigned lclNum) const
{
    // If this set has never contained anything, it does not contain the lclNum.
    if (!m_hasAnyLcl)
    {
        return false;
    }

    // If this set is not represented by a bit vector, see if its single lclNum is the same as the given lclNum.
    if (!m_hasBitVector)
    {
        return m_lclNum == lclNum;
    }

    // This set is represented by a bit vector. See if the bit vector contains the given lclNum.
    return m_bitVector->testBit(lclNum);
}

//------------------------------------------------------------------------
// LclVarSet::Clear:
//    Clears the contents of this LclVarSet.
//
void LclVarSet::Clear()
{
    if (m_hasBitVector)
    {
        assert(m_hasAnyLcl);
        m_bitVector->ZeroAll();
    }
    else if (m_hasAnyLcl)
    {
        m_hasAnyLcl = false;
    }
}

AliasSet::AliasSet()
    : m_lclVarReads(), m_lclVarWrites(), m_readsAddressableLocation(false), m_writesAddressableLocation(false)
{
}

//------------------------------------------------------------------------
// AliasSet::NodeInfo::NodeInfo:
//    Computes the alias info for a given node. Note that this does not
//    include the set of lclVar accesses for a node unless the node is
//    itself a lclVar access (e.g. a GT_LCL_VAR, GT_STORE_LCL_VAR, etc.).
//
// Arguments:
//    compiler - The compiler context.
//    node - The node in question.
//
AliasSet::NodeInfo::NodeInfo(Compiler* compiler, GenTree* node)
    : m_compiler(compiler), m_node(node), m_flags(0), m_lclNum(0), m_lclOffs(0)
{
    if (node->IsCall())
    {
        // For calls having return buffer, update the local number that is written after this call.
        GenTree* retBufArgNode = compiler->gtCallGetDefinedRetBufLclAddr(node->AsCall());
        if (retBufArgNode != nullptr)
        {
            m_flags |= ALIAS_WRITES_LCL_VAR;
            m_lclNum  = retBufArgNode->AsLclVarCommon()->GetLclNum();
            m_lclOffs = retBufArgNode->AsLclVarCommon()->GetLclOffs();

            if (compiler->lvaTable[m_lclNum].IsAddressExposed())
            {
                m_flags |= ALIAS_WRITES_ADDRESSABLE_LOCATION;
            }
        }

        // Calls are treated as reads and writes of addressable locations unless they are known to be pure.
        if (node->AsCall()->IsPure(compiler))
        {
            m_flags = ALIAS_NONE;
        }
        else
        {
            m_flags = ALIAS_READS_ADDRESSABLE_LOCATION | ALIAS_WRITES_ADDRESSABLE_LOCATION;
        }
        return;
    }
    else if (node->OperIsAtomicOp())
    {
        // Atomic operations both read and write addressable locations.
        m_flags = ALIAS_READS_ADDRESSABLE_LOCATION | ALIAS_WRITES_ADDRESSABLE_LOCATION;
        return;
    }

    // Is the operation a write? If so, set `node` to the location that is being written to.
    bool isWrite = false;
    if (node->OperIsStore() || node->OperIs(GT_STORE_DYN_BLK, GT_MEMORYBARRIER))
    {
        isWrite = true;
    }
#ifdef FEATURE_HW_INTRINSICS
    else if (node->OperIsHWIntrinsic())
    {
        if (node->AsHWIntrinsic()->OperIsMemoryStoreOrBarrier())
        {
            // For barriers, we model the behavior after GT_MEMORYBARRIER
            isWrite = true;
        }
    }
#endif // FEATURE_HW_INTRINSICS

    assert(isWrite || !node->OperRequiresAsgFlag());

    // `node` is the location being accessed. Determine whether or not it is a memory or local variable access, and if
    // it is the latter, get the number of the lclVar.
    bool     isMemoryAccess = false;
    bool     isLclVarAccess = false;
    unsigned lclNum         = 0;
    unsigned lclOffs        = 0;
    if (node->OperIsIndir())
    {
        // If the indirection targets a lclVar, we can be more precise with regards to aliasing by treating the
        // indirection as a lclVar access.
        GenTree* address = node->AsIndir()->Addr();
        if (address->OperIs(GT_LCL_ADDR))
        {
            isLclVarAccess = true;
            lclNum         = address->AsLclVarCommon()->GetLclNum();
            lclOffs        = address->AsLclVarCommon()->GetLclOffs();
        }
        else
        {
            isMemoryAccess = true;
        }
    }
    else if (node->OperIsImplicitIndir())
    {
        isMemoryAccess = true;
    }
    else if (node->OperIsLocal())
    {
        isLclVarAccess = true;
        lclNum         = node->AsLclVarCommon()->GetLclNum();
        lclOffs        = node->AsLclVarCommon()->GetLclOffs();
    }
    else
    {
        // This is neither a memory nor a local var access.
        m_flags = ALIAS_NONE;
        return;
    }

    assert(isMemoryAccess || isLclVarAccess);

    // Now that we've determined whether or not this access is a read or a write and whether the accessed location is
    // memory or a lclVar, determine whether or not the location is addressable and update the alias set.
    const bool isAddressableLocation = isMemoryAccess || compiler->lvaTable[lclNum].IsAddressExposed();

    if (!isWrite)
    {
        if (isAddressableLocation)
        {
            m_flags |= ALIAS_READS_ADDRESSABLE_LOCATION;
        }

        if (isLclVarAccess)
        {
            m_flags |= ALIAS_READS_LCL_VAR;
            m_lclNum  = lclNum;
            m_lclOffs = lclOffs;
        }
    }
    else
    {
        if (isAddressableLocation)
        {
            m_flags |= ALIAS_WRITES_ADDRESSABLE_LOCATION;
        }

        if (isLclVarAccess)
        {
            m_flags |= ALIAS_WRITES_LCL_VAR;
            m_lclNum  = lclNum;
            m_lclOffs = lclOffs;
        }
    }
}

//------------------------------------------------------------------------
// AliasSet::AddNode:
//    Adds the given node's accesses to this AliasSet.
//
// Arguments:
//    compiler - The compiler context.
//    node - The node to add to the set.
//
void AliasSet::AddNode(Compiler* compiler, GenTree* node)
{
    // First, add all lclVar uses associated with the node to the set. This is necessary because the lclVar reads occur
    // at the position of the user, not at the position of the GenTreeLclVar node.
    node->VisitOperands([compiler, this](GenTree* operand) -> GenTree::VisitResult {
        if (operand->OperIsLocalRead())
        {
            const unsigned lclNum = operand->AsLclVarCommon()->GetLclNum();
            if (compiler->lvaTable[lclNum].IsAddressExposed())
            {
                m_readsAddressableLocation = true;
            }

            m_lclVarReads.Add(compiler, lclNum);
        }
        if (operand->isContained())
        {
            AddNode(compiler, operand);
        }
        return GenTree::VisitResult::Continue;
    });

    NodeInfo nodeInfo(compiler, node);
    if (nodeInfo.ReadsAddressableLocation())
    {
        m_readsAddressableLocation = true;
    }
    if (nodeInfo.WritesAddressableLocation())
    {
        m_writesAddressableLocation = true;
    }
    if (nodeInfo.IsLclVarRead())
    {
        m_lclVarReads.Add(compiler, nodeInfo.LclNum());
    }
    if (nodeInfo.IsLclVarWrite())
    {
        m_lclVarWrites.Add(compiler, nodeInfo.LclNum());
    }
}

//------------------------------------------------------------------------
// AliasSet::InterferesWith:
//    Returns true if the reads and writes in this alias set interfere
//    with the given alias set.
//
//    Two alias sets interfere under any of the following conditions:
//    - Both sets write to any addressable location (e.g. the heap,
//      address-exposed locals)
//    - One set reads any addressable location and the other set writes
//      any addressable location
//    - Both sets write to the same lclVar
//    - One set writes to a lclVar that is read by the other set
//
// Arguments:
//    other - The other alias set.
//
bool AliasSet::InterferesWith(const AliasSet& other) const
{
    // If both sets write any addressable location, the sets interfere.
    if (m_writesAddressableLocation && other.m_writesAddressableLocation)
    {
        return true;
    }

    // If one set writes any addressable location and the other reads any addressable location, the sets interfere.
    if ((m_readsAddressableLocation && other.m_writesAddressableLocation) ||
        (m_writesAddressableLocation && other.m_readsAddressableLocation))
    {
        return true;
    }

    // If the set of lclVars written by this alias set intersects with the set of lclVars accessed by the other alias
    // set, the alias sets interfere.
    if (m_lclVarWrites.Intersects(other.m_lclVarReads) || m_lclVarWrites.Intersects(other.m_lclVarWrites))
    {
        return true;
    }

    // If the set of lclVars read by this alias set intersects with the set of lclVars written by the other alias set,
    // the alias sets interfere. Otherwise, the alias sets do not interfere.
    return m_lclVarReads.Intersects(other.m_lclVarWrites);
}

//------------------------------------------------------------------------
// AliasSet::InterferesWith:
//    Returns true if the reads and writes in this alias set interfere
//    with those for the given node.
//
//    An alias set interferes with a given node iff it interferes with the
//    alias set for that node.
//
// Arguments:
//    other - The info for the node in question.
//
bool AliasSet::InterferesWith(const NodeInfo& other) const
{
    // First check whether or not this set interferes with the lclVar uses associated with the given node.
    if (m_writesAddressableLocation || !m_lclVarWrites.IsEmpty())
    {
        Compiler* compiler = other.TheCompiler();
        for (GenTree* operand : other.Node()->Operands())
        {
            if (operand->OperIsLocalRead())
            {
                // If this set writes any addressable location and the node uses an address-exposed lclVar,
                // the set interferes with the node.
                const unsigned lclNum = operand->AsLclVarCommon()->GetLclNum();
                if (compiler->lvaTable[lclNum].IsAddressExposed() && m_writesAddressableLocation)
                {
                    return true;
                }

                // If this set writes to a lclVar used by the node, the set interferes with the node.
                if (m_lclVarWrites.Contains(lclNum))
                {
                    return true;
                }
            }
        }
    }

    // If the node and the set both write to any addressable location, they interfere.
    if (m_writesAddressableLocation && other.WritesAddressableLocation())
    {
        return true;
    }

    // If the node or the set writes any addressable location and the other reads any addressable location,
    // they interfere.
    if ((m_readsAddressableLocation && other.WritesAddressableLocation()) ||
        (m_writesAddressableLocation && other.ReadsAddressableLocation()))
    {
        return true;
    }

    // If the set writes a local var accessed by the node, they interfere.
    if ((other.IsLclVarRead() || other.IsLclVarWrite()) && m_lclVarWrites.Contains(other.LclNum()))
    {
        return true;
    }

    // If the set reads a local var written by the node, they interfere.
    return other.IsLclVarWrite() && m_lclVarReads.Contains(other.LclNum());
}

//------------------------------------------------------------------------
// AliasSet::Clear:
//    Clears the current alias set.
//
void AliasSet::Clear()
{
    m_readsAddressableLocation  = false;
    m_writesAddressableLocation = false;

    m_lclVarReads.Clear();
    m_lclVarWrites.Clear();
}

SideEffectSet::SideEffectSet() : m_sideEffectFlags(0), m_aliasSet()
{
}

//------------------------------------------------------------------------
// SideEffectSet::SideEffectSet:
//    Constructs a side effect set initialized using the given node.
//    Equivalent to the following;
//
//       SideEffectSet sideEffectSet;
//       sideEffectSet.AddNode(compiler, node);
//
// Arguments:
//    compiler - The compiler context.
//    node - The node to use for initialization.
//
SideEffectSet::SideEffectSet(Compiler* compiler, GenTree* node) : m_sideEffectFlags(0), m_aliasSet()
{
    AddNode(compiler, node);
}

//------------------------------------------------------------------------
// SideEffectSet::AddNode:
//    Adds the given node's accesses to this SideEffectSet.
//
// Arguments:
//    compiler - The compiler context.
//    node - The node to add to the set.
//
void SideEffectSet::AddNode(Compiler* compiler, GenTree* node)
{
    m_sideEffectFlags |= (node->gtFlags & GTF_ALL_EFFECT);
    m_aliasSet.AddNode(compiler, node);
}

//------------------------------------------------------------------------
// SideEffectSet::InterferesWith:
//    Returns true if the side effects in this set interfere with the
//    given side effect flags and alias information.
//
//    Two side effect sets interfere under any of the following
//    conditions:
//    - If the analysis is strict, and:
//        - One set contains a compiler barrier and the other set contains a global reference, or
//        - Both sets produce an exception
//    - Whether or not the analysis is strict:
//        - One set produces an exception and the other set contains a
//          write
//        - One set's reads and writes interfere with the other set's
//          reads and writes
//
// Arguments:
//    otherSideEffectFlags - The side effect flags for the other side
//                           effect set.
//    otherAliasInfo - The alias information for the other side effect
//                     set.
//    strict - True if the analysis should be strict as described above.
//
template <typename TOtherAliasInfo>
bool SideEffectSet::InterferesWith(unsigned               otherSideEffectFlags,
                                   const TOtherAliasInfo& otherAliasInfo,
                                   bool                   strict) const
{
    const bool thisProducesException  = (m_sideEffectFlags & GTF_EXCEPT) != 0;
    const bool otherProducesException = (otherSideEffectFlags & GTF_EXCEPT) != 0;

    if (strict)
    {
        // If either set contains a compiler barrier, and the other set contains a global reference,
        // the sets interfere.
        if (((m_sideEffectFlags & GTF_ORDER_SIDEEFF) != 0) && ((otherSideEffectFlags & GTF_GLOB_REF) != 0))
        {
            return true;
        }

        if (((otherSideEffectFlags & GTF_ORDER_SIDEEFF) != 0) && ((m_sideEffectFlags & GTF_GLOB_REF) != 0))
        {
            return true;
        }

        // If both sets produce an exception, the sets interfere.
        if (thisProducesException && otherProducesException)
        {
            return true;
        }
    }

    // If one set produces an exception and the other set writes to any location, the sets interfere.
    if ((thisProducesException && otherAliasInfo.WritesAnyLocation()) ||
        (otherProducesException && m_aliasSet.WritesAnyLocation()))
    {
        return true;
    }

    // At this point, the only interference between the sets will arise from their alias sets.
    return m_aliasSet.InterferesWith(otherAliasInfo);
}

//------------------------------------------------------------------------
// SideEffectSet::InterferesWith:
//    Returns true if the side effects in this set interfere with the side
//    effects in the given side effect set.
//
//    Two side effect sets interfere under any of the following
//    conditions:
//    - If the analysis is strict, and:
//        - Either set contains a compiler barrier, or
//        - Both sets produce an exception
//    - Whether or not the analysis is strict:
//        - One set produces an exception and the other set contains a
//          write
//        - One set's reads and writes interfere with the other set's
//          reads and writes
//
// Arguments:
//    other - The other side effect set.
//    strict - True if the analysis should be strict as described above.
//
bool SideEffectSet::InterferesWith(const SideEffectSet& other, bool strict) const
{
    return InterferesWith(other.m_sideEffectFlags, other.m_aliasSet, strict);
}

//------------------------------------------------------------------------
// SideEffectSet::InterferesWith:
//    Returns true if the side effects in this set interfere with the side
//    effects for the given node.
//
//    A side effect set interferes with a given node iff it interferes
//    with the side effect set of the node.
//
// Arguments:
//    compiler - The compiler context.
//    node - The node in question.
//    strict - True if the analysis should be strict as described above.
//
bool SideEffectSet::InterferesWith(Compiler* compiler, GenTree* node, bool strict) const
{
    return InterferesWith((node->gtFlags & GTF_ALL_EFFECT), AliasSet::NodeInfo(compiler, node), strict);
}

//------------------------------------------------------------------------
// SideEffectSet::Clear:
//    Clears the current side effect set.
//
void SideEffectSet::Clear()
{
    m_sideEffectFlags = 0;
    m_aliasSet.Clear();
}
