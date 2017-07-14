#include <config.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-proclib.h>
#include "log.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

typedef struct {
	const char *event_name;
	const int mask;
} NameAndMask;

static NameAndMask event_list[] = {
	{ "exception", PROFLOG_EXCEPTION_EVENTS },
	{ "monitor", PROFLOG_MONITOR_EVENTS },
	{ "gc", PROFLOG_GC_EVENTS },
	{ "gcalloc", PROFLOG_GC_ALLOCATION_EVENTS },
	{ "gcmove", PROFLOG_GC_MOVE_EVENTS },
	{ "gcroot", PROFLOG_GC_ROOT_EVENTS },
	{ "gchandle", PROFLOG_GC_HANDLE_EVENTS },
	{ "finalization", PROFLOG_FINALIZATION_EVENTS },
	{ "counter", PROFLOG_COUNTER_EVENTS },
	{ "jit", PROFLOG_JIT_EVENTS },

	{ "alloc", PROFLOG_ALLOC_ALIAS },
	{ "legacy", PROFLOG_LEGACY_ALIAS },
};

static void usage (void);
static void set_hsmode (ProfilerConfig *config, const char* val);
static void set_sample_freq (ProfilerConfig *config, const char *val);

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
	} else if (match_option (arg, "heapshot", &val)) {
		set_hsmode (config, val);
		if (config->hs_mode != MONO_PROFILER_HEAPSHOT_NONE)
			config->enable_mask |= PROFLOG_HEAPSHOT_ALIAS;
	} else if (match_option (arg, "heapshot-on-shutdown", NULL)) {
		config->hs_on_shutdown = TRUE;
		config->enable_mask |= PROFLOG_HEAPSHOT_ALIAS;
	} else if (match_option (arg, "sample", &val)) {
		set_sample_freq (config, val);
		config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_PROCESS;
		config->enable_mask |= PROFLOG_SAMPLE_EVENTS;
	} else if (match_option (arg, "sample-real", &val)) {
		set_sample_freq (config, val);
		config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_REAL;
		config->enable_mask |= PROFLOG_SAMPLE_EVENTS;
	} else if (match_option (arg, "calls", NULL)) {
		config->enter_leave = TRUE;
	} else if (match_option (arg, "coverage", NULL)) {
		config->collect_coverage = TRUE;
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

		if (i == G_N_ELEMENTS (event_list))
			mono_profiler_printf_err ("Could not parse argument: %s", arg);
	}
}

static void
load_args_from_env_or_default (ProfilerConfig *config)
{
	//XXX change this to header constants

	config->max_allocated_sample_hits = mono_cpu_count () * 1000;
	config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_NONE;
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
	if (!val) {
		config->hs_mode = MONO_PROFILER_HEAPSHOT_MAJOR;
		return;
	}

	if (strcmp (val, "ondemand") == 0) {
		config->hs_mode = MONO_PROFILER_HEAPSHOT_ON_DEMAND;
		return;
	}

	char *end;

	unsigned int count = strtoul (val, &end, 10);

	if (val == end) {
		usage ();
		return;
	}

	if (strcmp (end, "ms") == 0) {
		config->hs_mode = MONO_PROFILER_HEAPSHOT_X_MS;
		config->hs_freq_ms = count;
	} else if (strcmp (end, "gc") == 0) {
		config->hs_mode = MONO_PROFILER_HEAPSHOT_X_GC;
		config->hs_freq_gc = count;
	} else
		usage ();
}

static void
set_sample_freq (ProfilerConfig *config, const char *val)
{
	if (!val)
		return;

	char *end;

	int freq = strtoul (val, &end, 10);

	if (val == end) {
		usage ();
		return;
	}

	config->sample_freq = freq;
}

static void
usage (void)
{
	mono_profiler_printf ("Mono log profiler version %d.%d (format: %d)", LOG_VERSION_MAJOR, LOG_VERSION_MINOR, LOG_DATA_VERSION);
	mono_profiler_printf ("Usage: mono --profile=log[:OPTION1[,OPTION2...]] program.exe\n");
	mono_profiler_printf ("Options:");
	mono_profiler_printf ("\thelp                 show this usage info");
	mono_profiler_printf ("\t[no]'EVENT'          enable/disable an individual profiling event");
	mono_profiler_printf ("\t                     valid EVENT values:");

	for (int i = 0; i < G_N_ELEMENTS (event_list); i++)
		mono_profiler_printf ("\t                         %s", event_list [i].event_name);

	mono_profiler_printf ("\t[no]alloc            enable/disable recording allocation info");
	mono_profiler_printf ("\t[no]legacy           enable/disable pre mono 5.4 default profiler events");
	mono_profiler_printf ("\tsample[-real][=FREQ] enable/disable statistical sampling of threads");
	mono_profiler_printf ("\t                     FREQ in Hz, 100 by default");
	mono_profiler_printf ("\t                     the -real variant uses wall clock time instead of process time");
	mono_profiler_printf ("\theapshot[=MODE]      record heapshot info (by default at each major collection)");
	mono_profiler_printf ("\t                     MODE: every XXms milliseconds, every YYgc collections, ondemand");
	mono_profiler_printf ("\theapshot-on-shutdown do a heapshot on runtime shutdown");
	mono_profiler_printf ("\t                     this option is independent of the above option");
	mono_profiler_printf ("\tcalls                enable recording enter/leave method events (very heavy)");
	mono_profiler_printf ("\tcoverage             enable collection of code coverage data");
	mono_profiler_printf ("\tcovfilter=ASSEMBLY   add ASSEMBLY to the code coverage filters");
	mono_profiler_printf ("\t                     prefix a + to include the assembly or a - to exclude it");
	mono_profiler_printf ("\t                     e.g. covfilter=-mscorlib");
	mono_profiler_printf ("\tcovfilter-file=FILE  use FILE to generate the list of assemblies to be filtered");
	mono_profiler_printf ("\tmaxframes=NUM        collect up to NUM stack frames");
	mono_profiler_printf ("\tcalldepth=NUM        ignore method events for call chain depth bigger than NUM");
	mono_profiler_printf ("\toutput=FILENAME      write the data to file FILENAME (the file is always overwritten)");
	mono_profiler_printf ("\toutput=+FILENAME     write the data to file FILENAME.pid (the file is always overwritten)");
	mono_profiler_printf ("\toutput=|PROGRAM      write the data to the stdin of PROGRAM");
	mono_profiler_printf ("\t                     %%t is substituted with date and time, %%p with the pid");
	mono_profiler_printf ("\treport               create a report instead of writing the raw data to a file");
	mono_profiler_printf ("\tzip                  compress the output data");
	mono_profiler_printf ("\tport=PORTNUM         use PORTNUM for the listening command server");
}
