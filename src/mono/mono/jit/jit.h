#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include "regset.h"

void arch_emit_prologue (MonoMethod *method, int locals_size, MonoRegSet *rs);
void arch_emit_epilogue (MonoMethod *method, MonoRegSet *rs);

#endif
