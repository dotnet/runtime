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
arch_emit_prologue (guint8 **buf, MonoMethod *method, int locals_size, MonoRegSet *rs)
{
	x86_push_reg (*buf, X86_EBP);
	x86_mov_reg_reg (*buf, X86_EBP, X86_ESP, 4);
	
	if (locals_size)
		x86_alu_reg_imm (*buf, X86_SUB, X86_ESP, locals_size);

	if (mono_regset_reg_used (rs, X86_EBX)) 
		x86_push_reg (*buf, X86_EBX);

	if (mono_regset_reg_used (rs, X86_EDI)) 
		x86_push_reg (*buf, X86_EDI);

	if (mono_regset_reg_used (rs, X86_ESI))
		x86_push_reg (*buf, X86_ESI);
}

void
arch_emit_epilogue (guint8 **buf, MonoMethod *method, MonoRegSet *rs)
{
	//printf ("epilog:\n");

	if (mono_regset_reg_used (rs, X86_EDI))
		x86_pop_reg (*buf, X86_EDI);

	if (mono_regset_reg_used (rs, X86_ESI))
		x86_pop_reg (*buf, X86_ESI);

	if (mono_regset_reg_used (rs, X86_EBX))
		x86_pop_reg (*buf, X86_EBX);

	x86_leave (*buf);
	x86_ret (*buf);
}


