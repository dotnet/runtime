/*
 * dwarfwriter.h: Creation of DWARF debug information
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

#include <mono/metadata/debug-mono-symfile.h>

#include <glib.h>

typedef struct _MonoDwarfWriter MonoDwarfWriter;

MonoDwarfWriter* mono_dwarf_writer_create (MonoImageWriter *writer, FILE *il_file, gboolean appending) MONO_INTERNAL;

void mono_dwarf_writer_destroy (MonoDwarfWriter *w) MONO_INTERNAL;

void mono_dwarf_writer_emit_base_info (MonoDwarfWriter *w, GSList *base_unwind_program) MONO_INTERNAL;

void mono_dwarf_writer_close (MonoDwarfWriter *w) MONO_INTERNAL;

void mono_dwarf_writer_emit_trampoline (MonoDwarfWriter *w, const char *tramp_name, char *start_symbol, char *end_symbol, guint8 *code, guint32 code_size, GSList *unwind_info) MONO_INTERNAL;

void
mono_dwarf_writer_emit_method (MonoDwarfWriter *w, MonoCompile *cfg, MonoMethod *method, char *start_symbol, char *end_symbol, guint8 *code, guint32 code_size, MonoInst **args, MonoInst **locals, GSList *unwind_info, MonoDebugMethodJitInfo *debug_info) MONO_INTERNAL;

#endif
