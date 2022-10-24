#ifndef _SRC_INC_DNMD_H_
#define _SRC_INC_DNMD_H_

#include "platform.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef void* mdhandle_t;

// Create a metadata handle that can be used to parse the supplied metadata.
//
// The supplied data is expected to be unmoved and available until all
// handles created with the data have been destroyed.
bool md_create_handle(void* data, size_t data_len, mdhandle_t* handle);

// Destroy the metadata handle and free all associated memory.
void md_destroy_handle(mdhandle_t handle);

// Validate the metadata associated with the handle.
bool md_validate(mdhandle_t handle);

// Write all tables to stdout
bool md_dump_tables(mdhandle_t handle);

// Table cursor definition
typedef struct _mdcursor_t
{
    intptr_t _reserved1;
    intptr_t _reserved2;
} mdcursor_t;

// Query how many rows a table contains.
bool md_table_row_count(mdhandle_t handle, CorTokenType type, uint32_t* count);

// Create a cursor to the first row in a table.
bool md_create_cursor(mdhandle_t handle, CorTokenType type, mdcursor_t* cursor);

// Move the cursor +/- number of rows.
bool md_cursor_move(mdcursor_t* c, int32_t delta);

// Move to the next row.
bool md_cursor_next(mdcursor_t* c);

// Given two cursors into the same table, compute the
// relative distance between the rows.
// if 'end' is before 'begin', the distance will be negative.
bool md_row_distance(mdcursor_t begin, mdcursor_t end, int32_t* distance);

// Convert between a token and location in metadata tables.
bool md_token_to_cursor(mdhandle_t handle, mdToken tk, mdcursor_t* c);
bool md_cursor_to_token(mdcursor_t c, mdToken* tk);

// Query row's column values
bool md_get_column_value_as_token(mdcursor_t c, uint32_t col_idx, mdToken* tk);
bool md_get_column_value_as_cursor(mdcursor_t c, uint32_t col_idx, mdcursor_t* cursor);
bool md_get_column_value_as_constant(mdcursor_t c, uint32_t col_idx, uint32_t* constant);
bool md_get_column_value_as_utf8(mdcursor_t c, uint32_t col_idx, char const** str);
bool md_get_column_value_as_wchar(mdcursor_t c, uint32_t col_idx, WCHAR const** str, uint32_t* str_chars, uint8_t* final_byte);
bool md_get_column_value_as_blob(mdcursor_t c, uint32_t col_idx, uint8_t const** blob, uint32_t* blob_len);
bool md_get_column_value_as_guid(mdcursor_t c, uint32_t col_idx, GUID* guid);

#ifdef __cplusplus
}
#endif

#endif // _SRC_INC_DNMD_H_
