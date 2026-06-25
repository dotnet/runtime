// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// lookupInstructionSet: Gets the InstructionSet for a given class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//
// Return Value:
//    The InstructionSet associated with className
CORINFO_InstructionSet Compiler::lookupInstructionSet(const char* className)
{
    assert(className != nullptr);
    if (strcmp(className, "WasmBase") == 0)
    {
        return InstructionSet_WasmBase;
    }
    else if (strcmp(className, "PackedSimd") == 0)
    {
        return InstructionSet_PackedSimd;
    }
    else if (strcmp(className, "Vector128") == 0)
    {
        return InstructionSet_Vector128;
    }

    return InstructionSet_ILLEGAL;
}

int HWIntrinsicInfo::lookupImmUpperBound(NamedIntrinsic id, var_types baseType)
{
    NYI_WASM_SIMD("lookupImmUpperBound");
    return 0;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclosing class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//    innerEnclosingClassName -- The name of the inner enclosing class or nullptr if one doesn't exist
//    outerEnclosingClassName -- The name of the outer enclosing class or nullptr if one doesn't exist
//
// Return Value:
//    The InstructionSet associated with className and enclosingClassName
//
CORINFO_InstructionSet Compiler::lookupIsa(const char* className,
                                           const char* innerEnclosingClassName,
                                           const char* outerEnclosingClassName)
{
    assert(className != nullptr);

    if (innerEnclosingClassName == nullptr)
    {
        return lookupInstructionSet(className);
    }

    return InstructionSet_ILLEGAL;
}

GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types simdBaseType)
{
    NYI_WASM_SIMD("impNonConstFallback");
    return nullptr;
}

GenTree* Compiler::impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                       CORINFO_CLASS_HANDLE  clsHnd,
                                       CORINFO_METHOD_HANDLE method,
                                       CORINFO_SIG_INFO* sig R2RARG(CORINFO_CONST_LOOKUP* entryPoint),
                                       var_types             simdBaseType,
                                       var_types             retType,
                                       unsigned              simdSize,
                                       bool                  mustExpand)
{
    assert(varTypeIsArithmetic(simdBaseType));

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;

    switch (intrinsic)
    {
        case NI_Vector128_GetElement:
        case NI_Vector128_WithElement:
        {
            // Vector128.GetElement / WithElement have valid managed implementations in
            // Vector128.cs. Return nullptr to let the importer fall back to those rather
            // than asserting / NYI'ing in the JIT.
            return nullptr;
        }

        // The following PackedSimd intrinsics are not yet implemented on WASM. Because
        // PackedSimd is decorated with a class-level [Intrinsic], the managed bodies in
        // PackedSimd.cs are self-recursive stubs that would stack-overflow at runtime if
        // the JIT didn't recognize them. Fail loudly with a descriptive NYI_WASM_SIMD
        // instead until codegen lands.
        case NI_PackedSimd_CompareGreaterThan:
        case NI_PackedSimd_CompareGreaterThanOrEqual:
        case NI_PackedSimd_CompareLessThan:
        case NI_PackedSimd_CompareLessThanOrEqual:
            NYI_WASM_SIMD("PackedSimd ordered compare (needs special codegen for Vector128<ulong>)");
            break;

        case NI_PackedSimd_ExtractScalar:
            NYI_WASM_SIMD("PackedSimd.ExtractScalar");
            break;
        case NI_PackedSimd_ReplaceScalar:
            NYI_WASM_SIMD("PackedSimd.ReplaceScalar");
            break;

        case NI_PackedSimd_LoadVector128:
        case NI_PackedSimd_LoadScalarVector128:
        case NI_PackedSimd_LoadScalarAndSplatVector128:
        case NI_PackedSimd_LoadScalarAndInsert:
        case NI_PackedSimd_LoadWideningVector128:
            NYI_WASM_SIMD("PackedSimd load");
            break;

        case NI_PackedSimd_Store:
        case NI_PackedSimd_StoreSelectedScalar:
            NYI_WASM_SIMD("PackedSimd store");
            break;

        case NI_PackedSimd_ShiftLeft:
        case NI_PackedSimd_ShiftRightArithmetic:
        case NI_PackedSimd_ShiftRightLogical:
            NYI_WASM_SIMD("PackedSimd shift");
            break;

        case NI_PackedSimd_Splat:
            NYI_WASM_SIMD("PackedSimd.Splat");
            break;

        case NI_Vector128_Create:
        {
            // Vector128.Create(T)                    -> broadcast/"splat" one T value to all elements
            // Vector128.Create(T, T, ..., T)         -> pack N scalar T values
            //
            // For either form, if T is a const we can leverage a GenTreeVecCon and map directly to v128.const
            if (sig->numArgs == 1)
            {
                GenTree* const arg = impStackTop().val;
                if (!arg->IsIntegralConst() && !arg->IsCnsFltOrDbl())
                {
                    // Non-constant broadcast isn't supported yet on WASM;
                    return nullptr;
                }

                op1     = impPopStack().val;
                retNode = gtNewSimdCreateBroadcastNode(retType, op1, simdBaseType, simdSize);
                break;
            }

            uint32_t simdLength = getSIMDVectorLength(simdSize, simdBaseType);
            assert(sig->numArgs == simdLength);

            bool isConstant = true;

            if (varTypeIsFloating(simdBaseType))
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    if (!impStackTop(index).val->IsCnsFltOrDbl())
                    {
                        isConstant = false;
                        break;
                    }
                }
            }
            else
            {
                assert(varTypeIsIntegral(simdBaseType));

                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    if (!impStackTop(index).val->IsIntegralConst())
                    {
                        isConstant = false;
                        break;
                    }
                }
            }

            if (isConstant)
            {
                assert(simdSize == 16);

                GenTreeVecCon* vecCon = gtNewVconNode(retType);

                switch (simdBaseType)
                {
                    case TYP_BYTE:
                    case TYP_UBYTE:
                    {
                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            uint8_t cnsVal = static_cast<uint8_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u8[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_SHORT:
                    case TYP_USHORT:
                    {
                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            uint16_t cnsVal =
                                static_cast<uint16_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u16[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_INT:
                    case TYP_UINT:
                    {
                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            uint32_t cnsVal =
                                static_cast<uint32_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u32[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_LONG:
                    case TYP_ULONG:
                    {
                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            uint64_t cnsVal =
                                static_cast<uint64_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u64[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_FLOAT:
                    {
                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            float cnsVal = static_cast<float>(impPopStack().val->AsDblCon()->DconValue());
                            vecCon->gtSimdVal.f32[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_DOUBLE:
                    {
                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            double cnsVal = static_cast<double>(impPopStack().val->AsDblCon()->DconValue());
                            vecCon->gtSimdVal.f64[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                retNode = vecCon;
                break;
            }

            // TODO-WASM-SIMD: Build a GT_HWINTRINSIC node packing N non-constant operands
            // (mirroring the arm64/xarch IntrinsicNodeBuilder path) once WASM SIMD lowering
            // and codegen for Vector128.Create are implemented.
            retNode = nullptr;
            break;
        }

        default:
        {
            NYI_WASM_SIMD("impSpecialIntrinsic");
            break;
        }
    }

    return retNode;
}

//------------------------------------------------------------------------
// getHWIntrinsicImmOps: Gets the immediate Ops for an intrinsic
//
// Arguments:
//    intrinsic       -- NamedIntrinsic associated with the HWIntrinsic to lookup
//    sig             -- signature of the intrinsic call.
//    immOp1Ptr [OUT] -- The first immediate Op
//    immOp2Ptr [OUT] -- The second immediate Op, if any. Otherwise unchanged.
//
void Compiler::getHWIntrinsicImmOps(NamedIntrinsic    intrinsic,
                                    CORINFO_SIG_INFO* sig,
                                    GenTree**         immOp1Ptr,
                                    GenTree**         immOp2Ptr)
{
    if ((sig->numArgs > 0) && HWIntrinsicInfo::isImmOp(intrinsic, impStackTop().val))
    {
        // NOTE: The following code assumes that for all intrinsics
        // taking an immediate operand, that operand will be last.
        *immOp1Ptr = impStackTop().val;
    }
}
#endif
