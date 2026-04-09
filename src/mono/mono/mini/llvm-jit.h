/**
 * \file
 * LLVM backend
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2009 Novell, Inc.
 */

#ifndef __MONO_LLVM_JIT_H__
#define __MONO_LLVM_JIT_H__

#include <glib.h>

#include "llvm-c/Core.h"
#include "llvm-c/ExecutionEngine.h"

#include "mini.h"

#ifdef HAVE_UNWIND_H
#include <unwind.h>
#endif

/* These can't go into mini-<ARCH>.h since thats not included into llvm-jit.cpp */
#if defined(TARGET_OSX) || defined(__linux__)
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_X86)
#define MONO_ARCH_LLVM_JIT_SUPPORTED 1
#endif
#endif

G_BEGIN_DECLS

typedef void* MonoEERef;

void
mono_llvm_jit_init (void);

MonoEERef
mono_llvm_create_ee (LLVMExecutionEngineRef *ee);

void
mono_llvm_dispose_ee (MonoEERef *mono_ee);

void
mono_llvm_optimize_method (LLVMValueRef method);

gpointer
mono_llvm_compile_method (MonoEERef mono_ee, MonoCompile *cfg, LLVMValueRef method, int nvars, LLVMValueRef *callee_vars, gpointer *callee_addrs, gpointer *eh_frame);

void
mono_llvm_set_unhandled_exception_handler (void);

G_END_DECLS

#endif /* __MONO_LLVM_JIT_H__ */
