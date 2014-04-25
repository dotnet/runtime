/*
 * mono-hwcap-arm64.c: ARM hardware feature detection
 *
 * Copyright 2013 Xamarin Inc
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
