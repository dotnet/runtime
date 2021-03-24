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
mono_dump_metadata_offsets (void);

void
mono_metadata_cross_helpers_run (void);


static void
mono_dump_jit_offsets (void)
{
#ifdef USED_CROSS_COMPILER_OFFSETS
	g_print ("#error not using native offsets\n");
#else
	mono_dump_metadata_offsets ();

	g_print ("#ifndef DISABLE_JIT_OFFSETS\n");
	g_print ("#define USED_CROSS_COMPILER_OFFSETS\n");
#define DISABLE_METADATA_OFFSETS
#define DECL_OFFSET2(struct,field,offset) this_should_not_happen
#define DECL_ALIGN2(type,size) this_should_not_happen

#define DECL_OFFSET(struct,field) g_print ("DECL_OFFSET2(%s,%s,%d)\n", #struct, #field, (int)MONO_STRUCT_OFFSET (struct, field));
#define DECL_ALIGN(type)
#define DECL_SIZE2(type,size) this_should_not_happen
#define DECL_SIZE(type)
#include <mono/metadata/object-offsets.h>

	g_print ("#endif //disable jit check\n");
	g_print ("#endif //cross compiler checks\n");
	g_print ("#endif //gc check\n");
	g_print ("#endif //os check\n");
	g_print ("#endif //arch check\n");
	g_print ("#endif //USED_CROSS_COMPILER_OFFSETS check\n");
#endif
}

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

#ifndef USED_CROSS_COMPILER_OFFSETS
	if (g_hasenv ("DUMP_CROSS_OFFSETS"))
		mono_dump_jit_offsets ();
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
