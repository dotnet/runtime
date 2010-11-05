#include <stdlib.h>
#include <inttypes.h>
#include <time.h>
#include <pthread.h>
#include <stdio.h>
#include <string.h>
#include <sched.h>

#include "utils.h"

//#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
//#endif
//#if HAVE_SYS_MMAN_H
#include <sys/mman.h>
//#endif

#if defined(__APPLE__)
#include <mach/mach_time.h>  
#include <stdio.h> 

static mach_timebase_info_data_t timebase_info;
#endif

#ifndef MAP_ANON
#define MAP_ANONYMOUS MAP_ANON
#endif

#define TICKS_PER_SEC 1000000000LL

typedef struct {
	unsigned int timer_count;
	int last_cpu;
	uint64_t last_rdtsc;
	uint64_t last_time;
} TlsData;

#if HAVE_KW_THREAD
static __thread TlsData tls_data;
#define DECL_TLS_DATA TlsData *tls = &tls_data
#define TLS_INIT(x)
#else
static pthread_key_t tls_data;
#define DECL_TLS_DATA TlsData *tls; tls = (TlsData *) pthread_getspecific (tls_data); if (tls == NULL) { tls = (TlsData *) malloc (sizeof (TlsData)); pthread_setspecific (tls_data, tls); }
#define TLS_INIT(x) pthread_key_create(&x, NULL)
#endif

static pthread_mutex_t log_lock = PTHREAD_MUTEX_INITIALIZER;

static uint64_t time_inc = 0;
typedef uint64_t (*TimeFunc)(void);

static TimeFunc time_func;

static uint64_t
clock_time (void)
{
#if defined(__APPLE__)
	uint64_t time = mach_absolute_time ();
	
	time *= info.numer;
	time /= info.denom;

	return time;
#else
	struct timespec tspec;
	clock_gettime (CLOCK_MONOTONIC, &tspec);
	return ((uint64_t)tspec.tv_sec * TICKS_PER_SEC + tspec.tv_nsec);
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

#define rdtsc(low,high) \
	__asm__ __volatile__("rdtsc" : "=a" (low), "=d" (high))

#if !defined(__APPLE__)
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
	TLS_INIT (tls_data);

	if (fast_time > 1) {
		time_func = null_time;
	} else if (fast_time) {
#if defined (__APPLE__)
		mach_timebase_info (&timebase_info);
		time_func = fast_current_time;
#else
		uint64_t timea;
		uint64_t timeb;
		int cpu = sched_getcpu ();
		clock_time ();
		timea = clock_time ();
		timeb = clock_time ();
		time_inc = (timeb - timea) / TIME_ADJ;
		/*printf ("time inc: %llu, timea: %llu, timeb: %llu, diff: %llu\n", time_inc, timea, timeb, timec-timeb);*/
		if (cpu != -1 && have_rdtsc ())
			time_func = rdtsc_current_time;
		else
			time_func = fast_current_time;
#endif
	} else {
		time_func = clock_time;
	}
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
	ptr = mmap (NULL, size, PROT_READ|PROT_WRITE, MAP_ANONYMOUS|MAP_PRIVATE, -1, 0);
	if (ptr == (void*)-1)
		return NULL;
	return ptr;
}

void
free_buffer (void *buf, int size)
{
	munmap (buf, size);
}

void
take_lock (void)
{
	pthread_mutex_lock (&log_lock);
}

void
release_lock (void)
{
	pthread_mutex_unlock (&log_lock);
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

/* FIXME: make sure this works for 64 bit systems/values */
void
encode_sleb128 (intptr_t value, uint8_t *buf, uint8_t **endbuf)
{
	int more = 1;
	int negative = (value < 0);
	unsigned int size = 32;
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
			value |= - (1 <<(size - 7));
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

		res = res | (((int)(b & 0x7f)) << shift);
		shift += 7;
		if (!(b & 0x80)) {
			if (shift < 32 && (b & 0x40))
				res |= - (1 << shift);
			break;
		}
	}

	*endbuf = p;

	return res;
}

uintptr_t
thread_id (void)
{
	return (uintptr_t)pthread_self ();
}

