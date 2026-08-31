#ifndef __MONO_PROFILER_AOT_H__
#define __MONO_PROFILER_AOT_H__

#include <config.h>

/*
 * File format:
 * - magic (AOT_PROFILER_MAGIC)
 * - int: major/minor version, e.g. 0x00010000 (AOT_PROFILER_MAJOR_VERSION, AOT_PROFILER_MINOR_VERSION)
 * - sequence of records terminated by an AOTPROF_RECORD_NONE record
 *
 * Record format:
 * - byte: record type (AotProfRecordType)
 * - int: record id (unique across all record types in the file)
 * - followed by record specific data (see below)
 *
 * Encoding rules:
 * - int: 4 bytes little endian
 * - string: int length followed by UTF-8 data (no null terminator)
 */

typedef enum {
	/*
	 * Indicates EOF. No additional record data.
	 */
	AOTPROF_RECORD_NONE,
	/*
	 * Contains info about a loaded image.
	 * - string: assembly name
	 * - string: module mvid
	 */
	AOTPROF_RECORD_IMAGE,
	/*
	 * Contains info about a type referenced by other records.
	 * - byte: MONO_TYPE_CLASS
	 * - int: record id for the containing image (AOTPROF_RECORD_IMAGE)
	 * - int: record id for the generic instance or -1 if N/A (AOTPROF_RECORD_GINST)
	 * - string: type name
	 */
	AOTPROF_RECORD_TYPE,
	/*
	 * Contains info about a generic instantiation of a type or method.
	 * - int: type argument count
	 * - for 0 .. type argument count:
	 * -- int: record id for the type argument (AOTPROF_RECORD_TYPE)
	 */
	AOTPROF_RECORD_GINST,
	/*
	 * Contains info about a JITed method.
	 * - int: record id for the containing class (AOTPROF_RECORD_TYPE)
	 * - int: record id for the generic instance or -1 if N/A (AOTPROF_RECORD_GINST)
	 * - int: parameter count
	 * - string: method name
	 * - string: method signature
	 */
	AOTPROF_RECORD_METHOD
} AotProfRecordType;

#define AOT_PROFILER_MAGIC "AOTPROFILE"

#define AOT_PROFILER_MAJOR_VERSION 1
#define AOT_PROFILER_MINOR_VERSION 0

#endif /* __MONO_PROFILER_AOT_H__ */
