/**
 * \file
 */

#ifndef __MONO_JIT_ICALLS_H__
#define __MONO_JIT_ICALLS_H__

#include <math.h>
#include "mini.h"
#include <mono/metadata/icalls.h>

ICALL_EXPORT void* mono_ldftn (MonoMethod *method);

ICALL_EXPORT void* mono_ldvirtfn (MonoObject *obj, MonoMethod *method);

ICALL_EXPORT void* mono_ldvirtfn_gshared (MonoObject *obj, MonoMethod *method);

ICALL_EXPORT void mono_helper_stelem_ref_check (MonoArray *array, MonoObject *val);

ICALL_EXPORT gint64 mono_llmult (gint64 a, gint64 b);

ICALL_EXPORT guint64 mono_llmult_ovf_un (guint64 a, guint64 b);

ICALL_EXPORT guint64 mono_llmult_ovf_un_oom (guint64 a, guint64 b);

ICALL_EXPORT guint64 mono_llmult_ovf (gint64 a, gint64 b);

ICALL_EXPORT gint32 mono_idiv (gint32 a, gint32 b);

ICALL_EXPORT guint32 mono_idiv_un (guint32 a, guint32 b);

ICALL_EXPORT gint32 mono_irem (gint32 a, gint32 b);

ICALL_EXPORT guint32 mono_irem_un (guint32 a, guint32 b);

ICALL_EXPORT gint32 mono_imul (gint32 a, gint32 b);

ICALL_EXPORT gint32 mono_imul_ovf (gint32 a, gint32 b);

ICALL_EXPORT gint32 mono_imul_ovf_un (guint32 a, guint32 b);

ICALL_EXPORT gint32 mono_imul_ovf_un_oom (guint32 a, guint32 b);

ICALL_EXPORT double mono_fdiv (double a, double b);

ICALL_EXPORT gint64 mono_lldiv (gint64 a, gint64 b);

ICALL_EXPORT gint64 mono_llrem (gint64 a, gint64 b);

ICALL_EXPORT guint64 mono_lldiv_un (guint64 a, guint64 b);

ICALL_EXPORT guint64 mono_llrem_un (guint64 a, guint64 b);

ICALL_EXPORT guint64 mono_lshl (guint64 a, gint32 shamt);

ICALL_EXPORT guint64 mono_lshr_un (guint64 a, gint32 shamt);

ICALL_EXPORT gint64 mono_lshr (gint64 a, gint32 shamt);

// For param_count > 4.
ICALL_EXPORT MonoArray *mono_array_new_n_icall (MonoMethod *cm, gint32 param_count, intptr_t *params);

ICALL_EXPORT MonoArray *mono_array_new_2_jagged (MonoMethod *cm, guint32 length1, guint32 length2);

ICALL_EXPORT MonoArray *mono_array_new_1 (MonoMethod *cm, guint32 length);

ICALL_EXPORT MonoArray *mono_array_new_2 (MonoMethod *cm, guint32 length1, guint32 length2);

ICALL_EXPORT MonoArray *mono_array_new_3 (MonoMethod *cm, guint32 length1, guint32 length2, guint32 length3);

ICALL_EXPORT MonoArray *mono_array_new_4 (MonoMethod *cm, guint32 length1, guint32 length2, guint32 length3, guint32 length4);

ICALL_EXPORT gpointer mono_class_static_field_address (MonoClassField *field);

ICALL_EXPORT gpointer mono_ldtoken_wrapper (MonoImage *image, int token, MonoGenericContext *context);

ICALL_EXPORT gpointer mono_ldtoken_wrapper_generic_shared (MonoImage *image, int token, MonoMethod *method);

ICALL_EXPORT guint64 mono_fconv_u8 (double v);

ICALL_EXPORT guint64 mono_rconv_u8 (float v);

ICALL_EXPORT gint64 mono_fconv_i8 (double v);

ICALL_EXPORT guint32 mono_fconv_u4 (double v);

ICALL_EXPORT guint32 mono_rconv_u4 (float v);

ICALL_EXPORT gint64 mono_fconv_ovf_i8 (double v);

ICALL_EXPORT guint64 mono_fconv_ovf_u8 (double v);

ICALL_EXPORT gint64 mono_rconv_i8 (float v);

ICALL_EXPORT gint64 mono_rconv_ovf_i8 (float v);

ICALL_EXPORT guint64 mono_rconv_ovf_u8 (float v);

ICALL_EXPORT double mono_lconv_to_r8 (gint64 a);

ICALL_EXPORT double mono_conv_to_r8 (gint32 a);

ICALL_EXPORT double mono_conv_to_r4 (gint32 a);

ICALL_EXPORT float mono_lconv_to_r4 (gint64 a);

ICALL_EXPORT double mono_conv_to_r8_un (guint32 a);

ICALL_EXPORT double mono_lconv_to_r8_un (guint64 a);

ICALL_EXPORT gpointer mono_helper_compile_generic_method (MonoObject *obj, MonoMethod *method, gpointer *this_arg);

ICALL_EXPORT MonoString *mono_helper_ldstr (MonoImage *image, guint32 idx);

ICALL_EXPORT MonoString *mono_helper_ldstr_mscorlib (guint32 idx);

ICALL_EXPORT MonoObject *mono_helper_newobj_mscorlib (guint32 idx);

ICALL_EXPORT double mono_fsub (double a, double b);

ICALL_EXPORT double mono_fadd (double a, double b);

ICALL_EXPORT double mono_fmul (double a, double b);

ICALL_EXPORT double mono_fneg (double a);

ICALL_EXPORT double mono_fconv_r4 (double a);

ICALL_EXPORT gint8 mono_fconv_i1 (double a);

ICALL_EXPORT gint16 mono_fconv_i2 (double a);

ICALL_EXPORT gint32 mono_fconv_i4 (double a);

ICALL_EXPORT guint8 mono_fconv_u1 (double a);

ICALL_EXPORT guint16 mono_fconv_u2 (double a);

ICALL_EXPORT gboolean mono_fcmp_eq (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_ge (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_gt (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_le (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_lt (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_ne_un (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_ge_un (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_gt_un (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_le_un (double a, double b);

ICALL_EXPORT gboolean mono_fcmp_lt_un (double a, double b);

ICALL_EXPORT gboolean mono_fceq (double a, double b);

ICALL_EXPORT gboolean mono_fcgt (double a, double b);

ICALL_EXPORT gboolean mono_fcgt_un (double a, double b);

ICALL_EXPORT gboolean mono_fclt (double a, double b);

ICALL_EXPORT gboolean mono_fclt_un (double a, double b);

ICALL_EXPORT double   mono_fload_r4 (float *ptr);

ICALL_EXPORT void     mono_fstore_r4 (double val, float *ptr);

ICALL_EXPORT guint32  mono_fload_r4_arg (double val);

ICALL_EXPORT double mono_fmod (double a, double b);

ICALL_EXPORT void     mono_break (void);

ICALL_EXPORT MonoException *mono_create_corlib_exception_0 (guint32 token);

ICALL_EXPORT MonoException *mono_create_corlib_exception_1 (guint32 token, MonoString *arg);

ICALL_EXPORT MonoException *mono_create_corlib_exception_2 (guint32 token, MonoString *arg1, MonoString *arg2);

ICALL_EXPORT MonoObject* mono_object_castclass_unbox (MonoObject *obj, MonoClass *klass);

ICALL_EXPORT gpointer mono_get_native_calli_wrapper (MonoImage *image, MonoMethodSignature *sig, gpointer func);

ICALL_EXPORT MonoObject* mono_object_isinst_with_cache (MonoObject *obj, MonoClass *klass, gpointer *cache);

ICALL_EXPORT MonoObject* mono_object_castclass_with_cache (MonoObject *obj, MonoClass *klass, gpointer *cache);

ICALL_EXPORT
void
ves_icall_runtime_class_init (MonoVTable *vtable);

ICALL_EXPORT void
mono_generic_class_init (MonoVTable *vtable);

ICALL_EXPORT
void
ves_icall_mono_delegate_ctor (MonoObject *this_obj, MonoObject *target, gpointer addr);

ICALL_EXPORT
void
ves_icall_mono_delegate_ctor_interp (MonoObject *this_obj, MonoObject *target, gpointer addr);

ICALL_EXPORT gpointer mono_gsharedvt_constrained_call_fast (gpointer mp, MonoGsharedvtConstrainedCallInfo *info, gpointer *out_receiver);

ICALL_EXPORT MonoObject* mono_gsharedvt_constrained_call (gpointer mp, MonoMethod *cmethod, MonoClass *klass,
														  MonoGsharedvtConstrainedCallInfo *info, guint8 *deref_args, gpointer *args);

ICALL_EXPORT void mono_gsharedvt_value_copy (gpointer dest, gpointer src, MonoClass *klass);

ICALL_EXPORT gpointer mono_fill_class_rgctx (MonoVTable *vtable, int index);

ICALL_EXPORT gpointer mono_fill_method_rgctx (MonoMethodRuntimeGenericContext *mrgctx, int index);

ICALL_EXPORT MonoObject* mono_get_assembly_object (MonoImage *image);

ICALL_EXPORT MonoObject* mono_get_method_object (MonoMethod *method);

ICALL_EXPORT double mono_ckfinite (double d);

ICALL_EXPORT void mono_throw_method_access (MonoMethod *caller, MonoMethod *callee);

ICALL_EXPORT void mono_throw_ambiguous_implementation (void);

ICALL_EXPORT void mono_throw_bad_image (void);

ICALL_EXPORT void mono_throw_not_supported (void);

ICALL_EXPORT void mono_throw_platform_not_supported (void);

ICALL_EXPORT void mono_throw_invalid_program (const char *msg);

ICALL_EXPORT void mono_throw_type_load (MonoClass* klass);

ICALL_EXPORT void mono_dummy_jit_icall (void);

ICALL_EXPORT void mono_dummy_jit_icall_val (gpointer ptr);

ICALL_EXPORT void mono_dummy_runtime_init_callback (void);

ICALL_EXPORT void mini_init_method_rgctx (MonoMethodRuntimeGenericContext *mrgctx, MonoGSharedMethodInfo *info);

#endif /* __MONO_JIT_ICALLS_H__ */
