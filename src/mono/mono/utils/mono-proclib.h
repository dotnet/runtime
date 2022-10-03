/**
 * \file
 */

#ifndef __MONO_PROC_LIB_H__
#define __MONO_PROC_LIB_H__
/*
 * Utility functions to access processes information and other info about the system.
 */

#include <glib.h>
#include <mono/utils/mono-compiler.h>

MONO_COMPONENT_API
int
mono_process_current_pid (void);

MONO_API
int
mono_cpu_count (void);

#endif /* __MONO_PROC_LIB_H__ */

