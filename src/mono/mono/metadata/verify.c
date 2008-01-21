
#include <mono/metadata/object-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/tokentype.h>
#include <string.h>
#include <signal.h>
#include <ctype.h>


/*
 * Pull the list of opcodes
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

#ifdef MONO_VERIFIER_DEBUG
#define VERIFIER_DEBUG(code) do { code } while (0)
#else
#define VERIFIER_DEBUG(code)
#endif

//////////////////////////////////////////////////////////////////
#define IS_STRICT_MODE(ctx) (((ctx)->level & MONO_VERIFY_NON_STRICT) == 0)
#define IS_FAIL_FAST_MODE(ctx) (((ctx)->level & MONO_VERIFY_FAIL_FAST) == MONO_VERIFY_FAIL_FAST)

#define ADD_VERIFY_INFO(__ctx, __msg, __status)	\
	do {	\
		MonoVerifyInfo *vinfo = g_new (MonoVerifyInfo, 1);	\
		vinfo->status = __status;	\
		vinfo->message = ( __msg );	\
		(__ctx)->list = g_slist_prepend ((__ctx)->list, vinfo);	\
	} while (0)

#define ADD_VERIFY_ERROR(__ctx, __msg)	\
	do {	\
		ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_ERROR); \
		(__ctx)->valid = 0; \
	} while (0)

#define CODE_NOT_VERIFIABLE(__ctx, __msg) \
	do {	\
		if ((__ctx)->verifiable) { \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_NOT_VERIFIABLE); \
			(__ctx)->verifiable = 0; \
			if (IS_FAIL_FAST_MODE (__ctx)) \
				(__ctx)->valid = 0; \
		} \
	} while (0)

/*Flags to be used with ILCodeDesc::flags */
enum {
	/*Instruction has not been processed.*/
	IL_CODE_FLAG_NOT_PROCESSED  = 0,
	/*Instruction was decoded by mono_method_verify loop.*/
	IL_CODE_FLAG_SEEN = 1,
	/*Instruction was target of a branch or is at a protected block boundary.*/
	IL_CODE_FLAG_WAS_TARGET = 2,
	/*Used by stack_init to avoid double initialize each entry.*/
	IL_CODE_FLAG_STACK_INITED = 4,
	/*Used by merge_stacks to decide if it should just copy the eval stack.*/
	IL_CODE_STACK_MERGED = 8,
	/*This instruction is part of the delegate construction sequence, it cannot be target of a branch.*/
	IL_CODE_DELEGATE_SEQUENCE = 0x10,
	/*This is a delegate created from a ldftn to a non final virtual method*/
	IL_CODE_LDFTN_DELEGATE_NONFINAL_VIRTUAL = 0x20,
};

typedef struct {
	MonoType *type;
	int stype;
	MonoMethod *method;
} ILStackDesc;


typedef struct {
	ILStackDesc *stack;
	guint16 size;
	guint16 flags;
} ILCodeDesc;

typedef struct {
	int max_args;
	int max_stack;
	int verifiable;
	int valid;
	int level;

	int code_size;
	ILCodeDesc *code;
	ILCodeDesc eval;

	MonoType **params;
	GSList *list;
	/*Allocated fnptr MonoType that should be freed by us.*/
	GSList *funptrs;

	int num_locals;
	MonoType **locals;

	/*TODO get rid of target here, need_merge in mono_method_verify and hoist the merging code in the branching code*/
	int target;

	guint32 ip_offset;
	MonoMethodSignature *signature;
	MonoMethodHeader *header;

	MonoGenericContext *generic_context;
	MonoImage *image;
	MonoMethod *method;

	/*This flag helps solving a corner case of delegate verification in that you cannot have a "starg 0" 
	 *on a method that creates a delegate for a non-final virtual method using ldftn*/
	gboolean has_this_store;
} VerifyContext;

static void
merge_stacks (VerifyContext *ctx, ILCodeDesc *from, ILCodeDesc *to, int start, gboolean external);

//////////////////////////////////////////////////////////////////



enum {
	TYPE_INV = 0, /* leave at 0. */
	TYPE_I4  = 1,
	TYPE_I8  = 2,
	TYPE_NATIVE_INT = 3,
	TYPE_R8  = 4,
	/* Used by operator tables to resolve pointer types (managed & unmanaged) and by unmanaged pointer types*/
	TYPE_PTR  = 5,
	/* value types and classes */
	TYPE_COMPLEX = 6,
	/* Number of types, used to define the size of the tables*/
	TYPE_MAX = 6,

	/* Used by tables to signal that a result is not verifiable*/
	NON_VERIFIABLE_RESULT = 0x80,

	/*Mask used to extract just the type, excluding flags */
	TYPE_MASK = 0x0F,

	/* The stack type is a managed pointer, unmask the value to res */
	POINTER_MASK = 0x100,
	
	/*Stack type with the pointer mask*/
	RAW_TYPE_MASK = 0x10F,

	/* Controlled Mutability Manager Pointer */
	CMMP_MASK = 0x200,

	/* The stack type is a null literal*/
	NULL_LITERAL_MASK = 0x400,
	
	/**Used by ldarg.0 and family to let delegate verification happens.*/
	THIS_POINTER_MASK = 0x800,

	/**Signals that this is a boxed value type*/
	BOXED_MASK = 0x1000,

};

static const char* const
type_names [TYPE_MAX + 1] = {
	"Invalid",
	"Int32",
	"Int64",
	"Native Int",
	"Float64",
	"Native Pointer",
	"Complex"	
};

enum {
	PREFIX_UNALIGNED = 1,
	PREFIX_VOLATILE  = 2,
	PREFIX_TAIL      = 4,
	PREFIX_ADDR_MASK = 3,
	PREFIX_FUNC_MASK = 4
};
//////////////////////////////////////////////////////////////////


/*Token validation macros and functions */
#define IS_MEMBER_REF(token) (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF)
#define IS_METHOD_DEF(token) (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
#define IS_METHOD_SPEC(token) (mono_metadata_token_table (token) == MONO_TABLE_METHODSPEC)

#define IS_TYPE_REF(token) (mono_metadata_token_table (token) == MONO_TABLE_TYPEREF)
#define IS_TYPE_DEF(token) (mono_metadata_token_table (token) == MONO_TABLE_TYPEDEF)
#define IS_TYPE_SPEC(token) (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC)
#define IS_METHOD_DEF_OR_REF_OR_SPEC(token) (IS_METHOD_DEF (token) || IS_MEMBER_REF (token) || IS_METHOD_SPEC (token))
#define IS_TYPE_DEF_OR_REF_OR_SPEC(token) (IS_TYPE_DEF (token) || IS_TYPE_REF (token) || IS_TYPE_SPEC (token))

/*
 * Verify if @token refers to a valid row on int's table.
 */
static gboolean
token_bounds_check (MonoImage *image, guint32 token)
{
	return image->tables [mono_metadata_token_table (token)].rows >= mono_metadata_token_index (token);
}

static MonoType *
mono_type_create_fnptr_from_mono_method (VerifyContext *ctx, MonoMethod *method)
{
	MonoType *res = g_new0 (MonoType, 1);
	res->data.method = mono_method_signature (method);
	res->type = MONO_TYPE_FNPTR;
	ctx->funptrs = g_slist_prepend (ctx->funptrs, res);
	return res;
}

#define CTOR_REQUIRED_FLAGS (METHOD_ATTRIBUTE_SPECIAL_NAME | METHOD_ATTRIBUTE_RT_SPECIAL_NAME)
#define CTOR_INVALID_FLAGS (METHOD_ATTRIBUTE_STATIC)

static gboolean
mono_method_is_constructor (MonoMethod *method) 
{
	return ((method->flags & CTOR_REQUIRED_FLAGS) == CTOR_REQUIRED_FLAGS &&
			!(method->flags & CTOR_INVALID_FLAGS) &&
			!strcmp (".ctor", method->name));
}


/* stack_slot_get_type:
 * 
 * Returns the stack type of @value. This value includes POINTER_MASK.
 * 
 * Use this function to checks that account for a managed pointer.
 */
static gint32
stack_slot_get_type (ILStackDesc *value)
{
	return value->stype & RAW_TYPE_MASK;
}

/* stack_slot_get_underlying_type:
 * 
 * Returns the stack type of @value. This value does not include POINTER_MASK.
 * 
 * Use this function is cases where the fact that the value could be a managed pointer is
 * irrelevant. For example, field load doesn't care about this fact of type on stack.
 */
static gint32
stack_slot_get_underlying_type (ILStackDesc *value)
{
	return value->stype & TYPE_MASK;
}

/* stack_slot_is_managed_pointer:
 * 
 * Returns TRUE is @value is a managed pointer.
 */
static gboolean
stack_slot_is_managed_pointer (ILStackDesc *value)
{
	return (value->stype & POINTER_MASK) == POINTER_MASK;
}

/* stack_slot_is_managed_mutability_pointer:
 * 
 * Returns TRUE is @value is a managed mutability pointer.
 */
static gboolean
stack_slot_is_managed_mutability_pointer (ILStackDesc *value)
{
	return (value->stype & CMMP_MASK) == CMMP_MASK;
}

/* stack_slot_is_null_literal:
 * 
 * Returns TRUE is @value is the null literal.
 */
static gboolean
stack_slot_is_null_literal (ILStackDesc *value)
{
	return (value->stype & NULL_LITERAL_MASK) == NULL_LITERAL_MASK;
}


/* stack_slot_is_this_pointer:
 * 
 * Returns TRUE is @value is the this literal
 */
static gboolean
stack_slot_is_this_pointer (ILStackDesc *value)
{
	return (value->stype & THIS_POINTER_MASK) == THIS_POINTER_MASK;
}

/* stack_slot_is_boxed_value:
 * 
 * Returns TRUE is @value is a boxed value
 */
static gboolean
stack_slot_is_boxed_value (ILStackDesc *value)
{
	return (value->stype & BOXED_MASK) == BOXED_MASK;
}

static const char *
stack_slot_get_name (ILStackDesc *value)
{
	return type_names [value->stype & TYPE_MASK];
}
//////////////////////////////////////////////////////////////////
void
mono_free_verify_list (GSList *list)
{
	MonoVerifyInfo *info;
	GSList *tmp;

	for (tmp = list; tmp; tmp = tmp->next) {
		info = tmp->data;
		g_free (info->message);
		g_free (info);
	}
	g_slist_free (list);
}

#define ADD_ERROR(list,msg)	\
	do {	\
		MonoVerifyInfo *vinfo = g_new (MonoVerifyInfo, 1);	\
		vinfo->status = MONO_VERIFY_ERROR;	\
		vinfo->message = (msg);	\
		(list) = g_slist_prepend ((list), vinfo);	\
	} while (0)

#define ADD_WARN(list,code,msg)	\
	do {	\
		MonoVerifyInfo *vinfo = g_new (MonoVerifyInfo, 1);	\
		vinfo->status = (code);	\
		vinfo->message = (msg);	\
		(list) = g_slist_prepend ((list), vinfo);	\
	} while (0)

static const char
valid_cultures[][9] = {
	"ar-SA", "ar-IQ", "ar-EG", "ar-LY",
	"ar-DZ", "ar-MA", "ar-TN", "ar-OM",
	"ar-YE", "ar-SY", "ar-JO", "ar-LB",
	"ar-KW", "ar-AE", "ar-BH", "ar-QA",
	"bg-BG", "ca-ES", "zh-TW", "zh-CN",
	"zh-HK", "zh-SG", "zh-MO", "cs-CZ",
	"da-DK", "de-DE", "de-CH", "de-AT",
	"de-LU", "de-LI", "el-GR", "en-US",
	"en-GB", "en-AU", "en-CA", "en-NZ",
	"en-IE", "en-ZA", "en-JM", "en-CB",
	"en-BZ", "en-TT", "en-ZW", "en-PH",
	"es-ES-Ts", "es-MX", "es-ES-Is", "es-GT",
	"es-CR", "es-PA", "es-DO", "es-VE",
	"es-CO", "es-PE", "es-AR", "es-EC",
	"es-CL", "es-UY", "es-PY", "es-BO",
	"es-SV", "es-HN", "es-NI", "es-PR",
	"Fi-FI", "fr-FR", "fr-BE", "fr-CA",
	"Fr-CH", "fr-LU", "fr-MC", "he-IL",
	"hu-HU", "is-IS", "it-IT", "it-CH",
	"Ja-JP", "ko-KR", "nl-NL", "nl-BE",
	"nb-NO", "nn-NO", "pl-PL", "pt-BR",
	"pt-PT", "ro-RO", "ru-RU", "hr-HR",
	"Lt-sr-SP", "Cy-sr-SP", "sk-SK", "sq-AL",
	"sv-SE", "sv-FI", "th-TH", "tr-TR",
	"ur-PK", "id-ID", "uk-UA", "be-BY",
	"sl-SI", "et-EE", "lv-LV", "lt-LT",
	"fa-IR", "vi-VN", "hy-AM", "Lt-az-AZ",
	"Cy-az-AZ",
	"eu-ES", "mk-MK", "af-ZA",
	"ka-GE", "fo-FO", "hi-IN", "ms-MY",
	"ms-BN", "kk-KZ", "ky-KZ", "sw-KE",
	"Lt-uz-UZ", "Cy-uz-UZ", "tt-TA", "pa-IN",
	"gu-IN", "ta-IN", "te-IN", "kn-IN",
	"mr-IN", "sa-IN", "mn-MN", "gl-ES",
	"kok-IN", "syr-SY", "div-MV"
};

static int
is_valid_culture (const char *cname)
{
	int i;
	int found;

	found = *cname == 0;
	for (i = 0; i < G_N_ELEMENTS (valid_cultures); ++i) {
		if (g_strcasecmp (valid_cultures [i], cname)) {
			found = 1;
			break;
		}
	}
	return found;
}

static int
is_valid_assembly_flags (guint32 flags) {
	/* Metadata: 22.1.2 */
	flags &= ~(0x8000 | 0x4000); /* ignore reserved bits 0x0030? */
	return ((flags == 1) || (flags == 0));
}

static int
is_valid_blob (MonoImage *image, guint32 blob_index, int notnull)
{
	guint32 size;
	const char *p, *blob_end;

	if (blob_index >= image->heap_blob.size)
		return 0;
	p = mono_metadata_blob_heap (image, blob_index);
	size = mono_metadata_decode_blob_size (p, &blob_end);
	if (blob_index + size + (blob_end-p) > image->heap_blob.size)
		return 0;
	if (notnull && !size)
		return 0;
	return 1;
}

static const char*
is_valid_string (MonoImage *image, guint32 str_index, int notnull)
{
	const char *p, *blob_end, *res;

	if (str_index >= image->heap_strings.size)
		return NULL;
	res = p = mono_metadata_string_heap (image, str_index);
	blob_end = mono_metadata_string_heap (image, image->heap_strings.size - 1);
	if (notnull && !*p)
		return 0;
	/* 
	 * FIXME: should check it's a valid utf8 string, too.
	 */
	while (p <= blob_end) {
		if (!*p)
			return res;
		++p;
	}
	return *p? NULL: res;
}

static int
is_valid_cls_ident (const char *p)
{
	/*
	 * FIXME: we need the full unicode glib support for this.
	 * Check: http://www.unicode.org/unicode/reports/tr15/Identifier.java
	 * We do the lame thing for now.
	 */
	if (!isalpha (*p))
		return 0;
	++p;
	while (*p) {
		if (!isalnum (*p) && *p != '_')
			return 0;
		++p;
	}
	return 1;
}

static int
is_valid_filename (const char *p)
{
	if (!*p)
		return 0;
	return strpbrk (p, "\\//:")? 0: 1;
}

static GSList*
verify_assembly_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLY];
	guint32 cols [MONO_ASSEMBLY_SIZE];
	const char *p;

	if (level & MONO_VERIFY_ERROR) {
		if (t->rows > 1)
			ADD_ERROR (list, g_strdup ("Assembly table may only have 0 or 1 rows"));
		mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);

		switch (cols [MONO_ASSEMBLY_HASH_ALG]) {
		case ASSEMBLY_HASH_NONE:
		case ASSEMBLY_HASH_MD5:
		case ASSEMBLY_HASH_SHA1:
			break;
		default:
			ADD_ERROR (list, g_strdup_printf ("Hash algorithm 0x%x unknown", cols [MONO_ASSEMBLY_HASH_ALG]));
		}

		if (!is_valid_assembly_flags (cols [MONO_ASSEMBLY_FLAGS]))
			ADD_ERROR (list, g_strdup_printf ("Invalid flags in assembly: 0x%x", cols [MONO_ASSEMBLY_FLAGS]));

		if (!is_valid_blob (image, cols [MONO_ASSEMBLY_PUBLIC_KEY], FALSE))
			ADD_ERROR (list, g_strdup ("Assembly public key is an invalid index"));

		if (!(p = is_valid_string (image, cols [MONO_ASSEMBLY_NAME], TRUE))) {
			ADD_ERROR (list, g_strdup ("Assembly name is invalid"));
		} else {
			if (strpbrk (p, ":\\/."))
				ADD_ERROR (list, g_strdup_printf ("Assembly name `%s' contains invalid chars", p));
		}

		if (!(p = is_valid_string (image, cols [MONO_ASSEMBLY_CULTURE], FALSE))) {
			ADD_ERROR (list, g_strdup ("Assembly culture is an invalid index"));
		} else {
			if (!is_valid_culture (p))
				ADD_ERROR (list, g_strdup_printf ("Assembly culture `%s' is invalid", p));
		}
	}
	return list;
}

static GSList*
verify_assemblyref_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLYREF];
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *p;
	int i;

	if (level & MONO_VERIFY_ERROR) {
		for (i = 0; i < t->rows; ++i) {
			mono_metadata_decode_row (t, i, cols, MONO_ASSEMBLYREF_SIZE);
			if (!is_valid_assembly_flags (cols [MONO_ASSEMBLYREF_FLAGS]))
				ADD_ERROR (list, g_strdup_printf ("Invalid flags in assemblyref row %d: 0x%x", i + 1, cols [MONO_ASSEMBLY_FLAGS]));

			if (!is_valid_blob (image, cols [MONO_ASSEMBLYREF_PUBLIC_KEY], FALSE))
				ADD_ERROR (list, g_strdup_printf ("AssemblyRef public key in row %d is an invalid index", i + 1));

			if (!(p = is_valid_string (image, cols [MONO_ASSEMBLYREF_CULTURE], FALSE))) {
				ADD_ERROR (list, g_strdup_printf ("AssemblyRef culture in row %d is invalid", i + 1));
			} else {
				if (!is_valid_culture (p))
					ADD_ERROR (list, g_strdup_printf ("AssemblyRef culture `%s' in row %d is invalid", p, i + 1));
			}

			if (cols [MONO_ASSEMBLYREF_HASH_VALUE] && !is_valid_blob (image, cols [MONO_ASSEMBLYREF_HASH_VALUE], TRUE))
				ADD_ERROR (list, g_strdup_printf ("AssemblyRef hash value in row %d is invalid or not null and empty", i + 1));
		}
	}
	if (level & MONO_VERIFY_WARNING) {
		/* check for duplicated rows */
		for (i = 0; i < t->rows; ++i) {
		}
	}
	return list;
}

static GSList*
verify_class_layout_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_CLASSLAYOUT];
	MonoTableInfo *tdef = &image->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_CLASS_LAYOUT_SIZE];
	guint32 value, i;

	if (level & MONO_VERIFY_ERROR) {
		for (i = 0; i < t->rows; ++i) {
			mono_metadata_decode_row (t, i, cols, MONO_CLASS_LAYOUT_SIZE);

			if (cols [MONO_CLASS_LAYOUT_PARENT] > tdef->rows || !cols [MONO_CLASS_LAYOUT_PARENT]) {
				ADD_ERROR (list, g_strdup_printf ("Parent in class layout is invalid in row %d", i + 1));
			} else {
				value = mono_metadata_decode_row_col (tdef, cols [MONO_CLASS_LAYOUT_PARENT] - 1, MONO_TYPEDEF_FLAGS);
				if (value & TYPE_ATTRIBUTE_INTERFACE)
					ADD_ERROR (list, g_strdup_printf ("Parent in class layout row %d is an interface", i + 1));
				if (value & TYPE_ATTRIBUTE_AUTO_LAYOUT)
					ADD_ERROR (list, g_strdup_printf ("Parent in class layout row %d is AutoLayout", i + 1));
				if (value & TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT) {
					switch (cols [MONO_CLASS_LAYOUT_PACKING_SIZE]) {
					case 0: case 1: case 2: case 4: case 8: case 16:
					case 32: case 64: case 128: break;
					default:
						ADD_ERROR (list, g_strdup_printf ("Packing size %d in class layout row %d is invalid", cols [MONO_CLASS_LAYOUT_PACKING_SIZE], i + 1));
					}
				} else if (value & TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
					/*
					 * FIXME: LAMESPEC: it claims it must be 0 (it's 1, instead).
					if (cols [MONO_CLASS_LAYOUT_PACKING_SIZE])
						ADD_ERROR (list, g_strdup_printf ("Packing size %d in class layout row %d is invalid with explicit layout", cols [MONO_CLASS_LAYOUT_PACKING_SIZE], i + 1));
					*/
				}
				/*
				 * FIXME: we need to check that if class size != 0, 
				 * it needs to be greater than the class calculated size.
				 * If parent is a valuetype it also needs to be smaller than
				 * 1 MByte (0x100000 bytes).
				 * To do both these checks we need to load the referenced 
				 * assemblies, though (the spec claims we didn't have to, bah).
				 */
				/* 
				 * We need to check that the parent types have the same layout 
				 * type as well.
				 */
			}
		}
	}

	return list;
}

static GSList*
verify_constant_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_CONSTANT];
	guint32 cols [MONO_CONSTANT_SIZE];
	guint32 value, i;
	GHashTable *dups = g_hash_table_new (NULL, NULL);

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_CONSTANT_SIZE);

		if (level & MONO_VERIFY_ERROR)
			if (g_hash_table_lookup (dups, GUINT_TO_POINTER (cols [MONO_CONSTANT_PARENT])))
				ADD_ERROR (list, g_strdup_printf ("Parent 0x%08x is duplicated in Constant row %d", cols [MONO_CONSTANT_PARENT], i + 1));
		g_hash_table_insert (dups, GUINT_TO_POINTER (cols [MONO_CONSTANT_PARENT]),
				GUINT_TO_POINTER (cols [MONO_CONSTANT_PARENT]));

		switch (cols [MONO_CONSTANT_TYPE]) {
		case MONO_TYPE_U1: /* LAMESPEC: it says I1...*/
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
			if (level & MONO_VERIFY_CLS)
				ADD_WARN (list, MONO_VERIFY_CLS, g_strdup_printf ("Type 0x%x not CLS compliant in Constant row %d", cols [MONO_CONSTANT_TYPE], i + 1));
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:
			break;
		default:
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Type 0x%x is invalid in Constant row %d", cols [MONO_CONSTANT_TYPE], i + 1));
		}
		if (level & MONO_VERIFY_ERROR) {
			value = cols [MONO_CONSTANT_PARENT] >> MONO_HASCONSTANT_BITS;
			switch (cols [MONO_CONSTANT_PARENT] & MONO_HASCONSTANT_MASK) {
			case MONO_HASCONSTANT_FIEDDEF:
				if (value > image->tables [MONO_TABLE_FIELD].rows)
					ADD_ERROR (list, g_strdup_printf ("Parent (field) is invalid in Constant row %d", i + 1));
				break;
			case MONO_HASCONSTANT_PARAM:
				if (value > image->tables [MONO_TABLE_PARAM].rows)
					ADD_ERROR (list, g_strdup_printf ("Parent (param) is invalid in Constant row %d", i + 1));
				break;
			case MONO_HASCONSTANT_PROPERTY:
				if (value > image->tables [MONO_TABLE_PROPERTY].rows)
					ADD_ERROR (list, g_strdup_printf ("Parent (property) is invalid in Constant row %d", i + 1));
				break;
			default:
				ADD_ERROR (list, g_strdup_printf ("Parent is invalid in Constant row %d", i + 1));
				break;
			}
		}
		if (level & MONO_VERIFY_CLS) {
			/* 
			 * FIXME: verify types is consistent with the enum type
			 * is parent is an enum.
			 */
		}
	}
	g_hash_table_destroy (dups);
	return list;
}

static GSList*
verify_event_map_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_EVENTMAP];
	guint32 cols [MONO_EVENT_MAP_SIZE];
	guint32 i, last_event;
	GHashTable *dups = g_hash_table_new (NULL, NULL);

	last_event = 0;

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_EVENT_MAP_SIZE);
		if (level & MONO_VERIFY_ERROR)
			if (g_hash_table_lookup (dups, GUINT_TO_POINTER (cols [MONO_EVENT_MAP_PARENT])))
				ADD_ERROR (list, g_strdup_printf ("Parent 0x%08x is duplicated in Event Map row %d", cols [MONO_EVENT_MAP_PARENT], i + 1));
		g_hash_table_insert (dups, GUINT_TO_POINTER (cols [MONO_EVENT_MAP_PARENT]),
				GUINT_TO_POINTER (cols [MONO_EVENT_MAP_PARENT]));
		if (level & MONO_VERIFY_ERROR) {
			if (cols [MONO_EVENT_MAP_PARENT] > image->tables [MONO_TABLE_TYPEDEF].rows)
				ADD_ERROR (list, g_strdup_printf ("Parent 0x%08x is invalid in Event Map row %d", cols [MONO_EVENT_MAP_PARENT], i + 1));
			if (cols [MONO_EVENT_MAP_EVENTLIST] > image->tables [MONO_TABLE_EVENT].rows)
				ADD_ERROR (list, g_strdup_printf ("EventList 0x%08x is invalid in Event Map row %d", cols [MONO_EVENT_MAP_EVENTLIST], i + 1));

			if (cols [MONO_EVENT_MAP_EVENTLIST] <= last_event)
				ADD_ERROR (list, g_strdup_printf ("EventList overlap in Event Map row %d", i + 1));
			last_event = cols [MONO_EVENT_MAP_EVENTLIST];
		}
	}

	g_hash_table_destroy (dups);
	return list;
}

static GSList*
verify_event_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_EVENT];
	guint32 cols [MONO_EVENT_SIZE];
	const char *p;
	guint32 value, i;

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_EVENT_SIZE);

		if (cols [MONO_EVENT_FLAGS] & ~(EVENT_SPECIALNAME|EVENT_RTSPECIALNAME)) {
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Flags 0x%04x invalid in Event row %d", cols [MONO_EVENT_FLAGS], i + 1));
		}
		if (!(p = is_valid_string (image, cols [MONO_EVENT_NAME], TRUE))) {
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Invalid name in Event row %d", i + 1));
		} else {
			if (level & MONO_VERIFY_CLS) {
				if (!is_valid_cls_ident (p))
					ADD_WARN (list, MONO_VERIFY_CLS, g_strdup_printf ("Invalid CLS name '%s` in Event row %d", p, i + 1));
			}
		}

		if (level & MONO_VERIFY_ERROR && cols [MONO_EVENT_TYPE]) {
			value = cols [MONO_EVENT_TYPE] >> MONO_TYPEDEFORREF_BITS;
			switch (cols [MONO_EVENT_TYPE] & MONO_TYPEDEFORREF_MASK) {
			case MONO_TYPEDEFORREF_TYPEDEF:
				if (!value || value > image->tables [MONO_TABLE_TYPEDEF].rows)
					ADD_ERROR (list, g_strdup_printf ("Type invalid in Event row %d", i + 1));
				break;
			case MONO_TYPEDEFORREF_TYPEREF:
				if (!value || value > image->tables [MONO_TABLE_TYPEREF].rows)
					ADD_ERROR (list, g_strdup_printf ("Type invalid in Event row %d", i + 1));
				break;
			case MONO_TYPEDEFORREF_TYPESPEC:
				if (!value || value > image->tables [MONO_TABLE_TYPESPEC].rows)
					ADD_ERROR (list, g_strdup_printf ("Type invalid in Event row %d", i + 1));
				break;
			default:
				ADD_ERROR (list, g_strdup_printf ("Type invalid in Event row %d", i + 1));
			}
		}
		/*
		 * FIXME: check that there is 1 add and remove row in methodsemantics
		 * and 0 or 1 raise and 0 or more other (maybe it's better to check for 
		 * these while checking methodsemantics).
		 * check for duplicated names for the same type [ERROR]
		 * check for CLS duplicate names for the same type [CLS]
		 */
	}
	return list;
}

static GSList*
verify_field_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_FIELD];
	guint32 cols [MONO_FIELD_SIZE];
	const char *p;
	guint32 i, flags;

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_FIELD_SIZE);
		/*
		 * Check this field has only one owner and that the owner is not 
		 * an interface (done in verify_typedef_table() )
		 */
		flags = cols [MONO_FIELD_FLAGS];
		switch (flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) {
		case FIELD_ATTRIBUTE_COMPILER_CONTROLLED:
		case FIELD_ATTRIBUTE_PRIVATE:
		case FIELD_ATTRIBUTE_FAM_AND_ASSEM:
		case FIELD_ATTRIBUTE_ASSEMBLY:
		case FIELD_ATTRIBUTE_FAMILY:
		case FIELD_ATTRIBUTE_FAM_OR_ASSEM:
		case FIELD_ATTRIBUTE_PUBLIC:
			break;
		default:
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Invalid access mask in Field row %d", i + 1));
			break;
		}
		if (level & MONO_VERIFY_ERROR) {
			if ((flags & FIELD_ATTRIBUTE_LITERAL) && (flags & FIELD_ATTRIBUTE_INIT_ONLY))
				ADD_ERROR (list, g_strdup_printf ("Literal and InitOnly cannot be both set in Field row %d", i + 1));
			if ((flags & FIELD_ATTRIBUTE_LITERAL) && !(flags & FIELD_ATTRIBUTE_STATIC))
				ADD_ERROR (list, g_strdup_printf ("Literal needs also Static set in Field row %d", i + 1));
			if ((flags & FIELD_ATTRIBUTE_RT_SPECIAL_NAME) && !(flags & FIELD_ATTRIBUTE_SPECIAL_NAME))
				ADD_ERROR (list, g_strdup_printf ("RTSpecialName needs also SpecialName set in Field row %d", i + 1));
			/*
			 * FIXME: check there is only one owner in the respective table.
			 * if (flags & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL)
			 * if (flags & FIELD_ATTRIBUTE_HAS_DEFAULT)
			 * if (flags & FIELD_ATTRIBUTE_HAS_FIELD_RVA)
			 */
		}
		if (!(p = is_valid_string (image, cols [MONO_FIELD_NAME], TRUE))) {
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Invalid name in Field row %d", i + 1));
		} else {
			if (level & MONO_VERIFY_CLS) {
				if (!is_valid_cls_ident (p))
					ADD_WARN (list, MONO_VERIFY_CLS, g_strdup_printf ("Invalid CLS name '%s` in Field row %d", p, i + 1));
			}
		}
		/*
		 * check signature.
		 * if owner is module needs to be static, access mask needs to be compilercontrolled,
		 * public or private (not allowed in cls mode).
		 * if owner is an enum ...
		 */


	}
	return list;
}

static GSList*
verify_file_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_FILE];
	guint32 cols [MONO_FILE_SIZE];
	const char *p;
	guint32 i;
	GHashTable *dups = g_hash_table_new (g_str_hash, g_str_equal);

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_FILE_SIZE);
		if (level & MONO_VERIFY_ERROR) {
			if (cols [MONO_FILE_FLAGS] != FILE_CONTAINS_METADATA && cols [MONO_FILE_FLAGS] != FILE_CONTAINS_NO_METADATA)
				ADD_ERROR (list, g_strdup_printf ("Invalid flags in File row %d", i + 1));
			if (!is_valid_blob (image, cols [MONO_FILE_HASH_VALUE], TRUE))
				ADD_ERROR (list, g_strdup_printf ("File hash value in row %d is invalid or not null and empty", i + 1));
		}
		if (!(p = is_valid_string (image, cols [MONO_FILE_NAME], TRUE))) {
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Invalid name in File row %d", i + 1));
		} else {
			if (level & MONO_VERIFY_ERROR) {
				if (!is_valid_filename (p))
					ADD_ERROR (list, g_strdup_printf ("Invalid name '%s` in File row %d", p, i + 1));
				else if (g_hash_table_lookup (dups, p)) {
					ADD_ERROR (list, g_strdup_printf ("Duplicate name '%s` in File row %d", p, i + 1));
				}
				g_hash_table_insert (dups, (gpointer)p, (gpointer)p);
			}
		}
		/*
		 * FIXME: I don't understand what this means:
		 * If this module contains a row in the Assembly table (that is, if this module "holds the manifest") 
		 * then there shall not be any row in the File table for this module - i.e., no self-reference  [ERROR]
		 */

	}
	if (level & MONO_VERIFY_WARNING) {
		if (!t->rows && image->tables [MONO_TABLE_EXPORTEDTYPE].rows)
			ADD_WARN (list, MONO_VERIFY_WARNING, g_strdup ("ExportedType table should be empty if File table is empty"));
	}
	g_hash_table_destroy (dups);
	return list;
}

static GSList*
verify_moduleref_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_MODULEREF];
	MonoTableInfo *tfile = &image->tables [MONO_TABLE_FILE];
	guint32 cols [MONO_MODULEREF_SIZE];
	const char *p, *pf;
	guint32 found, i, j, value;
	GHashTable *dups = g_hash_table_new (g_str_hash, g_str_equal);

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_MODULEREF_SIZE);
		if (!(p = is_valid_string (image, cols [MONO_MODULEREF_NAME], TRUE))) {
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Invalid name in ModuleRef row %d", i + 1));
		} else {
			if (level & MONO_VERIFY_ERROR) {
				if (!is_valid_filename (p))
					ADD_ERROR (list, g_strdup_printf ("Invalid name '%s` in ModuleRef row %d", p, i + 1));
				else if (g_hash_table_lookup (dups, p)) {
					ADD_WARN (list, MONO_VERIFY_WARNING, g_strdup_printf ("Duplicate name '%s` in ModuleRef row %d", p, i + 1));
					g_hash_table_insert (dups, (gpointer)p, (gpointer)p);
					found = 0;
					for (j = 0; j < tfile->rows; ++j) {
						value = mono_metadata_decode_row_col (tfile, j, MONO_FILE_NAME);
						if ((pf = is_valid_string (image, value, TRUE)))
							if (strcmp (p, pf) == 0) {
								found = 1;
								break;
							}
					}
					if (!found)
						ADD_ERROR (list, g_strdup_printf ("Name '%s` in ModuleRef row %d doesn't have a match in File table", p, i + 1));
				}
			}
		}
	}
	g_hash_table_destroy (dups);
	return list;
}

static GSList*
verify_standalonesig_table (MonoImage *image, GSList *list, int level)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_STANDALONESIG];
	guint32 cols [MONO_STAND_ALONE_SIGNATURE_SIZE];
	const char *p;
	guint32 i;

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_STAND_ALONE_SIGNATURE_SIZE);
		if (level & MONO_VERIFY_ERROR) {
			if (!is_valid_blob (image, cols [MONO_STAND_ALONE_SIGNATURE], TRUE)) {
				ADD_ERROR (list, g_strdup_printf ("Signature is invalid in StandAloneSig row %d", i + 1));
			} else {
				p = mono_metadata_blob_heap (image, cols [MONO_STAND_ALONE_SIGNATURE]);
				/* FIXME: check it's a valid locals or method sig.*/
			}
		}
	}
	return list;
}

GSList*
mono_image_verify_tables (MonoImage *image, int level)
{
	GSList *error_list = NULL;

	error_list = verify_assembly_table (image, error_list, level);
	/* 
	 * AssemblyOS, AssemblyProcessor, AssemblyRefOs and
	 * AssemblyRefProcessor should be ignored, 
	 * though we may want to emit a warning, since it should not 
	 * be present in a PE file.
	 */
	error_list = verify_assemblyref_table (image, error_list, level);
	error_list = verify_class_layout_table (image, error_list, level);
	error_list = verify_constant_table (image, error_list, level);
	/*
	 * cutom attribute, declsecurity 
	 */
	error_list = verify_event_map_table (image, error_list, level);
	error_list = verify_event_table (image, error_list, level);
	error_list = verify_field_table (image, error_list, level);
	error_list = verify_file_table (image, error_list, level);
	error_list = verify_moduleref_table (image, error_list, level);
	error_list = verify_standalonesig_table (image, error_list, level);

	return g_slist_reverse (error_list);
}

#define ADD_INVALID(list,msg)	\
	do {	\
		MonoVerifyInfo *vinfo = g_new (MonoVerifyInfo, 1);	\
		vinfo->status = MONO_VERIFY_ERROR;	\
		vinfo->message = (msg);	\
		(list) = g_slist_prepend ((list), vinfo);	\
		/*G_BREAKPOINT ();*/	\
		goto invalid_cil;	\
	} while (0)

#define CHECK_STACK_UNDERFLOW(num)	\
	do {	\
		if (cur_stack < (num))	\
			ADD_INVALID (list, g_strdup_printf ("Stack underflow at 0x%04x (%d items instead of %d)", ip_offset, cur_stack, (num)));	\
	} while (0)

#define CHECK_STACK_OVERFLOW()	\
	do {	\
		if (cur_stack >= max_stack)	\
			ADD_INVALID (list, g_strdup_printf ("Maxstack exceeded at 0x%04x", ip_offset));	\
	} while (0)


static int
in_any_block (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, offset))
			return 1;
		if (MONO_OFFSET_IN_HANDLER (clause, offset))
			return 1;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return 1;
	}
	return 0;
}

/*
 * in_any_exception_block:
 * 
 * Returns TRUE is @offset is part of any exception clause (filter, handler, catch, finally or fault).
 */
static gboolean
in_any_exception_block (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_HANDLER (clause, offset))
			return TRUE;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return TRUE;
	}
	return FALSE;
}

/*
 * is_valid_branch_instruction:
 *
 * Verify if it's valid to perform a branch from @offset to @target.
 * This should be used with br and brtrue/false.
 * It returns 0 if valid, 1 for unverifiable and 2 for invalid.
 * The major diferent from other similiar functions is that branching into a
 * finally/fault block is invalid instead of just unverifiable.  
 */
static int
is_valid_branch_instruction (MonoMethodHeader *header, guint offset, guint target)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		/*branching into a finally block is invalid*/
		if ((clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY || clause->flags == MONO_EXCEPTION_CLAUSE_FAULT) &&
			!MONO_OFFSET_IN_HANDLER (clause, offset) &&
			MONO_OFFSET_IN_HANDLER (clause, target))
			return 2;

		if (clause->try_offset != target && (MONO_OFFSET_IN_CLAUSE (clause, offset) ^ MONO_OFFSET_IN_CLAUSE (clause, target)))
			return 1;
		if (MONO_OFFSET_IN_HANDLER (clause, offset) ^ MONO_OFFSET_IN_HANDLER (clause, target))
			return 1;
		if (MONO_OFFSET_IN_FILTER (clause, offset) ^ MONO_OFFSET_IN_FILTER (clause, target))
			return 1;
	}
	return 0;
}

/*
 * is_valid_cmp_branch_instruction:
 * 
 * Verify if it's valid to perform a branch from @offset to @target.
 * This should be used with binary comparison branching instruction, like beq, bge and similars.
 * It returns 0 if valid, 1 for unverifiable and 2 for invalid.
 * 
 * The major diferences from other similar functions are that most errors lead to invalid
 * code and only branching out of finally, filter or fault clauses is unverifiable. 
 */
static int
is_valid_cmp_branch_instruction (MonoMethodHeader *header, guint offset, guint target)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		/*branching out of a handler or finally*/
		if (clause->flags != MONO_EXCEPTION_CLAUSE_NONE &&
			MONO_OFFSET_IN_HANDLER (clause, offset) &&
			!MONO_OFFSET_IN_HANDLER (clause, target))
			return 1;

		if (clause->try_offset != target && (MONO_OFFSET_IN_CLAUSE (clause, offset) ^ MONO_OFFSET_IN_CLAUSE (clause, target)))
			return 2;
		if (MONO_OFFSET_IN_HANDLER (clause, offset) ^ MONO_OFFSET_IN_HANDLER (clause, target))
			return 2;
		if (MONO_OFFSET_IN_FILTER (clause, offset) ^ MONO_OFFSET_IN_FILTER (clause, target))
			return 2;
	}
	return 0;
}

/*
 * A leave can't escape a finally block 
 */
static int
is_correct_leave (MonoMethodHeader *header, guint offset, guint target)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY && MONO_OFFSET_IN_HANDLER (clause, offset) && !MONO_OFFSET_IN_HANDLER (clause, target))
			return 0;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return 0;
	}
	return 1;
}

/*
 * A rethrow can't happen outside of a catch handler.
 */
static int
is_correct_rethrow (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_HANDLER (clause, offset))
			return 1;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return 1;
	}
	return 0;
}

/*
 * An endfinally can't happen outside of a finally/fault handler.
 */
static int
is_correct_endfinally (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_HANDLER (clause, offset) && (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT || clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY))
			return 1;
	}
	return 0;
}


/*
 * An endfilter can only happens inside a filter clause.
 * In non-strict mode filter is allowed inside the handler clause too
 */
static MonoExceptionClause *
is_correct_endfilter (VerifyContext *ctx, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < ctx->header->num_clauses; ++i) {
		clause = &ctx->header->clauses [i];
		if (clause->flags != MONO_EXCEPTION_CLAUSE_FILTER)
			continue;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return clause;
		if (!IS_STRICT_MODE (ctx) && MONO_OFFSET_IN_HANDLER (clause, offset))
			return clause;
	}
	return NULL;
}


/*
 * Non-strict endfilter can happens inside a try block or any handler block
 */
static int
is_unverifiable_endfilter (VerifyContext *ctx, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < ctx->header->num_clauses; ++i) {
		clause = &ctx->header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, offset))
			return 1;
	}
	return 0;
}

static gboolean
is_valid_bool_arg (ILStackDesc *arg)
{
	if (stack_slot_is_managed_pointer (arg))
		return TRUE;
	switch (stack_slot_get_underlying_type (arg)) {
	case TYPE_I4:
	case TYPE_I8:
	case TYPE_NATIVE_INT:
	case TYPE_PTR:
		return TRUE;
	case TYPE_COMPLEX:
		g_assert (arg->type);
		switch (arg->type->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_PTR:
			return TRUE;
		case MONO_TYPE_GENERICINST:
			/*We need to check if the container class
			 * of the generic type is a valuetype, iow:
			 * is it a "class Foo<T>" or a "struct Foo<T>"?
			 */
			return !arg->type->data.generic_class->container_class->valuetype;
		}
	default:
		return FALSE;
	}
}


/*Type manipulation helper*/

/*Returns the byref version of the supplied MonoType*/
static MonoType*
mono_type_get_type_byref (MonoType *type)
{
	if (type->byref)
		return type;
	return &mono_class_from_mono_type (type)->this_arg;
}


/*Returns the byval version of the supplied MonoType*/
static MonoType*
mono_type_get_type_byval (MonoType *type)
{
	if (!type->byref)
		return type;
	return &mono_class_from_mono_type (type)->byval_arg;
}

static MonoType*
mono_type_from_stack_slot (ILStackDesc *slot)
{
	if (stack_slot_is_managed_pointer (slot))
		return mono_type_get_type_byref (slot->type);
	return slot->type;
}

/*Stack manipulation code*/

static void
stack_init (VerifyContext *ctx, ILCodeDesc *state) 
{
	if (state->flags & IL_CODE_FLAG_STACK_INITED)
		return;
	state->size = 0;
	state->flags |= IL_CODE_FLAG_STACK_INITED;
	if (!state->stack)
		state->stack = g_new0 (ILStackDesc, ctx->max_stack);
}

static void
stack_copy (ILCodeDesc *to, ILCodeDesc *from)
{
	to->size = from->size;
	memcpy (to->stack, from->stack, sizeof (ILStackDesc) * from->size);
}

static void
copy_stack_value (ILStackDesc *to, ILStackDesc *from)
{
	to->stype = from->stype;
	to->type = from->type;
}

static int
check_underflow (VerifyContext *ctx, int size)
{
	if (ctx->eval.size < size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Stack underflow, required %d, but have %d at 0x%04x", size, ctx->eval.size, ctx->ip_offset));
		return 0;
	}
	return 1;
}

static int
check_overflow (VerifyContext *ctx)
{
	if (ctx->eval.size >= ctx->max_stack) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have stack-depth %d at 0x%04x", ctx->eval.size + 1, ctx->ip_offset));
		return 0;
	}
	return 1;
}

static gboolean
check_unmanaged_pointer (VerifyContext *ctx, ILStackDesc *value)
{
	if (stack_slot_get_type (value) == TYPE_PTR) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Unmanaged pointer is not a verifiable type at 0x%04x", ctx->ip_offset));
		return 0;
	}
	return 1;
}

static gboolean
check_unverifiable_type (VerifyContext *ctx, MonoType *type)
{
	if (type->type == MONO_TYPE_PTR || type->type == MONO_TYPE_FNPTR) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Unmanaged pointer is not a verifiable type at 0x%04x", ctx->ip_offset));
		return 0;
	}
	return 1;
}


static ILStackDesc *
stack_push (VerifyContext *ctx)
{
	return & ctx->eval.stack [ctx->eval.size++];
}

static ILStackDesc *
stack_push_val (VerifyContext *ctx, int stype, MonoType *type)
{
	ILStackDesc *top = stack_push (ctx);
	top->stype = stype;
	top->type = type;
	return top;
}

static ILStackDesc *
stack_pop (VerifyContext *ctx)
{
	return ctx->eval.stack + --ctx->eval.size;
}

static inline ILStackDesc *
stack_top (VerifyContext *ctx)
{
	return ctx->eval.stack + (ctx->eval.size - 1);
}

static inline ILStackDesc *
stack_get (VerifyContext *ctx, int distance)
{
	return ctx->eval.stack + (ctx->eval.size - distance - 1);
}

/* Returns the MonoType associated with the token, or NULL if it is invalid.
 * 
 * A boxable type can be either a reference or value type, but cannot be a byref type or an unmanaged pointer   
 * */
static MonoType*
get_boxable_mono_type (VerifyContext* ctx, int token)
{
	MonoType *type;
	if (!IS_TYPE_DEF_OR_REF_OR_SPEC (token) || !token_bounds_check (ctx->image, token)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid type token %x at 0x%04x", token, ctx->ip_offset));
		return NULL;
	}
	
	type = mono_type_get_full (ctx->image, token, ctx->generic_context);

	if (!type) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Type (0x%08x) not found at 0x%04x", token, ctx->ip_offset));
		return NULL;
	}

	if (type->byref && type->type != MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of byref type at 0x%04x", ctx->ip_offset));
		return NULL;
	}

	if (type->type == MONO_TYPE_VOID) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of void type at 0x%04x", ctx->ip_offset));
		return NULL;
	}

	if (type->type == MONO_TYPE_TYPEDBYREF)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid use of typedbyref at 0x%04x", ctx->ip_offset));

	check_unverifiable_type (ctx, type);
	return type;
}


/*operation result tables */

static const unsigned char bin_op_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_R8, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char add_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_R8, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char sub_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_R8, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_NATIVE_INT | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char int_bin_op_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char shift_op_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_I8, TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char cmp_br_op [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char cmp_br_eq_op [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_I4 | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_I4 | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_I4, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4},
};

static const unsigned char add_ovf_un_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char sub_ovf_un_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_NATIVE_INT | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char bin_ovf_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};


/*debug helpers */
static void
dump_stack_value (ILStackDesc *value)
{
	printf ("[(%x)(%x)", value->type->type, value->stype);

	if (stack_slot_is_this_pointer (value))
		printf ("[this] ");

	if (stack_slot_is_boxed_value (value))
		printf ("[boxed] ");

	if (stack_slot_is_null_literal (value))
		printf ("[null] ");

	if (stack_slot_is_managed_mutability_pointer (value))
		printf ("Controled Mutability MP: ");

	if (stack_slot_is_managed_pointer (value))
		printf ("Managed Pointer to: ");

	switch (stack_slot_get_underlying_type (value)) {
		case TYPE_INV:
			printf ("invalid type]"); 
			return;
		case TYPE_I4:
			printf ("int32]"); 
			return;
		case TYPE_I8:
			printf ("int64]"); 
			return;
		case TYPE_NATIVE_INT:
			printf ("native int]"); 
			return;
		case TYPE_R8:
			printf ("float64]"); 
			return;
		case TYPE_PTR:
			printf ("unmanaged pointer]"); 
			return;
		case TYPE_COMPLEX:
			switch (value->type->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
				printf ("complex] (%s)", value->type->data.klass->name);
				return;
			case MONO_TYPE_STRING:
				printf ("complex] (string)");
				return;
			case MONO_TYPE_OBJECT:
				printf ("complex] (object)");
				return;
			case MONO_TYPE_SZARRAY:
				printf ("complex] (%s [])", value->type->data.klass->name);
				return;
			case MONO_TYPE_ARRAY:
				printf ("complex] (%s [%d %d %d])",
					value->type->data.array->eklass->name,
					value->type->data.array->rank,
					value->type->data.array->numsizes,
					value->type->data.array->numlobounds);
				return;
			case MONO_TYPE_GENERICINST:
				printf ("complex] (inst of %s )", value->type->data.generic_class->container_class->name);
				return;
			case MONO_TYPE_VAR:
				printf ("complex] (type generic param !%d - %s) ", value->type->data.generic_param->num, value->type->data.generic_param->name);
				return;
			case MONO_TYPE_MVAR:
				printf ("complex] (method generic param !!%d - %s) ", value->type->data.generic_param->num, value->type->data.generic_param->name);
				return;
			default: {
				//should be a boxed value 
				char * name = mono_type_full_name (value->type);
				printf ("complex] %s", name);
				g_free (name);
				return;
				}
			}
		default:
			printf ("unknown stack %x type]\n", value->stype);
			g_assert_not_reached ();
	}
}

static void
dump_stack_state (ILCodeDesc *state) 
{
	int i;

	printf ("(%d) ", state->size);
	for (i = 0; i < state->size; ++i)
		dump_stack_value (state->stack + i);
	printf ("\n");
}

/*Returns TRUE if candidate array type can be assigned to target.
 *Both parameters MUST be of type MONO_TYPE_ARRAY (target->type == MONO_TYPE_ARRAY)
 */
static gboolean
is_array_type_compatible (MonoType *target, MonoType *candidate)
{
	int i;
	MonoArrayType *left = target->data.array;
	MonoArrayType *right = candidate->data.array;

	g_assert (target->type == MONO_TYPE_ARRAY);
	g_assert (candidate->type == MONO_TYPE_ARRAY);


	if ((left->rank != right->rank) ||
			(left->numsizes != right->numsizes) ||
			(left->numlobounds != right->numlobounds))
		return FALSE;

	for (i = 0; i < left->numsizes; ++i) 
		if (left->sizes [i] != right->sizes [i])
			return FALSE;

	for (i = 0; i < left->numlobounds; ++i) 
		if (left->lobounds [i] != right->lobounds [i])
			return FALSE;

	return mono_class_is_assignable_from (left->eklass, right->eklass);
}

static int
get_stack_type (MonoType *type)
{
	int mask = 0;
	int type_kind = type->type;
	if (type->byref)
		mask = POINTER_MASK;
	/*TODO handle CMMP_MASK */

handle_enum:
	switch (type_kind) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return TYPE_I4 | mask;

	case MONO_TYPE_I:
	case MONO_TYPE_U:
		return TYPE_NATIVE_INT | mask;

	/* FIXME: the spec says that you cannot have a pointer to method pointer, do we need to check this here? */ 
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_TYPEDBYREF:
		return TYPE_PTR | mask;

	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:

	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_GENERICINST:
		return TYPE_COMPLEX | mask;

	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return TYPE_I8 | mask;

	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return TYPE_R8 | mask;

	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			type_kind = type->type;
			goto handle_enum;
		} else 
			return TYPE_COMPLEX | mask;

	default:
		VERIFIER_DEBUG ( printf ("unknown type %02x in eval stack type\n", type->type); );
		g_assert_not_reached ();
		return 0;
	}
}

/* convert MonoType to ILStackDesc format (stype) */
static gboolean
set_stack_value (VerifyContext *ctx, ILStackDesc *stack, MonoType *type, int take_addr)
{
	int mask = 0;
	int type_kind = type->type;

	if (type->byref || take_addr)
		mask = POINTER_MASK;
	/* TODO handle CMMP_MASK */

handle_enum:
	stack->type = type;

	switch (type_kind) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		stack->stype = TYPE_I4 | mask;
		break;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		stack->stype = TYPE_NATIVE_INT | mask;
		break;

	/*FIXME: Do we need to check if it's a pointer to the method pointer? The spec says it' illegal to have that.*/
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_TYPEDBYREF:
		stack->stype = TYPE_PTR | mask;
		break;

	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:

	case MONO_TYPE_GENERICINST:
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR: 
		stack->stype = TYPE_COMPLEX | mask;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		stack->stype = TYPE_I8 | mask;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		stack->stype = TYPE_R8 | mask;
		break;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			type_kind = type->type;
			goto handle_enum;
		} else {
			stack->stype = TYPE_COMPLEX | mask;
			break;
		}
	default:
		VERIFIER_DEBUG ( printf ("unknown type 0x%02x in eval stack type\n", type->type); );
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Illegal value set on stack 0x%02x at %d", type->type, ctx->ip_offset));
		return FALSE;
	}
	return TRUE;
}

/* 
 * init_stack_with_value_at_exception_boundary:
 * 
 * Initialize the stack and push a given type.
 * The instruction is marked as been on the exception boundary.
 */
static void
init_stack_with_value_at_exception_boundary (VerifyContext *ctx, ILCodeDesc *code, MonoClass *klass)
{
	stack_init (ctx, code);
	set_stack_value (ctx, code->stack, &klass->byval_arg, FALSE);
	code->size = 1;
	code->flags |= IL_CODE_FLAG_WAS_TARGET;
}

/* Generics validation stuff, should be moved to another metadata/? file */
static gboolean
mono_is_generic_type_compatible (MonoType *target, MonoType *candidate)
{
	if (target->byref != candidate->byref)
		return FALSE;

handle_enum:
	switch (target->type) {
	case MONO_TYPE_STRING:
		if (candidate->type == MONO_TYPE_STRING)
			return TRUE;
		return FALSE;

	case MONO_TYPE_CLASS:
		if (candidate->type != MONO_TYPE_CLASS)
			return FALSE;

		VERIFIER_DEBUG ( printf ("verifying type class %p %p\n", target->data.klass, candidate->data.klass); );
		return mono_class_is_assignable_from (target->data.klass, candidate->data.klass);

	case MONO_TYPE_OBJECT:
		return MONO_TYPE_IS_REFERENCE (candidate);

	case MONO_TYPE_SZARRAY:
		if (candidate->type != MONO_TYPE_SZARRAY)
			return FALSE;
		return mono_class_is_assignable_from (target->data.klass, candidate->data.klass);

	case MONO_TYPE_VALUETYPE:
		if (target->data.klass->enumtype) {
			target = target->data.klass->enum_basetype;
			goto handle_enum;
		} else {
			if (candidate->type != MONO_TYPE_VALUETYPE)
				return FALSE;
			return candidate->data.klass == target->data.klass;
		}

	case MONO_TYPE_ARRAY:
		if (candidate->type != MONO_TYPE_ARRAY)
			return FALSE;
		return is_array_type_compatible (target, candidate);

	default:
		VERIFIER_DEBUG ( printf ("unknown target type %d\n", target->type); );
		g_assert_not_reached ();
	}

	return FALSE;
}


static gboolean
mono_is_generic_instance_compatible (MonoGenericClass *target, MonoGenericClass *candidate, MonoGenericClass *root_candidate) {
	MonoGenericContainer *container;
	int i;

	VERIFIER_DEBUG ( printf ("candidate container %p\n", candidate->container_class->generic_container); );
	if (target->container_class != candidate->container_class) {
		MonoType *param_class;
		MonoClass *cand_class;
		VERIFIER_DEBUG ( printf ("generic class != target\n"); );
		param_class = candidate->context.class_inst->type_argv [0];
		VERIFIER_DEBUG ( printf ("param 0 %d\n", param_class->type); );
		cand_class = candidate->container_class;

		/* We must check if it's an interface type*/
		if (MONO_CLASS_IS_INTERFACE (target->container_class)) {
			VERIFIER_DEBUG ( printf ("generic type is an interface\n"); );

			do {
				int iface_count = cand_class->interface_count;
				MonoClass **ifaces = cand_class->interfaces;
				int i;
				VERIFIER_DEBUG ( printf ("type has %d interfaces\n", iface_count); );
				for (i = 0; i< iface_count; ++i) {
					MonoClass *ifc = ifaces[i];
					VERIFIER_DEBUG ( printf ("analysing %s\n", ifc->name); );
					if (ifc->generic_class) {
						VERIFIER_DEBUG ( printf ("interface has generic info\n"); );
					}
					if (mono_is_generic_instance_compatible (target, ifc->generic_class, root_candidate)) {
						VERIFIER_DEBUG ( printf ("we got compatible stuff!\n"); );
						return TRUE;
					}
				}

				cand_class = cand_class->parent;
			} while (cand_class);

			VERIFIER_DEBUG ( printf ("don't implements an interface\n"); );

		} else {
			VERIFIER_DEBUG ( printf ("verifying upper classes\n"); );

			cand_class = cand_class->parent;

			while (cand_class) {
				VERIFIER_DEBUG ( printf ("verifying parent class name %s\n", cand_class->name); );	
				if (cand_class->generic_class) {
					VERIFIER_DEBUG ( printf ("super type has generic context\n"); );

					/* TODO break loop if target->container_class == cand_class->generic_class->container_class */
					return mono_is_generic_instance_compatible (target, cand_class->generic_class, root_candidate);
				} else
					VERIFIER_DEBUG ( printf ("super class has no generic context\n"); );
				cand_class = cand_class->parent;
			}
		}
		return FALSE;
	}

	/* now we verify if the instantiations are compatible*/	
	if (target->context.class_inst == candidate->context.class_inst) {
		VERIFIER_DEBUG ( printf ("generic types are compatible, both have the same instantiation\n"); );
		return TRUE;
	}

	if (target->context.class_inst->type_argc != candidate->context.class_inst->type_argc) {
		VERIFIER_DEBUG ( printf ("generic instantiations with different arg counts\n"); );
		return FALSE;
	}

	//verify if open instance -- none should be 

	container = target->container_class->generic_container;

	for (i = 0; i < container->type_argc; ++i) {
		MonoGenericParam *param = container->type_params + i;
		MonoType *target_type = target->context.class_inst->type_argv [i];
		MonoType *candidate_type = candidate->context.class_inst->type_argv [i];
		/* We resolve TYPE_VAR types before proceeding */

		if (candidate_type->type == MONO_TYPE_VAR) {
			MonoGenericParam *var_param = candidate_type->data.generic_param;
			candidate_type = root_candidate->context.class_inst->type_argv [var_param->num];
		}

		if ((param->flags & GENERIC_PARAMETER_ATTRIBUTE_VARIANCE_MASK) == 0) {
			VERIFIER_DEBUG ( printf ("generic type have no variance flag, checking each type %d %d \n",target_type->type, candidate_type->type); );

			if (!mono_metadata_type_equal (target_type, candidate_type))
				return FALSE;
		} else {
			VERIFIER_DEBUG ( printf ("generic type has variance flag, need to perform deeper check\n"); );
			/* first we check if they are the same kind */
			/* byref generic params are forbiden, but better safe than sorry.*/

			if ((param->flags & GENERIC_PARAMETER_ATTRIBUTE_COVARIANT) == GENERIC_PARAMETER_ATTRIBUTE_COVARIANT) {
				if (!mono_is_generic_type_compatible (target_type, candidate_type))
					return FALSE;
			/* the attribute must be contravariant */
			} else if (!mono_is_generic_type_compatible (candidate_type, target_type))
				return FALSE;
		}
	}
	return TRUE;
}



/*Verify if type 'candidate' can be stored in type 'target'.
 * 
 * If strict, check for the underlying type and not the verification stack types
 */
static gboolean
verify_type_compatibility_full (VerifyContext *ctx, MonoType *target, MonoType *candidate, gboolean strict)
{
#define IS_ONE_OF3(T, A, B, C) (T == A || T == B || T == C)
#define IS_ONE_OF2(T, A, B) (T == A || T == B)

	VERIFIER_DEBUG ( printf ("checking type compatibility %p %p[%x][%x] %p[%x][%x]\n", ctx, target, target->type, target->byref, candidate, candidate->type, candidate->byref); );

 	/*only one is byref */
	if (candidate->byref ^ target->byref) {
		/* converting from native int to byref*/
		if (get_stack_type (candidate) == TYPE_NATIVE_INT && target->byref) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("using byref native int at 0x%04x", ctx->ip_offset));
			return TRUE;
		}
		return FALSE;
	}
	strict |= target->byref;

handle_enum:
	switch (target->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		if (strict)
			return IS_ONE_OF3 (candidate->type, MONO_TYPE_I1, MONO_TYPE_U1, MONO_TYPE_BOOLEAN);
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		if (strict)
			return IS_ONE_OF3 (candidate->type, MONO_TYPE_I2, MONO_TYPE_U2, MONO_TYPE_CHAR);
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		gboolean is_native_int = IS_ONE_OF2 (candidate->type, MONO_TYPE_I, MONO_TYPE_U);
		gboolean is_int4 = IS_ONE_OF2 (candidate->type, MONO_TYPE_I4, MONO_TYPE_U4);
		if (strict)
			return is_native_int || is_int4;
		return is_native_int || get_stack_type (candidate) == TYPE_I4;
	}

	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return IS_ONE_OF2 (candidate->type, MONO_TYPE_I8, MONO_TYPE_U8);

	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		if (strict)
			return candidate->type == target->type;
		return IS_ONE_OF2 (candidate->type, MONO_TYPE_R4, MONO_TYPE_R8);

	case MONO_TYPE_I:
	case MONO_TYPE_U: {
		gboolean is_native_int = IS_ONE_OF2 (candidate->type, MONO_TYPE_I, MONO_TYPE_U);
		gboolean is_int4 = IS_ONE_OF2 (candidate->type, MONO_TYPE_I4, MONO_TYPE_U4);
		if (strict)
			return is_native_int || is_int4;
		return is_native_int || get_stack_type (candidate) == TYPE_I4;
	}

	case MONO_TYPE_PTR:
		if (candidate->type != MONO_TYPE_PTR)
			return FALSE;
		/* check the underlying type */
		return verify_type_compatibility_full (ctx, target->data.type, candidate->data.type, TRUE);

	case MONO_TYPE_FNPTR: {
		MonoMethodSignature *left, *right;
		if (candidate->type != MONO_TYPE_FNPTR)
			return FALSE;

		left = mono_type_get_signature (target);
		right = mono_type_get_signature (candidate);
		return mono_metadata_signature_equal (left, right) && left->call_convention == right->call_convention;
	}

	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *left;
		MonoGenericClass *right;
		if (candidate->type != MONO_TYPE_GENERICINST)
			return mono_class_is_assignable_from (mono_class_from_mono_type (target), mono_class_from_mono_type (candidate));
		left = target->data.generic_class;
		right = candidate->data.generic_class;

		return mono_is_generic_instance_compatible (left, right, right);
	}

	case MONO_TYPE_STRING:
		return candidate->type == MONO_TYPE_STRING;

	case MONO_TYPE_CLASS:
		return mono_class_is_assignable_from (target->data.klass, mono_class_from_mono_type (candidate));

	case MONO_TYPE_OBJECT:
		return MONO_TYPE_IS_REFERENCE (candidate);

	case MONO_TYPE_SZARRAY: {
		MonoClass *left;
		MonoClass *right;
		if (candidate->type != MONO_TYPE_SZARRAY)
			return FALSE;

		left = target->data.klass;
		right = candidate->data.klass;
		return mono_class_is_assignable_from(left, right);
	}

	case MONO_TYPE_ARRAY:
		if (candidate->type != MONO_TYPE_ARRAY)
			return FALSE;
		return is_array_type_compatible (target, candidate);

	//TODO verify aditional checks that needs to be done
	case MONO_TYPE_TYPEDBYREF:
		return candidate->type == MONO_TYPE_TYPEDBYREF;

	case MONO_TYPE_VALUETYPE:
		if (candidate->type == MONO_TYPE_VALUETYPE && target->data.klass == candidate->data.klass)
			return TRUE;
		if (target->data.klass->enumtype) {
			target = target->data.klass->enum_basetype;
			goto handle_enum;
		}
		return FALSE;

	case MONO_TYPE_VAR:
		if (candidate->type != MONO_TYPE_VAR)
			return FALSE;
		return candidate->data.generic_param->num == target->data.generic_param->num;

	case MONO_TYPE_MVAR:
		if (candidate->type != MONO_TYPE_MVAR)
			return FALSE;
		return candidate->data.generic_param->num == target->data.generic_param->num;

	default:
		VERIFIER_DEBUG ( printf ("unknown store type %d\n", target->type); );
		g_assert_not_reached ();
		return FALSE;
	}
	return 1;
#undef IS_ONE_OF3
#undef IS_ONE_OF2
}

static gboolean
verify_type_compatibility (VerifyContext *ctx, MonoType *target, MonoType *candidate)
{
	return verify_type_compatibility_full (ctx, target, candidate, FALSE);
}


static int
verify_stack_type_compatibility (VerifyContext *ctx, MonoType *type, ILStackDesc *stack)
{
	MonoType *candidate = mono_type_from_stack_slot (stack);
	if (MONO_TYPE_IS_REFERENCE (type) && !type->byref && stack_slot_is_null_literal (stack))
		return TRUE;

	if (type->type == MONO_TYPE_OBJECT && mono_class_from_mono_type (candidate)->valuetype && !candidate->byref && stack_slot_is_boxed_value (stack))
		return TRUE;

	return verify_type_compatibility_full (ctx, type, candidate, FALSE);
}

/*
 * mono_delegate_signature_equal:
 * 
 * Compare two signatures in the way expected by delegates.
 * 
 * This function only exists due to the fact that it should ignore the 'has_this' part of the signature.
 *
 * FIXME can this function be eliminated and proper metadata functionality be used?
 */
static gboolean
mono_delegate_signature_equal (MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
	int i;

	//FIXME do we need to check for explicit_this?
	//We don't check for has_this since this is irrelevant for delegate signatures
	if (sig1->param_count != sig2->param_count) 
		return FALSE;

	if (sig1->generic_param_count != sig2->generic_param_count)
		return FALSE;

	for (i = 0; i < sig1->param_count; i++) { 
		MonoType *p1 = sig1->params [i];
		MonoType *p2 = sig2->params [i];

		if (!mono_metadata_type_equal_full (p1, p2, TRUE))
			return FALSE;
	}

	if (!mono_metadata_type_equal_full (sig1->ret, sig2->ret, TRUE))
		return FALSE;

	return TRUE;
}

/* 
 * verify_ldftn_delegate:
 * 
 * Verify properties of ldftn based delegates.
 */
static void
verify_ldftn_delegate (VerifyContext *ctx, MonoClass *delegate, ILStackDesc *value, ILStackDesc *funptr)
{
	MonoMethod *method = funptr->method;

	/*ldftn non-final virtuals only allowed if method is not static,
	 * the object is a this arg (comes from a ldarg.0), and there is no starg.0.
	 * This rules doesn't apply if the object on stack is a boxed valuetype.
	 */
	if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) && !(method->flags & METHOD_ATTRIBUTE_FINAL) && !stack_slot_is_boxed_value (value)) {
		/*A stdarg 0 must not happen, we fail here only in fail fast mode to avoid double error reports*/
		if (IS_FAIL_FAST_MODE (ctx) && ctx->has_this_store)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid ldftn with virtual function in method with stdarg 0 at  0x%04x", ctx->ip_offset));

		/*current method must not be static*/
		if (ctx->method->flags & METHOD_ATTRIBUTE_STATIC)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid ldftn with virtual function at 0x%04x", ctx->ip_offset));

		/*value is the this pointer, loaded using ldarg.0 */
		if (!stack_slot_is_this_pointer (value))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid object argument, it is not the this pointer, to ldftn with virtual method at  0x%04x", ctx->ip_offset));

		ctx->code [ctx->ip_offset].flags |= IL_CODE_LDFTN_DELEGATE_NONFINAL_VIRTUAL;
	}
}

/*
 * verify_delegate_compatibility:
 * 
 * Verify delegate creation sequence.
 * 
 */
static void
verify_delegate_compatibility (VerifyContext *ctx, MonoClass *delegate, ILStackDesc *value, ILStackDesc *funptr)
{
#define IS_VALID_OPCODE(offset, opcode) (ip [ip_offset - offset] == opcode && (ctx->code [ip_offset - offset].flags & IL_CODE_FLAG_SEEN))
#define IS_LOAD_FUN_PTR(kind) (IS_VALID_OPCODE (6, CEE_PREFIX1) && ip [ip_offset - 5] == kind)

	MonoMethod *invoke, *method;
	const guint8 *ip = ctx->header->code;
	guint32 ip_offset = ctx->ip_offset;
	
	if (stack_slot_get_type (funptr) != TYPE_PTR || !funptr->method) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid function pointer parameter for delegate constructor at %d", ctx->ip_offset));
		return;
	}
	
	invoke = mono_get_delegate_invoke (delegate);
	method = funptr->method;

	if (!mono_delegate_signature_equal (mono_method_signature (invoke), mono_method_signature (method)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Function pointer parameter for delegate constructor has diferent signature at %d", ctx->ip_offset));

	/* 
	 * Delegate code sequences:
	 * [-6] ldftn token
	 * newobj ...
	 * 
	 * 
	 * [-7] dup
	 * [-6] ldvirtftn token
	 * newobj ...
	 * 
	 * ldftn sequence:*/
	if (ip_offset > 5 && IS_LOAD_FUN_PTR (CEE_LDFTN)) {
		verify_ldftn_delegate (ctx, delegate, value, funptr);
	} else if (ip_offset > 6 && IS_VALID_OPCODE (7, CEE_DUP) && IS_LOAD_FUN_PTR (CEE_LDVIRTFTN)) {
		ctx->code [ip_offset - 6].flags |= IL_CODE_DELEGATE_SEQUENCE;	
	}else {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid code sequence for delegate creation at 0x%04x", ctx->ip_offset));
	}
	ctx->code [ip_offset].flags |= IL_CODE_DELEGATE_SEQUENCE;

	//general tests
	if (!verify_type_compatibility (ctx, &method->klass->byval_arg, value->type) && !stack_slot_is_null_literal (value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("This object not compatible with function pointer for delegate creation at 0x%04x", ctx->ip_offset));

	if (stack_slot_get_type (value) != TYPE_COMPLEX)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid first parameter for delegate creation at 0x%04x", ctx->ip_offset));

#undef IS_VALID_OPCODE
#undef IS_LOAD_FUN_PTR
}

/* implement the opcode checks*/
static void
push_arg (VerifyContext *ctx, unsigned int arg, int take_addr) 
{
	if (arg >= ctx->max_args) {
		if (take_addr) 
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have argument %d", arg + 1));
		else {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method doesn't have argument %d", arg + 1));
			if (check_overflow (ctx)) //FIXME: what sane value could we ever push?
				stack_push_val (ctx, TYPE_I4, &mono_defaults.int32_class->byval_arg);
		}
	} else if (check_overflow (ctx)) {
		/*We must let the value be pushed, otherwise we would get an underflow error*/
		check_unverifiable_type (ctx, ctx->params [arg]);
		if (ctx->params [arg]->byref && take_addr)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("ByRef of ByRef at 0x%04x", ctx->ip_offset));
		if (!set_stack_value (ctx, stack_push (ctx), ctx->params [arg], take_addr))
			return;

		if (arg == 0 && !take_addr && !(ctx->method->flags & METHOD_ATTRIBUTE_STATIC))
			stack_top (ctx)->stype |= THIS_POINTER_MASK;
	} 
}

static void
push_local (VerifyContext *ctx, guint32 arg, int take_addr) 
{
	if (arg >= ctx->num_locals) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have local %d", arg + 1));
	} else if (check_overflow (ctx)) {
		/*We must let the value be pushed, otherwise we would get an underflow error*/
		check_unverifiable_type (ctx, ctx->locals [arg]);
		if (ctx->locals [arg]->byref && take_addr)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("ByRef of ByRef at 0x%04x", ctx->ip_offset));

		set_stack_value (ctx, stack_push (ctx), ctx->locals [arg], take_addr);
	} 
}

static void
store_arg (VerifyContext *ctx, guint32 arg)
{
	ILStackDesc *value;

	if (arg >= ctx->max_args) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", arg + 1, ctx->ip_offset));
		check_underflow (ctx, 1);
		stack_pop (ctx);
		return;
	}

	if (check_underflow (ctx, 1)) {
		value = stack_pop (ctx);
		if (!verify_stack_type_compatibility (ctx, ctx->params [arg], value)) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type %s in argument store at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
		}
	}
	if (arg == 0)
		ctx->has_this_store = 1;
}

static void
store_local (VerifyContext *ctx, guint32 arg)
{
	ILStackDesc *value;
	if (arg >= ctx->num_locals) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", arg + 1, ctx->ip_offset));
		return;
	}

	/*TODO verify definite assigment */		
	if (check_underflow (ctx, 1)) {
		value = stack_pop(ctx);
		if (!verify_stack_type_compatibility (ctx, ctx->locals [arg], value)) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type %s in local store at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));	
		}
	}
}

/*FIXME add and sub needs special care here*/
static void
do_binop (VerifyContext *ctx, unsigned int opcode, const unsigned char table [TYPE_MAX][TYPE_MAX])
{
	ILStackDesc *a, *b;
	int idxa, idxb, complexMerge = 0;
	unsigned char res;

	if (!check_underflow (ctx, 2))
		return;
	a = stack_get (ctx, 1);
	b = stack_top (ctx);

	idxa = stack_slot_get_underlying_type (a);
	if (stack_slot_is_managed_pointer (a)) {
		idxa = TYPE_PTR;
		complexMerge = 1;
	}

	idxb = stack_slot_get_underlying_type (b);
	if (stack_slot_is_managed_pointer (b)) {
		idxb = TYPE_PTR;
		complexMerge = 2;
	}

	--idxa;
	--idxb;
	res = table [idxa][idxb];

	VERIFIER_DEBUG ( printf ("binop res %d\n", res); );
	VERIFIER_DEBUG ( printf ("idxa %d idxb %d\n", idxa, idxb); );

	ctx->eval.size--;
	if (res == TYPE_INV) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Binary instruction applyed to ill formed stack (%s x %s)", stack_slot_get_name (a), stack_slot_get_name (b)));
		return;
	}

 	if (res & NON_VERIFIABLE_RESULT) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Binary instruction is not verifiable (%s x %s)", stack_slot_get_name (a), stack_slot_get_name (b)));

 		res = res & ~NON_VERIFIABLE_RESULT;
 	}

 	if (complexMerge && res == TYPE_PTR) {
 		if (complexMerge == 1) 
 			copy_stack_value (stack_top (ctx), a);
 		else if (complexMerge == 2)
 			copy_stack_value (stack_top (ctx), b);
		/*
		 * There is no need to merge the type of two pointers.
		 * The only valid operation is subtraction, that returns a native
		 *  int as result and can be used with any 2 pointer kinds.
		 * This is valid acording to Patition III 1.1.4
		 */
 	} else
		stack_top (ctx)->stype = res;
 	
}


static void
do_boolean_branch_op (VerifyContext *ctx, int delta)
{
	int target = ctx->ip_offset + delta;
	ILStackDesc *top;

	VERIFIER_DEBUG ( printf ("boolean branch offset %d delta %d target %d\n", ctx->ip_offset, delta, target); );
 
	if (target < 0 || target >= ctx->code_size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Boolean branch target out of code at 0x%04x", ctx->ip_offset));
		return;
	}

	switch (is_valid_branch_instruction (ctx->header, ctx->ip_offset, target)) {
	case 1:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	case 2:
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		return;
	}

	ctx->target = target;

	if (!check_underflow (ctx, 1))
		return;

	top = stack_pop (ctx);
	if (!is_valid_bool_arg (top))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Argument type %s not valid for brtrue/brfalse at 0x%04x", stack_slot_get_name (stack_get (ctx, -1)), ctx->ip_offset));

	check_unmanaged_pointer (ctx, top);
}


static void
do_branch_op (VerifyContext *ctx, signed int delta, const unsigned char table [TYPE_MAX][TYPE_MAX])
{
	ILStackDesc *a, *b;
	int idxa, idxb;
	unsigned char res;
	int target = ctx->ip_offset + delta;

	VERIFIER_DEBUG ( printf ("branch offset %d delta %d target %d\n", ctx->ip_offset, delta, target); );
 
	if (target < 0 || target >= ctx->code_size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target out of code at 0x%04x", ctx->ip_offset));
		return;
	}

	switch (is_valid_cmp_branch_instruction (ctx->header, ctx->ip_offset, target)) {
	case 1:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	case 2:
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		return;
	}

	ctx->target = target;

	if (!check_underflow (ctx, 2))
		return;

	b = stack_pop (ctx);
	a = stack_pop (ctx);

	idxa = stack_slot_get_underlying_type (a);
	if (stack_slot_is_managed_pointer (a))
		idxa = TYPE_PTR;

	idxb = stack_slot_get_underlying_type (b);
	if (stack_slot_is_managed_pointer (b))
		idxb = TYPE_PTR;

	--idxa;
	--idxb;
	res = table [idxa][idxb];

	VERIFIER_DEBUG ( printf ("branch res %d\n", res); );
	VERIFIER_DEBUG ( printf ("idxa %d idxb %d\n", idxa, idxb); );

	if (res == TYPE_INV) {
		ADD_VERIFY_ERROR (ctx,
			g_strdup_printf ("Compare and Branch instruction applyed to ill formed stack (%s x %s) at 0x%04x", stack_slot_get_name (a), stack_slot_get_name (b), ctx->ip_offset));
	} else if (res & NON_VERIFIABLE_RESULT) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Compare and Branch instruction is not verifiable (%s x %s) at 0x%04x", stack_slot_get_name (a), stack_slot_get_name (b), ctx->ip_offset)); 
 		res = res & ~NON_VERIFIABLE_RESULT;
 	}
}

static void
do_cmp_op (VerifyContext *ctx, const unsigned char table [TYPE_MAX][TYPE_MAX], guint32 opcode)
{
	ILStackDesc *a, *b;
	int idxa, idxb;
	unsigned char res;

	if (!check_underflow (ctx, 2))
		return;
	b = stack_pop (ctx);
	a = stack_pop (ctx);

	if (opcode == CEE_CGT_UN) {
		if (stack_slot_get_type (a) == TYPE_COMPLEX && stack_slot_get_type (b) == TYPE_COMPLEX) {
			stack_push_val (ctx, TYPE_I4, &mono_defaults.int32_class->byval_arg);
			return;
		}
	}

	idxa = stack_slot_get_underlying_type (a);
	if (stack_slot_is_managed_pointer (a))
		idxa = TYPE_PTR;

	idxb = stack_slot_get_underlying_type (b);
	if (stack_slot_is_managed_pointer (b)) 
		idxb = TYPE_PTR;

	--idxa;
	--idxb;
	res = table [idxa][idxb];

	if(res == TYPE_INV) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf("Compare instruction applyed to ill formed stack (%s x %s) at 0x%04x", stack_slot_get_name (a), stack_slot_get_name (b), ctx->ip_offset));
	} else if (res & NON_VERIFIABLE_RESULT) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Compare instruction is not verifiable (%s x %s) at 0x%04x", stack_slot_get_name (a), stack_slot_get_name (b), ctx->ip_offset)); 
 		res = res & ~NON_VERIFIABLE_RESULT;
 	}
 	stack_push_val (ctx, TYPE_I4, &mono_defaults.int32_class->byval_arg);
}

static void
do_ret (VerifyContext *ctx)
{
	VERIFIER_DEBUG ( printf ("checking ret\n"); );
	if (ctx->signature->ret->type != MONO_TYPE_VOID) {
		ILStackDesc *top;
		if (!check_underflow (ctx, 1))
			return;

		top = stack_pop(ctx);

		if (!verify_stack_type_compatibility (ctx, ctx->signature->ret, top)) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible return value on stack with method signature ret at 0x%04x", ctx->ip_offset));
			return;
		}
	}

	if (ctx->eval.size > 0) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Stack not empty (%d) after ret at 0x%04x", ctx->eval.size, ctx->ip_offset));
	} 
	if (in_any_block (ctx->header, ctx->ip_offset))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("ret cannot escape exception blocks at 0x%04x", ctx->ip_offset));
}

/* FIXME: we could just load the signature instead of the whole MonoMethod
 * TODO handle vararg calls
 * TODO handle non virt calls to non-final virtual calls (from the verifiability clause in page 52 of partition 3)
 * TODO handle abstract calls
 * TODO handle calling .ctor outside one or calling the .ctor for other class but super
 * TODO handle call invoking virtual methods (only allowed to invoke super)  
 */
static void
do_invoke_method (VerifyContext *ctx, int method_token)
{
	int param_count, i;
	MonoMethodSignature *sig;
	ILStackDesc *value;
	MonoMethod *method = mono_get_method_full (ctx->image, method_token, NULL, ctx->generic_context);

	if (!method) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method 0x%08x not found at 0x%04x", method_token, ctx->ip_offset));
		return;
	}

	if (!(sig = mono_method_signature (method)))
		sig = mono_method_get_signature (method, ctx->image, method_token);

	param_count = sig->param_count + sig->hasthis;
	if (!check_underflow (ctx, param_count))
		return;

	for (i = sig->param_count - 1; i >= 0; --i) {
		VERIFIER_DEBUG ( printf ("verifying argument %d\n", i); );
		value = stack_pop (ctx);
		if (!verify_stack_type_compatibility (ctx, sig->params[i], value))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible parameter value with function signature at 0x%04x", ctx->ip_offset));
	}

	if (sig->hasthis) {
		MonoType * type = method->klass->valuetype ? &method->klass->this_arg : &method->klass->byval_arg;

		VERIFIER_DEBUG ( printf ("verifying this argument\n"); );
		value = stack_pop (ctx);
		if (!verify_stack_type_compatibility (ctx, type, value)) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Incompatible this argument on stack with method signature at 0x%04x", ctx->ip_offset));
			return;
		}
	}
	if (!mono_method_can_access_method (ctx->method, method))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method is not accessible at 0x%04x", ctx->ip_offset));

	if (sig->ret->type != MONO_TYPE_VOID) {
		if (check_overflow (ctx))
			set_stack_value (ctx, stack_push (ctx), sig->ret, FALSE);
	}

	if (sig->ret->type == MONO_TYPE_TYPEDBYREF
		|| sig->ret->byref
		|| (sig->ret->type == MONO_TYPE_VALUETYPE && !strcmp ("System", sig->ret->data.klass->name_space) && !strcmp ("ArgIterator", sig->ret->data.klass->name)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method returns typedbyref, byref or ArgIterator at 0x%04x", ctx->ip_offset));
}

static void
do_push_static_field (VerifyContext *ctx, int token, gboolean take_addr)
{
	MonoClassField *field;
	MonoClass *klass;

	field = mono_field_from_token (ctx->image, token, &klass, ctx->generic_context);
	if (!field) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) { 
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot load non static field at 0x%04x", ctx->ip_offset));
		return;
	}
	/*taking the address of initonly field only works from the static constructor */
	if (take_addr && (field->type->attrs & FIELD_ATTRIBUTE_INIT_ONLY) &&
		!(field->parent == ctx->method->klass && (ctx->method->flags & (METHOD_ATTRIBUTE_SPECIAL_NAME | METHOD_ATTRIBUTE_STATIC)) && !strcmp (".cctor", ctx->method->name)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot take the address of a init-only field at 0x%04x", ctx->ip_offset));

	if (!mono_method_can_access_field (ctx->method, field))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), field->type, take_addr);
}

static void
do_store_static_field (VerifyContext *ctx, int token) {
	MonoClassField *field;
	MonoClass *klass;
	ILStackDesc *value;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	field = mono_field_from_token (ctx->image, token, &klass, ctx->generic_context);
	if (!field) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot store field from token 0x%08x at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) { 
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot store non static field at 0x%04x", ctx->ip_offset));
		return;
	}

	if (field->type->type == MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Typedbyref field is an unverfiable type in store static field at 0x%04x", ctx->ip_offset));
		return;
	}

	if (!mono_method_can_access_field (ctx->method, field))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset));

	if (!verify_stack_type_compatibility (ctx, field->type, value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type %s in static field store at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));	
}

static gboolean
check_is_valid_type_for_field_ops (VerifyContext *ctx, int token, ILStackDesc *obj, MonoClassField **ret_field)
{
	MonoClassField *field;
	MonoClass *klass;

	/*must be one of: complex type, managed pointer (to complex type), unmanaged pointer (native int) or an instance of a value type */
	if (!( stack_slot_get_underlying_type (obj) == TYPE_COMPLEX
		|| stack_slot_get_type (obj) == TYPE_NATIVE_INT
		|| stack_slot_get_type (obj) == TYPE_PTR)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid argument %s to load field at 0x%04x", stack_slot_get_name (obj), ctx->ip_offset));
	}

	field = mono_field_from_token (ctx->image, token, &klass, ctx->generic_context);
	if (!field) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ctx->ip_offset));
		return FALSE;
	}

	*ret_field = field;

	if (field->type->type == MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Typedbyref field is an unverfiable type at 0x%04x", ctx->ip_offset));
		return FALSE;
	}
	g_assert (obj->type);

	/*The value on the stack must be a subclass of the defining type of the field*/ 
	/* we need to check if we can load the field from the stack value*/
	if (stack_slot_get_underlying_type (obj) == TYPE_COMPLEX) {
		if (!field->parent->valuetype && stack_slot_is_managed_pointer (obj))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is a managed pointer to a reference type and is not compatible to reference the field at 0x%04x", ctx->ip_offset));

		/*a value type can be loaded from a value or a managed pointer, but not a boxed object*/
		if (field->parent->valuetype && stack_slot_is_boxed_value (obj))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is a boxed valuetype and is not compatible to reference the field at 0x%04x", ctx->ip_offset));

		if (!stack_slot_is_null_literal (obj) && !verify_type_compatibility (ctx, &field->parent->byval_arg, mono_type_get_type_byval (obj->type)))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is not compatible to reference the field at 0x%04x", ctx->ip_offset));

		if (!mono_method_can_access_field (ctx->method, field))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset));
	}

	if (!mono_method_can_access_field (ctx->method, field))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset));

	if (stack_slot_get_underlying_type (obj) == TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Native int is not a verifiable type to reference a field at 0x%04x", ctx->ip_offset));

	check_unmanaged_pointer (ctx, obj);
	return TRUE;
}

static void
do_push_field (VerifyContext *ctx, int token, gboolean take_addr)
{
	ILStackDesc *obj;
	MonoClassField *field;

	if (!check_underflow (ctx, 1))
		return;
	obj = stack_pop (ctx);

	if (!check_is_valid_type_for_field_ops (ctx, token, obj, &field))
		return;

	if (take_addr && field->parent->valuetype && !stack_slot_is_managed_pointer (obj))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot take the address of a temporary value-type at 0x%04x", ctx->ip_offset));

	if (take_addr && (field->type->attrs & FIELD_ATTRIBUTE_INIT_ONLY) &&
		!(field->parent == ctx->method->klass && mono_method_is_constructor (ctx->method)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot take the address of a init-only field at 0x%04x", ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), field->type, take_addr);
}

static void
do_store_field (VerifyContext *ctx, int token)
{
	ILStackDesc *value, *obj;
	MonoClassField *field;

	if (!check_underflow (ctx, 2))
		return;

	value = stack_pop (ctx);
	obj = stack_pop (ctx);

	if (!check_is_valid_type_for_field_ops (ctx, token, obj, &field))
		return;

	if (!verify_stack_type_compatibility (ctx, field->type, value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type %s in field store at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));	
}

/*TODO proper handle for Nullable<T>*/
static void
do_box_value (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, klass_token);

	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_top (ctx);
	/*box is a nop for reference types*/

	if (stack_slot_get_underlying_type (value) == TYPE_COMPLEX && MONO_TYPE_IS_REFERENCE (value->type) && MONO_TYPE_IS_REFERENCE (type)) {
		value->stype |= BOXED_MASK;
		return;
	}

	value = stack_pop (ctx);

	if (!verify_stack_type_compatibility (ctx, type, value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for boxing operation at 0x%04x", ctx->ip_offset));

	stack_push_val (ctx, TYPE_COMPLEX | BOXED_MASK, type);
}

static void
do_unbox_value (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, klass_token);

	if (!type)
		return;
 
	if (!check_underflow (ctx, 1))
		return;

	if (!mono_class_from_mono_type (type)->valuetype)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid reference type for unbox at 0x%04x", ctx->ip_offset));

	value = stack_pop (ctx);

	/*Value should be: a boxed valuetype or a reference type*/
	if (!(stack_slot_get_type (value) == TYPE_COMPLEX &&
		(stack_slot_is_boxed_value (value) || !mono_class_from_mono_type (value->type)->valuetype)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type %s at stack for unbox operation at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));

	//TODO Pushed managed pointer is controled mutability (CMMP) 
	set_stack_value (ctx, stack_push (ctx), mono_type_get_type_byref (type), FALSE);
}

static void
do_unbox_any (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, klass_token);

	if (!type)
		return;
 
	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	/*Value should be: a boxed valuetype or a reference type*/
	if (!(stack_slot_get_type (value) == TYPE_COMPLEX &&
		(stack_slot_is_boxed_value (value) || !mono_class_from_mono_type (value->type)->valuetype)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type %s at stack for unbox.any operation at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
 
	set_stack_value (ctx, stack_push (ctx), type, FALSE);
}

static void
do_unary_math_op (VerifyContext *ctx, int op)
{
	ILStackDesc *value;
	if (!check_underflow (ctx, 1))
		return;
	value = stack_top (ctx);
	switch (stack_slot_get_type (value)) {
	case TYPE_I4:
	case TYPE_I8:
	case TYPE_NATIVE_INT:
		break;
	case TYPE_R8:
		if (op == CEE_NEG)
			break;
	case TYPE_COMPLEX: /*only enums are ok*/
		if (value->type->type == MONO_TYPE_VALUETYPE && value->type->data.klass->enumtype)
			break;
	default:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for unary not at 0x%04x", ctx->ip_offset));
	}
}

static void
do_conversion (VerifyContext *ctx, int kind) 
{
	ILStackDesc *value;
	if (!check_underflow (ctx, 1))
		return;
	value = stack_pop (ctx);

	switch (stack_slot_get_type (value)) {
	case TYPE_I4:
	case TYPE_I8:
	case TYPE_NATIVE_INT:
	case TYPE_R8:
		break;
	default:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type (%s) at stack for conversion operation at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
	}

	switch (kind) {
	case TYPE_I4:
		stack_push_val (ctx, TYPE_I4, &mono_defaults.int32_class->byval_arg);
		break;
	case TYPE_I8:
		stack_push_val (ctx,TYPE_I8, &mono_defaults.int64_class->byval_arg);
		break;
	case TYPE_R8:
		stack_push_val (ctx, TYPE_R8, &mono_defaults.double_class->byval_arg);
		break;
	case TYPE_NATIVE_INT:
		stack_push_val (ctx, TYPE_NATIVE_INT, &mono_defaults.int_class->byval_arg);
		break;
	default:
		g_error ("unknown type %02x in conversion", kind);

	}
}

static void
do_load_token (VerifyContext *ctx, int token) 
{
	gpointer handle;
	MonoClass *handle_class;
	if (!check_overflow (ctx))
		return;
	handle = mono_ldtoken (ctx->image, token, &handle_class, ctx->generic_context);
	if (!handle) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid token 0x%x for ldtoken at 0x%04x", token, ctx->ip_offset));
		return;
	}
	stack_push_val (ctx, TYPE_COMPLEX, mono_class_get_type (handle_class));
}

static void
do_ldobj_value (VerifyContext *ctx, int token) 
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, token);

	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);
	if (!stack_slot_is_managed_pointer (value) 
			&& stack_slot_get_type (value) != TYPE_NATIVE_INT
			&& !(stack_slot_get_type (value) == TYPE_PTR && value->type->type != MONO_TYPE_FNPTR)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid argument %s to ldobj at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
		return;
	}

	if (stack_slot_get_type (value) == TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Using native pointer to ldobj at 0x%04x", ctx->ip_offset));

	/*We have a byval on the stack, but the comparison must be strict. */
	if (!verify_type_compatibility_full (ctx, type, mono_type_get_type_byval (value->type), TRUE))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for ldojb operation at 0x%04x", ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), type, FALSE);
}

static void
do_stobj (VerifyContext *ctx, int token) 
{
	ILStackDesc *dest, *src;
	MonoType *type = get_boxable_mono_type (ctx, token);
	if (!type)
		return;

	if (!check_underflow (ctx, 2))
		return;

	src = stack_pop (ctx);
	dest = stack_pop (ctx);

	if (!stack_slot_is_managed_pointer (dest)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid destination of stobj operation at 0x%04x", ctx->ip_offset));

	if (!verify_stack_type_compatibility (ctx, type, src))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Token and source types of stobj don't match at 0x%04x", ctx->ip_offset));

	if (!verify_type_compatibility (ctx, mono_type_get_type_byval (dest->type), type))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Destination and token types of stobj don't match at 0x%04x", ctx->ip_offset));
}

static void
do_cpobj (VerifyContext *ctx, int token)
{
	ILStackDesc *dest, *src;
	MonoType *type = get_boxable_mono_type (ctx, token);
	if (!type)
		return;

	if (!check_underflow (ctx, 2))
		return;

	src = stack_pop (ctx);
	dest = stack_pop (ctx);

	if (!stack_slot_is_managed_pointer (src)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid source of cpobj operation at 0x%04x", ctx->ip_offset));

	if (!stack_slot_is_managed_pointer (dest)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid destination of cpobj operation at 0x%04x", ctx->ip_offset));

	if (!verify_type_compatibility (ctx, type, mono_type_get_type_byval (src->type)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Token and source types of cpobj don't match at 0x%04x", ctx->ip_offset));

	if (!verify_type_compatibility (ctx, mono_type_get_type_byval (dest->type), type))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Destination and token types of cpobj don't match at 0x%04x", ctx->ip_offset));
}

static void
do_initobj (VerifyContext *ctx, int token)
{
	ILStackDesc *obj;
	MonoType *stack, *type = get_boxable_mono_type (ctx, token);
	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	obj = stack_pop (ctx);

	if (!stack_slot_is_managed_pointer (obj)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid object address for initobj at 0x%04x", ctx->ip_offset));

	stack = mono_type_get_type_byval (obj->type);
	if (MONO_TYPE_IS_REFERENCE (stack)) {
		if (!verify_type_compatibility (ctx, stack, type)) 
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type token of initobj not compatible with value on stack at 0x%04x", ctx->ip_offset));
		else if (IS_STRICT_MODE (ctx) && !mono_metadata_type_equal (type, stack)) 
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type token of initobj not compatible with value on stack at 0x%04x", ctx->ip_offset));
	} else if (!verify_type_compatibility (ctx, stack, type)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type token of initobj not compatible with value on stack at 0x%04x", ctx->ip_offset));
	}
}

static void
do_newobj (VerifyContext *ctx, int token) 
{
	ILStackDesc *value;
	int i;
	MonoMethodSignature *sig;
	MonoMethod *method = mono_get_method_full (ctx->image, token, NULL, ctx->generic_context);
	gboolean is_delegate = FALSE;

	if (!method) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Constructor 0x%08x not found at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (!mono_method_is_constructor (method)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method from token 0x%08x not a constructor at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (method->klass->flags & (TYPE_ATTRIBUTE_ABSTRACT | TYPE_ATTRIBUTE_INTERFACE))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Trying to instantiate an abstract or interface type at 0x%04x", ctx->ip_offset));

	if (!mono_method_can_access_method (ctx->method, method))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Constructor not visible at 0x%04x", ctx->ip_offset));

	sig = mono_method_signature (method);
	if (!check_underflow (ctx, sig->param_count))
		return;

	is_delegate = method->klass->parent == mono_defaults.multicastdelegate_class;

	if (is_delegate) {
		ILStackDesc *funptr;
		//first arg is object, second arg is fun ptr
		if (sig->param_count != 2) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid delegate constructor at 0x%04x", ctx->ip_offset));
			return;
		}
		funptr = stack_pop (ctx);
		value = stack_pop (ctx);
		verify_delegate_compatibility (ctx, method->klass, value, funptr);
	} else {
		for (i = sig->param_count - 1; i >= 0; --i) {
			VERIFIER_DEBUG ( printf ("verifying constructor argument %d\n", i); );
			value = stack_pop (ctx);
			if (!verify_stack_type_compatibility (ctx, sig->params [i], value))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible parameter value with function signature at 0x%04x", ctx->ip_offset));
		}
	}

	if (check_overflow (ctx))
		set_stack_value (ctx, stack_push (ctx),  &method->klass->byval_arg, FALSE);
}

static void
do_cast (VerifyContext *ctx, int token) {
	ILStackDesc *value;
	MonoType *type;
	gboolean is_boxed;

	if (!check_underflow (ctx, 1))
		return;

	type = mono_type_get_full (ctx->image, token, ctx->generic_context);

	if (!type) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Type (0x%08x) not found at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (type->byref) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid castclass type at 0x%04x",  ctx->ip_offset));
		return;
	}

	value = stack_pop (ctx);
	is_boxed = stack_slot_is_boxed_value (value);

	if (stack_slot_is_managed_pointer (value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value for checkcast at 0x%04x", ctx->ip_offset));
	else if (mono_class_from_mono_type (value->type)->valuetype && !is_boxed)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Value cannot be a valuetype for checkast at 0x%04x", ctx->ip_offset));

	switch (value->type->type) {
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_TYPEDBYREF: 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value for checkast at 0x%04x", ctx->ip_offset));
	}

	stack_push_val (ctx, TYPE_COMPLEX | (is_boxed ? BOXED_MASK : 0), type);
}

static MonoType *
mono_type_from_opcode (int opcode) {
	switch (opcode) {
	case CEE_LDIND_I1:
	case CEE_LDIND_U1:
	case CEE_STIND_I1:
	case CEE_LDELEM_I1:
	case CEE_LDELEM_U1:
	case CEE_STELEM_I1:
		return &mono_defaults.sbyte_class->byval_arg;

	case CEE_LDIND_I2:
	case CEE_LDIND_U2:
	case CEE_STIND_I2:
	case CEE_LDELEM_I2:
	case CEE_LDELEM_U2:
	case CEE_STELEM_I2:
		return &mono_defaults.int16_class->byval_arg;

	case CEE_LDIND_I4:
	case CEE_LDIND_U4:
	case CEE_STIND_I4:
	case CEE_LDELEM_I4:
	case CEE_LDELEM_U4:
	case CEE_STELEM_I4:
		return &mono_defaults.int32_class->byval_arg;

	case CEE_LDIND_I8:
	case CEE_STIND_I8:
	case CEE_LDELEM_I8:
	case CEE_STELEM_I8:
		return &mono_defaults.int64_class->byval_arg;

	case CEE_LDIND_R4:
	case CEE_STIND_R4:
	case CEE_LDELEM_R4:
	case CEE_STELEM_R4:
		return &mono_defaults.single_class->byval_arg;

	case CEE_LDIND_R8:
	case CEE_STIND_R8:
	case CEE_LDELEM_R8:
	case CEE_STELEM_R8:
		return &mono_defaults.double_class->byval_arg;

	case CEE_LDIND_I:
	case CEE_STIND_I:
	case CEE_LDELEM_I:
	case CEE_STELEM_I:
		return &mono_defaults.int_class->byval_arg;

	case CEE_LDIND_REF:
	case CEE_STIND_REF:
	case CEE_LDELEM_REF:
	case CEE_STELEM_REF:
		return &mono_defaults.object_class->byval_arg;

	default:
		g_error ("unknown opcode %02x in mono_type_from_opcode ", opcode);
		return NULL;
	}
}

static void
do_load_indirect (VerifyContext *ctx, int opcode)
{
	ILStackDesc *value;
	if (!check_underflow (ctx, 1))
		return;
	
	value = stack_pop (ctx);
	if (!stack_slot_is_managed_pointer (value)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Load indirect not using a manager pointer at 0x%04x", ctx->ip_offset));
		set_stack_value (ctx, stack_push (ctx), mono_type_from_opcode (opcode), FALSE);
		return;
	}

	if (opcode == CEE_LDIND_REF) {
		if (stack_slot_get_underlying_type (value) != TYPE_COMPLEX || mono_class_from_mono_type (value->type)->valuetype)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for ldind_ref expected object byref operation at 0x%04x", ctx->ip_offset));
		set_stack_value (ctx, stack_push (ctx), mono_type_get_type_byval (value->type), FALSE);
	} else {
		if (!verify_type_compatibility_full (ctx, mono_type_from_opcode (opcode), mono_type_get_type_byval (value->type), TRUE))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for ldind 0x%x operation at 0x%04x", opcode, ctx->ip_offset));
		set_stack_value (ctx, stack_push (ctx), mono_type_from_opcode (opcode), FALSE);
	}
}

static void
do_store_indirect (VerifyContext *ctx, int opcode)
{
	ILStackDesc *addr, *val;
	if (!check_underflow (ctx, 2))
		return;

	val = stack_pop (ctx);
	addr = stack_pop (ctx);	

	check_unmanaged_pointer (ctx, addr);

	if (!stack_slot_is_managed_pointer (addr) && stack_slot_get_type (addr) != TYPE_PTR) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid non-pointer argument to stind at 0x%04x", ctx->ip_offset));
		return;
	}

	if (!verify_type_compatibility_full (ctx, mono_type_from_opcode (opcode), mono_type_get_type_byval (addr->type), TRUE))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid addr type at stack for stind 0x%x operation at 0x%04x", opcode, ctx->ip_offset));

	if (!verify_stack_type_compatibility (ctx, mono_type_from_opcode (opcode), val))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value type at stack for stind 0x%x operation at 0x%04x", opcode, ctx->ip_offset));
}

static void
do_newarr (VerifyContext *ctx, int token) 
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, token);

	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);
	if (stack_slot_get_type (value) != TYPE_I4 && stack_slot_get_type (value) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Array size type on stack (%s) is not a verifiable type at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), mono_class_get_type (mono_array_class_get (mono_class_from_mono_type (type), 1)), FALSE);
}

/*FIXME handle arrays that are not 0-indexed*/
static void
do_ldlen (VerifyContext *ctx)
{
	ILStackDesc *value;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	if (stack_slot_get_type (value) != TYPE_COMPLEX || value->type->type != MONO_TYPE_SZARRAY)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type for ldlen at 0x%04x", ctx->ip_offset));

	stack_push_val (ctx, TYPE_NATIVE_INT, &mono_defaults.int_class->byval_arg);	
}

/*FIXME handle arrays that are not 0-indexed*/
/*FIXME handle readonly prefix and CMMP*/
static void
do_ldelema (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *index, *array;
	MonoType *type = get_boxable_mono_type (ctx, klass_token);
	gboolean valid; 

	if (!type)
		return;

	if (!check_underflow (ctx, 2))
		return;

	index = stack_pop (ctx);
	array = stack_pop (ctx);

	if (stack_slot_get_type (index) != TYPE_I4 && stack_slot_get_type (index) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Index type(%s) for ldelema is not an int or a native int at 0x%04x", stack_slot_get_name (index), ctx->ip_offset));

	if (!stack_slot_is_null_literal (array)) {
		if (stack_slot_get_type (array) != TYPE_COMPLEX || array->type->type != MONO_TYPE_SZARRAY)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type(%s) for ldelema at 0x%04x", stack_slot_get_name (array), ctx->ip_offset));
		else {
			if (get_stack_type (type) == TYPE_I4 || get_stack_type (type) == TYPE_NATIVE_INT) {
					valid = verify_type_compatibility_full (ctx, type, &array->type->data.klass->byval_arg, TRUE);
			} else {
				valid = mono_metadata_type_equal (type, &array->type->data.klass->byval_arg);
			}
			if (!valid)
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type on stack for ldelema at 0x%04x", ctx->ip_offset));
		}
	}

	set_stack_value (ctx, stack_push (ctx), type, TRUE);	
}

/*FIXME handle arrays that are not 0-indexed*/
/*FIXME handle readonly prefix and CMMP*/
static void
do_ldelem (VerifyContext *ctx, int opcode, int token)
{
	ILStackDesc *index, *array;
	MonoType *type;
	if (!check_underflow (ctx, 2))
		return;

	if (opcode == CEE_LDELEM_ANY) {
		type = mono_type_get_full (ctx->image, token, ctx->generic_context);
		if (!type) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Type (0x%08x) not found at 0x%04x", token, ctx->ip_offset));
			return;
		}
	} else {
		type = mono_type_from_opcode (opcode);
	}

	index = stack_pop (ctx);
	array = stack_pop (ctx);

	if (stack_slot_get_type (index) != TYPE_I4 && stack_slot_get_type (index) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Index type(%s) for ldelema is not an int or a native int at 0x%04x", stack_slot_get_name (index), ctx->ip_offset));

	if (!stack_slot_is_null_literal (array)) {
		if (stack_slot_get_type (array) != TYPE_COMPLEX || array->type->type != MONO_TYPE_SZARRAY)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type(%s) for ldelem.X at 0x%04x", stack_slot_get_name (array), ctx->ip_offset));
		else {
			if (opcode == CEE_LDELEM_REF) {
				if (array->type->data.klass->valuetype)
					CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type is not a reference type for ldelem.ref 0x%04x", ctx->ip_offset));
				type = &array->type->data.klass->byval_arg;
			} else if (!verify_type_compatibility_full (ctx, type, &array->type->data.klass->byval_arg, TRUE)) {
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type on stack for ldelem.X at 0x%04x", ctx->ip_offset));
			}
		}
	}

	set_stack_value (ctx, stack_push (ctx), type, FALSE);
}

/*FIXME handle arrays that are not 0-indexed*/
static void
do_stelem (VerifyContext *ctx, int opcode, int token)
{
	ILStackDesc *index, *array, *value;
	MonoType *type;
	if (!check_underflow (ctx, 3))
		return;

	if (opcode == CEE_STELEM_ANY) {
		type = mono_type_get_full (ctx->image, token, ctx->generic_context);
		if (!type) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Type (0x%08x) not found at 0x%04x", token, ctx->ip_offset));
			return;
		}
	} else {
		type = mono_type_from_opcode (opcode);
	}
	
	value = stack_pop (ctx);
	index = stack_pop (ctx);
	array = stack_pop (ctx);

	if (stack_slot_get_type (index) != TYPE_I4 && stack_slot_get_type (index) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Index type(%s) for stdelem.X is not an int or a native int at 0x%04x", stack_slot_get_name (index), ctx->ip_offset));

	if (!stack_slot_is_null_literal (array)) {
		if (stack_slot_get_type (array) != TYPE_COMPLEX || array->type->type != MONO_TYPE_SZARRAY) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type(%s) for stelem.X at 0x%04x", stack_slot_get_name (array), ctx->ip_offset));
		} else {
			if (opcode == CEE_STELEM_REF) {
				if (array->type->data.klass->valuetype)
					CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type is not a reference type for stelem.ref 0x%04x", ctx->ip_offset));
			} else if (!verify_type_compatibility_full (ctx, &array->type->data.klass->byval_arg, type, TRUE)) {
					CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type on stack for stdelem.X at 0x%04x", ctx->ip_offset));
			}
		}
	}

	if (opcode == CEE_STELEM_REF) {
		if (!stack_slot_is_boxed_value (value) && mono_class_from_mono_type (value->type)->valuetype)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value is not a reference type for stelem.ref 0x%04x", ctx->ip_offset));
	} else if (opcode != CEE_STELEM_REF && !verify_type_compatibility_full (ctx, type, mono_type_from_stack_slot (value), FALSE)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value on stack for stdelem.X at 0x%04x", ctx->ip_offset));
	}
}

static void
do_throw (VerifyContext *ctx)
{
	ILStackDesc *exception;
	if (!check_underflow (ctx, 1))
		return;
	exception = stack_pop (ctx);

	if (!stack_slot_is_null_literal (exception) && !(stack_slot_get_type (exception) == TYPE_COMPLEX && !mono_class_from_mono_type (exception->type)->valuetype))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type on stack for throw, expected reference type at 0x%04x", ctx->ip_offset));

	/*The stack is left empty after a throw*/
	ctx->eval.size = 0;
}


static void
do_endfilter (VerifyContext *ctx)
{
	MonoExceptionClause *clause;

	if (IS_STRICT_MODE (ctx)) {
		if (ctx->eval.size != 1)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Stack size must have one item for endfilter at 0x%04x", ctx->ip_offset));

		if (ctx->eval.size >= 1 && stack_slot_get_type (stack_pop (ctx)) != TYPE_I4)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Stack item type is not an int32 for endfilter at 0x%04x", ctx->ip_offset));
	}

	if ((clause = is_correct_endfilter (ctx, ctx->ip_offset))) {
		if (IS_STRICT_MODE (ctx)) {
			if (ctx->ip_offset != clause->handler_offset - 2)
				ADD_VERIFY_ERROR (ctx, g_strdup_printf ("endfilter is not the last instruction of the filter clause at 0x%04x", ctx->ip_offset));			
		} else {
			if ((ctx->ip_offset != clause->handler_offset - 2) && !MONO_OFFSET_IN_HANDLER (clause, ctx->ip_offset))
				ADD_VERIFY_ERROR (ctx, g_strdup_printf ("endfilter is not the last instruction of the filter clause at 0x%04x", ctx->ip_offset));
		}
	} else {
		if (IS_STRICT_MODE (ctx) && !is_unverifiable_endfilter (ctx, ctx->ip_offset))
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("endfilter outside filter clause at 0x%04x", ctx->ip_offset));
		else
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("endfilter outside filter clause at 0x%04x", ctx->ip_offset));
	}

	ctx->eval.size = 0;
}

static void
do_leave (VerifyContext *ctx, int delta)
{
	int target = ((gint32)ctx->ip_offset) + delta;
	if (target >= ctx->code_size || target < 0)
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target out of code at 0x%04x", ctx->ip_offset));

	if (!is_correct_leave (ctx->header, ctx->ip_offset, target))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Leave not allowed in finally block at 0x%04x", ctx->ip_offset));
}

/* 
 * do_static_branch:
 * 
 * Verify br and br.s opcodes.
 */
static void
do_static_branch (VerifyContext *ctx, int delta)
{
	int target = ctx->ip_offset + delta;
	if (target < 0 || target >= ctx->code_size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("branch target out of code at 0x%04x", ctx->ip_offset));
		return;
	}

	switch (is_valid_branch_instruction (ctx->header, ctx->ip_offset, target)) {
	case 1:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	case 2:
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	}

	ctx->target = target;
}

static void
do_switch (VerifyContext *ctx, int count, const unsigned char *data)
{
	int i, base = ctx->ip_offset + 5 + count * 4;
	ILStackDesc *value;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	if (stack_slot_get_type (value) != TYPE_I4 && stack_slot_get_type (value) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid argument to switch at 0x%04x", ctx->ip_offset));

	for (i = 0; i < count; ++i) {
		int target = base + read32 (data + i * 4);

		if (target < 0 || target >= ctx->code_size)
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Switch target %x out of code at 0x%04x", i, ctx->ip_offset));

		switch (is_valid_branch_instruction (ctx->header, ctx->ip_offset, target)) {
		case 1:
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Switch target %x escapes out of exception block at 0x%04x", i, ctx->ip_offset));
			break;
		case 2:
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Switch target %x escapes out of exception block at 0x%04x", i, ctx->ip_offset));
			break;
		}
		merge_stacks (ctx, &ctx->eval, &ctx->code [target], FALSE, TRUE);
	}
}

static void
do_load_function_ptr (VerifyContext *ctx, guint32 token, gboolean virtual)
{
	ILStackDesc *top;
	MonoMethod *method;

	if (virtual && !check_underflow (ctx, 1))
		return;

	if (!virtual && !check_overflow (ctx))
		return;

	if (!IS_METHOD_DEF_OR_REF_OR_SPEC (token) || !token_bounds_check (ctx->image, token)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid token %x for ldftn  at 0x%04x", token, ctx->ip_offset));
		return;
	}

	method = mono_get_method_full (ctx->image, token, NULL, ctx->generic_context);

	if (!method) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Could not resolve ldftn token %x at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (mono_method_is_constructor (method))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use ldftn with a constructor at 0x%04x", ctx->ip_offset));

	if (virtual) {
		ILStackDesc *top = stack_pop (ctx);
	
		if (stack_slot_get_type (top) != TYPE_COMPLEX || top->type->type == MONO_TYPE_VALUETYPE)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid argument to ldvirtftn at 0x%04x", ctx->ip_offset));
	
		if (method->flags & METHOD_ATTRIBUTE_STATIC)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use ldvirtftn with a constructor at 0x%04x", ctx->ip_offset));
	
		if (!verify_type_compatibility (ctx, &method->klass->byval_arg, top->type))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Unexpected object for ldvirtftn at 0x%04x", ctx->ip_offset));
	}
	
	if (!mono_method_can_access_method (ctx->method, method))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Loaded method is not visible for ldftn/ldvirtftn at 0x%04x", ctx->ip_offset));

	top = stack_push_val(ctx, TYPE_PTR, mono_type_create_fnptr_from_mono_method (ctx, method));
	top->method = method;
}

static void
do_sizeof (VerifyContext *ctx, int token)
{
	MonoType *type;

	if (!IS_TYPE_DEF_OR_REF_OR_SPEC (token) || !token_bounds_check (ctx->image, token)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid type token %x at 0x%04x", token, ctx->ip_offset));
		return;
	}
	
	type = mono_type_get_full (ctx->image, token, ctx->generic_context);

	if (!type) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Type (0x%08x) not found at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (type->byref && type->type != MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of byref type at 0x%04x", ctx->ip_offset));
		return;
	}

	if (type->type == MONO_TYPE_VOID) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of void type at 0x%04x", ctx->ip_offset));
		return;
	}

	if (check_overflow (ctx))
		set_stack_value (ctx, stack_push (ctx), &mono_defaults.uint32_class->byval_arg, FALSE);
}

/* Stack top can be of any type, the runtime doesn't care and treat everything as an int. */
static void
do_localloc (VerifyContext *ctx)
{
	if (ctx->eval.size != 1) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Stack must have only size item in localloc at 0x%04x", ctx->ip_offset));
		return;		
	}

	if (in_any_exception_block (ctx->header, ctx->ip_offset)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Stack must have only size item in localloc at 0x%04x", ctx->ip_offset));
		return;
	}

	set_stack_value (ctx, stack_top (ctx), &mono_defaults.int_class->byval_arg, FALSE);
	CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Instruction localloc in never verifiable at 0x%04x", ctx->ip_offset));
}

static void
do_ldstr (VerifyContext *ctx, guint32 token)
{
	if (token >= ctx->image->heap_us.size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid string index at 0x%04x", ctx->ip_offset));
		return;
	}

	if (check_overflow (ctx))
		stack_push_val (ctx, TYPE_COMPLEX,  &mono_defaults.string_class->byval_arg);
}

/*Merge the stacks and perform compat checks*/
static void
merge_stacks (VerifyContext *ctx, ILCodeDesc *from, ILCodeDesc *to, int start, gboolean external) 
{
	int i;
	stack_init (ctx, to);

	if (start) {
		if (to->flags == IL_CODE_FLAG_NOT_PROCESSED) 
			from->size = 0;
		else
			stack_copy (&ctx->eval, to);
		goto end_verify;
	} else if (!(to->flags & IL_CODE_STACK_MERGED)) {
		stack_copy (to, &ctx->eval);
		goto end_verify;
	}
	VERIFIER_DEBUG ( printf ("performing stack merge %d x %d\n", from->size, to->size); );

	if (from->size != to->size) {
		VERIFIER_DEBUG ( printf ("different stack sizes %d x %d at 0x%04x\n", from->size, to->size, ctx->ip_offset); );
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Could not merge stacks, different sizes (%d x %d) at 0x%04x", from->size, to->size, ctx->ip_offset)); 
		goto end_verify;
	}

	for (i = 0; i < from->size; ++i) {
		ILStackDesc *from_slot = from->stack + i;
		ILStackDesc *to_slot = to->stack + i;

		if (!verify_type_compatibility (ctx, mono_type_from_stack_slot (to_slot), mono_type_from_stack_slot (from_slot))) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Could not merge stacks, types not compatible at 0x%04x", ctx->ip_offset)); 
			goto end_verify;
		}

		/*TODO we need to choose the base class for merging reference types*/
		copy_stack_value (to_slot, from_slot);
	}

end_verify:
	if (external)
		to->flags |= IL_CODE_FLAG_WAS_TARGET;
	to->flags |= IL_CODE_STACK_MERGED;
}

#define HANDLER_START(clause) ((clause)->flags == MONO_EXCEPTION_CLAUSE_FILTER ? (clause)->data.filter_offset : clause->handler_offset)
#define IS_CATCH_OR_FILTER(clause) ((clause)->flags == MONO_EXCEPTION_CLAUSE_FILTER || (clause)->flags == MONO_EXCEPTION_CLAUSE_NONE)

/*
 * is_clause_in_range :
 * 
 * Returns TRUE if either the protected block or the handler of @clause is in the @start - @end range.  
 */
static gboolean
is_clause_in_range (MonoExceptionClause *clause, guint32 start, guint32 end)
{
	if (clause->try_offset >= start && clause->try_offset < end)
		return TRUE;
	if (HANDLER_START (clause) >= start && HANDLER_START (clause) < end)
		return TRUE;
	return FALSE;
}

/*
 * is_clause_inside_range :
 * 
 * Returns TRUE if @clause lies completely inside the @start - @end range.  
 */
static gboolean
is_clause_inside_range (MonoExceptionClause *clause, guint32 start, guint32 end)
{
	if (clause->try_offset < start || (clause->try_offset + clause->try_len) > end)
		return FALSE;
	if (HANDLER_START (clause) < start || (clause->handler_offset + clause->handler_len) > end)
		return FALSE;
	return TRUE;
}

/*
 * is_clause_nested :
 * 
 * Returns TRUE if @nested is nested in @clause.   
 */
static gboolean
is_clause_nested (MonoExceptionClause *clause, MonoExceptionClause *nested)
{
	if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER && is_clause_inside_range (nested, clause->data.filter_offset, clause->handler_offset))
		return TRUE;
	return is_clause_inside_range (nested, clause->try_offset, clause->try_offset + clause->try_len) ||
	is_clause_inside_range (nested, clause->handler_offset, clause->handler_offset + clause->handler_len);
}

/* Test the relationship between 2 exception clauses. Follow  P.1 12.4.2.7 of ECMA
 * the each pair of exception must have the following properties:
 *  - one is fully nested on another (the outer must not be a filter clause) (the nested one must come earlier)
 *  - completely disjoin (none of the 3 regions of each entry overlap with the other 3)
 *  - mutual protection (protected block is EXACT the same, handlers are disjoin and all handler are catch or all handler are filter)
 */
static void
verify_clause_relationship (VerifyContext *ctx, MonoExceptionClause *clause, MonoExceptionClause *to_test)
{
	/*clause is nested*/
	if (is_clause_nested (to_test, clause)) {
		if (to_test->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception clause inside filter"));
		}
		return;
	}

	/*wrong nesting order.*/
	if (is_clause_nested (clause, to_test)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Nested exception clause appears after enclosing clause"));
		return;
	}

	/*mutual protection*/
	if (clause->try_offset == to_test->try_offset && clause->try_len == to_test->try_len) {
		/*handlers are not disjoint*/
		if (is_clause_in_range (to_test, HANDLER_START (clause), clause->handler_offset + clause->handler_len)) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception handlers overlap"));
			return;
		}
		/* handlers are not catch or filter */
		if (!IS_CATCH_OR_FILTER (clause) || !IS_CATCH_OR_FILTER (to_test)) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception clauses with shared protected block are neither catch or filter"));
			return;
		}
		/*OK*/
		return;
	}

	/*not completelly disjoint*/
	if (is_clause_in_range (to_test, clause->try_offset, clause->try_offset + clause->try_len) ||
		is_clause_in_range (to_test, HANDLER_START (clause), clause->handler_offset + clause->handler_len))
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception clauses overlap"));
}

/*
 * FIXME: need to distinguish between valid and verifiable.
 * Need to keep track of types on the stack.
 * Verify types for opcodes.
 */
GSList*
mono_method_verify (MonoMethod *method, int level)
{
	const unsigned char *ip;
	const unsigned char *end;
	int i, n, need_merge = 0, start = 0;
	guint token, ip_offset = 0, prefix = 0, prefix_list = 0;
	MonoGenericContext *generic_context = NULL;
	MonoImage *image;
	VerifyContext ctx;
	GSList *tmp;

	VERIFIER_DEBUG ( printf ("Verify IL for method %s %s %s\n",  method->klass->name,  method->klass->name, method->name); );

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
			(method->flags & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT))) {
		return NULL;
	}

	memset (&ctx, 0, sizeof (VerifyContext));

	ctx.signature = mono_method_signature (method);
	if (!ctx.signature) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Could not decode method signature"));
		return ctx.list;
	}
	ctx.header = mono_method_get_header (method);
	if (!ctx.header) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Could not decode method header"));
		return ctx.list;
	}
	ctx.method = method;
	ip = ctx.header->code;
	end = ip + ctx.header->code_size;
	ctx.image = image = method->klass->image;


	ctx.max_args = ctx.signature->param_count + ctx.signature->hasthis;
	ctx.max_stack = ctx.header->max_stack;
	ctx.verifiable = ctx.valid = 1;
	ctx.level = level;

	ctx.code = g_new0 (ILCodeDesc, ctx.header->code_size);
	ctx.code_size = ctx.header->code_size;

	memset(ctx.code, 0, sizeof (ILCodeDesc) * ctx.header->code_size);


	ctx.num_locals = ctx.header->num_locals;
	ctx.locals = ctx.header->locals;


	if (ctx.signature->hasthis) {
		ctx.params = g_new0 (MonoType*, ctx.max_args);
		ctx.params [0] = method->klass->valuetype ? &method->klass->this_arg : &method->klass->byval_arg;
		memcpy (ctx.params + 1, ctx.signature->params, sizeof (MonoType *) * ctx.signature->param_count);
	} else {
		ctx.params = ctx.signature->params;
	}

	if (ctx.signature->is_inflated)
		ctx.generic_context = generic_context = mono_method_get_context (method);

	stack_init (&ctx, &ctx.eval);

	for (i = 0; i < ctx.header->num_clauses && ctx.valid; ++i) {
		MonoExceptionClause *clause = ctx.header->clauses + i;
		VERIFIER_DEBUG (printf ("clause try %x len %x filter at %x handler at %x len %x\n", clause->try_offset, clause->try_len, clause->data.filter_offset, clause->handler_offset, clause->handler_len); );

		if (clause->try_offset > ctx.code_size)
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try clause out of bounds at 0x%04x", clause->try_offset));

		if (clause->try_len <= 0)
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try clause len <= 0 at 0x%04x", clause->try_offset));

		if (clause->handler_offset > ctx.code_size)
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try clause out of bounds at 0x%04x", clause->try_offset));

		if (clause->handler_len <= 0)
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try clause len <= 0 at 0x%04x", clause->try_offset));

		if (clause->try_offset < clause->handler_offset && clause->try_offset + clause->try_len > HANDLER_START (clause))
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try block (at 0x%04x) includes handler block (at 0x%04x)", clause->try_offset, clause->handler_offset));

		for (n = i + 1; n < ctx.header->num_clauses && ctx.valid; ++n)
			verify_clause_relationship (&ctx, clause, ctx.header->clauses + n);

		if (!ctx.valid)
			break;

		ctx.code [clause->try_offset].flags |= IL_CODE_FLAG_WAS_TARGET;
		ctx.code [clause->try_offset + clause->try_len].flags |= IL_CODE_FLAG_WAS_TARGET;
		ctx.code [clause->handler_offset + clause->handler_len].flags |= IL_CODE_FLAG_WAS_TARGET;

		if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) {
			init_stack_with_value_at_exception_boundary (&ctx, ctx.code + clause->handler_offset, clause->data.catch_class);
		}
		else if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			init_stack_with_value_at_exception_boundary (&ctx, ctx.code + clause->data.filter_offset, mono_defaults.exception_class);
			init_stack_with_value_at_exception_boundary (&ctx, ctx.code + clause->handler_offset, mono_defaults.exception_class);	
		}
	}

	while (ip < end && ctx.valid) {
		ctx.ip_offset = ip_offset = ip - ctx.header->code;

		/*We need to check against fallthrou in and out of protected blocks.
		 * For fallout we check the once a protected block ends, if the start flag is not set.
		 * Likewise for fallthru in, we check if ip is the start of a protected block and start is not set
		 * TODO convert these checks to be done using flags and not this loop
		 */
		for (i = 0; i < ctx.header->num_clauses && ctx.valid; ++i) {
			MonoExceptionClause *clause = ctx.header->clauses + i;

			if ((clause->try_offset + clause->try_len == ip_offset) && start == 0) {
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("fallthru off try block at 0x%04x", ip_offset));
				start = 1;
			}

			if ((clause->handler_offset + clause->handler_len == ip_offset) && start == 0) {
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER)
					ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("fallout of handler block at 0x%04x", ip_offset));
				else
					CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("fallout of handler block at 0x%04x", ip_offset));
				start = 1;
			}

			if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER && clause->handler_offset == ip_offset && start == 0) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("fallout of filter block at 0x%04x", ip_offset));
				start = 1;
			}

			if (clause->handler_offset == ip_offset && start == 0) {
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("fallthru handler block at 0x%04x", ip_offset));
				start = 1;
			}

			if (clause->try_offset == ip_offset && ctx.eval.size > 0) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Try to enter try block with a non-empty stack at 0x%04x", ip_offset));
				start = 1;
			}
		}

		if (!ctx.valid)
			break;

		if (need_merge) {
			VERIFIER_DEBUG ( printf ("extra merge needed! 0x%04x \n", ctx.target); );
			merge_stacks (&ctx, &ctx.eval, &ctx.code [ctx.target], FALSE, TRUE);
			need_merge = 0;	
		}
		merge_stacks (&ctx, &ctx.eval, &ctx.code[ip_offset], start, FALSE);
		start = 0;

		/*TODO we can fast detect a forward branch or exception block targeting code after prefix, we should fail fast*/
		if (prefix) {
			prefix_list |= prefix;
		} else {
			prefix_list = 0;
			ctx.code [ip_offset].flags |= IL_CODE_FLAG_SEEN;
		}
		prefix = 0;

#ifdef MONO_VERIFIER_DEBUG
		{
			char *discode;
			discode = mono_disasm_code_one (NULL, method, ip, NULL);
			discode [strlen (discode) - 1] = 0; /* no \n */
			g_print ("[%d] %-29s (%d)\n",  ip_offset, discode, ctx.eval.size);
			g_free (discode);
		}
		dump_stack_state (&ctx.code [ip_offset]);
		dump_stack_state (&ctx.eval);
#endif

		switch (*ip) {
		case CEE_NOP:
		case CEE_BREAK:
			++ip;
			break;

		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
			push_arg (&ctx, *ip - CEE_LDARG_0, FALSE);
			++ip;
			break;

		case CEE_LDARG_S:
		case CEE_LDARGA_S:
			push_arg (&ctx, ip [1],  *ip == CEE_LDARGA_S);
			ip += 2;
			break;

		case CEE_ADD_OVF_UN:
			do_binop (&ctx, *ip, add_ovf_un_table);
			++ip;
			break;

		case CEE_SUB_OVF_UN:
			do_binop (&ctx, *ip, sub_ovf_un_table);
			++ip;
			break;

		case CEE_ADD_OVF:
		case CEE_SUB_OVF:
		case CEE_MUL_OVF:
		case CEE_MUL_OVF_UN:
			do_binop (&ctx, *ip, bin_ovf_table);
			++ip;
			break;

		case CEE_ADD:
			do_binop (&ctx, *ip, add_table);
			++ip;
			break;

		case CEE_SUB:
			do_binop (&ctx, *ip, sub_table);
			++ip;
			break;

		case CEE_MUL:
		case CEE_DIV:
		case CEE_REM:
			do_binop (&ctx, *ip, bin_op_table);
			++ip;
			break;

		case CEE_AND:
		case CEE_DIV_UN:
		case CEE_OR:
		case CEE_REM_UN:
		case CEE_XOR:
			do_binop (&ctx, *ip, int_bin_op_table);
			++ip;
			break;

		case CEE_SHL:
		case CEE_SHR:
		case CEE_SHR_UN:
			do_binop (&ctx, *ip, shift_op_table);
			++ip;
			break;

		case CEE_POP:
			if (!check_underflow (&ctx, 1))
				break;
			stack_pop (&ctx);
			++ip;
			break;

		case CEE_RET:
			do_ret (&ctx);
			++ip;
			start = 1;
			break;

		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			/*TODO support definite assignment verification? */
			push_local (&ctx, *ip - CEE_LDLOC_0, FALSE);
			++ip;
			break;

		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3:
			store_local (&ctx, *ip - CEE_STLOC_0);
			++ip;
			break;

		case CEE_STLOC_S:
			store_local (&ctx, ip [1]);
			ip += 2;
			break;

		case CEE_STARG_S:
			store_arg (&ctx, ip [1]);
			ip += 2;
			break;

		case CEE_LDC_I4_M1:
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_I4, &mono_defaults.int32_class->byval_arg);
			++ip;
			break;

		case CEE_LDC_I4_S:
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_I4, &mono_defaults.int32_class->byval_arg);
			ip += 2;
			break;

		case CEE_LDC_I4:
			if (check_overflow (&ctx))
				stack_push_val (&ctx,TYPE_I4, &mono_defaults.int32_class->byval_arg);
			ip += 5;
			break;

		case CEE_LDC_I8:
			if (check_overflow (&ctx))
				stack_push_val (&ctx,TYPE_I8, &mono_defaults.int64_class->byval_arg);
			ip += 9;
			break;

		case CEE_LDC_R4:
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_R8, &mono_defaults.double_class->byval_arg);
			ip += 5;
			break;

		case CEE_LDC_R8:
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_R8, &mono_defaults.double_class->byval_arg);
			ip += 9;
			break;

		case CEE_LDNULL:
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_COMPLEX | NULL_LITERAL_MASK, &mono_defaults.object_class->byval_arg);
			++ip;
			break;

		case CEE_BEQ_S:
		case CEE_BNE_UN_S:
			do_branch_op (&ctx, (signed char)ip [1] + 2, cmp_br_eq_op);
			ip += 2;
			need_merge = 1;
			break;

		case CEE_BGE_S:
		case CEE_BGT_S:
		case CEE_BLE_S:
		case CEE_BLT_S:
		case CEE_BGE_UN_S:
		case CEE_BGT_UN_S:
		case CEE_BLE_UN_S:
		case CEE_BLT_UN_S:
			do_branch_op (&ctx, (signed char)ip [1] + 2, cmp_br_op);
			ip += 2;
			need_merge = 1;
			break;

		case CEE_BEQ:
		case CEE_BNE_UN:
			do_branch_op (&ctx, (gint32)read32 (ip + 1) + 5, cmp_br_eq_op);
			ip += 5;
			need_merge = 1;
			break;

		case CEE_BGE:
		case CEE_BGT:
		case CEE_BLE:
		case CEE_BLT:
		case CEE_BGE_UN:
		case CEE_BGT_UN:
		case CEE_BLE_UN:
		case CEE_BLT_UN:
			do_branch_op (&ctx, (gint32)read32 (ip + 1) + 5, cmp_br_op);
			ip += 5;
			need_merge = 1;
			break;

		case CEE_LDLOC_S:
		case CEE_LDLOCA_S:
			push_local (&ctx, ip[1], *ip == CEE_LDLOCA_S);
			ip += 2;
			break;

		/* FIXME: warn/error instead? */
		case CEE_UNUSED99:
			++ip;
			break; 

		case CEE_DUP: {
			ILStackDesc * top;
			if (!check_underflow (&ctx, 1))
				break;
			if (!check_overflow (&ctx))
				break;
			top = stack_push (&ctx);
			copy_stack_value (top, stack_get (&ctx, 1)); 
			++ip;
			break;
		}

		case CEE_JMP:
			if (ctx.eval.size)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Eval stack must be empty in jmp at 0x%04x", ip_offset));
			token = read32 (ip + 1);
			if (in_any_block (ctx.header, ip_offset))
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("jmp cannot escape exception blocks at 0x%04x", ip_offset));
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_CALL:
		case CEE_CALLVIRT:
			do_invoke_method (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_CALLI:
			token = read32 (ip + 1);
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_BR_S:
			do_static_branch (&ctx, (signed char)ip [1] + 2);
			need_merge = 1;
			ip += 2;
			start = 1;
			break;

		case CEE_BRFALSE_S:
		case CEE_BRTRUE_S:
			do_boolean_branch_op (&ctx, (signed char)ip [1] + 2);
			ip += 2;
			need_merge = 1;
			break;

		case CEE_BR:
			do_static_branch (&ctx, (gint32)read32 (ip + 1) + 5);
			need_merge = 1;
			ip += 5;
			start = 1;
			break;

		case CEE_BRFALSE:
		case CEE_BRTRUE:
			do_boolean_branch_op (&ctx, (gint32)read32 (ip + 1) + 5);
			ip += 5;
			need_merge = 1;
			break;

		case CEE_SWITCH:
			n = read32 (ip + 1);
			do_switch (&ctx, n, (ip + 5));
			start = 1;
			ip += 5 + sizeof (guint32) * n;
			break;

		case CEE_LDIND_I1:
		case CEE_LDIND_U1:
		case CEE_LDIND_I2:
		case CEE_LDIND_U2:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
		case CEE_LDIND_I8:
		case CEE_LDIND_I:
		case CEE_LDIND_R4:
		case CEE_LDIND_R8:
		case CEE_LDIND_REF:
			do_load_indirect (&ctx, *ip);
			++ip;
			break;
			
		case CEE_STIND_REF:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
		case CEE_STIND_I:
			do_store_indirect (&ctx, *ip);
			++ip;
			break;

		case CEE_NOT:
		case CEE_NEG:
			do_unary_math_op (&ctx, *ip);
			++ip;
			break;

		//TODO: implement proper typecheck
		case CEE_CONV_I1:
		case CEE_CONV_I2:
		case CEE_CONV_I4:
		case CEE_CONV_U1:
		case CEE_CONV_U2:
		case CEE_CONV_U4:
			do_conversion (&ctx, TYPE_I4);
			++ip;
			break;			

		case CEE_CONV_I8:
		case CEE_CONV_U8:
			do_conversion (&ctx, TYPE_I8);
			++ip;
			break;			

		case CEE_CONV_R4:
		case CEE_CONV_R8:
		case CEE_CONV_R_UN:
			do_conversion (&ctx, TYPE_R8);
			++ip;
			break;			

		case CEE_CONV_I:
		case CEE_CONV_U:
			do_conversion (&ctx, TYPE_NATIVE_INT);
			++ip;
			break;

		case CEE_CPOBJ:
			do_cpobj (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDOBJ:
			do_ldobj_value (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDSTR:
			do_ldstr (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_NEWOBJ:
			do_newobj (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_CASTCLASS:
		case CEE_ISINST:
			do_cast (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_UNUSED58:
		case CEE_UNUSED1:
			++ip; /* warn, error ? */
			break;
		case CEE_UNBOX:
			do_unbox_value (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_THROW:
			do_throw (&ctx);
			start = 1;
			++ip;
			break;

		case CEE_LDFLD:
		case CEE_LDFLDA:
			do_push_field (&ctx, read32 (ip + 1), *ip == CEE_LDFLDA);
			ip += 5;
			break;

		case CEE_LDSFLD:
		case CEE_LDSFLDA:
			do_push_static_field (&ctx, read32 (ip + 1), *ip == CEE_LDSFLDA);
			ip += 5;
			break;

		case CEE_STFLD:
			do_store_field (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_STSFLD:
			do_store_static_field (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_STOBJ:
			do_stobj (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_CONV_OVF_I1_UN:
		case CEE_CONV_OVF_I2_UN:
		case CEE_CONV_OVF_I4_UN:
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U4_UN:
			do_conversion (&ctx, TYPE_I4);
			++ip;
			break;			

		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U8_UN:
			do_conversion (&ctx, TYPE_I8);
			++ip;
			break;			

		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
			do_conversion (&ctx, TYPE_NATIVE_INT);
			++ip;
			break;

		case CEE_BOX:
			do_box_value (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_NEWARR:
			do_newarr (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDLEN:
			do_ldlen (&ctx);
			++ip;
			break;

		case CEE_LDELEMA:
			do_ldelema (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDELEM_I1:
		case CEE_LDELEM_U1:
		case CEE_LDELEM_I2:
		case CEE_LDELEM_U2:
		case CEE_LDELEM_I4:
		case CEE_LDELEM_U4:
		case CEE_LDELEM_I8:
		case CEE_LDELEM_I:
		case CEE_LDELEM_R4:
		case CEE_LDELEM_R8:
		case CEE_LDELEM_REF:
			do_ldelem (&ctx, *ip, 0);
			++ip;
			break;

		case CEE_STELEM_I:
		case CEE_STELEM_I1:
		case CEE_STELEM_I2:
		case CEE_STELEM_I4:
		case CEE_STELEM_I8:
		case CEE_STELEM_R4:
		case CEE_STELEM_R8:
		case CEE_STELEM_REF:
			do_stelem (&ctx, *ip, 0);
			++ip;
			break;

		case CEE_LDELEM_ANY:
			do_ldelem (&ctx, *ip, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_STELEM_ANY:
			do_stelem (&ctx, *ip, read32 (ip + 1));
			ip += 5;
			break;
			
		case CEE_UNBOX_ANY:
			do_unbox_any (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_UNUSED5:
		case CEE_UNUSED6:
		case CEE_UNUSED7:
		case CEE_UNUSED8:
		case CEE_UNUSED9:
		case CEE_UNUSED10:
		case CEE_UNUSED11:
		case CEE_UNUSED12:
		case CEE_UNUSED13:
		case CEE_UNUSED14:
		case CEE_UNUSED15:
		case CEE_UNUSED16:
		case CEE_UNUSED17:
			++ip; /* warn, error ? */
			break;

		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_U2:
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_U4:
			do_conversion (&ctx, TYPE_I4);
			++ip;
			break;

		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
			do_conversion (&ctx, TYPE_I8);
			++ip;
			break;

		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_U:
			do_conversion (&ctx, TYPE_NATIVE_INT);
			++ip;
			break;

		case CEE_UNUSED50:
		case CEE_UNUSED18:
		case CEE_UNUSED19:
		case CEE_UNUSED20:
		case CEE_UNUSED21:
		case CEE_UNUSED22:
		case CEE_UNUSED23:
			++ip; /* warn, error ? */
			break;
		case CEE_REFANYVAL:
			if (!check_underflow (&ctx, 1))
				break;
			++ip;
			break;
		case CEE_CKFINITE:
			if (!check_underflow (&ctx, 1))
				break;
			++ip;
			break;
		case CEE_UNUSED24:
		case CEE_UNUSED25:
			++ip; /* warn, error ? */
			break;
		case CEE_MKREFANY:
			if (!check_underflow (&ctx, 1))
				break;
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_UNUSED59:
		case CEE_UNUSED60:
		case CEE_UNUSED61:
		case CEE_UNUSED62:
		case CEE_UNUSED63:
		case CEE_UNUSED64:
		case CEE_UNUSED65:
		case CEE_UNUSED66:
		case CEE_UNUSED67:
			++ip; /* warn, error ? */
			break;
		case CEE_LDTOKEN:
			do_load_token (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_ENDFINALLY:
			if (!is_correct_endfinally (ctx.header, ip_offset))
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("endfinally must be used inside a finally/fault handler at 0x%04x", ctx.ip_offset));
			ctx.eval.size = 0;
			start = 1;
			++ip;
			break;

		case CEE_LEAVE:
			do_leave (&ctx, read32 (ip + 1) + 5);
			ip += 5;
			start = 1;
			break;

		case CEE_LEAVE_S:
			do_leave (&ctx, (signed char)ip [1] + 2);
			ip += 2;
			start = 1;
			break;
			
		case CEE_UNUSED26:
		case CEE_UNUSED27:
		case CEE_UNUSED28:
		case CEE_UNUSED29:
		case CEE_UNUSED30:
		case CEE_UNUSED31:
		case CEE_UNUSED32:
		case CEE_UNUSED33:
		case CEE_UNUSED34:
		case CEE_UNUSED35:
		case CEE_UNUSED36:
		case CEE_UNUSED37:
		case CEE_UNUSED38:
		case CEE_UNUSED39:
		case CEE_UNUSED40:
		case CEE_UNUSED41:
		case CEE_UNUSED42:
		case CEE_UNUSED43:
		case CEE_UNUSED44:
		case CEE_UNUSED45:
		case CEE_UNUSED46:
		case CEE_UNUSED47:
		case CEE_UNUSED48:
			++ip;
			break;
		case CEE_PREFIX7:
		case CEE_PREFIX6:
		case CEE_PREFIX5:
		case CEE_PREFIX4:
		case CEE_PREFIX3:
		case CEE_PREFIX2:
		case CEE_PREFIXREF:
			++ip;
			break;
		case CEE_PREFIX1:
			++ip;
			switch (*ip) {
			case CEE_STLOC:
				store_local (&ctx, read16 (ip + 1));
				ip += 3;
				break;

			case CEE_CEQ:
				do_cmp_op (&ctx, cmp_br_eq_op, *ip);
				++ip;
				break;

			case CEE_CGT:
			case CEE_CGT_UN:
			case CEE_CLT:
			case CEE_CLT_UN:
				do_cmp_op (&ctx, cmp_br_op, *ip);
				++ip;
				break;

			case CEE_STARG:
				store_arg (&ctx, read16 (ip + 1) );
				ip += 3;
				break;


			case CEE_ARGLIST:
				check_overflow (&ctx);
				++ip;
	
			case CEE_LDFTN:
				do_load_function_ptr (&ctx, read32 (ip + 1), FALSE);
				ip += 5;
				break;

			case CEE_LDVIRTFTN:
				do_load_function_ptr (&ctx, read32 (ip + 1), TRUE);
				ip += 5;
				break;

			case CEE_UNUSED56:
				++ip;
				break;

			case CEE_LDARG:
			case CEE_LDARGA:
				push_arg (&ctx, read16 (ip + 1),  *ip == CEE_LDARGA);
				ip += 3;
				break;

			case CEE_LDLOC:
			case CEE_LDLOCA:
				push_local (&ctx, read16 (ip + 1), *ip == CEE_LDLOCA);
				ip += 3;
				break;

			case CEE_LOCALLOC:
				do_localloc (&ctx);
				++ip;
				break;

			case CEE_UNUSED57:
				++ip;
				break;
			case CEE_ENDFILTER:
				do_endfilter (&ctx);
				start = 1;
				++ip;
				break;
			case CEE_UNALIGNED_:
				prefix |= PREFIX_UNALIGNED;
				ip += 2;
				break;
			case CEE_VOLATILE_:
				prefix |= PREFIX_VOLATILE;
				++ip;
				break;
			case CEE_TAIL_:
				prefix |= PREFIX_TAIL;
				++ip;
				if (ip < end && (*ip != CEE_CALL && *ip != CEE_CALLI && *ip != CEE_CALLVIRT))
					ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("tail prefix must be used only with call opcodes at 0x%04x", ip_offset));
				break;

			case CEE_INITOBJ:
				do_initobj (&ctx, read32 (ip + 1));
				ip += 5;
				break;

			case CEE_CONSTRAINED_:
				token = read32 (ip + 1);
				ip += 5;
				break;
			case CEE_CPBLK:
				if (!check_underflow (&ctx, 3))
					break;
				ip++;
				break;
			case CEE_INITBLK:
				if (!check_underflow (&ctx, 3))
					break;
				ip++;
				break;
			case CEE_NO_:
				ip += 2;
				break;
			case CEE_RETHROW:
				if (!is_correct_rethrow (ctx.header, ip_offset))
					ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("rethrow must be used inside a catch handler at 0x%04x", ctx.ip_offset));
				ctx.eval.size = 0;
				++ip;
				break;
			case CEE_UNUSED:
				++ip;
				break;

			case CEE_SIZEOF:
				do_sizeof (&ctx, read32 (ip + 1));
				ip += 5;
				break;

			case CEE_REFANYTYPE:
				if (!check_underflow (&ctx, 1))
					break;
				++ip;
				break;
			case CEE_UNUSED53:
			case CEE_UNUSED54:
			case CEE_UNUSED55:
			case CEE_UNUSED70:
				++ip;
				break;
			}
		}
	}
	/*
	 * if ip != end we overflowed: mark as error.
	 */
	if ((ip != end || !start) && ctx.verifiable && !ctx.list) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Run ahead of method code at 0x%04x", ip_offset));
	}

	/*We should guard against the last decoded opcode, otherwise we might add errors that doesn't make sense.*/
	for (i = 0; i < ctx.code_size && i < ip_offset; ++i) {
		if (ctx.code [i].flags & IL_CODE_FLAG_WAS_TARGET) {
			if (!(ctx.code [i].flags & IL_CODE_FLAG_SEEN))
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Branch or exception block target middle of intruction at 0x%04x", i));

			if (ctx.code [i].flags & IL_CODE_DELEGATE_SEQUENCE)
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Branch to delegate code sequence at 0x%04x", i));
		}
		if ((ctx.code [i].flags & IL_CODE_LDFTN_DELEGATE_NONFINAL_VIRTUAL) && ctx.has_this_store)
			CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Invalid ldftn with virtual function in method with stdarg 0 at  0x%04x", i));
	}

	if (ctx.code) {
		for (i = 0; i < ctx.header->code_size; ++i) {
			if (ctx.code [i].stack)
				g_free (ctx.code [i].stack);
		}
	}

	for (tmp = ctx.funptrs; tmp; tmp = tmp->next)
		g_free (tmp->data);
	g_slist_free (ctx.funptrs);

	if (ctx.eval.stack)
		g_free (ctx.eval.stack);
	if (ctx.code)
		g_free (ctx.code);
	if (ctx.signature->hasthis)
		g_free (ctx.params);

	return ctx.list;
}

char*
mono_verify_corlib ()
{
	/* This is a public API function so cannot be removed */
	return NULL;
}


