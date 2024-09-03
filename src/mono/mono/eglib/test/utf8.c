#include <stdlib.h>

#include "test.h"

/*
 * g_utf16_to_utf8
 */

static glong
compare_strings_utf8_pos (const gchar *expected, const gchar *actual, glong size)
{
	int i;
	for (i = 0; i < size; i++)
		if (expected [i] != actual [i])
			return i;
	return -1;
}

static RESULT
compare_strings_utf8_RESULT (const gchar *expected, const gchar *actual, glong size)
{
	glong ret;

	ret = compare_strings_utf8_pos (expected, actual, size);
	if (ret < 0)
		return OK;
	return FAILED ("Incorrect output: expected '%s' but was '%s', differ at %d\n", expected, actual, ret);
}

static void
gchar_to_gunichar2 (gunichar2 ret[], const gchar *src)
{
	int i;

	for (i = 0; src [i]; i++)
		ret [i] = src [i];
	ret [i] = 0;
}

static RESULT
compare_utf16_to_utf8_explicit (const gchar *expected, const gunichar2 *utf16, glong len_in, glong len_out, glong size_spec)
{
	GError *gerror;
	gchar* ret;
	RESULT result;
	glong in_read, out_read;

	result = NULL;

	gerror = NULL;
	ret = g_utf16_to_utf8 (utf16, size_spec, &in_read, &out_read, &gerror);
	if (gerror) {
		result = FAILED ("The error is %d %s\n", gerror->code, gerror->message);
		g_error_free (gerror);
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

static RESULT
compare_utf16_to_utf8 (const gchar *expected, const gunichar2 *utf16, glong len_in, glong len_out)
{
	RESULT result;

	result = compare_utf16_to_utf8_explicit (expected, utf16, len_in, len_out, -1);
	if (result != OK)
		return result;
	return compare_utf16_to_utf8_explicit (expected, utf16, len_in, len_out, len_in);
}

static RESULT
test_utf16_to_utf8 (void)
{
	const gchar *src0 = "", *src1 = "ABCDE", *src2 = "\xE5\xB9\xB4\x27", *src3 = "\xEF\xBC\xA1", *src4 = "\xEF\xBD\x81", *src5 = "\xF0\x90\x90\x80";
	gunichar2 str0 [] = {0}, str1 [6], str2 [] = {0x5E74, 39, 0}, str3 [] = {0xFF21, 0}, str4 [] = {0xFF41, 0}, str5 [] = {0xD801, 0xDC00, 0};
	RESULT result;

	gchar_to_gunichar2 (str1, src1);

	/* empty string */
	result = compare_utf16_to_utf8 (src0, str0, 0, 0);
	if (result != OK)
		return result;

	result = compare_utf16_to_utf8 (src1, str1, 5, 5);
	if (result != OK)
		return result;
	result = compare_utf16_to_utf8 (src2, str2, 2, 4);
	if (result != OK)
		return result;
	result = compare_utf16_to_utf8 (src3, str3, 1, 3);
	if (result != OK)
		return result;
	result = compare_utf16_to_utf8 (src4, str4, 1, 3);
	if (result != OK)
		return result;
	result = compare_utf16_to_utf8 (src5, str5, 2, 4);
	if (result != OK)
		return result;

	return OK;
}

/*
 * g_utf8_to_utf16
 */

static glong
compare_strings_utf16_pos (const gunichar2 *expected, const gunichar2 *actual, glong size)
{
	int i;
	for (i = 0; i < size; i++)
		if (expected [i] != actual [i])
			return i;
	return -1;
}

static RESULT
compare_strings_utf16_RESULT (const gunichar2 *expected, const gunichar2 *actual, glong size)
{
	glong ret;

	ret = compare_strings_utf16_pos (expected, actual, size);
	if (ret < 0)
		return OK;
	return FAILED ("Incorrect output: expected '%s' but was '%s', differ at %d ('%c' x '%c')\n", expected, actual, ret, expected [ret], actual [ret]);
}

#if !defined(EGLIB_TESTS)
#define eg_utf8_to_utf16_with_nuls g_utf8_to_utf16
#endif

static RESULT
compare_utf8_to_utf16_explicit (const gunichar2 *expected, const gchar *utf8, glong len_in, glong len_out, glong size_spec, gboolean include_nuls)
{
	GError *gerror;
	gunichar2* ret;
	RESULT result;
	glong in_read, out_read;

	result = NULL;

	gerror = NULL;
	if (include_nuls)
		ret = g_utf8_to_utf16 (utf8, size_spec, &in_read, &out_read, &gerror);
	else
		ret = g_utf8_to_utf16 (utf8, size_spec, &in_read, &out_read, &gerror);

	if (gerror) {
		result = FAILED ("The error is %d %s\n", gerror->code, gerror->message);
		g_error_free (gerror);
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

static RESULT
compare_utf8_to_utf16_general (const gunichar2 *expected, const gchar *utf8, glong len_in, glong len_out, gboolean include_nuls)
{
	RESULT result;

	result = compare_utf8_to_utf16_explicit (expected, utf8, len_in, len_out, -1, include_nuls);
	if (result != OK)
		return result;
	return compare_utf8_to_utf16_explicit (expected, utf8, len_in, len_out, len_in, include_nuls);
}

static RESULT
compare_utf8_to_utf16 (const gunichar2 *expected, const gchar *utf8, glong len_in, glong len_out)
{
	return compare_utf8_to_utf16_general (expected, utf8, len_in, len_out, FALSE);
}

static RESULT
compare_utf8_to_utf16_with_nuls (const gunichar2 *expected, const gchar *utf8, glong len_in, glong len_out)
{
	return compare_utf8_to_utf16_explicit (expected, utf8, len_in, len_out, len_in, TRUE);
}

static RESULT
test_utf8_seq (void)
{
	const gchar *src = "\xE5\xB9\xB4\x27";
	glong in_read, out_read;
	//gunichar2 expected [6];
	GError *gerror = NULL;
	gunichar2 *dst;

	//printf ("got: %s\n", src);
	dst = g_utf8_to_utf16 (src, (glong)strlen (src), &in_read, &out_read, &gerror);
	if (gerror != NULL){
		return gerror->message;
	}

	if (in_read != 4) {
		return FAILED ("in_read is expected to be 4 but was %d\n", in_read);
	}
	if (out_read != 2) {
		return FAILED ("out_read is expected to be 2 but was %d\n", out_read);
	}
	g_free (dst);

	return OK;
}

static RESULT
test_utf8_to_utf16 (void)
{
	const gchar *src0 = "", *src1 = "ABCDE", *src2 = "\xE5\xB9\xB4\x27", *src3 = "\xEF\xBC\xA1", *src4 = "\xEF\xBD\x81";
	gunichar2 str0 [] = {0}, str1 [6], str2 [] = {0x5E74, 39, 0}, str3 [] = {0xFF21, 0}, str4 [] = {0xFF41, 0};
	RESULT result;

	gchar_to_gunichar2 (str1, src1);

	/* empty string */
	result = compare_utf8_to_utf16 (str0, src0, 0, 0);
	if (result != OK)
		return result;

	result = compare_utf8_to_utf16 (str1, src1, 5, 5);
	if (result != OK)
		return result;
	result = compare_utf8_to_utf16 (str2, src2, 4, 2);
	if (result != OK)
		return result;
	result = compare_utf8_to_utf16 (str3, src3, 3, 1);
	if (result != OK)
		return result;
	result = compare_utf8_to_utf16 (str4, src4, 3, 1);
	if (result != OK)
		return result;

	return OK;
}

static RESULT
test_utf8_to_utf16_with_nuls (void)
{
	const gchar *src0 = "", *src1 = "AB\0DE", *src2 = "\xE5\xB9\xB4\x27", *src3 = "\xEF\xBC\xA1", *src4 = "\xEF\xBD\x81";
	gunichar2 str0 [] = {0}, str1 [] = {'A', 'B', 0, 'D', 'E', 0}, str2 [] = {0x5E74, 39, 0}, str3 [] = {0xFF21, 0}, str4 [] = {0xFF41, 0};
	RESULT result;

#if !defined(EGLIB_TESTS)
	return OK;
#endif

	/* implicit length is forbidden */
		if (g_utf8_to_utf16 (src1, -1, NULL, NULL, NULL) != NULL)
		return FAILED ("explicit nulls must fail with -1 length\n");

	/* empty string */
	result = compare_utf8_to_utf16_with_nuls (str0, src0, 0, 0);
	if (result != OK)
		return result;

	result = compare_utf8_to_utf16_with_nuls  (str1, src1, 5, 5);
	if (result != OK)
		return result;
	result = compare_utf8_to_utf16_with_nuls  (str2, src2, 4, 2);
	if (result != OK)
		return result;
	result = compare_utf8_to_utf16_with_nuls  (str3, src3, 3, 1);
	if (result != OK)
		return result;
	result = compare_utf8_to_utf16_with_nuls  (str4, src4, 3, 1);
	if (result != OK)
		return result;

	return OK;
}

static RESULT
ucs4_to_utf16_check_result (const gunichar2 *result_str, const gunichar2 *expected_str,
			    glong result_items_read, glong expected_items_read,
			    glong result_items_written, glong expected_items_written,
			    GError* result_error, gboolean expect_error)
{
	glong i;
	if (result_items_read != expected_items_read)
		return FAILED("Incorrect number of items read; expected %d, got %d", expected_items_read, result_items_read);
	if (result_items_written != expected_items_written)
		return FAILED("Incorrect number of items written; expected %d, got %d", expected_items_written, result_items_written);
	if (result_error && !expect_error)
		return FAILED("There should not be an error code.");
	if (!result_error && expect_error)
		return FAILED("Unexpected error object.");
	if (expect_error && result_str)
		return FAILED("NULL should be returned when an error occurs.");
	if (!expect_error && !result_str)
		return FAILED("When no error occurs NULL should not be returned.");
	for (i=0; i<expected_items_written;i++) {
		if (result_str [i] != expected_str [i])
			return FAILED("Incorrect value %d at index %d", result_str [i], i);
	}
	if (result_str && result_str[expected_items_written] != '\0')
		return FAILED("Null termination not found at the end of the string.");

	return OK;
}

static RESULT
test_ucs4_to_utf16 (void)
{
	static gunichar str1[12] = {'H','e','l','l','o',' ','W','o','r','l','d','\0'};
	static gunichar2 exp1[12] = {'H','e','l','l','o',' ','W','o','r','l','d','\0'};
	static gunichar str2[3] = {'h',0x80000000,'\0'};
	static gunichar2 exp2[2] = {'h','\0'};
	static gunichar str3[3] = {'h',0xDA00,'\0'};
	static gunichar str4[3] = {'h',0x10FFFF,'\0'};
	static gunichar2 exp4[4] = {'h',0xdbff,0xdfff,'\0'};
	static gunichar str5[7] = {0xD7FF,0xD800,0xDFFF,0xE000,0x110000,0x10FFFF,'\0'};
	static gunichar2 exp5[5] = {0xD7FF,0xE000,0xdbff,0xdfff,'\0'};
	static gunichar str6[2] = {0x10400, '\0'};
	static gunichar2 exp6[3] = {0xD801, 0xDC00, '\0'};
	static glong read_write[12] = {1,1,0,0,0,0,1,1,0,0,1,2};
	gunichar2* res;
	glong items_read, items_written, current_write_index;
	GError* err=0;
	RESULT check_result;
	glong i;

	res = g_ucs4_to_utf16 (str1, 12, &items_read, &items_written, &err);
	check_result = ucs4_to_utf16_check_result (res, exp1, items_read, 11, items_written, 11, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_ucs4_to_utf16 (str2, 0, &items_read, &items_written, &err);
	check_result = ucs4_to_utf16_check_result (res, exp2, items_read, 0, items_written, 0, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_ucs4_to_utf16 (str2, 1, &items_read, &items_written, &err);
	check_result = ucs4_to_utf16_check_result (res, exp2, items_read, 1, items_written, 1, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_ucs4_to_utf16 (str2, 2, &items_read, &items_written, &err);
	check_result = ucs4_to_utf16_check_result (res, 0, items_read, 1, items_written, 0, err, TRUE);
	g_free (res);
	if (check_result) return check_result;

	items_read = items_written = 0;
	err = 0;
	res = g_ucs4_to_utf16 (str3, 2, &items_read, &items_written, &err);
	check_result = ucs4_to_utf16_check_result (res, 0, items_read, 1, items_written, 0, err, TRUE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	err = 0;
	res = g_ucs4_to_utf16 (str4, 5, &items_read, &items_written, &err);
	check_result = ucs4_to_utf16_check_result (res, exp4, items_read, 2, items_written, 3, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	// This loop tests the bounds of the conversion algorithm
	current_write_index = 0;
	for (i=0;i<6;i++) {
		items_read = items_written = 0;
		err = 0;
		res = g_ucs4_to_utf16 (&str5[i], 1, &items_read, &items_written, &err);
		check_result = ucs4_to_utf16_check_result (res, &exp5[current_write_index],
					items_read, read_write[i*2], items_written, read_write[(i*2)+1], err, !read_write[(i*2)+1]);
		if (check_result) return check_result;
		g_free (res);
		current_write_index += items_written;
	}

	items_read = items_written = 0;
	err = 0;
	res = g_ucs4_to_utf16 (str6, 1, &items_read, &items_written, &err);
	check_result = ucs4_to_utf16_check_result (res, exp6, items_read, 1, items_written, 2, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	return OK;
}

static RESULT
utf16_to_ucs4_check_result (const gunichar *result_str, const gunichar *expected_str,
			    glong result_items_read, glong expected_items_read,
			    glong result_items_written, glong expected_items_written,
			    GError* result_error, gboolean expect_error)
{
	glong i;
	if (result_items_read != expected_items_read)
		return FAILED("Incorrect number of items read; expected %d, got %d", expected_items_read, result_items_read);
	if (result_items_written != expected_items_written)
		return FAILED("Incorrect number of items written; expected %d, got %d", expected_items_written, result_items_written);
	if (result_error && !expect_error)
		return FAILED("There should not be an error code.");
	if (!result_error && expect_error)
		return FAILED("Unexpected error object.");
	if (expect_error && result_str)
		return FAILED("NULL should be returned when an error occurs.");
	if (!expect_error && !result_str)
		return FAILED("When no error occurs NULL should not be returned.");
	for (i=0; i<expected_items_written;i++) {
		if (result_str [i] != expected_str [i])
			return FAILED("Incorrect value %d at index %d", result_str [i], i);
	}
	if (result_str && result_str[expected_items_written] != '\0')
		return FAILED("Null termination not found at the end of the string.");

	return OK;
}

static RESULT
test_utf16_to_ucs4 (void)
{
	static gunichar2 str1[12] = {'H','e','l','l','o',' ','W','o','r','l','d','\0'};
	static gunichar exp1[12] = {'H','e','l','l','o',' ','W','o','r','l','d','\0'};
	static gunichar2 str2[7] = {'H', 0xD800, 0xDC01,0xD800,0xDBFF,'l','\0'};
	static gunichar exp2[3] = {'H',0x00010001,'\0'};
	static gunichar2 str3[4] = {'H', 0xDC00 ,'l','\0'};
	static gunichar exp3[2] = {'H','\0'};
	static gunichar2 str4[20] = {0xDC00,0xDFFF,0xDFF,0xD800,0xDBFF,0xD800,0xDC00,0xD800,0xDFFF,
				     0xD800,0xE000,0xDBFF,0xDBFF,0xDBFF,0xDC00,0xDBFF,0xDFFF,0xDBFF,0xE000,'\0'};
	static gunichar exp4[6] = {0xDFF,0x10000,0x103ff,0x10fc00,0x10FFFF,'\0'};
	static gunichar2 str5[3] = {0xD801, 0xDC00, 0};
	static gunichar exp5[2] = {0x10400, 0};
	static glong read_write[33] = {1,0,0,1,0,0,1,1,1,2,1,0,2,2,1,2,2,1,2,1,0,2,1,0,2,2,1,2,2,1,2,1,0};
	gunichar* res;
	glong items_read, items_written, current_read_index,current_write_index;
	GError* err=0;
	RESULT check_result;
	glong i;

	res = g_utf16_to_ucs4 (str1, 12, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp1, items_read, 11, items_written, 11, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_utf16_to_ucs4 (str2, 0, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp2, items_read, 0, items_written, 0, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_utf16_to_ucs4 (str2, 1, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp2, items_read, 1, items_written, 1, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_utf16_to_ucs4 (str2, 2, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp2, items_read, 1, items_written, 1, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_utf16_to_ucs4 (str2, 3, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp2, items_read, 3, items_written, 2, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_utf16_to_ucs4 (str2, 4, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp2, items_read, 3, items_written, 2, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	res = g_utf16_to_ucs4 (str2, 5, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp2, items_read, 4, items_written, 0, err, TRUE);
	if (check_result) return check_result;
	g_free (res);

	items_read = items_written = 0;
	err = 0;
	res = g_utf16_to_ucs4 (str3, 5, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp3, items_read, 1, items_written, 0, err, TRUE);
	if (check_result) return check_result;
	g_free (res);

	// This loop tests the bounds of the conversion algorithm
	current_read_index = current_write_index = 0;
	for (i=0;i<11;i++) {
		items_read = items_written = 0;
		err = 0;
		res = g_utf16_to_ucs4 (&str4[current_read_index], read_write[i*3], &items_read, &items_written, &err);
		check_result = utf16_to_ucs4_check_result (res, &exp4[current_write_index], items_read,
					     read_write[(i*3)+1], items_written, read_write[(i*3)+2], err,
					     !read_write[(i*3)+2]);
		if (check_result) return check_result;
		g_free (res);
		current_read_index += read_write[i*3];
		current_write_index += items_written;
	}

	items_read = items_written = 0;
	err = 0;
	res = g_utf16_to_ucs4 (str5, 2, &items_read, &items_written, &err);
	check_result = utf16_to_ucs4_check_result (res, exp5, items_read, 2, items_written, 1, err, FALSE);
	if (check_result) return check_result;
	g_free (res);

	return OK;
}

static RESULT
test_utf8_strlen (void)
{
	gchar word1 [] = {0xC2, 0x82,0x45,0xE1, 0x81, 0x83,0x58,0xF1, 0x82, 0x82, 0x82,'\0'};//Valid, len = 5
	gchar word2 [] = {0xF1, 0x82, 0x82, 0x82,0xC2, 0x82,0x45,0xE1, 0x81, 0x83,0x58,'\0'};//Valid, len = 5
	gchar word3 [] = {'h','e',0xC2, 0x82,0x45,'\0'};										//Valid, len = 4
	gchar word4 [] = {0x62,0xC2, 0x82,0x45,0xE1, 0x81, 0x83,0x58,'\0'}; 					//Valid, len = 5

	glong len = 0;

	//Test word1
	len = g_utf8_strlen (word1,-1);
	if (len != 5)
		return FAILED ("Word1 expected length of 5, but was %i", len);
	//Do tests with different values for max parameter.
	len = g_utf8_strlen (word1,1);
	if (len != 0)
		return FAILED ("Word1, max = 1, expected length of 0, but was %i", len);
	len = g_utf8_strlen (word1,2);
	if (len != 1)
		return FAILED ("Word1, max = 1, expected length of 1, but was %i", len);
	len = g_utf8_strlen (word1,3);
	if (len != 2)
		return FAILED ("Word1, max = 2, expected length of 2, but was %i", len);

	//Test word2
	len = g_utf8_strlen (word2,-1);
	if (len != 5)
		return FAILED ("Word2 expected length of 5, but was %i", len);

	//Test word3
	len = g_utf8_strlen (word3,-1);
	if (len != 4)
		return FAILED ("Word3 expected length of 4, but was %i", len);

	//Test word4
	len = g_utf8_strlen (word4,-1);
	if (len != 5)
		return FAILED ("Word4 expected length of 5, but was %i", len);

	//Test null case
	len = g_utf8_strlen(NULL,0);
	if (len != 0)
		return FAILED ("Expected passing null to result in a length of 0");
	return OK;
}

static RESULT
test_utf8_validate (void)
{
	gchar invalidWord1 [] = {0xC3, 0x82, 0xC1,0x90,'\0'}; //Invalid, 1nd oct Can't be 0xC0 or 0xC1
	gchar invalidWord2 [] = {0xC1, 0x89, 0x60, '\0'}; //Invalid, 1st oct can not be 0xC1
	gchar invalidWord3 [] = {0xC2, 0x45,0xE1, 0x81, 0x83,0x58,'\0'}; //Invalid, oct after 0xC2 must be > 0x80

	gchar validWord1 [] = {0xC2, 0x82, 0xC3,0xA0,'\0'}; //Valid
	gchar validWord2 [] = {0xC2, 0x82,0x45,0xE1, 0x81, 0x83,0x58,0xF1, 0x82, 0x82, 0x82,'\0'}; //Valid

	const gchar* end;
	gboolean retVal = g_utf8_validate (invalidWord1, -1, &end);
	if (retVal != FALSE)
		return FAILED ("Expected invalidWord1 to be invalid");
	if (end != &invalidWord1 [2])
		return FAILED ("Expected end parameter to be pointing to invalidWord1[2]");

	end = NULL;
	retVal = g_utf8_validate (invalidWord2, -1, &end);
	if (retVal != FALSE)
		return FAILED ("Expected invalidWord2 to be invalid");
	if (end != &invalidWord2 [0])
		return FAILED ("Expected end parameter to be pointing to invalidWord2[0]");

	end = NULL;
	retVal = g_utf8_validate (invalidWord3, -1, &end);
	if (retVal != FALSE)
		return FAILED ("Expected invalidWord3 to be invalid");
	if (end != &invalidWord3 [0])
		return FAILED ("Expected end parameter to be pointing to invalidWord3[1]");

	end = NULL;
	retVal = g_utf8_validate (validWord1, -1, &end);
	if (retVal != TRUE)
		return FAILED ("Expected validWord1 to be valid");
	if (end != &validWord1 [4])
		return FAILED ("Expected end parameter to be pointing to validWord1[4]");

	end = NULL;
	retVal = g_utf8_validate (validWord2, -1, &end);
	if (retVal != TRUE)
		return FAILED ("Expected validWord2 to be valid");
	if (end != &validWord2 [11])
		return FAILED ("Expected end parameter to be pointing to validWord2[11]");
	return OK;
}

static glong
utf8_byteslen (const gchar *src)
{
	int i = 0;
	do {
		if (src [i] == '\0')
			return i;
		i++;
	} while (TRUE);
}

/*
 * test initialization
 */

static Test utf8_tests [] = {
	{"g_utf16_to_utf8", test_utf16_to_utf8},
	{"g_utf8_to_utf16", test_utf8_to_utf16},
	{"g_utf8_to_utf16_nuls", test_utf8_to_utf16_with_nuls},
	{"g_utf8_seq", test_utf8_seq},
	{"g_ucs4_to_utf16", test_ucs4_to_utf16 },
	{"g_utf16_to_ucs4", test_utf16_to_ucs4 },
	{"g_utf8_strlen", test_utf8_strlen },
	{"g_utf8_validate", test_utf8_validate },
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(utf8_tests_init, utf8_tests)
