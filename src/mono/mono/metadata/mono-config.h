/*
 * mono-config.h
 *
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */
#ifndef __MONO_METADATA_CONFIG_H__
#define __MONO_METADATA_CONFIG_H__

#include <mono/utils/mono-publib.h>
#include <mono/metadata/image.h>

MONO_BEGIN_DECLS

const char* mono_get_config_dir (void);
void        mono_set_config_dir (const char *dir);

const char* mono_get_machine_config (void);

void mono_config_cleanup      (void);
void mono_config_parse        (const char *filename);
void mono_config_for_assembly (MonoImage *assembly);
void mono_config_parse_memory (const char *buffer);

const char* mono_config_string_for_assembly_file (const char *filename);

MONO_END_DECLS

#endif /* __MONO_METADATA_CONFIG_H__ */

