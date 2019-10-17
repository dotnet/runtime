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

static RESULT
test_module_get_module_filename (void)
{
#if _WIN32
	const HMODULE mods [ ] = {NULL, LoadLibraryW (L"msvcrt.dll"), (HMODULE)(gssize)-1 };

	for (int i = 0; i < G_N_ELEMENTS (mods); ++i) {
		const HMODULE mod = mods [i];
		for (int j = 0; j <= 2; ++j) {
			wchar_t* str = { 0 };
			guint32 length = { 0 };
			wchar_t buf2 [999] = { 0 };
			wchar_t buf3 [2] = { 0 };
			gboolean success = { 0 };
			guint32 length2 = { 0 };
			gboolean success2 = { 0 };
			guint32 length3 = { 0 };
			gboolean success3 = { 0 };

			switch (j) {
			case 0:
				success = mono_get_module_filename (mod, &str, &length);
				length2 = GetModuleFileNameW (mod, buf2, G_N_ELEMENTS (buf2)); // large buf
				length3 = GetModuleFileNameW (mod, buf3, 1); // small buf
				break;
			case 1:
				success = mono_get_module_filename_ex (GetCurrentProcess (), mods [i], &str, &length);
				length2 = GetModuleFileNameExW (GetCurrentProcess (), mod, buf2, G_N_ELEMENTS (buf2)); // large buf
				length3 = GetModuleFileNameExW (GetCurrentProcess (), mod, buf3, 1); // small buf
				break;
			case 2:
				success = mono_get_module_basename (GetCurrentProcess (), mod, &str, &length);
				length2 = GetModuleBaseNameW (GetCurrentProcess (), mod, buf2, G_N_ELEMENTS (buf2)); // large buf
				length3 = GetModuleBaseNameW (GetCurrentProcess (), mod, buf3, 1); // small buf
				break;
			}
			success2 = length2 && length2 < G_N_ELEMENTS (buf2);
			success3 = length3 == 1;
			printf ("j:%d s:%X s2:%X s3:%X l:%u l2:%u l3:%u str:%X b2:%X b3:%X\n",
				j,
				success, success2, success3,
				length, length2, length3,
				str ? str [0] : 0, buf2 [0], buf3 [0]);
			g_assert (success == success2);
			g_assert (success == success3 || j > 0);
			g_assert (!success || str [0] == buf2 [0]);
			//g_assert (!success || str [0] == buf3 [0]);
			g_assert (length3 == 0 || length3 == 1);
			g_assert (length == (success2 ? wcslen (buf2) :0));
			g_assert (!success || !wcscmp (str, buf2));
			g_assert (!success || str);
			if (success)
				printf ("%p %ls %ls %d %d\n", mod, str, buf2, length, length2);
			else
				printf ("!%p %u\n", str, (guint)length);
			g_free (str);
		}
	}
#endif
	return OK;
}

static RESULT
test_get_current_directory (void)
{
#if _WIN32
	wchar_t* str = { 0 };
	guint32 length = { 0 };
	gboolean success = mono_get_current_directory (&str, &length);
	wchar_t buf2 [999] = { 0 };
	const int length2 = GetCurrentDirectoryW (G_N_ELEMENTS (buf2), buf2);
	const gboolean success2 = length2 && length2 < G_N_ELEMENTS (buf2);
	wchar_t buf3 [2] = { 0 };
	const int length3 = GetCurrentDirectoryW (G_N_ELEMENTS (buf3), buf3);
	const gboolean success3 = length3 > 0;
	printf ("s:%X s2:%X s3:%X str:%X b2:%X b3:%X\n", success, success2, success3, str ? str [0] : 0, buf2 [0], buf3 [0]);
	g_assert (length == length2);
	g_assert (success == success2);
	g_assert (success == success3);
	g_assert (!success || !wcscmp (str, buf2));
	g_assert (!success || str);
	if (success)
		printf ("%ls\n%ls\n", str, buf2);
	else
		printf ("!%p %u\n", str, (guint)length);
	g_free (str);
#endif
	return OK;
}

static Test module_tests [] = {
	{"g_module_symbol_null", test_module_symbol_null},
	{"module_get_module_filename", test_module_get_module_filename},
	{"get_current_directory", test_get_current_directory},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(module_tests_init, module_tests)
