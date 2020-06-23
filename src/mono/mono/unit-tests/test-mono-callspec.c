/*
 * test-mono-callspec.c: Unit test for the callspec parsing and evaluation.
 *
 * Copyright (C) 2017 vFunction, Inc.
 *
 */

// Embedders do not have the luxury of our config.h, so skip it here.
//#include "config.h"

// But we need MONO_INSIDE_RUNTIME to get MonoError mangled correctly
// because we also test unexported functions (mono_class_from_name_checked).
#define MONO_INSIDE_RUNTIME 1

#include "mono/utils/mono-publib.h"

// Allow to test external_only w/o deprecation error.
#undef MONO_RT_EXTERNAL_ONLY
#define MONO_RT_EXTERNAL_ONLY /* nothing */

#include <glib.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/callspec.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/mini/jit.h>
#include <mono/utils/mono-error-internals.h>

#define TESTPROG "callspec.exe"

GArray *test_methods = NULL;

enum test_method_enums {
	FOO_BAR,
	FOO_BARP,
	GOO_BAR,
	FOO2_BAR,
	CONSOLE_WRITELINE,
};

struct {
	int method;
	const char *callspec;
	gboolean expect_match;
} test_entries[] = {
    /* program tests */
    {FOO_BAR, "program", TRUE},
    {CONSOLE_WRITELINE, "program", FALSE},
    {FOO_BAR, "all,-program", FALSE},
    {CONSOLE_WRITELINE, "all,-program", TRUE},

    /* assembly tests */
    {FOO_BAR, "mscorlib", FALSE},
    {CONSOLE_WRITELINE, "mscorlib", TRUE},
    {FOO_BAR, "all,-mscorlib", TRUE},
    {CONSOLE_WRITELINE, "all,-mscorlib", FALSE},

    /* class tests */
    {FOO_BAR, "T:Baz.Foo", TRUE},
    {CONSOLE_WRITELINE, "T:Baz.Foo", FALSE},
    {FOO_BAR, "all,-T:Baz.Foo", FALSE},
    {CONSOLE_WRITELINE, "all,-T:Baz.Foo", TRUE},

    /* namespace tests */
    {FOO_BAR, "N:Baz", TRUE},
    {CONSOLE_WRITELINE, "N:Baz", FALSE},
    {FOO_BAR, "all,-N:Baz", FALSE},
    {CONSOLE_WRITELINE, "all,-N:Baz", TRUE},

    /* method tests without parameters */
    {FOO_BAR, "M:Baz.Foo:Bar", TRUE},
    {FOO_BARP, "M:Baz.Foo:Bar", TRUE},
    {GOO_BAR, "M:Baz.Foo:Bar", FALSE},
    {FOO2_BAR, "M:Baz.Foo:Bar", FALSE},
    {CONSOLE_WRITELINE, "M:Baz.Foo:Bar", FALSE},
    {FOO_BAR, "all,-M:Baz.Foo:Bar", FALSE},
    {CONSOLE_WRITELINE, "all,-M:Baz.Foo:Bar", TRUE},

    /* method tests without class */
    {FOO_BAR, "M::Bar", TRUE},
    {FOO_BARP, "M::Bar", TRUE},
    {GOO_BAR, "M::Bar", TRUE},
    {FOO2_BAR, "M::Bar", TRUE},

    {0, NULL, FALSE}};

static int test_callspec (int test_idx,
			  MonoMethod *method,
			  const char *callspec,
			  gboolean expect_match)
{
	int res = 0;
	gboolean initialized = FALSE;
	gboolean match;
	MonoCallSpec spec = {0};
	char *errstr;
	char *method_name = mono_method_full_name (method, TRUE);
	if (!method_name) {
		printf ("FAILED getting method name in callspec test #%d\n",
			test_idx);
		res = 1;
		goto out;
	}

	if (!mono_callspec_parse (callspec, &spec, &errstr)) {
		printf ("FAILED parsing callspec '%s' - %s\n", callspec,
			errstr);
		g_free (errstr);
		res = 1;
		goto out;
	}
	initialized = TRUE;

	match = mono_callspec_eval (method, &spec);

	if (match && !expect_match) {
		printf ("FAILED unexpected match '%s' against '%s'\n",
			method_name, callspec);
		res = 1;
		goto out;
	}
	if (!match && expect_match) {
		printf ("FAILED unexpected mismatch '%s' against '%s'\n",
			method_name, callspec);
		res = 1;
		goto out;
	}

out:
	if (initialized)
		mono_callspec_cleanup(&spec);
	if (method_name)
		g_free (method_name);
	return res;
}

static int test_all_callspecs (void)
{
	int idx;
	for (idx = 0; test_entries[idx].callspec; ++idx) {
		MonoMethod *meth = g_array_index (test_methods, MonoMethod *,
						  test_entries[idx].method);
		if (test_callspec (idx, meth, test_entries[idx].callspec,
				   test_entries[idx].expect_match))
			return 1;
	}

	return 0;
}

static MonoClass *test_mono_class_from_name (MonoImage *image,
					     const char *name_space,
					     const char *name)
{
	ERROR_DECL (error);
	MonoClass *klass;

	klass = mono_class_from_name_checked (image, name_space, name, error);
	mono_error_cleanup (error); /* FIXME Don't swallow the error */

	return klass;
}

#ifdef __cplusplus
extern "C"
#endif
int
test_mono_callspec_main (void);

int
test_mono_callspec_main (void)
{
	int res = 0;
	MonoDomain *domain = NULL;
	MonoAssembly *assembly = NULL;
	MonoImage *prog_image = NULL;
	MonoImage *corlib = NULL;
	MonoClass *prog_klass, *console_klass;
	MonoMethod *meth;
	MonoImageOpenStatus status;

	//FIXME This is a hack due to embedding simply not working from the tree
	mono_set_assemblies_path ("../../mcs/class/lib/net_4_x");

	test_methods = g_array_new (FALSE, TRUE, sizeof (MonoMethod *));
	if (!test_methods) {
		res = 1;
		printf ("FAILED INITIALIZING METHODS ARRAY\n");
		goto out;
	}

	domain = mono_jit_init_version_for_test_only ("TEST RUNNER", "mobile");
	assembly = mono_assembly_open (TESTPROG, &status);
	if (!domain || !assembly) {
		res = 1;
		printf("FAILED LOADING TEST PROGRAM\n");
		goto out;
	}

	mono_callspec_set_assembly(assembly);

	prog_image = mono_assembly_get_image_internal (assembly);

	prog_klass = test_mono_class_from_name (prog_image, "Baz", "Foo");
	if (!prog_klass) {
		res = 1;
		printf ("FAILED FINDING Baz.Foo\n");
		goto out;
	}
	meth = mono_class_get_method_from_name (prog_klass, "Bar", 0);
	if (!meth) {
		res = 1;
		printf ("FAILED FINDING Baz.Foo:Bar ()\n");
		goto out;
	}
	g_array_append_val (test_methods, meth);
	meth = mono_class_get_method_from_name (prog_klass, "Bar", 1);
	if (!meth) {
		res = 1;
		printf ("FAILED FINDING Baz.Foo:Bar (string)\n");
		goto out;
	}
	g_array_append_val (test_methods, meth);

	prog_klass = test_mono_class_from_name (prog_image, "Baz", "Goo");
	if (!prog_klass) {
		res = 1;
		printf ("FAILED FINDING Baz.Goo\n");
		goto out;
	}
	meth = mono_class_get_method_from_name (prog_klass, "Bar", 1);
	if (!meth) {
		res = 1;
		printf ("FAILED FINDING Baz.Goo:Bar (string)\n");
		goto out;
	}
	g_array_append_val (test_methods, meth);

	prog_klass = test_mono_class_from_name (prog_image, "Baz", "Foo2");
	if (!prog_klass) {
		res = 1;
		printf ("FAILED FINDING Baz.Foo2\n");
		goto out;
	}
	meth = mono_class_get_method_from_name (prog_klass, "Bar", 1);
	if (!meth) {
		res = 1;
		printf ("FAILED FINDING Baz.Foo2:Bar (string)\n");
		goto out;
	}
	g_array_append_val (test_methods, meth);

	corlib = mono_get_corlib ();

	console_klass = test_mono_class_from_name (corlib, "System", "Console");
	if (!console_klass) {
		res = 1;
		printf ("FAILED FINDING System.Console\n");
		goto out;
	}
	meth = mono_class_get_method_from_name (console_klass, "WriteLine", 1);
	if (!meth) {
		res = 1;
		printf ("FAILED FINDING System.Console:WriteLine\n");
		goto out;
	}
	g_array_append_val (test_methods, meth);

	res = test_all_callspecs ();
out:
	return res;
}
