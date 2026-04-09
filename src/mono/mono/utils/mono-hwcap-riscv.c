/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include <mono/utils/mono-hwcap.h>

#include <sys/auxv.h>

void
mono_hwcap_arch_init (void)
{
	// See arch/riscv/include/uapi/asm/hwcap.h in the kernel source tree.

	unsigned long hwcap;

	if ((hwcap = getauxval (AT_HWCAP))) {
		// COMPAT_HWCAP_ISA_A
		if (hwcap & (1 << ('A' - 'A')))
			mono_hwcap_riscv_has_stdext_a = TRUE;

		// COMPAT_HWCAP_ISA_C
		if (hwcap & (1 << ('C' - 'A')))
			mono_hwcap_riscv_has_stdext_c = TRUE;

		// COMPAT_HWCAP_ISA_D
		if (hwcap & (1 << ('D' - 'A')))
			mono_hwcap_riscv_has_stdext_d = TRUE;

		// COMPAT_HWCAP_ISA_F
		if (hwcap & (1 << ('F' - 'A')))
			mono_hwcap_riscv_has_stdext_f = TRUE;

		// Why does COMPAT_HWCAP_ISA_I even exist...?

		// COMPAT_HWCAP_ISA_M
		if (hwcap & (1 << ('M' - 'A')))
			mono_hwcap_riscv_has_stdext_m = TRUE;
	}
}
