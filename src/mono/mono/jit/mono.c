
#include "jit.h"
#include "regset.h"
#include "codegen.h"
#include "debug.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/verify.h"

/**
 * mono_jit_assembly:
 * @assembly: reference to an assembly
 *
 * JIT compilation of all methods in the assembly. Prints debugging
 * information on stdout.
 */
static void
mono_jit_assembly (MonoAssembly *assembly)
{
	MonoImage *image = assembly->image;
	MonoMethod *method;
	MonoTableInfo *t = &image->tables [MONO_TABLE_METHOD];
	int i;

	for (i = 0; i < t->rows; i++) {

		method = mono_get_method (image, 
					  (MONO_TABLE_METHOD << 24) | (i + 1), 
					  NULL);

		printf ("\nMethod: %s\n\n", method->name);

		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT)
			printf ("ABSTARCT\n");
		else
			arch_compile_method (method);

	}

}

static void
usage (char *name)
{
	fprintf (stderr,
		 "%s %s, the Mono ECMA CLI JIT Compiler, (C) 2001 Ximian, Inc.\n\n"
		 "Usage is: %s [options] executable args...\n", name,  VERSION, name);
	fprintf (stderr,
		 "Valid Options are:\n"
		 "-d               debug the jit, show disassembler output.\n"
		 "--dump-asm       dumps the assembly code generated\n"
		 "--dump-forest    dumps the reconstructed forest\n"
		 "--trace-calls    printf function call trace\n"
		 "--share-code     force jit to produce shared code\n"
		 "--print-vtable   print the VTable of all used classes\n"
		 "--workers n      maximum number of worker threads\n"
		 "--stabs          write stabs debug information\n"
		 "--dwarf          write dwarf2 debug information\n"
		 "--dwarf-plus     write extended dwarf2 debug information\n"
		 "--stats          print statistics about the jit operations\n"
		 "--compile cname  compile methods in given class (namespace.name[:methodname])\n"
		 "--ncompile num   compile methods num times (default: 1000)\n"
		 "--debug name     insert a breakpoint at the start of method name\n"
		 "--help           print this help message\n");
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
	char *file, *error;
	gboolean testjit = FALSE;
	int stack, verbose = FALSE;

	mono_end_of_stack = &stack; /* a pointer to a local variable is always < BP */

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
		else if (strcmp (argv [i], "--trace-calls") == 0)
			mono_jit_trace_calls = TRUE;
		else if (strcmp (argv [i], "--share-code") == 0)
			mono_jit_share_code = TRUE;
		else if (strcmp (argv [i], "--print-vtable") == 0)
			mono_print_vtable = TRUE;
		else if (strcmp (argv [i], "--debug") == 0) {
			MonoMethodDesc *desc = mono_method_desc_new (argv [++i], FALSE);
			if (!desc)
				g_error ("Invalid method name '%s'", argv [i]);
			mono_debug_methods = g_list_append (mono_debug_methods, desc);
		} else if (strcmp (argv [i], "--count") == 0) {
			compile_times = atoi (argv [++i]);
		} else if (strcmp (argv [i], "--workers") == 0) {
			mono_worker_threads = atoi (argv [++i]);
			if (mono_worker_threads < 1)
				mono_worker_threads = 1;
		} else if (strcmp (argv [i], "--compile") == 0) {
			compile_class = argv [++i];
		} else if (strcmp (argv [i], "--ncompile") == 0) {
			compile_times = atoi (argv [++i]);
		} else if (strcmp (argv [i], "--stats") == 0) {
			memset (&mono_jit_stats, 0, sizeof (MonoJitStats));
			mono_jit_stats.enabled = TRUE;
		} else if (strcmp (argv [i], "--stabs") == 0) {
			if (mono_debug_handle)
				g_error ("You can use either --stabs or --dwarf, but not both.");
			mono_debug_handle = mono_debug_open_file ("", MONO_DEBUG_FORMAT_STABS);
		} else if (strcmp (argv [i], "--dwarf") == 0) {
			if (mono_debug_handle)
				g_error ("You can use either --stabs or --dwarf, but not both.");
			mono_debug_handle = mono_debug_open_file ("", MONO_DEBUG_FORMAT_DWARF2);
		} else if (strcmp (argv [i], "--dwarf-plus") == 0) {
			if (mono_debug_handle)
				g_error ("You can use either --stabs or --dwarf, but not both.");
			mono_debug_handle = mono_debug_open_file ("", MONO_DEBUG_FORMAT_DWARF2_PLUS);
		} else if (strcmp (argv [i], "--verbose") == 0) {
			verbose = TRUE;;
		} else
			usage (argv [0]);
	}
	
	file = argv [i];

	if (!file)
		usage (argv [0]);

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

	if (testjit) {
		mono_jit_assembly (assembly);
	} else if (compile_class) {
		const char *cmethod = strrchr (compile_class, ':');
		char *cname;
		char *code;
		int j;
		MonoClass *class;

		if (cmethod) {
			MonoMethodDesc *mdesc;
			MonoMethod *m;
			mdesc = mono_method_desc_new (compile_class, FALSE);
			if (!mdesc)
				g_error ("Invalid method name '%s'", compile_class);
			m = mono_method_desc_search_in_image (mdesc, assembly->image);
			if (!m)
				g_error ("Cannot find method '%s'", compile_class);
			for (j = 0; j < compile_times; ++j) {
				code = arch_compile_method (m);
				g_free (code);
			}
		} else {
			cname = strrchr (compile_class, '.');
			if (cname)
				*cname++ = 0;
			else {
				cname = compile_class;
				compile_class = (char *)"";
			}
			class = mono_class_from_name (assembly->image, compile_class, cname);
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
						g_print ("Compiling: %s\n", class->methods [i]->name);
					code = arch_compile_method (class->methods [i]);
					g_free (code);
				}
			}
		}
	} else {
		/*
		 * skip the program name from the args.
		 */
		++i;
		retval = mono_jit_exec (domain, assembly, argc - i, argv + i);
		printf ("RESULT: %d\n", retval);
	}

	mono_jit_cleanup (domain);

	return retval;
}


