/*
 * emit-x86.c: Support functions for emitting x86 code
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/endian.h>
#include <mono/arch/x86/x86-codegen.h>

#include "jit.h"

void
arch_emit_prologue (MBCodeGenStatus *s)
{
	x86_push_reg (s->code, X86_EBP);
	x86_mov_reg_reg (s->code, X86_EBP, X86_ESP, 4);
	
	if (s->locals_size)
		x86_alu_reg_imm (s->code, X86_SUB, X86_ESP, s->locals_size);

	if (mono_regset_reg_used (s->rs, X86_EBX)) 
		x86_push_reg (s->code, X86_EBX);

	if (mono_regset_reg_used (s->rs, X86_EDI)) 
		x86_push_reg (s->code, X86_EDI);

	if (mono_regset_reg_used (s->rs, X86_ESI))
		x86_push_reg (s->code, X86_ESI);
}

void
arch_emit_epilogue (MBCodeGenStatus *s)
{
	if (mono_regset_reg_used (s->rs, X86_EDI))
		x86_pop_reg (s->code, X86_EDI);

	if (mono_regset_reg_used (s->rs, X86_ESI))
		x86_pop_reg (s->code, X86_ESI);

	if (mono_regset_reg_used (s->rs, X86_EBX))
		x86_pop_reg (s->code, X86_EBX);

	x86_leave (s->code);
	x86_ret (s->code);
}


