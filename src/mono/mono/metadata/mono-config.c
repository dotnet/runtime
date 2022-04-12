/**
 * \file
 *
 * Runtime and assembly configuration file support routines.
 *
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#include <glib.h>
#include <string.h>

#include "mono/metadata/assembly.h"
#include "mono/metadata/loader.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/mono-config.h"
#include "mono/metadata/mono-config-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/object-internals.h"
#include "mono/utils/mono-logger-internals.h"

#if defined(TARGET_PS3)
#define CONFIG_OS "CellOS"
#elif defined(__linux__)
#define CONFIG_OS "linux"
#elif defined(__APPLE__)
#define CONFIG_OS "osx"
#elif defined(sun)
#define CONFIG_OS "solaris"
#elif defined(__FreeBSD__)
#define CONFIG_OS "freebsd"
#elif defined(__NetBSD__)
#define CONFIG_OS "netbsd"
#elif defined(__OpenBSD__)
#define CONFIG_OS "openbsd"
#elif defined(__WIN32__) || defined(TARGET_WIN32)
#define CONFIG_OS "windows"
#elif defined(_IBMR2)
#define CONFIG_OS "aix"
#elif defined(__hpux)
#define CONFIG_OS "hpux"
#elif defined(__HAIKU__)
#define CONFIG_OS "haiku"
#elif defined (TARGET_WASM)
#define CONFIG_OS "wasm"
#else
#warning Unknown operating system
#define CONFIG_OS "unknownOS"
#endif

#ifndef CONFIG_CPU
#if defined(__i386__) || defined(_M_IX86) || defined(TARGET_X86)
#define CONFIG_CPU "x86"
#define CONFIG_WORDSIZE "32"
#elif defined(__x86_64__) || defined(_M_X64) || defined(TARGET_AMD64)
#define CONFIG_CPU "x86-64"
#define CONFIG_WORDSIZE "64"
#elif defined(__ppc64__) || defined(__powerpc64__) || defined(_ARCH_64) || defined(TARGET_POWERPC)
#define CONFIG_WORDSIZE "64"
#ifdef __mono_ppc_ilp32__
#   define CONFIG_CPU "ppc64ilp32"
#else
#   define CONFIG_CPU "ppc64"
#endif
#elif defined(__ppc__) || defined(__powerpc__)
#define CONFIG_CPU "ppc"
#define CONFIG_WORDSIZE "32"
#elif defined(__s390x__)
#define CONFIG_CPU "s390x"
#define CONFIG_WORDSIZE "64"
#elif defined(__s390__)
#define CONFIG_CPU "s390"
#define CONFIG_WORDSIZE "32"
#elif defined(__arm__) || defined(TARGET_ARM)
#define CONFIG_CPU "arm"
#define CONFIG_WORDSIZE "32"
#elif defined(__aarch64__) || defined(TARGET_ARM64)
#define CONFIG_CPU "armv8"
#define CONFIG_WORDSIZE "64"
#elif defined (TARGET_RISCV32)
#define CONFIG_CPU "riscv32"
#define CONFIG_WORDSIZE "32"
#elif defined (TARGET_RISCV64)
#define CONFIG_CPU "riscv64"
#define CONFIG_WORDSIZE "64"
#elif defined(TARGET_WASM)
#define CONFIG_CPU "wasm"
#define CONFIG_WORDSIZE "32"
#else
#error Unknown CPU
#define CONFIG_CPU "unknownCPU"
#endif
#endif

/**
 * mono_config_get_os:
 *
 * Returns the operating system that Mono is running on, as used for dllmap entries.
 */
const char *
mono_config_get_os (void)
{
	return CONFIG_OS;
}

/**
 * mono_config_get_cpu:
 *
 * Returns the architecture that Mono is running on, as used for dllmap entries.
 */
const char *
mono_config_get_cpu (void)
{
	return CONFIG_CPU;
}

/**
 * mono_config_get_wordsize:
 *
 * Returns the word size that Mono is running on, as used for dllmap entries.
 */
const char *
mono_config_get_wordsize (void)
{
	return CONFIG_WORDSIZE;
}

static char *mono_cfg_dir;
static const char *bundled_machine_config;

/**
 * mono_set_config_dir:
 * Invoked during startup
 */
void
mono_set_config_dir (const char *dir)
{
	/* If this environment variable is set, overrides the directory computed */
	char *env_mono_cfg_dir = g_getenv ("MONO_CFG_DIR");
	if (env_mono_cfg_dir == NULL && dir != NULL)
		env_mono_cfg_dir = g_strdup (dir);

	if (mono_cfg_dir)
		g_free (mono_cfg_dir);
	mono_cfg_dir = env_mono_cfg_dir;
}

/**
 * mono_get_config_dir:
 */
const char*
mono_get_config_dir (void)
{
	if (mono_cfg_dir == NULL)
		mono_set_dirs (NULL, NULL);

	return mono_cfg_dir;
}

/**
 * mono_register_machine_config:
 */
void
mono_register_machine_config (const char *config_xml)
{
	bundled_machine_config = config_xml;
}

/**
 * mono_get_machine_config:
 */
const char *
mono_get_machine_config (void)
{
	return bundled_machine_config;
}

const char *
mono_config_string_for_assembly_file (const char *filename)
{
       return NULL;
}

static mono_bool mono_server_mode = FALSE;

/**
 * mono_config_set_server_mode:
 */
void
mono_config_set_server_mode (mono_bool server_mode)
{
	mono_server_mode = server_mode;
}

/**
 * mono_config_is_server_mode:
 */
mono_bool
mono_config_is_server_mode (void)
{
	return mono_server_mode;
}
