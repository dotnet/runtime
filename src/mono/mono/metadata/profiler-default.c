/*
 * profiler-default.c: Builtin profiler
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#include "config.h"
#include "mono/metadata/profiler-default.h"
#include "mono/metadata/profiler-private.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/mono-debug.h"
#include "mono/metadata/debug-mono-symfile.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internal.h"
#include "mono/io-layer/io-layer.h"
#include "mono/utils/mono-dl.h"
#include <string.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_BACKTRACE_SYMBOLS
#include <execinfo.h>
#endif

#ifndef DISABLE_PROFILER

/*
 * Small profiler extracted from mint: we should move it in a loadable module
 * and improve it to do graphs and more accurate timestamping with rdtsc.
 */

static FILE* poutput = NULL;

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
	GSList *domains;
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
	guint64 count;
	guint64 mem;
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

	prof->methods = g_hash_table_new (mono_aligned_addr_hash, NULL);
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
	
	sig = mono_signature_get_desc (mono_method_signature (method), FALSE);
	res = g_strdup_printf ("%s%s%s::%s(%s)", method->klass->name_space,
			method->klass->name_space ? "." : "", method->klass->name,
		method->name, sig);
	g_free (sig);
	return res;
}

static void output_callers (MethodProfile *p);

/* This isn't defined on older glib versions and on some platforms */
#ifndef G_GUINT64_FORMAT
#define G_GUINT64_FORMAT "ul"
#endif
#ifndef G_GINT64_FORMAT
#define G_GINT64_FORMAT "lld"
#endif

static void
output_profile (GList *funcs)
{
	GList *tmp;
	MethodProfile *p;
	char *m;
	guint64 total_calls = 0;

	if (funcs)
		fprintf (poutput, "Time(ms) Count   P/call(ms) Method name\n");
	for (tmp = funcs; tmp; tmp = tmp->next) {
		p = tmp->data;
		total_calls += p->count;
		if (!(gint)(p->total*1000))
			continue;
		m = method_get_name (p->method);
		fprintf (poutput, "########################\n");
		fprintf (poutput, "% 8.3f ", (double) (p->total * 1000));
		fprintf (poutput, "%7" G_GUINT64_FORMAT " ", (guint64)p->count);
		fprintf (poutput, "% 8.3f ", (double) (p->total * 1000)/(double)p->count);
		fprintf (poutput, "  %s\n", m);

		g_free (m);
		/* callers */
		output_callers (p);
	}
	fprintf (poutput, "Total number of calls: %" G_GINT64_FORMAT "\n", (gint64)total_calls);
}

typedef struct {
	MethodProfile *mp;
	guint64 count;
} NewobjProfile;

static gint
compare_newobj_profile (NewobjProfile *profa, NewobjProfile *profb)
{
	if (profb->count == profa->count)
		return 0;
	else
		return profb->count > profa->count ? 1 : -1;
}

static void
build_newobj_profile (MonoClass *class, MethodProfile *mprof, GList **funcs)
{
	NewobjProfile *prof = g_new (NewobjProfile, 1);
	AllocInfo *tmp;
	guint64 count = 0;
	
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
	
	fprintf (poutput, "  Callers (with count) that contribute at least for 1%%:\n");
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
		fprintf (poutput, "    %8d % 3d %% %s\n", cinfo->count, percent, m);
		g_free (m);
	}
}

static int moved_objects = 0;
static void
simple_gc_move (MonoProfiler *prof, void **objects, int num)
{
	moved_objects += num / 2;
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
	guint64 total = 0;
	GSList *sorted, *tmps;

	fprintf (poutput, "\nAllocation profiler\n");

	if (proflist)
		fprintf (poutput, "%-9s %s\n", "Total mem", "Method");
	for (tmp = proflist; tmp; tmp = tmp->next) {
		p = tmp->data;
		total += p->count;
		if (p->count < 50000)
			continue;
		mp = p->mp;
		m = method_get_name (mp->method);
		fprintf (poutput, "########################\n%8" G_GUINT64_FORMAT " KB %s\n", (p->count / 1024), m);
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
			g_snprintf (buf, sizeof (buf), "%s%s%s%s",
				klass->name_space, klass->name_space ? "." : "", klass->name, isarray);
			fprintf (poutput, "    %8" G_GUINT64_FORMAT " KB %8" G_GUINT64_FORMAT " %-48s\n", (ainfo->mem / 1024), ainfo->count, buf);
		}
		/* callers */
		output_callers (mp);
	}
	fprintf (poutput, "Total memory allocated: %" G_GUINT64_FORMAT " KB\n", total / 1024);
	fprintf (poutput, "Objects copied: %d\n", moved_objects);
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
		if (caller->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE && prof->callers->next)
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
	tmp->mem += mono_object_get_size (obj);
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

/* about 10 minutes of samples */
#define MAX_PROF_SAMPLES (1000*60*10)
static int prof_counts = 0;
static int prof_ucounts = 0;
static gpointer* prof_addresses = NULL;
static GHashTable *prof_table = NULL;

static void
simple_stat_hit (MonoProfiler *prof, guchar *ip, void *context)
{
	int pos;

	if (prof_counts >= MAX_PROF_SAMPLES)
		return;
	pos = InterlockedIncrement (&prof_counts);
	prof_addresses [pos - 1] = ip;
}

static int
compare_methods_prof (gconstpointer a, gconstpointer b)
{
	int ca = GPOINTER_TO_UINT (g_hash_table_lookup (prof_table, a));
	int cb = GPOINTER_TO_UINT (g_hash_table_lookup (prof_table, b));
	return cb-ca;
}

static void
prof_foreach (char *method, gpointer c, gpointer data)
{
	GList **list = data;
	*list = g_list_insert_sorted (*list, method, compare_methods_prof);
}

typedef struct Addr2LineData Addr2LineData;

struct Addr2LineData {
	Addr2LineData *next;
	FILE *pipein;
	FILE *pipeout;
	char *binary;
	int child_pid;
};

static Addr2LineData *addr2line_pipes = NULL;

static char*
try_addr2line (const char* binary, gpointer ip)
{
	char buf [1024];
	char *res;
	Addr2LineData *addr2line;

	for (addr2line = addr2line_pipes; addr2line; addr2line = addr2line->next) {
		if (strcmp (binary, addr2line->binary) == 0)
			break;
	}
	if (!addr2line) {
		const char *addr_argv[] = {"addr2line", "-f", "-e", binary, NULL};
		int child_pid;
		int ch_in, ch_out;
#ifdef __linux__
		char monobin [1024];
		/* non-linux platforms will need different code here */
		if (strcmp (binary, "mono") == 0) {
			int count = readlink ("/proc/self/exe", monobin, sizeof (monobin));
			if (count >= 0 && count < sizeof (monobin)) {
				monobin [count] = 0;
				addr_argv [3] = monobin;
			}
		}
#endif
		if (!g_spawn_async_with_pipes (NULL, (char**)addr_argv, NULL, G_SPAWN_SEARCH_PATH, NULL, NULL,
				&child_pid, &ch_in, &ch_out, NULL, NULL)) {
			return g_strdup (binary);
		}
		addr2line = g_new0 (Addr2LineData, 1);
		addr2line->child_pid = child_pid;
		addr2line->binary = g_strdup (binary);
		addr2line->pipein = fdopen (ch_in, "w");
		addr2line->pipeout = fdopen (ch_out, "r");
		addr2line->next = addr2line_pipes;
		addr2line_pipes = addr2line;
	}
	fprintf (addr2line->pipein, "%p\n", ip);
	fflush (addr2line->pipein);
	/* we first get the func name and then file:lineno in a second line */
	if (fgets (buf, sizeof (buf), addr2line->pipeout) && buf [0] != '?') {
		char *end = strchr (buf, '\n');
		char *unused;
		if (end)
			*end = 0;
		res = g_strdup_printf ("%s(%s", binary, buf);
		/* discard the filename/line info */
		unused = fgets (buf, sizeof (buf), addr2line->pipeout);
	} else {
		res = g_strdup (binary);
	}
	return res;
}

static void
stat_prof_report (MonoProfiler *prof)
{
	MonoJitInfo *ji;
	int count = prof_counts;
	int i, c;
	char *mn;
	gpointer ip;
	GList *tmp, *sorted = NULL;
	GSList *l;
	int pcount = ++ prof_counts;

	prof_counts = MAX_PROF_SAMPLES;
	for (i = 0; i < count; ++i) {
		ip = prof_addresses [i];
		ji = mono_jit_info_table_find (mono_domain_get (), ip);

		if (!ji) {
			for (l = prof->domains; l && !ji; l = l->next)
				ji = mono_jit_info_table_find (l->data, ip);
		}

		if (ji) {
			mn = mono_method_full_name (ji->method, TRUE);
		} else {
#ifdef HAVE_BACKTRACE_SYMBOLS
			char **names;
			char *send;
			int no_func;
			prof_ucounts++;
			names = backtrace_symbols (&ip, 1);
			send = strchr (names [0], '+');
			if (send) {
				*send = 0;
				no_func = 0;
			} else {
				no_func = 1;
			}
			send = strchr (names [0], '[');
			if (send)
				*send = 0;
			if (no_func && names [0][0]) {
				char *endp = strchr (names [0], 0);
				while (--endp >= names [0] && g_ascii_isspace (*endp))
					*endp = 0;
				mn = try_addr2line (names [0], ip);
			} else {
				mn = g_strdup (names [0]);
			}
			free (names);
#else
			prof_ucounts++;
			mn = g_strdup_printf ("unmanaged [%p]", ip);
#endif
		}
		c = GPOINTER_TO_UINT (g_hash_table_lookup (prof_table, mn));
		c++;
		g_hash_table_insert (prof_table, mn, GUINT_TO_POINTER (c));
		if (c > 1)
			g_free (mn);
	}
	fprintf (poutput, "prof counts: total/unmanaged: %d/%d\n", pcount, prof_ucounts);
	g_hash_table_foreach (prof_table, (GHFunc)prof_foreach, &sorted);
	for (tmp = sorted; tmp; tmp = tmp->next) {
		double perc;
		c = GPOINTER_TO_UINT (g_hash_table_lookup (prof_table, tmp->data));
		perc = c*100.0/count;
		fprintf (poutput, "%7d\t%5.2f %% %s\n", c, perc, (char*)tmp->data);
	}
	g_list_free (sorted);
}

static void
simple_appdomain_load (MonoProfiler *prof, MonoDomain *domain, int result)
{
	prof->domains = g_slist_prepend (prof->domains, domain);
}

static void
simple_appdomain_unload (MonoProfiler *prof, MonoDomain *domain)
{
	/* FIXME: we should actually record partial data for each domain, 
	 * but at this point it's must easier using the new logging profiler.
	 */
	mono_profiler_shutdown ();
}

static gint32 simple_shutdown_done = FALSE;

static void
simple_shutdown (MonoProfiler *prof)
{
	GList *profile = NULL;
	MonoProfiler *tprof;
	GSList *tmp;
	char *str;
	gint32 see_shutdown_done;

#ifndef HOST_WIN32
	mono_thread_attach(mono_get_root_domain());
#endif

	// Make sure we execute simple_shutdown only once
	see_shutdown_done = InterlockedExchange(& simple_shutdown_done, TRUE);
	if (see_shutdown_done)
		return;

	if (mono_profiler_events & MONO_PROFILE_STATISTICAL) {
		stat_prof_report (prof);
	}

	// Stop all incoming events
	mono_profiler_set_events (0);
	
	for (tmp = prof->per_thread; tmp; tmp = tmp->next) {
		tprof = tmp->data;
		merge_thread_data (prof, tprof);
	}

	fprintf (poutput, "Total time spent compiling %d methods (sec): %.4g\n", prof->methods_jitted, prof->jit_time);
	if (prof->max_jit_method) {
		str = method_get_name (prof->max_jit_method);
		fprintf (poutput, "Slowest method to compile (sec): %.4g: %s\n", prof->max_jit_time, str);
		g_free (str);
	}
	g_hash_table_foreach (prof->methods, (GHFunc)build_profile, &profile);
	output_profile (profile);
	g_list_free (profile);
	profile = NULL;
		
	g_hash_table_foreach (prof->methods, (GHFunc)build_newobj_profile, &profile);
	output_newobj_profile (profile);
	g_list_free (profile);

	g_free (prof_addresses);
	prof_addresses = NULL;
	g_hash_table_destroy (prof_table);
}

void
mono_profiler_install_simple (const char *desc)
{
	MonoProfiler *prof;
	gchar **args, **ptr;
	MonoProfileFlags flags = 0;

	MONO_TIMER_STARTUP;
	poutput = stdout;

	if (!desc)
		desc = "alloc,time,jit";

	if (desc) {
		/* Parse options */
		if (strstr (desc, ":"))
			desc = strstr (desc, ":") + 1;
		else
			desc = "alloc,time,jit";
		args = g_strsplit (desc, ",", -1);

		for (ptr = args; ptr && *ptr; ptr++) {
			const char *arg = *ptr;

			// Alwais listen to appdomaon events to shutdown at the first unload
			flags |= MONO_PROFILE_APPDOMAIN_EVENTS;
			if (!strcmp (arg, "time"))
				flags |= MONO_PROFILE_ENTER_LEAVE | MONO_PROFILE_EXCEPTIONS;
			else if (!strcmp (arg, "alloc"))
				flags |= MONO_PROFILE_ALLOCATIONS;
			else if (!strcmp (arg, "stat"))
				flags |= MONO_PROFILE_STATISTICAL;
			else if (!strcmp (arg, "jit"))
				flags |= MONO_PROFILE_JIT_COMPILATION;
			else if (strncmp (arg, "file=", 5) == 0) {
				poutput = fopen (arg + 5, "wb");
				if (!poutput) {
					poutput = stdout;
					fprintf (stderr, "profiler : cannot open profile output file '%s'.\n", arg + 5);
				}
			} else {
				fprintf (stderr, "profiler : Unknown argument '%s'.\n", arg);
				return;
			}
		}
	}
	if (flags & MONO_PROFILE_ALLOCATIONS)
		flags |= MONO_PROFILE_GC_MOVES;
	if (flags & MONO_PROFILE_ALLOCATIONS)
		flags |= MONO_PROFILE_ENTER_LEAVE | MONO_PROFILE_EXCEPTIONS;
	if (!flags)
		flags = MONO_PROFILE_ENTER_LEAVE | MONO_PROFILE_ALLOCATIONS | MONO_PROFILE_JIT_COMPILATION | MONO_PROFILE_EXCEPTIONS;

	prof = create_profiler ();
	ALLOC_PROFILER ();
	SET_PROFILER (prof);

	/* statistical profiler data */
	prof_addresses = g_new0 (gpointer, MAX_PROF_SAMPLES);
	prof_table = g_hash_table_new (g_str_hash, g_str_equal);

	mono_profiler_install (prof, simple_shutdown);
	mono_profiler_install_enter_leave (simple_method_enter, simple_method_leave);
	mono_profiler_install_exception (NULL, simple_method_leave, NULL);
	mono_profiler_install_jit_compile (simple_method_jit, simple_method_end_jit);
	mono_profiler_install_allocation (simple_allocation);
	mono_profiler_install_appdomain (NULL, simple_appdomain_load, simple_appdomain_unload, NULL);
	mono_profiler_install_statistical (simple_stat_hit);
	mono_profiler_install_gc_moves (simple_gc_move);
	mono_profiler_set_events (flags);
}

#endif /* DISABLE_PROFILER */
