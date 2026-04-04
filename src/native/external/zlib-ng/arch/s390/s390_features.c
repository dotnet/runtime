#include "zbuild.h"
#include "s390_features.h"

#ifdef HAVE_SYS_AUXV_H
#  include <sys/auxv.h>
#endif

#ifndef HWCAP_S390_VXRS
#define HWCAP_S390_VXRS (1 << 11)
#endif

void Z_INTERNAL s390_check_features(struct s390_cpu_features *features) {
    features->has_vx = getauxval(AT_HWCAP) & HWCAP_S390_VXRS;
}
