
#include <mono/metadata/object.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mono-endian.h>
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
is_valid_blob (MonoImage *image, guint32 index, int notnull)
{
	guint32 size;
	const char *p, *send;
	
	if (index >= image->heap_blob.size)
		return 0;
	p = mono_metadata_blob_heap (image, index);
	size = mono_metadata_decode_blob_size (p, &send);
	if (index + size + (send-p) > image->heap_blob.size)
		return 0;
	if (notnull && !size)
		return 0;
	return 1;
}

static const char*
is_valid_string (MonoImage *image, guint32 index, int notnull)
{
	const char *p, *send, *res;
	
	if (index >= image->heap_strings.size)
		return NULL;
	res = p = mono_metadata_string_heap (image, index);
	send = mono_metadata_string_heap (image, image->heap_strings.size - 1);
	if (notnull && !*p)
		return 0;
	/* 
	 * FIXME: should check it's a valid utf8 string, too.
	 */
	while (p <= send) {
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
	guint32 i, last_event;
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
		G_BREAKPOINT ();	\
		goto invalid_cil;	\
	} while (0)

#define CHECK_STACK_UNDERFLOW(num)	\
	do {	\
		if (cur_stack < (num))	\
			ADD_INVALID (list, g_strdup_printf ("Stack underflow at 0x%04x", ip_offset));	\
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
	MonoClass *klass;
	int type;
} ILStackDesc;

typedef struct {
	ILStackDesc *stack;
	guint16 stack_count;
	guint16 flags;
} ILCodeDesc;

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
	MonoMethod *cmethod;
	MonoImage *image;
	register const unsigned char *ip;
	register const unsigned char *end;
	const unsigned char *target; /* branch target */
	int max_args, max_stack, cur_stack, i, n, need_merge, start;
	guint32 token, ip_offset;
	char *local_state = NULL;
	GSList *list = NULL;
	guint prefix = 0;
	ILCodeDesc *code;

	signature = method->signature;
	header = ((MonoMethodNormal *)method)->header;
	ip = header->code;
	end = ip + header->code_size;
	max_args = method->signature->param_count + method->signature->hasthis;
	max_stack = header->max_stack;
	need_merge = cur_stack = 0;
	start = 1;
	image = method->klass->image;
	code = g_new0 (ILCodeDesc, header->code_size);

	if (header->num_locals) {
		local_state = g_new (char, header->num_locals);
		memset (local_state, header->init_locals, header->num_locals);
	}
	g_print ("Method %s.%s::%s\n", method->klass->name_space, method->klass->name, method->name);

	while (ip < end) {
		ip_offset = ip - header->code;
		g_print ("IL_%04x: %02x %s\n", ip_offset, *ip, mono_opcode_names [*ip]);
		if (start || !(code [ip_offset].flags & CODE_SEEN)) {
			if (start) {
				code [ip_offset].stack_count = 0;
				start = 0;
			} else {
				code [ip_offset].stack_count = cur_stack;
			}
			code [ip_offset].flags |= CODE_SEEN;
		} else {
			/* stack merge */
			if (code [ip_offset].stack_count != cur_stack)
				ADD_INVALID (list, g_strdup_printf ("Cannot merge stack states at 0x%04x", ip_offset));
		}
		if (need_merge) {
			if (!can_merge_stack (&code [ip_offset], &code [target - header->code]))
				ADD_INVALID (list, g_strdup_printf ("Cannot merge stack states at 0x%04x", ip_offset));
			need_merge = 0;
		}

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
			++cur_stack;
			++ip;
			break;
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			if (*ip - CEE_LDLOC_0 >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", *ip - CEE_LDLOC_0, ip_offset));
			if (!local_state [*ip - CEE_LDLOC_0])
				ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", *ip - CEE_LDLOC_0, ip_offset));
			CHECK_STACK_OVERFLOW ();
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
			++ip;
			break;
		case CEE_LDARG_S:
		case CEE_LDARGA_S:
			if (ip [1] >= max_args)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", ip [1], ip_offset));
			CHECK_STACK_OVERFLOW ();
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
			if (*ip == CEE_LDLOC_S && !local_state [ip [1]])
				ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", ip [1], ip_offset));
			CHECK_STACK_OVERFLOW ();
			++cur_stack;
			ip += 2;
			break;
		case CEE_STLOC_S:
			if (ip [1] >= header->num_locals)
				ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", ip [1], ip_offset));
			local_state [ip [1]] = 1;
			CHECK_STACK_UNDERFLOW (1);
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
			CHECK_STACK_UNDERFLOW (1);
			CHECK_STACK_OVERFLOW ();
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
			cmethod = mono_get_method (image, token, NULL);
			if (!cmethod)
				ADD_INVALID (list, g_strdup_printf ("Method 0x%08x not found at 0x%04x", token, ip_offset));
			csig = cmethod->signature;
			CHECK_STACK_UNDERFLOW (csig->param_count + csig->hasthis);
			cur_stack -= csig->param_count + csig->hasthis;
			if (csig->ret->type != MONO_TYPE_VOID) {
				CHECK_STACK_OVERFLOW ();
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
				if (cur_stack != 1)
					ADD_INVALID (list, g_strdup_printf ("Stack not empty after ret at 0x%04x", ip_offset));
				--cur_stack;
			} else {
				if (cur_stack)
					ADD_INVALID (list, g_strdup_printf ("Stack not empty after ret at 0x%04x", ip_offset));
				cur_stack = 0;
			}
			if (in_any_block (header, ip_offset))
				ADD_INVALID (list, g_strdup_printf ("ret cannot escape exception blocks at 0x%04x", ip_offset));
			++ip;
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
			 * FIXME: we could just load the signature ...
			 */
			cmethod = mono_get_method (image, token, NULL);
			if (!cmethod)
				ADD_INVALID (list, g_strdup_printf ("Constructor 0x%08x not found at 0x%04x", token, ip_offset));
			csig = cmethod->signature;
			CHECK_STACK_UNDERFLOW (csig->param_count);
			cur_stack -= csig->param_count;
			CHECK_STACK_OVERFLOW ();
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
			ip += 5;
			break;
		case CEE_THROW:
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			++ip;
			break;
		case CEE_LDFLD:
			CHECK_STACK_UNDERFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_LDFLDA:
			CHECK_STACK_UNDERFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_STFLD:
			CHECK_STACK_UNDERFLOW (2);
			cur_stack -= 2;
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_LDSFLD:
			CHECK_STACK_OVERFLOW ();
			token = read32 (ip + 1);
			++cur_stack;
			ip += 5;
			break;
		case CEE_LDSFLDA:
			CHECK_STACK_OVERFLOW ();
			token = read32 (ip + 1);
			++cur_stack;
			ip += 5;
			break;
		case CEE_STSFLD:
			CHECK_STACK_UNDERFLOW (1);
			--cur_stack;
			token = read32 (ip + 1);
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
			++ip;
			break;
		case CEE_BOX:
			CHECK_STACK_UNDERFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_NEWARR:
			CHECK_STACK_UNDERFLOW (1);
			token = read32 (ip + 1);
			ip += 5;
			break;
		case CEE_LDLEN:
			CHECK_STACK_UNDERFLOW (1);
			++ip;
			break;
		case CEE_LDELEMA:
			CHECK_STACK_UNDERFLOW (2);
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
			CHECK_STACK_UNDERFLOW (2);
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
			CHECK_STACK_UNDERFLOW (3);
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
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_U2:
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_U4:
		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
			CHECK_STACK_UNDERFLOW (1);
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
				++ip;
				break;
			case CEE_LDFTN:
				CHECK_STACK_OVERFLOW ();
				token = read32 (ip + 1);
				ip += 5;
				break;
			case CEE_LDVIRTFTN:
				CHECK_STACK_UNDERFLOW (1);
				token = read32 (ip + 1);
				ip += 5;
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
				if (read16 (ip + 1) >= header->num_locals)
					ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", read16 (ip + 1), ip_offset));
				/* no need to check if the var is initialized if the address is taken */
				if (*ip == CEE_LDLOC && !local_state [read16 (ip + 1)])
					ADD_INVALID (list, g_strdup_printf ("Local var %d is initialized at 0x%04x", read16 (ip + 1), ip_offset));
				CHECK_STACK_OVERFLOW ();
				++cur_stack;
				ip += 3;
				break;
			case CEE_STLOC:
				if (read16 (ip + 1) >= header->num_locals)
					ADD_INVALID (list, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", read16 (ip + 1), ip_offset));
				local_state [read16 (ip + 1)] = 1;
				CHECK_STACK_UNDERFLOW (1);
				--cur_stack;
				ip += 3;
				break;
			case CEE_LOCALLOC:
				if (cur_stack != 1)
					ADD_INVALID (list, g_strdup_printf ("Stack must have only size item in localloc at 0x%04x", ip_offset));
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
				break;
			case CEE_UNUSED68:
				++ip;
				break;
			case CEE_CPBLK:
				CHECK_STACK_UNDERFLOW (3);
				ip++;
				break;
			case CEE_INITBLK:
				CHECK_STACK_UNDERFLOW (3);
				ip++;
				break;
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
				CHECK_STACK_OVERFLOW ();
				token = read32 (ip + 1);
				ip += 5;
				break;
			case CEE_REFANYTYPE:
				CHECK_STACK_UNDERFLOW (1);
				++ip;
				break;
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
	g_free (code);
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
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionModuleBuilder, table_idx)},
	{NULL, 0}
};

static const FieldDesc 
assemblybuilder_fields[] = {
	{"entry_point", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, entry_point)},
	{"modules", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, modules)},
	{"name", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, name)},
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
	{"mbuilder", G_STRUCT_OFFSET (MonoReflectionILGen, mbuilder)},
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
delegate_fields[] = {
	{"target_type", G_STRUCT_OFFSET (MonoDelegate, target_type)},
	{"m_target", G_STRUCT_OFFSET (MonoDelegate, target)},
	{"method", G_STRUCT_OFFSET (MonoDelegate, method)},
	{"method_ptr", G_STRUCT_OFFSET (MonoDelegate, method_ptr)},
	{NULL, 0}
};

static const ClassDesc
system_classes_to_check [] = {
	{"Delegate", delegate_fields},
	{NULL, NULL}
};

typedef struct {
	const char *name;
	const ClassDesc *types;
} NameSpaceDesc;

static const NameSpaceDesc
namespaces_to_check[] = {
	{"System.Reflection.Emit", emit_classes_to_check},
	{"System", system_classes_to_check},
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

	for (ndesc = namespaces_to_check; ndesc->name; ++ndesc) {
		for (cdesc = ndesc->types; cdesc->name; ++cdesc) {
			klass = mono_class_from_name (corlib, ndesc->name, cdesc->name);
			if (!klass)
				return g_strdup_printf ("Cannot find class %s", cdesc->name);
			mono_class_init (klass);
			/*
			 * FIXME: we should also check the size of valuetypes, or
			 * we're going to have trouble when we access them in arrays.
			 */
			if (klass->valuetype)
				struct_offset = 8;
			else
				struct_offset = 0;
			for (fdesc = cdesc->fields; fdesc->name; ++fdesc) {
				field = mono_class_get_field_from_name (klass, fdesc->name);
				if (!field || (field->offset != (fdesc->offset + struct_offset)))
					return g_strdup_printf ("field `%s' mismatch in class %s (%ld != %ld)", fdesc->name, cdesc->name, (long) fdesc->offset, (long) (field?field->offset:-1));
			}
		}
	}
	return NULL;
}

char*
mono_verify_corlib () {
	return check_corlib (mono_defaults.corlib);
}

