/**
 * \file
 *
 * Copyright 2015 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * This file contains functions to return the values of various directories defined in Makefile.am.
 */

#include <mono/metadata/mono-config-dirs.h>

const char*
mono_config_get_assemblies_dir (void)
{
#ifdef MONO_ASSEMBLIES
	return MONO_ASSEMBLIES;
#else
	return NULL;
#endif
}

const char*
mono_config_get_cfg_dir (void)
{
#ifdef MONO_CFG_DIR
	return MONO_CFG_DIR;
#else
	return NULL;
#endif
}

const char*
mono_config_get_bin_dir (void)
{
#ifdef MONO_BINDIR
	return MONO_BINDIR;
#else
	return NULL;
#endif
}

const char*
mono_config_get_reloc_lib_dir (void)
{
#ifdef MONO_RELOC_LIBDIR
	return MONO_RELOC_LIBDIR;
#else
	return NULL;
#endif
}

