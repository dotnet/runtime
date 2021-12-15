// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Definitions of each configuration value used by the RhConfig class.
//
// Each variable is lazily inspected on first query and the resulting value cached for future use. To keep
// things simple we support reading only 32-bit hex quantities and a zero value is considered equivalent to
// the environment variable not being defined. We can get more sophisticated if needs be, but the hope is that
// very few configuration values are exposed in this manner.
//

// By default, print assert to console and break in the debugger, if attached.  Set to 0 for a pop-up dialog on assert.
DEBUG_CONFIG_VALUE_WITH_DEFAULT(BreakOnAssert, 1)

RETAIL_CONFIG_VALUE(StressLogLevel)
RETAIL_CONFIG_VALUE(TotalStressLogSize)
RETAIL_CONFIG_VALUE(gcServer)
DEBUG_CONFIG_VALUE(GcStressThrottleMode)    // gcstm_TriggerAlways / gcstm_TriggerOnFirstHit / gcstm_TriggerRandom
DEBUG_CONFIG_VALUE(GcStressFreqCallsite)    // Number of times to force GC out of GcStressFreqDenom (for GCSTM_RANDOM)
DEBUG_CONFIG_VALUE(GcStressFreqLoop)        // Number of times to force GC out of GcStressFreqDenom (for GCSTM_RANDOM)
DEBUG_CONFIG_VALUE(GcStressFreqDenom)       // Denominator defining frequencies above, 10,000 used when left unspecified (for GCSTM_RANDOM)
DEBUG_CONFIG_VALUE(GcStressSeed)            // Specify Seed for random generator (for GCSTM_RANDOM)
