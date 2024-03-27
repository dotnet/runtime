#include "dn-simdhash.h"

#ifndef DN_SIMDHASH_KEY_T
#error Expected DN_SIMDHASH_KEY_T definition
#endif

#ifndef DN_SIMDHASH_VALUE_T
#error Expected DN_SIMDHASH_VALUE_T definition
#endif

#ifndef DN_SIMDHASH_BUCKET_COUNT
#error Expected DN_SIMDHASH_BUCKET_COUNT definition
#endif

#define DN_SIMDHASH_BUCKET_T (DN_SIMDHASH_T ## _bucket)

typedef struct {
    dn_simdhash_suffixes suffixes;
    DN_SIMDHASH_KEY_T keys[DN_SIMDHASH_BUCKET_COUNT];
} DN_SIMDHASH_BUCKET_T __attribute__((aligned(16)));
