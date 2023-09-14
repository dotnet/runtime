#ifndef __MONO_INTRINSICS_HELPER_H__
#define __MONO_INTRINSICS_HELPER_H__

gboolean is_SIMD_feature_supported(MonoCompile *cfg, MonoCPUFeatures feature);
gboolean is_elementwise_ctor (MonoMethodSignature *fsig, MonoType *etype);
gboolean is_elementwise_create_overload (MonoMethodSignature *fsig, MonoType *ret_type);
gboolean is_create_from_half_vectors_overload (MonoMethodSignature *fsig);
gboolean is_element_type_primitive (MonoType *vector_type);
gboolean is_intrinsics_vector_type (MonoType *vector_type);
MonoType* get_vector_t_elem_type (MonoType *vector_type);
gboolean type_is_unsigned (MonoType *type);
gboolean type_enum_is_unsigned (MonoTypeEnum type);
gboolean type_is_float (MonoType *type);
gboolean type_enum_is_float (MonoTypeEnum type);
int type_to_expand_op (MonoTypeEnum type);
int type_to_insert_op (MonoTypeEnum type);
int type_to_xinsert_op (MonoTypeEnum type);
int type_to_xextract_op (MonoTypeEnum type);
int type_to_extract_op (MonoTypeEnum type);
int type_to_width_log2 (MonoTypeEnum type);
MonoTypeEnum get_underlying_type (MonoType* type);
MonoClass * create_class_instance (const char* name_space, const char *name, MonoType *param_type);
MonoInst* extract_first_element (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, int sreg);
MonoInst* emit_simd_ins_for_binary_op (MonoCompile *cfg, MonoClass *klass, MonoMethodSignature *fsig, MonoInst **args, MonoTypeEnum arg_type, int id);
MonoInst* emit_simd_ins_for_unary_op (MonoCompile *cfg, MonoClass *klass, MonoMethodSignature *fsig, MonoInst **args, MonoTypeEnum arg_type, int id);
MonoInst* emit_xcompare (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum etype, MonoInst *arg1, MonoInst *arg2);
MonoInst* emit_xcompare_for_intrinsic (MonoCompile *cfg, MonoClass *klass, int intrinsic_id, MonoTypeEnum etype, MonoInst *arg1, MonoInst *arg2);
MonoInst* emit_xequal (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, MonoInst *arg1, MonoInst *arg2);
MonoInst* emit_not_xequal (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, MonoInst *arg1, MonoInst *arg2);
MonoInst* emit_xzero (MonoCompile *cfg, MonoClass *klass);
MonoInst* emit_xones (MonoCompile *cfg, MonoClass *klass);
MonoInst* emit_xconst_v128 (MonoCompile *cfg, MonoClass *klass, guint8 value[16]);
#ifdef TARGET_ARM64
MonoInst* emit_msb_vector_mask (MonoCompile *cfg, MonoClass *arg_class, MonoTypeEnum arg_type);
MonoInst* emit_msb_shift_vector_constant (MonoCompile *cfg, MonoClass *arg_class, MonoTypeEnum arg_type);
#endif
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_WASM)
MonoInst* emit_sum_vector (MonoCompile *cfg, MonoType *vector_type, MonoTypeEnum element_type, MonoInst *arg);
#endif
MonoInst* emit_vector_create_elementwise (MonoCompile *cfg, MonoMethodSignature *fsig, MonoType *vtype, MonoTypeEnum type, MonoInst **args);
MonoInst* emit_simd_ins (MonoCompile *cfg, MonoClass *klass, int opcode, int sreg1, int sreg2);
MonoInst* emit_simd_ins_for_sig (MonoCompile *cfg, MonoClass *klass, int opcode, int instc0, int instc1, MonoMethodSignature *fsig, MonoInst **args);

#endif
