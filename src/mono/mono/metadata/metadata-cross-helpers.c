/**
 * \file
 */

#include <stdio.h>

#include "config.h"
#include <mono/metadata/abi-details.h>

#include <mono/metadata/class-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/handle.h>
#ifdef HAVE_SGEN_GC
#include <mono/sgen/sgen-gc.h>
#endif
#ifdef MONO_CLASS_DEF_PRIVATE
/* Rationale: MonoClass field offsets are computed here.  Need to see the definition.
 */
#define REALLY_INCLUDE_CLASS_DEF 1
#include <mono/metadata/class-private-definition.h>
#undef REALLY_INCLUDE_CLASS_DEF
#endif

#ifdef MONO_GENERATING_OFFSETS
/* The offsets tool uses this structure to compute basic type sizes/alignment */
struct basic_types_struct {
	gint8 gint8_f;
	gint16 gint16_f;
	gint32 gint32_f;
	gint64 gint64_f;
	float float_f;
	double double_f;
	gpointer gpointer_f;
};
typedef struct { gint8 i; } gint8_struct;
typedef struct { gint16 i; } gint16_struct;
typedef struct { gint32 i; } gint32_struct;
typedef struct { gint64 i; } gint64_struct;
typedef struct { float i; } float_struct;
typedef struct { double i; } double_struct;
typedef struct { gpointer i; } gpointer_struct;
#endif

void
mono_metadata_cross_helpers_run (void);

/*
 * mono_metadata_cross_helpers_run:
 *
 *   Check that the offsets given by object-offsets.h match the offsets
 * on the host. This only checks the metadata offsets.
 */
void
mono_metadata_cross_helpers_run (void)
{
#if defined (HAS_CROSS_COMPILER_OFFSETS) && !defined (MONO_CROSS_COMPILE)
	gboolean is_broken = FALSE;

#define DISABLE_JIT_OFFSETS
#define USE_CROSS_COMPILE_OFFSETS
#define DECL_OFFSET(struct,field) this_should_not_happen_for_cross_fields
#define DECL_OFFSET2(struct,field,offset) \
	 if ((int)G_STRUCT_OFFSET (struct, field) != offset) { \
		g_print (#struct ":" #field " invalid struct offset %d (expected %d)\n",	\
			offset,	\
			(int)G_STRUCT_OFFSET (struct, field));	\
		is_broken = TRUE;	\
	}
#define DECL_ALIGN(type) this_should_not_happen_for_cross_align
#define DECL_ALIGN2(name,size) \
	 if (mono_abi_alignment (MONO_ALIGN_ ## name) != size) { \
		g_print (#name ": invalid alignment %d (expected %d)\n",	\
		size,	\
		MONO_ALIGN_ ## name);	\
		is_broken = TRUE;	\
	}
#define DECL_SIZE(type) this_should_not_happen_for_cross_size
#define DECL_SIZE2(name,size) \
	 if (MONO_SIZEOF_ ## name != size) { \
		g_print (#name ": invalid size %d (expected %d)\n",	\
		size,	\
		MONO_SIZEOF_ ## name);	\
		is_broken = TRUE;	\
	}

#include <mono/metadata/object-offsets.h>

	g_assert (!is_broken);
#endif
}

