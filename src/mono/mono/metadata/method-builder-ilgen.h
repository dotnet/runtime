/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METHOD_BUILDER_ILGEN_H__
#define __MONO_METHOD_BUILDER_ILGEN_H__

#include "config.h"
#include <mono/metadata/class.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/method-builder.h>

MONO_API void
mono_method_builder_ilgen_init (void);

void
mono_mb_patch_addr (MonoMethodBuilder *mb, int pos, int value);

void
mono_mb_patch_addr_s (MonoMethodBuilder *mb, int pos, gint8 value);

void
mono_mb_patch_branch (MonoMethodBuilder *mb, guint32 pos);

void
mono_mb_patch_short_branch (MonoMethodBuilder *mb, guint32 pos);

int
mono_mb_get_label (MonoMethodBuilder *mb);

int
mono_mb_get_pos (MonoMethodBuilder *mb);

void
mono_mb_emit_ptr (MonoMethodBuilder *mb, gpointer ptr);

void
mono_mb_emit_calli (MonoMethodBuilder *mb, MonoMethodSignature *sig);

void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, gpointer func);

#ifdef __cplusplus
template <typename T>
inline void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, T func)
{
	mono_mb_emit_native_call (mb, sig, (gpointer)func);
}
#endif // __cplusplus

void
mono_mb_emit_managed_call (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *opt_sig);

void
mono_mb_emit_icall_id (MonoMethodBuilder *mb, MonoJitICallId jit_icall_id);

#define mono_mb_emit_icall(mb, name) (mono_mb_emit_icall_id ((mb), MONO_JIT_ICALL_ ## name))

int
mono_mb_add_local (MonoMethodBuilder *mb, MonoType *type);

void
mono_mb_emit_ldarg (MonoMethodBuilder *mb, guint argnum);

void
mono_mb_emit_ldarg_addr (MonoMethodBuilder *mb, guint argnum);

void
mono_mb_emit_ldloc (MonoMethodBuilder *mb, guint num);

void
mono_mb_emit_ldloc_addr (MonoMethodBuilder *mb, guint locnum);

void
mono_mb_emit_stloc (MonoMethodBuilder *mb, guint num);

void
mono_mb_emit_exception (MonoMethodBuilder *mb, const char *exc_name, const char *msg);

void
mono_mb_emit_exception_full (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg);

void
mono_mb_emit_exception_for_error (MonoMethodBuilder *mb, MonoError *error);

void
mono_mb_emit_icon (MonoMethodBuilder *mb, gint32 value);

void
mono_mb_emit_icon8 (MonoMethodBuilder *mb, gint64 value);

guint32
mono_mb_emit_branch (MonoMethodBuilder *mb, guint8 op);

guint32
mono_mb_emit_short_branch (MonoMethodBuilder *mb, guint8 op);

void
mono_mb_emit_branch_label (MonoMethodBuilder *mb, guint8 op, guint32 label);

void
mono_mb_emit_add_to_local (MonoMethodBuilder *mb, guint16 local, gint32 incr);

void
mono_mb_emit_ldflda (MonoMethodBuilder *mb, gint32 offset);

void
mono_mb_emit_byte (MonoMethodBuilder *mb, guint8 op);

void
mono_mb_emit_i2 (MonoMethodBuilder *mb, gint16 data);

void
mono_mb_emit_i4 (MonoMethodBuilder *mb, gint32 data);

void
mono_mb_emit_i8 (MonoMethodBuilder *mb, gint64 data);

void
mono_mb_emit_op (MonoMethodBuilder *mb, guint8 op, gpointer data);

void
mono_mb_emit_ldstr (MonoMethodBuilder *mb, char *str);

void
mono_mb_set_clauses (MonoMethodBuilder *mb, int num_clauses, MonoExceptionClause *clauses);

void
mono_mb_set_param_names (MonoMethodBuilder *mb, const char **param_names);

char*
mono_mb_strdup (MonoMethodBuilder *mb, const char *s);

#endif
