#ifndef __MONO_JIT_ICALLS_H__
#define __MONO_JIT_ICALLS_H__

#include <math.h>

#include "mini.h"

void* mono_ldftn (MonoMethod *method);

void* mono_ldftn_nosync (MonoMethod *method);

void* mono_ldvirtfn (MonoObject *obj, MonoMethod *method);

void helper_stelem_ref (MonoArray *array, int index, MonoObject *val);

void helper_stelem_ref_check (MonoArray *array, MonoObject *val);

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

gpointer ves_array_element_address (MonoArray *this, ...);

MonoArray *mono_array_new_va (MonoMethod *cm, ...);

gpointer mono_class_static_field_address (MonoDomain *domain, MonoClassField *field);

gpointer mono_ldtoken_wrapper (MonoImage *image, int token, MonoGenericContext *context);

guint64 mono_fconv_u8 (double v);

gint64 mono_fconv_i8 (double v);

guint32 mono_fconv_u4 (double v);

gint64 mono_fconv_ovf_i8 (double v);

guint64 mono_fconv_ovf_u8 (double v);

double mono_lconv_to_r8 (gint64 a);

float mono_lconv_to_r4 (gint64 a);

double mono_conv_to_r8_un (guint32 a);

double mono_lconv_to_r8_un (guint64 a);

gpointer helper_compile_generic_method (MonoObject *obj, MonoMethod *method, MonoGenericContext *context);

MonoString *helper_ldstr (MonoImage *image, guint32 idx);

#endif /* __MONO_JIT_ICALLS_H__ */
