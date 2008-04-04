/*
 * versioninfo.h:  Version info structures found in PE file resources
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_VERSIONINFO_H_
#define _WAPI_VERSIONINFO_H_

#include <glib.h>

/*
 * VS_VERSIONINFO:
 *
 * 2 bytes: Length in bytes (this block, and all child blocks. does _not_ include alignment padding between blocks)
 * 2 bytes: Length in bytes of VS_FIXEDFILEINFO struct
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string (null terminated): Key (currently "VS_VERSION_INFO")
 * Variable length padding to align VS_FIXEDFILEINFO on a 32-bit boundary
 * VS_FIXEDFILEINFO struct
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child struct (zero or one StringFileInfo structs, zero or one VarFileInfo structs)
 */

/*
 * StringFileInfo:
 *
 * 2 bytes: Length in bytes (includes this block, as well as all Child blocks)
 * 2 bytes: Value length (always zero)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key (currently "StringFileInfo")
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child structs ( one or more StringTable structs.  Each StringTable struct's Key member indicates the appropriate language and code page for displaying the text in that StringTable struct.)
 */

/*
 * StringTable:
 *
 * 2 bytes: Length in bytes (includes this block as well as all Child blocks, but excludes any padding between String blocks)
 * 2 bytes: Value length (always zero)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key. An 8-digit hex number stored as a unicode string.  The four most significant digits represent the language identifier.  The four least significant digits represent the code page for which the data is formatted.
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child structs (an array of one or more String structs (each aligned on a 32-bit boundary)
 */

/*
 * String:
 *
 * 2 bytes: Length in bytes (of this block)
 * 2 bytes: Value length (the length in words of the Value member)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key. arbitrary string, identifies data.
 * Variable length padding to align Value on a 32-bit boundary
 * Value: Variable length unicode string, holding data.
 */

/*
 * VarFileInfo:
 *
 * 2 bytes: Length in bytes (includes this block, as well as all Child blocks)
 * 2 bytes: Value length (always zero)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key (currently "VarFileInfo")
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child structs (a Var struct)
 */

/*
 * Var:
 *
 * 2 bytes: Length in bytes of this block
 * 2 bytes: Value length in bytes of the Value
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key ("Translation")
 * Variable length padding to align Value on a 32-bit boundary
 * Value: an array of one or more 4 byte values that are language and code page identifier pairs, low-order word containing a language identifier, and the high-order word containing a code page number.  Either word can be zero, indicating that the file is language or code page independent.
 */

typedef struct
{
	guint32 dwSignature;		/* Should contain 0xFEEF04BD
					 * on le machines */
	guint32 dwStrucVersion;
	guint32 dwFileVersionMS;
	guint32 dwFileVersionLS;
	guint32 dwProductVersionMS;
	guint32 dwProductVersionLS;
	guint32 dwFileFlagsMask;
	guint32 dwFileFlags;
	guint32 dwFileOS;
	guint32 dwFileType;
	guint32 dwFileSubtype;
	guint32 dwFileDateMS;
	guint32 dwFileDateLS;
} WapiFixedFileInfo;

#if G_BYTE_ORDER == G_BIG_ENDIAN
#define VS_FFI_SIGNATURE	0xbd04effe
#define VS_FFI_STRUCVERSION	0x00000100
#else
#define VS_FFI_SIGNATURE	0xfeef04bd
#define VS_FFI_STRUCVERSION	0x00010000
#endif

#define VS_FFI_FILEFLAGSMASK	0x3f

typedef struct
{
	gpointer lpBaseOfDll;
	guint32 SizeOfImage;
	gpointer EntryPoint;
} WapiModuleInfo;

#define IMAGE_NUMBEROF_DIRECTORY_ENTRIES 16

#define IMAGE_DIRECTORY_ENTRY_EXPORT	0
#define IMAGE_DIRECTORY_ENTRY_IMPORT	1
#define IMAGE_DIRECTORY_ENTRY_RESOURCE	2
#define IMAGE_DIRECTORY_ENTRY_EXCEPTION	3
#define IMAGE_DIRECTORY_ENTRY_SECURITY	4
#define IMAGE_DIRECTORY_ENTRY_BASERELOC	5
#define IMAGE_DIRECTORY_ENTRY_DEBUG	6
#define IMAGE_DIRECTORY_ENTRY_COPYRIGHT	7
#define IMAGE_DIRECTORY_ENTRY_ARCHITECTURE	7
#define IMAGE_DIRECTORY_ENTRY_GLOBALPTR	8
#define IMAGE_DIRECTORY_ENTRY_TLS	9
#define IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG	10
#define IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT	11
#define IMAGE_DIRECTORY_ENTRY_IAT	12
#define IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT	13
#define IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR	14

#define IMAGE_SIZEOF_SHORT_NAME	8

#define IMAGE_RESOURCE_NAME_IS_STRING		0x80000000
#define IMAGE_RESOURCE_DATA_IS_DIRECTORY	0x80000000

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

typedef struct
{
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
} WapiImageDosHeader;

typedef struct
{
	guint16 Machine;
	guint16 NumberOfSections;
	guint32 TimeDateStamp;
	guint32 PointerToSymbolTable;
	guint32 NumberOfSymbols;
	guint16 SizeOfOptionalHeader;
	guint16 Characteristics;
} WapiImageFileHeader;

typedef struct
{
	guint32 VirtualAddress;
	guint32 Size;
} WapiImageDataDirectory;

typedef struct
{
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
	WapiImageDataDirectory DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
} WapiImageOptionalHeader32;

typedef struct
{
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
	WapiImageDataDirectory DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
} WapiImageOptionalHeader64;

#if SIZEOF_VOID_P == 8
typedef WapiImageOptionalHeader64	WapiImageOptionalHeader;
#else
typedef WapiImageOptionalHeader32	WapiImageOptionalHeader;
#endif

typedef struct
{
	guint32 Signature;
	WapiImageFileHeader FileHeader;
	WapiImageOptionalHeader32 OptionalHeader;
} WapiImageNTHeaders32;

typedef struct
{
	guint32 Signature;
	WapiImageFileHeader FileHeader;
	WapiImageOptionalHeader64 OptionalHeader;
} WapiImageNTHeaders64;

#if SIZEOF_VOID_P == 8
typedef WapiImageNTHeaders64	WapiImageNTHeaders;
#else
typedef WapiImageNTHeaders32	WapiImageNTHeaders;
#endif

typedef struct
{
	guint8 Name[IMAGE_SIZEOF_SHORT_NAME];
	union
	{
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
} WapiImageSectionHeader;

#define IMAGE_FIRST_SECTION(header) ((WapiImageSectionHeader *)((gsize)(header) + G_STRUCT_OFFSET (WapiImageNTHeaders, OptionalHeader) + GUINT16_FROM_LE (((WapiImageNTHeaders *)(header))->FileHeader.SizeOfOptionalHeader)))

#define _WAPI_IMAGE_FIRST_SECTION32(header) ((WapiImageSectionHeader *)((gsize)(header) + G_STRUCT_OFFSET (WapiImageNTHeaders32, OptionalHeader) + GUINT16_FROM_LE (((WapiImageNTHeaders32 *)(header))->FileHeader.SizeOfOptionalHeader)))

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

typedef struct
{
	guint32 Characteristics;
	guint32 TimeDateStamp;
	guint16 MajorVersion;
	guint16 MinorVersion;
	guint16 NumberOfNamedEntries;
	guint16 NumberOfIdEntries;
} WapiImageResourceDirectory;

typedef struct
{
	union 
	{
		struct 
		{
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
		struct
		{
			guint16 __wapi_big_endian_padding;
			guint16 Id;
		};
#else
		guint16 Id;
#endif
	};
	union
	{
		guint32 OffsetToData;
		struct 
		{
#if G_BYTE_ORDER == G_BIG_ENDIAN
			guint32 DataIsDirectory:1;
			guint32 OffsetToDirectory:31;
#else
			guint32 OffsetToDirectory:31;
			guint32 DataIsDirectory:1;
#endif
		};
	};
} WapiImageResourceDirectoryEntry;

typedef struct 
{
	guint32 OffsetToData;
	guint32 Size;
	guint32 CodePage;
	guint32 Reserved;
} WapiImageResourceDataEntry;

#define VS_FF_DEBUG		0x0001
#define VS_FF_PRERELEASE	0x0002
#define VS_FF_PATCHED		0x0004
#define VS_FF_PRIVATEBUILD	0x0008
#define VS_FF_INFOINFERRED	0x0010
#define VS_FF_SPECIALBUILD	0x0020

#define VOS_UNKNOWN		0x00000000
#define VOS_DOS			0x00010000
#define VOS_OS216		0x00020000
#define VOS_OS232		0x00030000
#define VOS_NT			0x00040000
#define VOS__BASE		0x00000000
#define VOS__WINDOWS16		0x00000001
#define VOS__PM16		0x00000002
#define VOS__PM32		0x00000003
#define VOS__WINDOWS32		0x00000004
/* Should "embrace and extend" here with some entries for linux etc */

#define VOS_DOS_WINDOWS16	0x00010001
#define VOS_DOS_WINDOWS32	0x00010004
#define VOS_OS216_PM16		0x00020002
#define VOS_OS232_PM32		0x00030003
#define VOS_NT_WINDOWS32	0x00040004

#define VFT_UNKNOWN		0x0000
#define VFT_APP			0x0001
#define VFT_DLL			0x0002
#define VFT_DRV			0x0003
#define VFT_FONT		0x0004
#define VFT_VXD			0x0005
#define VFT_STATIC_LIB		0x0007

#define VFT2_UNKNOWN		0x0000
#define VFT2_DRV_PRINTER	0x0001
#define VFT2_DRV_KEYBOARD	0x0002
#define VFT2_DRV_LANGUAGE	0x0003
#define VFT2_DRV_DISPLAY	0x0004
#define VFT2_DRV_MOUSE		0x0005
#define VFT2_DRV_NETWORK	0x0006
#define VFT2_DRV_SYSTEM		0x0007
#define VFT2_DRV_INSTALLABLE	0x0008
#define VFT2_DRV_SOUND		0x0009
#define VFT2_DRV_COMM		0x000a
#define VFT2_DRV_INPUTMETHOD	0x000b
#define VFT2_FONT_RASTER	0x0001
#define VFT2_FONT_VECTOR	0x0002
#define VFT2_FONT_TRUETYPE	0x0003

#define MAKELANGID(primary,secondary) ((guint16)((secondary << 10) | (primary)))

extern guint32 GetFileVersionInfoSize (gunichar2 *filename, guint32 *handle);
extern gboolean GetFileVersionInfo (gunichar2 *filename, guint32 handle,
				    guint32 len, gpointer data);
extern gboolean VerQueryValue (gconstpointer datablock,
			       const gunichar2 *subblock, gpointer *buffer,
			       guint32 *len);
extern guint32 VerLanguageName (guint32 lang, gunichar2 *lang_out,
				guint32 lang_len);

#endif /* _WAPI_VERSIONINFO_H_ */
