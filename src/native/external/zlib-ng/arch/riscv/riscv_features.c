#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/utsname.h>

#if defined(__linux__) && defined(HAVE_SYS_AUXV_H)
#  include <sys/auxv.h>
#endif

#include "zbuild.h"
#include "riscv_features.h"

#define ISA_V_HWCAP (1 << ('v' - 'a'))

int Z_INTERNAL is_kernel_version_greater_or_equal_to_6_5() {
    struct utsname buffer;
    if (uname(&buffer) == -1) {
        // uname failed
        return 0;
    }

    int major, minor;
    if (sscanf(buffer.release, "%d.%d", &major, &minor) != 2) {
        // Something bad with uname()
        return 0;
    }

    if (major > 6 || (major == 6 && minor >= 5))
        return 1;
    return 0;
}

void Z_INTERNAL riscv_check_features_compile_time(struct riscv_cpu_features *features) {
#if defined(__riscv_v) && defined(__linux__)
    features->has_rvv = 1;
#else
    features->has_rvv = 0;
#endif
}

void Z_INTERNAL riscv_check_features_runtime(struct riscv_cpu_features *features) {
#if defined(__linux__) && defined(HAVE_SYS_AUXV_H)
    unsigned long hw_cap = getauxval(AT_HWCAP);
#else
    unsigned long hw_cap = 0;
#endif
    features->has_rvv = hw_cap & ISA_V_HWCAP;
}

void Z_INTERNAL riscv_check_features(struct riscv_cpu_features *features) {
    if (is_kernel_version_greater_or_equal_to_6_5())
        riscv_check_features_runtime(features);
    else
        riscv_check_features_compile_time(features);
    if (features->has_rvv) {
        size_t e8m1_vec_len;
        intptr_t vtype_reg_val;
        // Check that a vuint8m1_t vector is at least 16 bytes and that tail
        // agnostic and mask agnostic mode are supported
        //
        __asm__ volatile(
                "vsetvli %0, zero, e8, m1, ta, ma\n\t"
                "csrr %1, vtype"
                : "=r"(e8m1_vec_len), "=r"(vtype_reg_val));

        // The RVV target is supported if the VILL bit of VTYPE (the MSB bit of
        // VTYPE) is not set and the length of a vuint8m1_t vector is at least 16
        // bytes
        features->has_rvv = (vtype_reg_val >= 0 && e8m1_vec_len >= 16);
    }
}
