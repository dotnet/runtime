//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// veropcodes.hpp
//

//
// Declares the enumeration of the opcodes and the decoding tables.
//

#include "openum.h"

#define HackInlineAnnData  0x7F

#ifdef DECLARE_DATA
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) L##s,

const WCHAR * const ppOpcodeNameList[] =
{
#include "../inc/opcode.def"
};

#undef OPDEF

#else /* !DECLARE_DATA */

extern const WCHAR * const ppOpcodeNameList[];

#endif /* DECLARE_DATA */
