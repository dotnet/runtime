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

#undef CONFIG_STRING
#undef RELEASE_CONFIG_STRING
#undef RELEASE_CONFIG_METHODSET
#undef RELEASE_CONFIG_INTEGER
