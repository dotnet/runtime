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
CONFIG_METHODSET(InterpHaltOnCall, "InterpHaltOnCall"); // Assert in the compiler when compiling a call to these method(s)
CONFIG_METHODSET(InterpHalt, "InterpHalt");
CONFIG_METHODSET(InterpDump, "InterpDump");
CONFIG_INTEGER(InterpList, "InterpList", 0); // List the methods which are compiled by the interpreter JIT
RELEASE_CONFIG_INTEGER(InterpMode, "InterpMode", 0); // Interpreter mode, one of the following:
// 0: default, do not use interpreter except explicit opt-in via DOTNET_Interpreter
// 1: use interpreter for everything except (1) methods that have R2R compiled code and (2) all code in System.Private.CoreLib. All code in System.Private.CoreLib falls back to JIT if there is no R2R available for it.
// 2: use interpreter for everything except intrinsics. All intrinsics fallback to JIT. Implies DOTNET_ReadyToRun=0.
// 3: use interpreter for everything, the full interpreter-only mode, no fallbacks to R2R or JIT whatsoever. Implies DOTNET_ReadyToRun=0, DOTNET_EnableHWIntrinsic=0

#undef CONFIG_STRING
#undef RELEASE_CONFIG_STRING
#undef RELEASE_CONFIG_METHODSET
#undef RELEASE_CONFIG_INTEGER
