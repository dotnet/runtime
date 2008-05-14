/*
 * coree.h: mscoree.dll functions
 *
 * Author:
 *   Kornel Pal <http://www.kornelpal.hu/>
 *
 * Copyright (C) 2008 Kornel Pal
 */

#ifndef __MONO_COREE_H__
#define __MONO_COREE_H__

#include <config.h>

#ifdef PLATFORM_WIN32

#include <mono/io-layer/io-layer.h>
#include "image.h"

#define STATUS_SUCCESS 0x00000000L
#define STATUS_INVALID_IMAGE_FORMAT 0xC000007BL

STDAPI MonoFixupCorEE(HMODULE ModuleHandle);
STDAPI MonoFixupExe(HMODULE ModuleHandle);

extern HMODULE mono_module_handle MONO_INTERNAL;
extern HMODULE coree_module_handle MONO_INTERNAL;

gchar* mono_get_module_file_name (HMODULE module_handle) MONO_INTERNAL;
void mono_load_coree (const char* file_name) MONO_INTERNAL;
void mono_fixup_exe_image (MonoImage* image) MONO_INTERNAL;

/* Declared in image.c. */
MonoImage* mono_image_open_from_module_handle (HMODULE module_handle, char* fname, int ref_count, MonoImageOpenStatus* status) MONO_INTERNAL;

#endif /* PLATFORM_WIN32 */

#endif /* __MONO_COREE_H__ */
