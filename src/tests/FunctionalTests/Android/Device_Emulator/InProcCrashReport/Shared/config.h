// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// AndroidAppBuilder does not supply CoreCLR's platform/architecture definitions.
#define HOST_UNIX 1
#define TARGET_UNIX 1
#define PLATFORM_UNIX 1
#define TARGET_ANDROID 1

#if defined(__x86_64__)
#define HOST_AMD64 1
#define TARGET_AMD64 1
#elif defined(__aarch64__)
#define HOST_ARM64 1
#define TARGET_ARM64 1
#elif defined(__arm__)
#define HOST_ARM 1
#define TARGET_ARM 1
#elif defined(__i386__)
#define HOST_X86 1
#define TARGET_X86 1
#else
#error Unsupported Android architecture
#endif

#if defined(__LP64__)
#define HOST_64BIT 1
#define TARGET_64BIT 1
#endif
