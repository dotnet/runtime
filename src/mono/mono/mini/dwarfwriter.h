/**
 * \file
 * Creation of DWARF debug information
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2008-2009 Novell, Inc.
 */

#ifndef __MONO_DWARF_WRITER_H__
#define __MONO_DWARF_WRITER_H__

#include "config.h"
#include "image-writer.h"
#include "mini.h"

#include <mono/metadata/debug-internals.h>

#include <glib.h>

typedef struct _MonoDwarfWriter MonoDwarfWriter;

MonoDwarfWriter* mono_dwarf_writer_create (MonoImageWriter *writer, FILE *il_file, int il_file_start_line, gboolean emit_line_numbers);

void mono_dwarf_writer_destroy (MonoDwarfWriter *w);

void mono_dwarf_writer_emit_base_info (MonoDwarfWriter *w, const char *cu_name, GSList *base_unwind_program);

void mono_dwarf_writer_close (MonoDwarfWriter *w);

int mono_dwarf_writer_get_il_file_line_index (MonoDwarfWriter *w);

void mono_dwarf_writer_emit_trampoline (MonoDwarfWriter *w, const char *tramp_name, char *start_symbol, char *end_symbol, guint8 *code, guint32 code_size, GSList *unwind_info);

void
mono_dwarf_writer_emit_method (MonoDwarfWriter *w, MonoCompile *cfg, MonoMethod *method, char *start_symbol, char *end_symbol, char *linkage_name,
							   guint8 *code, guint32 code_size, MonoInst **args, MonoInst **locals, GSList *unwind_info, MonoDebugMethodJitInfo *debug_info);

char *
mono_dwarf_escape_path (const char *name);

#endif
