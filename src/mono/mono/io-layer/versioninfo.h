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

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define VS_FFI_SIGNATURE	0xbd04effe
#define VS_FFI_STRUCVERSION	0x00000100
#else
#define VS_FFI_SIGNATURE	0xfeef04bd
#define VS_FFI_STRUCVERSION	0x00010000
#endif

#define VS_FFI_FILEFLAGSMASK	0x3f

#endif /* _WAPI_VERSIONINFO_H_ */
