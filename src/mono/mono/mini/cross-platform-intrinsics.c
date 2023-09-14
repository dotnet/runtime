// HKTN-TODO: cull the #includes
#include <config.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/icall-decl.h>
#include "mini.h"
#include "mini-runtime.h"
#include "ir-emit.h"
#include "llvm-intrinsics-types.h"
#include "cross-platform-intrinsics.h"
#include "intrinsics-helper.h"
#ifdef ENABLE_LLVM
#include "mini-llvm.h"
#include "mini-llvm-cpp.h"
#endif
#include "mono/utils/bsearch.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/mono-hwcap.h>

static NamedIntrinsic 
lookup_named_intrinsic (const char* class_ns, const char* class_name, MonoMethod* method)
{
	// We should be able to get class_ns and class_name for free - emit_intrinsics generates that.

	NamedIntrinsic ret = NamedIntrinsic.NI_Illegal;
	// HKTN-TODO: https://github.com/dotnet/runtime/blob/559470195bec88d9c74e70ea440c8394a0a6cfdc/src/coreclr/jit/importercalls.cpp#L8487
	// HKTN-TODO: Are we interested in automatically generating this search code? We could use some C# code to generate that perhaps.

	return ret;
}

MonoInst*
emit_cross_platform_intrinsics_for_vector_classes (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, const char* class_ns, const char* class_name)
{
	NamedIntrinsic ni = resolveNamedIntrinsic (class_ns, class_name);

	return emit_hw_intrinsics_for_vector_classes (cfg, cmethod, fsig, args, ni);
}

MonoInst*
emit_hw_intrinsics_for_vector_classes (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, NamedIntrinsic id)
{
	//Copy over the content of emit_sri_vector and emit_vector64_vector128_t and merge them. Then update the case id with NamedIntrinsic enums
	if (id == NI_Illegal) {
		return NULL;
	}

	int vector_size;
	if (!strcmp (m_class_get_name (cmethod->klass), "Vector64"))
		vector_size = 64;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector128"))
		vector_size = 128;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector256"))
		vector_size = 256;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector512"))
		vector_size = 512;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector"))
		vector_size = register_size * 8;
	else
		return NULL;

#ifdef TARGET_WASM
	g_assert (COMPILE_LLVM (cfg));
#endif

#ifdef TARGET_AMD64
	if (!COMPILE_LLVM (cfg)) {
#ifdef TARGET_WIN32
		return NULL;
#endif
		if (!is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSE41))
			/* Some opcodes like pextrd require sse41 */
			return NULL;
	}
#endif

	MonoClass* klass = fsig->param_count > 0 ? args[0]->klass : cmethod->klass;
	MonoTypeEnum arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case NI_Vector128_get_IsHardwareAccelerated: {
		MonoInst* ins;
		EMIT_NEW_ICONST (cfg, ins, 1);
		return ins;
	}
	case NI_Vector128_Abs: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		if (type_enum_is_unsigned (arg0_type))
			return NULL;
#ifdef TARGET_ARM64
		int iid = type_enum_is_float (arg0_type) ? INTRINS_AARCH64_ADV_SIMD_FABS : INTRINS_AARCH64_ADV_SIMD_ABS;
		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X, iid, arg0_type, fsig, args);
#elif defined(TARGET_AMD64)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		if (type_enum_is_float(arg0_type)) {
			// args [0] & ~vector(-0.0)
			MonoInst *zero = emit_xzero(cfg, arg_class);	// 0.0
			zero = emit_simd_ins (cfg, klass, OP_NEGATION, zero->dreg, -1); // -0.0
			zero->inst_c1 = arg0_type;
			MonoInst *ins = emit_simd_ins (cfg, klass, OP_VECTOR_ANDN, zero->dreg, args [0]->dreg);
			ins->inst_c1 = arg0_type;
			return ins;
		} else {
			if (!COMPILE_LLVM (cfg))
				// FIXME:
				return NULL;
			return emit_simd_ins_for_sig (cfg, klass, OP_VECTOR_IABS, -1, arg0_type, fsig, args);
		}
#elif defined(TARGET_WASM)
		if (type_enum_is_float(arg0_type)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X, arg0_type == MONO_TYPE_R8 ? INTRINS_WASM_FABS_V2 : INTRINS_WASM_FABS_V4, -1, fsig, args);
		} else {
			return emit_simd_ins_for_sig (cfg, klass, OP_VECTOR_IABS, -1, arg0_type, fsig, args);
		}
#else
		return NULL;
#endif
	}
	case NI_Vector128_Add:
	case NI_Vector128_BitwiseAnd:
	case NI_Vector128_BitwiseOr:
	case NI_Vector128_Divide:
	case NI_Vector128_Max:
	case NI_Vector128_Min:
	case NI_Vector128_Multiply:
	case NI_Vector128_Subtract:
	case NI_Vector128_Xor:
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		return emit_simd_ins_for_binary_op (cfg, klass, fsig, args, arg0_type, id);
	case NI_Vector128_AndNot: {
		if (!is_element_type_primitive (fsig->params [0])) 
			return NULL;
#ifdef TARGET_ARM64
		return emit_simd_ins_for_sig (cfg, klass, OP_ARM64_BIC, -1, arg0_type, fsig, args);
#elif defined(TARGET_AMD64) || defined(TARGET_WASM)
		/* Swap lhs and rhs because Vector128 needs lhs & !rhs
		   whereas SSE2 does !lhs & rhs */
		MonoInst *tmp = args[0];
		args[0] = args[1];
		args[1] = tmp;

		return emit_simd_ins_for_sig (cfg, klass, OP_VECTOR_ANDN, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_As:
	case NI_Vector128_AsByte:
	case NI_Vector128_AsDouble:
	case NI_Vector128_AsInt16:
	case NI_Vector128_AsInt32:
	case NI_Vector128_AsInt64:
	case NI_Vector128_AsSByte:
	case NI_Vector128_AsSingle:
	case NI_Vector128_AsUInt16:
	case NI_Vector128_AsUInt32:
	case NI_Vector128_AsUInt64: {
		if (!is_element_type_primitive (fsig->ret) || !is_element_type_primitive (fsig->params [0]))
			return NULL;
		return emit_simd_ins (cfg, klass, OP_XCAST, args [0]->dreg, -1);
	}
	case NI_Vector128_Ceiling:
	case NI_Vector128_Floor: {
		if (!type_enum_is_float (arg0_type))
			return NULL;
#ifdef TARGET_ARM64
		int ceil_or_floor = id == NI_Vector128_Ceiling ? INTRINS_SIMD_CEIL : INTRINS_SIMD_FLOOR;
		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X, ceil_or_floor, arg0_type, fsig, args);
#elif defined(TARGET_AMD64)
		if (!is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSE41))
			return NULL;

		int ceil_or_floor = id == NI_Vector128_Ceiling ? 10 : 9;
		return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_ROUNDP, ceil_or_floor, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_ConditionalSelect: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

#if defined(TARGET_ARM64) || defined(TARGET_AMD64) || defined(TARGET_WASM)

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg)) {
			MonoInst *val1 = emit_simd_ins (cfg, klass, OP_XBINOP_FORCEINT, args [0]->dreg, args [1]->dreg);
			val1->inst_c0 = XBINOP_FORCEINT_AND;
			MonoInst *val2 = emit_simd_ins (cfg, klass, OP_VECTOR_ANDN, args [0]->dreg, args [2]->dreg);
			MonoInst *ins = emit_simd_ins (cfg, klass, OP_XBINOP_FORCEINT, val1->dreg, val2->dreg);
			ins->inst_c0 = XBINOP_FORCEINT_OR;
			return ins;
		}
#endif

		return emit_simd_ins_for_sig (cfg, klass, OP_BSL, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_ConvertToDouble: {
		if ((arg0_type != MONO_TYPE_I8) && (arg0_type != MONO_TYPE_U8))
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				arg0_type == MONO_TYPE_I8 ? OP_CVT_SI_FP : OP_CVT_UI_FP, 
				MONO_TYPE_R8, fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);
		int op = -1;
		if (size == 8)
			op = arg0_type == MONO_TYPE_I8 ? OP_CVT_SI_FP_SCALAR : OP_CVT_UI_FP_SCALAR;
		else
			op = arg0_type == MONO_TYPE_I8 ? OP_CVT_SI_FP : OP_CVT_UI_FP;

#ifdef TARGET_AMD64
		// Fall back to the c# code
		if (!COMPILE_LLVM (cfg))
			return NULL;
#endif

		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_ConvertToInt32: 
	case NI_Vector128_ConvertToUInt32: {
		if (arg0_type != MONO_TYPE_R4)
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				id == NI_Vector128_ConvertToInt32 ? OP_CVT_FP_SI : OP_CVT_FP_UI, 
				id == NI_Vector128_ConvertToInt32 ? MONO_TYPE_I4 : MONO_TYPE_U4, 
				fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg) && id == NI_Vector128_ConvertToUInt32)
			// FIXME:
			return NULL;
#endif

		int op = id == NI_Vector128_ConvertToInt32 ? OP_CVT_FP_SI : OP_CVT_FP_UI;
		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_ConvertToInt64:
	case NI_Vector128_ConvertToUInt64: {
		if (arg0_type != MONO_TYPE_R8)
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				id == NI_Vector128_ConvertToInt64 ? OP_CVT_FP_SI : OP_CVT_FP_UI, 
				id == NI_Vector128_ConvertToInt64 ? MONO_TYPE_I8 : MONO_TYPE_U8, 
				fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);
		int op = -1;
		if (id == NI_Vector128_ConvertToInt64)
			op = size == 8 ? OP_CVT_FP_SI_SCALAR : OP_CVT_FP_SI;
		else
			op = size == 8 ? OP_CVT_FP_UI_SCALAR : OP_CVT_FP_UI;

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg))
			// FIXME:
			return NULL;
#endif

		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_ConvertToSingle: {
		if ((arg0_type != MONO_TYPE_I4) && (arg0_type != MONO_TYPE_U4))
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				arg0_type == MONO_TYPE_I4 ? OP_CVT_SI_FP : OP_CVT_UI_FP, 
				MONO_TYPE_R4, fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
		int op = arg0_type == MONO_TYPE_I4 ? OP_CVT_SI_FP : OP_CVT_UI_FP;

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg) && op == OP_CVT_UI_FP)
			// FIXME:
			return NULL;
#endif

		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_Create: {
		MonoType *etype = get_vector_t_elem_type (fsig->ret);
		if (!MONO_TYPE_IS_VECTOR_PRIMITIVE (etype))
			return NULL;
		if (fsig->param_count == 1 && mono_metadata_type_equal (fsig->params [0], etype)) {
			MonoInst* ins = emit_simd_ins (cfg, klass, type_to_expand_op (etype->type), args [0]->dreg, -1);
			ins->inst_c1 = arg0_type;
			return ins;
		} else if (is_create_from_half_vectors_overload (fsig)) {
#if defined(TARGET_AMD64)
			// Require Vector64 SIMD support
			if (!COMPILE_LLVM (cfg))
				return NULL;
#endif
			return emit_simd_ins (cfg, klass, OP_XCONCAT, args [0]->dreg, args [1]->dreg);
		}
		else if (is_elementwise_create_overload (fsig, etype))
			return emit_vector_create_elementwise (cfg, fsig, fsig->ret, arg0_type, args);
		break;
	}
	case NI_Vector128_CreateScalar:
	case NI_Vector128_CreateScalarUnsafe: {
		MonoType *etype = get_vector_t_elem_type (fsig->ret);
		if (!MONO_TYPE_IS_VECTOR_PRIMITIVE (etype))
			return NULL;
		gboolean is_unsafe = id == NI_Vector128_CreateScalarUnsafe;
		if (COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, is_unsafe ? OP_CREATE_SCALAR_UNSAFE : OP_CREATE_SCALAR, -1, arg0_type, fsig, args);
		} else {
#ifdef TARGET_AMD64
			MonoInst *ins;

			ins = emit_xzero (cfg, klass);
			ins = emit_simd_ins (cfg, klass, type_to_insert_op (arg0_type), ins->dreg, args [0]->dreg);
			ins->inst_c0 = 0;
			ins->inst_c1 = arg0_type;
			return ins;
#else
			if (type_enum_is_float (arg0_type)) {
				return emit_simd_ins_for_sig (cfg, klass, is_unsafe ? OP_CREATE_SCALAR_UNSAFE_FLOAT : OP_CREATE_SCALAR_FLOAT, -1, arg0_type, fsig, args);
			} else {
				return emit_simd_ins_for_sig (cfg, klass, is_unsafe ? OP_CREATE_SCALAR_UNSAFE_INT : OP_CREATE_SCALAR_INT, -1, arg0_type, fsig, args);
			}
#endif
		}
	}
	case NI_Vector128_Dot: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#if defined(TARGET_WASM)
		if (!COMPILE_LLVM (cfg) && (arg0_type == MONO_TYPE_I8 || arg0_type == MONO_TYPE_U8))
			return NULL;
#elif defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg) && (arg0_type == MONO_TYPE_I8 || arg0_type == MONO_TYPE_U8 || arg0_type == MONO_TYPE_I || arg0_type == MONO_TYPE_U))
			return NULL;
#endif

#if defined(TARGET_ARM64) || defined(TARGET_WASM)
		int instc0 = type_enum_is_float (arg0_type) ? OP_FMUL : OP_IMUL;
		MonoInst *pairwise_multiply = emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, instc0, arg0_type, fsig, args);
		return emit_sum_vector (cfg, fsig->params [0], arg0_type, pairwise_multiply);
#elif defined(TARGET_AMD64)
		int instc =-1;
		if (type_enum_is_float (arg0_type)) {
			if (is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSE41)) {
				int mask_val = -1;
				switch (arg0_type) {
				case MONO_TYPE_R4:
					instc = COMPILE_LLVM (cfg) ? OP_SSE41_DPPS : OP_SSE41_DPPS_IMM;
					mask_val = 0xf1; // 0xf1 ... 0b11110001
					break;
				case MONO_TYPE_R8:
					instc = COMPILE_LLVM (cfg) ? OP_SSE41_DPPD : OP_SSE41_DPPD_IMM;
					mask_val = 0x31; // 0x31 ... 0b00110001
					break;
				default:
					return NULL;
				}	

				MonoInst *dot;
				if (COMPILE_LLVM (cfg)) {
					int mask_reg = alloc_ireg (cfg);
					MONO_EMIT_NEW_ICONST (cfg, mask_reg, mask_val);

					dot = emit_simd_ins (cfg, klass, instc, args [0]->dreg, args [1]->dreg);
					dot->sreg3 = mask_reg;
				} else {
					dot = emit_simd_ins (cfg, klass, instc, args [0]->dreg, args [1]->dreg);
					dot->inst_c0 = mask_val;
				}

				return extract_first_element (cfg, klass, arg0_type, dot->dreg);
			} else {
				instc = OP_FMUL;
			}	
		} else {
			if (arg0_type == MONO_TYPE_I1 || arg0_type == MONO_TYPE_U1)
				return NULL; 	// We don't support sum vector for byte, sbyte types yet

			// FIXME:
			if (!COMPILE_LLVM (cfg))
				return NULL;

			instc = OP_IMUL;
		}
		MonoInst *pairwise_multiply = emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, instc, arg0_type, fsig, args);

		return emit_sum_vector (cfg, fsig->params [0], arg0_type, pairwise_multiply);
#else
		return NULL;
#endif
	}
	case NI_Vector128_Equals:
	case NI_Vector128_EqualsAll:
	case NI_Vector128_EqualsAny: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		if (id == NI_Vector128_Equals)
			return emit_xcompare (cfg, klass, arg0_type, args [0], args [1]);

		if (COMPILE_LLVM (cfg)) {
			switch (id) {
				case NI_Vector128_EqualsAll:
					return emit_xequal (cfg, arg_class, arg0_type, args [0], args [1]);
				case NI_Vector128_EqualsAny: {
					MonoInst *cmp_eq = emit_xcompare (cfg, arg_class, arg0_type, args [0], args [1]);
					MonoInst *zero = emit_xzero (cfg, arg_class);
					return emit_not_xequal (cfg, arg_class, arg0_type, cmp_eq, zero);
				}
			}
		} else {
			MonoInst* cmp = emit_xcompare (cfg, arg_class, arg0_type, args [0], args [1]);
			MonoInst* ret = emit_simd_ins (cfg, mono_defaults.boolean_class, OP_XEXTRACT, cmp->dreg, -1);
			ret->inst_c0 = (id == NI_Vector128_EqualsAll) ? SIMD_EXTR_ARE_ALL_SET : SIMD_EXTR_IS_ANY_SET;
			ret->inst_c1 = mono_class_value_size (klass, NULL);
			return ret;
		}
		g_assert_not_reached ();
	}
	case NI_Vector128_ExtractMostSignificantBits: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#ifdef TARGET_WASM
		if (type_enum_is_float (arg0_type))
			return NULL;

		return emit_simd_ins_for_sig (cfg, klass, OP_WASM_SIMD_BITMASK, -1, -1, fsig, args);
#elif defined(TARGET_ARM64)
		MonoClass* arg_class;
		if (type_enum_is_float (arg0_type)) {
			MonoClass* cast_class;
			if (arg0_type == MONO_TYPE_R4) {
				arg0_type = MONO_TYPE_I4;
				cast_class = mono_defaults.int32_class;
			} else {
				arg0_type = MONO_TYPE_I8;
				cast_class = mono_defaults.int64_class;
			}
			arg_class = create_class_instance ("System.Runtime.Intrinsics", m_class_get_name (klass), m_class_get_byval_arg (cast_class));
		} else {
			arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		}
		
		// FIXME: Add support for Vector64 on arm64
		int size = mono_class_value_size (arg_class, NULL);
		if (size != 16)
			return NULL;

		MonoInst* msb_mask_vec = emit_msb_vector_mask (cfg, arg_class, arg0_type);
		MonoInst* and_res_vec = emit_simd_ins_for_binary_op (cfg, arg_class, fsig, args, arg0_type, NI_Vector128_BitwiseAnd);
		and_res_vec->sreg2 = msb_mask_vec->dreg;

		MonoInst* msb_shift_vec = emit_msb_shift_vector_constant (cfg, arg_class, arg0_type);

		MonoInst* shift_res_vec = emit_simd_ins (cfg, arg_class, OP_XOP_OVR_X_X_X, and_res_vec->dreg, msb_shift_vec->dreg);
		shift_res_vec->inst_c0 = INTRINS_AARCH64_ADV_SIMD_USHL;
		shift_res_vec->inst_c1 = arg0_type;

		MonoInst* result_ins = NULL;
		if (arg0_type == MONO_TYPE_I1 || arg0_type == MONO_TYPE_U1) {
			// Always perform unsigned operations as vector sum and extract operations could sign-extend the result into the GP register
			// making the final result invalid. This is not needed for wider type as the maximum sum of extracted MSB cannot be larger than 8bits
			arg0_type = MONO_TYPE_U1;

			MonoInst* ext_low_vec = emit_simd_ins_for_sig (cfg, arg_class, OP_XLOWER, 8, arg0_type, fsig, &shift_res_vec);
			MonoInst* sum_low_vec = emit_sum_vector (cfg, fsig->params [0], arg0_type, ext_low_vec);
			
			MonoInst* ext_high_vec = emit_simd_ins_for_sig (cfg, arg_class, OP_XUPPER, 8, arg0_type, fsig, &shift_res_vec);
			MonoInst* sum_high_vec = emit_sum_vector (cfg, fsig->params [0], arg0_type, ext_high_vec);			
			
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, sum_high_vec->dreg, sum_high_vec->dreg, 8);
			EMIT_NEW_BIALU (cfg, result_ins, OP_IOR, sum_high_vec->dreg, sum_high_vec->dreg, sum_low_vec->dreg);
		} else {
			result_ins = emit_sum_vector (cfg, fsig->params [0], arg0_type, shift_res_vec);
		}
		return result_ins;
#elif defined(TARGET_AMD64)
		int type = MONO_TYPE_I1;

		switch (arg0_type) {
			case MONO_TYPE_U2:
			case MONO_TYPE_I2: {
				if (!is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSSE3)) 
					return NULL;
					
				type = type_enum_is_unsigned (arg0_type) ? MONO_TYPE_U1 : MONO_TYPE_I1;
				MonoClass* arg_class = mono_class_from_mono_type_internal (fsig->params [0]);

				guint64 shuffle_mask[2];
				shuffle_mask[0] = 0x0F0D0B0907050301; // Place odd bytes in the lower half of vector
				shuffle_mask[1] = 0x8080808080808080; // Zero the upper half

				MonoInst* shuffle_vec = emit_xconst_v128 (cfg, arg_class, (guint8*)shuffle_mask);
				shuffle_vec->klass = arg_class;

				args [0] = emit_simd_ins (cfg, klass, OP_SSSE3_SHUFFLE, args [0]->dreg, shuffle_vec->dreg);
				args [0]->inst_c1 = type;
				break;
			}
#if TARGET_SIZEOF_VOID_P == 4
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
			case MONO_TYPE_R4: {
				type = MONO_TYPE_R4;
				break;
			}
#if TARGET_SIZEOF_VOID_P == 8
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
			case MONO_TYPE_R8: {
				type = MONO_TYPE_R8;
				break;
			}
		}

		return emit_simd_ins_for_sig (cfg, klass, OP_SSE_MOVMSK, -1, type, fsig, args);
#endif
	}
	case NI_Vector128_GetElement: {
		int elems;

		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);

		if (fsig->params [0]->type == MONO_TYPE_GENERICINST) {
			MonoType *etype = mono_class_get_context (arg_class)->class_inst->type_argv [0];
			int size = mono_class_value_size (arg_class, NULL);
			int esize = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
			elems = size / esize;
		} else {
			// This exists to handle the static extension methods for Vector2/3/4, Quaternion, and Plane
			// which live on System.Numerics.Vector

			arg0_type = MONO_TYPE_R4;
			elems = 4;
		}

		if (args [1]->opcode == OP_ICONST) {
			// If the index is provably a constant, we can generate vastly better code.
			int index = GTMREG_TO_INT (args[1]->inst_c0);

			if (index < 0 || index >= elems) {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
				MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");
			} 

			// Bounds check is elided if we know the index is safe.
			int extract_op = type_to_extract_op (arg0_type);
			MonoInst* ret = emit_simd_ins (cfg, args [0]->klass, extract_op, args [0]->dreg, -1);
			ret->inst_c0 = index;
			ret->inst_c1 = fsig->ret->type;
			return ret;
		}

		// Bounds check needed in non-const case.
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");

		gboolean use_xextract;
#ifdef TARGET_AMD64
		use_xextract = FALSE;
#else
		use_xextract = type_to_width_log2 (arg0_type) == 3;
#endif

		if (COMPILE_LLVM (cfg) || use_xextract) {
			// Use optimized paths for 64-bit extractions or whatever LLVM yields if enabled.
			int extract_op = type_to_xextract_op (arg0_type);
			return emit_simd_ins_for_sig (cfg, klass, extract_op, -1, arg0_type, fsig, args);
		} else {
			// Spill the vector reg.
			// Load back from spilled + index << elem_size_log2
			// TODO: on x86, use a LEA
			MonoInst* spilled;
			NEW_VARLOADA_VREG (cfg, spilled, args [0]->dreg, fsig->params [0]);
			MONO_ADD_INS (cfg->cbb, spilled);
			int offset_reg = alloc_lreg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, args [1]->dreg, type_to_width_log2 (arg0_type));
			int addr_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_BIALU(cfg, OP_PADD, addr_reg, spilled->dreg, offset_reg);
			MonoInst* ret;
			int dreg = arg0_type == MONO_TYPE_R4 ? alloc_freg (cfg) : alloc_ireg (cfg);
			NEW_LOAD_MEMBASE (cfg, ret, mono_type_to_load_membase (cfg, fsig->ret), dreg, addr_reg, 0);
			MONO_ADD_INS (cfg->cbb, ret);
			return ret;
		}
		break;
	}
	case NI_Vector128_GetLower:
	case NI_Vector128_GetUpper: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int op = id == NI_Vector128_GetLower ? OP_XLOWER : OP_XUPPER;

#ifdef TARGET_AMD64
		if (!COMPILE_LLVM (cfg))
		  /* These return a Vector64 */
			return NULL;
#endif
		return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
	}
	case NI_Vector128_GreaterThan:
	case NI_Vector128_GreaterThanOrEqual:
	case NI_Vector128_LessThan:
	case NI_Vector128_LessThanOrEqual: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		return emit_xcompare_for_intrinsic (cfg, klass, id, arg0_type, args [0], args [1]);
	}
	case NI_Vector128_GreaterThanAll:
	case NI_Vector128_GreaterThanAny:
	case NI_Vector128_GreaterThanOrEqualAll:
	case NI_Vector128_GreaterThanOrEqualAny:
	case NI_Vector128_LessThanAll:
	case NI_Vector128_LessThanAny:
	case NI_Vector128_LessThanOrEqualAll:
	case NI_Vector128_LessThanOrEqualAny: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		g_assert (fsig->param_count == 2 &&
			fsig->ret->type == MONO_TYPE_BOOLEAN &&
			mono_metadata_type_equal (fsig->params [0], fsig->params [1]));

		gboolean is_all = FALSE;
		switch (id) {
		case NI_Vector128_GreaterThanAll:
		case NI_Vector128_GreaterThanOrEqualAll:
		case NI_Vector128_LessThanAll:
		case NI_Vector128_LessThanOrEqualAll: 
			is_all = TRUE;
			break;
		}

		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		if (COMPILE_LLVM (cfg)) {
			MonoInst *cmp = emit_xcompare_for_intrinsic (cfg, klass, id, arg0_type, args [0], args [1]);
			if (is_all) {
				// for floating point numbers all ones is NaN and so
				// they must be treated differently than integer types
				if (type_enum_is_float (arg0_type)) {
					MonoInst *zero = emit_xzero (cfg, arg_class);
					MonoInst *inverted_cmp = emit_xcompare (cfg, klass, arg0_type, cmp, zero);
					return emit_xequal (cfg, arg_class, arg0_type, inverted_cmp, zero);
				}

				MonoInst *ones = emit_xones (cfg, arg_class);
				return emit_xequal (cfg, arg_class, arg0_type, cmp, ones);
			} else {
				MonoInst *zero = emit_xzero (cfg, arg_class);
				return emit_not_xequal (cfg, arg_class, arg0_type, cmp, zero);
			}
		} else {
			MonoInst* cmp = emit_xcompare_for_intrinsic (cfg, arg_class, id, arg0_type, args [0], args [1]);
			MonoInst* ret = emit_simd_ins (cfg, mono_defaults.boolean_class, OP_XEXTRACT, cmp->dreg, -1);
			ret->inst_c0 = is_all ? SIMD_EXTR_ARE_ALL_SET : SIMD_EXTR_IS_ANY_SET;
			ret->inst_c1 = mono_class_value_size (klass, NULL);
			return ret;
		}
	}
	case NI_Vector128_Narrow: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

#ifdef TARGET_ARM64
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);

		if (size == 16) {
			switch (arg0_type) {
			case MONO_TYPE_R8: {
				MonoInst* ins = emit_simd_ins (cfg, arg_class, OP_ARM64_FCVTN, args [0]->dreg, -1);
				ins->inst_c1 = arg0_type;
				MonoInst* ret = emit_simd_ins (cfg, arg_class, OP_ARM64_FCVTN2, ins->dreg, args [1]->dreg);
				ret->inst_c1 = arg0_type;
				return ret;
			}
			case MONO_TYPE_I2:
			case MONO_TYPE_I4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U2:
			case MONO_TYPE_U4:
			case MONO_TYPE_U8: {
				MonoInst* ins = emit_simd_ins (cfg, arg_class, OP_ARM64_XTN, args [0]->dreg, -1);
				ins->inst_c1 = arg0_type;
				MonoInst* ret = emit_simd_ins (cfg, arg_class, OP_ARM64_XTN2, ins->dreg, args [1]->dreg);
				ret->inst_c1 = arg0_type;
				return ret;
			}
			default:
				return NULL;
			}
		} else {
			if (!COMPILE_LLVM (cfg))
				return NULL;

			switch (arg0_type) {
			case MONO_TYPE_R8: {
				//Widen arg0
				MonoInst *ins1 = emit_simd_ins (cfg, arg_class, OP_XWIDEN_UNSAFE, args [0]->dreg, -1);

				//Insert arg1 to arg0
				int tmp = alloc_ireg (cfg);
				MONO_EMIT_NEW_ICONST (cfg, tmp, 1);
				MonoInst *ins2 = emit_simd_ins (cfg, arg_class, OP_EXTRACT_R8, args [1]->dreg, -1);
				ins2->inst_c0 = 0;
				ins2->inst_c1 = arg0_type;

				MonoType* param_type = get_vector_t_elem_type (fsig->params[0]);
				MonoClass *ivector128_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector128`1", param_type);

				ins1 = emit_simd_ins (cfg, ivector128_inst, OP_XINSERT_R8, ins1->dreg, ins2->dreg);
				ins1->sreg3 = tmp;
				ins1->inst_c1 = arg0_type;

				//ConvertToSingleLower
				return emit_simd_ins (cfg, arg_class, OP_ARM64_FCVTN, ins1->dreg, -1);
			}
			case MONO_TYPE_I2:
			case MONO_TYPE_I4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U2:
			case MONO_TYPE_U4:
			case MONO_TYPE_U8: {
				//Widen arg0
				MonoInst *arg0 = emit_simd_ins (cfg, arg_class, OP_XWIDEN_UNSAFE, args [0]->dreg, -1);

				//Cast arg0 and arg1 to u/int64
				MonoType *type_new;
				MonoTypeEnum type_enum_new;
				if (type_enum_is_unsigned (arg0_type)) {
					type_new = m_class_get_byval_arg (mono_defaults.uint64_class);
					type_enum_new = MONO_TYPE_U8;
				} else {
					type_new = m_class_get_byval_arg (mono_defaults.int64_class);
					type_enum_new = MONO_TYPE_I8;
				}
				MonoClass *ivector128_64_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector128`1", type_new);
				arg0 = emit_simd_ins (cfg, ivector128_64_inst, OP_XCAST, arg0->dreg, -1);
				MonoClass *ivector64_64_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector64`1", type_new);
				MonoInst *arg1 = emit_simd_ins (cfg, ivector64_64_inst, OP_XCAST, args [1]->dreg, -1);

				//Insert arg1 to arg0
				int tmp = alloc_ireg (cfg);
				MONO_EMIT_NEW_ICONST (cfg, tmp, 1);
				arg1 = emit_simd_ins (cfg, ivector64_64_inst, OP_EXTRACT_I8, arg1->dreg, -1);
				arg1->inst_c0 = 0;
				arg1->inst_c1 = type_enum_new;
				MonoType *param_type = get_vector_t_elem_type (fsig->params[0]);
				MonoClass *ivector128_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector128`1", param_type);
				MonoInst *ins = emit_simd_ins (cfg, ivector128_64_inst, OP_XINSERT_I8, arg0->dreg, arg1->dreg);
				ins->sreg3 = tmp;
				ins->inst_c1 = type_enum_new;

				//Cast arg0 back to its original element type (arg0_type)
				ins = emit_simd_ins (cfg, ivector128_inst, OP_XCAST, ins->dreg, -1);

				//ExtractNarrowingLower
				return emit_simd_ins (cfg, ivector128_inst, OP_ARM64_XTN, ins->dreg, -1);
			}
			default:
				return NULL;
			}
		}
#elif defined(TARGET_WASM)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);

		if (size != 16)
			return NULL;

		switch (arg0_type) {
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
			return emit_simd_ins_for_sig (cfg, klass, OP_WASM_EXTRACT_NARROW, -1, -1, fsig, args);
		}

		return NULL;
#else
		return NULL;
#endif
	}
	case NI_Vector128_Negate:
	case NI_Vector128_OnesComplement: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		return emit_simd_ins_for_unary_op (cfg, klass, fsig, args, arg0_type, id);
	} 
	case NI_Vector128_Shuffle: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#ifdef TARGET_WASM
		return emit_simd_ins_for_sig (cfg, klass, OP_WASM_SIMD_SWIZZLE, -1, -1, fsig, args);
#elif defined(TARGET_ARM64)
		if (vector_size == 128 && (arg0_type == MONO_TYPE_I1 || arg0_type == MONO_TYPE_U1))
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_TBL1, 0, fsig, args);
		return NULL;
#elif defined(TARGET_AMD64)
		// FIXME:
		return NULL;
#else
		return NULL;
#endif
	}
	case NI_Vector128_Sum: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#if defined(TARGET_ARM64) || defined(TARGET_AMD64) || defined(TARGET_WASM)
		return emit_sum_vector (cfg, fsig->params [0], arg0_type, args [0]);
#else
		return NULL;
#endif
	}
	case NI_Vector128_Sqrt: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		if (!type_enum_is_float (arg0_type))
			return NULL;
#ifdef TARGET_ARM64
		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FSQRT, arg0_type, fsig, args);
#elif defined(TARGET_AMD64) || defined(TARGET_WASM)
		int instc0 = arg0_type == MONO_TYPE_R4 ? INTRINS_SIMD_SQRT_R4 : INTRINS_SIMD_SQRT_R8;

		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X, instc0, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case NI_Vector128_ToScalar: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int extract_op = type_to_extract_op (arg0_type);
		return emit_simd_ins_for_sig (cfg, klass, extract_op, 0, arg0_type, fsig, args);
	}
	case NI_Vector128_ToVector128:
	case NI_Vector128_ToVector128Unsafe: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int op = id == NI_Vector128_ToVector128 ? OP_XWIDEN : OP_XWIDEN_UNSAFE;
		return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
	}
	case NI_Vector128_WithElement: {
		int elems;
		
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);

		if (fsig->params [0]->type == MONO_TYPE_GENERICINST) {
			MonoType *etype = mono_class_get_context (arg_class)->class_inst->type_argv [0];
			int size = mono_class_value_size (arg_class, NULL);
			int esize = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
			elems = size / esize;
		} else {
			// This exists to handle the static extension methods for Vector2/3/4, Quaternion, and Plane
			// which live on System.Numerics.Vector

			arg0_type = MONO_TYPE_R4;
			elems = 4;
		}

		if (args [1]->opcode == OP_ICONST) {
			// If the index is provably a constant, we can generate vastly better code.
			int index = GTMREG_TO_INT (args[1]->inst_c0);

			if (index < 0 || index >= elems) {
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
					MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");
			}

			int insert_op = type_to_insert_op (arg0_type);
			MonoInst *ins = emit_simd_ins (cfg, klass, insert_op, args [0]->dreg, args [2]->dreg);
			ins->inst_c0 = index;
			ins->inst_c1 = arg0_type;
			return ins;
		} 

		if (!COMPILE_LLVM (cfg) && fsig->params [0]->type != MONO_TYPE_GENERICINST)
			return NULL;

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");

		gboolean use_xextract;
#ifdef TARGET_AMD64
		use_xextract = FALSE;
#else
		use_xextract = type_to_width_log2 (arg0_type) == 3;
#endif

		if (COMPILE_LLVM (cfg) || use_xextract) {
			int insert_op = type_to_xinsert_op (arg0_type);
			MonoInst *ins = emit_simd_ins (cfg, klass, insert_op, args [0]->dreg, args [2]->dreg);
			ins->sreg3 = args [1]->dreg;
			ins->inst_c1 = arg0_type;
			return ins;
		} else {
			// Create a blank reg and spill it.
			// Overwrite memory with original value.
			// Overwrite [spilled + index << elem_size_log2] with replacement value
			// Read back.
			// TODO: on x86, use a LEA
			MonoInst* scratch = emit_xzero (cfg, args [0]->klass);
			MonoInst* scratcha, *ins;
			NEW_VARLOADA_VREG (cfg, scratcha, scratch->dreg, fsig->params [0]);
			MONO_ADD_INS (cfg->cbb, scratcha);
			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, fsig->params [0], scratcha->dreg, 0, args [0]->dreg);

			int offset_reg = alloc_lreg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, args [1]->dreg, type_to_width_log2 (arg0_type));
			int addr_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_BIALU(cfg, OP_PADD, addr_reg, scratcha->dreg, offset_reg);

			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, fsig->params [2], addr_reg, 0, args [2]->dreg);

			MonoInst* ret;
			NEW_LOAD_MEMBASE (cfg, ret, mono_type_to_load_membase (cfg, fsig->ret), scratch->dreg, scratcha->dreg, 0);
			MONO_ADD_INS (cfg->cbb, ret);

			return ret;
		}
		break;
	}
	case NI_Vector128_WidenLower:
	case NI_Vector128_WidenUpper: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			int subop = 0;
			gboolean is_upper = (id == NI_Vector128_WidenUpper);
			if (type_enum_is_float (arg0_type))
				subop = is_upper ? OP_SIMD_FCVTL2 : OP_SIMD_FCVTL;
			else if (type_enum_is_unsigned (arg0_type))
				subop = is_upper ? OP_ARM64_UXTL2 : OP_ARM64_UXTL;
			else
				subop = is_upper ? OP_ARM64_SXTL2 : OP_ARM64_SXTL;
			
			MonoInst* ins = emit_simd_ins (cfg, klass, OP_XUNOP, args [0]->dreg, -1);
			ins->inst_c0 = subop;
			ins->inst_c1 = arg0_type;
			return ins;
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_WASM)
		int op = id == NI_Vector128_WidenLower ? OP_XLOWER : OP_XUPPER;
		MonoInst *lower_or_upper_half = emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
		if (type_enum_is_float (arg0_type)) {
			return emit_simd_ins (cfg, klass, OP_SIMD_FCVTL, lower_or_upper_half->dreg, -1);
		} else {
			int zero = alloc_ireg (cfg);
			MONO_EMIT_NEW_ICONST (cfg, zero, 0);
			op = type_enum_is_unsigned (arg0_type) ? OP_SIMD_USHLL : OP_SIMD_SSHLL;
			return emit_simd_ins (cfg, klass, op, lower_or_upper_half->dreg, zero);
		}
#elif defined(TARGET_AMD64)
		// FIXME:
		return NULL;
#else
		return NULL;
#endif
	}
	case NI_Vector128_WithLower:
	case NI_Vector128_WithUpper: {
#ifdef TARGET_AMD64
		if (!COMPILE_LLVM (cfg))
		  /* These return a Vector64 */
			return NULL;
#endif

		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int op = id == NI_Vector128_WithLower ? OP_XINSERT_LOWER : OP_XINSERT_UPPER;
		return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
	}
	default:
		break;
	}

	return NULL;
}
