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
arch_emit_prologue (MonoMethod *method, int locals_size, MonoRegSet *rs)
{
	printf ("\tpush    %%ebp\n"
		"\tmov     %%esp,%%ebp\n");
	
	if (locals_size)
		printf ("\tsub     $0x%x,%%esp\n", locals_size);

	if (mono_regset_reg_used (rs, X86_EBX))
		printf ("\tpush    %%ebx\n");

	if (mono_regset_reg_used (rs, X86_EDI))
		printf ("\tpush    %%edi\n");

	if (mono_regset_reg_used (rs, X86_ESI))
		printf ("\tpush    %%esi\n");
}

void
arch_emit_epilogue (MonoMethod *method, MonoRegSet *rs)
{
	printf ("epilog:\n");

	if (mono_regset_reg_used (rs, X86_EDI))
		printf ("\tpop    %%esi\n");

	if (mono_regset_reg_used (rs, X86_ESI))
		printf ("\tpop    %%edi\n");

	if (mono_regset_reg_used (rs, X86_EBX))
		printf ("\tpop    %%ebx\n");

	printf ("\tleave\n"
		"\tret\n");
}


