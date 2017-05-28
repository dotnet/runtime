// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           ValueNum                                        XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "valuenum.h"
#include "ssaconfig.h"

VNFunc GetVNFuncForOper(genTreeOps oper, bool isUnsigned)
{
    if (!isUnsigned || (oper == GT_EQ) || (oper == GT_NE))
    {
        return VNFunc(oper);
    }
    switch (oper)
    {
        case GT_LT:
            return VNF_LT_UN;
        case GT_LE:
            return VNF_LE_UN;
        case GT_GE:
            return VNF_GE_UN;
        case GT_GT:
            return VNF_GT_UN;
        case GT_ADD:
            return VNF_ADD_UN;
        case GT_SUB:
            return VNF_SUB_UN;
        case GT_MUL:
            return VNF_MUL_UN;
        case GT_DIV:
            return VNF_DIV_UN;
        case GT_MOD:
            return VNF_MOD_UN;

        case GT_NOP:
        case GT_COMMA:
            return VNFunc(oper);
        default:
            unreached();
    }
}

ValueNumStore::ValueNumStore(Compiler* comp, IAllocator* alloc)
    : m_pComp(comp)
    , m_alloc(alloc)
    ,
#ifdef DEBUG
    m_numMapSels(0)
    ,
#endif
    m_nextChunkBase(0)
    , m_fixedPointMapSels(alloc, 8)
    , m_checkedBoundVNs(comp)
    , m_chunks(alloc, 8)
    , m_intCnsMap(nullptr)
    , m_longCnsMap(nullptr)
    , m_handleMap(nullptr)
    , m_floatCnsMap(nullptr)
    , m_doubleCnsMap(nullptr)
    , m_byrefCnsMap(nullptr)
    , m_VNFunc0Map(nullptr)
    , m_VNFunc1Map(nullptr)
    , m_VNFunc2Map(nullptr)
    , m_VNFunc3Map(nullptr)
    , m_VNFunc4Map(nullptr)
{
    // We have no current allocation chunks.
    for (unsigned i = 0; i < TYP_COUNT; i++)
    {
        for (unsigned j = CEA_None; j <= CEA_Count + MAX_LOOP_NUM; j++)
        {
            m_curAllocChunk[i][j] = NoChunk;
        }
    }

    for (unsigned i = 0; i < SmallIntConstNum; i++)
    {
        m_VNsForSmallIntConsts[i] = NoVN;
    }
    // We will reserve chunk 0 to hold some special constants, like the constant NULL, the "exception" value, and the
    // "zero map."
    Chunk* specialConstChunk = new (m_alloc) Chunk(m_alloc, &m_nextChunkBase, TYP_REF, CEA_Const, MAX_LOOP_NUM);
    specialConstChunk->m_numUsed +=
        SRC_NumSpecialRefConsts; // Implicitly allocate 0 ==> NULL, and 1 ==> Exception, 2 ==> ZeroMap.
    ChunkNum cn = m_chunks.Push(specialConstChunk);
    assert(cn == 0);

    m_mapSelectBudget = JitConfig.JitVNMapSelBudget();
}

// static.
template <typename T>
T ValueNumStore::EvalOp(VNFunc vnf, T v0)
{
    genTreeOps oper = genTreeOps(vnf);

    // Here we handle those unary ops that are the same for integral and floating-point types.
    switch (oper)
    {
        case GT_NEG:
            return -v0;
        default:
            // Must be int-specific
            return EvalOpIntegral(vnf, v0);
    }
}

template <typename T>
T ValueNumStore::EvalOpIntegral(VNFunc vnf, T v0)
{
    genTreeOps oper = genTreeOps(vnf);

    // Here we handle unary ops that are the same for all integral types.
    switch (oper)
    {
        case GT_NOT:
            return ~v0;
        default:
            unreached();
    }
}

// static
template <typename T>
T ValueNumStore::EvalOp(VNFunc vnf, T v0, T v1, ValueNum* pExcSet)
{
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);
        // Here we handle those that are the same for integral and floating-point types.
        switch (oper)
        {
            case GT_ADD:
                return v0 + v1;
            case GT_SUB:
                return v0 - v1;
            case GT_MUL:
                return v0 * v1;
            case GT_DIV:
                if (IsIntZero(v1))
                {
                    *pExcSet = VNExcSetSingleton(VNForFunc(TYP_REF, VNF_DivideByZeroExc));
                    return (T)0;
                }
                if (IsOverflowIntDiv(v0, v1))
                {
                    *pExcSet = VNExcSetSingleton(VNForFunc(TYP_REF, VNF_ArithmeticExc));
                    return (T)0;
                }
                else
                {
                    return v0 / v1;
                }

            default:
                // Must be int-specific
                return EvalOpIntegral(vnf, v0, v1, pExcSet);
        }
    }
    else // must be a VNF_ function
    {
        typedef typename jitstd::make_unsigned<T>::type UT;
        switch (vnf)
        {
            case VNF_GT_UN:
                return T(UT(v0) > UT(v1));
            case VNF_GE_UN:
                return T(UT(v0) >= UT(v1));
            case VNF_LT_UN:
                return T(UT(v0) < UT(v1));
            case VNF_LE_UN:
                return T(UT(v0) <= UT(v1));
            case VNF_ADD_UN:
                return T(UT(v0) + UT(v1));
            case VNF_SUB_UN:
                return T(UT(v0) - UT(v1));
            case VNF_MUL_UN:
                return T(UT(v0) * UT(v1));
            case VNF_DIV_UN:
                if (IsIntZero(v1))
                {
                    *pExcSet = VNExcSetSingleton(VNForFunc(TYP_REF, VNF_DivideByZeroExc));
                    return (T)0;
                }
                else
                {
                    return T(UT(v0) / UT(v1));
                }
            default:
                // Must be int-specific
                return EvalOpIntegral(vnf, v0, v1, pExcSet);
        }
    }
}

struct FloatTraits
{
    static float NaN()
    {
        unsigned bits = 0xFFC00000u;
        float    result;
        static_assert(sizeof(bits) == sizeof(result), "sizeof(unsigned) must equal sizeof(float)");
        memcpy(&result, &bits, sizeof(result));
        return result;
    }
};

struct DoubleTraits
{
    static double NaN()
    {
        unsigned long long bits = 0xFFF8000000000000ull;
        double             result;
        static_assert(sizeof(bits) == sizeof(result), "sizeof(unsigned long long) must equal sizeof(double)");
        memcpy(&result, &bits, sizeof(result));
        return result;
    }
};

template <typename TFp, typename TFpTraits>
TFp FpRem(TFp dividend, TFp divisor)
{
    // From the ECMA standard:
    //
    // If [divisor] is zero or [dividend] is infinity
    //   the result is NaN.
    // If [divisor] is infinity,
    //   the result is [dividend]

    if (divisor == 0 || !_finite(dividend))
    {
        return TFpTraits::NaN();
    }
    else if (!_finite(divisor) && !_isnan(divisor))
    {
        return dividend;
    }

    return (TFp)fmod((double)dividend, (double)divisor);
}

// Specialize for double for floating operations, that doesn't involve unsigned.
template <>
double ValueNumStore::EvalOp<double>(VNFunc vnf, double v0, double v1, ValueNum* pExcSet)
{
    genTreeOps oper = genTreeOps(vnf);
    // Here we handle those that are the same for floating-point types.
    switch (oper)
    {
        case GT_ADD:
            return v0 + v1;
        case GT_SUB:
            return v0 - v1;
        case GT_MUL:
            return v0 * v1;
        case GT_DIV:
            return v0 / v1;
        case GT_MOD:
            return FpRem<double, DoubleTraits>(v0, v1);

        default:
            unreached();
    }
}

// Specialize for float for floating operations, that doesn't involve unsigned.
template <>
float ValueNumStore::EvalOp<float>(VNFunc vnf, float v0, float v1, ValueNum* pExcSet)
{
    genTreeOps oper = genTreeOps(vnf);
    // Here we handle those that are the same for floating-point types.
    switch (oper)
    {
        case GT_ADD:
            return v0 + v1;
        case GT_SUB:
            return v0 - v1;
        case GT_MUL:
            return v0 * v1;
        case GT_DIV:
            return v0 / v1;
        case GT_MOD:
            return FpRem<float, FloatTraits>(v0, v1);

        default:
            unreached();
    }
}

template <typename T>
int ValueNumStore::EvalComparison(VNFunc vnf, T v0, T v1)
{
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);
        // Here we handle those that are the same for floating-point types.
        switch (oper)
        {
            case GT_EQ:
                return v0 == v1;
            case GT_NE:
                return v0 != v1;
            case GT_GT:
                return v0 > v1;
            case GT_GE:
                return v0 >= v1;
            case GT_LT:
                return v0 < v1;
            case GT_LE:
                return v0 <= v1;
            default:
                unreached();
        }
    }
    else // must be a VNF_ function
    {
        switch (vnf)
        {
            case VNF_GT_UN:
                return unsigned(v0) > unsigned(v1);
            case VNF_GE_UN:
                return unsigned(v0) >= unsigned(v1);
            case VNF_LT_UN:
                return unsigned(v0) < unsigned(v1);
            case VNF_LE_UN:
                return unsigned(v0) <= unsigned(v1);
            default:
                unreached();
        }
    }
}

/* static */
template <typename T>
int ValueNumStore::EvalOrderedComparisonFloat(VNFunc vnf, T v0, T v1)
{
    // !! NOTE !!
    //
    // All comparisons below are ordered comparisons.
    //
    // We should guard this function from unordered comparisons
    // identified by the GTF_RELOP_NAN_UN flag. Either the flag
    // should be bubbled (similar to GTF_UNSIGNED for ints)
    // to this point or we should bail much earlier if any of
    // the operands are NaN.
    //
    genTreeOps oper = genTreeOps(vnf);
    // Here we handle those that are the same for floating-point types.
    switch (oper)
    {
        case GT_EQ:
            return v0 == v1;
        case GT_NE:
            return v0 != v1;
        case GT_GT:
            return v0 > v1;
        case GT_GE:
            return v0 >= v1;
        case GT_LT:
            return v0 < v1;
        case GT_LE:
            return v0 <= v1;
        default:
            unreached();
    }
}

template <>
int ValueNumStore::EvalComparison<double>(VNFunc vnf, double v0, double v1)
{
    return EvalOrderedComparisonFloat(vnf, v0, v1);
}

template <>
int ValueNumStore::EvalComparison<float>(VNFunc vnf, float v0, float v1)
{
    return EvalOrderedComparisonFloat(vnf, v0, v1);
}

template <typename T>
T ValueNumStore::EvalOpIntegral(VNFunc vnf, T v0, T v1, ValueNum* pExcSet)
{
    genTreeOps oper = genTreeOps(vnf);
    switch (oper)
    {
        case GT_EQ:
            return v0 == v1;
        case GT_NE:
            return v0 != v1;
        case GT_GT:
            return v0 > v1;
        case GT_GE:
            return v0 >= v1;
        case GT_LT:
            return v0 < v1;
        case GT_LE:
            return v0 <= v1;
        case GT_OR:
            return v0 | v1;
        case GT_XOR:
            return v0 ^ v1;
        case GT_AND:
            return v0 & v1;
        case GT_LSH:
            return v0 << v1;
        case GT_RSH:
            return v0 >> v1;
        case GT_RSZ:
            if (sizeof(T) == 8)
            {
                return UINT64(v0) >> v1;
            }
            else
            {
                return UINT32(v0) >> v1;
            }
        case GT_ROL:
            if (sizeof(T) == 8)
            {
                return (v0 << v1) | (UINT64(v0) >> (64 - v1));
            }
            else
            {
                return (v0 << v1) | (UINT32(v0) >> (32 - v1));
            }

        case GT_ROR:
            if (sizeof(T) == 8)
            {
                return (v0 << (64 - v1)) | (UINT64(v0) >> v1);
            }
            else
            {
                return (v0 << (32 - v1)) | (UINT32(v0) >> v1);
            }

        case GT_DIV:
        case GT_MOD:
            if (v1 == 0)
            {
                *pExcSet = VNExcSetSingleton(VNForFunc(TYP_REF, VNF_DivideByZeroExc));
            }
            else if (IsOverflowIntDiv(v0, v1))
            {
                *pExcSet = VNExcSetSingleton(VNForFunc(TYP_REF, VNF_ArithmeticExc));
                return 0;
            }
            else // We are not dividing by Zero, so we can calculate the exact result.
            {
                // Perform the appropriate operation.
                if (oper == GT_DIV)
                {
                    return v0 / v1;
                }
                else // Must be GT_MOD
                {
                    return v0 % v1;
                }
            }

        case GT_UDIV:
        case GT_UMOD:
            if (v1 == 0)
            {
                *pExcSet = VNExcSetSingleton(VNForFunc(TYP_REF, VNF_DivideByZeroExc));
                return 0;
            }
            else // We are not dividing by Zero, so we can calculate the exact result.
            {
                typedef typename jitstd::make_unsigned<T>::type UT;
                // We need for force the source operands for the divide or mod operation
                // to be considered unsigned.
                //
                if (oper == GT_UDIV)
                {
                    // This is return unsigned(v0) / unsigned(v1) for both sizes of integers
                    return T(UT(v0) / UT(v1));
                }
                else // Must be GT_UMOD
                {
                    // This is return unsigned(v0) % unsigned(v1) for both sizes of integers
                    return T(UT(v0) % UT(v1));
                }
            }
        default:
            unreached(); // NYI?
    }
}

ValueNum ValueNumStore::VNExcSetSingleton(ValueNum x)
{
    ValueNum res = VNForFunc(TYP_REF, VNF_ExcSetCons, x, VNForEmptyExcSet());
#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    " STR_VN "%x = singleton exc set", res);
        vnDump(m_pComp, x);
        printf("\n");
    }
#endif
    return res;
}

ValueNumPair ValueNumStore::VNPExcSetSingleton(ValueNumPair xp)
{
    return ValueNumPair(VNExcSetSingleton(xp.GetLiberal()), VNExcSetSingleton(xp.GetConservative()));
}

ValueNum ValueNumStore::VNExcSetUnion(ValueNum xs0, ValueNum xs1 DEBUGARG(bool topLevel))
{
    if (xs0 == VNForEmptyExcSet())
    {
        return xs1;
    }
    else if (xs1 == VNForEmptyExcSet())
    {
        return xs0;
    }
    else
    {
        VNFuncApp funcXs0;
        bool      b0 = GetVNFunc(xs0, &funcXs0);
        assert(b0 && funcXs0.m_func == VNF_ExcSetCons); // Precondition: xs0 is an exception set.
        VNFuncApp funcXs1;
        bool      b1 = GetVNFunc(xs1, &funcXs1);
        assert(b1 && funcXs1.m_func == VNF_ExcSetCons); // Precondition: xs1 is an exception set.
        ValueNum res = NoVN;
        if (funcXs0.m_args[0] < funcXs1.m_args[0])
        {
            res = VNForFunc(TYP_REF, VNF_ExcSetCons, funcXs0.m_args[0],
                            VNExcSetUnion(funcXs0.m_args[1], xs1 DEBUGARG(false)));
        }
        else if (funcXs0.m_args[0] == funcXs1.m_args[0])
        {
            // Equal elements; only add one to the result.
            res = VNExcSetUnion(funcXs0.m_args[1], xs1);
        }
        else
        {
            assert(funcXs0.m_args[0] > funcXs1.m_args[0]);
            res = VNForFunc(TYP_REF, VNF_ExcSetCons, funcXs1.m_args[0],
                            VNExcSetUnion(xs0, funcXs1.m_args[1] DEBUGARG(false)));
        }

        return res;
    }
}

ValueNumPair ValueNumStore::VNPExcSetUnion(ValueNumPair xs0vnp, ValueNumPair xs1vnp)
{
    return ValueNumPair(VNExcSetUnion(xs0vnp.GetLiberal(), xs1vnp.GetLiberal()),
                        VNExcSetUnion(xs0vnp.GetConservative(), xs1vnp.GetConservative()));
}

void ValueNumStore::VNUnpackExc(ValueNum vnWx, ValueNum* pvn, ValueNum* pvnx)
{
    assert(vnWx != NoVN);
    VNFuncApp funcApp;
    if (GetVNFunc(vnWx, &funcApp) && funcApp.m_func == VNF_ValWithExc)
    {
        *pvn  = funcApp.m_args[0];
        *pvnx = funcApp.m_args[1];
    }
    else
    {
        *pvn = vnWx;
    }
}

void ValueNumStore::VNPUnpackExc(ValueNumPair vnWx, ValueNumPair* pvn, ValueNumPair* pvnx)
{
    VNUnpackExc(vnWx.GetLiberal(), pvn->GetLiberalAddr(), pvnx->GetLiberalAddr());
    VNUnpackExc(vnWx.GetConservative(), pvn->GetConservativeAddr(), pvnx->GetConservativeAddr());
}

ValueNum ValueNumStore::VNNormVal(ValueNum vn)
{
    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp) && funcApp.m_func == VNF_ValWithExc)
    {
        return funcApp.m_args[0];
    }
    else
    {
        return vn;
    }
}

ValueNumPair ValueNumStore::VNPNormVal(ValueNumPair vnp)
{
    return ValueNumPair(VNNormVal(vnp.GetLiberal()), VNNormVal(vnp.GetConservative()));
}

ValueNum ValueNumStore::VNExcVal(ValueNum vn)
{
    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp) && funcApp.m_func == VNF_ValWithExc)
    {
        return funcApp.m_args[1];
    }
    else
    {
        return VNForEmptyExcSet();
    }
}

ValueNumPair ValueNumStore::VNPExcVal(ValueNumPair vnp)
{
    return ValueNumPair(VNExcVal(vnp.GetLiberal()), VNExcVal(vnp.GetConservative()));
}

// If vn "excSet" is not "VNForEmptyExcSet()", return "VNF_ValWithExc(vn, excSet)".  Otherwise,
// just return "vn".
ValueNum ValueNumStore::VNWithExc(ValueNum vn, ValueNum excSet)
{
    if (excSet == VNForEmptyExcSet())
    {
        return vn;
    }
    else
    {
        ValueNum vnNorm;
        ValueNum vnX = VNForEmptyExcSet();
        VNUnpackExc(vn, &vnNorm, &vnX);
        return VNForFunc(TypeOfVN(vnNorm), VNF_ValWithExc, vnNorm, VNExcSetUnion(vnX, excSet));
    }
}

ValueNumPair ValueNumStore::VNPWithExc(ValueNumPair vnp, ValueNumPair excSetVNP)
{
    return ValueNumPair(VNWithExc(vnp.GetLiberal(), excSetVNP.GetLiberal()),
                        VNWithExc(vnp.GetConservative(), excSetVNP.GetConservative()));
}

bool ValueNumStore::IsKnownNonNull(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    VNFuncApp funcAttr;
    return GetVNFunc(vn, &funcAttr) && (s_vnfOpAttribs[funcAttr.m_func] & VNFOA_KnownNonNull) != 0;
}

bool ValueNumStore::IsSharedStatic(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    VNFuncApp funcAttr;
    return GetVNFunc(vn, &funcAttr) && (s_vnfOpAttribs[funcAttr.m_func] & VNFOA_SharedStatic) != 0;
}

ValueNumStore::Chunk::Chunk(
    IAllocator* alloc, ValueNum* pNextBaseVN, var_types typ, ChunkExtraAttribs attribs, BasicBlock::loopNumber loopNum)
    : m_defs(nullptr), m_numUsed(0), m_baseVN(*pNextBaseVN), m_typ(typ), m_attribs(attribs), m_loopNum(loopNum)
{
    // Allocate "m_defs" here, according to the typ/attribs pair.
    switch (attribs)
    {
        case CEA_None:
        case CEA_NotAField:
            break; // Nothing to do.
        case CEA_Const:
            switch (typ)
            {
                case TYP_INT:
                    m_defs = new (alloc) Alloc<TYP_INT>::Type[ChunkSize];
                    break;
                case TYP_FLOAT:
                    m_defs = new (alloc) Alloc<TYP_FLOAT>::Type[ChunkSize];
                    break;
                case TYP_LONG:
                    m_defs = new (alloc) Alloc<TYP_LONG>::Type[ChunkSize];
                    break;
                case TYP_DOUBLE:
                    m_defs = new (alloc) Alloc<TYP_DOUBLE>::Type[ChunkSize];
                    break;
                case TYP_BYREF:
                    m_defs = new (alloc) Alloc<TYP_BYREF>::Type[ChunkSize];
                    break;
                case TYP_REF:
                    // We allocate space for a single REF constant, NULL, so we can access these values uniformly.
                    // Since this value is always the same, we represent it as a static.
                    m_defs = &s_specialRefConsts[0];
                    break; // Nothing to do.
                default:
                    assert(false); // Should not reach here.
            }
            break;

        case CEA_Handle:
            m_defs = new (alloc) VNHandle[ChunkSize];
            break;

        case CEA_Func0:
            m_defs = new (alloc) VNFunc[ChunkSize];
            break;

        case CEA_Func1:
            m_defs = new (alloc) VNDefFunc1Arg[ChunkSize];
            break;
        case CEA_Func2:
            m_defs = new (alloc) VNDefFunc2Arg[ChunkSize];
            break;
        case CEA_Func3:
            m_defs = new (alloc) VNDefFunc3Arg[ChunkSize];
            break;
        case CEA_Func4:
            m_defs = new (alloc) VNDefFunc4Arg[ChunkSize];
            break;
        default:
            unreached();
    }
    *pNextBaseVN += ChunkSize;
}

ValueNumStore::Chunk* ValueNumStore::GetAllocChunk(var_types              typ,
                                                   ChunkExtraAttribs      attribs,
                                                   BasicBlock::loopNumber loopNum)
{
    Chunk*   res;
    unsigned index;
    if (loopNum == MAX_LOOP_NUM)
    {
        // Loop nest is unknown/irrelevant for this VN.
        index = attribs;
    }
    else
    {
        // Loop nest is interesting.  Since we know this is only true for unique VNs, we know attribs will
        // be CEA_None and can just index based on loop number.
        noway_assert(attribs == CEA_None);
        // Map NOT_IN_LOOP -> MAX_LOOP_NUM to make the index range contiguous [0..MAX_LOOP_NUM]
        index = CEA_Count + (loopNum == BasicBlock::NOT_IN_LOOP ? MAX_LOOP_NUM : loopNum);
    }
    ChunkNum cn = m_curAllocChunk[typ][index];
    if (cn != NoChunk)
    {
        res = m_chunks.Get(cn);
        if (res->m_numUsed < ChunkSize)
        {
            return res;
        }
    }
    // Otherwise, must allocate a new one.
    res                         = new (m_alloc) Chunk(m_alloc, &m_nextChunkBase, typ, attribs, loopNum);
    cn                          = m_chunks.Push(res);
    m_curAllocChunk[typ][index] = cn;
    return res;
}

ValueNum ValueNumStore::VNForIntCon(INT32 cnsVal)
{
    if (IsSmallIntConst(cnsVal))
    {
        unsigned ind = cnsVal - SmallIntConstMin;
        ValueNum vn  = m_VNsForSmallIntConsts[ind];
        if (vn != NoVN)
        {
            return vn;
        }
        vn                          = GetVNForIntCon(cnsVal);
        m_VNsForSmallIntConsts[ind] = vn;
        return vn;
    }
    else
    {
        return GetVNForIntCon(cnsVal);
    }
}

ValueNum ValueNumStore::VNForLongCon(INT64 cnsVal)
{
    ValueNum res;
    if (GetLongCnsMap()->Lookup(cnsVal, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                             = GetAllocChunk(TYP_LONG, CEA_Const);
        unsigned offsetWithinChunk                             = c->AllocVN();
        res                                                    = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<INT64*>(c->m_defs)[offsetWithinChunk] = cnsVal;
        GetLongCnsMap()->Set(cnsVal, res);
        return res;
    }
}

ValueNum ValueNumStore::VNForFloatCon(float cnsVal)
{
    ValueNum res;
    if (GetFloatCnsMap()->Lookup(cnsVal, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                             = GetAllocChunk(TYP_FLOAT, CEA_Const);
        unsigned offsetWithinChunk                             = c->AllocVN();
        res                                                    = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<float*>(c->m_defs)[offsetWithinChunk] = cnsVal;
        GetFloatCnsMap()->Set(cnsVal, res);
        return res;
    }
}

ValueNum ValueNumStore::VNForDoubleCon(double cnsVal)
{
    ValueNum res;
    if (GetDoubleCnsMap()->Lookup(cnsVal, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                              = GetAllocChunk(TYP_DOUBLE, CEA_Const);
        unsigned offsetWithinChunk                              = c->AllocVN();
        res                                                     = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<double*>(c->m_defs)[offsetWithinChunk] = cnsVal;
        GetDoubleCnsMap()->Set(cnsVal, res);
        return res;
    }
}

ValueNum ValueNumStore::VNForByrefCon(INT64 cnsVal)
{
    ValueNum res;
    if (GetByrefCnsMap()->Lookup(cnsVal, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                             = GetAllocChunk(TYP_BYREF, CEA_Const);
        unsigned offsetWithinChunk                             = c->AllocVN();
        res                                                    = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<INT64*>(c->m_defs)[offsetWithinChunk] = cnsVal;
        GetByrefCnsMap()->Set(cnsVal, res);
        return res;
    }
}

ValueNum ValueNumStore::VNForCastOper(var_types castToType, bool srcIsUnsigned /*=false*/)
{
    assert(castToType != TYP_STRUCT);
    INT32 cnsVal = INT32(castToType) << INT32(VCA_BitCount);
    assert((cnsVal & INT32(VCA_ReservedBits)) == 0);

    if (srcIsUnsigned)
    {
        // We record the srcIsUnsigned by or-ing a 0x01
        cnsVal |= INT32(VCA_UnsignedSrc);
    }
    ValueNum result = VNForIntCon(cnsVal);

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForCastOper(%s%s) is " STR_VN "%x\n", varTypeName(castToType),
               srcIsUnsigned ? ", unsignedSrc" : "", result);
    }
#endif

    return result;
}

ValueNum ValueNumStore::VNForHandle(ssize_t cnsVal, unsigned handleFlags)
{
    assert((handleFlags & ~GTF_ICON_HDL_MASK) == 0);

    ValueNum res;
    VNHandle handle;
    VNHandle::Initialize(&handle, cnsVal, handleFlags);
    if (GetHandleMap()->Lookup(handle, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                                = GetAllocChunk(TYP_I_IMPL, CEA_Handle);
        unsigned offsetWithinChunk                                = c->AllocVN();
        res                                                       = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNHandle*>(c->m_defs)[offsetWithinChunk] = handle;
        GetHandleMap()->Set(handle, res);
        return res;
    }
}

// Returns the value number for zero of the given "typ".
// It has an unreached() for a "typ" that has no zero value, such as TYP_VOID.
ValueNum ValueNumStore::VNZeroForType(var_types typ)
{
    switch (typ)
    {
        case TYP_BOOL:
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_CHAR:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
            return VNForIntCon(0);
        case TYP_LONG:
        case TYP_ULONG:
            return VNForLongCon(0);
        case TYP_FLOAT:
#if FEATURE_X87_DOUBLES
            return VNForDoubleCon(0.0);
#else
            return VNForFloatCon(0.0f);
#endif
        case TYP_DOUBLE:
            return VNForDoubleCon(0.0);
        case TYP_REF:
        case TYP_ARRAY:
            return VNForNull();
        case TYP_BYREF:
            return VNForByrefCon(0);
        case TYP_STRUCT:
#ifdef FEATURE_SIMD
        // TODO-CQ: Improve value numbering for SIMD types.
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
        case TYP_SIMD32:
#endif                             // FEATURE_SIMD
            return VNForZeroMap(); // Recursion!

        // These should be unreached.
        default:
            unreached(); // Should handle all types.
    }
}

// Returns the value number for one of the given "typ".
// It returns NoVN for a "typ" that has no one value, such as TYP_REF.
ValueNum ValueNumStore::VNOneForType(var_types typ)
{
    switch (typ)
    {
        case TYP_BOOL:
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_CHAR:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
            return VNForIntCon(1);
        case TYP_LONG:
        case TYP_ULONG:
            return VNForLongCon(1);
        case TYP_FLOAT:
            return VNForFloatCon(1.0f);
        case TYP_DOUBLE:
            return VNForDoubleCon(1.0);

        default:
            return NoVN;
    }
}

class Object* ValueNumStore::s_specialRefConsts[] = {nullptr, nullptr, nullptr};

// Nullary operators (i.e., symbolic constants).
ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func)
{
    assert(VNFuncArity(func) == 0);
    assert(func != VNF_NotAField);

    ValueNum res;

    if (GetVNFunc0Map()->Lookup(func, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                              = GetAllocChunk(typ, CEA_Func0);
        unsigned offsetWithinChunk                              = c->AllocVN();
        res                                                     = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNFunc*>(c->m_defs)[offsetWithinChunk] = func;
        GetVNFunc0Map()->Set(func, res);
        return res;
    }
}

ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func, ValueNum arg0VN)
{
    assert(arg0VN == VNNormVal(arg0VN)); // Arguments don't carry exceptions.

    ValueNum      res;
    VNDefFunc1Arg fstruct(func, arg0VN);

    // Do constant-folding.
    if (CanEvalForConstantArgs(func) && IsVNConstant(arg0VN))
    {
        return EvalFuncForConstantArgs(typ, func, arg0VN);
    }

    if (GetVNFunc1Map()->Lookup(fstruct, &res))
    {
        return res;
    }
    else
    {
        // Otherwise, create a new VN for this application.
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func1);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        res                                                            = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc1Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
        GetVNFunc1Map()->Set(fstruct, res);
        return res;
    }
}

// Windows x86 and Windows ARM/ARM64 may not define _isnanf() but they do define _isnan().
// We will redirect the macros to these other functions if the macro is not defined for the
// platform. This has the side effect of a possible implicit upcasting for arguments passed.
#if (defined(_TARGET_X86_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)) && !defined(FEATURE_PAL)

#if !defined(_isnanf)
#define _isnanf _isnan
#endif

#endif

ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(arg0VN != NoVN && arg1VN != NoVN);
    assert(arg0VN == VNNormVal(arg0VN)); // Arguments carry no exceptions.
    assert(arg1VN == VNNormVal(arg1VN)); // Arguments carry no exceptions.
    assert(VNFuncArity(func) == 2);
    assert(func != VNF_MapSelect); // Precondition: use the special function VNForMapSelect defined for that.

    ValueNum res;

    // Do constant-folding.
    if (CanEvalForConstantArgs(func) && IsVNConstant(arg0VN) && IsVNConstant(arg1VN))
    {
        bool canFold = true; // Normally we will be able to fold this 'func'

        // Special case for VNF_Cast of constant handles
        // Don't allow eval/fold of a GT_CAST(non-I_IMPL, Handle)
        //
        if ((func == VNF_Cast) && (typ != TYP_I_IMPL) && IsVNHandle(arg0VN))
        {
            canFold = false;
        }

        // It is possible for us to have mismatched types (see Bug 750863)
        // We don't try to fold a binary operation when one of the constant operands
        // is a floating-point constant and the other is not.
        //
        var_types arg0VNtyp      = TypeOfVN(arg0VN);
        bool      arg0IsFloating = varTypeIsFloating(arg0VNtyp);

        var_types arg1VNtyp      = TypeOfVN(arg1VN);
        bool      arg1IsFloating = varTypeIsFloating(arg1VNtyp);

        if (arg0IsFloating != arg1IsFloating)
        {
            canFold = false;
        }

        // NaNs are unordered wrt to other floats. While an ordered
        // comparison would return false, an unordered comparison
        // will return true if any operands are a NaN. We only perform
        // ordered NaN comparison in EvalComparison.
        if ((arg0IsFloating && (((arg0VNtyp == TYP_FLOAT) && _isnanf(GetConstantSingle(arg0VN))) ||
                                ((arg0VNtyp == TYP_DOUBLE) && _isnan(GetConstantDouble(arg0VN))))) ||
            (arg1IsFloating && (((arg1VNtyp == TYP_FLOAT) && _isnanf(GetConstantSingle(arg1VN))) ||
                                ((arg0VNtyp == TYP_DOUBLE) && _isnan(GetConstantDouble(arg1VN))))))
        {
            canFold = false;
        }

        if (canFold)
        {
            return EvalFuncForConstantArgs(typ, func, arg0VN, arg1VN);
        }
    }
    // We canonicalize commutative operations.
    // (Perhaps should eventually handle associative/commutative [AC] ops -- but that gets complicated...)
    if (VNFuncIsCommutative(func))
    {
        // Order arg0 arg1 by numerical VN value.
        if (arg0VN > arg1VN)
        {
            jitstd::swap(arg0VN, arg1VN);
        }
    }
    VNDefFunc2Arg fstruct(func, arg0VN, arg1VN);
    if (GetVNFunc2Map()->Lookup(fstruct, &res))
    {
        return res;
    }
    else
    {
        // We have ways of evaluating some binary functions.
        if (func < VNF_Boundary)
        {
            if (typ != TYP_BYREF) // We don't want/need to optimize a zero byref
            {
                ValueNum resultVN = NoVN;
                ValueNum ZeroVN, OneVN; // We may need to create one of these in the switch below.
                switch (genTreeOps(func))
                {
                    case GT_ADD:
                        // This identity does not apply for floating point (when x == -0.0)
                        if (!varTypeIsFloating(typ))
                        {
                            // (x + 0) == (0 + x) => x
                            ZeroVN = VNZeroForType(typ);
                            if (arg0VN == ZeroVN)
                            {
                                resultVN = arg1VN;
                            }
                            else if (arg1VN == ZeroVN)
                            {
                                resultVN = arg0VN;
                            }
                        }
                        break;

                    case GT_SUB:
                        // (x - 0) => x
                        ZeroVN = VNZeroForType(typ);
                        if (arg1VN == ZeroVN)
                        {
                            resultVN = arg0VN;
                        }
                        break;

                    case GT_MUL:
                        // (x * 1) == (1 * x) => x
                        OneVN = VNOneForType(typ);
                        if (OneVN != NoVN)
                        {
                            if (arg0VN == OneVN)
                            {
                                resultVN = arg1VN;
                            }
                            else if (arg1VN == OneVN)
                            {
                                resultVN = arg0VN;
                            }
                        }

                        if (!varTypeIsFloating(typ))
                        {
                            // (x * 0) == (0 * x) => 0 (unless x is NaN, which we must assume a fp value may be)
                            ZeroVN = VNZeroForType(typ);
                            if (arg0VN == ZeroVN)
                            {
                                resultVN = ZeroVN;
                            }
                            else if (arg1VN == ZeroVN)
                            {
                                resultVN = ZeroVN;
                            }
                        }
                        break;

                    case GT_DIV:
                    case GT_UDIV:
                        // (x / 1) => x
                        OneVN = VNOneForType(typ);
                        if (OneVN != NoVN)
                        {
                            if (arg1VN == OneVN)
                            {
                                resultVN = arg0VN;
                            }
                        }
                        break;

                    case GT_OR:
                    case GT_XOR:
                        // (x | 0) == (0 | x) => x
                        // (x ^ 0) == (0 ^ x) => x
                        ZeroVN = VNZeroForType(typ);
                        if (arg0VN == ZeroVN)
                        {
                            resultVN = arg1VN;
                        }
                        else if (arg1VN == ZeroVN)
                        {
                            resultVN = arg0VN;
                        }
                        break;

                    case GT_AND:
                        // (x & 0) == (0 & x) => 0
                        ZeroVN = VNZeroForType(typ);
                        if (arg0VN == ZeroVN)
                        {
                            resultVN = ZeroVN;
                        }
                        else if (arg1VN == ZeroVN)
                        {
                            resultVN = ZeroVN;
                        }
                        break;

                    case GT_LSH:
                    case GT_RSH:
                    case GT_RSZ:
                    case GT_ROL:
                    case GT_ROR:
                        // (x << 0) => x
                        // (x >> 0) => x
                        // (x rol 0) => x
                        // (x ror 0) => x
                        ZeroVN = VNZeroForType(typ);
                        if (arg1VN == ZeroVN)
                        {
                            resultVN = arg0VN;
                        }
                        break;

                    case GT_EQ:
                        // (x == x) => true (unless x is NaN)
                        if (!varTypeIsFloating(TypeOfVN(arg0VN)) && (arg0VN != NoVN) && (arg0VN == arg1VN))
                        {
                            resultVN = VNOneForType(typ);
                        }
                        if ((arg0VN == VNForNull() && IsKnownNonNull(arg1VN)) ||
                            (arg1VN == VNForNull() && IsKnownNonNull(arg0VN)))
                        {
                            resultVN = VNZeroForType(typ);
                        }
                        break;
                    case GT_NE:
                        // (x != x) => false (unless x is NaN)
                        if (!varTypeIsFloating(TypeOfVN(arg0VN)) && (arg0VN != NoVN) && (arg0VN == arg1VN))
                        {
                            resultVN = VNZeroForType(typ);
                        }
                        if ((arg0VN == VNForNull() && IsKnownNonNull(arg1VN)) ||
                            (arg1VN == VNForNull() && IsKnownNonNull(arg0VN)))
                        {
                            resultVN = VNOneForType(typ);
                        }
                        break;

                    default:
                        break;
                }

                if ((resultVN != NoVN) && (TypeOfVN(resultVN) == typ))
                {
                    return resultVN;
                }
            }
        }
        else // must be a VNF_ function
        {
            if (func == VNF_CastClass)
            {
                // In terms of values, a castclass always returns its second argument, the object being cast.
                // The IL operation may also throw an exception
                return VNWithExc(arg1VN, VNExcSetSingleton(VNForFunc(TYP_REF, VNF_InvalidCastExc, arg1VN, arg0VN)));
            }
        }

        // Otherwise, assign a new VN for the function application.
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func2);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        res                                                            = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc2Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
        GetVNFunc2Map()->Set(fstruct, res);
        return res;
    }
}

//------------------------------------------------------------------------------
// VNForMapStore : Evaluate VNF_MapStore with the given arguments.
//
//
// Arguments:
//    typ  -    Value type
//    arg0VN  - Map value number
//    arg1VN  - Index value number
//    arg2VN  - New value for map[index]
//
// Return Value:
//    Value number for the result of the evaluation.

ValueNum ValueNumStore::VNForMapStore(var_types typ, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN)
{
    ValueNum result = VNForFunc(typ, VNF_MapStore, arg0VN, arg1VN, arg2VN);
#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForMapStore(" STR_VN "%x, " STR_VN "%x, " STR_VN "%x):%s returns ", arg0VN, arg1VN, arg2VN,
               varTypeName(typ));
        m_pComp->vnPrint(result, 1);
        printf("\n");
    }
#endif
    return result;
}

//------------------------------------------------------------------------------
// VNForMapSelect : Evaluate VNF_MapSelect with the given arguments.
//
//
// Arguments:
//    vnk  -    Value number kind
//    typ  -    Value type
//    arg0VN  - Map value number
//    arg1VN  - Index value number
//
// Return Value:
//    Value number for the result of the evaluation.
//
// Notes:
//    This requires a "ValueNumKind" because it will attempt, given "select(phi(m1, ..., mk), ind)", to evaluate
//    "select(m1, ind)", ..., "select(mk, ind)" to see if they agree.  It needs to know which kind of value number
//    (liberal/conservative) to read from the SSA def referenced in the phi argument.

ValueNum ValueNumStore::VNForMapSelect(ValueNumKind vnk, var_types typ, ValueNum arg0VN, ValueNum arg1VN)
{
    unsigned budget          = m_mapSelectBudget;
    bool     usedRecursiveVN = false;
    ValueNum result          = VNForMapSelectWork(vnk, typ, arg0VN, arg1VN, &budget, &usedRecursiveVN);
#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForMapSelect(" STR_VN "%x, " STR_VN "%x):%s returns ", arg0VN, arg1VN, varTypeName(typ));
        m_pComp->vnPrint(result, 1);
        printf("\n");
    }
#endif
    return result;
}

//------------------------------------------------------------------------------
// VNForMapSelectWork : A method that does the work for VNForMapSelect and may call itself recursively.
//
//
// Arguments:
//    vnk  -             Value number kind
//    typ  -             Value type
//    arg0VN  -          Zeroth argument
//    arg1VN  -          First argument
//    pBudget -          Remaining budget for the outer evaluation
//    pUsedRecursiveVN - Out-parameter that is set to true iff RecursiveVN was returned from this method
//                       or from a method called during one of recursive invocations.
//
// Return Value:
//    Value number for the result of the evaluation.
//
// Notes:
//    This requires a "ValueNumKind" because it will attempt, given "select(phi(m1, ..., mk), ind)", to evaluate
//    "select(m1, ind)", ..., "select(mk, ind)" to see if they agree.  It needs to know which kind of value number
//    (liberal/conservative) to read from the SSA def referenced in the phi argument.

ValueNum ValueNumStore::VNForMapSelectWork(
    ValueNumKind vnk, var_types typ, ValueNum arg0VN, ValueNum arg1VN, unsigned* pBudget, bool* pUsedRecursiveVN)
{
TailCall:
    // This label allows us to directly implement a tail call by setting up the arguments, and doing a goto to here.
    assert(arg0VN != NoVN && arg1VN != NoVN);
    assert(arg0VN == VNNormVal(arg0VN)); // Arguments carry no exceptions.
    assert(arg1VN == VNNormVal(arg1VN)); // Arguments carry no exceptions.

    *pUsedRecursiveVN = false;

#ifdef DEBUG
    // Provide a mechanism for writing tests that ensure we don't call this ridiculously often.
    m_numMapSels++;
#if 1
// This printing is sometimes useful in debugging.
// if ((m_numMapSels % 1000) == 0) printf("%d VNF_MapSelect applications.\n", m_numMapSels);
#endif
    unsigned selLim = JitConfig.JitVNMapSelLimit();
    assert(selLim == 0 || m_numMapSels < selLim);
#endif
    ValueNum res;

    VNDefFunc2Arg fstruct(VNF_MapSelect, arg0VN, arg1VN);
    if (GetVNFunc2Map()->Lookup(fstruct, &res))
    {
        return res;
    }
    else
    {

        // Give up if we've run out of budget.
        if (--(*pBudget) == 0)
        {
            // We have to use 'nullptr' for the basic block here, because subsequent expressions
            // in different blocks may find this result in the VNFunc2Map -- other expressions in
            // the IR may "evaluate" to this same VNForExpr, so it is not "unique" in the sense
            // that permits the BasicBlock attribution.
            res = VNForExpr(nullptr, typ);
            GetVNFunc2Map()->Set(fstruct, res);
            return res;
        }

        // If it's recursive, stop the recursion.
        if (SelectIsBeingEvaluatedRecursively(arg0VN, arg1VN))
        {
            *pUsedRecursiveVN = true;
            return RecursiveVN;
        }

        if (arg0VN == VNForZeroMap())
        {
            return VNZeroForType(typ);
        }
        else if (IsVNFunc(arg0VN))
        {
            VNFuncApp funcApp;
            GetVNFunc(arg0VN, &funcApp);
            if (funcApp.m_func == VNF_MapStore)
            {
                // select(store(m, i, v), i) == v
                if (funcApp.m_args[1] == arg1VN)
                {
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP("      AX1: select([" STR_VN "%x]store(" STR_VN "%x, " STR_VN "%x, " STR_VN "%x), " STR_VN
                            "%x) ==> " STR_VN "%x.\n",
                            funcApp.m_args[0], arg0VN, funcApp.m_args[1], funcApp.m_args[2], arg1VN, funcApp.m_args[2]);
#endif
                    return funcApp.m_args[2];
                }
                // i # j ==> select(store(m, i, v), j) == select(m, j)
                // Currently the only source of distinctions is when both indices are constants.
                else if (IsVNConstant(arg1VN) && IsVNConstant(funcApp.m_args[1]))
                {
                    assert(funcApp.m_args[1] != arg1VN); // we already checked this above.
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP("      AX2: " STR_VN "%x != " STR_VN "%x ==> select([" STR_VN "%x]store(" STR_VN
                            "%x, " STR_VN "%x, " STR_VN "%x), " STR_VN "%x) ==> select(" STR_VN "%x, " STR_VN "%x).\n",
                            arg1VN, funcApp.m_args[1], arg0VN, funcApp.m_args[0], funcApp.m_args[1], funcApp.m_args[2],
                            arg1VN, funcApp.m_args[0], arg1VN);
#endif
                    // This is the equivalent of the recursive tail call:
                    // return VNForMapSelect(vnk, typ, funcApp.m_args[0], arg1VN);
                    // Make sure we capture any exceptions from the "i" and "v" of the store...
                    arg0VN = funcApp.m_args[0];
                    goto TailCall;
                }
            }
            else if (funcApp.m_func == VNF_PhiDef || funcApp.m_func == VNF_PhiMemoryDef)
            {
                unsigned  lclNum   = BAD_VAR_NUM;
                bool      isMemory = false;
                VNFuncApp phiFuncApp;
                bool      defArgIsFunc = false;
                if (funcApp.m_func == VNF_PhiDef)
                {
                    lclNum       = unsigned(funcApp.m_args[0]);
                    defArgIsFunc = GetVNFunc(funcApp.m_args[2], &phiFuncApp);
                }
                else
                {
                    assert(funcApp.m_func == VNF_PhiMemoryDef);
                    isMemory     = true;
                    defArgIsFunc = GetVNFunc(funcApp.m_args[1], &phiFuncApp);
                }
                if (defArgIsFunc && phiFuncApp.m_func == VNF_Phi)
                {
                    // select(phi(m1, m2), x): if select(m1, x) == select(m2, x), return that, else new fresh.
                    // Get the first argument of the phi.

                    // We need to be careful about breaking infinite recursion.  Record the outer select.
                    m_fixedPointMapSels.Push(VNDefFunc2Arg(VNF_MapSelect, arg0VN, arg1VN));

                    assert(IsVNConstant(phiFuncApp.m_args[0]));
                    unsigned phiArgSsaNum = ConstantValue<unsigned>(phiFuncApp.m_args[0]);
                    ValueNum phiArgVN;
                    if (isMemory)
                    {
                        phiArgVN = m_pComp->GetMemoryPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                    }
                    else
                    {
                        phiArgVN = m_pComp->lvaTable[lclNum].GetPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                    }
                    if (phiArgVN != ValueNumStore::NoVN)
                    {
                        bool     allSame = true;
                        ValueNum argRest = phiFuncApp.m_args[1];
                        ValueNum sameSelResult =
                            VNForMapSelectWork(vnk, typ, phiArgVN, arg1VN, pBudget, pUsedRecursiveVN);
                        while (allSame && argRest != ValueNumStore::NoVN)
                        {
                            ValueNum  cur = argRest;
                            VNFuncApp phiArgFuncApp;
                            if (GetVNFunc(argRest, &phiArgFuncApp) && phiArgFuncApp.m_func == VNF_Phi)
                            {
                                cur     = phiArgFuncApp.m_args[0];
                                argRest = phiArgFuncApp.m_args[1];
                            }
                            else
                            {
                                argRest = ValueNumStore::NoVN; // Cause the loop to terminate.
                            }
                            assert(IsVNConstant(cur));
                            phiArgSsaNum = ConstantValue<unsigned>(cur);
                            if (isMemory)
                            {
                                phiArgVN = m_pComp->GetMemoryPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                            }
                            else
                            {
                                phiArgVN = m_pComp->lvaTable[lclNum].GetPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                            }
                            if (phiArgVN == ValueNumStore::NoVN)
                            {
                                allSame = false;
                            }
                            else
                            {
                                bool     usedRecursiveVN = false;
                                ValueNum curResult =
                                    VNForMapSelectWork(vnk, typ, phiArgVN, arg1VN, pBudget, &usedRecursiveVN);
                                *pUsedRecursiveVN |= usedRecursiveVN;
                                if (sameSelResult == ValueNumStore::RecursiveVN)
                                {
                                    sameSelResult = curResult;
                                }
                                if (curResult != ValueNumStore::RecursiveVN && curResult != sameSelResult)
                                {
                                    allSame = false;
                                }
                            }
                        }
                        if (allSame && sameSelResult != ValueNumStore::RecursiveVN)
                        {
                            // Make sure we're popping what we pushed.
                            assert(FixedPointMapSelsTopHasValue(arg0VN, arg1VN));
                            m_fixedPointMapSels.Pop();

                            // To avoid exponential searches, we make sure that this result is memo-ized.
                            // The result is always valid for memoization if we didn't rely on RecursiveVN to get it.
                            // If RecursiveVN was used, we are processing a loop and we can't memo-ize this intermediate
                            // result if, e.g., this block is in a multi-entry loop.
                            if (!*pUsedRecursiveVN)
                            {
                                GetVNFunc2Map()->Set(fstruct, sameSelResult);
                            }

                            return sameSelResult;
                        }
                        // Otherwise, fall through to creating the select(phi(m1, m2), x) function application.
                    }
                    // Make sure we're popping what we pushed.
                    assert(FixedPointMapSelsTopHasValue(arg0VN, arg1VN));
                    m_fixedPointMapSels.Pop();
                }
            }
        }

        // Otherwise, assign a new VN for the function application.
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func2);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        res                                                            = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc2Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
        GetVNFunc2Map()->Set(fstruct, res);
        return res;
    }
}

ValueNum ValueNumStore::EvalFuncForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN)
{
    assert(CanEvalForConstantArgs(func));
    assert(IsVNConstant(arg0VN));
    switch (TypeOfVN(arg0VN))
    {
        case TYP_INT:
        {
            int resVal = EvalOp(func, ConstantValue<int>(arg0VN));
            // Unary op on a handle results in a handle.
            return IsVNHandle(arg0VN) ? VNForHandle(ssize_t(resVal), GetHandleFlags(arg0VN)) : VNForIntCon(resVal);
        }
        case TYP_LONG:
        {
            INT64 resVal = EvalOp(func, ConstantValue<INT64>(arg0VN));
            // Unary op on a handle results in a handle.
            return IsVNHandle(arg0VN) ? VNForHandle(ssize_t(resVal), GetHandleFlags(arg0VN)) : VNForLongCon(resVal);
        }
        case TYP_FLOAT:
            return VNForFloatCon(EvalOp(func, ConstantValue<float>(arg0VN)));
        case TYP_DOUBLE:
            return VNForDoubleCon(EvalOp(func, ConstantValue<double>(arg0VN)));
        case TYP_REF:
            // If arg0 has a possible exception, it wouldn't have been constant.
            assert(!VNHasExc(arg0VN));
            // Otherwise...
            assert(arg0VN == VNForNull());         // Only other REF constant.
            assert(func == VNFunc(GT_ARR_LENGTH)); // Only function we can apply to a REF constant!
            return VNWithExc(VNForVoid(), VNExcSetSingleton(VNForFunc(TYP_REF, VNF_NullPtrExc, VNForNull())));
        default:
            unreached();
    }
}

bool ValueNumStore::SelectIsBeingEvaluatedRecursively(ValueNum map, ValueNum ind)
{
    for (unsigned i = 0; i < m_fixedPointMapSels.Size(); i++)
    {
        VNDefFunc2Arg& elem = m_fixedPointMapSels.GetRef(i);
        assert(elem.m_func == VNF_MapSelect);
        if (elem.m_arg0 == map && elem.m_arg1 == ind)
        {
            return true;
        }
    }
    return false;
}

#ifdef DEBUG
bool ValueNumStore::FixedPointMapSelsTopHasValue(ValueNum map, ValueNum index)
{
    if (m_fixedPointMapSels.Size() == 0)
    {
        return false;
    }
    VNDefFunc2Arg& top = m_fixedPointMapSels.TopRef();
    return top.m_func == VNF_MapSelect && top.m_arg0 == map && top.m_arg1 == index;
}
#endif

// Given an integer constant value number return its value as an int.
//
int ValueNumStore::GetConstantInt32(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    var_types argVNtyp = TypeOfVN(argVN);

    int result = 0;

    switch (argVNtyp)
    {
        case TYP_INT:
            result = ConstantValue<int>(argVN);
            break;
#ifndef _TARGET_64BIT_
        case TYP_REF:
        case TYP_BYREF:
            result = (int)ConstantValue<size_t>(argVN);
            break;
#endif
        default:
            unreached();
    }
    return result;
}

// Given an integer constant value number return its value as an INT64.
//
INT64 ValueNumStore::GetConstantInt64(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    var_types argVNtyp = TypeOfVN(argVN);

    INT64 result = 0;

    switch (argVNtyp)
    {
        case TYP_INT:
            result = (INT64)ConstantValue<int>(argVN);
            break;
        case TYP_LONG:
            result = ConstantValue<INT64>(argVN);
            break;
        case TYP_REF:
        case TYP_BYREF:
            result = (INT64)ConstantValue<size_t>(argVN);
            break;
        default:
            unreached();
    }
    return result;
}

// Given a double constant value number return its value as a double.
//
double ValueNumStore::GetConstantDouble(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_DOUBLE);

    return ConstantValue<double>(argVN);
}

// Given a float constant value number return its value as a float.
//
float ValueNumStore::GetConstantSingle(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_FLOAT);

    return ConstantValue<float>(argVN);
}

// Compute the proper value number when the VNFunc has all constant arguments
// This essentially performs constant folding at value numbering time
//
ValueNum ValueNumStore::EvalFuncForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(CanEvalForConstantArgs(func));
    assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));
    assert(!VNHasExc(arg0VN) && !VNHasExc(arg1VN)); // Otherwise, would not be constant.

    // if our func is the VNF_Cast operation we handle it first
    if (func == VNF_Cast)
    {
        return EvalCastForConstantArgs(typ, func, arg0VN, arg1VN);
    }

    if (typ == TYP_BYREF)
    {
        // We don't want to fold expressions that produce TYP_BYREF
        return false;
    }

    var_types arg0VNtyp = TypeOfVN(arg0VN);
    var_types arg1VNtyp = TypeOfVN(arg1VN);

    // When both arguments are floating point types
    // We defer to the EvalFuncForConstantFPArgs()
    if (varTypeIsFloating(arg0VNtyp) && varTypeIsFloating(arg1VNtyp))
    {
        return EvalFuncForConstantFPArgs(typ, func, arg0VN, arg1VN);
    }

    // after this we shouldn't have to deal with floating point types for arg0VN or arg1VN
    assert(!varTypeIsFloating(arg0VNtyp));
    assert(!varTypeIsFloating(arg1VNtyp));

    // Stack-normalize the result type.
    if (varTypeIsSmall(typ))
    {
        typ = TYP_INT;
    }

    ValueNum result; // left uninitialized, we are required to initialize it on all paths below.
    ValueNum excSet = VNForEmptyExcSet();

    // Are both args of the same type?
    if (arg0VNtyp == arg1VNtyp)
    {
        if (arg0VNtyp == TYP_INT)
        {
            int arg0Val = ConstantValue<int>(arg0VN);
            int arg1Val = ConstantValue<int>(arg1VN);

            assert(typ == TYP_INT);
            int resultVal = EvalOp(func, arg0Val, arg1Val, &excSet);
            // Bin op on a handle results in a handle.
            ValueNum handleVN = IsVNHandle(arg0VN) ? arg0VN : IsVNHandle(arg1VN) ? arg1VN : NoVN;
            ValueNum resultVN = (handleVN != NoVN)
                                    ? VNForHandle(ssize_t(resultVal), GetHandleFlags(handleVN)) // Use VN for Handle
                                    : VNForIntCon(resultVal);
            result = VNWithExc(resultVN, excSet);
        }
        else if (arg0VNtyp == TYP_LONG)
        {
            INT64 arg0Val = ConstantValue<INT64>(arg0VN);
            INT64 arg1Val = ConstantValue<INT64>(arg1VN);

            if (VNFuncIsComparison(func))
            {
                assert(typ == TYP_INT);
                result = VNForIntCon(EvalComparison(func, arg0Val, arg1Val));
            }
            else
            {
                assert(typ == TYP_LONG);
                INT64    resultVal = EvalOp(func, arg0Val, arg1Val, &excSet);
                ValueNum handleVN  = IsVNHandle(arg0VN) ? arg0VN : IsVNHandle(arg1VN) ? arg1VN : NoVN;
                ValueNum resultVN  = (handleVN != NoVN)
                                        ? VNForHandle(ssize_t(resultVal), GetHandleFlags(handleVN)) // Use VN for Handle
                                        : VNForLongCon(resultVal);
                result = VNWithExc(resultVN, excSet);
            }
        }
        else // both args are TYP_REF or both args are TYP_BYREF
        {
            INT64 arg0Val = ConstantValue<size_t>(arg0VN); // We represent ref/byref constants as size_t's.
            INT64 arg1Val = ConstantValue<size_t>(arg1VN); // Also we consider null to be zero.

            if (VNFuncIsComparison(func))
            {
                assert(typ == TYP_INT);
                result = VNForIntCon(EvalComparison(func, arg0Val, arg1Val));
            }
            else if (typ == TYP_INT) // We could see GT_OR of a constant ByRef and Null
            {
                int resultVal = (int)EvalOp(func, arg0Val, arg1Val, &excSet);
                result        = VNWithExc(VNForIntCon(resultVal), excSet);
            }
            else // We could see GT_OR of a constant ByRef and Null
            {
                assert((typ == TYP_BYREF) || (typ == TYP_LONG));
                INT64 resultVal = EvalOp(func, arg0Val, arg1Val, &excSet);
                result          = VNWithExc(VNForByrefCon(resultVal), excSet);
            }
        }
    }
    else // We have args of different types
    {
        // We represent ref/byref constants as size_t's.
        // Also we consider null to be zero.
        //
        INT64 arg0Val = GetConstantInt64(arg0VN);
        INT64 arg1Val = GetConstantInt64(arg1VN);

        if (VNFuncIsComparison(func))
        {
            assert(typ == TYP_INT);
            result = VNForIntCon(EvalComparison(func, arg0Val, arg1Val));
        }
        else if (typ == TYP_INT) // We could see GT_OR of an int and constant ByRef or Null
        {
            int resultVal = (int)EvalOp(func, arg0Val, arg1Val, &excSet);
            result        = VNWithExc(VNForIntCon(resultVal), excSet);
        }
        else
        {
            assert(typ != TYP_INT);
            ValueNum resultValx = VNForEmptyExcSet();
            INT64    resultVal  = EvalOp(func, arg0Val, arg1Val, &resultValx);

            // check for the Exception case
            if (resultValx != VNForEmptyExcSet())
            {
                result = VNWithExc(VNForVoid(), resultValx);
            }
            else
            {
                switch (typ)
                {
                    case TYP_BYREF:
                        result = VNForByrefCon(resultVal);
                        break;
                    case TYP_LONG:
                        result = VNForLongCon(resultVal);
                        break;
                    case TYP_REF:
                        assert(resultVal == 0); // Only valid REF constant
                        result = VNForNull();
                        break;
                    default:
                        unreached();
                }
            }
        }
    }

    return result;
}

// Compute the proper value number when the VNFunc has all constant floating-point arguments
// This essentially must perform constant folding at value numbering time
//
ValueNum ValueNumStore::EvalFuncForConstantFPArgs(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(CanEvalForConstantArgs(func));
    assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));

    // We expect both argument types to be floating-point types
    var_types arg0VNtyp = TypeOfVN(arg0VN);
    var_types arg1VNtyp = TypeOfVN(arg1VN);

    assert(varTypeIsFloating(arg0VNtyp));
    assert(varTypeIsFloating(arg1VNtyp));

    // We also expect both arguments to be of the same floating-point type
    assert(arg0VNtyp == arg1VNtyp);

    ValueNum result; // left uninitialized, we are required to initialize it on all paths below.

    if (VNFuncIsComparison(func))
    {
        assert(genActualType(typ) == TYP_INT);

        if (arg0VNtyp == TYP_FLOAT)
        {
            result = VNForIntCon(EvalComparison(func, GetConstantSingle(arg0VN), GetConstantSingle(arg1VN)));
        }
        else
        {
            assert(arg0VNtyp == TYP_DOUBLE);
            result = VNForIntCon(EvalComparison(func, GetConstantDouble(arg0VN), GetConstantDouble(arg1VN)));
        }
    }
    else
    {
        // We expect the return type to be the same as the argument type
        assert(varTypeIsFloating(typ));
        assert(arg0VNtyp == typ);

        ValueNum exception = VNForEmptyExcSet();

        if (typ == TYP_FLOAT)
        {
            float floatResultVal = EvalOp(func, GetConstantSingle(arg0VN), GetConstantSingle(arg1VN), &exception);
            assert(exception == VNForEmptyExcSet()); // Floating point ops don't throw.
            result = VNForFloatCon(floatResultVal);
        }
        else
        {
            assert(typ == TYP_DOUBLE);

            double doubleResultVal = EvalOp(func, GetConstantDouble(arg0VN), GetConstantDouble(arg1VN), &exception);
            assert(exception == VNForEmptyExcSet()); // Floating point ops don't throw.
            result = VNForDoubleCon(doubleResultVal);
        }
    }

    return result;
}

// Compute the proper value number for a VNF_Cast with constant arguments
// This essentially must perform constant folding at value numbering time
//
ValueNum ValueNumStore::EvalCastForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(func == VNF_Cast);
    assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));

    // Stack-normalize the result type.
    if (varTypeIsSmall(typ))
    {
        typ = TYP_INT;
    }

    var_types arg0VNtyp = TypeOfVN(arg0VN);
    var_types arg1VNtyp = TypeOfVN(arg1VN);

    // arg1VN is really the gtCastType that we are casting to
    assert(arg1VNtyp == TYP_INT);
    int arg1Val = ConstantValue<int>(arg1VN);
    assert(arg1Val >= 0);

    if (IsVNHandle(arg0VN))
    {
        // We don't allow handles to be cast to random var_types.
        assert(typ == TYP_I_IMPL);
    }

    // We previously encoded the castToType operation using vnForCastOper()
    //
    bool      srcIsUnsigned = ((arg1Val & INT32(VCA_UnsignedSrc)) != 0);
    var_types castToType    = var_types(arg1Val >> INT32(VCA_BitCount));

    var_types castFromType = arg0VNtyp;

    switch (castFromType) // GT_CAST source type
    {
#ifndef _TARGET_64BIT_
        case TYP_REF:
        case TYP_BYREF:
#endif
        case TYP_INT:
        {
            int arg0Val = GetConstantInt32(arg0VN);

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_BOOL:
                case TYP_UBYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT8(arg0Val));
                case TYP_SHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT16(arg0Val));
                case TYP_CHAR:
                case TYP_USHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT16(arg0Val));
                case TYP_INT:
                case TYP_UINT:
                    assert(typ == TYP_INT);
                    return arg0VN;
                case TYP_LONG:
                case TYP_ULONG:
                    assert(!IsVNHandle(arg0VN));
#ifdef _TARGET_64BIT_
                    if (typ == TYP_LONG)
                    {
                        if (srcIsUnsigned)
                        {
                            return VNForLongCon(INT64(unsigned(arg0Val)));
                        }
                        else
                        {
                            return VNForLongCon(INT64(arg0Val));
                        }
                    }
                    else
                    {
                        assert(typ == TYP_BYREF);
                        if (srcIsUnsigned)
                        {
                            return VNForByrefCon(INT64(unsigned(arg0Val)));
                        }
                        else
                        {
                            return VNForByrefCon(INT64(arg0Val));
                        }
                    }
#else // TARGET_32BIT
                    if (srcIsUnsigned)
                        return VNForLongCon(INT64(unsigned(arg0Val)));
                    else
                        return VNForLongCon(INT64(arg0Val));
#endif
                case TYP_BYREF:
                    assert(typ == TYP_BYREF);
                    return VNForByrefCon((INT64)arg0Val);
                case TYP_FLOAT:
                    assert(typ == TYP_FLOAT);
                    if (srcIsUnsigned)
                    {
                        return VNForFloatCon(float(unsigned(arg0Val)));
                    }
                    else
                    {
                        return VNForFloatCon(float(arg0Val));
                    }
                case TYP_DOUBLE:
                    assert(typ == TYP_DOUBLE);
                    if (srcIsUnsigned)
                    {
                        return VNForDoubleCon(double(unsigned(arg0Val)));
                    }
                    else
                    {
                        return VNForDoubleCon(double(arg0Val));
                    }
                default:
                    unreached();
            }
            break;
        }
            {
#ifdef _TARGET_64BIT_
                case TYP_REF:
                case TYP_BYREF:
#endif
                case TYP_LONG:
                    INT64 arg0Val = GetConstantInt64(arg0VN);

                    switch (castToType)
                    {
                        case TYP_BYTE:
                            assert(typ == TYP_INT);
                            return VNForIntCon(INT8(arg0Val));
                        case TYP_BOOL:
                        case TYP_UBYTE:
                            assert(typ == TYP_INT);
                            return VNForIntCon(UINT8(arg0Val));
                        case TYP_SHORT:
                            assert(typ == TYP_INT);
                            return VNForIntCon(INT16(arg0Val));
                        case TYP_CHAR:
                        case TYP_USHORT:
                            assert(typ == TYP_INT);
                            return VNForIntCon(UINT16(arg0Val));
                        case TYP_INT:
                            assert(typ == TYP_INT);
                            return VNForIntCon(INT32(arg0Val));
                        case TYP_UINT:
                            assert(typ == TYP_INT);
                            return VNForIntCon(UINT32(arg0Val));
                        case TYP_LONG:
                        case TYP_ULONG:
                            assert(typ == TYP_LONG);
                            return arg0VN;
                        case TYP_BYREF:
                            assert(typ == TYP_BYREF);
                            return VNForByrefCon((INT64)arg0Val);
                        case TYP_FLOAT:
                            assert(typ == TYP_FLOAT);
                            if (srcIsUnsigned)
                            {
                                return VNForFloatCon(FloatingPointUtils::convertUInt64ToFloat(UINT64(arg0Val)));
                            }
                            else
                            {
                                return VNForFloatCon(float(arg0Val));
                            }
                        case TYP_DOUBLE:
                            assert(typ == TYP_DOUBLE);
                            if (srcIsUnsigned)
                            {
                                return VNForDoubleCon(FloatingPointUtils::convertUInt64ToDouble(UINT64(arg0Val)));
                            }
                            else
                            {
                                return VNForDoubleCon(double(arg0Val));
                            }
                        default:
                            unreached();
                    }
            }
        case TYP_FLOAT:
        {
            float arg0Val = GetConstantSingle(arg0VN);

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_BOOL:
                case TYP_UBYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT8(arg0Val));
                case TYP_SHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT16(arg0Val));
                case TYP_CHAR:
                case TYP_USHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT16(arg0Val));
                case TYP_INT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT32(arg0Val));
                case TYP_UINT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT32(arg0Val));
                case TYP_LONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(INT64(arg0Val));
                case TYP_ULONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(UINT64(arg0Val));
                case TYP_FLOAT:
                    assert(typ == TYP_FLOAT);
                    return VNForFloatCon(arg0Val);
                case TYP_DOUBLE:
                    assert(typ == TYP_DOUBLE);
                    return VNForDoubleCon(double(arg0Val));
                default:
                    unreached();
            }
        }
        case TYP_DOUBLE:
        {
            double arg0Val = GetConstantDouble(arg0VN);

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_BOOL:
                case TYP_UBYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT8(arg0Val));
                case TYP_SHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT16(arg0Val));
                case TYP_CHAR:
                case TYP_USHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT16(arg0Val));
                case TYP_INT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT32(arg0Val));
                case TYP_UINT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT32(arg0Val));
                case TYP_LONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(INT64(arg0Val));
                case TYP_ULONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(UINT64(arg0Val));
                case TYP_FLOAT:
                    assert(typ == TYP_FLOAT);
                    return VNForFloatCon(float(arg0Val));
                case TYP_DOUBLE:
                    assert(typ == TYP_DOUBLE);
                    return VNForDoubleCon(arg0Val);
                default:
                    unreached();
            }
        }
        default:
            unreached();
    }
}

bool ValueNumStore::CanEvalForConstantArgs(VNFunc vnf)
{
    if (vnf < VNF_Boundary)
    {
        // We'll refine this as we get counterexamples.  But to
        // a first approximation, VNFuncs that are genTreeOps should
        // be things we can evaluate.
        genTreeOps oper = genTreeOps(vnf);
        // Some exceptions...
        switch (oper)
        {
            case GT_MKREFANY: // We can't evaluate these.
            case GT_RETFILT:
            case GT_LIST:
            case GT_FIELD_LIST:
            case GT_ARR_LENGTH:
                return false;
            case GT_MULHI:
                assert(false && "Unexpected GT_MULHI node encountered before lowering");
                return false;
            default:
                return true;
        }
    }
    else
    {
        // some VNF_ that we can evaluate
        switch (vnf)
        {
            case VNF_Cast: // We can evaluate these.
                return true;
            case VNF_ObjGetType:
                return false;
            default:
                return false;
        }
    }
}

unsigned ValueNumStore::VNFuncArity(VNFunc vnf)
{
    // Read the bit field out of the table...
    return (s_vnfOpAttribs[vnf] & VNFOA_ArityMask) >> VNFOA_ArityShift;
}

template <>
bool ValueNumStore::IsOverflowIntDiv(int v0, int v1)
{
    return (v1 == -1) && (v0 == INT32_MIN);
}
template <>
bool ValueNumStore::IsOverflowIntDiv(INT64 v0, INT64 v1)
{
    return (v1 == -1) && (v0 == INT64_MIN);
}
template <typename T>
bool ValueNumStore::IsOverflowIntDiv(T v0, T v1)
{
    return false;
}

template <>
bool ValueNumStore::IsIntZero(int v)
{
    return v == 0;
}
template <>
bool ValueNumStore::IsIntZero(unsigned v)
{
    return v == 0;
}
template <>
bool ValueNumStore::IsIntZero(INT64 v)
{
    return v == 0;
}
template <>
bool ValueNumStore::IsIntZero(UINT64 v)
{
    return v == 0;
}
template <typename T>
bool ValueNumStore::IsIntZero(T v)
{
    return false;
}

template <>
float ValueNumStore::EvalOpIntegral<float>(VNFunc vnf, float v0)
{
    assert(!"EvalOpIntegral<float>");
    return 0.0f;
}

template <>
double ValueNumStore::EvalOpIntegral<double>(VNFunc vnf, double v0)
{
    assert(!"EvalOpIntegral<double>");
    return 0.0;
}

template <>
float ValueNumStore::EvalOpIntegral<float>(VNFunc vnf, float v0, float v1, ValueNum* pExcSet)
{
    genTreeOps oper = genTreeOps(vnf);
    switch (oper)
    {
        case GT_MOD:
            return fmodf(v0, v1);
        default:
            // For any other values of 'oper', we will assert and return 0.0f
            break;
    }
    assert(!"EvalOpIntegral<float> with pExcSet");
    return 0.0f;
}

template <>
double ValueNumStore::EvalOpIntegral<double>(VNFunc vnf, double v0, double v1, ValueNum* pExcSet)
{
    genTreeOps oper = genTreeOps(vnf);
    switch (oper)
    {
        case GT_MOD:
            return fmod(v0, v1);
        default:
            // For any other value of 'oper', we will assert and return 0.0
            break;
    }
    assert(!"EvalOpIntegral<double> with pExcSet");
    return 0.0;
}

ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN)
{
    assert(arg0VN != NoVN);
    assert(arg1VN != NoVN);
    assert(arg2VN != NoVN);
    assert(VNFuncArity(func) == 3);

    // Function arguments carry no exceptions.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (func != VNF_PhiDef)
    {
        // For a phi definition first and second argument are "plain" local/ssa numbers.
        // (I don't know if having such non-VN arguments to a VN function is a good idea -- if we wanted to declare
        // ValueNum to be "short" it would be a problem, for example.  But we'll leave it for now, with these explicit
        // exceptions.)
        assert(arg0VN == VNNormVal(arg0VN));
        assert(arg1VN == VNNormVal(arg1VN));
    }
    assert(arg2VN == VNNormVal(arg2VN));

#endif
    assert(VNFuncArity(func) == 3);

    ValueNum      res;
    VNDefFunc3Arg fstruct(func, arg0VN, arg1VN, arg2VN);
    if (GetVNFunc3Map()->Lookup(fstruct, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func3);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        res                                                            = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc3Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
        GetVNFunc3Map()->Set(fstruct, res);
        return res;
    }
}

ValueNum ValueNumStore::VNForFunc(
    var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN, ValueNum arg3VN)
{
    assert(arg0VN != NoVN && arg1VN != NoVN && arg2VN != NoVN && arg3VN != NoVN);
    // Function arguments carry no exceptions.
    assert(arg0VN == VNNormVal(arg0VN));
    assert(arg1VN == VNNormVal(arg1VN));
    assert(arg2VN == VNNormVal(arg2VN));
    assert(arg3VN == VNNormVal(arg3VN));
    assert(VNFuncArity(func) == 4);

    ValueNum      res;
    VNDefFunc4Arg fstruct(func, arg0VN, arg1VN, arg2VN, arg3VN);
    if (GetVNFunc4Map()->Lookup(fstruct, &res))
    {
        return res;
    }
    else
    {
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func4);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        res                                                            = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc4Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
        GetVNFunc4Map()->Set(fstruct, res);
        return res;
    }
}

//------------------------------------------------------------------------
// VNForExpr: Opaque value number that is equivalent to itself but unique
//    from all other value numbers.
//
// Arguments:
//    block - BasicBlock where the expression that produces this value occurs.
//            May be nullptr to force conservative "could be anywhere" interpretation.
//     typ - Type of the expression in the IR
//
// Return Value:
//    A new value number distinct from any previously generated, that compares as equal
//    to itself, but not any other value number, and is annotated with the given
//    type and block.

ValueNum ValueNumStore::VNForExpr(BasicBlock* block, var_types typ)
{
    BasicBlock::loopNumber loopNum;
    if (block == nullptr)
    {
        loopNum = MAX_LOOP_NUM;
    }
    else
    {
        loopNum = block->bbNatLoopNum;
    }

    // We always allocate a new, unique VN in this call.
    // The 'typ' is used to partition the allocation of VNs into different chunks.
    Chunk*   c                 = GetAllocChunk(typ, CEA_None, loopNum);
    unsigned offsetWithinChunk = c->AllocVN();
    ValueNum result            = c->m_baseVN + offsetWithinChunk;
    return result;
}

ValueNum ValueNumStore::VNApplySelectors(ValueNumKind  vnk,
                                         ValueNum      map,
                                         FieldSeqNode* fieldSeq,
                                         size_t*       wbFinalStructSize)
{
    if (fieldSeq == nullptr)
    {
        return map;
    }
    else
    {
        assert(fieldSeq != FieldSeqStore::NotAField());

        // Skip any "FirstElem" pseudo-fields or any "ConstantIndex" pseudo-fields
        if (fieldSeq->IsPseudoField())
        {
            return VNApplySelectors(vnk, map, fieldSeq->m_next, wbFinalStructSize);
        }

        // Otherwise, is a real field handle.
        CORINFO_FIELD_HANDLE fldHnd    = fieldSeq->m_fieldHnd;
        CORINFO_CLASS_HANDLE structHnd = NO_CLASS_HANDLE;
        ValueNum             fldHndVN  = VNForHandle(ssize_t(fldHnd), GTF_ICON_FIELD_HDL);
        noway_assert(fldHnd != nullptr);
        CorInfoType fieldCit  = m_pComp->info.compCompHnd->getFieldType(fldHnd, &structHnd);
        var_types   fieldType = JITtype2varType(fieldCit);

        size_t structSize = 0;
        if (varTypeIsStruct(fieldType))
        {
            structSize = m_pComp->info.compCompHnd->getClassSize(structHnd);
            // We do not normalize the type field accesses during importation unless they
            // are used in a call, return or assignment.
            if ((fieldType == TYP_STRUCT) && (structSize <= m_pComp->largestEnregisterableStructSize()))
            {
                fieldType = m_pComp->impNormStructType(structHnd);
            }
        }
        if (wbFinalStructSize != nullptr)
        {
            *wbFinalStructSize = structSize;
        }

#ifdef DEBUG
        if (m_pComp->verbose)
        {
            printf("  VNApplySelectors:\n");
            const char* modName;
            const char* fldName = m_pComp->eeGetFieldName(fldHnd, &modName);
            printf("    VNForHandle(Fseq[%s]) is " STR_VN "%x, fieldType is %s", fldName, fldHndVN,
                   varTypeName(fieldType));
            if (varTypeIsStruct(fieldType))
            {
                printf(", size = %d", structSize);
            }
            printf("\n");
        }
#endif

        if (fieldSeq->m_next != nullptr)
        {
            ValueNum newMap = VNForMapSelect(vnk, fieldType, map, fldHndVN);
            return VNApplySelectors(vnk, newMap, fieldSeq->m_next, wbFinalStructSize);
        }
        else // end of fieldSeq
        {
            return VNForMapSelect(vnk, fieldType, map, fldHndVN);
        }
    }
}

ValueNum ValueNumStore::VNApplySelectorsTypeCheck(ValueNum elem, var_types indType, size_t elemStructSize)
{
    var_types elemTyp = TypeOfVN(elem);

    // Check if the elemTyp is matching/compatible

    if (indType != elemTyp)
    {
        bool isConstant = IsVNConstant(elem);
        if (isConstant && (elemTyp == genActualType(indType)))
        {
            // (i.e. We recorded a constant of TYP_INT for a TYP_BYTE field)
        }
        else
        {
            // We are trying to read from an 'elem' of type 'elemType' using 'indType' read

            size_t elemTypSize = (elemTyp == TYP_STRUCT) ? elemStructSize : genTypeSize(elemTyp);
            size_t indTypeSize = genTypeSize(indType);

            if ((indType == TYP_REF) && (varTypeIsStruct(elemTyp)))
            {
                // indType is TYP_REF and elemTyp is TYP_STRUCT
                //
                // We have a pointer to a static that is a Boxed Struct
                //
                return elem;
            }
            else if (indTypeSize > elemTypSize)
            {
                // Reading beyong the end of 'elem'

                // return a new unique value number
                elem = VNForExpr(nullptr, indType);
                JITDUMP("    *** Mismatched types in VNApplySelectorsTypeCheck (reading beyond the end)\n");
            }
            else if (varTypeIsStruct(indType))
            {
                // indType is TYP_STRUCT

                // return a new unique value number
                elem = VNForExpr(nullptr, indType);
                JITDUMP("    *** Mismatched types in VNApplySelectorsTypeCheck (indType is TYP_STRUCT)\n");
            }
            else
            {
                // We are trying to read an 'elem' of type 'elemType' using 'indType' read

                // insert a cast of elem to 'indType'
                elem = VNForCast(elem, indType, elemTyp);
            }
        }
    }
    return elem;
}

ValueNum ValueNumStore::VNApplySelectorsAssignTypeCoerce(ValueNum elem, var_types indType, BasicBlock* block)
{
    var_types elemTyp = TypeOfVN(elem);

    // Check if the elemTyp is matching/compatible

    if (indType != elemTyp)
    {
        bool isConstant = IsVNConstant(elem);
        if (isConstant && (elemTyp == genActualType(indType)))
        {
            // (i.e. We recorded a constant of TYP_INT for a TYP_BYTE field)
        }
        else
        {
            // We are trying to write an 'elem' of type 'elemType' using 'indType' store

            if (varTypeIsStruct(indType))
            {
                // return a new unique value number
                elem = VNForExpr(block, indType);
                JITDUMP("    *** Mismatched types in VNApplySelectorsAssignTypeCoerce (indType is TYP_STRUCT)\n");
            }
            else
            {
                // We are trying to write an 'elem' of type 'elemType' using 'indType' store

                // insert a cast of elem to 'indType'
                elem = VNForCast(elem, indType, elemTyp);
            }
        }
    }
    return elem;
}

//------------------------------------------------------------------------
// VNApplySelectorsAssign: Compute the value number corresponding to "map" but with
//    the element at "fieldSeq" updated to have type "elem"; this is the new memory
//    value for an assignment of value "elem" into the memory at location "fieldSeq"
//    that occurs in block "block" and has type "indType" (so long as the selectors
//    into that memory occupy disjoint locations, which is true for GcHeap).
//
// Arguments:
//    vnk - Identifies whether to recurse to Conservative or Liberal value numbers
//          when recursing through phis
//    map - Value number for the field map before the assignment
//    elem - Value number for the value being stored (to the given field)
//    indType - Type of the indirection storing the value to the field
//    block - Block where the assignment occurs
//
// Return Value:
//    The value number corresponding to memory after the assignment.

ValueNum ValueNumStore::VNApplySelectorsAssign(
    ValueNumKind vnk, ValueNum map, FieldSeqNode* fieldSeq, ValueNum elem, var_types indType, BasicBlock* block)
{
    if (fieldSeq == nullptr)
    {
        return VNApplySelectorsAssignTypeCoerce(elem, indType, block);
    }
    else
    {
        assert(fieldSeq != FieldSeqStore::NotAField());

        // Skip any "FirstElem" pseudo-fields or any "ConstantIndex" pseudo-fields
        // These will occur, at least, in struct static expressions, for method table offsets.
        if (fieldSeq->IsPseudoField())
        {
            return VNApplySelectorsAssign(vnk, map, fieldSeq->m_next, elem, indType, block);
        }

        // Otherwise, fldHnd is a real field handle.
        CORINFO_FIELD_HANDLE fldHnd     = fieldSeq->m_fieldHnd;
        CORINFO_CLASS_HANDLE structType = nullptr;
        noway_assert(fldHnd != nullptr);
        CorInfoType fieldCit  = m_pComp->info.compCompHnd->getFieldType(fldHnd, &structType);
        var_types   fieldType = JITtype2varType(fieldCit);

        ValueNum fieldHndVN = VNForHandle(ssize_t(fldHnd), GTF_ICON_FIELD_HDL);

#ifdef DEBUG
        if (m_pComp->verbose)
        {
            printf("  fieldHnd " STR_VN "%x is ", fieldHndVN);
            vnDump(m_pComp, fieldHndVN);
            printf("\n");

            ValueNum seqNextVN  = VNForFieldSeq(fieldSeq->m_next);
            ValueNum fieldSeqVN = VNForFunc(TYP_REF, VNF_FieldSeq, fieldHndVN, seqNextVN);

            printf("  fieldSeq " STR_VN "%x is ", fieldSeqVN);
            vnDump(m_pComp, fieldSeqVN);
            printf("\n");
        }
#endif

        ValueNum elemAfter;
        if (fieldSeq->m_next)
        {
            ValueNum fseqMap = VNForMapSelect(vnk, fieldType, map, fieldHndVN);
            elemAfter        = VNApplySelectorsAssign(vnk, fseqMap, fieldSeq->m_next, elem, indType, block);
        }
        else
        {
            elemAfter = VNApplySelectorsAssignTypeCoerce(elem, indType, block);
        }

        ValueNum newMap = VNForMapStore(fieldType, map, fieldHndVN, elemAfter);
        return newMap;
    }
}

ValueNumPair ValueNumStore::VNPairApplySelectors(ValueNumPair map, FieldSeqNode* fieldSeq, var_types indType)
{
    size_t   structSize = 0;
    ValueNum liberalVN  = VNApplySelectors(VNK_Liberal, map.GetLiberal(), fieldSeq, &structSize);
    liberalVN           = VNApplySelectorsTypeCheck(liberalVN, indType, structSize);

    structSize         = 0;
    ValueNum conservVN = VNApplySelectors(VNK_Conservative, map.GetConservative(), fieldSeq, &structSize);
    conservVN          = VNApplySelectorsTypeCheck(conservVN, indType, structSize);

    return ValueNumPair(liberalVN, conservVN);
}

bool ValueNumStore::IsVNNotAField(ValueNum vn)
{
    return m_chunks.GetNoExpand(GetChunkNum(vn))->m_attribs == CEA_NotAField;
}

ValueNum ValueNumStore::VNForFieldSeq(FieldSeqNode* fieldSeq)
{
    if (fieldSeq == nullptr)
    {
        return VNForNull();
    }
    else if (fieldSeq == FieldSeqStore::NotAField())
    {
        // We always allocate a new, unique VN in this call.
        Chunk*   c                 = GetAllocChunk(TYP_REF, CEA_NotAField);
        unsigned offsetWithinChunk = c->AllocVN();
        ValueNum result            = c->m_baseVN + offsetWithinChunk;
        return result;
    }
    else
    {
        ssize_t  fieldHndVal = ssize_t(fieldSeq->m_fieldHnd);
        ValueNum fieldHndVN  = VNForHandle(fieldHndVal, GTF_ICON_FIELD_HDL);
        ValueNum seqNextVN   = VNForFieldSeq(fieldSeq->m_next);
        ValueNum fieldSeqVN  = VNForFunc(TYP_REF, VNF_FieldSeq, fieldHndVN, seqNextVN);

#ifdef DEBUG
        if (m_pComp->verbose)
        {
            printf("  fieldHnd " STR_VN "%x is ", fieldHndVN);
            vnDump(m_pComp, fieldHndVN);
            printf("\n");

            printf("  fieldSeq " STR_VN "%x is ", fieldSeqVN);
            vnDump(m_pComp, fieldSeqVN);
            printf("\n");
        }
#endif

        return fieldSeqVN;
    }
}

FieldSeqNode* ValueNumStore::FieldSeqVNToFieldSeq(ValueNum vn)
{
    if (vn == VNForNull())
    {
        return nullptr;
    }

    assert(IsVNFunc(vn));

    VNFuncApp funcApp;
    GetVNFunc(vn, &funcApp);
    if (funcApp.m_func == VNF_NotAField)
    {
        return FieldSeqStore::NotAField();
    }

    assert(funcApp.m_func == VNF_FieldSeq);
    const ssize_t fieldHndVal = ConstantValue<ssize_t>(funcApp.m_args[0]);
    FieldSeqNode* head =
        m_pComp->GetFieldSeqStore()->CreateSingleton(reinterpret_cast<CORINFO_FIELD_HANDLE>(fieldHndVal));
    FieldSeqNode* tail = FieldSeqVNToFieldSeq(funcApp.m_args[1]);
    return m_pComp->GetFieldSeqStore()->Append(head, tail);
}

ValueNum ValueNumStore::FieldSeqVNAppend(ValueNum fsVN1, ValueNum fsVN2)
{
    if (fsVN1 == VNForNull())
    {
        return fsVN2;
    }

    assert(IsVNFunc(fsVN1));

    VNFuncApp funcApp1;
    GetVNFunc(fsVN1, &funcApp1);

    if ((funcApp1.m_func == VNF_NotAField) || IsVNNotAField(fsVN2))
    {
        return VNForFieldSeq(FieldSeqStore::NotAField());
    }

    assert(funcApp1.m_func == VNF_FieldSeq);
    ValueNum tailRes    = FieldSeqVNAppend(funcApp1.m_args[1], fsVN2);
    ValueNum fieldSeqVN = VNForFunc(TYP_REF, VNF_FieldSeq, funcApp1.m_args[0], tailRes);

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("  fieldSeq " STR_VN "%x is ", fieldSeqVN);
        vnDump(m_pComp, fieldSeqVN);
        printf("\n");
    }
#endif

    return fieldSeqVN;
}

ValueNum ValueNumStore::ExtendPtrVN(GenTreePtr opA, GenTreePtr opB)
{
    if (opB->OperGet() == GT_CNS_INT)
    {
        FieldSeqNode* fldSeq = opB->gtIntCon.gtFieldSeq;
        if (fldSeq != nullptr)
        {
            return ExtendPtrVN(opA, opB->gtIntCon.gtFieldSeq);
        }
    }
    return NoVN;
}

ValueNum ValueNumStore::ExtendPtrVN(GenTreePtr opA, FieldSeqNode* fldSeq)
{
    assert(fldSeq != nullptr);

    ValueNum res = NoVN;

    ValueNum opAvnWx = opA->gtVNPair.GetLiberal();
    assert(VNIsValid(opAvnWx));
    ValueNum opAvn;
    ValueNum opAvnx = VNForEmptyExcSet();
    VNUnpackExc(opAvnWx, &opAvn, &opAvnx);
    assert(VNIsValid(opAvn) && VNIsValid(opAvnx));

    VNFuncApp funcApp;
    if (!GetVNFunc(opAvn, &funcApp))
    {
        return res;
    }

    if (funcApp.m_func == VNF_PtrToLoc)
    {
#ifdef DEBUG
        // For PtrToLoc, lib == cons.
        VNFuncApp consFuncApp;
        assert(GetVNFunc(VNNormVal(opA->GetVN(VNK_Conservative)), &consFuncApp) && consFuncApp.Equals(funcApp));
#endif
        ValueNum fldSeqVN = VNForFieldSeq(fldSeq);
        res = VNForFunc(TYP_BYREF, VNF_PtrToLoc, funcApp.m_args[0], FieldSeqVNAppend(funcApp.m_args[1], fldSeqVN));
    }
    else if (funcApp.m_func == VNF_PtrToStatic)
    {
        ValueNum fldSeqVN = VNForFieldSeq(fldSeq);
        res               = VNForFunc(TYP_BYREF, VNF_PtrToStatic, FieldSeqVNAppend(funcApp.m_args[0], fldSeqVN));
    }
    else if (funcApp.m_func == VNF_PtrToArrElem)
    {
        ValueNum fldSeqVN = VNForFieldSeq(fldSeq);
        res = VNForFunc(TYP_BYREF, VNF_PtrToArrElem, funcApp.m_args[0], funcApp.m_args[1], funcApp.m_args[2],
                        FieldSeqVNAppend(funcApp.m_args[3], fldSeqVN));
    }
    if (res != NoVN)
    {
        res = VNWithExc(res, opAvnx);
    }
    return res;
}

ValueNum Compiler::fgValueNumberArrIndexAssign(CORINFO_CLASS_HANDLE elemTypeEq,
                                               ValueNum             arrVN,
                                               ValueNum             inxVN,
                                               FieldSeqNode*        fldSeq,
                                               ValueNum             rhsVN,
                                               var_types            indType)
{
    bool      invalidateArray      = false;
    ValueNum  elemTypeEqVN         = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
    var_types arrElemType          = DecodeElemType(elemTypeEq);
    ValueNum  hAtArrType           = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, fgCurMemoryVN[GcHeap], elemTypeEqVN);
    ValueNum  hAtArrTypeAtArr      = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, hAtArrType, arrVN);
    ValueNum  hAtArrTypeAtArrAtInx = vnStore->VNForMapSelect(VNK_Liberal, arrElemType, hAtArrTypeAtArr, inxVN);

    ValueNum newValAtInx     = ValueNumStore::NoVN;
    ValueNum newValAtArr     = ValueNumStore::NoVN;
    ValueNum newValAtArrType = ValueNumStore::NoVN;

    if (fldSeq == FieldSeqStore::NotAField())
    {
        // This doesn't represent a proper array access
        JITDUMP("    *** NotAField sequence encountered in fgValueNumberArrIndexAssign\n");

        // Store a new unique value for newValAtArrType
        newValAtArrType = vnStore->VNForExpr(compCurBB, TYP_REF);
        invalidateArray = true;
    }
    else
    {
        // Note that this does the right thing if "fldSeq" is null -- returns last "rhs" argument.
        // This is the value that should be stored at "arr[inx]".
        newValAtInx =
            vnStore->VNApplySelectorsAssign(VNK_Liberal, hAtArrTypeAtArrAtInx, fldSeq, rhsVN, indType, compCurBB);

        var_types arrElemFldType = arrElemType; // Uses arrElemType unless we has a non-null fldSeq
        if (vnStore->IsVNFunc(newValAtInx))
        {
            VNFuncApp funcApp;
            vnStore->GetVNFunc(newValAtInx, &funcApp);
            if (funcApp.m_func == VNF_MapStore)
            {
                arrElemFldType = vnStore->TypeOfVN(newValAtInx);
            }
        }

        if (indType != arrElemFldType)
        {
            // Mismatched types: Store between different types (indType into array of arrElemFldType)
            //

            JITDUMP("    *** Mismatched types in fgValueNumberArrIndexAssign\n");

            // Store a new unique value for newValAtArrType
            newValAtArrType = vnStore->VNForExpr(compCurBB, TYP_REF);
            invalidateArray = true;
        }
    }

    if (!invalidateArray)
    {
        newValAtArr     = vnStore->VNForMapStore(indType, hAtArrTypeAtArr, inxVN, newValAtInx);
        newValAtArrType = vnStore->VNForMapStore(TYP_REF, hAtArrType, arrVN, newValAtArr);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("  hAtArrType " STR_VN "%x is MapSelect(curGcHeap(" STR_VN "%x), ", hAtArrType, fgCurMemoryVN[GcHeap]);

        if (arrElemType == TYP_STRUCT)
        {
            printf("%s[]).\n", eeGetClassName(elemTypeEq));
        }
        else
        {
            printf("%s[]).\n", varTypeName(arrElemType));
        }
        printf("  hAtArrTypeAtArr " STR_VN "%x is MapSelect(hAtArrType(" STR_VN "%x), arr=" STR_VN "%x)\n",
               hAtArrTypeAtArr, hAtArrType, arrVN);
        printf("  hAtArrTypeAtArrAtInx " STR_VN "%x is MapSelect(hAtArrTypeAtArr(" STR_VN "%x), inx=" STR_VN "%x):%s\n",
               hAtArrTypeAtArrAtInx, hAtArrTypeAtArr, inxVN, varTypeName(arrElemType));

        if (!invalidateArray)
        {
            printf("  newValAtInd " STR_VN "%x is ", newValAtInx);
            vnStore->vnDump(this, newValAtInx);
            printf("\n");

            printf("  newValAtArr " STR_VN "%x is ", newValAtArr);
            vnStore->vnDump(this, newValAtArr);
            printf("\n");
        }

        printf("  newValAtArrType " STR_VN "%x is ", newValAtArrType);
        vnStore->vnDump(this, newValAtArrType);
        printf("\n");

        printf("  fgCurMemoryVN assigned:\n");
    }
#endif // DEBUG

    return vnStore->VNForMapStore(TYP_REF, fgCurMemoryVN[GcHeap], elemTypeEqVN, newValAtArrType);
}

ValueNum Compiler::fgValueNumberArrIndexVal(GenTreePtr tree, VNFuncApp* pFuncApp, ValueNum addrXvn)
{
    assert(vnStore->IsVNHandle(pFuncApp->m_args[0]));
    CORINFO_CLASS_HANDLE arrElemTypeEQ = CORINFO_CLASS_HANDLE(vnStore->ConstantValue<ssize_t>(pFuncApp->m_args[0]));
    ValueNum             arrVN         = pFuncApp->m_args[1];
    ValueNum             inxVN         = pFuncApp->m_args[2];
    FieldSeqNode*        fldSeq        = vnStore->FieldSeqVNToFieldSeq(pFuncApp->m_args[3]);
    return fgValueNumberArrIndexVal(tree, arrElemTypeEQ, arrVN, inxVN, addrXvn, fldSeq);
}

ValueNum Compiler::fgValueNumberArrIndexVal(GenTreePtr           tree,
                                            CORINFO_CLASS_HANDLE elemTypeEq,
                                            ValueNum             arrVN,
                                            ValueNum             inxVN,
                                            ValueNum             excVN,
                                            FieldSeqNode*        fldSeq)
{
    assert(tree == nullptr || tree->OperIsIndir());

    // The VN inputs are required to be non-exceptional values.
    assert(arrVN == vnStore->VNNormVal(arrVN));
    assert(inxVN == vnStore->VNNormVal(inxVN));

    var_types elemTyp = DecodeElemType(elemTypeEq);
    var_types indType = (tree == nullptr) ? elemTyp : tree->TypeGet();
    ValueNum  selectedElem;

    if (fldSeq == FieldSeqStore::NotAField())
    {
        // This doesn't represent a proper array access
        JITDUMP("    *** NotAField sequence encountered in fgValueNumberArrIndexVal\n");

        // a new unique value number
        selectedElem = vnStore->VNForExpr(compCurBB, elemTyp);

#ifdef DEBUG
        if (verbose)
        {
            printf("  IND of PtrToArrElem is unique VN " STR_VN "%x.\n", selectedElem);
        }
#endif // DEBUG

        if (tree != nullptr)
        {
            tree->gtVNPair.SetBoth(selectedElem);
        }
    }
    else
    {
        ValueNum elemTypeEqVN    = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
        ValueNum hAtArrType      = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, fgCurMemoryVN[GcHeap], elemTypeEqVN);
        ValueNum hAtArrTypeAtArr = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, hAtArrType, arrVN);
        ValueNum wholeElem       = vnStore->VNForMapSelect(VNK_Liberal, elemTyp, hAtArrTypeAtArr, inxVN);

#ifdef DEBUG
        if (verbose)
        {
            printf("  hAtArrType " STR_VN "%x is MapSelect(curGcHeap(" STR_VN "%x), ", hAtArrType,
                   fgCurMemoryVN[GcHeap]);
            if (elemTyp == TYP_STRUCT)
            {
                printf("%s[]).\n", eeGetClassName(elemTypeEq));
            }
            else
            {
                printf("%s[]).\n", varTypeName(elemTyp));
            }

            printf("  hAtArrTypeAtArr " STR_VN "%x is MapSelect(hAtArrType(" STR_VN "%x), arr=" STR_VN "%x).\n",
                   hAtArrTypeAtArr, hAtArrType, arrVN);

            printf("  wholeElem " STR_VN "%x is MapSelect(hAtArrTypeAtArr(" STR_VN "%x), ind=" STR_VN "%x).\n",
                   wholeElem, hAtArrTypeAtArr, inxVN);
        }
#endif // DEBUG

        selectedElem          = wholeElem;
        size_t elemStructSize = 0;
        if (fldSeq)
        {
            selectedElem = vnStore->VNApplySelectors(VNK_Liberal, wholeElem, fldSeq, &elemStructSize);
            elemTyp      = vnStore->TypeOfVN(selectedElem);
        }
        selectedElem = vnStore->VNApplySelectorsTypeCheck(selectedElem, indType, elemStructSize);
        selectedElem = vnStore->VNWithExc(selectedElem, excVN);

#ifdef DEBUG
        if (verbose && (selectedElem != wholeElem))
        {
            printf("  selectedElem is " STR_VN "%x after applying selectors.\n", selectedElem);
        }
#endif // DEBUG

        if (tree != nullptr)
        {
            tree->gtVNPair.SetLiberal(selectedElem);
            // TODO-CQ: what to do here about exceptions?  We don't have the array and ind conservative
            // values, so we don't have their exceptions.  Maybe we should.
            tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
        }
    }

    return selectedElem;
}

ValueNum Compiler::fgValueNumberByrefExposedLoad(var_types type, ValueNum pointerVN)
{
    ValueNum memoryVN = fgCurMemoryVN[ByrefExposed];
    // The memoization for VNFunc applications does not factor in the result type, so
    // VNF_ByrefExposedLoad takes the loaded type as an explicit parameter.
    ValueNum typeVN = vnStore->VNForIntCon(type);
    ValueNum loadVN = vnStore->VNForFunc(type, VNF_ByrefExposedLoad, typeVN, vnStore->VNNormVal(pointerVN), memoryVN);

    return loadVN;
}

var_types ValueNumStore::TypeOfVN(ValueNum vn)
{
    if (vn == NoVN)
    {
        return TYP_UNDEF;
    }

    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    return c->m_typ;
}

//------------------------------------------------------------------------
// LoopOfVN: If the given value number is an opaque one associated with a particular
//    expression in the IR, give the loop number where the expression occurs; otherwise,
//    returns MAX_LOOP_NUM.
//
// Arguments:
//    vn - Value number to query
//
// Return Value:
//    The correspondingblock's bbNatLoopNum, which may be BasicBlock::NOT_IN_LOOP.
//    Returns MAX_LOOP_NUM if this VN is not an opaque value number associated with
//    a particular expression/location in the IR.

BasicBlock::loopNumber ValueNumStore::LoopOfVN(ValueNum vn)
{
    if (vn == NoVN)
    {
        return MAX_LOOP_NUM;
    }

    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    return c->m_loopNum;
}

bool ValueNumStore::IsVNConstant(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    if (c->m_attribs == CEA_Const)
    {
        return vn != VNForVoid(); // Void is not a "real" constant -- in the sense that it represents no value.
    }
    else
    {
        return c->m_attribs == CEA_Handle;
    }
}

bool ValueNumStore::IsVNInt32Constant(ValueNum vn)
{
    if (!IsVNConstant(vn))
    {
        return false;
    }

    return TypeOfVN(vn) == TYP_INT;
}

unsigned ValueNumStore::GetHandleFlags(ValueNum vn)
{
    assert(IsVNHandle(vn));
    Chunk*    c      = m_chunks.GetNoExpand(GetChunkNum(vn));
    unsigned  offset = ChunkOffset(vn);
    VNHandle* handle = &reinterpret_cast<VNHandle*>(c->m_defs)[offset];
    return handle->m_flags;
}

bool ValueNumStore::IsVNHandle(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }

    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    return c->m_attribs == CEA_Handle;
}

bool ValueNumStore::IsVNConstantBound(ValueNum vn)
{
    // Do we have "var < 100"?
    if (vn == NoVN)
    {
        return false;
    }

    VNFuncApp funcAttr;
    if (!GetVNFunc(vn, &funcAttr))
    {
        return false;
    }
    if (funcAttr.m_func != (VNFunc)GT_LE && funcAttr.m_func != (VNFunc)GT_GE && funcAttr.m_func != (VNFunc)GT_LT &&
        funcAttr.m_func != (VNFunc)GT_GT)
    {
        return false;
    }

    return IsVNInt32Constant(funcAttr.m_args[0]) != IsVNInt32Constant(funcAttr.m_args[1]);
}

void ValueNumStore::GetConstantBoundInfo(ValueNum vn, ConstantBoundInfo* info)
{
    assert(IsVNConstantBound(vn));
    assert(info);

    // Do we have var < 100?
    VNFuncApp funcAttr;
    GetVNFunc(vn, &funcAttr);

    bool isOp1Const = IsVNInt32Constant(funcAttr.m_args[1]);

    if (isOp1Const)
    {
        info->cmpOper  = funcAttr.m_func;
        info->cmpOpVN  = funcAttr.m_args[0];
        info->constVal = GetConstantInt32(funcAttr.m_args[1]);
    }
    else
    {
        info->cmpOper  = GenTree::SwapRelop((genTreeOps)funcAttr.m_func);
        info->cmpOpVN  = funcAttr.m_args[1];
        info->constVal = GetConstantInt32(funcAttr.m_args[0]);
    }
}

//------------------------------------------------------------------------
// IsVNArrLenUnsignedBound: Checks if the specified vn represents an expression
//    such as "(uint)i < (uint)len" that implies that the index is valid
//    (0 <= i && i < a.len).
//
// Arguments:
//    vn - Value number to query
//    info - Pointer to an UnsignedCompareCheckedBoundInfo object to return information about
//           the expression. Not populated if the vn expression isn't suitable (e.g. i <= len).
//           This enables optCreateJTrueBoundAssertion to immediatly create an OAK_NO_THROW
//           assertion instead of the OAK_EQUAL/NOT_EQUAL assertions created by signed compares
//           (IsVNCompareCheckedBound, IsVNCompareCheckedBoundArith) that require further processing.

bool ValueNumStore::IsVNUnsignedCompareCheckedBound(ValueNum vn, UnsignedCompareCheckedBoundInfo* info)
{
    VNFuncApp funcApp;

    if (GetVNFunc(vn, &funcApp))
    {
        if ((funcApp.m_func == VNF_LT_UN) || (funcApp.m_func == VNF_GE_UN))
        {
            // We only care about "(uint)i < (uint)len" and its negation "(uint)i >= (uint)len"
            if (IsVNCheckedBound(funcApp.m_args[1]))
            {
                info->vnIdx   = funcApp.m_args[0];
                info->cmpOper = funcApp.m_func;
                info->vnBound = funcApp.m_args[1];
                return true;
            }
        }
        else if ((funcApp.m_func == VNF_GT_UN) || (funcApp.m_func == VNF_LE_UN))
        {
            // We only care about "(uint)a.len > (uint)i" and its negation "(uint)a.len <= (uint)i"
            if (IsVNCheckedBound(funcApp.m_args[0]))
            {
                info->vnIdx = funcApp.m_args[1];
                // Let's keep a consistent operand order - it's always i < len, never len > i
                info->cmpOper = (funcApp.m_func == VNF_GT_UN) ? VNF_LT_UN : VNF_GE_UN;
                info->vnBound = funcApp.m_args[0];
                return true;
            }
        }
    }

    return false;
}

bool ValueNumStore::IsVNCompareCheckedBound(ValueNum vn)
{
    // Do we have "var < len"?
    if (vn == NoVN)
    {
        return false;
    }

    VNFuncApp funcAttr;
    if (!GetVNFunc(vn, &funcAttr))
    {
        return false;
    }
    if (funcAttr.m_func != (VNFunc)GT_LE && funcAttr.m_func != (VNFunc)GT_GE && funcAttr.m_func != (VNFunc)GT_LT &&
        funcAttr.m_func != (VNFunc)GT_GT)
    {
        return false;
    }
    if (!IsVNCheckedBound(funcAttr.m_args[0]) && !IsVNCheckedBound(funcAttr.m_args[1]))
    {
        return false;
    }

    return true;
}

void ValueNumStore::GetCompareCheckedBound(ValueNum vn, CompareCheckedBoundArithInfo* info)
{
    assert(IsVNCompareCheckedBound(vn));

    // Do we have var < a.len?
    VNFuncApp funcAttr;
    GetVNFunc(vn, &funcAttr);

    bool isOp1CheckedBound = IsVNCheckedBound(funcAttr.m_args[1]);
    if (isOp1CheckedBound)
    {
        info->cmpOper = funcAttr.m_func;
        info->cmpOp   = funcAttr.m_args[0];
        info->vnBound = funcAttr.m_args[1];
    }
    else
    {
        info->cmpOper = GenTree::SwapRelop((genTreeOps)funcAttr.m_func);
        info->cmpOp   = funcAttr.m_args[1];
        info->vnBound = funcAttr.m_args[0];
    }
}

bool ValueNumStore::IsVNCheckedBoundArith(ValueNum vn)
{
    // Do we have "a.len +or- var"
    if (vn == NoVN)
    {
        return false;
    }

    VNFuncApp funcAttr;

    return GetVNFunc(vn, &funcAttr) &&                                                     // vn is a func.
           (funcAttr.m_func == (VNFunc)GT_ADD || funcAttr.m_func == (VNFunc)GT_SUB) &&     // the func is +/-
           (IsVNCheckedBound(funcAttr.m_args[0]) || IsVNCheckedBound(funcAttr.m_args[1])); // either op1 or op2 is a.len
}

void ValueNumStore::GetCheckedBoundArithInfo(ValueNum vn, CompareCheckedBoundArithInfo* info)
{
    // Do we have a.len +/- var?
    assert(IsVNCheckedBoundArith(vn));
    VNFuncApp funcArith;
    GetVNFunc(vn, &funcArith);

    bool isOp1CheckedBound = IsVNCheckedBound(funcArith.m_args[1]);
    if (isOp1CheckedBound)
    {
        info->arrOper = funcArith.m_func;
        info->arrOp   = funcArith.m_args[0];
        info->vnBound = funcArith.m_args[1];
    }
    else
    {
        info->arrOper = funcArith.m_func;
        info->arrOp   = funcArith.m_args[1];
        info->vnBound = funcArith.m_args[0];
    }
}

bool ValueNumStore::IsVNCompareCheckedBoundArith(ValueNum vn)
{
    // Do we have: "var < a.len - var"
    if (vn == NoVN)
    {
        return false;
    }

    VNFuncApp funcAttr;
    if (!GetVNFunc(vn, &funcAttr))
    {
        return false;
    }

    // Suitable comparator.
    if (funcAttr.m_func != (VNFunc)GT_LE && funcAttr.m_func != (VNFunc)GT_GE && funcAttr.m_func != (VNFunc)GT_LT &&
        funcAttr.m_func != (VNFunc)GT_GT)
    {
        return false;
    }

    // Either the op0 or op1 is arr len arithmetic.
    if (!IsVNCheckedBoundArith(funcAttr.m_args[0]) && !IsVNCheckedBoundArith(funcAttr.m_args[1]))
    {
        return false;
    }

    return true;
}

void ValueNumStore::GetCompareCheckedBoundArithInfo(ValueNum vn, CompareCheckedBoundArithInfo* info)
{
    assert(IsVNCompareCheckedBoundArith(vn));

    VNFuncApp funcAttr;
    GetVNFunc(vn, &funcAttr);

    // Check whether op0 or op1 is checked bound arithmetic.
    bool isOp1CheckedBoundArith = IsVNCheckedBoundArith(funcAttr.m_args[1]);
    if (isOp1CheckedBoundArith)
    {
        info->cmpOper = funcAttr.m_func;
        info->cmpOp   = funcAttr.m_args[0];
        GetCheckedBoundArithInfo(funcAttr.m_args[1], info);
    }
    else
    {
        info->cmpOper = GenTree::SwapRelop((genTreeOps)funcAttr.m_func);
        info->cmpOp   = funcAttr.m_args[1];
        GetCheckedBoundArithInfo(funcAttr.m_args[0], info);
    }
}

ValueNum ValueNumStore::GetArrForLenVn(ValueNum vn)
{
    if (vn == NoVN)
    {
        return NoVN;
    }

    VNFuncApp funcAttr;
    if (GetVNFunc(vn, &funcAttr) && funcAttr.m_func == (VNFunc)GT_ARR_LENGTH)
    {
        return funcAttr.m_args[0];
    }
    return NoVN;
}

bool ValueNumStore::IsVNNewArr(ValueNum vn, VNFuncApp* funcApp)
{
    if (vn == NoVN)
    {
        return false;
    }
    bool result = false;
    if (GetVNFunc(vn, funcApp))
    {
        result = (funcApp->m_func == VNF_JitNewArr) || (funcApp->m_func == VNF_JitReadyToRunNewArr);
    }
    return result;
}

int ValueNumStore::GetNewArrSize(ValueNum vn)
{
    VNFuncApp funcApp;
    if (IsVNNewArr(vn, &funcApp))
    {
        ValueNum arg1VN = funcApp.m_args[1];
        if (IsVNConstant(arg1VN) && TypeOfVN(arg1VN) == TYP_INT)
        {
            return ConstantValue<int>(arg1VN);
        }
    }
    return 0;
}

bool ValueNumStore::IsVNArrLen(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    VNFuncApp funcAttr;
    return (GetVNFunc(vn, &funcAttr) && funcAttr.m_func == (VNFunc)GT_ARR_LENGTH);
}

bool ValueNumStore::IsVNCheckedBound(ValueNum vn)
{
    bool dummy;
    if (m_checkedBoundVNs.TryGetValue(vn, &dummy))
    {
        // This VN appeared as the conservative VN of the length argument of some
        // GT_ARR_BOUND node.
        return true;
    }
    if (IsVNArrLen(vn))
    {
        // Even if we haven't seen this VN in a bounds check, if it is an array length
        // VN then consider it a checked bound VN.  This facilitates better bounds check
        // removal by ensuring that compares against array lengths get put in the
        // optCseCheckedBoundMap; such an array length might get CSEd with one that was
        // directly used in a bounds check, and having the map entry will let us update
        // the compare's VN so that OptimizeRangeChecks can recognize such compares.
        return true;
    }

    return false;
}

void ValueNumStore::SetVNIsCheckedBound(ValueNum vn)
{
    // This is meant to flag VNs for lengths that aren't known at compile time, so we can
    // form and propagate assertions about them.  Ensure that callers filter out constant
    // VNs since they're not what we're looking to flag, and assertion prop can reason
    // directly about constants.
    assert(!IsVNConstant(vn));
    m_checkedBoundVNs.AddOrUpdate(vn, true);
}

ValueNum ValueNumStore::EvalMathFuncUnary(var_types typ, CorInfoIntrinsics gtMathFN, ValueNum arg0VN)
{
    assert(arg0VN == VNNormVal(arg0VN));

    // If the math intrinsic is not implemented by target-specific instructions, such as implemented
    // by user calls, then don't do constant folding on it. This minimizes precision loss.

    if (IsVNConstant(arg0VN) && Compiler::IsTargetIntrinsic(gtMathFN))
    {
        assert(varTypeIsFloating(TypeOfVN(arg0VN)));

        if (typ == TYP_DOUBLE)
        {
            // Both operand and its result must be of the same floating point type.
            assert(typ == TypeOfVN(arg0VN));
            double arg0Val = GetConstantDouble(arg0VN);

            double res = 0.0;
            switch (gtMathFN)
            {
                case CORINFO_INTRINSIC_Sin:
                    res = sin(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Cos:
                    res = cos(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Sqrt:
                    res = sqrt(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Abs:
                    res = fabs(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Round:
                    res = FloatingPointUtils::round(arg0Val);
                    break;
                default:
                    unreached(); // the above are the only math intrinsics at the time of this writing.
            }

            return VNForDoubleCon(res);
        }
        else if (typ == TYP_FLOAT)
        {
            // Both operand and its result must be of the same floating point type.
            assert(typ == TypeOfVN(arg0VN));
            float arg0Val = GetConstantSingle(arg0VN);

            float res = 0.0f;
            switch (gtMathFN)
            {
                case CORINFO_INTRINSIC_Sin:
                    res = sinf(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Cos:
                    res = cosf(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Sqrt:
                    res = sqrtf(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Abs:
                    res = fabsf(arg0Val);
                    break;
                case CORINFO_INTRINSIC_Round:
                    res = FloatingPointUtils::round(arg0Val);
                    break;
                default:
                    unreached(); // the above are the only math intrinsics at the time of this writing.
            }

            return VNForFloatCon(res);
        }
        else
        {
            // CORINFO_INTRINSIC_Round is currently the only intrinsic that takes floating-point arguments
            // and that returns a non floating-point result.

            assert(typ == TYP_INT);
            assert(gtMathFN == CORINFO_INTRINSIC_Round);

            int res = 0;

            switch (TypeOfVN(arg0VN))
            {
                case TYP_DOUBLE:
                {
                    double arg0Val = GetConstantDouble(arg0VN);
                    res            = int(FloatingPointUtils::round(arg0Val));
                    break;
                }
                case TYP_FLOAT:
                {
                    float arg0Val = GetConstantSingle(arg0VN);
                    res           = int(FloatingPointUtils::round(arg0Val));
                    break;
                }
                default:
                    unreached();
            }

            return VNForIntCon(res);
        }
    }
    else
    {
        assert(typ == TYP_DOUBLE || typ == TYP_FLOAT || (typ == TYP_INT && gtMathFN == CORINFO_INTRINSIC_Round));

        VNFunc vnf = VNF_Boundary;
        switch (gtMathFN)
        {
            case CORINFO_INTRINSIC_Sin:
                vnf = VNF_Sin;
                break;
            case CORINFO_INTRINSIC_Cos:
                vnf = VNF_Cos;
                break;
            case CORINFO_INTRINSIC_Sqrt:
                vnf = VNF_Sqrt;
                break;
            case CORINFO_INTRINSIC_Abs:
                vnf = VNF_Abs;
                break;
            case CORINFO_INTRINSIC_Round:
                if (typ == TYP_DOUBLE)
                {
                    vnf = VNF_RoundDouble;
                }
                else if (typ == TYP_FLOAT)
                {
                    vnf = VNF_RoundFloat;
                }
                else if (typ == TYP_INT)
                {
                    vnf = VNF_RoundInt;
                }
                else
                {
                    noway_assert(!"Invalid INTRINSIC_Round");
                }
                break;
            case CORINFO_INTRINSIC_Cosh:
                vnf = VNF_Cosh;
                break;
            case CORINFO_INTRINSIC_Sinh:
                vnf = VNF_Sinh;
                break;
            case CORINFO_INTRINSIC_Tan:
                vnf = VNF_Tan;
                break;
            case CORINFO_INTRINSIC_Tanh:
                vnf = VNF_Tanh;
                break;
            case CORINFO_INTRINSIC_Asin:
                vnf = VNF_Asin;
                break;
            case CORINFO_INTRINSIC_Acos:
                vnf = VNF_Acos;
                break;
            case CORINFO_INTRINSIC_Atan:
                vnf = VNF_Atan;
                break;
            case CORINFO_INTRINSIC_Log10:
                vnf = VNF_Log10;
                break;
            case CORINFO_INTRINSIC_Exp:
                vnf = VNF_Exp;
                break;
            case CORINFO_INTRINSIC_Ceiling:
                vnf = VNF_Ceiling;
                break;
            case CORINFO_INTRINSIC_Floor:
                vnf = VNF_Floor;
                break;
            default:
                unreached(); // the above are the only math intrinsics at the time of this writing.
        }

        return VNForFunc(typ, vnf, arg0VN);
    }
}

ValueNum ValueNumStore::EvalMathFuncBinary(var_types typ, CorInfoIntrinsics gtMathFN, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(varTypeIsFloating(typ));
    assert(arg0VN == VNNormVal(arg0VN));
    assert(arg1VN == VNNormVal(arg1VN));

    VNFunc vnf = VNF_Boundary;

    // Currently, none of the binary math intrinsic are implemented by target-specific instructions.
    // To minimize precision loss, do not do constant folding on them.

    switch (gtMathFN)
    {
        case CORINFO_INTRINSIC_Atan2:
            vnf = VNF_Atan2;
            break;

        case CORINFO_INTRINSIC_Pow:
            vnf = VNF_Pow;
            break;

        default:
            unreached(); // the above are the only binary math intrinsics at the time of this writing.
    }

    return VNForFunc(typ, vnf, arg0VN, arg1VN);
}

bool ValueNumStore::IsVNFunc(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    switch (c->m_attribs)
    {
        case CEA_NotAField:
        case CEA_Func0:
        case CEA_Func1:
        case CEA_Func2:
        case CEA_Func3:
        case CEA_Func4:
            return true;
        default:
            return false;
    }
}

bool ValueNumStore::GetVNFunc(ValueNum vn, VNFuncApp* funcApp)
{
    if (vn == NoVN)
    {
        return false;
    }

    Chunk*   c      = m_chunks.GetNoExpand(GetChunkNum(vn));
    unsigned offset = ChunkOffset(vn);
    assert(offset < c->m_numUsed);
    switch (c->m_attribs)
    {
        case CEA_Func4:
        {
            VNDefFunc4Arg* farg4 = &reinterpret_cast<VNDefFunc4Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg4->m_func;
            funcApp->m_arity     = 4;
            funcApp->m_args[0]   = farg4->m_arg0;
            funcApp->m_args[1]   = farg4->m_arg1;
            funcApp->m_args[2]   = farg4->m_arg2;
            funcApp->m_args[3]   = farg4->m_arg3;
            return true;
        }
        case CEA_Func3:
        {
            VNDefFunc3Arg* farg3 = &reinterpret_cast<VNDefFunc3Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg3->m_func;
            funcApp->m_arity     = 3;
            funcApp->m_args[0]   = farg3->m_arg0;
            funcApp->m_args[1]   = farg3->m_arg1;
            funcApp->m_args[2]   = farg3->m_arg2;
            return true;
        }
        case CEA_Func2:
        {
            VNDefFunc2Arg* farg2 = &reinterpret_cast<VNDefFunc2Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg2->m_func;
            funcApp->m_arity     = 2;
            funcApp->m_args[0]   = farg2->m_arg0;
            funcApp->m_args[1]   = farg2->m_arg1;
            return true;
        }
        case CEA_Func1:
        {
            VNDefFunc1Arg* farg1 = &reinterpret_cast<VNDefFunc1Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg1->m_func;
            funcApp->m_arity     = 1;
            funcApp->m_args[0]   = farg1->m_arg0;
            return true;
        }
        case CEA_Func0:
        {
            VNDefFunc0Arg* farg0 = &reinterpret_cast<VNDefFunc0Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg0->m_func;
            funcApp->m_arity     = 0;
            return true;
        }
        case CEA_NotAField:
        {
            funcApp->m_func  = VNF_NotAField;
            funcApp->m_arity = 0;
            return true;
        }
        default:
            return false;
    }
}

ValueNum ValueNumStore::VNForRefInAddr(ValueNum vn)
{
    var_types vnType = TypeOfVN(vn);
    if (vnType == TYP_REF)
    {
        return vn;
    }
    // Otherwise...
    assert(vnType == TYP_BYREF);
    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp))
    {
        assert(funcApp.m_arity == 2 && (funcApp.m_func == VNFunc(GT_ADD) || funcApp.m_func == VNFunc(GT_SUB)));
        var_types vnArg0Type = TypeOfVN(funcApp.m_args[0]);
        if (vnArg0Type == TYP_REF || vnArg0Type == TYP_BYREF)
        {
            return VNForRefInAddr(funcApp.m_args[0]);
        }
        else
        {
            assert(funcApp.m_func == VNFunc(GT_ADD) &&
                   (TypeOfVN(funcApp.m_args[1]) == TYP_REF || TypeOfVN(funcApp.m_args[1]) == TYP_BYREF));
            return VNForRefInAddr(funcApp.m_args[1]);
        }
    }
    else
    {
        assert(IsVNConstant(vn));
        return vn;
    }
}

bool ValueNumStore::VNIsValid(ValueNum vn)
{
    ChunkNum cn = GetChunkNum(vn);
    if (cn >= m_chunks.Size())
    {
        return false;
    }
    // Otherwise...
    Chunk* c = m_chunks.GetNoExpand(cn);
    return ChunkOffset(vn) < c->m_numUsed;
}

#ifdef DEBUG

void ValueNumStore::vnDump(Compiler* comp, ValueNum vn, bool isPtr)
{
    printf(" {");
    if (vn == NoVN)
    {
        printf("NoVN");
    }
    else if (IsVNHandle(vn))
    {
        ssize_t val = ConstantValue<ssize_t>(vn);
        printf("Hnd const: 0x%p", dspPtr(val));
    }
    else if (IsVNConstant(vn))
    {
        var_types vnt = TypeOfVN(vn);
        switch (vnt)
        {
            case TYP_BOOL:
            case TYP_BYTE:
            case TYP_UBYTE:
            case TYP_CHAR:
            case TYP_SHORT:
            case TYP_USHORT:
            case TYP_INT:
            case TYP_UINT:
            {
                int val = ConstantValue<int>(vn);
                if (isPtr)
                {
                    printf("PtrCns[%p]", dspPtr(val));
                }
                else
                {
                    printf("IntCns");
                    if ((val > -1000) && (val < 1000))
                    {
                        printf(" %ld", val);
                    }
                    else
                    {
                        printf(" 0x%X", val);
                    }
                }
            }
            break;
            case TYP_LONG:
            case TYP_ULONG:
            {
                INT64 val = ConstantValue<INT64>(vn);
                if (isPtr)
                {
                    printf("LngPtrCns: 0x%p", dspPtr(val));
                }
                else
                {
                    printf("LngCns: ");
                    if ((val > -1000) && (val < 1000))
                    {
                        printf(" %ld", val);
                    }
                    else if ((val & 0xFFFFFFFF00000000LL) == 0)
                    {
                        printf(" 0x%X", val);
                    }
                    else
                    {
                        printf(" 0x%llx", val);
                    }
                }
            }
            break;
            case TYP_FLOAT:
                printf("FltCns[%f]", ConstantValue<float>(vn));
                break;
            case TYP_DOUBLE:
                printf("DblCns[%f]", ConstantValue<double>(vn));
                break;
            case TYP_REF:
            case TYP_ARRAY:
                if (vn == VNForNull())
                {
                    printf("null");
                }
                else if (vn == VNForVoid())
                {
                    printf("void");
                }
                else
                {
                    assert(vn == VNForZeroMap());
                    printf("zeroMap");
                }
                break;
            case TYP_BYREF:
                printf("byrefVal");
                break;
            case TYP_STRUCT:
#ifdef FEATURE_SIMD
            case TYP_SIMD8:
            case TYP_SIMD12:
            case TYP_SIMD16:
            case TYP_SIMD32:
#endif // FEATURE_SIMD
                printf("structVal");
                break;

            // These should be unreached.
            default:
                unreached();
        }
    }
    else if (IsVNCompareCheckedBound(vn))
    {
        CompareCheckedBoundArithInfo info;
        GetCompareCheckedBound(vn, &info);
        info.dump(this);
    }
    else if (IsVNCompareCheckedBoundArith(vn))
    {
        CompareCheckedBoundArithInfo info;
        GetCompareCheckedBoundArithInfo(vn, &info);
        info.dump(this);
    }
    else if (IsVNFunc(vn))
    {
        VNFuncApp funcApp;
        GetVNFunc(vn, &funcApp);
        // A few special cases...
        switch (funcApp.m_func)
        {
            case VNF_FieldSeq:
                vnDumpFieldSeq(comp, &funcApp, true);
                break;
            case VNF_MapSelect:
                vnDumpMapSelect(comp, &funcApp);
                break;
            case VNF_MapStore:
                vnDumpMapStore(comp, &funcApp);
                break;
            default:
                printf("%s(", VNFuncName(funcApp.m_func));
                for (unsigned i = 0; i < funcApp.m_arity; i++)
                {
                    if (i > 0)
                    {
                        printf(", ");
                    }

                    printf(STR_VN "%x", funcApp.m_args[i]);

#if FEATURE_VN_DUMP_FUNC_ARGS
                    printf("=");
                    vnDump(comp, funcApp.m_args[i]);
#endif
                }
                printf(")");
        }
    }
    else
    {
        // Otherwise, just a VN with no structure; print just the VN.
        printf("%x", vn);
    }
    printf("}");
}

void ValueNumStore::vnDumpFieldSeq(Compiler* comp, VNFuncApp* fieldSeq, bool isHead)
{
    assert(fieldSeq->m_func == VNF_FieldSeq); // Precondition.
    // First arg is the field handle VN.
    assert(IsVNConstant(fieldSeq->m_args[0]) && TypeOfVN(fieldSeq->m_args[0]) == TYP_I_IMPL);
    ssize_t fieldHndVal = ConstantValue<ssize_t>(fieldSeq->m_args[0]);
    bool    hasTail     = (fieldSeq->m_args[1] != VNForNull());

    if (isHead && hasTail)
    {
        printf("(");
    }

    CORINFO_FIELD_HANDLE fldHnd = CORINFO_FIELD_HANDLE(fieldHndVal);
    if (fldHnd == FieldSeqStore::FirstElemPseudoField)
    {
        printf("#FirstElem");
    }
    else if (fldHnd == FieldSeqStore::ConstantIndexPseudoField)
    {
        printf("#ConstantIndex");
    }
    else
    {
        const char* modName;
        const char* fldName = m_pComp->eeGetFieldName(fldHnd, &modName);
        printf("%s", fldName);
    }

    if (hasTail)
    {
        printf(", ");
        assert(IsVNFunc(fieldSeq->m_args[1]));
        VNFuncApp tail;
        GetVNFunc(fieldSeq->m_args[1], &tail);
        vnDumpFieldSeq(comp, &tail, false);
    }

    if (isHead && hasTail)
    {
        printf(")");
    }
}

void ValueNumStore::vnDumpMapSelect(Compiler* comp, VNFuncApp* mapSelect)
{
    assert(mapSelect->m_func == VNF_MapSelect); // Precondition.

    ValueNum mapVN   = mapSelect->m_args[0]; // First arg is the map id
    ValueNum indexVN = mapSelect->m_args[1]; // Second arg is the index

    comp->vnPrint(mapVN, 0);
    printf("[");
    comp->vnPrint(indexVN, 0);
    printf("]");
}

void ValueNumStore::vnDumpMapStore(Compiler* comp, VNFuncApp* mapStore)
{
    assert(mapStore->m_func == VNF_MapStore); // Precondition.

    ValueNum mapVN    = mapStore->m_args[0]; // First arg is the map id
    ValueNum indexVN  = mapStore->m_args[1]; // Second arg is the index
    ValueNum newValVN = mapStore->m_args[2]; // Third arg is the new value

    comp->vnPrint(mapVN, 0);
    printf("[");
    comp->vnPrint(indexVN, 0);
    printf(" := ");
    comp->vnPrint(newValVN, 0);
    printf("]");
}
#endif // DEBUG

// Static fields, methods.
static UINT8      vnfOpAttribs[VNF_COUNT];
static genTreeOps genTreeOpsIllegalAsVNFunc[] = {GT_IND, // When we do heap memory.
                                                 GT_NULLCHECK, GT_QMARK, GT_COLON, GT_LOCKADD, GT_XADD, GT_XCHG,
                                                 GT_CMPXCHG, GT_LCLHEAP, GT_BOX,

                                                 // These need special semantics:
                                                 GT_COMMA, // == second argument (but with exception(s) from first).
                                                 GT_ADDR, GT_ARR_BOUNDS_CHECK,
                                                 GT_OBJ,      // May reference heap memory.
                                                 GT_BLK,      // May reference heap memory.
                                                 GT_INIT_VAL, // Not strictly a pass-through.

                                                 // These control-flow operations need no values.
                                                 GT_JTRUE, GT_RETURN, GT_SWITCH, GT_RETFILT, GT_CKFINITE};

UINT8* ValueNumStore::s_vnfOpAttribs = nullptr;

void ValueNumStore::InitValueNumStoreStatics()
{
    // Make sure we've gotten constants right...
    assert(unsigned(VNFOA_Arity) == (1 << VNFOA_ArityShift));
    assert(unsigned(VNFOA_AfterArity) == (unsigned(VNFOA_Arity) << VNFOA_ArityBits));

    s_vnfOpAttribs = &vnfOpAttribs[0];
    for (unsigned i = 0; i < GT_COUNT; i++)
    {
        genTreeOps gtOper = static_cast<genTreeOps>(i);
        unsigned   arity  = 0;
        if (GenTree::OperIsUnary(gtOper))
        {
            arity = 1;
        }
        else if (GenTree::OperIsBinary(gtOper))
        {
            arity = 2;
        }
        // Since GT_ARR_BOUNDS_CHECK is not currently GTK_BINOP
        else if (gtOper == GT_ARR_BOUNDS_CHECK)
        {
            arity = 2;
        }
        vnfOpAttribs[i] |= (arity << VNFOA_ArityShift);

        if (GenTree::OperIsCommutative(gtOper))
        {
            vnfOpAttribs[i] |= VNFOA_Commutative;
        }
    }

    // I so wish this wasn't the best way to do this...

    int vnfNum = VNF_Boundary + 1; // The macro definition below will update this after using it.

#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic)                                               \
    if (commute)                                                                                                       \
        vnfOpAttribs[vnfNum] |= VNFOA_Commutative;                                                                     \
    if (knownNonNull)                                                                                                  \
        vnfOpAttribs[vnfNum] |= VNFOA_KnownNonNull;                                                                    \
    if (sharedStatic)                                                                                                  \
        vnfOpAttribs[vnfNum] |= VNFOA_SharedStatic;                                                                    \
    vnfOpAttribs[vnfNum] |= (arity << VNFOA_ArityShift);                                                               \
    vnfNum++;

#include "valuenumfuncs.h"
#undef ValueNumFuncDef

    unsigned n = sizeof(genTreeOpsIllegalAsVNFunc) / sizeof(genTreeOps);
    for (unsigned i = 0; i < n; i++)
    {
        vnfOpAttribs[genTreeOpsIllegalAsVNFunc[i]] |= VNFOA_IllegalGenTreeOp;
    }
}

#ifdef DEBUG
// Define the name array.
#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic) #vnf,

const char* ValueNumStore::VNFuncNameArr[] = {
#include "valuenumfuncs.h"
#undef ValueNumFuncDef
};

// static
const char* ValueNumStore::VNFuncName(VNFunc vnf)
{
    if (vnf < VNF_Boundary)
    {
        return GenTree::OpName(genTreeOps(vnf));
    }
    else
    {
        return VNFuncNameArr[vnf - (VNF_Boundary + 1)];
    }
}

static const char* s_reservedNameArr[] = {
    "$VN.Recursive",    // -2  RecursiveVN
    "$VN.No",           // -1  NoVN
    "$VN.Null",         //  0  VNForNull()
    "$VN.ZeroMap",      //  1  VNForZeroMap()
    "$VN.ReadOnlyHeap", //  2  VNForROH()
    "$VN.Void",         //  3  VNForVoid()
    "$VN.EmptyExcSet"   //  4  VNForEmptyExcSet()
};

// Returns the string name of "vn" when it is a reserved value number, nullptr otherwise
// static
const char* ValueNumStore::reservedName(ValueNum vn)
{
    int val = vn - ValueNumStore::RecursiveVN; // Add two, making 'RecursiveVN' equal to zero
    int max = ValueNumStore::SRC_NumSpecialRefConsts - ValueNumStore::RecursiveVN;

    if ((val >= 0) && (val < max))
    {
        return s_reservedNameArr[val];
    }
    return nullptr;
}

#endif // DEBUG

// Returns true if "vn" is a reserved value number

// static
bool ValueNumStore::isReservedVN(ValueNum vn)
{
    int val = vn - ValueNumStore::RecursiveVN; // Adding two, making 'RecursiveVN' equal to zero
    int max = ValueNumStore::SRC_NumSpecialRefConsts - ValueNumStore::RecursiveVN;

    if ((val >= 0) && (val < max))
    {
        return true;
    }
    return false;
}

#ifdef DEBUG
void ValueNumStore::RunTests(Compiler* comp)
{
    VNFunc VNF_Add = GenTreeOpToVNFunc(GT_ADD);

    ValueNumStore* vns    = new (comp->getAllocatorDebugOnly()) ValueNumStore(comp, comp->getAllocatorDebugOnly());
    ValueNum       vnNull = VNForNull();
    assert(vnNull == VNForNull());

    ValueNum vnFor1 = vns->VNForIntCon(1);
    assert(vnFor1 == vns->VNForIntCon(1));
    assert(vns->TypeOfVN(vnFor1) == TYP_INT);
    assert(vns->IsVNConstant(vnFor1));
    assert(vns->ConstantValue<int>(vnFor1) == 1);

    ValueNum vnFor100 = vns->VNForIntCon(100);
    assert(vnFor100 == vns->VNForIntCon(100));
    assert(vnFor100 != vnFor1);
    assert(vns->TypeOfVN(vnFor100) == TYP_INT);
    assert(vns->IsVNConstant(vnFor100));
    assert(vns->ConstantValue<int>(vnFor100) == 100);

    ValueNum vnFor1F = vns->VNForFloatCon(1.0f);
    assert(vnFor1F == vns->VNForFloatCon(1.0f));
    assert(vnFor1F != vnFor1 && vnFor1F != vnFor100);
    assert(vns->TypeOfVN(vnFor1F) == TYP_FLOAT);
    assert(vns->IsVNConstant(vnFor1F));
    assert(vns->ConstantValue<float>(vnFor1F) == 1.0f);

    ValueNum vnFor1D = vns->VNForDoubleCon(1.0);
    assert(vnFor1D == vns->VNForDoubleCon(1.0));
    assert(vnFor1D != vnFor1F && vnFor1D != vnFor1 && vnFor1D != vnFor100);
    assert(vns->TypeOfVN(vnFor1D) == TYP_DOUBLE);
    assert(vns->IsVNConstant(vnFor1D));
    assert(vns->ConstantValue<double>(vnFor1D) == 1.0);

    ValueNum vnRandom1   = vns->VNForExpr(nullptr, TYP_INT);
    ValueNum vnForFunc2a = vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnRandom1);
    assert(vnForFunc2a == vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnRandom1));
    assert(vnForFunc2a != vnFor1D && vnForFunc2a != vnFor1F && vnForFunc2a != vnFor1 && vnForFunc2a != vnRandom1);
    assert(vns->TypeOfVN(vnForFunc2a) == TYP_INT);
    assert(!vns->IsVNConstant(vnForFunc2a));
    assert(vns->IsVNFunc(vnForFunc2a));
    VNFuncApp fa2a;
    bool      b = vns->GetVNFunc(vnForFunc2a, &fa2a);
    assert(b);
    assert(fa2a.m_func == VNF_Add && fa2a.m_arity == 2 && fa2a.m_args[0] == vnFor1 && fa2a.m_args[1] == vnRandom1);

    ValueNum vnForFunc2b = vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnFor100);
    assert(vnForFunc2b == vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnFor100));
    assert(vnForFunc2b != vnFor1D && vnForFunc2b != vnFor1F && vnForFunc2b != vnFor1 && vnForFunc2b != vnFor100);
    assert(vns->TypeOfVN(vnForFunc2b) == TYP_INT);
    assert(vns->IsVNConstant(vnForFunc2b));
    assert(vns->ConstantValue<int>(vnForFunc2b) == 101);

    // printf("Did ValueNumStore::RunTests.\n");
}
#endif // DEBUG

typedef ExpandArrayStack<BasicBlock*> BlockStack;

// This represents the "to do" state of the value number computation.
struct ValueNumberState
{
    // These two stacks collectively represent the set of blocks that are candidates for
    // processing, because at least one predecessor has been processed.  Blocks on "m_toDoAllPredsDone"
    // have had *all* predecessors processed, and thus are candidates for some extra optimizations.
    // Blocks on "m_toDoNotAllPredsDone" have at least one predecessor that has not been processed.
    // Blocks are initially on "m_toDoNotAllPredsDone" may be moved to "m_toDoAllPredsDone" when their last
    // unprocessed predecessor is processed, thus maintaining the invariants.
    BlockStack m_toDoAllPredsDone;
    BlockStack m_toDoNotAllPredsDone;

    Compiler* m_comp;

    // TBD: This should really be a bitset...
    // For now:
    // first bit indicates completed,
    // second bit indicates that it's been pushed on all-done stack,
    // third bit indicates that it's been pushed on not-all-done stack.
    BYTE* m_visited;

    enum BlockVisitBits
    {
        BVB_complete     = 0x1,
        BVB_onAllDone    = 0x2,
        BVB_onNotAllDone = 0x4,
    };

    bool GetVisitBit(unsigned bbNum, BlockVisitBits bvb)
    {
        return (m_visited[bbNum] & bvb) != 0;
    }
    void SetVisitBit(unsigned bbNum, BlockVisitBits bvb)
    {
        m_visited[bbNum] |= bvb;
    }

    ValueNumberState(Compiler* comp)
        : m_toDoAllPredsDone(comp->getAllocator(), /*minSize*/ 4)
        , m_toDoNotAllPredsDone(comp->getAllocator(), /*minSize*/ 4)
        , m_comp(comp)
        , m_visited(new (comp, CMK_ValueNumber) BYTE[comp->fgBBNumMax + 1]())
    {
    }

    BasicBlock* ChooseFromNotAllPredsDone()
    {
        assert(m_toDoAllPredsDone.Size() == 0);
        // If we have no blocks with all preds done, then (ideally, if all cycles have been captured by loops)
        // we must have at least one block within a loop.  We want to do the loops first.  Doing a loop entry block
        // should break the cycle, making the rest of the body of the loop (unless there's a nested loop) doable by the
        // all-preds-done rule.  If several loop entry blocks are available, at least one should have all non-loop preds
        // done -- we choose that.
        for (unsigned i = 0; i < m_toDoNotAllPredsDone.Size(); i++)
        {
            BasicBlock* cand = m_toDoNotAllPredsDone.Get(i);

            // Skip any already-completed blocks (a block may have all its preds finished, get added to the
            // all-preds-done todo set, and get processed there).  Do this by moving the last one down, to
            // keep the array compact.
            while (GetVisitBit(cand->bbNum, BVB_complete))
            {
                if (i + 1 < m_toDoNotAllPredsDone.Size())
                {
                    cand = m_toDoNotAllPredsDone.Pop();
                    m_toDoNotAllPredsDone.Set(i, cand);
                }
                else
                {
                    // "cand" is the last element; delete it.
                    (void)m_toDoNotAllPredsDone.Pop();
                    break;
                }
            }
            // We may have run out of non-complete candidates above.  If so, we're done.
            if (i == m_toDoNotAllPredsDone.Size())
            {
                break;
            }

            // See if "cand" is a loop entry.
            unsigned lnum;
            if (m_comp->optBlockIsLoopEntry(cand, &lnum))
            {
                // "lnum" is the innermost loop of which "cand" is the entry; find the outermost.
                unsigned lnumPar = m_comp->optLoopTable[lnum].lpParent;
                while (lnumPar != BasicBlock::NOT_IN_LOOP)
                {
                    if (m_comp->optLoopTable[lnumPar].lpEntry == cand)
                    {
                        lnum = lnumPar;
                    }
                    else
                    {
                        break;
                    }
                    lnumPar = m_comp->optLoopTable[lnumPar].lpParent;
                }

                bool allNonLoopPredsDone = true;
                for (flowList* pred = m_comp->BlockPredsWithEH(cand); pred != nullptr; pred = pred->flNext)
                {
                    BasicBlock* predBlock = pred->flBlock;
                    if (!m_comp->optLoopTable[lnum].lpContains(predBlock))
                    {
                        if (!GetVisitBit(predBlock->bbNum, BVB_complete))
                        {
                            allNonLoopPredsDone = false;
                        }
                    }
                }
                if (allNonLoopPredsDone)
                {
                    return cand;
                }
            }
        }

        // If we didn't find a loop entry block with all non-loop preds done above, then return a random member (if
        // there is one).
        if (m_toDoNotAllPredsDone.Size() == 0)
        {
            return nullptr;
        }
        else
        {
            return m_toDoNotAllPredsDone.Pop();
        }
    }

// Debugging output that is too detailed for a normal JIT dump...
#define DEBUG_VN_VISIT 0

    // Record that "blk" has been visited, and add any unvisited successors of "blk" to the appropriate todo set.
    void FinishVisit(BasicBlock* blk)
    {
#ifdef DEBUG_VN_VISIT
        JITDUMP("finish(BB%02u).\n", blk->bbNum);
#endif // DEBUG_VN_VISIT

        SetVisitBit(blk->bbNum, BVB_complete);

        for (BasicBlock* succ : blk->GetAllSuccs(m_comp))
        {
#ifdef DEBUG_VN_VISIT
            JITDUMP("   Succ(BB%02u).\n", succ->bbNum);
#endif // DEBUG_VN_VISIT

            if (GetVisitBit(succ->bbNum, BVB_complete))
            {
                continue;
            }
#ifdef DEBUG_VN_VISIT
            JITDUMP("     Not yet completed.\n");
#endif // DEBUG_VN_VISIT

            bool allPredsVisited = true;
            for (flowList* pred = m_comp->BlockPredsWithEH(succ); pred != nullptr; pred = pred->flNext)
            {
                BasicBlock* predBlock = pred->flBlock;
                if (!GetVisitBit(predBlock->bbNum, BVB_complete))
                {
                    allPredsVisited = false;
                    break;
                }
            }

            if (allPredsVisited)
            {
#ifdef DEBUG_VN_VISIT
                JITDUMP("     All preds complete, adding to allDone.\n");
#endif // DEBUG_VN_VISIT

                assert(!GetVisitBit(succ->bbNum, BVB_onAllDone)); // Only last completion of last succ should add to
                                                                  // this.
                m_toDoAllPredsDone.Push(succ);
                SetVisitBit(succ->bbNum, BVB_onAllDone);
            }
            else
            {
#ifdef DEBUG_VN_VISIT
                JITDUMP("     Not all preds complete  Adding to notallDone, if necessary...\n");
#endif // DEBUG_VN_VISIT

                if (!GetVisitBit(succ->bbNum, BVB_onNotAllDone))
                {
#ifdef DEBUG_VN_VISIT
                    JITDUMP("       Was necessary.\n");
#endif // DEBUG_VN_VISIT
                    m_toDoNotAllPredsDone.Push(succ);
                    SetVisitBit(succ->bbNum, BVB_onNotAllDone);
                }
            }
        }
    }

    bool ToDoExists()
    {
        return m_toDoAllPredsDone.Size() > 0 || m_toDoNotAllPredsDone.Size() > 0;
    }
};

void Compiler::fgValueNumber()
{
#ifdef DEBUG
    // This could be a JITDUMP, but some people find it convenient to set a breakpoint on the printf.
    if (verbose)
    {
        printf("\n*************** In fgValueNumber()\n");
    }
#endif

    // If we skipped SSA, skip VN as well.
    if (fgSsaPassesCompleted == 0)
    {
        return;
    }

    // Allocate the value number store.
    assert(fgVNPassesCompleted > 0 || vnStore == nullptr);
    if (fgVNPassesCompleted == 0)
    {
        CompAllocator* allocator = new (this, CMK_ValueNumber) CompAllocator(this, CMK_ValueNumber);
        vnStore                  = new (this, CMK_ValueNumber) ValueNumStore(this, allocator);
    }
    else
    {
        ValueNumPair noVnp;
        // Make sure the memory SSA names have no value numbers.
        for (unsigned i = 0; i < lvMemoryNumSsaNames; i++)
        {
            lvMemoryPerSsaData.GetRef(i).m_vnPair = noVnp;
        }
        for (BasicBlock* blk = fgFirstBB; blk != nullptr; blk = blk->bbNext)
        {
            // Now iterate over the block's statements, and their trees.
            for (GenTreePtr stmts = blk->FirstNonPhiDef(); stmts != nullptr; stmts = stmts->gtNext)
            {
                assert(stmts->IsStatement());
                for (GenTreePtr tree = stmts->gtStmt.gtStmtList; tree; tree = tree->gtNext)
                {
                    tree->gtVNPair.SetBoth(ValueNumStore::NoVN);
                }
            }
        }
    }

    // Compute the side effects of loops.
    optComputeLoopSideEffects();

    // At the block level, we will use a modified worklist algorithm.  We will have two
    // "todo" sets of unvisited blocks.  Blocks (other than the entry block) are put in a
    // todo set only when some predecessor has been visited, so all blocks have at least one
    // predecessor visited.  The distinction between the two sets is whether *all* predecessors have
    // already been visited.  We visit such blocks preferentially if they exist, since phi definitions
    // in such blocks will have all arguments defined, enabling a simplification in the case that all
    // arguments to the phi have the same VN.  If no such blocks exist, we pick a block with at least
    // one unvisited predecessor.  In this case, we assign a new VN for phi definitions.

    // Start by giving incoming arguments value numbers.
    // Also give must-init vars a zero of their type.
    for (unsigned i = 0; i < lvaCount; i++)
    {
        LclVarDsc* varDsc = &lvaTable[i];
        if (varDsc->lvIsParam)
        {
            // We assume that code equivalent to this variable initialization loop
            // has been performed when doing SSA naming, so that all the variables we give
            // initial VNs to here have been given initial SSA definitions there.
            // SSA numbers always start from FIRST_SSA_NUM, and we give the value number to SSA name FIRST_SSA_NUM.
            // We use the VNF_InitVal(i) from here so we know that this value is loop-invariant
            // in all loops.
            ValueNum      initVal = vnStore->VNForFunc(varDsc->TypeGet(), VNF_InitVal, vnStore->VNForIntCon(i));
            LclSsaVarDsc* ssaDef  = varDsc->GetPerSsaData(SsaConfig::FIRST_SSA_NUM);
            ssaDef->m_vnPair.SetBoth(initVal);
            ssaDef->m_defLoc.m_blk = fgFirstBB;
        }
        else if (info.compInitMem || varDsc->lvMustInit ||
                 (varDsc->lvTracked && VarSetOps::IsMember(this, fgFirstBB->bbLiveIn, varDsc->lvVarIndex)))
        {
            // The last clause covers the use-before-def variables (the ones that are live-in to the the first block),
            // these are variables that are read before being initialized (at least on some control flow paths)
            // if they are not must-init, then they get VNF_InitVal(i), as with the param case.)

            bool      isZeroed = (info.compInitMem || varDsc->lvMustInit);
            ValueNum  initVal  = ValueNumStore::NoVN; // We must assign a new value to initVal
            var_types typ      = varDsc->TypeGet();

            switch (typ)
            {
                case TYP_LCLBLK: // The outgoing args area for arm and x64
                case TYP_BLK:    // A blob of memory
                    // TYP_BLK is used for the EHSlots LclVar on x86 (aka shadowSPslotsVar)
                    // and for the lvaInlinedPInvokeFrameVar on x64, arm and x86
                    // The stack associated with these LclVars are not zero initialized
                    // thus we set 'initVN' to a new, unique VN.
                    //
                    initVal = vnStore->VNForExpr(fgFirstBB);
                    break;

                case TYP_BYREF:
                    if (isZeroed)
                    {
                        // LclVars of TYP_BYREF can be zero-inited.
                        initVal = vnStore->VNForByrefCon(0);
                    }
                    else
                    {
                        // Here we have uninitialized TYP_BYREF
                        initVal = vnStore->VNForFunc(typ, VNF_InitVal, vnStore->VNForIntCon(i));
                    }
                    break;

                default:
                    if (isZeroed)
                    {
                        // By default we will zero init these LclVars
                        initVal = vnStore->VNZeroForType(typ);
                    }
                    else
                    {
                        initVal = vnStore->VNForFunc(typ, VNF_InitVal, vnStore->VNForIntCon(i));
                    }
                    break;
            }
#ifdef _TARGET_X86_
            bool isVarargParam = (i == lvaVarargsBaseOfStkArgs || i == lvaVarargsHandleArg);
            if (isVarargParam)
                initVal = vnStore->VNForExpr(fgFirstBB); // a new, unique VN.
#endif
            assert(initVal != ValueNumStore::NoVN);

            LclSsaVarDsc* ssaDef = varDsc->GetPerSsaData(SsaConfig::FIRST_SSA_NUM);
            ssaDef->m_vnPair.SetBoth(initVal);
            ssaDef->m_defLoc.m_blk = fgFirstBB;
        }
    }
    // Give memory an initial value number (about which we know nothing).
    ValueNum memoryInitVal = vnStore->VNForFunc(TYP_REF, VNF_InitVal, vnStore->VNForIntCon(-1)); // Use -1 for memory.
    GetMemoryPerSsaData(SsaConfig::FIRST_SSA_NUM)->m_vnPair.SetBoth(memoryInitVal);
#ifdef DEBUG
    if (verbose)
    {
        printf("Memory Initial Value in BB01 is: " STR_VN "%x\n", memoryInitVal);
    }
#endif // DEBUG

    ValueNumberState vs(this);

    // Push the first block.  This has no preds.
    vs.m_toDoAllPredsDone.Push(fgFirstBB);

    while (vs.ToDoExists())
    {
        while (vs.m_toDoAllPredsDone.Size() > 0)
        {
            BasicBlock* toDo = vs.m_toDoAllPredsDone.Pop();
            fgValueNumberBlock(toDo);
            // Record that we've visited "toDo", and add successors to the right sets.
            vs.FinishVisit(toDo);
        }
        // OK, we've run out of blocks whose predecessors are done.  Pick one whose predecessors are not all done,
        // process that.  This may make more "all-done" blocks, so we'll go around the outer loop again --
        // note that this is an "if", not a "while" loop.
        if (vs.m_toDoNotAllPredsDone.Size() > 0)
        {
            BasicBlock* toDo = vs.ChooseFromNotAllPredsDone();
            if (toDo == nullptr)
            {
                continue; // We may have run out, because of completed blocks on the not-all-preds done list.
            }

            fgValueNumberBlock(toDo);
            // Record that we've visited "toDo", and add successors to the right sest.
            vs.FinishVisit(toDo);
        }
    }

#ifdef DEBUG
    JitTestCheckVN();
#endif // DEBUG

    fgVNPassesCompleted++;
}

void Compiler::fgValueNumberBlock(BasicBlock* blk)
{
    compCurBB = blk;

#ifdef DEBUG
    compCurStmtNum = blk->bbStmtNum - 1; // Set compCurStmtNum
#endif

    unsigned outerLoopNum = BasicBlock::NOT_IN_LOOP;

    // First: visit phi's.  If "newVNForPhis", give them new VN's.  If not,
    // first check to see if all phi args have the same value.
    GenTreePtr firstNonPhi = blk->FirstNonPhiDef();
    for (GenTreePtr phiDefs = blk->bbTreeList; phiDefs != firstNonPhi; phiDefs = phiDefs->gtNext)
    {
        // TODO-Cleanup: It has been proposed that we should have an IsPhiDef predicate.  We would use it
        // in Block::FirstNonPhiDef as well.
        GenTreePtr phiDef = phiDefs->gtStmt.gtStmtExpr;
        assert(phiDef->OperGet() == GT_ASG);
        GenTreeLclVarCommon* newSsaVar = phiDef->gtOp.gtOp1->AsLclVarCommon();

        ValueNumPair phiAppVNP;
        ValueNumPair sameVNPair;

        GenTreePtr phiFunc = phiDef->gtOp.gtOp2;

        // At this point a GT_PHI node should never have a nullptr for gtOp1
        // and the gtOp1 should always be a GT_LIST node.
        GenTreePtr phiOp1 = phiFunc->gtOp.gtOp1;
        noway_assert(phiOp1 != nullptr);
        noway_assert(phiOp1->OperGet() == GT_LIST);

        GenTreeArgList* phiArgs = phiFunc->gtOp.gtOp1->AsArgList();

        // A GT_PHI node should have more than one argument.
        noway_assert(phiArgs->Rest() != nullptr);

        GenTreeLclVarCommon* phiArg = phiArgs->Current()->AsLclVarCommon();
        phiArgs                     = phiArgs->Rest();

        phiAppVNP.SetBoth(vnStore->VNForIntCon(phiArg->gtSsaNum));
        bool allSameLib  = true;
        bool allSameCons = true;
        sameVNPair       = lvaTable[phiArg->gtLclNum].GetPerSsaData(phiArg->gtSsaNum)->m_vnPair;
        if (!sameVNPair.BothDefined())
        {
            allSameLib  = false;
            allSameCons = false;
        }
        while (phiArgs != nullptr)
        {
            phiArg = phiArgs->Current()->AsLclVarCommon();
            // Set the VN of the phi arg.
            phiArg->gtVNPair = lvaTable[phiArg->gtLclNum].GetPerSsaData(phiArg->gtSsaNum)->m_vnPair;
            if (phiArg->gtVNPair.BothDefined())
            {
                if (phiArg->gtVNPair.GetLiberal() != sameVNPair.GetLiberal())
                {
                    allSameLib = false;
                }
                if (phiArg->gtVNPair.GetConservative() != sameVNPair.GetConservative())
                {
                    allSameCons = false;
                }
            }
            else
            {
                allSameLib  = false;
                allSameCons = false;
            }
            ValueNumPair phiArgSsaVNP;
            phiArgSsaVNP.SetBoth(vnStore->VNForIntCon(phiArg->gtSsaNum));
            phiAppVNP = vnStore->VNPairForFunc(newSsaVar->TypeGet(), VNF_Phi, phiArgSsaVNP, phiAppVNP);
            phiArgs   = phiArgs->Rest();
        }

        ValueNumPair newVNPair;
        if (allSameLib)
        {
            newVNPair.SetLiberal(sameVNPair.GetLiberal());
        }
        else
        {
            newVNPair.SetLiberal(phiAppVNP.GetLiberal());
        }
        if (allSameCons)
        {
            newVNPair.SetConservative(sameVNPair.GetConservative());
        }
        else
        {
            newVNPair.SetConservative(phiAppVNP.GetConservative());
        }

        LclSsaVarDsc* newSsaVarDsc = lvaTable[newSsaVar->gtLclNum].GetPerSsaData(newSsaVar->GetSsaNum());
        // If all the args of the phi had the same value(s, liberal and conservative), then there wasn't really
        // a reason to have the phi -- just pass on that value.
        if (allSameLib && allSameCons)
        {
            newSsaVarDsc->m_vnPair = newVNPair;
#ifdef DEBUG
            if (verbose)
            {
                printf("In SSA definition, incoming phi args all same, set VN of local %d/%d to ",
                       newSsaVar->GetLclNum(), newSsaVar->GetSsaNum());
                vnpPrint(newVNPair, 1);
                printf(".\n");
            }
#endif // DEBUG
        }
        else
        {
            // They were not the same; we need to create a phi definition.
            ValueNumPair lclNumVNP;
            lclNumVNP.SetBoth(ValueNum(newSsaVar->GetLclNum()));
            ValueNumPair ssaNumVNP;
            ssaNumVNP.SetBoth(ValueNum(newSsaVar->GetSsaNum()));
            ValueNumPair vnPhiDef =
                vnStore->VNPairForFunc(newSsaVar->TypeGet(), VNF_PhiDef, lclNumVNP, ssaNumVNP, phiAppVNP);
            newSsaVarDsc->m_vnPair = vnPhiDef;
#ifdef DEBUG
            if (verbose)
            {
                printf("SSA definition: set VN of local %d/%d to ", newSsaVar->GetLclNum(), newSsaVar->GetSsaNum());
                vnpPrint(vnPhiDef, 1);
                printf(".\n");
            }
#endif // DEBUG
        }
    }

    // Now do the same for each MemoryKind.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        // Is there a phi for this block?
        if (blk->bbMemorySsaPhiFunc[memoryKind] == nullptr)
        {
            fgCurMemoryVN[memoryKind] = GetMemoryPerSsaData(blk->bbMemorySsaNumIn[memoryKind])->m_vnPair.GetLiberal();
            assert(fgCurMemoryVN[memoryKind] != ValueNumStore::NoVN);
        }
        else
        {
            if ((memoryKind == ByrefExposed) && byrefStatesMatchGcHeapStates)
            {
                // The update for GcHeap will copy its result to ByrefExposed.
                assert(memoryKind < GcHeap);
                assert(blk->bbMemorySsaPhiFunc[memoryKind] == blk->bbMemorySsaPhiFunc[GcHeap]);
                continue;
            }

            unsigned loopNum;
            ValueNum newMemoryVN;
            if (optBlockIsLoopEntry(blk, &loopNum))
            {
                newMemoryVN = fgMemoryVNForLoopSideEffects(memoryKind, blk, loopNum);
            }
            else
            {
                // Are all the VN's the same?
                BasicBlock::MemoryPhiArg* phiArgs = blk->bbMemorySsaPhiFunc[memoryKind];
                assert(phiArgs != BasicBlock::EmptyMemoryPhiDef);
                // There should be > 1 args to a phi.
                assert(phiArgs->m_nextArg != nullptr);
                ValueNum phiAppVN = vnStore->VNForIntCon(phiArgs->GetSsaNum());
                JITDUMP("  Building phi application: $%x = SSA# %d.\n", phiAppVN, phiArgs->GetSsaNum());
                bool     allSame = true;
                ValueNum sameVN  = GetMemoryPerSsaData(phiArgs->GetSsaNum())->m_vnPair.GetLiberal();
                if (sameVN == ValueNumStore::NoVN)
                {
                    allSame = false;
                }
                phiArgs = phiArgs->m_nextArg;
                while (phiArgs != nullptr)
                {
                    ValueNum phiArgVN = GetMemoryPerSsaData(phiArgs->GetSsaNum())->m_vnPair.GetLiberal();
                    if (phiArgVN == ValueNumStore::NoVN || phiArgVN != sameVN)
                    {
                        allSame = false;
                    }
#ifdef DEBUG
                    ValueNum oldPhiAppVN = phiAppVN;
#endif
                    unsigned phiArgSSANum   = phiArgs->GetSsaNum();
                    ValueNum phiArgSSANumVN = vnStore->VNForIntCon(phiArgSSANum);
                    JITDUMP("  Building phi application: $%x = SSA# %d.\n", phiArgSSANumVN, phiArgSSANum);
                    phiAppVN = vnStore->VNForFunc(TYP_REF, VNF_Phi, phiArgSSANumVN, phiAppVN);
                    JITDUMP("  Building phi application: $%x = phi($%x, $%x).\n", phiAppVN, phiArgSSANumVN,
                            oldPhiAppVN);
                    phiArgs = phiArgs->m_nextArg;
                }
                if (allSame)
                {
                    newMemoryVN = sameVN;
                }
                else
                {
                    newMemoryVN =
                        vnStore->VNForFunc(TYP_REF, VNF_PhiMemoryDef, vnStore->VNForHandle(ssize_t(blk), 0), phiAppVN);
                }
            }
            GetMemoryPerSsaData(blk->bbMemorySsaNumIn[memoryKind])->m_vnPair.SetLiberal(newMemoryVN);
            fgCurMemoryVN[memoryKind] = newMemoryVN;
            if ((memoryKind == GcHeap) && byrefStatesMatchGcHeapStates)
            {
                // Keep the CurMemoryVNs in sync
                fgCurMemoryVN[ByrefExposed] = newMemoryVN;
            }
        }
#ifdef DEBUG
        if (verbose)
        {
            printf("The SSA definition for %s (#%d) at start of BB%02u is ", memoryKindNames[memoryKind],
                   blk->bbMemorySsaNumIn[memoryKind], blk->bbNum);
            vnPrint(fgCurMemoryVN[memoryKind], 1);
            printf("\n");
        }
#endif // DEBUG
    }

    // Now iterate over the remaining statements, and their trees.
    for (GenTreePtr stmt = firstNonPhi; stmt != nullptr; stmt = stmt->gtNext)
    {
        assert(stmt->IsStatement());

#ifdef DEBUG
        compCurStmtNum++;
        if (verbose)
        {
            printf("\n***** BB%02u, stmt %d (before)\n", blk->bbNum, compCurStmtNum);
            gtDispTree(stmt->gtStmt.gtStmtExpr);
            printf("\n");
        }
#endif

        for (GenTreePtr tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
        {
            fgValueNumberTree(tree);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\n***** BB%02u, stmt %d (after)\n", blk->bbNum, compCurStmtNum);
            gtDispTree(stmt->gtStmt.gtStmtExpr);
            printf("\n");
            if (stmt->gtNext)
            {
                printf("---------\n");
            }
        }
#endif
    }

    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((memoryKind == GcHeap) && byrefStatesMatchGcHeapStates)
        {
            // The update to the shared SSA data will have already happened for ByrefExposed.
            assert(memoryKind > ByrefExposed);
            assert(blk->bbMemorySsaNumOut[memoryKind] == blk->bbMemorySsaNumOut[ByrefExposed]);
            assert(GetMemoryPerSsaData(blk->bbMemorySsaNumOut[memoryKind])->m_vnPair.GetLiberal() ==
                   fgCurMemoryVN[memoryKind]);
            continue;
        }

        if (blk->bbMemorySsaNumOut[memoryKind] != blk->bbMemorySsaNumIn[memoryKind])
        {
            GetMemoryPerSsaData(blk->bbMemorySsaNumOut[memoryKind])->m_vnPair.SetLiberal(fgCurMemoryVN[memoryKind]);
        }
    }

    compCurBB = nullptr;
}

ValueNum Compiler::fgMemoryVNForLoopSideEffects(MemoryKind  memoryKind,
                                                BasicBlock* entryBlock,
                                                unsigned    innermostLoopNum)
{
    // "loopNum" is the innermost loop for which "blk" is the entry; find the outermost one.
    assert(innermostLoopNum != BasicBlock::NOT_IN_LOOP);
    unsigned loopsInNest = innermostLoopNum;
    unsigned loopNum     = innermostLoopNum;
    while (loopsInNest != BasicBlock::NOT_IN_LOOP)
    {
        if (optLoopTable[loopsInNest].lpEntry != entryBlock)
        {
            break;
        }
        loopNum     = loopsInNest;
        loopsInNest = optLoopTable[loopsInNest].lpParent;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Computing %s state for block BB%02u, entry block for loops %d to %d:\n", memoryKindNames[memoryKind],
               entryBlock->bbNum, innermostLoopNum, loopNum);
    }
#endif // DEBUG

    // If this loop has memory havoc effects, just use a new, unique VN.
    if (optLoopTable[loopNum].lpLoopHasMemoryHavoc[memoryKind])
    {
        ValueNum res = vnStore->VNForExpr(entryBlock, TYP_REF);
#ifdef DEBUG
        if (verbose)
        {
            printf("  Loop %d has memory havoc effect; heap state is new fresh $%x.\n", loopNum, res);
        }
#endif // DEBUG
        return res;
    }

    // Otherwise, find the predecessors of the entry block that are not in the loop.
    // If there is only one such, use its memory value as the "base."  If more than one,
    // use a new unique VN.
    BasicBlock* nonLoopPred          = nullptr;
    bool        multipleNonLoopPreds = false;
    for (flowList* pred = BlockPredsWithEH(entryBlock); pred != nullptr; pred = pred->flNext)
    {
        BasicBlock* predBlock = pred->flBlock;
        if (!optLoopTable[loopNum].lpContains(predBlock))
        {
            if (nonLoopPred == nullptr)
            {
                nonLoopPred = predBlock;
            }
            else
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("  Entry block has >1 non-loop preds: (at least) BB%02u and BB%02u.\n", nonLoopPred->bbNum,
                           predBlock->bbNum);
                }
#endif // DEBUG
                multipleNonLoopPreds = true;
                break;
            }
        }
    }
    if (multipleNonLoopPreds)
    {
        ValueNum res = vnStore->VNForExpr(entryBlock, TYP_REF);
#ifdef DEBUG
        if (verbose)
        {
            printf("  Therefore, memory state is new, fresh $%x.\n", res);
        }
#endif // DEBUG
        return res;
    }
    // Otherwise, there is a single non-loop pred.
    assert(nonLoopPred != nullptr);
    // What is its memory post-state?
    ValueNum newMemoryVN = GetMemoryPerSsaData(nonLoopPred->bbMemorySsaNumOut[memoryKind])->m_vnPair.GetLiberal();
    assert(newMemoryVN !=
           ValueNumStore::NoVN); // We must have processed the single non-loop pred before reaching the loop entry.

#ifdef DEBUG
    if (verbose)
    {
        printf("  Init %s state is $%x, with new, fresh VN at:\n", memoryKindNames[memoryKind], newMemoryVN);
    }
#endif // DEBUG
    // Modify "base" by setting all the modified fields/field maps/array maps to unknown values.
    // These annotations apply specifically to the GcHeap, where we disambiguate across such stores.
    if (memoryKind == GcHeap)
    {
        // First the fields/field maps.
        Compiler::LoopDsc::FieldHandleSet* fieldsMod = optLoopTable[loopNum].lpFieldsModified;
        if (fieldsMod != nullptr)
        {
            for (Compiler::LoopDsc::FieldHandleSet::KeyIterator ki = fieldsMod->Begin(); !ki.Equal(fieldsMod->End());
                 ++ki)
            {
                CORINFO_FIELD_HANDLE fldHnd   = ki.Get();
                ValueNum             fldHndVN = vnStore->VNForHandle(ssize_t(fldHnd), GTF_ICON_FIELD_HDL);

#ifdef DEBUG
                if (verbose)
                {
                    const char* modName;
                    const char* fldName = eeGetFieldName(fldHnd, &modName);
                    printf("     VNForHandle(Fseq[%s]) is " STR_VN "%x\n", fldName, fldHndVN);

                    printf("  fgCurMemoryVN assigned:\n");
                }
#endif // DEBUG

                newMemoryVN =
                    vnStore->VNForMapStore(TYP_REF, newMemoryVN, fldHndVN, vnStore->VNForExpr(entryBlock, TYP_REF));
            }
        }
        // Now do the array maps.
        Compiler::LoopDsc::ClassHandleSet* elemTypesMod = optLoopTable[loopNum].lpArrayElemTypesModified;
        if (elemTypesMod != nullptr)
        {
            for (Compiler::LoopDsc::ClassHandleSet::KeyIterator ki = elemTypesMod->Begin();
                 !ki.Equal(elemTypesMod->End()); ++ki)
            {
                CORINFO_CLASS_HANDLE elemClsHnd = ki.Get();

#ifdef DEBUG
                if (verbose)
                {
                    var_types elemTyp = DecodeElemType(elemClsHnd);
                    if (varTypeIsStruct(elemTyp))
                    {
                        printf("     Array map %s[]\n", eeGetClassName(elemClsHnd));
                    }
                    else
                    {
                        printf("     Array map %s[]\n", varTypeName(elemTyp));
                    }
                    printf("  fgCurMemoryVN assigned:\n");
                }
#endif // DEBUG

                ValueNum elemTypeVN = vnStore->VNForHandle(ssize_t(elemClsHnd), GTF_ICON_CLASS_HDL);
                ValueNum uniqueVN   = vnStore->VNForExpr(entryBlock, TYP_REF);
                newMemoryVN         = vnStore->VNForMapStore(TYP_REF, newMemoryVN, elemTypeVN, uniqueVN);
            }
        }
    }
    else
    {
        // If there were any fields/elements modified, this should have been recorded as havoc
        // for ByrefExposed.
        assert(memoryKind == ByrefExposed);
        assert((optLoopTable[loopNum].lpFieldsModified == nullptr) ||
               optLoopTable[loopNum].lpLoopHasMemoryHavoc[memoryKind]);
        assert((optLoopTable[loopNum].lpArrayElemTypesModified == nullptr) ||
               optLoopTable[loopNum].lpLoopHasMemoryHavoc[memoryKind]);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("  Final %s state is $%x.\n", memoryKindNames[memoryKind], newMemoryVN);
    }
#endif // DEBUG
    return newMemoryVN;
}

void Compiler::fgMutateGcHeap(GenTreePtr tree DEBUGARG(const char* msg))
{
    // Update the current memory VN, and if we're tracking the heap SSA # caused by this node, record it.
    recordGcHeapStore(tree, vnStore->VNForExpr(compCurBB, TYP_REF) DEBUGARG(msg));
}

void Compiler::fgMutateAddressExposedLocal(GenTreePtr tree DEBUGARG(const char* msg))
{
    // Update the current ByrefExposed VN, and if we're tracking the heap SSA # caused by this node, record it.
    recordAddressExposedLocalStore(tree, vnStore->VNForExpr(compCurBB) DEBUGARG(msg));
}

void Compiler::recordGcHeapStore(GenTreePtr curTree, ValueNum gcHeapVN DEBUGARG(const char* msg))
{
    // bbMemoryDef must include GcHeap for any block that mutates the GC Heap
    // and GC Heap mutations are also ByrefExposed mutations
    assert((compCurBB->bbMemoryDef & memoryKindSet(GcHeap, ByrefExposed)) == memoryKindSet(GcHeap, ByrefExposed));
    fgCurMemoryVN[GcHeap] = gcHeapVN;

    if (byrefStatesMatchGcHeapStates)
    {
        // Since GcHeap and ByrefExposed share SSA nodes, they need to share
        // value numbers too.
        fgCurMemoryVN[ByrefExposed] = gcHeapVN;
    }
    else
    {
        // GcHeap and ByrefExposed have different defnums and VNs.  We conservatively
        // assume that this GcHeap store may alias any byref load/store, so don't
        // bother trying to record the map/select stuff, and instead just an opaque VN
        // for ByrefExposed
        fgCurMemoryVN[ByrefExposed] = vnStore->VNForExpr(compCurBB);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("  fgCurMemoryVN[GcHeap] assigned by %s at ", msg);
        Compiler::printTreeID(curTree);
        printf(" to VN: " STR_VN "%x.\n", gcHeapVN);
    }
#endif // DEBUG

    // If byrefStatesMatchGcHeapStates is true, then since GcHeap and ByrefExposed share
    // their SSA map entries, the below will effectively update both.
    fgValueNumberRecordMemorySsa(GcHeap, curTree);
}

void Compiler::recordAddressExposedLocalStore(GenTreePtr curTree, ValueNum memoryVN DEBUGARG(const char* msg))
{
    // This should only happen if GcHeap and ByrefExposed are being tracked separately;
    // otherwise we'd go through recordGcHeapStore.
    assert(!byrefStatesMatchGcHeapStates);

    // bbMemoryDef must include ByrefExposed for any block that mutates an address-exposed local
    assert((compCurBB->bbMemoryDef & memoryKindSet(ByrefExposed)) != 0);
    fgCurMemoryVN[ByrefExposed] = memoryVN;

#ifdef DEBUG
    if (verbose)
    {
        printf("  fgCurMemoryVN[ByrefExposed] assigned by %s at ", msg);
        Compiler::printTreeID(curTree);
        printf(" to VN: " STR_VN "%x.\n", memoryVN);
    }
#endif // DEBUG

    fgValueNumberRecordMemorySsa(ByrefExposed, curTree);
}

void Compiler::fgValueNumberRecordMemorySsa(MemoryKind memoryKind, GenTreePtr tree)
{
    unsigned ssaNum;
    if (GetMemorySsaMap(memoryKind)->Lookup(tree, &ssaNum))
    {
        GetMemoryPerSsaData(ssaNum)->m_vnPair.SetLiberal(fgCurMemoryVN[memoryKind]);
#ifdef DEBUG
        if (verbose)
        {
            printf("Node ");
            Compiler::printTreeID(tree);
            printf(" sets %s SSA # %d to VN $%x: ", memoryKindNames[memoryKind], ssaNum, fgCurMemoryVN[memoryKind]);
            vnStore->vnDump(this, fgCurMemoryVN[memoryKind]);
            printf("\n");
        }
#endif // DEBUG
    }
}

// The input 'tree' is a leaf node that is a constant
// Assign the proper value number to the tree
void Compiler::fgValueNumberTreeConst(GenTreePtr tree)
{
    genTreeOps oper = tree->OperGet();
    var_types  typ  = tree->TypeGet();
    assert(GenTree::OperIsConst(oper));

    switch (typ)
    {
        case TYP_LONG:
        case TYP_ULONG:
        case TYP_INT:
        case TYP_UINT:
        case TYP_CHAR:
        case TYP_SHORT:
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_BOOL:
            if (tree->IsCnsIntOrI() && tree->IsIconHandle())
            {
                tree->gtVNPair.SetBoth(
                    vnStore->VNForHandle(ssize_t(tree->gtIntConCommon.IconValue()), tree->GetIconHandleFlag()));
            }
            else if ((typ == TYP_LONG) || (typ == TYP_ULONG))
            {
                tree->gtVNPair.SetBoth(vnStore->VNForLongCon(INT64(tree->gtIntConCommon.LngValue())));
            }
            else
            {
                tree->gtVNPair.SetBoth(vnStore->VNForIntCon(int(tree->gtIntConCommon.IconValue())));
            }
            break;

        case TYP_FLOAT:
            tree->gtVNPair.SetBoth(vnStore->VNForFloatCon((float)tree->gtDblCon.gtDconVal));
            break;
        case TYP_DOUBLE:
            tree->gtVNPair.SetBoth(vnStore->VNForDoubleCon(tree->gtDblCon.gtDconVal));
            break;
        case TYP_REF:
            if (tree->gtIntConCommon.IconValue() == 0)
            {
                tree->gtVNPair.SetBoth(ValueNumStore::VNForNull());
            }
            else
            {
                assert(tree->gtFlags == GTF_ICON_STR_HDL); // Constant object can be only frozen string.
                tree->gtVNPair.SetBoth(
                    vnStore->VNForHandle(ssize_t(tree->gtIntConCommon.IconValue()), tree->GetIconHandleFlag()));
            }
            break;

        case TYP_BYREF:
            if (tree->gtIntConCommon.IconValue() == 0)
            {
                tree->gtVNPair.SetBoth(ValueNumStore::VNForNull());
            }
            else
            {
                assert(tree->IsCnsIntOrI());

                if (tree->IsIconHandle())
                {
                    tree->gtVNPair.SetBoth(
                        vnStore->VNForHandle(ssize_t(tree->gtIntConCommon.IconValue()), tree->GetIconHandleFlag()));
                }
                else
                {
                    tree->gtVNPair.SetBoth(vnStore->VNForByrefCon(tree->gtIntConCommon.IconValue()));
                }
            }
            break;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// fgValueNumberBlockAssignment: Perform value numbering for block assignments.
//
// Arguments:
//    tree          - the block assignment to be value numbered.
//    evalAsgLhsInd - true iff we should value number the LHS of the assignment.
//
// Return Value:
//    None.
//
// Assumptions:
//    'tree' must be a block assignment (GT_INITBLK, GT_COPYBLK, GT_COPYOBJ).

void Compiler::fgValueNumberBlockAssignment(GenTreePtr tree, bool evalAsgLhsInd)
{
    GenTree* lhs = tree->gtGetOp1();
    GenTree* rhs = tree->gtGetOp2();
#ifdef DEBUG
    // Sometimes we query the memory ssa map in an assertion, and need a dummy location for the ignored result.
    unsigned memorySsaNum;
#endif

    if (tree->OperIsInitBlkOp())
    {
        GenTreeLclVarCommon* lclVarTree;
        bool                 isEntire;

        if (tree->DefinesLocal(this, &lclVarTree, &isEntire))
        {
            assert(lclVarTree->gtFlags & GTF_VAR_DEF);
            // Should not have been recorded as updating the GC heap.
            assert(!GetMemorySsaMap(GcHeap)->Lookup(tree, &memorySsaNum));

            unsigned lclNum = lclVarTree->GetLclNum();

            // Ignore vars that we excluded from SSA (for example, because they're address-exposed). They don't have
            // SSA names in which to store VN's on defs.  We'll yield unique VN's when we read from them.
            if (!fgExcludeFromSsa(lclNum))
            {
                // Should not have been recorded as updating ByrefExposed.
                assert(!GetMemorySsaMap(ByrefExposed)->Lookup(tree, &memorySsaNum));

                unsigned lclDefSsaNum = GetSsaNumForLocalVarDef(lclVarTree);

                ValueNum   initBlkVN = ValueNumStore::NoVN;
                GenTreePtr initConst = rhs;
                if (isEntire && initConst->OperGet() == GT_CNS_INT)
                {
                    unsigned initVal = 0xFF & (unsigned)initConst->AsIntConCommon()->IconValue();
                    if (initVal == 0)
                    {
                        initBlkVN = vnStore->VNZeroForType(lclVarTree->TypeGet());
                    }
                }
                ValueNum lclVarVN = (initBlkVN != ValueNumStore::NoVN)
                                        ? initBlkVN
                                        : vnStore->VNForExpr(compCurBB, var_types(lvaTable[lclNum].lvType));

                lvaTable[lclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair.SetBoth(lclVarVN);
#ifdef DEBUG
                if (verbose)
                {
                    printf("N%03u ", tree->gtSeqNum);
                    Compiler::printTreeID(tree);
                    printf(" ");
                    gtDispNodeName(tree);
                    printf(" V%02u/%d => ", lclNum, lclDefSsaNum);
                    vnPrint(lclVarVN, 1);
                    printf("\n");
                }
#endif // DEBUG
            }
            else if (lvaVarAddrExposed(lclVarTree->gtLclNum))
            {
                fgMutateAddressExposedLocal(tree DEBUGARG("INITBLK - address-exposed local"));
            }
        }
        else
        {
            // For now, arbitrary side effect on GcHeap/ByrefExposed.
            // TODO-CQ: Why not be complete, and get this case right?
            fgMutateGcHeap(tree DEBUGARG("INITBLK - non local"));
        }
        // Initblock's are of type void.  Give them the void "value" -- they may occur in argument lists, which we
        // want to be able to give VN's to.
        tree->gtVNPair.SetBoth(ValueNumStore::VNForVoid());
    }
    else
    {
        assert(tree->OperIsCopyBlkOp());
        // TODO-Cleanup: We should factor things so that we uniformly rely on "PtrTo" VN's, and
        // the memory cases can be shared with assignments.
        GenTreeLclVarCommon* lclVarTree = nullptr;
        bool                 isEntire   = false;
        // Note that we don't care about exceptions here, since we're only using the values
        // to perform an assignment (which happens after any exceptions are raised...)

        if (tree->DefinesLocal(this, &lclVarTree, &isEntire))
        {
            // Should not have been recorded as updating the GC heap.
            assert(!GetMemorySsaMap(GcHeap)->Lookup(tree, &memorySsaNum));

            unsigned      lhsLclNum = lclVarTree->GetLclNum();
            FieldSeqNode* lhsFldSeq = nullptr;
            // If it's excluded from SSA, don't need to do anything.
            if (!fgExcludeFromSsa(lhsLclNum))
            {
                // Should not have been recorded as updating ByrefExposed.
                assert(!GetMemorySsaMap(ByrefExposed)->Lookup(tree, &memorySsaNum));

                unsigned lclDefSsaNum = GetSsaNumForLocalVarDef(lclVarTree);

                if (lhs->IsLocalExpr(this, &lclVarTree, &lhsFldSeq) ||
                    (lhs->OperIsBlk() && (lhs->AsBlk()->gtBlkSize == lvaLclSize(lhsLclNum))))
                {
                    noway_assert(lclVarTree->gtLclNum == lhsLclNum);
                }
                else
                {
                    GenTree* lhsAddr;
                    if (lhs->OperIsBlk())
                    {
                        lhsAddr = lhs->AsBlk()->Addr();
                    }
                    else
                    {
                        assert(lhs->OperGet() == GT_IND);
                        lhsAddr = lhs->gtOp.gtOp1;
                    }

                    // For addr-of-local expressions, lib/cons shouldn't matter.
                    assert(lhsAddr->gtVNPair.BothEqual());
                    ValueNum lhsAddrVN = lhsAddr->GetVN(VNK_Liberal);

                    // Unpack the PtrToLoc value number of the address.
                    assert(vnStore->IsVNFunc(lhsAddrVN));

                    VNFuncApp lhsAddrFuncApp;
                    vnStore->GetVNFunc(lhsAddrVN, &lhsAddrFuncApp);

                    assert(lhsAddrFuncApp.m_func == VNF_PtrToLoc);
                    assert(vnStore->IsVNConstant(lhsAddrFuncApp.m_args[0]) &&
                           vnStore->ConstantValue<unsigned>(lhsAddrFuncApp.m_args[0]) == lhsLclNum);

                    lhsFldSeq = vnStore->FieldSeqVNToFieldSeq(lhsAddrFuncApp.m_args[1]);
                }

                // Now we need to get the proper RHS.
                GenTreeLclVarCommon* rhsLclVarTree = nullptr;
                LclVarDsc*           rhsVarDsc     = nullptr;
                FieldSeqNode*        rhsFldSeq     = nullptr;
                ValueNumPair         rhsVNPair;
                bool                 isNewUniq = false;
                if (!rhs->OperIsIndir())
                {
                    if (rhs->IsLocalExpr(this, &rhsLclVarTree, &rhsFldSeq))
                    {
                        unsigned rhsLclNum = rhsLclVarTree->GetLclNum();
                        rhsVarDsc          = &lvaTable[rhsLclNum];
                        if (fgExcludeFromSsa(rhsLclNum) || rhsFldSeq == FieldSeqStore::NotAField())
                        {
                            rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, rhsLclVarTree->TypeGet()));
                            isNewUniq = true;
                        }
                        else
                        {
                            rhsVNPair = lvaTable[rhsLclVarTree->GetLclNum()]
                                            .GetPerSsaData(rhsLclVarTree->GetSsaNum())
                                            ->m_vnPair;
                            var_types indType = rhsLclVarTree->TypeGet();

                            rhsVNPair = vnStore->VNPairApplySelectors(rhsVNPair, rhsFldSeq, indType);
                        }
                    }
                    else
                    {
                        rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, rhs->TypeGet()));
                        isNewUniq = true;
                    }
                }
                else
                {
                    GenTreePtr srcAddr = rhs->AsIndir()->Addr();
                    VNFuncApp  srcAddrFuncApp;
                    if (srcAddr->IsLocalAddrExpr(this, &rhsLclVarTree, &rhsFldSeq))
                    {
                        unsigned rhsLclNum = rhsLclVarTree->GetLclNum();
                        rhsVarDsc          = &lvaTable[rhsLclNum];
                        if (fgExcludeFromSsa(rhsLclNum) || rhsFldSeq == FieldSeqStore::NotAField())
                        {
                            isNewUniq = true;
                        }
                        else
                        {
                            rhsVNPair = lvaTable[rhsLclVarTree->GetLclNum()]
                                            .GetPerSsaData(rhsLclVarTree->GetSsaNum())
                                            ->m_vnPair;
                            var_types indType = rhsLclVarTree->TypeGet();

                            rhsVNPair = vnStore->VNPairApplySelectors(rhsVNPair, rhsFldSeq, indType);
                        }
                    }
                    else if (vnStore->GetVNFunc(vnStore->VNNormVal(srcAddr->gtVNPair.GetLiberal()), &srcAddrFuncApp))
                    {
                        if (srcAddrFuncApp.m_func == VNF_PtrToStatic)
                        {
                            var_types indType    = lclVarTree->TypeGet();
                            ValueNum  fieldSeqVN = srcAddrFuncApp.m_args[0];

                            FieldSeqNode* zeroOffsetFldSeq = nullptr;
                            if (GetZeroOffsetFieldMap()->Lookup(srcAddr, &zeroOffsetFldSeq))
                            {
                                fieldSeqVN =
                                    vnStore->FieldSeqVNAppend(fieldSeqVN, vnStore->VNForFieldSeq(zeroOffsetFldSeq));
                            }

                            FieldSeqNode* fldSeqForStaticVar = vnStore->FieldSeqVNToFieldSeq(fieldSeqVN);

                            if (fldSeqForStaticVar != FieldSeqStore::NotAField())
                            {
                                // We model statics as indices into GcHeap (which is a subset of ByrefExposed).
                                ValueNum selectedStaticVar;
                                size_t   structSize = 0;
                                selectedStaticVar   = vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                              fldSeqForStaticVar, &structSize);
                                selectedStaticVar =
                                    vnStore->VNApplySelectorsTypeCheck(selectedStaticVar, indType, structSize);

                                rhsVNPair.SetLiberal(selectedStaticVar);
                                rhsVNPair.SetConservative(vnStore->VNForExpr(compCurBB, indType));
                            }
                            else
                            {
                                JITDUMP("    *** Missing field sequence info for Src/RHS of COPYBLK\n");
                                rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, indType)); //  a new unique value number
                            }
                        }
                        else if (srcAddrFuncApp.m_func == VNF_PtrToArrElem)
                        {
                            ValueNum elemLib =
                                fgValueNumberArrIndexVal(nullptr, &srcAddrFuncApp, vnStore->VNForEmptyExcSet());
                            rhsVNPair.SetLiberal(elemLib);
                            rhsVNPair.SetConservative(vnStore->VNForExpr(compCurBB, lclVarTree->TypeGet()));
                        }
                        else
                        {
                            isNewUniq = true;
                        }
                    }
                    else
                    {
                        isNewUniq = true;
                    }
                }

                if (lhsFldSeq == FieldSeqStore::NotAField())
                {
                    // We don't have proper field sequence information for the lhs
                    //
                    JITDUMP("    *** Missing field sequence info for Dst/LHS of COPYBLK\n");
                    isNewUniq = true;
                }
                else if (lhsFldSeq != nullptr && isEntire)
                {
                    // This can occur in for structs with one field, itself of a struct type.
                    // We won't promote these.
                    // TODO-Cleanup: decide what exactly to do about this.
                    // Always treat them as maps, making them use/def, or reconstitute the
                    // map view here?
                    isNewUniq = true;
                }
                else if (!isNewUniq)
                {
                    ValueNumPair oldLhsVNPair = lvaTable[lhsLclNum].GetPerSsaData(lclVarTree->GetSsaNum())->m_vnPair;
                    rhsVNPair                 = vnStore->VNPairApplySelectorsAssign(oldLhsVNPair, lhsFldSeq, rhsVNPair,
                                                                    lclVarTree->TypeGet(), compCurBB);
                }

                if (isNewUniq)
                {
                    rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lclVarTree->TypeGet()));
                }

                lvaTable[lhsLclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair = vnStore->VNPNormVal(rhsVNPair);

#ifdef DEBUG
                if (verbose)
                {
                    printf("Tree ");
                    Compiler::printTreeID(tree);
                    printf(" assigned VN to local var V%02u/%d: ", lhsLclNum, lclDefSsaNum);
                    if (isNewUniq)
                    {
                        printf("new uniq ");
                    }
                    vnpPrint(rhsVNPair, 1);
                    printf("\n");
                }
#endif // DEBUG
            }
            else if (lvaVarAddrExposed(lhsLclNum))
            {
                fgMutateAddressExposedLocal(tree DEBUGARG("COPYBLK - address-exposed local"));
            }
        }
        else
        {
            // For now, arbitrary side effect on GcHeap/ByrefExposed.
            // TODO-CQ: Why not be complete, and get this case right?
            fgMutateGcHeap(tree DEBUGARG("COPYBLK - non local"));
        }
        // Copyblock's are of type void.  Give them the void "value" -- they may occur in argument lists, which we want
        // to be able to give VN's to.
        tree->gtVNPair.SetBoth(ValueNumStore::VNForVoid());
    }
}

void Compiler::fgValueNumberTree(GenTreePtr tree, bool evalAsgLhsInd)
{
    genTreeOps oper = tree->OperGet();

#ifdef FEATURE_SIMD
    // TODO-CQ: For now TYP_SIMD values are not handled by value numbering to be amenable for CSE'ing.
    if (oper == GT_SIMD)
    {
        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, TYP_UNKNOWN));
        return;
    }
#endif

    var_types typ = tree->TypeGet();
    if (GenTree::OperIsConst(oper))
    {
        // If this is a struct assignment, with a constant rhs, it is an initBlk, and it is not
        // really useful to value number the constant.
        if (!varTypeIsStruct(tree))
        {
            fgValueNumberTreeConst(tree);
        }
    }
    else if (GenTree::OperIsLeaf(oper))
    {
        switch (oper)
        {
            case GT_LCL_VAR:
            case GT_REG_VAR:
            {
                GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
                unsigned             lclNum = lcl->gtLclNum;

                if ((lcl->gtFlags & GTF_VAR_DEF) == 0 ||
                    (lcl->gtFlags & GTF_VAR_USEASG)) // If it is a "pure" def, will handled as part of the assignment.
                {
                    LclVarDsc* varDsc = &lvaTable[lcl->gtLclNum];
                    if (varDsc->lvPromoted && varDsc->lvFieldCnt == 1)
                    {
                        // If the promoted var has only one field var, treat like a use of the field var.
                        lclNum = varDsc->lvFieldLclStart;
                    }

                    // Initialize to the undefined value, so we know whether we hit any of the cases here.
                    lcl->gtVNPair = ValueNumPair();

                    if (lcl->gtSsaNum == SsaConfig::RESERVED_SSA_NUM)
                    {
                        // Not an SSA variable.

                        if (lvaVarAddrExposed(lclNum))
                        {
                            // Address-exposed locals are part of ByrefExposed.
                            ValueNum addrVN = vnStore->VNForFunc(TYP_BYREF, VNF_PtrToLoc, vnStore->VNForIntCon(lclNum),
                                                                 vnStore->VNForFieldSeq(nullptr));
                            ValueNum loadVN = fgValueNumberByrefExposedLoad(typ, addrVN);

                            lcl->gtVNPair.SetBoth(loadVN);
                        }
                        else
                        {
                            // Assign odd cases a new, unique, VN.
                            lcl->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lcl->TypeGet()));
                        }
                    }
                    else
                    {
                        var_types    varType        = varDsc->TypeGet();
                        ValueNumPair wholeLclVarVNP = varDsc->GetPerSsaData(lcl->gtSsaNum)->m_vnPair;

                        // Check for mismatched LclVar size
                        //
                        unsigned typSize = genTypeSize(genActualType(typ));
                        unsigned varSize = genTypeSize(genActualType(varType));

                        if (typSize == varSize)
                        {
                            lcl->gtVNPair = wholeLclVarVNP;
                        }
                        else // mismatched LclVar definition and LclVar use size
                        {
                            if (typSize < varSize)
                            {
                                // the indirection is reading less that the whole LclVar
                                // create a new VN that represent the partial value
                                //
                                ValueNumPair partialLclVarVNP = vnStore->VNPairForCast(wholeLclVarVNP, typ, varType);
                                lcl->gtVNPair                 = partialLclVarVNP;
                            }
                            else
                            {
                                assert(typSize > varSize);
                                // the indirection is reading beyond the end of the field
                                //
                                lcl->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, typ)); // return a new unique value
                                                                                           // number
                            }
                        }
                    }
                    // Temporary, to make progress.
                    // TODO-CQ: This should become an assert again...
                    if (lcl->gtVNPair.GetLiberal() == ValueNumStore::NoVN)
                    {
                        assert(lcl->gtVNPair.GetConservative() == ValueNumStore::NoVN);

                        // We don't want to fabricate arbitrary value numbers to things we can't reason about.
                        // So far, we know about two of these cases:
                        // Case 1) We have a local var who has never been defined but it's seen as a use.
                        //         This is the case of storeIndir(addr(lclvar)) = expr.  In this case since we only
                        //         take the address of the variable, this doesn't mean it's a use nor we have to
                        //         initialize it, so in this very rare case, we fabricate a value number.
                        // Case 2) Local variables that represent structs which are assigned using CpBlk.
                        GenTree* nextNode = lcl->gtNext;
                        assert((nextNode->gtOper == GT_ADDR && nextNode->gtOp.gtOp1 == lcl) ||
                               varTypeIsStruct(lcl->TypeGet()));
                        lcl->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lcl->TypeGet()));
                    }
                    assert(lcl->gtVNPair.BothDefined());
                }

                // TODO-Review: For the short term, we have a workaround for copyblk/initblk.  Those that use
                // addrSpillTemp will have a statement like "addrSpillTemp = addr(local)."  If we previously decided
                // that this block operation defines the local, we will have labeled the "local" node as a DEF
                // (or USEDEF).  This flag propogates to the "local" on the RHS.  So we'll assume that this is correct,
                // and treat it as a def (to a new, unique VN).
                else if ((lcl->gtFlags & GTF_VAR_DEF) != 0)
                {
                    LclVarDsc* varDsc = &lvaTable[lcl->gtLclNum];
                    if (lcl->gtSsaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        lvaTable[lclNum]
                            .GetPerSsaData(lcl->gtSsaNum)
                            ->m_vnPair.SetBoth(vnStore->VNForExpr(compCurBB, lcl->TypeGet()));
                    }
                    lcl->gtVNPair = ValueNumPair(); // Avoid confusion -- we don't set the VN of a lcl being defined.
                }
            }
            break;

            case GT_FTN_ADDR:
                // Use the value of the function pointer (actually, a method handle.)
                tree->gtVNPair.SetBoth(
                    vnStore->VNForHandle(ssize_t(tree->gtFptrVal.gtFptrMethod), GTF_ICON_METHOD_HDL));
                break;

            // This group passes through a value from a child node.
            case GT_RET_EXPR:
                tree->SetVNsFromNode(tree->gtRetExpr.gtInlineCandidate);
                break;

            case GT_LCL_FLD:
            {
                GenTreeLclFld* lclFld = tree->AsLclFld();
                assert(fgExcludeFromSsa(lclFld->GetLclNum()) || lclFld->gtFieldSeq != nullptr);
                // If this is a (full) def, then the variable will be labeled with the new SSA number,
                // which will not have a value.  We skip; it will be handled by one of the assignment-like
                // forms (assignment, or initBlk or copyBlk).
                if (((lclFld->gtFlags & GTF_VAR_DEF) == 0) || (lclFld->gtFlags & GTF_VAR_USEASG))
                {
                    unsigned   lclNum = lclFld->GetLclNum();
                    unsigned   ssaNum = lclFld->GetSsaNum();
                    LclVarDsc* varDsc = &lvaTable[lclNum];

                    if (ssaNum == SsaConfig::UNINIT_SSA_NUM)
                    {
                        if (varDsc->GetPerSsaData(ssaNum)->m_vnPair.GetLiberal() == ValueNumStore::NoVN)
                        {
                            ValueNum vnForLcl                       = vnStore->VNForExpr(compCurBB, lclFld->TypeGet());
                            varDsc->GetPerSsaData(ssaNum)->m_vnPair = ValueNumPair(vnForLcl, vnForLcl);
                        }
                    }

                    var_types indType = tree->TypeGet();
                    if (lclFld->gtFieldSeq == FieldSeqStore::NotAField() || fgExcludeFromSsa(lclFld->GetLclNum()))
                    {
                        // This doesn't represent a proper field access or it's a struct
                        // with overlapping fields that is hard to reason about; return a new unique VN.
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, indType));
                    }
                    else
                    {
                        ValueNumPair lclVNPair = varDsc->GetPerSsaData(ssaNum)->m_vnPair;
                        tree->gtVNPair         = vnStore->VNPairApplySelectors(lclVNPair, lclFld->gtFieldSeq, indType);
                    }
                }
            }
            break;

            // The ones below here all get a new unique VN -- but for various reasons, explained after each.
            case GT_CATCH_ARG:
                // We know nothing about the value of a caught expression.
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;

            case GT_CLS_VAR:
                // Skip GT_CLS_VAR nodes that are the LHS of an assignment.  (We labeled these earlier.)
                // We will "evaluate" this as part of the assignment.  (Unless we're explicitly told by
                // the caller to evaluate anyway -- perhaps the assignment is an "op=" assignment.)
                //
                if (((tree->gtFlags & GTF_CLS_VAR_ASG_LHS) == 0) || evalAsgLhsInd)
                {
                    bool isVolatile = (tree->gtFlags & GTF_FLD_VOLATILE) != 0;

                    if (isVolatile)
                    {
                        // For Volatile indirection, first mutate GcHeap/ByrefExposed
                        fgMutateGcHeap(tree DEBUGARG("GTF_FLD_VOLATILE - read"));
                    }

                    // We just mutate GcHeap/ByrefExposed if isVolatile is true, and then do the read as normal.
                    //
                    // This allows:
                    //   1: read s;
                    //   2: volatile read s;
                    //   3: read s;
                    //
                    // We should never assume that the values read by 1 and 2 are the same (because the heap was mutated
                    // in between them)... but we *should* be able to prove that the values read in 2 and 3 are the
                    // same.
                    //

                    ValueNumPair clsVarVNPair;

                    // If the static field handle is for a struct type field, then the value of the static
                    // is a "ref" to the boxed struct -- treat it as the address of the static (we assume that a
                    // first element offset will be added to get to the actual struct...)
                    GenTreeClsVar* clsVar = tree->AsClsVar();
                    FieldSeqNode*  fldSeq = clsVar->gtFieldSeq;
                    assert(fldSeq != nullptr); // We need to have one.
                    ValueNum selectedStaticVar = ValueNumStore::NoVN;
                    if (gtIsStaticFieldPtrToBoxedStruct(clsVar->TypeGet(), fldSeq->m_fieldHnd))
                    {
                        clsVarVNPair.SetBoth(
                            vnStore->VNForFunc(TYP_BYREF, VNF_PtrToStatic, vnStore->VNForFieldSeq(fldSeq)));
                    }
                    else
                    {
                        // This is a reference to heap memory.
                        // We model statics as indices into GcHeap (which is a subset of ByrefExposed).

                        FieldSeqNode* fldSeqForStaticVar =
                            GetFieldSeqStore()->CreateSingleton(tree->gtClsVar.gtClsVarHnd);
                        size_t structSize = 0;
                        selectedStaticVar = vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                      fldSeqForStaticVar, &structSize);
                        selectedStaticVar =
                            vnStore->VNApplySelectorsTypeCheck(selectedStaticVar, tree->TypeGet(), structSize);

                        clsVarVNPair.SetLiberal(selectedStaticVar);
                        // The conservative interpretation always gets a new, unique VN.
                        clsVarVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    }

                    // The ValueNum returned must represent the full-sized IL-Stack value
                    // If we need to widen this value then we need to introduce a VNF_Cast here to represent
                    // the widened value.    This is necessary since the CSE package can replace all occurances
                    // of a given ValueNum with a LclVar that is a full-sized IL-Stack value
                    //
                    if (varTypeIsSmall(tree->TypeGet()))
                    {
                        var_types castToType = tree->TypeGet();
                        clsVarVNPair         = vnStore->VNPairForCast(clsVarVNPair, castToType, castToType);
                    }
                    tree->gtVNPair = clsVarVNPair;
                }
                break;

            case GT_MEMORYBARRIER: // Leaf
                // For MEMORYBARRIER add an arbitrary side effect on GcHeap/ByrefExposed.
                fgMutateGcHeap(tree DEBUGARG("MEMORYBARRIER"));
                break;

            // These do not represent values.
            case GT_NO_OP:
            case GT_JMP:   // Control flow
            case GT_LABEL: // Control flow
#if !FEATURE_EH_FUNCLETS
            case GT_END_LFIN: // Control flow
#endif
            case GT_ARGPLACE:
                // This node is a standin for an argument whose value will be computed later.  (Perhaps it's
                // a register argument, and we don't want to preclude use of the register in arg evaluation yet.)
                // We give this a "fake" value number now; if the call in which it occurs cares about the
                // value (e.g., it's a helper call whose result is a function of argument values) we'll reset
                // this later, when the later args have been assigned VNs.
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;

            case GT_PHI_ARG:
                // This one is special because we should never process it in this method: it should
                // always be taken care of, when needed, during pre-processing of a blocks phi definitions.
                assert(false);
                break;

            default:
                unreached();
        }
    }
    else if (GenTree::OperIsSimple(oper))
    {
#ifdef DEBUG
        // Sometimes we query the memory ssa map in an assertion, and need a dummy location for the ignored result.
        unsigned memorySsaNum;
#endif

        if (GenTree::OperIsAssignment(oper) && !varTypeIsStruct(tree))
        {

            GenTreePtr lhs = tree->gtOp.gtOp1;
            GenTreePtr rhs = tree->gtOp.gtOp2;

            ValueNumPair rhsVNPair;
            if (oper == GT_ASG)
            {
                rhsVNPair = rhs->gtVNPair;
            }
            else // Must be an "op="
            {
                // If the LHS is an IND, we didn't evaluate it when we visited it previously.
                // But we didn't know that the parent was an op=.  We do now, so go back and evaluate it.
                // (We actually check if the effective val is the IND.  We will have evaluated any non-last
                // args of an LHS comma already -- including their memory effects.)
                GenTreePtr lhsVal = lhs->gtEffectiveVal(/*commaOnly*/ true);
                if (lhsVal->OperIsIndir() || (lhsVal->OperGet() == GT_CLS_VAR))
                {
                    fgValueNumberTree(lhsVal, /*evalAsgLhsInd*/ true);
                }
                // Now we can make this assertion:
                assert(lhsVal->gtVNPair.BothDefined());
                genTreeOps op = GenTree::OpAsgToOper(oper);
                if (GenTree::OperIsBinary(op))
                {
                    ValueNumPair lhsNormVNP;
                    ValueNumPair lhsExcVNP;
                    lhsExcVNP.SetBoth(ValueNumStore::VNForEmptyExcSet());
                    vnStore->VNPUnpackExc(lhsVal->gtVNPair, &lhsNormVNP, &lhsExcVNP);
                    assert(rhs->gtVNPair.BothDefined());
                    ValueNumPair rhsNormVNP;
                    ValueNumPair rhsExcVNP;
                    rhsExcVNP.SetBoth(ValueNumStore::VNForEmptyExcSet());
                    vnStore->VNPUnpackExc(rhs->gtVNPair, &rhsNormVNP, &rhsExcVNP);
                    rhsVNPair = vnStore->VNPWithExc(vnStore->VNPairForFunc(tree->TypeGet(),
                                                                           GetVNFuncForOper(op, (tree->gtFlags &
                                                                                                 GTF_UNSIGNED) != 0),
                                                                           lhsNormVNP, rhsNormVNP),
                                                    vnStore->VNPExcSetUnion(lhsExcVNP, rhsExcVNP));
                }
                else
                {
                    // As of now, GT_CHS ==> GT_NEG is the only pattern fitting this.
                    assert(GenTree::OperIsUnary(op));
                    ValueNumPair lhsNormVNP;
                    ValueNumPair lhsExcVNP;
                    lhsExcVNP.SetBoth(ValueNumStore::VNForEmptyExcSet());
                    vnStore->VNPUnpackExc(lhsVal->gtVNPair, &lhsNormVNP, &lhsExcVNP);
                    rhsVNPair = vnStore->VNPWithExc(vnStore->VNPairForFunc(tree->TypeGet(),
                                                                           GetVNFuncForOper(op, (tree->gtFlags &
                                                                                                 GTF_UNSIGNED) != 0),
                                                                           lhsNormVNP),
                                                    lhsExcVNP);
                }
            }
            if (tree->TypeGet() != TYP_VOID)
            {
                // Assignment operators, as expressions, return the value of the RHS.
                tree->gtVNPair = rhsVNPair;
            }

            // Now that we've labeled the assignment as a whole, we don't care about exceptions.
            rhsVNPair = vnStore->VNPNormVal(rhsVNPair);

            // If the types of the rhs and lhs are different then we
            //  may want to change the ValueNumber assigned to the lhs.
            //
            if (rhs->TypeGet() != lhs->TypeGet())
            {
                if (rhs->TypeGet() == TYP_REF)
                {
                    // If we have an unsafe IL assignment of a TYP_REF to a non-ref (typically a TYP_BYREF)
                    // then don't propagate this ValueNumber to the lhs, instead create a new unique VN
                    //
                    rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lhs->TypeGet()));
                }
            }

            // We have to handle the case where the LHS is a comma.  In that case, we don't evaluate the comma,
            // so we give it VNForVoid, and we're really interested in the effective value.
            GenTreePtr lhsCommaIter = lhs;
            while (lhsCommaIter->OperGet() == GT_COMMA)
            {
                lhsCommaIter->gtVNPair.SetBoth(vnStore->VNForVoid());
                lhsCommaIter = lhsCommaIter->gtOp.gtOp2;
            }
            lhs = lhs->gtEffectiveVal();

            // Now, record the new VN for an assignment (performing the indicated "state update").
            // It's safe to use gtEffectiveVal here, because the non-last elements of a comma list on the
            // LHS will come before the assignment in evaluation order.
            switch (lhs->OperGet())
            {
                case GT_LCL_VAR:
                case GT_REG_VAR:
                {
                    GenTreeLclVarCommon* lcl          = lhs->AsLclVarCommon();
                    unsigned             lclDefSsaNum = GetSsaNumForLocalVarDef(lcl);

                    // Should not have been recorded as updating the GC heap.
                    assert(!GetMemorySsaMap(GcHeap)->Lookup(tree, &memorySsaNum));

                    if (lclDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        // Should not have been recorded as updating ByrefExposed mem.
                        assert(!GetMemorySsaMap(ByrefExposed)->Lookup(tree, &memorySsaNum));

                        assert(rhsVNPair.GetLiberal() != ValueNumStore::NoVN);

                        lhs->gtVNPair                                                 = rhsVNPair;
                        lvaTable[lcl->gtLclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair = rhsVNPair;

#ifdef DEBUG
                        if (verbose)
                        {
                            printf("N%03u ", lhs->gtSeqNum);
                            Compiler::printTreeID(lhs);
                            printf(" ");
                            gtDispNodeName(lhs);
                            gtDispLeaf(lhs, nullptr);
                            printf(" => ");
                            vnpPrint(lhs->gtVNPair, 1);
                            printf("\n");
                        }
#endif // DEBUG
                    }
                    else if (lvaVarAddrExposed(lcl->gtLclNum))
                    {
                        // We could use MapStore here and MapSelect on reads of address-exposed locals
                        // (using the local nums as selectors) to get e.g. propagation of values
                        // through address-taken locals in regions of code with no calls or byref
                        // writes.
                        // For now, just use a new opaque VN.
                        ValueNum heapVN = vnStore->VNForExpr(compCurBB);
                        recordAddressExposedLocalStore(tree, heapVN DEBUGARG("local assign"));
                    }
#ifdef DEBUG
                    else
                    {
                        if (verbose)
                        {
                            JITDUMP("Tree ");
                            Compiler::printTreeID(tree);
                            printf(" assigns to non-address-taken local var V%02u; excluded from SSA, so value not "
                                   "tracked.\n",
                                   lcl->GetLclNum());
                        }
                    }
#endif // DEBUG
                }
                break;
                case GT_LCL_FLD:
                {
                    GenTreeLclFld* lclFld       = lhs->AsLclFld();
                    unsigned       lclDefSsaNum = GetSsaNumForLocalVarDef(lclFld);

                    // Should not have been recorded as updating the GC heap.
                    assert(!GetMemorySsaMap(GcHeap)->Lookup(tree, &memorySsaNum));

                    if (lclDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        ValueNumPair newLhsVNPair;
                        // Is this a full definition?
                        if ((lclFld->gtFlags & GTF_VAR_USEASG) == 0)
                        {
                            assert(!lclFld->IsPartialLclFld(this));
                            assert(rhsVNPair.GetLiberal() != ValueNumStore::NoVN);
                            newLhsVNPair = rhsVNPair;
                        }
                        else
                        {
                            // We should never have a null field sequence here.
                            assert(lclFld->gtFieldSeq != nullptr);
                            if (lclFld->gtFieldSeq == FieldSeqStore::NotAField())
                            {
                                // We don't know what field this represents.  Assign a new VN to the whole variable
                                // (since we may be writing to an unknown portion of it.)
                                newLhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lvaGetActualType(lclFld->gtLclNum)));
                            }
                            else
                            {
                                // We do know the field sequence.
                                // The "lclFld" node will be labeled with the SSA number of its "use" identity
                                // (we looked in a side table above for its "def" identity).  Look up that value.
                                ValueNumPair oldLhsVNPair =
                                    lvaTable[lclFld->GetLclNum()].GetPerSsaData(lclFld->GetSsaNum())->m_vnPair;
                                newLhsVNPair = vnStore->VNPairApplySelectorsAssign(oldLhsVNPair, lclFld->gtFieldSeq,
                                                                                   rhsVNPair, // Pre-value.
                                                                                   lclFld->TypeGet(), compCurBB);
                            }
                        }
                        lvaTable[lclFld->GetLclNum()].GetPerSsaData(lclDefSsaNum)->m_vnPair = newLhsVNPair;
                        lhs->gtVNPair                                                       = newLhsVNPair;
#ifdef DEBUG
                        if (verbose)
                        {
                            if (lhs->gtVNPair.GetLiberal() != ValueNumStore::NoVN)
                            {
                                printf("N%03u ", lhs->gtSeqNum);
                                Compiler::printTreeID(lhs);
                                printf(" ");
                                gtDispNodeName(lhs);
                                gtDispLeaf(lhs, nullptr);
                                printf(" => ");
                                vnpPrint(lhs->gtVNPair, 1);
                                printf("\n");
                            }
                        }
#endif // DEBUG
                    }
                    else if (lvaVarAddrExposed(lclFld->gtLclNum))
                    {
                        // This side-effects ByrefExposed.  Just use a new opaque VN.
                        // As with GT_LCL_VAR, we could probably use MapStore here and MapSelect at corresponding
                        // loads, but to do so would have to identify the subset of address-exposed locals
                        // whose fields can be disambiguated.
                        ValueNum heapVN = vnStore->VNForExpr(compCurBB);
                        recordAddressExposedLocalStore(tree, heapVN DEBUGARG("local field assign"));
                    }
                }
                break;

                case GT_PHI_ARG:
                    assert(false); // Phi arg cannot be LHS.

                case GT_BLK:
                case GT_OBJ:
                case GT_IND:
                {
                    bool isVolatile = (lhs->gtFlags & GTF_IND_VOLATILE) != 0;

                    if (isVolatile)
                    {
                        // For Volatile store indirection, first mutate GcHeap/ByrefExposed
                        fgMutateGcHeap(lhs DEBUGARG("GTF_IND_VOLATILE - store"));
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lhs->TypeGet()));
                    }

                    GenTreePtr arg = lhs->gtOp.gtOp1;

                    // Indicates whether the argument of the IND is the address of a local.
                    bool wasLocal = false;

                    lhs->gtVNPair = rhsVNPair;

                    VNFuncApp funcApp;
                    ValueNum  argVN = arg->gtVNPair.GetLiberal();

                    bool argIsVNFunc = vnStore->GetVNFunc(vnStore->VNNormVal(argVN), &funcApp);

                    // Is this an assignment to a (field of, perhaps) a local?
                    // If it is a PtrToLoc, lib and cons VNs will be the same.
                    if (argIsVNFunc)
                    {
                        IndirectAssignmentAnnotation* pIndirAnnot =
                            nullptr; // This will be used if "tree" is an "indirect assignment",
                                     // explained below.
                        if (funcApp.m_func == VNF_PtrToLoc)
                        {
                            assert(arg->gtVNPair.BothEqual()); // If it's a PtrToLoc, lib/cons shouldn't differ.
                            assert(vnStore->IsVNConstant(funcApp.m_args[0]));
                            unsigned lclNum = vnStore->ConstantValue<unsigned>(funcApp.m_args[0]);

                            wasLocal = true;

                            if (!fgExcludeFromSsa(lclNum))
                            {
                                FieldSeqNode* fieldSeq = vnStore->FieldSeqVNToFieldSeq(funcApp.m_args[1]);

                                // Either "arg" is the address of (part of) a local itself, or the assignment is an
                                // "indirect assignment", where an outer comma expression assigned the address of a
                                // local to a temp, and that temp is our lhs, and we recorded this in a table when we
                                // made the indirect assignment...or else we have a "rogue" PtrToLoc, one that should
                                // have made the local in question address-exposed.  Assert on that.
                                GenTreeLclVarCommon* lclVarTree   = nullptr;
                                bool                 isEntire     = false;
                                unsigned             lclDefSsaNum = SsaConfig::RESERVED_SSA_NUM;
                                ValueNumPair         newLhsVNPair;

                                if (arg->DefinesLocalAddr(this, genTypeSize(lhs->TypeGet()), &lclVarTree, &isEntire))
                                {
                                    // The local #'s should agree.
                                    assert(lclNum == lclVarTree->GetLclNum());

                                    if (fieldSeq == FieldSeqStore::NotAField())
                                    {
                                        // We don't know where we're storing, so give the local a new, unique VN.
                                        // Do this by considering it an "entire" assignment, with an unknown RHS.
                                        isEntire = true;
                                        rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lclVarTree->TypeGet()));
                                    }

                                    if (isEntire)
                                    {
                                        newLhsVNPair = rhsVNPair;
                                        lclDefSsaNum = lclVarTree->GetSsaNum();
                                    }
                                    else
                                    {
                                        // Don't use the lclVarTree's VN: if it's a local field, it will
                                        // already be dereferenced by it's field sequence.
                                        ValueNumPair oldLhsVNPair = lvaTable[lclVarTree->GetLclNum()]
                                                                        .GetPerSsaData(lclVarTree->GetSsaNum())
                                                                        ->m_vnPair;
                                        lclDefSsaNum = GetSsaNumForLocalVarDef(lclVarTree);
                                        newLhsVNPair =
                                            vnStore->VNPairApplySelectorsAssign(oldLhsVNPair, fieldSeq, rhsVNPair,
                                                                                lhs->TypeGet(), compCurBB);
                                    }
                                    lvaTable[lclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair = newLhsVNPair;
                                }
                                else if (m_indirAssignMap != nullptr && GetIndirAssignMap()->Lookup(tree, &pIndirAnnot))
                                {
                                    // The local #'s should agree.
                                    assert(lclNum == pIndirAnnot->m_lclNum);
                                    assert(pIndirAnnot->m_defSsaNum != SsaConfig::RESERVED_SSA_NUM);
                                    lclDefSsaNum = pIndirAnnot->m_defSsaNum;
                                    // Does this assignment write the entire width of the local?
                                    if (genTypeSize(lhs->TypeGet()) == genTypeSize(var_types(lvaTable[lclNum].lvType)))
                                    {
                                        assert(pIndirAnnot->m_useSsaNum == SsaConfig::RESERVED_SSA_NUM);
                                        assert(pIndirAnnot->m_isEntire);
                                        newLhsVNPair = rhsVNPair;
                                    }
                                    else
                                    {
                                        assert(pIndirAnnot->m_useSsaNum != SsaConfig::RESERVED_SSA_NUM);
                                        assert(!pIndirAnnot->m_isEntire);
                                        assert(pIndirAnnot->m_fieldSeq == fieldSeq);
                                        ValueNumPair oldLhsVNPair =
                                            lvaTable[lclNum].GetPerSsaData(pIndirAnnot->m_useSsaNum)->m_vnPair;
                                        newLhsVNPair =
                                            vnStore->VNPairApplySelectorsAssign(oldLhsVNPair, fieldSeq, rhsVNPair,
                                                                                lhs->TypeGet(), compCurBB);
                                    }
                                    lvaTable[lclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair = newLhsVNPair;
                                }
                                else
                                {
                                    unreached(); // "Rogue" PtrToLoc, as discussed above.
                                }
#ifdef DEBUG
                                if (verbose)
                                {
                                    printf("Tree ");
                                    Compiler::printTreeID(tree);
                                    printf(" assigned VN to local var V%02u/%d: VN ", lclNum, lclDefSsaNum);
                                    vnpPrint(newLhsVNPair, 1);
                                    printf("\n");
                                }
#endif // DEBUG
                            }
                            else if (lvaVarAddrExposed(lclNum))
                            {
                                // Need to record the effect on ByrefExposed.
                                // We could use MapStore here and MapSelect on reads of address-exposed locals
                                // (using the local nums as selectors) to get e.g. propagation of values
                                // through address-taken locals in regions of code with no calls or byref
                                // writes.
                                // For now, just use a new opaque VN.
                                ValueNum heapVN = vnStore->VNForExpr(compCurBB);
                                recordAddressExposedLocalStore(tree, heapVN DEBUGARG("PtrToLoc indir"));
                            }
                        }
                    }

                    // Was the argument of the GT_IND the address of a local, handled above?
                    if (!wasLocal)
                    {
                        GenTreePtr    obj          = nullptr;
                        GenTreePtr    staticOffset = nullptr;
                        FieldSeqNode* fldSeq       = nullptr;

                        // Is the LHS an array index expression?
                        if (argIsVNFunc && funcApp.m_func == VNF_PtrToArrElem)
                        {
                            CORINFO_CLASS_HANDLE elemTypeEq =
                                CORINFO_CLASS_HANDLE(vnStore->ConstantValue<ssize_t>(funcApp.m_args[0]));
                            ValueNum      arrVN  = funcApp.m_args[1];
                            ValueNum      inxVN  = funcApp.m_args[2];
                            FieldSeqNode* fldSeq = vnStore->FieldSeqVNToFieldSeq(funcApp.m_args[3]);

                            // Does the child of the GT_IND 'arg' have an associated zero-offset field sequence?
                            FieldSeqNode* addrFieldSeq = nullptr;
                            if (GetZeroOffsetFieldMap()->Lookup(arg, &addrFieldSeq))
                            {
                                fldSeq = GetFieldSeqStore()->Append(addrFieldSeq, fldSeq);
                            }

#ifdef DEBUG
                            if (verbose)
                            {
                                printf("Tree ");
                                Compiler::printTreeID(tree);
                                printf(" assigns to an array element:\n");
                            }
#endif // DEBUG

                            ValueNum heapVN = fgValueNumberArrIndexAssign(elemTypeEq, arrVN, inxVN, fldSeq,
                                                                          rhsVNPair.GetLiberal(), lhs->TypeGet());
                            recordGcHeapStore(tree, heapVN DEBUGARG("Array element assignment"));
                        }
                        // It may be that we haven't parsed it yet.  Try.
                        else if (lhs->gtFlags & GTF_IND_ARR_INDEX)
                        {
                            ArrayInfo arrInfo;
                            bool      b = GetArrayInfoMap()->Lookup(lhs, &arrInfo);
                            assert(b);
                            ValueNum      arrVN  = ValueNumStore::NoVN;
                            ValueNum      inxVN  = ValueNumStore::NoVN;
                            FieldSeqNode* fldSeq = nullptr;

                            // Try to parse it.
                            GenTreePtr arr = nullptr;
                            arg->ParseArrayAddress(this, &arrInfo, &arr, &inxVN, &fldSeq);
                            if (arr == nullptr)
                            {
                                fgMutateGcHeap(tree DEBUGARG("assignment to unparseable array expression"));
                                return;
                            }
                            // Otherwise, parsing succeeded.

                            // Need to form H[arrType][arr][ind][fldSeq] = rhsVNPair.GetLiberal()

                            // Get the element type equivalence class representative.
                            CORINFO_CLASS_HANDLE elemTypeEq =
                                EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                            arrVN = arr->gtVNPair.GetLiberal();

                            FieldSeqNode* zeroOffsetFldSeq = nullptr;
                            if (GetZeroOffsetFieldMap()->Lookup(arg, &zeroOffsetFldSeq))
                            {
                                fldSeq = GetFieldSeqStore()->Append(fldSeq, zeroOffsetFldSeq);
                            }

                            ValueNum heapVN = fgValueNumberArrIndexAssign(elemTypeEq, arrVN, inxVN, fldSeq,
                                                                          rhsVNPair.GetLiberal(), lhs->TypeGet());
                            recordGcHeapStore(tree, heapVN DEBUGARG("assignment to unparseable array expression"));
                        }
                        else if (arg->IsFieldAddr(this, &obj, &staticOffset, &fldSeq))
                        {
                            if (fldSeq == FieldSeqStore::NotAField())
                            {
                                fgMutateGcHeap(tree DEBUGARG("NotAField"));
                            }
                            else
                            {
                                assert(fldSeq != nullptr);
#ifdef DEBUG
                                CORINFO_CLASS_HANDLE fldCls = info.compCompHnd->getFieldClass(fldSeq->m_fieldHnd);
                                if (obj != nullptr)
                                {
                                    // Make sure that the class containing it is not a value class (as we are expecting
                                    // an instance field)
                                    assert((info.compCompHnd->getClassAttribs(fldCls) & CORINFO_FLG_VALUECLASS) == 0);
                                    assert(staticOffset == nullptr);
                                }
#endif // DEBUG
                                // Get the first (instance or static) field from field seq.  GcHeap[field] will yield
                                // the "field map".
                                if (fldSeq->IsFirstElemFieldSeq())
                                {
                                    fldSeq = fldSeq->m_next;
                                    assert(fldSeq != nullptr);
                                }

                                // Get a field sequence for just the first field in the sequence
                                //
                                FieldSeqNode* firstFieldOnly = GetFieldSeqStore()->CreateSingleton(fldSeq->m_fieldHnd);

                                // The final field in the sequence will need to match the 'indType'
                                var_types indType = lhs->TypeGet();
                                ValueNum  fldMapVN =
                                    vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap], firstFieldOnly);

                                // The type of the field is "struct" if there are more fields in the sequence,
                                // otherwise it is the type returned from VNApplySelectors above.
                                var_types firstFieldType = vnStore->TypeOfVN(fldMapVN);

                                ValueNum storeVal =
                                    rhsVNPair.GetLiberal(); // The value number from the rhs of the assignment
                                ValueNum newFldMapVN = ValueNumStore::NoVN;

                                // when (obj != nullptr) we have an instance field, otherwise a static field
                                // when (staticOffset != nullptr) it represents a offset into a static or the call to
                                // Shared Static Base
                                if ((obj != nullptr) || (staticOffset != nullptr))
                                {
                                    ValueNum valAtAddr = fldMapVN;
                                    ValueNum normVal   = ValueNumStore::NoVN;

                                    if (obj != nullptr)
                                    {
                                        // construct the ValueNumber for 'fldMap at obj'
                                        normVal = vnStore->VNNormVal(obj->GetVN(VNK_Liberal));
                                        valAtAddr =
                                            vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, normVal);
                                    }
                                    else // (staticOffset != nullptr)
                                    {
                                        // construct the ValueNumber for 'fldMap at staticOffset'
                                        normVal = vnStore->VNNormVal(staticOffset->GetVN(VNK_Liberal));
                                        valAtAddr =
                                            vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, normVal);
                                    }
                                    // Now get rid of any remaining struct field dereferences. (if they exist)
                                    if (fldSeq->m_next)
                                    {
                                        storeVal =
                                            vnStore->VNApplySelectorsAssign(VNK_Liberal, valAtAddr, fldSeq->m_next,
                                                                            storeVal, indType, compCurBB);
                                    }

                                    // From which we can construct the new ValueNumber for 'fldMap at normVal'
                                    newFldMapVN = vnStore->VNForMapStore(vnStore->TypeOfVN(fldMapVN), fldMapVN, normVal,
                                                                         storeVal);
                                }
                                else
                                {
                                    // plain static field

                                    // Now get rid of any remaining struct field dereferences. (if they exist)
                                    if (fldSeq->m_next)
                                    {
                                        storeVal =
                                            vnStore->VNApplySelectorsAssign(VNK_Liberal, fldMapVN, fldSeq->m_next,
                                                                            storeVal, indType, compCurBB);
                                    }

                                    newFldMapVN = vnStore->VNApplySelectorsAssign(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                                  fldSeq, storeVal, indType, compCurBB);
                                }

                                // It is not strictly necessary to set the lhs value number,
                                // but the dumps read better with it set to the 'storeVal' that we just computed
                                lhs->gtVNPair.SetBoth(storeVal);

#ifdef DEBUG
                                if (verbose)
                                {
                                    printf("  fgCurMemoryVN assigned:\n");
                                }
#endif // DEBUG
                                // bbMemoryDef must include GcHeap for any block that mutates the GC heap
                                assert((compCurBB->bbMemoryDef & memoryKindSet(GcHeap)) != 0);

                                // Update the field map for firstField in GcHeap to this new value.
                                ValueNum heapVN =
                                    vnStore->VNApplySelectorsAssign(VNK_Liberal, fgCurMemoryVN[GcHeap], firstFieldOnly,
                                                                    newFldMapVN, indType, compCurBB);

                                recordGcHeapStore(tree, heapVN DEBUGARG("StoreField"));
                            }
                        }
                        else
                        {
                            GenTreeLclVarCommon* lclVarTree = nullptr;
                            bool                 isLocal    = tree->DefinesLocal(this, &lclVarTree);

                            if (isLocal && lvaVarAddrExposed(lclVarTree->gtLclNum))
                            {
                                // Store to address-exposed local; need to record the effect on ByrefExposed.
                                // We could use MapStore here and MapSelect on reads of address-exposed locals
                                // (using the local nums as selectors) to get e.g. propagation of values
                                // through address-taken locals in regions of code with no calls or byref
                                // writes.
                                // For now, just use a new opaque VN.
                                ValueNum memoryVN = vnStore->VNForExpr(compCurBB);
                                recordAddressExposedLocalStore(tree, memoryVN DEBUGARG("PtrToLoc indir"));
                            }
                            else if (!isLocal)
                            {
                                // If it doesn't define a local, then it might update GcHeap/ByrefExposed.
                                // For the new ByrefExposed VN, we could use an operator here like
                                // VNF_ByrefExposedStore that carries the VNs of the pointer and RHS, then
                                // at byref loads if the current ByrefExposed VN happens to be
                                // VNF_ByrefExposedStore with the same pointer VN, we could propagate the
                                // VN from the RHS to the VN for the load.  This would e.g. allow tracking
                                // values through assignments to out params.  For now, just model this
                                // as an opaque GcHeap/ByrefExposed mutation.
                                fgMutateGcHeap(tree DEBUGARG("assign-of-IND"));
                            }
                        }
                    }

                    // We don't actually evaluate an IND on the LHS, so give it the Void value.
                    tree->gtVNPair.SetBoth(vnStore->VNForVoid());
                }
                break;

                case GT_CLS_VAR:
                {
                    bool isVolatile = (lhs->gtFlags & GTF_FLD_VOLATILE) != 0;

                    if (isVolatile)
                    {
                        // For Volatile store indirection, first mutate GcHeap/ByrefExposed
                        fgMutateGcHeap(lhs DEBUGARG("GTF_CLS_VAR - store")); // always change fgCurMemoryVN
                    }

                    // We model statics as indices into GcHeap (which is a subset of ByrefExposed).
                    FieldSeqNode* fldSeqForStaticVar = GetFieldSeqStore()->CreateSingleton(lhs->gtClsVar.gtClsVarHnd);
                    assert(fldSeqForStaticVar != FieldSeqStore::NotAField());

                    ValueNum storeVal = rhsVNPair.GetLiberal(); // The value number from the rhs of the assignment
                    storeVal = vnStore->VNApplySelectorsAssign(VNK_Liberal, fgCurMemoryVN[GcHeap], fldSeqForStaticVar,
                                                               storeVal, lhs->TypeGet(), compCurBB);

                    // It is not strictly necessary to set the lhs value number,
                    // but the dumps read better with it set to the 'storeVal' that we just computed
                    lhs->gtVNPair.SetBoth(storeVal);
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("  fgCurMemoryVN assigned:\n");
                    }
#endif // DEBUG
                    // bbMemoryDef must include GcHeap for any block that mutates the GC heap
                    assert((compCurBB->bbMemoryDef & memoryKindSet(GcHeap)) != 0);

                    // Update the field map for the fgCurMemoryVN and SSA for the tree
                    recordGcHeapStore(tree, storeVal DEBUGARG("Static Field store"));
                }
                break;

                default:
                    assert(!"Unknown node for lhs of assignment!");

                    // For Unknown stores, mutate GcHeap/ByrefExposed
                    fgMutateGcHeap(lhs DEBUGARG("Unkwown Assignment - store")); // always change fgCurMemoryVN
                    break;
            }
        }
        // Other kinds of assignment: initblk and copyblk.
        else if (oper == GT_ASG && varTypeIsStruct(tree))
        {
            fgValueNumberBlockAssignment(tree, evalAsgLhsInd);
        }
        else if (oper == GT_ADDR)
        {
            // We have special representations for byrefs to lvalues.
            GenTreePtr arg = tree->gtOp.gtOp1;
            if (arg->OperIsLocal())
            {
                FieldSeqNode* fieldSeq = nullptr;
                ValueNum      newVN    = ValueNumStore::NoVN;
                if (fgExcludeFromSsa(arg->gtLclVarCommon.GetLclNum()))
                {
                    newVN = vnStore->VNForExpr(compCurBB, TYP_BYREF);
                }
                else if (arg->OperGet() == GT_LCL_FLD)
                {
                    fieldSeq = arg->AsLclFld()->gtFieldSeq;
                    if (fieldSeq == nullptr)
                    {
                        // Local field with unknown field seq -- not a precise pointer.
                        newVN = vnStore->VNForExpr(compCurBB, TYP_BYREF);
                    }
                }
                if (newVN == ValueNumStore::NoVN)
                {
                    assert(arg->gtLclVarCommon.GetSsaNum() != ValueNumStore::NoVN);
                    newVN = vnStore->VNForFunc(TYP_BYREF, VNF_PtrToLoc,
                                               vnStore->VNForIntCon(arg->gtLclVarCommon.GetLclNum()),
                                               vnStore->VNForFieldSeq(fieldSeq));
                }
                tree->gtVNPair.SetBoth(newVN);
            }
            else if ((arg->gtOper == GT_IND) || arg->OperIsBlk())
            {
                // Usually the ADDR and IND just cancel out...
                // except when this GT_ADDR has a valid zero-offset field sequence
                //
                FieldSeqNode* zeroOffsetFieldSeq = nullptr;
                if (GetZeroOffsetFieldMap()->Lookup(tree, &zeroOffsetFieldSeq) &&
                    (zeroOffsetFieldSeq != FieldSeqStore::NotAField()))
                {
                    ValueNum addrExtended = vnStore->ExtendPtrVN(arg->gtOp.gtOp1, zeroOffsetFieldSeq);
                    if (addrExtended != ValueNumStore::NoVN)
                    {
                        tree->gtVNPair.SetBoth(addrExtended); // We don't care about lib/cons differences for addresses.
                    }
                    else
                    {
                        // ExtendPtrVN returned a failure result
                        // So give this address a new unique value
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, TYP_BYREF));
                    }
                }
                else
                {
                    // They just cancel, so fetch the ValueNumber from the op1 of the GT_IND node.
                    //
                    GenTree* addr  = arg->AsIndir()->Addr();
                    tree->gtVNPair = addr->gtVNPair;

                    // For the CSE phase mark the address as GTF_DONT_CSE
                    // because it will end up with the same value number as tree (the GT_ADDR).
                    addr->gtFlags |= GTF_DONT_CSE;
                }
            }
            else
            {
                // May be more cases to do here!  But we'll punt for now.
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, TYP_BYREF));
            }
        }
        else if ((oper == GT_IND) || GenTree::OperIsBlk(oper))
        {
            // So far, we handle cases in which the address is a ptr-to-local, or if it's
            // a pointer to an object field or array alement.  Other cases become uses of
            // the current ByrefExposed value and the pointer value, so that at least we
            // can recognize redundant loads with no stores between them.
            GenTreePtr           addr         = tree->AsIndir()->Addr();
            GenTreeLclVarCommon* lclVarTree   = nullptr;
            FieldSeqNode*        fldSeq1      = nullptr;
            FieldSeqNode*        fldSeq2      = nullptr;
            GenTreePtr           obj          = nullptr;
            GenTreePtr           staticOffset = nullptr;
            bool                 isVolatile   = (tree->gtFlags & GTF_IND_VOLATILE) != 0;

            // See if the addr has any exceptional part.
            ValueNumPair addrNvnp;
            ValueNumPair addrXvnp = ValueNumPair(ValueNumStore::VNForEmptyExcSet(), ValueNumStore::VNForEmptyExcSet());
            vnStore->VNPUnpackExc(addr->gtVNPair, &addrNvnp, &addrXvnp);

            // Is the dereference immutable?  If so, model it as referencing the read-only heap.
            if (tree->gtFlags & GTF_IND_INVARIANT)
            {
                assert(!isVolatile); // We don't expect both volatile and invariant
                tree->gtVNPair =
                    ValueNumPair(vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, ValueNumStore::VNForROH(),
                                                         addrNvnp.GetLiberal()),
                                 vnStore->VNForMapSelect(VNK_Conservative, TYP_REF, ValueNumStore::VNForROH(),
                                                         addrNvnp.GetConservative()));
                tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
            }
            else if (isVolatile)
            {
                // For Volatile indirection, mutate GcHeap/ByrefExposed
                fgMutateGcHeap(tree DEBUGARG("GTF_IND_VOLATILE - read"));

                // The value read by the GT_IND can immediately change
                ValueNum newUniq = vnStore->VNForExpr(compCurBB, tree->TypeGet());
                tree->gtVNPair   = vnStore->VNPWithExc(ValueNumPair(newUniq, newUniq), addrXvnp);
            }
            // We always want to evaluate the LHS when the GT_IND node is marked with GTF_IND_ARR_INDEX
            // as this will relabel the GT_IND child correctly using the VNF_PtrToArrElem
            else if ((tree->gtFlags & GTF_IND_ARR_INDEX) != 0)
            {
                ArrayInfo arrInfo;
                bool      b = GetArrayInfoMap()->Lookup(tree, &arrInfo);
                assert(b);

                ValueNum      inxVN  = ValueNumStore::NoVN;
                FieldSeqNode* fldSeq = nullptr;

                // GenTreePtr addr = tree->gtOp.gtOp1;
                ValueNum addrVN = addrNvnp.GetLiberal();

                // Try to parse it.
                GenTreePtr arr = nullptr;
                addr->ParseArrayAddress(this, &arrInfo, &arr, &inxVN, &fldSeq);
                if (arr == nullptr)
                {
                    tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    return;
                }
                assert(fldSeq != FieldSeqStore::NotAField());

                // Otherwise...
                // Need to form H[arrType][arr][ind][fldSeq]
                // Get the array element type equivalence class rep.
                CORINFO_CLASS_HANDLE elemTypeEq   = EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                ValueNum             elemTypeEqVN = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);

                // We take the "VNNormVal"s here, because if either has exceptional outcomes, they will be captured
                // as part of the value of the composite "addr" operation...
                ValueNum arrVN = vnStore->VNNormVal(arr->gtVNPair.GetLiberal());
                inxVN          = vnStore->VNNormVal(inxVN);

                // Additionally, relabel the address with a PtrToArrElem value number.
                ValueNum fldSeqVN = vnStore->VNForFieldSeq(fldSeq);
                ValueNum elemAddr =
                    vnStore->VNForFunc(TYP_BYREF, VNF_PtrToArrElem, elemTypeEqVN, arrVN, inxVN, fldSeqVN);

                // The aggregate "addr" VN should have had all the exceptions bubble up...
                elemAddr = vnStore->VNWithExc(elemAddr, addrXvnp.GetLiberal());
                addr->gtVNPair.SetBoth(elemAddr);
#ifdef DEBUG
                if (verbose)
                {
                    printf("  Relabeled IND_ARR_INDEX address node ");
                    Compiler::printTreeID(addr);
                    printf(" with l:" STR_VN "%x: ", elemAddr);
                    vnStore->vnDump(this, elemAddr);
                    printf("\n");
                    if (vnStore->VNNormVal(elemAddr) != elemAddr)
                    {
                        printf("      [" STR_VN "%x is: ", vnStore->VNNormVal(elemAddr));
                        vnStore->vnDump(this, vnStore->VNNormVal(elemAddr));
                        printf("]\n");
                    }
                }
#endif // DEBUG
                // We now need to retrieve the value number for the array element value
                // and give this value number to the GT_IND node 'tree'
                // We do this whenever we have an rvalue, or for the LHS when we have an "op=",
                // but we don't do it for a normal LHS assignment into an array element.
                //
                if (evalAsgLhsInd || ((tree->gtFlags & GTF_IND_ASG_LHS) == 0))
                {
                    fgValueNumberArrIndexVal(tree, elemTypeEq, arrVN, inxVN, addrXvnp.GetLiberal(), fldSeq);
                }
            }
            else if (tree->gtFlags & GTF_IND_ARR_LEN)
            {
                // It's an array length.  The argument is the sum of an array ref with some integer values...
                ValueNum arrRefLib  = vnStore->VNForRefInAddr(tree->gtOp.gtOp1->gtVNPair.GetLiberal());
                ValueNum arrRefCons = vnStore->VNForRefInAddr(tree->gtOp.gtOp1->gtVNPair.GetConservative());

                assert(vnStore->TypeOfVN(arrRefLib) == TYP_REF || vnStore->TypeOfVN(arrRefLib) == TYP_BYREF);
                if (vnStore->IsVNConstant(arrRefLib))
                {
                    // (or in weird cases, a REF or BYREF constant, in which case the result is an exception).
                    tree->gtVNPair.SetLiberal(
                        vnStore->VNWithExc(ValueNumStore::VNForVoid(),
                                           vnStore->VNExcSetSingleton(
                                               vnStore->VNForFunc(TYP_REF, VNF_NullPtrExc, arrRefLib))));
                }
                else
                {
                    tree->gtVNPair.SetLiberal(vnStore->VNForFunc(TYP_INT, VNFunc(GT_ARR_LENGTH), arrRefLib));
                }
                assert(vnStore->TypeOfVN(arrRefCons) == TYP_REF || vnStore->TypeOfVN(arrRefCons) == TYP_BYREF);
                if (vnStore->IsVNConstant(arrRefCons))
                {
                    // (or in weird cases, a REF or BYREF constant, in which case the result is an exception).
                    tree->gtVNPair.SetConservative(
                        vnStore->VNWithExc(ValueNumStore::VNForVoid(),
                                           vnStore->VNExcSetSingleton(
                                               vnStore->VNForFunc(TYP_REF, VNF_NullPtrExc, arrRefCons))));
                }
                else
                {
                    tree->gtVNPair.SetConservative(vnStore->VNForFunc(TYP_INT, VNFunc(GT_ARR_LENGTH), arrRefCons));
                }
            }

            // In general we skip GT_IND nodes on that are the LHS of an assignment.  (We labeled these earlier.)
            // We will "evaluate" this as part of the assignment.  (Unless we're explicitly told by
            // the caller to evaluate anyway -- perhaps the assignment is an "op=" assignment.)
            else if (((tree->gtFlags & GTF_IND_ASG_LHS) == 0) || evalAsgLhsInd)
            {
                FieldSeqNode* localFldSeq = nullptr;
                VNFuncApp     funcApp;

                // Is it a local or a heap address?
                if (addr->IsLocalAddrExpr(this, &lclVarTree, &localFldSeq) &&
                    !fgExcludeFromSsa(lclVarTree->GetLclNum()))
                {
                    unsigned   lclNum = lclVarTree->GetLclNum();
                    unsigned   ssaNum = lclVarTree->GetSsaNum();
                    LclVarDsc* varDsc = &lvaTable[lclNum];

                    if ((localFldSeq == FieldSeqStore::NotAField()) || (localFldSeq == nullptr))
                    {
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    }
                    else
                    {
                        var_types    indType   = tree->TypeGet();
                        ValueNumPair lclVNPair = varDsc->GetPerSsaData(ssaNum)->m_vnPair;
                        tree->gtVNPair         = vnStore->VNPairApplySelectors(lclVNPair, localFldSeq, indType);
                        ;
                    }
                    tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                }
                else if (vnStore->GetVNFunc(addrNvnp.GetLiberal(), &funcApp) && funcApp.m_func == VNF_PtrToStatic)
                {
                    var_types indType    = tree->TypeGet();
                    ValueNum  fieldSeqVN = funcApp.m_args[0];

                    FieldSeqNode* fldSeqForStaticVar = vnStore->FieldSeqVNToFieldSeq(fieldSeqVN);

                    if (fldSeqForStaticVar != FieldSeqStore::NotAField())
                    {
                        ValueNum selectedStaticVar;
                        // We model statics as indices into the GcHeap (which is a subset of ByrefExposed).
                        size_t structSize = 0;
                        selectedStaticVar = vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                      fldSeqForStaticVar, &structSize);
                        selectedStaticVar = vnStore->VNApplySelectorsTypeCheck(selectedStaticVar, indType, structSize);

                        tree->gtVNPair.SetLiberal(selectedStaticVar);
                        tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, indType));
                    }
                    else
                    {
                        JITDUMP("    *** Missing field sequence info for VNF_PtrToStatic value GT_IND\n");
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, indType)); //  a new unique value number
                    }
                    tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                }
                else if (vnStore->GetVNFunc(addrNvnp.GetLiberal(), &funcApp) && (funcApp.m_func == VNF_PtrToArrElem))
                {
                    fgValueNumberArrIndexVal(tree, &funcApp, addrXvnp.GetLiberal());
                }
                else if (addr->IsFieldAddr(this, &obj, &staticOffset, &fldSeq2))
                {
                    if (fldSeq2 == FieldSeqStore::NotAField())
                    {
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    }
                    else if (fldSeq2 != nullptr)
                    {
                        // Get the first (instance or static) field from field seq.  GcHeap[field] will yield the "field
                        // map".
                        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                        CORINFO_CLASS_HANDLE fldCls = info.compCompHnd->getFieldClass(fldSeq2->m_fieldHnd);
                        if (obj != nullptr)
                        {
                            // Make sure that the class containing it is not a value class (as we are expecting an
                            // instance field)
                            assert((info.compCompHnd->getClassAttribs(fldCls) & CORINFO_FLG_VALUECLASS) == 0);
                            assert(staticOffset == nullptr);
                        }
#endif // DEBUG
                        // Get a field sequence for just the first field in the sequence
                        //
                        FieldSeqNode* firstFieldOnly = GetFieldSeqStore()->CreateSingleton(fldSeq2->m_fieldHnd);
                        size_t        structSize     = 0;
                        ValueNum      fldMapVN =
                            vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap], firstFieldOnly, &structSize);

                        // The final field in the sequence will need to match the 'indType'
                        var_types indType = tree->TypeGet();

                        // The type of the field is "struct" if there are more fields in the sequence,
                        // otherwise it is the type returned from VNApplySelectors above.
                        var_types firstFieldType = vnStore->TypeOfVN(fldMapVN);

                        ValueNum valAtAddr = fldMapVN;
                        if (obj != nullptr)
                        {
                            // construct the ValueNumber for 'fldMap at obj'
                            ValueNum objNormVal = vnStore->VNNormVal(obj->GetVN(VNK_Liberal));
                            valAtAddr = vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, objNormVal);
                        }
                        else if (staticOffset != nullptr)
                        {
                            // construct the ValueNumber for 'fldMap at staticOffset'
                            ValueNum offsetNormVal = vnStore->VNNormVal(staticOffset->GetVN(VNK_Liberal));
                            valAtAddr = vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, offsetNormVal);
                        }

                        // Now get rid of any remaining struct field dereferences.
                        if (fldSeq2->m_next)
                        {
                            valAtAddr = vnStore->VNApplySelectors(VNK_Liberal, valAtAddr, fldSeq2->m_next, &structSize);
                        }
                        valAtAddr = vnStore->VNApplySelectorsTypeCheck(valAtAddr, indType, structSize);

                        tree->gtVNPair.SetLiberal(valAtAddr);

                        // The conservative value is a new, unique VN.
                        tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                        tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                    }
                    else
                    {
                        // Occasionally we do an explicit null test on a REF, so we just dereference it with no
                        // field sequence.  The result is probably unused.
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                        tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                    }
                }
                else // We don't know where the address points, so it is an ByrefExposed load.
                {
                    ValueNum addrVN = addr->gtVNPair.GetLiberal();
                    ValueNum loadVN = fgValueNumberByrefExposedLoad(typ, addrVN);
                    tree->gtVNPair.SetLiberal(loadVN);
                    tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                }
            }
        }
        else if (tree->OperGet() == GT_CAST)
        {
            fgValueNumberCastTree(tree);
        }
        else if (tree->OperGet() == GT_INTRINSIC)
        {
            fgValueNumberIntrinsic(tree);
        }
        else if (ValueNumStore::VNFuncIsLegal(GetVNFuncForOper(oper, (tree->gtFlags & GTF_UNSIGNED) != 0)))
        {
            if (GenTree::OperIsUnary(oper))
            {
                if (tree->gtOp.gtOp1 != nullptr)
                {
                    if (tree->OperGet() == GT_NOP)
                    {
                        // Pass through arg vn.
                        tree->gtVNPair = tree->gtOp.gtOp1->gtVNPair;
                    }
                    else
                    {
                        ValueNumPair op1VNP;
                        ValueNumPair op1VNPx = ValueNumStore::VNPForEmptyExcSet();
                        vnStore->VNPUnpackExc(tree->gtOp.gtOp1->gtVNPair, &op1VNP, &op1VNPx);
                        tree->gtVNPair =
                            vnStore->VNPWithExc(vnStore->VNPairForFunc(tree->TypeGet(),
                                                                       GetVNFuncForOper(oper, (tree->gtFlags &
                                                                                               GTF_UNSIGNED) != 0),
                                                                       op1VNP),
                                                op1VNPx);
                    }
                }
                else // Is actually nullary.
                {
                    // Mostly we'll leave these without a value number, assuming we'll detect these as VN failures
                    // if they actually need to have values.  With the exception of NOPs, which can sometimes have
                    // meaning.
                    if (tree->OperGet() == GT_NOP)
                    {
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    }
                }
            }
            else
            {
                assert(!GenTree::OperIsAssignment(oper)); // We handled assignments earlier.
                assert(GenTree::OperIsBinary(oper));
                // Standard binary operator.
                ValueNumPair op2VNPair;
                if (tree->gtOp.gtOp2 == nullptr)
                {
                    op2VNPair.SetBoth(ValueNumStore::VNForNull());
                }
                else
                {
                    op2VNPair = tree->gtOp.gtOp2->gtVNPair;
                }
                // A few special case: if we add a field offset constant to a PtrToXXX, we get back a new PtrToXXX.
                ValueNum newVN = ValueNumStore::NoVN;

                ValueNumPair op1vnp;
                ValueNumPair op1Xvnp = ValueNumStore::VNPForEmptyExcSet();
                vnStore->VNPUnpackExc(tree->gtOp.gtOp1->gtVNPair, &op1vnp, &op1Xvnp);
                ValueNumPair op2vnp;
                ValueNumPair op2Xvnp = ValueNumStore::VNPForEmptyExcSet();
                vnStore->VNPUnpackExc(op2VNPair, &op2vnp, &op2Xvnp);
                ValueNumPair excSet = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);

                if (oper == GT_ADD)
                {
                    newVN = vnStore->ExtendPtrVN(tree->gtOp.gtOp1, tree->gtOp.gtOp2);
                    if (newVN == ValueNumStore::NoVN)
                    {
                        newVN = vnStore->ExtendPtrVN(tree->gtOp.gtOp2, tree->gtOp.gtOp1);
                    }
                }
                if (newVN != ValueNumStore::NoVN)
                {
                    newVN = vnStore->VNWithExc(newVN, excSet.GetLiberal());
                    // We don't care about differences between liberal and conservative for pointer values.
                    tree->gtVNPair.SetBoth(newVN);
                }
                else
                {

                    ValueNumPair normalRes =
                        vnStore->VNPairForFunc(tree->TypeGet(),
                                               GetVNFuncForOper(oper, (tree->gtFlags & GTF_UNSIGNED) != 0), op1vnp,
                                               op2vnp);
                    // Overflow-checking operations add an overflow exception
                    if (tree->gtOverflowEx())
                    {
                        ValueNum overflowExcSet =
                            vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_OverflowExc));
                        excSet = vnStore->VNPExcSetUnion(excSet, ValueNumPair(overflowExcSet, overflowExcSet));
                    }
                    tree->gtVNPair = vnStore->VNPWithExc(normalRes, excSet);
                }
            }
        }
        else // ValueNumStore::VNFuncIsLegal returns false
        {
            // Some of the genTreeOps that aren't legal VNFuncs so they get special handling.
            switch (oper)
            {
                case GT_COMMA:
                {
                    ValueNumPair op1vnp;
                    ValueNumPair op1Xvnp = ValueNumStore::VNPForEmptyExcSet();
                    vnStore->VNPUnpackExc(tree->gtOp.gtOp1->gtVNPair, &op1vnp, &op1Xvnp);
                    ValueNumPair op2vnp;
                    ValueNumPair op2Xvnp = ValueNumStore::VNPForEmptyExcSet();

                    GenTree* op2 = tree->gtGetOp2();
                    if (op2->OperIsIndir() && ((op2->gtFlags & GTF_IND_ASG_LHS) != 0))
                    {
                        // If op2 represents the lhs of an assignment then we give a VNForVoid for the lhs
                        op2vnp = ValueNumPair(ValueNumStore::VNForVoid(), ValueNumStore::VNForVoid());
                    }
                    else if ((op2->OperGet() == GT_CLS_VAR) && (op2->gtFlags & GTF_CLS_VAR_ASG_LHS))
                    {
                        // If op2 represents the lhs of an assignment then we give a VNForVoid for the lhs
                        op2vnp = ValueNumPair(ValueNumStore::VNForVoid(), ValueNumStore::VNForVoid());
                    }
                    else
                    {
                        vnStore->VNPUnpackExc(op2->gtVNPair, &op2vnp, &op2Xvnp);
                    }

                    tree->gtVNPair = vnStore->VNPWithExc(op2vnp, vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp));
                }
                break;

                case GT_NULLCHECK:
                    // Explicit null check.
                    tree->gtVNPair =
                        vnStore->VNPWithExc(ValueNumPair(ValueNumStore::VNForVoid(), ValueNumStore::VNForVoid()),
                                            vnStore->VNPExcSetSingleton(
                                                vnStore->VNPairForFunc(TYP_REF, VNF_NullPtrExc,
                                                                       tree->gtOp.gtOp1->gtVNPair)));
                    break;

                case GT_LOCKADD: // Binop
                case GT_XADD:    // Binop
                case GT_XCHG:    // Binop
                    // For CMPXCHG and other intrinsics add an arbitrary side effect on GcHeap/ByrefExposed.
                    fgMutateGcHeap(tree DEBUGARG("Interlocked intrinsic"));
                    tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    break;

                case GT_JTRUE:
                case GT_LIST:
                    // These nodes never need to have a ValueNumber
                    tree->gtVNPair.SetBoth(ValueNumStore::NoVN);
                    break;

                default:
                    // The default action is to give the node a new, unique VN.
                    tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    break;
            }
        }
    }
    else
    {
        assert(GenTree::OperIsSpecial(oper));

        // TBD: We must handle these individually.  For now:
        switch (oper)
        {
            case GT_CALL:
                fgValueNumberCall(tree->AsCall());
                break;

            case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            {
                // A bounds check node has no value, but may throw exceptions.
                ValueNumPair excSet = vnStore->VNPExcSetSingleton(
                    vnStore->VNPairForFunc(TYP_REF, VNF_IndexOutOfRangeExc,
                                           vnStore->VNPNormVal(tree->AsBoundsChk()->gtIndex->gtVNPair),
                                           vnStore->VNPNormVal(tree->AsBoundsChk()->gtArrLen->gtVNPair)));
                excSet = vnStore->VNPExcSetUnion(excSet, vnStore->VNPExcVal(tree->AsBoundsChk()->gtIndex->gtVNPair));
                excSet = vnStore->VNPExcSetUnion(excSet, vnStore->VNPExcVal(tree->AsBoundsChk()->gtArrLen->gtVNPair));

                tree->gtVNPair = vnStore->VNPWithExc(vnStore->VNPForVoid(), excSet);

                // Record non-constant value numbers that are used as the length argument to bounds checks, so that
                // assertion prop will know that comparisons against them are worth analyzing.
                ValueNum lengthVN = tree->AsBoundsChk()->gtArrLen->gtVNPair.GetConservative();
                if ((lengthVN != ValueNumStore::NoVN) && !vnStore->IsVNConstant(lengthVN))
                {
                    vnStore->SetVNIsCheckedBound(lengthVN);
                }
            }
            break;

            case GT_CMPXCHG: // Specialop
                // For CMPXCHG and other intrinsics add an arbitrary side effect on GcHeap/ByrefExposed.
                fgMutateGcHeap(tree DEBUGARG("Interlocked intrinsic"));
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;

            default:
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
        }
    }
#ifdef DEBUG
    if (verbose)
    {
        if (tree->gtVNPair.GetLiberal() != ValueNumStore::NoVN)
        {
            printf("N%03u ", tree->gtSeqNum);
            printTreeID(tree);
            printf(" ");
            gtDispNodeName(tree);
            if (tree->OperIsLeaf() || tree->OperIsLocalStore()) // local stores used to be leaves
            {
                gtDispLeaf(tree, nullptr);
            }
            printf(" => ");
            vnpPrint(tree->gtVNPair, 1);
            printf("\n");
        }
    }
#endif // DEBUG
}

void Compiler::fgValueNumberIntrinsic(GenTreePtr tree)
{
    assert(tree->OperGet() == GT_INTRINSIC);
    GenTreeIntrinsic* intrinsic = tree->AsIntrinsic();
    ValueNumPair      arg0VNP, arg1VNP;
    ValueNumPair      arg0VNPx = ValueNumStore::VNPForEmptyExcSet();
    ValueNumPair      arg1VNPx = ValueNumStore::VNPForEmptyExcSet();

    vnStore->VNPUnpackExc(intrinsic->gtOp.gtOp1->gtVNPair, &arg0VNP, &arg0VNPx);

    if (intrinsic->gtOp.gtOp2 != nullptr)
    {
        vnStore->VNPUnpackExc(intrinsic->gtOp.gtOp2->gtVNPair, &arg1VNP, &arg1VNPx);
    }

    switch (intrinsic->gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Sin:
        case CORINFO_INTRINSIC_Sqrt:
        case CORINFO_INTRINSIC_Abs:
        case CORINFO_INTRINSIC_Cos:
        case CORINFO_INTRINSIC_Round:
        case CORINFO_INTRINSIC_Cosh:
        case CORINFO_INTRINSIC_Sinh:
        case CORINFO_INTRINSIC_Tan:
        case CORINFO_INTRINSIC_Tanh:
        case CORINFO_INTRINSIC_Asin:
        case CORINFO_INTRINSIC_Acos:
        case CORINFO_INTRINSIC_Atan:
        case CORINFO_INTRINSIC_Atan2:
        case CORINFO_INTRINSIC_Log10:
        case CORINFO_INTRINSIC_Pow:
        case CORINFO_INTRINSIC_Exp:
        case CORINFO_INTRINSIC_Ceiling:
        case CORINFO_INTRINSIC_Floor:

            // GT_INTRINSIC is a currently a subtype of binary operators. But most of
            // the math intrinsics are actually unary operations.

            if (intrinsic->gtOp.gtOp2 == nullptr)
            {
                intrinsic->gtVNPair =
                    vnStore->VNPWithExc(vnStore->EvalMathFuncUnary(tree->TypeGet(), intrinsic->gtIntrinsicId, arg0VNP),
                                        arg0VNPx);
            }
            else
            {
                ValueNumPair newVNP =
                    vnStore->EvalMathFuncBinary(tree->TypeGet(), intrinsic->gtIntrinsicId, arg0VNP, arg1VNP);
                ValueNumPair excSet = vnStore->VNPExcSetUnion(arg0VNPx, arg1VNPx);
                intrinsic->gtVNPair = vnStore->VNPWithExc(newVNP, excSet);
            }

            break;

        case CORINFO_INTRINSIC_Object_GetType:
            intrinsic->gtVNPair =
                vnStore->VNPWithExc(vnStore->VNPairForFunc(intrinsic->TypeGet(), VNF_ObjGetType, arg0VNP), arg0VNPx);
            break;

        default:
            unreached();
    }
}

void Compiler::fgValueNumberCastTree(GenTreePtr tree)
{
    assert(tree->OperGet() == GT_CAST);

    ValueNumPair srcVNPair        = tree->gtOp.gtOp1->gtVNPair;
    var_types    castToType       = tree->CastToType();
    var_types    castFromType     = tree->CastFromType();
    bool         srcIsUnsigned    = ((tree->gtFlags & GTF_UNSIGNED) != 0);
    bool         hasOverflowCheck = tree->gtOverflowEx();

    assert(genActualType(castToType) == genActualType(tree->TypeGet())); // Insure that the resultType is correct

    tree->gtVNPair = vnStore->VNPairForCast(srcVNPair, castToType, castFromType, srcIsUnsigned, hasOverflowCheck);
}

// Compute the normal ValueNumber for a cast operation with no exceptions
ValueNum ValueNumStore::VNForCast(ValueNum  srcVN,
                                  var_types castToType,
                                  var_types castFromType,
                                  bool      srcIsUnsigned /* = false */)
{
    // The resulting type after performingthe cast is always widened to a supported IL stack size
    var_types resultType = genActualType(castToType);

    // When we're considering actual value returned by a non-checking cast whether or not the source is
    // unsigned does *not* matter for non-widening casts.  That is, if we cast an int or a uint to short,
    // we just extract the first two bytes from the source bit pattern, not worrying about the interpretation.
    // The same is true in casting between signed/unsigned types of the same width.  Only when we're doing
    // a widening cast do we care about whether the source was unsigned,so we know whether to sign or zero extend it.
    //
    bool srcIsUnsignedNorm = srcIsUnsigned;
    if (genTypeSize(castToType) <= genTypeSize(castFromType))
    {
        srcIsUnsignedNorm = false;
    }

    ValueNum castTypeVN = VNForCastOper(castToType, srcIsUnsigned);
    ValueNum resultVN   = VNForFunc(resultType, VNF_Cast, srcVN, castTypeVN);

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForCast(" STR_VN "%x, " STR_VN "%x) returns ", srcVN, castTypeVN);
        m_pComp->vnPrint(resultVN, 1);
        printf("\n");
    }
#endif

    return resultVN;
}

// Compute the ValueNumberPair for a cast operation
ValueNumPair ValueNumStore::VNPairForCast(ValueNumPair srcVNPair,
                                          var_types    castToType,
                                          var_types    castFromType,
                                          bool         srcIsUnsigned,    /* = false */
                                          bool         hasOverflowCheck) /* = false */
{
    // The resulting type after performingthe cast is always widened to a supported IL stack size
    var_types resultType = genActualType(castToType);

    ValueNumPair castArgVNP;
    ValueNumPair castArgxVNP = ValueNumStore::VNPForEmptyExcSet();
    VNPUnpackExc(srcVNPair, &castArgVNP, &castArgxVNP);

    // When we're considering actual value returned by a non-checking cast (or a checking cast that succeeds),
    // whether or not the source is unsigned does *not* matter for non-widening casts.
    // That is, if we cast an int or a uint to short, we just extract the first two bytes from the source
    // bit pattern, not worrying about the interpretation.  The same is true in casting between signed/unsigned
    // types of the same width.  Only when we're doing a widening cast do we care about whether the source
    // was unsigned, so we know whether to sign or zero extend it.
    //
    // Important: Casts to floating point cannot be optimized in this fashion. (bug 946768)
    //
    bool srcIsUnsignedNorm = srcIsUnsigned;
    if (genTypeSize(castToType) <= genTypeSize(castFromType) && !varTypeIsFloating(castToType))
    {
        srcIsUnsignedNorm = false;
    }

    ValueNum     castTypeVN = VNForCastOper(castToType, srcIsUnsignedNorm);
    ValueNumPair castTypeVNPair(castTypeVN, castTypeVN);
    ValueNumPair castNormRes = VNPairForFunc(resultType, VNF_Cast, castArgVNP, castTypeVNPair);

    ValueNumPair resultVNP = VNPWithExc(castNormRes, castArgxVNP);

    // If we have a check for overflow, add the exception information.
    if (hasOverflowCheck)
    {
        // For overflow checking, we always need to know whether the source is unsigned.
        castTypeVNPair.SetBoth(VNForCastOper(castToType, srcIsUnsigned));
        ValueNumPair excSet =
            VNPExcSetSingleton(VNPairForFunc(TYP_REF, VNF_ConvOverflowExc, castArgVNP, castTypeVNPair));
        excSet    = VNPExcSetUnion(excSet, castArgxVNP);
        resultVNP = VNPWithExc(castNormRes, excSet);
    }

    return resultVNP;
}

void Compiler::fgValueNumberHelperCallFunc(GenTreeCall* call, VNFunc vnf, ValueNumPair vnpExc)
{
    unsigned nArgs = ValueNumStore::VNFuncArity(vnf);
    assert(vnf != VNF_Boundary);
    GenTreeArgList* args                    = call->gtCallArgs;
    bool            generateUniqueVN        = false;
    bool            useEntryPointAddrAsArg0 = false;

    switch (vnf)
    {
        case VNF_JitNew:
        {
            generateUniqueVN = true;
            vnpExc           = ValueNumStore::VNPForEmptyExcSet();
        }
        break;

        case VNF_JitNewArr:
        {
            generateUniqueVN  = true;
            ValueNumPair vnp1 = vnStore->VNPNormVal(args->Rest()->Current()->gtVNPair);

            // The New Array helper may throw an overflow exception
            vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_NewArrOverflowExc, vnp1));
        }
        break;

        case VNF_BoxNullable:
        {
            // Generate unique VN so, VNForFunc generates a uniq value number for box nullable.
            // Alternatively instead of using vnpUniq below in VNPairForFunc(...),
            // we could use the value number of what the byref arg0 points to.
            //
            // But retrieving the value number of what the byref arg0 points to is quite a bit more work
            // and doing so only very rarely allows for an additional optimization.
            generateUniqueVN = true;
        }
        break;

        case VNF_JitReadyToRunNew:
        {
            generateUniqueVN        = true;
            vnpExc                  = ValueNumStore::VNPForEmptyExcSet();
            useEntryPointAddrAsArg0 = true;
        }
        break;

        case VNF_JitReadyToRunNewArr:
        {
            generateUniqueVN  = true;
            ValueNumPair vnp1 = vnStore->VNPNormVal(args->Current()->gtVNPair);

            // The New Array helper may throw an overflow exception
            vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_NewArrOverflowExc, vnp1));
            useEntryPointAddrAsArg0 = true;
        }
        break;

        case VNF_ReadyToRunStaticBase:
        case VNF_ReadyToRunGenericStaticBase:
        case VNF_ReadyToRunIsInstanceOf:
        case VNF_ReadyToRunCastClass:
        {
            useEntryPointAddrAsArg0 = true;
        }
        break;

        default:
        {
            assert(s_helperCallProperties.IsPure(eeGetHelperNum(call->gtCallMethHnd)));
        }
        break;
    }

    if (generateUniqueVN)
    {
        nArgs--;
    }

    ValueNumPair vnpUniq;
    if (generateUniqueVN)
    {
        // Generate unique VN so, VNForFunc generates a unique value number.
        vnpUniq.SetBoth(vnStore->VNForExpr(compCurBB, call->TypeGet()));
    }

    if (nArgs == 0)
    {
        if (generateUniqueVN)
        {
            call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnpUniq);
        }
        else
        {
            call->gtVNPair.SetBoth(vnStore->VNForFunc(call->TypeGet(), vnf));
        }
    }
    else
    {
        auto getCurrentArg = [call, &args, useEntryPointAddrAsArg0](int currentIndex) {
            GenTreePtr arg = args->Current();
            if ((arg->gtFlags & GTF_LATE_ARG) != 0)
            {
                // This arg is a setup node that moves the arg into position.
                // Value-numbering will have visited the separate late arg that
                // holds the actual value, and propagated/computed the value number
                // for this arg there.
                if (useEntryPointAddrAsArg0)
                {
                    // The args in the fgArgInfo don't include the entry point, so
                    // index into them using one less than the requested index.
                    --currentIndex;
                }
                return call->fgArgInfo->GetLateArg(currentIndex);
            }
            return arg;
        };
        // Has at least one argument.
        ValueNumPair vnp0;
        ValueNumPair vnp0x = ValueNumStore::VNPForEmptyExcSet();
#ifdef FEATURE_READYTORUN_COMPILER
        if (useEntryPointAddrAsArg0)
        {
            ValueNum callAddrVN = vnStore->VNForPtrSizeIntCon((ssize_t)call->gtCall.gtEntryPoint.addr);
            vnp0                = ValueNumPair(callAddrVN, callAddrVN);
        }
        else
#endif
        {
            assert(!useEntryPointAddrAsArg0);
            ValueNumPair vnp0wx = getCurrentArg(0)->gtVNPair;
            vnStore->VNPUnpackExc(vnp0wx, &vnp0, &vnp0x);

            // Also include in the argument exception sets
            vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp0x);

            args = args->Rest();
        }
        if (nArgs == 1)
        {
            if (generateUniqueVN)
            {
                call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnpUniq);
            }
            else
            {
                call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0);
            }
        }
        else
        {
            // Has at least two arguments.
            ValueNumPair vnp1wx = getCurrentArg(1)->gtVNPair;
            ValueNumPair vnp1;
            ValueNumPair vnp1x = ValueNumStore::VNPForEmptyExcSet();
            vnStore->VNPUnpackExc(vnp1wx, &vnp1, &vnp1x);
            vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp1x);

            args = args->Rest();
            if (nArgs == 2)
            {
                if (generateUniqueVN)
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1, vnpUniq);
                }
                else
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1);
                }
            }
            else
            {
                ValueNumPair vnp2wx = getCurrentArg(2)->gtVNPair;
                ValueNumPair vnp2;
                ValueNumPair vnp2x = ValueNumStore::VNPForEmptyExcSet();
                vnStore->VNPUnpackExc(vnp2wx, &vnp2, &vnp2x);
                vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp2x);

                args = args->Rest();
                assert(nArgs == 3); // Our current maximum.
                assert(args == nullptr);
                if (generateUniqueVN)
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1, vnp2, vnpUniq);
                }
                else
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1, vnp2);
                }
            }
        }
        // Add the accumulated exceptions.
        call->gtVNPair = vnStore->VNPWithExc(call->gtVNPair, vnpExc);
    }
}

void Compiler::fgValueNumberCall(GenTreeCall* call)
{
    // First: do value numbering of any argument placeholder nodes in the argument list
    // (by transferring from the VN of the late arg that they are standing in for...)
    unsigned        i               = 0;
    GenTreeArgList* args            = call->gtCallArgs;
    bool            updatedArgPlace = false;
    while (args != nullptr)
    {
        GenTreePtr arg = args->Current();
        if (arg->OperGet() == GT_ARGPLACE)
        {
            // Find the corresponding late arg.
            GenTreePtr lateArg = call->fgArgInfo->GetLateArg(i);
            assert(lateArg->gtVNPair.BothDefined());
            arg->gtVNPair   = lateArg->gtVNPair;
            updatedArgPlace = true;
#ifdef DEBUG
            if (verbose)
            {
                printf("VN of ARGPLACE tree ");
                Compiler::printTreeID(arg);
                printf(" updated to ");
                vnpPrint(arg->gtVNPair, 1);
                printf("\n");
            }
#endif
        }
        i++;
        args = args->Rest();
    }
    if (updatedArgPlace)
    {
        // Now we have to update the VN's of the argument list nodes, since that will be used in determining
        // loop-invariance.
        fgUpdateArgListVNs(call->gtCallArgs);
    }

    if (call->gtCallType == CT_HELPER)
    {
        bool modHeap = fgValueNumberHelperCall(call);

        if (modHeap)
        {
            // For now, arbitrary side effect on GcHeap/ByrefExposed.
            fgMutateGcHeap(call DEBUGARG("HELPER - modifies heap"));
        }
    }
    else
    {
        if (call->TypeGet() == TYP_VOID)
        {
            call->gtVNPair.SetBoth(ValueNumStore::VNForVoid());
        }
        else
        {
            call->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, call->TypeGet()));
        }

        // For now, arbitrary side effect on GcHeap/ByrefExposed.
        fgMutateGcHeap(call DEBUGARG("CALL"));
    }
}

void Compiler::fgUpdateArgListVNs(GenTreeArgList* args)
{
    if (args == nullptr)
    {
        return;
    }
    // Otherwise...
    fgUpdateArgListVNs(args->Rest());
    fgValueNumberTree(args);
}

VNFunc Compiler::fgValueNumberHelperMethVNFunc(CorInfoHelpFunc helpFunc)
{
    assert(s_helperCallProperties.IsPure(helpFunc) || s_helperCallProperties.IsAllocator(helpFunc));

    VNFunc vnf = VNF_Boundary; // An illegal value...
    switch (helpFunc)
    {
        // These translate to other function symbols:
        case CORINFO_HELP_DIV:
            vnf = VNFunc(GT_DIV);
            break;
        case CORINFO_HELP_MOD:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_UDIV:
            vnf = VNFunc(GT_UDIV);
            break;
        case CORINFO_HELP_UMOD:
            vnf = VNFunc(GT_UMOD);
            break;
        case CORINFO_HELP_LLSH:
            vnf = VNFunc(GT_LSH);
            break;
        case CORINFO_HELP_LRSH:
            vnf = VNFunc(GT_RSH);
            break;
        case CORINFO_HELP_LRSZ:
            vnf = VNFunc(GT_RSZ);
            break;
        case CORINFO_HELP_LMUL:
        case CORINFO_HELP_LMUL_OVF:
            vnf = VNFunc(GT_MUL);
            break;
        case CORINFO_HELP_ULMUL_OVF:
            vnf = VNFunc(GT_MUL);
            break; // Is this the right thing?
        case CORINFO_HELP_LDIV:
            vnf = VNFunc(GT_DIV);
            break;
        case CORINFO_HELP_LMOD:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_ULDIV:
            vnf = VNFunc(GT_UDIV);
            break;
        case CORINFO_HELP_ULMOD:
            vnf = VNFunc(GT_UMOD);
            break;

        case CORINFO_HELP_LNG2DBL:
            vnf = VNF_Lng2Dbl;
            break;
        case CORINFO_HELP_ULNG2DBL:
            vnf = VNF_ULng2Dbl;
            break;
        case CORINFO_HELP_DBL2INT:
            vnf = VNF_Dbl2Int;
            break;
        case CORINFO_HELP_DBL2INT_OVF:
            vnf = VNF_Dbl2Int;
            break;
        case CORINFO_HELP_DBL2LNG:
            vnf = VNF_Dbl2Lng;
            break;
        case CORINFO_HELP_DBL2LNG_OVF:
            vnf = VNF_Dbl2Lng;
            break;
        case CORINFO_HELP_DBL2UINT:
            vnf = VNF_Dbl2UInt;
            break;
        case CORINFO_HELP_DBL2UINT_OVF:
            vnf = VNF_Dbl2UInt;
            break;
        case CORINFO_HELP_DBL2ULNG:
            vnf = VNF_Dbl2ULng;
            break;
        case CORINFO_HELP_DBL2ULNG_OVF:
            vnf = VNF_Dbl2ULng;
            break;
        case CORINFO_HELP_FLTREM:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_DBLREM:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_FLTROUND:
            vnf = VNF_FltRound;
            break; // Is this the right thing?
        case CORINFO_HELP_DBLROUND:
            vnf = VNF_DblRound;
            break; // Is this the right thing?

        // These allocation operations probably require some augmentation -- perhaps allocSiteId,
        // something about array length...
        case CORINFO_HELP_NEW_CROSSCONTEXT:
        case CORINFO_HELP_NEWFAST:
        case CORINFO_HELP_NEWSFAST:
        case CORINFO_HELP_NEWSFAST_ALIGN8:
            vnf = VNF_JitNew;
            break;

        case CORINFO_HELP_READYTORUN_NEW:
            vnf = VNF_JitReadyToRunNew;
            break;

        case CORINFO_HELP_NEWARR_1_DIRECT:
        case CORINFO_HELP_NEWARR_1_OBJ:
        case CORINFO_HELP_NEWARR_1_VC:
        case CORINFO_HELP_NEWARR_1_ALIGN8:
            vnf = VNF_JitNewArr;
            break;

        case CORINFO_HELP_READYTORUN_NEWARR_1:
            vnf = VNF_JitReadyToRunNewArr;
            break;

        case CORINFO_HELP_GETGENERICS_GCSTATIC_BASE:
            vnf = VNF_GetgenericsGcstaticBase;
            break;
        case CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE:
            vnf = VNF_GetgenericsNongcstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE:
            vnf = VNF_GetsharedGcstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE:
            vnf = VNF_GetsharedNongcstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedGcstaticBaseNoctor;
            break;
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedNongcstaticBaseNoctor;
            break;
        case CORINFO_HELP_READYTORUN_STATIC_BASE:
            vnf = VNF_ReadyToRunStaticBase;
            break;
        case CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE:
            vnf = VNF_ReadyToRunGenericStaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedGcstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedNongcstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS:
            vnf = VNF_ClassinitSharedDynamicclass;
            break;
        case CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE:
            vnf = VNF_GetgenericsGcthreadstaticBase;
            break;
        case CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE:
            vnf = VNF_GetgenericsNongcthreadstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE:
            vnf = VNF_GetsharedGcthreadstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE:
            vnf = VNF_GetsharedNongcthreadstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedGcthreadstaticBaseNoctor;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedNongcthreadstaticBaseNoctor;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedGcthreadstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedNongcthreadstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSTATICFIELDADDR_CONTEXT:
            vnf = VNF_GetStaticAddrContext;
            break;
        case CORINFO_HELP_GETSTATICFIELDADDR_TLS:
            vnf = VNF_GetStaticAddrTLS;
            break;

        case CORINFO_HELP_RUNTIMEHANDLE_METHOD:
        case CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG:
            vnf = VNF_RuntimeHandleMethod;
            break;

        case CORINFO_HELP_RUNTIMEHANDLE_CLASS:
        case CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG:
            vnf = VNF_RuntimeHandleClass;
            break;

        case CORINFO_HELP_STRCNS:
            vnf = VNF_StrCns;
            break;

        case CORINFO_HELP_CHKCASTCLASS:
        case CORINFO_HELP_CHKCASTCLASS_SPECIAL:
        case CORINFO_HELP_CHKCASTARRAY:
        case CORINFO_HELP_CHKCASTINTERFACE:
        case CORINFO_HELP_CHKCASTANY:
            vnf = VNF_CastClass;
            break;

        case CORINFO_HELP_READYTORUN_CHKCAST:
            vnf = VNF_ReadyToRunCastClass;
            break;

        case CORINFO_HELP_ISINSTANCEOFCLASS:
        case CORINFO_HELP_ISINSTANCEOFINTERFACE:
        case CORINFO_HELP_ISINSTANCEOFARRAY:
        case CORINFO_HELP_ISINSTANCEOFANY:
            vnf = VNF_IsInstanceOf;
            break;

        case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE:
            vnf = VNF_TypeHandleToRuntimeType;
            break;

        case CORINFO_HELP_READYTORUN_ISINSTANCEOF:
            vnf = VNF_ReadyToRunIsInstanceOf;
            break;

        case CORINFO_HELP_LDELEMA_REF:
            vnf = VNF_LdElemA;
            break;

        case CORINFO_HELP_UNBOX:
            vnf = VNF_Unbox;
            break;

        // A constant within any method.
        case CORINFO_HELP_GETCURRENTMANAGEDTHREADID:
            vnf = VNF_ManagedThreadId;
            break;

        case CORINFO_HELP_GETREFANY:
            // TODO-CQ: This should really be interpreted as just a struct field reference, in terms of values.
            vnf = VNF_GetRefanyVal;
            break;

        case CORINFO_HELP_GETCLASSFROMMETHODPARAM:
            vnf = VNF_GetClassFromMethodParam;
            break;

        case CORINFO_HELP_GETSYNCFROMCLASSHANDLE:
            vnf = VNF_GetSyncFromClassHandle;
            break;

        case CORINFO_HELP_LOOP_CLONE_CHOICE_ADDR:
            vnf = VNF_LoopCloneChoiceAddr;
            break;

        case CORINFO_HELP_BOX_NULLABLE:
            vnf = VNF_BoxNullable;
            break;

        default:
            unreached();
    }

    assert(vnf != VNF_Boundary);
    return vnf;
}

bool Compiler::fgValueNumberHelperCall(GenTreeCall* call)
{
    CorInfoHelpFunc helpFunc    = eeGetHelperNum(call->gtCallMethHnd);
    bool            pure        = s_helperCallProperties.IsPure(helpFunc);
    bool            isAlloc     = s_helperCallProperties.IsAllocator(helpFunc);
    bool            modHeap     = s_helperCallProperties.MutatesHeap(helpFunc);
    bool            mayRunCctor = s_helperCallProperties.MayRunCctor(helpFunc);
    bool            noThrow     = s_helperCallProperties.NoThrow(helpFunc);

    ValueNumPair vnpExc = ValueNumStore::VNPForEmptyExcSet();

    // If the JIT helper can throw an exception make sure that we fill in
    // vnpExc with a Value Number that represents the exception(s) that can be thrown.
    if (!noThrow)
    {
        // If the helper is known to only throw only one particular exception
        // we can set vnpExc to that exception, otherwise we conservatively
        // model the JIT helper as possibly throwing multiple different exceptions
        //
        switch (helpFunc)
        {
            case CORINFO_HELP_OVERFLOW:
                // This helper always throws the VNF_OverflowExc exception
                vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_OverflowExc));
                break;

            default:
                // Setup vnpExc with the information that multiple different exceptions
                // could be generated by this helper
                vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_HelperMultipleExc));
        }
    }

    ValueNumPair vnpNorm;

    if (call->TypeGet() == TYP_VOID)
    {
        vnpNorm = ValueNumStore::VNPForVoid();
    }
    else
    {
        // TODO-CQ: this is a list of helpers we're going to treat as non-pure,
        // because they raise complications.  Eventually, we need to handle those complications...
        bool needsFurtherWork = false;
        switch (helpFunc)
        {
            case CORINFO_HELP_NEW_MDARR:
                // This is a varargs helper.  We need to represent the array shape in the VN world somehow.
                needsFurtherWork = true;
                break;
            default:
                break;
        }

        if (!needsFurtherWork && (pure || isAlloc))
        {
            VNFunc vnf = fgValueNumberHelperMethVNFunc(helpFunc);

            if (mayRunCctor)
            {
                if ((call->gtFlags & GTF_CALL_HOISTABLE) == 0)
                {
                    modHeap = true;
                }
            }

            fgValueNumberHelperCallFunc(call, vnf, vnpExc);
            return modHeap;
        }
        else
        {
            vnpNorm.SetBoth(vnStore->VNForExpr(compCurBB, call->TypeGet()));
        }
    }

    call->gtVNPair = vnStore->VNPWithExc(vnpNorm, vnpExc);
    return modHeap;
}

#ifdef DEBUG
// This method asserts that SSA name constraints specified are satisfied.
// Until we figure out otherwise, all VN's are assumed to be liberal.
// TODO-Cleanup: new JitTestLabels for lib vs cons vs both VN classes?
void Compiler::JitTestCheckVN()
{
    typedef SimplerHashTable<ssize_t, SmallPrimitiveKeyFuncs<ssize_t>, ValueNum, JitSimplerHashBehavior>  LabelToVNMap;
    typedef SimplerHashTable<ValueNum, SmallPrimitiveKeyFuncs<ValueNum>, ssize_t, JitSimplerHashBehavior> VNToLabelMap;

    // If we have no test data, early out.
    if (m_nodeTestData == nullptr)
    {
        return;
    }

    NodeToTestDataMap* testData = GetNodeTestData();

    // First we have to know which nodes in the tree are reachable.
    typedef SimplerHashTable<GenTreePtr, PtrKeyFuncs<GenTree>, int, JitSimplerHashBehavior> NodeToIntMap;
    NodeToIntMap* reachable = FindReachableNodesInNodeTestData();

    LabelToVNMap* labelToVN = new (getAllocatorDebugOnly()) LabelToVNMap(getAllocatorDebugOnly());
    VNToLabelMap* vnToLabel = new (getAllocatorDebugOnly()) VNToLabelMap(getAllocatorDebugOnly());

    if (verbose)
    {
        printf("\nJit Testing: Value numbering.\n");
    }
    for (NodeToTestDataMap::KeyIterator ki = testData->Begin(); !ki.Equal(testData->End()); ++ki)
    {
        TestLabelAndNum tlAndN;
        GenTreePtr      node   = ki.Get();
        ValueNum        nodeVN = node->GetVN(VNK_Liberal);

        bool b = testData->Lookup(node, &tlAndN);
        assert(b);
        if (tlAndN.m_tl == TL_VN || tlAndN.m_tl == TL_VNNorm)
        {
            int dummy;
            if (!reachable->Lookup(node, &dummy))
            {
                printf("Node ");
                Compiler::printTreeID(node);
                printf(" had a test constraint declared, but has become unreachable at the time the constraint is "
                       "tested.\n"
                       "(This is probably as a result of some optimization -- \n"
                       "you may need to modify the test case to defeat this opt.)\n");
                assert(false);
            }

            if (verbose)
            {
                printf("  Node ");
                Compiler::printTreeID(node);
                printf(" -- VN class %d.\n", tlAndN.m_num);
            }

            if (tlAndN.m_tl == TL_VNNorm)
            {
                nodeVN = vnStore->VNNormVal(nodeVN);
            }

            ValueNum vn;
            if (labelToVN->Lookup(tlAndN.m_num, &vn))
            {
                if (verbose)
                {
                    printf("      Already in hash tables.\n");
                }
                // The mapping(s) must be one-to-one: if the label has a mapping, then the ssaNm must, as well.
                ssize_t num2;
                bool    b = vnToLabel->Lookup(vn, &num2);
                // And the mappings must be the same.
                if (tlAndN.m_num != num2)
                {
                    printf("Node: ");
                    Compiler::printTreeID(node);
                    printf(", with value number " STR_VN "%x, was declared in VN class %d,\n", nodeVN, tlAndN.m_num);
                    printf("but this value number " STR_VN
                           "%x has already been associated with a different SSA name class: %d.\n",
                           vn, num2);
                    assert(false);
                }
                // And the current node must be of the specified SSA family.
                if (nodeVN != vn)
                {
                    printf("Node: ");
                    Compiler::printTreeID(node);
                    printf(", " STR_VN "%x was declared in SSA name class %d,\n", nodeVN, tlAndN.m_num);
                    printf("but that name class was previously bound to a different value number: " STR_VN "%x.\n", vn);
                    assert(false);
                }
            }
            else
            {
                ssize_t num;
                // The mapping(s) must be one-to-one: if the label has no mapping, then the ssaNm may not, either.
                if (vnToLabel->Lookup(nodeVN, &num))
                {
                    printf("Node: ");
                    Compiler::printTreeID(node);
                    printf(", " STR_VN "%x was declared in value number class %d,\n", nodeVN, tlAndN.m_num);
                    printf(
                        "but this value number has already been associated with a different value number class: %d.\n",
                        num);
                    assert(false);
                }
                // Add to both mappings.
                labelToVN->Set(tlAndN.m_num, nodeVN);
                vnToLabel->Set(nodeVN, tlAndN.m_num);
                if (verbose)
                {
                    printf("      added to hash tables.\n");
                }
            }
        }
    }
}

void Compiler::vnpPrint(ValueNumPair vnp, unsigned level)
{
    if (vnp.BothEqual())
    {
        vnPrint(vnp.GetLiberal(), level);
    }
    else
    {
        printf("<l:");
        vnPrint(vnp.GetLiberal(), level);
        printf(", c:");
        vnPrint(vnp.GetConservative(), level);
        printf(">");
    }
}

void Compiler::vnPrint(ValueNum vn, unsigned level)
{

    if (ValueNumStore::isReservedVN(vn))
    {
        printf(ValueNumStore::reservedName(vn));
    }
    else
    {
        printf(STR_VN "%x", vn);
        if (level > 0)
        {
            vnStore->vnDump(this, vn);
        }
    }
}

#endif // DEBUG

// Methods of ValueNumPair.
ValueNumPair::ValueNumPair() : m_liberal(ValueNumStore::NoVN), m_conservative(ValueNumStore::NoVN)
{
}

bool ValueNumPair::BothDefined() const
{
    return (m_liberal != ValueNumStore::NoVN) && (m_conservative != ValueNumStore::NoVN);
}
