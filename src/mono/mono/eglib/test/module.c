#include <config.h>
#include <glib.h>
#include <gmodule.h>
#include <string.h>
#include <stdio.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#if defined (G_OS_WIN32)
#define EXTERNAL_SYMBOL "GetProcAddress"
#else
#define EXTERNAL_SYMBOL "system"
#endif

#if _WIN32
#ifndef PSAPI_VERSION
#define PSAPI_VERSION 2 // Use the Windows 7 or newer version more directly.
#endif
#include <windows.h>
#include <wchar.h>
#include <psapi.h>
#endif

#include "test.h"

void G_MODULE_EXPORT
dummy_test_export (void);

void G_MODULE_EXPORT
dummy_test_export (void)
{
}

/* test for g_module_open (NULL, ...) */
static RESULT
test_module_symbol_null (void)
{
	gpointer proc = GINT_TO_POINTER (42);

	GModule *m = g_module_open (NULL, G_MODULE_BIND_LAZY);

	if (m == NULL)
		return FAILED ("bind to main module failed. #0");

	if (g_module_symbol (m, "__unlikely_\nexistent__", &proc))
		return FAILED ("non-existent symbol lookup failed. #1");

	if (proc)
		return FAILED ("non-existent symbol lookup failed. #2");

	if (!g_module_symbol (m, EXTERNAL_SYMBOL, &proc))
		return FAILED ("external lookup failed. #3");

	if (!proc)
		return FAILED ("external lookup failed. #4");

	if (!g_module_symbol (m, "dummy_test_export", &proc))
		return FAILED ("in-proc lookup failed. #5");

	if (!proc)
		return FAILED ("in-proc lookup failed. #6");

	if (!g_module_close (m))
		return FAILED ("close failed. #7");

	return OK;
}

static Test module_tests [] = {
	{"g_module_symbol_null", test_module_symbol_null},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(module_tests_init, module_tests)
