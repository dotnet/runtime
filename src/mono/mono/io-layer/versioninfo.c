/*
 * versioninfo.c:  Version information support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2007 Novell, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/versioninfo.h>
#include <mono/io-layer/io-portability.h>
#include <mono/io-layer/error.h>
#include <mono/utils/strenc.h>

#undef DEBUG

static WapiImageSectionHeader *get_enclosing_section_header (guint32 rva, WapiImageNTHeaders *nt_headers)
{
	WapiImageSectionHeader *section = IMAGE_FIRST_SECTION (nt_headers);
	guint32 i;
	
	for (i = 0; i < nt_headers->FileHeader.NumberOfSections; i++, section++) {
		guint32 size = section->Misc.VirtualSize;
		if (size == 0) {
			size = section->SizeOfRawData;
		}
		
		if ((rva >= section->VirtualAddress) &&
		    (rva < (section->VirtualAddress + size))) {
			return(section);
		}
	}
	
	return(NULL);
}

static gpointer get_ptr_from_rva (guint32 rva, WapiImageNTHeaders *ntheaders,
				  gpointer file_map)
{
	WapiImageSectionHeader *section_header;
	guint32 delta;
	
	section_header = get_enclosing_section_header (rva, ntheaders);
	if (section_header == NULL) {
		return(NULL);
	}
	
	delta = (guint32)(section_header->VirtualAddress -
			  section_header->PointerToRawData);
	
	return((guint8 *)file_map + rva - delta);
}

static gpointer scan_resource_dir (WapiImageResourceDirectory *root,
				   WapiImageNTHeaders *nt_headers,
				   gpointer file_map,
				   WapiImageResourceDirectoryEntry *entry,
				   int level, guint32 res_id, guint32 lang_id,
				   guint32 *size)
{
	gboolean is_string = entry->NameIsString;
	gboolean is_dir = entry->DataIsDirectory;
	guint32 name_offset = GUINT32_FROM_LE (entry->NameOffset);
	guint32 dir_offset = GUINT32_FROM_LE (entry->OffsetToDirectory);
	guint32 data_offset = GUINT32_FROM_LE (entry->OffsetToData);
	
	if (level == 0) {
		/* Normally holds a directory entry for each type of
		 * resource
		 */
		if ((is_string == FALSE &&
		     name_offset != res_id) ||
		    (is_string == TRUE)) {
			return(NULL);
		}
	} else if (level == 1) {
		/* Normally holds a directory entry for each resource
		 * item
		 */
	} else if (level == 2) {
		/* Normally holds a directory entry for each language
		 */
		if ((is_string == FALSE &&
		     name_offset != lang_id &&
		     lang_id != 0) ||
		    (is_string == TRUE)) {
			return(NULL);
		}
	} else {
		g_assert_not_reached ();
	}
	
	if (is_dir == TRUE) {
		WapiImageResourceDirectory *res_dir = (WapiImageResourceDirectory *)((guint8 *)root + dir_offset);
		WapiImageResourceDirectoryEntry *sub_entries = (WapiImageResourceDirectoryEntry *)(res_dir + 1);
		guint32 entries, i;
		
		entries = GUINT16_FROM_LE (res_dir->NumberOfNamedEntries) + GUINT16_FROM_LE (res_dir->NumberOfIdEntries);
		
		for (i = 0; i < entries; i++) {
			WapiImageResourceDirectoryEntry *sub_entry = &sub_entries[i];
			gpointer ret;
			
			ret = scan_resource_dir (root, nt_headers, file_map,
						 sub_entry, level + 1, res_id,
						 lang_id, size);
			if (ret != NULL) {
				return(ret);
			}
		}
		
		return(NULL);
	} else {
		WapiImageResourceDataEntry *data_entry = (WapiImageResourceDataEntry *)((guint8 *)root + data_offset);
		*size = GUINT32_FROM_LE (data_entry->Size);
		
		return(get_ptr_from_rva (data_entry->OffsetToData, nt_headers, file_map));
	}
}

static gpointer find_pe_file_resources (gpointer file_map, guint32 map_size,
					guint32 res_id, guint32 lang_id,
					guint32 *size)
{
	WapiImageDosHeader *dos_header;
	WapiImageNTHeaders *nt_headers;
	WapiImageResourceDirectory *resource_dir;
	WapiImageResourceDirectoryEntry *resource_dir_entry;
	guint32 resource_rva, entries, i;
	gpointer ret = NULL;

	dos_header = (WapiImageDosHeader *)file_map;
	if (dos_header->e_magic != IMAGE_DOS_SIGNATURE) {
#ifdef DEBUG
		g_message ("%s: Bad dos signature 0x%x", __func__,
			   dos_header->e_magic);
#endif

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	if (map_size < sizeof(WapiImageNTHeaders) + GUINT32_FROM_LE (dos_header->e_lfanew)) {
#ifdef DEBUG
		g_message ("%s: File is too small: %d", __func__, map_size);
#endif

		SetLastError (ERROR_BAD_LENGTH);
		return(NULL);
	}
	
	nt_headers = (WapiImageNTHeaders *)((guint8 *)file_map + GUINT32_FROM_LE (dos_header->e_lfanew));
	if (nt_headers->Signature != IMAGE_NT_SIGNATURE) {
#ifdef DEBUG
		g_message ("%s: Bad NT signature 0x%x", __func__,
			   nt_headers->Signature);
#endif

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	if (nt_headers->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC) {
		/* Do 64-bit stuff */
		resource_rva = GUINT32_FROM_LE (((WapiImageNTHeaders64 *)nt_headers)->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
	} else {
		resource_rva = GUINT32_FROM_LE (nt_headers->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
	}

	if (resource_rva == 0) {
#ifdef DEBUG
		g_message ("%s: No resources in file!", __func__);
#endif
		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	resource_dir = (WapiImageResourceDirectory *)get_ptr_from_rva (resource_rva, nt_headers, file_map);
	if (resource_dir == NULL) {
#ifdef DEBUG
		g_message ("%s: Can't find resource directory", __func__);
#endif
		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	entries = GUINT16_FROM_LE (resource_dir->NumberOfNamedEntries) + GUINT16_FROM_LE (resource_dir->NumberOfIdEntries);
	resource_dir_entry = (WapiImageResourceDirectoryEntry *)(resource_dir + 1);
	
	for (i = 0; i < entries; i++) {
		WapiImageResourceDirectoryEntry *direntry = &resource_dir_entry[i];
		ret = scan_resource_dir (resource_dir, nt_headers, file_map,
					 direntry, 0, res_id, lang_id, size);
		if (ret != NULL) {
			return(ret);
		}
	}
	
	return(NULL);
}

static gpointer map_pe_file (gunichar2 *filename, guint32 *map_size)
{
	gchar *filename_ext;
	int fd;
	struct stat statbuf;
	gpointer file_map;
	
	/* According to the MSDN docs, a search path is applied to
	 * filename.  FIXME: implement this, for now just pass it
	 * straight to fopen
	 */

	filename_ext = mono_unicode_to_external (filename);
	if (filename_ext == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(NULL);
	}
	
	fd = _wapi_open (filename_ext, O_RDONLY, 0);
	if (fd == -1) {
#ifdef DEBUG
		g_message ("%s: Error opening file %s: %s", __func__,
			   filename_ext, strerror (errno));
#endif

		SetLastError (_wapi_get_win32_file_error (errno));
		g_free (filename_ext);
		
		return(NULL);
	}

	if (fstat (fd, &statbuf) == -1) {
#ifdef DEBUG
		g_message ("%s: Error stat()ing file %s: %s", __func__,
			   filename_ext, strerror (errno));
#endif

		SetLastError (_wapi_get_win32_file_error (errno));
		g_free (filename_ext);
		close (fd);
		return(NULL);
	}
	*map_size = statbuf.st_size;

	/* Check basic file size */
	if (statbuf.st_size < sizeof(WapiImageDosHeader)) {
#ifdef DEBUG
		g_message ("%s: File %s is too small: %ld", __func__,
			   filename_ext, statbuf.st_size);
#endif

		SetLastError (ERROR_BAD_LENGTH);
		g_free (filename_ext);
		close (fd);
		return(NULL);
	}
	
	file_map = mmap (NULL, statbuf.st_size, PROT_READ, MAP_PRIVATE, fd, 0);
	if (file_map == MAP_FAILED) {
#ifdef DEBUG
		g_message ("%s: Error mmap()int file %s: %s", __func__,
			   filename_ext, strerror (errno));
#endif

		SetLastError (_wapi_get_win32_file_error (errno));
		g_free (filename_ext);
		close (fd);
		return(NULL);
	}

	/* Don't need the fd any more */
	close (fd);

	return(file_map);
}

static void unmap_pe_file (gpointer file_map, guint32 map_size)
{
	munmap (file_map, map_size);
}

guint32 GetFileVersionInfoSize (gunichar2 *filename, guint32 *handle)
{
	gpointer file_map;
	gpointer versioninfo;
	guint32 map_size;
	guint32 size;
	
	/* This value is unused, but set to zero */
	*handle = 0;
	
	file_map = map_pe_file (filename, &map_size);
	if (file_map == NULL) {
		return(0);
	}
	
	versioninfo = find_pe_file_resources (file_map, map_size, RT_VERSION,
					      0, &size);
	if (versioninfo == NULL) {
		/* Didn't find the resource, so set the return value
		 * to 0
		 */
		size = 0;
	}

	unmap_pe_file (file_map, map_size);

	return(size);
}

gboolean GetFileVersionInfo (gunichar2 *filename, guint32 handle G_GNUC_UNUSED,
			     guint32 len, gpointer data)
{
	gpointer file_map;
	gpointer versioninfo;
	guint32 map_size;
	guint32 size;
	gboolean ret = FALSE;
	
	file_map = map_pe_file (filename, &map_size);
	if (file_map == NULL) {
		return(FALSE);
	}
	
	versioninfo = find_pe_file_resources (file_map, map_size, RT_VERSION,
					      0, &size);
	if (versioninfo != NULL) {
		/* This could probably process the data so that
		 * VerQueryValue() doesn't have to follow the data
		 * blocks every time.  But hey, these functions aren't
		 * likely to appear in many profiles.
		 */
		memcpy (data, versioninfo, len < size?len:size);
		ret = TRUE;
	}

	unmap_pe_file (file_map, map_size);
	
	return(ret);
}

static guint32 unicode_chars (const gunichar2 *str)
{
	guint32 len = 0;
	
	do {
		if (str[len] == '\0') {
			return(len);
		}
		len++;
	} while(1);
}

static gboolean unicode_compare (const gunichar2 *str1, const gunichar2 *str2)
{
	while (*str1 && *str2) {
		if (GUINT16_TO_LE (*str1) != GUINT16_TO_LE (*str2)) {
			return(FALSE);
		}
		++str1;
		++str2;
	}
	
	return(*str1 == *str2);
}

/* compare a little-endian null-terminated utf16 string and a normal string.
 * Can be used only for ascii or latin1 chars.
 */
static gboolean unicode_string_equals (const gunichar2 *str1, const gchar *str2)
{
	while (*str1 && *str2) {
		if (GUINT16_TO_LE (*str1) != *str2) {
			return(FALSE);
		}
		++str1;
		++str2;
	}
	
	return(*str1 == *str2);
}

typedef struct 
{
	guint16 data_len;
	guint16 value_len;
	guint16 type;
	gunichar2 *key;
} version_data;

/* Returns a pointer to the value data, because there's no way to know
 * how big that data is (value_len is set to zero for most blocks :-( )
 */
static gconstpointer get_versioninfo_block (gconstpointer data,
					    version_data *block)
{
	block->data_len = GUINT16_FROM_LE (*((guint16 *)data));
	data = (char *)data + sizeof(guint16);
	block->value_len = GUINT16_FROM_LE (*((guint16 *)data));
	data = (char *)data + sizeof(guint16);
	
	/* No idea what the type is supposed to indicate */
	block->type = GUINT16_FROM_LE (*((guint16 *)data));
	data = (char *)data + sizeof(guint16);
	block->key = ((gunichar2 *)data);
	
	/* Skip over the key (including the terminator) */
	data = ((gunichar2 *)data) + (unicode_chars (block->key) + 1);
	
	/* align on a 32-bit boundary */
	data = (gpointer)((char *)data + 3);
	data = (gpointer)((char *)data - (GPOINTER_TO_INT (data) & 3));
	
	return(data);
}

static gconstpointer get_fixedfileinfo_block (gconstpointer data,
					      version_data *block)
{
	gconstpointer data_ptr;
	gint32 data_len; /* signed to guard against underflow */
	WapiFixedFileInfo *ffi;

	data_ptr = get_versioninfo_block (data, block);
	data_len = block->data_len;
		
	if (block->value_len != sizeof(WapiFixedFileInfo)) {
#ifdef DEBUG
		g_message ("%s: FIXEDFILEINFO size mismatch", __func__);
#endif
		return(NULL);
	}

	if (!unicode_string_equals (block->key, "VS_VERSION_INFO")) {
#ifdef DEBUG
		g_message ("%s: VS_VERSION_INFO mismatch", __func__);
#endif
		return(NULL);
	}

	ffi = ((WapiFixedFileInfo *)data_ptr);
	if ((ffi->dwSignature != VS_FFI_SIGNATURE) ||
	    (ffi->dwStrucVersion != VS_FFI_STRUCVERSION)) {
#ifdef DEBUG
		g_message ("%s: FIXEDFILEINFO bad signature", __func__);
#endif
		return(NULL);
	}

	return(data_ptr);
}

static gconstpointer get_varfileinfo_block (gconstpointer data_ptr,
					    version_data *block)
{
	/* data is pointing at a Var block
	 */
	data_ptr = get_versioninfo_block (data_ptr, block);

	return(data_ptr);
}

static gconstpointer get_string_block (gconstpointer data_ptr,
				       const gunichar2 *string_key,
				       gpointer *string_value,
				       guint32 *string_value_len,
				       version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 28; /* Length of the StringTable block */
 	
	/* data_ptr is pointing at an array of one or more String blocks
	 * with total length (not including alignment padding) of
	 * data_len
	 */
	while (string_len < data_len) {
		gunichar2 *value;
		
		/* align on a 32-bit boundary */
		data_ptr = (gpointer)((char *)data_ptr + 3);
		data_ptr = (gpointer)((char *)data_ptr - (GPOINTER_TO_INT (data_ptr) & 3));
		
		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
#ifdef DEBUG
			g_message ("%s: Hit 0-length block, giving up",
				   __func__);
#endif
			return(NULL);
		}
		
		string_len = string_len + block->data_len;
		value = (gunichar2 *)data_ptr;
		
		if (string_key != NULL &&
		    string_value != NULL &&
		    string_value_len != NULL &&
		    unicode_compare (string_key, block->key) == TRUE) {
			*string_value = (gpointer)data_ptr;
			*string_value_len = block->value_len;
		}
		
		/* Skip over the value */
		data_ptr = ((gunichar2 *)data_ptr) + block->value_len;
	}
	
	return(data_ptr);
}

/* Returns a pointer to the byte following the Stringtable block, or
 * NULL if the data read hits padding.  We can't recover from this
 * because the data length does not include padding bytes, so it's not
 * possible to just return the start position + length
 */
static gconstpointer get_stringtable_block (gconstpointer data_ptr,
					    gchar *lang,
					    const gunichar2 *string_key,
					    gpointer *string_value,
					    guint32 *string_value_len,
					    version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 36; /* length of the StringFileInfo block */
	gchar *found_lang;
	
	/* data_ptr is pointing at an array of StringTable blocks,
	 * with total length (not including alignment padding) of
	 * data_len
	 */

	while(string_len < data_len) {
		/* align on a 32-bit boundary */
		data_ptr = (gpointer)((char *)data_ptr + 3);
		data_ptr = (gpointer)((char *)data_ptr - (GPOINTER_TO_INT (data_ptr) & 3));
		
		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
#ifdef DEBUG
			g_message ("%s: Hit 0-length block, giving up",
				   __func__);
#endif
			return(NULL);
		}
		
		string_len = string_len + block->data_len;

		found_lang = g_utf16_to_utf8 (block->key, 8, NULL, NULL, NULL);
		if (found_lang == NULL) {
#ifdef DEBUG
			g_message ("%s: Didn't find a valid language key, giving up", __func__);
#endif
			return(NULL);
		}
		
		g_strdown (found_lang);
		
		if (!strcmp (found_lang, lang)) {
			/* Got the one we're interested in */
			data_ptr = get_string_block (data_ptr, string_key,
						     string_value,
						     string_value_len, block);
		} else {
			data_ptr = get_string_block (data_ptr, NULL, NULL,
						     NULL, block);
		}

		g_free (found_lang);
		
		if (data_ptr == NULL) {
			/* Child block hit padding */
#ifdef DEBUG
			g_message ("%s: Child block hit 0-length block, giving up", __func__);
#endif
			return(NULL);
		}
	}
	
	return(data_ptr);
}

gboolean VerQueryValue (gconstpointer datablock, const gunichar2 *subblock,
			gpointer *buffer, guint32 *len)
{
	gchar *subblock_utf8, *lang_utf8 = NULL;
	gboolean ret = FALSE;
	version_data block;
	gconstpointer data_ptr;
	gint32 data_len; /* signed to guard against underflow */
	gboolean want_var = FALSE;
	gboolean want_string = FALSE;
	gunichar2 lang[8];
	const gunichar2 *string_key = NULL;
	gpointer string_value = NULL;
	guint32 string_value_len = 0;
	
	subblock_utf8 = g_utf16_to_utf8 (subblock, -1, NULL, NULL, NULL);
	if (subblock_utf8 == NULL) {
		return(FALSE);
	}

	if (!strcmp (subblock_utf8, "\\VarFileInfo\\Translation")) {
		want_var = TRUE;
	} else if (!strncmp (subblock_utf8, "\\StringFileInfo\\", 16)) {
		want_string = TRUE;
		memcpy (lang, subblock + 16, 8 * sizeof(gunichar2));
		lang_utf8 = g_utf16_to_utf8 (lang, 8, NULL, NULL, NULL);
		g_strdown (lang_utf8);
		string_key = subblock + 25;
	}
	
	if (!strcmp (subblock_utf8, "\\")) {
		data_ptr = get_fixedfileinfo_block (datablock, &block);
		if (data_ptr != NULL) {
			*buffer = (gpointer)data_ptr;
			*len = block.value_len;
		
			ret = TRUE;
		}
	} else if (want_var || want_string) {
		data_ptr = get_fixedfileinfo_block (datablock, &block);
		if (data_ptr != NULL) {
			/* The FFI and header occupies the first 92
			 * bytes
			 */
			data_ptr = (char *)data_ptr + sizeof(WapiFixedFileInfo);
			data_len = block.data_len - 92;
			
			/* There now follow zero or one StringFileInfo
			 * blocks and zero or one VarFileInfo blocks
			 */
			while (data_len > 0) {
				/* align on a 32-bit boundary */
				data_ptr = (gpointer)((char *)data_ptr + 3);
				data_ptr = (gpointer)((char *)data_ptr - (GPOINTER_TO_INT (data_ptr) & 3));
				
				data_ptr = get_versioninfo_block (data_ptr,
								  &block);
				if (block.data_len == 0) {
					/* We must have hit padding,
					 * so give up processing now
					 */
#ifdef DEBUG
					g_message ("%s: Hit 0-length block, giving up", __func__);
#endif
					goto done;
				}
				
				data_len = data_len - block.data_len;
				
				if (unicode_string_equals (block.key, "VarFileInfo")) {
					data_ptr = get_varfileinfo_block (data_ptr, &block);
					if (want_var) {
						*buffer = (gpointer)data_ptr;
						*len = block.value_len;
						ret = TRUE;
						goto done;
					} else {
						/* Skip over the Var block */
						data_ptr = ((guchar *)data_ptr) + block.value_len;
					}
				} else if (unicode_string_equals (block.key, "StringFileInfo")) {
					data_ptr = get_stringtable_block (data_ptr, lang_utf8, string_key, &string_value, &string_value_len, &block);
					if (want_string &&
					    string_value != NULL &&
					    string_value_len != 0) {
						*buffer = string_value;
						*len = string_value_len;
						ret = TRUE;
						goto done;
					}
				} else {
					/* Bogus data */
#ifdef DEBUG
					g_message ("%s: Not a valid VERSIONINFO child block", __func__);
#endif
					goto done;
				}
				
				if (data_ptr == NULL) {
					/* Child block hit padding */
#ifdef DEBUG
					g_message ("%s: Child block hit 0-length block, giving up", __func__);
#endif
					goto done;
				}
			}
		}
	}

  done:
	if (lang_utf8) {
		g_free (lang_utf8);
	}
	
	g_free (subblock_utf8);
	return(ret);
}

guint32 VerLanguageName (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	return(0);
}
