/**
 * \file
 */

#ifndef _MONO_UTILS_RAND_WINDOWS_H_
#define _MONO_UTILS_RAND_WINDOWS_H_

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#include <wincrypt.h>
#define MONO_WIN32_CRYPT_PROVIDER_HANDLE HCRYPTPROV

#else

#include <bcrypt.h>
#define MONO_WIN32_CRYPT_PROVIDER_HANDLE BCRYPT_ALG_HANDLE
#endif

MONO_WIN32_CRYPT_PROVIDER_HANDLE
mono_rand_win_open_provider (void);

gboolean
mono_rand_win_gen (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider, guchar *buffer, size_t buffer_size);

gboolean
mono_rand_win_seed (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider, guchar *seed, size_t seed_size);

void
mono_rand_win_close_provider (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider);

#endif /* HOST_WIN32 */
#endif /* _MONO_UTILS_RAND_WINDOWS_H_ */

