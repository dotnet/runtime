// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __smopenum_h__
#define __smopenum_h__

typedef enum smopcode_t {
#define SMOPDEF(smname, string) smname,
#include "smopcode.def"
#undef SMOPDEF

    SM_COUNT, /* number of state machine opcodes */

} SM_OPCODE;

#endif /* __smopenum_h__ */
