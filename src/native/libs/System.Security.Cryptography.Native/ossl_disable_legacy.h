// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// If this file is included in a source file, in some certain configurations disable obsolete APIs to ensure
// no new usages of obsolete APIs are introduced.

#pragma once

#if !defined(FEATURE_DISTRO_AGNOSTIC_SSL) && OPENSSL_VERSION_MAJOR >= 3
#ifdef OPENSSL_API_COMPAT
#undef OPENSSL_API_COMPAT
#endif

#define OPENSSL_API_COMPAT 0x30000000L
#define OPENSSL_NO_DEPRECATED 1
#endif
