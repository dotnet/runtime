/**
 * \file
 */

#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/metadata/details/object-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/object-functions.h>
#undef MONO_API_FUNCTION

#define MONO_OBJECT_SETREF(obj,fieldname,value) do {	\
		mono_gc_wbarrier_set_field ((MonoObject*)(obj), &((obj)->fieldname), (MonoObject*)value);	\
		/*(obj)->fieldname = (value);*/	\
	} while (0)

/* This should be used if 's' can reside on the heap */
#define MONO_STRUCT_SETREF(s,field,value) do { \
        mono_gc_wbarrier_generic_store (&((s)->field), (MonoObject*)(value)); \
    } while (0)

#define mono_array_addr(array,type,index) ((type*)mono_array_addr_with_size ((array), sizeof (type), (index)))
#define mono_array_get(array,type,index) ( *(type*)mono_array_addr ((array), type, (index)) )
#define mono_array_set(array,type,index,value)	\
	do {	\
		type *__p = (type *) mono_array_addr ((array), type, (index));	\
		*__p = (value);	\
	} while (0)
#define mono_array_setref(array,index,value)	\
	do {	\
		void **__p = (void **) mono_array_addr ((array), void*, (index));	\
		mono_gc_wbarrier_set_arrayref ((array), __p, (MonoObject*)(value));	\
		/* *__p = (value);*/	\
	} while (0)
#define mono_array_memcpy_refs(dest,destidx,src,srcidx,count)	\
	do {	\
		void **__p = (void **) mono_array_addr ((dest), void*, (destidx));	\
		void **__s = mono_array_addr ((src), void*, (srcidx));	\
		mono_gc_wbarrier_arrayref_copy (__p, __s, (count));	\
	} while (0)

MONO_END_DECLS

#endif
