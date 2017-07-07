#include <config.h>

#include "log.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef HAVE_SCHED_GETAFFINITY
#include <sched.h>

#  ifndef GLIBC_HAS_CPU_COUNT
static int
CPU_COUNT(cpu_set_t *set)
{
	int i, count = 0;

	for (int i = 0; i < CPU_SETSIZE; i++)
		if (CPU_ISSET(i, set))
			count++;
	return count;
}
#  endif
#endif

typedef struct {
	const char *event_name;
	const int mask;
} NameAndMask;

static NameAndMask event_list[] = {
	{ "domain", PROFLOG_DOMAIN_EVENTS },
	{ "assembly", PROFLOG_ASSEMBLY_EVENTS },
	{ "module", PROFLOG_MODULE_EVENTS },
	{ "class", PROFLOG_CLASS_EVENTS },
	{ "jit", PROFLOG_JIT_COMPILATION_EVENTS },
	{ "exception", PROFLOG_EXCEPTION_EVENTS },
	{ "gcalloc", PROFLOG_ALLOCATION_EVENTS },
	{ "gc", PROFLOG_GC_EVENTS },
	{ "thread", PROFLOG_THREAD_EVENTS },
	{ "calls", PROFLOG_CALL_EVENTS },
	//{ "inscov", PROFLOG_INS_COVERAGE_EVENTS }, //this is a profiler API event, but there's no actual event for us to emit here
	//{ "sampling", PROFLOG_SAMPLING_EVENTS }, //it makes no sense to enable/disable this event by itself
	{ "monitor", PROFLOG_MONITOR_EVENTS },
	{ "gcmove", PROFLOG_GC_MOVES_EVENTS },
	{ "gcroot", PROFLOG_GC_ROOT_EVENTS },
	{ "context", PROFLOG_CONTEXT_EVENTS },
	{ "finalization", PROFLOG_FINALIZATION_EVENTS },
	{ "counter", PROFLOG_COUNTER_EVENTS },
	{ "gchandle", PROFLOG_GC_HANDLE_EVENTS },

	{ "typesystem", PROFLOG_TYPELOADING_ALIAS },
	{ "coverage", PROFLOG_CODECOV_ALIAS },
	//{ "sample", PROFLOG_PERF_SAMPLING_ALIAS }, //takes args, explicitly handles
	{ "alloc", PROFLOG_GC_ALLOC_ALIAS },
	//{ "heapshot", PROFLOG_HEAPSHOT_ALIAS }, //takes args, explicitly handled
	{ "legacy", PROFLOG_LEGACY_ALIAS },
};

static void usage (void);
static void set_hsmode (ProfilerConfig *config, const char* val);
static void set_sample_freq (ProfilerConfig *config, const char *val);
static int mono_cpu_count (void);


static gboolean
match_option (const char *arg, const char *opt_name, const char **rval)
{
	if (rval) {
		const char *end = strchr (arg, '=');

		*rval = NULL;
		if (!end)
			return !strcmp (arg, opt_name);

		if (strncmp (arg, opt_name, strlen (opt_name)) || (end - arg) > strlen (opt_name) + 1)
			return FALSE;
		*rval = end + 1;
		return TRUE;
	} else {
		//FIXME how should we handle passing a value to an arg that doesn't expect it?
		return !strcmp (arg, opt_name);
	}
}

static void
parse_arg (const char *arg, ProfilerConfig *config)
{
	const char *val;

	if (match_option (arg, "help", NULL)) {
		usage ();
	} else if (match_option (arg, "report", NULL)) {
		config->do_report = TRUE;
	} else if (match_option (arg, "debug", NULL)) {
		config->do_debug = TRUE;
	} else if (match_option (arg, "debug-coverage", NULL)) {
		config->debug_coverage = TRUE;
	} else if (match_option (arg, "sampling-real", NULL)) {
		config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_REAL;
	} else if (match_option (arg, "sampling-process", NULL)) {
		config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_PROCESS;
	} else if (match_option (arg, "heapshot", &val)) {
		config->enable_mask |= PROFLOG_HEAPSHOT_ALIAS;
		set_hsmode (config, val);
	} else if (match_option (arg, "sample", &val)) {
		set_sample_freq (config, val);
		if (config->sample_freq)
			config->enable_mask |= PROFLOG_PERF_SAMPLING_ALIAS;
	} else if (match_option (arg, "zip", NULL)) {
		config->use_zip = TRUE;
	} else if (match_option (arg, "output", &val)) {
		config->output_filename = g_strdup (val);
	} else if (match_option (arg, "port", &val)) {
		char *end;
		config->command_port = strtoul (val, &end, 10);
	} else if (match_option (arg, "maxframes", &val)) {
		char *end;
		int num_frames = strtoul (val, &end, 10);
		if (num_frames > MAX_FRAMES)
			num_frames = MAX_FRAMES;
		config->notraces = num_frames == 0;
		config->num_frames = num_frames;
	} else if (match_option (arg, "maxsamples", &val)) {
		char *end;
		int max_samples = strtoul (val, &end, 10);
		if (max_samples)
			config->max_allocated_sample_hits = max_samples;
	} else if (match_option (arg, "calldepth", &val)) {
		char *end;
		config->max_call_depth = strtoul (val, &end, 10);
	} else if (match_option (arg, "covfilter-file", &val)) {
		if (config->cov_filter_files == NULL)
			config->cov_filter_files = g_ptr_array_new ();
		g_ptr_array_add (config->cov_filter_files, g_strdup (val));
	} else if (match_option (arg, "onlycoverage", NULL)) {
		config->only_coverage = TRUE;
	} else {
		int i;

		for (i = 0; i < G_N_ELEMENTS (event_list); ++i){
			if (!strcmp (arg, event_list [i].event_name)) {
				config->enable_mask |= event_list [i].mask;
				break;
			} else if (!strncmp (arg, "no", 2) && !strcmp (arg + 2, event_list [i].event_name)) {
				config->disable_mask |= event_list [i].mask;
				break;
			}
		}
		if (i == G_N_ELEMENTS (event_list)) {
			printf ("Could not parse argument %s\n", arg);
		}
	}
}

static void
load_args_from_env_or_default (ProfilerConfig *config)
{
	//XXX change this to header constants

	config->max_allocated_sample_hits = mono_cpu_count () * 1000;
	config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_PROCESS;
	config->sample_freq = 100;
	config->max_call_depth = 100;
	config->num_frames = MAX_FRAMES;
}


void
proflog_parse_args (ProfilerConfig *config, const char *desc)
{
	const char *p;
	gboolean in_quotes = FALSE;
	char quote_char = '\0';
	char *buffer = malloc (strlen (desc));
	int buffer_pos = 0;

	load_args_from_env_or_default (config);

	for (p = desc; *p; p++){
		switch (*p){
		case ',':
			if (!in_quotes) {
				if (buffer_pos != 0){
					buffer [buffer_pos] = 0;
					parse_arg (buffer, config);
					buffer_pos = 0;
				}
			} else {
				buffer [buffer_pos++] = *p;
			}
			break;

		case '\\':
			if (p [1]) {
				buffer [buffer_pos++] = p[1];
				p++;
			}
			break;
		case '\'':
		case '"':
			if (in_quotes) {
				if (quote_char == *p)
					in_quotes = FALSE;
				else
					buffer [buffer_pos++] = *p;
			} else {
				in_quotes = TRUE;
				quote_char = *p;
			}
			break;
		default:
			buffer [buffer_pos++] = *p;
			break;
		}
	}
		
	if (buffer_pos != 0) {
		buffer [buffer_pos] = 0;
		parse_arg (buffer, config);
	}

	g_free (buffer);

	//Compure config effective mask
	config->effective_mask = config->enable_mask & ~config->disable_mask;
}

static void
set_hsmode (ProfilerConfig *config, const char* val)
{
	char *end;
	unsigned int count;
	if (!val)
		return;
	if (strcmp (val, "ondemand") == 0) {
		config->hs_mode_ondemand = TRUE;
		return;
	}

	count = strtoul (val, &end, 10);
	if (val == end) {
		usage ();
		return;
	}

	if (strcmp (end, "ms") == 0)
		config->hs_mode_ms = count;
	else if (strcmp (end, "gc") == 0)
		config->hs_mode_gc = count;
	else
		usage ();
}

/*
Sampling frequency allows for one undocumented, hidden and ignored argument. The sampling kind.
Back in the day when this was done using perf, we could specify one of: cycles,instr,cacherefs,cachemiss,branches,branchmiss
With us moving ot userland sampling, those options are now meaningless.
*/
static void
set_sample_freq (ProfilerConfig *config, const char *val)
{
	if (!val)
		return;

	const char *p = val;

	// Is it only the frequency (new option style)?
	if (isdigit (*p))
		goto parse;

	// Skip the sample type for backwards compatibility.
	while (isalpha (*p))
		p++;

	// Skip the forward slash only if we got a sample type.
	if (p != val && *p == '/') {
		p++;

		char *end;

	parse:
		config->sample_freq = strtoul (p, &end, 10);

		if (p == end) {
			usage ();
			return;	
		}

		p = end;
	}

	if (*p)
		usage ();
}

static void
usage (void)
{
	printf ("Log profiler version %d.%d (format: %d)\n", LOG_VERSION_MAJOR, LOG_VERSION_MINOR, LOG_DATA_VERSION);
	printf ("Usage: mono --profile=log[:OPTION1[,OPTION2...]] program.exe\n");
	printf ("Options:\n");
	printf ("\thelp                 show this usage info\n");
	printf ("\t[no]'event'          enable/disable a profiling event. Valid values: domain, assembly, module, class, jit, exception, gcalloc, gc, thread, monitor, gcmove, gcroot, context, finalization, counter, gchandle\n");
	printf ("\t[no]typesystem       enable/disable typesystem related events such as class and assembly loading\n");
	printf ("\t[no]alloc            enable/disable recording allocation info\n");
	printf ("\t[no]calls            enable/disable recording enter/leave method events\n");
	printf ("\t[no]legacy           enable/disable pre mono 5.4 default profiler events\n");
	printf ("\tsample[=frequency]   enable/disable statistical sampling of threads (frequency in Hz, 100 by default)\n");
	printf ("\theapshot[=MODE]      record heap shot info (by default at each major collection)\n");
	printf ("\t                     MODE: every XXms milliseconds, every YYgc collections, ondemand\n");
	printf ("\t[no]coverage         enable collection of code coverage data\n");
	printf ("\tcovfilter=ASSEMBLY   add an assembly to the code coverage filters\n");
	printf ("\t                     add a + to include the assembly or a - to exclude it\n");
	printf ("\t                     covfilter=-mscorlib\n");
	printf ("\tcovfilter-file=FILE  use FILE to generate the list of assemblies to be filtered\n");
	printf ("\tmaxframes=NUM        collect up to NUM stack frames\n");
	printf ("\tcalldepth=NUM        ignore method events for call chain depth bigger than NUM\n");
	printf ("\toutput=FILENAME      write the data to file FILENAME (The file is always overwriten)\n");
	printf ("\toutput=+FILENAME     write the data to file FILENAME.pid (The file is always overwriten)\n");
	printf ("\toutput=|PROGRAM      write the data to the stdin of PROGRAM\n");
	printf ("\t                     %%t is subtituted with date and time, %%p with the pid\n");
	printf ("\treport               create a report instead of writing the raw data to a file\n");
	printf ("\tzip                  compress the output data\n");
	printf ("\tport=PORTNUM         use PORTNUM for the listening command server\n");
}

static int
mono_cpu_count (void)
{
#ifdef PLATFORM_ANDROID
	/* Android tries really hard to save power by powering off CPUs on SMP phones which
	 * means the normal way to query cpu count returns a wrong value with userspace API.
	 * Instead we use /sys entries to query the actual hardware CPU count.
	 */
	int count = 0;
	char buffer[8] = {'\0'};
	int present = open ("/sys/devices/system/cpu/present", O_RDONLY);
	/* Format of the /sys entry is a cpulist of indexes which in the case
	 * of present is always of the form "0-(n-1)" when there is more than
	 * 1 core, n being the number of CPU cores in the system. Otherwise
	 * the value is simply 0
	 */
	if (present != -1 && read (present, (char*)buffer, sizeof (buffer)) > 3)
		count = strtol (((char*)buffer) + 2, NULL, 10);
	if (present != -1)
		close (present);
	if (count > 0)
		return count + 1;
#endif

#if defined(HOST_ARM) || defined (HOST_ARM64)

	/* ARM platforms tries really hard to save power by powering off CPUs on SMP phones which
	 * means the normal way to query cpu count returns a wrong value with userspace API. */

#ifdef _SC_NPROCESSORS_CONF
	{
		int count = sysconf (_SC_NPROCESSORS_CONF);
		if (count > 0)
			return count;
	}
#endif

#else

#ifdef HAVE_SCHED_GETAFFINITY
	{
		cpu_set_t set;
		if (sched_getaffinity (getpid (), sizeof (set), &set) == 0)
			return CPU_COUNT (&set);
	}
#endif
#ifdef _SC_NPROCESSORS_ONLN
	{
		int count = sysconf (_SC_NPROCESSORS_ONLN);
		if (count > 0)
			return count;
	}
#endif

#endif /* defined(HOST_ARM) || defined (HOST_ARM64) */

#ifdef USE_SYSCTL
	{
		int count;
		int mib [2];
		size_t len = sizeof (int);
		mib [0] = CTL_HW;
		mib [1] = HW_NCPU;
		if (sysctl (mib, 2, &count, &len, NULL, 0) == 0)
			return count;
	}
#endif
#ifdef HOST_WIN32
	{
		SYSTEM_INFO info;
		GetSystemInfo (&info);
		return info.dwNumberOfProcessors;
	}
#endif

	static gboolean warned;

	if (!warned) {
		g_warning ("Don't know how to determine CPU count on this platform; assuming 1");
		warned = TRUE;
	}

	return 1;
}
