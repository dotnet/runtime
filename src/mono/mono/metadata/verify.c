
#include <mono/metadata/object.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-endian.h>
#include <string.h>
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

void
mono_free_verify_list (GSList *list)
{
	MonoVerifyInfo* info;
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

static const char*
valid_cultures[] = {
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
	"kok-IN", "syr-SY", "div-MV",
	NULL
};

static int
is_valid_culture (const char *cname)
{
	int i;
	int found;
			
	found = *cname == 0;
	for (i = 0; !found && valid_cultures [i]; ++i) {
		if (g_strcasecmp (valid_cultures [i], cname))
			found = 1;
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
is_valid_blob (MonoImage *image, guint32 index, int notnull)
{
	guint32 size;
	const char *p, *send;
	
	if (index >= image->heap_blob.size)
		return 0;
	p = mono_metadata_blob_heap (image, index);
	size = mono_metadata_decode_blob_size (p, &send);
	if (index + size + (send-p) >= image->heap_blob.size)
		return 0;
	if (notnull && !size)
		return 0;
	return 1;
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
		
		if (cols [MONO_ASSEMBLY_NAME] >= image->heap_strings.size) {
			ADD_ERROR (list, g_strdup ("Assembly name is an invalid index"));
		} else {
			p = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_NAME]);
			if (!*p || strpbrk (p, ":\\/."))
				ADD_ERROR (list, g_strdup_printf ("Assembly name `%s' contains invalid chars", p));
		}

		if (cols [MONO_ASSEMBLY_CULTURE] >= image->heap_strings.size) {
			ADD_ERROR (list, g_strdup ("Assembly culture is an invalid index"));
		} else {
			p = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_CULTURE]);
			
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
		
			if (cols [MONO_ASSEMBLYREF_CULTURE] >= image->heap_strings.size) {
				ADD_ERROR (list, g_strdup_printf ("AssemblyRef culture in row %d is an invalid index", i + 1));
			} else {
				p = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_CULTURE]);
			
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
	const char *p;
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
				 * We need to check that the parent types have the samme layout 
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
	const char *p;
	guint32 value, i;
	GHashTable *dups = g_hash_table_new (g_direct_hash, g_direct_equal);
	
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
			value = cols [MONO_CONSTANT_PARENT] >> HASCONSTANT_BITS;
			switch (cols [MONO_CONSTANT_PARENT] & HASCONSTANT_MASK) {
			case HASCONSTANT_FIEDDEF:
				if (value > image->tables [MONO_TABLE_FIELD].rows)
					ADD_ERROR (list, g_strdup_printf ("Parent (field) is invalid in Constant row %d", i + 1));
				break;
			case HASCONSTANT_PARAM:
				if (value > image->tables [MONO_TABLE_PARAM].rows)
					ADD_ERROR (list, g_strdup_printf ("Parent (param) is invalid in Constant row %d", i + 1));
				break;
			case HASCONSTANT_PROPERTY:
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
	const char *p;
	guint32 value, i, last_event;
	GHashTable *dups = g_hash_table_new (g_direct_hash, g_direct_equal);

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
	guint32 value, i, last_event;
	
	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_EVENT_SIZE);

		if (cols [MONO_EVENT_FLAGS] & ~(EVENT_SPECIALNAME|EVENT_RTSPECIALNAME)) {
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Flags 0x%04x invalid in Event row %d", cols [MONO_EVENT_FLAGS], i + 1));
		}
		if (cols [MONO_EVENT_NAME] > image->heap_strings.size || !cols [MONO_EVENT_NAME]) {
			if (level & MONO_VERIFY_ERROR)
				ADD_ERROR (list, g_strdup_printf ("Invalid name in Event row %d", i + 1));
		} else {
			if (level & MONO_VERIFY_CLS) {
				p = mono_metadata_string_heap (image, cols [MONO_EVENT_NAME]);
				if (!is_valid_cls_ident (p))
					ADD_WARN (list, MONO_VERIFY_CLS, g_strdup_printf ("Invalid CLS name '%s` in Event row %d", p, i + 1));
			}
		}
		
		if (level & MONO_VERIFY_ERROR && cols [MONO_EVENT_TYPE]) {
			value = cols [MONO_EVENT_TYPE] >> TYPEDEFORREF_BITS;
			switch (cols [MONO_EVENT_TYPE] & TYPEDEFORREF_MASK) {
			case TYPEDEFORREF_TYPEDEF:
				if (!value || value > image->tables [MONO_TABLE_TYPEDEF].rows)
					ADD_ERROR (list, g_strdup_printf ("Type invalid in Event row %d", i + 1));
				break;
			case TYPEDEFORREF_TYPEREF:
				if (!value || value > image->tables [MONO_TABLE_TYPEREF].rows)
					ADD_ERROR (list, g_strdup_printf ("Type invalid in Event row %d", i + 1));
				break;
			case TYPEDEFORREF_TYPESPEC:
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

	return g_slist_reverse (error_list);
}

enum {
	TYPE_INVALID = 0, /* leave at 0. */
	TYPE_INT32 = 1,
	TYPE_INT64 = 2,
	TYPE_NINT  = 3,
	TYPE_FLOAT = 4,
	TYPE_MANP = 5,
	TYPE_OBJREF = 6,
	TYPE_MAX = 7
};

const static unsigned char 
valid_binops [TYPE_MAX] [TYPE_MAX] = {
	{TYPE_INVALID},
	{TYPE_INVALID, TYPE_INT32,   TYPE_INVALID, TYPE_NINT,    TYPE_INVALID, TYPE_MANP},
	{TYPE_INVALID, TYPE_INVALID, TYPE_INT64,   TYPE_INVALID},
	{TYPE_INVALID, TYPE_NINT,    TYPE_INVALID, TYPE_NINT,    TYPE_INVALID, TYPE_MANP},
	{TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_FLOAT},
	{TYPE_INVALID, TYPE_MANP,    TYPE_INVALID, TYPE_MANP,    TYPE_INVALID, TYPE_NINT},
	{TYPE_INVALID}
	               /* int32 */   /* int64 */   /* native */  /* float */   /* managed p */ /* objref */
};

const static unsigned char 
valid_unnops [TYPE_MAX] = {
	TYPE_INVALID, TYPE_INT32,    TYPE_INT64,   TYPE_NINT,    TYPE_FLOAT,   TYPE_INVALID
	               /* int32 */   /* int64 */   /* native */  /* float */   /* managed p */ /* objref */
};

/* note: the resulting type is always a boolean */
const static unsigned char 
valid_bincomp [TYPE_MAX] [TYPE_MAX] = {
	{TYPE_INVALID},
	{TYPE_INVALID, TYPE_INT32,   TYPE_INVALID, TYPE_INT32,   TYPE_INVALID},
	{TYPE_INVALID, TYPE_INVALID, TYPE_INT32,   TYPE_INVALID},
	{TYPE_INVALID, TYPE_INT32,   TYPE_INVALID, TYPE_INT32,   TYPE_INVALID, TYPE_INT32},
	{TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INT32},
	{TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INT32,   TYPE_INVALID, TYPE_INT32},
	{TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INVALID, TYPE_INT32}
	               /* int32 */   /* int64 */   /* native */  /* float */   /* managed p */ /* objref */
};

const static unsigned char 
valid_intops [TYPE_MAX] [TYPE_MAX] = {
	{TYPE_INVALID},
	{TYPE_INVALID, TYPE_INT32,   TYPE_INVALID, TYPE_NINT},
	{TYPE_INVALID, TYPE_INVALID, TYPE_INT64},
	{TYPE_INVALID, TYPE_NINT,    TYPE_INVALID, TYPE_NINT},
	{TYPE_INVALID}
};

const static unsigned char 
valid_shiftops [TYPE_MAX] [TYPE_MAX] = {
	{TYPE_INVALID},
	{TYPE_INVALID, TYPE_INT32,   TYPE_INVALID, TYPE_INT32},
	{TYPE_INVALID, TYPE_INT64,   TYPE_INVALID, TYPE_INT64},
	{TYPE_INVALID, TYPE_NINT,    TYPE_INVALID, TYPE_NINT},
	{TYPE_INVALID}
};

#define ADD_INVALID(list,msg)	\
	do {	\
		MonoVerifyInfo *vinfo = g_new (MonoVerifyInfo, 1);	\
		vinfo->status = MONO_VERIFY_ERROR;	\
		vinfo->message = (msg);	\
		(list) = g_slist_prepend ((list), vinfo);	\
		goto invalid_cil;	\
	} while (0)

#define CHECK_STACK_UNDEFLOW(num)	\
	do {	\
		if (cur_stack < (num))	\
			ADD_INVALID (list, g_strdup_printf ("Stack underflow at 0x%04x", ip - header->code));	\
	} while (0)

#define CHECK_STACK_OVERFLOW()	\
	do {	\
		if (cur_stack >= max_stack)	\
			ADD_INVALID (list, g_strdup_printf ("Maxstack exceeded at 0x%04x", ip - header->code));	\
	} while (0)

/*
 * FIXME: need to distinguish between valid and verifiable.
 * Need to keep track of types on the stack.
 * Verify types for opcodes.
 */
GSList*
mono_method_verify (MonoMethod *method, int level)
{
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	register const unsigned char *ip;
	register const unsigned char *end;
	const unsigned char *target; /* branch target */
	int max_args, max_stack, cur_stack, i, n;
	guint32 token;
	char *local_state = NULL;
	GSList *list = NULL;

	signature = method->signature;
	header = ((MonoMethodNormal *)method)->header;
	ip = header->code;
	end = ip + header->code_size;
	max_args = method->signature->param_count + method->signature->hasthis;
	max_stack = header->max_stack;
	cur_stack = 0;

	if (header->num_locals) {
		local_state = g_new (char, header->num_locals);
		memset (local_state, header->init_locals, header->num_locals);
	}

	while (ip < end) {
		switch (*ip) {
		case CEE_NOP:
		case CEE_BREAK: 
			++ip;
			break;
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
			if (*ip - CEE_LDARG_0 >= max_args)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", *ip - CEE_LDARG_0, ip - header->code));
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			++ip;
			break;
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			if (*ip - CEE_LDLOC_0 >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", *ip - CEE_LDLOC_0, ip - header->code));
			if (!local_state [*ip - CEE_LDLOC_0])
				ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", *ip - CEE_LDLOC_0, ip - header->code));
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			++ip;
			break;
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3:
			if (*ip - CEE_STLOC_0 >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", *ip - CEE_STLOC_0, ip - header->code));
			local_state [*ip - CEE_STLOC_0] = 1;
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			++ip;
			break;
		case CEE_LDARG_S:
		case CEE_LDARGA_S:
			if (ip [1] >= max_args)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", ip [1], ip - header->code));
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 2;
			break;
		case CEE_STARG_S:
			if (ip [1] >= max_args)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", ip [1], ip - header->code));
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			ip += 2;
			break;
		case CEE_LDLOC_S:
		case CEE_LDLOCA_S:
			if (ip [1] >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", ip [1], ip - header->code));
			/* no need to check if the var is initialized if the address is taken */
			if (*ip == CEE_LDLOC_S && !local_state [ip [1]])
				ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", ip [1], ip - header->code));
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 2;
			break;
		case CEE_STLOC_S:
			if (ip [1] >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", ip [1], ip - header->code));
			local_state [ip [1]] = 1;
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			ip += 2;
			break;
		case CEE_LDNULL:
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			++ip;
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
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			++ip;
			break;
		case CEE_LDC_I4_S:
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 2;
			break;
		case CEE_LDC_I4:
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 5;
			break;
		case CEE_LDC_I8:
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 9;
			break;
		case CEE_LDC_R4:
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 5;
			break;
		case CEE_LDC_R8:
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 9;
			break;
		case CEE_UNUSED99: ++ip; break; /* warn/error instead? */
		case CEE_DUP:
			CHECK_STACK_UNDEFLOW (1);
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			++ip;
			break;
		case CEE_POP:
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			++ip;
			break;
		case CEE_JMP:
			++ip;
			break;
		case CEE_CALL:
			token = read32 (ip + 1);
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_CALLI:
			token = read32 (ip + 1);
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_RET:
			if (signature->ret->type != MONO_TYPE_VOID) {
				if (cur_stack != 1)
					ADD_INVALID (list, g_strdup_printf ("Stack not empty after ret at 0x%04x", ip - header->code));
				--cur_stack;
			} else {
				if (cur_stack)
					ADD_INVALID (list, g_strdup_printf ("Stack not empty after ret at 0x%04x", ip - header->code));
				cur_stack = 0;
			}
			++ip;
			break;
		case CEE_BR_S:
			target = ip + (signed char)ip [1] + 2;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip - header->code));
			ip += 2;
			break;
		case CEE_BRFALSE_S:
		case CEE_BRTRUE_S:
			target = ip + (signed char)ip [1] + 2;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip - header->code));
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			ip += 2;
			break;
		case CEE_BEQ_S:
		case CEE_BGE_S:
		case CEE_BGT_S:
		case CEE_BLE_S:
		case CEE_BLT_S:
		case CEE_BNE_UN_S:
		case CEE_BGE_UN_S:
		case CEE_BGT_UN_S:
		case CEE_BLE_UN_S:
		case CEE_BLT_UN_S:
			target = ip + (signed char)ip [1] + 2;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip - header->code));
			CHECK_STACK_UNDEFLOW (2);
			cur_stack -= 2;
			ip += 2;
			break;
		case CEE_BR:
			target = ip + (gint32)read32 (ip + 1) + 5;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip - header->code));
			ip += 5;
			break;
		case CEE_BRFALSE:
		case CEE_BRTRUE:
			target = ip + (gint32)read32 (ip + 1) + 5;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip - header->code));
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			ip += 5;
			break;
		case CEE_BEQ:
		case CEE_BGE:
		case CEE_BGT:
		case CEE_BLE:
		case CEE_BLT:
		case CEE_BNE_UN:
		case CEE_BGE_UN:
		case CEE_BGT_UN:
		case CEE_BLE_UN:
		case CEE_BLT_UN:
			target = ip + (gint32)read32 (ip + 1) + 5;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip - header->code));
			CHECK_STACK_UNDEFLOW (2);
			cur_stack -= 2;
			ip += 5;
			break;
		case CEE_SWITCH:
			n = read32 (ip + 1);
			target = ip + sizeof (guint32) * n;
			/* FIXME: check that ip is in range */
			for (i = 0; i < n; ++i)
				if (target + (gint32) read32 (ip + 5 + i * sizeof (gint32)) >= end || target + (gint32) read32 (ip + 5 + i * sizeof (gint32)) < header->code)
					ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip - header->code));
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
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
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			++ip;
			break;
		case CEE_STIND_REF:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			++ip;
			break;
		case CEE_ADD:
		case CEE_SUB:
		case CEE_MUL:
		case CEE_DIV:
		case CEE_DIV_UN:
		case CEE_REM:
		case CEE_REM_UN:
		case CEE_AND:
		case CEE_OR:
		case CEE_XOR:
		case CEE_SHL:
		case CEE_SHR:
		case CEE_SHR_UN:
			CHECK_STACK_UNDEFLOW (2);
			--cur_stack;
			++ip;
			break;
		case CEE_NEG:
		case CEE_NOT:
		case CEE_CONV_I1:
		case CEE_CONV_I2:
		case CEE_CONV_I4:
		case CEE_CONV_I8:
		case CEE_CONV_R4:
		case CEE_CONV_R8:
		case CEE_CONV_U4:
		case CEE_CONV_U8:
			CHECK_STACK_UNDEFLOW (1);
			++ip;
			break;
		case CEE_CALLVIRT:
			token = read32 (ip + 1);
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_CPOBJ:
			token = read32 (ip + 1);
			CHECK_STACK_UNDEFLOW (2);
			cur_stack -= 2;
			ip += 5;
			break;
		case CEE_LDOBJ:
			token = read32 (ip + 1);
			CHECK_STACK_UNDEFLOW (1);
			ip += 5;
			break;
		case CEE_LDSTR:
			token = read32 (ip + 1);
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 5;
			break;
		case CEE_NEWOBJ:
			token = read32 (ip + 1);
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_CASTCLASS:
		case CEE_ISINST:
			token = read32 (ip + 1);
			CHECK_STACK_UNDEFLOW (1);
			ip += 5;
			break;
		case CEE_CONV_R_UN:
			CHECK_STACK_UNDEFLOW (1);
			++ip;
			break;
		case CEE_UNUSED58:
		case CEE_UNUSED1:
			++ip; /* warn, error ? */
			break;
		case CEE_UNBOX:
			token = read32 (ip + 1);
			CHECK_STACK_UNDEFLOW (1);
			ip += 5;
			break;
		case CEE_THROW:
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			++ip;
			break;
		case CEE_LDFLD:
			CHECK_STACK_UNDEFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_LDFLDA:
			CHECK_STACK_UNDEFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_STFLD:
			CHECK_STACK_UNDEFLOW (2);
			cur_stack -= 2;
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_LDSFLD:
			CHECK_STACK_OVERFLOW ();
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_LDSFLDA:
			CHECK_STACK_OVERFLOW ();
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_STSFLD:
			CHECK_STACK_UNDEFLOW (1);
			--cur_stack;
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_STOBJ:
			CHECK_STACK_UNDEFLOW (2);
			cur_stack -= 2;
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_CONV_OVF_I1_UN:
		case CEE_CONV_OVF_I2_UN:
		case CEE_CONV_OVF_I4_UN:
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U4_UN:
		case CEE_CONV_OVF_U8_UN:
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
			CHECK_STACK_UNDEFLOW (1);
			++ip;
			break;
		case CEE_BOX:
			CHECK_STACK_UNDEFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_NEWARR:
			CHECK_STACK_UNDEFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_LDLEN:
			CHECK_STACK_UNDEFLOW (1);
			++ip;
			break;
		case CEE_LDELEMA:
			CHECK_STACK_UNDEFLOW (2);
			--cur_stack;
			token = read32 (ip + 1);
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
			CHECK_STACK_UNDEFLOW (2);
			--cur_stack;
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
			CHECK_STACK_UNDEFLOW (3);
			cur_stack -= 3;
			++ip;
			break;
		case CEE_UNUSED2:
		case CEE_UNUSED3:
		case CEE_UNUSED4:
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
		case CEE_CONV_OVF_I1: break;
		case CEE_CONV_OVF_U1: break;
		case CEE_CONV_OVF_I2: break;
		case CEE_CONV_OVF_U2: break;
		case CEE_CONV_OVF_I4: break;
		case CEE_CONV_OVF_U4: break;
		case CEE_CONV_OVF_I8: break;
		case CEE_CONV_OVF_U8: break;
		case CEE_UNUSED50:
		case CEE_UNUSED18:
		case CEE_UNUSED19:
		case CEE_UNUSED20:
		case CEE_UNUSED21:
		case CEE_UNUSED22:
		case CEE_UNUSED23:
			++ip; /* warn, error ? */
			break;
		case CEE_REFANYVAL: break;
		case CEE_CKFINITE: break;
		case CEE_UNUSED24:
		case CEE_UNUSED25:
			++ip; /* warn, error ? */
			break;
		case CEE_MKREFANY: break;
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
		case CEE_LDTOKEN: break;
		case CEE_CONV_U2: break;
		case CEE_CONV_U1: break;
		case CEE_CONV_I: break;
		case CEE_CONV_OVF_I: break;
		case CEE_CONV_OVF_U: break;
		case CEE_ADD_OVF: break;
		case CEE_ADD_OVF_UN: break;
		case CEE_MUL_OVF: break;
		case CEE_MUL_OVF_UN: break;
		case CEE_SUB_OVF: break;
		case CEE_SUB_OVF_UN: break;
		case CEE_ENDFINALLY: break;
		case CEE_LEAVE: break;
		case CEE_LEAVE_S: break;
		case CEE_STIND_I: break;
		case CEE_CONV_U: break;
		case CEE_UNUSED26: break;
		case CEE_UNUSED27: break;
		case CEE_UNUSED28: break;
		case CEE_UNUSED29: break;
		case CEE_UNUSED30: break;
		case CEE_UNUSED31: break;
		case CEE_UNUSED32: break;
		case CEE_UNUSED33: break;
		case CEE_UNUSED34: break;
		case CEE_UNUSED35: break;
		case CEE_UNUSED36: break;
		case CEE_UNUSED37: break;
		case CEE_UNUSED38: break;
		case CEE_UNUSED39: break;
		case CEE_UNUSED40: break;
		case CEE_UNUSED41: break;
		case CEE_UNUSED42: break;
		case CEE_UNUSED43: break;
		case CEE_UNUSED44: break;
		case CEE_UNUSED45: break;
		case CEE_UNUSED46: break;
		case CEE_UNUSED47: break;
		case CEE_UNUSED48: break;
		case CEE_PREFIX7: break;
		case CEE_PREFIX6: break;
		case CEE_PREFIX5: break;
		case CEE_PREFIX4: break;
		case CEE_PREFIX3: break;
		case CEE_PREFIX2: break;
		case CEE_PREFIXREF: break;
		case CEE_PREFIX1:
			++ip;
			switch (*ip) {
			case CEE_ARGLIST: break;
			case CEE_CEQ: break;
			case CEE_CGT: break;
			case CEE_CGT_UN: break;
			case CEE_CLT: break;
			case CEE_CLT_UN: break;
			case CEE_LDFTN: break;
			case CEE_LDVIRTFTN: break;
			case CEE_UNUSED56: break;
			case CEE_LDARG: break;
			case CEE_LDARGA: break;
			case CEE_STARG: break;
			case CEE_LDLOC: break;
			case CEE_LDLOCA: break;
			case CEE_STLOC: break;
			case CEE_LOCALLOC: break;
			case CEE_UNUSED57: break;
			case CEE_ENDFILTER: break;
			case CEE_UNALIGNED_: break;
			case CEE_VOLATILE_: break;
			case CEE_TAIL_: break;
			case CEE_INITOBJ:
				CHECK_STACK_UNDEFLOW (1);
				token = read32 (ip + 1);
				ip += 5;
			case CEE_UNUSED68: break;
			case CEE_CPBLK: break;
			case CEE_INITBLK: break;
			case CEE_UNUSED69:
				++ip;
				break;
			case CEE_RETHROW:
				++ip;
				break;
			case CEE_UNUSED:
				++ip;
				break;
			case CEE_SIZEOF:
				token = read32 (ip + 1);
				CHECK_STACK_OVERFLOW ();
				ip += 5;
				break;
			case CEE_REFANYTYPE: break;
			case CEE_UNUSED52:
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
	 * FIXME: if ip != end we overflowed: mark as error.
	 */
invalid_cil:

	g_free (local_state);
	return list;
}
