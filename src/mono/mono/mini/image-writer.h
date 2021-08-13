/**
 * \file
 * Creation of object files or assembly files using the same interface.
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com);
 *   Zoltan Varga (vargaz@gmail.com);
 *   Paolo Molaro (lupus@ximian.com);
 *
 * (C); 2002 Ximian, Inc.
 */

#ifndef __MONO_IMAGE_WRITER_H__
#define __MONO_IMAGE_WRITER_H__

#include "config.h"

#include <glib.h>
#include <stdio.h>

#include <mono/utils/mono-compiler.h>

typedef struct _MonoImageWriter MonoImageWriter;

MonoImageWriter* mono_img_writer_create (FILE *fp);

void mono_img_writer_destroy (MonoImageWriter *w);

void mono_img_writer_emit_start (MonoImageWriter *w);

int mono_img_writer_emit_writeout (MonoImageWriter *w);

guint8* mono_img_writer_get_output (MonoImageWriter *acfg, guint32 *size);

void mono_img_writer_emit_section_change (MonoImageWriter *w, const char *section_name, int subsection_index);

void mono_img_writer_emit_push_section (MonoImageWriter *w, const char *section_name, int subsection);

void mono_img_writer_emit_pop_section (MonoImageWriter *w);

void mono_img_writer_set_section_addr (MonoImageWriter *acfg, guint64 addr);

void mono_img_writer_emit_global (MonoImageWriter *w, const char *name, gboolean func);

void mono_img_writer_emit_local_symbol (MonoImageWriter *w, const char *name, const char *end_label, gboolean func);

void mono_img_writer_emit_symbol_size (MonoImageWriter *w, const char *start, const char *end_label);

void mono_img_writer_emit_label (MonoImageWriter *w, const char *name);

void mono_img_writer_emit_bytes (MonoImageWriter *w, const guint8* buf, int size);

void mono_img_writer_emit_string (MonoImageWriter *w, const char *value);

void mono_img_writer_emit_line (MonoImageWriter *w);

void mono_img_writer_emit_alignment (MonoImageWriter *w, int size);

void mono_img_writer_emit_alignment_fill (MonoImageWriter *w, int size, int fill);

void mono_img_writer_emit_pointer_unaligned (MonoImageWriter *w, const char *target);

void mono_img_writer_emit_pointer (MonoImageWriter *w, const char *target);

void mono_img_writer_emit_int16 (MonoImageWriter *w, int value);

void mono_img_writer_emit_int32 (MonoImageWriter *w, int value);

void mono_img_writer_emit_symbol (MonoImageWriter *w, const char *symbol);

void mono_img_writer_emit_symbol_diff (MonoImageWriter *w, const char *end, const char* start, int offset);

void mono_img_writer_emit_zero_bytes (MonoImageWriter *w, int num);

void mono_img_writer_emit_global (MonoImageWriter *w, const char *name, gboolean func);

void mono_img_writer_emit_byte (MonoImageWriter *w, guint8 val);

void mono_img_writer_emit_reloc (MonoImageWriter *acfg, int reloc_type, const char *symbol, int addend);

void mono_img_writer_emit_unset_mode (MonoImageWriter *acfg);

gboolean mono_img_writer_subsections_supported (MonoImageWriter *acfg);

FILE * mono_img_writer_get_fp (MonoImageWriter *acfg);

const char *mono_img_writer_get_temp_label_prefix (MonoImageWriter *acfg);

#endif
