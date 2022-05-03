// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_error_init, (MonoError *error))
MONO_API_FUNCTION(void, mono_error_init_flags, (MonoError *error, unsigned short flags))

MONO_API_FUNCTION(void, mono_error_cleanup, (MonoError *error))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_error_ok, (MonoError *error))

MONO_API_FUNCTION(unsigned short, mono_error_get_error_code, (MonoError *error))

MONO_API_FUNCTION(const char*, mono_error_get_message, (MonoError *error))
