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
#include <mono/utils/mono-mmap.h>

#define LOGDEBUG(...)  
//define LOGDEBUG(...) g_message(__VA_ARGS__)

#define ALIGN32(ptr) ptr = (gpointer)((char *)ptr + 3); ptr = (gpointer)((char *)ptr - ((gsize)ptr & 3));

static WapiImageSectionHeader *
get_enclosing_section_header (guint32 rva, WapiImageNTHeaders32 *nt_headers)
{
	WapiImageSectionHeader *section = _WAPI_IMAGE_FIRST_SECTION32 (nt_headers);
	guint32 i;
	
	for (i = 0; i < GUINT16_FROM_LE (nt_headers->FileHeader.NumberOfSections); i++, section++) {
		guint32 size = GUINT32_FROM_LE (section->Misc.VirtualSize);
		if (size == 0) {
			size = GUINT32_FROM_LE (section->SizeOfRawData);
		}
		
		if ((rva >= GUINT32_FROM_LE (section->VirtualAddress)) &&
		    (rva < (GUINT32_FROM_LE (section->VirtualAddress) + size))) {
			return(section);
		}
	}
	
	return(NULL);
}

/* This works for both 32bit and 64bit files, as the differences are
 * all after the section header block
 */
static gpointer
get_ptr_from_rva (guint32 rva, WapiImageNTHeaders32 *ntheaders, gpointer file_map)
{
	WapiImageSectionHeader *section_header;
	guint32 delta;
	
	section_header = get_enclosing_section_header (rva, ntheaders);
	if (section_header == NULL) {
		return(NULL);
	}
	
	delta = (guint32)(GUINT32_FROM_LE (section_header->VirtualAddress) -
			  GUINT32_FROM_LE (section_header->PointerToRawData));
	
	return((guint8 *)file_map + rva - delta);
}

static gpointer
scan_resource_dir (WapiImageResourceDirectory *root,
		   WapiImageNTHeaders32 *nt_headers,
		   gpointer file_map,
		   WapiImageResourceDirectoryEntry *entry,
		   int level, guint32 res_id, guint32 lang_id,
		   guint32 *size)
{
	WapiImageResourceDirectoryEntry swapped_entry;
	gboolean is_string, is_dir;
	guint32 name_offset, dir_offset, data_offset;
	
	swapped_entry.Name = GUINT32_FROM_LE (entry->Name);
	swapped_entry.OffsetToData = GUINT32_FROM_LE (entry->OffsetToData);
	
	is_string = swapped_entry.NameIsString;
	is_dir = swapped_entry.DataIsDirectory;
	name_offset = swapped_entry.NameOffset;
	dir_offset = swapped_entry.OffsetToDirectory;
	data_offset = swapped_entry.OffsetToData;
	
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
		
		return(get_ptr_from_rva (GUINT32_FROM_LE (data_entry->OffsetToData), nt_headers, file_map));
	}
}

static gpointer
find_pe_file_resources32 (gpointer file_map, guint32 map_size,
			  guint32 res_id, guint32 lang_id,
			  guint32 *size)
{
	WapiImageDosHeader *dos_header;
	WapiImageNTHeaders32 *nt_headers;
	WapiImageResourceDirectory *resource_dir;
	WapiImageResourceDirectoryEntry *resource_dir_entry;
	guint32 resource_rva, entries, i;
	gpointer ret = NULL;

	dos_header = (WapiImageDosHeader *)file_map;
	if (dos_header->e_magic != IMAGE_DOS_SIGNATURE) {
		LOGDEBUG ("%s: Bad dos signature 0x%x", __func__, dos_header->e_magic);

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	if (map_size < sizeof(WapiImageNTHeaders32) + GUINT32_FROM_LE (dos_header->e_lfanew)) {
		LOGDEBUG ("%s: File is too small: %d", __func__, map_size);

		SetLastError (ERROR_BAD_LENGTH);
		return(NULL);
	}
	
	nt_headers = (WapiImageNTHeaders32 *)((guint8 *)file_map + GUINT32_FROM_LE (dos_header->e_lfanew));
	if (nt_headers->Signature != IMAGE_NT_SIGNATURE) {
		LOGDEBUG ("%s: Bad NT signature 0x%x", __func__, nt_headers->Signature);

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
		LOGDEBUG ("%s: No resources in file!", __func__);

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	resource_dir = (WapiImageResourceDirectory *)get_ptr_from_rva (resource_rva, (WapiImageNTHeaders32 *)nt_headers, file_map);
	if (resource_dir == NULL) {
		LOGDEBUG ("%s: Can't find resource directory", __func__);

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	entries = GUINT16_FROM_LE (resource_dir->NumberOfNamedEntries) + GUINT16_FROM_LE (resource_dir->NumberOfIdEntries);
	resource_dir_entry = (WapiImageResourceDirectoryEntry *)(resource_dir + 1);
	
	for (i = 0; i < entries; i++) {
		WapiImageResourceDirectoryEntry *direntry = &resource_dir_entry[i];
		ret = scan_resource_dir (resource_dir,
					 (WapiImageNTHeaders32 *)nt_headers,
					 file_map, direntry, 0, res_id,
					 lang_id, size);
		if (ret != NULL) {
			return(ret);
		}
	}

	return(NULL);
}

static gpointer
find_pe_file_resources64 (gpointer file_map, guint32 map_size,
			  guint32 res_id, guint32 lang_id,
			  guint32 *size)
{
	WapiImageDosHeader *dos_header;
	WapiImageNTHeaders64 *nt_headers;
	WapiImageResourceDirectory *resource_dir;
	WapiImageResourceDirectoryEntry *resource_dir_entry;
	guint32 resource_rva, entries, i;
	gpointer ret = NULL;

	dos_header = (WapiImageDosHeader *)file_map;
	if (dos_header->e_magic != IMAGE_DOS_SIGNATURE) {
		LOGDEBUG ("%s: Bad dos signature 0x%x", __func__, dos_header->e_magic);

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	if (map_size < sizeof(WapiImageNTHeaders64) + GUINT32_FROM_LE (dos_header->e_lfanew)) {
		LOGDEBUG ("%s: File is too small: %d", __func__, map_size);

		SetLastError (ERROR_BAD_LENGTH);
		return(NULL);
	}
	
	nt_headers = (WapiImageNTHeaders64 *)((guint8 *)file_map + GUINT32_FROM_LE (dos_header->e_lfanew));
	if (nt_headers->Signature != IMAGE_NT_SIGNATURE) {
		LOGDEBUG ("%s: Bad NT signature 0x%x", __func__,
			   nt_headers->Signature);

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
		LOGDEBUG ("%s: No resources in file!", __func__);

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}
	
	resource_dir = (WapiImageResourceDirectory *)get_ptr_from_rva (resource_rva, (WapiImageNTHeaders32 *)nt_headers, file_map);
	if (resource_dir == NULL) {
		LOGDEBUG ("%s: Can't find resource directory", __func__);

		SetLastError (ERROR_INVALID_DATA);
		return(NULL);
	}

	entries = GUINT16_FROM_LE (resource_dir->NumberOfNamedEntries) + GUINT16_FROM_LE (resource_dir->NumberOfIdEntries);
	resource_dir_entry = (WapiImageResourceDirectoryEntry *)(resource_dir + 1);
	
	for (i = 0; i < entries; i++) {
		WapiImageResourceDirectoryEntry *direntry = &resource_dir_entry[i];
		ret = scan_resource_dir (resource_dir,
					 (WapiImageNTHeaders32 *)nt_headers,
					 file_map, direntry, 0, res_id,
					 lang_id, size);
		if (ret != NULL) {
			return(ret);
		}
	}

	return(NULL);
}

static gpointer
find_pe_file_resources (gpointer file_map, guint32 map_size,
			guint32 res_id, guint32 lang_id,
			guint32 *size)
{
	/* Figure this out when we support 64bit PE files */
	if (1) {
		return find_pe_file_resources32 (file_map, map_size, res_id,
						 lang_id, size);
	} else {
		return find_pe_file_resources64 (file_map, map_size, res_id,
						 lang_id, size);
	}
}

static gpointer
map_pe_file (gunichar2 *filename, gint32 *map_size, void **handle)
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
		LOGDEBUG ("%s: unicode conversion returned NULL", __func__);

		SetLastError (ERROR_INVALID_NAME);
		return(NULL);
	}
	
	fd = _wapi_open (filename_ext, O_RDONLY, 0);
	if (fd == -1) {
		LOGDEBUG ("%s: Error opening file %s: %s", __func__, filename_ext, strerror (errno));

		SetLastError (_wapi_get_win32_file_error (errno));
		g_free (filename_ext);
		
		return(NULL);
	}

	if (fstat (fd, &statbuf) == -1) {
		LOGDEBUG ("%s: Error stat()ing file %s: %s", __func__, filename_ext, strerror (errno));

		SetLastError (_wapi_get_win32_file_error (errno));
		g_free (filename_ext);
		close (fd);
		return(NULL);
	}
	*map_size = statbuf.st_size;
	
	/* Check basic file size */
	if (statbuf.st_size < sizeof(WapiImageDosHeader)) {
		LOGDEBUG ("%s: File %s is too small: %lld", __func__, filename_ext, statbuf.st_size);

		SetLastError (ERROR_BAD_LENGTH);
		g_free (filename_ext);
		close (fd);
		return(NULL);
	}
	
	file_map = mono_file_map (statbuf.st_size, MONO_MMAP_READ | MONO_MMAP_PRIVATE, fd, 0, handle);
	if (file_map == NULL) {
		LOGDEBUG ("%s: Error mmap()int file %s: %s", __func__, filename_ext, strerror (errno));

		SetLastError (_wapi_get_win32_file_error (errno));
		g_free (filename_ext);
		close (fd);
		return(NULL);
	}

	/* Don't need the fd any more */
	close (fd);
	g_free (filename_ext);

	return(file_map);
}

static void
unmap_pe_file (gpointer file_map, void *handle)
{
	mono_file_unmap (file_map, handle);
}

static guint32
unicode_chars (const gunichar2 *str)
{
	guint32 len = 0;
	
	do {
		if (str[len] == '\0') {
			return(len);
		}
		len++;
	} while(1);
}

static gboolean
unicode_compare (const gunichar2 *str1, const gunichar2 *str2)
{
	while (*str1 && *str2) {
		if (*str1 != *str2) {
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
static gboolean
unicode_string_equals (const gunichar2 *str1, const gchar *str2)
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
static gconstpointer
get_versioninfo_block (gconstpointer data, version_data *block)
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
	ALIGN32 (data);
	
	return(data);
}

static gconstpointer
get_fixedfileinfo_block (gconstpointer data, version_data *block)
{
	gconstpointer data_ptr;
	gint32 data_len; /* signed to guard against underflow */
	WapiFixedFileInfo *ffi;

	data_ptr = get_versioninfo_block (data, block);
	data_len = block->data_len;
		
	if (block->value_len != sizeof(WapiFixedFileInfo)) {
		LOGDEBUG ("%s: FIXEDFILEINFO size mismatch", __func__);
		return(NULL);
	}

	if (!unicode_string_equals (block->key, "VS_VERSION_INFO")) {
		LOGDEBUG ("%s: VS_VERSION_INFO mismatch", __func__);

		return(NULL);
	}

	ffi = ((WapiFixedFileInfo *)data_ptr);
	if ((ffi->dwSignature != VS_FFI_SIGNATURE) ||
	    (ffi->dwStrucVersion != VS_FFI_STRUCVERSION)) {
		LOGDEBUG ("%s: FIXEDFILEINFO bad signature", __func__);

		return(NULL);
	}

	return(data_ptr);
}

static gconstpointer
get_varfileinfo_block (gconstpointer data_ptr, version_data *block)
{
	/* data is pointing at a Var block
	 */
	data_ptr = get_versioninfo_block (data_ptr, block);

	return(data_ptr);
}

static gconstpointer
get_string_block (gconstpointer data_ptr,
		  const gunichar2 *string_key,
		  gpointer *string_value,
		  guint32 *string_value_len,
		  version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 28; /* Length of the StringTable block */
 	char *orig_data_ptr = (char *)data_ptr - 28;

	/* data_ptr is pointing at an array of one or more String blocks
	 * with total length (not including alignment padding) of
	 * data_len
	 */
	while (((char *)data_ptr - (char *)orig_data_ptr) < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);
		
		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			LOGDEBUG ("%s: Hit 0-length block, giving up", __func__);

			return(NULL);
		}
		
		string_len = string_len + block->data_len;
		
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
 *
 * If lang == NULL it means we're just stepping through this block
 */
static gconstpointer
get_stringtable_block (gconstpointer data_ptr,
		       gchar *lang,
		       const gunichar2 *string_key,
		       gpointer *string_value,
		       guint32 *string_value_len,
		       version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 36; /* length of the StringFileInfo block */
	gchar *found_lang;
	gchar *lowercase_lang;
	
	/* data_ptr is pointing at an array of StringTable blocks,
	 * with total length (not including alignment padding) of
	 * data_len
	 */

	while(string_len < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);
		
		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			LOGDEBUG ("%s: Hit 0-length block, giving up", __func__);
			return(NULL);
		}
		
		string_len = string_len + block->data_len;

		found_lang = g_utf16_to_utf8 (block->key, 8, NULL, NULL, NULL);
		if (found_lang == NULL) {
			LOGDEBUG ("%s: Didn't find a valid language key, giving up", __func__);
			return(NULL);
		}
		
		lowercase_lang = g_utf8_strdown (found_lang, -1);
		g_free (found_lang);
		found_lang = lowercase_lang;
		lowercase_lang = NULL;
		
		if (lang != NULL && !strcmp (found_lang, lang)) {
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
			LOGDEBUG ("%s: Child block hit 0-length block, giving up", __func__);
			return(NULL);
		}
	}
	
	return(data_ptr);
}

#if G_BYTE_ORDER == G_BIG_ENDIAN
static gconstpointer
big_up_string_block (gconstpointer data_ptr, version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 28; /* Length of the StringTable block */
	gchar *big_value;
	char *orig_data_ptr = (char *)data_ptr - 28;
	
	/* data_ptr is pointing at an array of one or more String
	 * blocks with total length (not including alignment padding)
	 * of data_len
	 */
	while (((char *)data_ptr - (char *)orig_data_ptr) < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);
		
		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			LOGDEBUG ("%s: Hit 0-length block, giving up", __func__);
			return(NULL);
		}
		
		string_len = string_len + block->data_len;
		
		big_value = g_convert ((gchar *)block->key,
				       unicode_chars (block->key) * 2,
				       "UTF-16BE", "UTF-16LE", NULL, NULL,
				       NULL);
		if (big_value == NULL) {
			LOGDEBUG ("%s: Didn't find a valid string, giving up", __func__);
			return(NULL);
		}
		
		/* The swapped string should be exactly the same
		 * length as the original little-endian one, but only
		 * copy the number of original chars just to be on the
		 * safe side
		 */
		memcpy (block->key, big_value, unicode_chars (block->key) * 2);
		g_free (big_value);

		big_value = g_convert ((gchar *)data_ptr,
				       unicode_chars (data_ptr) * 2,
				       "UTF-16BE", "UTF-16LE", NULL, NULL,
				       NULL);
		if (big_value == NULL) {
			LOGDEBUG ("%s: Didn't find a valid data string, giving up", __func__);
			return(NULL);
		}
		memcpy ((gpointer)data_ptr, big_value,
			unicode_chars (data_ptr) * 2);
		g_free (big_value);

		data_ptr = ((gunichar2 *)data_ptr) + block->value_len;
	}
	
	return(data_ptr);
}

/* Returns a pointer to the byte following the Stringtable block, or
 * NULL if the data read hits padding.  We can't recover from this
 * because the data length does not include padding bytes, so it's not
 * possible to just return the start position + length
 */
static gconstpointer
big_up_stringtable_block (gconstpointer data_ptr, version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 36; /* length of the StringFileInfo block */
	gchar *big_value;
	
	/* data_ptr is pointing at an array of StringTable blocks,
	 * with total length (not including alignment padding) of
	 * data_len
	 */

	while(string_len < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);

		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			LOGDEBUG ("%s: Hit 0-length block, giving up", __func__);
			return(NULL);
		}
		
		string_len = string_len + block->data_len;

		big_value = g_convert ((gchar *)block->key, 16, "UTF-16BE",
				       "UTF-16LE", NULL, NULL, NULL);
		if (big_value == NULL) {
			LOGDEBUG ("%s: Didn't find a valid string, giving up", __func__);
			return(NULL);
		}
		
		memcpy (block->key, big_value, 16);
		g_free (big_value);
		
		data_ptr = big_up_string_block (data_ptr, block);
		
		if (data_ptr == NULL) {
			/* Child block hit padding */
			LOGDEBUG ("%s: Child block hit 0-length block, giving up", __func__);
			return(NULL);
		}
	}
	
	return(data_ptr);
}

/* Follows the data structures and turns all UTF-16 strings from the
 * LE found in the resource section into UTF-16BE
 */
static void
big_up (gconstpointer datablock, guint32 size)
{
	gconstpointer data_ptr;
	gint32 data_len; /* signed to guard against underflow */
	version_data block;
	
	data_ptr = get_fixedfileinfo_block (datablock, &block);
	if (data_ptr != NULL) {
		WapiFixedFileInfo *ffi = (WapiFixedFileInfo *)data_ptr;
		
		/* Byteswap all the fields */
		ffi->dwFileVersionMS = GUINT32_SWAP_LE_BE (ffi->dwFileVersionMS);
		ffi->dwFileVersionLS = GUINT32_SWAP_LE_BE (ffi->dwFileVersionLS);
		ffi->dwProductVersionMS = GUINT32_SWAP_LE_BE (ffi->dwProductVersionMS);
		ffi->dwProductVersionLS = GUINT32_SWAP_LE_BE (ffi->dwProductVersionLS);
		ffi->dwFileFlagsMask = GUINT32_SWAP_LE_BE (ffi->dwFileFlagsMask);
		ffi->dwFileFlags = GUINT32_SWAP_LE_BE (ffi->dwFileFlags);
		ffi->dwFileOS = GUINT32_SWAP_LE_BE (ffi->dwFileOS);
		ffi->dwFileType = GUINT32_SWAP_LE_BE (ffi->dwFileType);
		ffi->dwFileSubtype = GUINT32_SWAP_LE_BE (ffi->dwFileSubtype);
		ffi->dwFileDateMS = GUINT32_SWAP_LE_BE (ffi->dwFileDateMS);
		ffi->dwFileDateLS = GUINT32_SWAP_LE_BE (ffi->dwFileDateLS);

		/* The FFI and header occupies the first 92 bytes
		 */
		data_ptr = (char *)data_ptr + sizeof(WapiFixedFileInfo);
		data_len = block.data_len - 92;
		
		/* There now follow zero or one StringFileInfo blocks
		 * and zero or one VarFileInfo blocks
		 */
		while (data_len > 0) {
			/* align on a 32-bit boundary */
			ALIGN32 (data_ptr);
			
			data_ptr = get_versioninfo_block (data_ptr, &block);
			if (block.data_len == 0) {
				/* We must have hit padding, so give
				 * up processing now
				 */
				LOGDEBUG ("%s: Hit 0-length block, giving up", __func__);
				return;
			}
			
			data_len = data_len - block.data_len;
			
			if (unicode_string_equals (block.key, "VarFileInfo")) {
				data_ptr = get_varfileinfo_block (data_ptr,
								  &block);
				data_ptr = ((guchar *)data_ptr) + block.value_len;
			} else if (unicode_string_equals (block.key,
							  "StringFileInfo")) {
				data_ptr = big_up_stringtable_block (data_ptr,
								     &block);
			} else {
				/* Bogus data */
				LOGDEBUG ("%s: Not a valid VERSIONINFO child block", __func__);
				return;
			}
			
			if (data_ptr == NULL) {
				/* Child block hit padding */
				LOGDEBUG ("%s: Child block hit 0-length block, giving up", __func__);
				return;
			}
		}
	}
}
#endif

gboolean
VerQueryValue (gconstpointer datablock, const gunichar2 *subblock, gpointer *buffer, guint32 *len)
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
	gchar *lowercase_lang;
	
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
		lowercase_lang = g_utf8_strdown (lang_utf8, -1);
		g_free (lang_utf8);
		lang_utf8 = lowercase_lang;
		lowercase_lang = NULL;
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
				ALIGN32 (data_ptr);
				
				data_ptr = get_versioninfo_block (data_ptr,
								  &block);
				if (block.data_len == 0) {
					/* We must have hit padding,
					 * so give up processing now
					 */
					LOGDEBUG ("%s: Hit 0-length block, giving up", __func__);
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
						*len = unicode_chars (string_value) + 1; /* Include trailing null */
						ret = TRUE;
						goto done;
					}
				} else {
					/* Bogus data */
					LOGDEBUG ("%s: Not a valid VERSIONINFO child block", __func__);
					goto done;
				}
				
				if (data_ptr == NULL) {
					/* Child block hit padding */
					LOGDEBUG ("%s: Child block hit 0-length block, giving up", __func__);
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

guint32
GetFileVersionInfoSize (gunichar2 *filename, guint32 *handle)
{
	gpointer file_map;
	gpointer versioninfo;
	void *map_handle;
	gint32 map_size;
	guint32 size;

	/* This value is unused, but set to zero */
	*handle = 0;
	
	file_map = map_pe_file (filename, &map_size, &map_handle);
	if (file_map == NULL) {
		return(0);
	}
	
	versioninfo = find_pe_file_resources (file_map, map_size, RT_VERSION, 0, &size);
	if (versioninfo == NULL) {
		/* Didn't find the resource, so set the return value
		 * to 0
		 */
		size = 0;
	}

	unmap_pe_file (file_map, map_handle);

	return(size);
}

gboolean
GetFileVersionInfo (gunichar2 *filename, guint32 handle G_GNUC_UNUSED, guint32 len, gpointer data)
{
	gpointer file_map;
	gpointer versioninfo;
	void *map_handle;
	gint32 map_size;
	guint32 size;
	gboolean ret = FALSE;
	
	file_map = map_pe_file (filename, &map_size, &map_handle);
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

#if G_BYTE_ORDER == G_BIG_ENDIAN
		big_up (data, size);
#endif
	}

	unmap_pe_file (file_map, map_handle);
	
	return(ret);
}

static guint32
copy_lang (gunichar2 *lang_out, guint32 lang_len, const gchar *text)
{
	gunichar2 *unitext;
	int chars = strlen (text);
	int ret;
	
	unitext = g_utf8_to_utf16 (text, -1, NULL, NULL, NULL);
	g_assert (unitext != NULL);
	
	if (chars < (lang_len - 1)) {
		memcpy (lang_out, (gpointer)unitext, chars * 2);
		lang_out[chars] = '\0';
		ret = chars;
	} else {
		memcpy (lang_out, (gpointer)unitext, (lang_len - 1) * 2);
		lang_out[lang_len] = '\0';
		ret = lang_len;
	}
	
	g_free (unitext);

	return(ret);
}

guint32
VerLanguageName (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	int primary, secondary;
	const char *name = NULL;

	primary = lang & 0x3FF;
	secondary = (lang >> 10) & 0x3F;

	switch(primary) {
	case 0x00:
		switch(secondary) {
		case 0x01:
			name = "Process Default Language";
			break;
		}
		break;
	case 0x01:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Arabic (Saudi Arabia)";
			break;
		case 0x02:
			name = "Arabic (Iraq)";
			break;
		case 0x03:
			name = "Arabic (Egypt)";
			break;
		case 0x04:
			name = "Arabic (Libya)";
			break;
		case 0x05:
			name = "Arabic (Algeria)";
			break;
		case 0x06:
			name = "Arabic (Morocco)";
			break;
		case 0x07:
			name = "Arabic (Tunisia)";
			break;
		case 0x08:
			name = "Arabic (Oman)";
			break;
		case 0x09:
			name = "Arabic (Yemen)";
			break;
		case 0x0a:
			name = "Arabic (Syria)";
			break;
		case 0x0b:
			name = "Arabic (Jordan)";
			break;
		case 0x0c:
			name = "Arabic (Lebanon)";
			break;
		case 0x0d:
			name = "Arabic (Kuwait)";
			break;
		case 0x0e:
			name = "Arabic (U.A.E.)";
			break;
		case 0x0f:
			name = "Arabic (Bahrain)";
			break;
		case 0x10:
			name = "Arabic (Qatar)";
			break;
		}
		break;
	case 0x02:
		switch(secondary) {
		case 0x00:
			name = "Bulgarian (Bulgaria)";
			break;
		case 0x01:
			name = "Bulgarian";
			break;
		}
		break;
	case 0x03:
		switch(secondary) {
		case 0x00:
			name = "Catalan (Spain)";
			break;
		case 0x01:
			name = "Catalan";
			break;
		}
		break;
	case 0x04:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Chinese (Taiwan)";
			break;
		case 0x02:
			name = "Chinese (PRC)";
			break;
		case 0x03:
			name = "Chinese (Hong Kong S.A.R.)";
			break;
		case 0x04:
			name = "Chinese (Singapore)";
			break;
		case 0x05:
			name = "Chinese (Macau S.A.R.)";
			break;
		}
		break;
	case 0x05:
		switch(secondary) {
		case 0x00:
			name = "Czech (Czech Republic)";
			break;
		case 0x01:
			name = "Czech";
			break;
		}
		break;
	case 0x06:
		switch(secondary) {
		case 0x00:
			name = "Danish (Denmark)";
			break;
		case 0x01:
			name = "Danish";
			break;
		}
		break;
	case 0x07:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "German (Germany)";
			break;
		case 0x02:
			name = "German (Switzerland)";
			break;
		case 0x03:
			name = "German (Austria)";
			break;
		case 0x04:
			name = "German (Luxembourg)";
			break;
		case 0x05:
			name = "German (Liechtenstein)";
			break;
		}
		break;
	case 0x08:
		switch(secondary) {
		case 0x00:
			name = "Greek (Greece)";
			break;
		case 0x01:
			name = "Greek";
			break;
		}
		break;
	case 0x09:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "English (United States)";
			break;
		case 0x02:
			name = "English (United Kingdom)";
			break;
		case 0x03:
			name = "English (Australia)";
			break;
		case 0x04:
			name = "English (Canada)";
			break;
		case 0x05:
			name = "English (New Zealand)";
			break;
		case 0x06:
			name = "English (Ireland)";
			break;
		case 0x07:
			name = "English (South Africa)";
			break;
		case 0x08:
			name = "English (Jamaica)";
			break;
		case 0x09:
			name = "English (Caribbean)";
			break;
		case 0x0a:
			name = "English (Belize)";
			break;
		case 0x0b:
			name = "English (Trinidad and Tobago)";
			break;
		case 0x0c:
			name = "English (Zimbabwe)";
			break;
		case 0x0d:
			name = "English (Philippines)";
			break;
		case 0x10:
			name = "English (India)";
			break;
		case 0x11:
			name = "English (Malaysia)";
			break;
		case 0x12:
			name = "English (Singapore)";
			break;
		}
		break;
	case 0x0a:
		switch(secondary) {
		case 0x00:
			name = "Spanish (Spain)";
			break;
		case 0x01:
			name = "Spanish (Traditional Sort)";
			break;
		case 0x02:
			name = "Spanish (Mexico)";
			break;
		case 0x03:
			name = "Spanish (International Sort)";
			break;
		case 0x04:
			name = "Spanish (Guatemala)";
			break;
		case 0x05:
			name = "Spanish (Costa Rica)";
			break;
		case 0x06:
			name = "Spanish (Panama)";
			break;
		case 0x07:
			name = "Spanish (Dominican Republic)";
			break;
		case 0x08:
			name = "Spanish (Venezuela)";
			break;
		case 0x09:
			name = "Spanish (Colombia)";
			break;
		case 0x0a:
			name = "Spanish (Peru)";
			break;
		case 0x0b:
			name = "Spanish (Argentina)";
			break;
		case 0x0c:
			name = "Spanish (Ecuador)";
			break;
		case 0x0d:
			name = "Spanish (Chile)";
			break;
		case 0x0e:
			name = "Spanish (Uruguay)";
			break;
		case 0x0f:
			name = "Spanish (Paraguay)";
			break;
		case 0x10:
			name = "Spanish (Bolivia)";
			break;
		case 0x11:
			name = "Spanish (El Salvador)";
			break;
		case 0x12:
			name = "Spanish (Honduras)";
			break;
		case 0x13:
			name = "Spanish (Nicaragua)";
			break;
		case 0x14:
			name = "Spanish (Puerto Rico)";
			break;
		case 0x15:
			name = "Spanish (United States)";
			break;
		}
		break;
	case 0x0b:
		switch(secondary) {
		case 0x00:
			name = "Finnish (Finland)";
			break;
		case 0x01:
			name = "Finnish";
			break;
		}
		break;
	case 0x0c:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "French (France)";
			break;
		case 0x02:
			name = "French (Belgium)";
			break;
		case 0x03:
			name = "French (Canada)";
			break;
		case 0x04:
			name = "French (Switzerland)";
			break;
		case 0x05:
			name = "French (Luxembourg)";
			break;
		case 0x06:
			name = "French (Monaco)";
			break;
		}
		break;
	case 0x0d:
		switch(secondary) {
		case 0x00:
			name = "Hebrew (Israel)";
			break;
		case 0x01:
			name = "Hebrew";
			break;
		}
		break;
	case 0x0e:
		switch(secondary) {
		case 0x00:
			name = "Hungarian (Hungary)";
			break;
		case 0x01:
			name = "Hungarian";
			break;
		}
		break;
	case 0x0f:
		switch(secondary) {
		case 0x00:
			name = "Icelandic (Iceland)";
			break;
		case 0x01:
			name = "Icelandic";
			break;
		}
		break;
	case 0x10:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Italian (Italy)";
			break;
		case 0x02:
			name = "Italian (Switzerland)";
			break;
		}
		break;
	case 0x11:
		switch(secondary) {
		case 0x00:
			name = "Japanese (Japan)";
			break;
		case 0x01:
			name = "Japanese";
			break;
		}
		break;
	case 0x12:
		switch(secondary) {
		case 0x00:
			name = "Korean (Korea)";
			break;
		case 0x01:
			name = "Korean";
			break;
		}
		break;
	case 0x13:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Dutch (Netherlands)";
			break;
		case 0x02:
			name = "Dutch (Belgium)";
			break;
		}
		break;
	case 0x14:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Norwegian (Bokmal)";
			break;
		case 0x02:
			name = "Norwegian (Nynorsk)";
			break;
		}
		break;
	case 0x15:
		switch(secondary) {
		case 0x00:
			name = "Polish (Poland)";
			break;
		case 0x01:
			name = "Polish";
			break;
		}
		break;
	case 0x16:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Portuguese (Brazil)";
			break;
		case 0x02:
			name = "Portuguese (Portugal)";
			break;
		}
		break;
	case 0x17:
		switch(secondary) {
		case 0x01:
			name = "Romansh (Switzerland)";
			break;
		}
		break;
	case 0x18:
		switch(secondary) {
		case 0x00:
			name = "Romanian (Romania)";
			break;
		case 0x01:
			name = "Romanian";
			break;
		}
		break;
	case 0x19:
		switch(secondary) {
		case 0x00:
			name = "Russian (Russia)";
			break;
		case 0x01:
			name = "Russian";
			break;
		}
		break;
	case 0x1a:
		switch(secondary) {
		case 0x00:
			name = "Croatian (Croatia)";
			break;
		case 0x01:
			name = "Croatian";
			break;
		case 0x02:
			name = "Serbian (Latin)";
			break;
		case 0x03:
			name = "Serbian (Cyrillic)";
			break;
		case 0x04:
			name = "Croatian (Bosnia and Herzegovina)";
			break;
		case 0x05:
			name = "Bosnian (Latin, Bosnia and Herzegovina)";
			break;
		case 0x06:
			name = "Serbian (Latin, Bosnia and Herzegovina)";
			break;
		case 0x07:
			name = "Serbian (Cyrillic, Bosnia and Herzegovina)";
			break;
		case 0x08:
			name = "Bosnian (Cyrillic, Bosnia and Herzegovina)";
			break;
		}
		break;
	case 0x1b:
		switch(secondary) {
		case 0x00:
			name = "Slovak (Slovakia)";
			break;
		case 0x01:
			name = "Slovak";
			break;
		}
		break;
	case 0x1c:
		switch(secondary) {
		case 0x00:
			name = "Albanian (Albania)";
			break;
		case 0x01:
			name = "Albanian";
			break;
		}
		break;
	case 0x1d:
		switch(secondary) {
		case 0x00:
			name = "Swedish (Sweden)";
			break;
		case 0x01:
			name = "Swedish";
			break;
		case 0x02:
			name = "Swedish (Finland)";
			break;
		}
		break;
	case 0x1e:
		switch(secondary) {
		case 0x00:
			name = "Thai (Thailand)";
			break;
		case 0x01:
			name = "Thai";
			break;
		}
		break;
	case 0x1f:
		switch(secondary) {
		case 0x00:
			name = "Turkish (Turkey)";
			break;
		case 0x01:
			name = "Turkish";
			break;
		}
		break;
	case 0x20:
		switch(secondary) {
		case 0x00:
			name = "Urdu (Islamic Republic of Pakistan)";
			break;
		case 0x01:
			name = "Urdu";
			break;
		}
		break;
	case 0x21:
		switch(secondary) {
		case 0x00:
			name = "Indonesian (Indonesia)";
			break;
		case 0x01:
			name = "Indonesian";
			break;
		}
		break;
	case 0x22:
		switch(secondary) {
		case 0x00:
			name = "Ukrainian (Ukraine)";
			break;
		case 0x01:
			name = "Ukrainian";
			break;
		}
		break;
	case 0x23:
		switch(secondary) {
		case 0x00:
			name = "Belarusian (Belarus)";
			break;
		case 0x01:
			name = "Belarusian";
			break;
		}
		break;
	case 0x24:
		switch(secondary) {
		case 0x00:
			name = "Slovenian (Slovenia)";
			break;
		case 0x01:
			name = "Slovenian";
			break;
		}
		break;
	case 0x25:
		switch(secondary) {
		case 0x00:
			name = "Estonian (Estonia)";
			break;
		case 0x01:
			name = "Estonian";
			break;
		}
		break;
	case 0x26:
		switch(secondary) {
		case 0x00:
			name = "Latvian (Latvia)";
			break;
		case 0x01:
			name = "Latvian";
			break;
		}
		break;
	case 0x27:
		switch(secondary) {
		case 0x00:
			name = "Lithuanian (Lithuania)";
			break;
		case 0x01:
			name = "Lithuanian";
			break;
		}
		break;
	case 0x28:
		switch(secondary) {
		case 0x01:
			name = "Tajik (Tajikistan)";
			break;
		}
		break;
	case 0x29:
		switch(secondary) {
		case 0x00:
			name = "Farsi (Iran)";
			break;
		case 0x01:
			name = "Farsi";
			break;
		}
		break;
	case 0x2a:
		switch(secondary) {
		case 0x00:
			name = "Vietnamese (Viet Nam)";
			break;
		case 0x01:
			name = "Vietnamese";
			break;
		}
		break;
	case 0x2b:
		switch(secondary) {
		case 0x00:
			name = "Armenian (Armenia)";
			break;
		case 0x01:
			name = "Armenian";
			break;
		}
		break;
	case 0x2c:
		switch(secondary) {
		case 0x00:
			name = "Azeri (Latin) (Azerbaijan)";
			break;
		case 0x01:
			name = "Azeri (Latin)";
			break;
		case 0x02:
			name = "Azeri (Cyrillic)";
			break;
		}
		break;
	case 0x2d:
		switch(secondary) {
		case 0x00:
			name = "Basque (Spain)";
			break;
		case 0x01:
			name = "Basque";
			break;
		}
		break;
	case 0x2e:
		switch(secondary) {
		case 0x01:
			name = "Upper Sorbian (Germany)";
			break;
		case 0x02:
			name = "Lower Sorbian (Germany)";
			break;
		}
		break;
	case 0x2f:
		switch(secondary) {
		case 0x00:
			name = "FYRO Macedonian (Former Yugoslav Republic of Macedonia)";
			break;
		case 0x01:
			name = "FYRO Macedonian";
			break;
		}
		break;
	case 0x32:
		switch(secondary) {
		case 0x00:
			name = "Tswana (South Africa)";
			break;
		case 0x01:
			name = "Tswana";
			break;
		}
		break;
	case 0x34:
		switch(secondary) {
		case 0x00:
			name = "Xhosa (South Africa)";
			break;
		case 0x01:
			name = "Xhosa";
			break;
		}
		break;
	case 0x35:
		switch(secondary) {
		case 0x00:
			name = "Zulu (South Africa)";
			break;
		case 0x01:
			name = "Zulu";
			break;
		}
		break;
	case 0x36:
		switch(secondary) {
		case 0x00:
			name = "Afrikaans (South Africa)";
			break;
		case 0x01:
			name = "Afrikaans";
			break;
		}
		break;
	case 0x37:
		switch(secondary) {
		case 0x00:
			name = "Georgian (Georgia)";
			break;
		case 0x01:
			name = "Georgian";
			break;
		}
		break;
	case 0x38:
		switch(secondary) {
		case 0x00:
			name = "Faroese (Faroe Islands)";
			break;
		case 0x01:
			name = "Faroese";
			break;
		}
		break;
	case 0x39:
		switch(secondary) {
		case 0x00:
			name = "Hindi (India)";
			break;
		case 0x01:
			name = "Hindi";
			break;
		}
		break;
	case 0x3a:
		switch(secondary) {
		case 0x00:
			name = "Maltese (Malta)";
			break;
		case 0x01:
			name = "Maltese";
			break;
		}
		break;
	case 0x3b:
		switch(secondary) {
		case 0x00:
			name = "Sami (Northern) (Norway)";
			break;
		case 0x01:
			name = "Sami, Northern (Norway)";
			break;
		case 0x02:
			name = "Sami, Northern (Sweden)";
			break;
		case 0x03:
			name = "Sami, Northern (Finland)";
			break;
		case 0x04:
			name = "Sami, Lule (Norway)";
			break;
		case 0x05:
			name = "Sami, Lule (Sweden)";
			break;
		case 0x06:
			name = "Sami, Southern (Norway)";
			break;
		case 0x07:
			name = "Sami, Southern (Sweden)";
			break;
		case 0x08:
			name = "Sami, Skolt (Finland)";
			break;
		case 0x09:
			name = "Sami, Inari (Finland)";
			break;
		}
		break;
	case 0x3c:
		switch(secondary) {
		case 0x02:
			name = "Irish (Ireland)";
			break;
		}
		break;
	case 0x3e:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Malay (Malaysia)";
			break;
		case 0x02:
			name = "Malay (Brunei Darussalam)";
			break;
		}
		break;
	case 0x3f:
		switch(secondary) {
		case 0x00:
			name = "Kazakh (Kazakhstan)";
			break;
		case 0x01:
			name = "Kazakh";
			break;
		}
		break;
	case 0x40:
		switch(secondary) {
		case 0x00:
			name = "Kyrgyz (Kyrgyzstan)";
			break;
		case 0x01:
			name = "Kyrgyz (Cyrillic)";
			break;
		}
		break;
	case 0x41:
		switch(secondary) {
		case 0x00:
			name = "Swahili (Kenya)";
			break;
		case 0x01:
			name = "Swahili";
			break;
		}
		break;
	case 0x42:
		switch(secondary) {
		case 0x01:
			name = "Turkmen (Turkmenistan)";
			break;
		}
		break;
	case 0x43:
		switch(secondary) {
		case 0x00:
			name = "Uzbek (Latin) (Uzbekistan)";
			break;
		case 0x01:
			name = "Uzbek (Latin)";
			break;
		case 0x02:
			name = "Uzbek (Cyrillic)";
			break;
		}
		break;
	case 0x44:
		switch(secondary) {
		case 0x00:
			name = "Tatar (Russia)";
			break;
		case 0x01:
			name = "Tatar";
			break;
		}
		break;
	case 0x45:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Bengali (India)";
			break;
		}
		break;
	case 0x46:
		switch(secondary) {
		case 0x00:
			name = "Punjabi (India)";
			break;
		case 0x01:
			name = "Punjabi";
			break;
		}
		break;
	case 0x47:
		switch(secondary) {
		case 0x00:
			name = "Gujarati (India)";
			break;
		case 0x01:
			name = "Gujarati";
			break;
		}
		break;
	case 0x49:
		switch(secondary) {
		case 0x00:
			name = "Tamil (India)";
			break;
		case 0x01:
			name = "Tamil";
			break;
		}
		break;
	case 0x4a:
		switch(secondary) {
		case 0x00:
			name = "Telugu (India)";
			break;
		case 0x01:
			name = "Telugu";
			break;
		}
		break;
	case 0x4b:
		switch(secondary) {
		case 0x00:
			name = "Kannada (India)";
			break;
		case 0x01:
			name = "Kannada";
			break;
		}
		break;
	case 0x4c:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Malayalam (India)";
			break;
		}
		break;
	case 0x4d:
		switch(secondary) {
		case 0x01:
			name = "Assamese (India)";
			break;
		}
		break;
	case 0x4e:
		switch(secondary) {
		case 0x00:
			name = "Marathi (India)";
			break;
		case 0x01:
			name = "Marathi";
			break;
		}
		break;
	case 0x4f:
		switch(secondary) {
		case 0x00:
			name = "Sanskrit (India)";
			break;
		case 0x01:
			name = "Sanskrit";
			break;
		}
		break;
	case 0x50:
		switch(secondary) {
		case 0x00:
			name = "Mongolian (Mongolia)";
			break;
		case 0x01:
			name = "Mongolian (Cyrillic)";
			break;
		case 0x02:
			name = "Mongolian (PRC)";
			break;
		}
		break;
	case 0x51:
		switch(secondary) {
		case 0x01:
			name = "Tibetan (PRC)";
			break;
		case 0x02:
			name = "Tibetan (Bhutan)";
			break;
		}
		break;
	case 0x52:
		switch(secondary) {
		case 0x00:
			name = "Welsh (United Kingdom)";
			break;
		case 0x01:
			name = "Welsh";
			break;
		}
		break;
	case 0x53:
		switch(secondary) {
		case 0x01:
			name = "Khmer (Cambodia)";
			break;
		}
		break;
	case 0x54:
		switch(secondary) {
		case 0x01:
			name = "Lao (Lao PDR)";
			break;
		}
		break;
	case 0x56:
		switch(secondary) {
		case 0x00:
			name = "Galician (Spain)";
			break;
		case 0x01:
			name = "Galician";
			break;
		}
		break;
	case 0x57:
		switch(secondary) {
		case 0x00:
			name = "Konkani (India)";
			break;
		case 0x01:
			name = "Konkani";
			break;
		}
		break;
	case 0x5a:
		switch(secondary) {
		case 0x00:
			name = "Syriac (Syria)";
			break;
		case 0x01:
			name = "Syriac";
			break;
		}
		break;
	case 0x5b:
		switch(secondary) {
		case 0x01:
			name = "Sinhala (Sri Lanka)";
			break;
		}
		break;
	case 0x5d:
		switch(secondary) {
		case 0x01:
			name = "Inuktitut (Syllabics, Canada)";
			break;
		case 0x02:
			name = "Inuktitut (Latin, Canada)";
			break;
		}
		break;
	case 0x5e:
		switch(secondary) {
		case 0x01:
			name = "Amharic (Ethiopia)";
			break;
		}
		break;
	case 0x5f:
		switch(secondary) {
		case 0x02:
			name = "Tamazight (Algeria, Latin)";
			break;
		}
		break;
	case 0x61:
		switch(secondary) {
		case 0x01:
			name = "Nepali (Nepal)";
			break;
		}
		break;
	case 0x62:
		switch(secondary) {
		case 0x01:
			name = "Frisian (Netherlands)";
			break;
		}
		break;
	case 0x63:
		switch(secondary) {
		case 0x01:
			name = "Pashto (Afghanistan)";
			break;
		}
		break;
	case 0x64:
		switch(secondary) {
		case 0x01:
			name = "Filipino (Philippines)";
			break;
		}
		break;
	case 0x65:
		switch(secondary) {
		case 0x00:
			name = "Divehi (Maldives)";
			break;
		case 0x01:
			name = "Divehi";
			break;
		}
		break;
	case 0x68:
		switch(secondary) {
		case 0x01:
			name = "Hausa (Nigeria, Latin)";
			break;
		}
		break;
	case 0x6a:
		switch(secondary) {
		case 0x01:
			name = "Yoruba (Nigeria)";
			break;
		}
		break;
	case 0x6b:
		switch(secondary) {
		case 0x00:
		case 0x01:
			name = "Quechua (Bolivia)";
			break;
		case 0x02:
			name = "Quechua (Ecuador)";
			break;
		case 0x03:
			name = "Quechua (Peru)";
			break;
		}
		break;
	case 0x6c:
		switch(secondary) {
		case 0x00:
			name = "Northern Sotho (South Africa)";
			break;
		case 0x01:
			name = "Northern Sotho";
			break;
		}
		break;
	case 0x6d:
		switch(secondary) {
		case 0x01:
			name = "Bashkir (Russia)";
			break;
		}
		break;
	case 0x6e:
		switch(secondary) {
		case 0x01:
			name = "Luxembourgish (Luxembourg)";
			break;
		}
		break;
	case 0x6f:
		switch(secondary) {
		case 0x01:
			name = "Greenlandic (Greenland)";
			break;
		}
		break;
	case 0x78:
		switch(secondary) {
		case 0x01:
			name = "Yi (PRC)";
			break;
		}
		break;
	case 0x7a:
		switch(secondary) {
		case 0x01:
			name = "Mapudungun (Chile)";
			break;
		}
		break;
	case 0x7c:
		switch(secondary) {
		case 0x01:
			name = "Mohawk (Mohawk)";
			break;
		}
		break;
	case 0x7e:
		switch(secondary) {
		case 0x01:
			name = "Breton (France)";
			break;
		}
		break;
	case 0x7f:
		switch(secondary) {
		case 0x00:
			name = "Invariant Language (Invariant Country)";
			break;
		}
		break;
	case 0x80:
		switch(secondary) {
		case 0x01:
			name = "Uighur (PRC)";
			break;
		}
		break;
	case 0x81:
		switch(secondary) {
		case 0x00:
			name = "Maori (New Zealand)";
			break;
		case 0x01:
			name = "Maori";
			break;
		}
		break;
	case 0x83:
		switch(secondary) {
		case 0x01:
			name = "Corsican (France)";
			break;
		}
		break;
	case 0x84:
		switch(secondary) {
		case 0x01:
			name = "Alsatian (France)";
			break;
		}
		break;
	case 0x85:
		switch(secondary) {
		case 0x01:
			name = "Yakut (Russia)";
			break;
		}
		break;
	case 0x86:
		switch(secondary) {
		case 0x01:
			name = "K'iche (Guatemala)";
			break;
		}
		break;
	case 0x87:
		switch(secondary) {
		case 0x01:
			name = "Kinyarwanda (Rwanda)";
			break;
		}
		break;
	case 0x88:
		switch(secondary) {
		case 0x01:
			name = "Wolof (Senegal)";
			break;
		}
		break;
	case 0x8c:
		switch(secondary) {
		case 0x01:
			name = "Dari (Afghanistan)";
			break;
		}
		break;

	default:
		name = "Language Neutral";

	}
	
	if (!name)
		name = "Language Neutral";

	return copy_lang (lang_out, lang_len, name);
}
