#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

void arch_emit_prologue (MonoMethod *method, int locals_size);
void arch_emit_epilogue (MonoMethod *method);

#endif
