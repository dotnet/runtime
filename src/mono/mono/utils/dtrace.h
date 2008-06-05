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

#define MONO_PROBE_VES_INIT_BEGIN()		MONO_VES_INIT_BEGIN ()
#define MONO_PROBE_VES_INIT_BEGIN_ENABLED()	MONO_VES_INIT_BEGIN_ENABLED ()

#define MONO_PROBE_VES_INIT_END()		MONO_VES_INIT_END ()
#define MONO_PROBE_VES_INIT_END_ENABLED()	MONO_VES_INIT_END_ENABLED ()


#define MONO_PROBE_METHOD_COMPILE_BEGIN(method) \
	MONO_METHOD_COMPILE_BEGIN ( \
		mono_type_get_full_name ((method)->klass), \
		(gchar*)(method)->name, \
		mono_signature_get_desc ((method)->signature, TRUE) \
	)
#define MONO_PROBE_METHOD_COMPILE_BEGIN_ENABLED()	MONO_METHOD_COMPILE_BEGIN_ENABLED ()

#define MONO_PROBE_METHOD_COMPILE_END(method, success) \
	MONO_METHOD_COMPILE_END ( \
		mono_type_get_full_name ((method)->klass), \
		(gchar*)(method)->name, \
		mono_signature_get_desc ((method)->signature, TRUE), \
		success \
	)
#define MONO_PROBE_METHOD_COMPILE_END_ENABLED()	MONO_METHOD_COMPILE_END_ENABLED ()


#define MONO_PROBE_GC_BEGIN(generation)	MONO_GC_BEGIN (generation)
#define MONO_PROBE_GC_BEGIN_ENABLED()	MONO_GC_BEGIN_ENABLED ()

#define MONO_PROBE_GC_END(generation)	MONO_GC_END (generation)
#define MONO_PROBE_GC_END_ENABLED()	MONO_GC_END_ENABLED ()


#else


#define MONO_PROBE_VES_INIT_BEGIN()
#define MONO_PROBE_VES_INIT_BEGIN_ENABLED() (0)

#define MONO_PROBE_VES_INIT_END()
#define MONO_PROBE_VES_INIT_END_ENABLED() (0)


#define MONO_PROBE_METHOD_COMPILE_BEGIN(method)
#define MONO_PROBE_METHOD_COMPILE_BEGIN_ENABLED() (0)

#define MONO_PROBE_METHOD_COMPILE_END(method, success)
#define MONO_PROBE_METHOD_COMPILE_END_ENABLED() (0)


#define MONO_PROBE_GC_BEGIN(generation)
#define MONO_PROBE_GC_BEGIN_ENABLED() (0)

#define MONO_PROBE_GC_END(generation)
#define MONO_PROBE_GC_END_ENABLED() (0)


#endif

#endif

