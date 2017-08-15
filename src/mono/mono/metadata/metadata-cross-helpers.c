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

static int
dump_arch (void)
{
#if defined (TARGET_X86)
	g_print ("#ifdef TARGET_X86\n");
#elif defined (TARGET_AMD64)
	g_print ("#ifdef TARGET_AMD64\n");
#elif defined (TARGET_ARM)
	g_print ("#ifdef TARGET_ARM\n");
#elif defined (TARGET_ARM64)
	g_print ("#ifdef TARGET_ARM64\n");
#else
	return 0;
#endif
	return 1;
}

static int
dump_os (void)
{
#if defined (HOST_WIN32)
	g_print ("#ifdef TARGET_WIN32\n");
#elif defined (HOST_ANDROID)
	g_print ("#ifdef TARGET_ANDROID\n");
#elif defined (HOST_DARWIN)
	g_print ("#ifdef TARGET_OSX\n");
#elif defined (PLATFORM_IOS)
	g_print ("#ifdef TARGET_IOS\n");
#else
	return 0;
#endif
	return 1;
}

void
mono_dump_metadata_offsets (void);

void
mono_dump_metadata_offsets (void)
{
#ifdef USED_CROSS_COMPILER_OFFSETS
	g_print ("not using native offsets\n");
#else
	g_print ("#ifndef USED_CROSS_COMPILER_OFFSETS\n");

	if (!dump_arch ()) {
		g_print ("#error failed to figure out the current arch\n");
		return;
	}

	if (!dump_os ()) {
		g_print ("#error failed to figure out the current OS\n");
		return;
	}

#ifdef HAVE_SGEN_GC
	g_print ("#ifndef HAVE_BOEHM_GC\n");
#elif HAVE_BOEHM_GC
	g_print ("#ifndef HAVE_SGEN_GC\n");
#else
	g_print ("#error no gc conf not supported\n");
	return;
#endif

	g_print ("#define HAS_CROSS_COMPILER_OFFSETS\n");
	g_print ("#if defined (USE_CROSS_COMPILE_OFFSETS) || defined (MONO_CROSS_COMPILE)\n");
	g_print ("#if !defined (DISABLE_METADATA_OFFSETS)\n");
	g_print ("#define USED_CROSS_COMPILER_OFFSETS\n");

#define DISABLE_JIT_OFFSETS
#define DECL_OFFSET2(struct,field,offset) this_should_not_happen
#define DECL_ALIGN2(type,size) this_should_not_happen

#define DECL_OFFSET(struct,field) g_print ("DECL_OFFSET2(%s,%s,%d)\n", #struct, #field, (int)MONO_STRUCT_OFFSET (struct, field));
#define DECL_ALIGN(type) g_print ("DECL_ALIGN2(%s,%d)\n", #type, (int)MONO_ABI_ALIGNOF (type));
#define DECL_SIZE(type) g_print ("DECL_SIZE2(%s,%d)\n", #type, (int)MONO_ABI_SIZEOF (type));
#include <mono/metadata/object-offsets.h>

	g_print ("#endif //disable metadata check\n");
	g_print ("#endif //gc check\n");
#endif
}

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
	 if (MONO_ALIGN_ ## name != size) { \
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

