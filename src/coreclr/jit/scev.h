// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// This file contains the definition of the scalar evolution IR. This IR allows
// representing the values of IR nodes inside loops in a closed form, taking
// into account that they are changing on each loop iteration. The IR is based
// around the following possible operations. At the core is ScevOper::AddRec,
// which represents a value that evolves by an add recurrence. In dumps it is
// described by <loop, start, step> where "loop" is the loop the value is
// evolving in, "start" is the initial value and "step" is the step by which
// the value evolves in every iteration.
//
// See scev.cpp for further documentation.
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

#ifdef DEBUG
    void Dump(Compiler* comp);
#endif
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
    ScevAddRec(var_types type, Scev* start, Scev* step DEBUGARG(FlowGraphNaturalLoop* loop))
        : Scev(ScevOper::AddRec, type), Start(start), Step(step) DEBUGARG(Loop(loop))
    {
    }

    Scev* const Start;
    Scev* const Step;
    INDEBUG(FlowGraphNaturalLoop* const Loop);
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
    ScalarEvolutionContext(Compiler* comp);

    void ResetForLoop(FlowGraphNaturalLoop* loop);

    ScevConstant* NewConstant(var_types type, int64_t value);
    ScevLocal* NewLocal(unsigned lclNum, unsigned ssaNum);
    ScevUnop* NewExtension(ScevOper oper, var_types targetType, Scev* op);
    ScevBinop* NewBinop(ScevOper oper, Scev* op1, Scev* op2);
    ScevAddRec* NewAddRec(Scev* start, Scev* step);

    Scev* Analyze(BasicBlock* block, GenTree* tree);
    Scev* Simplify(Scev* scev);
};
