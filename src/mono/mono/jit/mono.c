/*
 * mono.c: Main driver for the Mono JIT engine
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001, 2002 Ximian, Inc (http://www.ximian.com)
 */
#include "jit.h"
#include "regset.h"
#include "codegen.h"
#include "debug.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/verify.h"
#include "mono/metadata/profiler.h"
#include "mono/metadata/threadpool.h"
#include "mono/metadata/mono-config.h"
#include <mono/metadata/profiler-private.h>
#include <mono/os/util.h>

/**
 * mono_jit_image:
 * @image: reference to an image
 * @verbose: If true, print debugging information on stdout.
 *
 * JIT compilation of all methods in the image.
 */
void
mono_jit_compile_image (MonoImage *image, int verbose)
{
	MonoMethod *method;
	MonoTableInfo *t = &image->tables [MONO_TABLE_METHOD];
	int i;

	for (i = 0; i < t->rows; i++) {

		method = mono_get_method (image, 
					  (MONO_TABLE_METHOD << 24) | (i + 1), 
					  NULL);

		if (verbose)
			g_print ("Compiling: %s:%s\n\n", image->assembly_name, method->name);

		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT) {
			if (verbose)
				printf ("ABSTARCT\n");
		} else
			mono_compile_method (method);

	}

}

static MonoClass *
find_class_in_assembly (MonoAssembly *assembly, const char *namespace, const char *name)
{
	MonoAssembly **ptr;
	MonoClass *class;

	class = mono_class_from_name (assembly->image, namespace, name);
	if (class)
		return class;

	for (ptr = assembly->image->references; ptr && *ptr; ptr++) {
		class = find_class_in_assembly (*ptr, namespace, name);
		if (class)
			return class;
	}

	return NULL;
}

static MonoMethod *
find_method_in_assembly (MonoAssembly *assembly, MonoMethodDesc *mdesc)
{
	MonoAssembly **ptr;
	MonoMethod *method;

	method = mono_method_desc_search_in_image (mdesc, assembly->image);
	if (method)
		return method;

	for (ptr = assembly->image->references; ptr && *ptr; ptr++) {
		method = find_method_in_assembly (*ptr, mdesc);
		if (method)
			return method;
	}

	return NULL;
}

/**
 * mono_jit_compile_class:
 * @assembly: Lookup things in this assembly
 * @compile_class: Name of the image/class/method to compile
 * @compile_times: Compile it that many times
 * @verbose: If true, print debugging information on stdout.
 *
 * JIT compilation of the image/class/method.
 *
 * @compile_class can be one of:
 *
 * - an image name (`@corlib')
 * - a class name (`System.Int32')
 * - a method name (`System.Int32:Parse')
 */
void
mono_jit_compile_class (MonoAssembly *assembly, char *compile_class,
			int compile_times, int verbose)
{
	const char *cmethod = strrchr (compile_class, ':');
	char *cname;
	char *code;
	int i, j;
	MonoClass *class;
	MonoDomain *domain = mono_domain_get ();

	if (compile_class [0] == '@') {
		MonoImage *image = mono_image_loaded (compile_class + 1);
		if (!image)
			g_error ("Cannot find image %s", compile_class + 1);
		if (verbose)
			g_print ("Compiling image %s\n", image->name);
		for (j = 0; j < compile_times; ++j)
			mono_jit_compile_image (image, verbose);
	} else if (cmethod) {
		MonoMethodDesc *mdesc;
		MonoMethod *m;
		mdesc = mono_method_desc_new (compile_class, FALSE);
		if (!mdesc)
			g_error ("Invalid method name '%s'", compile_class);
		m = find_method_in_assembly (assembly, mdesc);
		if (!m)
			g_error ("Cannot find method '%s'", compile_class);
		for (j = 0; j < compile_times; ++j) {
			code = mono_compile_method (m);
			// g_free (code);
			g_hash_table_remove (domain->jit_code_hash, m);
		}
	} else {
		cname = strrchr (compile_class, '.');
		if (cname)
			*cname++ = 0;
		else {
			cname = compile_class;
			compile_class = (char *)"";
		}
		class = find_class_in_assembly (assembly, compile_class, cname);
		if (!class)
			g_error ("Cannot find class %s.%s", compile_class, cname);
		mono_class_init (class);
		for (j = 0; j < compile_times; ++j) {
			for (i = 0; i < class->method.count; ++i) {
				if (class->methods [i]->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
					continue;
				if (class->methods [i]->flags & METHOD_ATTRIBUTE_ABSTRACT)
					continue;
				if (verbose)
					g_print ("Compiling: %s.%s:%s\n",
						 compile_class, cname, class->methods [i]->name);
				code = mono_compile_method (class->methods [i]);
				g_hash_table_remove (domain->jit_code_hash, class->methods [i]);
				// g_free (code);
			}
		}
	}
}

static void
usage (char *name)
{
	fprintf (stderr,
		 "mono %s, the Mono ECMA CLI JIT Compiler, (C) 2001, 2002 Ximian, Inc.\n\n"
		 "Usage is: %s [options] executable args...\n\n",  VERSION, name);
	fprintf (stderr,
		 "Runtime Debugging:\n"
		 "    -d                 debug the jit, show disassembler output.\n"
		 "    --dump-asm         dumps the assembly code generated\n"
		 "    --dump-forest      dumps the reconstructed forest\n"
		 "    --print-vtable     print the VTable of all used classes\n"
		 "    --stats            print statistics about the jit operations\n"
		 "    --trace            printf function call trace\n"
		 "    --compile NAME     compile NAME, then exit.\n"
		 "                       NAME is in one of the following formats:\n"
		 "                         namespace.name          compile the given class\n"
		 "                         namespace.name:method   compile the given method\n"
		 "                         @imagename              compile the given image\n"
		 "    --ncompile NUM     compile methods NUM times (default: 1000)\n"
		 "\n"
		 "Development:\n"
		 "    --debug[=FORMAT]   write a debugging file.  FORMAT is one of:\n"
		 "                         stabs        to write stabs information\n"
		 "                         dwarf        to write dwarf2 information\n"
		 "                         dwarf-plus   to write extended dwarf2 information\n"
		 "    --debug-args ARGS  comma-separated list of additional arguments for the\n"
		 "                       symbol writer.  See the manpage for details.\n"
		 "    --profile          record and dump profile info\n"
		 "    --breakonex        set a breakpoint for unhandled exception\n"
		 "    --break NAME       insert a breakpoint at the start of method NAME\n"
		 "                       (NAME is in `namespace.name:methodname' format)\n"
		 "    --precompile name  precompile NAME before executing the main application:\n"
		 "                       NAME is in one of the following formats:\n"
		 "                         namespace.name          compile the given class\n"
		 "                         namespace.name:method   compile the given method\n"
		 "                         @imagename              compile the given image\n"
		 "\n"
		 "Runtime:\n"
		 "    --config filename  Load specified config file instead of the default.\n"
		 "    --fast-iconv       Use fast floating point integer conversion\n"
		 "    --noinline         Disable code inliner\n"
		 "    --nointrinsic      Disable memcopy inliner\n"
		 "    --nols             disable linear scan register allocation\n"
		 "    --share-code       force jit to produce shared code\n"
		 "    --workers n        maximum number of worker threads\n"
		);
	exit (1);
}

int 
main (int argc, char *argv [])
{
	MonoDomain *domain;
	MonoAssembly *assembly;
	int retval = 0, i;
	int compile_times = 1000;
	char *compile_class = NULL;
	char *debug_args = NULL;
	char *file, *error, *config_file = NULL;
	gboolean testjit = FALSE;
	int verbose = FALSE;
	GList *precompile_classes = NULL;

	g_log_set_always_fatal (G_LOG_LEVEL_ERROR);
	g_log_set_fatal_mask (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR);
	
	if (argc < 2)
		usage (argv [0]);

	for (i = 1; i < argc && argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--help") == 0) {
			usage (argv [0]);
		} else if (strcmp (argv [i], "-d") == 0) {
			testjit = TRUE;
			mono_jit_dump_asm = TRUE;
			mono_jit_dump_forest = TRUE;
		} else if (strcmp (argv [i], "--dump-asm") == 0)
			mono_jit_dump_asm = TRUE;
		else if (strcmp (argv [i], "--dump-forest") == 0)
			mono_jit_dump_forest = TRUE;
		else if (strcmp (argv [i], "--trace") == 0)
			mono_jit_trace_calls = TRUE;
		else if (strcmp (argv [i], "--share-code") == 0)
			mono_jit_share_code = TRUE;
		else if (strcmp (argv [i], "--noinline") == 0)
			mono_jit_inline_code = FALSE;
		else if (strcmp (argv [i], "--nointrinsic") == 0)
			mono_inline_memcpy = FALSE;
		else if (strcmp (argv [i], "--nols") == 0)
			mono_use_linear_scan = FALSE;
		else if (strcmp (argv [i], "--breakonex") == 0)
			mono_break_on_exc = TRUE;
		else if (strcmp (argv [i], "--print-vtable") == 0)
			mono_print_vtable = TRUE;
		else if (strcmp (argv [i], "--break") == 0) {
			MonoMethodDesc *desc = mono_method_desc_new (argv [++i], FALSE);
			if (!desc)
				g_error ("Invalid method name '%s'", argv [i]);
			mono_debug_methods = g_list_append (mono_debug_methods, desc);
		} else if (strcmp (argv [i], "--count") == 0) {
			compile_times = atoi (argv [++i]);
		} else if (strcmp (argv [i], "--config") == 0) {
			config_file = argv [++i];
		} else if (strcmp (argv [i], "--workers") == 0) {
			mono_worker_threads = atoi (argv [++i]);
			if (mono_worker_threads < 1)
				mono_worker_threads = 1;
		} else if (strcmp (argv [i], "--profile") == 0) {
			mono_jit_profile = TRUE;
			mono_profiler_install_simple ();
		} else if (strcmp (argv [i], "--compile") == 0) {
			compile_class = argv [++i];
		} else if (strcmp (argv [i], "--ncompile") == 0) {
			compile_times = atoi (argv [++i]);
		} else if (strcmp (argv [i], "--stats") == 0) {
			memset (&mono_jit_stats, 0, sizeof (MonoJitStats));
			mono_jit_stats.enabled = TRUE;
		} else if (strncmp (argv [i], "--debug=", 8) == 0) {
			const char *format = &argv [i][8];
				
			if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
				g_error ("You can only use one debugging format.");
			if (strcmp (format, "stabs") == 0)
				mono_debug_format = MONO_DEBUG_FORMAT_STABS;
			else if (strcmp (format, "dwarf") == 0)
				mono_debug_format = MONO_DEBUG_FORMAT_DWARF2;
			else if (strcmp (format, "dwarf-plus") == 0)
				mono_debug_format = MONO_DEBUG_FORMAT_DWARF2_PLUS;
			else
				g_error ("Unknown debugging format: %s", argv [i] + 8);
		} else if (strcmp (argv [i], "--debug") == 0) {
			if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
				g_error ("You can only use one debugging format.");
			mono_debug_format = MONO_DEBUG_FORMAT_DWARF2_PLUS;
		} else if (strcmp (argv [i], "--debug-args") == 0) {
			if (debug_args)
				g_error ("You can use --debug-args only once.");
			debug_args = argv [++i];
		} else if (strcmp (argv [i], "--precompile") == 0) {
			precompile_classes = g_list_append (precompile_classes, argv [++i]);
		} else if (strcmp (argv [i], "--verbose") == 0) {
			verbose = TRUE;;
		} else if (strcmp (argv [i], "--fast-iconv") == 0) {
			mono_use_fast_iconv = TRUE;
		} else
			usage (argv [0]);
	}
	
	file = argv [i];

	if (!file)
		usage (argv [0]);

	mono_config_parse (config_file);
	mono_set_rootdir (argv [0]);
	domain = mono_jit_init (file);

	error = mono_verify_corlib ();
	if (error) {
		fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
		exit (1);
	}

	assembly = mono_domain_assembly_open (domain, file);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}

	if (mono_debug_format != MONO_DEBUG_FORMAT_NONE) {
		MonoDebugHandle *debug;
		gchar **args;

		args = g_strsplit (debug_args ? debug_args : "", ",", -1);
		debug = mono_debug_open (assembly, mono_debug_format, (const char **) args);
		mono_debug_add_image (debug, assembly->image);
		g_strfreev (args);
	}

	if (testjit) {
		mono_jit_compile_image (assembly->image, TRUE);
	} else if (compile_class) {
		mono_jit_compile_class (assembly, compile_class, compile_times, TRUE);
	} else {
		GList *tmp;

		for (tmp = precompile_classes; tmp; tmp = tmp->next)
			mono_jit_compile_class (assembly, tmp->data, 1, verbose);

		retval = mono_jit_exec (domain, assembly, argc - i, argv + i);
		printf ("RESULT: %d\n", retval);
	}

	mono_profiler_shutdown ();
	mono_jit_cleanup (domain);

	return retval;
}


