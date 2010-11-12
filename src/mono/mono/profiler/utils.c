/*
 * utils.c: log profiler and reporter utils
 *
 * We have here the minimal needed portability functions: we can't depend
 * on the ones provided by the runtime, since they are internal and,
 * especially mprof-report is an external program.
 * Note also that we don't take a glib/eglib dependency here for mostly
 * the same reason (but also because we need tight control in the profiler
 * over memory allocation, which needs to work with the world stopped).
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 */
#include "utils.h"
#include <stdlib.h>
#include <time.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>
#ifdef HOST_WIN32
#include <windows.h>
#else
#include <pthread.h>
#include <sched.h>
#endif


#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#if HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif

#if defined(__APPLE__)
#include <mach/mach_time.h>  
#include <stdio.h> 

static mach_timebase_info_data_t timebase_info;
#endif

#ifndef MAP_ANONYMOUS
#define MAP_ANONYMOUS MAP_ANON
#endif

#define TICKS_PER_SEC 1000000000LL

#if (defined(TARGET_X86) || defined(TARGET_AMD64)) && defined(__linux__) && defined(HAVE_SCHED_GETCPU)
#define HAVE_RDTSC 1
#endif

typedef struct {
	unsigned int timer_count;
	int last_cpu;
	uint64_t last_rdtsc;
	uint64_t last_time;
} TlsData;

#ifdef HOST_WIN32
static int tls_data;
#define DECL_TLS_DATA TlsData *tls; tls = (TlsData *) TlsGetValue (tls_data); if (tls == NULL) { tls = (TlsData *) calloc (sizeof (TlsData), 1); TlsSetValue (tls_data, tls); }
#define TLS_INIT(x) x = TlsAlloc()
#elif HAVE_KW_THREAD
static __thread TlsData tls_data;
#define DECL_TLS_DATA TlsData *tls = &tls_data
#define TLS_INIT(x)
#else
static pthread_key_t tls_data;
#define DECL_TLS_DATA TlsData *tls; tls = (TlsData *) pthread_getspecific (tls_data); if (tls == NULL) { tls = (TlsData *) calloc (sizeof (TlsData), 1); pthread_setspecific (tls_data, tls); }
#define TLS_INIT(x) pthread_key_create(&x, NULL)
#endif

#ifdef HOST_WIN32
static CRITICAL_SECTION log_lock;
static LARGE_INTEGER pcounter_freq;
#else
static pthread_mutex_t log_lock = PTHREAD_MUTEX_INITIALIZER;
#endif

static int timer_overhead = 0;
static uint64_t time_inc = 0;
typedef uint64_t (*TimeFunc)(void);

static TimeFunc time_func;

static uint64_t
clock_time (void)
{
#if defined(__APPLE__)
	uint64_t time = mach_absolute_time ();
	
	time *= timebase_info.numer;
	time /= timebase_info.denom;

	return time;
#elif defined(HOST_WIN32)
	LARGE_INTEGER value;
	QueryPerformanceCounter (&value);
	return value.QuadPart * TICKS_PER_SEC / pcounter_freq.QuadPart;
#elif defined(CLOCK_MONOTONIC)
	struct timespec tspec;
	clock_gettime (CLOCK_MONOTONIC, &tspec);
	return ((uint64_t)tspec.tv_sec * TICKS_PER_SEC + tspec.tv_nsec);
#else
	struct timeval tv;
	gettimeofday (&tv, NULL);
	return ((uint64_t)tv.tv_sec * TICKS_PER_SEC + tv.tv_usec * 1000);
#endif
}

/* must be power of two */
#define TIME_ADJ 8

static uint64_t
fast_current_time (void)
{
	DECL_TLS_DATA;
	if (tls->timer_count++ & (TIME_ADJ - 1)) {
		tls->last_time += time_inc;
		return tls->last_time;
	}
	tls->last_time = clock_time ();
	return tls->last_time;
}

#if HAVE_RDTSC

#define rdtsc(low,high) \
	__asm__ __volatile__("rdtsc" : "=a" (low), "=d" (high))

static uint64_t
safe_rdtsc (int *cpu)
{
	unsigned int low, high;
	int c1 = sched_getcpu ();
	int c2;
	rdtsc (low, high);
	c2 = sched_getcpu ();
	if (c1 != c2) {
		*cpu = -1;
		return 0;
	}
	*cpu = c1;
	return (((uint64_t) high) << 32) + (uint64_t)low;
}

static double cpu_freq;

static int 
have_rdtsc (void) {
	char buf[256];
	int have_freq = 0;
	int have_flag = 0;
	float val;
	FILE *cpuinfo;
	int cpu = sched_getcpu ();

	if (cpu < 0)
		return 0;

	if (!(cpuinfo = fopen ("/proc/cpuinfo", "r")))
		return 0;
	while (fgets (buf, sizeof(buf), cpuinfo)) {
		if (sscanf (buf, "cpu MHz : %f", &val) == 1) {
			/*printf ("got mh: %f\n", val);*/
			have_freq = 1;
			cpu_freq = val * 1000000;
		}
		if (strncmp (buf, "flags :", 5) == 0) {
			if (strstr (buf, "constant_tsc")) {
				have_flag = 1;
				/*printf ("have tsc\n");*/
			}
		}
	}
	fclose (cpuinfo);
	return have_flag? have_freq: 0;
}

static uint64_t
rdtsc_current_time (void)
{
	DECL_TLS_DATA;
	if (tls->timer_count++ & (TIME_ADJ*8 - 1)) {
		int cpu;
		uint64_t tsc = safe_rdtsc (&cpu);
		if (cpu != -1 && cpu == tls->last_cpu) {
			int64_t diff = tsc - tls->last_rdtsc;
			uint64_t nsecs;
			if (diff > 0) {
				nsecs = (double)diff/cpu_freq;
				//printf ("%llu cycles: %llu nsecs\n", diff, nsecs);
				return tls->last_time + nsecs;
			} else {
				printf ("tsc went backwards\n");
			}
		} else {
			//printf ("wrong cpu: %d\n", cpu);
		}
	}
	tls->last_time = clock_time ();
	tls->last_rdtsc = safe_rdtsc (&tls->last_cpu);
	return tls->last_time;
}
#else
#define have_rdtsc() 0
#define rdtsc_current_time fast_current_time
#endif

static uint64_t
null_time (void)
{
	static uint64_t timer = 0;
	return timer++;
}

void
utils_init (int fast_time)
{
	int i;
	uint64_t time_start, time_end;
	TLS_INIT (tls_data);
#ifdef HOST_WIN32
	InitializeCriticalSection (&log_lock);
	QueryPerformanceFrequency (&pcounter_freq);
#endif
#if defined (__APPLE__)
	mach_timebase_info (&timebase_info);
#endif

	if (fast_time > 1) {
		time_func = null_time;
	} else if (fast_time) {
		uint64_t timea;
		uint64_t timeb;
		clock_time ();
		timea = clock_time ();
		timeb = clock_time ();
		time_inc = (timeb - timea) / TIME_ADJ;
		/*printf ("time inc: %llu, timea: %llu, timeb: %llu, diff: %llu\n", time_inc, timea, timeb, timec-timeb);*/
		if (have_rdtsc ())
			time_func = rdtsc_current_time;
		else
			time_func = fast_current_time;
	} else {
		time_func = clock_time;
	}
	time_start = time_func ();
	for (i = 0; i < 256; ++i)
		time_func ();
	time_end = time_func ();
	timer_overhead = (time_end - time_start) / 256;
}

int
get_timer_overhead (void)
{
	return timer_overhead;
}

uint64_t
current_time (void)
{
	return time_func ();
}

void*
alloc_buffer (int size)
{
	void *ptr;
#ifdef HOST_WIN32
	ptr = VirtualAlloc (NULL, size, MEM_COMMIT, PAGE_READWRITE);
	return ptr;
#else
	ptr = mmap (NULL, size, PROT_READ|PROT_WRITE, MAP_ANONYMOUS|MAP_PRIVATE, -1, 0);
	if (ptr == (void*)-1)
		return NULL;
	return ptr;
#endif
}

void
free_buffer (void *buf, int size)
{
#ifdef HOST_WIN32
	VirtualFree (buf, 0, MEM_RELEASE);
#else
	munmap (buf, size);
#endif
}

void
take_lock (void)
{
#ifdef HOST_WIN32
	EnterCriticalSection (&log_lock);
#else
	pthread_mutex_lock (&log_lock);
#endif
}

void
release_lock (void)
{
#ifdef HOST_WIN32
	LeaveCriticalSection (&log_lock);
#else
	pthread_mutex_unlock (&log_lock);
#endif
}

void
encode_uleb128 (uint64_t value, uint8_t *buf, uint8_t **endbuf)
{
	uint8_t *p = buf;

	do {
		uint8_t b = value & 0x7f;
		value >>= 7;
		if (value != 0) /* more bytes to come */
			b |= 0x80;
		*p ++ = b;
	} while (value);

	*endbuf = p;
}

void
encode_sleb128 (intptr_t value, uint8_t *buf, uint8_t **endbuf)
{
	int more = 1;
	int negative = (value < 0);
	unsigned int size = sizeof (intptr_t) * 8;
	uint8_t byte;
	uint8_t *p = buf;

	while (more) {
		byte = value & 0x7f;
		value >>= 7;
		/* the following is unnecessary if the
		 * implementation of >>= uses an arithmetic rather
		 * than logical shift for a signed left operand
		 */
		if (negative)
			/* sign extend */
			value |= - ((intptr_t)1 <<(size - 7));
		/* sign bit of byte is second high order bit (0x40) */
		if ((value == 0 && !(byte & 0x40)) ||
			(value == -1 && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;
		*p ++= byte;
	}

	*endbuf = p;
}

uint64_t
decode_uleb128 (uint8_t *buf, uint8_t **endbuf)
{
	uint64_t res = 0;
	int shift = 0;

	while (1) {
		uint8_t b = *buf++;

		res |= (((uint64_t)(b & 0x7f)) << shift);
		if (!(b & 0x80))
			break;
		shift += 7;
	}

	*endbuf = buf;

	return res;
}

intptr_t
decode_sleb128 (uint8_t *buf, uint8_t **endbuf)
{
	uint8_t *p = buf;
	intptr_t res = 0;
	int shift = 0;

	while (1) {
		uint8_t b = *p;
		p ++;

		res = res | (((intptr_t)(b & 0x7f)) << shift);
		shift += 7;
		if (!(b & 0x80)) {
			if (shift < sizeof (intptr_t) * 8 && (b & 0x40))
				res |= - ((intptr_t)1 << shift);
			break;
		}
	}

	*endbuf = p;

	return res;
}

uintptr_t
thread_id (void)
{
#ifdef HOST_WIN32
	return (uintptr_t)GetCurrentThreadId ();
#else
	return (uintptr_t)pthread_self ();
#endif
}

uintptr_t
process_id (void)
{
#ifdef HOST_WIN32
	return 0; /* FIXME */
#else
	return (uintptr_t)getpid ();
#endif
}

