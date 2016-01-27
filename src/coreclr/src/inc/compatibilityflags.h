// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Unmanaged enum with compatibility flags. See compatibilityflagsdef.h for more details.


#ifndef __COMPATIBILITYFLAGS_H__
#define __COMPATIBILITYFLAGS_H__

enum CompatibilityFlag {
#define COMPATFLAGDEF(name) compat##name,
#include "compatibilityflagsdef.h"
    compatCount,
};

#endif // __COMPATIBILITYFLAGS_H__
