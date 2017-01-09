#ifndef __MONO_PROFILER_AOT_H__
#define __MONO_PROFILER_AOT_H__

#include <config.h>

/*
 * File format:
 * - magic
 * - major/minor version as an int, i.e. 0x00010001
 * - sequence of records terminated by a record with type TYPE_NONE
 * Record format:
 * - 1 byte record type (AotProfRecordType)
 * - 1 int record id
 * - record specific data
 * Encoding rules:
 * - int - 4 bytes little endian
 * - string - int length followed by data
 */

typedef enum {
	AOTPROF_RECORD_NONE,
	AOTPROF_RECORD_IMAGE,
	AOTPROF_RECORD_TYPE,
	AOTPROF_RECORD_GINST,
	AOTPROF_RECORD_METHOD
} AotProfRecordType;

#define AOT_PROFILER_MAGIC "AOTPROFILE"

#define AOT_PROFILER_MAJOR_VERSION 1
#define AOT_PROFILER_MINOR_VERSION 0

#endif /* __MONO_PROFILER_AOT_H__ */
