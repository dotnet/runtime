#ifndef TEST_SHARED_H
#define TEST_SHARED_H

/* Test definitions that can be used in the original zlib build environment. */

/* "hello world" would be more standard, but the repeated "hello"
 * stresses the compression code better, sorry...
 */
static const char hello[] = "hello, hello!";
static const int hello_len = sizeof(hello);

/* Clang static analyzer doesn't understand googletest's ASSERT_TRUE, so we need to tell that it's like assert() */
#ifdef __clang_analyzer__
#  undef  ASSERT_TRUE
#  define ASSERT_TRUE assert
#endif

#endif
