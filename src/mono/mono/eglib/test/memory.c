#include <glib.h>
#include "test.h"

static RESULT
test_memory_zero_size_allocations (void)
{
	gpointer p;

	p = g_malloc (0);
	if (p)
		return FAILED ("Calling g_malloc with size zero should return NULL.");

	p = g_malloc0 (0);
	if (p)
		return FAILED ("Calling g_malloc0 with size zero should return NULL.");

	p = g_realloc (NULL, 0);
	if (p)
		return FAILED ("Calling g_realloc with size zero should return NULL.");

	p = g_new (int, 0);
	if (p)
		return FAILED ("Calling g_new with size zero should return NULL.");

	p = g_new0 (int, 0);
	if (p)
		return FAILED ("Calling g_new0 with size zero should return NULL.");

	return OK;
}

/*
 * Test the following macros with alignment value as signed int
 *     <> ALIGN_TO
 *     <> ALIGN_DOWN_TO
 *     <> ALIGN_PTR_TO
 */
static RESULT
test_align_signed_int (void)
{
	gssize orig_value = 67;
	gssize result, exp_value;
	gpointer orig_ptr = (gpointer)orig_value;
	gpointer result_ptr, exp_ptr;

	// test case #1
	int align = 1;
	result = ALIGN_TO (orig_value, align);
	exp_value = orig_value;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	result = ALIGN_DOWN_TO (orig_value, align);
	exp_value = orig_value;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	result_ptr = ALIGN_PTR_TO (orig_ptr,align);
	exp_ptr = orig_ptr;
	if (result_ptr != exp_ptr)
		return FAILED ("Expected address after aligned is %p, however, the actual address is %p", exp_ptr, result_ptr);

	// test case #2
	align = 8;
	result = ALIGN_TO (orig_value, align);
	exp_value = 72;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	result_ptr = ALIGN_PTR_TO (orig_ptr,align);
	exp_ptr= (gpointer)exp_value;
	if (result_ptr != exp_ptr)
		return FAILED ("Expected address after aligned is %p, however, the actual address is %p", exp_ptr, result_ptr);

	result = ALIGN_DOWN_TO (orig_value, align);
	exp_value = 64;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	return OK;
}

/*
 * Test the following macros with alignment value as unsigned int
 *     <> ALIGN_TO
 *     <> ALIGN_DOWN_TO
 *     <> ALIGN_PTR_TO
 */
static RESULT
test_align_unsigned_int (void)
{
	gssize orig_value = 67;
	gssize result, exp_value;
	gpointer orig_ptr = (gpointer)orig_value;
	gpointer result_ptr, exp_ptr;

	// test case #1
	unsigned int align = 1;
	result = ALIGN_TO (orig_value, align);
	exp_value = orig_value;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	result = ALIGN_DOWN_TO (orig_value, align);
	exp_value = orig_value;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	result_ptr = ALIGN_PTR_TO (orig_ptr,align);
	exp_ptr = orig_ptr;
	if (result_ptr != exp_ptr)
		return FAILED ("Expected address after aligned is %p, however, the actual address is %p", exp_ptr, result_ptr);

	// test case #2
	align= 16;
	result = ALIGN_TO (orig_value, align);
	exp_value = 80;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	result_ptr = ALIGN_PTR_TO (orig_ptr,align);
	exp_ptr= (gpointer)exp_value;
	if (result_ptr != exp_ptr)
		return FAILED ("Expected address after aligned is %p, however, the actual address is %p", exp_ptr, result_ptr);

	result = ALIGN_DOWN_TO (orig_value, align);
	exp_value = 64;
	if (result != exp_value)
		return FAILED ("Expected value after aligned is %d, however, the actual value is %d", exp_value, result);

	return OK;
}

static Test memory_tests [] = {
	{"zero_size_allocations", test_memory_zero_size_allocations},
	{"align_signed_int", test_align_signed_int},
	{"align_unsigned_int", test_align_unsigned_int},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(memory_tests_init, memory_tests)
