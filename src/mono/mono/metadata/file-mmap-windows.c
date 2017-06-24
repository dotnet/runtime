/**
 * \file
 * MemoryMappedFile internal calls for Windows
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * The code in this file has been inspired by the CoreFX MemoryMappedFile Windows implementation contained in the files
 *
 * https://github.com/dotnet/corefx/blob/master/src/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.Windows.cs
 * https://github.com/dotnet/corefx/blob/master/src/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedView.Windows.cs
 */

#include <config.h>
#include <glib.h>
#include <mono/utils/mono-compiler.h>
#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) && defined(HOST_WIN32)

#include <glib.h>

#include <mono/metadata/file-mmap.h>

// These control the retry behaviour when lock violation errors occur during Flush:
#define MAX_FLUSH_WAITS 15  // must be <=30
#define MAX_FLUSH_RETIRES_PER_WAIT 20

typedef struct {
	void *address;
	size_t length;
} MmapInstance;

enum {
	BAD_CAPACITY_FOR_FILE_BACKED = 1,
	CAPACITY_SMALLER_THAN_FILE_SIZE,
	FILE_NOT_FOUND,
	FILE_ALREADY_EXISTS,
	PATH_TOO_LONG,
	COULD_NOT_OPEN,
	CAPACITY_MUST_BE_POSITIVE,
	INVALID_FILE_MODE,
	COULD_NOT_MAP_MEMORY,
	ACCESS_DENIED,
	CAPACITY_LARGER_THAN_LOGICAL_ADDRESS_SPACE
};

enum {
	FILE_MODE_CREATE_NEW = 1,
	FILE_MODE_CREATE = 2,
	FILE_MODE_OPEN = 3,
	FILE_MODE_OPEN_OR_CREATE = 4,
	FILE_MODE_TRUNCATE = 5,
	FILE_MODE_APPEND = 6,
};

enum {
	MMAP_FILE_ACCESS_READ_WRITE = 0,
	MMAP_FILE_ACCESS_READ = 1,
	MMAP_FILE_ACCESS_WRITE = 2,
	MMAP_FILE_ACCESS_COPY_ON_WRITE = 3,
	MMAP_FILE_ACCESS_READ_EXECUTE = 4,
	MMAP_FILE_ACCESS_READ_WRITE_EXECUTE = 5,
};

static DWORD get_page_access (int access)
{
	switch (access) {
	case MMAP_FILE_ACCESS_READ:
		return PAGE_READONLY;
	case MMAP_FILE_ACCESS_READ_WRITE:
		return PAGE_READWRITE;
	case MMAP_FILE_ACCESS_COPY_ON_WRITE:
		return PAGE_WRITECOPY;
	case MMAP_FILE_ACCESS_READ_EXECUTE:
		return PAGE_EXECUTE_READ;
	case MMAP_FILE_ACCESS_READ_WRITE_EXECUTE:
		return PAGE_EXECUTE_READWRITE;
	default:
		g_error ("unknown MemoryMappedFileAccess %d", access);
	}
}

static DWORD get_file_access (int access)
{
	switch (access) {
	case MMAP_FILE_ACCESS_READ:
	case MMAP_FILE_ACCESS_READ_EXECUTE:
		return GENERIC_READ;
	case MMAP_FILE_ACCESS_READ_WRITE:
	case MMAP_FILE_ACCESS_COPY_ON_WRITE:
	case MMAP_FILE_ACCESS_READ_WRITE_EXECUTE:
		return GENERIC_READ | GENERIC_WRITE;
	case MMAP_FILE_ACCESS_WRITE:
		return GENERIC_WRITE;
	default:
		g_error ("unknown MemoryMappedFileAccess %d", access);
	}
}

static int get_file_map_access (int access)
{
	switch (access) {
	case MMAP_FILE_ACCESS_READ:
		return FILE_MAP_READ;
	case MMAP_FILE_ACCESS_WRITE:
		return FILE_MAP_WRITE;
	case MMAP_FILE_ACCESS_READ_WRITE:
		return FILE_MAP_READ | FILE_MAP_WRITE;
	case MMAP_FILE_ACCESS_COPY_ON_WRITE:
		return FILE_MAP_COPY;
	case MMAP_FILE_ACCESS_READ_EXECUTE:
		return FILE_MAP_EXECUTE | FILE_MAP_READ;
	case MMAP_FILE_ACCESS_READ_WRITE_EXECUTE:
		return FILE_MAP_EXECUTE | FILE_MAP_READ | FILE_MAP_WRITE;
	default:
		g_error ("unknown MemoryMappedFileAccess %d", access);
	}
}

static int convert_win32_error (int error, int def)
{
	switch (error) {
	case ERROR_FILE_NOT_FOUND:
		return FILE_NOT_FOUND;
	case ERROR_FILE_EXISTS:
	case ERROR_ALREADY_EXISTS:
		return FILE_ALREADY_EXISTS;
	case ERROR_ACCESS_DENIED:
		return ACCESS_DENIED;
	}
	return def;
}

static void *open_handle (void *handle, MonoString *mapName, int mode, gint64 *capacity, int access, int options, int *error)
{
	g_assert (handle != NULL);

	wchar_t *w_mapName = NULL;
	HANDLE result = NULL;

	if (handle == INVALID_HANDLE_VALUE) {
		if (*capacity <= 0 && mode != FILE_MODE_OPEN) {
			*error = CAPACITY_MUST_BE_POSITIVE;
			return NULL;
		}
#if SIZEOF_VOID_P == 4
		if (*capacity > UINT32_MAX) {
			*error = CAPACITY_LARGER_THAN_LOGICAL_ADDRESS_SPACE;
			return NULL;
		}
#endif
		if (!(mode == FILE_MODE_CREATE_NEW || mode == FILE_MODE_OPEN_OR_CREATE || mode == FILE_MODE_OPEN)) {
			*error = INVALID_FILE_MODE;
			return NULL;
		}
	} else {
		FILE_STANDARD_INFO info;
		if (!GetFileInformationByHandleEx ((HANDLE) handle, FileStandardInfo, &info, sizeof (FILE_STANDARD_INFO))) {
			*error = convert_win32_error (GetLastError (), COULD_NOT_OPEN);
			return NULL;
		}
		if (*capacity == 0) {
			if (info.EndOfFile.QuadPart == 0) {
				*error = CAPACITY_SMALLER_THAN_FILE_SIZE;
				return NULL;
			}
		} else if (*capacity < info.EndOfFile.QuadPart) {
			*error = CAPACITY_SMALLER_THAN_FILE_SIZE;
			return NULL;
		}
	}

	w_mapName = mapName ? mono_string_to_utf16 (mapName) : NULL;

	if (mode == FILE_MODE_CREATE_NEW || handle != INVALID_HANDLE_VALUE) {
		result = CreateFileMappingW ((HANDLE)handle, NULL, get_page_access (access) | options, (DWORD)(((guint64)*capacity) >> 32), (DWORD)*capacity, w_mapName);
		if (result && GetLastError () == ERROR_ALREADY_EXISTS) {
			CloseHandle (result);
			result = NULL;
			*error = FILE_ALREADY_EXISTS;
		} else if (!result && GetLastError () != NO_ERROR) {
			*error = convert_win32_error (GetLastError (), COULD_NOT_OPEN);
		}
	} else if (mode == FILE_MODE_OPEN || mode == FILE_MODE_OPEN_OR_CREATE && access == MMAP_FILE_ACCESS_WRITE) {
		result = OpenFileMappingW (get_file_map_access (access), FALSE, w_mapName);
		if (!result) {
			if (mode == FILE_MODE_OPEN_OR_CREATE && GetLastError () == ERROR_FILE_NOT_FOUND) {
				*error = INVALID_FILE_MODE;
			} else {
				*error = convert_win32_error (GetLastError (), COULD_NOT_OPEN);
			}
		}
	} else if (mode == FILE_MODE_OPEN_OR_CREATE) {

		// This replicates how CoreFX does MemoryMappedFile.CreateOrOpen ().

		/// Try to open the file if it exists -- this requires a bit more work. Loop until we can
		/// either create or open a memory mapped file up to a timeout. CreateFileMapping may fail
		/// if the file exists and we have non-null security attributes, in which case we need to
		/// use OpenFileMapping.  But, there exists a race condition because the memory mapped file
		/// may have closed between the two calls -- hence the loop. 
		/// 
		/// The retry/timeout logic increases the wait time each pass through the loop and times 
		/// out in approximately 1.4 minutes. If after retrying, a MMF handle still hasn't been opened, 
		/// throw an InvalidOperationException.

		guint32 waitRetries = 14;   //((2^13)-1)*10ms == approximately 1.4mins
		guint32 waitSleep = 0;

		while (waitRetries > 0) {
			result = CreateFileMappingW ((HANDLE)handle, NULL, get_page_access (access) | options, (DWORD)(((guint64)*capacity) >> 32), (DWORD)*capacity, w_mapName);
			if (result)
				break;
			if (GetLastError() != ERROR_ACCESS_DENIED) {
				*error = convert_win32_error (GetLastError (), COULD_NOT_OPEN);
				break;
			}
			result = OpenFileMappingW (get_file_map_access (access), FALSE, w_mapName);
			if (result)
				break;
			if (GetLastError () != ERROR_FILE_NOT_FOUND) {
				*error = convert_win32_error (GetLastError (), COULD_NOT_OPEN);
				break;
			}
			// increase wait time
			--waitRetries;
			if (waitSleep == 0) {
				waitSleep = 10;
			} else {
				mono_thread_info_sleep (waitSleep, NULL);
				waitSleep *= 2;
			}
		}

		if (!result) {
			*error = COULD_NOT_OPEN;
		}
	}

	if (w_mapName)
		g_free (w_mapName);
	return result;
}

void *mono_mmap_open_file (MonoString *path, int mode, MonoString *mapName, gint64 *capacity, int access, int options, int *error)
{
	g_assert (path != NULL || mapName != NULL);

	wchar_t *w_path = NULL;
	HANDLE hFile = INVALID_HANDLE_VALUE;
	HANDLE result = NULL;
	gboolean delete_on_error = FALSE;

	if (path) {
		w_path = mono_string_to_utf16 (path);
		WIN32_FILE_ATTRIBUTE_DATA file_attrs;
		gboolean existed = GetFileAttributesExW (w_path, GetFileExInfoStandard, &file_attrs);
		if (!existed && mode == FILE_MODE_CREATE_NEW && *capacity == 0) {
			*error = CAPACITY_SMALLER_THAN_FILE_SIZE;
			goto done;
		}
		hFile = CreateFileW (w_path, get_file_access (access), FILE_SHARE_READ, NULL, mode, FILE_ATTRIBUTE_NORMAL, NULL);
		if (hFile == INVALID_HANDLE_VALUE) {
			*error = convert_win32_error (GetLastError (), COULD_NOT_OPEN);
			goto done;
		}
		delete_on_error = !existed;
	}

	result = open_handle (hFile, mapName, mode, capacity, access, options, error);

done:
	if (hFile != INVALID_HANDLE_VALUE)
		CloseHandle (hFile);
	if (!result && delete_on_error)
		DeleteFileW (w_path);
	if (w_path)
		g_free (w_path);

	return result;
}

void *mono_mmap_open_handle (void *handle, MonoString *mapName, gint64 *capacity, int access, int options, int *error)
{
	g_assert (handle != NULL);

	return open_handle (handle, mapName, FILE_MODE_OPEN, capacity, access, options, error);
}

void mono_mmap_close (void *mmap_handle)
{
	g_assert (mmap_handle);
	CloseHandle ((HANDLE) mmap_handle);
}

void mono_mmap_configure_inheritability (void *mmap_handle, gboolean inheritability)
{
	g_assert (mmap_handle);
	if (!SetHandleInformation ((HANDLE) mmap_handle, HANDLE_FLAG_INHERIT, inheritability ? HANDLE_FLAG_INHERIT : 0)) {
		g_error ("mono_mmap_configure_inheritability: SetHandleInformation failed with error %d!", GetLastError ());
	}
}

void mono_mmap_flush (void *mmap_handle)
{
	g_assert (mmap_handle);
	MmapInstance *h = (MmapInstance *)mmap_handle;

	if (FlushViewOfFile (h->address, h->length))
		return;

	// This replicates how CoreFX does MemoryMappedView.Flush ().

	// It is a known issue within the NTFS transaction log system that
	// causes FlushViewOfFile to intermittently fail with ERROR_LOCK_VIOLATION
	// As a workaround, we catch this particular error and retry the flush operation 
	// a few milliseconds later. If it does not work, we give it a few more tries with
	// increasing intervals. Eventually, however, we need to give up. In ad-hoc tests
	// this strategy successfully flushed the view after no more than 3 retries.

	if (GetLastError () != ERROR_LOCK_VIOLATION)
		// TODO: Propagate error to caller
		return;

	for (int w = 0; w < MAX_FLUSH_WAITS; w++) {
		int pause = (1 << w);  // MaxFlushRetries should never be over 30
		mono_thread_info_sleep (pause, NULL);

		for (int r = 0; r < MAX_FLUSH_RETIRES_PER_WAIT; r++) {
			if (FlushViewOfFile (h->address, h->length))
				return;

			if (GetLastError () != ERROR_LOCK_VIOLATION)
				// TODO: Propagate error to caller
				return;

			mono_thread_info_yield ();
		}
	}

	// We got to here, so there was no success:
	// TODO: Propagate error to caller
}

int mono_mmap_map (void *handle, gint64 offset, gint64 *size, int access, void **mmap_handle, void **base_address)
{
	static DWORD allocationGranularity = 0;
	if (allocationGranularity == 0) {
		SYSTEM_INFO info;
		GetSystemInfo (&info);
		allocationGranularity = info.dwAllocationGranularity;
	}

	gint64 extraMemNeeded = offset % allocationGranularity;
	guint64 newOffset = offset - extraMemNeeded;
	gint64 nativeSize = (*size != 0) ? *size + extraMemNeeded : 0;

#if SIZEOF_VOID_P == 4
	if (nativeSize > UINT32_MAX)
		return CAPACITY_LARGER_THAN_LOGICAL_ADDRESS_SPACE;
#endif
	
	void *address = MapViewOfFile ((HANDLE) handle, get_file_map_access (access), (DWORD) (newOffset >> 32), (DWORD) newOffset, (SIZE_T) nativeSize);
	if (!address)
		return convert_win32_error (GetLastError (), COULD_NOT_MAP_MEMORY);

	// Query the view for its size and allocation type
	MEMORY_BASIC_INFORMATION viewInfo;
	VirtualQuery (address, &viewInfo, sizeof (MEMORY_BASIC_INFORMATION));
	guint64 viewSize = (guint64) viewInfo.RegionSize;

	// Allocate the pages if we were using the MemoryMappedFileOptions.DelayAllocatePages option
	// OR check if the allocated view size is smaller than the expected native size
	// If multiple overlapping views are created over the file mapping object, the pages in a given region
	// could have different attributes(MEM_RESERVE OR MEM_COMMIT) as MapViewOfFile preserves coherence between 
	// views created on a mapping object backed by same file.
	// In which case, the viewSize will be smaller than nativeSize required and viewState could be MEM_COMMIT 
	// but more pages may need to be committed in the region.
	// This is because, VirtualQuery function(that internally invokes VirtualQueryEx function) returns the attributes 
	// and size of the region of pages with matching attributes starting from base address.
	// VirtualQueryEx: http://msdn.microsoft.com/en-us/library/windows/desktop/aa366907(v=vs.85).aspx
	if (((viewInfo.State & MEM_RESERVE) != 0) || viewSize < (guint64) nativeSize) {
		void *tempAddress = VirtualAlloc (address, nativeSize != 0 ? nativeSize : viewSize, MEM_COMMIT, get_page_access (access));
		if (!tempAddress) {
			return convert_win32_error (GetLastError (), COULD_NOT_MAP_MEMORY);
		}
		// again query the view for its new size
		VirtualQuery (address, &viewInfo, sizeof (MEMORY_BASIC_INFORMATION));
		viewSize = (guint64) viewInfo.RegionSize;
	}

	if (*size == 0)
		*size = viewSize - extraMemNeeded;

	MmapInstance *h = g_malloc0 (sizeof (MmapInstance));
	h->address = address;
	h->length = *size + extraMemNeeded;
	*mmap_handle = h;
	*base_address = (char*) address + (offset - newOffset);

	return 0;
}

gboolean mono_mmap_unmap (void *mmap_handle)
{
	g_assert (mmap_handle);

	MmapInstance *h = (MmapInstance *) mmap_handle;

	gboolean result = UnmapViewOfFile (h->address);

	g_free (h);
	return result;
}

#else

MONO_EMPTY_SOURCE_FILE (file_mmap_windows);

#endif
