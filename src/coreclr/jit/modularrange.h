// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// A contiguous interval on the ring of integers modulo mask + 1. Endpoints
// are inclusive for constant bounds; lower > upper denotes a range that crosses zero. The mask
// must be 2^n - 1. Empty is represented separately from a full-ring interval.
// For an 8-bit domain, (uint8)(X + 10) < 20 describes [246..255] U [0..9],
// represented by lower=246, upper=9.
class ModularRange
{
    // A symbolic upper endpoint denotes a nonnegative value of the domain's
    // signed type. Its payload is an opaque identity, not an integer value.
    struct Bound
    {
        uint64_t value;
        bool     isSymbolic  = false;
        bool     isExclusive = false;
    };
    uint64_t m_lower = 0;
    Bound    m_upper = {0};
    bool     m_empty = true;

public:
    enum class Intersection
    {
        Empty,
        Single,
        Multiple,
        Unknown
    };

    ModularRange() = default;
    ModularRange(uint64_t lower, uint64_t upper)
        : m_lower(lower)
        , m_upper{upper}
        , m_empty(false)
    {
    }

    bool IsEmpty() const
    {
        return m_empty;
    }
    uint64_t Lower() const
    {
        assert(!m_empty);
        return m_lower;
    }
    uint64_t Upper() const
    {
        assert(!m_empty);
        assert(!HasSymbolicUpper());
        return m_upper.value;
    }
    uint64_t Width(uint64_t mask) const
    {
        assert(!m_empty);
        assert(!HasSymbolicUpper());
        return (m_upper.value - m_lower) & mask;
    }
    bool IsFull(uint64_t mask) const
    {
        return !m_empty && !HasSymbolicUpper() && Width(mask) == mask;
    }
    bool Contains(uint64_t value, uint64_t mask) const
    {
        return !m_empty && (((value - m_lower) & mask) <= Width(mask));
    }
    bool Equals(const ModularRange& other, uint64_t mask) const
    {
        return m_empty ? other.m_empty
                       : !other.m_empty && ((m_lower == other.m_lower && m_upper.value == other.m_upper.value &&
                                             m_upper.isSymbolic == other.m_upper.isSymbolic &&
                                             m_upper.isExclusive == other.m_upper.isExclusive) ||
                                            (IsFull(mask) && other.IsFull(mask)));
    }

    bool HasSymbolicUpper() const
    {
        return m_upper.isSymbolic;
    }
    uint64_t UpperIdentity() const
    {
        assert(HasSymbolicUpper());
        return m_upper.value;
    }
    bool IsUpperExclusive() const
    {
        return m_upper.isExclusive;
    }

    // Describe X < bound or X <= bound, where bound is a nonnegative symbolic
    // value. Retain strictness instead of forming bound - 1: bound can be zero.
    static ModularRange FromNonNegativeBound(genTreeOps oper, uint64_t identity, bool isUnsigned, uint64_t mask)
    {
        assert(oper == GT_LT || oper == GT_LE);
        assert(mask != 0 && (mask & (mask + 1)) == 0);
        ModularRange result(isUnsigned ? 0 : (mask >> 1) + 1, 0);
        result.m_upper = {identity, true, oper == GT_LT};
        return result;
    }

    // Describe all X satisfying (X + offset) oper bound. Signed ordering is
    // unsigned ordering rotated by the sign bit; all arithmetic here wraps.
    static ModularRange FromComparison(
        genTreeOps oper, uint64_t bound, bool isUnsigned, uint64_t mask, uint64_t offset = 0)
    {
        assert(mask != 0 && (mask & (mask + 1)) == 0);
        uint64_t bias  = isUnsigned ? 0 : (mask >> 1) + 1;
        uint64_t limit = (bound + bias) & mask;
        uint64_t lower = 0;
        uint64_t upper = mask;
        switch (oper)
        {
            case GT_LT:
                if (limit == 0)
                    return {};
                upper = limit - 1;
                break;
            case GT_LE:
                upper = limit;
                break;
            case GT_GT:
                if (limit == mask)
                    return {};
                lower = limit + 1;
                break;
            case GT_GE:
                lower = limit;
                break;
            case GT_EQ:
                lower = upper = limit;
                break;
            case GT_NE:
                lower = (limit + 1) & mask;
                upper = (limit - 1) & mask;
                break;
            default:
                assert(!"Unsupported comparison");
                return {};
        }
        return {(lower - bias - offset) & mask, (upper - bias - offset) & mask};
    }

    // Rotate the first range to [0, width]. The second range then has either
    // one linear piece or two. Decline intersections with two disjoint pieces.
    Intersection Intersect(const ModularRange& other, uint64_t mask, ModularRange* result) const
    {
        if (m_empty || other.m_empty)
        {
            *result = {};
            return Intersection::Empty;
        }
        if (IsFull(mask))
        {
            *result = other;
            return Intersection::Single;
        }
        if (other.IsFull(mask))
        {
            *result = *this;
            return Intersection::Single;
        }

        if (HasSymbolicUpper() || other.HasSymbolicUpper())
        {
            if (HasSymbolicUpper() && other.HasSymbolicUpper())
            {
                if (UpperIdentity() != other.UpperIdentity())
                    return Intersection::Unknown;

                // Both ranges end at the same nonnegative value. A zero lower
                // bound excludes the negative half of a signed comparison.
                *result         = *this;
                result->m_lower = std::min(m_lower, other.m_lower);
                result->m_upper.isExclusive |= other.m_upper.isExclusive;
                return Intersection::Single;
            }

            const ModularRange& symbolic = HasSymbolicUpper() ? *this : other;
            const ModularRange& constant = HasSymbolicUpper() ? other : *this;
            uint64_t            signBit  = (mask >> 1) + 1;
            if (constant.m_lower == 0 && constant.Upper() == signBit - 1)
            {
                // [0, MAX] intersect [MIN, length) is [0, length).
                *result         = symbolic;
                result->m_lower = 0;
                return Intersection::Single;
            }
            if (constant.m_lower == signBit && constant.Upper() == mask)
            {
                // The symbolic endpoint cannot exclude any negative value.
                *result = symbolic.m_lower == 0 ? ModularRange() : constant;
                return result->IsEmpty() ? Intersection::Empty : Intersection::Single;
            }
            return Intersection::Unknown;
        }

        uint64_t width = Width(mask);
        uint64_t lower = (other.m_lower - m_lower) & mask;
        uint64_t upper = (other.m_upper.value - m_lower) & mask;
        if (lower <= upper)
        {
            upper = std::min(upper, width);
            if (lower > upper)
            {
                *result = {};
                return Intersection::Empty;
            }
            *result = {(m_lower + lower) & mask, (m_lower + upper) & mask};
            return Intersection::Single;
        }
        if (lower <= width)
        {
            return Intersection::Multiple;
        }
        *result = {m_lower, (m_lower + std::min(upper, width)) & mask};
        return Intersection::Single;
    }
};
