/*
 * mono-config.h
 *
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */
#ifndef __MONO_METADATA_CONFIG_H__
#define __MONO_METADATA_CONFIG_H__

const char* mono_get_config_dir (void);
void        mono_set_config_dir (const char *dir);

void mono_config_parse        (const char *filename);
void mono_config_for_assembly (MonoImage *assembly);
void mono_config_parse_memory (const char *buffer);

#endif /* __MONO_METADATA_CONFIG_H__ */

