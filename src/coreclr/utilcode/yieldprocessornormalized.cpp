// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

// Defaults are for when InitializeYieldProcessorNormalized has not yet been called or when no measurement is done, and are
// tuned for Skylake processors
unsigned int g_yieldsPerNormalizedYield = 1; // current value is for Skylake processors, this is expected to be ~8 for pre-Skylake
unsigned int g_optimalMaxNormalizedYieldsPerSpinIteration = 7;
