/**
 * \file
 */

#ifndef __MONO_JIT_ICALLS_H__
#define __MONO_JIT_ICALLS_H__

#include <math.h>

#include "mini.h"

void* mono_ldftn (MonoMethod *method);

void* mono_ldvirtfn (MonoObject *obj, MonoMethod *method);

void* mono_ldvirtfn_gshared (MonoObject *obj, MonoMethod *method);

void mono_helper_stelem_ref_check (MonoArray *array, MonoObject *val);

gint64 mono_llmult (gint64 a, gint64 b);

guint64 mono_llmult_ovf_un (guint64 a, guint64 b);

guint64 mono_llmult_ovf (gint64 a, gint64 b);

gint32 mono_idiv (gint32 a, gint32 b);

guint32 mono_idiv_un (guint32 a, guint32 b);

gint32 mono_irem (gint32 a, gint32 b);

guint32 mono_irem_un (guint32 a, guint32 b);

gint32 mono_imul (gint32 a, gint32 b);

gint32 mono_imul_ovf (gint32 a, gint32 b);

gint32 mono_imul_ovf_un (guint32 a, guint32 b);

double mono_fdiv (double a, double b);

gint64 mono_lldiv (gint64 a, gint64 b);

gint64 mono_llrem (gint64 a, gint64 b);

guint64 mono_lldiv_un (guint64 a, guint64 b);

guint64 mono_llrem_un (guint64 a, guint64 b);

guint64 mono_lshl (guint64 a, gint32 shamt);

guint64 mono_lshr_un (guint64 a, gint32 shamt);

gint64 mono_lshr (gint64 a, gint32 shamt);

MonoArray *mono_array_new_va (MonoMethod *cm, ...);

MonoArray *mono_array_new_1 (MonoMethod *cm, guint32 length);

MonoArray *mono_array_new_2 (MonoMethod *cm, guint32 length1, guint32 length2);

MonoArray *mono_array_new_3 (MonoMethod *cm, guint32 length1, guint32 length2, guint32 length3);

MonoArray *mono_array_new_4 (MonoMethod *cm, guint32 length1, guint32 length2, guint32 length3, guint32 length4);

gpointer mono_class_static_field_address (MonoDomain *domain, MonoClassField *field);

gpointer mono_ldtoken_wrapper (MonoImage *image, int token, MonoGenericContext *context);

gpointer mono_ldtoken_wrapper_generic_shared (MonoImage *image, int token, MonoMethod *method);

guint64 mono_fconv_u8 (double v);

guint64 mono_rconv_u8 (float v);

gint64 mono_fconv_i8 (double v);

guint32 mono_fconv_u4 (double v);

gint64 mono_fconv_ovf_i8 (double v);

guint64 mono_fconv_ovf_u8 (double v);

gint64 mono_rconv_i8 (float v);

gint64 mono_rconv_ovf_i8 (float v);

guint64 mono_rconv_ovf_u8 (float v);

double mono_lconv_to_r8 (gint64 a);

double mono_conv_to_r8 (gint32 a);

double mono_conv_to_r4 (gint32 a);

float mono_lconv_to_r4 (gint64 a);

double mono_conv_to_r8_un (guint32 a);

double mono_lconv_to_r8_un (guint64 a);

gpointer mono_helper_compile_generic_method (MonoObject *obj, MonoMethod *method, gpointer *this_arg);

MonoString*
ves_icall_mono_ldstr (MonoDomain *domain, MonoImage *image, guint32 idx);

MonoString *mono_helper_ldstr (MonoImage *image, guint32 idx);

MonoString *mono_helper_ldstr_mscorlib (guint32 idx);

MonoObject *mono_helper_newobj_mscorlib (guint32 idx);

double mono_fsub (double a, double b);

double mono_fadd (double a, double b);

double mono_fmul (double a, double b);

double mono_fneg (double a);

double mono_fconv_r4 (double a);

gint8 mono_fconv_i1 (double a);

gint16 mono_fconv_i2 (double a);

gint32 mono_fconv_i4 (double a);

guint8 mono_fconv_u1 (double a);

guint16 mono_fconv_u2 (double a);

gboolean mono_fcmp_eq (double a, double b);

gboolean mono_fcmp_ge (double a, double b);

gboolean mono_fcmp_gt (double a, double b);

gboolean mono_fcmp_le (double a, double b);

gboolean mono_fcmp_lt (double a, double b);

gboolean mono_fcmp_ne_un (double a, double b);

gboolean mono_fcmp_ge_un (double a, double b);

gboolean mono_fcmp_gt_un (double a, double b);

gboolean mono_fcmp_le_un (double a, double b);

gboolean mono_fcmp_lt_un (double a, double b);

gboolean mono_fceq (double a, double b);

gboolean mono_fcgt (double a, double b);

gboolean mono_fcgt_un (double a, double b);

gboolean mono_fclt (double a, double b);

gboolean mono_fclt_un (double a, double b);

gboolean mono_isfinite (double a);

double   mono_fload_r4 (float *ptr);

void     mono_fstore_r4 (double val, float *ptr);

guint32  mono_fload_r4_arg (double val);

void     mono_break (void);

MonoException *mono_create_corlib_exception_0 (guint32 token);

MonoException *mono_create_corlib_exception_1 (guint32 token, MonoString *arg);

MonoException *mono_create_corlib_exception_2 (guint32 token, MonoString *arg1, MonoString *arg2);

MonoObject* mono_object_castclass_unbox (MonoObject *obj, MonoClass *klass);

gpointer mono_get_native_calli_wrapper (MonoImage *image, MonoMethodSignature *sig, gpointer func);

MonoObject*
mono_object_isinst_with_cache (MonoObject *obj, MonoClass *klass, gpointer *cache);

MonoObject*
mono_object_castclass_with_cache (MonoObject *obj, MonoClass *klass, gpointer *cache);

void
ves_icall_runtime_class_init (MonoVTable *vtable);

void
mono_generic_class_init (MonoVTable *vtable);

void
ves_icall_mono_delegate_ctor (MonoObject *this_obj, MonoObject *target, gpointer addr);

MonoObject*
mono_gsharedvt_constrained_call (gpointer mp, MonoMethod *cmethod, MonoClass *klass, gboolean deref_arg, gpointer *args);

void mono_gsharedvt_value_copy (gpointer dest, gpointer src, MonoClass *klass);

gpointer mono_fill_class_rgctx (MonoVTable *vtable, int index);

gpointer mono_fill_method_rgctx (MonoMethodRuntimeGenericContext *mrgctx, int index);

gpointer mono_resolve_iface_call_gsharedvt (MonoObject *this_obj, int imt_slot, MonoMethod *imt_method, gpointer *out_arg);

gpointer mono_resolve_vcall_gsharedvt (MonoObject *this_obj, int imt_slot, MonoMethod *imt_method, gpointer *out_arg);

MonoFtnDesc* mono_resolve_generic_virtual_call (MonoVTable *vt, int slot, MonoMethod *imt_method);

MonoFtnDesc* mono_resolve_generic_virtual_iface_call (MonoVTable *vt, int imt_slot, MonoMethod *imt_method);

gpointer mono_init_vtable_slot (MonoVTable *vtable, int slot);

void mono_llvmonly_init_delegate (MonoDelegate *del);

void mono_llvmonly_init_delegate_virtual (MonoDelegate *del, MonoObject *target, MonoMethod *method);

MonoObject* mono_get_assembly_object (MonoImage *image);

MonoObject* mono_get_method_object (MonoMethod *method);

double mono_ckfinite (double d);

void mono_throw_method_access (MonoMethod *caller, MonoMethod *callee);

void mono_dummy_jit_icall (void);

#endif /* __MONO_JIT_ICALLS_H__ */
