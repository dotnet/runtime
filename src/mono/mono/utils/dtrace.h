/* 
 * dtrace.h: DTrace probes
 * 
 * Authors:
 *   Andreas Faerber <andreas.faerber@web.de>
 * 
 */

#ifndef __UTILS_DTRACE_H__
#define __UTILS_DTRACE_H__

#ifdef ENABLE_DTRACE

#include <mono/utils/mono-dtrace.h>

#define MONO_PROBE_METHOD_COMPILE_BEGIN(method) \
	MONO_METHOD_COMPILE_BEGIN ( \
		mono_type_get_full_name ((method)->klass), \
		(gchar*)(method)->name, \
		mono_signature_get_desc ((method)->signature, TRUE) \
	)

#define MONO_PROBE_METHOD_COMPILE_END(method, success) \
	MONO_METHOD_COMPILE_END ( \
		mono_type_get_full_name ((method)->klass), \
		(gchar*)(method)->name, \
		mono_signature_get_desc ((method)->signature, TRUE), \
		success \
	)

#else

#define MONO_VES_INIT_BEGIN()
#define MONO_VES_INIT_BEGIN_ENABLED() (0)

#define MONO_VES_INIT_END()
#define MONO_VES_INIT_END_ENABLED() (0)


#define MONO_PROBE_METHOD_COMPILE_BEGIN(method)
#define MONO_METHOD_COMPILE_BEGIN_ENABLED() (0)

#define MONO_PROBE_METHOD_COMPILE_END(method, success)
#define MONO_METHOD_COMPILE_END_ENABLED() (0)


#define MONO_GC_BEGIN(generation)
#define MONO_GC_BEGIN_ENABLED() (0)

#define MONO_GC_END(generation)
#define MONO_GC_END_ENABLED() (0)


#define MONO_GC_LOCKED()
#define MONO_GC_LOCKED_ENABLED() (0)

#define MONO_GC_UNLOCKED()
#define MONO_GC_UNLOCKED_ENABLED() (0)


#define MONO_GC_HEAP_ALLOC(addr,size)
#define MONO_GC_HEAP_FREE(addr,size)


#define MONO_GC_NURSERY_TLAB_ALLOC(addr,len)
#define MONO_GC_NURSERY_TLAB_ALLOC_ENABLED()	(0)

#define MONO_GC_NURSERY_OBJ_ALLOC(addr,size,ns_name,class_name)
#define MONO_GC_NURSERY_OBJ_ALLOC_ENABLED()	(0)


#define MONO_GC_MAJOR_OBJ_ALLOC_LARGE(addr,size,ns_name,class_name)
#define MONO_GC_MAJOR_OBJ_ALLOC_LARGE_ENABLED()	(0)

#define MONO_GC_MAJOR_OBJ_ALLOC_PINNED(addr,size,ns_name,class_name)
#define MONO_GC_MAJOR_OBJ_ALLOC_PINNED_ENABLED()	(0)

#define MONO_GC_MAJOR_OBJ_ALLOC_DEGRADED(addr,size,ns_name,class_name)
#define MONO_GC_MAJOR_OBJ_ALLOC_DEGRADED_ENABLED()	(0)

#define MONO_GC_MAJOR_OBJ_ALLOC_MATURE(addr,size,ns_name,class_name)
#define MONO_GC_MAJOR_OBJ_ALLOC_MATURE_ENABLED()	(0)


#define MONO_GC_OBJ_MOVED(dest,src,dest_gen,src_gen,size,ns_name,class_name)
#define MONO_GC_OBJ_MOVED_ENABLED()	(0)


#define MONO_GC_NURSERY_SWEPT(addr,len)
#define MONO_GC_NURSERY_SWEPT_ENABLED()	(0)

#define MONO_GC_MAJOR_SWEPT(addr,len)
#define MONO_GC_MAJOR_SWEPT_ENABLED()	(0)


#define MONO_GC_OBJ_PINNED(addr,size,ns_name,class_name,generation)
#define MONO_GC_OBJ_PINNED_ENABLED()	(0)

#endif

#endif
