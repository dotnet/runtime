/*
 * mono-perfcounters.c
 *
 * Performance counters support.
 *
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2008 Novell, Inc
 */

#include "config.h"
#include <time.h>
#include <string.h>
#include <stdlib.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#include "metadata/mono-perfcounters.h"
#include "metadata/appdomain.h"
/* for mono_stats */
#include "metadata/class-internals.h"
#include "utils/mono-time.h"
#include <mono/io-layer/io-layer.h>

/* map of CounterSample.cs */
struct _MonoCounterSample {
	gint64 rawValue;
	gint64 baseValue;
	gint64 counterFrequency;
	gint64 systemFrequency;
	gint64 timeStamp;
	gint64 timeStamp100nSec;
	gint64 counterTimeStamp;
	int counterType;
};

/* map of PerformanceCounterType.cs */
enum {
	NumberOfItemsHEX32=0x00000000,
	NumberOfItemsHEX64=0x00000100,
	NumberOfItems32=0x00010000,
	NumberOfItems64=0x00010100,
	CounterDelta32=0x00400400,
	CounterDelta64=0x00400500,
	SampleCounter=0x00410400,
	CountPerTimeInterval32=0x00450400,
	CountPerTimeInterval64=0x00450500,
	RateOfCountsPerSecond32=0x10410400,
	RateOfCountsPerSecond64=0x10410500,
	RawFraction=0x20020400,
	CounterTimer=0x20410500,
	Timer100Ns=0x20510500,
	SampleFraction=0x20C20400,
	CounterTimerInverse=0x21410500,
	Timer100NsInverse=0x21510500,
	CounterMultiTimer=0x22410500,
	CounterMultiTimer100Ns=0x22510500,
	CounterMultiTimerInverse=0x23410500,
	CounterMultiTimer100NsInverse=0x23510500,
	AverageTimer32=0x30020400,
	ElapsedTime=0x30240500,
	AverageCount64=0x40020500,
	SampleBase=0x40030401,
	AverageBase=0x40030402,
	RawBase=0x40030403,
	CounterMultiBase=0x42030500
};

enum {
	SingleInstance,
	MultiInstance,
	CatTypeUnknown = -1
};

#define PERFCTR_CAT(id,name,help,type,first_counter) CATEGORY_ ## id,
#define PERFCTR_COUNTER(id,name,help,type)
enum {
#include "mono-perfcounters-def.h"
	NUM_CATEGORIES
};

#undef PERFCTR_CAT
#undef PERFCTR_COUNTER
#define PERFCTR_CAT(id,name,help,type,first_counter) CATEGORY_START_ ## id = -1,
#define PERFCTR_COUNTER(id,name,help,type) COUNTER_ ## id,
/* each counter is assigned an id starting from 0 inside the category */
enum {
#include "mono-perfcounters-def.h"
	END_COUNTERS
};

#undef PERFCTR_CAT
#undef PERFCTR_COUNTER
#define PERFCTR_CAT(id,name,help,type,first_counter)
#define PERFCTR_COUNTER(id,name,help,type) CCOUNTER_ ## id,
/* this is used just to count the number of counters */
enum {
#include "mono-perfcounters-def.h"
	NUM_COUNTERS
};

typedef struct {
	const char *name;
	const char *help;
	unsigned char id;
	signed char type;
	short first_counter;
} CategoryDesc;

typedef struct {
	const char *name;
	const char *help;
	int id;
	int type;
} CounterDesc;

#undef PERFCTR_CAT
#undef PERFCTR_COUNTER
#define PERFCTR_CAT(id,name,help,type,first_counter) {name, help, CATEGORY_ ## id, type, CCOUNTER_ ## first_counter},
#define PERFCTR_COUNTER(id,name,help,type)
static const CategoryDesc
predef_categories [] = {
#include "mono-perfcounters-def.h"
	{NULL, NULL, NUM_CATEGORIES, -1, NUM_COUNTERS}
};

#undef PERFCTR_CAT
#undef PERFCTR_COUNTER
#define PERFCTR_CAT(id,name,help,type,first_counter)
#define PERFCTR_COUNTER(id,name,help,type) {name, help, COUNTER_ ## id, type},
static const CounterDesc
predef_counters [] = {
#include "mono-perfcounters-def.h"
	{NULL, NULL, -1, 0}
};

/*
 * We have several different classes of counters:
 * *) system counters
 * *) runtime counters
 * *) remote counters
 * *) user-defined counters
 * *) windows counters (the implementation on windows will use this)
 *
 * To easily handle the differences we create a vtable for each class that contains the
 * function pointers with the actual implementation to access the counters.
 */
typedef struct _ImplVtable ImplVtable;

typedef MonoBoolean (*SampleFunc) (ImplVtable *vtable, MonoBoolean only_value, MonoCounterSample* sample);
typedef gint64 (*UpdateFunc) (ImplVtable *vtable, MonoBoolean do_incr, gint64 value);

struct _ImplVtable {
	void *arg;
	SampleFunc sample;
	UpdateFunc update;
};

static ImplVtable*
create_vtable (void *arg, SampleFunc sample, UpdateFunc update)
{
	ImplVtable *vtable = g_new (ImplVtable, 1);
	vtable->arg = arg;
	vtable->sample = sample;
	vtable->update = update;
	return vtable;
}

MonoPerfCounters *mono_perfcounters = NULL;

void
mono_perfcounters_init (void)
{
	/* later allocate in the shared memory area */
	mono_perfcounters = g_new0 (MonoPerfCounters, 1);
}

static int
mono_string_compare_ascii (MonoString *str, const char *ascii_str)
{
	guint16 *strc = mono_string_chars (str);
	while (*strc == *ascii_str++) {
		if (*strc == 0)
			return 0;
		strc++;
	}
	return *strc - *(const unsigned char *)(ascii_str - 1);
}

/* fill the info in sample (except the raw value) */
static void
fill_sample (MonoCounterSample *sample)
{
	sample->timeStamp = mono_100ns_ticks ();
	sample->timeStamp100nSec = sample->timeStamp;
	sample->counterTimeStamp = sample->timeStamp;
	sample->counterFrequency = 10000000;
	sample->systemFrequency = 10000000;
	// the real basevalue needs to be get from a different counter...
	sample->baseValue = 0;
}

static int
id_from_string (MonoString *instance)
{
	int id = -1;
	if (mono_string_length (instance)) {
		char *id_str = mono_string_to_utf8 (instance);
		char *end;
		id = strtol (id_str, &end, 0);
		g_free (id_str);
		if (end == id_str)
			return -1;
	}
	return id;
}

static void
get_cpu_times (int cpu_id, gint64 *user, gint64 *systemt, gint64 *irq, gint64 *sirq, gint64 *idle)
{
	char buf [256];
	char *s;
	int hz = 100 * 2; // 2 numprocs
	gint64 user_ticks, nice_ticks, system_ticks, idle_ticks, iowait_ticks, irq_ticks, sirq_ticks;
	FILE *f = fopen ("/proc/stat", "r");
	if (!f)
		return;
	while ((s = fgets (buf, sizeof (buf), f))) {
		char *data = NULL;
		if (cpu_id < 0 && strncmp (s, "cpu", 3) == 0 && g_ascii_isspace (s [3])) {
			data = s + 4;
		} else if (cpu_id >= 0 && strncmp (s, "cpu", 3) == 0 && strtol (s + 3, &data, 10) == cpu_id) {
			if (data == s + 3)
				continue;
			data++;
		} else {
			continue;
		}
		sscanf (data, "%Lu %Lu %Lu %Lu %Lu %Lu %Lu", &user_ticks, &nice_ticks, &system_ticks, &idle_ticks, &iowait_ticks, &irq_ticks, &sirq_ticks);
	}
	fclose (f);

	if (user)
		*user = (user_ticks + nice_ticks) * 10000000 / hz;
	if (systemt)
		*systemt = (system_ticks) * 10000000 / hz;
	if (irq)
		*irq = (irq_ticks) * 10000000 / hz;
	if (sirq)
		*sirq = (sirq_ticks) * 10000000 / hz;
	if (idle)
		*idle = (idle_ticks) * 10000000 / hz;
}

static MonoBoolean
get_cpu_counter (ImplVtable *vtable, MonoBoolean only_value, MonoCounterSample *sample)
{
	gint64 value = 0;
	int id = GPOINTER_TO_INT (vtable->arg);
	int pid = id >> 5;
	id &= 0x1f;
	if (!only_value) {
		fill_sample (sample);
		sample->baseValue = 1;
	}
	sample->counterType = predef_counters [predef_categories [CATEGORY_PROC].first_counter + id].type;
	switch (id) {
	case COUNTER_CPU_USER_TIME:
		get_cpu_times (pid, &value, NULL, NULL, NULL, NULL);
		sample->rawValue = value;
		return TRUE;
	case COUNTER_CPU_PRIV_TIME:
		get_cpu_times (pid, NULL, &value, NULL, NULL, NULL);
		sample->rawValue = value;
		return TRUE;
	case COUNTER_CPU_INTR_TIME:
		get_cpu_times (pid, NULL, NULL, &value, NULL, NULL);
		sample->rawValue = value;
		return TRUE;
	case COUNTER_CPU_DCP_TIME:
		get_cpu_times (pid, NULL, NULL, NULL, &value, NULL);
		sample->rawValue = value;
		return TRUE;
	case COUNTER_CPU_PROC_TIME:
		get_cpu_times (pid, NULL, NULL, NULL, NULL, &value);
		sample->rawValue = value;
		return TRUE;
	}
	return FALSE;
}

static void*
cpu_get_impl (MonoString* counter, MonoString* instance, int *type, MonoBoolean *custom)
{
	int id = id_from_string (instance) << 5;
	int i;
	const CategoryDesc *desc = &predef_categories [CATEGORY_CPU];
	*custom = FALSE;
	/* increase the shift above and the mask also in the implementation functions */
	g_assert (32 > desc [1].first_counter - desc->first_counter);
	for (i = desc->first_counter; i < desc [1].first_counter; ++i) {
		const CounterDesc *cdesc = &predef_counters [i];
		if (mono_string_compare_ascii (counter, cdesc->name) == 0) {
			*type = cdesc->type;
			return create_vtable (GINT_TO_POINTER (id | cdesc->id), get_cpu_counter, NULL);
		}
	}
	return NULL;
}

/*
 * /proc/pid/stat format:
 * pid (cmdname) S 
 * 	[0] ppid pgid sid tty_nr tty_pgrp flags min_flt cmin_flt maj_flt cmaj_flt
 * 	[10] utime stime cutime cstime prio nice threads start_time vsize rss
 * 	[20] rsslim start_code end_code start_stack esp eip pending blocked sigign sigcatch
 * 	[30] wchan 0 0 exit_signal cpu rt_prio policy
 */

static gint64
get_process_time (int pid, int pos, int sum)
{
	char buf [512];
	char *s, *end;
	FILE *f;
	int len, i;
	gint64 value;

	g_snprintf (buf, sizeof (buf), "/proc/%d/stat", pid);
	f = fopen (buf, "r");
	if (!f)
		return 0;
	len = fread (buf, 1, sizeof (buf), f);
	fclose (f);
	if (len <= 0)
		return 0;
	s = strchr (buf, ')');
	if (!s)
		return 0;
	s++;
	while (g_ascii_isspace (*s)) s++;
	if (!*s)
		return 0;
	/* skip the status char */
	while (*s && !g_ascii_isspace (*s)) s++;
	if (!*s)
		return 0;
	for (i = 0; i < pos; ++i) {
		while (g_ascii_isspace (*s)) s++;
		if (!*s)
			return 0;
		while (*s && !g_ascii_isspace (*s)) s++;
		if (!*s)
			return 0;
	}
	/* we are finally at the needed item */
	value = strtoul (s, &end, 0);
	/* add also the following value */
	if (sum) {
		while (g_ascii_isspace (*s)) s++;
		if (!*s)
			return 0;
		value += strtoul (s, &end, 0);
	}
	return value;
}

static gint64
get_pid_stat_item (int pid, const char *item)
{
	char buf [256];
	char *s;
	FILE *f;
	int len = strlen (item);

	g_snprintf (buf, sizeof (buf), "/proc/%d/status", pid);
	f = fopen (buf, "r");
	if (!f)
		return 0;
	while ((s = fgets (buf, sizeof (buf), f))) {
		if (*item != *buf)
			continue;
		if (strncmp (buf, item, len))
			continue;
		if (buf [len] != ':')
			continue;
		fclose (f);
		return atoi (buf + len + 1);
	}
	fclose (f);
	return 0;
}

static MonoBoolean
get_process_counter (ImplVtable *vtable, MonoBoolean only_value, MonoCounterSample *sample)
{
	int id = GPOINTER_TO_INT (vtable->arg);
	int pid = id >> 5;
	if (pid < 0)
		return FALSE;
	id &= 0x1f;
	if (!only_value) {
		fill_sample (sample);
		sample->baseValue = 1;
	}
	sample->counterType = predef_counters [predef_categories [CATEGORY_PROC].first_counter + id].type;
	switch (id) {
	case COUNTER_PROC_USER_TIME:
		sample->rawValue = get_process_time (pid, 12, FALSE);
		return TRUE;
	case COUNTER_PROC_PRIV_TIME:
		sample->rawValue = get_process_time (pid, 13, FALSE);
		return TRUE;
	case COUNTER_PROC_PROC_TIME:
		sample->rawValue = get_process_time (pid, 12, TRUE);
		return TRUE;
	case COUNTER_PROC_THREADS:
		sample->rawValue = get_pid_stat_item (pid, "Threads");
		return TRUE;
	case COUNTER_PROC_VBYTES:
		sample->rawValue = get_pid_stat_item (pid, "VmSize") * 1024;
		return TRUE;
	case COUNTER_PROC_WSET:
		sample->rawValue = get_pid_stat_item (pid, "VmRSS") * 1024;
		return TRUE;
	case COUNTER_PROC_PBYTES:
		sample->rawValue = get_pid_stat_item (pid, "VmData") * 1024;
		return TRUE;
	}
	return FALSE;
}

static void*
process_get_impl (MonoString* counter, MonoString* instance, int *type, MonoBoolean *custom)
{
	int id = id_from_string (instance) << 5;
	int i;
	const CategoryDesc *desc = &predef_categories [CATEGORY_PROC];
	*custom = FALSE;
	/* increase the shift above and the mask also in the implementation functions */
	g_assert (32 > desc [1].first_counter - desc->first_counter);
	for (i = desc->first_counter; i < desc [1].first_counter; ++i) {
		const CounterDesc *cdesc = &predef_counters [i];
		if (mono_string_compare_ascii (counter, cdesc->name) == 0) {
			*type = cdesc->type;
			return create_vtable (GINT_TO_POINTER (id | cdesc->id), get_process_counter, NULL);
		}
	}
	return NULL;
}

static MonoBoolean
mono_mem_counter (ImplVtable *vtable, MonoBoolean only_value, MonoCounterSample *sample)
{
	int id = GPOINTER_TO_INT (vtable->arg);
	if (!only_value) {
		fill_sample (sample);
		sample->baseValue = 1;
	}
	sample->counterType = predef_counters [predef_categories [CATEGORY_MONO_MEM].first_counter + id].type;
	switch (id) {
	case COUNTER_MEM_NUM_OBJECTS:
		sample->rawValue = mono_stats.new_object_count;
		return TRUE;
	}
	return FALSE;
}

static void*
mono_mem_get_impl (MonoString* counter, MonoString* instance, int *type, MonoBoolean *custom)
{
	int i;
	const CategoryDesc *desc = &predef_categories [CATEGORY_MONO_MEM];
	*custom = FALSE;
	for (i = desc->first_counter; i < desc [1].first_counter; ++i) {
		const CounterDesc *cdesc = &predef_counters [i];
		if (mono_string_compare_ascii (counter, cdesc->name) == 0) {
			*type = cdesc->type;
			return create_vtable (GINT_TO_POINTER (cdesc->id), mono_mem_counter, NULL);
		}
	}
	return NULL;
}

/* consider storing the pointer directly in vtable->arg, so the runtime overhead is lower:
 * this needs some way to set sample->counterType as well, though.
 */
static MonoBoolean
predef_writable_counter (ImplVtable *vtable, MonoBoolean only_value, MonoCounterSample *sample)
{
	int cat_id = GPOINTER_TO_INT (vtable->arg);
	int id = cat_id >> 16;
	cat_id &= 0xffff;
	if (!only_value) {
		fill_sample (sample);
		sample->baseValue = 1;
	}
	sample->counterType = predef_counters [predef_categories [cat_id].first_counter + id].type;
	switch (id) {
	case COUNTER_ASPNET_REQ_Q:
		sample->rawValue = mono_perfcounters->aspnet_requests_queued;
		return TRUE;
	}
	return FALSE;
}

static gint64
predef_writable_update (ImplVtable *vtable, MonoBoolean do_incr, gint64 value)
{
	glong *ptr = NULL;
	int cat_id = GPOINTER_TO_INT (vtable->arg);
	int id = cat_id >> 16;
	cat_id &= 0xffff;
	switch (cat_id) {
	case CATEGORY_ASPNET:
		switch (id) {
		case COUNTER_ASPNET_REQ_Q: ptr = (glong*)&mono_perfcounters->aspnet_requests_queued; break;
		}
		break;
	}
	if (ptr) {
		if (do_incr) {
			/* FIXME: we need to do this atomically */
			*ptr += value;
			return *ptr;
		}
		/* this can be non-atomic */
		*ptr = value;
		return value;
	}
	return 0;
}

static void*
predef_writable_get_impl (int cat, MonoString* counter, MonoString* instance, int *type, MonoBoolean *custom)
{
	int i;
	const CategoryDesc *desc = &predef_categories [cat];
	*custom = TRUE;
	for (i = desc->first_counter; i < desc [1].first_counter; ++i) {
		const CounterDesc *cdesc = &predef_counters [i];
		if (mono_string_compare_ascii (counter, cdesc->name) == 0) {
			*type = cdesc->type;
			return create_vtable (GINT_TO_POINTER ((cdesc->id << 16) | cat), predef_writable_counter, predef_writable_update);
		}
	}
	return NULL;
}

static const CategoryDesc*
find_category (MonoString *category)
{
	int i;
	for (i = 0; i < NUM_CATEGORIES; ++i) {
		if (mono_string_compare_ascii (category, predef_categories [i].name) == 0)
			return &predef_categories [i];
	}
	return NULL;
}

void*
mono_perfcounter_get_impl (MonoString* category, MonoString* counter, MonoString* instance,
		MonoString* machine, int *type, MonoBoolean *custom)
{
	const CategoryDesc *cdesc;
	/* no support for counters on other machines */
	if (mono_string_compare_ascii (machine, "."))
		return NULL;
	cdesc = find_category (category);
	if (!cdesc)
		return NULL;
	switch (cdesc->id) {
	case CATEGORY_CPU:
		return cpu_get_impl (counter, instance, type, custom);
	case CATEGORY_PROC:
		return process_get_impl (counter, instance, type, custom);
	case CATEGORY_MONO_MEM:
		return mono_mem_get_impl (counter, instance, type, custom);
	case CATEGORY_ASPNET:
		return predef_writable_get_impl (cdesc->id, counter, instance, type, custom);
	}
	return NULL;
}

MonoBoolean
mono_perfcounter_get_sample (void *impl, MonoBoolean only_value, MonoCounterSample *sample)
{
	ImplVtable *vtable = impl;
	if (vtable && vtable->sample)
		return vtable->sample (vtable, only_value, sample);
	return FALSE;
}

gint64
mono_perfcounter_update_value (void *impl, MonoBoolean do_incr, gint64 value)
{
	ImplVtable *vtable = impl;
	if (vtable && vtable->update)
		return vtable->update (vtable, do_incr, value);
	return 0;
}

void
mono_perfcounter_free_data (void *impl)
{
	g_free (impl);
}

/* Category icalls */
MonoBoolean
mono_perfcounter_category_del (MonoString *name)
{
	return FALSE;
}

MonoString*
mono_perfcounter_category_help (MonoString *category, MonoString *machine)
{
	const CategoryDesc *cdesc;
	/* no support for counters on other machines */
	if (mono_string_compare_ascii (machine, "."))
		return NULL;
	cdesc = find_category (category);
	if (!cdesc)
		return NULL;
	return mono_string_new (mono_domain_get (), cdesc->help);
}

MonoBoolean
mono_perfcounter_category_exists (MonoString *counter, MonoString *category, MonoString *machine)
{
	int i;
	const CategoryDesc *cdesc;
	/* no support for counters on other machines */
	if (mono_string_compare_ascii (machine, "."))
		return FALSE;
	cdesc = find_category (category);
	if (!cdesc)
		return FALSE;
	/* counter is allowed to be null */
	if (!counter)
		return TRUE;
	for (i = cdesc->first_counter; i < cdesc [1].first_counter; ++i) {
		const CounterDesc *desc = &predef_counters [i];
		if (mono_string_compare_ascii (counter, desc->name) == 0)
			return TRUE;
	}
	return FALSE;
}

/*
 * Since we'll keep a copy of the category per-process, we should also make sure
 * categories with the same name are compatible.
 */
MonoBoolean
mono_perfcounter_create (MonoString *category, MonoString *help, int type, MonoArray *items)
{
	return FALSE;
}

int
mono_perfcounter_instance_exists (MonoString *instance, MonoString *category, MonoString *machine)
{
	const CategoryDesc *cdesc;
	/* no support for counters on other machines */
	if (mono_string_compare_ascii (machine, "."))
		return FALSE;
	cdesc = find_category (category);
	if (!cdesc)
		return FALSE;
	return FALSE;
}

MonoArray*
mono_perfcounter_category_names (MonoString *machine)
{
	int i;
	MonoArray *res;
	MonoDomain *domain = mono_domain_get ();
	/* no support for counters on other machines */
	if (mono_string_compare_ascii (machine, "."))
		return mono_array_new (domain, mono_get_string_class (), 0);
	res = mono_array_new (domain, mono_get_string_class (), NUM_CATEGORIES);
	for (i = 0; i < NUM_CATEGORIES; ++i) {
		const CategoryDesc *cdesc = &predef_categories [i];
		mono_array_setref (res, i, mono_string_new (domain, cdesc->name));
	}
	return res;
}

MonoArray*
mono_perfcounter_counter_names (MonoString *category, MonoString *machine)
{
	int i;
	const CategoryDesc *cdesc;
	MonoArray *res;
	MonoDomain *domain = mono_domain_get ();
	/* no support for counters on other machines */
	if (mono_string_compare_ascii (machine, ".") || !(cdesc = find_category (category)))
		return mono_array_new (domain, mono_get_string_class (), 0);
	res = mono_array_new (domain, mono_get_string_class (), cdesc [1].first_counter - cdesc->first_counter);
	for (i = cdesc->first_counter; i < cdesc [1].first_counter; ++i) {
		const CounterDesc *desc = &predef_counters [i];
		mono_array_setref (res, i - cdesc->first_counter, mono_string_new (domain, desc->name));
	}
	return res;
}

MonoArray*
mono_perfcounter_instance_names (MonoString *category, MonoString *machine)
{
	return mono_array_new (mono_domain_get (), mono_get_string_class (), 0);
}

