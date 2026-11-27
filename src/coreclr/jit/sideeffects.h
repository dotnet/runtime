// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SIDEEFFECTS_H_
#define _SIDEEFFECTS_H_

//------------------------------------------------------------------------
// LclVarSet:
//    Represents a set of lclVars. Optimized for the case that the set
//    never holds more than a single element. This type is used internally
//    by `AliasSet` to track the sets of lclVars that are read and
//    written for a given alias set.
//
class LclVarSet final
{
    union
    {
        hashBv*  m_bitVector;
        unsigned m_lclNum;
    };

    bool m_hasAnyLcl;
    bool m_hasBitVector;

public:
    LclVarSet();

    inline bool IsEmpty() const
    {
        return !m_hasAnyLcl || !m_hasBitVector || !m_bitVector->anySet();
    }

    void Add(Compiler* compiler, unsigned lclNum);
    bool Intersects(const LclVarSet& other) const;
    bool Contains(unsigned lclNum) const;
    void Clear();
};

//------------------------------------------------------------------------
// AliasSet:
//    Represents a set of reads and writes for the purposes of alias
//    analysis. This type partitions storage into two categories:
//    lclVars and addressable locations. The definition of the former is
//    intuitive. The latter is the union of the set of address-exposed
//    lclVars with the set of all other memory locations. Any memory
//    access is assumed to alias any other memory access.
//
class AliasSet final
{
    LclVarSet m_lclVarReads;
    LclVarSet m_lclVarWrites;

    bool m_readsAddressableLocation;
    bool m_writesAddressableLocation;

public:
    //------------------------------------------------------------------------
    // AliasSet::NodeInfo:
    //    Represents basic alias information for a single IR node.
    //
    class NodeInfo final
    {
        enum : unsigned
        {
            ALIAS_NONE                        = 0x0,
            ALIAS_READS_ADDRESSABLE_LOCATION  = 0x1,
            ALIAS_WRITES_ADDRESSABLE_LOCATION = 0x2,
            ALIAS_READS_LCL_VAR               = 0x4,
            ALIAS_WRITES_LCL_VAR              = 0x8
        };

        Compiler* m_compiler;
        GenTree*  m_node;
        unsigned  m_flags;
        unsigned  m_readLclNum;
        unsigned  m_readLclOffs;

    public:
        NodeInfo(Compiler* compiler, GenTree* node);

        inline Compiler* TheCompiler() const
        {
            return m_compiler;
        }

        inline GenTree* Node() const
        {
            return m_node;
        }

        inline bool ReadsAddressableLocation() const
        {
            return (m_flags & ALIAS_READS_ADDRESSABLE_LOCATION) != 0;
        }

        inline bool WritesAddressableLocation() const
        {
            return (m_flags & ALIAS_WRITES_ADDRESSABLE_LOCATION) != 0;
        }

        inline bool IsLclVarRead() const
        {
            return (m_flags & ALIAS_READS_LCL_VAR) != 0;
        }

        inline bool IsLclVarWrite() const
        {
            return (m_flags & ALIAS_WRITES_LCL_VAR) != 0;
        }

        inline unsigned ReadLclNum() const
        {
            assert(IsLclVarRead());
            return m_readLclNum;
        }

        inline unsigned ReadLclOffs() const
        {
            assert(IsLclVarRead());
            return m_readLclOffs;
        }

        template <typename TVisitor>
        GenTree::VisitResult VisitLocalWrites(TVisitor visitor) const
        {
            assert(IsLclVarWrite());
            bool                 anyDefs = false;
            GenTree::VisitResult result  = m_node->VisitLogicalLocalDefs(m_compiler, [&](const auto& def) {
                anyDefs = true;
                return visitor(def);
            });
            if (anyDefs)
            {
                return result;
            }

            GenTree* location = m_node->OperIsIndir() ? m_node->AsIndir()->Addr() : m_node;
            assert(location->OperIsAnyLocal());

            struct IndirectLocalDef
            {
                GenTree*             m_node;
                GenTreeLclVarCommon* m_local;

                unsigned GetLclNum() const
                {
                    return m_local->GetLclNum();
                }

                ssize_t GetOffset(Compiler* compiler) const
                {
                    return m_local->GetLclOffs();
                }

                ValueSize GetSize(Compiler* compiler) const
                {
                    return ValueSize(genTypeSize(m_node));
                }
            };

            const IndirectLocalDef def{m_node, location->AsLclVarCommon()};
            return visitor(def);
        }

        inline bool WritesAnyLocation() const
        {
            if ((m_flags & ALIAS_WRITES_ADDRESSABLE_LOCATION) != 0)
            {
                return true;
            }

            if ((m_flags & ALIAS_WRITES_LCL_VAR) != 0)
            {
                // Stores to locals live into handlers cannot be reordered with
                // exception-throwing nodes so we conservatively consider them
                // globally visible.

                return VisitLocalWrites([&](const auto& def) {
                    LclVarDsc* varDsc = m_compiler->lvaGetDesc(def.GetLclNum());
                    bool       liveInHandler =
                        varDsc->lvTracked ? varDsc->IsLiveInOutOfHandler() : m_compiler->compHndBBtabCount > 0;
                    return liveInHandler ? GenTree::VisitResult::Abort : GenTree::VisitResult::Continue;
                }) == GenTree::VisitResult::Abort;
            }

            return false;
        }
    };

    AliasSet();

    inline bool WritesAnyLocation() const
    {
        return m_writesAddressableLocation || !m_lclVarWrites.IsEmpty();
    }

    void AddNode(Compiler* compiler, GenTree* node);
    bool InterferesWith(const AliasSet& other) const;
    bool InterferesWith(const NodeInfo& node) const;
    bool WritesLocal(unsigned lclNum) const;
    void Clear();
};

//------------------------------------------------------------------------
// SideEffectSet:
//    Represents a set of side effects for the purposes of analyzing code
//    motion.
//    Note that for non-fixed-size frames without a frame pointer (currently
//    x86-only), we don't track the modification of the stack level that occurs
//    with a GT_PUTARG_STK as a side-effect. If we ever support general code
//    reordering, that would have to be taken into account. As it happens,
//    we currently do not reorder any other side-effecting nodes relative to
//    these.
//
class SideEffectSet final
{
    unsigned          m_sideEffectFlags; // A mask of GTF_* flags that represents exceptional and barrier side effects.
    ExceptionSetFlags m_preciseExceptions; // Set representing exceptions that may be thrown
    AliasSet          m_aliasSet;          // An AliasSet that represents read and write side effects.

    template <typename TOtherAliasInfo>
    bool InterferesWith(unsigned               otherSideEffectFlags,
                        ExceptionSetFlags      otherPreciseExceptions,
                        const TOtherAliasInfo& otherAliasInfo,
                        bool                   strict) const;

public:
    SideEffectSet();
    SideEffectSet(Compiler* compiler, GenTree* node);

    void AddNode(Compiler* compiler, GenTree* node);
    bool InterferesWith(const SideEffectSet& other, bool strict) const;
    bool InterferesWith(Compiler* compiler, GenTree* node, bool strict) const;
    void Clear();

    bool IsLirInvariantInRange(Compiler*    comp,
                               GenTree*     node,
                               GenTree*     endExclusive,
                               GenTreeFlags ignoreFlagsOnNode = GTF_EMPTY);

    bool IsLirInvariantInRange(Compiler*    comp,
                               GenTree*     node,
                               GenTree*     endExclusive,
                               GenTree*     ignoreNode,
                               GenTreeFlags ignoreFlagsOnNode = GTF_EMPTY);

    bool IsLirRangeInvariantInRange(
        Compiler* comp, GenTree* rangeStart, GenTree* rangeEnd, GenTree* endExclusive, GenTree* ignoreNode);

    bool WritesLocal(unsigned lclNum) const
    {
        return m_aliasSet.WritesLocal(lclNum);
    }
};

#endif // _SIDEEFFECTS_H_
