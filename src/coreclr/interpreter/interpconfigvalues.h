// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef DEBUG
#define CONFIG_STRING(name, key)                RELEASE_CONFIG_STRING(name, key)
#else
#define CONFIG_STRING(name, key)
#endif

#ifdef DEBUG
#define CONFIG_METHODSET(name, key)             RELEASE_CONFIG_METHODSET(name, key)
#else
#define CONFIG_METHODSET(name, key)
#endif

#ifdef DEBUG
#define CONFIG_INTEGER(name, key, defaultValue) RELEASE_CONFIG_INTEGER(name, key, defaultValue)
#else
#define CONFIG_INTEGER(name, key, defaultValue)
#endif

RELEASE_CONFIG_METHODSET(Interpreter, "Interpreter")
CONFIG_METHODSET(InterpHalt, "InterpHalt");
CONFIG_METHODSET(InterpDump, "InterpDump");
CONFIG_INTEGER(InterpList, "InterpList", 0); // List the methods which are compiled by the interpreter JIT
CONFIG_INTEGER(InterpMode, "InterpMode", 0); // Interpreter mode, one of the following:
// 0: default, do not use interpreter except explicit opt-in via DOTNET_Interpreter
// 1: use interpreter for everything except (1) methods that have R2R compiled code and (2) all code in System.Private.CoreLib. All code in System.Private.CoreLib falls back to JIT if there is no R2R available for it. This can replace the testing mode introduced in https://github.com/dotnet/runtime/pull/116570/files#diff-3e5a329159ca5b2268e62be8a0d776b6092681e9b241210cb4d57e3454816abcR403 since it will cover code in non-entrypoint assemblies too and thus will be more comprehensive. This mode should have good balance between speed and coverage. We may want to use it for running libraries tests with interpreter eventually.
// 2: use interpreter for everything except intrinsics. All intrinsics fallback to JIT. Implies DOTNET_ReadyToRun=0.
// 3: use interpreter for everything, the full interpreter-only mode, no fallbacks to R2R or JIT whatsoever. Implies DOTNET_ReadyToRun=0, DOTNET_EnableHWIntrinsic=0,

#undef CONFIG_STRING
#undef RELEASE_CONFIG_STRING
#undef RELEASE_CONFIG_METHODSET
#undef RELEASE_CONFIG_INTEGER
