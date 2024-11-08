// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#if defined(TARGET_ARM64)

//-----------------------------------------------------------------------------
// UpdateWeight: Updates the weighting to take account of a local.
//
// Arguments:
//     isStore - Is this a lcl store
//     hasConvert - Is this local converted
//     blockWeight - Weight of the block the store is contained in
//
void Compiler::LclMasksWeight::UpdateWeight(bool isStore, bool hasConvert, weight_t blockWeight)
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
void Compiler::LclMasksWeight::CacheSimdTypes(GenTreeHWIntrinsic* op)
{
    CorInfoType newSimdBaseJitType = op->GetSimdBaseJitType();
    unsigned    newSimdSize        = op->GetSimdSize();

    assert((newSimdBaseJitType != CORINFO_TYPE_UNDEF));

    simdBaseJitType = newSimdBaseJitType;
    simdSize        = newSimdSize;
}

//-----------------------------------------------------------------------------
// LclMasksCheckLclVisitor: Find the user of a lcl var and check if it is a convert to mask
//
class LclMasksCheckLclVisitor final : public GenTreeVisitor<LclMasksCheckLclVisitor>
{
public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true
    };

    LclMasksCheckLclVisitor(Compiler* compiler, GenTreeLclVarCommon* lclOp)
        : GenTreeVisitor<LclMasksCheckLclVisitor>(compiler)
        , lclOp(lclOp)
    {
    }

    Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        if ((*use) == lclOp)
        {
            switch (lclOp->OperGet())
            {
                case GT_STORE_LCL_VAR:
                    // Look for:
                    //      STORE_LCL_VAR(ConvertMaskToVector(x))

                    if (lclOp->Data()->OperIsConvertMaskToVector())
                    {
                        convertOp = lclOp->Data()->AsHWIntrinsic();
                    }
                    break;

                case GT_LCL_VAR:
                    // Look for:
                    //      ConvertVectorToMask(LCL_VAR(x)))

                    if (user->OperIsConvertVectorToMask())
                    {
                        convertOp = user->AsHWIntrinsic();
                    }
                    break;

                default:
                    break;
            }
            return fgWalkResult::WALK_ABORT;
        }
        return fgWalkResult::WALK_CONTINUE;
    }

    GenTreeHWIntrinsic* convertOp = nullptr;

private:
    GenTreeLclVarCommon* lclOp;
};

//-----------------------------------------------------------------------------
// fgLclMasksCheckLcl: For the given lcl var, update the weights in the table.
//
// Arguments:
//     lclVar - The local variable.
//     stmt - The statement the local variable is contained in.
//     block - The block the local variable is contained in.
//     weightsTable - table to update.
//
// Returns:
//     True if a converted local store was found.
//
bool Compiler::fgLclMasksCheckLcl(GenTreeLclVarCommon* lclOp,
                                  Statement* const     stmt,
                                  BasicBlock* const    block,
                                  LclMasksWeightTable* weightsTable)
{
    // Only these can have conversions.
    if (!lclOp->OperIs(GT_STORE_LCL_VAR) && !lclOp->OperIs(GT_LCL_VAR))
    {
        return false;
    }

    bool isStore = lclOp->OperIs(GT_STORE_LCL_VAR);

    // Get the existing weighting (if any).
    LclMasksWeight weight;
    weightsTable->Lookup(lclOp->GetLclNum(), &weight);

    // Find the parent of the lcl var.
    LclMasksCheckLclVisitor ev(this, lclOp);
    GenTree*                root = stmt->GetRootNode();
    ev.WalkTree(&root, nullptr);
    bool foundConversion = (ev.convertOp != nullptr);

    // Update the weights.
    JITDUMP("Local %s V%02d at [%06u] has %s conversion. ", isStore ? "store" : "var", lclOp->GetLclNum(),
            dspTreeID(lclOp), foundConversion ? "mask" : "no");
    weight.UpdateWeight(isStore, foundConversion, block->getBBWeight(this));

    // Cache the simd type data of the conversion.
    if (foundConversion)
    {
        assert(ev.convertOp != nullptr);
        weight.CacheSimdTypes(ev.convertOp);
    }

    // Update the table.
    weightsTable->Set(lclOp->GetLclNum(), weight, LclMasksWeightTable::Overwrite);

    return foundConversion;
}

//-----------------------------------------------------------------------------
// LclMasksUpdateLclVisitor: tree visitor to remove conversion to masks for uses of LCL
//
class LclMasksUpdateLclVisitor final : public GenTreeVisitor<LclMasksUpdateLclVisitor>
{
public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true
    };

    LclMasksUpdateLclVisitor(
        Compiler* compiler, GenTreeLclVarCommon* lclOp, Statement* stmt, CorInfoType simdBaseJitType, unsigned simdSize)
        : GenTreeVisitor<LclMasksUpdateLclVisitor>(compiler)
        , lclOp(lclOp)
        , stmt(stmt)
        , simdBaseJitType(simdBaseJitType)
        , simdSize(simdSize)
    {
    }

    Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        switch (lclOp->OperGet())
        {
            case GT_STORE_LCL_VAR:
                if ((*use) == lclOp)
                {
                    // Either Convert
                    //      use:STORE_LCL_VAR(ConvertMaskToVector(x))
                    // to
                    //      use:STORE_LCL_VAR(x)
                    //
                    // Or, convert
                    //      use:STORE_LCL_VAR(x)
                    // to
                    //      use:STORE_LCL_VAR(ConvertVectorToMask(x))

                    // Update the type of the STORELCL - including the lclvar.
                    assert(lclOp->gtType != TYP_MASK);
                    lclOp->gtType     = TYP_MASK;
                    LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclOp->GetLclNum());
                    varDsc->lvType = TYP_MASK;

                    if (lclOp->Data()->OperIsConvertMaskToVector())
                    {
                        // Remove the ConvertMaskToVector

                        GenTreeHWIntrinsic* convertOp = lclOp->Data()->AsHWIntrinsic();
                        GenTree*            maskOp    = convertOp->Op(1);

                        convertOp->gtBashToNOP();
                        lclOp->gtOp1 = maskOp;
                        m_compiler->fgSequenceLocals(stmt);
                    }
                    else
                    {
                        // Convert the input of the store to a mask.
                        // There is not enough information in the lcl to get simd types. Instead we reuse the cached
                        // simd types from the removed convert nodes.
                        assert(simdBaseJitType != CORINFO_TYPE_UNDEF);
                        GenTree* convertOp = m_compiler->gtNewSimdCvtVectorToMaskNode(TYP_MASK, lclOp->Data(),
                                                                                      simdBaseJitType, simdSize);
                        lclOp->Data()      = convertOp;

                        addedConversion = true;
                    }

                    found      = true;
                    modifiedOp = *use;
                }
                break;

            case GT_LCL_VAR:
                if ((*use)->OperIsConvertVectorToMask() && (*use)->AsHWIntrinsic()->Op(2) == lclOp)
                {
                    // Convert
                    //      user(use:ConvertVectorToMask(LCL_VAR(x)))
                    // to
                    //      user(use:LCL_VAR(x))

                    GenTree* const convertOp = *use;

                    // Find the location of convertOp in the user
                    int opNum = 1;
                    for (; opNum <= user->AsHWIntrinsic()->GetOperandCount(); opNum++)
                    {
                        if (user->AsHWIntrinsic()->Op(opNum) == convertOp)
                        {
                            break;
                        }
                    }
                    assert(opNum <= user->AsHWIntrinsic()->GetOperandCount());

                    // Fix up the type of the lcl
                    assert(lclOp->gtType != TYP_MASK);
                    lclOp->gtType = convertOp->gtType;

                    // Remove the convert convertOp
                    convertOp->gtBashToNOP();
                    *use = lclOp;
                    m_compiler->fgSequenceLocals(stmt);

                    found      = true;
                    modifiedOp = *use;
                }
                else if (((*use) == lclOp) && (!user->OperIsConvertVectorToMask()))
                {
                    // Convert
                    //      user(use:LCL_VAR(x))
                    // to
                    //      user(ConvertMaskToVector(use:LCL_VAR(x)))

                    GenTreeLclVar* lclOp = (*use)->AsLclVar();

                    // Fix up the type of the lcl
                    assert(lclOp->gtType != TYP_MASK);
                    var_types vectorType = lclOp->gtType;
                    lclOp->gtType        = TYP_MASK;

                    // Create a convert to mask node and insert it infront of the lcl.
                    // There is not enough information in the lcl to get simd types. Instead we reuse the cached simd
                    // types from the removed convert nodes.
                    assert(simdBaseJitType != CORINFO_TYPE_UNDEF);
                    *use = m_compiler->gtNewSimdCvtMaskToVectorNode(vectorType, lclOp, simdBaseJitType, simdSize);

                    addedConversion = true;
                    found           = true;
                    modifiedOp      = *use;
                }
                break;

            default:
                break;
        }

        return found ? fgWalkResult::WALK_ABORT : fgWalkResult::WALK_CONTINUE;
    }

public:
    bool     addedConversion = false;
    bool     found           = false;
    GenTree* modifiedOp      = nullptr;

private:
    GenTreeLclVarCommon* lclOp;
    Statement*           stmt;
    CorInfoType          simdBaseJitType;
    unsigned             simdSize;
};

//-----------------------------------------------------------------------------
// fgLclMasksUpdateLcl: For the given lcl, if the weighting recommends to switch, then update.
//
// Arguments:
//     lclOp - The local variable.
//     stmt - The statement the local variable is contained in.
//     weightsTable - table to update.
//
void Compiler::fgLclMasksUpdateLcl(GenTreeLclVarCommon* lclOp, Statement* const stmt, LclMasksWeightTable* weightsTable)
{

    // Only these can have conversions.
    if (!lclOp->OperIs(GT_STORE_LCL_VAR) && !lclOp->OperIs(GT_LCL_VAR))
    {
        return;
    }

    bool isStore = lclOp->OperIs(GT_STORE_LCL_VAR);

    // Get the existing weighting (if any).
    LclMasksWeight weight;
    bool           found = weightsTable->Lookup(lclOp->GetLclNum(), &weight);
    assert(found);

    if (!weight.ShouldSwitch())
    {
        JITDUMP("Local %s V%02d at [%06u] will not be converted. ", isStore ? "store" : "var", lclOp->GetLclNum(),
                dspTreeID(lclOp));
        weight.DumpTotalWeight();
        return;
    }

    JITDUMP("Local %s V%02d at [%06u] will be converted. ", isStore ? "store" : "var", lclOp->GetLclNum(),
            dspTreeID(lclOp));
    weight.DumpTotalWeight();

    // Remove or add a mask conversion.
    LclMasksUpdateLclVisitor ev(this, lclOp, stmt, weight.simdBaseJitType, weight.simdSize);
    GenTree*                 root = stmt->GetRootNode();
    ev.WalkTree(&root, nullptr);

    if (ev.found)
    {
        JITDUMP("Updated %s V%02d at [%06u] to mask (%s conversion)\n", isStore ? "store" : "var", lclOp->GetLclNum(),
                dspTreeID(lclOp), ev.addedConversion ? "added" : "removed");

#ifdef DEBUG
        if (verbose)
        {
            gtDispTree(ev.modifiedOp);
        }
#endif
    }
}

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

    if (!compMaskConvertUsed)
    {
        JITDUMP("Skipping. There are no converts of locals \n");
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
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                foundConversion |= fgLclMasksCheckLcl(lcl, stmt, block, &weightsTable);
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
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                fgLclMasksUpdateLcl(lcl, stmt, &weightsTable);
            }
        }
    }

    return PhaseStatus::MODIFIED_EVERYTHING;

#else
    return PhaseStatus::MODIFIED_NOTHING;
#endif // TARGET_ARM64
}
