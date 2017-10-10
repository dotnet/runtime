/**
 * \file
 */

#ifndef _MONO_METADATA_W32PROCESS_INTERNALS_H_
#define _MONO_METADATA_W32PROCESS_INTERNALS_H_

#include <config.h>
#include <glib.h>

#ifndef HOST_WIN32

typedef struct {
	guint32 dwSignature; /* Should contain 0xFEEF04BD on le machines */
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
} VS_FIXEDFILEINFO;

typedef struct {
	gpointer lpBaseOfDll;
	guint32 SizeOfImage;
	gpointer EntryPoint;
} MODULEINFO;

#define VS_FF_DEBUG		0x0001
#define VS_FF_PRERELEASE	0x0002
#define VS_FF_PATCHED		0x0004
#define VS_FF_PRIVATEBUILD	0x0008
#define VS_FF_INFOINFERRED	0x0010
#define VS_FF_SPECIALBUILD	0x0020

guint32
mono_w32process_get_pid (gpointer handle);

gboolean
mono_w32process_try_get_modules (gpointer process, gpointer *modules, guint32 size, guint32 *needed);

guint32
mono_w32process_module_get_name (gpointer process, gpointer module, gunichar2 *basename, guint32 size);

guint32
mono_w32process_module_get_filename (gpointer process, gpointer module, gunichar2 *basename, guint32 size);

gboolean
mono_w32process_module_get_information (gpointer process, gpointer module, MODULEINFO *modinfo, guint32 size);

gboolean
mono_w32process_get_fileversion_info (gunichar2 *filename, gpointer *data);

gboolean
mono_w32process_ver_query_value (gconstpointer datablock, const gunichar2 *subblock, gpointer *buffer, guint32 *len);

guint32
mono_w32process_ver_language_name (guint32 lang, gunichar2 *lang_out, guint32 lang_len);

#endif /* HOST_WIN32 */

#endif /* _MONO_METADATA_W32PROCESS_INTERNALS_H_ */
