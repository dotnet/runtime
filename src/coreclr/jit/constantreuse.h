// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#if defined(TARGET_ARM64) && defined(FEATURE_MASKED_HW_INTRINSICS)

namespace ConstantReuse
{

constexpr int PFalseReusePattern = -1;

struct ConstantMaskCandidate
{
    // The exact constant mask key. PFalse uses a sentinel pattern and the byte
    // arrangement because pfalse itself is encoded as .b.
    insOpts opt;
    int     pattern;
};

int  PTrueReusePattern(SveMaskPattern pattern);
bool TryGetSvePTrueOpt(var_types baseType, insOpts* opt);
bool TryGetConstantMaskCandidate(GenTree* node, ConstantMaskCandidate* candidate);
bool CanReuseConstantMaskCandidate(GenTree* node);
bool ReuseConstantMaskCandidates(Compiler* compiler, BasicBlock* block);

} // namespace ConstantReuse

#endif // TARGET_ARM64 && FEATURE_MASKED_HW_INTRINSICS
