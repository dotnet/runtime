/**
 * \file
 */

#include "config.h"

#if defined (TARGET_ARM)

MONO_HWCAP_VAR(arm_is_v5)
MONO_HWCAP_VAR(arm_is_v6)
MONO_HWCAP_VAR(arm_is_v7)
MONO_HWCAP_VAR(arm_has_vfp)
MONO_HWCAP_VAR(arm_has_vfp3)
MONO_HWCAP_VAR(arm_has_vfp3_d16)
MONO_HWCAP_VAR(arm_has_thumb)
MONO_HWCAP_VAR(arm_has_thumb2)

#elif defined (TARGET_ARM64)

// Nothing here yet.

#elif defined (TARGET_MIPS)

// Nothing here yet.

#elif defined (TARGET_POWERPC) || defined (TARGET_POWERPC64)

MONO_HWCAP_VAR(ppc_has_icache_snoop)
MONO_HWCAP_VAR(ppc_is_isa_2x)
MONO_HWCAP_VAR(ppc_is_isa_64)
MONO_HWCAP_VAR(ppc_has_move_fpr_gpr)
MONO_HWCAP_VAR(ppc_has_multiple_ls_units)

#elif defined (TARGET_S390X)

MONO_HWCAP_VAR(s390x_has_fpe)
MONO_HWCAP_VAR(s390x_has_vec)
MONO_HWCAP_VAR(s390x_has_mlt)
MONO_HWCAP_VAR(s390x_has_ia)
MONO_HWCAP_VAR(s390x_has_gie)
MONO_HWCAP_VAR(s390x_has_mie2)
MONO_HWCAP_VAR(s390x_has_gs)

#elif defined (TARGET_SPARC) || defined (TARGET_SPARC64)

MONO_HWCAP_VAR(sparc_is_v9)

#elif defined (TARGET_X86) || defined (TARGET_AMD64)

MONO_HWCAP_VAR(x86_is_xen)
MONO_HWCAP_VAR(x86_has_cmov)
MONO_HWCAP_VAR(x86_has_fcmov)
MONO_HWCAP_VAR(x86_has_sse1)
MONO_HWCAP_VAR(x86_has_sse2)
MONO_HWCAP_VAR(x86_has_sse3)
MONO_HWCAP_VAR(x86_has_ssse3)
MONO_HWCAP_VAR(x86_has_sse41)
MONO_HWCAP_VAR(x86_has_sse42)
MONO_HWCAP_VAR(x86_has_sse4a)

#endif
