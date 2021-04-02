/**
 * \file
 */

#ifndef __MONO_PROC_LIB_H__
#define __MONO_PROC_LIB_H__
/*
 * Utility functions to access processes information and other info about the system.
 */

#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-publib.h>

/* never remove or reorder these enums values: they are used in corlib/System */

typedef enum {
	MONO_PROCESS_NUM_THREADS,
	MONO_PROCESS_USER_TIME, /* milliseconds */
	MONO_PROCESS_SYSTEM_TIME, /* milliseconds */
	MONO_PROCESS_TOTAL_TIME, /* milliseconds */
	MONO_PROCESS_WORKING_SET,
	MONO_PROCESS_WORKING_SET_PEAK, /* 5 */
	MONO_PROCESS_PRIVATE_BYTES,
	MONO_PROCESS_VIRTUAL_BYTES,
	MONO_PROCESS_VIRTUAL_BYTES_PEAK,
	MONO_PROCESS_FAULTS,
	MONO_PROCESS_ELAPSED, /* 10 */
	MONO_PROCESS_PPID,
	MONO_PROCESS_PAGED_BYTES,
	MONO_PROCESS_END
} MonoProcessData;

typedef enum {
	MONO_CPU_USER_TIME,
	MONO_CPU_PRIV_TIME,
	MONO_CPU_INTR_TIME,
	MONO_CPU_DCP_TIME,
	MONO_CPU_IDLE_TIME,
	MONO_CPU_END
} MonoCpuData;

typedef enum {
	MONO_PROCESS_ERROR_NONE, /* no error happened */
	MONO_PROCESS_ERROR_NOT_FOUND, /* process not found */
	MONO_PROCESS_ERROR_OTHER
} MonoProcessError;

typedef struct _MonoCpuUsageState MonoCpuUsageState;
#ifndef HOST_WIN32
struct _MonoCpuUsageState {
	gint64 kernel_time;
	gint64 user_time;
	gint64 current_time;
};
#else
struct _MonoCpuUsageState {
	guint64 kernel_time;
	guint64 user_time;
	guint64 idle_time;
};
#endif

gpointer* mono_process_list     (int *size);

void      mono_process_get_times (gpointer pid, gint64 *start_time, gint64 *user_time, gint64 *kernel_time);

char*     mono_process_get_name (gpointer pid, char *buf, int len);

gint64    mono_process_get_data (gpointer pid, MonoProcessData data);
gint64    mono_process_get_data_with_error (gpointer pid, MonoProcessData data, MonoProcessError *error);

int       mono_process_current_pid (void);

MONO_API int       mono_cpu_count    (void);
gint64    mono_cpu_get_data (int cpu_id, MonoCpuData data, MonoProcessError *error);
gint32    mono_cpu_usage (MonoCpuUsageState *prev);

int       mono_atexit (void (*func)(void));

#ifndef HOST_WIN32

#include <sys/stat.h>
#include <unistd.h>

#define IMAGE_NUMBEROF_DIRECTORY_ENTRIES 16

#define IMAGE_DIRECTORY_ENTRY_EXPORT	0
#define IMAGE_DIRECTORY_ENTRY_IMPORT	1
#define IMAGE_DIRECTORY_ENTRY_RESOURCE	2

#define IMAGE_SIZEOF_SHORT_NAME	8

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define IMAGE_DOS_SIGNATURE	0x4d5a
#define IMAGE_NT_SIGNATURE	0x50450000
#define IMAGE_NT_OPTIONAL_HDR32_MAGIC	0xb10
#define IMAGE_NT_OPTIONAL_HDR64_MAGIC	0xb20
#else
#define IMAGE_DOS_SIGNATURE	0x5a4d
#define IMAGE_NT_SIGNATURE	0x00004550
#define IMAGE_NT_OPTIONAL_HDR32_MAGIC	0x10b
#define IMAGE_NT_OPTIONAL_HDR64_MAGIC	0x20b
#endif

typedef struct {
	guint16 e_magic;
	guint16 e_cblp;
	guint16 e_cp;
	guint16 e_crlc;
	guint16 e_cparhdr;
	guint16 e_minalloc;
	guint16 e_maxalloc;
	guint16 e_ss;
	guint16 e_sp;
	guint16 e_csum;
	guint16 e_ip;
	guint16 e_cs;
	guint16 e_lfarlc;
	guint16 e_ovno;
	guint16 e_res[4];
	guint16 e_oemid;
	guint16 e_oeminfo;
	guint16 e_res2[10];
	guint32 e_lfanew;
} IMAGE_DOS_HEADER;

typedef struct {
	guint16 Machine;
	guint16 NumberOfSections;
	guint32 TimeDateStamp;
	guint32 PointerToSymbolTable;
	guint32 NumberOfSymbols;
	guint16 SizeOfOptionalHeader;
	guint16 Characteristics;
} IMAGE_FILE_HEADER;

typedef struct {
	guint32 VirtualAddress;
	guint32 Size;
} IMAGE_DATA_DIRECTORY;

typedef struct {
	guint16 Magic;
	guint8 MajorLinkerVersion;
	guint8 MinorLinkerVersion;
	guint32 SizeOfCode;
	guint32 SizeOfInitializedData;
	guint32 SizeOfUninitializedData;
	guint32 AddressOfEntryPoint;
	guint32 BaseOfCode;
	guint32 BaseOfData;
	guint32 ImageBase;
	guint32 SectionAlignment;
	guint32 FileAlignment;
	guint16 MajorOperatingSystemVersion;
	guint16 MinorOperatingSystemVersion;
	guint16 MajorImageVersion;
	guint16 MinorImageVersion;
	guint16 MajorSubsystemVersion;
	guint16 MinorSubsystemVersion;
	guint32 Win32VersionValue;
	guint32 SizeOfImage;
	guint32 SizeOfHeaders;
	guint32 CheckSum;
	guint16 Subsystem;
	guint16 DllCharacteristics;
	guint32 SizeOfStackReserve;
	guint32 SizeOfStackCommit;
	guint32 SizeOfHeapReserve;
	guint32 SizeOfHeapCommit;
	guint32 LoaderFlags;
	guint32 NumberOfRvaAndSizes;
	IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
} IMAGE_OPTIONAL_HEADER32;

typedef struct {
	guint16 Magic;
	guint8 MajorLinkerVersion;
	guint8 MinorLinkerVersion;
	guint32 SizeOfCode;
	guint32 SizeOfInitializedData;
	guint32 SizeOfUninitializedData;
	guint32 AddressOfEntryPoint;
	guint32 BaseOfCode;
	guint64 ImageBase;
	guint32 SectionAlignment;
	guint32 FileAlignment;
	guint16 MajorOperatingSystemVersion;
	guint16 MinorOperatingSystemVersion;
	guint16 MajorImageVersion;
	guint16 MinorImageVersion;
	guint16 MajorSubsystemVersion;
	guint16 MinorSubsystemVersion;
	guint32 Win32VersionValue;
	guint32 SizeOfImage;
	guint32 SizeOfHeaders;
	guint32 CheckSum;
	guint16 Subsystem;
	guint16 DllCharacteristics;
	guint64 SizeOfStackReserve;
	guint64 SizeOfStackCommit;
	guint64 SizeOfHeapReserve;
	guint64 SizeOfHeapCommit;
	guint32 LoaderFlags;
	guint32 NumberOfRvaAndSizes;
	IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
} IMAGE_OPTIONAL_HEADER64;

#if SIZEOF_VOID_P == 8
typedef IMAGE_OPTIONAL_HEADER64 IMAGE_OPTIONAL_HEADER;
#else
typedef IMAGE_OPTIONAL_HEADER32 IMAGE_OPTIONAL_HEADER;
#endif

typedef struct {
	guint32 Signature;
	IMAGE_FILE_HEADER FileHeader;
	IMAGE_OPTIONAL_HEADER32 OptionalHeader;
} IMAGE_NT_HEADERS32;

typedef struct {
	guint32 Signature;
	IMAGE_FILE_HEADER FileHeader;
	IMAGE_OPTIONAL_HEADER64 OptionalHeader;
} IMAGE_NT_HEADERS64;

#if SIZEOF_VOID_P == 8
typedef IMAGE_NT_HEADERS64 IMAGE_NT_HEADERS;
#else
typedef IMAGE_NT_HEADERS32 IMAGE_NT_HEADERS;
#endif

typedef struct {
	guint8 Name[IMAGE_SIZEOF_SHORT_NAME];
	union {
		guint32 PhysicalAddress;
		guint32 VirtualSize;
	} Misc;
	guint32 VirtualAddress;
	guint32 SizeOfRawData;
	guint32 PointerToRawData;
	guint32 PointerToRelocations;
	guint32 PointerToLinenumbers;
	guint16 NumberOfRelocations;
	guint16 NumberOfLinenumbers;
	guint32 Characteristics;
} IMAGE_SECTION_HEADER;

#define IMAGE_FIRST_SECTION32(header) ((IMAGE_SECTION_HEADER *)((gsize)(header) + G_STRUCT_OFFSET (IMAGE_NT_HEADERS32, OptionalHeader) + GUINT16_FROM_LE (((IMAGE_NT_HEADERS32 *)(header))->FileHeader.SizeOfOptionalHeader)))

#define RT_CURSOR	0x01
#define RT_BITMAP	0x02
#define RT_ICON		0x03
#define RT_MENU		0x04
#define RT_DIALOG	0x05
#define RT_STRING	0x06
#define RT_FONTDIR	0x07
#define RT_FONT		0x08
#define RT_ACCELERATOR	0x09
#define RT_RCDATA	0x0a
#define RT_MESSAGETABLE	0x0b
#define RT_GROUP_CURSOR	0x0c
#define RT_GROUP_ICON	0x0e
#define RT_VERSION	0x10
#define RT_DLGINCLUDE	0x11
#define RT_PLUGPLAY	0x13
#define RT_VXD		0x14
#define RT_ANICURSOR	0x15
#define RT_ANIICON	0x16
#define RT_HTML		0x17
#define RT_MANIFEST	0x18

typedef struct {
	guint32 Characteristics;
	guint32 TimeDateStamp;
	guint16 MajorVersion;
	guint16 MinorVersion;
	guint16 NumberOfNamedEntries;
	guint16 NumberOfIdEntries;
} IMAGE_RESOURCE_DIRECTORY;

typedef struct {
	union {
		struct {
#if G_BYTE_ORDER == G_BIG_ENDIAN
			guint32 NameIsString:1;
			guint32 NameOffset:31;
#else
			guint32 NameOffset:31;
			guint32 NameIsString:1;
#endif
		};
		guint32 Name;
#if G_BYTE_ORDER == G_BIG_ENDIAN
		struct {
			guint16 __wapi_big_endian_padding;
			guint16 Id;
		};
#else
		guint16 Id;
#endif
	};
	union {
		guint32 OffsetToData;
		struct {
#if G_BYTE_ORDER == G_BIG_ENDIAN
			guint32 DataIsDirectory:1;
			guint32 OffsetToDirectory:31;
#else
			guint32 OffsetToDirectory:31;
			guint32 DataIsDirectory:1;
#endif
		};
	};
} IMAGE_RESOURCE_DIRECTORY_ENTRY;

typedef struct {
	guint32 OffsetToData;
	guint32 Size;
	guint32 CodePage;
	guint32 Reserved;
} IMAGE_RESOURCE_DATA_ENTRY;



gboolean
mono_pe_file_time_date_stamp (const gunichar2 *filename, guint32 *out);

gpointer
mono_pe_file_map (const gunichar2 *filename, guint32 *map_size, void **handle);

void
mono_pe_file_unmap (gpointer file_map, void *handle);
#endif /* HOST_WIN32 */

#endif /* __MONO_PROC_LIB_H__ */

