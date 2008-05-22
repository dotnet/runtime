#ifndef __MONO_JIT_ICALLS_H__
#define __MONO_JIT_ICALLS_H__

#include <math.h>

#include "mini.h"

void* mono_ldftn (MonoMethod *method) MONO_INTERNAL;

void* mono_ldftn_nosync (MonoMethod *method) MONO_INTERNAL;

void* mono_ldvirtfn (MonoObject *obj, MonoMethod *method) MONO_INTERNAL;

void mono_helper_stelem_ref_check (MonoArray *array, MonoObject *val) MONO_INTERNAL;

gint64 mono_llmult (gint64 a, gint64 b) MONO_INTERNAL;

guint64 mono_llmult_ovf_un (guint64 a, guint64 b) MONO_INTERNAL;

guint64 mono_llmult_ovf (gint64 a, gint64 b) MONO_INTERNAL;

gint32 mono_idiv (gint32 a, gint32 b) MONO_INTERNAL;

guint32 mono_idiv_un (guint32 a, guint32 b) MONO_INTERNAL;

gint32 mono_irem (gint32 a, gint32 b) MONO_INTERNAL;

guint32 mono_irem_un (guint32 a, guint32 b) MONO_INTERNAL;

gint32 mono_imul (gint32 a, gint32 b) MONO_INTERNAL;

gint32 mono_imul_ovf (gint32 a, gint32 b) MONO_INTERNAL;

gint32 mono_imul_ovf_un (guint32 a, guint32 b) MONO_INTERNAL;

double mono_fdiv (double a, double b) MONO_INTERNAL;

gint64 mono_lldiv (gint64 a, gint64 b) MONO_INTERNAL;

gint64 mono_llrem (gint64 a, gint64 b) MONO_INTERNAL;

guint64 mono_lldiv_un (guint64 a, guint64 b) MONO_INTERNAL;

guint64 mono_llrem_un (guint64 a, guint64 b) MONO_INTERNAL;

guint64 mono_lshl (guint64 a, gint32 shamt) MONO_INTERNAL;

guint64 mono_lshr_un (guint64 a, gint32 shamt) MONO_INTERNAL;

gint64 mono_lshr (gint64 a, gint32 shamt) MONO_INTERNAL;

MonoArray *mono_array_new_va (MonoMethod *cm, ...) MONO_INTERNAL;

gpointer mono_class_static_field_address (MonoDomain *domain, MonoClassField *field) MONO_INTERNAL;

gpointer mono_ldtoken_wrapper (MonoImage *image, int token, MonoGenericContext *context) MONO_INTERNAL;

gpointer mono_ldtoken_wrapper_generic_shared (MonoImage *image, int token, MonoMethod *method) MONO_INTERNAL;

guint64 mono_fconv_u8 (double v) MONO_INTERNAL;

gint64 mono_fconv_i8 (double v) MONO_INTERNAL;

guint32 mono_fconv_u4 (double v) MONO_INTERNAL;

gint64 mono_fconv_ovf_i8 (double v) MONO_INTERNAL;

guint64 mono_fconv_ovf_u8 (double v) MONO_INTERNAL;

double mono_lconv_to_r8 (gint64 a) MONO_INTERNAL;

double mono_conv_to_r8 (gint32 a) MONO_INTERNAL;

double mono_conv_to_r4 (gint32 a) MONO_INTERNAL;

float mono_lconv_to_r4 (gint64 a) MONO_INTERNAL;

double mono_conv_to_r8_un (guint32 a) MONO_INTERNAL;

double mono_lconv_to_r8_un (guint64 a) MONO_INTERNAL;

gpointer mono_helper_compile_generic_method (MonoObject *obj, MonoMethod *method, MonoGenericContext *context, gpointer *this_arg) MONO_INTERNAL;

MonoString *mono_helper_ldstr (MonoImage *image, guint32 idx) MONO_INTERNAL;

MonoString *mono_helper_ldstr_mscorlib (guint32 idx) MONO_INTERNAL;

MonoObject *mono_helper_newobj_mscorlib (guint32 idx) MONO_INTERNAL;

double mono_fsub (double a, double b) MONO_INTERNAL;

double mono_fadd (double a, double b) MONO_INTERNAL;

double mono_fmul (double a, double b) MONO_INTERNAL;

double mono_fneg (double a) MONO_INTERNAL;

double mono_fconv_r4 (double a) MONO_INTERNAL;

gint8 mono_fconv_i1 (double a) MONO_INTERNAL;

gint16 mono_fconv_i2 (double a) MONO_INTERNAL;

gint32 mono_fconv_i4 (double a) MONO_INTERNAL;

guint8 mono_fconv_u1 (double a) MONO_INTERNAL;

guint16 mono_fconv_u2 (double a) MONO_INTERNAL;

gboolean mono_fcmp_eq (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_ge (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_gt (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_le (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_lt (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_ne_un (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_ge_un (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_gt_un (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_le_un (double a, double b) MONO_INTERNAL;

gboolean mono_fcmp_lt_un (double a, double b) MONO_INTERNAL;

gboolean mono_fceq (double a, double b) MONO_INTERNAL;

gboolean mono_fcgt (double a, double b) MONO_INTERNAL;

gboolean mono_fcgt_un (double a, double b) MONO_INTERNAL;

gboolean mono_fclt (double a, double b) MONO_INTERNAL;

gboolean mono_fclt_un (double a, double b) MONO_INTERNAL;

double   mono_fload_r4 (float *ptr) MONO_INTERNAL;

void     mono_fstore_r4 (double val, float *ptr) MONO_INTERNAL;

guint32  mono_fload_r4_arg (double val) MONO_INTERNAL;

void     mono_break (void) MONO_INTERNAL;

MonoException *mono_create_corlib_exception_0 (guint32 token) MONO_INTERNAL;

MonoException *mono_create_corlib_exception_1 (guint32 token, MonoString *arg) MONO_INTERNAL;

MonoException *mono_create_corlib_exception_2 (guint32 token, MonoString *arg1, MonoString *arg2) MONO_INTERNAL;

#endif /* __MONO_JIT_ICALLS_H__ */

