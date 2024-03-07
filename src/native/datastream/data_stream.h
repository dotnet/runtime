// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DN_DATA_STREAM_H
#define DN_DATA_STREAM_H

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>
#include <assert.h>

#ifdef BUILD_SHARED_LIBRARY
#ifdef _MSC_VER
#define DATA_STREAM_EXPORT __declspec(dllexport)
#else
#define DATA_STREAM_EXPORT __attribute__ ((visibility ("default")))
#endif // _MSC_VER
#else
#define DATA_STREAM_EXPORT
#endif // BUILD_SHARED_LIBRARY

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

// Forward declarations
typedef struct data_stream__ data_stream_t;

typedef struct data_stream_context__
{
    uint32_t magic; // Value used to confirm the pointer and check endianness of the target machine.
    uint16_t size; // The current size of this data structure.
    uint16_t version; // The version of the data stream binary format.

    uint32_t reserved; // Reserved.
    uint32_t streams_count; // Count of statically allocated streams.
    data_stream_t* streams; // Statically allocated streams.
} data_stream_context_t;

// Validation results for a data stream.
typedef enum
{
    dsv_invalid,
    dsv_little_endian,
    dsv_big_endian,
} ds_validate_t;

// Validate the magic number is valid
ds_validate_t dnds_validate(uint32_t magic);

// Determine if the data stream is big or little endian.
DATA_STREAM_EXPORT bool dnds_is_big_endian(data_stream_context_t*);

//
// Target APIs
//

// Initialize the data stream.
// Statically allocates streams with a default block size.
DATA_STREAM_EXPORT bool dnds_init(
    data_stream_context_t*,
    uint32_t stream_count,
    size_t const* stream_byte_lengths);

// Deallocate all memory associated with the data stream.
DATA_STREAM_EXPORT void dnds_destroy(data_stream_context_t*);

// Define a maximum value for the type datatype.
#define MAX_TYPE_SIZE UINT16_MAX

typedef struct type_details__
{
    uint16_t type;
    uint16_t version;
    uint16_t reserved; // Must be zero
    uint16_t name_len; // Includes null
    char const* name;
} type_details_t;
static_assert((offsetof(type_details_t, name) % sizeof(void*)) == 0, "Pointer field should be pointer aligned");

typedef struct field_offset__
{
    uint16_t offset;
    uint16_t type;
} field_offset_t;
static_assert(sizeof(field_offset_t) == sizeof(uint32_t), "Field offset structures should be 4 bytes");

// Define a type in the data stream
DATA_STREAM_EXPORT bool dnds_define_type(
    data_stream_context_t*,
    type_details_t const* details,
    size_t total_size,
    size_t offsets_length,
    field_offset_t const* offsets);

// Get an available stream to write to
DATA_STREAM_EXPORT data_stream_t* dnds_get_stream(
    data_stream_context_t*,
    size_t id);

// Record an instance in the stream
DATA_STREAM_EXPORT bool dnds_record_instance(
    data_stream_t*,
    uint16_t type,
    void* inst);

// Record a data blob in the stream
DATA_STREAM_EXPORT bool dnds_record_blob(
    data_stream_t*,
    uint16_t type,
    uint16_t size,
    void* inst);

//
// Reader APIs
//

typedef struct memory_reader__
{
    bool(*read_ptr)(struct memory_reader__*,intptr_t,size_t*,void**);
    void(*free_ptr)(struct memory_reader__*,size_t,void*);
} memory_reader_t;

// Return false to stop enumeration
typedef bool(*on_next_type)(
    type_details_t const* details,
    size_t total_size,
    size_t offsets_length,
    field_offset_t const* offsets,
    void* user_defined);

// Enumerate the types in the data stream
DATA_STREAM_EXPORT bool dnds_enum_type(
    data_stream_context_t*,
    on_next_type on_next,
    void* user_defined,
    memory_reader_t* reader);

// Return false to stop enumeration
typedef bool(*on_next_blob)(
    uint16_t type,
    uint16_t data_size_bytes,
    void* data,
    void* user_defined);

// Enumerate all blobs in the data stream
DATA_STREAM_EXPORT bool dnds_enum_blobs(
    data_stream_context_t*,
    on_next_blob on_next,
    void* user_defined,
    memory_reader_t* reader);

// Return false to stop enumeration
typedef bool(*on_next_instance)(
    uint16_t type,
    intptr_t instance,
    void* user_defined);

// Enumerate all instances in the data stream
DATA_STREAM_EXPORT bool dnds_enum_instances(
    data_stream_context_t*,
    on_next_instance on_next,
    void* user_defined,
    memory_reader_t* reader);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // DN_DATA_STREAM_H
