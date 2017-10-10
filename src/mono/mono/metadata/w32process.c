/**
 * \file
 */

#include <glib.h>

#include "w32process.h"
#include "w32process-internals.h"
#include "w32process-win32-internals.h"
#include "w32file.h"
#include "object.h"
#include "object-internals.h"
#include "class.h"
#include "class-internals.h"
#include "image.h"
#include "utils/mono-proclib.h"
#include "utils/w32api.h"

#define LOGDEBUG(...)
/* define LOGDEBUG(...) g_message(__VA_ARGS__)  */

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) && defined(HOST_WIN32)

static guint32
mono_w32process_get_pid (gpointer handle)
{
	return GetProcessId (handle);
}

static gboolean
mono_w32process_try_get_modules (gpointer process, gpointer *modules, guint32 size, guint32 *needed)
{
	return EnumProcessModules (process, (HMODULE *) modules, size, (LPDWORD) needed);
}

static guint32
mono_w32process_module_get_name (gpointer process, gpointer module, gunichar2 *basename, guint32 size)
{
	return GetModuleBaseName (process, module, basename, size);
}

static guint32
mono_w32process_module_get_filename (gpointer process, gpointer module, gunichar2 *basename, guint32 size)
{
	return GetModuleFileNameEx (process, module, basename, size);
}

static gboolean
mono_w32process_module_get_information (gpointer process, gpointer module, MODULEINFO *modinfo, guint32 size)
{
	return GetModuleInformation (process, module, modinfo, size);
}

static gboolean
mono_w32process_get_fileversion_info (gunichar2 *filename, gpointer *data)
{
	guint32 handle;
	gsize datasize;

	g_assert (data);
	*data = NULL;

	datasize = GetFileVersionInfoSize (filename, &handle);
	if (datasize <= 0)
		return FALSE;

	*data = g_malloc0 (datasize);
	if (!GetFileVersionInfo (filename, handle, datasize, *data)) {
		g_free (*data);
		return FALSE;
	}

	return TRUE;
}

static gboolean
mono_w32process_ver_query_value (gconstpointer datablock, const gunichar2 *subblock, gpointer *buffer, guint32 *len)
{
	return VerQueryValue (datablock, subblock, buffer, len);
}

static guint32
mono_w32process_ver_language_name (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	return VerLanguageName (lang, lang_out, lang_len);
}

#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) && defined(HOST_WIN32) */

static MonoImage *system_image;

static void
stash_system_image (MonoImage *image)
{
	system_image = image;
}

static MonoClass*
get_file_version_info_class (void)
{
	static MonoClass *file_version_info_class;

	if (file_version_info_class)
		return file_version_info_class;

	g_assert (system_image);

	return file_version_info_class = mono_class_load_from_name (
		system_image, "System.Diagnostics", "FileVersionInfo");
}

static MonoClass*
get_process_module_class (void)
{
	static MonoClass *process_module_class;

	if (process_module_class)
		return process_module_class;

	g_assert (system_image);

	return process_module_class = mono_class_load_from_name (
		system_image, "System.Diagnostics", "ProcessModule");
}

static guint32
unicode_chars (const gunichar2 *str)
{
	guint32 len;
	for (len = 0; str [len] != '\0'; ++len) {}
	return len;
}

static void
process_set_field_object (MonoObject *obj, const gchar *fieldname, MonoObject *data)
{
	MonoClass *klass;
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to object at %p", __func__, fieldname, data));

	klass = mono_object_class (obj);
	g_assert (klass);

	field = mono_class_get_field_from_name (klass, fieldname);
	g_assert (field);

	mono_gc_wbarrier_generic_store (((char *)obj) + field->offset, data);
}

static void
process_set_field_string (MonoObject *obj, const gchar *fieldname, const gunichar2 *val, guint32 len, MonoError *error)
{
	MonoDomain *domain;
	MonoClass *klass;
	MonoClassField *field;
	MonoString *string;

	error_init (error);

	LOGDEBUG (g_message ("%s: Setting field %s to [%s]", __func__, fieldname, g_utf16_to_utf8 (val, len, NULL, NULL, NULL)));

	domain = mono_object_domain (obj);
	g_assert (domain);

	klass = mono_object_class (obj);
	g_assert (klass);

	field = mono_class_get_field_from_name (klass, fieldname);
	g_assert (field);

	string = mono_string_new_utf16_checked (domain, val, len, error);
	return_if_nok (error);

	mono_gc_wbarrier_generic_store (((char *)obj) + field->offset, (MonoObject*)string);
}

static void
process_set_field_string_char (MonoObject *obj, const gchar *fieldname, const gchar *val, MonoError *error)
{
	MonoDomain *domain;
	MonoClass *klass;
	MonoClassField *field;
	MonoString *string;

	error_init (error);
	LOGDEBUG (g_message ("%s: Setting field %s to [%s]", __func__, fieldname, val));

	domain = mono_object_domain (obj);
	g_assert (domain);

	klass = mono_object_class (obj);
	g_assert (klass);

	field = mono_class_get_field_from_name (klass, fieldname);
	g_assert (field);

	string = mono_string_new_checked (domain, val, error);
	return_if_nok (error);

	mono_gc_wbarrier_generic_store (((char *)obj) + field->offset, (MonoObject*)string);
}

static void
process_set_field_int (MonoObject *obj, const gchar *fieldname, guint32 val)
{
	MonoClass *klass;
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to %d", __func__,fieldname, val));

	klass = mono_object_class (obj);
	g_assert (klass);

	field = mono_class_get_field_from_name (klass, fieldname);
	g_assert (field);

	*(guint32 *)(((char *)obj) + field->offset)=val;
}

static void
process_set_field_intptr (MonoObject *obj, const gchar *fieldname, gpointer val)
{
	MonoClass *klass;
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to %p", __func__, fieldname, val));

	klass = mono_object_class (obj);
	g_assert (klass);

	field = mono_class_get_field_from_name (klass, fieldname);
	g_assert (field);

	*(gpointer *)(((char *)obj) + field->offset) = val;
}

static void
process_set_field_bool (MonoObject *obj, const gchar *fieldname, gboolean val)
{
	MonoClass *klass;
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to %s", __func__, fieldname, val ? "TRUE":"FALSE"));

	klass = mono_object_class (obj);
	g_assert (klass);

	field = mono_class_get_field_from_name (klass, fieldname);
	g_assert (field);

	*(guint8 *)(((char *)obj) + field->offset) = val;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)

#define SFI_COMMENTS		"\\StringFileInfo\\%02X%02X%02X%02X\\Comments"
#define SFI_COMPANYNAME		"\\StringFileInfo\\%02X%02X%02X%02X\\CompanyName"
#define SFI_FILEDESCRIPTION	"\\StringFileInfo\\%02X%02X%02X%02X\\FileDescription"
#define SFI_FILEVERSION		"\\StringFileInfo\\%02X%02X%02X%02X\\FileVersion"
#define SFI_INTERNALNAME	"\\StringFileInfo\\%02X%02X%02X%02X\\InternalName"
#define SFI_LEGALCOPYRIGHT	"\\StringFileInfo\\%02X%02X%02X%02X\\LegalCopyright"
#define SFI_LEGALTRADEMARKS	"\\StringFileInfo\\%02X%02X%02X%02X\\LegalTrademarks"
#define SFI_ORIGINALFILENAME	"\\StringFileInfo\\%02X%02X%02X%02X\\OriginalFilename"
#define SFI_PRIVATEBUILD	"\\StringFileInfo\\%02X%02X%02X%02X\\PrivateBuild"
#define SFI_PRODUCTNAME		"\\StringFileInfo\\%02X%02X%02X%02X\\ProductName"
#define SFI_PRODUCTVERSION	"\\StringFileInfo\\%02X%02X%02X%02X\\ProductVersion"
#define SFI_SPECIALBUILD	"\\StringFileInfo\\%02X%02X%02X%02X\\SpecialBuild"
#define EMPTY_STRING		(gunichar2*)"\000\000"

typedef struct {
	const char *name;
	const char *id;
} StringTableEntry;

static StringTableEntry stringtable_entries [] = {
	{ "comments", SFI_COMMENTS },
	{ "companyname", SFI_COMPANYNAME },
	{ "filedescription", SFI_FILEDESCRIPTION },
	{ "fileversion", SFI_FILEVERSION },
	{ "internalname", SFI_INTERNALNAME },
	{ "legalcopyright", SFI_LEGALCOPYRIGHT },
	{ "legaltrademarks", SFI_LEGALTRADEMARKS },
	{ "originalfilename", SFI_ORIGINALFILENAME },
	{ "privatebuild", SFI_PRIVATEBUILD },
	{ "productname", SFI_PRODUCTNAME },
	{ "productversion", SFI_PRODUCTVERSION },
	{ "specialbuild", SFI_SPECIALBUILD }
};

static void
process_module_string_read (MonoObject *filever, gpointer data, const gchar *fieldname,
		guchar lang_hi, guchar lang_lo, const gchar *key, MonoError *error)
{
	gchar *lang_key_utf8;
	gunichar2 *lang_key, *buffer;
	UINT chars;

	error_init (error);

	lang_key_utf8 = g_strdup_printf (key, lang_lo, lang_hi, 0x04, 0xb0);

	LOGDEBUG (g_message ("%s: asking for [%s]", __func__, lang_key_utf8));

	lang_key = g_utf8_to_utf16 (lang_key_utf8, -1, NULL, NULL, NULL);

	if (mono_w32process_ver_query_value (data, lang_key, (gpointer *)&buffer, &chars) && chars > 0) {
		LOGDEBUG (g_message ("%s: found %d chars of [%s]", __func__, chars, g_utf16_to_utf8 (buffer, chars, NULL, NULL, NULL)));
		/* chars includes trailing null */
		process_set_field_string (filever, fieldname, buffer, chars - 1, error);
	} else {
		process_set_field_string (filever, fieldname, EMPTY_STRING, 0, error);
	}

	g_free (lang_key);
	g_free (lang_key_utf8);
}

static void
process_module_stringtable (MonoObject *filever, gpointer data, guchar lang_hi, guchar lang_lo, MonoError *error)
{
	for (int i = 0; i < G_N_ELEMENTS (stringtable_entries); ++i) {
		process_module_string_read (filever, data, stringtable_entries [i].name,
			lang_hi, lang_lo, stringtable_entries [i].id, error);
		return_if_nok (error);
	}
}

static void
mono_w32process_get_fileversion (MonoObject *filever, gunichar2 *filename, MonoError *error)
{
	VS_FIXEDFILEINFO *ffi;
	gpointer data;
	guchar *trans_data;
	gunichar2 *query;
	UINT ffi_size, trans_size;
	gunichar2 lang_buf[128];
	guint32 lang, lang_count;

	error_init (error);

	if (!mono_w32process_get_fileversion_info (filename, &data))
		return;

	query = g_utf8_to_utf16 ("\\", -1, NULL, NULL, NULL);
	if (query == NULL) {
		g_free (data);
		return;
	}

	if (mono_w32process_ver_query_value (data, query, (gpointer *)&ffi, &ffi_size)) {
		#define LOWORD(i32) ((guint16)((i32) & 0xFFFF))
		#define HIWORD(i32) ((guint16)(((guint32)(i32) >> 16) & 0xFFFF))

		LOGDEBUG (g_message ("%s: recording assembly: FileName [%s] FileVersionInfo [%d.%d.%d.%d]",
			__func__, g_utf16_to_utf8 (filename, -1, NULL, NULL, NULL), HIWORD (ffi->dwFileVersionMS),
				LOWORD (ffi->dwFileVersionMS), HIWORD (ffi->dwFileVersionLS), LOWORD (ffi->dwFileVersionLS)));

		process_set_field_int (filever, "filemajorpart", HIWORD (ffi->dwFileVersionMS));
		process_set_field_int (filever, "fileminorpart", LOWORD (ffi->dwFileVersionMS));
		process_set_field_int (filever, "filebuildpart", HIWORD (ffi->dwFileVersionLS));
		process_set_field_int (filever, "fileprivatepart", LOWORD (ffi->dwFileVersionLS));

		process_set_field_int (filever, "productmajorpart", HIWORD (ffi->dwProductVersionMS));
		process_set_field_int (filever, "productminorpart", LOWORD (ffi->dwProductVersionMS));
		process_set_field_int (filever, "productbuildpart", HIWORD (ffi->dwProductVersionLS));
		process_set_field_int (filever, "productprivatepart", LOWORD (ffi->dwProductVersionLS));

		process_set_field_bool (filever, "isdebug", ((ffi->dwFileFlags & ffi->dwFileFlagsMask) & VS_FF_DEBUG) != 0);
		process_set_field_bool (filever, "isprerelease", ((ffi->dwFileFlags & ffi->dwFileFlagsMask) & VS_FF_PRERELEASE) != 0);
		process_set_field_bool (filever, "ispatched", ((ffi->dwFileFlags & ffi->dwFileFlagsMask) & VS_FF_PATCHED) != 0);
		process_set_field_bool (filever, "isprivatebuild", ((ffi->dwFileFlags & ffi->dwFileFlagsMask) & VS_FF_PRIVATEBUILD) != 0);
		process_set_field_bool (filever, "isspecialbuild", ((ffi->dwFileFlags & ffi->dwFileFlagsMask) & VS_FF_SPECIALBUILD) != 0);

		#undef LOWORD
		#undef HIWORD
	}
	g_free (query);

	query = g_utf8_to_utf16 ("\\VarFileInfo\\Translation", -1, NULL, NULL, NULL);
	if (query == NULL) {
		g_free (data);
		return;
	}

	if (mono_w32process_ver_query_value (data, query, (gpointer *)&trans_data, &trans_size)) {
		/* use the first language ID we see */
		if (trans_size >= 4) {
 			LOGDEBUG (g_message("%s: %s has 0x%0x 0x%0x 0x%0x 0x%0x", __func__, g_utf16_to_utf8 (filename, -1, NULL, NULL, NULL), trans_data[0], trans_data[1], trans_data[2], trans_data[3]));
			lang = (trans_data[0]) | (trans_data[1] << 8) | (trans_data[2] << 16) | (trans_data[3] << 24);
			/* Only give the lower 16 bits to mono_w32process_ver_language_name, as Windows gets confused otherwise  */
			lang_count = mono_w32process_ver_language_name (lang & 0xFFFF, lang_buf, 128);
			if (lang_count) {
				process_set_field_string (filever, "language", lang_buf, lang_count, error);
				if (!is_ok (error))
					goto cleanup;
			}
			process_module_stringtable (filever, data, trans_data[0], trans_data[1], error);
			if (!is_ok (error))
				goto cleanup;
		}
	} else {
		int i;

		for (i = 0; i < G_N_ELEMENTS (stringtable_entries); ++i) {
			/* No strings, so set every field to the empty string */
			process_set_field_string (filever, stringtable_entries [i].name, EMPTY_STRING, 0, error);
			if (!is_ok (error))
				goto cleanup;
		}

		/* And language seems to be set to en_US according to bug 374600 */
		lang_count = mono_w32process_ver_language_name (0x0409, lang_buf, 128);
		if (lang_count) {
			process_set_field_string (filever, "language", lang_buf, lang_count, error);
			if (!is_ok (error))
				goto cleanup;
		}
	}

cleanup:
	g_free (query);
	g_free (data);
}

#endif /* #if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

void
ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this_obj, MonoString *filename)
{
	MonoError error;

	stash_system_image (mono_object_class (this_obj)->image);

	mono_w32process_get_fileversion (this_obj, mono_string_chars (filename), &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return;
	}

	process_set_field_string (this_obj, "filename", mono_string_chars (filename), mono_string_length (filename), &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
	}
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)

static GPtrArray*
get_domain_assemblies (MonoDomain *domain)
{
	GSList *tmp;
	GPtrArray *assemblies;

	/*
	 * Make a copy of the list of assemblies because we can't hold the assemblies
	 * lock while creating objects etc.
	 */
	assemblies = g_ptr_array_new ();
	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *ass = (MonoAssembly *)tmp->data;
		if (ass->image->fileio_used)
			continue;
		g_ptr_array_add (assemblies, ass);
	}
	mono_domain_assemblies_unlock (domain);

	return assemblies;
}

static MonoObject*
process_add_module (HANDLE process, HMODULE mod, gunichar2 *filename, gunichar2 *modulename, MonoClass *proc_class, MonoError *error)
{
	MonoObject *item, *filever;
	MonoDomain *domain = mono_domain_get ();
	MODULEINFO modinfo;
	BOOL ok;

	error_init (error);

	/* Build a System.Diagnostics.ProcessModule with the data. */
	item = mono_object_new_checked (domain, proc_class, error);
	return_val_if_nok (error, NULL);

	filever = mono_object_new_checked (domain, get_file_version_info_class (), error);
	return_val_if_nok (error, NULL);

	mono_w32process_get_fileversion (filever, filename, error);
	return_val_if_nok (error, NULL);

	process_set_field_string (filever, "filename", filename, unicode_chars (filename), error);
	return_val_if_nok (error, NULL);

	ok = mono_w32process_module_get_information (process, mod, &modinfo, sizeof(MODULEINFO));
	if (ok) {
		process_set_field_intptr (item, "baseaddr", modinfo.lpBaseOfDll);
		process_set_field_intptr (item, "entryaddr", modinfo.EntryPoint);
		process_set_field_int (item, "memory_size", modinfo.SizeOfImage);
	}

	process_set_field_string (item, "filename", filename, unicode_chars (filename), error);
	return_val_if_nok (error, NULL);

	process_set_field_string (item, "modulename", modulename, unicode_chars (modulename), error);
	return_val_if_nok (error, NULL);

	process_set_field_object (item, "version_info", filever);

	return item;
}

static void
process_get_assembly_fileversion (MonoObject *filever, MonoAssembly *assembly)
{
	process_set_field_int (filever, "filemajorpart", assembly->aname.major);
	process_set_field_int (filever, "fileminorpart", assembly->aname.minor);
	process_set_field_int (filever, "filebuildpart", assembly->aname.build);
}

static MonoObject*
process_get_module (MonoAssembly *assembly, MonoClass *proc_class, MonoError *error)
{
	MonoObject *item, *filever;
	MonoDomain *domain;
	gchar *filename;
	const gchar *modulename;

	error_init (error);

	domain = mono_domain_get ();

	modulename = assembly->aname.name;

	/* Build a System.Diagnostics.ProcessModule with the data. */
	item = mono_object_new_checked (domain, proc_class, error);
	return_val_if_nok (error, NULL);

	filever = mono_object_new_checked (domain, get_file_version_info_class (), error);
	return_val_if_nok (error, NULL);

	filename = g_strdup_printf ("[In Memory] %s", modulename);

	process_get_assembly_fileversion (filever, assembly);
	process_set_field_string_char (filever, "filename", filename, error);
	return_val_if_nok (error, NULL);
	process_set_field_object (item, "version_info", filever);

	process_set_field_intptr (item, "baseaddr", assembly->image->raw_data);
	process_set_field_int (item, "memory_size", assembly->image->raw_data_len);
	process_set_field_string_char (item, "filename", filename, error);
	return_val_if_nok (error, NULL);
	process_set_field_string_char (item, "modulename", modulename, error);
	return_val_if_nok (error, NULL);

	g_free (filename);

	return item;
}

/* Returns an array of System.Diagnostics.ProcessModule */
MonoArray *
ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this_obj, HANDLE process)
{
	MonoError error;
	MonoArray *temp_arr = NULL;
	MonoArray *arr;
	HMODULE mods[1024];
	gunichar2 filename[MAX_PATH];
	gunichar2 modname[MAX_PATH];
	DWORD needed;
	guint32 count = 0, module_count = 0, assembly_count = 0;
	guint32 i, num_added = 0;
	GPtrArray *assemblies = NULL;

	stash_system_image (mono_object_class (this_obj)->image);

	if (mono_w32process_get_pid (process) == mono_process_current_pid ()) {
		assemblies = get_domain_assemblies (mono_domain_get ());
		assembly_count = assemblies->len;
	}

	if (mono_w32process_try_get_modules (process, mods, sizeof(mods), &needed))
		module_count += needed / sizeof(HMODULE);

	count = module_count + assembly_count;
	temp_arr = mono_array_new_checked (mono_domain_get (), get_process_module_class (), count, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	for (i = 0; i < module_count; i++) {
		if (mono_w32process_module_get_name (process, mods[i], modname, MAX_PATH)
			 && mono_w32process_module_get_filename (process, mods[i], filename, MAX_PATH))
		{
			MonoObject *module = process_add_module (process, mods[i], filename, modname, get_process_module_class (), &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return NULL;
			}
			mono_array_setref (temp_arr, num_added++, module);
		}
	}

	if (assemblies) {
		for (i = 0; i < assembly_count; i++) {
			MonoAssembly *ass = (MonoAssembly *)g_ptr_array_index (assemblies, i);
			MonoObject *module = process_get_module (ass, get_process_module_class (), &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return NULL;
			}
			mono_array_setref (temp_arr, num_added++, module);
		}
		g_ptr_array_free (assemblies, TRUE);
	}

	if (count == num_added) {
		arr = temp_arr;
	} else {
		/* shorter version of the array */
		arr = mono_array_new_checked (mono_domain_get (), get_process_module_class (), num_added, &error);
		if (mono_error_set_pending_exception (&error))
			return NULL;

		for (i = 0; i < num_added; i++)
			mono_array_setref (arr, i, mono_array_get (temp_arr, MonoObject*, i));
	}

	return arr;
}

#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)

MonoString *
ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process)
{
	MonoError error;
	MonoString *string;
	gunichar2 name[MAX_PATH];
	guint32 len;
	gboolean ok;
	HMODULE mod;
	DWORD needed;

	ok = mono_w32process_try_get_modules (process, &mod, sizeof(mod), &needed);
	if (!ok)
		return NULL;

	len = mono_w32process_module_get_name (process, mod, name, MAX_PATH);
	if (len == 0)
		return NULL;

	string = mono_string_new_utf16_checked (mono_domain_get (), name, len, &error);
	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);

	return string;
}

#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

gint64
ves_icall_System_Diagnostics_Process_GetProcessData (int pid, gint32 data_type, gint32 *error)
{
	MonoProcessError perror;
	guint64 res;

	res = mono_process_get_data_with_error (GINT_TO_POINTER (pid), (MonoProcessData)data_type, &perror);
	if (error)
		*error = perror;
	return res;
}
