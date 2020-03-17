/**
 * \file
 * Runtime support for llvm generated code
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2015 Xamarin, Inc.
 */

#ifndef __MONO_LLVM_RUNTIME_H__
#define __MONO_LLVM_RUNTIME_H__

#include <glib.h>

G_BEGIN_DECLS

typedef void (*MonoLLVMInvokeCallback) (void *arg);

void
mono_llvm_cpp_throw_exception (void);

void
mono_llvm_cpp_catch_exception (MonoLLVMInvokeCallback cb, gpointer arg, gboolean *out_thrown);

G_END_DECLS

#endif /* __MONO_LLVM_RUNTIME_H__ */
 


