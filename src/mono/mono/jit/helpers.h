/*
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_JIT_HELPERS_H_
#define _MONO_JIT_HELPERS_H_

#include <config.h>

#include "jit.h"

int
mono_map_store_svt_type    (int svt);

void
mono_get_val_sizes         (MonoValueType type, int *size, int *align);

int
mono_map_stind_type        (MonoType *type);

int
mono_map_remote_stind_type (MonoType *type);

int
mono_map_starg_type        (MonoType *type);

int
mono_map_arg_type          (MonoType *type);

int
mono_map_ldind_type        (MonoType *type, MonoValueType *svt);

int
mono_map_ldarg_type        (MonoType *type, MonoValueType *svt);

int
mono_map_call_type         (MonoType *type, MonoValueType *svt);

MBTree *
mono_ctree_new             (MonoMemPool *mp, int op, MBTree *left, 
			    MBTree *right);
MBTree *
mono_ctree_new_leaf        (MonoMemPool *mp, int op);

MBTree *
mono_ctree_new_icon4       (MonoMemPool *mp, gint32 data);

void
mono_print_ctree           (MonoFlowGraph *cfg, MBTree *tree);

void
mono_print_forest          (MonoFlowGraph *cfg, GPtrArray *forest);

void
mono_disassemble_code      (guint8 *code, int size, char *id);


#endif
