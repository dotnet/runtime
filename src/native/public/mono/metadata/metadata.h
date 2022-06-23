/**
 * \file
 */

#ifndef __MONO_METADATA_H__
#define __MONO_METADATA_H__

#include <mono/metadata/details/metadata-types.h>

MONO_BEGIN_DECLS

#define MONO_TYPE_ISSTRUCT(t) mono_type_is_struct (t)
#define MONO_TYPE_IS_VOID(t) mono_type_is_void (t)
#define MONO_TYPE_IS_POINTER(t) mono_type_is_pointer (t)
#define MONO_TYPE_IS_REFERENCE(t) mono_type_is_reference (t)

#define MONO_CLASS_IS_INTERFACE(c) ((mono_class_get_flags (c) & TYPE_ATTRIBUTE_INTERFACE) || mono_type_is_generic_parameter (mono_class_get_type (c)))

#define MONO_CLASS_IS_IMPORT(c) ((mono_class_get_flags (c) & TYPE_ATTRIBUTE_IMPORT))

/*
 * This macro is used to extract the size of the table encoded in
 * the size_bitfield of MonoTableInfo.
 */
#define mono_metadata_table_size(bitfield,table) ((((bitfield) >> ((table)*2)) & 0x3) + 1)
#define mono_metadata_table_count(bitfield) ((bitfield) >> 24)

#define MONO_OFFSET_IN_CLAUSE(clause,offset) \
	((clause)->try_offset <= (offset) && (offset) < ((clause)->try_offset + (clause)->try_len))
#define MONO_OFFSET_IN_HANDLER(clause,offset) \
	((clause)->handler_offset <= (offset) && (offset) < ((clause)->handler_offset + (clause)->handler_len))
#define MONO_OFFSET_IN_FILTER(clause,offset) \
	((clause)->flags == MONO_EXCEPTION_CLAUSE_FILTER && (clause)->data.filter_offset <= (offset) && (offset) < ((clause)->handler_offset))

/*
 * Makes a token based on a table and an index
 */
#define mono_metadata_make_token(table,idx) (((table) << 24)| (idx))

/*
 * Returns the table index that this token encodes.
 */
#define mono_metadata_token_table(token) ((token) >> 24)

 /*
 * Returns the index that a token refers to
 */
#define mono_metadata_token_index(token) ((token) & 0xffffff)


#define mono_metadata_token_code(token) ((token) & 0xff000000)

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/metadata-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __MONO_METADATA_H__ */
