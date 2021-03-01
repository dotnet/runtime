/**
 * \file
 * The new mono JIT compiler.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002-2003 Ximian, Inc.
 * (C) 2003-2006 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <signal.h>
#if HAVE_SCHED_SETAFFINITY
#include <sched.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/environment-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/coree.h>
#include <mono/metadata/w32process.h>
#include "mono/utils/mono-counters.h"
#include "mono/utils/mono-hwcap.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/options.h"
#include "mono/metadata/w32handle.h"
#include "mono/metadata/callspec.h"
#include "mono/metadata/custom-attrs-internals.h"
#include <mono/utils/w32subset.h>

#include "mini.h"
#include "jit.h"
#include "aot-compiler.h"
#include "aot-runtime.h"
#include "mini-runtime.h"
#include "interp/interp.h"

#include <string.h>
#include <ctype.h>
#include <locale.h>
#include "debugger-agent.h"
#if TARGET_OSX
#   include <sys/resource.h>
#endif

static FILE *mini_stats_fd;

static void mini_usage (void);
static void mono_runtime_set_execution_mode (int mode);
static void mono_runtime_set_execution_mode_full (int mode, gboolean override);
static int mono_jit_exec_internal (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[]);

#ifdef HOST_WIN32
/* Need this to determine whether to detach console */
#include <mono/metadata/cil-coff.h>
/* This turns off command line globbing under win32 */
int _CRT_glob = 0;
#endif

typedef void (*OptFunc) (const char *p);

#undef OPTFLAG

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line

static const struct msgstr_t {
#define OPTFLAG(id,shift,name,desc) char MSGSTRFIELD(__LINE__) [sizeof (name) + sizeof (desc)];
#include "optflags-def.h"
#undef OPTFLAG
} opstr = {
#define OPTFLAG(id,shift,name,desc) name "\0" desc,
#include "optflags-def.h"
#undef OPTFLAG
};
static const gint16 opt_names [] = {
#define OPTFLAG(id,shift,name,desc) offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "optflags-def.h"
#undef OPTFLAG
};

#define optflag_get_name(id) ((const char*)&opstr + opt_names [(id)])
#define optflag_get_desc(id) (optflag_get_name(id) + 1 + strlen (optflag_get_name(id)))

#define DEFAULT_OPTIMIZATIONS (	\
	MONO_OPT_PEEPHOLE |	\
	MONO_OPT_CFOLD |	\
	MONO_OPT_INLINE |       \
	MONO_OPT_CONSPROP |     \
	MONO_OPT_COPYPROP |     \
	MONO_OPT_DEADCE |       \
	MONO_OPT_BRANCH |	\
	MONO_OPT_LINEARS |	\
	MONO_OPT_INTRINS |  \
	MONO_OPT_LOOP |  \
	MONO_OPT_EXCEPTION |  \
    MONO_OPT_CMOV |  \
	MONO_OPT_GSHARED |	\
	MONO_OPT_SIMD |	\
	MONO_OPT_ALIAS_ANALYSIS	| \
	MONO_OPT_AOT | \
	MONO_OPT_FLOAT32)

#define EXCLUDED_FROM_ALL (MONO_OPT_PRECOMP | MONO_OPT_UNSAFE | MONO_OPT_GSHAREDVT)

static char *mono_parse_options (const char *options, int *ref_argc, char **ref_argv [], gboolean prepend);
static char *mono_parse_response_options (const char *options, int *ref_argc, char **ref_argv [], gboolean prepend);

static guint32
parse_optimizations (guint32 opt, const char* p, gboolean cpu_opts)
{
	guint32 exclude = 0;
	const char *n;
	int i, invert;
	char **parts, **ptr;

	/* Initialize the hwcap module if necessary. */
	mono_hwcap_init ();

	/* call out to cpu detection code here that sets the defaults ... */
	if (cpu_opts) {
#ifndef MONO_CROSS_COMPILE
		opt |= mono_arch_cpu_optimizations (&exclude);
		opt &= ~exclude;
#endif
	}
	if (!p)
		return opt;

	parts = g_strsplit (p, ",", -1);
	for (ptr = parts; ptr && *ptr; ptr ++) {
		char *arg = *ptr;
		char *p = arg;

		if (*p == '-') {
			p++;
			invert = TRUE;
		} else {
			invert = FALSE;
		}
		for (i = 0; i < G_N_ELEMENTS (opt_names) && optflag_get_name (i); ++i) {
			n = optflag_get_name (i);
			if (!strcmp (p, n)) {
				if (invert)
					opt &= ~ (1 << i);
				else
					opt |= 1 << i;
				break;
			}
		}
		if (i == G_N_ELEMENTS (opt_names) || !optflag_get_name (i)) {
			if (strncmp (p, "all", 3) == 0) {
				if (invert)
					opt = 0;
				else
					opt = ~(EXCLUDED_FROM_ALL | exclude);
			} else {
				fprintf (stderr, "Invalid optimization name `%s'\n", p);
				exit (1);
			}
		}

		g_free (arg);
	}
	g_free (parts);

	return opt;
}

static gboolean
parse_debug_options (const char* p)
{
	MonoDebugOptions *opt = mini_get_debug_options ();
	opt->enabled = TRUE;

	do {
		if (!*p) {
			fprintf (stderr, "Syntax error; expected debug option name\n");
			return FALSE;
		}

		if (!strncmp (p, "casts", 5)) {
			opt->better_cast_details = TRUE;
			p += 5;
		} else if (!strncmp (p, "mdb-optimizations", 17)) {
			opt->mdb_optimizations = TRUE;
			p += 17;
		} else if (!strncmp (p, "gdb", 3)) {
			opt->gdb = TRUE;
			p += 3;
		} else if (!strncmp (p, "ignore", 6)) {
			opt->enabled = FALSE;
			p += 6;
		} else {
			fprintf (stderr, "Invalid debug option `%s', use --help-debug for details\n", p);
			return FALSE;
		}

		if (*p == ',') {
			p++;
			if (!*p) {
				fprintf (stderr, "Syntax error; expected debug option name\n");
				return FALSE;
			}
		}
	} while (*p);

	return TRUE;
}

typedef struct {
	char name [6];
	char desc [18];
	MonoGraphOptions value;
} GraphName;

static const GraphName 
graph_names [] = {
	{"cfg",      "Control Flow",                            MONO_GRAPH_CFG},
	{"dtree",    "Dominator Tree",                          MONO_GRAPH_DTREE},
	{"code",     "CFG showing code",                        MONO_GRAPH_CFG_CODE},
	{"ssa",      "CFG after SSA",                           MONO_GRAPH_CFG_SSA},
	{"optc",     "CFG after IR opts",                       MONO_GRAPH_CFG_OPTCODE}
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

/**
 * mono_parse_default_optimizations:
 */
int
mono_parse_default_optimizations (const char* p)
{
	guint32 opt;

	opt = parse_optimizations (DEFAULT_OPTIMIZATIONS, p, TRUE);
	return opt;
}

char*
mono_opt_descr (guint32 flags) {
	GString *str = g_string_new ("");
	int i;
	gboolean need_comma;

	need_comma = FALSE;
	for (i = 0; i < G_N_ELEMENTS (opt_names); ++i) {
		if (flags & (1 << i) && optflag_get_name (i)) {
			if (need_comma)
				g_string_append_c (str, ',');
			g_string_append (str, optflag_get_name (i));
			need_comma = TRUE;
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
       MONO_OPT_ALIAS_ANALYSIS,
#ifdef MONO_ARCH_SIMD_INTRINSICS
       MONO_OPT_SIMD | MONO_OPT_INTRINS,
       MONO_OPT_SSE2,
       MONO_OPT_SIMD | MONO_OPT_SSE2 | MONO_OPT_INTRINS,
#endif
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_INTRINS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_INTRINS | MONO_OPT_ALIAS_ANALYSIS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_CFOLD,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_ALIAS_ANALYSIS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_TAILCALL,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_SSA,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_EXCEPTION,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_EXCEPTION | MONO_OPT_CMOV,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_EXCEPTION | MONO_OPT_ABCREM,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_ABCREM,
       MONO_OPT_BRANCH | MONO_OPT_PEEPHOLE | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP | MONO_OPT_DEADCE | MONO_OPT_LOOP | MONO_OPT_INLINE | MONO_OPT_INTRINS | MONO_OPT_EXCEPTION | MONO_OPT_CMOV,
       DEFAULT_OPTIMIZATIONS, 
};

static const guint32
interp_opt_sets [] = {
	INTERP_OPT_NONE,
	INTERP_OPT_INLINE,
	INTERP_OPT_CPROP,
	INTERP_OPT_SUPER_INSTRUCTIONS,
	INTERP_OPT_INLINE | INTERP_OPT_CPROP,
	INTERP_OPT_INLINE | INTERP_OPT_SUPER_INSTRUCTIONS,
	INTERP_OPT_CPROP | INTERP_OPT_SUPER_INSTRUCTIONS,
	INTERP_OPT_INLINE | INTERP_OPT_CPROP | INTERP_OPT_SUPER_INSTRUCTIONS | INTERP_OPT_BBLOCKS,
};

static const char* const
interp_opflags_names [] = {
	"inline",
	"cprop",
	"super-insn",
	"bblocks"
};

static const char*
interp_optflag_get_name (guint32 i)
{
	g_assert (i < G_N_ELEMENTS (interp_opflags_names));
	return interp_opflags_names [i];
}

static char*
interp_opt_descr (guint32 flags)
{
	GString *str = g_string_new ("");
	int i;
	gboolean need_comma;

	need_comma = FALSE;
	for (i = 0; i < G_N_ELEMENTS (interp_opflags_names); ++i) {
		if (flags & (1 << i) && interp_optflag_get_name (i)) {
			if (need_comma)
				g_string_append_c (str, ',');
			g_string_append (str, interp_optflag_get_name (i));
			need_comma = TRUE;
		}
	}
	return g_string_free (str, FALSE);
}

typedef int (*TestMethod) (void);

#if 0
static void
domain_dump_native_code (MonoDomain *domain) {
	// need to poke into the domain, move to metadata/domain.c
	// need to empty jit_info_table and code_mp
}
#endif

static gboolean do_regression_retries;
static int regression_test_skip_index;


static gboolean
method_should_be_regression_tested (MonoMethod *method, gboolean interp)
{
	ERROR_DECL (error);

	if (strncmp (method->name, "test_", 5) != 0)
		return FALSE;

	static gboolean filter_method_init = FALSE;
	static const char *filter_method = NULL;

	if (!filter_method_init) {
		filter_method = g_getenv ("REGRESSION_FILTER_METHOD");
		filter_method_init = TRUE;
	}

	if (filter_method) {
		const char *name = filter_method;

		if ((strchr (name, '.') > name) || strchr (name, ':')) {
			MonoMethodDesc *desc = mono_method_desc_new (name, TRUE);
			gboolean res = mono_method_desc_full_match (desc, method);
			mono_method_desc_free (desc);
			return res;
		} else {
			return strcmp (method->name, name) == 0;
		}
	}

	MonoCustomAttrInfo* ainfo = mono_custom_attrs_from_method_checked (method, error);
	mono_error_cleanup (error);
	if (!ainfo)
		return TRUE;

	int j;
	for (j = 0; j < ainfo->num_attrs; ++j) {
		MonoCustomAttrEntry *centry = &ainfo->attrs [j];
		if (centry->ctor == NULL)
			continue;

		MonoClass *klass = centry->ctor->klass;
		if (strcmp (m_class_get_name (klass), "CategoryAttribute") || mono_method_signature_internal (centry->ctor)->param_count != 1)
			continue;

		gpointer *typed_args, *named_args;
		int num_named_args;
		CattrNamedArg *arginfo;

		mono_reflection_create_custom_attr_data_args_noalloc (
			mono_defaults.corlib, centry->ctor, centry->data, centry->data_size,
			&typed_args, &named_args, &num_named_args, &arginfo, error);
		if (!is_ok (error))
			continue;

		const char *arg = (const char*)typed_args [0];
		mono_metadata_decode_value (arg, &arg);
		char *utf8_str = (char*)arg; //this points into image memory that is constant
		g_free (typed_args);
		g_free (named_args);
		g_free (arginfo);

		if (interp && !strcmp (utf8_str, "!INTERPRETER")) {
			g_print ("skip %s...\n", method->name);
			return FALSE;
		}

#if HOST_WASM
		if (!strcmp (utf8_str, "!WASM")) {
			g_print ("skip %s...\n", method->name);
			return FALSE;
		}
#endif
		if (mono_aot_mode == MONO_AOT_MODE_FULL && !strcmp (utf8_str, "!FULLAOT")) {
			g_print ("skip %s...\n", method->name);
			return FALSE;
		}

		if ((mono_aot_mode == MONO_AOT_MODE_INTERP_LLVMONLY || mono_aot_mode == MONO_AOT_MODE_LLVMONLY) && !strcmp (utf8_str, "!BITCODE")) {
			g_print ("skip %s...\n", method->name);
			return FALSE;
		}
	}

	return TRUE;
}

static void
mini_regression_step (MonoImage *image, int verbose, int *total_run, int *total,
		guint32 opt_flags,
		GTimer *timer, MonoDomain *domain)
{
	int result, expected, failed, cfailed, run, code_size;
	double elapsed, comp_time, start_time;
	char *n;
	int i;

	mono_set_defaults (verbose, opt_flags);
	n = mono_opt_descr (opt_flags);
	g_print ("Test run: image=%s, opts=%s\n", mono_image_get_filename (image), n);
	g_free (n);
	cfailed = failed = run = code_size = 0;
	comp_time = elapsed = 0.0;
	int local_skip_index = 0;

	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
	g_hash_table_destroy (jit_mm->jit_trampoline_hash);
	jit_mm->jit_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	mono_internal_hash_table_destroy (&(domain->jit_code_hash));
	mono_jit_code_hash_init (&(domain->jit_code_hash));

	g_timer_start (timer);
	if (mini_stats_fd)
		fprintf (mini_stats_fd, "[");
	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		ERROR_DECL (error);
		MonoMethod *method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, error);
		if (!method) {
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			continue;
		}
		if (method_should_be_regression_tested (method, FALSE)) {
			MonoCompile *cfg = NULL;
			TestMethod func = NULL;

			expected = atoi (method->name + 5);
			run++;
			start_time = g_timer_elapsed (timer, NULL);

#ifdef DISABLE_JIT
#ifdef MONO_USE_AOT_COMPILER
			ERROR_DECL (error);
			func = (TestMethod)mono_aot_get_method (method, error);
			mono_error_cleanup (error);
#else
			g_error ("No JIT or AOT available, regression testing not possible!");
#endif

#else
			comp_time -= start_time;
			cfg = mini_method_compile (method, mono_get_optimizations_for_method (method, opt_flags), JIT_FLAG_RUN_CCTORS, 0, -1);
			comp_time += g_timer_elapsed (timer, NULL);
			if (cfg->exception_type == MONO_EXCEPTION_NONE) {
#ifdef MONO_USE_AOT_COMPILER
				ERROR_DECL (error);
				func = (TestMethod)mono_aot_get_method (method, error);
				mono_error_cleanup (error);
				if (!func) {
					func = (TestMethod)MINI_ADDR_TO_FTNPTR (cfg->native_code);
				}
#else
				func = (TestMethod)(gpointer)cfg->native_code;
				func = MINI_ADDR_TO_FTNPTR (func);
#endif
				func = (TestMethod)mono_create_ftnptr ((gpointer)func);
			}
#endif

			if (func) {
				if (do_regression_retries) {
					++local_skip_index;

					if(local_skip_index <= regression_test_skip_index)
						continue;
					++regression_test_skip_index;
				}

				if (verbose >= 2)
					g_print ("Running '%s' ...\n", method->name);

#if HOST_WASM
				//WASM AOT injects dummy args and we must call with exact signatures
				int (*func_2)(int) = (int (*)(int))(void*)func;
				result = func_2 (-1);
#else
				result = func ();
#endif
				if (result != expected) {
					failed++;
					g_print ("Test '%s' failed result (got %d, expected %d).\n", method->name, result, expected);
				}
				if (cfg) {
					code_size += cfg->code_len;
					mono_destroy_compile (cfg);
				}
			} else {
				cfailed++;
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
	if (failed > 0 || cfailed > 0){
		g_print ("Results: total tests: %d, failed: %d, cfailed: %d (pass: %.2f%%)\n",
				run, failed, cfailed, 100.0*(run-failed-cfailed)/run);
	} else {
		g_print ("Results: total tests: %d, all pass \n",  run);
	}

	g_print ("Elapsed time: %f secs (%f, %f), Code size: %d\n\n", elapsed,
			elapsed - comp_time, comp_time, code_size);
	*total += failed + cfailed;
	*total_run += run;
}

static int
mini_regression (MonoImage *image, int verbose, int *total_run)
{
	guint32 i, opt;
	MonoMethod *method;
	char *n;
	GTimer *timer = g_timer_new ();
	MonoDomain *domain = mono_domain_get ();
	guint32 exclude = 0;
	int total;

	/* Note: mono_hwcap_init () called in mono_init () before we get here. */
	mono_arch_cpu_optimizations (&exclude);

	if (mini_stats_fd) {
		fprintf (mini_stats_fd, "$stattitle = \'Mono Benchmark Results (various optimizations)\';\n");

		fprintf (mini_stats_fd, "$graph->set_legend(qw(");
		for (opt = 0; opt < G_N_ELEMENTS (opt_sets); opt++) {
			guint32 opt_flags = opt_sets [opt];
			n = mono_opt_descr (opt_flags);
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
		ERROR_DECL (error);
		method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, error);
		if (!method) {
			mono_error_cleanup (error);
			continue;
		}
		mono_class_init_internal (method->klass);

		if (!strncmp (method->name, "test_", 5) && mini_stats_fd) {
			fprintf (mini_stats_fd, "\"%s\",", method->name);
		}
	}
	if (mini_stats_fd)
		fprintf (mini_stats_fd, "],\n");


	total = 0;
	*total_run = 0;
	if (mono_do_single_method_regression) {
		GSList *iter;

		mini_regression_step (image, verbose, total_run, &total,
				0,
				timer, domain);
		if (total)
			return total;
		g_print ("Single method regression: %d methods\n", g_slist_length (mono_single_method_list));

		for (iter = mono_single_method_list; iter; iter = g_slist_next (iter)) {
			char *method_name;

			mono_current_single_method = (MonoMethod *)iter->data;

			method_name = mono_method_full_name (mono_current_single_method, TRUE);
			g_print ("Current single method: %s\n", method_name);
			g_free (method_name);

			mini_regression_step (image, verbose, total_run, &total,
					0,
					timer, domain);
			if (total)
				return total;
		}
	} else {
		for (opt = 0; opt < G_N_ELEMENTS (opt_sets); ++opt) {
			/* builtin-types.cs & aot-tests.cs need OPT_INTRINS enabled */
			if (!strcmp ("builtin-types", image->assembly_name) || !strcmp ("aot-tests", image->assembly_name))
				if (!(opt_sets [opt] & MONO_OPT_INTRINS))
					continue;

			//we running in AOT only, it makes no sense to try multiple flags
			if ((mono_aot_mode == MONO_AOT_MODE_FULL || mono_aot_mode == MONO_AOT_MODE_LLVMONLY) && opt_sets [opt] != DEFAULT_OPTIMIZATIONS) {
				continue;
			}

			mini_regression_step (image, verbose, total_run, &total,
					opt_sets [opt] & ~exclude,
					timer, domain);
		}
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
		MonoAssemblyOpenRequest req;
		mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, mono_domain_default_alc (mono_get_root_domain ()));
		ass = mono_assembly_request_open (images [i], &req, NULL);
		if (!ass) {
			g_warning ("failed to load assembly: %s", images [i]);
			continue;
		}
		total += mini_regression (mono_assembly_get_image_internal (ass), verbose, &run);
		total_run += run;
	}
	if (total > 0){
		g_print ("Overall results: tests: %d, failed: %d, opt combinations: %d (pass: %.2f%%)\n", 
			 total_run, total, (int)G_N_ELEMENTS (opt_sets), 100.0*(total_run-total)/total_run);
	} else {
		g_print ("Overall results: tests: %d, 100%% pass, opt combinations: %d\n", 
			 total_run, (int)G_N_ELEMENTS (opt_sets));
	}
	
	return total;
}

static void
interp_regression_step (MonoImage *image, int verbose, int *total_run, int *total, const guint32 *opt_flags, GTimer *timer, MonoDomain *domain)
{
	int result, expected, failed, cfailed, run;
	double elapsed, transform_time;
	int i;
	MonoObject *result_obj;
	int local_skip_index = 0;

	const char *n = NULL;
	if (opt_flags) {
		mini_get_interp_callbacks ()->set_optimizations (*opt_flags);
		n = interp_opt_descr (*opt_flags);
	} else {
		n = mono_interp_opts_string;
	}
	g_print ("Test run: image=%s, opts=%s\n", mono_image_get_filename (image), n);

	cfailed = failed = run = 0;
	transform_time = elapsed = 0.0;

	mini_get_interp_callbacks ()->invalidate_transformed ();

	g_timer_start (timer);
	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		ERROR_DECL (error);
		MonoMethod *method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, error);
		if (!method) {
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			continue;
		}

		if (method_should_be_regression_tested (method, TRUE)) {
			ERROR_DECL (interp_error);
			MonoObject *exc = NULL;

			if (do_regression_retries) {
				++local_skip_index;

				if(local_skip_index <= regression_test_skip_index)
					continue;
				++regression_test_skip_index;
			}

			result_obj = mini_get_interp_callbacks ()->runtime_invoke (method, NULL, NULL, &exc, interp_error);
			if (!is_ok (interp_error)) {
				cfailed++;
				g_print ("Test '%s' execution failed.\n", method->name);
			} else if (exc != NULL) {
				g_print ("Exception in Test '%s' occurred:\n", method->name);
				mono_object_describe (exc);
				run++;
				failed++;
			} else {
				result = *(gint32 *) mono_object_unbox_internal (result_obj);
				expected = atoi (method->name + 5);  // FIXME: oh no.
				run++;

				if (result != expected) {
					failed++;
					g_print ("Test '%s' failed result (got %d, expected %d).\n", method->name, result, expected);
				}
			}
		}
	}
	g_timer_stop (timer);
	elapsed = g_timer_elapsed (timer, NULL);
	if (failed > 0 || cfailed > 0){
		g_print ("Results: total tests: %d, failed: %d, cfailed: %d (pass: %.2f%%)\n",
				run, failed, cfailed, 100.0*(run-failed-cfailed)/run);
	} else {
		g_print ("Results: total tests: %d, all pass \n",  run);
	}

	g_print ("Elapsed time: %f secs (%f, %f)\n\n", elapsed,
			elapsed - transform_time, transform_time);
	*total += failed + cfailed;
	*total_run += run;
}

static int
interp_regression (MonoImage *image, int verbose, int *total_run)
{
	MonoMethod *method;
	GTimer *timer = g_timer_new ();
	MonoDomain *domain = mono_domain_get ();
	guint32 i;
	int total;

	/* load the metadata */
	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		ERROR_DECL (error);
		method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, error);
		if (!method) {
			mono_error_cleanup (error);
			continue;
		}
		mono_class_init_internal (method->klass);
	}

	total = 0;
	*total_run = 0;

	if (mono_interp_opts_string) {
		/* explicit option requested*/
		interp_regression_step (image, verbose, total_run, &total, NULL, timer, domain);
	} else {
		for (int opt = 0; opt < G_N_ELEMENTS (interp_opt_sets); ++opt)
			interp_regression_step (image, verbose, total_run, &total, &interp_opt_sets [opt], timer, domain);
	}

	g_timer_destroy (timer);
	return total;
}

/* TODO: merge this code with the regression harness of the JIT */
static int
mono_interp_regression_list (int verbose, int count, char *images [])
{
	int i, total, total_run, run;

	total_run = total = 0;
	for (i = 0; i < count; ++i) {
		MonoAssemblyOpenRequest req;
		mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, mono_domain_default_alc (mono_get_root_domain ()));
		MonoAssembly *ass = mono_assembly_request_open (images [i], &req, NULL);
		if (!ass) {
			g_warning ("failed to load assembly: %s", images [i]);
			continue;
		}
		total += interp_regression (mono_assembly_get_image_internal (ass), verbose, &run);
		total_run += run;
	}
	if (total > 0) {
		g_print ("Overall results: tests: %d, failed: %d (pass: %.2f%%)\n", total_run, total, 100.0*(total_run-total)/total_run);
	} else {
		g_print ("Overall results: tests: %d, 100%% pass\n", total_run);
	}

	return total;
}


#ifdef MONO_JIT_INFO_TABLE_TEST
typedef struct _JitInfoData
{
	guint start;
	guint length;
	MonoJitInfo *ji;
	struct _JitInfoData *next;
} JitInfoData;

typedef struct
{
	guint start;
	guint length;
	int num_datas;
	JitInfoData *data;
} Region;

typedef struct
{
	int num_datas;
	int num_regions;
	Region *regions;
	int num_frees;
	JitInfoData *frees;
} ThreadData;

static int num_threads;
static ThreadData *thread_datas;
static MonoDomain *test_domain;

static JitInfoData*
alloc_random_data (Region *region)
{
	JitInfoData **data;
	JitInfoData *prev;
	guint prev_end;
	guint next_start;
	guint max_len;
	JitInfoData *d;
	int num_retries = 0;
	int pos, i;

 restart:
	prev = NULL;
	data = &region->data;
	pos = random () % (region->num_datas + 1);
	i = 0;
	while (*data != NULL) {
		if (i++ == pos)
			break;
		prev = *data;
		data = &(*data)->next;
	}

	if (prev == NULL)
		g_assert (*data == region->data);
	else
		g_assert (prev->next == *data);

	if (prev == NULL)
		prev_end = region->start;
	else
		prev_end = prev->start + prev->length;

	if (*data == NULL)
		next_start = region->start + region->length;
	else
		next_start = (*data)->start;

	g_assert (prev_end <= next_start);

	max_len = next_start - prev_end;
	if (max_len < 128) {
		if (++num_retries >= 10)
			return NULL;
		goto restart;
	}
	if (max_len > 1024)
		max_len = 1024;

	d = g_new0 (JitInfoData, 1);
	d->start = prev_end + random () % (max_len / 2);
	d->length = random () % MIN (max_len, next_start - d->start) + 1;

	g_assert (d->start >= prev_end && d->start + d->length <= next_start);

	d->ji = g_new0 (MonoJitInfo, 1);
	d->ji->d.method = (MonoMethod*) 0xABadBabe;
	d->ji->code_start = (gpointer)(gulong) d->start;
	d->ji->code_size = d->length;
	d->ji->cas_inited = 1;	/* marks an allocated jit info */

	d->next = *data;
	*data = d;

	++region->num_datas;

	return d;
}

static JitInfoData**
choose_random_data (Region *region)
{
	int n;
	int i;
	JitInfoData **d;

	g_assert (region->num_datas > 0);

	n = random () % region->num_datas;

	for (d = &region->data, i = 0;
	     i < n;
	     d = &(*d)->next, ++i)
		;

	return d;
}

static Region*
choose_random_region (ThreadData *td)
{
	return &td->regions [random () % td->num_regions];
}

static ThreadData*
choose_random_thread (void)
{
	return &thread_datas [random () % num_threads];
}

static void
free_jit_info_data (ThreadData *td, JitInfoData *free)
{
	free->next = td->frees;
	td->frees = free;

	if (++td->num_frees >= 1000) {
		int i;

		for (i = 0; i < 500; ++i)
			free = free->next;

		while (free->next != NULL) {
			JitInfoData *next = free->next->next;

			//g_free (free->next->ji);
			g_free (free->next);
			free->next = next;

			--td->num_frees;
		}
	}
}

#define NUM_THREADS		8
#define REGIONS_PER_THREAD	10
#define REGION_SIZE		0x10000

#define MAX_ADDR		(REGION_SIZE * REGIONS_PER_THREAD * NUM_THREADS)

#define MODE_ALLOC	1
#define MODE_FREE	2

static void
test_thread_func (gpointer void_arg)
{
	ThreadData* td = (ThreadData*)void_arg;
	int mode = MODE_ALLOC;
	int i = 0;
	gulong lookup_successes = 0, lookup_failures = 0;
	MonoDomain *domain = test_domain;
	int thread_num = (int)(td - thread_datas);
	gboolean modify_thread = thread_num < NUM_THREADS / 2; /* only half of the threads modify the table */

	for (;;) {
		int alloc;
		int lookup = 1;

		if (td->num_datas == 0) {
			lookup = 0;
			alloc = 1;
		} else if (modify_thread && random () % 1000 < 5) {
			lookup = 0;
			if (mode == MODE_ALLOC)
				alloc = (random () % 100) < 70;
			else if (mode == MODE_FREE)
				alloc = (random () % 100) < 30;
		}

		if (lookup) {
			/* modify threads sometimes look up their own jit infos */
			if (modify_thread && random () % 10 < 5) {
				Region *region = choose_random_region (td);

				if (region->num_datas > 0) {
					JitInfoData **data = choose_random_data (region);
					guint pos = (*data)->start + random () % (*data)->length;
					MonoJitInfo *ji;

					ji = mono_jit_info_table_find (domain, (char*)(gsize)pos);

					g_assert (ji->cas_inited);
					g_assert ((*data)->ji == ji);
				}
			} else {
				int pos = random () % MAX_ADDR;
				char *addr = (char*)(uintptr_t)pos;
				MonoJitInfo *ji;

				ji = mono_jit_info_table_find (domain, addr);

				/*
				 * FIXME: We are actually not allowed
				 * to do this.  By the time we examine
				 * the ji another thread might already
				 * have removed it.
				 */
				if (ji != NULL) {
					g_assert (addr >= (char*)ji->code_start && addr < (char*)ji->code_start + ji->code_size);
					++lookup_successes;
				} else
					++lookup_failures;
			}
		} else if (alloc) {
			JitInfoData *data = alloc_random_data (choose_random_region (td));

			if (data != NULL) {
				mono_jit_info_table_add (domain, data->ji);

				++td->num_datas;
			}
		} else {
			Region *region = choose_random_region (td);

			if (region->num_datas > 0) {
				JitInfoData **data = choose_random_data (region);
				JitInfoData *free;

				mono_jit_info_table_remove (domain, (*data)->ji);

				//(*data)->ji->cas_inited = 0; /* marks a free jit info */

				free = *data;
				*data = (*data)->next;

				free_jit_info_data (td, free);

				--region->num_datas;
				--td->num_datas;
			}
		}

		if (++i % 100000 == 0) {
			int j;
			g_print ("num datas %d (%ld - %ld): %d", (int)(td - thread_datas),
				 lookup_successes, lookup_failures, td->num_datas);
			for (j = 0; j < td->num_regions; ++j)
				g_print ("  %d", td->regions [j].num_datas);
			g_print ("\n");
		}

		if (td->num_datas < 100)
			mode = MODE_ALLOC;
		else if (td->num_datas > 2000)
			mode = MODE_FREE;
	}
}

/*
static void
small_id_thread_func (gpointer arg)
{
	MonoThread *thread = mono_thread_current ();
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	g_print ("my small id is %d\n", (int)thread->small_id);
	mono_hazard_pointer_clear (hp, 1);
	sleep (3);
	g_print ("done %d\n", (int)thread->small_id);
}
*/

static void
jit_info_table_test (MonoDomain *domain)
{
	ERROR_DECL (error);
	int i;

	g_print ("testing jit_info_table\n");

	num_threads = NUM_THREADS;
	thread_datas = g_new0 (ThreadData, num_threads);

	for (i = 0; i < num_threads; ++i) {
		int j;

		thread_datas [i].num_regions = REGIONS_PER_THREAD;
		thread_datas [i].regions = g_new0 (Region, REGIONS_PER_THREAD);

		for (j = 0; j < REGIONS_PER_THREAD; ++j) {
			thread_datas [i].regions [j].start = (num_threads * j + i) * REGION_SIZE;
			thread_datas [i].regions [j].length = REGION_SIZE;
		}
	}

	test_domain = domain;

	/*
	for (i = 0; i < 72; ++i)
		mono_thread_create (domain, small_id_thread_func, NULL);

	sleep (2);
	*/

	for (i = 0; i < num_threads; ++i) {
		mono_thread_create_checked (domain, (gpointer)test_thread_func, &thread_datas [i], error);
		mono_error_assert_ok (error);
	}
}
#endif

enum {
	DO_BENCH,
	DO_REGRESSION,
	DO_SINGLE_METHOD_REGRESSION,
	DO_COMPILE,
	DO_EXEC,
	DO_DRAW,
	DO_DEBUGGER
};

typedef struct CompileAllThreadArgs {
	MonoAssembly *ass;
	int verbose;
	guint32 opts;
	guint32 recompilation_times;
} CompileAllThreadArgs;

static void
compile_all_methods_thread_main_inner (CompileAllThreadArgs *args)
{
	MonoAssembly *ass = args->ass;
	int verbose = args->verbose;
	MonoImage *image = mono_assembly_get_image_internal (ass);
	MonoMethod *method;
	MonoCompile *cfg;
	int i, count = 0, fail_count = 0;

	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		ERROR_DECL (error);
		guint32 token = MONO_TOKEN_METHOD_DEF | (i + 1);
		MonoMethodSignature *sig;

		if (mono_metadata_has_generic_params (image, token))
			continue;

		method = mono_get_method_checked (image, token, NULL, NULL, error);
		if (!method) {
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			continue;
		}
		if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		    (method->flags & METHOD_ATTRIBUTE_ABSTRACT))
			continue;

		if (mono_class_is_gtd (method->klass))
			continue;
		sig = mono_method_signature_internal (method);
		if (!sig) {
			char * desc = mono_method_full_name (method, TRUE);
			g_print ("Could not retrieve method signature for %s\n", desc);
			g_free (desc);
			fail_count ++;
			continue;
		}

		if (sig->has_type_parameters)
			continue;

		count++;
		if (verbose) {
			char * desc = mono_method_full_name (method, TRUE);
			g_print ("Compiling %d %s\n", count, desc);
			g_free (desc);
		}
		if (mono_use_interpreter) {
			mini_get_interp_callbacks ()->create_method_pointer (method, TRUE, error);
			// FIXME There are a few failures due to DllNotFoundException related to System.Native
			if (verbose && !is_ok (error))
				g_print ("Compilation of %s failed\n", mono_method_full_name (method, TRUE));
		} else {
			cfg = mini_method_compile (method, mono_get_optimizations_for_method (method, args->opts), (JitFlags)JIT_FLAG_DISCARD_RESULTS, 0, -1);
			if (cfg->exception_type != MONO_EXCEPTION_NONE) {
				const char *msg = cfg->exception_message;
				if (cfg->exception_type == MONO_EXCEPTION_MONO_ERROR)
					msg = mono_error_get_message (cfg->error);
				g_print ("Compilation of %s failed with exception '%s':\n", mono_method_full_name (cfg->method, TRUE), msg);
				fail_count ++;
			}
			mono_destroy_compile (cfg);
		}
	}

	if (fail_count)
		exit (1);
}

static void
compile_all_methods_thread_main (gpointer void_args)
{
	CompileAllThreadArgs *args = (CompileAllThreadArgs*)void_args;
	guint32 i;
	for (i = 0; i < args->recompilation_times; ++i)
		compile_all_methods_thread_main_inner (args);
}

static void
compile_all_methods (MonoAssembly *ass, int verbose, guint32 opts, guint32 recompilation_times)
{
	ERROR_DECL (error);
	CompileAllThreadArgs args;

	args.ass = ass;
	args.verbose = verbose;
	args.opts = opts;
	args.recompilation_times = recompilation_times;

	/* 
	 * Need to create a mono thread since compilation might trigger
	 * running of managed code.
	 */
	mono_thread_create_checked (mono_domain_get (), (gpointer)compile_all_methods_thread_main, &args, error);
	mono_error_assert_ok (error);

	mono_thread_manage_internal ();
}

/**
 * mono_jit_exec:
 * \param assembly reference to an assembly
 * \param argc argument count
 * \param argv argument vector
 * Start execution of a program.
 */
int 
mono_jit_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	int rv;
	MONO_ENTER_GC_UNSAFE;
	rv = mono_jit_exec_internal (domain, assembly, argc, argv);
	MONO_EXIT_GC_UNSAFE;
	return rv;
}

int
mono_jit_exec_internal (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	MONO_REQ_GC_UNSAFE_MODE;
	ERROR_DECL (error);
	MonoImage *image = mono_assembly_get_image_internal (assembly);

    // We need to ensure that any module cctor for this image
    // is run *before* we invoke the entry point
    // For more information, see https://blogs.msdn.microsoft.com/junfeng/2005/11/19/module-initializer-a-k-a-module-constructor/
    //
    // This is required in order for tools like Costura
    // (https://github.com/Fody/Costura) to work properly, as they inject
    // a module initializer which sets up event handlers (e.g. AssemblyResolve)
    // that allow the main method to run properly
    if (!mono_runtime_run_module_cctor(image, error)) {
        g_print ("Failed to run module constructor due to %s\n", mono_error_get_message (error));
        return 1;
    }

	MonoMethod *method;
	guint32 entry = mono_image_get_entry_point (image);

	if (!entry) {
		g_print ("Assembly '%s' doesn't have an entry point.\n", mono_image_get_filename (image));
		/* FIXME: remove this silly requirement. */
		mono_environment_exitcode_set (1);
		return 1;
	}

	method = mono_get_method_checked (image, entry, NULL, NULL, error);
	if (method == NULL){
		g_print ("The entry point method could not be loaded due to %s\n", mono_error_get_message (error));
		mono_error_cleanup (error);
		mono_environment_exitcode_set (1);
		return 1;
	}
	
	if (mono_llvm_only) {
		MonoObject *exc = NULL;
		int res;

		res = mono_runtime_try_run_main (method, argc, argv, &exc);
		if (exc) {
			mono_unhandled_exception_internal (exc);
			mono_invoke_unhandled_exception_hook (exc);
			g_assert_not_reached ();
		}
		return res;
	} else {
		int res = mono_runtime_run_main_checked (method, argc, argv, error);
		if (!is_ok (error)) {
			MonoException *ex = mono_error_convert_to_exception (error);
			if (ex) {
				mono_unhandled_exception_internal (&ex->object);
				mono_invoke_unhandled_exception_hook (&ex->object);
				g_assert_not_reached ();
			}
		}
		return res;
	}
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
	MainThreadArgs *main_args = (MainThreadArgs *)user_data;
	MonoAssembly *assembly;

	if (mono_compile_aot) {
		int i, res;
		gpointer *aot_state = NULL;

		/* Treat the other arguments as assemblies to compile too */
		for (i = 0; i < main_args->argc; ++i) {
			assembly = mono_domain_assembly_open_internal (main_args->domain, mono_domain_default_alc (main_args->domain), main_args->argv [i]);
			if (!assembly) {
				fprintf (stderr, "Can not open image %s\n", main_args->argv [i]);
				exit (1);
			}
			/* Check that the assembly loaded matches the filename */
			{
				MonoImageOpenStatus status;
				MonoImage *img;

				img = mono_image_open (main_args->argv [i], &status);
				if (img && strcmp (img->name, assembly->image->name)) {
					fprintf (stderr, "Error: Loaded assembly '%s' doesn't match original file name '%s'. Set MONO_PATH to the assembly's location.\n", assembly->image->name, img->name);
					exit (1);
				}
			}
			res = mono_compile_assembly (assembly, main_args->opts, main_args->aot_options, &aot_state);
			if (res != 0) {
				fprintf (stderr, "AOT of image %s failed.\n", main_args->argv [i]);
				exit (1);
			}
		}
		if (aot_state) {
			res = mono_compile_deferred_assemblies (main_args->opts, main_args->aot_options, &aot_state);
			if (res != 0) {
				fprintf (stderr, "AOT of mode-specific deferred assemblies failed.\n");
				exit (1);
			}
		}
	} else {
		assembly = mono_domain_assembly_open_internal (main_args->domain, mono_domain_default_alc (main_args->domain), main_args->file);
		if (!assembly){
			fprintf (stderr, "Can not open image %s\n", main_args->file);
			exit (1);
		}

		/* 
		 * This must be done in a thread managed by mono since it can invoke
		 * managed code.
		 */
		if (main_args->opts & MONO_OPT_PRECOMP)
			mono_precompile_assemblies ();

		mono_jit_exec (main_args->domain, assembly, main_args->argc, main_args->argv);
	}
}

static int
load_agent (MonoDomain *domain, char *desc)
{
	ERROR_DECL (error);
	char* col = strchr (desc, ':');	
	char *agent, *args;
	MonoAssembly *agent_assembly;
	MonoImage *image;
	MonoMethod *method;
	guint32 entry;
	MonoArray *main_args;
	gpointer pa [1];
	MonoImageOpenStatus open_status;

	if (col) {
		agent = (char *)g_memdup (desc, col - desc + 1);
		agent [col - desc] = '\0';
		args = col + 1;
	} else {
		agent = g_strdup (desc);
		args = NULL;
	}

	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, mono_domain_default_alc (mono_get_root_domain ()));
	agent_assembly = mono_assembly_request_open (agent, &req, &open_status);
	if (!agent_assembly) {
		fprintf (stderr, "Cannot open agent assembly '%s': %s.\n", agent, mono_image_strerror (open_status));
		g_free (agent);
		return 2;
	}

	/* 
	 * Can't use mono_jit_exec (), as it sets things which might confuse the
	 * real Main method.
	 */
	image = mono_assembly_get_image_internal (agent_assembly);
	entry = mono_image_get_entry_point (image);
	if (!entry) {
		g_print ("Assembly '%s' doesn't have an entry point.\n", mono_image_get_filename (image));
		g_free (agent);
		return 1;
	}

	method = mono_get_method_checked (image, entry, NULL, NULL, error);
	if (method == NULL){
		g_print ("The entry point method of assembly '%s' could not be loaded due to %s\n", agent, mono_error_get_message (error));
		mono_error_cleanup (error);
		g_free (agent);
		return 1;
	}
	
	mono_thread_set_main (mono_thread_current ());

	if (args) {
		main_args = (MonoArray*)mono_array_new_checked (mono_defaults.string_class, 1, error);
		if (main_args) {
			MonoString *str = mono_string_new_checked (args, error);
			if (str)
				mono_array_set_internal (main_args, MonoString*, 0, str);
		}
	} else {
		main_args = (MonoArray*)mono_array_new_checked (mono_defaults.string_class, 0, error);
	}
	if (!main_args) {
		g_print ("Could not allocate array for main args of assembly '%s' due to %s\n", agent, mono_error_get_message (error));
		mono_error_cleanup (error);
		g_free (agent);
		return 1;
	}
	

	pa [0] = main_args;
	/* Pass NULL as 'exc' so unhandled exceptions abort the runtime */
	mono_runtime_invoke_checked (method, NULL, pa, error);
	if (!is_ok (error)) {
		g_print ("The entry point method of assembly '%s' could not execute due to %s\n", agent, mono_error_get_message (error));
		mono_error_cleanup (error);
		g_free (agent);
		return 1;
	}

	g_free (agent);
	return 0;
}

static void
mini_usage_jitdeveloper (void)
{
	int i;
	
	fprintf (stdout,
		 "Runtime and JIT debugging options:\n"
		 "    --apply-bindings=FILE  Apply assembly bindings from FILE (only for AOT)\n"
		 "    --breakonex            Inserts a breakpoint on exceptions\n"
		 "    --break METHOD         Inserts a breakpoint at METHOD entry\n"
		 "    --break-at-bb METHOD N Inserts a breakpoint in METHOD at BB N\n"
		 "    --compile METHOD       Just compile METHOD in assembly\n"
		 "    --compile-all=N        Compiles all the methods in the assembly multiple times (default: 1)\n"
		 "    --ncompile N           Number of times to compile METHOD (default: 1)\n"
		 "    --print-vtable         Print the vtable of all used classes\n"
		 "    --regression           Runs the regression test contained in the assembly\n"
		 "    --single-method=OPTS   Runs regressions with only one method optimized with OPTS at any time\n"
		 "    --statfile FILE        Sets the stat file to FILE\n"
		 "    --stats                Print statistics about the JIT operations\n"
		 "    --inject-async-exc METHOD OFFSET Inject an asynchronous exception at METHOD\n"
		 "    --verify-all           Run the verifier on all assemblies and methods\n"
		 "    --full-aot             Avoid JITting any code\n"
		 "    --llvmonly             Use LLVM compiled code only\n"
		 "    --agent=ASSEMBLY[:ARG] Loads the specific agent assembly and executes its Main method with the given argument before loading the main assembly.\n"
		 "    --no-x86-stack-align   Don't align stack on x86\n"
		 "\n"
		 "The options supported by MONO_DEBUG can also be passed on the command line.\n"
		 "\n"
		 "Other options:\n" 
		 "    --graph[=TYPE] METHOD  Draws a graph of the specified method:\n");
	
	for (i = 0; i < G_N_ELEMENTS (graph_names); ++i) {
		fprintf (stdout, "                           %-10s %s\n", graph_names [i].name, graph_names [i].desc);
	}
}

static void
mini_usage_list_opt (void)
{
	int i;
	
	for (i = 0; i < G_N_ELEMENTS (opt_names); ++i)
		fprintf (stdout, "                           %-10s %s\n", optflag_get_name (i), optflag_get_desc (i));
}

static void
mini_usage (void)
{
	fprintf (stdout,
		"Usage is: mono [options] program [program-options]\n"
		"\n"
		"Development:\n"
		"    --aot[=<options>]      Compiles the assembly to native code\n"
		"    --debug=ignore         Disable debugging support (on by default)\n"
		"    --debug=[<options>]    Disable debugging support or enable debugging extras, use --help-debug for details\n"
 		"    --debugger-agent=options Enable the debugger agent\n"
		"    --profile[=profiler]   Runs in profiling mode with the specified profiler module\n"
		"    --trace[=EXPR]         Enable tracing, use --help-trace for details\n"
#ifdef __linux__		
		"    --jitmap               Output a jit method map to /tmp/perf-PID.map\n"
#endif
#ifdef ENABLE_JIT_DUMP
		"    --jitdump              Output a jitdump file to /tmp/jit-PID.dump\n"
#endif
		"    --help-devel           Shows more options available to developers\n"
		"\n"
		"Runtime:\n"
		"    --config FILE          Loads FILE as the Mono config\n"
		"    --verbose, -v          Increases the verbosity level\n"
		"    --help, -h             Show usage information\n"
		"    --version, -V          Show version information\n"
		"    --version=number       Show version number\n"
		"    --runtime=VERSION      Use the VERSION runtime, instead of autodetecting\n"
		"    --optimize=OPT         Turns on or off a specific optimization\n"
		"                           Use --list-opt to get a list of optimizations\n"
		"    --attach=OPTIONS       Pass OPTIONS to the attach agent in the runtime.\n"
		"                           Currently the only supported option is 'disable'.\n"
		"    --llvm, --nollvm       Controls whenever the runtime uses LLVM to compile code.\n"
	        "    --gc=[sgen,boehm]      Select SGen or Boehm GC (runs mono or mono-sgen)\n"
#ifdef TARGET_OSX
		"    --arch=[32,64]         Select architecture (runs mono32 or mono64)\n"
#endif
#ifdef HOST_WIN32
	        "    --mixed-mode           Enable mixed-mode image support.\n"
#endif
		"    --handlers             Install custom handlers, use --help-handlers for details.\n"
		"    --aot-path=PATH        List of additional directories to search for AOT images.\n"
	  );

	g_print ("\nOptions:\n");
	mono_options_print_usage ();
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
		 "    wrapper              All wrappers bridging native and managed code\n"
		 "    M:Type:Method        Specifies a method\n"
		 "    N:Namespace          Specifies a namespace\n"
		 "    T:Type               Specifies a type\n"
		 "    E:Type               Specifies stack traces for an exception type\n"
		 "    EXPR                 Includes expression\n"
		 "    -EXPR                Excludes expression\n"
		 "    EXPR,EXPR            Multiple expressions\n"
		 "    disabled             Don't print any output until toggled via SIGUSR2\n");
}

static void
mini_debug_usage (void)
{
	fprintf (stdout,
		 "Debugging options:\n"
		 "   --debug[=OPTIONS]     Disable debugging support or enable debugging extras, optional OPTIONS is a comma\n"
		 "                         separated list of options\n"
		 "\n"
		 "OPTIONS is composed of:\n"
		 "    ignore               Disable debugging support (on by default).\n"
		 "    casts                Enable more detailed InvalidCastException messages.\n"
		 "    mdb-optimizations    Disable some JIT optimizations which are normally\n"
		 "                         disabled when running inside the debugger.\n"
		 "                         This is useful if you plan to attach to the running\n"
		 "                         process with the debugger.\n");
}

#if defined(MONO_ARCH_ARCHITECTURE)
/* Redefine MONO_ARCHITECTURE to include more information */
#undef MONO_ARCHITECTURE
#define MONO_ARCHITECTURE MONO_ARCH_ARCHITECTURE
#endif

static char *
mono_get_version_info (void) 
{
	GString *output;
	output = g_string_new ("");

#ifdef MONO_KEYWORD_THREAD
	g_string_append_printf (output, "\tTLS:           __thread\n");
#else
	g_string_append_printf (output, "\tTLS:           \n");
#endif /* MONO_KEYWORD_THREAD */

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	g_string_append_printf (output, "\tSIGSEGV:       altstack\n");
#else
	g_string_append_printf (output, "\tSIGSEGV:       normal\n");
#endif

#ifdef HAVE_EPOLL
	g_string_append_printf (output, "\tNotifications: epoll\n");
#elif defined(HAVE_KQUEUE)
	g_string_append_printf (output, "\tNotification:  kqueue\n");
#else
	g_string_append_printf (output, "\tNotification:  Thread + polling\n");
#endif

	g_string_append_printf (output, "\tArchitecture:  %s\n", MONO_ARCHITECTURE);
	g_string_append_printf (output, "\tDisabled:      %s\n", DISABLED_FEATURES);

	g_string_append_printf (output, "\tMisc:          ");
#ifdef MONO_SMALL_CONFIG
	g_string_append_printf (output, "smallconfig ");
#endif

#ifdef MONO_BIG_ARRAYS
	g_string_append_printf (output, "bigarrays ");
#endif

#if !defined(DISABLE_SDB)
	g_string_append_printf (output, "softdebug ");
#endif
	g_string_append_printf (output, "\n");

#ifndef DISABLE_INTERPRETER
	g_string_append_printf (output, "\tInterpreter:   yes\n");
#else
	g_string_append_printf (output, "\tInterpreter:   no\n");
#endif

#ifdef MONO_ARCH_LLVM_SUPPORTED
#ifdef ENABLE_LLVM
	g_string_append_printf (output, "\tLLVM:          yes(%d)\n", LLVM_API_VERSION);
#else
	g_string_append_printf (output, "\tLLVM:          supported, not enabled.\n");
#endif
#endif

	mono_threads_suspend_policy_init ();
	g_string_append_printf (output, "\tSuspend:       %s\n", mono_threads_suspend_policy_name (mono_threads_suspend_policy ()));

	return g_string_free (output, FALSE);
}

#ifndef MONO_ARCH_AOT_SUPPORTED
#define error_if_aot_unsupported() do {fprintf (stderr, "AOT compilation is not supported on this platform.\n"); exit (1);} while (0)
#else
#define error_if_aot_unsupported()
#endif

static gboolean enable_debugging;

static void
enable_runtime_stats (void)
{
	mono_counters_enable (-1);
	mono_atomic_store_bool (&mono_stats.enabled, TRUE);
	mono_atomic_store_bool (&mono_jit_stats.enabled, TRUE);
}

static MonoMethodDesc *
parse_qualified_method_name (char *method_name)
{
	if (strlen (method_name) == 0) {
		g_printerr ("Couldn't parse empty method name.");
		exit (1);
	}
	MonoMethodDesc *result = mono_method_desc_new (method_name, TRUE);
	if (!result) {
		g_printerr ("Couldn't parse method name: %s\n", method_name);
		exit (1);
	}
	return result;
}

/**
 * mono_jit_parse_options:
 *
 * Process the command line options in \p argv as done by the runtime executable.
 * This should be called before \c mono_jit_init.
 */
void
mono_jit_parse_options (int argc, char * argv[])
{
	int i;
	char *trace_options = NULL;
	int mini_verbose_level = 0;
	guint32 opt;

	/* 
	 * Some options have no effect here, since they influence the behavior of 
	 * mono_main ().
	 */

	opt = mono_parse_default_optimizations (NULL);

	/* FIXME: Avoid code duplication */
	for (i = 0; i < argc; ++i) {
		if (argv [i] [0] != '-')
			break;
		if (strncmp (argv [i], "--debugger-agent=", 17) == 0) {
			MonoDebugOptions *opt = mini_get_debug_options ();

			sdb_options = g_strdup (argv [i] + 17);
			opt->mdb_optimizations = TRUE;
			enable_debugging = TRUE;
		} else if (!strcmp (argv [i], "--soft-breakpoints")) {
			MonoDebugOptions *opt = mini_get_debug_options ();

			opt->soft_breakpoints = TRUE;
			opt->explicit_null_checks = TRUE;
		} else if (strncmp (argv [i], "--optimize=", 11) == 0) {
			opt = parse_optimizations (opt, argv [i] + 11, TRUE);
			mono_set_optimizations (opt);
		} else if (strncmp (argv [i], "-O=", 3) == 0) {
			opt = parse_optimizations (opt, argv [i] + 3, TRUE);
			mono_set_optimizations (opt);
		} else if (strcmp (argv [i], "--trace") == 0) {
			trace_options = (char*)"";
		} else if (strncmp (argv [i], "--trace=", 8) == 0) {
			trace_options = &argv [i][8];
		} else if (strcmp (argv [i], "--verbose") == 0 || strcmp (argv [i], "-v") == 0) {
			mini_verbose_level++;
		} else if (strcmp (argv [i], "--breakonex") == 0) {
			MonoDebugOptions *opt = mini_get_debug_options ();

			opt->break_on_exc = TRUE;
		} else if (strcmp (argv [i], "--stats") == 0) {
			enable_runtime_stats ();
		} else if (strncmp (argv [i], "--stats=", 8) == 0) {
			enable_runtime_stats ();
			if (mono_stats_method_desc)
				g_free (mono_stats_method_desc);
			mono_stats_method_desc = parse_qualified_method_name (argv [i] + 8);
		} else if (strcmp (argv [i], "--break") == 0) {
			if (i+1 >= argc){
				fprintf (stderr, "Missing method name in --break command line option\n");
				exit (1);
			}
			
			if (!mono_debugger_insert_breakpoint (argv [++i], FALSE))
				fprintf (stderr, "Error: invalid method name '%s'\n", argv [i]);
		} else if (strncmp (argv[i], "--gc-params=", 12) == 0) {
			mono_gc_params_set (argv[i] + 12);
		} else if (strncmp (argv[i], "--gc-debug=", 11) == 0) {
			mono_gc_debug_set (argv[i] + 11);
		} else if (strcmp (argv [i], "--llvm") == 0) {
#ifndef MONO_ARCH_LLVM_SUPPORTED
			fprintf (stderr, "Mono Warning: --llvm not supported on this platform.\n");
#elif !defined(ENABLE_LLVM)
			fprintf (stderr, "Mono Warning: --llvm not enabled in this runtime.\n");
#else
			mono_use_llvm = TRUE;
#endif
		} else if (strcmp (argv [i], "--profile") == 0) {
			mini_add_profiler_argument (NULL);
		} else if (strncmp (argv [i], "--profile=", 10) == 0) {
			mini_add_profiler_argument (argv [i] + 10);
		} else if (argv [i][0] == '-' && argv [i][1] == '-' && mini_parse_debug_option (argv [i] + 2)) {
		} else {
			fprintf (stderr, "Unsupported command line option: '%s'\n", argv [i]);
			exit (1);
		}
	}

	if (trace_options != NULL) {
		/* 
		 * Need to call this before mini_init () so we can trace methods 
		 * compiled there too.
		 */
		mono_jit_trace_calls = mono_trace_set_options (trace_options);
		if (mono_jit_trace_calls == NULL)
			exit (1);
	}

	if (mini_verbose_level)
		mono_set_verbose_level (mini_verbose_level);
}

static void
mono_set_use_smp (int use_smp)
{
#if HAVE_SCHED_SETAFFINITY
	if (!use_smp) {
		unsigned long proc_mask = 1;
#ifdef GLIBC_BEFORE_2_3_4_SCHED_SETAFFINITY
		sched_setaffinity (getpid(), (gpointer)&proc_mask);
#else
		sched_setaffinity (getpid(), sizeof (unsigned long), (const cpu_set_t *)&proc_mask);
#endif
	}
#endif
}

static void
switch_gc (char* argv[], const char* target_gc)
{
	GString *path;

	if (!strcmp (mono_gc_get_gc_name (), target_gc)) {
		return;
	}

	path = g_string_new (argv [0]);

	/*Running mono without any argument*/
	if (strstr (argv [0], "-sgen"))
		g_string_truncate (path, path->len - 5);
	else if (strstr (argv [0], "-boehm"))
		g_string_truncate (path, path->len - 6);

	g_string_append_c (path, '-');
	g_string_append (path, target_gc);

#ifdef HAVE_EXECVP
	execvp (path->str, argv);
	fprintf (stderr, "Error: Failed to switch to %s gc. mono-%s is not installed.\n", target_gc, target_gc);
#else
	fprintf (stderr, "Error: --gc=<NAME> option not supported on this platform.\n");
#endif
}

#ifdef TARGET_OSX

/*
 * tries to increase the minimum number of files, if the number is below 1024
 */
static void
darwin_change_default_file_handles ()
{
	struct rlimit limit;
	
	if (getrlimit (RLIMIT_NOFILE, &limit) == 0){
		if (limit.rlim_cur < 1024){
			limit.rlim_cur = MAX(1024,limit.rlim_cur);
			setrlimit (RLIMIT_NOFILE, &limit);
		}
	}
}

static void
switch_arch (char* argv[], const char* target_arch)
{
	GString *path;
	gsize arch_offset;

	if ((strcmp (target_arch, "32") == 0 && strcmp (MONO_ARCHITECTURE, "x86") == 0) ||
		(strcmp (target_arch, "64") == 0 && strcmp (MONO_ARCHITECTURE, "amd64") == 0)) {
		return; /* matching arch loaded */
	}

	path = g_string_new (argv [0]);
	arch_offset = path->len -2; /* last two characters */

	/* Remove arch suffix if present */
	if (strstr (&path->str[arch_offset], "32") || strstr (&path->str[arch_offset], "64")) {
		g_string_truncate (path, arch_offset);
	}

	g_string_append (path, target_arch);

	if (execvp (path->str, argv) < 0) {
		fprintf (stderr, "Error: --arch=%s Failed to switch to '%s'.\n", target_arch, path->str);
		exit (1);
	}
}

#endif

#define MONO_HANDLERS_ARGUMENT "--handlers="
#define MONO_HANDLERS_ARGUMENT_LEN G_N_ELEMENTS(MONO_HANDLERS_ARGUMENT)-1

static void
apply_root_domain_configuration_file_bindings (MonoDomain *domain, char *root_domain_configuration_file)
{
	g_assert_not_reached ();
}

static void
mono_check_interp_supported (void)
{
#ifdef MONO_CROSS_COMPILE
	g_error ("--interpreter on cross-compile runtimes not supported\n");
#endif

#ifndef MONO_ARCH_INTERPRETER_SUPPORTED
	g_error ("--interpreter not supported on this architecture.\n");
#endif
}

static int
mono_exec_regression_internal (int verbose_level, int count, char *images [], gboolean single_method)
{
	mono_do_single_method_regression = single_method;
	if (mono_use_interpreter) {
		if (mono_interp_regression_list (verbose_level, count, images)) {
			g_print ("Regression ERRORS!\n");
			return 1;
		}
		return 0;
	}
	if (mini_regression_list (verbose_level, count, images)) {
		g_print ("Regression ERRORS!\n");
		return 1;
	}
	return 0;
}


/**
 * Returns TRUE for success, FALSE for failure.
 */
gboolean
mono_regression_test_step (int verbose_level, const char *image, const char *method_name)
{
	if (method_name) {
		//TODO
	} else {
		do_regression_retries = TRUE;
	}

	char *images[] = {
		(char*)image,
		NULL
	};

	return mono_exec_regression_internal (verbose_level, 1, images, FALSE) == 0;
}

#ifdef ENABLE_ICALL_SYMBOL_MAP
/* Print the icall table as JSON */
static void
print_icall_table (void)
{
	// We emit some dummy values to make the code simpler

	printf ("[\n{ \"klass\": \"\", \"icalls\": [");
#define NOHANDLES(inner) inner
#define HANDLES(id, name, func, ...)	printf ("\t,{ \"name\": \"%s\", \"func\": \"%s_raw\", \"handles\": true }\n", name, #func);
#define HANDLES_REUSE_WRAPPER		HANDLES
#define MONO_HANDLE_REGISTER_ICALL(...) /* nothing  */
#define ICALL_TYPE(id,name,first) printf ("]},\n { \"klass\":\"%s\", \"icalls\": [{} ", name);
#define ICALL(id,name,func) printf ("\t,{ \"name\": \"%s\", \"func\": \"%s\", \"handles\": false }\n", name, #func);
#include <mono/metadata/icall-def.h>

	printf ("]}\n]\n");
}
#endif

/**
 * mono_main:
 * \param argc number of arguments in the argv array
 * \param argv array of strings containing the startup arguments
 * Launches the Mono JIT engine and parses all the command line options
 * in the same way that the mono command line VM would.
 */
int
mono_main (int argc, char* argv[])
{
	MainThreadArgs main_args;
	MonoAssembly *assembly;
	MonoMethodDesc *desc;
	MonoMethod *method;
	MonoDomain *domain;
	MonoImageOpenStatus open_status;
	const char* aname, *mname = NULL;
	char *config_file = NULL;
	int i, count = 1;
	guint32 opt, action = DO_EXEC, recompilation_times = 1;
	MonoGraphOptions mono_graph_options = (MonoGraphOptions)0;
	int mini_verbose_level = 0;
	char *trace_options = NULL;
	char *aot_options = NULL;
	char *forced_version = NULL;
	GPtrArray *agents = NULL;
	char *extra_bindings_config_file = NULL;
#ifdef MONO_JIT_INFO_TABLE_TEST
	int test_jit_info_table = FALSE;
#endif
#ifdef HOST_WIN32
	int mixed_mode = FALSE;
#endif
	ERROR_DECL (error);

#ifdef MOONLIGHT
#ifndef HOST_WIN32
	/* stdout defaults to block buffering if it's not writing to a terminal, which
	 * happens with our test harness: we redirect stdout to capture it. Force line
	 * buffering in all cases. */
	setlinebuf (stdout);
#endif
#endif

	setlocale (LC_ALL, "");

#if TARGET_OSX
	darwin_change_default_file_handles ();
#endif

	if (g_hasenv ("MONO_NO_SMP"))
		mono_set_use_smp (FALSE);

#ifdef MONO_JEMALLOC_ENABLED

	gboolean use_jemalloc = FALSE;
#ifdef MONO_JEMALLOC_DEFAULT
	use_jemalloc = TRUE;
#endif
	if (!use_jemalloc)
		use_jemalloc = g_hasenv ("MONO_USE_JEMALLOC");

	if (use_jemalloc)
		mono_init_jemalloc ();

#endif
	
	g_log_set_always_fatal (G_LOG_LEVEL_ERROR);
	g_log_set_fatal_mask (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR);

	opt = mono_parse_default_optimizations (NULL);

	enable_debugging = TRUE;

	mono_options_parse_options ((const char**)argv + 1, argc - 1, &argc, error);
	argc ++;
	if (!is_ok (error)) {
		g_printerr ("%s", mono_error_get_message (error));
		mono_error_cleanup (error);
		return 1;
	}

	for (i = 1; i < argc; ++i) {
		if (argv [i] [0] != '-')
			break;
		if (strcmp (argv [i], "--regression") == 0) {
			action = DO_REGRESSION;
		} else if (strncmp (argv [i], "--single-method=", 16) == 0) {
			char *full_opts = g_strdup_printf ("-all,%s", argv [i] + 16);
			action = DO_SINGLE_METHOD_REGRESSION;
			mono_single_method_regression_opt = parse_optimizations (opt, full_opts, TRUE);
			g_free (full_opts);
		} else if (strcmp (argv [i], "--verbose") == 0 || strcmp (argv [i], "-v") == 0) {
			mini_verbose_level++;
		} else if (strcmp (argv [i], "--version=number") == 0) {
			g_print ("%s\n", VERSION);
			return 0;
		} else if (strcmp (argv [i], "--version") == 0 || strcmp (argv [i], "-V") == 0) {
			char *build = mono_get_runtime_build_info ();
			char *gc_descr;

			g_print ("Mono JIT compiler version %s\nCopyright (C) Novell, Inc, Xamarin Inc and Contributors. www.mono-project.com\n", build);
			g_free (build);
			char *info = mono_get_version_info ();
			g_print (info);
			g_free (info);

			gc_descr = mono_gc_get_description ();
			g_print ("\tGC:            %s\n", gc_descr);
			g_free (gc_descr);
			return 0;
		} else if (strcmp (argv [i], "--help") == 0 || strcmp (argv [i], "-h") == 0) {
			mini_usage ();
			return 0;
		} else if (strcmp (argv [i], "--help-trace") == 0){
			mini_trace_usage ();
			return 0;
		} else if (strcmp (argv [i], "--help-devel") == 0){
			mini_usage_jitdeveloper ();
			return 0;
		} else if (strcmp (argv [i], "--help-debug") == 0){
			mini_debug_usage ();
			return 0;
		} else if (strcmp (argv [i], "--list-opt") == 0){
			mini_usage_list_opt ();
			return 0;
		} else if (strncmp (argv [i], "--statfile", 10) == 0) {
			if (i + 1 >= argc){
				fprintf (stderr, "error: --statfile requires a filename argument\n");
				return 1;
			}
			mini_stats_fd = fopen (argv [++i], "w+");
		} else if (strncmp (argv [i], "--optimize=", 11) == 0) {
			opt = parse_optimizations (opt, argv [i] + 11, TRUE);
		} else if (strncmp (argv [i], "-O=", 3) == 0) {
			opt = parse_optimizations (opt, argv [i] + 3, TRUE);
		} else if (strncmp (argv [i], "--bisect=", 9) == 0) {
			char *param = argv [i] + 9;
			char *sep = strchr (param, ':');
			if (!sep) {
				fprintf (stderr, "Error: --bisect requires OPT:FILENAME\n");
				return 1;
			}
			char *opt_string = g_strndup (param, sep - param);
			guint32 opt = parse_optimizations (0, opt_string, FALSE);
			g_free (opt_string);
			mono_set_bisect_methods (opt, sep + 1);
		} else if (strcmp (argv [i], "--gc=sgen") == 0) {
			switch_gc (argv, "sgen");
		} else if (strcmp (argv [i], "--gc=boehm") == 0) {
			switch_gc (argv, "boehm");
		} else if (strncmp (argv[i], "--gc-params=", 12) == 0) {
			mono_gc_params_set (argv[i] + 12);
		} else if (strncmp (argv[i], "--gc-debug=", 11) == 0) {
			mono_gc_debug_set (argv[i] + 11);
		}
#ifdef TARGET_OSX
		else if (strcmp (argv [i], "--arch=32") == 0) {
			switch_arch (argv, "32");
		} else if (strcmp (argv [i], "--arch=64") == 0) {
			switch_arch (argv, "64");
		}
#endif
		else if (strcmp (argv [i], "--config") == 0) {
			if (i +1 >= argc){
				fprintf (stderr, "error: --config requires a filename argument\n");
				return 1;
			}
			config_file = argv [++i];
#ifdef HOST_WIN32
		} else if (strcmp (argv [i], "--mixed-mode") == 0) {
			mixed_mode = TRUE;
#endif
		} else if (strcmp (argv [i], "--ncompile") == 0) {
			if (i + 1 >= argc){
				fprintf (stderr, "error: --ncompile requires an argument\n");
				return 1;
			}
			count = atoi (argv [++i]);
			action = DO_BENCH;
		} else if (strcmp (argv [i], "--trace") == 0) {
			trace_options = (char*)"";
		} else if (strncmp (argv [i], "--trace=", 8) == 0) {
			trace_options = &argv [i][8];
		} else if (strcmp (argv [i], "--breakonex") == 0) {
			MonoDebugOptions *opt = mini_get_debug_options ();

			opt->break_on_exc = TRUE;
		} else if (strcmp (argv [i], "--break") == 0) {
			if (i+1 >= argc){
				fprintf (stderr, "Missing method name in --break command line option\n");
				return 1;
			}
			
			if (!mono_debugger_insert_breakpoint (argv [++i], FALSE))
				fprintf (stderr, "Error: invalid method name '%s'\n", argv [i]);
		} else if (strcmp (argv [i], "--break-at-bb") == 0) {
			if (i + 2 >= argc) {
				fprintf (stderr, "Missing method name or bb num in --break-at-bb command line option.");
				return 1;
			}
			mono_break_at_bb_method = mono_method_desc_new (argv [++i], TRUE);
			if (mono_break_at_bb_method == NULL) {
				fprintf (stderr, "Method name is in a bad format in --break-at-bb command line option.");
				return 1;
			}
			mono_break_at_bb_bb_num = atoi (argv [++i]);
		} else if (strcmp (argv [i], "--inject-async-exc") == 0) {
			if (i + 2 >= argc) {
				fprintf (stderr, "Missing method name or position in --inject-async-exc command line option\n");
				return 1;
			}
			mono_inject_async_exc_method = mono_method_desc_new (argv [++i], TRUE);
			if (mono_inject_async_exc_method == NULL) {
				fprintf (stderr, "Method name is in a bad format in --inject-async-exc command line option\n");
				return 1;
			}
			mono_inject_async_exc_pos = atoi (argv [++i]);
		} else if (strcmp (argv [i], "--verify-all") == 0) {
			g_warning ("--verify-all is obsolete, ignoring");
		} else if (strcmp (argv [i], "--full-aot") == 0) {
			mono_jit_set_aot_mode (MONO_AOT_MODE_FULL);
		} else if (strcmp (argv [i], "--llvmonly") == 0) {
			mono_jit_set_aot_mode (MONO_AOT_MODE_LLVMONLY);
		} else if (strcmp (argv [i], "--hybrid-aot") == 0) {
			mono_jit_set_aot_mode (MONO_AOT_MODE_HYBRID);
		} else if (strcmp (argv [i], "--full-aot-interp") == 0) {
			mono_jit_set_aot_mode (MONO_AOT_MODE_INTERP);
		} else if (strcmp (argv [i], "--llvmonly-interp") == 0) {
			mono_jit_set_aot_mode (MONO_AOT_MODE_LLVMONLY_INTERP);
		} else if (strcmp (argv [i], "--print-vtable") == 0) {
			mono_print_vtable = TRUE;
		} else if (strcmp (argv [i], "--stats") == 0) {
			enable_runtime_stats ();
		} else if (strncmp (argv [i], "--stats=", 8) == 0) {
			enable_runtime_stats ();
			if (mono_stats_method_desc)
				g_free (mono_stats_method_desc);
			mono_stats_method_desc = parse_qualified_method_name (argv [i] + 8);
#ifndef DISABLE_AOT
		} else if (strcmp (argv [i], "--aot") == 0) {
			error_if_aot_unsupported ();
			mono_compile_aot = TRUE;
		} else if (strncmp (argv [i], "--aot=", 6) == 0) {
			error_if_aot_unsupported ();
			mono_compile_aot = TRUE;
			if (aot_options) {
				char *tmp = g_strdup_printf ("%s,%s", aot_options, &argv [i][6]);
				g_free (aot_options);
				aot_options = tmp;
			} else {
				aot_options = g_strdup (&argv [i][6]);
			}
#endif
		} else if (strncmp (argv [i], "--apply-bindings=", 17) == 0) {
			extra_bindings_config_file = &argv[i][17];
		} else if (strncmp (argv [i], "--aot-path=", 11) == 0) {
			char **splitted;

			splitted = g_strsplit (argv [i] + 11, G_SEARCHPATH_SEPARATOR_S, 1000);
			while (*splitted) {
				char *tmp = *splitted;
				mono_aot_paths = g_list_append (mono_aot_paths, g_strdup (tmp));
				g_free (tmp);
				splitted++;
			}
		} else if (strncmp (argv [i], "--compile-all=", 14) == 0) {
			action = DO_COMPILE;
			recompilation_times = atoi (argv [i] + 14);
		} else if (strcmp (argv [i], "--compile-all") == 0) {
			action = DO_COMPILE;
		} else if (strncmp (argv [i], "--runtime=", 10) == 0) {
			forced_version = &argv [i][10];
		} else if (strcmp (argv [i], "--jitmap") == 0) {
			mono_enable_jit_map ();
#ifdef ENABLE_JIT_DUMP
		} else if (strcmp (argv [i], "--jitdump") == 0) {
			mono_enable_jit_dump ();
#endif
		} else if (strcmp (argv [i], "--profile") == 0) {
			mini_add_profiler_argument (NULL);
		} else if (strncmp (argv [i], "--profile=", 10) == 0) {
			mini_add_profiler_argument (argv [i] + 10);
		} else if (strncmp (argv [i], "--agent=", 8) == 0) {
			if (agents == NULL)
				agents = g_ptr_array_new ();
			g_ptr_array_add (agents, argv [i] + 8);
		} else if (strncmp (argv [i], "--attach=", 9) == 0) {
			g_warning ("--attach= option no longer supported.");
		} else if (strcmp (argv [i], "--compile") == 0) {
			if (i + 1 >= argc){
				fprintf (stderr, "error: --compile option requires a method name argument\n");
				return 1;
			}
			
			mname = argv [++i];
			action = DO_BENCH;
		} else if (strncmp (argv [i], "--graph=", 8) == 0) {
			if (i + 1 >= argc){
				fprintf (stderr, "error: --graph option requires a method name argument\n");
				return 1;
			}
			
			mono_graph_options = mono_parse_graph_options (argv [i] + 8);
			mname = argv [++i];
			action = DO_DRAW;
		} else if (strcmp (argv [i], "--graph") == 0) {
			if (i + 1 >= argc){
				fprintf (stderr, "error: --graph option requires a method name argument\n");
				return 1;
			}
			
			mname = argv [++i];
			mono_graph_options = MONO_GRAPH_CFG;
			action = DO_DRAW;
		} else if (strcmp (argv [i], "--debug") == 0) {
			enable_debugging = TRUE;
		} else if (strncmp (argv [i], "--debug=", 8) == 0) {
			enable_debugging = TRUE;
			if (!parse_debug_options (argv [i] + 8))
				return 1;
			MonoDebugOptions *opt = mini_get_debug_options ();

			if (!opt->enabled) {
				enable_debugging = FALSE;
			}
		} else if (strncmp (argv [i], "--debugger-agent=", 17) == 0) {
			MonoDebugOptions *opt = mini_get_debug_options ();

			sdb_options = g_strdup (argv [i] + 17);
			opt->mdb_optimizations = TRUE;
			enable_debugging = TRUE;
		} else if (strcmp (argv [i], "--security") == 0) {
			fprintf (stderr, "error: --security is obsolete.");
			return 1;
		} else if (strncmp (argv [i], "--security=", 11) == 0) {
			if (strcmp (argv [i] + 11, "core-clr") == 0) {
				fprintf (stderr, "error: --security=core-clr is obsolete.");
				return 1;
			} else if (strcmp (argv [i] + 11, "core-clr-test") == 0) {
				fprintf (stderr, "error: --security=core-clr-test is obsolete.");
				return 1;
			} else if (strcmp (argv [i] + 11, "cas") == 0) {
				fprintf (stderr, "error: --security=cas is obsolete.");
				return 1;
			} else if (strcmp (argv [i] + 11, "validil") == 0) {
                                fprintf (stderr, "error: --security=validil is obsolete.");
                                return 1;
			} else if (strcmp (argv [i] + 11, "verifiable") == 0) {
                                fprintf (stderr, "error: --securty=verifiable is obsolete.");
                                return 1;
			} else {
				fprintf (stderr, "error: --security= option has invalid argument (cas, core-clr, verifiable or validil)\n");
				return 1;
			}
		} else if (strcmp (argv [i], "--desktop") == 0) {
			mono_gc_set_desktop_mode ();
			/* Put more desktop-specific optimizations here */
		} else if (strcmp (argv [i], "--server") == 0){
			mono_config_set_server_mode (TRUE);
			/* Put more server-specific optimizations here */
		} else if (strcmp (argv [i], "--inside-mdb") == 0) {
			action = DO_DEBUGGER;
		} else if (strncmp (argv [i], "--wapi=", 7) == 0) {
			fprintf (stderr, "--wapi= option no longer supported\n.");
			return 1;
		} else if (strcmp (argv [i], "--no-x86-stack-align") == 0) {
			mono_do_x86_stack_align = FALSE;
#ifdef MONO_JIT_INFO_TABLE_TEST
		} else if (strcmp (argv [i], "--test-jit-info-table") == 0) {
			test_jit_info_table = TRUE;
#endif
		} else if (strcmp (argv [i], "--llvm") == 0) {
#ifndef MONO_ARCH_LLVM_SUPPORTED
			fprintf (stderr, "Mono Warning: --llvm not supported on this platform.\n");
#elif !defined(ENABLE_LLVM)
			fprintf (stderr, "Mono Warning: --llvm not enabled in this runtime.\n");
#else
			mono_use_llvm = TRUE;
#endif
		} else if (strcmp (argv [i], "--nollvm") == 0){
			mono_use_llvm = FALSE;
		} else if (strcmp (argv [i], "--ffast-math") == 0){
			mono_use_fast_math = TRUE;
		} else if ((strcmp (argv [i], "--interpreter") == 0) || !strcmp (argv [i], "--interp")) {
			mono_runtime_set_execution_mode (MONO_EE_MODE_INTERP);
		} else if (strncmp (argv [i], "--interp=", 9) == 0) {
			mono_runtime_set_execution_mode_full (MONO_EE_MODE_INTERP, FALSE);
			mono_interp_opts_string = argv [i] + 9;
		} else if (strcmp (argv [i], "--print-icall-table") == 0) {
#ifdef ENABLE_ICALL_SYMBOL_MAP
			print_icall_table ();
			exit (0);
#else
			fprintf (stderr, "--print-icall-table requires a runtime configured with the --enable-icall-symbol-map option.\n");
			exit (1);
#endif
		} else if (strncmp (argv [i], "--assembly-loader=", strlen("--assembly-loader=")) == 0) {
			gchar *arg = argv [i] + strlen ("--assembly-loader=");
			if (strcmp (arg, "strict") == 0)
				mono_loader_set_strict_assembly_name_check (TRUE);
			else if (strcmp (arg, "legacy") == 0)
				mono_loader_set_strict_assembly_name_check (FALSE);
			else
				fprintf (stderr, "Warning: unknown argument to --assembly-loader. Should be \"strict\" or \"legacy\"\n");
		} else if (strncmp (argv [i], MONO_HANDLERS_ARGUMENT, MONO_HANDLERS_ARGUMENT_LEN) == 0) {
			//Install specific custom handlers.
			if (!mono_runtime_install_custom_handlers (argv[i] + MONO_HANDLERS_ARGUMENT_LEN)) {
				fprintf (stderr, "error: " MONO_HANDLERS_ARGUMENT ", one or more unknown handlers: '%s'\n", argv [i]);
				return 1;
			}
		} else if (strcmp (argv [i], "--help-handlers") == 0) {
			mono_runtime_install_custom_handlers_usage ();
			return 0;
		} else if (strncmp (argv [i], "--response=", 11) == 0){
			gchar *response_content;
			gchar *response_options;
			gsize response_content_len;

			if (!g_file_get_contents (&argv[i][11], &response_content, &response_content_len, NULL)){
				fprintf (stderr, "The specified response file can not be read\n");
				exit (1);
			}

			response_options = response_content;

			// Check for UTF8 BOM in file and remove if found.
			if (response_content_len >= 3 && response_content [0] == '\xef' && response_content [1] == '\xbb' && response_content [2] == '\xbf') {
				response_content_len -= 3;
				response_options += 3;
			}

			if (response_content_len == 0) {
				fprintf (stderr, "The specified response file is empty\n");
				exit (1);
			}

			mono_parse_response_options (response_options, &argc, &argv, FALSE);
			g_free (response_content);
		} else if (argv [i][0] == '-' && argv [i][1] == '-' && mini_parse_debug_option (argv [i] + 2)) {
		} else if (strcmp (argv [i], "--use-map-jit") == 0){
			mono_setmmapjit (TRUE);
		} else {
			fprintf (stderr, "Unknown command line option: '%s'\n", argv [i]);
			return 1;
		}
	}

#if defined(DISABLE_HW_TRAPS) || defined(MONO_ARCH_DISABLE_HW_TRAPS)
	// Signal handlers not available
	{
		MonoDebugOptions *opt = mini_get_debug_options ();
		opt->explicit_null_checks = TRUE;
	}
#endif

	if (!argv [i]) {
		mini_usage ();
		return 1;
	}

	if (g_hasenv ("MONO_XDEBUG"))
		enable_debugging = TRUE;

#ifdef MONO_CROSS_COMPILE
       if (!mono_compile_aot) {
		   fprintf (stderr, "This mono runtime is compiled for cross-compiling. Only the --aot option is supported.\n");
		   exit (1);
       }
#if TARGET_SIZEOF_VOID_P == 4 && (defined(TARGET_ARM64) || defined(TARGET_AMD64)) && !defined(MONO_ARCH_ILP32)
       fprintf (stderr, "Can't cross-compile on 32-bit platforms to 64-bit architecture.\n");
       exit (1);
#endif
#endif

	if (mono_compile_aot || action == DO_EXEC || action == DO_DEBUGGER) {
		g_set_prgname (argv[i]);
	}

	mono_counters_init ();

#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif

	/* Set rootdir before loading config */
	mono_set_rootdir ();

	if (trace_options != NULL){
		/* 
		 * Need to call this before mini_init () so we can trace methods 
		 * compiled there too.
		 */
		mono_jit_trace_calls = mono_trace_set_options (trace_options);
		if (mono_jit_trace_calls == NULL)
			exit (1);
	}

#ifdef DISABLE_JIT
	if (!mono_aot_only && !mono_use_interpreter) {
		fprintf (stderr, "This runtime has been configured with --enable-minimal=jit, so the --full-aot command line option is required.\n");
		exit (1);
	}
#endif

	if (action == DO_DEBUGGER) {
		enable_debugging = TRUE;
		g_print ("The Mono Debugger is no longer supported.\n");
		return 1;
	} else if (enable_debugging)
		mono_debug_init (MONO_DEBUG_FORMAT_MONO);

#ifdef HOST_WIN32
	if (mixed_mode)
		mono_load_coree (argv [i]);
#endif

	mono_set_defaults (mini_verbose_level, opt);
	mono_set_os_args (argc, argv);

	domain = mini_init (argv [i], forced_version);

	mono_gc_set_stack_end (&domain);

	if (agents) {
		int i;

		for (i = 0; i < agents->len; ++i) {
			int res = load_agent (domain, (char*)g_ptr_array_index (agents, i));
			if (res) {
				g_ptr_array_free (agents, TRUE);
				mini_cleanup (domain);
				return 1;
			}
		}

		g_ptr_array_free (agents, TRUE);
	}
	
	switch (action) {
	case DO_SINGLE_METHOD_REGRESSION:
	case DO_REGRESSION:
		 return mono_exec_regression_internal (mini_verbose_level, argc -i, argv + i, action == DO_SINGLE_METHOD_REGRESSION);

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

#ifdef MONO_JIT_INFO_TABLE_TEST
	if (test_jit_info_table)
		jit_info_table_test (domain);
#endif

	if (mono_compile_aot && extra_bindings_config_file != NULL) {
		apply_root_domain_configuration_file_bindings (domain, extra_bindings_config_file);
	}

	MonoAssemblyOpenRequest open_req;
	mono_assembly_request_prepare_open (&open_req, MONO_ASMCTX_DEFAULT, mono_domain_default_alc (mono_get_root_domain ()));
	assembly = mono_assembly_request_open (aname, &open_req, &open_status);
	if (!assembly && !mono_compile_aot) {
		fprintf (stderr, "Cannot open assembly '%s': %s.\n", aname, mono_image_strerror (open_status));
		mini_cleanup (domain);
		return 2;
	}

	mono_callspec_set_assembly (assembly);

	if (mono_compile_aot || action == DO_EXEC) {
		const char *error;

		//mono_set_rootdir ();

		error = mono_check_corlib_version ();
		if (error) {
			fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
			fprintf (stderr, "Loaded from: %s\n",
				mono_defaults.corlib? mono_image_get_filename (mono_defaults.corlib): "unknown");
			fprintf (stderr, "Download a newer corlib or a newer runtime at http://www.mono-project.com/download.\n");
			exit (1);
		}

#if defined(HOST_WIN32) && HAVE_API_SUPPORT_WIN32_CONSOLE
		/* Detach console when executing IMAGE_SUBSYSTEM_WINDOWS_GUI on win32 */
		if (!enable_debugging && !mono_compile_aot && mono_assembly_get_image_internal (assembly)->image_info->cli_header.nt.pe_subsys_required == IMAGE_SUBSYSTEM_WINDOWS_GUI)
			FreeConsole ();
#endif

		main_args.domain = domain;
		main_args.file = aname;		
		main_args.argc = argc - i;
		main_args.argv = argv + i;
		main_args.opts = opt;
		main_args.aot_options = aot_options;
		main_thread_handler (&main_args);
		mono_thread_manage_internal ();

		mini_cleanup (domain);

		/* Look up return value from System.Environment.ExitCode */
		i = mono_environment_exitcode_get ();
		return i;
	} else if (action == DO_COMPILE) {
		compile_all_methods (assembly, mini_verbose_level, opt, recompilation_times);
		mini_cleanup (domain);
		return 0;
	} else if (action == DO_DEBUGGER) {
		return 1;
	}
	desc = mono_method_desc_new (mname, 0);
	if (!desc) {
		g_print ("Invalid method name %s\n", mname);
		mini_cleanup (domain);
		return 3;
	}
	method = mono_method_desc_search_in_image (desc, mono_assembly_get_image_internal (assembly));
	if (!method) {
		g_print ("Cannot find method %s\n", mname);
		mini_cleanup (domain);
		return 3;
	}

#ifndef DISABLE_JIT
	MonoCompile *cfg;
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
			nm = mono_marshal_get_native_wrapper (method, TRUE, FALSE);
			cfg = mini_method_compile (nm, opt, (JitFlags)0, part, -1);
		}
		else
			cfg = mini_method_compile (method, opt, (JitFlags)0, part, -1);
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
				n = mono_opt_descr (opt);
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
					cfg = mini_method_compile (method, opt, (JitFlags)0, 0, -1);
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
					method = mono_marshal_get_native_wrapper (method, TRUE, FALSE);

				cfg = mini_method_compile (method, opt, (JitFlags)0, 0, -1);
				mono_destroy_compile (cfg);
			}
		}
	} else {
		cfg = mini_method_compile (method, opt, (JitFlags)0, 0, -1);
		mono_destroy_compile (cfg);
	}
#endif

	mini_cleanup (domain);
 	return 0;
}

/**
 * mono_jit_init:
 */
MonoDomain * 
mono_jit_init (const char *file)
{
	MonoDomain *ret = mini_init (file, NULL);
	MONO_ENTER_GC_SAFE_UNBALANCED; //once it is not executing any managed code yet, it's safe to run the gc
	return ret;
}

/**
 * mono_jit_init_version:
 * \param domain_name the name of the root domain
 * \param runtime_version the version of the runtime to load
 *
 * Use this version when you want to force a particular runtime
 * version to be used.  By default Mono will pick the runtime that is
 * referenced by the initial assembly (specified in \p file), this
 * routine allows programmers to specify the actual runtime to be used
 * as the initial runtime is inherited by all future assemblies loaded
 * (since Mono does not support having more than one mscorlib runtime
 * loaded at once).
 *
 * The \p runtime_version can be one of these strings: "v4.0.30319" for
 * desktop, "mobile" for mobile or "moonlight" for Silverlight compat.
 * If an unrecognized string is input, the vm will default to desktop.
 *
 * \returns the \c MonoDomain representing the domain where the assembly
 * was loaded.
 */
MonoDomain * 
mono_jit_init_version (const char *domain_name, const char *runtime_version)
{
	MonoDomain *ret = mini_init (domain_name, runtime_version);
	MONO_ENTER_GC_SAFE_UNBALANCED; //once it is not executing any managed code yet, it's safe to run the gc
	return ret;
}

MonoDomain * 
mono_jit_init_version_for_test_only (const char *domain_name, const char *runtime_version)
{
	MonoDomain *ret = mini_init (domain_name, runtime_version);
	return ret;
}

/**
 * mono_jit_cleanup:
 */
void        
mono_jit_cleanup (MonoDomain *domain)
{
	MONO_STACKDATA (dummy);
	(void) mono_threads_enter_gc_unsafe_region_unbalanced_internal (&dummy);

	// after mini_cleanup everything is cleaned up so MONO_EXIT_GC_UNSAFE
	// can't work and doesn't make sense.

	mono_thread_manage_internal ();

	mini_cleanup (domain);

}

void
mono_jit_set_aot_only (gboolean val)
{
	mono_aot_only = val;
	mono_ee_features.use_aot_trampolines = val;
}

static void
mono_runtime_set_execution_mode_full (int mode, gboolean override)
{
	static gboolean mode_initialized = FALSE;
	if (mode_initialized && !override)
		return;

	mode_initialized = TRUE;
	memset (&mono_ee_features, 0, sizeof (mono_ee_features));

	switch (mode) {
	case MONO_AOT_MODE_LLVMONLY:
		mono_aot_only = TRUE;
		mono_llvm_only = TRUE;

		mono_ee_features.use_aot_trampolines = TRUE;
		break;

	case MONO_AOT_MODE_FULL:
		mono_aot_only = TRUE;

		mono_ee_features.use_aot_trampolines = TRUE;
		break;

	case MONO_AOT_MODE_HYBRID:
		mono_set_generic_sharing_vt_supported (TRUE);
		mono_set_partial_sharing_supported (TRUE);
		break;

	case MONO_AOT_MODE_INTERP:
		mono_aot_only = TRUE;
		mono_use_interpreter = TRUE;

		mono_ee_features.use_aot_trampolines = TRUE;
		break;

	case MONO_AOT_MODE_INTERP_LLVMONLY:
		mono_aot_only = TRUE;
		mono_use_interpreter = TRUE;
		mono_llvm_only = TRUE;

		mono_ee_features.force_use_interpreter = TRUE;
		break;

	case MONO_AOT_MODE_LLVMONLY_INTERP:
		mono_aot_only = TRUE;
		mono_use_interpreter = TRUE;
		mono_llvm_only = TRUE;
		break;

	case MONO_AOT_MODE_INTERP_ONLY:
		mono_check_interp_supported ();
		mono_use_interpreter = TRUE;

		mono_ee_features.force_use_interpreter = TRUE;
		break;

	case MONO_AOT_MODE_NORMAL:
	case MONO_AOT_MODE_NONE:
		break;

	default:
		g_error ("Unknown execution-mode %d", mode);
	}

}

static void
mono_runtime_set_execution_mode (int mode)
{
	mono_runtime_set_execution_mode_full (mode, TRUE);
}

/**
 * mono_jit_set_aot_mode:
 */
void
mono_jit_set_aot_mode (MonoAotMode mode)
{
	/* we don't want to set mono_aot_mode twice */
	static gboolean inited;

	g_assert (!inited);
	mono_aot_mode = mode;
	inited = TRUE;
	
	mono_runtime_set_execution_mode (mode);
}

mono_bool
mono_jit_aot_compiling (void)
{
	return mono_compile_aot;
}

/**
 * mono_jit_set_trace_options:
 * \param options string representing the trace options
 * Set the options of the tracing engine. This function can be called before initializing
 * the mono runtime. See the --trace mono(1) manpage for the options format.
 *
 * \returns TRUE if the options were parsed and set correctly, FALSE otherwise.
 */
gboolean
mono_jit_set_trace_options (const char* options)
{
	MonoCallSpec *trace_opt = mono_trace_set_options (options);
	if (trace_opt == NULL)
		return FALSE;
	mono_jit_trace_calls = trace_opt;
	return TRUE;
}

/**
 * mono_set_signal_chaining:
 *
 * Enable/disable signal chaining. This should be called before \c mono_jit_init.
 * If signal chaining is enabled, the runtime saves the original signal handlers before
 * installing its own handlers, and calls the original ones in the following cases:
 * - a \c SIGSEGV / \c SIGABRT signal received while executing native (i.e. not JITted) code.
 * - \c SIGPROF
 * - \c SIGFPE
 * - \c SIGQUIT
 * - \c SIGUSR2
 * Signal chaining only works on POSIX platforms.
 */
void
mono_set_signal_chaining (gboolean chain_signals)
{
	mono_do_signal_chaining = chain_signals;
}

/**
 * mono_set_crash_chaining:
 *
 * Enable/disable crash chaining due to signals. When a fatal signal is delivered and
 * Mono doesn't know how to handle it, it will invoke the crash handler. If chrash chaining
 * is enabled, it will first print its crash information and then try to chain with the native handler.
 */
void
mono_set_crash_chaining (gboolean chain_crashes)
{
	mono_do_crash_chaining = chain_crashes;
}

/**
 * mono_parse_options_from:
 * \param options string containing strings 
 * \param ref_argc pointer to the \c argc variable that might be updated 
 * \param ref_argv pointer to the \c argv string vector variable that might be updated
 *
 * This function parses the contents of the \c MONO_ENV_OPTIONS
 * environment variable as if they were parsed by a command shell
 * splitting the contents by spaces into different elements of the
 * \p argv vector.  This method supports quoting with both the " and '
 * characters.  Inside quoting, spaces and tabs are significant,
 * otherwise, they are considered argument separators.
 *
 * The \ character can be used to escape the next character which will
 * be added to the current element verbatim.  Typically this is used
 * inside quotes.   If the quotes are not balanced, this method 
 *
 * If the environment variable is empty, no changes are made
 * to the values pointed by \p ref_argc and \p ref_argv.
 *
 * Otherwise the \p ref_argv is modified to point to a new array that contains
 * all the previous elements contained in the vector, plus the values parsed.
 * The \p argc is updated to match the new number of parameters.
 *
 * \returns The value NULL is returned on success, otherwise a \c g_strdup allocated
 * string is returned (this is an alias to \c malloc under normal circumstances) that
 * contains the error message that happened during parsing.
 */
char *
mono_parse_options_from (const char *options, int *ref_argc, char **ref_argv [])
{
	return mono_parse_options (options, ref_argc, ref_argv, TRUE);
}

static void
merge_parsed_options (GPtrArray *parsed_options, int *ref_argc, char **ref_argv [], gboolean prepend)
{
	int argc = *ref_argc;
	char **argv = *ref_argv;

	if (parsed_options->len > 0){
		int new_argc = parsed_options->len + argc;
		char **new_argv = g_new (char *, new_argc + 1);
		guint i;
		guint j;

		new_argv [0] = argv [0];

		i = 1;
		if (prepend){
			/* First the environment variable settings, to allow the command line options to override */
			for (i = 0; i < parsed_options->len; i++)
				new_argv [i+1] = (char *)g_ptr_array_index (parsed_options, i);
			i++;
		}
		for (j = 1; j < argc; j++)
			new_argv [i++] = argv [j];
		if (!prepend){
			for (j = 0; j < parsed_options->len; j++)
				new_argv [i++] = (char *)g_ptr_array_index (parsed_options, j);
		}
		new_argv [i] = NULL;

		*ref_argc = new_argc;
		*ref_argv = new_argv;
	}
}

static char *
mono_parse_options (const char *options, int *ref_argc, char **ref_argv [], gboolean prepend)
{
	if (options == NULL)
		return NULL;

	GPtrArray *array = g_ptr_array_new ();
	GString *buffer = g_string_new ("");
	const char *p;
	gboolean in_quotes = FALSE;
	char quote_char = '\0';

	for (p = options; *p; p++){
		switch (*p){
		case ' ': case '\t': case '\n':
			if (!in_quotes) {
				if (buffer->len != 0){
					g_ptr_array_add (array, g_strdup (buffer->str));
					g_string_truncate (buffer, 0);
				}
			} else {
				g_string_append_c (buffer, *p);
			}
			break;
		case '\\':
			if (p [1]){
				g_string_append_c (buffer, p [1]);
				p++;
			}
			break;
		case '\'':
		case '"':
			if (in_quotes) {
				if (quote_char == *p)
					in_quotes = FALSE;
				else
					g_string_append_c (buffer, *p);
			} else {
				in_quotes = TRUE;
				quote_char = *p;
			}
			break;
		default:
			g_string_append_c (buffer, *p);
			break;
		}
	}
	if (in_quotes) 
		return g_strdup_printf ("Unmatched quotes in value: [%s]\n", options);

	if (buffer->len != 0)
		g_ptr_array_add (array, g_strdup (buffer->str));
	g_string_free (buffer, TRUE);

	merge_parsed_options (array, ref_argc, ref_argv, prepend);
	g_ptr_array_free (array, TRUE);

	return NULL;
}

#if defined(HOST_WIN32) && HAVE_API_SUPPORT_WIN32_COMMAND_LINE_TO_ARGV
#include <shellapi.h>

static char *
mono_win32_parse_options (const char *options, int *ref_argc, char **ref_argv [], gboolean prepend)
{
	int argc;
	gunichar2 **argv;
	gunichar2 *optionsw;

	if (!options)
		return NULL;

	GPtrArray *array = g_ptr_array_new ();
	optionsw = g_utf8_to_utf16 (options, -1, NULL, NULL, NULL);
	if (optionsw) {
		gunichar2 *p;
		gboolean in_quotes = FALSE;
		gunichar2 quote_char = L'\0';
		for (p = optionsw; *p; p++){
			switch (*p){
			case L'\n':
				if (!in_quotes)
					*p = L' ';
				break;
			case L'\'':
			case L'"':
				if (in_quotes) {
					if (quote_char == *p)
						in_quotes = FALSE;
				} else {
					in_quotes = TRUE;
					quote_char = *p;
				}
				break;
			}
		}

		argv = CommandLineToArgvW (optionsw, &argc);
		if (argv) {
			for (int i = 0; i < argc; i++)
				g_ptr_array_add (array, g_utf16_to_utf8 (argv[i], -1, NULL, NULL, NULL));

			LocalFree (argv);
		}

		g_free (optionsw);
	}

	merge_parsed_options (array, ref_argc, ref_argv, prepend);
	g_ptr_array_free (array, TRUE);

	return NULL;
}

static char *
mono_parse_response_options (const char *options, int *ref_argc, char **ref_argv [], gboolean prepend)
{
	return mono_win32_parse_options (options, ref_argc, ref_argv, prepend);
}
#else
static char *
mono_parse_response_options (const char *options, int *ref_argc, char **ref_argv [], gboolean prepend)
{
	return mono_parse_options (options, ref_argc, ref_argv, prepend);
}
#endif

/**
 * mono_parse_env_options:
 * \param ref_argc pointer to the \c argc variable that might be updated 
 * \param ref_argv pointer to the \c argv string vector variable that might be updated
 *
 * This function parses the contents of the \c MONO_ENV_OPTIONS
 * environment variable as if they were parsed by a command shell
 * splitting the contents by spaces into different elements of the
 * \p argv vector.  This method supports quoting with both the " and '
 * characters.  Inside quoting, spaces and tabs are significant,
 * otherwise, they are considered argument separators.
 *
 * The \ character can be used to escape the next character which will
 * be added to the current element verbatim.  Typically this is used
 * inside quotes.   If the quotes are not balanced, this method 
 *
 * If the environment variable is empty, no changes are made
 * to the values pointed by \p ref_argc and \p ref_argv.
 *
 * Otherwise the \p ref_argv is modified to point to a new array that contains
 * all the previous elements contained in the vector, plus the values parsed.
 * The \p argc is updated to match the new number of parameters.
 *
 * If there is an error parsing, this method will terminate the process by
 * calling exit(1).
 *
 * An alternative to this method that allows an arbitrary string to be parsed
 * and does not exit on error is the `api:mono_parse_options_from`.
 */
void
mono_parse_env_options (int *ref_argc, char **ref_argv [])
{
	char *ret;
	
	char *env_options = g_getenv ("MONO_ENV_OPTIONS");
	if (env_options == NULL)
		return;
	ret = mono_parse_options_from (env_options, ref_argc, ref_argv);
	g_free (env_options);
	if (ret == NULL)
		return;
	fprintf (stderr, "%s", ret);
	exit (1);
}
