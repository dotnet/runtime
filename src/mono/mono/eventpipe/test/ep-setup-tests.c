#include <eventpipe/ep.h>
#include <eglib/test/test.h>
#include <mono/mini/jit.h>
#include <mono/metadata/assembly.h>

MonoDomain *eventpipe_test_domain;

static RESULT
test_setup (void)
{
	char *core_root = g_getenv ("CORE_ROOT");
	if (core_root) {
		mono_set_assemblies_path (core_root);
		g_free (core_root);
	} else {
		mono_set_assemblies_path (".");
	}

	eventpipe_test_domain = mono_jit_init_version_for_test_only ("eventpipe-tests", "v4.0.30319");

	return NULL;
}

static Test ep_setup_tests [] = {
	{"test_setup", test_setup},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_setup_tests_init, ep_setup_tests)
