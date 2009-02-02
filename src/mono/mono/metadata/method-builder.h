/*
 * method-builder.h: Functions for creating IL methods at runtime.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#ifndef __MONO_METHOD_BUILDER_H__
#define __MONO_METHOD_BUILDER_H__

#include <mono/metadata/class.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/reflection.h>

G_BEGIN_DECLS

typedef struct _MonoMethodBuilder {
	MonoMethod *method;
	char *name;
	GList *locals_list;
	int locals;
	gboolean dynamic;
	gboolean no_dup_name;
	gboolean skip_visibility;
	guint32 code_size, pos;
	unsigned char *code;
	int num_clauses;
	MonoExceptionClause *clauses;
} MonoMethodBuilder;

MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name, MonoWrapperType type) MONO_INTERNAL;

MonoMethodBuilder *
mono_mb_new_no_dup_name (MonoClass *klass, const char *name, MonoWrapperType type) MONO_INTERNAL;

void
mono_mb_free (MonoMethodBuilder *mb) MONO_INTERNAL;

void
mono_mb_patch_addr (MonoMethodBuilder *mb, int pos, int value) MONO_INTERNAL;

void
mono_mb_patch_addr_s (MonoMethodBuilder *mb, int pos, gint8 value) MONO_INTERNAL;

void
mono_mb_patch_branch (MonoMethodBuilder *mb, guint32 pos) MONO_INTERNAL;

void
mono_mb_patch_short_branch (MonoMethodBuilder *mb, guint32 pos) MONO_INTERNAL;

int
mono_mb_get_label (MonoMethodBuilder *mb) MONO_INTERNAL;

int
mono_mb_get_pos (MonoMethodBuilder *mb) MONO_INTERNAL;

guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data) MONO_INTERNAL;

void
mono_mb_emit_ptr (MonoMethodBuilder *mb, gpointer ptr) MONO_INTERNAL;

void
mono_mb_emit_calli (MonoMethodBuilder *mb, MonoMethodSignature *sig) MONO_INTERNAL;

void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, gpointer func) MONO_INTERNAL;

void
mono_mb_emit_managed_call (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *opt_sig) MONO_INTERNAL;

void
mono_mb_emit_icall (MonoMethodBuilder *mb, gpointer func) MONO_INTERNAL;

int
mono_mb_add_local (MonoMethodBuilder *mb, MonoType *type) MONO_INTERNAL;

MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack) MONO_INTERNAL;

void
mono_mb_emit_ldarg (MonoMethodBuilder *mb, guint argnum) MONO_INTERNAL;

void
mono_mb_emit_ldarg_addr (MonoMethodBuilder *mb, guint argnum) MONO_INTERNAL;

void
mono_mb_emit_ldloc (MonoMethodBuilder *mb, guint num) MONO_INTERNAL;

void
mono_mb_emit_ldloc_addr (MonoMethodBuilder *mb, guint locnum) MONO_INTERNAL;

void
mono_mb_emit_stloc (MonoMethodBuilder *mb, guint num) MONO_INTERNAL;

void
mono_mb_emit_exception (MonoMethodBuilder *mb, const char *exc_name, const char *msg) MONO_INTERNAL;

void
mono_mb_emit_exception_full (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg) MONO_INTERNAL;

void
mono_mb_emit_icon (MonoMethodBuilder *mb, gint32 value) MONO_INTERNAL;

guint32
mono_mb_emit_branch (MonoMethodBuilder *mb, guint8 op) MONO_INTERNAL;

guint32
mono_mb_emit_short_branch (MonoMethodBuilder *mb, guint8 op) MONO_INTERNAL;

void
mono_mb_emit_branch_label (MonoMethodBuilder *mb, guint8 op, guint32 label) MONO_INTERNAL;

void
mono_mb_emit_add_to_local (MonoMethodBuilder *mb, guint16 local, gint32 incr) MONO_INTERNAL;

void
mono_mb_emit_ldflda (MonoMethodBuilder *mb, gint32 offset) MONO_INTERNAL;

void
mono_mb_emit_byte (MonoMethodBuilder *mb, guint8 op) MONO_INTERNAL;

void
mono_mb_emit_i2 (MonoMethodBuilder *mb, gint16 data) MONO_INTERNAL;

void
mono_mb_emit_i4 (MonoMethodBuilder *mb, gint32 data) MONO_INTERNAL;

void
mono_mb_emit_op (MonoMethodBuilder *mb, guint8 op, gpointer data) MONO_INTERNAL;

void
mono_mb_emit_ldstr (MonoMethodBuilder *mb, char *str) MONO_INTERNAL;

void
mono_mb_set_clauses (MonoMethodBuilder *mb, int num_clauses, MonoExceptionClause *clauses) MONO_INTERNAL;

G_END_DECLS

#endif /* __MONO_METHOD_BUILDER_H__ */

