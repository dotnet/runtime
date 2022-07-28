// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

// WARNING: The functions in this header are no-ops and provided for source compatibility with older versions of mono only.

MONO_API_FUNCTION(void, mono_counters_enable, (int section_mask))
MONO_API_FUNCTION(void, mono_counters_init, (void))

/*
 * register addr as the address of a counter of type type.
 * It may be a function pointer if MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 */
MONO_API_FUNCTION(void, mono_counters_register, (const char* descr, int type, void *addr))
MONO_API_FUNCTION(void, mono_counters_register_with_size, (const char *name, int type, void *addr, int size))

MONO_API_FUNCTION(void, mono_counters_on_register, (MonoCounterRegisterCallback callback))

/*
 * Create a readable dump of the counters for section_mask sections (ORed section values)
 */
MONO_API_FUNCTION(void, mono_counters_dump, (int section_mask, FILE *outfile))

MONO_API_FUNCTION(void, mono_counters_cleanup, (void))

MONO_API_FUNCTION(void, mono_counters_foreach, (CountersEnumCallback cb, void *user_data))

MONO_API_FUNCTION(int, mono_counters_sample, (MonoCounter *counter, void *buffer, int buffer_size))

MONO_API_FUNCTION(const char*, mono_counter_get_name, (MonoCounter *name))
MONO_API_FUNCTION(int, mono_counter_get_type, (MonoCounter *counter))
MONO_API_FUNCTION(int, mono_counter_get_section, (MonoCounter *counter))
MONO_API_FUNCTION(int, mono_counter_get_unit, (MonoCounter *counter))
MONO_API_FUNCTION(int, mono_counter_get_variance, (MonoCounter *counter))
MONO_API_FUNCTION(size_t, mono_counter_get_size, (MonoCounter *counter))

MONO_API_FUNCTION(int,  mono_runtime_resource_limit, (int resource_type, uintptr_t soft_limit, uintptr_t hard_limit))
MONO_API_FUNCTION(void, mono_runtime_resource_set_callback, (MonoResourceCallback callback))
MONO_API_FUNCTION(void, mono_runtime_resource_check_limit,  (int resource_type, uintptr_t value))
