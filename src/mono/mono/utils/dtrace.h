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


#define MONO_GC_REQUESTED(generation,requested_size,wait_to_finish)
#define MONO_GC_REQUESTED_ENABLED()	(0)


#define MONO_GC_CHECKPOINT_1(generation)
#define MONO_GC_CHECKPOINT_1_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_2(generation)
#define MONO_GC_CHECKPOINT_2_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_3(generation)
#define MONO_GC_CHECKPOINT_3_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_4(generation)
#define MONO_GC_CHECKPOINT_4_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_5(generation)
#define MONO_GC_CHECKPOINT_5_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_6(generation)
#define MONO_GC_CHECKPOINT_6_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_7(generation)
#define MONO_GC_CHECKPOINT_7_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_8(generation)
#define MONO_GC_CHECKPOINT_8_ENABLED()	(0)

#define MONO_GC_CHECKPOINT_9(generation)
#define MONO_GC_CHECKPOINT_9_ENABLED()	(0)


#define MONO_GC_CONCURRENT_START_BEGIN(generation)
#define MONO_GC_CONCURRENT_START_BEGIN_ENABLED()	(0)

#define MONO_GC_CONCURRENT_START_END(generation,num_major_objects_marked)
#define MONO_GC_CONCURRENT_START_END_ENABLED()	(0)

#define MONO_GC_CONCURRENT_UPDATE_FINISH_BEGIN(generation,num_major_objects_marked)
#define MONO_GC_CONCURRENT_UPDATE_FINISH_BEGIN_ENABLED()	(0)

#define MONO_GC_CONCURRENT_UPDATE_END(generation,num_major_objects_marked)
#define MONO_GC_CONCURRENT_UPDATE_END_ENABLED()	(0)

#define MONO_GC_CONCURRENT_FINISH_END(generation,num_major_objects_marked)
#define MONO_GC_CONCURRENT_FINISH_END_ENABLED()	(0)


#define MONO_GC_SWEEP_BEGIN(generation,full_sweep)
#define MONO_GC_SWEEP_BEGIN_ENABLED()	(0)

#define MONO_GC_SWEEP_END(generation,full_sweep)
#define MONO_GC_SWEEP_END_ENABLED()	(0)


#define MONO_GC_WORLD_STOP_BEGIN()
#define MONO_GC_WORLD_STOP_BEGIN_ENABLED()	(0)

#define MONO_GC_WORLD_STOP_END()
#define MONO_GC_WORLD_STOP_END_ENABLED()	(0)

#define MONO_GC_WORLD_RESTART_BEGIN(generation)
#define MONO_GC_WORLD_RESTART_BEGIN_ENABLED()	(0)

#define MONO_GC_WORLD_RESTART_END(generation)
#define MONO_GC_WORLD_RESTART_END_ENABLED()	(0)


#define MONO_GC_HEAP_ALLOC(addr,size)
#define MONO_GC_HEAP_ALLOC_ENABLED()	(0)

#define MONO_GC_HEAP_FREE(addr,size)
#define MONO_GC_HEAP_FREE_ENABLED()	(0)


#define MONO_GC_LOCKED()
#define MONO_GC_LOCKED_ENABLED() (0)

#define MONO_GC_UNLOCKED()
#define MONO_GC_UNLOCKED_ENABLED() (0)


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


#define MONO_GC_FINALIZE_ENQUEUE(addr,size,ns_name,class_name,generation,is_critical)
#define MONO_GC_FINALIZE_ENQUEUE_ENABLED()	(0)

#define MONO_GC_FINALIZE_INVOKE(addr,size,ns_name,class_name)
#define MONO_GC_FINALIZE_INVOKE_ENABLED()	(0)


#define MONO_GC_WEAK_UPDATE(ref_addr,old_addr,new_addr,size,ns_name,class_name,track)
#define MONO_GC_WEAK_UPDATE_ENABLED()	(0)


#define MONO_GC_GLOBAL_REMSET_ADD(ref_addr,obj_addr,size,ns_name,class_name)
#define MONO_GC_GLOBAL_REMSET_ADD_ENABLED()	(0)

#define MONO_GC_OBJ_CEMENTED(addr,size,ns_name,class_name)
#define MONO_GC_OBJ_CEMENTED_ENABLED()	(0)


#define MONO_GC_INTERNAL_ALLOC(addr,size,type)
#define MONO_GC_INTERNAL_ALLOC_ENABLED()	(0)

#define MONO_GC_INTERNAL_DEALLOC(addr,size,type)
#define MONO_GC_INTERNAL_DEALLOC_ENABLED()	(0)

#endif

#endif
