//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Unmanaged enum with compatibility flags. See compatibilityflagsdef.h for more details.


#ifndef __COMPATIBILITYFLAGS_H__
#define __COMPATIBILITYFLAGS_H__

enum CompatibilityFlag {
#define COMPATFLAGDEF(name) compat##name,
#include "compatibilityflagsdef.h"
    compatCount,
};

#endif // __COMPATIBILITYFLAGS_H__
