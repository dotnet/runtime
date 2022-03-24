// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file is here because we share some common code with the CLR and that platform uses common.h as a
// precompiled header. Due to limitations on precompilation (a precompiled header must be included first
// and must not be preceded by any other preprocessor directive) we cannot conditionally include common.h,
// so the simplest solution is to maintain this empty header under Redhawk.
//

//
// For our DAC build, we precompile gcrhenv.h because it is extremely large (~3MB of text).  For non-DAC
// builds, we do not do this because the majority of the files have more constrained #includes.
//

#include "stdint.h"
