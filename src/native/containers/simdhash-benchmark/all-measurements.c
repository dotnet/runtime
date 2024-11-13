// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define _CRT_RAND_S

#include <stdint.h>
#include <inttypes.h>
#include <stdio.h>
#include <assert.h>

#include "../dn-vector.h"
#include "../dn-simdhash.h"
#include "../dn-simdhash-utils.h"
#include "../dn-simdhash-specializations.h"

#include "measurement.h"

#undef MEASUREMENT
#define MEASUREMENT(name, data_type, setup, teardown, body) \
    static void DN_SIMDHASH_GLUE(measurement_, name) (void *_data) { \
        data_type data = (data_type)_data; \
        body; \
    }

#include "all-measurements.h"

#undef MEASUREMENT
#define MEASUREMENT(name, data_type, setup, teardown, body) \
    measurement_info DN_SIMDHASH_GLUE(name, _measurement_info) = { \
        #name, \
        setup, \
        DN_SIMDHASH_GLUE(measurement_, name), \
        teardown \
    };

#include "all-measurements.h"
