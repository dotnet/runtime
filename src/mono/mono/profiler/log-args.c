#include <config.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/w32subset.h>
#include "log.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

typedef struct {
	const char *event_name;
	const int mask;
	const int compat_mask;
} NameAndMask;

static NameAndMask event_list[] = {
	{ "exception", PROFLOG_EXCEPTION_EVENTS },
	{ "monitor", PROFLOG_MONITOR_EVENTS },
	{ "gc", PROFLOG_GC_EVENTS },
	{ "gcalloc", PROFLOG_GC_ALLOCATION_EVENTS },
	{ "gcmove", PROFLOG_GC_MOVE_EVENTS },
	{ "gcroot", PROFLOG_GC_ROOT_EVENTS },
	{ "gchandle", PROFLOG_GC_HANDLE_EVENTS },
	{ "finalization", PROFLOG_GC_FINALIZATION_EVENTS },
	{ "counter", PROFLOG_COUNTER_EVENTS },
	{ "jit", PROFLOG_JIT_EVENTS },

	{ "counters", PROFLOG_COUNTER_EVENTS },
	{ "alloc", PROFLOG_ALLOC_ALIAS, PROFLOG_ALLOC_ALIAS | PROFLOG_GC_ROOT_EVENTS },
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

static gboolean compat_args_parsing;

static void
parse_arg (const char *arg, ProfilerConfig *config)
{
	const char *val;

	static gboolean first_processed;

	if (!first_processed) {
		first_processed = TRUE;
		if (match_option (arg, "nodefaults", NULL)) {
			//enables new style of default events, IE, nothing.
			return;
		} else {
			config->enable_mask = PROFLOG_EXCEPTION_EVENTS | PROFLOG_COUNTER_EVENTS;
			config->always_do_root_report = TRUE;
			compat_args_parsing = TRUE;
		}
	}

	if (match_option (arg, "help", NULL)) {
		usage ();
	} else if (match_option (arg, "nodefaults", NULL)) {
		mono_profiler_printf_err ("The nodefaults option can only be used as the first argument.");
	} else if (match_option (arg, "report", NULL)) {
#if HAVE_API_SUPPORT_WIN32_PIPE_OPEN_CLOSE && !defined (HOST_WIN32)
		config->do_report = TRUE;
#else
		mono_profiler_printf_err ("'report' argument not supported on platform.");
#endif
	} else if (match_option (arg, "debug", NULL)) {
		config->do_debug = TRUE;
	} else if (match_option (arg, "heapshot", &val)) {
		set_hsmode (config, val);
		if (config->hs_mode != MONO_PROFILER_HEAPSHOT_NONE) {
			config->enable_mask |= PROFLOG_HEAPSHOT_ALIAS;
			if (compat_args_parsing)
				config->enable_mask |= PROFLOG_GC_MOVE_EVENTS;
		}
	} else if (match_option (arg, "heapshot-on-shutdown", NULL)) {
		config->hs_on_shutdown = TRUE;
		config->enable_mask |= PROFLOG_HEAPSHOT_ALIAS;
	} else if (match_option (arg, "sample", &val)) {
		set_sample_freq (config, val);
		config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_PROCESS;
		config->enable_mask |= PROFLOG_SAMPLE_EVENTS;
	} else if (match_option (arg, "sample-real", &val) || (compat_args_parsing && match_option (arg, "sampling-real", &val))) {
		set_sample_freq (config, val);
		config->sampling_mode = MONO_PROFILER_SAMPLE_MODE_REAL;
		config->enable_mask |= PROFLOG_SAMPLE_EVENTS;
	} else if (match_option (arg, "calls", NULL)) {
		config->enter_leave = TRUE;
	} else if (match_option (arg, "nocalls", NULL)) {
		if (!compat_args_parsing)
			mono_profiler_printf_err ("Could not parse argument '%s'", arg);
	} else if (match_option (arg, "coverage", NULL)) {
		mono_profiler_printf_err ("The log profiler no longer supports code coverage. Please use the dedicated coverage profiler instead. See mono-profilers(1) for more information.");
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
	} else if (match_option (arg, "callspec", &val)) {
		if (!val)
			val = "";
		if (val[0] == '\"')
			++val;
		char *spec = g_strdup (val);
		size_t speclen = strlen (val);
		if (speclen > 0 && spec[speclen - 1] == '\"')
			spec[speclen - 1] = '\0';
		char *errstr;
		if (!mono_callspec_parse (spec, &config->callspec, &errstr)) {
			mono_profiler_printf_err ("Could not parse callspec '%s': %s", spec, errstr);
			g_free (errstr);
			mono_callspec_cleanup (&config->callspec);
		}
		g_free (spec);
	} else {
		int i;

		for (i = 0; i < G_N_ELEMENTS (event_list); ++i){
			int mask = event_list [i].mask;
			if (compat_args_parsing && event_list [i].compat_mask)
				mask = event_list [i].compat_mask;
			if (!strcmp (arg, event_list [i].event_name)) {
				config->enable_mask |= mask;
				break;
			} else if (!strncmp (arg, "no", 2) && !strcmp (arg + 2, event_list [i].event_name)) {
				config->disable_mask |= mask;
				break;
			}
		}

		if (i == G_N_ELEMENTS (event_list))
			mono_profiler_printf_err ("Could not parse argument '%s'", arg);
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
	char *buffer = g_malloc (strlen (desc) + 1);
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
		mono_profiler_printf_err ("Could not parse heapshot mode");
		return;
	}

	if (strcmp (end, "ms") == 0) {
		config->hs_mode = MONO_PROFILER_HEAPSHOT_X_MS;
		config->hs_freq_ms = count;
	} else if (strcmp (end, "gc") == 0) {
		config->hs_mode = MONO_PROFILER_HEAPSHOT_X_GC;
		config->hs_freq_gc = count;
	} else
		mono_profiler_printf_err ("Could not parse heapshot mode");
}

static void
set_sample_freq (ProfilerConfig *config, const char *val)
{
	int freq;

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
		freq = strtoul (p, &end, 10);

		if (p == end)
			mono_profiler_printf_err ("Could not parse sample frequency");
		else
			config->sample_freq = freq;
	}
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

	mono_profiler_printf ("\tnodefaults           disable legacy rules for enabling extra events");
	mono_profiler_printf ("\t[no]alloc            enable/disable recording allocation info");
	mono_profiler_printf ("\t[no]legacy           enable/disable pre Mono 5.6 default profiler events");
	mono_profiler_printf ("\tsample[-real][=FREQ] enable/disable statistical sampling of threads");
	mono_profiler_printf ("\t                     FREQ in Hz, 100 by default");
	mono_profiler_printf ("\t                     the -real variant uses wall clock time instead of process time");
	mono_profiler_printf ("\theapshot[=MODE]      record heapshot info (by default at each major collection)");
	mono_profiler_printf ("\t                     MODE: every XXms milliseconds, every YYgc collections, ondemand");
	mono_profiler_printf ("\theapshot-on-shutdown do a heapshot on runtime shutdown");
	mono_profiler_printf ("\t                     this option is independent of the above option");
	mono_profiler_printf ("\tcalls                enable recording enter/leave method events (very heavy)");
	mono_profiler_printf ("\tmaxframes=NUM        collect up to NUM stack frames");
	mono_profiler_printf ("\tcalldepth=NUM        ignore method events for call chain depth bigger than NUM");
	mono_profiler_printf ("\toutput=FILENAME      write the data to file FILENAME (the file is always overwritten)");
	mono_profiler_printf ("\toutput=+FILENAME     write the data to file FILENAME.pid (the file is always overwritten)");
	mono_profiler_printf ("\toutput=|PROGRAM      write the data to the stdin of PROGRAM");
	mono_profiler_printf ("\t                     %%t is substituted with date and time, %%p with the pid");
	mono_profiler_printf ("\treport               create a report instead of writing the raw data to a file");
	mono_profiler_printf ("\tzip                  compress the output data");
	mono_profiler_printf ("\tport=PORTNUM         use PORTNUM for the listening command server");

	exit (0);
}
