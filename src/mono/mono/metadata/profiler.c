
#include "mono/metadata/profiler-private.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/mono-debug.h"
#include "mono/metadata/class-internals.h"
#include "mono/io-layer/io-layer.h"
#include <string.h>
#include <gmodule.h>

static MonoProfiler * current_profiler = NULL;

static MonoProfileAppDomainFunc   domain_start_load;
static MonoProfileAppDomainResult domain_end_load;
static MonoProfileAppDomainFunc   domain_start_unload;
static MonoProfileAppDomainFunc   domain_end_unload;

static MonoProfileAssemblyFunc   assembly_start_load;
static MonoProfileAssemblyResult assembly_end_load;
static MonoProfileAssemblyFunc   assembly_start_unload;
static MonoProfileAssemblyFunc   assembly_end_unload;

static MonoProfileModuleFunc   module_start_load;
static MonoProfileModuleResult module_end_load;
static MonoProfileModuleFunc   module_start_unload;
static MonoProfileModuleFunc   module_end_unload;

static MonoProfileClassFunc   class_start_load;
static MonoProfileClassResult class_end_load;
static MonoProfileClassFunc   class_start_unload;
static MonoProfileClassFunc   class_end_unload;

static MonoProfileMethodFunc   jit_start;
static MonoProfileMethodResult jit_end;
static MonoProfileMethodResult man_unman_transition;
static MonoProfileAllocFunc    allocation_cb;
static MonoProfileMethodFunc   method_enter;
static MonoProfileMethodFunc   method_leave;

static MonoProfileThreadFunc   thread_start;
static MonoProfileThreadFunc   thread_end;

static MonoProfileCoverageFilterFunc coverage_filter_cb;

static MonoProfileFunc shutdown_callback;

static CRITICAL_SECTION profiler_coverage_mutex;

/* this is directly accessible to other mono libs. */
MonoProfileFlags mono_profiler_events;

void
mono_profiler_install (MonoProfiler *prof, MonoProfileFunc callback)
{
	if (current_profiler)
		g_error ("profiler already setup");
	current_profiler = prof;
	shutdown_callback = callback;
	InitializeCriticalSection (&profiler_coverage_mutex);
}

void
mono_profiler_set_events (MonoProfileFlags events)
{
	mono_profiler_events = events;
}

MonoProfileFlags
mono_profiler_get_events (void)
{
	return mono_profiler_events;
}

void
mono_profiler_install_enter_leave (MonoProfileMethodFunc enter, MonoProfileMethodFunc fleave)
{
	method_enter = enter;
	method_leave = fleave;
}

void 
mono_profiler_install_jit_compile (MonoProfileMethodFunc start, MonoProfileMethodResult end)
{
	jit_start = start;
	jit_end = end;
}

void 
mono_profiler_install_thread (MonoProfileThreadFunc start, MonoProfileThreadFunc end)
{
	thread_start = start;
	thread_end = end;
}

void 
mono_profiler_install_transition (MonoProfileMethodResult callback)
{
	man_unman_transition = callback;
}

void 
mono_profiler_install_allocation (MonoProfileAllocFunc callback)
{
	allocation_cb = callback;
}

void 
mono_profiler_install_coverage_filter (MonoProfileCoverageFilterFunc callback)
{
	coverage_filter_cb = callback;
}

void 
mono_profiler_install_appdomain   (MonoProfileAppDomainFunc start_load, MonoProfileAppDomainResult end_load,
                                   MonoProfileAppDomainFunc start_unload, MonoProfileAppDomainFunc end_unload)

{
	domain_start_load = start_load;
	domain_end_load = end_load;
	domain_start_unload = start_unload;
	domain_end_unload = end_unload;
}

void 
mono_profiler_install_assembly    (MonoProfileAssemblyFunc start_load, MonoProfileAssemblyResult end_load,
                                   MonoProfileAssemblyFunc start_unload, MonoProfileAssemblyFunc end_unload)
{
	assembly_start_load = start_load;
	assembly_end_load = end_load;
	assembly_start_unload = start_unload;
	assembly_end_unload = end_unload;
}

void 
mono_profiler_install_module      (MonoProfileModuleFunc start_load, MonoProfileModuleResult end_load,
                                   MonoProfileModuleFunc start_unload, MonoProfileModuleFunc end_unload)
{
	module_start_load = start_load;
	module_end_load = end_load;
	module_start_unload = start_unload;
	module_end_unload = end_unload;
}

void
mono_profiler_install_class       (MonoProfileClassFunc start_load, MonoProfileClassResult end_load,
                                   MonoProfileClassFunc start_unload, MonoProfileClassFunc end_unload)
{
	class_start_load = start_load;
	class_end_load = end_load;
	class_start_unload = start_unload;
	class_end_unload = end_unload;
}

void
mono_profiler_method_enter (MonoMethod *method)
{
	if ((mono_profiler_events & MONO_PROFILE_ENTER_LEAVE) && method_enter)
		method_enter (current_profiler, method);
}

void
mono_profiler_method_leave (MonoMethod *method)
{
	if ((mono_profiler_events & MONO_PROFILE_ENTER_LEAVE) && method_leave)
		method_leave (current_profiler, method);
}

void 
mono_profiler_method_jit (MonoMethod *method)
{
	if ((mono_profiler_events & MONO_PROFILE_JIT_COMPILATION) && jit_start)
		jit_start (current_profiler, method);
}

void 
mono_profiler_method_end_jit (MonoMethod *method, int result)
{
	if ((mono_profiler_events & MONO_PROFILE_JIT_COMPILATION) && jit_end)
		jit_end (current_profiler, method, result);
}

void 
mono_profiler_code_transition (MonoMethod *method, int result)
{
	if ((mono_profiler_events & MONO_PROFILE_TRANSITIONS) && man_unman_transition)
		man_unman_transition (current_profiler, method, result);
}

void 
mono_profiler_allocation (MonoObject *obj, MonoClass *klass)
{
	if ((mono_profiler_events & MONO_PROFILE_ALLOCATIONS) && allocation_cb)
		allocation_cb (current_profiler, obj, klass);
}

void
mono_profiler_thread_start (guint32 tid)
{
	if ((mono_profiler_events & MONO_PROFILE_THREADS) && thread_start)
		thread_start (current_profiler, tid);
}

void 
mono_profiler_thread_end (guint32 tid)
{
	if ((mono_profiler_events & MONO_PROFILE_THREADS) && thread_end)
		thread_end (current_profiler, tid);
}

void 
mono_profiler_assembly_event  (MonoAssembly *assembly, int code)
{
	if (!(mono_profiler_events & MONO_PROFILE_ASSEMBLY_EVENTS))
		return;
	
	switch (code) {
	case MONO_PROFILE_START_LOAD:
		if (assembly_start_load)
			assembly_start_load (current_profiler, assembly);
		break;
	case MONO_PROFILE_START_UNLOAD:
		if (assembly_start_unload)
			assembly_start_unload (current_profiler, assembly);
		break;
	case MONO_PROFILE_END_UNLOAD:
		if (assembly_end_unload)
			assembly_end_unload (current_profiler, assembly);
		break;
	default:
		g_assert_not_reached ();
	}
}

void 
mono_profiler_assembly_loaded (MonoAssembly *assembly, int result)
{
	if ((mono_profiler_events & MONO_PROFILE_ASSEMBLY_EVENTS) && assembly_end_load)
		assembly_end_load (current_profiler, assembly, result);
}

void 
mono_profiler_module_event  (MonoImage *module, int code)
{
	if (!(mono_profiler_events & MONO_PROFILE_MODULE_EVENTS))
		return;
	
	switch (code) {
	case MONO_PROFILE_START_LOAD:
		if (module_start_load)
			module_start_load (current_profiler, module);
		break;
	case MONO_PROFILE_START_UNLOAD:
		if (module_start_unload)
			module_start_unload (current_profiler, module);
		break;
	case MONO_PROFILE_END_UNLOAD:
		if (module_end_unload)
			module_end_unload (current_profiler, module);
		break;
	default:
		g_assert_not_reached ();
	}
}

void 
mono_profiler_module_loaded (MonoImage *module, int result)
{
	if ((mono_profiler_events & MONO_PROFILE_MODULE_EVENTS) && module_end_load)
		module_end_load (current_profiler, module, result);
}

void 
mono_profiler_class_event  (MonoClass *klass, int code)
{
	if (!(mono_profiler_events & MONO_PROFILE_CLASS_EVENTS))
		return;
	
	switch (code) {
	case MONO_PROFILE_START_LOAD:
		if (class_start_load)
			class_start_load (current_profiler, klass);
		break;
	case MONO_PROFILE_START_UNLOAD:
		if (class_start_unload)
			class_start_unload (current_profiler, klass);
		break;
	case MONO_PROFILE_END_UNLOAD:
		if (class_end_unload)
			class_end_unload (current_profiler, klass);
		break;
	default:
		g_assert_not_reached ();
	}
}

void 
mono_profiler_class_loaded (MonoClass *klass, int result)
{
	if ((mono_profiler_events & MONO_PROFILE_CLASS_EVENTS) && class_end_load)
		class_end_load (current_profiler, klass, result);
}

void 
mono_profiler_appdomain_event  (MonoDomain *domain, int code)
{
	if (!(mono_profiler_events & MONO_PROFILE_APPDOMAIN_EVENTS))
		return;
	
	switch (code) {
	case MONO_PROFILE_START_LOAD:
		if (domain_start_load)
			domain_start_load (current_profiler, domain);
		break;
	case MONO_PROFILE_START_UNLOAD:
		if (domain_start_unload)
			domain_start_unload (current_profiler, domain);
		break;
	case MONO_PROFILE_END_UNLOAD:
		if (domain_end_unload)
			domain_end_unload (current_profiler, domain);
		break;
	default:
		g_assert_not_reached ();
	}
}

void 
mono_profiler_appdomain_loaded (MonoDomain *domain, int result)
{
	if ((mono_profiler_events & MONO_PROFILE_APPDOMAIN_EVENTS) && domain_end_load)
		domain_end_load (current_profiler, domain, result);
}

void 
mono_profiler_shutdown (void)
{
	if (current_profiler && shutdown_callback)
		shutdown_callback (current_profiler);
}

static GHashTable *coverage_hash = NULL;

MonoProfileCoverageInfo* 
mono_profiler_coverage_alloc (MonoMethod *method, int entries)
{
	MonoProfileCoverageInfo *res;

	if (coverage_filter_cb)
		if (! (*coverage_filter_cb) (current_profiler, method))
			return NULL;

	EnterCriticalSection (&profiler_coverage_mutex);
	if (!coverage_hash)
		coverage_hash = g_hash_table_new (NULL, NULL);

	res = g_malloc0 (sizeof (MonoProfileCoverageInfo) + sizeof (void*) * 2 * entries);

	res->entries = entries;

	g_hash_table_insert (coverage_hash, method, res);
	LeaveCriticalSection (&profiler_coverage_mutex);

	return res;
}

/* safe only when the method antive code has been unloaded */
void
mono_profiler_coverage_free (MonoMethod *method)
{
	MonoProfileCoverageInfo* info;

	EnterCriticalSection (&profiler_coverage_mutex);
	if (!coverage_hash) {
		LeaveCriticalSection (&profiler_coverage_mutex);
		return;
	}

	info = g_hash_table_lookup (coverage_hash, method);
	if (info) {
		g_free (info);
		g_hash_table_remove (coverage_hash, method);
	}
	LeaveCriticalSection (&profiler_coverage_mutex);
}

void 
mono_profiler_coverage_get (MonoProfiler *prof, MonoMethod *method, MonoProfileCoverageFunc func)
{
	MonoProfileCoverageInfo* info;
	int i, offset;
	guint32 line, col;
	unsigned char *start, *end, *cil_code;
	MonoMethodHeader *header;
	MonoProfileCoverageEntry entry;

	EnterCriticalSection (&profiler_coverage_mutex);
	info = g_hash_table_lookup (coverage_hash, method);
	LeaveCriticalSection (&profiler_coverage_mutex);

	if (!info)
		return;

	header = mono_method_get_header (method);
	start = (unsigned char*)header->code;
	end = start + header->code_size;
	for (i = 0; i < info->entries; ++i) {
		cil_code = info->data [i].cil_code;
		if (cil_code && cil_code >= start && cil_code < end) {
			offset = cil_code - start;
			entry.iloffset = offset;
			entry.method = method;
			entry.counter = info->data [i].count;
			/* the debug interface doesn't support column info, sigh */
			col = line = 1;
			entry.filename = mono_debug_source_location_from_il_offset (method, offset, &line);
			entry.line = line;
			entry.col = col;
			func (prof, &entry);
		}
	}
}

/*
 * Small profiler extracted from mint: we should move it in a loadable module
 * and improve it to do graphs and more accurate timestamping with rdtsc.
 */

#define USE_X86TSC 0
#define USE_WIN32COUNTER 0
#if USE_X86TSC

typedef struct {
	unsigned int lows, highs, lowe, highe;
} MonoRdtscTimer;

#define rdtsc(low,high) \
        __asm__ __volatile__("rdtsc" : "=a" (low), "=d" (high))

static int freq;

static double
rdtsc_elapsed (MonoRdtscTimer *t)
{
	unsigned long long diff;
	unsigned int highe = t->highe;
	if (t->lowe < t->lows)
		highe--;
	diff = (((unsigned long long) highe - t->highs) << 32) + (t->lowe - t->lows);
	return ((double)diff / freq) / 1000000; /* have to return the result in seconds */
}

static int 
have_rdtsc (void) {
	char buf[256];
	int have_freq = 0;
	int have_flag = 0;
	float val;
	FILE *cpuinfo;

	if (!(cpuinfo = fopen ("/proc/cpuinfo", "r")))
		return 0;
	while (fgets (buf, sizeof(buf), cpuinfo)) {
		if (sscanf (buf, "cpu MHz : %f", &val) == 1) {
			/*printf ("got mh: %f\n", val);*/
			have_freq = val;
		}
		if (strncmp (buf, "flags", 5) == 0) {
			if (strstr (buf, "tsc")) {
				have_flag = 1;
				/*printf ("have tsc\n");*/
			}
		}
	}
	fclose (cpuinfo);
	return have_flag? have_freq: 0;
}

#define MONO_TIMER_STARTUP 	\
	if (!(freq = have_rdtsc ())) g_error ("Compiled with TSC support, but none found");
#define MONO_TIMER_TYPE  MonoRdtscTimer
#define MONO_TIMER_INIT(t)
#define MONO_TIMER_DESTROY(t)
#define MONO_TIMER_START(t) rdtsc ((t).lows, (t).highs);
#define MONO_TIMER_STOP(t) rdtsc ((t).lowe, (t).highe);
#define MONO_TIMER_ELAPSED(t) rdtsc_elapsed (&(t))

#elif USE_WIN32COUNTER
#include <windows.h>

typedef struct {
	LARGE_INTEGER start, stop;
} MonoWin32Timer;

static int freq;

static double
win32_elapsed (MonoWin32Timer *t)
{
	LONGLONG diff = t->stop.QuadPart - t->start.QuadPart;
	return ((double)diff / freq) / 1000000; /* have to return the result in seconds */
}

static int 
have_win32counter (void) {
	LARGE_INTEGER f;

	if (!QueryPerformanceFrequency (&f))
		return 0;
	return f.LowPart;
}

#define MONO_TIMER_STARTUP 	\
	if (!(freq = have_win32counter ())) g_error ("Compiled with Win32 counter support, but none found");
#define MONO_TIMER_TYPE  MonoWin32Timer
#define MONO_TIMER_INIT(t)
#define MONO_TIMER_DESTROY(t)
#define MONO_TIMER_START(t) QueryPerformanceCounter (&(t).start)
#define MONO_TIMER_STOP(t) QueryPerformanceCounter (&(t).stop)
#define MONO_TIMER_ELAPSED(t) win32_elapsed (&(t))

#else

typedef struct {
	GTimeVal start, stop;
} MonoGLibTimer;

static double
timeval_elapsed (MonoGLibTimer *t)
{
	if (t->start.tv_usec > t->stop.tv_usec) {
		t->stop.tv_usec += G_USEC_PER_SEC;
		t->stop.tv_sec--;
	}
	return (t->stop.tv_sec - t->start.tv_sec) 
		+ ((double)(t->stop.tv_usec - t->start.tv_usec))/ G_USEC_PER_SEC;
}

#define MONO_TIMER_STARTUP
#define MONO_TIMER_TYPE MonoGLibTimer
#define MONO_TIMER_INIT(t)
#define MONO_TIMER_DESTROY(t)
#define MONO_TIMER_START(t) g_get_current_time (&(t).start)
#define MONO_TIMER_STOP(t) g_get_current_time (&(t).stop)
#define MONO_TIMER_ELAPSED(t) timeval_elapsed (&(t))
#endif

typedef struct _AllocInfo AllocInfo;
typedef struct _CallerInfo CallerInfo;
typedef struct _LastCallerInfo LastCallerInfo;

struct _MonoProfiler {
	GHashTable *methods;
	MonoMemPool *mempool;
	/* info about JIT time */
	MONO_TIMER_TYPE jit_timer;
	double      jit_time;
	double      max_jit_time;
	MonoMethod *max_jit_method;
	int         methods_jitted;
	
	GSList     *per_thread;
	
	/* chain of callers for the current thread */
	LastCallerInfo *callers;
	/* LastCallerInfo nodes for faster allocation */
	LastCallerInfo *cstorage;
};

typedef struct {
	MonoMethod *method;
	guint64 count;
	double total;
	AllocInfo *alloc_info;
	CallerInfo *caller_info;
} MethodProfile;

typedef struct _MethodCallProfile MethodCallProfile;

struct _MethodCallProfile {
	MethodCallProfile *next;
	MONO_TIMER_TYPE timer;
	MonoMethod *method;
};

struct _AllocInfo {
	AllocInfo *next;
	MonoClass *klass;
	guint count;
	guint mem;
};

struct _CallerInfo {
	CallerInfo *next;
	MonoMethod *caller;
	guint count;
};

struct _LastCallerInfo {
	LastCallerInfo *next;
	MonoMethod *method;
	MONO_TIMER_TYPE timer;
};

static MonoProfiler*
create_profiler (void)
{
	MonoProfiler *prof = g_new0 (MonoProfiler, 1);

	prof->methods = g_hash_table_new (NULL, NULL);
	MONO_TIMER_INIT (prof->jit_timer);
	prof->mempool = mono_mempool_new ();
	return prof;
}
#if 1

#ifdef HAVE_KW_THREAD
	static __thread MonoProfiler * tls_profiler;
#	define GET_PROFILER() tls_profiler
#	define SET_PROFILER(x) tls_profiler = (x)
#	define ALLOC_PROFILER() /* nop */
#else
	static guint32 profiler_thread_id = -1;
#	define GET_PROFILER() ((MonoProfiler *)TlsGetValue (profiler_thread_id))
#	define SET_PROFILER(x) TlsSetValue (profiler_thread_id, x);
#	define ALLOC_PROFILER() profiler_thread_id = TlsAlloc ()
#endif

#define GET_THREAD_PROF(prof) do {                                                           \
		MonoProfiler *_tprofiler = GET_PROFILER ();                                  \
		if (!_tprofiler) {	                                                     \
			_tprofiler = create_profiler ();                                     \
			prof->per_thread = g_slist_prepend (prof->per_thread, _tprofiler);   \
			SET_PROFILER (_tprofiler);                                           \
		}	                                                                     \
		prof = _tprofiler;	                                                     \
	} while (0)
#else
/* thread unsafe but faster variant */
#define GET_THREAD_PROF(prof)
#endif

static gint
compare_profile (MethodProfile *profa, MethodProfile *profb)
{
	return (gint)((profb->total - profa->total)*1000);
}

static void
build_profile (MonoMethod *m, MethodProfile *prof, GList **funcs)
{
	prof->method = m;
	*funcs = g_list_insert_sorted (*funcs, prof, (GCompareFunc)compare_profile);
}

static char*
method_get_name (MonoMethod* method)
{
	char *sig, *res;
	
	sig = mono_signature_get_desc (method->signature, FALSE);
	res = g_strdup_printf ("%s.%s::%s(%s)", method->klass->name_space, method->klass->name,
		method->name, sig);
	g_free (sig);
	return res;
}

static void output_callers (MethodProfile *p);

static void
output_profile (GList *funcs)
{
	GList *tmp;
	MethodProfile *p;
	char *m;
	guint64 total_calls = 0;

	if (funcs)
		g_print ("Time(ms) Count   P/call(ms) Method name\n");
	for (tmp = funcs; tmp; tmp = tmp->next) {
		p = tmp->data;
		total_calls += p->count;
		if (!(gint)(p->total*1000))
			continue;
		m = method_get_name (p->method);
		printf ("########################\n");
		printf ("% 8.3f ", (double) (p->total * 1000));
		printf ("%7llu ", p->count);
		printf ("% 8.3f ", (double) (p->total * 1000)/(double)p->count);
		printf ("  %s\n", m);

		g_free (m);
		/* callers */
		output_callers (p);
	}
	printf ("Total number of calls: %lld\n", total_calls);
}

typedef struct {
	MethodProfile *mp;
	guint count;
} NewobjProfile;

static gint
compare_newobj_profile (NewobjProfile *profa, NewobjProfile *profb)
{
	return (gint)profb->count - (gint)profa->count;
}

static void
build_newobj_profile (MonoClass *class, MethodProfile *mprof, GList **funcs)
{
	NewobjProfile *prof = g_new (NewobjProfile, 1);
	AllocInfo *tmp;
	guint count = 0;
	
	prof->mp = mprof;
	/* we use the total amount of memory to sort */
	for (tmp = mprof->alloc_info; tmp; tmp = tmp->next)
		count += tmp->mem;
	prof->count = count;
	*funcs = g_list_insert_sorted (*funcs, prof, (GCompareFunc)compare_newobj_profile);
}

static int
compare_caller (CallerInfo *a, CallerInfo *b)
{
	return b->count - a->count;
}

static int
compare_alloc (AllocInfo *a, AllocInfo *b)
{
	return b->mem - a->mem;
}

static GSList*
sort_alloc_list (AllocInfo *ai)
{
	GSList *l = NULL;
	AllocInfo *tmp;
	for (tmp = ai; tmp; tmp = tmp->next) {
		l = g_slist_insert_sorted (l, tmp, (GCompareFunc)compare_alloc);
	}
	return l;
}

static GSList*
sort_caller_list (CallerInfo *ai)
{
	GSList *l = NULL;
	CallerInfo *tmp;
	for (tmp = ai; tmp; tmp = tmp->next) {
		l = g_slist_insert_sorted (l, tmp, (GCompareFunc)compare_caller);
	}
	return l;
}

static void
output_callers (MethodProfile *p) {
	guint total_callers, percent;
	GSList *sorted, *tmps;
	CallerInfo *cinfo;
	char *m;
	
	g_print ("  Callers (with count) that contribute at least for 1%%:\n");
	total_callers = 0;
	for (cinfo = p->caller_info; cinfo; cinfo = cinfo->next) {
		total_callers += cinfo->count;
	}
	sorted = sort_caller_list (p->caller_info);
	for (tmps = sorted; tmps; tmps = tmps->next) {
		cinfo = tmps->data;
		percent = (cinfo->count * 100)/total_callers;
		if (percent < 1)
			continue;
		m = method_get_name (cinfo->caller);
		g_print ("    %8d % 3d %% %s\n", cinfo->count, percent, m);
		g_free (m);
	}
}

static void
output_newobj_profile (GList *proflist)
{
	GList *tmp;
	NewobjProfile *p;
	MethodProfile *mp;
	AllocInfo *ainfo;
	MonoClass *klass;
	const char* isarray;
	char buf [256];
	char *m;
	guint total = 0;
	GSList *sorted, *tmps;

	g_print ("\nAllocation profiler\n");

	if (proflist)
		g_print ("%-9s %s\n", "Total mem", "Method");
	for (tmp = proflist; tmp; tmp = tmp->next) {
		p = tmp->data;
		total += p->count;
		if (p->count < 50000)
			continue;
		mp = p->mp;
		m = method_get_name (mp->method);
		g_print ("########################\n%8d KB %s\n", p->count / 1024, m);
		g_free (m);
		sorted = sort_alloc_list (mp->alloc_info);
		for (tmps = sorted; tmps; tmps = tmps->next) {
			ainfo = tmps->data;
			if (ainfo->mem < 50000)
				continue;
			klass = ainfo->klass;
			if (klass->rank) {
				isarray = "[]";
				klass = klass->element_class;
			} else {
				isarray = "";
			}
			g_snprintf (buf, sizeof (buf), "%s.%s%s",
				klass->name_space, klass->name, isarray);
			g_print ("    %8d KB %8d %-48s\n", ainfo->mem / 1024, ainfo->count, buf);
		}
		/* callers */
		output_callers (mp);
	}
	g_print ("Total memory allocated: %d KB\n", total / 1024);
}

static void
merge_methods (MonoMethod *method, MethodProfile *profile, MonoProfiler *prof)
{
	MethodProfile *mprof;
	AllocInfo *talloc_info, *alloc_info;
	CallerInfo *tcaller_info, *caller_info;

	mprof = g_hash_table_lookup (prof->methods, method);
	if (!mprof) {
		/* the master thread didn't see this method, just transfer the info as is */
		g_hash_table_insert (prof->methods, method, profile);
		return;
	}
	/* merge the info from profile into mprof */
	mprof->count += profile->count;
	mprof->total += profile->total;
	/* merge alloc info */
	for (talloc_info = profile->alloc_info; talloc_info; talloc_info = talloc_info->next) {
		for (alloc_info = mprof->alloc_info; alloc_info; alloc_info = alloc_info->next) {
			if (alloc_info->klass == talloc_info->klass) {
				/* mprof already has a record for the klass, merge */
				alloc_info->count += talloc_info->count;
				alloc_info->mem += talloc_info->mem;
				break;
			}
		}
		if (!alloc_info) {
			/* mprof didn't have the info, just copy it over */
			alloc_info = mono_mempool_alloc0 (prof->mempool, sizeof (AllocInfo));
			*alloc_info = *talloc_info;
			alloc_info->next = mprof->alloc_info;
			mprof->alloc_info = alloc_info->next;
		}
	}
	/* merge callers info */
	for (tcaller_info = profile->caller_info; tcaller_info; tcaller_info = tcaller_info->next) {
		for (caller_info = mprof->caller_info; caller_info; caller_info = caller_info->next) {
			if (caller_info->caller == tcaller_info->caller) {
				/* mprof already has a record for the caller method, merge */
				caller_info->count += tcaller_info->count;
				break;
			}
		}
		if (!caller_info) {
			/* mprof didn't have the info, just copy it over */
			caller_info = mono_mempool_alloc0 (prof->mempool, sizeof (CallerInfo));
			*caller_info = *tcaller_info;
			caller_info->next = mprof->caller_info;
			mprof->caller_info = caller_info;
		}
	}
}

static void
merge_thread_data (MonoProfiler *master, MonoProfiler *tprof)
{
	master->jit_time += tprof->jit_time;
	master->methods_jitted += tprof->methods_jitted;
	if (master->max_jit_time < tprof->max_jit_time) {
		master->max_jit_time = tprof->max_jit_time;
		master->max_jit_method = tprof->max_jit_method;
	}

	g_hash_table_foreach (tprof->methods, (GHFunc)merge_methods, master);
}

static void
simple_method_enter (MonoProfiler *prof, MonoMethod *method)
{
	MethodProfile *profile_info;
	LastCallerInfo *callinfo;
	GET_THREAD_PROF (prof);
	/*g_print ("enter %p %s::%s in %d (%p)\n", method, method->klass->name, method->name, GetCurrentThreadId (), prof);*/
	if (!(profile_info = g_hash_table_lookup (prof->methods, method))) {
		profile_info = mono_mempool_alloc0 (prof->mempool, sizeof (MethodProfile));
		MONO_TIMER_INIT (profile_info->u.timer);
		g_hash_table_insert (prof->methods, method, profile_info);
	}
	profile_info->count++;
	if (prof->callers) {
		CallerInfo *cinfo;
		MonoMethod *caller = prof->callers->method;
		for (cinfo = profile_info->caller_info; cinfo; cinfo = cinfo->next) {
			if (cinfo->caller == caller)
				break;
		}
		if (!cinfo) {
			cinfo = mono_mempool_alloc0 (prof->mempool, sizeof (CallerInfo));
			cinfo->caller = caller;
			cinfo->next = profile_info->caller_info;
			profile_info->caller_info = cinfo;
		}
		cinfo->count++;
	}
	if (!(callinfo = prof->cstorage)) {
		callinfo = mono_mempool_alloc (prof->mempool, sizeof (LastCallerInfo));
		MONO_TIMER_INIT (callinfo->timer);
	} else {
		prof->cstorage = prof->cstorage->next;
	}
	callinfo->method = method;
	callinfo->next = prof->callers;
	prof->callers = callinfo;
	MONO_TIMER_START (callinfo->timer);
}

static void
simple_method_leave (MonoProfiler *prof, MonoMethod *method)
{
	MethodProfile *profile_info;
	LastCallerInfo *callinfo, *newcallinfo = NULL;
	
	GET_THREAD_PROF (prof);
	/*g_print ("leave %p %s::%s in %d (%p)\n", method, method->klass->name, method->name, GetCurrentThreadId (), prof);*/
	callinfo = prof->callers;
	/* should really not happen, but we don't catch exceptions events, yet ... */
	while (callinfo) {
		MONO_TIMER_STOP (callinfo->timer);
		profile_info = g_hash_table_lookup (prof->methods, callinfo->method);
		if (profile_info)
			profile_info->total += MONO_TIMER_ELAPSED (callinfo->timer);
		newcallinfo = callinfo->next;
		callinfo->next = prof->cstorage;
		prof->cstorage = callinfo;
		if (callinfo->method == method)
			break;
		callinfo = newcallinfo;
	}
	prof->callers = newcallinfo;
}

static void
simple_allocation (MonoProfiler *prof, MonoObject *obj, MonoClass *klass)
{
	MethodProfile *profile_info;
	AllocInfo *tmp;

	GET_THREAD_PROF (prof);
	if (prof->callers) {
		MonoMethod *caller = prof->callers->method;

		/* Otherwise all allocations are attributed to icall_wrapper_mono_object_new */
		if (caller->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE)
			caller = prof->callers->next->method;

		if (!(profile_info = g_hash_table_lookup (prof->methods, caller)))
			g_assert_not_reached ();
	} else {
		return; /* fine for now */
	}

	for (tmp = profile_info->alloc_info; tmp; tmp = tmp->next) {
		if (tmp->klass == klass)
			break;
	}
	if (!tmp) {
		tmp = mono_mempool_alloc0 (prof->mempool, sizeof (AllocInfo));
		tmp->klass = klass;
		tmp->next = profile_info->alloc_info;
		profile_info->alloc_info = tmp;
	}
	tmp->count++;
	if (klass == mono_defaults.string_class) {
		tmp->mem += sizeof (MonoString) + 2 * mono_string_length ((MonoString*)obj) + 2;
	} else if (klass->parent == mono_defaults.array_class) {
		tmp->mem += sizeof (MonoArray) + mono_array_element_size (klass) * mono_array_length ((MonoArray*)obj);
	} else {
		tmp->mem += mono_class_instance_size (klass);
	}
}

static void
simple_method_jit (MonoProfiler *prof, MonoMethod *method)
{
	GET_THREAD_PROF (prof);
	prof->methods_jitted++;
	MONO_TIMER_START (prof->jit_timer);
}

static void
simple_method_end_jit (MonoProfiler *prof, MonoMethod *method, int result)
{
	double jtime;
	GET_THREAD_PROF (prof);
	MONO_TIMER_STOP (prof->jit_timer);
	jtime = MONO_TIMER_ELAPSED (prof->jit_timer);
	prof->jit_time += jtime;
	if (jtime > prof->max_jit_time) {
		prof->max_jit_time = jtime;
		prof->max_jit_method = method;
	}
}

static void
simple_shutdown (MonoProfiler *prof)
{
	GList *profile = NULL;
	MonoProfiler *tprof;
	GSList *tmp;
	char *str;

	for (tmp = prof->per_thread; tmp; tmp = tmp->next) {
		tprof = tmp->data;
		merge_thread_data (prof, tprof);
	}

	printf("Total time spent compiling %d methods (sec): %.4g\n", prof->methods_jitted, prof->jit_time);
	if (prof->max_jit_method) {
		str = method_get_name (prof->max_jit_method);
		printf("Slowest method to compile (sec): %.4g: %s\n", prof->max_jit_time, str);
		g_free (str);
	}
	g_hash_table_foreach (prof->methods, (GHFunc)build_profile, &profile);
	output_profile (profile);
	g_list_free (profile);
	profile = NULL;
		
	g_hash_table_foreach (prof->methods, (GHFunc)build_newobj_profile, &profile);
	output_newobj_profile (profile);
	g_list_free (profile);
}

static void
mono_profiler_install_simple (const char *desc)
{
	MonoProfiler *prof;
	gchar **args, **ptr;
	MonoProfileFlags flags = MONO_PROFILE_ENTER_LEAVE|MONO_PROFILE_JIT_COMPILATION|MONO_PROFILE_ALLOCATIONS;

	MONO_TIMER_STARTUP;

	if (desc) {
		/* Parse options */
		if (strstr (desc, ":"))
			desc = strstr (desc, ":") + 1;
		else
			desc = NULL;
		args = g_strsplit (desc ? desc : "", ",", -1);

		for (ptr = args; ptr && *ptr; ptr++) {
			const char *arg = *ptr;

			if (!strcmp (arg, "-time"))
				flags &= ~MONO_PROFILE_ENTER_LEAVE;
			else
			   if (!strcmp (arg, "-alloc"))
				   flags &= ~MONO_PROFILE_ALLOCATIONS;
			   else {
				   fprintf (stderr, "profiler : Unknown argument '%s'.\n", arg);
				   return;
			   }
		}
	}

	prof = create_profiler ();
	ALLOC_PROFILER ();
	SET_PROFILER (prof);

	mono_profiler_install (prof, simple_shutdown);
	/* later do also object creation */
	mono_profiler_install_enter_leave (simple_method_enter, simple_method_leave);
	mono_profiler_install_jit_compile (simple_method_jit, simple_method_end_jit);
	mono_profiler_install_allocation (simple_allocation);
	mono_profiler_set_events (flags);
}

typedef void (*ProfilerInitializer) (const char*);
#define INITIALIZER_NAME "mono_profiler_startup"

void 
mono_profiler_load (const char *desc)
{
	if (!desc || (strcmp ("default", desc) == 0) || (strncmp (desc, "default:", 8) == 0)) {
		mono_profiler_install_simple (desc);
	} else {
		GModule *pmodule;
		const char* col = strchr (desc, ':');
		char* libname;
		char* path;
		char *mname;
		if (col != NULL) {
			mname = g_memdup (desc, col - desc);
			mname [col - desc] = 0;
		} else {
			mname = g_strdup (desc);
		}
		libname = g_strdup_printf ("mono-profiler-%s", mname);
		path = g_module_build_path (NULL, libname);
		pmodule = g_module_open (path, G_MODULE_BIND_LAZY);
		if (pmodule) {
			ProfilerInitializer func;
			if (!g_module_symbol (pmodule, INITIALIZER_NAME, (gpointer *)&func)) {
				g_warning ("Cannot find initializer function %s in profiler module: %s", INITIALIZER_NAME, libname);
			} else {
				func (desc);
			}
		} else {
			g_warning ("Error loading profiler module '%s': %s", libname, g_module_error ());
		}

		g_free (libname);
		g_free (mname);
		g_free (path);
	}
}

