//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef __smopenum_h__
#define __smopenum_h__

typedef enum smopcode_t
{
#define SMOPDEF(smname,string) smname,
#include "smopcode.def"
#undef  SMOPDEF
  
    SM_COUNT,        /* number of state machine opcodes */
    
} SM_OPCODE;

#endif /* __smopenum_h__ */


