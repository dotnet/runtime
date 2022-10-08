// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(const char *, mono_config_get_os, (void))
MONO_API_FUNCTION(const char *, mono_config_get_cpu, (void))
MONO_API_FUNCTION(const char *, mono_config_get_wordsize, (void))

/**
 * These functions are no-ops since app.config/machine.config handling is not available in dotnet/runtime.
 */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY const char*, mono_get_config_dir, (void))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_set_config_dir, (const char *dir))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY const char *, mono_get_machine_config, (void))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_config_cleanup, (void))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_config_parse, (const char *filename))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_config_for_assembly, (MonoImage *assembly))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_config_parse_memory, (const char *buffer))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY const char*, mono_config_string_for_assembly_file, (const char *filename))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_config_set_server_mode, (mono_bool server_mode))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_config_is_server_mode, (void))
