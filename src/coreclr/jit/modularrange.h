// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// A contiguous interval on the ring of integers modulo mask + 1. Endpoints are inclusive.
// lower > upper denotes a range that crosses zero. The mask must be 2^n - 1. Empty is represented separately from a
// full-ring interval. For an 8-bit domain, (uint8)(X + 10) < 20 describes [246..255] U [0..9], represented by
// lower=246, upper=9.
class ModularRange
{
    target_size_t m_lower = 0;
    target_size_t m_upper = 0;
    bool          m_empty = true;

public:
    enum class Intersection
    {
        Empty,
        Single,
        Multiple,
        Unknown
    };

    ModularRange() = default;
    ModularRange(target_size_t lower, target_size_t upper)
        : m_lower(lower)
        , m_upper(upper)
        , m_empty(false)
    {
    }

    bool IsEmpty() const
    {
        return m_empty;
    }
    target_size_t Lower() const
    {
        assert(!m_empty);
        return m_lower;
    }
    target_size_t Upper() const
    {
        assert(!m_empty);
        return m_upper;
    }
    target_size_t Width(target_size_t mask) const
    {
        assert(!m_empty);
        return (m_upper - m_lower) & mask;
    }
    bool IsFull(target_size_t mask) const
    {
        return !m_empty && Width(mask) == mask;
    }
    bool Contains(target_size_t value, target_size_t mask) const
    {
        return !m_empty && (((value - m_lower) & mask) <= Width(mask));
    }
    bool Equals(const ModularRange& other, target_size_t mask) const
    {
        return m_empty ? other.m_empty
                       : !other.m_empty && ((m_lower == other.m_lower && m_upper == other.m_upper) ||
                                            (IsFull(mask) && other.IsFull(mask)));
    }

    // Describe all X satisfying (X + offset) oper bound. A signed comparison is the unsigned
    // comparison of the value with the sign bit flipped. All arithmetic here wraps.
    static ModularRange FromComparison(
        genTreeOps oper, target_size_t bound, bool isUnsigned, target_size_t mask, target_size_t offset = 0)
    {
        assert(mask != 0 && (mask & (mask + 1)) == 0);
        target_size_t bias  = isUnsigned ? 0 : (mask >> 1) + 1;
        target_size_t limit = (bound + bias) & mask;
        target_size_t lower = 0;
        target_size_t upper = mask;
        switch (oper)
        {
            case GT_LT:
                // x < cns -> [min, cns - 1], empty when cns is min
                if (limit == 0)
                    return {};
                upper = limit - 1;
                break;
            case GT_LE:
                // x <= cns -> [min, cns]
                upper = limit;
                break;
            case GT_GT:
                // x > cns -> [cns + 1, max], empty when cns is max
                if (limit == mask)
                    return {};
                lower = limit + 1;
                break;
            case GT_GE:
                // x >= cns -> [cns, max]
                lower = limit;
                break;
            case GT_EQ:
                // x == cns -> [cns, cns]
                lower = upper = limit;
                break;
            case GT_NE:
                // x != cns -> [cns + 1, cns - 1], the rest of the ring
                lower = (limit + 1) & mask;
                upper = (limit - 1) & mask;
                break;
            default:
                assert(!"Unsupported comparison");
                return {};
        }
        return {(lower - bias - offset) & mask, (upper - bias - offset) & mask};
    }

    // Intersect two ranges. Rotate this range to [0, width]. The other range then has either
    // one linear piece or two. Decline intersections with two disjoint pieces.
    //
    // E.g., in a signed 32-bit domain:
    //
    //   x >= 100 [100 .. MAX] and x <= 10  [MIN .. 10]  -> Empty
    //                                                      if (x >= 100) { if (x <= 10) } // always false
    //   x >= 100 [100 .. MAX] and x >= 10  [10 .. MAX]  -> Single [100 .. MAX], the first range
    //                                                      if (x >= 100) { if (x >= 10) } // always true
    //   x >= 10  [10 .. MAX]  and x <= 100 [MIN .. 100] -> Single [10 .. 100]
    //                                                      (x - 10) u<= 90
    //   x != 5   [6 .. 4]     and x != 6   [7 .. 5]     -> Single [7 .. 4], wrapping
    //                                                      (x - 7) u<= -3, all values but 5 and 6
    //   x != 5   [6 .. 4]     and x != 100 [101 .. 99]  -> Multiple: [6 .. 99] and [101 .. 4]
    Intersection Intersect(const ModularRange& other, target_size_t mask, ModularRange* result) const
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

        target_size_t width = Width(mask);
        target_size_t lower = (other.m_lower - m_lower) & mask;
        target_size_t upper = (other.m_upper - m_lower) & mask;
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
