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
    union {
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
        unsigned  m_lclNum;
        unsigned  m_lclOffs;

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

        inline unsigned LclNum() const
        {
            assert(IsLclVarRead() || IsLclVarWrite());
            return m_lclNum;
        }

        inline unsigned LclOffs() const
        {
            assert(IsLclVarRead() || IsLclVarWrite());
            return m_lclOffs;
        }

        inline bool WritesAnyLocation() const
        {
            if ((m_flags & ALIAS_WRITES_ADDRESSABLE_LOCATION) != 0)
            {
                return true;
            }

            if ((m_flags & ALIAS_WRITES_LCL_VAR) != 0)
            {
                LclVarDsc* const varDsc = m_compiler->lvaGetDesc(LclNum());
                return varDsc->IsAlwaysAliveInMemory();
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
    unsigned m_sideEffectFlags; // A mask of GTF_* flags that represents exceptional and barrier side effects.
    AliasSet m_aliasSet;        // An AliasSet that represents read and write side effects.

    template <typename TOtherAliasInfo>
    bool InterferesWith(unsigned otherSideEffectFlags, const TOtherAliasInfo& otherAliasInfo, bool strict) const;

public:
    SideEffectSet();
    SideEffectSet(Compiler* compiler, GenTree* node);

    void AddNode(Compiler* compiler, GenTree* node);
    bool InterferesWith(const SideEffectSet& other, bool strict) const;
    bool InterferesWith(Compiler* compiler, GenTree* node, bool strict) const;
    void Clear();
};

#endif // _SIDEEFFECTS_H_
