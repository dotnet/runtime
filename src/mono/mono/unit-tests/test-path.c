/*
 * test-path.c
 */

#include "config.h"
#include <stdio.h>
#include <stdlib.h>
#include "glib.h"

#include "mono/utils/mono-path.c"

static char*
make_path (const char *a, int itrail, int slash, int upcase)
{
	// Append 0, 1, or 2 trailing slashes to a.
	g_assert (itrail >= 0 && itrail <= 2);
	char trail [] = "//";
	trail [itrail] = 0;
	char *b = g_strdup_printf ("%s%s", a, trail);

#ifdef HOST_WIN32
	if (slash)
		g_strdelimit (b, '/', '\\');
	if (upcase)
		g_strdelimit (b, 'a', 'A');
#endif
	return b;
}

int
main (void)
{
	// Use letters not numbers in this data to exercise case insensitivity.
	static const char * const bases [2] = {"/", "/a"};
	static const char * const files [6] = {"/a", "/a/b", "/a/b/c", "/ab", "/b", "/b/b/"};

	static const gboolean result [2][6] = {
		{ TRUE, FALSE, FALSE, TRUE, TRUE, FALSE },
		{ FALSE, TRUE, FALSE, FALSE, FALSE, FALSE }
	};

	int i = 0;
	gboolean const verbose = !!getenv("V");

#ifdef HOST_WIN32
	const int win32 = 1;
#else
	const int win32 = 0;
#endif

	// Iterate a cross product.
	for (int upcase_file = 0; upcase_file <= win32; ++upcase_file) {
		for (int upcase_base = 0; upcase_base <= win32; ++upcase_base) {
			for (int itrail_base = 0; itrail_base <= 2; ++itrail_base) {
				for (int itrail_file = 0; itrail_file <= 2; ++itrail_file) {
					for (int ibase = 0; ibase < G_N_ELEMENTS (bases); ++ibase) {
						for (int ifile = 0; ifile < G_N_ELEMENTS (files); ++ifile) {
							for (int islash_base = 0; islash_base <= win32; ++islash_base) {
								for (int islash_file = 0; islash_file <= win32; ++islash_file) {

									char *base = make_path (bases [ibase], itrail_base, islash_base, upcase_base);
									char *file = make_path (files [ifile], itrail_file, islash_file, upcase_file);
									//verbose && printf ("mono_path_filename_in_basedir (%s, %s)\n", file, base);
									gboolean r = mono_path_filename_in_basedir (file, base);
									verbose && printf ("mono_path_filename_in_basedir (%s, %s):%d\n", file, base, r);
									g_assertf (result [ibase][ifile] == r,
										"mono_path_filename_in_basedir (%s, %s):%d\n", file, base, r);
									if (strcmp (base, file) == 0)
										g_assertf (!r,
											"mono_path_filename_in_basedir (%s, %s):%d\n", file, base, r);
									g_free (base);
									g_free (file);
									++i;
								}
							}
						}
					}
				}
			}
		}
	}
	printf ("%d tests\n", i);

	return 0;
}
