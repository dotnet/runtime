
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

static const char* const
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
			 * FIXME: check there is only ono owner in the respective table.
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

enum {
	TYPE_INV = 0, /* leave at 0. */
	TYPE_I4  = 1,
	TYPE_I8  = 2,
	TYPE_PTR = 3,
	TYPE_R8  = 4,
	TYPE_MP  = 5,
	TYPE_OBJ = 6,
	TYPE_VT  = 7,
	TYPE_MAX = 8
};

static const char* 
arg_name [TYPE_MAX] = {
	"Invalid",
	"Int32",
	"Int64",
	"IntPtr",
	"Double",
	"Managed Pointer",
	"ObjRef",
	"ValueType"
};

static const char
bin_num_table [TYPE_MAX] [TYPE_MAX] = {
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I4,  TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_MP,  TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_I8,  TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_MP,  TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_R8,  TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_MP,  TYPE_INV, TYPE_MP,  TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV}
};

static const char 
neg_table [] = {
	TYPE_INV, TYPE_I4, TYPE_I8, TYPE_PTR, TYPE_R8, TYPE_INV, TYPE_INV, TYPE_INV
};

/* reduce the size of this table */
static const char
bin_int_table [TYPE_MAX] [TYPE_MAX] = {
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I4,  TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_I8,  TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV}
};

static const char
bin_comp_table [TYPE_MAX] [TYPE_MAX] = {
	{0},
	{0, 1, 0, 1, 0, 0, 0, 0},
	{0, 0, 1, 0, 0, 0, 0, 0},
	{0, 1, 0, 1, 0, 2, 0, 0},
	{0, 0, 0, 0, 1, 0, 0, 0},
	{0, 0, 0, 2, 0, 1, 0, 0},
	{0, 0, 0, 0, 0, 0, 3, 0},
	{0, 0, 0, 0, 0, 0, 0, 0},
};

/* reduce the size of this table */
static const char
shift_table [TYPE_MAX] [TYPE_MAX] = {
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I4,  TYPE_INV, TYPE_I4,  TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8,  TYPE_INV, TYPE_I8,  TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_PTR, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV}
};

static const char 
ldind_type [] = {
	TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I8, TYPE_PTR, TYPE_R8, TYPE_R8, TYPE_OBJ
};

static const char
ldelem_type [] = {
	TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I4, TYPE_I8, TYPE_PTR, TYPE_R8, TYPE_R8, TYPE_OBJ
};

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

enum {
	PREFIX_UNALIGNED = 1,
	PREFIX_VOLATILE  = 2,
	PREFIX_TAIL      = 4,
	PREFIX_ADDR_MASK = 3,
	PREFIX_FUNC_MASK = 4
};

enum {
	CODE_SEEN = 1
};

typedef struct {
	MonoType *type;
	int stype;
} ILStackDesc;

typedef struct {
	ILStackDesc *stack;
	guint16 stack_count;
	guint16 flags;
} ILCodeDesc;

static void
type_to_eval_stack_type (MonoType *type, ILStackDesc *stack, int take_addr) {
	int t = type->type;

	stack->type = type;
	if (type->byref || take_addr) { /* fix double addr issue */
		stack->stype = TYPE_MP;
		return;
	}

handle_enum:
	switch (t) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		stack->stype = TYPE_I4;
		return;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
		stack->stype = TYPE_PTR;
		return;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		stack->stype = TYPE_OBJ;
		return;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		stack->stype = TYPE_I8;
		return;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		stack->stype = TYPE_R8;
		return;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			stack->stype = TYPE_VT;
			return;
		}
	default:
		g_error ("unknown type %02x in eval stack type", type->type);
	}
	return;
}

static int
type_from_op (int ins, ILStackDesc *arg) {
	switch (ins) {
	/* binops */
	case CEE_ADD:
	case CEE_SUB:
	case CEE_MUL:
	case CEE_DIV:
	case CEE_REM:
		/* FIXME: check unverifiable args for TYPE_MP */
		return arg->stype = bin_num_table [arg->stype] [arg [1].stype];
	case CEE_DIV_UN:
	case CEE_REM_UN:
	case CEE_AND:
	case CEE_OR:
	case CEE_XOR:
		return arg->stype = bin_int_table [arg->stype] [arg [1].stype];
	case CEE_SHL:
	case CEE_SHR:
	case CEE_SHR_UN:
		return arg->stype = shift_table [arg->stype] [arg [1].stype];
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
		/* FIXME: handle some specifics with ins->next->type */
		return bin_comp_table [arg->stype] [arg [1].stype] ? TYPE_I4: TYPE_INV;
	case 256+CEE_CEQ:
	case 256+CEE_CGT:
	case 256+CEE_CGT_UN:
	case 256+CEE_CLT:
	case 256+CEE_CLT_UN:
		return arg->stype = bin_comp_table [arg->stype] [arg [1].stype] ? TYPE_I4: TYPE_INV;
	/* unops */
	case CEE_NEG:
		return arg->stype = neg_table [arg->stype];
	case CEE_NOT:
		if (arg->stype >= TYPE_I4 && arg->stype <= TYPE_PTR)
			return arg->stype;
		else
			return arg->stype = TYPE_INV;
	case CEE_CONV_I1:
	case CEE_CONV_U1:
	case CEE_CONV_I2:
	case CEE_CONV_U2:
	case CEE_CONV_I4:
	case CEE_CONV_U4:
	case CEE_CONV_OVF_I1:
	case CEE_CONV_OVF_U1:
	case CEE_CONV_OVF_I2:
	case CEE_CONV_OVF_U2:
	case CEE_CONV_OVF_I4:
	case CEE_CONV_OVF_U4:
	case CEE_CONV_OVF_I1_UN:
	case CEE_CONV_OVF_U1_UN:
	case CEE_CONV_OVF_I2_UN:
	case CEE_CONV_OVF_U2_UN:
	case CEE_CONV_OVF_I4_UN:
	case CEE_CONV_OVF_U4_UN:
		if (arg->stype == TYPE_INV || arg->stype >= TYPE_MP)
			return arg->stype = TYPE_INV;
		return arg->stype = TYPE_I4;
	case CEE_CONV_I:
	case CEE_CONV_U:
	case CEE_CONV_OVF_I:
	case CEE_CONV_OVF_U:
	case CEE_CONV_OVF_I_UN:
	case CEE_CONV_OVF_U_UN:
		if (arg->stype == TYPE_INV || arg->stype == TYPE_VT)
			return arg->stype = TYPE_INV;
		return arg->stype = TYPE_PTR;
	case CEE_CONV_I8:
	case CEE_CONV_U8:
	case CEE_CONV_OVF_I8:
	case CEE_CONV_OVF_U8:
	case CEE_CONV_OVF_I8_UN:
	case CEE_CONV_OVF_U8_UN:
		return arg->stype = TYPE_I8;
	case CEE_CONV_R4:
	case CEE_CONV_R8:
		return arg->stype = TYPE_R8;
	default:
		g_error ("opcode 0x%04x not handled in type from op", ins);
		break;
	}
	return FALSE;
}

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
		/* need to check filter ... */
	}
	return 0;
}

static int
in_same_block (MonoMethodHeader *header, guint offset, guint target)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, offset) && !MONO_OFFSET_IN_CLAUSE (clause, target))
			return 0;
		if (MONO_OFFSET_IN_HANDLER (clause, offset) && !MONO_OFFSET_IN_HANDLER (clause, target))
			return 0;
		/* need to check filter ... */
	}
	return 1;
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
		/* need to check filter ... */
	}
	return 1;
}

static int
can_merge_stack (ILCodeDesc *a, ILCodeDesc *b)
{
	if (!b->flags & CODE_SEEN) {
		b->flags |= CODE_SEEN;
		b->stack_count = a->stack_count;
		/* merge types */
		return 1;
	}
	if (a->stack_count != b->stack_count)
		return 0;
	/* merge types */
	return 1;
}

static int
is_valid_bool_arg (ILStackDesc *arg)
{
	switch (arg->stype) {
	case TYPE_I4:
	case TYPE_I8:
	case TYPE_PTR:
	case TYPE_MP:
	case TYPE_OBJ:
		return TRUE;
	default:
		return FALSE;
	}
}

static int
can_store_type (ILStackDesc *arg, MonoType *type)
{
	int t = type->type;
	if (type->byref && arg->stype != TYPE_MP)
		return FALSE;
handle_enum:
	switch (t) {
	case MONO_TYPE_VOID:
		return FALSE;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		if (arg->stype == TYPE_I4 || arg->stype == TYPE_PTR)
			return TRUE;
		return FALSE;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
		return TRUE;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return TRUE; /* FIXME */
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		if (arg->stype == TYPE_I8)
			return TRUE;
		return FALSE;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		if (arg->stype == TYPE_R8)
			return TRUE;
		return FALSE;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			if (arg->type->data.klass != type->data.klass)
				return FALSE;
			return TRUE;
		}
	default:
		g_error ("unknown type %02x in store type", type->type);
	}
	return FALSE;
}

static int
stind_type (int op, int type) {
	switch (op) {
	case CEE_STIND_REF:
		return type == TYPE_OBJ;
	case CEE_STIND_I1:
	case CEE_STIND_I2:
	case CEE_STIND_I4:
		return type == TYPE_I4;
	case CEE_STIND_I8:
		return type == TYPE_I8;
	case CEE_STIND_R4:
	case CEE_STIND_R8:
		return type == TYPE_R8;
	default:
		g_assert_not_reached ();
	}
	return FALSE;
}

/*
 * FIXME: need to distinguish between valid and verifiable.
 * Need to keep track of types on the stack.
 * Verify types for opcodes.
 */
GSList*
mono_method_verify (MonoMethod *method, int level)
{
	MonoMethodHeader *header;
	MonoMethodSignature *signature, *csig;
	MonoGenericContext *generic_context = NULL;
	MonoMethod *cmethod;
	MonoClassField *field;
	MonoClass *klass;
	MonoImage *image;
	MonoType **params;
	ILStackDesc *stack;
	register const unsigned char *ip;
	register const unsigned char *end;
	const unsigned char *target = NULL; /* branch target */
	int max_args, max_stack, cur_stack, i, n, need_merge, start;
	guint32 token, ip_offset = 0;
	char *local_state = NULL;
	GSList *list = NULL;
	guint prefix = 0;
	ILCodeDesc *code;

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
			(method->flags & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT))) {
		return NULL;
	}
	signature = method->signature;
	header = ((MonoMethodNormal *)method)->header;
	ip = header->code;
	end = ip + header->code_size;
	max_args = signature->param_count + signature->hasthis;
	max_stack = header->max_stack;
	need_merge = cur_stack = 0;
	start = 1;
	image = method->klass->image;
	code = g_new0 (ILCodeDesc, header->code_size);
	stack = g_new0 (ILStackDesc, max_stack);
	if (signature->hasthis) {
		params = g_new0 (MonoType*, max_args);
		params [0] = &method->klass->this_arg;
		memcpy (params + 1, signature->params, sizeof (MonoType*) * signature->param_count);
	} else {
		params = signature->params;
	}

	if (method->signature->is_inflated)
		generic_context = ((MonoMethodInflated *) method)->context;

	if (header->num_locals) {
		local_state = g_new (char, header->num_locals);
		memset (local_state, header->init_locals, header->num_locals);
	}
	/*g_print ("Method %s.%s::%s\n", method->klass->name_space, method->klass->name, method->name);*/

	for (i = 0; i < header->num_clauses; ++i) {
		MonoExceptionClause *clause = &header->clauses [i];
		/* catch blocks have the exception on the stack. */
		if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) {
			code [clause->handler_offset].stack_count = 1;
			code [clause->handler_offset].flags |= CODE_SEEN;
		}
	}

	while (ip < end) {
		ip_offset = ip - header->code;
		if (start || !(code [ip_offset].flags & CODE_SEEN)) {
			if (start) {
				/* g_print ("setting stack of IL_%04x to %d\n", ip_offset, 0); */
				cur_stack = code [ip_offset].stack_count;
			} else {
				code [ip_offset].stack_count = cur_stack;
			}
			code [ip_offset].flags |= CODE_SEEN;
		} else {
			/* stack merge */
			if (code [ip_offset].stack_count != cur_stack)
				ADD_INVALID (list, g_strdup_printf ("Cannot merge stack states at 0x%04x", ip_offset));
		}
		start = 0;
		if (need_merge) {
			if (!can_merge_stack (&code [ip_offset], &code [target - header->code]))
				ADD_INVALID (list, g_strdup_printf ("Cannot merge stack states at 0x%04x", ip_offset));
			need_merge = 0;
		}
#if 0
		{
			char *discode;
			discode = mono_disasm_code_one (NULL, method, ip, NULL);
			discode [strlen (discode) - 1] = 0; /* no \n */
			g_print ("%-29s (%d)\n", discode, cur_stack);
			g_free (discode);
		}
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
			if (*ip - CEE_LDARG_0 >= max_args)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", *ip - CEE_LDARG_0, ip_offset));
			CHECK_STACK_OVERFLOW ();
			type_to_eval_stack_type (params [*ip - CEE_LDARG_0], stack + cur_stack, FALSE);
			++cur_stack;
			++ip;
			break;
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			if (*ip - CEE_LDLOC_0 >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", *ip - CEE_LDLOC_0, ip_offset));
			if (0 && !local_state [*ip - CEE_LDLOC_0])
				ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", *ip - CEE_LDLOC_0, ip_offset));
			CHECK_STACK_OVERFLOW ();
			type_to_eval_stack_type (header->locals [*ip - CEE_LDLOC_0], stack + cur_stack, FALSE);
			++cur_stack;
			++ip;
			break;
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3:
			if (*ip - CEE_STLOC_0 >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", *ip - CEE_STLOC_0, ip_offset));
			local_state [*ip - CEE_STLOC_0] = 1;
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			if (!can_store_type (stack + cur_stack, header->locals [*ip - CEE_STLOC_0]))
				ADD_INVALID (list, g_strdup_printf ("Incompatible type %s in store at 0x%04x", arg_name [stack [cur_stack].stype], ip_offset));
			++ip;
			break;
		case CEE_LDARG_S:
		case CEE_LDARGA_S:
			if (ip [1] >= max_args)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", ip [1], ip_offset));
			CHECK_STACK_OVERFLOW ();
			type_to_eval_stack_type (params [ip [1]], stack + cur_stack, *ip == CEE_LDARGA_S);
			++cur_stack;
			ip += 2;
			break;
		case CEE_STARG_S:
			if (ip [1] >= max_args)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", ip [1], ip_offset));
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			ip += 2;
			break;
		case CEE_LDLOC_S:
		case CEE_LDLOCA_S:
			if (ip [1] >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", ip [1], ip_offset));
			/* no need to check if the var is initialized if the address is taken */
			if (0 && *ip == CEE_LDLOC_S && !local_state [ip [1]])
				ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", ip [1], ip_offset));
			CHECK_STACK_OVERFLOW ();
			type_to_eval_stack_type (header->locals [ip [1]], stack + cur_stack, *ip == CEE_LDLOCA_S);
			++cur_stack;
			ip += 2;
			break;
		case CEE_STLOC_S:
			if (ip [1] >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", ip [1], ip_offset));
			local_state [ip [1]] = 1;
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			if (!can_store_type (stack + cur_stack, header->locals [ip [1]]))
				ADD_INVALID (list, g_strdup_printf ("Incompatible type %s in store at 0x%04x", arg_name [stack [cur_stack].stype], ip_offset));
			ip += 2;
			break;
		case CEE_LDNULL:
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &mono_defaults.object_class->byval_arg;
			stack [cur_stack].stype = TYPE_OBJ;
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
			stack [cur_stack].type = &mono_defaults.int_class->byval_arg;
			stack [cur_stack].stype = TYPE_I4;
			++cur_stack;
			++ip;
			break;
		case CEE_LDC_I4_S:
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &mono_defaults.int_class->byval_arg;
			stack [cur_stack].stype = TYPE_I4;
			++cur_stack;
			ip += 2;
			break;
		case CEE_LDC_I4:
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &mono_defaults.int_class->byval_arg;
			stack [cur_stack].stype = TYPE_I4;
			++cur_stack;
			ip += 5;
			break;
		case CEE_LDC_I8:
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &mono_defaults.int64_class->byval_arg;
			stack [cur_stack].stype = TYPE_I8;
			++cur_stack;
			ip += 9;
			break;
		case CEE_LDC_R4:
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &mono_defaults.double_class->byval_arg;
			stack [cur_stack].stype = TYPE_R8;
			++cur_stack;
			ip += 5;
			break;
		case CEE_LDC_R8:
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &mono_defaults.double_class->byval_arg;
			stack [cur_stack].stype = TYPE_R8;
			++cur_stack;
			ip += 9;
			break;
		case CEE_UNUSED99: ++ip; break; /* warn/error instead? */
		case CEE_DUP:
			CHECK_STACK_UNDERFLOW (1);
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack] = stack [cur_stack - 1];
			++cur_stack;
			++ip;
			break;
		case CEE_POP:
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			++ip;
			break;
		case CEE_JMP:
			if (cur_stack)
				ADD_INVALID (list, g_strdup_printf ("Eval stack must be empty in jmp at 0x%04x", ip_offset));
			token = read32 (ip + 1);
			if (in_any_block (header, ip_offset))
				ADD_INVALID (list, g_strdup_printf ("jmp cannot escape exception blocks at 0x%04x", ip_offset));
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_CALL:
		case CEE_CALLVIRT:
			token = read32 (ip + 1);
			/*
			 * FIXME: we could just load the signature ...
			 */
			cmethod = mono_get_method_full (image, token, NULL, generic_context);
			if (!cmethod)
				ADD_INVALID (list, g_strdup_printf ("Method 0x%08x not found at 0x%04x", token, ip_offset));
			if (cmethod->signature->pinvoke) {
				csig = cmethod->signature;
			} else {
				csig = mono_method_get_signature (cmethod, image, token);
			}

			CHECK_STACK_UNDERFLOW (csig->param_count + csig->hasthis);
			cur_stack -= csig->param_count + csig->hasthis;
			if (csig->ret->type != MONO_TYPE_VOID) {
				CHECK_STACK_OVERFLOW ();
				type_to_eval_stack_type (csig->ret, stack + cur_stack, FALSE);
				++cur_stack;
			}
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
				CHECK_STACK_UNDERFLOW (1);
				--cur_stack;
				if (!can_store_type (stack + cur_stack, signature->ret))
					ADD_INVALID (list, g_strdup_printf ("Incompatible type %s in ret at 0x%04x", arg_name [stack [cur_stack].stype], ip_offset));
			}
			if (cur_stack)
				ADD_INVALID (list, g_strdup_printf ("Stack not empty (%d) after ret at 0x%04x", cur_stack, ip_offset));
			cur_stack = 0;
			if (in_any_block (header, ip_offset))
				ADD_INVALID (list, g_strdup_printf ("ret cannot escape exception blocks at 0x%04x", ip_offset));
			++ip;
			start = 1;
			break;
		case CEE_BR_S:
			target = ip + (signed char)ip [1] + 2;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!in_same_block (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ip_offset));
			ip += 2;
			start = 1;
			break;
		case CEE_BRFALSE_S:
		case CEE_BRTRUE_S:
			target = ip + (signed char)ip [1] + 2;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!in_same_block (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ip_offset));
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			if (!is_valid_bool_arg (stack + cur_stack))
				ADD_INVALID (list, g_strdup_printf ("Argument type %s not valid for brtrue/brfalse at 0x%04x", arg_name [stack [cur_stack].stype], ip_offset));
			ip += 2;
			need_merge = 1;
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
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!in_same_block (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ip_offset));
			CHECK_STACK_UNDERFLOW (2);
			cur_stack -= 2;
			if (type_from_op (*ip, stack + cur_stack) == TYPE_INV)
				ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0x%02x at 0x%04x", *ip, ip_offset));
			ip += 2;
			need_merge = 1;
			break;
		case CEE_BR:
			target = ip + (gint32)read32 (ip + 1) + 5;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!in_same_block (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ip_offset));
			ip += 5;
			start = 1;
			break;
		case CEE_BRFALSE:
		case CEE_BRTRUE:
			target = ip + (gint32)read32 (ip + 1) + 5;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!in_same_block (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ip_offset));
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			if (!is_valid_bool_arg (stack + cur_stack))
				ADD_INVALID (list, g_strdup_printf ("Argument type %s not valid for brtrue/brfalse at 0x%04x", arg_name [stack [cur_stack].stype], ip_offset));
			ip += 5;
			need_merge = 1;
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
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!in_same_block (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ip_offset));
			CHECK_STACK_UNDERFLOW (2);
			cur_stack -= 2;
			if (type_from_op (*ip, stack + cur_stack) == TYPE_INV)
				ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0x%02x at 0x%04x", *ip, ip_offset));
			ip += 5;
			need_merge = 1;
			break;
		case CEE_SWITCH:
			n = read32 (ip + 1);
			target = ip + sizeof (guint32) * n;
			/* FIXME: check that ip is in range (and within the same exception block) */
			for (i = 0; i < n; ++i)
				if (target + (gint32) read32 (ip + 5 + i * sizeof (gint32)) >= end || target + (gint32) read32 (ip + 5 + i * sizeof (gint32)) < header->code)
					ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			if (stack [cur_stack].stype != TYPE_I4)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument to switch at 0x%04x", ip_offset));
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
			CHECK_STACK_UNDERFLOW (1);
			if (stack [cur_stack - 1].stype != TYPE_PTR && stack [cur_stack - 1].stype != TYPE_MP)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument to ldind at 0x%04x", ip_offset));
			stack [cur_stack - 1].stype = ldind_type [*ip - CEE_LDIND_I1];
			++ip;
			break;
		case CEE_STIND_REF:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
			CHECK_STACK_UNDERFLOW (2);
			cur_stack -= 2;
			if (stack [cur_stack].stype != TYPE_PTR && stack [cur_stack].stype != TYPE_MP)
				ADD_INVALID (list, g_strdup_printf ("Invalid pointer argument to stind at 0x%04x", ip_offset));
			if (!stind_type (*ip, stack [cur_stack + 1].stype))
				ADD_INVALID (list, g_strdup_printf ("Incompatible value argument to stind at 0x%04x", ip_offset));
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
			CHECK_STACK_UNDERFLOW (2);
			--cur_stack;
			if (type_from_op (*ip, stack + cur_stack - 1) == TYPE_INV)
				ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0x%02x at 0x%04x", *ip, ip_offset));
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
			CHECK_STACK_UNDERFLOW (1);
			if (type_from_op (*ip, stack + cur_stack - 1) == TYPE_INV)
				ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0x%02x at 0x%04x", *ip, ip_offset));
			++ip;
			break;
		case CEE_CPOBJ:
			token = read32 (ip + 1);
			CHECK_STACK_UNDERFLOW (2);
			cur_stack -= 2;
			ip += 5;
			break;
		case CEE_LDOBJ:
			token = read32 (ip + 1);
			CHECK_STACK_UNDERFLOW (1);
			if (stack [cur_stack - 1].stype != TYPE_MP)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument to ldobj at 0x%04x", ip_offset));
			klass = mono_class_get_full (image, token, generic_context);
			if (!klass)
				ADD_INVALID (list, g_strdup_printf ("Cannot load class from token 0x%08x at 0x%04x", token, ip_offset));
			if (!klass->valuetype)
				ADD_INVALID (list, g_strdup_printf ("Class is not a valuetype at 0x%04x", ip_offset));
			stack [cur_stack - 1].stype = TYPE_VT;
			stack [cur_stack - 1].type = &klass->byval_arg;
			ip += 5;
			break;
		case CEE_LDSTR:
			token = read32 (ip + 1);
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &mono_defaults.string_class->byval_arg;
			stack [cur_stack].stype = TYPE_OBJ;
			++cur_stack;
			ip += 5;
			break;
		case CEE_NEWOBJ:
			token = read32 (ip + 1);
			/*
			 * FIXME: we could just load the signature ...
			 */
			cmethod = mono_get_method_full (image, token, NULL, generic_context);
			if (!cmethod)
				ADD_INVALID (list, g_strdup_printf ("Constructor 0x%08x not found at 0x%04x", token, ip_offset));
			csig = cmethod->signature;
			CHECK_STACK_UNDERFLOW (csig->param_count);
			cur_stack -= csig->param_count;
			CHECK_STACK_OVERFLOW ();
			stack [cur_stack].type = &cmethod->klass->byval_arg;
			stack [cur_stack].stype = cmethod->klass->valuetype? TYPE_VT: TYPE_OBJ;
			++cur_stack;
			ip += 5;
			break;
		case CEE_CASTCLASS:
		case CEE_ISINST:
			token = read32 (ip + 1);
			CHECK_STACK_UNDERFLOW (1);
			ip += 5;
			break;
		case CEE_CONV_R_UN:
			CHECK_STACK_UNDERFLOW (1);
			++ip;
			break;
		case CEE_UNUSED58:
		case CEE_UNUSED1:
			++ip; /* warn, error ? */
			break;
		case CEE_UNBOX:
			token = read32 (ip + 1);
			CHECK_STACK_UNDERFLOW (1);
			if (stack [cur_stack - 1].stype != TYPE_OBJ)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument %s to unbox at 0x%04x", arg_name [stack [cur_stack - 1].stype], ip_offset));
			stack [cur_stack - 1].stype = TYPE_MP;
			stack [cur_stack - 1].type = NULL;
			ip += 5;
			break;
		case CEE_THROW:
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			++ip;
			start = 1;
			break;
		case CEE_LDFLD:
			CHECK_STACK_UNDERFLOW (1);
			if (stack [cur_stack - 1].stype != TYPE_OBJ && stack [cur_stack - 1].stype != TYPE_MP)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument %s to ldfld at 0x%04x", arg_name [stack [cur_stack].stype], ip_offset));
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass, generic_context);
			if (!field)
				ADD_INVALID (list, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ip_offset));
			type_to_eval_stack_type (field->type, stack + cur_stack - 1, FALSE);
			ip += 5;
			break;
		case CEE_LDFLDA:
			CHECK_STACK_UNDERFLOW (1);
			if (stack [cur_stack - 1].stype != TYPE_OBJ && stack [cur_stack - 1].stype != TYPE_MP)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument to ldflda at 0x%04x", ip_offset));
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass, generic_context);
			if (!field)
				ADD_INVALID (list, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ip_offset));
			type_to_eval_stack_type (field->type, stack + cur_stack - 1, TRUE);
			ip += 5;
			break;
		case CEE_STFLD:
			CHECK_STACK_UNDERFLOW (2);
			cur_stack -= 2;
			if (stack [cur_stack].stype != TYPE_OBJ && stack [cur_stack].stype != TYPE_MP)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument to stfld at 0x%04x", ip_offset));
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass, generic_context);
			if (!field)
				ADD_INVALID (list, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ip_offset));
			/* can_store */
			ip += 5;
			break;
		case CEE_LDSFLD:
			CHECK_STACK_OVERFLOW ();
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass, generic_context);
			if (!field)
				ADD_INVALID (list, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ip_offset));
			type_to_eval_stack_type (field->type, stack + cur_stack, FALSE);
			++cur_stack;
			ip += 5;
			break;
		case CEE_LDSFLDA:
			CHECK_STACK_OVERFLOW ();
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass, generic_context);
			if (!field)
				ADD_INVALID (list, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ip_offset));
			type_to_eval_stack_type (field->type, stack + cur_stack, TRUE);
			++cur_stack;
			ip += 5;
			break;
		case CEE_STSFLD:
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass, generic_context);
			if (!field)
				ADD_INVALID (list, g_strdup_printf ("Cannot load field from token 0x%08x at 0x%04x", token, ip_offset));
			/* can store */
			ip += 5;
			break;
		case CEE_STOBJ:
			CHECK_STACK_UNDERFLOW (2);
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
			CHECK_STACK_UNDERFLOW (1);
			if (type_from_op (*ip, stack + cur_stack - 1) == TYPE_INV)
				ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0x%02x at 0x%04x", *ip, ip_offset));
			++ip;
			break;
		case CEE_BOX:
			CHECK_STACK_UNDERFLOW (1);
			token = read32 (ip + 1);
			if (stack [cur_stack - 1].stype == TYPE_OBJ)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument %s to box at 0x%04x", arg_name [stack [cur_stack - 1].stype], ip_offset));
			stack [cur_stack - 1].stype = TYPE_OBJ;
			ip += 5;
			break;
		case CEE_NEWARR:
			CHECK_STACK_UNDERFLOW (1);
			token = read32 (ip + 1);
			stack [cur_stack - 1].stype = TYPE_OBJ;
			ip += 5;
			break;
		case CEE_LDLEN:
			CHECK_STACK_UNDERFLOW (1);
			if (stack [cur_stack - 1].stype != TYPE_OBJ)
				ADD_INVALID (list, g_strdup_printf ("Invalid argument to ldlen at 0x%04x", ip_offset));
			stack [cur_stack - 1].type = &mono_defaults.int_class->byval_arg; /* FIXME: use a native int type */
			stack [cur_stack - 1].stype = TYPE_PTR;
			++ip;
			break;
		case CEE_LDELEMA:
			CHECK_STACK_UNDERFLOW (2);
			--cur_stack;
			if (stack [cur_stack - 1].stype != TYPE_OBJ)
				ADD_INVALID (list, g_strdup_printf ("Invalid array argument to ldelema at 0x%04x", ip_offset));
			if (stack [cur_stack].stype != TYPE_I4 && stack [cur_stack].stype != TYPE_PTR)
				ADD_INVALID (list, g_strdup_printf ("Array index needs to be Int32 or IntPtr at 0x%04x", ip_offset));
			stack [cur_stack - 1].stype = TYPE_MP;
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
			CHECK_STACK_UNDERFLOW (2);
			--cur_stack;
			if (stack [cur_stack - 1].stype != TYPE_OBJ)
				ADD_INVALID (list, g_strdup_printf ("Invalid array argument to ldelem at 0x%04x", ip_offset));
			if (stack [cur_stack].stype != TYPE_I4 && stack [cur_stack].stype != TYPE_PTR)
				ADD_INVALID (list, g_strdup_printf ("Array index needs to be Int32 or IntPtr at 0x%04x", ip_offset));
			stack [cur_stack - 1].stype = ldelem_type [*ip - CEE_LDELEM_I1];
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
			CHECK_STACK_UNDERFLOW (3);
			cur_stack -= 3;
			++ip;
			break;
		case CEE_LDELEM_ANY:
		case CEE_STELEM_ANY:
		case CEE_UNBOX_ANY:
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
		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
			CHECK_STACK_UNDERFLOW (1);
			if (type_from_op (*ip, stack + cur_stack - 1) == TYPE_INV)
				ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0x%02x at 0x%04x", *ip, ip_offset));
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
			CHECK_STACK_UNDERFLOW (1);
			++ip;
			break;
		case CEE_CKFINITE:
			CHECK_STACK_UNDERFLOW (1);
			++ip;
			break;
		case CEE_UNUSED24:
		case CEE_UNUSED25:
			++ip; /* warn, error ? */
			break;
		case CEE_MKREFANY:
			CHECK_STACK_UNDERFLOW (1);
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
			CHECK_STACK_OVERFLOW ();
			token = read32 (ip + 1);
			++cur_stack;
			ip += 5;
			break;
		case CEE_CONV_U2:
		case CEE_CONV_U1:
		case CEE_CONV_I:
		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_U:
			CHECK_STACK_UNDERFLOW (1);
			if (type_from_op (*ip, stack + cur_stack - 1) == TYPE_INV)
				ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0x%02x at 0x%04x", *ip, ip_offset));
			++ip;
			break;
		case CEE_ADD_OVF:
		case CEE_ADD_OVF_UN:
		case CEE_MUL_OVF:
		case CEE_MUL_OVF_UN:
		case CEE_SUB_OVF:
		case CEE_SUB_OVF_UN:
			CHECK_STACK_UNDERFLOW (2);
			--cur_stack;
			++ip;
			break;
		case CEE_ENDFINALLY:
			++ip;
			start = 1;
			break;
		case CEE_LEAVE:
			target = ip + (gint32)read32(ip + 1) + 5;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!is_correct_leave (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Leave not allowed in finally block at 0x%04x", ip_offset));
			ip += 5;
			start = 1;
			break;
		case CEE_LEAVE_S:
			target = ip + (signed char)ip [1] + 2;
			if (target >= end || target < header->code)
				ADD_INVALID (list, g_strdup_printf ("Branch target out of code at 0x%04x", ip_offset));
			if (!is_correct_leave (header, ip_offset, target - header->code))
				ADD_INVALID (list, g_strdup_printf ("Leave not allowed in finally block at 0x%04x", ip_offset));
			ip += 2;
			start = 1;
			break;
		case CEE_STIND_I:
			CHECK_STACK_UNDERFLOW (2);
			cur_stack -= 2;
			++ip;
			break;
		case CEE_CONV_U:
			CHECK_STACK_UNDERFLOW (1);
			++ip;
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
			case CEE_ARGLIST:
				CHECK_STACK_OVERFLOW ();
				++ip;
				break;
			case CEE_CEQ:
			case CEE_CGT:
			case CEE_CGT_UN:
			case CEE_CLT:
			case CEE_CLT_UN:
				CHECK_STACK_UNDERFLOW (2);
				--cur_stack;
				if (type_from_op (256 + *ip, stack + cur_stack - 1) == TYPE_INV)
					ADD_INVALID (list, g_strdup_printf ("Invalid arguments to opcode 0xFE 0x%02x at 0x%04x", *ip, ip_offset));
				++ip;
				break;
			case CEE_LDFTN:
				CHECK_STACK_OVERFLOW ();
				token = read32 (ip + 1);
				ip += 5;
				stack [cur_stack].stype = TYPE_PTR;
				cur_stack++;
				break;
			case CEE_LDVIRTFTN:
				CHECK_STACK_UNDERFLOW (1);
				token = read32 (ip + 1);
				ip += 5;
				if (stack [cur_stack - 1].stype != TYPE_OBJ)
					ADD_INVALID (list, g_strdup_printf ("Invalid argument to ldvirtftn at 0x%04x", ip_offset));
				stack [cur_stack - 1].stype = TYPE_PTR;
				break;
			case CEE_UNUSED56:
				++ip;
				break;
			case CEE_LDARG:
			case CEE_LDARGA:
				if (read16 (ip + 1) >= max_args)
					ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", read16 (ip + 1), ip_offset));
				CHECK_STACK_OVERFLOW ();
				++cur_stack;
				ip += 3;
				break;
			case CEE_STARG:
				if (read16 (ip + 1) >= max_args)
					ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", read16(ip + 1), ip_offset));
				CHECK_STACK_UNDERFLOW (1);
				--cur_stack;
				ip += 3;
				break;
			case CEE_LDLOC:
			case CEE_LDLOCA:
				n = read16 (ip + 1);
				if (n >= header->num_locals)
					ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", n, ip_offset));
				/* no need to check if the var is initialized if the address is taken */
				if (0 && *ip == CEE_LDLOC && !local_state [n])
					ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", n, ip_offset));
				CHECK_STACK_OVERFLOW ();
				type_to_eval_stack_type (header->locals [n], stack + cur_stack, *ip == CEE_LDLOCA);
				++cur_stack;
				ip += 3;
				break;
			case CEE_STLOC:
				n = read16 (ip + 1);
				if (n >= header->num_locals)
					ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", n, ip_offset));
				local_state [n] = 1;
				CHECK_STACK_UNDERFLOW (1);
				--cur_stack;
				if (!can_store_type (stack + cur_stack, header->locals [n]))
					ADD_INVALID (list, g_strdup_printf ("Incompatible type %s in store at 0x%04x", arg_name [stack [cur_stack].stype], ip_offset));
				ip += 3;
				break;
			case CEE_LOCALLOC:
				if (cur_stack != 1)
					ADD_INVALID (list, g_strdup_printf ("Stack must have only size item in localloc at 0x%04x", ip_offset));
				if (stack [cur_stack -1].stype != TYPE_I4 && stack [cur_stack -1].stype != TYPE_PTR)
					ADD_INVALID (list, g_strdup_printf ("Invalid argument to localloc at 0x%04x", ip_offset));
				stack [cur_stack -1].stype = TYPE_MP;
				++ip;
				break;
			case CEE_UNUSED57:
				++ip;
				break;
			case CEE_ENDFILTER:
				if (cur_stack != 1)
					ADD_INVALID (list, g_strdup_printf ("Stack must have only filter result in endfilter at 0x%04x", ip_offset));
				++ip;
				break;
			case CEE_UNALIGNED_:
				prefix |= PREFIX_UNALIGNED;
				++ip;
				break;
			case CEE_VOLATILE_:
				prefix |= PREFIX_VOLATILE;
				++ip;
				break;
			case CEE_TAIL_:
				prefix |= PREFIX_TAIL;
				++ip;
				if (ip < end && (*ip != CEE_CALL && *ip != CEE_CALLI && *ip != CEE_CALLVIRT))
					ADD_INVALID (list, g_strdup_printf ("tail prefix must be used only with call opcodes at 0x%04x", ip_offset));
				break;
			case CEE_INITOBJ:
				CHECK_STACK_UNDERFLOW (1);
				token = read32 (ip + 1);
				ip += 5;
				--cur_stack;
				break;
			case CEE_CONSTRAINED_:
				token = read32 (ip + 1);
				ip += 5;
				break;
			case CEE_CPBLK:
				CHECK_STACK_UNDERFLOW (3);
				ip++;
				break;
			case CEE_INITBLK:
				CHECK_STACK_UNDERFLOW (3);
				ip++;
				break;
			case CEE_NO_:
				ip += 2;
				break;
			case CEE_RETHROW:
				++ip;
				break;
			case CEE_UNUSED:
				++ip;
				break;
			case CEE_SIZEOF:
				CHECK_STACK_OVERFLOW ();
				token = read32 (ip + 1);
				ip += 5;
				stack [cur_stack].type = &mono_defaults.uint_class->byval_arg;
				stack [cur_stack].stype = TYPE_I4;
				cur_stack++;
				break;
			case CEE_REFANYTYPE:
				CHECK_STACK_UNDERFLOW (1);
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
	if (ip != end || !start) {
		ADD_INVALID (list, g_strdup_printf ("Run ahead of method code at 0x%04x", ip_offset));
	}
invalid_cil:

	g_free (local_state);
	g_free (code);
	g_free (stack);
	if (signature->hasthis)
		g_free (params);
	return list;
}

typedef struct {
	const char *name;
	guint64 offset;
} FieldDesc;

typedef struct {
	const char *name;
	const FieldDesc *fields;
} ClassDesc;

static const FieldDesc 
typebuilder_fields[] = {
	{"tname", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, name)},
	{"nspace", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, nspace)},
	{"parent", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, parent)},
	{"interfaces", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, interfaces)},
	{"methods", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, methods)},
	{"properties", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, properties)},
	{"fields", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, fields)},
	{"attrs", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, attrs)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, table_idx)},
	{NULL, 0}
};

static const FieldDesc 
modulebuilder_fields[] = {
	{"types", G_STRUCT_OFFSET (MonoReflectionModuleBuilder, types)},
	{"cattrs", G_STRUCT_OFFSET (MonoReflectionModuleBuilder, cattrs)},
	{"guid", G_STRUCT_OFFSET (MonoReflectionModuleBuilder, guid)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionModuleBuilder, table_idx)},
	{NULL, 0}
};

static const FieldDesc 
assemblybuilder_fields[] = {
	{"entry_point", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, entry_point)},
	{"modules", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, modules)},
	{"name", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, name)},
	{"resources", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, resources)},
	{"version", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, version)},
	{"culture", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, culture)},
	{NULL, 0}
};

static const FieldDesc 
ctorbuilder_fields[] = {
	{"ilgen", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, ilgen)},
	{"parameters", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, parameters)},
	{"attrs", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, attrs)},
	{"iattrs", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, iattrs)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, table_idx)},
	{"call_conv", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, call_conv)},
	{"type", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, type)},
	{NULL, 0}
};

static const FieldDesc 
methodbuilder_fields[] = {
	{"mhandle", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, mhandle)},
	{"rtype", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, rtype)},
	{"parameters", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, parameters)},
	{"attrs", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, attrs)},
	{"iattrs", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, iattrs)},
	{"name", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, name)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, table_idx)},
	{"code", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, code)},
	{"ilgen", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, ilgen)},
	{"type", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, type)},
	{"pinfo", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, pinfo)},
	{"pi_dll", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, dll)},
	{"pi_entry", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, dllentry)},
	{"ncharset", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, charset)},
	{"native_cc", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, native_cc)},
	{"call_conv", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, call_conv)},
	{NULL, 0}
};

static const FieldDesc 
fieldbuilder_fields[] = {
	{"attrs", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, attrs)},
	{"type", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, type)},
	{"name", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, name)},
	{"def_value", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, def_value)},
	{"offset", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, offset)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, table_idx)},
	{NULL, 0}
};

static const FieldDesc 
propertybuilder_fields[] = {
	{"attrs", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, attrs)},
	{"name", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, name)},
	{"type", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, type)},
	{"parameters", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, parameters)},
	{"def_value", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, def_value)},
	{"set_method", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, set_method)},
	{"get_method", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, get_method)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, table_idx)},
	{NULL, 0}
};

static const FieldDesc 
ilgenerator_fields[] = {
	{"code", G_STRUCT_OFFSET (MonoReflectionILGen, code)},
	{"code_len", G_STRUCT_OFFSET (MonoReflectionILGen, code_len)},
	{"max_stack", G_STRUCT_OFFSET (MonoReflectionILGen, max_stack)},
	{"cur_stack", G_STRUCT_OFFSET (MonoReflectionILGen, cur_stack)},
	{"locals", G_STRUCT_OFFSET (MonoReflectionILGen, locals)},
	{"ex_handlers", G_STRUCT_OFFSET (MonoReflectionILGen, ex_handlers)},
	{NULL, 0}
};

static const FieldDesc 
ilexinfo_fields[] = {
	{"handlers", G_STRUCT_OFFSET (MonoILExceptionInfo, handlers)},
	{"start", G_STRUCT_OFFSET (MonoILExceptionInfo, start)},
	{"len", G_STRUCT_OFFSET (MonoILExceptionInfo, len)},
	{"end", G_STRUCT_OFFSET (MonoILExceptionInfo, label)},
	{NULL, 0}
};

static const FieldDesc 
ilexblock_fields[] = {
	{"extype", G_STRUCT_OFFSET (MonoILExceptionBlock, extype)},
	{"type", G_STRUCT_OFFSET (MonoILExceptionBlock, type)},
	{"start", G_STRUCT_OFFSET (MonoILExceptionBlock, start)},
	{"len", G_STRUCT_OFFSET (MonoILExceptionBlock, len)},
	{"filter_offset", G_STRUCT_OFFSET (MonoILExceptionBlock, filter_offset)},
	{NULL, 0}
};

static const ClassDesc
emit_classes_to_check [] = {
	{"TypeBuilder", typebuilder_fields},
	{"ModuleBuilder", modulebuilder_fields},
	{"AssemblyBuilder", assemblybuilder_fields},
	{"ConstructorBuilder", ctorbuilder_fields},
	{"MethodBuilder", methodbuilder_fields},
	{"FieldBuilder", fieldbuilder_fields},
	{"PropertyBuilder", propertybuilder_fields},
	{"ILGenerator", ilgenerator_fields},
	{"ILExceptionBlock", ilexblock_fields},
	{"ILExceptionInfo", ilexinfo_fields},
	{NULL, NULL}
};

static const FieldDesc 
monoevent_fields[] = {
	{"klass", G_STRUCT_OFFSET (MonoReflectionEvent, klass)},
	{"handle", G_STRUCT_OFFSET (MonoReflectionEvent, event)},
	{NULL, 0}
};

static const FieldDesc 
monoproperty_fields[] = {
	{"klass", G_STRUCT_OFFSET (MonoReflectionProperty, klass)},
	{"prop", G_STRUCT_OFFSET (MonoReflectionProperty, property)},
	{NULL, 0}
};

static const FieldDesc 
monofield_fields[] = {
	{"klass", G_STRUCT_OFFSET (MonoReflectionField, klass)},
	{"fhandle", G_STRUCT_OFFSET (MonoReflectionField, field)},
	{NULL, 0}
};

static const FieldDesc 
monomethodinfo_fields[] = {
	{"parent", G_STRUCT_OFFSET (MonoMethodInfo, parent)},
	{"ret", G_STRUCT_OFFSET (MonoMethodInfo, ret)},
	{"attrs", G_STRUCT_OFFSET (MonoMethodInfo, attrs)},
	{"iattrs", G_STRUCT_OFFSET (MonoMethodInfo, implattrs)},
	{NULL, 0}
};

static const FieldDesc 
monopropertyinfo_fields[] = {
	{"parent", G_STRUCT_OFFSET (MonoPropertyInfo, parent)},
	{"name", G_STRUCT_OFFSET (MonoPropertyInfo, name)},
	{"get_method", G_STRUCT_OFFSET (MonoPropertyInfo, get)},
	{"set_method", G_STRUCT_OFFSET (MonoPropertyInfo, set)},
	{"attrs", G_STRUCT_OFFSET (MonoPropertyInfo, attrs)},
	{NULL, 0}
};

static const FieldDesc 
monomethod_fields[] = {
	{"mhandle", G_STRUCT_OFFSET (MonoReflectionMethod, method)},
	{NULL, 0}
};

static const FieldDesc 
monocmethod_fields[] = {
	{"mhandle", G_STRUCT_OFFSET (MonoReflectionMethod, method)},
	{NULL, 0}
};

static const FieldDesc 
pinfo_fields[] = {
	{"ClassImpl", G_STRUCT_OFFSET (MonoReflectionParameter, ClassImpl)},
	{"DefaultValueImpl", G_STRUCT_OFFSET (MonoReflectionParameter, DefaultValueImpl)},
	{"MemberImpl", G_STRUCT_OFFSET (MonoReflectionParameter, MemberImpl)},
	{"NameImpl", G_STRUCT_OFFSET (MonoReflectionParameter, NameImpl)},
	{"PositionImpl", G_STRUCT_OFFSET (MonoReflectionParameter, PositionImpl)},
	{"AttrsImpl", G_STRUCT_OFFSET (MonoReflectionParameter, AttrsImpl)},
	{NULL, 0}
};

static const ClassDesc
reflection_classes_to_check [] = {
	{"MonoEvent", monoevent_fields},
	{"MonoProperty", monoproperty_fields},
	{"MonoField", monofield_fields},
	{"MonoMethodInfo", monomethodinfo_fields},
	{"MonoPropertyInfo", monopropertyinfo_fields},
	{"MonoMethod", monomethod_fields},
	{"MonoCMethod", monocmethod_fields},
	{"ParameterInfo", pinfo_fields},
	{NULL, NULL}
};

static FieldDesc 
enuminfo_fields[] = {
	{"utype", G_STRUCT_OFFSET (MonoEnumInfo, utype)},
	{"values", G_STRUCT_OFFSET (MonoEnumInfo, values)},
	{"names", G_STRUCT_OFFSET (MonoEnumInfo, names)},
	{NULL, 0}
};

static FieldDesc 
delegate_fields[] = {
	{"target_type", G_STRUCT_OFFSET (MonoDelegate, target_type)},
	{"m_target", G_STRUCT_OFFSET (MonoDelegate, target)},
	{"method_name", G_STRUCT_OFFSET (MonoDelegate, method_name)},
	{"method_ptr", G_STRUCT_OFFSET (MonoDelegate, method_ptr)},
	{"delegate_trampoline", G_STRUCT_OFFSET (MonoDelegate, delegate_trampoline)},
	{"method_info", G_STRUCT_OFFSET (MonoDelegate, method_info)},
	{NULL, 0}
};

static FieldDesc 
multicast_delegate_fields[] = {
	{"prev", G_STRUCT_OFFSET (MonoMulticastDelegate, prev)},
	{NULL, 0}
};

static FieldDesc 
async_result_fields[] = {
	{"async_state", G_STRUCT_OFFSET (MonoAsyncResult, async_state)},
	{"handle", G_STRUCT_OFFSET (MonoAsyncResult, handle)},
	{"async_delegate", G_STRUCT_OFFSET (MonoAsyncResult, async_delegate)},
	{"data", G_STRUCT_OFFSET (MonoAsyncResult, data)},
	{"sync_completed", G_STRUCT_OFFSET (MonoAsyncResult, sync_completed)},
	{"completed", G_STRUCT_OFFSET (MonoAsyncResult, completed)},
	{"endinvoke_called", G_STRUCT_OFFSET (MonoAsyncResult, endinvoke_called)},
	{"async_callback", G_STRUCT_OFFSET (MonoAsyncResult, async_callback)},
	{NULL, 0}
};

static FieldDesc 
exception_fields[] = {
	{"trace_ips", G_STRUCT_OFFSET (MonoException, trace_ips)},
	{"inner_exception", G_STRUCT_OFFSET (MonoException, inner_ex)},
	{"message", G_STRUCT_OFFSET (MonoException, message)},
	{"help_link", G_STRUCT_OFFSET (MonoException, help_link)},
	{"class_name", G_STRUCT_OFFSET (MonoException, class_name)},
	{"stack_trace", G_STRUCT_OFFSET (MonoException, stack_trace)},
	{"remote_stack_trace", G_STRUCT_OFFSET (MonoException, remote_stack_trace)},
	{"remote_stack_index", G_STRUCT_OFFSET (MonoException, remote_stack_index)},
	{"hresult", G_STRUCT_OFFSET (MonoException, hresult)},
	{"source", G_STRUCT_OFFSET (MonoException, source)},
	{NULL, 0}
};

static const ClassDesc
system_classes_to_check [] = {
	{"Exception", exception_fields},
	{"MonoEnumInfo", enuminfo_fields},
	{"Delegate", delegate_fields},
	{"MulticastDelegate", multicast_delegate_fields},
	{NULL, NULL}
};

static FieldDesc 
stack_frame_fields [] = {
	{"ilOffset", G_STRUCT_OFFSET (MonoStackFrame, il_offset)},
	{"nativeOffset", G_STRUCT_OFFSET (MonoStackFrame, native_offset)},
	{"methodBase", G_STRUCT_OFFSET (MonoStackFrame, method)},
	{"fileName", G_STRUCT_OFFSET (MonoStackFrame, filename)},
	{"lineNumber", G_STRUCT_OFFSET (MonoStackFrame, line)},
	{"columnNumber", G_STRUCT_OFFSET (MonoStackFrame, column)},
	{NULL, 0}
};

static const ClassDesc
system_diagnostics_classes_to_check [] = {
	{"StackFrame", stack_frame_fields},
	{NULL, NULL}
};

static FieldDesc 
mono_method_message_fields[] = {
	{"method", G_STRUCT_OFFSET (MonoMethodMessage, method)},
	{"args", G_STRUCT_OFFSET (MonoMethodMessage, args)},
	{"names", G_STRUCT_OFFSET (MonoMethodMessage, names)},
	{"arg_types", G_STRUCT_OFFSET (MonoMethodMessage, arg_types)},
	{"ctx", G_STRUCT_OFFSET (MonoMethodMessage, ctx)},
	{"rval", G_STRUCT_OFFSET (MonoMethodMessage, rval)},
	{"exc", G_STRUCT_OFFSET (MonoMethodMessage, exc)},
	{NULL, 0}
};

static const ClassDesc
messaging_classes_to_check [] = {
	{"AsyncResult", async_result_fields},
	{"MonoMethodMessage", mono_method_message_fields},
	{NULL, NULL}
};

static FieldDesc 
transparent_proxy_fields[] = {
	{"_rp", G_STRUCT_OFFSET (MonoTransparentProxy, rp)},
	{"_class", G_STRUCT_OFFSET (MonoTransparentProxy, remote_class)},
	{NULL, 0}
};

static FieldDesc 
real_proxy_fields[] = {
	{"class_to_proxy", G_STRUCT_OFFSET (MonoRealProxy, class_to_proxy)},
	{NULL, 0}
};

static const ClassDesc
proxy_classes_to_check [] = {
	{"TransparentProxy", transparent_proxy_fields},
	{"RealProxy", real_proxy_fields},
	{NULL, NULL}
};

static FieldDesc 
wait_handle_fields[] = {
	{"os_handle", G_STRUCT_OFFSET (MonoWaitHandle, handle)},
	{"disposed", G_STRUCT_OFFSET (MonoWaitHandle, disposed)},
	{NULL, 0}
};

static FieldDesc 
thread_fields[] = {
	{"system_thread_handle", G_STRUCT_OFFSET (MonoThread, handle)},
	{"current_culture", G_STRUCT_OFFSET (MonoThread, culture_info)},
	{"threadpool_thread", G_STRUCT_OFFSET (MonoThread, threadpool_thread)},
	{"state", G_STRUCT_OFFSET (MonoThread, state)},
	{"abort_exc", G_STRUCT_OFFSET (MonoThread, abort_exc)},
	{"abort_state", G_STRUCT_OFFSET (MonoThread, abort_state)},
	{"thread_id", G_STRUCT_OFFSET (MonoThread, tid)},
	{NULL, 0}
};

static const ClassDesc
threading_classes_to_check [] = {
	{"Thread", thread_fields},
	{"WaitHandle", wait_handle_fields},
	{NULL, NULL}
};

static const FieldDesc
cinfo_fields[] = {
	{"datetime_format", G_STRUCT_OFFSET (MonoCultureInfo, datetime_format)},
	{"number_format", G_STRUCT_OFFSET (MonoCultureInfo, number_format)},
	{"textinfo", G_STRUCT_OFFSET (MonoCultureInfo, textinfo)},
	{"name", G_STRUCT_OFFSET (MonoCultureInfo, name)},
	{"displayname", G_STRUCT_OFFSET (MonoCultureInfo, displayname)},
	{"englishname", G_STRUCT_OFFSET (MonoCultureInfo, englishname)},
	{"nativename", G_STRUCT_OFFSET (MonoCultureInfo, nativename)},
	{"iso3lang", G_STRUCT_OFFSET (MonoCultureInfo, iso3lang)},
	{"iso2lang", G_STRUCT_OFFSET (MonoCultureInfo, iso2lang)},
	{"icu_name", G_STRUCT_OFFSET (MonoCultureInfo, icu_name)},
	{"win3lang", G_STRUCT_OFFSET (MonoCultureInfo, win3lang)},
	{"compareinfo", G_STRUCT_OFFSET (MonoCultureInfo, compareinfo)},
	{NULL, 0}
};

static const FieldDesc
dtfinfo_fields[] = {
	{"_AMDesignator", G_STRUCT_OFFSET (MonoDateTimeFormatInfo, AMDesignator)},
	{"_PMDesignator", G_STRUCT_OFFSET (MonoDateTimeFormatInfo, PMDesignator)},
	{"_DayNames", G_STRUCT_OFFSET (MonoDateTimeFormatInfo, DayNames)},
	{"_MonthNames", G_STRUCT_OFFSET (MonoDateTimeFormatInfo, MonthNames)},
	{NULL, 0}
};

static const FieldDesc
nfinfo_fields[] = {
	{"decimalFormats", G_STRUCT_OFFSET (MonoNumberFormatInfo, decimalFormats)},
	{"currencySymbol", G_STRUCT_OFFSET (MonoNumberFormatInfo, currencySymbol)},
	{"percentSymbol", G_STRUCT_OFFSET (MonoNumberFormatInfo, percentSymbol)},
	{"positiveSign", G_STRUCT_OFFSET (MonoNumberFormatInfo, positiveSign)},
	{NULL, 0}
};

static const FieldDesc
compinfo_fields[] = {
	{"lcid", G_STRUCT_OFFSET (MonoCompareInfo, lcid)},
	{"ICU_collator", G_STRUCT_OFFSET (MonoCompareInfo, ICU_collator)},
	{NULL, 0}
};

static const FieldDesc
sortkey_fields[] = {
	{"str", G_STRUCT_OFFSET (MonoSortKey, str)},
	{"options", G_STRUCT_OFFSET (MonoSortKey, options)},
	{"key", G_STRUCT_OFFSET (MonoSortKey, key)},
	{"lcid", G_STRUCT_OFFSET (MonoSortKey, lcid)},
	{NULL, 0}
};

static const ClassDesc
globalization_classes_to_check [] = {
	{"CultureInfo", cinfo_fields},
	{"DateTimeFormatInfo", dtfinfo_fields},
	{"NumberFormatInfo", nfinfo_fields},
	{"CompareInfo", compinfo_fields},
	{"SortKey", sortkey_fields},
	{NULL, NULL}
};

typedef struct {
	const char *name;
	const ClassDesc *types;
} NameSpaceDesc;

static const NameSpaceDesc
namespaces_to_check[] = {
	{"System.Runtime.Remoting.Proxies", proxy_classes_to_check},
	{"System.Runtime.Remoting.Messaging", messaging_classes_to_check},
	{"System.Reflection.Emit", emit_classes_to_check},
	{"System.Reflection", reflection_classes_to_check},
	{"System.Threading", threading_classes_to_check},
	{"System.Diagnostics", system_diagnostics_classes_to_check},
	{"System", system_classes_to_check},
	{"System.Globalization", globalization_classes_to_check},
	{NULL, NULL}
};

static char*
check_corlib (MonoImage *corlib)
{
	MonoClass *klass;
	MonoClassField *field;
	const FieldDesc *fdesc;
	const ClassDesc *cdesc;
	const NameSpaceDesc *ndesc;
	gint struct_offset;
	GString *result = NULL;

	for (ndesc = namespaces_to_check; ndesc->name; ++ndesc) {
		for (cdesc = ndesc->types; cdesc->name; ++cdesc) {
			klass = mono_class_from_name (corlib, ndesc->name, cdesc->name);
			if (!klass) {
				if (!result)
					result = g_string_new ("");
				g_string_append_printf (result, "Cannot find class %s\n", cdesc->name);
				continue;
			}
			mono_class_init (klass);
			/*
			 * FIXME: we should also check the size of valuetypes, or
			 * we're going to have trouble when we access them in arrays.
			 */
			if (klass->valuetype)
				struct_offset = sizeof (MonoObject);
			else
				struct_offset = 0;
			for (fdesc = cdesc->fields; fdesc->name; ++fdesc) {
				field = mono_class_get_field_from_name (klass, fdesc->name);
				if (!field || (field->offset != (fdesc->offset + struct_offset))) {
					if (!result)
						result = g_string_new ("");
					g_string_append_printf (result, "field `%s' mismatch in class %s (%ld + %ld != %ld)\n", fdesc->name, cdesc->name, (long) fdesc->offset, (long)struct_offset, (long) (field?field->offset:-1));
				}
			}
		}
	}
	if (result)
		return g_string_free (result, FALSE);
	return NULL;
}

char*
mono_verify_corlib () {
	return check_corlib (mono_defaults.corlib);
}


