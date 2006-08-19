/*
 * Tests to ensure that our type definitions are correct
 *
 * These depend on -Werror, -Wall being set to catch the build error.
 */
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <glib.h>
#include "test.h"

RESULT
test_formats ()
{
	char buffer [1024];
	gsize a = 1;
	
	sprintf (buffer, "%" G_GSIZE_FORMAT, a);

	return NULL;
}

RESULT
test_ptrconv ()
{
	int iv, iv2;
	unsigned int uv, uv2;
	gpointer ptr;

	iv = G_MAXINT32;
	ptr = GINT_TO_POINTER (iv);
	iv2 = GPOINTER_TO_INT (ptr);
	if (iv != iv2)
		return FAILED ("int to pointer and back conversions fail %d != %d", iv, iv2);

	iv = G_MININT32;
	ptr = GINT_TO_POINTER (iv);
	iv2 = GPOINTER_TO_INT (ptr);
	if (iv != iv2)
		return FAILED ("int to pointer and back conversions fail %d != %d", iv, iv2);

	iv = 1;
	ptr = GINT_TO_POINTER (iv);
	iv2 = GPOINTER_TO_INT (ptr);
	if (iv != iv2)
		return FAILED ("int to pointer and back conversions fail %d != %d", iv, iv2);

	iv = -1;
	ptr = GINT_TO_POINTER (iv);
	iv2 = GPOINTER_TO_INT (ptr);
	if (iv != iv2)
		return FAILED ("int to pointer and back conversions fail %d != %d", iv, iv2);

	iv = 0;
	ptr = GINT_TO_POINTER (iv);
	iv2 = GPOINTER_TO_INT (ptr);
	if (iv != iv2)
		return FAILED ("int to pointer and back conversions fail %d != %d", iv, iv2);

	uv = 0;
	ptr = GUINT_TO_POINTER (iv);
	uv2 = GPOINTER_TO_UINT (ptr);
	if (iv != iv2)
		return FAILED ("uint to pointer and back conversions fail %u != %d", iv, iv2);
	
	uv = 1;
	ptr = GUINT_TO_POINTER (iv);
	uv2 = GPOINTER_TO_UINT (ptr);
	if (iv != iv2)
		return FAILED ("uint to pointer and back conversions fail %u != %d", iv, iv2);

	uv = UINT32_MAX;
	ptr = GUINT_TO_POINTER (iv);
	uv2 = GPOINTER_TO_UINT (ptr);
	if (iv != iv2)
		return FAILED ("uint to pointer and back conversions fail %u != %d", iv, iv2);

	return NULL;
	
}

static Test size_tests [] = {
	{"formats", test_formats},
	{"ptrconv", test_ptrconv},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(size_tests_init, size_tests)
