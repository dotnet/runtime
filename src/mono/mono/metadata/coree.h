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

STDAPI MonoFixupCorEE(HMODULE ModuleHandle);
STDAPI MonoFixupExe(HMODULE ModuleHandle);

extern HMODULE mono_module_handle;
extern HMODULE coree_module_handle;

gchar* mono_get_module_file_name (HMODULE module_handle);
void mono_load_coree ();
void mono_fixup_exe_image (MonoImage* image);

/* Declared in image.c. */
MonoImage* mono_image_open_from_module_handle (HMODULE module_handle, const char* fname, MonoImageOpenStatus* status);

#endif /* PLATFORM_WIN32 */

#endif /* __MONO_COREE_H__ */
