/*
 * process.c: System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>

#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/metadata/process.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/cil-coff.h>
#include <mono/io-layer/io-layer.h>

#undef DEBUG

HANDLE ves_icall_System_Diagnostics_Process_GetCurrentProcess_internal (void)
{
	HANDLE handle;
	
	/* GetCurrentProcess returns a pseudo-handle, so use
	 * OpenProcess instead
	 */
	handle=OpenProcess (PROCESS_ALL_ACCESS, TRUE, GetCurrentProcessId ());
	
	if(handle==NULL) {
		/* FIXME: Throw an exception */
		return(NULL);
	}
	
	return(handle);
}

guint32 ves_icall_System_Diagnostics_Process_GetPid_internal (void)
{
	return(GetCurrentProcessId ());
}

void ves_icall_System_Diagnostics_Process_Process_free_internal (MonoObject *this,
								 HANDLE process)
{
#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Closing process %p, handle %p",
		   this, process);
#endif

	CloseHandle (process);
}

#define STASH_SYS_ASS(this) \
	if(system_assembly == NULL) { \
		system_assembly=this->vtable->klass->image; \
	}

static MonoImage *system_assembly=NULL;

static guint32 unicode_chars (const gunichar2 *str)
{
	guint32 len=0;
	
	do {
		if(str[len]=='\0') {
			return(len);
		}
		len++;
	} while(1);
}

static guint32 unicode_bytes (const gunichar2 *str)
{
	guint32 len=0;
	
	do {
		if(str[len]=='\0') {
			/* Include the terminators */
			return((len*2)+2);
		}
		len++;
	} while(1);
}

static void process_set_field_object (MonoObject *obj, const guchar *fieldname,
				      MonoObject *data)
{
	MonoClassField *field;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Setting field %s to object at %p",
		   fieldname, data);
#endif

	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	*(MonoObject **)(((char *)obj) + field->offset)=data;
}

static void process_set_field_string (MonoObject *obj, const guchar *fieldname,
				      const gunichar2 *val, guint32 len)
{
	MonoClassField *field;
	MonoString *string;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Setting field %s to [%s]",
		   fieldname, g_utf16_to_utf8 (val, len, NULL, NULL, NULL));
#endif

	string=mono_string_new_utf16 (mono_object_domain (obj), val, len);
	
	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	*(MonoString **)(((char *)obj) + field->offset)=string;
}

static void process_set_field_string_utf8 (MonoObject *obj,
					   const guchar *fieldname,
					   const guchar *val)
{
	MonoClassField *field;
	MonoString *string;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Setting field %s to [%s]",
		   fieldname, val);
#endif

	string=mono_string_new (mono_object_domain (obj), val);
	
	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	*(MonoString **)(((char *)obj) + field->offset)=string;
}

static void process_set_field_int (MonoObject *obj, const guchar *fieldname,
				   guint32 val)
{
	MonoClassField *field;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Setting field %s to %d",
		   fieldname, val);
#endif
	
	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	*(guint32 *)(((char *)obj) + field->offset)=val;
}

static void process_set_field_bool (MonoObject *obj, const guchar *fieldname,
				    gboolean val)
{
	MonoClassField *field;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Setting field %s to %s",
		   fieldname, val?"TRUE":"FALSE");
#endif
	
	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	*(guint8 *)(((char *)obj) + field->offset)=val;
}

typedef struct {
	guint16 data_len;
	guint16 value_len;
	guint16 type;
	gunichar2 *key;
} version_data;

/* Returns a pointer to the value data, because theres no way to know
 * how big that data is (value_len is set to zero for most blocks :-()
 */
static gpointer process_get_versioninfo_block (gpointer data,
					       version_data *block)
{
	block->data_len=*(((guint16 *)data)++);
	block->value_len=*(((guint16 *)data)++);

	/* No idea what the type is supposed to indicate */
	block->type=*(((guint16 *)data)++);
	block->key=((gunichar2 *)data);

	/* skip over the key (including the terminator) */
	data=((gunichar2 *)data)+(unicode_chars (block->key)+1);

	/* align on a 32-bit boundary */
	data=(gpointer)(((unsigned)data+3) & (~3));
	
	return(data);
}

/* Returns a pointer to the byte following the Var block */
static gpointer process_read_var_block (MonoObject *filever, gpointer data_ptr,
					guint16 data_len)
{
	/* Not currently interested in the VarFileInfo block.  This
	 * might change if language support is needed for file version
	 * strings (VarFileInfo contains lists of supported
	 * languages.)
	 */
	version_data block;

	/* data_ptr is pointing at a Var block of length data_len */
	data_ptr=process_get_versioninfo_block (data_ptr, &block);
	data_ptr=((guchar *)data_ptr)+block.value_len;

	return(data_ptr);
}

/* Returns a pointer to the byte following the String block */
static gpointer process_read_string_block (MonoObject *filever,
					   gpointer data_ptr,
					   guint16 data_len,
					   gboolean store)
{
	version_data block;
	guint16 string_len=0;
	guchar comments_key[]= {'C', '\0', 'o', '\0', 'm', '\0',
				'm', '\0', 'e', '\0', 'n', '\0',
				't', '\0', 's', '\0', '\0', '\0'};
	guchar compname_key[]= {'C', '\0', 'o', '\0', 'm', '\0',
				'p', '\0', 'a', '\0', 'n', '\0',
				'y', '\0', 'N', '\0', 'a', '\0',
				'm', '\0', 'e', '\0', '\0', '\0'};
	guchar filedesc_key[]= {'F', '\0', 'i', '\0', 'l', '\0',
				'e', '\0', 'D', '\0', 'e', '\0',
				's', '\0', 'c', '\0', 'r', '\0',
				'i', '\0', 'p', '\0', 't', '\0',
				'i', '\0', 'o', '\0', 'n', '\0',
				'\0', '\0'};
	guchar filever_key[]= {'F', '\0', 'i', '\0', 'l', '\0',
			       'e', '\0', 'V', '\0', 'e', '\0',
			       'r', '\0', 's', '\0', 'i', '\0',
			       'o', '\0', 'n', '\0', '\0', '\0'};
	guchar internal_key[]= {'I', '\0', 'n', '\0', 't', '\0',
				'e', '\0', 'r', '\0', 'n', '\0',
				'a', '\0', 'l', '\0', 'N', '\0',
				'a', '\0', 'm', '\0', 'e', '\0',
				'\0', '\0'};
	guchar legalcpy_key[]= {'L', '\0', 'e', '\0', 'g', '\0',
				'a', '\0', 'l', '\0', 'C', '\0',
				'o', '\0', 'p', '\0', 'y', '\0',
				'r', '\0', 'i', '\0', 'g', '\0',
				'h', '\0', 't', '\0', '\0', '\0'};
	guchar legaltrade_key[]= {'L', '\0', 'e', '\0', 'g', '\0',
				  'a', '\0', 'l', '\0', 'T', '\0',
				  'r', '\0', 'a', '\0', 'd', '\0',
				  'e', '\0', 'm', '\0', 'a', '\0',
				  'r', '\0', 'k', '\0', 's', '\0',
				  '\0', '\0'};
	guchar origfile_key[]= {'O', '\0', 'r', '\0', 'i', '\0',
				'g', '\0', 'i', '\0', 'n', '\0',
				'a', '\0', 'l', '\0', 'F', '\0',
				'i', '\0', 'l', '\0', 'e', '\0',
				'n', '\0', 'a', '\0', 'm', '\0',
				'e', '\0', '\0', '\0'};
	guchar privbuild_key[]= {'P', '\0', 'r', '\0', 'i', '\0',
				 'v', '\0', 'a', '\0', 't', '\0',
				 'e', '\0', 'B', '\0', 'u', '\0',
				 'i', '\0', 'l', '\0', 'd', '\0',
				 '\0', '\0'};
	guchar prodname_key[]= {'P', '\0', 'r', '\0', 'o', '\0',
				'd', '\0', 'u', '\0', 'c', '\0',
				't', '\0', 'N', '\0', 'a', '\0',
				'm', '\0', 'e', '\0', '\0', '\0'};
	guchar prodver_key[]= {'P', '\0', 'r', '\0', 'o', '\0',
			       'd', '\0', 'u', '\0', 'c', '\0',
			       't', '\0', 'V', '\0', 'e', '\0',
			       'r', '\0', 's', '\0', 'i', '\0',
			       'o', '\0', 'n', '\0', '\0', '\0'};
	guchar specbuild_key[]= {'S', '\0', 'p', '\0', 'e', '\0',
				 'c', '\0', 'i', '\0', 'a', '\0',
				 'l', '\0', 'B', '\0', 'u', '\0',
				 'i', '\0', 'l', '\0', 'd', '\0',
				 '\0', '\0'};
	
	/* data_ptr is pointing at an array of one or more String
	 * blocks with total length (not including alignment padding)
	 * of data_len.
	 */
	while(string_len<data_len) {
		gunichar2 *value;
		
		/* align on a 32-bit boundary */
		data_ptr=(gpointer)(((unsigned)data_ptr+3) & (~3));

		data_ptr=process_get_versioninfo_block (data_ptr, &block);
		string_len=string_len+block.data_len;
		value=(gunichar2 *)data_ptr;
		/* Skip over the value */
		data_ptr=((gunichar2 *)data_ptr)+block.value_len;
		
		if(store==TRUE) {
			if(!memcmp (block.key, &comments_key,
				    unicode_bytes (block.key))) {
				process_set_field_string (filever, "comments", value, unicode_chars (value));
			} else if (!memcmp (block.key, &compname_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "companyname", value, unicode_chars (value));
			} else if (!memcmp (block.key, &filedesc_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "filedescription", value, unicode_chars (value));
			} else if (!memcmp (block.key, &filever_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "fileversion", value, unicode_chars (value));
			} else if (!memcmp (block.key, &internal_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "internalname", value, unicode_chars (value));
			} else if (!memcmp (block.key, &legalcpy_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "legalcopyright", value, unicode_chars (value));
			} else if (!memcmp (block.key, &legaltrade_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "legaltrademarks", value, unicode_chars (value));
			} else if (!memcmp (block.key, &origfile_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "originalfilename", value, unicode_chars (value));
			} else if (!memcmp (block.key, &privbuild_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "privatebuild", value, unicode_chars (value));
			} else if (!memcmp (block.key, &prodname_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "productname", value, unicode_chars (value));
			} else if (!memcmp (block.key, &prodver_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "productversion", value, unicode_chars (value));
			} else if (!memcmp (block.key, &specbuild_key,
					    unicode_bytes (block.key))) {
				process_set_field_string (filever, "specialbuild", value, unicode_chars (value));
			} else {
				/* Not an error, just not interesting
				 * in this case
				 */
			}
		}
	}
	
	return(data_ptr);
}

/* returns a pointer to the byte following the Stringtable block */
static gpointer process_read_stringtable_block (MonoObject *filever,
						gpointer data_ptr,
						guint16 data_len)
{
	version_data block;
	guint16 string_len=36;	/* length of the StringFileInfo block */

	/* Specifies language-neutral unicode string block */
	guchar uni_key[]= {'0', '\0', '0', '\0', '0', '\0', '0', '\0',
			   '0', '\0', '4', '\0', 'b', '\0', '0', '\0',
			   '\0', '\0'
	};
	guchar uni_key_uc[]= {'0', '\0', '0', '\0', '0', '\0', '0', '\0',
			      '0', '\0', '4', '\0', 'B', '\0', '0', '\0',
			      '\0', '\0'
	};
	
	/* data_ptr is pointing at an array of StringTable blocks,
	 * with total length (not including alignment padding) of
	 * data_len.
	 */

	while(string_len<data_len) {
		/* align on a 32-bit boundary */
		data_ptr=(gpointer)(((unsigned)data_ptr+3) & (~3));

		data_ptr=process_get_versioninfo_block (data_ptr, &block);
		string_len=string_len+block.data_len;
	
		if(!memcmp (block.key, &uni_key, unicode_bytes (block.key)) ||
		   !memcmp (block.key, &uni_key_uc, unicode_bytes (block.key))) {
			/* Got the one we're interested in */
			process_set_field_string_utf8 (filever, "language",
						       "Language Neutral");
			
			data_ptr=process_read_string_block (filever, data_ptr,
							    block.data_len,
							    TRUE);
		} else {
			/* Some other language.  We might want to do
			 * something with this in the future.
			 */
			data_ptr=process_read_string_block (filever, data_ptr,
							    block.data_len,
							    FALSE);
		}
	}
		
	return(data_ptr);
}

static void process_read_fixedfileinfo_block (MonoObject *filever,
					      VS_FIXEDFILEINFO *ffi)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": ffi: sig 0x%x, strucver 0x%x, fileverm 0x%x, fileverl 0x%x, prodverm 0x%x, prodverl 0x%x, ffmask 0x%x, ff 0x%x, os 0x%x, type 0x%x, subtype 0x%x, datem 0x%x, datel 0x%x", ffi->dwSignature, ffi->dwStrucVersion, ffi->dwFileVersionMS, ffi->dwFileVersionLS, ffi->dwProductVersionMS, ffi->dwProductVersionLS, ffi->dwFileFlagsMask, ffi->dwFileFlags, ffi->dwFileOS, ffi->dwFileType, ffi->dwFileSubtype, ffi->dwFileDateMS, ffi->dwFileDateLS);
#endif
		
	process_set_field_int (filever, "filemajorpart",
			       HIWORD (ffi->dwFileVersionMS));
	process_set_field_int (filever, "fileminorpart",
			       LOWORD (ffi->dwFileVersionMS));
	process_set_field_int (filever, "filebuildpart",
			       HIWORD (ffi->dwFileVersionLS));
	process_set_field_int (filever, "fileprivatepart",
			       LOWORD (ffi->dwFileVersionLS));
		
	process_set_field_int (filever, "productmajorpart",
			       HIWORD (ffi->dwProductVersionMS));
	process_set_field_int (filever, "productminorpart",
			       LOWORD (ffi->dwProductVersionMS));
	process_set_field_int (filever, "productbuildpart",
			       HIWORD (ffi->dwProductVersionLS));
	process_set_field_int (filever, "productprivatepart",
			       LOWORD (ffi->dwProductVersionLS));
	
	process_set_field_bool (filever, "isdebug",
				ffi->dwFileFlags&VS_FF_DEBUG);
	process_set_field_bool (filever, "isprerelease",
				ffi->dwFileFlags&VS_FF_PRERELEASE);
	process_set_field_bool (filever, "ispatched",
				ffi->dwFileFlags&VS_FF_PATCHED);
	process_set_field_bool (filever, "isprivatebuild",
				ffi->dwFileFlags&VS_FF_PRIVATEBUILD);
	process_set_field_bool (filever, "isspecialbuild",
				ffi->dwFileFlags&VS_FF_SPECIALBUILD);
}

static void process_get_fileversion (MonoObject *filever, MonoImage *image)
{
	MonoPEResourceDataEntry *version_info;
	gpointer data;
	VS_FIXEDFILEINFO *ffi;
	gpointer data_ptr;
	version_data block;
	gint32 data_len; /* signed to guard against underflow */
	guchar vs_key[]= {'V', '\0', 'S', '\0', '_', '\0', 'V', '\0',
			  'E', '\0', 'R', '\0', 'S', '\0', 'I', '\0',
			  'O', '\0', 'N', '\0', '_', '\0', 'I', '\0',
			  'N', '\0', 'F', '\0', 'O', '\0', '\0', '\0'
	};
	guchar var_key[]= {'V', '\0', 'a', '\0', 'r', '\0', 'F', '\0',
			   'i', '\0', 'l', '\0', 'e', '\0', 'I', '\0',
			   'n', '\0', 'f', '\0', 'o', '\0', '\0', '\0', 
	};
	guchar str_key[]= {'S', '\0', 't', '\0', 'r', '\0', 'i', '\0',
			   'n', '\0', 'g', '\0', 'F', '\0', 'i', '\0',
			   'l', '\0', 'e', '\0', 'I', '\0', 'n', '\0',
			   'f', '\0', 'o', '\0', '\0', '\0', 
	};
	
	version_info=mono_image_lookup_resource (image,
						 MONO_PE_RESOURCE_ID_VERSION,
						 0, NULL);
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": image_lookup returned %p",
		   version_info);
#endif

	if(version_info==NULL) {
		return;
	}
	
	data=mono_cli_rva_map (image->image_info,
			       version_info->rde_data_offset);
	if(data==NULL) {
		return;
	}

	/* See io-layer/versioninfo.h for the gory details on how this
	 * data is laid out. (data should be pointing to
	 * VS_VERSIONINFO data).
	 */

	data_ptr=process_get_versioninfo_block (data, &block);
		
	data_len=block.data_len;
		
	if(block.value_len!=sizeof(VS_FIXEDFILEINFO)) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": FIXEDFILEINFO size mismatch");
#endif
		return;
	}

	if(memcmp (block.key, &vs_key, unicode_bytes (block.key))) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": VS_VERSION_INFO mismatch");
#endif
		return;
	}

	ffi=(((VS_FIXEDFILEINFO *)data_ptr)++);
	if((ffi->dwSignature!=VS_FFI_SIGNATURE) ||
	   (ffi->dwStrucVersion!=VS_FFI_STRUCVERSION)) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": FIXEDFILEINFO bad signature");
#endif
		return;
	}
	process_read_fixedfileinfo_block (filever, ffi);
	
	/* Subtract the 92 bytes we've already seen */
	data_len -= 92;
	
	/* There now follow zero or one StringFileInfo blocks and zero
	 * or one VarFileInfo blocks
	 */
	while(data_len > 0) {
		/* align on a 32-bit boundary */
		data_ptr=(gpointer)(((unsigned)data_ptr+3) & (~3));

		data_ptr=process_get_versioninfo_block (data_ptr, &block);
		data_len=data_len-block.data_len;

		if(!memcmp (block.key, &var_key, unicode_bytes (block.key))) {
			data_ptr=process_read_var_block (filever, data_ptr,
							 block.data_len);
		} else if (!memcmp (block.key, &str_key,
				    unicode_bytes (block.key))) {
			data_ptr=process_read_stringtable_block (filever, data_ptr, block.data_len);
		} else {
			/* Bogus data */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Not a valid VERSIONINFO child block");
			return;
#endif
		}
	}
}

static void process_add_module (GPtrArray *modules, MonoAssembly *ass)
{
	MonoClass *proc_class, *filever_class;
	MonoObject *item, *filever;
	MonoDomain *domain=mono_domain_get ();
	gchar *modulename;
	
	/* Build a System.Diagnostics.ProcessModule with the data.
	 * Leave BaseAddress and EntryPointAddress set to NULL,
	 * FileName is ass->image->name, FileVersionInfo is an object
	 * constructed from the PE image data referenced by
	 * ass->image, ModuleMemorySize set to 0, ModuleName the last
	 * component of FileName.
	 */
	proc_class=mono_class_from_name (system_assembly, "System.Diagnostics",
					 "ProcessModule");
	item=mono_object_new (domain, proc_class);

	filever_class=mono_class_from_name (system_assembly,
					    "System.Diagnostics",
					    "FileVersionInfo");
	filever=mono_object_new (domain, filever_class);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": recording assembly: FileName [%s] FileVersionInfo [%d.%d.%d.%d], ModuleName [%s]", ass->image->name, ass->aname.major, ass->aname.minor, ass->aname.build, ass->aname.revision, ass->image->name);
#endif

	process_get_fileversion (filever, ass->image);

	process_set_field_string_utf8 (filever, "filename", ass->image->name);
	process_set_field_string_utf8 (item, "filename", ass->image->name);
	process_set_field_object (item, "version_info", filever);

	modulename=g_path_get_basename (ass->image->name);
	process_set_field_string_utf8 (item, "modulename", modulename);
	g_free (modulename);

	g_ptr_array_add (modules, item);
}

static void process_scan_modules (gpointer data, gpointer user_data)
{
	MonoAssembly *ass=data;
	GPtrArray *modules=user_data;

	/* The main assembly is already in the list */
	if(mono_assembly_get_main () != ass) {
		process_add_module (modules, ass);
	}
}


/* Returns an array of System.Diagnostics.ProcessModule */
MonoArray *ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this)
{
	/* I was going to use toolhelp for this, but then realised I
	 * was being an idiot :)
	 *
	 * (Toolhelp would give shared libraries open by the runtime,
	 * as well as open assemblies.  On windows my tests didnt find
	 * the assemblies loaded by mono either.)
	 */
	GPtrArray *modules_list=g_ptr_array_new ();
	MonoArray *arr;
	guint32 i;
	
	STASH_SYS_ASS (this);
	
	/* Make sure the first entry is the main module */
	process_add_module (modules_list, mono_assembly_get_main ());
	
	mono_assembly_foreach (process_scan_modules, modules_list);

	/* Build a MonoArray out of modules_list */
	arr=mono_array_new (mono_domain_get (), mono_defaults.object_class,
			    modules_list->len);
	
	for(i=0; i<modules_list->len; i++) {
		mono_array_set (arr, MonoObject *, i,
				g_ptr_array_index (modules_list, i));
	}
	
	g_ptr_array_free (modules_list, FALSE);
	
	return(arr);
}

void ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this, MonoString *filename)
{
	MonoImage *image;
	guchar *filename_utf8;
	
	STASH_SYS_ASS (this);
	
	filename_utf8=mono_string_to_utf8 (filename);
	image=mono_image_open (filename_utf8, NULL);
	g_free (filename_utf8);
	
	if(image==NULL) {
		/* FIXME: an exception might be appropriate here */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Failed to load image");
#endif

		return;
	}
	
	process_get_fileversion (this, image);
	process_set_field_string_utf8 (this, "filename", image->name);
	
	mono_image_close (image);
}

MonoBoolean ves_icall_System_Diagnostics_Process_Start_internal (MonoString *filename, MonoString *args, MonoProcInfo *process_info)
{
	gboolean ret;
	gunichar2 *utf16_filename;
	gunichar2 *utf16_args;
	STARTUPINFO startinfo;
	PROCESS_INFORMATION procinfo;
	
	utf16_filename=mono_string_to_utf16 (filename);
	utf16_args=mono_string_to_utf16 (args);
	
	ret=CreateProcess (utf16_filename, utf16_args, NULL, NULL, TRUE, CREATE_UNICODE_ENVIRONMENT, NULL, NULL, &startinfo, &procinfo);

	g_free (utf16_filename);
	g_free (utf16_args);

	if(ret==TRUE) {
		process_info->process_handle=procinfo.hProcess;
		process_info->thread_handle=procinfo.hThread;
		process_info->pid=procinfo.dwProcessId;
		process_info->tid=procinfo.dwThreadId;
	}
	
	return(ret);
}

MonoBoolean ves_icall_System_Diagnostics_Process_WaitForExit_internal (MonoObject *this, HANDLE process, gint32 ms)
{
	guint32 ret;
	
	if(ms<0) {
		/* Wait forever */
		ret=WaitForSingleObject (process, INFINITE);
	} else {
		ret=WaitForSingleObject (process, ms);
	}
	
	if(ret==WAIT_OBJECT_0) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}

gint64 ves_icall_System_Diagnostics_Process_ExitTime_internal (HANDLE process)
{
	gboolean ret;
	gint64 ticks;
	FILETIME create_time, exit_time, kernel_time, user_time;
	
	ret=GetProcessTimes (process, &create_time, &exit_time, &kernel_time,
			     &user_time);
	if(ret==TRUE) {
		ticks=((guint64)exit_time.dwHighDateTime << 32) +
			exit_time.dwLowDateTime;
		
		return(ticks);
	} else {
		return(0);
	}
}

gint64 ves_icall_System_Diagnostics_Process_StartTime_internal (HANDLE process)
{
	gboolean ret;
	gint64 ticks;
	FILETIME create_time, exit_time, kernel_time, user_time;
	
	ret=GetProcessTimes (process, &create_time, &exit_time, &kernel_time,
			     &user_time);
	if(ret==TRUE) {
		ticks=((guint64)create_time.dwHighDateTime << 32) +
			create_time.dwLowDateTime;
		
		return(ticks);
	} else {
		return(0);
	}
}

gint32 ves_icall_System_Diagnostics_Process_ExitCode_internal (HANDLE process)
{
	guint32 code;
	
	GetExitCodeProcess (process, &code);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": process exit code is %d", code);
#endif
	
	return(code);
}
