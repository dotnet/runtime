/**
 * \file
 */

#ifndef __MONO_UTILS_HWCAP_H__
#define __MONO_UTILS_HWCAP_H__

#include <stdio.h>
#include <glib.h>

#include "config.h"

#include "mono/utils/mono-compiler.h"

#define MONO_HWCAP_VAR(NAME) extern gboolean mono_hwcap_ ## NAME;
#include "mono/utils/mono-hwcap-vars.h"
#undef MONO_HWCAP_VAR

/* Call this function to perform hardware feature detection. Until
 * this function has been called, all feature variables will be
 * FALSE as a default.
 *
 * While this function can be called multiple times, doing so from
 * several threads at the same time is not supported as it will
 * result in an inconsistent state of the variables. Further,
 * feature variables should not be read *while* this function is
 * executing.
 */
void mono_hwcap_init (void);

/* Implemented in mono-hwcap-$TARGET.c. Do not call. */
void mono_hwcap_arch_init (void);

/* Print detected features to stdout. */
void mono_hwcap_print (void);

/* Please note: If you're going to use the Linux auxiliary vector
 * to detect CPU features, don't use any of the constant names in
 * the hwcap.h header. This ties us to a particular version of the
 * header, and since the values are guaranteed to be stable, hard-
 * coding them is not that terrible.
 *
 * Also, please do not add assumptions to mono-hwcap. The code here
 * is meant to *discover* facts about the hardware, not assume that
 * some feature exists because of $arbitrary_preprocessor_define.
 * If you have to make assumptions, do so elsewhere, e.g. in the
 * Mini back end you're modifying.
 *
 * Finally, be conservative. If you can't determine precisely if a
 * feature is present, assume that it isn't. In the rare cases where
 * the hardware or operating system are lying, work around that in
 * a different place, as with the rule above.
 */

#endif /* __MONO_UTILS_HWCAP_H__ */
