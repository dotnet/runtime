// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#if defined(TARGET_ARM64)

struct LclMasksWeight
{
    // For the given variable, the cost of storing as vector.
    weight_t currentCost = 0.0;

    // For the given variable, the cost of storing as mask.
    weight_t switchCost = 0.0;

    // The weighting is invalid.
    bool invalid = false;

    // Conversion of mask to vector is one instruction.
    static constexpr const weight_t costOfConvertMaskToVector = 1.0;

    // Conversion of vector to mask is two instructions.
    static constexpr const weight_t costOfConvertVectorToMask = 2.0;

    // The simd types of the Lcl Store after conversion to vector.
    CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
    unsigned    simdSize        = 0;

    void UpdateWeight(bool isStore, bool hasConvert, weight_t blockWeight);

    void InvalidateWeight()
    {
        JITDUMP("Invalidating weight. \n");
        invalid = true;
        DumpTotalWeight();
    }

    void DumpTotalWeight()
    {
        JITDUMP("Weighting: %s{%.2fc %.2fs}\n", invalid ? "Invalid" : "", currentCost, switchCost);
    }

    void CacheSimdTypes(GenTreeHWIntrinsic* op);
};

typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, LclMasksWeight> LclMasksWeightTable;

//-----------------------------------------------------------------------------
// UpdateWeight: Updates the weighting to take account of a local.
//
// Arguments:
//     isStore - Is this a lcl store
//     hasConvert - Is this local converted
//     blockWeight - Weight of the block the store is contained in
//
void LclMasksWeight::UpdateWeight(bool isStore, bool hasConvert, weight_t blockWeight)
{
    if (hasConvert)
    {
        // Count the cost of the existing convert.
        weight_t cost = isStore ? costOfConvertMaskToVector : costOfConvertVectorToMask;
        cost *= blockWeight;

        JITDUMP("Incrementing currentCost by %.2f. ", cost);
        currentCost += cost;
    }
    else
    {
        // Switching would require adding a convert.
        weight_t cost = isStore ? costOfConvertVectorToMask : costOfConvertMaskToVector;
        cost *= blockWeight;

        JITDUMP("Incrementing switchCost by %.2f. ", cost);
        switchCost += cost;
    }
    DumpTotalWeight();
}

//-----------------------------------------------------------------------------
// CacheSimdTypes: Cache the simd types of a hwintrinsic
//
// Arguments:
//     op - The HW intrinsic to cache
//
void LclMasksWeight::CacheSimdTypes(GenTreeHWIntrinsic* op)
{
    CorInfoType newSimdBaseJitType = op->GetSimdBaseJitType();
    unsigned    newSimdSize        = op->GetSimdSize();

    assert((newSimdBaseJitType != CORINFO_TYPE_UNDEF));

    simdBaseJitType = newSimdBaseJitType;
    simdSize        = newSimdSize;
}

//-----------------------------------------------------------------------------
// LclMasksCheckVisitor: Find all lcl var definitions and uses. For each one, update the weighting.
//
class LclMasksCheckVisitor final : public GenTreeVisitor<LclMasksCheckVisitor>
{
public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true
    };

    LclMasksCheckVisitor(Compiler* compiler, weight_t bbWeight, LclMasksWeightTable* weightsTable)
        : GenTreeVisitor<LclMasksCheckVisitor>(compiler)
        , bbWeight(bbWeight)
        , weightsTable(weightsTable)
    {
    }

    Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTreeHWIntrinsic* convertOp = nullptr;

        bool isLocalStore  = false;
        bool isLocalUse    = false;
        bool isInvalid     = false;
        bool hasConversion = false;

        switch ((*use)->OperGet())
        {
            case GT_STORE_LCL_VAR:
                isLocalStore = true;

                // Look for:
                //      use:STORE_LCL_VAR(ConvertMaskToVector(x))

                if ((*use)->AsLclVar()->Data()->OperIsConvertMaskToVector())
                {
                    convertOp     = (*use)->AsLclVar()->Data()->AsHWIntrinsic();
                    hasConversion = true;
                }
                break;

            case GT_LCL_VAR:
                isLocalUse = true;

                // Look for:
                //      user:ConvertVectorToMask(use:LCL_VAR(x)))

                if (user->OperIsConvertVectorToMask())
                {
                    convertOp     = user->AsHWIntrinsic();
                    hasConversion = true;
                }
                break;

            case GT_LCL_ADDR:
                isInvalid = true;
                break;

            default:
                break;
        }

        if (isLocalStore || isLocalUse || isInvalid)
        {
            GenTreeLclVarCommon* lclOp = (*use)->AsLclVarCommon();

            // Get the existing weighting (if any).
            LclMasksWeight  defaultWeight;
            LclMasksWeight* weight = weightsTable->LookupPointerOrAdd(lclOp->GetLclNum(), defaultWeight);

            // Update the weights.
            JITDUMP("Local %s V%02d at [%06u] ", isLocalStore ? "store" : "var", lclOp->GetLclNum(),
                    m_compiler->dspTreeID(lclOp));
            if (isInvalid)
            {
                JITDUMP("cannot be converted. ");
                weight->InvalidateWeight();
            }
            else
            {
                JITDUMP("has %s conversion. ", hasConversion ? "mask" : "no");
                weight->UpdateWeight(isLocalStore, hasConversion, bbWeight);
            }

            // Cache the simd type data of the conversion.
            if (hasConversion)
            {
                assert(convertOp != nullptr);
                weight->CacheSimdTypes(convertOp);
            }

            foundConversions |= hasConversion;
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    bool foundConversions = false;

private:
    weight_t             bbWeight;
    LclMasksWeightTable* weightsTable;
};

//-----------------------------------------------------------------------------
// LclMasksUpdateVisitor: tree visitor to remove conversion to masks for uses of LCL
//
class LclMasksUpdateVisitor final : public GenTreeVisitor<LclMasksUpdateVisitor>
{
public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true
    };

    LclMasksUpdateVisitor(Compiler* compiler, Statement* stmt, LclMasksWeightTable* weightsTable)
        : GenTreeVisitor<LclMasksUpdateVisitor>(compiler)
        , stmt(stmt)
        , weightsTable(weightsTable)
    {
    }

    Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTreeLclVarCommon* lclOp            = nullptr;
        bool                 isLocalStore     = false;
        bool                 isLocalUse       = false;
        bool                 addConversion    = false;
        bool                 removeConversion = false;

        if ((*use)->OperIs(GT_STORE_LCL_VAR) && (*use)->AsLclVarCommon()->Data()->OperIsConvertMaskToVector())
        {
            // Found
            //      use:STORE_LCL_VAR(ConvertMaskToVector(x))
            lclOp            = (*use)->AsLclVarCommon();
            isLocalStore     = true;
            removeConversion = true;
        }
        else if ((*use)->OperIs(GT_STORE_LCL_VAR) && !(*use)->AsLclVarCommon()->Data()->OperIsConvertMaskToVector())
        {
            // Found
            //      use:STORE_LCL_VAR(x)
            lclOp         = (*use)->AsLclVarCommon();
            isLocalStore  = true;
            addConversion = true;
        }
        else if ((*use)->OperIsConvertVectorToMask() && (*use)->AsHWIntrinsic()->Op(2)->OperIs(GT_LCL_VAR))
        {
            // Found
            //      user(use:ConvertVectorToMask(LCL_VAR(x)))
            lclOp            = (*use)->AsHWIntrinsic()->Op(2)->AsLclVarCommon();
            isLocalUse       = true;
            removeConversion = true;
        }
        else if ((*use)->OperIs(GT_LCL_VAR) && ((user == nullptr) || !user->OperIsConvertVectorToMask()))
        {
            // Found
            //      user(use:LCL_VAR(x))
            lclOp         = (*use)->AsLclVar();
            isLocalUse    = true;
            addConversion = true;
        }
        else
        {
            // Found something else
            return fgWalkResult::WALK_CONTINUE;
        }

        assert(isLocalStore != isLocalUse);
        assert(addConversion != removeConversion);
        assert(lclOp != nullptr);

        // Get the existing weighting.
        LclMasksWeight weight;
        bool           found = weightsTable->Lookup(lclOp->GetLclNum(), &weight);
        assert(found);

        // Quit if the cost of changing is higher or is invalid.
        if (weight.currentCost <= weight.switchCost || weight.invalid)
        {
            JITDUMP("Local %s V%02d at [%06u] will not be converted. ", isLocalStore ? "store" : "var",
                    lclOp->GetLclNum(), Compiler::dspTreeID(lclOp));
            weight.DumpTotalWeight();
            return fgWalkResult::WALK_CONTINUE;
        }

        JITDUMP("Local %s V%02d at [%06u] will be converted. ", isLocalStore ? "store" : "var", lclOp->GetLclNum(),
                Compiler::dspTreeID(lclOp));
        weight.DumpTotalWeight();

        // Fix up the type of the lcl and the lclvar.
        assert(lclOp->gtType != TYP_MASK);
        var_types lclOrigType = lclOp->gtType;
        lclOp->gtType         = TYP_MASK;
        LclVarDsc* varDsc     = m_compiler->lvaGetDesc(lclOp->GetLclNum());
        varDsc->lvType        = TYP_MASK;

        // Add or remove a conversion

        if (isLocalStore && removeConversion)
        {
            // Convert
            //      use:STORE_LCL_VAR(ConvertMaskToVector(x))
            // to
            //      use:STORE_LCL_VAR(x)

            lclOp->Data() = lclOp->Data()->AsHWIntrinsic()->Op(1);
        }

        else if (isLocalStore && addConversion)
        {
            // Convert
            //      use:STORE_LCL_VAR(x)
            // to
            //      use:STORE_LCL_VAR(ConvertVectorToMask(x))

            // There is not enough information in the lcl to get simd types. Instead reuse the cached
            // simd types from the removed convert nodes.
            assert(weight.simdBaseJitType != CORINFO_TYPE_UNDEF);
            lclOp->Data() = m_compiler->gtNewSimdCvtVectorToMaskNode(TYP_MASK, lclOp->Data(), weight.simdBaseJitType,
                                                                     weight.simdSize);
        }

        else if (isLocalUse && removeConversion)
        {
            // Convert
            //      user(use:ConvertVectorToMask(LCL_VAR(x)))
            // to
            //      user(use:LCL_VAR(x))

            *use = lclOp;
        }

        else if (isLocalUse && addConversion)
        {
            // Convert
            //      user(use:LCL_VAR(x))
            // to
            //      user(ConvertMaskToVector(use:LCL_VAR(x)))

            // There is not enough information in the lcl to get simd types. Instead reuse the cached simd
            // types from the removed convert nodes.
            assert(weight.simdBaseJitType != CORINFO_TYPE_UNDEF);
            *use =
                m_compiler->gtNewSimdCvtMaskToVectorNode(lclOrigType, lclOp, weight.simdBaseJitType, weight.simdSize);
        }

        JITDUMP("Updated %s V%02d at [%06u] to mask (%s conversion)\n", isLocalStore ? "store" : "var",
                lclOp->GetLclNum(), m_compiler->dspTreeID(lclOp), addConversion ? "added" : "removed");
        DISPTREE(*use);

        updatedConversions = true;
        return fgWalkResult::WALK_CONTINUE;
    }

public:
    bool updatedConversions = false;

private:
    Statement*           stmt;
    LclMasksWeightTable* weightsTable;
};

#endif // TARGET_ARM64

//------------------------------------------------------------------------
// optLclMasks: Allow locals to be of Mask type
//
// At the C# level, Masks share the same type as a Vector. It's possible for the same
// variable to be used as a mask or vector. Any APIs that return a mask must first convert
// the value to a vector before storing it to a variable. Any uses of a variable as a mask
// must first convert from vector before using it. In many cases this creates unnecessary
// conversions. For variables that live outside the scope of the current method then the
// conversions are required to ensure correctness. However, for local variables where the
// scope is local to the current method, then it is possible to keep the value as a mask,
// by updating all definitions and uses.
//
// In the common case it is expected that uses of masks are consistent - once a variable is
// created as a mask it will continue to be used and updated as a mask.
//
// In the uncommon case, a variable may be created in one type, used as another and/or
// updated to a different type.
//
// For example (the conversion is implicit)
//   vector<int> x = _ConvertMaskToVector_(CreateMask());
//   x = Add(x, y);
//
// To optimize this, the pass searches every local variable definition (GT_STORE_LCL_VAR)
// and use (GT_LCL_VAR). A weighting is calculated and kept in a hash table - one entry
// for each lclvar number. The weighting contains two values. The first value is the count of
// of every convert node for the var, each instance multiplied by the number of instructions
// in the convert and the weighting of the block it exists in. The second value assumes the
// local var has been switched to store as a mask and performs the same count. The switch
// will count removes every existing convert and add a convert where there isn't currently
// a convert.
//
// Once every definition and use has been parsed, the parsing runs again. At each step,
// if the weighting for switching that var is lower than the current weighting then switch
// to store as mask and add/remove conversions as required.
//
// Limitations:
//
// Local variables that are defined then immediately used just once may not be saved to a
// store. Here a convert to to vector will be used by a convert to mask. These instances will
// be caught in the lowering phase.
//
// This weighting does not account for re-definition. A variable may first be created as a
// mask used as such, then later in the method defined as a vector and used as such from
// then on. This can be worked around at the user level by encouraging users not to reuse
// variable names.
//
// Returns:
//    Suitable phase status
//
PhaseStatus Compiler::fgOptimizeLclMasks()
{
#if defined(TARGET_ARM64)

    if (opts.OptimizationDisabled())
    {
        JITDUMP("Skipping. Optimizations Disabled\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

#if defined(DEBUG)
    if (JitConfig.JitDoOptimizeLclMasks() == 0)
    {
        JITDUMP("Skipping. Disable by config option\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    if (!compMaskConvertUsed)
    {
        JITDUMP("Skipping. There are no converts of locals\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    LclMasksWeightTable weightsTable = LclMasksWeightTable(getAllocator());

    // Find every local and add them to weightsTable.
    bool foundConversion = false;
    JITDUMP("\n");
    for (BasicBlock* block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            // Only check statements where there is a local of type TYP_SIMD16/TYP_MASK.
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                if (lcl->gtType == TYP_SIMD16 || lcl->gtType == TYP_MASK)
                {
                    // Parse the entire statement.
                    LclMasksCheckVisitor ev(this, block->getBBWeight(this), &weightsTable);
                    GenTree*             root = stmt->GetRootNode();
                    ev.WalkTree(&root, nullptr);
                    foundConversion |= ev.foundConversions;
                    break;
                }
            }
        }
    }

    if (!foundConversion)
    {
        JITDUMP("Done. No conversions of locals found.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // For each Local, potentially add/remove a conversion.
    JITDUMP("\n");
    for (BasicBlock* block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            // Only check statements where there is a local of type TYP_SIMD16/TYP_MASK.
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                if (lcl->gtType == TYP_SIMD16 || lcl->gtType == TYP_MASK)
                {
                    // Parse the entire statement.
                    LclMasksUpdateVisitor ev(this, stmt, &weightsTable);
                    GenTree*              root = stmt->GetRootNode();
                    ev.WalkTree(&root, nullptr);
                    if (ev.updatedConversions)
                    {
                        fgSequenceLocals(stmt);
                    }
                    break;
                }
            }
        }
    }

    return PhaseStatus::MODIFIED_EVERYTHING;

#else
    return PhaseStatus::MODIFIED_NOTHING;
#endif // TARGET_ARM64
}
