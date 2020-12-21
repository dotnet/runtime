// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __openum_h__
#define __openum_h__


typedef enum opcode_t
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) c,
#include "opcode.def"
#undef OPDEF
  CEE_COUNT,        /* number of instructions and macros pre-defined */
} OPCODE;


typedef enum opcode_format_t
{
	InlineNone		= 0,	// no inline args
	InlineVar		= 1,	// local variable       (U2 (U1 if Short on))
	InlineI			= 2,	// an signed integer    (I4 (I1 if Short on))
	InlineR			= 3,	// a real number        (R8 (R4 if Short on))
	InlineBrTarget	= 4,    // branch target        (I4 (I1 if Short on))
	InlineI8		= 5,
	InlineMethod	= 6,   // method token (U4)
	InlineField		= 7,   // field token  (U4)
	InlineType		= 8,   // type token   (U4)
	InlineString	= 9,   // string TOKEN (U4)
	InlineSig		= 10,  // signature tok (U4)
	InlineRVA		= 11,  // ldptr token  (U4)
	InlineTok		= 12,  // a meta-data token of unknown type (U4)
	InlineSwitch	= 13,  // count (U4), pcrel1 (U4) .... pcrelN (U4)
	InlinePhi		= 14,  // count (U1), var1 (U2) ... varN (U2)

	// WATCH OUT we are close to the limit here, if you add
	// more enumerations you need to change ShortIline definition below

	// The extended enumeration also encodes the size in the IL stream
	ShortInline 	= 16,						// if this bit is set, the format is the 'short' format
	PrimaryMask   	= (ShortInline-1),			// mask these off to get primary enumeration above
	ShortInlineVar 	= (ShortInline + InlineVar),
	ShortInlineI	= (ShortInline + InlineI),
	ShortInlineR	= (ShortInline + InlineR),
	ShortInlineBrTarget = (ShortInline + InlineBrTarget),
	InlineOpcode	= (ShortInline + InlineNone),    // This is only used internally.  It means the 'opcode' is two byte instead of 1
} OPCODE_FORMAT;

#endif /* __openum_h__ */


