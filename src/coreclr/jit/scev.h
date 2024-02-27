// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains the definition of the scalar evolution IR. This IR allows
// representing the values of IR nodes inside loops in a closed form, taking
// into account that they are changing on each loop iteration. The IR is based
// around the following possible operations. At the core is ScevOper::AddRec,
// which represents a value that evolves by an add recurrence. In dumps it is
// described by <loop, start, step> where "loop" is the loop the value is
// evolving in, "start" is the initial value and "step" is the step by which
// the value evolves in every iteration.
//
enum class ScevOper
{
    Constant,
    Local,
    ZeroExtend,
    SignExtend,
    Add,
    Mul,
    Lsh,
    AddRec,
};

static bool ScevOperIs(ScevOper oper, ScevOper otherOper)
{
    return oper == otherOper;
}

template <typename... Args>
static bool ScevOperIs(ScevOper oper, ScevOper operFirst, Args... operTail)
{
    return oper == operFirst || ScevOperIs(oper, operTail...);
}

struct Scev
{
    const ScevOper  Oper;
    const var_types Type;

    Scev(ScevOper oper, var_types type) : Oper(oper), Type(type)
    {
    }

    template <typename... Args>
    bool OperIs(Args... opers)
    {
        return ScevOperIs(Oper, opers...);
    }

    bool TypeIs(var_types type)
    {
        return Type == type;
    }

    bool GetConstantValue(Compiler* comp, int64_t* cns);
};

struct ScevConstant : Scev
{
    ScevConstant(var_types type, int64_t value) : Scev(ScevOper::Constant, type), Value(value)
    {
    }

    int64_t Value;
};

struct ScevLocal : Scev
{
    ScevLocal(var_types type, unsigned lclNum, unsigned ssaNum)
        : Scev(ScevOper::Local, type), LclNum(lclNum), SsaNum(ssaNum)
    {
    }

    const unsigned LclNum;
    const unsigned SsaNum;

    bool GetConstantValue(Compiler* comp, int64_t* cns);
};

struct ScevUnop : Scev
{
    ScevUnop(ScevOper oper, var_types type, Scev* op1) : Scev(oper, type), Op1(op1)
    {
    }

    Scev* const Op1;
};

struct ScevBinop : ScevUnop
{
    ScevBinop(ScevOper oper, var_types type, Scev* op1, Scev* op2) : ScevUnop(oper, type, op1), Op2(op2)
    {
    }

    Scev* const Op2;
};

// Represents a value that evolves by an add recurrence.
// The value at iteration N is Start + N * Step.
// "Start" and "Step" are guaranteed to be invariant in "Loop".
struct ScevAddRec : Scev
{
    ScevAddRec(var_types type, Scev* start, Scev* step) : Scev(ScevOper::AddRec, type), Start(start), Step(step)
    {
    }

    Scev* const Start;
    Scev* const Step;
};

typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, Scev*> ScalarEvolutionMap;

// Scalar evolution is analyzed in the context of a single loop, and are
// computed on-demand by the use of the "Analyze" method on this class, which
// also maintains a cache.
class ScalarEvolutionContext
{
    Compiler*             m_comp;
    FlowGraphNaturalLoop* m_loop = nullptr;
    ScalarEvolutionMap    m_cache;

    Scev* Analyze(BasicBlock* block, GenTree* tree, int depth);
    Scev* AnalyzeNew(BasicBlock* block, GenTree* tree, int depth);
    Scev* CreateSimpleAddRec(GenTreeLclVarCommon* headerStore,
                             ScevLocal*           start,
                             BasicBlock*          stepDefBlock,
                             GenTree*             stepDefData);
    Scev* CreateSimpleInvariantScev(GenTree* tree);
    Scev* CreateScevForConstant(GenTreeIntConCommon* tree);

public:
    ScalarEvolutionContext(Compiler* comp) : m_comp(comp), m_cache(comp->getAllocator(CMK_LoopIVOpts))
    {
    }

    void ResetForLoop(FlowGraphNaturalLoop* loop);
    void DumpScev(Scev* scev);

    //------------------------------------------------------------------------
    // NewConstant: Create a SCEV node that represents a constant.
    //
    // Returns:
    //   The new node.
    //
    ScevConstant* NewConstant(var_types type, int64_t value)
    {
        ScevConstant* constant = new (m_comp, CMK_LoopIVOpts) ScevConstant(type, value);
        return constant;
    }

    //------------------------------------------------------------------------
    // NewLocal: Create a SCEV node that represents an invariant local (i.e. a
    // use of an SSA def from outside the loop).
    //
    // Parameters:
    //   lclNum - The local
    //   ssaNum - The SSA number of the def outside the loop that is being used.
    //
    // Returns:
    //   The new node.
    //
    ScevLocal* NewLocal(unsigned lclNum, unsigned ssaNum)
    {
        var_types  type           = genActualType(m_comp->lvaGetDesc(lclNum));
        ScevLocal* invariantLocal = new (m_comp, CMK_LoopIVOpts) ScevLocal(type, lclNum, ssaNum);
        return invariantLocal;
    }

    //------------------------------------------------------------------------
    // NewExtension: Create a SCEV node that represents a zero or sign extension.
    //
    // Parameters:
    //   oper       - The operation (ScevOper::ZeroExtend or ScevOper::SignExtend)
    //   targetType - The target type of the extension
    //   op         - The operand being extended.
    //
    // Returns:
    //   The new node.
    //
    ScevUnop* NewExtension(ScevOper oper, var_types targetType, Scev* op)
    {
        assert(op != nullptr);
        ScevUnop* ext = new (m_comp, CMK_LoopIVOpts) ScevUnop(oper, targetType, op);
        return ext;
    }

    //------------------------------------------------------------------------
    // NewBinop: Create a SCEV node that represents a binary operation.
    //
    // Parameters:
    //   oper - The operation
    //   op1  - First operand
    //   op2  - Second operand
    //
    // Returns:
    //   The new node.
    //
    ScevBinop* NewBinop(ScevOper oper, Scev* op1, Scev* op2)
    {
        assert((op1 != nullptr) && (op2 != nullptr));
        ScevBinop* binop = new (m_comp, CMK_LoopIVOpts) ScevBinop(oper, op1->Type, op1, op2);
        return binop;
    }

    //------------------------------------------------------------------------
    // NewAddRec: Create a SCEV node that represents a new add recurrence.
    //
    // Parameters:
    //   start - Value of the recurrence at the first iteration
    //   step  - Step value of the recurrence
    //
    // Returns:
    //   The new node.
    //
    ScevAddRec* NewAddRec(Scev* start, Scev* step)
    {
        assert((start != nullptr) && (step != nullptr));
        ScevAddRec* addRec = new (m_comp, CMK_LoopIVOpts) ScevAddRec(start->Type, start, step);
        return addRec;
    }

    Scev* Analyze(BasicBlock* block, GenTree* tree);
    Scev* Simplify(Scev* scev);
};
