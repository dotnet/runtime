/*
 * process.c: System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright 2002 Ximian, Inc.
 * Copyright 2002-2006 Novell, Inc.
 */

#include <config.h>

#include <glib.h>
#include <string.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/process.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threadpool-ms-io.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-proclib.h>
#include <mono/io-layer/io-layer.h>
/* FIXME: fix this code to not depend so much on the internals */
#include <mono/metadata/class-internals.h>

#define LOGDEBUG(...)  
/* define LOGDEBUG(...) g_message(__VA_ARGS__)  */

#ifdef _WIN32
#include <shellapi.h>
#endif

HANDLE
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid)
{
	HANDLE handle;
	
	/* GetCurrentProcess returns a pseudo-handle, so use
	 * OpenProcess instead
	 */
	handle = OpenProcess (PROCESS_ALL_ACCESS, TRUE, pid);
	if (handle == NULL)
		/* FIXME: Throw an exception */
		return NULL;
	return handle;
}

static MonoImage *system_assembly;

static void
stash_system_assembly (MonoObject *obj)
{
	if (!system_assembly)
		system_assembly = obj->vtable->klass->image;
}

//Hand coded version that loads from system
static MonoClass*
mono_class_get_file_version_info_class (void)
{
	static MonoClass *tmp_class;
	MonoClass *klass = tmp_class;
	if (!klass) {
		klass = mono_class_load_from_name (system_assembly, "System.Diagnostics", "FileVersionInfo");
		mono_memory_barrier ();
		tmp_class = klass;
	}
	return klass;
}

static MonoClass*
mono_class_get_process_module_class (void)
{
	static MonoClass *tmp_class;
	MonoClass *klass = tmp_class;
	if (!klass) {
		klass = mono_class_load_from_name (system_assembly, "System.Diagnostics", "ProcessModule");
		mono_memory_barrier ();
		tmp_class = klass;
	}
	return klass;
}

static guint32
unicode_chars (const gunichar2 *str)
{
	guint32 len;

	for (len = 0; str [len] != '\0'; ++len)
		;
	return len;
}

static void
process_set_field_object (MonoObject *obj, const gchar *fieldname,
						  MonoObject *data)
{
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to object at %p", __func__, fieldname, data));

	field = mono_class_get_field_from_name (mono_object_class (obj),
											fieldname);
	mono_gc_wbarrier_generic_store (((char *)obj) + field->offset, data);
}

static void
process_set_field_string (MonoObject *obj, const gchar *fieldname,
						  const gunichar2 *val, guint32 len, MonoError *error)
{
	MonoClassField *field;
	MonoString *string;

	mono_error_init (error);

	LOGDEBUG (g_message ("%s: Setting field %s to [%s]", __func__, fieldname, g_utf16_to_utf8 (val, len, NULL, NULL, NULL)));

	string = mono_string_new_utf16_checked (mono_object_domain (obj), val, len, error);
	if (!mono_error_ok (error))
		return;
	
	field = mono_class_get_field_from_name (mono_object_class (obj),
											fieldname);
	mono_gc_wbarrier_generic_store (((char *)obj) + field->offset, (MonoObject*)string);
}

static void
process_set_field_string_char (MonoObject *obj, const gchar *fieldname,
							   const gchar *val)
{
	MonoClassField *field;
	MonoString *string;

	LOGDEBUG (g_message ("%s: Setting field %s to [%s]", __func__, fieldname, val));

	string = mono_string_new (mono_object_domain (obj), val);
	
	field = mono_class_get_field_from_name (mono_object_class (obj), fieldname);
	mono_gc_wbarrier_generic_store (((char *)obj) + field->offset, (MonoObject*)string);
}

static void
process_set_field_int (MonoObject *obj, const gchar *fieldname,
					   guint32 val)
{
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to %d", __func__,fieldname, val));
	
	field = mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	*(guint32 *)(((char *)obj) + field->offset)=val;
}

static void
process_set_field_intptr (MonoObject *obj, const gchar *fieldname,
						  gpointer val)
{
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to %p", __func__, fieldname, val));
	
	field = mono_class_get_field_from_name (mono_object_class (obj),
											fieldname);
	*(gpointer *)(((char *)obj) + field->offset) = val;
}

static void
process_set_field_bool (MonoObject *obj, const gchar *fieldname,
						gboolean val)
{
	MonoClassField *field;

	LOGDEBUG (g_message ("%s: Setting field %s to %s", __func__, fieldname, val ? "TRUE":"FALSE"));
	
	field = mono_class_get_field_from_name (mono_object_class (obj),
											fieldname);
	*(guint8 *)(((char *)obj) + field->offset) = val;
}

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

static void
process_module_string_read (MonoObject *filever, gpointer data,
							const gchar *fieldname,
							guchar lang_hi, guchar lang_lo,
							const gchar *key, MonoError *error)
{
	gchar *lang_key_utf8;
	gunichar2 *lang_key, *buffer;
	UINT chars;

	mono_error_init (error);

	lang_key_utf8 = g_strdup_printf (key, lang_lo, lang_hi, 0x04, 0xb0);

	LOGDEBUG (g_message ("%s: asking for [%s]", __func__, lang_key_utf8));

	lang_key = g_utf8_to_utf16 (lang_key_utf8, -1, NULL, NULL, NULL);

	if (VerQueryValue (data, lang_key, (gpointer *)&buffer, &chars) && chars > 0) {
		LOGDEBUG (g_message ("%s: found %d chars of [%s]", __func__, chars, g_utf16_to_utf8 (buffer, chars, NULL, NULL, NULL)));
		/* chars includes trailing null */
		process_set_field_string (filever, fieldname, buffer, chars - 1, error);
	} else {
		process_set_field_string (filever, fieldname, EMPTY_STRING, 0, error);
	}

	g_free (lang_key);
	g_free (lang_key_utf8);
}

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
process_module_stringtable (MonoObject *filever, gpointer data,
							guchar lang_hi, guchar lang_lo, MonoError *error)
{
	int i;

	for (i = 0; i < G_N_ELEMENTS (stringtable_entries); ++i) {
		process_module_string_read (filever, data, stringtable_entries [i].name, lang_hi, lang_lo,
									stringtable_entries [i].id, error);
		if (!mono_error_ok (error))
			return;
	}
}

static void
process_get_fileversion (MonoObject *filever, gunichar2 *filename, MonoError *error)
{
	DWORD verinfohandle;
	VS_FIXEDFILEINFO *ffi;
	gpointer data;
	DWORD datalen;
	guchar *trans_data;
	gunichar2 *query;
	UINT ffi_size, trans_size;
	BOOL ok;
	gunichar2 lang_buf[128];
	guint32 lang, lang_count;

	mono_error_init (error);

	datalen = GetFileVersionInfoSize (filename, &verinfohandle);
	if (datalen) {
		data = g_malloc0 (datalen);
		ok = GetFileVersionInfo (filename, verinfohandle, datalen,
					 data);
		if (ok) {
			query = g_utf8_to_utf16 ("\\", -1, NULL, NULL, NULL);
			if (query == NULL) {
				g_free (data);
				return;
			}
			
			if (VerQueryValue (data, query, (gpointer *)&ffi,
			    &ffi_size)) {
				LOGDEBUG (g_message ("%s: recording assembly: FileName [%s] FileVersionInfo [%d.%d.%d.%d]", __func__, g_utf16_to_utf8 (filename, -1, NULL, NULL, NULL), HIWORD (ffi->dwFileVersionMS), LOWORD (ffi->dwFileVersionMS), HIWORD (ffi->dwFileVersionLS), LOWORD (ffi->dwFileVersionLS)));
	
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
			}
			g_free (query);

			query = g_utf8_to_utf16 ("\\VarFileInfo\\Translation", -1, NULL, NULL, NULL);
			if (query == NULL) {
				g_free (data);
				return;
			}
			
			if (VerQueryValue (data, query,
					   (gpointer *)&trans_data,
					   &trans_size)) {
				/* use the first language ID we see
				 */
				if (trans_size >= 4) {
		 			LOGDEBUG (g_message("%s: %s has 0x%0x 0x%0x 0x%0x 0x%0x", __func__, g_utf16_to_utf8 (filename, -1, NULL, NULL, NULL), trans_data[0], trans_data[1], trans_data[2], trans_data[3]));
					lang = (trans_data[0]) |
						(trans_data[1] << 8) |
						(trans_data[2] << 16) |
						(trans_data[3] << 24);
					/* Only give the lower 16 bits
					 * to VerLanguageName, as
					 * Windows gets confused
					 * otherwise
					 */
					lang_count = VerLanguageName (lang & 0xFFFF, lang_buf, 128);
					if (lang_count) {
						process_set_field_string (filever, "language", lang_buf, lang_count, error);
						if (!mono_error_ok (error))
							return;
					}
					process_module_stringtable (filever, data, trans_data[0], trans_data[1], error);
					if (!mono_error_ok (error))
						return;
				}
			} else {
				int i;

				for (i = 0; i < G_N_ELEMENTS (stringtable_entries); ++i) {
					/* No strings, so set every field to
					 * the empty string
					 */
					process_set_field_string (filever,
											  stringtable_entries [i].name,
											  EMPTY_STRING, 0, error);
					if (!mono_error_ok (error))
						return;
				}

				/* And language seems to be set to
				 * en_US according to bug 374600
				 */
				lang_count = VerLanguageName (0x0409, lang_buf, 128);
				if (lang_count) {
					process_set_field_string (filever, "language", lang_buf, lang_count, error);
					if (!mono_error_ok (error))
						return;
				}
			}
			
			g_free (query);
		}
		g_free (data);
	}
}

static void
process_get_assembly_fileversion (MonoObject *filever, MonoAssembly *assembly)
{
	process_set_field_int (filever, "filemajorpart", assembly->aname.major);
	process_set_field_int (filever, "fileminorpart", assembly->aname.minor);
	process_set_field_int (filever, "filebuildpart", assembly->aname.build);
}

static MonoObject*
get_process_module (MonoAssembly *assembly, MonoClass *proc_class, MonoError *error)
{
	MonoObject *item, *filever;
	MonoDomain *domain = mono_domain_get ();
	char *filename;
	const char *modulename = assembly->aname.name;

	mono_error_init (error);

	/* Build a System.Diagnostics.ProcessModule with the data.
	 */
	item = mono_object_new_checked (domain, proc_class, error);
	if (!mono_error_ok (error))
		return NULL;
	filever = mono_object_new_checked (domain, mono_class_get_file_version_info_class (), error);
	if (!mono_error_ok (error))
		return NULL;

	filename = g_strdup_printf ("[In Memory] %s", modulename);

	process_get_assembly_fileversion (filever, assembly);
	process_set_field_string_char (filever, "filename", filename);
	process_set_field_object (item, "version_info", filever);

	process_set_field_intptr (item, "baseaddr", assembly->image->raw_data);
	process_set_field_int (item, "memory_size", assembly->image->raw_data_len);
	process_set_field_string_char (item, "filename", filename);
	process_set_field_string_char (item, "modulename", modulename);

	g_free (filename);

	return item;
}

static MonoObject*
process_add_module (HANDLE process, HMODULE mod, gunichar2 *filename, gunichar2 *modulename, MonoClass *proc_class, MonoError *error)
{
	MonoObject *item, *filever;
	MonoDomain *domain = mono_domain_get ();
	MODULEINFO modinfo;
	BOOL ok;

	mono_error_init (error);

	/* Build a System.Diagnostics.ProcessModule with the data.
	 */
	item = mono_object_new_checked (domain, proc_class, error);
	if (!mono_error_ok (error))
		return NULL;
	filever = mono_object_new_checked (domain, mono_class_get_file_version_info_class (), error);
	if (!mono_error_ok (error))
		return NULL;

	process_get_fileversion (filever, filename, error);
	if (!mono_error_ok (error))
		return NULL;

	process_set_field_string (filever, "filename", filename,
							  unicode_chars (filename), error);
	if (!mono_error_ok (error))
		return NULL;
	ok = GetModuleInformation (process, mod, &modinfo, sizeof(MODULEINFO));
	if (ok) {
		process_set_field_intptr (item, "baseaddr",
					  modinfo.lpBaseOfDll);
		process_set_field_intptr (item, "entryaddr",
					  modinfo.EntryPoint);
		process_set_field_int (item, "memory_size",
				       modinfo.SizeOfImage);
	}
	process_set_field_string (item, "filename", filename,
							  unicode_chars (filename), error);
	if (!mono_error_ok (error))
		return NULL;
	process_set_field_string (item, "modulename", modulename,
							  unicode_chars (modulename), error);
	if (!mono_error_ok (error))
		return NULL;
	process_set_field_object (item, "version_info", filever);

	return item;
}

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
	static HANDLE current_process = 0;
	
	if (current_process == 0) {
		int pid = mono_process_current_pid ();
		current_process = ves_icall_System_Diagnostics_Process_GetProcess_internal (pid);
	}

	stash_system_assembly (this_obj);

	if (process == current_process) {
		assemblies = get_domain_assemblies (mono_domain_get ());
		assembly_count = assemblies->len;
	}

	if (EnumProcessModules (process, mods, sizeof(mods), &needed)) {
		module_count += needed / sizeof(HMODULE);
	}

	count = module_count + assembly_count; 
	temp_arr = mono_array_new (mono_domain_get (), mono_class_get_process_module_class (), count);

	for (i = 0; i < module_count; i++) {
		if (GetModuleBaseName (process, mods[i], modname, MAX_PATH) &&
				GetModuleFileNameEx (process, mods[i], filename, MAX_PATH)) {
			MonoObject *module = process_add_module (process, mods[i],
													 filename, modname, mono_class_get_process_module_class (), &error);
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
			MonoObject *module = get_process_module (ass, mono_class_get_process_module_class (), &error);
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
		arr = mono_array_new (mono_domain_get (), mono_class_get_process_module_class (), num_added);

		for (i = 0; i < num_added; i++)
			mono_array_setref (arr, i, mono_array_get (temp_arr, MonoObject*, i));
	}

	return arr;
}

void
ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this_obj, MonoString *filename)
{
	MonoError error;

	stash_system_assembly (this_obj);
	
	process_get_fileversion (this_obj, mono_string_chars (filename), &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return;
	}
	process_set_field_string (this_obj, "filename",
							  mono_string_chars (filename),
							  mono_string_length (filename), &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return;
	}
}

/* Only used when UseShellExecute is false */
static gchar *
quote_path (const gchar *path)
{
	gchar *res = g_shell_quote (path);
#ifdef TARGET_WIN32
	{
	gchar *q = res;
	while (*q) {
		if (*q == '\'')
			*q = '\"';
		q++;
	}
	}
#endif
	return res;
}

/* Only used when UseShellExecute is false */
static gboolean
complete_path (const gunichar2 *appname, gchar **completed)
{
	gchar *utf8app, *utf8appmemory;
	gchar *found;

	utf8appmemory = utf8app = g_utf16_to_utf8 (appname, -1, NULL, NULL, NULL);
#ifdef TARGET_WIN32 // Should this happen on all platforms? 
	{
		// remove the quotes around utf8app.
		size_t len;
		len = strlen (utf8app);
		if (len) {
			if (utf8app[len-1] == '\"')
				utf8app[len-1] = '\0';
			if (utf8app[0] == '\"')
				utf8app++;
		}
	}
#endif

	if (g_path_is_absolute (utf8app)) {
		*completed = quote_path (utf8app);
		g_free (utf8appmemory);
		return TRUE;
	}

	if (g_file_test (utf8app, G_FILE_TEST_IS_EXECUTABLE) && !g_file_test (utf8app, G_FILE_TEST_IS_DIR)) {
		*completed = quote_path (utf8app);
		g_free (utf8appmemory);
		return TRUE;
	}
	
	found = g_find_program_in_path (utf8app);
	if (found == NULL) {
		*completed = NULL;
		g_free (utf8appmemory);
		return FALSE;
	}

	*completed = quote_path (found);
	g_free (found);
	g_free (utf8appmemory);
	return TRUE;
}

MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoProcessStartInfo *proc_start_info, MonoProcInfo *process_info)
{
	SHELLEXECUTEINFO shellex = {0};
	gboolean ret;

	shellex.cbSize = sizeof(SHELLEXECUTEINFO);
	shellex.fMask = (gulong)(SEE_MASK_FLAG_DDEWAIT | SEE_MASK_NOCLOSEPROCESS | SEE_MASK_UNICODE);
	shellex.nShow = (gulong)proc_start_info->window_style;
	shellex.nShow = (gulong)((shellex.nShow == 0) ? 1 : (shellex.nShow == 1 ? 0 : shellex.nShow));

	if (proc_start_info->filename != NULL) {
		shellex.lpFile = mono_string_chars (proc_start_info->filename);
	}

	if (proc_start_info->arguments != NULL) {
		shellex.lpParameters = mono_string_chars (proc_start_info->arguments);
	}

	if (proc_start_info->verb != NULL &&
	    mono_string_length (proc_start_info->verb) != 0) {
		shellex.lpVerb = mono_string_chars (proc_start_info->verb);
	}

	if (proc_start_info->working_directory != NULL &&
	    mono_string_length (proc_start_info->working_directory) != 0) {
		shellex.lpDirectory = mono_string_chars (proc_start_info->working_directory);
	}

	if (proc_start_info->error_dialog) {	
		shellex.hwnd = proc_start_info->error_dialog_parent_handle;
	} else {
		shellex.fMask = (gulong)(shellex.fMask | SEE_MASK_FLAG_NO_UI);
	}

	ret = ShellExecuteEx (&shellex);
	if (ret == FALSE) {
		process_info->pid = -GetLastError ();
	} else {
		process_info->process_handle = shellex.hProcess;
		process_info->thread_handle = NULL;
#if !defined(MONO_CROSS_COMPILE)
		process_info->pid = GetProcessId (shellex.hProcess);
#else
		process_info->pid = 0;
#endif
		process_info->tid = 0;
	}

	return ret;
}

MonoBoolean
ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoProcessStartInfo *proc_start_info, HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoProcInfo *process_info)
{
	gboolean ret;
	gunichar2 *dir;
	STARTUPINFO startinfo={0};
	PROCESS_INFORMATION procinfo;
	gunichar2 *shell_path = NULL;
	gchar *env_vars = NULL;
	gboolean free_shell_path = TRUE;
	gchar *spath = NULL;
	MonoString *cmd = proc_start_info->arguments;
	guint32 creation_flags, logon_flags;
	
	startinfo.cb = sizeof(STARTUPINFO);
	startinfo.dwFlags = STARTF_USESTDHANDLES;
	startinfo.hStdInput = stdin_handle;
	startinfo.hStdOutput = stdout_handle;
	startinfo.hStdError = stderr_handle;

	creation_flags = CREATE_UNICODE_ENVIRONMENT;
	if (proc_start_info->create_no_window)
		creation_flags |= CREATE_NO_WINDOW;
	
	shell_path = mono_string_chars (proc_start_info->filename);
	complete_path (shell_path, &spath);
	if (spath == NULL) {
		process_info->pid = -ERROR_FILE_NOT_FOUND;
		return FALSE;
	}
#ifdef TARGET_WIN32
	/* Seems like our CreateProcess does not work as the windows one.
	 * This hack is needed to deal with paths containing spaces */
	shell_path = NULL;
	free_shell_path = FALSE;
	if (cmd) {
		gchar *newcmd, *tmp;
		tmp = mono_string_to_utf8 (cmd);
		newcmd = g_strdup_printf ("%s %s", spath, tmp);
		cmd = mono_string_new_wrapper (newcmd);
		g_free (tmp);
		g_free (newcmd);
	}
	else {
		cmd = mono_string_new_wrapper (spath);
	}
#else
	shell_path = g_utf8_to_utf16 (spath, -1, NULL, NULL, NULL);
#endif
	g_free (spath);

	if (process_info->env_keys) {
		gint i, len; 
		MonoString *ms;
		MonoString *key, *value;
		gunichar2 *str, *ptr;
		gunichar2 *equals16;

		for (len = 0, i = 0; i < mono_array_length (process_info->env_keys); i++) {
			ms = mono_array_get (process_info->env_values, MonoString *, i);
			if (ms == NULL)
				continue;

			len += mono_string_length (ms) * sizeof (gunichar2);
			ms = mono_array_get (process_info->env_keys, MonoString *, i);
			len += mono_string_length (ms) * sizeof (gunichar2);
			len += 2 * sizeof (gunichar2);
		}

		equals16 = g_utf8_to_utf16 ("=", 1, NULL, NULL, NULL);
		ptr = str = g_new0 (gunichar2, len + 1);
		for (i = 0; i < mono_array_length (process_info->env_keys); i++) {
			value = mono_array_get (process_info->env_values, MonoString *, i);
			if (value == NULL)
				continue;

			key = mono_array_get (process_info->env_keys, MonoString *, i);
			memcpy (ptr, mono_string_chars (key), mono_string_length (key) * sizeof (gunichar2));
			ptr += mono_string_length (key);

			memcpy (ptr, equals16, sizeof (gunichar2));
			ptr++;

			memcpy (ptr, mono_string_chars (value), mono_string_length (value) * sizeof (gunichar2));
			ptr += mono_string_length (value);
			ptr++;
		}

		g_free (equals16);
		env_vars = (gchar *) str;
	}
	
	/* The default dir name is "".  Turn that into NULL to mean
	 * "current directory"
	 */
	if (proc_start_info->working_directory == NULL || mono_string_length (proc_start_info->working_directory) == 0)
		dir = NULL;
	else
		dir = mono_string_chars (proc_start_info->working_directory);

	if (process_info->username) {
		logon_flags = process_info->load_user_profile ? LOGON_WITH_PROFILE : 0;
		ret = CreateProcessWithLogonW (
			mono_string_chars (process_info->username),
			process_info->domain ? mono_string_chars (process_info->domain) : NULL,
			(const gunichar2 *)process_info->password, logon_flags, shell_path,
			cmd ? mono_string_chars (cmd) : NULL,
			creation_flags, env_vars, dir, &startinfo, &procinfo);
	} else {
		ret = CreateProcess (shell_path, cmd ? mono_string_chars (cmd): NULL, NULL, NULL, TRUE, creation_flags, env_vars, dir, &startinfo, &procinfo);
	}

	g_free (env_vars);
	if (free_shell_path)
		g_free (shell_path);

	if (ret) {
		process_info->process_handle = procinfo.hProcess;
		/*process_info->thread_handle=procinfo.hThread;*/
		process_info->thread_handle = NULL;
		if (procinfo.hThread != NULL && procinfo.hThread != INVALID_HANDLE_VALUE)
			CloseHandle (procinfo.hThread);
		process_info->pid = procinfo.dwProcessId;
		process_info->tid = procinfo.dwThreadId;
	} else {
		process_info->pid = -GetLastError ();
	}
	
	return ret;
}

MonoString *
ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process)
{
	MonoError error;
	MonoString *string;
	gboolean ok;
	HMODULE mod;
	gunichar2 name[MAX_PATH];
	DWORD needed;
	guint32 len;
	
	ok = EnumProcessModules (process, &mod, sizeof(mod), &needed);
	if (!ok)
		return NULL;
	
	len = GetModuleBaseName (process, mod, name, MAX_PATH);
	if (len == 0)
		return NULL;
	
	LOGDEBUG (g_message ("%s: process name is [%s]", __func__, g_utf16_to_utf8 (name, -1, NULL, NULL, NULL)));
	
	string = mono_string_new_utf16_checked (mono_domain_get (), name, len, &error);
	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);
	
	return string;
}

/* Returns an array of pids */
MonoArray *
ves_icall_System_Diagnostics_Process_GetProcesses_internal (void)
{
#if !defined(HOST_WIN32)
	MonoArray *procs;
	gpointer *pidarray;
	int i, count;

	pidarray = mono_process_list (&count);
	if (!pidarray) {
		mono_set_pending_exception (mono_get_exception_not_supported ("This system does not support EnumProcesses"));
		return NULL;
	}
	procs = mono_array_new (mono_domain_get (), mono_get_int32_class (), count);
	if (sizeof (guint32) == sizeof (gpointer)) {
		memcpy (mono_array_addr (procs, guint32, 0), pidarray, count * sizeof (gint32));
	} else {
		for (i = 0; i < count; ++i)
			*(mono_array_addr (procs, guint32, i)) = GPOINTER_TO_UINT (pidarray [i]);
	}
	g_free (pidarray);

	return procs;
#else
	MonoArray *procs;
	gboolean ret;
	DWORD needed;
	int count;
	guint32 *pids;

	count = 512;
	do {
		pids = g_new0 (guint32, count);
		ret = EnumProcesses (pids, count * sizeof (guint32), &needed);
		if (ret == FALSE) {
			MonoException *exc;

			g_free (pids);
			pids = NULL;
			exc = mono_get_exception_not_supported ("This system does not support EnumProcesses");
			mono_set_pending_exception (exc);
			return NULL;
		}
		if (needed < (count * sizeof (guint32)))
			break;
		g_free (pids);
		pids = NULL;
		count = (count * 3) / 2;
	} while (TRUE);

	count = needed / sizeof (guint32);
	procs = mono_array_new (mono_domain_get (), mono_get_int32_class (), count);
	memcpy (mono_array_addr (procs, guint32, 0), pids, needed);
	g_free (pids);
	pids = NULL;
	
	return procs;
#endif
}

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
