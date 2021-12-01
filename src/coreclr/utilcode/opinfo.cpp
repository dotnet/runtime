// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***************************************************************************/
/*                                OpInfo.cpp                               */
/***************************************************************************/

#include "stdafx.h"
#include <cor.h>                // for debugMacros.h
#include "debugmacros.h"        // for ASSERTE
#include "opinfo.h"


OpInfo::OpInfoData OpInfo::table[] = {

#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
    { s, (OPCODE_FORMAT) (args + type), FLOW_ ## ctrl, pop, push, c },

    // Kind of a workaround, get the prefixes (IInternal) to return InlineOpcode instead of InlineNone
#define IInternal   (InlineOpcode - InlineNone)
#define IMacro      0
#define IPrimitive  0
#define IAnnotation 0
#define IObjModel   0
#define IPrefix     0

#define Pop0    0
#define Pop1    1
#define PopI    1
#define PopI4   1
#define PopR4   1
#define PopI8   1
#define PopR8   1
#define PopRef  1
#define VarPop  -1

#define Push0    0
#define Push1    1
#define PushI    1
#define PushI4   1
#define PushR4   1
#define PushI8   1
#define PushR8   1
#define PushRef  1
#define VarPush  -1

#include "opcode.def"
#undef OPDEF
};


/***************************************************************************/
/* parse instruction at 'instrPtr', into its opcode (OpInfo), and its
   (inline)args, 'args'  'instrPtr' is updated */

/***************************************************************************/
const BYTE* OpInfo::fetch(const BYTE* instrPtr, OpArgsVal* args) {

    data = &table[*instrPtr++];
AGAIN:
    _ASSERTE(data - table == data->opcode);
    switch(data->format) {
        case InlineNone:
            break;
        case InlineOpcode:
            _ASSERTE(*instrPtr + 256 < (int) (sizeof(table) / sizeof(OpInfoData)));
            data = &table[256 + *instrPtr++];
            goto AGAIN;

        case ShortInlineVar:
            args->i = *instrPtr; instrPtr +=1;
            break;
        case InlineVar:
            args->i = GET_UNALIGNED_VAL16(instrPtr); instrPtr +=2;
            break;
        case ShortInlineI:
        case ShortInlineBrTarget:
            args->i = *instrPtr; instrPtr +=1;
            break;
        case ShortInlineR: {
            DWORD f = GET_UNALIGNED_VAL32(instrPtr); instrPtr +=4;
            args->r = *((float*) (&f));
            }
            break;
        case InlineRVA:
        case InlineI:
        case InlineMethod:
        case InlineField:
        case InlineType:
        case InlineString:
        case InlineSig:
        case InlineTok:
        case InlineBrTarget:
            args->i = GET_UNALIGNED_VAL32(instrPtr); instrPtr +=4;
            break;
        case InlineI8:
            args->i8 = GET_UNALIGNED_VAL64(instrPtr); instrPtr +=8;
            break;
        case InlineR: {
            __int64 d = GET_UNALIGNED_VAL64(instrPtr); instrPtr +=8;
            args->r = *((double*) (&d));
            } break;
        case InlineSwitch:
            args->switch_.count   = GET_UNALIGNED_VAL32(instrPtr); instrPtr +=4;
            args->switch_.targets = (int*) instrPtr; instrPtr += (4 * args->switch_.count);
            break;
        case InlinePhi:
            args->phi.count = GET_UNALIGNED_VAL32(instrPtr); instrPtr +=1;
            args->phi.vars  = (unsigned short*) instrPtr; instrPtr += (2 * args->phi.count);
            break;
        default:
#ifdef _DEBUG
            _ASSERTE(!"BadType");
#else
            __assume(0);        // we are really certain the default case does not happen
#endif
            break;
        }
    return(instrPtr);
}

