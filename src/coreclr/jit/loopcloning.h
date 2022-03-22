// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                            Loop Cloning                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

    Loop cloning is an optimization which duplicates a loop to create two versions.
    One copy is optimized by hoisting out various dynamic checks, such as array bounds
    checks that can't be statically eliminated. The checks are dynamically run. If
    they fail, the original copy of the loop is executed. If they pass, the
    optimized copy of the loop is executed, knowing that the bounds checks are
    dynamically unnecessary.

    The optimization can reduce the amount of code executed within a loop body.

    For example:

        public static int f(int[] a, int l)
        {
            int sum = 0;
            for (int i = 0; i < l; i++)
            {
                sum += a[i];     // This array bounds check must be executed in the loop
            }
        }

    This can be transformed to (in pseudo-code):

        public static int f(int[] a, int l)
        {
            int sum = 0;
            if (a != null && l <= a.Length)
            {
                for (int i = 0; i < l; i++)
                {
                    sum += a[i]; // no bounds check needed
                }
            }
            else
            {
                for (int i = 0; i < l; i++)
                {
                    // bounds check needed. We need to do the normal computation (esp., side effects) before the
exception occurs.
                    sum += a[i];
                }
            }
        }

    One generalization of this is "loop unswitching".

    Because code is duplicated, this is a code size expanding optimization, and
    therefore we need to be careful to avoid duplicating too much code unnecessarily.

    Also, there is a risk that we can duplicate the loops and later, downstream
    phases optimize away the bounds checks even on the un-optimized copy of the loop.

    Loop cloning is implemented with the following steps:

    1. Loop detection logic, which is existing logic in the JIT that records
       loop information with loop flags.

    2. Identify loop optimization candidates. This is done by optObtainLoopCloningOpts.
       The loop context variable is updated with all the necessary information (for example:
       block, stmt, tree information) to do the optimization later.
            a) This involves checking if the loop is well-formed with respect to
            the optimization being performed.
            b) In array bounds check case, reconstructing the morphed GT_INDEX
            nodes back to their array representation.
                i) The array index is stored in the "context" variable with
                additional block, tree, stmt info.

    3. Once the optimization candidates are identified, we derive cloning conditions.
       For example: to clone a simple "for (i=0; i<n; ++i) { a[i] }" loop, we need the
       following conditions:
              (a != null) && (n >= 0) && (n <= a.length) && (stride > 0)
       Note that "&&" implies a short-circuiting operator. This requires each condition
       to be in its own block with its own comparison and branch instruction. This can
       be optimized if there are no dependent conditions in a block by using a bitwise
       AND instead of a short-circuit AND. The (a != null) condition needs to occur before
       "a.length" is checked. But otherwise, the last three conditions can be computed in
       the same block, as:
              (a != null) && ((n >= 0) & (n <= a.length) & (stride > 0))
       Since we're optimizing for the expected fast path case, where all the conditions
       are true, we expect all the conditions to be executed most of the time. Thus, it
       is advantageous to make as many as possible non-short-circuiting to reduce the
       number of compare/branch/blocks needed.

       In the above case, stride == 1, so we statically know stride > 0.

       If we had "for (i=0; i<=n; ++i) { a[i] }", we would need:
              (a != null) && (n >= 0) && (a.length >= 1) && (n <= a.length - 1) && (stride > 0)
       This is more complicated. The loop is equivalent (except for possible overflow) to:
              for (i=0; i<n+1; ++i) { a[i] }"
       (`n+1` due to the `++i` stride). We'd have to worry about overflow doing this conversion, though.

       REVIEW: why do we need the (n >= 0) condition? We do need to know
       "array index var initialization value >= array lower bound (0)".

              a) Conditions that need to be in their own blocks to enable short-circuit are called block
              conditions or deref-conditions.
                 i) For a doubly nested loop on i, j, we would then have conditions like
                     (a != null) && (i < a.len) && (a[i] != null) && (j < a[i].len)
                 all short-circuiting creating blocks.

                 Advantage:
                    All conditions are checked before we enter the fast path. So fast
                    path gets as fast as it can be.

                 Disadvantage:
                    Creation of blocks.

                 Heuristic:
                    Therefore we will not clone if we exceed creating 4 blocks.
                    Note: this means we never clone more than 2-dimension a[i][j] expressions
                    (see optComputeDerefConditions()).
                    REVIEW: make this heuristic defined by a COMPlus variable, for easier
                    experimentation, and make it more dynamic and based on potential benefit?

              b) The other conditions called cloning conditions are transformed into LC_Condition
              structs which are then optimized.
                 i) Optimization of conditions involves removing redundant condition checks.
                 ii) If some conditions evaluate to true statically, then they are removed.
                 iii) If any condition evaluates to false statically, then loop cloning is
                 aborted for that loop.

    4. Then the block splitting occurs and loop cloning conditions are transformed into
       GenTree and added to the loop cloning choice block (the block that determines which
       copy of the loop is executed).

    Preconditions

    1. Loop detection has completed and the loop table is populated.

    2. The loops that will be considered are the ones with the LPFLG_ITER flag:
       "for ( ; test_condition(); i++)"

    Limitations

    1. Loops containing nested exception handling regions are not cloned. (Cloning them
       would require creating new exception handling regions for the cloned loop, which
       is "hard".) There are a few other EH-related edge conditions that also cause us to
       reject cloning.

    2. If the loop contains RETURN blocks, and cloning those would push us over the maximum
       number of allowed RETURN blocks in the function (either due to GC info encoding limitations
       or otherwise), we reject cloning.

    3. Loop increment must be `i += 1`

    4. Loop test must be `i < x` or `i <= x` where `x` is a constant, a variable, or `a.Length` for array `a`

    (There is some implementation support for decrementing loops, but it is incomplete.)

    5. Loop must have been converted to a do-while form.

    6. There are a few other loop well-formedness conditions.

    7. Multi-dimensional (non-jagged) loop index checking is only partially implemented.

    8. Constant initializations and constant limits must be non-negative. This is because the
       iterator variable will be used as an array index, and array indices must be non-negative.
       For non-constant (or not found) iterator variable `i` initialization, we add a dynamic check that
       `i >= 0`. Constant initializations can be checked statically.

    9. The cloned loop (the slow path) is not added to the loop table, meaning certain
       downstream optimization passes do not see them. See
       https://github.com/dotnet/runtime/issues/43713.

    Assumptions

    1. The assumption is that the optimization candidates collected during the
       identification phase will be the ones that will be optimized. In other words,
       the loop that is present originally will be the fast path. The cloned
       path will be the slow path and will be unoptimized. This allows us to
       collect additional information at the same time as identifying the optimization
       candidates. This later helps us to perform the optimizations during actual cloning.

    2. All loop cloning choice conditions will automatically be "AND"-ed. These are bitwise AND operations.

    3. Perform short circuit AND for (array != null) side effect check
      before hoisting (limit <= a.length) check.

*/
#pragma once

class Compiler;

/**
 *
 *  Represents an array access and associated bounds checks.
 *  Array access is required to have the array and indices in local variables.
 *  This struct is constructed using a GT_INDEX node that is broken into
 *  its sub trees.
 *
 */
struct ArrIndex
{
    unsigned                      arrLcl;   // The array base local num
    JitExpandArrayStack<unsigned> indLcls;  // The indices local nums
    JitExpandArrayStack<GenTree*> bndsChks; // The bounds checks nodes along each dimension.
    unsigned                      rank;     // Rank of the array
    BasicBlock*                   useBlock; // Block where the [] occurs

    ArrIndex(CompAllocator alloc) : arrLcl(BAD_VAR_NUM), indLcls(alloc), bndsChks(alloc), rank(0), useBlock(nullptr)
    {
    }

#ifdef DEBUG
    void Print(unsigned dim = -1);
    void PrintBoundsCheckNodes(unsigned dim = -1);
#endif
};

// Forward declarations
#define LC_OPT(en) struct en##OptInfo;
#include "loopcloningopts.h"

/**
 *
 *  LcOptInfo represents the optimization information for loop cloning,
 *  other classes are supposed to derive from this base class.
 *
 *  Example usage:
 *
 *  LcMdArrayOptInfo is multi-dimensional array optimization for which the
 *  loop can be cloned.
 *
 *  LcArrIndexOptInfo is a jagged array optimization for which the loop
 *  can be cloned.
 *
 *  So LcOptInfo represents any type of optimization opportunity that
 *  occurs in a loop and the metadata for the optimization is stored in
 *  this class.
 */
struct LcOptInfo
{
    enum OptType
    {
#define LC_OPT(en) en,
#include "loopcloningopts.h"
    };

    OptType optType;
    LcOptInfo(OptType optType) : optType(optType)
    {
    }

    OptType GetOptType()
    {
        return optType;
    }

#define LC_OPT(en)                                                                                                     \
    en##OptInfo* As##en##OptInfo()                                                                                     \
    {                                                                                                                  \
        assert(optType == en);                                                                                         \
        return reinterpret_cast<en##OptInfo*>(this);                                                                   \
    }
#include "loopcloningopts.h"
};

/**
 *
 * Optimization info for a multi-dimensional array.
 */
struct LcMdArrayOptInfo : public LcOptInfo
{
    GenTreeArrElem* arrElem; // "arrElem" node of an MD array.
    unsigned        dim;     // "dim" represents up to what level of the rank this optimization applies to.
                             //    For example, a[i,j,k] could be the MD array "arrElem" but if "dim" is 2,
                             //    then this node is treated as though it were a[i,j]
    ArrIndex* index;         // "index" cached computation in the form of an ArrIndex representation.

    LcMdArrayOptInfo(GenTreeArrElem* arrElem, unsigned dim)
        : LcOptInfo(LcMdArray), arrElem(arrElem), dim(dim), index(nullptr)
    {
    }

    ArrIndex* GetArrIndexForDim(CompAllocator alloc)
    {
        if (index == nullptr)
        {
            index       = new (alloc) ArrIndex(alloc);
            index->rank = arrElem->gtArrRank;
            for (unsigned i = 0; i < dim; ++i)
            {
                index->indLcls.Push(arrElem->gtArrInds[i]->AsLclVarCommon()->GetLclNum());
            }
            index->arrLcl = arrElem->gtArrObj->AsLclVarCommon()->GetLclNum();
        }
        return index;
    }
};

/**
 *
 * Optimization info for a jagged array.
 */
struct LcJaggedArrayOptInfo : public LcOptInfo
{
    unsigned dim;        // "dim" represents up to what level of the rank this optimization applies to.
                         //    For example, a[i][j][k] could be the jagged array but if "dim" is 2,
                         //    then this node is treated as though it were a[i][j]
    ArrIndex   arrIndex; // ArrIndex representation of the array.
    Statement* stmt;     // "stmt" where the optimization opportunity occurs.

    LcJaggedArrayOptInfo(ArrIndex& arrIndex, unsigned dim, Statement* stmt)
        : LcOptInfo(LcJaggedArray), dim(dim), arrIndex(arrIndex), stmt(stmt)
    {
    }
};

/**
 *
 * Symbolic representation of a.length, or a[i][j].length or a[i,j].length and so on.
 * OperType decides whether "arrLength" is invoked on the array or if it is just an array.
 */
struct LC_Array
{
    enum ArrType
    {
        Invalid,
        Jagged,
        MdArray
    };

    enum OperType
    {
        None,
        ArrLen,
    };

    ArrType   type;     // The type of the array on which to invoke length operator.
    ArrIndex* arrIndex; // ArrIndex representation of this array.

    OperType oper;

#ifdef DEBUG
    void Print()
    {
        arrIndex->Print(dim);
        if (oper == ArrLen)
        {
            printf(".Length");
        }
    }
#endif

    int dim; // "dim" = which index to invoke arrLen on, if -1 invoke on the whole array
             //     Example 1: a[0][1][2] and dim =  2 implies a[0][1].length
             //     Example 2: a[0][1][2] and dim = -1 implies a[0][1][2].length
    LC_Array() : type(Invalid), dim(-1)
    {
    }
    LC_Array(ArrType type, ArrIndex* arrIndex, int dim, OperType oper)
        : type(type), arrIndex(arrIndex), oper(oper), dim(dim)
    {
    }

    LC_Array(ArrType type, ArrIndex* arrIndex, OperType oper) : type(type), arrIndex(arrIndex), oper(oper), dim(-1)
    {
    }

    // Equality operator
    bool operator==(const LC_Array& that) const
    {
        assert(type != Invalid && that.type != Invalid);

        // Types match and the array base matches.
        if (type != that.type || arrIndex->arrLcl != that.arrIndex->arrLcl || oper != that.oper)
        {
            return false;
        }

        // If the dim ranks are not matching, quit.
        int rank1 = GetDimRank();
        int rank2 = that.GetDimRank();
        if (rank1 != rank2)
        {
            return false;
        }

        // Check for the indices.
        for (int i = 0; i < rank1; ++i)
        {
            if (arrIndex->indLcls[i] != that.arrIndex->indLcls[i])
            {
                return false;
            }
        }
        return true;
    }

    // The max dim on which length is invoked.
    int GetDimRank() const
    {
        return (dim < 0) ? (int)arrIndex->rank : dim;
    }

    // Get a tree representation for this symbolic a.length
    GenTree* ToGenTree(Compiler* comp, BasicBlock* bb);
};

/**
 *
 * Symbolic representation of either a constant like 1 or 2, or a variable like V02 or V03, or an "LC_Array",
 * or the null constant.
 */
struct LC_Ident
{
    enum IdentType
    {
        Invalid,
        Const,
        Var,
        ArrLen,
        Null,
    };

    LC_Array  arrLen;   // The LC_Array if the type is "ArrLen"
    unsigned  constant; // The constant value if this node is of type "Const", or the lcl num if "Var"
    IdentType type;     // The type of this object

    // Equality operator
    bool operator==(const LC_Ident& that) const
    {
        switch (type)
        {
            case Const:
            case Var:
                return (type == that.type) && (constant == that.constant);
            case ArrLen:
                return (type == that.type) && (arrLen == that.arrLen);
            case Null:
                return (type == that.type);
            default:
                assert(!"Unknown LC_Ident type");
                unreached();
        }
    }

#ifdef DEBUG
    void Print()
    {
        switch (type)
        {
            case Const:
                printf("%u", constant);
                break;
            case Var:
                printf("V%02d", constant);
                break;
            case ArrLen:
                arrLen.Print();
                break;
            case Null:
                printf("null");
                break;
            default:
                printf("INVALID");
                break;
        }
    }
#endif

    LC_Ident() : type(Invalid)
    {
    }
    LC_Ident(unsigned constant, IdentType type) : constant(constant), type(type)
    {
    }
    explicit LC_Ident(IdentType type) : type(type)
    {
    }
    explicit LC_Ident(const LC_Array& arrLen) : arrLen(arrLen), type(ArrLen)
    {
    }

    // Convert this symbolic representation into a tree node.
    GenTree* ToGenTree(Compiler* comp, BasicBlock* bb);
};

/**
 *
 *  Symbolic representation of an expr that involves an "LC_Ident"
 */
struct LC_Expr
{
    enum ExprType
    {
        Invalid,
        Ident,
    };

    LC_Ident ident;
    ExprType type;

    // Equality operator
    bool operator==(const LC_Expr& that) const
    {
        assert(type != Invalid && that.type != Invalid);

        // If the types don't match quit.
        if (type != that.type)
        {
            return false;
        }

        // Check if the ident match.
        return (ident == that.ident);
    }

#ifdef DEBUG
    void Print()
    {
        if (type == Ident)
        {
            ident.Print();
        }
        else
        {
            printf("INVALID");
        }
    }
#endif

    LC_Expr() : type(Invalid)
    {
    }
    explicit LC_Expr(const LC_Ident& ident) : ident(ident), type(Ident)
    {
    }

    // Convert LC_Expr into a tree node.
    GenTree* ToGenTree(Compiler* comp, BasicBlock* bb);
};

/**
 *
 *  Symbolic representation of a conditional operation involving two "LC_Expr":
 *  LC_Expr < LC_Expr, for example: i > 0, i < a.length
 */
struct LC_Condition
{
    LC_Expr    op1;
    LC_Expr    op2;
    genTreeOps oper;

#ifdef DEBUG
    void Print()
    {
        op1.Print();
        printf(" %s ", GenTree::OpName(oper));
        op2.Print();
    }
#endif

    // Check if the condition evaluates statically to true or false, i < i => false, a.length > 0 => true
    // The result is put in "pResult" parameter and is valid if the method returns "true". Otherwise, the
    // condition could not be evaluated.
    bool Evaluates(bool* pResult);

    // Check if two conditions can be combined to yield one condition.
    bool Combines(const LC_Condition& cond, LC_Condition* newCond);

    LC_Condition()
    {
    }
    LC_Condition(genTreeOps oper, const LC_Expr& op1, const LC_Expr& op2) : op1(op1), op2(op2), oper(oper)
    {
    }

    // Convert this conditional operation into a GenTree.
    GenTree* ToGenTree(Compiler* comp, BasicBlock* bb, bool invert);
};

/**
 *  A deref tree of an array expression.
 *  a[i][j][k], b[i] and a[i][y][k] are the occurrences in the loop, then, the tree would be:
 *      a => {
 *          i => {
 *              j => {
 *                  k => {}
 *              },
 *              y => {
 *                  k => {}
 *              },
 *          }
 *      },
 *      b => {
 *          i => {}
 *      }
 */
struct LC_Deref
{
    const LC_Array                  array;
    JitExpandArrayStack<LC_Deref*>* children;

    unsigned level;

    LC_Deref(const LC_Array& array, unsigned level) : array(array), children(nullptr), level(level)
    {
    }

    LC_Deref* Find(unsigned lcl);

    unsigned Lcl();

    bool HasChildren();
    void EnsureChildren(CompAllocator alloc);
    static LC_Deref* Find(JitExpandArrayStack<LC_Deref*>* children, unsigned lcl);

    void DeriveLevelConditions(JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* len);

#ifdef DEBUG
    void Print(unsigned indent = 0)
    {
        unsigned tab = 4 * indent;
        printf("%*sV%02d, level %d => {", tab, "", Lcl(), level);
        if (children != nullptr)
        {
            for (unsigned i = 0; i < children->Size(); ++i)
            {
                if (i > 0)
                {
                    printf(",");
                }
                printf("\n");
#ifdef _MSC_VER
                (*children)[i]->Print(indent + 1);
#else  // _MSC_VER
                (*((JitExpandArray<LC_Deref*>*)children))[i]->Print(indent + 1);
#endif // _MSC_VER
            }
        }
        printf("\n%*s}", tab, "");
    }
#endif
};

/**
 *
 *  The "context" represents data that is used for making loop-cloning decisions.
 *   - The data is the collection of optimization opportunities
 *   - and the conditions (LC_Condition) that decide between the fast
 *     path or the slow path.
 *
 *   BNF for LC_Condition:
 *       LC_Condition :  LC_Expr genTreeOps LC_Expr
 *       LC_Expr      :  LC_Ident | LC_Ident + Constant
 *       LC_Ident     :  Constant | Var | LC_Array
 *       LC_Array     :  .
 *       genTreeOps   :  GT_GE | GT_LE | GT_GT | GT_LT
 *
 */
struct LoopCloneContext
{
    CompAllocator alloc; // The allocator

    // The array of optimization opportunities found in each loop. (loop x optimization-opportunities)
    jitstd::vector<JitExpandArrayStack<LcOptInfo*>*> optInfo;

    // The array of conditions that influence which path to take for each loop. (loop x cloning-conditions)
    jitstd::vector<JitExpandArrayStack<LC_Condition>*> conditions;

    // The array of dereference conditions found in each loop. (loop x deref-conditions)
    jitstd::vector<JitExpandArrayStack<LC_Array>*> derefs;

    // The array of block levels of conditions for each loop. (loop x level x conditions)
    jitstd::vector<JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>*> blockConditions;

    LoopCloneContext(unsigned loopCount, CompAllocator alloc)
        : alloc(alloc), optInfo(alloc), conditions(alloc), derefs(alloc), blockConditions(alloc)
    {
        optInfo.resize(loopCount, nullptr);
        conditions.resize(loopCount, nullptr);
        derefs.resize(loopCount, nullptr);
        blockConditions.resize(loopCount, nullptr);
    }

    // Evaluate conditions into a JTRUE stmt and put it in a new block after `insertAfter`.
    BasicBlock* CondToStmtInBlock(Compiler*                          comp,
                                  JitExpandArrayStack<LC_Condition>& conds,
                                  BasicBlock*                        slowHead,
                                  BasicBlock*                        insertAfter);

    // Get all the optimization information for loop "loopNum"; this information is held in "optInfo" array.
    // If NULL this allocates the optInfo[loopNum] array for "loopNum".
    JitExpandArrayStack<LcOptInfo*>* EnsureLoopOptInfo(unsigned loopNum);

    // Get all the optimization information for loop "loopNum"; this information is held in "optInfo" array.
    // If NULL this does not allocate the optInfo[loopNum] array for "loopNum".
    JitExpandArrayStack<LcOptInfo*>* GetLoopOptInfo(unsigned loopNum);

    // Cancel all optimizations for loop "loopNum" by clearing out the "conditions" member if non-null
    // and setting the optInfo to "null". If "null", then the user of this class is not supposed to
    // clone this loop.
    void CancelLoopOptInfo(unsigned loopNum);

    // Get the conditions that decide which loop to take for "loopNum." If NULL allocate an empty array.
    JitExpandArrayStack<LC_Condition>* EnsureConditions(unsigned loopNum);

    // Get the conditions for loop. No allocation is performed.
    JitExpandArrayStack<LC_Condition>* GetConditions(unsigned loopNum);

    // Ensure that the "deref" conditions array is allocated.
    JitExpandArrayStack<LC_Array>* EnsureDerefs(unsigned loopNum);

    // Get block conditions for each loop, no allocation is performed.
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* GetBlockConditions(unsigned loopNum);

    // Ensure that the block condition is present, if not allocate space.
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* EnsureBlockConditions(unsigned loopNum,
                                                                                   unsigned totalBlocks);

#ifdef DEBUG
    // Print the block conditions for the loop.
    void PrintBlockConditions(unsigned loopNum);
    void PrintBlockLevelConditions(unsigned level, JitExpandArrayStack<LC_Condition>* levelCond);
#endif

    // Does the loop have block conditions?
    bool HasBlockConditions(unsigned loopNum);

    // Evaluate the conditions for "loopNum" and indicate if they are either all true or any of them are false.
    //
    // `pAllTrue` and `pAnyFalse` are OUT parameters.
    //
    // If `*pAllTrue` is `true`, then all the conditions are statically known to be true.
    // The caller doesn't need to clone the loop, but it can perform fast path optimizations.
    //
    // If `*pAnyFalse` is `true`, then at least one condition is statically known to be false.
    // The caller needs to abort cloning the loop (neither clone nor fast path optimizations.)
    //
    // If neither `*pAllTrue` nor `*pAnyFalse` is true, then the evaluation of some conditions are statically unknown.
    //
    // Assumes the conditions involve an AND join operator.
    void EvaluateConditions(unsigned loopNum, bool* pAllTrue, bool* pAnyFalse DEBUGARG(bool verbose));

private:
    void OptimizeConditions(JitExpandArrayStack<LC_Condition>& conds);

public:
    // Optimize conditions to remove redundant conditions.
    void OptimizeConditions(unsigned loopNum DEBUGARG(bool verbose));

    void OptimizeBlockConditions(unsigned loopNum DEBUGARG(bool verbose));

#ifdef DEBUG
    void PrintConditions(unsigned loopNum);
#endif
};
