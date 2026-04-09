#include <config.h>

#include <mono/utils/mono-memory-model.h>
#include <mono/metadata/abi-details.h>


typedef struct {
	const char alignment [MONO_ALIGN_COUNT];
} AbiDetails;


#define DECLARE_ABI_DETAILS(I8, I16, I32, I64, F32, F64, PTR) \
const static AbiDetails mono_abi_details = {	\
	{ I8, I16, I32, I64, F32, F64, PTR }	\
};	\

#ifdef MONO_CROSS_COMPILE

#if TARGET_WASM

DECLARE_ABI_DETAILS (1, 2, 4, 8, 4, 8, 4)

#elif TARGET_S390X

DECLARE_ABI_DETAILS (1, 2, 4, 8, 4, 8, 8)

#else

#define DECL_OFFSET(struct,field)
#define DECL_OFFSET2(struct,field,offset)
#define DECL_ALIGN2(type,size) MONO_ALIGN_value_ ##type = size,
#define DECL_SIZE(type)
#define DECL_SIZE2(type,size)

enum {
#include "object-offsets.h"
};

DECLARE_ABI_DETAILS (
	MONO_ALIGN_value_gint8,
	MONO_ALIGN_value_gint16,
	MONO_ALIGN_value_gint32,
	MONO_ALIGN_value_gint64,
	MONO_ALIGN_value_float,
	MONO_ALIGN_value_double,
	MONO_ALIGN_value_gpointer)

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

DECLARE_ABI_DETAILS (
	MONO_CURRENT_ABI_ALIGNOF (gint8),
	MONO_CURRENT_ABI_ALIGNOF (gint16),
	MONO_CURRENT_ABI_ALIGNOF (gint32),
	MONO_CURRENT_ABI_ALIGNOF (gint64),
	MONO_CURRENT_ABI_ALIGNOF (float),
	MONO_CURRENT_ABI_ALIGNOF (double),
	MONO_CURRENT_ABI_ALIGNOF (gpointer))

#endif

int
mono_abi_alignment (CoreTypeAlign type)
{
	return mono_abi_details.alignment [type];
}
