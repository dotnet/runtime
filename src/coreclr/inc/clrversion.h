// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "runtime_version.h"

#ifndef QUOTE_MACRO
#define QUOTE_MACRO_HELPER(x)       #x
#define QUOTE_MACRO(x)              QUOTE_MACRO_HELPER(x)
#endif

#ifndef QUOTE_MACRO_L
#define QUOTE_MACRO_L_HELPER(x)     L###x
#define QUOTE_MACRO_L(x)            QUOTE_MACRO_L_HELPER(x)
#endif

#define CLR_METADATA_VERSION        "v4.0.30319"
#define CLR_METADATA_VERSION_L      W("v4.0.30319")

#define CLR_PRODUCT_VERSION         QUOTE_MACRO(RuntimeProductVersion)
#define CLR_PRODUCT_VERSION_L       QUOTE_MACRO_L(RuntimeProductVersion)

#define VER_ASSEMBLYVERSION_STR     QUOTE_MACRO(RuntimeAssemblyMajorVersion.RuntimeAssemblyMinorVersion.0.0)
#define VER_ASSEMBLYVERSION_STR_L   QUOTE_MACRO_L(RuntimeAssemblyMajorVersion.RuntimeAssemblyMinorVersion.0.0)

#define VER_FILEVERSION_STR         QUOTE_MACRO(RuntimeFileMajorVersion.RuntimeFileMinorVersion.RuntimeFileBuildVersion.RuntimeFileRevisionVersion)
#define VER_FILEVERSION_STR_L       QUOTE_MACRO_L(RuntimeFileMajorVersion.RuntimeFileMinorVersion.RuntimeFileBuildVersion.RuntimeFileRevisionVersion)

#define VER_LEGALCOPYRIGHT_LOGO_STR    "Copyright (c) Microsoft Corporation.  All rights reserved."
#define VER_LEGALCOPYRIGHT_LOGO_STR_L L"Copyright (c) Microsoft Corporation.  All rights reserved."
