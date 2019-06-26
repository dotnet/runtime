/**
 * \file
 * Copyright 2002-2003 Ximian Inc
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_LLVMONLY_RUNTIME_H__
#define __MONO_LLVMONLY_RUNTIME_H__

#include "mini-runtime.h"

gpointer  mini_llvmonly_load_method         (MonoMethod *method, gboolean caller_gsharedvt, gboolean need_unbox, gpointer *out_arg, MonoError *error);
MonoFtnDesc* mini_llvmonly_load_method_ftndesc (MonoMethod *method, gboolean caller_gsharedvt, gboolean need_unbox, MonoError *error);
gpointer  mini_llvmonly_load_method_delegate (MonoMethod *method, gboolean caller_gsharedvt, gboolean need_unbox, gpointer *out_arg, MonoError *error);
gpointer  mini_llvmonly_get_delegate_arg     (MonoMethod *method, gpointer method_ptr);
gpointer  mini_llvmonly_add_method_wrappers (MonoMethod *m, gpointer compiled_method, gboolean caller_gsharedvt, gboolean add_unbox_tramp, gpointer *out_arg);
MonoFtnDesc *mini_llvmonly_create_ftndesc (MonoDomain *domain, gpointer addr, gpointer arg);
gpointer mini_llvmonly_get_imt_trampoline (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp);
gpointer mini_llvmonly_get_vtable_trampoline (MonoVTable *vt, int slot_index, int index);

G_EXTERN_C gpointer mini_llvmonly_init_vtable_slot (MonoVTable *vtable, int slot);
G_EXTERN_C gpointer mini_llvmonly_resolve_vcall_gsharedvt (MonoObject *this_obj, int imt_slot, MonoMethod *imt_method, gpointer *out_arg);
G_EXTERN_C gpointer mini_llvmonly_resolve_iface_call_gsharedvt (MonoObject *this_obj, int imt_slot, MonoMethod *imt_method, gpointer *out_arg);
G_EXTERN_C MonoFtnDesc* mini_llvmonly_resolve_generic_virtual_call (MonoVTable *vt, int slot, MonoMethod *imt_method);
G_EXTERN_C MonoFtnDesc* mini_llvmonly_resolve_generic_virtual_iface_call (MonoVTable *vt, int imt_slot, MonoMethod *imt_method);
G_EXTERN_C void mini_llvmonly_init_delegate (MonoDelegate *del);
G_EXTERN_C void mini_llvmonly_init_delegate_virtual (MonoDelegate *del, MonoObject *target, MonoMethod *method);

G_EXTERN_C void mini_llvm_init_method          (gpointer aot_module, guint32 method_index);
G_EXTERN_C void mini_llvm_init_gshared_method_this  (gpointer aot_module, guint32 method_index, MonoObject *this_ins);
G_EXTERN_C void mini_llvm_init_gshared_method_mrgctx  (gpointer aot_module, guint32 method_index, MonoMethodRuntimeGenericContext *rgctx);
G_EXTERN_C void mini_llvm_init_gshared_method_vtable  (gpointer aot_module, guint32 method_index, MonoVTable *vtable);

G_EXTERN_C void mini_llvmonly_throw_nullref_exception (void);

#endif
