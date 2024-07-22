#ifndef S390_FEATURES_H_
#define S390_FEATURES_H_

struct s390_cpu_features {
    int has_vx;
};

void Z_INTERNAL s390_check_features(struct s390_cpu_features *features);

#endif
