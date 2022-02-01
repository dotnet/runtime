/*
 * Fake test allows debugging of the driver itself
 */
 
#include "test.h"

static RESULT
test_fake (void)
{
	return OK;
}

static Test fake_tests [] = {
	{"fake", test_fake},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(fake_tests_init, fake_tests)
