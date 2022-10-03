// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "stdafx.h"

//
// This file exists just to pull in platform specific source files. More precisely we're switching on the
// platform being targeted for debugging (not the platform we're currently building debugger executables
// for). We do this instead of using build rules to overcome limitations with build when it comes including
// different source files based on build macros.
//

#if FEATURE_DBGIPC_TRANSPORT_DI
#include "dbgtransportpipeline.cpp"
#include "shimremotedatatarget.cpp"
#include "remoteeventchannel.cpp"
#elif WIN32
#include "windowspipeline.cpp"
#include "eventredirectionpipeline.cpp"
#include "shimlocaldatatarget.cpp"
#include "localeventchannel.cpp"
#endif

#if TARGET_X86
#include "i386/cordbregisterset.cpp"
#include "i386/primitives.cpp"
#elif TARGET_AMD64
#include "amd64/cordbregisterset.cpp"
#include "amd64/primitives.cpp"
#elif TARGET_ARM
#include "arm/cordbregisterset.cpp"
#include "arm/primitives.cpp"
#elif TARGET_ARM64
#include "arm64/cordbregisterset.cpp"
#include "arm64/primitives.cpp"
#elif TARGET_LOONGARCH64
#include "loongarch64/cordbregisterset.cpp"
#include "loongarch64/primitives.cpp"
#else
#error Unsupported platform
#endif
