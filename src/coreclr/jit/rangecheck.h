// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//  We take the following approach to range check analysis:
//
//  Consider the following loop:
//  for (int i = 0; i < a.len; ++i) {
//      a[i] = 0;
//  }
//
//  This would be represented as:
//              i_0 = 0; BB0
//               /        ______  a[i_1] = 0;     BB2
//              /        /        i_2 = i_1 + 1;
//             /        /          ^
//  i_1 = phi(i_0, i_2); BB1       |
//  i_1 < a.len -------------------+
//
//  BB0 -> BB1
//  BB1 -> (i_1 < a.len) ? BB2 : BB3
//  BB2 -> BB1
//  BB3 -> return
//
//  **Step 1. Walk the statements in the method checking if there is a bounds check.
//  If there is a bounds check, ask the range of the index variable.
//  In the above example i_1's range.
//
//  **Step 2. Follow the defs and the dependency chain:
//  i_1 is a local, so go to its definition which is i_1 = phi(i_0, i_2).
//
//  Since rhs is a phi, we ask the range for i_0 and i_2 in the hopes of merging
//  the resulting ranges for i_1.
//
//  The range of i_0 follows immediately when going to its definition.
//  Ask for the range of i_2, which leads to i_1 + 1.
//  Ask for the range of i_1 and figure we are looping. Call the range of i_1 as
//  "dependent" and quit looping further. The range of "1" is just <1, 1>.
//
//  Now we have exhausted all the variables for which the range can be determined.
//  The others are either "unknown" or "dependent."
//
//  We also merge assertions from its pred block's edges for a phi argument otherwise
//  from the block's assertionIn. This gives us an upper bound for i_1 as a.len.
//
//  **Step 3. Check if an overflow occurs in the dependency chain (loop.)
//  In the above case, we want to make sure there is no overflow in the definitions
//  involving i_1 and i_2. Merge assertions from the block's edges whenever possible.
//
//  **Step 4. Check if the dependency chain is monotonic.
//
//  **Step 5. If monotonic increasing is true, then perform a widening step, where we assume, the
//  SSA variables that are "dependent" get their lower bound values from the definitions in the
//  dependency loop and their initial values must be the definitions that are not in
//  the dependency loop, in this case i_0's value which is 0.
//

#pragma once
#include "compiler.h"

static bool IntAddOverflows(int max1, int max2)
{
    if (max1 > 0 && max2 > 0 && INT_MAX - max1 < max2)
    {
        return true;
    }
    if (max1 < 0 && max2 < 0 && max1 < INT_MIN - max2)
    {
        return true;
    }
    return false;
}

struct Limit
{
    enum LimitType
    {
        keUndef, // The limit is yet to be computed.
        keBinOpArray,
        keConstant,
        keDependent, // The limit is dependent on some other value.
        keUnknown,   // The limit could not be determined.
    };

    Limit()
        : type(keUndef)
    {
    }

    Limit(LimitType type)
        : type(type)
    {
    }

    Limit(LimitType type, int cns)
        : cns(cns)
        , vn(ValueNumStore::NoVN)
        , type(type)
    {
        assert(type == keConstant);
    }

    Limit(LimitType type, ValueNum vn, int cns)
        : cns(cns)
        , vn(vn)
        , type(type)
    {
        assert(type == keBinOpArray);
    }

    bool IsUndef() const
    {
        return type == keUndef;
    }
    bool IsDependent() const
    {
        return type == keDependent;
    }
    bool IsUnknown() const
    {
        return type == keUnknown;
    }
    bool IsConstant() const
    {
        return type == keConstant;
    }
    int GetConstant() const
    {
        return cns;
    }
    bool IsBinOpArray() const
    {
        return type == keBinOpArray;
    }
    bool AddConstant(int i)
    {
        switch (type)
        {
            case keDependent:
                return true;
            case keBinOpArray:
            case keConstant:
                if (IntAddOverflows(cns, i))
                {
                    return false;
                }
                cns += i;
                return true;
            case keUndef:
            case keUnknown:
                // For these values of 'type', conservatively return false
                break;
        }

        return false;
    }
    bool MultiplyConstant(int i)
    {
        switch (type)
        {
            case keDependent:
                return true;
            case keBinOpArray:
            case keConstant:
                if (CheckedOps::MulOverflows(cns, i, CheckedOps::Signed))
                {
                    return false;
                }
                cns *= i;
                return true;
            case keUndef:
            case keUnknown:
                // For these values of 'type', conservatively return false
                break;
        }

        return false;
    }

    bool ShiftRightConstant(int i)
    {
        switch (type)
        {
            case keDependent:
                return true;
            case keBinOpArray:
            case keConstant:
                // >> never overflows
                assert((unsigned)i <= 31);
                cns >>= i;
                return true;
            case keUndef:
            case keUnknown:
                // For these values of 'type', conservatively return false
                break;
        }

        return false;
    }

    bool Equals(const Limit& l) const
    {
        switch (type)
        {
            case keUndef:
            case keUnknown:
            case keDependent:
                return l.type == type;

            case keBinOpArray:
                return l.type == type && l.vn == vn && l.cns == cns;

            case keConstant:
                return l.type == type && l.cns == cns;
        }
        return false;
    }
#ifdef DEBUG
    const char* ToString(Compiler* comp)
    {
        switch (type)
        {
            case keUndef:
                return "Undef";

            case keUnknown:
                return "Unknown";

            case keDependent:
                return "Dependent";

            case keBinOpArray:
                return comp->printfAlloc(FMT_VN " + %d", vn, cns);

            case keConstant:
                return comp->printfAlloc("%d", cns);
        }
        unreached();
    }
#endif
    int       cns;
    ValueNum  vn;
    LimitType type;
};

// Range struct contains upper and lower limit.
struct Range
{
    Limit uLimit;
    Limit lLimit;

    Range(const Limit& limit)
        : uLimit(limit)
        , lLimit(limit)
    {
    }

    Range(const Limit& lLimit, const Limit& uLimit)
        : uLimit(uLimit)
        , lLimit(lLimit)
    {
    }

    const Limit& UpperLimit() const
    {
        return uLimit;
    }

    Limit& UpperLimit()
    {
        return uLimit;
    }

    const Limit& LowerLimit() const
    {
        return lLimit;
    }

    Limit& LowerLimit()
    {
        return lLimit;
    }

#ifdef DEBUG
    const char* ToString(Compiler* comp)
    {
        return comp->printfAlloc("<%s, %s>", lLimit.ToString(comp), uLimit.ToString(comp));
    }
#endif
};

// Helpers for operations performed on ranges
struct RangeOps
{
    // Perform 'value' + 'cns'
    static Limit AddConstantLimit(const Limit& value, const Limit& cns)
    {
        assert(cns.IsConstant());
        Limit l = value;
        if (l.AddConstant(cns.GetConstant()))
        {
            return l;
        }
        return Limit(Limit::keUnknown);
    }

    // Perform 'value' * 'cns'
    static Limit MultiplyConstantLimit(const Limit& value, const Limit& cns)
    {
        assert(cns.IsConstant());
        Limit l = value;
        if (l.MultiplyConstant(cns.GetConstant()))
        {
            return l;
        }
        return Limit(Limit::keUnknown);
    }

    // Perform 'value' >> 'cns'
    static Limit ShiftRightConstantLimit(const Limit& value, const Limit& cns)
    {
        assert(value.IsConstant());
        Limit result = value;
        if (result.ShiftRightConstant(cns.GetConstant()))
        {
            return result;
        }
        return Limit(Limit::keUnknown);
    }

    // Given two ranges "r1" and "r2", perform an add operation on the
    // ranges.
    static Range Add(Range& r1, Range& r2)
    {
        Limit& r1lo = r1.LowerLimit();
        Limit& r1hi = r1.UpperLimit();
        Limit& r2lo = r2.LowerLimit();
        Limit& r2hi = r2.UpperLimit();

        Range result = Limit(Limit::keUnknown);

        // Check lo ranges if they are dependent and not unknown.
        if ((r1lo.IsDependent() && !r1lo.IsUnknown()) || (r2lo.IsDependent() && !r2lo.IsUnknown()))
        {
            result.lLimit = Limit(Limit::keDependent);
        }
        // Check hi ranges if they are dependent and not unknown.
        if ((r1hi.IsDependent() && !r1hi.IsUnknown()) || (r2hi.IsDependent() && !r2hi.IsUnknown()))
        {
            result.uLimit = Limit(Limit::keDependent);
        }

        if (r1lo.IsConstant())
        {
            result.lLimit = AddConstantLimit(r2lo, r1lo);
        }
        if (r2lo.IsConstant())
        {
            result.lLimit = AddConstantLimit(r1lo, r2lo);
        }
        if (r1hi.IsConstant())
        {
            result.uLimit = AddConstantLimit(r2hi, r1hi);
        }
        if (r2hi.IsConstant())
        {
            result.uLimit = AddConstantLimit(r1hi, r2hi);
        }
        return result;
    }

    static Range ShiftRight(Range& r1, Range& r2)
    {
        Limit& r1lo = r1.LowerLimit();
        Limit& r1hi = r1.UpperLimit();
        Limit& r2lo = r2.LowerLimit();
        Limit& r2hi = r2.UpperLimit();

        Range result = Limit(Limit::keUnknown);

        // For now we only support r1 >> positive_cns (to simplify)
        if (!r2lo.IsConstant() || !r2hi.IsConstant() || (r2lo.cns < 0) || (r2hi.cns < 0))
        {
            return result;
        }

        // Check lo ranges if they are dependent and not unknown.
        if (r1lo.IsDependent())
        {
            result.lLimit = Limit(Limit::keDependent);
        }
        else if (r1lo.IsConstant())
        {
            result.lLimit = ShiftRightConstantLimit(r1lo, r2lo);
        }

        if (r1hi.IsDependent())
        {
            result.uLimit = Limit(Limit::keDependent);
        }
        else if (r1hi.IsConstant())
        {
            result.uLimit = ShiftRightConstantLimit(r1hi, r2hi);
        }

        return result;
    }

    // Given two ranges "r1" and "r2", perform an multiply operation on the
    // ranges.
    static Range Multiply(Range& r1, Range& r2)
    {
        Limit& r1lo = r1.LowerLimit();
        Limit& r1hi = r1.UpperLimit();
        Limit& r2lo = r2.LowerLimit();
        Limit& r2hi = r2.UpperLimit();

        Range result = Limit(Limit::keUnknown);

        // Check lo ranges if they are dependent and not unknown.
        if ((r1lo.IsDependent() && !r1lo.IsUnknown()) || (r2lo.IsDependent() && !r2lo.IsUnknown()))
        {
            result.lLimit = Limit(Limit::keDependent);
        }
        // Check hi ranges if they are dependent and not unknown.
        if ((r1hi.IsDependent() && !r1hi.IsUnknown()) || (r2hi.IsDependent() && !r2hi.IsUnknown()))
        {
            result.uLimit = Limit(Limit::keDependent);
        }

        if (r1lo.IsConstant())
        {
            result.lLimit = MultiplyConstantLimit(r2lo, r1lo);
        }
        if (r2lo.IsConstant())
        {
            result.lLimit = MultiplyConstantLimit(r1lo, r2lo);
        }
        if (r1hi.IsConstant())
        {
            result.uLimit = MultiplyConstantLimit(r2hi, r1hi);
        }
        if (r2hi.IsConstant())
        {
            result.uLimit = MultiplyConstantLimit(r1hi, r2hi);
        }
        return result;
    }

    // Given two ranges "r1" and "r2", do a Phi merge. If "monIncreasing" is true,
    // then ignore the dependent variables for the lower bound but not for the upper bound.
    static Range Merge(const Range& r1, const Range& r2, bool monIncreasing)
    {
        const Limit& r1lo = r1.LowerLimit();
        const Limit& r1hi = r1.UpperLimit();
        const Limit& r2lo = r2.LowerLimit();
        const Limit& r2hi = r2.UpperLimit();

        // Take care of lo part.
        Range result = Limit(Limit::keUnknown);
        if (r1lo.IsUnknown() || r2lo.IsUnknown())
        {
            result.lLimit = Limit(Limit::keUnknown);
        }
        // Uninitialized, just copy.
        else if (r1lo.IsUndef())
        {
            result.lLimit = r2lo;
        }
        else if (r1lo.IsDependent() || r2lo.IsDependent())
        {
            if (monIncreasing)
            {
                result.lLimit = r1lo.IsDependent() ? r2lo : r1lo;
            }
            else
            {
                result.lLimit = Limit(Limit::keDependent);
            }
        }

        // Take care of hi part.
        if (r1hi.IsUnknown() || r2hi.IsUnknown())
        {
            result.uLimit = Limit(Limit::keUnknown);
        }
        else if (r1hi.IsUndef())
        {
            result.uLimit = r2hi;
        }
        else if (r1hi.IsDependent() || r2hi.IsDependent())
        {
            result.uLimit = Limit(Limit::keDependent);
        }

        if (r1lo.IsConstant() && r2lo.IsConstant())
        {
            result.lLimit = Limit(Limit::keConstant, min(r1lo.GetConstant(), r2lo.GetConstant()));
        }
        if (r1hi.IsConstant() && r2hi.IsConstant())
        {
            result.uLimit = Limit(Limit::keConstant, max(r1hi.GetConstant(), r2hi.GetConstant()));
        }
        if (r2hi.Equals(r1hi))
        {
            result.uLimit = r2hi;
        }
        if (r2lo.Equals(r1lo))
        {
            result.lLimit = r1lo;
        }
        // Widen Upper Limit => Max(k, (a.len + n)) yields (a.len + n),
        // This is correct if k >= 0 and n >= k, since a.len always >= 0
        // (a.len + n) could overflow, but the result (a.len + n) also
        // preserves the overflow.
        if (r1hi.IsConstant() && r1hi.GetConstant() >= 0 && r2hi.IsBinOpArray() &&
            r2hi.GetConstant() >= r1hi.GetConstant())
        {
            result.uLimit = r2hi;
        }
        if (r2hi.IsConstant() && r2hi.GetConstant() >= 0 && r1hi.IsBinOpArray() &&
            r1hi.GetConstant() >= r2hi.GetConstant())
        {
            result.uLimit = r1hi;
        }
        if (r1hi.IsBinOpArray() && r2hi.IsBinOpArray() && r1hi.vn == r2hi.vn)
        {
            result.uLimit = r1hi;
            // Widen the upper bound if the other constant is greater.
            if (r2hi.GetConstant() > r1hi.GetConstant())
            {
                result.uLimit = r2hi;
            }
        }
        return result;
    }

    // Given a Range C from an op (x << C), convert it to be used as
    // (x * C'), where C' is a power of 2.
    static Range ConvertShiftToMultiply(Range& r1)
    {
        Limit& r1lo = r1.LowerLimit();
        Limit& r1hi = r1.UpperLimit();

        if (!r1lo.IsConstant() || !r1hi.IsConstant())
        {
            return Limit(Limit::keUnknown);
        }

        // Keep it simple for now, check if 0 <= C < 31
        int r1loConstant = r1lo.GetConstant();
        int r1hiConstant = r1hi.GetConstant();
        if (r1loConstant <= 0 || r1loConstant > 31 || r1hiConstant <= 0 || r1hiConstant > 31)
        {
            return Limit(Limit::keUnknown);
        }

        Range result  = Limit(Limit::keConstant);
        result.lLimit = Limit(Limit::keConstant, 1 << r1loConstant);
        result.uLimit = Limit(Limit::keConstant, 1 << r1hiConstant);
        return result;
    }

    static Range Negate(Range& range)
    {
        // Only constant ranges can be negated.
        if (!range.LowerLimit().IsConstant() || !range.UpperLimit().IsConstant())
        {
            return Limit(Limit::keUnknown);
        }

        const int hi = range.UpperLimit().GetConstant();
        const int lo = range.LowerLimit().GetConstant();

        // Give up on edge cases
        if ((hi == INT_MIN) || (lo == INT_MIN))
        {
            return Limit(Limit::keUnknown);
        }

        // Example: [0..7] => [-7..0]
        Range result  = Limit(Limit::keConstant);
        result.lLimit = Limit(Limit::keConstant, -hi);
        result.uLimit = Limit(Limit::keConstant, -lo);
        return result;
    }

    enum class RelationKind
    {
        AlwaysTrue,
        AlwaysFalse,
        Unknown
    };

    //------------------------------------------------------------------------
    // EvalRelop: Evaluate the relation between two ranges for the given relop
    //    Example: "x >= y" is AlwaysTrue when "x.LowerLimit() >= y.UpperLimit()"
    //
    // Arguments:
    //    relop      - The relational operator (LE,LT,GE,GT,EQ,NE)
    //    isUnsigned - True if the comparison is unsigned
    //    x          - The left range
    //    y          - The right range
    //
    // Returns:
    //    AlwaysTrue when the given relop always evaluates to true for the given ranges
    //    AlwaysFalse when the given relop always evaluates to false for the given ranges
    //    Otherwise Unknown
    //
    static RelationKind EvalRelop(const genTreeOps relop, bool isUnsigned, const Range& x, const Range& y)
    {
        const Limit& xLower = x.LowerLimit();
        const Limit& yLower = y.LowerLimit();
        const Limit& xUpper = x.UpperLimit();
        const Limit& yUpper = y.UpperLimit();

        // For unsigned comparisons, we only support non-negative ranges.
        if (isUnsigned)
        {
            if (!xLower.IsConstant() || !yUpper.IsConstant() || (xLower.GetConstant() < 0) ||
                (yLower.GetConstant() < 0))
            {
                return RelationKind::Unknown;
            }
        }

        switch (relop)
        {
            case GT_GE:
            case GT_LT:
                if (xLower.IsConstant() && yUpper.IsConstant() && (xLower.GetConstant() >= yUpper.GetConstant()))
                {
                    return relop == GT_GE ? RelationKind::AlwaysTrue : RelationKind::AlwaysFalse;
                }

                if (xUpper.IsConstant() && yLower.IsConstant() && (xUpper.GetConstant() < yLower.GetConstant()))
                {
                    return relop == GT_GE ? RelationKind::AlwaysFalse : RelationKind::AlwaysTrue;
                }
                break;

            case GT_GT:
            case GT_LE:
                if (xLower.IsConstant() && yUpper.IsConstant() && (xLower.GetConstant() > yUpper.GetConstant()))
                {
                    return relop == GT_GT ? RelationKind::AlwaysTrue : RelationKind::AlwaysFalse;
                }

                if (xUpper.IsConstant() && yLower.IsConstant() && (xUpper.GetConstant() <= yLower.GetConstant()))
                {
                    return relop == GT_GT ? RelationKind::AlwaysFalse : RelationKind::AlwaysTrue;
                }
                break;

            case GT_EQ:
            case GT_NE:
                if ((xLower.IsConstant() && yUpper.IsConstant() && (xLower.GetConstant() > yUpper.GetConstant())) ||
                    (xUpper.IsConstant() && yLower.IsConstant() && (xUpper.GetConstant() < yLower.GetConstant())))
                {
                    return relop == GT_EQ ? RelationKind::AlwaysFalse : RelationKind::AlwaysTrue;
                }
                break;

            default:
                assert(!"unknown comparison operator");
                break;
        }
        return RelationKind::Unknown;
    }
};

class RangeCheck
{
public:
    // Constructor
    RangeCheck(Compiler* pCompiler);

    // Entry point to optimize range checks in the method. Assumes value numbering
    // and assertion prop phases are completed.
    bool OptimizeRangeChecks();

    bool TryGetRange(BasicBlock* block, GenTree* expr, Range* pRange);

    // Cheaper version of TryGetRange that is based only on incoming assertions.
    static bool TryGetRangeFromAssertions(Compiler* comp, ValueNum num, ASSERT_VALARG_TP assertions, Range* pRange);

private:
    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, bool>        OverflowMap;
    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, Range*>      RangeMap;
    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, BasicBlock*> SearchPath;

    int GetArrLength(ValueNum vn);

    // Check whether the computed range is within 0 and upper bounds. This function
    // assumes that the lower range is resolved and upper range is symbolic as in an
    // increasing loop.
    // TODO-CQ: This is not general enough.
    bool BetweenBounds(Range& range, GenTree* upper, int arrSize);

    // Given a "tree" node, check if it contains array bounds check node and
    // optimize to remove it, if possible. Requires "stmt" and "block" that
    // contain the tree.
    void OptimizeRangeCheck(BasicBlock* block, Statement* stmt, GenTree* tree);

    // Internal worker for GetRange.
    Range GetRangeWorker(BasicBlock* block, GenTree* expr, bool monIncreasing DEBUGARG(int indent));

    // Compute the range from the given type
    Range GetRangeFromType(var_types type);

    // Given the local variable, first find the definition of the local and find the range of the rhs.
    // Helper for GetRangeWorker.
    Range ComputeRangeForLocalDef(BasicBlock* block, GenTreeLclVarCommon* lcl, bool monIncreasing DEBUGARG(int indent));

    // Compute the range, rather than retrieve a cached value. Helper for GetRangeWorker.
    Range ComputeRange(BasicBlock* block, GenTree* expr, bool monIncreasing DEBUGARG(int indent));

    // Compute the range for the op1 and op2 for the given binary operator.
    Range ComputeRangeForBinOp(BasicBlock* block, GenTreeOp* binop, bool monIncreasing DEBUGARG(int indent));

    // Merge assertions from AssertionProp's flags, for the corresponding "phiArg."
    // Requires "pRange" to contain range that is computed partially.
    void MergeAssertion(BasicBlock* block, GenTree* phiArg, Range* pRange DEBUGARG(int indent));

    // Inspect the "assertions" and extract assertions about the given "phiArg" and
    // refine the "pRange" value.
    void MergeEdgeAssertions(GenTreeLclVarCommon* lcl, ASSERT_VALARG_TP assertions, Range* pRange);

    // Inspect the assertions about the current ValueNum to refine pRange
    static void MergeEdgeAssertions(
        Compiler* comp, ValueNum num, ValueNum preferredBoundVN, ASSERT_VALARG_TP assertions, Range* pRange);

    // The maximum possible value of the given "limit". If such a value could not be determined
    // return "false". For example: CORINFO_Array_MaxLength for array length.
    bool GetLimitMax(Limit& limit, int* pMax);

    // Does the addition of the two limits overflow?
    bool AddOverflows(Limit& limit1, Limit& limit2);

    // Does the multiplication of the two limits overflow?
    bool MultiplyOverflows(Limit& limit1, Limit& limit2);

    // Does the binary operation between the operands overflow? Check recursively.
    bool DoesBinOpOverflow(BasicBlock* block, GenTreeOp* binop, const Range& range);

    // Do the phi operands involve a definition that could overflow?
    bool DoesPhiOverflow(BasicBlock* block, GenTree* expr, const Range& range);

    // Find the def of the "expr" local and recurse on the arguments if any of them involve a
    // calculation that overflows.
    bool DoesVarDefOverflow(BasicBlock* block, GenTreeLclVarCommon* lcl, const Range& range);

    // Does the current "expr", which is a use, involve a definition that overflows.
    bool DoesOverflow(BasicBlock* block, GenTree* tree, const Range& range);

    bool ComputeDoesOverflow(BasicBlock* block, GenTree* expr, const Range& range);

    // Widen the range by first checking if the induction variable is monotonically increasing.
    // Requires "pRange" to be partially computed.
    void Widen(BasicBlock* block, GenTree* tree, Range* pRange);

    // Is the binary operation increasing the value.
    bool IsBinOpMonotonicallyIncreasing(GenTreeOp* binop);

    // Given an expression trace its value to check if it is monotonically increasing.
    bool IsMonotonicallyIncreasing(GenTree* tree, bool rejectNegativeConst);

    // We allocate a budget to avoid walking long UD chains. When traversing each link in the UD
    // chain, we decrement the budget. When the budget hits 0, then no more range check optimization
    // will be applied for the currently compiled method.
    bool IsOverBudget();

    // Given a lclvar use, try to find the lclvar's defining store and its containing block.
    LclSsaVarDsc* GetSsaDefStore(GenTreeLclVarCommon* lclUse);

    // When we have this bound and a constant, we prefer to use this bound (if set)
    ValueNum m_preferredBound;

    // Get the cached overflow values.
    OverflowMap* GetOverflowMap();
    void         ClearOverflowMap();
    OverflowMap* m_pOverflowMap;

    // Get the cached range values.
    RangeMap* GetRangeMap();
    void      ClearRangeMap();
    RangeMap* m_pRangeMap;

    SearchPath* GetSearchPath();
    void        ClearSearchPath();
    SearchPath* m_pSearchPath;

    Compiler*     m_pCompiler;
    CompAllocator m_alloc;

    // The number of nodes for which range is computed throughout the current method.
    // When this limit is zero, we have exhausted all the budget to walk the ud-chain.
    int m_nVisitBudget;

    // Set to "true" whenever we remove a check and need to re-thread the statement.
    bool m_updateStmt;
};
