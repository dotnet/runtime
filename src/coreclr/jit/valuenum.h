// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Defines the class "ValueNumStore", which maintains value numbers for a compilation.

// Recall that "value numbering" assigns an integer value number to each expression.  The "value
// number property" is that two expressions with the same value number will evaluate to the same value
// at runtime.  Expressions with different value numbers may or may not be equivalent.  This property
// of value numbers has obvious applications in redundancy-elimination optimizations.
//
// Since value numbers give us a way of talking about the (immutable) values to which expressions
// evaluate, they provide a good "handle" to use for attributing properties to values.  For example,
// we might note that some value number represents some particular integer constant -- which has obvious
// application to constant propagation.  Or that we know the exact type of some object reference,
// which might be used in devirtualization.
//
// Finally, we will also use value numbers to express control-flow-dependent assertions.  Some test may
// imply that after the test, something new is known about a value: that an object reference is non-null
// after a dereference (since control flow continued because no exception was thrown); that an integer value
// is restricted to some subrange in after a comparison test; etc.

/*****************************************************************************/
#ifndef _VALUENUM_H_
#define _VALUENUM_H_
/*****************************************************************************/

#include "vartype.h"
// For "GT_COUNT"
#include "gentree.h"
// Defines the type ValueNum.
#include "valuenumtype.h"
// Defines the type SmallHashTable.
#include "smallhash.h"

// A "ValueNumStore" represents the "universe" of value numbers used in a single
// compilation.

// All members of the enumeration genTreeOps are also members of VNFunc.
// (Though some of these may be labeled "illegal").
enum VNFunc
{
    // Implicitly, elements of genTreeOps here.
    VNF_Boundary = GT_COUNT,
#define ValueNumFuncDef(nm, arity, commute, knownNonNull, sharedStatic) VNF_##nm,
#include "valuenumfuncs.h"
    VNF_COUNT
};

// Given a GenTree node return the VNFunc that should be used when value numbering
//
VNFunc GetVNFuncForNode(GenTree* node);

// An instance of this struct represents an application of the function symbol
// "m_func" to the first "m_arity" (<= 4) argument values in "m_args."
struct VNFuncApp
{
    VNFunc   m_func;
    unsigned m_arity;
    ValueNum m_args[4];

    bool Equals(const VNFuncApp& funcApp)
    {
        if (m_func != funcApp.m_func)
        {
            return false;
        }
        if (m_arity != funcApp.m_arity)
        {
            return false;
        }
        for (unsigned i = 0; i < m_arity; i++)
        {
            if (m_args[i] != funcApp.m_args[i])
            {
                return false;
            }
        }
        return true;
    }
};

// We use a unique prefix character when printing value numbers in dumps:  i.e.  $1c0
// This define is used with string concatenation to put this in printf format strings
#define FMT_VN "$%x"

class ValueNumStore
{

public:
    // We will reserve "max unsigned" to represent "not a value number", for maps that might start uninitialized.
    static const ValueNum NoVN = UINT32_MAX;
    // A second special value, used to indicate that a function evaluation would cause infinite recursion.
    static const ValueNum RecursiveVN = UINT32_MAX - 1;

    // ==================================================================================================
    // VNMap - map from something to ValueNum, where something is typically a constant value or a VNFunc
    //         This class has two purposes - to abstract the implementation and to validate the ValueNums
    //         being stored or retrieved.
    template <class fromType, class keyfuncs = JitLargePrimitiveKeyFuncs<fromType>>
    class VNMap : public JitHashTable<fromType, keyfuncs, ValueNum>
    {
    public:
        VNMap(CompAllocator alloc) : JitHashTable<fromType, keyfuncs, ValueNum>(alloc)
        {
        }
        ~VNMap()
        {
            ~VNMap<fromType, keyfuncs>::JitHashTable();
        }

        bool Set(fromType k, ValueNum val)
        {
            assert(val != RecursiveVN);
            return JitHashTable<fromType, keyfuncs, ValueNum>::Set(k, val);
        }
        bool Lookup(fromType k, ValueNum* pVal = nullptr) const
        {
            bool result = JitHashTable<fromType, keyfuncs, ValueNum>::Lookup(k, pVal);
            assert(!result || *pVal != RecursiveVN);
            return result;
        }
    };

private:
    Compiler* m_pComp;

    // For allocations.  (Other things?)
    CompAllocator m_alloc;

    // TODO-Cleanup: should transform "attribs" into a struct with bit fields.  That would be simpler...

    enum VNFOpAttrib
    {
        VNFOA_IllegalGenTreeOp = 0x1,  // corresponds to a genTreeOps value that is not a legal VN func.
        VNFOA_Commutative      = 0x2,  // 1 iff the function is commutative.
        VNFOA_Arity1           = 0x4,  // Bits 2,3,4 encode the arity.
        VNFOA_Arity2           = 0x8,  // Bits 2,3,4 encode the arity.
        VNFOA_Arity4           = 0x10, // Bits 2,3,4 encode the arity.
        VNFOA_KnownNonNull     = 0x20, // 1 iff the result is known to be non-null.
        VNFOA_SharedStatic     = 0x40, // 1 iff this VNF is represent one of the shared static jit helpers
    };

    static const unsigned VNFOA_ArityShift = 2;
    static const unsigned VNFOA_ArityBits  = 3;
    static const unsigned VNFOA_MaxArity   = (1 << VNFOA_ArityBits) - 1; // Max arity we can represent.
    static const unsigned VNFOA_ArityMask  = (VNFOA_Arity4 | VNFOA_Arity2 | VNFOA_Arity1);

    // These enum constants are used to encode the cast operation in the lowest bits by VNForCastOper
    enum VNFCastAttrib
    {
        VCA_UnsignedSrc = 0x01,

        VCA_BitCount     = 1,    // the number of reserved bits
        VCA_ReservedBits = 0x01, // i.e. (VCA_UnsignedSrc)
    };

    // An array of length GT_COUNT, mapping genTreeOp values to their VNFOpAttrib.
    static UINT8* s_vnfOpAttribs;

    // Returns "true" iff gtOper is a legal value number function.
    // (Requires InitValueNumStoreStatics to have been run.)
    static bool GenTreeOpIsLegalVNFunc(genTreeOps gtOper);

    // Returns "true" iff "vnf" is a commutative (and thus binary) operator.
    // (Requires InitValueNumStoreStatics to have been run.)
    static bool VNFuncIsCommutative(VNFunc vnf);

    // Returns "true" iff "vnf" is a comparison (and thus binary) operator.
    static bool VNFuncIsComparison(VNFunc vnf);

    // Returns "true" iff "vnf" can be evaluated for constant arguments.
    static bool CanEvalForConstantArgs(VNFunc vnf);

    // Returns "true" iff "vnf" should be folded by evaluating the func with constant arguments.
    bool VNEvalShouldFold(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN);

    // return vnf(v0)
    template <typename T>
    static T EvalOp(VNFunc vnf, T v0);

    // returns vnf(v0, v1).
    template <typename T>
    T EvalOp(VNFunc vnf, T v0, T v1);

    // return vnf(v0) or vnf(v0, v1), respectively (must, of course be unary/binary ops, respectively.)
    template <typename T>
    static T EvalOpSpecialized(VNFunc vnf, T v0);
    template <typename T>
    T EvalOpSpecialized(VNFunc vnf, T v0, T v1);

    template <typename T>
    static int EvalComparison(VNFunc vnf, T v0, T v1);

    // Should only instantiate (in a non-trivial way) for "int" and "INT64".  Returns true iff dividing "v0" by "v1"
    // would produce integer overflow (an ArithmeticException -- *not* division by zero, which is separate.)
    template <typename T>
    static bool IsOverflowIntDiv(T v0, T v1);

    // Should only instantiate (in a non-trivial way) for integral types (signed/unsigned int32/int64).
    // Returns true iff v is the zero of the appropriate type.
    template <typename T>
    static bool IsIntZero(T v);

    // Given an constant value number return its value.
    int GetConstantInt32(ValueNum argVN);
    INT64 GetConstantInt64(ValueNum argVN);
    double GetConstantDouble(ValueNum argVN);
    float GetConstantSingle(ValueNum argVN);

    // Assumes that all the ValueNum arguments of each of these functions have been shown to represent constants.
    // Assumes that "vnf" is a operator of the appropriate arity (unary for the first, binary for the second).
    // Assume that "CanEvalForConstantArgs(vnf)" is true.
    // Returns the result of evaluating the function with those constant arguments.
    ValueNum EvalFuncForConstantArgs(var_types typ, VNFunc vnf, ValueNum vn0);
    ValueNum EvalFuncForConstantArgs(var_types typ, VNFunc vnf, ValueNum vn0, ValueNum vn1);
    ValueNum EvalFuncForConstantFPArgs(var_types typ, VNFunc vnf, ValueNum vn0, ValueNum vn1);
    ValueNum EvalCastForConstantArgs(var_types typ, VNFunc vnf, ValueNum vn0, ValueNum vn1);

    ValueNum EvalUsingMathIdentity(var_types typ, VNFunc vnf, ValueNum vn0, ValueNum vn1);

// This is the constant value used for the default value of m_mapSelectBudget
#define DEFAULT_MAP_SELECT_BUDGET 100 // used by JitVNMapSelBudget

    // This is the maximum number of MapSelect terms that can be "considered" as part of evaluation of a top-level
    // MapSelect application.
    int m_mapSelectBudget;

    template <typename T, typename NumMap>
    inline ValueNum VnForConst(T cnsVal, NumMap* numMap, var_types varType);

    // returns true iff vn is known to be a constant int32 that is > 0
    bool IsVNPositiveInt32Constant(ValueNum vn);

public:
    // Initializes any static variables of ValueNumStore.
    static void InitValueNumStoreStatics();

    // Initialize an empty ValueNumStore.
    ValueNumStore(Compiler* comp, CompAllocator allocator);

    // Returns "true" iff "vnf" (which may have been created by a cast from an integral value) represents
    // a legal value number function.
    // (Requires InitValueNumStoreStatics to have been run.)
    static bool VNFuncIsLegal(VNFunc vnf)
    {
        return unsigned(vnf) > VNF_Boundary || GenTreeOpIsLegalVNFunc(static_cast<genTreeOps>(vnf));
    }

    // Returns "true" iff "vnf" is one of:
    // VNF_ADD_OVF, VNF_SUB_OVF, VNF_MUL_OVF,
    // VNF_ADD_UN_OVF, VNF_SUB_UN_OVF, VNF_MUL_UN_OVF.
    static bool VNFuncIsOverflowArithmetic(VNFunc vnf);

    // Returns "true" iff "vnf" is VNF_Cast or VNF_CastOvf.
    static bool VNFuncIsNumericCast(VNFunc vnf);

    // Returns the arity of "vnf".
    static unsigned VNFuncArity(VNFunc vnf);

    // Requires "gtOper" to be a genTreeOps legally representing a VNFunc, and returns that
    // VNFunc.
    // (Requires InitValueNumStoreStatics to have been run.)
    static VNFunc GenTreeOpToVNFunc(genTreeOps gtOper)
    {
        assert(GenTreeOpIsLegalVNFunc(gtOper));
        return static_cast<VNFunc>(gtOper);
    }

#ifdef DEBUG
    static void RunTests(Compiler* comp);
#endif // DEBUG

    // This block of methods gets value numbers for constants of primitive types.
    ValueNum VNForIntCon(INT32 cnsVal);
    ValueNum VNForLongCon(INT64 cnsVal);
    ValueNum VNForFloatCon(float cnsVal);
    ValueNum VNForDoubleCon(double cnsVal);
    ValueNum VNForByrefCon(target_size_t byrefVal);

#ifdef TARGET_64BIT
    ValueNum VNForPtrSizeIntCon(INT64 cnsVal)
    {
        return VNForLongCon(cnsVal);
    }
#else
    ValueNum VNForPtrSizeIntCon(INT32 cnsVal)
    {
        return VNForIntCon(cnsVal);
    }
#endif

    // Packs information about the cast into an integer constant represented by the returned value number,
    // to be used as the second operand of VNF_Cast & VNF_CastOvf.
    ValueNum VNForCastOper(var_types castToType, bool srcIsUnsigned = false DEBUGARG(bool printResult = true));

    // Unpacks the information stored by VNForCastOper in the constant represented by the value number.
    void GetCastOperFromVN(ValueNum vn, var_types* pCastToType, bool* pSrcIsUnsigned);

    // We keep handle values in a separate pool, so we don't confuse a handle with an int constant
    // that happens to be the same...
    ValueNum VNForHandle(ssize_t cnsVal, GenTreeFlags iconFlags);

    // And the single constant for an object reference type.
    static ValueNum VNForNull()
    {
        // We reserve Chunk 0 for "special" VNs.  SRC_Null (== 0) is the VN of "null".
        return ValueNum(SRC_Null);
    }

    // The zero map is the map that returns a zero "for the appropriate type" when indexed at any index.
    static ValueNum VNForZeroMap()
    {
        // We reserve Chunk 0 for "special" VNs.  Let SRC_ZeroMap (== 1) be the zero map.
        return ValueNum(SRC_ZeroMap);
    }

    // The ROH map is the map for the "read-only heap".  We assume that this is never mutated, and always
    // has the same value number.
    static ValueNum VNForROH()
    {
        // We reserve Chunk 0 for "special" VNs.  Let SRC_ReadOnlyHeap (== 3) be the read-only heap.
        return ValueNum(SRC_ReadOnlyHeap);
    }

    // A special value number for "void" -- sometimes a type-void thing is an argument to a
    // GT_LIST, and we want the args to be non-NoVN.
    static ValueNum VNForVoid()
    {
        // We reserve Chunk 0 for "special" VNs.  Let SRC_Void (== 4) be the value for "void".
        return ValueNum(SRC_Void);
    }
    static ValueNumPair VNPForVoid()
    {
        return ValueNumPair(VNForVoid(), VNForVoid());
    }

    // A special value number for the empty set of exceptions.
    static ValueNum VNForEmptyExcSet()
    {
        // We reserve Chunk 0 for "special" VNs.  Let SRC_EmptyExcSet (== 5) be the value for the empty set of
        // exceptions.
        return ValueNum(SRC_EmptyExcSet);
    }
    static ValueNumPair VNPForEmptyExcSet()
    {
        return ValueNumPair(VNForEmptyExcSet(), VNForEmptyExcSet());
    }

    // Returns the value number for zero of the given "typ".
    // It has an unreached() for a "typ" that has no zero value, such as TYP_VOID.
    ValueNum VNZeroForType(var_types typ);

    // Returns the value number for one of the given "typ".
    // It returns NoVN for a "typ" that has no one value, such as TYP_REF.
    ValueNum VNOneForType(var_types typ);

    // Create or return the existimg value number representing a singleton exception set
    // for the the exception value "x".
    ValueNum VNExcSetSingleton(ValueNum x);
    ValueNumPair VNPExcSetSingleton(ValueNumPair x);

    // Returns true if the current pair of items are in ascending order and they are not duplicates.
    // Used to verify that exception sets are in ascending order when processing them.
    bool VNCheckAscending(ValueNum item, ValueNum xs1);

    // Returns the VN representing the union of the two exception sets "xs0" and "xs1".
    // These must be VNForEmtpyExcSet() or applications of VNF_ExcSetCons, obeying
    // the ascending order invariant. (which is preserved in the result)
    ValueNum VNExcSetUnion(ValueNum xs0, ValueNum xs1);

    ValueNumPair VNPExcSetUnion(ValueNumPair xs0vnp, ValueNumPair xs1vnp);

    // Returns the VN representing the intersection of the two exception sets "xs0" and "xs1".
    // These must be applications of VNF_ExcSetCons or the empty set. (i.e VNForEmptyExcSet())
    // and also must be in ascending order.
    ValueNum VNExcSetIntersection(ValueNum xs0, ValueNum xs1);

    ValueNumPair VNPExcSetIntersection(ValueNumPair xs0vnp, ValueNumPair xs1vnp);

    // Returns true if every exeception singleton in the vnCandidateSet is also present
    // in the vnFullSet.
    // Both arguments must be either VNForEmptyExcSet() or applications of VNF_ExcSetCons.
    bool VNExcIsSubset(ValueNum vnFullSet, ValueNum vnCandidateSet);

    // Returns "true" iff "vn" is an application of "VNF_ValWithExc".
    bool VNHasExc(ValueNum vn)
    {
        VNFuncApp funcApp;
        return GetVNFunc(vn, &funcApp) && funcApp.m_func == VNF_ValWithExc;
    }

    // If vn "excSet" is "VNForEmptyExcSet()" we just return "vn"
    // otherwise we use VNExcSetUnion to combine the exception sets of both "vn" and "excSet"
    // and return that ValueNum
    ValueNum VNWithExc(ValueNum vn, ValueNum excSet);

    ValueNumPair VNPWithExc(ValueNumPair vnp, ValueNumPair excSetVNP);

    // This sets "*pvn" to the Normal value and sets "*pvnx" to Exception set value.
    // "pvnx" represents the set of all exceptions that can happen for the expression
    void VNUnpackExc(ValueNum vnWx, ValueNum* pvn, ValueNum* pvnx);

    void VNPUnpackExc(ValueNumPair vnWx, ValueNumPair* pvn, ValueNumPair* pvnx);

    // This returns the Union of exceptions from vnWx and vnExcSet
    ValueNum VNUnionExcSet(ValueNum vnWx, ValueNum vnExcSet);

    // This returns the Union of exceptions from vnpWx and vnpExcSet
    ValueNumPair VNPUnionExcSet(ValueNumPair vnpWx, ValueNumPair vnpExcSet);

    // Sets the normal value to a new unique ValueNum
    // Keeps any Exception set values
    ValueNum VNMakeNormalUnique(ValueNum vn);

    // Sets the liberal & conservative
    // Keeps any Exception set values
    ValueNumPair VNPMakeNormalUniquePair(ValueNumPair vnp);

    // If "vn" is a "VNF_ValWithExc(norm, excSet)" value, returns the "norm" argument; otherwise,
    // just returns "vn".
    // The Normal value is the value number of the expression when no exceptions occurred
    ValueNum VNNormalValue(ValueNum vn);

    // Given a "vnp", get the ValueNum kind based upon vnk,
    // then call VNNormalValue on that ValueNum
    // The Normal value is the value number of the expression when no exceptions occurred
    ValueNum VNNormalValue(ValueNumPair vnp, ValueNumKind vnk);

    // Given a "vnp", get the NormalValuew for the VNK_Liberal part of that ValueNum
    // The Normal value is the value number of the expression when no exceptions occurred
    inline ValueNum VNLiberalNormalValue(ValueNumPair vnp)
    {
        return VNNormalValue(vnp, VNK_Liberal);
    }

    // Given a "vnp", get the NormalValuew for the VNK_Conservative part of that ValueNum
    // The Normal value is the value number of the expression when no exceptions occurred
    inline ValueNum VNConservativeNormalValue(ValueNumPair vnp)
    {
        return VNNormalValue(vnp, VNK_Conservative);
    }

    // Given a "vnp", get the Normal values for both the liberal and conservative parts of "vnp"
    // The Normal value is the value number of the expression when no exceptions occurred
    ValueNumPair VNPNormalPair(ValueNumPair vnp);

    // If "vn" is a "VNF_ValWithExc(norm, excSet)" value, returns the "excSet" argument; otherwise,
    // we return a special Value Number representing the empty exception set.
    // The exeception set value is the value number of the set of possible exceptions.
    ValueNum VNExceptionSet(ValueNum vn);

    ValueNumPair VNPExceptionSet(ValueNumPair vn);

    // True "iff" vn is a value known to be non-null.  (For example, the result of an allocation...)
    bool IsKnownNonNull(ValueNum vn);

    // True "iff" vn is a value returned by a call to a shared static helper.
    bool IsSharedStatic(ValueNum vn);

    // VNForFunc: We have five overloads, for arities 0, 1, 2, 3 and 4
    ValueNum VNForFunc(var_types typ, VNFunc func);
    ValueNum VNForFunc(var_types typ, VNFunc func, ValueNum opVNwx);
    // This must not be used for VNF_MapSelect applications; instead use VNForMapSelect, below.
    ValueNum VNForFunc(var_types typ, VNFunc func, ValueNum op1VNwx, ValueNum op2VNwx);
    ValueNum VNForFunc(var_types typ, VNFunc func, ValueNum op1VNwx, ValueNum op2VNwx, ValueNum op3VNwx);

    // The following four-op VNForFunc is used for VNF_PtrToArrElem, elemTypeEqVN, arrVN, inxVN, fldSeqVN
    ValueNum VNForFunc(
        var_types typ, VNFunc func, ValueNum op1VNwx, ValueNum op2VNwx, ValueNum op3VNwx, ValueNum op4VNwx);

    // This requires a "ValueNumKind" because it will attempt, given "select(phi(m1, ..., mk), ind)", to evaluate
    // "select(m1, ind)", ..., "select(mk, ind)" to see if they agree.  It needs to know which kind of value number
    // (liberal/conservative) to read from the SSA def referenced in the phi argument.
    ValueNum VNForMapSelect(ValueNumKind vnk, var_types typ, ValueNum op1VN, ValueNum op2VN);

    // A method that does the work for VNForMapSelect and may call itself recursively.
    ValueNum VNForMapSelectWork(
        ValueNumKind vnk, var_types typ, ValueNum op1VN, ValueNum op2VN, int* pBudget, bool* pUsedRecursiveVN);

    // A specialized version of VNForFunc that is used for VNF_MapStore and provides some logging when verbose is set
    ValueNum VNForMapStore(var_types typ, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN);

    // These functions parallel the ones above, except that they take liberal/conservative VN pairs
    // as arguments, and return such a pair (the pair of the function applied to the liberal args, and
    // the function applied to the conservative args).
    ValueNumPair VNPairForFunc(var_types typ, VNFunc func)
    {
        ValueNumPair res;
        res.SetBoth(VNForFunc(typ, func));
        return res;
    }
    ValueNumPair VNPairForFunc(var_types typ, VNFunc func, ValueNumPair opVN)
    {
        return ValueNumPair(VNForFunc(typ, func, opVN.GetLiberal()), VNForFunc(typ, func, opVN.GetConservative()));
    }
    ValueNumPair VNPairForFunc(var_types typ, VNFunc func, ValueNumPair op1VN, ValueNumPair op2VN)
    {
        return ValueNumPair(VNForFunc(typ, func, op1VN.GetLiberal(), op2VN.GetLiberal()),
                            VNForFunc(typ, func, op1VN.GetConservative(), op2VN.GetConservative()));
    }
    ValueNumPair VNPairForFunc(var_types typ, VNFunc func, ValueNumPair op1VN, ValueNumPair op2VN, ValueNumPair op3VN)
    {
        return ValueNumPair(VNForFunc(typ, func, op1VN.GetLiberal(), op2VN.GetLiberal(), op3VN.GetLiberal()),
                            VNForFunc(typ, func, op1VN.GetConservative(), op2VN.GetConservative(),
                                      op3VN.GetConservative()));
    }
    ValueNumPair VNPairForFunc(
        var_types typ, VNFunc func, ValueNumPair op1VN, ValueNumPair op2VN, ValueNumPair op3VN, ValueNumPair op4VN)
    {
        return ValueNumPair(VNForFunc(typ, func, op1VN.GetLiberal(), op2VN.GetLiberal(), op3VN.GetLiberal(),
                                      op4VN.GetLiberal()),
                            VNForFunc(typ, func, op1VN.GetConservative(), op2VN.GetConservative(),
                                      op3VN.GetConservative(), op4VN.GetConservative()));
    }

    // Get a new, unique value number for an expression that we're not equating to some function,
    // which is the value of a tree in the given block.
    ValueNum VNForExpr(BasicBlock* block, var_types typ = TYP_UNKNOWN);

// This controls extra tracing of the "evaluation" of "VNF_MapSelect" functions.
#define FEATURE_VN_TRACE_APPLY_SELECTORS 1

    // Return the value number corresponding to constructing "MapSelect(map, f0)", where "f0" is the
    // (value number of) the first field in "fieldSeq".  (The type of this application will be the type of "f0".)
    // If there are no remaining fields in "fieldSeq", return that value number; otherwise, return VNApplySelectors
    // applied to that value number and the remainder of "fieldSeq". When the 'fieldSeq' specifies a TYP_STRUCT
    // then the size of the struct is returned by 'wbFinalStructSize' (when it is non-null)
    ValueNum VNApplySelectors(ValueNumKind  vnk,
                              ValueNum      map,
                              FieldSeqNode* fieldSeq,
                              size_t*       wbFinalStructSize = nullptr);

    // Used after VNApplySelectors has determined that "selectedVN" is contained in a Map using VNForMapSelect
    // It determines whether the 'selectedVN' is of an appropriate type to be read using and indirection of 'indType'
    // If it is appropriate type then 'selectedVN' is returned, otherwise it may insert a cast to indType
    // or return a unique value number for an incompatible indType.
    ValueNum VNApplySelectorsTypeCheck(ValueNum selectedVN, var_types indType, size_t structSize);

    // Assumes that "map" represents a map that is addressable by the fields in "fieldSeq", to get
    // to a value of the type of "rhs".  Returns an expression for the RHS of an assignment, in the given "block",
    // to a location containing value "map" that will change the field addressed by "fieldSeq" to "rhs", leaving
    // all other indices in "map" the same.
    ValueNum VNApplySelectorsAssign(
        ValueNumKind vnk, ValueNum map, FieldSeqNode* fieldSeq, ValueNum rhs, var_types indType, BasicBlock* block);

    // Used after VNApplySelectorsAssign has determined that "elem" is to be writen into a Map using VNForMapStore
    // It determines whether the 'elem' is of an appropriate type to be writen using using an indirection of 'indType'
    // It may insert a cast to indType or return a unique value number for an incompatible indType.
    ValueNum VNApplySelectorsAssignTypeCoerce(ValueNum elem, var_types indType, BasicBlock* block);

    ValueNumPair VNPairApplySelectors(ValueNumPair map, FieldSeqNode* fieldSeq, var_types indType);

    ValueNumPair VNPairApplySelectorsAssign(
        ValueNumPair map, FieldSeqNode* fieldSeq, ValueNumPair rhs, var_types indType, BasicBlock* block)
    {
        return ValueNumPair(VNApplySelectorsAssign(VNK_Liberal, map.GetLiberal(), fieldSeq, rhs.GetLiberal(), indType,
                                                   block),
                            VNApplySelectorsAssign(VNK_Conservative, map.GetConservative(), fieldSeq,
                                                   rhs.GetConservative(), indType, block));
    }

    // Compute the normal ValueNumber for a cast with no exceptions
    ValueNum VNForCast(ValueNum srcVN, var_types castToType, var_types castFromType, bool srcIsUnsigned = false);

    // Compute the ValueNumberPair for a cast
    ValueNumPair VNPairForCast(ValueNumPair srcVNPair,
                               var_types    castToType,
                               var_types    castFromType,
                               bool         srcIsUnsigned    = false,
                               bool         hasOverflowCheck = false);

    // Returns true iff the VN represents an application of VNF_NotAField.
    bool IsVNNotAField(ValueNum vn);

    // PtrToLoc values need to express a field sequence as one of their arguments.  VN for null represents
    // empty sequence, otherwise, "FieldSeq(VN(FieldHandle), restOfSeq)".
    ValueNum VNForFieldSeq(FieldSeqNode* fieldSeq);

    // Requires that "vn" represents a field sequence, that is, is the result of a call to VNForFieldSeq.
    // Returns the FieldSequence it represents.
    FieldSeqNode* FieldSeqVNToFieldSeq(ValueNum vn);

    // Both argument must represent field sequences; returns the value number representing the
    // concatenation "fsVN1 || fsVN2".
    ValueNum FieldSeqVNAppend(ValueNum fsVN1, ValueNum fsVN2);

    // If "opA" has a PtrToLoc, PtrToArrElem, or PtrToStatic application as its value numbers, and "opB" is an integer
    // with a "fieldSeq", returns the VN for the pointer form extended with the field sequence; or else NoVN.
    ValueNum ExtendPtrVN(GenTree* opA, GenTree* opB);
    // If "opA" has a PtrToLoc, PtrToArrElem, or PtrToStatic application as its value numbers, returns the VN for the
    // pointer form extended with "fieldSeq"; or else NoVN.
    ValueNum ExtendPtrVN(GenTree* opA, FieldSeqNode* fieldSeq);

    // Queries on value numbers.
    // All queries taking value numbers require that those value numbers are valid, that is, that
    // they have been returned by previous "VNFor..." operations.  They can assert false if this is
    // not true.

    // Returns TYP_UNKNOWN if the given value number has not been given a type.
    var_types TypeOfVN(ValueNum vn);

    // Returns MAX_LOOP_NUM if the given value number's loop nest is unknown or ill-defined.
    BasicBlock::loopNumber LoopOfVN(ValueNum vn);

    // Returns true iff the VN represents a (non-handle) constant.
    bool IsVNConstant(ValueNum vn);

    // Returns true iff the VN represents an integer constant.
    bool IsVNInt32Constant(ValueNum vn);

    typedef SmallHashTable<ValueNum, bool, 8U> CheckedBoundVNSet;

    // Returns true if the VN is known or likely to appear as the conservative value number
    // of the length argument to a GT_ARR_BOUNDS_CHECK node.
    bool IsVNCheckedBound(ValueNum vn);

    // Record that a VN is known to appear as the conservative value number of the length
    // argument to a GT_ARR_BOUNDS_CHECK node.
    void SetVNIsCheckedBound(ValueNum vn);

    // Information about the individual components of a value number representing an unsigned
    // comparison of some value against a checked bound VN.
    struct UnsignedCompareCheckedBoundInfo
    {
        unsigned cmpOper;
        ValueNum vnIdx;
        ValueNum vnBound;

        UnsignedCompareCheckedBoundInfo() : cmpOper(GT_NONE), vnIdx(NoVN), vnBound(NoVN)
        {
        }
    };

    struct CompareCheckedBoundArithInfo
    {
        // (vnBound - 1) > vnOp
        // (vnBound arrOper arrOp) cmpOper cmpOp
        ValueNum vnBound;
        unsigned arrOper;
        ValueNum arrOp;
        unsigned cmpOper;
        ValueNum cmpOp;
        CompareCheckedBoundArithInfo() : vnBound(NoVN), arrOper(GT_NONE), arrOp(NoVN), cmpOper(GT_NONE), cmpOp(NoVN)
        {
        }
#ifdef DEBUG
        void dump(ValueNumStore* vnStore)
        {
            vnStore->vnDump(vnStore->m_pComp, cmpOp);
            printf(" ");
            printf(vnStore->VNFuncName((VNFunc)cmpOper));
            printf(" ");
            vnStore->vnDump(vnStore->m_pComp, vnBound);
            if (arrOper != GT_NONE)
            {
                printf(vnStore->VNFuncName((VNFunc)arrOper));
                vnStore->vnDump(vnStore->m_pComp, arrOp);
            }
        }
#endif
    };

    struct ConstantBoundInfo
    {
        // 100 > vnOp
        int      constVal;
        unsigned cmpOper;
        ValueNum cmpOpVN;

        ConstantBoundInfo() : constVal(0), cmpOper(GT_NONE), cmpOpVN(NoVN)
        {
        }

#ifdef DEBUG
        void dump(ValueNumStore* vnStore)
        {
            vnStore->vnDump(vnStore->m_pComp, cmpOpVN);
            printf(" ");
            printf(vnStore->VNFuncName((VNFunc)cmpOper));
            printf(" ");
            printf("%d", constVal);
        }
#endif
    };

    // Check if "vn" is "new [] (type handle, size)"
    bool IsVNNewArr(ValueNum vn, VNFuncApp* funcApp);

    // Check if "vn" IsVNNewArr and return <= 0 if arr size cannot be determined, else array size.
    int GetNewArrSize(ValueNum vn);

    // Check if "vn" is "a.len"
    bool IsVNArrLen(ValueNum vn);

    // If "vn" is VN(a.len) then return VN(a); NoVN if VN(a) can't be determined.
    ValueNum GetArrForLenVn(ValueNum vn);

    // Return true with any Relop except for == and !=  and one operand has to be a 32-bit integer constant.
    bool IsVNConstantBound(ValueNum vn);

    // If "vn" is constant bound, then populate the "info" fields for constVal, cmpOp, cmpOper.
    void GetConstantBoundInfo(ValueNum vn, ConstantBoundInfo* info);

    // If "vn" is of the form "(uint)var < (uint)len" (or equivalent) return true.
    bool IsVNUnsignedCompareCheckedBound(ValueNum vn, UnsignedCompareCheckedBoundInfo* info);

    // If "vn" is of the form "var < len" or "len <= var" return true.
    bool IsVNCompareCheckedBound(ValueNum vn);

    // If "vn" is checked bound, then populate the "info" fields for the boundVn, cmpOp, cmpOper.
    void GetCompareCheckedBound(ValueNum vn, CompareCheckedBoundArithInfo* info);

    // If "vn" is of the form "len +/- var" return true.
    bool IsVNCheckedBoundArith(ValueNum vn);

    // If "vn" is checked bound arith, then populate the "info" fields for arrOper, arrVn, arrOp.
    void GetCheckedBoundArithInfo(ValueNum vn, CompareCheckedBoundArithInfo* info);

    // If "vn" is of the form "var < len +/- k" return true.
    bool IsVNCompareCheckedBoundArith(ValueNum vn);

    // If "vn" is checked bound arith, then populate the "info" fields for cmpOp, cmpOper.
    void GetCompareCheckedBoundArithInfo(ValueNum vn, CompareCheckedBoundArithInfo* info);

    // Returns the flags on the current handle. GTF_ICON_SCOPE_HDL for example.
    GenTreeFlags GetHandleFlags(ValueNum vn);

    // Returns true iff the VN represents a handle constant.
    bool IsVNHandle(ValueNum vn);

    // Convert a vartype_t to the value number's storage type for that vartype_t.
    // For example, ValueNum of type TYP_LONG are stored in a map of INT64 variables.
    // Lang is the language (C++) type for the corresponding vartype_t.
    template <int N>
    struct VarTypConv
    {
    };

private:
    struct Chunk;

    template <typename T>
    static T CoerceTypRefToT(Chunk* c, unsigned offset);

    // Get the actual value and coerce the actual type c->m_typ to the wanted type T.
    template <typename T>
    FORCEINLINE T SafeGetConstantValue(Chunk* c, unsigned offset);

    template <typename T>
    T ConstantValueInternal(ValueNum vn DEBUGARG(bool coerce))
    {
        Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
        assert(c->m_attribs == CEA_Const || c->m_attribs == CEA_Handle);

        unsigned offset = ChunkOffset(vn);

        switch (c->m_typ)
        {
            case TYP_REF:
                assert(0 <= offset && offset <= 1); // Null or exception.
                FALLTHROUGH;

            case TYP_BYREF:

#ifdef _MSC_VER

                assert(&typeid(T) == &typeid(size_t)); // We represent ref/byref constants as size_t's.

#endif // _MSC_VER

                FALLTHROUGH;

            case TYP_INT:
            case TYP_LONG:
            case TYP_FLOAT:
            case TYP_DOUBLE:
                if (c->m_attribs == CEA_Handle)
                {
                    C_ASSERT(offsetof(VNHandle, m_cnsVal) == 0);
                    return (T) reinterpret_cast<VNHandle*>(c->m_defs)[offset].m_cnsVal;
                }
#ifdef DEBUG
                if (!coerce)
                {
                    T val1 = reinterpret_cast<T*>(c->m_defs)[offset];
                    T val2 = SafeGetConstantValue<T>(c, offset);

                    // Detect if there is a mismatch between the VN storage type and explicitly
                    // passed-in type T.
                    bool mismatch = false;
                    if (varTypeIsFloating(c->m_typ))
                    {
                        mismatch = (memcmp(&val1, &val2, sizeof(val1)) != 0);
                    }
                    else
                    {
                        mismatch = (val1 != val2);
                    }

                    if (mismatch)
                    {
                        assert(
                            !"Called ConstantValue<T>(vn), but type(T) != type(vn); Use CoercedConstantValue instead.");
                    }
                }
#endif
                return SafeGetConstantValue<T>(c, offset);

            default:
                assert(false); // We do not record constants of this typ.
                return (T)0;
        }
    }

public:
    // Requires that "vn" is a constant, and that its type is compatible with the explicitly passed
    // type "T". Also, note that "T" has to have an accurate storage size of the TypeOfVN(vn).
    template <typename T>
    T ConstantValue(ValueNum vn)
    {
        return ConstantValueInternal<T>(vn DEBUGARG(false));
    }

    // Requires that "vn" is a constant, and that its type can be coerced to the explicitly passed
    // type "T".
    template <typename T>
    T CoercedConstantValue(ValueNum vn)
    {
        return ConstantValueInternal<T>(vn DEBUGARG(true));
    }

    // Requires "mthFunc" to be an intrinsic math function (one of the allowable values for the "gtMath" field
    // of a GenTreeMath node).  For unary ops, return the value number for the application of this function to
    // "arg0VN". For binary ops, return the value number for the application of this function to "arg0VN" and
    // "arg1VN".

    ValueNum EvalMathFuncUnary(var_types typ, NamedIntrinsic mthFunc, ValueNum arg0VN);

    ValueNum EvalMathFuncBinary(var_types typ, NamedIntrinsic mthFunc, ValueNum arg0VN, ValueNum arg1VN);

    ValueNumPair EvalMathFuncUnary(var_types typ, NamedIntrinsic mthFunc, ValueNumPair arg0VNP)
    {
        return ValueNumPair(EvalMathFuncUnary(typ, mthFunc, arg0VNP.GetLiberal()),
                            EvalMathFuncUnary(typ, mthFunc, arg0VNP.GetConservative()));
    }

    ValueNumPair EvalMathFuncBinary(var_types typ, NamedIntrinsic mthFunc, ValueNumPair arg0VNP, ValueNumPair arg1VNP)
    {
        return ValueNumPair(EvalMathFuncBinary(typ, mthFunc, arg0VNP.GetLiberal(), arg1VNP.GetLiberal()),
                            EvalMathFuncBinary(typ, mthFunc, arg0VNP.GetConservative(), arg1VNP.GetConservative()));
    }

    // Returns "true" iff "vn" represents a function application.
    bool IsVNFunc(ValueNum vn);

    // If "vn" represents a function application, returns "true" and set "*funcApp" to
    // the function application it represents; otherwise, return "false."
    bool GetVNFunc(ValueNum vn, VNFuncApp* funcApp);

    // Requires that "vn" represents a "GC heap address" the sum of a "TYP_REF" value and some integer
    // value.  Returns the TYP_REF value.
    ValueNum VNForRefInAddr(ValueNum vn);

    // Returns "true" iff "vn" is a valid value number -- one that has been previously returned.
    bool VNIsValid(ValueNum vn);

#ifdef DEBUG
// This controls whether we recursively call vnDump on function arguments.
#define FEATURE_VN_DUMP_FUNC_ARGS 0

    // Prints, to standard out, a representation of "vn".
    void vnDump(Compiler* comp, ValueNum vn, bool isPtr = false);

    // Requires "fieldSeq" to be a field sequence VNFuncApp.
    // Prints a representation (comma-separated list of field names) on standard out.
    void vnDumpFieldSeq(Compiler* comp, VNFuncApp* fieldSeq, bool isHead);

    // Requires "mapSelect" to be a map select VNFuncApp.
    // Prints a representation of a MapSelect operation on standard out.
    void vnDumpMapSelect(Compiler* comp, VNFuncApp* mapSelect);

    // Requires "mapStore" to be a map store VNFuncApp.
    // Prints a representation of a MapStore operation on standard out.
    void vnDumpMapStore(Compiler* comp, VNFuncApp* mapStore);

    // Requires "valWithExc" to be a value with an exeception set VNFuncApp.
    // Prints a representation of the exeception set on standard out.
    void vnDumpValWithExc(Compiler* comp, VNFuncApp* valWithExc);

    // Requires "excSeq" to be a ExcSetCons sequence.
    // Prints a representation of the set of exceptions on standard out.
    void vnDumpExcSeq(Compiler* comp, VNFuncApp* excSeq, bool isHead);

#ifdef FEATURE_SIMD
    // Requires "simdType" to be a VNF_SimdType VNFuncApp.
    // Prints a representation (comma-separated list of field names) on standard out.
    void vnDumpSimdType(Compiler* comp, VNFuncApp* simdType);
#endif // FEATURE_SIMD

    // Returns the string name of "vnf".
    static const char* VNFuncName(VNFunc vnf);
    // Used in the implementation of the above.
    static const char* VNFuncNameArr[];

    // Returns the string name of "vn" when it is a reserved value number, nullptr otherwise
    static const char* reservedName(ValueNum vn);

#endif // DEBUG

    // Returns true if "vn" is a reserved value number
    static bool isReservedVN(ValueNum);

private:
    // We will allocate value numbers in "chunks".  Each chunk will have the same type and "constness".
    static const unsigned LogChunkSize    = 6;
    static const unsigned ChunkSize       = 1 << LogChunkSize;
    static const unsigned ChunkOffsetMask = ChunkSize - 1;

    // A "ChunkNum" is a zero-based index naming a chunk in the Store, or else the special "NoChunk" value.
    typedef UINT32        ChunkNum;
    static const ChunkNum NoChunk = UINT32_MAX;

    // Returns the ChunkNum of the Chunk that holds "vn" (which is required to be a valid
    // value number, i.e., one returned by some VN-producing method of this class).
    static ChunkNum GetChunkNum(ValueNum vn)
    {
        return vn >> LogChunkSize;
    }

    // Returns the offset of the given "vn" within its chunk.
    static unsigned ChunkOffset(ValueNum vn)
    {
        return vn & ChunkOffsetMask;
    }

    // The base VN of the next chunk to be allocated.  Should always be a multiple of ChunkSize.
    ValueNum m_nextChunkBase;

    enum ChunkExtraAttribs : BYTE
    {
        CEA_None,      // No extra attributes.
        CEA_Const,     // This chunk contains constant values.
        CEA_Handle,    // This chunk contains handle constants.
        CEA_NotAField, // This chunk contains "not a field" values.
        CEA_Func0,     // Represents functions of arity 0.
        CEA_Func1,     // ...arity 1.
        CEA_Func2,     // ...arity 2.
        CEA_Func3,     // ...arity 3.
        CEA_Func4,     // ...arity 4.
        CEA_Count
    };

    // A "Chunk" holds "ChunkSize" value numbers, starting at "m_baseVN".  All of these share the same
    // "m_typ" and "m_attribs".  These properties determine the interpretation of "m_defs", as discussed below.
    struct Chunk
    {
        // If "m_defs" is non-null, it is an array of size ChunkSize, whose element type is determined by the other
        // members. The "m_numUsed" field indicates the number of elements of "m_defs" that are already consumed (the
        // next one to allocate).
        void*    m_defs;
        unsigned m_numUsed;

        // The value number of the first VN in the chunk.
        ValueNum m_baseVN;

        // The common attributes of this chunk.
        var_types              m_typ;
        ChunkExtraAttribs      m_attribs;
        BasicBlock::loopNumber m_loopNum;

        // Initialize a chunk, starting at "*baseVN", for the given "typ", "attribs", and "loopNum" (using "alloc" for
        // allocations).
        // (Increments "*baseVN" by ChunkSize.)
        Chunk(CompAllocator          alloc,
              ValueNum*              baseVN,
              var_types              typ,
              ChunkExtraAttribs      attribs,
              BasicBlock::loopNumber loopNum);

        // Requires that "m_numUsed < ChunkSize."  Returns the offset of the allocated VN within the chunk; the
        // actual VN is this added to the "m_baseVN" of the chunk.
        unsigned AllocVN()
        {
            assert(m_numUsed < ChunkSize);
            return m_numUsed++;
        }

        template <int N>
        struct Alloc
        {
            typedef typename ValueNumStore::VarTypConv<N>::Type Type;
        };
    };

    struct VNHandle : public JitKeyFuncsDefEquals<VNHandle>
    {
        ssize_t      m_cnsVal;
        GenTreeFlags m_flags;
        // Don't use a constructor to use the default copy constructor for hashtable rehash.
        static void Initialize(VNHandle* handle, ssize_t m_cnsVal, GenTreeFlags m_flags)
        {
            handle->m_cnsVal = m_cnsVal;
            handle->m_flags  = m_flags;
        }
        bool operator==(const VNHandle& y) const
        {
            return m_cnsVal == y.m_cnsVal && m_flags == y.m_flags;
        }
        static unsigned GetHashCode(const VNHandle& val)
        {
            return static_cast<unsigned>(val.m_cnsVal);
        }
    };

    struct VNDefFunc0Arg
    {
        VNFunc m_func;
        VNDefFunc0Arg(VNFunc func) : m_func(func)
        {
        }

        VNDefFunc0Arg() : m_func(VNF_COUNT)
        {
        }

        bool operator==(const VNDefFunc0Arg& y) const
        {
            return m_func == y.m_func;
        }
    };

    struct VNDefFunc1Arg : public VNDefFunc0Arg
    {
        ValueNum m_arg0;
        VNDefFunc1Arg(VNFunc func, ValueNum arg0) : VNDefFunc0Arg(func), m_arg0(arg0)
        {
        }

        VNDefFunc1Arg() : VNDefFunc0Arg(), m_arg0(ValueNumStore::NoVN)
        {
        }

        bool operator==(const VNDefFunc1Arg& y) const
        {
            return VNDefFunc0Arg::operator==(y) && m_arg0 == y.m_arg0;
        }
    };

    struct VNDefFunc2Arg : public VNDefFunc1Arg
    {
        ValueNum m_arg1;
        VNDefFunc2Arg(VNFunc func, ValueNum arg0, ValueNum arg1) : VNDefFunc1Arg(func, arg0), m_arg1(arg1)
        {
        }

        VNDefFunc2Arg() : m_arg1(ValueNumStore::NoVN)
        {
        }

        bool operator==(const VNDefFunc2Arg& y) const
        {
            return VNDefFunc1Arg::operator==(y) && m_arg1 == y.m_arg1;
        }
    };

    struct VNDefFunc3Arg : public VNDefFunc2Arg
    {
        ValueNum m_arg2;
        VNDefFunc3Arg(VNFunc func, ValueNum arg0, ValueNum arg1, ValueNum arg2)
            : VNDefFunc2Arg(func, arg0, arg1), m_arg2(arg2)
        {
        }
        VNDefFunc3Arg() : m_arg2(ValueNumStore::NoVN)
        {
        }

        bool operator==(const VNDefFunc3Arg& y) const
        {
            return VNDefFunc2Arg::operator==(y) && m_arg2 == y.m_arg2;
        }
    };

    struct VNDefFunc4Arg : public VNDefFunc3Arg
    {
        ValueNum m_arg3;
        VNDefFunc4Arg(VNFunc func, ValueNum arg0, ValueNum arg1, ValueNum arg2, ValueNum arg3)
            : VNDefFunc3Arg(func, arg0, arg1, arg2), m_arg3(arg3)
        {
        }
        VNDefFunc4Arg() : m_arg3(ValueNumStore::NoVN)
        {
        }

        bool operator==(const VNDefFunc4Arg& y) const
        {
            return VNDefFunc3Arg::operator==(y) && m_arg3 == y.m_arg3;
        }
    };

    // When we evaluate "select(m, i)", if "m" is a the value of a phi definition, we look at
    // all the values of the phi args, and see if doing the "select" on each of them yields identical
    // results.  If so, that is the result of the entire "select" form.  We have to be careful, however,
    // because phis may be recursive in the presence of loop structures -- the VN for the phi may be (or be
    // part of the definition of) the VN's of some of the arguments.  But there will be at least one
    // argument that does *not* depend on the outer phi VN -- after all, we had to get into the loop somehow.
    // So we have to be careful about breaking infinite recursion.  We can ignore "recursive" results -- if all the
    // non-recursive results are the same, the recursion indicates that the loop structure didn't alter the result.
    // This stack represents the set of outer phis such that select(phi, ind) is being evaluated.
    JitExpandArrayStack<VNDefFunc2Arg> m_fixedPointMapSels;

#ifdef DEBUG
    // Returns "true" iff "m_fixedPointMapSels" is non-empty, and it's top element is
    // "select(map, index)".
    bool FixedPointMapSelsTopHasValue(ValueNum map, ValueNum index);
#endif

    // Returns true if "sel(map, ind)" is a member of "m_fixedPointMapSels".
    bool SelectIsBeingEvaluatedRecursively(ValueNum map, ValueNum ind);

    // This is the set of value numbers that have been flagged as arguments to bounds checks, in the length position.
    CheckedBoundVNSet m_checkedBoundVNs;

    // This is a map from "chunk number" to the attributes of the chunk.
    JitExpandArrayStack<Chunk*> m_chunks;

    // These entries indicate the current allocation chunk, if any, for each valid combination of <var_types,
    // ChunkExtraAttribute, loopNumber>.  Valid combinations require attribs==CEA_None or loopNum==MAX_LOOP_NUM.
    // If the value is NoChunk, it indicates that there is no current allocation chunk for that pair, otherwise
    // it is the index in "m_chunks" of a chunk with the given attributes, in which the next allocation should
    // be attempted.
    ChunkNum m_curAllocChunk[TYP_COUNT][CEA_Count + MAX_LOOP_NUM + 1];

    // Returns a (pointer to a) chunk in which a new value number may be allocated.
    Chunk* GetAllocChunk(var_types typ, ChunkExtraAttribs attribs, BasicBlock::loopNumber loopNum = MAX_LOOP_NUM);

    // First, we need mechanisms for mapping from constants to value numbers.
    // For small integers, we'll use an array.
    static const int      SmallIntConstMin = -1;
    static const int      SmallIntConstMax = 10;
    static const unsigned SmallIntConstNum = SmallIntConstMax - SmallIntConstMin + 1;
    static bool IsSmallIntConst(int i)
    {
        return SmallIntConstMin <= i && i <= SmallIntConstMax;
    }
    ValueNum m_VNsForSmallIntConsts[SmallIntConstNum];

    struct ValueNumList
    {
        ValueNum      vn;
        ValueNumList* next;
        ValueNumList(const ValueNum& v, ValueNumList* n = nullptr) : vn(v), next(n)
        {
        }
    };

    // Keeps track of value numbers that are integer constants and also handles (GTG_ICON_HDL_MASK.)
    ValueNumList* m_intConHandles;

    typedef VNMap<INT32> IntToValueNumMap;
    IntToValueNumMap*    m_intCnsMap;
    IntToValueNumMap*    GetIntCnsMap()
    {
        if (m_intCnsMap == nullptr)
        {
            m_intCnsMap = new (m_alloc) IntToValueNumMap(m_alloc);
        }
        return m_intCnsMap;
    }

    typedef VNMap<INT64> LongToValueNumMap;
    LongToValueNumMap*   m_longCnsMap;
    LongToValueNumMap*   GetLongCnsMap()
    {
        if (m_longCnsMap == nullptr)
        {
            m_longCnsMap = new (m_alloc) LongToValueNumMap(m_alloc);
        }
        return m_longCnsMap;
    }

    typedef VNMap<VNHandle, VNHandle> HandleToValueNumMap;
    HandleToValueNumMap* m_handleMap;
    HandleToValueNumMap* GetHandleMap()
    {
        if (m_handleMap == nullptr)
        {
            m_handleMap = new (m_alloc) HandleToValueNumMap(m_alloc);
        }
        return m_handleMap;
    }

    struct LargePrimitiveKeyFuncsFloat : public JitLargePrimitiveKeyFuncs<float>
    {
        static bool Equals(float x, float y)
        {
            return *(unsigned*)&x == *(unsigned*)&y;
        }
    };

    typedef VNMap<float, LargePrimitiveKeyFuncsFloat> FloatToValueNumMap;
    FloatToValueNumMap* m_floatCnsMap;
    FloatToValueNumMap* GetFloatCnsMap()
    {
        if (m_floatCnsMap == nullptr)
        {
            m_floatCnsMap = new (m_alloc) FloatToValueNumMap(m_alloc);
        }
        return m_floatCnsMap;
    }

    // In the JIT we need to distinguish -0.0 and 0.0 for optimizations.
    struct LargePrimitiveKeyFuncsDouble : public JitLargePrimitiveKeyFuncs<double>
    {
        static bool Equals(double x, double y)
        {
            return *(__int64*)&x == *(__int64*)&y;
        }
    };

    typedef VNMap<double, LargePrimitiveKeyFuncsDouble> DoubleToValueNumMap;
    DoubleToValueNumMap* m_doubleCnsMap;
    DoubleToValueNumMap* GetDoubleCnsMap()
    {
        if (m_doubleCnsMap == nullptr)
        {
            m_doubleCnsMap = new (m_alloc) DoubleToValueNumMap(m_alloc);
        }
        return m_doubleCnsMap;
    }

    typedef VNMap<size_t> ByrefToValueNumMap;
    ByrefToValueNumMap*   m_byrefCnsMap;
    ByrefToValueNumMap*   GetByrefCnsMap()
    {
        if (m_byrefCnsMap == nullptr)
        {
            m_byrefCnsMap = new (m_alloc) ByrefToValueNumMap(m_alloc);
        }
        return m_byrefCnsMap;
    }

    struct VNDefFunc0ArgKeyFuncs : public JitKeyFuncsDefEquals<VNDefFunc1Arg>
    {
        static unsigned GetHashCode(VNDefFunc1Arg val)
        {
            return (val.m_func << 24) + val.m_arg0;
        }
    };
    typedef VNMap<VNFunc> VNFunc0ToValueNumMap;
    VNFunc0ToValueNumMap* m_VNFunc0Map;
    VNFunc0ToValueNumMap* GetVNFunc0Map()
    {
        if (m_VNFunc0Map == nullptr)
        {
            m_VNFunc0Map = new (m_alloc) VNFunc0ToValueNumMap(m_alloc);
        }
        return m_VNFunc0Map;
    }

    struct VNDefFunc1ArgKeyFuncs : public JitKeyFuncsDefEquals<VNDefFunc1Arg>
    {
        static unsigned GetHashCode(VNDefFunc1Arg val)
        {
            return (val.m_func << 24) + val.m_arg0;
        }
    };
    typedef VNMap<VNDefFunc1Arg, VNDefFunc1ArgKeyFuncs> VNFunc1ToValueNumMap;
    VNFunc1ToValueNumMap* m_VNFunc1Map;
    VNFunc1ToValueNumMap* GetVNFunc1Map()
    {
        if (m_VNFunc1Map == nullptr)
        {
            m_VNFunc1Map = new (m_alloc) VNFunc1ToValueNumMap(m_alloc);
        }
        return m_VNFunc1Map;
    }

    struct VNDefFunc2ArgKeyFuncs : public JitKeyFuncsDefEquals<VNDefFunc2Arg>
    {
        static unsigned GetHashCode(const VNDefFunc2Arg& val)
        {
            return (val.m_func << 24) + (val.m_arg0 << 8) + val.m_arg1;
        }
    };
    typedef VNMap<VNDefFunc2Arg, VNDefFunc2ArgKeyFuncs> VNFunc2ToValueNumMap;
    VNFunc2ToValueNumMap* m_VNFunc2Map;
    VNFunc2ToValueNumMap* GetVNFunc2Map()
    {
        if (m_VNFunc2Map == nullptr)
        {
            m_VNFunc2Map = new (m_alloc) VNFunc2ToValueNumMap(m_alloc);
        }
        return m_VNFunc2Map;
    }

    struct VNDefFunc3ArgKeyFuncs : public JitKeyFuncsDefEquals<VNDefFunc3Arg>
    {
        static unsigned GetHashCode(const VNDefFunc3Arg& val)
        {
            return (val.m_func << 24) + (val.m_arg0 << 16) + (val.m_arg1 << 8) + val.m_arg2;
        }
    };
    typedef VNMap<VNDefFunc3Arg, VNDefFunc3ArgKeyFuncs> VNFunc3ToValueNumMap;
    VNFunc3ToValueNumMap* m_VNFunc3Map;
    VNFunc3ToValueNumMap* GetVNFunc3Map()
    {
        if (m_VNFunc3Map == nullptr)
        {
            m_VNFunc3Map = new (m_alloc) VNFunc3ToValueNumMap(m_alloc);
        }
        return m_VNFunc3Map;
    }

    struct VNDefFunc4ArgKeyFuncs : public JitKeyFuncsDefEquals<VNDefFunc4Arg>
    {
        static unsigned GetHashCode(const VNDefFunc4Arg& val)
        {
            return (val.m_func << 24) + (val.m_arg0 << 16) + (val.m_arg1 << 8) + val.m_arg2 + (val.m_arg3 << 12);
        }
    };
    typedef VNMap<VNDefFunc4Arg, VNDefFunc4ArgKeyFuncs> VNFunc4ToValueNumMap;
    VNFunc4ToValueNumMap* m_VNFunc4Map;
    VNFunc4ToValueNumMap* GetVNFunc4Map()
    {
        if (m_VNFunc4Map == nullptr)
        {
            m_VNFunc4Map = new (m_alloc) VNFunc4ToValueNumMap(m_alloc);
        }
        return m_VNFunc4Map;
    }

    enum SpecialRefConsts
    {
        SRC_Null,
        SRC_ZeroMap,
        SRC_ReadOnlyHeap,
        SRC_Void,
        SRC_EmptyExcSet,

        SRC_NumSpecialRefConsts
    };

    // The "values" of special ref consts will be all be "null" -- their differing meanings will
    // be carried by the distinct value numbers.
    static class Object* s_specialRefConsts[SRC_NumSpecialRefConsts];
    static class Object* s_nullConst;

#ifdef DEBUG
    // This helps test some performance pathologies related to "evaluation" of VNF_MapSelect terms,
    // especially relating to GcHeap/ByrefExposed.  We count the number of applications of such terms we consider,
    // and if this exceeds a limit, indicated by a COMPlus_ variable, we assert.
    unsigned m_numMapSels;
#endif
};

template <>
struct ValueNumStore::VarTypConv<TYP_INT>
{
    typedef INT32 Type;
    typedef int   Lang;
};
template <>
struct ValueNumStore::VarTypConv<TYP_FLOAT>
{
    typedef INT32 Type;
    typedef float Lang;
};
template <>
struct ValueNumStore::VarTypConv<TYP_LONG>
{
    typedef INT64 Type;
    typedef INT64 Lang;
};
template <>
struct ValueNumStore::VarTypConv<TYP_DOUBLE>
{
    typedef INT64  Type;
    typedef double Lang;
};
template <>
struct ValueNumStore::VarTypConv<TYP_BYREF>
{
    typedef size_t Type;
    typedef void*  Lang;
};
template <>
struct ValueNumStore::VarTypConv<TYP_REF>
{
    typedef class Object* Type;
    typedef class Object* Lang;
};

// Get the actual value and coerce the actual type c->m_typ to the wanted type T.
template <typename T>
FORCEINLINE T ValueNumStore::SafeGetConstantValue(Chunk* c, unsigned offset)
{
    switch (c->m_typ)
    {
        case TYP_REF:
            return CoerceTypRefToT<T>(c, offset);
        case TYP_BYREF:
            return static_cast<T>(reinterpret_cast<VarTypConv<TYP_BYREF>::Type*>(c->m_defs)[offset]);
        case TYP_INT:
            return static_cast<T>(reinterpret_cast<VarTypConv<TYP_INT>::Type*>(c->m_defs)[offset]);
        case TYP_LONG:
            return static_cast<T>(reinterpret_cast<VarTypConv<TYP_LONG>::Type*>(c->m_defs)[offset]);
        case TYP_FLOAT:
            return static_cast<T>(reinterpret_cast<VarTypConv<TYP_FLOAT>::Lang*>(c->m_defs)[offset]);
        case TYP_DOUBLE:
            return static_cast<T>(reinterpret_cast<VarTypConv<TYP_DOUBLE>::Lang*>(c->m_defs)[offset]);
        default:
            assert(false);
            return (T)0;
    }
}

// Inline functions.

// static
inline bool ValueNumStore::GenTreeOpIsLegalVNFunc(genTreeOps gtOper)
{
    return (s_vnfOpAttribs[gtOper] & VNFOA_IllegalGenTreeOp) == 0;
}

// static
inline bool ValueNumStore::VNFuncIsCommutative(VNFunc vnf)
{
    return (s_vnfOpAttribs[vnf] & VNFOA_Commutative) != 0;
}

inline bool ValueNumStore::VNFuncIsComparison(VNFunc vnf)
{
    if (vnf >= VNF_Boundary)
    {
        // For integer types we have unsigned comparisions, and
        // for floating point types these are the unordered variants.
        //
        return ((vnf == VNF_LT_UN) || (vnf == VNF_LE_UN) || (vnf == VNF_GE_UN) || (vnf == VNF_GT_UN));
    }
    genTreeOps gtOp = genTreeOps(vnf);
    return GenTree::OperIsCompare(gtOp) != 0;
}

template <>
inline size_t ValueNumStore::CoerceTypRefToT(Chunk* c, unsigned offset)
{
    return reinterpret_cast<size_t>(reinterpret_cast<VarTypConv<TYP_REF>::Type*>(c->m_defs)[offset]);
}

template <typename T>
inline T ValueNumStore::CoerceTypRefToT(Chunk* c, unsigned offset)
{
    noway_assert(sizeof(T) >= sizeof(VarTypConv<TYP_REF>::Type));
    unreached();
}

/*****************************************************************************/
#endif // _VALUENUM_H_
/*****************************************************************************/
