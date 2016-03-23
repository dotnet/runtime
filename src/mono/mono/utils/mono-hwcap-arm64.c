/*
 * mono-hwcap-arm64.c: ARM hardware feature detection
 *
 * Copyright 2013 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mono/utils/mono-hwcap-arm64.h"

#if defined(MONO_CROSS_COMPILE)
void
mono_hwcap_arch_init (void)
{
}
#else
void
mono_hwcap_arch_init (void)
{
}
#endif

void
mono_hwcap_print(FILE *f)
{
}
