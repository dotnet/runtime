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
#include <string.h>

#include <mono/metadata/object.h>
#include <mono/metadata/process.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/exception.h>
#include <mono/utils/strenc.h>
#include <mono/io-layer/io-layer.h>
/* FIXME: fix this code to not depend so much on the inetrnals */
#include <mono/metadata/class-internals.h>

#undef DEBUG

HANDLE ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid)
{
	HANDLE handle;
	
	MONO_ARCH_SAVE_REGS;

	/* GetCurrentProcess returns a pseudo-handle, so use
	 * OpenProcess instead
	 */
	handle=OpenProcess (PROCESS_ALL_ACCESS, TRUE, pid);
	
	if(handle==NULL) {
		/* FIXME: Throw an exception */
		return(NULL);
	}
	
	return(handle);
}

guint32 ves_icall_System_Diagnostics_Process_GetPid_internal (void)
{
	MONO_ARCH_SAVE_REGS;

	return(GetCurrentProcessId ());
}

void ves_icall_System_Diagnostics_Process_Process_free_internal (MonoObject *this,
								 HANDLE process)
{
	MONO_ARCH_SAVE_REGS;

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
	block->data_len=*((guint16 *)data);
	data = (char *)data + sizeof(guint16);
	block->value_len=*((guint16 *)data);
	data = (char *)data + sizeof(guint16);

	/* No idea what the type is supposed to indicate */
	block->type=*((guint16 *)data);
	data = (char *)data + sizeof(guint16);
	block->key=((gunichar2 *)data);

	/* skip over the key (including the terminator) */
	data=((gunichar2 *)data)+(unicode_chars (block->key)+1);

	/* align on a 32-bit boundary */
	data=(gpointer)((char *)data + 3);
	data=(gpointer)((char *)data - (GPOINTER_TO_INT(data) & 3));
	
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

/* Returns a pointer to the byte following the String block, or NULL
 * if the data read hits padding.  We can't recover from this because
 * the data length does not include padding bytes, so it's not
 * possible to just return the start position + length.
 */
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
		data_ptr=(gpointer)((char *)data_ptr + 3);
		data_ptr=(gpointer)((char *)data_ptr -
		    (GPOINTER_TO_INT(data_ptr) & 3));

		data_ptr=process_get_versioninfo_block (data_ptr, &block);
		if(block.data_len==0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Hit 0-length block, giving up");
#endif
			return(NULL);
		}
		
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

/* returns a pointer to the byte following the Stringtable block, or
 * NULL if the data read hits padding.  We can't recover from this
 * because the data length does not include padding bytes, so it's not
 * possible to just return the start position + length
 */
static gpointer process_read_stringtable_block (MonoObject *filever,
						gpointer data_ptr,
						guint16 data_len)
{
	version_data block;
	gchar *language;
	guint16 string_len=36;	/* length of the StringFileInfo block */

	/* data_ptr is pointing at an array of StringTable blocks,
	 * with total length (not including alignment padding) of
	 * data_len.
	 */

	while(string_len<data_len) {
		/* align on a 32-bit boundary */
		data_ptr=(gpointer)((char *)data_ptr + 3);
		data_ptr=(gpointer)((char *)data_ptr -
		    (GPOINTER_TO_INT(data_ptr) & 3));

		data_ptr=process_get_versioninfo_block (data_ptr, &block);
		if(block.data_len==0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Hit 0-length block, giving up");
#endif
			return(NULL);
		}
		string_len=string_len+block.data_len;

		language = g_utf16_to_utf8 (block.key, unicode_bytes (block.key), NULL, NULL, NULL);
		g_strdown (language);
		if (!strcmp (language, "007f04b0") || !strcmp (language, "000004b0")) {
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
		g_free (language);

		if(data_ptr==NULL) {
			/* Child block hit padding */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": Child block hit 0-length block, giving up");
#endif
			return(NULL);
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
	
	data=mono_image_rva_map (image,
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

	ffi=((VS_FIXEDFILEINFO *)data_ptr);
	data_ptr = (char *)data_ptr + sizeof(VS_FIXEDFILEINFO);
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
		data_ptr=(gpointer)((char *)data_ptr + 3);
		data_ptr=(gpointer)((char *)data_ptr -
		    (GPOINTER_TO_INT(data_ptr) & 3));

		data_ptr=process_get_versioninfo_block (data_ptr, &block);
		if(block.data_len==0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Hit 0-length block, giving up");
#endif
			return;
		}
		
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

		if(data_ptr==NULL) {
			/* Child block hit padding */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": Child block hit 0-length block, giving up");
#endif
			return;
		}
	}
}

static void process_add_module (GPtrArray *modules, MonoAssembly *ass)
{
	MonoClass *proc_class, *filever_class;
	MonoObject *item, *filever;
	MonoDomain *domain=mono_domain_get ();
	gchar *modulename;
	const char* filename;
	
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

	process_get_fileversion (filever, mono_assembly_get_image (ass));

	filename = mono_image_get_filename (mono_assembly_get_image (ass));
	process_set_field_string_utf8 (filever, "filename", filename);
	process_set_field_string_utf8 (item, "filename", filename);
	process_set_field_object (item, "version_info", filever);

	modulename=g_path_get_basename (filename);
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
	
	MONO_ARCH_SAVE_REGS;

	STASH_SYS_ASS (this);
	
	/* Make sure the first entry is the main module */
	process_add_module (modules_list, mono_assembly_get_main ());
	
	mono_assembly_foreach (process_scan_modules, modules_list);

	/* Build a MonoArray out of modules_list */
	arr=mono_array_new (mono_domain_get (), mono_get_object_class (),
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
	
	MONO_ARCH_SAVE_REGS;

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
	process_set_field_string_utf8 (this, "filename", mono_image_get_filename (image));
	
	mono_image_close (image);
}

static gchar *
quote_path (const gchar *path)
{
	gchar *res = g_shell_quote (path);
#ifdef PLATFORM_WIN32
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

static gboolean
complete_path (const gunichar2 *appname, gunichar2 **completed)
{
	gchar *utf8app;
	gchar *found;
	gchar *quoted8;

	utf8app = g_utf16_to_utf8 (appname, -1, NULL, NULL, NULL);
	if (g_path_is_absolute (utf8app)) {
		quoted8 = quote_path (utf8app);
		*completed = g_utf8_to_utf16 (quoted8, -1, NULL, NULL, NULL);
		g_free (quoted8);
		g_free (utf8app);
		return TRUE;
	}

	if (g_file_test (utf8app, G_FILE_TEST_IS_EXECUTABLE) && !g_file_test (utf8app, G_FILE_TEST_IS_DIR)) {
		quoted8 = quote_path (utf8app);
		*completed = g_utf8_to_utf16 (quoted8, -1, NULL, NULL, NULL);
		g_free (quoted8);
		g_free (utf8app);
		return TRUE;
	}
	
	found = g_find_program_in_path (utf8app);
	if (found == NULL) {
		*completed = NULL;
		g_free (utf8app);
		return FALSE;
	}

	quoted8 = quote_path (found);
	*completed = g_utf8_to_utf16 (quoted8, -1, NULL, NULL, NULL);
	g_free (quoted8);
	g_free (found);
	g_free (utf8app);
	return TRUE;
}

MonoBoolean ves_icall_System_Diagnostics_Process_Start_internal (MonoString *appname, MonoString *cmd, MonoString *dirname, HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoProcInfo *process_info)
{
	gboolean ret;
	gunichar2 *dir;
	STARTUPINFO startinfo={0};
	PROCESS_INFORMATION procinfo;
	gunichar2 *shell_path = NULL;
	gchar *env_vars = NULL;
	gboolean free_shell_path = TRUE;
	
	MONO_ARCH_SAVE_REGS;

	startinfo.cb=sizeof(STARTUPINFO);
	startinfo.dwFlags=STARTF_USESTDHANDLES;
	startinfo.hStdInput=stdin_handle;
	startinfo.hStdOutput=stdout_handle;
	startinfo.hStdError=stderr_handle;
	
	if (process_info->use_shell) {
		const gchar *spath;
		const gchar *shell_args;
#ifdef PLATFORM_WIN32
		spath = g_getenv ("COMSPEC");
		shell_args = "/c %s";
#else
		spath = g_getenv ("SHELL");
		shell_args = "-c %s";
#endif
		if (spath != NULL) {
			gint dummy;
			gchar *newcmd, *tmp;
			gchar *quoted;

			shell_path = mono_unicode_from_external (spath, &dummy);
			tmp = mono_string_to_utf8 (cmd);
			quoted = g_shell_quote (tmp);
#ifdef PLATFORM_WIN32
			{
				gchar *q = quoted;
				while (*q) {
					if (*q == '\'')
						*q = '\"';
					q++;
				}
			}
#endif
			newcmd = g_strdup_printf (shell_args, quoted);
			g_free (quoted);
			g_free (tmp);
			cmd = mono_string_new (mono_domain_get (), newcmd);
			g_free (newcmd);
		}
	} else {
		shell_path = mono_string_chars (appname);
		free_shell_path = complete_path (shell_path, &shell_path);
		if (shell_path == NULL) {
			process_info->pid = -ERROR_FILE_NOT_FOUND;
			return FALSE;
		}
	}

	if (process_info->env_keys != NULL) {
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
	if(mono_string_length (dirname)==0) {
		dir=NULL;
	} else {
		dir=mono_string_chars (dirname);
	}
	
	ret=CreateProcess (shell_path, mono_string_chars (cmd), NULL, NULL, TRUE, CREATE_UNICODE_ENVIRONMENT, env_vars, dir, &startinfo, &procinfo);

	g_free (env_vars);
	if (free_shell_path)
		g_free (shell_path);

	if(ret) {
		process_info->process_handle=procinfo.hProcess;
		/*process_info->thread_handle=procinfo.hThread;*/
		process_info->thread_handle=NULL;
		CloseHandle(procinfo.hThread);
		process_info->pid=procinfo.dwProcessId;
		process_info->tid=procinfo.dwThreadId;
	} else {
		process_info->pid = -GetLastError ();
	}
	
	return(ret);
}

MonoBoolean ves_icall_System_Diagnostics_Process_WaitForExit_internal (MonoObject *this, HANDLE process, gint32 ms)
{
	guint32 ret;
	
	MONO_ARCH_SAVE_REGS;

	if(ms<0) {
		/* Wait forever */
		ret=WaitForSingleObjectEx (process, INFINITE, TRUE);
	} else {
		ret=WaitForSingleObjectEx (process, ms, TRUE);
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
	
	MONO_ARCH_SAVE_REGS;

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
	
	MONO_ARCH_SAVE_REGS;

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
	
	MONO_ARCH_SAVE_REGS;

	GetExitCodeProcess (process, &code);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": process exit code is %d", code);
#endif
	
	return(code);
}

MonoString *ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process)
{
	MonoString *string;
	gboolean ok;
	HMODULE mod;
	gunichar2 name[MAX_PATH];
	guint32 needed;
	guint32 len;
	
	MONO_ARCH_SAVE_REGS;

	ok=EnumProcessModules (process, &mod, sizeof(mod), &needed);
	if(ok==FALSE) {
		return(NULL);
	}
	
	len=GetModuleBaseName (process, mod, name, sizeof(name));
	if(len==0) {
		return(NULL);
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": process name is [%s]",
		   g_utf16_to_utf8 (name, -1, NULL, NULL, NULL));
#endif
	
	string=mono_string_new_utf16 (mono_domain_get (), name, len);
	
	return(string);
}

/* Returns an array of pids */
MonoArray *ves_icall_System_Diagnostics_Process_GetProcesses_internal (void)
{
	MonoArray *procs;
	gboolean ret;
	guint32 needed, count, i;
	guint32 pids[1024];

	MONO_ARCH_SAVE_REGS;

	ret=EnumProcesses (pids, sizeof(pids), &needed);
	if(ret==FALSE) {
		/* FIXME: throw an exception */
		return(NULL);
	}
	
	count=needed/sizeof(guint32);
	procs=mono_array_new (mono_domain_get (), mono_get_int32_class (),
			      count);
	for(i=0; i<count; i++) {
		mono_array_set (procs, guint32, i, pids[i]);
	}
	
	return(procs);
}

MonoBoolean ves_icall_System_Diagnostics_Process_GetWorkingSet_internal (HANDLE process, guint32 *min, guint32 *max)
{
	gboolean ret;
	size_t ws_min, ws_max;
	
	MONO_ARCH_SAVE_REGS;

	ret=GetProcessWorkingSetSize (process, &ws_min, &ws_max);
	*min=(guint32)ws_min;
	*max=(guint32)ws_max;
	
	return(ret);
}

MonoBoolean ves_icall_System_Diagnostics_Process_SetWorkingSet_internal (HANDLE process, guint32 min, guint32 max, MonoBoolean use_min)
{
	gboolean ret;
	size_t ws_min;
	size_t ws_max;
	
	MONO_ARCH_SAVE_REGS;

	ret=GetProcessWorkingSetSize (process, &ws_min, &ws_max);
	if(ret==FALSE) {
		return(FALSE);
	}
	
	if(use_min==TRUE) {
		ws_min=min;
	} else {
		ws_max=max;
	}
	
	ret=SetProcessWorkingSetSize (process, ws_min, ws_max);

	return(ret);
}

MonoBoolean
ves_icall_System_Diagnostics_Process_Kill_internal (HANDLE process, gint32 sig)
{
	MONO_ARCH_SAVE_REGS;

	/* sig == 1 -> Kill, sig == 2 -> CloseMainWindow */

	return TerminateProcess (process, -sig);
}

