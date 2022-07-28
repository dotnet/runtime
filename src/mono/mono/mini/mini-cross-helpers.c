/**
 * \file
 */

#include "config.h"

#include <stdio.h>

#include "config.h"

#include "mini.h"
#include "mini-runtime.h"
#include "interp/interp.h"
#include <mono/metadata/abi-details.h>

void
mono_metadata_cross_helpers_run (void);

/*
 * mono_cross_helpers_run:
 *
 *   Check that the offsets given by object-offsets.h match the offsets
 * on the host.
 */
void
mono_cross_helpers_run (void)
{
#if defined (HAS_CROSS_COMPILER_OFFSETS) && !defined (MONO_CROSS_COMPILE)
	gboolean is_broken = FALSE;
#endif

#if defined (HAS_CROSS_COMPILER_OFFSETS) && !defined (MONO_CROSS_COMPILE)
	mono_metadata_cross_helpers_run ();

	/* The metadata offsets are already checked above */
#define DISABLE_METADATA_OFFSETS
#define USE_CROSS_COMPILE_OFFSETS
#define DECL_OFFSET(struct,field) this_should_not_happen_for_cross_fields
#define DECL_OFFSET2(struct,field,offset) \
	 if ((int)G_STRUCT_OFFSET (struct, field) != offset) { \
		g_print (#struct ":" #field " invalid struct offset %d (expected %d)\n",	\
			offset,	\
			(int)G_STRUCT_OFFSET (struct, field));	\
		is_broken = TRUE;	\
	}
#define DECL_ALIGN(name,type) this_should_not_happen_for_cross_align
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
