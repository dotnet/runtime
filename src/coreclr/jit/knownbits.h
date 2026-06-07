// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// KnownBits is a 32/64-bit fixed-width "known bits" lattice for an integral value, together with
// transfer functions that compute the lattice for the result of an operation from its operands.
// It is the bit-level analog of the interval/range analysis in rangecheck.{h,cpp}.
//
// The struct (KnownBits) and the transfer functions (KnownBitsOps) are ports of LLVM's
// `llvm::KnownBits` from `llvm/Support/KnownBits.{h,cpp}`. Differences from LLVM:
//
// * The lattice is fixed-width (32 or 64 bits) and stored in two uint64_t fields; LLVM uses APInt
//   so it can carry any bit width. An explicit `width` parameter takes the place of getBitWidth();
//   masking and sign-extension are done explicitly here.
//
// * Only the subset of operations needed by today's consumers (And/Or/UDiv/Cast/EvalRelop) is
//   ported; the rest of LLVM's catalog (mul, urem, shifts, sadd_sat, abs, ...) is intentionally
//   omitted and can be added back if and when a consumer wants it.
//
// * `KnownBits::Compute` is the analysis driver (the analog of `RangeCheck::GetRangeFromAssertions`):
//   it derives the known bits of a value number from its VN structure and the incoming assertions.
//

#pragma once

#include "compiler.h"

// "Known bits" lattice. For each bit i in [0, width):
//   * set in knownZero => definitely 0
//   * set in knownOne  => definitely 1
//   * set in neither   => unknown
// Invariants: (knownZero & knownOne) == 0, and bits at positions >= width are 0 in both masks.
struct KnownBits
{
    uint64_t knownZero;
    uint64_t knownOne;

    KnownBits()
        : knownZero(0)
        , knownOne(0)
    {
    }
    KnownBits(uint64_t z, uint64_t o)
        : knownZero(z)
        , knownOne(o)
    {
        assert((z & o) == 0);
    }

    // Mask covering the low "width" bits (width must be 32 or 64). Replaces APInt's implicit width.
    static uint64_t WidthMask(unsigned width)
    {
        assert((width == 32) || (width == 64));
        return (width == 64) ? UINT64_MAX : 0xFFFFFFFFull;
    }

    // Mask with the low "n" bits set, n in [0, 64]. Analog of APInt::getLowBitsSet.
    static uint64_t LowMask(unsigned n)
    {
        assert(n <= 64);
        return (n == 0) ? 0 : (UINT64_MAX >> (64 - n));
    }

    // Port of llvm::KnownBits::isUnknown.
    bool IsUnknown() const
    {
        return (knownZero == 0) && (knownOne == 0);
    }

    // Port of llvm::KnownBits::isConstant.
    bool IsConstant(unsigned width) const
    {
        const uint64_t mask = WidthMask(width);
        return ((knownZero | knownOne) & mask) == mask;
    }

    // Port of llvm::KnownBits::getConstant.
    uint64_t GetConstant(unsigned width) const
    {
        assert(IsConstant(width));
        return knownOne & WidthMask(width);
    }

    // Port of llvm::KnownBits::trunc.
    KnownBits Truncate(unsigned width) const
    {
        const uint64_t mask = WidthMask(width);
        return KnownBits(knownZero & mask, knownOne & mask);
    }

    // Port of llvm::KnownBits::makeConstant.
    static KnownBits FromConstant(uint64_t value, unsigned width)
    {
        const uint64_t mask = WidthMask(width);
        value &= mask;
        return KnownBits(~value & mask, value);
    }

    // KnownBits with all bits above the highest set bit of maxVal forced to 0. Used to fold an
    // unsigned upper bound "value <= maxVal" into the lattice.
    static KnownBits FromUnsignedUpperBound(uint64_t maxVal, unsigned width)
    {
        const uint64_t mask = WidthMask(width);
        if (maxVal >= mask)
        {
            return KnownBits();
        }
        // bitLen = number of bits needed to represent maxVal (0 when maxVal == 0); LeadingZeroCount(0) == 64.
        const unsigned bitLen = 64 - (unsigned)BitOperations::LeadingZeroCount(maxVal);
        return KnownBits(~LowMask(bitLen) & mask, 0);
    }

    // Combine two facts about the *same* value (assertion refinement). Conflicting bits (one says
    // 0, the other 1) imply a dead path and are dropped to "unknown" so we never assert a false
    // fact. This is llvm::KnownBits::unionWith with the conflict-drop step folded in; the name
    // describes the *intersection* of the two sets of possible values.
    static KnownBits Intersect(const KnownBits& a, const KnownBits& b)
    {
        const uint64_t z        = a.knownZero | b.knownZero;
        const uint64_t o        = a.knownOne | b.knownOne;
        const uint64_t conflict = z & o;
        return KnownBits(z & ~conflict, o & ~conflict);
    }

    // Merge facts across two possible values (e.g. phi inputs). Port of llvm::KnownBits::intersectWith.
    static KnownBits Union(const KnownBits& a, const KnownBits& b)
    {
        return KnownBits(a.knownZero & b.knownZero, a.knownOne & b.knownOne);
    }

    // Sign-extend the low "width" bits of "value" to a 64-bit signed integer.
    static int64_t SignExtend(uint64_t value, unsigned width)
    {
        if (width == 64)
        {
            return (int64_t)value;
        }
        const uint64_t mask    = WidthMask(width);
        const uint64_t signBit = 1ull << (width - 1);
        value &= mask;
        return (int64_t)(((value & signBit) != 0) ? (value | ~mask) : value);
    }

    // Express as a signed [lo, hi] range. Succeeds only when the sign bit is known (otherwise the
    // range would straddle 0). Combines llvm::KnownBits::getSignedMinValue / getSignedMaxValue,
    // gated on a known sign bit so the caller gets a single contiguous interval.
    bool TryGetSignedRange(unsigned width, int64_t* lo, int64_t* hi) const
    {
        const uint64_t mask    = WidthMask(width);
        const uint64_t signBit = 1ull << (width - 1);
        const uint64_t minBits = knownOne & mask;   // unknown bits taken as 0
        const uint64_t maxBits = ~knownZero & mask; // unknown bits taken as 1

        if ((knownZero & signBit) != 0)
        {
            // Sign bit known 0 => value is non-negative.
            *lo = (int64_t)minBits;
            *hi = (int64_t)maxBits;
            return true;
        }
        if ((knownOne & signBit) != 0)
        {
            // Sign bit known 1 => value is negative; sign-extend both bounds.
            *lo = SignExtend(minBits, width);
            *hi = SignExtend(maxBits, width);
            return true;
        }
        return false;
    }

    // Port of llvm::KnownBits::getMinValue / getMaxValue (unsigned).
    uint64_t GetUMin(unsigned width) const
    {
        return knownOne & WidthMask(width);
    }
    uint64_t GetUMax(unsigned width) const
    {
        return ~knownZero & WidthMask(width);
    }

    // Derive KnownBits of "num" from its VN structure and the incoming assertions. Bit-level
    // analog of RangeCheck::GetRangeFromAssertions. Returns the fully-unknown lattice on
    // unsupported types or when "budget" is exhausted. See knownbits.cpp.
    static KnownBits Compute(Compiler* comp, ValueNum num, ASSERT_VALARG_TP assertions, int budget = 10);
};

// Transfer functions from operand KnownBits to result KnownBits. Each is a port of the matching
// routine in llvm/lib/Support/KnownBits.cpp; the logic is unchanged, only APInt is replaced by
// uint64_t + explicit masking.
struct KnownBitsOps
{
    // Port of llvm::KnownBits::operator&=. Bit is 0 if either operand bit is 0; 1 only if both are 1.
    static KnownBits And(const KnownBits& a, const KnownBits& b)
    {
        return KnownBits(a.knownZero | b.knownZero, a.knownOne & b.knownOne);
    }

    // Port of llvm::KnownBits::operator|=. Bit is 1 if either operand bit is 1; 0 only if both are 0.
    static KnownBits Or(const KnownBits& a, const KnownBits& b)
    {
        return KnownBits(a.knownZero & b.knownZero, a.knownOne | b.knownOne);
    }

    // Port of llvm::KnownBits::udiv (we keep only the leading-zeros result and omit the
    // exact-division low-bit refinement which doesn't trigger in measurements).
    static KnownBits UDiv(const KnownBits& a, const KnownBits& b, unsigned width)
    {
        const uint64_t mask     = KnownBits::WidthMask(width);
        const uint64_t maxNum   = ~a.knownZero & mask; // a.getMaxValue
        const uint64_t minDenom = b.knownOne & mask;   // b.getMinValue
        if (maxNum == 0)
        {
            // 0 / x == 0. Matches LLVM's "if (LHS.isZero()) ... setAllZero".
            return KnownBits::FromConstant(0, width);
        }
        // Largest possible result = maxNumerator / minDenominator; LLVM falls back to maxNum when
        // minDenom == 0, we do the same.
        const uint64_t maxRes = (minDenom == 0) ? maxNum : (maxNum / minDenom);
        const unsigned bitLen = (maxRes == 0) ? 0 : (64 - (unsigned)BitOperations::LeadingZeroCount(maxRes));
        if (bitLen >= width)
        {
            return KnownBits();
        }
        return KnownBits(mask & ~KnownBits::LowMask(bitLen), 0);
    }

    // KnownBits of a cast from "srcWidth" bits to "castToType". Combines llvm::KnownBits::trunc
    // and zext/sext into a single helper, because in the JIT a GT_CAST does both at once: it
    // narrows to the value-bits of castToType and then sign- or zero-extends to the destination's
    // normalized integer width (TYP_INT/TYP_LONG).
    static KnownBits Cast(const KnownBits& srcKB, unsigned srcWidth, var_types castToType, bool srcIsUnsigned)
    {
        const unsigned vb       = genTypeSize(castToType) * BITS_PER_BYTE; // value bits of dest type
        const unsigned dstWidth = (vb <= 32) ? 32 : 64;                    // normalized to int/long
        const unsigned passBits = min(vb, srcWidth);
        const bool     isWiden  = (vb > srcWidth);

        // Low "passBits" bits pass through (LLVM trunc).
        const uint64_t lowMask = KnownBits::LowMask(passBits);
        KnownBits      result(srcKB.knownZero & lowMask, srcKB.knownOne & lowMask);

        // Higher bits: zero-extend if unsigned (LLVM zext), otherwise replicate the sign bit when
        // it is known (LLVM sext).
        if (passBits < dstWidth)
        {
            const uint64_t extMask = KnownBits::LowMask(dstWidth) & ~lowMask;
            const uint64_t signBit = 1ull << (passBits - 1);
            const bool     unsignd = isWiden ? srcIsUnsigned : varTypeIsUnsigned(castToType);
            if (unsignd)
            {
                result.knownZero |= extMask; // zext
            }
            else if ((srcKB.knownOne & signBit) != 0)
            {
                result.knownOne |= extMask; // sext, sign known 1
            }
            else if ((srcKB.knownZero & signBit) != 0)
            {
                result.knownZero |= extMask; // sext, sign known 0
            }
        }
        return result.Truncate(dstWidth);
    }

    // Port of llvm::KnownBits::eq/ne/ult/ule/ugt/uge/slt/sle/sgt/sge (all min/max based), fused
    // into one switch. Returns 1 when the comparison is always true, 0 when always false, -1 when
    // undetermined (LLVM returns std::optional<bool>; -1 corresponds to std::nullopt).
    static int EvalRelop(genTreeOps oper, bool isUnsigned, const KnownBits& a, const KnownBits& b, unsigned width)
    {
        const uint64_t mask = KnownBits::WidthMask(width);

        // Equality (LLVM KnownBits::eq): a and b must differ if some bit is known 1 in one and
        // known 0 in the other ("LHS.One.intersects(RHS.Zero) || RHS.One.intersects(LHS.Zero)").
        if ((oper == GT_EQ) || (oper == GT_NE))
        {
            const bool mustDiffer =
                ((a.knownOne & b.knownZero & mask) != 0) || ((a.knownZero & b.knownOne & mask) != 0);
            if (mustDiffer)
            {
                return (oper == GT_EQ) ? 0 : 1;
            }
            if (a.IsConstant(width) && b.IsConstant(width))
            {
                return ((a.GetConstant(width) == b.GetConstant(width)) == (oper == GT_EQ)) ? 1 : 0;
            }
            return -1;
        }

        // Ordered comparisons reduce to min/max. Unsigned uses GetUMin/GetUMax directly; signed
        // follows LLVM's getSignedMinValue/getSignedMaxValue which sign-extend after fixing the
        // sign bit.
        const uint64_t signBit = 1ull << (width - 1);
        auto           minMax  = [=](const KnownBits& kb, uint64_t* lo, uint64_t* hi) {
            uint64_t mn = kb.knownOne & mask;
            uint64_t mx = ~kb.knownZero & mask;
            if (!isUnsigned)
            {
                if ((kb.knownZero & signBit) == 0)
                    mn |= signBit; // sign could be 1 => most-negative candidate sets it
                if ((kb.knownOne & signBit) == 0)
                    mx &= ~signBit; // sign could be 0 => most-positive candidate clears it
                mn = (uint64_t)KnownBits::SignExtend(mn, width);
                mx = (uint64_t)KnownBits::SignExtend(mx, width);
            }
            *lo = mn;
            *hi = mx;
        };

        uint64_t aMin, aMax, bMin, bMax;
        minMax(a, &aMin, &aMax);
        minMax(b, &bMin, &bMax);

        auto lt = [=](uint64_t x, uint64_t y) {
            return isUnsigned ? (x < y) : ((int64_t)x < (int64_t)y);
        };
        auto le = [=](uint64_t x, uint64_t y) {
            return isUnsigned ? (x <= y) : ((int64_t)x <= (int64_t)y);
        };

        // Same shape as LLVM's ult/ule/ugt/uge (and their signed counterparts): decidable iff one
        // side's max is strictly/non-strictly below the other side's min.
        switch (oper)
        {
            case GT_LT:
                return lt(aMax, bMin) ? 1 : (le(bMax, aMin) ? 0 : -1);
            case GT_LE:
                return le(aMax, bMin) ? 1 : (lt(bMax, aMin) ? 0 : -1);
            case GT_GT:
                return lt(bMax, aMin) ? 1 : (le(aMax, bMin) ? 0 : -1);
            case GT_GE:
                return le(bMax, aMin) ? 1 : (lt(aMax, bMin) ? 0 : -1);
            default:
                return -1;
        }
    }
};
