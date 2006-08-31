#include "test.h"

/*
 * g_utf16_to_utf8
 */

glong
compare_strings_utf8_pos (const gchar *expected, const gchar *actual, glong size)
{
	int i;
	for (i = 0; i < size; i++)
		if (expected [i] != actual [i])
			return i;
	return -1;
}

RESULT
compare_strings_utf8_RESULT (const gchar *expected, const gchar *actual, glong size)
{
	glong ret;

	ret = compare_strings_utf8_pos (expected, actual, size);
	if (ret < 0)
		return OK;
	return FAILED ("Incorrect output: expected '%s' but was '%s', differ at %d\n", expected, actual, ret);
}

void
gchar_to_gunichar2 (gunichar2 ret[], const gchar *src)
{
	int i;

	for (i = 0; src [i]; i++)
		ret [i] = src [i];
	ret [i] = 0;
}

RESULT
compare_utf16_to_utf8_explicit (const gchar *expected, const gunichar2 *utf16, glong len_in, glong len_out, glong size_spec)
{
	GError *error;
	gchar* ret;
	RESULT result;
	glong in_read, out_read;

	result = NULL;

	error = NULL;
	ret = g_utf16_to_utf8 (utf16, size_spec, &in_read, &out_read, &error);
	if (error) {
		result = FAILED ("The error is %d %s\n", (error)->code, (error)->message);
		g_error_free (error);
		if (ret)
			g_free (ret);
		return result;
	}
	if (in_read != len_in)
		result = FAILED ("Read size is incorrect: expected %d but was %d\n", len_in, in_read);
	else if (out_read != len_out)
		result = FAILED ("Converted size is incorrect: expected %d but was %d\n", len_out, out_read);
	else
		result = compare_strings_utf8_RESULT (expected, ret, len_out);

	g_free (ret);
	if (result)
		return result;

	return OK;
}

RESULT
compare_utf16_to_utf8 (const gchar *expected, const gunichar2 *utf16, glong len_in, glong len_out)
{
	RESULT result;

	result = compare_utf16_to_utf8_explicit (expected, utf16, len_in, len_out, -1);
	if (result != OK)
		return result;
	return compare_utf16_to_utf8_explicit (expected, utf16, len_in, len_out, len_in);
}

RESULT
test_utf16_to_utf8 ()
{
	const gchar *src0 = "", *src1 = "ABCDE";
	gunichar2 str0 [1], str1 [6];
	RESULT result;

	str0 [0] = 0;
	gchar_to_gunichar2 (str1, src1);

	/* empty string */
	result = compare_utf16_to_utf8 (src0, str0, 0, 0);
	if (result != OK)
		return result;

	result = compare_utf16_to_utf8 (src1, str1, 5, 5);
	if (result != OK)
		return result;

	return OK;
}

/*
 * g_utf8_to_utf16 
 */

glong
compare_strings_utf16_pos (const gunichar2 *expected, const gunichar2 *actual, glong size)
{
	int i;
	for (i = 0; i < size; i++)
		if (expected [i] != actual [i])
			return i;
	return -1;
}

RESULT
compare_strings_utf16_RESULT (const gunichar2 *expected, const gunichar2 *actual, glong size)
{
	glong ret;

	ret = compare_strings_utf16_pos (expected, actual, size);
	if (ret < 0)
		return OK;
	return FAILED ("Incorrect output: expected '%s' but was '%s'\n", expected, actual);
}

RESULT
compare_utf8_to_utf16_explicit (const gunichar2 *expected, const gchar *utf8, glong len_in, glong len_out, glong size_spec)
{
	GError *error;
	gunichar2* ret;
	RESULT result;
	glong in_read, out_read;

	result = NULL;

	error = NULL;
	ret = g_utf8_to_utf16 (utf8, size_spec, &in_read, &out_read, &error);
	if (error) {
		result = FAILED ("The error is %d %s\n", (error)->code, (error)->message);
		g_error_free (error);
		if (ret)
			g_free (ret);
		return result;
	}
	if (in_read != len_in)
		result = FAILED ("Read size is incorrect: expected %d but was %d\n", len_in, in_read);
	else if (out_read != len_out)
		result = FAILED ("Converted size is incorrect: expected %d but was %d\n", len_out, out_read);
	else
		result = compare_strings_utf16_RESULT (expected, ret, len_out);

	g_free (ret);
	if (result)
		return result;

	return OK;
}


RESULT
compare_utf8_to_utf16 (const gunichar2 *expected, const gchar *utf8, glong len_in, glong len_out)
{
	RESULT result;

	result = compare_utf8_to_utf16_explicit (expected, utf8, len_in, len_out, -1);
	if (result != OK)
		return result;
	return compare_utf8_to_utf16_explicit (expected, utf8, len_in, len_out, len_in);
}

RESULT
test_utf8_to_utf16 ()
{
	const gchar *src0 = "", *src1 = "ABCDE";
	gunichar2 str0 [1], str1 [6];
	RESULT result;

	str0 [0] = 0;
	gchar_to_gunichar2 (str1, src1);

	/* empty string */
	result = compare_utf8_to_utf16 (str0, src0, 0, 0);
	if (result != OK)
		return result;

	result = compare_utf8_to_utf16 (str1, src1, 5, 5);
	if (result != OK)
		return result;

	return OK;
}


/*
 * test initialization
 */

static Test utf8_tests [] = {
	{"g_utf16_to_utf8", test_utf16_to_utf8},
	{"g_utf8_to_utf16", test_utf8_to_utf16},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(utf8_tests_init, utf8_tests)

