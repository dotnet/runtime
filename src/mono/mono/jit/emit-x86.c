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

#include "jit.h"

void
arch_emit_prologue (MonoMethod *method, int locals_size)
{
	printf ("\tpush    %%ebp\n"
		"\tmov     %%esp,%%ebp\n"
		"\tsub     $0x%x,%%esp\n"
		"\tpush    %%edi\n"
		"\tpush    %%esi\n",
		locals_size);
}

void
arch_emit_epilogue (MonoMethod *method)
{
	printf ("epilog:\n"
		"\tpop     %%esi\n"
		"\tpop     %%edi\n"
		"\tleave\n"
		"\tret\n");
}


