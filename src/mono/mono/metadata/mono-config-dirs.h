/**
 * \file
 */

#ifndef __MONO_CONFIG_INTERNAL_H__
#define __MONO_CONFIG_INTERNAL_H__

#include <config.h>
#include <glib.h>

const char*
mono_config_get_assemblies_dir (void);

const char*
mono_config_get_cfg_dir (void);

const char*
mono_config_get_bin_dir (void);

const char*
mono_config_get_reloc_lib_dir (void);

#endif
