#include "test.h"

/*
 * g_unichar_type
 */
static RESULT
test_g_unichar_type (void)
{
	if (g_unichar_type ('A') != G_UNICODE_UPPERCASE_LETTER)
		return FAILED ("#1");
	if (g_unichar_type ('a') != G_UNICODE_LOWERCASE_LETTER)
		return FAILED ("#2");
	if (g_unichar_type ('1') != G_UNICODE_DECIMAL_NUMBER)
		return FAILED ("#3");
	if (g_unichar_type (0xA3) != G_UNICODE_CURRENCY_SYMBOL)
		return FAILED ("#4");
	return NULL;
}

/*
 * g_unichar_toupper
 */
static RESULT
test_g_unichar_toupper (void)
{
	if (g_unichar_toupper (0) != 0)
		return FAILED ("#0");
	if (g_unichar_toupper ('a') != 'A')
		return FAILED ("#1");
	if (g_unichar_toupper ('1') != '1')
		return FAILED ("#2");
	if (g_unichar_toupper (0x1C4) != 0x1C4)
		return FAILED ("#3");
	if (g_unichar_toupper (0x1F2) != 0x1F1)
		return FAILED ("#4");
	if (g_unichar_toupper (0x1F3) != 0x1F1)
		return FAILED ("#5");
	if (g_unichar_toupper (0xFFFF) != 0xFFFF)
		return FAILED ("#6");
	if (g_unichar_toupper (0x10428) != 0x10400)
		return FAILED ("#7");
	return NULL;
}

/*
 * g_unichar_tolower
 */
static RESULT
test_g_unichar_tolower (void)
{
	if (g_unichar_tolower (0) != 0)
		return FAILED ("#0");
	if (g_unichar_tolower ('A') != 'a')
		return FAILED ("#1");
	if (g_unichar_tolower ('1') != '1')
		return FAILED ("#2");
	if (g_unichar_tolower (0x1C5) != 0x1C6)
		return FAILED ("#3");
	if (g_unichar_tolower (0x1F1) != 0x1F3)
		return FAILED ("#4");
	if (g_unichar_tolower (0x1F2) != 0x1F3)
		return FAILED ("#5");
	if (g_unichar_tolower (0xFFFF) != 0xFFFF)
		return FAILED ("#6");
	return NULL;
}

/*
 * g_unichar_totitle
 */
static RESULT
test_g_unichar_totitle (void)
{
	if (g_unichar_toupper (0) != 0)
		return FAILED ("#0");
	if (g_unichar_totitle ('a') != 'A')
		return FAILED ("#1");
	if (g_unichar_totitle ('1') != '1')
		return FAILED ("#2");
	if (g_unichar_totitle (0x1C4) != 0x1C5)
		return FAILED ("#3");
	if (g_unichar_totitle (0x1F2) != 0x1F2)
		return FAILED ("#4");
	if (g_unichar_totitle (0x1F3) != 0x1F2)
		return FAILED ("#5");
	if (g_unichar_toupper (0xFFFF) != 0xFFFF)
		return FAILED ("#6");
	return NULL;
}

static Test unicode_tests [] = {
	{"g_unichar_type", test_g_unichar_type},
	{"g_unichar_toupper", test_g_unichar_toupper},
	{"g_unichar_tolower", test_g_unichar_tolower},
	{"g_unichar_totitle", test_g_unichar_totitle},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(unicode_tests_init, unicode_tests)
