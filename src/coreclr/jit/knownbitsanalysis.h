// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "knownbits.h"

//------------------------------------------------------------------------
// KnownBitsAnalysis: Computes the known bits of a value number: the bits that are known to be
//    zero or one given the shape of the value number, the PHIs and a set of assertions.
//    It complements RangeCheck::GetRangeFromAssertions: ranges describe the magnitude of a value
//    while known bits describe its bit pattern (alignment, masks, flags), and they work for
//    64-bit values too.
//
//    The KnownBits type and its transfer functions are LLVM's, see knownbits.h.
//
// Notes:
//    Besides the known bits, the analysis tracks the signed and unsigned bounds of a value:
//    "x < 10" doesn't tell us anything about the bits of "x" unless "x" is known to be
//    non-negative as well, which we may learn from another assertion.
//
//    The analysis relies on the assertions and their data flow, so it can only be used
//    during the global assertion propagation.
//
class KnownBitsAnalysis
{
    // The number of non-constant value numbers a query is allowed to look at.
    static const int Budget = 16;
    // How deep the PHIs of PHIs are followed.
    static const int MaxPhiDepth = 3;

    Compiler*      m_compiler;
    ValueNumStore* m_vnStore;
    int            m_budget;
    int            m_phiDepth;
    int            m_maxPhiDepth;
    bool           m_followVNAssertions; // whether to use "vn relop otherVN" assertions
    bool           m_followIntPhis;      // whether to look through 32-bit PHIs

    // Unsigned and signed bounds of a value.
    struct Bounds
    {
        APInt UMin;
        APInt UMax;
        APInt SMin;
        APInt SMax;

        Bounds(const KnownBits& known);

        bool IsUnknown() const;
        void ApplyRelop(Compiler::optAssertionKind kind, const Bounds& other);
        void Normalize();
    };

    static unsigned GetWidth(var_types type);

    KnownBits Compute(ValueNum vn, unsigned width, ASSERT_VALARG_TP assertions, Bounds* pBounds = nullptr);
    KnownBits ComputeForPhi(ValueNum vn, unsigned width);
    KnownBits ComputeForFunc(const VNFuncApp& funcApp, unsigned width, ASSERT_VALARG_TP assertions);
    void MergeAssertions(ValueNum vn, unsigned width, ASSERT_VALARG_TP assertions, KnownBits* pKnown, Bounds* pBounds);

public:
    KnownBitsAnalysis(Compiler* comp, int phiDepth = 0);

    RelopEvaluationResult EvalRelop(GenTreeOp* relop, ASSERT_VALARG_TP assertions);
};
