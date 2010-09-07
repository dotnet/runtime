/*
 * image-writer.h: Creation of object files or assembly files using the same interface.
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

/* Relocation types */
#define R_ARM_CALL 28
#define R_ARM_JUMP24 29
#define R_ARM_ALU_PC_G0_NC 59

gboolean bin_writer_supported (void) MONO_INTERNAL;

MonoImageWriter* img_writer_create (FILE *fp, gboolean use_bin_writer) MONO_INTERNAL;

void img_writer_destroy (MonoImageWriter *w) MONO_INTERNAL;

void img_writer_emit_start (MonoImageWriter *w) MONO_INTERNAL;

int img_writer_emit_writeout (MonoImageWriter *w) MONO_INTERNAL;

guint8* img_writer_get_output (MonoImageWriter *acfg, guint32 *size) MONO_INTERNAL;

void img_writer_emit_section_change (MonoImageWriter *w, const char *section_name, int subsection_index) MONO_INTERNAL;

void img_writer_emit_push_section (MonoImageWriter *w, const char *section_name, int subsection) MONO_INTERNAL;

void img_writer_emit_pop_section (MonoImageWriter *w) MONO_INTERNAL;

void img_writer_set_section_addr (MonoImageWriter *acfg, guint64 addr) MONO_INTERNAL;

void img_writer_emit_global (MonoImageWriter *w, const char *name, gboolean func) MONO_INTERNAL;

void img_writer_emit_local_symbol (MonoImageWriter *w, const char *name, const char *end_label, gboolean func) MONO_INTERNAL;

void img_writer_emit_symbol_size (MonoImageWriter *w, const char *start, const char *end_label);

void img_writer_emit_label (MonoImageWriter *w, const char *name) MONO_INTERNAL;

void img_writer_emit_bytes (MonoImageWriter *w, const guint8* buf, int size) MONO_INTERNAL;

void img_writer_emit_string (MonoImageWriter *w, const char *value) MONO_INTERNAL;

void img_writer_emit_line (MonoImageWriter *w) MONO_INTERNAL;

void img_writer_emit_alignment (MonoImageWriter *w, int size) MONO_INTERNAL;

#ifdef __native_client_codegen__
void img_writer_emit_nacl_call_alignment (MonoImageWriter *w) MONO_INTERNAL;
#endif

void img_writer_emit_pointer_unaligned (MonoImageWriter *w, const char *target) MONO_INTERNAL;

void img_writer_emit_pointer (MonoImageWriter *w, const char *target) MONO_INTERNAL;

void img_writer_emit_int16 (MonoImageWriter *w, int value) MONO_INTERNAL;

void img_writer_emit_int32 (MonoImageWriter *w, int value) MONO_INTERNAL;

void img_writer_emit_symbol_diff (MonoImageWriter *w, const char *end, const char* start, int offset) MONO_INTERNAL;

void img_writer_emit_zero_bytes (MonoImageWriter *w, int num) MONO_INTERNAL;

void img_writer_emit_global (MonoImageWriter *w, const char *name, gboolean func) MONO_INTERNAL;

void img_writer_emit_byte (MonoImageWriter *w, guint8 val) MONO_INTERNAL;

void img_writer_emit_reloc (MonoImageWriter *acfg, int reloc_type, const char *symbol, int addend) MONO_INTERNAL;

void img_writer_emit_unset_mode (MonoImageWriter *acfg) MONO_INTERNAL;

gboolean img_writer_subsections_supported (MonoImageWriter *acfg) MONO_INTERNAL;

FILE * img_writer_get_fp (MonoImageWriter *acfg) MONO_INTERNAL;

const char *img_writer_get_temp_label_prefix (MonoImageWriter *acfg) MONO_INTERNAL;

#endif
