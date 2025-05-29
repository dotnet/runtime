// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef DEBUG
#define CONFIG_STRING(name, key)                RELEASE_CONFIG_STRING(name, key)
#else
#define CONFIG_STRING(name, key)
#endif

RELEASE_CONFIG_STRING(Interpreter, "Interpreter")
CONFIG_STRING(InterpHalt, "InterpHalt");
CONFIG_STRING(InterpDump, "InterpDump");

#undef CONFIG_STRING
#undef RELEASE_CONFIG_STRING
