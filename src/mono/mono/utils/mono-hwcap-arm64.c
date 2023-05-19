/**
 * \file
 * ARM64 hardware feature detection
 *
 * Copyright 2013 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifdef __APPLE__
#include <sys/types.h>
#include <sys/sysctl.h>
#endif

#include "mono/utils/mono-hwcap.h"

void
mono_hwcap_arch_init (void)
{
#ifdef __APPLE__
	const char *prop;
	guint val [16];
	size_t val_len;
	int res;

	val_len = sizeof (val);
	prop = "hw.optional.armv8_crc32";
	res = sysctlbyname (prop, val, &val_len, NULL, 0);
	g_assert (res == 0);
	g_assert (val_len == 4);
	mono_hwcap_arm64_has_crc32 = *(int*)val;

	val_len = sizeof (val);
	prop = "hw.optional.arm.FEAT_RDM";
	res = sysctlbyname (prop, val, &val_len, NULL, 0);
	g_assert (res == 0);
	g_assert (val_len == 4);
	mono_hwcap_arm64_has_rdm = *(int*)val;

	val_len = sizeof (val);
	prop = "hw.optional.arm.FEAT_DotProd";
	res = sysctlbyname (prop, val, &val_len, NULL, 0);
	g_assert (res == 0);
	g_assert (val_len == 4);
	mono_hwcap_arm64_has_dot = *(int*)val;

	val_len = sizeof (val);
	prop = "hw.optional.arm.FEAT_SHA1";
	res = sysctlbyname (prop, val, &val_len, NULL, 0);
	g_assert (res == 0);
	g_assert (val_len == 4);
	mono_hwcap_arm64_has_sha1 = *(int*)val;

	val_len = sizeof (val);
	prop = "hw.optional.arm.FEAT_SHA256";
	res = sysctlbyname (prop, val, &val_len, NULL, 0);
	g_assert (res == 0);
	g_assert (val_len == 4);
	mono_hwcap_arm64_has_sha256 = *(int*)val;

	val_len = sizeof (val);
	prop = "hw.optional.arm.FEAT_AES";
	res = sysctlbyname (prop, val, &val_len, NULL, 0);
	g_assert (res == 0);
	g_assert (val_len == 4);
	mono_hwcap_arm64_has_aes = *(int*)val;

#endif
}
