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

void
mono_llvm_cpp_throw_exception (void);

G_END_DECLS

#endif /* __MONO_LLVM_RUNTIME_H__ */
 


