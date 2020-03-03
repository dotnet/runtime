/**
 * \file
 * Support for emitting gdb debug info for JITted code.
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2010 Novell, Inc.
 */

/*
 * This works as follows:
 * - the runtime writes out an xdb.s file containing DWARF debug info.
 * - the user calls a gdb macro
 * - the macro compiles and loads this shared library using add-symbol-file.
 *
 * This is based on the xdebug functionality in the Kaffe Java VM.
 * 
 * We emit assembly code instead of using the ELF writer, so we can emit debug info
 * incrementally as each method is JITted, and the debugger doesn't have to call
 * into the runtime to emit the shared library, which would cause all kinds of
 * complications, like threading issues, and the fact that the ELF writer's
 * emit_writeout () function cannot be called more than once.
 * GDB 7.0 and later has a JIT interface.
 */

#include "config.h"
#include <glib.h>
#include "mini.h"
#include "mini-runtime.h"

#include <sys/types.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_STDINT_H
#include <stdint.h>
#endif
#include <fcntl.h>
#include <ctype.h>
#include <string.h>
#ifndef HOST_WIN32
#include <sys/time.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#include <errno.h>
#include <sys/stat.h>

#include "image-writer.h"

#if !defined(DISABLE_AOT) && !defined(DISABLE_JIT) && USE_BIN_WRITER

#include "dwarfwriter.h"

#include "mono/utils/mono-compiler.h"

#define USE_GDB_JIT_INTERFACE

/* The recommended gdb macro is: */
/*
  define xdb
  shell rm -f xdb.so && as --64 -o xdb.o xdb.s && ld -shared -o xdb.so xdb.o
  add-symbol-file xdb.so 0
  end
*/

/*
 * GDB JIT interface definitions.
 *
 *	http://sources.redhat.com/gdb/onlinedocs/gdb_30.html
 */
typedef enum
{
  JIT_NOACTION = 0,
  JIT_REGISTER_FN,
  JIT_UNREGISTER_FN
} jit_actions_t;

struct jit_code_entry;
typedef struct jit_code_entry jit_code_entry;

struct jit_code_entry
{
	jit_code_entry *next_entry;
	jit_code_entry *prev_entry;
	const char *symfile_addr;
	/*
	 * The gdb code in gdb/jit.c which reads this structure ignores alignment
	 * requirements, so use two 32 bit fields.
	 */
	guint32 symfile_size1, symfile_size2;
};

typedef struct jit_descriptor
{
	guint32 version;
	/* This type should be jit_actions_t, but we use guint32
	   to be explicit about the bitwidth.  */
	guint32 action_flag;
	jit_code_entry *relevant_entry;
	jit_code_entry *first_entry;
} jit_descriptor;

G_BEGIN_DECLS

/* GDB puts a breakpoint in this function.  */
void MONO_NEVER_INLINE __jit_debug_register_code(void);

#if defined(ENABLE_LLVM) && !defined(MONO_CROSS_COMPILE)

/* LLVM already defines these */

extern jit_descriptor __jit_debug_descriptor;

#else

/* gcc seems to inline/eliminate calls to noinline functions, thus the asm () */
void MONO_NEVER_INLINE __jit_debug_register_code(void) {
#if defined(__GNUC__)
	asm ("");
#endif
}

/* Make sure to specify the version statically, because the
   debugger may check the version before we can set it.  */
jit_descriptor __jit_debug_descriptor = { 1, 0, 0, 0 };

#endif

G_END_DECLS

static MonoImageWriter *xdebug_w;
static MonoDwarfWriter *xdebug_writer;
static FILE *xdebug_fp, *il_file;
static gboolean use_gdb_interface, save_symfiles;
static int il_file_line_index;
static GHashTable *xdebug_syms;

void
mono_xdebug_init (const char *options)
{
	MonoImageWriter *w;
	char **args, **ptr;

	args = g_strsplit (options, ",", -1);
	for (ptr = args; ptr && *ptr; ptr ++) {
		char *arg = *ptr;

		if (!strcmp (arg, "gdb"))
			use_gdb_interface = TRUE;
		if (!strcmp (arg, "save-symfiles"))
			save_symfiles = TRUE;
	}

	/* This file will contain the IL code for methods which don't have debug info */
	il_file = fopen ("xdb.il", "w");
	if (il_file == NULL) {
		use_gdb_interface = FALSE;
		g_warning ("** Unable to create xdb.il. Managed symbol names won't be available.");
		return;
	}

	if (use_gdb_interface)
		return;

	unlink ("xdb.s");
	xdebug_fp = fopen ("xdb.s", "w");
	
	w = mono_img_writer_create (xdebug_fp, FALSE);

	mono_img_writer_emit_start (w);

	xdebug_writer = mono_dwarf_writer_create (w, il_file, 0, TRUE);

	/* Emit something so the file has a text segment */
	mono_img_writer_emit_section_change (w, ".text", 0);
	mono_img_writer_emit_string (w, "");

	mono_dwarf_writer_emit_base_info (xdebug_writer, "JITted code", mono_unwind_get_cie_program ());
}

static void
xdebug_begin_emit (MonoImageWriter **out_w, MonoDwarfWriter **out_dw)
{
	MonoImageWriter *w;
	MonoDwarfWriter *dw;

	w = mono_img_writer_create (NULL, TRUE);

	mono_img_writer_emit_start (w);

	/* This file will contain the IL code for methods which don't have debug info */
	if (!il_file)
		il_file = fopen ("xdb.il", "w");

	dw = mono_dwarf_writer_create (w, il_file, il_file_line_index, TRUE);

	mono_dwarf_writer_emit_base_info (dw, "JITted code", mono_unwind_get_cie_program ());

	*out_w = w;
	*out_dw = dw;
}

static void
xdebug_end_emit (MonoImageWriter *w, MonoDwarfWriter *dw, MonoMethod *method)
{
	guint8 *img;
	guint32 img_size;
	jit_code_entry *entry;
	guint64 *psize;

	il_file_line_index = mono_dwarf_writer_get_il_file_line_index (dw);
	mono_dwarf_writer_close (dw);

	mono_img_writer_emit_writeout (w);

	img = mono_img_writer_get_output (w, &img_size);

	mono_img_writer_destroy (w);

	if (FALSE) {
		/* Save the symbol files to help debugging */
		FILE *fp;
		char *file_name;
		static int file_counter;

		file_counter ++;
		file_name = g_strdup_printf ("xdb-%d.o", file_counter);
		printf ("%s %p %d\n", file_name, img, img_size);

		fp = fopen (file_name, "w");
		fwrite (img, img_size, 1, fp);
		fclose (fp);
		g_free (file_name);
	}

	/* Register the image with GDB */

	entry = g_malloc0 (sizeof (jit_code_entry));

	entry->symfile_addr = (const char*)img;
	psize = (guint64*)&entry->symfile_size1;
	*psize = img_size;

	entry->next_entry = __jit_debug_descriptor.first_entry;
	if (__jit_debug_descriptor.first_entry)
		__jit_debug_descriptor.first_entry->prev_entry = entry;
	__jit_debug_descriptor.first_entry = entry;
	
	__jit_debug_descriptor.relevant_entry = entry;
	__jit_debug_descriptor.action_flag = JIT_REGISTER_FN;

	__jit_debug_register_code ();
}

/*
 * mono_xdebug_flush:
 *
 *   This could be called from inside gdb to flush the debugging information not yet
 * registered with gdb.
 */
void
mono_xdebug_flush (void)
{
	if (xdebug_w)
		xdebug_end_emit (xdebug_w, xdebug_writer, NULL);

	xdebug_begin_emit (&xdebug_w, &xdebug_writer);
}

static int xdebug_method_count;

/*
 * mono_save_xdebug_info:
 *
 *   Emit debugging info for METHOD into an assembly file which can be assembled
 * and loaded into gdb to provide debugging info for JITted code.
 * LOCKING: Acquires the loader lock.
 */
void
mono_save_xdebug_info (MonoCompile *cfg)
{
	MonoDebugMethodJitInfo *dmji;

	if (use_gdb_interface) {
		mono_loader_lock ();

		if (!xdebug_syms)
			xdebug_syms = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);

		/*
		 * gdb is not designed to handle 1000s of symbol files (one per method). So we
		 * group them into groups of 100.
		 */
		if ((xdebug_method_count % 100) == 0)
			mono_xdebug_flush ();

		xdebug_method_count ++;

		dmji = mono_debug_find_method (jinfo_get_method (cfg->jit_info), mono_domain_get ());
		mono_dwarf_writer_emit_method (xdebug_writer, cfg, jinfo_get_method (cfg->jit_info), NULL, NULL, NULL,
									   (guint8*)cfg->jit_info->code_start, cfg->jit_info->code_size, cfg->args, cfg->locals, cfg->unwind_ops, dmji);
		mono_debug_free_method_jit_info (dmji);

#if 0
		/* 
		 * Emit a symbol for the code by emitting it at the beginning of the text 
		 * segment, and setting the text segment to have an absolute address.
		 * This symbol can be used to set breakpoints in gdb.
		 * FIXME: This doesn't work when multiple methods are emitted into the same file.
		 */
		sym = get_debug_sym (cfg->jit_info->method, "", xdebug_syms);
		mono_img_writer_emit_section_change (w, ".text", 0);
		if (!xdebug_text_addr) {
			xdebug_text_addr = cfg->jit_info->code_start;
			mono_img_writer_set_section_addr (w, (gssize)xdebug_text_addr);
		}
		mono_img_writer_emit_global_with_size (w, sym, cfg->jit_info->code_size, TRUE);
		mono_img_writer_emit_label (w, sym);
		mono_img_writer_emit_bytes (w, cfg->jit_info->code_start, cfg->jit_info->code_size);
		g_free (sym);
#endif
		
		mono_loader_unlock ();
	} else {
		if (!xdebug_writer)
			return;

		mono_loader_lock ();
		dmji = mono_debug_find_method (jinfo_get_method (cfg->jit_info), mono_domain_get ());
		mono_dwarf_writer_emit_method (xdebug_writer, cfg, jinfo_get_method (cfg->jit_info), NULL, NULL, NULL,
									   (guint8*)cfg->jit_info->code_start, cfg->jit_info->code_size, cfg->args, cfg->locals, cfg->unwind_ops, dmji);
		mono_debug_free_method_jit_info (dmji);
		fflush (xdebug_fp);
		mono_loader_unlock ();
	}

}

/*
 * mono_save_trampoline_xdebug_info:
 *
 *   Same as mono_save_xdebug_info, but for trampolines.
 * LOCKING: Acquires the loader lock.
 */
void
mono_save_trampoline_xdebug_info (MonoTrampInfo *info)
{
	const char *info_name = info->name;
	if (info_name == NULL)
		info_name = "";

	if (use_gdb_interface) {
		MonoImageWriter *w;
		MonoDwarfWriter *dw;

		/* This can be called before the loader lock is initialized */
		mono_loader_lock_if_inited ();

		xdebug_begin_emit (&w, &dw);

		mono_dwarf_writer_emit_trampoline (dw, info_name, NULL, NULL, info->code, info->code_size, info->unwind_ops);

		xdebug_end_emit (w, dw, NULL);
		
		mono_loader_unlock_if_inited ();
	} else {
		if (!xdebug_writer)
			return;

		mono_loader_lock_if_inited ();
		mono_dwarf_writer_emit_trampoline (xdebug_writer, info_name, NULL, NULL, info->code, info->code_size, info->unwind_ops);
		fflush (xdebug_fp);
		mono_loader_unlock_if_inited ();
	}
}

#else /* !defined(DISABLE_AOT) && !defined(DISABLE_JIT) */

void
mono_xdebug_init (const char *options)
{
}

void
mono_save_xdebug_info (MonoCompile *cfg)
{
}

void
mono_save_trampoline_xdebug_info (MonoTrampInfo *info)
{
}

#endif
