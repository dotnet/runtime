/*
 * driver.c: The new mono JIT compiler.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002-2003 Ximian, Inc.
 * (C) 2003-2004 Novell, Inc.
 */

#include <config.h>
#include <signal.h>
#include <unistd.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/io-layer/io-layer.h>
#include "mono/metadata/profiler.h"
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/os/gc_wrapper.h>

#include "mini.h"
#include "jit.h"
#include <string.h>
#include <ctype.h>
#include "inssel.h"
#include <locale.h>

static FILE *mini_stats_fd = NULL;

static void mini_usage (void);

typedef void (*OptFunc) (const char *p);

/* keep in sync with enum in mini.h */
typedef struct {
	const char* name;
	const char* desc;
	const OptFunc func;
} OptName;

static const OptName 
opt_names [] = {
	{"peephole", "Peephole postpass"},
	{"branch",   "Branch optimizations"},
	{"inline",   "Inline method calls"},
	{"cfold",    "Constant folding"},
	{"consprop", "Constant propagation"},
	{"copyprop", "Copy propagation"},
	{"deadce",   "Dead code elimination"},
	{"linears",  "Linear scan global reg allocation"},
	{"cmov",     "Conditional moves"},
	{"shared",   "Emit per-domain code"},
	{"sched",    "Instruction scheduling"},
	{"intrins",  "Intrinsic method implementations"},
	{"tailc",    "Tail recursion and tail calls"},
	{"loop",     "Loop related optimizations"},
	{"fcmov",    "Fast x86 FP compares"},
	{"leaf",     "Leaf procedures optimizations"},
	{"aot",      "Usage of Ahead Of Time compiled code"},
	{"precomp",  "Precompile all methods before executing Main"},
	{"abcrem",   "Array bound checks removal"}
};

#define DEFAULT_OPTIMIZATIONS (	\
	MONO_OPT_PEEPHOLE |	\
	MONO_OPT_CFOLD |	\
	MONO_OPT_BRANCH |	\
	MONO_OPT_LINEARS |	\
	MONO_OPT_INTRINS |  \
	MONO_OPT_LOOP |  \
    MONO_OPT_AOT)

#define EXCLUDED_FROM_ALL (MONO_OPT_SHARED | MONO_OPT_PRECOMP)

static guint32
parse_optimizations (const char* p)
{
	/* the default value */
	guint32 opt = DEFAULT_OPTIMIZATIONS;
	guint32 exclude = 0;
	const char *n;
	int i, invert, len;

	/* call out to cpu detection code here that sets the defaults ... */
	opt |= mono_arch_cpu_optimizazions (&exclude);
	opt &= ~exclude;
	if (!p)
		return opt;

	while (*p) {
		if (*p == '-') {
			p++;
			invert = TRUE;
		} else {
			invert = FALSE;
		}
		for (i = 0; i < G_N_ELEMENTS (opt_names); ++i) {
			n = opt_names [i].name;
			len = strlen (n);
			if (strncmp (p, n, len) == 0) {
				if (invert)
					opt &= ~ (1 << i);
				else
					opt |= 1 << i;
				p += len;
				if (*p == ',') {
					p++;
					break;
				} else if (*p == '=') {
					p++;
					if (opt_names [i].func)
						opt_names [i].func (p);
					while (*p && *p++ != ',');
					break;
				}
				/* error out */
				break;
			}
		}
		if (i == G_N_ELEMENTS (opt_names)) {
			if (strncmp (p, "all", 3) == 0) {
				if (invert)
					opt = 0;
				else
					opt = ~(EXCLUDED_FROM_ALL | exclude);
				p += 3;
				if (*p == ',')
					p++;
			} else {
				fprintf (stderr, "Invalid optimization name `%s'\n", p);
				exit (1);
			}
		}
	}
	return opt;
}

typedef struct {
	const char* name;
	const char* desc;
	MonoGraphOptions value;
} GraphName;

static const GraphName 
graph_names [] = {
	{"cfg",      "Control Flow Graph (CFG)" ,               MONO_GRAPH_CFG},
	{"dtree",    "Dominator Tree",                          MONO_GRAPH_DTREE},
	{"code",     "CFG showing code",                        MONO_GRAPH_CFG_CODE},
	{"ssa",      "CFG showing code after SSA translation",  MONO_GRAPH_CFG_SSA},
	{"optcode",  "CFG showing code after IR optimizations", MONO_GRAPH_CFG_OPTCODE}
};

static MonoGraphOptions
mono_parse_graph_options (const char* p)
{
	const char *n;
	int i, len;

	for (i = 0; i < G_N_ELEMENTS (graph_names); ++i) {
		n = graph_names [i].name;
		len = strlen (n);
		if (strncmp (p, n, len) == 0)
			return graph_names [i].value;
	}

	fprintf (stderr, "Invalid graph name provided: %s\n", p);
	exit (1);
}

int
mono_parse_default_optimizations (const char* p)
{
	guint32 opt;

	opt = parse_optimizations (p);
	return opt;
}

static char*
opt_descr (guint32 flags) {
	GString *str = g_string_new ("");
	int i, need_comma;

	need_comma = 0;
	for (i = 0; i < G_N_ELEMENTS (opt_names); ++i) {
		if (flags & (1 << i)) {
			if (need_comma)
				g_string_append_c (str, ',');
			g_string_append (str, opt_names [i].name);
			need_comma = 1;
		}
	}
	return g_string_free (str, FALSE);
}

static const guint32
opt_sets [] = {
       0,
       MONO_OPT_PEEPHOLE,
       MONO_OPT_BRANCH,
       MONO_OPT_CFOLD,
       MONO_OPT_FCMOV,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_INTRINS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_CFOLD,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_ABCREM,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_ABCREM | MONO_OPT_SHARED
};

typedef int (*TestMethod) (void);

#if 0
static void
domain_dump_native_code (MonoDomain *domain) {
	// need to poke into the domain, move to metadata/domain.c
	// need to empty jit_info_table and code_mp
}
#endif

static int
mini_regression (MonoImage *image, int verbose, int *total_run) {
	guint32 i, opt, opt_flags;
	MonoMethod *method;
	MonoCompile *cfg;
	char *n;
	int result, expected, failed, cfailed, run, code_size, total;
	TestMethod func;
	GTimer *timer = g_timer_new ();

	if (mini_stats_fd) {
		fprintf (mini_stats_fd, "$stattitle = \'Mono Benchmark Results (various optimizations)\';\n");

		fprintf (mini_stats_fd, "$graph->set_legend(qw(");
		for (opt = 0; opt < G_N_ELEMENTS (opt_sets); opt++) {
			opt_flags = opt_sets [opt];
			n = opt_descr (opt_flags);
			if (!n [0])
				n = (char *)"none";
			if (opt)
				fprintf (mini_stats_fd, " ");
			fprintf (mini_stats_fd, "%s", n);
		

		}
		fprintf (mini_stats_fd, "));\n");

		fprintf (mini_stats_fd, "@data = (\n");
		fprintf (mini_stats_fd, "[");
	}

	/* load the metadata */
	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
       	        method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
		mono_class_init (method->klass);

		if (!strncmp (method->name, "test_", 5) && mini_stats_fd) {
			fprintf (mini_stats_fd, "\"%s\",", method->name);
		}
	}
	if (mini_stats_fd)
		fprintf (mini_stats_fd, "],\n");


	total = 0;
	*total_run = 0;
	for (opt = 0; opt < G_N_ELEMENTS (opt_sets); ++opt) {
		double elapsed, comp_time, start_time;
		MonoJitInfo *jinfo;

		opt_flags = opt_sets [opt];
		mono_set_defaults (verbose, opt_flags);
		n = opt_descr (opt_flags);
		g_print ("Test run: image=%s, opts=%s\n", mono_image_get_filename (image), n);
		g_free (n);
		cfailed = failed = run = code_size = 0;
		comp_time = elapsed = 0.0;

		/* fixme: ugly hack - delete all previously compiled methods */
		for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
			method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
			method->info = NULL;
		}

		g_timer_start (timer);
		if (mini_stats_fd)
			fprintf (mini_stats_fd, "[");
		for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
        	        method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
			if (strncmp (method->name, "test_", 5) == 0) {
				expected = atoi (method->name + 5);
				run++;
				start_time = g_timer_elapsed (timer, NULL);
				comp_time -= start_time; 
				cfg = mini_method_compile (method, opt_flags, mono_get_root_domain (), TRUE, 0);
				comp_time += g_timer_elapsed (timer, NULL);
				if (cfg) {
					if (verbose >= 2)
						g_print ("Running '%s' ...\n", method->name);
#ifdef MONO_USE_AOT_COMPILER
					if ((jinfo = mono_aot_get_method (mono_get_root_domain (), method)))
						func = jinfo->code_start;
					else
#endif
						func = (TestMethod)cfg->native_code;
					result = func ();
					if (result != expected) {
						failed++;
						if (verbose)
							g_print ("Test '%s' failed result (got %d, expected %d).\n", method->name, result, expected);
					}
					code_size += cfg->code_len;
					mono_destroy_compile (cfg);

				} else {
					cfailed++;
					if (verbose)
						g_print ("Test '%s' failed compilation.\n", method->name);
				}
				if (mini_stats_fd)
					fprintf (mini_stats_fd, "%f, ", 
						 g_timer_elapsed (timer, NULL) - start_time);
			}
		}
		if (mini_stats_fd)
			fprintf (mini_stats_fd, "],\n");
		g_timer_stop (timer);
		elapsed = g_timer_elapsed (timer, NULL);
		g_print ("Results: total tests: %d, failed: %d, cfailed: %d (pass: %.2f%%)\n", 
			run, failed, cfailed, 100.0*(run-failed-cfailed)/run);
		g_print ("Elapsed time: %f secs (%f, %f), Code size: %d\n\n", elapsed, 
			 elapsed - comp_time, comp_time, code_size);
		total += failed + cfailed;
		*total_run += run;
	}

	if (mini_stats_fd) {
		fprintf (mini_stats_fd, ");\n");
		fflush (mini_stats_fd);
	}

	g_timer_destroy (timer);
	return total;
}

static int
mini_regression_list (int verbose, int count, char *images [])
{
	int i, total, total_run, run;
	MonoAssembly *ass;
	
	total_run =  total = 0;
	for (i = 0; i < count; ++i) {
		ass = mono_assembly_open (images [i], NULL);
		if (!ass) {
			g_warning ("failed to load assembly: %s", images [i]);
			continue;
		}
		total += mini_regression (mono_assembly_get_image (ass), verbose, &run);
		total_run += run;
	}
	g_print ("Overall results: tests: %d, failed: %d, opt combinations: %d (pass: %.2f%%)\n", 
		total_run, total, (int)G_N_ELEMENTS (opt_sets), 100.0*(total_run-total)/total_run);
	return total;
}

enum {
	DO_BENCH,
	DO_REGRESSION,
	DO_COMPILE,
	DO_EXEC,
	DO_DRAW
};

typedef struct CompileAllThreadArgs {
	MonoAssembly *ass;
	int verbose;
} CompileAllThreadArgs;

static void
compile_all_methods_thread_main (CompileAllThreadArgs *args)
{
	MonoAssembly *ass = args->ass;
	int verbose = args->verbose;
	MonoImage *image = mono_assembly_get_image (ass);
	MonoMethod *method;
	int i, count = 0;

	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT)
			continue;
		if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
			continue;

		count++;
		if (verbose) {
			char * desc = mono_method_full_name (method, TRUE);
			g_print ("Compiling %d %s\n", count, desc);
			g_free (desc);
		}
		mono_compile_method (method);
	}

}

static void
compile_all_methods (MonoAssembly *ass, int verbose)
{
	CompileAllThreadArgs args;

	args.ass = ass;
	args.verbose = verbose;

	/* 
	 * Need to create a mono thread since compilation might trigger
	 * running of managed code.
	 */
	mono_thread_create (mono_domain_get (), compile_all_methods_thread_main, &args);

	mono_thread_manage ();
}

/**
 * mono_jit_exec:
 * @assembly: reference to an assembly
 * @argc: argument count
 * @argv: argument vector
 *
 * Start execution of a program.
 */
int 
mono_jit_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	MonoImage *image = mono_assembly_get_image (assembly);
	MonoMethod *method;
	guint32 entry = mono_image_get_entry_point (image);

	if (!entry) {
		g_print ("Assembly '%s' doesn't have an entry point.\n", mono_image_get_filename (image));
		/* FIXME: remove this silly requirement. */
		mono_environment_exitcode_set (1);
		return 1;
	}

	method = mono_get_method (image, entry, NULL);

	return mono_runtime_run_main (method, argc, argv, NULL);
}

typedef struct 
{
	MonoDomain *domain;
	const char *file;
	int argc;
	char **argv;
	guint32 opts;
	char *aot_options;
} MainThreadArgs;

static void main_thread_handler (gpointer user_data)
{
	MainThreadArgs *main_args = user_data;
	MonoAssembly *assembly;

	assembly = mono_domain_assembly_open (main_args->domain, main_args->file);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", main_args->file);
		exit (1);
	}

	if (mono_compile_aot) {
		int res = mono_compile_assembly (assembly, main_args->opts, main_args->aot_options);
		printf ("AOT RESULT %d\n", res);
	} else {
		/* 
		 * This must be done in a thread managed by mono since it can invoke
		 * managed code.
		 */
		if (main_args->opts & MONO_OPT_PRECOMP)
			mono_precompile_assemblies ();

		mono_jit_exec (main_args->domain, assembly, main_args->argc, main_args->argv);
	}
}

static void
mini_usage (void)
{
	int i;

	fprintf (stdout,
		"Usage is: mono [options] assembly\n\n"
		"Runtime and JIT debugging:\n"
		"    --compile METHOD       Just compile METHOD in assembly\n"
		"    --ncompile N           Number of times to compile METHOD (default: 1)\n"
		"    --regression           Runs the regression test contained in the assembly\n"
		"    --print-vtable         Print the vtable of all used classes\n"
		"    --trace[=EXPR]         Enable tracing, use --help-trace for details\n"
		"    --compile-all          Compiles all the methods in the assembly\n"
		"    --breakonex            Inserts a breakpoint on exceptions\n"
		"    --break METHOD         Inserts a breakpoint at METHOD entry\n"
		"    --debug                Enable debugging support\n"
	        "    --stats                Print statistics about the JIT operations\n"
		"\n"
		"Development:\n"
		"    --statfile FILE        Sets the stat file to FILE\n"
		"    --aot                  Compiles the assembly to native code\n"
		"    --profile[=profiler]   Runs in profiling mode with the specified profiler module\n"
		"    --graph[=TYPE] METHOD  Draws a graph of the specified method:\n");
	
	for (i = 0; i < G_N_ELEMENTS (graph_names); ++i) {
		fprintf (stdout, "                           %-10s %s\n", graph_names [i].name, graph_names [i].desc);
	}

	fprintf (stdout,
		"\n"
		"Runtime:\n"
		"    --config FILE          Loads FILE as the Mono config\n"
		"    --verbose, -v          Increases the verbosity level\n"
		"    --help, -h             Show usage information\n"
		"    --version, -V          Show version information\n"
		"    --optimize=OPT         Turns on a specific optimization:\n");

	for (i = 0; i < G_N_ELEMENTS (opt_names); ++i)
		fprintf (stdout, "                           %-10s %s\n", opt_names [i].name, opt_names [i].desc);
}

static void
mini_trace_usage (void)
{
	fprintf (stdout,
		 "Tracing options:\n"
		 "   --trace[=EXPR]        Trace every call, optional EXPR controls the scope\n"
		 "\n"
		 "EXPR is composed of:\n"
		 "    all                  All assemblies\n"
		 "    none                 No assemblies\n"
		 "    program              Entry point assembly\n"
		 "    assembly             Specifies an assembly\n"
		 "    M:Type:Method        Specifies a method\n"
		 "    N:Namespace          Specifies a namespace\n"
		 "    T:Type               Specifies a type\n"
		 "    +EXPR                Includes expression\n"
		 "    -EXPR                Excludes expression\n");
}

static const char *info = ""
#ifdef HAVE_KW_THREAD
	"\tTLS:           __thread\n"
#else
	"\tTLS:           normal\n"
#endif /* HAVE_KW_THREAD */
#ifdef HAVE_BOEHM_GC
#ifdef USE_INCLUDED_LIBGC
	"\tGC:            Included Boehm (with typed GC)\n"
#else
#if HAVE_GC_GCJ_MALLOC
	"\tGC:            System Boehm (with typed GC)\n"
#else
	"\tGC:            System Boehm (no typed GC available)\n"
#endif
#endif
#else
	"\tGC:            none\n"
#endif /* HAVE_BOEHM_GC */
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
    "\tSIGSEGV      : altstack\n"
#else
    "\tSIGSEGV      : normal\n"
#endif
#ifdef HAVE_ICU
	"\tGlobalization: ICU\n"
#else
	"\tGlobalization: none\n"
#endif /* HAVE_ICU */
	"";

int
mono_main (int argc, char* argv[])
{
	MainThreadArgs main_args;
	MonoAssembly *assembly;
	MonoMethodDesc *desc;
	MonoMethod *method;
	MonoCompile *cfg;
	MonoDomain *domain;
	const char* aname, *mname = NULL;
	char *config_file = NULL;
	int i, count = 1;
	int enable_debugging = FALSE;
	guint32 opt, action = DO_EXEC;
	MonoGraphOptions mono_graph_options = 0;
	int mini_verbose = 0;
	gboolean enable_profile = FALSE;
	char *trace_options = NULL;
	char *profile_options;
	char *aot_options = NULL;

	setlocale (LC_ALL, "");

	g_log_set_always_fatal (G_LOG_LEVEL_ERROR);
	g_log_set_fatal_mask (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR);

	opt = parse_optimizations (NULL);

	for (i = 1; i < argc; ++i) {
		if (argv [i] [0] != '-')
			break;
		if (strcmp (argv [i], "--regression") == 0) {
			action = DO_REGRESSION;
		} else if (strcmp (argv [i], "--verbose") == 0 || strcmp (argv [i], "-v") == 0) {
			mini_verbose++;
		} else if (strcmp (argv [i], "--version") == 0 || strcmp (argv [i], "-V") == 0) {
			g_print ("Mono JIT compiler version %s, (C) 2002-2004 Novell, Inc and Contributors. www.go-mono.com\n", VERSION);
			g_print (info);
			if (mini_verbose) {
				const guchar *cerror;
				const guchar *clibpath;
				mono_init ("mono");
				cerror = mono_check_corlib_version ();
				clibpath = mono_defaults.corlib? mono_image_get_filename (mono_defaults.corlib): "unknown";
				if (cerror) {
					g_print ("The currently installed mscorlib doesn't match this runtime version.\n");
					g_print ("The error is: %s\n", cerror);
					g_print ("mscorlib.dll loaded at: %s\n", clibpath);
					return 1;
				}
			}
			return 0;
		} else if (strcmp (argv [i], "--help") == 0 || strcmp (argv [i], "-h") == 0) {
			mini_usage ();
			return 0;
		} else if (strcmp (argv [i], "--help-trace") == 0){
			mini_trace_usage ();
			return 0;
		} else if (strncmp (argv [i], "--statfile", 10) == 0) {
			mini_stats_fd = fopen (argv [++i], "w+");
		} else if (strncmp (argv [i], "--optimize=", 11) == 0) {
			opt = parse_optimizations (argv [i] + 11);
		} else if (strncmp (argv [i], "-O=", 3) == 0) {
			opt = parse_optimizations (argv [i] + 3);
		} else if (strcmp (argv [i], "--config") == 0) {
			config_file = argv [++i];
		} else if (strcmp (argv [i], "--ncompile") == 0) {
			count = atoi (argv [++i]);
			action = DO_BENCH;
		} else if (strcmp (argv [i], "--trace") == 0) {
			trace_options = (char*)"";
		} else if (strncmp (argv [i], "--trace=", 8) == 0) {
			trace_options = &argv [i][8];
		} else if (strcmp (argv [i], "--breakonex") == 0) {
			mono_break_on_exc = TRUE;
		} else if (strcmp (argv [i], "--break") == 0) {
			if (!mono_debugger_insert_breakpoint (argv [++i], FALSE))
				g_error ("Invalid method name '%s'", argv [i]);
		} else if (strcmp (argv [i], "--print-vtable") == 0) {
			mono_print_vtable = TRUE;
		} else if (strcmp (argv [i], "--stats") == 0) {
			mono_stats.enabled = TRUE;
			mono_jit_stats.enabled = TRUE;
		} else if (strcmp (argv [i], "--aot") == 0) {
			mono_compile_aot = TRUE;
		} else if (strncmp (argv [i], "--aot=", 6) == 0) {
			mono_compile_aot = TRUE;
			aot_options = &argv [i][6];
		} else if (strcmp (argv [i], "--compile-all") == 0) {
			action = DO_COMPILE;
		} else if (strcmp (argv [i], "--profile") == 0) {
			enable_profile = TRUE;
			profile_options = NULL;
		} else if (strncmp (argv [i], "--profile=", 10) == 0) {
			enable_profile = TRUE;
			profile_options = argv [i] + 10;
		} else if (strcmp (argv [i], "--compile") == 0) {
			mname = argv [++i];
			action = DO_BENCH;
		} else if (strncmp (argv [i], "--graph=", 8) == 0) {
			mono_graph_options = mono_parse_graph_options (argv [i] + 8);
			mname = argv [++i];
			action = DO_DRAW;
		} else if (strcmp (argv [i], "--graph") == 0) {
			mname = argv [++i];
			mono_graph_options = MONO_GRAPH_CFG;
			action = DO_DRAW;
		} else if (strcmp (argv [i], "--debug") == 0) {
			enable_debugging = TRUE;
		} else {
			fprintf (stderr, "Unknown command line option: '%s'\n", argv [i]);
			return 1;
		}
	}

	if (!argv [i]) {
		mini_usage ();
		return 1;
	}

	if (mono_compile_aot || action == DO_EXEC) {
		g_set_prgname (argv[i]);
	}

	if (enable_profile) {
		/* Needed because of TLS accesses in mono_profiler_load () */
		MONO_GC_PRE_INIT ();
		mono_profiler_load (profile_options);
	}

	if (trace_options != NULL){
		/* 
		 * Need to call this before mini_init () so we can trace methods 
		 * compiled there too.
		 */
		mono_jit_trace_calls = mono_trace_parse_options (trace_options);
		if (mono_jit_trace_calls == NULL)
			exit (1);
	}

	mono_set_defaults (mini_verbose, opt);
	domain = mini_init (argv [i]);
	
	switch (action) {
	case DO_REGRESSION:
		if (mini_regression_list (mini_verbose, argc -i, argv + i)) {
			g_print ("Regression ERRORS!\n");
			mini_cleanup (domain);
			return 1;
		}
		mini_cleanup (domain);
		return 0;
	case DO_BENCH:
		if (argc - i != 1 || mname == NULL) {
			g_print ("Usage: mini --ncompile num --compile method assembly\n");
			mini_cleanup (domain);
			return 1;
		}
		aname = argv [i];
		break;
	case DO_COMPILE:
		if (argc - i != 1) {
			mini_usage ();
			mini_cleanup (domain);
			return 1;
		}
		aname = argv [i];
		break;
	case DO_DRAW:
		if (argc - i != 1 || mname == NULL) {
			mini_usage ();
			mini_cleanup (domain);
			return 1;
		}
		aname = argv [i];
		break;
	default:
		if (argc - i < 1) {
			mini_usage ();
			mini_cleanup (domain);
			return 1;
		}
		aname = argv [i];
		break;
	}

	if (enable_debugging) {
		mono_debug_init (MONO_DEBUG_FORMAT_MONO);
		mono_debug_init_1 (domain);
	}

	/* Parse gac loading options before loading assemblies. */
	if (mono_compile_aot || action == DO_EXEC) {
		mono_config_parse (config_file);
	}

	assembly = mono_assembly_open (aname, NULL);
	if (!assembly) {
		fprintf (stderr, "cannot open assembly %s\n", aname);
		mini_cleanup (domain);
		return 2;
	}

	if (trace_options != NULL)
		mono_trace_set_assembly (assembly);

	if (enable_debugging)
		mono_debug_init_2 (assembly);

	if (mono_compile_aot || action == DO_EXEC) {
		const guchar *error;

		//mono_set_rootdir ();

		error = mono_check_corlib_version ();
		if (error) {
			fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
			fprintf (stderr, "Download a newer corlib or a newer runtime at http://www.go-mono.com/daily.\n");
			exit (1);
		}

		main_args.domain = domain;
		main_args.file = aname;		
		main_args.argc = argc - i;
		main_args.argv = argv + i;
		main_args.opts = opt;
		main_args.aot_options = aot_options;
	     
		mono_runtime_exec_managed_code (domain, main_thread_handler, &main_args);
		mini_cleanup (domain);

		/* Look up return value from System.Environment.ExitCode */
		i = mono_environment_exitcode_get ();
		return i;
	} else if (action == DO_COMPILE) {
		compile_all_methods (assembly, mini_verbose);
		mini_cleanup (domain);
		return 0;
	}
	desc = mono_method_desc_new (mname, 0);
	if (!desc) {
		g_print ("Invalid method name %s\n", mname);
		mini_cleanup (domain);
		return 3;
	}
	method = mono_method_desc_search_in_image (desc, mono_assembly_get_image (assembly));
	if (!method) {
		g_print ("Cannot find method %s\n", mname);
		mini_cleanup (domain);
		return 3;
	}

	if (action == DO_DRAW) {
		int part = 0;

		switch (mono_graph_options) {
		case MONO_GRAPH_DTREE:
			part = 1;
			opt |= MONO_OPT_LOOP;
			break;
		case MONO_GRAPH_CFG_CODE:
			part = 1;
			break;
		case MONO_GRAPH_CFG_SSA:
			part = 2;
			break;
		case MONO_GRAPH_CFG_OPTCODE:
			part = 3;
			break;
		default:
			break;
		}

		if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
			(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
			MonoMethod *nm;
			nm = mono_marshal_get_native_wrapper (method);
			cfg = mini_method_compile (nm, opt, mono_get_root_domain (), FALSE, part);
		}
		else
			cfg = mini_method_compile (method, opt, mono_get_root_domain (), FALSE, part);
		if ((mono_graph_options & MONO_GRAPH_CFG_SSA) && !(cfg->comp_done & MONO_COMP_SSA)) {
			g_warning ("no SSA info available (use -O=deadce)");
			return 1;
		}
		mono_draw_graph (cfg, mono_graph_options);
		mono_destroy_compile (cfg);

	} else if (action == DO_BENCH) {
		if (mini_stats_fd) {
			const char *n;
			double no_opt_time = 0.0;
			GTimer *timer = g_timer_new ();
			fprintf (mini_stats_fd, "$stattitle = \'Compilations times for %s\';\n", 
				 mono_method_full_name (method, TRUE));
			fprintf (mini_stats_fd, "@data = (\n");
			fprintf (mini_stats_fd, "[");
			for (i = 0; i < G_N_ELEMENTS (opt_sets); i++) {
				opt = opt_sets [i];
				n = opt_descr (opt);
				if (!n [0])
					n = "none";
				fprintf (mini_stats_fd, "\"%s\",", n);
			}
			fprintf (mini_stats_fd, "],\n[");

			for (i = 0; i < G_N_ELEMENTS (opt_sets); i++) {
				int j;
				double elapsed;
				opt = opt_sets [i];
				g_timer_start (timer);
				for (j = 0; j < count; ++j) {
					cfg = mini_method_compile (method, opt, mono_get_root_domain (), FALSE, 0);
					mono_destroy_compile (cfg);
				}
				g_timer_stop (timer);
				elapsed = g_timer_elapsed (timer, NULL);
				if (!opt)
					no_opt_time = elapsed;
				fprintf (mini_stats_fd, "%f, ", elapsed);
			}
			fprintf (mini_stats_fd, "]");
			if (no_opt_time > 0.0) {
				fprintf (mini_stats_fd, ", \n[");
				for (i = 0; i < G_N_ELEMENTS (opt_sets); i++) 
					fprintf (mini_stats_fd, "%f,", no_opt_time);
				fprintf (mini_stats_fd, "]");
			}
			fprintf (mini_stats_fd, ");\n");
		} else {
			for (i = 0; i < count; ++i) {
				if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
					(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
					method = mono_marshal_get_native_wrapper (method);

				cfg = mini_method_compile (method, opt, mono_get_root_domain (), FALSE, 0);
				mono_destroy_compile (cfg);
			}
		}
	} else {
		cfg = mini_method_compile (method, opt, mono_get_root_domain (), FALSE, 0);
		mono_destroy_compile (cfg);
	}

	mini_cleanup (domain);
 	return 0;
}

MonoDomain * 
mono_jit_init (const char *file)
{
	return mini_init (file);
}

void        
mono_jit_cleanup (MonoDomain *domain)
{
	mini_cleanup (domain);
}


