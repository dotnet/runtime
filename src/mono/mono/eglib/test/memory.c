
#include <glib.h>
#include "test.h"

RESULT
test_memory_zero_size_allocations ()
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


static Test memory_tests [] = {
        {       "zero_size_allocations", test_memory_zero_size_allocations},
        {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(memory_tests_init, memory_tests)

