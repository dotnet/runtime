#include <config.h>

#include <mono/utils/mono-memory-model.h>
#include <mono/metadata/abi-details.h>


typedef struct {
	char alignment [MONO_ALIGN_COUNT];
} AbiDetails;

AbiDetails mono_abi_details;

#ifdef MONO_CROSS_COMPILE

#if TARGET_WASM

static void
init_abi_detail (void)
{
#define INIT_ALIGN(type, size) mono_abi_details.alignment [MONO_ALIGN_  ## type] = size
	INIT_ALIGN (gint8, 1);
	INIT_ALIGN (gint16, 2);
	INIT_ALIGN (gint32, 4);
	INIT_ALIGN (gint64, 8);
	INIT_ALIGN (float, 4);
	INIT_ALIGN (double, 8);
	INIT_ALIGN (gpointer, 4);
#undef INIT_ALIGN
}

#else

#define DECL_OFFSET(struct,field)
#define DECL_OFFSET2(struct,field,offset)
#define DECL_ALIGN2(type,size) MONO_ALIGN_value_ ##type = size,
#define DECL_ALIGN2(type,size)
#define DECL_SIZE(type)
#define DECL_SIZE2(type,size)

enum {
#include "object-offsets.h"
};

static void
init_abi_detail (void)
{
#define INIT_ALIGN(type) mono_abi_details.alignment [MONO_ALIGN_  ## type] = MONO_ALIGN_value_ ## type
	INIT_ALIGN (gint8);
	INIT_ALIGN (gint16);
	INIT_ALIGN (gint32);
	INIT_ALIGN (gint64);
	INIT_ALIGN (float);
	INIT_ALIGN (double);
	INIT_ALIGN (gpointer);
#undef INIT_ALIGN
}

#endif

#else

#define MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(type) typedef struct { char c; type x; } Mono_Align_Struct_ ##type;
#define MONO_CURRENT_ABI_ALIGNOF(type) ((int)G_STRUCT_OFFSET(Mono_Align_Struct_ ##type, x))

/* Needed by MONO_CURRENT_ABI_ALIGNOF */
MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(gint8)
MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(gint16)
MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(gint32)
MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(gint64)
MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(float)
MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(double)
MONO_CURRENT_ABI_ALIGNOF_TYPEDEF(gpointer)

static void
init_abi_detail (void)
{
#define INIT_ALIGN(type) mono_abi_details.alignment [MONO_ALIGN_  ## type] = MONO_CURRENT_ABI_ALIGNOF (type)
	INIT_ALIGN (gint8);
	INIT_ALIGN (gint16);
	INIT_ALIGN (gint32);
	INIT_ALIGN (gint64);
	INIT_ALIGN (float);
	INIT_ALIGN (double);
	INIT_ALIGN (gpointer);
#undef INIT_ALIGN
}

#endif

int
mono_abi_alignment (CoreTypeAlign type)
{
	static gboolean inited;
	/*
	 * OMG This is RACY! Like SUPER racy!
	 * That's true, *but* given the values are constant and we do safe publication, it's harmless.
	 */
	if (!inited) {
		init_abi_detail ();
		mono_memory_barrier ();
		inited = TRUE;
	}

	return mono_abi_details.alignment [type];
}
