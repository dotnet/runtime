/**
* \file
*/

#ifndef _MONO_METADATA_LOADER_INTERNALS_H_
#define _MONO_METADATA_LOADER_INTERNALS_H_

#ifdef __cplusplus

template <typename T>
inline void
mono_add_internal_call (const char *name, T method)
{
	return mono_add_internal_call (name, (const void*)method);
}

#endif // __cplusplus

#endif
