#include "test.h"

static RESULT
test_swap (void)
{
	guint32 a = 0xabcdef01, res32;
	guint64 b = (((guint64)a) << 32) | a, res64;
	guint64 b_expect = (((guint64)0x1efcdab) << 32) | 0x01efcdab;
	guint16 c = 0xabcd, res16;
	
	res32 = GUINT32_SWAP_LE_BE (a);
	if (res32 != 0x01efcdab)
		return FAILED ("GUINT32_SWAP_LE_BE returned 0x%x", res32);
	res32 = GUINT32_SWAP_LE_BE (1);
	if (res32 != 0x1000000)
		return FAILED ("GUINT32_SWAP_LE_BE returned 0x%x", res32);

	res64 = GUINT64_SWAP_LE_BE(b);
	if (res64 != b_expect)
		return FAILED ("GUINT64_SWAP_LE_BE returned 0x%" PRIx64 " (had=0x%" PRIx64 ")", (guint64)res64, (guint64)b);
	res16 = GUINT16_SWAP_LE_BE(c);
	if (res16 != 0xcdab)
		return FAILED ("GUINT16_SWAP_LE_BE returned 0x%x", (guint32) res16);	
	
	return OK;
}

/*
 * test initialization
 */

static Test endian_tests [] = {
	{"swap", test_swap},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(endian_tests_init, endian_tests)
