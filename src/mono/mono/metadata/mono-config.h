/**
 * \file
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

MONO_API const char *mono_config_get_os (void);
MONO_API const char *mono_config_get_cpu (void);
MONO_API const char *mono_config_get_wordsize (void);

MONO_API const char* mono_get_config_dir (void);
MONO_API void        mono_set_config_dir (const char *dir);

MONO_API const char* mono_get_machine_config (void);

MONO_API void mono_config_cleanup      (void);
MONO_API void mono_config_parse        (const char *filename);
MONO_API void mono_config_for_assembly (MonoImage *assembly);
MONO_API void mono_config_parse_memory (const char *buffer);

MONO_API const char* mono_config_string_for_assembly_file (const char *filename);

MONO_API void mono_config_set_server_mode (mono_bool server_mode);
MONO_API mono_bool mono_config_is_server_mode (void);

MONO_END_DECLS

#endif /* __MONO_METADATA_CONFIG_H__ */

