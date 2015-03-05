//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include "stdafx.h"

//
// This file exists just to pull in platform specific source files. More precisely we're switching on the
// platform being targetted for debugging (not the platform we're currently building debugger executables
// for). We do this instead of using build rules to overcome limitations with build when it comes including
// different source files based on build macros.
//

#if FEATURE_DBGIPC_TRANSPORT_DI
#include "dbgtransportpipeline.cpp"
#include "shimremotedatatarget.cpp"
#include "remoteeventchannel.cpp"
#else
#include "WindowsPipeline.cpp"
#include "EventRedirectionPipeline.cpp"
#include "ShimLocalDataTarget.cpp"
#include "LocalEventChannel.cpp"
#endif 

#if DBG_TARGET_X86
#include "i386/cordbregisterset.cpp"
#include "i386/primitives.cpp"
#elif DBG_TARGET_AMD64
#include "amd64/cordbregisterset.cpp"
#include "amd64/primitives.cpp"
#elif DBG_TARGET_ARM
#include "arm/cordbregisterset.cpp"
#include "arm/primitives.cpp"
#elif DBG_TARGET_ARM64
#include "arm64/cordbregisterset.cpp"
#include "arm64/primitives.cpp"
#else
#error Unsupported platform
#endif
