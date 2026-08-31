#include "config.h"
#include "glib.h"

typedef enum {
	Black = 0,
	Red = 1,
	Blue = 2,
	Purple = Red | Blue, // 3
	Green = 4,
	Yellow = Red | Green, // 5
	White = 7,
} Color;

G_ENUM_FUNCTIONS (Color)

static void
test_enum1 (void)
{
	const Color green = Green;
	const Color blue = Blue;
	const Color red = Red;
	const Color white = White;
	const Color purple = Purple;

	g_assert ((red & blue) == Black);
	g_assert ((red | blue | green) == White);
	g_assert ((red | blue) == Purple);
	g_assert ((white ^ purple) == green);

	Color c = Black;
	Color c2 = Black;
	c |= red;
	g_assert (c == Red);
	c ^= red;
	g_assert (c == Black);

	c |= (c2 |= Red) | Blue;
	g_assert (c == Purple);
	g_assert (c2 == Red);

	c = c2 = Black;
	c |= (c2 |= Red) |= Blue;
	g_assert (c == Purple);
	g_assert (c2 == Purple);

	c = red;
	c &= red;
	g_assert (c == Red);
	c &= blue;
	g_assert (c == Black);
}

#include "test.h"

static RESULT
test_enum (void)
{
	test_enum1 ();
	return OK;
}

const static Test enum_tests [2] = {{"test_enum", test_enum}};

extern "C"
{
DEFINE_TEST_GROUP_INIT (enum_tests_init, enum_tests)
}
