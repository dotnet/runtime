// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//===----------------------------------------------------------------------===//
//
// This file is a port of LLVM's "known bits" infrastructure. It tracks, for an integer value,
// which bits are known to be zero and which are known to be one, and provides the transfer
// functions that compute the known bits of an operation's result from those of its operands.
//
// Everything in this file is derived from the LLVM Project:
//
//   class  APInt              - llvm/include/llvm/ADT/APInt.h, llvm/lib/Support/APInt.cpp
//                               (a subset, limited to values that fit into a single 64-bit word)
//   struct KnownBits          - llvm/include/llvm/Support/KnownBits.h, llvm/lib/Support/KnownBits.cpp
//   struct KnownBitsRange     - the KnownBits related parts of llvm/lib/IR/ConstantRange.cpp
//                               (toKnownBits)
//   struct KnownBitsTracking  - the IR independent parts of llvm/lib/Analysis/ValueTracking.cpp
//                               (computeKnownBitsMul)
//
// Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
// See https://llvm.org/LICENSE.txt for license information.
// SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
//
// The code deliberately keeps LLVM's names and structure so that it can be compared against the
// LLVM sources, and it contains nothing that is specific to the JIT: the JIT's own logic (walking
// value numbers and assertions) lives in knownbitsanalysis.cpp.
//
// Differences from LLVM:
//  * Only the functions that the JIT uses (directly or indirectly) are here.
//  * APInt is limited to 64 bits, std::optional<bool> is replaced with the JIT's
//    RelopEvaluationResult.
//  * Functionality that has no JIT counterpart is omitted: nuw/nsw flags on shifts, exact
//    divisions and shifts, saturating arithmetic, funnel shifts, min/max, pext/pdep, etc.
//
//===----------------------------------------------------------------------===//

#ifndef _KNOWNBITS_H_
#define _KNOWNBITS_H_

// A single-word (at most 64 bits) subset of llvm::APInt: an unsigned integer of a fixed bit width
// with two's complement semantics. Bits above BitWidth are always zero.
class APInt
{
    uint64_t U        = 0;
    unsigned BitWidth = 1;

    static uint64_t maskTrailingOnes(unsigned N)
    {
        assert(N <= 64);
        return (N == 0) ? 0 : (UINT64_MAX >> (64 - N));
    }

    static int64_t SignExtend64(uint64_t X, unsigned B)
    {
        assert((B > 0) && (B <= 64));
        return (int64_t)(X << (64 - B)) >> (64 - B);
    }

public:
    APInt() = default;

    APInt(unsigned numBits, uint64_t val)
        : U(val & maskTrailingOnes(numBits))
        , BitWidth(numBits)
    {
    }

    static APInt getAllOnes(unsigned numBits)
    {
        return APInt(numBits, UINT64_MAX);
    }
    static APInt getMaxValue(unsigned numBits)
    {
        return getAllOnes(numBits);
    }
    static APInt getSignedMaxValue(unsigned numBits)
    {
        APInt API = getAllOnes(numBits);
        API.clearBit(numBits - 1);
        return API;
    }
    static APInt getSignedMinValue(unsigned numBits)
    {
        APInt API(numBits, 0);
        API.setBit(numBits - 1);
        return API;
    }
    static APInt getHighBitsSet(unsigned numBits, unsigned hiBitsSet)
    {
        APInt Res(numBits, 0);
        Res.setHighBits(hiBitsSet);
        return Res;
    }
    static APInt getLowBitsSet(unsigned numBits, unsigned loBitsSet)
    {
        APInt Res(numBits, 0);
        Res.setLowBits(loBitsSet);
        return Res;
    }

    unsigned getBitWidth() const
    {
        return BitWidth;
    }
    uint64_t getZExtValue() const
    {
        return U;
    }
    int64_t getSExtValue() const
    {
        return SignExtend64(U, BitWidth);
    }
    uint64_t getLimitedValue(uint64_t Limit = UINT64_MAX) const
    {
        return (U > Limit) ? Limit : U;
    }

    bool isNegative() const
    {
        return (*this)[BitWidth - 1];
    }
    bool isNonNegative() const
    {
        return !isNegative();
    }
    bool isSignBitSet() const
    {
        return (*this)[BitWidth - 1];
    }
    bool isSignBitClear() const
    {
        return !isSignBitSet();
    }
    bool isAllOnes() const
    {
        return U == maskTrailingOnes(BitWidth);
    }
    bool isZero() const
    {
        return U == 0;
    }
    bool isMinSignedValue() const
    {
        return U == (uint64_t(1) << (BitWidth - 1));
    }
    bool isMaxSignedValue() const
    {
        return U == ((uint64_t(1) << (BitWidth - 1)) - 1);
    }
    bool isPowerOf2() const
    {
        return (U != 0) && ((U & (U - 1)) == 0);
    }
    bool operator[](unsigned bitPosition) const
    {
        assert(bitPosition < BitWidth);
        return ((U >> bitPosition) & 1) != 0;
    }
    bool intersects(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return (U & RHS.U) != 0;
    }
    bool isSubsetOf(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return (U & ~RHS.U) == 0;
    }
    bool isInverseOf(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return (U ^ RHS.U) == maskTrailingOnes(BitWidth);
    }

    void setAllBits()
    {
        U = maskTrailingOnes(BitWidth);
    }
    void setBit(unsigned BitPosition)
    {
        assert(BitPosition < BitWidth);
        U |= uint64_t(1) << BitPosition;
    }
    void setSignBit()
    {
        setBit(BitWidth - 1);
    }
    void setBits(unsigned loBit, unsigned hiBit)
    {
        assert((hiBit <= BitWidth) && (loBit <= hiBit));
        if (loBit != hiBit)
        {
            U |= maskTrailingOnes(hiBit - loBit) << loBit;
        }
    }
    void setBitsFrom(unsigned loBit)
    {
        setBits(loBit, BitWidth);
    }
    void setLowBits(unsigned loBits)
    {
        setBits(0, loBits);
    }
    void setHighBits(unsigned hiBits)
    {
        setBits(BitWidth - hiBits, BitWidth);
    }
    void clearAllBits()
    {
        U = 0;
    }
    void clearBit(unsigned BitPosition)
    {
        assert(BitPosition < BitWidth);
        U &= ~(uint64_t(1) << BitPosition);
    }
    void clearLowBits(unsigned loBits)
    {
        assert(loBits <= BitWidth);
        APInt Keep = getHighBitsSet(BitWidth, BitWidth - loBits);
        *this &= Keep;
    }
    void clearSignBit()
    {
        clearBit(BitWidth - 1);
    }

    unsigned countl_zero() const
    {
        if (U == 0)
        {
            return BitWidth;
        }
        return BitOperations::LeadingZeroCount(U) - (64 - BitWidth);
    }
    unsigned countl_one() const
    {
        return (~*this).countl_zero();
    }
    unsigned countr_zero() const
    {
        if (U == 0)
        {
            return BitWidth;
        }
        return BitOperations::TrailingZeroCount(U);
    }
    unsigned countr_one() const
    {
        return (~*this).countr_zero();
    }
    unsigned countLeadingZeros() const
    {
        return countl_zero();
    }
    unsigned countLeadingOnes() const
    {
        return countl_one();
    }

    APInt operator~() const
    {
        return APInt(BitWidth, ~U);
    }
    APInt operator-() const
    {
        return APInt(BitWidth, 0 - U);
    }
    APInt& operator&=(const APInt& RHS)
    {
        assert(BitWidth == RHS.BitWidth);
        U &= RHS.U;
        return *this;
    }
    APInt& operator|=(const APInt& RHS)
    {
        assert(BitWidth == RHS.BitWidth);
        U |= RHS.U;
        return *this;
    }
    APInt& operator+=(const APInt& RHS)
    {
        assert(BitWidth == RHS.BitWidth);
        U = (U + RHS.U) & maskTrailingOnes(BitWidth);
        return *this;
    }
    APInt& operator<<=(unsigned ShiftAmt)
    {
        assert(ShiftAmt <= BitWidth);
        U = (ShiftAmt >= 64) ? 0 : ((U << ShiftAmt) & maskTrailingOnes(BitWidth));
        return *this;
    }
    APInt operator&(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return APInt(BitWidth, U & RHS.U);
    }
    APInt operator|(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return APInt(BitWidth, U | RHS.U);
    }
    APInt operator^(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return APInt(BitWidth, U ^ RHS.U);
    }
    APInt operator+(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return APInt(BitWidth, U + RHS.U);
    }
    APInt operator+(uint64_t RHS) const
    {
        return APInt(BitWidth, U + RHS);
    }
    APInt operator-(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return APInt(BitWidth, U - RHS.U);
    }
    APInt operator-(uint64_t RHS) const
    {
        return APInt(BitWidth, U - RHS);
    }
    APInt operator*(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return APInt(BitWidth, U * RHS.U);
    }
    APInt operator<<(unsigned Bits) const
    {
        APInt R(*this);
        R <<= Bits;
        return R;
    }
    bool operator==(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return U == RHS.U;
    }
    void lshrInPlace(unsigned ShiftAmt)
    {
        assert(ShiftAmt <= BitWidth);
        U = (ShiftAmt >= 64) ? 0 : (U >> ShiftAmt);
    }
    APInt lshr(unsigned shiftAmt) const
    {
        APInt R(*this);
        R.lshrInPlace(shiftAmt);
        return R;
    }
    void ashrInPlace(unsigned ShiftAmt)
    {
        assert(ShiftAmt <= BitWidth);
        int64_t SExtVAL = SignExtend64(U, BitWidth);
        if (ShiftAmt == BitWidth)
        {
            U = (uint64_t)(SExtVAL >> 63); // Fill with sign bit.
        }
        else
        {
            U = (uint64_t)(SExtVAL >> ShiftAmt);
        }
        U &= maskTrailingOnes(BitWidth);
    }
    APInt trunc(unsigned width) const
    {
        assert(width <= BitWidth);
        return APInt(width, U);
    }
    APInt zext(unsigned width) const
    {
        assert(width >= BitWidth);
        return APInt(width, U);
    }
    APInt sext(unsigned width) const
    {
        assert(width >= BitWidth);
        return APInt(width, (uint64_t)SignExtend64(U, BitWidth));
    }
    APInt zextOrTrunc(unsigned width) const
    {
        return APInt(width, U);
    }
    APInt getLoBits(unsigned numBits) const
    {
        APInt Result(getLowBitsSet(BitWidth, numBits));
        Result &= *this;
        return Result;
    }
    uint64_t extractBitsAsZExtValue(unsigned numBits, unsigned bitPosition) const
    {
        assert((bitPosition < BitWidth) && ((numBits + bitPosition) <= BitWidth));
        return (U >> bitPosition) & maskTrailingOnes(numBits);
    }
    bool ult(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return U < RHS.U;
    }
    bool ule(const APInt& RHS) const
    {
        return !RHS.ult(*this);
    }
    bool ugt(const APInt& RHS) const
    {
        return RHS.ult(*this);
    }
    bool uge(const APInt& RHS) const
    {
        return !ult(RHS);
    }
    bool slt(const APInt& RHS) const
    {
        assert(BitWidth == RHS.BitWidth);
        return getSExtValue() < RHS.getSExtValue();
    }
    bool sle(const APInt& RHS) const
    {
        return !RHS.slt(*this);
    }
    bool sgt(const APInt& RHS) const
    {
        return RHS.slt(*this);
    }
    bool sge(const APInt& RHS) const
    {
        return !slt(RHS);
    }

    APInt udiv(const APInt& RHS) const
    {
        assert((BitWidth == RHS.BitWidth) && (RHS.U != 0));
        return APInt(BitWidth, U / RHS.U);
    }
    APInt sdiv(const APInt& RHS) const
    {
        if (isNegative())
        {
            if (RHS.isNegative())
            {
                return (-(*this)).udiv(-RHS);
            }
            return -((-(*this)).udiv(RHS));
        }
        if (RHS.isNegative())
        {
            return -(this->udiv(-RHS));
        }
        return this->udiv(RHS);
    }

    APInt sadd_ov(const APInt& RHS, bool& Overflow) const
    {
        APInt Res = *this + RHS;
        Overflow  = (isNonNegative() == RHS.isNonNegative()) && (Res.isNonNegative() != isNonNegative());
        return Res;
    }
    APInt uadd_ov(const APInt& RHS, bool& Overflow) const
    {
        APInt Res = *this + RHS;
        Overflow  = Res.ult(RHS);
        return Res;
    }
    APInt ssub_ov(const APInt& RHS, bool& Overflow) const
    {
        APInt Res = *this - RHS;
        Overflow  = (isNonNegative() != RHS.isNonNegative()) && (Res.isNonNegative() != isNonNegative());
        return Res;
    }
    APInt usub_ov(const APInt& RHS, bool& Overflow) const
    {
        APInt Res = *this - RHS;
        Overflow  = Res.ugt(*this);
        return Res;
    }
    APInt umul_ov(const APInt& RHS, bool& Overflow) const
    {
        if ((countl_zero() + RHS.countl_zero() + 2) <= BitWidth)
        {
            Overflow = true;
            return *this * RHS;
        }

        APInt Res = lshr(1) * RHS;
        Overflow  = Res.isNegative();
        Res <<= 1;
        if ((*this)[0])
        {
            Res += RHS;
            if (Res.ult(RHS))
            {
                Overflow = true;
            }
        }
        return Res;
    }
    APInt ushl_ov(unsigned ShAmt, bool& Overflow) const
    {
        Overflow = ShAmt >= getBitWidth();
        if (Overflow)
        {
            return APInt(BitWidth, 0);
        }

        Overflow = ShAmt > countl_zero();

        return *this << ShAmt;
    }
    APInt sadd_sat(const APInt& RHS) const
    {
        bool  Overflow;
        APInt Res = sadd_ov(RHS, Overflow);
        if (!Overflow)
        {
            return Res;
        }

        return isNegative() ? APInt::getSignedMinValue(BitWidth) : APInt::getSignedMaxValue(BitWidth);
    }
    APInt uadd_sat(const APInt& RHS) const
    {
        bool  Overflow;
        APInt Res = uadd_ov(RHS, Overflow);
        if (!Overflow)
        {
            return Res;
        }

        return APInt::getMaxValue(BitWidth);
    }
    APInt ssub_sat(const APInt& RHS) const
    {
        bool  Overflow;
        APInt Res = ssub_ov(RHS, Overflow);
        if (!Overflow)
        {
            return Res;
        }

        return isNegative() ? APInt::getSignedMinValue(BitWidth) : APInt::getSignedMaxValue(BitWidth);
    }
    APInt usub_sat(const APInt& RHS) const
    {
        bool  Overflow;
        APInt Res = usub_ov(RHS, Overflow);
        if (!Overflow)
        {
            return Res;
        }

        return APInt(BitWidth, 0);
    }

    // APIntOps::umin
    static const APInt& umin(const APInt& A, const APInt& B)
    {
        return A.ult(B) ? A : B;
    }

    // APIntOps::umax
    static const APInt& umax(const APInt& A, const APInt& B)
    {
        return A.ugt(B) ? A : B;
    }

    // APIntOps::smin
    static const APInt& smin(const APInt& A, const APInt& B)
    {
        return A.slt(B) ? A : B;
    }

    // APIntOps::smax
    static const APInt& smax(const APInt& A, const APInt& B)
    {
        return A.sgt(B) ? A : B;
    }

    // APIntOps::GetMostSignificantDifferentBit. Returns false when A == B (std::nullopt in LLVM).
    static bool GetMostSignificantDifferentBit(const APInt& A, const APInt& B, unsigned* DifferentBit)
    {
        assert(A.getBitWidth() == B.getBitWidth());
        if (A == B)
        {
            return false;
        }
        *DifferentBit = A.getBitWidth() - ((A ^ B).countl_zero() + 1);
        return true;
    }
};

// Struct for tracking the known zeros and ones of a value.
struct KnownBits
{
    APInt Zero;
    APInt One;

    // Internal constructor for creating a KnownBits from two APInts.
    KnownBits(APInt Zero, APInt One)
        : Zero(Zero)
        , One(One)
    {
    }

private:
    static KnownBits computeForAddCarry(const KnownBits& LHS, const KnownBits& RHS, bool CarryZero, bool CarryOne)
    {
        APInt PossibleSumZero = LHS.getMaxValue() + RHS.getMaxValue() + (uint64_t)!CarryZero;
        APInt PossibleSumOne  = LHS.getMinValue() + RHS.getMinValue() + (uint64_t)CarryOne;

        // Compute known bits of the carry.
        APInt CarryKnownZero = ~(PossibleSumZero ^ LHS.Zero ^ RHS.Zero);
        APInt CarryKnownOne  = PossibleSumOne ^ LHS.One ^ RHS.One;

        // Compute set of known bits (where all three relevant bits are known).
        APInt LHSKnownUnion   = LHS.Zero | LHS.One;
        APInt RHSKnownUnion   = RHS.Zero | RHS.One;
        APInt CarryKnownUnion = CarryKnownZero | CarryKnownOne;
        APInt Known           = LHSKnownUnion & RHSKnownUnion & CarryKnownUnion;

        // Compute known bits of the result.
        KnownBits KnownOut;
        KnownOut.Zero = ~PossibleSumZero & Known;
        KnownOut.One  = PossibleSumOne & Known;
        return KnownOut;
    }

    static unsigned getMaxShiftAmount(const APInt& MaxValue, unsigned BitWidth)
    {
        if ((BitWidth & (BitWidth - 1)) == 0)
        {
            // Clamp to the shift amount's width: a narrower amount is already
            // < BitWidth, so this stays a valid upper bound.
            unsigned Log2 = BitOperations::Log2((uint32_t)BitWidth);
            return (unsigned)MaxValue.extractBitsAsZExtValue(std::min(Log2, MaxValue.getBitWidth()), 0);
        }
        // This is only an approximate upper bound.
        return (unsigned)MaxValue.getLimitedValue(BitWidth - 1);
    }

    // Internal helper for getting the initial KnownBits for an `srem` or `urem`
    // operation with the low-bits set.
    static KnownBits remGetLowBits(const KnownBits& LHS, const KnownBits& RHS)
    {
        unsigned BitWidth = LHS.getBitWidth();
        if (!RHS.isZero() && RHS.Zero[0])
        {
            // rem X, Y where Y[0:N] is zero will preserve X[0:N] in the result.
            unsigned RHSZeros  = RHS.countMinTrailingZeros();
            APInt    Mask      = APInt::getLowBitsSet(BitWidth, RHSZeros);
            APInt    OnesMask  = LHS.One & Mask;
            APInt    ZerosMask = LHS.Zero & Mask;
            return KnownBits(ZerosMask, OnesMask);
        }
        return KnownBits(BitWidth);
    }

public:
    // Default construct Zero and One.
    KnownBits() = default;

    /// Create a known bits object of BitWidth bits initialized to unknown.
    explicit KnownBits(unsigned BitWidth)
        : Zero(BitWidth, 0)
        , One(BitWidth, 0)
    {
    }

    /// Get the bit width of this value.
    unsigned getBitWidth() const
    {
        assert(Zero.getBitWidth() == One.getBitWidth());
        return Zero.getBitWidth();
    }

    /// Returns true if there is conflicting information.
    bool hasConflict() const
    {
        return Zero.intersects(One);
    }

    /// Returns true if we know the value of all bits.
    bool isConstant() const
    {
        return Zero.isInverseOf(One);
    }

    /// Returns the value when all bits have a known value. This just returns One
    /// with a protective assertion.
    const APInt& getConstant() const
    {
        assert(isConstant());
        return One;
    }

    /// Returns true if we don't know any bits.
    bool isUnknown() const
    {
        return Zero.isZero() && One.isZero();
    }

    /// Resets the known state of all bits.
    void resetAll()
    {
        Zero.clearAllBits();
        One.clearAllBits();
    }

    /// Returns true if value is all zero.
    bool isZero() const
    {
        return Zero.isAllOnes();
    }

    /// Returns true if value is all one bits.
    bool isAllOnes() const
    {
        return One.isAllOnes();
    }

    /// Make all bits known to be zero and discard any previous information.
    void setAllZero()
    {
        Zero.setAllBits();
        One.clearAllBits();
    }

    /// Make all bits known to be both zero and one. Useful before a loop that
    /// calls intersectWith.
    void setAllConflict()
    {
        Zero.setAllBits();
        One.setAllBits();
    }

    /// Returns true if this value is known to be negative.
    bool isNegative() const
    {
        return One.isSignBitSet();
    }

    /// Returns true if this value is known to be non-negative.
    bool isNonNegative() const
    {
        return Zero.isSignBitSet();
    }

    /// Returns true if this value is known to be non-zero.
    bool isNonZero() const
    {
        return !One.isZero();
    }

    /// Returns true if this value is known to be positive.
    bool isStrictlyPositive() const
    {
        return Zero.isSignBitSet() && !One.isZero();
    }

    /// Make this value negative.
    void makeNegative()
    {
        One.setSignBit();
    }

    /// Make this value non-negative.
    void makeNonNegative()
    {
        Zero.setSignBit();
    }

    /// Return the minimal unsigned value possible given these KnownBits.
    APInt getMinValue() const
    {
        // Assume that all bits that aren't known-ones are zeros.
        return One;
    }

    /// Return the minimal signed value possible given these KnownBits.
    APInt getSignedMinValue() const
    {
        // Assume that all bits that aren't known-ones are zeros.
        APInt Min = One;
        // Sign bit is unknown.
        if (Zero.isSignBitClear())
        {
            Min.setSignBit();
        }
        return Min;
    }

    /// Return the maximal unsigned value possible given these KnownBits.
    APInt getMaxValue() const
    {
        // Assume that all bits that aren't known-zeros are ones.
        return ~Zero;
    }

    /// Return the maximal signed value possible given these KnownBits.
    APInt getSignedMaxValue() const
    {
        // Assume that all bits that aren't known-zeros are ones.
        APInt Max = ~Zero;
        // Sign bit is unknown.
        if (One.isSignBitClear())
        {
            Max.clearSignBit();
        }
        return Max;
    }

    /// Return known bits for a truncation of the value we're tracking.
    KnownBits trunc(unsigned BitWidth) const
    {
        return KnownBits(Zero.trunc(BitWidth), One.trunc(BitWidth));
    }

    /// Return known bits for a zero extension of the value we're tracking.
    KnownBits zext(unsigned BitWidth) const
    {
        unsigned OldBitWidth = getBitWidth();
        APInt    NewZero     = Zero.zext(BitWidth);
        NewZero.setBitsFrom(OldBitWidth);
        return KnownBits(NewZero, One.zext(BitWidth));
    }

    /// Return known bits for a sign extension of the value we're tracking.
    KnownBits sext(unsigned BitWidth) const
    {
        return KnownBits(Zero.sext(BitWidth), One.sext(BitWidth));
    }

    /// Returns the minimum number of trailing zero bits.
    unsigned countMinTrailingZeros() const
    {
        return Zero.countr_one();
    }

    /// Returns the minimum number of leading zero bits.
    unsigned countMinLeadingZeros() const
    {
        return Zero.countl_one();
    }

    /// Returns the minimum number of leading one bits.
    unsigned countMinLeadingOnes() const
    {
        return One.countl_one();
    }

    /// Returns the number of times the sign bit is replicated into the other
    /// bits.
    unsigned countMinSignBits() const
    {
        if (isNonNegative())
        {
            return countMinLeadingZeros();
        }
        if (isNegative())
        {
            return countMinLeadingOnes();
        }
        // Every value has at least 1 sign bit.
        return 1;
    }

    /// Create known bits from a known constant.
    static KnownBits makeConstant(const APInt& C)
    {
        return KnownBits(~C, C);
    }

    /// Returns KnownBits information that is known to be true for both this and
    /// RHS.
    ///
    /// When an operation is known to return one of its operands, this can be used
    /// to combine information about the known bits of the operands to get the
    /// information that must be true about the result.
    KnownBits intersectWith(const KnownBits& RHS) const
    {
        return KnownBits(Zero & RHS.Zero, One & RHS.One);
    }

    /// Returns KnownBits information that is known to be true for either this or
    /// RHS or both.
    ///
    /// This can be used to combine different sources of information about the
    /// known bits of a single value, e.g. information about the low bits and the
    /// high bits of the result of a multiplication.
    KnownBits unionWith(const KnownBits& RHS) const
    {
        return KnownBits(Zero | RHS.Zero, One | RHS.One);
    }

    /// Compute known bits resulting from adding LHS and RHS.
    static KnownBits computeForAddSub(bool Add, bool NSW, bool NUW, const KnownBits& LHS, const KnownBits& RHS)
    {
        unsigned  BitWidth = LHS.getBitWidth();
        KnownBits KnownOut(BitWidth);
        // This can be a relatively expensive helper, so optimistically save some
        // work.
        if (LHS.isUnknown() && RHS.isUnknown())
        {
            return KnownOut;
        }

        if (!LHS.isUnknown() && !RHS.isUnknown())
        {
            if (Add)
            {
                // Sum = LHS + RHS + 0
                KnownOut = computeForAddCarry(LHS, RHS, /*CarryZero=*/true, /*CarryOne=*/false);
            }
            else
            {
                // Sum = LHS + ~RHS + 1
                KnownBits NotRHS = RHS;
                std::swap(NotRHS.Zero, NotRHS.One);
                KnownOut = computeForAddCarry(LHS, NotRHS, /*CarryZero=*/false, /*CarryOne=*/true);
            }
        }

        // Handle add/sub given nsw and/or nuw.
        if (NUW)
        {
            if (Add)
            {
                // (add nuw X, Y)
                APInt MinVal = LHS.getMinValue().uadd_sat(RHS.getMinValue());
                // None of the adds can end up overflowing, so min consecutive highbits
                // in minimum possible of X + Y must all remain set.
                if (NSW)
                {
                    unsigned NumBits = MinVal.trunc(BitWidth - 1).countl_one();
                    // If we have NSW as well, we also know we can't overflow the signbit so
                    // can start counting from 1 bit back.
                    KnownOut.One.setBits(BitWidth - 1 - NumBits, BitWidth - 1);
                }
                KnownOut.One.setHighBits(MinVal.countl_one());
            }
            else
            {
                // (sub nuw X, Y)
                APInt MaxVal = LHS.getMaxValue().usub_sat(RHS.getMinValue());
                // None of the subs can overflow at any point, so any common high bits
                // will subtract away and result in zeros.
                if (NSW)
                {
                    // If we have NSW as well, we also know we can't overflow the signbit so
                    // can start counting from 1 bit back.
                    unsigned NumBits = MaxVal.trunc(BitWidth - 1).countl_zero();
                    KnownOut.Zero.setBits(BitWidth - 1 - NumBits, BitWidth - 1);
                }
                KnownOut.Zero.setHighBits(MaxVal.countl_zero());
            }
        }

        if (NSW)
        {
            APInt MinVal;
            APInt MaxVal;
            if (Add)
            {
                // (add nsw X, Y)
                MinVal = LHS.getSignedMinValue().sadd_sat(RHS.getSignedMinValue());
                MaxVal = LHS.getSignedMaxValue().sadd_sat(RHS.getSignedMaxValue());
            }
            else
            {
                // (sub nsw X, Y)
                MinVal = LHS.getSignedMinValue().ssub_sat(RHS.getSignedMaxValue());
                MaxVal = LHS.getSignedMaxValue().ssub_sat(RHS.getSignedMinValue());
            }
            if (MinVal.isNonNegative())
            {
                // If min is non-negative, result will always be non-neg (can't overflow
                // around).
                unsigned NumBits = MinVal.trunc(BitWidth - 1).countl_one();
                KnownOut.One.setBits(BitWidth - 1 - NumBits, BitWidth - 1);
                KnownOut.Zero.setSignBit();
            }
            if (MaxVal.isNegative())
            {
                // If max is negative, result will always be neg (can't overflow around).
                unsigned NumBits = MaxVal.trunc(BitWidth - 1).countl_zero();
                KnownOut.Zero.setBits(BitWidth - 1 - NumBits, BitWidth - 1);
                KnownOut.One.setSignBit();
            }
        }

        // Just return 0 if the nsw/nuw is violated and we have poison.
        if (KnownOut.hasConflict())
        {
            KnownOut.setAllZero();
        }
        return KnownOut;
    }

    /// Compute knownbits resulting from subtraction of LHS and RHS.
    static KnownBits sub(const KnownBits& LHS, const KnownBits& RHS, bool NSW = false, bool NUW = false)
    {
        return computeForAddSub(/*Add=*/false, NSW, NUW, LHS, RHS);
    }

    /// Compute known bits resulting from multiplying LHS and RHS.
    static KnownBits mul(const KnownBits& LHS, const KnownBits& RHS, bool NoUndefSelfMultiply = false)
    {
        unsigned BitWidth = LHS.getBitWidth();
        assert(BitWidth == RHS.getBitWidth());
        assert(!NoUndefSelfMultiply || (LHS == RHS));

        // Compute the high known-0 bits by multiplying the unsigned max of each side.
        // Conservatively, M active bits * N active bits results in M + N bits in the
        // result. But if we know a value is a power-of-2 for example, then this
        // computes one more leading zero.
        // TODO: This could be generalized to number of sign bits (negative numbers).
        APInt UMaxLHS = LHS.getMaxValue();
        APInt UMaxRHS = RHS.getMaxValue();

        // For leading zeros in the result to be valid, the unsigned max product must
        // fit in the bitwidth (it must not overflow).
        bool     HasOverflow;
        APInt    UMaxResult = UMaxLHS.umul_ov(UMaxRHS, HasOverflow);
        unsigned LeadZ      = HasOverflow ? 0 : UMaxResult.countl_zero();

        // The result of the bottom bits of an integer multiply can be
        // inferred by looking at the bottom bits of both operands and
        // multiplying them together.
        // We can infer at least the minimum number of known trailing bits
        // of both operands. Depending on number of trailing zeros, we can
        // infer more bits, because (a*b) <=> ((a/m) * (b/n)) * (m*n) assuming
        // a and b are divisible by m and n respectively.
        // We then calculate how many of those bits are inferrable and set
        // the output. For example, the i8 mul:
        //  a = XXXX1100 (12)
        //  b = XXXX1110 (14)
        // We know the bottom 3 bits are zero since the first can be divided by
        // 4 and the second by 2, thus having ((12/4) * (14/2)) * (2*4).
        // Applying the multiplication to the trimmed arguments gets:
        //    XX11 (3)
        //    X111 (7)
        // -------
        //    XX11
        //   XX11
        //  XX11
        // XX11
        // -------
        // XXXXX01
        // Which allows us to infer the 2 LSBs. Since we're multiplying the result
        // by 8, the bottom 3 bits will be 0, so we can infer a total of 5 bits.

        // How many times we'd be able to divide each argument by 2 (shr by 1).
        // This gives us the number of trailing zeros on the multiplication result.
        unsigned TrailBitsKnownLHS = (LHS.Zero | LHS.One).countr_one();
        unsigned TrailBitsKnownRHS = (RHS.Zero | RHS.One).countr_one();
        unsigned TrailZeroLHS      = LHS.countMinTrailingZeros();
        unsigned TrailZeroRHS      = RHS.countMinTrailingZeros();
        unsigned TrailZ            = TrailZeroLHS + TrailZeroRHS;

        // Figure out the fewest known-bits operand.
        unsigned SmallestOperand = std::min(TrailBitsKnownLHS - TrailZeroLHS, TrailBitsKnownRHS - TrailZeroRHS);
        unsigned ResultBitsKnown = std::min(SmallestOperand + TrailZ, BitWidth);

        // The lower ResultBitsKnown bits of this are known.
        APInt BottomKnown = LHS.One * RHS.One;

        KnownBits Res(BitWidth);
        Res.Zero.setHighBits(LeadZ);
        Res.Zero |= (~BottomKnown).getLoBits(ResultBitsKnown);
        Res.One = BottomKnown.getLoBits(ResultBitsKnown);

        if (NoUndefSelfMultiply)
        {
            // If X has at least TZ trailing zeroes, then bit (2 * TZ + 1) must be zero.
            unsigned TwoTZP1 = 2 * TrailZeroLHS + 1;
            if (TwoTZP1 < BitWidth)
            {
                Res.Zero.setBit(TwoTZP1);
            }

            // If X has exactly TZ trailing zeros, then bit (2 * TZ + 2) must also be
            // zero.
            if ((TrailZeroLHS < BitWidth) && LHS.One[TrailZeroLHS])
            {
                unsigned TwoTZP2 = TwoTZP1 + 1;
                if (TwoTZP2 < BitWidth)
                {
                    Res.Zero.setBit(TwoTZP2);
                }
            }
        }

        return Res;
    }

    /// Compute known bits for sdiv(LHS, RHS).
    static KnownBits sdiv(const KnownBits& LHS, const KnownBits& RHS)
    {
        // Equivalent of `udiv`. We must have caught this before it was folded.
        if (LHS.isNonNegative() && RHS.isNonNegative())
        {
            return udiv(LHS, RHS);
        }

        unsigned  BitWidth = LHS.getBitWidth();
        KnownBits Known(BitWidth);

        if (LHS.isZero() || RHS.isZero())
        {
            // Result is either known Zero or UB. Return Zero either way.
            // Checking this earlier saves us a lot of special cases later on.
            Known.setAllZero();
            return Known;
        }

        bool  HasRes = false;
        APInt Res;
        if (LHS.isNegative() && RHS.isNegative())
        {
            // Result non-negative.
            APInt Denom = RHS.getSignedMaxValue();
            APInt Num   = LHS.getSignedMinValue();
            // INT_MIN/-1 would be a poison result (impossible). Estimate the division
            // as signed max (we will only set sign bit in the result).
            Res = (Num.isMinSignedValue() && Denom.isAllOnes()) ? APInt::getSignedMaxValue(BitWidth) : Num.sdiv(Denom);
            HasRes = true;
        }
        else if (LHS.isNegative() && RHS.isNonNegative())
        {
            // Result is negative if -LHS u>= RHS.
            if ((-LHS.getSignedMaxValue()).uge(RHS.getSignedMaxValue()))
            {
                APInt Denom = RHS.getSignedMinValue();
                APInt Num   = LHS.getSignedMinValue();
                // If the denominator may be zero, use the lowest unknown bit as the
                // minimum non-zero denominator, matching udiv.
                if (Denom.isZero())
                {
                    Denom.setBit(RHS.countMinTrailingZeros());
                }
                Res    = Num.sdiv(Denom);
                HasRes = true;
            }
        }
        else if (LHS.isStrictlyPositive() && RHS.isNegative())
        {
            // Result is negative if LHS u>= -RHS.
            if (LHS.getSignedMinValue().uge(-RHS.getSignedMinValue()))
            {
                APInt Denom = RHS.getSignedMaxValue();
                APInt Num   = LHS.getSignedMaxValue();
                Res         = Num.sdiv(Denom);
                HasRes      = true;
            }
        }

        if (HasRes)
        {
            if (Res.isNonNegative())
            {
                unsigned LeadZ = Res.countLeadingZeros();
                Known.Zero.setHighBits(LeadZ);
            }
            else
            {
                unsigned LeadO = Res.countLeadingOnes();
                Known.One.setHighBits(LeadO);
            }
        }

        return Known;
    }

    /// Compute known bits for udiv(LHS, RHS).
    static KnownBits udiv(const KnownBits& LHS, const KnownBits& RHS)
    {
        unsigned  BitWidth = LHS.getBitWidth();
        KnownBits Known(BitWidth);

        if (LHS.isZero() || RHS.isZero())
        {
            // Result is either known Zero or UB. Return Zero either way.
            // Checking this earlier saves us a lot of special cases later on.
            Known.setAllZero();
            return Known;
        }

        // A zero denominator is UB, so the minimum attainable denominator is the
        // smallest non-zero value consistent with the known bits.
        APInt MinDenom = RHS.getMinValue();
        if (MinDenom.isZero())
        {
            MinDenom.setBit(RHS.countMinTrailingZeros());
        }

        // The quotient grows when the numerator grows and shrinks when the
        // denominator grows, and all four operand bounds are attainable, so the
        // result is in [MinNum / MaxDenom, MaxNum / MinDenom]. Keep the common
        // leading bits of the two bounds (as ConstantRange::toKnownBits does),
        // which can include leading ones, not just leading zeros.
        APInt MaxDenom = RHS.getMaxValue();
        APInt MaxRes   = LHS.getMaxValue().udiv(MinDenom);
        APInt MinRes   = LHS.getMinValue().udiv(MaxDenom);

        Known = KnownBits::makeConstant(MinRes);
        unsigned DifferentBit;
        if (APInt::GetMostSignificantDifferentBit(MinRes, MaxRes, &DifferentBit))
        {
            Known.Zero.clearLowBits(DifferentBit + 1);
            Known.One.clearLowBits(DifferentBit + 1);
        }

        return Known;
    }

    /// Compute known bits for urem(LHS, RHS).
    static KnownBits urem(const KnownBits& LHS, const KnownBits& RHS)
    {
        // L % R for L < R is L. (A zero divisor is UB, and if MinR is zero this
        // condition cannot hold anyway.)
        if (LHS.getMaxValue().ult(RHS.getMinValue()))
        {
            return LHS;
        }

        KnownBits Known = remGetLowBits(LHS, RHS);

        // L % R is at most L and, since a zero divisor is UB, strictly less than
        // R, so the result has at least as many leading zeros as
        // umin(MaxL, MaxR - 1), as in ConstantRange::urem. For a power-of-two
        // constant R this computes the exact L & (R - 1): remGetLowBits preserved
        // the low bits and this clears the bits from log2(R) up. If R is known to
        // be zero, MaxR - 1 wraps around, which is harmless: every input is UB.
        APInt MaxRes = APInt::umin(LHS.getMaxValue(), RHS.getMaxValue() - 1);
        Known.Zero.setHighBits(MaxRes.countLeadingZeros());
        return Known;
    }

    /// Compute known bits for srem(LHS, RHS).
    static KnownBits srem(const KnownBits& LHS, const KnownBits& RHS)
    {
        KnownBits Known = remGetLowBits(LHS, RHS);
        if (RHS.isConstant() && RHS.getConstant().isPowerOf2())
        {
            // NB: Low bits are set in `remGetLowBits`.
            APInt LowBits = RHS.getConstant() - 1;
            // If the first operand is non-negative or has all low bits zero, then
            // the upper bits are all zero.
            if (LHS.isNonNegative() || LowBits.isSubsetOf(LHS.Zero))
            {
                Known.Zero |= ~LowBits;
            }

            // If the first operand is negative and not all low bits are zero, then
            // the upper bits are all one.
            if (LHS.isNegative() && LowBits.intersects(LHS.One))
            {
                Known.One |= ~LowBits;
            }
            return Known;
        }

        // The sign bit is the LHS's sign bit, except when the result of the
        // remainder is zero. The magnitude of the result should be less than or
        // equal to the magnitude of either operand.
        if (LHS.isNegative() && Known.isNonZero())
        {
            Known.One.setHighBits(std::max(LHS.countMinLeadingOnes(), RHS.countMinSignBits()));
        }
        else if (LHS.isNonNegative())
        {
            Known.Zero.setHighBits(std::max(LHS.countMinLeadingZeros(), RHS.countMinSignBits()));
        }
        return Known;
    }

    /// Compute known bits for shl(LHS, RHS).
    /// NOTE: RHS (shift amount) bitwidth doesn't need to be the same as LHS.
    static KnownBits shl(const KnownBits& LHS, const KnownBits& RHS, bool ShAmtNonZero = false)
    {
        unsigned BitWidth     = LHS.getBitWidth();
        auto     ShiftByConst = [&](const KnownBits& LHS, unsigned ShiftAmt) {
            KnownBits Known;
            bool      ShiftedOutZero, ShiftedOutOne;
            Known.Zero = LHS.Zero.ushl_ov(ShiftAmt, ShiftedOutZero);
            Known.Zero.setLowBits(ShiftAmt);
            Known.One = LHS.One.ushl_ov(ShiftAmt, ShiftedOutOne);
            return Known;
        };

        // Fast path for a common case when LHS is completely unknown.
        KnownBits Known(BitWidth);
        unsigned  MinShiftAmount = (unsigned)RHS.getMinValue().getLimitedValue(BitWidth);
        if ((MinShiftAmount == 0) && ShAmtNonZero)
        {
            MinShiftAmount = 1;
        }
        if (LHS.isUnknown())
        {
            Known.Zero.setLowBits(MinShiftAmount);
            return Known;
        }

        // Determine maximum shift amount.
        APInt    MaxValue       = RHS.getMaxValue();
        unsigned MaxShiftAmount = getMaxShiftAmount(MaxValue, BitWidth);

        // Fast path for common case where the shift amount is unknown.
        if ((MinShiftAmount == 0) && (MaxShiftAmount == (BitWidth - 1)) && ((BitWidth & (BitWidth - 1)) == 0))
        {
            Known.Zero.setLowBits(LHS.countMinTrailingZeros());
            if (LHS.isAllOnes())
            {
                Known.One.setSignBit();
            }
            return Known;
        }

        // Find the common bits from all possible shifts.
        unsigned ShiftAmtZeroMask = (unsigned)RHS.Zero.zextOrTrunc(32).getZExtValue();
        unsigned ShiftAmtOneMask  = (unsigned)RHS.One.zextOrTrunc(32).getZExtValue();
        Known.setAllConflict();
        for (unsigned ShiftAmt = MinShiftAmount; ShiftAmt <= MaxShiftAmount; ++ShiftAmt)
        {
            // Skip if the shift amount is impossible.
            if (((ShiftAmtZeroMask & ShiftAmt) != 0) || ((ShiftAmtOneMask | ShiftAmt) != ShiftAmt))
            {
                continue;
            }
            Known = Known.intersectWith(ShiftByConst(LHS, ShiftAmt));
            if (Known.isUnknown())
            {
                break;
            }
        }

        // All shift amounts may result in poison.
        if (Known.hasConflict())
        {
            Known.setAllZero();
        }
        return Known;
    }

    /// Compute known bits for lshr(LHS, RHS).
    /// NOTE: RHS (shift amount) bitwidth doesn't need to be the same as LHS.
    static KnownBits lshr(const KnownBits& LHS, const KnownBits& RHS, bool ShAmtNonZero = false)
    {
        unsigned BitWidth     = LHS.getBitWidth();
        auto     ShiftByConst = [&](const KnownBits& LHS, unsigned ShiftAmt) {
            KnownBits Known = LHS;
            Known >>= ShiftAmt;
            // High bits are known zero.
            Known.Zero.setHighBits(ShiftAmt);
            return Known;
        };

        // Fast path for a common case when LHS is completely unknown.
        KnownBits Known(BitWidth);
        unsigned  MinShiftAmount = (unsigned)RHS.getMinValue().getLimitedValue(BitWidth);
        if ((MinShiftAmount == 0) && ShAmtNonZero)
        {
            MinShiftAmount = 1;
        }
        if (LHS.isUnknown())
        {
            Known.Zero.setHighBits(MinShiftAmount);
            return Known;
        }

        // Find the common bits from all possible shifts.
        APInt    MaxValue       = RHS.getMaxValue();
        unsigned MaxShiftAmount = getMaxShiftAmount(MaxValue, BitWidth);

        unsigned ShiftAmtZeroMask = (unsigned)RHS.Zero.zextOrTrunc(32).getZExtValue();
        unsigned ShiftAmtOneMask  = (unsigned)RHS.One.zextOrTrunc(32).getZExtValue();
        Known.setAllConflict();
        for (unsigned ShiftAmt = MinShiftAmount; ShiftAmt <= MaxShiftAmount; ++ShiftAmt)
        {
            // Skip if the shift amount is impossible.
            if (((ShiftAmtZeroMask & ShiftAmt) != 0) || ((ShiftAmtOneMask | ShiftAmt) != ShiftAmt))
            {
                continue;
            }
            Known = Known.intersectWith(ShiftByConst(LHS, ShiftAmt));
            if (Known.isUnknown())
            {
                break;
            }
        }

        // All shift amounts may result in poison.
        if (Known.hasConflict())
        {
            Known.setAllZero();
        }
        return Known;
    }

    /// Compute known bits for ashr(LHS, RHS).
    /// NOTE: RHS (shift amount) bitwidth doesn't need to be the same as LHS.
    static KnownBits ashr(const KnownBits& LHS, const KnownBits& RHS, bool ShAmtNonZero = false)
    {
        unsigned BitWidth     = LHS.getBitWidth();
        auto     ShiftByConst = [&](const KnownBits& LHS, unsigned ShiftAmt) {
            KnownBits Known = LHS;
            Known.Zero.ashrInPlace(ShiftAmt);
            Known.One.ashrInPlace(ShiftAmt);
            return Known;
        };

        // Fast path for a common case when LHS is completely unknown.
        KnownBits Known(BitWidth);
        unsigned  MinShiftAmount = (unsigned)RHS.getMinValue().getLimitedValue(BitWidth);
        if ((MinShiftAmount == 0) && ShAmtNonZero)
        {
            MinShiftAmount = 1;
        }
        if (LHS.isUnknown())
        {
            if (MinShiftAmount == BitWidth)
            {
                // Always poison. Return zero because we don't like returning conflict.
                Known.setAllZero();
                return Known;
            }
            return Known;
        }

        // Find the common bits from all possible shifts.
        APInt    MaxValue       = RHS.getMaxValue();
        unsigned MaxShiftAmount = getMaxShiftAmount(MaxValue, BitWidth);

        unsigned ShiftAmtZeroMask = (unsigned)RHS.Zero.zextOrTrunc(32).getZExtValue();
        unsigned ShiftAmtOneMask  = (unsigned)RHS.One.zextOrTrunc(32).getZExtValue();
        Known.setAllConflict();
        for (unsigned ShiftAmt = MinShiftAmount; ShiftAmt <= MaxShiftAmount; ++ShiftAmt)
        {
            // Skip if the shift amount is impossible.
            if (((ShiftAmtZeroMask & ShiftAmt) != 0) || ((ShiftAmtOneMask | ShiftAmt) != ShiftAmt))
            {
                continue;
            }
            Known = Known.intersectWith(ShiftByConst(LHS, ShiftAmt));
            if (Known.isUnknown())
            {
                break;
            }
        }

        // All shift amounts may result in poison.
        if (Known.hasConflict())
        {
            Known.setAllZero();
        }
        return Known;
    }

    /// Determine if these known bits always give the same ICMP_EQ result.
    static RelopEvaluationResult eq(const KnownBits& LHS, const KnownBits& RHS)
    {
        if (LHS.isConstant() && RHS.isConstant())
        {
            return (LHS.getConstant() == RHS.getConstant()) ? RelopEvaluationResult::True
                                                            : RelopEvaluationResult::False;
        }
        if (LHS.One.intersects(RHS.Zero) || RHS.One.intersects(LHS.Zero))
        {
            return RelopEvaluationResult::False;
        }
        return RelopEvaluationResult::Unknown;
    }

    /// Determine if these known bits always give the same ICMP_SGT result.
    static RelopEvaluationResult sgt(const KnownBits& LHS, const KnownBits& RHS)
    {
        // LHS >s RHS -> false if smax(LHS) <= smax(RHS)
        if (LHS.getSignedMaxValue().sle(RHS.getSignedMinValue()))
        {
            return RelopEvaluationResult::False;
        }
        // LHS >s RHS -> true if smin(LHS) > smax(RHS)
        if (LHS.getSignedMinValue().sgt(RHS.getSignedMaxValue()))
        {
            return RelopEvaluationResult::True;
        }
        return RelopEvaluationResult::Unknown;
    }

    /// Update known bits based on ANDing with RHS.
    KnownBits& operator&=(const KnownBits& RHS)
    {
        // Result bit is 0 if either operand bit is 0.
        Zero |= RHS.Zero;
        // Result bit is 1 if both operand bits are 1.
        One &= RHS.One;
        return *this;
    }

    /// Update known bits based on ORing with RHS.
    KnownBits& operator|=(const KnownBits& RHS)
    {
        // Result bit is 0 if both operand bits are 0.
        Zero &= RHS.Zero;
        // Result bit is 1 if either operand bit is 1.
        One |= RHS.One;
        return *this;
    }

    /// Update known bits based on XORing with RHS.
    KnownBits& operator^=(const KnownBits& RHS)
    {
        // Result bit is 0 if both operand bits are 0 or both are 1.
        APInt Z = (Zero & RHS.Zero) | (One & RHS.One);
        // Result bit is 1 if one operand bit is 0 and the other is 1.
        One  = (Zero & RHS.One) | (One & RHS.Zero);
        Zero = Z;
        return *this;
    }

    /// Shift known bits right by ShAmt. Shifted in bits are unknown.
    KnownBits& operator>>=(unsigned ShAmt)
    {
        Zero.lshrInPlace(ShAmt);
        One.lshrInPlace(ShAmt);
        return *this;
    }

    bool operator==(const KnownBits& Other) const
    {
        return (Zero == Other.Zero) && (One == Other.One);
    }
};

inline KnownBits operator&(KnownBits LHS, const KnownBits& RHS)
{
    LHS &= RHS;
    return LHS;
}

inline KnownBits operator|(KnownBits LHS, const KnownBits& RHS)
{
    LHS |= RHS;
    return LHS;
}

inline KnownBits operator^(KnownBits LHS, const KnownBits& RHS)
{
    LHS ^= RHS;
    return LHS;
}

// The KnownBits related parts of llvm::ConstantRange.
struct KnownBitsRange
{
    // ConstantRange::toKnownBits for the range of unsigned values [Min, Max] (both inclusive):
    // we can only retain the top bits that are the same between min and max.
    static KnownBits toKnownBits(const APInt& Min, const APInt& Max)
    {
        assert(Min.ule(Max));
        KnownBits Known = KnownBits::makeConstant(Min);
        unsigned  DifferentBit;
        if (APInt::GetMostSignificantDifferentBit(Min, Max, &DifferentBit))
        {
            Known.Zero.clearLowBits(DifferentBit + 1);
            Known.One.clearLowBits(DifferentBit + 1);
        }
        return Known;
    }
};

// The parts of llvm/lib/Analysis/ValueTracking.cpp that do not depend on the IR.
struct KnownBitsTracking
{
    // computeKnownBitsMul: the known bits of "mul Known2, Known" given its no-wrap flags.
    static KnownBits computeKnownBitsMul(
        const KnownBits& Known2, const KnownBits& Known, bool NSW, bool NUW, bool SelfMultiply)
    {
        bool isKnownNegative    = false;
        bool isKnownNonNegative = false;
        // If the multiplication is known not to overflow, compute the sign bit.
        if (NSW)
        {
            if (SelfMultiply)
            {
                // The product of a number with itself is non-negative.
                isKnownNonNegative = true;
            }
            else
            {
                bool isKnownNonNegativeOp1 = Known.isNonNegative();
                bool isKnownNonNegativeOp0 = Known2.isNonNegative();
                bool isKnownNegativeOp1    = Known.isNegative();
                bool isKnownNegativeOp0    = Known2.isNegative();
                // The product of two numbers with the same sign is non-negative.
                isKnownNonNegative =
                    (isKnownNegativeOp1 && isKnownNegativeOp0) || (isKnownNonNegativeOp1 && isKnownNonNegativeOp0);
                if (!isKnownNonNegative && NUW)
                {
                    // mul nuw nsw with a factor > 1 is non-negative.
                    KnownBits             One  = KnownBits::makeConstant(APInt(Known.getBitWidth(), 1));
                    RelopEvaluationResult SGT1 = KnownBits::sgt(Known, One);
                    RelopEvaluationResult SGT2 = KnownBits::sgt(Known2, One);
                    isKnownNonNegative = (SGT1 == RelopEvaluationResult::True) || (SGT2 == RelopEvaluationResult::True);
                }

                // The product of a negative number and a non-negative number is either
                // negative or zero.
                if (!isKnownNonNegative)
                {
                    isKnownNegative = (isKnownNegativeOp1 && isKnownNonNegativeOp0 && Known2.isNonZero()) ||
                                      (isKnownNegativeOp0 && isKnownNonNegativeOp1 && Known.isNonZero());
                }
            }
        }

        KnownBits Result = KnownBits::mul(Known, Known2, SelfMultiply);

        // Only make use of no-wrap flags if we failed to compute the sign bit
        // directly.  This matters if the multiplication always overflows, in
        // which case we prefer to follow the result of the direct computation,
        // though as the program is invoking undefined behaviour we can choose
        // whatever we like here.
        if (isKnownNonNegative && !Result.isNegative())
        {
            Result.makeNonNegative();
        }
        else if (isKnownNegative && !Result.isNonNegative())
        {
            Result.makeNegative();
        }
        return Result;
    }
};

#endif // _KNOWNBITS_H_
