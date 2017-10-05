#ifndef WARD_H
#define WARD_H

/*
 * Ward is a static analysis tool that can be used to check for the presense of
 * a certain class of bugs in C code.
 *
 * See https://github.com/evincarofautumn/Ward#annotating-your-code for the Ward
 * permission annotations syntax.
 *
 * The Mono permissions are defined in
 * https://github.com/evincarofautumn/Ward/blob/prod/mono.config
 */
#if defined(__WARD__)
#define MONO_PERMIT(...) __attribute__ ((ward (__VA_ARGS__)))
#else
#define MONO_PERMIT(...) /*empty*/
#endif

#endif
